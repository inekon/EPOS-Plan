using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt „Katalogkosten und Ursprung"</b> (<see cref="KatalogkostenUrsprungSchema"/>): Nummer und Register,
    /// die neunzehn Spalten mit Verweis an der Testkopie, ein zweiter Lauf ändert nichts.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogkostenUrsprungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_haengt_ueber_die_Klasse_an_der_Kalenderbedienung_und_ist_das_Ziel()
        {
            Assert.Equal(KalenderbedienungSchema.SCHRITT + 1, KatalogkostenUrsprungSchema.SCHRITT);
            Assert.Equal(208, KatalogkostenUrsprungSchema.SCHRITT);
            Assert.Equal(KatalogkostenUrsprungSchema.SCHRITT, SchemaStand.Zielversion);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == KatalogkostenUrsprungSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
            object stand = DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation");
            if (_db.Vorhanden) Assert.Equal(KatalogkostenUrsprungSchema.SCHRITT, Convert.ToInt32(stand, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Acht_Kataloge_und_elf_Kopien_tragen_ihre_Spalte_mit_Verweis_und_der_Schritt_ist_wiederholbar()
        {
            Assert.Equal(8, KatalogkostenUrsprungSchema.KATALOGE_MIT_KOSTEN.Count);
            Assert.Equal(11, KatalogkostenUrsprungSchema.KOPIEN_OHNE_URSPRUNG.Count);
            Assert.Equal(19, KatalogkostenUrsprungSchema.SPALTEN.Count);
            if (!_db.Vorhanden) return;
            Assert.True(KatalogkostenUrsprungSchema.Vollstaendig());
            foreach (string t in KatalogkostenUrsprungSchema.KATALOGE_MIT_KOSTEN)
                Assert.Contains(Verweise(t), f => f.Spalte == KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE &&
                                                  f.Ziel == KatalogkostenUrsprungSchema.TAB_KOSTENVORLAGE);
            foreach ((string kopie, string katalog) in KatalogkostenUrsprungSchema.KOPIEN_OHNE_URSPRUNG)
                Assert.Contains(Verweise(kopie), f => f.Spalte == KatalogkostenUrsprungSchema.SPALTE_ID_STAMM && f.Ziel == katalog);

            var bericht = new List<string>();
            Assert.Equal(0, KatalogkostenUrsprungSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Alle_Spalten_entstehen_leer()
        {
            if (!_db.Vorhanden) return;
            foreach ((string tabelle, string spalte, _) in KatalogkostenUrsprungSchema.SPALTEN)
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" IS NOT NULL"), CultureInfo.InvariantCulture));
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
