using System;
using System.Collections.Generic;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Zwischenstand eines laufenden Katalogimports.</summary>
    public readonly struct ImportFortschritt
    {
        public ImportFortschritt(double? anteil, string schluessel, params string[] werte)
        {
            Anteil = anteil;
            Schluessel = schluessel ?? "";
            Werte = werte ?? Array.Empty<string>();
        }

        /// <summary>0…1, oder <c>null</c> fuer „unbestimmt" (der Balken laeuft).</summary>
        public double? Anteil { get; }

        /// <summary>Sprachneutraler Schluessel des Begleittextes.</summary>
        public string Schluessel { get; }

        /// <summary>Platzhalterwerte, bereits invariant formatiert.</summary>
        public string[] Werte { get; }
    }

    /// <summary>
    /// Die fuenf Zaehler eines Uebernahmelaufs — die Zahlen der Sammelmeldung.
    /// </summary>
    public sealed class ImportBilanz
    {
        /// <summary>Zahl der markierten Eintraege (das „von m").</summary>
        public int Markiert;

        /// <summary>Neu in die STAMM-Tabelle geschrieben.</summary>
        public int Gespeichert;

        /// <summary>Bereits vorhanden bzw. bewusst ausgelassen.</summary>
        public int Duplikat;

        /// <summary>Fehlgeschlagen.</summary>
        public int Fehler;

        /// <summary>Vorhandener Katalogsatz aktualisiert.</summary>
        public int Ueberschrieben;

        /// <summary>Unter neuem Namen angelegt.</summary>
        public int Umbenannt;

        /// <summary>Hat der Lauf ueberhaupt etwas geschrieben?</summary>
        public bool EtwasGeschrieben => Gespeichert > 0 || Ueberschrieben > 0 || Umbenannt > 0;
    }

    /// <summary>
    /// <b>Der Katalogimport als EIN Kern-Ablauf</b> (iU9-W13.0b) — Lesen, Filtern,
    /// Vorpruefen, Ausfuehren.
    ///
    /// <para><b>Warum es ihn gibt.</b> Diese vier Schritte standen viermal wortgleich
    /// im Bestand: <c>btn_VDI3805_Click</c>, <c>FuelleListe</c>, der Vorpruefblock
    /// von <c>btn_Uebernehmen_Click</c> und <c>FuehreAus</c> — je Maske rund
    /// 200 Zeilen, in denen sich nur Feldnamen und der Katalogschluessel
    /// unterscheiden (Befund W13-B3). Hier steht der Ablauf einmal; was ihn
    /// unterscheidet, steht als Daten in <see cref="KatalogImportProfil"/>.</para>
    ///
    /// <para><b>Er zeigt nichts an</b> — dieselbe Regel wie
    /// <see cref="GanglinienImportAblauf"/> aus Welle 12. Der Konfliktdialog ist
    /// kein Rueckruf, sondern eine Zaesur: Der Wirt ruft
    /// <see cref="Vorpruefen"/>, zeigt bei Bedarf seine Ueberlagerung und ruft dann
    /// <see cref="Ausfuehren"/> mit den Entscheidungen. So bleibt der Ablauf ohne
    /// Faden- und Fensterwissen.</para>
    ///
    /// <para><b>Das Lesen laeuft im Aufrufer-Faden</b>, gehoert dort aber in ein
    /// <c>Task.Run</c>: Die groesste Probendatei des Bestands hat 92 376 Zeilen und
    /// 8,3 MB, und in einer WebView ist der Renderfaden der Bedienfaden (Risiko
    /// R-W13-2). Deshalb nehmen <see cref="Lesen"/> und <see cref="Ausfuehren"/>
    /// einen Melder und ein Abbruchzeichen entgegen.</para>
    /// </summary>
    public sealed class KatalogImportAblauf
    {
        private readonly List<KatalogImportSatz> _saetze = new List<KatalogImportSatz>();
        private readonly List<PruefMeldung> _meldungen = new List<PruefMeldung>();

        public KatalogImportAblauf(KatalogImportProfil profil)
        {
            Profil = profil ?? throw new ArgumentNullException(nameof(profil));
        }

        /// <summary>Die Auspraegung dieses Ablaufs.</summary>
        public KatalogImportProfil Profil { get; }

        /// <summary>Die gelesenen Saetze in Dateireihenfolge.</summary>
        public IReadOnlyList<KatalogImportSatz> Saetze => _saetze;

        /// <summary>Was beim Lesen aufgefallen ist.</summary>
        public IReadOnlyList<PruefMeldung> Meldungen => _meldungen;

        // ==================================================================
        // 1 — Lesen
        // ==================================================================

        /// <summary>
        /// Liest eine VDI-3805-Datei mit dem Parser der Auspraegung. Liefert die Zahl
        /// der gelesenen Saetze; ein Lesefehler ergibt 0 und eine Meldung.
        /// </summary>
        public int Lesen(string pfad, IProgress<ImportFortschritt> melder = null,
                         CancellationToken abbruch = default)
        {
            _saetze.Clear();
            _meldungen.Clear();

            if (string.IsNullOrWhiteSpace(pfad))
                return 0;

            melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_LESEN"));

            try
            {
                abbruch.ThrowIfCancellationRequested();

                switch (Profil.Art)
                {
                    case KatalogImportArt.Heizkessel:
                        {
                            var p = new HeizkesselImport();
                            p.Import(pfad);
                            // Der Brennstoffdeckel EINMAL je Datei statt einmal je
                            // Kandidat (Befund W13-B17): bei 177 markierten Saetzen
                            // waren das 354 Abfragen derselben Zahl.
                            int deckel = HeizkesselImportSatz.MaxBrennstoff();
                            foreach (var a in p._list)
                                _saetze.Add(new HeizkesselImportSatz(a) { Deckel = deckel });
                            break;
                        }

                    case KatalogImportArt.Pufferspeicher:
                        {
                            var p = new PufferSpImport();
                            p.Import(pfad);
                            foreach (var a in p._list) _saetze.Add(new PufferSpImportSatz(a));
                            break;
                        }

                    case KatalogImportArt.Solarkollektoren:
                        {
                            var p = new Solarkollektorenlmport();
                            p.Import(pfad);
                            foreach (var a in p._list) _saetze.Add(new SolarkollektorImportSatz(a));
                            break;
                        }

                    case KatalogImportArt.Waermepumpe:
                        {
                            var p = new WaermepumpenImport();
                            p.Import(pfad);
                            for (int i = 0; i < p._list.Count; i++)
                                _saetze.Add(new WaermepumpeImportSatz(p, i));
                            _meldungen.AddRange(p.Meldungen);
                            break;
                        }
                }
            }
            catch (OperationCanceledException)
            {
                _saetze.Clear();
                throw;
            }
            catch (Exception ex)
            {
                _saetze.Clear();
                _meldungen.Add(new PruefMeldung(PruefStufe.Fehler, "IMP_KAT_PROT_LESEFEHLER", ex.Message));
            }

            melder?.Report(new ImportFortschritt(1.0, "IMP_KAT_PROT_GELESEN",
                _saetze.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));

            return _saetze.Count;
        }

        // ==================================================================
        // 2 — Filtern
        // ==================================================================

        /// <summary>
        /// Die Zuordnung Anzeigezeile → Satzindex unter dem Zahlen- und dem
        /// Suchfilter — der Rumpf von <c>FuelleListe</c> ohne Steuerelemente.
        ///
        /// <para>Die Reihenfolge ist die von Heizkessel, Pufferspeicher und
        /// Waermepumpe: erst der Zahlenfilter, dann der Suchtext. Solar prueft im
        /// Bestand umgekehrt (Befund, § 7.2 „Zufall") — am Ergebnis aendert das
        /// nichts, beide Bedingungen sind unabhaengig und beide muessen gelten.</para>
        /// </summary>
        public List<int> Anzeigeindex(double von, double bis, string suchtext)
        {
            var treffer = new List<int>();
            for (int i = 0; i < _saetze.Count; i++)
            {
                double wert = _saetze[i].Filterwert;
                if (wert < von) continue;
                if (wert > bis) continue;
                if (!VdiAuswahlFilter.Passt(suchtext, _saetze[i].Name, _saetze[i].Firma)) continue;
                treffer.Add(i);
            }
            return treffer;
        }

        // ==================================================================
        // 3 — Vorpruefen
        // ==================================================================

        /// <summary>
        /// Prueft die markierten Saetze gegen den Katalog UND gegen sich selbst
        /// (Dublettenkonzept 4.1) — noch ohne Schreibzugriff.
        ///
        /// <para><paramref name="bezeichnerZu"/> liefert den Bezeichner, den die
        /// Maske ZEIGT: In allen vier Auspraegungen darf der Anwender ihn aendern,
        /// und geprueft werden muss, was gespeichert wuerde. Ohne den Delegaten
        /// zaehlt der Name aus der Datei.</para>
        ///
        /// <para><c>ImportKandidat.Tag</c> traegt den Satzindex — genauso wie im
        /// Bestand, damit <see cref="Ausfuehren"/> den Satz wiederfindet.</para>
        /// </summary>
        public List<ImportPruefung> Vorpruefen(IReadOnlyList<int> markiert,
                                               Func<int, string> bezeichnerZu = null)
        {
            var kandidaten = new List<ImportKandidat>();
            if (markiert == null) return new List<ImportPruefung>();

            foreach (int i in markiert)
            {
                if (i < 0 || i >= _saetze.Count) continue;
                string name = Bezeichner(i, bezeichnerZu);

                var kand = new ImportKandidat { Name = name, Tag = i };
                foreach (var paar in _saetze[i].Vergleichswerte(name))
                    kand.Werte[paar.Key] = paar.Value;
                kandidaten.Add(kand);
            }

            return DublettenPruefung.PruefeKandidaten(Profil.Katalog, kandidaten);
        }

        /// <summary>
        /// Braucht die Auswahl den Konfliktdialog? Genau dann, wenn ein Kandidat
        /// nicht neu ist oder sein Name in der Auswahl zweimal vorkommt.
        /// </summary>
        public static bool Konfliktbehaftet(IEnumerable<ImportPruefung> pruefungen)
        {
            if (pruefungen == null) return false;
            foreach (ImportPruefung p in pruefungen)
                if (p.Befund != ImportBefund.Neu || p.NameDoppeltInAuswahl) return true;
            return false;
        }

        /// <summary>
        /// Die Entscheidungsliste eines konfliktfreien Laufs: alles importieren.
        /// Der Bestand baute sie in jeder der drei Masken selbst.
        /// </summary>
        public static List<KonfliktEntscheidung> AllesImportieren(IEnumerable<ImportPruefung> pruefungen)
        {
            var liste = new List<KonfliktEntscheidung>();
            if (pruefungen == null) return liste;
            foreach (ImportPruefung p in pruefungen)
                liste.Add(new KonfliktEntscheidung { Pruefung = p, Aktion = KonfliktAktion.Importieren });
            return liste;
        }

        // ==================================================================
        // 4 — Ausfuehren
        // ==================================================================

        /// <summary>
        /// Fuehrt die Entscheidungen aus und liefert die fuenf Zaehler.
        ///
        /// <para>Der Aktions-Schalter ist der des Bestands: <c>Auslassen</c> zaehlt
        /// als Duplikat, <c>Ueberschreiben</c> aktualisiert den Bestandssatz,
        /// <c>Umbenennen</c> legt unter dem neuen Namen an und zaehlt als
        /// <c>Umbenannt</c>, alles Uebrige legt an.</para>
        ///
        /// <para><b>Ein fehlerhafter Eintrag bricht den Lauf nicht ab</b> — er zaehlt
        /// als Fehler und die Schleife geht weiter (Kommentar
        /// <c>Form_Heizkessel_einlesen:298</c>). Nur ein Abbruch durch den Anwender
        /// beendet sie.</para>
        /// </summary>
        public ImportBilanz Ausfuehren(int markiertAnzahl,
                                       IList<KonfliktEntscheidung> entscheidungen,
                                       Func<int, string> bezeichnerZu = null,
                                       IProgress<ImportFortschritt> melder = null,
                                       CancellationToken abbruch = default)
        {
            var bilanz = new ImportBilanz { Markiert = markiertAnzahl };
            if (entscheidungen == null) return bilanz;

            for (int n = 0; n < entscheidungen.Count; n++)
            {
                abbruch.ThrowIfCancellationRequested();

                KonfliktEntscheidung ent = entscheidungen[n];
                int i = (ent.Pruefung != null && ent.Pruefung.Kandidat != null
                         && ent.Pruefung.Kandidat.Tag is int) ? (int)ent.Pruefung.Kandidat.Tag : -1;

                melder?.Report(new ImportFortschritt(
                    entscheidungen.Count > 0 ? (double)n / entscheidungen.Count : (double?)null,
                    "IMP_KAT_PROT_SCHREIBEN",
                    (n + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    entscheidungen.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));

                VdiUebernahmeErgebnis ergebnis;
                try
                {
                    if (i < 0 || i >= _saetze.Count)
                    {
                        ergebnis = VdiUebernahmeErgebnis.Fehler;
                    }
                    else
                    {
                        switch (ent.Aktion)
                        {
                            case KonfliktAktion.Auslassen:
                                ergebnis = VdiUebernahmeErgebnis.Duplikat;
                                break;
                            case KonfliktAktion.Ueberschreiben:
                                ergebnis = _saetze[i].Ueberschreiben(ent.Pruefung.Vorhanden.Id);
                                break;
                            case KonfliktAktion.Umbenennen:
                                ergebnis = _saetze[i].Anlegen(ent.NeuerName);
                                if (ergebnis == VdiUebernahmeErgebnis.Gespeichert)
                                    ergebnis = VdiUebernahmeErgebnis.Umbenannt;
                                break;
                            default:
                                ergebnis = _saetze[i].Anlegen(Bezeichner(i, bezeichnerZu));
                                break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Ein fehlerhafter Eintrag darf den Gesamtvorgang nicht abbrechen.
                    Console.WriteLine("Fehler beim Einlesen von '"
                        + (i >= 0 && i < _saetze.Count ? _saetze[i].Name : "?") + "': " + ex.Message);
                    ergebnis = VdiUebernahmeErgebnis.Fehler;
                }

                if (ergebnis == VdiUebernahmeErgebnis.Gespeichert) bilanz.Gespeichert++;
                else if (ergebnis == VdiUebernahmeErgebnis.Duplikat) bilanz.Duplikat++;
                else if (ergebnis == VdiUebernahmeErgebnis.Ueberschrieben) bilanz.Ueberschrieben++;
                else if (ergebnis == VdiUebernahmeErgebnis.Umbenannt) bilanz.Umbenannt++;
                else bilanz.Fehler++;
            }

            melder?.Report(new ImportFortschritt(1.0, "IMP_KAT_PROT_FERTIG"));
            return bilanz;
        }

        private string Bezeichner(int i, Func<int, string> bezeichnerZu)
        {
            if (bezeichnerZu == null) return _saetze[i].Name;
            string name = bezeichnerZu(i);
            return string.IsNullOrEmpty(name) ? _saetze[i].Name : name;
        }
    }
}
