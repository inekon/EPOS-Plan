using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Katalog der Stromganglinien: Loeschen, Kopieren, Sperren</b>
    /// (iU9-W12-E-1, Anwenderwunsch der Windows-Abnahme vom 05.09.2026).
    ///
    /// <para><b>Warum es diese Faelle gibt.</b> Der Dialog „Stromganglinien" kann seit
    /// diesem Wunsch am KATALOG schreiben — importieren, kopieren, loeschen. Der
    /// Referenzlauf sieht davon nichts: Er rechnet einen bestehenden Projektstand nach
    /// und beruehrt keinen Pflegepfad. Ohne diese Faelle waeren die drei neuen Wege
    /// allein am Windows-Geraet nachweisbar.</para>
    ///
    /// <para><b>Der Importweg selbst steht nicht hier</b>, sondern unveraendert in
    /// <see cref="GanglinienImportAblaufTests"/> und <see cref="GanglinienProbenTests"/>
    /// — es ist DIESELBE Kette (<c>GanglinienImportAblauf</c>), der Dialog haengt sie
    /// nur zusaetzlich ein. Hier steht, was mit W12-E-1 NEU im Kern ist:
    /// <c>Exists</c>, <c>HatProjektzuordnung</c>, <c>KopiereStamm</c> und das
    /// ReadOnly-Kennzeichen in <c>ReadAll</c>.</para>
    ///
    /// <para><b>Ohne Datenbank schweigen die Faelle</b> (<see cref="TestDatenbank"/>).
    /// Die Arbeitskopie wird je KLASSE geteilt; die schreibenden Faelle legen sich
    /// deshalb je einen EIGENEN Namen an und raeumen ihn nicht wieder weg — die Kopie
    /// faellt am Klassenende ohnehin.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromganglinieKatalogTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public StromganglinieKatalogTests(TestDatenbank db) { _db = db; }

        /// <summary>Der Satz, der in der Testdatenbank einem Projekt zugeordnet ist.</summary>
        private const string ZUGEORDNET = "Lastgang_Strom_NestleLB-05-2010-05-2011";

        /// <summary>Ein Satz ohne Projektzuordnung.</summary>
        private const string FREI = "test";

        // ==================================================================
        //  1 - ReadAll traegt das Auslieferungskennzeichen
        // ==================================================================

        /// <summary>
        /// <c>ReadAll</c> liest <c>ReadOnly</c> mit. Bis W12-E-1 fiel die Spalte weg,
        /// und jede Huelle fragte sie je Zeile einzeln nach (N+1) — die
        /// Zuordnungshuelle gar nicht, sie gab schlicht <c>false</c> weiter.
        /// Gegenprobe ist <see cref="StromganglinieStammCtrl.IsReadOnly"/>: dieselbe
        /// Spalte, dieselbe Zeile, derselbe Wert.
        /// </summary>
        [Fact]
        public void ReadAll_traegt_dasselbe_ReadOnly_wie_die_Einzelabfrage()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            ctrl.ReadAll();

            Assert.NotEmpty(ctrl.items);
            foreach (StromganglinieModel m in ctrl.items)
                Assert.Equal(ctrl.IsReadOnly(m.m_szBezeichner), m.m_bReadOnly);
        }

        // ==================================================================
        //  2 - Die zwei Loeschsperren
        // ==================================================================

        /// <summary>
        /// Die Projektzuordnungssperre: <c>Lastgang_Strom_NestleLB-…</c> haengt in
        /// <c>Z_ProjektStromganglinie</c> (drei Zeilen, Projekte 1008 und 1030),
        /// <c>test</c> nicht.
        /// </summary>
        [Fact]
        public void HatProjektzuordnung_trennt_zugeordnete_von_freien_Ganglinien()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            Assert.True(ctrl.HatProjektzuordnung(ZUGEORDNET));
            Assert.False(ctrl.HatProjektzuordnung(FREI));
            Assert.False(ctrl.HatProjektzuordnung("gibt es nicht"));
            Assert.False(ctrl.HatProjektzuordnung(null));
        }

        /// <summary>
        /// Dieselbe Zahl wie die Zaehlung von Hand — der Ersatz fuer das verkettete
        /// <c>SELECT</c>, das die Solarfassung bis W14b hatte.
        /// </summary>
        [Fact]
        public void HatProjektzuordnung_zaehlt_dieselben_Zeilen_wie_die_Tabelle()
        {
            if (!_db.Vorhanden) return;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT COUNT(*) AS N FROM Z_ProjektStromganglinie WHERE Bezeichner = ?",
                new DbParam("@bez", ZUGEORDNET));

            int erwartet = System.Convert.ToInt32(dt.Rows[0]["N"]);
            Assert.True(erwartet > 0);
            Assert.Equal(erwartet > 0, new StromganglinieStammCtrl().HatProjektzuordnung(ZUGEORDNET));
        }

        /// <summary>
        /// Ein Auslieferungssatz (<c>ReadOnly</c>) bleibt stehen — die zweite Sperre.
        /// Der Bestand der Testdatenbank fuehrt keinen; der Fall legt sich deshalb
        /// einen an.
        /// </summary>
        [Fact]
        public void Ein_Auslieferungssatz_wird_nicht_geloescht()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            const string SATZ_RO = "W12E1-Auslieferung";

            int id = ctrl.KopiereStamm(FREI, SATZ_RO);
            Assert.True(id > 0);

            DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromganglinie_STAMM SET ReadOnly = 1 WHERE ID = ?",
                new DbParam("@id", id));
            Assert.True(ctrl.IsReadOnly(SATZ_RO));

            Assert.False(ctrl.Delete(SATZ_RO));
            Assert.True(ctrl.Exists(SATZ_RO));          // steht noch
        }

        /// <summary>
        /// Eine freie Ganglinie faellt samt ihren Werten. Der Fall legt sich seinen
        /// eigenen Satz an, damit er den Bestand der Klasse nicht anfasst.
        /// </summary>
        [Fact]
        public void Eine_freie_Ganglinie_faellt_samt_ihren_Werten()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            const string SATZ_WEG = "W12E1-Loeschprobe";

            int id = ctrl.KopiereStamm(FREI, SATZ_WEG);
            Assert.True(id > 0);
            Assert.NotEmpty(Werte(id));

            Assert.True(ctrl.Delete(SATZ_WEG));
            Assert.False(ctrl.Exists(SATZ_WEG));
            Assert.Empty(Werte(id));                 // keine Datenwaisen
        }

        // ==================================================================
        //  3 - Speichern unter: die Kopie
        // ==================================================================

        /// <summary>
        /// Die Kopie traegt denselben Zeitschritt und dieselben Werte in derselben
        /// Reihenfolge — sonst waere sie eine andere Ganglinie.
        /// </summary>
        [Fact]
        public void Die_Kopie_traegt_dieselben_Werte_unter_neuem_Namen()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            const string SATZ_KOPIE = "W12E1-Kopie";

            int quellId = ctrl.GetStammId(FREI);
            Assert.True(quellId > 0);

            int neueId = ctrl.KopiereStamm(FREI, SATZ_KOPIE);
            Assert.True(neueId > 0);
            Assert.NotEqual(quellId, neueId);

            StromganglinieModel quelle = StromganglinieStammCtrl.FindeStamm(FREI);
            StromganglinieModel kopie = StromganglinieStammCtrl.FindeStamm(SATZ_KOPIE);
            Assert.NotNull(kopie);
            Assert.Equal(quelle.m_Zeitinterval, kopie.m_Zeitinterval);

            List<double> a = Werte(quellId);
            List<double> b = Werte(neueId);
            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.Equal(a[i], b[i]);

            // Eine Kopie ist Anwenderbestand, nie Auslieferung.
            Assert.False(ctrl.IsReadOnly(SATZ_KOPIE));
        }

        /// <summary>
        /// Ein Auslieferungssatz darf kopiert werden — die Kopie ist danach FREI. Das
        /// ist der Sinn von „Speichern unter": den Katalogsatz nicht anfassen und
        /// trotzdem eine eigene Fassung bekommen.
        /// </summary>
        [Fact]
        public void Die_Kopie_eines_Auslieferungssatzes_ist_frei()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            const string SATZ_QUELLE_RO = "W12E1-Quelle-RO";
            const string SATZ_ZIEL_RO = "W12E1-Quelle-RO - Kopie";

            int id = ctrl.KopiereStamm(FREI, SATZ_QUELLE_RO);
            Assert.True(id > 0);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromganglinie_STAMM SET ReadOnly = 1 WHERE ID = ?",
                new DbParam("@id", id));

            Assert.True(ctrl.KopiereStamm(SATZ_QUELLE_RO, SATZ_ZIEL_RO) > 0);
            Assert.False(ctrl.IsReadOnly(SATZ_ZIEL_RO));
            Assert.True(ctrl.Delete(SATZ_ZIEL_RO));          // und damit loeschbar
        }

        /// <summary>
        /// <b>Die Dublettenpruefung steht VOR dem Einfuegen.</b> Ein vergebener Name
        /// ergibt <c>0</c> und KEINE Zeile — nicht einen UNIQUE-Fehler, den der
        /// Anwender als Ausnahmetext saehe.
        /// </summary>
        [Fact]
        public void Ein_vergebener_Name_wird_abgewiesen_statt_zu_werfen()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            int vorher = Anzahl();

            Assert.Equal(0, ctrl.KopiereStamm(FREI, FREI));
            Assert.Equal(0, ctrl.KopiereStamm(FREI, ZUGEORDNET));
            Assert.Equal(0, ctrl.KopiereStamm(FREI, "  " + FREI + "  "));   // getrimmt geprueft

            Assert.Equal(vorher, Anzahl());
        }

        /// <summary>Leerer Name, unbekannte Quelle: <c>0</c> und keine Zeile.</summary>
        [Fact]
        public void Ohne_Quelle_oder_ohne_Namen_entsteht_keine_Kopie()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            int vorher = Anzahl();

            Assert.Equal(0, ctrl.KopiereStamm(FREI, ""));
            Assert.Equal(0, ctrl.KopiereStamm(FREI, "   "));
            Assert.Equal(0, ctrl.KopiereStamm(FREI, null));
            Assert.Equal(0, ctrl.KopiereStamm("", "W12E1-ohne-Quelle"));
            Assert.Equal(0, ctrl.KopiereStamm("gibt es nicht", "W12E1-ohne-Quelle"));

            Assert.Equal(vorher, Anzahl());
            Assert.False(ctrl.Exists("W12E1-ohne-Quelle"));
        }

        /// <summary>
        /// <c>Exists</c> ist die Pruefung, die der Namensdialog braucht — und sie ist
        /// zeichengenau, nicht praefixhaft (der Fehler, der beim Solarkatalog
        /// Befund W14-B70 war).
        /// </summary>
        [Fact]
        public void Exists_prueft_den_ganzen_Namen_und_nicht_seinen_Anfang()
        {
            if (!_db.Vorhanden) return;

            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            Assert.True(ctrl.Exists(FREI));
            Assert.False(ctrl.Exists(FREI + "x"));
            Assert.False(ctrl.Exists(""));
            Assert.False(ctrl.Exists(null));
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static List<double> Werte(int idGanglinie)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Wert FROM Tab_StromganglinieDaten_STAMM WHERE ID_Ganglinie = ? ORDER BY ID",
                new DbParam("@g", idGanglinie));

            List<double> werte = new List<double>();
            if (dt == null) return werte;
            foreach (DataRow r in dt.Rows)
                werte.Add(r["Wert"] != System.DBNull.Value ? System.Convert.ToDouble(r["Wert"]) : 0);
            return werte;
        }

        private static int Anzahl()
        {
            object v = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Stromganglinie_STAMM");
            return v != null && v != System.DBNull.Value ? System.Convert.ToInt32(v) : 0;
        }
    }
}
