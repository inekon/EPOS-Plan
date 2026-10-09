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
    /// <b>Der Schemaschritt von Teillast und Takten der Kältemaschine</b> (<see cref="KaeltemaschineTeillastSchema"/>;
    /// Fachkonzept Teillast und Takten, Abschnitte 4.1, 5.3 und 6): acht Eingabespalten an Katalog und Projektkopie, fünf
    /// Kennzahlspalten am Ergebnis — 21 Spalten, alle leer; die Ergänzung der Typkennfelder ist in E1-a ein leerer Rumpf.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltemaschineTeillastSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static readonly string[] EINGABE =
        {
            "Teillast_Weg", "Teillastkurve_a", "Teillastkurve_b", "Teillastkurve_c", "Teillastkurve_Lastgrad_Min",
            "Taktverlustfaktor_Cd", "Verdichterregelung", "Kennfeld_Randweg",
        };

        private static readonly string[] ERGEBNIS =
        {
            "Taktstrom_MWh", "Starts", "Teillaststunden", "Lastgrad_Mittel", "Stunden_Extrapoliert",
        };

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_209_das_Ziel_und_die_Paketanhebung_fuehrt_Katalog()
        {
            // Haengt an 208 KatalogkostenUrsprungSchema (KA1); beim Merge auf dessen SCHRITT + 1 umhaengen.
            Assert.Equal(209, KaeltemaschineTeillastSchema.SCHRITT);
            Assert.Equal(KaeltemaschineTeillastSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KaeltemaschineTeillastSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Der_Schritt_fuehrt_21_Spalten_wie_die_Spaltentafel()
        {
            Assert.Equal(21, KaeltemaschineTeillastSchema.SPALTEN.Count);
            Assert.Equal(21, KaeltemaschineTeillastSchema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).Distinct().Count());
            Assert.Equal(EINGABE, KaeltemaschineTeillastSchema.SPALTEN.Where(x => x.Tabelle == "Tab_Kaeltemaschine_STAMM").Select(x => x.Spalte));
            Assert.Equal(EINGABE, KaeltemaschineTeillastSchema.SPALTEN.Where(x => x.Tabelle == "Tab_Kaeltemaschine").Select(x => x.Spalte));
            Assert.Equal(ERGEBNIS, KaeltemaschineTeillastSchema.SPALTEN.Where(x => x.Tabelle == "Tab_ErgebnisKaeltemaschine").Select(x => x.Spalte));
            Assert.All(KaeltemaschineTeillastSchema.SPALTEN, x => Assert.DoesNotContain("NOT NULL", x.Typ, StringComparison.Ordinal));
            Assert.All(KaeltemaschineTeillastSchema.SPALTEN, x => Assert.DoesNotContain("DEFAULT", x.Typ, StringComparison.Ordinal));
            // Die acht Eingabespalten haengen hinten an den Fachspalten (Katalogfassung, Projektkopie).
            Assert.Equal(KaeltemaschineSchema.Grundspalten.Concat(EINGABE), KaeltemaschineSchema.Fachspalten);
            Assert.Equal(EINGABE, Katalogfassung.Tabelle(KaeltemaschineSchema.TAB_STAMM).Fachspalten.Skip(12));
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: ein Wert außerhalb wird abgewiesen, NULL angenommen.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_die_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            foreach (string t in KaeltemaschineTeillastSchema.SPALTEN.Select(x => x.Tabelle).Distinct())
            {
                Ausfuehren(c, "CREATE TABLE \"" + t + "\" (ID INTEGER PRIMARY KEY) STRICT");
                Ausfuehren(c, "INSERT INTO \"" + t + "\" (ID) VALUES (1)");
            }
            foreach ((string Tabelle, string Spalte, string Typ) s in KaeltemaschineTeillastSchema.SPALTEN)
                Ausfuehren(c, KaeltemaschineTeillastSchema.Anlegen(s));

            foreach (string t in new[] { "Tab_Kaeltemaschine_STAMM", "Tab_Kaeltemaschine" })
            {
                Pruefe(c, t, "Teillast_Weg", new[] { "NULL", "'LINEAR'", "'KURVE'" }, new[] { "'linear'", "''", "'STUFEN'", "1" });
                foreach (string k in new[] { "Teillastkurve_a", "Teillastkurve_b", "Teillastkurve_c" })
                    Pruefe(c, t, k, new[] { "NULL", "-1.5", "0", "3.5" }, new[] { "'x'" });
                Pruefe(c, t, "Teillastkurve_Lastgrad_Min", new[] { "NULL", "0", "0.25", "1" }, new[] { "-0.01", "1.01" });
                Pruefe(c, t, "Taktverlustfaktor_Cd", new[] { "NULL", "0", "0.9", "1" }, new[] { "-0.01", "1.01" });
                Pruefe(c, t, "Verdichterregelung", new[] { "NULL", "'EIN_AUS'", "'STUFEN'", "'DREHZAHL'" },
                       new[] { "'ein_aus'", "''", "'KURVE'" });
                Pruefe(c, t, "Kennfeld_Randweg", new[] { "NULL", "'RANDWERT'", "'GUETEGRAD'" }, new[] { "'randwert'", "''", "'LINEAR'" });
            }
            const string e = "Tab_ErgebnisKaeltemaschine";
            Pruefe(c, e, "Taktstrom_MWh", new[] { "NULL", "0", "1.25" }, new[] { "-0.001" });
            Pruefe(c, e, "Starts", new[] { "NULL", "0", "12000" }, new[] { "-1", "1.5" });
            Pruefe(c, e, "Teillaststunden", new[] { "NULL", "0", "8760" }, new[] { "-1", "8761" });
            Pruefe(c, e, "Lastgrad_Mittel", new[] { "NULL", "0", "0.6", "1" }, new[] { "-0.01", "1.01" });
            Pruefe(c, e, "Stunden_Extrapoliert", new[] { "NULL", "0", "8760" }, new[] { "-1", "8761" });
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und die Rundreise
        // =============================================================================

        [Fact]
        public void Die_Testkopie_steht_auf_dem_Schritt_und_alles_ist_leer()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KaeltemaschineTeillastSchema.SCHRITT);
            Assert.True(KaeltemaschineTeillastSchema.Vollstaendig());
            Assert.True(KaeltemaschineTeillastSchema.ErgebnisspaltenVorhanden());
            Assert.True(KaeltemaschineTeillastSchema.EingabespaltenVorhanden("Tab_Kaeltemaschine_STAMM"));
            Assert.True(KaeltemaschineTeillastSchema.EingabespaltenVorhanden("Tab_Kaeltemaschine"));
            // Kein DML in E1-a: jede Zeile steht leer - heutiger Weg, Kennzahlen „nicht erhoben".
            foreach ((string tabelle, string spalte, string _) in KaeltemaschineTeillastSchema.SPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" IS NOT NULL"));
            Assert.True(Wirft("UPDATE Tab_Kaeltemaschine_STAMM SET Teillast_Weg = 'STUFEN'"));
            Assert.True(Wirft("UPDATE Tab_Kaeltemaschine SET Taktverlustfaktor_Cd = 1.5"));
        }

        /// <summary>Rundreise: aus dem Stand davor legt der Schritt alle Spalten an; zweimal = nichts; eigene Sätze bleiben.</summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_ruft_Ergaenzen_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach ((string tabelle, string spalte, string _) in KaeltemaschineTeillastSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            Assert.False(KaeltemaschineTeillastSchema.Vollstaendig());
            string stammVorher = Abzug("Tab_Kaeltemaschine_STAMM");
            string projektVorher = Abzug("Tab_Kaeltemaschine");

            var bericht = new List<string>();
            KaeltemaschineTeillastSchema.Laufergebnis e = KaeltemaschineTeillastSchema.Ausfuehren(bericht);
            Assert.Equal(21, e.Angelegt);
            Assert.Equal(0, e.Ergaenzt);
            Assert.Contains(bericht, z => z.StartsWith("0 Typkennfeld(er)", StringComparison.Ordinal));
            Assert.True(KaeltemaschineTeillastSchema.Vollstaendig());
            Assert.Equal(stammVorher, Abzug("Tab_Kaeltemaschine_STAMM"));
            Assert.Equal(projektVorher, Abzug("Tab_Kaeltemaschine"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            var zweiter = new List<string>();
            KaeltemaschineTeillastSchema.Laufergebnis z2 = KaeltemaschineTeillastSchema.Ausfuehren(zweiter);
            Assert.Equal(0, z2.Angelegt);
            Assert.Equal(0, z2.Ergaenzt);
            Assert.Contains(zweiter, z => z.Contains("nichts anzulegen", StringComparison.Ordinal));
        }

        [Fact]
        public void Ergaenzen_ist_in_E1a_ein_leerer_Rumpf()
        {
            if (!_db.Vorhanden) return;
            string vorher = Abzug("Tab_Kaeltemaschine_STAMM");
            Assert.Equal(0, KaeltemaschinenTypkennfelder.Ergaenzen());
            Assert.Equal(vorher, Abzug("Tab_Kaeltemaschine_STAMM"));
        }

        /// <summary>Teilstand: Fehlt nur eine Spalte, legt der Schritt genau diese an.</summary>
        [Fact]
        public void Der_Schritt_ergaenzt_einen_Teilstand()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Kaeltemaschine DROP COLUMN Kennfeld_Randweg");
            Assert.False(KaeltemaschineTeillastSchema.EingabespaltenVorhanden("Tab_Kaeltemaschine"));
            Assert.Equal(1, KaeltemaschineTeillastSchema.Ausfuehren(null).Angelegt);
            Assert.True(KaeltemaschineTeillastSchema.Vollstaendig());
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>Alle Zeilen einer Tabelle als Text (ohne die Spalten des Schritts) — zum Vergleich vorher/nachher.</summary>
        private static string Abzug(string tabelle)
        {
            var spalten = DataRepository.SpaltenVonTabelle(tabelle)
                .Where(s => !KaeltemaschineTeillastSchema.EINGABESPALTEN.Contains(s)).ToList();
            var dt = DataRepository.GetDataTable("SELECT " + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) +
                                                 " FROM \"" + tabelle + "\" ORDER BY ID");
            return string.Join("\n", dt.Rows.Cast<System.Data.DataRow>()
                .Select(r => string.Join("|", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))));
        }

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

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try
            {
                Ausfuehren(c, sql);
                return false;
            }
            catch (SqliteException) { return true; }
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
