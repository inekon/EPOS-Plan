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

        // Die Berichtsbäume (Projekt 1030, synthetische Gruppe) und die volle Konfiguration
        // stehen in Berichtsdatenproben — dieselben, die die Messlatte der Berichtsvorlagen liest.

        // =====================================================================
        //  1 — die Einbettung selbst: ein Modell, zwei Teile
        // =====================================================================

        /// <summary>
        /// Jede Diagrammart des Wortberichts legt beide Teile ab: einen PNG-Teil, auf
        /// den der <c>a:blip</c> zeigt, und einen SVG-Teil, auf den der
        /// <c>asvg:svgBlip</c> darin zeigt. Der SVG-Teil beginnt wohlgeformt mit
        /// <c>&lt;svg</c> und trägt keinen BOM.
        ///
        /// <para>Die ersten vier sind die Berichtsbilder der Gruppe (d), die nächsten vier
        /// die Arten, die mit den Gruppen (b) und (c) ihr Zeichenmodell bekommen haben
        /// und seither ebenfalls über den Modellweg gehen; die nächsten zwei sind die Bilder
        /// der Etappe E6 (Verlauf mit drei Szenarien, Spannenbild), die letzten zwei die der
        /// Etappe E8a (Brückenbild U41, Zahlungsstrombild U42).</para>
        /// </summary>
        [Theory]
        [InlineData("jahresverlauf")]
        [InlineData("dauerlinie")]
        [InlineData("speicherverlauf")]
        [InlineData("speichertemperaturen")]
        [InlineData("strombilanz")]
        [InlineData("kuchen")]
        [InlineData("balken")]
        [InlineData("kapitalwert")]
        [InlineData("kapitalwert_szenarien")]
        [InlineData("kapitalwert_spanne")]
        [InlineData("kapitalwert_bruecke")]
        [InlineData("zahlungsstrom")]
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
                Assert.Equal(SvgSchreiber.Drucktext(m), text);

                // Der Druck traegt die Reihen als Pixelpfade: Word kennt
                // vector-effect nicht und dehnte Strich und Strichfolge des inneren
                // svg zu Baendern (Barwertverlauf im Wortbericht).
                Assert.DoesNotContain(SvgSchreiber.KLASSE_FLAECHE, text, StringComparison.Ordinal);
                Assert.DoesNotContain("vector-effect", text, StringComparison.Ordinal);

                Assert.Equal(2, main.ImageParts.Count());
            }
        }

        /// <summary>
        /// Der <c>byte[]</c>-Weg bleibt, was er war: EIN Teil, kein <c>svgBlip</c>.
        /// Kein Baustein des Berichts ruft ihn noch — er steht für FREMDBILDER und
        /// für die Renderer ohne Zeichenmodell bereit (<c>PeakShavingBild</c>,
        /// <c>SpeicherBetriebsbild</c>), falls eines davon einmal in den Bericht soll.
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

                // JEDE Bildstelle trägt inzwischen ein SVG — es gibt im Bericht keine
                // Diagrammart mehr ohne Zeichenmodell.
                Assert.True(mitSvg == blips.Count,
                    "Von " + blips.Count + " Bildstellen tragen nur " + mitSvg + " ein SVG.");
                Assert.True(mitSvg >= 4, "Nur " + mitSvg + " Bildstellen tragen ein SVG.");

                // Nie mehr Teile als Verweise — kein verwaister Teil, kein Verweis ins Leere.
                int svgTeile = main.ImageParts.Count(p => p.ContentType == "image/svg+xml");
                Assert.Equal(mitSvg, svgTeile);
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  3 — der Bericht aus dem REFERENZPROJEKT 1030
        // =====================================================================

        /// <summary>
        /// <b>Der Bericht, den ein Anwender bekommt.</b> Projekt 1030 der
        /// Testdatenbank, frisch simuliert, mit ALLEN Bausteinen — nicht die
        /// synthetische Prüfgruppe der Fälle darüber, sondern echte Reihen aus einem
        /// echten Lauf.
        ///
        /// <para><b>Die Regel:</b> Jede Bildstelle trägt beide Teile. Es gibt im
        /// Wortbericht keine Diagrammart mehr, die nur ein PNG ablegt — deshalb steht
        /// hier <c>mitSvg == blips.Count</c> und nicht eine Untergrenze. Die Zahl der
        /// Bilder selbst hängt am Projektstand der Testdatenbank (dieses Projekt
        /// führt sieben) und ist bewusst nur nach unten festgenagelt; die harte
        /// Aussage ist die Deckungsgleichheit.</para>
        ///
        /// <para>Dazu die Gültigkeit in <b>allen sechs</b> Office-Fassungen: Ein
        /// Bericht, den ein älterer Leser als beschädigt zurückweist, wäre mit SVG
        /// schlechter dran als ohne.</para>
        /// </summary>
        [Fact]
        public void DerBerichtAusProjekt1030TraegtAnJederBildstelleBeideTeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "bericht_1030.docx");
                new WordBerichtGenerator().Erzeuge(Berichtsdatenproben.Projektdaten1030(),
                                                   Berichtsdatenproben.VolleKonfiguration(), ziel);
                Assert.True(File.Exists(ziel), "Der Bericht wurde nicht geschrieben.");

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                MainDocumentPart main = doc.MainDocumentPart;

                List<A.Blip> blips = main.Document.Descendants<A.Blip>().ToList();
                Assert.True(blips.Count >= 7,
                    "Der Bericht aus 1030 führt nur " + blips.Count + " Bilder.");

                int mitSvg = 0;
                foreach (A.Blip blip in blips)
                {
                    ImagePart png = (ImagePart)main.GetPartById(blip.Embed.Value);
                    Assert.Equal("image/png", png.ContentType);

                    ASVG.SVGBlip svgBlip = blip.Descendants<ASVG.SVGBlip>().FirstOrDefault();
                    if (svgBlip == null) continue;
                    mitSvg++;

                    ImagePart svg = (ImagePart)main.GetPartById(svgBlip.Embed.Value);
                    Assert.Equal("image/svg+xml", svg.ContentType);
                    Assert.StartsWith("<svg", Encoding.UTF8.GetString(Inhalt(svg)),
                                      StringComparison.Ordinal);
                }

                Assert.True(mitSvg == blips.Count,
                    "Von " + blips.Count + " Bildstellen des Berichts 1030 tragen nur " +
                    mitSvg + " ein SVG.");

                // Je Bild genau zwei Teile: das PNG und das SVG.
                Assert.Equal(blips.Count, main.ImageParts.Count(p => p.ContentType == "image/png"));
                Assert.Equal(blips.Count, main.ImageParts.Count(p => p.ContentType == "image/svg+xml"));

                // Und derselbe Bericht ist in JEDER Office-Fassung gültig: Ein
                // Dokument, das ein älterer Leser als beschädigt zurückweist, wäre
                // mit SVG schlechter dran als ohne. Der Lauf steckt im selben Fall,
                // damit Projekt 1030 nur EINMAL simuliert wird.
                foreach (FileFormatVersions fassung in Fassungen)
                {
                    List<ValidationErrorInfo> fehler =
                        new OpenXmlValidator(fassung).Validate(doc).ToList();
                    Assert.True(fehler.Count == 0,
                        fassung + ": " + fehler.Count + " Fehler — " + string.Join(" | ",
                            fehler.Take(5).Select(f => f.Description + " @ " + f.Path?.XPath)));
                }
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Office 2007 bis 2021 — jede Fassung, die der Validator kennt (auch für die
        /// Messlatte der Berichtsvorlagen).</summary>
        internal static readonly FileFormatVersions[] Fassungen =
        {
            FileFormatVersions.Office2007, FileFormatVersions.Office2010,
            FileFormatVersions.Office2013, FileFormatVersions.Office2016,
            FileFormatVersions.Office2019, FileFormatVersions.Office2021
        };

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
                case "strombilanz": return ChartRenderer.StrombilanzMonateModell(z);
                case "kuchen": return ChartRenderer.KuchenModell("Wärmedeckung", Segmente());
                case "balken": return ChartRenderer.BalkenHorizontalModell(
                                          "Brennstoffeinsatz", "MWh/a", Balken());
                case "kapitalwert": return ChartRenderer.KapitalwertVerlaufModell(
                                          "Kumulierte Barwerte je Version", Barwerte(), null);
                // ETAPPE E6: das Dreierbild des Wortberichts (Verlauf mit drei Szenarien).
                case "kapitalwert_szenarien": return Dreierbild();
                // ETAPPE E6, Nachtrag E5b: das Spannenbild neben der Bandbreitentafel.
                case "kapitalwert_spanne": return ChartRenderer.KapitalwertSpanneModell(
                                          new List<ChartRenderer.Spannenbalken>
                                          {
                                              new ChartRenderer.Spannenbalken
                                              {
                                                  Name = "Variante A", Worst = -4000.0, Erwartet = 6000.0, Best = 11000.0
                                              }
                                          }, "Stamm", null);
                // ETAPPE E8a (U41): das Brückenbild von der Investition zur Kapitalwertdifferenz.
                case "kapitalwert_bruecke": return ChartRenderer.KapitalwertBrueckeModell(
                                          new List<ChartRenderer.Brueckenschritt>
                                          {
                                              new ChartRenderer.Brueckenschritt { Name = "Investition I₀", Wert = -40000.0 },
                                              new ChartRenderer.Brueckenschritt { Name = "Energiekosten", Wert = 55000.0 },
                                              new ChartRenderer.Brueckenschritt { Name = "Restwert am Ende", Wert = 3000.0 }
                                          }, null);
                // ETAPPE E8a (U42): das Zahlungsstrombild über der Mehrjahrestafel.
                case "zahlungsstrom": return ChartRenderer.ZahlungsstromModell(
                                          new List<ChartRenderer.Zahlungsstromreihe>
                                          {
                                              new ChartRenderer.Zahlungsstromreihe
                                              {
                                                  Schluessel = ChartRenderer.Zahlungsstromreihe.INVEST_ERSATZ,
                                                  Name = "Investition und Ersatz",
                                                  JeJahr = new[] { -40000.0, 0.0, -6000.0, 0.0 }
                                              },
                                              new ChartRenderer.Zahlungsstromreihe
                                              {
                                                  Schluessel = "ENERGIE", Name = "Energiekosten",
                                                  JeJahr = new[] { 0.0, -5000.0, -5100.0, -5200.0 }
                                              },
                                              new ChartRenderer.Zahlungsstromreihe
                                              {
                                                  Schluessel = "EINSPEISUNG", Name = "Einspeiseerlös",
                                                  JeJahr = new[] { 0.0, 9000.0, 9000.0, 9000.0 }
                                              }
                                          }, new[] { 2 }, null);
                default: throw new ArgumentOutOfRangeException(nameof(bild), bild, "unbekanntes Bild");
            }
        }

        private static List<ChartRenderer.Segment> Segmente() => new List<ChartRenderer.Segment>
        {
            new ChartRenderer.Segment("BHKW", 55.0, ChartRenderer.C_BHKW),
            new ChartRenderer.Segment("Spitzenkessel", 30.0, ChartRenderer.C_KESSEL),
            new ChartRenderer.Segment("Rest/ungedeckt", 15.0, ChartRenderer.C_REST)
        };

        private static List<ChartRenderer.Balken> Balken() => new List<ChartRenderer.Balken>
        {
            new ChartRenderer.Balken("Stamm", 1240.0, true),
            new ChartRenderer.Balken("Variante A", 980.0, false)
        };

        /// <summary>ETAPPE E6 — das Dreierbild aus einem synthetischen Sammelmodell: eine
        /// Variante in drei Szenarien, gegen den Stamm.</summary>
        private static Zeichenmodell Dreierbild()
        {
            var verlauf = new WirtschaftlichkeitVerlaufSzenarien { Jahre = 20 };
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                double f = s == WirtschaftlichkeitSzenario.WORST ? 0.9 : s == WirtschaftlichkeitSzenario.BEST ? 1.1 : 1.0;
                var d = new double[21];
                d[0] = -60000.0;
                for (int j = 1; j < d.Length; j++) d[j] = d[j - 1] + 7000.0 * f;
                var lauf = new WirtschaftlichkeitVerlauf { Jahre = 20, Szenario = s };
                lauf.Absolut.Add(new VerlaufSerie { IdProjekt = 1, Anzeige = "Stamm", IstStamm = true, Kumuliert = new double[21] });
                lauf.Absolut.Add(new VerlaufSerie { IdProjekt = 2, Anzeige = "Variante A", Kumuliert = d });
                lauf.Differenz.Add(new VerlaufSerie { IdProjekt = 2, Anzeige = "Variante A", Kumuliert = d });
                verlauf.Laeufe[s] = lauf;
            }
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            return ChartRenderer.KapitalwertSzenarienModell("Verlauf",
                ChartRenderer.VerlaufsReihenSzenarien(verlauf, texte), texte, null);
        }

        private static List<ChartRenderer.Reihe> Barwerte()
        {
            var stamm = new double[21];
            var variante = new double[21];
            for (int j = 0; j < stamm.Length; j++)
            {
                stamm[j] = -180000.0 + j * 14000.0;
                variante[j] = -240000.0 + j * 21000.0;
            }
            return new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Stamm", stamm, ChartRenderer.C_KESSEL),
                new ChartRenderer.Reihe("Variante A", variante, ChartRenderer.C_BHKW)
            };
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
            new WordBerichtGenerator().Erzeuge(Berichtsdatenproben.Gruppendaten(), Konfiguration(), ziel);
            Assert.True(File.Exists(ziel), "Der Bericht wurde nicht geschrieben.");
            return ziel;
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
