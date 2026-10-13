using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt 224 — die Ergebnistabelle je Split-Zeile</b> (<see cref="KaeltebedarfSplitAbrechnungSchema"/>): Nummer,
    /// Anlage als STRICT-Tabelle mit den Prüfklauseln, Wiederholbarkeit, kein Verweis an Zuordnung und Kühlträger, und in der
    /// Testdatenbank keine Zeile (kein Referenzprojekt führt eine Split-Zeile).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltebedarfSplitAbrechnungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static string Sql() => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
            new DbParam("?", KaeltebedarfSplitAbrechnungSchema.TAB_ERGEBNIS)), CultureInfo.InvariantCulture);

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
            Assert.Equal(224, KaeltebedarfSplitAbrechnungSchema.SCHRITT);
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

        [Fact]
        public void Ohne_Tabelle_bleibt_die_Liste_des_Laufs_leer()
        {
            if (!_db.Vorhanden) return;
            if (KaeltebedarfSplitAbrechnungSchema.TabelleVorhanden()) return;   // nach dem Einhängen trägt die Testdatenbank sie
            int projekt = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_Projekt FROM Tab_Ergebnis ORDER BY ID LIMIT 1"),
                                          CultureInfo.InvariantCulture);
            ErgebnisModel m = new ErgebnisCtrl().Load(projekt);
            Assert.NotNull(m);
            Assert.Empty(m.KaelteDezentral);
        }
    }
}
