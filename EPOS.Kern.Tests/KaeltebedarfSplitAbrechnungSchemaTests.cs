using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt 223 — die Ergebnistabelle je Split-Zeile</b> (<see cref="KaeltebedarfSplitAbrechnungSchema"/>): Nummer
    /// in der Kette (222 + 1), Stufe der Paketanhebung, Anlage aus einem Stand 222 als STRICT-Tabelle mit den Prüfklauseln,
    /// Wiederholbarkeit, kein Verweis an Zuordnung und Kühlträger, und die Testdatenbank auf dem Schritt ohne Zeile (kein
    /// Referenzprojekt führt eine Split-Zeile).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltebedarfSplitAbrechnungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static string Sql() => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
            new DbParam("?", KaeltebedarfSplitAbrechnungSchema.TAB_ERGEBNIS)), CultureInfo.InvariantCulture);

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static int Kopf() => Convert.ToInt32(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Ergebnis"),
                                                     CultureInfo.InvariantCulture);

        private static bool Einfuegen(string quelle, object zaehler, object strom)
        {
            try
            {
                // -1 = abgelehnt (der Zugriff meldet still und fängt), sonst eine eingefügte Zeile.
                return 1 == DataRepository.ExecuteNonQuery(
                    "INSERT INTO " + KaeltebedarfSplitAbrechnungSchema.TAB_ERGEBNIS +
                    " (ID_Ergebnis, Quelle, ID_Zuordnung, Bezeichner, Strom_MWh, Kuehl_ID_Carrier, Kuehl_EigenerZaehler) " +
                    "VALUES (?, ?, 999999, 'Probe', ?, 999999, ?)",
                    new DbParam("?", Kopf()), new DbParam("?", quelle), new DbParam("?", strom), new DbParam("?", zaehler));
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Fact]
        public void Nummer_und_Spalten()
        {
            Assert.Equal(223, KaeltebedarfSplitAbrechnungSchema.SCHRITT);
            Assert.Equal("Tab_ErgebnisKaelteDezentral", KaeltebedarfSplitAbrechnungSchema.TAB_ERGEBNIS);
            Assert.Equal(11, KaeltebedarfSplitAbrechnungSchema.SPALTEN.Count);
            Assert.Equal(KaeltebedarfSplitAbrechnungSchema.SPALTEN.Count,
                         KaeltebedarfSplitAbrechnungSchema.SPALTEN.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            string sql = KaeltebedarfSplitAbrechnungSchema.SqlCreate();
            Assert.EndsWith(") STRICT", sql);
            Assert.Contains("CHECK (\"Quelle\" IN ('lastgang','profil'))", sql);
            Assert.Contains("CHECK (\"Kuehl_EigenerZaehler\" IN (0,1))", sql);
            Assert.Contains("REFERENCES \"Tab_Ergebnis\" (\"ID\") ON DELETE CASCADE", sql);
            Assert.Equal(1, sql.Split(new[] { "REFERENCES" }, StringSplitOptions.None).Length - 1);   // nur am Lauf
            Assert.Equal(new[] { "Tab_Ergebnis" }, KaeltebedarfSplitAbrechnungSchema.Voraussetzungen());
        }

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_steht_im_Ziel_und_die_Paketanhebung_fuehrt_Ddl()
        {
            Assert.Equal(BauteilkatalogHaustechnikSchema.SCHRITT + 1, KaeltebedarfSplitAbrechnungSchema.SCHRITT);
            Assert.True(KaeltebedarfSplitAbrechnungSchema.SCHRITT <= SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Assert.Single(Paketanhebung.Stufen, x => x.Nr == KaeltebedarfSplitAbrechnungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Contains("Split", s.Text, StringComparison.Ordinal);
            Assert.True(Paketanhebung.Vorschauen(BauteilkatalogHaustechnikSchema.SCHRITT).Noetig);    // ein Paket 222 wird angehoben
        }

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_ohne_Zeile()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KaeltebedarfSplitAbrechnungSchema.SCHRITT);
            Assert.True(KaeltebedarfSplitAbrechnungSchema.Vollstaendig());
            Assert.Equal(0, KaeltebedarfSplitAbrechnungSchema.Ausfuehren(null));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + KaeltebedarfSplitAbrechnungSchema.TAB_ERGEBNIS));
            Assert.Equal(KaeltebedarfSplitAbrechnungSchema.SqlCreate().Replace("CREATE TABLE IF NOT EXISTS", "CREATE TABLE"), Sql());
        }

        [Fact]
        public void Der_Schritt_hebt_einen_Stand_222_an_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            // Stand 222 nachstellen: ohne die Tabelle liest der Lauf still keine Split-Zeile.
            Assert.True(DataRepository.ExecuteSQL("DROP TABLE " + KaeltebedarfSplitAbrechnungSchema.TAB_ERGEBNIS));
            Assert.False(KaeltebedarfSplitAbrechnungSchema.TabelleVorhanden());
            Assert.False(KaeltebedarfSplitAbrechnungSchema.Vollstaendig());
            int projekt = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_Projekt FROM Tab_Ergebnis ORDER BY ID LIMIT 1"),
                                          CultureInfo.InvariantCulture);
            ErgebnisModel m = new ErgebnisCtrl().Load(projekt);
            Assert.NotNull(m);
            Assert.Empty(m.KaelteDezentral);

            var bericht = new List<string>();
            Assert.Equal(1, KaeltebedarfSplitAbrechnungSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, b => b.Contains("angelegt", StringComparison.Ordinal));
            Assert.True(KaeltebedarfSplitAbrechnungSchema.Vollstaendig());
            Assert.EndsWith(") STRICT", Sql().TrimEnd(), StringComparison.Ordinal);
            Assert.Equal(0, KaeltebedarfSplitAbrechnungSchema.Ausfuehren(null));                // zweiter Lauf: 0 Handgriffe
            Assert.Empty(new ErgebnisCtrl().Load(projekt).KaelteDezentral);
        }

        [Fact]
        public void Anlage_ist_wiederholbar_STRICT_und_haelt_die_Pruefklauseln()
        {
            if (!_db.Vorhanden) return;
            // Die Testdatenbank führt keine Split-Zeile - ob die Tabelle schon steht oder nicht.
            if (KaeltebedarfSplitAbrechnungSchema.TabelleVorhanden())
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + KaeltebedarfSplitAbrechnungSchema.TAB_ERGEBNIS), CultureInfo.InvariantCulture));
            else
                Assert.False(KaeltebedarfSplitAbrechnungSchema.Vollstaendig());

            var bericht = new List<string>();
            int erst = KaeltebedarfSplitAbrechnungSchema.Ausfuehren(bericht);
            Assert.True(erst <= 1);
            Assert.True(KaeltebedarfSplitAbrechnungSchema.Vollstaendig());
            Assert.Equal(0, KaeltebedarfSplitAbrechnungSchema.Ausfuehren(bericht));       // zweiter Lauf: 0 Handgriffe
            Assert.Contains(bericht, b => b.Contains("byte-gleich"));
            Assert.EndsWith("STRICT", Sql().TrimEnd());

            Assert.True(Einfuegen(KaeltebedarfSplitAbrechnungSchema.QUELLE_PROFIL, 1, 1.5));
            Assert.True(Einfuegen(KaeltebedarfSplitAbrechnungSchema.QUELLE_LASTGANG, DBNull.Value, 0.5));
            Assert.False(Einfuegen("gebaeude", 0, 1.0));                                     // unbekannte Quelle
            Assert.False(Einfuegen(KaeltebedarfSplitAbrechnungSchema.QUELLE_PROFIL, 2, 1.0)); // Zähler nur 0/1
            Assert.False(Einfuegen(KaeltebedarfSplitAbrechnungSchema.QUELLE_PROFIL, 0, "viel")); // STRICT: REAL
        }
    }
}
