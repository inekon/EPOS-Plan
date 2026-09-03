using Xunit;
using WindowsFormsApplication1.Classes.Simulation;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Monatsgrenzen des Rechenkerns. Sie sind auf ein festes NICHT-Schaltjahr
    /// gerechnet, weil alle Vektoren fest 8760 Stunden fuehren - in einem Schaltjahr
    /// ergaebe sich <c>mo_ende[11] = 8783</c> und jeder Lauf brach mit einer
    /// IndexOutOfRangeException ab.
    /// </summary>
    public class InitTests
    {
        [Fact]
        public void Monatsgrenzen_umfassen_genau_8760_Stunden()
        {
            int[] anfang = new int[12];
            int[] ende = new int[12];

            new Init().Monatswerte_berechnen(anfang, ende);

            Assert.Equal(0, anfang[0]);
            Assert.Equal(8759, ende[11]);
        }

        [Fact]
        public void Monatsgrenzen_schliessen_lueckenlos_aneinander_an()
        {
            int[] anfang = new int[12];
            int[] ende = new int[12];

            new Init().Monatswerte_berechnen(anfang, ende);

            for (int i = 1; i < 12; i++)
                Assert.Equal(ende[i - 1] + 1, anfang[i]);
        }
    }
}
