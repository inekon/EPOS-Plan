using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schritt 188 — Ausweis der Vorlaufwahl der Wärmepumpe</b> (VW1a, <see cref="VorlaufwahlSchema"/>): Nummer,
    /// Ziel und Register; die drei Spalten samt Prüfklauseln; der Spaltentext hin und zurück; die Testdatenbank ohne
    /// Saat; der Stand davor und die Wiederholbarkeit; die Rundreise im Ergebnis der Wärmepumpe NULL-erhaltend.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class VorlaufwahlSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1007;

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_188_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(FreieKuehlungSoleSchema.SCHRITT + 1, VorlaufwahlSchema.SCHRITT);
            Assert.Equal(188, VorlaufwahlSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= VorlaufwahlSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == VorlaufwahlSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                {
                    "Tab_ErgebnisWaermepumpeModul.Vorlaufwahl_Stunden",
                    "Tab_ErgebnisWaermepumpeModul.Vorlauf_Darueber_Stunden",
                    "Tab_ErgebnisWaermepumpeModul.Vorlauf_Darunter_Stunden",
                },
                VorlaufwahlSchema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).ToArray());
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: Text frei, beide Zähler 0 … 8760 ganzzahlig oder NULL.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_beide_Zaehler()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_ErgebnisWaermepumpeModul\" (ID INTEGER PRIMARY KEY) STRICT");
            foreach (var s in VorlaufwahlSchema.SPALTEN) Ausfuehren(c, VorlaufwahlSchema.Anlegen(s));
            Ausfuehren(c, "INSERT INTO Tab_ErgebnisWaermepumpeModul (ID) VALUES (1)");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM Tab_ErgebnisWaermepumpeModul WHERE Vorlaufwahl_Stunden IS NOT NULL " +
                                       "OR Vorlauf_Darueber_Stunden IS NOT NULL OR Vorlauf_Darunter_Stunden IS NOT NULL"));

            Assert.False(Wirft(c, "UPDATE Tab_ErgebnisWaermepumpeModul SET Vorlaufwahl_Stunden = '35:1200;45:800;55:300'"));
            Assert.False(Wirft(c, "UPDATE Tab_ErgebnisWaermepumpeModul SET Vorlaufwahl_Stunden = NULL"));
            foreach (string sp in new[] { VorlaufwahlSchema.SPALTE_DARUEBER_STUNDEN, VorlaufwahlSchema.SPALTE_DARUNTER_STUNDEN })
            {
                foreach (string w in new[] { "0", "8760", "NULL" })
                    Assert.False(Wirft(c, "UPDATE Tab_ErgebnisWaermepumpeModul SET " + sp + " = " + w), sp + " " + w);
                foreach (string w in new[] { "-1", "8761", "1.5" })
                    Assert.True(Wirft(c, "UPDATE Tab_ErgebnisWaermepumpeModul SET " + sp + " = " + w), sp + " " + w);
            }
        }

        [Fact]
        public void Der_Spaltentext_steht_aufsteigend_und_liest_sich_zurueck()
        {
            var paare = new[]
            {
                new KeyValuePair<double, int>(55.0, 300),
                new KeyValuePair<double, int>(35.0, 1200),
                new KeyValuePair<double, int>(45.0, 0),
            };
            string text = VorlaufwahlSchema.StundenText(paare);
            Assert.Equal("35:1200;45:0;55:300", text);
            Assert.Equal(new[] { (35, 1200), (45, 0), (55, 300) },
                         VorlaufwahlSchema.StundenLesen(text).Select(p => (p.Key, p.Value)).ToArray());
            Assert.Null(VorlaufwahlSchema.StundenText(Array.Empty<KeyValuePair<double, int>>()));
            Assert.Null(VorlaufwahlSchema.StundenText(null));
            Assert.Empty(VorlaufwahlSchema.StundenLesen(null));
            Assert.Throws<FormatException>(() => VorlaufwahlSchema.StundenLesen("35-1200"));
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank, Stand davor, Wiederholbarkeit
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_und_traegt_keine_Saat()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= VorlaufwahlSchema.SCHRITT);
            Assert.True(VorlaufwahlSchema.Vollstaendig());
            foreach (var s in VorlaufwahlSchema.SPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + s.Tabelle + "\" WHERE \"" + s.Spalte + "\" IS NOT NULL"));
            List<string> erg = DataRepository.SpaltenVonTabelle(VorlaufwahlSchema.TAB_ERGEBNIS_WP_MODUL);
            // Dahinter stehen allein die dreizehn Modulspalten der Uebergabegrenze (Schritt 203).
            int ub = UebergabegrenzeSchema.SPALTEN_ERGEBNIS_MODUL.Count;
            Assert.Equal(VorlaufwahlSchema.SPALTEN_ERGEBNIS.ToArray(), erg.Skip(erg.Count - ub - 3).Take(3).ToArray());
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?",
                                                                       new DbParam("@n", VorlaufwahlSchema.TAB_ERGEBNIS_WP_MODUL)));
            Assert.Contains("\"Vorlaufwahl_Stunden\" TEXT", ddl, StringComparison.Ordinal);
            Assert.Contains("\"Vorlauf_Darueber_Stunden\" INTEGER CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("\"Vorlauf_Darunter_Stunden\" INTEGER CHECK", ddl, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            int spalten = DataRepository.SpaltenVonTabelle(VorlaufwahlSchema.TAB_ERGEBNIS_WP_MODUL).Count;
            foreach (var s in VorlaufwahlSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            Assert.False(VorlaufwahlSchema.Vollstaendig());
            Assert.False(VorlaufwahlSchema.ErgebnisspaltenVorhanden());

            // Ohne die Spalten (Stand 187, etwa iOS) legt das Ergebnis seine Zeilen weiter an.
            Assert.True(new ErgebnisCtrl().Save(ErgebnisMitWaermepumpe("35:10;45:20", 3, 4)) > 0);
            ErgebnisWaermepumpeModulModel alt = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Waermepumpe.Module);
            Assert.Null(alt.Vorlaufwahl_Stunden);
            Assert.Null(alt.Vorlauf_Darueber_Stunden);

            var bericht = new List<string>();
            Assert.Equal(3, VorlaufwahlSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(VorlaufwahlSchema.Vollstaendig());
            Assert.Equal(spalten, DataRepository.SpaltenVonTabelle(VorlaufwahlSchema.TAB_ERGEBNIS_WP_MODUL).Count);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            Assert.Equal(0, VorlaufwahlSchema.Ausfuehren(null));
        }

        // =============================================================================
        //  Teil 3 - Rundreise des Ergebnisses
        // =============================================================================

        private static ErgebnisModel ErgebnisMitWaermepumpe(string text, int? darueber, int? darunter)
        {
            return new ErgebnisModel
            {
                ID_Projekt = PROJEKT, Bezeichner = "VW1a", Zeitstempel = new DateTime(2026, 10, 5),
                Sim_Waermepumpe = true,
                Waermepumpe = new ErgebnisWaermepumpeModel
                {
                    Waermeproduktion_WP = 80.0,
                    Module = new List<ErgebnisWaermepumpeModulModel>
                    {
                        new ErgebnisWaermepumpeModulModel
                        {
                            Modul = "WP 1", Leistung = 50.0, Waermeproduktion = 80.0, Betriebsstunden = 2000,
                            Vorlaufwahl_Stunden = text, Vorlauf_Darueber_Stunden = darueber, Vorlauf_Darunter_Stunden = darunter,
                        },
                    },
                },
            };
        }

        [Fact]
        public void Das_Ergebnis_traegt_die_drei_Spalten_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ErgebnisCtrl();
            Assert.True(ctrl.Save(ErgebnisMitWaermepumpe(null, null, null)) > 0);
            ErgebnisWaermepumpeModulModel leer = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Waermepumpe.Module);
            Assert.Null(leer.Vorlaufwahl_Stunden);
            Assert.Null(leer.Vorlauf_Darueber_Stunden);
            Assert.Null(leer.Vorlauf_Darunter_Stunden);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisWaermepumpeModul WHERE Vorlaufwahl_Stunden IS NOT NULL " +
                                  "OR Vorlauf_Darueber_Stunden IS NOT NULL OR Vorlauf_Darunter_Stunden IS NOT NULL"));

            Assert.True(ctrl.Save(ErgebnisMitWaermepumpe("35:1200;45:800;55:300", 0, 17)) > 0);
            ErgebnisWaermepumpeModulModel mo = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Waermepumpe.Module);
            Assert.Equal("35:1200;45:800;55:300", mo.Vorlaufwahl_Stunden);
            Assert.Equal(0, mo.Vorlauf_Darueber_Stunden);
            Assert.Equal(17, mo.Vorlauf_Darunter_Stunden);
            Assert.Equal(80.0, mo.Waermeproduktion);
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql));

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using var k = c.CreateCommand();
            k.CommandText = sql;
            k.ExecuteNonQuery();
        }

        private static long Skalar(SqliteConnection c, string sql)
        {
            using var k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToInt64(k.ExecuteScalar());
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try
            {
                Ausfuehren(c, sql);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }
    }
}
