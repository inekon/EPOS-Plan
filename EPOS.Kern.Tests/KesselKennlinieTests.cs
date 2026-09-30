using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Teillastkennlinie des Heizkessels</b> (Konzept Kesselkennlinie 4.1 und 4.3, Etappe E2).
    ///
    /// <para><b>Die Kurve.</b> <see cref="Kesselkennlinie.Eta"/> trifft η₃₀ bei β = 0,3 und η₁₀₀
    /// bei β = 1 bitgenau, ist dazwischen linear, darunter flach und in β stetig — auch am Knick,
    /// wo ein β am letzten Bit keinen Zustand kippt.</para>
    ///
    /// <para><b>Die Normvorgabe (Konzept 7.1, Entscheid F1).</b> Ein leeres η₃₀ nimmt nach Bauart
    /// η₁₀₀ + 0,06 (Brennwertkessel, höchstens Hs/Hi), η₁₀₀ (Niedertemperaturkessel) oder
    /// η₁₀₀ − 0,03 (Standardkessel); ein gepflegter Wert geht vor, auch als Prozentangabe.</para>
    ///
    /// <para><b>Der Lauf.</b> Projekt 1023 (Brennwertkessel, η₃₀ leer) rechnet mit der Vorgabe
    /// 0,874 + 0,06 und spart in Teillast Brennstoff; seine Stundenreihe des Wirkungsgrads ist
    /// Stunde für Stunde die Kurve bei der Laststufe der Stunde. Projekt 1007
    /// (Niedertemperaturkessel nach Bauartregel) und der Elektrokessel von 1017 rechnen Stunde für
    /// Stunde wie mit dem festen Wirkungsgrad.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselKennlinieTests : IDisposable
    {
        private const int PROJEKT_BRENNWERT = 1023;
        private const double ETA100_1023 = 0.874;
        private const int PROJEKT_NIEDERTEMPERATUR = 1007;
        private const int PROJEKT_ELEKTROKESSEL = 1017;
        private const int BRENNSTOFF_ERDGAS = 3;
        private const int BRENNSTOFF_HEIZOEL = 9;
        private const int BRENNSTOFF_PELLETS = 15;
        private const int BRENNSTOFF_KOHLE = 11;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================
        //  Die Kurve
        // =================================================================

        [Theory]
        [InlineData(0.97, 1.05)]
        [InlineData(0.874, 0.934)]
        [InlineData(0.90, 0.87)]
        public void Die_Kurve_trifft_beide_Pruefpunkte_bitgenau(double eta100, double eta30)
        {
            Assert.Equal(eta30, Kesselkennlinie.Eta(0.3, eta100, eta30));
            Assert.Equal(eta100, Kesselkennlinie.Eta(1.0, eta100, eta30));
        }

        [Fact]
        public void Unter_dreissig_Prozent_gilt_eta30_und_ueber_Nennlast_eta100()
        {
            Assert.Equal(1.05, Kesselkennlinie.Eta(0.0, 0.97, 1.05));
            Assert.Equal(1.05, Kesselkennlinie.Eta(0.1, 0.97, 1.05));
            Assert.Equal(0.97, Kesselkennlinie.Eta(1.2, 0.97, 1.05));
            Assert.Equal(1.05, Kesselkennlinie.Eta(double.NaN, 0.97, 1.05));
            Assert.Equal(0.97, Kesselkennlinie.Eta(double.PositiveInfinity, 0.97, 1.05));
        }

        /// <summary>Das neutrale Beispiel aus Konzept 2: 60 kWh bei β = 0,6 brauchen 59,1 statt 61,9 kWh.</summary>
        [Fact]
        public void Das_Beispiel_des_Konzepts_rechnet_bei_sechzig_Prozent_rund_1_016()
        {
            double eta = Kesselkennlinie.Eta(0.6, 0.97, 1.05);
            Assert.Equal(1.05 + (0.97 - 1.05) * 0.3 / 0.7, eta, 12);
            Assert.Equal(1.016, eta, 3);
            Assert.Equal(59.1, 60.0 / eta, 1);
            Assert.Equal(61.9, 60.0 / 0.97, 1);
        }

        [Fact]
        public void Mit_eta30_gleich_eta100_ist_die_Kurve_fuer_jede_Laststufe_bitgleich_eta100()
        {
            foreach (double eta in new[] { 0.874, 0.876, 0.98, 1.0, 0.995 })
                for (int k = -5; k <= 130; k++)
                    Assert.Equal(eta, Kesselkennlinie.Eta(k / 100.0, eta, eta));
        }

        [Fact]
        public void Die_Kurve_ist_linear_zwischen_den_Pruefpunkten()
        {
            double a = Kesselkennlinie.Eta(0.4, 0.97, 1.05);
            double b = Kesselkennlinie.Eta(0.65, 0.97, 1.05);
            double c = Kesselkennlinie.Eta(0.9, 0.97, 1.05);
            Assert.Equal(b - a, c - b, 12);
            Assert.True(a > b && b > c);
        }

        /// <summary>
        /// <b>Keine Betriebsschwelle am Knick:</b> Ein β ein Bit vor oder hinter 0,3 verschiebt η
        /// höchstens um Rundungsreste — die Klemmung kippt keinen Zustand und braucht deshalb keinen
        /// Zahlenrand (<see cref="Rechenrand"/>).
        /// </summary>
        [Fact]
        public void Am_Knick_ist_die_Kurve_stetig()
        {
            double vor = Kesselkennlinie.Eta(Math.BitDecrement(0.3), 0.97, 1.05);
            double auf = Kesselkennlinie.Eta(0.3, 0.97, 1.05);
            double nach = Kesselkennlinie.Eta(Math.BitIncrement(0.3), 0.97, 1.05);
            Assert.Equal(auf, vor);
            Assert.True(Math.Abs(nach - auf) < 1e-15);

            double vorEins = Kesselkennlinie.Eta(Math.BitDecrement(1.0), 0.97, 1.05);
            Assert.True(Math.Abs(vorEins - 0.97) < 1e-15);
        }

        [Theory]
        [InlineData(10.0, 20.0, 0.5)]
        [InlineData(0.0, 20.0, 0.0)]
        [InlineData(-1.0, 20.0, 0.0)]
        [InlineData(25.0, 20.0, 1.0)]
        [InlineData(5.0, 0.0, 1.0)]
        public void Die_Laststufe_ist_Waerme_durch_Nennleistung_auf_null_bis_eins(double waerme, double nenn, double erwartet)
        {
            Assert.Equal(erwartet, Kesselkennlinie.Laststufe(waerme, nenn));
        }

        // =================================================================
        //  Bauart und Normvorgabe
        // =================================================================

        [Theory]
        [InlineData(true, "Brennwert-Kessel", KesselBauart.Brennwert)]
        [InlineData(true, "Standard-Kessel", KesselBauart.Brennwert)]
        [InlineData(false, "Standard-Kessel", KesselBauart.Standard)]
        [InlineData(false, "  standardkessel", KesselBauart.Standard)]
        [InlineData(false, "Brennwert-Kessel", KesselBauart.Niedertemperatur)]
        [InlineData(false, "Niedertemperatur-Kessel", KesselBauart.Niedertemperatur)]
        [InlineData(false, null, KesselBauart.Niedertemperatur)]
        public void Die_Bauart_folgt_dem_Brennwertschalter_und_der_VDI_Bauart_Standard(
            bool brennwert, string beschreibung, KesselBauart erwartet)
        {
            Assert.Equal(erwartet, Kesselkennlinie.Bauart(brennwert, beschreibung));
        }

        [Theory]
        [InlineData(0.874, KesselBauart.Brennwert, BRENNSTOFF_ERDGAS, 0.934)]
        [InlineData(1.06, KesselBauart.Brennwert, BRENNSTOFF_ERDGAS, 1.11)]      // Obergrenze Hs/Hi Gas
        [InlineData(1.12, KesselBauart.Brennwert, BRENNSTOFF_ERDGAS, 1.12)]      // nie unter eta100
        [InlineData(1.02, KesselBauart.Brennwert, BRENNSTOFF_HEIZOEL, 1.06)]     // Obergrenze Hs/Hi Öl
        [InlineData(1.00, KesselBauart.Brennwert, BRENNSTOFF_PELLETS, 1.06)]
        [InlineData(1.05, KesselBauart.Brennwert, BRENNSTOFF_PELLETS, 1.08)]     // Obergrenze Hs/Hi Holz
        [InlineData(0.97, KesselBauart.Brennwert, BRENNSTOFF_KOHLE, 1.0)]        // ohne Näherung: kein Gewinn über Hi
        [InlineData(0.90, KesselBauart.Niedertemperatur, BRENNSTOFF_ERDGAS, 0.90)]
        [InlineData(0.90, KesselBauart.Standard, BRENNSTOFF_HEIZOEL, 0.87)]
        public void Ein_leeres_eta30_nimmt_die_Normvorgabe_nach_Bauart(
            double eta100, KesselBauart bauart, int brennstoff, double erwartet)
        {
            Assert.Equal(erwartet, Kesselkennlinie.Eta30Vorgabe(eta100, bauart, brennstoff), 12);
            Assert.Equal(erwartet, Kesselkennlinie.Eta30Wirksam(null, eta100, bauart, brennstoff), 12);
        }

        [Fact]
        public void Die_Vorgabe_des_Niedertemperaturkessels_ist_bitgleich_eta100()
        {
            Assert.Equal(0.876, Kesselkennlinie.Eta30Vorgabe(0.876, KesselBauart.Niedertemperatur, BRENNSTOFF_ERDGAS));
        }

        [Theory]
        [InlineData(1.05, 1.05)]
        [InlineData(105.0, 1.05)]       // Prozentangabe
        [InlineData(0.0, 0.934)]        // nicht positiv: Vorgabe
        [InlineData(double.NaN, 0.934)]
        public void Ein_gepflegtes_eta30_geht_vor_der_Vorgabe(double gepflegt, double erwartet)
        {
            Assert.Equal(erwartet,
                         Kesselkennlinie.Eta30Wirksam(gepflegt, 0.874, KesselBauart.Brennwert, BRENNSTOFF_ERDGAS), 12);
        }

        [Fact]
        public void Der_Elektrokessel_rechnet_nie_mit_Kennlinie()
        {
            Assert.False(Kesselkennlinie.RechnetMitKennlinie(SimulationSPK.BRENNSTOFF_STROM));
            Assert.True(Kesselkennlinie.RechnetMitKennlinie(BRENNSTOFF_ERDGAS));
            Assert.True(Kesselkennlinie.RechnetMitKennlinie(0));
        }

        [Theory]
        [InlineData(1, Kesselkennlinie.HSHI_GAS)]
        [InlineData(14, Kesselkennlinie.HSHI_GAS)]
        [InlineData(6, Kesselkennlinie.HSHI_OEL)]
        [InlineData(22, Kesselkennlinie.HSHI_OEL)]
        [InlineData(12, Kesselkennlinie.HSHI_HOLZ)]
        [InlineData(15, Kesselkennlinie.HSHI_HOLZ)]
        [InlineData(10, Kesselkennlinie.HSHI_OHNE_BRENNWERT)]
        [InlineData(25, Kesselkennlinie.HSHI_OHNE_BRENNWERT)]
        public void Hs_durch_Hi_folgt_der_Brennstoffgruppe(int brennstoff, double erwartet)
        {
            Assert.Equal(erwartet, Kesselkennlinie.HsHi(brennstoff));
        }

        // =================================================================
        //  Der Lauf
        // =================================================================

        /// <summary>
        /// <b>1023: Brennwertkessel mit leerem η₃₀.</b> Er rechnet mit der Vorgabe 0,934; jede
        /// Laufstunde trägt η(β) der Kurve, der Brennstoff der Laufstunden ist Σ Q/η(β), und
        /// gegenüber η₁₀₀ spart die Teillast Brennstoff. Der Jahresverbrauch ist dieser Brennstoff
        /// plus der Bereitschaftsverlust.
        /// </summary>
        [Fact]
        public void Der_Brennwertkessel_von_1023_rechnet_je_Stunde_mit_der_Kurve_und_der_Vorgabe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_BRENNWERT, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.Equal(1, spk.KesselAnzahl);
            Assert.Equal(KesselBauart.Brennwert, spk.Bauart(0));
            Assert.True(spk.TeillastwirkungsgradIstVorgabe(0));
            Assert.Equal(ETA100_1023, spk.Nennwirkungsgrad(0), 12);
            Assert.Equal(ETA100_1023 + Kesselkennlinie.VORGABE_ZUSCHLAG_BRENNWERT, spk.Teillastwirkungsgrad(0), 12);

            double[] eta = spk.WirkungsgradStunden(0);
            double eta30 = spk.Teillastwirkungsgrad(0);
            double nenn = spk.Maximale_Kesselleistung_Spk;
            double brennstoff = 0, waerme = 0, mehr = 0;
            int laufstunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                double q = spk.Kesselleistung_stuendlich[h];
                if (eta[h] == 0) { Assert.True(q < Rechenrand.ABSOLUT); continue; }
                laufstunden++;
                Assert.Equal(Kesselkennlinie.Eta(Kesselkennlinie.Laststufe(q, nenn), ETA100_1023, eta30), eta[h]);
                Assert.InRange(eta[h], ETA100_1023, eta30);
                brennstoff += q / eta[h];
                waerme += q;
                mehr += q / eta[h] - q / ETA100_1023;
            }
            Assert.Equal(spk.Laufstunden_Spk[0], laufstunden);
            Assert.Equal(brennstoff, spk.BrennstoffBetrieb_KWh_Spk[0], 6);
            Assert.Equal(mehr, spk.TeillastMehrbrennstoff_KWh_Spk[0], 6);
            Assert.True(spk.TeillastMehrbrennstoff_KWh_Spk[0] < 0, "Die Teillast spart beim Brennwertkessel Brennstoff.");

            Assert.Equal((brennstoff + spk.Bereitschaftsverlust_KWh_Spk[0]) / 1000.0, spk.Kessel_Verbrauch_MWh_Spk[0], 9);
            Assert.Equal(waerme / brennstoff, spk.WirkungsgradBetrieb(0), 12);
            Assert.InRange(spk.WirkungsgradBetrieb(0), ETA100_1023, eta30);
            Assert.Equal(waerme / (laufstunden * nenn), spk.LaststufeMittel(0), 12);

            // Die Ergebnisseite ruft dieselben Zahlen.
            var erg = SimulationErgebnisCtrl.Heizkessel(laeufer.sim, laeufer.sim.simulation_Waermebedarf);
            Assert.True(erg.MitKennlinie);
            Assert.Equal(spk.TeillastMehrbrennstoff_KWh_Spk[0], erg.TeillastMehrbrennstoffKwh, 9);
            Assert.Equal(spk.WirkungsgradBetrieb(0) * 100.0, erg.WirkungsgradBetriebProzent, 9);
            var zeile = erg.Module.Single();
            Assert.True(zeile.MitKennlinie && zeile.TeillastVorgabe);
            Assert.Equal(eta30 * 100.0, zeile.TeillastwirkungsgradProzent, 9);
            Assert.Equal(spk.LaststufeMittel(0) * 100.0, zeile.LaststufeProzent, 9);

            // Das Laufprotokoll nennt die Stützwerte und die Herkunft von eta30.
            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_KENNLINIE_VORGABE.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(kopf) && h.Contains("0,934"));
        }

        /// <summary>
        /// <b>1007: Niedertemperaturkessel nach der Bauartregel</b> (Beschreibung „Brennwert…“, aber
        /// <c>Brennwert</c> = 0 in der Projektkopie): η₃₀ = η₁₀₀, jede Laufstunde rechnet bitgleich
        /// mit dem festen Wirkungsgrad, der Teillastbrennstoff ist genau 0.
        /// </summary>
        [Fact]
        public void Der_Niedertemperaturkessel_von_1007_rechnet_bitgleich_mit_dem_festen_Wirkungsgrad()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_NIEDERTEMPERATUR, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.Equal(KesselBauart.Niedertemperatur, spk.Bauart(0));
            double eta100 = spk.Nennwirkungsgrad(0);
            Assert.Equal(eta100, spk.Teillastwirkungsgrad(0));
            Assert.Equal(0.0, spk.TeillastMehrbrennstoff_KWh_Spk[0]);
            Assert.All(spk.WirkungsgradStunden(0).Where(e => e != 0), e => Assert.Equal(eta100, e));
            Assert.Equal(eta100, spk.WirkungsgradBetrieb(0), 12);
        }

        /// <summary>
        /// <b>Der Elektrokessel (1017) rechnet Wert für Wert wie ohne Kennlinie:</b> η₁₀₀ in jeder
        /// Laufstunde, kein Teillastbrennstoff, und die Ergebnistafel zeigt keine Kennliniengrößen.
        /// </summary>
        [Fact]
        public void Der_Elektrokessel_rechnet_in_jeder_Laufstunde_mit_eta100()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_ELEKTROKESSEL, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.True(spk.IstStromkessel(0));
            double eta100 = spk.Nennwirkungsgrad(0);
            Assert.Equal(eta100, spk.Teillastwirkungsgrad(0));
            Assert.False(spk.TeillastwirkungsgradIstVorgabe(0));
            Assert.Equal(0.0, spk.TeillastMehrbrennstoff_KWh_Spk[0]);
            Assert.All(spk.WirkungsgradStunden(0).Where(e => e != 0), e => Assert.Equal(eta100, e));

            double erwartetMwh = (spk.s_waerme_Gas_Spk[0] + spk.s_waerme_Oel_Spk[0]) / eta100 +
                                 spk.Bereitschaftsverlust_KWh_Spk[0] / 1000.0;
            Assert.Equal(erwartetMwh, spk.Kessel_Verbrauch_MWh_Spk[0], 9);

            var erg = SimulationErgebnisCtrl.Heizkessel(laeufer.sim, laeufer.sim.simulation_Waermebedarf);
            Assert.False(erg.MitKennlinie);
            Assert.False(erg.Module.Single().MitKennlinie);
        }

        /// <summary>
        /// <b>Ein gepflegtes η₃₀ der Projektkopie geht vor der Vorgabe</b> — gesetzt an 1023 auf 1,05
        /// (als Prozentwert 105), η₁₀₀ auf 0,97: Der Lauf rechnet mit 1,05, und der Teillastgewinn
        /// wächst gegenüber der Vorgabe.
        /// </summary>
        [Fact]
        public void Ein_gepflegtes_eta30_der_Projektkopie_geht_im_Lauf_vor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Heizkessel SET Wirkungsgrad_Gas = ?, Wirkungsgrad_Teillast30 = ? WHERE ID_Projekt = ?",
                new DbParam("@e100", 0.97), new DbParam("@e30", 105.0), new DbParam("@p", PROJEKT_BRENNWERT)));

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_BRENNWERT, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.False(spk.TeillastwirkungsgradIstVorgabe(0));
            Assert.Equal(1.05, spk.Teillastwirkungsgrad(0), 12);
            Assert.InRange(spk.WirkungsgradBetrieb(0), 0.97, 1.05);
            Assert.True(spk.TeillastMehrbrennstoff_KWh_Spk[0] < 0);

            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_KENNLINIE_GEPFLEGT.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(kopf) && h.Contains("1,050"));
        }
    }
}
