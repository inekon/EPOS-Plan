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

    /// <summary>Ein Teil der Wand nach dem Schnitt: Außenring gegen den Uhrzeigersinn und die Löcher darin.</summary>
    internal sealed class Kerbstueck
    {
        internal Kerbring Aussen { get; init; }

        internal List<Kerbring> Loecher { get; init; } = new List<Kerbring>();
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
    /// <b>Die Randkerbe</b> (Datenaustauschkonzept 17.3 Nr. 4): die Differenz Wandring minus Öffnungsringe in der Ebene der
    /// Wand, formatfrei und deterministisch. Alle Ringe werden an ihren Schnitt- und Berührpunkten geteilt (Punkte auf die
    /// Toleranz zusammengelegt, der kleinste Index gewinnt); die Teilkanten bilden einen ebenen Graphen, dessen Flächen
    /// links ihrer Kanten umlaufen werden. Behalten wird eine Fläche, die in der Wand und in keiner Öffnung liegt — über
    /// die Richtung der Ringkanten an ihrem Rand, sonst über einen Punkt dicht links ihrer längsten Kante. Eine Gruppe von
    /// Öffnungen ohne Verbindung zum Wandrand wird zum Loch der Fläche, die sie umgibt. Jede Ergebniskante trägt die
    /// Kennung ihrer Herkunft: auf dem Wandrand die der Wand, im Inneren der Wand die der Öffnung — dort entsteht die
    /// Laibung. Ohne Hash-Reihenfolge, ohne Zufall, ohne Plattformzugriff.
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
        /// Der Wandring <paramref name="wand"/> minus die Öffnungsringe <paramref name="oeffnungen"/> (Ring und Kennung).
        /// Ergebnis: die Teile der Wand samt Löchern (leer, wenn die Öffnungen die Wand ganz decken); <c>null</c>, wenn der
        /// Schnitt kein einfaches Vieleck ergibt (etwa eine Berührung in einem Punkt) oder ein Ring sich selbst überdeckt.
        /// </summary>
        internal static List<Kerbstueck> Differenz(Kerbring wand, IReadOnlyList<(IReadOnlyList<double[]> Ring, string Kennung)> oeffnungen, double tol)
        {
            // Ring 0 ist die Wand, die übrigen die Öffnungen; alle gegen den Uhrzeigersinn.
            Kerbring w = Gegenuhr(wand);
            if (w.Punkte.Count < 3) return null;
            var ringe = new List<List<double[]>> { w.Punkte };
            var kennungen = new List<string> { null };
            foreach ((IReadOnlyList<double[]> ring, string kennung) in oeffnungen)
            {
                List<double[]> r = Bereinigt(ring, tol);
                if (r.Count < 3) return null;
                if (Flaeche(r) < 0.0) r.Reverse();
                ringe.Add(r);
                kennungen.Add(kennung ?? "");
            }
            int zahl = ringe.Count;

            // 1. Punkte: Ringecken, dann Schnittpunkte je Paar verschiedener Ringe — zusammengelegt auf die Toleranz.
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
            var ecken = new List<int[]>();
            for (int r = 0; r < zahl; r++)
                ecken.Add(ringe[r].Select((p, i) => Stelle(p, r == 0 ? w.Herkunft[i] : -1)).ToArray());
            for (int r = 0; r < zahl; r++)
                for (int s = r + 1; s < zahl; s++)
                    for (int i = 0; i < ringe[r].Count; i++)
                        for (int j = 0; j < ringe[s].Count; j++)
                        {
                            double[] x = Schnitt(ringe[r][i], ringe[r][(i + 1) % ringe[r].Count], ringe[s][j], ringe[s][(j + 1) % ringe[s].Count]);
                            if (x != null) Stelle(x, -1);
                        }

            // 2. Teilkanten: jede Ringkante an den Punkten in ihrem Inneren geteilt; ungerichtet einmal, Richtung je Ring.
            var kanten = new Dictionary<(int, int), Teilkante>();
            var folge = new List<(int, int)>();
            for (int r = 0; r < zahl; r++)
            {
                int n = ecken[r].Length;
                for (int i = 0; i < n; i++)
                {
                    int u = ecken[r][i], v = ecken[r][(i + 1) % n];
                    if (u == v) continue;
                    var kette = new List<int> { u };
                    kette.AddRange(Teilpunkte(punkte, u, v, tol));
                    kette.Add(v);
                    for (int k = 0; k + 1 < kette.Count; k++)
                    {
                        int x = kette[k], y = kette[k + 1];
                        if (x == y) continue;
                        (int, int) s = x < y ? (x, y) : (y, x);
                        if (!kanten.TryGetValue(s, out Teilkante e)) { e = new Teilkante(zahl); kanten[s] = e; folge.Add(s); }
                        if (e.Richtung[r] != 0) return null;   // Ring überdeckt sich selbst
                        e.Richtung[r] = x < y ? 1 : -1;
                        if (r == 0) e.Kennung = w.Kanten[i];
                    }
                }
            }
            if (folge.Count == 0) return null;

            // 3. Zusammenhang (kleinster Index als Vertreter) und ausgehende Kanten je Punkt nach Winkel.
            var vertreter = Enumerable.Range(0, punkte.Count).ToArray();
            int Wurzel(int i) { while (vertreter[i] != i) i = vertreter[i] = vertreter[vertreter[i]]; return i; }
            var aus = new SortedDictionary<int, List<int>>();
            foreach ((int u, int v) in folge)
            {
                int a = Wurzel(u), b = Wurzel(v);
                if (a != b) vertreter[Math.Max(a, b)] = Math.Min(a, b);
                if (!aus.TryGetValue(u, out List<int> lu)) aus[u] = lu = new List<int>();
                if (!aus.TryGetValue(v, out List<int> lv)) aus[v] = lv = new List<int>();
                lu.Add(v);
                lv.Add(u);
            }
            foreach (KeyValuePair<int, List<int>> kv in aus)
            {
                double[] p = punkte[kv.Key];
                kv.Value.Sort((x, y) =>
                {
                    double wx = Math.Atan2(punkte[x][1] - p[1], punkte[x][0] - p[0]), wy = Math.Atan2(punkte[y][1] - p[1], punkte[y][0] - p[0]);
                    return wx != wy ? wx.CompareTo(wy) : x.CompareTo(y);
                });
            }
            int wandteil = Wurzel(ecken[0][0]);

            // 4. Umläufe: nach (u → v) folgt an v die Kante vor (v → u) in Winkelordnung — die Fläche liegt links.
            var besucht = new HashSet<(int, int)>();
            var flaechen = new List<Umlauf>();
            var raender = new List<Umlauf>();
            int grenze = 2 * folge.Count + 2;
            foreach ((int s0, int s1) in folge)
                foreach ((int u0, int v0) in new[] { (s0, s1), (s1, s0) })
                {
                    if (besucht.Contains((u0, v0))) continue;
                    var um = new Umlauf();
                    int u = u0, v = v0;
                    while (besucht.Add((u, v)))
                    {
                        um.Kanten.Add((u, v));
                        if (um.Kanten.Count > grenze) return null;
                        List<int> l = aus[v];
                        int k = l.IndexOf(u);
                        int nx = l[(k - 1 + l.Count) % l.Count];
                        u = v;
                        v = nx;
                    }
                    if (u != u0 || v != v0) return null;
                    um.Ring = um.Kanten.Select(x => punkte[x.U]).ToList();
                    um.Flaeche = Flaeche(um.Ring);
                    um.Teil = Wurzel(u0);
                    if (um.Flaeche > 0.0) flaechen.Add(um);
                    else raender.Add(um);
                }

            // 5. Flächen behalten: in der Wand, in keiner Öffnung, nicht verschwindend.
            foreach (Umlauf f in flaechen)
            {
                if (f.Flaeche <= tol * tol) continue;
                double[] probe = null;
                bool Innerhalb(int r)
                {
                    foreach ((int u, int v) in f.Kanten)
                    {
                        int d = kanten[u < v ? (u, v) : (v, u)].Richtung[r];
                        if (d != 0) return d == (u < v ? 1 : -1);
                    }
                    probe ??= Probepunkt(f, punkte, tol);
                    return Innen(ringe[r], probe);
                }
                f.Behalten = Innerhalb(0) && !Enumerable.Range(1, zahl - 1).Any(Innerhalb);
            }

            // 6. Die Ränder der Teile ohne Verbindung zur Wand werden Löcher der kleinsten umgebenden Fläche.
            var stuecke = new Dictionary<Umlauf, Kerbstueck>(ReferenceEqualityComparer.Instance);
            var ergebnis = new List<Kerbstueck>();
            foreach (Umlauf f in flaechen.Where(x => x.Behalten))
            {
                Kerbring r = Ring(f, kanten, punkte, herkunft, kennungen, tol);
                if (r == null) return null;
                var st = new Kerbstueck { Aussen = r };
                stuecke[f] = st;
                ergebnis.Add(st);
            }
            foreach (Umlauf rand in raender)
            {
                if (rand.Teil == wandteil) continue;
                double[] q = rand.Ring[0];
                Umlauf um = flaechen.Where(f => f.Teil != rand.Teil && Innen(f.Ring, q)).OrderBy(f => f.Flaeche).FirstOrDefault();
                if (um == null || !um.Behalten) continue;
                Kerbring loch = Ring(rand, kanten, punkte, herkunft, kennungen, tol);
                if (loch == null) return null;
                stuecke[um].Loecher.Add(loch);
            }
            return ergebnis;
        }

        /// <summary>Die Punkte im Inneren der Strecke u → v (Abstand ≤ Toleranz), nach ihrer Lage auf der Strecke.</summary>
        private static IEnumerable<int> Teilpunkte(List<double[]> punkte, int u, int v, double tol)
        {
            var teil = new List<(double T, int I)>();
            double[] p = punkte[u], q = punkte[v];
            double dx = q[0] - p[0], dy = q[1] - p[1], l2 = dx * dx + dy * dy;
            if (l2 <= tol * tol) return teil.Select(x => x.I);
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
            return teil.Select(x => x.I);
        }

        /// <summary>Ein Punkt dicht links der Mitte der längsten Kante eines Umlaufs — im Inneren der Fläche.</summary>
        private static double[] Probepunkt(Umlauf f, List<double[]> punkte, double tol)
        {
            (int U, int V) beste = f.Kanten[0];
            double laenge = -1.0;
            foreach ((int u, int v) in f.Kanten)
            {
                double l = Abstand2(punkte[u], punkte[v]);
                if (l > laenge) { laenge = l; beste = (u, v); }
            }
            double[] a = punkte[beste.U], b = punkte[beste.V];
            double l1 = Math.Sqrt(laenge), d = tol / 20.0;
            return new[] { (a[0] + b[0]) / 2.0 - (b[1] - a[1]) / l1 * d, (a[1] + b[1]) / 2.0 + (b[0] - a[0]) / l1 * d };
        }

        /// <summary>Der Ring eines Umlaufs mit Kennung je Kante; neue Punkte auf einer Geraden zwischen gleich benannten Kanten entfallen. <c>null</c> bei doppeltem Punkt.</summary>
        private static Kerbring Ring(Umlauf um, Dictionary<(int, int), Teilkante> kanten, List<double[]> punkte, List<int> herkunft, List<string> kennungen, double tol)
        {
            if (um.Kanten.Select(x => x.U).Distinct().Count() != um.Kanten.Count) return null;
            var r = new Kerbring();
            foreach ((int u, int v) in um.Kanten)
            {
                Teilkante e = kanten[u < v ? (u, v) : (v, u)];
                r.Punkte.Add(punkte[u]);
                r.Herkunft.Add(herkunft[u]);
                int ring = e.Richtung[0] != 0 ? 0 : Array.FindIndex(e.Richtung, d => d != 0);
                r.Kanten.Add(ring == 0 ? e.Kennung : kennungen[ring]);
            }
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
            internal Teilkante(int ringe) => Richtung = new int[ringe];

            /// <summary>Je Ring: +1 läuft vom kleineren zum größeren Index, −1 umgekehrt, 0 gehört nicht zum Ring.</summary>
            internal int[] Richtung { get; }

            /// <summary>Die Kennung der Wandkante.</summary>
            internal string Kennung;
        }

        private sealed class Umlauf
        {
            internal List<(int U, int V)> Kanten = new List<(int, int)>();
            internal List<double[]> Ring;
            internal double Flaeche;
            internal int Teil;
            internal bool Behalten;
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

        /// <summary>Der Umfang des Rings.</summary>
        internal static double Umfang(IReadOnlyList<double[]> p)
        {
            double s = 0.0;
            for (int i = 0; i < p.Count; i++) s += Math.Sqrt(Abstand2(p[i], p[(i + 1) % p.Count]));
            return s;
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
