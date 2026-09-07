using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Zerleger der CEC Energy Storage System List</b> (California Energy
    /// Commission) — Quelle 4 des <c>Konzept_Stromspeicherimport_EPOS-Plan.md</c>
    /// und die einzige der vier geprüften Quellen, die ein GERÄTEVERZEICHNIS
    /// kommerzieller Speicher ist (Anwenderwunsch <b>W13‑E‑2</b>, 07.09.2026).
    ///
    /// <para><b>Gemessen am 07.09.2026:</b> 6 654 Datenzeilen von 130 Herstellern,
    /// Stand der Liste „August 21, 2026". Jede Zeile führt Kapazität [kWh] und
    /// Nennleistung [kW]; einen Round-Trip-Wirkungsgrad führen 1 103 Zeilen
    /// (16,6 %), die übrigen tragen dort „No Information Submitted".</para>
    ///
    /// <para><b>Die Quelle liefert XLSX, nicht CSV.</b> Anders als bei den
    /// CEC-Modul- und -Wechselrichterlisten (die über NREL/SAM als CSV
    /// vorliegen) gibt es für die Speicherliste nur den Knopf „Download Excel
    /// file" (<c>/Home/DownloadtoExcel?filename=EnergyStorage</c>, Dateiname
    /// <c>Energy_Storage_System_List_Data_ADA.xlsx</c>). Dieser Zerleger liest
    /// deshalb BEIDES: die Mappe direkt über ClosedXML — dieselbe Bibliothek, mit
    /// der <see cref="GanglinienDatei"/> Excel liest — und eine daraus
    /// ausgeleitete CSV-Datei. Ein Zwischenschritt in Excel bleibt dem Anwender
    /// damit erspart, ist aber möglich.</para>
    ///
    /// <para><b>Die Kopfzeile steht nicht an Zeile 1.</b> Die Mappe beginnt mit
    /// 15 Erläuterungszeilen; erst dann kommt die Beschriftung, und darunter eine
    /// ZWEITE Kopfzeile mit den Einheiten („(kWh)", „(kW)"). Der Zerleger sucht
    /// die Kopfzeile deshalb an ihren Pflichtspalten, statt eine Zeilennummer
    /// festzuschreiben — eine neue Fußnote der CEC verschöbe sie sonst.</para>
    /// </summary>
    public sealed class CecSpeicherImport
    {
        /// <summary>Ohne diese vier Spalten ist die Datei keine Speicherliste.</summary>
        internal static readonly string[] PFLICHTSPALTEN =
        {
            "manufacturer name", "model number",
            "nameplate energy capacity", "nameplate power"
        };

        /// <summary>So viele Zeilen weit wird die Kopfzeile gesucht (die Liste braucht 16).</summary>
        private const int KOPF_SUCHTIEFE = 60;

        private readonly List<StromspeicherImportSatz> _saetze = new List<StromspeicherImportSatz>();

        /// <summary>Die gelesenen Geräte in Dateireihenfolge.</summary>
        public IReadOnlyList<StromspeicherImportSatz> Saetze => _saetze;

        /// <summary>Die Zeile, in der die Kopfzeile gefunden wurde (1-basiert, 0 = keine).</summary>
        public int Kopfzeile { get; private set; }

        /// <summary>Der Stand der Liste, wie ihn die Mappe in Zeile 2 ausweist; leer wenn keiner.</summary>
        public string Stand { get; private set; } = "";

        /// <summary>
        /// Liest eine CEC-Speicherliste — <c>.xlsx</c> über ClosedXML, alles andere
        /// als Textdatei mit Trennzeichenerkennung.
        /// </summary>
        public (bool Erfolg, SpeicherImportMeldung Meldung) AusDatei(string pfad)
        {
            _saetze.Clear();
            Kopfzeile = 0;
            Stand = "";

            if (string.IsNullOrWhiteSpace(pfad) || !File.Exists(pfad))
                return (false, new SpeicherImportMeldung("SPIMP_MSG_DATEI_FEHLT", pfad ?? ""));

            try
            {
                string endung = Path.GetExtension(pfad).ToLowerInvariant();

                // ClosedXML liest ausschliesslich OOXML - das alte .xls und das
                // binaere .xlsb bekommen die gezielte Meldung statt einer Ausnahme
                // aus der Bibliothek (woertlich die Regel aus GanglinienDatei).
                if (endung == ".xls" || endung == ".xlsb")
                    return (false, new SpeicherImportMeldung("SPIMP_MSG_FORMAT_ALT", endung));

                List<string[]> zeilen = endung == ".xlsx" || endung == ".xlsm"
                    ? AusMappe(pfad)
                    : AusText(pfad);

                return AusTabelle(zeilen, Path.GetFileName(pfad));
            }
            catch (Exception ex)
            {
                return (false, new SpeicherImportMeldung("SPIMP_MSG_FEHLER", ex.Message));
            }
        }

        // =================================================================
        //  Die zwei Wege in die Tabelle
        // =================================================================

        /// <summary>
        /// Die benutzte Fläche des ersten Blattes als Textzeilen — ein einziger
        /// Bulk-Read, wie in <c>GanglinienDatei.ExcelBulkRead</c>.
        /// </summary>
        private static List<string[]> AusMappe(string pfad)
        {
            var raus = new List<string[]>();

            using (FileStream strom = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (XLWorkbook mappe = new XLWorkbook(strom))
            {
                if (mappe.Worksheets.Count == 0) return raus;

                IXLWorksheet blatt = mappe.Worksheet(1);
                IXLRange bereich = blatt.RangeUsed();
                if (bereich == null) return raus;

                int zeilen = bereich.RowCount();
                int spalten = bereich.ColumnCount();
                for (int z = 1; z <= zeilen; z++)
                {
                    string[] felder = new string[spalten];
                    for (int s = 1; s <= spalten; s++)
                        felder[s - 1] = Zellentext(bereich.Cell(z, s).Value);
                    raus.Add(felder);
                }
            }
            return raus;
        }

        /// <summary>
        /// Der Zellinhalt als Text. Zahlen kommen INVARIANT heraus, damit der
        /// nachgelagerte <see cref="StromspeicherImportSatz.Zahl"/> denselben Text
        /// sieht wie aus einer CSV-Datei — sonst hinge das Ergebnis an der Kultur
        /// des Arbeitsplatzes.
        /// </summary>
        private static string Zellentext(XLCellValue wert)
        {
            if (wert.IsBlank) return "";
            if (wert.IsNumber) return wert.GetNumber().ToString("R", CultureInfo.InvariantCulture);
            if (wert.IsDateTime) return wert.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (wert.IsBoolean) return wert.GetBoolean() ? "TRUE" : "FALSE";
            if (wert.IsText) return wert.GetText() ?? "";
            return wert.ToString() ?? "";
        }

        /// <summary>
        /// Eine Textdatei als Tabelle. Das Trennzeichen ist das häufigere von
        /// <c>;</c> und <c>,</c> in der ersten belegten Zeile — dieselbe Regel wie
        /// beim Modul- und Wechselrichterimport. Zerlegt wird mit
        /// <see cref="CsvReader"/> (NReco), weil die CEC-Kopfzeile Zeilenumbrüche
        /// IN Anführungszeichen führt; ein zeilenweiser Zerleger zerrisse sie.
        /// </summary>
        private static List<string[]> AusText(string pfad)
        {
            return StromspeicherImportSatz.TabelleAusText(StromspeicherImportSatz.LiesText(pfad));
        }

        // =================================================================
        //  Der Zerleger selbst
        // =================================================================

        /// <summary>
        /// Erkennt Kopfzeile und Spalten und baut die Sätze. Öffentlich, damit die
        /// Prüfung ihn ohne Datei fahren kann.
        /// </summary>
        internal (bool Erfolg, SpeicherImportMeldung Meldung) AusTabelle(List<string[]> zeilen, string quelle)
        {
            if (zeilen == null || zeilen.Count == 0)
                return (false, new SpeicherImportMeldung("SPIMP_MSG_LEER"));

            // Der Stand steht in einer der Erlaeuterungszeilen ("Data has not
            // changed since ..."). Er ist ein Ausweis, keine Pflicht.
            foreach (string[] z in zeilen.Take(KOPF_SUCHTIEFE))
            {
                if (z.Length > 0 && z[0] != null &&
                    z[0].StartsWith("Data has not changed since", StringComparison.OrdinalIgnoreCase))
                {
                    Stand = z[0].Trim();
                    break;
                }
            }

            int kopf = -1;
            Dictionary<string, int> spalte = null;
            List<string> fehlendZuletzt = new List<string>(PFLICHTSPALTEN);

            int tiefe = Math.Min(zeilen.Count, KOPF_SUCHTIEFE);
            for (int z = 0; z < tiefe; z++)
            {
                Dictionary<string, int> kandidat = Spaltenkarte(zeilen[z]);
                List<string> fehlend = PFLICHTSPALTEN.Where(s => !kandidat.ContainsKey(s)).ToList();
                if (fehlend.Count == 0) { kopf = z; spalte = kandidat; break; }
                if (fehlend.Count < fehlendZuletzt.Count) fehlendZuletzt = fehlend;
            }

            if (kopf < 0)
                return (false, new SpeicherImportMeldung("SPIMP_MSG_KOPFZEILE",
                                                         string.Join(", ", fehlendZuletzt)));

            Kopfzeile = kopf + 1;

            int iHersteller = spalte["manufacturer name"];
            int iModell = spalte["model number"];
            int iTechnik = Spalte(spalte, "technology");
            int iEnergie = spalte["nameplate energy capacity"];
            int iLeistung = spalte["nameplate power"];
            int iDauerlast = Spalte(spalte, "maximum continuous discharge rate");
            int iEta = Spalte(spalte, "manufacturer declared roundtrip efficiency");

            for (int z = kopf + 1; z < zeilen.Count; z++)
            {
                string[] f = zeilen[z];

                string Feld(int idx) => (idx >= 0 && idx < f.Length && f[idx] != null) ? f[idx].Trim() : "";

                string modell = Feld(iModell);
                if (modell.Length == 0) continue;                 // Einheitenzeile und Leerzeilen

                double energie = StromspeicherImportSatz.Zahl(Feld(iEnergie));
                if (!(energie > 0.0)) continue;                   // ohne Kapazitaet kein Speicher

                double leistung = StromspeicherImportSatz.Zahl(Feld(iLeistung));
                if (!(leistung > 0.0)) leistung = StromspeicherImportSatz.Zahl(Feld(iDauerlast));

                _saetze.Add(new StromspeicherImportSatz
                {
                    Hersteller = Feld(iHersteller),
                    Modell = modell,
                    Technologie = Feld(iTechnik),
                    EnergieKwh = energie,
                    LeistungKw = leistung,
                    WirkungsgradRt = StromspeicherImportSatz.WirkungsgradAusText(Feld(iEta)),
                    StandbyW = 0.0,                               // fuehrt die Liste nicht
                    Quelle = quelle ?? ""
                });
            }

            if (_saetze.Count == 0)
                return (false, new SpeicherImportMeldung("SPIMP_MSG_KEINE_SAETZE"));

            return (true, new SpeicherImportMeldung("SPIMP_MSG_GELADEN",
                _saetze.Count.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>Beschriftung → Spaltenindex; die erste Nennung gewinnt.</summary>
        private static Dictionary<string, int> Spaltenkarte(string[] zeile)
        {
            var karte = new Dictionary<string, int>(StringComparer.Ordinal);
            if (zeile == null) return karte;
            for (int i = 0; i < zeile.Length; i++)
            {
                string name = StromspeicherImportSatz.Kopfname(zeile[i]);
                if (name.Length == 0) continue;
                if (!karte.ContainsKey(name)) karte[name] = i;
            }
            return karte;
        }

        private static int Spalte(Dictionary<string, int> karte, string name)
        {
            int i;
            return karte.TryGetValue(name, out i) ? i : -1;
        }

        /// <summary>Die Hersteller der geladenen Liste, aufsteigend und ohne Dubletten.</summary>
        public IEnumerable<string> Hersteller()
        {
            return _saetze.Select(s => s.Hersteller)
                          .Where(h => !string.IsNullOrEmpty(h))
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .OrderBy(h => h, StringComparer.CurrentCulture);
        }
    }
}
