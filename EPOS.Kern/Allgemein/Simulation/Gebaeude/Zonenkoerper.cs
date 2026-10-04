using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Fläche des Raumkörpers (Stufe G7b): eine Wand-, Boden- oder Deckengrenze eines schematischen Raums
    /// als ebener Ring in Weltkoordinaten [m], Umlaufsinn so, dass die Normale vom Raum weg zeigt.
    /// </summary>
    internal sealed class Koerperflaeche
    {
        /// <summary>Der Raum, aus dessen Sicht die Fläche steht.</summary>
        internal string RaumKennung { get; init; } = "";

        /// <summary>Die Grenze (Bauteil und Gegenstück).</summary>
        internal Grenzverweis Verweis { get; init; }

        /// <summary>Wand, Boden oder Decke aus Sicht des Raums.</summary>
        internal Grenzstellung Stellung { get; init; }

        /// <summary>Die vier Ecken (x, y, z), gerundet auf <see cref="Zonenkoerper.STELLEN"/> Nachkommastellen.</summary>
        internal IReadOnlyList<double[]> EckenM { get; init; } = Array.Empty<double[]>();

        /// <summary>
        /// Der Ring der Datei: die Ecken samt jedem Eckpunkt einer anderen Fläche desselben Raums, der auf einer
        /// ihrer Kanten liegt — so ist der Körper kantenschlüssig (1-mm-Regel, Datenaustauschkonzept 5.5 Punkt 4).
        /// </summary>
        internal IReadOnlyList<double[]> PunkteM { get; init; } = Array.Empty<double[]>();
    }

    /// <summary>Der Körper eines schematischen Raums: das Prisma seines Rechtecks und die Flächen seiner Grenzen.</summary>
    internal sealed class Raumkoerper
    {
        /// <summary>Die Kennung des Raums.</summary>
        internal string RaumKennung { get; init; } = "";

        /// <summary>Die Höhe des Bodens [m]: die Höhenlage des Geschosses, sonst 0 (keine Stapelung).</summary>
        internal double BodenM { get; init; }

        /// <summary>Die Raumhöhe [m].</summary>
        internal double HoeheM { get; init; }

        /// <summary>Die geschlossene Hülle: Boden, Decke und die vier Seiten des Prismas, Normale nach außen.</summary>
        internal IReadOnlyList<IReadOnlyList<double[]>> Schale { get; init; } = Array.Empty<IReadOnlyList<double[]>>();

        /// <summary>Die Flächen der Grenzen: erst die Wände je Kante und Strecke, dann Boden- und Deckenstreifen.</summary>
        internal IReadOnlyList<Koerperflaeche> Flaechen { get; init; } = Array.Empty<Koerperflaeche>();
    }

    /// <summary>
    /// <b>Die Raumkörper des Zonengeometrie-Modells</b> (Stufe G7b; Datenaustauschkonzept 5.5 und 14.3): je
    /// schematischem Raum mit Rechteck und Höhe das Prisma über dem Rechteck, die Wände auf den Strecken ihrer
    /// Kanten, Boden und Decke als Streifen. Liest nur das Modell — dieselbe Quelle für gbXML (<c>PolyLoop</c>,
    /// <c>ShellGeometry</c>), IFC (G7e) und die 3D-Ansicht. Räume mit Umriss aus Raumgrenzen tragen hier keinen
    /// Körper (benannt in <see cref="OhneKoerper"/>). Alle Koordinaten sind auf <see cref="STELLEN"/>
    /// Nachkommastellen gerundet: dieselbe Eingabe gibt dieselben Zahlen.
    /// </summary>
    internal sealed class Zonenkoerper
    {
        /// <summary>Nachkommastellen der Koordinaten [m] — 1 µm, weit unter der 1-mm-Kantenregel.</summary>
        internal const int STELLEN = 6;

        /// <summary>Kleinster Randabstand [m] einer Öffnung in ihrer Wand (5.5 Punkt 6).</summary>
        internal const double OEFFNUNG_RAND_M = 0.1;

        /// <summary>Größter Anteil einer Öffnung an ihrer Wandstrecke (5.5 Punkt 6).</summary>
        internal const double OEFFNUNG_ANTEIL_MAX = 0.9;

        private const double AUF_KANTE_M = 1e-6;

        private Zonenkoerper() { }

        /// <summary>Die Körper in der Reihenfolge der Räume des Modells.</summary>
        internal IReadOnlyList<Raumkoerper> Raeume { get; private set; } = Array.Empty<Raumkoerper>();

        /// <summary>Die Räume mit Umriss, aber ohne Körper (Umriss aus Raumgrenzen, kein Rechteck, keine Höhe).</summary>
        internal IReadOnlyList<string> OhneKoerper { get; private set; } = Array.Empty<string>();

        /// <summary>Der Körper eines Raums; <c>null</c> = keiner.</summary>
        internal Raumkoerper Raum(string kennung)
            => Raeume.FirstOrDefault(r => string.Equals(r.RaumKennung, kennung, StringComparison.Ordinal));

        /// <summary>Die Fläche eines Bauteils aus Sicht eines Raums; <c>null</c> = keine (mehrere: die erste).</summary>
        internal Koerperflaeche Flaeche(string raumKennung, string bauteilKennung)
            => Raum(raumKennung)?.Flaechen.FirstOrDefault(f => string.Equals(f.Verweis?.BauteilKennung, bauteilKennung, StringComparison.Ordinal));

        /// <summary>Bildet die Körper aus dem Modell; schreibt nichts, wirft nicht.</summary>
        internal static Zonenkoerper Bilden(Zonengeometrie z)
        {
            var k = new Zonenkoerper();
            if (z == null) return k;
            var raeume = new List<Raumkoerper>();
            var ohne = new List<string>();
            foreach (Raumumriss r in z.Raeume)
            {
                if (r.Polygone.Count == 0) continue;
                bool rechteck = r.Herkunft == Geometrieherkunft.Schematisch && r.Polygone.Count == 1 && r.Polygone[0].Punkte.Count == 4
                                && r.Polygone[0].Kanten.Count == 4;
                if (!rechteck || !(r.HoeheM > 0.0))
                {
                    ohne.Add(r.RaumKennung);
                    continue;
                }
                double boden = r.Geschoss >= 0 && r.Geschoss < z.Geschosse.Count ? z.Geschosse[r.Geschoss].LageM ?? 0.0 : 0.0;
                raeume.Add(Koerper(r, boden, r.HoeheM.Value));
            }
            k.Raeume = raeume;
            k.OhneKoerper = ohne;
            return k;
        }

        private static Raumkoerper Koerper(Raumumriss r, double z0, double h)
        {
            Umrisspolygon p = r.Polygone[0];
            double z1 = z0 + h;
            double[] p0 = p.Punkte[0], p3 = p.Punkte[3];
            double[] u0 = Einheit(p0, p.Punkte[1]);
            var flaechen = new List<(Grenzverweis V, Grenzstellung S, double[][] E)>();

            for (int i = 0; i < 4; i++)
            {
                double[] a = p.Punkte[i];
                double[] u = Einheit(a, p.Punkte[(i + 1) % 4]);
                foreach (Kantenabschnitt x in p.Kanten[i].Abschnitte)
                {
                    double[] von = { a[0] + u[0] * x.VonM, a[1] + u[1] * x.VonM };
                    double[] bis = { a[0] + u[0] * x.BisM, a[1] + u[1] * x.BisM };
                    flaechen.Add((x.Verweis, Grenzstellung.Wand, new[] { P(von, z0), P(bis, z0), P(bis, z1), P(von, z1) }));
                }
            }
            foreach (Kantenabschnitt x in r.BodenStreifen)
            {
                double[] s0 = Versetzt(p0, u0, x.VonM), s1 = Versetzt(p0, u0, x.BisM), t0 = Versetzt(p3, u0, x.VonM), t1 = Versetzt(p3, u0, x.BisM);
                flaechen.Add((x.Verweis, Grenzstellung.Boden, new[] { P(s0, z0), P(t0, z0), P(t1, z0), P(s1, z0) }));
            }
            foreach (Kantenabschnitt x in r.DeckenStreifen)
            {
                double[] s0 = Versetzt(p0, u0, x.VonM), s1 = Versetzt(p0, u0, x.BisM), t0 = Versetzt(p3, u0, x.VonM), t1 = Versetzt(p3, u0, x.BisM);
                flaechen.Add((x.Verweis, Grenzstellung.Decke, new[] { P(s0, z1), P(s1, z1), P(t1, z1), P(t0, z1) }));
            }

            // Kantenschlüssig: jeder Eckpunkt einer Fläche, der auf der Kante einer anderen liegt, steht in deren Ring.
            List<double[]> alle = flaechen.SelectMany(f => f.E).ToList();
            var ergebnis = flaechen.Select(f => new Koerperflaeche
            {
                RaumKennung = r.RaumKennung,
                Verweis = f.V,
                Stellung = f.S,
                EckenM = f.E,
                PunkteM = Kantenschluessig(f.E, alle),
            }).ToList();

            double[][] c = p.Punkte.Select(q => new[] { q[0], q[1] }).ToArray();
            var schale = new List<IReadOnlyList<double[]>>
            {
                new[] { P(c[0], z0), P(c[3], z0), P(c[2], z0), P(c[1], z0) },
                new[] { P(c[0], z1), P(c[1], z1), P(c[2], z1), P(c[3], z1) },
            };
            for (int i = 0; i < 4; i++)
                schale.Add(new[] { P(c[i], z0), P(c[(i + 1) % 4], z0), P(c[(i + 1) % 4], z1), P(c[i], z1) });

            return new Raumkoerper { RaumKennung = r.RaumKennung, BodenM = R(z0), HoeheM = R(h), Schale = schale, Flaechen = ergebnis };
        }

        /// <summary>
        /// <b>Das Rechteck einer Öffnung</b> in ihrer Wand (5.5 Punkt 6): Die Wandstrecke wird in
        /// <paramref name="anzahl"/> gleiche Felder geteilt, die Öffnung steht mittig in Feld
        /// <paramref name="stelle"/>, im Seitenverhältnis des Felds, mit ihrer Fläche. Passt sie nicht —
        /// Randabstand unter <see cref="OEFFNUNG_RAND_M"/> oder mehr als <see cref="OEFFNUNG_ANTEIL_MAX"/> des Felds —,
        /// wird sie verkleinert und <paramref name="begrenzt"/> gesetzt. Umlauf wie die Wand.
        /// </summary>
        /// <returns>Die vier Ecken; <c>null</c> = keine Wand oder kein Platz.</returns>
        internal static IReadOnlyList<double[]> Oeffnung(Koerperflaeche wand, int stelle, int anzahl, double flaecheM2, out bool begrenzt)
        {
            begrenzt = false;
            if (wand == null || wand.Stellung != Grenzstellung.Wand || wand.EckenM.Count != 4 || anzahl < 1 || !(flaecheM2 > 0.0)) return null;
            double[] a0 = wand.EckenM[0], b0 = wand.EckenM[1], a1 = wand.EckenM[3];
            double dx = b0[0] - a0[0], dy = b0[1] - a0[1];
            double w = Math.Sqrt(dx * dx + dy * dy), h = a1[2] - a0[2];
            if (!(w > 0.0) || !(h > 0.0)) return null;
            double ux = dx / w, uy = dy / w, feld = w / anzahl;
            double f = Math.Sqrt(flaecheM2 / (feld * h));
            double fMax = Math.Min(Math.Sqrt(OEFFNUNG_ANTEIL_MAX), Math.Min((feld - 2.0 * OEFFNUNG_RAND_M) / feld, (h - 2.0 * OEFFNUNG_RAND_M) / h));
            if (!(fMax > 0.0))
            {
                begrenzt = true;
                return null;
            }
            if (f > fMax)
            {
                f = fMax;
                begrenzt = true;
            }
            double ow = feld * f, oh = h * f;
            double s = stelle * feld + (feld - ow) / 2.0, z = a0[2] + (h - oh) / 2.0;
            double[] c0 = { a0[0] + ux * s, a0[1] + uy * s }, c1 = { a0[0] + ux * (s + ow), a0[1] + uy * (s + ow) };
            return new[] { P(c0, z), P(c1, z), P(c1, z + oh), P(c0, z + oh) };
        }

        // ------------------------------------------------------------------
        //  Hilfen
        // ------------------------------------------------------------------

        private static double R(double v) => Math.Round(v, STELLEN, MidpointRounding.ToEven) + 0.0;

        private static double[] P(double[] xy, double z) => new[] { R(xy[0]), R(xy[1]), R(z) };

        private static double[] Versetzt(double[] a, double[] u, double s) => new[] { a[0] + u[0] * s, a[1] + u[1] * s };

        private static double[] Einheit(double[] a, double[] b)
        {
            double dx = b[0] - a[0], dy = b[1] - a[1], l = Math.Sqrt(dx * dx + dy * dy);
            return l > 0.0 ? new[] { dx / l, dy / l } : new[] { 0.0, 0.0 };
        }

        /// <summary>Der Ring mit jedem Punkt aus <paramref name="alle"/>, der echt innerhalb einer seiner Kanten liegt — nach Lage geordnet, ohne Doppel.</summary>
        internal static List<double[]> Kantenschluessig(IReadOnlyList<double[]> ecken, IReadOnlyList<double[]> alle)
        {
            var ring = new List<double[]>();
            for (int i = 0; i < ecken.Count; i++)
            {
                double[] a = ecken[i], b = ecken[(i + 1) % ecken.Count];
                ring.Add(a);
                double lx = b[0] - a[0], ly = b[1] - a[1], lz = b[2] - a[2];
                double l2 = lx * lx + ly * ly + lz * lz;
                if (!(l2 > 0.0)) continue;
                var innen = new List<(double T, double[] P)>();
                foreach (double[] q in alle)
                {
                    double t = ((q[0] - a[0]) * lx + (q[1] - a[1]) * ly + (q[2] - a[2]) * lz) / l2;
                    double l = Math.Sqrt(l2);
                    if (t * l <= AUF_KANTE_M || (1.0 - t) * l <= AUF_KANTE_M) continue;
                    double ex = a[0] + t * lx - q[0], ey = a[1] + t * ly - q[1], ez = a[2] + t * lz - q[2];
                    if (Math.Sqrt(ex * ex + ey * ey + ez * ez) > AUF_KANTE_M) continue;
                    if (innen.Any(x => Gleich(x.P, q))) continue;
                    innen.Add((t, q));
                }
                ring.AddRange(innen.OrderBy(x => x.T).Select(x => x.P));
            }
            return ring;
        }

        private static bool Gleich(double[] a, double[] b)
            => Math.Abs(a[0] - b[0]) <= AUF_KANTE_M && Math.Abs(a[1] - b[1]) <= AUF_KANTE_M && Math.Abs(a[2] - b[2]) <= AUF_KANTE_M;
    }
}
