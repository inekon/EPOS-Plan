using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schemaschritt der Übergabegrenze</b> (<see cref="UebergabegrenzeSchema"/>; Umsetzungskonzept Übergabegrenze und
    /// Bivalenz, Abschnitt 4): acht Gerätespalten an beiden Wärmepumpentabellen, die Rücklaufgrenze an beiden BHKW-Tabellen,
    /// Einbindung und Vorwärmbetrieb an der Anlage, Bereiche und Zähler an Modul- und Projektergebnis — 43 Spalten.
    /// </summary>
    [Collection("Testdatenbank")]
    public class UebergabegrenzeSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_haengt_an_der_Vorgaengerklasse_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(KuehlkurveSchema.SCHRITT + 1, UebergabegrenzeSchema.SCHRITT);
            Assert.Equal(203, UebergabegrenzeSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= UebergabegrenzeSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == UebergabegrenzeSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Der_Schritt_fuehrt_43_Spalten_je_Tabelle_wie_die_Spaltentafel()
        {
            Assert.Equal(43, UebergabegrenzeSchema.SPALTEN.Count);
            Assert.Equal(43, UebergabegrenzeSchema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).Distinct().Count());
            var je = UebergabegrenzeSchema.SPALTEN.GroupBy(x => x.Tabelle).ToDictionary(g => g.Key, g => g.Count());
            Assert.Equal(8, je["Tab_WP"]);
            Assert.Equal(8, je["Tab_WP_STAMM"]);
            Assert.Equal(1, je["Tab_BHKW"]);
            Assert.Equal(1, je["Tab_BHKW_STAMM"]);
            Assert.Equal(2, je["Tab_Energieanlagen"]);
            Assert.Equal(13, je["Tab_ErgebnisWaermepumpeModul"]);
            Assert.Equal(10, je["Tab_ErgebnisWaermepumpe"]);
            Assert.Equal(7, je.Count);
            string[] wp =
            {
                "Spreizung_Auslegung_K", "Spreizung_Max_K", "Spreizung_Min_K", "Mindestvolumenstrom_Prozent", "Ruecklauf_Max",
                "Ruecklauf_Bezug", "Ruecklauf_Abwertung_ProzentJeK", "Kaeltemittel",
            };
            Assert.Equal(wp, UebergabegrenzeSchema.SPALTEN.Where(x => x.Tabelle == "Tab_WP").Select(x => x.Spalte).ToArray());
            // U-2: das Kältemittel ohne Werteliste in der Datenbank.
            Assert.Equal("TEXT", UebergabegrenzeSchema.SPALTEN.Single(x => x.Tabelle == "Tab_WP" && x.Spalte == "Kaeltemittel").Typ);
            // Bivalenzpunkte nur am Modul; das Projektergebnis führt seinen Bivalenzpunkt schon.
            Assert.DoesNotContain(UebergabegrenzeSchema.SPALTEN,
                                  x => x.Tabelle == "Tab_ErgebnisWaermepumpe" && x.Spalte.StartsWith("Bivalenzpunkt", StringComparison.Ordinal));
            // Alle Spalten nullbar: keine NOT NULL- und keine DEFAULT-Klausel.
            Assert.All(UebergabegrenzeSchema.SPALTEN, x => Assert.DoesNotContain("NOT NULL", x.Typ, StringComparison.Ordinal));
            Assert.All(UebergabegrenzeSchema.SPALTEN, x => Assert.DoesNotContain("DEFAULT", x.Typ, StringComparison.Ordinal));
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: ein Wert außerhalb wird abgewiesen, NULL angenommen.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_die_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            foreach (string t in UebergabegrenzeSchema.SPALTEN.Select(x => x.Tabelle).Distinct())
            {
                Ausfuehren(c, "CREATE TABLE \"" + t + "\" (ID INTEGER PRIMARY KEY) STRICT");
                Ausfuehren(c, "INSERT INTO \"" + t + "\" (ID) VALUES (1)");
            }
            foreach ((string Tabelle, string Spalte, string Typ) s in UebergabegrenzeSchema.SPALTEN)
                Ausfuehren(c, UebergabegrenzeSchema.Anlegen(s));

            foreach (string t in new[] { "Tab_WP", "Tab_WP_STAMM" })
            {
                Pruefe(c, t, "Spreizung_Auslegung_K", new[] { "NULL", "3", "5", "8" }, new[] { "2.9", "8.1", "'x'" });
                Pruefe(c, t, "Spreizung_Max_K", new[] { "NULL", "5", "10", "40" }, new[] { "4.9", "40.1" });
                Pruefe(c, t, "Spreizung_Min_K", new[] { "NULL", "0", "3", "8" }, new[] { "-0.1", "8.1" });
                Pruefe(c, t, "Mindestvolumenstrom_Prozent", new[] { "NULL", "20", "60", "100" }, new[] { "19.9", "100.1" });
                Pruefe(c, t, "Ruecklauf_Max", new[] { "NULL", "20", "55", "70" }, new[] { "19.9", "70.1" });
                Pruefe(c, t, "Ruecklauf_Bezug", new[] { "NULL", "20", "30", "40" }, new[] { "19.9", "40.1" });
                Pruefe(c, t, "Ruecklauf_Abwertung_ProzentJeK", new[] { "NULL", "0", "2.5", "5" }, new[] { "-0.1", "5.1" });
                Pruefe(c, t, "Kaeltemittel", new[] { "NULL", "'R290'", "'R744'", "''" }, Array.Empty<string>());
            }
            foreach (string t in new[] { "Tab_BHKW", "Tab_BHKW_STAMM" })
                Pruefe(c, t, "Ruecklauf_Max", new[] { "NULL", "40", "70", "90" }, new[] { "39.9", "90.1" });
            Pruefe(c, "Tab_Energieanlagen", "Einbindung", new[] { "NULL", "'DIREKT'", "'PUFFER'", "'WEICHE'" },
                   new[] { "'direkt'", "''", "'REIHE'", "1" });
            Pruefe(c, "Tab_Energieanlagen", "Vorwaermbetrieb", new[] { "NULL", "0", "1" }, new[] { "2", "-1", "'ja'" });
            foreach (string t in new[] { "Tab_ErgebnisWaermepumpeModul", "Tab_ErgebnisWaermepumpe" })
            {
                foreach (string h in UebergabegrenzeSchema.SPALTEN_ERGEBNIS.Where(x => x.EndsWith("_h", StringComparison.Ordinal)))
                    Pruefe(c, t, h, new[] { "NULL", "0", "8760" }, new[] { "-1", "8761", "1.5" });
                foreach (string m in UebergabegrenzeSchema.SPALTEN_ERGEBNIS.Where(x => x.EndsWith("_MWh", StringComparison.Ordinal)))
                    Pruefe(c, t, m, new[] { "NULL", "0", "123.4" }, new[] { "-0.001" });
            }
            Pruefe(c, "Tab_ErgebnisWaermepumpeModul", "Bivalenzpunkt_1", new[] { "NULL", "-7.5", "12" }, new[] { "'x'" });
            Pruefe(c, "Tab_ErgebnisWaermepumpeModul", "Bivalenzpunkt_2", new[] { "NULL", "-15", "3" }, new[] { "'x'" });
            Pruefe(c, "Tab_ErgebnisWaermepumpeModul", "Uebergabe_Max_kW", new[] { "NULL", "0", "45.5" }, new[] { "-1" });
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und die Rundreise
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= UebergabegrenzeSchema.SCHRITT);
            Assert.True(UebergabegrenzeSchema.Vollstaendig());
            Assert.True(UebergabegrenzeSchema.ErgebnisspaltenVorhanden());
            Assert.True(UebergabegrenzeSchema.AnlagenspaltenVorhanden());
            Assert.True(UebergabegrenzeSchema.GeraetespaltenVorhanden("Tab_WP_STAMM"));
            // Kein DML: jede Zeile steht leer - Bestandsweg (U-1), Kennzahlen „nicht erhoben".
            foreach ((string tabelle, string spalte, string _) in UebergabegrenzeSchema.SPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" IS NOT NULL"));
            // Die Prüfklausel wirkt auch an der Testdatenbank.
            Assert.True(Wirft("UPDATE Tab_Energieanlagen SET Einbindung = 'REIHE'"));
            Assert.True(Wirft("UPDATE Tab_Energieanlagen SET Vorwaermbetrieb = 2"));
        }

        /// <summary>Rundreise: aus dem Stand davor legt der Schritt alle Spalten an; zweimal = nichts.</summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach ((string tabelle, string spalte, string _) in UebergabegrenzeSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            long anlagen = Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen");
            long geraete = Zahl("SELECT COUNT(*) FROM Tab_WP_STAMM");
            Assert.False(UebergabegrenzeSchema.Vollstaendig());
            Assert.False(UebergabegrenzeSchema.ErgebnisspaltenVorhanden());

            var bericht = new List<string>();
            Assert.Equal(43, UebergabegrenzeSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(UebergabegrenzeSchema.Vollstaendig());
            Assert.Equal(anlagen, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen"));
            Assert.Equal(geraete, Zahl("SELECT COUNT(*) FROM Tab_WP_STAMM"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            var zweiter = new List<string>();
            Assert.Equal(0, UebergabegrenzeSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("nichts zu tun", StringComparison.Ordinal));
        }

        /// <summary>Teilstand: Fehlt nur eine Spalte, legt der Schritt genau diese an.</summary>
        [Fact]
        public void Der_Schritt_ergaenzt_einen_Teilstand()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Energieanlagen DROP COLUMN Vorwaermbetrieb");
            Assert.False(UebergabegrenzeSchema.AnlagenspaltenVorhanden());
            Assert.Equal(1, UebergabegrenzeSchema.Ausfuehren(null));
            Assert.True(UebergabegrenzeSchema.Vollstaendig());
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static void Pruefe(SqliteConnection c, string tabelle, string spalte, string[] gut, string[] schlecht)
        {
            foreach (string w in gut)
                Assert.False(Wirft(c, "UPDATE \"" + tabelle + "\" SET \"" + spalte + "\" = " + w), tabelle + "." + spalte + " = " + w);
            foreach (string w in schlecht)
                Assert.True(Wirft(c, "UPDATE \"" + tabelle + "\" SET \"" + spalte + "\" = " + w), tabelle + "." + spalte + " = " + w);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static bool Wirft(string sql)
        {
            try { return !DataRepository.ExecuteSQL(sql); }
            catch (Exception) { return true; }
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }
    }
}
