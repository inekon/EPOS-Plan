using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt „Katalogkosten Investition"</b> (<see cref="KatalogkostenInvestitionSchema"/>, KA‑E‑14): Nummer und
    /// Register, die acht Spalten mit Verweis an der Testkopie, leer, ein zweiter Lauf ändert nichts.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogkostenInvestitionSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_ist_210_und_das_Ziel_und_steht_im_Register()
        {
            Assert.Equal(210, KatalogkostenInvestitionSchema.SCHRITT);
            Assert.Equal(KatalogkostenInvestitionSchema.SCHRITT, SchemaStand.Zielversion);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == KatalogkostenInvestitionSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
            object stand = DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation");
            if (_db.Vorhanden) Assert.Equal(KatalogkostenInvestitionSchema.SCHRITT, Convert.ToInt32(stand, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Acht_Kataloge_tragen_die_Spalte_mit_Verweis_leer_und_der_Schritt_ist_wiederholbar()
        {
            Assert.Equal(8, KatalogkostenInvestitionSchema.SPALTEN.Count);
            Assert.Equal(KatalogkostenUrsprungSchema.KATALOGE_MIT_KOSTEN, KatalogkostenInvestitionSchema.SPALTEN.Select(s => s.Tabelle));
            if (!_db.Vorhanden) return;
            Assert.True(KatalogkostenInvestitionSchema.Vollstaendig());
            foreach ((string tabelle, string spalte, _) in KatalogkostenInvestitionSchema.SPALTEN)
            {
                Assert.Contains(Verweise(tabelle), f => f.Spalte == KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION &&
                                                        f.Ziel == KatalogkostenUrsprungSchema.TAB_KOSTENVORLAGE);
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" IS NOT NULL"), CultureInfo.InvariantCulture));
            }

            var bericht = new List<string>();
            Assert.Equal(0, KatalogkostenInvestitionSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Spalte_ist_Metaspalte_der_Katalogfassung_und_geht_im_Rueckweg_nie_mit()
        {
            Assert.Contains(KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION, Katalogrueckweg.NIE_MIT);
            Assert.Contains(KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION, Katalogfassung.Metaspalten);
        }

        private static List<(string Spalte, string Ziel)> Verweise(string tabelle)
        {
            var liste = new List<(string, string)>();
            var dt = DataRepository.GetDataTable("SELECT \"from\", \"table\" FROM pragma_foreign_key_list(?)", new DbParam("@t", tabelle));
            foreach (System.Data.DataRow r in dt.Rows)
                liste.Add((Convert.ToString(r[0], CultureInfo.InvariantCulture), Convert.ToString(r[1], CultureInfo.InvariantCulture)));
            return liste;
        }
    }
}
