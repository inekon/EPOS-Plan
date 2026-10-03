using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Reihenbildung des Bildes „Kälteproduktion" (Unterreiter „Kälte Produktion Chart"):
    /// je Kälteerzeuger eine Säule, darauf die ungedeckte Kälte, darüber der Kältebedarf als
    /// Linie — und kein Bild, wenn das Projekt keine Kälte rechnet.
    /// </summary>
    public class KaelteProduktionBildTests
    {
        private static double[] Reihe(double wert)
            => Enumerable.Repeat(wert, 8760).ToArray();

        private static KaelteProduktionBild.Reihen Satz(double wp, double rest)
        {
            var r = new KaelteProduktionBild.Reihen { Bedarf = Reihe(wp + rest), Rest = Reihe(rest) };
            r.Erzeuger.Add(new KaelteProduktionBild.Erzeugerreihe { Name = "", Werte = Reihe(wp) });
            return r;
        }

        [Fact]
        public void Erzeuger_Rest_und_Bedarf_stehen_in_ihrer_Rolle()
        {
            var (stapel, linien) = KaelteProduktionBild.Reihenbilden(Satz(3.0, 1.0),
                new KaelteProduktionBild.Texte { Erzeuger = "WP", Rest = "Rest", Bedarf = "Bedarf" }, false);

            Assert.Equal(new[] { "WP", "Rest" }, stapel.Select(s => s.Name).ToArray());
            Assert.Equal(new[] { Farbrolle.WAERME_WP, Farbrolle.REST }, stapel.Select(s => s.Rolle).ToArray());
            Assert.All(stapel, s => Assert.Equal(ChartRenderer.Stapelart.Saeule, s.Stapelgruppe));

            ChartRenderer.Reihe bedarf = Assert.Single(linien);
            Assert.Equal("Bedarf", bedarf.Name);
            Assert.Equal(Farbrolle.BEDARF, bedarf.Rolle);

            // Die Stapelhöhe ist in jeder Stunde der Bedarf.
            for (int h = 0; h < 8760; h += 997)
                Assert.Equal(bedarf.Werte[h], stapel.Sum(s => s.Werte[h]), 12);
        }

        [Fact]
        public void Ohne_ungedeckte_Kaelte_steht_kein_Rest()
        {
            var (stapel, _) = KaelteProduktionBild.Reihenbilden(Satz(2.0, 0.0), null, false);

            ChartRenderer.Reihe wp = Assert.Single(stapel);
            Assert.Equal(Farbrolle.WAERME_WP, wp.Rolle);
        }

        [Fact]
        public void Ohne_Kaelteerzeuger_ist_der_ganze_Bedarf_ungedeckt()
        {
            var r = new KaelteProduktionBild.Reihen { Bedarf = Reihe(2.0), Rest = Reihe(2.0) };
            var (stapel, linien) = KaelteProduktionBild.Reihenbilden(r, null, false);

            ChartRenderer.Reihe rest = Assert.Single(stapel);
            Assert.Equal(Farbrolle.REST, rest.Rolle);
            Assert.Equal(linien[0].Werte, rest.Werte);
        }

        [Fact]
        public void Mehrere_Erzeuger_behalten_ihren_Namen_und_bekommen_eigene_Rollen()
        {
            var r = Satz(1.0, 0.0);
            r.Erzeuger[0].Name = "WP 1";
            r.Erzeuger.Add(new KaelteProduktionBild.Erzeugerreihe { Name = "WP 2", Werte = Reihe(1.0) });

            var (stapel, _) = KaelteProduktionBild.Reihenbilden(r, null, false);

            Assert.Equal(new[] { "WP 1", "WP 2" }, stapel.Select(s => s.Name).ToArray());
            Assert.NotEqual(stapel[0].Rolle, stapel[1].Rolle);
        }

        [Fact]
        public void Die_Reihen_sind_Kopien()
        {
            var r = Satz(3.0, 1.0);
            var (stapel, linien) = KaelteProduktionBild.Reihenbilden(r, null, false);

            stapel[0].Werte[0] = 99.0;
            linien[0].Werte[0] = 99.0;

            Assert.Equal(3.0, r.Erzeuger[0].Werte[0]);
            Assert.Equal(4.0, r.Bedarf[0]);
        }

        [Fact]
        public void Ohne_Kaelte_gibt_es_kein_Bild()
        {
            Assert.Null(KaelteProduktionBild.AusLauf(null));
            Assert.Null(KaelteProduktionBild.AusLauf(new SimulationKaeltebedarf()));   // nicht gerechnet
            Assert.Null(KaelteProduktionBild.Modell(null));
            Assert.Null(KaelteProduktionBild.Png(null));
        }

        [Fact]
        public void Das_Bild_zeichnet_und_ist_deterministisch()
        {
            Assert.NotNull(KaelteProduktionBild.Modell(Satz(3.0, 1.0), null, true));
            byte[] a = KaelteProduktionBild.Png(Satz(3.0, 1.0));
            byte[] b = KaelteProduktionBild.Png(Satz(3.0, 1.0));
            Assert.NotNull(a);
            Assert.Equal(a, b);
        }
    }
}
