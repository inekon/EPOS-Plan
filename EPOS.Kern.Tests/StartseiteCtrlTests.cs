using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die DATENSEITE der Startseite (iU9-W16b.0, K4 der Vermessung) — Klimaregionen,
    /// Variantengruppe und Projektname.
    ///
    /// <para><b>Warum es diese Fälle gibt.</b> Die vier Abfragen bei
    /// <c>Form_Start:356/369/382/390</c> und der Speicherweg der Klimaregion
    /// (<c>btn_Speichern_Click</c>) standen in einer WinForms-Maske und waren damit nur
    /// am Gerät prüfbar. Eine davon verkettete den Anwendertext in das <c>WHERE</c>
    /// (Befund W16-B11) — genau die Falle, die ein Apostroph im Regionsnamen
    /// aufschlägt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StartseiteCtrlTests : IClassFixture<TestDatenbank>
    {
        private const int ID_1030 = 1030;
        private const string NAME_1030 = "Referenz BHKW-Kaskade (Regressionstest)";

        private const int ID_1019 = 1019;
        private const string NAME_1019 = "Wöhler";
        private const int ID_1023 = 1023;   // "Wöhler - Test1", eine Variante von 1019

        private readonly TestDatenbank _db;

        public StartseiteCtrlTests(TestDatenbank db)
        {
            _db = db;
        }

        // =========================================================================
        // Ohne Datenbank
        // =========================================================================

        /// <summary>Ohne Projekt gibt es keine Klimaregion und keinen Namen.</summary>
        [Fact]
        public void Ohne_Projekt_ist_alles_leer()
        {
            Assert.Equal("", StartseiteCtrl.ProjektKlimaregion(0));
            Assert.Equal("", StartseiteCtrl.ProjektKlimaregion(-1));
            Assert.Equal("", StartseiteCtrl.Projektname(0));
            Assert.Equal("", StartseiteCtrl.KlimaregionName(0));
            Assert.Equal(0, StartseiteCtrl.KlimaregionStammId(""));
            Assert.Equal(0, StartseiteCtrl.KlimaregionIdVonProjekt(""));
        }

        /// <summary>
        /// Die beiden Vorprüfungen des Speicherwegs laufen OHNE Datenbank — wörtlich
        /// die ersten zwei <c>if</c> von <c>btn_Speichern_Click</c>.
        /// </summary>
        [Fact]
        public void Der_Speicherweg_prueft_Projekt_und_Region_zuerst()
        {
            Assert.Equal(KlimaStand.KeinProjekt,
                         StartseiteCtrl.KlimaregionSpeichern(0, "", "München"));
            Assert.Equal(KlimaStand.KeineRegion,
                         StartseiteCtrl.KlimaregionSpeichern(ID_1030, NAME_1030, ""));
        }

        /// <summary>Ohne Projekt ist die Variantengruppe leer.</summary>
        [Fact]
        public void Ohne_Projekt_gibt_es_keine_Variantengruppe()
        {
            VariantenAnzeige a = StartseiteCtrl.Varianten(0);

            Assert.Empty(a.Eintraege);
            Assert.Equal(0, a.GewaehltId);
            Assert.Equal(0, a.Anzahl);
            Assert.Equal("", a.StammName);
        }

        // =========================================================================
        // Lesend - geteilte Arbeitskopie
        // =========================================================================

        /// <summary>Der Auslieferungskatalog trägt Klimaregionen.</summary>
        [Fact]
        public void Die_Klimaregionen_des_Katalogs_stehen_zur_Wahl()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<string> regionen = StartseiteCtrl.Klimaregionen();

            Assert.NotEmpty(regionen);
            Assert.Contains("München", regionen);
        }

        /// <summary>
        /// Name und Stamm-Id einer Region sind zueinander umkehrbar — die beiden
        /// Abfragen bei <c>Form_Start:356</c> und <c>:369</c>.
        /// </summary>
        [Fact]
        public void Regionsname_und_Stamm_Id_gehoeren_zusammen()
        {
            if (!_db.Vorhanden) return;

            int id = StartseiteCtrl.KlimaregionStammId("München");

            Assert.True(id > 0);
            Assert.Equal("München", StartseiteCtrl.KlimaregionName(id));
        }

        /// <summary>
        /// Ein Regionsname mit Apostroph bricht die Abfrage NICHT mehr (Befund
        /// W16-B11): Der Vorläufer verkettete ihn in das <c>WHERE</c>.
        /// </summary>
        [Fact]
        public void Ein_Apostroph_im_Regionsnamen_bricht_nichts()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0, StartseiteCtrl.KlimaregionStammId("L'Aquila"));
        }

        /// <summary>
        /// Die Klimaregion eines Projekts kommt aus der PROJEKTKOPIE
        /// (<c>Tab_Klimaregion.Bezeichner</c>), nicht aus dem Stammsatz — genau das
        /// zeigte das Auswahlfeld der Startmaske an, und genau das gibt
        /// <c>IProjektKontext.Klimazone</c> heraus.
        /// </summary>
        [Fact]
        public void Die_Klimaregion_des_Projekts_ist_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal("München", StartseiteCtrl.ProjektKlimaregion(ID_1030));
            Assert.Equal("stuttgart", StartseiteCtrl.ProjektKlimaregion(1007));

            // Die Id am Projekt ist die der KOPIE, nicht die des Stammsatzes.
            int idKopie = StartseiteCtrl.KlimaregionIdVonProjekt(NAME_1030);
            Assert.True(idKopie > 0);
            Assert.NotEqual(StartseiteCtrl.KlimaregionStammId("München"), idKopie);
        }

        /// <summary>Der Projektname zu einer Id — und nichts zu einer unbekannten.</summary>
        [Fact]
        public void Der_Projektname_kommt_zur_Id()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(NAME_1030, StartseiteCtrl.Projektname(ID_1030));
            Assert.Equal("", StartseiteCtrl.Projektname(999999));
        }

        /// <summary>
        /// Eine Variantengruppe: Stamm zuerst, das offene Projekt vorausgewählt.
        /// <c>Anzahl</c> zählt — wie im Vorläufer — nur die VARIANTEN.
        /// </summary>
        [Fact]
        public void Die_Variantengruppe_traegt_Stamm_und_Varianten()
        {
            if (!_db.Vorhanden) return;

            VariantenAnzeige a = StartseiteCtrl.Varianten(ID_1019);

            Assert.NotEmpty(a.Eintraege);
            Assert.Equal(NAME_1019, a.StammName);
            Assert.Equal(ID_1019, a.GewaehltId);
            Assert.Contains(a.Eintraege, e => e.IstStamm && e.Id == ID_1019);
            Assert.Equal(a.Anzahl, a.Eintraege.Count - 1);
        }

        /// <summary>
        /// Steht eine VARIANTE offen, wird die Gruppe ihres STAMMS geladen — und die
        /// Variante selbst ist die Vorauswahl.
        /// </summary>
        [Fact]
        public void Aus_einer_Variante_heraus_kommt_die_Gruppe_des_Stamms()
        {
            if (!_db.Vorhanden) return;

            VariantenAnzeige a = StartseiteCtrl.Varianten(ID_1023);

            Assert.Equal(NAME_1019, a.StammName);
            Assert.Equal(ID_1023, a.GewaehltId);
            Assert.Contains(a.Eintraege, e => e.IstStamm && e.Id == ID_1019);
            Assert.Contains(a.Eintraege, e => e.Id == ID_1023);
        }

        // =========================================================================
        // Schreibend - EIGENE Arbeitskopie je Probe
        // =========================================================================

        /// <summary>
        /// Der Speicherweg kopiert die Region aus dem STAMM in das Projekt und
        /// schreibt am Projekt die Id der KOPIE fort — wörtlich
        /// <c>btn_Speichern_Click</c>.
        /// </summary>
        [Fact]
        public void Eine_gewaehlte_Klimaregion_wird_in_das_Projekt_kopiert()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                Assert.Equal("München", StartseiteCtrl.ProjektKlimaregion(ID_1030));

                Assert.Equal(KlimaStand.Gespeichert,
                             StartseiteCtrl.KlimaregionSpeichern(ID_1030, NAME_1030, "Berlin"));

                Assert.Equal("Berlin", StartseiteCtrl.ProjektKlimaregion(ID_1030));

                // Am Projekt steht die Id der PROJEKTKOPIE, nicht die des Stammsatzes.
                int idAmProjekt = StartseiteCtrl.KlimaregionIdVonProjekt(NAME_1030);
                Assert.NotEqual(StartseiteCtrl.KlimaregionStammId("Berlin"), idAmProjekt);
            }
        }

        /// <summary>
        /// Eine unbekannte Region wird nicht gespeichert — und das Projekt behält
        /// seine bisherige.
        /// </summary>
        [Fact]
        public void Eine_unbekannte_Region_wird_nicht_gespeichert()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                Assert.Equal(KlimaStand.RegionNichtGefunden,
                             StartseiteCtrl.KlimaregionSpeichern(ID_1030, NAME_1030, "Nirgendwo"));

                Assert.Equal("München", StartseiteCtrl.ProjektKlimaregion(ID_1030));
            }
        }
    }
}
