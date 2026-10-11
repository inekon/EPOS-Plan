using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schemaschritt des Kältemittels als Stammdatum</b> (<see cref="KaeltemittelSchema"/>; KM4-E1, Entscheid E123):
    /// die Kältemitteltabelle samt Saat und Register „KMT“ vor „KM“, der Verweis <c>ID_Kaeltemittel</c> an Katalog und
    /// Projektkopie der Kältemaschine, das Inbetriebnahmejahr am Projekt und die Zuordnung des Textbestands.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltemittelSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Prüfsummen der Auslieferungssätze 1 bis 3 vor dem Schritt (Anfang der Summe, Testdatenbank 215).</summary>
        private static readonly string[] SUMMEN_VORHER = { "7445209f55b1", "3d365dcd6e64", "a2723cb4378b" };

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_216_das_Ziel_und_die_Paketanhebung_fuehrt_Import()
        {
            Assert.Equal(216, KaeltemittelSchema.SCHRITT);
            Assert.Equal(VorheizenSchema.SCHRITT + 1, KaeltemittelSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KaeltemittelSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KaeltemittelSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Import, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Das_Register_fuehrt_KMT_vor_KM_mit_dem_Code_als_Namen_und_dem_Verweis()
        {
            List<Katalogtabelle> stufe3 = Katalogfassung.Stufe3.ToList();
            int kmt = stufe3.FindIndex(t => t.Kuerzel == "KMT");
            int km = stufe3.FindIndex(t => t.Kuerzel == "KM");
            Assert.True(kmt >= 0 && kmt < km);
            Katalogtabelle t = stufe3[kmt];
            Assert.Equal(KaeltemittelSchema.TAB_STAMM, t.Tabelle);
            Assert.Equal(3, t.Stufe);
            Assert.Equal(new[] { "Code" }, t.Namensspalten);
            Assert.Empty(t.Kinder);
            Assert.Empty(t.Verweise);
            Assert.Equal(new[]
            {
                "Code", "Bezeichnung", "Gruppe", "GWP", "GWP_Quelle", "F_Gas", "Natuerlich", "Sicherheitsgruppe", "PFAS_Status",
                "Status", "Quelle", "Sortierung"
            }, t.Fachspalten);
            Assert.Equal("KMT:R1234ZE_E", Katalogfassung.Schluesselstamm(t, "R1234ze(E)"));
            Assert.Equal("KMT:R1233ZD_E", Katalogfassung.Schluesselstamm(t, "R1233zd(E)"));

            Katalogtabelle m = stufe3[km];
            Katalogverweis v = Assert.Single(m.Verweise);
            Assert.Equal(("ID_Kaeltemittel", "Tab_Kaeltemittel_STAMM", "Code"), (v.Spalte, v.Zieltabelle, v.Zielspalte));
            Assert.Equal("ID_Kaeltemittel", KaeltemaschineSchema.Fachspalten.Last());
            Assert.Equal(26, KaeltemaschineSchema.Fachspalten.Length);

            KatalogDefinition d = KatalogRegistry.FindeTabelle(KaeltemittelSchema.TAB_STAMM);
            Assert.NotNull(d);
            Assert.Equal("Code", d.NamensSpalte);
            Assert.True(d.VerwendungSperrt);
            Assert.Equal(2, d.VerwendungsPruefungen.Length);
        }

        [Fact]
        public void Die_Saat_fuehrt_achtzehn_Codes_mit_den_zehn_der_Waermepumpe()
        {
            Assert.Equal(18, KaeltemittelSchema.SAAT.Count);
            Assert.Equal(18, KaeltemittelSchema.SAAT.Select(k => k.Code.ToUpperInvariant()).Distinct().Count());
            string[] klappliste = { "R410A", "R32", "R290", "R744", "R134a", "R1234ze(E)", "R407C", "R454C", "R455A", "R1233zd(E)" };
            foreach (string c in klappliste)
                Assert.Contains(KaeltemittelSchema.SAAT, k => string.Equals(k.Code, c, StringComparison.Ordinal));
            Assert.DoesNotContain(KaeltemittelSchema.SAAT, k => k.Code == "SONSTIGES" || k.Code == "R1234ze");
            Assert.All(KaeltemittelSchema.SAAT, k =>
            {
                Assert.True(k.Code.Length <= KaeltemittelSchema.CODE_MAX);
                Assert.True(k.Gwp >= 0);
                Assert.True(k.GwpQuelle.Length <= KaeltemittelSchema.TEXT_MAX);
                Assert.True(KaeltemittelSchema.QuelleDer(k).Length <= KaeltemittelSchema.TEXT_MAX);
                Assert.Contains(k.Gruppe, DbWerte.KAELTEMITTEL_GRUPPEN);
                Assert.Contains(k.PfasStatus, DbWerte.KAELTEMITTEL_PFAS_STATUS);
                Assert.Equal(k.Gruppe == DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, k.Natuerlich);
                Assert.NotEqual(k.FGas, k.Natuerlich);
            });
            Assert.Equal(675, KaeltemittelSchema.SAAT.Single(k => k.Code == "R32").Gwp);
            Assert.Equal(1.37, KaeltemittelSchema.SAAT.Single(k => k.Code == "R1234ze(E)").Gwp);
        }

        [Fact]
        public void Die_Pruefklauseln_halten_die_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, KaeltemittelSchema.SqlCreateStamm());
            Ausfuehren(c, "INSERT INTO Tab_Kaeltemittel_STAMM (Code) VALUES ('R32')");
            string t = KaeltemittelSchema.TAB_STAMM;
            Pruefe(c, t, "GWP", new[] { "NULL", "0", "675.5" }, new[] { "-1", "'viel'" });
            Pruefe(c, t, "F_Gas", new[] { "NULL", "0", "1" }, new[] { "2", "-1" });
            Pruefe(c, t, "Natuerlich", new[] { "NULL", "0", "1" }, new[] { "2" });
            Pruefe(c, t, "Gruppe", new[] { "NULL", "'HFKW'", "'HFO'", "'HFCKW'", "'GEMISCH'", "'NATUERLICH'", "'SONSTIGE'" },
                   new[] { "'hfkw'", "'X'" });
            Pruefe(c, t, "Sicherheitsgruppe", new[] { "NULL", "'A1'", "'A2L'", "'A2'", "'A3'", "'B1'", "'B2L'", "'B2'", "'B3'" },
                   new[] { "'A4'", "'a1'" });
            Pruefe(c, t, "PFAS_Status", new[] { "NULL", "'PFAS'", "'BESTANDTEIL'", "'KEIN'" }, new[] { "'ENTHAELT'", "'X'" });
            Pruefe(c, t, "Status", new[] { "NULL", "'GESICHERT'", "'VORLAEUFIG'", "'PROGNOSE'", "'ABGEKUENDIGT'" }, new[] { "'X'" });
            Pruefe(c, t, "Bezeichnung", new[] { "NULL", "'" + new string('a', 120) + "'" }, new[] { "'" + new string('a', 121) + "'" });
            Pruefe(c, t, "Code", new[] { "'R290'" }, new[] { "NULL", "''", "'" + new string('R', 21) + "'" });
            Assert.True(Wirft(c, "INSERT INTO Tab_Kaeltemittel_STAMM (Code) VALUES ('r290')"));   // UNIQUE ohne Groß/Klein
            Assert.True(Wirft(c, "INSERT INTO Tab_Kaeltemittel_STAMM (Code, ReadOnly) VALUES ('R717', 2)"));

            Ausfuehren(c, "CREATE TABLE Tab_Projekt (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Projekt (ID) VALUES (1)");
            Ausfuehren(c, KaeltemittelSchema.Anlegen(KaeltemittelSchema.SPALTEN.Single(s => s.Spalte == "Inbetriebnahmejahr")));
            Pruefe(c, "Tab_Projekt", "Inbetriebnahmejahr", new[] { "NULL", "2000", "2027", "2100" }, new[] { "1999", "2101", "'2027a'" });
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank, die Zuordnung und die Rundreise
        // =============================================================================

        /// <summary>Die Testkopie steht auf dem Schritt: Tabelle, achtzehn Codes VORLAEUFIG, Spalten leer bis auf die Zuordnung.</summary>
        [Fact]
        public void Die_Testkopie_fuehrt_Tabelle_Saat_und_Spalten()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KaeltemittelSchema.SCHRITT);
            Assert.True(KaeltemittelSchema.Vollstaendig());
            Assert.Contains("STRICT", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE name = 'Tab_Kaeltemittel_STAMM'"), CultureInfo.InvariantCulture));
            Assert.Equal(18L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemittel_STAMM"));
            Assert.Equal(18L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemittel_STAMM WHERE ReadOnly = 1 AND Status = 'VORLAEUFIG' " +
                                   "AND Sicherheitsgruppe IS NULL AND GWP IS NOT NULL AND GWP_Quelle IS NOT NULL " +
                                   "AND Katalog_Schluessel LIKE 'KMT:%' AND Katalog_Pruefsumme IS NOT NULL"));
            Assert.Equal("KMT:R1234ZE_E", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Katalog_Schluessel FROM Tab_Kaeltemittel_STAMM WHERE Code = 'r1234ze(e)'"), CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE Inbetriebnahmejahr IS NOT NULL"));
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe3));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        /// <summary>
        /// Die Zuordnung: die drei Projektkopien (1055, 1059, 1063) tragen die ID von R513A bei unverändertem Text, die
        /// Auslieferungssätze 1 bis 3 ihre ID mit neuer Prüfsumme (Satz 3 „R1234ze“ → R1234ze(E)), die 34 Typkennfelder
        /// bleiben leer.
        /// </summary>
        [Fact]
        public void Die_Zuordnung_setzt_drei_Projektkopien_und_drei_Auslieferungssaetze()
        {
            if (!_db.Vorhanden) return;
            long r513a = Id("R513A");
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt IN (1055, 1059, 1063) " +
                                  "AND Kaeltemittel = 'R513A' AND ID_Kaeltemittel = " + r513a));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Kaeltemittel IS NULL"));

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Kaeltemittel, ID_Kaeltemittel, Katalog_Pruefsumme FROM Tab_Kaeltemaschine_STAMM " +
                "WHERE ID_Kaeltemittel IS NOT NULL ORDER BY ID");
            Assert.Equal(3, dt.Rows.Count);
            string[] text = { "R32", "R513A", "R1234ze" };
            long[] ziel = { Id("R32"), r513a, Id("R1234ze(E)") };
            for (int i = 0; i < 3; i++)
            {
                DataRow r = dt.Rows[i];
                Assert.Equal(i + 1L, Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture));
                Assert.Equal(text[i], Convert.ToString(r["Kaeltemittel"], CultureInfo.InvariantCulture));
                Assert.Equal(ziel[i], Convert.ToInt64(r["ID_Kaeltemittel"], CultureInfo.InvariantCulture));
                string summe = Convert.ToString(r["Katalog_Pruefsumme"], CultureInfo.InvariantCulture);
                Assert.Equal(64, summe.Length);
                Assert.False(summe.StartsWith(SUMMEN_VORHER[i], StringComparison.Ordinal), "Satz " + (i + 1) + " trägt die alte Summe");
            }
            Assert.Equal(34L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE ID_Kaeltemittel IS NULL " +
                                   "AND (Kaeltemittel IS NULL OR Kaeltemittel = '')"));
        }

        /// <summary>Ein zweiter Lauf ändert nichts — keine Spalte, kein Code, keine Zuordnung, keine Prüfsumme.</summary>
        [Fact]
        public void Der_Schritt_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            string vorher = Pruefsummen();
            var bericht = new List<string>();
            KaeltemittelSchema.Laufergebnis e = KaeltemittelSchema.Ausfuehren(bericht);
            Assert.Equal(new KaeltemittelSchema.Laufergebnis(0, 0, 0, 0, 0), e);
            Assert.Contains(bericht, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
            Assert.Equal(vorher, Pruefsummen());
        }

        /// <summary>
        /// Freitext an der Projektkopie: ein Treffer ohne Groß/Klein und mit Leerzeichen bekommt die ID; „R1234ze“ ohne
        /// Isomer und ein unbekannter Text bleiben ohne ID (nie raten), der Text bleibt.
        /// </summary>
        [Fact]
        public void Ein_unbekannter_Text_bleibt_ohne_ID()
        {
            if (!_db.Vorhanden) return;
            long basis = Zahl("SELECT MAX(ID) FROM Tab_Kaeltemaschine");
            foreach (string w in new[] { " r32 ", "R1234ze", "Kältemittel X", "" })
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO Tab_Kaeltemaschine (ID_Projekt, Bezeichner, Kaeltemittel) VALUES (1055, 'Probe', ?)",
                    new DbParam("?", w));
            KaeltemittelSchema.Laufergebnis e = KaeltemittelSchema.Ausfuehren(null);
            Assert.Equal(1, e.ProjektZugeordnet);
            Assert.Equal(0, e.KatalogZugeordnet);
            Assert.Equal(Id("R32"), Zahl("SELECT ID_Kaeltemittel FROM Tab_Kaeltemaschine WHERE ID = " + (basis + 1)));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID > " + basis + " AND ID_Kaeltemittel IS NULL"));
            Assert.Equal(" r32 ", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Kaeltemittel FROM Tab_Kaeltemaschine WHERE ID = " + (basis + 1)), CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Rundreise: aus dem Stand davor legt der Schritt Tabelle, Spalten und Katalogspalten an, sät achtzehn Codes,
        /// ordnet drei Katalogsätze und drei Projektkopien zu und bildet drei Prüfsummen neu — dieselben wie in der
        /// gelieferten Testdatenbank.
        /// </summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_trifft_die_gelieferten_Pruefsummen()
        {
            if (!_db.Vorhanden) return;
            string summenNachher = Pruefsummen();
            DataRepository.ExecuteNonQuery("DROP TABLE Tab_Kaeltemittel_STAMM");
            foreach ((string tabelle, string spalte, string _) in KaeltemittelSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            Assert.False(KaeltemittelSchema.Vollstaendig());
            Assert.Equal(-1, KaeltemittelSchema.ZeilenMitId(KaeltemaschineSchema.TAB_STAMM));
            // Der Stand davor: die Prüfsummen ohne Verweis (die Schritte der Kältemaschine stempeln allein ihren Katalog).
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine_STAMM SET Katalog_Pruefsumme = NULL WHERE ReadOnly = 1");
            KatalogSchluesselSaat.Ausfuehren(null, Katalogfassung.Stufe3);
            Assert.True(KaeltemaschineSchema.Vollstaendig());
            for (int i = 0; i < 3; i++)
                Assert.StartsWith(SUMMEN_VORHER[i], Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT Katalog_Pruefsumme FROM Tab_Kaeltemaschine_STAMM WHERE ID = " + (i + 1)), CultureInfo.InvariantCulture));

            KaeltemittelSchema.Laufergebnis e = KaeltemittelSchema.Ausfuehren(new List<string>());
            Assert.Equal(8, e.Angelegt);              // Tabelle, drei Spalten, drei Katalogspalten, Teilindex
            Assert.Equal(18, e.Saat);
            Assert.Equal(3, e.KatalogZugeordnet);
            Assert.Equal(3, e.ProjektZugeordnet);
            Assert.Equal(3, e.PruefsummenNeu);
            Assert.True(KaeltemittelSchema.Vollstaendig());
            Assert.Equal(summenNachher, Pruefsummen());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static long Id(string code)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Kaeltemittel_STAMM WHERE Code = ?",
                                                            new DbParam("?", code)), CultureInfo.InvariantCulture);

        private static string Pruefsummen()
            => string.Join("\n", DataRepository.GetDataTable(
                    "SELECT 'KM', ID, Katalog_Pruefsumme FROM Tab_Kaeltemaschine_STAMM UNION ALL " +
                    "SELECT 'KMT', ID, Katalog_Schluessel || '=' || Katalog_Pruefsumme FROM Tab_Kaeltemittel_STAMM ORDER BY 1, 2").Rows
                .Cast<DataRow>().Select(r => r[0] + ":" + r[1] + "=" + r[2]));

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static void Pruefe(SqliteConnection c, string tabelle, string spalte, string[] gut, string[] schlecht)
        {
            foreach (string w in gut)
                Assert.False(Wirft(c, "UPDATE \"" + tabelle + "\" SET \"" + spalte + "\" = " + w), tabelle + "." + spalte + " = " + w);
            foreach (string w in schlecht)
                Assert.True(Wirft(c, "UPDATE \"" + tabelle + "\" SET \"" + spalte + "\" = " + w), tabelle + "." + spalte + " = " + w);
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

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
