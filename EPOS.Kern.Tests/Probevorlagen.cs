using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.VariantTypes;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Baut Word-Vorlagen für die Prüfer- und Controllertests (BV-E1 A3) im Speicher, über das
    /// OpenXML SDK — Absätze, Tabellen, Kopfzeilen, Fußnoten, Kommentare, Stile, Eigenschaften,
    /// Rohes XML für Textfelder und Zweige. Keine Datei, keine Datenbank, kein Dienst.
    /// </summary>
    internal static class Probevorlagen
    {
        /// <summary>Namensräume für Absätze aus rohem XML.</summary>
        internal const string NS =
            "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" " +
            "xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" " +
            "xmlns:wps=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\" " +
            "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" " +
            "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
            "xmlns:v=\"urn:schemas-microsoft-com:vml\"";

        /// <summary>Baut eine Vorlage: <paramref name="rumpf"/> füllt den Rumpf (ohne Abschnittsangabe, sie kommt zuletzt).</summary>
        internal static byte[] Baue(Action<Bau> rumpf,
                                    WordprocessingDocumentType typ = WordprocessingDocumentType.Document)
        {
            using (var strom = new MemoryStream())
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Create(strom, typ))
                {
                    MainDocumentPart main = doc.AddMainDocumentPart();
                    var body = new W.Body();
                    main.Document = new W.Document(body);
                    var bau = new Bau(doc, main, body);
                    rumpf?.Invoke(bau);
                    body.Append(bau.Abschnitt);
                    main.Document.Save();
                }
                return strom.ToArray();
            }
        }

        /// <summary>Eine Vorlage aus Absätzen mit je einem Text.</summary>
        internal static byte[] AusAbsaetzen(params string[] texte)
        {
            return Baue(b => { foreach (string t in texte) b.Absatz(t); });
        }

        /// <summary>Ein Absatz aus Runs mit je einem Text.</summary>
        internal static W.Paragraph Absatz(params string[] runs)
        {
            return new W.Paragraph(runs.Select(r => new W.Run(new W.Text(r) { Space = SpaceProcessingModeValues.Preserve })));
        }

        /// <summary>Ein Stil mit Namen und wahlweise Gliederungsebene (0-basiert).</summary>
        internal static W.Style Stil(string id, string name, int? gliederungsebene, bool standard = false)
        {
            var stil = new W.Style { Type = W.StyleValues.Paragraph, StyleId = id };
            if (standard) stil.Default = true;
            stil.Append(new W.StyleName { Val = name });
            if (gliederungsebene != null)
                stil.Append(new W.StyleParagraphProperties(new W.OutlineLevel { Val = gliederungsebene.Value }));
            return stil;
        }

        /// <summary>Der Bauplatz einer Vorlage.</summary>
        internal sealed class Bau
        {
            internal Bau(WordprocessingDocument doc, MainDocumentPart main, W.Body body)
            {
                Doc = doc;
                Main = main;
                Body = body;
            }

            internal WordprocessingDocument Doc { get; }

            internal MainDocumentPart Main { get; }

            internal W.Body Body { get; }

            /// <summary>Die Abschnittsangabe am Ende des Rumpfs (Kopf- und Fußzeilenverweise).</summary>
            internal W.SectionProperties Abschnitt { get; } = new W.SectionProperties();

            /// <summary>Ein Absatz aus Runs.</summary>
            internal Bau Absatz(params string[] runs)
            {
                Body.Append(Probevorlagen.Absatz(runs));
                return this;
            }

            /// <summary>Ein beliebiges Element im Rumpf.</summary>
            internal Bau Element(OpenXmlElement element)
            {
                Body.Append(element);
                return this;
            }

            /// <summary>Ein Absatz aus rohem XML (Namensräume aus <see cref="NS"/> werden ergänzt).</summary>
            internal Bau Roh(string innenXml)
            {
                Body.Append(new W.Paragraph("<w:p " + NS + ">" + innenXml + "</w:p>"));
                return this;
            }

            /// <summary>Eine Tabelle: je Zeile die Zellentexte; <paramref name="spanne"/> setzt gridSpan 2 auf (Zeile, Zelle), 1-basiert.</summary>
            internal Bau Tabelle(string[][] zeilen, (int Zeile, int Zelle)? spanne = null)
            {
                var tabelle = new W.Table(new W.TableProperties(new W.TableWidth { Width = "5000", Type = W.TableWidthUnitValues.Pct }));
                int spalten = zeilen.Max(z => z.Length);
                var raster = new W.TableGrid();
                for (int i = 0; i < spalten + 1; i++) raster.Append(new W.GridColumn { Width = "1000" });
                tabelle.Append(raster);
                for (int r = 0; r < zeilen.Length; r++)
                {
                    var zeile = new W.TableRow();
                    for (int c = 0; c < zeilen[r].Length; c++)
                    {
                        var zelle = new W.TableCell();
                        if (spanne.HasValue && spanne.Value.Zeile == r + 1 && spanne.Value.Zelle == c + 1)
                            zelle.Append(new W.TableCellProperties(new W.GridSpan { Val = 2 }));
                        zelle.Append(Probevorlagen.Absatz(zeilen[r][c]));
                        zeile.Append(zelle);
                    }
                    tabelle.Append(zeile);
                }
                Body.Append(tabelle);
                return this;
            }

            /// <summary>Eine Kopfzeile des (einzigen) Abschnitts.</summary>
            internal Bau Kopfzeile(params W.Paragraph[] absaetze)
            {
                HeaderPart teil = Main.AddNewPart<HeaderPart>();
                teil.Header = new W.Header(absaetze.Cast<OpenXmlElement>());
                Abschnitt.Append(new W.HeaderReference { Type = W.HeaderFooterValues.Default, Id = Main.GetIdOfPart(teil) });
                return this;
            }

            /// <summary>Eine Fußzeile des (einzigen) Abschnitts.</summary>
            internal Bau Fusszeile(params W.Paragraph[] absaetze)
            {
                FooterPart teil = Main.AddNewPart<FooterPart>();
                teil.Footer = new W.Footer(absaetze.Cast<OpenXmlElement>());
                Abschnitt.Append(new W.FooterReference { Type = W.HeaderFooterValues.Default, Id = Main.GetIdOfPart(teil) });
                return this;
            }

            /// <summary>Fußnoten: die Trennlinie (wird übergangen) und je Text eine Fußnote mit Verweis im Rumpf.</summary>
            internal Bau Fussnoten(params string[] texte)
            {
                FootnotesPart teil = Main.AddNewPart<FootnotesPart>();
                var noten = new W.Footnotes(
                    new W.Footnote(Probevorlagen.Absatz("{{projekt.trennlinie}}")) { Type = W.FootnoteEndnoteValues.Separator, Id = -1 },
                    new W.Footnote(Probevorlagen.Absatz("")) { Type = W.FootnoteEndnoteValues.ContinuationSeparator, Id = 0 });
                for (int i = 0; i < texte.Length; i++)
                {
                    noten.Append(new W.Footnote(Probevorlagen.Absatz(texte[i])) { Id = i + 1 });
                    Body.Append(new W.Paragraph(new W.Run(new W.Text("Verweis")), new W.Run(new W.FootnoteReference { Id = i + 1 })));
                }
                teil.Footnotes = noten;
                return this;
            }

            /// <summary>Kommentare mit je einem Text (sie werden weder geprüft noch ersetzt).</summary>
            internal Bau Kommentare(params string[] texte)
            {
                WordprocessingCommentsPart teil = Main.AddNewPart<WordprocessingCommentsPart>();
                teil.Comments = new W.Comments(texte.Select((t, i) =>
                    new W.Comment(Probevorlagen.Absatz(t)) { Id = i.ToString(), Author = "Probe", Initials = "P" }));
                return this;
            }

            /// <summary>Stile.</summary>
            internal Bau Stile(params W.Style[] stile)
            {
                StyleDefinitionsPart teil = Main.AddNewPart<StyleDefinitionsPart>();
                teil.Styles = new W.Styles(stile.Cast<OpenXmlElement>());
                return this;
            }

            /// <summary>Die Überschriften 1 bis 3 mit deutschen Stil-IDs, wie ein deutsches Word sie schreibt.</summary>
            internal Bau DeutscheUeberschriften(bool dritteMitEbene = true)
            {
                return Stile(Stil("Standard", "Normal", null, standard: true),
                             Stil("berschrift1", "heading 1", 0),
                             Stil("berschrift2", "heading 2", 1),
                             Stil("berschrift3", "heading 3", dritteMitEbene ? 2 : (int?)null));
            }

            /// <summary>Eigenschaften in <c>custom.xml</c>: Katalogfassung und Sprache (je <c>null</c> = weglassen).</summary>
            internal Bau Eigenschaften(int? katalogfassung, string sprache)
            {
                CustomFilePropertiesPart teil = Doc.AddCustomFilePropertiesPart();
                var eigenschaften = new Properties();
                int id = 2;
                if (katalogfassung.HasValue)
                    eigenschaften.Append(new CustomDocumentProperty(new VTInt32(katalogfassung.Value.ToString()))
                    { FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", PropertyId = id++, Name = "EPOS.Katalogfassung" });
                if (sprache != null)
                    eigenschaften.Append(new CustomDocumentProperty(new VTLPWSTR(sprache))
                    { FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", PropertyId = id, Name = "EPOS.Sprache" });
                teil.Properties = eigenschaften;
                return this;
            }
        }

        /// <summary>Ein Textfeld in beiden Zweigen von <c>mc:AlternateContent</c> (Word 2010 und VML), je mit <paramref name="text"/>.</summary>
        internal static string TextfeldXml(string text)
        {
            return "<w:r><mc:AlternateContent>" +
                   "<mc:Choice Requires=\"wps\"><w:drawing>" +
                   "<wp:anchor distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\" simplePos=\"0\" relativeHeight=\"1\" behindDoc=\"0\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\">" +
                   "<wp:simplePos x=\"0\" y=\"0\"/><wp:positionH relativeFrom=\"column\"><wp:posOffset>0</wp:posOffset></wp:positionH>" +
                   "<wp:positionV relativeFrom=\"paragraph\"><wp:posOffset>0</wp:posOffset></wp:positionV>" +
                   "<wp:extent cx=\"1000000\" cy=\"500000\"/><wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/><wp:wrapNone/>" +
                   "<wp:docPr id=\"5\" name=\"Textfeld 5\"/><wp:cNvGraphicFramePr/>" +
                   "<a:graphic><a:graphicData uri=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\">" +
                   "<wps:wsp><wps:cNvSpPr txBox=\"1\"/><wps:spPr/><wps:txbx><w:txbxContent><w:p><w:r><w:t xml:space=\"preserve\">" + text +
                   "</w:t></w:r></w:p></w:txbxContent></wps:txbx><wps:bodyPr/></wps:wsp>" +
                   "</a:graphicData></a:graphic></wp:anchor></w:drawing></mc:Choice>" +
                   "<mc:Fallback><w:pict><v:shape style=\"width:100pt;height:50pt\"><v:textbox><w:txbxContent><w:p><w:r><w:t xml:space=\"preserve\">" + text +
                   "</w:t></w:r></w:p></w:txbxContent></v:textbox></v:shape></w:pict></mc:Fallback>" +
                   "</mc:AlternateContent></w:r>";
        }

        /// <summary>Ein Bild (Zeichnungsanker ohne Bildteil) mit Alternativtext.</summary>
        internal static string BildXml(string name, string beschreibung)
        {
            return "<w:r><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\"><wp:extent cx=\"100000\" cy=\"100000\"/>" +
                   "<wp:docPr id=\"7\" name=\"" + name + "\" descr=\"" + beschreibung + "\"/>" +
                   "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"/></a:graphic>" +
                   "</wp:inline></w:drawing></w:r>";
        }

        /// <summary>Ein Temp-Ordner für einen Test.</summary>
        internal static string TempOrdner(string vorsilbe)
        {
            string ordner = Path.Combine(Path.GetTempPath(), vorsilbe + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            return ordner;
        }

        /// <summary>Räumt einen Temp-Ordner weg; Scheitern ist still.</summary>
        internal static void Aufraeumen(string ordner)
        {
            try
            {
                if (!Directory.Exists(ordner)) return;
                foreach (string datei in Directory.EnumerateFiles(ordner, "*", SearchOption.AllDirectories))
                    File.SetAttributes(datei, FileAttributes.Normal);
                Directory.Delete(ordner, true);
            }
            catch (Exception)
            {
                // Aufräumen darf nicht scheitern.
            }
        }

        /// <summary>Die Kennungen der Meldungen eines Befunds, für Zusicherungen und Fehlertexte.</summary>
        internal static string Liste(WindowsFormsApplication1.Pruefbefund befund)
        {
            return string.Join("\n", befund.Meldungen.Select(m => m.ToString()));
        }

        /// <summary>Die Meldungen einer Kennung.</summary>
        internal static List<WindowsFormsApplication1.Pruefmeldung> Mit(WindowsFormsApplication1.Pruefbefund befund, string kennung)
        {
            return befund.Meldungen.Where(m => m.Kennung == kennung).ToList();
        }
    }
}
