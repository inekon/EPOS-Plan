using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Stundenachse nennt Stunde UND Datum</b> (Auftrag GX): Gemeinjahr ohne Jahr,
    /// Stunde 0 = 1. Januar 00:00, Monatskürzel und Muster aus dem Ressourcenkatalog.
    /// Gehalten werden die Grenzen des Jahres, die Viertelstundenreihe, die Uhrzeit im
    /// kurzen Ausschnitt, beide Sprachen und die Achsen, die KEIN Datum tragen.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZeitachseTests
    {
        private static Zeichenflaeche Flaeche(int n, Achsenart art = Achsenart.Stunden)
            => new Zeichenflaeche(new Rahmen(100f, 110f, 1000f, 360f),
                                  new Datenfenster(0, n - 1, 0, 10), art);

        [Theory]
        [InlineData(0, "1. Jan.")]
        [InlineData(23, "1. Jan.")]
        [InlineData(24, "2. Jan.")]
        [InlineData(59 * 24, "1. März")]
        [InlineData(2000, "25. März")]
        [InlineData(6000, "8. Sep.")]
        [InlineData(8759, "31. Dez.")]
        [InlineData(9999, "31. Dez.")]   // ausserhalb: an den Rand geklemmt
        [InlineData(-5, "1. Jan.")]
        public void Datum_deutsch(double stunde, string erwartet)
        {
            using (new Kulturvorrichtung("de-DE"))
                Assert.Equal(erwartet, Zeitachse.Datum(stunde));
        }

        [Theory]
        [InlineData(0, "Jan 1")]
        [InlineData(6000, "Sep 8")]
        [InlineData(8759, "Dec 31")]
        public void Datum_englisch(double stunde, string erwartet)
        {
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal(erwartet, Zeitachse.Datum(stunde));
        }

        [Fact]
        public void Uhrzeit_in_Stunden_und_Viertelstunden()
        {
            Assert.Equal("00:00", Zeitachse.Uhrzeit(0));
            Assert.Equal("23:00", Zeitachse.Uhrzeit(5999));
            Assert.Equal("06:15", Zeitachse.Uhrzeit(24 * 10 + 6.25));
            Assert.Equal("23:45", Zeitachse.Uhrzeit(8759.75));
        }

        [Fact]
        public void Markentext_nennt_die_Uhrzeit_nur_unter_einem_Tag()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                Assert.Equal("8. Sep.", Zeitachse.Markentext(6000, 500));
                Assert.Equal("8. Sep. 06:00", Zeitachse.Markentext(6006, 12));
            }
        }

        [Fact]
        public void Zeigertext_nennt_Stunde_Datum_und_Uhrzeit()
        {
            using (new Kulturvorrichtung("de-DE"))
                Assert.Equal("5.999 h · 7. Sep. 23:00",
                             Zeitachse.Zeigertext(5999, "h", CultureInfo.GetCultureInfo("de-DE")));
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("5,999 h · Sep 7 23:00",
                             Zeitachse.Zeigertext(5999, "h", CultureInfo.GetCultureInfo("en-US")));
        }

        [Fact]
        public void Nur_ein_Jahresraster_traegt_ein_Datum()
        {
            Assert.Equal(1, Zeitachse.WerteJeStunde(8760));
            Assert.Equal(4, Zeitachse.WerteJeStunde(35040));
            Assert.Equal(0, Zeitachse.WerteJeStunde(168));
            Assert.Equal(0, Zeitachse.WerteJeStunde(Flaeche(8760, Achsenart.Index)));   // Dauerlinie
            Assert.Equal(1, Zeitachse.WerteJeStunde(Flaeche(8760)));
        }

        [Fact]
        public void Datumszeile_der_Achsenteilung_im_Stunden_und_Viertelstundenraster()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                Assert.Equal("8. Sep.", ChartRenderer.Datumszeile(Flaeche(8760), 6000, 5000, 7000));
                // Viertelstundenreihe: x zaehlt die Stuetzstelle, 24 000 = Stunde 6 000.
                Assert.Equal("8. Sep.", ChartRenderer.Datumszeile(Flaeche(35040), 24000, 20000, 28000));
                // Ausschnitt unter einem Tag: mit Uhrzeit.
                Assert.Equal("8. Sep. 03:00", ChartRenderer.Datumszeile(Flaeche(35040), 24012, 24000, 24080));
                // Keine Zeitachse, kein Datum.
                Assert.Equal("", ChartRenderer.Datumszeile(Flaeche(8760, Achsenart.Index), 6000, 5000, 7000));
                Assert.Equal("", ChartRenderer.Datumszeile(Flaeche(168), 100, 0, 167));
            }
        }

        [Fact]
        public void Achsenteilung_einer_Viertelstundenreihe_nennt_Jahresstunden()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                var marken = ChartRenderer.Achsenteilung(Flaeche(35040), 20000, 28000);
                Assert.Contains(marken, m => m.Wert == 24000 && m.Text == "6.000");
                Assert.All(marken, m => Assert.True(m.Wert % 4 == 0));
                // Die Stundenreihe bleibt, wie sie war.
                var stunden = ChartRenderer.Achsenteilung(Flaeche(8760), 3000, 3500);
                Assert.Equal(ChartRenderer.Jahresstundenteilung(3000, 3500).Select(m => m.Text),
                             stunden.Select(m => m.Text));
            }
        }
    }
}
