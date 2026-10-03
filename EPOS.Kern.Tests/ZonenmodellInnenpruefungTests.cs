using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die innere Lastumkehr eines Abschnitts</b> (Rechenweg RP2a): Messung und allgemeine Innenprüfung.
    /// Grundlage ist die leichte Zone des Rechenbefunds RB-Z4 (<see cref="ZonenkopplungAbschnittsregelTests"/>):
    /// leichte Außenbauteile (Zeitkonstante rund 70 s), schwere Innenbauteile. Die schnelle Mode kehrt die
    /// Heizlast binnen Minuten um, die langsame holt sie bis zum Stundenende zurück.
    /// </summary>
    public class ZonenmodellInnenpruefungTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenmodellInnenpruefungTests(ITestOutputHelper aus) => _aus = aus;

        private static ErsatzparameterRC LeichteZone()
            => new ErsatzparameterRC(1.8016e6, 7.1519e7, 4.2769e-5, 5.44496e-4, 8.66509e-5, 1.93304e-4, 2.92046e-4,
                                     1.57705e-4, 3.02137e-3, 1762.195, 1268.193, 1466.932, 8.00903e-4, 4.0279e-3,
                                     153.804, 169.184, 2.08768e-5);

        /// <summary>Die Stunde des Befunds RB-Z4 mit dem Faktor <paramref name="gewinne"/> auf die Lasten.</summary>
        private static Stundenrand Stunde(double gewinne = 1.0)
            => new Stundenrand(6.15, 16.25, 20.0, double.PositiveInfinity,
                               4055.87 * gewinne, 2684.57 * gewinne, 1945.32 * gewinne);

        private static Zonenmodell2K Modell(bool messen) => new Zonenmodell2K(LeichteZone()) { Innenumkehrmessung = messen };

        /// <summary>
        /// Die Messung zählt einen geregelten Abschnitt, dessen Heizlast im Innern unter null fällt, obwohl Mittel
        /// und Endpunkt positiv sind (Massen 19,4985 / 21,0 °C, die Stunde des Befunds), und eine Bandverletzung im
        /// Totband (Massen 21,0 / 20,2 °C, Lasten × 1,5: die Raumluft fällt im Innern unter den Sollwert und steigt
        /// wieder darüber). Die Messung ändert keine Zahl.
        /// </summary>
        [Fact]
        public void Die_Messung_zaehlt_und_aendert_nichts()
        {
            var r = Stunde();
            var mit = Modell(true);
            var ohne = Modell(false);
            var muster = new Stundenmuster(new[] { Betriebsfall.HeizenGeregelt }, new[] { Zonenmodell2K.STUNDE_S });
            mit.Zuruecksetzen(19.4985, 21.0);
            ohne.Zuruecksetzen(19.4985, 21.0);
            Stundenergebnis a = mit.SchrittMitMuster(in r, muster);
            Stundenergebnis b = ohne.SchrittMitMuster(in r, muster);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Umkehr: {0} Abschnitt(e), {1:F1} J; Heizen {2:F3} W",
                                         a.MessungUmkehrAbschnitte, a.MessungUmkehrJ, a.HeizleistungW));
            Assert.Equal(1, a.MessungUmkehrAbschnitte);
            Assert.InRange(a.MessungUmkehrJ, 1.0e3, 1.0e6);
            Assert.Equal(0, b.MessungUmkehrAbschnitte);
            Assert.Equal(0.0, b.MessungUmkehrJ);
            Assert.Equal(b.HeizleistungW, a.HeizleistungW);
            Assert.Equal(b.ThetaMAwEnde, a.ThetaMAwEnde);
            Assert.Equal(b.ThetaMIwEnde, a.ThetaMIwEnde);

            var r2 = Stunde(1.5);
            var band = new Stundenmuster(new[] { Betriebsfall.Totband }, new[] { Zonenmodell2K.STUNDE_S });
            mit.Zuruecksetzen(21.0, 20.2);
            Stundenergebnis c = mit.SchrittMitMuster(in r2, band);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Band: {0} Abschnitt(e), {1:F3} K·s; Raumluft {2:F4} °C",
                                         c.MessungBandAbschnitte, c.MessungBandKs, c.ThetaAirMittel));
            Assert.Equal(1, c.MessungBandAbschnitte);
            Assert.True(c.MessungBandKs > 0.0);
            Assert.Equal(0, c.MessungUmkehrAbschnitte);
        }

        /// <summary>Die Messung des Laufs: Zähler und Einheiten (J → kWh, K·s → K·h), Summe und Skalierung.</summary>
        [Fact]
        public void Der_Zaehler_fasst_Stunden_und_Einheiten()
        {
            var z = new Innenumkehrzaehler();
            z.Aufnehmen(new Stundenergebnis(0, 0, 20, 20, 20, 20, 20, 20, 20, 20, 2) { MessungUmkehrJ = 3.6e6, MessungUmkehrAbschnitte = 2 });
            z.Aufnehmen(new Stundenergebnis(0, 0, 20, 20, 20, 20, 20, 20, 20, 20, 1) { MessungBandKs = 7200.0, MessungBandAbschnitte = 1 });
            z.Aufnehmen(new Stundenergebnis(0, 0, 20, 20, 20, 20, 20, 20, 20, 20, 1));
            Innenumkehrmessung m = z.Ergebnis();
            Assert.Equal(new Innenumkehrmessung(1, 2, 1.0, 1, 1, 2.0), m);
            Assert.Equal(new Innenumkehrmessung(2, 4, 2.0, 2, 2, 4.0), Innenumkehrmessung.Summe(new[] { m, null, m }));
            Assert.Null(Innenumkehrmessung.Summe(new Innenumkehrmessung[] { null }));
            Assert.Equal(new Innenumkehrmessung(1, 2, 3.0, 1, 1, 2.0), m.Skaliert(3.0));
        }
    }
}
