using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using TX = WindowsFormsApplication1.WordVorlagentexte;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Blöcke der Word-Engine</b> (Konzept Berichtsvorlagen 4.2, 4.7, 4.8, 6.4, 6.6; Etappe BV-E4):
    /// Wiederholblöcke <c>{{#je stand}}</c>, <c>{{#je variante}}</c>, <c>{{#je gebaeude}}</c> und
    /// Bedingungen <c>{{#wenn …}}</c> als Absatzblöcke, Musterzeilen und Steuerelemente; 0, 1, 3 und 7
    /// Varianten, Verschachtelung, <c>|block n</c>, Leerfälle, Laufmeldung. Jedes gefüllte Dokument
    /// besteht den <c>OpenXmlValidator</c> in allen Office-Fassungen, trägt kein <c>{{</c> und keine gelbe
    /// Stelle mehr (außer wo ein Fehler es verlangt).
    /// </summary>
    [Collection("Testdatenbank")]
    public class WordVorlagenBloeckeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly List<string> _ordner = new List<string>();

        public WordVorlagenBloeckeTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            foreach (string o in _ordner)
                try { Directory.Delete(o, true); } catch { /* Aufräumen darf nicht scheitern */ }
            _kultur.Dispose();
        }

        // =====================================================================
        //  Absatzblöcke mit 0, 1, 3 und 7 Varianten
        // =====================================================================

        /// <summary>
        /// Absatzblöcke je Stand und je Variante, Bedingungen mit und ohne „nicht“: Jede Wiederholung trägt
        /// ihren Stand (der Stamm mit seinem Projektnamen), ohne Variante entfällt der Variantenblock ganz, und
        /// von den Markenabsätzen bleibt nichts — kein leerer Absatz.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public void Absatzbloecke_je_Stand_und_Variante_mit_Bedingung(int varianten)
        {
            byte[] v = Vorlage(
                Absatz("Anfang"),
                Absatz("{{#je stand}}"), Absatz("Stand {{stand.anzeige}}"), Absatz("{{/je}}"),
                Absatz("{{#je variante}}"), Absatz("Variante {{stand.anzeige}}"), Absatz("{{/je}}"),
                Absatz("{{#wenn hat.varianten}}"), Absatz("Mit Varianten"), Absatz("{{/wenn}}"),
                Absatz("{{#wenn nicht hat.varianten}}"), Absatz("Nur Stamm"), Absatz("{{/wenn}}"),
                Absatz("Ende"));
            string ziel = Ziel("absatz_" + varianten + ".docx");
            Fuellergebnis e = Fuelle(v, Daten(varianten), ziel);

            List<string> namen = Namen(varianten);
            var erwartet = new List<string> { "Anfang" };
            erwartet.AddRange(new[] { "Stammprojekt" }.Concat(namen).Select(n => "Stand " + n));
            erwartet.AddRange(namen.Select(n => "Variante " + n));
            erwartet.Add(varianten > 0 ? "Mit Varianten" : "Nur Stamm");
            erwartet.Add("Ende");

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Assert.Equal(erwartet, Absaetze(doc));
            Assert.Equal(varianten + 1 + varianten + 2, e.Ersetzt);   // stand.anzeige je Wiederholung, zwei Bedingungen
            Assert.Equal(2 * varianten + 1, e.Stellen["stand.anzeige"]);
        }

        // =====================================================================
        //  Musterzeile (Konzept 6.4 Nr. 1)
        // =====================================================================

        /// <summary>
        /// Eine Musterzeile — <c>{{#je stand}}</c> in der ersten, <c>{{/je}}</c> in der letzten Zelle — wird je
        /// Stand geklont, die Kopfzeile bleibt; die Marken verschwinden aus den Zellen, ihr Text bleibt.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void Musterzeile_wird_je_Stand_geklont(int varianten)
        {
            byte[] v = Vorlage(
                Tabelle(new[] { "Stand", "Kennung" },
                        new[] { "{{#je stand}}{{stand.anzeige}}", "S{{/je}}" }),
                Absatz("Ende"));
            string ziel = Ziel("zeile_" + varianten + ".docx");
            Fuellergebnis e = Fuelle(v, Daten(varianten), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Table tabelle = Assert.Single(doc.MainDocumentPart.Document.Body.Elements<Table>());
            List<string> zeilen = tabelle.Elements<TableRow>()
                .Select(z => string.Join("|", z.Elements<TableCell>().Select(c => c.InnerText))).ToList();
            var erwartet = new List<string> { "Stand|Kennung" };
            erwartet.AddRange(new[] { "Stammprojekt" }.Concat(Namen(varianten)).Select(n => n + "|S"));
            Assert.Equal(erwartet, zeilen);
        }

        /// <summary>
        /// Leere Musterzeile: <c>{{#je variante}}</c> ohne Varianten entfernt die Zeile; bleibt in der Tabelle
        /// keine Zeile, entfällt die Tabelle — ohne leeren Absatz an ihrer Stelle.
        /// </summary>
        [Fact]
        public void Leere_Musterzeile_und_leere_Tabelle_entfallen()
        {
            byte[] v = Vorlage(
                Absatz("Vor"),
                Tabelle(new[] { "Kopf" }, new[] { "{{#je variante}}{{stand.anzeige}}{{/je}}" }),
                Tabelle(new[] { "{{#je variante}}{{stand.anzeige}}{{/je}}" }),
                Absatz("Nach"));
            string ziel = Ziel("leer.docx");
            Fuellergebnis e = Fuelle(v, Daten(0), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Body rumpf = doc.MainDocumentPart.Document.Body;
            Table tabelle = Assert.Single(rumpf.Elements<Table>());
            Assert.Equal("Kopf", Assert.Single(tabelle.Elements<TableRow>()).InnerText);
            Assert.Equal(new[] { "Vor", "Nach" }, Absaetze(doc));
        }

        // =====================================================================
        //  Verschachtelung (höchstens zwei Ebenen)
        // =====================================================================

        /// <summary>
        /// <c>{{#je stand}}</c> → <c>{{#je gebaeude}}</c>: je Stand seine Gebäude, der Gebäudename aus der
        /// Zeile des Stands; ein Stand ohne Gebäude zeigt nur seine Zeile. Ein leerer Gebäudename bekommt den
        /// Strich, und die Laufmeldung fasst ihn je Gebäude zusammen („leer bei 1 von 3 Gebäuden“).
        /// </summary>
        [Fact]
        public void Je_Stand_und_darin_je_Gebaeude()
        {
            BerichtsDaten daten = Daten(2);
            daten.Varianten[0].Details = MitGebaeuden("Haus A", "Haus B");
            daten.Varianten[1].Details = MitGebaeuden("");
            byte[] v = Vorlage(
                Absatz("{{#je stand}}"), Absatz("S {{stand.anzeige}}"),
                Absatz("{{#je gebaeude}}"), Absatz("G {{gebaeude.name}}"), Absatz("{{/je}}"),
                Absatz("{{/je}}"),
                Absatz("{{#je gebaeude}}"), Absatz("Stamm: {{gebaeude.name}}"), Absatz("{{/je}}"));
            string ziel = Ziel("gebaeude.docx");
            Fuellergebnis e = Fuelle(v, daten, ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Assert.Equal(new[]
            {
                "S Stammprojekt", "G Haus A", "G Haus B", "S Variante A", "G —", "S Variante B",
                "Stamm: Haus A", "Stamm: Haus B",
            }, Absaetze(doc));
            Assert.Equal(1, e.Leere["gebaeude.name"]);
            Assert.Contains(TX.F(false, TX.LEER_GEBAEUDE, "gebaeude.name", 1, 5), e.Meldungen());
        }

        /// <summary>
        /// <c>{{#je stand}}</c> → <c>{{#wenn stand.ist_stamm}}</c>: Die Bedingung gilt je Stand; mit „nicht“
        /// umgekehrt. Ein Schalter je Stand außerhalb eines Standblocks lässt Bereich und Marken stehen.
        /// </summary>
        [Fact]
        public void Je_Stand_und_darin_Bedingung_je_Stand()
        {
            byte[] v = Vorlage(
                Absatz("{{#je stand}}"), Absatz("{{stand.anzeige}}"),
                Absatz("{{#wenn stand.ist_stamm}}"), Absatz("ist Stamm"), Absatz("{{/wenn}}"),
                Absatz("{{#wenn nicht stand.ist_stamm}}"), Absatz("ist Variante"), Absatz("{{/wenn}}"),
                Absatz("{{/je}}"),
                Absatz("{{#wenn stand.ist_stamm}}"), Absatz("draußen"), Absatz("{{/wenn}}"));
            string ziel = Ziel("wenn.docx");
            Fuellergebnis e = Fuelle(v, Daten(2), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e, gelb: 2);
            Assert.Equal(new[]
            {
                "Stammprojekt", "ist Stamm", "Variante A", "ist Variante", "Variante B", "ist Variante",
                "{{#wenn stand.ist_stamm}}", "draußen", "{{/wenn}}",
            }, Absaetze(doc));
            Assert.Contains(e.Fehler, f => f.StartsWith("{{#wenn stand.ist_stamm}}: Der Schalter gilt nur in seinem Block", StringComparison.Ordinal));
            Assert.All(e.Unbekannte, b => Assert.Equal(Fuellbefundart.Block, b.Art));
        }

        /// <summary>Eine dritte Ebene wird nicht ausgewertet: Ihre Marken bleiben gelb stehen, die Laufmeldung nennt sie.</summary>
        [Fact]
        public void Dritte_Ebene_bleibt_stehen()
        {
            byte[] v = Vorlage(
                Absatz("{{#je stand}}"), Absatz("{{#wenn hat.varianten}}"),
                Absatz("{{#je gebaeude}}"), Absatz("x"), Absatz("{{/je}}"),
                Absatz("{{/wenn}}"), Absatz("{{/je}}"));
            string ziel = Ziel("tief.docx");
            Fuellergebnis e = Fuelle(v, Daten(1), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e, gelb: 4);
            Assert.Equal(new[] { "{{#je gebaeude}}", "x", "{{/je}}", "{{#je gebaeude}}", "x", "{{/je}}" }, Absaetze(doc));
            Assert.Equal(2, e.Fehler.Count(f => f.StartsWith("{{#je gebaeude}}: Blöcke sind höchstens zwei Ebenen tief", StringComparison.Ordinal)));
        }

        // =====================================================================
        //  Steuerelemente (Konzept 6.6)
        // =====================================================================

        /// <summary>
        /// Ein Block-Steuerelement mit dem Tag <c>#je stand</c> wiederholt seinen Inhalt samt Wertesteuerelement
        /// (Tag <c>stand.anzeige</c>) und wird ausgepackt; ein Zeilen-Steuerelement <c>#je variante</c> wiederholt
        /// seine Zeilen; <c>#wenn nicht hat.varianten</c> entfällt. Danach steht kein <c>w:sdt</c> mehr im Rumpf.
        /// </summary>
        [Fact]
        public void Steuerelemente_wiederholen_und_werden_ausgepackt()
        {
            byte[] v = Vorlage(
                "<w:sdt><w:sdtPr><w:tag w:val=\"#je stand\"/><w:id w:val=\"11\"/></w:sdtPr><w:sdtContent>" +
                Absatz("Stand:") +
                "<w:sdt><w:sdtPr><w:tag w:val=\"stand.anzeige\"/><w:showingPlcHdr/></w:sdtPr><w:sdtContent>" + Absatz("Name") +
                "</w:sdtContent></w:sdt></w:sdtContent></w:sdt>",
                "<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"4000\"/></w:tblGrid>" +
                "<w:tr><w:tc><w:tcPr><w:tcW w:w=\"4000\" w:type=\"dxa\"/></w:tcPr>" + Absatz("Kopf") + "</w:tc></w:tr>" +
                "<w:sdt><w:sdtPr><w:tag w:val=\"#je variante\"/></w:sdtPr><w:sdtContent>" +
                "<w:tr><w:tc><w:tcPr><w:tcW w:w=\"4000\" w:type=\"dxa\"/></w:tcPr>" + Absatz("{{stand.anzeige}}") + "</w:tc></w:tr>" +
                "</w:sdtContent></w:sdt></w:tbl>",
                "<w:sdt><w:sdtPr><w:tag w:val=\"#wenn nicht hat.varianten\"/></w:sdtPr><w:sdtContent>" + Absatz("Nur Stamm") +
                "</w:sdtContent></w:sdt>",
                Absatz("Ende"));
            string ziel = Ziel("sdt.docx");
            Fuellergebnis e = Fuelle(v, Daten(3), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Body rumpf = doc.MainDocumentPart.Document.Body;
            Assert.Empty(rumpf.Descendants<SdtElement>());
            Assert.Equal(new[] { "Stand:", "Stammprojekt", "Stand:", "Variante A", "Stand:", "Variante B", "Stand:", "Variante C", "Ende" },
                         rumpf.Elements<Paragraph>().Select(p => p.InnerText).ToList());
            Assert.Equal(new[] { "Kopf", "Variante A", "Variante B", "Variante C" },
                         Assert.Single(rumpf.Elements<Table>()).Elements<TableRow>().Select(z => z.InnerText));
        }

        // =====================================================================
        //  |block n (Konzept 4.8, 6.4 Nr. 3)
        // =====================================================================

        /// <summary>
        /// <c>{{#je variante|block 3}}</c> um Überschrift und Tabelle: Gruppen zu höchstens drei Varianten, je
        /// Gruppe ihre Tabelle mit der Stammzeile und den Varianten der Gruppe (innere <c>{{#je stand}}</c>).
        /// </summary>
        [Fact]
        public void Block_3_bildet_Gruppen_mit_Stammzeile()
        {
            byte[] v = Vorlage(
                Absatz("{{#je variante|block 3}}"),
                Absatz("Gruppe"),
                Tabelle(new[] { "{{#je stand}}{{stand.anzeige}}{{/je}}" }),
                Absatz("{{/je}}"));
            string ziel = Ziel("block3.docx");
            Fuellergebnis e = Fuelle(v, Daten(7), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Body rumpf = doc.MainDocumentPart.Document.Body;
            Assert.Equal(new[] { "Gruppe", "Gruppe", "Gruppe" }, Absaetze(doc));
            List<List<string>> tabellen = rumpf.Elements<Table>()
                .Select(t => t.Elements<TableRow>().Select(z => z.InnerText).ToList()).ToList();
            Assert.Equal(3, tabellen.Count);
            Assert.Equal(new[] { "Stammprojekt", "Variante A", "Variante B", "Variante C" }, tabellen[0]);
            Assert.Equal(new[] { "Stammprojekt", "Variante D", "Variante E", "Variante F" }, tabellen[1]);
            Assert.Equal(new[] { "Stammprojekt", "Variante G" }, tabellen[2]);
        }

        // =====================================================================
        //  Leerfälle, Überschriften, Abschnitte
        // =====================================================================

        /// <summary>
        /// Ein Block ohne Inhalt verschwindet samt Marken; entfällt ein Block (keine Variante, Bedingung
        /// falsch), entfällt eine Überschrift unmittelbar davor, der danach nur noch die nächste Überschrift
        /// folgt oder das Abschnittsende — eine Überschrift mit weiterem Text darunter bleibt. Eine
        /// Abschnittsangabe im Markenabsatz bleibt in einem leeren Absatz stehen.
        /// </summary>
        [Fact]
        public void Leerfaelle_ohne_Reste_und_ohne_verwaiste_Ueberschrift()
        {
            byte[] v = Vorlage(
                Absatz("{{#je stand}}"), Absatz("{{/je}}"),
                Kopf("Varianten"), Absatz("{{#je variante}}"), Absatz("{{stand.anzeige}}"), Absatz("{{/je}}"),
                Kopf("Hinweis"), Absatz("{{#wenn hat.varianten}}"), Absatz("mit"), Absatz("{{/wenn}}"), Absatz("Text bleibt"),
                Kopf("Schluss"),
                "<w:p><w:pPr><w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/></w:sectPr></w:pPr><w:r><w:t>{{#wenn hat.varianten}}</w:t></w:r></w:p>",
                Absatz("weg"), Absatz("{{/wenn}}"));
            string ziel = Ziel("leerfaelle.docx");
            Fuellergebnis e = Fuelle(v, Daten(0), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Body rumpf = doc.MainDocumentPart.Document.Body;
            Assert.Equal(new[] { "Hinweis", "Text bleibt", "" }, rumpf.Elements<Paragraph>().Select(p => p.InnerText));
            Assert.NotNull(rumpf.Elements<Paragraph>().Last().ParagraphProperties?.SectionProperties);
        }

        /// <summary>
        /// <b>Die Gliederungsebene zählt</b> (BV-E9, Befund der ausführlichen Vorlage): Ein Kapitelkopf vor einem
        /// entfallenden Block bleibt, wenn danach eine TIEFERE Überschrift folgt — sein Kapitel geht weiter. Eine
        /// Überschrift 2, der nur die nächste Überschrift 2 oder ein Kapitelkopf folgt, entfällt mit; ebenso ein
        /// Kapitelkopf vor dem nächsten Kapitelkopf.
        /// </summary>
        [Fact]
        public void Ueberschrift_vor_entfallendem_Block_folgt_der_Gliederungsebene()
        {
            byte[] v = Vorlage(
                Kopf("Kapitel"), Absatz("{{#wenn hat.varianten}}"), Absatz("weg"), Absatz("{{/wenn}}"),
                Ueberschrift2("Abschnitt"), Absatz("Text"),
                Ueberschrift2("Leer"), Absatz("{{#wenn hat.varianten}}"), Absatz("weg"), Absatz("{{/wenn}}"),
                Ueberschrift2("Zweiter"), Absatz("Text 2"),
                Ueberschrift2("Tief"), Absatz("{{#wenn hat.varianten}}"), Absatz("weg"), Absatz("{{/wenn}}"),
                Kopf("Nächstes Kapitel"), Absatz("{{#wenn hat.varianten}}"), Absatz("weg"), Absatz("{{/wenn}}"),
                Kopf("Letztes Kapitel"), Absatz("Ende"));
            string ziel = Ziel("ebenen.docx");
            Fuellergebnis e = Fuelle(v, Daten(0), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Assert.Equal(new[] { "Kapitel", "Abschnitt", "Text", "Zweiter", "Text 2", "Letztes Kapitel", "Ende" }, Absaetze(doc));
        }

        // =====================================================================
        //  Standardweg bleibt
        // =====================================================================

        /// <summary>Eine Vorlage ohne Blöcke wird wie bisher gefüllt: die Engine rührt Tabellen und Absätze nicht an.</summary>
        [Fact]
        public void Ohne_Bloecke_bleibt_alles_wie_es_ist()
        {
            byte[] v = Vorlage(Absatz("{{bericht.titel}}"), Tabelle(new[] { "a", "b" }, new[] { "c", "{{projekt.name}}" }));
            string ziel = Ziel("ohne.docx");
            Fuellergebnis e = Fuelle(v, Daten(2), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Assert.Equal(new[] { "ab", "cStammprojekt" },
                         doc.MainDocumentPart.Document.Body.Descendants<TableRow>().Select(z => z.InnerText));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private const string NS =
            "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"";

        private const string ABSCHNITT =
            "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/>" +
            "<w:pgMar w:top=\"1417\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1417\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr>";

        /// <summary>Eine Vorlage aus Rumpfteilen (XML), mit Kapitelkopf und Überschrift 2 als Stilen.</summary>
        private static byte[] Vorlage(params string[] teile)
        {
            using var ms = new MemoryStream();
            using (WordprocessingDocument doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                StyleDefinitionsPart stile = main.AddNewPart<StyleDefinitionsPart>();
                stile.Styles = new Styles(
                    new Style(new StyleName { Val = WordVorlagenstile.NAME_KAPITELKOPF })
                    { Type = StyleValues.Paragraph, StyleId = "EPOSKapitelkopf", CustomStyle = true },
                    new Style(new StyleName { Val = "heading 2" }) { Type = StyleValues.Paragraph, StyleId = "Heading2" });
                main.Document = new Document("<w:document " + NS + "><w:body>" + string.Concat(teile) + ABSCHNITT + "</w:body></w:document>");
            }
            return ms.ToArray();
        }

        private static string Absatz(string text)
        {
            return "<w:p><w:r><w:t xml:space=\"preserve\">" + text + "</w:t></w:r></w:p>";
        }

        private static string Kopf(string text)
        {
            return "<w:p><w:pPr><w:pStyle w:val=\"EPOSKapitelkopf\"/></w:pPr><w:r><w:t xml:space=\"preserve\">" + text + "</w:t></w:r></w:p>";
        }

        private static string Ueberschrift2(string text)
        {
            return "<w:p><w:pPr><w:pStyle w:val=\"Heading2\"/></w:pPr><w:r><w:t xml:space=\"preserve\">" + text + "</w:t></w:r></w:p>";
        }

        /// <summary>Eine Tabelle, je Zeile ein Feld von Zellentexten.</summary>
        private static string Tabelle(params string[][] zeilen)
        {
            int spalten = zeilen.Max(z => z.Length);
            string xml = "<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\"/></w:tblPr><w:tblGrid>" +
                         string.Concat(Enumerable.Repeat("<w:gridCol w:w=\"2000\"/>", spalten)) + "</w:tblGrid>";
            foreach (string[] zeile in zeilen)
                xml += "<w:tr>" + string.Concat(zeile.Select(z => "<w:tc><w:tcPr><w:tcW w:w=\"2000\" w:type=\"dxa\"/></w:tcPr>" + Absatz(z) + "</w:tc>")) + "</w:tr>";
            return xml + "</w:tbl>";
        }

        /// <summary>Die synthetische Gruppe: Stamm „Stammprojekt“ und <paramref name="varianten"/> Varianten A, B, ….</summary>
        private static BerichtsDaten Daten(int varianten) => Berichtsdatenproben.Gruppendaten(varianten + 1);

        private static List<string> Namen(int varianten)
        {
            return Enumerable.Range(0, varianten).Select(i => "Variante " + (char)('A' + i)).ToList();
        }

        /// <summary>Projektdetails mit Gebäuden der genannten Namen (Spalte <c>Gebaeudename</c>).</summary>
        private static ProjektDetails MitGebaeuden(params string[] namen)
        {
            var t = new DataTable();
            t.Columns.Add("ID", typeof(long));
            t.Columns.Add("Gebaeudename", typeof(string));
            long id = 1;
            foreach (string n in namen) t.Rows.Add(id++, n);
            return new ProjektDetails { Gebaeude = t };
        }

        private Fuellergebnis Fuelle(byte[] vorlage, BerichtsDaten daten, string ziel)
        {
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(), vorlage,
                                                                            new Erstellerangaben(), ziel);
            foreach (string zeile in e.Meldungen()) _ausgabe.WriteLine(zeile);
            return e;
        }

        /// <summary>Öffnet den Bericht und prüft: Validator in allen Fassungen ohne Befund, Zahl der gelben Stellen.</summary>
        private WordprocessingDocument Pruefe(string ziel, Fuellergebnis e, int gelb = 0)
        {
            var befunde = new List<string>();
            using (WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false))
                foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen)
                    foreach (ValidationErrorInfo f in new OpenXmlValidator(fassung).Validate(doc).Take(5))
                        befunde.Add(fassung + ": " + f.Description + " @ " + f.Path?.XPath);
            foreach (string b in befunde) _ausgabe.WriteLine(b);
            Assert.Empty(befunde);

            WordprocessingDocument offen = WordprocessingDocument.Open(ziel, false);
            Body rumpf = offen.MainDocumentPart.Document.Body;
            Assert.Equal(gelb, rumpf.Descendants<Run>().Count(r => r.RunProperties?.Highlight != null));
            Assert.Equal(gelb, e.Unbekannte.Count);
            if (gelb == 0) Assert.DoesNotContain("{{", rumpf.InnerText, StringComparison.Ordinal);
            return offen;
        }

        /// <summary>Die Texte der Absätze des Rumpfs auf oberster Ebene; leere Absätze vor der Abschnittsangabe zählen nicht.</summary>
        private static List<string> Absaetze(WordprocessingDocument doc)
        {
            return doc.MainDocumentPart.Document.Body.Elements<Paragraph>()
                .Where(p => !(p.InnerText.Length == 0 && p.ParagraphProperties?.SectionProperties != null))
                .Select(p => p.InnerText).ToList();
        }

        private string Ziel(string datei)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bv-e4-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            _ordner.Add(o);
            return Path.Combine(o, datei);
        }
    }
}
