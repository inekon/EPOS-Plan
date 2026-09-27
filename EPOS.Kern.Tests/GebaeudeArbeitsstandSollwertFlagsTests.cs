using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Flags <c>Wochenende</c> und <c>Ferien</c> folgen der Wirksamkeitsschwelle des Kerns</b>
    /// — nicht jedem Wert über 0. <see cref="GebaeudeArbeitsstand.Ableiten"/> setzte beide Flags
    /// bislang schon bei jeder Absenkung/jedem Feriensollwert &gt; 0; der Kern rechnet das
    /// Wochenende erst über 5 °C (<see cref="Gebaeudemodellvorgaben.WochenendsollwertWirksam"/>) und
    /// die Ferien erst ab 1 °C (<see cref="Gebaeudemodellvorgaben.FeriensollwertWirksam"/>,
    /// <c>GebaeudeModellEingang.Sollwertfahrplan</c>/<c>Ferienfahrplan</c>) — beide Stellen tragen
    /// jetzt dieselbe Regel.
    /// </summary>
    public sealed class GebaeudeArbeitsstandSollwertFlagsTests
    {
        private static GebaeudeArbeitsstand Abgeleitet(double? wochenendAbsenkung, double? sollFerien)
        {
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(null, true);
            arbeit.Stand.WochenendAbsenkung = wochenendAbsenkung;
            arbeit.Stand.SollFerien = sollFerien;
            arbeit.Ableiten();
            return arbeit;
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(3.0)]
        [InlineData(5.0)] // die Schwelle selbst wirkt noch NICHT (Vergleich ">", nicht ">=")
        public void Wochenende_bleibt_0_bis_zur_Schwelle(double wochenendAbsenkung)
            => Assert.Equal(0.0, Abgeleitet(wochenendAbsenkung, null).Stand.Wochenende);

        [Fact]
        public void Wochenende_wird_1_ueber_5_Grad()
            => Assert.Equal(1.0, Abgeleitet(5.0001, null).Stand.Wochenende);

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        public void Ferien_bleibt_0_unter_1_Grad(double sollFerien)
            => Assert.Equal(0.0, Abgeleitet(null, sollFerien).Stand.Ferien);

        [Fact]
        public void Ferien_wird_1_ab_1_Grad()
            => Assert.Equal(1.0, Abgeleitet(null, 1.0).Stand.Ferien);

        [Fact]
        public void Leere_Felder_gelten_wie_0_und_setzen_keins_der_Flags()
        {
            GebaeudeArbeitsstand arbeit = Abgeleitet(null, null);
            Assert.Equal(0.0, arbeit.Stand.Wochenende);
            Assert.Equal(0.0, arbeit.Stand.Ferien);
        }
    }
}
