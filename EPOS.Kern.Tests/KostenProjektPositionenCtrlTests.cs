using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// W5‑B‑6 (Windows-Abnahme 08.09.2026): „Positionen ohne Anlagenzuordnung löschen" nimmt
    /// auch verwaiste PFLICHTPOSITIONEN weg — die Pflichtzeilen einer LEBENDEN Anlage bleiben
    /// geschützt. Projekt 1026 der Testdatenbank führt an seiner Wärmepumpe drei
    /// Pflichtpositionen (Betrieb, H3).
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenProjektPositionenCtrlTests
    {
        private const int PROJEKT = 1026;
        private const int WAERMEPUMPE = 1;   // Tab_KostenKomponente.ID

        private static int Anzahl(string zusatz)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte AS w INNER JOIN Tab_Kostenfaktor AS k " +
                "ON w.StammID = k.StammID WHERE w.ProjektID = " + PROJEKT +
                " AND w.KomponentenID = " + WAERMEPUMPE + " " + zusatz);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        [Fact]
        public void Verwaiste_Pflichtpositionen_lassen_sich_als_ohne_Anlagenzuordnung_loeschen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(Anzahl("AND w.IstPflicht = 1") > 0);

            // Der Anlagentausch: Die Anlagenzeile der Waermepumpe faellt weg, alle ihre
            // Positionen verwaisen - genau der Zustand des Anwenderprojekts 1026.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET ID_Anlage = NULL WHERE ProjektID = ? AND KomponentenID = ?",
                new DbParam("@p", PROJEKT), new DbParam("@k", WAERMEPUMPE));
            int alle = Anzahl("");
            Assert.True(alle > 0);

            Assert.Equal(alle, KostenProjektPositionenCtrl.LoseLoeschen(PROJEKT, WAERMEPUMPE));
            Assert.Equal(0, Anzahl(""));
        }

        [Fact]
        public void Eine_Pflichtposition_einer_lebenden_Anlage_bleibt_geschuetzt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_ProjektWerte WHERE ProjektID = " + PROJEKT +
                " AND KomponentenID = " + WAERMEPUMPE + " AND IstPflicht = 1 AND ID_Anlage IN " +
                "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " + PROJEKT + ")");
            if (o == null || o == DBNull.Value) return;   // in dieser Datenbank keine lebende Pflichtzeile
            int id = Convert.ToInt32(o);

            Assert.False(KostenProjektPositionenCtrl.Loeschen(id));
            Assert.Equal(0, KostenProjektPositionenCtrl.LoseLoeschen(PROJEKT, WAERMEPUMPE));
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID = " + id)));
        }
    }
}
