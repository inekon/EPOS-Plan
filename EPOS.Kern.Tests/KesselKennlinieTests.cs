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
    /// Stunde für Stunde die Kurve bei der Laststufe der Stunde. Projekt 1007 trägt nach dem
    /// Nachzug des Brennwertkennzeichens (Etappe E2b) ebenfalls die Vorgabe des Brennwertkessels;
    /// ein Niedertemperaturkessel (1007 ohne Kennzeichen) und der Elektrokessel von 1017 rechnen
    /// Stunde für Stunde wie mit dem festen Wirkungsgrad.</para>
    ///
    /// <para><b>Die Brennwertkennlinie (Konzept 4.1 Punkte 3 bis 5, Etappe E3).</b>
    /// <see cref="Kesselkennlinie.EtaBrennwert"/> trifft beide Prüfpunkte (β = 0,3 bei 30 °C → η₃₀,
    /// β = 1 am Taupunkt → η₁₀₀), steigt unter 30 °C bis zum 1,2-Fachen des Gewinns, bleibt unter
    /// Hs/Hi und nie unter der trockenen Kurve; die Rücklaufkette lässt NaN durchfallen. Im Lauf
    /// rechnet allein 1050 mit ihr (Rückfall 50 °C); die Stufen Heizkreis (1047 mit AK1),
    /// Senkenspeicher und gepflegtes Paar hält je ein Fall an einer Arbeitskopie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselKennlinieTests : IDisposable
    {
        private const int PROJEKT_BRENNWERT = 1023;
        private const double ETA100_1023 = 0.874;
        private const int PROJEKT_NIEDERTEMPERATUR = 1007;
        private const int PROJEKT_ELEKTROKESSEL = 1017;
        private const int PROJEKT_REFERENZ = 1050;
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

        // =================================================================
        //  Die Brennwertkennlinie (Etappe E3)
        // =================================================================

        [Fact]
        public void Die_Brennwertkennlinie_trifft_beide_Pruefpunkte()
        {
            // β = 0,3 und 30 °C Rücklauf: η₃₀ (der Katalogwert ist dort gemessen).
            Assert.Equal(1.05, Kesselkennlinie.EtaBrennwert(0.3, 0.97, 1.05, 30.0, BRENNSTOFF_ERDGAS, out _), 12);
            Assert.Equal(1.05, Kesselkennlinie.EtaBrennwert(0.1, 0.97, 1.05, 30.0, BRENNSTOFF_ERDGAS, out _), 12);
            // β = 1 und Rücklauf am oder über dem Taupunkt: η₁₀₀, bitgenau.
            Assert.Equal(0.97, Kesselkennlinie.EtaBrennwert(1.0, 0.97, 1.05, 60.0, BRENNSTOFF_ERDGAS, out double tr));
            Assert.Equal(0.97, tr);
            Assert.Equal(0.97, Kesselkennlinie.EtaBrennwert(1.0, 0.97, 1.05, Kesselkennlinie.TAUPUNKT_GAS_C,
                                                            BRENNSTOFF_ERDGAS, out _));
        }

        [Fact]
        public void Das_trockene_eta30_nimmt_den_Kondensationsgewinn_heraus()
        {
            Assert.Equal(1.05 - 0.08, Kesselkennlinie.Eta30Trocken(1.05, BRENNSTOFF_ERDGAS));
            Assert.Equal(0.98 - 0.04, Kesselkennlinie.Eta30Trocken(0.98, BRENNSTOFF_HEIZOEL));
            Assert.Equal(0.9, Kesselkennlinie.Eta30Trocken(0.9, BRENNSTOFF_KOHLE));

            // Über dem Taupunkt rechnet die trockene Kurve: bei β = 0,3 also η₃₀ − Δ₃₀.
            Assert.Equal(1.05 - 0.08, Kesselkennlinie.EtaBrennwert(0.3, 0.97, 1.05, 70.0, BRENNSTOFF_ERDGAS, out _), 12);
        }

        [Fact]
        public void Heizoel_kondensiert_unter_47_Grad_mit_dem_halben_Gewinn()
        {
            Assert.Equal(47.0, Kesselkennlinie.Taupunkt(BRENNSTOFF_HEIZOEL));
            Assert.Equal(0.04, Kesselkennlinie.Kondensationsgewinn30(BRENNSTOFF_HEIZOEL));
            Assert.Equal(0.0, Kesselkennlinie.Kondensationsanteil(47.0, 47.0));
            Assert.Equal(0.98, Kesselkennlinie.EtaBrennwert(0.3, 0.92, 0.98, 30.0, BRENNSTOFF_HEIZOEL, out _), 12);
            double beiVierzig = Kesselkennlinie.EtaBrennwert(0.3, 0.92, 0.98, 40.0, BRENNSTOFF_HEIZOEL, out double tr);
            Assert.Equal(0.94 + 0.04 * 7.0 / 17.0, beiVierzig, 12);
            Assert.Equal(0.94, tr, 12);
            Assert.Equal(0.94, Kesselkennlinie.EtaBrennwert(0.3, 0.92, 0.98, 50.0, BRENNSTOFF_HEIZOEL, out _), 12);
        }

        [Theory]
        [InlineData(30.0, 1.0)]
        [InlineData(43.5, 0.5)]
        [InlineData(57.0, 0.0)]
        [InlineData(65.0, 0.0)]
        [InlineData(20.0, 1.2)]
        [InlineData(24.6, 1.2)]
        public void Der_Kondensationsanteil_ist_linear_und_geklemmt(double ruecklauf, double erwartet)
        {
            Assert.Equal(erwartet, Kesselkennlinie.Kondensationsanteil(ruecklauf, Kesselkennlinie.TAUPUNKT_GAS_C), 12);
        }

        [Fact]
        public void Ein_Ruecklauf_ohne_Wert_traegt_keinen_Gewinn()
        {
            Assert.Equal(0.0, Kesselkennlinie.Kondensationsanteil(double.NaN, 57.0));
            Assert.Equal(0.0, Kesselkennlinie.Kondensationsanteil(40.0, double.NaN));
            Assert.Equal(0.0, Kesselkennlinie.Kondensationsanteil(double.PositiveInfinity, 57.0));
        }

        [Fact]
        public void Die_Brennwertkennlinie_bleibt_unter_Hs_durch_Hi_und_nie_unter_der_trockenen_Kurve()
        {
            // Trocken 1,02 bei β = 0,3, Gewinn 0,08 · 1,2 = 0,096: 1,116 über 1,11 - gekappt.
            Assert.Equal(Kesselkennlinie.HSHI_GAS, Kesselkennlinie.EtaBrennwert(0.3, 1.05, 1.10, 20.0, BRENNSTOFF_ERDGAS, out _));
            // Liegt schon die trockene Kurve darüber, nimmt die Grenze nur den Gewinn zurück.
            Assert.Equal(1.12, Kesselkennlinie.EtaBrennwert(1.0, 1.12, 1.20, 20.0, BRENNSTOFF_ERDGAS, out _));
            // Heizöl: Grenze 1,06.
            Assert.Equal(Kesselkennlinie.HSHI_OEL, Kesselkennlinie.EtaBrennwert(0.3, 1.02, 1.06, 20.0, BRENNSTOFF_HEIZOEL, out _));
        }

        [Fact]
        public void Nur_der_Brennwertkessel_mit_gewaehlter_Kennlinie_rechnet_sie()
        {
            Assert.True(Kesselkennlinie.RechnetMitBrennwertkennlinie(true, true, BRENNSTOFF_ERDGAS));
            Assert.True(Kesselkennlinie.RechnetMitBrennwertkennlinie(true, true, BRENNSTOFF_HEIZOEL));
            Assert.True(Kesselkennlinie.RechnetMitBrennwertkennlinie(true, true, BRENNSTOFF_PELLETS));
            Assert.False(Kesselkennlinie.RechnetMitBrennwertkennlinie(true, false, BRENNSTOFF_ERDGAS));
            Assert.False(Kesselkennlinie.RechnetMitBrennwertkennlinie(false, true, BRENNSTOFF_ERDGAS));
            // Der Elektrokessel nie, und ein Brennstoff ohne Kondensationsgewinn auch nicht.
            Assert.False(Kesselkennlinie.RechnetMitBrennwertkennlinie(true, true, SimulationSPK.BRENNSTOFF_STROM));
            Assert.False(Kesselkennlinie.RechnetMitBrennwertkennlinie(true, true, BRENNSTOFF_KOHLE));
            Assert.True(double.IsNaN(Kesselkennlinie.Taupunkt(SimulationSPK.BRENNSTOFF_STROM)));
            Assert.Equal(50.0, Kesselkennlinie.Taupunkt(BRENNSTOFF_PELLETS));
            Assert.Equal(0.05, Kesselkennlinie.Kondensationsgewinn30(BRENNSTOFF_PELLETS));
        }

        [Fact]
        public void Die_Ruecklaufkette_nimmt_die_erste_belegte_Stufe()
        {
            Assert.Equal(40.0, Kesselkennlinie.Ruecklauf(40.0, 45.0, 35.0, out Ruecklaufstufe s1));
            Assert.Equal(Ruecklaufstufe.Heizkreis, s1);
            Assert.Equal(45.0, Kesselkennlinie.Ruecklauf(double.NaN, 45.0, 35.0, out Ruecklaufstufe s2));
            Assert.Equal(Ruecklaufstufe.Speicher, s2);
            Assert.Equal(35.0, Kesselkennlinie.Ruecklauf(double.NaN, double.NaN, 35.0, out Ruecklaufstufe s3));
            Assert.Equal(Ruecklaufstufe.Paar, s3);
            Assert.Equal(50.0, Kesselkennlinie.Ruecklauf(double.NaN, double.NaN, null, out Ruecklaufstufe s4));
            Assert.Equal(Ruecklaufstufe.Rueckfall, s4);
            Assert.Equal(50.0, Kesselkennlinie.Ruecklauf(double.PositiveInfinity, double.NaN, double.NaN, out _));
            Assert.Equal(SimulationControl.KESSEL_RUECKLAUF_RUECKFALL, Kesselkennlinie.RUECKLAUF_RUECKFALL_C);
        }

        [Fact]
        public void Der_Brennwertbetrieb_entscheidet_am_Taupunkt_mit_dem_Zahlenrand()
        {
            Assert.True(Kesselkennlinie.Brennwertbetrieb(56.9, 57.0));
            Assert.False(Kesselkennlinie.Brennwertbetrieb(57.0, 57.0));
            Assert.False(Kesselkennlinie.Brennwertbetrieb(57.0 - 1e-12, 57.0));
            Assert.False(Kesselkennlinie.Brennwertbetrieb(60.0, 57.0));
            Assert.False(Kesselkennlinie.Brennwertbetrieb(double.NaN, 57.0));
            Assert.False(Kesselkennlinie.Brennwertbetrieb(40.0, double.NaN));
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
        /// <b>Ein Niedertemperaturkessel</b> — der Kessel von 1007 ohne Brennwertkennzeichen in der
        /// Projektkopie: η₃₀ = η₁₀₀, jede Laufstunde rechnet bitgleich mit dem festen Wirkungsgrad, der
        /// Teillastbrennstoff ist genau 0.
        /// </summary>
        [Fact]
        public void Der_Niedertemperaturkessel_rechnet_bitgleich_mit_dem_festen_Wirkungsgrad()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Heizkessel SET Brennwert = 0 WHERE ID_Projekt = ?",
                                                  new DbParam("@p", PROJEKT_NIEDERTEMPERATUR)));

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
            Assert.False(spk.RechnetMitBrennwertkennlinie(0));
        }

        /// <summary>
        /// <b>1007 nach dem Nachzug (Etappe E2b):</b> Die Projektkopie trägt das Brennwertkennzeichen
        /// ihres Katalogsatzes; der Kessel rechnet als Brennwertkessel mit der Normvorgabe
        /// η₁₀₀ + 0,06 — ohne Brennwertkennlinie, also ohne Rücklauf und ohne Brennwertbrennstoff.
        /// </summary>
        [Fact]
        public void Nach_dem_Nachzug_rechnet_1007_als_Brennwertkessel_ohne_Brennwertkennlinie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_NIEDERTEMPERATUR, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.Equal(KesselBauart.Brennwert, spk.Bauart(0));
            Assert.True(spk.TeillastwirkungsgradIstVorgabe(0));
            Assert.Equal(spk.Nennwirkungsgrad(0) + Kesselkennlinie.VORGABE_ZUSCHLAG_BRENNWERT, spk.Teillastwirkungsgrad(0), 12);
            Assert.False(spk.RechnetMitBrennwertkennlinie(0));
            Assert.Null(spk.RuecklaufStunden(0));
            Assert.Equal(0.0, spk.BrennwertMehrbrennstoff_KWh_Spk[0]);
            Assert.Equal(0, spk.Brennwertstunden_Spk[0]);
            Assert.True(spk.TeillastMehrbrennstoff_KWh_Spk[0] < 0, "Die Teillast spart beim Brennwertkessel Brennstoff.");
            Assert.True(double.IsNaN(spk.RuecklaufMittel(0)));

            var erg = SimulationErgebnisCtrl.Heizkessel(laeufer.sim, laeufer.sim.simulation_Waermebedarf);
            Assert.False(erg.MitBrennwertkennlinie);
            Assert.False(erg.Module.Single().MitBrennwertkennlinie);
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
        /// <b>Das Referenzprojekt 1050 „Referenzprojekt Kesselkennlinie“</b> (Konzept 4.3, Entscheid
        /// F4): Kopie von 1023, der Projektkessel trägt die neutralen Kennlinienwerte — η₁₀₀ 0,97,
        /// η₃₀ 1,05, Brennwertkennlinie an, Mindestleistung 3,86 kW, Anfahrverlust 0,1 kWh,
        /// Mindestlaufzeit leer. Der Lauf rechnet mit dem gepflegten η₃₀ und liegt bei derselben
        /// Wärme wie 1023 rund 9 MWh Brennstoff darunter. Die Zahlen hält die Basis; dieser Fall
        /// hält die gesäten Zellen (Einfrierregel „gesäte Kesseldaten“).
        /// </summary>
        [Fact]
        public void Das_Referenzprojekt_1050_rechnet_mit_seiner_gepflegten_Kennlinie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            System.Data.DataTable k = DataRepository.GetDataTable(
                "SELECT Ptherm, Brennwert, Wirkungsgrad_Gas, Wirkungsgrad_Teillast30, Kennlinie_Brennwert, " +
                "Mindestleistung, Anfahrverlust_kWh, Mindestlaufzeit_min FROM Tab_Heizkessel WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT_REFERENZ));
            Assert.Equal(1, k.Rows.Count);
            System.Data.DataRow r = k.Rows[0];
            Assert.Equal(19.3, Convert.ToDouble(r["Ptherm"]), 9);
            Assert.Equal(1L, Convert.ToInt64(r["Brennwert"]));
            Assert.Equal(0.97, Convert.ToDouble(r["Wirkungsgrad_Gas"]), 9);
            Assert.Equal(1.05, Convert.ToDouble(r["Wirkungsgrad_Teillast30"]), 9);
            Assert.Equal(1L, Convert.ToInt64(r["Kennlinie_Brennwert"]));
            Assert.Equal(3.86, Convert.ToDouble(r["Mindestleistung"]), 9);
            Assert.Equal(0.1, Convert.ToDouble(r["Anfahrverlust_kWh"]), 9);
            Assert.Equal(DBNull.Value, r["Mindestlaufzeit_min"]);

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_REFERENZ, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.False(spk.TeillastwirkungsgradIstVorgabe(0));
            Assert.Equal(1.05, spk.Teillastwirkungsgrad(0), 12);
            Assert.Equal(0.97, spk.Nennwirkungsgrad(0), 12);

            // Etappe E3: Brennwertkennlinie mit dem Rückfall-Rücklauf 50 °C (kein Heizkreis der
            // Anlagenkopplung, kein Senkenspeicher, kein gepflegtes Paar). η₃₀,tr = 1,05 − 0,08 = η₁₀₀:
            // Die trockene Kurve ist flach, und jede Laufstunde rechnet mit 0,97 + 0,08 · 7/27.
            Assert.True(spk.RechnetMitBrennwertkennlinie(0));
            double erwartet = 0.97 + Kesselkennlinie.KONDENSATIONSGEWINN30_GAS * (57.0 - 50.0) / (57.0 - 30.0);
            double[] eta = spk.WirkungsgradStunden(0);
            double[] ruecklauf = spk.RuecklaufStunden(0);
            double nenn = spk.Maximale_Kesselleistung_Spk;
            double brennwert = 0;
            int laufstunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (eta[h] == 0) { Assert.Equal(0.0, ruecklauf[h]); continue; }
                laufstunden++;
                double q = spk.Kesselleistung_stuendlich[h];
                Assert.Equal(Kesselkennlinie.RUECKLAUF_RUECKFALL_C, ruecklauf[h]);
                Assert.Equal(Kesselkennlinie.EtaBrennwert(Kesselkennlinie.Laststufe(q, nenn), 0.97, 1.05, 50.0,
                                                          BRENNSTOFF_ERDGAS, out double trocken), eta[h]);
                Assert.Equal(erwartet, eta[h], 12);
                brennwert += q / eta[h] - q / trocken;
            }
            Assert.Equal(spk.Laufstunden_Spk[0], laufstunden);
            Assert.Equal(laufstunden, spk.RuecklaufStufenstunden(0, Ruecklaufstufe.Rueckfall));
            Assert.Equal(0, spk.RuecklaufStufenstunden(0, Ruecklaufstufe.Heizkreis) +
                            spk.RuecklaufStufenstunden(0, Ruecklaufstufe.Speicher) +
                            spk.RuecklaufStufenstunden(0, Ruecklaufstufe.Paar));
            Assert.Equal(laufstunden, spk.Brennwertstunden_Spk[0]);
            Assert.Equal(spk.WaermeBetriebKwh(0), spk.BrennwertWaerme_KWh_Spk[0], 9);
            Assert.Equal(brennwert, spk.BrennwertMehrbrennstoff_KWh_Spk[0], 6);
            Assert.True(spk.BrennwertMehrbrennstoff_KWh_Spk[0] < -1000, "Die Kondensation spart Brennstoff.");
            Assert.InRange(spk.TeillastMehrbrennstoff_KWh_Spk[0], -1e-6, 1e-6);
            Assert.Equal(50.0, spk.RuecklaufMittel(0), 9);
            Assert.Equal(erwartet, spk.WirkungsgradBetrieb(0), 9);
            Assert.InRange(spk.Kessel_Verbrauch_MWh_Spk[0], 80.6, 80.7);

            // Die Ergebnisseite ruft dieselben Zahlen.
            var erg = SimulationErgebnisCtrl.Heizkessel(laeufer.sim, laeufer.sim.simulation_Waermebedarf);
            Assert.True(erg.MitBrennwertkennlinie);
            Assert.Equal(laufstunden, erg.BrennwertLaufstunden);
            Assert.Equal(100.0, erg.BrennwertStundenProzent, 9);
            Assert.Equal(100.0, erg.BrennwertWaermeProzent, 9);
            Assert.Equal(50.0, erg.RuecklaufMittelC, 9);
            Assert.Equal(spk.BrennwertMehrbrennstoff_KWh_Spk[0], erg.BrennwertMehrbrennstoffKwh, 9);
            var zeile = erg.Module.Single();
            Assert.True(zeile.MitBrennwertkennlinie);
            Assert.Equal(50.0, zeile.RuecklaufMittelC, 9);
            Assert.Equal(100.0, zeile.BrennwertStundenProzent, 9);

            // Das Laufprotokoll nennt Stützwerte und Herkunft des Rücklaufs; über dem Taupunkt lag er nie.
            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_BRENNWERTKENNLINIE.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(kopf) && h.Contains("57") && h.Contains("0,970"));
            string betrieb = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_BRENNWERT_BETRIEB.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(betrieb) && h.Contains("50,0"));
            string ueber = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_BRENNWERT_UEBER_TAUPUNKT.Split('{')[0];
            Assert.DoesNotContain(laeufer.Protokoll.Hinweise, h => h.Contains(ueber));
        }

        /// <summary>
        /// <b>Stufe (c): das gepflegte Paar.</b> Mit Vorlauf/Rücklauf 55/35 °C am Projektkessel von 1050
        /// rechnet jede Laufstunde mit 35 °C Rücklauf — mehr Kondensationsgewinn als beim Rückfall.
        /// </summary>
        [Fact]
        public void Das_gepflegte_Paar_liefert_den_Ruecklauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Heizkessel SET Vorlauf = 55, Ruecklauf = 35 WHERE ID_Projekt = ?",
                                                  new DbParam("@p", PROJEKT_REFERENZ)));

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_REFERENZ, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.Equal(spk.Laufstunden_Spk[0], spk.RuecklaufStufenstunden(0, Ruecklaufstufe.Paar));
            Assert.All(spk.RuecklaufStunden(0).Where(t => t != 0), t => Assert.Equal(35.0, t));
            double erwartet = 0.97 + Kesselkennlinie.KONDENSATIONSGEWINN30_GAS * (57.0 - 35.0) / (57.0 - 30.0);
            Assert.Equal(erwartet, spk.WirkungsgradBetrieb(0), 9);
        }

        /// <summary>
        /// <b>Stufe (b): der Senkenspeicher.</b> Lädt der Kessel von 1050 den Puffer der Wärmepumpen
        /// (Rang-1-Senke „Puffer Heizung“), kommt der Rücklauf jeder Laufstunde aus dem Speicher — RL_eff,
        /// geschichtet die unterste Schicht, also im Band des Speichers.
        /// </summary>
        [Fact]
        public void Der_Senkenspeicher_liefert_den_Ruecklauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Z_AnlageSenke SET Ziel = 'PufferHeizung', ID_Puffer = 1054227 WHERE ID_Anlage = 16961 AND Rang = 1"));

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_REFERENZ, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.True(spk.Laufstunden_Spk[0] > 0);
            Assert.Equal(spk.Laufstunden_Spk[0], spk.RuecklaufStufenstunden(0, Ruecklaufstufe.Speicher));
            SimulationPufferspeicher sp = laeufer.sim.speicherRegistry[1054227];
            Assert.All(spk.RuecklaufStunden(0).Where(t => t != 0), t => Assert.InRange(t, sp.RL_eff, sp.VL_eff));
            if (!sp.Geschichtet)
                Assert.All(spk.RuecklaufStunden(0).Where(t => t != 0), t => Assert.Equal(sp.RL_eff, t));

            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_RUECKLAUF_SPEICHER.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(kopf));
        }

        /// <summary>
        /// <b>Stufe (a): der Heizkreis der Anlagenkopplung.</b> Projekt 1047 rechnet mit AK1; sein Kessel,
        /// an einer Arbeitskopie zum Gas-Brennwertkessel mit Brennwertkennlinie gemacht, nimmt in jeder
        /// Laufstunde mit gekoppeltem Bedarf den gerechneten Rücklauf des Heizkreises; eine Stunde ohne
        /// (NaN) fällt auf die nächste Stufe.
        /// </summary>
        [Fact]
        public void Der_Heizkreis_der_Anlagenkopplung_liefert_den_Ruecklauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Heizkessel SET Brennstoff = ?, Wirkungsgrad_Gas = 0.97, Brennwert = 1, Kennlinie_Brennwert = 1 " +
                "WHERE ID_Projekt = 1047", new DbParam("@b", BRENNSTOFF_ERDGAS)));

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(1047, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.True(spk.RechnetMitBrennwertkennlinie(0));
            double[] heizkreis = laeufer.sim.simulation_Waermebedarf.Heizkreis.RuecklaufC;
            double[] ruecklauf = spk.RuecklaufStunden(0);
            double[] eta = spk.WirkungsgradStunden(0);
            int ausHeizkreis = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (eta[h] == 0) continue;
                if (double.IsNaN(heizkreis[h])) { Assert.False(double.IsNaN(ruecklauf[h])); continue; }
                Assert.Equal(heizkreis[h], ruecklauf[h]);
                ausHeizkreis++;
            }
            Assert.True(ausHeizkreis > 0, "Keine Laufstunde mit gekoppeltem Bedarf.");
            Assert.Equal(ausHeizkreis, spk.RuecklaufStufenstunden(0, Ruecklaufstufe.Heizkreis));
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
