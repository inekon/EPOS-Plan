using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Prüfprojekt 1053 „Test BHKW mit PV ohne Kaskade"</b> — ein BHKW mit Photovoltaik ohne
    /// Speicherflotte, ohne Stromspeicher und ohne Einspeisegrenze.
    ///
    /// <para>Ohne Flotte hat die BHKW-Einspeisung zwei Quellen: Mit Photovoltaik liest der Zeitreihensatz
    /// die Viertelstundenbilanz der PV-Stufe (<see cref="SimulationPV.BhkwUeberschuss"/>), der BHKW-Reiter
    /// und seine Kennzahl die Stundenformel des KWK-Splits
    /// (<see cref="SimulationControl.BhkwEinspeisungDesLaufs"/>). 1053 erzeugt den Fall, in dem beide
    /// nebeneinander stehen. Angelegt von <c>Referenzlaeufe/Skripte/pruefprojekt_1053_bhkw_pv.cs</c>
    /// (Kopie von 1018, nur das BHKW in der Kaskade, 60 PV-Module, Stromverbraucher Hotel 50 MWh/a).</para>
    ///
    /// <para><b>Was hier steht:</b> die Form des Projekts und dass der Lauf BHKW-Überschuss und
    /// BHKW-Einspeisung erzeugt. <b>Nicht</b> hier steht, welche der beiden Reihen gilt — das entscheidet
    /// der Anwender; kein Test schreibt eine davon fest. Kein Referenzprojekt: 1053 steht in keiner Basis
    /// und in keiner Projektliste der CI.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BhkwPvPruefprojektTests : IDisposable
    {
        internal const int PROJEKT = 1053;
        internal const string NAME = "Test BHKW mit PV ohne Kaskade";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static object Wert(string sql, params object[] w)
            => DataRepository.ExecuteScalar(sql, w.Select(x => new DbParam("?", x ?? DBNull.Value)).ToArray());

        private static long Zahl(string sql, params object[] w)
        {
            object o = Wert(sql, w);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        [Fact]
        public void Projekt_1053_hat_das_BHKW_allein_in_der_Kaskade_PV_und_keinen_Speicher()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(NAME, Convert.ToString(Wert("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", PROJEKT)));

            DataTable e = DataRepository.GetDataTable(
                "SELECT Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6, Einspeisegrenze_Wert FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", PROJEKT));
            DataRow r = Assert.Single(e.Rows.Cast<DataRow>());
            string T(string s) => r[s] == DBNull.Value ? "" : Convert.ToString(r[s]);
            Assert.Equal(DbWerte.ERZEUGER_BHKW, T("Tool_1"));
            Assert.Equal("", T("Tool_2"));
            Assert.Equal("", T("Tool_3"));
            Assert.Equal("", T("Tool_4"));
            Assert.Equal(DbWerte.ERZEUGER_PHOTOVOLTAIK, T("Tool_5"));
            Assert.Equal("", T("Tool_6"));
            Assert.Equal(DBNull.Value, r["Einspeisegrenze_Wert"]);

            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_BHKW > 0", PROJEKT));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen a JOIN Tab_PV p ON p.ID = a.ID_PV " +
                                 "WHERE a.ID_Projekt = ? AND p.ID_Projekt = ? AND a.PV_Leistung > 0", PROJEKT, PROJEKT));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Stromspeicher WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", PROJEKT));
        }

        [Fact]
        public void Der_Lauf_von_1053_erzeugt_BHKW_Ueberschuss_mit_PV_ohne_Flotte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            SimulationControl sim = lauf.sim;

            Assert.True(sim.bSimulationBHKW);
            Assert.True(sim.bSimulationPV);
            Assert.Null(sim.Speicherflottennetzbilanz);
            Assert.True(sim.simulation_pv.BhkwUeberschussGesamtKwh > 0);
            Assert.True(sim.simulation_pv.Ueberschuss.Sum() > 0);

            double[] einspeisung = sim.BhkwEinspeisungDesLaufs();
            Assert.NotNull(einspeisung);
            Assert.True(einspeisung.Sum() > 0);
        }
    }
}
