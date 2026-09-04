using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die zwei Abfragen, die mit iU9-W12.0g aus dem Oberflaechencode in den Kern
    /// gezogen sind (Befund W12-B4).
    ///
    /// <para><b>Warum sie hier stehen.</b> Der INNER JOIN stand DREIMAL im Bestand
    /// (<c>Form_Start</c>, <c>StromganglinieKontextMenuCtrl</c> und, tot, in
    /// <c>Form_Stromganglinie</c>), die Katalogabfrage war mit dem ListBox-Text
    /// konkateniert — ein Bezeichner mit Apostroph brach sie. Der Referenzlauf deckt
    /// den RECHENWEG ab, nicht die Dialogpfade; ohne diese Faelle waeren beide allein
    /// am Windows-Geraet nachweisbar.</para>
    ///
    /// <para>Ohne Testdatenbank schweigen die Faelle. EINE Arbeitskopie je Klasse —
    /// hier wird nur gelesen (Regel seit iU9-W11a).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromganglinieSqlTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public StromganglinieSqlTests(TestDatenbank db) { _db = db; }

        private const int PROJEKT = 1030;

        /// <summary>
        /// <see cref="Z_ProjektStromganglinieCtrl.LiesProjekt"/> liefert dieselben
        /// Zeilen wie der abgeloeste konkatenierte JOIN — Feld fuer Feld.
        /// </summary>
        [Fact]
        public void LiesProjekt_liefert_dieselben_Zeilen_wie_der_abgeloeste_JOIN()
        {
            if (!_db.Vorhanden) return;

            Z_ProjektStromganglinieCtrl alt = new Z_ProjektStromganglinieCtrl();
            alt.ReadAll(
                "SELECT Z_ProjektStromganglinie.ID AS ID_Z, Z_ProjektStromganglinie.ID_Projekt, " +
                "Z_ProjektStromganglinie.ID_Ganglinie, Tab_Stromganglinie.Bezeichner " +
                "FROM Z_ProjektStromganglinie INNER JOIN Tab_Stromganglinie ON " +
                "Z_ProjektStromganglinie.ID_Ganglinie = Tab_Stromganglinie.ID " +
                " where Z_ProjektStromganglinie.ID_Projekt=" + PROJEKT);

            List<Z_ProjektStromganglinieModel> neu = Z_ProjektStromganglinieCtrl.LiesProjekt(PROJEKT);

            Assert.Equal(alt.rows, neu.Count);
            for (int i = 0; i < neu.Count; i++)
            {
                Assert.Equal(alt.items[i].m_ID_Z, neu[i].m_ID_Z);
                Assert.Equal(PROJEKT, neu[i].m_ID_Projekt);
                Assert.Equal(alt.items[i].m_ID_Stromganglinie, neu[i].m_ID_Stromganglinie);
                Assert.Equal(alt.items[i].m_szStromganglinie, neu[i].m_szStromganglinie);
            }
        }

        /// <summary>Ein Projekt ohne Zuordnung liefert eine leere Liste, nie <c>null</c>.</summary>
        [Fact]
        public void LiesProjekt_liefert_fuer_ein_unbekanntes_Projekt_eine_leere_Liste()
        {
            if (!_db.Vorhanden) return;

            List<Z_ProjektStromganglinieModel> liste = Z_ProjektStromganglinieCtrl.LiesProjekt(999999);
            Assert.NotNull(liste);
            Assert.Empty(liste);
        }

        /// <summary>
        /// <see cref="StromganglinieStammCtrl.FindeStamm"/> trifft denselben Satz, den
        /// <c>GetStammId</c> ueber denselben Bezeichner findet.
        /// </summary>
        [Fact]
        public void FindeStamm_trifft_denselben_Satz_wie_GetStammId()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            ctrl.ReadAll();
            if (ctrl.rows == 0) return;      // der Katalog der Testdatenbank ist leer

            string bezeichner = ctrl.items[0].m_szBezeichner;
            StromganglinieModel satz = StromganglinieStammCtrl.FindeStamm(bezeichner);

            Assert.NotNull(satz);
            Assert.Equal(ctrl.GetStammId(bezeichner), satz.ID);
            Assert.Equal(satz.ID, satz.m_ID_Ganglinie);
            Assert.Equal(bezeichner, satz.m_szBezeichner);
            Assert.Equal(ctrl.items[0].m_Zeitinterval, satz.m_Zeitinterval);
        }

        /// <summary>
        /// Ein Name, den es nicht gibt — und ein Name mit Apostroph, an dem die
        /// konkatenierte Fassung zerbrach (Befund W12-B4). Beide liefern <c>null</c>
        /// statt einer Ausnahme.
        /// </summary>
        [Fact]
        public void FindeStamm_vertraegt_einen_Apostroph_im_Namen()
        {
            if (!_db.Vorhanden) return;

            Assert.Null(StromganglinieStammCtrl.FindeStamm("gibt es nicht"));
            Assert.Null(StromganglinieStammCtrl.FindeStamm("O'Brien's Lastgang"));
            Assert.Null(StromganglinieStammCtrl.FindeStamm(""));
            Assert.Null(StromganglinieStammCtrl.FindeStamm(null));
        }
    }
}
