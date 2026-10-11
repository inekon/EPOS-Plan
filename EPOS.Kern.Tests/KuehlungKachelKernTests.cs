using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Kernwege der Kachel „Kühlung“ (Startseite, Reiter Energieerzeuger): die Liste der
    /// Wärmepumpen mit Kühlfunktion (<see cref="WPCtrl.KuehlfaehigeGeraete"/>) und das Umschalten des
    /// Kühlbetriebs (<see cref="WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten"/>) über den Schreibweg
    /// des Konfigurationsdialogs. Gerechnet auf einer Arbeitskopie der Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehlungKachelKernTests : IDisposable
    {
        /// <summary>1047: Wärmepumpe mit Kühlkennlinie im Projekt, auf Kühlbetrieb.</summary>
        private const int PROJEKT_1047 = 1047, WP_1047 = 1672046;

        /// <summary>1045: Wärmepumpe ohne Kühlkennlinie im Projekt.</summary>
        private const int PROJEKT_1045 = 1045, WP_1045 = 1672044;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static object Spalte(string spalte, int idWp)
            => DataRepository.ExecuteScalar("SELECT " + spalte + " FROM Tab_WP WHERE ID = ?", new DbParam("@id", idWp));

        [Fact]
        public void Die_Liste_fuehrt_die_Waermepumpe_mit_Kuehlkennlinie_samt_Kuehlbetrieb()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<WPCtrl.KuehlfaehigesGeraet> liste = WPCtrl.KuehlfaehigeGeraete(PROJEKT_1047);
            WPCtrl.KuehlfaehigesGeraet g = Assert.Single(liste, x => x.IdWp == WP_1047);
            Assert.True(g.Kuehlbetrieb);
            Assert.Null(g.Sperrgrund);
            Assert.False(string.IsNullOrEmpty(g.Bezeichner));
        }

        [Fact]
        public void Eine_Waermepumpe_ohne_Kuehlkennlinie_fehlt_in_der_Liste()
        {
            if (!_db.Vorhanden) return;

            Assert.False(KenndatenKuehlungCtrl.HatKenndatenProjekt(WP_1045));
            bool nachholbar = WPCtrl.KuehlkennlinieNachholbar(WP_1045);
            IReadOnlyList<WPCtrl.KuehlfaehigesGeraet> liste = WPCtrl.KuehlfaehigeGeraete(PROJEKT_1045);
            Assert.Equal(nachholbar, liste.Any(x => x.IdWp == WP_1045));
            Assert.Empty(WPCtrl.KuehlfaehigeGeraete(0));
        }

        [Fact]
        public void Umschalten_schreibt_nur_den_Kuehlbetrieb_und_laesst_Vorlauf_und_Hilfsstrom()
        {
            if (!_db.Vorhanden) return;

            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP_1047, PROJEKT_1047, true, 18, 0.05).Ok);

            Assert.Null(WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten(WP_1047, PROJEKT_1047, false));
            Assert.Equal(0L, Convert.ToInt64(Spalte("Kuehlbetrieb", WP_1047)));
            Assert.Equal(18L, Convert.ToInt64(Spalte("Kuehl_Vorlauf", WP_1047)));
            Assert.Equal(0.05, Convert.ToDouble(Spalte("Kuehl_Hilfsstromanteil", WP_1047)), 12);
            Assert.False(WPCtrl.KuehlfaehigeGeraete(PROJEKT_1047).Single(x => x.IdWp == WP_1047).Kuehlbetrieb);

            Assert.Null(WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten(WP_1047, PROJEKT_1047, true));
            Assert.Equal(1L, Convert.ToInt64(Spalte("Kuehlbetrieb", WP_1047)));
            Assert.Equal(18L, Convert.ToInt64(Spalte("Kuehl_Vorlauf", WP_1047)));
        }

        [Fact]
        public void Einschalten_ohne_Kuehlkennlinie_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;

            string grund = WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten(WP_1045, PROJEKT_1045, true);
            Assert.False(string.IsNullOrEmpty(grund));
            Assert.Equal(WPCtrl.KuehlbetriebSperrgrund(WP_1045, PROJEKT_1045), grund);
            Assert.Equal(0L, Convert.ToInt64(Spalte("Kuehlbetrieb", WP_1045)));
            Assert.Null(WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten(WP_1045, PROJEKT_1045, false));
        }
    }
}
