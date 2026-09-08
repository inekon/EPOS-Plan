using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ET‑2 (Anwenderbefund 08.09.2026): Ein Projekt mit elektrischer Welt (Wärmepumpe,
    /// Photovoltaik, Stromspeicher, Heizstab) bekommt seinen Stromträger zugeordnet — nicht
    /// mehr nur beim Speichern des Assistenten. Projekt 1026 der Testdatenbank ist das Projekt
    /// des Befunds: drei elektrische Anlagen, keine Stromzuordnung.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektEnergietraegerCtrlTests
    {
        private const int PROJEKT_OHNE_STROM = 1026;
        private const int PROJEKT_MIT_STROM = 1017;   // „Strom Variante" (54) ist zugeordnet

        private static int Stromzeilen(int projekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_project_settings s " +
                "INNER JOIN energy_carrier ec ON ec.id = s.[ID_Energieträger] " +
                "WHERE s.ID_Projekt = ? AND ec.pricing_model = 'ELECTRICITY'",
                new DbParam("@p", projekt));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        [Fact]
        public void Ein_Projekt_mit_Waermepumpe_ohne_Stromzuordnung_bekommt_seinen_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT_OHNE_STROM));
            Assert.Equal(0, Stromzeilen(PROJEKT_OHNE_STROM));

            int id = ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT_OHNE_STROM);

            Assert.True(id > 0);
            Assert.Equal(1, Stromzeilen(PROJEKT_OHNE_STROM));
            Assert.Equal(id, StromAufschlagCtrl.StromCarrierId(PROJEKT_OHNE_STROM));
            // Der Auslieferungstraeger des Katalogs (BK1: Code "Elektrische Energie").
            Assert.Equal(ProjektEnergietraegerCtrl.StandardStromTraeger(PROJEKT_OHNE_STROM), id);
        }

        [Fact]
        public void Ein_zweiter_Aufruf_legt_keine_zweite_Zeile_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int erster = ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT_OHNE_STROM);
            int zweiter = ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT_OHNE_STROM);

            Assert.True(erster > 0);
            Assert.Equal(erster, zweiter);
            Assert.Equal(1, Stromzeilen(PROJEKT_OHNE_STROM));
        }

        [Fact]
        public void Eine_vorhandene_Zuordnung_wird_nicht_ueberstimmt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int vorher = Stromzeilen(PROJEKT_MIT_STROM);
            Assert.True(vorher > 0);

            int id = ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT_MIT_STROM);

            Assert.Equal(StromAufschlagCtrl.StromCarrierId(PROJEKT_MIT_STROM), id);
            Assert.Equal(vorher, Stromzeilen(PROJEKT_MIT_STROM));
        }

        [Fact]
        public void Ohne_elektrische_Welt_geschieht_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object o = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Projekt WHERE ID NOT IN (SELECT ID_Projekt FROM Tab_Energieanlagen " +
                "WHERE ID_WP > 0 OR ID_PV > 0 OR ID_SP > 0 OR Heizstab = 1) ORDER BY ID");
            if (o == null || o == DBNull.Value) return;
            int projekt = Convert.ToInt32(o);

            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(projekt));
            Assert.Equal(0, ProjektEnergietraegerCtrl.StromTraegerSicherstellen(projekt));
        }

        // =============================================================================
        //  ET-5 (Anwenderentscheid 08.09.2026): der an der Anlage gewaehlte Traeger
        // =============================================================================

        private const int VARIANTE_STROM = 58;   // "Elektrische Energie 2" - ein zweiter ELECTRICITY-Traeger

        [Fact]
        public void Der_an_der_Waermepumpe_gewaehlte_Stromtraeger_gewinnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int standard = ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT_OHNE_STROM);
            Assert.True(standard > 0 && standard != VARIANTE_STROM);
            Assert.True(new WizardCtrl().TraegerSatzAnlegen(PROJEKT_OHNE_STROM, VARIANTE_STROM));
            // Ohne Anlagenwahl gilt die bisherige Regel: die kleinste Id der Zuordnungen.
            Assert.Equal(Math.Min(standard, VARIANTE_STROM), StromAufschlagCtrl.StromCarrierId(PROJEKT_OHNE_STROM));

            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET ID_Carrier = ? WHERE ID_Projekt = ? AND ID_WP > 0",
                new DbParam("@c", VARIANTE_STROM), new DbParam("@p", PROJEKT_OHNE_STROM));

            Assert.Equal(VARIANTE_STROM, ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_OHNE_STROM));
            Assert.Equal(VARIANTE_STROM, StromAufschlagCtrl.StromCarrierId(PROJEKT_OHNE_STROM));
            Assert.Equal(VARIANTE_STROM, Emissionsquelle.StromTraeger(PROJEKT_OHNE_STROM));
            Assert.Contains(ProjektEnergietraegerCtrl.Verwendete(PROJEKT_OHNE_STROM),
                            v => v.CarrierId == VARIANTE_STROM
                                 && v.BeitraegerText.Contains(DbWerte.ERZEUGER_WAERMEPUMPE));
        }

        [Fact]
        public void Ein_nicht_zugeordneter_Anlagentraeger_zaehlt_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int standard = ProjektEnergietraegerCtrl.StromTraegerSicherstellen(PROJEKT_OHNE_STROM);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET ID_Carrier = ? WHERE ID_Projekt = ? AND ID_WP > 0",
                new DbParam("@c", VARIANTE_STROM), new DbParam("@p", PROJEKT_OHNE_STROM));

            // 58 ist dem Projekt NICHT zugeordnet - die Wahl greift erst mit der Zuordnung.
            Assert.Equal(0, ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_OHNE_STROM));
            Assert.Equal(standard, StromAufschlagCtrl.StromCarrierId(PROJEKT_OHNE_STROM));
        }
    }
}
