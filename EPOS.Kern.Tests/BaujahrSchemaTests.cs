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
    /// Der Schemaschritt des <b>Baujahrs</b> (Stufe G4a; Umsetzungskonzept Gebäudesimulation 3.4 und
    /// 3.7; Nummer bei <see cref="BaujahrSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer (lückenlos hinter S-F), die Definition der Spalte samt
    /// Bereichsprüfung und der fünfte Sichtneubau; der Stand der Testdatenbank (die Spalte steht an
    /// beiden Gebäudetabellen, ist überall leer, die Sicht ist die geltende); der Schritt aus dem
    /// Stand davor, wiederholbar; <b>die vier fest verdrahteten Kopierwege</b> von
    /// <c>GebaeudeStammCtrl</c> samt Namensleser NULL-erhaltend; die Wege über die ganze Zeile
    /// (Katalog duplizieren, Projektduplikat, Projekttransfer); Repo-Datei, Werkzeug und Migration
    /// führen den Schritt zuletzt.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BaujahrSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Projekt (1007) mit genau einem Gebäude.</summary>
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        /// <summary>Ein rundes, erfundenes Baujahr für die Proben.</summary>
        private const int JAHR = 1965;

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf S-F; der Zielstand reicht bis zu ihr.</summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_auf_S_F()
        {
            Assert.Equal(ImportzuordnungSchema.SCHRITT + 1, BaujahrSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= BaujahrSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + BaujahrSchema.SCHRITT + ".");
        }

        /// <summary>
        /// Die Spalte: <c>INTEGER</c>, nullbar, <c>CHECK</c> 1500 … 2100 — wortgleich mit dem Auftrag;
        /// dieselben Grenzen wie die Leseregel des Imports.
        /// </summary>
        [Fact]
        public void Die_Spalte_ist_eine_nullbare_Ganzzahl_von_1500_bis_2100()
        {
            Assert.Equal("Baujahr", GebaeudeSchema.SPALTE_BAUJAHR);
            Assert.Equal("INTEGER CHECK (Baujahr IS NULL OR Baujahr BETWEEN 1500 AND 2100)", GebaeudeSchema.SQLITE_BAUJAHR);
            Assert.Equal(GebaeudeSchema.BAUJAHR_MIN, Baujahrregel.JAHR_MIN);
            Assert.Equal(GebaeudeSchema.BAUJAHR_MAX, Baujahrregel.JAHR_MAX);
            Assert.DoesNotContain(GebaeudeSchema.SPALTE_BAUJAHR, GebaeudeSchema.SICHT_KUEHLUEBERGABE);

            using SqliteConnection c = Speicher();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, GebaeudeSchema.BaujahrAnlegen("T"));
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            Assert.True(Leer(c, "SELECT Baujahr FROM T WHERE ID = 1"), "Ohne Wert ist das Baujahr NULL.");
            foreach (string gut in new[] { "1500", "2100", "1965", "NULL" })
                Assert.False(Wirft(c, "UPDATE T SET Baujahr = " + gut), gut);
            foreach (string schlecht in new[] { "1499", "2101", "0", "'neunzehnhundert'", "1965.5" })
                Assert.True(Wirft(c, "UPDATE T SET Baujahr = " + schlecht), schlecht);
        }

        /// <summary>
        /// Der fünfte Sichtneubau hängt das Baujahr HINTER die 98 Spalten von KAK-S1 (Stelle 98); die
        /// Bauvorschrift ist dieselbe, und er ist die GELTENDE Sicht.
        /// </summary>
        [Fact]
        public void Der_fuenfte_Sichtneubau_haengt_das_Baujahr_hinter_KAK_S1()
        {
            Assert.Equal(99, GebaeudeSchema.SICHT_BAUJAHR.Length);
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLUEBERGABE, GebaeudeSchema.SICHT_BAUJAHR.Take(98));
            Assert.Equal("Baujahr", GebaeudeSchema.SICHT_BAUJAHR[98]);
            Assert.Equal(GebaeudeSchema.SichtSql(GebaeudeSchema.NEUE_SPALTEN.Select(s => s.Key)
                                                 .Concat(GebaeudeSchema.KUEHL_SPALTEN.Select(s => s.Key))
                                                 .Concat(GebaeudeSchema.UEBERGABE_SPALTEN.Select(s => s.Key))
                                                 .Concat(GebaeudeSchema.KUEHLUEBERGABE_SPALTEN.Select(s => s.Key))
                                                 .Concat(new[] { "Baujahr" })),
                         GebaeudeSchema.SQL_VIEW_BAUJAHR);
            Assert.Contains("Tab_Gebaeude.Baujahr", GebaeudeSchema.SQL_VIEW_BAUJAHR, StringComparison.Ordinal);
            Assert.DoesNotContain("Tab_Gebaeude.Baujahr", GebaeudeSchema.SQL_VIEW_KUEHLUEBERGABE, StringComparison.Ordinal);
            // Die GELTENDE Sicht (der Nachtzeit, E43) beginnt mit der Sicht des Baujahrs an denselben Stellen.
            Assert.Equal(GebaeudeSchema.SICHT_BAUJAHR, GebaeudeSchema.SICHT_AKTUELL.Take(99));
            Assert.Equal(GebaeudeSchema.SQL_VIEW_NACHTZEIT, GebaeudeSchema.SQL_VIEW_AKTUELL);
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und der Schritt aus dem Stand davor
        // =============================================================================

        /// <summary>Die Spalte steht an beiden Gebäudetabellen, ist überall leer, die Sicht ist die geltende; STRICT bleibt.</summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Zielstand_und_das_Baujahr_ist_leer()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= BaujahrSchema.SCHRITT);
            Assert.True(BaujahrSchema.Vollstaendig());
            Assert.True(KuehluebergabeSchema.GebaeudeVollstaendig());

            string sicht = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?",
                new DbParam("?", GebaeudeSchema.VIEW)), CultureInfo.InvariantCulture);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, sicht);
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Equal(GebaeudeSchema.SICHT_BAUJAHR, GebaeudeSchema.SichtSpalten().Take(99));

            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                Assert.True(Zahl("SELECT COUNT(*) FROM [" + t + "]") > 0, t);
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + t + "] WHERE Baujahr IS NOT NULL"));
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd(), StringComparison.Ordinal);
                Assert.Contains(GebaeudeSchema.SQLITE_BAUJAHR, ddl, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Der Schritt aus dem Stand VOR ihm: Sicht und Spalte werden entfernt, die Sicht von KAK-S1
        /// steht; der Schritt legt die zwei Spalten an und baut die Sicht neu — die Bestandswerte
        /// bleiben, KAK-S1 steht weiter, und ein zweiter Lauf legt nichts mehr an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand();

            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach (string t in GebaeudeSchema.TABELLEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + t + "\" DROP COLUMN \"Baujahr\"");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_KUEHLUEBERGABE);    // der Stand nach KAK-S1
            Assert.True(KuehluebergabeSchema.GebaeudeVollstaendig());
            Assert.False(BaujahrSchema.Vollstaendig());

            var bericht = new List<string>();
            Assert.Equal(2, BaujahrSchema.Alle(bericht));
            Assert.Contains(bericht, z => z.Contains("2 von 2 Spalte(n) Baujahr angelegt"));
            Assert.Contains(bericht, z => z.Contains("(99 Spalten)"));
            Assert.True(BaujahrSchema.Vollstaendig());
            Assert.True(KuehluebergabeSchema.GebaeudeVollstaendig());
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Baujahr IS NOT NULL"));

            Assert.Equal(0, BaujahrSchema.Alle(null));
            Assert.Equal(GebaeudeSchema.SICHT_BAUJAHR, GebaeudeSchema.SichtSpalten());
        }

        /// <summary>Die Prüfung der Spalte greift an beiden Tabellen; NULL und ein Jahr im Bereich gehen durch.</summary>
        [Fact]
        public void Die_Pruefung_der_Spalte_weist_ungueltige_Jahre_ab()
        {
            if (!_db.Vorhanden) return;

            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                Assert.True(Wirft("UPDATE [" + t + "] SET Baujahr = 1499"), t);
                Assert.True(Wirft("UPDATE [" + t + "] SET Baujahr = 2101"), t);
                Assert.True(Wirft("UPDATE [" + t + "] SET Baujahr = 'ca. 1965'"), t);
                Assert.True(DataRepository.ExecuteSQL("UPDATE [" + t + "] SET Baujahr = ?", new DbParam("?", JAHR)), t);
                Assert.True(DataRepository.ExecuteSQL("UPDATE [" + t + "] SET Baujahr = NULL"), t);
            }
        }

        // =============================================================================
        //  Teil 3 - die vier fest verdrahteten Kopierwege von GebaeudeStammCtrl
        // =============================================================================

        /// <summary>KOPIERWEG 1 UND 2 — <c>BuildValueParams</c> und <c>Insert</c>: das Jahr kommt an, NULL bleibt NULL.</summary>
        [Fact]
        public void Kopierweg_1_und_2_BuildValueParams_und_Insert_tragen_das_Baujahr()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(new GebaeudeModel { Gebaeudename = "Baujahr-Probe mit", Nutzflaeche = 100, Baujahr = JAHR }));
            Assert.True(ctrl.Insert(new GebaeudeModel { Gebaeudename = "Baujahr-Probe ohne", Nutzflaeche = 100 }));

            Assert.Equal((long)JAHR, Convert.ToInt64(Zeile("SELECT Baujahr FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?",
                                                           "Baujahr-Probe mit")[0], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, Zeile("SELECT Baujahr FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", "Baujahr-Probe ohne")[0]);
            Assert.Equal(JAHR, Lesen("Baujahr-Probe mit").Baujahr);
            Assert.Null(Lesen("Baujahr-Probe ohne").Baujahr);
        }

        /// <summary>KOPIERWEG 3 — <c>Overwrite</c>: ein gesetztes Jahr wird NULL, ein leeres bekommt einen Wert.</summary>
        [Fact]
        public void Kopierweg_3_Overwrite_setzt_und_leert_das_Baujahr()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "Baujahr-Probe Overwrite";
            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(new GebaeudeModel { Gebaeudename = NAME, Nutzflaeche = 100, Baujahr = JAHR }));

            GebaeudeModel g = Lesen(NAME);
            g.Baujahr = null;
            Assert.True(ctrl.Overwrite(g));
            Assert.Equal(DBNull.Value, Zeile("SELECT Baujahr FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME)[0]);
            Assert.Null(Lesen(NAME).Baujahr);

            g = Lesen(NAME);
            g.Baujahr = 1972;
            Assert.True(ctrl.Overwrite(g));
            Assert.Equal(1972, Lesen(NAME).Baujahr);
        }

        /// <summary>KOPIERWEG 4 — <c>CopyFromStamm</c>: das Jahr geht in die Projektkopie; ein leeres wird KEIN 0.</summary>
        [Fact]
        public void Kopierweg_4_CopyFromStamm_traegt_das_Baujahr_und_haelt_NULL()
        {
            if (!_db.Vorhanden) return;

            DataRow z = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID LIMIT 1").Rows[0];
            int idProjekt = Convert.ToInt32(z["ID_Projekt"], CultureInfo.InvariantCulture);
            int idZuordnung = Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture);
            DataTable stamm = DataRepository.GetDataTable("SELECT ID, Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 2");
            int idMit = Convert.ToInt32(stamm.Rows[0]["ID"], CultureInfo.InvariantCulture);
            string mit = Convert.ToString(stamm.Rows[0]["Bezeichner"], CultureInfo.InvariantCulture);
            string ohne = Convert.ToString(stamm.Rows[1]["Bezeichner"], CultureInfo.InvariantCulture);
            SetzeBaujahr("Tab_Gebaeude_STAMM", idMit, JAHR);

            var ctrl = new GebaeudeStammCtrl();
            int neuMit = ctrl.CopyFromStamm(mit, idProjekt, idZuordnung);
            int neuOhne = ctrl.CopyFromStamm(ohne, idProjekt, idZuordnung);
            Assert.True(neuMit > 0 && neuOhne > 0);

            Assert.Equal((long)JAHR, Convert.ToInt64(Zeile("SELECT Baujahr FROM Tab_Gebaeude WHERE ID = ?", neuMit)[0],
                                                     CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, Zeile("SELECT Baujahr FROM Tab_Gebaeude WHERE ID = ?", neuOhne)[0]);

            var projekt = new GebaeudeCtrl();
            projekt.ReadAll("ID = " + neuMit.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(JAHR, Assert.Single(projekt.items).Baujahr);
        }

        /// <summary>DER NAMENSLESER DER SICHT: <c>ProjektGebaeudeCtrl</c> liefert das Baujahr NULL-erhaltend.</summary>
        [Fact]
        public void Der_Namensleser_der_Sicht_liefert_das_Baujahr_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            DataRow g = DataRepository.GetDataTable(
                "SELECT g.ID, z.ID_Projekt FROM Tab_Gebaeude g INNER JOIN Z_ProjektGebaeude z " +
                "ON z.ID = g.ID_ProjektGebaeude ORDER BY g.ID LIMIT 1").Rows[0];
            int idGebaeude = Convert.ToInt32(g["ID"], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(g["ID_Projekt"], CultureInfo.InvariantCulture);
            SetzeBaujahr("Tab_Gebaeude", idGebaeude, JAHR);

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            Assert.Equal(JAHR, ctrl.items.Single(x => x.ID_Gebaeude == idGebaeude).Baujahr);
            foreach (ProjektGebaeudeModel andere in ctrl.items.Where(x => x.ID_Gebaeude != idGebaeude))
                Assert.Null(andere.Baujahr);

            // Der Feldspiegel Katalog → Projekt (UebergabeHerleitungsquelle) führt das Feld mit.
            Assert.Contains("Baujahr", UebergabeHerleitungsquelle.Uebertragen());
        }

        // =============================================================================
        //  Teil 4 - die Wege über die ganze Zeile
        // =============================================================================

        /// <summary>„Duplizieren…" der Gebäudeverwaltung kopiert jede Spalte — auch das Baujahr.</summary>
        [Fact]
        public void Katalog_Duplizieren_traegt_das_Baujahr()
        {
            if (!_db.Vorhanden) return;

            int idStamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1"), CultureInfo.InvariantCulture);
            SetzeBaujahr("Tab_Gebaeude_STAMM", idStamm, JAHR);

            Katalogkopie.Ergebnis e = GebaeudeStammCtrl.Duplizieren(idStamm, "Baujahr-Probe Kopie");
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal((long)JAHR, Convert.ToInt64(Zeile("SELECT Baujahr FROM Tab_Gebaeude_STAMM WHERE ID = ?", e.Id)[0],
                                                     CultureInfo.InvariantCulture));
        }

        /// <summary>Das Projektduplikat trägt das Baujahr mit.</summary>
        [Fact]
        public void Projektduplikat_traegt_das_Baujahr()
        {
            if (!_db.Vorhanden) return;

            SetzeBaujahr("Tab_Gebaeude", GEBAEUDE, JAHR);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Baujahr");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal((long)JAHR, Convert.ToInt64(Zeile("SELECT Baujahr FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu)[0],
                                                     CultureInfo.InvariantCulture));
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt das Baujahr über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_das_Baujahr()
        {
            if (!_db.Vorhanden) return;

            SetzeBaujahr("Tab_Gebaeude", GEBAEUDE, JAHR);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-baujahr-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Baujahr", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal((long)JAHR, Convert.ToInt64(Zeile("SELECT Baujahr FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu)[0],
                                                         CultureInfo.InvariantCulture));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Teil 5 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Alle drei Wege führen den Schritt (Migration der Schale,
        /// Werkzeug <c>Testdatenbankschema</c>, Nachzieh-Liste der Tests) aus derselben Quelle, der
        /// Sichtneubau steht in Werkzeug und Testkopie ZULETZT, und die REPO-Datei trägt den Schritt
        /// (gelesen nur lesend und ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt_zuletzt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wBaujahr = werkzeug.IndexOf("BaujahrSchema.Alle(", StringComparison.Ordinal);
            Assert.True(wBaujahr > werkzeug.IndexOf("KuehluebergabeSchema.GebaeudeAlle(", StringComparison.Ordinal) &&
                        wBaujahr > werkzeug.IndexOf("ImportzuordnungSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Der fünfte Sichtneubau steht im Werkzeug nicht zuletzt.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_BAUJAHR = BaujahrSchema.SCHRITT", migration);
            int ortImport = migration.IndexOf("new Schritt(SCHRITT_IMPORTZUORDNUNG", StringComparison.Ordinal);
            int ortBaujahr = migration.IndexOf("new Schritt(SCHRITT_BAUJAHR", StringComparison.Ordinal);
            Assert.True(ortImport > 0 && ortBaujahr > ortImport, "Der Schritt steht nicht hinter S-F.");
            Assert.Contains("GebaeudeSchema.SQL_VIEW_BAUJAHR", migration);
            Assert.Contains("GebaeudeSchema.SQLITE_BAUJAHR", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vBaujahr = vorrichtung.IndexOf("BaujahrSchema.Alle(null)", StringComparison.Ordinal);
            Assert.True(vBaujahr > vorrichtung.IndexOf("KuehluebergabeSchema.GebaeudeAlle(null)", StringComparison.Ordinal) &&
                        vBaujahr > vorrichtung.IndexOf("ImportzuordnungSchema.Ausfuehren()", StringComparison.Ordinal),
                        "Der fünfte Sichtneubau steht in der Testkopie nicht zuletzt.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= BaujahrSchema.SCHRITT);
            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + t + "') WHERE name = 'Baujahr'"));
                Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM [" + t + "] WHERE Baujahr IS NOT NULL"));
            }
            using (SqliteCommand cmd = verbindung.CreateCommand())
            {
                cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = '" + GebaeudeSchema.VIEW + "'";
                Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static void SetzeBaujahr(string tabelle, int id, int jahr)
        {
            Assert.True(DataRepository.ExecuteSQL("UPDATE [" + tabelle + "] SET Baujahr = ? WHERE ID = ?",
                                                  new DbParam("?", jahr), new DbParam("?", id)));
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, object wert)
        {
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("?", wert));
            Assert.True(dt != null && dt.Rows.Count == 1, "Keine eindeutige Zeile: " + sql);
            return dt.Rows[0];
        }

        private static GebaeudeModel Lesen(string name)
        {
            var c = new GebaeudeStammCtrl();
            c.ReadAll("Bezeichner = '" + name + "'");
            Assert.Single(c.items);
            return c.items[0];
        }

        /// <summary>Scheitert die Anweisung an einer Prüfung? Der Vorgang wird nie festgeschrieben.</summary>
        private static bool Wirft(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            try
            {
                v.Ausfuehren(sql, parameter);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
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

        /// <summary>Die Bestandswerte der angefassten Tabellen, an denen der Schritt nichts ändern darf.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string sql in new[]
                     {
                         "SELECT ID, Nutzflaeche, Baualtersklasse, Kuehluebergabe_Aktiv, Uebergabe_Art FROM Tab_Gebaeude ORDER BY ID",
                         "SELECT ID, Nutzflaeche, Baualtersklasse, Kuehluebergabe_Aktiv, Uebergabe_Art FROM Tab_Gebaeude_STAMM ORDER BY ID",
                     })
            {
                DataTable dt = DataRepository.GetDataTable(sql);
                foreach (DataRow r in dt.Rows)
                    liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
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
