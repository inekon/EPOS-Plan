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
    /// Der Schemaschritt der <b>Kennlinienspalten des Heizkessels</b> (Konzept Kesselkennlinie 3.1
    /// und 3.2, Etappe E1, #569; Nummer bei <see cref="KesselKennlinieSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer (lückenlos hinter den Verfahrensvolumina der
    /// Füllstandslinie, Schritt 155) und ihr Eintrag im
    /// Register der Paketanhebung; die Definition der fünf Spalten an einer STRICT-Tabelle (vier
    /// nullbar ohne Vorgabe, der Schalter 0/1 mit Vorgabe 0); der Stand der Testdatenbank (die
    /// Spalten stehen, allein das Referenzprojekt 1050 trägt Werte); der Schritt aus dem Stand davor,
    /// wiederholbar und ohne DML; Projektduplikat und Projekttransfer tragen die Werte; Migration,
    /// Werkzeug und Testkopie führen den Schritt aus derselben Quelle, und die Repo-Datei trägt
    /// ihn.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselKennlinieSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Projekt 1007 „Laurentiuskirche" mit einem Kessel (Projektkopie 1007239).</summary>
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int PROJEKT = 1007;

        /// <summary>Erfundene Kennlinienwerte für die Proben.</summary>
        private const double ETA30 = 1.05;
        private const double PMIN = 4.5;
        private const double ANFAHR = 0.1;
        private const long LAUFZEIT = 10;

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>
        /// Die Nummer folgt lückenlos auf 155 (Verfahrensvolumina der Füllstandslinie); der Zielstand
        /// reicht bis zu ihr.
        /// </summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_auf_155()
        {
            Assert.Equal(TwwFuellstandSchema.SCHRITT + 1, KesselKennlinieSchema.SCHRITT);
            Assert.Equal(156, KesselKennlinieSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KesselKennlinieSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KesselKennlinieSchema.SCHRITT + ".");
        }

        /// <summary>Das Register der Paketanhebung führt den Schritt als reines DDL.</summary>
        [Fact]
        public void Das_Register_der_Paketanhebung_fuehrt_den_Schritt_als_DDL()
        {
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KesselKennlinieSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.False(string.IsNullOrWhiteSpace(s.Text));
        }

        /// <summary>
        /// Die fünf Spalten in der Reihenfolge des Konzepts: vier nullbar ohne Vorgabe, der Schalter
        /// <c>NOT NULL DEFAULT 0</c> mit Prüfklausel — an einer STRICT-Tabelle nehmen sie Zahl und
        /// NULL, der Schalter nur 0 und 1.
        /// </summary>
        [Fact]
        public void Die_Spalten_sind_nullbar_und_der_Schalter_ist_null_eins()
        {
            Assert.Equal(new[] { "Wirkungsgrad_Teillast30", "Kennlinie_Brennwert", "Mindestleistung",
                                 "Anfahrverlust_kWh", "Mindestlaufzeit_min" },
                         KesselKennlinieSchema.SPALTEN.Select(s => s.Key).ToArray());
            Assert.Equal(new[] { "Tab_Heizkessel_STAMM", "Tab_Heizkessel" }, KesselKennlinieSchema.TABELLEN);

            using SqliteConnection c = Speicher();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> s in KesselKennlinieSchema.SPALTEN)
                Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + s.Key + "\" " + s.Value);
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (2)");

            foreach (long id in new long[] { 1, 2 })
            {
                foreach (string leer in new[] { "Wirkungsgrad_Teillast30", "Mindestleistung", "Anfahrverlust_kWh",
                                                "Mindestlaufzeit_min" })
                    Assert.True(Leer(c, "SELECT " + leer + " FROM T WHERE ID = " + id), leer + " in Zeile " + id);
                Assert.Equal(0L, Skalar(c, "SELECT Kennlinie_Brennwert FROM T WHERE ID = " + id));
            }

            Assert.False(Wirft(c, "UPDATE T SET Wirkungsgrad_Teillast30 = 1.05, Mindestleistung = 4.5, " +
                                  "Anfahrverlust_kWh = 0.1, Mindestlaufzeit_min = 10, Kennlinie_Brennwert = 1"));
            Assert.False(Wirft(c, "UPDATE T SET Wirkungsgrad_Teillast30 = NULL, Mindestleistung = NULL, " +
                                  "Anfahrverlust_kWh = NULL, Mindestlaufzeit_min = NULL, Kennlinie_Brennwert = 0"));
            Assert.True(Wirft(c, "UPDATE T SET Kennlinie_Brennwert = 2"), "Der Schalter nimmt nur 0 und 1.");
            Assert.True(Wirft(c, "UPDATE T SET Kennlinie_Brennwert = NULL"), "Der Schalter ist NOT NULL.");
            Assert.True(Wirft(c, "UPDATE T SET Wirkungsgrad_Teillast30 = 'hoch'"), "Text an REAL einer STRICT-Tabelle.");
            Assert.True(Wirft(c, "UPDATE T SET Mindestlaufzeit_min = 7.5"), "Bruch an INTEGER einer STRICT-Tabelle.");
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und der Schritt aus dem Stand davor
        // =============================================================================

        /// <summary>
        /// Die Spalten stehen an beiden Tabellen, beide bleiben STRICT, und allein die Projektkopie des
        /// Referenzprojekts 1050 (Etappe E2, Konzept 4.3) trägt Kennlinienwerte — jede weitere verschöbe
        /// die Referenzbasis (Einfrierregel „gesäte Kesseldaten“).
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Zielstand_und_nur_1050_traegt_eine_Kennlinie()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KesselKennlinieSchema.SCHRITT);
            Assert.True(KesselKennlinieSchema.Vollstaendig());
            Assert.Empty(KesselKennlinieSchema.Anweisungen);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Heizkessel") > 0);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Heizkessel WHERE ID_Projekt <> 1050" +
                                  " AND (Wirkungsgrad_Teillast30 IS NOT NULL " +
                                  "OR Kennlinie_Brennwert <> 0 OR Mindestleistung IS NOT NULL " +
                                  "OR Anfahrverlust_kWh IS NOT NULL OR Mindestlaufzeit_min IS NOT NULL)"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Heizkessel WHERE ID_Projekt = 1050" +
                                  " AND Wirkungsgrad_Teillast30 IS NOT NULL AND Kennlinie_Brennwert = 1"));
            // Im Katalog pflegt die Nachpflege (F2) nur eta30, eta100, Brennwert und Mindestleistung -
            // Schalter, Anfahrverlust und Mindestlaufzeit bleiben ueberall leer.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Heizkessel_STAMM WHERE Kennlinie_Brennwert <> 0 " +
                                  "OR Anfahrverlust_kWh IS NOT NULL OR Mindestlaufzeit_min IS NOT NULL"));

            foreach (string tabelle in KesselKennlinieSchema.TABELLEN)
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                    new DbParam("?", tabelle)), CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd());
            }
        }

        /// <summary>
        /// Der Schritt aus dem Stand VOR ihm: Die Spalten werden entfernt; der Schritt legt alle
        /// zehn an, die Bestandswerte der Zeilen bleiben, und ein zweiter Lauf legt nichts mehr an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand();
            foreach (string tabelle in KesselKennlinieSchema.TABELLEN)
                foreach (KeyValuePair<string, string> s in KesselKennlinieSchema.SPALTEN)
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + s.Key + "\"");
            Assert.False(KesselKennlinieSchema.Vollstaendig());
            Assert.False(KesselKennlinieSchema.SpaltenVorhanden(KesselKennlinieSchema.TAB_STAMM));
            Assert.Equal(10, KesselKennlinieSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(10, KesselKennlinieSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Heizkessel.Kennlinie_Brennwert anlegen"));
            Assert.Contains(bericht, z => z.Contains("Tab_Heizkessel_STAMM.Wirkungsgrad_Teillast30 anlegen"));
            Assert.True(KesselKennlinieSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Heizkessel_STAMM WHERE Wirkungsgrad_Teillast30 IS NOT NULL " +
                                  "OR Kennlinie_Brennwert <> 0 OR Mindestleistung IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, KesselKennlinieSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        // =============================================================================
        //  Teil 3 - die Wege über die ganze Zeile
        // =============================================================================

        /// <summary>Das Projektduplikat trägt die Kennlinie der Projektkopie.</summary>
        [Fact]
        public void Projektduplikat_traegt_die_Kennlinie()
        {
            if (!_db.Vorhanden) return;

            SetzeKennlinie(PROJEKT);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Kennlinie");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeKennlinie(neu);
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt die Kennlinie über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_die_Kennlinie()
        {
            if (!_db.Vorhanden) return;

            SetzeKennlinie(PROJEKT);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-kennlinie-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Kennlinie", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeKennlinie(neu);
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
        /// Nachzieh-Liste der Tests führen den Schritt aus derselben Quelle, hinter Schritt 155; die
        /// REPO-Datei trägt die Spalten, und allein das Referenzprojekt 1050 trägt Werte (gelesen nur
        /// lesend und ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wKennlinie = werkzeug.IndexOf("KesselKennlinieSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wKennlinie > werkzeug.IndexOf("TwwFuellstandSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Das Werkzeug führt den Schritt nicht hinter Schritt 155.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KESSEL_KENNLINIE = KesselKennlinieSchema.SCHRITT", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_TWW_FUELLSTAND_VERFAHREN", StringComparison.Ordinal);
            int ortKennlinie = migration.IndexOf("new Schritt(SCHRITT_KESSEL_KENNLINIE", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortKennlinie > ortVorher, "Der Schritt steht nicht hinter 155.");
            Assert.Contains("KesselKennlinieSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vKennlinie = vorrichtung.IndexOf("KesselKennlinieSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vKennlinie > vorrichtung.IndexOf("TwwFuellstandSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Die Testkopie führt den Schritt nicht hinter Schritt 155.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= KesselKennlinieSchema.SCHRITT);
            foreach (string tabelle in KesselKennlinieSchema.TABELLEN)
            {
                Assert.Equal(4L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + tabelle + "') WHERE " +
                                                  "name IN ('Wirkungsgrad_Teillast30', 'Mindestleistung', " +
                                                  "'Anfahrverlust_kWh', 'Mindestlaufzeit_min') AND \"notnull\" = 0 " +
                                                  "AND dflt_value IS NULL"));
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + tabelle + "') WHERE " +
                                                  "name = 'Kennlinie_Brennwert' AND type = 'INTEGER' AND " +
                                                  "\"notnull\" = 1 AND dflt_value = '0'"));
            }
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Heizkessel WHERE ID_Projekt <> 1050" +
                                              " AND (Wirkungsgrad_Teillast30 IS NOT NULL " +
                                              "OR Kennlinie_Brennwert <> 0 OR Mindestleistung IS NOT NULL " +
                                              "OR Anfahrverlust_kWh IS NOT NULL OR Mindestlaufzeit_min IS NOT NULL)"));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static void SetzeKennlinie(int projekt)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Heizkessel SET Wirkungsgrad_Teillast30 = ?, Mindestleistung = ?, Anfahrverlust_kWh = ?, " +
                "Mindestlaufzeit_min = ? WHERE ID_Projekt = ?",
                new DbParam("?", ETA30), new DbParam("?", PMIN), new DbParam("?", ANFAHR),
                new DbParam("?", LAUFZEIT), new DbParam("?", projekt)));
        }

        private static void PruefeKennlinie(int projekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Wirkungsgrad_Teillast30, Mindestleistung, Anfahrverlust_kWh, Mindestlaufzeit_min, " +
                "Kennlinie_Brennwert FROM Tab_Heizkessel WHERE ID_Projekt = ?", new DbParam("?", projekt));
            Assert.True(dt != null && dt.Rows.Count >= 1, "Keine Kesselkopie für " + projekt + ".");
            foreach (DataRow r in dt.Rows)
            {
                Assert.Equal(ETA30, Convert.ToDouble(r[0], CultureInfo.InvariantCulture));
                Assert.Equal(PMIN, Convert.ToDouble(r[1], CultureInfo.InvariantCulture));
                Assert.Equal(ANFAHR, Convert.ToDouble(r[2], CultureInfo.InvariantCulture));
                Assert.Equal(LAUFZEIT, Convert.ToInt64(r[3], CultureInfo.InvariantCulture));
                Assert.Equal(0L, Convert.ToInt64(r[4], CultureInfo.InvariantCulture));
            }
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Zeilen beider Kesseltabellen ohne die neuen Spalten — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string tabelle in KesselKennlinieSchema.TABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Ptherm, Brennstoff, Wirkungsgrad_Gas, \"Wirkungsgrad_Öl\", " +
                    "Betriebsbereitschaftverlust, Brennwert, Vorlauf, Ruecklauf FROM \"" + tabelle + "\" ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                    liste.Add(tabelle + "|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
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

        private static long Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
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
