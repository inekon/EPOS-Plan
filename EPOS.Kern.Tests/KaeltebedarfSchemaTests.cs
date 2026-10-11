using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schemaschritt des Kältebedarfs</b> (<see cref="KaeltebedarfSchema"/>; Welle K1, Konzept Kältebedarf 5): fünf
    /// STRICT-Tabellen, Deckungsspalten an beiden Zuordnungen, fünf leere Ergebnisspalten, Saat; wiederholbar, ergebnisneutral.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltebedarfSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_ist_213_das_Ziel_und_die_Paketanhebung_fuehrt_Ddl()
        {
            Assert.Equal(213, KaeltebedarfSchema.SCHRITT);
            Assert.Equal(KaelteRangSchema.SCHRITT + 1, KaeltebedarfSchema.SCHRITT);
            Assert.Equal(KaeltebedarfSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KaeltebedarfSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Jede_neue_Tabelle_ist_STRICT_und_verweist_ueber_IDs()
        {
            foreach (KeyValuePair<string, string> t in KaeltebedarfSchema.Tabellen())
                Assert.EndsWith(") STRICT", t.Value, StringComparison.Ordinal);
            Assert.Contains("\"ID_Stamm\" INTEGER REFERENCES \"Tab_Kaeltebedarf_STAMM\" (\"ID\") ON DELETE SET NULL",
                            KaeltebedarfSchema.SqlKopf(), StringComparison.Ordinal);
            Assert.Contains("\"ID_Kaeltebedarf\" INTEGER NOT NULL REFERENCES \"Tab_Kaeltebedarf\"",
                            KaeltebedarfSchema.SqlZuordnung(), StringComparison.Ordinal);
            Assert.Contains("\"ID_Betriebskalender\" INTEGER REFERENCES \"Tab_Betriebskalender\"",
                            KaeltebedarfSchema.SqlZuordnung(), StringComparison.Ordinal);
            Assert.Equal(8, KaeltebedarfSchema.DECKUNGSSPALTEN.Count);
            Assert.Equal(5, KaeltebedarfSchema.ERGEBNISSPALTEN.Count);
            Assert.Equal(13, KaeltebedarfSchema.SPALTEN.Count);
        }

        [Fact]
        public void Die_Pruefklauseln_halten_Deckung_EER_und_Temperaturpaar()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Projekt\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_Betriebskalender\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"energy_carrier\" (id INTEGER PRIMARY KEY) STRICT");
            foreach (KeyValuePair<string, string> t in KaeltebedarfSchema.Tabellen()) Ausfuehren(c, t.Value);
            foreach (KeyValuePair<string, string> i in KaeltebedarfSchema.Indizes()) Ausfuehren(c, i.Value);
            Ausfuehren(c, "INSERT INTO Tab_Projekt (ID) VALUES (1)");
            Ausfuehren(c, "INSERT INTO Tab_Kaeltebedarf (ID, ID_Projekt, Bezeichner) VALUES (1, 1, 'A')");
            Ausfuehren(c, "INSERT INTO Z_Projekt_Kaeltebedarf (ID, ID_Projekt, ID_Kaeltebedarf) VALUES (1, 1, 1)");
            Assert.Equal("zentral", Text(c, "SELECT Deckung FROM Z_Projekt_Kaeltebedarf"));
            Assert.Equal("0", Text(c, "SELECT Kuehl_EigenerZaehler FROM Z_Projekt_Kaeltebedarf"));

            foreach (string gut in new[] { "Deckung = 'split'", "Split_EER_Weg = 'linear'", "Split_EER_1 = 3.0",
                                           "Split_Taussen_1 = -20", "Split_Taussen_2 = 50", "Kuehl_EigenerZaehler = 1" })
                Ausfuehren(c, "UPDATE Z_Projekt_Kaeltebedarf SET " + gut);
            foreach (string schlecht in new[] { "Deckung = 'raumgeraet'", "Deckung = NULL", "Split_EER_Weg = 'quadratisch'",
                                                "Split_EER_1 = 0", "Split_EER_2 = -1", "Split_Taussen_1 = -21",
                                                "Split_Taussen_2 = 51", "Kuehl_EigenerZaehler = 2" })
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Z_Projekt_Kaeltebedarf SET " + schlecht));

            Ausfuehren(c, "UPDATE Tab_Kaeltebedarf SET Vorlauf = -30, Ruecklauf = -24");
            foreach (string schlecht in new[] { "Vorlauf = 12, Ruecklauf = 6", "Vorlauf = 6, Ruecklauf = NULL",
                                                "Vorlauf = -41, Ruecklauf = 0", "Vorlauf = 6, Ruecklauf = 101" })
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Tab_Kaeltebedarf SET " + schlecht));
            Assert.Null(KaeltebedarfSchema.Paarpruefung(16, 19));
            Assert.Null(KaeltebedarfSchema.Paarpruefung(null, null));
            Assert.NotNull(KaeltebedarfSchema.Paarpruefung(12, 6));
            Assert.NotNull(KaeltebedarfSchema.Paarpruefung(6, null));
        }

        /// <summary>Die Testkopie steht auf dem Schritt; jeder Lastgang steht auf „zentral“, keine Zuordnung (Basis byte-gleich).</summary>
        [Fact]
        public void Die_Testkopie_steht_auf_213_ohne_Kaeltebedarf_im_Projekt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KaeltebedarfSchema.SCHRITT);
            Assert.True(KaeltebedarfSchema.Vollstaendig());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_Projekt_Kaeltebedarf"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE Deckung <> 'zentral' OR Split_EER_1 IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE Kaeltebedarf_Profil_MWh IS NOT NULL"));
        }

        [Fact]
        public void Der_Schritt_legt_alles_an_setzt_die_Vorgabe_zentral_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            long lastgaenge = Zahl("SELECT COUNT(*) FROM Z_ProjektWaermebedarf");
            foreach (string t in new[] { "Z_Projekt_Kaeltebedarf", "Tab_Kaeltetyp", "Tab_Kaeltebedarf", "Tab_Kaeltetyp_STAMM", "Tab_Kaeltebedarf_STAMM" })
                DataRepository.ExecuteNonQuery("DROP TABLE \"" + t + "\"");
            foreach ((string tabelle, string spalte, string _) in KaeltebedarfSchema.SPALTEN.Reverse())
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            Assert.False(KaeltebedarfSchema.Vollstaendig());

            var bericht = new List<string>();
            int n = KaeltebedarfSchema.Ausfuehren(bericht);
            Assert.Equal(KaeltebedarfSchema.Tabellen().Count + KaeltebedarfSchema.SPALTEN.Count + KaeltebedarfSchema.Indizes().Count, n);
            Assert.True(KaeltebedarfSchema.Vollstaendig());
            Assert.Equal(lastgaenge, Zahl("SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE Deckung = 'zentral' AND Kuehl_EigenerZaehler = 0"));
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf_STAMM WHERE ReadOnly = 1 AND Katalog_Schluessel IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            foreach (string t in new[] { "Tab_Kaeltebedarf_STAMM", "Tab_Kaeltetyp_STAMM", "Tab_Kaeltebedarf", "Tab_Kaeltetyp", "Z_Projekt_Kaeltebedarf" })
                Assert.Contains("STRICT", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@n", t)), CultureInfo.InvariantCulture),
                    StringComparison.Ordinal);

            var zweiter = new List<string>();
            Assert.Equal(0, KaeltebedarfSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("stehen bereits", StringComparison.Ordinal));
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyp_STAMM"));
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Text(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
