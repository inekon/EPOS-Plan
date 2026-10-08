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
    /// <b>Der Schemaschritt S1 von AK3-K</b> (<see cref="Ak3KSchema"/>; Entwurf AK3-K 3.5, Festlegung 20): die
    /// Kennzahlen der Zonensperre und der Kälteseite im Kreis an <c>Tab_ErgebnisEnergiebedarf</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class Ak3KSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_haengt_an_der_Vorgaengerklasse_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(ProjektdateiImportSchema.SCHRITT + 1, Ak3KSchema.SCHRITT);
            Assert.Equal(201, Ak3KSchema.SCHRITT);
            Assert.Equal(Ak3KSchema.SCHRITT, SchemaStand.Zielversion);
            Assert.True(Ak3KSchema.SCHRITT <= SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == Ak3KSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                {
                    "Zonensperre_Tage", "Zonensperre_Heizen_Gesperrt_MWh", "Zonensperre_Kuehlen_Gesperrt_MWh",
                    "Ak3_Kaelteschranke_Stunden", "Ak3_Umschalt_Stunden", "Ak3_Kaelterest_Stunden", "Ak3_Kaelterest_MWh",
                },
                Ak3KSchema.SPALTEN_ERGEBNIS.ToArray());
            Assert.All(Ak3KSchema.SPALTEN, x => Assert.Equal("Tab_ErgebnisEnergiebedarf", x.Tabelle));
            Assert.Equal(Ak3KSchema.SPALTEN_ERGEBNIS, Ak3KSchema.SPALTEN.Select(x => x.Spalte));
            // Kein Name doppelt mit dem Vorgängerschritt.
            Assert.Empty(Ak3KSchema.SPALTEN_ERGEBNIS.Intersect(Ak3Schema.SPALTEN_ERGEBNIS));
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: Stunden 0 bis 8760, Tage und MWh nicht negativ.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_die_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach ((string _, string spalte, string typ) in Ak3KSchema.SPALTEN)
                Ausfuehren(c, Ak3KSchema.Anlegen(("T", spalte, typ)));
            Assert.False(Wirft(c, "UPDATE T SET Zonensperre_Tage = 730, Zonensperre_Heizen_Gesperrt_MWh = 1.5, " +
                                  "Zonensperre_Kuehlen_Gesperrt_MWh = 0, Ak3_Kaelteschranke_Stunden = 8760, Ak3_Umschalt_Stunden = 0, " +
                                  "Ak3_Kaelterest_Stunden = 12, Ak3_Kaelterest_MWh = 0.25"));
            Assert.False(Wirft(c, "UPDATE T SET " + string.Join(", ", Ak3KSchema.SPALTEN_ERGEBNIS.Select(s => s + " = NULL"))));
            Assert.True(Wirft(c, "UPDATE T SET Zonensperre_Tage = -1"));
            Assert.True(Wirft(c, "UPDATE T SET Zonensperre_Heizen_Gesperrt_MWh = -0.1"));
            Assert.True(Wirft(c, "UPDATE T SET Zonensperre_Kuehlen_Gesperrt_MWh = 'abc'"));
            Assert.True(Wirft(c, "UPDATE T SET Ak3_Kaelteschranke_Stunden = 8761"));
            Assert.True(Wirft(c, "UPDATE T SET Ak3_Umschalt_Stunden = -1"));
            Assert.True(Wirft(c, "UPDATE T SET Ak3_Kaelterest_Stunden = 9000"));
            Assert.True(Wirft(c, "UPDATE T SET Ak3_Kaelterest_MWh = -1"));
        }

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= Ak3KSchema.SCHRITT);
            Assert.True(Ak3KSchema.Vollstaendig());
            foreach (string s in Ak3KSchema.SPALTEN_ERGEBNIS)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE \"" + s + "\" IS NOT NULL"));
        }

        /// <summary>Rundreise: aus dem Stand davor legt der Schritt alle sieben Spalten an; zweimal = nichts.</summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach ((string tabelle, string spalte, string _) in Ak3KSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE " + tabelle + " DROP COLUMN " + spalte);
            long zeilen = Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf");
            Assert.False(Ak3KSchema.Vollstaendig());
            Assert.True(Ak3Schema.Vollstaendig());

            var bericht = new List<string>();
            Assert.Equal(7, Ak3KSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(Ak3KSchema.Vollstaendig());
            Assert.Equal(zeilen, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            Assert.Equal(0, Ak3KSchema.Ausfuehren(null));
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }
    }
}
