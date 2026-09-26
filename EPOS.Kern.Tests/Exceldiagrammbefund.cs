using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using S = DocumentFormat.OpenXml.Spreadsheet;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Was eine Mappe an Excel-Diagrammen trägt</b> (Etappe BV-E8) — gelesen über das SDK: je Diagramm das Blatt, der
    /// Name der Zeichnung, Titel, die Diagrammarten und je Reihe die Bezüge (Name, Kategorien, Werte) samt Zwischenspeicher.
    /// Dazu die Zellwerte der Bezüge über ClosedXML.
    /// </summary>
    internal sealed class Exceldiagrammbefund
    {
        internal string Blatt;
        internal string Name;
        internal string Beschreibung;
        internal string Titel;
        internal int VonSpalte, VonZeile;
        internal List<string> Arten = new List<string>();
        internal string Balkenrichtung;
        internal string Gruppierung;
        internal List<Reihenbefund> Reihen = new List<Reihenbefund>();

        public override string ToString() { return Blatt + " · " + Name + " · " + Titel; }

        internal sealed class Reihenbefund
        {
            internal string Art;
            internal string NameBezug;
            internal string KategorienBezug;
            internal string WerteBezug;
            internal string NameSpeicher;
            internal List<double?> Speicher = new List<double?>();
            internal int Punkte;
        }

        /// <summary>Alle Diagramme der Mappe in Blatt- und Ankerfolge.</summary>
        internal static List<Exceldiagrammbefund> Lies(string pfad)
        {
            var liste = new List<Exceldiagrammbefund>();
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, false);
            WorkbookPart wbp = doc.WorkbookPart;
            foreach (S.Sheet s in wbp.Workbook.Sheets.Elements<S.Sheet>())
            {
                var teil = (WorksheetPart)wbp.GetPartById(s.Id);
                if (teil.DrawingsPart?.WorksheetDrawing == null) continue;
                foreach (Xdr.TwoCellAnchor anker in teil.DrawingsPart.WorksheetDrawing.Elements<Xdr.TwoCellAnchor>())
                {
                    C.ChartReference bezug = anker.Descendants<C.ChartReference>().FirstOrDefault();
                    if (bezug == null) continue;
                    var chart = (ChartPart)teil.DrawingsPart.GetPartById(bezug.Id);
                    Xdr.NonVisualDrawingProperties nv = anker.Descendants<Xdr.NonVisualDrawingProperties>().First();
                    var f = new Exceldiagrammbefund
                    {
                        Blatt = s.Name,
                        Name = nv.Name?.Value,
                        Beschreibung = nv.Description?.Value,
                        VonSpalte = int.Parse(anker.FromMarker.ColumnId.Text, CultureInfo.InvariantCulture),
                        VonZeile = int.Parse(anker.FromMarker.RowId.Text, CultureInfo.InvariantCulture),
                        Titel = string.Concat(chart.ChartSpace.Descendants<C.Chart>().First().Title?
                                              .Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text) ?? Array.Empty<string>()),
                    };
                    C.PlotArea flaeche = chart.ChartSpace.Descendants<C.PlotArea>().First();
                    foreach (OpenXmlElement g in flaeche.ChildElements)
                    {
                        if (g is C.Layout || g is C.CategoryAxis || g is C.ValueAxis || g is C.DateAxis || g is C.SeriesAxis) continue;
                        f.Arten.Add(g.LocalName);
                        if (g is C.BarChart bar)
                        {
                            f.Balkenrichtung = bar.BarDirection?.Val?.InnerText;
                            f.Gruppierung = bar.BarGrouping?.Val?.InnerText;
                        }
                        if (g is C.AreaChart area) f.Gruppierung = area.Grouping?.Val?.InnerText;
                        foreach (OpenXmlElement ser in g.ChildElements.Where(e => e.LocalName == "ser"))
                        {
                            var r = new Reihenbefund { Art = g.LocalName };
                            r.NameBezug = ser.GetFirstChild<C.SeriesText>()?.Descendants<C.Formula>().FirstOrDefault()?.Text;
                            r.NameSpeicher = ser.GetFirstChild<C.SeriesText>()?.Descendants<C.NumericValue>().FirstOrDefault()?.Text;
                            r.KategorienBezug = ser.GetFirstChild<C.CategoryAxisData>()?.Descendants<C.Formula>().FirstOrDefault()?.Text;
                            C.Values werte = ser.GetFirstChild<C.Values>();
                            r.WerteBezug = werte?.Descendants<C.Formula>().FirstOrDefault()?.Text;
                            C.NumberingCache speicher = werte?.Descendants<C.NumberingCache>().FirstOrDefault();
                            if (speicher != null)
                            {
                                r.Punkte = (int)(speicher.PointCount?.Val?.Value ?? 0);
                                var feld = new double?[r.Punkte];
                                foreach (C.NumericPoint p in speicher.Elements<C.NumericPoint>())
                                    feld[p.Index.Value] = double.Parse(p.NumericValue.Text, CultureInfo.InvariantCulture);
                                r.Speicher = feld.ToList();
                            }
                            f.Reihen.Add(r);
                        }
                    }
                    liste.Add(f);
                }
            }
            return liste;
        }

        /// <summary>Die Befunde des Validators (Office 2016) als lesbare Zeilen.</summary>
        internal static List<string> Validierungsfehler(string pfad)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, false);
            return new OpenXmlValidator(FileFormatVersions.Office2016).Validate(doc)
                .Select(f => f.Description + " @ " + f.Path?.XPath + " (" + f.Part?.Uri + ")")
                .ToList();
        }

        private static readonly Regex BEZUG = new Regex(@"^'?(?<blatt>(?:[^']|'')+?)'?!\$?(?<s1>[A-Z]+)\$?(?<z1>\d+)(?::\$?(?<s2>[A-Z]+)\$?(?<z2>\d+))?$");

        /// <summary>Zerlegt einen Bezug <c>'Blatt'!$B$4:$B$9</c>.</summary>
        internal static (string Blatt, int Spalte1, int Zeile1, int Spalte2, int Zeile2) Zerlege(string bezug)
        {
            Match m = BEZUG.Match(bezug ?? "");
            if (!m.Success) throw new FormatException("Kein einfacher Bezug: " + bezug);
            int s1 = Spalte(m.Groups["s1"].Value), z1 = int.Parse(m.Groups["z1"].Value, CultureInfo.InvariantCulture);
            int s2 = m.Groups["s2"].Success ? Spalte(m.Groups["s2"].Value) : s1;
            int z2 = m.Groups["z2"].Success ? int.Parse(m.Groups["z2"].Value, CultureInfo.InvariantCulture) : z1;
            return (m.Groups["blatt"].Value.Replace("''", "'"), s1, z1, s2, z2);
        }

        private static int Spalte(string buchstaben)
        {
            int n = 0;
            foreach (char c in buchstaben) n = n * 26 + (c - 'A' + 1);
            return n;
        }

        /// <summary>Die Zahlen hinter einem Bezug (leere Zelle = <c>null</c>).</summary>
        internal static List<double?> Zahlen(XLWorkbook wb, string bezug)
        {
            var (blatt, s1, z1, s2, z2) = Zerlege(bezug);
            IXLWorksheet ws = wb.Worksheet(blatt);
            var liste = new List<double?>();
            for (int z = z1; z <= z2; z++)
                for (int s = s1; s <= s2; s++)
                {
                    IXLCell c = ws.Cell(z, s);
                    liste.Add(c.Value.IsNumber ? c.Value.GetNumber() : (double?)null);
                }
            return liste;
        }

        /// <summary>Die Texte hinter einem Bezug.</summary>
        internal static List<string> Texte(XLWorkbook wb, string bezug)
        {
            var (blatt, s1, z1, s2, z2) = Zerlege(bezug);
            IXLWorksheet ws = wb.Worksheet(blatt);
            var liste = new List<string>();
            for (int z = z1; z <= z2; z++)
                for (int s = s1; s <= s2; s++) liste.Add(ws.Cell(z, s).GetFormattedString());
            return liste;
        }

        /// <summary>Die Zahlen einer Reihe mit dem Namen <paramref name="name"/> (über ihren Namensbezug).</summary>
        internal List<double?> Werte(XLWorkbook wb, string name)
        {
            Reihenbefund r = Reihen.FirstOrDefault(x => Texte(wb, x.NameBezug).Single() == name);
            if (r == null) throw new InvalidOperationException("Keine Reihe „" + name + "“ in " + this);
            return Zahlen(wb, r.WerteBezug);
        }
    }
}
