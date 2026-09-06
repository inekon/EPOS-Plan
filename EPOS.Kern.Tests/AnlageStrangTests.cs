using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis der Stufe S2 des Wechselrichterkonzepts</b> (Anwenderentscheide
    /// <b>W6‑E‑2</b> und <b>W6‑E‑3</b> vom 06.09.2026,
    /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c> 3.4, 3.5, 7.1 und 8/S2).
    ///
    /// <para><b>Was hier geprüft wird.</b> Das Schema des Migrationsschritts 66 (Tabelle
    /// und Spalte, Idempotenz), der Rundweg des <c>AnlageStrangCtrl</c> je Anlage
    /// (Lesen nach Rang, Schreiben als Ganzes, leere Liste löscht), die Löschweitergabe
    /// über <c>ID_Anlage</c> und der Rundweg des Schalters
    /// <c>PV_Wechselrichterweg</c> über die Anlagenzeile.</para>
    ///
    /// <para><b>Was hier NICHT geprüft wird: ein Rechenergebnis</b> — <b>S2 hat
    /// keines</b>. Kein Rechenweg liest die Strangzeilen oder den Schalter; der
    /// Referenzlauf bleibt byte-gleich. Die acht Auslegungsprüfungen stehen in
    /// <see cref="StrangPlausibilitaetTests"/>.</para>
    ///
    /// <para><b>Eine Arbeitskopie je Klasse</b> (Regel seit iU9‑W11a); fehlt die Datei,
    /// schweigen die Fälle. Die Klasse trägt <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AnlageStrangTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public AnlageStrangTests(TestDatenbank db) { _db = db; }

        // =================================================================================
        // 1 — Das Schema (Migrationsschritt 66)
        // =================================================================================

        /// <summary>
        /// Die Tabelle steht mit genau den zwölf Spalten des Konzepts 3.4 — in
        /// Schemareihenfolge, <c>ID</c> voran.
        /// </summary>
        [Fact]
        public void Die_Strangtabelle_fuehrt_die_zwoelf_Spalten_des_Konzepts()
        {
            if (!_db.Vorhanden) return;

            List<string> spalten = Spalten(SchemaKatalog.Z_ANLAGESTRANG);

            Assert.Equal(
                new[]
                {
                    "ID", "ID_Anlage", "Rang", "Bezeichner", "ID_Wechselrichter",
                    "Geraetenummer", "Mppt", "Module_Reihe", "Straenge_Parallel",
                    "Neigung", "Azimut", "ID_PV"
                },
                spalten.ToArray());
        }

        /// <summary>
        /// Die Spaltenliste des Schemas und <see cref="AnlageStrangSchema.Spalten"/>
        /// sind dieselbe Wahrheit — an ihr hängen Leseabfrage und <c>INSERT</c> des
        /// Controllers.
        /// </summary>
        [Fact]
        public void Die_Spaltenliste_deckt_das_Schema_ab()
        {
            if (!_db.Vorhanden) return;

            List<string> ohneId = Spalten(SchemaKatalog.Z_ANLAGESTRANG)
                                  .Where(s => !string.Equals(s, "ID", StringComparison.OrdinalIgnoreCase))
                                  .ToList();

            Assert.Equal(ohneId, AnlageStrangSchema.Spalten.ToList());
        }

        /// <summary>
        /// <b>Die zwei Fremdschlüssel</b> (Konzept 3.6): <c>ID_Anlage</c> auf
        /// <c>Tab_Energieanlagen</c> mit <c>ON DELETE CASCADE</c>,
        /// <c>ID_Wechselrichter</c> auf die PROJEKTKOPIE
        /// <c>Tab_Wechselrichter</c> — und ein dritter auf <c>Tab_PV</c> bewusst
        /// nicht (Begründung im Kopf von <see cref="AnlageStrangSchema"/>).
        /// </summary>
        [Fact]
        public void Die_Strangtabelle_fuehrt_genau_zwei_Fremdschluessel()
        {
            if (!_db.Vorhanden) return;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"table\", \"from\", \"on_delete\" FROM pragma_foreign_key_list('" +
                SchemaKatalog.Z_ANLAGESTRANG + "')");

            Assert.NotNull(dt);
            Assert.Equal(2, dt.Rows.Count);

            var nach = new Dictionary<string, (string Ziel, string Loeschweg)>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
                nach[Convert.ToString(r["from"])] =
                    (Convert.ToString(r["table"]), Convert.ToString(r["on_delete"]));

            Assert.Equal((SchemaKatalog.TAB_ENERGIEANLAGEN, "CASCADE"),
                         nach[AnlageStrangSchema.SPALTE_ID_ANLAGE]);
            Assert.Equal(SchemaKatalog.TAB_WECHSELRICHTER,
                         nach[AnlageStrangSchema.SPALTE_ID_WECHSELRICHTER].Ziel);
            Assert.DoesNotContain(AnlageStrangSchema.SPALTE_ID_PV, nach.Keys, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Der Schalter aus <b>W6‑E‑3</b> steht als Spalte an der Anlagenzeile
        /// (Konzept 7.1, Empfehlung a).
        /// </summary>
        [Fact]
        public void Der_Wechselrichterweg_steht_an_der_Anlagenzeile()
        {
            if (!_db.Vorhanden) return;

            Assert.Contains(SchemaKatalog.SPALTE_EA_PV_WECHSELRICHTERWEG,
                            Spalten(SchemaKatalog.TAB_ENERGIEANLAGEN),
                            StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <b>Der Migrationsschritt 66 ist idempotent</b>: Die Anweisung trägt
        /// <c>IF NOT EXISTS</c>, ein zweiter Lauf legt nichts an und ändert nichts. Der
        /// Fall setzt sie ein zweites Mal ab und prüft, dass Spaltenzahl und Inhalt
        /// gleich bleiben.
        /// </summary>
        [Fact]
        public void Der_Migrationsschritt_66_ist_idempotent()
        {
            if (!_db.Vorhanden) return;

            int anlage = AnlageAnlegen("Idempotenz 66");
            var ctrl = new AnlageStrangCtrl();
            Assert.True(ctrl.SchreibenJeAnlage(anlage, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Module_Reihe = 10 }
            }));

            foreach (KeyValuePair<string, string> a in AnlageStrangSchema.Anweisungen)
                Assert.True(DataRepository.ExecuteSQL(a.Value), a.Key + ": Zweitlauf schlug fehl");

            Assert.Equal(12, Spalten(SchemaKatalog.Z_ANLAGESTRANG).Count);

            List<AnlageStrangModel> nachher = ctrl.LesenJeAnlage(anlage);
            Assert.Single(nachher);
            Assert.Equal(10, nachher[0].Module_Reihe);

            AnlageLoeschen(anlage);
        }

        // =================================================================================
        // 2 — Der Rundweg des Controllers
        // =================================================================================

        /// <summary>
        /// Schreiben und Lesen je Anlage: Die Zeilen kommen in RANGFOLGE zurück, und
        /// jeder Wert steht unverändert darin — auch die Vorgabewerte, die NULL sind.
        /// </summary>
        [Fact]
        public void Die_Straenge_einer_Anlage_reisen_unveraendert_hin_und_zurueck()
        {
            if (!_db.Vorhanden) return;

            int anlage = AnlageAnlegen("Rundweg Strang");
            int geraet = WechselrichterAnlegen("Rundweg 2500TL");

            var ctrl = new AnlageStrangCtrl();
            Assert.True(ctrl.SchreibenJeAnlage(anlage, new List<AnlageStrangModel>
            {
                new AnlageStrangModel
                {
                    Bezeichner = "Dach Ost",
                    ID_Wechselrichter = geraet,
                    Geraetenummer = 1, Mppt = 1,
                    Module_Reihe = 11, Straenge_Parallel = 1,
                    Neigung = 25, Azimut = -90
                },
                new AnlageStrangModel
                {
                    Bezeichner = "Dach West",
                    ID_Wechselrichter = geraet,
                    Geraetenummer = 1, Mppt = 2,
                    Module_Reihe = 11, Straenge_Parallel = 1,
                    Neigung = 25, Azimut = 90
                }
            }));

            List<AnlageStrangModel> gelesen = ctrl.LesenJeAnlage(anlage);
            Assert.Equal(2, gelesen.Count);

            Assert.Equal(1, gelesen[0].Rang);
            Assert.Equal("Dach Ost", gelesen[0].Bezeichner);
            Assert.Equal(geraet, gelesen[0].ID_Wechselrichter);
            Assert.Equal(1, gelesen[0].Mppt);
            Assert.Equal(11, gelesen[0].Module_Reihe);
            Assert.Equal(-90, gelesen[0].Azimut);

            Assert.Equal(2, gelesen[1].Rang);
            Assert.Equal(2, gelesen[1].Mppt);
            Assert.Equal(90, gelesen[1].Azimut);

            AnlageLoeschen(anlage);
            WechselrichterLoeschen(geraet);
        }

        /// <summary>
        /// <b>NULL bleibt NULL</b>: Neigung und Azimut ohne Eintrag heissen „der
        /// Anlagenwert" (Konzept 3.4, Entwurfsentscheidung 2), und eine 0 wäre eine
        /// GÜLTIGE Ausrichtung (Süden). Wer beides gleichsetzt, macht aus einem
        /// geerbten Wert stillschweigend einen gepflegten.
        /// </summary>
        [Fact]
        public void Neigung_und_Azimut_ohne_Eintrag_bleiben_NULL()
        {
            if (!_db.Vorhanden) return;

            int anlage = AnlageAnlegen("Geerbte Ausrichtung");
            var ctrl = new AnlageStrangCtrl();

            Assert.True(ctrl.SchreibenJeAnlage(anlage, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Module_Reihe = 10, Neigung = null, Azimut = 0 }
            }));

            AnlageStrangModel z = Assert.Single(ctrl.LesenJeAnlage(anlage));
            Assert.Null(z.Neigung);
            Assert.Equal(0, z.Azimut);

            AnlageLoeschen(anlage);
        }

        /// <summary>
        /// Die Ränge werden beim Schreiben lückenlos ab 1 NEU vergeben — was der Dialog
        /// mitgibt, zählt als Reihenfolge, nicht als Wert (Bauart
        /// <c>Z_AnlageSenkeCtrl.SchreibenJeAnlage</c>).
        /// </summary>
        [Fact]
        public void Die_Raenge_werden_beim_Schreiben_neu_vergeben()
        {
            if (!_db.Vorhanden) return;

            int anlage = AnlageAnlegen("Rangvergabe");
            var ctrl = new AnlageStrangCtrl();

            Assert.True(ctrl.SchreibenJeAnlage(anlage, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Rang = 7, Module_Reihe = 8 },
                new AnlageStrangModel { Rang = 7, Module_Reihe = 9 }
            }));

            List<AnlageStrangModel> gelesen = ctrl.LesenJeAnlage(anlage);
            Assert.Equal(new[] { 1, 2 }, gelesen.Select(z => z.Rang).ToArray());
            Assert.Equal(new int?[] { 8, 9 }, gelesen.Select(z => z.Module_Reihe).ToList());

            AnlageLoeschen(anlage);
        }

        /// <summary>
        /// Eine LEERE Liste ist zulässig und löscht die Stränge der Anlage — der Weg,
        /// den die Oberfläche beim Entfernen der letzten Zeile braucht.
        /// </summary>
        [Fact]
        public void Eine_leere_Liste_loescht_die_Straenge()
        {
            if (!_db.Vorhanden) return;

            int anlage = AnlageAnlegen("Leeren");
            var ctrl = new AnlageStrangCtrl();

            Assert.True(ctrl.SchreibenJeAnlage(anlage, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Module_Reihe = 10 }
            }));
            Assert.Single(ctrl.LesenJeAnlage(anlage));

            Assert.True(ctrl.SchreibenJeAnlage(anlage, new List<AnlageStrangModel>()));
            Assert.Empty(ctrl.LesenJeAnlage(anlage));

            AnlageLoeschen(anlage);
        }

        /// <summary>
        /// <b>Die Löschweitergabe</b>: Fällt die Anlagenzeile, fallen ihre Strangzeilen
        /// mit. Genau das ist die Falle N3.3 — der Speicherweg jeder Anlage ist Löschen
        /// + Neuanlegen, und deshalb rettet <c>WizardCtrl</c> die Zeilen darüber hinweg
        /// (Block ST1).
        /// </summary>
        [Fact]
        public void Mit_der_Anlage_fallen_ihre_Straenge()
        {
            if (!_db.Vorhanden) return;

            int anlage = AnlageAnlegen("Kaskade");
            var ctrl = new AnlageStrangCtrl();
            Assert.True(ctrl.SchreibenJeAnlage(anlage, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Module_Reihe = 10 }
            }));

            AnlageLoeschen(anlage);

            Assert.Empty(ctrl.LesenJeAnlage(anlage));
        }

        /// <summary>
        /// <see cref="AnlageStrangCtrl.LesenJeProjekt"/> holt die Zeilen ALLER Anlagen
        /// eines Projekts in EINER Abfrage — der Weg, den die Rettung im Speicherweg
        /// und (ab S3) der Rechenkern nehmen.
        /// </summary>
        [Fact]
        public void Der_Projektleseweg_findet_die_Straenge_ueber_die_Anlage()
        {
            if (!_db.Vorhanden) return;

            int a1 = AnlageAnlegen("Projektweg A");
            int a2 = AnlageAnlegen("Projektweg B");
            var ctrl = new AnlageStrangCtrl();

            ctrl.SchreibenJeAnlage(a1, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Module_Reihe = 10 }
            });
            ctrl.SchreibenJeAnlage(a2, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Module_Reihe = 12 },
                new AnlageStrangModel { Module_Reihe = 14 }
            });

            List<AnlageStrangModel> alle = ctrl.LesenJeProjekt(TESTPROJEKT);
            Assert.Equal(3, alle.Count(z => z.ID_Anlage == a1 || z.ID_Anlage == a2));

            AnlageLoeschen(a1);
            AnlageLoeschen(a2);
        }

        /// <summary>
        /// <b>DIE FALLE N3.3, und ihre Behebung</b> (Konzept S2.2, Block ST1 des
        /// <c>WizardCtrl</c>). Der Speicherweg jeder Anlage ist Löschen + Neuanlegen;
        /// die Löschweitergabe nähme die Strangzeilen mit. Der Fall fährt genau diesen
        /// Weg — <c>Del_Projekt_Waermeerzeuger</c> gefolgt von
        /// <c>Add_WP_Waermeerzeuger</c> — und prüft, dass die Zuordnung danach an der
        /// NEUEN Anlagenzeile steht.
        /// </summary>
        /// <remarks>
        /// Der Fall arbeitet auf <c>ID_Type = PV</c> in Projekt <see cref="TESTPROJEKT"/>,
        /// weil dieses Projekt in der Testdatenbank KEINE PV-Anlage führt — der
        /// typgefilterte Löschbefehl trifft damit ausschliesslich die Zeile dieses
        /// Falles.
        /// </remarks>
        [Fact]
        public void Der_Speicherweg_rettet_die_Straenge_ueber_Loeschen_und_Neuanlegen()
        {
            if (!_db.Vorhanden) return;

            // Der Name steht als LOKALE Variable da, nicht als "const string name":
            // Werkzeuge/SqlDialektPruefer loest dynamische Tabellennamen ueber EINDEUTIGE
            // Kurznamen von Konstanten auf, und eine Konstante namens "name" waere im
            // ganzen Bestand die einzige - der Pruefer setzte sie dann fuer k.name in
            // ProjektExportImportCtrl ein und meldete vier Fundstellen.
            string bezeichner = "ST1 Rettungsprobe";

            var anlage = new WErzeugerCtrl
            {
                ID_Projekt = TESTPROJEKT,
                Bezeichner = bezeichner,
                ID_Type = WizardItemClass.PV_TYP,
                ID_PV = ModulAnlegen(),
                PV_Leistung = 21
            };
            Assert.True(anlage.Insert());

            int alteId = AnlagenId(bezeichner);
            int geraet = WechselrichterAnlegen("ST1 Rettung 5000TL");

            var ctrl = new AnlageStrangCtrl();
            Assert.True(ctrl.SchreibenJeAnlage(alteId, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Bezeichner = "Ost", ID_Wechselrichter = geraet,
                                        Mppt = 1, Module_Reihe = 11, Azimut = -90 },
                new AnlageStrangModel { Bezeichner = "West", ID_Wechselrichter = geraet,
                                        Mppt = 2, Module_Reihe = 10, Azimut = 90 }
            }));

            // Der Speicherweg, Zeichen fuer Zeichen: erst der typgefilterte Loeschbefehl,
            // dann das Neuanlegen aus der Dialogliste.
            var wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(TESTPROJEKT, WizardItemClass.PV_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(TESTPROJEKT,
                new List<WErzeugerModel> { anlage }));

            int neueId = AnlagenId(bezeichner);
            Assert.True(neueId > 0);

            List<AnlageStrangModel> gerettet = ctrl.LesenJeAnlage(neueId);
            Assert.Equal(2, gerettet.Count);
            Assert.Equal(new[] { "Ost", "West" }, gerettet.Select(z => z.Bezeichner).ToArray());
            Assert.Equal(new int?[] { 11, 10 }, gerettet.Select(z => z.Module_Reihe).ToList());
            Assert.Equal(new int?[] { -90, 90 }, gerettet.Select(z => z.Azimut).ToList());
            Assert.All(gerettet, z => Assert.Equal(geraet, z.ID_Wechselrichter));

            AnlageLoeschen(neueId);
            WechselrichterLoeschen(geraet);
        }

        // =================================================================================
        // 3 — Der Schalter aus W6-E-3 an der Anlagenzeile
        // =================================================================================

        /// <summary>
        /// Der Wechselrichterweg reist über <c>WErzeugerCtrl</c> und
        /// <c>AnlagenSql.SQL_ANLAGE_INSERT</c> unverändert hin und zurück — und
        /// <b>NULL bleibt NULL</b>: „nie gewählt" ist etwas anderes als „ausdrücklich
        /// vereinfacht", auch wenn beide gleich rechnen (Konzept 7.1).
        /// </summary>
        [Fact]
        public void Der_Wechselrichterweg_reist_unveraendert_hin_und_zurueck()
        {
            if (!_db.Vorhanden) return;

            AnlageAnlegen("Weg NULL");

            var mit = new WErzeugerCtrl
            {
                ID_Projekt = TESTPROJEKT,
                Bezeichner = "Weg KATALOG",
                ID_Type = WizardItemClass.PV_TYP,
                ID_PV = ModulAnlegen(),
                PV_Wechselrichterweg = DbWerte.PV_WR_WEG_KATALOG
            };
            Assert.True(mit.Insert());

            Assert.Null(WegLesen("Weg NULL"));
            Assert.Equal(DbWerte.PV_WR_WEG_KATALOG, WegLesen("Weg KATALOG"));

            AnlageLoeschen(AnlagenId("Weg NULL"));
            AnlageLoeschen(AnlagenId("Weg KATALOG"));
        }

        /// <summary>
        /// <b>Der Schalter ist eine MODELLspalte, keine Fachspalte</b>: Weil
        /// <c>SQL_ANLAGE_INSERT</c> ihn nennt, fällt er aus der Rettungsmenge
        /// <c>WizardCtrl.Fachspalten</c> heraus — es gibt keine zweite Liste, die
        /// hinterherhinken könnte.
        /// </summary>
        [Fact]
        public void Der_Wechselrichterweg_ist_keine_Fachspalte()
        {
            if (!_db.Vorhanden) return;

            Assert.DoesNotContain(SchemaKatalog.SPALTE_EA_PV_WECHSELRICHTERWEG,
                                  WizardCtrl.Fachspalten(), StringComparer.OrdinalIgnoreCase);
        }

        // =================================================================================
        // Hilfsmittel
        // =================================================================================

        /// <summary>Das Projekt der Testdatenbank, in dem die Wegwerf-Anlagen entstehen.</summary>
        private const int TESTPROJEKT = 1030;

        private static readonly StringComparer Vergleich = StringComparer.OrdinalIgnoreCase;

        private static List<string> Spalten(string tabelle)
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name FROM pragma_table_info('" + tabelle + "') ORDER BY cid");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows) liste.Add(Convert.ToString(r["name"]));
            return liste;
        }

        private static int AnlageAnlegen(string bezeichner)
        {
            var m = new WErzeugerCtrl
            {
                ID_Projekt = TESTPROJEKT,
                Bezeichner = bezeichner,
                ID_Type = WizardItemClass.PV_TYP,
                // Tab_Energieanlagen.ID_PV steht unter einer ERZWUNGENEN Beziehung auf
                // Tab_PV; eine 0 waere eine Phantom-Referenz und das INSERT schluege
                // fehl. Im Programm legt Add_WP_Waermeerzeuger die Projektkopie ueber
                // CopyFromStamm an - hier reicht eine Wegwerf-Modulzeile.
                ID_PV = ModulAnlegen()
            };
            Assert.True(m.Insert());
            return AnlagenId(bezeichner);
        }

        /// <summary>Die Projektkopie eines Wegwerf-Moduls; sie bleibt fuer die Dauer der
        /// Arbeitskopie stehen (Muster der uebrigen Wegwerfzeilen dieser Klasse).</summary>
        private static int ModulAnlegen()
        {
            if (m_Modul > 0) return m_Modul;

            int id = DataRepository.GetMaxID(SchemaKatalog.TAB_PV) + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [" + SchemaKatalog.TAB_PV + "] " +
                "(ID, ID_Projekt, Bezeichner, Leistung, U_Mpp, U_Leerlauf, " +
                " I_Kurzschluss, alpha_SC, beta_OC) VALUES (?,?,?,?,?,?,?,?,?)",
                new DbParam("@id", id), new DbParam("@p", TESTPROJEKT),
                new DbParam("@b", "Strangprobe 275"), new DbParam("@l", 275.19),
                new DbParam("@umpp", 31.4), new DbParam("@uoc", 38.4),
                new DbParam("@isc", 9.34), new DbParam("@a", 0.0047),
                new DbParam("@bo", -0.118)));

            m_Modul = id;
            return id;
        }

        private static int m_Modul;

        private static int AnlagenId(string bezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT MAX(ID) FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN +
                " WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", TESTPROJEKT), new DbParam("@b", bezeichner));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static string WegLesen(string bezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT " + SchemaKatalog.SPALTE_EA_PV_WECHSELRICHTERWEG +
                " FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN + " WHERE ID = ?",
                new DbParam("@id", AnlagenId(bezeichner)));
            return (v == null || v == DBNull.Value) ? null : Convert.ToString(v);
        }

        private static void AnlageLoeschen(int id)
        {
            if (id <= 0) return;
            DataRepository.ExecuteSQL(
                "DELETE FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN + " WHERE ID = ?",
                new DbParam("@id", id));
        }

        private static int WechselrichterAnlegen(string bezeichner)
        {
            int id = DataRepository.GetMaxID(WechselrichterCtrl.TABLE) + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [" + WechselrichterCtrl.TABLE + "] (ID, ID_Projekt, Bezeichner, P_AC_Nenn) " +
                "VALUES (?, ?, ?, ?)",
                new DbParam("@id", id), new DbParam("@p", TESTPROJEKT),
                new DbParam("@b", bezeichner), new DbParam("@n", 2.5)));
            return id;
        }

        private static void WechselrichterLoeschen(int id)
        {
            DataRepository.ExecuteSQL(
                "DELETE FROM [" + WechselrichterCtrl.TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));
        }
    }
}
