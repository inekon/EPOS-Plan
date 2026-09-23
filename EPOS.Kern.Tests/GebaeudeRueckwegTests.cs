using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Rückweg-Test des Tagesbilanz-Wegs</b> (Gebäudesimulation, F-Ü7; Architekturfrage
    /// A15, mit E27 entschieden; Systementwurf 8.4, Umsetzungskonzept 1.8/1.9).
    ///
    /// <para><b>Gegenstand ist genau ein Referenzprojekt:</b> 1040 mit seinem einzigen Gebäude
    /// 10645, das in der Testdatenbank ausdrücklich <c>Gebaeude_Modell = 'TAGESBILANZ'</c> trägt
    /// (<c>Referenzlaeufe/Skripte/gebaeude_1040_tagesbilanz.py</c>). Alle übrigen Gebäude der
    /// Referenzprojekte stehen auf NULL und rechnen damit nach VDI 6007.</para>
    ///
    /// <para><b>Was er hält:</b> Die Stundenreihe des Gebäudewärmebedarfs, die der Lauf für 1040
    /// rechnet, ist <b>zeichengleich</b> (Schreibweise des Referenzlaufs, G9) mit
    /// <c>Projekt_1040/waermebedarf_gebaeude.csv</c> der aktuellen Basis — und die Basis hat
    /// diese Reihe beim Einfrieren von G1 + G2 byte-gleich aus der letzten reinen
    /// Bestandsbasis übernommen. Ändert sich eine Zeile des Altwegs ergebniswirksam, wird der
    /// Test rot. Der Referenzlauf der dreizehn Projekte hält dasselbe Projekt zusätzlich im
    /// Ganzen; dieser Test läuft ohne ihn, mit dem Test-Gate und in der CI.</para>
    ///
    /// <para><b>Er endet mit der Stufe GA</b> (Löschliste, Umsetzungskonzept 6.1): Dann geht 1040
    /// auf VDI 6007 über, und diese Klasse fällt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeRueckwegTests : IClassFixture<TestDatenbank>
    {
        /// <summary>Das Referenzprojekt auf dem Altweg (A15).</summary>
        public const int PROJEKT = 1040;

        /// <summary>Sein einziges Gebäude, ausdrücklich auf <c>TAGESBILANZ</c>.</summary>
        public const int GEBAEUDE = 10645;

        /// <summary>Die dreizehn Referenzprojekte der Basis.</summary>
        private static readonly int[] Referenzprojekte =
            { 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046 };

        private readonly TestDatenbank _db;

        public GebaeudeRueckwegTests(TestDatenbank db) { _db = db; }

        /// <summary>
        /// Genau ein Gebäude der Referenzprojekte steht auf dem Altweg — 10645 in 1040, und es
        /// ist dort das einzige; alle anderen tragen keine Angabe und rechnen nach VDI 6007.
        /// </summary>
        [Fact]
        public void Genau_ein_Referenzgebaeude_steht_auf_dem_Tagesbilanz_Weg()
        {
            if (!_db.Vorhanden) return;

            var altweg = new List<(int Projekt, int Gebaeude)>();
            foreach (int projekt in Referenzprojekte)
            {
                var ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(projekt);
                for (int i = 0; i < ctrl.rows; i++)
                {
                    ProjektGebaeudeModel item = ctrl.items[i];
                    if (item.Gebaeude_Modell == null) continue;
                    Assert.Equal(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, item.Gebaeude_Modell);
                    altweg.Add((projekt, item.ID_Gebaeude));
                }
            }

            Assert.Equal(new[] { (PROJEKT, GEBAEUDE) }, altweg);

            var eigene = new ProjektGebaeudeCtrl();
            eigene.ReadAll(PROJEKT);
            Assert.Equal(1, eigene.rows);
            Assert.False(Gebaeuderechenweg.IstVdi6007(eigene.items[0].Gebaeude_Modell));
        }

        /// <summary>
        /// <b>Der Rückweg:</b> Der Gebäudewärmebedarf von 1040 ist Stunde für Stunde
        /// zeichengleich mit der eingefrorenen Reihe der aktuellen Basis.
        /// </summary>
        [Fact]
        public void Der_Tagesbilanz_Weg_rechnet_wie_die_Basis()
        {
            if (!_db.Vorhanden) return;

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(PROJEKT, projekt.m_ID_Klimaregion);

            // Kein Gebäude des Projekts rechnet auf dem VDI-Weg.
            Assert.Empty(sim.GebaeudeErgebnisse.Alle);

            string[] basis = BasisReihe("waermebedarf_gebaeude.csv");
            Assert.Equal("Index;Wert", basis[0]);
            Assert.Equal(8760, basis.Length - 1);

            double summe = 0;
            for (int h = 0; h < 8760; h++)
            {
                string erwartet = basis[h + 1];
                string ist = h.ToString(CultureInfo.InvariantCulture) + ";" + Zahl(sim.Waermebedarf_Gebaeude[h]);
                Assert.True(erwartet == ist, $"Stunde {h}: Basis '{erwartet}', Lauf '{ist}'");
                summe += sim.Waermebedarf_Gebaeude[h];
            }
            Assert.True(summe > 0, "Das Gebäude heizt gar nicht — der Test prüfte dann nichts.");
        }

        /// <summary>Die Schreibweise des Referenzlaufs (<c>Ergebnisexport.Zahl</c>).</summary>
        private static string Zahl(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (double.IsPositiveInfinity(d)) return "Inf";
            if (double.IsNegativeInfinity(d)) return "-Inf";
            return d.ToString("G9", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Die Zeilen einer Datei von 1040 in der <b>aktuellen</b> Basis — dem einen Ordner
        /// unter <c>Referenzlaeufe/</c> nach dem Namensschema <c>JJJJ-MM-TT_R&lt;n&gt;_…</c>.
        /// </summary>
        private static string[] BasisReihe(string datei)
        {
            string[] basen = Directory.GetDirectories(Path.Combine(Wurzel(), "Referenzlaeufe"))
                .Where(o => Regex.IsMatch(Path.GetFileName(o), @"^\d{4}-\d{2}-\d{2}_R\d+_"))
                .ToArray();
            Assert.True(basen.Length == 1,
                "Unter Referenzlaeufe/ liegt nicht genau eine Basis: " + string.Join(", ", basen));

            string pfad = Path.Combine(basen[0], "Projekt_" + PROJEKT, datei);
            Assert.True(File.Exists(pfad), "Die Basis führt " + pfad + " nicht.");
            return File.ReadAllLines(pfad);
        }

        /// <summary>Der Aufstieg zur Repowurzel (Muster <c>BerechnungshilfeEinbettungTests</c>).</summary>
        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null &&
                   !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;

            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}
