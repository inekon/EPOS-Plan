using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis zum Anwenderbefund vom 22.09.2026</b>: Das Änderungsdatum des
    /// Projekts (<c>Tab_Projekt.Aenderungsdatum</c>) wurde nur auf EINIGEN Wegen
    /// gesetzt.
    ///
    /// <para><b>Die Lage im Bestand.</b> Den Handgriff rief die Windows-Startseite je
    /// Kachel — und nur vier der zwölf Kacheln riefen ihn: Heizkessel, Photovoltaik,
    /// Solarthermie und Standardlastprofil. Wärmepumpe, BHKW, Stromspeicher und
    /// Pufferspeicher riefen ihn nicht. Folge: <c>BerichtsDatenSammler.ErmittleStatus</c>
    /// meldete den Simulationsstand nach genau diesen Änderungen als aktuell, und die
    /// Wirtschaftlichkeit rechnete stillschweigend auf einem Lauf weiter, der die neue
    /// Anlage gar nicht kannte.</para>
    ///
    /// <para><b>Die Marke sitzt jetzt im SCHREIBWEG</b> (<c>WizardCtrl</c>,
    /// <c>WErzeugerCtrl</c>) — also auf jeder Kachel, im Assistenten und auf jedem
    /// anderen Weg dorthin. Die Kachelaufrufe sind damit entfallen: eine Wahrheit.</para>
    ///
    /// <para>Jeder Fall legt seine EIGENE Arbeitskopie an — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektAenderungsdatumTests
    {
        private const int PROJEKT = 1030;

        /// <summary>Ein Datum, das jeder Schreibweg überbieten muss.</summary>
        private static readonly DateTime ALT = new DateTime(2000, 1, 1);

        /// <summary>
        /// DIE LÜCKE DES BEFUNDS. Der Weg der Anlagenkacheln ist
        /// <c>Del_Projekt_Waermeerzeuger</c> + <c>Add_WP_Waermeerzeuger</c> — er ging
        /// an <c>WErzeugerCtrl</c> vorbei und setzte das Datum deshalb nie.
        /// </summary>
        [Fact]
        public void Der_Anlagen_Schreibweg_markiert_das_Projekt_als_geaendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DatumZuruecksetzen();

            Assert.True(new WizardCtrl().Del_Projekt_Waermeerzeuger(
                            PROJEKT, WizardItemClass.WP_TYP));

            Assert.True(Aenderungsdatum() > ALT,
                        "Del_Projekt_Waermeerzeuger hat das Änderungsdatum nicht gesetzt.");
        }

        /// <summary>
        /// Die Stromganglinie ist der zweite Weg, den die Kachel „Stromganglinie"
        /// fährt — auch er schrieb das Datum bis hierher nicht selbst.
        /// </summary>
        [Fact]
        public void Del_Stromganglinie_markiert_das_Projekt_als_geaendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DatumZuruecksetzen();

            Assert.True(new WizardCtrl().Del_Stromganglinie(PROJEKT));

            Assert.True(Aenderungsdatum() > ALT,
                        "Del_Stromganglinie hat das Änderungsdatum nicht gesetzt.");
        }

        /// <summary>
        /// <c>WErzeugerCtrl.Delete</c> lässt ALLE Anlagenzeilen des Projekts fallen —
        /// die größte denkbare Änderung der Simulationseingaben.
        /// </summary>
        [Fact]
        public void WErzeugerCtrl_Delete_markiert_das_Projekt_als_geaendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DatumZuruecksetzen();

            var ctrl = new WErzeugerCtrl { ID_Projekt = PROJEKT };
            Assert.True(ctrl.Delete());

            Assert.True(Aenderungsdatum() > ALT,
                        "WErzeugerCtrl.Delete hat das Änderungsdatum nicht gesetzt.");
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        private static void DatumZuruecksetzen()
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Projekt SET Aenderungsdatum = ? WHERE ID = ?",
                new DbParam("@d", DbParamTyp.Date) { Wert = ALT },
                new DbParam("@id", PROJEKT));

            Assert.Equal(ALT, Aenderungsdatum());
        }

        /// <summary>Gelesen über den EINEN Leser der Anwendung, nicht über eine zweite
        /// Datumszerlegung.</summary>
        private static DateTime Aenderungsdatum()
        {
            var p = new ProjektCtrl();
            p.ReadSingle(PROJEKT);
            return p.m_Aenderungsdatum;
        }
    }
}
