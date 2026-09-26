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
    /// <b>Die dritte Berichtigung der Anschlusslängen im Gebäudekatalog</b> — Schemaschritt
    /// <see cref="GebaeudeAnschlusslaengenDritteReparatur.SCHRITT"/> (Welle #505,
    /// Anwenderentscheid vom 25.09.2026, Konzept Administrationsdialoge 7.1 (a)): Laibungen
    /// 0 m oder leer, gerundete EnEV-Laibungen, Kellerkanten 14,6 m und die Kanten von
    /// „Industrie_ne_81".
    ///
    /// <para><b>Jeder Fall auf seiner eigenen Arbeitskopie.</b> Die Repo-Datei steht schon auf
    /// dem Zielstand; die Fälle legen das Schadensbild deshalb wieder an (Werte des Bestands
    /// nach #496, in seinen Bitmustern) und lassen den Schritt darüber laufen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeAnschlusslaengenDritteReparaturTests
    {
        private const string FW = GebaeudeAnschlusslaengenReparatur.SPALTE_FENSTER_WAND;
        private const string WD = GebaeudeAnschlusslaengenReparatur.SPALTE_WAND_DACH;
        private const string KD = GebaeudeAnschlusslaengenReparatur.SPALTE_AUSSENWAND_KELLER;
        private const string INDUSTRIE = GebaeudeAnschlusslaengenDritteReparatur.INDUSTRIE;

        /// <summary>Die Projektkopie 10614 (Projekt 1007, „EFH-A-TS-212").</summary>
        private const int GEBAEUDE_1007 = 10614;

        // =====================================================================
        //  0 - Die Herleitungen
        // =====================================================================

        /// <summary>
        /// Die neuen Werte folgen aus ihren Herleitungen: Laibung = Verhältnis der Quelle ×
        /// Fensterfläche, Kanten = Umfang aus der eigenen Geometrie. 19 Berichtigungen an
        /// achtzehn Sätzen, drei davon mit dem Bild „leer"; kein Satz und keine Spalte der
        /// Schritte 130 und 141 wird ein zweites Mal angefasst.
        /// </summary>
        [Fact]
        public void Die_neuen_Werte_folgen_aus_ihren_Herleitungen()
        {
            Assert.Equal(1462.1, Math.Round(GebaeudeAnschlusslaengenDritteReparatur.LAIBUNG_JE_M2_VERW_F * 504.0, 1));
            Assert.Equal(238.3, Math.Round(GebaeudeAnschlusslaengenDritteReparatur.LAIBUNG_JE_M2_KMH_G * 99.37, 1));
            Assert.Equal(865.1, Math.Round(GebaeudeAnschlusslaengenDritteReparatur.LAIBUNG_JE_M2_UMKLEIDE * 428.9, 1));
            Assert.Equal(6164.4, Math.Round(GebaeudeAnschlusslaengenReparatur.LAIBUNG_JE_M2_F * 2395.9, 1));
            Assert.Equal(4460.0, GebaeudeAnschlusslaengenDritteReparatur.LAIBUNG_JE_M2_NE * 1784.0);
            Assert.Equal(2875.0, GebaeudeAnschlusslaengenDritteReparatur.LAIBUNG_JE_M2_NE * 1150.0);
            Assert.Equal(391.5, GebaeudeAnschlusslaengenDritteReparatur.LAIBUNG_JE_M2_NE * 156.6, 6);
            Assert.Equal(16000.0, GebaeudeAnschlusslaengenDritteReparatur.LAIBUNG_JE_M2_NE * 6400.0);
            Assert.Equal(GebaeudeAnschlusslaengenDritteReparatur.INDUSTRIE_UMFANG,
                         Math.Round((15100.0 + 6400.0) / (39645.0 / 36587.0 * 8.4), 1));
            // 7 337,4 m sind das 9,6-Fache der Quadratkante; der Umfang liegt darueber.
            double quadratkante = 4.0 * Math.Sqrt(36587.0);
            Assert.InRange(GebaeudeAnschlusslaengenDritteReparatur.INDUSTRIE_KANTE_ALT / quadratkante, 9.5, 9.7);
            Assert.True(GebaeudeAnschlusslaengenDritteReparatur.INDUSTRIE_UMFANG >= quadratkante);

            Anschlusslaengenberichtigung[] liste = GebaeudeAnschlusslaengenDritteReparatur.Berichtigungen;
            Assert.Equal(19, liste.Length);
            Assert.Equal(18, liste.Select(b => b.Bezeichner).Distinct().Count());
            Assert.Equal(3, liste.Count(b => b.Leer));
            Assert.All(liste.Where(b => b.Leer), b => Assert.Equal(FW, b.Spalte));
            foreach (Anschlusslaengenberichtigung b in liste)
            {
                // Das Bild und der neue Wert liegen weit auseinander - ein zweiter Lauf trifft nichts.
                Assert.False(b.Neu > b.BildVon && b.Neu < b.BildBis, b.Bezeichner + " " + b.Spalte);
                Assert.NotNull(b.Leer ? GebaeudeAnschlusslaengenReparatur.SqlBerichtigungLeer(b.Spalte)
                                      : GebaeudeAnschlusslaengenReparatur.SqlBerichtigung(b.Spalte));
                Assert.DoesNotContain(GebaeudeAnschlusslaengenReparatur.Berichtigungen,
                                      a => a.Bezeichner == b.Bezeichner && a.Spalte == b.Spalte);
                Assert.DoesNotContain(GebaeudeAnschlusslaengenFolgereparatur.Berichtigungen,
                                      a => a.Bezeichner == b.Bezeichner && a.Spalte == b.Spalte);
            }
            // Das Bild "leer" gibt es nur fuer die Laibung.
            Assert.Throws<ArgumentException>(() => Anschlusslaengenberichtigung.AusLeer("x", WD, 1.0));
        }

        // =====================================================================
        //  1 - Vorher / nachher je Satz, zweiter Lauf tut nichts
        // =====================================================================

        /// <summary>
        /// Das Schadensbild, wie es vor dem Schritt stand; nach dem Schritt trägt jede Spalte
        /// ihre Berichtigung, alle übrigen Spalten bleiben (auch die leeren oder 0-m-Kanten),
        /// die Werte liegen im Plausibilitätsband, und ein zweiter Lauf findet nichts mehr.
        /// </summary>
        [Fact]
        public void Das_Schadensbild_wird_je_Satz_und_Spalte_berichtigt_und_der_Schritt_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SchadensbildAnlegen();
            Assert.Equal(19L, GebaeudeAnschlusslaengenDritteReparatur.Offen());

            GebaeudeAnschlusslaengenReparatur.Bericht b = GebaeudeAnschlusslaengenDritteReparatur.Ausfuehren();
            Assert.Equal(19, b.Berichtigt.Count);
            Assert.Contains(b.Berichtigt, t => t.Contains("leer -> 391.5", StringComparison.Ordinal));
            Assert.Equal(0L, GebaeudeAnschlusslaengenDritteReparatur.Offen());

            foreach (Anschlusslaengenberichtigung k in GebaeudeAnschlusslaengenDritteReparatur.Berichtigungen)
                Assert.Equal(k.Neu, Spalte(k.Spalte, k.Bezeichner), 6);

            // Laibungen: im Band 0,95 ... 3,0 m je m2 Fenster (0,99 bei Satz 6 wie sein Zwilling 8).
            foreach (string name in GebaeudeAnschlusslaengenDritteReparatur.Berichtigungen
                                    .Where(x => x.Spalte == FW).Select(x => x.Bezeichner))
                Assert.InRange(Spalte(FW, name) / Spalte("gesamte_Fensterflaeche", name), 0.95, 3.0);

            // Kellerkanten: gleich der Dachkante, zwischen Quadratkante und ihrem Dreifachen.
            foreach (string name in GebaeudeAnschlusslaengenDritteReparatur.Berichtigungen
                                    .Where(x => x.Spalte == KD).Select(x => x.Bezeichner))
            {
                Assert.Equal(Spalte(WD, name), Spalte(KD, name), 6);
                double quadratkante = 4.0 * Math.Sqrt(Spalte("Grundflaeche", name));
                Assert.InRange(Spalte(KD, name) / quadratkante, 1.0, 3.2);
            }

            // Die Kanten der Laibungssaetze bleiben, wie sie sind (0 m bzw. leer).
            Assert.Equal(0.0, Spalte(WD, "Hotel_H_BZ"));
            Assert.Equal(0.0, Spalte(KD, "Industriehalle-320"));
            Assert.True(double.IsNaN(Spalte(WD, "Verw_H_75")));
            Assert.True(double.IsNaN(Spalte(KD, "Büro1-F-U-89")));
            // Die Laibung von Industrie_ne_81 traegt (2,5 m/m2) und bleibt.
            Assert.Equal(16000.0, Spalte(FW, INDUSTRIE), 6);
            // Die gerundeten Kanten von gr_Hotel-80-EnEV2016 bleiben.
            Assert.Equal(300.0, Spalte(WD, "gr_Hotel-80-EnEV2016"), 6);
            Assert.Equal(275L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)

            // Zweiter Lauf: nichts mehr zu tun.
            Assert.Empty(GebaeudeAnschlusslaengenDritteReparatur.Ausfuehren().Berichtigt);
        }

        // =====================================================================
        //  2 - Nur das Schadensbild: abweichende Saetze und Projektkopien bleiben
        // =====================================================================

        /// <summary>
        /// Eine Spalte, die schon berichtigt oder ANDERS ist, bleibt unberührt; ein Satz außerhalb
        /// der Liste mit demselben Bild bleibt; ein Auslieferungssatz (<c>ReadOnly</c> = 1) mit
        /// dem Bild wird berichtigt; eine PROJEKTKOPIE mit dem Bild bleibt, wie sie ist.
        /// </summary>
        [Fact]
        public void Ein_abweichender_Wert_ein_fremder_Satz_und_die_Projektkopie_bleiben_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SchadensbildAnlegen();
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 500 WHERE Bezeichner = 'Hotel_H_BZ'");
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 700 WHERE Bezeichner = 'Büro1-F-U-89'");
            Sql("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 1 WHERE Bezeichner = 'Verw_H_75'");
            // Die Zwillinge stehen nicht in der Liste.
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 0 WHERE Bezeichner = 'kl_Hotel-I-080'");
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = NULL WHERE Bezeichner = 'Verw_I_33'");
            Sql("UPDATE Tab_Gebaeude SET Gebaeudename = ?, \"" + WD + "\" = 7337.4, \"" + FW + "\" = NULL " +
                "WHERE ID = ?", INDUSTRIE, GEBAEUDE_1007);

            GebaeudeAnschlusslaengenReparatur.Bericht b = GebaeudeAnschlusslaengenDritteReparatur.Ausfuehren();
            Assert.Equal(17, b.Berichtigt.Count);

            Assert.Equal(500.0, Spalte(FW, "Hotel_H_BZ"));
            Assert.Equal(700.0, Spalte(FW, "Büro1-F-U-89"));
            Assert.Equal(391.5, Spalte(FW, "Verw_H_75"), 6);
            Assert.Equal(1L, Zahl("SELECT ReadOnly FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'Verw_H_75'"));

            // Die fremden Saetze behalten, was sie tragen.
            Assert.Equal(0.0, Spalte(FW, "kl_Hotel-I-080"));
            Assert.True(double.IsNaN(Spalte(FW, "Verw_I_33")));

            // Die Projektkopie traegt ihr Bild weiter - Rechengrundlage ihres Projekts.
            Assert.Equal(7337.4, Kommazahl("SELECT \"" + WD + "\" FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007), 6);
            Assert.True(double.IsNaN(Kommazahl("SELECT \"" + FW + "\" FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007)));

            Assert.Equal(0L, GebaeudeAnschlusslaengenDritteReparatur.Offen());
        }

        // =====================================================================
        //  3 - Repo-Datei, Werkzeug und Migration
        // =====================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache des Schritts.</b> Migration der Schale, Werkzeug
        /// <c>Testdatenbankschema</c> und Nachzieh-Liste der Tests führen ihn; die Nummer steht
        /// als Zahl allein bei <see cref="GebaeudeAnschlusslaengenDritteReparatur.SCHRITT"/>, er
        /// folgt auf die Folgeberichtigung und liegt nicht über dem Zielstand. Die REPO-Datei
        /// trägt die berichtigten Werte (nur lesend geöffnet): keine leere Laibung, keine
        /// Laibung 0 bei Fenstern, keine Kellerkante 14,6 m und keine Kellerkante über dem
        /// Neunfachen der Quadratkante mehr; kein berichtigter Satz hat eine Projektkopie.
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            Assert.True(SchemaStand.Zielversion >= GebaeudeAnschlusslaengenDritteReparatur.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter dem Schritt der dritten Berichtigung.");
            Assert.True(GebaeudeAnschlusslaengenDritteReparatur.SCHRITT > GebaeudeAnschlusslaengenFolgereparatur.SCHRITT);

            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("GebaeudeAnschlusslaengenDritteReparatur.Ausfuehren()", werkzeug);
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_GEBAEUDE_DRITTE_REPARATUR = GebaeudeAnschlusslaengenDritteReparatur.SCHRITT;", migration);
            Assert.Contains("private static bool Schritt_GebaeudeDritteReparatur(Lauf l)", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_GEBAEUDE_FOLGEREPARATUR", StringComparison.Ordinal);
            int ortSchritt = migration.IndexOf("new Schritt(SCHRITT_GEBAEUDE_DRITTE_REPARATUR", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSchritt > ortVorher, "Der Schritt steht nicht nach der Folgeberichtigung in der Schrittliste.");
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("GebaeudeAnschlusslaengenDritteReparatur.Ausfuehren()", vorrichtung);
            string stand = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Update", "SchemaStand.cs"));
            Assert.Contains("<see cref=\"GebaeudeAnschlusslaengenDritteReparatur.SCHRITT\"/>", stand, StringComparison.Ordinal);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.Equal(275L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)
            foreach (Anschlusslaengenberichtigung b in GebaeudeAnschlusslaengenDritteReparatur.Berichtigungen)
            {
                using SqliteCommand cmd = verbindung.CreateCommand();
                cmd.CommandText = "SELECT \"" + b.Spalte + "\" FROM Tab_Gebaeude_STAMM WHERE Bezeichner = $b";
                cmd.Parameters.AddWithValue("$b", b.Bezeichner);
                double wert = Convert.ToDouble(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                Assert.True(Math.Abs(wert - b.Neu) < 1e-6, b.Bezeichner + " " + b.Spalte + ": " +
                            wert.ToString(CultureInfo.InvariantCulture) + " statt " + b.Neu.ToString(CultureInfo.InvariantCulture));
            }
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE \"" + FW + "\" IS NULL"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE \"" + FW + "\" < 0.05 " +
                                              "AND gesamte_Fensterflaeche > 0"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE abs(\"" + KD + "\" - 14.6) < 0.05"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE \"" + KD + "\" * \"" + KD +
                                              "\" > 1296.0 * Grundflaeche"));
            // Die Referenzsaetze (Einfrierregel) stehen in keiner Berichtigung: Keiner der
            // achtzehn Saetze hat ueberhaupt eine Projektkopie - weder ueber ID_Gebaeude_Stamm
            // noch ueber den Namen.
            foreach (string name in GebaeudeAnschlusslaengenDritteReparatur.Berichtigungen.Select(x => x.Bezeichner).Distinct())
            {
                using SqliteCommand cmd = verbindung.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Tab_Gebaeude g LEFT JOIN Tab_Gebaeude_STAMM s " +
                                  "ON s.ID = g.ID_Gebaeude_Stamm WHERE s.Bezeichner = $b OR g.Gebaeudename = $b";
                cmd.Parameters.AddWithValue("$b", name);
                Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) == 0,
                            name + " hat eine Projektkopie.");
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>
        /// Legt auf der Arbeitskopie das Schadensbild wieder an — in den Bitmustern des Bestands
        /// (einfache Genauigkeit bei der Kellerkante von 84 und 85).
        /// </summary>
        private static void SchadensbildAnlegen()
        {
            foreach ((string name, double _) in GebaeudeAnschlusslaengenDritteReparatur.LaibungNull)
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 0 WHERE Bezeichner = ?", name);
            foreach ((string name, double _) in GebaeudeAnschlusslaengenDritteReparatur.LaibungLeer)
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = NULL WHERE Bezeichner = ?", name);
            foreach ((string name, double bild, double _) in GebaeudeAnschlusslaengenDritteReparatur.LaibungGerundet)
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = ? WHERE Bezeichner = ?", bild, name);
            foreach ((string name, double _) in GebaeudeAnschlusslaengenDritteReparatur.Kellerkanten)
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + KD + "\" = ? WHERE Bezeichner = ?",
                    name.StartsWith("GMH-BZ", StringComparison.Ordinal) || name == "GMH-J-015" ? 14.600000381469727 : 14.6, name);
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + WD + "\" = 7337.4, \"" + KD + "\" = 7337.4 WHERE Bezeichner = ?", INDUSTRIE);
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
