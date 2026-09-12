using Xunit;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Das Zahlenformat der Eingabefelder (iU4-1). Die Regel ist bewusst
    /// TAUSENDERTRENNZEICHENFREI: „1.234,5" wird abgelehnt, statt wie
    /// <c>double.Parse(CurrentCulture)</c> still zu 12345 zu werden.
    /// </summary>
    public class ZahlTextTests
    {
        [Fact]
        public void Komma_wird_als_Dezimaltrennzeichen_gelesen()
        {
            double wert;
            Assert.True(ZahlText.Parsen("1,5", out wert));
            Assert.Equal(1.5, wert, 10);
        }

        [Fact]
        public void Punkt_wird_als_Dezimaltrennzeichen_gelesen()
        {
            double wert;
            Assert.True(ZahlText.Parsen("1.5", out wert));
            Assert.Equal(1.5, wert, 10);
        }

        [Fact]
        public void Tausendertrennzeichen_wird_abgelehnt()
        {
            double wert;
            Assert.False(ZahlText.Parsen("1.234,5", out wert));
            Assert.Equal(0.0, wert, 10);
        }

        [Fact]
        public void Ganzzahl_lehnt_Nachkommastellen_ab()
        {
            int wert;
            Assert.False(ZahlText.GanzzahlParsen("1,5", out wert));
            Assert.Equal(0, wert);
        }
    }
}
