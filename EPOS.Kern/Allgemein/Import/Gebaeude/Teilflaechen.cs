using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Eine Teilfläche eines Bauteils aus Sicht einer Zone: Grenzen gleicher Orientierung und Neigung.</summary>
    internal sealed class Teilflaeche
    {
        /// <summary>Die Grenzen der Teilfläche, die größte zuerst.</summary>
        internal List<AbbildGrenze> Grenzen { get; } = new List<AbbildGrenze>();

        /// <summary>Die flächengewichtete Außennormale aus Sicht des Raums (Einheitsvektor).</summary>
        internal double[] Normale { get; set; }

        /// <summary>Die Bruttofläche: die Summe der Grenzen [m²].</summary>
        internal double BruttoM2 { get; set; }

        /// <summary>Die schon ausgeschnittenen Öffnungen der Grenzen [m²].</summary>
        internal double AusschnittM2 { get; set; }

        /// <summary>Die Neigung aus Sicht des Raums [°] (0° = nach oben, 90° = senkrecht, 180° = nach unten).</summary>
        internal double NeigungGrad { get; set; }

        /// <summary>Der Azimut der Außennormale [°] (0° = Nord, im Uhrzeigersinn); <c>null</c> = waagerecht.</summary>
        internal double? AzimutGrad { get; set; }
    }

    /// <summary>
    /// <b>Die Gliederung einer Zonenfläche in Teilflächen</b> — formatfrei für alle Wege mit Grenzen (Raumgrenzen der Datei,
    /// Raumkörperpaare, Bauteilkörper): Ein Bauteil, das eine Zone über mehrere ebene Seiten verschiedener Richtung sieht (eine
    /// gegliederte Wand über Eck, die Flächen eines Satteldachs über einem Raum), führt je Richtung eine Teilfläche mit eigener
    /// Orientierung und Neigung.
    /// <list type="bullet">
    /// <item><b>Zusammenfassen:</b> Grenzen, deren Außennormalen höchstens <see cref="ZUSAMMENFASSEN_GRAD"/> auseinander liegen
    /// (dieselbe Toleranz wie die Gliederung der Bauteilkörper, <see cref="IfcBauteilkoerper.ZUSAMMENFASSEN_GRAD"/>), bilden eine
    /// Teilfläche; die größte Grenze zuerst, gegen die flächengewichtete Richtung der Gruppe.</item>
    /// <item><b>Orientierung:</b> Trägt die größte Grenze Neigung und Azimut (Körperweg), gelten diese; sonst aus der
    /// Gruppennormale — Neigung aus n<sub>z</sub>, Azimut aus (n<sub>x</sub>, n<sub>y</sub>) gegen die angewandte Drehung.</item>
    /// <item><b>Öffnungen</b> gehen an die Teilfläche, deren Richtung die Normale ihrer Grenze trifft; ohne Normale an die
    /// größte Teilfläche.</item>
    /// </list>
    /// <para>Fehlt einer Grenze Fläche oder Normale, gibt es keine Gliederung (<c>null</c>); eine Teilfläche allein ist keine
    /// Gliederung — dann gilt die Zeile der Zonenfläche wie bisher.</para>
    /// </summary>
    internal static class Teilflaechen
    {
        /// <summary>Größter Winkel zwischen den Normalen zweier Grenzen derselben Teilfläche [°].</summary>
        internal const double ZUSAMMENFASSEN_GRAD = 5.0;

        /// <summary>
        /// Die Teilflächen der Grenzen <paramref name="grenzen"/>, die größte zuerst; <c>null</c> = keine Gliederung (weniger als
        /// zwei Richtungen, oder einer Grenze fehlt Fläche oder Normale).
        /// </summary>
        /// <param name="grenzen">Die Grenzen einer Zonenfläche.</param>
        /// <param name="drehungGrad">Die angewandte Drehung gegen Nord [°] für den Azimut aus der Normale.</param>
        internal static List<Teilflaeche> Gliedern(IReadOnlyList<AbbildGrenze> grenzen, double drehungGrad)
        {
            if (grenzen == null || grenzen.Count < 2) return null;
            if (grenzen.Any(g => !(g.FlaecheM2 > 0.0) || g.Normale == null || g.Normale.Length < 3 || Laenge(g.Normale) < 1e-9)) return null;
            double cosGrenze = Math.Cos(ZUSAMMENFASSEN_GRAD * Math.PI / 180.0);
            var gruppen = new List<(double[] Summe, Teilflaeche Teil, int Erstes)>();
            List<(AbbildGrenze G, int I)> geordnet = grenzen.Select((g, i) => (g, i))
                                                           .OrderByDescending(x => x.g.FlaecheM2.Value).ThenBy(x => x.i).ToList();
            foreach ((AbbildGrenze g, int i) in geordnet)
            {
                double[] n = Einheit(g.Normale);
                int treffer = gruppen.FindIndex(x => Punkt(Einheit(x.Summe), n) > cosGrenze);
                if (treffer < 0)
                {
                    gruppen.Add((new double[3], new Teilflaeche(), i));
                    treffer = gruppen.Count - 1;
                }
                (double[] summe, Teilflaeche t, _) = gruppen[treffer];
                double a = g.FlaecheM2.Value;
                for (int k = 0; k < 3; k++) summe[k] += n[k] * a;
                t.Grenzen.Add(g);
                t.BruttoM2 += a;
                t.AusschnittM2 += g.AusschnittM2;
            }
            if (gruppen.Count < 2) return null;
            var teile = new List<Teilflaeche>();
            foreach ((double[] summe, Teilflaeche t, _) in gruppen.OrderByDescending(x => x.Teil.BruttoM2).ThenBy(x => x.Erstes))
            {
                t.Normale = Einheit(summe);
                AbbildGrenze groesste = t.Grenzen[0];
                if (groesste.NeigungGrad.HasValue)
                {
                    t.NeigungGrad = groesste.NeigungGrad.Value;
                    t.AzimutGrad = groesste.AzimutGrad;
                }
                else
                {
                    t.NeigungGrad = Math.Round(Math.Acos(Math.Max(-1.0, Math.Min(1.0, t.Normale[2]))) * 180.0 / Math.PI, 6);
                    t.AzimutGrad = Math.Sqrt(t.Normale[0] * t.Normale[0] + t.Normale[1] * t.Normale[1]) <= 1e-9 ? (double?)null
                                 : Math.Round(Normiert(Math.Atan2(t.Normale[0], t.Normale[1]) * 180.0 / Math.PI - drehungGrad), 6);
                }
                teile.Add(t);
            }
            return teile;
        }

        /// <summary>
        /// Die Teilfläche, an der die Öffnung <paramref name="o"/> liegt: die Richtung, die die Normale ihrer größten Grenze in
        /// den Räumen <paramref name="raeume"/> (sonst ihrer größten Grenze überhaupt) bis <see cref="ZUSAMMENFASSEN_GRAD"/> trifft;
        /// sonst die größte Teilfläche (Stelle 0).
        /// </summary>
        internal static int Stelle(IReadOnlyList<Teilflaeche> teile, AbbildBauteil o, ICollection<string> raeume)
        {
            List<AbbildGrenze> mitNormale = o.Grenzen.Where(g => g.Normale != null && g.Normale.Length >= 3 && Laenge(g.Normale) >= 1e-9).ToList();
            AbbildGrenze grenze = mitNormale.Where(g => g.RaumKennung != null && raeume.Contains(g.RaumKennung))
                                            .OrderByDescending(g => g.FlaecheM2 ?? 0.0).FirstOrDefault()
                                  ?? mitNormale.OrderByDescending(g => g.FlaecheM2 ?? 0.0).FirstOrDefault();
            if (grenze == null) return 0;
            double[] n = Einheit(grenze.Normale);
            double cosGrenze = Math.Cos(ZUSAMMENFASSEN_GRAD * Math.PI / 180.0);
            int beste = -1;
            double besterWert = cosGrenze;
            for (int i = 0; i < teile.Count; i++)
            {
                double w = Punkt(teile[i].Normale, n);
                if (w > besterWert) { besterWert = w; beste = i; }
            }
            return beste < 0 ? 0 : beste;
        }

        private static double Normiert(double grad)
        {
            double a = grad % 360.0;
            if (a < 0.0) a += 360.0;
            return a + 0.0;
        }

        private static double Laenge(double[] v) => Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);

        private static double Punkt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        private static double[] Einheit(double[] v)
        {
            double l = Laenge(v);
            return l <= 1e-12 ? new[] { 0.0, 0.0, 0.0 } : new[] { v[0] / l, v[1] / l, v[2] / l };
        }
    }
}
