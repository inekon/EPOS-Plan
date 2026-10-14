using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schemaschritt NP7</b> (<see cref="RaumnutzungNormwerteSchema"/>; Entscheid E131): Nummer und Kette, die
    /// Testkopie auf dem Schritt (vier leere Spalten), der Lauf über einen Stand vor dem Schritt, die Prüfklauseln und die
    /// Wiederholbarkeit. Werte der Proben sind runde Phantasiewerte.
    /// </summary>
    [Collection("Testdatenbank")]
    public class RaumnutzungNormwerteSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_Katalog()
        {
            Assert.Equal(220, RaumnutzungNormwerteSchema.SCHRITT);
            Assert.Equal(RaumnutzungNormkategorienSchema.SCHRITT + 1, RaumnutzungNormwerteSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= RaumnutzungNormwerteSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == RaumnutzungNormwerteSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Die_Spalten_sind_nullbar_und_tragen_ihre_Pruefklausel()
        {
            Assert.Equal(new[] { "Aussenluft_Person", "Werte_Herkunft", "Werte_Quelle", "Werte_Datum" },
                         RaumnutzungNormwerteSchema.SPALTEN.Select(s => s.Spalte));
            Assert.All(RaumnutzungNormwerteSchema.SPALTEN, s => Assert.Contains("IS NULL OR", s.Typ, StringComparison.Ordinal));
            Assert.All(RaumnutzungNormwerteSchema.SPALTEN, s => Assert.DoesNotContain("NOT NULL", s.Typ, StringComparison.Ordinal));
            Assert.DoesNotContain(RaumnutzungSchema.SPALTEN_KENNWERTE, k => k.Spalte == RaumnutzungNormwerteSchema.SPALTE_AUSSENLUFT_PERSON);
        }

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_mit_leeren_Spalten()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= RaumnutzungNormwerteSchema.SCHRITT);
            Assert.True(RaumnutzungNormwerteSchema.Vollstaendig());
            Assert.True(RaumnutzungNormwerteSchema.Lesbar());
            Assert.Equal(0, RaumnutzungNormwerteSchema.Offen());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE Aussenluft_Person IS NOT NULL " +
                                  "OR Werte_Herkunft IS NOT NULL OR Werte_Quelle IS NOT NULL OR Werte_Datum IS NOT NULL"));
        }

        [Fact]
        public void Der_Schritt_legt_die_Spalten_an_ohne_eine_Zeile_zu_aendern_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            long profile = Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil");
            foreach ((string spalte, string _) in RaumnutzungNormwerteSchema.SPALTEN.Reverse())
                DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Raumnutzungsprofil DROP COLUMN \"" + spalte + "\"");
            Assert.False(RaumnutzungNormwerteSchema.Vollstaendig());
            Assert.Equal(4, RaumnutzungNormwerteSchema.Offen());

            var bericht = new List<string>();
            Assert.Equal(4, RaumnutzungNormwerteSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Raumnutzungsprofil.Aussenluft_Person angelegt", StringComparison.Ordinal));
            Assert.True(RaumnutzungNormwerteSchema.Vollstaendig());
            Assert.Equal(profile, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil"));

            var zweiter = new List<string>();
            Assert.Equal(0, RaumnutzungNormwerteSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Pruefklauseln_weisen_ungueltige_Werte_ab()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT MIN(ID) FROM Tab_Raumnutzungsprofil");
            Assert.Equal(1, Setzen("Aussenluft_Person", 30.0, id));
            Assert.Equal(1, Setzen("Werte_Herkunft", RaumnutzungNormwerteSchema.HERKUNFT_EINGELESEN, id));
            Assert.Equal(1, Setzen("Werte_Datum", "2026-10-12", id));
            Assert.ThrowsAny<Exception>(() => Setzen("Aussenluft_Person", -1.0, id));
            Assert.ThrowsAny<Exception>(() => Setzen("Werte_Herkunft", "HAND", id));
            Assert.ThrowsAny<Exception>(() => Setzen("Werte_Datum", "12.10.2026", id));
            Assert.ThrowsAny<Exception>(() => Setzen("Werte_Quelle", new string('x', RaumnutzungNormwerteSchema.QUELLE_MAX_ZEICHEN + 1), id));
        }

        private static int Setzen(string spalte, object wert, long id)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                int n = v.Ausfuehren("UPDATE Tab_Raumnutzungsprofil SET \"" + spalte + "\" = ? WHERE ID = ?",
                                     new DbParam("@w", wert), new DbParam("@id", id));
                v.Commit();
                return n;
            }
        }

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
