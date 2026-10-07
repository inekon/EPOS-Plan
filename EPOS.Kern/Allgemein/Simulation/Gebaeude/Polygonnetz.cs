using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Dreieckszerlegung ebener Vielecke, formatfrei</b> (Datenaustauschkonzept 15.2, 17.2 Nr. 3): der Ohrenschnitt mit
    /// Brückenkanten für nichtkonvexe Vielecke mit Löchern, wie ihn der IFC-Körper (<see cref="IfcRaumkoerper"/>) und der
    /// formatfreie <c>Koerperbildner</c> gemeinsam nutzen, dazu die Zerlegung eines Vielecks im Raum über seine
    /// Ebene. Feste Startecke, keine Zufallswahl: dieselbe Eingabe ergibt dieselben Dreiecke.
    /// </summary>
    internal static class Polygonnetz
    {
        // ==================================================================
        //  Ohrenschnitt mit Brückenkanten (2D)
        // ==================================================================

        /// <summary>
        /// Die Dreiecke eines ebenen Vielecks mit Löchern (Indextripel in <paramref name="p"/>, gegen den Uhrzeigersinn).
        /// <paramref name="ringe"/>[0] ist der Außenring (gegen den Uhrzeigersinn), die übrigen sind Löcher (im
        /// Uhrzeigersinn). Jedes Loch wird über eine Brückenkante von seinem rechtesten Punkt zum nächsten sichtbaren
        /// Punkt angebunden; gelingt das nicht, steht seine Stelle in <paramref name="verloren"/> und es entfällt.
        /// Ohrenschnitt mit fester Startecke (Stelle 0), deterministisch.
        /// </summary>
        internal static List<int[]> Dreiecke2D(List<double[]> p, List<List<int>> ringe, out List<int> verloren)
        {
            verloren = new List<int>();
            var poly = new List<int>(ringe[0]);
            var loecher = Enumerable.Range(1, ringe.Count - 1)
                .Select(r => (Stelle: r, Rechts: ringe[r].Max(i => p[i][0])))
                .OrderByDescending(x => x.Rechts).ThenBy(x => x.Stelle).ToList();
            var offen = loecher.Select(x => x.Stelle).ToList();
            foreach ((int stelle, double _) in loecher)
            {
                offen.Remove(stelle);
                if (!Bruecke(p, poly, ringe, stelle, offen)) verloren.Add(stelle);
            }
            return Ohrenschnitt(p, poly);
        }

        private static bool Bruecke(List<double[]> p, List<int> poly, List<List<int>> ringe, int stelle, List<int> offen)
        {
            List<int> loch = ringe[stelle];
            int mStelle = 0;
            for (int i = 1; i < loch.Count; i++)
                if (p[loch[i]][0] > p[loch[mStelle]][0]) mStelle = i;
            double[] m = p[loch[mStelle]];
            // Kandidaten: die Punkte des bisherigen Vielecks nach Abstand, bei Gleichstand nach Stelle.
            List<int> kandidaten = Enumerable.Range(0, poly.Count)
                .OrderBy(i => Abstand2(p[poly[i]], m)).ThenBy(i => i).ToList();
            foreach (int k in kandidaten)
            {
                double[] q = p[poly[k]];
                if (Abstand2(q, m) < 1e-24) continue;
                if (!ImKeil(p, poly, k, m)) continue;
                if (!Sichtbar(p, poly, ringe, loch, offen, m, q)) continue;
                // Einfügen: … Q, M, Loch ab M …, M, Q, …
                var einschub = new List<int>(loch.Count + 2);
                for (int i = 0; i <= loch.Count; i++) einschub.Add(loch[(mStelle + i) % loch.Count]);
                einschub.Add(poly[k]);
                poly.InsertRange(k + 1, einschub);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Liegt die Richtung zu <paramref name="m"/> im Innenwinkel des Vielecks (gegen den Uhrzeigersinn) an der Stelle
        /// <paramref name="k"/>? Ein Punkt, an dem schon eine Brücke hängt, steht mehrfach im Vieleck; nur an der Stelle,
        /// deren Innenwinkel die neue Brücke aufnimmt, bleibt das Vieleck einfach. Die Ränder des Winkels zählen dazu.
        /// </summary>
        private static bool ImKeil(List<double[]> p, List<int> poly, int k, double[] m)
        {
            double[] a = p[poly[(k + poly.Count - 1) % poly.Count]], q = p[poly[k]], b = p[poly[(k + 1) % poly.Count]];
            bool linksVonA = Kreuz2(a, q, m) >= 0.0, linksVonB = Kreuz2(q, b, m) >= 0.0;
            return Kreuz2(a, q, b) >= 0.0 ? linksVonA && linksVonB : linksVonA || linksVonB;
        }

        private static bool Sichtbar(List<double[]> p, List<int> poly, List<List<int>> ringe, List<int> loch, List<int> offen, double[] m, double[] q)
        {
            if (Schneidet(p, poly, m, q)) return false;
            if (Schneidet(p, loch, m, q)) return false;
            foreach (int o in offen) if (Schneidet(p, ringe[o], m, q)) return false;
            double[] mitte = { (m[0] + q[0]) / 2.0, (m[1] + q[1]) / 2.0 };
            if (!Innen(p, ringe[0], mitte)) return false;
            if (Innen(p, loch, mitte)) return false;
            foreach (int o in offen) if (Innen(p, ringe[o], mitte)) return false;
            return true;
        }

        /// <summary>Kreuzt die Strecke a–b eine Kante des Rings echt (gemeinsame Endpunkte zählen nicht)?</summary>
        private static bool Schneidet(List<double[]> p, List<int> ring, double[] a, double[] b)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                double[] c = p[ring[i]], d = p[ring[(i + 1) % ring.Count]];
                if (Gleich2(c, a) || Gleich2(c, b) || Gleich2(d, a) || Gleich2(d, b)) continue;
                double d1 = Kreuz2(a, b, c), d2 = Kreuz2(a, b, d), d3 = Kreuz2(c, d, a), d4 = Kreuz2(c, d, b);
                if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return true;
                if (Math.Abs(d1) < 1e-18 && Auf(a, b, c)) return true;
                if (Math.Abs(d2) < 1e-18 && Auf(a, b, d)) return true;
            }
            return false;
        }

        private static bool Auf(double[] a, double[] b, double[] c)
            => Math.Min(a[0], b[0]) <= c[0] && c[0] <= Math.Max(a[0], b[0]) && Math.Min(a[1], b[1]) <= c[1] && c[1] <= Math.Max(a[1], b[1]);

        private static bool Innen(List<double[]> p, List<int> ring, double[] x)
        {
            bool innen = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            {
                double[] a = p[ring[i]], b = p[ring[j]];
                if ((a[1] > x[1]) != (b[1] > x[1]) && x[0] < (b[0] - a[0]) * (x[1] - a[1]) / (b[1] - a[1]) + a[0]) innen = !innen;
            }
            return innen;
        }

        private static double Abstand2(double[] a, double[] b) => (a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]);

        private static bool Gleich2(double[] a, double[] b) => Math.Abs(a[0] - b[0]) <= 1e-12 && Math.Abs(a[1] - b[1]) <= 1e-12;

        private static double Kreuz2(double[] a, double[] b, double[] c) => (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);

        /// <summary>
        /// Der Ohrenschnitt eines einfachen Vielecks gegen den Uhrzeigersinn (Brückenpunkte dürfen doppelt stehen).
        /// Kollineare Ecken fallen ohne Dreieck heraus; findet sich kein Ohr mehr (verdrehte Fläche), wird der Rest als
        /// Fächer von seiner ersten Ecke geschlossen — keine Reparatur, nur kein Abbruch.
        /// </summary>
        internal static List<int[]> Ohrenschnitt(List<double[]> p, List<int> vieleck)
        {
            var v = new List<int>(vieleck);
            var ergebnis = new List<int[]>(Math.Max(0, v.Count - 2));
            double groesse = 0.0;
            foreach (int i in v) groesse = Math.Max(groesse, Math.Max(Math.Abs(p[i][0]), Math.Abs(p[i][1])));
            double eps = 1e-12 * Math.Max(1.0, groesse * groesse);
            int s = 0, versuche = 0;
            while (v.Count > 3)
            {
                int a = v[(s + v.Count - 1) % v.Count], b = v[s], c = v[(s + 1) % v.Count];
                double k = Kreuz2(p[a], p[b], p[c]);
                if (Math.Abs(k) <= eps && !Gleich2(p[a], p[c]))
                {
                    v.RemoveAt(s);   // kollinear: die Ecke fällt ohne Dreieck heraus
                    if (s >= v.Count) s = 0;
                    versuche = 0;
                    continue;
                }
                if (k > eps && Ohr(p, v, a, b, c))
                {
                    ergebnis.Add(new[] { a, b, c });
                    v.RemoveAt(s);
                    if (s >= v.Count) s = 0;
                    versuche = 0;
                    continue;
                }
                s = (s + 1) % v.Count;
                if (++versuche > v.Count)
                {
                    for (int i = 1; i + 1 < v.Count; i++) ergebnis.Add(new[] { v[0], v[i], v[i + 1] });
                    return ergebnis;
                }
            }
            if (v.Count == 3 && Math.Abs(Kreuz2(p[v[0]], p[v[1]], p[v[2]])) > eps) ergebnis.Add(new[] { v[0], v[1], v[2] });
            return ergebnis;
        }

        private static bool Ohr(List<double[]> p, List<int> v, int a, int b, int c)
        {
            double[] pa = p[a], pb = p[b], pc = p[c];
            foreach (int i in v)
            {
                double[] x = p[i];
                if (Gleich2(x, pa) || Gleich2(x, pb) || Gleich2(x, pc)) continue;
                if (Kreuz2(pa, pb, x) >= 0 && Kreuz2(pb, pc, x) >= 0 && Kreuz2(pc, pa, x) >= 0) return false;
            }
            return true;
        }

        // ==================================================================
        //  Vieleck im Raum
        // ==================================================================

        /// <summary>
        /// Die Dreiecke eines Vielecks im Raum mit Löchern, als Indextripel in die verkettete Punktliste (erst
        /// <paramref name="aussen"/>, dann die Löcher in ihrer Reihenfolge). Eben (alle Punkte höchstens
        /// <paramref name="ebenToleranz"/> von der Ebene des Außenrings): Ohrenschnitt in der Ebene, Umlauf der Dreiecke wie
        /// der Außenring (Normale nach Newell); nicht eben: Fächer vom ersten Punkt des Außenrings, die Löcher entfallen
        /// (<paramref name="eben"/> = <c>false</c>, alle Lochstellen in <paramref name="verloren"/>). Die Lochstellen in
        /// <paramref name="verloren"/> zählen ab 1 wie in <see cref="Dreiecke2D"/>. Die Ringe kommen bereinigt
        /// (<see cref="Bereinigt"/>); der Umlaufsinn der Löcher ist beliebig.
        /// </summary>
        internal static List<int[]> Flaeche3D(IReadOnlyList<double[]> aussen, IReadOnlyList<IReadOnlyList<double[]>> loecher,
                                              double ebenToleranz, out bool eben, out List<int> verloren)
        {
            verloren = new List<int>();
            eben = true;
            var ergebnis = new List<int[]>();
            if (aussen == null || aussen.Count < 3) return ergebnis;
            loecher ??= Array.Empty<IReadOnlyList<double[]>>();
            double[] n = Normiert(Newell(aussen));
            if (n == null) return ergebnis;
            double[] p0 = aussen[0];
            bool Abseits(double[] p) => Math.Abs(Punkt(Minus(p, p0), n)) > ebenToleranz;
            if (aussen.Any(Abseits) || loecher.Any(l => l.Any(Abseits))) eben = false;
            if (!eben)
            {
                for (int i = 1; i + 1 < aussen.Count; i++) ergebnis.Add(new[] { 0, i, i + 1 });
                for (int l = 0; l < loecher.Count; l++) verloren.Add(l + 1);
                return ergebnis;
            }

            // In der Ebene: u längs der ersten nicht verschwindenden Kante, v = n × u — der Außenring läuft dort gegen den
            // Uhrzeigersinn, weil n seine Newell-Normale ist.
            double[] u = null;
            for (int i = 1; i < aussen.Count && u == null; i++)
            {
                double[] d = Minus(aussen[i], p0);
                u = Normiert(Minus(d, Mal(n, Punkt(d, n))));
            }
            if (u == null) return ergebnis;
            double[] v = Kreuz(n, u);
            var eben2 = new List<double[]>();
            var ringe = new List<List<int>>();
            void Ring(IReadOnlyList<double[]> r, bool gegenUhrzeiger)
            {
                var stellen = new List<int>(r.Count);
                var punkte = new List<double[]>(r.Count);
                foreach (double[] p in r)
                {
                    double[] d = Minus(p, p0);
                    punkte.Add(new[] { Punkt(d, u), Punkt(d, v) });
                }
                foreach (double[] q in punkte) { stellen.Add(eben2.Count); eben2.Add(q); }
                if (Flaeche2D(punkte) > 0.0 != gegenUhrzeiger) stellen.Reverse();
                ringe.Add(stellen);
            }
            Ring(aussen, true);
            foreach (IReadOnlyList<double[]> l in loecher) Ring(l, false);
            return Dreiecke2D(eben2, ringe, out verloren);
        }

        /// <summary>Ein Ring ohne Schlusspunkt und ohne doppelte Folgepunkte (Abstand je Achse höchstens 1e-9).</summary>
        internal static List<double[]> Bereinigt(IEnumerable<double[]> ring)
        {
            var r = new List<double[]>();
            foreach (double[] p in ring)
                if (p != null && (r.Count == 0 || !Gleich3(r[r.Count - 1], p))) r.Add(p);
            while (r.Count > 1 && Gleich3(r[0], r[r.Count - 1])) r.RemoveAt(r.Count - 1);
            return r;
        }

        /// <summary>Die Normale eines Vielecks nach Newell (Länge = doppelte Fläche).</summary>
        internal static double[] Newell(IReadOnlyList<double[]> p)
        {
            var s = new double[3];
            for (int i = 0; i < p.Count; i++)
            {
                double[] a = p[i], b = p[(i + 1) % p.Count];
                s[0] += (a[1] - b[1]) * (a[2] + b[2]);
                s[1] += (a[2] - b[2]) * (a[0] + b[0]);
                s[2] += (a[0] - b[0]) * (a[1] + b[1]);
            }
            return s;
        }

        /// <summary>Die vorzeichenbehaftete Fläche eines Vielecks in der Ebene (positiv gegen den Uhrzeigersinn).</summary>
        internal static double Flaeche2D(IReadOnlyList<double[]> p)
        {
            double f = 0.0;
            for (int i = 0; i < p.Count; i++)
            {
                double[] a = p[i], b = p[(i + 1) % p.Count];
                f += a[0] * b[1] - b[0] * a[1];
            }
            return f / 2.0;
        }

        internal static bool Gleich3(double[] a, double[] b)
            => Math.Abs(a[0] - b[0]) <= 1e-9 && Math.Abs(a[1] - b[1]) <= 1e-9 && Math.Abs(a[2] - b[2]) <= 1e-9;

        internal static double[] Minus(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };

        internal static double[] Plus(double[] a, double[] b) => new[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] };

        internal static double[] Mal(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };

        internal static double Punkt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        internal static double[] Kreuz(double[] a, double[] b)
            => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        /// <summary>Der Einheitsvektor; <c>null</c> bei verschwindender Länge (unter 1e-12).</summary>
        internal static double[] Normiert(double[] a)
        {
            double l = Math.Sqrt(Punkt(a, a));
            return l < 1e-12 ? null : new[] { a[0] / l, a[1] / l, a[2] / l };
        }
    }
}
