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
    /// <b>Der Schemaschritt der Welle M7 Speicher</b> (<see cref="PufferOptionenSchema"/>): die Optionen
    /// des Pufferspeichers an <c>Tab_Pufferspeicher</c> (PS1 (c), PS1 (a), PS5 (a)) und die thermische
    /// Desinfektion an <c>Tab_Einstellungen</c> (BW5).
    ///
    /// <para><b>Geprüft wird:</b> Nummer, Paketstufe und Prüfklauseln (ohne Datenbank), der Schritt aus
    /// dem Stand davor auf der Testkopie, wiederholbar und ohne Wertänderung am Bestand, und dass
    /// Migration, Werkzeug und Testkopie den Schritt aus derselben Quelle führen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferOptionenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Nummer, Stufe und Prüfklauseln (ohne Datenbank)
        // =============================================================================

        /// <summary>Der Schritt folgt auf die Einspeisegrenze, ist das Ziel und eine DDL-Stufe.</summary>
        [Fact]
        public void Nummer_Ziel_und_Paketstufe()
        {
            Assert.Equal(StromViertelstundenSchema.SCHRITT + 1, PufferOptionenSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= PufferOptionenSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + PufferOptionenSchema.SCHRITT + ".");

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == PufferOptionenSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);

            Assert.Equal(new[] { "Bereitschaft_Weg", "Aufstellraum_Temperatur_C", "Schicht_Anteile",
                                 "Frischwassermodul", "FWM_Graedigkeit_K" },
                         PufferOptionenSchema.PUFFERSPALTEN.Select(p => p.Key).ToArray());
            Assert.Equal(new[] { "Desinfektion_Aktiv", "Desinfektion_Intervall_Tage", "Desinfektion_Stunde",
                                 "Desinfektion_Zieltemperatur_C", "Desinfektion_Volumen_l" },
                         PufferOptionenSchema.EINSTELLUNGSSPALTEN.Select(p => p.Key).ToArray());
        }

        /// <summary>Die Pufferspalten an einer STRICT-Tabelle: leer zulässig, Grenzen gehalten.</summary>
        [Fact]
        public void Pruefklauseln_der_Pufferspalten_halten_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE P (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO P (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> s in PufferOptionenSchema.PUFFERSPALTEN)
                Ausfuehren(c, "ALTER TABLE P ADD COLUMN \"" + s.Key + "\" " + s.Value);

            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM P WHERE Bereitschaft_Weg IS NULL AND " +
                                       "Aufstellraum_Temperatur_C IS NULL AND Schicht_Anteile IS NULL AND " +
                                       "Frischwassermodul IS NULL AND FWM_Graedigkeit_K IS NULL"));
            Assert.False(Wirft(c, "UPDATE P SET Bereitschaft_Weg = 'tag'"));
            Assert.False(Wirft(c, "UPDATE P SET Bereitschaft_Weg = 'temperatur'"));
            Assert.True(Wirft(c, "UPDATE P SET Bereitschaft_Weg = 'Tag'"));
            Assert.False(Wirft(c, "UPDATE P SET Aufstellraum_Temperatur_C = 35"));
            Assert.True(Wirft(c, "UPDATE P SET Aufstellraum_Temperatur_C = 35.5"));
            Assert.True(Wirft(c, "UPDATE P SET Aufstellraum_Temperatur_C = -1"));
            Assert.False(Wirft(c, "UPDATE P SET Schicht_Anteile = '0,10;0,16;0,37;0,37'"));
            Assert.True(Wirft(c, "UPDATE P SET Schicht_Anteile = ''"));
            Assert.False(Wirft(c, "UPDATE P SET Frischwassermodul = 1"));
            Assert.True(Wirft(c, "UPDATE P SET Frischwassermodul = 2"));
            Assert.False(Wirft(c, "UPDATE P SET FWM_Graedigkeit_K = 20"));
            Assert.True(Wirft(c, "UPDATE P SET FWM_Graedigkeit_K = 20.1"));
        }

        /// <summary>Die Desinfektionsspalten an einer STRICT-Tabelle: leer zulässig, Grenzen gehalten.</summary>
        [Fact]
        public void Pruefklauseln_der_Desinfektion_halten_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE E (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO E (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> s in PufferOptionenSchema.EINSTELLUNGSSPALTEN)
                Ausfuehren(c, "ALTER TABLE E ADD COLUMN \"" + s.Key + "\" " + s.Value);

            Assert.False(Wirft(c, "UPDATE E SET Desinfektion_Aktiv = 1, Desinfektion_Intervall_Tage = 7, " +
                                  "Desinfektion_Stunde = 2, Desinfektion_Zieltemperatur_C = 70, Desinfektion_Volumen_l = 1000"));
            Assert.True(Wirft(c, "UPDATE E SET Desinfektion_Aktiv = 2"));
            Assert.True(Wirft(c, "UPDATE E SET Desinfektion_Intervall_Tage = 0"));
            Assert.True(Wirft(c, "UPDATE E SET Desinfektion_Intervall_Tage = 32"));
            Assert.True(Wirft(c, "UPDATE E SET Desinfektion_Stunde = 24"));
            Assert.True(Wirft(c, "UPDATE E SET Desinfektion_Zieltemperatur_C = 54.9"));
            Assert.True(Wirft(c, "UPDATE E SET Desinfektion_Zieltemperatur_C = 90.1"));
            Assert.True(Wirft(c, "UPDATE E SET Desinfektion_Volumen_l = 100001"));
            Assert.False(Wirft(c, "UPDATE E SET Desinfektion_Aktiv = NULL, Desinfektion_Intervall_Tage = NULL, " +
                                  "Desinfektion_Stunde = NULL, Desinfektion_Zieltemperatur_C = NULL, Desinfektion_Volumen_l = NULL"));
        }

        // =============================================================================
        //  Teil 2 - der Schritt auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Der Schritt aus dem Stand davor: Die zehn Spalten werden entfernt; der Schritt legt sie leer an,
        /// die übrigen Werte bleiben, und ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_ist_wiederholbar_und_laesst_den_Bestand()
        {
            if (!_db.Vorhanden) return;

            Assert.True(PufferOptionenSchema.Vollstaendig());
            Assert.Empty(PufferOptionenSchema.Anweisungen);
            List<string> vorher = Bestand();

            foreach (KeyValuePair<string, string> s in PufferOptionenSchema.PUFFERSPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + PufferOptionenSchema.TAB_PUFFER +
                                               "\" DROP COLUMN \"" + s.Key + "\"");
            foreach (KeyValuePair<string, string> s in PufferOptionenSchema.EINSTELLUNGSSPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + PufferOptionenSchema.TAB_EINSTELLUNGEN +
                                               "\" DROP COLUMN \"" + s.Key + "\"");
            Assert.False(PufferOptionenSchema.Vollstaendig());
            Assert.Equal(10, PufferOptionenSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(10, PufferOptionenSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Pufferspeicher.Bereitschaft_Weg anlegen"));
            Assert.Contains(bericht, z => z.Contains("Tab_Einstellungen.Desinfektion_Aktiv anlegen"));
            Assert.True(PufferOptionenSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());

            foreach (KeyValuePair<string, string> s in PufferOptionenSchema.PUFFERSPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE \"" + s.Key + "\" IS NOT NULL"));
            foreach (KeyValuePair<string, string> s in PufferOptionenSchema.EINSTELLUNGSSPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE \"" + s.Key + "\" IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, PufferOptionenSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt
        /// aus derselben Quelle, hinter der Einspeisegrenze.
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("PufferOptionenSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("StromViertelstundenSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_PUFFER_OPTIONEN = PufferOptionenSchema.SCHRITT", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_PUFFER_OPTIONEN", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_STROM_VIERTELSTUNDEN", StringComparison.Ordinal));
            Assert.Contains("PufferOptionenSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("PufferOptionenSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("StromViertelstundenSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Einstellungs- und Pufferzeilen ohne die neuen Spalten — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            DataTable a = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt, Tool_1, Ladefuellstand_Min, Leistungsgrenze FROM Tab_Einstellungen ORDER BY ID");
            foreach (DataRow r in a.Rows)
                liste.Add("E|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            DataTable p = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt, Bezeichner, Gesamtvolumen, Bereitschaftsverluste, Schichten_Anzahl " +
                "FROM Tab_Pufferspeicher ORDER BY ID");
            foreach (DataRow r in p.Rows)
                liste.Add("P|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
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
