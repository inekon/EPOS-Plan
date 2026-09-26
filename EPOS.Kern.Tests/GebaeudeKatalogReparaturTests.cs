using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Reparatur der Gebäude-Katalogsätze</b> — Schemaschritt
    /// <see cref="GebaeudeKatalogReparatur.SCHRITT"/> (Welle #485, Konzept
    /// Administrationsdialoge 7.1 (a)): der Krankenhaussatz (U-Wert Fenster 0,09, Nordfenster
    /// 10 000 m²), vier Sätze ohne „Fläche je Nutzer", acht Testreste.
    ///
    /// <para><b>Jeder Fall auf seiner eigenen Arbeitskopie.</b> Die Repo-Datei steht schon auf
    /// dem Zielstand; die Fälle legen das Schadensbild deshalb wieder an (Werte des Befunds
    /// #473) und lassen den Schritt darüber laufen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeKatalogReparaturTests
    {
        private const string KRANKENHAUS = GebaeudeKatalogReparatur.KRANKENHAUS;

        /// <summary>Die Projektkopie 10614 (Projekt 1007, „EFH-A-TS-212").</summary>
        private const int GEBAEUDE_1007 = 10614;

        // =====================================================================
        //  1 - Vorher / nachher je Satz, zweiter Lauf tut nichts
        // =====================================================================

        /// <summary>
        /// Das Schadensbild des Befunds, wie es vor dem Schritt stand; nach dem Schritt trägt
        /// jeder Satz seine Berichtigung, die acht Testreste sind fort, und ein zweiter Lauf
        /// findet nichts mehr.
        /// </summary>
        [Fact]
        public void Das_Schadensbild_wird_je_Satz_berichtigt_und_der_Schritt_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SchadensbildAnlegen();
            Assert.Equal(275L + 8, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)
            Assert.Equal(14L, GebaeudeKatalogReparatur.Offen());

            GebaeudeKatalogReparatur.Bericht b = GebaeudeKatalogReparatur.Ausfuehren();
            Assert.Equal(6, b.Repariert.Count);
            Assert.Equal(GebaeudeKatalogReparatur.Testreste, b.Geloescht);
            Assert.Empty(b.Benutzt);
            Assert.Equal(0L, GebaeudeKatalogReparatur.Offen());

            // Krankenhaus: U-Wert 1,3, Nord 250, gesamt = Sued + Ost/West + Nord; Sued und Ost/West bleiben.
            Assert.Equal(1.3, Kommazahl("SELECT k_Wert_Fenster FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS), 9);
            Assert.Equal(250.0, Kommazahl("SELECT Fensterflaeche_Nord FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS));
            Assert.Equal(400.0, Kommazahl("SELECT Fensterflaeche_Ost_West FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS));
            Assert.Equal(1245.9, Kommazahl("SELECT Fensterflaeche_Sued FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS), 4);
            Assert.Equal(1895.9, Kommazahl("SELECT gesamte_Fensterflaeche FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS), 4);

            // Die vier Saetze: Flaeche je Nutzer = Wohnflaeche / Bewohner.
            Assert.Equal(40.0, Kommazahl("SELECT Flaeche_Nutzer FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'EFH-BZ2'"));
            Assert.Equal(50.0, Kommazahl("SELECT Flaeche_Nutzer FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'KrankenH-F-U-400'"));
            Assert.Equal(360.24, Kommazahl("SELECT Bewohner FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'KrankenH-F-U-400'"), 9);
            Assert.Equal(31.78, Kommazahl("SELECT Flaeche_Nutzer FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'KMEH-M-U-54'"), 9);
            Assert.Equal(28.71, Kommazahl("SELECT Flaeche_Nutzer FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'Z-EFH-A-S-126'"), 9);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Flaeche_Nutzer IS NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Flaeche_Nutzer IS NOT NULL " +
                                  "AND abs(Flaeche_Nutzer - Wohnflaeche_gesamt / Bewohner) > 0.01"));
            Assert.Equal(275L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)

            // Zweiter Lauf: nichts mehr zu tun.
            GebaeudeKatalogReparatur.Bericht zweiter = GebaeudeKatalogReparatur.Ausfuehren();
            Assert.Empty(zweiter.Repariert);
            Assert.Empty(zweiter.Geloescht);
            Assert.Empty(zweiter.Benutzt);
        }

        // =====================================================================
        //  2 - Nur das Schadensbild: abweichende Saetze und Projektkopien bleiben
        // =====================================================================

        /// <summary>
        /// Ein Satz, der schon berichtigt oder ANDERS ist, bleibt unberührt: Der Krankenhaussatz
        /// mit eigenem U-Wert 0,5 behält ihn (nur das Nordfenster mit dem Bild wird berichtigt),
        /// „EFH-BZ2" mit anderer Bewohnerzahl behält die leere Fläche je Nutzer. Ein
        /// Auslieferungssatz (<c>ReadOnly</c> = 1) mit dem Bild wird berichtigt; eine
        /// PROJEKTKOPIE mit dem Bild bleibt, wie sie ist.
        /// </summary>
        [Fact]
        public void Ein_abweichender_Satz_und_die_Projektkopie_bleiben_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SchadensbildAnlegen();
            Sql("UPDATE Tab_Gebaeude_STAMM SET k_Wert_Fenster = 0.5 WHERE Bezeichner = ?", KRANKENHAUS);
            Sql("UPDATE Tab_Gebaeude_STAMM SET Bewohner = 5 WHERE Bezeichner = 'EFH-BZ2'");
            Sql("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 1 WHERE Bezeichner = 'KMEH-M-U-54'");
            Sql("UPDATE Tab_Gebaeude SET Gebaeudename = ?, k_Wert_Fenster = 0.09, Fensterflaeche_Nord = 10000 WHERE ID = ?",
                KRANKENHAUS, GEBAEUDE_1007);

            GebaeudeKatalogReparatur.Bericht b = GebaeudeKatalogReparatur.Ausfuehren();

            Assert.Equal(0.5, Kommazahl("SELECT k_Wert_Fenster FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS), 9);
            Assert.Equal(250.0, Kommazahl("SELECT Fensterflaeche_Nord FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS));
            Assert.Null(Wert("SELECT Flaeche_Nutzer FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'EFH-BZ2'"));
            Assert.Equal(31.78, Kommazahl("SELECT Flaeche_Nutzer FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'KMEH-M-U-54'"), 9);
            Assert.Equal(1L, Zahl("SELECT ReadOnly FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'KMEH-M-U-54'"));
            Assert.DoesNotContain(b.Repariert, r => r.StartsWith("EFH-BZ2:", StringComparison.Ordinal));
            Assert.DoesNotContain(b.Repariert, r => r.Contains("U-Wert", StringComparison.Ordinal));

            // Die Projektkopie traegt ihr Bild weiter - Rechengrundlage ihres Projekts.
            Assert.Equal(0.09, Kommazahl("SELECT k_Wert_Fenster FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007), 9);
            Assert.Equal(10000.0, Kommazahl("SELECT Fensterflaeche_Nord FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007));

            // Die abweichenden Saetze zaehlen nicht als offen - sie tragen kein Bild.
            Assert.Equal(0L, GebaeudeKatalogReparatur.Offen());
        }

        // =====================================================================
        //  3 - Ein benutzter Testrest bleibt
        // =====================================================================

        /// <summary>
        /// Ein Testrest, den eine Projektkopie führt, bleibt stehen — über den Katalogverweis
        /// ebenso wie (ohne Verweis) über den Namen, groß/klein egal; der Bericht nennt ihn mit
        /// dem Projekt. Die übrigen Testreste werden gelöscht, und die Nachprobe ist 0: Ein
        /// benutzter Rest bleibt mit Absicht.
        /// </summary>
        [Fact]
        public void Ein_benutzter_Testrest_wird_nicht_geloescht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SchadensbildAnlegen();
            long idTest = Zahl("SELECT ID FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'Z2-EFH-A-S-test'");
            Sql("UPDATE Tab_Gebaeude SET ID_Gebaeude_Stamm = ? WHERE ID = ?", idTest, GEBAEUDE_1007);
            long projekt = Zahl("SELECT ID_Projekt FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007);
            long zweite = Zahl("SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID <> ?", GEBAEUDE_1007);
            Sql("UPDATE Tab_Gebaeude SET ID_Gebaeude_Stamm = NULL, Gebaeudename = 'efh-bz2 xxx' WHERE ID = ?", zweite);

            GebaeudeKatalogReparatur.Bericht b = GebaeudeKatalogReparatur.Ausfuehren();

            Assert.Equal(2, b.Benutzt.Count);
            Assert.Contains(b.Benutzt, t => t.StartsWith("Z2-EFH-A-S-test (Projekte " +
                                                         projekt.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
            Assert.Contains(b.Benutzt, t => t.StartsWith("EFH-BZ2 XXX (Projekte", StringComparison.Ordinal));
            Assert.Equal(6, b.Geloescht.Count);
            Assert.DoesNotContain("Z2-EFH-A-S-test", b.Geloescht);
            Assert.DoesNotContain("EFH-BZ2 XXX", b.Geloescht);

            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'Z2-EFH-A-S-test'"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'EFH-BZ2 XXX'"));
            Assert.Equal(idTest, Zahl("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007));
            Assert.Equal(0L, GebaeudeKatalogReparatur.Offen());
        }

        // =====================================================================
        //  4 - Repo-Datei, Werkzeug und Migration
        // =====================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache des Schritts.</b> Migration der Schale, Werkzeug
        /// <c>Testdatenbankschema</c> und Nachzieh-Liste der Tests führen ihn; die Nummer steht
        /// als Zahl allein bei <see cref="GebaeudeKatalogReparatur.SCHRITT"/> und ist der
        /// Zielstand. Die REPO-Datei trägt keinen Befund mehr (nur lesend geöffnet).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            Assert.True(SchemaStand.Zielversion >= GebaeudeKatalogReparatur.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + GebaeudeKatalogReparatur.SCHRITT + ".");

            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("GebaeudeKatalogReparatur.Ausfuehren()", werkzeug);
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_GEBAEUDE_KATALOGREPARATUR = GebaeudeKatalogReparatur.SCHRITT;", migration);
            Assert.Contains("private static bool Schritt_GebaeudeKatalogreparatur(Lauf l)", migration);
            int ort125 = migration.IndexOf("new Schritt(SCHRITT_125_RISIKOMODUL", StringComparison.Ordinal);
            int ortReparatur = migration.IndexOf("new Schritt(SCHRITT_GEBAEUDE_KATALOGREPARATUR", StringComparison.Ordinal);
            Assert.True(ort125 > 0 && ortReparatur > ort125, "Der Schritt steht nicht nach 125 in der Schrittliste.");
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("GebaeudeKatalogReparatur.Ausfuehren()", vorrichtung);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.Equal(275L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Flaeche_Nutzer IS NULL"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE k_Wert_Fenster < 0.1 " +
                                              "AND COALESCE(gesamte_Fensterflaeche, 0) > 0"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner LIKE 'Z2-EFH-A-S%' " +
                                              "OR Bezeichner = 'EFH-BZ2 XXX'"));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>
        /// Legt auf der Arbeitskopie das Schadensbild des Befunds #473 wieder an: Krankenhaus
        /// (U 0,09, Nord 10 000, gesamt 11 645,9), die vier leeren Flächen je Nutzer
        /// (KrankenH-F-U-400 mit 360 Bewohnern) und die acht Testreste als Kopie von
        /// „Z-EFH-A-S-126" bzw. „EFH-BZ2".
        /// </summary>
        private static void SchadensbildAnlegen()
        {
            Sql("UPDATE Tab_Gebaeude_STAMM SET k_Wert_Fenster = 0.09000000357627869, Fensterflaeche_Nord = 10000, " +
                "gesamte_Fensterflaeche = 11645.900024414062 WHERE Bezeichner = ?", KRANKENHAUS);
            Sql("UPDATE Tab_Gebaeude_STAMM SET Flaeche_Nutzer = NULL WHERE Bezeichner IN " +
                "('EFH-BZ2', 'KrankenH-F-U-400', 'KMEH-M-U-54', 'Z-EFH-A-S-126')");
            Sql("UPDATE Tab_Gebaeude_STAMM SET Bewohner = 360 WHERE Bezeichner = 'KrankenH-F-U-400'");

            foreach (string name in GebaeudeKatalogReparatur.Testreste)
            {
                string vorlage = name == "EFH-BZ2 XXX" ? "EFH-BZ2" : "Z-EFH-A-S-126";
                Sql("INSERT INTO Tab_Gebaeude_STAMM (Bezeichner, Wohnflaeche_gesamt, Bewohner, Flaeche_Nutzer, " +
                    "Nutzflaeche, k_Wert_Fenster) SELECT ?, Wohnflaeche_gesamt, Bewohner, NULL, Nutzflaeche, " +
                    "k_Wert_Fenster FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", name, vorlage);
            }
        }

        private static DbParam[] Parameter(object[] werte)
            => werte.Select((w, i) => new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), w)).ToArray();

        private static void Sql(string sql, params object[] werte)
            => Assert.True(DataRepository.ExecuteSQL(sql, Parameter(werte)), "Fehlgeschlagen: " + sql);

        private static object Wert(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == DBNull.Value ? null : o;
        }

        private static long Zahl(string sql, params object[] werte)
        {
            object o = Wert(sql, werte);
            return o == null ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static double Kommazahl(string sql, params object[] werte)
        {
            object o = Wert(sql, werte);
            return o == null ? double.NaN : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string Repowurzel(
            [System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string kandidat = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln"))) return kandidat;
            }
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
