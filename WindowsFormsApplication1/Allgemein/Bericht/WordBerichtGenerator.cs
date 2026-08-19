using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Word-Erzeugung des Variantenberichts über das OpenXML SDK (Konzept Kap. 4/8).
    ///
    /// Grundlage ist die Rahmen-/Stylevorlage Vorlagen\Berichtsvorlage.docx
    /// (Styles: Title, Subtitle, Heading1–3, Normal, Hinweis, Beschriftung; Kopfzeile
    /// mit Logo, Fußzeile mit Seitenfeldern). Fehlt die Vorlage, wird das Dokument
    /// mit programmatisch angelegten Ersatz-Styles erzeugt — der Bericht entsteht
    /// in jedem Fall. Die Kapitel schreiben IBerichtsBaustein-Implementierungen
    /// über den WordKontext; dieser Generator kennt Rahmen, Styles und Tabellenbau.
    /// </summary>
    public class WordBerichtGenerator
    {
        /// <summary>Nutzbare Inhaltsbreite in DXA (A4, Ränder 25/20 mm → 165 mm).</summary>
        public const int INHALT_B = 9355;

        public const string HEAD_FILL = "D9E1F2";
        public const string STAMM_FILL = "F2F2F2";
        public const string RAHMEN = "BFBFBF";

        /// <summary>Schriftgröße der Tabellenzellen in Halbpunkten (9 pt).</summary>
        public const int SCHRIFT_TABELLE = 18;

        /// <summary>
        /// Schriftgröße für breite Zahlentabellen in Halbpunkten (7 pt, Etappe E7) —
        /// die Mehrjahresübersicht führt bis zu dreizehn Spalten auf A4 hoch.
        /// </summary>
        public const int SCHRIFT_TABELLE_SCHMAL = 14;

        /// <summary>
        /// Erzeugt den Bericht. Rückgabe: Pfad der geschriebenen Datei.
        /// </summary>
        public string Erzeuge(BerichtsDaten daten, BerichtsKonfiguration konfig, string zielDatei)
        {
            if (daten == null || daten.Varianten.Count == 0)
                throw new ArgumentException("Keine Berichtsdaten vorhanden.");

            string vorlage = FindeVorlage();
            if (vorlage != null) File.Copy(vorlage, zielDatei, true);

            using (WordprocessingDocument doc = vorlage != null
                ? WordprocessingDocument.Open(zielDatei, true)
                : WordprocessingDocument.Create(zielDatei, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.MainDocumentPart;
                if (main == null)
                {
                    main = doc.AddMainDocumentPart();
                    main.Document = new Document(new Body());
                }
                if (main.Document == null) main.Document = new Document(new Body());
                Body body = main.Document.Body ?? main.Document.AppendChild(new Body());

                if (vorlage == null) ErgaenzeErsatzStyles(main);

                // Body leeren — die SectionProperties (Kopf-/Fußzeilen-Verweise, Ränder)
                // der Vorlage bleiben unangetastet; Inhalte werden davor eingefügt.
                SectionProperties sect = body.Elements<SectionProperties>().LastOrDefault();
                foreach (OpenXmlElement el in body.ChildElements.Where(c => !(c is SectionProperties)).ToList())
                    el.Remove();

                var kontext = new WordKontext(main, body, sect);

                // Felder (Inhaltsverzeichnis, Datum, Seitenzahlen) beim Öffnen aktualisieren.
                SetzeUpdateFields(main);

                foreach (IBerichtsBaustein baustein in AktiveBausteine(konfig))
                    baustein.SchreibeWord(kontext, daten, konfig);

                main.Document.Save();
            }
            return zielDatei;
        }

        /// <summary>Bausteine in Berichtsreihenfolge, gefiltert auf die aktive Auswahl.</summary>
        public static List<IBerichtsBaustein> AktiveBausteine(BerichtsKonfiguration konfig)
        {
            var alle = new List<IBerichtsBaustein>
            {
                new DeckblattBaustein(),
                new InhaltsverzeichnisBaustein(),
                new ProjektbeschreibungBaustein(),
                new KomponentenBaustein(),
                new ErgebnisseBaustein(),
                new VergleichBaustein(),
                new WirtschaftlichkeitBaustein(),   // Phase 6: liest Tab_ErgebnisWirtschaftlichkeit
                new AnhangBaustein(),
            };
            return alle.Where(b => konfig == null || konfig.IstAktiv(b.Schluessel)).ToList();
        }

        // ------------------------------------------------------------- Vorlage

        /// <summary>Sucht die Berichtsvorlage an den bekannten Orten (null = nicht gefunden).</summary>
        public static string FindeVorlage()
        {
            string basis = AppDomain.CurrentDomain.BaseDirectory ?? "";
            string[] kandidaten =
            {
                Path.Combine(basis, "Vorlagen", "Berichtsvorlage.docx"),
                Path.Combine(basis, "Allgemein", "Bericht", "Vorlagen", "Berichtsvorlage.docx"),
            };
            foreach (string k in kandidaten)
                if (File.Exists(k)) return k;
            return null;
        }

        private static void SetzeUpdateFields(MainDocumentPart main)
        {
            DocumentSettingsPart sp = main.DocumentSettingsPart ?? main.AddNewPart<DocumentSettingsPart>();
            if (sp.Settings == null) sp.Settings = new Settings();
            sp.Settings.RemoveAllChildren<UpdateFieldsOnOpen>();
            sp.Settings.PrependChild(new UpdateFieldsOnOpen { Val = true });
            sp.Settings.Save();
        }

        // Minimale Ersatz-Styles, falls die Vorlage fehlt (gleiche Style-IDs wie die Vorlage).
        private static void ErgaenzeErsatzStyles(MainDocumentPart main)
        {
            StyleDefinitionsPart part = main.StyleDefinitionsPart ?? main.AddNewPart<StyleDefinitionsPart>();
            if (part.Styles == null) part.Styles = new Styles();

            part.Styles.Append(ErsatzStyle("Normal", null, 21, false, null, true));
            part.Styles.Append(ErsatzStyle("Title", "Normal", 56, true, "1F4E79", false));
            part.Styles.Append(ErsatzStyle("Subtitle", "Normal", 28, false, "595959", false));
            part.Styles.Append(ErsatzStyle("Heading1", "Normal", 30, true, "1F4E79", false));
            part.Styles.Append(ErsatzStyle("Heading2", "Normal", 25, true, "1F4E79", false));
            part.Styles.Append(ErsatzStyle("Heading3", "Normal", 22, true, "595959", false));
            part.Styles.Append(ErsatzStyle("Hinweis", "Normal", 18, false, "595959", false));
            part.Styles.Append(ErsatzStyle("Beschriftung", "Normal", 18, false, "595959", false));
            part.Styles.Save();
        }

        private static Style ErsatzStyle(string id, string basedOn, int sizeHalf, bool bold, string farbe, bool standard)
        {
            var s = new Style { Type = StyleValues.Paragraph, StyleId = id, Default = standard };
            s.Append(new StyleName { Val = id });
            if (basedOn != null) s.Append(new BasedOn { Val = basedOn });
            var rp = new StyleRunProperties();
            rp.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
            if (bold) rp.Append(new Bold());
            if (farbe != null) rp.Append(new Color { Val = farbe });
            rp.Append(new FontSize { Val = sizeHalf.ToString() });
            s.Append(rp);
            return s;
        }
    }

    // =======================================================================
    /// <summary>
    /// Schreib-API für die Bausteine: Style-basierte Absätze, Tabellenbau mit den
    /// Berichtskonstanten, Zahlformatierung (Berichtssprache de, Phase 5: UI-Sprache),
    /// Blocksplitting-Hilfen für unbegrenzt viele Varianten (Konzept Kap. 5.1).
    /// </summary>
    public class WordKontext
    {
        public readonly MainDocumentPart Main;
        public readonly Body Body;
        private readonly SectionProperties _sect;

        /// <summary>Kultur der Berichtssprache (= UI-Sprache; BerichtTexte, Phase 5).</summary>
        public readonly CultureInfo Kultur = BerichtTexte.Kultur;

        /// <summary>Max. Varianten je Tabellenblock (A4 hoch; Stamm-Spalte wird je Block wiederholt).</summary>
        public const int MAX_VARIANTEN_JE_BLOCK = 3;

        public WordKontext(MainDocumentPart main, Body body, SectionProperties sect)
        { Main = main; Body = body; _sect = sect; }

        /// <summary>Fügt ein Element vor den SectionProperties ein (Kopf-/Fußzeile bleiben erhalten).</summary>
        public void Fuege(OpenXmlElement el)
        {
            if (_sect != null) Body.InsertBefore(el, _sect);
            else Body.Append(el);
        }

        // ------------------------------------------------------------- Absätze

        public void MitStil(string styleId, string text)
        {
            // Berichtssprache: bekannte Texte werden übersetzt, dynamische laufen durch.
            text = BerichtTexte.T(text);
            MitStilRoh(styleId, text);
        }

        /// <summary>
        /// Absatz OHNE <c>BerichtTexte.T()</c> — für Texte, die bereits aus
        /// <c>MyResource</c> kommen und damit schon in der Berichtssprache stehen
        /// (Etappe E7).
        ///
        /// <para><b>Warum das nötig ist.</b> <c>T()</c> ist ein Wörterbuch Deutsch → Englisch;
        /// ein bereits übersetzter Text läuft heute nur deshalb unverändert durch, weil er
        /// darin nicht vorkommt. Sobald ein deutscher <c>MyResource</c>-Wert einmal ins
        /// Wörterbuch gerät, übersetzt die Kette doppelt. Neue und umgestellte Texte gehen
        /// deshalb diesen Weg.</para>
        /// </summary>
        public void MitStilRoh(string styleId, string text)
        {
            var p = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = styleId }));
            p.Append(new Run(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
            Fuege(p);
        }

        public void Titel(string t) { MitStil("Title", t); }
        public void Untertitel(string t) { MitStil("Subtitle", t); }
        public void Ueberschrift1(string t) { MitStil("Heading1", t); }
        public void Ueberschrift2(string t) { MitStil("Heading2", t); }
        public void Ueberschrift3(string t) { MitStil("Heading3", t); }
        public void Text(string t) { MitStil("Normal", t); }
        public void Hinweis(string t) { MitStil("Hinweis", t); }
        public void Beschriftung(string t) { MitStil("Beschriftung", t); }

        /// <inheritdoc cref="MitStilRoh"/>
        public void Ueberschrift2Roh(string t) { MitStilRoh("Heading2", t); }
        /// <inheritdoc cref="MitStilRoh"/>
        public void Ueberschrift3Roh(string t) { MitStilRoh("Heading3", t); }
        /// <inheritdoc cref="MitStilRoh"/>
        public void HinweisRoh(string t) { MitStilRoh("Hinweis", t); }
        /// <inheritdoc cref="MitStilRoh"/>
        public void TextRoh(string t) { MitStilRoh("Normal", t); }

        public void Seitenumbruch()
        { Fuege(new Paragraph(new Run(new Break { Type = BreakValues.Page }))); }

        /// <summary>Inhaltsverzeichnis-Feld (Word aktualisiert beim Öffnen; UpdateFieldsOnOpen ist gesetzt).</summary>
        public void TocFeld()
        {
            var p = new Paragraph();
            p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            p.Append(new Run(new FieldCode(" TOC \\o \"1-3\" \\h \\z \\u ") { Space = SpaceProcessingModeValues.Preserve }));
            p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            p.Append(new Run(new Text("Das Inhaltsverzeichnis wird beim Öffnen in Word aktualisiert.")));
            p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
            Fuege(p);
        }

        // ------------------------------------------------------------- Bilder

        private uint _bildId = 1;

        /// <summary>
        /// Bettet ein PNG als Inline-Grafik ein (Anzeigegröße in Pixel bei 96 dpi;
        /// gerendert wird in doppelter Auflösung → scharfer Druck). Portierung der
        /// BildDrawing-Logik aus dem Bestandsbericht. png == null wird ignoriert.
        /// </summary>
        public void Bild(byte[] png, int anzeigeBreitePx, int anzeigeHoehePx)
        {
            if (png == null || png.Length == 0) return;

            ImagePart imgPart = Main.AddImagePart(ImagePartType.Png);
            using (var ms = new System.IO.MemoryStream(png)) imgPart.FeedData(ms);
            string relId = Main.GetIdOfPart(imgPart);

            long cx = anzeigeBreitePx * 9525L;   // 1 px @96dpi = 9525 EMU
            long cy = anzeigeHoehePx * 9525L;
            uint id = _bildId++;

            var drawing = new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = cx, Cy = cy },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = id, Name = "Diagramm" + id },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Diagramm" + id + ".png" },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = relId },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = cx, Cy = cy }),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
                { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

            Fuege(new Paragraph(new Run(drawing)));
        }

        // ------------------------------------------------------------- Tabellen

        public Table NeueTabelle(int[] breiten)
        {
            var t = new Table();
            var tp = new TableProperties();
            tp.Append(new TableWidth { Type = TableWidthUnitValues.Dxa, Width = breiten.Sum().ToString() });
            tp.Append(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new RightBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN }));
            t.Append(tp);
            var grid = new TableGrid();
            foreach (int b in breiten) grid.Append(new GridColumn { Width = b.ToString() });
            t.Append(grid);
            return t;
        }

        public TableCell Zelle(string text, int breite, bool fett, string fill, JustificationValues just)
        {
            return Zelle(text, breite, fett, fill, just, true,
                         WordBerichtGenerator.SCHRIFT_TABELLE);
        }

        /// <param name="uebersetzen">
        /// false = der Text kommt bereits aus <c>MyResource</c> und darf nicht noch
        /// einmal durch <c>BerichtTexte.T()</c> laufen (Etappe E7, siehe
        /// <see cref="MitStilRoh"/>).
        /// </param>
        /// <param name="schriftHalb">Schriftgröße in Halbpunkten.</param>
        public TableCell Zelle(string text, int breite, bool fett, string fill,
                               JustificationValues just, bool uebersetzen, int schriftHalb)
        {
            // Kopf-/Labelzellen (fett) durch die Berichtssprache übersetzen;
            // Datenzellen (nicht fett) bleiben unangetastet.
            if (fett && uebersetzen) text = BerichtTexte.T(text);
            var tc = new TableCell();
            var tcp = new TableCellProperties();
            tcp.Append(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = breite.ToString() });
            if (fill != null) tcp.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fill });
            tcp.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            tc.Append(tcp);

            var p = new Paragraph();
            var pp = new ParagraphProperties();
            pp.Append(new SpacingBetweenLines { Before = "20", After = "20" });
            pp.Append(new Justification { Val = just });
            p.Append(pp);
            var rp = new RunProperties();
            if (fett) rp.Append(new Bold());
            rp.Append(new FontSize { Val = schriftHalb.ToString(CultureInfo.InvariantCulture) });
            var r = new Run();
            r.Append(rp);
            r.Append(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            p.Append(r);
            tc.Append(p);
            return tc;
        }

        /// <summary>Zweispaltige Eigenschaftstabelle (Label · Wert) — z. B. Projektkopf.</summary>
        public void Eigenschaften(params string[] labelWertPaare)
        {
            int wLabel = 2800, wWert = WordBerichtGenerator.INHALT_B - 2800;
            Table t = NeueTabelle(new[] { wLabel, wWert });
            for (int i = 0; i + 1 < labelWertPaare.Length; i += 2)
            {
                var tr = new TableRow();
                tr.Append(Zelle(labelWertPaare[i], wLabel, true, WordBerichtGenerator.STAMM_FILL, JustificationValues.Left));
                tr.Append(Zelle(labelWertPaare[i + 1], wWert, false, null, JustificationValues.Left));
                t.Append(tr);
            }
            Fuege(t);
        }

        // ------------------------------------------------------------- Blocksplitting

        /// <summary>
        /// Zerlegt die Varianten (ohne Stamm) in Blöcke zu maximal MAX_VARIANTEN_JE_BLOCK;
        /// die Stamm-Spalte wird in jedem Block wiederholt (Konzept Kap. 5.1).
        /// Ohne Varianten liefert das genau einen Block mit leerer Liste (nur Stamm).
        /// </summary>
        public List<List<VariantenDaten>> VariantenBloecke(BerichtsDaten daten)
        {
            var varianten = daten.Varianten.Where(v => !v.IstStamm).ToList();
            var bloecke = new List<List<VariantenDaten>>();
            if (varianten.Count == 0) { bloecke.Add(new List<VariantenDaten>()); return bloecke; }
            for (int i = 0; i < varianten.Count; i += MAX_VARIANTEN_JE_BLOCK)
                bloecke.Add(varianten.Skip(i).Take(MAX_VARIANTEN_JE_BLOCK).ToList());
            return bloecke;
        }

        // ------------------------------------------------------------- Formatierung

        public string F(double v, int dez) { return v.ToString("N" + dez, Kultur); }

        /// <summary>Kennzahlwert formatiert; null → „—" (nie 0, Konzept Kap. 5).</summary>
        public string FW(double? v, string format)
        { return v.HasValue ? v.Value.ToString(format, Kultur) : "—"; }

        /// <summary>Δ Variante − Stamm mit Vorzeichen (aus Rohwerten, Befund B7 behoben).</summary>
        public string Delta(double? stamm, double? variante, string format)
        {
            if (!stamm.HasValue || !variante.HasValue) return "—";
            double d = variante.Value - stamm.Value;
            string betrag = Math.Abs(d).ToString(format, Kultur);
            if (d > 0) return "+" + betrag;
            if (d < 0) return "−" + betrag;
            return "±" + 0.0.ToString(format, Kultur);
        }

        /// <summary>Δ in Prozent zum Stammwert ("+12,3 %"); „—" wenn nicht berechenbar.</summary>
        public string DeltaProzent(double? stamm, double? variante)
        {
            if (!stamm.HasValue || !variante.HasValue || Math.Abs(stamm.Value) < 1e-9) return "—";
            double p = (variante.Value - stamm.Value) / Math.Abs(stamm.Value) * 100.0;
            string betrag = Math.Abs(p).ToString("N1", Kultur) + " %";
            if (p > 0.05) return "+" + betrag;
            if (p < -0.05) return "−" + betrag;
            return "±0,0 %";
        }
    }
}
