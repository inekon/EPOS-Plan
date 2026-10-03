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
    /// <b>Der Schemaschritt der Welle M5 Strom in Viertelstunden</b> (<see cref="StromViertelstundenSchema"/>):
    /// die Einspeisegrenze an <c>Tab_Einstellungen</c> (PV3) und die Selbstentladung an Katalog und
    /// Projektkopie des Stromspeichers (SP1).
    ///
    /// <para><b>Geprüft wird:</b> Nummer, Paketstufe und Prüfklauseln (ohne Datenbank), der Schritt aus
    /// dem Stand davor auf der Testkopie, wiederholbar und ohne Wertänderung am Bestand, und dass
    /// Migration, Werkzeug und Testkopie den Schritt aus derselben Quelle führen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromViertelstundenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Nummer, Stufe und Prüfklauseln (ohne Datenbank)
        // =============================================================================

        /// <summary>Der Schritt folgt auf die Teillastfelder der Erzeuger, ist das Ziel und eine DDL-Stufe.</summary>
        [Fact]
        public void Nummer_Ziel_und_Paketstufe()
        {
            Assert.Equal(ErzeugerTeillastSchema.SCHRITT + 1, StromViertelstundenSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= StromViertelstundenSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + StromViertelstundenSchema.SCHRITT + ".");

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == StromViertelstundenSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);

            Assert.Equal(new[] { "Einspeisegrenze_Wert", "Einspeisegrenze_Einheit" },
                         StromViertelstundenSchema.EINSTELLUNGSSPALTEN.Select(p => p.Key).ToArray());
            Assert.Equal(new[] { "Tab_Stromspeicher_STAMM", "Tab_Stromspeicher" },
                         StromViertelstundenSchema.KATALOGTABELLEN);
        }

        /// <summary>
        /// Die Einstellungsspalten an einer STRICT-Tabelle: leer zulässig, Wert nicht negativ, Einheit nur kW
        /// oder %. Die Selbstentladung: leer zulässig, 0 … 20.
        /// </summary>
        [Fact]
        public void Pruefklauseln_halten_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE E (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO E (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> s in StromViertelstundenSchema.EINSTELLUNGSSPALTEN)
                Ausfuehren(c, "ALTER TABLE E ADD COLUMN \"" + s.Key + "\" " + s.Value);

            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM E WHERE Einspeisegrenze_Wert IS NULL AND " +
                                       "Einspeisegrenze_Einheit IS NULL"));
            Assert.False(Wirft(c, "UPDATE E SET Einspeisegrenze_Wert = 0, Einspeisegrenze_Einheit = 'kW'"));
            Assert.False(Wirft(c, "UPDATE E SET Einspeisegrenze_Wert = 70, Einspeisegrenze_Einheit = '%'"));
            Assert.True(Wirft(c, "UPDATE E SET Einspeisegrenze_Wert = -0.1"));
            Assert.True(Wirft(c, "UPDATE E SET Einspeisegrenze_Einheit = 'MW'"));
            Assert.False(Wirft(c, "UPDATE E SET Einspeisegrenze_Wert = NULL, Einspeisegrenze_Einheit = NULL"));

            Ausfuehren(c, "CREATE TABLE K (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO K (ID) VALUES (1)");
            Ausfuehren(c, "ALTER TABLE K ADD COLUMN \"" + StromViertelstundenSchema.SPALTE_SELBSTENTLADUNG + "\" " +
                          StromViertelstundenSchema.TYP_SELBSTENTLADUNG);
            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM K WHERE Selbstentladung_Prozent_Monat IS NULL"));
            Assert.False(Wirft(c, "UPDATE K SET Selbstentladung_Prozent_Monat = 20"));
            Assert.False(Wirft(c, "UPDATE K SET Selbstentladung_Prozent_Monat = 0"));
            Assert.True(Wirft(c, "UPDATE K SET Selbstentladung_Prozent_Monat = 20.5"));
            Assert.True(Wirft(c, "UPDATE K SET Selbstentladung_Prozent_Monat = -1"));
        }

        // =============================================================================
        //  Teil 2 - der Schritt auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Der Schritt aus dem Stand davor: Die vier Spalten werden entfernt; der Schritt legt sie leer an,
        /// die übrigen Werte bleiben, und ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_ist_wiederholbar_und_laesst_den_Bestand()
        {
            if (!_db.Vorhanden) return;

            Assert.True(StromViertelstundenSchema.Vollstaendig());
            Assert.Empty(StromViertelstundenSchema.Anweisungen);
            List<string> vorher = Bestand();

            foreach (KeyValuePair<string, string> s in StromViertelstundenSchema.EINSTELLUNGSSPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + StromViertelstundenSchema.TAB_EINSTELLUNGEN +
                                               "\" DROP COLUMN \"" + s.Key + "\"");
            foreach (string tabelle in StromViertelstundenSchema.KATALOGTABELLEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" +
                                               StromViertelstundenSchema.SPALTE_SELBSTENTLADUNG + "\"");
            Assert.False(StromViertelstundenSchema.Vollstaendig());
            Assert.Equal(4, StromViertelstundenSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(4, StromViertelstundenSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Einstellungen.Einspeisegrenze_Wert anlegen"));
            Assert.Contains(bericht, z => z.Contains("Tab_Stromspeicher_STAMM.Selbstentladung_Prozent_Monat anlegen"));
            Assert.True(StromViertelstundenSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());

            foreach (KeyValuePair<string, string> s in StromViertelstundenSchema.EINSTELLUNGSSPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE \"" + s.Key + "\" IS NOT NULL"));
            foreach (string tabelle in StromViertelstundenSchema.KATALOGTABELLEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE Selbstentladung_Prozent_Monat IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, StromViertelstundenSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt
        /// aus derselben Quelle, hinter den Teillastfeldern der Erzeuger.
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("StromViertelstundenSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("ErzeugerTeillastSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_STROM_VIERTELSTUNDEN = StromViertelstundenSchema.SCHRITT", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_STROM_VIERTELSTUNDEN", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_ERZEUGER_TEILLAST", StringComparison.Ordinal));
            Assert.Contains("StromViertelstundenSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("StromViertelstundenSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("ErzeugerTeillastSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Einstellungs- und Speicherzeilen ohne die neuen Spalten — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            DataTable a = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt, Tool_1, Ladefuellstand_Min, Leistungsgrenze FROM Tab_Einstellungen ORDER BY ID");
            foreach (DataRow r in a.Rows)
                liste.Add("E|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            foreach (string tabelle in StromViertelstundenSchema.KATALOGTABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Leistung, Energie, Wirkungsgrad_RT, Standby_Verbrauch FROM \"" + tabelle +
                    "\" ORDER BY ID");
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
