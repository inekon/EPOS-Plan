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
    /// <b>Der Schemaschritt der pflegbaren Kältefolge</b> (<see cref="KaelteRangSchema"/>; Welle KB-D, Entscheid E117 F1):
    /// <c>Tab_Energieanlagen.Kaelte_Rang</c>, INTEGER ≥ 1, NULL = Vorgabefolge; reines DDL, wiederholbar, ergebnisneutral.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaelteRangSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_ist_212_das_Ziel_und_die_Paketanhebung_fuehrt_Ddl()
        {
            Assert.Equal(212, KaelteRangSchema.SCHRITT);
            Assert.Equal(KaelteKatalogfelderSchema.SCHRITT + 1, KaelteRangSchema.SCHRITT);
            Assert.Equal(KaelteRangSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KaelteRangSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Der_Schritt_fuehrt_eine_nullbare_Spalte_an_der_Anlagenzeile()
        {
            (string tabelle, string spalte, string typ) = Assert.Single(KaelteRangSchema.SPALTEN);
            Assert.Equal("Tab_Energieanlagen", tabelle);
            Assert.Equal("Kaelte_Rang", spalte);
            Assert.StartsWith("INTEGER CHECK", typ, StringComparison.Ordinal);
            Assert.DoesNotContain("NOT NULL", typ, StringComparison.Ordinal);
            Assert.DoesNotContain("DEFAULT", typ, StringComparison.Ordinal);
        }

        [Fact]
        public void Die_Pruefklausel_haelt_den_Rang_ab_eins()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Energieanlagen\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO \"Tab_Energieanlagen\" (ID) VALUES (1)");
            Ausfuehren(c, KaelteRangSchema.Anlegen(KaelteRangSchema.SPALTEN[0]));
            foreach (string gut in new[] { "NULL", "1", "2", "40" })
                Ausfuehren(c, "UPDATE Tab_Energieanlagen SET Kaelte_Rang = " + gut);
            foreach (string schlecht in new[] { "0", "-1", "'x'", "1.5" })
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Tab_Energieanlagen SET Kaelte_Rang = " + schlecht));
        }

        /// <summary>Die Testkopie steht auf dem Schritt, ohne einen gepflegten Rang (die Basis bleibt byte-gleich).</summary>
        [Fact]
        public void Die_Testkopie_steht_auf_212_ohne_Rang()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KaelteRangSchema.SCHRITT);
            Assert.True(KaelteRangSchema.Vollstaendig());
            Assert.Equal(0, KaelteRangSchema.ZeilenMitRang());
        }

        [Fact]
        public void Der_Schritt_legt_die_Spalte_leer_an_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            long anlagen = Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen");
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Energieanlagen\" DROP COLUMN \"Kaelte_Rang\"");
            Assert.False(KaelteRangSchema.Vollstaendig());
            Assert.Empty(Kaeltefolge.RaengeLesen(1017));     // ohne Spalte: kein Rang, die Vorgabefolge

            var bericht = new List<string>();
            Assert.Equal(1, KaelteRangSchema.Ausfuehren(bericht));
            Assert.True(KaelteRangSchema.Vollstaendig());
            Assert.Equal(0, KaelteRangSchema.ZeilenMitRang());
            Assert.Equal(anlagen, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            var zweiter = new List<string>();
            Assert.Equal(0, KaelteRangSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("nichts zu tun", StringComparison.Ordinal));
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
