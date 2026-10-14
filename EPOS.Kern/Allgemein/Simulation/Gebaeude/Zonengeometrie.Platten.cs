using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ======================================================================================
    //  Das Zonengeometrie-Modell — die Bauteilplatten an den Grundriss-Prismen (HC-5c, Konzept HottCAD-Verbund 11.4 „Körper“)
    // ======================================================================================

    /// <summary>
    /// <b>Eine Bauteilplatte an einem Grundriss-Prisma</b> (HC-5c): eine Wand auf einer Prismenkante bzw. ein Boden- oder
    /// Deckenstreifen auf dem Prisma — als ebener Ring in Weltkoordinaten des Modellsystems [m] (x, y, z), Umlaufsinn so,
    /// dass die Normale vom Raum weg zeigt. Nur Gestalt und Lage: Die Fläche des Bauteils bleibt, wie sie ist.
    /// </summary>
    internal sealed class Grundrissplatte
    {
        /// <summary>Die Grenze (Bauteil und Gegenstück).</summary>
        internal Grenzverweis Verweis { get; init; }

        /// <summary>Wand, Boden oder Decke aus Sicht des Raums.</summary>
        internal Grenzstellung Stellung { get; init; }

        /// <summary>Die Ecken (x, y, z) [m]; Wand: vier, Boden und Decke: der Streifen samt Stegen zu Löchern.</summary>
        internal IReadOnlyList<double[]> EckenM { get; init; } = Array.Empty<double[]>();

        /// <summary>Der Flächeninhalt der Platte [m²].</summary>
        internal double FlaecheM2 { get; init; }

        /// <summary>Wand: das Polygon des Raums; sonst das Prisma des Streifens.</summary>
        internal int Polygon { get; init; } = -1;

        /// <summary>Wand: die Kante im Polygon; Boden und Decke: −1.</summary>
        internal int Kante { get; init; } = -1;
    }

    /// <summary>Die Art einer Prismenkante (Konzept 11.4): innen (derselbe Raum), Trennkante zu einem anderen Raum, außen.</summary>
    internal enum Prismenkantenart
    {
        /// <summary>Gegenläufige Kante desselben Raums bzw. derselben Zone — dort keine Platte.</summary>
        Innen,

        /// <summary>Gegenläufige Kante eines anderen Raums bzw. einer anderen Zone.</summary>
        Trenn,

        /// <summary>Keine gegenläufige Kante.</summary>
        Aussen,
    }

    internal sealed partial class Zonengeometrie
    {
        /// <summary>Größte Abweichung [°] zwischen dem Azimut einer Wand und dem ihrer Kante (wie die Flächenklassifikation).</summary>
        internal const double PLATTE_WINKEL_GRAD = 15.0;

        /// <summary>Größter Abstand [m] zweier gegenläufiger Kanten, die einander gegenüberstehen (wie <c>Koerpernachbarschaft</c>).</summary>
        internal const double PLATTE_NACHBAR_M = 0.8;

        /// <summary>Kleinster Anteil der Kantenlänge, auf dem eine gegenläufige Kante gegenüberstehen muss.</summary>
        internal const double PLATTE_NACHBAR_ANTEIL = 0.5;

        /// <summary>Gegenläufig heißt: Richtungen bis 10° von der Gegenrichtung.</summary>
        private static readonly double GEGENLAEUFIG_COS = Math.Cos(10.0 * Math.PI / 180.0);

        /// <summary>Kürzeste Platte [m] bzw. kleinste Fläche [m²], die geschrieben wird.</summary>
        private const double PLATTE_MIN = 1e-9;

        /// <summary>Eine Prismenkante beim Zuordnen.</summary>
        private sealed class Prismenkante
        {
            internal int Raum, Polygon, Kante;
            internal double[] A, B;
            internal double Laenge, Boden, Hoehe;
            internal double? Azimut;
            internal Prismenkantenart Art = Prismenkantenart.Aussen;
            internal double Belegt;                          // belegte Höhe über dem Boden [m]
            internal readonly List<Grenzverweis> Grenzen = new List<Grenzverweis>();
        }

        /// <summary>
        /// <b>Die Platten der Räume mit Grundriss-Prismen</b> (HC-5c; Konzept 11.4 „Körper“). Regeln:
        /// <list type="number">
        /// <item><b>Kantenart.</b> Eine Kante mit gegenläufiger Kante (bis 10°, Abstand bis <see cref="PLATTE_NACHBAR_M"/>, gegenüber
        /// auf mindestens der halben Länge, Prismen in der Höhe überlappend) eines anderen Polygons DESSELBEN Raums (im Export: derselben
        /// Zone) ist innen — dort keine Platte —, eines anderen Raums Trennkante; alle übrigen sind außen.</item>
        /// <item><b>Wände.</b> Kandidaten einer Wand sind die nicht inneren Kanten ihres Raums, deren wahrer Azimut höchstens
        /// <see cref="PLATTE_WINKEL_GRAD"/> von dem der Wand abweicht, sonst die ihres Sektors — je unter den Kanten ihrer Art (Außenluft
        /// und Erdreich außen, sonst Trennkante), ohne Kante dieser Art unter allen. Wände mit denselben Kandidaten bilden eine Gruppe:
        /// Ihre Fläche liegt als ein Band der Höhe ΣA / ΣL über den Kanten (nach Kantenlänge verteilt), jede Wand als Strecke ihrer
        /// Fläche entlang der Kanten — Platten je Kante, die größte zuerst. Ein zweites Band derselben Kante steht über dem ersten; die
        /// Plattenhöhe endet an der Prismenhöhe.</item>
        /// <item><b>Boden und Decke.</b> Je Stellung verteilt sich die Fläche der Zeilen nach Prismenfläche auf die Prismen des Raums;
        /// je Prisma liegen die Zeilen als Streifen längs x hintereinander (Schnittlinie durch Halbierung auf die Fläche), auf dem
        /// Boden bzw. der Oberkante des Prismas. Übersteigt die Fläche die der Prismen, füllen die Streifen sie anteilig.</item>
        /// </list>
        /// Was keine Kante findet, steht benannt in <see cref="Raumumriss.OhneKante"/>.
        /// </summary>
        private static void PlattenZuordnen(List<Umrissentwurf> entwurf)
        {
            var kanten = new List<Prismenkante>();
            for (int i = 0; i < entwurf.Count; i++)
            {
                Umrissentwurf e = entwurf[i];
                if (!Prismen(e)) continue;
                for (int pi = 0; pi < e.Polygone.Count; pi++)
                {
                    Umrisspolygon p = e.Polygone[pi];
                    for (int k = 0; k < p.Kanten.Count; k++)
                    {
                        Umrisskante kante = p.Kanten[k];
                        if (!(kante.LaengeM > PUNKT_GLEICH_M)) continue;
                        kanten.Add(new Prismenkante
                        {
                            Raum = i, Polygon = pi, Kante = k, A = p.Punkte[k], B = p.Punkte[(k + 1) % p.Punkte.Count],
                            Laenge = kante.LaengeM, Azimut = kante.AzimutGrad, Boden = p.PrismaBodenM.Value, Hoehe = p.PrismaHoeheM.Value,
                        });
                    }
                }
            }
            if (kanten.Count == 0) return;
            Kantenarten(kanten);

            for (int i = 0; i < entwurf.Count; i++)
            {
                Umrissentwurf e = entwurf[i];
                if (!Prismen(e)) continue;
                List<Prismenkante> eigene = kanten.Where(k => k.Raum == i).ToList();
                var platten = new List<Grundrissplatte>();
                var gesetzt = new HashSet<Grenzverweis>();
                Waende(e, eigene, platten, gesetzt);
                Flaechen(e, Grenzstellung.Boden, platten, gesetzt);
                Flaechen(e, Grenzstellung.Decke, platten, gesetzt);
                e.Platten = platten;
                e.OhneKante = e.OhneKante.Where(v => !gesetzt.Contains(v)).ToList();

                // Die Kanten tragen die Wände, die auf ihnen stehen (Ansicht: Art je Kante).
                var neu = new List<Umrisspolygon>(e.Polygone.Count);
                for (int pi = 0; pi < e.Polygone.Count; pi++)
                {
                    Umrisspolygon p = e.Polygone[pi];
                    var neueKanten = new List<Umrisskante>(p.Kanten.Count);
                    for (int k = 0; k < p.Kanten.Count; k++)
                    {
                        Umrisskante alt = p.Kanten[k];
                        Prismenkante pk = eigene.FirstOrDefault(x => x.Polygon == pi && x.Kante == k);
                        neueKanten.Add(pk == null || pk.Grenzen.Count == 0 ? alt
                            : new Umrisskante(alt.Index, alt.LaengeM, alt.AzimutGrad, pk.Grenzen.ToList()) { Abschnitte = alt.Abschnitte });
                    }
                    neu.Add(new Umrisspolygon(p.Punkte, neueKanten, p.FlaecheM2, p.EbeneM, p.Quelle)
                    {
                        Loecher = p.Loecher, PrismaBodenM = p.PrismaBodenM, PrismaHoeheM = p.PrismaHoeheM,
                    });
                }
                e.Polygone = neu;
            }
        }

        private static bool Prismen(Umrissentwurf e) => e.Polygone.Count > 0 && e.Polygone.All(p => p.IstPrisma);

        /// <summary>Die Art jeder Kante (Regel 1): innen vor Trenn vor außen.</summary>
        private static void Kantenarten(List<Prismenkante> kanten)
        {
            foreach (Prismenkante e in kanten)
            {
                double ux = (e.B[0] - e.A[0]) / e.Laenge, uy = (e.B[1] - e.A[1]) / e.Laenge;
                Prismenkantenart art = Prismenkantenart.Aussen;
                foreach (Prismenkante f in kanten)
                {
                    if (f.Raum == e.Raum && f.Polygon == e.Polygon) continue;
                    double vx = (f.B[0] - f.A[0]) / f.Laenge, vy = (f.B[1] - f.A[1]) / f.Laenge;
                    if (ux * vx + uy * vy > -GEGENLAEUFIG_COS) continue;
                    if (Math.Min(e.Boden + e.Hoehe, f.Boden + f.Hoehe) - Math.Max(e.Boden, f.Boden) <= KANTE_UEBERLAPPUNG_M) continue;
                    // Abstand der Endpunkte von f zur Geraden von e, Überdeckung auf der Achse von e.
                    double da = Math.Abs((f.A[0] - e.A[0]) * uy - (f.A[1] - e.A[1]) * ux);
                    double db = Math.Abs((f.B[0] - e.A[0]) * uy - (f.B[1] - e.A[1]) * ux);
                    if (da > PLATTE_NACHBAR_M || db > PLATTE_NACHBAR_M) continue;
                    double ta = (f.A[0] - e.A[0]) * ux + (f.A[1] - e.A[1]) * uy;
                    double tb = (f.B[0] - e.A[0]) * ux + (f.B[1] - e.A[1]) * uy;
                    double ueber = Math.Min(e.Laenge, Math.Max(ta, tb)) - Math.Max(0.0, Math.Min(ta, tb));
                    if (ueber < PLATTE_NACHBAR_ANTEIL * e.Laenge) continue;
                    if (f.Raum == e.Raum)
                    {
                        art = Prismenkantenart.Innen;
                        break;
                    }
                    art = Prismenkantenart.Trenn;
                }
                e.Art = art;
            }
        }

        /// <summary>Die Wände eines Raums auf seine Kanten (Regel 2).</summary>
        private static void Waende(Umrissentwurf e, List<Prismenkante> eigene, List<Grundrissplatte> platten, HashSet<Grenzverweis> gesetzt)
        {
            List<Prismenkante> frei = eigene.Where(k => k.Art != Prismenkantenart.Innen && k.Azimut.HasValue).ToList();
            var gruppen = new List<(List<Prismenkante> Kanten, List<(Grenzverweis V, double A)> Waende)>();
            foreach (Umrissseite s in e.Raum.Seiten)
            {
                if (s.Verweis?.Stellung != Grenzstellung.Wand || !(s.FlaecheM2 > PLATTE_MIN) || gesetzt.Contains(s.Verweis)) continue;
                double? azimut = s.AzimutGrad ?? (s.Sektor is int sektor ? sektor * 90.0 : (double?)null);
                if (!azimut.HasValue) continue;
                bool aussen = s.Verweis.Lage == Randbedingung.Aussenluft || s.Verweis.Lage == Randbedingung.Erdreich;
                List<Prismenkante> kandidaten = Kandidaten(frei, azimut.Value, aussen);
                if (kandidaten.Count == 0) continue;
                var gruppe = gruppen.FirstOrDefault(g => g.Kanten.SequenceEqual(kandidaten));
                if (gruppe.Kanten == null)
                {
                    gruppe = (kandidaten, new List<(Grenzverweis V, double A)>());
                    gruppen.Add(gruppe);
                }
                gruppe.Waende.Add((s.Verweis, s.FlaecheM2.Value));
                gesetzt.Add(s.Verweis);
            }

            foreach ((List<Prismenkante> kette, List<(Grenzverweis V, double A)> waende) in gruppen)
            {
                double laenge = kette.Sum(k => k.Laenge), flaeche = waende.Sum(w => w.A);
                double hoehe = flaeche / laenge;
                double unten = kette.Max(k => k.Belegt);
                double s0 = 0.0;
                foreach ((Grenzverweis v, double a) in waende)
                {
                    double s1 = s0 + a / hoehe;
                    var eigenePlatten = new List<Grundrissplatte>();
                    double c = 0.0;
                    foreach (Prismenkante k in kette)
                    {
                        double von = Math.Max(s0, c) - c, bis = Math.Min(s1, c + k.Laenge) - c;
                        c += k.Laenge;
                        double z0 = k.Boden + Math.Min(unten, k.Hoehe), z1 = k.Boden + Math.Min(unten + hoehe, k.Hoehe);
                        if (bis - von <= PLATTE_MIN || z1 - z0 <= PLATTE_MIN) continue;
                        double ux = (k.B[0] - k.A[0]) / k.Laenge, uy = (k.B[1] - k.A[1]) / k.Laenge;
                        double[] p0 = { k.A[0] + ux * von, k.A[1] + uy * von }, p1 = { k.A[0] + ux * bis, k.A[1] + uy * bis };
                        eigenePlatten.Add(new Grundrissplatte
                        {
                            Verweis = v, Stellung = Grenzstellung.Wand, Polygon = k.Polygon, Kante = k.Kante,
                            EckenM = new[] { new[] { p0[0], p0[1], z0 }, new[] { p1[0], p1[1], z0 }, new[] { p1[0], p1[1], z1 }, new[] { p0[0], p0[1], z1 } },
                            FlaecheM2 = (bis - von) * (z1 - z0),
                        });
                        if (!k.Grenzen.Contains(v)) k.Grenzen.Add(v);
                    }
                    // Die größte Platte zuerst (an ihr stehen die Öffnungen), sonst in der Folge der Kanten.
                    platten.AddRange(eigenePlatten.Select((p, n) => (p, n)).OrderByDescending(x => Math.Round(x.p.FlaecheM2, 9)).ThenBy(x => x.n).Select(x => x.p));
                    if (eigenePlatten.Count == 0) gesetzt.Remove(v);
                    s0 = s1;
                }
                foreach (Prismenkante k in kette) k.Belegt = unten + hoehe;
            }
        }

        /// <summary>Die Kandidaten einer Wand (Regel 2): ±15°, sonst Sektor — je erst unter den Kanten ihrer Art.</summary>
        private static List<Prismenkante> Kandidaten(List<Prismenkante> frei, double azimut, bool aussen)
        {
            Prismenkantenart art = aussen ? Prismenkantenart.Aussen : Prismenkantenart.Trenn;
            int sektor = GebaeudeAggregation.Sektor(azimut);
            foreach (Func<Prismenkante, bool> richtung in new Func<Prismenkante, bool>[]
                     {
                         k => Winkelabstand(k.Azimut.Value, azimut) <= PLATTE_WINKEL_GRAD,
                         k => GebaeudeAggregation.Sektor(k.Azimut.Value) == sektor,
                     })
            {
                List<Prismenkante> passend = frei.Where(richtung).ToList();
                List<Prismenkante> gleicheArt = passend.Where(k => k.Art == art).ToList();
                if (gleicheArt.Count > 0) return gleicheArt;
                if (passend.Count > 0) return passend;
            }
            return new List<Prismenkante>();
        }

        /// <summary>Der Abstand zweier Azimute [°] in [0, 180].</summary>
        internal static double Winkelabstand(double a, double b)
        {
            double d = Math.Abs(Normiert(a) - Normiert(b));
            return d > 180.0 ? 360.0 - d : d;
        }

        /// <summary>Boden- bzw. Deckenzeilen eines Raums als Streifen auf seinen Prismen (Regel 3).</summary>
        private static void Flaechen(Umrissentwurf e, Grenzstellung stellung, List<Grundrissplatte> platten, HashSet<Grenzverweis> gesetzt)
        {
            List<(Grenzverweis V, double A)> zeilen = e.Raum.Seiten
                .Where(s => s.Verweis?.Stellung == stellung && s.FlaecheM2 > PLATTE_MIN)
                .Select(s => (s.Verweis, s.FlaecheM2.Value)).ToList();
            List<int> prismen = Enumerable.Range(0, e.Polygone.Count).Where(j => e.Polygone[j].FlaecheM2 > PLATTE_MIN).ToList();
            if (zeilen.Count == 0 || prismen.Count == 0) return;
            double summeA = zeilen.Sum(z => z.A), summeP = prismen.Sum(j => e.Polygone[j].FlaecheM2);
            double faktor = Math.Min(1.0, summeP / summeA);
            var jeZeile = zeilen.Select(_ => new List<Grundrissplatte>()).ToList();
            foreach (int j in prismen)
            {
                Umrisspolygon p = e.Polygone[j];
                double z = stellung == Grenzstellung.Boden ? p.PrismaBodenM.Value : p.PrismaBodenM.Value + p.PrismaHoeheM.Value;
                double xmin = p.Punkte.Min(q => q[0]), xmax = p.Punkte.Max(q => q[0]);
                double c0 = xmin, kumuliert = 0.0;
                for (int i = 0; i < zeilen.Count; i++)
                {
                    kumuliert += zeilen[i].A * faktor * p.FlaecheM2 / summeP;
                    double c1 = i == zeilen.Count - 1 && faktor >= 1.0 - 1e-12 && Math.Abs(kumuliert - p.FlaecheM2) <= 1e-9 * p.FlaecheM2
                        ? xmax : Schnittlinie(p, xmin, xmax, kumuliert);
                    (List<double[]> aussen, List<List<double[]>> loecher) = Streifen(p, c0, c1);
                    double inhalt = Inhalt(aussen) - loecher.Sum(Inhalt);
                    if (aussen.Count >= 3 && inhalt > PLATTE_MIN)
                    {
                        List<double[]> ring = Zonenkoerper.MitStegen(aussen, loecher);
                        if (stellung == Grenzstellung.Boden) ring.Reverse();
                        jeZeile[i].Add(new Grundrissplatte
                        {
                            Verweis = zeilen[i].V, Stellung = stellung, Polygon = j,
                            EckenM = ring.Select(q => new[] { q[0], q[1], z }).ToList(), FlaecheM2 = inhalt,
                        });
                    }
                    c0 = c1;
                }
            }
            for (int i = 0; i < zeilen.Count; i++)
            {
                if (jeZeile[i].Count == 0) continue;
                platten.AddRange(jeZeile[i].Select((p, n) => (p, n)).OrderByDescending(x => Math.Round(x.p.FlaecheM2, 9)).ThenBy(x => x.n).Select(x => x.p));
                gesetzt.Add(zeilen[i].V);
            }
        }

        /// <summary>Die Schnittlinie x = c, links derer das Polygon (ohne Löcher) die Fläche <paramref name="ziel"/> trägt (Halbierung).</summary>
        private static double Schnittlinie(Umrisspolygon p, double xmin, double xmax, double ziel)
        {
            double lo = xmin, hi = xmax;
            for (int n = 0; n < 100 && hi - lo > 1e-12; n++)
            {
                double mitte = (lo + hi) / 2.0;
                (List<double[]> a, List<List<double[]>> l) = Streifen(p, xmin - 1.0, mitte);
                if (Inhalt(a) - l.Sum(Inhalt) < ziel) lo = mitte;
                else hi = mitte;
            }
            return (lo + hi) / 2.0;
        }

        /// <summary>Das Polygon samt Löchern, beschnitten auf den Streifen <paramref name="von"/> ≤ x ≤ <paramref name="bis"/>.</summary>
        private static (List<double[]> Aussen, List<List<double[]>> Loecher) Streifen(Umrisspolygon p, double von, double bis)
        {
            List<double[]> aussen = Beschneiden(Beschneiden(p.Punkte.ToList(), von, links: false), bis, links: true);
            var loecher = new List<List<double[]>>();
            foreach (IReadOnlyList<double[]> loch in p.Loecher)
            {
                List<double[]> b = Beschneiden(Beschneiden(loch.ToList(), von, links: false), bis, links: true);
                if (b.Count >= 3 && Inhalt(b) > PLATTE_MIN) loecher.Add(b);
            }
            return (aussen, loecher);
        }

        /// <summary>Sutherland–Hodgman an der Geraden x = c: <paramref name="links"/> behält x ≤ c, sonst x ≥ c.</summary>
        private static List<double[]> Beschneiden(List<double[]> ring, double c, bool links)
        {
            var aus = new List<double[]>(ring.Count + 2);
            for (int i = 0; i < ring.Count; i++)
            {
                double[] a = ring[i], b = ring[(i + 1) % ring.Count];
                bool ina = links ? a[0] <= c : a[0] >= c, inb = links ? b[0] <= c : b[0] >= c;
                if (ina) aus.Add(a);
                if (ina != inb)
                {
                    double t = (c - a[0]) / (b[0] - a[0]);
                    aus.Add(new[] { c, a[1] + t * (b[1] - a[1]) });
                }
            }
            return aus;
        }

        /// <summary>Der Betrag des Flächeninhalts eines Rings [m²].</summary>
        private static double Inhalt(IReadOnlyList<double[]> ring) => ring.Count < 3 ? 0.0 : Math.Abs(DoppelteFlaeche(ring)) / 2.0;

        /// <summary>
        /// Die gemeinsame Drehung der Prismenkanten (HC-5c) — die erste Drehung der Grundrisse, die ein Prisma tragen; <c>null</c> =
        /// keines. Weichen Grundrisse voneinander ab (nie aus EINER Quelle), gilt die erste.
        /// </summary>
        private static double? Prismendrehung(IEnumerable<Umrissentwurf> entwurf, double drehung)
        {
            foreach (Umrissentwurf e in entwurf)
                if (Prismen(e) && e.Raum.Grundrisse.Count > 0)
                    return e.Raum.Grundrisse[0].DrehungGrad ?? drehung;
            return null;
        }
    }
}
