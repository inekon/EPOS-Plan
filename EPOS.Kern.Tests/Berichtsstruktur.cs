using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ASVG = DocumentFormat.OpenXml.Office2019.Drawing.SVG;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Strukturliste eines Berichts</b> (Konzept Berichtsvorlagen, Etappe BV-E0
    /// „Strukturmesslatte“): Wort- und Tabellenbericht als Liste von Textzeilen, die das
    /// GERÜST festhält und das Laufdatum nicht.
    ///
    /// <para><b>Wortbericht</b> (<see cref="Word"/>): zuerst Kopf- und Fußzeilen, auf die die
    /// letzte Abschnittsangabe verweist, dann je Kind des Rumpfs in seiner Reihenfolge eine
    /// Zeile — Absatz mit Stil-ID und Text, Tabelle mit Spalten- und Zeilenzahl und den
    /// Zellen der Kopfzeile, Bildstelle mit Breite × Höhe in Pixeln und „SVG ja/nein“,
    /// Seitenumbruch, TOC-Feld, Abschnittsangabe mit Seitenmaß, Rändern und Verweisen. Felder
    /// stehen als <c>{CODE}</c> ohne ihr Ergebnis; Tabulator als ⇥, Zeilenumbruch als ↵.</para>
    ///
    /// <para><b>Tabellenbericht</b> (<see cref="Excel"/>): je Blatt Name, belegter Bereich,
    /// Fixierung, Autofilter und verbundene Bereiche, dann die ersten
    /// <see cref="ERSTE_ZEILEN"/> Zeilen und jede ANKERZEILE (eine Zeile mit mindestens einer
    /// fetten Zelle — Titel, Kopfzeilen, Blocküberschriften); je Zelle ihre Spalte und ihr
    /// Inhalt: Text wörtlich, Zahl als <c>#</c>, Formel als <c>=FORMEL</c>, Datum als
    /// <see cref="DATUM"/>. Zum Schluss die benannten Bereiche mit ihrem Bezug.</para>
    ///
    /// <para><b>Ohne Zeitstempel:</b> Jedes Datum im Text — <c>dd.MM.yyyy</c>, auch mit
    /// Uhrzeit — wird zu <see cref="DATUM"/> (<see cref="Normalisiere"/>). Die Zahlen des
    /// Wortberichts bleiben stehen: Sie kommen aus derselben Rechnung und derselben
    /// Kultur, und genau eine andere Formatierung soll die Messlatte zeigen.</para>
    /// </summary>
    internal static class Berichtsstruktur
    {
        /// <summary>Ersatz für jedes Datum im Text.</summary>
        internal const string DATUM = "<datum>";

        /// <summary>Wie viele Zeilen je Blatt unabhängig von Ankern gelistet werden.</summary>
        internal const int ERSTE_ZEILEN = 3;

        private static readonly Regex DatumMuster = new Regex(
            @"\b\d{1,2}\.\d{1,2}\.\d{4}\b(?:,?\s+\d{1,2}:\d{2}(?::\d{2})?(?:\s*Uhr)?)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Leerraum = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Ersetzt jedes Datum (samt anhängender Uhrzeit) durch <see cref="DATUM"/>.</summary>
        internal static string Normalisiere(string text)
            => string.IsNullOrEmpty(text) ? "" : DatumMuster.Replace(text, DATUM);

        // =====================================================================
        //  Wortbericht
        // =====================================================================

        /// <summary>Die Strukturliste des Wortberichts <paramref name="docxPfad"/>.</summary>
        internal static List<string> Word(string docxPfad)
        {
            var liste = new List<string>();
            using WordprocessingDocument doc = WordprocessingDocument.Open(docxPfad, false);
            MainDocumentPart main = doc.MainDocumentPart;
            Body body = main.Document.Body;
            SectionProperties letzte = body.Elements<SectionProperties>().LastOrDefault();

            liste.Add("## Kopf- und Fußzeilen");
            int vorher = liste.Count;
            if (letzte != null)
            {
                foreach (HeaderReference h in letzte.Elements<HeaderReference>())
                    if (main.GetPartById(h.Id) is HeaderPart hp && hp.Header != null)
                        foreach (OpenXmlElement e in hp.Header.ChildElements)
                            liste.Add("Kopfzeile " + (h.Type?.InnerText ?? "default") + ": " + Kind(e));
                foreach (FooterReference f in letzte.Elements<FooterReference>())
                    if (main.GetPartById(f.Id) is FooterPart fp && fp.Footer != null)
                        foreach (OpenXmlElement e in fp.Footer.ChildElements)
                            liste.Add("Fußzeile " + (f.Type?.InnerText ?? "default") + ": " + Kind(e));
            }
            if (liste.Count == vorher) liste.Add("(keine)");

            liste.Add("## Rumpf");
            foreach (OpenXmlElement e in body.ChildElements)
                liste.Add(Kind(e));
            return liste;
        }

        /// <summary>Eine Zeile je Kind des Rumpfs (oder einer Kopf- bzw. Fußzeile).</summary>
        private static string Kind(OpenXmlElement e)
        {
            switch (e)
            {
                case Paragraph p: return Absatz(p);
                case Table t: return Tabelle(t);
                case SectionProperties s: return Abschnitt(s);
                case SdtBlock sdt: return "Inhaltssteuerelement " + Normalisiere(Leerraum.Replace(sdt.InnerText, " ").Trim());
                default: return "Element " + e.Prefix + ":" + e.LocalName;
            }
        }

        private static string Absatz(Paragraph p)
        {
            string stil = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            var leser = new Absatzleser();
            leser.Lies(p);
            string text = Normalisiere(leser.Inhalt.ToString()).Trim();
            bool leer = text.Length == 0;

            if (leer && leser.Bilder.Count == 0 && leser.Seitenumbruch && stil == null) return "Seitenumbruch";
            if (leer && leser.Bilder.Count > 0)
                return string.Join(" + ", leser.Bilder) + (stil != null ? " [" + stil + "]" : "") +
                       (leser.Seitenumbruch ? " + Seitenumbruch" : "");
            if (stil == null && leser.Bilder.Count == 0 && !leser.Seitenumbruch &&
                text.StartsWith("{TOC ", StringComparison.Ordinal) && text.EndsWith("}", StringComparison.Ordinal) &&
                text.IndexOf('{', 1) < 0)
                return "TOC-Feld " + text.Substring(1, text.Length - 2);

            return "Absatz [" + (stil ?? "—") + "] " + (leer ? "(leer)" : text) +
                   (leser.Bilder.Count > 0 ? " + " + string.Join(" + ", leser.Bilder) : "") +
                   (leser.Seitenumbruch ? " + Seitenumbruch" : "");
        }

        /// <summary>
        /// Eine Tabelle: Spalten laut Raster, Zeilen, die Zellen der ersten Zeile. Eine Zelle
        /// der ersten Zeile, die NICHT fett ist, ist eine Datenzelle (die Wertspalte einer
        /// Eigenschaftstabelle, etwa „Wärmebedarf gesamt | 6.138 MWh/a“) — ihre Zahlen stehen
        /// als <c>#</c> wie in der Mappe, damit die Messlatte am Gerüst hängt und nicht am
        /// Simulationsergebnis.
        /// </summary>
        private static string Tabelle(Table t)
        {
            int spalten = t.GetFirstChild<TableGrid>()?.Elements<GridColumn>().Count() ?? 0;
            List<TableRow> zeilen = t.Elements<TableRow>().ToList();
            string kopf = zeilen.Count == 0
                ? ""
                : string.Join(" | ", zeilen[0].Elements<TableCell>()
                                             .Select(c => IstFett(c) ? Zelltext(c) : Zahlmuster.Replace(Zelltext(c), "#")));
            return "Tabelle " + spalten + " Sp. × " + zeilen.Count + " Z. | Kopf: " + kopf;
        }

        /// <summary>Eine Zahl mit Vorzeichen und Gruppen- bzw. Dezimaltrennern.</summary>
        private static readonly Regex Zahlmuster = new Regex(@"[-+−±]?\d+(?:[.,]\d+)*",
                                                             RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Fett = jeder Textlauf der Zelle trägt <c>w:b</c> (nicht ausgeschaltet).</summary>
        private static bool IstFett(TableCell c)
        {
            List<Run> laeufe = c.Descendants<Run>().Where(r => r.GetFirstChild<Text>() != null).ToList();
            return laeufe.Count > 0 && laeufe.All(r =>
            {
                Bold b = r.RunProperties?.GetFirstChild<Bold>();
                return b != null && (b.Val == null || b.Val.Value);
            });
        }

        private static string Zelltext(TableCell c)
        {
            var teile = new List<string>();
            foreach (Paragraph p in c.Elements<Paragraph>())
            {
                var leser = new Absatzleser();
                leser.Lies(p);
                string text = Normalisiere(leser.Inhalt.ToString()).Trim();
                if (leser.Bilder.Count > 0) text = (text + " " + string.Join(" + ", leser.Bilder)).Trim();
                teile.Add(text);
            }
            return string.Join(" ¶ ", teile);
        }

        private static string Abschnitt(SectionProperties s)
        {
            PageSize ps = s.GetFirstChild<PageSize>();
            PageMargin pm = s.GetFirstChild<PageMargin>();
            string lage = ps?.Orient?.InnerText == "landscape" ? "quer" : "hoch";
            string groesse = ps == null ? "—" : ps.Width?.Value + "×" + ps.Height?.Value + " " + lage;
            string raender = pm == null ? "—" : pm.Top?.Value + "/" + pm.Right?.Value + "/" + pm.Bottom?.Value + "/" + pm.Left?.Value;
            string kopf = string.Join(",", s.Elements<HeaderReference>().Select(h => h.Type?.InnerText ?? "default"));
            string fuss = string.Join(",", s.Elements<FooterReference>().Select(f => f.Type?.InnerText ?? "default"));
            return "Abschnitt " + groesse + " · Ränder o/r/u/l " + raender +
                   " · Kopf " + (kopf.Length > 0 ? kopf : "—") + " · Fuß " + (fuss.Length > 0 ? fuss : "—");
        }

        /// <summary>
        /// Liest einen Absatz in Dokumentreihenfolge: Text, Tabulatoren, Umbrüche, Bilder und
        /// Felder. Ein komplexes Feld (<c>w:fldChar</c>) und ein einfaches (<c>w:fldSimple</c>)
        /// stehen als <c>{CODE}</c>; ihr Ergebnis wird nicht gelesen — es ist der Platzhalter
        /// bis zur Aktualisierung in Word und trüge sonst z. B. das Datum des Laufs.
        /// </summary>
        private sealed class Absatzleser
        {
            internal readonly StringBuilder Inhalt = new StringBuilder();
            internal readonly List<string> Bilder = new List<string>();
            internal bool Seitenumbruch;

            private int _feldTiefe;
            private StringBuilder _code;

            internal void Lies(OpenXmlElement element)
            {
                foreach (OpenXmlElement k in element.ChildElements)
                {
                    switch (k)
                    {
                        case ParagraphProperties _:
                        case RunProperties _:
                            break;
                        case SimpleField sf:
                            if (_feldTiefe == 0) Inhalt.Append('{').Append(Code(sf.Instruction?.Value)).Append('}');
                            break;
                        case FieldChar fc:
                            Feldzeichen(fc);
                            break;
                        case FieldCode code:
                            if (_feldTiefe == 1 && _code != null) _code.Append(code.Text);
                            break;
                        case Text t:
                            if (_feldTiefe == 0) Inhalt.Append(t.Text);
                            break;
                        case TabChar _:
                            if (_feldTiefe == 0) Inhalt.Append('⇥');
                            break;
                        case Break br:
                            if (br.Type != null && br.Type.Value == BreakValues.Page) Seitenumbruch = true;
                            else if (_feldTiefe == 0) Inhalt.Append('↵');
                            break;
                        case Drawing d:
                            Bilder.Add(Bild(d));
                            break;
                        default:
                            Lies(k);
                            break;
                    }
                }
            }

            private void Feldzeichen(FieldChar fc)
            {
                FieldCharValues? art = fc.FieldCharType?.Value;
                if (art == FieldCharValues.Begin)
                {
                    _feldTiefe++;
                    if (_feldTiefe == 1) _code = new StringBuilder();
                }
                else if (art == FieldCharValues.Separate)
                {
                    SchreibeCode();
                }
                else if (art == FieldCharValues.End)
                {
                    SchreibeCode();
                    if (_feldTiefe > 0) _feldTiefe--;
                }
            }

            private void SchreibeCode()
            {
                if (_feldTiefe != 1 || _code == null) return;
                Inhalt.Append('{').Append(Code(_code.ToString())).Append('}');
                _code = null;
            }

            private static string Code(string code) => Leerraum.Replace(code ?? "", " ").Trim();
        }

        private static string Bild(Drawing d)
        {
            DW.Extent ausdehnung = d.Descendants<DW.Extent>().FirstOrDefault();
            long cx = ausdehnung?.Cx?.Value ?? 0L;
            long cy = ausdehnung?.Cy?.Value ?? 0L;
            bool svg = d.Descendants<ASVG.SVGBlip>().Any();
            return "Bild " + Pixel(cx) + "×" + Pixel(cy) + " px, SVG " + (svg ? "ja" : "nein");
        }

        /// <summary>EMU in Pixel bei 96 dpi (1 px = 9 525 EMU).</summary>
        private static long Pixel(long emu) => (long)Math.Round(emu / 9525.0, MidpointRounding.AwayFromZero);

        // =====================================================================
        //  Tabellenbericht
        // =====================================================================

        /// <summary>Die Strukturliste der Mappe <paramref name="xlsxPfad"/>.</summary>
        internal static List<string> Excel(string xlsxPfad)
        {
            var liste = new List<string>();
            using var wb = new XLWorkbook(xlsxPfad);
            List<IXLWorksheet> blaetter = wb.Worksheets.OrderBy(w => w.Position).ToList();
            liste.Add("## Mappe · " + blaetter.Count + " Blätter");

            var namen = new List<string>();
            foreach (IXLDefinedName n in wb.DefinedNames)
                namen.Add(n.Name + " = " + n.RefersTo);

            foreach (IXLWorksheet ws in blaetter)
            {
                int zeilen = ws.LastRowUsed()?.RowNumber() ?? 0;
                int spalten = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
                liste.Add("## Blatt " + ws.Position + " „" + ws.Name + "“ · " + zeilen + " Z. × " + spalten + " Sp." +
                          " · fixiert " + ws.SheetView.SplitRow + "/" + ws.SheetView.SplitColumn +
                          " · Autofilter " + (ws.AutoFilter.IsEnabled ? ws.AutoFilter.Range.RangeAddress.ToString() : "—") +
                          " · verbunden " + ws.MergedRanges.Count());

                for (int r = 1; r <= zeilen; r++)
                {
                    List<IXLCell> zellen = ws.Row(r).CellsUsed(XLCellsUsedOptions.Contents).ToList();
                    if (zellen.Count == 0) continue;
                    if (r > ERSTE_ZEILEN && !zellen.Any(c => c.Style.Font.Bold)) continue;
                    liste.Add("Z" + r + ": " + string.Join(" | ",
                        zellen.Select(c => c.Address.ColumnLetter + "=" + Zelle(c))));
                }

                foreach (IXLDefinedName n in ws.DefinedNames)
                    namen.Add(ws.Name + "!" + n.Name + " = " + n.RefersTo);
            }

            namen.Sort(StringComparer.Ordinal);
            liste.Add("## Namen · " + namen.Count);
            liste.AddRange(namen);

            // BV-E8: die Excel-Diagramme — Blatt, Bildschlüssel, Titel, Arten und je Reihe der Bezug ihrer Werte.
            List<Exceldiagrammbefund> diagramme = Exceldiagrammbefund.Lies(xlsxPfad);
            liste.Add("## Diagramme · " + diagramme.Count);
            foreach (Exceldiagrammbefund d in diagramme)
                liste.Add(d.Blatt + " · " + d.Name + " · " + d.Titel + " · " + string.Join("+", d.Arten.Distinct()) + " · " +
                          string.Join(" ", d.Reihen.Select(r => r.WerteBezug)));
            return liste;
        }

        private static string Zelle(IXLCell c)
        {
            if (c.HasFormula) return "=" + c.FormulaA1;
            XLCellValue v = c.Value;
            if (v.IsNumber) return "#";
            if (v.IsDateTime || v.IsTimeSpan) return DATUM;
            if (v.IsBoolean) return v.GetBoolean() ? "WAHR" : "FALSCH";
            if (v.IsError) return v.GetError().ToString();
            if (v.IsText) return Normalisiere(v.GetText()).Replace("\r\n", "↵").Replace('\n', '↵');
            return "";
        }
    }
}
