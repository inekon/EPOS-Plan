using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt <b>Zonen im Gebäudekatalog</b> (<see cref="ZonenKatalogSchema"/>, Schritt 204): Kette und
    /// Zielstand, die drei Katalogzwillinge mit den Fachspalten der Projekttabellen und ihren Fremdschlüsseln, die
    /// Eigentümerspalte <c>ID_Zone_Stamm</c> samt Teilindizes, die Hebung einer Kopie ohne den Schritt und die
    /// Einträge in Werkzeug, Migration und Testkopie.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZonenKatalogSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_folgt_auf_die_Typkennfelder_und_ist_das_Ziel()
        {
            Assert.Equal(KaeltemaschinenTypkennfelderSchema.SCHRITT + 1, ZonenKatalogSchema.SCHRITT);
            Assert.Equal(204, ZonenKatalogSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ZonenKatalogSchema.SCHRITT);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == ZonenKatalogSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Katalog);
        }

        [Fact]
        public void Die_Zwillinge_fuehren_jede_Fachspalte_der_Projekttabellen_und_sind_STRICT()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ZonenKatalogSchema.Vollstaendig());
            foreach (var (projekt, katalog) in new[]
                     {
                         (ZonenSchema.TAB_ZONE, ZonenKatalogSchema.TAB_ZONE),
                         (ZonenSchema.TAB_BAUTEIL, ZonenKatalogSchema.TAB_BAUTEIL),
                         (ZonenkopplungSchema.TAB_LUFTSTROM, ZonenKatalogSchema.TAB_LUFTSTROM),
                     })
            {
                // Eine neue Spalte der Projekttabelle gehoert auch an den Zwilling - sonst reist sie nicht.
                Assert.Equal(DataRepository.SpaltenVonTabelle(projekt), DataRepository.SpaltenVonTabelle(katalog));
                string sql = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", katalog)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", sql.TrimEnd());
            }
        }

        [Fact]
        public void Die_Fremdschluessel_zeigen_in_den_Katalog_und_kaskadieren()
        {
            if (!_db.Vorhanden) return;
            var fk = new Dictionary<string, List<(string Von, string Ziel, string Loeschen)>>();
            foreach (string t in new[] { ZonenKatalogSchema.TAB_ZONE, ZonenKatalogSchema.TAB_BAUTEIL, ZonenKatalogSchema.TAB_LUFTSTROM,
                                         KonditionierungSchema.TAB_KALENDER, KonditionierungSchema.TAB_VORGABE })
            {
                DataTable dt = DataRepository.GetDataTable("SELECT \"from\", \"table\", on_delete FROM pragma_foreign_key_list(?)",
                                                           new DbParam("@t", t));
                fk[t] = dt.Rows.Cast<DataRow>().Select(r => (Convert.ToString(r[0]), Convert.ToString(r[1]), Convert.ToString(r[2]))).ToList();
            }
            Assert.Contains(("ID_Gebaeude", "Tab_Gebaeude_STAMM", "CASCADE"), fk[ZonenKatalogSchema.TAB_ZONE]);
            Assert.Contains(("ID_Zone", "Tab_Zone_STAMM", "CASCADE"), fk[ZonenKatalogSchema.TAB_BAUTEIL]);
            Assert.Contains(fk[ZonenKatalogSchema.TAB_BAUTEIL], f => f.Von == "ID_Nachbarzone" && f.Ziel == "Tab_Zone_STAMM");
            Assert.Contains(("ID_Aufbau", "Tab_Bauteilaufbau_STAMM", "SET NULL"), fk[ZonenKatalogSchema.TAB_BAUTEIL]);
            Assert.Contains(("ID_ZoneA", "Tab_Zone_STAMM", "CASCADE"), fk[ZonenKatalogSchema.TAB_LUFTSTROM]);
            Assert.Contains(("ID_ZoneB", "Tab_Zone_STAMM", "CASCADE"), fk[ZonenKatalogSchema.TAB_LUFTSTROM]);
            Assert.Contains(("ID_Zone_Stamm", "Tab_Zone_STAMM", "CASCADE"), fk[KonditionierungSchema.TAB_KALENDER]);
            Assert.Contains(("ID_Zone_Stamm", "Tab_Zone_STAMM", "CASCADE"), fk[KonditionierungSchema.TAB_VORGABE]);
        }

        /// <summary>Eine Zeile mit Katalogzone ohne Katalogbau weist die Prüfklausel ab.</summary>
        [Fact]
        public void Eine_Katalogzone_ohne_Katalogbau_weist_die_Pruefklausel_ab()
        {
            if (!_db.Vorhanden) return;
            long stamm = Zahl("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\"");
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone_STAMM\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\") VALUES (?, 1, 'Pruefzone')",
                new DbParam("@g", stamm)));
            long zone = Zahl("SELECT MAX(ID) FROM \"Tab_Zone_STAMM\"");
            Assert.False(DataRepository.ExecuteSQL(
                    "INSERT INTO \"Tab_Konditionierungskalender\" (\"ID_Gebaeude\", \"ID_Zone_Stamm\", \"Groesse\", \"Wert\") " +
                    "VALUES (?, ?, 'HEIZSOLL', 20.0)",
                    new DbParam("@g", Zahl("SELECT MIN(ID) FROM \"Tab_Gebaeude\"")), new DbParam("@z", zone)));
        }

        /// <summary>
        /// Eine Kopie ohne die Zwillinge und mit den alten Teilindizes des Katalogbaus (die Spalte bleibt: SQLite
        /// entfernt keine Spalte mit Fremdschlüssel) wird gehoben; ein zweiter Lauf hat nichts zu tun.
        /// </summary>
        [Fact]
        public void Der_Schritt_hebt_eine_Kopie_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach (string t in new[] { ZonenKatalogSchema.TAB_LUFTSTROM, ZonenKatalogSchema.TAB_BAUTEIL, ZonenKatalogSchema.TAB_ZONE })
                Assert.True(DataRepository.ExecuteSQL("DROP TABLE \"" + t + "\""));
            foreach (KeyValuePair<string, string> i in ZonenKatalogSchema.Katalogbauindizes)
                DataRepository.ExecuteSQL("DROP INDEX \"" + i.Key + "\"");
            Assert.True(DataRepository.ExecuteSQL("CREATE UNIQUE INDEX \"idx_KondKalender_EindeutigGebaeudeStamm\" ON " +
                                                  "\"Tab_Konditionierungskalender\" (\"ID_Gebaeude_Stamm\", \"Groesse\") WHERE \"ID_Gebaeude_Stamm\" IS NOT NULL"));
            Assert.True(DataRepository.ExecuteSQL("CREATE UNIQUE INDEX \"idx_KondVorgabe_EindeutigGebaeudeStamm\" ON " +
                                                  "\"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude_Stamm\", \"Groesse\", \"Zeile\") WHERE \"ID_Gebaeude_Stamm\" IS NOT NULL"));
            Assert.False(ZonenKatalogSchema.Vollstaendig());

            var bericht = new List<string>();
            Assert.Equal(3, ZonenKatalogSchema.Ausfuehren(bericht));
            Assert.Single(bericht);
            Assert.True(ZonenKatalogSchema.Vollstaendig());

            Assert.Equal(0, ZonenKatalogSchema.Ausfuehren(null));
            Assert.True(ZonenKatalogSchema.Vollstaendig());
        }

        /// <summary>Werkzeug, Migration und Testkopie führen den Schritt hinter 203; die Testdatenbank trägt ihn.</summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int w = werkzeug.IndexOf("ZonenKatalogSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(w > 0 && w > werkzeug.IndexOf("KaeltemaschinenTypkennfelderSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_ZONEN_KATALOG = ZonenKatalogSchema.SCHRITT", migration, StringComparison.Ordinal);
            int vorher = migration.IndexOf("new Schritt(SCHRITT_KAELTEMASCHINEN_TYPKENNFELDER", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_ZONEN_KATALOG", StringComparison.Ordinal);
            Assert.True(vorher > 0 && ort > vorher, "Der Schritt steht nicht hinter 203.");
            Assert.Contains("ZonenKatalogSchema.Ausfuehren(bericht)", migration, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int v = vorrichtung.IndexOf("ZonenKatalogSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(v > 0 && v > vorrichtung.IndexOf("KaeltemaschinenTypkennfelderSchema.Ausfuehren(null)", StringComparison.Ordinal));

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT SchemaVersion FROM Tab_Applikation";
            Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) >= ZonenKatalogSchema.SCHRITT);
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN " +
                              "('Tab_Zone_STAMM', 'Tab_Bauteil_STAMM', 'Tab_Zonenluftstrom_STAMM')";
            Assert.Equal(3L, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            // Die Testdatenbank traegt keine Katalogzone: Der Schritt ist reines DDL.
            cmd.CommandText = "SELECT COUNT(*) FROM \"Tab_Zone_STAMM\"";
            Assert.Equal(0L, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
        }

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);
    }
}
