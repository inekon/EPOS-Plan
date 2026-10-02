using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Welle M2 Solarthermie am Referenzprojekt 1049</b> (Entscheidungsvorlage Modellgrenzen
    /// ST1 bis ST6), je auf einer Arbeitskopie der Testdatenbank: Bezugsfläche (ST6) über das
    /// Potenzial der Felder.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SolarthermieModellgrenzenTests : IDisposable
    {
        private const int PROJEKT = 1049;

        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>ID der Anlagenzeile des Kollektorfelds von 1049.</summary>
        private static int FeldAnlage()
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Solar IS NOT NULL ORDER BY ID",
                new DbParam("@p", PROJEKT)));

        /// <summary>ID der Projektkopie des Kollektorsatzes von 1049.</summary>
        private static int Kollektorsatz()
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_Solar FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@id", FeldAnlage())));

        private static SimulationSolarthermie Vorbereitet()
        {
            SimulationProtokoll.NeuStarten();
            var st = new SimulationSolarthermie();
            Assert.True(st.Vorbereiten_Zweikanalig(PROJEKT, null));
            Assert.Equal(1, st.FelderAnzahl);
            return st;
        }

        // =================================================================
        // ST6 - Bezugsfläche
        // =================================================================

        /// <summary>
        /// Brutto rechnet mit der Modulfläche: Das Potenzial wächst um Brutto/Apertur — umgekehrt liegt
        /// eine Rechnung mit der Apertur um Apertur/Brutto unter der mit Brutto bezogenen Kennwerten.
        /// Ohne Modulfläche bleibt die Apertur, und der Lauf sagt es.
        /// </summary>
        [Fact]
        public void Bezugsflaeche_Brutto_skaliert_das_Potenzial_mit_Brutto_zu_Apertur()
        {
            if (!_db.Vorhanden) return;

            SimulationSolarthermie apertur = Vorbereitet();
            double sumApertur = apertur.PotenzialSumme(0);
            Assert.Equal(2.35 * 35, apertur.FeldFlaeche(0), 9);
            Assert.True(sumApertur > 0);

            int satz = Kollektorsatz();
            DataRepository.ExecuteNonQuery("UPDATE Tab_Solarkollektoren SET Bezugsflaeche = 'brutto' WHERE ID = ?",
                                           new DbParam("@id", satz));

            SimulationSolarthermie ohneBrutto = Vorbereitet();
            Assert.Equal(sumApertur, ohneBrutto.PotenzialSumme(0));
            Assert.Contains(SimulationProtokoll.Aktuell.Warnungen,
                            t => t.Contains("Bruttofläche bezogen") && t.Contains("Aperturfläche"));

            DataRepository.ExecuteNonQuery("UPDATE Tab_Solarkollektoren SET Modulflaeche = 2.51 WHERE ID = ?",
                                           new DbParam("@id", satz));
            SimulationSolarthermie brutto = Vorbereitet();
            Assert.Equal(2.51 * 35, brutto.FeldFlaeche(0), 9);
            Assert.Equal(2.35 / 2.51, sumApertur / brutto.PotenzialSumme(0), 12);
        }

        /// <summary>Die Projektkopie trägt die Bezugsfläche des Katalogsatzes.</summary>
        [Fact]
        public void Die_Projektkopie_uebernimmt_die_Bezugsflaeche()
        {
            if (!_db.Vorhanden) return;

            int stamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Solarkollektoren_STAMM WHERE Bezeichner = ?",
                new DbParam("@b", "auroTHERM plus VFK 155/2 H")));
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Solarkollektoren_STAMM SET Bezugsflaeche = 'brutto', Modulflaeche = 2.51 WHERE ID = ?",
                new DbParam("@id", stamm));

            int kopie = new SolarkollektorenCtrl().CopyFromStamm(stamm, PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal("brutto", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezugsflaeche FROM Tab_Solarkollektoren WHERE ID = ?", new DbParam("@id", kopie))));

            var gelesen = new SolarkollektorenCtrl();
            gelesen.ReadSingle(kopie);
            Assert.Equal("brutto", gelesen.m_Bezugsflaeche);

            SolarkollektorenModel m = SolarkollektorenStammCtrl.ReadById(stamm);
            Assert.Equal("brutto", m.m_Bezugsflaeche);
        }
    }
}
