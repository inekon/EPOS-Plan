using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zapfkategorien einer Nutzungsart für den Editor der Stufe Experte (Umsetzungskonzept
    /// Zapfprofilgenerator 4.4, 5.1; Stufe Z4): die Kategorien in der Reihenfolge des Katalogs, die
    /// Summe der Anteile, der Hinweis bei Σ ≠ 1 und die Sperre der Nutzungsart — frei
    /// (<see cref="TwwKatalogAusgang.Ausgefuehrt"/>: Speichern an Ort und Stelle) oder gesperrt
    /// (Auslieferung bzw. benutzt: Speichern nur als neue Katalogversion).
    /// </summary>
    internal sealed record TwwKategorienStand(int IdNutzungsart, IReadOnlyList<Zapfkategorie> Kategorien, double SummeAnteil,
                                              ZapfSatz Hinweis, TwwKatalogAusgang Sperre)
    {
        /// <summary>Speichert <see cref="TwwNutzungsartCtrl.KategorienSpeichern"/> an Ort und Stelle?</summary>
        public bool Frei => Sperre == TwwKatalogAusgang.Ausgefuehrt;
    }

    /// <summary>
    /// Ergebnis von <see cref="TwwNutzungsartCtrl.KategorienSpeichern"/>: Ausgang, die Nutzungsart,
    /// die jetzt die Kategorien trägt (dieselbe oder die per „Speichern unter" neue — dann
    /// <see cref="NeueZeile"/>), und bei <see cref="TwwKatalogAusgang.RasterUngueltig"/> der
    /// verletzte Satz der Regeln (<see cref="Zapfkategoriensatz.Pruefen"/>).
    /// </summary>
    internal sealed record TwwKategorienErgebnis(TwwKatalogAusgang Ausgang, int IdNutzungsart, ZapfSatz Grund)
    {
        /// <summary>Ist die Aktion geschrieben (oder war nichts zu schreiben)?</summary>
        public bool Ok => Ausgang == TwwKatalogAusgang.Ausgefuehrt;

        /// <summary>Ist eine neue Nutzungsart (Katalogkopie) entstanden?</summary>
        public bool NeueZeile { get; init; }
    }

    /// <summary>
    /// <b>Kategorien als Katalogkopie</b> (Umsetzungskonzept Zapfprofilgenerator 4.4: „Im
    /// Experten-Modus sind Kategorien und σ als Katalogkopie bearbeitbar (Status EIGEN)"; 3.2; Stufe
    /// Z4, Gruppe 1). Dieselbe Sperrregel wie Tagesgang und Nutzungsart: Eine Nutzungsart der
    /// Auslieferung (auch über eine ausgelieferte Kategorie) oder eine, die eine Zone benutzt, ist
    /// unveränderlich — geänderte Kategorien ergeben dann per „Speichern unter" eine NEUE
    /// Nutzungsart (Katalogversion des Aufrufers, <c>ID_Vorlage</c> auf die alte, Status
    /// <c>EIGEN</c>, Werte und Tagesgangsatz der alten) mit den neuen Kategorien; die alte bleibt
    /// unberührt. Eine freie Nutzungsart trägt die neuen Kategorien an Ort und Stelle, ihr
    /// Freigabevermerk entfällt.
    ///
    /// <para><b>Provenienz je Kategorie</b> gegen den Bezug (die gespeicherte Kategorie gleichen
    /// Namens, sonst die des Vorgabesatzes): gleiche Werte behalten Provenienz und Beleg; geänderte
    /// Werte tragen Herkunftsart <c>EIGENKONSTRUKTION</c> mit neutraler Quelle und der
    /// Katalogversion der Zeile, solange der Entwurf die Provenienz des Bezugs unverändert
    /// mitbringt; eine neue Kategorie ohne Bezug ist Eigenkonstruktion. Alle geschriebenen
    /// Kategorien tragen Status <c>EIGEN</c> und <c>ReadOnly = 0</c>; die Reihenfolge ist die des
    /// Entwurfs.</para>
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        private const string SQL_KATEGORIE_INSERT =
            "INSERT INTO " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " (ID_Nutzungsart, Kategorie, Reihenfolge, " +
            "Volumenstrom_l_min, Dauer_min, Anteil, Sigma, Kappung_l_min, Quelle, Ausgabe, Version, Herkunftsart, " +
            "Status, Beleg, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 0)";

        private const string SQL_KATEGORIEN_LESEN =
            "SELECT Kategorie, Volumenstrom_l_min, Sigma, Dauer_min, Anteil, Kappung_l_min, Quelle, Ausgabe, Version, " +
            "Herkunftsart, Beleg FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE ID_Nutzungsart = ? ORDER BY Reihenfolge, ID";

        /// <summary>
        /// Die Kategorien der Nutzungsart <paramref name="idNutzungsart"/> für den Editor samt Summe,
        /// Hinweis und Sperre. Ohne Tabellen (Stand vor Schritt 115) leer mit
        /// <see cref="TwwKatalogAusgang.TabellenFehlen"/>; eine fehlende Nutzungsart
        /// <see cref="TwwKatalogAusgang.NichtGefunden"/>.
        /// </summary>
        internal static TwwKategorienStand KategorienLesen(int idNutzungsart)
        {
            if (!TabellenVorhanden() || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM))
                return new TwwKategorienStand(idNutzungsart, new Zapfkategorie[0], 0.0, null, TwwKatalogAusgang.TabellenFehlen);
            if (Lies(idNutzungsart) == null)
                return new TwwKategorienStand(idNutzungsart, new Zapfkategorie[0], 0.0, null, TwwKatalogAusgang.NichtGefunden);

            IReadOnlyList<Zapfkategorie> k = ZapfprofilCtrl.Zapfkategorien(new[] { idNutzungsart });
            TwwKatalogAusgang sperre = IstReadOnly(idNutzungsart) ? TwwKatalogAusgang.ReadOnlyGesperrt
                                     : IstBenutzt(idNutzungsart) ? TwwKatalogAusgang.BenutztGesperrt
                                     : TwwKatalogAusgang.Ausgefuehrt;
            return new TwwKategorienStand(idNutzungsart, k, Zapfkategoriensatz.SummeAnteil(k), Zapfkategoriensatz.Summenhinweis(k), sperre);
        }

        /// <summary>
        /// <b>Der Vorgabesatz</b> (N12 (p)): die frei verfügbaren Kategorien des Paketteils
        /// (<c>Referenzlaeufe/Katalogpaket_frei/</c>), so wie sie beim Einspielen an die
        /// Nutzungsarten gebunden wurden — in ihrer Reihenfolge und mit Provenienz, ohne Bindung
        /// (<c>IdNutzungsart</c> 0). Startwerte des Editors, wenn eine Nutzungsart keine Kategorien
        /// führt; leer, wenn der Katalog keinen Vorgabesatz trägt.
        ///
        /// <para><b>Je Nutzungsartengruppe ein Satz</b> (Stufe Z5): Der Paketteil führt einen Satz
        /// für Wohnnutzungen (vier Kategorien) und einen für Nichtwohnen (zwei nach dem
        /// OpenDHW-Muster). Ist <paramref name="idNutzungsart"/> genannt, zählen nur die
        /// Nutzungsarten ihrer Gruppe als Kandidaten (<see cref="TwwSchema.Kategoriengruppe"/>, die
        /// Kalenderart entscheidet) — der Editor einer Nichtwohn-Nutzungsart startet nie mit dem
        /// Wohnsatz. Ohne Angabe (0) zählen alle Gruppen wie bisher.</para>
        ///
        /// <para><b>Eindeutig, auch nach Kopien und Änderungen</b> (nicht „die freien Kategorien der
        /// kleinsten Nutzungsart", die nach einer Änderung an Ort und Stelle einen Teilsatz trifft):
        /// In Frage kommt nur eine Nutzungsart, deren Kategorien ALLE Herkunftsart <c>FREI</c> tragen
        /// (eine geänderte Kategorie ist Eigenkonstruktion). Gibt es solche mit lauter Zeilen der
        /// Auslieferung (Status <c>AUSLIEFERUNG</c> — unveränderlich, so spielt die Vorlage den
        /// Paketteil ein), zählen nur sie. Unter den Kandidaten gilt der Satz, den die meisten
        /// Nutzungsarten gleich tragen (Namen und Werte in ihrer Reihenfolge — der Paketteil hängt an
        /// jeder Nutzungsart ohne eigene Kategorien), bei Gleichstand der größere, dann der der
        /// kleinsten Nutzungsart.</para>
        /// </summary>
        internal static IReadOnlyList<Zapfkategorie> KategorienVorgabe(int idNutzungsart = 0)
        {
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM)) return new Zapfkategorie[0];
            DataTable dt = DataRepository.GetDataTable(
                "SELECT k.ID_Nutzungsart, k.Kategorie, k.Volumenstrom_l_min, k.Sigma, k.Dauer_min, k.Anteil, k.Kappung_l_min, " +
                "k.Quelle, k.Ausgabe, k.Version, k.Herkunftsart, k.Status, n.Kalenderart FROM " +
                TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " k INNER JOIN " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                " n ON n.ID = k.ID_Nutzungsart ORDER BY k.ID_Nutzungsart, k.Reihenfolge, k.ID");
            if (dt == null) return new Zapfkategorie[0];

            // Die Gruppe der gefragten Nutzungsart: nur ihre Gruppe kommt als Vorgabe in Frage.
            string gruppe = null;
            if (idNutzungsart > 0)
            {
                object ka = DataRepository.ExecuteScalar(
                    "SELECT Kalenderart FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                    new DbParam("@id", idNutzungsart));
                if (ka != null && ka != DBNull.Value)
                    gruppe = TwwSchema.Kategoriengruppe(Convert.ToInt64(ka, CultureInfo.InvariantCulture));
            }

            string frei = TwwWertemengen.Text(Herkunftsart.Frei);
            string auslieferung = TwwWertemengen.Text(ZapfKatalogstatus.Auslieferung);
            var saetze = new List<(int Id, List<Zapfkategorie> Kategorien, bool Frei, bool Ausgeliefert)>();
            foreach (DataRow r in dt.Rows)
            {
                int id = ZapfprofilCtrl.Ganz(r, "ID_Nutzungsart");
                if (gruppe != null
                    && !string.Equals(TwwSchema.Kategoriengruppe(ZapfprofilCtrl.Ganz(r, "Kalenderart")), gruppe, StringComparison.Ordinal))
                    continue;
                if (saetze.Count == 0 || saetze[saetze.Count - 1].Id != id) saetze.Add((id, new List<Zapfkategorie>(), true, true));
                var s = saetze[saetze.Count - 1];
                s.Kategorien.Add(Kategorie(r, 0));
                bool istFrei = string.Equals(ZapfprofilCtrl.Text(r, "Herkunftsart"), frei, StringComparison.Ordinal);
                bool istAusgeliefert = string.Equals(ZapfprofilCtrl.Text(r, "Status"), auslieferung, StringComparison.Ordinal);
                saetze[saetze.Count - 1] = (s.Id, s.Kategorien, s.Frei && istFrei, s.Ausgeliefert && istAusgeliefert);
            }

            var kandidaten = saetze.FindAll(s => s.Frei);
            if (kandidaten.Exists(s => s.Ausgeliefert)) kandidaten = kandidaten.FindAll(s => s.Ausgeliefert);
            List<Zapfkategorie> bester = null;
            int besteZahl = 0;
            foreach (var s in kandidaten)                                  // aufsteigend nach Nutzungsart
            {
                int zahl = kandidaten.Count(t => GleicherSatz(t.Kategorien, s.Kategorien));
                if (bester == null || zahl > besteZahl || (zahl == besteZahl && s.Kategorien.Count > bester.Count))
                {
                    bester = s.Kategorien;
                    besteZahl = zahl;
                }
            }
            return bester != null ? bester.AsReadOnly() : (IReadOnlyList<Zapfkategorie>)new Zapfkategorie[0];
        }

        /// <summary>Zwei Sätze gleich: dieselben Namen mit denselben Werten in derselben Reihenfolge.</summary>
        private static bool GleicherSatz(List<Zapfkategorie> a, List<Zapfkategorie> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!string.Equals(a[i].Name, b[i].Name, StringComparison.Ordinal) || !WerteGleich(a[i], b[i])) return false;
            return true;
        }

        /// <summary>
        /// <b>Speichert die Kategorien</b> <paramref name="kategorien"/> (Reihenfolge = Position) an
        /// der Nutzungsart <paramref name="idNutzungsart"/> — in EINEM Vorgang, nach den Regeln von
        /// <see cref="Zapfkategoriensatz.Pruefen"/> (sonst <see cref="TwwKatalogAusgang.RasterUngueltig"/>
        /// mit dem Satz). Ist die Nutzungsart gesperrt, entsteht sie per „Speichern unter" neu in der
        /// Katalogversion <paramref name="katalogversion"/> (Pflicht, sonst
        /// <see cref="TwwKatalogAusgang.EntwurfUnvollstaendig"/>; vergeben:
        /// <see cref="TwwKatalogAusgang.NameBelegt"/>). Sind Namen, Reihenfolge und Werte gleich,
        /// wird nichts geschrieben.
        /// </summary>
        internal static TwwKategorienErgebnis KategorienSpeichern(int idNutzungsart, IReadOnlyList<Zapfkategorie> kategorien,
                                                                  string katalogversion = null)
        {
            if (!TabellenVorhanden() || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM))
                return new TwwKategorienErgebnis(TwwKatalogAusgang.TabellenFehlen, idNutzungsart, null);
            ZapfSatz grund = Zapfkategoriensatz.Pruefen(kategorien);
            if (grund != null) return new TwwKategorienErgebnis(TwwKatalogAusgang.RasterUngueltig, idNutzungsart, grund);

            string neueVersion = string.IsNullOrWhiteSpace(katalogversion) ? null : katalogversion.Trim();
            IReadOnlyList<Zapfkategorie> vorgabe = KategorienVorgabe(idNutzungsart);
            int ziel = idNutzungsart;
            bool neu = false;

            TwwKatalogErgebnis erg = Ausfuehren(idNutzungsart, v =>
            {
                TwwNutzungsartEntwurf bezug = Bezugszeile(v, idNutzungsart);
                if (bezug == null) return TwwKatalogAusgang.NichtGefunden;
                List<(Zapfkategorie Kategorie, string Beleg)> alt = KategorienImVorgang(v, idNutzungsart);
                if (Unveraendert(alt, kategorien)) return TwwKatalogAusgang.Ausgefuehrt;

                bool gesperrt = Sperre(v, idNutzungsart) != TwwKatalogAusgang.Ausgefuehrt;
                if (gesperrt && neueVersion == null) return TwwKatalogAusgang.EntwurfUnvollstaendig;
                string version = gesperrt ? neueVersion : bezug.Katalogversion;

                if (gesperrt)
                {
                    if (NameVergeben(v, bezug.Bezeichner, neueVersion, null)) return TwwKatalogAusgang.NameBelegt;
                    List<DbParam> p = Fachwerte(bezug with { Katalogversion = neueVersion });
                    p.Add(new DbParam("@vorlage", idNutzungsart));
                    p.Add(new DbParam("@status", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)));
                    // Die Werte der Nutzungsart sind unverändert — ihr interner Beleg kommt mit.
                    p.Add(new DbParam("@beleg", idNutzungsart));
                    ziel = v.EinfuegenUndId(SQL_INSERT, p.ToArray());
                    neu = true;
                }
                else
                {
                    v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE ID_Nutzungsart = ?",
                                 new DbParam("@id", idNutzungsart));
                    v.Ausfuehren("UPDATE " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " SET Freigabe = NULL WHERE ID = ?",
                                 new DbParam("@id", idNutzungsart));
                }

                for (int i = 0; i < kategorien.Count; i++)
                {
                    Zapfkategorie k = kategorien[i];
                    (Provenienz herkunft, string beleg) = KategorieHerkunft(k, alt, vorgabe, version);
                    v.Ausfuehren(SQL_KATEGORIE_INSERT,
                        new DbParam("@n", ziel), new DbParam("@k", k.Name.Trim()), new DbParam("@r", i + 1),
                        new DbParam("@v", k.VolumenstromLJeMin), new DbParam("@d", k.DauerMin), new DbParam("@a", k.Anteil),
                        new DbParam("@s", k.StreuungLJeMin), new DbParam("@kap", k.KappungLJeMin.HasValue ? (object)k.KappungLJeMin.Value : null),
                        new DbParam("@q", herkunft.Quelle), new DbParam("@aus", herkunft.Ausgabe), new DbParam("@ver", herkunft.Version),
                        new DbParam("@art", TwwWertemengen.Text(herkunft.Art)),
                        new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)), new DbParam("@beleg", beleg));
                }
                return TwwKatalogAusgang.Ausgefuehrt;
            });

            return erg.Ok
                ? new TwwKategorienErgebnis(TwwKatalogAusgang.Ausgefuehrt, ziel, null) { NeueZeile = neu }
                : new TwwKategorienErgebnis(erg.Ausgang, idNutzungsart, null);
        }

        // =================================================================================

        /// <summary>Die gespeicherten Kategorien samt internem Beleg — im laufenden Vorgang gelesen.</summary>
        private static List<(Zapfkategorie Kategorie, string Beleg)> KategorienImVorgang(DbVorgang v, int idNutzungsart)
        {
            var liste = new List<(Zapfkategorie, string)>();
            DataTable dt = v.Lese(SQL_KATEGORIEN_LESEN, new DbParam("@id", idNutzungsart));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add((Kategorie(r, idNutzungsart), r["Beleg"] == DBNull.Value ? null : Convert.ToString(r["Beleg"], CultureInfo.InvariantCulture)));
            return liste;
        }

        private static Zapfkategorie Kategorie(DataRow r, int idNutzungsart)
            => new Zapfkategorie(idNutzungsart, ZapfprofilCtrl.Text(r, "Kategorie"), ZapfprofilCtrl.Zahl(r, "Volumenstrom_l_min"),
                                 ZapfprofilCtrl.Zahl(r, "Sigma"), ZapfprofilCtrl.Ganz(r, "Dauer_min"), ZapfprofilCtrl.Zahl(r, "Anteil"),
                                 ZapfprofilCtrl.Herkunft(r, ""))
            {
                KappungLJeMin = ZapfprofilCtrl.ZahlOderNull(r, "Kappung_l_min")
            };

        /// <summary>Gleiche Namen in gleicher Reihenfolge mit gleichen Werten — dann ist nichts zu schreiben.</summary>
        private static bool Unveraendert(List<(Zapfkategorie Kategorie, string Beleg)> alt, IReadOnlyList<Zapfkategorie> neu)
        {
            if (alt.Count != neu.Count) return false;
            for (int i = 0; i < alt.Count; i++)
                if (!string.Equals(alt[i].Kategorie.Name, neu[i].Name.Trim(), StringComparison.Ordinal)
                    || !WerteGleich(alt[i].Kategorie, neu[i])) return false;
            return true;
        }

        private static bool WerteGleich(Zapfkategorie a, Zapfkategorie b)
            => a.VolumenstromLJeMin.Equals(b.VolumenstromLJeMin) && a.StreuungLJeMin.Equals(b.StreuungLJeMin)
               && a.DauerMin == b.DauerMin && a.Anteil.Equals(b.Anteil) && Nullable.Equals(a.KappungLJeMin, b.KappungLJeMin);

        /// <summary>
        /// Provenienz und Beleg einer geschriebenen Kategorie gegen ihren Bezug — die gespeicherte
        /// Kategorie gleichen Namens, sonst die des Vorgabesatzes (Klassenkommentar).
        /// </summary>
        private static (Provenienz, string) KategorieHerkunft(Zapfkategorie k, List<(Zapfkategorie Kategorie, string Beleg)> alt,
                                                               IReadOnlyList<Zapfkategorie> vorgabe, string version)
        {
            string name = k.Name.Trim();
            Zapfkategorie bezug = null;
            string beleg = null;
            foreach ((Zapfkategorie a, string b) in alt)
                if (string.Equals(a.Name, name, StringComparison.Ordinal)) { bezug = a; beleg = b; break; }
            if (bezug == null && vorgabe != null)
                foreach (Zapfkategorie a in vorgabe)
                    if (string.Equals(a.Name, name, StringComparison.Ordinal)) { bezug = a; break; }

            if (bezug == null)
                return (Belegt(k.Herkunft) ? k.Herkunft with { Version = version } : Eigenkonstruktion(version), null);
            bool unberuehrt = k.Herkunft == null
                              || (k.Herkunft.Art == bezug.Herkunft.Art
                                  && string.Equals(k.Herkunft.Quelle, bezug.Herkunft.Quelle, StringComparison.Ordinal)
                                  && string.Equals(k.Herkunft.Ausgabe, bezug.Herkunft.Ausgabe, StringComparison.Ordinal));
            if (WerteGleich(bezug, k)) return (unberuehrt ? bezug.Herkunft : k.Herkunft, unberuehrt ? beleg : null);
            return (unberuehrt ? Eigenkonstruktion(version) : k.Herkunft with { Version = version }, null);
        }
    }
}
