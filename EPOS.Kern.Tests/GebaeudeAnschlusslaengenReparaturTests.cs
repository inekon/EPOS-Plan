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
    /// <b>Die Anschlusslängen im Gebäudekatalog</b> — Schemaschritt
    /// <see cref="GebaeudeAnschlusslaengenReparatur.SCHRITT"/> (Welle #493, Konzept
    /// Administrationsdialoge 7.1 (a)): der Krankenhaussatz (Fenster–Wand 1 800 m, Außenwand
    /// 12 094 m²) und die sechs Sätze mit 243,7 / 7 879 / 1 392,8 m.
    ///
    /// <para><b>Jeder Fall auf seiner eigenen Arbeitskopie.</b> Die Repo-Datei steht schon auf
    /// dem Zielstand; die Fälle legen das Schadensbild deshalb wieder an (Werte des Befunds
    /// #491, in der einfachen Genauigkeit des Bestands) und lassen den Schritt darüber laufen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeAnschlusslaengenReparaturTests
    {
        private const string FW = GebaeudeAnschlusslaengenReparatur.SPALTE_FENSTER_WAND;
        private const string WD = GebaeudeAnschlusslaengenReparatur.SPALTE_WAND_DACH;
        private const string KD = GebaeudeAnschlusslaengenReparatur.SPALTE_AUSSENWAND_KELLER;
        private const string AW = GebaeudeAnschlusslaengenReparatur.SPALTE_FLAECHE_AUSSENWAND;
        private const string KRANKENHAUS = GebaeudeAnschlusslaengenReparatur.KRANKENHAUS;
        private const string KAUFHAUS = GebaeudeAnschlusslaengenReparatur.KAUFHAUS;

        /// <summary>Die Projektkopie 10614 (Projekt 1007, „EFH-A-TS-212").</summary>
        private const int GEBAEUDE_1007 = 10614;

        // =====================================================================
        //  0 - Die Herleitungen
        // =====================================================================

        /// <summary>
        /// Die neuen Werte folgen aus ihren Herleitungen: Laibung = Verhältnis des
        /// Ausgangssatzes × Fensterfläche, Außenwand = Hüllfläche − Fensterfläche; zwanzig
        /// Berichtigungen an sieben Sätzen.
        /// </summary>
        [Fact]
        public void Die_neuen_Werte_folgen_aus_ihren_Herleitungen()
        {
            Assert.Equal(GebaeudeAnschlusslaengenReparatur.KRANKENHAUS_FENSTER_WAND,
                         Math.Round(GebaeudeAnschlusslaengenReparatur.LAIBUNG_JE_M2_78 * 1895.9, 1));
            Assert.Equal(GebaeudeAnschlusslaengenReparatur.KRANKENHAUS_AUSSENWAND,
                         Math.Round(12094.0 + 3016.3 - 1895.9, 1));
            Assert.Equal(GebaeudeAnschlusslaengenReparatur.KAUFHAUS_FENSTER_WAND,
                         Math.Round(GebaeudeAnschlusslaengenReparatur.LAIBUNG_JE_M2_F * 2262.36, 1));
            // Der Tausch traegt fuer die Laibung der F-Quelle: 2,573 m/m2 gegen 2,538 beim Ausgangssatz.
            Assert.InRange(GebaeudeAnschlusslaengenReparatur.LAIBUNG_JE_M2_F /
                           GebaeudeAnschlusslaengenReparatur.LAIBUNG_JE_M2_78, 1.0, 1.02);

            Assert.Equal(20, GebaeudeAnschlusslaengenReparatur.Berichtigungen.Length);
            Assert.Equal(7, GebaeudeAnschlusslaengenReparatur.Berichtigungen.Select(b => b.Bezeichner).Distinct().Count());
            foreach (Anschlusslaengenberichtigung b in GebaeudeAnschlusslaengenReparatur.Berichtigungen)
            {
                // Das Bild und der neue Wert liegen weit auseinander - ein zweiter Lauf trifft nichts.
                Assert.False(b.Neu > b.BildVon && b.Neu < b.BildBis, b.Bezeichner + " " + b.Spalte);
                Assert.NotNull(GebaeudeAnschlusslaengenReparatur.SqlBerichtigung(b.Spalte));
            }
        }

        // =====================================================================
        //  1 - Vorher / nachher je Satz, zweiter Lauf tut nichts
        // =====================================================================

        /// <summary>
        /// Das Schadensbild des Befunds, wie es vor dem Schritt stand; nach dem Schritt trägt
        /// jede Spalte ihre Berichtigung, alle übrigen Spalten bleiben, und ein zweiter Lauf
        /// findet nichts mehr.
        /// </summary>
        [Fact]
        public void Das_Schadensbild_wird_je_Satz_und_Spalte_berichtigt_und_der_Schritt_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SchadensbildAnlegen();
            Assert.Equal(20L, GebaeudeAnschlusslaengenReparatur.Offen());
            double fensterVorher = Kommazahl("SELECT gesamte_Fensterflaeche FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS);

            GebaeudeAnschlusslaengenReparatur.Bericht b = GebaeudeAnschlusslaengenReparatur.Ausfuehren();
            Assert.Equal(20, b.Berichtigt.Count);
            Assert.Equal(0L, GebaeudeAnschlusslaengenReparatur.Offen());

            // Krankenhaus: Laibung 4 812, Aussenwand 13 214,4; Dach- und Kellerkante und Fenster bleiben.
            Assert.Equal(4812.0, Spalte(FW, KRANKENHAUS), 6);
            Assert.Equal(13214.4, Spalte(AW, KRANKENHAUS), 6);
            Assert.Equal(313.8, Spalte(WD, KRANKENHAUS), 3);
            Assert.Equal(313.8, Spalte(KD, KRANKENHAUS), 3);
            Assert.Equal(fensterVorher, Kommazahl("SELECT gesamte_Fensterflaeche FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", KRANKENHAUS));

            // F-Quelle: Laibung 7 879 (Tausch), Dach- und Kellerkante = Umfang 313,8; Aussenwand bleibt.
            foreach (string name in GebaeudeAnschlusslaengenReparatur.FQuelle)
            {
                Assert.Equal(7879.0, Spalte(FW, name), 6);
                Assert.Equal(313.8, Spalte(WD, name), 6);
                Assert.Equal(313.8, Spalte(KD, name), 6);
                Assert.Equal(10094.0, Spalte(AW, name), 6);
            }

            // Kaufhaus: Laibung nach dem Verhaeltnis der F-Quelle, Kanten = Umfang.
            Assert.Equal(5820.8, Spalte(FW, KAUFHAUS), 6);
            Assert.Equal(313.8, Spalte(WD, KAUFHAUS), 6);
            Assert.Equal(313.8, Spalte(KD, KAUFHAUS), 6);

            // Plausibel: Laibung je m2 Fenster zwischen 2 und 3 m, Kanten gleich.
            foreach (string name in GebaeudeAnschlusslaengenReparatur.Berichtigungen.Select(x => x.Bezeichner).Distinct())
            {
                double je = Spalte(FW, name) / Kommazahl("SELECT gesamte_Fensterflaeche FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", name);
                Assert.InRange(je, 2.0, 3.0);
                Assert.Equal(Spalte(WD, name), Spalte(KD, name), 6);
            }
            Assert.Equal(275L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)

            // Zweiter Lauf: nichts mehr zu tun.
            Assert.Empty(GebaeudeAnschlusslaengenReparatur.Ausfuehren().Berichtigt);
        }

        // =====================================================================
        //  2 - Nur das Schadensbild: abweichende Saetze und Projektkopien bleiben
        // =====================================================================

        /// <summary>
        /// Eine Spalte, die schon berichtigt oder ANDERS ist, bleibt unberührt (die übrigen
        /// Spalten desselben Satzes werden berichtigt); ein Satz außerhalb der Liste mit
        /// denselben Zahlen bleibt; ein Auslieferungssatz (<c>ReadOnly</c> = 1) mit dem Bild wird
        /// berichtigt; eine PROJEKTKOPIE mit dem Bild bleibt, wie sie ist.
        /// </summary>
        [Fact]
        public void Ein_abweichender_Wert_ein_fremder_Satz_und_die_Projektkopie_bleiben_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SchadensbildAnlegen();
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 500 WHERE Bezeichner = 'KrankenH-F-S-136'");
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + AW + "\" = 12000 WHERE Bezeichner = ?", KRANKENHAUS);
            Sql("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 1 WHERE Bezeichner = 'KrankenH-F-U-400'");
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 243.7, \"" + WD + "\" = 7879, \"" + KD + "\" = 1392.8 " +
                "WHERE Bezeichner = 'KrankenH_NE'");
            Sql("UPDATE Tab_Gebaeude SET Gebaeudename = 'KrankenH-F-S-136', \"" + FW + "\" = 243.7, \"" + WD + "\" = 7879 " +
                "WHERE ID = ?", GEBAEUDE_1007);

            GebaeudeAnschlusslaengenReparatur.Bericht b = GebaeudeAnschlusslaengenReparatur.Ausfuehren();
            Assert.Equal(18, b.Berichtigt.Count);

            Assert.Equal(500.0, Spalte(FW, "KrankenH-F-S-136"));
            Assert.Equal(313.8, Spalte(WD, "KrankenH-F-S-136"), 6);
            Assert.Equal(12000.0, Spalte(AW, KRANKENHAUS));
            Assert.Equal(4812.0, Spalte(FW, KRANKENHAUS), 6);
            Assert.Equal(7879.0, Spalte(FW, "KrankenH-F-U-400"), 6);
            Assert.Equal(1L, Zahl("SELECT ReadOnly FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'KrankenH-F-U-400'"));

            // Der Ausgangssatz steht nicht in der Liste - er behaelt, was er traegt.
            Assert.Equal(243.7, Spalte(FW, "KrankenH_NE"), 6);
            Assert.Equal(7879.0, Spalte(WD, "KrankenH_NE"), 6);

            // Die Projektkopie traegt ihr Bild weiter - Rechengrundlage ihres Projekts.
            Assert.Equal(243.7, Kommazahl("SELECT \"" + FW + "\" FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007), 6);
            Assert.Equal(7879.0, Kommazahl("SELECT \"" + WD + "\" FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007), 6);

            Assert.Equal(0L, GebaeudeAnschlusslaengenReparatur.Offen());
        }

        // =====================================================================
        //  3 - Repo-Datei, Werkzeug und Migration
        // =====================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache des Schritts.</b> Migration der Schale, Werkzeug
        /// <c>Testdatenbankschema</c> und Nachzieh-Liste der Tests führen ihn; die Nummer steht
        /// als Zahl allein bei <see cref="GebaeudeAnschlusslaengenReparatur.SCHRITT"/> und liegt
        /// nicht über dem Zielstand — der ist inzwischen weitergezogen (Schritt 131, die
        /// eingespielten Typtage der Stufe Z4b). Die REPO-Datei trägt kein Bild mehr und die
        /// berichtigten Werte (nur lesend geöffnet).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            Assert.True(SchemaStand.Zielversion >= GebaeudeAnschlusslaengenReparatur.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter dem Schritt der Anschlusslaengen.");
            Assert.True(GebaeudeAnschlusslaengenReparatur.SCHRITT > WiederholperiodeSchema.SCHRITT);

            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("GebaeudeAnschlusslaengenReparatur.Ausfuehren()", werkzeug);
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN = GebaeudeAnschlusslaengenReparatur.SCHRITT;", migration);
            Assert.Contains("private static bool Schritt_GebaeudeAnschlusslaengen(Lauf l)", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_WIEDERHOLPERIODE", StringComparison.Ordinal);
            int ortSchritt = migration.IndexOf("new Schritt(SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSchritt > ortVorher, "Der Schritt steht nicht nach der Wiederholperiode in der Schrittliste.");
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("GebaeudeAnschlusslaengenReparatur.Ausfuehren()", vorrichtung);
            string stand = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Update", "SchemaStand.cs"));
            // Der Zielstand ist spaeter weitergezogen (die eingespielten Typtage, Stufe Z4b);
            // SchemaStand nennt den Schritt weiter in seiner Chronik - Muster der E16-Wache.
            Assert.Contains("<see cref=\"GebaeudeAnschlusslaengenReparatur.SCHRITT\"/>", stand, StringComparison.Ordinal);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.Equal(275L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)
            foreach (Anschlusslaengenberichtigung b in GebaeudeAnschlusslaengenReparatur.Berichtigungen)
            {
                using SqliteCommand cmd = verbindung.CreateCommand();
                cmd.CommandText = "SELECT \"" + b.Spalte + "\" FROM Tab_Gebaeude_STAMM WHERE Bezeichner = $b";
                cmd.Parameters.AddWithValue("$b", b.Bezeichner);
                double wert = Convert.ToDouble(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                Assert.True(Math.Abs(wert - b.Neu) < 1e-6, b.Bezeichner + " " + b.Spalte + ": " +
                            wert.ToString(CultureInfo.InvariantCulture) + " statt " + b.Neu.ToString(CultureInfo.InvariantCulture));
            }
            // Keine Kopie derselben Quelle blieb zurueck: kein Satz traegt mehr 243,7 / 7 879 / 1 392,8.
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE abs(\"" + FW + "\" - 243.7) < 0.05 " +
                                              "AND abs(\"" + WD + "\" - 7879) < 0.05"));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>
        /// Legt auf der Arbeitskopie das Schadensbild des Befunds #491 wieder an — in den
        /// Bitmustern des Bestands (einfache Genauigkeit bei 80-83 und 37, 1 392,78 beim
        /// Kaufhaus).
        /// </summary>
        private static void SchadensbildAnlegen()
        {
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 1800, \"" + AW + "\" = 12094 WHERE Bezeichner = ?", KRANKENHAUS);
            foreach (string name in GebaeudeAnschlusslaengenReparatur.FQuelle)
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 243.6999969482422, \"" + WD + "\" = 7879, \"" +
                    KD + "\" = 1392.800048828125 WHERE Bezeichner = ?", name);
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 243.7, \"" + WD + "\" = 7879, \"" + KD + "\" = 1392.78 " +
                "WHERE Bezeichner = ?", KAUFHAUS);
        }

        private static double Spalte(string spalte, string bezeichner)
            => Kommazahl("SELECT \"" + spalte + "\" FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", bezeichner);

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
