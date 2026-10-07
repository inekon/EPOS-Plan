using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Welche Seite eines Bauteilkörpers maßgeblich ist (<see cref="IfcBauteilkoerper"/>).</summary>
    internal enum Bauteilkoerperart
    {
        /// <summary>Senkrechtes Bauteil (<c>IfcWall</c>): die größere der beiden Seitenflächen, Orientierung der Außenseite.</summary>
        Wand,
        /// <summary>Waagerechtes oder geneigtes Bauteil nach oben (<c>IfcSlab</c> Decke, <c>IfcRoof</c>): die Oberseite orientiert.</summary>
        PlatteOben,
        /// <summary>Bauteil nach unten (<c>IfcSlab BASESLAB</c>): die Unterseite orientiert.</summary>
        PlatteUnten,
    }

    /// <summary>Eine ebene Teilfläche eines Bauteilkörpers (oder mehrere unter 5° zusammengefasst).</summary>
    internal sealed class Koerperteilflaeche
    {
        /// <summary>Die Fläche [m²] — Bruttofläche, Öffnungen nicht abgezogen.</summary>
        internal double FlaecheM2 { get; init; }
        /// <summary>Die Außennormale im Modellsystem (x Ost, y Nord des Modells, z oben), Einheitsvektor.</summary>
        internal double[] Normale { get; init; }
        /// <summary>Die Neigung [°]: 0° = waagerecht nach oben, 90° = senkrecht, 180° = waagerecht nach unten.</summary>
        internal double NeigungGrad { get; init; }
        /// <summary>Der Azimut [°], 0° = Nord, im Uhrzeigersinn, nach TrueNorth bzw. IfcMapConversion; <c>null</c> = waagerecht.</summary>
        internal double? AzimutGrad { get; init; }
        /// <summary>Die Zahl der ebenen Flächen des Körpers in diesem Teil.</summary>
        internal int Flaechen { get; init; }
    }

    /// <summary>Das Ergebnis eines Bauteilkörpers: maßgebliche Fläche, Orientierung, Teilflächen und Vermerke.</summary>
    internal sealed class Bauteilkoerperflaeche
    {
        /// <summary>Die maßgebliche Bruttofläche [m²] (Summe der Teile).</summary>
        internal double FlaecheM2 { get; init; }
        /// <summary>Die Neigung [°] des größten Teils.</summary>
        internal double NeigungGrad { get; init; }
        /// <summary>Der Azimut [°] des größten Teils; <c>null</c> = waagerecht.</summary>
        internal double? AzimutGrad { get; init; }
        /// <summary>Die Teilflächen, größte zuerst; mehr als eine = gegliedertes Bauteil.</summary>
        internal IReadOnlyList<Koerperteilflaeche> Teile { get; init; } = Array.Empty<Koerperteilflaeche>();
        /// <summary>Wurden ebene Flächen mit einem Richtungsunterschied über 0° und unter 5° zusammengefasst?</summary>
        internal bool Zusammengefasst { get; init; }
        /// <summary>
        /// Die Außenseite einer Wand ließ sich am Gebäudeschwerpunkt nicht unterscheiden (beide Seiten gleich weit weg oder
        /// kein Schwerpunkt): gilt die Seite der größten Fläche mit der Normalen der Datei.
        /// </summary>
        internal bool AussenseiteUnbestimmt { get; init; }
        /// <summary>Ist das Bauteil gegliedert (mehr als eine Teilfläche)?</summary>
        internal bool Gegliedert => Teile.Count > 1;
    }

    /// <summary>
    /// <b>Der Bauteilkörper als Rechengröße</b> (Abstimmung G5, Teil G5-1; Datenaustauschkonzept 15): wertet das
    /// Dreiecksnetz eines Hüllbauteils aus, das <see cref="IfcRaumkoerper"/> ohne Geometriekern liest (Extrusion,
    /// Tessellation, BRep, <c>IfcMappedItem</c>, Placement-Kette; Weltkoordinaten in Meter) — Fläche, Neigung und Azimut je
    /// ebener Teilfläche.
    ///
    /// <list type="number">
    /// <item><b>Ebenen:</b> die Dreiecke werden nach Normale (Richtung gleich bis 1e-6) und Ebenenabstand (1e-4 m) zu
    /// ebenen Flächen gesammelt; die Normale ist die des Netzes (rechte Hand, vom Körper weg).</item>
    /// <item><b>Wand</b> (<see cref="Bauteilkoerperart.Wand"/>): die senkrechten Flächen (|n<sub>z</sub>| &lt; 0,5) ohne die
    /// Stirnflächen — Flächen, deren Breite (Fläche ÷ Höhe) höchstens das 1,5-fache der Wanddicke ist; die Dicke ist der
    /// kleinste Abstand zweier gegenläufiger Ebenen. Außen ist die Seite, deren Flächen vom <b>Gebäudeschwerpunkt</b> weg
    /// zeigen (Mittel der Raumkörper, ersatzweise der Bauteilkörper); lässt sich das nicht trennen, gilt die Seite der
    /// größten Fläche mit der Normalen der Datei (<see cref="Bauteilkoerperflaeche.AussenseiteUnbestimmt"/>). Maßgeblich ist die
    /// größere der beiden Seiten; die Teile tragen die Richtung der Außenseite.</item>
    /// <item><b>Platte und Dach</b>: Oberseite (n<sub>z</sub> &gt; 0,17) und Unterseite (n<sub>z</sub> &lt; −0,17); maßgeblich
    /// ist die größere, orientiert nach oben (<see cref="Bauteilkoerperart.PlatteOben"/>) bzw. unten
    /// (<see cref="Bauteilkoerperart.PlatteUnten"/>).</item>
    /// <item><b>Gliederung:</b> Flächen der maßgeblichen Seite unter 5° Richtungsunterschied werden flächengewichtet zu einem
    /// Teil zusammengefasst (<see cref="Bauteilkoerperflaeche.Zusammengefasst"/>), ab 5° bleiben sie getrennte Teile.</item>
    /// </list>
    /// <para><b>Brutto:</b> Öffnungen sind nicht abgezogen. <i>Hier hängt G5-2 den Abzug der Öffnungen ein</i>
    /// (<see cref="Auswerten"/> liefert die Bruttofläche, der Aufrufer zieht ab).</para>
    /// <para><b>Deterministisch:</b> Dreiecke in der Reihenfolge des Netzes, Teile nach Fläche absteigend, bei Gleichstand
    /// in der Reihenfolge ihres ersten Dreiecks.</para>
    /// </summary>
    internal static class IfcBauteilkoerper
    {
        /// <summary>Der Richtungsunterschied [°], unter dem ebene Teilflächen zusammengefasst werden.</summary>
        internal const double ZUSAMMENFASSEN_GRAD = 5.0;
        /// <summary>Die relative Abweichung Körper gegen Mengensatz, über der gemeldet wird (A5, wie die Trennflächen).</summary>
        internal const double ABWEICHUNG_GRENZE = 0.02;
        /// <summary>|n<sub>z</sub>| unter dieser Grenze: senkrechte Fläche (Wand).</summary>
        private const double SENKRECHT_NZ = 0.5;
        /// <summary>|n<sub>z</sub>| über dieser Grenze: Ober- bzw. Unterseite einer Platte (Neigung bis rund 80°).</summary>
        private const double WAAGERECHT_NZ = 0.17;
        /// <summary>Die Stirnfläche ist höchstens so viele Dicken breit.</summary>
        private const double STIRN_DICKEN = 1.5;
        private const double EBENE_TOLERANZ_M = 1e-4;
        private const double RICHTUNG_TOLERANZ = 1e-6;

        /// <summary>Eine ebene Fläche des Netzes.</summary>
        private sealed class Ebene
        {
            public double[] N;
            public double D;
            public double Flaeche;
            public double[] Schwerpunkt = new double[3];
            public double ZMin = double.PositiveInfinity, ZMax = double.NegativeInfinity;
            public int Erstes;
        }

        /// <summary>
        /// Wertet den Körper <paramref name="k"/> aus; <c>null</c> = keine maßgebliche Fläche (leerer Körper, keine
        /// passenden Flächen).
        /// </summary>
        /// <param name="k">Der Körper in Weltkoordinaten [m].</param>
        /// <param name="art">Welche Seite maßgeblich ist.</param>
        /// <param name="schwerpunktM">Der Gebäudeschwerpunkt [m] im Modellsystem; <c>null</c> = unbekannt.</param>
        /// <param name="drehungGrad">Die Drehung des Modells gegen Nord (<see cref="IfcPlatzierung.Drehung"/>).</param>
        internal static Bauteilkoerperflaeche Auswerten(Dateikoerper k, Bauteilkoerperart art, double[] schwerpunktM, double drehungGrad)
        {
            if (k == null || k.Dreiecke.Count == 0) return null;
            List<Ebene> ebenen = Ebenen(k);
            return art == Bauteilkoerperart.Wand ? Wand(ebenen, schwerpunktM, drehungGrad) : Platte(ebenen, art, drehungGrad);
        }

        /// <summary>Der Mittelpunkt eines Körpers [m]: das Mittel der Ecken seines umschließenden Quaders.</summary>
        internal static double[] Mitte(Dateikoerper k)
        {
            if (k == null || k.PunkteM.Count == 0) return null;
            var min = new[] { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity };
            var max = new[] { double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity };
            foreach (double[] p in k.PunkteM)
                for (int i = 0; i < 3; i++) { min[i] = Math.Min(min[i], p[i]); max[i] = Math.Max(max[i], p[i]); }
            return new[] { (min[0] + max[0]) / 2.0, (min[1] + max[1]) / 2.0, (min[2] + max[2]) / 2.0 };
        }

        /// <summary>Der Schwerpunkt mehrerer Körper: das Mittel ihrer Mitten; <c>null</c> = keiner.</summary>
        internal static double[] Schwerpunkt(IEnumerable<Dateikoerper> koerper)
        {
            double[] s = { 0.0, 0.0, 0.0 };
            int n = 0;
            foreach (Dateikoerper k in koerper)
            {
                double[] m = Mitte(k);
                if (m == null) continue;
                for (int i = 0; i < 3; i++) s[i] += m[i];
                n++;
            }
            return n == 0 ? null : new[] { s[0] / n, s[1] / n, s[2] / n };
        }

        /// <summary>Die relative Abweichung der Körperfläche gegen den Mengensatz.</summary>
        internal static double Abweichung(double mengensatzM2, double koerperM2)
            => Math.Abs(koerperM2 - mengensatzM2) / Math.Max(Math.Abs(mengensatzM2), 1e-12);

        // ==================================================================
        //  Ebenen
        // ==================================================================

        private static List<Ebene> Ebenen(Dateikoerper k)
        {
            var ebenen = new List<Ebene>();
            for (int t = 0; t < k.Dreiecke.Count; t++)
            {
                int[] d = k.Dreiecke[t];
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                double[] kreuz = IfcPlatzierung.Kreuz(IfcPlatzierung.Minus(b, a), IfcPlatzierung.Minus(c, a));
                double laenge = Math.Sqrt(kreuz[0] * kreuz[0] + kreuz[1] * kreuz[1] + kreuz[2] * kreuz[2]);
                if (laenge <= 1e-12) continue;
                double flaeche = laenge / 2.0;
                double[] n = { kreuz[0] / laenge, kreuz[1] / laenge, kreuz[2] / laenge };
                double[] mitte = { (a[0] + b[0] + c[0]) / 3.0, (a[1] + b[1] + c[1]) / 3.0, (a[2] + b[2] + c[2]) / 3.0 };
                Ebene e = null;
                foreach (Ebene x in ebenen)
                    if (Punkt(x.N, n) >= 1.0 - RICHTUNG_TOLERANZ && Math.Abs(Punkt(x.N, mitte) - x.D) <= EBENE_TOLERANZ_M) { e = x; break; }
                if (e == null)
                {
                    e = new Ebene { N = n, D = Punkt(n, mitte), Erstes = t };
                    ebenen.Add(e);
                }
                for (int i = 0; i < 3; i++) e.Schwerpunkt[i] = (e.Schwerpunkt[i] * e.Flaeche + mitte[i] * flaeche) / (e.Flaeche + flaeche);
                e.Flaeche += flaeche;
                e.ZMin = Math.Min(e.ZMin, Math.Min(a[2], Math.Min(b[2], c[2])));
                e.ZMax = Math.Max(e.ZMax, Math.Max(a[2], Math.Max(b[2], c[2])));
            }
            return ebenen;
        }

        // ==================================================================
        //  Wand
        // ==================================================================

        private static Bauteilkoerperflaeche Wand(List<Ebene> ebenen, double[] g, double drehung)
        {
            List<Ebene> seiten = ebenen.Where(e => Math.Abs(e.N[2]) < SENKRECHT_NZ).ToList();
            if (seiten.Count == 0) return null;

            // Die Dicke: der kleinste Abstand zweier gegenläufiger Ebenen.
            double dicke = double.PositiveInfinity;
            for (int i = 0; i < seiten.Count; i++)
                for (int j = i + 1; j < seiten.Count; j++)
                    if (Punkt(seiten[i].N, seiten[j].N) <= -1.0 + RICHTUNG_TOLERANZ)
                    {
                        double abstand = Math.Abs(seiten[i].D + seiten[j].D);
                        if (abstand > 1e-6) dicke = Math.Min(dicke, abstand);
                    }
            if (!double.IsInfinity(dicke))
            {
                List<Ebene> ohneStirn = seiten.Where(e => Breite(e) > STIRN_DICKEN * dicke + 1e-9).ToList();
                if (ohneStirn.Count > 0) seiten = ohneStirn;
            }

            // Außen: vom Gebäudeschwerpunkt weg (waagerecht gemessen).
            List<Ebene> aussen = null, innen = null;
            if (g != null)
            {
                aussen = seiten.Where(e => Weg(e, g) > 1e-9).ToList();
                innen = seiten.Where(e => Weg(e, g) <= 1e-9).ToList();
            }
            bool unbestimmt = aussen == null || aussen.Count == 0 || innen.Count == 0
                              || Math.Abs(aussen.Sum(e => Weg(e, g)) / aussen.Count + innen.Sum(e => Weg(e, g)) / innen.Count) <= 1e-9;
            if (unbestimmt)
            {
                Ebene groesste = Groesste(seiten);
                aussen = seiten.Where(e => Punkt(e.N, groesste.N) > 0.0).ToList();
                innen = seiten.Where(e => Punkt(e.N, groesste.N) <= 0.0).ToList();
            }
            double summeAussen = aussen.Sum(e => e.Flaeche), summeInnen = innen.Sum(e => e.Flaeche);
            double faktor = summeInnen > summeAussen && summeAussen > 0.0 ? summeInnen / summeAussen : 1.0;
            return Ergebnis(aussen, faktor, drehung, unbestimmt);
        }

        /// <summary>Wie weit zeigt die Fläche waagerecht vom Schwerpunkt weg (Normale · (Flächenmitte − Schwerpunkt))?</summary>
        private static double Weg(Ebene e, double[] g)
            => e.N[0] * (e.Schwerpunkt[0] - g[0]) + e.N[1] * (e.Schwerpunkt[1] - g[1]);

        private static double Breite(Ebene e)
        {
            double hoehe = e.ZMax - e.ZMin;
            return hoehe > 1e-9 ? e.Flaeche / hoehe : double.PositiveInfinity;
        }

        // ==================================================================
        //  Platte und Dach
        // ==================================================================

        private static Bauteilkoerperflaeche Platte(List<Ebene> ebenen, Bauteilkoerperart art, double drehung)
        {
            List<Ebene> oben = ebenen.Where(e => e.N[2] > WAAGERECHT_NZ).ToList();
            List<Ebene> unten = ebenen.Where(e => e.N[2] < -WAAGERECHT_NZ).ToList();
            List<Ebene> massgeblich = art == Bauteilkoerperart.PlatteUnten ? unten : oben;
            List<Ebene> gegen = art == Bauteilkoerperart.PlatteUnten ? oben : unten;
            if (massgeblich.Count == 0) { massgeblich = gegen; gegen = new List<Ebene>(); }
            if (massgeblich.Count == 0) return null;
            double eigen = massgeblich.Sum(e => e.Flaeche), andere = gegen.Sum(e => e.Flaeche);
            double faktor = andere > eigen && eigen > 0.0 ? andere / eigen : 1.0;
            return Ergebnis(massgeblich, faktor, drehung, false);
        }

        // ==================================================================
        //  Gliederung und Ergebnis
        // ==================================================================

        private static Bauteilkoerperflaeche Ergebnis(List<Ebene> seite, double faktor, double drehung, bool unbestimmt)
        {
            double cosGrenze = Math.Cos(ZUSAMMENFASSEN_GRAD * Math.PI / 180.0);
            var gruppen = new List<(double[] Summe, double Flaeche, int Zahl, int Erstes, bool Geknickt)>();
            foreach (Ebene e in seite.OrderByDescending(x => x.Flaeche).ThenBy(x => x.Erstes))
            {
                int treffer = -1;
                for (int i = 0; i < gruppen.Count && treffer < 0; i++)
                {
                    double[] r = Einheit(gruppen[i].Summe);
                    if (r != null && Punkt(r, e.N) > cosGrenze) treffer = i;
                }
                if (treffer < 0)
                {
                    gruppen.Add((new[] { e.N[0] * e.Flaeche, e.N[1] * e.Flaeche, e.N[2] * e.Flaeche }, e.Flaeche, 1, e.Erstes, false));
                    continue;
                }
                var gr = gruppen[treffer];
                double[] richtung = Einheit(gr.Summe);
                bool geknickt = gr.Geknickt || Punkt(richtung, e.N) < 1.0 - RICHTUNG_TOLERANZ;
                gruppen[treffer] = (new[] { gr.Summe[0] + e.N[0] * e.Flaeche, gr.Summe[1] + e.N[1] * e.Flaeche, gr.Summe[2] + e.N[2] * e.Flaeche },
                                    gr.Flaeche + e.Flaeche, gr.Zahl + 1, gr.Erstes, geknickt);
            }
            List<Koerperteilflaeche> teile = gruppen
                .OrderByDescending(x => x.Flaeche).ThenBy(x => x.Erstes)
                .Select(x =>
                {
                    double[] n = Einheit(x.Summe) ?? new[] { 0.0, 0.0, 1.0 };
                    return new Koerperteilflaeche
                    {
                        FlaecheM2 = x.Flaeche * faktor,
                        Normale = n,
                        NeigungGrad = Neigung(n),
                        AzimutGrad = Azimut(n, drehung),
                        Flaechen = x.Zahl,
                    };
                }).ToList();
            return new Bauteilkoerperflaeche
            {
                FlaecheM2 = teile.Sum(t => t.FlaecheM2),
                NeigungGrad = teile[0].NeigungGrad,
                AzimutGrad = teile[0].AzimutGrad,
                Teile = teile,
                Zusammengefasst = gruppen.Any(x => x.Geknickt),
                AussenseiteUnbestimmt = unbestimmt,
            };
        }

        /// <summary>Die Neigung [°] einer Außennormalen: 0° = nach oben, 90° = waagerecht zeigend, 180° = nach unten.</summary>
        internal static double Neigung(double[] n) => Math.Acos(Math.Max(-1.0, Math.Min(1.0, n[2]))) * 180.0 / Math.PI;

        /// <summary>Der Azimut einer Außennormalen nach Nord; <c>null</c> = ohne waagerechten Anteil.</summary>
        internal static double? Azimut(double[] n, double drehung)
            => Math.Sqrt(n[0] * n[0] + n[1] * n[1]) <= 1e-9 ? (double?)null : IfcPlatzierung.Azimut(n[0], n[1], drehung);

        private static Ebene Groesste(List<Ebene> ebenen) => ebenen.OrderByDescending(e => e.Flaeche).ThenBy(e => e.Erstes).First();

        private static double Punkt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        private static double[] Einheit(double[] v)
        {
            double l = Math.Sqrt(Punkt(v, v));
            return l <= 1e-12 ? null : new[] { v[0] / l, v[1] / l, v[2] / l };
        }
    }
}
