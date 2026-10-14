using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Überlappung zweier Grundrisse</b> in der waagerechten Projektion (Mehrzonenkonzept 6.5, Trenndecke ohne
    /// Raumgrenzen) — ohne Geometriekern: Beide Ringe werden in Dreiecke zerlegt (Ohrenschnitt, auch für nicht konvexe
    /// Ringe), je Dreieckspaar die Schnittfläche nach Sutherland–Hodgman (konvex gegen konvex) summiert. Ringe sind
    /// Folgen von Punkten x, y [m] ohne Schlusspunkt; die Umlaufrichtung ist gleich.
    /// </summary>
    internal static class Grundrissueberlappung
    {
        /// <summary>Die vorzeichenbehaftete Fläche eines Rings (Gaußsche Trapezformel; gegen den Uhrzeigersinn positiv) [m²].</summary>
        internal static double Flaeche(IReadOnlyList<double[]> ring)
        {
            if (ring == null || ring.Count < 3) return 0.0;
            double s = 0.0;
            for (int i = 0; i < ring.Count; i++)
            {
                double[] a = ring[i], b = ring[(i + 1) % ring.Count];
                s += a[0] * b[1] - b[0] * a[1];
            }
            return s / 2.0;
        }

        /// <summary>Die Fläche, in der sich zwei Grundrisse in der Projektion überdecken [m²]; 0 = keine oder kein Ring.</summary>
        internal static double Ueberlappung(IReadOnlyList<double[]> a, IReadOnlyList<double[]> b)
        {
            if (a == null || b == null || a.Count < 3 || b.Count < 3) return 0.0;
            if (a.Max(p => p[0]) <= b.Min(p => p[0]) || b.Max(p => p[0]) <= a.Min(p => p[0])
                || a.Max(p => p[1]) <= b.Min(p => p[1]) || b.Max(p => p[1]) <= a.Min(p => p[1])) return 0.0;
            List<double[][]> da = Dreiecke(a), db = Dreiecke(b);
            double summe = 0.0;
            foreach (double[][] x in da)
                foreach (double[][] y in db)
                    summe += Math.Abs(Flaeche(Schneiden(x, y)));
            return summe;
        }

        /// <summary>Die Dreiecke eines einfachen Rings (Ohrenschnitt), gegen den Uhrzeigersinn.</summary>
        internal static List<double[][]> Dreiecke(IReadOnlyList<double[]> ring)
        {
            var punkte = ring.Select(p => new[] { p[0], p[1] }).ToList();
            if (Flaeche(punkte) < 0.0) punkte.Reverse();
            var ergebnis = new List<double[][]>();
            int schutz = punkte.Count * punkte.Count + 10;
            while (punkte.Count > 3 && schutz-- > 0)
            {
                bool geschnitten = false;
                for (int i = 0; i < punkte.Count; i++)
                {
                    double[] p = punkte[(i + punkte.Count - 1) % punkte.Count], q = punkte[i], r = punkte[(i + 1) % punkte.Count];
                    double kreuz = (q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0]);
                    if (kreuz <= 1e-12) continue;   // spitz nach innen oder gerade
                    bool leer = true;
                    for (int k = 0; k < punkte.Count && leer; k++)
                    {
                        double[] t = punkte[k];
                        if (ReferenceEquals(t, p) || ReferenceEquals(t, q) || ReferenceEquals(t, r)) continue;
                        if (ImDreieck(t, p, q, r)) leer = false;
                    }
                    if (!leer) continue;
                    ergebnis.Add(new[] { p, q, r });
                    punkte.RemoveAt(i);
                    geschnitten = true;
                    break;
                }
                if (!geschnitten)
                {
                    // Entarteter Ring (Selbstüberschneidung, kollineare Reste): der Rest als Fächer.
                    for (int i = 1; i + 1 < punkte.Count; i++) ergebnis.Add(new[] { punkte[0], punkte[i], punkte[i + 1] });
                    return ergebnis;
                }
            }
            if (punkte.Count == 3) ergebnis.Add(punkte.ToArray());
            return ergebnis;
        }

        private static bool ImDreieck(double[] t, double[] a, double[] b, double[] c)
        {
            double d1 = Seite(t, a, b), d2 = Seite(t, b, c), d3 = Seite(t, c, a);
            return d1 >= 0.0 && d2 >= 0.0 && d3 >= 0.0;
        }

        private static double Seite(double[] t, double[] a, double[] b) => (b[0] - a[0]) * (t[1] - a[1]) - (b[1] - a[1]) * (t[0] - a[0]);

        /// <summary>Sutherland–Hodgman: das Dreieck <paramref name="subjekt"/> am konvexen Dreieck <paramref name="schnitt"/> (gegen den Uhrzeigersinn).</summary>
        private static List<double[]> Schneiden(double[][] subjekt, double[][] schnitt)
        {
            var aus = subjekt.ToList();
            for (int i = 0; i < schnitt.Length && aus.Count > 0; i++)
            {
                double[] a = schnitt[i], b = schnitt[(i + 1) % schnitt.Length];
                var ein = aus;
                aus = new List<double[]>();
                for (int k = 0; k < ein.Count; k++)
                {
                    double[] p = ein[k], q = ein[(k + 1) % ein.Count];
                    double sp = Seite(p, a, b), sq = Seite(q, a, b);
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
    }
}
