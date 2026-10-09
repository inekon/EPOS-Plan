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
    /// Kennzahlspalten am Ergebnis — 21 Spalten; gefüllt allein an den ausgelieferten Typkennfeldern über die Ergänzung.
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
            // DML allein an den ausgelieferten Typkennfeldern: Projektkopien, Ergebnis und alle übrigen Katalogsätze leer.
            foreach ((string tabelle, string spalte, string _) in KaeltemaschineTeillastSchema.SPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" IS NOT NULL" +
                                      (tabelle == KaeltemaschineTeillastSchema.TAB_STAMM ? " AND " + NICHT_TYPKENNFELD : "")));
            // Die 34 Typkennfelder: Verdichterregelung überall, Weg an allen 34 (31 Kurven, 3 linear), C_d und Randweg leer.
            Assert.Equal(34L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Verdichterregelung IS NOT NULL"));
            Assert.Equal(34L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Teillast_Weg IS NOT NULL"));
            Assert.Equal(31L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Teillast_Weg = 'KURVE' AND " +
                                   "Teillastkurve_a IS NOT NULL AND Teillastkurve_Lastgrad_Min IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Taktverlustfaktor_Cd IS NOT NULL OR " +
                                  "Kennfeld_Randweg IS NOT NULL"));
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
            Assert.Equal(34, e.Ergaenzt);
            Assert.Contains(bericht, z => z.StartsWith("34 Typkennfeld(er)", StringComparison.Ordinal));
            Assert.True(KaeltemaschineTeillastSchema.Vollstaendig());
            // Dieselben Werte wie zuvor: auch die neu gebildete Prüfsumme gleicht der gelieferten.
            Assert.Equal(stammVorher, Abzug("Tab_Kaeltemaschine_STAMM"));
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe3));
            Assert.Equal(projektVorher, Abzug("Tab_Kaeltemaschine"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            var zweiter = new List<string>();
            KaeltemaschineTeillastSchema.Laufergebnis z2 = KaeltemaschineTeillastSchema.Ausfuehren(zweiter);
            Assert.Equal(0, z2.Angelegt);
            Assert.Equal(0, z2.Ergaenzt);
            Assert.Contains(zweiter, z => z.Contains("nichts anzulegen", StringComparison.Ordinal));
        }

        /// <summary>
        /// Ergaenzen: auf der gelieferten Datenbank nichts zu tun (34 übersprungen); geleert füllt es die 34 Typkennfelder
        /// je Feld nur wo leer, bildet die Prüfsumme neu und lässt eigene Sätze, Beispielgeräte und Projektkopien stehen.
        /// </summary>
        [Fact]
        public void Ergaenzen_fuellt_die_Typkennfelder_nur_wo_leer_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            string stammVorher = Abzug("Tab_Kaeltemaschine_STAMM", alle: true);
            KaeltemaschinenTypkennfelder.Ergaenzungsergebnis nichts = KaeltemaschinenTypkennfelder.ErgaenzenMitZaehlung();
            Assert.Equal(new KaeltemaschinenTypkennfelder.Ergaenzungsergebnis(0, 34), nichts);
            Assert.Equal(stammVorher, Abzug("Tab_Kaeltemaschine_STAMM", alle: true));

            // Ein eigener Satz (ReadOnly = 0) und ein Typkennfeld mit gepflegter Regelung.
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Kaeltemaschine_STAMM (Bezeichner, ReadOnly) VALUES ('Eigener Satz', 0)");
            string projektVorher = Abzug("Tab_Kaeltemaschine", alle: true);
            string pruefsummen = Pruefsummen();
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine_STAMM SET " +
                string.Join(", ", KaeltemaschineTeillastSchema.EINGABESPALTEN.Select(s => "\"" + s + "\" = NULL")));
            const string GEPFLEGT = "KM:TYPKENNFELD_WASSER_SCROLL_100_KW";
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine_STAMM SET Verdichterregelung = 'DREHZAHL' WHERE " +
                                           "Katalog_Schluessel = ?", new DbParam("?", GEPFLEGT));

            KaeltemaschinenTypkennfelder.Ergaenzungsergebnis e = KaeltemaschinenTypkennfelder.ErgaenzenMitZaehlung();
            Assert.Equal(new KaeltemaschinenTypkennfelder.Ergaenzungsergebnis(34, 0), e);
            Assert.Equal(34L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Verdichterregelung IS NOT NULL"));
            Assert.Equal(34L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Teillast_Weg IS NOT NULL"));
            Assert.Equal("DREHZAHL", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Verdichterregelung FROM Tab_Kaeltemaschine_STAMM WHERE Katalog_Schluessel = ?", new DbParam("?", GEPFLEGT)),
                CultureInfo.InvariantCulture));
            Assert.Equal("KURVE", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Teillast_Weg FROM Tab_Kaeltemaschine_STAMM WHERE Katalog_Schluessel = ?", new DbParam("?", GEPFLEGT)),
                CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE " + NICHT_TYPKENNFELD + " AND " +
                                  "COALESCE(Teillast_Weg, Teillastkurve_a, Verdichterregelung) IS NOT NULL"));
            Assert.Equal(projektVorher, Abzug("Tab_Kaeltemaschine", alle: true));
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe3));
            // Die Prüfsumme ist neu gebildet: der gepflegte Satz weicht ab, alle übrigen gleichen der gelieferten.
            Assert.NotEqual(pruefsummen, Pruefsummen());
            Assert.Equal(Zeilen(pruefsummen).Where(z => !z.StartsWith(GEPFLEGT, StringComparison.Ordinal)),
                         Zeilen(Pruefsummen()).Where(z => !z.StartsWith(GEPFLEGT, StringComparison.Ordinal)));

            KaeltemaschinenTypkennfelder.Ergaenzungsergebnis zweiter = KaeltemaschinenTypkennfelder.ErgaenzenMitZaehlung();
            Assert.Equal(new KaeltemaschinenTypkennfelder.Ergaenzungsergebnis(0, 34), zweiter);
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

        /// <summary>Bedingung: ein Katalogsatz, der kein ausgeliefertes Typkennfeld ist.</summary>
        private const string NICHT_TYPKENNFELD = "(ReadOnly = 0 OR Katalog_Schluessel IS NULL OR Katalog_Schluessel NOT LIKE 'KM:TYPKENNFELD%')";

        /// <summary>Schlüssel und Prüfsumme der ausgelieferten Katalogsätze, je Zeile „Schlüssel|Prüfsumme“.</summary>
        private static string Pruefsummen()
        {
            var dt = DataRepository.GetDataTable("SELECT Katalog_Schluessel, Katalog_Pruefsumme FROM Tab_Kaeltemaschine_STAMM " +
                                                 "WHERE ReadOnly = 1 ORDER BY Katalog_Schluessel");
            return string.Join("\n", dt.Rows.Cast<System.Data.DataRow>()
                .Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture) + "|" + Convert.ToString(r[1], CultureInfo.InvariantCulture)));
        }

        private static IEnumerable<string> Zeilen(string text) => text.Split('\n');

        /// <summary>
        /// Alle Zeilen einer Tabelle als Text — ohne die Spalten des Schritts, mit <paramref name="alle"/> samt ihnen — zum
        /// Vergleich vorher/nachher.
        /// </summary>
        private static string Abzug(string tabelle, bool alle = false)
        {
            var spalten = DataRepository.SpaltenVonTabelle(tabelle)
                .Where(s => alle || !KaeltemaschineTeillastSchema.EINGABESPALTEN.Contains(s)).ToList();
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
