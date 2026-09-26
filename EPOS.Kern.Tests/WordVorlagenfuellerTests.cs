using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using R = WindowsFormsApplication1.MyResource.Resource;
using TX = WindowsFormsApplication1.WordVorlagentexte;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Word-Engine der Berichtsvorlagen</b> (Konzept Berichtsvorlagen mit Platzhaltern 4, 5.3,
    /// 6; Etappe BV-E1 Teil A2): <see cref="WordVorlagenfueller"/> über
    /// <see cref="WordBerichtGenerator.ErzeugeMitVorlage"/>.
    ///
    /// <para><b>(a) Standardvorlage.</b> Die Standardvorlage im vollen Aufbau (BV-E2: Deckblatt aus
    /// Platzhaltern, Kapitel einzeln unter Kapitelköpfen) füllt für 1030 und die Gruppe jede Stelle; den
    /// Rumpf Zeile für Zeile hält die Messlatte des Vorlagenwegs (<see cref="BerichtVorlagenMesslatteTests"/>),
    /// die Fußzeile trägt Firma, Berichtsdatum statt DATE-Feld und „Seite“. (b) Beispielvorlage: Deckblatt
    /// gefüllt, Kapitel ohne Häkchen entfallen samt Kapitelkopf. (c) Schmutzige Vorlagen, hier per SDK
    /// gebaut. (d) Leerwerte und Ausnahmen. (e) Kapitel an der falschen Stelle. (f) Kapitel einzeln:
    /// Folge, Entfall, doppelt, <c>|ohne titel</c>, <c>|ebene n</c>, Umbruch vor dem Kapitelkopf des
    /// Anhangs E, Sammelanker ohne die einzeln geführten, Stellen der Anhang-E-Checkliste. (g) Das Logo
    /// als Bildplatzhalter (Anwenderentscheid BV-E2-1).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WordVorlagenfuellerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly List<string> _ordner = new List<string>();

        public WordVorlagenfuellerTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            foreach (string o in _ordner)
                try { Directory.Delete(o, true); } catch { /* Aufräumen darf nicht scheitern */ }
            _kultur.Dispose();
        }

        /// <summary>Die Firma der Proben — dieselbe, die die Messlatte in der Fußzeile trägt.</summary>
        private const string FIRMA = "INEKON GmbH";

        // =====================================================================
        //  (a) Standardvorlage über die Engine
        // =====================================================================

        /// <summary>
        /// Die Standardvorlage im vollen Aufbau (BV-E2, Anhang B.3) über die Engine: Validator in allen
        /// Fassungen ohne Fehler, kein <c>{{</c> mehr, keine gelbe Stelle, nichts hinter der letzten
        /// Abschnittsangabe, Bildkennungen eindeutig; die Kopfzeile trägt das Programm wie bisher, die
        /// Fußzeile Firma, Berichtsdatum statt DATE-Feld und „Seite“. Die Stellen der Kapitel sind ihre
        /// Kapitelköpfe, das Deckblatt steht aus Platzhaltern. Den Rumpf Zeile für Zeile hält die
        /// Messlatte des Vorlagenwegs (<see cref="BerichtVorlagenMesslatteTests"/>).
        /// </summary>
        [Theory]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_GRUPPE)]
        public void Standardvorlage_fuellt_Deckblatt_Kapitel_Kopf_und_Fusszeile(string probe)
        {
            byte[] vorlage = Repovorlage(BerichtsvorlageDateiWacheTests.STANDARD);
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ziel = Ziel("standard_" + probe + ".docx");
            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(probe);
            Fuellergebnis ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(
                daten, Berichtsdatenproben.VolleKonfiguration(), vorlage, new Erstellerangaben { Firma = FIRMA }, ziel);

            Assert.Equal(ziel, ergebnis.Zieldatei);
            Assert.Empty(Validierungsfehler(ziel));

            // Kopfzeile wie bisher bis auf das Logo (ohne eingestelltes Logo entfällt das Platzhalterbild);
            // Fußzeile mit Firma, Berichtsdatum statt DATE-Feld und „Seite“.
            List<string> struktur = Berichtsstruktur.Word(ziel);
            List<string> messlatte = Messlatte("Bericht_Word_" + probe + ".txt");
            string kopfSoll = messlatte.Single(z => z.StartsWith("Kopfzeile ", StringComparison.Ordinal));
            kopfSoll = kopfSoll.Substring(0, kopfSoll.IndexOf('⇥') + 1);
            Assert.StartsWith(kopfSoll, struktur.Single(z => z.StartsWith("Kopfzeile ", StringComparison.Ordinal)), StringComparison.Ordinal);
            string fussSoll = messlatte.Single(z => z.StartsWith("Fußzeile ", StringComparison.Ordinal))
                                       .Replace("{DATE \\@ \"dd.MM.yyyy\"}", Berichtsstruktur.DATUM);
            Assert.Equal("Fußzeile default: Absatz [—] " + FIRMA + "⇥" + Berichtsstruktur.DATUM + "⇥" + R.BV_TEXT_SEITE +
                         " {PAGE} / {NUMPAGES}", fussSoll);
            Assert.Equal(new[] { fussSoll }, struktur.Where(z => z.StartsWith("Fußzeile ", StringComparison.Ordinal)));

            using (WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false))
            {
                Assert.DoesNotContain("{{", AllerText(doc));
                Body rumpf = doc.MainDocumentPart.Document.Body;
                Assert.Empty(rumpf.Elements<SectionProperties>().Last().ElementsAfter());
                Assert.Empty(rumpf.Descendants<Run>().Where(IstGelb));
                AssertBildkennungenEindeutig(doc);
                Assert.NotNull(doc.MainDocumentPart.DocumentSettingsPart?.Settings?.GetFirstChild<UpdateFieldsOnOpen>());
                Assert.Null(doc.MainDocumentPart.WordprocessingCommentsPart);
                Assert.Equal(daten.Stammprojektname, ErsterMitStil(rumpf, "Title"));
            }

            Assert.Empty(ergebnis.Unbekannte);
            Assert.Empty(ergebnis.Fehler);
            Assert.False(ergebnis.OhnePlatzhalter);
            Assert.Empty(ergebnis.Hinweise.Where(h => h.Contains("angelegt", StringComparison.Ordinal)));

            // Die Stellen: die Kapitelköpfe der Vorlage, das Inhaltsverzeichnis mit eigener Überschrift, das
            // Deckblatt aus Platzhaltern.
            Assert.Equal("Deckblatt", ergebnis.Kapitelstellen[BerichtsKonfiguration.B_DECKBLATT]);
            Assert.Equal("Inhalt", ergebnis.Kapitelstellen[BerichtsKonfiguration.B_INHALT]);
            Assert.Equal("Projektbeschreibung", ergebnis.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);
            Assert.Equal("Berechnungsergebnisse je Variante", ergebnis.Kapitelstellen[BerichtsKonfiguration.B_ERGEBNISSE]);
            Assert.Equal("Wirtschaftlichkeit", ergebnis.Kapitelstellen[BerichtsKonfiguration.B_WIRTSCHAFT]);
            Assert.Equal(R.WIRT_AE_TITEL, ergebnis.Kapitelstellen[Berichtskapitel.ANHANG_E]);
            foreach (string zeile in ergebnis.Meldungen()) _ausgabe.WriteLine(zeile);
        }

        // =====================================================================
        //  (b) Beispielvorlage
        // =====================================================================

        /// <summary>
        /// Die Beispielvorlage (voller Aufbau, Katalog v2 kennt ihre Kapitel): jeder Einzelplatzhalter des
        /// Deckblatts gefüllt, keine Stelle bleibt gelb. Ohne gesetztes Häkchen entfällt jedes Kapitel samt
        /// seinem Kapitelkopf — keine verwaiste Überschrift. Die Kommentare der Vorlage sind entfernt.
        /// </summary>
        [Fact]
        public void Beispielvorlage_fuellt_das_Deckblatt_und_laesst_Kapitel_ohne_Haekchen_entfallen()
        {
            byte[] vorlage = Repovorlage(BerichtsvorlageDateiWacheTests.BEISPIEL);
            if (vorlage == null) return;

            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            daten.ErstelltAm = new DateTime(2026, 9, 25);
            string ziel = Ziel("beispiel.docx");
            Fuellergebnis e = Fuelle(vorlage, daten, Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body body = doc.MainDocumentPart.Document.Body;

            Assert.Equal("Stammprojekt", ErsterMitStil(body, "Title"));
            Assert.Equal(BerichtTexte.T(DeckblattBaustein.UNTERTITEL, false), ErsterMitStil(body, "Subtitle"));
            string[] zeilen = body.Elements<Table>().First().Elements<TableRow>()
                .Select(r => string.Join("|", r.Elements<TableCell>().Select(c => c.InnerText))).ToArray();
            Assert.Equal(new[]
            {
                R.BV_TEXT_KUNDE + "|",              // ohne Projektdaten: Leerwert "" (nie „0“)
                R.BV_TEXT_BEARBEITER + "|",
                R.BV_TEXT_ERSTELLER + "|" + FIRMA,
                R.BV_TEXT_VARIANTEN + "|Variante A",
                R.BV_TEXT_DATUM + "|25.09.2026",
            }, zeilen);
            List<string> hinweise = body.Elements<Paragraph>()
                .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == "Hinweis").Select(p => p.InnerText).ToList();
            Assert.Equal("", hinweise[0]);   // Gebäudemodell „leer statt strich“
            Assert.StartsWith(R.BV_TEXT_ERSTELLT_MIT + " EPOS-Plan ", hinweise[1]);

            // Kein Häkchen: kein Kapitel, kein Kapitelkopf, keine gelbe Stelle, kein Platzhalter.
            Assert.Empty(body.Descendants<Paragraph>().Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == "EPOSKapitelkopf"));
            Assert.Empty(body.Descendants<Run>().Where(IstGelb));
            Assert.Empty(e.Unbekannte);
            Assert.Empty(e.Fehler);
            Assert.DoesNotContain("{{", AllerText(doc));
            Assert.All(Berichtskapitel.Alle.Where(k => k.Name != Berichtskapitel.DECKBLATT),
                       k => Assert.Null(e.Kapitelstellen[k.Stellenschluessel]));
            Assert.Equal("Deckblatt", e.Kapitelstellen[BerichtsKonfiguration.B_DECKBLATT]);

            Assert.Null(doc.MainDocumentPart.WordprocessingCommentsPart);
            Assert.Empty(body.Descendants<CommentRangeStart>());
            Assert.Empty(body.Descendants<CommentReference>());

            // Ohne eingestelltes Logo entfällt der Bildplatzhalter der Kopfzeile samt Bildteil (BV-E2-1);
            // das Programm davor bleibt.
            Assert.Equal(1, e.Leere[Vorlagenfeldkatalog.LOGO]);
            Assert.All(doc.MainDocumentPart.HeaderParts, h => Assert.Empty(h.Header.Descendants<Drawing>()));
            Assert.Empty(doc.MainDocumentPart.HeaderParts.SelectMany(h => h.ImageParts));
            Assert.Contains(doc.MainDocumentPart.HeaderParts, h => h.Header.InnerText.StartsWith("EPOS-Plan", StringComparison.Ordinal));
        }

        /// <summary>
        /// Das Logo in der Standardvorlage (Entscheid BV-E2-1): Die Engine setzt das eingestellte Logo in den
        /// Rahmen des Bildplatzhalters der Kopfzeile (857250 × 466725 EMU) — seitentreu, ein Logo 3 : 1 füllt
        /// die Breite; der Alternativtext wird der Dateiname, das Platzhalterbild verschwindet aus dem Paket,
        /// die Bildkennungen bleiben eindeutig.
        /// </summary>
        [Fact]
        public void Standardvorlage_setzt_das_Logo_seitentreu_in_den_Rahmen_der_Kopfzeile()
        {
            byte[] vorlage = Repovorlage(BerichtsvorlageDateiWacheTests.STANDARD);
            if (vorlage == null) return;
            byte[] logo = Png(300, 100);
            string ziel = Ziel("standard_logo.docx");
            Fuellergebnis e = FuelleMit(vorlage, Berichtsdatenproben.Gruppendaten(2), Konfig(),
                                        new Erstellerangaben { Firma = FIRMA, Logo = logo, LogoDateiname = "firma.png" }, ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Empty(e.Warnungen);
            Assert.False(e.Leere.ContainsKey(Vorlagenfeldkatalog.LOGO));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            HeaderPart kopf = Assert.Single(doc.MainDocumentPart.HeaderParts, h => h.Header.Descendants<DW.Extent>().Any());
            DW.Extent ausdehnung = kopf.Header.Descendants<DW.Extent>().Single();
            Assert.Equal(857250L, ausdehnung.Cx.Value);
            Assert.Equal(285750L, ausdehnung.Cy.Value);
            Assert.Equal("firma.png", kopf.Header.Descendants<DW.DocProperties>().Single().Description.Value);
            var teil = (ImagePart)kopf.GetPartById(kopf.Header.Descendants<A.Blip>().Single().Embed.Value);
            Assert.Equal("image/png", teil.ContentType);
            using (Stream s = teil.GetStream()) Assert.Equal(logo.Length, s.Length);
            Assert.DoesNotContain(doc.GetAllParts(),
                                  p => p.Uri.OriginalString.EndsWith("logoplatzhalter.png", StringComparison.OrdinalIgnoreCase));
            AssertBildkennungenEindeutig(doc);
        }

        // =====================================================================
        //  (c) Schmutzige Vorlagen
        // =====================================================================

        /// <summary>
        /// Messprobe 4: der Platzhalter auf drei Runs verteilt, der mittlere fett. Er wird in den Run
        /// mit <c>{{</c> gezogen und trägt dessen Format; der Rest des letzten Runs bleibt in seinem
        /// Format, der fette Run entfällt.
        /// </summary>
        [Fact]
        public void Zerlegte_Runs_werden_zusammengezogen_und_behalten_das_Format_des_ersten()
        {
            byte[] v = Vorlage(main =>
                "<w:p>" +
                "<w:r><w:rPr><w:i/></w:rPr><w:t xml:space=\"preserve\">Projekt: {{proj</w:t></w:r>" +
                "<w:proofErr w:type=\"spellStart\"/>" +
                "<w:r><w:rPr><w:b/></w:rPr><w:t>ekt.na</w:t></w:r>" +
                "<w:proofErr w:type=\"spellEnd\"/>" +
                "<w:r><w:rPr><w:u w:val=\"single\"/></w:rPr><w:t xml:space=\"preserve\">me}} und mehr</w:t></w:r>" +
                "</w:p>" + ABSCHNITT);
            string ziel = Ziel("zerlegt.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Equal(1, e.Ersetzt);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Paragraph p = doc.MainDocumentPart.Document.Body.Elements<Paragraph>().First();
            Assert.Equal("Projekt: Stammprojekt und mehr", p.InnerText);
            List<Run> laeufe = p.Elements<Run>().ToList();
            Assert.Equal(2, laeufe.Count);
            Assert.Equal("Projekt: Stammprojekt", laeufe[0].InnerText);
            Assert.NotNull(laeufe[0].RunProperties?.Italic);
            Assert.Null(laeufe[0].RunProperties?.Bold);
            Assert.Equal(" und mehr", laeufe[1].InnerText);
            Assert.NotNull(laeufe[1].RunProperties?.Underline);
        }

        /// <summary>
        /// Platzhalter in einem fetten Wort: Der Run mit <c>{{</c> ist fett, der Wert wird es auch;
        /// ein Tabulator trennt — ein Platzhalter über ihn hinweg bleibt unerkannter Text.
        /// </summary>
        [Fact]
        public void Platzhalter_im_fetten_Wort_behaelt_das_Format_und_ein_Tabulator_trennt()
        {
            byte[] v = Vorlage(main =>
                "<w:p><w:r><w:t xml:space=\"preserve\">Titel </w:t></w:r>" +
                "<w:r><w:rPr><w:b/></w:rPr><w:t>X{{bericht.</w:t></w:r>" +
                "<w:r><w:rPr><w:b/></w:rPr><w:t>titel}}Y</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t>{{bericht.</w:t></w:r><w:r><w:tab/></w:r><w:r><w:t>titel}}</w:t></w:r></w:p>" +
                ABSCHNITT);
            string ziel = Ziel("fett.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<Paragraph> absaetze = doc.MainDocumentPart.Document.Body.Elements<Paragraph>().ToList();
            Assert.Equal("Titel XStammprojektY", absaetze[0].InnerText);
            Run wert = absaetze[0].Elements<Run>().Single(r => r.InnerText.Contains("Stammprojekt"));
            Assert.NotNull(wert.RunProperties?.Bold);
            Assert.Equal("{{bericht.titel}}", absaetze[1].InnerText);   // über den Tabulator: bleibt
            Assert.Equal(1, e.Ersetzt);
        }

        /// <summary>
        /// Textfeld (VML) im Rumpf und <c>mc:AlternateContent</c> mit Textfeld in beiden Zweigen: jeder
        /// Absatz darin wird gefüllt. Dazu ein Bild im Rumpf und eines in der Kopfzeile, beide mit
        /// <c>docPr/@id</c> = 1 wie in der Vorlage — danach sind alle Kennungen eindeutig.
        /// </summary>
        [Fact]
        public void Textfelder_und_beide_Zweige_von_AlternateContent_werden_gefuellt_Bildkennungen_eindeutig()
        {
            byte[] v = Vorlage(main =>
            {
                HeaderPart kopf = main.AddNewPart<HeaderPart>();
                ImagePart logo = kopf.AddImagePart(ImagePartType.Png);
                using (var s = new MemoryStream(Convert.FromBase64String(PNG_1X1))) logo.FeedData(s);
                kopf.Header = new Header("<w:hdr " + NS + "><w:p><w:r><w:t>{{ersteller.programm}}</w:t></w:r>" +
                                         Bild(kopf.GetIdOfPart(logo), 1) + "</w:p></w:hdr>");
                ImagePart bild = main.AddImagePart(ImagePartType.Png);
                using (var s = new MemoryStream(Convert.FromBase64String(PNG_1X1))) bild.FeedData(s);
                return "<w:p><w:r><w:t>Vor dem Textfeld</w:t></w:r>" +
                       "<w:r><w:pict><v:shape id=\"Textfeld1\" style=\"width:150pt;height:40pt\"><v:textbox><w:txbxContent>" +
                       "<w:p><w:r><w:t>VML: {{bericht.titel}}</w:t></w:r></w:p></w:txbxContent></v:textbox></v:shape></w:pict></w:r></w:p>" +
                       "<w:p><w:r><mc:AlternateContent><mc:Choice Requires=\"wps\">" + WpsTextfeld("Wahl: {{projekt.name}}", 7) +
                       "</mc:Choice><mc:Fallback><w:pict><v:shape id=\"Textfeld2\" style=\"width:150pt;height:40pt\"><v:textbox><w:txbxContent>" +
                       "<w:p><w:r><w:t>Rückfall: {{projekt.name}}</w:t></w:r></w:p></w:txbxContent></v:textbox></v:shape></w:pict>" +
                       "</mc:Fallback></mc:AlternateContent></w:r></w:p>" +
                       "<w:p>" + Bild(main.GetIdOfPart(bild), 1) + "</w:p>" +
                       "<w:sectPr><w:headerReference w:type=\"default\" r:id=\"" + main.GetIdOfPart(kopf) + "\"/>" + SEITE + "</w:sectPr>";
            });
            string ziel = Ziel("textfeld.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Empty(e.Unbekannte);
            Assert.Equal(4, e.Ersetzt);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> textfelder = doc.MainDocumentPart.Document.Body.Descendants<TextBoxContent>()
                .Select(t => t.InnerText).ToList();
            Assert.Equal(new[] { "VML: Stammprojekt", "Wahl: Stammprojekt", "Rückfall: Stammprojekt" }, textfelder);
            Assert.DoesNotContain("{{", AllerText(doc));
            Assert.StartsWith("EPOS-Plan", doc.MainDocumentPart.HeaderParts.Single().Header.InnerText, StringComparison.Ordinal);
            AssertBildkennungenEindeutig(doc);
        }

        /// <summary>
        /// Kopfzeile nur auf Seite 1, zwei Abschnitte: Jede Kopfzeile jedes Abschnitts wird gefüllt;
        /// ein unbekannter Schlüssel in der Kopfzeile der ersten Seite nennt Abschnitt und Art.
        /// Dazu eine Fußnote.
        /// </summary>
        [Fact]
        public void Kopfzeilen_aller_Abschnitte_und_Fussnoten_werden_gefuellt()
        {
            byte[] v = Vorlage(main =>
            {
                string Kopf(string text)
                {
                    HeaderPart h = main.AddNewPart<HeaderPart>();
                    h.Header = new Header("<w:hdr " + NS + "><w:p><w:r><w:t xml:space=\"preserve\">" + text + "</w:t></w:r></w:p></w:hdr>");
                    return main.GetIdOfPart(h);
                }
                string erste = Kopf("{{bericht.titel}} {{kopf.unbekannt}}");
                string standard = Kopf("{{ersteller.programm}}");
                string zweiter = Kopf("{{text.seite}}");
                FootnotesPart fn = main.AddNewPart<FootnotesPart>();
                fn.Footnotes = new Footnotes("<w:footnotes " + NS + ">" +
                    "<w:footnote w:type=\"separator\" w:id=\"-1\"><w:p><w:r><w:separator/></w:r></w:p></w:footnote>" +
                    "<w:footnote w:type=\"continuationSeparator\" w:id=\"0\"><w:p><w:r><w:continuationSeparator/></w:r></w:p></w:footnote>" +
                    "<w:footnote w:id=\"1\"><w:p><w:r><w:footnoteRef/></w:r><w:r><w:t xml:space=\"preserve\"> Quelle: {{projekt.name}}</w:t></w:r></w:p></w:footnote>" +
                    "</w:footnotes>");
                return "<w:p><w:r><w:t>Deckblatt</w:t></w:r><w:r><w:footnoteReference w:id=\"1\"/></w:r></w:p>" +
                       "<w:p><w:pPr><w:sectPr><w:headerReference w:type=\"default\" r:id=\"" + standard + "\"/>" +
                       "<w:headerReference w:type=\"first\" r:id=\"" + erste + "\"/>" + SEITE + "<w:titlePg/></w:sectPr></w:pPr></w:p>" +
                       "<w:p><w:r><w:t>Seite zwei</w:t></w:r></w:p>" +
                       "<w:sectPr><w:headerReference w:type=\"default\" r:id=\"" + zweiter + "\"/>" + SEITE + "</w:sectPr>";
            });
            string ziel = Ziel("abschnitte.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> koepfe = doc.MainDocumentPart.HeaderParts.Select(h => h.Header.InnerText).OrderBy(t => t, StringComparer.Ordinal).ToList();
            Assert.Equal(new[] { "EPOS-Plan", R.BV_TEXT_SEITE, "Stammprojekt {{kopf.unbekannt}}" }.OrderBy(t => t, StringComparer.Ordinal), koepfe);
            Assert.Equal(" Quelle: Stammprojekt", doc.MainDocumentPart.FootnotesPart.Footnotes.Elements<Footnote>().Last().InnerText);

            Fuellbefund befund = Assert.Single(e.Unbekannte);
            Assert.Equal("{{kopf.unbekannt}}", befund.Normalform);
            Assert.Equal(Fuellbefundart.Unbekannt, befund.Art);
            Assert.StartsWith(TX.F(false, TX.FUNDORT_KOPF_ERSTE, 1, 1), befund.Fundort, StringComparison.Ordinal);
            Assert.Equal(4, e.Ersetzt);
        }

        /// <summary>
        /// Inhaltssteuerelemente mit einem Schlüssel als Tag (Konzept 6.6): im Satz mit dem eigenen
        /// Zeichenformat des Steuerelements, ohne das graue Platzhalterformat; als Block im Format des
        /// ersten Absatzes; <c>bericht.inhalt</c> als Block setzt die Kapitel. Alle drei sind danach
        /// ausgepackt; ein Steuerelement mit fremdem Tag bleibt, wie es ist.
        /// </summary>
        [Fact]
        public void Inhaltssteuerelemente_werden_gefuellt_und_ausgepackt()
        {
            byte[] v = Vorlage(main =>
                "<w:p><w:r><w:t xml:space=\"preserve\">Projekt: </w:t></w:r>" +
                "<w:sdt><w:sdtPr><w:rPr><w:b/></w:rPr><w:alias w:val=\"Projekt\"/><w:tag w:val=\"projekt.name\"/><w:id w:val=\"11\"/>" +
                "<w:showingPlcHdr/><w:text/></w:sdtPr><w:sdtContent><w:r><w:rPr><w:rStyle w:val=\"PlaceholderText\"/></w:rPr>" +
                "<w:t>Klicken Sie hier.</w:t></w:r></w:sdtContent></w:sdt>" +
                "<w:r><w:t xml:space=\"preserve\">, Stand heute.</w:t></w:r></w:p>" +
                "<w:sdt><w:sdtPr><w:alias w:val=\"Titel\"/><w:tag w:val=\"{{bericht.titel}}\"/><w:id w:val=\"12\"/></w:sdtPr>" +
                "<w:sdtContent><w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:t>Mustertitel</w:t></w:r></w:p></w:sdtContent></w:sdt>" +
                "<w:sdt><w:sdtPr><w:tag w:val=\"bericht.inhalt\"/><w:id w:val=\"13\"/></w:sdtPr>" +
                "<w:sdtContent><w:p><w:r><w:t>Hier stehen die Kapitel.</w:t></w:r></w:p></w:sdtContent></w:sdt>" +
                "<w:sdt><w:sdtPr><w:tag w:val=\"Kunde\"/><w:id w:val=\"14\"/></w:sdtPr>" +
                "<w:sdtContent><w:p><w:r><w:t>eigenes Steuerelement</w:t></w:r></w:p></w:sdtContent></w:sdt>" +
                ABSCHNITT);
            string ziel = Ziel("sdt.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_ANHANG), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Empty(e.Unbekannte);
            Assert.Equal(3, e.Ersetzt);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body body = doc.MainDocumentPart.Document.Body;

            SdtElement fremd = Assert.Single(body.Descendants<SdtElement>());
            Assert.Equal("Kunde", fremd.SdtProperties.GetFirstChild<Tag>().Val.Value);

            Paragraph satz = body.Elements<Paragraph>().First();
            Assert.Equal("Projekt: Stammprojekt, Stand heute.", satz.InnerText);
            Run wert = satz.Elements<Run>().Single(r => r.InnerText == "Stammprojekt");
            Assert.NotNull(wert.RunProperties?.Bold);
            Assert.Null(wert.RunProperties?.RunStyle);

            Paragraph titel = body.Elements<Paragraph>().Single(p => p.InnerText == "Stammprojekt");
            Assert.Equal(JustificationValues.Center, titel.ParagraphProperties?.Justification?.Val?.Value);

            Assert.Contains(body.Elements<Paragraph>(), p => p.InnerText == "Anhang");
            Assert.DoesNotContain(body.Elements<Paragraph>(), p => p.InnerText.Contains("Hier stehen die Kapitel", StringComparison.Ordinal));
            Assert.Empty(body.Descendants<ShowingPlaceholder>());
        }

        /// <summary>
        /// Ein Steuerelement des Anwenders (Tag kein Schlüssel) mit getipptem Platzhalter bleibt
        /// stehen, verliert nach dem Füllen aber <c>w:showingPlcHdr</c> und <c>w:dataBinding</c> —
        /// sonst zeigte Word den Wert grau oder überschriebe ihn mit der gebundenen Eigenschaft. Ein
        /// Steuerelement ohne Platzhalter bleibt unberührt.
        /// </summary>
        [Fact]
        public void Gebundenes_Steuerelement_des_Anwenders_verliert_Bindung_und_Platzhalteranzeige()
        {
            byte[] v = Vorlage(main =>
                "<w:sdt><w:sdtPr><w:alias w:val=\"Titel\"/><w:tag w:val=\"Titel\"/><w:id w:val=\"31\"/><w:showingPlcHdr/>" +
                "<w:dataBinding w:prefixMappings=\"xmlns:ns0='http://purl.org/dc/elements/1.1/'\" w:xpath=\"/ns0:coreProperties[1]/ns0:title[1]\" " +
                "w:storeItemID=\"{6C3C8BC8-F283-45AE-878A-BAB7291924A1}\"/><w:text/></w:sdtPr>" +
                "<w:sdtContent><w:p><w:r><w:t>{{bericht.titel}}</w:t></w:r></w:p></w:sdtContent></w:sdt>" +
                "<w:sdt><w:sdtPr><w:tag w:val=\"Frei\"/><w:id w:val=\"32\"/><w:showingPlcHdr/></w:sdtPr>" +
                "<w:sdtContent><w:p><w:r><w:t>Unberührt</w:t></w:r></w:p></w:sdtContent></w:sdt>" + ABSCHNITT);
            string ziel = Ziel("gebunden.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Equal(1, e.Ersetzt);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<SdtBlock> steuer = doc.MainDocumentPart.Document.Body.Elements<SdtBlock>().ToList();
            Assert.Equal(2, steuer.Count);
            Assert.Equal("Stammprojekt", steuer[0].Descendants<Paragraph>().Single().InnerText);
            Assert.Null(steuer[0].SdtProperties.GetFirstChild<ShowingPlaceholder>());
            Assert.Null(steuer[0].SdtProperties.GetFirstChild<DataBinding>());
            Assert.NotNull(steuer[1].SdtProperties.GetFirstChild<ShowingPlaceholder>());
        }

        /// <summary>
        /// Vorlage aus dem deutschen Word (Messprobe 5): Stil-IDs <c>Standard</c>, <c>Titel</c>,
        /// <c>Untertitel</c>, <c>berschrift1</c> bis <c>berschrift3</c>, die Beschriftung als
        /// eingebautes <c>caption</c>. Die Kapitel schreiben über die Rollenauflösung genau diese
        /// IDs; der fehlende Hinweisstil wird als „EPOS Hinweis“ angelegt und gemeldet.
        /// </summary>
        [Fact]
        public void Vorlage_mit_deutschen_Stil_IDs_loest_die_Rollen_auf()
        {
            byte[] v = Vorlage(main =>
            {
                StyleDefinitionsPart stile = main.AddNewPart<StyleDefinitionsPart>();
                stile.Styles = new Styles(
                    DeutscherStil("Standard", "Normal", null, standard: true),
                    DeutscherStil("berschrift1", "heading 1", 0),
                    DeutscherStil("berschrift2", "heading 2", 1),
                    DeutscherStil("berschrift3", "heading 3", 2),
                    DeutscherStil("Titel", "Title", null),
                    DeutscherStil("Untertitel", "Subtitle", null),
                    DeutscherStil("Beschriftung", "caption", null));
                return "<w:p><w:r><w:t>{{bericht.inhalt}}</w:t></w:r></w:p>" + ABSCHNITT;
            });
            string ziel = Ziel("deutsch.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_DECKBLATT, BerichtsKonfiguration.B_ANHANG), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<Style> stilliste = doc.MainDocumentPart.StyleDefinitionsPart.Styles.Elements<Style>().ToList();
            var definiert = new HashSet<string>(stilliste.Select(s => s.StyleId.Value));
            List<string> benutzt = doc.MainDocumentPart.Document.Body.Descendants<ParagraphStyleId>()
                .Select(p => p.Val.Value).Distinct().ToList();

            Assert.All(benutzt, id => Assert.Contains(id, definiert));
            foreach (string id in new[] { "Titel", "Untertitel", "berschrift1", "berschrift2", "Standard", "EPOSHinweis" })
                Assert.Contains(id, benutzt);
            foreach (string id in new[] { "Title", "Subtitle", "Heading1", "Heading2", "Normal", "Hinweis" })
                Assert.DoesNotContain(id, benutzt);

            Style hinweis = Assert.Single(stilliste, s => s.StyleName?.Val?.Value == WordVorlagenstile.NAME_HINWEIS);
            Assert.Equal("Standard", hinweis.BasedOn.Val.Value);
            Assert.Contains(TX.F(false, TX.STIL_ANGELEGT, WordVorlagenstile.NAME_HINWEIS), e.Hinweise);
            Assert.Single(e.Hinweise, h => h.Contains("angelegt", StringComparison.Ordinal));
        }

        /// <summary>
        /// Rollenauflösung einzeln: eingebaute Namen ohne Groß-/Kleinschreibung, der Standardabsatz
        /// über <c>w:default</c>, Beschriftung über den eigenen Namen vor <c>caption</c>; der Abstand
        /// nimmt nie die eingebaute Beschriftung, sondern bekommt „EPOS Abstand“. Ohne Auflösung
        /// bleibt die feste ID (Abstand = Beschriftung).
        /// </summary>
        [Fact]
        public void Rollen_werden_ueber_den_Namen_aufgeloest_und_nur_fehlende_angelegt()
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            main.AddNewPart<StyleDefinitionsPart>().Styles = new Styles(
                DeutscherStil("Standard", "Normal", null, standard: true),
                DeutscherStil("berschrift1", "HEADING 1", 0),
                DeutscherStil("Beschriftung", "caption", null),
                DeutscherStil("MeinHinweis", "Hinweis", null));

            var stile = new WordVorlagenstile(main);
            Assert.Equal("Standard", stile.Id(WordVorlagenstile.STANDARD));
            Assert.Equal("berschrift1", stile.Id(WordVorlagenstile.UEBERSCHRIFT1));
            Assert.Equal("Beschriftung", stile.Id(WordVorlagenstile.BESCHRIFTUNG));
            Assert.Equal("MeinHinweis", stile.Id(WordVorlagenstile.HINWEIS));
            Assert.Null(stile.Finde(WordVorlagenstile.KAPITELKOPF));
            Assert.Null(stile.Id(WordVorlagenstile.KAPITELKOPF));
            Assert.Empty(stile.Angelegt);

            Assert.Equal("EPOSAbstand", stile.Id(WordVorlagenstile.ABSTAND));
            Assert.Equal("Heading2", stile.Id(WordVorlagenstile.UEBERSCHRIFT2));
            Assert.Equal(new[] { WordVorlagenstile.NAME_ABSTAND, "heading 2" }, stile.Angelegt);
            Style h2 = main.StyleDefinitionsPart.Styles.Elements<Style>().Single(s => s.StyleId.Value == "Heading2");
            Assert.Equal(1, h2.StyleParagraphProperties.OutlineLevel.Val.Value);
            Assert.Equal("Standard", h2.BasedOn.Val.Value);

            Assert.Equal("Eigene", stile.Id("Eigene"));   // keine Rolle: unverändert
            Assert.Equal(WordVorlagenstile.BESCHRIFTUNG, WordVorlagenstile.FesteId(WordVorlagenstile.ABSTAND));
            Assert.Equal("Heading1", WordVorlagenstile.FesteId("Heading1"));
        }

        /// <summary>Konzept 6.1: ohne jeden Platzhalter Warnung, die Kapitel kommen ans Ende vor die
        /// Abschnittsangabe, nichts wird gelöscht.</summary>
        [Fact]
        public void Vorlage_ohne_Platzhalter_bekommt_die_Kapitel_am_Ende_und_verliert_nichts()
        {
            byte[] v = Vorlage(main =>
                "<w:p><w:r><w:t>Mein Deckblatt</w:t></w:r></w:p><w:p><w:r><w:t>Mein Vorwort</w:t></w:r></w:p>" + ABSCHNITT);
            string ziel = Ziel("ohne.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_ANHANG), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.True(e.OhnePlatzhalter);
            Assert.Contains(TX.T(TX.OHNE_PLATZHALTER, false), e.Warnungen);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body body = doc.MainDocumentPart.Document.Body;
            List<Paragraph> absaetze = body.Elements<Paragraph>().ToList();
            Assert.Equal("Mein Deckblatt", absaetze[0].InnerText);
            Assert.Equal("Mein Vorwort", absaetze[1].InnerText);
            Assert.Equal("Anhang", absaetze[2].InnerText);
            Assert.IsType<SectionProperties>(body.ChildElements.Last());
            Assert.DoesNotContain("{{", AllerText(doc));
        }

        /// <summary>Konzept 6.7: Kommentare werden weder ersetzt noch geprüft — die Engine entfernt alle
        /// samt Bereichsmarken, Verweisen und Teil und nennt ihre Zahl.</summary>
        [Fact]
        public void Kommentare_werden_entfernt_und_gezaehlt()
        {
            byte[] v = Vorlage(main =>
            {
                WordprocessingCommentsPart teil = main.AddNewPart<WordprocessingCommentsPart>();
                teil.Comments = new Comments(
                    new Comment(new Paragraph(new Run(new Text("Erläuterung {{projekt.name}}")))) { Id = "0", Author = "EPOS-Plan", Initials = "EP" },
                    new Comment(new Paragraph(new Run(new Text("Zweiter")))) { Id = "1", Author = "EPOS-Plan", Initials = "EP" });
                return "<w:p><w:commentRangeStart w:id=\"0\"/><w:r><w:t>{{bericht.titel}}</w:t></w:r><w:commentRangeEnd w:id=\"0\"/>" +
                       "<w:r><w:commentReference w:id=\"0\"/></w:r></w:p>" +
                       "<w:p><w:commentRangeStart w:id=\"1\"/><w:r><w:t>Text</w:t></w:r><w:commentRangeEnd w:id=\"1\"/>" +
                       "<w:r><w:commentReference w:id=\"1\"/></w:r></w:p>" + ABSCHNITT;
            });
            string ziel = Ziel("kommentare.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Equal(2, e.EntfernteKommentare);
            Assert.Contains(TX.F(false, TX.KOMMENTARE, 2), e.Hinweise);
            Assert.Equal(1, e.Ersetzt);   // der Platzhalter im Kommentar zählt nicht
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body body = doc.MainDocumentPart.Document.Body;
            Assert.Null(doc.MainDocumentPart.WordprocessingCommentsPart);
            Assert.Empty(body.Descendants<CommentRangeStart>());
            Assert.Empty(body.Descendants<CommentRangeEnd>());
            Assert.Empty(body.Descendants<CommentReference>());
            Assert.Equal(new[] { "Stammprojekt", "Text" }, body.Elements<Paragraph>().Select(p => p.InnerText));
        }

        /// <summary>
        /// BV-E1 B1a: Engine und Prüfer zählen Kommentare mit EINER Zählung
        /// (<see cref="Vorlagenteile.Kommentarzahl"/>) — die Prüfzeile und die Laufmeldung nennen
        /// dieselbe Zahl. Die Probe trägt zwei Einträge im Kommentarteil und drei Bereichsmarken; die
        /// dritte Marke ohne Eintrag entfällt beim Füllen mit, zählt aber nicht (Word zeigt sie nicht
        /// als Kommentar). Gegenprobe: die Beispielvorlage des Repositoriums.
        /// </summary>
        [Fact]
        public void Kommentare_zaehlen_in_Engine_und_Pruefer_gleich()
        {
            byte[] v = Vorlage(main =>
            {
                WordprocessingCommentsPart teil = main.AddNewPart<WordprocessingCommentsPart>();
                teil.Comments = new Comments(
                    new Comment(new Paragraph(new Run(new Text("Erster")))) { Id = "0", Author = "EPOS-Plan", Initials = "EP" },
                    new Comment(new Paragraph(new Run(new Text("Zweiter")))) { Id = "1", Author = "EPOS-Plan", Initials = "EP" });
                return "<w:p><w:commentRangeStart w:id=\"0\"/><w:r><w:t>{{bericht.titel}}</w:t></w:r><w:commentRangeEnd w:id=\"0\"/>" +
                       "<w:r><w:commentReference w:id=\"0\"/></w:r></w:p>" +
                       "<w:p><w:commentRangeStart w:id=\"1\"/><w:r><w:t>Text</w:t></w:r><w:commentRangeEnd w:id=\"1\"/>" +
                       "<w:r><w:commentReference w:id=\"1\"/></w:r></w:p>" +
                       "<w:p><w:commentRangeStart w:id=\"2\"/><w:r><w:t>Ohne Eintrag</w:t></w:r><w:commentRangeEnd w:id=\"2\"/>" +
                       "<w:r><w:commentReference w:id=\"2\"/></w:r></w:p>" + ABSCHNITT;
            });
            Pruefbefund befund = Vorlagenpruefer.Pruefe(v, Pruefstufe.Schnell, null);
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), Ziel("kommentare_zaehlung.docx"));

            Assert.Equal(2, befund.Kommentare);
            Assert.Equal(befund.Kommentare, e.EntfernteKommentare);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(e.Zieldatei, false))
            {
                Body body = doc.MainDocumentPart.Document.Body;
                Assert.Empty(body.Descendants<CommentRangeStart>());
                Assert.Empty(body.Descendants<CommentReference>());
            }

            byte[] beispiel = Repovorlage(BerichtsvorlageDateiWacheTests.BEISPIEL);
            if (beispiel == null) return;
            Fuellergebnis b = Fuelle(beispiel, Gruppe(), Konfig(), Ziel("beispiel_zaehlung.docx"));
            Assert.Equal(Vorlagenpruefer.Pruefe(beispiel, Pruefstufe.Schnell, null).Kommentare, b.EntfernteKommentare);
        }

        /// <summary>Konzept 6.1: ein verknüpftes Bild wird entfernt und als Warnung gemeldet, der Verweis
        /// auf die Dokumentvorlage als Hinweis; keine externe Beziehung bleibt, Hyperlinks bleiben.</summary>
        [Fact]
        public void Verknuepftes_Bild_und_Dokumentvorlage_werden_entfernt_und_gemeldet()
        {
            const string BILD = "file:///C:/Bilder/Logo.png";
            const string DOTM = "file:///C:/Vorlagen/Normal.dotm";
            byte[] v = Vorlage(main =>
            {
                string bild = main.AddExternalRelationship(
                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image", new Uri(BILD)).Id;
                string link = main.AddHyperlinkRelationship(new Uri("https://www.epos-plan.de"), true).Id;
                DocumentSettingsPart einst = main.AddNewPart<DocumentSettingsPart>();
                string vorlage = einst.AddExternalRelationship(
                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships/attachedTemplate", new Uri(DOTM)).Id;
                einst.Settings = new Settings("<w:settings " + NS + "><w:attachedTemplate r:id=\"" + vorlage + "\"/></w:settings>");
                return "<w:p><w:r><w:t>{{bericht.titel}}</w:t></w:r></w:p>" +
                       "<w:p>" + Bild(null, 3, bild) + "</w:p>" +
                       "<w:p><w:hyperlink r:id=\"" + link + "\"><w:r><w:t xml:space=\"preserve\">Netz {{proj</w:t></w:r>" +
                       "<w:r><w:rPr><w:b/></w:rPr><w:t>ekt.name}}</w:t></w:r></w:hyperlink></w:p>" + ABSCHNITT;
            });
            string ziel = Ziel("verknuepft.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Contains(TX.F(false, TX.VERKNUEPFTES_BILD, BILD), e.Warnungen);
            Assert.Contains(TX.F(false, TX.DOKUMENTVORLAGE, DOTM), e.Hinweise);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Assert.All(doc.GetAllParts(), t => Assert.Empty(t.ExternalRelationships));
            Assert.Empty(doc.MainDocumentPart.Document.Body.Descendants<Drawing>());
            Assert.Null(doc.MainDocumentPart.DocumentSettingsPart.Settings.GetFirstChild<AttachedTemplate>());
            Assert.Single(doc.MainDocumentPart.HyperlinkRelationships);
            Assert.Equal("Stammprojekt", doc.MainDocumentPart.Document.Body.Elements<Paragraph>().First().InnerText);
            // Im Hyperlink wird auch ein zerlegter Platzhalter gefüllt; der Hyperlink bleibt.
            Hyperlink verweis = Assert.Single(doc.MainDocumentPart.Document.Body.Descendants<Hyperlink>());
            Assert.Equal("Netz Stammprojekt", verweis.InnerText);
            Assert.Equal(2, e.Ersetzt);
        }

        /// <summary>
        /// Unbekannt, Kontextverstoß (<c>stand.*</c> außerhalb eines Standblocks), Blockmarken ohne
        /// Gegenstück: Der Platzhalter bleibt stehen, gelb, in einem eigenen Run im Format des
        /// Platzhalters; der Text um ihn bleibt in seinen Runs. Das Ergebnis nennt jeden mit Grund.
        /// </summary>
        [Fact]
        public void Unbekannte_Kontextfehler_und_offene_Bloecke_bleiben_gelb_stehen()
        {
            byte[] v = Vorlage(main =>
                "<w:p><w:r><w:rPr><w:i/></w:rPr><w:t xml:space=\"preserve\">A {{gibt.es.nicht}} B {{stand.anzeige}} C {{bericht.titel}}</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t>{{/je}}</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t>{{#je stand}}</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t>{{}}</w:t></w:r></w:p>" + ABSCHNITT);
            string ziel = Ziel("unbekannt.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Equal(1, e.Ersetzt);
            Assert.Equal(new[]
            {
                ("{{gibt.es.nicht}}", Fuellbefundart.Unbekannt),
                ("{{stand.anzeige}}", Fuellbefundart.Kontext),
                ("{{/je}}", Fuellbefundart.Block),
                ("{{#je stand}}", Fuellbefundart.Block),
                ("{{}}", Fuellbefundart.Unbekannt),
            }, e.Unbekannte.Select(b => (b.Normalform, b.Art)));
            Assert.Equal(TX.T(TX.GRUND_KONTEXT, false), e.Unbekannte[1].Grund);
            Assert.Equal(TX.T(TX.GRUND_BLOCK, false), e.Unbekannte[2].Grund);
            Assert.Contains(e.Fehler, f => f.StartsWith("{{#je stand}}: Der Block hat kein passendes Ende", StringComparison.Ordinal));

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Paragraph erster = doc.MainDocumentPart.Document.Body.Elements<Paragraph>().First();
            Assert.Equal(new[] { "A ", "{{gibt.es.nicht}}", " B ", "{{stand.anzeige}}", " C Stammprojekt" },
                         erster.Elements<Run>().Select(r => r.InnerText));
            Assert.Equal(new[] { false, true, false, true, false }, erster.Elements<Run>().Select(IstGelb));
            Assert.All(erster.Elements<Run>(), r => Assert.NotNull(r.RunProperties?.Italic));
            Assert.Equal(5, doc.MainDocumentPart.Document.Body.Descendants<Run>().Count(IstGelb));
        }

        /// <summary>
        /// Liste (v1: <c>bericht.warnungen</c>): allein im Absatz je Eintrag ein Absatz im Format des
        /// Platzhalterabsatzes und -runs, im Satz mit „; “ verbunden; in der Kopfzeile ein Fehler.
        /// </summary>
        [Fact]
        public void Liste_wird_zu_Absaetzen_im_Satz_verbunden_und_in_der_Kopfzeile_abgelehnt()
        {
            byte[] v = Vorlage(main =>
            {
                HeaderPart kopf = main.AddNewPart<HeaderPart>();
                kopf.Header = new Header("<w:hdr " + NS + "><w:p><w:r><w:t>{{bericht.warnungen}}</w:t></w:r></w:p></w:hdr>");
                return "<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:i/></w:rPr><w:t>{{bericht.warnungen}}</w:t></w:r></w:p>" +
                       "<w:p><w:r><w:t xml:space=\"preserve\">Warnungen: {{bericht.warnungen}}.</w:t></w:r></w:p>" +
                       "<w:sectPr><w:headerReference w:type=\"default\" r:id=\"" + main.GetIdOfPart(kopf) + "\"/>" + SEITE + "</w:sectPr>";
            });
            BerichtsDaten daten = Gruppe();
            daten.Warnungen.Add("Erste Warnung");
            daten.Warnungen.Add("Zweite Warnung");
            string ziel = Ziel("liste.docx");
            Fuellergebnis e = Fuelle(v, daten, Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<Paragraph> absaetze = doc.MainDocumentPart.Document.Body.Elements<Paragraph>().ToList();
            Assert.Equal(new[] { "Erste Warnung", "Zweite Warnung", "Warnungen: Erste Warnung; Zweite Warnung." },
                         absaetze.Select(p => p.InnerText));
            for (int i = 0; i < 2; i++)
            {
                Assert.Equal(JustificationValues.Center, absaetze[i].ParagraphProperties?.Justification?.Val?.Value);
                Assert.NotNull(absaetze[i].Elements<Run>().Single().RunProperties?.Italic);
            }
            Assert.Equal("{{bericht.warnungen}}", doc.MainDocumentPart.HeaderParts.Single().Header.InnerText);
            Fuellbefund befund = Assert.Single(e.Unbekannte);
            Assert.Equal(Fuellbefundart.FalscheStelle, befund.Art);
            Assert.Single(e.Fehler);
            Assert.Equal(2, e.Ersetzt);
        }

        /// <summary>
        /// Eine Dokumentvorlage (<c>.dotx</c>) wird als Dokument gespeichert (Hinweis); eine Vorlage
        /// mit Makros und eine Datei, die kein Word-Dokument ist, werden benannt abgelehnt.
        /// </summary>
        [Fact]
        public void Dotx_wird_Dokument_Makros_und_Fremdes_werden_abgelehnt()
        {
            byte[] dotx = Vorlage(main => "<w:p><w:r><w:t>{{bericht.titel}}</w:t></w:r></w:p>" + ABSCHNITT,
                                  WordprocessingDocumentType.Template);
            string ziel = Ziel("aus_dotx.docx");
            Fuellergebnis e = Fuelle(dotx, Gruppe(), Konfig(), ziel);
            Assert.Contains(TX.T(TX.DOTX, false), e.Hinweise);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false))
            {
                Assert.Equal(WordprocessingDocumentType.Document, doc.DocumentType);
                Assert.Equal("Stammprojekt", doc.MainDocumentPart.Document.Body.InnerText);
            }
            Assert.Empty(Validierungsfehler(ziel));

            byte[] docm = Vorlage(main => "<w:p/>" + ABSCHNITT, WordprocessingDocumentType.MacroEnabledDocument);
            var makros = Assert.Throws<NotSupportedException>(() => Fuelle(docm, Gruppe(), Konfig(), Ziel("docm.docx")));
            Assert.Equal(TX.T(TX.VORLAGE_MAKROS, false), makros.Message);

            var fremd = Assert.Throws<InvalidDataException>(() =>
                Fuelle(Encoding.UTF8.GetBytes("kein Word-Dokument"), Gruppe(), Konfig(), Ziel("fremd.docx")));
            Assert.Equal(TX.T(TX.VORLAGE_UNLESBAR, false), fremd.Message);
            Assert.Throws<ArgumentException>(() => Fuelle(Array.Empty<byte>(), Gruppe(), Konfig(), Ziel("leer.docx")));
        }

        // =====================================================================
        //  (d) Leerwerte und Ausnahmen
        // =====================================================================

        /// <summary>
        /// Leer ist nie 0 (Konzept 4.10): ohne Projektdaten der Leerwert des Eintrags, mit
        /// <c>|mit grund</c> samt Grund. Wirft die Quelle eines Einzelwerts, steht „—“, das Ergebnis
        /// warnt — und der Bericht entsteht.
        /// </summary>
        [Fact]
        public void Leerwerte_und_eine_werfende_Quelle_ergeben_Strich_und_der_Bericht_entsteht()
        {
            byte[] v = Vorlage(main =>
                "<w:p><w:r><w:t xml:space=\"preserve\">JAZ {{stamm.kennzahl.eff.jaz}} | {{stamm.kennzahl.eff.jaz|mit grund}}</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t xml:space=\"preserve\">Kunde [{{projekt.kunde}}] Region {{projekt.klimaregion}}</w:t></w:r></w:p>" +
                ABSCHNITT);
            BerichtsDaten daten = Gruppe();
            // Ein leeres Verzeichnis fragt den Vergleicher nie — eine andere Kennzahl steht darin.
            daten.Varianten[0].Kennzahlen = new Dictionary<string, double?>(new WerfenderVergleicher()) { ["andere"] = 1.0 };
            string ziel = Ziel("leer.docx");
            Fuellergebnis e = Fuelle(v, daten, Konfig(), ziel);

            Assert.True(File.Exists(ziel));
            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> texte = doc.MainDocumentPart.Document.Body.Elements<Paragraph>().Select(p => p.InnerText).ToList();
            Assert.Equal("JAZ — | — (" + R.BV_GRUND_AUSNAHME + ")", texte[0]);
            Assert.Equal("Kunde [] Region —", texte[1]);

            Assert.Equal(2, e.Leere["stamm.kennzahl.eff.jaz"]);
            Assert.Equal(1, e.Leere["projekt.kunde"]);
            Assert.Equal(1, e.Leere["projekt.klimaregion"]);
            // Die Stellen zählen jede Auflösung — Grundlage von „leer bei 2 von 2 Stellen“ (BV-E1 B1a).
            Assert.Equal(2, e.Stellen["stamm.kennzahl.eff.jaz"]);
            Assert.Equal(1, e.Stellen["projekt.kunde"]);
            Assert.Equal(e.Ersetzt, e.Stellen.Values.Sum());
            Assert.Equal(2, e.Warnungen.Count(w => w.StartsWith("stamm.kennzahl.eff.jaz: ", StringComparison.Ordinal)));
            Assert.Contains(e.Meldungen(), m => m == TX.F(false, TX.LEER, "stamm.kennzahl.eff.jaz", 2));
            Assert.Equal(4, e.Ersetzt);
        }

        // =====================================================================
        //  (e) Kapitel an der falschen Stelle
        // =====================================================================

        /// <summary>
        /// Ein Kapitel im Satz und eines in einer Tabellenzelle: Fehler im Ergebnis, der Platzhalter
        /// bleibt gelb stehen, kein Kapitel wird geschrieben, das Dokument ist gültig.
        /// </summary>
        [Fact]
        public void Kapitel_im_Satz_oder_in_der_Zelle_ist_ein_Fehler_und_das_Dokument_bleibt_gueltig()
        {
            byte[] v = Vorlage(main =>
                "<w:p><w:r><w:t xml:space=\"preserve\">Siehe {{bericht.inhalt}} unten.</w:t></w:r></w:p>" +
                "<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"dxa\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"5000\"/></w:tblGrid>" +
                "<w:tr><w:tc><w:tcPr><w:tcW w:w=\"5000\" w:type=\"dxa\"/></w:tcPr><w:p><w:r><w:t>{{bericht.inhalt}}</w:t></w:r></w:p></w:tc></w:tr></w:tbl>" +
                "<w:p/>" + ABSCHNITT);
            string ziel = Ziel("kapitel_falsch.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_ANHANG), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Equal(2, e.Fehler.Count);
            Assert.All(e.Fehler, f => Assert.StartsWith("{{bericht.inhalt}}: ", f, StringComparison.Ordinal));
            Assert.Equal(2, e.Unbekannte.Count(b => b.Art == Fuellbefundart.FalscheStelle));
            Assert.Contains(TX.F(false, TX.FUNDORT_TABELLE, 1, 1, 1), e.Unbekannte[1].Fundort);
            Assert.Equal(0, e.Ersetzt);

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body body = doc.MainDocumentPart.Document.Body;
            Assert.Equal(2, body.Descendants<Run>().Count(IstGelb));
            Assert.DoesNotContain(body.Descendants<Paragraph>(), p => p.InnerText == "Anhang");
        }

        // =====================================================================
        //  Inhaltsbreite am Anker
        // =====================================================================

        /// <summary>
        /// Zweispaltiger Satz: Die Kapitel schreiben in die Spaltenbreite — jede Tabelle ist darauf
        /// eingepasst (keine Spalte unter der Mindestbreite, Summe nicht breiter), jedes Bild höchstens
        /// so breit; das Dokument ist gültig.
        /// </summary>
        [Fact]
        public void Kapitel_im_zweispaltigen_Satz_passen_Tabellen_und_Bilder_in_die_Spalte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            byte[] v = Vorlage(main =>
                "<w:p><w:r><w:t>{{bericht.inhalt}}</w:t></w:r></w:p>" +
                "<w:sectPr>" + SEITE + "<w:cols w:num=\"2\" w:space=\"708\"/></w:sectPr>");
            string ziel = Ziel("zweispaltig.docx");
            Fuelle(v, Berichtsdatenproben.Gruppendaten(), Konfig(BerichtsKonfiguration.B_PROJEKT,
                   BerichtsKonfiguration.B_ERGEBNISSE, BerichtsKonfiguration.B_ANHANG), ziel);

            const int spalte = (9355 - 708) / 2;
            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body body = doc.MainDocumentPart.Document.Body;
            List<Table> tabellen = body.Descendants<Table>().ToList();
            Assert.NotEmpty(tabellen);
            foreach (Table t in tabellen)
            {
                List<int> raster = t.GetFirstChild<TableGrid>().Elements<GridColumn>().Select(g => int.Parse(g.Width.Value)).ToList();
                Assert.True(raster.Sum() <= spalte, "Tabelle breiter als die Spalte: " + raster.Sum());
                Assert.All(raster, w => Assert.True(w >= 1));
                Assert.True(int.Parse(t.GetFirstChild<TableProperties>().GetFirstChild<TableWidth>().Width.Value) <= spalte);
            }
            List<DW.Extent> bilder = body.Descendants<DW.Extent>().ToList();
            Assert.NotEmpty(bilder);
            Assert.All(bilder, x => Assert.True(x.Cx.Value <= spalte * 635L, "Bild breiter als die Spalte: " + x.Cx.Value));
        }

        /// <summary>Die Inhaltsbreite aus der Abschnittsangabe: Seite minus Ränder und Bundsteg, bei
        /// Spalten die Spaltenbreite, ohne Seitenmaß der alte Festwert.</summary>
        [Fact]
        public void Inhaltsbreite_kommt_aus_Seite_Raendern_und_Spalten()
        {
            SectionProperties Abschnitt(string xml) => new SectionProperties("<w:sectPr " + NS + ">" + xml + "</w:sectPr>");

            Assert.Equal(WordBerichtGenerator.INHALT_B, WordKontext.InhaltsbreiteAus(null));
            Assert.Equal(WordBerichtGenerator.INHALT_B, WordKontext.InhaltsbreiteAus(Abschnitt("")));
            Assert.Equal(9355, WordKontext.InhaltsbreiteAus(Abschnitt(SEITE)));
            Assert.Equal(16838 - 2 * 1134 - 200, WordKontext.InhaltsbreiteAus(Abschnitt(
                "<w:pgSz w:w=\"16838\" w:h=\"11906\" w:orient=\"landscape\"/><w:pgMar w:top=\"1134\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1134\" w:header=\"708\" w:footer=\"708\" w:gutter=\"200\"/>")));
            Assert.Equal((9355 - 2 * 500) / 3, WordKontext.InhaltsbreiteAus(Abschnitt(SEITE + "<w:cols w:num=\"3\" w:space=\"500\"/>")));
            Assert.Equal(3000, WordKontext.InhaltsbreiteAus(Abschnitt(SEITE +
                "<w:cols w:num=\"2\" w:equalWidth=\"0\"><w:col w:w=\"3000\" w:space=\"500\"/><w:col w:w=\"5855\"/></w:cols>")));

            // Der bisherige Weg ohne Abschnittsangabe bleibt beim Festwert.
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            Assert.Equal(WordBerichtGenerator.INHALT_B, new WordKontext(main, main.Document.Body, null).Inhaltsbreite);
        }

        // =====================================================================
        //  Weitere Regeln: Zeilenumbruch, Felder, Liste im Steuerelement, Entfall
        // =====================================================================

        /// <summary>BV-P1: Ein mehrzeiliger Text bleibt im Run mit seinem Format, die Zeilen trennt <c>w:br</c>.</summary>
        [Fact]
        public void Mehrzeiliger_Text_wird_zu_Zeilenumbruechen_im_selben_Run()
        {
            byte[] v = Vorlage(main =>
                "<w:p><w:r><w:rPr><w:b/></w:rPr><w:t xml:space=\"preserve\">Beschreibung: {{projekt.beschreibung}}!</w:t></w:r></w:p>" + ABSCHNITT);
            BerichtsDaten daten = Gruppe();
            daten.Varianten[0].Projekt = new ProjektModel { m_szBeschreibung = "Zeile eins\r\nZeile zwei\nZeile drei" };
            string ziel = Ziel("zeilen.docx");
            Fuelle(v, daten, Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Run r = Assert.Single(doc.MainDocumentPart.Document.Body.Elements<Paragraph>().First().Elements<Run>());
            Assert.NotNull(r.RunProperties?.Bold);
            Assert.Equal(2, r.Elements<Break>().Count());
            Assert.Equal(new[] { "Beschreibung: Zeile eins", "Zeile zwei", "Zeile drei!" }, r.Elements<Text>().Select(t => t.Text));
        }

        /// <summary>
        /// Konzept 6.7: <c>w:updateFields</c> nur bei TOC, PAGEREF, REF, SEQ, DOCPROPERTY — auch aus
        /// einem zerlegten Feldcode oder einem einfachen Feld, an der Schemastelle der Einstellungen;
        /// DATE und PAGE setzen es nicht, DATE bekommt einen Hinweis.
        /// </summary>
        [Fact]
        public void UpdateFields_nur_bei_Feldern_die_Word_nicht_selbst_aktualisiert()
        {
            byte[] ohne = Vorlage(main =>
                "<w:p><w:r><w:t xml:space=\"preserve\">{{bericht.titel}} </w:t></w:r>" +
                "<w:r><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:instrText xml:space=\"preserve\"> DATE \\@ \"dd.MM.yyyy\" </w:instrText></w:r>" +
                "<w:r><w:fldChar w:fldCharType=\"separate\"/></w:r><w:r><w:t>01.01.2026</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\"/></w:r>" +
                "<w:fldSimple w:instr=\" PAGE \"><w:r><w:t>1</w:t></w:r></w:fldSimple></w:p>" + ABSCHNITT);
            string ziel = Ziel("ohne_felder.docx");
            Fuellergebnis e = Fuelle(ohne, Gruppe(), Konfig(), ziel);
            Assert.Contains(TX.F(false, TX.DATUMSFELD, "DATE"), e.Hinweise);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false))
                Assert.Null(doc.MainDocumentPart.DocumentSettingsPart?.Settings?.GetFirstChild<UpdateFieldsOnOpen>());

            byte[] mit = Vorlage(main =>
            {
                main.AddNewPart<DocumentSettingsPart>().Settings = new Settings("<w:settings " + NS + "><w:displayBackgroundShape/>" +
                    "<w:evenAndOddHeaders w:val=\"false\"/><w:compat><w:compatSetting w:name=\"compatibilityMode\" " +
                    "w:uri=\"http://schemas.microsoft.com/office/word\" w:val=\"15\"/></w:compat></w:settings>");
                return "<w:p><w:r><w:t xml:space=\"preserve\">Abbildung </w:t></w:r>" +
                       "<w:r><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:instrText xml:space=\"preserve\"> SE</w:instrText></w:r>" +
                       "<w:r><w:instrText xml:space=\"preserve\">Q Abbildung \\* ARABIC </w:instrText></w:r><w:r><w:fldChar w:fldCharType=\"separate\"/></w:r>" +
                       "<w:r><w:t>1</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\"/></w:r></w:p>" + ABSCHNITT;
            });
            ziel = Ziel("mit_feldern.docx");
            Fuelle(mit, Gruppe(), Konfig(), ziel);
            Assert.Empty(Validierungsfehler(ziel));
            using (WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false))
                Assert.NotNull(doc.MainDocumentPart.DocumentSettingsPart.Settings.GetFirstChild<UpdateFieldsOnOpen>());
        }

        /// <summary>
        /// Liste im Inhaltssteuerelement: als Block je Eintrag ein Absatz im Format des ersten
        /// Absatzes, im Satz ein Fehler — das Steuerelement bleibt stehen.
        /// </summary>
        [Fact]
        public void Liste_im_Block_Steuerelement_wird_zu_Absaetzen_im_Satz_Steuerelement_ist_sie_ein_Fehler()
        {
            byte[] v = Vorlage(main =>
                "<w:sdt><w:sdtPr><w:tag w:val=\"bericht.warnungen\"/><w:id w:val=\"21\"/></w:sdtPr><w:sdtContent>" +
                "<w:p><w:pPr><w:ind w:left=\"720\"/></w:pPr><w:r><w:t>Warnungen</w:t></w:r></w:p></w:sdtContent></w:sdt>" +
                "<w:p><w:r><w:t xml:space=\"preserve\">Im Satz: </w:t></w:r><w:sdt><w:sdtPr><w:tag w:val=\"bericht.warnungen\"/>" +
                "<w:id w:val=\"22\"/></w:sdtPr><w:sdtContent><w:r><w:t>Warnungen</w:t></w:r></w:sdtContent></w:sdt></w:p>" + ABSCHNITT);
            BerichtsDaten daten = Gruppe();
            daten.Warnungen.Add("W1");
            daten.Warnungen.Add("W2");
            string ziel = Ziel("sdt_liste.docx");
            Fuellergebnis e = Fuelle(v, daten, Konfig(), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body body = doc.MainDocumentPart.Document.Body;
            List<Paragraph> absaetze = body.Elements<Paragraph>().ToList();
            Assert.Equal(new[] { "W1", "W2", "Im Satz: Warnungen" }, absaetze.Select(p => p.InnerText));
            Assert.Equal("720", absaetze[0].ParagraphProperties?.Indentation?.Left?.Value);
            Assert.Single(body.Descendants<SdtRun>());
            Assert.Equal(Fuellbefundart.FalscheStelle, Assert.Single(e.Unbekannte).Art);
            Assert.Contains(e.Fehler, f => f.StartsWith("{{bericht.warnungen}}: ", StringComparison.Ordinal));
            Assert.Equal(1, e.Ersetzt);
        }

        /// <summary>
        /// Konzept 5.3: Liefert ein Kapitelplatzhalter nichts (alle Häkchen ab), entfällt ein
        /// unmittelbar davor stehender Absatz im Format „EPOS Kapitelkopf“ mit — keine verwaiste
        /// Überschrift. Mit Inhalt bleibt er.
        /// </summary>
        [Fact]
        public void Kapitel_ohne_Inhalt_entfaellt_mit_seinem_Kapitelkopf()
        {
            Func<MainDocumentPart, string> bau = main =>
            {
                main.AddNewPart<StyleDefinitionsPart>().Styles = new Styles(
                    new Style(new StyleName { Val = WordVorlagenstile.NAME_KAPITELKOPF })
                    { Type = StyleValues.Paragraph, StyleId = "EPOSKapitelkopf", CustomStyle = true });
                return "<w:p><w:r><w:t>Deckblatt</w:t></w:r></w:p>" +
                       "<w:p><w:pPr><w:pStyle w:val=\"EPOSKapitelkopf\"/></w:pPr><w:r><w:t>Kapitel</w:t></w:r></w:p>" +
                       "<w:p><w:r><w:t>{{bericht.inhalt}}</w:t></w:r></w:p>" + ABSCHNITT;
            };

            string ohne = Ziel("ohne_kapitel.docx");
            Fuellergebnis e = Fuelle(Vorlage(bau), Gruppe(), Konfig(), ohne);
            Assert.Empty(Validierungsfehler(ohne));
            Assert.Equal(1, e.Leere[WordVorlagenfueller.SAMMELANKER]);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(ohne, false))
                Assert.Equal(new[] { "Deckblatt" }, doc.MainDocumentPart.Document.Body.Elements<Paragraph>().Select(p => p.InnerText));

            string mit = Ziel("mit_kapitel.docx");
            Fuelle(Vorlage(bau), Gruppe(), Konfig(BerichtsKonfiguration.B_ANHANG), mit);
            Assert.Empty(Validierungsfehler(mit));
            using (WordprocessingDocument doc = WordprocessingDocument.Open(mit, false))
            {
                List<string> texte = doc.MainDocumentPart.Document.Body.Elements<Paragraph>().Select(p => p.InnerText).ToList();
                Assert.Equal(new[] { "Deckblatt", "Kapitel", "Anhang" }, texte.Take(3));
            }
        }

        // =====================================================================
        //  (f) Kapitel einzeln, Folge, Entfall, Kapitelformat (BV-E2)
        // =====================================================================

        /// <summary>
        /// Konzept 5.3: Die Vorlage bestimmt die Folge — hier der Anhang vor der Projektbeschreibung. Mit
        /// <c>|ohne titel</c> steht der Kapitelkopf der Vorlage an der Stelle der eigenen Überschrift; ohne
        /// die Angabe schreibt das Kapitel seine eigene unter den Kapitelkopf. Die Stellen der Kapitel sind
        /// ihre Kapitelköpfe, das Deckblatt steht aus dem Platzhalter <c>{{bericht.titel}}</c>, ein Kapitel,
        /// das die Vorlage nicht führt, steht nicht im Bericht.
        /// </summary>
        [Fact]
        public void Kapitel_einzeln_in_der_Folge_der_Vorlage_mit_und_ohne_eigene_Ueberschrift()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            byte[] v = KapitelVorlage(TextAbsatz("{{bericht.titel}}"), KopfAbsatz("Zuerst der Anhang"),
                                      TextAbsatz("{{kapitel.anhang|ohne titel}}"), KopfAbsatz("Dann das Projekt"),
                                      TextAbsatz("{{kapitel.projekt}}"));
            string ziel = Ziel("kapitel_folge.docx");
            Fuellergebnis e = Fuelle(v, Berichtsdatenproben.Gruppendaten(),
                                     Konfig(BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_ANHANG), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Empty(e.Unbekannte);
            Assert.Equal(3, e.Ersetzt);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> ueberschriften = Gliederung(doc)
                .Where(z => z.StartsWith("EPOS Kapitelkopf:", StringComparison.Ordinal) || z.StartsWith("heading ", StringComparison.Ordinal))
                .ToList();
            int anhang = ueberschriften.IndexOf("EPOS Kapitelkopf: Zuerst der Anhang");
            int projekt = ueberschriften.IndexOf("EPOS Kapitelkopf: Dann das Projekt");
            Assert.True(anhang == 0 && projekt > anhang, string.Join(" | ", ueberschriften));
            Assert.Equal("heading 2: Simulationsstände", ueberschriften[anhang + 1]);   // ohne titel: kein „Anhang“
            Assert.Equal("heading 1: Projektbeschreibung", ueberschriften[projekt + 1]);   // eigene Überschrift unter dem Kopf
            Assert.DoesNotContain("heading 1: Anhang", ueberschriften);
            Assert.DoesNotContain("{{", AllerText(doc));

            Assert.Equal("Zuerst der Anhang", e.Kapitelstellen[BerichtsKonfiguration.B_ANHANG]);
            Assert.Equal("Dann das Projekt", e.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);
            Assert.Equal("Deckblatt", e.Kapitelstellen[BerichtsKonfiguration.B_DECKBLATT]);
            Assert.Null(e.Kapitelstellen[BerichtsKonfiguration.B_VERGLEICH]);
            Assert.Null(e.Kapitelstellen[Berichtskapitel.ANHANG_E]);
        }

        /// <summary>
        /// Konzept 5.3 „Entfall“: Ein Kapitel mit abgewähltem Häkchen liefert nichts, und der unmittelbar
        /// davor stehende Kapitelkopf entfällt mit — keine verwaiste Überschrift. Der Platzhalter zählt als
        /// leer; im Bericht steht das Kapitel nicht.
        /// </summary>
        [Fact]
        public void Abgewaehltes_Haekchen_laesst_Kapitel_und_Kapitelkopf_entfallen()
        {
            byte[] v = KapitelVorlage(TextAbsatz("Vorwort"), KopfAbsatz("Anhang der Vorlage"),
                                      TextAbsatz("{{kapitel.anhang|ohne titel}}"), TextAbsatz("Nachwort"));
            string ziel = Ziel("kapitel_entfall.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_PROJEKT), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Assert.Equal(new[] { "—: Vorwort", "—: Nachwort" }, Gliederung(doc));
            Assert.Equal(1, e.Leere["kapitel.anhang"]);
            Assert.Null(e.Kapitelstellen[BerichtsKonfiguration.B_ANHANG]);
            Assert.Empty(e.Unbekannte);
            Assert.Empty(e.Fehler);
        }

        /// <summary>
        /// Ein Kapitel zweimal in der Vorlage: Die erste Stelle wird gefüllt, die zweite bleibt gelb stehen
        /// und steht als Fehler im Ergebnis (Konzept 5.3) — das Kapitel erscheint genau einmal.
        /// </summary>
        [Fact]
        public void Kapitel_doppelt_fuellt_die_erste_Stelle_und_laesst_die_zweite_gelb()
        {
            byte[] v = KapitelVorlage(TextAbsatz("{{kapitel.anhang}}"), TextAbsatz("Zwischentext"),
                                      TextAbsatz("{{kapitel.anhang|ebene 2}}"));
            string ziel = Ziel("kapitel_doppelt.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_ANHANG), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> gliederung = Gliederung(doc);
            Assert.Single(gliederung, z => z == "heading 1: Anhang");
            Assert.Single(gliederung, z => z == "heading 2: Simulationsstände");
            Assert.True(gliederung.IndexOf("—: Zwischentext") < gliederung.IndexOf("—: {{kapitel.anhang|ebene 2}}"),
                        string.Join(" | ", gliederung));
            Run gelb = Assert.Single(doc.MainDocumentPart.Document.Body.Descendants<Run>().Where(IstGelb));
            Assert.Equal("{{kapitel.anhang|ebene 2}}", gelb.InnerText);

            Fuellbefund befund = Assert.Single(e.Unbekannte);
            Assert.Equal(Fuellbefundart.Doppelt, befund.Art);
            Assert.Equal(TX.T(TX.GRUND_DOPPELT, false), befund.Grund);
            string fehler = Assert.Single(e.Fehler);
            Assert.StartsWith("{{kapitel.anhang|ebene 2}}: Das Kapitel steht schon an einer früheren Stelle der Vorlage",
                              fehler, StringComparison.Ordinal);
            Assert.Equal(1, e.Ersetzt);
        }

        /// <summary>
        /// Konzept 4.8: <c>|ebene n</c> rückt jede Überschrift des Kapitels um n − 1 Ebenen tiefer — fehlende
        /// Überschriftenstile (hier Überschrift 4) legt die Engine mit ihrer Gliederungsebene an und nennt
        /// sie; mit <c>|ohne titel</c> entfällt die Kapitelüberschrift trotzdem. Der Sammelanker nimmt die
        /// Angabe für seine Kapitel und setzt den einzeln geführten Anhang nicht noch einmal ein.
        /// </summary>
        [Fact]
        public void Ebene_rueckt_die_Ueberschriften_tiefer_und_legt_fehlende_Stile_an()
        {
            byte[] v = KapitelVorlage(KopfAbsatz("Kopf"), TextAbsatz("{{kapitel.anhang|ohne titel|ebene 3}}"),
                                      TextAbsatz("{{bericht.inhalt|ebene 2}}"));
            string ziel = Ziel("kapitel_ebene.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_ANHANG, BerichtsKonfiguration.B_INHALT), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Empty(e.Unbekannte);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> gliederung = Gliederung(doc);
            Assert.Contains("heading 4: Simulationsstände", gliederung);
            Assert.Contains("heading 4: Datengrundlage und Methodik", gliederung);
            Assert.DoesNotContain(gliederung, z => z.EndsWith(": Anhang", StringComparison.Ordinal));
            Assert.Single(gliederung, z => z.EndsWith(": Simulationsstände", StringComparison.Ordinal));
            Assert.Contains("heading 2: Inhalt", gliederung);

            Style h4 = doc.MainDocumentPart.StyleDefinitionsPart.Styles.Elements<Style>()
                          .Single(s => s.StyleName?.Val?.Value == "heading 4");
            Assert.Equal(3, h4.StyleParagraphProperties?.OutlineLevel?.Val?.Value);
            Assert.Contains(TX.F(false, TX.STIL_ANGELEGT, "heading 4"), e.Hinweise);
        }

        /// <summary>
        /// Anhang E mit <c>|ohne titel</c> unter einem Kapitelkopf: Den Seitenumbruch, den der Baustein vor
        /// seine Überschrift schreibt, setzt die Engine VOR den Kapitelkopf — sonst hinge die Überschrift
        /// allein am Seitenende. Der Kapitelkopf <c>{{text.kapitel_anhang_e}}</c> ist aufgelöst. Die Spalte
        /// „Stelle“ nennt die Überschriften DIESES Berichts: den umbenannten Kapitelkopf der
        /// Wirtschaftlichkeit, „nicht im Bericht“ für die Kapitel, die die Vorlage nicht führt.
        /// </summary>
        [Fact]
        public void Anhang_E_setzt_den_Umbruch_vor_den_Kapitelkopf_und_nennt_die_Stellen_der_Vorlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            byte[] v = KapitelVorlage(KopfAbsatz("5 Wirtschaftliche Bewertung"), TextAbsatz("{{kapitel.wirtschaftlichkeit|ohne titel}}"),
                                      KopfAbsatz("{{text.kapitel_anhang_e}}"), TextAbsatz("{{kapitel.anhang_e|ohne titel}}"));
            string ziel = Ziel("anhang_e.docx");
            Fuellergebnis e = Fuelle(v, BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE),
                                     Konfig(BerichtsKonfiguration.B_WIRTSCHAFT), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Empty(e.Unbekannte);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> gliederung = Gliederung(doc);
            int kopf = gliederung.IndexOf("EPOS Kapitelkopf: " + R.WIRT_AE_TITEL);
            Assert.True(kopf > 0, string.Join(" | ", gliederung));
            Assert.Equal("Seitenumbruch", gliederung[kopf - 1]);
            Assert.EndsWith(": " + R.WIRT_AE_HINWEIS, gliederung[kopf + 1], StringComparison.Ordinal);
            Assert.DoesNotContain(gliederung, z => z.StartsWith("heading 1:", StringComparison.Ordinal));
            Assert.Equal("EPOS Kapitelkopf: 5 Wirtschaftliche Bewertung", gliederung[0]);

            Assert.Equal("5 Wirtschaftliche Bewertung", e.Kapitelstellen[BerichtsKonfiguration.B_WIRTSCHAFT]);
            Assert.Equal(R.WIRT_AE_TITEL, e.Kapitelstellen[Berichtskapitel.ANHANG_E]);
            Assert.Null(e.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);

            Table checkliste = doc.MainDocumentPart.Document.Body.Elements<Table>().Last();
            string Stelle(string nummer) => checkliste.Elements<TableRow>()
                .Select(r => r.Elements<TableCell>().Select(c => c.InnerText).ToList())
                .Single(z => z[0] == nummer)[3];
            Assert.Equal("Wortbericht: „5 Wirtschaftliche Bewertung“ › „Kennzahlen im Szenario „Erwartet““ · " +
                         "Tabellenbericht: Blatt „Wirtschaftlichkeit“, Block „Erwartet“", Stelle("1"));
            Assert.Equal("Wortbericht: nicht im Bericht · Tabellenbericht: Blatt „Übersicht“", Stelle("0.1"));
            Assert.Equal("Wortbericht: nicht im Bericht · Tabellenbericht: Blatt „Übersicht“", Stelle("0.2"));
            Assert.Equal("Wortbericht: „5 Wirtschaftliche Bewertung“ · Tabellenbericht: Formelmappe (Parameterblock, " +
                         "Mehrjahrestabellen, Kennzahlen)", Stelle("11"));
        }

        /// <summary>
        /// Der Sammelanker setzt die angehakten Kapitel OHNE die einzeln geführten (Konzept 5.3): Der Anhang
        /// steht einmal, an seiner eigenen Stelle unter dem Kapitelkopf <c>{{text.kapitel_anhang}}</c>; der
        /// Sammelanker bringt danach das Inhaltsverzeichnis.
        /// </summary>
        [Fact]
        public void Sammelanker_setzt_die_einzeln_gefuehrten_Kapitel_nicht_noch_einmal()
        {
            byte[] v = KapitelVorlage(KopfAbsatz("{{text.kapitel_anhang}}"), TextAbsatz("{{kapitel.anhang|ohne titel}}"),
                                      TextAbsatz("{{bericht.inhalt}}"));
            string ziel = Ziel("sammelanker_rest.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_ANHANG, BerichtsKonfiguration.B_INHALT), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> gliederung = Gliederung(doc);
            Assert.Equal("EPOS Kapitelkopf: Anhang", gliederung[0]);
            Assert.Single(gliederung, z => z == "heading 2: Simulationsstände");
            Assert.True(gliederung.IndexOf("heading 1: Inhalt") > gliederung.IndexOf("heading 2: Simulationsstände"),
                        string.Join(" | ", gliederung));
            Assert.Equal("Anhang", e.Kapitelstellen[BerichtsKonfiguration.B_ANHANG]);
            Assert.Equal("Inhalt", e.Kapitelstellen[BerichtsKonfiguration.B_INHALT]);
            Assert.Null(e.Kapitelstellen[BerichtsKonfiguration.B_VERGLEICH]);   // über den Sammelanker, aber ohne Häkchen
        }

        /// <summary>
        /// Kapitel nur im Rumpf oder im Block-Steuerelement (Konzept 4.3): das Steuerelement mit dem Tag
        /// <c>kapitel.anhang</c> wird gefüllt und ausgepackt; ein Kapitel in einer Tabellenzelle bleibt gelb
        /// stehen und steht nicht im Bericht.
        /// </summary>
        [Fact]
        public void Kapitel_im_Block_Steuerelement_wird_gefuellt_in_der_Zelle_bleibt_es_stehen()
        {
            byte[] v = KapitelVorlage(
                "<w:sdt><w:sdtPr><w:tag w:val=\"kapitel.anhang\"/><w:id w:val=\"31\"/></w:sdtPr><w:sdtContent>" +
                "<w:p><w:r><w:t>Anhang hierhin</w:t></w:r></w:p></w:sdtContent></w:sdt>",
                "<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"dxa\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"5000\"/></w:tblGrid>" +
                "<w:tr><w:tc><w:tcPr><w:tcW w:w=\"5000\" w:type=\"dxa\"/></w:tcPr><w:p><w:r><w:t>{{kapitel.projekt}}</w:t></w:r></w:p>" +
                "</w:tc></w:tr></w:tbl>",
                TextAbsatz("Ende"));
            string ziel = Ziel("kapitel_sdt_zelle.docx");
            Fuellergebnis e = Fuelle(v, Gruppe(), Konfig(BerichtsKonfiguration.B_ANHANG, BerichtsKonfiguration.B_PROJEKT), ziel);

            Assert.Empty(Validierungsfehler(ziel));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body body = doc.MainDocumentPart.Document.Body;
            Assert.Empty(body.Descendants<SdtBlock>());
            Assert.Contains("heading 1: Anhang", Gliederung(doc));
            Assert.Equal("{{kapitel.projekt}}", Assert.Single(body.Descendants<Run>().Where(IstGelb)).InnerText);
            Assert.Equal(Fuellbefundart.FalscheStelle, Assert.Single(e.Unbekannte).Art);
            Assert.Equal("Anhang", e.Kapitelstellen[BerichtsKonfiguration.B_ANHANG]);
            Assert.Null(e.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);
        }

        // =====================================================================
        //  (g) Logo als Bildplatzhalter (Anwenderentscheid BV-E2-1)
        // =====================================================================

        /// <summary>
        /// Das Logo der Einstellungen im Platzhalterbild der Kopfzeile: neuer Bildteil mit dem Inhaltstyp der
        /// Datei, das Platzhalterbild ist fort; mit seinem Seitenverhältnis in den Rahmen eingepasst
        /// (quadratisch: die Höhe begrenzt, breit: die Breite); Alternativtext der Dateiname; Text und
        /// Lage bleiben; gültig in allen Fassungen.
        /// </summary>
        [Theory]
        [InlineData(true, 40, 40, 476250L, 476250L, "image/png")]
        [InlineData(false, 200, 50, 952500L, 238125L, "image/jpeg")]
        public void Logo_wird_mit_seinem_Seitenverhaeltnis_in_den_Rahmen_eingepasst(bool png, int breite, int hoehe,
                                                                                   long cx, long cy, string typ)
        {
            byte[] logo = png ? Png(breite, hoehe) : Jpeg(breite, hoehe);
            string datei = png ? "firma.png" : "firma.jpg";
            string ziel = Ziel("logo.docx");
            Fuellergebnis e = FuelleMit(LogoVorlage(), Gruppe(), Konfig(),
                                        new Erstellerangaben { Firma = FIRMA, Logo = logo, LogoDateiname = datei }, ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Empty(e.Unbekannte);
            Assert.Empty(e.Warnungen);
            Assert.Equal(3, e.Ersetzt);   // Titel, Programm, Logo
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            HeaderPart kopf = doc.MainDocumentPart.HeaderParts.Single();
            DW.Extent ausdehnung = kopf.Header.Descendants<DW.Extent>().Single();
            Assert.Equal(cx, ausdehnung.Cx.Value);
            Assert.Equal(cy, ausdehnung.Cy.Value);
            A.Extents xfrm = kopf.Header.Descendants<A.Extents>().Single();
            Assert.Equal(cx, xfrm.Cx.Value);
            Assert.Equal(cy, xfrm.Cy.Value);

            A.Blip blip = kopf.Header.Descendants<A.Blip>().Single();
            var teil = (ImagePart)kopf.GetPartById(blip.Embed.Value);
            Assert.Equal(typ, teil.ContentType);
            Assert.Single(kopf.ImageParts);
            using (Stream s = teil.GetStream()) Assert.Equal(logo.Length, s.Length);
            Assert.Equal(datei, kopf.Header.Descendants<DW.DocProperties>().Single().Description.Value);
            Assert.StartsWith("EPOS-Plan", kopf.Header.InnerText, StringComparison.Ordinal);
            AssertBildkennungenEindeutig(doc);
        }

        /// <summary>
        /// Ohne Logo entfällt das Platzhalterbild samt Lauf und Bildteil (Konzept 4.10: kein Bild statt
        /// eines fremden): Steht es allein in der Kopfzeile, bleibt ihr leerer Absatz; steht Text daneben,
        /// bleibt der Text; ein danach leerer Absatz im Rumpf entfällt. Die Warnung der Erstellerangaben
        /// („Logo nicht gefunden: …“) steht einmal im Ergebnis; jede Stelle zählt als leer.
        /// </summary>
        [Fact]
        public void Ohne_Logo_entfaellt_das_Platzhalterbild_und_die_Warnung_nennt_die_Datei()
        {
            byte[] v = Vorlage(main =>
            {
                HeaderPart kopf = main.AddNewPart<HeaderPart>();
                kopf.Header = new Header("<w:hdr " + NS + "><w:p>" +
                    Logobild(kopf.GetIdOfPart(Einpixel(kopf.AddImagePart(ImagePartType.Png))), 1, 952500, 476250,
                             "{{bild.ersteller.logo}}") + "</w:p></w:hdr>");
                FooterPart fuss = main.AddNewPart<FooterPart>();
                fuss.Footer = new Footer("<w:ftr " + NS + "><w:p><w:r><w:t xml:space=\"preserve\">Firma </w:t></w:r>" +
                    Logobild(fuss.GetIdOfPart(Einpixel(fuss.AddImagePart(ImagePartType.Png))), 2, 952500, 476250,
                             "bild.ersteller.logo") + "</w:p></w:ftr>");
                string rumpfbild = main.GetIdOfPart(Einpixel(main.AddImagePart(ImagePartType.Png)));
                return TextAbsatz("Vor dem Logo") + "<w:p>" + Logobild(rumpfbild, 3, 952500, 476250, "{{bild.ersteller.logo}}") + "</w:p>" +
                       TextAbsatz("Nach dem Logo") +
                       "<w:sectPr><w:headerReference w:type=\"default\" r:id=\"" + main.GetIdOfPart(kopf) + "\"/>" +
                       "<w:footerReference w:type=\"default\" r:id=\"" + main.GetIdOfPart(fuss) + "\"/>" + SEITE + "</w:sectPr>";
            });
            const string warnung = "Logo nicht gefunden: C:\\fehlt\\logo.png";
            string ziel = Ziel("ohne_logo.docx");
            Fuellergebnis e = FuelleMit(v, Gruppe(), Konfig(), new Erstellerangaben { Firma = FIRMA, LogoWarnung = warnung }, ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Assert.Equal(new[] { warnung }, e.Warnungen);
            Assert.Equal(3, e.Leere[Vorlagenfeldkatalog.LOGO]);
            Assert.Empty(e.Unbekannte);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            MainDocumentPart main = doc.MainDocumentPart;
            Assert.Empty(main.ImageParts);
            Assert.Empty(main.HeaderParts.Single().ImageParts);
            Assert.Empty(main.FooterParts.Single().ImageParts);
            Assert.Equal("", Assert.Single(main.HeaderParts.Single().Header.Elements<Paragraph>()).InnerText);
            Assert.Equal("Firma ", Assert.Single(main.FooterParts.Single().Footer.Elements<Paragraph>()).InnerText);
            Assert.Equal(new[] { "Vor dem Logo", "Nach dem Logo" }, main.Document.Body.Elements<Paragraph>().Select(p => p.InnerText));
            Assert.Empty(main.Document.Body.Descendants<Drawing>());
        }

        /// <summary>
        /// Das Logo im Rumpf, eingepasst in einen breiten Rahmen; ein Bild mit einem unbekannten Bildschlüssel
        /// bleibt, wie es ist, und steht im Ergebnis.
        /// </summary>
        [Fact]
        public void Logo_im_Rumpf_und_ein_unbekannter_Bildschluessel()
        {
            byte[] v = Vorlage(main =>
            {
                string logo = main.GetIdOfPart(Einpixel(main.AddImagePart(ImagePartType.Png)));
                string fremd = main.GetIdOfPart(Einpixel(main.AddImagePart(ImagePartType.Png)));
                return "<w:p>" + Logobild(logo, 1, 1905000, 952500, "{{bild.ersteller.logo}}") + "</w:p>" +
                       "<w:p>" + Logobild(fremd, 2, 952500, 952500, "{{bild.gibt.es.nicht}}") + "</w:p>" + ABSCHNITT;
            });
            string ziel = Ziel("logo_rumpf.docx");
            Fuellergebnis e = FuelleMit(v, Gruppe(), Konfig(),
                                        new Erstellerangaben { Firma = FIRMA, Logo = Png(100, 100), LogoDateiname = "logo.png" }, ziel);

            Assert.Empty(Validierungsfehler(ziel));
            Fuellbefund fremdbefund = Assert.Single(e.Unbekannte);
            Assert.Equal("{{bild.gibt.es.nicht}}", fremdbefund.Normalform);
            Assert.Equal(Fuellbefundart.Unbekannt, fremdbefund.Art);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<DW.Extent> ausdehnungen = doc.MainDocumentPart.Document.Body.Descendants<DW.Extent>().ToList();
            Assert.Equal(952500L, ausdehnungen[0].Cx.Value);   // 200 × 100 px Rahmen, quadratisches Logo: 100 × 100 px
            Assert.Equal(952500L, ausdehnungen[0].Cy.Value);
            Assert.Equal(952500L, ausdehnungen[1].Cx.Value);   // das fremde Bild bleibt
            Assert.Equal(2, doc.MainDocumentPart.ImageParts.Count());
            Assert.Equal("{{bild.gibt.es.nicht}}", doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>().Last().Description.Value);
        }

        // =====================================================================
        //  Normalisierer und Texte
        // =====================================================================

        /// <summary>Mehrere zerlegte Platzhalter in einer Folge von Texten: jeder landet ganz im Text
        /// mit seinem <c>{{</c>; Rechtschreib- und Textmarken dazwischen stören nicht.</summary>
        [Fact]
        public void Normalisierer_zieht_mehrere_Platzhalter_einer_Folge_zusammen()
        {
            var p = new Paragraph(
                new Run(new Text("a{{x")),
                new ProofError { Type = ProofingErrorValues.SpellStart },
                new Run(new Text(".y}}b{{c")),
                new BookmarkStart { Id = "0", Name = "_GoBack" },
                new BookmarkEnd { Id = "0" },
                new Run(new Text(".d}}e")));
            Assert.Equal(2, WordVorlagennormalisierer.NormalisiereAbsatz(p));
            Assert.Equal(new[] { "a{{x.y}}", "b{{c.d}}", "e" }, p.Descendants<Text>().Select(t => t.Text));
            Assert.Equal(3, p.Elements<Run>().Count());

            var q = new Paragraph(new Run(new Text("{{pro")), new Run(new Text("jekt.")), new Run(new Text("name}}")));
            Assert.Equal(1, WordVorlagennormalisierer.NormalisiereAbsatz(q));
            Assert.Equal("{{projekt.name}}", Assert.Single(q.Elements<Run>()).InnerText);
        }

        /// <summary>Jeder Text der Engine steht zweisprachig in der BerichtTexte-Liste (Konzept 4.10).</summary>
        [Fact]
        public void Jeder_Text_der_Engine_ist_zweisprachig()
        {
            Assert.Equal(TX.Alle.Count, TX.Alle.Distinct(StringComparer.Ordinal).Count());
            foreach (string de in TX.Alle)
            {
                string en = BerichtTexte.T(de, true);
                Assert.False(string.Equals(de, en, StringComparison.Ordinal), "Ohne englische Fassung: " + de);
                foreach (string marke in new[] { "{0}", "{1}", "{2}", "{{bericht.inhalt}}", "{{bericht.datum}}" })
                    Assert.True(de.Contains(marke, StringComparison.Ordinal) == en.Contains(marke, StringComparison.Ordinal),
                                "Platzhalter " + marke + " nicht in beiden Fassungen: " + de);
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Die Namensräume der gebauten Vorlagen.</summary>
        private const string NS =
            "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" " +
            "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" " +
            "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
            "xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\" " +
            "xmlns:wps=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\" " +
            "xmlns:v=\"urn:schemas-microsoft-com:vml\" " +
            "xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
            "xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" " +
            "xmlns:w14=\"http://schemas.microsoft.com/office/word/2010/wordml\" " +
            "xmlns:wp14=\"http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing\" " +
            "mc:Ignorable=\"w14 wp14\"";

        /// <summary>A4 hoch, Ränder 25/20 mm: Inhaltsbreite 9 355 DXA wie der alte Festwert.</summary>
        private const string SEITE =
            "<w:pgSz w:w=\"11906\" w:h=\"16838\"/>" +
            "<w:pgMar w:top=\"1417\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1417\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/>";

        private const string ABSCHNITT = "<w:sectPr>" + SEITE + "</w:sectPr>";

        /// <summary>Ein PNG von 1 × 1 Bildpunkt.</summary>
        private const string PNG_1X1 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

        /// <summary>Baut eine Vorlage: <paramref name="rumpf"/> legt Teile an und liefert den Rumpf als XML.</summary>
        private static byte[] Vorlage(Func<MainDocumentPart, string> rumpf,
                                      WordprocessingDocumentType typ = WordprocessingDocumentType.Document)
        {
            using var ms = new MemoryStream();
            using (WordprocessingDocument doc = WordprocessingDocument.Create(ms, typ))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                string xml = rumpf(main);
                main.Document = new Document("<w:document " + NS + "><w:body>" + xml + "</w:body></w:document>");
            }
            return ms.ToArray();
        }

        /// <summary>Ein Bild als Inline-Zeichnung (eingebettet über <paramref name="embed"/> oder verknüpft über <paramref name="link"/>).</summary>
        private static string Bild(string embed, int kennung, string link = null)
        {
            string blip = link != null ? "<a:blip r:link=\"" + link + "\"/>" : "<a:blip r:embed=\"" + embed + "\"/>";
            return "<w:r><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">" +
                   "<wp:extent cx=\"952500\" cy=\"952500\"/><wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>" +
                   "<wp:docPr id=\"" + kennung + "\" name=\"Bild " + kennung + "\"/>" +
                   "<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>" +
                   "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">" +
                   "<pic:pic><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"Bild.png\"/><pic:cNvPicPr/></pic:nvPicPr>" +
                   "<pic:blipFill>" + blip + "<a:stretch><a:fillRect/></a:stretch></pic:blipFill>" +
                   "<pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"952500\" cy=\"952500\"/></a:xfrm>" +
                   "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic>" +
                   "</wp:inline></w:drawing></w:r>";
        }

        /// <summary>Ein Textfeld als DrawingML-Form (Zweig <c>mc:Choice Requires="wps"</c>).</summary>
        private static string WpsTextfeld(string text, int kennung)
        {
            return "<w:drawing><wp:anchor distT=\"0\" distB=\"0\" distL=\"114300\" distR=\"114300\" simplePos=\"0\" " +
                   "relativeHeight=\"251659264\" behindDoc=\"0\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\">" +
                   "<wp:simplePos x=\"0\" y=\"0\"/>" +
                   "<wp:positionH relativeFrom=\"column\"><wp:posOffset>0</wp:posOffset></wp:positionH>" +
                   "<wp:positionV relativeFrom=\"paragraph\"><wp:posOffset>0</wp:posOffset></wp:positionV>" +
                   "<wp:extent cx=\"1905000\" cy=\"508000\"/><wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/><wp:wrapNone/>" +
                   "<wp:docPr id=\"" + kennung + "\" name=\"Textfeld " + kennung + "\"/><wp:cNvGraphicFramePr/>" +
                   "<a:graphic><a:graphicData uri=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\">" +
                   "<wps:wsp><wps:cNvSpPr txBox=\"1\"/><wps:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"1905000\" cy=\"508000\"/></a:xfrm>" +
                   "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></wps:spPr>" +
                   "<wps:txbx><w:txbxContent><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:txbxContent></wps:txbx>" +
                   "<wps:bodyPr/></wps:wsp></a:graphicData></a:graphic></wp:anchor></w:drawing>";
        }

        /// <summary>Ein Absatzstil, wie ihn das deutsche Word schreibt: übersetzte ID, englischer Name.</summary>
        private static Style DeutscherStil(string id, string name, int? gliederung, bool standard = false)
        {
            var s = new Style { Type = StyleValues.Paragraph, StyleId = id };
            if (standard) s.Default = OnOffValue.FromBoolean(true);
            s.Append(new StyleName { Val = name });
            if (!standard) s.Append(new BasedOn { Val = "Standard" });
            if (gliederung.HasValue) s.Append(new StyleParagraphProperties(new OutlineLevel { Val = gliederung.Value }));
            return s;
        }

        private Fuellergebnis Fuelle(byte[] vorlage, BerichtsDaten daten, BerichtsKonfiguration konfig, string ziel)
        {
            return FuelleMit(vorlage, daten, konfig, new Erstellerangaben { Firma = FIRMA }, ziel);
        }

        private Fuellergebnis FuelleMit(byte[] vorlage, BerichtsDaten daten, BerichtsKonfiguration konfig,
                                        Erstellerangaben ersteller, string ziel)
        {
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, vorlage, ersteller, ziel);
            foreach (string zeile in e.Meldungen()) _ausgabe.WriteLine(zeile);
            return e;
        }

        /// <summary>Das Absatzformat „EPOS Kapitelkopf“, wie die mitgelieferten Vorlagen es führen (gefunden über den Namen).</summary>
        private static Style KapitelkopfStil()
        {
            return new Style(new StyleName { Val = WordVorlagenstile.NAME_KAPITELKOPF })
            { Type = StyleValues.Paragraph, StyleId = "EPOSKapitelkopf", CustomStyle = true };
        }

        /// <summary>Ein Absatz im Format „EPOS Kapitelkopf“.</summary>
        private static string KopfAbsatz(string text)
        {
            return "<w:p><w:pPr><w:pStyle w:val=\"EPOSKapitelkopf\"/></w:pPr><w:r><w:t xml:space=\"preserve\">" + text +
                   "</w:t></w:r></w:p>";
        }

        /// <summary>Ein Absatz ohne Format mit einem Text.</summary>
        private static string TextAbsatz(string text)
        {
            return "<w:p><w:r><w:t xml:space=\"preserve\">" + text + "</w:t></w:r></w:p>";
        }

        /// <summary>Eine Vorlage aus Rumpfteilen, deren einziger Stil der Kapitelkopf ist; A4 hoch.</summary>
        private static byte[] KapitelVorlage(params string[] teile)
        {
            return Vorlage(main =>
            {
                main.AddNewPart<StyleDefinitionsPart>().Styles = new Styles(KapitelkopfStil());
                return string.Concat(teile) + ABSCHNITT;
            });
        }

        /// <summary>
        /// Die Kinder des Rumpfs als „Stilname: Text“ — der Stilname über <c>w:name</c> („heading 2“, „EPOS
        /// Kapitelkopf“), ohne Stil „—“; ein Absatz nur mit Seitenumbruch als „Seitenumbruch“, eine Tabelle als „Tabelle“.
        /// </summary>
        private static List<string> Gliederung(WordprocessingDocument doc)
        {
            var namen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Style s in doc.MainDocumentPart.StyleDefinitionsPart?.Styles?.Elements<Style>() ?? Enumerable.Empty<Style>())
                if (s.StyleId?.Value != null && !namen.ContainsKey(s.StyleId.Value))
                    namen[s.StyleId.Value] = s.StyleName?.Val?.Value ?? s.StyleId.Value;

            var liste = new List<string>();
            foreach (OpenXmlElement e in doc.MainDocumentPart.Document.Body.ChildElements)
            {
                if (e is Table) liste.Add("Tabelle");
                if (!(e is Paragraph p)) continue;
                bool umbruch = p.Descendants<Break>().Any(b => b.Type?.Value == BreakValues.Page);
                if (umbruch && p.InnerText.Length == 0) { liste.Add("Seitenumbruch"); continue; }
                string id = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                string stil = id == null ? "—" : namen.TryGetValue(id, out string name) ? name : id;
                liste.Add(stil + ": " + p.InnerText);
            }
            return liste;
        }

        /// <summary>Ein PNG in der Größe <paramref name="breite"/> × <paramref name="hoehe"/> Bildpunkte.</summary>
        internal static byte[] Png(int breite, int hoehe) { return Kodiere(breite, hoehe, SkiaSharp.SKEncodedImageFormat.Png); }

        /// <summary>Ein JPEG in der Größe <paramref name="breite"/> × <paramref name="hoehe"/> Bildpunkte.</summary>
        internal static byte[] Jpeg(int breite, int hoehe) { return Kodiere(breite, hoehe, SkiaSharp.SKEncodedImageFormat.Jpeg); }

        private static byte[] Kodiere(int breite, int hoehe, SkiaSharp.SKEncodedImageFormat format)
        {
            using var bild = new SkiaSharp.SKBitmap(breite, hoehe);
            using (var leinwand = new SkiaSharp.SKCanvas(bild)) leinwand.Clear(SkiaSharp.SKColors.SteelBlue);
            using SkiaSharp.SKImage abbild = SkiaSharp.SKImage.FromBitmap(bild);
            using SkiaSharp.SKData daten = abbild.Encode(format, 90);
            return daten.ToArray();
        }

        /// <summary>Füllt einen Bildteil mit einem PNG von 1 × 1 Bildpunkt.</summary>
        private static ImagePart Einpixel(ImagePart teil)
        {
            using (var s = new MemoryStream(Convert.FromBase64String(PNG_1X1))) teil.FeedData(s);
            return teil;
        }

        /// <summary>Ein Bild (Inline-Zeichnung) mit Rahmen <paramref name="cx"/> × <paramref name="cy"/> EMU und Alternativtext.</summary>
        private static string Logobild(string embed, int kennung, long cx, long cy, string beschreibung)
        {
            string x = cx.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string y = cy.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "<w:r><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">" +
                   "<wp:extent cx=\"" + x + "\" cy=\"" + y + "\"/><wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>" +
                   "<wp:docPr id=\"" + kennung + "\" name=\"Logo " + kennung + "\" descr=\"" + beschreibung + "\"/>" +
                   "<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>" +
                   "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">" +
                   "<pic:pic><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"Logo.png\"/><pic:cNvPicPr/></pic:nvPicPr>" +
                   "<pic:blipFill><a:blip r:embed=\"" + embed + "\"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>" +
                   "<pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"" + x + "\" cy=\"" + y + "\"/></a:xfrm>" +
                   "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic>" +
                   "</wp:inline></w:drawing></w:r>";
        }

        /// <summary>
        /// Eine Vorlage mit Kopfzeile wie die Standardvorlage: Programm, Tabulator und rechts das Platzhalterbild
        /// des Logos (Rahmen 100 × 50 Bildpunkte, Alternativtext <c>{{bild.ersteller.logo}}</c>); im Rumpf der Titel.
        /// </summary>
        private static byte[] LogoVorlage()
        {
            return Vorlage(main =>
            {
                HeaderPart kopf = main.AddNewPart<HeaderPart>();
                string platzhalter = kopf.GetIdOfPart(Einpixel(kopf.AddImagePart(ImagePartType.Png)));
                kopf.Header = new Header("<w:hdr " + NS + "><w:p><w:r><w:t>{{ersteller.programm}}</w:t></w:r><w:r><w:tab/></w:r>" +
                                         Logobild(platzhalter, 1, 952500, 476250, "{{bild.ersteller.logo}}") + "</w:p></w:hdr>");
                return TextAbsatz("{{bericht.titel}}") +
                       "<w:sectPr><w:headerReference w:type=\"default\" r:id=\"" + main.GetIdOfPart(kopf) + "\"/>" + SEITE + "</w:sectPr>";
            });
        }

        /// <summary>Die synthetische Gruppe ohne Wirtschaftlichkeit: Stamm „Stammprojekt“, eine Variante A.</summary>
        private static BerichtsDaten Gruppe() => Berichtsdatenproben.Gruppendaten(2);

        private static BerichtsKonfiguration Konfig(params string[] bausteine)
        {
            var k = new BerichtsKonfiguration();
            k.AktiveBausteine.AddRange(bausteine);
            return k;
        }

        private string Ziel(string datei)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bv-e1-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            _ordner.Add(o);
            return Path.Combine(o, datei);
        }

        /// <summary>Eine Vorlage des Repositoriums als Bytes; <c>null</c> ohne Repositorium.</summary>
        private static byte[] Repovorlage(string datei)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, BerichtsvorlageDateiWacheTests.ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
            Assert.True(File.Exists(pfad), "Die Vorlage fehlt: " + pfad);
            return File.ReadAllBytes(pfad);
        }

        private static List<string> Messlatte(string datei)
        {
            string pfad = Path.Combine(Berichtsdatenproben.Repowurzel(),
                BerichtVorlagenMesslatteTests.MESSLATTEN_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
            return File.ReadAllText(pfad, Encoding.UTF8).Split('\n').Select(z => z.TrimEnd('\r'))
                       .Where((z, i) => z.Length > 0 || i == 0).ToList();
        }

        /// <summary>Die Befunde des Validators in allen Office-Fassungen 2007 bis 2021.</summary>
        private List<string> Validierungsfehler(string pfad)
        {
            var befunde = new List<string>();
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen)
                foreach (ValidationErrorInfo f in new OpenXmlValidator(fassung).Validate(doc).Take(5))
                {
                    string text = fassung + ": " + f.Description + " @ " + f.Part?.Uri + " " + f.Path?.XPath;
                    befunde.Add(text);
                    _ausgabe.WriteLine(text);
                }
            return befunde;
        }

        /// <summary>Der Text aller Teile: Rumpf, Kopf- und Fußzeilen, Fuß- und Endnoten.</summary>
        private static string AllerText(WordprocessingDocument doc)
        {
            MainDocumentPart m = doc.MainDocumentPart;
            var sb = new StringBuilder(m.Document.Body.InnerText);
            foreach (HeaderPart h in m.HeaderParts) sb.Append('\n').Append(h.Header?.InnerText);
            foreach (FooterPart f in m.FooterParts) sb.Append('\n').Append(f.Footer?.InnerText);
            sb.Append('\n').Append(m.FootnotesPart?.Footnotes?.InnerText);
            sb.Append('\n').Append(m.EndnotesPart?.Endnotes?.InnerText);
            return sb.ToString();
        }

        /// <summary>Jede <c>docPr/@id</c> kommt über alle Teile genau einmal vor.</summary>
        private static void AssertBildkennungenEindeutig(WordprocessingDocument doc)
        {
            MainDocumentPart m = doc.MainDocumentPart;
            var wurzeln = new List<OpenXmlElement> { m.Document.Body };
            wurzeln.AddRange(m.HeaderParts.Select(h => (OpenXmlElement)h.Header));
            wurzeln.AddRange(m.FooterParts.Select(f => (OpenXmlElement)f.Footer));
            List<uint> kennungen = wurzeln.SelectMany(w => w.Descendants<DW.DocProperties>()).Select(d => d.Id.Value).ToList();
            Assert.NotEmpty(kennungen);
            Assert.Equal(kennungen.Count, kennungen.Distinct().Count());
        }

        private static string ErsterMitStil(Body body, string stil)
            => body.Elements<Paragraph>().First(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == stil).InnerText;

        private static bool IstGelb(Run r)
            => r.RunProperties?.Highlight?.Val != null && r.RunProperties.Highlight.Val.Value == HighlightColorValues.Yellow;

        private static int Vorkommen(string text, string teil)
        {
            int zahl = 0;
            for (int i = text.IndexOf(teil, StringComparison.Ordinal); i >= 0; i = text.IndexOf(teil, i + teil.Length, StringComparison.Ordinal)) zahl++;
            return zahl;
        }

        /// <summary>Ein Vergleicher, der bei <c>eff.jaz</c> wirft — so wirft die Quelle von
        /// <c>stamm.kennzahl.eff.jaz</c> beim Nachschlagen.</summary>
        private sealed class WerfenderVergleicher : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.Ordinal);

            public int GetHashCode(string s)
            {
                if (s == "eff.jaz") throw new InvalidOperationException("Probe: die Quelle wirft");
                return StringComparer.Ordinal.GetHashCode(s);
            }
        }
    }
}
