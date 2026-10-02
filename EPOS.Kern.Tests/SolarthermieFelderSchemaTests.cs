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
    /// <b>Der Schemaschritt der Welle M2 Solarthermie</b> (<see cref="SolarthermieFelderSchema"/>): fünf
    /// nullbare Felder des Kollektorfelds an <c>Tab_Energieanlagen</c> und die Bezugsfläche der
    /// Kollektorkennwerte an Katalog und Projektkopie.
    ///
    /// <para><b>Geprüft wird:</b> Nummer, Paketstufe und Prüfklauseln (ohne Datenbank), der Schritt aus
    /// dem Stand davor auf der Testkopie, wiederholbar und ohne Wertänderung am Bestand, und dass
    /// Migration, Werkzeug und Testkopie den Schritt aus derselben Quelle führen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SolarthermieFelderSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Nummer, Stufe und Prüfklauseln (ohne Datenbank)
        // =============================================================================

        /// <summary>Der Schritt folgt auf die Kesseleinheit, ist das Ziel und eine DDL-Stufe der Paketanhebung.</summary>
        [Fact]
        public void Nummer_Ziel_und_Paketstufe()
        {
            Assert.Equal(KesselBereitschaftEinheitSchema.SCHRITT + 1, SolarthermieFelderSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= SolarthermieFelderSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + SolarthermieFelderSchema.SCHRITT + ".");

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == SolarthermieFelderSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);

            Assert.Equal(new[] { "Pumpenleistung_W", "Solarkreisverluste_Prozent", "Uebertrager_Graedigkeit_K",
                                 "Kollektor_Spreizung_K", "Arbeitstemperatur_Weg" },
                         SolarthermieFelderSchema.ANLAGENSPALTEN.Select(p => p.Key).ToArray());
            Assert.Equal(new[] { "Tab_Solarkollektoren_STAMM", "Tab_Solarkollektoren" },
                         SolarthermieFelderSchema.KATALOGTABELLEN);
        }

        /// <summary>
        /// Die Anlagenspalten an einer STRICT-Tabelle: leer zulässig, Grenzen gehalten, Weg nur fest oder
        /// speicher. Die Bezugsfläche: Vorgabe apertur, nur apertur und brutto, nicht NULL.
        /// </summary>
        [Fact]
        public void Pruefklauseln_halten_Grenzen_und_Vorgaben()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE A (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO A (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> s in SolarthermieFelderSchema.ANLAGENSPALTEN)
                Ausfuehren(c, "ALTER TABLE A ADD COLUMN \"" + s.Key + "\" " + s.Value);

            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM A WHERE Pumpenleistung_W IS NULL AND " +
                                       "Solarkreisverluste_Prozent IS NULL AND Arbeitstemperatur_Weg IS NULL"));
            Assert.False(Wirft(c, "UPDATE A SET Pumpenleistung_W = 10000, Solarkreisverluste_Prozent = 50, " +
                                  "Uebertrager_Graedigkeit_K = 30, Kollektor_Spreizung_K = 0, Arbeitstemperatur_Weg = 'speicher'"));
            Assert.True(Wirft(c, "UPDATE A SET Pumpenleistung_W = 10001"));
            Assert.True(Wirft(c, "UPDATE A SET Pumpenleistung_W = -1"));
            Assert.True(Wirft(c, "UPDATE A SET Solarkreisverluste_Prozent = 50.5"));
            Assert.True(Wirft(c, "UPDATE A SET Uebertrager_Graedigkeit_K = 31"));
            Assert.True(Wirft(c, "UPDATE A SET Kollektor_Spreizung_K = -0.1"));
            Assert.True(Wirft(c, "UPDATE A SET Arbeitstemperatur_Weg = 'maximal'"));
            Assert.False(Wirft(c, "UPDATE A SET Arbeitstemperatur_Weg = 'fest'"));
            Assert.False(Wirft(c, "UPDATE A SET Arbeitstemperatur_Weg = NULL, Pumpenleistung_W = NULL"));

            Ausfuehren(c, "CREATE TABLE K (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO K (ID) VALUES (1)");
            Ausfuehren(c, "ALTER TABLE K ADD COLUMN \"" + SolarthermieFelderSchema.SPALTE_BEZUGSFLAECHE + "\" " +
                          SolarthermieFelderSchema.TYP_BEZUGSFLAECHE);
            Ausfuehren(c, "INSERT INTO K (ID) VALUES (2)");
            Assert.Equal(2L, Skalar(c, "SELECT COUNT(*) FROM K WHERE Bezugsflaeche = 'apertur'"));
            Assert.False(Wirft(c, "UPDATE K SET Bezugsflaeche = 'brutto' WHERE ID = 2"));
            Assert.True(Wirft(c, "UPDATE K SET Bezugsflaeche = 'absorber'"));
            Assert.True(Wirft(c, "UPDATE K SET Bezugsflaeche = NULL"));
        }

        // =============================================================================
        //  Teil 2 - der Schritt auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Der Schritt aus dem Stand davor: Die sieben Spalten werden entfernt; der Schritt legt sie an,
        /// die Anlagenspalten bleiben leer, jeder Kollektorsatz steht auf apertur, die übrigen Werte
        /// bleiben, und ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_ist_wiederholbar_und_laesst_den_Bestand()
        {
            if (!_db.Vorhanden) return;

            Assert.True(SolarthermieFelderSchema.Vollstaendig());
            Assert.Empty(SolarthermieFelderSchema.Anweisungen);
            List<string> vorher = Bestand();

            foreach (KeyValuePair<string, string> s in SolarthermieFelderSchema.ANLAGENSPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + SolarthermieFelderSchema.TAB_ANLAGEN +
                                               "\" DROP COLUMN \"" + s.Key + "\"");
            foreach (string tabelle in SolarthermieFelderSchema.KATALOGTABELLEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" +
                                               SolarthermieFelderSchema.SPALTE_BEZUGSFLAECHE + "\"");
            Assert.False(SolarthermieFelderSchema.Vollstaendig());
            Assert.Equal(7, SolarthermieFelderSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(7, SolarthermieFelderSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Energieanlagen.Arbeitstemperatur_Weg anlegen"));
            Assert.Contains(bericht, z => z.Contains("Tab_Solarkollektoren_STAMM.Bezugsflaeche anlegen"));
            Assert.True(SolarthermieFelderSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());

            foreach (KeyValuePair<string, string> s in SolarthermieFelderSchema.ANLAGENSPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE \"" + s.Key + "\" IS NOT NULL"));
            foreach (string tabelle in SolarthermieFelderSchema.KATALOGTABELLEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE Bezugsflaeche <> 'apertur'"));

            var zweiter = new List<string>();
            Assert.Equal(0, SolarthermieFelderSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt
        /// aus derselben Quelle, hinter der Einheit des Kessel-Bereitschaftsverlusts.
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("SolarthermieFelderSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("KesselBereitschaftEinheitSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_SOLARTHERMIE_FELDER = SolarthermieFelderSchema.SCHRITT", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_SOLARTHERMIE_FELDER", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_KESSEL_BEREITSCHAFT_EINHEIT", StringComparison.Ordinal));
            Assert.Contains("SolarthermieFelderSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("SolarthermieFelderSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("KesselBereitschaftEinheitSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Kollektorzeilen und Kollektorsätze ohne die neuen Spalten — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            DataTable a = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Kollektormodulanzahl, Neigung, Azimut, Hilfsenergie_Anteil FROM Tab_Energieanlagen " +
                "WHERE ID_Solar IS NOT NULL ORDER BY ID");
            foreach (DataRow r in a.Rows)
                liste.Add("A|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            foreach (string tabelle in SolarthermieFelderSchema.KATALOGTABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Modulflaeche, Aperturflaeche, h0, k1, k2, Kdir, Kdfu FROM \"" + tabelle +
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
