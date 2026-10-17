using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ferienliste des Betriebskalenders — der Schemaschritt</b> (<see cref="BetriebskalenderFerienlisteSchema"/>,
    /// Anwenderentscheid 17.10.2026): Nummer, Kette und Register, dazu Tabelle, Spalten, Prüfklauseln und Index auf einer
    /// frisch migrierten Kopie der Testdatenbank. Die Wege von Modell und Controller hält
    /// <c>BetriebskalenderFerienlisteTests</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BetriebskalenderFerienlisteSchemaTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        [Fact]
        public void Nummer_Kette_und_Register()
        {
            // KETTE: 241 haengt ueber + 1 an 240 (ErgebnisWirtschaftlichkeitSpaltenSchema, KB-8) — vorlaeufig + 3 an 238
            // (KlimaRohwerteSchema), bis 239 und 240 auf origin liegen; dann umhaengen.
            Assert.Equal(241, BetriebskalenderFerienlisteSchema.SCHRITT);
            Assert.Equal(KlimaRohwerteSchema.SCHRITT + 3, BetriebskalenderFerienlisteSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= BetriebskalenderFerienlisteSchema.SCHRITT);
            Assert.Contains(Paketanhebung.Stufen,
                            s => s.Nr == BetriebskalenderFerienlisteSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
            Assert.Equal(new[] { BedarfNetzKalenderSchema.TAB_KALENDER }, BetriebskalenderFerienlisteSchema.Voraussetzungen());
        }

        [Fact]
        public void Tabelle_Spalten_und_Index_stehen_leer_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(BetriebskalenderFerienlisteSchema.Vollstaendig());
            string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                new DbParam("?", BetriebskalenderFerienlisteSchema.TAB_FERIEN)), CultureInfo.InvariantCulture);
            Assert.EndsWith("STRICT", ddl.Trim());
            Assert.Contains("REFERENCES \"Tab_Betriebskalender\" (\"ID\") ON DELETE CASCADE", ddl);
            Assert.Contains("UNIQUE (\"ID_Betriebskalender\", \"Reihenfolge\")", ddl);
            foreach (string sp in new[] { "ID", BetriebskalenderFerienlisteSchema.SPALTE_KALENDER,
                                          BetriebskalenderFerienlisteSchema.SPALTE_REIHENFOLGE, BetriebskalenderFerienlisteSchema.SPALTE_NAME,
                                          BetriebskalenderFerienlisteSchema.SPALTE_BEGINN, BetriebskalenderFerienlisteSchema.SPALTE_ENDE })
                Assert.True(DataRepository.SpalteVorhanden(BetriebskalenderFerienlisteSchema.TAB_FERIEN, sp), sp);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Betriebskalenderferien"));

            // Wiederholbar: ein zweiter Lauf legt nichts an.
            Assert.Equal(0, BetriebskalenderFerienlisteSchema.Ausfuehren(null));
        }

        [Fact]
        public void Pruefklauseln_halten_Tage_Reihenfolge_und_Eindeutigkeit()
        {
            if (!_db.Vorhanden) return;
            var k = new Betriebskalender { Bezeichner = "Klauselprobe" };
            int id = BetriebskalenderCtrl.Speichern(k);
            Assert.True(id > 0);

            Assert.True(Einfuegen(id, 1, 10, 20));
            Assert.False(Einfuegen(id, 1, 30, 40));      // Reihenfolge doppelt
            Assert.False(Einfuegen(id, 0, 30, 40));      // Reihenfolge < 1
            Assert.False(Einfuegen(id, 2, 0, 40));       // Beginn ausserhalb 1 … 365
            Assert.False(Einfuegen(id, 2, 30, 366));     // Ende ausserhalb 1 … 365
            Assert.True(Einfuegen(id, 2, 350, 5));       // ueber den Jahreswechsel
        }

        private static bool Einfuegen(int kalender, int reihe, int beginn, int ende)
        {
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren("INSERT INTO Tab_Betriebskalenderferien (ID_Betriebskalender, Reihenfolge, Beginn, Ende) " +
                                 "VALUES (?, ?, ?, ?)", new DbParam("?", kalender), new DbParam("?", reihe),
                                 new DbParam("?", beginn), new DbParam("?", ende));
                    v.Commit();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
