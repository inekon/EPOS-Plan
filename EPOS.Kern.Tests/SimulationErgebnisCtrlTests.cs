using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die acht DTO des <see cref="SimulationErgebnisCtrl"/> gegen einen echten Lauf von
    /// Projekt 1030 (iU9-W11a.3).
    ///
    /// <para><b>Was hier geprueft wird — und was nicht.</b> Nicht die Zahlen selbst: Sie
    /// haengen am Projektstand der Testdatenbank und waeren ein Golden-Test ohne Aussage
    /// ueber die Rechnung. Geprueft werden die INVARIANTEN, an denen ein Umbau
    /// zerbricht:</para>
    /// <list type="bullet">
    ///   <item>Jedes DTO-Feld deckt sich mit dem Ausdruck, den die Maske bisher hatte
    ///   (Zahlenabzug W11a.3) — bis auf die drei begruendeten Abweichungen.</item>
    ///   <item>Der Eigenanteil kommt aus den GETEILTEN Runner-Methoden, nicht aus einer
    ///   zweiten Abschrift: Restbedarf und Deckungsgrad muessen zwei Seiten derselben
    ///   Rechnung bleiben.</item>
    ///   <item>Die drei behobenen Befunde greifen: keine Division durch null (B15, B22),
    ///   die Ganglinie laeuft ueber alle 8 760 Stunden (B16).</item>
    /// </list>
    ///
    /// <para>Ohne Testdatenbank schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationErgebnisCtrlTests
    {
        private const int PROJEKT = 1030;

        private static SimulationRunner Lauf(int idProjekt = PROJEKT)
        {
            SimulationRunner laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(idProjekt, out fehler), "Lauf gescheitert: " + fehler);
            return laeufer;
        }

        // ---------------------------------------------------------------- Übersicht

        [Fact]
        public void Uebersicht_deckt_sich_mit_den_dreizehn_Feldern_der_Maske()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            var u = SimulationErgebnisCtrl.Uebersicht(l.sim, l.simulation_Waermebedarf, l.simulation_Strombedarf);

            Assert.Equal(l.simulation_Strombedarf.Strombedarf_gesamt, u.StrombedarfGesamtMwh);
            Assert.Equal(l.simulation_Waermebedarf.Waermebedarf_Gesamt, u.WaermebedarfGesamtMwh);
            Assert.Equal(l.sim.Restwaerme, u.RestwaermeMwh);
            Assert.Equal(l.sim.Reststrom, u.ReststromMwh);
            Assert.Equal(l.sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000.0, u.WpWaermeproduktionMwh);
            Assert.Equal(l.sim.simulation_wp.WP_Strombedarf_gesamt / 1000.0, u.WpStromverbrauchMwh);
            Assert.Equal(l.sim.simulation_spk.S_Waerme_spk, u.KesselWaermeproduktionMwh);
            Assert.Equal(l.sim.simulation_wp.Heizstab_gesamt / 1000.0, u.HeizstabStromverbrauchMwh);
            Assert.Equal(l.sim.simulation_spk.Stromverbrauch_Spk, u.KesselStromverbrauchMwh);
            Assert.Equal(l.sim.simulation_bhkw.Waermeproduktion_BHKW_MWh, u.BhkwWaermeproduktionMwh);
            Assert.Equal(l.sim.simulation_bhkw.Stromproduktion_BHKW_MWh, u.BhkwStromproduktionMwh);
            Assert.Equal(l.sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000.0, u.SolarWaermeproduktionMwh);
            Assert.Equal(l.sim.simulation_pv.Stromproduktion_gesamt / 1000.0, u.PvStromproduktionMwh);
        }

        /// <summary>
        /// W11-B35: Die Summe fuehrt den BHKW-Term — die Fassung des Navigators. Die
        /// Kesselwaerme ist dabei <c>S_Waerme_spk</c> und damit BITGLEICH der Summe
        /// <c>s_waerme_Gas_Spk + s_waerme_Oel_Spk</c>, die die Detailansicht bildete.
        /// </summary>
        [Fact]
        public void Uebersicht_zaehlt_das_BHKW_in_die_Waermesumme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            var u = SimulationErgebnisCtrl.Uebersicht(l.sim, l.simulation_Waermebedarf, l.simulation_Strombedarf);

            // Der Weg der Detailansicht ueber die Kesselliste ergibt denselben Wert.
            double ueberDieListe = 0;
            for (int i = 0; i < l.sim.simulation_spk.spk_list.Count; i++)
                ueberDieListe += l.sim.simulation_spk.s_waerme_Gas_Spk[i] +
                                 l.sim.simulation_spk.s_waerme_Oel_Spk[i];
            Assert.Equal(ueberDieListe, u.WaermeKesselMwh, 9);

            Assert.Equal(u.WaermeKesselMwh + u.WaermeWpMwh + u.WaermeHeizstabMwh +
                         u.WaermeSolarMwh + u.WaermeBhkwMwh, u.WaermeGesamtMwh, 9);
            Assert.Equal(l.sim.simulation_Waermebedarf.Waermebedarf_Gesamt - u.WaermeGesamtMwh,
                         u.RestwaermebedarfMwh, 9);
        }

        [Fact]
        public void Uebersicht_ohne_Lauf_liefert_null()
        {
            Assert.Null(SimulationErgebnisCtrl.Uebersicht(null, null, null));
        }

        // ---------------------------------------------------------------- Wärmepumpe

        /// <summary>
        /// Projekt 1030 fuehrt keine Waermepumpe — das DTO ist dann <c>null</c>, und die
        /// Rubrik bleibt leer statt die Zahlen des Vorlaufs zu zeigen.
        /// </summary>
        [Fact]
        public void Waermepumpe_ohne_WP_im_Lauf_liefert_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            if (l.sim.bSimulationWP) return;   // Projektstand geaendert - dann greift der Fall unten

            Assert.Null(SimulationErgebnisCtrl.Waermepumpe(l.sim, l.simulation_Waermebedarf));
        }

        /// <summary>
        /// Projekt 1007 fuehrt eine Waermepumpe. Geprueft werden die Invarianten und die
        /// beiden behobenen Befunde.
        /// </summary>
        [Fact]
        public void Waermepumpe_1007_haelt_Restbedarf_und_Deckung_zusammen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf(1007);
            var wp = SimulationErgebnisCtrl.Waermepumpe(l.sim, l.simulation_Waermebedarf);
            if (wp == null) return;

            double eigen = SimulationRunner.EigenanteilWpMwh(l.sim.simulation_wp);

            // Restbedarf und Deckung sind zwei Seiten DERSELBEN Rechnung.
            Assert.Equal(SimulationRunner.RestNachEigenanteil(wp.StufeneingangMwh, eigen),
                         wp.RestwaermeMwh, 9);
            Assert.Equal(SimulationRunner.DeckungProzent(eigen, l.simulation_Waermebedarf.Waermebedarf_Gesamt),
                         wp.DeckungProzent, 9);
            Assert.InRange(wp.DeckungProzent, 0.0, 100.0);
            Assert.True(wp.RestwaermeMwh >= 0.0);

            // W11-B15: keine Division durch null — der Wert ist endlich.
            Assert.False(double.IsInfinity(wp.Vollbenutzungsstunden));
            Assert.False(double.IsNaN(wp.Vollbenutzungsstunden));

            // W11-B16: das Maximum laeuft ueber die GANZE Ganglinie.
            double ueberAlles = l.sim.simulation_wp.waermerestbedarf_stuendlich.Max();
            Assert.Equal(ueberAlles, wp.MinSpkLeistungKw, 9);

            // Je Modul eine Zeile.
            Assert.Equal(l.sim.simulation_wp.wp_list.Count, wp.Module.Count);
        }

        /// <summary>
        /// W11-B15 unmittelbar: Ohne Modul in der Liste ist die Vollbenutzungsstundenzahl
        /// 0 und nicht ∞. Der Vorlaeufer teilte ungeprueft durch <c>wp_list.Count</c>.
        /// </summary>
        [Fact]
        public void Waermepumpe_ohne_Modul_meldet_null_Vollbenutzungsstunden()
        {
            var sim = new SimulationControl();
            sim.bSimulationWP = true;
            sim.simulation_wp.WP_Laufzeit = 1234.0;
            // wp_list bleibt leer

            var wp = SimulationErgebnisCtrl.Waermepumpe(sim, new SimulationWaermebedarf());

            Assert.NotNull(wp);
            Assert.Equal(0.0, wp.Vollbenutzungsstunden);
            Assert.False(double.IsInfinity(wp.Vollbenutzungsstunden));
        }

        /// <summary>
        /// Die Pufferzeilen sind BEWUSST unabhaengig von der Waermepumpe erreichbar — der
        /// Vorlaeufer fuellte die Rubrik ausserhalb von <c>if (sim.bSimulationWP)</c>.
        /// Projekt 1030 hat keine WP und trotzdem einen Senkenspeicher.
        /// </summary>
        [Fact]
        public void Pufferzeilen_gibt_es_auch_ohne_Waermepumpe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            var zeilen = SimulationErgebnisCtrl.Pufferzeilen(l.sim);

            Assert.Equal(l.sim.AlleSpeicher().Count, zeilen.Count);
            foreach (var z in zeilen)
            {
                Assert.False(string.IsNullOrEmpty(z.Bezeichner));
                Assert.True(z.KapazitaetKwh >= 0.0);
            }
        }

        /// <summary>
        /// Der Altausdruck der Pufferzeile: <c>Volumen · 1,16</c> — WOERTLICH, nicht ueber
        /// <c>ProjektPuffer.NutzbareKapazitaetKWh</c> (dort fehlten ΔT und die Division
        /// durch 1 000).
        /// </summary>
        [Fact]
        public void PufferVolumenKwh_bleibt_der_Altausdruck()
        {
            var sim = new SimulationControl();
            sim.simulation_wp.Volumen_Pufferspeicher = 1000.0;

            Assert.Equal(1160.0, SimulationErgebnisCtrl.PufferVolumenKwh(sim), 9);
            Assert.NotEqual(ProjektPuffer.NutzbareKapazitaetKWh(1000.0, 20.0),
                            SimulationErgebnisCtrl.PufferVolumenKwh(sim));
        }

        // ---------------------------------------------------------------- Heizkessel

        [Fact]
        public void Heizkessel_haelt_Restbedarf_und_Deckung_zusammen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            var hk = SimulationErgebnisCtrl.Heizkessel(l.sim, l.simulation_Waermebedarf);
            if (hk == null) return;

            double eigen = SimulationRunner.EigenanteilKesselMwh(l.sim.simulation_spk);

            Assert.Equal(l.sim.simulation_spk.Waermebedarf_gesamt, hk.StufeneingangMwh, 9);
            Assert.Equal(SimulationRunner.RestNachEigenanteil(hk.StufeneingangMwh, eigen),
                         hk.RestwaermeMwh, 9);
            Assert.Equal(SimulationRunner.DeckungProzent(eigen, l.simulation_Waermebedarf.Waermebedarf_Gesamt),
                         hk.DeckungProzent, 9);
            Assert.Equal(l.sim.simulation_spk.S_Waerme_spk, hk.WaermeproduktionMwh, 9);
            Assert.Equal(l.sim.simulation_spk.spk_list.Count, hk.Module.Count);
        }

        /// <summary>
        /// Die Sichtbarkeitsregel der zehn Brennstoffzeilen: Jahreswert &gt; 0 ODER der
        /// Brennstoff steht bei einem Kessel des Projekts.
        /// </summary>
        [Fact]
        public void BrennstoffZeileSichtbar_folgt_der_Oder_Regel()
        {
            var arten = new System.Collections.Generic.HashSet<int> { 3, 7 };

            Assert.True(SimulationErgebnisCtrl.BrennstoffZeileSichtbar(12.5, 99, arten));   // Wert > 0
            Assert.True(SimulationErgebnisCtrl.BrennstoffZeileSichtbar(0.0, 3, arten));     // Art gefuehrt
            Assert.False(SimulationErgebnisCtrl.BrennstoffZeileSichtbar(0.0, 99, arten));   // weder noch
            Assert.False(SimulationErgebnisCtrl.BrennstoffZeileSichtbar(0.0, 3, null));     // Arten unbekannt
        }

        // ---------------------------------------------------------------- Solarthermie

        [Fact]
        public void Solarthermie_ohne_Kollektor_im_Lauf_liefert_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            if (l.sim.bSimulationSolarthermie) return;

            Assert.Null(SimulationErgebnisCtrl.Solarthermie(l.sim, l.simulation_Waermebedarf));
        }

        /// <summary>
        /// Befund V0-O1 woertlich uebernommen: Der NENNER des Deckungsgrades ist der
        /// PROJEKTbedarf, der RESTBEDARF bleibt auf dem Stufeneingang.
        /// </summary>
        [Fact]
        public void Solarthermie_teilt_durch_den_Projektbedarf()
        {
            var sim = new SimulationControl();
            sim.bSimulationSolarthermie = true;
            sim.simulation_solarthermie.Waermeproduktion_gesamt = 40_000.0;   // kWh
            sim.simulation_solarthermie.Waermebedarf_gesamt = 100_000.0;      // kWh (Stufeneingang)

            var wb = new SimulationWaermebedarf();
            wb.Waermebedarf_Gesamt = 200.0f;                                  // MWh (Projekt)

            var st = SimulationErgebnisCtrl.Solarthermie(sim, wb);

            Assert.True(st.DeckungBekannt);
            Assert.Equal(40.0 / 200.0 * 100.0, st.DeckungProzent, 9);          // Projektbedarf
            Assert.Equal(60.0, st.RestwaermeMwh, 9);                           // Stufeneingang
        }

        [Fact]
        public void Solarthermie_ohne_Projektbedarf_meldet_die_Deckung_als_unbekannt()
        {
            var sim = new SimulationControl();
            sim.bSimulationSolarthermie = true;

            var st = SimulationErgebnisCtrl.Solarthermie(sim, new SimulationWaermebedarf());

            Assert.False(st.DeckungBekannt);
        }

        // ---------------------------------------------------------------- BHKW

        [Fact]
        public void Bhkw_haelt_Restbedarf_und_Deckung_zusammen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            var bh = SimulationErgebnisCtrl.Bhkw(l.sim, l.simulation_Waermebedarf, l.simulation_Strombedarf);

            double eigen = SimulationRunner.EigenanteilBhkwMwh(l.sim.simulation_bhkw);

            Assert.Equal(l.sim.simulation_bhkw.Waermebedarf_gesamt / 1000.0, bh.StufeneingangMwh, 9);
            Assert.Equal(SimulationRunner.RestNachEigenanteil(bh.StufeneingangMwh, eigen),
                         bh.RestwaermeMwh, 9);
            Assert.Equal(SimulationRunner.DeckungProzent(eigen, l.simulation_Waermebedarf.Waermebedarf_Gesamt),
                         bh.WaermedeckungProzent, 9);
            Assert.Equal(l.sim.simulation_bhkw.bhkw_list.Count, bh.Module.Count);
        }

        /// <summary>
        /// Anders als die drei anderen Erzeuger gibt es hier kein <c>null</c>: Der
        /// Vorlaeufer fuellte die BHKW-Felder ausserhalb jeder <c>if</c>-Bedingung.
        /// </summary>
        [Fact]
        public void Bhkw_liefert_auch_ohne_BHKW_ein_DTO()
        {
            var bh = SimulationErgebnisCtrl.Bhkw(new SimulationControl(),
                                                 new SimulationWaermebedarf(),
                                                 new SimulationStrombedarf());
            Assert.NotNull(bh);
            Assert.Equal(0.0, bh.WaermedeckungProzent);
            Assert.False(bh.VbhElektrischBekannt);
        }

        // ---------------------------------------------------------------- Photovoltaik

        /// <summary>
        /// W11-B22: Ohne Strombedarf ist der Deckungsgrad 0, nicht NaN. Genau das zeigte
        /// Projekt 1030 im Bestand — dort stand „NaN" im Feld.
        /// </summary>
        [Fact]
        public void Photovoltaik_ohne_Strombedarf_meldet_null_Prozent()
        {
            var pv = SimulationErgebnisCtrl.Photovoltaik(new SimulationControl());

            Assert.NotNull(pv);
            Assert.Equal(0.0, pv.DeckungProzent);
            Assert.False(double.IsNaN(pv.DeckungProzent));
        }

        [Fact]
        public void Photovoltaik_1030_meldet_keine_unbestimmte_Deckung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            var pv = SimulationErgebnisCtrl.Photovoltaik(l.sim);

            Assert.False(double.IsNaN(pv.DeckungProzent));
            Assert.False(double.IsInfinity(pv.DeckungProzent));
            Assert.Equal(l.sim.simulation_pv.Stromproduktion.Sum() / 1000.0, pv.StromproduktionMwh, 9);
            Assert.Equal(l.sim.Rest_Strombedarf_viertelstuendlich.Sum() / 4000.0, pv.ReststrombedarfMwh, 9);
        }

        // ---------------------------------------------------------------- Bedarf

        [Fact]
        public void Bedarf_liefert_Maxima_Summen_und_drei_Kanaele()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var l = Lauf();
            var b = SimulationErgebnisCtrl.Bedarf(l.simulation_Waermebedarf, l.simulation_Strombedarf);

            Assert.Equal(l.simulation_Waermebedarf.Waermebedarf_Max, b.WaermelastMaxKw);
            Assert.Equal(l.simulation_Waermebedarf.Waermebedarf_Gesamt, b.WaermebedarfGesamtMwh);
            Assert.Equal(l.simulation_Strombedarf.Strombedarf_Max, b.StrombedarfMaxKw);
            Assert.Equal(l.simulation_Strombedarf.Strombedarf_gesamt, b.StrombedarfGesamtMwh);
            Assert.Equal(Kanal.ANZAHL, b.KanalMwh.Count);
        }

        /// <summary>
        /// Der Warmwasseranteil wird je Stunde auf den ANLIEGENDEN Bedarf begrenzt — die
        /// Waermepumpe sieht in der Kaskade oft nur einen Teil des Gesamtbedarfs.
        /// </summary>
        [Fact]
        public void WarmwasserAnteil_begrenzt_auf_den_anliegenden_Bedarf()
        {
            var wb = new SimulationWaermebedarf();
            wb.brauchwasserwerte = new float[8760];
            wb.brauchwasserwerte[0] = 10f;
            wb.brauchwasserwerte[1] = 10f;
            wb.brauchwasserwerte[2] = -5f;

            float[] bedarf = new float[8760];
            bedarf[0] = 4f;      // begrenzt
            bedarf[1] = 40f;     // begrenzt nicht
            bedarf[2] = 40f;

            float[] ww = SimulationErgebnisCtrl.WarmwasserAnteil(wb, bedarf);

            Assert.Equal(8760, ww.Length);
            Assert.Equal(4f, ww[0]);
            Assert.Equal(10f, ww[1]);
            Assert.Equal(0f, ww[2]);   // negativer Quellwert wird 0
        }

        [Fact]
        public void WarmwasserAnteil_ohne_Brauchwasserwerte_bleibt_null()
        {
            float[] ww = SimulationErgebnisCtrl.WarmwasserAnteil(new SimulationWaermebedarf(), null);
            Assert.Equal(8760, ww.Length);
            Assert.All(ww, x => Assert.Equal(0f, x));
        }

        // ------------------------------------------------- die geteilten Runner-Methoden

        [Fact]
        public void RestNachEigenanteil_klemmt_auf_null()
        {
            Assert.Equal(3.0, SimulationRunner.RestNachEigenanteil(10.0, 7.0), 9);
            Assert.Equal(0.0, SimulationRunner.RestNachEigenanteil(10.0, 12.0), 9);
        }

        [Fact]
        public void DeckungProzent_klemmt_auf_null_bis_hundert()
        {
            Assert.Equal(50.0, SimulationRunner.DeckungProzent(50.0, 100.0), 9);
            Assert.Equal(100.0, SimulationRunner.DeckungProzent(150.0, 100.0), 9);
            Assert.Equal(0.0, SimulationRunner.DeckungProzent(-5.0, 100.0), 9);
            Assert.Equal(0.0, SimulationRunner.DeckungProzent(50.0, 0.0), 9);
        }

        [Fact]
        public void Eigenanteile_vertragen_null()
        {
            Assert.Equal(0.0, SimulationRunner.EigenanteilWpMwh(null));
            Assert.Equal(0.0, SimulationRunner.EigenanteilKesselMwh(null));
            Assert.Equal(0.0, SimulationRunner.EigenanteilSolarKwh(null));
            Assert.Equal(0.0, SimulationRunner.EigenanteilBhkwMwh(null));
        }
    }
}
