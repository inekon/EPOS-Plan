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
    /// <b>Der Schemaschritt der Katalogfelder der Kälteerzeuger</b> (<see cref="KaelteKatalogfelderSchema"/>; Stufe K-A,
    /// Entscheid E118): Geräteart, GWP, Füllmenge und saisonale Kennzahl an Katalog und Projektkopie — zehn Spalten; die
    /// Geräteart nach der Rückkühlart rückgefüllt, die Prüfsumme jedes stimmigen Katalogsatzes neu gebildet.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaelteKatalogfelderSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static readonly string[] FELDER =
        {
            "Geraeteart", "Kaeltemittel_GWP", "Kaeltemittel_Fuellmenge_kg", "Saisonkennzahl_Art", "Saisonkennzahl",
        };

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_211_das_Ziel_und_die_Paketanhebung_fuehrt_Katalog()
        {
            Assert.Equal(211, KaelteKatalogfelderSchema.SCHRITT);
            Assert.Equal(KaeltemaschineTeillastSchema.SCHRITT + 1, KaelteKatalogfelderSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KaelteKatalogfelderSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KaelteKatalogfelderSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Der_Schritt_fuehrt_zehn_Spalten_und_haengt_sie_an_die_Fachspalten()
        {
            Assert.Equal(10, KaelteKatalogfelderSchema.SPALTEN.Count);
            Assert.Equal(FELDER, KaelteKatalogfelderSchema.SPALTEN.Where(x => x.Tabelle == "Tab_Kaeltemaschine_STAMM").Select(x => x.Spalte));
            Assert.Equal(FELDER, KaelteKatalogfelderSchema.SPALTEN.Where(x => x.Tabelle == "Tab_Kaeltemaschine").Select(x => x.Spalte));
            Assert.All(KaelteKatalogfelderSchema.SPALTEN, x => Assert.DoesNotContain("NOT NULL", x.Typ, StringComparison.Ordinal));
            Assert.All(KaelteKatalogfelderSchema.SPALTEN, x => Assert.DoesNotContain("DEFAULT", x.Typ, StringComparison.Ordinal));
            Assert.Equal(FELDER, KaeltemaschineSchema.Fachspalten.Skip(20));
            Assert.Equal(FELDER, Katalogfassung.Tabelle(KaeltemaschineSchema.TAB_STAMM).Fachspalten.Skip(20));
        }

        /// <summary>Die Geräteart kennt die Folgestufen schon: Split, Multisplit und VRF (K-D, K-E) und Absorption (K-G).</summary>
        [Fact]
        public void Die_Geraeteart_kennt_Split_Multisplit_und_VRF()
        {
            Assert.Equal(new[] { "KWS_LUFT", "KWS_WASSER", "KWS_FREIKUEHLUNG", "SPLIT", "MULTISPLIT", "VRF", "ABSORPTION" },
                         KaelteKatalogfelderSchema.GERAETEARTEN);
            Assert.Equal(new[] { "SEER", "ETA_S_C" }, KaelteKatalogfelderSchema.SAISON_ARTEN);
            foreach (string art in KaelteKatalogfelderSchema.GERAETEARTEN)
                Assert.NotEqual(art, KaeltemaschineStammCtrl.GeraeteartText(art));
        }

        [Theory]
        [InlineData("LUFT", "KWS_LUFT")]
        [InlineData("WASSER", "KWS_WASSER")]
        [InlineData("TROCKENKUEHLER", "KWS_WASSER")]
        [InlineData("NASSKUEHLER", "KWS_WASSER")]
        [InlineData(null, "KWS_LUFT")]
        public void Die_Rueckfuellregel_folgt_der_Rueckkuehlart(string rueckkuehlart, string erwartet)
        {
            Assert.Equal(erwartet, KaelteKatalogfelderSchema.GeraeteartAusRueckkuehlart(rueckkuehlart));
            Assert.Equal(erwartet, KaelteKatalogfelderSchema.GeraeteartWirksam(null, rueckkuehlart));
            Assert.Equal("VRF", KaelteKatalogfelderSchema.GeraeteartWirksam("VRF", rueckkuehlart));
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: ein Wert außerhalb wird abgewiesen, NULL angenommen.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_die_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            foreach (string t in KaelteKatalogfelderSchema.Voraussetzungen())
            {
                Ausfuehren(c, "CREATE TABLE \"" + t + "\" (ID INTEGER PRIMARY KEY, Rueckkuehlart TEXT) STRICT");
                Ausfuehren(c, "INSERT INTO \"" + t + "\" (ID) VALUES (1)");
            }
            foreach ((string Tabelle, string Spalte, string Typ) s in KaelteKatalogfelderSchema.SPALTEN)
                Ausfuehren(c, KaelteKatalogfelderSchema.Anlegen(s));
            foreach (string t in KaelteKatalogfelderSchema.Voraussetzungen())
            {
                Pruefe(c, t, "Geraeteart", new[] { "NULL", "'KWS_LUFT'", "'KWS_WASSER'", "'SPLIT'", "'MULTISPLIT'", "'VRF'", "'ABSORPTION'" },
                       new[] { "'kws_luft'", "''", "'LUFT'", "1" });
                Pruefe(c, t, "Kaeltemittel_GWP", new[] { "NULL", "0", "675", "3922" }, new[] { "-1", "'x'" });
                Pruefe(c, t, "Kaeltemittel_Fuellmenge_kg", new[] { "NULL", "0.5", "120" }, new[] { "0", "-1" });
                Pruefe(c, t, "Saisonkennzahl_Art", new[] { "NULL", "'SEER'", "'ETA_S_C'" }, new[] { "'seer'", "''", "'EER'" });
                Pruefe(c, t, "Saisonkennzahl", new[] { "NULL", "5.6", "220" }, new[] { "0", "-1" });
            }
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank, die Rückfüllung und die Rundreise
        // =============================================================================

        /// <summary>Die Testkopie steht auf dem Schritt: 12 Sätze KWS_LUFT, 25 KWS_WASSER, die drei Projektkopien KWS_WASSER.</summary>
        [Fact]
        public void Die_Testkopie_ist_rueckgefuellt_und_ohne_Luecke()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KaelteKatalogfelderSchema.SCHRITT);
            Assert.True(KaelteKatalogfelderSchema.Vollstaendig());
            Assert.Equal(0, KaelteKatalogfelderSchema.ZeilenOhneGeraeteart());
            Assert.Equal(12L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Geraeteart = 'KWS_LUFT' AND Rueckkuehlart = 'LUFT'"));
            Assert.Equal(25L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Geraeteart = 'KWS_WASSER' AND Rueckkuehlart <> 'LUFT'"));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE Geraeteart = 'KWS_WASSER'"));
            foreach (string t in KaelteKatalogfelderSchema.Voraussetzungen())
                foreach (string s in FELDER.Skip(1))
                    Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"" + s + "\" IS NOT NULL"));
            // Die Auslieferung bleibt ohne Firma (Hersteller nur bei Anwenderimporten, E118).
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE ReadOnly = 1 AND Firma IS NOT NULL"));
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe3));
        }

        /// <summary>
        /// Rundreise: aus dem Stand davor legt der Schritt die zehn Spalten an, füllt 37 Katalogsätze und drei Projektkopien
        /// zurück und bildet 37 Prüfsummen neu — dieselben wie in der gelieferten Testdatenbank; zweimal = nichts.
        /// </summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_fuellt_zurueck_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            string summenVorher = Pruefsummen();
            foreach ((string tabelle, string spalte, string _) in KaelteKatalogfelderSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            Assert.False(KaelteKatalogfelderSchema.Vollstaendig());
            Assert.Equal(-1, KaelteKatalogfelderSchema.ZeilenOhneGeraeteart());
            // Die gespeicherten Prüfsummen stimmen ohne die Spalten nicht mehr: Die Prüfung vor der Rückfüllung sieht
            // die leere Geräteart. Ein Satz, dessen Summe nicht stimmt, bekommt keine neue - also vorher neu bilden.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine_STAMM SET Katalog_Pruefsumme = NULL WHERE ReadOnly = 1");
            KatalogSchluesselSaat.Ausfuehren(null, Katalogfassung.Stufe3);

            var bericht = new List<string>();
            KaelteKatalogfelderSchema.Laufergebnis e = KaelteKatalogfelderSchema.Ausfuehren(bericht);
            Assert.Equal(10, e.Angelegt);
            Assert.Equal(37, e.KatalogRueckgefuellt);
            Assert.Equal(3, e.ProjektRueckgefuellt);
            Assert.Equal(37, e.PruefsummenNeu);
            Assert.True(KaelteKatalogfelderSchema.Vollstaendig());
            Assert.Equal(0, KaelteKatalogfelderSchema.ZeilenOhneGeraeteart());
            Assert.Equal(summenVorher, Pruefsummen());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            var zweiter = new List<string>();
            KaelteKatalogfelderSchema.Laufergebnis z2 = KaelteKatalogfelderSchema.Ausfuehren(zweiter);
            Assert.Equal(new KaelteKatalogfelderSchema.Laufergebnis(0, 0, 0, 0), z2);
            Assert.Contains(zweiter, z => z.Contains("nichts anzulegen", StringComparison.Ordinal));
        }

        /// <summary>Ein Satz, dessen Prüfsumme vorher nicht stimmte (geändert), bleibt als geändert erkennbar.</summary>
        [Fact]
        public void Eine_unstimmige_Pruefsumme_bleibt_unstimmig()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT MIN(ID) FROM Tab_Kaeltemaschine_STAMM WHERE ReadOnly = 1");
            string fremd = new string('a', 64);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine_STAMM SET Geraeteart = NULL, Katalog_Pruefsumme = '" + fremd +
                                           "' WHERE ID = " + id.ToString(CultureInfo.InvariantCulture));
            KaelteKatalogfelderSchema.Laufergebnis e = KaelteKatalogfelderSchema.Ausfuehren(null);
            Assert.Equal(1, e.KatalogRueckgefuellt);
            Assert.Equal(0, e.PruefsummenNeu);
            Assert.Equal(fremd, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Katalog_Pruefsumme FROM Tab_Kaeltemaschine_STAMM WHERE ID = " + id.ToString(CultureInfo.InvariantCulture)),
                CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static string Pruefsummen()
            => string.Join("\n", DataRepository.GetDataTable(
                    "SELECT ID, Katalog_Pruefsumme FROM Tab_Kaeltemaschine_STAMM ORDER BY ID").Rows
                .Cast<System.Data.DataRow>().Select(r => r[0] + "=" + r[1]));

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
