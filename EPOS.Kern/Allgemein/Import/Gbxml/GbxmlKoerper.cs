using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Körper einer gbXML-Datei</b> (Datenaustauschkonzept 17.2 bis 17.4, Stufe K3): Raumkörper je Raum und
    /// Bauteilkörper je Fläche und Öffnung, gebildet aus den Polygonen der Datei mit dem formatfreien
    /// <see cref="Koerperbildner"/>. Abgelegt an <see cref="AbbildRaum.Koerper"/> und <see cref="AbbildBauteil.Koerper"/>
    /// mit der Quelle <see cref="Koerperquelle.AusFlaechen"/> — kein unabhängiger Beleg (17.5).
    ///
    /// <para><b>Raumkörper</b> in dieser Rangfolge: (a) <c>Space/ShellGeometry/ClosedShell</c>, jedes Schalenpolygon der
    /// <c>Surface</c> des Raums zugeordnet, deren Ebene parallel liegt und höchstens Dicke + 2 cm entfernt ist, sonst einer
    /// Ersatzkennung <c>Raum:Schale{n}</c>; (b) die <c>PlanarGeometry</c> der Flächen mit <c>AdjacentSpaceId</c> auf den
    /// Raum — eine Fläche zwischen zwei Räumen dient beiden, der Bildner richtet sie je Raum; (c) sonst kein Körper:
    /// Hat der Raum eine Schale oder Polygone, schließen sie aber nicht, steht eine benannte Meldung da, und die Ansicht
    /// zeigt das Umrissprisma.</para>
    ///
    /// <para><b>Bauteilkörper</b>: Dicke aus <c>Construction → Layer → Material/Thickness</c> (die Schichten des Aufbaus),
    /// sonst die Vorgabedicke der Bauteilart mit <see cref="Koerpervermerk.Vorgabedicke"/>. Die Richtung wird an einer
    /// Schale gemessen (17.3 Nr. 3) — Innenoberfläche, Achse, Außenmaß auf 1 cm —; ohne Schale oder ohne Treffer gilt die
    /// Achse mit <see cref="Koerpervermerk.Bezugsebene_angenommen"/>. Ein aus Flächen gebildeter Raumkörper misst nicht:
    /// er liegt in denselben Polygonen und bestätigte jede Lage. Öffnungen werden in ihre Fläche projiziert, dort als
    /// Loch mit Laibung ausgespart und selbst mittig in der Wanddicke extrudiert.</para>
    ///
    /// <para><b>Grenzen</b>: Raum- und Bauteilkörper zählen je Gebäude gegen <see cref="Dateikoerper.DREIECKSGRENZE"/>;
    /// Bauteilkörper, die sie überschritten, entfallen benannt. Reihenfolge der Datei, keine Hash-Ordnung — deterministisch.</para>
    /// </summary>
    internal static class GbxmlKoerper
    {
        /// <summary>Der Weg „Schale der Datei“ als Schlüssel in <see cref="Dateikoerper.Art"/> (17.5).</summary>
        internal const string ART_CLOSEDSHELL = "ClosedShell";

        /// <summary>Der Weg „Flächen des Raums“ als Schlüssel in <see cref="Dateikoerper.Art"/> (17.5).</summary>
        internal const string ART_RAUMFLAECHEN = "Raumflaechen";

        /// <summary>Die Lagetoleranz der Bezugsebene [m] (17.3 Nr. 3).</summary>
        internal const double LAGE_TOLERANZ_M = 0.01;

        /// <summary>Der Zuschlag auf die Dicke beim Zuordnen einer Schalenfläche zu ihrer Fläche [m] (17.4).</summary>
        internal const double ZUORDNUNG_ZUSCHLAG_M = 0.02;

        /// <summary>Die Winkeltoleranz für parallele Ebenen [°] (17.4).</summary>
        internal const double PARALLEL_GRAD = 1.0;

        private const string P = GbxmlImportProfil.MELDUNGSPRAEFIX;

        /// <summary>Die Vorgabedicke [m] je Bauteilart, wenn die Datei keine Schichtdicke nennt (17.3 Nr. 2).</summary>
        internal static double Vorgabedicke(Bauteilart art)
        {
            switch (art)
            {
                case Bauteilart.Aussenwand: return 0.30;
                case Bauteilart.Innenwand: return 0.12;
                case Bauteilart.Dach: return 0.30;
                case Bauteilart.Bodenplatte: return 0.25;
                case Bauteilart.Decke: return 0.20;
                case Bauteilart.Fenster: return 0.08;
                case Bauteilart.Tuer: return 0.06;
                case Bauteilart.Vorhangfassade: return 0.15;
                default: return 0.20;
            }
        }

        /// <summary>
        /// Bildet alle Körper des Abbilds aus der gelesenen Datei: zuerst je Gebäude die Raumkörper, dann die
        /// Bauteilkörper. Erwartet ein vom <see cref="GbxmlLeser"/> gefülltes Abbild samt der Wurzel des Dokuments.
        /// </summary>
        internal static void Bilden(GbxmlAbbild abbild, XElement wurzel)
        {
            if (abbild == null || wurzel == null) return;
            var raeume = new Dictionary<string, XElement>(StringComparer.Ordinal);
            foreach (XElement space in Kinder(wurzel, "Campus").SelectMany(c => Kinder(c, "Building")).SelectMany(b => Kinder(b, "Space")))
            {
                string id = Attr(space, "id");
                if (id != null && !raeume.ContainsKey(id)) raeume[id] = space;
            }
            var gebaut = new HashSet<AbbildBauteil>(ReferenceEqualityComparer.Instance);
            foreach (AbbildGebaeude g in abbild.Gebaeude)
                new Gebaeudelauf(abbild, g, raeume, gebaut).Bilden();
        }

        // ==================================================================
        //  Je Gebäude
        // ==================================================================

        private sealed class Gebaeudelauf
        {
            private readonly GbxmlAbbild _abbild;
            private readonly AbbildGebaeude _g;
            private readonly Dictionary<string, XElement> _spaces;
            private readonly HashSet<AbbildBauteil> _gebaut;
            private readonly Dictionary<string, Dateikoerper> _schalen = new Dictionary<string, Dateikoerper>(StringComparer.Ordinal);
            private readonly Dictionary<string, List<AbbildBauteil>> _flaechenJeRaum = new Dictionary<string, List<AbbildBauteil>>(StringComparer.Ordinal);
            private int _dreiecke, _ausgelassen;

            internal Gebaeudelauf(GbxmlAbbild abbild, AbbildGebaeude g, Dictionary<string, XElement> spaces, HashSet<AbbildBauteil> gebaut)
            {
                _abbild = abbild;
                _g = g;
                _spaces = spaces;
                _gebaut = gebaut;
            }

            internal void Bilden()
            {
                foreach (AbbildBauteil b in _g.Bauteile)
                    foreach (string raum in b.Nachbarn.Select(n => n.Kennung).Where(k => k != null).Distinct(StringComparer.Ordinal))
                    {
                        if (!_flaechenJeRaum.TryGetValue(raum, out List<AbbildBauteil> l)) _flaechenJeRaum[raum] = l = new List<AbbildBauteil>();
                        l.Add(b);
                    }
                foreach (AbbildRaum r in _g.Raeume) Raum(r);
                _dreiecke = _g.Raeume.Sum(r => r.Koerper?.DreieckZahl ?? 0);
                foreach (AbbildBauteil b in _g.Bauteile)
                {
                    if (!_gebaut.Add(b)) continue;
                    Bauteil(b);
                }
                if (_ausgelassen > 0)
                    _g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPER_GRENZE", Ganz(_ausgelassen),
                        Ganz(Dateikoerper.DREIECKSGRENZE), Ganz(_dreiecke)));
            }

            // --------------------------------------------------------------
            //  Raumkörper (17.2)
            // --------------------------------------------------------------

            private void Raum(AbbildRaum r)
            {
                if (string.IsNullOrEmpty(r.Kennung)) return;
                _flaechenJeRaum.TryGetValue(r.Kennung, out List<AbbildBauteil> flaechen);
                flaechen ??= new List<AbbildBauteil>();

                List<List<double[]>> schale = _spaces.TryGetValue(r.Kennung, out XElement space) ? Schale(space) : null;
                if (schale != null && schale.Count > 0)
                {
                    var quellen = new List<Quellflaeche>();
                    for (int i = 0; i < schale.Count; i++)
                        quellen.Add(new Quellflaeche { Kennung = Zuordnen(schale[i], flaechen) ?? r.Kennung + ":Schale" + Ganz(i), Aussen = schale[i] });
                    Koerperergebnis e = Koerperbildner.Huelle(quellen, ART_CLOSEDSHELL);
                    if (e.Gebildet)
                    {
                        r.Koerper = e.Koerper;
                        _schalen[r.Kennung] = e.Koerper;
                        return;
                    }
                    Rueckfall(r.Kennung, ART_CLOSEDSHELL, e);
                }

                List<AbbildBauteil> mitPolygon = flaechen.Where(b => b.RandpunkteM != null && b.RandpunkteM.Count >= 3).ToList();
                if (mitPolygon.Count == 0) return;
                Koerperergebnis f = Koerperbildner.Huelle(
                    mitPolygon.Select(b => new Quellflaeche { Kennung = b.Kennung, Aussen = b.RandpunkteM }).ToList(), ART_RAUMFLAECHEN);
                if (f.Gebildet) r.Koerper = f.Koerper;
                else Rueckfall(r.Kennung, ART_RAUMFLAECHEN, f);
            }

            private void Rueckfall(string raum, string weg, Koerperergebnis e)
            {
                if (e.Grund == Koerperbildner.GRUND_NICHT_GESCHLOSSEN)
                    _g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPER_NICHT_GESCHLOSSEN", raum, weg, Ganz(e.OffeneKanten)));
                else
                    _g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPER_NICHT_GEBILDET", raum, weg, e.Grund));
            }

            /// <summary>Die Fläche des Raums, in deren Ebene (parallel, Abstand ≤ Dicke + 2 cm) das Schalenpolygon liegt; <c>null</c> = keine.</summary>
            private static string Zuordnen(List<double[]> polygon, List<AbbildBauteil> flaechen)
            {
                double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(polygon));
                if (n == null) return null;
                double[] c = Mitte(polygon);
                string beste = null;
                double besterAbstand = double.MaxValue;
                foreach (AbbildBauteil b in flaechen)
                {
                    if (b.RandpunkteM == null || b.RandpunkteM.Count < 3) continue;
                    double[] m = Polygonnetz.Normiert(Polygonnetz.Newell(b.RandpunkteM));
                    if (m == null || Math.Abs(Polygonnetz.Punkt(n, m)) < Math.Cos(PARALLEL_GRAD * Math.PI / 180.0)) continue;
                    double abstand = Math.Abs(Polygonnetz.Punkt(Polygonnetz.Minus(c, b.RandpunkteM[0]), m));
                    if (abstand > Dicke(b).Wert + ZUORDNUNG_ZUSCHLAG_M) continue;
                    double[] projiziert = Polygonnetz.Minus(c, Polygonnetz.Mal(m, Polygonnetz.Punkt(Polygonnetz.Minus(c, b.RandpunkteM[0]), m)));
                    if (!ImPolygon(projiziert, b.RandpunkteM, m, TOLERANZ_RAND_M)) continue;
                    if (abstand < besterAbstand - 1e-9) { besterAbstand = abstand; beste = b.Kennung; }
                }
                return beste;
            }

            // --------------------------------------------------------------
            //  Bauteilkörper (17.3)
            // --------------------------------------------------------------

            private void Bauteil(AbbildBauteil b)
            {
                if (b.RandpunkteM == null || b.RandpunkteM.Count < 3)
                {
                    foreach (AbbildBauteil o in b.Oeffnungen) Oeffnung(o, null, 0.0, null, 0.0);
                    return;
                }
                (double dicke, bool vorgabe) = Dicke(b);
                (Extrusionsrichtung richtung, double[] zumRaum, bool angenommen) = Richtung(b, dicke);

                double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(b.RandpunkteM));
                var loecher = new List<IReadOnlyList<double[]>>();
                var lochkennungen = new List<string>();
                var mitLoch = new List<(AbbildBauteil Oeffnung, List<double[]> Ring)>();
                foreach (AbbildBauteil o in b.Oeffnungen)
                {
                    if (o.RandpunkteM == null || o.RandpunkteM.Count < 3) continue;
                    List<double[]> ring = n == null ? null : Projiziert(o.RandpunkteM, b.RandpunkteM[0], n);
                    if (ring != null && ring.All(p => ImPolygon(p, b.RandpunkteM, n, TOLERANZ_RAND_M)))
                    {
                        loecher.Add(ring);
                        lochkennungen.Add(o.Kennung);
                        mitLoch.Add((o, ring));
                    }
                    else
                    {
                        o.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "OEFFNUNG_OHNE_WAND", o.Kennung, b.Kennung));
                        Oeffnung(o, null, 0.0, null, 0.0);
                    }
                }

                Koerperergebnis e = Koerperbildner.Extrusion(
                    new Quellflaeche { Kennung = b.Kennung, Aussen = b.RandpunkteM, Loecher = loecher, Lochkennungen = lochkennungen },
                    dicke, richtung, zumRaum);
                if (e.Gebildet)
                    Ablegen(b, MitVermerken(e.Koerper, vorgabe, angenommen));
                else
                    b.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPER_NICHT_GEBILDET", b.Kennung, Koerperbildner.ART_FLAECHENEXTRUSION, e.Grund));

                // Die Mitte der Wanddicke: um so viel liegt sie von der Fläche zur Raumseite hin.
                double mitte = richtung switch
                {
                    Extrusionsrichtung.NachInnen => dicke / 2.0,
                    Extrusionsrichtung.NachAussen => -dicke / 2.0,
                    _ => 0.0,
                };
                double[] raum = n == null ? null : (zumRaum != null && Polygonnetz.Punkt(zumRaum, n) > 0.0 ? n : Polygonnetz.Mal(n, -1.0));
                foreach ((AbbildBauteil o, List<double[]> ring) in mitLoch) Oeffnung(o, ring, dicke, raum, mitte);
            }

            /// <summary>Der Öffnungskörper: mit seiner Dicke (höchstens der Wanddicke) mittig in der Wand, sonst in seiner eigenen Ebene.</summary>
            private void Oeffnung(AbbildBauteil o, List<double[]> ring, double wanddicke, double[] raum, double mitte)
            {
                if (!_gebaut.Add(o)) return;
                ring ??= o.RandpunkteM?.ToList();
                if (ring == null || ring.Count < 3) return;
                (double dicke, bool vorgabe) = Dicke(o);
                bool angenommen = raum == null;
                if (wanddicke > 0.0) dicke = Math.Min(dicke, wanddicke);
                if (raum != null) ring = ring.Select(p => Polygonnetz.Plus(p, Polygonnetz.Mal(raum, mitte))).ToList();
                Koerperergebnis e = Koerperbildner.Extrusion(new Quellflaeche { Kennung = o.Kennung, Aussen = ring }, dicke, Extrusionsrichtung.Beidseitig, raum);
                if (e.Gebildet) Ablegen(o, MitVermerken(e.Koerper, vorgabe, angenommen));
                else o.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPER_NICHT_GEBILDET", o.Kennung, Koerperbildner.ART_FLAECHENEXTRUSION, e.Grund));
            }

            private void Ablegen(AbbildBauteil b, Dateikoerper k)
            {
                if (_dreiecke + k.DreieckZahl > Dateikoerper.DREIECKSGRENZE) { _ausgelassen++; return; }
                b.Koerper = k;
                _dreiecke += k.DreieckZahl;
            }

            /// <summary>
            /// Die Extrusionsrichtung, gemessen an der Schale eines Nachbarraums (17.3 Nr. 3): der nächste Treffer der
            /// Flächennormale durch die Flächenmitte auf eine parallele Schalenfläche; Abstand ≤ 1 cm Innenoberfläche,
            /// halbe Dicke Achse, ganze Dicke Außenmaß. Ohne Treffer die Achse, angenommen.
            /// </summary>
            private (Extrusionsrichtung Richtung, double[] ZumRaum, bool Angenommen) Richtung(AbbildBauteil b, double dicke)
            {
                double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(b.RandpunkteM));
                if (n != null)
                {
                    double[] c = Mitte(b.RandpunkteM);
                    foreach (string raum in b.Nachbarn.Select(x => x.Kennung).Where(k => k != null).Distinct(StringComparer.Ordinal))
                    {
                        if (!_schalen.TryGetValue(raum, out Dateikoerper schale)) continue;
                        (double abstand, double[] aussen)? treffer = Treffer(schale, c, n);
                        if (!treffer.HasValue) continue;
                        double d = treffer.Value.abstand;
                        double[] zumRaum = Polygonnetz.Mal(treffer.Value.aussen, -1.0);
                        if (d <= LAGE_TOLERANZ_M) return (Extrusionsrichtung.NachAussen, zumRaum, false);
                        if (Math.Abs(d - dicke / 2.0) <= LAGE_TOLERANZ_M) return (Extrusionsrichtung.Beidseitig, zumRaum, false);
                        if (Math.Abs(d - dicke) <= LAGE_TOLERANZ_M) return (Extrusionsrichtung.NachInnen, zumRaum, false);
                    }
                }
                return (Extrusionsrichtung.Beidseitig, null, true);
            }
        }

        // ==================================================================
        //  Geometrie
        // ==================================================================

        /// <summary>Die Randtoleranz beim Prüfen, ob ein Punkt in einem Polygon liegt [m].</summary>
        private const double TOLERANZ_RAND_M = 0.001;

        /// <summary>Die Dicke der Fläche: Summe der Schichtdicken des Aufbaus, sonst die Vorgabedicke (mit Kennzeichen).</summary>
        private static (double Wert, bool Vorgabe) Dicke(AbbildBauteil b)
        {
            double summe = b.Aufbau?.Schichten.Where(s => s.DickeM > 0.0).Sum(s => s.DickeM.Value) ?? 0.0;
            return summe > 0.0 ? (summe, false) : (Vorgabedicke(b.Art), true);
        }

        /// <summary>Der nächste Treffer der Geraden c + s·n auf ein paralleles Dreieck der Schale: |s| und die Normale des Dreiecks.</summary>
        private static (double, double[])? Treffer(Dateikoerper schale, double[] c, double[] n)
        {
            double grenze = Math.Cos(PARALLEL_GRAD * Math.PI / 180.0);
            (double, double[])? beste = null;
            for (int i = 0; i < schale.Dreiecke.Count; i++)
            {
                double[] nt = schale.Normalen[i];
                double cos = Polygonnetz.Punkt(nt, n);
                if (Math.Abs(cos) < grenze) continue;
                int[] d = schale.Dreiecke[i];
                double[] a = schale.PunkteM[d[0]], bb = schale.PunkteM[d[1]], cc = schale.PunkteM[d[2]];
                double s = Polygonnetz.Punkt(Polygonnetz.Minus(a, c), nt) / cos;
                double[] h = Polygonnetz.Plus(c, Polygonnetz.Mal(n, s));
                if (!ImDreieck(h, a, bb, cc, nt)) continue;
                if (!beste.HasValue || Math.Abs(s) < beste.Value.Item1 - 1e-9) beste = (Math.Abs(s), nt);
            }
            return beste;
        }

        private static bool ImDreieck(double[] p, double[] a, double[] b, double[] c, double[] n)
        {
            bool Seite(double[] u, double[] v)
            {
                double[] kante = Polygonnetz.Minus(v, u);
                double laenge = Math.Sqrt(Polygonnetz.Punkt(kante, kante));
                if (laenge <= 0.0) return true;
                return Polygonnetz.Punkt(Polygonnetz.Kreuz(kante, Polygonnetz.Minus(p, u)), n) / laenge >= -TOLERANZ_RAND_M;
            }
            return Seite(a, b) && Seite(b, c) && Seite(c, a);
        }

        /// <summary>Liegt der Punkt (in der Ebene mit Normale <paramref name="n"/>) im Polygon oder auf seinem Rand?</summary>
        private static bool ImPolygon(double[] p, IReadOnlyList<double[]> polygon, double[] n, double toleranz)
        {
            (double[] u, double[] v) = Basis(n);
            double px = Polygonnetz.Punkt(p, u), py = Polygonnetz.Punkt(p, v);
            int k = polygon.Count;
            bool innen = false;
            for (int i = 0, j = k - 1; i < k; j = i++)
            {
                double xi = Polygonnetz.Punkt(polygon[i], u), yi = Polygonnetz.Punkt(polygon[i], v);
                double xj = Polygonnetz.Punkt(polygon[j], u), yj = Polygonnetz.Punkt(polygon[j], v);
                if (AbstandStrecke(px, py, xi, yi, xj, yj) <= toleranz) return true;
                if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) innen = !innen;
            }
            return innen;
        }

        private static double AbstandStrecke(double px, double py, double ax, double ay, double bx, double by)
        {
            double dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy;
            double t = l2 <= 0.0 ? 0.0 : Math.Max(0.0, Math.Min(1.0, ((px - ax) * dx + (py - ay) * dy) / l2));
            double qx = ax + t * dx - px, qy = ay + t * dy - py;
            return Math.Sqrt(qx * qx + qy * qy);
        }

        private static (double[] U, double[] V) Basis(double[] n)
        {
            double[] hilf = Math.Abs(n[2]) < 0.9 ? new[] { 0.0, 0.0, 1.0 } : new[] { 1.0, 0.0, 0.0 };
            double[] u = Polygonnetz.Normiert(Polygonnetz.Kreuz(hilf, n));
            return (u, Polygonnetz.Kreuz(n, u));
        }

        private static List<double[]> Projiziert(IReadOnlyList<double[]> ring, double[] ursprung, double[] n)
            => ring.Select(p => Polygonnetz.Minus(p, Polygonnetz.Mal(n, Polygonnetz.Punkt(Polygonnetz.Minus(p, ursprung), n)))).ToList();

        /// <summary>Der Flächenschwerpunkt (Fächer vom ersten Punkt, flächengewichtet); bei entarteten Polygonen das Mittel der Punkte.</summary>
        private static double[] Mitte(IReadOnlyList<double[]> p)
        {
            double[] n = Polygonnetz.Newell(p);
            double gesamt = 0.0;
            var s = new double[3];
            for (int i = 1; i + 1 < p.Count; i++)
            {
                double w = Polygonnetz.Punkt(Polygonnetz.Kreuz(Polygonnetz.Minus(p[i], p[0]), Polygonnetz.Minus(p[i + 1], p[0])), n);
                for (int k = 0; k < 3; k++) s[k] += w * (p[0][k] + p[i][k] + p[i + 1][k]) / 3.0;
                gesamt += w;
            }
            if (Math.Abs(gesamt) > 1e-12) return new[] { s[0] / gesamt, s[1] / gesamt, s[2] / gesamt };
            return new[] { p.Average(q => q[0]), p.Average(q => q[1]), p.Average(q => q[2]) };
        }

        /// <summary>Die Polygone der Schale (<c>ShellGeometry/ClosedShell/PolyLoop</c>) in Metern; <c>null</c> = keine Schale.</summary>
        private static List<List<double[]>> Schale(XElement space)
        {
            XElement shell = Kinder(space, "ShellGeometry").SelectMany(s => Kinder(s, "ClosedShell")).FirstOrDefault();
            if (shell == null) return null;
            string global = space.Document?.Root != null ? Attr(space.Document.Root, "lengthUnit") : null;
            var polygone = new List<List<double[]>>();
            foreach (XElement loop in Kinder(shell, "PolyLoop"))
            {
                var punkte = new List<double[]>();
                foreach (XElement cp in Kinder(loop, "CartesianPoint"))
                {
                    List<XElement> k = Kinder(cp, "Coordinate").ToList();
                    if (k.Count < 3) { punkte = null; break; }
                    double? x = Laenge(k[0], global), y = Laenge(k[1], global), z = Laenge(k[2], global);
                    if (!x.HasValue || !y.HasValue || !z.HasValue) { punkte = null; break; }
                    punkte.Add(new[] { x.Value, y.Value, z.Value });
                }
                if (punkte == null) continue;
                List<double[]> ring = Polygonnetz.Bereinigt(punkte);
                if (ring.Count >= 3) polygone.Add(ring);
            }
            return polygone;
        }

        private static double? Laenge(XElement e, string global)
        {
            string t = e.Value?.Trim();
            if (string.IsNullOrEmpty(t) || !double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double w)
                || double.IsNaN(w) || double.IsInfinity(w))
                return null;
            string einheit = Attr(e, "unit") ?? global;
            return einheit == null ? null : GbxmlEinheiten.Laenge(w, einheit);
        }

        /// <summary>Der Körper mit den Vermerken der Dicke und der Bezugsebene, sonst unverändert.</summary>
        private static Dateikoerper MitVermerken(Dateikoerper k, bool vorgabedicke, bool angenommen)
        {
            if (!vorgabedicke && !angenommen) return k;
            var vermerke = new SortedSet<Koerpervermerk>(k.Vermerke);
            if (vorgabedicke) vermerke.Add(Koerpervermerk.Vorgabedicke);
            if (angenommen) vermerke.Add(Koerpervermerk.Bezugsebene_angenommen);
            return new Dateikoerper
            {
                PunkteM = k.PunkteM, Dreiecke = k.Dreiecke, Normalen = k.Normalen, Randkanten = k.Randkanten,
                Art = k.Art, Vermerke = vermerke.ToList(), Quelle = k.Quelle, Quellflaechen = k.Quellflaechen,
            };
        }

        private static IEnumerable<XElement> Kinder(XElement e, string name)
            => e?.Elements().Where(x => x.Name.LocalName == name) ?? Enumerable.Empty<XElement>();

        private static string Attr(XElement e, string name)
            => e?.Attributes().FirstOrDefault(a => a.Name.LocalName == name)?.Value;

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
