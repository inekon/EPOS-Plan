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
    /// <b>Der Schemaschritt der Welle M4 „Erzeuger in Teillast"</b> (<see cref="ErzeugerTeillastSchema"/>):
    /// zwei nullbare Felder der Wärmepumpe an <c>Tab_WP(_STAMM)</c> und vier des BHKW an
    /// <c>Tab_BHKW(_STAMM)</c>.
    ///
    /// <para><b>Geprüft wird:</b> Nummer, Paketstufe und Prüfklauseln (ohne Datenbank), der Schritt aus
    /// dem Stand davor auf der Testkopie, wiederholbar und ohne Wertänderung am Bestand, und dass
    /// Migration, Werkzeug und Testkopie den Schritt aus derselben Quelle führen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErzeugerTeillastSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Nummer, Stufe und Prüfklauseln (ohne Datenbank)
        // =============================================================================

        /// <summary>Der Schritt folgt auf die Felder des Kollektorfelds, ist das Ziel und eine DDL-Stufe.</summary>
        [Fact]
        public void Nummer_Ziel_und_Paketstufe()
        {
            Assert.Equal(SolarthermieFelderSchema.SCHRITT + 1, ErzeugerTeillastSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ErzeugerTeillastSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + ErzeugerTeillastSchema.SCHRITT + ".");

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == ErzeugerTeillastSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);

            Assert.Equal(new[] { "Mindestleistung_kW", "Taktverlustfaktor_Cd" },
                         ErzeugerTeillastSchema.WP_SPALTEN.Select(p => p.Key).ToArray());
            Assert.Equal(new[] { "Wirkungsgrad_el_Teillast50", "Wirkungsgrad_th_Teillast50", "Anfahrverlust_kWh",
                                 "Mindestlaufzeit_min" },
                         ErzeugerTeillastSchema.BHKW_SPALTEN.Select(p => p.Key).ToArray());
            Assert.Equal(new[] { "Tab_WP_STAMM", "Tab_WP", "Tab_BHKW_STAMM", "Tab_BHKW" },
                         ErzeugerTeillastSchema.TABELLEN);
        }

        /// <summary>Die sechs Spalten an einer STRICT-Tabelle: leer zulässig, Grenzen gehalten.</summary>
        [Fact]
        public void Pruefklauseln_halten_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE A (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO A (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> s in ErzeugerTeillastSchema.WP_SPALTEN.Concat(ErzeugerTeillastSchema.BHKW_SPALTEN))
                Ausfuehren(c, "ALTER TABLE A ADD COLUMN \"" + s.Key + "\" " + s.Value);

            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM A WHERE Mindestleistung_kW IS NULL AND " +
                                       "Taktverlustfaktor_Cd IS NULL AND Wirkungsgrad_el_Teillast50 IS NULL AND " +
                                       "Mindestlaufzeit_min IS NULL"));
            Assert.False(Wirft(c, "UPDATE A SET Mindestleistung_kW = 1000, Taktverlustfaktor_Cd = 1, " +
                                  "Wirkungsgrad_el_Teillast50 = 1, Wirkungsgrad_th_Teillast50 = 0, " +
                                  "Anfahrverlust_kWh = 100, Mindestlaufzeit_min = 60"));
            Assert.True(Wirft(c, "UPDATE A SET Mindestleistung_kW = 1000.5"));
            Assert.True(Wirft(c, "UPDATE A SET Mindestleistung_kW = -1"));
            Assert.True(Wirft(c, "UPDATE A SET Taktverlustfaktor_Cd = 1.01"));
            Assert.True(Wirft(c, "UPDATE A SET Wirkungsgrad_el_Teillast50 = 34"));
            Assert.True(Wirft(c, "UPDATE A SET Wirkungsgrad_th_Teillast50 = -0.1"));
            Assert.True(Wirft(c, "UPDATE A SET Anfahrverlust_kWh = 100.1"));
            Assert.True(Wirft(c, "UPDATE A SET Mindestlaufzeit_min = 61"));
            Assert.True(Wirft(c, "UPDATE A SET Mindestlaufzeit_min = 12.5"));
            Assert.False(Wirft(c, "UPDATE A SET Mindestleistung_kW = NULL, Mindestlaufzeit_min = NULL"));
        }

        // =============================================================================
        //  Teil 2 - der Schritt auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Der Schritt aus dem Stand davor: Die zwölf Spalten werden entfernt; der Schritt legt sie leer
        /// an, die übrigen Werte bleiben, und ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_ist_wiederholbar_und_laesst_den_Bestand()
        {
            if (!_db.Vorhanden) return;

            Assert.True(ErzeugerTeillastSchema.Vollstaendig());
            Assert.Empty(ErzeugerTeillastSchema.Anweisungen);
            List<string> vorher = Bestand();

            foreach (string tabelle in ErzeugerTeillastSchema.WP_TABELLEN)
                foreach (KeyValuePair<string, string> s in ErzeugerTeillastSchema.WP_SPALTEN)
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + s.Key + "\"");
            foreach (string tabelle in ErzeugerTeillastSchema.BHKW_TABELLEN)
                foreach (KeyValuePair<string, string> s in ErzeugerTeillastSchema.BHKW_SPALTEN)
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + s.Key + "\"");
            Assert.False(ErzeugerTeillastSchema.Vollstaendig());
            Assert.False(ErzeugerTeillastSchema.WpSpaltenVorhanden("Tab_WP"));
            Assert.False(ErzeugerTeillastSchema.BhkwSpaltenVorhanden("Tab_BHKW_STAMM"));
            Assert.Equal(12, ErzeugerTeillastSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(12, ErzeugerTeillastSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_WP_STAMM.Mindestleistung_kW anlegen"));
            Assert.Contains(bericht, z => z.Contains("Tab_BHKW.Mindestlaufzeit_min anlegen"));
            Assert.True(ErzeugerTeillastSchema.Vollstaendig());
            Assert.True(ErzeugerTeillastSchema.WpSpaltenVorhanden("Tab_WP"));
            Assert.True(ErzeugerTeillastSchema.BhkwSpaltenVorhanden("Tab_BHKW_STAMM"));
            Assert.Equal(vorher, Bestand());

            foreach (string tabelle in ErzeugerTeillastSchema.WP_TABELLEN)
                foreach (KeyValuePair<string, string> s in ErzeugerTeillastSchema.WP_SPALTEN)
                    Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + s.Key + "\" IS NOT NULL"));
            foreach (string tabelle in ErzeugerTeillastSchema.BHKW_TABELLEN)
                foreach (KeyValuePair<string, string> s in ErzeugerTeillastSchema.BHKW_SPALTEN)
                    Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + s.Key + "\" IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, ErzeugerTeillastSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt
        /// aus derselben Quelle, hinter den Feldern des Kollektorfelds.
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("ErzeugerTeillastSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("SolarthermieFelderSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_ERZEUGER_TEILLAST = ErzeugerTeillastSchema.SCHRITT", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_ERZEUGER_TEILLAST", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_SOLARTHERMIE_FELDER", StringComparison.Ordinal));
            Assert.Contains("ErzeugerTeillastSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("ErzeugerTeillastSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("SolarthermieFelderSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Geräte ohne die neuen Spalten — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string tabelle in ErzeugerTeillastSchema.WP_TABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Nennleistung, maxPtherm FROM \"" + tabelle + "\" ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                    liste.Add(tabelle + "|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            foreach (string tabelle in ErzeugerTeillastSchema.BHKW_TABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Ptherm, Pel, Wirkungsgrad, Grenzleistung FROM \"" + tabelle + "\" ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                    liste.Add(tabelle + "|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static long Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
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
