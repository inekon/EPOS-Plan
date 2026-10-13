using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schemaschritt DA-2</b> (<see cref="ZonenLastfelderSchema"/>; Entscheid E138 F5): Nummer und Kette, die Testkopie auf
    /// dem Schritt (drei leere Spalten an Zone und Katalogzone, keine Referenzzone mit Wert), der Lauf über einen Stand vor dem
    /// Schritt, die Prüfklauseln und die Wiederholbarkeit. Werte der Proben sind runde Phantasiewerte.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZonenLastfelderSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_Ddl()
        {
            Assert.Equal(BauteilkatalogHaustechnikSchema.SCHRITT + 1, ZonenLastfelderSchema.SCHRITT);
            Assert.Equal(ZonenLastfelderSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == ZonenLastfelderSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Die_Spalten_sind_nullbar_und_tragen_ihre_Pruefklausel()
        {
            Assert.Equal(new[] { "Aussenluft_Person", "Licht_W_m2", "Personen_Waerme_W" }, ZonenLastfelderSchema.SPALTEN.Select(s => s.Spalte));
            Assert.Equal(new[] { "Tab_Zone", "Tab_Zone_STAMM" }, ZonenLastfelderSchema.TABELLEN);
            Assert.All(ZonenLastfelderSchema.SPALTEN, s => Assert.Contains("IS NULL OR", s.Typ, StringComparison.Ordinal));
            Assert.All(ZonenLastfelderSchema.SPALTEN, s => Assert.DoesNotContain("NOT NULL", s.Typ, StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_und_keine_Zone_traegt_einen_Wert()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= ZonenLastfelderSchema.SCHRITT);
            Assert.True(ZonenLastfelderSchema.Vollstaendig());
            Assert.True(ZonenLastfelderSchema.Lesbar());
            foreach (string t in ZonenLastfelderSchema.TABELLEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + t + " WHERE Aussenluft_Person IS NOT NULL OR Licht_W_m2 IS NOT NULL " +
                                      "OR Personen_Waerme_W IS NOT NULL"));
        }

        [Fact]
        public void Der_Schritt_legt_die_Spalten_an_ohne_eine_Zeile_zu_aendern_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            long zonen = Zahl("SELECT COUNT(*) FROM Tab_Zone");
            foreach (string t in ZonenLastfelderSchema.TABELLEN)
                foreach ((string spalte, string _) in ZonenLastfelderSchema.SPALTEN.Reverse())
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + t + "\" DROP COLUMN \"" + spalte + "\"");
            Assert.False(ZonenLastfelderSchema.Vollstaendig());
            Assert.Equal(6, ZonenLastfelderSchema.Offen());

            var bericht = new List<string>();
            Assert.Equal(6, ZonenLastfelderSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Zone_STAMM.Licht_W_m2 angelegt", StringComparison.Ordinal));
            Assert.True(ZonenLastfelderSchema.Vollstaendig());
            Assert.Equal(zonen, Zahl("SELECT COUNT(*) FROM Tab_Zone"));

            var zweiter = new List<string>();
            Assert.Equal(0, ZonenLastfelderSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Pruefklauseln_weisen_negative_Werte_ab()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT MIN(ID) FROM Tab_Zone");
            Assert.Equal(1, Setzen("Aussenluft_Person", 30.0, id));
            Assert.Equal(1, Setzen("Licht_W_m2", 10.0, id));
            Assert.Equal(1, Setzen("Personen_Waerme_W", 80.0, id));
            Assert.ThrowsAny<Exception>(() => Setzen("Aussenluft_Person", -1.0, id));
            Assert.ThrowsAny<Exception>(() => Setzen("Licht_W_m2", -1.0, id));
            Assert.ThrowsAny<Exception>(() => Setzen("Personen_Waerme_W", -1.0, id));
        }

        private static int Setzen(string spalte, object wert, long id)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                int n = v.Ausfuehren("UPDATE Tab_Zone SET \"" + spalte + "\" = ? WHERE ID = ?",
                                     new DbParam("@w", wert), new DbParam("@id", id));
                v.Commit();
                return n;
            }
        }

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
