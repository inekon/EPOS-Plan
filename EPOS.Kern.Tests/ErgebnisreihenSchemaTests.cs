using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>EB-1: der Schemaschritt 239 der Ergebnisreihen auf der Kopie der Testdatenbank.</summary>
    [Collection("Testdatenbank")]
    public class ErgebnisreihenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_und_das_Ziel_liegt_nicht_davor()
        {
            // KETTE: 239 haengt ueber + 1 an 238 (KlimaRohwerteSchema, KD-4).
            Assert.Equal(239, ErgebnisreihenSchema.SCHRITT);
            Assert.Equal(KlimaRohwerteSchema.SCHRITT + 1, ErgebnisreihenSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ErgebnisreihenSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == ErgebnisreihenSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Equal(new[] { "Stromeinspeisung", "Abregelung", "Eigenverbrauch_System" }, ErgebnisreihenSchema.SPALTEN.Select(x => x.Name));
            Assert.Equal(new[] { "Tab_ErgebnisBHKW", "Tab_ErgebnisPhotovoltaik", "Tab_ErgebnisStromspeicher" }, ErgebnisreihenSchema.TABELLEN);
            Assert.Equal("REAL CHECK (\"Abregelung\" IS NULL OR \"Abregelung\" >= 0)", ErgebnisreihenSchema.Typ(ErgebnisreihenSchema.SPALTE_PV_ABREGELUNG));
        }

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ErgebnisreihenSchema.Vollstaendig());
            Assert.True(ErgebnisreihenSchema.Lesbar());
            Assert.Equal((long)SchemaStand.Zielversion, Zahl("SELECT \"SchemaVersion\" FROM \"Tab_Applikation\""));
        }

        [Fact]
        public void Der_Schritt_legt_die_Spalten_leer_an_mit_Check_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ErgebnisreihenSchema.Vollstaendig());
            foreach (ErgebnisreihenSchema.Spalte sp in ErgebnisreihenSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + sp.Tabelle + "\" DROP COLUMN \"" + sp.Name + "\"");
            ErgebnisreihenSchema.ProbeVerwerfen();
            Assert.Equal(3, ErgebnisreihenSchema.Offen());
            Assert.False(ErgebnisreihenSchema.Lesbar());

            var bericht = new List<string>();
            Assert.Equal(3, ErgebnisreihenSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_ErgebnisBHKW.Stromeinspeisung angelegt", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("Tab_ErgebnisStromspeicher.Eigenverbrauch_System angelegt", StringComparison.Ordinal));
            Assert.True(ErgebnisreihenSchema.Vollstaendig());
            Assert.True(ErgebnisreihenSchema.Lesbar());
            foreach (ErgebnisreihenSchema.Spalte sp in ErgebnisreihenSchema.SPALTEN)
            {
                string sql = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT \"sql\" FROM sqlite_master WHERE \"type\" = 'table' AND \"name\" = ?", new DbParam("@t", sp.Tabelle)),
                    CultureInfo.InvariantCulture) ?? "";
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + sp.Tabelle + "\" WHERE \"" + sp.Name + "\" IS NOT NULL"));
                Assert.Contains("\"" + sp.Name + "\" " + sp.Typ, sql, StringComparison.Ordinal);
                Assert.EndsWith("STRICT", sql.TrimEnd(), StringComparison.Ordinal);
            }
            var zweiter = new List<string>();
            Assert.Equal(0, ErgebnisreihenSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Der_Check_weist_negative_Werte_ab()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT COALESCE(MIN(\"ID\"), 0) FROM \"Tab_ErgebnisBHKW\"");
            if (id == 0) return;
            foreach (double erlaubt in new[] { 0.0, 18.07 })
            {
                DataRepository.ExecuteNonQuery(
                    "UPDATE \"Tab_ErgebnisBHKW\" SET \"Stromeinspeisung\" = ? WHERE \"ID\" = ?",
                    new DbParam("@a", DbParamTyp.Double) { Wert = erlaubt }, new DbParam("@i", DbParamTyp.Integer) { Wert = id });
                object gelesen = DataRepository.ExecuteScalar(
                    "SELECT \"Stromeinspeisung\" FROM \"Tab_ErgebnisBHKW\" WHERE \"ID\" = ?", new DbParam("@i", DbParamTyp.Integer) { Wert = id });
                Assert.Equal(erlaubt, Convert.ToDouble(gelesen, CultureInfo.InvariantCulture), 9);
            }
            Assert.ThrowsAny<Exception>(() =>
            {
                using DbVorgang v = DataRepository.Vorgang();
                v.Ausfuehren("UPDATE \"Tab_ErgebnisBHKW\" SET \"Stromeinspeisung\" = ? WHERE \"ID\" = ?",
                             new DbParam("@a", -0.1), new DbParam("@i", id));
                v.Commit();
            });
        }
    }
}
