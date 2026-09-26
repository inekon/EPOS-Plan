using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die mitgelieferten Vorlagen tragen ihre Sprache in den Lauf</b> (Konzept Berichtsvorlagen 4.9, BV-Q7 b; Etappe
    /// BV-E9): Kurzbericht und ausführliche Vorlage je Sprache tragen <c>EPOS.Sprache</c>, die Standardvorlage keine. Bei
    /// der anderen Oberflächensprache hält die Prüfung nicht an (ein Hinweis), und der Lauf in der Klammer der Vorlage
    /// (<see cref="BerichtTexte.ImLauf"/>) ergibt DENSELBEN Bericht wie ein Lauf mit der Oberfläche in der Sprache der
    /// Vorlage — Rumpf, Kopf- und Fußzeilen, Wort für Wort, Zahl für Zahl, für das Referenzprojekt 1030.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtsspracheVorlagenTests : IDisposable
    {
        private readonly ITestOutputHelper _ausgabe;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-sprache");

        public BerichtsspracheVorlagenTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        /// <summary>Die Standardvorlage ist sprachneutral: kein <c>EPOS.Sprache</c>, der Lauf nimmt die Oberflächensprache.</summary>
        [Fact]
        public void Die_Standardvorlage_traegt_keine_Sprache()
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            if (pfad == null || !File.Exists(pfad)) return;
            Assert.Null(Berichtssprache.AusPaket(File.ReadAllBytes(pfad)));
        }

        [Theory]
        [InlineData(BerichtsvorlageDateiWacheTests.KURZBERICHT_EN, true)]
        [InlineData(BerichtsvorlageDateiWacheTests.AUSFUEHRLICH_EN, true)]
        [InlineData(BerichtsvorlageDateiWacheTests.KURZBERICHT, false)]
        [InlineData(BerichtsvorlageDateiWacheTests.AUSFUEHRLICH, false)]
        public void Die_Vorlage_bestimmt_die_Sprache_auch_bei_der_anderen_Oberflaeche(string datei, bool englisch)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(datei);
            if (pfad == null || !File.Exists(pfad)) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            byte[] vorlage = File.ReadAllBytes(pfad);
            Assert.Equal(englisch, Berichtssprache.AusPaket(vorlage));
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            int vorher = Sprache.Nummer;
            try
            {
                // Die Oberfläche in der ANDEREN Sprache: Hinweis statt Anhalten, der Lauf in der Sprache der Vorlage.
                List<string> ausVorlage;
                using (new Kulturvorrichtung(englisch ? "de-DE" : "en-US"))
                {
                    Sprache.Nummer = englisch ? 0 : 1;
                    Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, Pruefkontext.Aus(konfig, !englisch, 1, datei));
                    Assert.True(befund.SpracheAbweichend);
                    Assert.False(befund.HatFehler);
                    Assert.Equal(Befundstufe.Hinweis, Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_SPRACHE")).Stufe);

                    Fuellergebnis ergebnis;
                    using (BerichtTexte.ImLauf(englisch))
                        ergebnis = Fuelle(vorlage, konfig, "aus_vorlage.docx");
                    Assert.Equal(englisch, ergebnis.Englisch);
                    Assert.Equal(!englisch, BerichtTexte.Englisch);
                    ausVorlage = Texte(ergebnis.Zieldatei);
                }

                // Zum Vergleich: die Oberfläche in der Sprache der Vorlage, ohne Klammer.
                List<string> ausOberflaeche;
                using (new Kulturvorrichtung(englisch ? "en-US" : "de-DE"))
                {
                    Sprache.Nummer = englisch ? 1 : 0;
                    Fuellergebnis ergebnis = Fuelle(vorlage, konfig, "aus_oberflaeche.docx");
                    Assert.Equal(englisch, ergebnis.Englisch);
                    ausOberflaeche = Texte(ergebnis.Zieldatei);
                }

                Assert.True(ausOberflaeche.Count > 20, "zu wenig Text: " + ausOberflaeche.Count);
                for (int i = 0; i < Math.Min(ausVorlage.Count, ausOberflaeche.Count); i++)
                    if (ausVorlage[i] != ausOberflaeche[i])
                        _ausgabe.WriteLine("Absatz " + i + ":\n  Vorlage:    " + ausVorlage[i] + "\n  Oberfläche: " + ausOberflaeche[i]);
                Assert.Equal(ausOberflaeche, ausVorlage);
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        /// <summary>Sammeln und Füllen — beides in der Sprache, die gerade gilt.</summary>
        private Fuellergebnis Fuelle(byte[] vorlage, BerichtsKonfiguration konfig, string datei)
        {
            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_1030);
            return new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, vorlage,
                new Erstellerangaben { Firma = "Firma 1", Version = "1.0" }, Path.Combine(_ordner, datei));
        }

        /// <summary>Die Absatztexte aus Rumpf, Kopf- und Fußzeilen.</summary>
        private static List<string> Texte(string pfad)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            MainDocumentPart main = doc.MainDocumentPart;
            var teile = new List<OpenXmlElement> { main.Document.Body };
            teile.AddRange(main.HeaderParts.Select(h => (OpenXmlElement)h.Header));
            teile.AddRange(main.FooterParts.Select(f => (OpenXmlElement)f.Footer));
            return teile.SelectMany(t => t.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                        .Select(p => p.InnerText).ToList();
        }
    }
}
