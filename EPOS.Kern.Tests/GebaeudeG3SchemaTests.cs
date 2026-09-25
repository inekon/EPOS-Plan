using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Stufe G3 der Gebäudesimulation, Welle B — die REGELN der Schritte S-A bis S-C ohne
    /// Datenbank: Nummern, Wertlisten, DDL-Texte und die Baustoffsaat
    /// (Softwarearchitektur Gebäudesimulation 2.2, 2.3 und 2.4).
    /// </summary>
    public class GebaeudeG3SchemaRegelTests
    {
        [Fact]
        public void Die_drei_Schritte_folgen_aufeinander_und_der_Zielstand_traegt_sie()
        {
            Assert.Equal(BaustoffSchema.SCHRITT + 1, BauteilaufbauSchema.SCHRITT);
            Assert.Equal(BauteilaufbauSchema.SCHRITT + 1, ZonenSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ZonenSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + ZonenSchema.SCHRITT + ".");
            Assert.True(BaustoffSchema.SCHRITT > 128, "S-A muss hinter den vergebenen Schritten liegen.");
        }

        /// <summary>Die SQL-Wertlisten sind die Listen in <see cref="DbWerte"/> — neun, vier, fünf (W7 bis W9).</summary>
        [Fact]
        public void Die_Wertlisten_der_CHECKs_sind_die_Persistenzwerte()
        {
            Assert.Equal(9, DbWerte.BAUTEILARTEN.Count);
            Assert.Equal(4, DbWerte.RANDBEDINGUNGEN.Count);
            Assert.Equal(5, DbWerte.HERKUENFTE.Count);
            Assert.DoesNotContain("KELLER", DbWerte.RANDBEDINGUNGEN);
            Assert.Contains(DbWerte.BAUTEILART_VORHANGFASSADE, DbWerte.BAUTEILARTEN);
            Assert.Contains(DbWerte.HERKUNFT_KATALOG, DbWerte.HERKUENFTE);

            Assert.Equal(BauteilaufbauSchema.WERTE_BAUTEILART, Liste(DbWerte.BAUTEILARTEN));
            Assert.Equal(ZonenSchema.WERTE_RANDBEDINGUNG, Liste(DbWerte.RANDBEDINGUNGEN));
            Assert.Equal(BaustoffSchema.WERTE_HERKUNFT, Liste(DbWerte.HERKUENFTE));
            Assert.Equal(ZonenSchema.WERTE_UEBERGABE_ART,
                         Liste(new[] { DbWerte.UEBERGABE_IDEAL, DbWerte.UEBERGABE_RADIATOR,
                                       DbWerte.UEBERGABE_FLAECHE, DbWerte.UEBERGABE_KONVEKTOR }));

            foreach (string w in DbWerte.BAUTEILARTEN.Concat(DbWerte.RANDBEDINGUNGEN).Concat(DbWerte.HERKUENFTE))
                Assert.True(w.All(c => c >= 'A' && c <= 'Z'), "Kein ASCII-Grossbuchstabenwert: " + w);
        }

        /// <summary>Die Hausregeln an jeder der acht Tabellen: STRICT, IF NOT EXISTS, AUTOINCREMENT.</summary>
        [Fact]
        public void Jede_Tabelle_ist_STRICT_wiederholbar_und_mit_AUTOINCREMENT()
        {
            var alle = BaustoffSchema.Anweisungen.Concat(BauteilaufbauSchema.Tabellenanweisungen)
                                                  .Concat(ZonenSchema.Tabellenanweisungen).ToList();
            Assert.Equal(8, alle.Count);
            foreach (KeyValuePair<string, string> a in alle)
            {
                Assert.StartsWith("CREATE TABLE IF NOT EXISTS \"" + a.Key + "\" (", a.Value, StringComparison.Ordinal);
                Assert.EndsWith(") STRICT", a.Value, StringComparison.Ordinal);
                Assert.Contains("\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT", a.Value);
            }
            foreach (KeyValuePair<string, string> i in BauteilaufbauSchema.Indexanweisungen.Concat(ZonenSchema.Indexanweisungen))
                Assert.StartsWith("CREATE INDEX IF NOT EXISTS \"" + i.Key + "\"", i.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Kein DDL-DEFAULT auf einem Fachwert: DEFAULT tragen allein die drei Schalter
        /// (<c>ReadOnly</c>, <c>IstLuftschicht</c>, <c>IstBeheizt</c>).
        /// </summary>
        [Fact]
        public void DEFAULT_tragen_nur_die_Schalter()
        {
            var alle = BaustoffSchema.Anweisungen.Concat(BauteilaufbauSchema.Tabellenanweisungen)
                                                  .Concat(ZonenSchema.Tabellenanweisungen);
            foreach (KeyValuePair<string, string> a in alle)
                foreach (string zeile in a.Value.Split('\n').Where(z => z.Contains("DEFAULT")))
                    Assert.True(zeile.Contains("\"ReadOnly\" INTEGER NOT NULL DEFAULT 0") ||
                                zeile.Contains("\"IstLuftschicht\" INTEGER NOT NULL DEFAULT 0") ||
                                zeile.Contains("\"IstBeheizt\" INTEGER NOT NULL DEFAULT 1"),
                                a.Key + ": DEFAULT auf einem Fachwert - " + zeile.Trim());
        }

        /// <summary>
        /// Das Bauteil ohne <c>ID_Nachbarzone</c> (S-G) und ohne <c>IstAussen</c> (W6); die Zone mit
        /// GENAU den Blöcken aus KU-S1 und AK-S1 und keiner weiteren Gebäudespalte der Kopplung (E37).
        /// </summary>
        [Fact]
        public void Bauteil_und_Zone_tragen_genau_die_festgelegten_Spalten()
        {
            Assert.DoesNotContain("ID_Nachbarzone", ZonenSchema.SQL_CREATE_BAUTEIL);
            Assert.DoesNotContain("IstAussen", ZonenSchema.SQL_CREATE_BAUTEIL);
            Assert.DoesNotContain("KELLER", ZonenSchema.SQL_CREATE_BAUTEIL);

            foreach (string s in ZonenSchema.KuehlSpalten.Concat(ZonenSchema.UebergabeSpalten))
                Assert.Contains("\"" + s + "\"", ZonenSchema.SQL_CREATE_ZONE);
            Assert.Contains("\"Kuehlung_Aktiv\" INTEGER CHECK (\"Kuehlung_Aktiv\" IN (0,1)),", ZonenSchema.SQL_CREATE_ZONE);

            // Von AK-S1 nur Art, Exponent und Nennleistung - nichts vom Auslegungspunkt, der Heizkurve
            // oder dem Zeitprogramm; von KU-S1 nichts ausser den vier Eingaben.
            foreach (KeyValuePair<string, string> s in GebaeudeSchema.UEBERGABE_SPALTEN)
                if (!ZonenSchema.UebergabeSpalten.Contains(s.Key))
                    Assert.DoesNotContain("\"" + s.Key + "\"", ZonenSchema.SQL_CREATE_ZONE);
            Assert.DoesNotContain("Kuehl_Vorlauf", ZonenSchema.SQL_CREATE_ZONE);
            Assert.DoesNotContain("Kuehluebergabe", ZonenSchema.SQL_CREATE_ZONE);

            // Die neun Gebäudewertspalten buchstabengetreu, samt Maximaleraumtemperatur.
            Assert.Equal(9, ZonenSchema.GebaeudewertSpalten.Count);
            Assert.Contains("Maximaleraumtemperatur", ZonenSchema.GebaeudewertSpalten);
            Assert.Equal(ZonenSchema.Zonenspalten.Count, ZonenSchema.Zonenspalten.Distinct().Count());
            Assert.Equal(27, ZonenSchema.Zonenspalten.Count);
            Assert.Equal(16, ZonenSchema.Bauteilspalten.Count);
        }

        [Fact]
        public void Die_Kaskade_zeigt_nur_zum_Eltern()
        {
            Assert.Contains("REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE", ZonenSchema.SQL_CREATE_ZONE);
            Assert.Contains("REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE", ZonenSchema.SQL_CREATE_BAUTEIL);
            Assert.Contains("FOREIGN KEY (\"ID_Aufbau\") REFERENCES \"Tab_Bauteilaufbau\" (\"ID\")\n", ZonenSchema.SQL_CREATE_BAUTEIL);
            Assert.Contains("REFERENCES \"Tab_Bauteilaufbau_STAMM\" (\"ID\") ON DELETE CASCADE", BauteilaufbauSchema.SQL_CREATE_SCHICHT_STAMM);
            Assert.Contains("FOREIGN KEY (\"ID_Baustoff\") REFERENCES \"Tab_Baustoff_STAMM\" (\"ID\")\n", BauteilaufbauSchema.SQL_CREATE_SCHICHT_STAMM);
            Assert.Contains("REFERENCES \"Tab_Bauteilaufbau\" (\"ID\") ON DELETE CASCADE", BauteilaufbauSchema.SQL_CREATE_SCHICHT);
            Assert.Contains("FOREIGN KEY (\"ID_Baustoff\") REFERENCES \"Tab_Baustoff\" (\"ID\")\n", BauteilaufbauSchema.SQL_CREATE_SCHICHT);
            // Die Schicht ohne ReadOnly und ohne Herkunft (L1).
            foreach (string sql in new[] { BauteilaufbauSchema.SQL_CREATE_SCHICHT_STAMM, BauteilaufbauSchema.SQL_CREATE_SCHICHT })
            {
                Assert.DoesNotContain("ReadOnly", sql);
                Assert.DoesNotContain("Herkunft", sql);
                Assert.DoesNotContain("ID_Projekt", sql);
            }
            Assert.DoesNotContain("ID_Projekt", ZonenSchema.SQL_CREATE_ZONE);
            Assert.DoesNotContain("ID_Projekt", ZonenSchema.SQL_CREATE_BAUTEIL);
        }

        /// <summary>
        /// Die Baustoffsaat: 65 herstellerneutrale Normzeilen (Ids 1 bis 65) und 67 Herstellerzeilen
        /// (Ids 1001 bis 1067), je lückenlos; der natürliche Schlüssel (Hersteller, Bezeichner)
        /// eindeutig ohne Rücksicht auf Groß- und Kleinschreibung, Längen und Stoffwerte im Band des
        /// Imports (Mehrzonenkonzept 3.5), Quelle je Zeile. Die Dämmstoffe der Normsaat nennen λD,
        /// nicht „WLS" (Vorgabe der Orchestrierung).
        /// </summary>
        [Fact]
        public void Die_Baustoffsaat_ist_vollstaendig_eindeutig_und_im_Band()
        {
            IReadOnlyList<BaustoffSaat> saat = BaustoffSchema.Saat;
            Assert.Equal(132, saat.Count);
            Assert.Equal(Enumerable.Range(1, 65), BaustoffSaattabelle.Norm.Select(s => s.Id));
            Assert.Equal(Enumerable.Range(1001, 67), BaustoffSaattabelle.Hersteller.Select(s => s.Id));
            Assert.All(BaustoffSaattabelle.Norm, s => Assert.Null(s.Hersteller));
            Assert.All(BaustoffSaattabelle.Hersteller, s => Assert.False(string.IsNullOrWhiteSpace(s.Hersteller)));
            Assert.True(saat.All(s => s.Id < BaustoffSchema.SAAT_ID_GRENZE));
            Assert.Equal(saat.Count, saat.Select(s => (s.Hersteller ?? "").ToLowerInvariant() + "|" +
                                                      s.Bezeichner.ToLowerInvariant()).Distinct().Count());

            foreach (BaustoffSaat s in saat)
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Bezeichner));
                Assert.True(s.Bezeichner.Length <= BaustoffSchema.LAENGE_BEZEICHNER, s.Bezeichner);
                Assert.True(s.Gruppe.Length <= BaustoffSchema.LAENGE_GRUPPE, s.Gruppe);
                Assert.False(string.IsNullOrWhiteSpace(s.Quelle), s.Bezeichner);
                Assert.True(s.Quelle.Length <= BaustoffSchema.LAENGE_QUELLE, s.Quelle);
                Assert.InRange(s.Lambda, 0.005, 500.0);
                Assert.InRange(s.Rho, 5.0, 8000.0);
                Assert.InRange(s.Cp, 100.0, 5000.0);
                Assert.DoesNotContain("WLS", s.Bezeichner);
                if (s.Hersteller != null)
                    Assert.True(s.Hersteller.Length <= BaustoffSchema.LAENGE_HERSTELLER, s.Hersteller);
            }

            Assert.Equal(13, BaustoffSaattabelle.Norm.Count(s => s.Gruppe == "Dämmstoffe"));
            // Die Herstellersaat ordnet sich in die Gruppen der Normsaat ein - ein Gruppenfilter
            // zeigt Norm- und Herstellerzeilen zusammen.
            Assert.Subset(new HashSet<string>(BaustoffSaattabelle.Norm.Select(s => s.Gruppe)),
                          new HashSet<string>(BaustoffSaattabelle.Hersteller.Select(s => s.Gruppe)));
            Assert.All(saat.Where(s => s.Gruppe == "Dämmstoffe" && s.Id <= 47), s => Assert.Contains("λD 0,0", s.Bezeichner));
            Assert.Equal("Mineralwolle λD 0,035", BaustoffSchema.SaatZu(36).Bezeichner);
            Assert.Equal(0.036, BaustoffSchema.SaatZu(36).Lambda);
            Assert.Equal("Stahlbeton", BaustoffSchema.SaatZu(10).Bezeichner);
            Assert.Null(BaustoffSchema.SaatZu(66));
            Assert.Equal("Xella", BaustoffSchema.SaatZu(1026).Hersteller);
            Assert.Equal(0.07, BaustoffSchema.SaatZu(1026).Lambda);
        }

        private static string Liste(IEnumerable<string> werte) => "'" + string.Join("','", werte) + "'";
    }

    /// <summary>
    /// Stufe G3, Welle B — die acht Tabellen gegen die Arbeitskopie der Testdatenbank: STRICT,
    /// Spalten, Fremdschlüssel, CHECKs, Saat und Idempotenz der Schritte S-A bis S-C.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeG3SchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static readonly string[] Tabellen =
        {
            SchemaKatalog.TAB_BAUSTOFF_STAMM, SchemaKatalog.TAB_BAUSTOFF,
            SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM, SchemaKatalog.TAB_BAUTEILAUFBAU,
            SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM, SchemaKatalog.TAB_BAUTEILSCHICHT,
            SchemaKatalog.TAB_ZONE, SchemaKatalog.TAB_BAUTEIL
        };

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        private static List<string> Spalten(string tabelle) => DataRepository.SpaltenVonTabelle(tabelle);

        [Fact]
        public void Die_acht_Tabellen_stehen_STRICT_samt_Indizes()
        {
            if (!_db.Vorhanden) return;

            foreach (string t in Tabellen)
                Assert.True(Zahl("SELECT COUNT(*) FROM pragma_table_list WHERE name = ? AND strict = 1",
                                 new DbParam("@t", t)) == 1, t + " fehlt oder ist nicht STRICT.");
            foreach (string i in new[] { BauteilaufbauSchema.INDEX_SCHICHT_STAMM, BauteilaufbauSchema.INDEX_SCHICHT,
                                         ZonenSchema.INDEX_ZONE, ZonenSchema.INDEX_BAUTEIL })
                Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?",
                                      new DbParam("@i", i)));
            Assert.Equal(new[] { "ID_Gebaeude", "Rang" },
                         IndexSpalten(ZonenSchema.INDEX_ZONE));
            Assert.Equal(new[] { "ID_Zone", "Rang" }, IndexSpalten(ZonenSchema.INDEX_BAUTEIL));
            Assert.Equal(new[] { "ID_Aufbau", "Reihenfolge" }, IndexSpalten(BauteilaufbauSchema.INDEX_SCHICHT));
            Assert.Equal(new[] { "ID_Aufbau", "Reihenfolge" }, IndexSpalten(BauteilaufbauSchema.INDEX_SCHICHT_STAMM));

            Assert.True(BaustoffSchema.Vollstaendig());
            Assert.True(BauteilaufbauSchema.Vollstaendig());
            Assert.True(ZonenSchema.Vollstaendig());
        }

        /// <summary>Katalog und Projektkopie sind spaltengleich — nur <c>ReadOnly</c> bzw. <c>ID_Projekt</c> trennt sie.</summary>
        [Fact]
        public void Katalog_und_Projektkopie_sind_spaltengleich()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(Spalten(SchemaKatalog.TAB_BAUSTOFF_STAMM).Where(s => s != "ReadOnly"),
                         Spalten(SchemaKatalog.TAB_BAUSTOFF).Where(s => s != "ID_Projekt"));
            Assert.Equal(Spalten(SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM).Where(s => s != "ReadOnly"),
                         Spalten(SchemaKatalog.TAB_BAUTEILAUFBAU).Where(s => s != "ID_Projekt"));
            Assert.Equal(Spalten(SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM), Spalten(SchemaKatalog.TAB_BAUTEILSCHICHT));

            Assert.Equal(new[] { "ID" }.Concat(new[] { "Bezeichner" }).Concat(BaustoffSchema.Fachspalten).Concat(new[] { "ReadOnly" }),
                         Spalten(SchemaKatalog.TAB_BAUSTOFF_STAMM));
            Assert.Equal(new[] { "ID", "Bezeichner" }.Concat(BauteilaufbauSchema.Fachspalten).Concat(new[] { "ReadOnly" }),
                         Spalten(SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM));
            Assert.Equal(new[] { "ID" }.Concat(BauteilaufbauSchema.Schichtspalten), Spalten(SchemaKatalog.TAB_BAUTEILSCHICHT));
            // Hinter den Spalten von S-C hängt der eigene Zonenschritt der Kühlübergabe (E37,
            // KuehluebergabeSchema.SCHRITT_ZONE) seine drei Spalten an.
            Assert.Equal(new[] { "ID" }.Concat(ZonenSchema.Zonenspalten)
                                       .Concat(KuehluebergabeSchema.SpaltenZone.Select(s => s.Key)),
                         Spalten(SchemaKatalog.TAB_ZONE));
            Assert.Equal(new[] { "ID" }.Concat(ZonenSchema.Bauteilspalten), Spalten(SchemaKatalog.TAB_BAUTEIL));
            Assert.Contains(BaustoffSchema.SPALTE_HERSTELLER, Spalten(SchemaKatalog.TAB_BAUSTOFF));
        }

        /// <summary>Die Fremdschlüssel: Kaskade nur zum Eltern, der Stoff je Seite auf die eigene Ablage (W11).</summary>
        [Fact]
        public void Die_Fremdschluessel_stehen_wie_festgelegt()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(new[] { ("ID_Gebaeude", "Tab_Gebaeude", "CASCADE") }, Fks(SchemaKatalog.TAB_ZONE));
            Assert.Equal(new[] { ("ID_Aufbau", "Tab_Bauteilaufbau", "NO ACTION"), ("ID_Zone", "Tab_Zone", "CASCADE") },
                         Fks(SchemaKatalog.TAB_BAUTEIL));
            Assert.Equal(new[] { ("ID_Aufbau", "Tab_Bauteilaufbau", "CASCADE"), ("ID_Baustoff", "Tab_Baustoff", "NO ACTION") },
                         Fks(SchemaKatalog.TAB_BAUTEILSCHICHT));
            Assert.Equal(new[] { ("ID_Aufbau", "Tab_Bauteilaufbau_STAMM", "CASCADE"), ("ID_Baustoff", "Tab_Baustoff_STAMM", "NO ACTION") },
                         Fks(SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM));
            // Die Projektkopien tragen den Projektfremdschluessel der Hausregel (Schritt 96).
            Assert.Equal(new[] { ("ID_Projekt", "Tab_Projekt", "CASCADE") }, Fks(SchemaKatalog.TAB_BAUSTOFF));
            Assert.Equal(new[] { ("ID_Projekt", "Tab_Projekt", "CASCADE") }, Fks(SchemaKatalog.TAB_BAUTEILAUFBAU));
            Assert.Empty(Fks(SchemaKatalog.TAB_BAUSTOFF_STAMM));
            Assert.Empty(Fks(SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM));
        }

        /// <summary>Die Tabellen halten ihre Wertlisten selbst — auch an jeder Oberfläche vorbei.</summary>
        [Theory]
        [InlineData("INSERT INTO Tab_Baustoff_STAMM (Bezeichner, Herkunft) VALUES ('x', 'FREMD')")]
        [InlineData("INSERT INTO Tab_Baustoff_STAMM (Bezeichner, ReadOnly) VALUES ('x', 2)")]
        [InlineData("INSERT INTO Tab_Baustoff (ID_Projekt, Bezeichner, Quellkennung) VALUES (1, 'x', '12345678901234567890123456789012345678901234567890123456789012345')")]
        [InlineData("INSERT INTO Tab_Baustoff_STAMM (Bezeichner, Hersteller) VALUES ('x', '123456789012345678901234567890123456789012345678901234567890123456789012345678901')")]
        [InlineData("INSERT INTO Tab_Bauteilaufbau_STAMM (Bezeichner, Bauteilart) VALUES ('x', 'KELLER')")]
        [InlineData("INSERT INTO Tab_Bauteilaufbau (Bezeichner) VALUES ('ohne Projekt')")]
        [InlineData("INSERT INTO Tab_Bauteilschicht_STAMM (ID_Aufbau, Reihenfolge, Dicke) VALUES (999999, 1, 0.1)")]
        public void Der_CHECK_und_der_Fremdschluessel_weisen_ab(string sql)
        {
            if (!_db.Vorhanden) return;
            using (DbVorgang v = DataRepository.Vorgang())
                Assert.ThrowsAny<Exception>(() => v.Ausfuehren(sql));
        }

        /// <summary>
        /// Zone und Bauteil: Bauteilart ohne <c>KELLER</c>, Randbedingung ohne <c>KELLER</c> (W8), die
        /// Schalter nur 0/1, die Übergabeart nur aus der Liste.
        /// </summary>
        [Fact]
        public void Zone_und_Bauteil_weisen_fremde_Werte_ab()
        {
            if (!_db.Vorhanden) return;
            int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Gebaeude"));

            using (DbVorgang v = DataRepository.Vorgang())
            {
                int zone = v.EinfuegenUndId("INSERT INTO Tab_Zone (ID_Gebaeude, Rang, Bezeichner) VALUES (?, 1, 'Z')",
                                            new[] { new DbParam("@g", gebaeude) });
                Assert.Equal(1L, Convert.ToInt64(v.Skalar("SELECT IstBeheizt FROM Tab_Zone WHERE ID = ?", new DbParam("@z", zone))));
                Assert.Null(v.Skalar("SELECT Kuehlung_Aktiv FROM Tab_Zone WHERE ID = ?", new DbParam("@z", zone)));

                foreach (string sql in new[]
                {
                    "UPDATE Tab_Zone SET Kuehlung_Aktiv = 2 WHERE ID = ?",
                    "UPDATE Tab_Zone SET IstBeheizt = NULL WHERE ID = ?",
                    "UPDATE Tab_Zone SET Uebergabe_Art = 'HEIZKOERPER' WHERE ID = ?",
                    "UPDATE Tab_Zone SET Herkunft = 'ACCESS' WHERE ID = ?",
                    "INSERT INTO Tab_Bauteil (ID_Zone, Rang, Bezeichner, Bauteilart, Flaeche) VALUES (?, 1, 'B', 'KELLER', 1)",
                    "INSERT INTO Tab_Bauteil (ID_Zone, Rang, Bezeichner, Bauteilart, Flaeche, Randbedingung) VALUES (?, 1, 'B', 'AUSSENWAND', 1, 'KELLER')",
                    "INSERT INTO Tab_Bauteil (ID_Zone, Rang, Bezeichner, Bauteilart) VALUES (?, 1, 'B', 'AUSSENWAND')",
                })
                    Assert.ThrowsAny<Exception>(() => v.Ausfuehren(sql, new DbParam("@z", zone)));

                foreach (string art in DbWerte.BAUTEILARTEN)
                    v.Ausfuehren("INSERT INTO Tab_Bauteil (ID_Zone, Rang, Bezeichner, Bauteilart, Flaeche) VALUES (?, 1, 'B', ?, 1)",
                                 new DbParam("@z", zone), new DbParam("@a", art));
                foreach (string rb in DbWerte.RANDBEDINGUNGEN)
                    v.Ausfuehren("INSERT INTO Tab_Bauteil (ID_Zone, Rang, Bezeichner, Bauteilart, Flaeche, Randbedingung) " +
                                 "VALUES (?, 2, 'B', 'AUSSENWAND', 1, ?)", new DbParam("@z", zone), new DbParam("@r", rb));
                foreach (string h in DbWerte.HERKUENFTE)
                    v.Ausfuehren("UPDATE Tab_Zone SET Herkunft = ? WHERE ID = ?", new DbParam("@h", h), new DbParam("@z", zone));
                v.Rollback();
            }
        }

        /// <summary>
        /// Die Saat in der Datenbank: 132 Zeilen unter ihren festen Ids, <c>ReadOnly = 1</c>,
        /// <c>Herkunft = VORGABE</c>, Hersteller, Werte und Quelle wie in der Saattabelle;
        /// die AUTOINCREMENT-Folge steht auf der Saatgrenze.
        /// </summary>
        [Fact]
        public void Die_Saat_steht_unter_festen_Ids_mit_ReadOnly_und_Quelle()
        {
            if (!_db.Vorhanden) return;

            DataTable t = DataRepository.GetDataTable("SELECT * FROM Tab_Baustoff_STAMM ORDER BY ID");
            Assert.Equal(BaustoffSchema.Saat.Count, t.Rows.Count);
            foreach (DataRow r in t.Rows)
            {
                BaustoffSaat s = BaustoffSchema.SaatZu(Convert.ToInt32(r["ID"]));
                Assert.NotNull(s);
                Assert.Equal(s.Bezeichner, Convert.ToString(r["Bezeichner"]));
                Assert.Equal(s.Gruppe, Convert.ToString(r["Gruppe"]));
                Assert.Equal(s.Lambda, Convert.ToDouble(r["Lambda"]));
                Assert.Equal(s.Rho, Convert.ToDouble(r["Rho"]));
                Assert.Equal(s.Cp, Convert.ToDouble(r["cp"]));
                Assert.Equal(s.Quelle, Convert.ToString(r["Quelle"]));
                Assert.Equal(DbWerte.HERKUNFT_VORGABE, Convert.ToString(r["Herkunft"]));
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"]));
                Assert.Equal(s.Hersteller, r["Hersteller"] == DBNull.Value ? null : Convert.ToString(r["Hersteller"]));
                Assert.Equal(DBNull.Value, r["Quellkennung"]);
            }
            Assert.Equal(BaustoffSchema.SAAT_ID_GRENZE - 1L,
                         Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Baustoff_STAMM'"));

            // Eine vom Anwender angelegte Zeile bekommt die erste Id jenseits der Saat.
            using (DbVorgang v = DataRepository.Vorgang())
            {
                int id = v.EinfuegenUndId("INSERT INTO Tab_Baustoff_STAMM (Bezeichner) VALUES (?)",
                                          new[] { new DbParam("@b", "Eigener Stoff") });
                Assert.Equal(BaustoffSchema.SAAT_ID_GRENZE, id);
                v.Rollback();
            }
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Baustoff"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteilaufbau_STAMM"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone"));
        }

        /// <summary>
        /// <b>Idempotenz:</b> Ein zweiter Lauf aller drei Schritte legt nichts an und sät nichts; ein
        /// Lauf auf einer Datei OHNE die Tabellen legt alle acht an und sät vollständig — und eine
        /// vom Anwender geänderte Saatzeile überschreibt kein Lauf.
        /// </summary>
        [Fact]
        public void Die_Schritte_sind_wiederholbar_und_ueberschreiben_nichts()
        {
            if (!_db.Vorhanden) return;

            BaustoffSchema.Bericht b2 = BaustoffSchema.Ausfuehren();
            Assert.Equal(0, b2.TabellenAngelegt);
            Assert.Equal(0, b2.Gesaet);
            Assert.Equal(0, BauteilaufbauSchema.Ausfuehren());
            Assert.Equal(0, ZonenSchema.Ausfuehren());

            DataRepository.ExecuteNonQuery("UPDATE Tab_Baustoff_STAMM SET Lambda = 0.5 WHERE ID = 13");
            Assert.Equal(0, BaustoffSchema.SaatSchreiben());
            Assert.Equal(0.5, Convert.ToDouble(DataRepository.ExecuteScalar("SELECT Lambda FROM Tab_Baustoff_STAMM WHERE ID = 13")));

            // Eine Datei vor dem Schritt: Kind vor Eltern abräumen, dann alle drei Schritte. Die
            // Tabellen des späteren Schritts S-F (ImportzuordnungSchema) sind Kinder aller G3-Tabellen
            // außer den Katalogen und fallen deshalb zuerst - eine Datei vor S-A trägt sie nicht.
            foreach (string t in new[] { "Tab_Importzuordnung", "Tab_Importquelle",
                                         "Tab_Bauteil", "Tab_Zone", "Tab_Bauteilschicht", "Tab_Bauteilschicht_STAMM",
                                         "Tab_Bauteilaufbau", "Tab_Bauteilaufbau_STAMM", "Tab_Baustoff", "Tab_Baustoff_STAMM" })
                DataRepository.ExecuteNonQuery("DROP TABLE \"" + t + "\"");
            DataRepository.ExecuteNonQuery("DELETE FROM sqlite_sequence WHERE name = 'Tab_Baustoff_STAMM'");
            Assert.False(BaustoffSchema.Vollstaendig());
            Assert.False(BauteilaufbauSchema.Vollstaendig());
            Assert.False(ZonenSchema.Vollstaendig());

            BaustoffSchema.Bericht b = BaustoffSchema.Ausfuehren();
            Assert.Equal(2, b.TabellenAngelegt);
            Assert.Equal(BaustoffSchema.Saat.Count, b.Gesaet);
            Assert.Equal(4, BauteilaufbauSchema.Ausfuehren());
            Assert.Equal(2, ZonenSchema.Ausfuehren());
            Assert.True(BaustoffSchema.Vollstaendig());
            Assert.True(BauteilaufbauSchema.Vollstaendig());
            Assert.True(ZonenSchema.Vollstaendig());
            Assert.Contains("132 von 132 Saatzeile(n)", b.Zeile());
            Assert.Equal(2, ImportzuordnungSchema.Ausfuehren());
            Assert.True(ImportzuordnungSchema.Vollstaendig());
        }

        private static List<string> IndexSpalten(string index)
        {
            DataTable t = DataRepository.GetDataTable("SELECT name FROM pragma_index_info(?) ORDER BY seqno",
                                                      new DbParam("@i", index));
            return t.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["name"])).ToList();
        }

        private static List<(string, string, string)> Fks(string tabelle)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"from\" AS Von, \"table\" AS Ziel, on_delete AS Loeschen FROM pragma_foreign_key_list(?) ORDER BY \"from\"",
                new DbParam("@t", tabelle));
            return t.Rows.Cast<DataRow>()
                    .Select(r => (Convert.ToString(r["Von"]), Convert.ToString(r["Ziel"]), Convert.ToString(r["Loeschen"])))
                    .ToList();
        }
    }
}
