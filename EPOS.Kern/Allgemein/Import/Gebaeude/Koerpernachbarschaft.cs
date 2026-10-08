using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Woher eine Grenze zwischen zwei Räumen stammt — Rangfolge Raumgrenzen vor Körpern vor Raumbezügen.</summary>
    internal enum Grenzherkunft
    {
        /// <summary>Aus einer Raumgrenze der Datei (<c>IfcRelSpaceBoundary</c>, gbXML <c>Surface</c>).</summary>
        Raumgrenze = 0,

        /// <summary>Aus zwei gegenläufigen, gemeinsamen Flächen zweier Raumkörper (<see cref="Koerpernachbarschaft"/>).</summary>
        Koerper = 1,

        /// <summary>Aus den Raumbezügen der Datei (<c>IfcRelReferencedInSpatialStructure</c>).</summary>
        Raumbezug = 2,

        /// <summary>
        /// Aus der Überlappung eines Raumkörpers mit dem Körper des Bauteils (<see cref="Koerperflaechen"/>, Abstimmung G5,
        /// Teil G5-3) — nur, wo die Datei weder Raumgrenzen noch Raumbezüge liefert.
        /// </summary>
        Bauteilkoerper = 3,
    }

    /// <summary>
    /// <b>Ein Flächenpaar zweier Raumkörper</b>: die Trennfläche zweier Räume, wie die Körper der Datei sie zeigen. Die
    /// Seite A ist der Raum mit der kleineren Stelle in der Eingabe; Punkte in Weltkoordinaten [m], auf 1e‑6 gerundet.
    /// </summary>
    internal sealed class Koerperpaar
    {
        /// <summary>Die Stelle des Raums A in der Eingabe.</summary>
        internal int RaumA { get; init; }

        /// <summary>Die Stelle des Raums B in der Eingabe.</summary>
        internal int RaumB { get; init; }

        /// <summary>Eine Trenndecke (sonst eine Trennwand: Normale waagerecht ± <see cref="Koerpernachbarschaft.WAND_NEIGUNG_GRAD"/>).</summary>
        internal bool Decke { get; init; }

        /// <summary>Bei einer Trenndecke die Stelle des oberen Raums (dessen Fläche nach unten weist); sonst −1.</summary>
        internal int Oben { get; init; } = -1;

        /// <summary>Die Schnittfläche [m²].</summary>
        internal double FlaecheM2 { get; init; }

        /// <summary>Der Ebenenabstand der beiden Flächen [m] (Wand- oder Deckendicke).</summary>
        internal double AbstandM { get; init; }

        /// <summary>Die Einheitsnormale der Fläche von A (von A weg, zu B hin).</summary>
        internal double[] NormaleA { get; init; }

        /// <summary>Der Flächenschwerpunkt der Schnittfläche in der Ebene von A bzw. B.</summary>
        internal double[] SchwerpunktA { get; init; }

        /// <summary>Der Flächenschwerpunkt der Schnittfläche in der Ebene von B.</summary>
        internal double[] SchwerpunktB { get; init; }

        /// <summary>Der Ring der Schnittfläche in der Ebene von A, Umlauf zur Normalen von A; <c>null</c> = nicht konvex darstellbar.</summary>
        internal IReadOnlyList<double[]> RandA { get; init; }

        /// <summary>Der Ring in der Ebene von B, Umlauf zur Normalen von B; <c>null</c> wie <see cref="RandA"/>.</summary>
        internal IReadOnlyList<double[]> RandB { get; init; }
    }

    /// <summary>
    /// <b>Die Nachbarschaft der Räume aus ihren Körpern</b> (Mehrzonenkonzept 6.2, „Trennflächen aus Raumkörpern“): Aus
    /// jedem <see cref="Dateikoerper"/> die ebenen Flächen (Dreiecke gleicher Normale und gleichen Ebenenabstands);
    /// zwei Flächen verschiedener Räume sind ein Paar, wenn ihre Normalen gegenläufig sind (höchstens
    /// <see cref="WINKEL_MAX_GRAD"/>), sie einander zugewandt höchstens <see cref="TRENNDICKE_MAX_M"/> auseinanderliegen
    /// und sich ihre Projektionen auf die gemeinsame Ebene um mindestens <see cref="FLAECHE_MIN_M2"/> überschneiden. Die
    /// Trennfläche ist die Schnittfläche: die Summe der Schnitte der Dreiecke beider Flächen (konvex gegen konvex), exakt
    /// für jede Form; der Ring wird nur geführt, wo die Schnittfläche konvex ist.
    ///
    /// <para><b>Deterministisch und n·log n:</b> Räume in der Reihenfolge der Eingabe, Flächen in der Reihenfolge ihres
    /// ersten Dreiecks; die Flächen liegen in Fächern nach ihrer Richtung und darin nach dem Ebenenabstand sortiert —
    /// gesucht wird nur in den Nachbarfächern im Fenster der Trenndicke, nie alle Räume gegen alle.</para>
    /// </summary>
    internal static class Koerpernachbarschaft
    {
        /// <summary>Größter Ebenenabstand zweier Flächen eines Paars [m] — Wand- oder Deckendicke.</summary>
        internal const double TRENNDICKE_MAX_M = 0.8;

        /// <summary>Kleinste Trennfläche [m²]; kleinere Schnitte entfallen.</summary>
        internal const double FLAECHE_MIN_M2 = 0.1;

        /// <summary>Größte Abweichung der Normalen von der Gegenrichtung [°].</summary>
        internal const double WINKEL_MAX_GRAD = 1.0;

        /// <summary>Eine Fläche mit waagerechter Normale bis zu dieser Abweichung [°] ist eine Wand, sonst eine Decke.</summary>
        internal const double WAND_NEIGUNG_GRAD = 10.0;

        /// <summary>Überlappung zweier Flächen bei der Zuwendung [m] — Rundung und anstoßende Körper.</summary>
        internal const double UEBERLAPPUNG_M = 0.05;

        private const double FACH = 0.02;
        private const double EPS = 1e-9;

        private sealed class Flaeche
        {
            internal int Raum;
            internal double[] N;       // die Normale der Datei (vom Raum weg)
            internal double[] C;       // die kanonische Richtung (erste wesentliche Komponente positiv)
            internal double S;         // Ebenenabstand entlang C
            internal int Vorzeichen;   // N = Vorzeichen · C
            internal List<double[][]> Dreiecke = new List<double[][]>();
        }

        /// <summary>Die Flächenpaare der Räume eines Gebäudes; Räume ohne Körper tragen nichts bei.</summary>
        internal static List<Koerperpaar> Paare(IReadOnlyList<AbbildRaum> raeume)
            => Paare(raeume?.Select(r => r?.Koerper).ToList() ?? new List<Dateikoerper>());

        /// <summary>
        /// Die Flächenpaare einer Reihe von Körpern (Stelle = Raum); <c>null</c>-Körper tragen nichts bei, ein aus Flächen
        /// gebildeter Körper (<see cref="Dateikoerper.IstBeleg"/> falsch) ebenso — er ist kein unabhängiger Beleg (17.5): Seine
        /// Trennflächen kommen aus den Raumbezügen der Hüllflächen der Datei, nie aus einem Körperpaar.
        /// </summary>
        internal static List<Koerperpaar> Paare(IReadOnlyList<Dateikoerper> koerper)
        {
            var flaechen = new List<Flaeche>();
            for (int r = 0; r < (koerper?.Count ?? 0); r++)
                if (Dateikoerper.Beleg(koerper[r])) flaechen.AddRange(Flaechen(koerper[r], r));

            // Fächer nach Richtung, darin nach Ebenenabstand sortiert.
            var faecher = new Dictionary<(long, long, long), List<int>>();
            for (int i = 0; i < flaechen.Count; i++)
            {
                (long, long, long) k = Fach(flaechen[i].C);
                if (!faecher.TryGetValue(k, out List<int> l)) faecher[k] = l = new List<int>();
                l.Add(i);
            }
            var sortiert = new Dictionary<(long, long, long), (double[] S, int[] I)>();
            foreach (KeyValuePair<(long, long, long), List<int>> f in faecher)
            {
                int[] idx = f.Value.OrderBy(i => flaechen[i].S).ThenBy(i => i).ToArray();
                sortiert[f.Key] = (idx.Select(i => flaechen[i].S).ToArray(), idx);
            }

            double cosMax = Math.Cos(WINKEL_MAX_GRAD * Math.PI / 180.0);
            double sinWand = Math.Sin(WAND_NEIGUNG_GRAD * Math.PI / 180.0);
            var paare = new List<(int Fa, int Fb, Koerperpaar P)>();
            for (int a = 0; a < flaechen.Count; a++)
            {
                Flaeche fa = flaechen[a];
                (long x, long y, long z) = Fach(fa.C);
                var kandidaten = new List<int>();
                for (long dx = -1; dx <= 1; dx++)
                    for (long dy = -1; dy <= 1; dy++)
                        for (long dz = -1; dz <= 1; dz++)
                        {
                            if (!sortiert.TryGetValue((x + dx, y + dy, z + dz), out (double[] S, int[] I) fach)) continue;
                            int von = Untergrenze(fach.S, fa.S - TRENNDICKE_MAX_M - UEBERLAPPUNG_M - FACH);
                            for (int j = von; j < fach.S.Length && fach.S[j] <= fa.S + TRENNDICKE_MAX_M + UEBERLAPPUNG_M + FACH; j++)
                                if (fach.I[j] > a) kandidaten.Add(fach.I[j]);
                        }
                kandidaten.Sort();
                foreach (int b in kandidaten)
                {
                    Flaeche fb = flaechen[b];
                    if (fb.Raum == fa.Raum) continue;
                    if (Punkt(fa.N, fb.N) > -cosMax) continue;
                    // Zugewandt: Die Ebene von B liegt vor A (in Richtung der Normalen von A), höchstens die Trenndicke.
                    double pb = Punkt(fa.N, fb.Dreiecke[0][0]), pa = Punkt(fa.N, fa.Dreiecke[0][0]);
                    double abstand = pb - pa;
                    if (abstand < -UEBERLAPPUNG_M || abstand > TRENNDICKE_MAX_M) continue;
                    Koerperpaar p = Schnitt(fa, fb, Math.Max(0.0, abstand), sinWand);
                    if (p != null) paare.Add((a, b, p));
                }
            }
            return paare.OrderBy(p => p.P.RaumA).ThenBy(p => p.Fa).ThenBy(p => p.P.RaumB).ThenBy(p => p.Fb).Select(p => p.P).ToList();
        }

        /// <summary>Die ebenen Flächen eines Körpers in der Reihenfolge ihres ersten Dreiecks.</summary>
        private static List<Flaeche> Flaechen(Dateikoerper k, int raum)
        {
            var liste = new List<Flaeche>();
            var jeSchluessel = new Dictionary<(long, long, long, long), Flaeche>();
            for (int t = 0; t < k.Dreiecke.Count; t++)
            {
                int[] d = k.Dreiecke[t];
                double[] p0 = k.PunkteM[d[0]], p1 = k.PunkteM[d[1]], p2 = k.PunkteM[d[2]];
                double[] n = Normale(p0, p1, p2);
                if (n == null) continue;
                double s = Punkt(n, p0);
                var schluessel = ((long)Math.Round(n[0] * 1e3), (long)Math.Round(n[1] * 1e3), (long)Math.Round(n[2] * 1e3), (long)Math.Round(s * 1e3));
                if (!jeSchluessel.TryGetValue(schluessel, out Flaeche f))
                {
                    double[] c = Kanonisch(n, out int vz);
                    f = new Flaeche { Raum = raum, N = n, C = c, Vorzeichen = vz, S = vz * s };
                    jeSchluessel[schluessel] = f;
                    liste.Add(f);
                }
                f.Dreiecke.Add(new[] { p0, p1, p2 });
            }
            return liste;
        }

        /// <summary>Die Schnittfläche zweier Flächen; <c>null</c> = unter <see cref="FLAECHE_MIN_M2"/>.</summary>
        private static Koerperpaar Schnitt(Flaeche fa, Flaeche fb, double abstand, double sinWand)
        {
            double[] c = fa.C;
            double[] u = Einheit(Kreuz(c, Math.Abs(c[2]) < 0.9 ? new[] { 0.0, 0.0, 1.0 } : new[] { 1.0, 0.0, 0.0 }));
            double[] v = Kreuz(c, u);
            List<double[][]> ta = fa.Dreiecke.Select(t => Gegen(Projektion(t, u, v))).ToList();
            List<double[][]> tb = fb.Dreiecke.Select(t => Gegen(Projektion(t, u, v))).ToList();
            double[][] ka = ta.Select(Kasten).ToArray(), kb = tb.Select(Kasten).ToArray();
            if (!Ueberlappt(Gesamtkasten(ka), Gesamtkasten(kb))) return null;

            var stuecke = new List<List<double[]>>();
            double flaeche = 0.0, su = 0.0, sv = 0.0;
            for (int i = 0; i < ta.Count; i++)
                for (int j = 0; j < tb.Count; j++)
                {
                    if (!Ueberlappt(ka[i], kb[j])) continue;
                    List<double[]> s = Klippen(ta[i], tb[j]);
                    if (s.Count < 3) continue;
                    double f = Inhalt(s, out double cu, out double cv);
                    if (f <= EPS) continue;
                    stuecke.Add(s);
                    flaeche += f;
                    su += f * cu;
                    sv += f * cv;
                }
            if (flaeche < FLAECHE_MIN_M2) return null;
            su /= flaeche;
            sv /= flaeche;

            // Der Ring: ein Stück selbst, sonst die konvexe Hülle, wo sie die Fläche trägt.
            List<double[]> ring = stuecke.Count == 1 ? stuecke[0] : Huelle(stuecke.SelectMany(x => x).ToList());
            if (ring != null && Math.Abs(Inhalt(ring, out _, out _) - flaeche) > 0.01 * flaeche) ring = null;

            double sa = fa.S, sb = fb.S;
            double[] Welt(double pu, double pv, double ebene)
                => new[] { R(pu * u[0] + pv * v[0] + ebene * c[0]), R(pu * u[1] + pv * v[1] + ebene * c[1]), R(pu * u[2] + pv * v[2] + ebene * c[2]) };
            List<double[]> Ring(double ebene, int vorzeichen)
            {
                if (ring == null) return null;
                List<double[]> r = ring.Select(p => Welt(p[0], p[1], ebene)).ToList();
                if (vorzeichen < 0) r.Reverse();   // gegen den Uhrzeigersinn um C; um −C umgekehrt
                return r;
            }
            bool decke = Math.Abs(c[2]) > sinWand;
            return new Koerperpaar
            {
                RaumA = fa.Raum, RaumB = fb.Raum, Decke = decke,
                Oben = !decke ? -1 : fa.N[2] < 0.0 ? fa.Raum : fb.Raum,
                FlaecheM2 = R(flaeche), AbstandM = R(abstand),
                NormaleA = fa.N.Select(R).ToArray(),
                SchwerpunktA = Welt(su, sv, sa), SchwerpunktB = Welt(su, sv, sb),
                RandA = Ring(sa, fa.Vorzeichen), RandB = Ring(sb, fb.Vorzeichen),
            };
        }

        // ------------------------------------------------------------------
        //  Geometrie
        // ------------------------------------------------------------------

        private static (long, long, long) Fach(double[] c)
            => ((long)Math.Floor(c[0] / FACH), (long)Math.Floor(c[1] / FACH), (long)Math.Floor(c[2] / FACH));

        private static int Untergrenze(double[] s, double wert)
        {
            int lo = 0, hi = s.Length;
            while (lo < hi)
            {
                int m = (lo + hi) / 2;
                if (s[m] < wert) lo = m + 1; else hi = m;
            }
            return lo;
        }

        private static double[] Normale(double[] a, double[] b, double[] c)
        {
            double[] n = Kreuz(new[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] }, new[] { c[0] - a[0], c[1] - a[1], c[2] - a[2] });
            double l = Math.Sqrt(Punkt(n, n));
            return l < 1e-12 ? null : new[] { n[0] / l, n[1] / l, n[2] / l };
        }

        private static double[] Kanonisch(double[] n, out int vz)
        {
            const double e = 1e-6;
            vz = Math.Abs(n[2]) > e ? Math.Sign(n[2]) : Math.Abs(n[1]) > e ? Math.Sign(n[1]) : Math.Sign(n[0]);
            if (vz == 0) vz = 1;
            return new[] { vz * n[0], vz * n[1], vz * n[2] };
        }

        private static double Punkt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        private static double[] Kreuz(double[] a, double[] b)
            => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        private static double[] Einheit(double[] a)
        {
            double l = Math.Sqrt(Punkt(a, a));
            return new[] { a[0] / l, a[1] / l, a[2] / l };
        }

        private static double[][] Projektion(double[][] t, double[] u, double[] v)
            => t.Select(p => new[] { Punkt(p, u), Punkt(p, v) }).ToArray();

        /// <summary>Ein Dreieck gegen den Uhrzeigersinn.</summary>
        private static double[][] Gegen(double[][] t)
        {
            double f = (t[1][0] - t[0][0]) * (t[2][1] - t[0][1]) - (t[2][0] - t[0][0]) * (t[1][1] - t[0][1]);
            return f >= 0.0 ? t : new[] { t[0], t[2], t[1] };
        }

        private static double[] Kasten(double[][] t)
            => new[] { t.Min(p => p[0]), t.Min(p => p[1]), t.Max(p => p[0]), t.Max(p => p[1]) };

        private static double[] Gesamtkasten(double[][] kaesten) => kaesten.Length == 0 ? new double[4]
            : new[] { kaesten.Min(k => k[0]), kaesten.Min(k => k[1]), kaesten.Max(k => k[2]), kaesten.Max(k => k[3]) };

        private static bool Ueberlappt(double[] a, double[] b)
            => a[0] < b[2] - EPS && b[0] < a[2] - EPS && a[1] < b[3] - EPS && b[1] < a[3] - EPS;

        /// <summary>Sutherland–Hodgman: ein konvexes Vieleck gegen ein Dreieck gegen den Uhrzeigersinn.</summary>
        private static List<double[]> Klippen(double[][] subjekt, double[][] klinge)
        {
            var aus = new List<double[]>(subjekt);
            for (int k = 0; k < 3 && aus.Count > 0; k++)
            {
                double[] a = klinge[k], b = klinge[(k + 1) % 3];
                double Seite(double[] p) => (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0]);
                var ein = aus;
                aus = new List<double[]>();
                for (int i = 0; i < ein.Count; i++)
                {
                    double[] p = ein[i], q = ein[(i + 1) % ein.Count];
                    double sp = Seite(p), sq = Seite(q);
                    if (sp >= 0.0) aus.Add(p);
                    if ((sp >= 0.0) != (sq >= 0.0))
                    {
                        double t = sp / (sp - sq);
                        aus.Add(new[] { p[0] + t * (q[0] - p[0]), p[1] + t * (q[1] - p[1]) });
                    }
                }
            }
            return aus;
        }

        /// <summary>Der Inhalt eines Vielecks (gegen den Uhrzeigersinn positiv) und sein Schwerpunkt.</summary>
        private static double Inhalt(List<double[]> p, out double cu, out double cv)
        {
            double a = 0.0, x = 0.0, y = 0.0;
            for (int i = 0; i < p.Count; i++)
            {
                double[] s = p[i], t = p[(i + 1) % p.Count];
                double k = s[0] * t[1] - t[0] * s[1];
                a += k;
                x += (s[0] + t[0]) * k;
                y += (s[1] + t[1]) * k;
            }
            a *= 0.5;
            cu = Math.Abs(a) > EPS ? x / (6.0 * a) : p.Average(q => q[0]);
            cv = Math.Abs(a) > EPS ? y / (6.0 * a) : p.Average(q => q[1]);
            return a;
        }

        /// <summary>Die konvexe Hülle gegen den Uhrzeigersinn (Andrew); <c>null</c> = entartet.</summary>
        private static List<double[]> Huelle(List<double[]> punkte)
        {
            List<double[]> p = punkte.OrderBy(q => q[0]).ThenBy(q => q[1]).ToList();
            if (p.Count < 3) return null;
            double X(double[] o, double[] a, double[] b) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
            var h = new List<double[]>();
            foreach (double[] q in p)
            {
                while (h.Count >= 2 && X(h[h.Count - 2], h[h.Count - 1], q) <= 1e-12) h.RemoveAt(h.Count - 1);
                h.Add(q);
            }
            int unten = h.Count + 1;
            for (int i = p.Count - 2; i >= 0; i--)
            {
                while (h.Count >= unten && X(h[h.Count - 2], h[h.Count - 1], p[i]) <= 1e-12) h.RemoveAt(h.Count - 1);
                h.Add(p[i]);
            }
            h.RemoveAt(h.Count - 1);
            return h.Count >= 3 ? h : null;
        }

        private static double R(double w) => Math.Round(w, 6);
    }
}
