using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BK-3c</b> — Schemaschritt <see cref="BauteilkatalogHaustechnikSchema"/> (E134): die Klasse Haustechnik in der
    /// Prüfklausel von <c>Klasse_Code</c> an den vier Kopftabellen, Neubau mit Erhalt von Zeilen, IDs, Indizes und STRICT,
    /// wiederholbar; dazu Ressourcen und der Klassenfilter. Alle Werte sind synthetisch.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BauteilkatalogHaustechnikSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static long Zahl(string sql, params DbParam[] p) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(sql, p) ?? 0L, CultureInfo.InvariantCulture);

        private static string Sql(string tabelle) =>
            Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                          new DbParam("p1", tabelle)), CultureInfo.InvariantCulture);

        /// <summary>Setzt einen Katalogsatz auf Sonstige/Haustechnik — in einem eigenen Vorgang, der eine Verletzung wirft.</summary>
        private static int Haustechnik(long id)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                int n = v.Ausfuehren("UPDATE \"Tab_Baustoff_STAMM\" SET \"Klasse_Code\" = ?, \"Gruppe_Code\" = ? WHERE \"ID\" = ?",
                                     new DbParam("p1", BauteilkatalogHaustechnikSchema.KLASSE), new DbParam("p2", "SONSTIGE"),
                                     new DbParam("p3", id));
                v.Commit();
                return n;
            }
        }

        // ---- ohne Datenbank -----------------------------------------------------------------

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_Ddl()
        {
            Assert.Equal(222, BauteilkatalogHaustechnikSchema.SCHRITT);
            Assert.Equal(BauteilkatalogSchema.SCHRITT + 1, BauteilkatalogHaustechnikSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= BauteilkatalogHaustechnikSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == BauteilkatalogHaustechnikSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
        }

        [Fact]
        public void Die_Klasse_steht_zuletzt_in_der_Gruppe_Sonstige_beider_Kataloge_und_die_Klausel_waechst_nur_um_sie()
        {
            Assert.Equal(("SONSTIGE", "HAUSTECHNIK"), BauteilkatalogSchema.BAUSTOFF_KLASSEN.Last());
            Assert.Equal(("SONSTIGE", "HAUSTECHNIK"), BauteilkatalogSchema.AUFBAU_KLASSEN.Last());
            foreach (string t in BauteilkatalogHaustechnikSchema.Voraussetzungen())
            {
                string alt = BauteilkatalogHaustechnikSchema.KlauselAlt(t), neu = BauteilkatalogHaustechnikSchema.KlauselNeu(t);
                Assert.Equal(alt.Replace("'))", "','HAUSTECHNIK'))"), neu);
                Assert.DoesNotContain("HAUSTECHNIK", alt);
            }
            Assert.Contains("HAUSTECHNIK", Bauteilkatalog.Klassen(true, "SONSTIGE"));
            Assert.Contains("HAUSTECHNIK", Bauteilkatalog.Klassen(false, "SONSTIGE"));
            Assert.Null(Bauteilkatalog.GliederungPruefen(true, "SONSTIGE", "HAUSTECHNIK"));
            Assert.Null(Bauteilkatalog.GliederungPruefen(false, "SONSTIGE", "HAUSTECHNIK"));
            Assert.Equal("HAUSTECHNIK", BauteilkatalogImport.KlasseHaustechnik(true));
            Assert.Equal("HAUSTECHNIK", BauteilkatalogImport.KlasseHaustechnik(false));
        }

        [Fact]
        public void Der_Anzeigetext_steht_in_beiden_Sprachen()
        {
            var r = WindowsFormsApplication1.MyResource.Resource.ResourceManager;
            foreach (string s in new[] { "BKAT_BST_KLASSE_HAUSTECHNIK", "BKAT_BTA_KLASSE_HAUSTECHNIK" })
            {
                Assert.Equal("Haustechnik", r.GetString(s, new CultureInfo("de-DE")));
                Assert.Equal("Building services", r.GetString(s, new CultureInfo("en-US")));
            }
        }

        [Fact]
        public void Der_Klassenfilter_blendet_Haustechnik_mit_der_Verneinung_aus()
        {
            Katalogfilterprofil bst = Katalogfilterprofil.FuerBaustoff();
            string ht = Bauteilkatalog.KlasseText(true, "HAUSTECHNIK"), sonst = Bauteilkatalog.KlasseText(true, "STOFF_SONSTIGE");
            Assert.NotEqual("HAUSTECHNIK", ht);
            var zeilen = new List<Katalogfilterzeile>();
            for (int i = 1; i <= 6; i++)
                zeilen.Add(new Katalogfilterzeile(i, "Stoff " + i)
                    .MitText(Katalogfilterprofil.SpBezeichner, "Stoff " + i)
                    .MitText(Katalogfilterprofil.SpGruppe, Bauteilkatalog.GruppeText(true, "SONSTIGE"))
                    .MitText(Katalogfilterprofil.SpKlasse, i % 3 == 0 ? ht : sonst));
            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpKlasse, Katalogfilterprofil.AUSDRUCK_NICHT + ht);
            IReadOnlyList<Katalogfilterzeile> ohne = Katalogfilter.Anwenden(bst, zeilen, stand);
            Assert.Equal(4, ohne.Count);
            Assert.All(ohne, z => Assert.Equal(sonst, z.Text(Katalogfilterprofil.SpKlasse)));
            stand.Setzen(Katalogfilterprofil.SpKlasse, ht);
            Assert.Equal(2, Katalogfilter.Anwenden(bst, zeilen, stand).Count);
        }

        // ---- mit der Testdatenbank ----------------------------------------------------------

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= BauteilkatalogHaustechnikSchema.SCHRITT);
            Assert.True(BauteilkatalogHaustechnikSchema.Vollstaendig());
            Assert.Equal(0, BauteilkatalogHaustechnikSchema.Ausfuehren(null));
            foreach (string t in BauteilkatalogHaustechnikSchema.Voraussetzungen())
                Assert.Null(BauteilkatalogHaustechnikSchema.Zieltext(t, Sql(t)));
        }

        [Fact]
        public void Der_Schritt_baut_einen_Stand_221_um_erhaelt_Zeilen_IDs_Indizes_und_STRICT_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyList<string> tabellen = BauteilkatalogHaustechnikSchema.Voraussetzungen();
            var abdruck = new Dictionary<string, (long Zeilen, long IdSumme, long Objekte, long Seq)>();
            foreach (string t in tabellen)
                abdruck[t] = (Zahl("SELECT COUNT(*) FROM \"" + t + "\""), Zahl("SELECT COALESCE(SUM(\"ID\"), 0) FROM \"" + t + "\""),
                              Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type IN ('index','trigger') AND tbl_name = ?", new DbParam("p1", t)),
                              Zahl("SELECT COALESCE(MAX(seq), 0) FROM sqlite_sequence WHERE name = ?", new DbParam("p1", t)));

            // Stand 221 nachstellen: die Klausel ohne Haustechnik, mit dem Rezept des Schritts.
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                foreach (string t in tabellen)
                {
                    string text = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                            new DbParam("p1", t)), CultureInfo.InvariantCulture);
                    TwwBezugsartSchema.Neubau(v, t, text.Replace(BauteilkatalogHaustechnikSchema.KlauselNeu(t),
                                                                 BauteilkatalogHaustechnikSchema.KlauselAlt(t)), t + "_Probe_alt");
                }
                v.Commit();
            }
            Assert.Equal(4, BauteilkatalogHaustechnikSchema.Offen());
            long id = Zahl("SELECT MIN(\"ID\") FROM \"Tab_Baustoff_STAMM\"");
            Assert.ThrowsAny<Exception>(() => Haustechnik(id));

            var bericht = new List<string>();
            Assert.Equal(4, BauteilkatalogHaustechnikSchema.Ausfuehren(bericht));
            Assert.True(BauteilkatalogHaustechnikSchema.Vollstaendig());
            Assert.Contains(bericht, z => z.Contains("foreign_key_check leer", StringComparison.Ordinal));
            foreach (string t in tabellen)
            {
                string sql = Sql(t);
                Assert.Contains(BauteilkatalogHaustechnikSchema.KlauselNeu(t), sql, StringComparison.Ordinal);
                Assert.EndsWith(") STRICT", sql.TrimEnd(), StringComparison.Ordinal);
                Assert.Equal(abdruck[t], (Zahl("SELECT COUNT(*) FROM \"" + t + "\""), Zahl("SELECT COALESCE(SUM(\"ID\"), 0) FROM \"" + t + "\""),
                                          Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type IN ('index','trigger') AND tbl_name = ?", new DbParam("p1", t)),
                                          Zahl("SELECT COALESCE(MAX(seq), 0) FROM sqlite_sequence WHERE name = ?", new DbParam("p1", t))));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE name = ?", new DbParam("p1", BauteilkatalogHaustechnikSchema.Hilfsname(t))));
            }
            Assert.Equal(1, Haustechnik(id));
            Assert.Equal(0, BauteilkatalogHaustechnikSchema.Ausfuehren(null));
        }
    }
}
