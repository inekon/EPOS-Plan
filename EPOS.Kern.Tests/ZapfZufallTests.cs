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
    /// <b>Der portable Zufall</b> (Umsetzungskonzept Zapfprofilgenerator 4.4, ZU8): dieselbe Folge
    /// auf jeder Plattform — gegen eine feste Erwartungsfolge und gegen die unabhängig in Python
    /// gerechnete Referenz <c>Proben/Zapfprofil/zufall_referenz.csv</c>, Bit für Bit —, dazu die
    /// statistischen Proben der Ziehungen mit Toleranz und die Quelltextprobe „keine transzendente
    /// Funktion in der Ziehung".
    /// </summary>
    public sealed class ZapfZufallTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Plattformtest
        // =================================================================================

        [Fact]
        public void Die_Folge_ist_auf_jeder_Plattform_dieselbe()
        {
            // SplitMix64 ab Zustand 0: die veröffentlichte Folge des Verfahrens.
            ulong zustand = 0;
            Assert.Equal(0xE220A8397B1DCDAFUL, ZapfZufall.SplitMix64(ref zustand));
            Assert.Equal(0x6E789E6AA1B965F4UL, ZapfZufall.SplitMix64(ref zustand));
            Assert.Equal(0x06C45D188009454FUL, ZapfZufall.SplitMix64(ref zustand));
            Assert.Equal(0xF88BB8A8724C81ECUL, ZapfZufall.SplitMix64(ref zustand));

            // xoshiro256** zum Seed 1 — feste Erwartungsfolge.
            var z = new ZapfZufall(1);
            Assert.Equal(0xB3F2AF6D0FC710C5UL, z.Naechste());
            Assert.Equal(0x853B559647364CEAUL, z.Naechste());
            Assert.Equal(0x92F89756082A4514UL, z.Naechste());
            Assert.Equal(0x642E1C7BC266A3A7UL, z.Naechste());
            Assert.Equal(0xB27A48E29A233673UL, z.Naechste());

            // Die ganze Referenz, Bit für Bit.
            Dictionary<string, string> r = Referenz();
            int verglichen = 0;
            zustand = 0;
            for (int i = 0; i < 4; i++) verglichen += Hex(r, "splitmix_0_" + i, ZapfZufall.SplitMix64(ref zustand));
            foreach (long seed in new[] { 0L, 1L, 42L, -1L, long.MaxValue })
                for (int k = 0; k < 3; k++)
                    verglichen += Hex(r, "realisierungsseed_" + seed.ToString(CultureInfo.InvariantCulture) + "_" + k,
                                      ZapfZufall.Realisierungsseed(seed, k));
            ulong basis = ZapfZufall.Realisierungsseed(1, 0);
            for (int i = 0; i < 3; i++) verglichen += Hex(r, "kindseed_" + i, ZapfZufall.Kindseed(basis, i));
            foreach (ulong seed in new[] { 0UL, 1UL, 42UL, ulong.MaxValue })
            {
                var g = new ZapfZufall(seed);
                for (int i = 0; i < 20; i++)
                    verglichen += Hex(r, "naechste_" + seed.ToString("X", CultureInfo.InvariantCulture) + "_" + i.ToString("00"), g.Naechste());
            }
            var gl = new ZapfZufall(ZapfZufall.Realisierungsseed(1, 0));
            for (int i = 0; i < 20; i++) verglichen += Bits(r, "gleich_" + i.ToString("00"), gl.Gleich());
            foreach (int n in new[] { 1, 7, 60, 1440, 1000000007 })
            {
                var g = new ZapfZufall((ulong)n);
                for (int i = 0; i < 20; i++)
                    verglichen += Ganz(r, "ganzzahl_" + n.ToString(CultureInfo.InvariantCulture) + "_" + i.ToString("00"), g.Ganzzahl(n));
            }
            var no = new ZapfZufall(7);
            for (int i = 0; i < 20; i++) verglichen += Bits(r, "normal_" + i.ToString("00"), no.Normal());
            var ex = new ZapfZufall(8);
            for (int i = 0; i < 20; i++) verglichen += Bits(r, "exponential_" + i.ToString("00"), ex.Exponential());
            foreach (var (lambda, name) in new[] { (0.5, "0_5"), (3.7, "3_7"), (20.0, "20") })
            {
                var g = new ZapfZufall(9);
                for (int i = 0; i < 20; i++) verglichen += Ganz(r, "poisson_" + name + "_" + i.ToString("00"), g.Poisson(lambda));
            }
            var ge = new ZapfZufall(ZapfZufall.Kindseed(ZapfZufall.Kindseed(ZapfZufall.Realisierungsseed(1, 2), 0), 3));
            for (int i = 0; i < 10; i++)
            {
                string p = "gemischt_" + i.ToString("00") + "_";
                verglichen += Ganz(r, p + "poisson", ge.Poisson(2.5));
                verglichen += Bits(r, p + "gleich", ge.Gleich());
                verglichen += Ganz(r, p + "minute", ge.Ganzzahl(60));
                verglichen += Bits(r, p + "normal", ge.Normal());
            }
            Assert.Equal(r.Count, verglichen);
            Assert.True(verglichen >= 350, verglichen + " Werte verglichen.");
        }

        [Fact]
        public void Derselbe_Seed_liefert_dieselbe_Folge_und_ein_anderer_eine_andere()
        {
            var a = new ZapfZufall(123);
            var b = new ZapfZufall(123);
            var c = new ZapfZufall(124);
            int gleich = 0;
            for (int i = 0; i < 1000; i++)
            {
                ulong x = a.Naechste();
                Assert.Equal(x, b.Naechste());
                if (x == c.Naechste()) gleich++;
            }
            Assert.Equal(0, gleich);
        }

        [Fact]
        public void Benachbarte_Seeds_teilen_keine_Realisierung()
        {
            var eins = Enumerable.Range(0, 64).Select(r => ZapfZufall.Realisierungsseed(1, r)).ToHashSet();
            var zwei = Enumerable.Range(0, 64).Select(r => ZapfZufall.Realisierungsseed(2, r)).ToHashSet();
            var null_ = Enumerable.Range(0, 64).Select(r => ZapfZufall.Realisierungsseed(0, r)).ToHashSet();
            Assert.Equal(64, eins.Count);
            Assert.Empty(eins.Intersect(zwei));
            Assert.Empty(eins.Intersect(null_));
            // Die Kindseeds einer Basis sind verschieden und hängen nur von Basis und Index ab.
            ulong basis = ZapfZufall.Realisierungsseed(5, 3);
            Assert.Equal(100, Enumerable.Range(0, 100).Select(i => ZapfZufall.Kindseed(basis, i)).Distinct().Count());
            Assert.Equal(ZapfZufall.Kindseed(basis, 17), ZapfZufall.Kindseed(ZapfZufall.Realisierungsseed(5, 3), 17));
        }

        // =================================================================================
        // Statistische Proben (Toleranz, feste Seeds)
        // =================================================================================

        private const int N = 200000;

        [Fact]
        public void Gleich_liegt_in_null_bis_eins_mit_Mittel_und_Varianz_der_Gleichverteilung()
        {
            var z = new ZapfZufall(11);
            var w = new double[N];
            for (int i = 0; i < N; i++)
            {
                w[i] = z.Gleich();
                Assert.True(w[i] >= 0.0 && w[i] < 1.0);
            }
            Assert.InRange(Mittel(w), 0.5 - 0.005, 0.5 + 0.005);
            Assert.InRange(Varianz(w), 1.0 / 12 * 0.98, 1.0 / 12 * 1.02);
        }

        [Fact]
        public void Ganzzahl_ist_gleichverteilt_und_bleibt_im_Bereich()
        {
            var z = new ZapfZufall(12);
            var zahl = new int[7];
            for (int i = 0; i < 70000; i++) zahl[z.Ganzzahl(7)]++;
            Assert.All(zahl, k => Assert.InRange(k, 10000 * 0.95, 10000 * 1.05));
            var m = new int[60];
            for (int i = 0; i < 60000; i++) m[z.Ganzzahl(60)]++;
            Assert.All(m, k => Assert.InRange(k, 1000 * 0.85, 1000 * 1.15));
            for (int i = 0; i < 100; i++) Assert.Equal(0, z.Ganzzahl(1));
            for (int i = 0; i < 1000; i++) Assert.InRange(z.Ganzzahl(int.MaxValue), 0, int.MaxValue - 1);
        }

        [Fact]
        public void Normal_hat_Mittel_null_Varianz_eins_und_bleibt_in_sechs_Sigma()
        {
            var z = new ZapfZufall(13);
            var w = new double[N];
            for (int i = 0; i < N; i++)
            {
                w[i] = z.Normal();
                Assert.InRange(w[i], -6.0, 6.0);
            }
            Assert.InRange(Mittel(w), -0.01, 0.01);
            Assert.InRange(Varianz(w), 0.98, 1.02);
            // Symmetrie: Anteil unter 0 und über 1,96 wie bei der Normalverteilung (±5 % relativ).
            Assert.InRange(w.Count(x => x < 0) / (double)N, 0.49, 0.51);
            Assert.InRange(w.Count(x => x > 1.96) / (double)N, 0.025 * 0.9, 0.025 * 1.1);
        }

        [Fact]
        public void Exponential_hat_Mittel_und_Varianz_eins()
        {
            var z = new ZapfZufall(14);
            var w = new double[N];
            for (int i = 0; i < N; i++)
            {
                w[i] = z.Exponential();
                Assert.True(w[i] >= 0.0);
            }
            Assert.InRange(Mittel(w), 0.99, 1.01);
            Assert.InRange(Varianz(w), 0.97, 1.03);
            // Gedächtnislosigkeit als Stichprobe: P(X > 1) ≈ e^-1 (die Probe darf rechnen, der Zufall nicht).
            Assert.InRange(w.Count(x => x > 1.0) / (double)N, Math.Exp(-1) * 0.98, Math.Exp(-1) * 1.02);
        }

        [Theory]
        [InlineData(0.05)]
        [InlineData(0.5)]
        [InlineData(3.7)]
        [InlineData(20.0)]
        public void Poisson_trifft_Mittel_und_Varianz(double lambda)
        {
            var z = new ZapfZufall(15);
            var w = new double[N];
            for (int i = 0; i < N; i++) w[i] = z.Poisson(lambda);
            Assert.InRange(Mittel(w), lambda * 0.98, lambda * 1.02);
            Assert.InRange(Varianz(w), lambda * 0.95, lambda * 1.05);
        }

        [Fact]
        public void Poisson_ohne_Mittel_zieht_nicht()
        {
            var a = new ZapfZufall(16);
            var b = new ZapfZufall(16);
            Assert.Equal(0, a.Poisson(0.0));
            Assert.Equal(0, a.Poisson(-1.0));
            Assert.Equal(b.Naechste(), a.Naechste());
        }

        [Fact]
        public void Ungueltige_Eingaben_werden_benannt_abgelehnt()
        {
            var z = new ZapfZufall(17);
            Assert.Throws<ArgumentOutOfRangeException>(() => z.Ganzzahl(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => z.Ganzzahl(-3));
            Assert.Throws<ArgumentOutOfRangeException>(() => z.Poisson(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => z.Poisson(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => ZapfZufall.Realisierungsseed(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ZapfZufall.Kindseed(1, -1));
        }

        // =================================================================================
        // ZU8: keine transzendente Funktion in der Ziehung
        // =================================================================================

        /// <summary>Die Dateien der Ziehung — keine nennt eine transzendente Funktion der Plattform.</summary>
        internal static readonly string[] Ziehdateien = { "ZapfZufall.cs" };

        private static readonly Regex Transzendent = new Regex(
            @"\bMath\s*\.\s*(Log|Log10|Log2|Exp|Cos|Sin|Tan|Pow|Atan|Atan2|Acos|Asin|Cosh|Sinh|Tanh|Cbrt)\s*\(|\bMathF\s*\.",
            RegexOptions.Compiled);

        [Fact]
        public void Die_Ziehung_nennt_keine_transzendente_Funktion()
        {
            var funde = new List<string>();
            foreach (string datei in Ziehdateien)
            {
                string[] zeilen = File.ReadAllText(Path.Combine(Zapfprofilordner(), datei)).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < zeilen.Length; i++)
                {
                    string s = zeilen[i].TrimStart();
                    if (s.StartsWith("//", StringComparison.Ordinal)) continue;
                    if (Transzendent.IsMatch(zeilen[i])) funde.Add(datei + ":" + (i + 1) + "  " + s);
                }
            }
            Assert.True(funde.Count == 0, "Transzendente Funktion in der Ziehung:\n" + string.Join("\n", funde));
            Assert.Matches(Transzendent, "double x = Math.Log(u);");
            Assert.Matches(Transzendent, "double y = Math.Exp (-l);");
            Assert.DoesNotMatch(Transzendent, "double s = Math.Sqrt(v); double m = Math.Max(a, b);");
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static double Mittel(double[] w) => w.Sum() / w.Length;

        private static double Varianz(double[] w)
        {
            double m = Mittel(w);
            return w.Sum(x => (x - m) * (x - m)) / (w.Length - 1);
        }

        private static int Hex(Dictionary<string, string> r, string name, ulong ist)
        {
            Assert.True(r.TryGetValue(name, out string soll), "Die Referenz nennt „" + name + "“ nicht.");
            Assert.Equal(soll, "0x" + ist.ToString("X16", CultureInfo.InvariantCulture));
            return 1;
        }

        private static int Ganz(Dictionary<string, string> r, string name, int ist)
        {
            Assert.True(r.TryGetValue(name, out string soll), "Die Referenz nennt „" + name + "“ nicht.");
            Assert.Equal(soll, ist.ToString(CultureInfo.InvariantCulture));
            return 1;
        }

        private static int Bits(Dictionary<string, string> r, string name, double ist)
        {
            Assert.True(r.TryGetValue(name, out string soll), "Die Referenz nennt „" + name + "“ nicht.");
            double referenz = double.Parse(soll, NumberStyles.Float, CultureInfo.InvariantCulture);
            Assert.True(BitConverter.DoubleToInt64Bits(referenz) == BitConverter.DoubleToInt64Bits(ist),
                name + ": C# " + ist.ToString("R", CultureInfo.InvariantCulture) + ", Referenz " + soll);
            return 1;
        }

        private static Dictionary<string, string> Referenz()
        {
            var r = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string z in File.ReadAllLines(Path.Combine(Probenordner(), "zufall_referenz.csv")))
            {
                if (z.StartsWith("#", StringComparison.Ordinal) || z.StartsWith("groesse", StringComparison.Ordinal)
                    || z.Length == 0) continue;
                string[] t = z.Split(',');
                r.Add(t[0], t[1]);
            }
            return r;
        }

        internal static string Probenordner() => Ordner(Path.Combine("EPOS.Kern.Tests", "Proben", "Zapfprofil"));

        internal static string Zapfprofilordner() => Ordner(Path.Combine("EPOS.Kern", "Allgemein", "Zapfprofil"));

        private static string Ordner(string relativ)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, relativ);
                if (Directory.Exists(kandidat)) return kandidat;
            }
            Assert.Fail("Der Ordner " + relativ + " wurde nicht gefunden.");
            return null;
        }
    }
}
