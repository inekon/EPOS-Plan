using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using SVG = DocumentFormat.OpenXml.Office2019.Drawing.SVG;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Energieflussbild im Word-Bericht</b> (Konzept Energieflussbild, Abschnitt 5; SK-4): der Platzhalter
    /// <c>stand.bild.energiefluss</c> samt Schalter <c>hat.bild.energiefluss</c> in Katalogfassung 25 (nur Word), die
    /// Bilanz beim Sammeln je Stand (<see cref="VariantenDaten.Energieflussbilanz"/>), das Bild im Baustein „Ergebnisse
    /// je Variante“ nach der Kennzahlentafel mit Beschriftung — die Knoten tragen ihre Jahressumme (MWh/a) — und ohne
    /// Fluss der benannte Satz statt des Bildes. Geschrieben wird ohne Datenbank.
    ///
    /// <para>Ist die Umgebungsvariable <c>EPOS_PROBE_ORDNER</c> gesetzt, legt der Gruppenfall seine Word-Datei dort ab
    /// (Sichtprobe; sonst bleibt sie im Temp-Ordner und wird entfernt).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtEnergieflussTests : IDisposable
    {
        private const string SCHLUESSEL = "stand.bild.energiefluss";

        /// <summary>Die Gruppe der Testdatenbank mit Stamm und zwei Varianten.</summary>
        private const int GRUPPE_1019 = 1019;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-sk4-energiefluss");

        public BerichtEnergieflussTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            Probevorlagen.Aufraeumen(_ordner);
            _kultur.Dispose();
        }

        // =====================================================================
        //  Katalog
        // =====================================================================

        [Fact]
        public void Bild_und_Schalter_stehen_in_Katalog_25_nur_Word_ohne_Zeitreihen()
        {
            Vorlagenfeld bild = Vorlagenfeldkatalog.Finde(SCHLUESSEL);
            Assert.NotNull(bild);
            Assert.Equal(Vorlagenfeldart.Bild, bild.Art);
            Assert.Equal(Vorlagenfeldkontext.Stand, bild.Kontext);
            Assert.Equal(25, bild.Seit);
            Assert.Equal(Vorlagenausgabe.Word, bild.Ausgaben);
            Assert.Equal(Vorlagenbedarf.Keiner, bild.Bedarf);

            Vorlagenfeld schalter = Vorlagenfeldkatalog.Finde("hat.bild.energiefluss");
            Assert.NotNull(schalter);
            Assert.Equal(Vorlagenfeldart.Schalter, schalter.Art);
            Assert.Equal(25, schalter.Seit);
            Assert.True(Vorlagenfeldkatalog.KatalogfassungWord >= 25);   // v26 (6b) kam danach

            // Das Kapitel „Ergebnisse je Variante“ deckt Bild und Schalter (der Baustein schreibt das Bild).
            IReadOnlyList<string> deckt = Vorlagenfeldkatalog.Finde(Berichtskapitel.Alle
                .Single(k => k.Name == Berichtskapitel.ERGEBNISSE).Schluessel).Deckt;
            int i = deckt.ToList().IndexOf(SCHLUESSEL);
            Assert.True(i >= 0 && deckt[i + 1] == "hat.bild.energiefluss");

            foreach (string ressource in new[] { "VF_STAND__BILD__ENERGIEFLUSS", "VF_HAT__BILD__ENERGIEFLUSS",
                                                 nameof(R.EF_BER_BESCHRIFTUNG), nameof(R.EF_BER_OHNE_BILD) })
            {
                string de = R.ResourceManager.GetString(ressource, BerichtTexte.KulturFuer(false));
                string en = R.ResourceManager.GetString(ressource, BerichtTexte.KulturFuer(true));
                Assert.False(string.IsNullOrWhiteSpace(de), ressource);
                Assert.False(string.IsNullOrWhiteSpace(en), ressource);
                Assert.NotEqual(de, en);
            }
        }

        [Fact]
        public void Das_Mass_folgt_der_Berichtsfassung_und_ergaenzt_eine_fehlende_Seite()
        {
            Bildmass bericht = ChartRenderer.ENERGIEFLUSS_BERICHT;
            Assert.Equal(bericht, Berichtsbilder.EnergieflussMass(null));
            Assert.Equal(bericht, Berichtsbilder.EnergieflussMass(new Bildmass(0, 0)));
            Bildmass halb = Berichtsbilder.EnergieflussMass(new Bildmass(bericht.Breite / 2, 0));
            Assert.Equal(bericht.Breite / 2, halb.Breite);
            Assert.InRange(halb.Hoehe, bericht.Hoehe / 2 - 1, bericht.Hoehe / 2 + 1);
            Assert.Equal(new Bildmass(900, 500), Berichtsbilder.EnergieflussMass(new Bildmass(900, 500)));
        }

        // =====================================================================
        //  Ohne Fluss: der benannte Satz
        // =====================================================================

        /// <summary>
        /// Die synthetische Gruppe (Ergebnisbaum ohne Flüsse) und ein Stand ohne Ergebnis: Der Baustein schreibt statt
        /// des Bildes den Satz „Kein Energieflussbild — Grund“, die Beschriftung fehlt; der Platzhalter bleibt mit Grund
        /// leer und sein Schalter ist aus.
        /// </summary>
        [Fact]
        public void Ohne_Fluss_steht_der_benannte_Satz_statt_des_Bildes()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            daten.Varianten[1].Ergebnis = null;
            Assert.All(daten.Varianten, v => Assert.Null(Berichtsbilder.Energiefluss(v, null, false)));

            string ziel = Path.Combine(_ordner, "ohne_fluss.docx");
            new WordBerichtGenerator().Erzeuge(daten, NurErgebnisse(), ziel);

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            List<string> absaetze = doc.MainDocumentPart.Document.Body.Descendants<Paragraph>().Select(p => p.InnerText).ToList();
            Assert.DoesNotContain(Text(nameof(R.EF_BER_BESCHRIFTUNG)), absaetze);
            string vorsatz = Text(nameof(R.EF_BER_OHNE_BILD)).Replace("{0}", "");
            List<string> saetze = absaetze.Where(a => a.StartsWith(vorsatz, StringComparison.Ordinal)).ToList();
            Assert.Equal(2, saetze.Count);
            Assert.Contains(saetze, s => s.EndsWith(string.Format(Text(nameof(R.EF_LEER)), "Stammprojekt"), StringComparison.Ordinal));
            Assert.Contains(saetze, s => s.EndsWith(string.Format(Text(nameof(R.EF_NICHT_SIMULIERT)), "Variante A"), StringComparison.Ordinal));
        }

        // =====================================================================
        //  Die Gruppe 1019: Bausteinweg und Platzhalter
        // =====================================================================

        /// <summary>
        /// <b>Der Bericht der Gruppe 1019</b> (Stamm und zwei Varianten): Der Sammler baut je Stand die Bilanz; der
        /// Baustein „Ergebnisse je Variante“ zeichnet je Stand das Bild (PNG mit SVG, Seitenverhältnis der Berichtsfassung)
        /// und darunter die Beschriftung, die Knoten nennen MWh/a. Dieselbe Bilanz füllt den Platzhalter im Block
        /// <c>je stand</c>, der Schalter ist an. Geschrieben wird ohne Datenbank; das Dokument hält der Validator.
        /// </summary>
        [Fact]
        public void Gruppe_1019_traegt_das_Bild_je_Stand_im_Baustein_und_im_Platzhalter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<int> varianten = new VariantenCtrl().LadeGruppe(GRUPPE_1019, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();
            Assert.Equal(2, varianten.Count);
            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(GRUPPE_1019, "Probe " + GRUPPE_1019, varianten,
                                                                              false, null, CancellationToken.None);
            Assert.Equal(3, daten.Varianten.Count);
            foreach (VariantenDaten v in daten.Varianten)
            {
                Assert.Null(v.Fehler);
                Assert.NotNull(v.Energieflussbilanz);
                Assert.False(v.Energieflussbilanz.Leer, v.Anzeige + ": " + v.Energieflussbilanz.Grund);
                Assert.False(v.Energieflussbilanz.Englisch);
            }

            // ---- Bausteinweg ----
            string ziel = Path.Combine(_ordner, "Bericht_Energiefluss_1019.docx");
            List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() => new WordBerichtGenerator().Erzeuge(daten, NurErgebnisse(), ziel));
            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Schreiben:\n" + string.Join("\n", zugriffe.Take(10)));
            Assert.Empty(Validierungsfehler(ziel));
            string probe = Environment.GetEnvironmentVariable("EPOS_PROBE_ORDNER");
            if (!string.IsNullOrWhiteSpace(probe))
            {
                Directory.CreateDirectory(probe);
                File.Copy(ziel, Path.Combine(probe, Path.GetFileName(ziel)), true);
            }

            using (WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false))
            {
                string beschriftung = Text(nameof(R.EF_BER_BESCHRIFTUNG));
                List<Paragraph> unter = doc.MainDocumentPart.Document.Body.Descendants<Paragraph>()
                    .Where(p => p.InnerText == beschriftung).ToList();
                Assert.Equal(3, unter.Count);
                Bildmass mass = ChartRenderer.ENERGIEFLUSS_BERICHT;
                foreach (Paragraph p in unter)
                {
                    Paragraph bild = Assert.IsType<Paragraph>(p.PreviousSibling<Paragraph>());
                    A.Blip blip = bild.Descendants<A.Blip>().Single();
                    Assert.Equal("image/png", doc.MainDocumentPart.GetPartById(blip.Embed.Value).ContentType);
                    SVG.SVGBlip svgBlip = blip.Descendants<SVG.SVGBlip>().Single();
                    string svg = Lies(doc.MainDocumentPart.GetPartById(svgBlip.Embed.Value));
                    Assert.Contains("MWh/a", svg, StringComparison.Ordinal);
                    DW.Extent ext = bild.Descendants<DW.Extent>().Single();
                    double soll = mass.Hoehe / (double)mass.Breite, ist = ext.Cy.Value / (double)ext.Cx.Value;
                    Assert.InRange(ist, soll * 0.98, soll * 1.02);
                }
                Assert.DoesNotContain(doc.MainDocumentPart.Document.Body.Descendants<Paragraph>(),
                                      p => p.InnerText.StartsWith(Text(nameof(R.EF_BER_OHNE_BILD)).Replace("{0}", ""), StringComparison.Ordinal));
            }

            // ---- Platzhalter und Schalter im Block je stand ----
            byte[] vorlage = Probevorlagen.AusAbsaetzen("{{#je stand}}", "{{#wenn hat.bild.energiefluss}}", "{{" + SCHLUESSEL + "}}",
                                                       "Schalter an", "{{/wenn}}", "{{/je}}");
            string gefuellt = Path.Combine(_ordner, "platzhalter_1019.docx");
            Fuellergebnis e = null;
            zugriffe = BerichtWertesatzTests.OhneDatenbank(() =>
                e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(), vorlage,
                                                                 new Erstellerangaben { Firma = "Firma 1", Version = "1.0" }, gefuellt));
            foreach (string m in e.Meldungen()) _ausgabe.WriteLine(m);
            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Füllen:\n" + string.Join("\n", zugriffe.Take(10)));
            Assert.Empty(e.Fehler);
            Assert.Empty(e.Unbekannte);
            Assert.Empty(Validierungsfehler(gefuellt));
            using (WordprocessingDocument doc = WordprocessingDocument.Open(gefuellt, false))
            {
                Body body = doc.MainDocumentPart.Document.Body;
                Assert.DoesNotContain("{{", body.InnerText);
                Assert.Equal(3, body.Descendants<Paragraph>().Count(p => p.InnerText == "Schalter an"));
                Assert.Equal(3, body.Descendants<SVG.SVGBlip>().Count());
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static BerichtsKonfiguration NurErgebnisse()
        {
            var k = new BerichtsKonfiguration();
            k.AktiveBausteine.Clear();
            k.AktiveBausteine.Add(BerichtsKonfiguration.B_ERGEBNISSE);
            return k;
        }

        private static string Text(string ressource)
            => R.ResourceManager.GetString(ressource, BerichtTexte.KulturFuer(false));

        private static string Lies(OpenXmlPart teil)
        {
            using Stream s = teil.GetStream();
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }

        private static List<string> Validierungsfehler(string pfad)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            return new OpenXmlValidator().Validate(doc).Select(f => f.Description).ToList();
        }
    }
}
