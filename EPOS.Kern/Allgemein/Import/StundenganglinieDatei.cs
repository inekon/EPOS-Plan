using System;
using System.Collections.Generic;
using System.Globalization;
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
                ZaehlerspalteUeberspringen(vorschau, format);
            }
            erg.Format = format;
            erg.Kopftext = Kopftext(vorschau, format);

            GanglinienRohdaten roh = GanglinienDatei.Lies(pfad, format);
            erg.Meldungen.AddRange(roh.Meldungen);
            if (!roh.Erfolgreich) return erg;

            erg.AnzahlWerte = roh.Werte.Length;
            if (roh.Werte.Length == STUNDEN)
            {
                erg.Raster = GanglinienRaster.Stunde;
                erg.StundenwerteKw = roh.Werte;
            }
            else if (roh.Werte.Length == VIERTELSTUNDEN)
            {
                erg.Raster = GanglinienRaster.Viertelstunde;
                erg.StundenwerteKw = new SimulationControl().Viertelstunden_zu_Stundenwerte_Mittelwert(roh.Werte);
            }
            else
            {
                erg.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselAnzahl,
                    roh.Werte.Length.ToString(CultureInfo.InvariantCulture)));
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
