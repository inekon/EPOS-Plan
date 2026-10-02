using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Zwei stille Grenzen der Wärmepumpe werden benannt (Verbesserungen 29.09.2026, B7):
    /// <list type="number">
    /// <item>Die Modulgrenze: <see cref="SimulationWaermepumpe.MAX_WP"/> = 10 Module GELTEN,
    /// erst das elfte wird abgelehnt — mit einer Meldung im Fehlertext des Laufs statt
    /// eines stillen Abbruchs.</item>
    /// <item>Die unbrauchbare CSV-Quelle: Fehlt die Datei oder liefert sie keine 8 760
    /// Stundenwerte, rechnet die Anlage mit der Außentemperatur und sagt es als Warnung im
    /// Simulationsprotokoll.</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeModulgrenzeTests
    {
        [Fact]
        public void Zehn_Module_gelten_das_elfte_wird_benannt_abgelehnt()
        {
            Assert.Equal(10, SimulationWaermepumpe.MAX_WP);
            Assert.Null(SimulationWaermepumpe.ModulzahlPruefen(1));
            Assert.Null(SimulationWaermepumpe.ModulzahlPruefen(10));

            string grund = SimulationWaermepumpe.ModulzahlPruefen(11);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.SIMENG_WP_ZU_VIELE_MODULE, 11, 10), grund);
        }

        [Fact]
        public void Der_Lauf_bricht_beim_elften_Modul_mit_Fehlertext_ab()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();

            var wp = new SimulationWaermepumpe();
            for (int i = 1; i <= 11; i++) wp.wp_list.Add(i);

            Assert.False(wp.Vorbereiten_Zweikanalig());
            Assert.Equal(SimulationWaermepumpe.ModulzahlPruefen(11), wp.Fehlertext);
            Assert.Contains(protokoll.Fehler, f => f.Contains(wp.Fehlertext, StringComparison.Ordinal));
        }

        [Fact]
        public void Eine_unbrauchbare_CSV_Quelle_meldet_den_Rueckfall_auf_die_Aussenluft()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object id = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Type = ? ORDER BY ID LIMIT 1",
                new DbParam("@t", WizardItemClass.WP_TYP));
            Assert.True(id != null && id != DBNull.Value, "Die Testdatenbank führt keine Wärmepumpenanlage.");
            int idAnlage = Convert.ToInt32(id);

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET WQ_Typ = ?, WQ_CSV = ? WHERE ID = ?",
                new DbParam("@typ", WaermequelleClass.TYP_CSV),
                new DbParam("@csv", "/gibt/es/nicht/quelle.csv"),
                new DbParam("@id", idAnlage)));

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            double[] aussen = Enumerable.Repeat(5.0, 8760).ToArray();

            double[] quelle = WaermequelleClass.Quelltemperatur(idAnlage, 0, DbWerte.WP_BAUART_SOLE_WASSER, aussen);

            Assert.Same(aussen, quelle);
            string erwartet = string.Format(R.SIMENG_QUELLE_CSV_UNBRAUCHBAR, idAnlage, "/gibt/es/nicht/quelle.csv");
            Assert.Contains(protokoll.Warnungen, w => w.Contains(erwartet, StringComparison.Ordinal));
        }
    }
}
