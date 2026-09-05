using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Einheit der ausgewiesenen Prozesswaerme (Anwenderentscheid W9-O-3 vom
    /// 04.09.2026: „Prozesswaerme im W9-Weg ebenfalls ueber die Einheitenklasse
    /// umrechnen").
    ///
    /// <para><b>Was hier haengt.</b> <c>SimulationWaermebedarf.Waermebedarf_Prozess</c>
    /// wird von vier Wegen gesetzt und von EINER Anzeige gelesen. Die Anzeige
    /// (<c>BedarfErgebnisHuelle</c>) nennt fuer dieses Feld
    /// <c>Energieeinheit.MWh</c> als Quelleneinheit; der Bedarfsprofildialog (W9)
    /// setzte es dagegen als blanke Summe der Stundenwerte, also in kWh, und
    /// „Waermebedarf Prozess" stand dort um den Faktor 1000 zu gross. Seit dem
    /// Entscheid geht dieser Weg ueber
    /// <see cref="SimulationWaermebedarf.ProzesssummeUebernehmen"/>.</para>
    ///
    /// <para>Die Faelle kommen ohne Datenbank und ohne Oberflaeche aus: Die
    /// Stundenreihe wird direkt gesetzt, gerechnet wird nur die Uebernahme.
    /// Verglichen werden ZAHLEN, kein formatierter Text — die Klasse braucht
    /// deshalb keine gepinnte Oberflaechensprache.</para>
    /// </summary>
    public class ProzesssummeEinheitTests
    {
        private const int STUNDEN = 8760;

        /// <summary>Eine Simulation mit einer gefuellten Prozess-Stundenreihe [kWh].</summary>
        private static SimulationWaermebedarf MitProzessreihe(Func<int, float> wert)
        {
            var sim = new SimulationWaermebedarf();
            for (int h = 0; h < STUNDEN; h++) sim.prozesswerte[h] = wert(h);
            return sim;
        }

        /// <summary>
        /// Der Fall des Entscheids: 8760 Stunden zu je 1 kWh sind 8760 kWh und damit
        /// <b>8,76 MWh</b> — nicht 8760, wie es der W9-Weg bis hierher auswies.
        /// </summary>
        [Fact]
        public void Achttausendsiebenhundertsechzig_mal_ein_kWh_ergeben_8_76_MWh()
        {
            SimulationWaermebedarf sim = MitProzessreihe(h => 1f);

            sim.ProzesssummeUebernehmen();

            Assert.Equal(8.76, sim.Waermebedarf_Prozess, 10);

            // Und ausdruecklich NICHT der Bestandswert des W9-Weges.
            Assert.NotEqual(8760.0, sim.Waermebedarf_Prozess, 10);
        }

        /// <summary>
        /// Die Uebernahme ist genau die Einheitenumrechnung — kein eigener Teiler,
        /// der irgendwann auseinanderlaufen koennte.
        /// </summary>
        [Fact]
        public void Uebernahme_ist_die_Umrechnung_der_Einheitenklasse()
        {
            SimulationWaermebedarf sim = MitProzessreihe(h => (h % 24) * 0.25f);
            double summeKWh = sim.prozesswerte.Sum();

            sim.ProzesssummeUebernehmen();

            Assert.Equal(Energieeinheit.MWh.AusKWh(summeKWh), sim.Waermebedarf_Prozess);
        }

        /// <summary>Eine leere Reihe bleibt 0 — in jeder Einheit.</summary>
        [Fact]
        public void Leere_Reihe_ergibt_null()
        {
            var sim = new SimulationWaermebedarf();

            sim.ProzesssummeUebernehmen();

            Assert.Equal(0.0, sim.Waermebedarf_Prozess);
        }

        /// <summary>
        /// Die Gegenprobe zur Anzeige: Was hier herauskommt, liegt in MWh — der
        /// Rueckweg nach kWh trifft die Summe der Stundenwerte wieder.
        /// </summary>
        [Fact]
        public void Ergebnis_liegt_in_MWh_und_laesst_sich_zurueckrechnen()
        {
            SimulationWaermebedarf sim = MitProzessreihe(h => 2f);

            sim.ProzesssummeUebernehmen();

            Assert.Equal(17.52, sim.Waermebedarf_Prozess, 10);
            Assert.Equal(17520.0, Energieeinheit.KWh.AusMWh(sim.Waermebedarf_Prozess), 10);
        }
    }
}
