using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Erzeuger in Teillast</b> (Welle M4 der Entscheidungsvorlage Modellgrenzen): der Taktverlust der
    /// Wärmepumpe nach EN 14825 (WP1), die Teillastkennlinie des BHKW (BH1) und sein Takten mit
    /// Mindestlaufzeit und Anfahrverlust (BH2).
    ///
    /// <para><b>Ohne Datenbank</b> die Formeln an Stützwerten: COP_takt nach EN 14825 bei C_d = 0,9,
    /// η(β) bei β = 0,5 / 0,75 / 1, die Umkehrung Wärme ↔ Strom, die Starts- und Anfahrverlustregel gegen
    /// die Kesselregel. <b>Auf der Testkopie</b> die Läufe: 1039 (Wärmepumpe) und 1018 (BHKW) ohne Felder
    /// wie zuvor, mit Feldern in plausibler Richtung — der Wärmepumpenstrom steigt in der Übergangszeit,
    /// die Stromkennzahl des BHKW sinkt in Teillast, sein Brennstoff steigt beim Takten.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErzeugerTeillastTests : IDisposable
    {
        private const int PROJEKT_WP = 1039;
        private const int PROJEKT_KUEHLUNG = 1017;
        private const int PROJEKT_BHKW = 1018;
        private const int PROJEKT_BHKW_STROM = 1024;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  WP1 - die Formeln
        // =============================================================================

        /// <summary>Der Teillastfaktor nach EN 14825 an Stützwerten: f = CR / (C_d · CR + 1 − C_d).</summary>
        [Theory]
        [InlineData(0.5, 0.9, 0.5 / 0.55)]
        [InlineData(0.25, 0.9, 0.25 / 0.325)]
        [InlineData(0.2, 0.9, 0.2 / 0.28)]
        [InlineData(0.5, 1.0, 1.0)]
        [InlineData(0.5, 0.0, 0.5)]
        [InlineData(1.0, 0.9, 1.0)]
        [InlineData(1.5, 0.9, 1.0)]
        [InlineData(0.0, 0.9, 1.0)]
        public void Teillastfaktor_nach_EN_14825(double cr, double cd, double erwartet)
        {
            Assert.Equal(erwartet, Waermepumpentakt.Teillastfaktor(cr, cd), 12);
            Assert.Equal(4.0 * erwartet, Waermepumpentakt.CopTakt(4.0, cr, cd), 12);
        }

        /// <summary>Vorgabe C_d = 0,9; ein Wert außerhalb 0 … 1 nimmt die Vorgabe; ohne Mindestleistung keine Taktrechnung.</summary>
        [Fact]
        public void Vorgaben_und_Schalter_der_Taktrechnung()
        {
            Assert.Equal(0.9, Waermepumpentakt.CdWirksam(null));
            Assert.Equal(0.75, Waermepumpentakt.CdWirksam(0.75));
            Assert.Equal(0.9, Waermepumpentakt.CdWirksam(1.2));
            Assert.Equal(0.9, Waermepumpentakt.CdWirksam(-0.1));
            Assert.False(Waermepumpentakt.RechnetMitTakt(null));
            Assert.False(Waermepumpentakt.RechnetMitTakt(0));
            Assert.True(Waermepumpentakt.RechnetMitTakt(3.5));
        }

        /// <summary>
        /// Der Mehrstrom einer Taktstunde: Q = 4 kWh bei P_min = 8 kW ist CR = 0,5; mit 1 kWh Strom und
        /// C_d = 0,9 wird daraus 1 / (0,5/0,55) = 1,1 kWh. Über P_min kein Mehrstrom.
        /// </summary>
        [Fact]
        public void Mehrstrom_der_Taktstunde()
        {
            Assert.Equal(0.1, Waermepumpentakt.Mehrstrom(1.0, 4.0, 8.0, 0.9), 12);
            Assert.Equal(0.0, Waermepumpentakt.Mehrstrom(2.0, 8.0, 8.0, 0.9));
            Assert.Equal(0.0, Waermepumpentakt.Mehrstrom(2.0, 9.0, 8.0, 0.9));
            Assert.Equal(0.0, Waermepumpentakt.Mehrstrom(1.0, 4.0, 0.0, 0.9));
            Assert.Equal(0.0, Waermepumpentakt.Mehrstrom(1.0, 4.0, 8.0, 1.0), 12);
        }

        /// <summary>Die Starts einer Taktstunde sind die des Kessels mit 10 min Mindestlaufzeit.</summary>
        [Theory]
        [InlineData(0.5, 8.0)]
        [InlineData(1.4, 8.0)]
        [InlineData(4.0, 8.0)]
        [InlineData(7.9, 8.0)]
        public void Starts_der_Waermepumpe_folgen_der_Kesselregel(double q, double pmin)
        {
            Assert.True(Waermepumpentakt.Taktet(q, pmin));
            Assert.Equal(Kesselkennlinie.StartsImTakt(q, pmin, Kesselkennlinie.VORGABE_MINDESTLAUFZEIT_MIN),
                         Waermepumpentakt.StartsImTakt(q, pmin));
            Assert.InRange(Waermepumpentakt.StartsImTakt(q, pmin), 1, 6);
            Assert.False(Waermepumpentakt.Taktet(8.0, 8.0));
            Assert.False(Waermepumpentakt.Taktet(q, 0.0));
        }

        // =============================================================================
        //  BH1, BH2 - die Formeln
        // =============================================================================

        private static BhkwTeillast Modul(double? el50, double? th50, double grenze = 0.5,
                                          double? anfahr = null, int? laufzeit = null)
            => new BhkwTeillast(14.5, 30.8, 0.9216, el50, th50, grenze, anfahr, laufzeit);

        /// <summary>
        /// Das Modul taktet nur ab einem Mindestlauf: Q_min · t_min / 60 Wärme, mit leerer Mindestlaufzeit
        /// 10 min; auf der Schwelle ja, deutlich darunter nein. Der Strom eines Mindestlaufs folgt der
        /// Stromkennzahl der Untergrenze.
        /// </summary>
        [Fact]
        public void Takten_erst_ab_einem_Mindestlauf()
        {
            BhkwTeillast leer = Modul(null, null, 0.5, 0.5);
            double qmin = 0.5 * 30.8;
            Assert.Equal(qmin * 10 / 60.0, leer.MindestlaufWaermeKwh, 12);
            Assert.True(leer.NimmtMindestlaufWaerme(qmin * 10 / 60.0));
            Assert.False(leer.NimmtMindestlaufWaerme(qmin * 10 / 60.0 * 0.99));
            Assert.Equal(qmin * 10 / 60.0 / 30.8 * 14.5, leer.MindestlaufStromKwh, 12);

            BhkwTeillast lang = Modul(0.27, 0.62, 0.5, null, 30);
            Assert.Equal(lang.MindestwaermeKw * 0.5, lang.MindestlaufWaermeKwh, 12);
            Assert.True(lang.NimmtMindestlaufStrom(lang.MindestlaufStromKwh));
            Assert.False(lang.NimmtMindestlaufStrom(lang.MindestlaufStromKwh * 0.9));
        }

        /// <summary>η(β) trifft η₅₀ bei 0,5 und η₁₀₀ bei 1, liegt bei 0,75 in der Mitte und ist außerhalb geklemmt.</summary>
        [Fact]
        public void Kennlinie_an_den_Stuetzpunkten()
        {
            Assert.Equal(0.25, BhkwTeillast.Eta(0.5, 0.30, 0.25), 15);
            Assert.Equal(0.275, BhkwTeillast.Eta(0.75, 0.30, 0.25), 15);
            Assert.Equal(0.30, BhkwTeillast.Eta(1.0, 0.30, 0.25), 15);
            Assert.Equal(0.25, BhkwTeillast.Eta(0.3, 0.30, 0.25), 15);
            Assert.Equal(0.30, BhkwTeillast.Eta(1.2, 0.30, 0.25), 15);
            foreach (double beta in new[] { 0.1, 0.5, 0.731, 1.0 })
                Assert.Equal(0.30, BhkwTeillast.Eta(beta, 0.30, null));
        }

        /// <summary>
        /// Die Volllastwerte stammen aus Gesamtwirkungsgrad und Leistungsverhältnis: Die Volllast rechnet
        /// wie zuvor, und die Stromkennzahl sinkt in Teillast, wenn η_el fällt und η_th steigt.
        /// </summary>
        [Fact]
        public void Volllast_wie_zuvor_Stromkennzahl_sinkt_in_Teillast()
        {
            BhkwTeillast t = Modul(0.26, 0.66);
            Assert.True(t.MitKennlinie);
            Assert.False(t.MitTakten);
            Assert.Equal(0.9216 * 14.5 / 45.3, t.EtaEl100, 15);
            Assert.Equal(0.9216 * 30.8 / 45.3, t.EtaTh100, 15);
            Assert.Equal(30.8, t.WaermeAusStrom(14.5), 9);
            Assert.Equal(45.3 / 0.9216, t.Brennstoff(14.5, 30.8, false), 9);
            Assert.True(t.Stromkennzahl(0.5) < t.Stromkennzahl(0.75));
            Assert.True(t.Stromkennzahl(0.75) < t.Stromkennzahl(1.0));
            Assert.Equal(0.26 / 0.66, t.Stromkennzahl(0.5), 12);
        }

        /// <summary>Wärme und Strom sind zueinander die Umkehrung — unter, zwischen und über den Stützpunkten.</summary>
        [Theory]
        [InlineData(4.0)]
        [InlineData(8.0)]
        [InlineData(11.0)]
        [InlineData(14.5)]
        [InlineData(20.0)]
        public void Waerme_und_Strom_kehren_sich_um(double strom)
        {
            BhkwTeillast t = Modul(0.26, 0.66);
            double q = t.WaermeAusStrom(strom);
            Assert.Equal(strom, t.StromAusWaerme(q), 9);
        }

        /// <summary>Ohne Teillastwert rechnen Wärme und Strom bitgleich mit dem Dreisatz des Bestands.</summary>
        [Theory]
        [InlineData(3.3)]
        [InlineData(17.77)]
        [InlineData(30.8)]
        public void Ohne_Kennlinie_der_Dreisatz_des_Bestands(double waerme)
        {
            BhkwTeillast t = Modul(null, null, 0.35, 0.5, null);
            Assert.False(t.MitKennlinie);
            Assert.True(t.MitTakten);
            Assert.Equal(waerme / 30.8 * 14.5, t.StromAusWaerme(waerme));
            Assert.Equal(waerme / 14.5 * 30.8, t.WaermeAusStrom(waerme));
            Assert.Equal((waerme + 2.0) / 0.9216, t.Brennstoff(2.0, waerme, true));
        }

        /// <summary>
        /// Die Startregel ist die des Kessels: P_min ist die Wärme an der Untergrenze, die Mindestlaufzeit
        /// leer nimmt 10 min; ohne Anfahrverlust und Mindestlaufzeit taktet das Modul nicht.
        /// </summary>
        [Fact]
        public void Startregel_und_Anfahrverlust_wie_beim_Kessel()
        {
            BhkwTeillast t = Modul(null, null, 0.5, 0.8, 20);
            Assert.Equal(15.4, t.MindestwaermeKw, 12);
            Assert.Equal(20, t.MindestlaufzeitWirksam);
            Assert.Equal(0.8, t.AnfahrverlustWirksam);
            foreach (double q in new[] { 1.0, 5.1, 5.2, 10.0, 15.0 })
                Assert.Equal(Kesselkennlinie.StartsImTakt(q, 15.4, 20), t.StartsImTakt(q));
            Assert.Equal(3, t.StartsImTakt(15.0));
            Assert.Equal(1, t.StartsImTakt(5.0));

            BhkwTeillast nurLaufzeit = Modul(null, null, 0.5, null, 15);
            Assert.True(nurLaufzeit.MitTakten);
            Assert.Equal(0.0, nurLaufzeit.AnfahrverlustWirksam);

            BhkwTeillast nurAnfahr = Modul(null, null, 0.5, 0.5, null);
            Assert.Equal(Kesselkennlinie.VORGABE_MINDESTLAUFZEIT_MIN, nurAnfahr.MindestlaufzeitWirksam);

            Assert.False(Modul(null, null, 0.5).Aktiv);
            Assert.False(Modul(null, null, 0.0, 0.5, 15).MitTakten);
        }

        // =============================================================================
        //  Die Läufe auf der Testkopie
        // =============================================================================

        private static SimulationRunner Lauf(int projekt)
        {
            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(projekt, out fehler), "Lauf gescheitert: " + fehler);
            return laeufer;
        }

        private static readonly int[] MONATSTAGE = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        private static double[] Monate(double[] stunden)
        {
            var m = new double[12];
            int h = 0;
            for (int i = 0; i < 12; i++)
                for (int k = 0; k < MONATSTAGE[i] * 24; k++, h++)
                    m[i] += stunden[h];
            return m;
        }

        /// <summary>
        /// Projekt 1039 (35 kW) ohne Mindestleistung: keine Taktrechnung, kein Start, kein Mehrstrom. Mit
        /// 12 kW Mindestleistung bleibt die Wärme Stunde für Stunde gleich, der Strom steigt — in der
        /// Übergangszeit stärker als im Januar, in dem das Gerät nicht taktet —, und die Starts stehen im
        /// Ergebnis.
        /// </summary>
        [Fact]
        public void Waermepumpe_1039_taktet_in_der_Uebergangszeit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Die Wärmepumpe von 1039 rechnet an ihrer Erdsonde; deren Entzug (Wärme − Strom) nimmt den
            // Taktstrom mit und verschiebt so die Quelltemperatur. Die Taktregel selbst lässt die Wärme
            // unberührt — geprüft wird sie darum an der Außenluft (Quelle leer in der Arbeitskopie).
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET WQ_Typ = NULL WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT_WP)));

            SimulationWaermepumpe ohne = Lauf(PROJEKT_WP).sim.simulation_wp;
            Assert.False(ohne.RechnetMitTakt(0));
            Assert.Equal(0, ohne.Starts_WP[0]);
            Assert.Equal(0.0, ohne.Taktstrom_KWh_WP[0]);
            double[] waermeOhne = (double[])ohne.WP_Waermeproduktion_stuendlich.Clone();
            double[] stromOhne = (double[])ohne.WP_Strombedarf_stuendlich.Clone();
            double stromOhneKwh = ohne.WpStrombedarfGesamtKwh;

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_WP SET Mindestleistung_kW = 12 WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT_WP)));
            SimulationRunner mitLauf = Lauf(PROJEKT_WP);
            SimulationWaermepumpe mit = mitLauf.sim.simulation_wp;

            Assert.True(mit.RechnetMitTakt(0));
            Assert.True(mit.TaktCdIstVorgabe(0));
            Assert.Equal(0.9, mit.TaktCd(0));
            Assert.Equal(waermeOhne, mit.WP_Waermeproduktion_stuendlich);
            Assert.True(mit.Taktstunden_WP[0] > 0);
            Assert.True(mit.Starts_WP[0] > mit.Taktstunden_WP[0], "Eine Taktstunde zählt mindestens einen Start.");
            Assert.True(mit.Taktstrom_KWh_WP[0] > 0);
            Assert.Equal(stromOhneKwh + mit.Taktstrom_KWh_WP[0], mit.WpStrombedarfGesamtKwh, 6);
            for (int h = 0; h < 8760; h++) Assert.True(mit.WP_Strombedarf_stuendlich[h] >= stromOhne[h]);

            double[] mo = Monate(stromOhne), mm = Monate(mit.WP_Strombedarf_stuendlich);
            double januar = (mm[0] - mo[0]) / mo[0];
            double uebergang = (mm[3] + mm[4] + mm[8] + mm[9] - mo[3] - mo[4] - mo[8] - mo[9]) /
                               (mo[3] + mo[4] + mo[8] + mo[9]);
            Assert.True(uebergang > januar, "Übergangszeit " + uebergang + " gegen Januar " + januar);

            SimulationErgebnisCtrl.WaermepumpeErgebnis e = SimulationErgebnisCtrl.Waermepumpe(
                mitLauf.sim, mitLauf.sim.simulation_Waermebedarf);
            Assert.True(e.Module[0].MitTakt);
            Assert.Equal(mit.Starts_WP[0], e.Module[0].Starts);
            Assert.Equal(mit.Taktstrom_KWh_WP[0] / 1000.0, e.Module[0].TaktstromMwh, 9);
        }

        /// <summary>
        /// Projekt 1017 kühlt mit seiner Wärmepumpe: Mit Mindestleistung taktet sie auch im Kühlbetrieb —
        /// dieselbe Kälte, mehr Kältestrom, Starts am Kälteerzeuger.
        /// </summary>
        [Fact]
        public void Waermepumpe_1017_taktet_auch_im_Kuehlbetrieb()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Kaeltekaskade ohne = Lauf(PROJEKT_KUEHLUNG).sim.simulation_Waermebedarf.Kaelteseite.Kaskade;
            Assert.NotNull(ohne);
            Assert.Equal(0, ohne.Erzeuger[0].Starts);
            Assert.Equal(0.0, ohne.Erzeuger[0].Mindestanteil);
            double kaelte = ohne.DeckungGesamtKwh, strom = ohne.StromGesamtKwh;

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_WP SET Mindestleistung_kW = 12, Taktverlustfaktor_Cd = 0.85 WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT_KUEHLUNG)));
            Kaeltekaskade mit = Lauf(PROJEKT_KUEHLUNG).sim.simulation_Waermebedarf.Kaelteseite.Kaskade;
            Kaelteerzeuger e = mit.Erzeuger[0];

            Assert.Equal(12.0 / 35.0, e.Mindestanteil, 12);
            Assert.Equal(0.85, e.Cd);
            Assert.Equal(kaelte, mit.DeckungGesamtKwh, 6);
            Assert.True(e.Taktstunden > 0);
            Assert.True(e.Starts >= e.Taktstunden);
            Assert.True(e.TaktstromKwh > 0);
            Assert.True(mit.StromGesamtKwh > strom);
        }

        /// <summary>
        /// Projekt 1018 ohne Teillastfelder: kein Teillasteintrag, keine Starts, der Brennstoff ist
        /// (Wärme + Strom) / η wie zuvor.
        /// </summary>
        [Fact]
        public void Bhkw_1018_ohne_Felder_rechnet_wie_zuvor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationBHKW bh = Lauf(PROJEKT_BHKW).sim.simulation_bhkw;
            Assert.Null(bh.Teillast(0));
            Assert.Equal(0, bh.Starts_BHKW[0]);
            Assert.Equal(0.0, bh.Anfahrverlust_KWh_BHKW[0]);
            Assert.Equal(0.0, bh.TeillastMehrbrennstoff_KWh_BHKW[0]);
            Assert.Equal((bh.s_waerme_MWh[0] + bh.s_strom_MWh[0]) / 0.9216, bh.BruttoBHKWErzeugung, 9);
        }

        /// <summary>
        /// BH1 an 1018 (wärmegeführt): Mit η_el,50 = 0,26 und η_th,50 = 0,66 sinkt die Stromkennzahl der
        /// Jahressummen; die Wärme des Moduls bleibt im Rahmen, und der Mehrbrennstoff der Kennlinie
        /// steht im Brennstoff.
        /// </summary>
        [Fact]
        public void Bhkw_1018_Kennlinie_senkt_die_Stromkennzahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationBHKW ohne = Lauf(PROJEKT_BHKW).sim.simulation_bhkw;
            double skzOhne = ohne.s_strom_MWh[0] / ohne.s_waerme_MWh[0];

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_BHKW SET Wirkungsgrad_el_Teillast50 = 0.26, Wirkungsgrad_th_Teillast50 = 0.66 WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT_BHKW)));
            SimulationBHKW mit = Lauf(PROJEKT_BHKW).sim.simulation_bhkw;

            Assert.NotNull(mit.Teillast(0));
            Assert.True(mit.Teillast(0).MitKennlinie);
            double skzMit = mit.s_strom_MWh[0] / mit.s_waerme_MWh[0];
            Assert.True(skzMit < skzOhne, "Stromkennzahl " + skzMit + " gegen " + skzOhne);
            Assert.Equal(ohne.s_waerme_MWh[0], mit.s_waerme_MWh[0], 1);
            Assert.Equal(0, mit.Starts_BHKW[0]);
            double zuvor = (mit.s_waerme_MWh[0] + mit.s_strom_MWh[0]) / 0.9216;
            Assert.Equal(zuvor + mit.TeillastMehrbrennstoff_KWh_BHKW[0] / 1000.0, mit.BruttoBHKWErzeugung, 9);
        }

        /// <summary>
        /// BH2 in allen drei Betriebsarten (Untergrenze der Anlage 90 %): wärmegeführt an 1018, stromgeführt
        /// an 1017 (Strombedarf bis 7 kW gegen 10 kW elektrisch) und ohne Einspeisung an 1024 — 1018 führt
        /// keinen Strombedarf, ein stromseitiges Modul liefe dort nie; 1024 hat einen gleichbleibenden
        /// Strombedarf über der Nennleistung. Mit Anfahrverlust und Mindestlaufzeit läuft das Modul unter seiner Untergrenze im Takt statt
        /// aus — mehr Laufstunden, Starts und Anfahrverlust, mehr Brennstoff. Die Energieprobe (Produktion =
        /// Direktdeckung + Ladung + Überschuss) bleibt erfüllt.
        /// </summary>
        [Theory]
        [InlineData(0, PROJEKT_BHKW)]
        [InlineData(1, PROJEKT_KUEHLUNG)]
        [InlineData(2, PROJEKT_BHKW_STROM)]
        public void Bhkw_taktet_mit_Anfahrverlust(int betriebsart, int projekt)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Betriebsart = ? WHERE ID_Projekt = ?",
                                                  new DbParam("@b", betriebsart), new DbParam("@p", projekt)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Grenzleistung = 90 WHERE ID_Projekt = ? AND ID_BHKW IS NOT NULL",
                new DbParam("@p", projekt)));

            SimulationBHKW ohne = Lauf(projekt).sim.simulation_bhkw;
            double brennstoffOhne = ohne.BruttoBHKWErzeugung;
            int stundenOhne = ohne.waermeproduktion.Count(w => w > 0);

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_BHKW SET Anfahrverlust_kWh = 0.5, Mindestlaufzeit_min = 15 WHERE ID_Projekt = ?",
                new DbParam("@p", projekt)));
            SimulationRunner lauf = Lauf(projekt);
            SimulationBHKW mit = lauf.sim.simulation_bhkw;

            Assert.True(mit.Teillast(0).MitTakten);
            Assert.True(mit.Taktstunden_BHKW[0] > 0, "Betriebsart " + betriebsart + ": keine Taktstunde.");
            Assert.True(mit.Starts_BHKW[0] >= mit.Taktstunden_BHKW[0]);
            Assert.Equal(mit.Starts_BHKW[0] * 0.5, mit.Anfahrverlust_KWh_BHKW[0], 9);
            Assert.True(mit.waermeproduktion.Count(w => w > 0) > stundenOhne);
            Assert.True(mit.BruttoBHKWErzeugung > brennstoffOhne);

            SimulationErgebnisCtrl.BhkwErgebnis erg = SimulationErgebnisCtrl.Bhkw(
                lauf.sim, lauf.sim.simulation_Waermebedarf, lauf.sim.simulation_Strombedarf);
            Assert.True(erg.Module[0].MitTakten);
            Assert.Equal(mit.Starts_BHKW[0], erg.Module[0].Starts);
            Assert.Equal(mit.Anfahrverlust_KWh_BHKW[0], erg.Module[0].AnfahrverlustKwh);

            double produktion = mit.waermeproduktion.Sum();
            double summe = mit.DirektdeckungGesamtKwh + mit.SpeicherladungGesamtKwh + mit.WaermeueberschussKwh;
            Assert.True(Math.Abs(produktion - summe) <= 1.0, "Energieprobe: " + produktion + " gegen " + summe);
        }
    }
}
