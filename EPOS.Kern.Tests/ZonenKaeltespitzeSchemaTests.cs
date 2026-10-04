using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schritt 185 — die Kältespitze je Zone</b> (<see cref="ZonenKaeltespitzeSchema"/>): Nummer und Kette, die
    /// zwei Spalten an <c>Tab_ErgebnisZone</c> samt Prüfklauseln, Spaltenzahl, Rundreise und Wiederholbarkeit, und
    /// das Lesen und Schreiben über <see cref="ErgebnisCtrl"/> (die Spalten bleiben ohne Kühlung leer).
    /// </summary>
    public sealed class ZonenKaeltespitzeSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_ist_185_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            // Vorläufig hinter KaeltemaschineAnlageSchema + 2; nach dem Merge von KU3-4d an KaeltestromabrechnungSchema + 1.
            Assert.Equal(185, ZonenKaeltespitzeSchema.SCHRITT);
            Assert.Equal(ZonenKaeltespitzeSchema.SCHRITT, SchemaStand.Zielversion);
            Assert.Equal(Paketanhebung.Art.Ddl, Paketanhebung.Stufen.Single(x => x.Nr == ZonenKaeltespitzeSchema.SCHRITT).Wirkung);
            Assert.Equal(ZonenUebergabeSchema.SPALTENZAHL_ERGEBNIS_ZONE + 2, ZonenKaeltespitzeSchema.SPALTENZAHL_ERGEBNIS_ZONE);
        }

        [Fact]
        public void Die_Testkopie_fuehrt_beide_Spalten_leer_mit_Pruefklauseln()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ZonenKaeltespitzeSchema.Vollstaendig());
            Assert.Equal(ZonenKaeltespitzeSchema.SPALTENZAHL_ERGEBNIS_ZONE, DataRepository.SpaltenVonTabelle(ZonenKaeltespitzeSchema.TAB_ERGEBNIS_ZONE).Count);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"Tab_ErgebnisZone\" WHERE \"Kaeltespitze_kW\" IS NOT NULL OR \"Kuehlstunden\" IS NOT NULL")));
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?", new DbParam("@n", "Tab_ErgebnisZone")));
            Assert.Contains("\"Kaeltespitze_kW\" REAL CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("\"Kuehlstunden\" INTEGER CHECK", ddl, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach (var s in ZonenKaeltespitzeSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            Assert.False(ZonenKaeltespitzeSchema.Vollstaendig());
            Assert.Equal(ZonenUebergabeSchema.SPALTENZAHL_ERGEBNIS_ZONE, DataRepository.SpaltenVonTabelle(ZonenKaeltespitzeSchema.TAB_ERGEBNIS_ZONE).Count);
            var bericht = new List<string>();
            Assert.Equal(2, ZonenKaeltespitzeSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(ZonenKaeltespitzeSchema.Vollstaendig());
            Assert.Equal(0, ZonenKaeltespitzeSchema.Ausfuehren(null));
        }
    }
}
