using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using RR = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die drei Tabellen mit reiner Excel-Quelle</b> (Konzept Berichtsvorlagen 5.4, 7.3, Anhang A; Etappe BV-E9):
    /// <c>tabelle.wirtschaft.parameter</c> (Parameterblock der Formelmappe), <c>tabelle.wirtschaft.verlauf</c> (Blatt „Verlauf“)
    /// und <c>stand.tabelle.monatswerte</c> (Monatsblock des Detailblatts). Es gibt sie nur als Excel-Block — darum baut der
    /// Kern sie <b>aus denselben Blattbauern</b>: Der Bauer schreibt in ein Blatt einer Arbeitsmappe im Speicher, und
    /// <see cref="AusBlatt"/> liest den Block Zelle für Zelle als <see cref="Berichtstabelle"/> zurück — Zahlen mit ihrem
    /// Excel-Zahlenformat (<see cref="Tabellenzelle.Excelformat"/>), Fettdruck und Hinterlegung. So zeigt die Vorlage dieselben
    /// Zahlen wie die erzeugte Mappe, ohne dass ein zweiter Rechenweg entsteht. Ausgabe nur Excel.
    /// </summary>
    public static partial class Berichtstabellen
    {
        /// <summary>
        /// <b><c>tabelle.wirtschaft.parameter</c></b> — der Parameterblock der Formelmappe
        /// (<see cref="ExcelFormelmappe.Parameterblock(IXLWorksheet, int, WirtschaftlichkeitParameter, IEnumerable{VariantenDaten}, Formelregister, Func{VariantenDaten, IReadOnlyList{Traegerpreissatz}})"/>):
        /// Kopf „Parameter · Erwartet · Günstig · Ungünstig · Name“, je Satz eine Zeile als Anteil im Prozentformat, die
        /// gepflegten Trägerpreise der Stände, die beiden Hinweiszeilen darunter.
        /// </summary>
        public static Berichtstabelle Parameter(BerichtsDaten daten, WirtschaftsBerichtswerte werte, CultureInfo kultur)
        {
            if (werte == null || werte.Ergebnisse.Count == 0 || werte.Parameter == null)
                return Leer(nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), kultur);
            return Blatt(ws =>
            {
                int ende = ExcelFormelmappe.Parameterblock(ws, 1, werte.Parameter, daten?.Varianten, new Formelregister(),
                                                           werte.Traegerpreise);
                return (1, ende - 2);
            }, true, kultur);
        }

        /// <summary>
        /// <b><c>tabelle.wirtschaft.verlauf</c></b> — das Blatt „Verlauf“ (<see cref="VerlaufExcel.SchreibeBlatt"/>) als Bereich:
        /// die Zeile der Szenarien, die Kopfzeile der Stände, je Jahr eine Zeile, Nulldurchgang, Restwert und
        /// Kapitalwertdifferenz; die Statuszeile als Hinweis. Verbundene Köpfe — nicht listentauglich, ein erzeugter Bereich.
        /// </summary>
        public static Berichtstabelle Verlauf(WirtschaftsBerichtswerte werte, CultureInfo kultur)
        {
            if (werte == null || werte.Ergebnisse.Count == 0) return Leer(nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), kultur);
            if (werte.VerlaufEntfaellt) return Leer(nameof(RR.BV_GRUND_NICHT_VERFUEGBAR), kultur);
            WirtschaftlichkeitVerlaufSzenarien verlauf = werte.Verlauf;
            if (verlauf == null || verlauf.Leer) return Leer(nameof(RR.BV_GRUND_KEIN_VERLAUF), kultur);
            string status = string.Format(kultur, RR.ResourceManager.GetString(nameof(RR.WIRT_VERL_STATUS), kultur) ?? "{0}",
                                          verlauf.Jahre) + ".";
            Berichtstabelle t = Blatt(ws =>
            {
                VerlaufExcel.SchreibeBlatt(ws.Workbook, verlauf, VerlaufBlattTexte.AusRessourcen(), status);
                IXLWorksheet blatt = ws.Workbook.Worksheets.Last();
                return (-1, blatt.LastRowUsed()?.RowNumber() ?? 0);
            }, false, kultur, VerlaufExcel.ZEILE_JAHR0 - 2, true);
            t.Hinweis(status);
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.monatswerte</c></b> — die Monatswerte des Stands in MWh aus dem Simulationslauf dieses Berichts
        /// (Monatsblock des Detailblatts, <see cref="ExcelBerichtGenerator.MonatsBlock"/>): Kopf „Monat“ und je Reihe eine
        /// Spalte, zwölf Zeilen. Listentauglich — auch als Excel-Tabelle <c>EPOS_stand__tabelle__monatswerte</c> auf dem
        /// Musterblatt.
        /// </summary>
        public static Berichtstabelle Monatswerte(VariantenDaten v, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAND), kultur);
            if (v.Zeitreihen == null) return Leer(nameof(RR.BV_GRUND_KEINE_ZEITREIHEN), kultur);
            Berichtstabelle t = Blatt(ws =>
            {
                int ende = ExcelBerichtGenerator.MonatsBlock(ws, 1, v.Zeitreihen);
                return ende <= 1 ? (0, 0) : (2, ende - 2);
            }, true, kultur);
            return t;
        }

        /// <summary>
        /// Schreibt über <paramref name="bauer"/> in ein leeres Blatt einer Mappe im Speicher und liest den Block zurück. Der
        /// Bauer liefert die erste Zeile (die Kopfzeile, wenn <paramref name="mitKopf"/>; −1 = erste benutzte Zeile ab
        /// <paramref name="abZeile"/>) und die letzte Zeile; eine erste Zeile 0 heißt „kein Block“ — die Tabelle ist leer.
        /// <paramref name="imNeuenBlatt"/>: Der Bauer legt sein Blatt selbst an (das letzte der Mappe wird gelesen).
        /// </summary>
        private static Berichtstabelle Blatt(Func<IXLWorksheet, (int Von, int Bis)> bauer, bool mitKopf, CultureInfo kultur,
                                             int abZeile = 1, bool imNeuenBlatt = false)
        {
            using (var wb = new XLWorkbook())
            {
                IXLWorksheet ws = wb.Worksheets.Add("Block");
                (int von, int bis) = bauer(ws);
                if (imNeuenBlatt) ws = wb.Worksheets.Last();
                if (von < 0) von = abZeile;
                if (von <= 0 || bis < von) return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);
                return AusBlatt(ws, von, bis, mitKopf, kultur);
            }
        }

        /// <summary>
        /// Liest die Zeilen <paramref name="von"/> bis <paramref name="bis"/> eines Blattes als <see cref="Berichtstabelle"/>:
        /// die Spalten bis zur letzten benutzten; mit <paramref name="mitKopf"/> ist die erste Zeile der Kopf. Eine Zahl bleibt
        /// Zahl mit dem Excel-Format ihrer Zelle (eine Formel mit ihrem Ergebnis), ein Text Text, eine leere Zelle leer; Fett
        /// und die Hinterlegungen Kopf und Stamm der Blattbauer gehen mit. Nachlaufende Zeilen, die nur in Spalte A einen Text
        /// ohne Fettdruck tragen, werden Hinweise unter der Tabelle.
        /// </summary>
        internal static Berichtstabelle AusBlatt(IXLWorksheet ws, int von, int bis, bool mitKopf, CultureInfo kultur)
        {
            int spalten = 1;
            for (int r = von; r <= bis; r++)
            {
                IXLCell letzte = ws.Row(r).LastCellUsed(XLCellsUsedOptions.Contents);
                if (letzte != null) spalten = Math.Max(spalten, letzte.Address.ColumnNumber);
            }

            // Nachlaufende Hinweiszeilen: nur Spalte A, Text, nicht fett.
            var hinweise = new List<string>();
            while (bis > von + (mitKopf ? 1 : 0) && IstHinweiszeile(ws, bis, spalten))
            {
                hinweise.Insert(0, ws.Cell(bis, 1).GetString());
                bis--;
            }

            var t = new Berichtstabelle();
            t.Feste(Enumerable.Repeat(0, spalten).ToArray());
            int start = von;
            if (mitKopf)
            {
                t.MitKopf(Enumerable.Range(1, spalten).Select(c => Zellen.Kopf(ws.Cell(von, c).GetString())));
                start++;
            }
            for (int r = start; r <= bis; r++)
            {
                if (ws.Row(r).CellsUsed(XLCellsUsedOptions.Contents).Count() == 0) continue;
                List<Tabellenzelle> zellen = Enumerable.Range(1, spalten).Select(c => Zelle(ws.Cell(r, c), kultur)).ToList();
                bool gruppe = zellen.Count > 1 && zellen[0].Fett && zellen.Skip(1).All(z => z.Text.Length == 0);
                t.Zeile(zellen, gruppe ? Tabellenrolle.Gruppe : Tabellenrolle.Keine);
            }
            foreach (string h in hinweise) t.Hinweis(h);
            if (t.IstLeer) t.Leergrund = Grund(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);
            return t;
        }

        private static bool IstHinweiszeile(IXLWorksheet ws, int r, int spalten)
        {
            IXLCell a = ws.Cell(r, 1);
            if (!a.Value.IsText || a.Style.Font.Bold || a.GetString().Length == 0) return false;
            for (int c = 2; c <= spalten; c++)
                if (!ws.Cell(r, c).Value.IsBlank) return false;
            return true;
        }

        /// <summary>Eine Zelle des Blocks als Tabellenzelle (Zahl mit Excel-Format, sonst Text).</summary>
        private static Tabellenzelle Zelle(IXLCell c, CultureInfo kultur)
        {
            bool fett = c.Style.Font.Bold;
            Tabellenhinterlegung hinterlegung = Hinterlegung(c);
            XLCellValue wert;
            try { wert = c.Value; }
            catch (Exception) { wert = Blank.Value; }
            if (c.HasFormula)
            {
                try { wert = c.CachedValue.IsBlank ? c.Value : c.CachedValue; }
                catch (Exception) { /* bleibt */ }
            }

            if (wert.IsNumber)
            {
                double zahl = wert.GetNumber();
                string format = Excelformat(c);
                return new Tabellenzelle
                {
                    Text = Anzeige(zahl, format, kultur), Zahl = zahl, Excelformat = format,
                    Ausrichtung = Tabellenausrichtung.Rechts, Fett = fett, Hinterlegung = hinterlegung,
                };
            }
            string text = wert.IsBlank ? "" : wert.IsText ? wert.GetText() : c.GetFormattedString();
            return new Tabellenzelle
            {
                Text = text, Fett = fett, Hinterlegung = hinterlegung,
                Ausrichtung = c.Style.Alignment.Horizontal == XLAlignmentHorizontalValues.Right ? Tabellenausrichtung.Rechts
                            : c.Style.Alignment.Horizontal == XLAlignmentHorizontalValues.Center ? Tabellenausrichtung.Mitte
                            : Tabellenausrichtung.Links,
            };
        }

        private static Tabellenhinterlegung Hinterlegung(IXLCell c)
        {
            XLColor f = c.Style.Fill.BackgroundColor;
            if (Equals(f, ExcelBerichtGenerator.KOPF)) return Tabellenhinterlegung.Kopf;
            if (Equals(f, ExcelBerichtGenerator.STAMM)) return Tabellenhinterlegung.Stamm;
            return Tabellenhinterlegung.Keine;
        }

        /// <summary>Das Zahlenformat einer Zelle; „Standard“ als <c>General</c>.</summary>
        private static string Excelformat(IXLCell c)
        {
            string f = c.Style.NumberFormat.Format;
            if (!string.IsNullOrEmpty(f)) return f;
            switch (c.Style.NumberFormat.NumberFormatId)
            {
                case 1: return "0";
                case 2: return "0.00";
                case 3: return "#,##0";
                case 4: return "#,##0.00";
                case 9: return "0%";
                case 10: return "0.00%";
                default: return "General";
            }
        }

        /// <summary>Die Anzeige einer Zahl nach ihrem Excel-Format in der Kultur (für Katalog und Vorschau).</summary>
        private static string Anzeige(double zahl, string format, CultureInfo kultur)
        {
            string f = (format ?? "").Split(';')[0];
            int stellen = 0;
            int punkt = f.IndexOf('.');
            if (punkt >= 0) stellen = f.Skip(punkt + 1).TakeWhile(ch => ch == '0' || ch == '#').Count();
            if (f.Contains('%')) return (zahl * 100.0).ToString("N" + stellen, kultur) + " %";
            if (string.Equals(f, "General", StringComparison.Ordinal)) return zahl.ToString(kultur);
            return zahl.ToString("N" + stellen, kultur);
        }
    }
}
