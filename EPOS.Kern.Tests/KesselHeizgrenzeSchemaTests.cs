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
    /// Der Schemaschritt der <b>Heizgrenze der Kesselbereitschaft</b> (Anwenderentscheid 27.09.2026 zu
    /// #568; Nummer bei <see cref="KesselHeizgrenzeSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer (lückenlos hinter 151) und ihr Eintrag im Register der
    /// Paketanhebung; die Definition der Spalte (nullbares <c>REAL</c> ohne Vorgabe an einer
    /// STRICT-Tabelle); der Stand der Testdatenbank (die Spalte steht und ist überall leer); der
    /// Schritt aus dem Stand davor, wiederholbar und ohne DML; Projektduplikat und Projekttransfer
    /// tragen den Wert; Migration, Werkzeug und Testkopie führen den Schritt aus derselben
    /// Quelle, und die Repo-Datei trägt ihn.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselHeizgrenzeSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Projekt 1007 „Laurentiuskirche" mit einem Einstellungssatz.</summary>
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int PROJEKT = 1007;

        /// <summary>Eine erfundene Heizgrenze für die Proben.</summary>
        private const double GRENZE = 12.5;

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf 152 (KP1b); der Zielstand reicht bis zu ihr.</summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_auf_152()
        {
            Assert.Equal(KonditionierungVorlagenSchema.SCHRITT + 1, KesselHeizgrenzeSchema.SCHRITT);
            Assert.Equal(153, KesselHeizgrenzeSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KesselHeizgrenzeSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KesselHeizgrenzeSchema.SCHRITT + ".");
        }

        /// <summary>Das Register der Paketanhebung führt den Schritt als reines DDL.</summary>
        [Fact]
        public void Das_Register_der_Paketanhebung_fuehrt_den_Schritt_als_DDL()
        {
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KesselHeizgrenzeSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.False(string.IsNullOrWhiteSpace(s.Text));
        }

        /// <summary>
        /// Die Spalte: <c>REAL</c>, nullbar, ohne Vorgabe — an einer STRICT-Tabelle nimmt sie eine Zahl
        /// und NULL, aber keinen Text.
        /// </summary>
        [Fact]
        public void Die_Spalte_ist_ein_nullbares_REAL_ohne_Vorgabe()
        {
            Assert.Equal("Tab_Einstellungen", KesselHeizgrenzeSchema.TABELLE);
            Assert.Equal("Kessel_Heizgrenze", KesselHeizgrenzeSchema.SPALTE);
            Assert.Equal("REAL", KesselHeizgrenzeSchema.TYP);

            using SqliteConnection c = Speicher();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + KesselHeizgrenzeSchema.SPALTE + "\" " + KesselHeizgrenzeSchema.TYP);
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (2)");
            Assert.True(Leer(c, "SELECT Kessel_Heizgrenze FROM T WHERE ID = 1"), "Ein Bestandssatz steht auf NULL.");
            Assert.True(Leer(c, "SELECT Kessel_Heizgrenze FROM T WHERE ID = 2"), "Ein neuer Satz steht auf NULL.");
            foreach (string gut in new[] { "15", "12.5", "0", "30", "NULL" })
                Assert.False(Wirft(c, "UPDATE T SET Kessel_Heizgrenze = " + gut), gut);
            Assert.True(Wirft(c, "UPDATE T SET Kessel_Heizgrenze = 'fünfzehn'"), "Text an REAL einer STRICT-Tabelle.");
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und der Schritt aus dem Stand davor
        // =============================================================================

        /// <summary>Die Spalte steht, ist überall leer, und Tab_Einstellungen bleibt STRICT.</summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Zielstand_und_die_Heizgrenze_ist_leer()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KesselHeizgrenzeSchema.SCHRITT);
            Assert.True(KesselHeizgrenzeSchema.Vollstaendig());
            Assert.Empty(KesselHeizgrenzeSchema.Anweisungen);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Einstellungen") > 0);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Kessel_Heizgrenze IS NOT NULL"));

            string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                new DbParam("?", KesselHeizgrenzeSchema.TABELLE)), CultureInfo.InvariantCulture);
            Assert.EndsWith("STRICT", ddl.TrimEnd());
        }

        /// <summary>
        /// Der Schritt aus dem Stand VOR ihm (151): Die Spalte wird entfernt; der Schritt legt sie an,
        /// die Bestandswerte der Zeilen bleiben, und ein zweiter Lauf legt nichts mehr an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand();
            DataRepository.ExecuteNonQuery("ALTER TABLE \"" + KesselHeizgrenzeSchema.TABELLE + "\" DROP COLUMN \"" +
                                           KesselHeizgrenzeSchema.SPALTE + "\"");
            Assert.False(KesselHeizgrenzeSchema.Vollstaendig());
            Assert.Single(KesselHeizgrenzeSchema.Anweisungen);

            var bericht = new List<string>();
            Assert.Equal(1, KesselHeizgrenzeSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Kessel_Heizgrenze anlegen"));
            Assert.True(KesselHeizgrenzeSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Kessel_Heizgrenze IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, KesselHeizgrenzeSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        // =============================================================================
        //  Teil 3 - die Wege über die ganze Zeile
        // =============================================================================

        /// <summary>Das Projektduplikat trägt die Heizgrenze.</summary>
        [Fact]
        public void Projektduplikat_traegt_die_Heizgrenze()
        {
            if (!_db.Vorhanden) return;

            SetzeGrenze(PROJEKT, GRENZE);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Heizgrenze");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(GRENZE, Grenze(neu));
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt die Heizgrenze über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_die_Heizgrenze()
        {
            if (!_db.Vorhanden) return;

            SetzeGrenze(PROJEKT, GRENZE);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-heizgrenze-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Heizgrenze", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(GRENZE, Grenze(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Teil 4 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und die
        /// Nachzieh-Liste der Tests führen den Schritt aus derselben Quelle, hinter KP-S1; die REPO-Datei
        /// trägt die Spalte leer (gelesen nur lesend und ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wGrenze = werkzeug.IndexOf("KesselHeizgrenzeSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wGrenze > werkzeug.IndexOf("KonditionierungSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Das Werkzeug führt den Schritt nicht hinter KP-S1.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KESSEL_HEIZGRENZE = KesselHeizgrenzeSchema.SCHRITT", migration);
            int ortKond = migration.IndexOf("new Schritt(SCHRITT_KONDITIONIERUNG", StringComparison.Ordinal);
            int ortGrenze = migration.IndexOf("new Schritt(SCHRITT_KESSEL_HEIZGRENZE", StringComparison.Ordinal);
            Assert.True(ortKond > 0 && ortGrenze > ortKond, "Der Schritt steht nicht hinter 151.");
            Assert.Contains("KesselHeizgrenzeSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vGrenze = vorrichtung.IndexOf("KesselHeizgrenzeSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vGrenze > vorrichtung.IndexOf("KonditionierungSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Die Testkopie führt den Schritt nicht hinter KP-S1.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= KesselHeizgrenzeSchema.SCHRITT);
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_Einstellungen') " +
                                              "WHERE name = 'Kessel_Heizgrenze' AND type = 'REAL' AND \"notnull\" = 0 " +
                                              "AND dflt_value IS NULL"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Einstellungen WHERE Kessel_Heizgrenze IS NOT NULL"));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static void SetzeGrenze(int projekt, double grenze)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Einstellungen SET Kessel_Heizgrenze = ? WHERE ID_Projekt = ?",
                new DbParam("?", grenze), new DbParam("?", projekt)));
        }

        private static double Grenze(int projekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Kessel_Heizgrenze FROM Tab_Einstellungen WHERE ID_Projekt = ?", new DbParam("?", projekt));
            Assert.True(dt != null && dt.Rows.Count == 1, "Kein eindeutiger Einstellungssatz für " + projekt + ".");
            return Convert.ToDouble(dt.Rows[0][0], CultureInfo.InvariantCulture);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Zeilen von Tab_Einstellungen ohne die neue Spalte — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt, Kessel_Betriebsbereitschaft, Tool_1, Tool_2, Tool_3, Tool_4, Kuehlbetrieb, " +
                "Anlagenkopplung FROM Tab_Einstellungen ORDER BY ID");
            foreach (DataRow r in dt.Rows)
                liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            return liste;
        }

        /// <summary>Eine leere Datenbank im Speicher — für die Prüfungen der Definition ohne Testdatenbank.</summary>
        private static SqliteConnection Speicher()
        {
            var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            return c;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static bool Leer(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar() is DBNull;
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try
            {
                Ausfuehren(c, sql);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
