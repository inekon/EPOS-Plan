using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Aufräumlauf <see cref="GeraeteWaisen"/> — die Probe dafür, dass er im KERN
    /// liegt und dort dasselbe leistet wie zuvor in der Windows-Schale.
    ///
    /// <para><b>Warum es diese Fälle braucht.</b> Der Lauf LÖSCHT, und er entscheidet
    /// dabei über Gerätezeilen, an denen <c>Tab_Energieanlagen</c> mit Löschweitergabe
    /// hängt: Eine falsch als verwaist erkannte Zeile risse ihre Anlagenzeile lautlos
    /// mit. Gemessen wird deshalb in beide Richtungen — was er NICHT anfasst (Fall 1
    /// und 3) wiegt schwerer als was er entfernt (Fall 2).</para>
    ///
    /// <para><b>Die Fälle schreiben in der ARBEITSKOPIE</b> (<see cref="TestDatenbank"/>,
    /// je Fall eine frische 77-MB-Kopie). Der synthetische Zustand trägt Schlüssel weit
    /// oberhalb des Bestands; die Referenzprojekte bleiben unberührt, die Einfrierregeln
    /// gelten weiter.</para>
    ///
    /// <para><b>Nach jedem Lauf ein <c>foreign_key_check</c>.</b> Er ist die
    /// Gegenmessung zur Löschweitergabe: Bliebe eine Kindzeile ohne Elternzeile
    /// stehen, stünde sie hier.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GeraeteWaisenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Projekt 1045 führt zwei PV-Modulkopien — beide bleiben stehen.</summary>
        private const int PV_PROJEKT = 1045;
        private const int PV_DIREKT = 1015248;
        private const int PV_NUR_STRANG = 1015249;

        /// <summary>Projekt 1030 führt keine Wärmepumpe — Fall 2 legt dort seine an.</summary>
        private const int WP_PROJEKT = 1030;
        private const int G_WP_WAISE = 249101;
        private const int K_WP_1 = 249111;
        private const int K_WP_2 = 249112;

        /// <summary>Der synthetische Zustand des dritten Falls.</summary>
        private const int PROJEKT = 249001;
        private const int G_PV_STRANG = 249201;
        private const int G_PV_WAISE = 249202;
        private const int A_PV = 249301;
        private const int Z_STRANG = 249401;

        // =====================================================================
        //  1. Die frische Kopie ist geräumt — der Lauf findet nichts
        // =====================================================================

        /// <summary>
        /// <b>Auf dem ausgelieferten Stand ist nichts zu tun.</b> Für JEDES Projekt aus
        /// <c>Tab_Projekt</c> meldet <see cref="GeraeteWaisen.Aufraeumen"/> 0 Geräte und
        /// 0 Kindzeilen, und <see cref="GeraeteWaisen.Waisen"/> gibt für jedes Gewerk
        /// <c>sicher = true</c> zurück — „unbekannte Verweise" hieße hier „nichts
        /// löschen" und bliebe unbemerkt, wenn nur die Zahlen geprüft würden.
        ///
        /// <para>Projekt 1045 ist die Gegenprobe mit Substanz: Seine beiden
        /// <c>Tab_PV</c>-Kopien stehen vorher und nachher da — die eine am
        /// <c>Tab_Energieanlagen.ID_PV</c> ihrer Anlagenzeile, die andere ALLEIN am
        /// <c>Z_AnlageStrang.ID_PV</c> eines Strangs.</para>
        /// </summary>
        [Fact]
        public void Auf_der_frischen_Kopie_findet_der_Lauf_in_keinem_Projekt_eine_Waise()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<int> projekte = Projekte();
            Assert.NotEmpty(projekte);

            foreach (int id in projekte)
            {
                foreach (KomponentenUebernahmeCtrl.GewerkPlan plan in KomponentenUebernahmeCtrl.Plaene.Values)
                {
                    bool sicher;
                    List<int> waisen = GeraeteWaisen.Waisen(plan, id, out sicher);
                    Assert.True(sicher, "Projekt " + id + ", " + plan.Gewerk +
                                        ": Die Verweise liessen sich nicht vollstaendig lesen.");
                    Assert.Empty(waisen);
                }

                GeraeteWaisen.Bericht b = GeraeteWaisen.Aufraeumen(id);
                Assert.False(b.Unvollstaendig, "Projekt " + id + ": " + string.Join(" | ", b.Notizen));
                Assert.Equal(0, b.Geraete);
                Assert.Equal(0, b.Kindzeilen);
                Assert.False(b.EtwasGetan);
            }

            Assert.Equal(new[] { PV_DIREKT, PV_NUR_STRANG }, Ids(
                "SELECT ID FROM Tab_PV WHERE ID_Projekt = " + PV_PROJEKT + " ORDER BY ID"));
            Assert.Equal(0, Fremdschluesselfehler());
        }

        // =====================================================================
        //  2. Eine verwaiste Gerätezeile fällt — mit ihren Kindzeilen
        // =====================================================================

        /// <summary>
        /// <b>Die Wärmepumpe ohne Anlagenzeile geht, ihre Kennlinien gehen mit.</b> In
        /// Projekt 1030 — das keine Wärmepumpe führt — entsteht EINE
        /// <c>Tab_WP</c>-Zeile, auf die keine Zeile in <c>Tab_Energieanlagen</c> zeigt,
        /// dazu zwei <c>Tab_Kenndaten</c>-Kindzeilen.
        ///
        /// <para>Gemessen werden die Zeilenzahlen der fünf berührbaren Tabellen vorher
        /// und nachher: <c>Tab_WP</c> und <c>Tab_Kenndaten</c> stehen hinterher wieder
        /// auf ihrem Ausgangswert, <c>Tab_Energieanlagen</c>, <c>Tab_PV</c> und
        /// <c>Tab_Pufferspeicher</c> haben sich nicht bewegt. Die Kindzeilen fielen auch
        /// über die Löschweitergabe <c>Tab_WP.ID → Tab_Kenndaten.ID_WP</c>; der Lauf
        /// löscht sie trotzdem ausdrücklich, und nur deshalb ist ihre ZAHL im Bericht
        /// nachweisbar.</para>
        /// </summary>
        [Fact]
        public void Eine_verwaiste_Geraetezeile_faellt_mit_ihren_Kindzeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int wpVorher = Anzahl("Tab_WP");
            int kenndatenVorher = Anzahl("Tab_Kenndaten");
            int anlagenVorher = Anzahl("Tab_Energieanlagen");
            int pvVorher = Anzahl("Tab_PV");
            int pufferVorher = Anzahl("Tab_Pufferspeicher");

            Assert.Equal(0, Ganz("SELECT COUNT(*) FROM Tab_WP WHERE ID_Projekt = " + WP_PROJEKT));

            WaermepumpeOhneAnlage();
            Assert.Equal(wpVorher + 1, Anzahl("Tab_WP"));
            Assert.Equal(kenndatenVorher + 2, Anzahl("Tab_Kenndaten"));

            GeraeteWaisen.Bericht b = GeraeteWaisen.Aufraeumen(WP_PROJEKT);

            Assert.False(b.Unvollstaendig, string.Join(" | ", b.Notizen));
            Assert.Equal(1, b.Geraete);
            Assert.Equal(2, b.Kindzeilen);
            Assert.True(b.EtwasGetan);

            Assert.Equal(wpVorher, Anzahl("Tab_WP"));
            Assert.Equal(kenndatenVorher, Anzahl("Tab_Kenndaten"));
            Assert.Equal(anlagenVorher, Anzahl("Tab_Energieanlagen"));
            Assert.Equal(pvVorher, Anzahl("Tab_PV"));
            Assert.Equal(pufferVorher, Anzahl("Tab_Pufferspeicher"));
            Assert.Equal(0, Ganz("SELECT COUNT(*) FROM Tab_Kenndaten WHERE ID_WP = " + G_WP_WAISE));
            Assert.Equal(0, Fremdschluesselfehler());
        }

        // =====================================================================
        //  3. Der Strangverweis schützt das Modul
        // =====================================================================

        /// <summary>
        /// <b><c>Z_AnlageStrang.ID_PV</c> zählt wie ein Verweis aus
        /// <c>Tab_Energieanlagen</c>.</b> Ein Strang trägt den abweichenden Modultyp
        /// EINES Strangs; führt die Anlagenzeile ein anderes Modul, ist die Strangzeile
        /// der EINZIGE Verweis auf diese Projektkopie.
        ///
        /// <para>Der Fall stellt beide Seiten nebeneinander: zwei
        /// <c>Tab_PV</c>-Kopien desselben Projekts, die eine am Strang, die andere ohne
        /// jeden Verweis. Die Anlagenzeile führt <c>ID_PV = NULL</c> — ohne die Abfrage
        /// auf <c>Z_AnlageStrang</c> fielen BEIDE.</para>
        /// </summary>
        [Fact]
        public void Ein_PV_Modul_am_Strangverweis_bleibt_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektMitStrangmodulAnlegen();

            bool sicher;
            List<int> waisen = GeraeteWaisen.Waisen(
                KomponentenUebernahmeCtrl.Plaene["Photovoltaik"], PROJEKT, out sicher);
            Assert.True(sicher);
            Assert.Equal(new[] { G_PV_WAISE }, waisen.ToArray());

            GeraeteWaisen.Bericht b = GeraeteWaisen.Aufraeumen(PROJEKT);

            Assert.False(b.Unvollstaendig, string.Join(" | ", b.Notizen));
            Assert.Equal(1, b.Geraete);
            Assert.Equal(0, b.Kindzeilen);

            Assert.Equal(new[] { G_PV_STRANG }, Ids(
                "SELECT ID FROM Tab_PV WHERE ID_Projekt = " + PROJEKT + " ORDER BY ID"));
            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Z_AnlageStrang WHERE ID = " + Z_STRANG +
                                 " AND ID_PV = " + G_PV_STRANG));
            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID = " + A_PV));
            Assert.Equal(0, Fremdschluesselfehler());
        }

        // =====================================================================
        //  Prüfstand
        // =====================================================================

        /// <summary>
        /// Eine Wärmepumpe OHNE Anlagenzeile, mit zwei Kennlinienpunkten. Sie ist damit
        /// genau das, was der Del+Add-Speicherweg und das Projekt-Löschen hinterlassen.
        /// </summary>
        private static void WaermepumpeOhneAnlage()
        {
            Sql("INSERT INTO Tab_WP (ID, Bezeichner, ID_Projekt, Nennleistung, maxPtherm) " +
                "VALUES (" + G_WP_WAISE + ", 'Pruef-Waise', " + WP_PROJEKT + ", 10, 12)");
            Kennlinienpunkt(K_WP_1, vorlauf: 35, temperatur: 7, cop: 4.2, ptherm: 11.0);
            Kennlinienpunkt(K_WP_2, vorlauf: 55, temperatur: -7, cop: 2.4, ptherm: 8.5);
        }

        private static void Kennlinienpunkt(int id, int vorlauf, int temperatur, double cop, double ptherm)
            => Sql("INSERT INTO Tab_Kenndaten (ID, ID_Projekt, ID_WP, Vorlauf, Temperatur, COP, Ptherm) " +
                   "VALUES (" + id + ", " + WP_PROJEKT + ", " + G_WP_WAISE + ", " + vorlauf + ", " +
                   temperatur + ", " + Z(cop) + ", " + Z(ptherm) + ")");

        /// <summary>
        /// Der synthetische Zustand des dritten Falls: ein Projekt, zwei PV-Kopien, eine
        /// PV-Anlagenzeile OHNE <c>ID_PV</c> und ein Strang, der auf die erste Kopie
        /// zeigt.
        /// </summary>
        private static void ProjektMitStrangmodulAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" + PROJEKT + ", 'Pruefung Strangmodul')");
            PvKopie(G_PV_STRANG, "Modul am Strang");
            PvKopie(G_PV_WAISE, "Modul ohne Verweis");
            PvAnlageOhneModul();
            Sql("INSERT INTO Z_AnlageStrang (ID, ID_Anlage, Rang, Bezeichner, ID_PV) " +
                "VALUES (" + Z_STRANG + ", " + A_PV + ", 1, 'Strang 1', " + G_PV_STRANG + ")");
        }

        private static void PvKopie(int id, string bezeichner)
            => Sql("INSERT INTO Tab_PV (ID, ID_Projekt, Bezeichner, Leistung, Wirkungsgrad) " +
                   "VALUES (" + id + ", " + PROJEKT + ", '" + bezeichner + "', " +
                   Z(400.0) + ", " + Z(0.21) + ")");

        /// <summary>
        /// Die sieben Geräte-Verweisspalten stehen ausdrücklich auf <c>NULL</c> — ihr
        /// Vorgabewert 0 wäre ein Fremdschlüssel auf ein Gerät, das es nicht gibt
        /// (Muster <see cref="SpeicherFlottenUebernahmeTests"/>).
        /// </summary>
        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void PvAnlageOhneModul()
        {
            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = A_PV + ", " + PROJEKT + ", 'PV-Anlage', " + WizardItemClass.PV_TYP;
            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", NULL";
            }
            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
        }

        /// <summary>
        /// Die Gegenmessung nach jedem Lauf. Gelesen über die Tabellenfunktion
        /// <c>pragma_foreign_key_check</c> statt über <c>PRAGMA foreign_key_check</c>:
        /// So kommt EINE ganzzahlige Spalte zurück, und der Weg umgeht die
        /// Typlosigkeit einer PRAGMA-Ergebnismenge.
        /// </summary>
        private static int Fremdschluesselfehler()
            => Ganz("SELECT COUNT(*) FROM pragma_foreign_key_check");

        private static List<int> Projekte() => Ids("SELECT ID FROM Tab_Projekt ORDER BY ID");

        private static List<int> Ids(string sql)
        {
            var liste = new List<int>();
            DataTable t = DataRepository.GetDataTable(sql);
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                if (r[0] != null && r[0] != DBNull.Value)
                    liste.Add(Convert.ToInt32(r[0], CultureInfo.InvariantCulture));
            return liste;
        }

        private static int Anzahl(string tabelle) => Ganz("SELECT COUNT(*) FROM [" + tabelle + "]");

        private static int Ganz(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static string Z(double wert) => wert.ToString(CultureInfo.InvariantCulture);

        private static void Sql(string sql)
        {
            Assert.True(DataRepository.ExecuteSQL(sql), sql);
        }
    }
}
