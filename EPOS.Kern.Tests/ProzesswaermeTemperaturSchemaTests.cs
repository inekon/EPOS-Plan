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
    /// <b>Das Temperaturpaar je Prozess — der Schemaschritt</b> (Entscheidungsvorlage Modellgrenzen,
    /// PW1 Stufe 1; Schritt bei <see cref="ProzesswaermeTemperaturSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> Nummer und Register, die Prüfklauseln an einer STRICT-Tabelle (beide
    /// leer oder beide gesetzt, 0 bis 250 °C, Vorlauf nicht unter dem Rücklauf), der Schritt aus dem
    /// Stand davor und wiederholbar ohne Änderung am Bestand, und dass Migration, Werkzeug und
    /// Testkopie ihn aus derselben Quelle führen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProzesswaermeTemperaturSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Nummer hinter der Bodenalbedo, Ziel des Schemas, Register mit Art Ddl.</summary>
        [Fact]
        public void Nummer_Ziel_und_Register()
        {
            Assert.Equal(AlbedoSchema.SCHRITT + 1, ProzesswaermeTemperaturSchema.SCHRITT);
            Assert.Equal(164, ProzesswaermeTemperaturSchema.SCHRITT);   // 163 ist die Bodenalbedo
            Assert.True(SchemaStand.Zielversion >= ProzesswaermeTemperaturSchema.SCHRITT);
            Assert.Equal(new[] { "Tab_Prozesswaerme_STAMM", "Tab_Prozesswaerme" }, ProzesswaermeTemperaturSchema.TABELLEN);
            Assert.Equal(new[] { "Vorlauf", "Ruecklauf" }, ProzesswaermeTemperaturSchema.SPALTEN.Select(s => s.Key));

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == ProzesswaermeTemperaturSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
        }

        /// <summary>
        /// Die Prüfklauseln an einer STRICT-Tabelle: leer und leer, ein gepflegtes Paar und Vorlauf =
        /// Rücklauf sind zulässig; ein halbes Paar, ein Vorlauf unter dem Rücklauf und Werte außerhalb
        /// 0 … 250 °C nicht.
        /// </summary>
        [Fact]
        public void Die_Pruefklauseln_halten_das_Paar()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> sp in ProzesswaermeTemperaturSchema.SPALTEN)
                Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + sp.Key + "\" " + sp.Value);

            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM T WHERE Vorlauf IS NULL AND Ruecklauf IS NULL"));
            Assert.False(Wirft(c, "UPDATE T SET Vorlauf = 90, Ruecklauf = 60"));
            Assert.False(Wirft(c, "UPDATE T SET Vorlauf = 60, Ruecklauf = 60"));
            Assert.False(Wirft(c, "UPDATE T SET Vorlauf = 250, Ruecklauf = 0"));
            Assert.False(Wirft(c, "UPDATE T SET Vorlauf = NULL, Ruecklauf = NULL"));

            Assert.True(Wirft(c, "UPDATE T SET Vorlauf = 90"), "Ein halbes Paar ist unzulässig.");
            Assert.True(Wirft(c, "INSERT INTO T (ID, Ruecklauf) VALUES (2, 40)"), "Ein halbes Paar ist unzulässig.");
            Assert.True(Wirft(c, "UPDATE T SET Vorlauf = 50, Ruecklauf = 60"), "Vorlauf unter dem Rücklauf.");
            Assert.True(Wirft(c, "UPDATE T SET Vorlauf = 251, Ruecklauf = 60"), "Über 250 °C.");
            Assert.True(Wirft(c, "UPDATE T SET Vorlauf = 60, Ruecklauf = -1"), "Unter 0 °C.");
        }

        /// <summary>
        /// Der Schritt aus dem Stand davor: Die Spalten werden entfernt; der Schritt legt alle vier
        /// an, jede Zeile bleibt ohne Paar, die übrigen Werte bleiben, ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Assert.True(ProzesswaermeTemperaturSchema.Vollstaendig());
            Assert.Empty(ProzesswaermeTemperaturSchema.Anweisungen);
            List<string> vorher = Bestand();

            // Erst der Rücklauf: Seine Paarklauseln nennen den Vorlauf.
            foreach (string tabelle in ProzesswaermeTemperaturSchema.TABELLEN)
            {
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"Ruecklauf\"");
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"Vorlauf\"");
            }
            Assert.False(ProzesswaermeTemperaturSchema.Vollstaendig());
            Assert.Equal(4, ProzesswaermeTemperaturSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(4, ProzesswaermeTemperaturSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Prozesswaerme_STAMM.Vorlauf anlegen"));
            Assert.True(ProzesswaermeTemperaturSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());
            foreach (string tabelle in ProzesswaermeTemperaturSchema.TABELLEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE Vorlauf IS NOT NULL OR Ruecklauf IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, ProzesswaermeTemperaturSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt
        /// aus derselben Quelle, hinter der Bodenalbedo.
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("ProzesswaermeTemperaturSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("AlbedoSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_PROZESSWAERME_TEMPERATUR = ProzesswaermeTemperaturSchema.SCHRITT", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_PROZESSWAERME_TEMPERATUR", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_ALBEDO", StringComparison.Ordinal));
            Assert.Contains("ProzesswaermeTemperaturSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("ProzesswaermeTemperaturSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("AlbedoSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Zeilen beider Tabellen ohne die neuen Spalten — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string tabelle in ProzesswaermeTemperaturSchema.TABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Typ, Monat_1, Monat_12, ReadOnly FROM \"" + tabelle + "\" ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                    liste.Add(tabelle + "|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static object Skalar(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }

        private static string Repowurzel()
        {
            string d = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && d != null; i++)
            {
                if (File.Exists(Path.Combine(d, "WP-Plan.sln"))) return d;
                d = Path.GetDirectoryName(d.TrimEnd(Path.DirectorySeparatorChar));
            }
            return null;
        }
    }
}
