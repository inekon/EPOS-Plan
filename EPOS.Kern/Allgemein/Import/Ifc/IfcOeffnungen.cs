using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Öffnungen eines Hüllbauteils aus dem Körper</b> (Abstimmung G5, Teil G5-2): Fläche einer Öffnung in der Ebene
    /// ihres Wirts und ihre Tiefe quer dazu, ohne Geometriekern. Grundlage ist der Körper aus <see cref="IfcRaumkoerper"/>
    /// (Dreiecke in Weltkoordinaten, Meter).
    ///
    /// <para><b>Profilfläche:</b> die Projektion des Körpers auf die Ebene des Wirts, ½ Σ |A·n| über alle Dreiecke — für
    /// eine Extrusion quer durch die Wand genau die Fläche des Profils, für einen flachen Fenster- oder Türkörper seine
    /// Ansichtsfläche. <b>Tiefe:</b> die Ausdehnung des Körpers längs der Normalen des Wirts; eine Öffnung, die weniger tief
    /// ist als der Wirt dick, durchdringt ihn nicht (Nische).</para>
    /// </summary>
    internal static class IfcOeffnungen
    {
        /// <summary>Die Toleranz der Tiefe gegen die Dicke des Wirts [m]: darunter gilt eine Öffnung als durchgehend.</summary>
        internal const double TIEFE_TOLERANZ_M = 0.001;

        /// <summary>Die Projektion des geschlossenen Körpers auf die Ebene mit der Normalen <paramref name="n"/> [m²]; <c>null</c> ohne Körper.</summary>
        internal static double? Profilflaeche(Dateikoerper k, double[] n)
        {
            if (k == null || n == null || k.Dreiecke.Count == 0) return null;
            double summe = 0.0;
            foreach (int[] d in k.Dreiecke)
            {
                double[] a = Flaechenvektor(k, d);
                summe += Math.Abs(a[0] * n[0] + a[1] * n[1] + a[2] * n[2]);
            }
            return summe / 4.0;   // je Dreieck die halbe Kreuzproduktlänge, Vorder- und Rückseite zusammen doppelt
        }

        /// <summary>Die Ausdehnung des Körpers längs der Normalen <paramref name="n"/> [m]; <c>null</c> ohne Körper.</summary>
        internal static double? Tiefe(Dateikoerper k, double[] n)
        {
            if (k == null || n == null || k.PunkteM.Count == 0) return null;
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            foreach (double[] p in k.PunkteM)
            {
                double s = p[0] * n[0] + p[1] * n[1] + p[2] * n[2];
                min = Math.Min(min, s);
                max = Math.Max(max, s);
            }
            return max - min;
        }

        /// <summary>
        /// Die Richtung der größten ebenen Fläche des Körpers (Einheitsvektor, Vorzeichen gleichgültig): ohne Außenseite des
        /// Wirts die Ebene einer Wand oder Platte bzw. das Profil einer flachen Öffnung. Richtungen unter 5° gelten als eine.
        /// </summary>
        internal static double[] Hauptnormale(Dateikoerper k)
        {
            if (k == null || k.Dreiecke.Count == 0) return null;
            double grenze = Math.Cos(IfcBauteilkoerper.ZUSAMMENFASSEN_GRAD * Math.PI / 180.0);
            var gruppen = new List<(double[] N, double Flaeche)>();
            foreach (int[] d in k.Dreiecke)
            {
                double[] a = Flaechenvektor(k, d);
                double betrag = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]);
                if (betrag <= 0.0) continue;
                double[] e = { a[0] / betrag, a[1] / betrag, a[2] / betrag };
                int i = gruppen.FindIndex(g => Math.Abs(g.N[0] * e[0] + g.N[1] * e[1] + g.N[2] * e[2]) >= grenze);
                if (i < 0) gruppen.Add((e, betrag / 2.0));
                else gruppen[i] = (gruppen[i].N, gruppen[i].Flaeche + betrag / 2.0);
            }
            return gruppen.Count == 0 ? null : gruppen.OrderByDescending(g => g.Flaeche).First().N;
        }

        /// <summary>Die Normale der größten Teilfläche eines ausgewerteten Bauteilkörpers; <c>null</c> ohne Auswertung.</summary>
        internal static double[] Hauptnormale(Bauteilkoerperflaeche kf)
            => kf == null || kf.Teile.Count == 0 ? null : kf.Teile.OrderByDescending(t => t.FlaecheM2).First().Normale;

        /// <summary>Ist die Öffnung eine Nische — weniger tief als der Wirt dick (beide bekannt)?</summary>
        internal static bool Nische(double? tiefeM, double? dickeM)
            => tiefeM is double t && dickeM is double d && t > 0.0 && d > 0.0 && t < d - TIEFE_TOLERANZ_M;

        /// <summary>Die Nettofläche des Wirts: Brutto minus Abzug, nie negativ (<paramref name="nichtPositiv"/> = ≤ 0).</summary>
        internal static double Netto(double bruttoM2, double abzugM2, out bool nichtPositiv)
        {
            double netto = bruttoM2 - abzugM2;
            nichtPositiv = netto <= 0.0;
            return Math.Max(0.0, netto);
        }

        /// <summary>
        /// Die Toleranz der Aussparung [m]: Liegt die Mitte einer Öffnung näher als diese an einem Dreieck der Wirtsebene,
        /// gilt sie als bedeckt (nicht ausgespart).
        /// </summary>
        internal const double AUSSPARUNG_TOLERANZ_M = 0.01;
        /// <summary>
        /// Der größte Abstand [m] der Öffnungsmitte von den Ebenen der maßgeblichen Wirtsfläche (längs ihrer Normalen) —
        /// weiter weg liegt die Öffnung nicht in dieser Fläche (etwa im anderen Schenkel einer gegliederten Wand).
        /// </summary>
        internal const double AUSSPARUNG_ABSTAND_M = 0.5;

        /// <summary>
        /// <b>Ist die Öffnung im Körper des Wirts schon ausgespart?</b> (G5-N) Die Mitte <paramref name="mitte"/> der Öffnung
        /// wird längs der Wirtsnormalen <paramref name="n"/> auf die Dreiecke der Wirtskörper projiziert, deren Normale unter
        /// 5° parallel zu <paramref name="n"/> liegt (beide Seiten). Ausgespart ist sie, wenn ihre Mitte im Umriss dieser
        /// Dreiecke liegt (Toleranz <see cref="AUSSPARUNG_TOLERANZ_M"/>), höchstens <see cref="AUSSPARUNG_ABSTAND_M"/> vor
        /// oder hinter ihren Ebenen, und kein Dreieck sie bedeckt — eine Fläche mit innerer Begrenzung (<c>IfcFaceBound</c>)
        /// oder ein Profil mit Löchern (<c>IfcArbitraryProfileDefWithVoids</c>). Ohne Körper, Normale oder Mitte: nein.
        /// </summary>
        internal static bool Ausgespart(IEnumerable<Dateikoerper> wirt, double[] n, double[] mitte)
        {
            if (wirt == null || n == null || mitte == null) return false;
            double betragN = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
            if (betragN <= 1e-12) return false;
            double[] e = { n[0] / betragN, n[1] / betragN, n[2] / betragN };
            double[] hilfe = Math.Abs(e[2]) < 0.9 ? new[] { 0.0, 0.0, 1.0 } : new[] { 1.0, 0.0, 0.0 };
            double[] u = Einheit(Kreuz(hilfe, e)), v = Kreuz(e, u);
            double grenze = Math.Cos(IfcBauteilkoerper.ZUSAMMENFASSEN_GRAD * Math.PI / 180.0);
            double pu = Punkt(mitte, u), pv = Punkt(mitte, v), ps = Punkt(mitte, e);
            double umin = double.PositiveInfinity, umax = double.NegativeInfinity, vmin = double.PositiveInfinity, vmax = double.NegativeInfinity;
            double smin = double.PositiveInfinity, smax = double.NegativeInfinity;
            bool gefunden = false, bedeckt = false;
            foreach (Dateikoerper k in wirt)
            {
                if (k == null) continue;
                foreach (int[] d in k.Dreiecke)
                {
                    double[] a = Flaechenvektor(k, d);
                    double betrag = Math.Sqrt(Punkt(a, a));
                    if (betrag <= 1e-12 || Math.Abs(Punkt(a, e)) / betrag < grenze) continue;
                    var ecken = new double[3][];
                    for (int i = 0; i < 3; i++)
                    {
                        double[] q = k.PunkteM[d[i]];
                        ecken[i] = new[] { Punkt(q, u), Punkt(q, v) };
                        umin = Math.Min(umin, ecken[i][0]); umax = Math.Max(umax, ecken[i][0]);
                        vmin = Math.Min(vmin, ecken[i][1]); vmax = Math.Max(vmax, ecken[i][1]);
                        double sq = Punkt(q, e);
                        smin = Math.Min(smin, sq); smax = Math.Max(smax, sq);
                    }
                    gefunden = true;
                    if (!bedeckt && AbstandZumDreieck(pu, pv, ecken) <= AUSSPARUNG_TOLERANZ_M) bedeckt = true;
                }
            }
            if (!gefunden || bedeckt) return false;
            const double T = AUSSPARUNG_TOLERANZ_M;
            return pu >= umin - T && pu <= umax + T && pv >= vmin - T && pv <= vmax + T
                   && ps >= smin - AUSSPARUNG_ABSTAND_M && ps <= smax + AUSSPARUNG_ABSTAND_M;
        }

        /// <summary>
        /// <b>Liegt die Öffnung in Ebene und Umriss des Wirts?</b> (G5-3d) Abstand [m] der Mitte <paramref name="mitte"/> von
        /// der nächsten Seitenfläche der Wirtskörper, deren Normale unter 5° parallel zu einer der Richtungen
        /// <paramref name="normalen"/> liegt: <c>null</c>, wenn die Mitte außerhalb jedes Umrisses (Toleranz
        /// <see cref="AUSSPARUNG_TOLERANZ_M"/>, in der Ebene) oder weiter als <see cref="AUSSPARUNG_ABSTAND_M"/> vor oder hinter
        /// den Ebenen liegt; sonst der Abstand quer zur Ebene (0 = in der Wand). Anders als <see cref="Ausgespart"/> zählt der
        /// Umriss je Seitenfläche samt ihren Aussparungen — eine Öffnung in einem Loch der Wand liegt in ihr. Ohne Körper,
        /// Richtung oder Mitte: <c>null</c>. <paramref name="normale"/> ist die Richtung der getroffenen Seitenfläche.
        /// </summary>
        internal static double? AbstandZurWand(IEnumerable<Dateikoerper> wirt, IEnumerable<double[]> normalen, double[] mitte, out double[] normale)
        {
            normale = null;
            if (wirt == null || normalen == null || mitte == null) return null;
            double grenze = Math.Cos(IfcBauteilkoerper.ZUSAMMENFASSEN_GRAD * Math.PI / 180.0);
            double? bester = null;
            foreach (double[] n in normalen)
            {
                if (n == null) continue;
                double betragN = Math.Sqrt(Punkt(n, n));
                if (betragN <= 1e-12) continue;
                double[] e = { n[0] / betragN, n[1] / betragN, n[2] / betragN };
                double[] hilfe = Math.Abs(e[2]) < 0.9 ? new[] { 0.0, 0.0, 1.0 } : new[] { 1.0, 0.0, 0.0 };
                double[] u = Einheit(Kreuz(hilfe, e)), v = Kreuz(e, u);
                double pu = Punkt(mitte, u), pv = Punkt(mitte, v), ps = Punkt(mitte, e);
                // Die Seitenflächen: zusammenhängende parallele Dreiecke gleicher Ebene, je Ebene ihr Umriss in (u, v).
                var ebenen = new List<(double S, double Umin, double Umax, double Vmin, double Vmax)>();
                foreach (Dateikoerper k in wirt)
                {
                    if (k == null) continue;
                    foreach (int[] d in k.Dreiecke)
                    {
                        double[] a = Flaechenvektor(k, d);
                        double betrag = Math.Sqrt(Punkt(a, a));
                        if (betrag <= 1e-12 || Math.Abs(Punkt(a, e)) / betrag < grenze) continue;
                        double s = 0.0, umin = double.PositiveInfinity, umax = double.NegativeInfinity, vmin = double.PositiveInfinity, vmax = double.NegativeInfinity;
                        for (int i = 0; i < 3; i++)
                        {
                            double[] q = k.PunkteM[d[i]];
                            double qu = Punkt(q, u), qv = Punkt(q, v);
                            umin = Math.Min(umin, qu); umax = Math.Max(umax, qu);
                            vmin = Math.Min(vmin, qv); vmax = Math.Max(vmax, qv);
                            s += Punkt(q, e) / 3.0;
                        }
                        int j = ebenen.FindIndex(x => Math.Abs(x.S - s) <= AUSSPARUNG_TOLERANZ_M);
                        if (j < 0) ebenen.Add((s, umin, umax, vmin, vmax));
                        else
                        {
                            var x = ebenen[j];
                            ebenen[j] = (x.S, Math.Min(x.Umin, umin), Math.Max(x.Umax, umax), Math.Min(x.Vmin, vmin), Math.Max(x.Vmax, vmax));
                        }
                    }
                }
                const double T = AUSSPARUNG_TOLERANZ_M;
                foreach (var x in ebenen)
                {
                    if (pu < x.Umin - T || pu > x.Umax + T || pv < x.Vmin - T || pv > x.Vmax + T) continue;
                    double abstand = Math.Abs(ps - x.S);
                    if (abstand > AUSSPARUNG_ABSTAND_M) continue;
                    if (bester.HasValue && abstand >= bester.Value) continue;
                    bester = abstand;
                    normale = e;
                }
            }
            return bester;
        }

        /// <summary>Der Abstand eines Punkts der Ebene vom Dreieck <paramref name="ecken"/> [m]; 0 = innen oder auf dem Rand.</summary>
        private static double AbstandZumDreieck(double x, double y, double[][] ecken)
        {
            double Seite(double[] p, double[] q) => (q[0] - p[0]) * (y - p[1]) - (q[1] - p[1]) * (x - p[0]);
            double s0 = Seite(ecken[0], ecken[1]), s1 = Seite(ecken[1], ecken[2]), s2 = Seite(ecken[2], ecken[0]);
            if ((s0 >= 0 && s1 >= 0 && s2 >= 0) || (s0 <= 0 && s1 <= 0 && s2 <= 0)) return 0.0;
            double best = double.PositiveInfinity;
            for (int i = 0; i < 3; i++)
            {
                double[] p = ecken[i], q = ecken[(i + 1) % 3];
                double dx = q[0] - p[0], dy = q[1] - p[1], l2 = dx * dx + dy * dy;
                double t = l2 <= 1e-24 ? 0.0 : Math.Max(0.0, Math.Min(1.0, ((x - p[0]) * dx + (y - p[1]) * dy) / l2));
                double ex = p[0] + t * dx - x, ey = p[1] + t * dy - y;
                best = Math.Min(best, Math.Sqrt(ex * ex + ey * ey));
            }
            return best;
        }

        private static double Punkt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        private static double[] Kreuz(double[] a, double[] b)
            => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        private static double[] Einheit(double[] a)
        {
            double l = Math.Sqrt(Punkt(a, a));
            return new[] { a[0] / l, a[1] / l, a[2] / l };
        }

        /// <summary>Der doppelte Flächenvektor eines Dreiecks (b − a) × (c − a).</summary>
        private static double[] Flaechenvektor(Dateikoerper k, int[] d)
        {
            double[] p = k.PunkteM[d[0]], q = k.PunkteM[d[1]], r = k.PunkteM[d[2]];
            double ux = q[0] - p[0], uy = q[1] - p[1], uz = q[2] - p[2];
            double vx = r[0] - p[0], vy = r[1] - p[1], vz = r[2] - p[2];
            return new[] { uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx };
        }
    }
}
