using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kältemaschine verwendet Strom</b> (Welle NB2): <see cref="ProjektEnergietraegerCtrl.BrauchtStromTraeger"/>
    /// zählt sie zur elektrischen Welt. Ein Projekt, dessen einziger Stromverbraucher eine Kältemaschine ist, fällt
    /// damit nicht unter „Strombedarf ohne Verwendung" (<see cref="ProjektEnergietraegerCtrl.StromOhneVerwendung"/>)
    /// und bekommt seinen Stromträger — mit leerem Kühlträger („wie Heizbetrieb") wie mit eigenem Kühlträger samt
    /// eigenem Zähler. Arbeitskopie des Referenzprojekts 1055 (Kältemaschine neben Wärmepumpe, Elektrokessel, BHKW und
    /// Stromspeicher), dem die übrigen Stromverwender genommen werden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineStromverwendungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1055;
        private const int KUEHLTRAEGER_EIGEN = 58;   // „Elektrische Energie 2" — ein zweiter ELECTRICITY-Träger

        /// <summary>Nimmt dem Projekt jeden Stromverwender außer der Kältemaschine (auch den Elektrokessel) und jede Stromzuordnung.</summary>
        private static void NurKaeltemaschine()
        {
            DataRepository.ExecuteNonQuery(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND (ID_WP > 0 OR ID_BHKW > 0 OR ID_PV > 0 OR ID_SP > 0 OR ID_Kessel > 0)",
                new DbParam("@p", PROJEKT));
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET Heizstab = 0, Hilfsenergie_Anteil = NULL WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT));
            DataRepository.ExecuteNonQuery(
                "DELETE FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] IN " +
                "(SELECT id FROM energy_carrier WHERE pricing_model = 'ELECTRICITY')",
                new DbParam("@p", PROJEKT));
        }

        private static void Kuehltraeger(int? traeger, bool? eigenerZaehler)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET Kuehl_ID_Carrier = ?, Kuehl_EigenerZaehler = ? " +
                "WHERE ID_Projekt = ? AND ID_Kaeltemaschine > 0",
                new DbParam("@t", traeger.HasValue ? (object)traeger.Value : DBNull.Value),
                new DbParam("@z", eigenerZaehler.HasValue ? (object)(eigenerZaehler.Value ? 1 : 0) : DBNull.Value),
                new DbParam("@p", PROJEKT));
        }

        [Fact]
        public void Das_Referenzprojekt_mit_Kaeltemaschine_braucht_weiter_Strom()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT));
            Assert.False(ProjektEnergietraegerCtrl.StromOhneVerwendung(PROJEKT, 100.0));
        }

        [Fact]
        public void Ohne_Kaeltemaschine_verwendet_das_Projekt_keinen_Strom()
        {
            if (!_db.Vorhanden) return;
            NurKaeltemaschine();
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET ID_Kaeltemaschine = NULL WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));

            // Gegenprobe: Ohne Kältemaschine bleibt nur der Puffer — er verwendet keinen Strom.
            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT));
            Assert.True(ProjektEnergietraegerCtrl.StromOhneVerwendung(PROJEKT, 100.0));
            Assert.Equal(0, ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT));
        }

        [Fact]
        public void Ein_Projekt_nur_mit_Kaeltemaschine_braucht_Strom_und_bekommt_den_Projekttraeger()
        {
            if (!_db.Vorhanden) return;
            NurKaeltemaschine();
            Kuehltraeger(null, null);   // leerer Kühlträger: „wie Heizbetrieb"
            Assert.Equal(0, StrompreisZerlegungCtrl.StromCarrierId(PROJEKT));

            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT));
            Assert.False(ProjektEnergietraegerCtrl.StromOhneVerwendung(PROJEKT, 100.0));

            int id = ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT);
            Assert.True(id > 0);
            Assert.Equal(ProjektEnergietraegerCtrl.StandardStromTraeger(PROJEKT), id);
            Assert.Equal(id, StrompreisZerlegungCtrl.StromCarrierId(PROJEKT));
            object gruppe = DataRepository.ExecuteScalar(
                "SELECT group_code FROM energy_carrier WHERE id = ?", new DbParam("@id", id));
            var gruppen = EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT);
            Assert.NotNull(gruppen);
            Assert.Contains(Convert.ToString(gruppe).Trim(), gruppen);
        }

        [Fact]
        public void Eine_Kaeltemaschine_mit_eigenem_Kuehltraeger_und_Zaehler_verwendet_ebenfalls_Strom()
        {
            if (!_db.Vorhanden) return;
            NurKaeltemaschine();
            Kuehltraeger(KUEHLTRAEGER_EIGEN, true);

            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT));
            Assert.False(ProjektEnergietraegerCtrl.StromOhneVerwendung(PROJEKT, 100.0));

            // Der Projektträger bleibt der Auslieferungsträger — der Kühlträger der Anlage wählt ihn nicht.
            int id = ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT);
            Assert.True(id > 0);
            Assert.Equal(ProjektEnergietraegerCtrl.StandardStromTraeger(PROJEKT), id);
        }
    }
}
