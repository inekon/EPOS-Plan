using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Stück Bauteilfläche aus Sicht eines Raums</b> (Abstimmung G5, Teil G5-3): die Überlappung einer ebenen Fläche
    /// des Raumkörpers mit einer Seite des Bauteilkörpers — mit dem Raum auf der anderen Seite oder ohne ihn.
    /// </summary>
    internal sealed class Koerperflaechenstueck
    {
        /// <summary>Die Stelle des Raums in der Eingabe.</summary>
        internal int Raum { get; init; }

        /// <summary>Die Stelle des Raums auf der anderen Seite des Bauteils; −1 = keiner.</summary>
        internal int Gegenraum { get; init; } = -1;

        /// <summary>Der Schlüssel des Stücks im Bauteil (eindeutig je Bauteil).</summary>
        internal string Schluessel { get; init; } = "";

        /// <summary>Der Schlüssel des Gegenstücks (das Stück des Gegenraums); <c>null</c> = keins.</summary>
        internal string Gegenschluessel { get; init; }

        /// <summary>Die Fläche [m²] aus Sicht des Raums.</summary>
        internal double FlaecheM2 { get; init; }

        /// <summary>Die Außennormale aus Sicht des Raums (vom Raum weg, ins Bauteil), Einheitsvektor.</summary>
        internal double[] Normale { get; init; }

        /// <summary>Der Flächenschwerpunkt [m] in der Ebene der Raumfläche.</summary>
        internal double[] SchwerpunktM { get; init; }

        /// <summary>Was auf der anderen Seite liegt: <see cref="Randbedingung.Innen"/> (Gegenraum), sonst Außenluft, Erdreich oder unbeheizt.</summary>
        internal Randbedingung Lage { get; init; }

        /// <summary>Die Stelle der Bauteilseite (ebene Fläche des Bauteilkörpers), an der das Stück liegt.</summary>
        internal int Seite { get; init; }
    }

    /// <summary>Was ein Raum für die Zuordnung mitbringt (<see cref="Koerperflaechen.Zuordnen"/>).</summary>
    internal sealed class Koerperflaechenraum
    {
        /// <summary>Der Körper des Raums [m]; <c>null</c> = keiner (trägt nichts bei).</summary>
        internal Dateikoerper Koerper { get; init; }

        /// <summary>Liegt der Raum unter der Geländehöhe (Geschosslage unter 0 oder ganz unter der Bezugsebene)?</summary>
        internal bool Unterirdisch { get; init; }

        /// <summary>Liegt der Raum im untersten Geschoss (sein Boden ohne Raum darunter liegt am Erdreich)?</summary>
        internal bool Unterster { get; init; }
    }

    /// <summary>Das Ergebnis eines Bauteils: seine Stücke je Raum und die Gegenprobe.</summary>
    internal sealed class Koerperflaechenergebnis
    {
        /// <summary>Die Stücke in fester Reihenfolge (Bauteilseite, Raum, Gegenraum).</summary>
        internal List<Koerperflaechenstueck> Stuecke { get; } = new List<Koerperflaechenstueck>();

        /// <summary>Die Summe der Stücke der stärker belegten Seite des Bauteils [m²] — Gegenprobe gegen die Körperfläche.</summary>
        internal double SeiteM2 { get; init; }

        internal List<(int Seite, int Raum, double[] N, double S, double Abstand, List<double[][]> Stuecke)> Treffer { get; }
            = new List<(int, int, double[], double, double, List<double[][]>)>();

        /// <summary>
        /// Die Räume, in deren überlappendem Teil der Punkt <paramref name="punktM"/> liegt (die Mitte einer Öffnung): Der Punkt
        /// liegt höchstens <paramref name="abstandMaxM"/> vor oder hinter der Bauteilseite, seine Projektion in einem Stück der
        /// Überlappung. Leer = keiner.
        /// </summary>
        internal List<int> RaeumeAn(double[] punktM, double abstandMaxM)
        {
            var raeume = new List<int>();
            if (punktM == null) return raeume;
            foreach (var t in Treffer)
            {
                if (raeume.Contains(t.Raum)) continue;
                if (Math.Abs(Koerperflaechen.Punkt(t.N, punktM) - t.S) > abstandMaxM) continue;
                (double[] u, double[] v) = Koerperflaechen.Basis(t.N);
                double[] p = { Koerperflaechen.Punkt(punktM, u), Koerperflaechen.Punkt(punktM, v) };
                if (t.Stuecke.Any(s => Koerperflaechen.Enthaelt(Koerperflaechen.Eben(s, u, v), p))) raeume.Add(t.Raum);
            }
            return raeume;
        }

        /// <summary>
        /// <b>Die Anteile einer Öffnung je Raum</b> nach ihrer Überlappung mit den Stücken (Abstimmung G5, Teil G5-3): Je
        /// Bauteilseite, die der Öffnungskörper <paramref name="oeffnung"/> bis <paramref name="abstandMaxM"/> erreicht, wird die
        /// Fläche seiner zur Seite gleichläufigen Flächen (Normale bis <see cref="Koerperflaechen.PARALLEL_GRAD"/>; sonst der
        /// gegenläufigen) mit den Stücken jedes Raums geschnitten; der Anteil eines Raums ist sein Schnitt durch die Summe der
        /// Schnitte an dieser Seite — Räume auf derselben Seite teilen die Öffnung, Räume auf beiden Seiten (eine Tür in einer
        /// Innenwand) sehen sie ganz. Dazu die Außennormale der Seite aus Sicht des Raums. Leer = kein Schnitt (dann gilt die
        /// Lage der Mitte, <see cref="RaeumeAn"/>).
        /// </summary>
        internal List<(int Raum, double Anteil, double[] Normale)> Anteile(Dateikoerper oeffnung, double abstandMaxM)
        {
            var ergebnis = new List<(int Raum, double Anteil, double[] Normale)>();
            if (oeffnung == null || oeffnung.Dreiecke.Count == 0) return ergebnis;
            double cosMax = Math.Cos(Koerperflaechen.PARALLEL_GRAD * Math.PI / 180.0);
            var dreiecke = new List<(double[][] D, double[] N)>();
            foreach (int[] d in oeffnung.Dreiecke)
            {
                double[] a = oeffnung.PunkteM[d[0]], b = oeffnung.PunkteM[d[1]], c = oeffnung.PunkteM[d[2]];
                double[] n = Koerperflaechen.Normale(a, b, c);
                if (n != null) dreiecke.Add((new[] { a, b, c }, n));
            }
            var jeSeite = new SortedDictionary<int, List<(int Raum, double Flaeche, double[] N)>>();
            foreach (var t in Treffer)
            {
                double tiefMin = oeffnung.PunkteM.Min(p => Koerperflaechen.Punkt(t.N, p)), tiefMax = oeffnung.PunkteM.Max(p => Koerperflaechen.Punkt(t.N, p));
                if (tiefMin > t.S + abstandMaxM || tiefMax < t.S - abstandMaxM) continue;
                List<double[][]> gleich = dreiecke.Where(x => Koerperflaechen.Punkt(x.N, t.N) > cosMax).Select(x => x.D).ToList();
                if (gleich.Count == 0) gleich = dreiecke.Where(x => Koerperflaechen.Punkt(x.N, t.N) < -cosMax).Select(x => x.D).ToList();
                if (gleich.Count == 0) continue;
                double f = Koerperflaechen.Ueberdeckung(gleich, t.Stuecke, t.N, out _);
                if (f < Koerperflaechen.FLAECHE_MIN_M2) continue;
                if (!jeSeite.TryGetValue(t.Seite, out var liste)) jeSeite[t.Seite] = liste = new List<(int, double, double[])>();
                liste.Add((t.Raum, f, t.N));
            }
            foreach (List<(int Raum, double Flaeche, double[] N)> liste in jeSeite.Values)
            {
                double summe = liste.Sum(x => x.Flaeche);
                foreach ((int raum, double f, double[] n) in liste)
                {
                    double anteil = f / summe;
                    int i = ergebnis.FindIndex(x => x.Raum == raum);
                    if (i >= 0 && ergebnis[i].Anteil >= anteil) continue;
                    var eintrag = (raum, anteil, n.Select(x => Math.Round(-x, 6) + 0.0).ToArray());
                    if (i >= 0) ergebnis[i] = eintrag;
                    else ergebnis.Add(eintrag);
                }
            }
            return ergebnis;
        }
    }

    /// <summary>
    /// <b>Bauteilflächen je Raum aus den Körpern</b> (Abstimmung G5, Teil G5-3, Anforderungen A1–A4): Liefert eine Datei weder
    /// Raumgrenzen noch Raumbezüge, ordnet der Import die Bauteile über ihre Körper den Räumen zu — ohne Geometriekern, mit
    /// den ebenen Polygonen wie <see cref="Koerpernachbarschaft"/>.
    ///
    /// <list type="number">
    /// <item><b>Ebenen:</b> Raum- und Bauteilkörper zerfallen in ebene Flächen (Dreiecke gleicher Normale und gleichen
    /// Ebenenabstands, Rundung 1e-3). Am Bauteil entfallen die Stirnflächen: Flächen, deren Breite (Fläche ÷ Diagonale)
    /// höchstens das 1,5-fache der Dicke ist (wie <see cref="IfcBauteilkoerper"/>).</item>
    /// <item><b>Ebenenabgleich:</b> Eine Fläche des Raums fällt mit einer Seite des Bauteils zusammen, wenn ihre Normalen bis
    /// <see cref="PARALLEL_GRAD"/> gegenläufig sind und die Mitte der Raumfläche höchstens die halbe Bauteildicke +
    /// <see cref="TOLERANZ_M"/> vor oder hinter der Seite liegt (Räume bis an die Wandoberfläche, bis zur Achse oder mit einer
    /// Fuge gezeichnet).</item>
    /// <item><b>Polygonschnitt:</b> Die Fläche des Stücks ist die Überlappung in der Ebene der Seite — die Summe der Schnitte
    /// der Dreiecke beider Flächen (konvex gegen konvex, Sutherland–Hodgman), exakt für jede Form; unter
    /// <see cref="FLAECHE_MIN_M2"/> entfällt sie.</item>
    /// <item><b>Gegenraum:</b> Liegt auf der Gegenseite des Bauteils (gegenläufige Seite desselben Körpers) ein anderer Raum,
    /// wird die Überlappung beider Stücke in der Ebene geschnitten: das ist die Trennfläche der beiden Räume (Gegenstücke
    /// wechselseitig, <see cref="Randbedingung.Innen"/>). Der Rest eines Stücks ohne Gegenraum geht bei einem inneren Bauteil
    /// anteilig an seine Trennflächen (etwa der Streifen einer Decke unter einer Innenwand), sonst ist er ein eigenes Stück
    /// gegen außen.</item>
    /// <item><b>Randbedingung ohne Gegenraum:</b> Erdreich für Wände und Böden eines unterirdischen Raums und für den Boden
    /// eines Raums im untersten Geschoss; sonst unbeheizt bei einem inneren Bauteil (die Datei nennt es innen, der Raum
    /// dahinter fehlt), sonst Außenluft.</item>
    /// </list>
    /// <para><b>Deterministisch:</b> Bauteilseiten in der Reihenfolge ihres ersten Dreiecks, Räume in der Reihenfolge der
    /// Eingabe; Punkte auf 1e-6 gerundet.</para>
    /// </summary>
    internal static class Koerperflaechen
    {
        /// <summary>Größte Abweichung der Normalen von der Gegenrichtung [°].</summary>
        internal const double PARALLEL_GRAD = 5.0;

        /// <summary>Zuschlag zur halben Bauteildicke für den Abstand der Raumfläche zur Bauteilseite [m].</summary>
        internal const double TOLERANZ_M = 0.05;

        /// <summary>Kleinste Fläche eines Stücks [m²]; kleinere Schnitte entfallen.</summary>
        internal const double FLAECHE_MIN_M2 = 1e-4;

        /// <summary>Ohne Dicke der Datei und ohne Gegenseite: größter Abstand zweier Seiten eines Bauteils [m].</summary>
        private const double DICKE_MAX_M = Koerpernachbarschaft.TRENNDICKE_MAX_M;

        /// <summary>Die Stirnfläche ist höchstens so viele Dicken breit.</summary>
        private const double STIRN_DICKEN = 1.5;

        /// <summary>|n<sub>z</sub>| ab dieser Grenze ist eine Fläche Boden bzw. Decke (Neigung bis 45°), darunter Wand.</summary>
        private const double WAAGERECHT_NZ = 0.7071;

        private const double EPS = 1e-12;

        private sealed class Ebene
        {
            internal double[] N;
            internal double S;
            internal double Flaeche;
            internal readonly List<double[][]> Dreiecke = new List<double[][]>();
            internal readonly double[] Min = { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity };
            internal readonly double[] Max = { double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity };
        }

        /// <summary>
        /// Ordnet den Bauteilkörper (ein oder mehrere Körper, etwa die Platten eines Dachs) den Räumen zu; leer = kein Raum
        /// liegt an.
        /// </summary>
        /// <param name="raeume">Die Räume des Gebäudes in fester Reihenfolge.</param>
        /// <param name="bauteil">Die Körper des Bauteils [m].</param>
        /// <param name="dickeM">Die Dicke der Datei [m]; <c>null</c> = aus dem Körper (kleinster Abstand gegenläufiger Seiten).</param>
        /// <param name="innen">Ist das Bauteil innen (die Datei nennt es nicht außen)? Bestimmt den Rest ohne Gegenraum.</param>
        internal static Koerperflaechenergebnis Zuordnen(IReadOnlyList<Koerperflaechenraum> raeume, IReadOnlyList<Dateikoerper> bauteil,
                                                       double? dickeM, bool innen)
        {
            var seiten = new List<Ebene>();
            foreach (Dateikoerper k in bauteil ?? Array.Empty<Dateikoerper>())
                if (k != null) seiten.AddRange(Ebenen(k));
            if (seiten.Count == 0 || raeume == null) return new Koerperflaechenergebnis();

            double dicke = dickeM > 0.0 ? dickeM.Value : Dicke(seiten);
            if (dicke > 0.0)
            {
                List<Ebene> ohne = seiten.Where(e => Breite(e) > STIRN_DICKEN * dicke + 1e-9).ToList();
                if (ohne.Count > 0) seiten = ohne;
            }
            double[] bMin = Min(seiten), bMax = Max(seiten);
            double fenster = (dicke > 0.0 ? dicke : DICKE_MAX_M) + 2.0 * TOLERANZ_M;
            double cosMax = Math.Cos(PARALLEL_GRAD * Math.PI / 180.0);

            // Treffer je (Seite, Raum): die Stücke der Überlappung in der Ebene der Seite.
            var treffer = new SortedDictionary<(int Seite, int Raum), (double Abstand, List<double[][]> Stuecke)>();
            for (int r = 0; r < raeume.Count; r++)
            {
                Dateikoerper rk = raeume[r]?.Koerper;
                if (rk == null || rk.Dreiecke.Count == 0) continue;
                List<Ebene> flaechen = Ebenen(rk);
                if (flaechen.Count == 0 || !Ueberlappt3(Min(flaechen), Max(flaechen), bMin, bMax, fenster)) continue;
                for (int s = 0; s < seiten.Count; s++)
                {
                    Ebene f = seiten[s];
                    foreach (Ebene rf in flaechen)
                    {
                        if (Punkt(rf.N, f.N) > -cosMax) continue;
                        if (!Ueberlappt3(rf.Min, rf.Max, f.Min, f.Max, fenster)) continue;
                        // Abstand der Raumfläche (Mitte) zur Seite, davor (> 0) oder in der Wand (< 0): höchstens halbe Dicke + Toleranz.
                        double t = Punkt(f.N, Mitte(rf)) - f.S;
                        if (Math.Abs(t) > dicke / 2.0 + TOLERANZ_M) continue;
                        List<double[][]> stuecke = Schnitt(f, rf);
                        if (stuecke.Count == 0) continue;
                        if (!treffer.TryGetValue((s, r), out var alt)) treffer[(s, r)] = alt = (t, new List<double[][]>());
                        alt.Stuecke.AddRange(stuecke);
                    }
                }
            }
            var ergebnisTreffer = new List<(int Seite, int Raum, double[] N, double S, double Abstand, List<double[][]> Stuecke)>();
            foreach (KeyValuePair<(int Seite, int Raum), (double Abstand, List<double[][]> Stuecke)> t in treffer)
                if (Summe(t.Value.Stuecke, seiten[t.Key.Seite].N) >= FLAECHE_MIN_M2)
                    ergebnisTreffer.Add((t.Key.Seite, t.Key.Raum, seiten[t.Key.Seite].N, seiten[t.Key.Seite].S, t.Value.Abstand, t.Value.Stuecke));

            var stueckliste = new List<Koerperflaechenstueck>();
            foreach (var h in ergebnisTreffer)
            {
                Ebene f = seiten[h.Seite];
                double[] n = f.N.Select(x => -x).ToArray();
                double flaeche = Summe(h.Stuecke, f.N);
                // Die Gegenräume: Treffer an gegenläufigen Seiten desselben Bauteils, höchstens die Dicke dahinter.
                var partner = new List<(int Seite, int Raum, double Flaeche, double[] Schwerpunkt)>();
                foreach (var k in ergebnisTreffer)
                {
                    if (k.Raum == h.Raum || Punkt(seiten[k.Seite].N, f.N) > -cosMax) continue;
                    double tiefe = f.S - Punkt(f.N, Mitte(seiten[k.Seite]));
                    if (tiefe < -TOLERANZ_M || tiefe > fenster) continue;
                    double i = Ueberdeckung(h.Stuecke, k.Stuecke, f.N, out double[] sp);
                    if (i >= FLAECHE_MIN_M2) partner.Add((k.Seite, k.Raum, i, sp));
                }
                double summe = partner.Sum(p => p.Flaeche);
                double rest = flaeche - summe;
                bool verteilen = summe > 0.0 && (innen || rest < FLAECHE_MIN_M2);
                foreach (var p in partner)
                    stueckliste.Add(new Koerperflaechenstueck
                    {
                        Raum = h.Raum, Gegenraum = p.Raum, Seite = h.Seite,
                        Schluessel = Schluessel(h.Seite, h.Raum, p.Seite, p.Raum), Gegenschluessel = Schluessel(p.Seite, p.Raum, h.Seite, h.Raum),
                        FlaecheM2 = R(verteilen ? flaeche * p.Flaeche / summe : p.Flaeche),
                        Normale = n.Select(R).ToArray(), SchwerpunktM = Verschoben(p.Schwerpunkt, f.N, h.Abstand),
                        Lage = Randbedingung.Innen,
                    });
                if (verteilen || rest < FLAECHE_MIN_M2) continue;
                Koerperflaechenraum raum = raeume[h.Raum];
                bool boden = n[2] <= -WAAGERECHT_NZ, wand = Math.Abs(n[2]) < WAAGERECHT_NZ;
                Randbedingung lage = (boden && (raum.Unterster || raum.Unterirdisch)) || (wand && raum.Unterirdisch) ? Randbedingung.Erdreich
                                   : innen ? Randbedingung.Unbeheizt : Randbedingung.Aussenluft;
                double[] schwerpunkt = Schwerpunkt(h.Stuecke, f.N);
                stueckliste.Add(new Koerperflaechenstueck
                {
                    Raum = h.Raum, Seite = h.Seite, Schluessel = Schluessel(h.Seite, h.Raum, -1, -1),
                    FlaecheM2 = R(rest), Normale = n.Select(R).ToArray(), SchwerpunktM = Verschoben(schwerpunkt, f.N, h.Abstand), Lage = lage,
                });
            }

            // Gegenprobe: die stärker belegte Seite (Richtung der größten Seite bzw. dagegen).
            double[] n0 = seiten.OrderByDescending(e => e.Flaeche).First().N;
            double plus = stueckliste.Where(x => Punkt(seiten[x.Seite].N, n0) > 0.0).Sum(x => x.FlaecheM2);
            double minus = stueckliste.Where(x => Punkt(seiten[x.Seite].N, n0) <= 0.0).Sum(x => x.FlaecheM2);
            var e = new Koerperflaechenergebnis { SeiteM2 = R(Math.Max(plus, minus)) };
            e.Stuecke.AddRange(stueckliste);
            e.Treffer.AddRange(ergebnisTreffer);
            return e;
        }

        private static string Schluessel(int seite, int raum, int gegenseite, int gegenraum)
            => "S" + seite + "R" + raum + (gegenraum < 0 ? "" : "-S" + gegenseite + "R" + gegenraum);

        // ==================================================================
        //  Ebenen
        // ==================================================================

        private static List<Ebene> Ebenen(Dateikoerper k)
        {
            var liste = new List<Ebene>();
            var jeSchluessel = new Dictionary<(long, long, long, long), Ebene>();
            foreach (int[] d in k.Dreiecke)
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                double[] kreuz = Kreuz(Minus(b, a), Minus(c, a));
                double l = Math.Sqrt(Punkt(kreuz, kreuz));
                if (l < 1e-12) continue;
                double[] n = { kreuz[0] / l, kreuz[1] / l, kreuz[2] / l };
                double s = Punkt(n, a);
                var schluessel = ((long)Math.Round(n[0] * 1e3), (long)Math.Round(n[1] * 1e3), (long)Math.Round(n[2] * 1e3), (long)Math.Round(s * 1e3));
                if (!jeSchluessel.TryGetValue(schluessel, out Ebene e))
                {
                    e = new Ebene { N = n, S = s };
                    jeSchluessel[schluessel] = e;
                    liste.Add(e);
                }
                e.Dreiecke.Add(new[] { a, b, c });
                e.Flaeche += l / 2.0;
                foreach (double[] p in new[] { a, b, c })
                    for (int i = 0; i < 3; i++) { e.Min[i] = Math.Min(e.Min[i], p[i]); e.Max[i] = Math.Max(e.Max[i], p[i]); }
            }
            return liste;
        }

        /// <summary>Der kleinste Abstand zweier gegenläufiger Ebenen über 1 mm; 0 = keiner.</summary>
        private static double Dicke(List<Ebene> ebenen)
        {
            double dicke = double.PositiveInfinity;
            for (int i = 0; i < ebenen.Count; i++)
                for (int j = i + 1; j < ebenen.Count; j++)
                    if (Punkt(ebenen[i].N, ebenen[j].N) <= -1.0 + 1e-6)
                    {
                        double abstand = Math.Abs(ebenen[i].S + ebenen[j].S);
                        if (abstand > 1e-3) dicke = Math.Min(dicke, abstand);
                    }
            return double.IsInfinity(dicke) ? 0.0 : dicke;
        }

        private static double Breite(Ebene e)
        {
            double dx = e.Max[0] - e.Min[0], dy = e.Max[1] - e.Min[1], dz = e.Max[2] - e.Min[2];
            double diagonale = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            return diagonale > 1e-9 ? e.Flaeche / diagonale : 0.0;
        }

        private static double[] Mitte(Ebene e) => new[] { (e.Min[0] + e.Max[0]) / 2.0, (e.Min[1] + e.Max[1]) / 2.0, (e.Min[2] + e.Max[2]) / 2.0 };

        private static double[] Min(List<Ebene> e) => new[] { e.Min(x => x.Min[0]), e.Min(x => x.Min[1]), e.Min(x => x.Min[2]) };

        private static double[] Max(List<Ebene> e) => new[] { e.Max(x => x.Max[0]), e.Max(x => x.Max[1]), e.Max(x => x.Max[2]) };

        private static bool Ueberlappt3(double[] amin, double[] amax, double[] bmin, double[] bmax, double rand)
        {
            for (int i = 0; i < 3; i++)
                if (amin[i] > bmax[i] + rand || bmin[i] > amax[i] + rand) return false;
            return true;
        }

        // ==================================================================
        //  Polygone in der Ebene
        // ==================================================================

        /// <summary>Eine Basis (u, v) der Ebene mit der Normalen <paramref name="n"/>.</summary>
        internal static (double[] U, double[] V) Basis(double[] n)
        {
            double[] u = Einheit(Kreuz(n, Math.Abs(n[2]) < 0.9 ? new[] { 0.0, 0.0, 1.0 } : new[] { 1.0, 0.0, 0.0 }));
            return (u, Kreuz(n, u));
        }

        /// <summary>Die Projektion eines Vielecks [m] in die Basis (u, v), gegen den Uhrzeigersinn.</summary>
        internal static List<double[]> Eben(double[][] vieleck, double[] u, double[] v)
        {
            List<double[]> p = vieleck.Select(q => new[] { Punkt(q, u), Punkt(q, v) }).ToList();
            if (Inhalt(p, out _, out _) < 0.0) p.Reverse();
            return p;
        }

        /// <summary>Die Stücke der Überlappung der Seite <paramref name="f"/> mit der Raumfläche <paramref name="r"/> [m], in der Ebene der Seite.</summary>
        private static List<double[][]> Schnitt(Ebene f, Ebene r)
        {
            (double[] u, double[] v) = Basis(f.N);
            List<List<double[]>> ta = f.Dreiecke.Select(t => Eben(t, u, v)).ToList();
            List<List<double[]>> tb = r.Dreiecke.Select(t => Eben(t, u, v)).ToList();
            var stuecke = new List<double[][]>();
            foreach (List<double[]> a in ta)
                foreach (List<double[]> b in tb)
                {
                    if (!Kasten(a, b)) continue;
                    List<double[]> s = Klippen(a, b);
                    if (s.Count < 3 || Inhalt(s, out _, out _) <= EPS) continue;
                    stuecke.Add(s.Select(p => new[] { p[0] * u[0] + p[1] * v[0] + f.S * f.N[0], p[0] * u[1] + p[1] * v[1] + f.S * f.N[1],
                                                      p[0] * u[2] + p[1] * v[2] + f.S * f.N[2] }).ToArray());
                }
            return stuecke;
        }

        /// <summary>Die Überdeckung zweier Stücklisten in der Ebene mit der Normalen <paramref name="n"/> [m²] und ihr Schwerpunkt.</summary>
        internal static double Ueberdeckung(List<double[][]> a, List<double[][]> b, double[] n, out double[] schwerpunkt)
        {
            (double[] u, double[] v) = Basis(n);
            List<List<double[]>> pa = a.Select(x => Eben(x, u, v)).ToList(), pb = b.Select(x => Eben(x, u, v)).ToList();
            double flaeche = 0.0, su = 0.0, sv = 0.0;
            foreach (List<double[]> x in pa)
                foreach (List<double[]> y in pb)
                {
                    if (!Kasten(x, y)) continue;
                    List<double[]> s = Klippen(x, y);
                    if (s.Count < 3) continue;
                    double f = Inhalt(s, out double cu, out double cv);
                    if (f <= EPS) continue;
                    flaeche += f;
                    su += f * cu;
                    sv += f * cv;
                }
            schwerpunkt = null;
            if (flaeche <= EPS) return 0.0;
            // Der Schwerpunkt in der Ebene des ersten Stücks von a.
            double s0 = Punkt(n, a[0][0]);
            schwerpunkt = new[] { su / flaeche * u[0] + sv / flaeche * v[0] + s0 * n[0], su / flaeche * u[1] + sv / flaeche * v[1] + s0 * n[1],
                                  su / flaeche * u[2] + sv / flaeche * v[2] + s0 * n[2] };
            return flaeche;
        }

        private static double Summe(List<double[][]> stuecke, double[] n)
        {
            (double[] u, double[] v) = Basis(n);
            return stuecke.Sum(s => Inhalt(Eben(s, u, v), out _, out _));
        }

        private static double[] Schwerpunkt(List<double[][]> stuecke, double[] n)
        {
            (double[] u, double[] v) = Basis(n);
            double f = 0.0, su = 0.0, sv = 0.0;
            foreach (double[][] s in stuecke)
            {
                double a = Inhalt(Eben(s, u, v), out double cu, out double cv);
                f += a;
                su += a * cu;
                sv += a * cv;
            }
            double s0 = Punkt(n, stuecke[0][0]);
            return new[] { su / f * u[0] + sv / f * v[0] + s0 * n[0], su / f * u[1] + sv / f * v[1] + s0 * n[1], su / f * u[2] + sv / f * v[2] + s0 * n[2] };
        }

        /// <summary>Der Punkt <paramref name="p"/> um <paramref name="t"/> entlang <paramref name="n"/> verschoben, gerundet.</summary>
        private static double[] Verschoben(double[] p, double[] n, double t)
            => p == null ? null : new[] { R(p[0] + t * n[0]), R(p[1] + t * n[1]), R(p[2] + t * n[2]) };

        private static bool Kasten(List<double[]> a, List<double[]> b)
            => a.Min(p => p[0]) < b.Max(p => p[0]) - 1e-9 && b.Min(p => p[0]) < a.Max(p => p[0]) - 1e-9
               && a.Min(p => p[1]) < b.Max(p => p[1]) - 1e-9 && b.Min(p => p[1]) < a.Max(p => p[1]) - 1e-9;

        /// <summary>
        /// Sutherland–Hodgman: ein konvexes Vieleck gegen ein konvexes Vieleck, beide gegen den Uhrzeigersinn. Kanten der Klinge
        /// unter 1e-9 m bleiben unbeachtet (ihre Richtung ist Rundungsrauschen), das Ergebnis ohne doppelte Folgepunkte.
        /// </summary>
        private static List<double[]> Klippen(List<double[]> subjekt, List<double[]> klinge)
        {
            var aus = new List<double[]>(subjekt);
            for (int k = 0; k < klinge.Count && aus.Count > 0; k++)
            {
                double[] a = klinge[k], b = klinge[(k + 1) % klinge.Count];
                if (Math.Abs(b[0] - a[0]) + Math.Abs(b[1] - a[1]) < 1e-9) continue;
                double Seite(double[] p) => (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0]);
                List<double[]> ein = aus;
                aus = new List<double[]>();
                for (int i = 0; i < ein.Count; i++)
                {
                    double[] p = ein[i], q = ein[(i + 1) % ein.Count];
                    double sp = Seite(p), sq = Seite(q);
                    if (sp >= 0.0) aus.Add(p);
                    if ((sp >= 0.0) != (sq >= 0.0))
                    {
                        double t = sp / (sp - sq);
                        aus.Add(new[] { p[0] + t * (q[0] - p[0]), p[1] + t * (q[1] - p[1]) });
                    }
                }
            }
            var ohne = new List<double[]>(aus.Count);
            foreach (double[] p in aus)
                if (ohne.Count == 0 || Math.Abs(p[0] - ohne[ohne.Count - 1][0]) + Math.Abs(p[1] - ohne[ohne.Count - 1][1]) >= 1e-9) ohne.Add(p);
            while (ohne.Count > 1 && Math.Abs(ohne[0][0] - ohne[ohne.Count - 1][0]) + Math.Abs(ohne[0][1] - ohne[ohne.Count - 1][1]) < 1e-9)
                ohne.RemoveAt(ohne.Count - 1);
            return ohne;
        }

        /// <summary>Liegt <paramref name="p"/> im konvexen Vieleck (gegen den Uhrzeigersinn, Rand eingeschlossen bis 1 mm)?</summary>
        internal static bool Enthaelt(List<double[]> vieleck, double[] p)
        {
            for (int k = 0; k < vieleck.Count; k++)
            {
                double[] a = vieleck[k], b = vieleck[(k + 1) % vieleck.Count];
                double l = Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]));
                if (l < 1e-12) continue;
                if (((b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])) / l < -1e-3) return false;
            }
            return true;
        }

        /// <summary>Der Inhalt eines Vielecks (gegen den Uhrzeigersinn positiv) und sein Schwerpunkt.</summary>
        private static double Inhalt(List<double[]> p, out double cu, out double cv)
        {
            double a = 0.0, x = 0.0, y = 0.0;
            for (int i = 0; i < p.Count; i++)
            {
                double[] s = p[i], t = p[(i + 1) % p.Count];
                double k = s[0] * t[1] - t[0] * s[1];
                a += k;
                x += (s[0] + t[0]) * k;
                y += (s[1] + t[1]) * k;
            }
            a *= 0.5;
            cu = Math.Abs(a) > EPS ? x / (6.0 * a) : p.Average(q => q[0]);
            cv = Math.Abs(a) > EPS ? y / (6.0 * a) : p.Average(q => q[1]);
            return a;
        }

        /// <summary>Die Einheitsnormale des Dreiecks (a, b, c); <c>null</c> = entartet.</summary>
        internal static double[] Normale(double[] a, double[] b, double[] c)
        {
            double[] k = Kreuz(Minus(b, a), Minus(c, a));
            double l = Math.Sqrt(Punkt(k, k));
            return l < 1e-12 ? null : new[] { k[0] / l, k[1] / l, k[2] / l };
        }

        internal static double Punkt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        private static double[] Minus(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };

        private static double[] Kreuz(double[] a, double[] b)
            => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        private static double[] Einheit(double[] a)
        {
            double l = Math.Sqrt(Punkt(a, a));
            return new[] { a[0] / l, a[1] / l, a[2] / l };
        }

        private static double R(double w) => Math.Round(w, 6);
    }
}
