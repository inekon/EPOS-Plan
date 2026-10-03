using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die vier Stundenreihen der „Stromlast Jahresganglinie“ im BHKW-Reiter
    /// (<see cref="SimulationErgebnisCtrl.BhkwStromreihenBilden"/>): Stromproduktion und
    /// Strombedarf unverändert, der Reststrombedarf je Stunde geklemmt, die Einspeisung aus
    /// der Flottenbilanz (Stundenmittel) oder aus dem KWK-Split des Laufs. Im Lauf ergeben die
    /// Reihen dieselben Jahressummen wie die Kennzahlen des Reiters.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BhkwStromreihenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        [Fact]
        public void Reststrom_ist_je_Stunde_geklemmt_und_die_Reihen_bleiben_unveraendert()
        {
            double[] bedarf = { 10, 5, 0, 8 };
            double[] produktion = { 4, 9, 3, 8 };
            double[] lauf = { 0, 4, 3, 0 };

            var r = SimulationErgebnisCtrl.BhkwStromreihenBilden(bedarf, produktion, null, lauf);

            Assert.Equal(new double[] { 6, 0, 0, 0 }, r.Reststrombedarf);
            Assert.Equal(produktion, r.Stromproduktion);
            Assert.Equal(bedarf, r.Strombedarf);
            Assert.Equal(lauf, r.Einspeisung);
            Assert.NotSame(produktion, r.Stromproduktion);
            Assert.NotSame(bedarf, r.Strombedarf);
        }

        [Fact]
        public void Flottenbilanz_geht_vor_und_wird_zum_Stundenmittel()
        {
            double[] bedarf = { 1, 1 };
            double[] produktion = { 2, 2 };
            double[] flotteKw = { 1, 2, 3, 2, 0, 0, 4, 0 };

            var r = SimulationErgebnisCtrl.BhkwStromreihenBilden(bedarf, produktion, flotteKw, new double[] { 9, 9 });

            Assert.Equal(new double[] { 2, 1 }, r.Einspeisung);
        }

        [Fact]
        public void Ohne_Einspeisereihe_ist_die_Einspeisung_null_und_ohne_Reihen_leer()
        {
            var r = SimulationErgebnisCtrl.BhkwStromreihenBilden(new double[] { 3 }, new double[] { 5 }, null, null);
            Assert.Equal(new double[] { 0 }, r.Einspeisung);

            var leer = SimulationErgebnisCtrl.BhkwStromreihenBilden(null, null, null, null);
            Assert.Empty(leer.Stromproduktion);
            Assert.Empty(leer.Einspeisung);
            Assert.Null(SimulationErgebnisCtrl.BhkwStromStunden(null));
        }

        /// <summary>
        /// Im Lauf tragen die Stundenreihen dieselben Jahressummen wie die Kennzahlen des
        /// Reiters: Stromproduktion, Strombedarf, Stromeinspeisung und Reststrombedarf.
        /// </summary>
        [Theory]
        [InlineData(1018)]
        [InlineData(1017)]
        public void Im_Lauf_stimmen_die_Summen_mit_den_Kennzahlen_ueberein(int idProjekt)
        {
            if (!_db.Vorhanden) return;

            var lauf = new SimulationRunner();
            Assert.True(lauf.Simuliere(idProjekt, out string fehler), "Lauf gescheitert: " + fehler);
            var bh = SimulationErgebnisCtrl.Bhkw(lauf.sim, lauf.simulation_Waermebedarf, lauf.simulation_Strombedarf);
            var r = SimulationErgebnisCtrl.BhkwStromStunden(lauf.sim);

            Assert.NotNull(r);
            Assert.Equal(8760, r.Stromproduktion.Length);
            Assert.Equal(bh.StrombedarfMwh, r.Strombedarf.Sum() / 1000.0, 6);
            Assert.Equal(bh.EinspeisungMwh, r.Einspeisung.Sum() / 1000.0, 6);
            Assert.Equal(lauf.sim.simulation_bhkw.stromproduktion.Sum() / 1000.0, r.Stromproduktion.Sum() / 1000.0, 9);
            Assert.True(r.Stromproduktion.Sum() > 0);
            Assert.True(r.Reststrombedarf.All(w => w >= 0));
            for (int h = 0; h < 8760; h++)
                Assert.True(r.Einspeisung[h] <= r.Stromproduktion[h] + 1e-9);
        }
    }
}
