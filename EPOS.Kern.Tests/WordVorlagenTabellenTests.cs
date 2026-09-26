using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Strukturtabellen der Word-Engine</b> (Konzept Berichtsvorlagen 4.2, 4.3, 4.10, 6.2, 6.4 Nr. 2; Etappe
    /// BV-E5): <c>{{tabelle.…}}</c> allein im Absatz wird die Tabelle — Spaltenblöcke zu drei Varianten, <c>|block n</c>,
    /// Breite in Prozent; ohne Tabellenformatvorlage und Mustertabelle in der heutigen Direktformatierung, mit
    /// Mustertabelle die Rollenformate aus ihr (sie wird entfernt, das Format „EPOS Tabelle“ angelegt), mit eigener
    /// Formatvorlage deren Kopfzeile; <c>stand.tabelle.*</c> im Block <c>je stand</c>; die leere Tabelle als Leertext mit
    /// Grund; falsche Stellen bleiben gelb stehen. Jedes gefüllte Dokument besteht den <c>OpenXmlValidator</c>; gefüllt
    /// wird nach dem Sammler ohne Datenbank. Dazu die Tabellenregeln des Prüfers.
    /// </summary>
    [Collection("Testdatenbank")]
    public class WordVorlagenTabellenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly List<string> _ordner = new List<string>();

        public WordVorlagenTabellenTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            foreach (string o in _ordner)
                try { Directory.Delete(o, true); } catch { /* Aufräumen darf nicht scheitern */ }
            _kultur.Dispose();
        }

        // =====================================================================
        //  Blöcke, Breite in Prozent, Direktformatierung
        // =====================================================================

        /// <summary>
        /// Ohne Formatvorlage und Mustertabelle: je Block eine Tabelle (7 Varianten → 3), Breite 100 % und jede Zeile
        /// in Prozent zu 100 % aufgeteilt, Rahmen und Kopfhinterlegung wie der Bausteinweg, die Stammspalte grau, die
        /// Δ-Spalte nur bei genau einer Variante; kein Tabellenformat angelegt.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public void Strukturtabellen_in_Bloecken_mit_Prozentbreite_und_Direktformatierung(int varianten)
        {
            byte[] v = Vorlage(Absatz("Vorher"), Absatz("{{tabelle.komponenten.matrix}}"), Absatz("{{tabelle.vergleich.energiebilanz}}"), Absatz("Nachher"));
            string ziel = Ziel("bloecke_" + varianten + ".docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(varianten), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            List<Table> tabellen = doc.MainDocumentPart.Document.Body.Elements<Table>().ToList();
            int bloecke = varianten > 3 ? 3 : 1;
            Assert.Equal(2 * bloecke, tabellen.Count);
            foreach (Table t in tabellen)
            {
                TableProperties tp = t.GetFirstChild<TableProperties>();
                Assert.Equal(TableWidthUnitValues.Pct, tp.TableWidth.Type.Value);
                Assert.Equal("5000", tp.TableWidth.Width.Value);
                Assert.Null(tp.TableStyle);
                Assert.NotNull(tp.TableBorders);
                foreach (TableRow z in t.Elements<TableRow>())
                    Assert.Equal(5000, z.Elements<TableCell>().Sum(c => int.Parse(c.TableCellProperties.TableCellWidth.Width.Value, CultureInfo.InvariantCulture)));
                TableRow kopf = t.Elements<TableRow>().First();
                Assert.All(kopf.Elements<TableCell>(), c => Assert.Equal(WordBerichtGenerator.HEAD_FILL, Fuellung(c)));
                Assert.Equal("Stamm", kopf.Elements<TableCell>().ElementAt(1).InnerText);
                Assert.All(t.Elements<TableRow>().Skip(1), z => Assert.Equal(WordBerichtGenerator.STAMM_FILL, Fuellung(z.Elements<TableCell>().ElementAt(1))));
            }
            Assert.Equal(varianten == 1, doc.MainDocumentPart.Document.Body.InnerText.Contains("Δ (Var. − Stamm)", StringComparison.Ordinal));
            Assert.Empty(e.Hinweise.Where(h => h.Contains(WordVorlagenstile.NAME_TABELLE, StringComparison.Ordinal)));
            Assert.Null(new WordVorlagenstile(doc.MainDocumentPart).Finde(WordVorlagenstile.TABELLE));

            // Zwischen zwei Tabellen steht ein Absatz — sonst verschmölzen sie.
            List<Type> folge = doc.MainDocumentPart.Document.Body.ChildElements.Select(c => c.GetType()).ToList();
            for (int i = 1; i < folge.Count; i++)
                Assert.False(folge[i] == typeof(Table) && folge[i - 1] == typeof(Table), "zwei Tabellen ohne Absatz dazwischen");
        }

        /// <summary>
        /// <c>|block n</c> stellt die Blockgröße ein: sieben Varianten zu dreien → drei Tabellen (wie ohne Angabe), zu
        /// zweien → vier; jeder Block beginnt mit Beschriftung und Stamm.
        /// </summary>
        [Theory]
        [InlineData(3, new[] { 5, 5, 3 })]
        [InlineData(2, new[] { 4, 4, 4, 3 })]
        public void Blockangabe_stellt_die_Blockgroesse_ein(int n, int[] spalten)
        {
            byte[] v = Vorlage(Absatz("{{tabelle.komponenten.matrix|block " + n + "}}"));
            string ziel = Ziel("block" + n + ".docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(7), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            List<Table> tabellen = doc.MainDocumentPart.Document.Body.Elements<Table>().ToList();
            Assert.Equal(spalten, tabellen.Select(t => t.Elements<TableRow>().First().Elements<TableCell>().Count()));
            Assert.All(tabellen, t => Assert.Equal("Stamm", t.Elements<TableRow>().First().Elements<TableCell>().ElementAt(1).InnerText));
        }

        // =====================================================================
        //  Mustertabelle und Formatvorlage
        // =====================================================================

        /// <summary>
        /// Die Mustertabelle gibt den Rollen ihr Format: Stamm- und Gruppenzellen tragen Schattierung und Zeichenformat
        /// der Musterzellen; die Mustertabelle ist entfernt, das Tabellenformat „EPOS Tabelle“ angelegt (mit Hinweis) und
        /// jeder Tabelle zugewiesen; die Kopfzeile formatiert die Formatvorlage (keine Direkthinterlegung) und
        /// wiederholt sich je Seite.
        /// </summary>
        [Fact]
        public void Mustertabelle_gibt_den_Rollen_ihr_Format_und_wird_entfernt()
        {
            byte[] v = Vorlage(Muster(), Absatz("{{tabelle.vergleich}}"));
            string ziel = Ziel("muster.docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(2), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Table t = Assert.Single(doc.MainDocumentPart.Document.Body.Elements<Table>());
            Assert.False(Tabellenmuster.IstMuster(t));
            string stil = new WordVorlagenstile(doc.MainDocumentPart).Finde(WordVorlagenstile.TABELLE);
            Assert.NotNull(stil);
            Assert.Equal(stil, t.GetFirstChild<TableProperties>().TableStyle.Val.Value);
            Assert.Contains(e.Hinweise, h => h.Contains(WordVorlagenstile.NAME_TABELLE, StringComparison.Ordinal));

            List<TableRow> zeilen = t.Elements<TableRow>().ToList();
            Assert.NotNull(zeilen[0].TableRowProperties?.GetFirstChild<TableHeader>());
            Assert.All(zeilen[0].Elements<TableCell>(), c => Assert.Null(Fuellung(c)));

            TableRow gruppe = zeilen.First(z => z.InnerText.StartsWith("Energiebilanz", StringComparison.Ordinal));
            Assert.All(gruppe.Elements<TableCell>(), c => Assert.Equal("00B050", Fuellung(c)));
            TableRow waerme = zeilen.First(z => z.InnerText.StartsWith("Wärmebedarf gesamt", StringComparison.Ordinal));
            TableCell stamm = waerme.Elements<TableCell>().ElementAt(1);
            Assert.Equal("FFC000", Fuellung(stamm));
            Assert.NotNull(stamm.Descendants<RunProperties>().First().GetFirstChild<Italic>());
            Assert.Null(Fuellung(waerme.Elements<TableCell>().ElementAt(2)));
        }

        /// <summary>
        /// Trägt die Vorlage ihr eigenes Format „EPOS Tabelle“ (über <c>w:name</c>, mit eigener ID), nimmt die Tabelle es —
        /// nichts wird angelegt; ohne Muster bleibt die Stammspalte grau.
        /// </summary>
        [Fact]
        public void Das_Format_der_Vorlage_gilt_ueber_seinen_Namen()
        {
            byte[] v = Vorlage(new[] { Absatz("{{tabelle.komponenten.matrix}}") }, "MeineTabelle");
            string ziel = Ziel("stil.docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(1), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Table t = Assert.Single(doc.MainDocumentPart.Document.Body.Elements<Table>());
            Assert.Equal("MeineTabelle", t.GetFirstChild<TableProperties>().TableStyle.Val.Value);
            Assert.Null(t.GetFirstChild<TableProperties>().TableBorders);
            Assert.DoesNotContain(e.Hinweise, h => h.Contains(WordVorlagenstile.NAME_TABELLE, StringComparison.Ordinal));
            Assert.Equal(WordBerichtGenerator.STAMM_FILL, Fuellung(t.Elements<TableRow>().ElementAt(1).Elements<TableCell>().ElementAt(1)));
        }

        /// <summary>Eine Mustertabelle ohne Rolle wird entfernt, mit Hinweis; es gilt die Direktformatierung.</summary>
        [Fact]
        public void Mustertabelle_ohne_Rolle_wird_entfernt_mit_Hinweis()
        {
            byte[] v = Vorlage(Muster(("Irgendwas", "FF0000", null)), Absatz("{{tabelle.komponenten.matrix}}"));
            string ziel = Ziel("muster_leer.docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(1), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Table t = Assert.Single(doc.MainDocumentPart.Document.Body.Elements<Table>());
            Assert.Null(t.GetFirstChild<TableProperties>().TableStyle);
            Assert.Contains(e.Hinweise, h => h == WordVorlagentexte.MUSTER_OHNE_ROLLEN);
        }

        // =====================================================================
        //  Block je stand, Leertext, Schalter, falsche Stellen
        // =====================================================================

        /// <summary><c>stand.tabelle.kennzahlen</c> im Block <c>je stand</c>: je Stand seine Tafel mit seinen Zahlen.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void Standtabelle_im_Block_je_stand(int varianten)
        {
            byte[] v = Vorlage(Absatz("{{#je stand}}"), Absatz("Stand {{stand.anzeige}}"), Absatz("{{stand.tabelle.kennzahlen}}"), Absatz("{{/je}}"));
            string ziel = Ziel("je_stand_" + varianten + ".docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(varianten), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            List<Table> tabellen = doc.MainDocumentPart.Document.Body.Elements<Table>().ToList();
            Assert.Equal(varianten + 1, tabellen.Count);
            for (int i = 0; i < tabellen.Count; i++)
            {
                List<string> werte = tabellen[i].Elements<TableRow>().Select(z => z.Elements<TableCell>().Last().InnerText).ToList();
                Assert.Contains((100 + 10 * i).ToString(CultureInfo.InvariantCulture) + " MWh/a", werte);
            }
        }

        /// <summary>
        /// Ein Inhaltssteuerelement auf Blockebene mit dem Tabellenschlüssel als Tag wird die Tabelle und ist danach
        /// ausgepackt; eine Tabelle in einer Tabellenzelle wird eine geschachtelte Tabelle.
        /// </summary>
        [Fact]
        public void Steuerelement_und_Tabellenzelle_tragen_die_Tabelle()
        {
            string sdt = "<w:sdt><w:sdtPr><w:tag w:val=\"tabelle.varianten\"/></w:sdtPr><w:sdtContent>" + Absatz("Varianten") + "</w:sdtContent></w:sdt>";
            string zelle = "<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"9000\"/></w:tblGrid>" +
                           "<w:tr><w:tc><w:tcPr><w:tcW w:w=\"9000\" w:type=\"dxa\"/></w:tcPr>" + Absatz("{{tabelle.anhang.simulationsstaende}}") +
                           "</w:tc></w:tr></w:tbl>";
            byte[] v = Vorlage(sdt, Absatz(""), zelle);
            string ziel = Ziel("sdt_zelle.docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(2), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Body rumpf = doc.MainDocumentPart.Document.Body;
            Assert.Empty(rumpf.Descendants<SdtElement>());
            List<Table> oben = rumpf.Elements<Table>().ToList();
            Assert.Equal(2, oben.Count);
            Assert.Equal("Rolle", oben[0].Elements<TableRow>().First().Elements<TableCell>().First().InnerText);
            Table innen = Assert.Single(oben[1].Descendants<Table>());
            Assert.Equal(4, innen.Elements<TableRow>().Count());   // Kopf, Stamm, zwei Varianten
            Assert.IsType<Paragraph>(innen.Parent.ChildElements.Last());   // die Zelle endet mit einem Absatz
        }

        /// <summary>Eine Tabelle ohne Zeilen: an ihrer Stelle der Leertext mit Grund, nie eine leere Stelle.</summary>
        [Fact]
        public void Leere_Tabelle_schreibt_den_Leertext_mit_Grund()
        {
            byte[] v = Vorlage(Absatz("{{tabelle.vergleich.delta_prozent}}"));
            string ziel = Ziel("leer.docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(1), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Assert.Empty(doc.MainDocumentPart.Document.Body.Elements<Table>());
            Assert.Contains("— (weniger als zwei Stände mit Wert)", Absaetze(doc));
            Assert.True(e.Leere.ContainsKey("tabelle.vergleich.delta_prozent"));
        }

        /// <summary>Der Schalter je Tabelle wertet, ob die Tabelle Zeilen trägt.</summary>
        [Theory]
        [InlineData(1, false)]
        [InlineData(3, true)]
        public void Schalter_je_Tabelle(int varianten, bool erwartet)
        {
            byte[] v = Vorlage(Absatz("{{#wenn hat.tabelle.vergleich.delta_prozent}}"), Absatz("Mit Tafel"), Absatz("{{/wenn}}"), Absatz("Ende"));
            string ziel = Ziel("schalter_" + varianten + ".docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(varianten), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e);
            Assert.Equal(erwartet, Absaetze(doc).Contains("Mit Tafel"));
        }

        /// <summary>
        /// Im Satz, als Wert je Stand außerhalb des Blocks und als getippte Mustertabelle bleibt der Platzhalter gelb
        /// stehen und steht im Ergebnis.
        /// </summary>
        [Fact]
        public void Falsche_Stellen_bleiben_stehen()
        {
            byte[] v = Vorlage(Absatz("Siehe {{tabelle.varianten}} oben"), Absatz("{{stand.tabelle.kennzahlen}}"), Absatz("{{muster.tabelle}}"));
            string ziel = Ziel("falsch.docx");
            Fuellergebnis e = Fuelle(v, BerichtstabelleTests.Kennzahldaten(1), ziel);

            using WordprocessingDocument doc = Pruefe(ziel, e, gelb: 3);
            Assert.Empty(doc.MainDocumentPart.Document.Body.Elements<Table>());
            Assert.Equal(new[] { Fuellbefundart.FalscheStelle, Fuellbefundart.Kontext, Fuellbefundart.FalscheStelle },
                         e.Unbekannte.Select(b => b.Art));
        }

        // =====================================================================
        //  Referenzprojekte ohne Datenbank
        // =====================================================================

        /// <summary>
        /// <b>Nach dem Sammler füllt jede Tabelle ohne Datenbank</b> — 1030 und 1019, alle Tabellen der Gruppe und je
        /// Stand alle Tafeln des Stands: kein Zugriff, kein unbekannter Schlüssel, Validator grün.
        /// </summary>
        [Theory]
        [InlineData(Berichtsdatenproben.PROJEKT_1030)]
        [InlineData(1019)]
        public void Nach_dem_Sammler_fuellen_alle_Tabellen_ohne_Datenbank(int stamm)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            BerichtsDaten daten = BerichtstabelleTests.Sammle(stamm, konfig);

            var teile = new List<string>();
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle.Where(f => f.Art == Vorlagenfeldart.Tabelle && f.Kontext != Vorlagenfeldkontext.Stand
                                                                           && f.Schluessel != Vorlagenfeldkatalog.MUSTER_TABELLE))
                teile.Add(Absatz("{{" + f.Schluessel + "}}"));
            teile.Add(Absatz("{{#je stand}}"));
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle.Where(f => f.Art == Vorlagenfeldart.Tabelle && f.Kontext == Vorlagenfeldkontext.Stand))
                teile.Add(Absatz("{{" + f.Schluessel + "}}"));
            teile.Add(Absatz("{{/je}}"));
            byte[] v = Vorlage(teile.ToArray());

            string ziel = Ziel("ohne_db_" + stamm + ".docx");
            Fuellergebnis e = null;
            List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() =>
                e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, v, new Erstellerangaben(), ziel));

            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Füllen:\n" + string.Join("\n", zugriffe.Take(20)));
            using WordprocessingDocument doc = Pruefe(ziel, e);
            int tabellen = doc.MainDocumentPart.Document.Body.Elements<Table>().Count();
            _ausgabe.WriteLine(stamm + ": " + tabellen + " Tabellen, leer: " + string.Join(", ", e.Leere.Keys));
            Assert.True(tabellen >= (stamm == 1019 ? 25 : 12), "zu wenig Tabellen: " + tabellen);
        }

        // =====================================================================
        //  Prüfer
        // =====================================================================

        /// <summary>
        /// Die Tabellenregeln des Prüfers: eine Tabelle im Satz und eine getippte Mustertabelle sind Fehler „passt nicht
        /// an diese Stelle“ mit Vorschlag, eine Mustertabelle ohne Rolle ist eine Warnung; eine gültige Vorlage mit
        /// Tabellen, Mustertabelle und Block bleibt ohne Befund.
        /// </summary>
        [Fact]
        public void Pruefer_meldet_Tabellen_an_falscher_Stelle_und_Muster_ohne_Rolle()
        {
            byte[] schlecht = Vorlage(Muster(("Irgendwas", "FF0000", null)), Absatz("Siehe {{tabelle.varianten}}"), Absatz("{{muster.tabelle}}"));
            Pruefbefund b = Vorlagenpruefer.Pruefe(schlecht, Pruefstufe.Schnell, new Pruefkontext());
            _ausgabe.WriteLine(Probevorlagen.Liste(b));
            List<Pruefmeldung> ort = Probevorlagen.Mit(b, "VF_PRUEF_ORT");
            Assert.Equal(2, ort.Count);
            Assert.All(ort, m => Assert.Equal(Befundstufe.Fehler, m.Stufe));
            Assert.All(ort, m => Assert.False(string.IsNullOrWhiteSpace(m.WasTun)));
            Pruefmeldung muster = Assert.Single(Probevorlagen.Mit(b, "VF_PRUEF_MUSTER_OHNE_ROLLEN"));
            Assert.Equal(Befundstufe.Warnung, muster.Stufe);

            byte[] gut = Vorlage(Muster(), Absatz("{{tabelle.vergleich|block 2}}"),
                                 Absatz("{{#je stand}}"), Absatz("{{stand.tabelle.erzeuger}}"), Absatz("{{/je}}"),
                                 Absatz("{{#wenn hat.tabelle.erzeuger}}"), Absatz("x"), Absatz("{{/wenn}}"));
            Pruefbefund g = Vorlagenpruefer.Pruefe(gut, Pruefstufe.Voll, new Pruefkontext());
            _ausgabe.WriteLine(Probevorlagen.Liste(g));
            Assert.Empty(g.Meldungen.Where(m => m.Stufe == Befundstufe.Fehler));
            Assert.Empty(Probevorlagen.Mit(g, "VF_PRUEF_MUSTER_OHNE_ROLLEN"));
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

        private static byte[] Vorlage(params string[] teile) => Vorlage(teile, null);

        /// <summary>Eine Vorlage aus Rumpfteilen (XML); <paramref name="tabellenstil"/>: ID eines Formats „EPOS Tabelle“.</summary>
        private static byte[] Vorlage(string[] teile, string tabellenstil)
        {
            using var ms = new MemoryStream();
            using (WordprocessingDocument doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                StyleDefinitionsPart stile = main.AddNewPart<StyleDefinitionsPart>();
                stile.Styles = new Styles(Probevorlagen.Stil("Standard", "Normal", null, true));
                if (tabellenstil != null)
                {
                    var s = new Style { Type = StyleValues.Table, StyleId = tabellenstil, CustomStyle = true };
                    s.Append(new StyleName { Val = WordVorlagenstile.NAME_TABELLE });
                    stile.Styles.Append(s);
                }
                main.Document = new Document("<w:document " + NS + "><w:body>" + string.Concat(teile) + ABSCHNITT + "</w:body></w:document>");
            }
            return ms.ToArray();
        }

        private static string Absatz(string text)
        {
            return "<w:p><w:r><w:t xml:space=\"preserve\">" + text + "</w:t></w:r></w:p>";
        }

        /// <summary>
        /// Eine Mustertabelle (Titel <c>{{muster.tabelle}}</c>) mit Zellen (Text, Schattierung, Zeichenformat-XML);
        /// ohne Angabe Stamm (FFC000, kursiv), Gruppe (00B050, fett) und Summe (7030A0).
        /// </summary>
        private static string Muster(params (string Text, string Farbe, string Zeichen)[] zellen)
        {
            if (zellen.Length == 0)
                zellen = new[] { ("Stamm", "FFC000", "<w:i/>"), ("Gruppe", "00B050", "<w:b/>"), ("Summe", "7030A0", (string)null) };
            string xml = "<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\"/><w:tblCaption w:val=\"{{muster.tabelle}}\"/></w:tblPr><w:tblGrid>" +
                         string.Concat(zellen.Select(_ => "<w:gridCol w:w=\"2000\"/>")) + "</w:tblGrid><w:tr>";
            foreach ((string text, string farbe, string zeichen) in zellen)
                xml += "<w:tc><w:tcPr><w:tcW w:w=\"2000\" w:type=\"dxa\"/><w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"" + farbe + "\"/></w:tcPr>" +
                       "<w:p><w:r>" + (zeichen != null ? "<w:rPr>" + zeichen + "</w:rPr>" : "") + "<w:t>" + text + "</w:t></w:r></w:p></w:tc>";
            return xml + "</w:tr></w:tbl><w:p/>";
        }

        private static string Fuellung(TableCell c)
        {
            return c.TableCellProperties?.GetFirstChild<Shading>()?.Fill?.Value;
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
                foreach (DocumentFormat.OpenXml.FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen)
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

        private static List<string> Absaetze(WordprocessingDocument doc)
        {
            return doc.MainDocumentPart.Document.Body.Elements<Paragraph>().Select(p => p.InnerText).Where(t => t.Length > 0).ToList();
        }

        private string Ziel(string datei)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bv-e5-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            _ordner.Add(o);
            return Path.Combine(o, datei);
        }
    }
}
