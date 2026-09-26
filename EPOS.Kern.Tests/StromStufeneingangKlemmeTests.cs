using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>E28 (#535): Ein BHKW-Überschuss ist weder PV-Überschuss noch ein negativer
    /// Strombedarf</b> (Befund N7 aus E27). Die Kaskade zieht die BHKW-Erzeugung ungeklemmt ab
    /// (E27). Stufen dahinter sehen deshalb in Überschussstunden einen negativen Rest.
    /// <list type="bullet">
    /// <item>Der Vorab-Überschuss für den PV-Modus der Wärmepumpe klemmt den Bedarf je Stunde bei 0
    /// (<see cref="SimulationControl.PvUeberschussVorab"/>, Entscheid E28‑Q1 a), dieselbe Regel
    /// wie V1 in <see cref="SimulationPV"/>.</item>
    /// <item>Die Kesselzeile klemmt ihren Stromeingang je Stunde (E28‑Q2 a), an allen drei Wegen:
    /// Vektorstufe, Mitglied der Speicherstufe, N3-Nachzug hinter der Wärmepumpe.</item>
    /// <item>Die PV-Zeile klemmt ihren Strombedarf (E28‑Q3 a, Befund N8).</item>
    /// </list>
    ///
    /// <para>Kein Referenzprojekt erreicht den Vorab-Überschuss mit BHKW-Überschuss: Es gibt keine
    /// Wärmepumpe im PV-Modus und kein Projekt mit BHKW und Photovoltaik. Ein Gegenstück in der
    /// Testdatenbank bräuchte eine WP-Anlage samt Gerät, Senke und Puffer in einem BHKW-Projekt
    /// mit Überschuss; das wäre ein größerer Umbau. Die Regel steht deshalb als Theorie; die
    /// Anker unten halten die Bitgleichheit der Projekte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class StromStufeneingangKlemmeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =====================================================================
        //  Stelle 1: Vorab-Überschuss des PV-Modus
        // =====================================================================

        /// <summary>
        /// Eine Stunde: Potenzial und Bedarf [kW] → PV-Überschuss [kW]. Ein negativer Bedarf ist
        /// BHKW-Überschuss und zählt als 0 — (10, −5) ergibt 10, nicht 15; (0, −5) ergibt 0,
        /// nicht 5 (nachts kein PV-Überschuss).
        /// </summary>
        [Theory]
        [InlineData(10.0, 4.0, 6.0)]
        [InlineData(10.0, -5.0, 10.0)]
        [InlineData(0.0, -5.0, 0.0)]
        [InlineData(3.0, 8.0, 0.0)]
        [InlineData(0.0, 0.0, 0.0)]
        public void Der_PV_Ueberschuss_zaehlt_keinen_BHKW_Ueberschuss(double potenzial, double bedarf,
                                                                       double erwartet)
        {
            double[] pot = new double[8760];
            double[] bed = new double[8760];
            pot[17] = potenzial;
            bed[17] = bedarf;

            double[] ueberschuss = SimulationControl.PvUeberschussVorab(pot, bed);

            Assert.Equal(8760, ueberschuss.Length);
            Assert.Equal(erwartet, ueberschuss[17]);
            Assert.Equal(erwartet, ueberschuss.Sum());
            Assert.Equal(bedarf, bed[17]);   // Eingabe unberührt
        }

        [Fact]
        public void Ohne_negativen_Bedarf_ist_der_Vorab_Ueberschuss_bitgleich_zur_alten_Formel()
        {
            var zufall = new Random(535);
            double[] pot = new double[8760];
            double[] bed = new double[8760];
            for (int i = 0; i < 8760; i++)
            {
                pot[i] = zufall.NextDouble() * 20.0;
                bed[i] = zufall.NextDouble() * 20.0;
            }
            bed[5] = 0.0;
            bed[6] = -0.0;

            double[] neu = SimulationControl.PvUeberschussVorab(pot, bed);
            for (int i = 0; i < 8760; i++)
            {
                double rest = pot[i] - bed[i];   // die Formel vor E28
                double alt = rest > 0 ? rest : 0;
                Assert.Equal(BitConverter.DoubleToInt64Bits(alt), BitConverter.DoubleToInt64Bits(neu[i]));
            }
        }

        [Fact]
        public void Ein_kurzer_Bedarfsvektor_zaehlt_die_fehlenden_Stunden_als_0()
        {
            double[] pot = Enumerable.Repeat(2.0, 8760).ToArray();
            double[] ueberschuss = SimulationControl.PvUeberschussVorab(pot, new[] { 5.0, -1.0 });
            Assert.Equal(0.0, ueberschuss[0]);
            Assert.Equal(2.0, ueberschuss[1]);
            Assert.Equal(2.0, ueberschuss[8759]);
        }

        // =====================================================================
        //  Stelle 2: Stromeingang der Kesselzeile (dieselbe Klemme je Stunde)
        // =====================================================================

        [Fact]
        public void Der_Kessel_Stromeingang_klemmt_je_Stunde_und_ohne_Ueberschuss_bleibt_er_dasselbe_Array()
        {
            // Stufeneingang nach einem BHKW [kW je Stunde]: zwei Überschussstunden.
            double[] eingang = { 12.0, -3.0, 5.0, -0.5 };
            double[] geklemmt = SimulationControl.NetzbezugGeklemmt(eingang);
            Assert.Equal(17.0, geklemmt.Sum());      // vorher 13,5 (verrechnet)
            Assert.Equal(-3.0, eingang[1]);          // Eingabe unberührt

            double[] ohne = { 12.0, 0.0, 5.0 };
            Assert.Same(ohne, SimulationControl.NetzbezugGeklemmt(ohne));
        }

        // =====================================================================
        //  Anker an der Testdatenbank: die Kesselzeile bleibt bitgleich
        // =====================================================================

        /// <summary>
        /// 1017 und 1047 haben den Kessel hinter dem BHKW (1047 über den N3-Nachzug hinter der
        /// Wärmepumpe), aber keine Überschussstunde. 1018 und 1030 haben Überschussstunden; ihr
        /// Kessel rechnet in derselben Speicherstufe wie das BHKW und sieht den Eingang vor dem
        /// BHKW. Alle vier bleiben, was R20 führt.
        /// </summary>
        [Theory]
        [InlineData(1017, 635.2)]
        [InlineData(1018, 0.0)]
        [InlineData(1030, 4790.09)]
        [InlineData(1047, 640.19)]
        public void Die_Kesselzeile_der_BHKW_Projekte_bleibt_bitgleich(int idProjekt, double strombedarfMwh)
        {
            if (!_db.Vorhanden) return;

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(idProjekt, out string fehler), "Lauf gescheitert: " + fehler);
            ErgebnisModel e = SimulationRunner.BaueErgebnis(idProjekt, r.simulation_Waermebedarf,
                                                            r.simulation_Strombedarf, r.sim);

            Assert.Equal(strombedarfMwh, Math.Round(e.Heizkessel.Strombedarf, 2));
            Assert.True(e.Heizkessel.Strombedarf >= 0.0);
            Assert.Equal(e.Heizkessel.Strombedarf,
                         SimulationErgebnisCtrl.Heizkessel(r.sim, r.simulation_Waermebedarf).StrombedarfMwh);
        }

        // =====================================================================
        //  N8: die PV-Zeile hinter einem BHKW-Überschuss
        // =====================================================================

        /// <summary>
        /// 1018 (BHKW ohne Strombedarf, ganzjährig Überschuss) mit der PV-Anlage von 1040 auf
        /// der Arbeitskopie: Der Stufeneingang der PV ist in allen Überschussstunden negativ.
        /// Vor E28 zeigte die PV-Zeile −27,46 MWh Strombedarf, jetzt 0. Die Erzeugung und der
        /// BHKW-Überschuss bleiben (V1).
        /// </summary>
        [Fact]
        public void Die_PV_Zeile_hinter_einem_BHKW_Ueberschuss_hat_keinen_negativen_Strombedarf()
        {
            if (!_db.Vorhanden) return;
            Kopiere("Tab_Energieanlagen", "ID = 14742");
            Kopiere("Tab_PV", "ID_Projekt = 1040");
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Einstellungen SET Tool_5 = 'Photovoltaik' WHERE ID_Projekt = 1018");

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(1018, out string fehler), "Lauf gescheitert: " + fehler);
            Assert.True(r.sim.bSimulationPV);
            Assert.True(r.sim.simulation_pv.Strombedarf.Any(x => x < 0),
                        "Vorbedingung: der Stufeneingang der PV ist in Überschussstunden negativ");

            ErgebnisModel e = SimulationRunner.BaueErgebnis(1018, r.simulation_Waermebedarf,
                                                            r.simulation_Strombedarf, r.sim);
            Assert.Equal(0.0, e.Photovoltaik.Strombedarf);
            Assert.Equal(0.0, SimulationErgebnisCtrl.Photovoltaik(r.sim).StrombedarfMwh);
            Assert.Equal(27457.510347756746, r.sim.simulation_pv.BhkwUeberschussGesamtKwh, 6);
        }

        private static void Kopiere(string tabelle, string bedingung)
        {
            List<string> spalten = DataRepository.SpaltenVonTabelle(tabelle)
                .Where(s => !string.Equals(s, "ID", StringComparison.OrdinalIgnoreCase)).ToList();
            string ziel = string.Join(", ", spalten.Select(s => "[" + s + "]"));
            string quelle = string.Join(", ", spalten.Select(s =>
                string.Equals(s, "ID_Projekt", StringComparison.OrdinalIgnoreCase) ? "1018" : "[" + s + "]"));
            Assert.True(DataRepository.ExecuteSQL("INSERT INTO " + tabelle + " (" + ziel + ") SELECT " +
                                                  quelle + " FROM " + tabelle + " WHERE " + bedingung));
        }
    }
}
