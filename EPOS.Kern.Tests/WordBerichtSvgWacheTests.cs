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
using WindowsFormsApplication1.Zeichnung;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;
using ASVG = DocumentFormat.OpenXml.Office2019.Drawing.SVG;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache über das SVG im WORTBERICHT</b> (Entscheid DG-E3-8,
    /// Anwenderentscheid 20.09.2026 „alle Grafiken, soweit möglich").
    ///
    /// <para><b>Die Regel.</b> Jede Bildstelle, deren Renderer-Methode ein
    /// <c>Zeichenmodell</c> hat, legt im Dokument ZWEI Teile ab: das PNG als
    /// <c>a:blip</c> — der Rückfall für jeden Leser vor Word 2016 und für jeden
    /// Konverter — und den SVG-Text als zweiten <c>ImagePart</c> mit dem Inhaltstyp
    /// <c>image/svg+xml</c>, verknüpft über <c>asvg:svgBlip</c> in der
    /// Erweiterungsliste des Blips. Maße und Lage bleiben die des PNG.</para>
    ///
    /// <para><b>Warum eine Wache.</b> Ein Dokument, dem die SVG-Verknüpfung fehlt,
    /// sieht in Word genauso aus wie eines, das sie hat — nur unschärfer. Der Fehler
    /// fiele also erst am gedruckten Bericht auf. Und ein Dokument, dessen
    /// Erweiterung auf einen Teil zeigt, den es nicht gibt, ist kaputt, ohne dass
    /// irgendetwas es meldete: Word zeigt dann stumm das PNG.</para>
    ///
    /// <para>Der Excelbericht bleibt bewusst beim PNG (ClosedXML kennt die Office-
    /// Erweiterung nicht), und einen eigenen PDF-Weg gibt es im Programm nicht —
    /// ein PDF entsteht aus dem Wortbericht heraus und erbt dessen Bilder.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WordBerichtSvgWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int STAMM = 9101;
        private const int VARIANTE_A = 9102;

        // =====================================================================
        //  1 — die Einbettung selbst: ein Modell, zwei Teile
        // =====================================================================

        /// <summary>
        /// Jedes der vier Berichtsbilder der Gruppe (d) legt beide Teile ab: einen
        /// PNG-Teil, auf den der <c>a:blip</c> zeigt, und einen SVG-Teil, auf den der
        /// <c>asvg:svgBlip</c> darin zeigt. Der SVG-Teil beginnt wohlgeformt mit
        /// <c>&lt;svg</c> und trägt keinen BOM.
        /// </summary>
        [Theory]
        [InlineData("jahresverlauf")]
        [InlineData("dauerlinie")]
        [InlineData("speicherverlauf")]
        [InlineData("speichertemperaturen")]
        public void EinModellLegtBeideTeileAb(string bild)
        {
            Zeichenmodell m = Bildmodell(bild);
            Assert.NotNull(m);

            using var ms = new MemoryStream();
            using (WordprocessingDocument doc =
                   WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body());

                new WordKontext(main, main.Document.Body, null).Bild(m, 620, 280);

                A.Blip blip = Assert.Single(main.Document.Descendants<A.Blip>());

                // Der Rueckfall: das PNG.
                ImagePart png = (ImagePart)main.GetPartById(blip.Embed.Value);
                Assert.Equal("image/png", png.ContentType);
                Assert.Equal(SkiaMaler.Png(m), Inhalt(png));

                // Die Erweiterung mit der festgelegten Kennung.
                A.BlipExtension ext = Assert.Single(blip.Descendants<A.BlipExtension>());
                Assert.Equal(WordBerichtGenerator.SVG_EXT_URI, ext.Uri.Value);

                ASVG.SVGBlip svgBlip = Assert.Single(ext.Descendants<ASVG.SVGBlip>());
                ImagePart svg = (ImagePart)main.GetPartById(svgBlip.Embed.Value);
                Assert.Equal("image/svg+xml", svg.ContentType);

                byte[] roh = Inhalt(svg);
                Assert.False(roh.Length > 2 && roh[0] == 0xEF && roh[1] == 0xBB && roh[2] == 0xBF,
                             "Der SVG-Teil trägt eine Vorzeichenfolge (BOM).");

                string text = Encoding.UTF8.GetString(roh);
                Assert.StartsWith("<svg", text, StringComparison.Ordinal);
                Assert.Equal(SvgSchreiber.Text(m), text);

                Assert.Equal(2, main.ImageParts.Count());
            }
        }

        /// <summary>
        /// Der <c>byte[]</c>-Weg bleibt, was er war: EIN Teil, kein <c>svgBlip</c>.
        /// Die Bilder ohne Zeichenmodell (Gruppen b und c) gehen weiter darüber.
        /// </summary>
        [Fact]
        public void DerByteWegLegtNurDasPngAb()
        {
            byte[] png = ChartRenderer.JahresverlaufWaerme(ChartRendererGruppeDTests.Satz());
            Assert.NotNull(png);

            using var ms = new MemoryStream();
            using (WordprocessingDocument doc =
                   WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body());

                new WordKontext(main, main.Document.Body, null).Bild(png, 620, 280);

                Assert.Single(main.ImageParts);
                Assert.Empty(main.Document.Descendants<ASVG.SVGBlip>());
            }
        }

        /// <summary>Ein Modell, das der Lauf nicht hergibt, schreibt gar nichts.</summary>
        [Fact]
        public void EinNullModellSchreibtNichts()
        {
            using var ms = new MemoryStream();
            using (WordprocessingDocument doc =
                   WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body());

                new WordKontext(main, main.Document.Body, null).Bild((Zeichenmodell)null, 620, 280);

                Assert.Empty(main.ImageParts);
                Assert.Empty(main.Document.Body.Elements<Paragraph>());
            }
        }

        // =====================================================================
        //  2 — der ganze Bericht
        // =====================================================================

        /// <summary>
        /// Der erzeugte Wortbericht ist gültiges OpenXML — mit der Office-2019-
        /// Erweiterung darin. Ein Validator-Fehler heißt: Word meldet das Dokument als
        /// beschädigt.
        /// </summary>
        [Fact]
        public void DerBerichtIstGueltigesOpenXml()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Bericht(ordner);

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                var validator = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2019);
                List<ValidationErrorInfo> fehler = validator.Validate(doc).ToList();

                Assert.True(fehler.Count == 0,
                    "Der Wortbericht ist nicht gültig: " + string.Join(" | ",
                        fehler.Take(5).Select(f => f.Description + " @ " + f.Path?.XPath)));
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// <b>Die eigentliche Wache.</b> Im fertigen Bericht steht zu JEDEM
        /// <c>svgBlip</c> ein SVG-Teil, der wohlgeformt beginnt, und der Blip darüber
        /// zeigt auf einen PNG-Teil. Es gibt so viele SVG-Teile wie Verweise — kein
        /// verwaister Teil, kein Verweis ins Leere —, und es sind mindestens die vier
        /// Bilder der Gruppe (d).
        /// </summary>
        [Fact]
        public void JedeBildstelleMitModellTraegtBeideTeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Bericht(ordner);

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                MainDocumentPart main = doc.MainDocumentPart;

                List<A.Blip> blips = main.Document.Descendants<A.Blip>().ToList();
                Assert.NotEmpty(blips);

                int mitSvg = 0;
                foreach (A.Blip blip in blips)
                {
                    // Der Rueckfall steht IMMER — auch an den Stellen mit Modell.
                    ImagePart png = (ImagePart)main.GetPartById(blip.Embed.Value);
                    Assert.Equal("image/png", png.ContentType);

                    ASVG.SVGBlip svgBlip = blip.Descendants<ASVG.SVGBlip>().FirstOrDefault();
                    if (svgBlip == null) continue;
                    mitSvg++;

                    A.BlipExtension ext = blip.Descendants<A.BlipExtension>().First();
                    Assert.Equal(WordBerichtGenerator.SVG_EXT_URI, ext.Uri.Value);

                    ImagePart svg = (ImagePart)main.GetPartById(svgBlip.Embed.Value);
                    Assert.Equal("image/svg+xml", svg.ContentType);
                    Assert.StartsWith("<svg", Encoding.UTF8.GetString(Inhalt(svg)),
                                      StringComparison.Ordinal);
                }

                // Die vier Bilder der Gruppe (d): Jahresverlauf, Dauerlinie und
                // Speicherverlauf je Variante (zwei), dazu die Speichertemperaturen
                // des Stamms — mindestens vier, und nie mehr Teile als Verweise.
                Assert.True(mitSvg >= 4, "Nur " + mitSvg + " Bildstellen tragen ein SVG.");

                int svgTeile = main.ImageParts.Count(p => p.ContentType == "image/svg+xml");
                Assert.Equal(mitSvg, svgTeile);
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static Zeichenmodell Bildmodell(string bild)
        {
            ZeitreihenSatz z = ChartRendererGruppeDTests.Satz();
            switch (bild)
            {
                case "jahresverlauf": return ChartRenderer.JahresverlaufWaermeModell(z);
                case "dauerlinie": return ChartRenderer.DauerlinieWaermeModell(z);
                case "speicherverlauf": return ChartRenderer.SpeicherverlaufModell(z);
                case "speichertemperaturen": return ChartRenderer.SpeichertemperaturenModell(z);
                default: throw new ArgumentOutOfRangeException(nameof(bild), bild, "unbekanntes Bild");
            }
        }

        private static byte[] Inhalt(ImagePart teil)
        {
            using Stream s = teil.GetStream();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }

        /// <summary>Der Bericht mit Ganglinien: beide Stände tragen einen Zeitreihensatz.</summary>
        private static string Bericht(string ordner)
        {
            string ziel = Path.Combine(ordner, "probe.docx");
            new WordBerichtGenerator().Erzeuge(Gruppendaten(), Konfiguration(), ziel);
            Assert.True(File.Exists(ziel), "Der Bericht wurde nicht geschrieben.");
            return ziel;
        }

        private static BerichtsDaten Gruppendaten()
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(STAMM, true, "Stammprojekt"));
            daten.Varianten.Add(Stand(VARIANTE_A, false, "Variante A"));
            return daten;
        }

        private static VariantenDaten Stand(int id, bool istStamm, string name)
        {
            var ergebnis = new ErgebnisModel();

            // Eine Speicherzeile MIT Temperaturkennzahl — sonst lässt der Baustein
            // „Projektbeschreibung" den Abschnitt samt Bild aus.
            ergebnis.Pufferspeicher.Add(new ErgebnisPufferspeicherModel
            {
                ID_Pufferspeicher = 11,
                Bezeichner = "Heizungspuffer",
                Verwendung = "Heizung",
                T_oben_Mittel = 62.0,
                T_oben_Min = 48.0
            });

            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = istStamm,
                Projektname = "Stammprojekt",
                Variantenname = istStamm ? "" : name,
                Ergebnis = ergebnis,
                Zeitreihen = ChartRendererGruppeDTests.Satz()
            };
        }

        /// <summary>
        /// Nur die zwei Bausteine, die Bilder der Gruppe (d) zeichnen — die
        /// Wirtschaftlichkeit braucht eine gerechnete Zahlungsreihe und trägt zu
        /// dieser Frage nichts bei.
        /// </summary>
        private static BerichtsKonfiguration Konfiguration()
        {
            var k = new BerichtsKonfiguration();
            k.AktiveBausteine.Add(BerichtsKonfiguration.B_PROJEKT);
            k.AktiveBausteine.Add(BerichtsKonfiguration.B_ERGEBNISSE);
            return k;
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(),
                                    "epos-dge3d-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}
