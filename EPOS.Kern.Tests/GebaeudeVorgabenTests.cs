using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
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
    /// Klasse; ohne Katalogsatz keine Vorgabe — nie ein Wert der Nachbarklasse.</para>
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

        /// <summary>Klassen und Standards ohne Katalogsatz haben keine Vorgabe — heute die Klassen A und M.</summary>
        [Fact]
        public void Klassen_und_Standards_ohne_Katalogsatz_haben_keine_Vorgabe()
        {
            foreach (Baualtersvorgabe v in GebaeudeVorgaben.Alle.Concat(GebaeudeVorgaben.Standards))
            {
                if (v.Katalogsaetze > 0) continue;
                Assert.All(Spalten, s => Assert.Null(s.Wert(v)));
            }
            Assert.Equal("AM", new string(GebaeudeVorgaben.Alle.Where(v => v.Katalogsaetze == 0).Select(v => v.Klasse).ToArray()));
            Assert.Equal(new[] { Energiestandard.NIEDRIGENERGIE, Energiestandard.EH70, Energiestandard.PASSIVHAUS },
                         GebaeudeVorgaben.Standards.Where(v => v.Katalogsaetze > 0).Select(v => v.Energiestandard));
        }

        /// <summary>
        /// DER VORRANG (E47, F4): Ein Standard mit Katalogsätzen schlägt die Klasse; ein Standard ohne Satz
        /// fällt auf die Klasse zurück; eine Klasse ohne Satz liefert keinen Wert — auch nicht über einen
        /// Standard ohne Satz, und nie den der Nachbarklasse.
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

            // Klasse M ohne Katalogsatz, Standard EH40 ohne Satz: keine Vorgabe - nicht die von L.
            foreach (string feld in GebaeudeVorgaben.Klassenfelder)
            {
                Assert.Null(GebaeudeVorgaben.Wert('M', Energiestandard.EH40, feld));
                Assert.Null(GebaeudeVorgaben.Wert('M', feld));
                Assert.Null(GebaeudeVorgaben.Wert('A', feld));
                Assert.NotNull(GebaeudeVorgaben.Wert('L', feld));
            }

            // Ein Standard mit Satz hilft auch der leeren Klasse (die Wahl ist ausdrücklich, kein Leihen).
            Assert.Equal(GebaeudeVorgaben.FuerStandard(Energiestandard.NIEDRIGENERGIE).UFenster,
                         GebaeudeVorgaben.Wert('M', Energiestandard.NIEDRIGENERGIE, GebaeudeZielfelder.U_FENSTER));

            Assert.Null(GebaeudeVorgaben.FuerStandard("EH155"));
            Assert.Null(GebaeudeVorgaben.Fuer(null, null));
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
