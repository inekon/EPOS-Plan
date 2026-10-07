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
