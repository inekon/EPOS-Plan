using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>MA-6b (E149): der Schemaschritt der Herkunft des U-Werts auf der Kopie der Testdatenbank.</summary>
    [Collection("Testdatenbank")]
    public class BauteilUHerkunftSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        [Fact]
        public void Die_Nummer_haengt_an_BK5_und_ist_das_Ziel()
        {
            // Keine feste Nummer: die Kette haengt ueber + 1 an der Vorgaengerklasse.
            Assert.Equal(BauteilkatalogAbschnittSchema.SCHRITT + 1, BauteilUHerkunftSchema.SCHRITT);
            Assert.Equal(BauteilUHerkunftSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == BauteilUHerkunftSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Equal(new[] { "Tab_Bauteil", "Tab_Bauteil_STAMM" }, BauteilUHerkunftSchema.TABELLEN);
            Assert.Equal("TEXT CHECK (\"U_Herkunft\" IS NULL OR \"U_Herkunft\" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ'))",
                         BauteilUHerkunftSchema.Typ);
        }

        [Fact]
        public void Der_Schritt_legt_die_Spalte_leer_an_mit_Check_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(BauteilUHerkunftSchema.Vollstaendig());
            foreach (string t in BauteilUHerkunftSchema.TABELLEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + t + "\" DROP COLUMN \"U_Herkunft\"");
            GebaeudeZonenanschluss.ProbeVerwerfen();
            Assert.Equal(2, BauteilUHerkunftSchema.Offen());
            Assert.False(GebaeudeZonenanschluss.UHerkunftVorhanden());

            var bericht = new List<string>();
            Assert.Equal(2, BauteilUHerkunftSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Bauteil.U_Herkunft angelegt", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("Tab_Bauteil_STAMM.U_Herkunft angelegt", StringComparison.Ordinal));
            Assert.True(BauteilUHerkunftSchema.Vollstaendig());
            Assert.True(GebaeudeZonenanschluss.UHerkunftVorhanden());
            foreach (string t in BauteilUHerkunftSchema.TABELLEN)
            {
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"U_Herkunft\" IS NOT NULL"));
                // STRICT bleibt, und der CHECK steht im Tabellentext.
                string sql = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT \"sql\" FROM sqlite_master WHERE \"type\" = 'table' AND \"name\" = ?", new DbParam("@t", t)),
                    CultureInfo.InvariantCulture) ?? "";
                Assert.Contains("\"U_Herkunft\" " + BauteilUHerkunftSchema.Typ, sql, StringComparison.Ordinal);
                Assert.EndsWith("STRICT", sql.TrimEnd(), StringComparison.Ordinal);
            }
            var zweiter = new List<string>();
            Assert.Equal(0, BauteilUHerkunftSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Der_Check_weist_einen_fremden_Wert_ab()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT COALESCE(MIN(\"ID\"), 0) FROM \"Tab_Bauteil\"");
            if (id == 0) return;
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_Bauteil\" SET \"U_Herkunft\" = ? WHERE \"ID\" = ?",
                                           new DbParam("@h", DbWerte.HERKUNFT_MANUELL), new DbParam("@i", id));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil\" WHERE \"U_Herkunft\" = 'MANUELL'"));
            Assert.ThrowsAny<Exception>(() =>
            {
                using DbVorgang v = DataRepository.Vorgang();
                v.Ausfuehren("UPDATE \"Tab_Bauteil\" SET \"U_Herkunft\" = ? WHERE \"ID\" = ?", new DbParam("@h", "HANDISCH"), new DbParam("@i", id));
                v.Commit();
            });
        }
    }

    /// <summary>MA-6b (E149): die Herkunft des U-Werts im Schreibweg der Pflege — ohne Datenbank.</summary>
    public class BauteilUHerkunftPflegeTests
    {
        private const string M = DbWerte.HERKUNFT_MANUELL;
        private const string K = DbWerte.HERKUNFT_KATALOG;
        private const string Q = DbWerte.HERKUNFT_SQPROJ;

        [Fact]
        public void Unveraendert_bleibt_die_gespeicherte_Herkunft()
        {
            Assert.Equal(Q, GebaeudeZonenCtrl.UHerkunftNachPflege(Q, 5, 0.5, 5, 0.5));
            Assert.Equal(Q, GebaeudeZonenCtrl.UHerkunftNachPflege(Q, null, 0.5, null, 0.5 + 1e-12));
            Assert.Null(GebaeudeZonenCtrl.UHerkunftNachPflege(null, null, 0.5, null, 0.5));
        }

        [Fact]
        public void Ein_von_Hand_geaenderter_U_Wert_ohne_Aufbauwechsel_wird_MANUELL()
        {
            Assert.Equal(M, GebaeudeZonenCtrl.UHerkunftNachPflege(Q, null, 0.5, null, 0.3));
            Assert.Equal(M, GebaeudeZonenCtrl.UHerkunftNachPflege(Q, 7, 0.5, 7, 0.3));
            Assert.Equal(M, GebaeudeZonenCtrl.UHerkunftNachPflege(null, null, null, null, 0.3));
        }

        [Fact]
        public void Ein_anderer_Aufbau_macht_den_U_Wert_zum_Katalogwert()
        {
            Assert.Equal(K, GebaeudeZonenCtrl.UHerkunftNachPflege(Q, null, 0.5, 9, 0.5));
            Assert.Equal(K, GebaeudeZonenCtrl.UHerkunftNachPflege(M, 7, 0.3, 9, null));
            // Aufbau entfernt, U-Wert bleibt stehen: von Hand; ohne beides keine Herkunft.
            Assert.Equal(M, GebaeudeZonenCtrl.UHerkunftNachPflege(K, 9, 0.5, null, 0.5));
            Assert.Null(GebaeudeZonenCtrl.UHerkunftNachPflege(K, 9, null, null, null));
        }

        [Fact]
        public void Ein_neues_Bauteil_der_Pflege()
        {
            Assert.Equal(M, GebaeudeZonenCtrl.UHerkunftNeu(new BauteilModel { U_Wert = 0.3 }));
            Assert.Equal(K, GebaeudeZonenCtrl.UHerkunftNeu(new BauteilModel { ID_Aufbau = 3, U_Wert = 0.3 }));
            Assert.Null(GebaeudeZonenCtrl.UHerkunftNeu(new BauteilModel()));
            Assert.Equal(DbWerte.HERKUNFT_VORGABE,
                         GebaeudeZonenCtrl.UHerkunftNeu(new BauteilModel { U_Wert = 0.3, U_Herkunft = DbWerte.HERKUNFT_VORGABE }));
        }
    }
}
