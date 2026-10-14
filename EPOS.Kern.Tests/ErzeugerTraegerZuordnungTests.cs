using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Speicherweg der Erzeugerdialoge ordnet den Träger seiner Anlagen dem Projekt zu</b> (Nachzug zu B2):
    /// Kessel- und BHKW-Dialog wählen einem Brenner ohne Träger den Gas- bzw. Ölträger seines Geräts vor und
    /// schreiben ihn als <c>ID_Carrier</c>; <c>WizardCtrl.Add_WP_Waermeerzeuger</c> legt dazu das Satzpaar in
    /// <c>energy_price</c>/<c>energy_project_settings</c> an, sonst fände die Wirtschaftlichkeit weder Preis noch
    /// Emission. Idempotent: ein zweites Speichern legt nichts an.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ErzeugerTraegerZuordnungTests
    {
        /// <summary>Projekt 1017: ein Kessel, Träger 54/58/69 zugeordnet, Erdgas E (63) nicht.</summary>
        private const int PROJEKT = 1017;
        private const int ERDGAS_E = 63;

        private static int Zuordnungen(int carrier)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                new DbParam("@p", PROJEKT), new DbParam("@c", carrier)));

        [Fact]
        public void Der_Kachelweg_ordnet_den_vorgewaehlten_Gastraeger_dem_Projekt_zu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.Equal(0, Zuordnungen(ERDGAS_E));

            List<WErzeugerModel> liste = WErzeugerCtrl.ModelleJeTyp(PROJEKT, WizardItemClass.KESSEL_TYP);
            Assert.NotEmpty(liste);
            foreach (WErzeugerModel m in liste) m.ID_Carrier = ERDGAS_E;

            var wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.KESSEL_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT, liste));

            Assert.Equal(1, Zuordnungen(ERDGAS_E));
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_price WHERE ID_Projekt = ? AND carrier_id = ?",
                new DbParam("@p", PROJEKT), new DbParam("@c", ERDGAS_E))));
        }

        [Fact]
        public void Die_Zuordnung_ist_idempotent_und_uebergeht_Anlagen_ohne_Traeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var liste = new List<WErzeugerModel>
            {
                new WErzeugerModel { ID_Carrier = ERDGAS_E },
                new WErzeugerModel { ID_Carrier = ERDGAS_E },
                new WErzeugerModel { ID_Carrier = 0 }
            };
            var wizard = new WizardCtrl();

            Assert.Equal(1, wizard.TraegerDerListeZuordnen(PROJEKT, liste));
            Assert.Equal(0, wizard.TraegerDerListeZuordnen(PROJEKT, liste));
            Assert.Equal(1, Zuordnungen(ERDGAS_E));
            Assert.Equal(0, wizard.TraegerDerListeZuordnen(0, liste));
        }
    }
}
