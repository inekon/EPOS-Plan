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
    /// Der Schemaschritt der <b>Bezugsart Zimmer</b> (Auftrag A2, Entscheide E-A2-1, E-A2-3 und E-A2-4;
    /// Nummer bei <see cref="TwwBezugsartSchema.SCHRITT"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer und der Zielstand; die Prüfklausel des Grundschemas und des
    /// Schritts; dass Grundschema und Schritt Zeichen für Zeichen denselben CREATE-Text liefern; der
    /// Neubau auf der Testdatenbank (Zeilen, IDs, Zähler und Fremdschlüssel unverändert, ein zweiter
    /// Lauf ohne Wirkung); die Nachführung der gespeicherten Paketzeilen samt ihrer Grenze; der Stand der
    /// Repo-Datei; Migration, Werkzeug und Testkopie führen den Schritt aus derselben Quelle.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben. Alle Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class TwwBezugsartSchemaTests
    {
        private const string HOTEL_FRUEHER = "Hotel (aus Messung)";
        private const string HOTEL = "Hotel (aus Messung, je Zimmer)";

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf die Konditionierungsvorlagen (Schritt 152); sie ist das Ziel.</summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_und_ist_das_Ziel()
        {
            Assert.Equal(KonditionierungVorlagenSchema.SCHRITT + 1, TwwBezugsartSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= TwwBezugsartSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + TwwBezugsartSchema.SCHRITT + ".");
            Paketanhebung.Stufe s = Assert.Single(Paketanhebung.Stufen, x => x.Nr == TwwBezugsartSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Import, s.Wirkung);
        }

        /// <summary>
        /// EINE Wertemenge: Aufzählung, Grundschema (Nutzungsart aus T1, Bedarfstag aus T3) und Schritt
        /// führen 1 bis 8; die alte Klausel steht nirgends mehr im Grundschema.
        /// </summary>
        [Fact]
        public void Grundschema_und_Schritt_fuehren_dieselbe_Wertemenge()
        {
            Assert.Equal(Enum.GetValues(typeof(ZapfBezugsart)).Cast<int>().ToArray(), TwwSchema.Werte(TwwSchema.BEZUGSART_WERTE));
            Assert.Equal(8, (int)ZapfBezugsart.Zimmer);
            Assert.Equal("CHECK (\"Bezugsart\" IN (1,2,3,4,5,6,7,8))", TwwBezugsartSchema.CHECK_NEU);
            Assert.Contains(TwwBezugsartSchema.CHECK_NEU, TwwSchema.SQL_CREATE_NUTZUNGSART, StringComparison.Ordinal);
            TwwSpalte t3 = TwwSchema.SpaltenT3.Single(s => s.Tabelle == TwwSchema.TAB_TWW_BEDARFSTAG_STAMM);
            Assert.Equal("INTEGER " + TwwBezugsartSchema.CHECK_NEU, t3.Definition);
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen)
                Assert.DoesNotContain(TwwBezugsartSchema.CHECK_ALT, a.Value, StringComparison.Ordinal);
            Assert.Equal(new[] { TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM },
                         TwwBezugsartSchema.TABELLEN.ToArray());
        }

        /// <summary>Der Zieltext tauscht allein die Prüfklausel; fertig, ohne Spalte, fremd — benannt.</summary>
        [Fact]
        public void Der_Zieltext_tauscht_allein_die_Pruefklausel()
        {
            string alt = "CREATE TABLE \"T\" (\n    \"ID\" INTEGER PRIMARY KEY,\n    \"Bezugsart\" INTEGER " +
                         TwwBezugsartSchema.CHECK_ALT + "\n) STRICT";
            Assert.Equal(alt.Replace(TwwBezugsartSchema.CHECK_ALT, TwwBezugsartSchema.CHECK_NEU),
                         TwwBezugsartSchema.Zieltext("T", alt));
            Assert.Null(TwwBezugsartSchema.Zieltext("T", alt.Replace(TwwBezugsartSchema.CHECK_ALT, TwwBezugsartSchema.CHECK_NEU)));
            Assert.Null(TwwBezugsartSchema.Zieltext("T", "CREATE TABLE \"T\" (\"ID\" INTEGER PRIMARY KEY) STRICT"));
            Assert.Throws<InvalidOperationException>(() => TwwBezugsartSchema.Zieltext("T",
                "CREATE TABLE \"T\" (\"Bezugsart\" INTEGER CHECK (\"Bezugsart\" IN (1,2))) STRICT"));
            Assert.Throws<InvalidOperationException>(() => TwwBezugsartSchema.Zieltext("T", alt.Replace(") STRICT", ")")));
            Assert.Throws<InvalidOperationException>(() => TwwBezugsartSchema.Zieltext("T", ""));
        }

        /// <summary>
        /// <b>Grundschema = Schritt.</b> Eine NEU angelegte Datenbank (T1 und T3 mit den heutigen
        /// Konstanten) und eine unter der alten Klausel angelegte, deren Text der Schritt tauscht, tragen
        /// Zeichen für Zeichen denselben CREATE-Text — der Neubau führt genau diesen Text aus.
        /// </summary>
        [Fact]
        public void Grundschema_und_Zieltext_des_Schritts_sind_zeichengleich()
        {
            using SqliteConnection neu = Speicher();
            Grundschema(neu, alt: false);
            using SqliteConnection alt = Speicher();
            Grundschema(alt, alt: true);

            foreach (string t in TwwBezugsartSchema.TABELLEN)
            {
                string textNeu = Sql(neu, t);
                string textAlt = Sql(alt, t);
                Assert.Contains(TwwBezugsartSchema.CHECK_NEU, textNeu, StringComparison.Ordinal);
                Assert.Contains(TwwBezugsartSchema.CHECK_ALT, textAlt, StringComparison.Ordinal);
                Assert.Equal(textNeu, TwwBezugsartSchema.Zieltext(t, textAlt));
                Assert.Null(TwwBezugsartSchema.Zieltext(t, textNeu));
            }
            // Die neue Klausel greift: 8 geht, 9 nicht — an beiden Tabellen.
            Assert.False(Wirft(neu, "UPDATE \"Tab_TwwBedarfstag_STAMM\" SET \"Bezugsart\" = 8"));
            Assert.True(Wirft(neu, "INSERT INTO \"Tab_TwwBedarfstag_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Quelle_Art\", " +
                                   "\"Quelle\", \"Version\", \"Herkunftsart\", \"Status\", \"Bezugsart\") VALUES ('X', 'V', 2, 'Q', " +
                                   "'V', 'FIKTIV', 'EIGEN', 9)"));
            Assert.False(Wirft(neu, "INSERT INTO \"Tab_TwwBedarfstag_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Quelle_Art\", " +
                                    "\"Quelle\", \"Version\", \"Herkunftsart\", \"Status\", \"Bezugsart\") VALUES ('Y', 'V', 2, 'Q', " +
                                    "'V', 'FIKTIV', 'EIGEN', 8)"));
        }

        // =============================================================================
        //  Teil 2 - der Neubau auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// <b>Der Neubau</b> auf einer Arbeitskopie, die vorher auf die alte Klausel zurückgebaut ist:
        /// zwei Tabellen offen, danach keine; der CREATE-Text wieder der des Grundschemas; jede Zeile,
        /// jede ID und der Zählerstand wie vorher; kein verletzter Fremdschlüssel; ein zweiter Lauf baut
        /// nichts.
        /// </summary>
        [Fact]
        public void Der_Neubau_haelt_Zeilen_Ids_und_Zaehler_und_laeuft_ein_zweites_Mal_ohne_Wirkung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var textVorher = TwwBezugsartSchema.TABELLEN.ToDictionary(t => t, SqlDerKopie);
            var zeilenVorher = TwwBezugsartSchema.TABELLEN.ToDictionary(t => t, Abzug);
            var zaehlerVorher = TwwBezugsartSchema.TABELLEN.ToDictionary(t => t, Zaehler);
            // Die Hotelzeile trägt Zimmer (8) - vor dem Rückbau auf die alte Klausel kurz Betten.
            long hotel = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezugsart = ?", new DbParam("?", (int)ZapfBezugsart.Zimmer)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_TwwNutzungsart_STAMM SET Bezugsart = 3 WHERE ID = ?",
                                                  new DbParam("?", hotel)));
            Zurueckbauen();
            Assert.Equal(2, TwwBezugsartSchema.Offen());
            Assert.False(TwwBezugsartSchema.Vollstaendig());

            var bericht = new List<string>();
            Assert.Equal(2, TwwBezugsartSchema.Ausfuehren(bericht));
            Assert.Equal(0, TwwBezugsartSchema.Offen());
            Assert.True(TwwBezugsartSchema.Vollstaendig());
            Assert.Contains(bericht, z => z.StartsWith(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ": neu gebaut", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.StartsWith(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + ": neu gebaut", StringComparison.Ordinal));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_TwwNutzungsart_STAMM SET Bezugsart = ? WHERE ID = ?",
                                                  new DbParam("?", (int)ZapfBezugsart.Zimmer), new DbParam("?", hotel)));

            foreach (string t in TwwBezugsartSchema.TABELLEN)
            {
                Assert.Equal(textVorher[t], SqlDerKopie(t));
                Assert.Equal(zeilenVorher[t], Abzug(t));
                Assert.Equal(zaehlerVorher[t], Zaehler(t));
            }
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM pragma_foreign_key_check")));
            Assert.Equal("ok", Convert.ToString(DataRepository.ExecuteScalar("PRAGMA integrity_check")));
            // Kein Hilfsname bleibt liegen.
            foreach (string t in TwwBezugsartSchema.TABELLEN)
                Assert.False(DataRepository.TabelleVorhanden(TwwBezugsartSchema.Hilfsname(t)));

            // Ein zweiter Lauf baut nichts und führt nichts nach.
            var zweiter = new List<string>();
            Assert.Equal(0, TwwBezugsartSchema.Ausfuehren(zweiter));
            Assert.Contains("0 Paketzeile(n) in einem frueheren Stand nachgefuehrt", zweiter);
            foreach (string t in TwwBezugsartSchema.TABELLEN) Assert.Equal(zeilenVorher[t], Abzug(t));
        }

        /// <summary>
        /// <b>Die Nachführung der gespeicherten Paketzeilen (E-A2-4 a)</b> und ihre Grenze: Eine
        /// Auslieferungszeile des Paketteils im früheren Stand heißt danach wie heute und trägt Zimmer —
        /// dieselbe ID, die Zone zeigt weiter darauf. Führt ihre Katalogversion den heutigen Namen schon,
        /// bleibt der Name und nur die Bezugsart geht mit, benannt. Eine Anwenderzeile gleichen Namens, eine
        /// fremde Provenienz und eine andere Bezugsart bleiben, wie sie sind.
        /// </summary>
        [Fact]
        public void Die_Nachfuehrung_trifft_nur_Auslieferungszeilen_des_Paketteils()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Probesatz Hotel", "PROBE-A2");

            int geliefert = Hotelzeile(HOTEL_FRUEHER, "FREI-1", satz, TwwSchema.STATUS_AUSLIEFERUNG, true, 3, "FREI-1",
                                       TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
            int zone = TwwTestdatenbank.ZoneAnlegen(1006, geliefert, "Zone Hotel", 40.0);
            int dublette = Hotelzeile(HOTEL_FRUEHER, "PAKET-2", satz, TwwSchema.STATUS_AUSLIEFERUNG, true, 3, "FREI-1",
                                      TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
            int eingespielt = Hotelzeile(HOTEL, "PAKET-2", satz, TwwSchema.STATUS_IMPORT, false, 8, "FREI-1",
                                         TwwSchema.HERKUNFT_IMPORT);
            int zwischenstand = Hotelzeile(HOTEL, "PAKET-3", satz, TwwSchema.STATUS_AUSLIEFERUNG, true, 3, "FREI-1",
                                           TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
            int anwender = Hotelzeile(HOTEL_FRUEHER, "EIGEN-1", satz, TwwSchema.STATUS_EIGEN, false, 3, "FREI-1",
                                      TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
            int fremd = Hotelzeile(HOTEL_FRUEHER, "PAKET-4", satz, TwwSchema.STATUS_AUSLIEFERUNG, true, 3, "FREI-2",
                                   TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
            int personen = Hotelzeile(HOTEL_FRUEHER, "PAKET-5", satz, TwwSchema.STATUS_AUSLIEFERUNG, true, 1, "FREI-1",
                                      TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
            int verfahren = Hotelzeile(HOTEL_FRUEHER, "PAKET-6", satz, TwwSchema.STATUS_AUSLIEFERUNG, true, 3, "FREI-1",
                                       TwwSchema.HERKUNFT_VERFAHREN);
            long zeilenVorher = Anzahl("SELECT COUNT(*) FROM Tab_TwwNutzungsart_STAMM");

            Assert.Equal(3L, PaketteilNachfuehrung.Offen());
            Assert.False(TwwBezugsartSchema.Vollstaendig());
            var bericht = new List<string>();
            Assert.Equal(0, TwwBezugsartSchema.Ausfuehren(bericht));      // die Tabellen stehen schon
            Assert.Equal(0L, PaketteilNachfuehrung.Offen());
            Assert.True(TwwBezugsartSchema.Vollstaendig());
            Assert.Contains("3 Paketzeile(n) in einem frueheren Stand nachgefuehrt", bericht);

            AssertZeile(geliefert, HOTEL, ZapfBezugsart.Zimmer);
            Assert.Equal((long)geliefert, Anzahl("SELECT ID_Nutzungsart FROM Tab_TwwZone WHERE ID = " + zone));
            AssertZeile(dublette, HOTEL_FRUEHER, ZapfBezugsart.Zimmer);
            Assert.Contains(bericht, z => z.Contains("ID " + dublette + " ", StringComparison.Ordinal)
                                          && z.Contains("bleibt unter seinem Namen", StringComparison.Ordinal));
            AssertZeile(eingespielt, HOTEL, ZapfBezugsart.Zimmer);
            AssertZeile(zwischenstand, HOTEL, ZapfBezugsart.Zimmer);
            AssertZeile(anwender, HOTEL_FRUEHER, ZapfBezugsart.Betten);
            AssertZeile(fremd, HOTEL_FRUEHER, ZapfBezugsart.Betten);
            AssertZeile(personen, HOTEL_FRUEHER, ZapfBezugsart.Personen);
            AssertZeile(verfahren, HOTEL_FRUEHER, ZapfBezugsart.Betten);
            Assert.Equal(zeilenVorher, Anzahl("SELECT COUNT(*) FROM Tab_TwwNutzungsart_STAMM"));

            // Wiederholbar: ein zweiter Lauf führt nichts nach.
            var zweiter = new List<string>();
            TwwBezugsartSchema.Ausfuehren(zweiter);
            Assert.Contains("0 Paketzeile(n) in einem frueheren Stand nachgefuehrt", zweiter);
        }

        // =============================================================================
        //  Teil 3 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und
        /// Testkopie führen den Schritt aus derselben Quelle und hinter den Konditionierungsprofilen; die
        /// Repo-Datei trägt ihn: beide Tabellen mit dem CREATE-Text des Grundschemas, die Hotelzeile unter
        /// ihrem heutigen Namen mit der Bezugsart Zimmer (lesend geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("TwwBezugsartSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("KonditionierungSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Der Schritt steht im Werkzeug nicht hinter den Konditionierungsprofilen.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_TWW_BEZUGSART_ZIMMER = TwwBezugsartSchema.SCHRITT", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_KONDITIONIERUNG", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_TWW_BEZUGSART_ZIMMER", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ort > ortVorher, "Der Schritt steht nicht hinter 151.");
            Assert.Contains("TwwBezugsartSchema.Ausfuehren(zeilen)", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("TwwBezugsartSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("KonditionierungSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Der Schritt steht in der Testkopie nicht hinter den Konditionierungsprofilen.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            Assert.True(Skalar(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") is long stand
                        && stand >= TwwBezugsartSchema.SCHRITT);

            using SqliteConnection grund = Speicher();
            Grundschema(grund, alt: false);
            foreach (string t in TwwBezugsartSchema.TABELLEN)
                Assert.Equal(Sql(grund, t), Sql(verbindung, t));

            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT Bezugsart FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = $n";
            cmd.Parameters.AddWithValue("$n", HOTEL);
            Assert.Equal((long)ZapfBezugsart.Zimmer, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            Assert.Equal(0L, Skalar(verbindung, "SELECT COUNT(*) FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = '" +
                                                HOTEL_FRUEHER + "'"));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Das Grundschema der beiden Tabellen in einer leeren Datenbank: T1 und T3 (Spalte am Bedarfstag)
        /// — mit den heutigen Konstanten oder, für den Stand vor dem Schritt, mit der alten Klausel.
        /// </summary>
        private static void Grundschema(SqliteConnection c, bool alt)
        {
            string Klausel(string sql) => alt ? sql.Replace(TwwBezugsartSchema.CHECK_NEU, TwwBezugsartSchema.CHECK_ALT) : sql;
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen) Ausfuehren(c, Klausel(a.Value));
            foreach (TwwSpalte s in TwwSchema.SpaltenT3) Ausfuehren(c, Klausel(TwwSchema.SpalteAnlegen(s)));
        }

        /// <summary>Baut beide Tabellen der Arbeitskopie auf die alte Klausel zurück — mit dem Rezept des Schritts.</summary>
        private static void Zurueckbauen()
        {
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                foreach (string t in TwwBezugsartSchema.TABELLEN)
                {
                    string text = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                            new DbParam("p1", t)), CultureInfo.InvariantCulture);
                    TwwBezugsartSchema.Neubau(v, t, text.Replace(TwwBezugsartSchema.CHECK_NEU, TwwBezugsartSchema.CHECK_ALT));
                }
                v.Commit();
            }
            foreach (string t in TwwBezugsartSchema.TABELLEN)
                Assert.Contains(TwwBezugsartSchema.CHECK_ALT, SqlDerKopie(t), StringComparison.Ordinal);
        }

        /// <summary>Eine Nutzungsart mit den Merkmalen, nach denen die Regel fragt; die übrigen Werte erfunden.</summary>
        private static int Hotelzeile(string name, string version, int satz, string status, bool readOnly, int bezugsart,
                                      string bedarfVersion, string herkunft)
        {
            int id = TwwTestdatenbank.NutzungsartAnlegen(name, version, satz, status, readOnly);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwNutzungsart_STAMM SET Bezugsart = ?, Bedarf_Version = ?, Bedarf_Herkunftsart = ? WHERE ID = ?",
                new DbParam("?", bezugsart), new DbParam("?", bedarfVersion), new DbParam("?", herkunft), new DbParam("?", id)));
            return id;
        }

        private static void AssertZeile(int id, string name, ZapfBezugsart bezug)
        {
            DataRow r = Assert.Single(DataRepository.GetDataTable(
                "SELECT Bezeichner, Bezugsart FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("?", id)).Rows.Cast<DataRow>());
            Assert.Equal(name, Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture));
            Assert.Equal((long)bezug, Convert.ToInt64(r["Bezugsart"], CultureInfo.InvariantCulture));
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

        private static long Anzahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

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
