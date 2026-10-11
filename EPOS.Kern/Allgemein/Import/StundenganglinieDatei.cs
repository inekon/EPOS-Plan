using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Jahresreihe in Stundenwerten, aus einer Text-, CSV- oder Excel-Datei</b> —
    /// das Ergebnis von <see cref="StundenganglinieDatei.Lies"/>.
    ///
    /// <para><b>Leistung in kW.</b> Die Reihe trägt je Stunde die mittlere Leistung;
    /// eine Viertelstundenreihe wird deshalb je Stunde GEMITTELT, nicht summiert.</para>
    /// </summary>
    public sealed class StundenganglinieLesung
    {
        /// <summary>Steht die Reihe mit genau 8 760 Stundenwerten?</summary>
        public bool Erfolgreich;

        /// <summary>Die 8 760 Stundenwerte [kW]; leer bei einem Fehlschlag.</summary>
        public double[] StundenwerteKw = Array.Empty<double>();

        /// <summary>
        /// Die Werte im Raster der DATEI [kW] — 8 760 Stunden- oder 35 040 Viertelstundenwerte, ungemittelt;
        /// leer bei einem Fehlschlag. Wer das Viertelstundenraster behalten will (PV-Ganglinie), nimmt diese
        /// Reihe statt <see cref="StundenwerteKw"/>.
        /// </summary>
        public double[] WerteImDateirasterKw = Array.Empty<double>();

        /// <summary>Wie viele Werte die Datei trug (8 760 oder 35 040 bei Erfolg).</summary>
        public int AnzahlWerte;

        /// <summary>Das Raster der DATEI: Stunde oder Viertelstunde.</summary>
        public GanglinienRaster Raster = GanglinienRaster.Unbekannt;

        /// <summary>Das erkannte Format (Trennzeichen, Dezimaltrenner, Kopfzeile, Spalten).</summary>
        public GanglinienImportOptionen Format = new GanglinienImportOptionen();

        /// <summary>
        /// Der Text der Kopfzeile — bei einer Spalte die ganze erste Zeile, sonst der
        /// Kopf der Wertspalte; leer ohne Kopfzeile.
        /// </summary>
        public string Kopftext = "";

        /// <summary>Σ der Stundenleistungen [kW] × 1 h ÷ 1 000.</summary>
        public double JahresarbeitMwh;

        /// <summary>Der Höchstwert der STUNDENreihe [kW].</summary>
        public double SpitzeKw;

        /// <summary>Was Erkennung und Lesen melden; bei einem Fehlschlag steht der Grund hier.</summary>
        public List<PruefMeldung> Meldungen = new List<PruefMeldung>();

        /// <summary>Die erste Fehlermeldung oder <c>null</c>.</summary>
        public PruefMeldung ErsterFehler
        {
            get
            {
                foreach (PruefMeldung m in Meldungen)
                    if (m.Stufe == PruefStufe.Fehler) return m;
                return null;
            }
        }
    }

    /// <summary>
    /// <b>Liest eine Jahresreihe mit Formaterkennung und bringt sie auf 8 760 Stunden.</b>
    /// Die Datei-Schicht ist <see cref="GanglinienDatei"/> — Trennzeichen (<c>;</c>, Tab,
    /// <c>|</c>, <c>,</c>), Dezimalzeichen, Kopfzeile und Spaltenwahl erkennt sie; hier
    /// stehen nur die Regeln der STUNDENREIHE:
    /// <list type="bullet">
    ///   <item><description>8 760 Werte sind Stunden, 35 040 Werte Viertelstunden — sie werden
    ///   je Stunde gemittelt (<c>SimulationControl.Viertelstunden_zu_Stundenwerte_Mittelwert</c>,
    ///   derselbe Weg wie die Kennzahlen der Katalogliste); jede andere Zahl ist ein Fehler.</description></item>
    ///   <item><description>Eine Zeitstempelspalte darf da sein, muss aber nicht; gezählt wird
    ///   die Wertspalte. Eine laufende Nummer vor der Wertspalte („Nr;Leistung") wird
    ///   übersprungen.</description></item>
    ///   <item><description>Die einspaltige Textdatei — erste Zeile Beschreibung, danach ein
    ///   Wert je Zeile — ist der Sonderfall „kein Trennzeichen, Kopfzeile".</description></item>
    /// </list>
    ///
    /// <para><b>Eine Spalte mit Dezimalkomma</b> sieht für die Trennzeichenerkennung wie
    /// zwei Spalten mit Komma aus (<c>12,5</c>). Stehen höchstens zwei Felder je Zeile,
    /// gibt es keine Zeitspalte und ist das zweite Feld stets eine reine Ziffernfolge,
    /// liest der Leser die Datei als EINE Spalte mit Dezimalkomma — eine echte
    /// Zweispaltendatei mit Komma trägt im zweiten Feld einen Dezimalpunkt.</para>
    ///
    /// <para>Plattformfrei, ohne Datenbank: Die Reihe gehört danach dem Aufrufer.</para>
    /// </summary>
    public static class StundenganglinieDatei
    {
        /// <summary>Stunden eines Jahres (kein Schaltjahr).</summary>
        public const int STUNDEN = 8760;

        /// <summary>Viertelstunden eines Jahres.</summary>
        public const int VIERTELSTUNDEN = STUNDEN * 4;

        /// <summary>Die Wertzahl passt weder auf Stunden noch auf Viertelstunden. {0} = Anzahl.</summary>
        public const string SchluesselAnzahl = "SGL_IMP_ANZAHL";

        /// <summary>
        /// Liest die Datei und liefert die Stundenreihe samt erkanntem Format und
        /// Kennzahlen; nie <c>null</c>, wirft nicht.
        /// </summary>
        /// <param name="pfad">Die Quelldatei.</param>
        public static StundenganglinieLesung Lies(string pfad)
        {
            var erg = new StundenganglinieLesung();

            GanglinienVorschau vorschau = GanglinienDatei.Erkenne(pfad);
            erg.Meldungen.AddRange(vorschau.Meldungen);
            if (!vorschau.Lesbar) return erg;

            GanglinienImportOptionen format = Vorbelegung(vorschau);
            erg.Format = format;
            erg.Kopftext = Kopftext(vorschau, format);

            GanglinienRohdaten roh = GanglinienDatei.Lies(pfad, format);
            erg.Meldungen.AddRange(roh.Meldungen);
            if (!roh.Erfolgreich) return erg;
            return Auswerten(erg, roh.Werte);
        }

        /// <summary>
        /// <b>Die Formaterkennung des Katalogimports</b> für den Optionendialog: dieselbe Erkennung wie
        /// <see cref="GanglinienDatei.Erkenne"/>, die Vorbelegung aber so, wie <see cref="Lies(string)"/>
        /// die Datei ohne Dialog liest (eine Spalte mit Dezimalkomma statt „Komma, zwei Spalten“, die
        /// laufende Nummer übersprungen). Nie <c>null</c>, wirft nicht.
        /// </summary>
        /// <param name="pfad">Die Quelldatei.</param>
        public static GanglinienVorschau Erkenne(string pfad)
        {
            GanglinienVorschau vorschau = GanglinienDatei.Erkenne(pfad);
            if (vorschau.Lesbar) vorschau.Vorschlag = Vorbelegung(vorschau);
            return vorschau;
        }

        /// <summary>
        /// <b>Die Lesung aus einer schon geprüften Reihe</b> (Optionendialog und
        /// <c>GanglinienImportAblauf.OhneAblage</c>): 8 760 oder 35 040 Werte in kW, dazu Format und
        /// Kopftext; Raster, Stundenreihe, Jahresarbeit und Spitze wie bei <see cref="Lies(string)"/>.
        /// Nie <c>null</c>, wirft nicht.
        /// </summary>
        /// <param name="werteKw">Die geprüfte Reihe im Raster der Datei [kW].</param>
        /// <param name="format">Die bestätigten Leseoptionen; <c>null</c> = Vorgabe.</param>
        /// <param name="kopftext">Der Text der Kopfzeile (<see cref="Kopftext(string, GanglinienImportOptionen)"/>).</param>
        public static StundenganglinieLesung AusWerten(double[] werteKw, GanglinienImportOptionen format, string kopftext)
        {
            var erg = new StundenganglinieLesung
            {
                Format = format ?? new GanglinienImportOptionen(),
                Kopftext = kopftext ?? ""
            };
            return Auswerten(erg, werteKw ?? Array.Empty<double>());
        }

        /// <summary>
        /// <b>Die Lesung aus dem Ergebnis der Importkette</b> (<c>GanglinienImportAblauf.OhneAblage</c>):
        /// die geprüften Werte samt den bestätigten Optionen und dem Kopftext der Datei unter diesen
        /// Optionen. Ohne erfolgreiches Ergebnis eine Lesung ohne Erfolg. Wirft nicht.
        /// </summary>
        /// <param name="pfad">Die Quelldatei.</param>
        /// <param name="gelesen">Das Ergebnis der Kette.</param>
        public static StundenganglinieLesung AusImport(string pfad, GanglinienImportErgebnis gelesen)
        {
            if (gelesen == null || !gelesen.Erfolgreich) return new StundenganglinieLesung();
            return AusWerten(gelesen.Werte, gelesen.Optionen, Kopftext(pfad, gelesen.Optionen));
        }

        /// <summary>
        /// Der Text der Kopfzeile einer Datei unter den gewählten Optionen ("" ohne Kopfzeile oder ohne
        /// lesbare Datei). Wirft nicht.
        /// </summary>
        /// <param name="pfad">Die Quelldatei.</param>
        /// <param name="format">Die bestätigten Leseoptionen.</param>
        public static string Kopftext(string pfad, GanglinienImportOptionen format)
        {
            if (format == null || !format.Kopfzeile) return "";
            try
            {
                GanglinienVorschau erkannt = GanglinienDatei.Erkenne(pfad);
                if (!erkannt.Lesbar) return "";
                // Die Zeilen unter dem gewählten Trennzeichen; das Zusammensetzen einer am Komma zerlegten
                // Kopfzeile richtet sich nach der ursprünglichen Erkennung.
                GanglinienVorschau gewaehlt = GanglinienDatei.Vorschau(pfad, format.Kopie());
                if (!gewaehlt.Lesbar) return "";
                gewaehlt.Vorschlag = erkannt.Vorschlag;
                return Kopftext(gewaehlt, format);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>Die Vorbelegung, mit der <see cref="Lies(string)"/> eine erkannte Datei liest.</summary>
        private static GanglinienImportOptionen Vorbelegung(GanglinienVorschau vorschau)
        {
            GanglinienImportOptionen format = vorschau.Vorschlag.Kopie();
            if (IstEinspaltigMitDezimalkomma(vorschau))
            {
                format.Trennzeichen = '\0';
                format.Dezimaltrenner = ',';
                format.WertSpalte = 0;
                format.ZeitSpalte = -1;
            }
            else
            {
                // Den Dezimaltrenner bestimmt GanglinienDatei.Erkenne schon aus den Datenzeilen allein.
                ZaehlerspalteUeberspringen(vorschau, format);
            }
            return format;
        }

        /// <summary>Raster, Stundenreihe, Jahresarbeit und Spitze zu einer gelesenen Reihe.</summary>
        private static StundenganglinieLesung Auswerten(StundenganglinieLesung erg, double[] werte)
        {
            erg.AnzahlWerte = werte.Length;
            erg.WerteImDateirasterKw = werte;
            if (werte.Length == STUNDEN)
            {
                erg.Raster = GanglinienRaster.Stunde;
                erg.StundenwerteKw = (double[])werte.Clone();
            }
            else if (werte.Length == VIERTELSTUNDEN)
            {
                erg.Raster = GanglinienRaster.Viertelstunde;
                erg.StundenwerteKw = new SimulationControl().Viertelstunden_zu_Stundenwerte_Mittelwert(werte);
            }
            else
            {
                erg.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselAnzahl,
                    werte.Length.ToString(CultureInfo.InvariantCulture)));
                return erg;
            }

            double summe = 0, spitze = 0;
            foreach (double w in erg.StundenwerteKw)
            {
                summe += w;
                if (w > spitze) spitze = w;
            }
            erg.JahresarbeitMwh = summe / 1000.0;
            erg.SpitzeKw = spitze;
            erg.Erfolgreich = true;
            return erg;
        }

        /// <summary>
        /// <b>Der benannte Grund einer gescheiterten Lesung</b> für Banner und Protokoll:
        /// „Datei „x.csv“: Zeile 12: „abc“ ist keine Zahl.“ — Dateiname und erster Fehler
        /// (mit Zeilennummer, wo es eine gibt). Ohne Fehlermeldung der Grund „keine
        /// auswertbare Zeile“; nie leer.
        /// </summary>
        /// <param name="pfad">Die Quelldatei.</param>
        /// <param name="lesung">Die gescheiterte Lesung.</param>
        public static string Ablehnungstext(string pfad, StundenganglinieLesung lesung)
        {
            string grund = GanglinienProtokollText.Text(lesung?.ErsterFehler);
            if (string.IsNullOrWhiteSpace(grund))
                grund = GanglinienProtokollText.Text(new PruefMeldung(PruefStufe.Fehler, GanglinienDatei.SchluesselDateiLeer));
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMP_MSG_DATEI_GRUND,
                                 Path.GetFileName(pfad ?? ""), grund);
        }

        /// <summary>
        /// Ist die als „Komma, zwei Spalten" erkannte Datei in Wahrheit EINE Spalte mit
        /// Dezimalkomma? Nur ohne Zeitspalte, mit höchstens zwei Feldern je Zeile, und
        /// das zweite Feld ist überall, wo es steht, eine reine Ziffernfolge.
        /// </summary>
        private static bool IstEinspaltigMitDezimalkomma(GanglinienVorschau v)
        {
            GanglinienImportOptionen o = v.Vorschlag;
            if (v.IstExcel || o.Trennzeichen != ',' || o.ZeitSpalte >= 0 || v.Spaltenzahl != 2) return false;

            bool zweiFelder = false;
            for (int z = o.Kopfzeile ? 1 : 0; z < v.Zeilen.Count; z++)
            {
                string[] f = v.Zeilen[z];
                if (f.Length > 2) return false;
                if (f.Length < 2 || f[1].Length == 0) continue;
                if (!IstGanzzahl(f[0], true) || !IstGanzzahl(f[1], false)) return false;
                zweiFelder = true;
            }
            return zweiFelder;
        }

        /// <summary>
        /// Eine laufende Nummer ist keine Wertspalte: Steht in der gewählten Wertspalte eine
        /// lückenlos um 1 steigende Ganzzahlfolge und gibt es rechts davon eine weitere
        /// Zahlenspalte, wird diese die Wertspalte (CSV mit „Nr;Leistung").
        /// </summary>
        private static void ZaehlerspalteUeberspringen(GanglinienVorschau v, GanglinienImportOptionen o)
        {
            int von = o.Kopfzeile ? 1 : 0;
            if (v.Zeilen.Count - von < 2) return;
            if (!IstZaehler(v, o.WertSpalte, von, o.Dezimaltrenner)) return;

            for (int s = o.WertSpalte + 1; s < v.Spaltenzahl; s++)
            {
                if (s == o.ZeitSpalte) continue;
                if (IstZahlenspalte(v, s, von, o.Dezimaltrenner)) { o.WertSpalte = s; return; }
            }
        }

        private static bool IstZaehler(GanglinienVorschau v, int spalte, int von, char dezimal)
        {
            double vorher = double.NaN;
            for (int z = von; z < v.Zeilen.Count; z++)
            {
                string[] f = v.Zeilen[z];
                if (spalte >= f.Length || !IstGanzzahl(f[spalte], false)) return false;
                if (!GanglinienDatei.VersucheZahl(f[spalte], dezimal, out double w)) return false;
                if (!double.IsNaN(vorher) && w != vorher + 1) return false;
                vorher = w;
            }
            return true;
        }

        private static bool IstZahlenspalte(GanglinienVorschau v, int spalte, int von, char dezimal)
        {
            for (int z = von; z < v.Zeilen.Count; z++)
            {
                string[] f = v.Zeilen[z];
                if (spalte >= f.Length || !GanglinienDatei.VersucheZahl(f[spalte], dezimal, out _)) return false;
            }
            return true;
        }

        private static bool IstGanzzahl(string text, bool vorzeichen)
        {
            if (string.IsNullOrEmpty(text)) return false;
            int i = vorzeichen && (text[0] == '-' || text[0] == '+') ? 1 : 0;
            if (i >= text.Length) return false;
            for (; i < text.Length; i++)
                if (!char.IsDigit(text[i])) return false;
            return true;
        }

        /// <summary>
        /// Der Text der Kopfzeile: eine Spalte → die ganze erste Zeile (zerlegt die
        /// Erkennung sie am Komma, wird sie wieder zusammengesetzt), sonst der Kopf der
        /// Wertspalte.
        /// </summary>
        private static string Kopftext(GanglinienVorschau v, GanglinienImportOptionen o)
        {
            if (!o.Kopfzeile || v.Zeilen.Count == 0) return "";
            string[] erste = v.Zeilen[0];
            if (o.Trennzeichen == '\0')
                return string.Join(v.Vorschlag.Trennzeichen == ',' ? "," : "", erste).Trim();
            return o.WertSpalte < erste.Length ? (erste[o.WertSpalte] ?? "").Trim() : "";
        }
    }
}
