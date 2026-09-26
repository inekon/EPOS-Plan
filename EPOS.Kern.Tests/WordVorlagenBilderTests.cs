using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using SVG = DocumentFormat.OpenXml.Office2019.Drawing.SVG;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Bildplatzhalter der Word-Engine</b> (Konzept Berichtsvorlagen 4.2, 4.6 BV-P5, 4.10, 5.4, 6.5, 6.8;
    /// Etappe BV-E5): <c>bild.*</c>, <c>stand.bild.*</c>, <c>stamm.bild.*</c> im Alternativtext eines Bildes
    /// oder als Text allein im Absatz.
    ///
    /// <para><b>Was sie halten.</b> (a) Jedes der dreizehn Berichtsbilder (sechzehn Schlüssel) entsteht als
    /// Platzhalter für 1030 bzw. die Gruppe 1019 — PNG mit SVG-Fassung, Alternativtext = Titel des Diagramms,
    /// Rahmen und Lage erhalten, <c>a:srcRect</c> weg —, gefüllt nach dem Sammler mit dem Bedarf der Vorlage
    /// und unter einem Zugriff, der bei jedem Datenbankvorgang wirft; nichts wird nachgeholt. (b) Stufe 2: zwei
    /// Rahmen, zwei Zeichenmaße. (c) Text allein im Absatz: Satzspiegelbreite; im Satz: Fehler. (d) Bild ohne
    /// Modell: Hinweisabsatz mit Grund, die Laufmeldung nennt es. (e) <c>stand.bild.*</c> im Block mit drei
    /// Ständen, außerhalb ein Kontextfehler. (f) Kein Nachholen: ohne erhobene Reihen und ohne Verlauf bleiben
    /// die Bilder mit Grund leer. (g) Prüfer: Satz, unbekannt, vorgemerkt, Kontext, Bildrahmen unter 80 %.
    /// (h) Bedarf der Vorlage und Bildschalter. Jede erzeugte Datei hält der OpenXmlValidator.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WordVorlagenBilderTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e5-bilder");

        public WordVorlagenBilderTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            Probevorlagen.Aufraeumen(_ordner);
            _kultur.Dispose();
        }

        /// <summary>Die Gruppe der Testdatenbank mit zwei Varianten und Kraftwerkspark.</summary>
        private const int GRUPPE_1019 = 1019;

        /// <summary>Ein Rahmen in voller Berichtsbreite (620 px) — Höhe 280 px.</summary>
        private const long BREIT = 620L * 9525L, HOCH = 280L * 9525L;

        /// <summary>Ein Rahmen in halber Satzspiegelbreite (311 px) — Höhe 200 px.</summary>
        private const long HALB = 311L * 9525L, HALB_HOCH = 200L * 9525L;

        private static readonly string[] Standbilder =
        {
            "stand.bild.waerme_jahresverlauf", "stand.bild.waerme_dauerlinie", "stand.bild.strombilanz_monate",
            "stand.bild.speicherverlauf", "stand.bild.deckung_waerme", "stand.bild.deckung_strom", "stand.bild.zahlungsstrom",
        };

        private static readonly string[] Gruppenbilder =
        {
            "stamm.bild.speichertemperaturen",
            "bild.vergleich.balken.energie.brennstoff", "bild.vergleich.balken.energie.netzbezug",
            "bild.vergleich.balken.energie.waermerest", "bild.vergleich.balken.eff.jaz",
            "bild.wirtschaft.kapitalwert_szenarien", "bild.wirtschaft.barwerte_kumuliert",
            "bild.wirtschaft.bruecke", "bild.wirtschaft.spanne",
        };

        // =====================================================================
        //  (a) Jedes Bild — nach dem Sammler, ohne Datenbank, ohne Nachholen
        // =====================================================================

        /// <summary>
        /// <b>Die sechzehn Bildschlüssel</b> in einer Vorlage (die je Stand im Block <c>je stand</c>), der Bedarf
        /// aus der Vorlage (<see cref="Berichtsbedarf.AusVorlage"/>): Er trägt Stundenreihen und Verlauf; der
        /// Sammler erhebt sie für 1030 und die Gruppe 1019; gefüllt wird unter einem werfenden Zugriff. Jeder
        /// Schlüssel bekommt in wenigstens einem der beiden Läufe sein Bild — PNG und SVG, Titel als
        /// Alternativtext, Breite des Rahmens, Rahmenlinie erhalten, Zuschnitt weg —; keiner holt nach.
        /// </summary>
        [Fact]
        public void Jedes_Berichtsbild_entsteht_als_Platzhalter_nach_dem_Sammler_ohne_Datenbank()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            byte[] vorlage = AlleBilderVorlage();
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, new Pruefkontext { AnzahlVarianten = 2 });
            Assert.Empty(befund.UnbekannteSchluessel);
            Assert.DoesNotContain(befund.Meldungen, m => m.Stufe == Befundstufe.Fehler);
            Berichtsbedarf bedarf = Berichtsbedarf.AusVorlage(befund, konfig);
            Assert.True(bedarf.Zeitreihen, "Die Zeitreihenbilder tragen den Bedarf der Stundenreihen.");
            Assert.True(bedarf.Verlauf, "Die Verlaufsbilder tragen den Bedarf des Kapitalwertverlaufs.");

            var gefuellt = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (int stamm in new[] { Berichtsdatenproben.PROJEKT_1030, GRUPPE_1019 })
            {
                BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(stamm, "Probe " + stamm, Varianten(stamm),
                    bedarf, null, CancellationToken.None, null);
                Assert.True(daten.Wirtschaft.Gesammelt);

                string ziel = Path.Combine(_ordner, "alle_" + stamm + ".docx");
                Fuellergebnis ergebnis = null;
                List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() =>
                    ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, vorlage, Ersteller(), ziel));
                foreach (string z in ergebnis.Meldungen()) _ausgabe.WriteLine(stamm + ": " + z);

                Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Füllen:\n" + string.Join("\n", zugriffe.Take(10)));
                Assert.Empty(daten.Wirtschaft.Nachgeholt);
                Assert.Empty(ergebnis.Unbekannte);
                Assert.Empty(Validierungsfehler(ziel));

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Assert.DoesNotContain("{{", doc.MainDocumentPart.Document.Body.InnerText);
                AssertKennungenEindeutig(doc);
                foreach (DW.DocProperties d in doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>())
                {
                    string schluessel = (d.Name?.Value ?? "").Replace("Bild ", "");
                    PruefeDiagramm(doc, d, BREIT);
                    if (!gefuellt.ContainsKey(schluessel)) gefuellt[schluessel] = stamm + ": " + d.Description?.Value;
                }
            }

            foreach (KeyValuePair<string, string> g in gefuellt.OrderBy(p => p.Key, StringComparer.Ordinal))
                _ausgabe.WriteLine(g.Key + " ← " + g.Value);
            List<string> fehlen = Standbilder.Concat(Gruppenbilder).Where(s => !gefuellt.ContainsKey(s)).ToList();
            Assert.True(fehlen.Count == 0, "Ohne Bild in 1030 und 1019: " + string.Join(", ", fehlen));
            Assert.Equal("Wärmeerzeugung im Jahresverlauf (Tagesmittel) [kW]",
                         gefuellt["stand.bild.waerme_jahresverlauf"].Substring(gefuellt["stand.bild.waerme_jahresverlauf"].IndexOf(": ", StringComparison.Ordinal) + 2));
        }

        /// <summary>
        /// Die synthetische Gruppe mit drei Ständen und Wirtschaftlichkeit: jedes Bild mit Modell — auch das
        /// Speichertemperaturbild und die Deckung, wo die Testdatenbank kein Modell hergibt.
        /// </summary>
        [Fact]
        public void Die_synthetische_Gruppe_fuellt_die_Bilder_der_Wirtschaftlichkeit_und_der_Speicher()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE, 3);
            string ziel = Path.Combine(_ordner, "gruppe.docx");
            Fuellergebnis ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(daten, Berichtsdatenproben.VolleKonfiguration(),
                AlleBilderVorlage(), Ersteller(), ziel);
            foreach (string z in ergebnis.Meldungen()) _ausgabe.WriteLine(z);
            Assert.Empty(ergebnis.Unbekannte);
            Assert.Empty(Validierungsfehler(ziel));

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            var namen = new HashSet<string>(StringComparer.Ordinal);
            foreach (DW.DocProperties d in doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>())
            {
                PruefeDiagramm(doc, d, BREIT);
                namen.Add((d.Name?.Value ?? "").Replace("Bild ", ""));
            }
            // Die synthetischen Stände tragen keine Kennzahlen — die Vergleichsbalken hält die Gruppe 1019.
            foreach (string s in new[] { "stamm.bild.speichertemperaturen", "stand.bild.speicherverlauf",
                                         "bild.wirtschaft.spanne", "bild.wirtschaft.bruecke", "stand.bild.zahlungsstrom" })
                Assert.Contains(s, namen);
            // Drei Stände im Block: je Standbild mit Modell drei Bilder.
            Assert.Equal(3, doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>()
                               .Count(d => d.Name?.Value == "Bild stand.bild.waerme_jahresverlauf"));
        }

        // =====================================================================
        //  (b) Stufe 2 in zwei Größen
        // =====================================================================

        /// <summary>
        /// <b>Stufe 2:</b> Derselbe Schlüssel in einem vollen (620 × 280 px) und einem halben Rahmen (311 × 200 px):
        /// Der Renderer zeichnet in der doppelten Rahmengröße (1240 × 560 und 622 × 400 Bildpunkte), die Breite des
        /// Rahmens bleibt, die Höhe folgt dem Modell; ein Rahmen unter der Mindestbreite bekommt das Bild in der
        /// Mindestbreite (Stufe 1), und die Laufmeldung nennt den Anteil.
        /// </summary>
        [Fact]
        public void Stufe_2_zeichnet_in_der_Groesse_des_Rahmens()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            byte[] vorlage = Vorlage(main =>
                "<w:p><w:r><w:t>{{#je stand}}</w:t></w:r></w:p>" +
                "<w:p>" + Rahmenbild(main, 11, "voll", "{{stand.bild.waerme_jahresverlauf}}", BREIT, HOCH) + "</w:p>" +
                "<w:p>" + Rahmenbild(main, 12, "halb", "{{stand.bild.waerme_jahresverlauf}}", HALB, HALB_HOCH) + "</w:p>" +
                "<w:p>" + Rahmenbild(main, 13, "klein", "{{stand.bild.waerme_jahresverlauf}}", 150L * 9525L, 100L * 9525L) + "</w:p>" +
                "<w:p><w:r><w:t>{{/je}}</w:t></w:r></w:p>");
            string ziel = Path.Combine(_ordner, "stufe2.docx");
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(), vorlage, Ersteller(), ziel);
            foreach (string z in e.Meldungen()) _ausgabe.WriteLine(z);
            Assert.Empty(Validierungsfehler(ziel));

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<DW.DocProperties> bilder = doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>().ToList();
            Assert.Equal(6, bilder.Count);   // zwei Stände × drei Rahmen

            (int b, int h) voll = Pixel(doc, bilder.First(d => d.Name == "voll"));
            (int b, int h) halb = Pixel(doc, bilder.First(d => d.Name == "halb"));
            (int b, int h) klein = Pixel(doc, bilder.First(d => d.Name == "klein"));
            Assert.Equal((1240, 560), voll);
            Assert.Equal((622, 400), halb);
            Assert.Equal(Bildmass.MIN_BREITE, klein.b);     // Stufe 1: in der Mindestbreite gezeichnet

            Assert.Equal(BREIT, Extent(bilder.First(d => d.Name == "voll")).Cx.Value);
            Assert.Equal(HALB, Extent(bilder.First(d => d.Name == "halb")).Cx.Value);
            Assert.Equal(HALB_HOCH, Extent(bilder.First(d => d.Name == "halb")).Cy.Value);
            Assert.Contains(e.Hinweise, h => h.StartsWith("stand.bild.waerme_jahresverlauf: Der Rahmen ist schmaler", StringComparison.Ordinal));
        }

        // =====================================================================
        //  (c) Text allein im Absatz, im Satz
        // =====================================================================

        /// <summary>
        /// Der Schlüssel als Text allein im Absatz: das Bild in Satzspiegelbreite (A4, 9 355 DXA → 623 px, gezeichnet
        /// 1 246 Bildpunkte breit), mit Titel als Alternativtext; das Absatzformat bleibt. Im Satz bleibt der
        /// Platzhalter gelb stehen, das Ergebnis nennt den Fehler.
        /// </summary>
        [Fact]
        public void Text_allein_im_Absatz_steht_in_Satzspiegelbreite_im_Satz_ist_ein_Fehler()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            byte[] vorlage = Vorlage(main =>
                "<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:t>{{stamm.bild.speichertemperaturen}}</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t xml:space=\"preserve\">Hier {{stamm.bild.speichertemperaturen}} im Satz.</w:t></w:r></w:p>");
            string ziel = Path.Combine(_ordner, "absatz.docx");
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(), vorlage, Ersteller(), ziel);
            foreach (string z in e.Meldungen()) _ausgabe.WriteLine(z);
            Assert.Empty(Validierungsfehler(ziel));

            Fuellbefund satz = Assert.Single(e.Unbekannte);
            Assert.Equal(Fuellbefundart.FalscheStelle, satz.Art);
            Assert.Contains(e.Fehler, f => f.Contains("Ein Bild steht allein in einem Absatz", StringComparison.Ordinal));

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            DW.DocProperties d = Assert.Single(doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>());
            Assert.Equal("Speichertemperaturen — oberste und unterste Schicht [°C]", d.Description?.Value);
            Assert.Equal(623L * 9525L, Extent(d).Cx.Value);
            Assert.Equal(1246, Pixel(doc, d).b);
            Paragraph absatz = d.Ancestors<Paragraph>().First();
            Assert.Equal(JustificationValues.Center, absatz.ParagraphProperties?.Justification?.Val?.Value);
            PruefeDiagramm(doc, d, 623L * 9525L);
        }

        // =====================================================================
        //  (d) Bild ohne Modell
        // =====================================================================

        /// <summary>
        /// <b>Bild ohne Modell</b> (Konzept 4.10): Das Balkenbild braucht zwei Stände, die Gruppe hat nur den Stamm —
        /// an der Stelle des Bildes steht der Hinweis „Diagramm entfällt: …“ mit dem Grund, das Platzhalterbild und
        /// sein Bildteil sind weg; die Laufmeldung nennt den Schlüssel, die Leermeldung zählt ihn.
        /// </summary>
        [Fact]
        public void Bild_ohne_Modell_wird_ein_Hinweis_mit_Grund()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(1);
            byte[] vorlage = Vorlage(main =>
                "<w:p>" + Rahmenbild(main, 3, "balken", "{{bild.vergleich.balken.eff.jaz}}", BREIT, HOCH) + "</w:p>" +
                "<w:p><w:r><w:t>{{bild.vergleich.balken.eff.jaz}}</w:t></w:r></w:p>");
            string ziel = Path.Combine(_ordner, "ohne_modell.docx");
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(), vorlage, Ersteller(), ziel);
            foreach (string z in e.Meldungen()) _ausgabe.WriteLine(z);
            Assert.Empty(Validierungsfehler(ziel));
            Assert.Empty(e.Unbekannte);

            string grund = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(nameof(WindowsFormsApplication1.MyResource.Resource.BV_GRUND_ZU_WENIG_STAENDE),
                                                                          BerichtTexte.KulturFuer(false));
            Assert.Contains(e.Warnungen, w => w == "bild.vergleich.balken.eff.jaz: kein Bild — " + grund);
            Assert.Equal(2, e.Leere["bild.vergleich.balken.eff.jaz"]);

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Assert.Empty(doc.MainDocumentPart.Document.Body.Descendants<Drawing>());
            Assert.Empty(doc.MainDocumentPart.ImageParts);
            List<Paragraph> hinweise = doc.MainDocumentPart.Document.Body.Elements<Paragraph>()
                .Where(p => p.InnerText == "Diagramm entfällt: " + grund).ToList();
            Assert.Equal(2, hinweise.Count);
            Assert.All(hinweise, p => Assert.NotNull(p.ParagraphProperties?.ParagraphStyleId));
        }

        // =====================================================================
        //  (e) Block mit drei Ständen, Kontext
        // =====================================================================

        /// <summary>
        /// <c>stand.bild.*</c> im Block <c>je stand</c> mit drei Ständen: drei Bilder, je Stand eigene Bildteile,
        /// eindeutige Kennungen, der Bildteil des Platzhalters ist weg. Derselbe Schlüssel außerhalb des Blocks
        /// bleibt als Kontextverstoß stehen.
        /// </summary>
        [Fact]
        public void Standbilder_im_Block_mit_drei_Staenden_und_ausserhalb_ein_Kontextfehler()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(3);
            byte[] vorlage = Vorlage(main =>
                "<w:p><w:r><w:t>{{#je stand}}</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t>{{stand.bezeichner}}</w:t></w:r></w:p>" +
                "<w:p>" + Rahmenbild(main, 4, "strom", "{{stand.bild.strombilanz_monate}}", BREIT, HOCH) + "</w:p>" +
                "<w:p><w:r><w:t>{{/je}}</w:t></w:r></w:p>" +
                "<w:p>" + Rahmenbild(main, 5, "draussen", "{{stand.bild.strombilanz_monate}}", BREIT, HOCH) + "</w:p>");
            string ziel = Path.Combine(_ordner, "block.docx");
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(), vorlage, Ersteller(), ziel);
            foreach (string z in e.Meldungen()) _ausgabe.WriteLine(z);
            Assert.Empty(Validierungsfehler(ziel));

            Fuellbefund aussen = Assert.Single(e.Unbekannte);
            Assert.Equal(Fuellbefundart.Kontext, aussen.Art);

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<DW.DocProperties> bilder = doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>()
                                               .Where(d => d.Name == "strom").ToList();
            Assert.Equal(3, bilder.Count);
            foreach (DW.DocProperties d in bilder) PruefeDiagramm(doc, d, BREIT);
            AssertKennungenEindeutig(doc);
            // Drei Stände mit je PNG und SVG, dazu der stehen gebliebene Platzhalter außerhalb.
            Assert.Equal(3 * 2 + 1, doc.MainDocumentPart.ImageParts.Count());
            Assert.Equal("{{stand.bild.strombilanz_monate}}",
                         doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>().Single(d => d.Name == "draussen").Description?.Value);
        }

        // =====================================================================
        //  (f) Kein Nachholen
        // =====================================================================

        /// <summary>
        /// <b>Kein Nachholen:</b> Der Sammler erhebt weder Stundenreihen noch Verlauf (Bedarf „nichts“); die
        /// Zeitreihen- und Verlaufsbilder bleiben mit ihrem Grund leer, der Wertesatz rechnet den Verlauf nicht nach,
        /// und gefüllt wird ohne Datenbank.
        /// </summary>
        [Fact]
        public void Ohne_erhobene_Reihen_und_Verlauf_holt_kein_Bild_nach()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(GRUPPE_1019, "Probe", Varianten(GRUPPE_1019),
                Berichtsbedarf.Nichts, null, CancellationToken.None, null);
            Assert.Equal(0, daten.Wirtschaft.VerlaufRechnungen);

            byte[] vorlage = Vorlage(main =>
                "<w:p><w:r><w:t>{{#je stand}}</w:t></w:r></w:p>" +
                "<w:p>" + Rahmenbild(main, 1, "reihen", "{{stand.bild.waerme_jahresverlauf}}", BREIT, HOCH) + "</w:p>" +
                "<w:p><w:r><w:t>{{/je}}</w:t></w:r></w:p>" +
                "<w:p>" + Rahmenbild(main, 2, "verlauf", "{{bild.wirtschaft.barwerte_kumuliert}}", BREIT, HOCH) + "</w:p>");
            string ziel = Path.Combine(_ordner, "nachholen.docx");
            Fuellergebnis e = null;
            List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() =>
                e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(), vorlage, Ersteller(), ziel));
            foreach (string z in e.Meldungen()) _ausgabe.WriteLine(z);

            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe:\n" + string.Join("\n", zugriffe.Take(10)));
            Assert.Empty(daten.Wirtschaft.Nachgeholt);
            Assert.Equal(0, daten.Wirtschaft.VerlaufRechnungen);
            Assert.Empty(Validierungsfehler(ziel));

            string keineReihen = Text(nameof(WindowsFormsApplication1.MyResource.Resource.BV_GRUND_KEINE_ZEITREIHEN));
            string nichtErhoben = Text(nameof(WindowsFormsApplication1.MyResource.Resource.BV_GRUND_VERLAUF_NICHT_ERHOBEN));
            Assert.Contains(e.Warnungen, w => w == "stand.bild.waerme_jahresverlauf: kein Bild — " + keineReihen);
            Assert.Contains(e.Warnungen, w => w == "bild.wirtschaft.barwerte_kumuliert: kein Bild — " + nichtErhoben);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Assert.Empty(doc.MainDocumentPart.Document.Body.Descendants<Drawing>());
            Assert.Contains("Diagramm entfällt: " + nichtErhoben, doc.MainDocumentPart.Document.Body.InnerText);
        }

        // =====================================================================
        //  (g) Prüfer
        // =====================================================================

        /// <summary>
        /// Die Prüferregeln der Bilder: allein im Absatz kein Befund; im Satz ein Ortsfehler mit dem Rat
        /// „allein in einen Absatz … oder als Alternativtext“; ein unbekannter Bildschlüssel ein Fehler; ein
        /// vorgemerktes App-Diagramm „erst in einer späteren Programmfassung“; <c>stand.bild.*</c> außerhalb
        /// eines Standblocks ein Kontextfehler, im Block keiner; die gebauten Schlüssel nicht mehr „später“.
        /// </summary>
        [Fact]
        public void Pruefer_meldet_Satz_unbekannt_vorgemerkt_und_Kontext()
        {
            byte[] vorlage = Probevorlagen.AusAbsaetzen(
                "{{bild.wirtschaft.spanne}}",
                "Im Satz {{bild.wirtschaft.bruecke}} steht es falsch.",
                "{{bild.gibt.es.nicht}}",
                "{{bild.kosten.profil}}",
                "{{stand.bild.deckung_waerme}}",
                "{{#je stand}}", "{{stand.bild.deckung_strom}}", "{{/je}}");
            Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, new Pruefkontext());
            foreach (Pruefmeldung m in befund.Meldungen) _ausgabe.WriteLine(m.ToString());

            Pruefmeldung satz = Assert.Single(befund.Meldungen, m => m.Kennung == "VF_PRUEF_ORT");
            Assert.Equal("{{bild.wirtschaft.bruecke}}", satz.Marke);
            Assert.Contains("allein in einen Absatz", satz.WasTun);
            Assert.Contains("{{bild.wirtschaft.bruecke}} als Alternativtext", satz.WasTun);

            Pruefmeldung unbekannt = Assert.Single(befund.Meldungen, m => m.Kennung == "VF_PRUEF_UNBEKANNT");
            Assert.Contains("bild.gibt.es.nicht", unbekannt.Text);

            Pruefmeldung spaeter = Assert.Single(befund.Meldungen, m => m.Kennung == "VF_PRUEF_SPAETER");
            Assert.Equal(Befundstufe.Fehler, spaeter.Stufe);
            Assert.Equal("{{bild.kosten.profil}}", spaeter.Marke);

            Pruefmeldung kontext = Assert.Single(befund.Meldungen, m => m.Kennung == "VF_PRUEF_KONTEXT_STAND");
            Assert.Equal("{{stand.bild.deckung_waerme}}", kontext.Marke);
            Assert.Equal(4, befund.Meldungen.Count(m => m.Stufe == Befundstufe.Fehler));
        }

        /// <summary>
        /// <b>Bildrahmen unter 80 %</b> (volle Prüfung): Das Brückenbild zeichnet nicht schmaler als 1 000 Bildpunkte
        /// (500 px); ein Rahmen von 300 px ist 60 % davon — Warnung mit der Mindestbreite in cm. Ein Rahmen von 620 px
        /// und der Jahresverlauf in 250 px (Mindestbreite 280 px: 89 %) bleiben ohne Befund; in 200 px (71 %) warnt
        /// der Prüfer. Die Schnellprüfung prüft keine Rahmen.
        /// </summary>
        [Fact]
        public void Pruefer_warnt_unter_80_Prozent_der_Zeichenbreite()
        {
            byte[] vorlage = Vorlage(main =>
                "<w:p>" + Rahmenbild(main, 1, "b300", "{{bild.wirtschaft.bruecke}}", 300L * 9525L, HOCH) + "</w:p>" +
                "<w:p>" + Rahmenbild(main, 2, "b620", "{{bild.wirtschaft.bruecke}}", BREIT, HOCH) + "</w:p>" +
                "<w:p><w:r><w:t>{{#je stand}}</w:t></w:r></w:p>" +
                "<w:p>" + Rahmenbild(main, 3, "j250", "{{stand.bild.waerme_jahresverlauf}}", 250L * 9525L, HOCH) + "</w:p>" +
                "<w:p>" + Rahmenbild(main, 4, "j200", "{{stand.bild.waerme_jahresverlauf}}", 200L * 9525L, HOCH) + "</w:p>" +
                "<w:p><w:r><w:t>{{/je}}</w:t></w:r></w:p>");
            Pruefbefund voll = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, new Pruefkontext());
            foreach (Pruefmeldung m in voll.Meldungen) _ausgabe.WriteLine(m + " | " + m.WasTun);
            List<Pruefmeldung> rahmen = voll.Meldungen.Where(m => m.Kennung == "VF_PRUEF_BILDRAHMEN").ToList();
            Assert.Equal(2, rahmen.Count);
            Assert.All(rahmen, m => Assert.Equal(Befundstufe.Warnung, m.Stufe));
            Assert.Contains(rahmen, m => m.Marke == "{{bild.wirtschaft.bruecke}}" && m.Text.Contains("60 %") && m.WasTun.Contains("10,6 cm"));
            Assert.Contains(rahmen, m => m.Marke == "{{stand.bild.waerme_jahresverlauf}}" && m.Text.Contains("71 %"));
            Assert.DoesNotContain(voll.Meldungen, m => m.Stufe == Befundstufe.Fehler);

            Pruefbefund schnell = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, new Pruefkontext());
            Assert.DoesNotContain(schnell.Meldungen, m => m.Kennung == "VF_PRUEF_BILDRAHMEN");
        }

        // =====================================================================
        //  (h) Bedarf und Schalter
        // =====================================================================

        /// <summary>
        /// Der Bedarf der Bildschlüssel: Zeitreihenbilder Stundenreihen, Verlaufsbilder den Verlauf und — als Zahl
        /// der Wirtschaftlichkeit — die Stundenreihen, wenn das Häkchen „Ergebnisse je Variante“ sie verlangt; die
        /// Deckung und das Spannenbild keinen eigenen. Die Schalter tragen den Bedarf ihres Bildes.
        /// </summary>
        [Fact]
        public void Die_Bilder_tragen_ihren_Bedarf_und_die_Schalter_den_des_Bildes()
        {
            Assert.Equal(Vorlagenbedarf.Zeitreihen, Vorlagenfeldkatalog.Finde("stand.bild.waerme_jahresverlauf").Bedarf);
            Assert.Equal(Vorlagenbedarf.Zeitreihen, Vorlagenfeldkatalog.Finde("stamm.bild.speichertemperaturen").Bedarf);
            Assert.Equal(Vorlagenbedarf.Verlauf, Vorlagenfeldkatalog.Finde("bild.wirtschaft.bruecke").Bedarf);
            Assert.Equal(Vorlagenbedarf.Verlauf, Vorlagenfeldkatalog.Finde("stand.bild.zahlungsstrom").Bedarf);
            Assert.Equal(Vorlagenbedarf.Keiner, Vorlagenfeldkatalog.Finde("stand.bild.deckung_waerme").Bedarf);
            Assert.Equal(Vorlagenbedarf.Keiner, Vorlagenfeldkatalog.Finde("bild.wirtschaft.spanne").Bedarf);
            Assert.Equal(Vorlagenbedarf.Zeitreihen, Vorlagenfeldkatalog.Finde("hat.bild.waerme_jahresverlauf").Bedarf);

            foreach (string s in Standbilder.Concat(Gruppenbilder))
            {
                Vorlagenfeld f = Vorlagenfeldkatalog.Finde(s);
                Assert.NotNull(f);
                Assert.Equal(Vorlagenfeldart.Bild, f.Art);
                Assert.Equal(4, f.Seit);
                // BV-E8 (Katalog v6): jedes Berichtsbild hat in Excel sein Diagramm — Ausgabe beide, Fassung bleibt 4.
                Assert.Equal(Vorlagenausgabe.Beide, f.Ausgaben);
                string name = s.Substring(s.IndexOf("bild.", StringComparison.Ordinal) + 5);
                Vorlagenfeld schalter = Vorlagenfeldkatalog.Finde("hat.bild." + name);
                Assert.NotNull(schalter);
                Assert.Equal(Vorlagenfeldart.Schalter, schalter.Art);
                Assert.Equal(Vorlagenfeldkontext.Gruppe, schalter.Kontext);
            }
            Assert.All(Standbilder, s => Assert.Equal(Vorlagenfeldkontext.Stand, Vorlagenfeldkatalog.Finde(s).Kontext));
            Assert.All(Vorlagenfeldkatalog.VorgemerkteBilder, s => Assert.Null(Vorlagenfeldkatalog.Finde(s)));

            var konfig = new BerichtsKonfiguration();
            konfig.AktiveBausteine.Add(BerichtsKonfiguration.B_ERGEBNISSE);
            Berichtsbedarf nurBruecke = Berichtsbedarf.AusVorlage(
                Vorlagenpruefer.Pruefe(Probevorlagen.AusAbsaetzen("{{bild.wirtschaft.bruecke}}"), Pruefstufe.Schnell, new Pruefkontext()), konfig);
            Assert.True(nurBruecke.Verlauf);
            Assert.True(nurBruecke.Zeitreihen, "Eine Zahl der Wirtschaftlichkeit entsteht wie im Kapitel — mit den Stundenreihen.");
            Berichtsbedarf nurDeckung = Berichtsbedarf.AusVorlage(
                Vorlagenpruefer.Pruefe(Probevorlagen.AusAbsaetzen("{{#je stand}}", "{{stand.bild.deckung_strom}}", "{{/je}}"),
                                       Pruefstufe.Schnell, new Pruefkontext()), konfig);
            Assert.Equal(Vorlagenbedarf.Keiner, nurDeckung.Flags);
        }

        /// <summary>
        /// Die Bildschalter in Bedingungen: <c>hat.bild.vergleich.balken.eff.jaz</c> braucht zwei Stände;
        /// <c>hat.bild.speicherverlauf</c> gilt im Standblock für den laufenden Stand.
        /// </summary>
        [Fact]
        public void Bildschalter_steuern_Bedingungen()
        {
            byte[] vorlage = Probevorlagen.AusAbsaetzen(
                "{{#wenn hat.bild.vergleich.balken.eff.jaz}}", "BALKEN", "{{/wenn}}",
                "{{#wenn nicht hat.bild.wirtschaft.spanne}}", "OHNE SPANNE", "{{/wenn}}",
                "{{#je stand}}", "{{#wenn hat.bild.speicherverlauf}}", "SPEICHER {{stand.bezeichner}}", "{{/wenn}}", "{{/je}}");

            foreach ((int staende, bool balken) in new[] { (1, false), (2, true) })
            {
                BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(staende);
                foreach (VariantenDaten v in daten.Varianten) v.Kennzahlen["eff.jaz"] = 3.0 + v.IdProjekt % 3;
                string ziel = Path.Combine(_ordner, "schalter_" + staende + ".docx");
                Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(), vorlage, Ersteller(), ziel);
                foreach (string z in e.Meldungen()) _ausgabe.WriteLine(z);
                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                string text = doc.MainDocumentPart.Document.Body.InnerText;
                Assert.Equal(balken, text.Contains("BALKEN", StringComparison.Ordinal));
                Assert.Contains("OHNE SPANNE", text);
                Assert.Equal(staende, CountOf(text, "SPEICHER "));
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>
        /// Die Vorlage mit allen sechzehn Bildschlüsseln in Rahmen voller Breite (620 × 280 px; Name „Bild
        /// &lt;schlüssel&gt;“): die je Stand im Block <c>je stand</c>, die übrigen davor.
        /// </summary>
        private static byte[] AlleBilderVorlage()
        {
            return Vorlage(main =>
            {
                var xml = new System.Text.StringBuilder();
                int id = 1;
                foreach (string s in Gruppenbilder)
                    xml.Append("<w:p>").Append(Rahmenbild(main, id++, "Bild " + s, "{{" + s + "}}", BREIT, HOCH)).Append("</w:p>");
                xml.Append("<w:p><w:r><w:t>{{#je stand}}</w:t></w:r></w:p>");
                foreach (string s in Standbilder)
                    xml.Append("<w:p>").Append(Rahmenbild(main, id++, "Bild " + s, "{{" + s + "}}", BREIT, HOCH)).Append("</w:p>");
                xml.Append("<w:p><w:r><w:t>{{/je}}</w:t></w:r></w:p>");
                return xml.ToString();
            });
        }

        /// <summary>
        /// Prüft ein gefülltes Diagramm: PNG-Blip mit SVG-Fassung (<c>image/png</c>, <c>image/svg+xml</c>), der
        /// Alternativtext ist ein Titel (kein Platzhalter mehr), die Breite des Rahmens blieb, die Höhe ist höchstens
        /// die des Rahmens; die Rahmenlinie (<c>a:ln</c>) ist erhalten, der Zuschnitt (<c>a:srcRect</c>) weg.
        /// </summary>
        private static void PruefeDiagramm(WordprocessingDocument doc, DW.DocProperties d, long breite)
        {
            string beschreibung = d.Description?.Value ?? "";
            Assert.False(string.IsNullOrWhiteSpace(beschreibung), d.Name + ": ohne Alternativtext");
            Assert.DoesNotContain("{{", beschreibung);
            OpenXmlElement rahmen = d.Parent;
            A.Blip blip = rahmen.Descendants<A.Blip>().Single();
            Assert.Equal("image/png", doc.MainDocumentPart.GetPartById(blip.Embed.Value).ContentType);
            SVG.SVGBlip svg = blip.Descendants<SVG.SVGBlip>().Single();
            Assert.Equal("image/svg+xml", doc.MainDocumentPart.GetPartById(svg.Embed.Value).ContentType);
            DW.Extent ext = rahmen.GetFirstChild<DW.Extent>();
            Assert.True(ext.Cx.Value <= breite, d.Name + ": breiter als der Rahmen");
            Assert.True(ext.Cx.Value == breite || ext.Cy.Value == HOCH, d.Name + ": weder Breite noch Höhe des Rahmens");
            Assert.True(ext.Cy.Value <= Math.Max(HOCH, ext.Cy.Value), d.Name);
            Assert.Empty(rahmen.Descendants<A.SourceRectangle>());
            if (d.Name?.Value?.StartsWith("Bild ", StringComparison.Ordinal) == true || d.Name == "strom")
                Assert.Single(rahmen.Descendants<A.Outline>());
        }

        /// <summary>Die Bildpunkte des PNG hinter einem Bild (aus dem IHDR).</summary>
        private static (int b, int h) Pixel(WordprocessingDocument doc, DW.DocProperties d)
        {
            A.Blip blip = d.Parent.Descendants<A.Blip>().Single();
            using Stream s = doc.MainDocumentPart.GetPartById(blip.Embed.Value).GetStream();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            Bildinhalt bild = Bildinhalt.Aus(ms.ToArray(), "x.png");
            return (bild.Breite, bild.Hoehe);
        }

        private static DW.Extent Extent(DW.DocProperties d) => d.Parent.GetFirstChild<DW.Extent>();

        private static void AssertKennungenEindeutig(WordprocessingDocument doc)
        {
            List<uint> ids = doc.MainDocumentPart.Document.Body.Descendants<DW.DocProperties>().Select(d => d.Id.Value).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        private static int CountOf(string text, string teil)
        {
            int n = 0, i = 0;
            while ((i = text.IndexOf(teil, i, StringComparison.Ordinal)) >= 0) { n++; i += teil.Length; }
            return n;
        }

        private static string Text(string ressource)
            => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(ressource, BerichtTexte.KulturFuer(false));

        private static Erstellerangaben Ersteller() => new Erstellerangaben { Firma = "INEKON GmbH", Version = "9.9.9.9" };

        private static List<int> Varianten(int stamm)
            => new VariantenCtrl().LadeGruppe(stamm, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();

        private const string NS =
            "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" " +
            "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" " +
            "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
            "xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"";

        /// <summary>A4 hoch, Ränder 25/20 mm: Inhaltsbreite 9 355 DXA.</summary>
        private const string ABSCHNITT =
            "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/>" +
            "<w:pgMar w:top=\"1417\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1417\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr>";

        /// <summary>Ein PNG von 1 × 1 Bildpunkt (das Platzhalterbild).</summary>
        private const string PNG_1X1 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

        /// <summary>Eine Vorlage: <paramref name="rumpf"/> legt Bildteile an und liefert den Rumpf ohne Abschnittsangabe.</summary>
        private static byte[] Vorlage(Func<MainDocumentPart, string> rumpf)
        {
            using var ms = new MemoryStream();
            using (WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                string xml = rumpf(main);
                main.Document = new Document("<w:document " + NS + "><w:body>" + xml + ABSCHNITT + "</w:body></w:document>");
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Ein Platzhalterbild im Satz (<c>wp:inline</c>): eigener 1×1-Bildteil, Rahmen <paramref name="cx"/> ×
        /// <paramref name="cy"/> EMU, Alternativtext <paramref name="beschreibung"/>, mit Zuschnitt (<c>a:srcRect</c>)
        /// und Rahmenlinie (<c>a:ln</c>).
        /// </summary>
        private static string Rahmenbild(MainDocumentPart main, int kennung, string name, string beschreibung, long cx, long cy)
        {
            ImagePart teil = main.AddImagePart(ImagePartType.Png);
            using (var s = new MemoryStream(Convert.FromBase64String(PNG_1X1))) teil.FeedData(s);
            string embed = main.GetIdOfPart(teil);
            string x = cx.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string y = cy.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "<w:r><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">" +
                   "<wp:extent cx=\"" + x + "\" cy=\"" + y + "\"/><wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>" +
                   "<wp:docPr id=\"" + kennung + "\" name=\"" + name + "\" descr=\"" + beschreibung + "\"/>" +
                   "<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>" +
                   "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">" +
                   "<pic:pic><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"Platzhalter.png\" descr=\"" + beschreibung + "\"/><pic:cNvPicPr/></pic:nvPicPr>" +
                   "<pic:blipFill><a:blip r:embed=\"" + embed + "\"/><a:srcRect l=\"1000\" r=\"1000\"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>" +
                   "<pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"" + x + "\" cy=\"" + y + "\"/></a:xfrm>" +
                   "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom><a:ln w=\"12700\"><a:solidFill><a:srgbClr val=\"808080\"/></a:solidFill></a:ln></pic:spPr>" +
                   "</pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r>";
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
    }
}
