using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Kennwerte der gerechneten Soletemperatur am Quelleintritt, die der Erdreichdialog nach einem
    /// Lauf zeigt (Anwendermeldung 10.10.2026): Jahresmittel, Tiefst- und Höchstwert mit Zeitpunkt im
    /// Raster des Kerns (8760 Stunden, Gemeinjahr).
    /// </summary>
    public class ErdreichLaufkennwerteTests
    {
        private static double[] Reihe()
        {
            var r = new double[ErdreichTemperatur.STUNDEN_JAHR];
            for (int i = 0; i < r.Length; i++) r[i] = 5.0;
            r[1000] = -1.26;   // 11. Februar, 16:00 (Tag 41, Stunde 16)
            r[1001] = -1.26;   // derselbe Tiefstwert später: zählt nicht als Zeitpunkt
            r[5000] = 12.0;    // 28. Juli, 08:00 (Tag 208, Stunde 8)
            return r;
        }

        [Fact]
        public void Mittel_Tiefst_und_Hoechstwert_mit_Zeitpunkt()
        {
            ErdreichTemperatur.Laufkennwerte k = ErdreichTemperatur.LaufKennwerte(Reihe());

            Assert.NotNull(k);
            Assert.Equal(-1.26, k.Min);
            Assert.Equal(1000, k.StundeMin);
            Assert.Equal(12.0, k.Max);
            Assert.Equal(5000, k.StundeMax);
            double erwartet = (5.0 * (8760 - 3) - 2.52 + 12.0) / 8760;
            Assert.Equal(erwartet, k.Mittel, 12);
        }

        [Fact]
        public void Nicht_endliche_Werte_zaehlen_nicht_und_ohne_Wert_gibt_es_keine_Kennwerte()
        {
            double[] r = Reihe();
            r[0] = double.NaN;
            r[1] = double.NegativeInfinity;
            ErdreichTemperatur.Laufkennwerte k = ErdreichTemperatur.LaufKennwerte(r);
            Assert.Equal(-1.26, k.Min);
            Assert.Equal(1000, k.StundeMin);
            Assert.Equal((5.0 * (8758 - 3) - 2.52 + 12.0) / 8758, k.Mittel, 12);

            Assert.Null(ErdreichTemperatur.LaufKennwerte(null));
            Assert.Null(ErdreichTemperatur.LaufKennwerte(new[] { double.NaN }));
        }

        [Fact]
        public void Die_Zeile_steht_im_Hausformat()
        {
            using var kultur = new Kulturvorrichtung();

            Assert.Equal("01.01., 00:00 Uhr", ErdreichTemperatur.Zeitpunkt(0));
            Assert.Equal("31.12., 23:00 Uhr", ErdreichTemperatur.Zeitpunkt(8759));
            Assert.Equal("01.03., 00:00 Uhr", ErdreichTemperatur.Zeitpunkt(59 * 24));   // kein Schaltjahr

            string zeile = ErdreichTemperatur.LaufKennwerte(Reihe()).Zeile();
            Assert.Equal("Jahresmittel 5,0 °C  ·  Tiefstwert -1,3 °C am 11.02., 16:00 Uhr  ·  "
                         + "Höchstwert 12,0 °C am 28.07., 08:00 Uhr", zeile);
        }
    }
}
