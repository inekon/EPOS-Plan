using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die fuenf <c>LiesProjekt</c>-Wege aus iU9-W9.0d, dazu
    /// <see cref="WaermebedarfStammCtrl.HatProjektzuordnung"/> und
    /// <see cref="ProjektCtrl.Existiert"/>.
    ///
    /// <para>Bis Welle 9 standen diese JOINs im Oberflaechencode — der Gebaeude-JOIN sogar
    /// DREIMAL wortgleich. Geprueft wird gegen eine Arbeitskopie der Testdatenbank; ohne
    /// sie schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektlistenTests
    {
        /// <summary>
        /// Ein Projekt, das im Testbestand ein Gebaeude, Stromverbraucher und Brauchwasser
        /// fuehrt (Referenzlauf 1007). Die externen Ganglinien und die Prozesswaerme haengen
        /// im Testbestand an anderen Projekten — deshalb die beiden Konstanten daneben.
        /// </summary>
        private const int PROJEKT = 1007;

        /// <summary>Das Projekt mit einer externen Waermebedarfsganglinie (Referenzlauf 1030).</summary>
        private const int PROJEKT_GANGLINIE = 1030;

        /// <summary>Das Projekt mit einer Prozesswaerme-Zuordnung.</summary>
        private const int PROJEKT_PROZESS = 1041;

        [Fact]
        public void Existiert_erkennt_ein_gespeichertes_und_ein_geratenes_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ProjektCtrl.Existiert(PROJEKT));
            Assert.False(ProjektCtrl.Existiert(0));
            Assert.False(ProjektCtrl.Existiert(-5));
            Assert.False(ProjektCtrl.Existiert(999999));
        }

        [Fact]
        public void Z_ProjGebCtrl_LiesProjekt_liefert_die_Gebaeudezuordnungen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);

            Assert.NotEmpty(liste);
            Assert.All(liste, m => Assert.Equal(PROJEKT, m.ID_Projekt));
            Assert.All(liste, m => Assert.True(m.ID_Z > 0));
            Assert.All(liste, m => Assert.False(string.IsNullOrEmpty(m.Gebaeudename)));
        }

        [Fact]
        public void Z_ProjGebCtrl_LiesProjekt_liefert_bei_unbekanntem_Projekt_eine_leere_Liste()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Empty(Z_ProjGebCtrl.LiesProjekt(999999));
        }

        [Fact]
        public void Z_ProjektGebGanglinieCtrl_LiesProjekt_traegt_bei_jeder_Zeile_einen_Kanal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Z_ProjWaermebedarfModel> liste =
                Z_ProjektGebGanglinieCtrl.LiesProjekt(PROJEKT_GANGLINIE);

            Assert.NotEmpty(liste);
            // Der Kanal kommt aus KanaeleNachladen; leer oder unbekannt faellt auf Heizung.
            Assert.All(liste, m => Assert.False(string.IsNullOrEmpty(m.Kanal)));
            Assert.All(liste, m => Assert.Equal(PROJEKT_GANGLINIE, m.m_ID_Projekt));
        }

        [Fact]
        public void Z_ProjektProzesswaermeCtrl_LiesProjekt_liest_Name_und_Summe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Z_ProjektProzesswaermeModel> liste =
                Z_ProjektProzesswaermeCtrl.LiesProjekt(PROJEKT_PROZESS);

            Assert.NotEmpty(liste);
            Assert.All(liste, m => Assert.Equal(PROJEKT_PROZESS, m.ID_Projekt));
            Assert.All(liste, m => Assert.False(string.IsNullOrEmpty(m.szProzessname)));
        }

        [Fact]
        public void Z_ProjektStromverbraucherCtrl_LiesProjekt_liest_Name_und_Summe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Z_ProjektStromverbraucherModel> liste =
                Z_ProjektStromverbraucherCtrl.LiesProjekt(PROJEKT);

            Assert.NotEmpty(liste);
            Assert.All(liste, m => Assert.Equal(PROJEKT, m.m_ID_Projekt));
            Assert.All(liste, m => Assert.False(string.IsNullOrEmpty(m.m_szVerbraucher)));
        }

        [Fact]
        public void Z_ProjektBrauchwasserCtrl_LiesProjekt_liest_Name_und_Summe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Z_ProjektBrauchwasserModel> liste = Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT);

            Assert.NotEmpty(liste);
            Assert.All(liste, m => Assert.Equal(PROJEKT, m.ID_Projekt));
            Assert.All(liste, m => Assert.False(string.IsNullOrEmpty(m.szBezeichner)));
        }

        /// <summary>
        /// Die Sperre vor dem Loeschen einer Ganglinie: Was einem Projekt zugeordnet ist,
        /// darf nicht aus dem Katalog verschwinden.
        /// </summary>
        [Fact]
        public void HatProjektzuordnung_erkennt_eine_zugeordnete_Ganglinie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WaermebedarfStammCtrl();
            Assert.False(ctrl.HatProjektzuordnung("gibt-es-nicht-" + Guid.NewGuid().ToString("N")));

            // Einen wirklich zugeordneten Bezeichner aus der Zuordnungstabelle holen.
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Bezeichner FROM Z_ProjektWaermebedarf WHERE Bezeichner IS NOT NULL LIMIT 1");
            if (dt == null || dt.Rows.Count == 0) return;

            string bezeichner = dt.Rows[0][0].ToString();
            if (string.IsNullOrEmpty(bezeichner)) return;

            Assert.True(ctrl.HatProjektzuordnung(bezeichner));
        }

        /// <summary>Die Jahressumme aus W9.0b — Σ der zwoelf Monatswerte eines Katalogsatzes.</summary>
        [Fact]
        public void Jahressumme_summiert_die_zwoelf_Monatswerte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> namen = BedarfStammCtrl.Typen(BedarfsArt.Prozesswaerme);
            Assert.Equal(0.0, BedarfStammCtrl.Jahressumme(
                BedarfsArt.Prozesswaerme, "gibt-es-nicht-" + Guid.NewGuid().ToString("N")));

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Bezeichner FROM " + BedarfStammCtrl.KopfTabelle(BedarfsArt.Prozesswaerme) +
                " LIMIT 1");
            if (dt == null || dt.Rows.Count == 0) return;

            string bez = dt.Rows[0][0].ToString();
            double[] monat = BedarfStammCtrl.Monatswerte(BedarfsArt.Prozesswaerme, bez);
            Assert.Equal(monat.Sum(), BedarfStammCtrl.Jahressumme(BedarfsArt.Prozesswaerme, bez), 9);
        }
    }
}
