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
    /// Der Schemaschritt der <b>Verfahrensvolumina als Bezug der Füllstandslinie</b> (Auftrag F1,
    /// Anwenderauftrag 29.09.2026; Nummer bei <see cref="TwwFuellstandSchema.SCHRITT"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer und der Zielstand; die Prüfklausel des Grundschemas und
    /// des Schritts; dass Grundschema und Schritt Zeichen für Zeichen denselben CREATE-Text liefern;
    /// der Neubau auf der Testdatenbank (Zeilen, IDs, Zähler und Fremdschlüssel unverändert, ein
    /// zweiter Lauf ohne Wirkung); der Stand der Repo-Datei; Migration, Werkzeug und Testkopie führen
    /// den Schritt aus derselben Quelle.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben. Alle Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class TwwFuellstandSchemaTests
    {
        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf die Heizgrenze (Schritt 154); sie ist das Ziel.</summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_und_ist_das_Ziel()
        {
            Assert.Equal(KesselHeizgrenzeSchema.SCHRITT + 1, TwwFuellstandSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= TwwFuellstandSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + TwwFuellstandSchema.SCHRITT + ".");
            Paketanhebung.Stufe s = Assert.Single(Paketanhebung.Stufen, x => x.Nr == TwwFuellstandSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
        }

        /// <summary>
        /// EINE Wertemenge: Aufzählung, Grundschema (Spalte aus T3) und Schritt führen 1 bis 8; die
        /// alte Klausel steht nirgends mehr im Grundschema. 5 … 8 sind genau die Verfahren.
        /// </summary>
        [Fact]
        public void Grundschema_und_Schritt_fuehren_dieselbe_Wertemenge()
        {
            Assert.Equal(Enum.GetValues(typeof(ZapfFuellstandbezug)).Cast<int>().ToArray(),
                         TwwSchema.Werte(TwwSchema.FUELLSTAND_BEZUG_WERTE));
            Assert.Equal(8, (int)ZapfFuellstandbezug.VerfahrenKlassisch);
            Assert.Equal("CHECK (\"Fuellstand_Bezug\" IN (1,2,3,4,5,6,7,8))", TwwFuellstandSchema.CHECK_NEU);
            TwwSpalte t3 = TwwSchema.SpaltenT3.Single(s => s.Name == TwwSchema.SPALTE_FUELLSTAND_BEZUG);
            Assert.Equal(TwwSchema.TAB_TWW_PROJEKT, t3.Tabelle);
            Assert.Equal("INTEGER " + TwwFuellstandSchema.CHECK_NEU, t3.Definition);
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen)
                Assert.DoesNotContain(TwwFuellstandSchema.CHECK_ALT, a.Value, StringComparison.Ordinal);
            Assert.Equal(new[] { TwwSchema.TAB_TWW_PROJEKT }, TwwFuellstandSchema.TABELLEN.ToArray());

            // Jeder Bezug 5 … 8 zeigt auf genau ein Verfahren des Vergleichs, jeder Bezug 1 … 4 auf keines.
            foreach (ZapfSpeicherverfahren v in Enum.GetValues(typeof(ZapfSpeicherverfahren)).Cast<ZapfSpeicherverfahren>())
                Assert.Equal(v, TwwSpeicherauslegung.VerfahrenZuBezug(
                                    (ZapfFuellstandbezug)((int)v + TwwSpeicherauslegung.VERFAHREN_VERSATZ)));
            Assert.Null(TwwSpeicherauslegung.VerfahrenZuBezug(ZapfFuellstandbezug.NenninhaltPunkt));
            Assert.Null(TwwSpeicherauslegung.VerfahrenZuBezug(ZapfFuellstandbezug.BandMax));
        }

        /// <summary>Der Zieltext tauscht allein die Prüfklausel; fertig, ohne Spalte, fremd — benannt.</summary>
        [Fact]
        public void Der_Zieltext_tauscht_allein_die_Pruefklausel()
        {
            string alt = "CREATE TABLE \"T\" (\n    \"ID\" INTEGER PRIMARY KEY,\n    \"Fuellstand_Bezug\" INTEGER " +
                         TwwFuellstandSchema.CHECK_ALT + "\n) STRICT";
            Assert.Equal(alt.Replace(TwwFuellstandSchema.CHECK_ALT, TwwFuellstandSchema.CHECK_NEU),
                         TwwFuellstandSchema.Zieltext("T", alt));
            Assert.Null(TwwFuellstandSchema.Zieltext("T", alt.Replace(TwwFuellstandSchema.CHECK_ALT, TwwFuellstandSchema.CHECK_NEU)));
            Assert.Null(TwwFuellstandSchema.Zieltext("T", "CREATE TABLE \"T\" (\"ID\" INTEGER PRIMARY KEY) STRICT"));
            Assert.Throws<InvalidOperationException>(() => TwwFuellstandSchema.Zieltext("T",
                "CREATE TABLE \"T\" (\"Fuellstand_Bezug\" INTEGER CHECK (\"Fuellstand_Bezug\" IN (1,2))) STRICT"));
            Assert.Throws<InvalidOperationException>(() => TwwFuellstandSchema.Zieltext("T", alt.Replace(") STRICT", ")")));
            Assert.Throws<InvalidOperationException>(() => TwwFuellstandSchema.Zieltext("T", ""));
        }

        /// <summary>
        /// <b>Grundschema = Schritt.</b> Eine NEU angelegte Datenbank (T1, T3 und T3 „Typtage" mit den
        /// heutigen Konstanten) und eine unter der alten Klausel angelegte, deren Text der Schritt
        /// tauscht, tragen Zeichen für Zeichen denselben CREATE-Text — der Neubau führt genau diesen
        /// Text aus. Und die neue Klausel greift: 8 geht, 9 nicht.
        /// </summary>
        [Fact]
        public void Grundschema_und_Zieltext_des_Schritts_sind_zeichengleich()
        {
            using SqliteConnection neu = Speicher();
            Grundschema(neu, alt: false);
            using SqliteConnection alt = Speicher();
            Grundschema(alt, alt: true);

            string textNeu = Sql(neu, TwwSchema.TAB_TWW_PROJEKT);
            string textAlt = Sql(alt, TwwSchema.TAB_TWW_PROJEKT);
            Assert.Contains(TwwFuellstandSchema.CHECK_NEU, textNeu, StringComparison.Ordinal);
            Assert.Contains(TwwFuellstandSchema.CHECK_ALT, textAlt, StringComparison.Ordinal);
            Assert.Equal(textNeu, TwwFuellstandSchema.Zieltext(TwwSchema.TAB_TWW_PROJEKT, textAlt));
            Assert.Null(TwwFuellstandSchema.Zieltext(TwwSchema.TAB_TWW_PROJEKT, textNeu));

            Ausfuehren(neu, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (7)");
            Ausfuehren(neu, "INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\") VALUES (7)");
            foreach (int wert in TwwSchema.Werte(TwwSchema.FUELLSTAND_BEZUG_WERTE))
                Assert.False(Wirft(neu, "UPDATE \"Tab_TwwProjekt\" SET \"Fuellstand_Bezug\" = " +
                                        wert.ToString(CultureInfo.InvariantCulture)),
                             "Der Wert " + wert + " wird abgewiesen.");
            Assert.True(Wirft(neu, "UPDATE \"Tab_TwwProjekt\" SET \"Fuellstand_Bezug\" = 9"));
            Assert.True(Wirft(neu, "UPDATE \"Tab_TwwProjekt\" SET \"Fuellstand_Bezug\" = 0"));
            // Vor dem Schritt wies dieselbe Tabelle die Verfahren ab.
            Ausfuehren(alt, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (7)");
            Ausfuehren(alt, "INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\") VALUES (7)");
            Assert.True(Wirft(alt, "UPDATE \"Tab_TwwProjekt\" SET \"Fuellstand_Bezug\" = 5"));
        }

        // =============================================================================
        //  Teil 2 - der Neubau auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// <b>Der Neubau</b> auf einer Arbeitskopie, die vorher auf die alte Klausel zurückgebaut ist:
        /// eine Tabelle offen, danach keine; der CREATE-Text wieder der des Grundschemas; jede Zeile,
        /// jede ID und der Zählerstand wie vorher; kein verletzter Fremdschlüssel, auch nicht an der
        /// Kindtabelle; ein zweiter Lauf baut nichts. Danach nimmt die Tabelle ein Verfahren an.
        /// </summary>
        [Fact]
        public void Der_Neubau_haelt_Zeilen_Ids_und_Zaehler_und_laeuft_ein_zweites_Mal_ohne_Wirkung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string tabelle = TwwSchema.TAB_TWW_PROJEKT;
            string textVorher = SqlDerKopie(tabelle);
            string[] zeilenVorher = Abzug(tabelle);
            string[] kinderVorher = Abzug(TwwSchema.TAB_TWW_KONSTRUKTORZEILE);
            long zaehlerVorher = Zaehler(tabelle);

            Zurueckbauen();
            Assert.Equal(1, TwwFuellstandSchema.Offen());
            Assert.False(TwwFuellstandSchema.Vollstaendig());

            var bericht = new List<string>();
            Assert.Equal(1, TwwFuellstandSchema.Ausfuehren(bericht));
            Assert.Equal(0, TwwFuellstandSchema.Offen());
            Assert.True(TwwFuellstandSchema.Vollstaendig());
            Assert.Contains(bericht, z => z.StartsWith(tabelle + ": neu gebaut", StringComparison.Ordinal));

            Assert.Equal(textVorher, SqlDerKopie(tabelle));
            Assert.Equal(zeilenVorher, Abzug(tabelle));
            Assert.Equal(kinderVorher, Abzug(TwwSchema.TAB_TWW_KONSTRUKTORZEILE));
            Assert.Equal(zaehlerVorher, Zaehler(tabelle));
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM pragma_foreign_key_check")));
            Assert.Equal("ok", Convert.ToString(DataRepository.ExecuteScalar("PRAGMA integrity_check")));
            Assert.False(DataRepository.TabelleVorhanden(TwwFuellstandSchema.Hilfsname(tabelle)));

            // Ein zweiter Lauf baut nichts.
            var zweiter = new List<string>();
            Assert.Equal(0, TwwFuellstandSchema.Ausfuehren(zweiter));
            Assert.Equal(zeilenVorher, Abzug(tabelle));

            // Und die Klausel trägt nun jedes Verfahren.
            long id = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM " + tabelle),
                                      CultureInfo.InvariantCulture);
            foreach (int wert in TwwSchema.Werte(TwwSchema.FUELLSTAND_BEZUG_WERTE))
                Assert.True(DataRepository.ExecuteSQL("UPDATE " + tabelle + " SET Fuellstand_Bezug = ? WHERE ID = ?",
                                                      new DbParam("?", wert), new DbParam("?", id)),
                            "Der Wert " + wert + " wird abgewiesen.");
        }

        // =============================================================================
        //  Teil 3 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und
        /// Testkopie führen den Schritt aus derselben Quelle und hinter der Heizgrenze; die Repo-Datei
        /// trägt ihn: <c>Tab_TwwProjekt</c> mit dem CREATE-Text des Grundschemas (lesend geprüft, ohne
        /// Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("TwwFuellstandSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("KesselHeizgrenzeSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Der Schritt steht im Werkzeug nicht hinter der Heizgrenze.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_TWW_FUELLSTAND_VERFAHREN = TwwFuellstandSchema.SCHRITT", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_KESSEL_HEIZGRENZE", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_TWW_FUELLSTAND_VERFAHREN", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ort > ortVorher, "Der Schritt steht nicht hinter 154.");
            Assert.Contains("TwwFuellstandSchema.Ausfuehren(zeilen)", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("TwwFuellstandSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("KesselHeizgrenzeSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Der Schritt steht in der Testkopie nicht hinter der Heizgrenze.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            Assert.True(Skalar(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") is long stand
                        && stand >= TwwFuellstandSchema.SCHRITT);

            using SqliteConnection grund = Speicher();
            Grundschema(grund, alt: false);
            Assert.Equal(Sql(grund, TwwSchema.TAB_TWW_PROJEKT), Sql(verbindung, TwwSchema.TAB_TWW_PROJEKT));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Das Grundschema der Tww-Tabellen in einer leeren Datenbank: T1 bis T5, die Spalten aus T3
        /// und die Spalten aus T3 „Typtage" — in genau der Reihenfolge, in der sie die Migration
        /// anlegt — mit den heutigen Konstanten oder, für den Stand vor dem Schritt, mit der alten
        /// Klausel. <c>Tab_Projekt</c> entsteht als Rumpf, damit der Fremdschlüssel trägt.
        /// </summary>
        private static void Grundschema(SqliteConnection c, bool alt)
        {
            string Klausel(string sql) => alt ? sql.Replace(TwwFuellstandSchema.CHECK_NEU, TwwFuellstandSchema.CHECK_ALT) : sql;
            Ausfuehren(c, "CREATE TABLE \"Tab_Projekt\" (\"ID\" INTEGER PRIMARY KEY)");
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen) Ausfuehren(c, Klausel(a.Value));
            foreach (TwwSpalte s in TwwSchema.SpaltenT3) Ausfuehren(c, Klausel(TwwSchema.SpalteAnlegen(s)));
            foreach (TwwSpalte s in TwwSchema.SpaltenT3Typtage) Ausfuehren(c, Klausel(TwwSchema.SpalteAnlegen(s)));
        }

        /// <summary>Baut die Tabelle der Arbeitskopie auf die alte Klausel zurück — mit dem Rezept des Schritts.</summary>
        private static void Zurueckbauen()
        {
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                string text = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                        new DbParam("p1", TwwSchema.TAB_TWW_PROJEKT)), CultureInfo.InvariantCulture);
                TwwBezugsartSchema.Neubau(v, TwwSchema.TAB_TWW_PROJEKT,
                                          text.Replace(TwwFuellstandSchema.CHECK_NEU, TwwFuellstandSchema.CHECK_ALT));
                v.Commit();
            }
            Assert.Contains(TwwFuellstandSchema.CHECK_ALT, SqlDerKopie(TwwSchema.TAB_TWW_PROJEKT), StringComparison.Ordinal);
        }

        private static string SqlDerKopie(string tabelle)
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                             new DbParam("?", tabelle)), CultureInfo.InvariantCulture);

        /// <summary>Jede Zeile der Tabelle als Text, nach ID — für „Zeilen und IDs unverändert".</summary>
        private static string[] Abzug(string tabelle)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" ORDER BY ID");
            return dt.Rows.Cast<DataRow>()
                     .Select(r => string.Join("|", r.ItemArray.Select(w => Convert.ToString(w, CultureInfo.InvariantCulture))))
                     .ToArray();
        }

        private static long Zaehler(string tabelle)
        {
            object o = DataRepository.ExecuteScalar("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("?", tabelle));
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static SqliteConnection Speicher()
        {
            var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            return c;
        }

        private static string Sql(SqliteConnection c, string tabelle)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $n";
            cmd.Parameters.AddWithValue("$n", tabelle);
            return Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static object Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
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

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
