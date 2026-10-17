using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// K6d (Konzept Kältebedarf 4.3, Frage 1 des Plans K6, Katalog v23): die sechs Kennzahlen des Kältebedarfs aus Profilen
    /// und der dezentralen (Split-)Deckung — ohne Profil- bzw. Split-Zeile <c>null</c> (keine Zeile, kein „—“), mit ihr die
    /// Werte der Ergebnisspalten und die zwei Deckungszahlen nach <see cref="KaelteDeckungsgrad"/>; der Bericht nimmt sie
    /// aus dem Katalog.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaelteDezentralKennzahlenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly string[] NEUE =
        {
            KennzahlenKatalog.SCHLUESSEL_KAELTE_PROFIL, KennzahlenKatalog.SCHLUESSEL_KAELTE_DEZENTRAL,
            KennzahlenKatalog.SCHLUESSEL_KAELTE_DEZENTRAL_STROM, KennzahlenKatalog.SCHLUESSEL_KAELTE_DEZENTRAL_EER,
            KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_GESAMT, KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_KAELTEFOLGE,
        };

        /// <summary>Eine Variante mit 100 MWh/a Kältebedarf; die Wärmepumpe deckt <paramref name="wpProzent"/> % des Kühlkanals.</summary>
        private static VariantenDaten Variante(double? dezentral, double? profil, double? wpProzent)
        {
            var erg = new ErgebnisModel();
            erg.Energiebedarf = new ErgebnisEnergiebedarfModel
            {
                Waermebedarf_Gesamt = 100.0,
                Waermelast_Max = 50.0,
                Waermebedarf_Kanal = new[] { 100.0, 0.0, 0.0, 100.0 },
                Kaeltebedarf_Gesamt = 100.0,
                Kaeltelast_Max = 40.0,
                Kaelterestbedarf = 100.0 - (dezentral ?? 0.0) - (wpProzent ?? 0.0),
                KaeltebedarfProfilMwh = profil,
                KaeltebedarfDezentralMwh = dezentral,
                StromDezentralMwh = dezentral.HasValue ? dezentral / 4.0 : null,
                JahresEerDezentral = dezentral.HasValue ? 4.0 : null,
            };
            if (wpProzent.HasValue)
            {
                erg.Waermepumpe = new ErgebnisWaermepumpeModel
                {
                    Kaelteproduktion_WP = wpProzent.Value,
                    Stromverbrauch_Kuehlung = wpProzent.Value / 5.0,
                    Deckung_Kanal = new[] { 0.0, 0.0, 0.0, wpProzent.Value },
                };
            }
            var v = new VariantenDaten { IstStamm = true, Projektname = "Probe", Ergebnis = erg };
            KennzahlenKatalog.Berechne(v);
            return v;
        }

        [Fact]
        public void Die_sechs_Schluessel_stehen_in_der_Gruppe_Kaelte_mit_Fassung_23()
        {
            foreach (string s in NEUE)
            {
                Kennzahl k = KennzahlenKatalog.Alle().Single(x => x.Schluessel == s);
                Assert.Equal(KennzahlenKatalog.GR_KAELTE, k.Gruppe);
                Assert.Equal(23, k.Seit);
                Assert.Equal(23, Vorlagenfeldkatalog.SeitDerKennzahl(k));
                Assert.NotNull(Vorlagenfeldkatalog.Finde("stamm.kennzahl." + s));
                Assert.NotNull(Vorlagenfeldkatalog.Finde("stand.kennzahl." + s));
            }
            Assert.Equal("%", KennzahlenKatalog.Alle().Single(x => x.Schluessel == KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_GESAMT).Einheit);
            Assert.Equal("–", KennzahlenKatalog.Alle().Single(x => x.Schluessel == KennzahlenKatalog.SCHLUESSEL_KAELTE_DEZENTRAL_EER).Einheit);
        }

        [Fact]
        public void Ohne_Split_und_Profilzeile_sind_alle_sechs_leer()
        {
            VariantenDaten v = Variante(null, null, 60.0);
            foreach (string s in NEUE) Assert.Null(v.Kennzahlen[s]);
            // Der Deckungsgrad des Kühlkanals bleibt, wie er war.
            Assert.Equal(60.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD].Value, 9);
        }

        [Fact]
        public void Mit_Split_gelten_Hauptzahl_und_Kaeltefolge_nach_KaelteDeckungsgrad()
        {
            VariantenDaten v = Variante(20.0, 30.0, 60.0);
            Assert.Equal(30.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_PROFIL]);
            Assert.Equal(20.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DEZENTRAL]);
            Assert.Equal(5.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DEZENTRAL_STROM]);
            Assert.Equal(4.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DEZENTRAL_EER]);
            // (60 + 20) / 100 und 60 / (100 − 20).
            Assert.Equal(80.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_GESAMT].Value, 9);
            Assert.Equal(75.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_KAELTEFOLGE].Value, 9);
            Assert.Equal(KaelteDeckungsgrad.Hauptzahl(60.0, 20.0, 100.0), v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_GESAMT]);
            Assert.Equal(KaelteDeckungsgrad.DavonKaeltefolge(60.0, 20.0, 100.0), v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_KAELTEFOLGE]);
        }

        [Fact]
        public void Mit_Split_ohne_Kaelteerzeuger_steht_die_Hauptzahl_ohne_Kaeltefolge()
        {
            VariantenDaten v = Variante(20.0, null, null);
            Assert.Null(v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_PROFIL]);
            Assert.Equal(20.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_GESAMT].Value, 9);
            Assert.Null(v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD_KAELTEFOLGE]);
        }

        [Fact]
        public void Der_Bericht_nimmt_die_Deckungszahlen_aus_dem_Katalog()
        {
            string mit = Schreibe(Variante(20.0, 30.0, 60.0));
            Assert.Contains("Deckungsgrad Kälte", mit);
            Assert.Contains("80,0 %", mit);
            Assert.Contains("davon Kältefolge", mit);
            Assert.Contains("75,0 %", mit);
            Assert.DoesNotContain("Deckungsgrad Kühlung", mit);

            string ohne = Schreibe(Variante(null, null, 60.0));
            Assert.DoesNotContain("Deckungsgrad Kälte", ohne);
            Assert.DoesNotContain("davon Kältefolge", ohne);
            Assert.Contains("Deckungsgrad Kühlung", ohne);
        }

        [Fact]
        public void Der_Variantenvergleich_fuehrt_die_Split_Zeilen_nur_mit_Wert()
        {
            var ohne = new BerichtsDaten { Stammprojektname = "Probe" };
            ohne.Varianten.Add(Variante(null, null, 60.0));
            string[] zeilenOhne = Berichtstabellen.Vergleichsgruppe(ohne, KennzahlenKatalog.GR_KAELTE, false,
                System.Globalization.CultureInfo.CurrentCulture).Zeilen.Select(z => z.Zellen[0].Text).ToArray();
            Assert.DoesNotContain(zeilenOhne, z => z.Contains("Split") || z.Contains("Profilen") || z.Contains("Kältefolge"));

            var mit = new BerichtsDaten { Stammprojektname = "Probe" };
            mit.Varianten.Add(Variante(20.0, 30.0, 60.0));
            string[] zeilenMit = Berichtstabellen.Vergleichsgruppe(mit, KennzahlenKatalog.GR_KAELTE, false,
                System.Globalization.CultureInfo.CurrentCulture).Zeilen.Select(z => z.Zellen[0].Text).ToArray();
            foreach (string s in NEUE)
            {
                string label = KennzahlenKatalog.Alle().Single(x => x.Schluessel == s).LabelDe;
                Assert.Contains(zeilenMit, z => z.StartsWith(label, StringComparison.Ordinal));
            }
        }

        private static string Schreibe(VariantenDaten v)
        {
            var daten = new BerichtsDaten { Stammprojektname = "Probe" };
            daten.Varianten.Add(v);
            using var ms = new System.IO.MemoryStream();
            using DocumentFormat.OpenXml.Packaging.WordprocessingDocument doc =
                DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            DocumentFormat.OpenXml.Packaging.MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            new ProjektbeschreibungBaustein().SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
        }
    }
}
