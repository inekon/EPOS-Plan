using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorgaben je Baualtersklasse und je Energiestandard sind der Median des eigenen
    /// Gebäudekatalogs</b> (Frage U12, entschieden mit E27; Entscheid E47): Der Fall rechnet die Tabellen
    /// in <see cref="GebaeudeVorgaben"/> aus <c>Tab_Gebaeude_STAMM</c> der Testdatenbank nach — je Klasse
    /// A…M (Spalte <c>Baualtersklasse</c>, nach der Umschlüsselung) und je Energiestandard (Spalte
    /// <c>Energiestandard</c>) der Median der Werte größer null, U- und g-Werte auf zwei, ψ-Werte auf drei
    /// Nachkommastellen. Weicht etwas ab, nennt die Meldung die neuen Tabellen in der Schreibweise der
    /// Quelle.
    ///
    /// <para>Dazu der <b>Vorrang</b> (Konzept Baualtersklassen 4): der Standard mit Katalogsätzen vor der
    /// Klasse; ohne Katalogsatz der freie Wert nach Stein/Loga (E51, geprüft über die Lesenaht
    /// <see cref="GebaeudeVorgaben.KatalogOhne"/>) — nie ein Wert der Nachbarklasse.</para>
    ///
    /// <para>Ohne Testdatenbank schweigen die Fälle mit Datenbank (<see cref="TestDatenbank.Vorhanden"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeVorgabenTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public GebaeudeVorgabenTests(TestDatenbank db) { _db = db; }

        /// <summary>Die neun Spalten in der Reihenfolge der Tabelle, je mit ihren Nachkommastellen.</summary>
        private static readonly (string Spalte, int Stellen, Func<Baualtersvorgabe, double?> Wert)[] Spalten =
        {
            ("k_Wert_Außenwand", 2, v => v.UAussenwand),
            ("k_Wert_Fenster", 2, v => v.UFenster),
            ("k_Wert_Dachflaeche", 2, v => v.UDach),
            ("k_Wert_Grundflaeche", 2, v => v.UGrund),
            ("k_Wert_Sonstiges", 2, v => v.USonstige),
            ("Fensterdurchlassgrad", 2, v => v.GWert),
            ("WBVK_Anschluß_Fenster_Wand", 3, v => v.PsiFensterWand),
            ("WBVK_Anschluß_Wand_Dach", 3, v => v.PsiWandDach),
            ("WBVK_Anschluß_Außenwand_Kellerdecke", 3, v => v.PsiAussenwandKeller),
        };

        [Fact]
        public void Die_Tabelle_ist_der_Median_des_Gebaeudekatalogs()
        {
            if (!_db.Vorhanden) return;

            DataTable t = DataRepository.GetDataTable("SELECT * FROM Tab_Gebaeude_STAMM");
            Assert.NotNull(t);
            Assert.True(t.Rows.Count > 0, "Tab_Gebaeude_STAMM ist leer.");
            Assert.True(t.Columns.Contains("Energiestandard"), "Die Testdatenbank trägt die Spalte Energiestandard nicht.");

            var abweichungen = new List<string>();
            var neu = new StringBuilder();
            foreach (Baualtersvorgabe v in GebaeudeVorgaben.Alle)
                Vergleichen(t, v, "Baualtersklasse", v.Klasse.ToString(),
                            "Z('" + v.Klasse + "', ", abweichungen, neu);
            foreach (Baualtersvorgabe v in GebaeudeVorgaben.Standards)
                Vergleichen(t, v, "Energiestandard", v.Energiestandard,
                            "S(WindowsFormsApplication1.Energiestandard." + v.Energiestandard + ", ", abweichungen, neu);

            Assert.True(abweichungen.Count == 0,
                "Die Vorgaben passen nicht mehr zum Gebäudekatalog der Testdatenbank:\n" + string.Join("\n", abweichungen)
                + "\n\nNeue Tabellen für GebaeudeVorgaben.cs:\n" + neu);
        }

        private static void Vergleichen(DataTable t, Baualtersvorgabe v, string spalteSchluessel, string schluessel,
                                        string zeilenkopf, List<string> abweichungen, StringBuilder neu)
        {
            List<DataRow> saetze = t.Rows.Cast<DataRow>()
                .Where(r => string.Equals(Convert.ToString(r[spalteSchluessel], CultureInfo.InvariantCulture), schluessel, StringComparison.Ordinal))
                .ToList();
            if (saetze.Count != v.Katalogsaetze)
                abweichungen.Add(v.Kennung + ": " + saetze.Count + " Katalogsätze, die Tabelle nennt " + v.Katalogsaetze);

            var zeile = new List<string>();
            foreach ((string spalte, int stellen, Func<Baualtersvorgabe, double?> wert) in Spalten)
            {
                double? median = Median(saetze.Select(r => r[spalte]).Where(x => x != DBNull.Value)
                                              .Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture)).Where(x => x > 0.0));
                double? tabelle = wert(v);
                zeile.Add(median.HasValue ? Math.Round(median.Value, stellen, MidpointRounding.AwayFromZero)
                                                .ToString("F" + stellen, CultureInfo.InvariantCulture) : "null");
                if (median.HasValue != tabelle.HasValue)
                    abweichungen.Add(v.Kennung + "." + spalte + ": Median " + (median?.ToString("R", CultureInfo.InvariantCulture) ?? "keiner")
                                     + ", Tabelle " + (tabelle?.ToString("R", CultureInfo.InvariantCulture) ?? "leer"));
                else if (median.HasValue && Math.Abs(median.Value - tabelle.Value) > 0.5 * Math.Pow(10, -stellen) + 1e-9)
                    abweichungen.Add(v.Kennung + "." + spalte + ": Median " + median.Value.ToString("R", CultureInfo.InvariantCulture)
                                     + ", Tabelle " + tabelle.Value.ToString("R", CultureInfo.InvariantCulture));
            }
            neu.Append("            ").Append(zeilenkopf).Append(saetze.Count.ToString(CultureInfo.InvariantCulture))
               .Append(", ").Append(string.Join(", ", zeile)).AppendLine("),");
        }

        /// <summary>Die Klassen A…M in ihrer Reihenfolge, die Standards in der Reihenfolge der Codes.</summary>
        [Fact]
        public void Die_Klassen_sind_A_bis_M_und_die_Standards_die_elf_Codes()
        {
            Assert.Equal(13, GebaeudeVorgaben.Alle.Count);
            Assert.Equal("ABCDEFGHIJKLM", new string(GebaeudeVorgaben.Alle.Select(v => v.Klasse).ToArray()));
            Assert.All(GebaeudeVorgaben.Alle, v => Assert.Null(v.Energiestandard));
            Assert.Equal(Energiestandard.CODES, GebaeudeVorgaben.Standards.Select(v => v.Energiestandard));
            Assert.All(GebaeudeVorgaben.Standards, v => Assert.Equal(GebaeudeVorgaben.KEINE_KLASSE, v.Klasse));
            Assert.Equal(Gebaeudeklassen.Anzahl, GebaeudeVorgaben.Alle.Count);
        }

        /// <summary>
        /// Katalogzeilen ohne Satz sind leer; nach E51 hat jede Klasse A…M Katalogsätze (A und M je drei),
        /// und Katalogsätze tragen die Standards Niedrigenergie, EH70, EH55 und Passivhaus.
        /// </summary>
        [Fact]
        public void Katalogzeilen_ohne_Satz_sind_leer_und_jede_Klasse_hat_Saetze()
        {
            foreach (Baualtersvorgabe v in GebaeudeVorgaben.Alle.Concat(GebaeudeVorgaben.Standards))
            {
                Assert.False(v.Frei);
                if (v.Katalogsaetze > 0) continue;
                Assert.All(Spalten, s => Assert.Null(s.Wert(v)));
            }
            Assert.Empty(GebaeudeVorgaben.Alle.Where(v => v.Katalogsaetze == 0));
            Assert.Equal(3, GebaeudeVorgaben.Fuer('A').Katalogsaetze);
            Assert.Equal(3, GebaeudeVorgaben.Fuer('M').Katalogsaetze);
            Assert.Equal(new[] { Energiestandard.NIEDRIGENERGIE, Energiestandard.EH70, Energiestandard.EH55, Energiestandard.PASSIVHAUS },
                         GebaeudeVorgaben.Standards.Where(v => v.Katalogsaetze > 0).Select(v => v.Energiestandard));
            Assert.Equal(1, GebaeudeVorgaben.FuerStandard(Energiestandard.EH55).Katalogsaetze);
        }

        /// <summary>
        /// DIE FREIEN WERTE (E51): 13 Zeilen A…M nach Stein/Loga (2025), Tab. 28 — U-Werte und g-Wert,
        /// ψ leer, je mit der Klasse der Quelle; A und B beruhen auf derselben Quellklasse „bis 1918".
        /// </summary>
        [Fact]
        public void Die_freien_Werte_fuehren_13_Klassen_mit_Quellklasse_und_ohne_Psi()
        {
            IReadOnlyList<Baualtersvorgabe> frei = GebaeudeVorgaben.FreieWerte;
            Assert.Equal("ABCDEFGHIJKLM", new string(frei.Select(v => v.Klasse).ToArray()));
            Assert.All(frei, v =>
            {
                Assert.True(v.Frei);
                Assert.Equal(0, v.Katalogsaetze);
                Assert.Null(v.Energiestandard);
                Assert.False(string.IsNullOrEmpty(v.Quellklasse));
                Assert.NotNull(v.UAussenwand);
                Assert.NotNull(v.UFenster);
                Assert.NotNull(v.UDach);
                Assert.NotNull(v.UGrund);
                Assert.NotNull(v.USonstige);
                Assert.NotNull(v.GWert);
                Assert.Null(v.PsiFensterWand);
                Assert.Null(v.PsiWandDach);
                Assert.Null(v.PsiAussenwandKeller);
                Assert.Same(v, GebaeudeVorgaben.Frei(v.Klasse));
            });
            Assert.Equal("bis 1918", GebaeudeVorgaben.Frei('A').Quellklasse);
            Assert.Equal("bis 1918", GebaeudeVorgaben.Frei('B').Quellklasse);
            Assert.Equal("2021–2025", GebaeudeVorgaben.Frei('M').Quellklasse);

            // Stichproben der Quelle (Tab. 28, EZFH freistehend, auf zwei Stellen): A/B, C, G/H, M.
            Zeile(GebaeudeVorgaben.Frei('A'), 1.37, 3.49, 1.32, 1.02, 3.49, 0.59);
            Zeile(GebaeudeVorgaben.Frei('C'), 1.37, 3.46, 1.32, 1.02, 3.46, 0.59);
            Zeile(GebaeudeVorgaben.Frei('E'), 1.14, 3.38, 1.07, 1.01, 3.38, 0.59);
            Zeile(GebaeudeVorgaben.Frei('H'), 0.74, 2.85, 0.47, 0.67, 2.85, 0.71);
            Zeile(GebaeudeVorgaben.Frei('I'), 0.50, 1.80, 0.25, 0.40, 1.80, 0.56);
            Zeile(GebaeudeVorgaben.Frei('J'), 0.34, 1.48, 0.22, 0.37, 1.48, 0.55);
            Zeile(GebaeudeVorgaben.Frei('K'), 0.20, 1.10, 0.15, 0.25, 1.10, 0.55);
            Zeile(GebaeudeVorgaben.Frei('L'), 0.18, 0.98, 0.15, 0.18, 0.98, 0.55);
            Zeile(GebaeudeVorgaben.Frei('M'), 0.16, 0.95, 0.13, 0.16, 0.95, 0.55);
            Assert.Null(GebaeudeVorgaben.Frei(null));
            Assert.Null(GebaeudeVorgaben.Frei('N'));
            Assert.Same(GebaeudeVorgaben.Frei('m'), GebaeudeVorgaben.Frei('M'));
        }

        /// <summary>
        /// DER RÜCKFALL (E51) über die Lesenaht: Hat die Klasse keinen Katalogsatz, gilt ihr freier Wert —
        /// Standard mit Sätzen → Klasse mit Sätzen → freier Wert; Beleg und Herkunft nennen ihn; ψ bleibt
        /// leer; nach dem Ende der Naht gilt wieder der Katalog.
        /// </summary>
        [Fact]
        public void Ohne_Katalogsatz_greift_der_freie_Wert_mit_Beleg()
        {
            Assert.Same(GebaeudeVorgaben.Fuer('M'), GebaeudeVorgaben.Fuer('M', null));
            using (GebaeudeVorgaben.KatalogOhne("AM", new[] { Energiestandard.EH55 }))
            {
                Assert.Equal(0, GebaeudeVorgaben.Fuer('M').Katalogsaetze);
                Assert.Equal(0, GebaeudeVorgaben.FuerStandard(Energiestandard.EH55).Katalogsaetze);

                Baualtersvorgabe m = GebaeudeVorgaben.Fuer('M', null);
                Assert.True(m.Frei);
                Assert.Same(GebaeudeVorgaben.Frei('M'), m);
                Assert.Same(m, GebaeudeVorgaben.Fuer('M', Energiestandard.EH55));      // Standard ohne Satz: Klasse, dann frei
                Assert.Same(m, GebaeudeVorgaben.Fuer('m', ""));
                Assert.Equal(0.16, GebaeudeVorgaben.Wert('M', GebaeudeZielfelder.U_AUSSENWAND));
                Assert.Equal(0.55, GebaeudeVorgaben.Wert('M', GebaeudeZielfelder.G_WERT));
                Assert.Null(GebaeudeVorgaben.Wert('M', GebaeudeZielfelder.PSI_WAND_DACH));
                Assert.NotEqual(GebaeudeVorgaben.Wert('L', GebaeudeZielfelder.U_AUSSENWAND),
                                GebaeudeVorgaben.Wert('M', GebaeudeZielfelder.U_AUSSENWAND));   // nie die Nachbarklasse

                // Ein Standard mit Sätzen hat weiter Vorrang vor dem freien Wert.
                Assert.Same(GebaeudeVorgaben.FuerStandard(Energiestandard.PASSIVHAUS), GebaeudeVorgaben.Fuer('M', Energiestandard.PASSIVHAUS));
                // Eine Klasse mit Sätzen bleibt beim Katalog.
                Assert.False(GebaeudeVorgaben.Fuer('L', null).Frei);

                GebaeudeBeleg b = GebaeudeVorgaben.Beleg(m);
                Assert.Equal(GebaeudeVorgaben.BELEG_FREI, b.Schluessel);
                Assert.Equal(new[] { "M", "2021–2025" }, b.Werte);
                Assert.Equal(Importherkunft.VorgabeFrei, GebaeudeVorgaben.Herkunft(m));
                Assert.Equal("VORGABE", ImportherkunftWerte.Wert(GebaeudeVorgaben.Herkunft(m)));
                Assert.True(GebaeudeVorgaben.Fuer('A', null).Frei);
                Assert.Equal("bis 1918", GebaeudeVorgaben.Fuer('A', null).Quellklasse);
            }

            // Nach der Naht: wieder der Katalog.
            Baualtersvorgabe k = GebaeudeVorgaben.Fuer('M', null);
            Assert.False(k.Frei);
            Assert.Equal(3, k.Katalogsaetze);
            GebaeudeBeleg kb = GebaeudeVorgaben.Beleg(k);
            Assert.Equal(GebaeudeVorgaben.BELEG_KLASSE, kb.Schluessel);
            Assert.Equal(new[] { "M", "3" }, kb.Werte);
            Assert.Equal(Importherkunft.Vorgabe, GebaeudeVorgaben.Herkunft(k));
            Assert.Null(GebaeudeVorgaben.Beleg(null));
        }

        /// <summary>Die Texte des freien Werts nennen Quelle, Tabelle, Lizenz und das Wohngebäudemodell — in beiden Sprachen.</summary>
        [Fact]
        public void Beleg_und_Meldung_des_freien_Werts_nennen_die_Quelle()
        {
            foreach (string kultur in new[] { "de-DE", "en-US" })
            {
                using var _ = new Kulturvorrichtung(kultur);
                string beleg = GebaeudeZuordnungsModell.BelegText(new GebaeudeBeleg(GebaeudeVorgaben.BELEG_FREI, "M", "2021–2025"));
                string meldung = GebaeudeZuordnungsModell.MeldungText(
                    new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "KLASSE_VORGABE_FREI", "M", "2021–2025"));
                foreach (string text in new[] { beleg, meldung })
                {
                    Assert.Contains("Stein, B.; Loga, T. (2025)", text, StringComparison.Ordinal);
                    Assert.Contains("CC BY 4.0", text, StringComparison.Ordinal);
                    Assert.Contains("2021–2025", text, StringComparison.Ordinal);
                    Assert.DoesNotContain("{", text, StringComparison.Ordinal);
                }
                Assert.Contains(kultur == "de-DE" ? "Wohngebäudemodell" : "residential building model", beleg, StringComparison.Ordinal);
                Assert.NotEqual(GebaeudeZuordnungsModell.HerkunftText(Importherkunft.Vorgabe),
                                GebaeudeZuordnungsModell.HerkunftText(Importherkunft.VorgabeFrei));
            }
        }

        /// <summary>
        /// DER VORRANG (E47, E51): Ein Standard mit Katalogsätzen schlägt die Klasse; ein Standard ohne
        /// Satz fällt auf die Klasse zurück; eine Klasse liefert nie den Wert der Nachbarklasse.
        /// </summary>
        [Fact]
        public void Die_Vorgabe_kommt_aus_dem_Standard_vor_der_Klasse_und_wird_nie_geliehen()
        {
            Baualtersvorgabe passivhaus = GebaeudeVorgaben.FuerStandard(Energiestandard.PASSIVHAUS);
            Assert.True(passivhaus.Katalogsaetze > 0);
            Assert.Same(passivhaus, GebaeudeVorgaben.Fuer('J', Energiestandard.PASSIVHAUS));
            Assert.Equal(passivhaus.UAussenwand, GebaeudeVorgaben.Wert('J', Energiestandard.PASSIVHAUS, GebaeudeZielfelder.U_AUSSENWAND));
            Assert.NotEqual(GebaeudeVorgaben.Wert('J', GebaeudeZielfelder.U_AUSSENWAND),
                            GebaeudeVorgaben.Wert('J', Energiestandard.PASSIVHAUS, GebaeudeZielfelder.U_AUSSENWAND));

            // Ein Standard ohne Katalogsatz: die Klasse.
            Assert.Equal(0, GebaeudeVorgaben.FuerStandard(Energiestandard.EH40).Katalogsaetze);
            Assert.Same(GebaeudeVorgaben.Fuer('K'), GebaeudeVorgaben.Fuer('K', Energiestandard.EH40));
            Assert.Same(GebaeudeVorgaben.Fuer('K'), GebaeudeVorgaben.Fuer('K', null));
            Assert.Same(GebaeudeVorgaben.Fuer('K'), GebaeudeVorgaben.Fuer('K', ""));

            // Klasse M mit Katalogsätzen (E51), Standard EH40 ohne Satz: die Vorgabe von M - nicht die von L.
            foreach (string feld in GebaeudeVorgaben.Klassenfelder)
            {
                Assert.Equal(GebaeudeVorgaben.Wert('M', feld), GebaeudeVorgaben.Wert('M', Energiestandard.EH40, feld));
                Assert.NotNull(GebaeudeVorgaben.Wert('M', feld));
                Assert.NotNull(GebaeudeVorgaben.Wert('A', feld));
                Assert.NotNull(GebaeudeVorgaben.Wert('L', feld));
            }
            Assert.NotEqual(GebaeudeVorgaben.Wert('L', GebaeudeZielfelder.U_SONSTIGE), GebaeudeVorgaben.Wert('M', GebaeudeZielfelder.U_SONSTIGE));

            // EH55 hat jetzt einen Satz: er schlägt die Klasse M.
            Assert.Same(GebaeudeVorgaben.FuerStandard(Energiestandard.EH55), GebaeudeVorgaben.Fuer('M', Energiestandard.EH55));
            Assert.Equal(0.91, GebaeudeVorgaben.Wert('M', Energiestandard.EH55, GebaeudeZielfelder.U_FENSTER));

            // Ein Standard mit Satz gilt auch für eine andere Klasse (die Wahl ist ausdrücklich, kein Leihen).
            Assert.Equal(GebaeudeVorgaben.FuerStandard(Energiestandard.NIEDRIGENERGIE).UFenster,
                         GebaeudeVorgaben.Wert('M', Energiestandard.NIEDRIGENERGIE, GebaeudeZielfelder.U_FENSTER));

            Assert.Null(GebaeudeVorgaben.FuerStandard("EH155"));
            Assert.Null(GebaeudeVorgaben.Fuer(null, null));
        }

        private static void Zeile(Baualtersvorgabe v, double uAw, double uFe, double uDa, double uGr, double uSo, double g)
        {
            Assert.Equal(uAw, v.UAussenwand);
            Assert.Equal(uFe, v.UFenster);
            Assert.Equal(uDa, v.UDach);
            Assert.Equal(uGr, v.UGrund);
            Assert.Equal(uSo, v.USonstige);
            Assert.Equal(g, v.GWert);
        }

        /// <summary>Der Median; bei gerader Zahl das Mittel der beiden mittleren Werte. <c>null</c> ohne Werte.</summary>
        private static double? Median(IEnumerable<double> werte)
        {
            double[] w = werte.OrderBy(x => x).ToArray();
            if (w.Length == 0) return null;
            return w.Length % 2 == 1 ? w[w.Length / 2] : (w[w.Length / 2 - 1] + w[w.Length / 2]) / 2.0;
        }
    }
}
