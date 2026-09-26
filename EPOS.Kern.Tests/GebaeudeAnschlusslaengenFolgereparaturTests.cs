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
    /// <b>Die Folgeberichtigung im Gebäudekatalog</b> — Schemaschritt
    /// <see cref="GebaeudeAnschlusslaengenFolgereparatur.SCHRITT"/> (Welle #496, Konzept
    /// Administrationsdialoge 7.1 (a)): die Scan-Kandidaten mit vertauschter Laibung und
    /// Dachkante, die Dach- und Kellerkante der „Hotel-F-228"-Sätze und die Außenwand des
    /// Kaufhauses.
    ///
    /// <para><b>Jeder Fall auf seiner eigenen Arbeitskopie.</b> Die Repo-Datei steht schon auf
    /// dem Zielstand; die Fälle legen das Schadensbild deshalb wieder an (Werte des Scans nach
    /// #493, in den Bitmustern des Bestands) und lassen den Schritt darüber laufen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeAnschlusslaengenFolgereparaturTests
    {
        private const string FW = GebaeudeAnschlusslaengenReparatur.SPALTE_FENSTER_WAND;
        private const string WD = GebaeudeAnschlusslaengenReparatur.SPALTE_WAND_DACH;
        private const string KD = GebaeudeAnschlusslaengenReparatur.SPALTE_AUSSENWAND_KELLER;
        private const string AW = GebaeudeAnschlusslaengenReparatur.SPALTE_FLAECHE_AUSSENWAND;
        private const string KAUFHAUS = GebaeudeAnschlusslaengenFolgereparatur.KAUFHAUS;
        private const string HOTEL = GebaeudeAnschlusslaengenFolgereparatur.HOTEL_F_228;

        /// <summary>Die Projektkopie 10614 (Projekt 1007, „EFH-A-TS-212").</summary>
        private const int GEBAEUDE_1007 = 10614;

        // =====================================================================
        //  0 - Die Herleitungen
        // =====================================================================

        /// <summary>
        /// Die neuen Werte folgen aus ihren Herleitungen: Laibung von 84, 85 = Verhältnis der
        /// G-096-Sätze nach dem Tausch × Fensterfläche, Dachkante = Umfang aus der eigenen
        /// Geometrie, Außenwand des Kaufhauses = Hüllfläche − Fenster; beim Tausch ist der neue
        /// Wert jeder Spalte das Bild der anderen. 39 Berichtigungen an zwanzig Sätzen.
        /// </summary>
        [Fact]
        public void Die_neuen_Werte_folgen_aus_ihren_Herleitungen()
        {
            Assert.Equal(GebaeudeAnschlusslaengenFolgereparatur.GMH_BZ_FENSTER_WAND,
                         Math.Round(GebaeudeAnschlusslaengenFolgereparatur.LAIBUNG_JE_M2_G096 * 307.6, 1));
            Assert.Equal(GebaeudeAnschlusslaengenFolgereparatur.GMH_BZ_UMFANG,
                         Math.Round((633.0 + 307.6) / (1430.0 / 485.2 * 2.61), 1));
            Assert.Equal(GebaeudeAnschlusslaengenFolgereparatur.KAUFHAUS_AUSSENWAND,
                         Math.Round(GebaeudeAnschlusslaengenReparatur.UMFANG * 4201.0 / 1468.97 * 4.55 - 2262.36, 1));
            // Der Tausch traegt bei den Heimen: Umfang aus der Geometrie 187,7 m neben 185 m.
            Assert.InRange((2132.0 + 545.0) / (3020.0 / 540.0 * 2.55) / GebaeudeAnschlusslaengenFolgereparatur.HEIME_KURZ,
                           1.0, 1.02);
            // ... und bei den G-096-Saetzen: 87,7 m neben 86,6 m.
            Assert.InRange((433.0 + 237.6) / (1263.0 / 431.2 * 2.61) / GebaeudeAnschlusslaengenFolgereparatur.G096_KURZ,
                           1.0, 1.02);

            Anschlusslaengenberichtigung[] liste = GebaeudeAnschlusslaengenFolgereparatur.Berichtigungen;
            Assert.Equal(39, liste.Length);
            Assert.Equal(20, liste.Select(b => b.Bezeichner).Distinct().Count());
            Assert.Single(liste, b => b.Spalte == AW);
            foreach (Anschlusslaengenberichtigung b in liste)
            {
                // Das Bild und der neue Wert liegen weit auseinander - ein zweiter Lauf trifft nichts.
                Assert.False(b.Neu > b.BildVon && b.Neu < b.BildBis, b.Bezeichner + " " + b.Spalte);
                Assert.NotNull(GebaeudeAnschlusslaengenReparatur.SqlBerichtigung(b.Spalte));
                // Kein Satz der #493 wird ein zweites Mal angefasst.
                Assert.DoesNotContain(GebaeudeAnschlusslaengenReparatur.Berichtigungen,
                                      a => a.Bezeichner == b.Bezeichner && a.Spalte == b.Spalte);
            }
        }

        // =====================================================================
        //  1 - Vorher / nachher je Satz, zweiter Lauf tut nichts
        // =====================================================================

        /// <summary>
        /// Das Schadensbild des Scans, wie es vor dem Schritt stand; nach dem Schritt trägt jede
        /// Spalte ihre Berichtigung, alle übrigen Spalten bleiben, die Werte sind plausibel, und
        /// ein zweiter Lauf findet nichts mehr.
        /// </summary>
        [Fact]
        public void Das_Schadensbild_wird_je_Satz_und_Spalte_berichtigt_und_der_Schritt_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SchadensbildAnlegen();
            Assert.Equal(39L, GebaeudeAnschlusslaengenFolgereparatur.Offen());
            double kellerHeim = Spalte(KD, "AltenH-C-S-108");
            double kellerG096 = Spalte(KD, "Hotel_G_96");

            GebaeudeAnschlusslaengenReparatur.Bericht b = GebaeudeAnschlusslaengenFolgereparatur.Ausfuehren();
            Assert.Equal(39, b.Berichtigt.Count);
            Assert.Equal(0L, GebaeudeAnschlusslaengenFolgereparatur.Offen());

            // Heime, Schulen, Hallenbaeder: getauscht; die Kellerkante bleibt.
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.HeimeUndSchulen
                                    .Concat(GebaeudeAnschlusslaengenFolgereparatur.Hallenbaeder))
            {
                Assert.Equal(985.0, Spalte(FW, name), 6);
                Assert.Equal(185.0, Spalte(WD, name), 6);
                Assert.Equal(kellerHeim, Spalte(KD, name), 6);
            }

            // Hotel-F-228: Dach- und Kellerkante = Umfang von Kaufhalle_NE; die Laibung bleibt.
            foreach (string name in new[] { HOTEL }.Concat(GebaeudeAnschlusslaengenFolgereparatur.HotelF228Abwandlungen))
            {
                Assert.Equal(116.16, Spalte(WD, name), 6);
                Assert.Equal(116.16, Spalte(KD, name), 6);
                Assert.Equal(515.2, Spalte(FW, name), 3);
                Assert.Equal(Spalte(WD, "Kaufhalle_NE"), Spalte(WD, name), 6);
            }

            // G-096: getauscht; 84, 85: hergeleitet; die Kellerkante bleibt.
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.HotelG096)
            {
                Assert.Equal(295.5, Spalte(FW, name), 6);
                Assert.Equal(86.6, Spalte(WD, name), 6);
                Assert.Equal(kellerG096, Spalte(KD, name), 6);
            }
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.GmhBz)
            {
                Assert.Equal(382.6, Spalte(FW, name), 6);
                Assert.Equal(122.3, Spalte(WD, name), 6);
            }

            // Kaufhaus: Aussenwand 1 820,9 m2; die Laengen aus #493 bleiben.
            Assert.Equal(1820.9, Spalte(AW, KAUFHAUS), 6);
            Assert.Equal(5820.8, Spalte(FW, KAUFHAUS), 6);
            Assert.Equal(313.8, Spalte(WD, KAUFHAUS), 6);

            // Plausibel: Laibung je m2 Fenster im Band des Katalogs, Dachkante nicht unter der
            // Quadratkante der Grundflaeche und nicht ueber dem Dreifachen.
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.Berichtigungen
                                    .Where(x => x.Spalte != AW).Select(x => x.Bezeichner).Distinct())
            {
                double je = Spalte(FW, name) / Spalte("gesamte_Fensterflaeche", name);
                Assert.InRange(je, 1.2, 3.0);
                double quadratkante = 4.0 * Math.Sqrt(Spalte("Grundflaeche", name));
                Assert.InRange(Spalte(WD, name) / quadratkante, 1.0, 3.0);
            }
            Assert.Equal(275L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)

            // Zweiter Lauf: nichts mehr zu tun.
            Assert.Empty(GebaeudeAnschlusslaengenFolgereparatur.Ausfuehren().Berichtigt);
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
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 500 WHERE Bezeichner = 'AltenH-C-S-108'");
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + AW + "\" = 9000 WHERE Bezeichner = ?", KAUFHAUS);
            Sql("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 1 WHERE Bezeichner = 'Schule-NE1'");
            // Die EnEV-Abwandlung derselben Geometrie steht nicht in der Liste.
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 185, \"" + WD + "\" = 985 " +
                "WHERE Bezeichner = 'AltenH-C-S-110-EnEV2016'");
            Sql("UPDATE Tab_Gebaeude SET Gebaeudename = ?, \"" + WD + "\" = 5380.75, \"" + KD + "\" = 40 " +
                "WHERE ID = ?", HOTEL, GEBAEUDE_1007);

            GebaeudeAnschlusslaengenReparatur.Bericht b = GebaeudeAnschlusslaengenFolgereparatur.Ausfuehren();
            Assert.Equal(37, b.Berichtigt.Count);

            Assert.Equal(500.0, Spalte(FW, "AltenH-C-S-108"));
            Assert.Equal(185.0, Spalte(WD, "AltenH-C-S-108"), 6);
            Assert.Equal(9000.0, Spalte(AW, KAUFHAUS));
            Assert.Equal(985.0, Spalte(FW, "Schule-NE1"), 6);
            Assert.Equal(1L, Zahl("SELECT ReadOnly FROM Tab_Gebaeude_STAMM WHERE Bezeichner = 'Schule-NE1'"));

            // Der fremde Satz behaelt, was er traegt.
            Assert.Equal(185.0, Spalte(FW, "AltenH-C-S-110-EnEV2016"), 6);
            Assert.Equal(985.0, Spalte(WD, "AltenH-C-S-110-EnEV2016"), 6);

            // Die Projektkopie traegt ihr Bild weiter - Rechengrundlage ihres Projekts.
            Assert.Equal(5380.75, Kommazahl("SELECT \"" + WD + "\" FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007), 6);
            Assert.Equal(40.0, Kommazahl("SELECT \"" + KD + "\" FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007), 6);

            Assert.Equal(0L, GebaeudeAnschlusslaengenFolgereparatur.Offen());
        }

        // =====================================================================
        //  3 - Repo-Datei, Werkzeug und Migration
        // =====================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache des Schritts.</b> Migration der Schale, Werkzeug
        /// <c>Testdatenbankschema</c> und Nachzieh-Liste der Tests führen ihn; die Nummer steht
        /// als Zahl allein bei <see cref="GebaeudeAnschlusslaengenFolgereparatur.SCHRITT"/>, er
        /// folgt auf die Messreihen (Z5) und liegt nicht über dem Zielstand. Die REPO-Datei trägt
        /// kein Bild mehr und die berichtigten Werte (nur lesend geöffnet); kein Satz führt mehr
        /// die vertauschten Paare.
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            Assert.True(SchemaStand.Zielversion >= GebaeudeAnschlusslaengenFolgereparatur.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter dem Schritt der Folgeberichtigung.");
            Assert.True(GebaeudeAnschlusslaengenFolgereparatur.SCHRITT > TwwSchema.SCHRITT_T4_MESSREIHEN);

            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("GebaeudeAnschlusslaengenFolgereparatur.Ausfuehren()", werkzeug);
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_GEBAEUDE_FOLGEREPARATUR = GebaeudeAnschlusslaengenFolgereparatur.SCHRITT;", migration);
            Assert.Contains("private static bool Schritt_GebaeudeFolgereparatur(Lauf l)", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_140_ZAPFPROFIL_MESSREIHEN", StringComparison.Ordinal);
            int ortSchritt = migration.IndexOf("new Schritt(SCHRITT_GEBAEUDE_FOLGEREPARATUR", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSchritt > ortVorher, "Der Schritt steht nicht nach den Messreihen in der Schrittliste.");
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("GebaeudeAnschlusslaengenFolgereparatur.Ausfuehren()", vorrichtung);
            string stand = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Update", "SchemaStand.cs"));
            Assert.Contains("<see cref=\"GebaeudeAnschlusslaengenFolgereparatur.SCHRITT\"/>", stand, StringComparison.Ordinal);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.Equal(275L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));   // 269 + sechs Katalogsätze M/A (E51)
            foreach (Anschlusslaengenberichtigung b in GebaeudeAnschlusslaengenFolgereparatur.Berichtigungen)
            {
                using SqliteCommand cmd = verbindung.CreateCommand();
                cmd.CommandText = "SELECT \"" + b.Spalte + "\" FROM Tab_Gebaeude_STAMM WHERE Bezeichner = $b";
                cmd.Parameters.AddWithValue("$b", b.Bezeichner);
                double wert = Convert.ToDouble(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                Assert.True(Math.Abs(wert - b.Neu) < 1e-6, b.Bezeichner + " " + b.Spalte + ": " +
                            wert.ToString(CultureInfo.InvariantCulture) + " statt " + b.Neu.ToString(CultureInfo.InvariantCulture));
            }
            // Kein Satz traegt mehr die vertauschten Paare oder eine Dachkante ueber dem
            // Zwanzigfachen der Quadratkante seines Dachs (L^2 > 400 x 16 x Dachflaeche).
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE abs(\"" + FW + "\" - 185) < 0.05 " +
                                              "AND abs(\"" + WD + "\" - 985) < 0.05"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE abs(\"" + FW + "\" - 86.6) < 0.05 " +
                                              "AND abs(\"" + WD + "\" - 295.5) < 0.05"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE \"" + WD + "\" * \"" + WD +
                                              "\" > 6400.0 * Dachflaeche"));
            // Die Referenzsaetze (Einfrierregel) stehen in keiner Berichtigung.
            // (Keiner der zwanzig Saetze hat ueberhaupt eine Projektkopie - weder ueber
            // ID_Gebaeude_Stamm noch ueber den Namen.)
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.Berichtigungen.Select(x => x.Bezeichner).Distinct())
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
        /// Legt auf der Arbeitskopie das Schadensbild des Scans wieder an — in den Bitmustern
        /// des Bestands (einfache Genauigkeit bei 69, 71, 84 und 85; 5 380,75 bei 54; 10 093,99
        /// beim Kaufhaus).
        /// </summary>
        private static void SchadensbildAnlegen()
        {
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.HeimeUndSchulen
                                    .Concat(GebaeudeAnschlusslaengenFolgereparatur.Hallenbaeder))
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 185, \"" + WD + "\" = 985 WHERE Bezeichner = ?", name);
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + WD + "\" = 5380.75, \"" + KD + "\" = 40 WHERE Bezeichner = ?", HOTEL);
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.HotelF228Abwandlungen)
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + WD + "\" = 5380.7998046875, \"" + KD + "\" = 40 WHERE Bezeichner = ?", name);
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.HotelG096)
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 86.6, \"" + WD + "\" = 295.5 WHERE Bezeichner = ?", name);
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.GmhBz)
                Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + FW + "\" = 86.5999984741211, \"" + WD + "\" = 295.5 WHERE Bezeichner = ?", name);
            Sql("UPDATE Tab_Gebaeude_STAMM SET \"" + AW + "\" = 10093.99 WHERE Bezeichner = ?", KAUFHAUS);
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
