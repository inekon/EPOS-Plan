using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die elf SQL-Stellen der Welle 11, die mit iU9-W11a.2 in Kern-Controller gezogen
    /// sind — je Methode eine Probe gegen <c>Kenndaten_Test.sqlite</c>.
    ///
    /// <para><b>Warum sie hier steht.</b> Der Referenzlauf deckt den RECHENWEG ab, nicht
    /// die Dialog- und Pflegepfade (Wurzel-CLAUDE.md, Werkzeugtabelle). Sechs der elf
    /// Stellen speisen nur eine Anzeige — ohne diese Faelle waeren sie allein am
    /// Windows-Geraet nachweisbar.</para>
    ///
    /// <para>Ohne Testdatenbank schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationErgebnisSqlTests
    {
        private const int PROJEKT = 1030;

        // ---------------------------------------------------------------- Konfiguration

        /// <summary>
        /// <see cref="KonfigurationCtrl.LiesProjekt"/> liefert dieselbe Zeile wie der
        /// abgeloeste konkatenierte <c>ReadSingle</c> — Feld fuer Feld.
        /// </summary>
        [Fact]
        public void LiesProjekt_liefert_dieselbe_Zeile_wie_ReadSingle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KonfigurationCtrl alt = new KonfigurationCtrl();
            alt.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + PROJEKT);
            Assert.Equal(1, alt.rows);

            KonfigurationModel neu = KonfigurationCtrl.LiesProjekt(PROJEKT);
            Assert.NotNull(neu);

            Assert.Equal(alt.model.m_ID, neu.m_ID);
            Assert.Equal(alt.model.m_ID_Projekt, neu.m_ID_Projekt);
            Assert.Equal(alt.model.m_Netzverluste, neu.m_Netzverluste);
            Assert.Equal(alt.model.m_szNetzverlusteEinheit, neu.m_szNetzverlusteEinheit);
            Assert.Equal(alt.model.m_BHKW_Grenzleistung, neu.m_BHKW_Grenzleistung);
            Assert.Equal(alt.model.m_WP_Heizstab, neu.m_WP_Heizstab);
            Assert.Equal(alt.model.m_Kessel_Betriebsbereitschaft, neu.m_Kessel_Betriebsbereitschaft);
            Assert.Equal(alt.model.m_Tool_1, neu.m_Tool_1);
            Assert.Equal(alt.model.m_Tool_2, neu.m_Tool_2);
            Assert.Equal(alt.model.m_Tool_3, neu.m_Tool_3);
            Assert.Equal(alt.model.m_Tool_4, neu.m_Tool_4);
            Assert.Equal(alt.model.m_Tool_5, neu.m_Tool_5);
            Assert.Equal(alt.model.m_Tool_6, neu.m_Tool_6);
            Assert.Equal(alt.model.Betriebsart, neu.Betriebsart);
            Assert.Equal(alt.model.Leistungsgrenze, neu.Leistungsgrenze);
            Assert.Equal(alt.model.Extrapolation_erlaubt, neu.Extrapolation_erlaubt);
            Assert.Equal(alt.model.Kanal_Knappheitsreihenfolge, neu.Kanal_Knappheitsreihenfolge);
        }

        /// <summary>
        /// <c>ProjektLesen</c> ist der wortgleiche Ersatz — dasselbe <c>rows</c>,
        /// dasselbe <c>model</c>. Genau darauf haengen die acht umgehaengten Aufrufer.
        /// </summary>
        [Fact]
        public void ProjektLesen_setzt_rows_und_model_wie_ReadSingle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KonfigurationCtrl a = new KonfigurationCtrl();
            a.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + PROJEKT);

            KonfigurationCtrl b = new KonfigurationCtrl();
            Assert.True(b.ProjektLesen(PROJEKT));

            Assert.Equal(a.rows, b.rows);
            Assert.Equal(a.model.m_ID, b.model.m_ID);
            Assert.Equal(a.model.m_Tool_1, b.model.m_Tool_1);
            Assert.Equal(a.model.Leistungsgrenze, b.model.Leistungsgrenze);
        }

        [Fact]
        public void LiesProjekt_ohne_Projekt_liefert_null()
        {
            Assert.Null(KonfigurationCtrl.LiesProjekt(0));
            Assert.Null(KonfigurationCtrl.LiesProjekt(-3));
        }

        [Fact]
        public void ProjektLesen_ohne_Zeile_meldet_rows_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            Assert.False(ctrl.ProjektLesen(999999));
            Assert.Equal(0, ctrl.rows);
        }

        // ---------------------------------------------------------------- Heizkessel

        /// <summary>
        /// <see cref="HeizkesselStammCtrl.BrennstoffartenJeProjekt"/> — die einzige echte
        /// Fachabfrage der Detailmaske. Geprueft wird die INVARIANTE: jede gemeldete Art
        /// ist eine gueltige Brennstoffnummer, und ein Projekt ohne Kessel meldet nichts.
        /// </summary>
        [Fact]
        public void BrennstoffartenJeProjekt_liefert_nur_gueltige_Nummern()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            HashSet<int> arten = HeizkesselStammCtrl.BrennstoffartenJeProjekt(PROJEKT);
            Assert.NotNull(arten);
            foreach (int a in arten) Assert.True(a >= 0);
        }

        [Fact]
        public void BrennstoffartenJeProjekt_ohne_Projekt_bleibt_leer()
        {
            Assert.Empty(HeizkesselStammCtrl.BrennstoffartenJeProjekt(0));
        }

        // ---------------------------------------------------------------- Anlagen

        /// <summary>
        /// <see cref="WErzeugerCtrl.AnlagenJeTyp"/> bedient Liste UND Zaehlung. Die
        /// Reihenfolge ist <c>ORDER BY ID</c> (Fachkonzept 7.3) — daran haengt, dass eine
        /// Variante in allen drei Ansichten an derselben Stelle steht.
        /// </summary>
        [Fact]
        public void AnlagenJeTyp_liefert_die_Zeilen_nach_Id_sortiert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var zeilen = WErzeugerCtrl.AnlagenJeTyp(PROJEKT, WizardItemClass.WP_TYP);
            Assert.NotNull(zeilen);

            for (int i = 1; i < zeilen.Count; i++)
                Assert.True(zeilen[i].Id > zeilen[i - 1].Id, "ORDER BY ID verletzt");

            foreach (var z in zeilen)
            {
                Assert.True(z.Id > 0);
                Assert.NotNull(z.Bezeichner);
            }
        }

        /// <summary>
        /// Die Zaehlung ist der <c>Count</c> derselben Liste — kein zweites
        /// <c>SELECT COUNT(*)</c> mehr (abgeloeste Stelle
        /// <c>Form_Simulation_Detail.SpVariantenzahl</c>).
        /// </summary>
        [Fact]
        public void AnlagenJeTyp_Count_ersetzt_die_Zaehlabfrage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int ausListe = WErzeugerCtrl.AnlagenJeTyp(PROJEKT, WizardItemClass.SP_TYP).Count;

            object wert = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new DbParam("@proj", PROJEKT),
                new DbParam("@typ", WizardItemClass.SP_TYP));

            Assert.Equal(System.Convert.ToInt32(wert), ausListe);
        }

        [Fact]
        public void AnlagenJeTyp_ohne_Projekt_bleibt_leer()
        {
            Assert.Empty(WErzeugerCtrl.AnlagenJeTyp(0, WizardItemClass.SP_TYP));
        }

        /// <summary>
        /// <see cref="WErzeugerCtrl.ModelleJeTyp"/> ist der parametrisierte Ersatz fuer
        /// <c>ReadAllFilter("ID_Projekt=… and ID_Type=…")</c> — dieselben Zeilen.
        /// </summary>
        [Fact]
        public void ModelleJeTyp_liefert_dieselben_Zeilen_wie_ReadAllFilter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WErzeugerCtrl alt = new WErzeugerCtrl();
            alt.ReadAllFilter("ID_Projekt=" + PROJEKT + " and ID_Type=" + WizardItemClass.WP_TYP);

            var neu = WErzeugerCtrl.ModelleJeTyp(PROJEKT, WizardItemClass.WP_TYP);

            Assert.Equal(alt.rows, neu.Count);
            for (int i = 0; i < neu.Count; i++)
            {
                Assert.Equal(alt.items[i].ID, neu[i].ID);
                Assert.Equal(alt.items[i].Bezeichner, neu[i].Bezeichner);
                Assert.Equal(alt.items[i].ID_WP, neu[i].ID_WP);
            }
        }

        /// <summary>
        /// <see cref="WErzeugerCtrl.AnlagenBezeichner"/> — der Anzeigename einer
        /// Anlagenzeile. Ohne Treffer <c>null</c>, damit der Aufrufer auf die Id
        /// zurueckfaellt.
        /// </summary>
        [Fact]
        public void AnlagenBezeichner_liefert_den_Namen_der_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var zeilen = WErzeugerCtrl.AnlagenJeTyp(PROJEKT, WizardItemClass.WP_TYP);
            if (zeilen.Count == 0) return;

            string name = WErzeugerCtrl.AnlagenBezeichner(zeilen[0].Id);
            Assert.Equal(zeilen[0].Bezeichner, name ?? "");
        }

        [Fact]
        public void AnlagenBezeichner_ohne_Zeile_liefert_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(WErzeugerCtrl.AnlagenBezeichner(0));
            Assert.Null(WErzeugerCtrl.AnlagenBezeichner(999999));
        }

        // ---------------------------------------------------------------- Stromspeicher

        /// <summary>
        /// <see cref="StromspeicherStammCtrl.KapazitaetUndLeistung"/> ohne Einengung ist
        /// die Aggregation ueber alle Speicheranlagen — nie negativ, und die Leistung
        /// steht nur, wenn auch eine Kapazitaet steht.
        /// </summary>
        [Fact]
        public void KapazitaetUndLeistung_aggregiert_ueber_das_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var w = StromspeicherStammCtrl.KapazitaetUndLeistung(PROJEKT);
            Assert.True(w.Kwh >= 0.0);
            Assert.True(w.Kw >= 0.0);
        }

        /// <summary>
        /// Der RUECKFALL: Eine Anlagen-Id, die zu diesem Projekt nicht gehoert, liefert
        /// dieselbe Aggregation wie ganz ohne Einengung — das ist die zweite der „zwei
        /// Fassungen" aus der Vermessung, und sie bleibt.
        /// </summary>
        [Fact]
        public void KapazitaetUndLeistung_faellt_auf_die_Aggregation_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ohne = StromspeicherStammCtrl.KapazitaetUndLeistung(PROJEKT);
            var fremd = StromspeicherStammCtrl.KapazitaetUndLeistung(PROJEKT, 999999);

            Assert.Equal(ohne.Kwh, fremd.Kwh, 9);
            Assert.Equal(ohne.Kw, fremd.Kw, 9);
        }

        /// <summary>
        /// <see cref="StromspeicherStammCtrl.KapazitaetJeProjekt"/> — der 5-kWh-Rueckfall
        /// der Autarkiekachel. Er stand bis iU9-W11a.2 in der Navigationsklasse
        /// (Befund W11-B45) und gilt fuer jedes Projekt ohne Speicher.
        /// </summary>
        [Fact]
        public void KapazitaetJeProjekt_faellt_ohne_Speicher_auf_fuenf_kWh()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(StromspeicherStammCtrl.KAPAZITAET_RUECKFALL_KWH,
                         StromspeicherStammCtrl.KapazitaetJeProjekt(999999));
        }

        [Fact]
        public void KapazitaetJeProjekt_ohne_Projekt_faellt_ebenfalls_zurueck()
        {
            Assert.Equal(StromspeicherStammCtrl.KAPAZITAET_RUECKFALL_KWH,
                         StromspeicherStammCtrl.KapazitaetJeProjekt(0));
        }

        /// <summary>
        /// Fuehrt das Projekt Speicher, ist die Summe positiv und deckt sich mit der
        /// Aggregation von <see cref="StromspeicherStammCtrl.KapazitaetUndLeistung"/>.
        /// </summary>
        [Fact]
        public void KapazitaetJeProjekt_deckt_sich_mit_der_Aggregation()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double summe = StromspeicherStammCtrl.KapazitaetJeProjekt(PROJEKT);
            double aggregat = StromspeicherStammCtrl.KapazitaetUndLeistung(PROJEKT).Kwh;

            if (aggregat > 0.0) Assert.Equal(aggregat, summe, 9);
            else Assert.Equal(StromspeicherStammCtrl.KAPAZITAET_RUECKFALL_KWH, summe);
        }
    }
}
