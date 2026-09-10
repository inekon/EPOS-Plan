using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// BEFUND W11b‑B‑25 (Windows-Abnahme 09.09.2026): „Auslegung scheint nicht zu
    /// funktionieren." Die HINWEISE der Auslegungsoptimierung — sie sagen, warum ein
    /// Raster aussieht, wie es aussieht.
    ///
    /// <para><b>Die Lage im Befund.</b> Projekt 1050 rechnete 120 Rasterpunkte, alle
    /// mit ΔJ = 0. Die Karte war einfarbig, und darüber standen zwei Warnungen:
    /// „Optimum am Rand — Suchbereich erweitern" und „c_pow ist 0". Beide waren
    /// irreführend: Ein Optimum gab es gar nicht, und die c_pow-Warnung behauptete die
    /// obere C-Raten-Grenze, während das angezeigte Optimum an der UNTEREN lag. Der
    /// eigentliche Grund — keine Erzeugung im Projekt und Modulkosten 0 — stand
    /// nirgends.</para>
    ///
    /// <para><b>Was hier geprüft wird.</b> (a) Ohne Erzeugung meldet sich die Anzeige
    /// vor dem Lauf, (b) c_cap = 0 ebenso, (c) ein Raster ohne Unterschiede sagt das
    /// und unterdrückt dabei die Randwarnung, (d) die zwei richtiggestellten Texte
    /// nennen, was das Programm wirklich tut.</para>
    /// </summary>
    public sealed class SpeicherOptimierungHinweiseTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public SpeicherOptimierungHinweiseTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        // =================================================================
        // Prüfstand — die Lage aus Projekt 1050
        // =================================================================

        /// <summary>Vier Viertelstunden Last, KEINE Erzeugung, Preis 20 ct/kWh.</summary>
        private static SpeicherEingang OhneErzeugung()
        {
            double[] last = { 100.0, 100.0, 140.0, 100.0 };
            double[] preis = { 20.0, 20.0, 20.0, 20.0 };
            return new SpeicherEingang(last, new double[4], preis);
        }

        /// <summary>Dieselbe Reihe MIT PV-Überschuss.</summary>
        private static SpeicherEingang MitErzeugung()
        {
            double[] last = { 0.0, 0.0, 40.0, 40.0 };
            double[] pv = { 40.0, 40.0, 0.0, 0.0 };
            double[] preis = { 20.0, 20.0, 20.0, 20.0 };
            return new SpeicherEingang(last, pv, preis);
        }

        /// <summary>Auslegung mit Modulkosten 0 — genau der Stand des Geräts 1017062.</summary>
        private static SpeicherParameter OhneKosten() => new SpeicherParameter
        {
            CNomKwh = 129.0,
            PKw = 100.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 129.0,
            RoundTripWirkungsgrad = 1.0,
            DtH = 0.25,
            CCapEurProKwh = 0.0,
            CPowEurProKw = 0.0,
            IFixEur = 0.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.0
        };

        private static SpeicherParameter MitKosten()
            => OhneKosten() with { CCapEurProKwh = 300.0, CPowEurProKw = 100.0 };

        private static StromspeicherOptimierungVorbereitung Vorbereitung(
            SpeicherEingang eingang, SpeicherParameter basis)
            => new StromspeicherOptimierungVorbereitung
            {
                Eingang = eingang,
                Basis = basis,
                Kontext = null
            };

        private static SpeicherOptimierungEingaben Suchraum(
            OptimiererStrategie strategie = OptimiererStrategie.Dauernutzung)
            => new SpeicherOptimierungEingaben
            {
                CMinKwh = 50.0,
                CMaxKwh = 300.0,
                Stuetzstellen = 3,
                RMin = 0.5,
                RMax = 1.5,
                RSchritt = 0.5,
                Feinraster = false,
                Strategie = strategie
            };

        // =================================================================
        // (a) Ohne Erzeugung bewerten Dauer- und Nachtnutzung nichts
        // =================================================================

        [Fact]
        public void Ohne_Erzeugung_Meldet_Sich_Die_Anzeige_Vor_Dem_Lauf()
        {
            var hinweise = SpeicherOptimierungCtrl.Vorhinweise(
                Vorbereitung(OhneErzeugung(), MitKosten()), Suchraum());

            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_KEINE_ERZEUGUNG, hinweise);
        }

        [Fact]
        public void Mit_Erzeugung_Bleibt_Der_Hinweis_Weg()
        {
            var hinweise = SpeicherOptimierungCtrl.Vorhinweise(
                Vorbereitung(MitErzeugung(), MitKosten()), Suchraum());

            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_KEINE_ERZEUGUNG, hinweise);
        }

        [Fact]
        public void Die_Lastspitzenkappung_Braucht_Keine_Erzeugung()
        {
            var hinweise = SpeicherOptimierungCtrl.Vorhinweise(
                Vorbereitung(OhneErzeugung(), MitKosten()),
                Suchraum(OptimiererStrategie.Lastspitzenkappung));

            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_KEINE_ERZEUGUNG, hinweise);
        }

        // =================================================================
        // (b) Modulkosten 0
        // =================================================================

        [Fact]
        public void Investitionskosten_Null_Nennen_Ihren_Pflegeort()
        {
            var hinweise = SpeicherOptimierungCtrl.Vorhinweise(
                Vorbereitung(MitErzeugung(), OhneKosten()), Suchraum());

            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_CCAP_NULL, hinweise);
            Assert.Contains("Modulkosten",
                            WindowsFormsApplication1.MyResource.Resource.OPT_WARN_CCAP_NULL,
                            StringComparison.Ordinal);
        }

        [Fact]
        public void Mit_Modulkosten_Bleibt_Der_Hinweis_Weg()
        {
            var hinweise = SpeicherOptimierungCtrl.Vorhinweise(
                Vorbereitung(MitErzeugung(), MitKosten()), Suchraum());

            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_CCAP_NULL, hinweise);
        }

        [Fact]
        public void Ohne_Vorbereitung_Gibt_Es_Keine_Vorhinweise()
        {
            Assert.Empty(SpeicherOptimierungCtrl.Vorhinweise(null, Suchraum()));
        }

        // =================================================================
        // (c) Ein Raster ohne Unterschiede
        // =================================================================

        [Fact]
        public void Ein_Raster_Ohne_Unterschiede_Sagt_Es_Und_Warnt_Nicht_Vor_Dem_Rand()
        {
            // Genau die Lage des Befunds: keine Erzeugung, keine Kosten - jeder
            // Rasterpunkt traegt dJ = 0.
            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Rechnen(
                Vorbereitung(OhneErzeugung(), OhneKosten()), Suchraum(), null, CancellationToken.None);

            Assert.True(dto.Erfolg);
            Assert.Equal(0.0, dto.ZielfunktionEur, 9);

            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_ALLE_GLEICH, dto.Hinweise);
            Assert.False(dto.Randlage);
            Assert.DoesNotContain(dto.Hinweise,
                h => h.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_RAND_C_UNTEN,
                                StringComparison.Ordinal));

            // Und der GRUND steht mit da - beide Vorhinweise, ganz vorn.
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_KEINE_ERZEUGUNG, dto.Hinweise[0]);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_CCAP_NULL, dto.Hinweise[1]);
        }

        [Fact]
        public void Ein_Raster_Mit_Unterschieden_Bekommt_Den_Hinweis_Nicht()
        {
            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Rechnen(
                Vorbereitung(MitErzeugung(), MitKosten()), Suchraum(), null, CancellationToken.None);

            Assert.True(dto.Erfolg);
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_ALLE_GLEICH, dto.Hinweise);
        }

        // =================================================================
        // (d) Die richtiggestellten Texte
        // =================================================================

        [Fact]
        public void Der_Suchraumhinweis_Nennt_Die_Tatsaechliche_Vorbelegungsregel()
        {
            string text = WindowsFormsApplication1.MyResource.Resource.OPT_HINWEIS_SUCHRAUM;

            Assert.Contains("500", text, StringComparison.Ordinal);
            Assert.Contains("0,25", text, StringComparison.Ordinal);
            Assert.Contains("2,5", text, StringComparison.Ordinal);
            Assert.Contains("Kapazität", text, StringComparison.Ordinal);
        }

        [Fact]
        public void Die_CPow_Warnung_Behauptet_Keine_Obere_Kante_Mehr()
        {
            string text = WindowsFormsApplication1.MyResource.Resource.OPT_WARN_CPOW;

            Assert.Contains("kostenneutral", text, StringComparison.Ordinal);
            Assert.Contains("KLEINSTE", text, StringComparison.Ordinal);
            Assert.DoesNotContain("obere C-Raten-Grenze", text, StringComparison.Ordinal);
        }
    }
}
