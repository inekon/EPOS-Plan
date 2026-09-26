using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Bereitschaftsverlust des Heizkessels ist eine Leistung in kW</b> — und ein
    /// Wärmerest nach der ganzen Kaskade steht im Laufprotokoll.
    ///
    /// <para><b>Einheit.</b> <c>Tab_Heizkessel.Betriebsbereitschaftverlust</c> kommt aus
    /// VDI 3805 Blatt 3, Satz 700, Spalte 28, und der Import weist sie in kW aus. Eine
    /// Stillstandsstunde kostet deshalb genau diesen Wert in kWh Brennstoff — nicht das
    /// Produkt mit der Nennleistung. Gemessen an Projekt 1007 der Testdatenbank: ein
    /// Gaskessel mit 22,1 kW und 0,05 kW Bereitschaftsleistung, ohne Quellpuffer; seine
    /// Stillstandsstunden sind die Stunden ohne Kesselabgabe.</para>
    ///
    /// <para><b>Meldung.</b> Projekt 1023 behält nach allen Erzeugern Wärme übrig — der
    /// Lauf nennt das als Warnung; Projekt 1030 deckt alles und bleibt ohne sie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselBereitschaftTests : IDisposable
    {
        private const int PROJEKT_GASKESSEL = 1007;
        private const double BEREITSCHAFT_1007_KW = 0.05;
        private const int PROJEKT_MIT_REST = 1023;
        private const int PROJEKT_OHNE_REST = 1030;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Theory]
        [InlineData(0.075, 0.075)]
        [InlineData(1.5, 1.5)]      // kein Prozentwert: bleibt 1,5 kW
        [InlineData(0.0, 0.0)]
        [InlineData(-0.2, 0.0)]
        [InlineData(double.NaN, 0.0)]
        public void Der_Katalogwert_ist_die_Bereitschaftsleistung_in_kW(double katalog, double erwartetKw)
        {
            Assert.Equal(erwartetKw, SimulationSPK.BereitschaftsleistungKw(katalog));
        }

        [Fact]
        public void Eine_Stillstandsstunde_kostet_die_Bereitschaftsleistung_nicht_ihr_Produkt_mit_der_Nennleistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_GASKESSEL, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.Equal(1, spk.KesselAnzahl);

            int stillstand = spk.Kesselleistung_stuendlich.Count(q => q <= 0);
            Assert.InRange(stillstand, 1, 8759);

            double waermeMwh = spk.s_waerme_Gas_Spk[0];
            double wirk = spk.Kessel_Wirk_Gas_Spk[0];
            Assert.True(waermeMwh > 0 && wirk > 0);

            double erwartetMwh = waermeMwh / wirk + stillstand * BEREITSCHAFT_1007_KW / 1000.0;
            Assert.Equal(erwartetMwh, spk.Kessel_Verbrauch_MWh_Spk[0], 9);
        }

        [Fact]
        public void Ein_Waermerest_nach_der_Kaskade_steht_als_Warnung_im_Laufprotokoll()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_WAERME_UNTERDECKUNG.Split('{')[0];

            var mitRest = new SimulationRunner();
            string fehler;
            Assert.True(mitRest.Simuliere(PROJEKT_MIT_REST, out fehler), "Lauf gescheitert: " + fehler);
            Assert.True(mitRest.sim.RestwaermeMwh > 1.0);
            Assert.Contains(mitRest.Protokoll.Warnungen, w => w.Contains(kopf));

            var ohneRest = new SimulationRunner();
            Assert.True(ohneRest.Simuliere(PROJEKT_OHNE_REST, out fehler), "Lauf gescheitert: " + fehler);
            Assert.DoesNotContain(ohneRest.Protokoll.Warnungen, w => w.Contains(kopf));
        }
    }
}
