using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using S = DocumentFormat.OpenXml.Spreadsheet;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace WindowsFormsApplication1
{
    /// <summary>Wo ein Diagramm auf seinem Blatt steht: die Zelle links oben (nullbasiert) und die Größe in Spalten und Zeilen.</summary>
    internal readonly record struct Diagrammanker(int Spalte, int Zeile, int Spalten, int Zeilen)
    {
        /// <summary>Die Vorgabegröße eines Diagramms: zehn Spalten × zwanzig Zeilen (rund 640 × 400 Punkte).</summary>
        internal const int SPALTEN = 10, ZEILEN = 20;
    }

    /// <summary>
    /// Wo die Zahlen eines Diagramms stehen: das Blatt, die Kopfzeile (Reihennamen), die Datenzeilen und je Reihe ihre Spalte
    /// (einsbasiert, wie in Excel). Daraus entstehen die Reihenbezüge <c>'Diagrammdaten'!$B$4:$B$368</c>.
    /// </summary>
    internal sealed class Datenbereich
    {
        internal Datenbereich(string blatt, int kopfzeile, int ersteZeile, int zeilen, int kategorienspalte)
        {
            Blatt = blatt;
            Kopfzeile = kopfzeile;
            ErsteZeile = ersteZeile;
            Zeilen = zeilen;
            Kategorienspalte = kategorienspalte;
        }

        /// <summary>Der Name des Blattes.</summary>
        internal string Blatt { get; set; }

        /// <summary>Die Zeile der Reihennamen.</summary>
        internal int Kopfzeile { get; }

        /// <summary>Die erste Datenzeile.</summary>
        internal int ErsteZeile { get; }

        /// <summary>Die Zahl der Datenzeilen.</summary>
        internal int Zeilen { get; }

        /// <summary>Die Spalte der Kategorien.</summary>
        internal int Kategorienspalte { get; }

        /// <summary>Die Spalte je Reihe.</summary>
        internal Dictionary<Excelreihe, int> Spalten { get; } = new Dictionary<Excelreihe, int>();

        /// <summary>Die letzte Spalte des Bereichs.</summary>
        internal int LetzteSpalte { get { return Spalten.Count == 0 ? Kategorienspalte : Math.Max(Kategorienspalte, Spalten.Values.Max()); } }

        /// <summary>Der Bezug der Kategorien.</summary>
        internal string Kategorien { get { return Bezug(Blatt, Kategorienspalte, ErsteZeile, Kategorienspalte, ErsteZeile + Zeilen - 1); } }

        /// <summary>Der Bezug der Werte einer Reihe.</summary>
        internal string Werte(Excelreihe r) { return Bezug(Blatt, Spalten[r], ErsteZeile, Spalten[r], ErsteZeile + Zeilen - 1); }

        /// <summary>Der Bezug des Namens einer Reihe (ihr Spaltenkopf).</summary>
        internal string Name(Excelreihe r) { return Bezug(Blatt, Spalten[r], Kopfzeile, Spalten[r], Kopfzeile); }

        /// <summary>Ein absoluter Bezug mit Blattnamen in Hochkommas: <c>'Diagrammdaten'!$B$4:$B$9</c>.</summary>
        internal static string Bezug(string blatt, int spalte1, int zeile1, int spalte2, int zeile2)
        {
            string a = "$" + Spaltenname(spalte1) + "$" + zeile1.ToString(CultureInfo.InvariantCulture);
            string b = "$" + Spaltenname(spalte2) + "$" + zeile2.ToString(CultureInfo.InvariantCulture);
            return "'" + (blatt ?? "").Replace("'", "''") + "'!" + (a == b ? a : a + ":" + b);
        }

        /// <summary>Der Spaltenbuchstabe (1 → A, 27 → AA).</summary>
        internal static string Spaltenname(int spalte)
        {
            string s = "";
            for (int n = spalte; n > 0; n = (n - 1) / 26) s = (char)('A' + (n - 1) % 26) + s;
            return s;
        }
    }

    /// <summary>
    /// <b>Der Schreiber der Excel-Diagramme</b> (Konzept Berichtsvorlagen 7.4, Entscheid BV-Q11, Messprobe 6 von BV-E0;
    /// Etappe BV-E8): setzt ein <see cref="Exceldiagramm"/> über das OpenXML SDK als natives Excel-Diagramm auf ein Blatt —
    /// <c>ChartPart</c> im <c>DrawingsPart</c>, verankert über <c>TwoCellAnchor</c>, jede Reihe mit Bezügen auf die Zellen
    /// ihres <see cref="Datenbereich"/>s (Name, Kategorien, Werte) und einem Zwischenspeicher mit denselben Zahlen, damit
    /// auch eine Vorschau ohne Rechenwerk das Diagramm zeigt. ClosedXML kann keine Diagramme anlegen; der Schreiber läuft
    /// deshalb auf dem gespeicherten Paket, nach ClosedXML und nach dem Nachtrag der Formelergebnisse.
    /// </summary>
    internal static class Exceldiagrammschreiber
    {
        private const string CHART_URI = "http://schemas.openxmlformats.org/drawingml/2006/chart";

        /// <summary>Setzt das Diagramm auf das Blatt; gibt den neuen <c>ChartPart</c> zurück.</summary>
        internal static ChartPart Setze(WorksheetPart blatt, Diagrammanker anker, Exceldiagramm d, Datenbereich bereich, bool englisch)
        {
            DrawingsPart zeichnung = blatt.DrawingsPart ?? blatt.AddNewPart<DrawingsPart>();
            if (zeichnung.WorksheetDrawing == null)
            {
                zeichnung.WorksheetDrawing = new Xdr.WorksheetDrawing();
                zeichnung.WorksheetDrawing.AddNamespaceDeclaration("xdr", "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing");
                zeichnung.WorksheetDrawing.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            }
            ChartPart teil = zeichnung.AddNewPart<ChartPart>();
            teil.ChartSpace = Diagrammraum(d, bereich, englisch);
            teil.ChartSpace.Save();

            uint kennung = 2;
            foreach (Xdr.NonVisualDrawingProperties nv in zeichnung.WorksheetDrawing.Descendants<Xdr.NonVisualDrawingProperties>())
                if (nv.Id != null && nv.Id.Value >= kennung) kennung = nv.Id.Value + 1;

            zeichnung.WorksheetDrawing.Append(new Xdr.TwoCellAnchor(
                new Xdr.FromMarker(new Xdr.ColumnId(Zahl(anker.Spalte)), new Xdr.ColumnOffset("0"),
                                   new Xdr.RowId(Zahl(anker.Zeile)), new Xdr.RowOffset("0")),
                new Xdr.ToMarker(new Xdr.ColumnId(Zahl(anker.Spalte + anker.Spalten)), new Xdr.ColumnOffset("0"),
                                 new Xdr.RowId(Zahl(anker.Zeile + anker.Zeilen)), new Xdr.RowOffset("0")),
                new Xdr.GraphicFrame(
                    new Xdr.NonVisualGraphicFrameProperties(
                        // Name = der Bildschlüssel (Auswahlbereich von Excel), Alternativtext = der Titel samt Stand (BV-Q14).
                        new Xdr.NonVisualDrawingProperties
                        {
                            Id = kennung,
                            Name = d.Schluessel,
                            Description = string.IsNullOrEmpty(d.Bezug) ? d.Titel : d.Titel + " — " + d.Bezug,
                        },
                        new Xdr.NonVisualGraphicFrameDrawingProperties()),
                    new Xdr.Transform(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = 0L, Cy = 0L }),
                    new A.Graphic(new A.GraphicData(new C.ChartReference { Id = zeichnung.GetIdOfPart(teil) }) { Uri = CHART_URI }))
                { Macro = "" },
                new Xdr.ClientData())
            { EditAs = Xdr.EditAsValues.OneCell });
            zeichnung.WorksheetDrawing.Save();

            // Das drawing-Element steht im Blatt VOR legacyDrawing … tableParts, extLst (Reihenfolge von CT_Worksheet).
            S.Worksheet ws = blatt.Worksheet;
            if (!ws.Elements<S.Drawing>().Any())
            {
                var verweis = new S.Drawing { Id = blatt.GetIdOfPart(zeichnung) };
                OpenXmlElement danach = ws.ChildElements.FirstOrDefault(e =>
                    e is S.LegacyDrawing || e is S.LegacyDrawingHeaderFooter || e is S.DrawingHeaderFooter ||
                    e is S.Picture || e is S.OleObjects || e is S.Controls || e is S.WebPublishItems ||
                    e is S.TableParts || e is S.WorksheetExtensionList);
                if (danach != null) ws.InsertBefore(verweis, danach);
                else ws.Append(verweis);
            }
            ws.Save();
            return teil;
        }

        // =====================================================================
        //  Der Diagrammraum
        // =====================================================================

        private static C.ChartSpace Diagrammraum(Exceldiagramm d, Datenbereich bereich, bool englisch)
        {
            const uint KATEGORIEACHSE = 500000001u, WERTEACHSE = 500000002u;
            List<Excelreihe> reihen = d.Reihen.Where(r => !r.NurDaten).ToList();
            bool kreis = reihen.Any(r => r.Art == Excelreihenart.Kreis);
            bool waagerecht = reihen.Any(r => r.Art == Excelreihenart.Balken);

            var flaeche = new C.PlotArea(new C.Layout());
            var index = new Dictionary<Excelreihe, uint>();
            uint i = 0;
            foreach (Excelreihe r in reihen) index[r] = i++;

            if (kreis)
            {
                var torte = new C.PieChart(new C.VaryColors { Val = true });
                foreach (Excelreihe r in reihen.Where(r => r.Art == Excelreihenart.Kreis))
                    torte.Append(Kreisreihe(r, index[r], d, bereich));
                torte.Append(new C.FirstSliceAngle { Val = (UInt16Value)0 });
                flaeche.Append(torte);
            }
            else
            {
                List<Excelreihe> flaechen = reihen.Where(r => r.Art == Excelreihenart.Flaeche).ToList();
                if (flaechen.Count > 0)
                {
                    var gruppe = new C.AreaChart(
                        new C.Grouping { Val = d.Gestapelt ? C.GroupingValues.Stacked : C.GroupingValues.Standard },
                        new C.VaryColors { Val = false });
                    foreach (Excelreihe r in flaechen) gruppe.Append(Flaechenreihe(r, index[r], d, bereich));
                    gruppe.Append(new C.AxisId { Val = KATEGORIEACHSE }, new C.AxisId { Val = WERTEACHSE });
                    flaeche.Append(gruppe);
                }

                List<Excelreihe> saeulen = reihen.Where(r => r.Art == Excelreihenart.Saeule || r.Art == Excelreihenart.Balken).ToList();
                if (saeulen.Count > 0)
                {
                    var gruppe = new C.BarChart(
                        new C.BarDirection { Val = waagerecht ? C.BarDirectionValues.Bar : C.BarDirectionValues.Column },
                        new C.BarGrouping { Val = d.Gestapelt ? C.BarGroupingValues.Stacked : C.BarGroupingValues.Clustered },
                        new C.VaryColors { Val = false });
                    foreach (Excelreihe r in saeulen) gruppe.Append(Saeulenreihe(r, index[r], d, bereich));
                    gruppe.Append(new C.GapWidth { Val = (UInt16Value)(d.Ueberdeckt ? 60 : 80) });
                    if (d.Gestapelt || d.Ueberdeckt) gruppe.Append(new C.Overlap { Val = 100 });
                    gruppe.Append(new C.AxisId { Val = KATEGORIEACHSE }, new C.AxisId { Val = WERTEACHSE });
                    flaeche.Append(gruppe);
                }

                List<Excelreihe> linien = reihen.Where(r => r.Art == Excelreihenart.Linie).ToList();
                if (linien.Count > 0)
                {
                    var gruppe = new C.LineChart(
                        new C.Grouping { Val = C.GroupingValues.Standard },
                        new C.VaryColors { Val = false });
                    foreach (Excelreihe r in linien) gruppe.Append(Linienreihe(r, index[r], d, bereich));
                    gruppe.Append(new C.ShowMarker { Val = false });
                    gruppe.Append(new C.AxisId { Val = KATEGORIEACHSE }, new C.AxisId { Val = WERTEACHSE });
                    flaeche.Append(gruppe);
                }

                flaeche.Append(Kategorienachse(d, KATEGORIEACHSE, WERTEACHSE, waagerecht));
                flaeche.Append(Wertachse(d, WERTEACHSE, KATEGORIEACHSE, waagerecht));
            }

            var legende = new C.Legend(new C.LegendPosition { Val = C.LegendPositionValues.Bottom });
            if (!kreis)
                foreach (Excelreihe r in reihen.Where(r => r.OhneLegende || r.Unsichtbar))
                    legende.Append(new C.LegendEntry(new C.Index { Val = index[r] }, new C.Delete { Val = true }));
            legende.Append(new C.Overlay { Val = false });

            var diagramm = new C.Chart(
                Titel(d.Titel, 1400, true),
                new C.AutoTitleDeleted { Val = false },
                flaeche,
                legende,
                new C.PlotVisibleOnly { Val = true },
                new C.DisplayBlanksAs { Val = C.DisplayBlanksAsValues.Gap });

            var raum = new C.ChartSpace(
                new C.Date1904 { Val = false },
                new C.EditingLanguage { Val = englisch ? "en-US" : "de-DE" },
                new C.RoundedCorners { Val = false },
                diagramm);
            raum.AddNamespaceDeclaration("c", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            raum.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            raum.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            return raum;
        }

        // =====================================================================
        //  Reihen
        // =====================================================================

        private static C.LineChartSeries Linienreihe(Excelreihe r, uint index, Exceldiagramm d, Datenbereich b)
        {
            var reihe = new C.LineChartSeries(new C.Index { Val = index }, new C.Order { Val = index }, Reihenname(r, b),
                                              new C.ChartShapeProperties(Strich(r)),
                                              new C.Marker(new C.Symbol { Val = C.MarkerStyleValues.None }));
            reihe.Append(Kategorien(d, b), Werte(r, d, b), new C.Smooth { Val = false });
            return reihe;
        }

        private static C.AreaChartSeries Flaechenreihe(Excelreihe r, uint index, Exceldiagramm d, Datenbereich b)
        {
            return new C.AreaChartSeries(new C.Index { Val = index }, new C.Order { Val = index }, Reihenname(r, b),
                                         new C.ChartShapeProperties(Fuellung(r.Farbe, r.Deckung, r.Unsichtbar), new A.Outline(new A.NoFill())),
                                         Kategorien(d, b), Werte(r, d, b));
        }

        private static C.BarChartSeries Saeulenreihe(Excelreihe r, uint index, Exceldiagramm d, Datenbereich b)
        {
            var reihe = new C.BarChartSeries(new C.Index { Val = index }, new C.Order { Val = index }, Reihenname(r, b),
                                             new C.ChartShapeProperties(Fuellung(r.Farbe, r.Deckung, r.Unsichtbar),
                                                                        new A.Outline(new A.NoFill())),
                                             new C.InvertIfNegative { Val = false });
            if (r.Punktfarben != null && !r.Unsichtbar)
                for (int p = 0; p < r.Punktfarben.Length; p++)
                    if (r.Punktfarben[p] != null && p < r.Werte.Length && r.Werte[p].HasValue)
                        reihe.Append(new C.DataPoint(new C.Index { Val = (uint)p }, new C.InvertIfNegative { Val = false },
                                                     new C.Bubble3D { Val = false },
                                                     new C.ChartShapeProperties(Fuellung(r.Punktfarben[p], r.Deckung, false),
                                                                                new A.Outline(new A.NoFill()))));
            reihe.Append(Kategorien(d, b), Werte(r, d, b));
            return reihe;
        }

        private static C.PieChartSeries Kreisreihe(Excelreihe r, uint index, Exceldiagramm d, Datenbereich b)
        {
            var reihe = new C.PieChartSeries(new C.Index { Val = index }, new C.Order { Val = index }, Reihenname(r, b));
            if (r.Punktfarben != null)
                for (int p = 0; p < r.Punktfarben.Length; p++)
                    if (r.Punktfarben[p] != null)
                        reihe.Append(new C.DataPoint(new C.Index { Val = (uint)p }, new C.Bubble3D { Val = false },
                                                     new C.ChartShapeProperties(Fuellung(r.Punktfarben[p], 255, false),
                                                                                new A.Outline(new A.SolidFill(Farbe("FFFFFF", 255))))));
            reihe.Append(Kategorien(d, b), Werte(r, d, b));
            return reihe;
        }

        private static C.SeriesText Reihenname(Excelreihe r, Datenbereich b)
        {
            return new C.SeriesText(new C.StringReference(new C.Formula(b.Name(r)), Textspeicher(new[] { r.Name })));
        }

        private static C.CategoryAxisData Kategorien(Exceldiagramm d, Datenbereich b)
        {
            if (d.Kategorien.Count > 0 && d.Kategorien.All(k => k is double))
                return new C.CategoryAxisData(new C.NumberReference(new C.Formula(b.Kategorien),
                    Zahlspeicher(d.Kategorien.Select(k => (double?)(double)k).ToList(), b.Zeilen, "General")));
            return new C.CategoryAxisData(new C.StringReference(new C.Formula(b.Kategorien),
                Textspeicher(Enumerable.Range(0, b.Zeilen)
                                       .Select(k => k < d.Kategorien.Count ? Convert.ToString(d.Kategorien[k], CultureInfo.InvariantCulture) : "")
                                       .ToArray())));
        }

        private static C.Values Werte(Excelreihe r, Exceldiagramm d, Datenbereich b)
        {
            return new C.Values(new C.NumberReference(new C.Formula(b.Werte(r)), Zahlspeicher(r.Werte, b.Zeilen, d.Zahlformat)));
        }

        /// <summary>Der Zahlenspeicher einer Reihe: je Zelle mit Zahl ein Punkt, Lücken bleiben aus.</summary>
        internal static C.NumberingCache Zahlspeicher(IReadOnlyList<double?> werte, int anzahl, string format)
        {
            var speicher = new C.NumberingCache(new C.FormatCode(string.IsNullOrEmpty(format) ? "General" : format),
                                                new C.PointCount { Val = (uint)anzahl });
            for (int i = 0; i < anzahl && i < werte.Count; i++)
                if (werte[i].HasValue)
                    speicher.Append(new C.NumericPoint(new C.NumericValue(werte[i].Value.ToString("R", CultureInfo.InvariantCulture)))
                    { Index = (uint)i });
            return speicher;
        }

        /// <summary>Der Textspeicher: je Zelle ein Punkt.</summary>
        internal static C.StringCache Textspeicher(IReadOnlyList<string> texte)
        {
            var speicher = new C.StringCache(new C.PointCount { Val = (uint)texte.Count });
            for (int i = 0; i < texte.Count; i++)
                if (texte[i] != null)
                    speicher.Append(new C.StringPoint(new C.NumericValue(texte[i])) { Index = (uint)i });
            return speicher;
        }

        // =====================================================================
        //  Achsen und Titel
        // =====================================================================

        private static C.CategoryAxis Kategorienachse(Exceldiagramm d, uint id, uint gegen, bool waagerecht)
        {
            var achse = new C.CategoryAxis(
                new C.AxisId { Val = id },
                new C.Scaling(new C.Orientation { Val = d.KategorienVonOben ? C.OrientationValues.MaxMin : C.OrientationValues.MinMax }),
                new C.Delete { Val = false },
                new C.AxisPosition { Val = waagerecht ? C.AxisPositionValues.Left : C.AxisPositionValues.Bottom });
            if (!string.IsNullOrEmpty(d.Kategorienkopf)) achse.Append(Titel(d.Kategorienkopf, 1000, false));
            achse.Append(
                new C.NumberingFormat { FormatCode = "General", SourceLinked = true },
                new C.MajorTickMark { Val = C.TickMarkValues.Outside },
                new C.MinorTickMark { Val = C.TickMarkValues.None },
                new C.TickLabelPosition { Val = C.TickLabelPositionValues.Low },
                new C.CrossingAxis { Val = gegen },
                new C.Crosses { Val = C.CrossesValues.AutoZero },
                new C.AutoLabeled { Val = true },
                new C.LabelAlignment { Val = C.LabelAlignmentValues.Center },
                new C.LabelOffset { Val = (UInt16Value)100 });
            if (d.Beschriftungsabstand > 1)
                achse.Append(new C.TickLabelSkip { Val = d.Beschriftungsabstand }, new C.TickMarkSkip { Val = d.Beschriftungsabstand });
            achse.Append(new C.NoMultiLevelLabels { Val = true });
            return achse;
        }

        private static C.ValueAxis Wertachse(Exceldiagramm d, uint id, uint gegen, bool waagerecht)
        {
            var achse = new C.ValueAxis(
                new C.AxisId { Val = id },
                new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
                new C.Delete { Val = false },
                new C.AxisPosition { Val = waagerecht ? C.AxisPositionValues.Bottom : C.AxisPositionValues.Left },
                new C.MajorGridlines(new C.ChartShapeProperties(new A.Outline(new A.SolidFill(Farbe("D9D9D9", 255))) { Width = 9525 })));
            if (!string.IsNullOrEmpty(d.Wertachse)) achse.Append(Titel(d.Wertachse, 1000, false));
            achse.Append(
                new C.NumberingFormat { FormatCode = string.IsNullOrEmpty(d.Zahlformat) ? "General" : d.Zahlformat, SourceLinked = false },
                new C.MajorTickMark { Val = C.TickMarkValues.Outside },
                new C.MinorTickMark { Val = C.TickMarkValues.None },
                new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
                new C.CrossingAxis { Val = gegen },
                new C.Crosses { Val = d.KategorienVonOben ? C.CrossesValues.Maximum : C.CrossesValues.AutoZero },
                new C.CrossBetween { Val = C.CrossBetweenValues.Between });
            return achse;
        }

        private static C.Title Titel(string text, int groesse, bool fett)
        {
            return new C.Title(
                new C.ChartText(new C.RichText(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(
                        new A.ParagraphProperties(new A.DefaultRunProperties { FontSize = groesse, Bold = fett }),
                        new A.Run(new A.RunProperties { Language = "de-DE", FontSize = groesse, Bold = fett }, new A.Text(text ?? ""))))),
                new C.Overlay { Val = false });
        }

        // =====================================================================
        //  Farben und Striche
        // =====================================================================

        private static OpenXmlElement Fuellung(string farbe, byte deckung, bool unsichtbar)
        {
            if (unsichtbar) return new A.NoFill();
            if (string.IsNullOrEmpty(farbe)) return new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.Accent1 });
            return new A.SolidFill(Farbe(farbe, deckung));
        }

        private static A.Outline Strich(Excelreihe r)
        {
            double pt = r.Staerke > 0 ? r.Staerke : 1.5;
            var linie = new A.Outline { Width = (int)Math.Round(pt * 12700), CapType = A.LineCapValues.Flat };
            linie.Append(string.IsNullOrEmpty(r.Farbe)
                ? (OpenXmlElement)new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.Accent1 })
                : new A.SolidFill(Farbe(r.Farbe, r.Deckung)));
            linie.Append(new A.PresetDash
            {
                Val = r.Strich == Excelstrich.Gestrichelt ? A.PresetLineDashValues.Dash
                    : r.Strich == Excelstrich.Gepunktet ? A.PresetLineDashValues.SystemDot
                    : A.PresetLineDashValues.Solid
            });
            linie.Append(new A.Round());
            return linie;
        }

        private static A.RgbColorModelHex Farbe(string hex, byte deckung)
        {
            var farbe = new A.RgbColorModelHex { Val = hex };
            if (deckung < 255) farbe.Append(new A.Alpha { Val = (int)Math.Round(deckung / 255.0 * 100000) });
            return farbe;
        }

        private static string Zahl(int n) { return n.ToString(CultureInfo.InvariantCulture); }
    }
}
