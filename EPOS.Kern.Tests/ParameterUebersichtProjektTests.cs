using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// KA-E-24: „Vergleichen…" stellt Projektzeilen und Katalogsätze nebeneinander — die Projektkopie liefert ihre
    /// Werte in DERSELBEN Parameterliste wie der Katalogsatz (<c>ParameterUebersichtCtrl.ProjektWerte</c>,
    /// <c>KaeltemaschineParameteruebersicht.ProjektWerte</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public class ParameterUebersichtProjektTests : System.IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly CultureInfo _kultur = Thread.CurrentThread.CurrentCulture;

        public ParameterUebersichtProjektTests() => Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _kultur;
            _db.Dispose();
        }

        [Fact]
        public void Die_Projektkopie_eines_Kessels_fuellt_dieselbe_Parameterliste()
        {
            IReadOnlyList<Parameterwert> katalog = ParameterUebersichtCtrl.Werte(Anlagenart.Heizkessel, "");
            IReadOnlyList<Parameterwert> projekt = ParameterUebersichtCtrl.ProjektWerte(Anlagenart.Heizkessel, 1007239);

            Assert.Equal(katalog.Select(p => p.Eintrag.Spalte), projekt.Select(p => p.Eintrag.Spalte));
            Assert.Equal("22.1", projekt.Single(p => p.Eintrag.Spalte == "Ptherm").Wert);
            Assert.Equal(ParameterVerwendung.LEER, projekt.Single(p => p.Eintrag.Spalte == "ReadOnly").Wert);
        }

        [Fact]
        public void Eine_unbekannte_Projektkopie_liefert_lauter_Leerwerte()
        {
            foreach (Anlagenart art in new[] { Anlagenart.Heizkessel, Anlagenart.Bhkw, Anlagenart.Waermepumpe,
                                               Anlagenart.Solarkollektoren, Anlagenart.Photovoltaik,
                                               Anlagenart.Stromspeicher, Anlagenart.Pufferspeicher })
                Assert.All(ParameterUebersichtCtrl.ProjektWerte(art, -1), p => Assert.Equal(ParameterVerwendung.LEER, p.Wert));
        }

        [Fact]
        public void Die_Projektkopie_einer_Kaeltemaschine_traegt_Kennwerte_und_Kennlinie()
        {
            IReadOnlyList<Parameterwert> projekt = KaeltemaschineParameteruebersicht.ProjektWerte(1);
            Assert.Equal(KaeltemaschineParameteruebersicht.Werte(null, false).Select(p => p.Eintrag.Spalte),
                         projekt.Select(p => p.Eintrag.Spalte));
            Assert.NotEqual(ParameterVerwendung.LEER,
                            projekt.Single(p => p.Eintrag.Spalte == KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG).Wert);
            Assert.All(KaeltemaschineParameteruebersicht.ProjektWerte(0), p => Assert.Equal(ParameterVerwendung.LEER, p.Wert));
        }
    }
}
