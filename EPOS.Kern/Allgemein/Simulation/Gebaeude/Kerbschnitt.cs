using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Ring in der Ebene einer Wand (2D) mit einer Kennung je Kante — Kante <c>i</c> läuft von Punkt <c>i</c> zu
    /// Punkt <c>i + 1</c> — und der Herkunft je Punkt: die Stelle im Außenring der Wand, aus dem der Punkt stammt, oder
    /// −1 für einen Punkt, den erst der Schnitt erzeugt hat.
    /// </summary>
    internal sealed class Kerbring
    {
        internal List<double[]> Punkte { get; init; } = new List<double[]>();

        internal List<string> Kanten { get; init; } = new List<string>();

        internal List<int> Herkunft { get; init; } = new List<int>();
    }

    /// <summary>Die Lage einer Öffnung zu einem Ring in derselben Ebene.</summary>
    internal enum Kerblage
    {
        /// <summary>Ganz im Inneren, jede Ecke weiter als die Toleranz vom Rand: ein Loch.</summary>
        Innen,
        /// <summary>Berührt oder schneidet den Rand: eine Kerbe.</summary>
        Beruehrt,
        /// <summary>Ganz außerhalb, ohne Berührung.</summary>
        Aussen,
        /// <summary>Der Ring liegt ganz in der Öffnung.</summary>
        Deckt,
    }

    /// <summary>
    /// <b>Die Randkerbe</b> (Datenaustauschkonzept 17.3 Nr. 4): die Differenz Wandring minus Öffnungsring in der Ebene der
    /// Wand, formatfrei und deterministisch. Beide Ringe werden an ihren Schnitt- und Berührpunkten geteilt (Punkte auf
    /// die Toleranz zusammengelegt, der kleinste Index gewinnt), die Teilkanten bilden einen ebenen Graphen, dessen
    /// Flächen über die Kante mit der kleinsten Linksdrehung umlaufen werden; behalten werden die Flächen links einer
    /// Wandkante in Wandrichtung und nicht links einer Öffnungskante in Öffnungsrichtung. Jede Ergebniskante trägt die
    /// Kennung ihrer Herkunft: eine Kante auf dem Wandrand die der Wand, eine Kante der Öffnung im Inneren der Wand die
    /// der Öffnung — dort entsteht die Laibung. Ohne Hash-Reihenfolge, ohne Zufall, ohne Plattformzugriff.
    /// </summary>
    internal static class Kerbschnitt
    {
        /// <summary>Die Lage des Rings <paramref name="b"/> zum Ring <paramref name="a"/> (beide 2D, beliebiger Umlauf).</summary>
        internal static Kerblage Lage(IReadOnlyList<double[]> a, IReadOnlyList<double[]> b, double tol)
        {
            if (b.Any(q => RandAbstand(a, q) <= tol) || a.Any(q => RandAbstand(b, q) <= tol)) return Kerblage.Beruehrt;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    if (Schnitt(a[i], a[(i + 1) % a.Count], b[j], b[(j + 1) % b.Count]) != null) return Kerblage.Beruehrt;
            if (Innen(a, b[0])) return Kerblage.Innen;
            if (Innen(b, a[0])) return Kerblage.Deckt;
            return Kerblage.Aussen;
        }

        /// <summary>
        /// <paramref name="a"/> minus <paramref name="bRoh"/>. Ergebnis: die Teilringe gegen den Uhrzeigersinn (leer, wenn
        /// die Öffnung den Ring ganz deckt); <c>null</c>, wenn sich die Ringe nicht berühren (<paramref name="beruehrt"/>
        /// = false) oder der Schnitt kein einfaches Vieleck ergibt (<paramref name="beruehrt"/> = true).
        /// </summary>
        internal static List<Kerbring> Differenz(Kerbring a, IReadOnlyList<double[]> bRoh, string kennungB, double tol, out bool beruehrt)
        {
            beruehrt = false;
            Kerbring ra = Gegenuhr(a);
            List<double[]> rb = Bereinigt(bRoh, tol);
            if (ra.Punkte.Count < 3 || rb.Count < 3) return null;
            if (Flaeche(rb) < 0.0) rb.Reverse();

            // 1. Punkte: Wand, Öffnung, Schnittpunkte — zusammengelegt auf die Toleranz.
            var punkte = new List<double[]>();
            var herkunft = new List<int>();
            int Stelle(double[] p, int her)
            {
                for (int i = 0; i < punkte.Count; i++)
                    if (Abstand2(punkte[i], p) <= tol * tol)
                    {
                        if (herkunft[i] < 0 && her >= 0) herkunft[i] = her;
                        return i;
                    }
                punkte.Add(new[] { p[0], p[1] });
                herkunft.Add(her);
                return punkte.Count - 1;
            }
            int na = ra.Punkte.Count, nb = rb.Count;
            int[] ia = new int[na], ib = new int[nb];
            for (int i = 0; i < na; i++) ia[i] = Stelle(ra.Punkte[i], ra.Herkunft[i]);
            for (int j = 0; j < nb; j++) ib[j] = Stelle(rb[j], -1);
            for (int i = 0; i < na; i++)
                for (int j = 0; j < nb; j++)
                {
                    double[] s = Schnitt(ra.Punkte[i], ra.Punkte[(i + 1) % na], rb[j], rb[(j + 1) % nb]);
                    if (s != null) Stelle(s, -1);
                }

            // 2. Teilkanten: jede Ringkante an den Punkten in ihrem Inneren geteilt; ungerichtet einmal, mit Richtung je Ring.
            var kanten = new Dictionary<(int, int), Teilkante>();
            var folge = new List<(int, int)>();
            bool Eintragen(int u, int v, string kennung, bool istWand)
            {
                var teil = new List<(double T, int I)>();
                double[] p = punkte[u], q = punkte[v];
                double dx = q[0] - p[0], dy = q[1] - p[1], l2 = dx * dx + dy * dy;
                if (l2 <= tol * tol) return true;
                for (int k = 0; k < punkte.Count; k++)
                {
                    if (k == u || k == v) continue;
                    double[] m = punkte[k];
                    double t = ((m[0] - p[0]) * dx + (m[1] - p[1]) * dy) / l2;
                    if (t <= 0.0 || t >= 1.0) continue;
                    double lx = p[0] + t * dx - m[0], ly = p[1] + t * dy - m[1];
                    if (lx * lx + ly * ly > tol * tol || Abstand2(m, p) <= tol * tol || Abstand2(m, q) <= tol * tol) continue;
                    teil.Add((t, k));
                }
                teil.Sort((x, y) => x.T != y.T ? x.T.CompareTo(y.T) : x.I.CompareTo(y.I));
                var kette = new List<int> { u };
                kette.AddRange(teil.Select(x => x.I));
                kette.Add(v);
                for (int k = 0; k + 1 < kette.Count; k++)
                {
                    int x = kette[k], y = kette[k + 1];
                    if (x == y) continue;
                    (int, int) s = x < y ? (x, y) : (y, x);
                    int richtung = x < y ? 1 : -1;
                    if (!kanten.TryGetValue(s, out Teilkante e)) { e = new Teilkante(); kanten[s] = e; folge.Add(s); }
                    if (istWand)
                    {
                        if (e.RichtungWand != 0) return false;
                        e.RichtungWand = richtung;
                        e.Kennung = kennung;
                    }
                    else
                    {
                        if (e.RichtungOeffnung != 0) return false;
                        e.RichtungOeffnung = richtung;
                    }
                }
                return true;
            }
            for (int i = 0; i < na; i++)
                if (!Eintragen(ia[i], ia[(i + 1) % na], ra.Kanten[i], true)) { beruehrt = true; return null; }
            for (int j = 0; j < nb; j++)
                if (!Eintragen(ib[j], ib[(j + 1) % nb], kennungB, false)) { beruehrt = true; return null; }

            // Berühren sich die Ringe? Nur dann ist der Graph zusammenhängend.
            var anWand = new HashSet<int>();
            var anOeffnung = new HashSet<int>();
            foreach ((int u, int v) in folge)
            {
                Teilkante e = kanten[(u, v)];
                if (e.RichtungWand != 0) { anWand.Add(u); anWand.Add(v); }
                if (e.RichtungOeffnung != 0) { anOeffnung.Add(u); anOeffnung.Add(v); }
            }
            if (!anWand.Overlaps(anOeffnung)) return null;
            beruehrt = true;

            // 3. Ausgehende Kanten je Punkt, nach Winkel geordnet (Gleichstand: Index).
            var aus = new Dictionary<int, List<int>>();
            void Nachbar(int u, int v)
            {
                if (!aus.TryGetValue(u, out List<int> l)) { l = new List<int>(); aus[u] = l; }
                l.Add(v);
            }
            foreach ((int u, int v) in folge) { Nachbar(u, v); Nachbar(v, u); }
            foreach (KeyValuePair<int, List<int>> kv in aus)
            {
                double[] p = punkte[kv.Key];
                kv.Value.Sort((x, y) =>
                {
                    double wx = Math.Atan2(punkte[x][1] - p[1], punkte[x][0] - p[0]), wy = Math.Atan2(punkte[y][1] - p[1], punkte[y][0] - p[0]);
                    return wx != wy ? wx.CompareTo(wy) : x.CompareTo(y);
                });
            }

            // 4. Flächen umlaufen: nach (u → v) folgt an v die Kante vor (v → u) in Winkelordnung — die Fläche liegt links.
            var besucht = new HashSet<(int, int)>();
            var ergebnis = new List<Kerbring>();
            int grenze = 2 * folge.Count + 2;
            foreach ((int s0, int s1) in folge)
                foreach ((int u0, int v0) in new[] { (s0, s1), (s1, s0) })
                {
                    if (besucht.Contains((u0, v0))) continue;
                    var umlauf = new List<(int U, int V)>();
                    int u = u0, v = v0;
                    while (besucht.Add((u, v)))
                    {
                        umlauf.Add((u, v));
                        if (umlauf.Count > grenze) return null;
                        List<int> l = aus[v];
                        int k = l.IndexOf(u);
                        int w = l[(k - 1 + l.Count) % l.Count];
                        u = v;
                        v = w;
                    }
                    if (u != u0 || v != v0) return null;
                    Kerbring f = Behalten(umlauf, kanten, punkte, herkunft, rb, kennungB, tol);
                    if (f == null) continue;
                    if (f.Punkte.Count < 3) return null;
                    ergebnis.Add(f);
                }
            return ergebnis;
        }

        /// <summary>Die Fläche eines Umlaufs, wenn sie in der Wand und nicht in der Öffnung liegt; sonst <c>null</c>. Ein Umlauf mit doppeltem Punkt ergibt einen leeren Ring.</summary>
        private static Kerbring Behalten(List<(int U, int V)> umlauf, Dictionary<(int, int), Teilkante> kanten, List<double[]> punkte, List<int> herkunft,
                                         List<double[]> rb, string kennungB, double tol)
        {
            bool? inWand = null, inOeffnung = null;
            foreach ((int u, int v) in umlauf)
            {
                Teilkante e = kanten[u < v ? (u, v) : (v, u)];
                int richtung = u < v ? 1 : -1;
                if (inWand == null && e.RichtungWand != 0) inWand = e.RichtungWand == richtung;
                if (inOeffnung == null && e.RichtungOeffnung != 0) inOeffnung = e.RichtungOeffnung == richtung;
            }
            if (inWand != true) return null;
            List<double[]> ring = umlauf.Select(x => punkte[x.U]).ToList();
            if (Flaeche(ring) <= tol * tol) return null;
            inOeffnung ??= ring.All(q => Innen(rb, q) || RandAbstand(rb, q) <= tol);
            if (inOeffnung == true) return null;
            if (umlauf.Select(x => x.U).Distinct().Count() != umlauf.Count) return new Kerbring();

            var r = new Kerbring();
            foreach ((int u, int v) in umlauf)
            {
                Teilkante e = kanten[u < v ? (u, v) : (v, u)];
                r.Punkte.Add(punkte[u]);
                r.Herkunft.Add(herkunft[u]);
                r.Kanten.Add(e.RichtungWand != 0 ? e.Kennung : kennungB);
            }
            // Neue Punkte auf einer Geraden zwischen zwei Kanten derselben Kennung entfallen.
            bool weiter = true;
            while (weiter && r.Punkte.Count > 3)
            {
                weiter = false;
                for (int i = 0; i < r.Punkte.Count; i++)
                {
                    int vor = (i - 1 + r.Punkte.Count) % r.Punkte.Count;
                    if (r.Herkunft[i] >= 0 || r.Kanten[vor] != r.Kanten[i]) continue;
                    double[] a = r.Punkte[vor], m = r.Punkte[i], b = r.Punkte[(i + 1) % r.Punkte.Count];
                    if (RandAbstand(new[] { a, b }, m) > tol / 2.0) continue;
                    if ((m[0] - a[0]) * (b[0] - m[0]) + (m[1] - a[1]) * (b[1] - m[1]) <= 0.0) continue;
                    r.Punkte.RemoveAt(i);
                    r.Herkunft.RemoveAt(i);
                    r.Kanten.RemoveAt(i);
                    weiter = true;
                    break;
                }
            }
            return r;
        }

        private sealed class Teilkante
        {
            /// <summary>+1: die Wand läuft vom kleineren zum größeren Index, −1 umgekehrt, 0: keine Wandkante.</summary>
            internal int RichtungWand;

            /// <summary>Wie <see cref="RichtungWand"/> für die Öffnung.</summary>
            internal int RichtungOeffnung;

            /// <summary>Die Kennung der Wandkante.</summary>
            internal string Kennung;
        }

        // ==================================================================
        //  Hilfen in 2D
        // ==================================================================

        /// <summary>Der Ring gegen den Uhrzeigersinn; die Kantenkennungen wandern mit.</summary>
        private static Kerbring Gegenuhr(Kerbring a)
        {
            if (Flaeche(a.Punkte) >= 0.0) return a;
            int n = a.Punkte.Count;
            var r = new Kerbring();
            for (int j = 0; j < n; j++)
            {
                r.Punkte.Add(a.Punkte[n - 1 - j]);
                r.Herkunft.Add(a.Herkunft[n - 1 - j]);
                // neue Kante j: Punkt n−1−j → n−2−j = alte Kante n−2−j, gegenläufig
                r.Kanten.Add(a.Kanten[(n - 2 - j + n) % n]);
            }
            return r;
        }

        /// <summary>Doppelte Folgepunkte (Abstand ≤ Toleranz) entfernt.</summary>
        internal static List<double[]> Bereinigt(IReadOnlyList<double[]> ring, double tol)
        {
            var r = new List<double[]>();
            foreach (double[] p in ring ?? Array.Empty<double[]>())
                if (p != null && (r.Count == 0 || Abstand2(r[r.Count - 1], p) > tol * tol)) r.Add(new[] { p[0], p[1] });
            while (r.Count > 1 && Abstand2(r[0], r[r.Count - 1]) <= tol * tol) r.RemoveAt(r.Count - 1);
            return r;
        }

        /// <summary>Die vorzeichenbehaftete Fläche (positiv gegen den Uhrzeigersinn).</summary>
        internal static double Flaeche(IReadOnlyList<double[]> p)
        {
            double s = 0.0;
            for (int i = 0; i < p.Count; i++)
            {
                double[] a = p[i], b = p[(i + 1) % p.Count];
                s += a[0] * b[1] - b[0] * a[1];
            }
            return s / 2.0;
        }

        /// <summary>Punkt im Ring (gerade-ungerade-Regel).</summary>
        internal static bool Innen(IReadOnlyList<double[]> ring, double[] x)
        {
            bool innen = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            {
                double[] a = ring[i], b = ring[j];
                if ((a[1] > x[1]) != (b[1] > x[1]) && x[0] < (b[0] - a[0]) * (x[1] - a[1]) / (b[1] - a[1]) + a[0]) innen = !innen;
            }
            return innen;
        }

        /// <summary>Der kleinste Abstand des Punkts zu den Kanten des Rings (bei zwei Punkten: zur Strecke).</summary>
        internal static double RandAbstand(IReadOnlyList<double[]> ring, double[] x)
        {
            double best = double.MaxValue;
            int kanten = ring.Count == 2 ? 1 : ring.Count;
            for (int i = 0; i < kanten; i++)
            {
                double[] a = ring[i], b = ring[(i + 1) % ring.Count];
                double dx = b[0] - a[0], dy = b[1] - a[1], l2 = dx * dx + dy * dy;
                double t = l2 < 1e-18 ? 0.0 : Math.Clamp(((x[0] - a[0]) * dx + (x[1] - a[1]) * dy) / l2, 0.0, 1.0);
                double px = a[0] + t * dx - x[0], py = a[1] + t * dy - x[1];
                best = Math.Min(best, Math.Sqrt(px * px + py * py));
            }
            return best;
        }

        /// <summary>Der Schnittpunkt zweier nicht paralleler Strecken, Endpunkte eingeschlossen; sonst <c>null</c>.</summary>
        private static double[] Schnitt(double[] p, double[] p2, double[] q, double[] q2)
        {
            double rx = p2[0] - p[0], ry = p2[1] - p[1], sx = q2[0] - q[0], sy = q2[1] - q[1];
            double d = rx * sy - ry * sx;
            double skala = Math.Sqrt((rx * rx + ry * ry) * (sx * sx + sy * sy));
            if (skala <= 0.0 || Math.Abs(d) <= 1e-12 * skala) return null;
            double wx = q[0] - p[0], wy = q[1] - p[1];
            double t = (wx * sy - wy * sx) / d, u = (wx * ry - wy * rx) / d;
            const double RAND = 1e-12;
            if (t < -RAND || t > 1.0 + RAND || u < -RAND || u > 1.0 + RAND) return null;
            return new[] { p[0] + t * rx, p[1] + t * ry };
        }

        private static double Abstand2(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1];
            return dx * dx + dy * dy;
        }
    }
}
