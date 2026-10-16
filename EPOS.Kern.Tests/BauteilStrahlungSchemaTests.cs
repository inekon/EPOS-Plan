using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>DA-8: der Schemaschritt der Strahlungswerte je Aufbau und Bauteil auf der Kopie der Testdatenbank.</summary>
    [Collection("Testdatenbank")]
    public class BauteilStrahlungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_und_ist_das_Ziel()
        {
            // Keine feste Nummer: vorläufig über + 1 an MA-6b (nach Schritt 236 umhängen).
            Assert.Equal(BauteilUHerkunftSchema.SCHRITT + 1, BauteilStrahlungSchema.SCHRITT);
            Assert.Equal(BauteilStrahlungSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == BauteilStrahlungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Equal(new[] { "Absorptionsgrad_Solar", "Emissionsgrad" }, BauteilStrahlungSchema.SPALTEN.Select(x => x.Name));
            Assert.Equal(new[] { "Tab_Bauteilaufbau_STAMM", "Tab_Bauteilaufbau", "Tab_Bauteil", "Tab_Bauteil_STAMM" },
                         BauteilStrahlungSchema.TABELLEN);
            Assert.Equal("REAL CHECK (\"Emissionsgrad\" IS NULL OR (\"Emissionsgrad\" >= 0 AND \"Emissionsgrad\" <= 1))",
                         BauteilStrahlungSchema.Typ(BauteilStrahlungSchema.SPALTE_EMISSION));
        }

        [Fact]
        public void Der_Schritt_legt_die_Spalten_leer_an_mit_Check_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(BauteilStrahlungSchema.Vollstaendig());
            foreach (string t in BauteilStrahlungSchema.TABELLEN)
                foreach (BauteilkatalogSchema.Spalte sp in BauteilStrahlungSchema.SPALTEN)
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + t + "\" DROP COLUMN \"" + sp.Name + "\"");
            BauteilStrahlungSchema.ProbeVerwerfen();
            Assert.Equal(8, BauteilStrahlungSchema.Offen());
            Assert.False(BauteilStrahlungSchema.Lesbar());

            var bericht = new List<string>();
            Assert.Equal(8, BauteilStrahlungSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Bauteil.Emissionsgrad angelegt", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("Tab_Bauteilaufbau_STAMM.Absorptionsgrad_Solar angelegt", StringComparison.Ordinal));
            Assert.True(BauteilStrahlungSchema.Vollstaendig());
            Assert.True(BauteilStrahlungSchema.Lesbar());
            foreach (string t in BauteilStrahlungSchema.TABELLEN)
            {
                string sql = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT \"sql\" FROM sqlite_master WHERE \"type\" = 'table' AND \"name\" = ?", new DbParam("@t", t)),
                    CultureInfo.InvariantCulture) ?? "";
                foreach (BauteilkatalogSchema.Spalte sp in BauteilStrahlungSchema.SPALTEN)
                {
                    Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"" + sp.Name + "\" IS NOT NULL"));
                    Assert.Contains("\"" + sp.Name + "\" " + sp.Typ, sql, StringComparison.Ordinal);
                }
                Assert.EndsWith("STRICT", sql.TrimEnd(), StringComparison.Ordinal);
            }
            var zweiter = new List<string>();
            Assert.Equal(0, BauteilStrahlungSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Der_Check_weist_Werte_ausserhalb_null_bis_eins_ab()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT COALESCE(MIN(\"ID\"), 0) FROM \"Tab_Bauteil\"");
            if (id == 0) return;
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_Bauteil\" SET \"Absorptionsgrad_Solar\" = ?, \"Emissionsgrad\" = ? WHERE \"ID\" = ?",
                new DbParam("@a", 0.35), new DbParam("@e", 1.0), new DbParam("@i", id));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil\" WHERE \"Emissionsgrad\" = 1.0 AND \"ID\" = ?", new DbParam("@i", id)));
            foreach (double falsch in new[] { -0.1, 1.2 })
                Assert.ThrowsAny<Exception>(() =>
                {
                    using DbVorgang v = DataRepository.Vorgang();
                    v.Ausfuehren("UPDATE \"Tab_Bauteil\" SET \"Absorptionsgrad_Solar\" = ? WHERE \"ID\" = ?",
                                 new DbParam("@a", falsch), new DbParam("@i", id));
                    v.Commit();
                });
        }
    }
}
