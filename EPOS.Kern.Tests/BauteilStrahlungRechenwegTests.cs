using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// DA-8: Absorptions- und Emissionsgrad je Bauteil im Rechenweg — das Feld des Bauteils, sonst das seines Aufbaus, sonst
    /// der Festwert; leer rechnet bitgleich zum Festwert.
    /// </summary>
    public class BauteilStrahlungRechenwegTests
    {
        private static BauteilModel Fenster(int? aufbau = null)
            => new BauteilModel { ID = 3, Bezeichner = "Probe", Bauteilart = DbWerte.BAUTEILART_FENSTER, Flaeche = 2.0, U_Wert = 1.1,
                                  ID_Aufbau = aufbau };

        private static Dictionary<int, BauteilaufbauModel> Aufbauten(double? absorption, double? emission)
        {
            var a = new BauteilaufbauModel { ID = 40, Bezeichner = "Aufbau", Bauteilart = DbWerte.BAUTEILART_FENSTER };
            a.Zusatz[BauteilStrahlungSchema.SPALTE_ABSORPTION] = absorption;
            a.Zusatz[BauteilStrahlungSchema.SPALTE_EMISSION] = emission;
            return new Dictionary<int, BauteilaufbauModel> { [40] = a };
        }

        [Fact]
        public void Leer_gilt_der_Festwert_bitgleich()
        {
            BauteilEingang b = GebaeudeZonenabbildung.AlsBauteil(Fenster(), new Dictionary<int, BauteilaufbauModel>(), "Probe");
            Assert.True(double.IsNaN(b.AbsorptionsgradSolar));
            Assert.True(double.IsNaN(b.Emissionsgrad));
            Assert.Equal(GebaeudeFestwerte.ABSORPTIONSGRAD_OPAK, b.AbsorptionsgradSolarWirksam);
            Assert.Equal(GebaeudeFestwerte.EMISSIONSGRAD_AUSSEN, b.EmissionsgradWirksam);

            const double eA = 300.0, theta = 4.0;
            Assert.Equal(GebaeudeKlimaweg.DeltaThetaLangwellig(eA, theta, 0.5),
                         GebaeudeKlimaweg.DeltaThetaLangwellig(eA, theta, 0.5, b.EmissionsgradWirksam));
            Assert.Equal(GebaeudeKlimaweg.DeltaThetaKurzwellig(500.0, eA, theta),
                         GebaeudeKlimaweg.DeltaThetaKurzwellig(500.0, eA, theta, b.AbsorptionsgradSolarWirksam));
        }

        [Fact]
        public void Das_Bauteil_vor_dem_Aufbau_vor_dem_Festwert()
        {
            BauteilEingang ausAufbau = GebaeudeZonenabbildung.AlsBauteil(Fenster(40), Aufbauten(0.3, 0.8), "Probe");
            Assert.Equal(0.3, ausAufbau.AbsorptionsgradSolar);
            Assert.Equal(0.8, ausAufbau.Emissionsgrad);

            BauteilModel eigen = Fenster(40);
            eigen.Absorptionsgrad_Solar = 0.2;
            BauteilEingang gemischt = GebaeudeZonenabbildung.AlsBauteil(eigen, Aufbauten(0.3, null), "Probe");
            Assert.Equal(0.2, gemischt.AbsorptionsgradSolar);
            Assert.Equal(GebaeudeFestwerte.EMISSIONSGRAD_AUSSEN, gemischt.EmissionsgradWirksam);

            // Die abgeleiteten Eingänge tragen die Werte weiter.
            BauteilEingang mitGebaeude = gemischt.MitGebaeudewerten(0.5, 0.3, 0.9);
            Assert.Equal(0.2, mitGebaeude.AbsorptionsgradSolar);
        }

        [Fact]
        public void Die_Terme_skalieren_mit_dem_Feld()
        {
            // NULL-Regel: ohne Gegenstrahlung α_A = 25 W/(m²K), Δθ_kw = I·a_F/α_A.
            Assert.Equal(500.0 * 0.3 / 25.0, GebaeudeKlimaweg.DeltaThetaKurzwellig(500.0, double.NaN, 5.0, 0.3), 12);
            double eA = 280.0, theta = 0.0;
            double fest = GebaeudeKlimaweg.DeltaThetaLangwellig(eA, theta, 1.0);
            Assert.Equal(fest * 0.45 / GebaeudeFestwerte.EMISSIONSGRAD_AUSSEN,
                         GebaeudeKlimaweg.DeltaThetaLangwellig(eA, theta, 1.0, 0.45), 12);
            Assert.Equal(0.0, GebaeudeKlimaweg.DeltaThetaLangwellig(eA, theta, 1.0, 0.0));
        }
    }
}
