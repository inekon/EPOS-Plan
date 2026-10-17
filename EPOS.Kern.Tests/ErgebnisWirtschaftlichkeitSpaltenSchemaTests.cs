using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// KB-8: der Schemaschritt der vier Ergebnisspalten der Wirtschaftlichkeit auf der Kopie der Testdatenbank —
    /// er legt die Spalten mit der Typübersetzung des früheren Laufzeitwegs an, nur wenn sie fehlen, und die Tabelle
    /// bleibt <c>STRICT</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErgebnisWirtschaftlichkeitSpaltenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const string TABELLE = ErgebnisWirtschaftlichkeitSpaltenSchema.TABELLE;

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        private static string Schema() => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT \"sql\" FROM sqlite_master WHERE \"type\" = 'table' AND \"name\" = ?", new DbParam("@t", TABELLE)),
            CultureInfo.InvariantCulture) ?? "";

        [Fact]
        public void Die_Nummer_und_die_Typen_stehen()
        {
            // KETTE (KB-8): vorlaeufig ueber + 2 an 238; nach dem Umhaengen auf 239 + 1.
            Assert.Equal(240, ErgebnisWirtschaftlichkeitSpaltenSchema.SCHRITT);
            Assert.Equal(TABELLE, WirtschaftlichkeitCtrl.TAB_ERGEBNIS);
            Assert.Equal(new[] { "StromsteuerBefreiungModus", "ErsatzBarwert", "Nachweis_Json", "Lauf_Staende" },
                         ErgebnisWirtschaftlichkeitSpaltenSchema.SPALTEN.Select(s => s.Name));
            Assert.Equal(new[]
            {
                "TEXT CHECK (length(\"StromsteuerBefreiungModus\") <= 20)",
                "REAL",
                "TEXT",
                "TEXT",
            }, ErgebnisWirtschaftlichkeitSpaltenSchema.SPALTEN.Select(s => s.Typ));
            // Dieselbe Schreibweise wie der fruehere Laufzeitweg (StilleDb.AlterTableAddColumn).
            Assert.Equal("ALTER TABLE [Tab_ErgebnisWirtschaftlichkeit] ADD COLUMN [ErsatzBarwert] REAL",
                         ErgebnisWirtschaftlichkeitSpaltenSchema.Anlegen(ErgebnisWirtschaftlichkeitSpaltenSchema.SPALTEN[1]));
        }

        [Fact]
        public void Der_Schritt_legt_die_fehlenden_Spalten_an_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ErgebnisWirtschaftlichkeitSpaltenSchema.Vollstaendig());
            long zeilen = Zahl("SELECT COUNT(*) FROM \"" + TABELLE + "\"");
            foreach (ErgebnisWirtschaftlichkeitSpaltenSchema.Spalte sp in ErgebnisWirtschaftlichkeitSpaltenSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + TABELLE + "\" DROP COLUMN \"" + sp.Name + "\"");
            Assert.Equal(4, ErgebnisWirtschaftlichkeitSpaltenSchema.Offen());

            var bericht = new List<string>();
            Assert.Equal(4, ErgebnisWirtschaftlichkeitSpaltenSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.StartsWith(TABELLE + ".Lauf_Staende angelegt", StringComparison.Ordinal));
            Assert.True(ErgebnisWirtschaftlichkeitSpaltenSchema.Vollstaendig());

            string sql = Schema();
            foreach (ErgebnisWirtschaftlichkeitSpaltenSchema.Spalte sp in ErgebnisWirtschaftlichkeitSpaltenSchema.SPALTEN)
            {
                Assert.Contains("[" + sp.Name + "] " + sp.Typ, sql, StringComparison.Ordinal);
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + TABELLE + "\" WHERE \"" + sp.Name + "\" IS NOT NULL"));
                Assert.Equal(0L, Zahl("SELECT \"notnull\" FROM pragma_table_info(?) WHERE \"name\" = ?",
                                      new DbParam("@t", TABELLE), new DbParam("@s", sp.Name)));
            }
            Assert.EndsWith("STRICT", sql.TrimEnd(), StringComparison.Ordinal);
            Assert.Equal(zeilen, Zahl("SELECT COUNT(*) FROM \"" + TABELLE + "\""));

            var zweiter = new List<string>();
            Assert.Equal(0, ErgebnisWirtschaftlichkeitSpaltenSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
            Assert.Equal(sql, Schema());
        }

        [Fact]
        public void Eine_vorhandene_Spalte_bleibt_unberuehrt()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("ALTER TABLE \"" + TABELLE + "\" DROP COLUMN \"" +
                                           WirtschaftlichkeitCtrl.SPALTE_LAUF_STAENDE + "\"");
            string vorher = Schema();
            Assert.Equal(1, ErgebnisWirtschaftlichkeitSpaltenSchema.Offen());

            Assert.Equal(1, ErgebnisWirtschaftlichkeitSpaltenSchema.Ausfuehren(null));
            // Die drei vorhandenen Spalten behalten ihre Definition; allein Lauf_Staende kommt hinzu.
            Assert.StartsWith(vorher.Substring(0, vorher.IndexOf(", FOREIGN KEY", StringComparison.Ordinal)), Schema(),
                              StringComparison.Ordinal);
            Assert.True(ErgebnisWirtschaftlichkeitSpaltenSchema.Vollstaendig());
        }

        [Fact]
        public void Der_Modus_haelt_die_Laengengrenze_nach_dem_Anlegen()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT COALESCE(MIN(rowid), 0) FROM \"" + TABELLE + "\"");
            if (id == 0) return;
            DataRepository.ExecuteNonQuery("ALTER TABLE \"" + TABELLE + "\" DROP COLUMN \"" +
                                           WirtschaftlichkeitCtrl.SPALTE_STROMST_MODUS + "\"");
            ErgebnisWirtschaftlichkeitSpaltenSchema.Ausfuehren(null);
            DataRepository.ExecuteNonQuery("UPDATE \"" + TABELLE + "\" SET \"StromsteuerBefreiungModus\" = ? WHERE rowid = ?",
                                           new DbParam("@m", "ERLOES"), new DbParam("@i", id));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"" + TABELLE + "\" WHERE \"StromsteuerBefreiungModus\" = 'ERLOES'"));
            Assert.ThrowsAny<Exception>(() =>
            {
                using DbVorgang v = DataRepository.Vorgang();
                v.Ausfuehren("UPDATE \"" + TABELLE + "\" SET \"StromsteuerBefreiungModus\" = ? WHERE rowid = ?",
                             new DbParam("@m", new string('X', 21)), new DbParam("@i", id));
                v.Commit();
            });
        }
    }
}
