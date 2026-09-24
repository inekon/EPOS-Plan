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
    /// <b>Die Vorgaben je Baualtersklasse sind der Median des eigenen Gebäudekatalogs</b> (Frage
    /// U12, entschieden mit E27): Der Fall rechnet die Tabelle in <see cref="GebaeudeVorgaben"/> aus
    /// <c>Tab_Gebaeude_STAMM</c> der Testdatenbank nach — je Klasse und Spalte der Median der Werte
    /// größer null, U- und g-Werte auf zwei, ψ-Werte auf drei Nachkommastellen. Weicht etwas ab, nennt
    /// die Meldung die neue Tabelle in der Schreibweise der Quelle.
    ///
    /// <para>Ohne Testdatenbank schweigt der Fall (<see cref="TestDatenbank.Vorhanden"/>).</para>
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

            var abweichungen = new List<string>();
            var neu = new StringBuilder();
            foreach (Baualtersvorgabe v in GebaeudeVorgaben.Alle)
            {
                List<DataRow> saetze = t.Rows.Cast<DataRow>()
                    .Where(r => string.Equals(Convert.ToString(r["Baualtersklasse"], CultureInfo.InvariantCulture), v.Klasse.ToString(), StringComparison.Ordinal))
                    .ToList();
                if (saetze.Count != v.Katalogsaetze)
                    abweichungen.Add(v.Klasse + ": " + saetze.Count + " Katalogsätze, die Tabelle nennt " + v.Katalogsaetze);

                var zeile = new List<string>();
                foreach ((string spalte, int stellen, Func<Baualtersvorgabe, double?> wert) in Spalten)
                {
                    double? median = Median(saetze.Select(r => r[spalte]).Where(x => x != DBNull.Value)
                                                  .Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture)).Where(x => x > 0.0));
                    double? tabelle = wert(v);
                    zeile.Add(median.HasValue ? Math.Round(median.Value, stellen, MidpointRounding.AwayFromZero)
                                                    .ToString("F" + stellen, CultureInfo.InvariantCulture) : "null");
                    if (median.HasValue != tabelle.HasValue)
                        abweichungen.Add(v.Klasse + "." + spalte + ": Median " + (median?.ToString("R", CultureInfo.InvariantCulture) ?? "keiner")
                                         + ", Tabelle " + (tabelle?.ToString("R", CultureInfo.InvariantCulture) ?? "leer"));
                    else if (median.HasValue && Math.Abs(median.Value - tabelle.Value) > 0.5 * Math.Pow(10, -stellen) + 1e-9)
                        abweichungen.Add(v.Klasse + "." + spalte + ": Median " + median.Value.ToString("R", CultureInfo.InvariantCulture)
                                         + ", Tabelle " + tabelle.Value.ToString("R", CultureInfo.InvariantCulture));
                }
                neu.Append("            Z('").Append(v.Klasse).Append("', ").Append(saetze.Count.ToString(CultureInfo.InvariantCulture))
                   .Append(", ").Append(string.Join(", ", zeile)).AppendLine("),");
            }

            Assert.True(abweichungen.Count == 0,
                "Die Vorgaben passen nicht mehr zum Gebäudekatalog der Testdatenbank:\n" + string.Join("\n", abweichungen)
                + "\n\nNeue Tabelle für GebaeudeVorgaben.cs:\n" + neu);
        }

        [Fact]
        public void Klassen_ohne_Katalogsatz_haben_keine_Vorgabe()
        {
            foreach (Baualtersvorgabe v in GebaeudeVorgaben.Alle)
            {
                if (v.Katalogsaetze > 0) continue;
                Assert.All(Spalten, s => Assert.Null(s.Wert(v)));
            }
            Assert.Equal("LOPRTU", new string(GebaeudeVorgaben.Alle.Where(v => v.Katalogsaetze == 0).Select(v => v.Klasse).ToArray()));
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
