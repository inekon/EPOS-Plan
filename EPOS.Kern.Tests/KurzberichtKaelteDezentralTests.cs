using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// K6d-N (Katalog v23): Der Kühlsatz des Kurzberichts hängt am Schalter <c>hat.kaelte.dezentral</c>. Ohne Split-Zeile
    /// steht der Satz der Anlage mit <c>kaelte.deckungsgrad</c> — derselbe Satz wie in der Vorlage vor dem Schalter —, mit
    /// ihr die Hauptzahl <c>kaelte.deckungsgrad_gesamt</c> samt „davon Kältefolge“ (<c>kaelte.deckungsgrad_kaeltefolge</c>),
    /// je in der Sprache der Vorlage; ohne Kälteerzeuger (<c>hat.kaelte.kaeltefolge</c> falsch) nennt er allein den Anteil der
    /// Split-Geräte, ohne Leerwert. Die jeweils anderen Sätze entfallen samt ihren Blockmarken.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KurzberichtKaelteDezentralTests : IDisposable
    {
        private readonly ITestOutputHelper _ausgabe;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public KurzberichtKaelteDezentralTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose() => _kultur.Dispose();

        /// <summary>
        /// Ein Stamm mit 100 MWh/a Kältebedarf; mit <paramref name="erzeuger"/> deckt die Wärmepumpe 60 MWh/a über die
        /// Kältefolge, mit <paramref name="dezentral"/> decken Split-Geräte 20 MWh/a daneben — Hauptzahl 80 %, davon
        /// Kältefolge 75 %; ohne Kälteerzeuger Hauptzahl 20 % und keine Kältefolge.
        /// </summary>
        private static BerichtsDaten Daten(bool dezentral, bool erzeuger = true)
        {
            double? split = dezentral ? 20.0 : null;
            double wp = erzeuger ? 60.0 : 0.0;
            var erg = new ErgebnisModel();
            erg.Energiebedarf = new ErgebnisEnergiebedarfModel
            {
                Waermebedarf_Gesamt = 100.0,
                Waermelast_Max = 50.0,
                Waermebedarf_Kanal = new[] { 100.0, 0.0, 0.0, 100.0 },
                Kaeltebedarf_Gesamt = 100.0,
                Kaeltelast_Max = 40.0,
                Kaelterestbedarf = 100.0 - (split ?? 0.0) - wp,
                KaeltebedarfDezentralMwh = split,
                StromDezentralMwh = split / 4.0,
                JahresEerDezentral = dezentral ? 4.0 : null,
            };
            if (erzeuger)
            {
                erg.Waermepumpe = new ErgebnisWaermepumpeModel
                {
                    Kaelteproduktion_WP = wp,
                    Stromverbrauch_Kuehlung = wp / 5.0,
                    Deckung_Kanal = new[] { 0.0, 0.0, 0.0, wp },
                };
            }
            var v = new VariantenDaten { IstStamm = true, Projektname = "Probe", Ergebnis = erg };
            KennzahlenKatalog.Berechne(v);
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(v);
            return d;
        }

        [Fact]
        public void Der_Schalter_steht_mit_Fassung_23_und_folgt_der_Split_Zeile()
        {
            Vorlagenfeld f = Vorlagenfeldkatalog.Finde("hat.kaelte.dezentral");
            Assert.NotNull(f);
            Assert.Equal(Vorlagenfeldart.Schalter, f.Art);
            Assert.Equal(Vorlagenfeldkontext.Gruppe, f.Kontext);
            Assert.Equal(23, f.Seit);
            Assert.Null(KennzahlenKatalog.DeckungsgradKaelteGesamt(Daten(false).Varianten[0]));
            Assert.Equal(80.0, KennzahlenKatalog.DeckungsgradKaelteGesamt(Daten(true).Varianten[0]).Value, 9);
            Assert.Equal(75.0, KennzahlenKatalog.DeckungsgradKaelteKaeltefolge(Daten(true).Varianten[0]).Value, 9);

            Vorlagenfeld folge = Vorlagenfeldkatalog.Finde("hat.kaelte.kaeltefolge");
            Assert.NotNull(folge);
            Assert.Equal(Vorlagenfeldart.Schalter, folge.Art);
            Assert.Equal(Vorlagenfeldkontext.Gruppe, folge.Kontext);
            Assert.Equal(23, folge.Seit);
            Assert.Equal(20.0, KennzahlenKatalog.DeckungsgradKaelteGesamt(Daten(true, false).Varianten[0]).Value, 9);
            Assert.Null(KennzahlenKatalog.DeckungsgradKaelteKaeltefolge(Daten(true, false).Varianten[0]));
        }

        [Theory]
        [InlineData(BerichtsvorlageDateiWacheTests.KURZBERICHT, false)]
        [InlineData(BerichtsvorlageDateiWacheTests.KURZBERICHT_EN, true)]
        public void Der_Kuehlsatz_nennt_mit_Split_die_Hauptzahl_samt_Kaeltefolge(string datei, bool englisch)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(datei);
            if (pfad == null || !File.Exists(pfad)) return;

            int vorher = Sprache.Nummer;
            string ordner = Path.Combine(Path.GetTempPath(), "epos-kurzbericht-split-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                using var kultur = new Kulturvorrichtung(englisch ? "en-US" : "de-DE");
                Sprache.Nummer = englisch ? 1 : 0;
                byte[] vorlage = File.ReadAllBytes(pfad);
                BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
                string anfang = englisch ? "The cooling demand is" : "Der Kältebedarf beträgt";

                List<string> ohne = Fuellen(Daten(false), konfig, vorlage, Path.Combine(ordner, "ohne.docx"));
                List<string> mit = Fuellen(Daten(true), konfig, vorlage, Path.Combine(ordner, "mit.docx"));
                List<string> allein = Fuellen(Daten(true, false), konfig, vorlage, Path.Combine(ordner, "allein.docx"));
                string satzOhne = Assert.Single(ohne, a => a.StartsWith(anfang, StringComparison.Ordinal));
                string satzMit = Assert.Single(mit, a => a.StartsWith(anfang, StringComparison.Ordinal));
                string satzAllein = Assert.Single(allein, a => a.StartsWith(anfang, StringComparison.Ordinal));
                _ausgabe.WriteLine(satzOhne);
                _ausgabe.WriteLine(satzMit);
                _ausgabe.WriteLine(satzAllein);

                Assert.Equal(englisch
                        ? "The cooling demand is 100.0 MWh/a; the system covers 60.0 % of it."
                        : "Der Kältebedarf beträgt 100,0 MWh/a; davon deckt die Anlage 60,0 %.",
                    satzOhne);
                Assert.Equal(englisch
                        ? "The cooling demand is 100.0 MWh/a; the system and the decentralised split units cover 80.0 % of it, "
                          + "of which the cooling sequence covers 75.0 % of the centrally supplied cooling demand."
                        : "Der Kältebedarf beträgt 100,0 MWh/a; davon decken die Anlage und die dezentralen Split-Geräte 80,0 %, "
                          + "davon die Kältefolge 75,0 % des zentral zu deckenden Kältebedarfs.",
                    satzMit);
                // Split ohne Kälteerzeuger: allein der Anteil der Split-Geräte, kein Leerwert für „davon Kältefolge“.
                Assert.Equal(englisch
                        ? "The cooling demand is 100.0 MWh/a; the decentralised split units cover 20.0 % of it."
                        : "Der Kältebedarf beträgt 100,0 MWh/a; davon decken die dezentralen Split-Geräte 20,0 %.",
                    satzAllein);

                // Bis auf den Kühlsatz gleichen sich beide Berichte Absatz für Absatz; keine Blockmarke bleibt stehen.
                Assert.Equal(ohne.Count, mit.Count);
                Assert.Equal(1, ohne.Zip(mit, (a, b) => a == b ? 0 : 1).Sum());
                foreach (string a in ohne.Concat(mit).Concat(allein)) Assert.DoesNotContain("{{", a, StringComparison.Ordinal);
            }
            finally
            {
                Sprache.Nummer = vorher;
                try { Directory.Delete(ordner, true); } catch { /* Aufräumen nach bestem Bemühen */ }
            }
        }

        private static List<string> Fuellen(BerichtsDaten daten, BerichtsKonfiguration konfig, byte[] vorlage, string ziel)
        {
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, vorlage,
                new Erstellerangaben { Firma = "Firma 1", Version = "1.0" }, ziel);
            Assert.Empty(e.Fehler);
            Assert.Empty(e.Unbekannte.Select(u => u.Normalform));
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            return doc.MainDocumentPart.Document.Body.Descendants<Paragraph>().Select(p => p.InnerText).ToList();
        }
    }
}
