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
    /// <b>Die Quelle der Herstellerzeilen 1041 und 1066 nennt die Herkunft der Rohdichte</b> —
    /// Schemaschritt <see cref="BaustoffQuellenBerichtigung.SCHRITT"/> (Gebäudesimulation G3,
    /// Regel aus Entscheid E39, Nachweis zu N1.44): Stammt die Rohdichte einer Herstellerzeile aus
    /// einer Umweltproduktdeklaration, dann nennt die Quelle das.
    ///
    /// <para><b>Jeder Fall auf seiner eigenen Arbeitskopie.</b> Die Repo-Datei steht schon auf
    /// dem Zielstand; die Fälle legen den alten Saattext deshalb wieder an — im Katalog und über
    /// den echten Kopierweg (<see cref="BaustoffCtrl.CopyFromStamm(int, int)"/>) in einem
    /// Projekt — und lassen den Schritt darüber laufen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BaustoffQuellenBerichtigungTests
    {
        /// <summary>Ein Referenzprojekt der Testdatenbank als Träger der Projektkopien.</summary>
        private const int PROJEKT = 1007;

        // =====================================================================
        //  0 - Die Texte
        // =====================================================================

        /// <summary>
        /// Die neuen Texte stehen in der Saat, nennen die Herkunft der Rohdichte, beginnen mit
        /// dem alten Text und halten die Höchstlänge der Spalte; die Werte der Zeilen bleiben.
        /// </summary>
        [Fact]
        public void Die_Saat_nennt_die_Herkunft_der_Rohdichte()
        {
            Assert.Equal(new[] { 1041, 1066 }, BaustoffQuellenBerichtigung.Berichtigungen.Select(z => z.Id));
            foreach (BaustoffQuellenBerichtigung.Zeile z in BaustoffQuellenBerichtigung.Berichtigungen)
            {
                BaustoffSaat s = BaustoffSchema.SaatZu(z.Id);
                Assert.Equal(s.Quelle, z.Neu);
                Assert.Equal(s.Hersteller, z.Hersteller);
                Assert.Equal(s.Bezeichner, z.Bezeichner);
                Assert.NotEqual(z.Alt, z.Neu);
                Assert.StartsWith(z.Alt + "; Rohdichte ", z.Neu, StringComparison.Ordinal);
                Assert.True(z.Neu.Length <= BaustoffSchema.LAENGE_QUELLE, z.Neu);
            }
            Assert.EndsWith("; Rohdichte aus FDES 120 mm", BaustoffSchema.SaatZu(1041).Quelle, StringComparison.Ordinal);
            Assert.EndsWith("; Rohdichte Mindestwert A2-s1,d0 nach VDPM-EPD", BaustoffSchema.SaatZu(1066).Quelle,
                            StringComparison.Ordinal);
            Assert.Equal(35.0, BaustoffSchema.SaatZu(1041).Rho);
            Assert.Equal(230.0, BaustoffSchema.SaatZu(1066).Rho);
        }

        // =====================================================================
        //  1 - Der Schritt berichtigt Katalog und Projektkopie, und er ist wiederholbar
        // =====================================================================

        /// <summary>
        /// Mit dem alten Text im Katalog und in den Projektkopien berichtigt der Schritt beide
        /// Katalogzeilen und beide Kopien; Rohdichte und übrige Spalten bleiben, und ein zweiter
        /// Lauf findet nichts mehr.
        /// </summary>
        [Fact]
        public void Der_Schritt_berichtigt_beide_Zeilen_und_ihre_Projektkopien_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            AltenTextAnlegen();
            var ctrl = new BaustoffCtrl();
            int kopie1041 = ctrl.CopyFromStamm(1041, PROJEKT);
            int kopie1066 = ctrl.CopyFromStamm(1066, PROJEKT);
            Assert.True(kopie1041 > 0 && kopie1066 > 0);
            Assert.Equal(BaustoffQuellenBerichtigung.ALT_1041, Text("SELECT Quelle FROM Tab_Baustoff WHERE ID = ?", kopie1041));
            Assert.Equal(4L, BaustoffQuellenBerichtigung.Offen());

            BaustoffQuellenBerichtigung.Bericht b = BaustoffQuellenBerichtigung.Ausfuehren();
            Assert.Equal(2, b.Katalog);
            Assert.Equal(2, b.Kopien);
            Assert.Equal(4, b.Berichtigt.Count);
            Assert.Equal(0L, BaustoffQuellenBerichtigung.Offen());

            foreach (BaustoffQuellenBerichtigung.Zeile z in BaustoffQuellenBerichtigung.Berichtigungen)
            {
                BaustoffSaat s = BaustoffSchema.SaatZu(z.Id);
                Assert.Equal(z.Neu, Text("SELECT Quelle FROM Tab_Baustoff_STAMM WHERE ID = ?", z.Id));
                Assert.Equal(s.Rho, Zahl("SELECT Rho FROM Tab_Baustoff_STAMM WHERE ID = ?", z.Id));
                Assert.Equal(1.0, Zahl("SELECT ReadOnly FROM Tab_Baustoff_STAMM WHERE ID = ?", z.Id));
            }
            Assert.Equal(BaustoffQuellenBerichtigung.Berichtigungen[0].Neu,
                         Text("SELECT Quelle FROM Tab_Baustoff WHERE ID = ?", kopie1041));
            Assert.Equal(BaustoffQuellenBerichtigung.Berichtigungen[1].Neu,
                         Text("SELECT Quelle FROM Tab_Baustoff WHERE ID = ?", kopie1066));
            Assert.Equal(DbWerte.HERKUNFT_KATALOG, Text("SELECT Herkunft FROM Tab_Baustoff WHERE ID = ?", kopie1066));
            Assert.Equal(35.0, Zahl("SELECT Rho FROM Tab_Baustoff WHERE ID = ?", kopie1041));

            // Zweiter Lauf: nichts mehr zu tun, und der Stand bleibt.
            BaustoffQuellenBerichtigung.Bericht zweiter = BaustoffQuellenBerichtigung.Ausfuehren();
            Assert.Equal(0, zweiter.Katalog);
            Assert.Equal(0, zweiter.Kopien);
            Assert.Empty(zweiter.Berichtigt);
            Assert.Equal(BaustoffQuellenBerichtigung.Berichtigungen[0].Neu,
                         Text("SELECT Quelle FROM Tab_Baustoff_STAMM WHERE ID = ?", 1041));
            Assert.Equal(0L, BaustoffQuellenBerichtigung.Offen());
        }

        // =====================================================================
        //  2 - Eine Änderung des Anwenders bleibt stehen
        // =====================================================================

        /// <summary>
        /// Eine vom Anwender geänderte Quelle bleibt — im Katalog wie in der Kopie; eine Kopie mit
        /// dem alten Text, aber einem anderen Bezeichner, ist keine Kopie der Saatzeile und bleibt
        /// ebenfalls. Berichtigt wird allein, was den alten Text wortgleich trägt.
        /// </summary>
        [Fact]
        public void Ein_vom_Anwender_geaenderter_Quelltext_bleibt_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const string eigen1041 = "Eigene Messung, Labor 2026";
            const string eigenKopie = "Kingspan, Produktblatt Kooltherm K5 WDVS-Dämmplatte (DE), Version 15, 07/2026 (geprüft)";
            AltenTextAnlegen();
            var ctrl = new BaustoffCtrl();
            int kopie1041 = ctrl.CopyFromStamm(1041, PROJEKT);
            int kopie1066 = ctrl.CopyFromStamm(1066, PROJEKT);
            Sql("UPDATE Tab_Baustoff_STAMM SET Quelle = ? WHERE ID = 1041", eigen1041);
            Sql("UPDATE Tab_Baustoff SET Quelle = ? WHERE ID = ?", eigenKopie, kopie1041);
            Sql("UPDATE Tab_Baustoff SET Bezeichner = ? WHERE ID = ?", "Dämmputz Nordfassade", kopie1066);
            Assert.Equal(1L, BaustoffQuellenBerichtigung.Offen());

            BaustoffQuellenBerichtigung.Bericht b = BaustoffQuellenBerichtigung.Ausfuehren();
            Assert.Equal(1, b.Katalog);
            Assert.Equal(0, b.Kopien);

            Assert.Equal(eigen1041, Text("SELECT Quelle FROM Tab_Baustoff_STAMM WHERE ID = 1041"));
            Assert.Equal(BaustoffQuellenBerichtigung.Berichtigungen[1].Neu, Text("SELECT Quelle FROM Tab_Baustoff_STAMM WHERE ID = 1066"));
            Assert.Equal(eigenKopie, Text("SELECT Quelle FROM Tab_Baustoff WHERE ID = ?", kopie1041));
            Assert.Equal(BaustoffQuellenBerichtigung.ALT_1066, Text("SELECT Quelle FROM Tab_Baustoff WHERE ID = ?", kopie1066));
            Assert.Equal(0L, BaustoffQuellenBerichtigung.Offen());
        }

        // =====================================================================
        //  3 - Eine neue Datenbank bekommt die neuen Texte mit der Saat
        // =====================================================================

        /// <summary>
        /// Fehlen die zwei Zeilen (eine neue Datenbank), sät <see cref="BaustoffSchema.SaatSchreiben"/>
        /// sie gleich mit den neuen Texten; der Schritt findet danach nichts zu tun.
        /// </summary>
        [Fact]
        public void Die_Saat_einer_neuen_Datenbank_traegt_gleich_die_neuen_Texte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Sql("DELETE FROM Tab_Baustoff_STAMM WHERE ID IN (1041, 1066)");
            Assert.Equal(2, BaustoffSchema.SaatSchreiben());

            foreach (BaustoffQuellenBerichtigung.Zeile z in BaustoffQuellenBerichtigung.Berichtigungen)
                Assert.Equal(z.Neu, Text("SELECT Quelle FROM Tab_Baustoff_STAMM WHERE ID = ?", z.Id));
            Assert.Equal(0L, BaustoffQuellenBerichtigung.Offen());
            BaustoffQuellenBerichtigung.Bericht b = BaustoffQuellenBerichtigung.Ausfuehren();
            Assert.Equal(0, b.Katalog + b.Kopien);
        }

        // =====================================================================
        //  4 - Repo-Datei, Werkzeug und Migration
        // =====================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache des Schritts.</b> Migration der Schale, Werkzeug
        /// <c>Testdatenbankschema</c> und Nachzieh-Liste der Tests führen ihn; die Nummer steht
        /// als Zahl allein bei <see cref="BaustoffQuellenBerichtigung.SCHRITT"/>, er folgt auf die
        /// dritte Berichtigung der Anschlusslängen und liegt nicht über dem Zielstand. Die
        /// REPO-Datei (nur lesend geöffnet) trägt die neuen Texte und keinen alten mehr.
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            Assert.True(SchemaStand.Zielversion >= BaustoffQuellenBerichtigung.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter dem Schritt der Baustoffquellen.");
            Assert.True(BaustoffQuellenBerichtigung.SCHRITT > GebaeudeAnschlusslaengenDritteReparatur.SCHRITT);
            Assert.True(BaustoffQuellenBerichtigung.SCHRITT > BaustoffSchema.SCHRITT);

            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("BaustoffQuellenBerichtigung.Ausfuehren()", werkzeug);
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_BAUSTOFF_QUELLEN = BaustoffQuellenBerichtigung.SCHRITT;", migration);
            Assert.Contains("private static bool Schritt_BaustoffQuellen(Lauf l)", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_GEBAEUDE_DRITTE_REPARATUR", StringComparison.Ordinal);
            int ortSchritt = migration.IndexOf("new Schritt(SCHRITT_BAUSTOFF_QUELLEN", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSchritt > ortVorher,
                        "Der Schritt steht nicht nach der dritten Berichtigung in der Schrittliste.");
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("BaustoffQuellenBerichtigung.Ausfuehren()", vorrichtung);
            string stand = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Update", "SchemaStand.cs"));
            Assert.Contains("<see cref=\"BaustoffQuellenBerichtigung.SCHRITT\"/>", stand, StringComparison.Ordinal);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            foreach (BaustoffQuellenBerichtigung.Zeile z in BaustoffQuellenBerichtigung.Berichtigungen)
            {
                using SqliteCommand cmd = verbindung.CreateCommand();
                cmd.CommandText = "SELECT Quelle FROM Tab_Baustoff_STAMM WHERE ID = $id";
                cmd.Parameters.AddWithValue("$id", z.Id);
                Assert.Equal(z.Neu, Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));

                using SqliteCommand alt = verbindung.CreateCommand();
                alt.CommandText = "SELECT (SELECT COUNT(*) FROM Tab_Baustoff_STAMM WHERE Quelle = $alt) + " +
                                  "(SELECT COUNT(*) FROM Tab_Baustoff WHERE Quelle = $alt)";
                alt.Parameters.AddWithValue("$alt", z.Alt);
                Assert.Equal(0L, Convert.ToInt64(alt.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Legt im Katalog der Arbeitskopie den alten Saattext beider Zeilen wieder an.</summary>
        private static void AltenTextAnlegen()
        {
            foreach (BaustoffQuellenBerichtigung.Zeile z in BaustoffQuellenBerichtigung.Berichtigungen)
                Sql("UPDATE Tab_Baustoff_STAMM SET Quelle = ? WHERE ID = ?", z.Alt, z.Id);
        }

        private static DbParam[] Parameter(object[] werte)
            => werte.Select((w, i) => new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), w)).ToArray();

        private static void Sql(string sql, params object[] werte)
            => Assert.True(DataRepository.ExecuteSQL(sql, Parameter(werte)), "Fehlgeschlagen: " + sql);

        private static string Text(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static double Zahl(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == null || o == DBNull.Value ? double.NaN : Convert.ToDouble(o, CultureInfo.InvariantCulture);
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
