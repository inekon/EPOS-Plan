using System;
using System.Globalization;
using System.Threading;
using SkiaSharp;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// BEFUND W11b‑B‑25 (Windows-Abnahme 09.09.2026): „Lastgang und Speicherung in
    /// einer Grafik." Das EINE Bild des Bestpunkts —
    /// <see cref="SpeicherOptimierungCtrl.Betriebsbild"/>.
    ///
    /// <para><b>Der Prüfstand.</b> 30 Tage in Stundenwerten: nachts 100 kW, tagsüber
    /// 300 kW, am 21. Tag um 12 Uhr eine Spitze von 700 kW. Verlustfrei, Preisreihe 0,
    /// Zins 0, N = 20 a. Der Bestpunkt der Rastersuche ist der 400-kWh-Speicher bei
    /// 1 C; er kappt die Spitze um 400 kW.</para>
    ///
    /// <para><b>Was geprüft wird.</b> Dass ein Bild ENTSTEHT (die abgelöste Maske
    /// zeigte für den Betrieb gar keines), dass es die WOCHE UM DIE JAHRESSPITZE zeigt
    /// und nicht deren Anfang, dass der Umschalter auf das ganze Jahr wirkt, und dass
    /// eine Reihenwahl das Bild nicht zu Fall bringt.</para>
    /// </summary>
    public sealed class SpeicherOptimierungBetriebsbildTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public SpeicherOptimierungBetriebsbildTests()
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

        /// <summary>30 Tage à 24 Stunden.</summary>
        private const int Stunden = 30 * 24;

        /// <summary>Die Spitzenstunde: 21. Tag, 12 Uhr.</summary>
        private const int SpitzenStunde = 20 * 24 + 12;

        /// <summary>Eine Woche in Stundenwerten.</summary>
        private const int Woche = 7 * 24;

        // =================================================================
        // Prüfstand
        // =================================================================

        private static double[] Lastgang()
        {
            double[] last = new double[Stunden];
            for (int i = 0; i < Stunden; i++)
            {
                int stunde = i % 24;
                last[i] = stunde >= 8 && stunde < 20 ? 300.0 : 100.0;
            }
            last[SpitzenStunde] = 700.0;
            return last;
        }

        private static SpeicherEingang Eingang()
        {
            double[] last = Lastgang();
            return SpeicherEingang.MitFixpreis(last, new double[last.Length], 0.0);
        }

        private static SpeicherParameter Basis() => new SpeicherParameter
        {
            CNomKwh = 400.0,
            PKw = 400.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 400.0,
            RoundTripWirkungsgrad = 1.0,
            DtH = 1.0,
            CCapEurProKwh = 300.0,
            CPowEurProKw = 100.0,
            IFixEur = 0.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.0
        };

        private static StromspeicherOptimierungVorbereitung Vorbereitung()
            => new StromspeicherOptimierungVorbereitung
            {
                Eingang = Eingang(),
                Basis = Basis(),
                Kontext = null
            };

        private static SpeicherOptimierungEingaben Suchraum(
            OptimiererStrategie strategie = OptimiererStrategie.Lastspitzenkappung)
            => new SpeicherOptimierungEingaben
            {
                CMinKwh = 200.0,
                CMaxKwh = 400.0,
                Stuetzstellen = 2,
                RMin = 1.0,
                RMax = 1.0,
                RSchritt = 1.0,
                Feinraster = false,
                Strategie = strategie,
                LeistungspreisEurProKwA =
                    strategie == OptimiererStrategie.Lastspitzenkappung ? 100.0 : 0.0
            };

        private static OptimiererErgebnis Roh(
            OptimiererStrategie strategie = OptimiererStrategie.Lastspitzenkappung)
        {
            StromspeicherOptimierungVorbereitung v = Vorbereitung();
            return StromspeicherSimCtrl.FuehreOptimierungAus(
                v, SpeicherOptimierungCtrl.Optionen(Suchraum(strategie)), null, CancellationToken.None);
        }

        private static (int Breite, int Hoehe) Mass(byte[] png)
        {
            using (var bild = SKBitmap.Decode(png)) return (bild.Width, bild.Height);
        }

        // =================================================================
        // Das Bild entsteht
        // =================================================================

        [Fact]
        public void Der_Lauf_Liefert_Das_Betriebsbild_Mit()
        {
            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Rechnen(
                Vorbereitung(), Suchraum(), null, CancellationToken.None);

            Assert.True(dto.Erfolg);
            Assert.NotNull(dto.BetriebBild);
            Assert.NotEmpty(dto.BetriebBild);
            Assert.Equal((1240, 560), Mass(dto.BetriebBild));

            // Der Titel nennt den gezeigten Ausschnitt.
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_BETRIEB_WOCHE,
                            dto.BetriebTitel, StringComparison.Ordinal);

            // Das rohe Raster kommt mit - sonst koennte die Huelle nicht nachzeichnen.
            Assert.NotNull(dto.Roh);
            Assert.Equal(400.0, dto.Roh.BestPunkt.CNomKwh, 6);
        }

        [Fact]
        public void Die_Vorgabe_Ist_Die_Woche_Um_Die_Jahresspitze()
        {
            SpeicherOptimierungBetriebsbild bild = SpeicherOptimierungCtrl.Betriebsbild(
                Vorbereitung(), Roh(), false, null, null);

            Assert.Equal(Woche, bild.Stuetzstellen);
            Assert.Equal(SpitzenStunde - Woche / 2, bild.VonIntervall);
            Assert.Equal(SpitzenStunde - Woche / 2 + Woche, bild.BisIntervall);

            // Die Spitze liegt IN der Woche, und zwar in ihrer Mitte.
            Assert.InRange(SpitzenStunde, bild.VonIntervall, bild.BisIntervall - 1);
        }

        [Fact]
        public void Der_Umschalter_Zeigt_Das_Ganze_Jahr()
        {
            SpeicherOptimierungBetriebsbild bild = SpeicherOptimierungCtrl.Betriebsbild(
                Vorbereitung(), Roh(), true, null, null);

            Assert.Equal(0, bild.VonIntervall);
            Assert.Equal(Stunden, bild.BisIntervall);
            Assert.Equal(Stunden, bild.Stuetzstellen);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_BETRIEB_JAHR,
                            bild.Titel, StringComparison.Ordinal);
            Assert.NotEmpty(bild.Png);
        }

        [Fact]
        public void Eine_Einzelne_Reihe_Bringt_Das_Bild_Nicht_Zu_Fall()
        {
            // Nur die Schwelle: eine waagerechte Linie, also eine Spanne von 0 - genau
            // der Fall, der ohne Wache durch null teilte.
            SpeicherOptimierungBetriebsbild bild = SpeicherOptimierungCtrl.Betriebsbild(
                Vorbereitung(), Roh(), false,
                new[] { SpeicherOptimierungCtrl.REIHE_SCHWELLE }, null);

            Assert.NotEmpty(bild.Png);
            Assert.Equal((1240, 560), Mass(bild.Png));
        }

        [Fact]
        public void Ohne_Erzeugung_Zeichnet_Auch_Die_Dauernutzung_Ein_Bild()
        {
            SpeicherOptimierungBetriebsbild bild = SpeicherOptimierungCtrl.Betriebsbild(
                Vorbereitung(), Roh(OptimiererStrategie.Dauernutzung), false, null, null);

            Assert.NotEmpty(bild.Png);
            Assert.Equal(Woche, bild.Stuetzstellen);
        }

        [Fact]
        public void Eine_Leere_Reihenwahl_Zeigt_Den_Leerhinweis()
        {
            // Hausregel der Ergebnisseite (Doku_Simulationsergebnis_Darstellung.md, 5):
            // null heisst "alle", eine LEERE Liste heisst "keine" - kein Rueckfall.
            SpeicherOptimierungBetriebsbild leer = SpeicherOptimierungCtrl.Betriebsbild(
                Vorbereitung(), Roh(), false, new string[0], null);

            SpeicherOptimierungBetriebsbild alle = SpeicherOptimierungCtrl.Betriebsbild(
                Vorbereitung(), Roh(), false, null, null);

            Assert.NotEmpty(leer.Png);
            Assert.Equal((1240, 560), Mass(leer.Png));
            Assert.NotEqual(alle.Png, leer.Png);
        }

        [Fact]
        public void Ohne_Lauf_Gibt_Es_Kein_Bild()
        {
            Assert.Null(SpeicherOptimierungCtrl.Betriebsbild(null, Roh(), false, null, null).Png);
            Assert.Null(SpeicherOptimierungCtrl.Betriebsbild(Vorbereitung(), null, false, null, null).Png);
        }

        // =================================================================
        // Die Kappung erscheint im Bild
        // =================================================================

        [Fact]
        public void Das_Bild_Der_Kappung_Unterscheidet_Sich_Vom_Bild_Ohne_Schwelle()
        {
            OptimiererErgebnis roh = Roh();

            byte[] mitSchwelle = SpeicherOptimierungCtrl.Betriebsbild(
                Vorbereitung(), roh, false, null, null).Png;

            byte[] ohneSchwelle = SpeicherOptimierungCtrl.Betriebsbild(
                Vorbereitung(), roh, false,
                new[]
                {
                    SpeicherOptimierungCtrl.REIHE_OHNE,
                    SpeicherOptimierungCtrl.REIHE_MIT,
                    SpeicherOptimierungCtrl.REIHE_SPEICHER
                }, null).Png;

            Assert.NotEmpty(mitSchwelle);
            Assert.NotEmpty(ohneSchwelle);
            Assert.NotEqual(mitSchwelle, ohneSchwelle);
        }

        [Fact]
        public void Zweimal_Dasselbe_Bild_Ist_Bitgleich()
        {
            OptimiererErgebnis roh = Roh();

            byte[] a = SpeicherOptimierungCtrl.Betriebsbild(Vorbereitung(), roh, false, null, null).Png;
            byte[] b = SpeicherOptimierungCtrl.Betriebsbild(Vorbereitung(), roh, false, null, null).Png;

            Assert.Equal(a, b);
        }
    }
}
