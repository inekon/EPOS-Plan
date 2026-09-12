using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Katalog des externen Waermebedarfs: Kennzahlen, Loeschen, Kopieren</b>
    /// (iU9-W9-E-3, Anwenderwunsch der Windows-Abnahme vom 05.09.2026: „Gestalte den
    /// Dialog bei Waermebedarf → Daten importieren analog zum Import des
    /// Strombedarf … mit grafischer Darstellung etc.").
    ///
    /// <para><b>Warum es diese Faelle gibt.</b> Der Dialog „Waermebedarf Extern" zeigt
    /// seit diesem Wunsch drei Zahlen und eine Kurve und kann am KATALOG schreiben —
    /// importieren, kopieren, loeschen. Der Referenzlauf sieht davon nichts: Er
    /// rechnet einen bestehenden Projektstand nach und oeffnet keinen Dialog. Ohne
    /// diese Faelle waeren die neuen Wege allein am Windows-Geraet nachweisbar.</para>
    ///
    /// <para><b>Der Rechenweg ist derselbe wie beim Strom</b> — es gibt ihn nur EINMAL
    /// (<see cref="GanglinienAuswertungCtrl"/>, Auspraegung
    /// <see cref="GanglinienQuelle"/>); die Faelle hier belegen, dass die
    /// Waerme-Auspraegung die richtigen Tabellen liest und dass die Zahlen zu den
    /// Werten der Testdatenbank passen. <b>Eingefroren</b>: Wer den Leseweg anfasst,
    /// sieht es an dieser Stelle.</para>
    ///
    /// <para><b>Der Importweg selbst steht nicht hier</b>, sondern unveraendert in
    /// <see cref="GanglinienImportAblaufTests"/> und <see cref="GanglinienProbenTests"/>
    /// — es ist DIESELBE Kette (<c>GanglinienImportAblauf</c>), der Waermebedarf haengt
    /// sie seit W9-E-3 nur mit einer anderen Auspraegung ein. Hier steht, was daran
    /// neu im Kern ist: die zwei Schreibwege der Auspraegung
    /// (<c>ImportGanglinie</c>/<c>ErsetzeGanglinie</c>), <c>Exists</c> und
    /// <c>KopiereStamm</c>.</para>
    ///
    /// <para><b>Ohne Datenbank schweigen die Faelle</b> (<see cref="TestDatenbank"/>).
    /// Die Arbeitskopie wird je KLASSE geteilt; die schreibenden Faelle legen sich
    /// deshalb je einen EIGENEN Namen an und raeumen ihn nicht wieder weg — die Kopie
    /// faellt am Klassenende ohnehin.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermebedarfKatalogTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public WaermebedarfKatalogTests(TestDatenbank db) { _db = db; }

        /// <summary>Ein Katalogsatz, der in <c>Z_ProjektWaermebedarf</c> haengt (Projekt 1041).</summary>
        private const string ZUGEORDNET = "Wärmebedarf_Laurentiuskirche";

        /// <summary>Ein Katalogsatz ohne Projektzuordnung und ohne ReadOnly.</summary>
        private const string FREI = "test";

        /// <summary>Ein Auslieferungssatz (<c>ReadOnly = TRUE</c>).</summary>
        private const string AUSLIEFERUNG = "Nestle_Sprühturm-Wärmebedarf-1098kW-4300h-4724MWh.txt";

        // ==================================================================
        //  1 - Die Kennzahlen der Grafik, eingefroren
        // ==================================================================

        /// <summary>
        /// <c>Wärmebedarf_Laurentiuskirche</c> liegt im Stundenraster; die Verdichtung
        /// fasst sie nicht an. Jahresarbeit = Σ der 8 760 Stundenleistungen ÷ 1 000,
        /// Spitze = ihr Hoechstwert, Vollbenutzungsstunden = Arbeit [kWh] ÷ Spitze.
        /// </summary>
        [Fact]
        public void Ein_Waermekatalogsatz_traegt_die_eingefrorenen_Kennzahlen()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, ZUGEORDNET);

            Assert.True(a.Erfolgreich);
            Assert.Equal(ZUGEORDNET, a.Bezeichner);
            Assert.Equal(GanglinienAuswertungCtrl.STUNDEN_JAHR, a.Stundenwerte.Length);

            Assert.Equal(65.4298, a.JahresarbeitMwh, 3);
            Assert.Equal(47.6489, a.SpitzeKw, 3);

            Assert.NotNull(a.VollbenutzungsstundenH);
            Assert.Equal(1373.1626, a.VollbenutzungsstundenH.Value, 3);
        }

        /// <summary>
        /// Die zweite eingefrorene Reihe — der Auslieferungssatz, dessen Name seine
        /// Kennzahlen schon nennt: 1 098 kW Spitze, 4 300 h, 4 724 MWh. Genau das
        /// muss die Anzeige zeigen.
        /// </summary>
        [Fact]
        public void Der_Auslieferungssatz_traegt_die_Zahlen_aus_seinem_Namen()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, AUSLIEFERUNG);

            Assert.True(a.Erfolgreich);
            Assert.Equal(4724.694, a.JahresarbeitMwh, 3);
            Assert.Equal(1098.0, a.SpitzeKw, 3);
            Assert.Equal(4303.0, a.VollbenutzungsstundenH.Value, 3);
        }

        /// <summary>
        /// Die PROJEKTKOPIE traegt dieselben Zahlen wie ihr Katalogsatz — es sind
        /// dieselben Werte, nur in <c>Tab_WaermebedarfDaten</c> statt in
        /// <c>…Daten_STAMM</c> (Projekt 1041, Kopf-Id 11).
        /// </summary>
        [Fact]
        public void Die_Projektkopie_traegt_dieselben_Zahlen_wie_der_Katalog()
        {
            if (!_db.Vorhanden) return;

            int idKopie = KopieId(1041, ZUGEORDNET);
            Assert.True(idKopie > 0);

            GanglinienAuswertung kopie = GanglinienAuswertungCtrl.AusProjekt(
                GanglinienQuelle.Waermebedarf, idKopie, ZUGEORDNET);
            GanglinienAuswertung katalog = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, ZUGEORDNET);

            Assert.True(kopie.Erfolgreich);
            Assert.Equal(katalog.JahresarbeitMwh, kopie.JahresarbeitMwh, 6);
            Assert.Equal(katalog.SpitzeKw, kopie.SpitzeKw, 6);
        }

        /// <summary>
        /// <b>Der Rueckfall ist der Normalfall, kein Notnagel.</b> Eine im Dialog eben
        /// erst zugeordnete Zeile hat noch KEINE Projektkopie (Id 0); gezeigt wird
        /// dann der Katalogsatz, aus dem die Kopie entstehen wird.
        /// </summary>
        [Fact]
        public void Ohne_Projektkopie_faellt_die_Auswertung_auf_den_Katalog_zurueck()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung ausProjekt = GanglinienAuswertungCtrl.AusProjekt(
                GanglinienQuelle.Waermebedarf, 0, ZUGEORDNET);
            GanglinienAuswertung ausKatalog = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, ZUGEORDNET);

            Assert.True(ausProjekt.Erfolgreich);
            Assert.Equal(ausKatalog.JahresarbeitMwh, ausProjekt.JahresarbeitMwh, 6);
            Assert.Equal(ausKatalog.SpitzeKw, ausProjekt.SpitzeKw, 6);
        }

        /// <summary>
        /// <b>Strom und Waerme lesen verschiedene Tabellen.</b> Die Testdatenbank
        /// fuehrt den Namen <c>test</c> in BEIDEN Katalogen — mit verschiedenen
        /// Reihen. Waere die Auspraegung wirkungslos, kaeme zweimal dieselbe Zahl.
        /// </summary>
        [Fact]
        public void Die_Auspraegung_entscheidet_ueber_die_Tabelle()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung strom = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Strom, FREI);
            GanglinienAuswertung waerme = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, FREI);

            Assert.True(strom.Erfolgreich);
            Assert.True(waerme.Erfolgreich);
            Assert.NotEqual(strom.JahresarbeitMwh, waerme.JahresarbeitMwh, 3);
            Assert.Equal(6137.56, waerme.JahresarbeitMwh, 2);
        }

        /// <summary>Einen Namen, den es nicht gibt, meldet die Auswertung als Fehlschlag.</summary>
        [Fact]
        public void Ein_unbekannter_Name_ergibt_keine_Auswertung()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, "gibt es nicht");

            Assert.False(a.Erfolgreich);
            Assert.Empty(a.Stundenwerte);
            Assert.Null(a.VollbenutzungsstundenH);
        }

        // ==================================================================
        //  2 - Die zwei Loeschsperren
        // ==================================================================

        /// <summary>
        /// Die Projektzuordnungssperre: <c>Wärmebedarf_Laurentiuskirche</c> haengt in
        /// <c>Z_ProjektWaermebedarf</c>, <c>test</c> nicht.
        /// </summary>
        [Fact]
        public void HatProjektzuordnung_trennt_zugeordnete_von_freien_Ganglinien()
        {
            if (!_db.Vorhanden) return;

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();

            Assert.True(ctrl.HatProjektzuordnung(ZUGEORDNET));
            Assert.False(ctrl.HatProjektzuordnung(FREI));
            Assert.False(ctrl.HatProjektzuordnung("gibt es nicht"));
        }

        /// <summary>Ein Auslieferungssatz wird nicht geloescht.</summary>
        [Fact]
        public void Ein_Auslieferungssatz_wird_nicht_geloescht()
        {
            if (!_db.Vorhanden) return;

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();
            Assert.True(ctrl.IsReadOnly(AUSLIEFERUNG));

            Assert.False(ctrl.Delete(AUSLIEFERUNG));
            Assert.True(ctrl.GetStammId(AUSLIEFERUNG) > 0);
        }

        // ==================================================================
        //  3 - Exists und KopiereStamm ("Speichern unter")
        // ==================================================================

        /// <summary>
        /// <c>Exists</c> prueft den GANZEN Namen und nicht seinen Anfang — der
        /// Fehler, der beim Solarkatalog Befund W14-B70 war.
        /// </summary>
        [Fact]
        public void Exists_prueft_den_ganzen_Namen()
        {
            if (!_db.Vorhanden) return;

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();

            Assert.True(ctrl.Exists(FREI));
            Assert.False(ctrl.Exists("tes"));
            Assert.False(ctrl.Exists(""));
            Assert.False(ctrl.Exists(null));
        }

        /// <summary>
        /// Die Kopie traegt dieselben Werte unter neuem Namen — in derselben
        /// Reihenfolge (<c>ORDER BY ID</c>), damit die Zeitreihe erhalten bleibt.
        /// </summary>
        [Fact]
        public void Die_Kopie_traegt_dieselben_Werte_unter_neuem_Namen()
        {
            if (!_db.Vorhanden) return;

            const string KOPIE_WERTE = "W9E3-Kopie-Werte";
            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();

            int neu = ctrl.KopiereStamm(ZUGEORDNET, KOPIE_WERTE);
            Assert.True(neu > 0);

            Assert.Equal(Werte(ctrl.GetStammId(ZUGEORDNET)), Werte(neu));

            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, KOPIE_WERTE);
            Assert.True(a.Erfolgreich);
            Assert.Equal(65.4298, a.JahresarbeitMwh, 3);
        }

        /// <summary>Die Kopie eines Auslieferungssatzes ist Anwenderbestand — frei.</summary>
        [Fact]
        public void Die_Kopie_eines_Auslieferungssatzes_ist_frei()
        {
            if (!_db.Vorhanden) return;

            const string KOPIE_FREI = "W9E3-Kopie-Frei";
            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();

            Assert.True(ctrl.KopiereStamm(AUSLIEFERUNG, KOPIE_FREI) > 0);
            Assert.False(ctrl.IsReadOnly(KOPIE_FREI));
            Assert.True(ctrl.Delete(KOPIE_FREI));
        }

        /// <summary>
        /// Ein vergebener Name wird abgewiesen — mit <c>0</c> und ohne Zeile, nicht
        /// mit einem SQLite-UNIQUE-Fehler. Auch getrimmt.
        /// </summary>
        [Fact]
        public void Ein_vergebener_Name_wird_abgewiesen()
        {
            if (!_db.Vorhanden) return;

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();

            Assert.Equal(0, ctrl.KopiereStamm(ZUGEORDNET, FREI));
            Assert.Equal(0, ctrl.KopiereStamm(ZUGEORDNET, "  " + FREI + "  "));
        }

        /// <summary>Ohne Quelle oder ohne Namen entsteht keine Kopie.</summary>
        [Fact]
        public void Ohne_Quelle_oder_Namen_entsteht_keine_Kopie()
        {
            if (!_db.Vorhanden) return;

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();

            Assert.Equal(0, ctrl.KopiereStamm("gibt es nicht", "W9E3-Nichts"));
            Assert.Equal(0, ctrl.KopiereStamm(ZUGEORDNET, "   "));
            Assert.Equal(0, ctrl.KopiereStamm("", "W9E3-Nichts"));
            Assert.False(ctrl.Exists("W9E3-Nichts"));
        }

        // ==================================================================
        //  4 - Die zwei Schreibwege der Auspraegung
        // ==================================================================

        /// <summary>
        /// <c>ImportGanglinie</c> nimmt seit W9-E-3 die geprueffte Zahlenreihe der
        /// Kette entgegen (bis dahin eine rohe Zeilenliste) und legt Kopf und Werte
        /// in EINER Transaktion an.
        /// </summary>
        [Fact]
        public void ImportGanglinie_legt_Kopf_und_Werte_an()
        {
            if (!_db.Vorhanden) return;

            const string IMPORT_ZIEL = "W9E3-Import";
            List<double> werte = Reihe(8760, 100.0);

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();
            Assert.True(ctrl.ImportGanglinie(IMPORT_ZIEL, werte));

            int id = ctrl.GetStammId(IMPORT_ZIEL);
            Assert.True(id > 0);
            Assert.False(ctrl.IsReadOnly(IMPORT_ZIEL));
            Assert.Equal(8760, Werte(id).Count);

            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, IMPORT_ZIEL);
            Assert.True(a.Erfolgreich);
            Assert.Equal(876.0, a.JahresarbeitMwh, 3);
            Assert.Equal(100.0, a.SpitzeKw, 3);
        }

        /// <summary>
        /// <b>Eine leere Reihe wird nicht geschrieben.</b> Sonst stuende ein Kopfsatz
        /// ohne Werte im Katalog, und der Lauf faende eine Ganglinie mit 0 Werten.
        /// </summary>
        [Fact]
        public void Eine_leere_Reihe_wird_nicht_geschrieben()
        {
            if (!_db.Vorhanden) return;

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();

            Assert.False(ctrl.ImportGanglinie("W9E3-Leer", new List<double>()));
            Assert.False(ctrl.ImportGanglinie("W9E3-Leer", null));
            Assert.False(ctrl.Exists("W9E3-Leer"));
        }

        /// <summary>
        /// <b>Ueberschreiben laesst die Kopf-Id STEHEN</b> (W9-E-3). Bis dahin loeschte
        /// die Waermebedarfsverwaltung den ganzen Satz und legte ihn neu an — die
        /// Kopf-Id wechselte dabei, und wer sie sich gemerkt hatte, zeigte danach auf
        /// nichts.
        /// </summary>
        [Fact]
        public void ErsetzeGanglinie_tauscht_die_Werte_und_behaelt_die_Id()
        {
            if (!_db.Vorhanden) return;

            const string ERSETZ_ZIEL = "W9E3-Ersetzen";
            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();
            Assert.True(ctrl.ImportGanglinie(ERSETZ_ZIEL, Reihe(8760, 100.0)));

            int vorher = ctrl.GetStammId(ERSETZ_ZIEL);
            Assert.True(ctrl.ErsetzeGanglinie(ERSETZ_ZIEL, Reihe(8760, 250.0)));
            int nachher = ctrl.GetStammId(ERSETZ_ZIEL);

            Assert.Equal(vorher, nachher);
            Assert.Equal(8760, Werte(nachher).Count);

            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Waermebedarf, ERSETZ_ZIEL);
            Assert.Equal(250.0, a.SpitzeKw, 3);
            Assert.Equal(2190.0, a.JahresarbeitMwh, 3);
        }

        /// <summary>Ohne vorhandenen Satz ersetzt <c>ErsetzeGanglinie</c> nichts.</summary>
        [Fact]
        public void ErsetzeGanglinie_ohne_Satz_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();

            Assert.False(ctrl.ErsetzeGanglinie("gibt es nicht", Reihe(8760, 1.0)));
            Assert.False(ctrl.ErsetzeGanglinie(FREI, new List<double>()));
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static List<double> Reihe(int anzahl, double wert)
        {
            List<double> l = new List<double>(anzahl);
            for (int i = 0; i < anzahl; i++) l.Add(wert);
            return l;
        }

        /// <summary>Die Werte eines Katalogsatzes in Stamm-Reihenfolge.</summary>
        private static List<double> Werte(int idGanglinie)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Wert FROM Tab_WaermebedarfDaten_STAMM WHERE ID_Ganglinie = ? ORDER BY ID",
                new DbParam("@g", DbParamTyp.Integer) { Wert = idGanglinie });

            List<double> werte = new List<double>();
            if (dt == null) return werte;
            foreach (DataRow r in dt.Rows)
                werte.Add(r[0] == System.DBNull.Value ? 0 : System.Convert.ToDouble(r[0]));
            return werte;
        }

        /// <summary>Die Kopf-Id der Projektkopie eines Bezeichners.</summary>
        private static int KopieId(int idProjekt, string bezeichner)
            => WaermebedarfStammCtrl.GetProjektGanglinieId(bezeichner, idProjekt);
    }
}
