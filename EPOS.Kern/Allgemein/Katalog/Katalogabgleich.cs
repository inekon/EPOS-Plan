using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER KATALOGABGLEICH NACH DER SCHEMAMIGRATION - Entscheidungsvorlage Modellgrenzen KU1 Stufe 1 und 2.
    //
    // JE SATZ DES PAKETS (Tabelle, Schlüssel):
    //   Schlüssel fehlt in der Datenbank                → EINFÜGEN (ReadOnly = 1, Schlüssel, Prüfsumme)
    //   Zeile = neuer Auslieferungsstand                → nichts (unverändert)
    //   Zeile gesperrt und Prüfsumme der Zeile = die
    //     gespeicherte ausgelieferte Prüfsumme          → AKTUALISIEREN (neue Werte, neue Prüfsumme)
    //   Zeile geändert (Prüfsumme weicht ab) oder
    //     entsperrt (ReadOnly = 0)                      → BEHALTEN, im Protokoll „Ihre Anpassung bleibt;
    //                                                     der neue Auslieferungsstand liegt als Vergleich vor"
    // JE SATZ MIT SCHLÜSSEL, DEN DAS PAKET NICHT MEHR FÜHRT → AUSGELAUFEN (Katalog_Ausgelaufen = 1);
    //   gelöscht wird nie.
    //
    // ANBINDEN (Schlüssel fehlt, Name im Katalog belegt): Ein Satz, der ungesperrt ausgeliefert und
    // später in der Quelle gesperrt wurde, liegt beim Anwender als Zeile ohne Schlüssel. Trägt in
    // derselben Katalogtabelle GENAU EINE Zeile den Namen des Paketsatzes (ohne Unterschied von Groß-
    // und Kleinschreibung) und ist sie ohne Schlüssel und ungesperrt, bekommt sie Schlüssel, die
    // Prüfsumme des Paketsatzes und ReadOnly = 1; ihre ID und ihre Werte bleiben, Kindzeilen bleiben:
    //   Inhalt = Paketsatz (Prüfsumme)                  → ANGEBUNDEN; künftige Fassungen führen sie nach
    //   Inhalt weicht ab                                → ANGEBUNDEN_BEHALTEN; die Werte des Anwenders
    //                                                     bleiben, die Zeile gilt danach als geänderte
    //                                                     gesperrte Zeile (BEHALTEN, solange sie abweicht)
    //   mehrere gleichnamige Zeilen ohne Schlüssel      → BEHALTEN „Name mehrdeutig“
    //   gleichnamige Zeile mit anderem Schlüssel        → BEHALTEN „Name gehört einem anderen Satz“
    //   einzige gleichnamige Zeile gesperrt             → BEHALTEN „Name belegt“
    // Im Protokoll steht ANGEBUNDEN als AKTUALISIERT und ANGEBUNDEN_BEHALTEN als BEHALTEN, je mit eigenem
    // Hinweis (Tab_Katalogabgleich.Aktion lässt nur die Aktionen seines Schemaschritts zu).
    //
    // NIE ANGEFASST: Anwenderzeilen (ohne Schlüssel) und jede Projektkopie (Tab_* mit ID_Projekt;
    // die Kühlkennlinie trägt eine Projektspalte und wird nur mit ID_Projekt 0 oder leer gelesen und
    // geschrieben). Brennstoffe und Vorgaben der Pufferauslegung haben ihre Projektkopie in eigenen
    // Tabellen (Tab_Brennstoff, Tab_PufferAuslegungParameter); vor dem ersten Schreiben legt der Abgleich
    // die fehlenden Kopien wertgleich an. Konditionierungsvorlagen werden bei der Übernahme kopiert.
    // Projekte rechnen nach dem Abgleich wie vorher.
    //
    // TRANSAKTIONAL UND WIEDERHOLBAR. Alles in EINEM Vorgang, samt Protokoll und neuer Fassung an
    // Tab_Applikation. Steht die Datenbank schon auf der Fassung des Pakets, tut ein zweiter Lauf
    // nichts; auch ein erzwungener Lauf schreibt keine doppelte Protokollzeile.
    //
    // KEIN ABGLEICH OHNE PAKET: Fehlt die Datei oder ist sie beschädigt, steht das benannt im
    // Protokoll (Aktion KEIN_PAKET), und der Katalog bleibt, wie er ist.
    // ====================================================================================

    /// <summary>Eine Zeile des Abgleichs (geplant oder ausgeführt).</summary>
    public sealed class KatalogabgleichEintrag
    {
        internal KatalogabgleichEintrag(string tabelle, string schluessel, string bezeichner, string aktion, string hinweis)
        {
            Tabelle = tabelle;
            Schluessel = schluessel;
            Bezeichner = bezeichner;
            Aktion = aktion;
            Hinweis = hinweis ?? "";
        }

        /// <summary>Die Katalogtabelle.</summary>
        public string Tabelle { get; }

        /// <summary>Der Schlüssel des Satzes.</summary>
        public string Schluessel { get; }

        /// <summary>Der Bezeichner (Anzeige).</summary>
        public string Bezeichner { get; }

        /// <summary>Die Aktion (<c>Katalogabgleich.AKTION_*</c>).</summary>
        public string Aktion { get; }

        /// <summary>Der Hinweis für den Anwender.</summary>
        public string Hinweis { get; }

        /// <summary>Kann der Auslieferungsstand dieses Satzes wiederhergestellt werden?</summary>
        public bool Wiederherstellbar =>
            (Aktion == Katalogabgleich.AKTION_BEHALTEN || Aktion == Katalogabgleich.AKTION_ANGEBUNDEN_BEHALTEN) && Id > 0;

        internal long Id { get; set; }
        internal Katalogpaketsatz Satz { get; set; }
    }

    /// <summary>Was ein Abgleich ergeben hat.</summary>
    public sealed class KatalogabgleichErgebnis
    {
        /// <summary>Wurde abgeglichen (oder, bei <see cref="NurGeprueft"/>, geplant)?</summary>
        public bool Ausgefuehrt { get; internal set; }

        /// <summary>„Nur prüfen": nichts geschrieben.</summary>
        public bool NurGeprueft { get; internal set; }

        /// <summary>Die Fassung des Pakets (0 ohne Paket).</summary>
        public int Fassung { get; internal set; }

        /// <summary>Die Fassung der Datenbank vor dem Lauf (<c>null</c> = noch nie abgeglichen).</summary>
        public int? FassungVorher { get; internal set; }

        /// <summary>Eingefügte Sätze.</summary>
        public int Neu { get; internal set; }

        /// <summary>Aktualisierte Sätze.</summary>
        public int Aktualisiert { get; internal set; }

        /// <summary>Behaltene Anpassungen (samt den angebundenen Zeilen mit abweichendem Inhalt).</summary>
        public int Behalten { get; internal set; }

        /// <summary>An eine gleichnamige Anwenderzeile angebundene Sätze (gleicher und abweichender Inhalt).</summary>
        public int Angebunden { get; internal set; }

        /// <summary>Ausgelaufene Sätze.</summary>
        public int Ausgelaufen { get; internal set; }

        /// <summary>Unveränderte Sätze (schon auf dem Stand des Pakets).</summary>
        public int Unveraendert { get; internal set; }

        /// <summary>Die Zeilen je Satz mit Aktion (ohne die unveränderten).</summary>
        public List<KatalogabgleichEintrag> Eintraege { get; } = new List<KatalogabgleichEintrag>();

        /// <summary>Warum nicht abgeglichen wurde (leer, wenn abgeglichen).</summary>
        public string Meldung { get; internal set; } = "";

        /// <summary>Der Pfad der Sicherung vor dem Abgleich (leer ohne Sicherung).</summary>
        public string Sicherung { get; internal set; } = "";

        /// <summary>Hat der Lauf etwas geändert (oder würde er)?</summary>
        public bool EtwasZuTun => Neu + Aktualisiert + Ausgelaufen + Angebunden > 0;

        /// <summary>„n neu, m aktualisiert, k behalten, a ausgelaufen" — mit angebundenen Sätzen „, b angebunden".</summary>
        public string Zusammenfassung() =>
            string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_BERICHT,
                          Neu, Aktualisiert, Behalten, Ausgelaufen) +
            (Angebunden > 0
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_BERICHT_ANGEBUNDEN, Angebunden)
                : "");

        /// <summary>
        /// Der Text der Überlagerung beim Start: Fassung und Zusammenfassung, bei behaltenen
        /// Anpassungen der Weg zum Vergleich, dazu die Sicherung.
        /// </summary>
        public string Starttext()
        {
            string text = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_START_TEXT,
                                        Fassung, Zusammenfassung());
            if (Behalten > 0) text += "\n\n" + MyResource.Resource.KABG_START_BEHALTEN;
            if (!string.IsNullOrEmpty(Sicherung))
                text += "\n\n" + string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_SICHERUNG, Sicherung);
            return text;
        }
    }

    /// <summary>Eine Zeile des Protokolls <c>Tab_Katalogabgleich</c>.</summary>
    public sealed record KatalogabgleichProtokollzeile(
        string Zeitpunkt, int Fassung, string Tabelle, string Schluessel, string Aktion, string Hinweis);

    /// <summary>
    /// <b>Der Katalogabgleich</b> — Regeln im Kopf der Datei.
    /// </summary>
    public static class Katalogabgleich
    {
        /// <summary>Satz neu eingefügt.</summary>
        public const string AKTION_EINGEFUEGT = "EINGEFUEGT";

        /// <summary>Satz auf den neuen Auslieferungsstand gebracht.</summary>
        public const string AKTION_AKTUALISIERT = "AKTUALISIERT";

        /// <summary>Anpassung des Anwenders behalten.</summary>
        public const string AKTION_BEHALTEN = "BEHALTEN";

        /// <summary>Gleichnamige Anwenderzeile mit dem Inhalt des Paketsatzes angebunden (Schlüssel, Prüfsumme, gesperrt).</summary>
        public const string AKTION_ANGEBUNDEN = "ANGEBUNDEN";

        /// <summary>Gleichnamige Anwenderzeile angebunden, ihr abweichender Inhalt behalten.</summary>
        public const string AKTION_ANGEBUNDEN_BEHALTEN = "ANGEBUNDEN_BEHALTEN";

        /// <summary>Satz in der Auslieferung entfallen, bleibt stehen.</summary>
        public const string AKTION_AUSGELAUFEN = "AUSGELAUFEN";

        /// <summary>Auslieferungsstand eines Satzes auf Wunsch wiederhergestellt.</summary>
        public const string AKTION_WIEDERHERGESTELLT = "WIEDERHERGESTELLT";

        /// <summary>Kein (lesbares) Paket — nicht abgeglichen.</summary>
        public const string AKTION_KEIN_PAKET = "KEIN_PAKET";

        /// <summary>Die Zusammenfassung eines Laufs.</summary>
        public const string AKTION_BERICHT = "BERICHT";

        /// <summary>
        /// Der Bericht des letzten Abgleichs beim Programmstart — die Überlagerung der Oberfläche
        /// liest ihn einmal (<see cref="StartberichtAbholen"/>). <c>null</c> = nichts zu melden.
        /// </summary>
        private static KatalogabgleichErgebnis _startbericht;

        /// <summary>Holt den Startbericht ab (einmal; danach <c>null</c>).</summary>
        public static KatalogabgleichErgebnis StartberichtAbholen()
        {
            KatalogabgleichErgebnis e = _startbericht;
            _startbericht = null;
            return e;
        }

        /// <summary>Ist die Datenbank für den Abgleich eingerichtet (Schemaschritt gelaufen)?</summary>
        public static bool Bereit() =>
            DataRepository.TabelleVorhanden(KatalogfassungSchema.TAB_ABGLEICH) &&
            DataRepository.SpalteVorhanden(Katalogfassung.TAB_APPLIKATION, Katalogfassung.SPALTE_FASSUNG);

        /// <summary>Die Katalogfassung der Datenbank (<c>null</c> = noch nie abgeglichen oder nicht eingerichtet).</summary>
        public static int? FassungDerDatenbank()
        {
            if (!DataRepository.SpalteVorhanden(Katalogfassung.TAB_APPLIKATION, Katalogfassung.SPALTE_FASSUNG)) return null;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT MAX(\"" + Katalogfassung.SPALTE_FASSUNG + "\") FROM \"" + Katalogfassung.TAB_APPLIKATION + "\"");
            if (dt == null || dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return null;
            return Convert.ToInt32(dt.Rows[0][0], CultureInfo.InvariantCulture);
        }

        // =================================================================================
        // Einstiege
        // =================================================================================

        /// <summary>
        /// <b>Der Abgleich beim Programmstart</b>, nach der Schemamigration: nur wenn das Paket eine
        /// NEUERE Fassung trägt als die Datenbank, und erst nach einer Sicherung per
        /// <c>VACUUM INTO</c>. Ohne Paket geschieht nichts (eine Neuinstallation ohne Paket ist kein
        /// Fehler); ein beschädigtes Paket steht im Protokoll. Das Ergebnis liegt danach für die
        /// Überlagerung bereit (<see cref="StartberichtAbholen"/>), wenn sich etwas geändert hat.
        /// </summary>
        /// <param name="paketpfad">Der Ort des Pakets (<see cref="Katalogpaket.Pfad"/>).</param>
        /// <param name="sichern">Legt die Sicherung an und liefert ihren Pfad; <c>null</c> = ohne Sicherung (Tests).</param>
        public static KatalogabgleichErgebnis BeimStart(string paketpfad, Func<string> sichern)
        {
            var leer = new KatalogabgleichErgebnis { FassungVorher = FassungDerDatenbank() };
            if (!Bereit()) { leer.Meldung = MyResource.Resource.KABG_NICHT_BEREIT; return leer; }
            if (string.IsNullOrWhiteSpace(paketpfad) || !File.Exists(paketpfad)) return leer;

            Katalogpaket paket;
            try { paket = Katalogpaket.Lesen(paketpfad); }
            catch (Exception ex)
            {
                return KeinPaket(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_PAKET_FEHLER, ex.Message), false);
            }
            if (leer.FassungVorher.HasValue && leer.FassungVorher.Value >= paket.Fassung) return leer;

            string sicherung = "";
            if (sichern != null) sicherung = sichern() ?? "";
            KatalogabgleichErgebnis e = Ausfuehren(paket, nurPruefen: false, erzwingen: false);
            e.Sicherung = sicherung;
            if (e.Ausgefuehrt && (e.EtwasZuTun || e.Behalten > 0)) _startbericht = e;
            return e;
        }

        /// <summary>
        /// Der Abgleich aus einer Paketdatei — der Weg des Dialogs. Ohne Paket: benannt im Protokoll
        /// (außer bei „Nur prüfen") und als <see cref="KatalogabgleichErgebnis.Meldung"/>.
        /// </summary>
        public static KatalogabgleichErgebnis AusDatei(string paketpfad, bool nurPruefen, bool erzwingen = false)
        {
            if (!Bereit())
                return new KatalogabgleichErgebnis { Meldung = MyResource.Resource.KABG_NICHT_BEREIT, NurGeprueft = nurPruefen };
            if (string.IsNullOrWhiteSpace(paketpfad) || !File.Exists(paketpfad))
                return KeinPaket(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_KEIN_PAKET,
                                               string.IsNullOrWhiteSpace(paketpfad) ? "—" : paketpfad), nurPruefen);
            Katalogpaket paket;
            try { paket = Katalogpaket.Lesen(paketpfad); }
            catch (Exception ex)
            {
                return KeinPaket(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_PAKET_FEHLER, ex.Message), nurPruefen);
            }
            return Ausfuehren(paket, nurPruefen, erzwingen);
        }

        /// <summary>Kein Paket: Ergebnis mit Meldung, beim echten Lauf auch eine Protokollzeile.</summary>
        private static KatalogabgleichErgebnis KeinPaket(string meldung, bool nurPruefen)
        {
            var e = new KatalogabgleichErgebnis { Meldung = meldung, NurGeprueft = nurPruefen, FassungVorher = FassungDerDatenbank() };
            if (!nurPruefen && Bereit())
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        Protokollieren(v, e.FassungVorher ?? 0, "", null, AKTION_KEIN_PAKET, meldung, doppeltPruefen: false);
                        v.Commit();
                    }
                    catch { v.Rollback(); }
                }
            }
            return e;
        }

        /// <summary>
        /// <b>Der Abgleich</b> mit einem gelesenen Paket. <paramref name="nurPruefen"/> plant nur;
        /// <paramref name="erzwingen"/> gleicht auch ab, wenn die Datenbank die Fassung schon trägt.
        /// </summary>
        public static KatalogabgleichErgebnis Ausfuehren(Katalogpaket paket, bool nurPruefen, bool erzwingen = false)
        {
            if (paket == null) throw new ArgumentNullException(nameof(paket));
            var e = new KatalogabgleichErgebnis
            {
                Fassung = paket.Fassung,
                FassungVorher = FassungDerDatenbank(),
                NurGeprueft = nurPruefen,
            };
            if (!Bereit()) { e.Meldung = MyResource.Resource.KABG_NICHT_BEREIT; return e; }
            if (!erzwingen && e.FassungVorher.HasValue && e.FassungVorher.Value >= paket.Fassung)
            {
                e.Meldung = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_BEREITS,
                                          e.FassungVorher.Value);
                return e;
            }

            Planen(paket, e);
            e.Ausgefuehrt = true;
            if (nurPruefen) return e;

            bool kopien = ProjektkopienBereit();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    // Vor dem ersten Schreiben: die Projektkopien der Kataloge ohne eigene Projekttabelle
                    // (Brennstoffe, Vorgaben der Pufferauslegung) sichern - wertgleich zum alten Stamm.
                    if (kopien && e.EtwasZuTun) ProjektkopienSichern(v);
                    foreach (KatalogabgleichEintrag z in e.Eintraege)
                    {
                        Katalogtabelle t = Katalogfassung.Tabelle(z.Tabelle);
                        switch (z.Aktion)
                        {
                            case AKTION_EINGEFUEGT: Einfuegen(v, t, z.Satz); break;
                            case AKTION_AKTUALISIERT: Schreiben(v, t, z.Id, z.Satz); break;
                            case AKTION_ANGEBUNDEN:
                            case AKTION_ANGEBUNDEN_BEHALTEN: Anbinden(v, t, z.Id, z.Satz); break;
                            case AKTION_AUSGELAUFEN:
                                v.Ausfuehren("UPDATE \"" + t.Tabelle + "\" SET \"" + Katalogfassung.SPALTE_AUSGELAUFEN +
                                             "\" = 1 WHERE ID = ?", new DbParam("@id", z.Id));
                                break;
                        }
                        Protokollieren(v, paket.Fassung, z.Tabelle, z.Schluessel, Protokollaktion(z.Aktion), z.Hinweis, doppeltPruefen: true);
                    }
                    if (e.EtwasZuTun || !e.FassungVorher.HasValue || e.FassungVorher.Value != paket.Fassung)
                        Protokollieren(v, paket.Fassung, "", null, AKTION_BERICHT, e.Zusammenfassung(), doppeltPruefen: true);
                    v.Ausfuehren("UPDATE \"" + Katalogfassung.TAB_APPLIKATION + "\" SET \"" + Katalogfassung.SPALTE_FASSUNG + "\" = ?",
                                 new DbParam("@f", paket.Fassung));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return e;
        }

        // =================================================================================
        // Planen
        // =================================================================================

        /// <summary>Trägt je Satz des Pakets und je ausgelaufenen Satz die Aktion ein.</summary>
        private static void Planen(Katalogpaket paket, KatalogabgleichErgebnis e)
        {
            foreach (Katalogpakettabelle pt in paket.Tabellen)
            {
                Katalogtabelle t = Katalogfassung.Tabelle(pt.Tabelle);
                if (t == null || !DataRepository.TabelleVorhanden(t.Tabelle) || !Katalogfassung.SpaltenVorhanden(t.Tabelle)) continue;
                List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
                Dictionary<string, DataRow> zeilen = ZeilenMitSchluessel(t, spalten);
                HashSet<string> namen = Bezeichner(t);
                Dictionary<string, List<DataRow>> gleichnamige = null;   // erst gelesen, wenn ein Name belegt ist
                var angebunden = new HashSet<long>();
                var imPaket = new HashSet<string>(StringComparer.Ordinal);

                foreach (Katalogpaketsatz s in pt.Saetze)
                {
                    imPaket.Add(s.Schluessel);
                    if (!zeilen.TryGetValue(s.Schluessel, out DataRow r))
                    {
                        if (namen.Contains(s.Bezeichner))
                        {
                            gleichnamige ??= ZeilenNachName(t, spalten);
                            (DataRow ziel, string hinweis) = Anbindbar(t, gleichnamige, s);
                            if (ziel != null && angebunden.Add(Convert.ToInt64(ziel["ID"], CultureInfo.InvariantCulture)))
                            {
                                long zielId = Convert.ToInt64(ziel["ID"], CultureInfo.InvariantCulture);
                                bool gleich = string.Equals(
                                    Katalogfassung.PruefsummeDerZeile(t, spalten, ziel, Katalogfassung.LeseOhneVorgang),
                                    s.Pruefsumme, StringComparison.Ordinal);
                                e.Angebunden++;
                                if (!gleich) e.Behalten++;
                                e.Eintraege.Add(new KatalogabgleichEintrag(t.Tabelle, s.Schluessel, Katalogfassung.Name(t, ziel),
                                    gleich ? AKTION_ANGEBUNDEN : AKTION_ANGEBUNDEN_BEHALTEN,
                                    gleich ? MyResource.Resource.KABG_HINWEIS_ANGEBUNDEN : MyResource.Resource.KABG_HINWEIS_ANGEBUNDEN_BEHALTEN)
                                { Satz = s, Id = zielId });
                                continue;
                            }
                            e.Behalten++;
                            e.Eintraege.Add(new KatalogabgleichEintrag(t.Tabelle, s.Schluessel, s.Bezeichner, AKTION_BEHALTEN,
                                hinweis ?? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_HINWEIS_NAME_BELEGT, s.Bezeichner))
                            { Satz = s, Id = 0 });
                            continue;
                        }
                        e.Neu++;
                        e.Eintraege.Add(new KatalogabgleichEintrag(t.Tabelle, s.Schluessel, s.Bezeichner, AKTION_EINGEFUEGT, "") { Satz = s });
                        continue;
                    }

                    long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                    string ist = Katalogfassung.PruefsummeDerZeile(t, spalten, r, Katalogfassung.LeseOhneVorgang);
                    string gespeichert = r[Katalogfassung.SPALTE_PRUEFSUMME] as string ?? "";
                    bool gesperrt = Gesperrt(r);
                    bool ausgelaufen = Ganz(r[Katalogfassung.SPALTE_AUSGELAUFEN]) != 0;
                    string bezeichner = Katalogfassung.Name(t, r);

                    bool gleichNeu = string.Equals(ist, s.Pruefsumme, StringComparison.Ordinal);
                    if (gesperrt)
                    {
                        if (gleichNeu && !ausgelaufen && string.Equals(gespeichert, s.Pruefsumme, StringComparison.Ordinal))
                        {
                            e.Unveraendert++;
                            continue;
                        }
                        // Wie ausgeliefert (oder schon mit den neuen Werten): der neue Stand darf darüber.
                        if (gleichNeu || string.Equals(ist, gespeichert, StringComparison.Ordinal))
                        {
                            e.Aktualisiert++;
                            e.Eintraege.Add(new KatalogabgleichEintrag(t.Tabelle, s.Schluessel, bezeichner, AKTION_AKTUALISIERT, "")
                            { Satz = s, Id = id });
                            continue;
                        }
                    }
                    else if (gleichNeu)
                    {
                        // Der Anwender hat entsperrt, aber die Werte sind schon die neuen - nichts zu tun.
                        e.Unveraendert++;
                        continue;
                    }
                    e.Behalten++;
                    e.Eintraege.Add(new KatalogabgleichEintrag(t.Tabelle, s.Schluessel, bezeichner, AKTION_BEHALTEN,
                        gesperrt ? MyResource.Resource.KABG_HINWEIS_BEHALTEN_GEAENDERT : MyResource.Resource.KABG_HINWEIS_BEHALTEN_ENTSPERRT)
                    { Satz = s, Id = id });
                }

                foreach (KeyValuePair<string, DataRow> kv in zeilen)
                {
                    if (imPaket.Contains(kv.Key)) continue;
                    if (Ganz(kv.Value[Katalogfassung.SPALTE_AUSGELAUFEN]) != 0) continue;
                    e.Ausgelaufen++;
                    e.Eintraege.Add(new KatalogabgleichEintrag(t.Tabelle, kv.Key,
                        Katalogfassung.Name(t, kv.Value),
                        AKTION_AUSGELAUFEN, MyResource.Resource.KABG_HINWEIS_AUSGELAUFEN)
                    { Id = Convert.ToInt64(kv.Value["ID"], CultureInfo.InvariantCulture) });
                }
            }
        }

        /// <summary>
        /// Die Aktion der Protokollzeile. <c>Tab_Katalogabgleich.Aktion</c> lässt nur die Aktionen seines
        /// Schemaschritts zu; ein angebundener Satz steht deshalb als <c>AKTUALISIERT</c> (gleicher Inhalt)
        /// bzw. <c>BEHALTEN</c> (abweichender Inhalt) im Protokoll, kenntlich an seinem eigenen Hinweis.
        /// </summary>
        internal static string Protokollaktion(string aktion) => aktion switch
        {
            AKTION_ANGEBUNDEN => AKTION_AKTUALISIERT,
            AKTION_ANGEBUNDEN_BEHALTEN => AKTION_BEHALTEN,
            _ => aktion,
        };

        private static bool Gesperrt(DataRow r) => Ganz(r[Katalogfassung.SPALTE_READONLY]) != 0;

        private static long Ganz(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            if (o is bool b) return b ? 1 : 0;
            return Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Die Zeilen mit Schlüssel einer Tabelle, nach Schlüssel.</summary>
        private static Dictionary<string, DataRow> ZeilenMitSchluessel(Katalogtabelle t, List<string> spalten)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, \"ReadOnly\", \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\", \"" + Katalogfassung.SPALTE_PRUEFSUMME +
                "\", \"" + Katalogfassung.SPALTE_AUSGELAUFEN + "\", " + Katalogfassung.Spaltentext(spalten) +
                " FROM \"" + t.Tabelle + "\" WHERE \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" IS NOT NULL ORDER BY ID");
            var d = new Dictionary<string, DataRow>(StringComparer.Ordinal);
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    d[Convert.ToString(r[Katalogfassung.SPALTE_SCHLUESSEL], CultureInfo.InvariantCulture)] = r;
            return d;
        }

        /// <summary>
        /// Alle Namen einer Tabelle (aus ihren Namensspalten, eindeutig). Ohne Unterschied von Groß- und
        /// Kleinschreibung: Ein Name, der sich nur darin unterscheidet, gilt als belegt — so stößt ein
        /// eingefügter Satz nie an einen Index mit <c>COLLATE NOCASE</c>.
        /// </summary>
        private static HashSet<string> Bezeichner(Katalogtabelle t)
        {
            var h = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> spalten = t.Namensspalten.Where(s => DataRepository.SpalteVorhanden(t.Tabelle, s)).ToList();
            if (spalten.Count == 0) return h;
            DataTable dt = DataRepository.GetDataTable("SELECT " + Katalogfassung.Spaltentext(spalten) + " FROM \"" + t.Tabelle + "\"");
            if (dt != null)
                foreach (DataRow r in dt.Rows) h.Add(Katalogfassung.Name(t, r));
            return h;
        }

        /// <summary>
        /// Alle Zeilen einer Katalogtabelle nach Name (ohne Unterschied von Groß- und Kleinschreibung, wie
        /// <see cref="Bezeichner"/>), mit ID, Schloss, Schlüssel und Fachspalten — die Kandidaten des Anbindens.
        /// </summary>
        private static Dictionary<string, List<DataRow>> ZeilenNachName(Katalogtabelle t, List<string> spalten)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, \"ReadOnly\", \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\", " + Katalogfassung.Spaltentext(spalten) +
                " FROM \"" + t.Tabelle + "\" ORDER BY ID");
            var d = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                {
                    string name = Katalogfassung.Name(t, r);
                    if (!d.TryGetValue(name, out List<DataRow> liste)) d[name] = liste = new List<DataRow>();
                    liste.Add(r);
                }
            return d;
        }

        /// <summary>
        /// <b>Die Regel des Anbindens</b> für einen Paketsatz, dessen Schlüssel fehlt und dessen Name belegt ist:
        /// angebunden wird nur die EINE gleichnamige Zeile ohne Schlüssel und ohne Schloss. Sonst <c>null</c>
        /// mit dem Hinweis, warum nicht (mehrdeutig, fremder Schlüssel; <c>null</c> = „Name belegt").
        /// </summary>
        private static (DataRow Zeile, string Hinweis) Anbindbar(Katalogtabelle t, Dictionary<string, List<DataRow>> gleichnamige,
                                                                  Katalogpaketsatz s)
        {
            if (!gleichnamige.TryGetValue(s.Bezeichner, out List<DataRow> liste) || liste.Count == 0) return (null, null);
            List<DataRow> ohneSchluessel = liste.Where(r => r[Katalogfassung.SPALTE_SCHLUESSEL] == DBNull.Value).ToList();
            if (ohneSchluessel.Count > 1)
                return (null, string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_HINWEIS_NAME_MEHRDEUTIG, s.Bezeichner));
            if (ohneSchluessel.Count < liste.Count)
                return (null, string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_HINWEIS_NAME_FREMDER_SCHLUESSEL, s.Bezeichner));
            if (Gesperrt(ohneSchluessel[0])) return (null, null);
            return (ohneSchluessel[0], null);
        }

        // =================================================================================
        // Schreiben
        // =================================================================================

        /// <summary>
        /// Bindet eine Anwenderzeile an einen Paketsatz: Schlüssel, Prüfsumme des Paketsatzes, gesperrt, nicht
        /// ausgelaufen. ID, Werte und Kindzeilen bleiben — Projektkopien und Zuordnungen über die ID stimmen weiter.
        /// </summary>
        private static void Anbinden(DbVorgang v, Katalogtabelle t, long id, Katalogpaketsatz s)
        {
            int n = v.Ausfuehren("UPDATE \"" + t.Tabelle + "\" SET \"ReadOnly\" = 1, \"" + Katalogfassung.SPALTE_SCHLUESSEL +
                                 "\" = ?, \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" = ?, \"" + Katalogfassung.SPALTE_AUSGELAUFEN +
                                 "\" = 0 WHERE ID = ? AND \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" IS NULL",
                                 new DbParam("@s", s.Schluessel), new DbParam("@h", s.Pruefsumme), new DbParam("@id", id));
            if (n != 1)
                throw new InvalidOperationException("Katalogabgleich: Zeile " + id.ToString(CultureInfo.InvariantCulture) + " in " +
                                                    t.Tabelle + " ist nicht mehr anbindbar.");
        }

        /// <summary>Fügt einen Satz des Pakets ein (ReadOnly = 1) samt Kindzeilen.</summary>
        private static long Einfuegen(DbVorgang v, Katalogtabelle t, Katalogpaketsatz s)
        {
            List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
            var p = new List<DbParam>();
            foreach (string sp in spalten)
                p.Add(new DbParam("@p" + p.Count.ToString(CultureInfo.InvariantCulture), Wert(v, t.Verweise, s.Werte, sp)));
            p.Add(new DbParam("@s", s.Schluessel));
            p.Add(new DbParam("@h", s.Pruefsumme));
            string sql = "INSERT INTO \"" + t.Tabelle + "\" (" + Katalogfassung.Spaltentext(spalten) + ", \"ReadOnly\", \"" +
                         Katalogfassung.SPALTE_SCHLUESSEL + "\", \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\", \"" +
                         Katalogfassung.SPALTE_AUSGELAUFEN + "\") VALUES (" +
                         string.Join(", ", spalten.Select(_ => "?")) + ", 1, ?, ?, 0)";
            long id = v.EinfuegenUndId(sql, p.ToArray());
            KinderSchreiben(v, t, id, s);
            return id;
        }

        /// <summary>Schreibt die Werte des Pakets über eine Zeile (gesperrt, neue Prüfsumme, nicht ausgelaufen).</summary>
        private static void Schreiben(DbVorgang v, Katalogtabelle t, long id, Katalogpaketsatz s)
        {
            List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
            var p = new List<DbParam>();
            foreach (string sp in spalten)
                p.Add(new DbParam("@p" + p.Count.ToString(CultureInfo.InvariantCulture), Wert(v, t.Verweise, s.Werte, sp)));
            p.Add(new DbParam("@s", s.Schluessel));
            p.Add(new DbParam("@h", s.Pruefsumme));
            p.Add(new DbParam("@id", id));
            v.Ausfuehren("UPDATE \"" + t.Tabelle + "\" SET " + string.Join(", ", spalten.Select(sp => "\"" + sp + "\" = ?")) +
                         ", \"ReadOnly\" = 1, \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" = ?, \"" +
                         Katalogfassung.SPALTE_PRUEFSUMME + "\" = ?, \"" + Katalogfassung.SPALTE_AUSGELAUFEN +
                         "\" = 0 WHERE ID = ?", p.ToArray());
            foreach (Katalogkind k in t.Kinder) KinderLoeschen(v, k, id);
            KinderSchreiben(v, t, id, s);
        }

        /// <summary>Löscht die Katalogzeilen eines Kindes zu einer Eigentümer-ID, seine Enkel zuerst.</summary>
        private static void KinderLoeschen(DbVorgang v, Katalogkind k, long id)
        {
            if (!DataRepository.TabelleVorhanden(k.Tabelle)) return;
            if (k.Enkel.Count > 0)
            {
                DataTable dt = v.Lese("SELECT ID FROM \"" + k.Tabelle + "\" WHERE \"" + k.Fremdschluessel + "\" = ?" +
                                      k.Katalogbedingung, new DbParam("@id", id));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        foreach (Katalogkind e in k.Enkel)
                            KinderLoeschen(v, e, Convert.ToInt64(r[0], CultureInfo.InvariantCulture));
            }
            v.Ausfuehren("DELETE FROM \"" + k.Tabelle + "\" WHERE \"" + k.Fremdschluessel + "\" = ?" + k.Katalogbedingung,
                         new DbParam("@id", id));
        }

        private static void KinderSchreiben(DbVorgang v, Katalogtabelle t, long id, Katalogpaketsatz s)
        {
            foreach (Katalogkind k in t.Kinder)
            {
                if (!s.Kinder.TryGetValue(k.Tabelle, out List<Dictionary<string, object>> zeilen)) continue;
                Kindzeilen(v, k, id, zeilen);
            }
        }

        /// <summary>Fügt die Zeilen eines Kindes zu einer Eigentümer-ID ein, je Zeile danach ihre Enkel.</summary>
        private static void Kindzeilen(DbVorgang v, Katalogkind k, long id, IEnumerable<IReadOnlyDictionary<string, object>> zeilen)
        {
            if (!DataRepository.TabelleVorhanden(k.Tabelle)) return;
            List<string> spalten = Katalogfassung.VorhandeneFachspalten(k);
            bool sperren = k.GesperrtEinfuegen && DataRepository.SpalteVorhanden(k.Tabelle, Katalogfassung.SPALTE_READONLY);
            string sql = "INSERT INTO \"" + k.Tabelle + "\" (\"" + k.Fremdschluessel + "\"" +
                         (k.Projektspalte != null ? ", \"" + k.Projektspalte + "\"" : "") +
                         (sperren ? ", \"" + Katalogfassung.SPALTE_READONLY + "\"" : "") +
                         (spalten.Count > 0 ? ", " + Katalogfassung.Spaltentext(spalten) : "") + ") VALUES (?" +
                         (k.Projektspalte != null ? ", 0" : "") +
                         (sperren ? ", 1" : "") +
                         string.Concat(spalten.Select(_ => ", ?")) + ")";
            foreach (IReadOnlyDictionary<string, object> z in zeilen)
            {
                var p = new List<DbParam> { new DbParam("@fk", id) };
                foreach (string sp in spalten)
                    p.Add(new DbParam("@p" + p.Count.ToString(CultureInfo.InvariantCulture), Wert(v, k.Verweise, z, sp)));
                if (k.Enkel.Count == 0)
                {
                    v.Ausfuehren(sql, p.ToArray());
                    continue;
                }
                long kindId = v.EinfuegenUndId(sql, p.ToArray());
                foreach (Katalogkind e in k.Enkel)
                    Kindzeilen(v, e, kindId, Katalogfassung.Enkelzeilen(z, e.Tabelle));
            }
        }

        /// <summary>Der Schreibwert einer Spalte — ein Verweis als ID des Ziels, gelesen im selben Vorgang.</summary>
        private static object Wert(DbVorgang v, IReadOnlyList<Katalogverweis> verweise,
                                   IReadOnlyDictionary<string, object> werte, string spalte) =>
            Katalogfassung.Schreibwert(verweise, werte, spalte, (sql, p) => v.Lese(sql, p));

        /// <summary>
        /// Eine Protokollzeile; mit <paramref name="doppeltPruefen"/> nur, wenn dieselbe (Fassung,
        /// Tabelle, Schlüssel, Aktion) noch nicht steht — ein wiederholter Lauf schreibt nichts doppelt.
        /// </summary>
        private static void Protokollieren(DbVorgang v, int fassung, string tabelle, string schluessel, string aktion,
                                           string hinweis, bool doppeltPruefen)
        {
            if (doppeltPruefen)
            {
                object o = v.Skalar("SELECT COUNT(*) FROM \"" + KatalogfassungSchema.TAB_ABGLEICH +
                                    "\" WHERE \"Fassung\" = ? AND \"Tabelle\" = ? AND COALESCE(\"Schluessel\", '') = ? AND \"Aktion\" = ?" +
                                    (aktion == AKTION_BERICHT ? " AND COALESCE(\"Hinweis\", '') = ?" : ""),
                                    aktion == AKTION_BERICHT
                                        ? new[] { new DbParam("@f", fassung), new DbParam("@t", tabelle ?? ""),
                                                  new DbParam("@s", schluessel ?? ""), new DbParam("@a", aktion),
                                                  new DbParam("@h", hinweis ?? "") }
                                        : new[] { new DbParam("@f", fassung), new DbParam("@t", tabelle ?? ""),
                                                  new DbParam("@s", schluessel ?? ""), new DbParam("@a", aktion) });
                if (o != null && o != DBNull.Value && Convert.ToInt64(o, CultureInfo.InvariantCulture) > 0) return;
            }
            v.Ausfuehren("INSERT INTO \"" + KatalogfassungSchema.TAB_ABGLEICH +
                         "\" (\"Zeitpunkt\", \"Fassung\", \"Tabelle\", \"Schluessel\", \"Aktion\", \"Hinweis\") VALUES (?, ?, ?, ?, ?, ?)",
                         new DbParam("@z", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                         new DbParam("@f", fassung), new DbParam("@t", tabelle ?? ""),
                         new DbParam("@s", (object)schluessel ?? DBNull.Value), new DbParam("@a", aktion),
                         new DbParam("@h", (object)hinweis ?? DBNull.Value));
        }

        // =================================================================================
        // Wiederherstellen und Protokoll
        // =================================================================================

        /// <summary>
        /// <b>Stellt den Auslieferungsstand EINES Satzes wieder her</b> — die Werte des Pakets,
        /// gesperrt, mit neuer Prüfsumme, nicht ausgelaufen; fehlt die Zeile, wird sie eingefügt.
        /// Liefert die Meldung für den Anwender; wirft nicht bei fachlichen Hindernissen.
        /// </summary>
        /// <returns>(ok, Meldung)</returns>
        public static (bool Ok, string Meldung) Wiederherstellen(Katalogpaket paket, string tabelle, string schluessel)
        {
            if (paket == null) return (false, MyResource.Resource.KABG_KEIN_PAKET_KURZ);
            Katalogtabelle t = Katalogfassung.Tabelle(tabelle);
            Katalogpaketsatz s = paket.Tabellen.FirstOrDefault(x => string.Equals(x.Tabelle, tabelle, StringComparison.Ordinal))?
                                      .Saetze.FirstOrDefault(x => string.Equals(x.Schluessel, schluessel, StringComparison.Ordinal));
            if (t == null || s == null || !Bereit() || !Katalogfassung.SpaltenVorhanden(t.Tabelle))
                return (false, MyResource.Resource.KABG_SATZ_NICHT_IM_PAKET);

            List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
            Dictionary<string, DataRow> zeilen = ZeilenMitSchluessel(t, spalten);
            zeilen.TryGetValue(s.Schluessel, out DataRow r);
            if (r == null && Bezeichner(t).Contains(s.Bezeichner))
            {
                // Noch nicht angebunden („Nur prüfen"): Die eine anbindbare Zeile bekommt den Auslieferungsstand.
                (DataRow ziel, string hinweis) = Anbindbar(t, ZeilenNachName(t, spalten), s);
                if (ziel == null)
                    return (false, hinweis ?? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_HINWEIS_NAME_BELEGT, s.Bezeichner));
                r = ziel;
            }

            bool kopien = ProjektkopienBereit();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    if (kopien) ProjektkopienSichern(v);
                    if (r == null) Einfuegen(v, t, s);
                    else Schreiben(v, t, Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture), s);
                    Protokollieren(v, paket.Fassung, t.Tabelle, s.Schluessel, AKTION_WIEDERHERGESTELLT,
                                   MyResource.Resource.KABG_WIEDERHERGESTELLT, doppeltPruefen: false);
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return (false, ex.Message);
                }
            }
            return (true, MyResource.Resource.KABG_WIEDERHERGESTELLT);
        }

        /// <summary>Stehen die Projektkopien der Brennstoffe und der Pufferauslegungs-Vorgaben (Schritt 175)?</summary>
        private static bool ProjektkopienBereit() => ProjektkopienKatalogeSchema.SchemaVollstaendig();

        /// <summary>
        /// <b>Die Vorstufe des Abgleichs:</b> legt im Vorgang die fehlenden Projektkopien an — je Projekt
        /// jede Brennstoffart, je Projekt mit Pufferauslegung jede Vorgabe —, wertgleich zum Stamm VOR dem
        /// Abgleich. Danach fasst der Abgleich nur den Stamm an, und kein Projekt liest einen geänderten
        /// Stammwert. (Die Konditionierungsvorlagen brauchen das nicht: „Vorlage übernehmen" kopiert.)
        /// </summary>
        private static void ProjektkopienSichern(DbVorgang v)
        {
            ProjektBrennstoffe.Sichern(v, null);
            ProjektPufferparameter.Sichern(v, null);
        }

        /// <summary>Die jüngsten Zeilen des Protokolls, neueste zuerst.</summary>
        public static IReadOnlyList<KatalogabgleichProtokollzeile> Protokoll(int hoechstens = 200)
        {
            var liste = new List<KatalogabgleichProtokollzeile>();
            if (!DataRepository.TabelleVorhanden(KatalogfassungSchema.TAB_ABGLEICH)) return liste;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"Zeitpunkt\", \"Fassung\", \"Tabelle\", \"Schluessel\", \"Aktion\", \"Hinweis\" FROM \"" +
                KatalogfassungSchema.TAB_ABGLEICH + "\" ORDER BY ID DESC LIMIT ?", new DbParam("@n", hoechstens));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add(new KatalogabgleichProtokollzeile(
                    Convert.ToString(r[0], CultureInfo.InvariantCulture),
                    (int)Ganz(r[1]),
                    Convert.ToString(r[2], CultureInfo.InvariantCulture),
                    r[3] == DBNull.Value ? "" : Convert.ToString(r[3], CultureInfo.InvariantCulture),
                    Convert.ToString(r[4], CultureInfo.InvariantCulture),
                    r[5] == DBNull.Value ? "" : Convert.ToString(r[5], CultureInfo.InvariantCulture)));
            return liste;
        }

        /// <summary>
        /// Die Sicherung vor dem Abgleich: <c>VACUUM INTO</c> in <c>DB-Backup</c> neben der Datenbank
        /// (falls es ihn gibt, sonst daneben) — über <see cref="Datenbanksicherung.KopieAnlegen"/>.
        /// </summary>
        public static string SicherungAnlegen()
        {
            string db = DataRepository.GetDBPath();
            string ordner = Path.GetDirectoryName(db) ?? "";
            string backup = Path.Combine(ordner, "DB-Backup");
            if (Directory.Exists(backup)) ordner = backup;
            return Datenbanksicherung.KopieAnlegen(db, ordner,
                Path.GetFileNameWithoutExtension(db) + "_Katalogabgleich");
        }
    }
}
