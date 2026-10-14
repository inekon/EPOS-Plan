using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Monatsstapel der Autarkie-Analyse (Ergebnisseite, Reiter Stromspeicher) summiert
    /// je KALENDERMONAT im 365-Tage-Raster ohne Schaltjahr — nicht in starren 730-h-Blöcken.
    /// Eine konstante Last von 1 kW ohne PV und ohne Speicher ergibt damit als Autarkielücke
    /// genau Tage × 24 kWh je Monat: Januar 744, Februar 672, April 720.
    /// </summary>
    public class AutarkieMonateTests
    {
        private const int VIERTELSTUNDEN = 35040;

        [Fact]
        public void Die_Monatssaeulen_stehen_auf_Kalendermonaten()
        {
            double[] last = new double[VIERTELSTUNDEN];
            double[] pv = new double[VIERTELSTUNDEN];
            double[] entladung = new double[VIERTELSTUNDEN];
            for (int i = 0; i < VIERTELSTUNDEN; i++) last[i] = 1.0;

            SimulationErgebnisHuelle.AutarkieMonate(last, pv, entladung,
                out double[] direkt, out double[] ausSpeicher, out double[] luecke);

            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            double summe = 0.0;
            for (int m = 0; m < 12; m++)
            {
                Assert.Equal(tage[m] * 24.0, luecke[m], 6);
                Assert.Equal(0.0, direkt[m], 9);
                Assert.Equal(0.0, ausSpeicher[m], 9);
                summe += luecke[m];
            }
            Assert.Equal(8760.0, summe, 6);
        }

        [Fact]
        public void Direktverbrauch_und_Speicherentnahme_fallen_in_ihren_Kalendermonat()
        {
            double[] last = new double[VIERTELSTUNDEN];
            double[] pv = new double[VIERTELSTUNDEN];
            double[] entladung = new double[VIERTELSTUNDEN];

            // Letzte Viertelstunde des Januars (Index 31·96 − 1) und erste des Februars.
            int janEnde = 31 * 96 - 1;
            last[janEnde] = 4.0; pv[janEnde] = 4.0;            // 1 kWh direkt im Januar
            last[janEnde + 1] = 4.0; entladung[janEnde + 1] = 1.0;  // 1 kWh aus dem Speicher im Februar

            SimulationErgebnisHuelle.AutarkieMonate(last, pv, entladung,
                out double[] direkt, out double[] ausSpeicher, out double[] luecke);

            Assert.Equal(1.0, direkt[0], 9);
            Assert.Equal(0.0, direkt[1], 9);
            Assert.Equal(1.0, ausSpeicher[1], 9);
            Assert.Equal(0.0, luecke[1], 9);
        }
    }
}
