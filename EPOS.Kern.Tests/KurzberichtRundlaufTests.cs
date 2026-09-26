using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;
using ASVG = DocumentFormat.OpenXml.Office2019.Drawing.SVG;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Rundlauf des Kurzberichts</b> (Konzept Berichtsvorlagen 12, Zeile „Rundlauf“: „Kurzbericht mit 1030“; 6.3
    /// Nr. 2, 6.5, Anhang B.1; Etappe BV-E5): Beide Kurzberichte — deutsch und englisch, in ihrer Sprache — füllen das
    /// Referenzprojekt 1030 ohne Prüferfehler und ohne unbekannten Platzhalter; im Bericht bleibt kein <c>{{</c>, der
    /// Validator ist in jeder Fassung von Office 2007 bis 2021 grün, und jede Bildstelle trägt ein SVG mit PNG-Rückfall
    /// (<see cref="WordBerichtSvgWacheTests"/>). Die beiden Deckungsbilder stehen nebeneinander in halber
    /// Satzspiegelbreite — in Zielgröße gezeichnet (Stufe 2), nicht verkleinert.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KurzberichtRundlaufTests : IDisposable
    {
        private readonly ITestOutputHelper _ausgabe;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public KurzberichtRundlaufTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Breite eines der beiden Bildrahmen nebeneinander (7,8 cm) — wie im Werkzeug.</summary>
        private const long HALBE_BREITE = 2808000L;

        [Theory]
        [InlineData(BerichtsvorlageDateiWacheTests.KURZBERICHT, false)]
        [InlineData(BerichtsvorlageDateiWacheTests.KURZBERICHT_EN, true)]
        public void Der_Kurzbericht_fuellt_1030_ohne_Befund(string datei, bool englisch)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(datei);
            if (pfad == null || !File.Exists(pfad)) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = Sprache.Nummer;
            string ordner = Path.Combine(Path.GetTempPath(), "epos-kurzbericht-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                using var kultur = new Kulturvorrichtung(englisch ? "en-US" : "de-DE");
                Sprache.Nummer = englisch ? 1 : 0;
                byte[] vorlage = File.ReadAllBytes(pfad);
                BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();

                // Prüfer: kein Fehler, Sprache der Vorlage = Sprache des Laufs.
                Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, Pruefkontext.Aus(konfig, englisch, 1, datei));
                foreach (Pruefmeldung m in befund.Meldungen) _ausgabe.WriteLine(m.Stufe + ": " + m.Text);
                Assert.Empty(befund.Meldungen.Where(m => m.Stufe == Befundstufe.Fehler).Select(m => m.Text));
                Assert.Empty(befund.UnbekannteSchluessel);
                Assert.False(befund.SpracheAbweichend);
                Assert.Equal(englisch ? "en" : "de", befund.Sprache);
                Assert.Equal((int?)Vorlagenfeldkatalog.KATALOGFASSUNG, befund.Katalogfassung);
                // Der Kurzbericht führt Einzelwerte, Blöcke, Tabellen, Bilder — und nur den Anhang als Kapitel.
                Assert.Contains("tabelle.wirtschaft.szenarien", befund.Schluessel);
                Assert.Contains("stand.bild.deckung_waerme", befund.Schluessel);
                Assert.Contains("stand.kennzahl.eff.jaz", befund.Schluessel);
                Assert.Equal(new[] { "kapitel.anhang" }, befund.Schluessel.Where(s => s.StartsWith("kapitel.", StringComparison.Ordinal)));

                // In der anderen Sprache fragt die Vorprüfung zurück.
                Assert.True(Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, Pruefkontext.Aus(konfig, !englisch)).SpracheAbweichend);

                string ziel = Path.Combine(ordner, "kurzbericht.docx");
                Fuellergebnis ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(
                    BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_1030), konfig, vorlage,
                    new Erstellerangaben { Firma = "Firma 1", Version = "1.0" }, ziel);
                foreach (string w in ergebnis.Warnungen.Concat(ergebnis.Fehler)) _ausgabe.WriteLine("Lauf: " + w);
                Assert.Empty(ergebnis.Unbekannte.Select(u => u.Normalform + " @ " + u.Fundort));
                Assert.Empty(ergebnis.Fehler);
                Assert.Equal(9, ergebnis.EntfernteKommentare);

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                MainDocumentPart main = doc.MainDocumentPart;
                var teile = new List<OpenXmlElement> { main.Document.Body };
                teile.AddRange(main.HeaderParts.Select(h => (OpenXmlElement)h.Header));
                teile.AddRange(main.FooterParts.Select(f => (OpenXmlElement)f.Footer));
                foreach (OpenXmlElement teil in teile)
                    Assert.DoesNotContain("{{", teil.InnerText, StringComparison.Ordinal);
                Assert.Null(main.WordprocessingCommentsPart);
                Assert.Empty(main.Document.Body.Elements<Table>().Where(Tabellenmuster.IstMuster));

                List<string> fehler = BerichtsvorlageDateiWacheTests.Validatorfehler(doc);
                Assert.True(fehler.Count == 0, string.Join(" | ", fehler.Take(5)));

                // Jede Bildstelle: PNG-Rückfall und SVG.
                List<A.Blip> blips = main.Document.Body.Descendants<A.Blip>().ToList();
                Assert.True(blips.Count >= 2, blips.Count + " Bilder im Rumpf");
                foreach (A.Blip blip in blips)
                {
                    Assert.Equal("image/png", main.GetPartById(blip.Embed.Value).ContentType);
                    ASVG.SVGBlip svg = blip.Descendants<ASVG.SVGBlip>().FirstOrDefault();
                    Assert.NotNull(svg);
                    OpenXmlPart svgTeil = main.GetPartById(svg.Embed.Value);
                    Assert.Equal("image/svg+xml", svgTeil.ContentType);
                    using var leser = new StreamReader(svgTeil.GetStream(FileMode.Open, FileAccess.Read), Encoding.UTF8);
                    Assert.StartsWith("<svg", leser.ReadToEnd(), StringComparison.Ordinal);
                }

                // Die beiden Deckungsbilder je Stand nebeneinander, in halber Breite.
                List<DW.Inline> halb = main.Document.Body.Descendants<Table>()
                    .SelectMany(t => t.Descendants<DW.Inline>()).Where(i => i.Extent.Cx.Value == HALBE_BREITE).ToList();
                Assert.True(halb.Count >= 2, halb.Count + " Bilder in halber Breite");
                Assert.True(halb.Count % 2 == 0, halb.Count + " Bilder in halber Breite — erwartet je Stand zwei");
                _ausgabe.WriteLine(datei + ": " + ergebnis.Ersetzt + " ersetzt, " + blips.Count + " Bilder, " + halb.Count + " nebeneinander");
            }
            finally
            {
                Sprache.Nummer = vorher;
                try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }
    }
}
