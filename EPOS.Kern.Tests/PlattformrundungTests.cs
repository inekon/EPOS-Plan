using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache der Naht <see cref="Plattformrundung"/></b> — der Stelle, an der der
    /// Referenzlauf mit <c>--stoerung ulp</c> die Ergebnisse von <c>Exp</c>, <c>Sin</c>,
    /// <c>Cos</c>, <c>Asin</c> und <c>Acos</c> um ±1 ulp verschiebt, um zu zeigen, dass keine
    /// Betriebsentscheidung am letzten Bit der C‑Laufzeit hängt.
    ///
    /// <para><b>Drei Zusagen.</b> (1) Ohne Schalter liefert die Naht bitgleich <c>Math.*</c> —
    /// der Normallauf rechnet also byte-gleich zur Basis. (2) Die Störung verschiebt höchstens
    /// ein ulp, deterministisch, etwa jedes sechzehnte Ergebnis, in beide Richtungen, und lässt
    /// exakte Werte stehen. (3) In den Dateien der Naht ruft keine Codezeile die fünf
    /// Funktionen an ihr vorbei — sonst prüfte der gestörte Lauf dort nichts.</para>
    ///
    /// <para>Der Schalter selbst wird hier nie gesetzt: <see cref="Plattformrundung.UlpStoerung"/>
    /// wird einmal gelesen und gälte dann für alle Tests des Prozesses. Die Störung wird
    /// deshalb über <see cref="Plattformrundung.Verschieben"/> geprüft, der Schalter im
    /// Referenzlauf der CI (<c>kern.yml</c>: gestört gegen ungestört).</para>
    /// </summary>
    public class PlattformrundungTests
    {
        /// <summary>Die Dateien, deren Aufrufe über die Naht laufen (siehe Klassenkommentar der Naht).</summary>
        private static readonly string[] NAHTDATEIEN =
        {
            "EPOS.Kern/Allgemein/Simulation/Gebaeude/Matrix2.cs",
            "EPOS.Kern/Allgemein/SolarPVGISCalculator.cs",
            "EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs",
            "EPOS.Kern/Allgemein/Simulation/SimulationSolarthermie.cs",
            "EPOS.Kern/Allgemein/Simulation/Altweg/TagesbilanzPhysik.cs",
        };

        // =====================================================================
        //  1 — Ohne Schalter: bitgleich
        // =====================================================================

        [Fact]
        public void Ohne_Schalter_ist_die_Naht_bitgleich_Math()
        {
            Assert.False(Plattformrundung.UlpStoerung, "In den Tests darf die Störung nie eingeschaltet sein.");

            foreach (double x in Argumente())
            {
                Assert.Equal(Bits(Math.Exp(x)), Bits(Plattformrundung.Exp(x)));
                Assert.Equal(Bits(Math.Sin(x)), Bits(Plattformrundung.Sin(x)));
                Assert.Equal(Bits(Math.Cos(x)), Bits(Plattformrundung.Cos(x)));

                double u = Math.Clamp(x / 10.0, -1.0, 1.0);
                Assert.Equal(Bits(Math.Asin(u)), Bits(Plattformrundung.Asin(u)));
                Assert.Equal(Bits(Math.Acos(u)), Bits(Plattformrundung.Acos(u)));
            }
        }

        // =====================================================================
        //  2 — Die Störung
        // =====================================================================

        /// <summary>
        /// Über 100 000 Argumente: jedes Ergebnis bleibt oder rückt um genau ein ulp, der Anteil
        /// liegt bei 1/16 (hier zwischen 5 und 7,5 %), beide Richtungen kommen vor, und ein
        /// zweiter Aufruf liefert dasselbe.
        /// </summary>
        [Fact]
        public void Die_Stoerung_verschiebt_hoechstens_ein_ulp_deterministisch_in_beide_Richtungen()
        {
            const ulong salz = 0x1234_5678_9ABC_DEF0UL;
            int gesamt = 0, hoch = 0, runter = 0;

            var zufall = new Random(20260929);
            for (int i = 0; i < 100_000; i++)
            {
                double x = (zufall.NextDouble() - 0.5) * 40.0;
                double y = Math.Sin(x);
                double g = Plattformrundung.Verschieben(y, x, salz);

                Assert.Equal(Bits(g), Bits(Plattformrundung.Verschieben(y, x, salz)));

                gesamt++;
                if (Bits(g) == Bits(y)) continue;
                if (Bits(g) == Bits(Math.BitIncrement(y))) hoch++;
                else if (Bits(g) == Bits(Math.BitDecrement(y))) runter++;
                else Assert.Fail("Mehr als ein ulp verschoben: " + y.ToString("R") + " -> " + g.ToString("R"));
            }

            double anteil = (hoch + runter) / (double)gesamt;
            Assert.InRange(anteil, 0.05, 0.075);
            Assert.True(hoch > 0 && runter > 0, "Beide Richtungen müssen vorkommen.");
        }

        /// <summary>
        /// 0, ±1 und nicht endliche Werte bleiben stehen, gleich welches Argument — jede
        /// Bibliothek trifft sie exakt, und ±1 dürfen Sinus und Kosinus nicht verlassen.
        /// </summary>
        [Fact]
        public void Exakte_und_nicht_endliche_Werte_bleiben_stehen()
        {
            double[] feste = { 0.0, -0.0, 1.0, -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            foreach (double wert in feste)
                foreach (double x in Argumente())
                    for (ulong salz = 1; salz <= 5; salz++)
                        Assert.Equal(Bits(wert), Bits(Plattformrundung.Verschieben(wert, x, salz)));
        }

        /// <summary>
        /// Jede Funktion hat ihr eigenes Salz: Welche Argumente gestört werden, hängt von ihm ab.
        /// Sonst wären Sin und Cos desselben Arguments immer zugleich verschoben.
        /// </summary>
        [Fact]
        public void Verschiedene_Salze_stoeren_verschiedene_Argumente()
        {
            var a = new HashSet<double>();
            var b = new HashSet<double>();
            foreach (double x in Argumente())
            {
                if (Plattformrundung.Verschieben(0.5, x, 2) != 0.5) a.Add(x);
                if (Plattformrundung.Verschieben(0.5, x, 3) != 0.5) b.Add(x);
            }
            Assert.NotEmpty(a);
            Assert.NotEmpty(b);
            Assert.False(a.SetEquals(b));
        }

        // =====================================================================
        //  3 — Keine Codezeile der Nahtdateien ruft an der Naht vorbei
        // =====================================================================

        [Fact]
        public void In_den_Nahtdateien_laufen_die_fuenf_Funktionen_ueber_die_Naht()
        {
            string wurzel = Repowurzel();
            Assert.NotNull(wurzel);

            var vorbei = new Regex(@"(?<![\w.])Math\.(Exp|Sin|Cos|Asin|Acos)\(");
            var funde = new List<string>();
            int ueberDieNaht = 0;

            foreach (string rel in NAHTDATEIEN)
            {
                string[] zeilen = File.ReadAllLines(Path.Combine(wurzel, rel));
                for (int i = 0; i < zeilen.Length; i++)
                {
                    string code = zeilen[i].Split("//")[0];
                    if (code.TrimStart().StartsWith("*", StringComparison.Ordinal)) continue;
                    if (vorbei.IsMatch(code)) funde.Add(rel + ":" + (i + 1) + "  " + zeilen[i].Trim());
                    if (code.Contains("Plattformrundung.", StringComparison.Ordinal)) ueberDieNaht++;
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Zeilen rufen Exp/Sin/Cos/Asin/Acos an der Naht Plattformrundung vorbei - " +
                "der gestörte Referenzlauf prüft sie dann nicht:\n" + string.Join("\n", funde));

            // GEGENPROBE: Die Liste ist nicht leer gelaufen - die Dateien tragen die Naht.
            Assert.True(ueberDieNaht >= NAHTDATEIEN.Length, "Die Nahtdateien rufen die Naht nicht.");
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        /// <summary>
        /// Argumente der Probe: ein festes Raster über die Bereiche von Gebäudematrix (negative
        /// Exponenten), Winkeln und Jahreszeit, dazu Sonderwerte.
        /// </summary>
        private static IEnumerable<double> Argumente()
        {
            for (int i = -2000; i <= 2000; i++) yield return i * 0.00731;
            yield return 0.0;
            yield return -0.0;
            yield return Math.PI / 2;
            yield return Math.PI;
            yield return 1e-300;
            yield return -745.0;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
