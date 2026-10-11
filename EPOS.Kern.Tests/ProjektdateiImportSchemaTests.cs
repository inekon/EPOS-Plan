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
    /// <b>Schritt 200 — Import aus der Projektdatei</b> (<see cref="ProjektdateiImportSchema"/>): Nummer, Ziel und Register;
    /// Grundschema und Schritt führen dieselben Wertlisten; der Zieltext tauscht allein die Prüfklausel; die Tabellenliste gegen
    /// <c>sqlite_master</c>; <c>SQPROJ</c> wird in Format und jeder Herkunftsspalte angenommen, ein unbekannter Wert abgewiesen;
    /// der Neubau aus dem Stand davor hält Zeilen, Spaltenfolge, STRICT, Fremdschlüssel, Indizes, Trigger und Zähler und läuft
    /// ein zweites Mal ohne Wirkung; die Werkzeug-Wache und die Repo-Datei.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ProjektdateiImportSchemaTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private const int GEBAEUDE = 10614;              // Projekt 1007, ohne Zonen und ohne Quelle in der Testdatenbank

        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definition (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_200_das_Ziel_steht_darauf_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(NordrichtungSchema.SCHRITT + 1, ProjektdateiImportSchema.SCHRITT);
            Assert.Equal(200, ProjektdateiImportSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ProjektdateiImportSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == ProjektdateiImportSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                         {
                             "Tab_Importquelle", "Tab_Baustoff_STAMM", "Tab_Baustoff", "Tab_Bauteilaufbau_STAMM",
                             "Tab_Bauteilaufbau", "Tab_Zone", "Tab_Bauteil"
                         },
                         ProjektdateiImportSchema.TABELLEN.Select(u => u.Tabelle).ToArray());
            Assert.Equal(ProjektdateiImportSchema.TABELLEN.Select(u => u.Tabelle), ProjektdateiImportSchema.Voraussetzungen());
        }

        [Fact]
        public void Werte_und_Pruefklauseln_tragen_SQPROJ_und_die_alten_Werte()
        {
            Assert.Equal("SQPROJ", DbWerte.IMPORT_FORMAT_SQPROJ);
            Assert.Equal("SQPROJ", DbWerte.HERKUNFT_SQPROJ);
            Assert.Equal(DbWerte.HERKUNFT_SQPROJ, ImportherkunftWerte.SQPROJ);
            Assert.Contains(DbWerte.IMPORT_FORMAT_SQPROJ, DbWerte.IMPORT_FORMATE);
            Assert.Contains(DbWerte.HERKUNFT_SQPROJ, DbWerte.HERKUENFTE);

            Assert.Equal("CHECK (\"Herkunft\" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ'))",
                         ProjektdateiImportSchema.CHECK_HERKUNFT_NEU);
            Assert.Equal("CHECK (\"Format\" IN ('IFC','GBXML','SQPROJ'))", ProjektdateiImportSchema.CHECK_FORMAT_NEU);
            // Die neue Liste ist die alte plus SQPROJ - kein Wert fällt weg.
            Assert.Equal(ProjektdateiImportSchema.CHECK_HERKUNFT_ALT.Replace("'VORGABE'", "'VORGABE','SQPROJ'"),
                         ProjektdateiImportSchema.CHECK_HERKUNFT_NEU);
            Assert.Equal(ProjektdateiImportSchema.CHECK_FORMAT_ALT.Replace("'GBXML'", "'GBXML','SQPROJ'"),
                         ProjektdateiImportSchema.CHECK_FORMAT_NEU);

            // Je Format die gleichnamige Herkunft.
            Assert.Equal(ImportherkunftWerte.IFC, ImportherkunftWerte.ZuFormat(DbWerte.IMPORT_FORMAT_IFC));
            Assert.Equal(ImportherkunftWerte.GBXML, ImportherkunftWerte.ZuFormat(DbWerte.IMPORT_FORMAT_GBXML));
            Assert.Equal(ImportherkunftWerte.SQPROJ, ImportherkunftWerte.ZuFormat(DbWerte.IMPORT_FORMAT_SQPROJ));
            Assert.Null(ImportherkunftWerte.ZuFormat("sqproj"));
            Assert.Null(ImportherkunftWerte.ZuFormat(null));
            foreach (string f in DbWerte.IMPORT_FORMATE)
                Assert.Contains(ImportherkunftWerte.ZuFormat(f), DbWerte.HERKUENFTE);
        }

        /// <summary>
        /// <b>Grundschema = Schritt:</b> Die Grundschemata der sieben Tabellen tragen die neue Klausel genau einmal; mit der
        /// alten Klausel zurückgetauscht ergibt der Zieltext des Schritts Zeichen für Zeichen wieder das Grundschema.
        /// </summary>
        [Fact]
        public void Grundschema_und_Zieltext_des_Schritts_sind_zeichengleich()
        {
            var grund = new Dictionary<string, string>
            {
                [SchemaKatalog.TAB_IMPORTQUELLE] = ImportzuordnungSchema.SQL_CREATE_QUELLE,
            };
            foreach (KeyValuePair<string, string> a in BaustoffSchema.Anweisungen.Concat(BauteilaufbauSchema.Anweisungen)
                                                                                  .Concat(ZonenSchema.Anweisungen))
                if (!grund.ContainsKey(a.Key)) grund[a.Key] = a.Value;

            foreach (ProjektdateiImportSchema.Umbau u in ProjektdateiImportSchema.TABELLEN)
            {
                Assert.True(grund.ContainsKey(u.Tabelle), "Kein Grundschema zu " + u.Tabelle + ".");
                string neu = grund[u.Tabelle];
                Assert.Equal(1, Vorkommen(neu, u.Neu));
                string alt = neu.Replace(u.Neu, u.Alt);
                Assert.Equal(neu, ProjektdateiImportSchema.Zieltext(u, alt));
                Assert.Null(ProjektdateiImportSchema.Zieltext(u, neu));
            }
        }

        [Fact]
        public void Der_Zieltext_tauscht_allein_die_Pruefklausel_und_bricht_bei_Fremdem_ab()
        {
            ProjektdateiImportSchema.Umbau u = ProjektdateiImportSchema.TABELLEN.Single(x => x.Tabelle == "Tab_Zone");
            string alt = "CREATE TABLE \"Tab_Zone\" (\n    \"ID\" INTEGER PRIMARY KEY,\n    \"Herkunft\" TEXT " + u.Alt + "\n) STRICT";
            Assert.Equal(alt.Replace(u.Alt, u.Neu), ProjektdateiImportSchema.Zieltext(u, alt));

            string fremd = alt.Replace("'VORGABE'", "'VORGABE','XY'");
            Assert.Throws<InvalidOperationException>(() => ProjektdateiImportSchema.Zieltext(u, fremd));
            Assert.Throws<InvalidOperationException>(() => ProjektdateiImportSchema.Zieltext(u, alt.Replace(") STRICT", ")")));
            Assert.Throws<InvalidOperationException>(() => ProjektdateiImportSchema.Zieltext(u, alt.Replace(") STRICT", ", \"H2\" TEXT " + u.Alt + ") STRICT")));
            Assert.Throws<InvalidOperationException>(() => ProjektdateiImportSchema.Zieltext(u, null));
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank: Liste, Annahme, Stand davor, Neubau, Wiederholbarkeit
        // =============================================================================

        /// <summary>Jede Tabelle, deren Prüfklausel eine der beiden Wertlisten trägt, steht in der Liste des Schritts — und umgekehrt.</summary>
        [Fact]
        public void Die_Tabellenliste_ist_die_der_Datenbank()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ProjektdateiImportSchema.Vollstaendig());
            Assert.Equal(0, ProjektdateiImportSchema.Offen());
            DataTable t = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND sql LIKE ? ORDER BY name", new DbParam("@m", "%'GBXML'%"));
            // Die Katalogzwillinge der Zonen (Schritt ZK) entstehen spaeter schon mit SQPROJ - kein Umbau dieses Schritts.
            var zwillinge = new[] { ZonenKatalogSchema.TAB_ZONE, ZonenKatalogSchema.TAB_BAUTEIL };
            foreach (string z in zwillinge) Assert.Equal(1, Vorkommen(Sql(z), "'SQPROJ'"));
            var namen = t.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["name"], CultureInfo.InvariantCulture))
                         .Where(n => !zwillinge.Contains(n)).ToList();
            Assert.Equal(ProjektdateiImportSchema.TABELLEN.Select(u => u.Tabelle).OrderBy(x => x, StringComparer.Ordinal),
                         namen.OrderBy(x => x, StringComparer.Ordinal));
            foreach (ProjektdateiImportSchema.Umbau u in ProjektdateiImportSchema.TABELLEN)
            {
                string sql = Sql(u.Tabelle);
                Assert.Equal(1, Vorkommen(sql, u.Neu));
                Assert.Equal(0, Vorkommen(sql, u.Alt));
                Assert.EndsWith(") STRICT", sql.TrimEnd(), StringComparison.Ordinal);
            }
        }

        [Fact]
        public void SQPROJ_wird_in_jeder_Spalte_angenommen_ein_fremder_Wert_abgewiesen()
        {
            if (!_db.Vorhanden) return;
            using SqliteConnection c = Verbindung();
            foreach (ProjektdateiImportSchema.Umbau u in ProjektdateiImportSchema.TABELLEN)
            {
                Assert.False(Wirft(c, Probe(u, "SQPROJ")), u.Tabelle + " nimmt SQPROJ nicht an.");
                Assert.True(Wirft(c, Probe(u, "XYZ")), u.Tabelle + " nimmt XYZ an.");
                Assert.True(Wirft(c, Probe(u, "sqproj")), u.Tabelle + " nimmt sqproj an.");
            }
            foreach (string h in DbWerte.HERKUENFTE)
                Assert.False(Wirft(c, Probe(ProjektdateiImportSchema.TABELLEN.Single(x => x.Tabelle == "Tab_Bauteil"), h)), h);
            foreach (string f in DbWerte.IMPORT_FORMATE)
                Assert.False(Wirft(c, Probe(ProjektdateiImportSchema.TABELLEN[0], f)), f);
        }

        /// <summary>
        /// <b>Der Neubau</b> auf einer Arbeitskopie, die vorher mit dem Rezept des Schritts auf die alten Klauseln zurückgebaut
        /// ist: Zeilen, Spaltenfolge, STRICT, Fremdschlüssel samt Kaskaden, Indizes, Trigger und AUTOINCREMENT-Zähler wie davor,
        /// der CREATE-Text wieder zeichengleich, <c>foreign_key_check</c> leer; ein zweiter Lauf ohne Wirkung.
        /// </summary>
        [Fact]
        public void Der_Neubau_haelt_Daten_Indizes_Trigger_Fremdschluessel_und_laeuft_ein_zweites_Mal_ohne_Wirkung()
        {
            if (!_db.Vorhanden) return;
            // Ein Trigger und eine Sicht auf einer der Tabellen, damit beide den Neubau nachweislich überstehen.
            DataRepository.ExecuteNonQuery("CREATE TRIGGER \"trg_Probe_Zone\" AFTER UPDATE ON \"Tab_Zone\" BEGIN SELECT 1; END");
            DataRepository.ExecuteNonQuery("CREATE VIEW \"Sicht_Probe_Bauteil\" AS SELECT ID, Herkunft FROM \"Tab_Bauteil\"");
            Dictionary<string, string> vorher = ProjektdateiImportSchema.TABELLEN.ToDictionary(u => u.Tabelle, u => Abdruck(u.Tabelle));
            Dictionary<string, string> textVorher = ProjektdateiImportSchema.TABELLEN.ToDictionary(u => u.Tabelle, u => Sql(u.Tabelle));
            long sichtVorher = Zahl("SELECT COUNT(*) FROM \"Sicht_Probe_Bauteil\"");

            Zurueckbauen();
            Assert.False(ProjektdateiImportSchema.Vollstaendig());
            Assert.Equal(ProjektdateiImportSchema.TABELLEN.Count, ProjektdateiImportSchema.Offen());
            using (SqliteConnection c = Verbindung())
                Assert.True(Wirft(c, Probe(ProjektdateiImportSchema.TABELLEN[0], "SQPROJ")));

            var bericht = new List<string>();
            Assert.Equal(ProjektdateiImportSchema.TABELLEN.Count, ProjektdateiImportSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("neu gebaut", StringComparison.Ordinal));
            Assert.True(ProjektdateiImportSchema.Vollstaendig());
            foreach (ProjektdateiImportSchema.Umbau u in ProjektdateiImportSchema.TABELLEN)
            {
                Assert.Equal(textVorher[u.Tabelle], Sql(u.Tabelle));
                Assert.Equal(vorher[u.Tabelle], Abdruck(u.Tabelle));
            }
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            Assert.Equal(sichtVorher, Zahl("SELECT COUNT(*) FROM \"Sicht_Probe_Bauteil\""));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'trg_Probe_Zone'"));
            Assert.Equal("off", Text(DataRepository.ExecuteScalar("PRAGMA legacy_alter_table")) == "0" ? "off" : "on");

            // Wiederholt: nichts mehr zu tun, nichts geändert.
            var zweiter = new List<string>();
            Assert.Equal(0, ProjektdateiImportSchema.Ausfuehren(zweiter));
            Assert.Equal(ProjektdateiImportSchema.TABELLEN.Count, zweiter.Count(z => z.StartsWith("steht bereits", StringComparison.Ordinal)));
            foreach (ProjektdateiImportSchema.Umbau u in ProjektdateiImportSchema.TABELLEN)
                Assert.Equal(vorher[u.Tabelle], Abdruck(u.Tabelle));
        }

        [Fact]
        public void Ohne_eine_Tabelle_wirft_der_Schritt_benannt()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("DROP TABLE \"Tab_Importzuordnung\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"Tab_Raumgrundriss\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"Tab_Importquelle\"");
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ProjektdateiImportSchema.Ausfuehren(null));
            Assert.Contains("Tab_Importquelle", ex.Message, StringComparison.Ordinal);
            Assert.False(ProjektdateiImportSchema.Vollstaendig());
        }

        // =============================================================================
        //  Teil 3 - Werkzeug, Migration, Testkopie und Repo-Datei
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt aus
        /// derselben Quelle NACH 199; die Repo-Datei trägt die neuen Klauseln (lesend geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int w = werkzeug.IndexOf("ProjektdateiImportSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(w > 0 && w > werkzeug.IndexOf("NordrichtungSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Der Schritt steht im Werkzeug nicht hinter 199.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_PROJEKTDATEI_IMPORT = ProjektdateiImportSchema.SCHRITT", migration, StringComparison.Ordinal);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_NORDRICHTUNG", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_PROJEKTDATEI_IMPORT", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ort > ortVorher, "Der Schritt steht nicht hinter 199.");
            Assert.Contains("ProjektdateiImportSchema.Ausfuehren(bericht)", migration, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int v = vorrichtung.IndexOf("ProjektdateiImportSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(v > 0 && v > vorrichtung.IndexOf("NordrichtungSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Der Schritt steht in der Testkopie nicht hinter 199.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            Assert.True(Skalar(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= ProjektdateiImportSchema.SCHRITT);
            foreach (ProjektdateiImportSchema.Umbau u in ProjektdateiImportSchema.TABELLEN)
            {
                using SqliteCommand k = verbindung.CreateCommand();
                k.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $n";
                k.Parameters.AddWithValue("$n", u.Tabelle);
                string sql = Convert.ToString(k.ExecuteScalar(), CultureInfo.InvariantCulture);
                Assert.True(sql.Contains(u.Neu, StringComparison.Ordinal), "Die Repo-Datei: " + u.Tabelle + " ohne SQPROJ.");
            }
            // Kein Datensatz trägt SQPROJ - der Schritt ist ergebnisneutral.
            Assert.Equal(0L, Skalar(verbindung, "SELECT COUNT(*) FROM Tab_Importquelle WHERE Format = 'SQPROJ'"));
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>Baut alle sieben Tabellen der Arbeitskopie auf die alte Klausel zurück — mit dem Rezept des Schritts.</summary>
        private static void Zurueckbauen()
        {
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                foreach (ProjektdateiImportSchema.Umbau u in ProjektdateiImportSchema.TABELLEN)
                {
                    string text = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                            new DbParam("p1", u.Tabelle)), CultureInfo.InvariantCulture);
                    TwwBezugsartSchema.Neubau(v, u.Tabelle, text.Replace(u.Neu, u.Alt), u.Tabelle + "_Probe_alt");
                }
                v.Commit();
            }
            foreach (ProjektdateiImportSchema.Umbau u in ProjektdateiImportSchema.TABELLEN)
                Assert.Contains(u.Alt, Sql(u.Tabelle), StringComparison.Ordinal);
        }

        /// <summary>
        /// Der Abdruck einer Tabelle ohne ihren CREATE-Text: Zeilen und ihr Inhalt (Prüfsumme über alle Spalten), Spaltenfolge
        /// samt Typ und NOT NULL, Fremdschlüssel samt Kaskaden, Indizes und Trigger mit Text, AUTOINCREMENT-Zähler.
        /// </summary>
        private static string Abdruck(string tabelle)
        {
            var teile = new List<string>();
            DataTable spalten = DataRepository.GetDataTable("SELECT name, type, \"notnull\", dflt_value, pk FROM pragma_table_info(?)",
                                                            new DbParam("@t", tabelle));
            var namen = new List<string>();
            foreach (DataRow r in spalten.Rows)
            {
                namen.Add("\"" + Text(r["name"]) + "\"");
                teile.Add("S:" + Text(r["name"]) + "|" + Text(r["type"]) + "|" + Text(r["notnull"]) + "|" + Text(r["dflt_value"]) + "|" + Text(r["pk"]));
            }
            DataTable fk = DataRepository.GetDataTable(
                "SELECT \"table\", \"from\", \"to\", on_update, on_delete FROM pragma_foreign_key_list(?) ORDER BY id, seq",
                new DbParam("@t", tabelle));
            foreach (DataRow r in fk.Rows)
                teile.Add("F:" + Text(r["table"]) + "|" + Text(r["from"]) + "|" + Text(r["to"]) + "|" + Text(r["on_update"]) + "|" + Text(r["on_delete"]));
            DataTable obj = DataRepository.GetDataTable(
                "SELECT type, name, sql FROM sqlite_master WHERE tbl_name = ? AND type IN ('index', 'trigger') ORDER BY type, name",
                new DbParam("@t", tabelle));
            foreach (DataRow r in obj.Rows)
                teile.Add("O:" + Text(r["type"]) + "|" + Text(r["name"]) + "|" + Text(r["sql"]));
            teile.Add("Z:" + Text(DataRepository.ExecuteScalar("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle))));
            teile.Add("N:" + Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\""));
            DataTable zeilen = DataRepository.GetDataTable("SELECT " + string.Join(" || '|' || ", namen.Select(n => "quote(" + n + ")")) +
                                                           " AS z FROM \"" + tabelle + "\" ORDER BY rowid");
            foreach (DataRow r in zeilen.Rows) teile.Add("R:" + Text(r["z"]));
            return string.Join("\n", teile);
        }

        /// <summary>Eine Anweisung, die den Wert in die Spalte der Tabelle schreibt (Einfügen in leere, Ändern in volle Tabellen).</summary>
        private static string Probe(ProjektdateiImportSchema.Umbau u, string wert)
        {
            string w = "'" + wert + "'";
            switch (u.Tabelle)
            {
                case "Tab_Importquelle":
                    return "INSERT INTO \"Tab_Importquelle\" (ID_Gebaeude, Format, Dateiname, Hash, Groesse, Zeitpunkt) VALUES (" +
                           GEBAEUDE.ToString(CultureInfo.InvariantCulture) + ", " + w + ", 'haus.sqproj', '" + new string('a', 64) +
                           "', 1, '2026-10-07T00:00:00')";
                case "Tab_Baustoff":
                case "Tab_Bauteilaufbau":
                    return "INSERT INTO \"" + u.Tabelle + "\" (ID_Projekt, Bezeichner, Herkunft) VALUES (" +
                           PROJEKT.ToString(CultureInfo.InvariantCulture) + ", 'Probe', " + w + ")";
                default:
                    return "UPDATE \"" + u.Tabelle + "\" SET \"" + u.Spalte + "\" = " + w +
                           " WHERE ID = (SELECT MIN(ID) FROM \"" + u.Tabelle + "\")";
            }
        }

        /// <summary>Eine eigene Verbindung auf die Arbeitskopie; jede Anweisung läuft in einer Transaktion, die zurückrollt.</summary>
        private static SqliteConnection Verbindung()
        {
            var c = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = DataRepository.PfadUeberschreibung,
                ForeignKeys = false,
                Pooling = false
            }.ToString());
            c.Open();
            return c;
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            using SqliteTransaction t = c.BeginTransaction();
            try
            {
                using SqliteCommand k = c.CreateCommand();
                k.Transaction = t;
                k.CommandText = sql;
                int n = k.ExecuteNonQuery();
                Assert.Equal(1, n);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
            finally
            {
                t.Rollback();
            }
        }

        private static string Sql(string tabelle)
            => Text(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", tabelle)));

        private static int Vorkommen(string text, string teil)
        {
            int n = 0;
            for (int i = text.IndexOf(teil, StringComparison.Ordinal); i >= 0; i = text.IndexOf(teil, i + teil.Length, StringComparison.Ordinal)) n++;
            return n;
        }

        private static string Repowurzel()
        {
            string d = AppContext.BaseDirectory;
            while (d != null && !File.Exists(Path.Combine(d, "WP-Plan.sln"))) d = Path.GetDirectoryName(d);
            return d;
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static long Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToInt64(k.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string Text(object o) => o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
    }
}
