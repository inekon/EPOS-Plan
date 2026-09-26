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
    ///
    /// <para><b>Betriebsbereitschaft (#568).</b> Der Verlust fällt nur in Stillstandsstunden
    /// an, in denen der Kessel betriebsbereit ist: an einem Heiztag (Raumwärmebedarf des
    /// Projekts vor der Kaskade &gt; 0) oder im Nachlauf von 24 Stunden nach seiner letzten
    /// Laufstunde. 1007 läuft 1 797 Stunden und steht 6 963 Stunden still; betriebsbereit
    /// sind davon 5 931 — die übrigen 1 032 liegen im Sommer außerhalb des Nachlaufs. Die
    /// Vorgabe <c>Tab_Einstellungen.Kessel_Betriebsbereitschaft</c> [h/a] deckelt Lauf- plus
    /// Bereitschaftsstunden; 0 heißt „kein Deckel“.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselBereitschaftTests : IDisposable
    {
        private const int PROJEKT_GASKESSEL = 1007;
        private const double BEREITSCHAFT_1007_KW = 0.05;
        private const int LAUFSTUNDEN_1007 = 1797;
        private const int BEREITSCHAFTSSTUNDEN_1007 = 5931;
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
        public void Eine_betriebsbereite_Stillstandsstunde_kostet_die_Bereitschaftsleistung_nicht_ihr_Produkt_mit_der_Nennleistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_GASKESSEL, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.Equal(1, spk.KesselAnzahl);

            int stillstand = spk.Kesselleistung_stuendlich.Count(q => q <= 0);
            Assert.Equal(8760 - LAUFSTUNDEN_1007, stillstand);
            Assert.Equal(LAUFSTUNDEN_1007, spk.Laufstunden_Spk[0]);

            // Nur ein Teil der Stillstandsstunden ist betriebsbereit (Sommer ohne Nachlauf).
            Assert.Equal(BEREITSCHAFTSSTUNDEN_1007, spk.Bereitschaftsstunden_Spk[0]);
            Assert.True(spk.Bereitschaftsstunden_Spk[0] < stillstand);

            double waermeMwh = spk.s_waerme_Gas_Spk[0];
            double wirk = spk.Kessel_Wirk_Gas_Spk[0];
            Assert.True(waermeMwh > 0 && wirk > 0);

            double bereitschaftKwh = BEREITSCHAFTSSTUNDEN_1007 * BEREITSCHAFT_1007_KW;
            Assert.Equal(bereitschaftKwh, spk.Bereitschaftsverlust_KWh_Spk[0], 9);

            double erwartetMwh = waermeMwh / wirk + bereitschaftKwh / 1000.0;
            Assert.Equal(erwartetMwh, spk.Kessel_Verbrauch_MWh_Spk[0], 9);
        }

        [Theory]
        [InlineData(100, 50, true)]      // Heiztag: betriebsbereit, egal wann er lief
        [InlineData(5000, int.MinValue, false)] // Sommertag, nie gelaufen: abgeschaltet
        [InlineData(5000, 4976, true)]   // Sommertag, 24 h nach der letzten Laufstunde: Nachlauf
        [InlineData(5000, 4975, false)]  // Sommertag, 25 h danach: abgeschaltet
        public void Betriebsbereit_ist_der_Kessel_am_Heiztag_oder_im_Nachlauf(int stunde, int letzteLauf, bool erwartet)
        {
            double[] raumwaerme = new double[8760];
            for (int h = 0; h < 24 * 120; h++) raumwaerme[h] = 1.0;   // Heiztage 0 … 119

            bool[] heiztage = SimulationSPK.HeiztageAus(raumwaerme);
            Assert.Equal(120, heiztage.Count(t => t));
            Assert.Equal(erwartet, SimulationSPK.IstBetriebsbereit(heiztage, stunde, letzteLauf));
        }

        [Fact]
        public void Ohne_Heizperiode_gilt_jede_Stunde_als_betriebsbereit()
        {
            Assert.Null(SimulationSPK.HeiztageAus(null));
            Assert.True(SimulationSPK.IstBetriebsbereit(null, 5000, int.MinValue));
        }

        [Theory]
        [InlineData(0, 1797, 5931, 5931)]      // 0 = kein Deckel
        [InlineData(6000, 1797, 5931, 4203)]   // Deckel: Vorgabe − Laufstunden
        [InlineData(9000, 1797, 5931, 5931)]   // Vorgabe über dem Bedarf: nichts zu kappen
        [InlineData(1000, 1797, 5931, 0)]      // Vorgabe unter den Laufstunden: keine Bereitschaft
        public void Die_Vorgabe_deckelt_Lauf_und_Bereitschaftsstunden(int vorgabe, int lauf, int bereit, int erwartet)
        {
            Assert.Equal(erwartet, SimulationSPK.BereitschaftsstundenGedeckelt(vorgabe, lauf, bereit));
        }

        [Fact]
        public void Die_Vorgabe_des_Projekts_nimmt_den_Ueberhang_samt_Verbrauch_zurueck_und_meldet_ihn()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int VORGABE = 6000;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Einstellungen SET Kessel_Betriebsbereitschaft = ? WHERE ID_Projekt = ?",
                new DbParam("@v", VORGABE), new DbParam("@p", PROJEKT_GASKESSEL)));

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_GASKESSEL, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            int erlaubt = VORGABE - LAUFSTUNDEN_1007;
            Assert.Equal(erlaubt, spk.Bereitschaftsstunden_Spk[0]);
            Assert.Equal(erlaubt * BEREITSCHAFT_1007_KW, spk.Bereitschaftsverlust_KWh_Spk[0], 9);

            double erwartetMwh = spk.s_waerme_Gas_Spk[0] / spk.Kessel_Wirk_Gas_Spk[0] +
                                 erlaubt * BEREITSCHAFT_1007_KW / 1000.0;
            Assert.Equal(erwartetMwh, spk.Kessel_Verbrauch_MWh_Spk[0], 9);

            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_BEREITSCHAFT_GEDECKELT.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(kopf));
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
