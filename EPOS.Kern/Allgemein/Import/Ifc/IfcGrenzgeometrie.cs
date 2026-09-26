using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Fläche einer Raumgrenze ohne Geometriekern</b> (Stufe G6c; Mehrzonenkonzept 6.2): Aus
    /// <c>IfcConnectionSurfaceGeometry.SurfaceOnRelatingElement</c> werden Flächeninhalt, Schwerpunkt und
    /// Normale in Weltkoordinaten — reine Vektorrechnung.
    ///
    /// <list type="bullet">
    /// <item><b><c>IfcCurveBoundedPlane</c></b>: der Außenrand minus die Innenränder derselben Ebene. Die
    /// Randkurve ist eine <c>IfcPolyline</c>, eine <c>IfcCompositeCurve</c> aus solchen oder eine
    /// <c>IfcIndexedPolyCurve</c> ohne Bogenstücke. 2D-Punkte liegen in der Ebene
    /// (<c>IfcPlane.Position</c>); 3D-Punkte im System des Raums — sie werden nicht auf zwei Koordinaten
    /// gekürzt, der Inhalt folgt über die Newell-Summe exakt. Ein roh angehängter Schlusspunkt zählt nicht.</item>
    /// <item><b><c>IfcSurfaceOfLinearExtrusion</c></b> mit gerader Profilkurve: Profil mal Tiefe; ein
    /// gekrümmtes Profil ist nicht eben und wird benannt nicht ausgewertet.</item>
    /// <item><b>Eben</b> heißt: Kein Punkt liegt mehr als <see cref="EBEN_TOLERANZ_M"/> neben der Ebene.</item>
    /// </list>
    /// Die Flächen stehen im System des Raums (<c>SurfaceOnRelatingElement</c> gehört zum
    /// <c>RelatingSpace</c>) und werden über dessen Platzierungskette in Weltkoordinaten gebracht.
    /// </summary>
    internal static class IfcGrenzgeometrie
    {
        /// <summary>Größter Abstand eines Randpunkts von der Ebene [m], bis zu dem die Berandung eben ist.</summary>
        internal const double EBEN_TOLERANZ_M = 0.001;

        /// <summary>Das Ergebnis einer Grenzfläche.</summary>
        internal sealed class Flaeche
        {
            /// <summary>Außenrand [m²].</summary>
            internal double FlaecheM2;

            /// <summary>Summe der Innenränder [m²].</summary>
            internal double AusschnittM2;

            /// <summary>Schwerpunkt des Außenrands in Weltkoordinaten [m].</summary>
            internal double[] SchwerpunktM;

            /// <summary>Einheitsnormale in Weltkoordinaten.</summary>
            internal double[] Normale;

            /// <summary>Der Grund, warum nichts ausgewertet ist (Entitätstyp); <c>null</c> = ausgewertet.</summary>
            internal string Fehler;
        }

        /// <summary>
        /// Die Fläche einer Raumgrenze; <c>null</c>, wenn die Grenze keine Geometrie trägt. Eine
        /// Geometrie, die sich nicht auswerten lässt, liefert ein Ergebnis mit <see cref="Flaeche.Fehler"/>.
        /// </summary>
        /// <param name="rsb">Die Raumgrenze.</param>
        /// <param name="raum">Der Weltrahmen ihres Raums; <c>null</c> = das Weltsystem.</param>
        /// <param name="laenge">Der Faktor der Längeneinheit nach Meter.</param>
        internal static Flaeche Lesen(IIfcRelSpaceBoundary rsb, IfcRahmen? raum, double laenge)
        {
            if (!(rsb?.ConnectionGeometry is IIfcConnectionSurfaceGeometry verbindung)) return null;
            IIfcSurfaceOrFaceSurface f = verbindung.SurfaceOnRelatingElement;
            if (f == null) return null;
            IfcRahmen rahmen = raum ?? IfcRahmen.Welt;
            try
            {
                switch (f)
                {
                    case IIfcCurveBoundedPlane ebene:
                        return Ebene(ebene, rahmen, laenge);
                    case IIfcSurfaceOfLinearExtrusion extrusion:
                        return Extrusion(extrusion, rahmen, laenge);
                    default:
                        return new Flaeche { Fehler = Typ(f) };
                }
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is InvalidCastException || ex is NullReferenceException)
            {
                return new Flaeche { Fehler = Typ(f) };
            }
        }

        private static string Typ(object e) => (e as Xbim.Common.IPersistEntity)?.ExpressType?.ExpressName ?? e?.GetType().Name ?? "";

        private static Flaeche Ebene(IIfcCurveBoundedPlane f, IfcRahmen raum, double laenge)
        {
            IfcRahmen ebene = f.BasisSurface is IIfcPlane p && p.Position != null ? IfcPlatzierung.Lokal(p.Position) : IfcRahmen.Welt;
            List<double[]> aussen = Punkte(f.OuterBoundary, ebene, out string fehler);
            if (aussen == null) return new Flaeche { Fehler = fehler ?? Typ(f.OuterBoundary) };
            Flaeche e = Auswerten(aussen, raum, laenge);
            if (e.Fehler != null) return e;
            // Die Normale der Ebene (im System des Raums) zeigt vom Raum weg; ohne sie die der Randkurve.
            double[] z = IfcPlatzierung.Normiert(IfcPlatzierung.Drehen(raum, ebene.Z));
            if (z != null) e.Normale = z;
            foreach (IIfcCurve innen in f.InnerBoundaries ?? Enumerable.Empty<IIfcCurve>())
            {
                List<double[]> loch = Punkte(innen, ebene, out _);
                if (loch == null) continue;
                Flaeche l = Auswerten(loch, raum, laenge);
                if (l.Fehler == null) e.AusschnittM2 += l.FlaecheM2;
            }
            return e;
        }

        private static Flaeche Extrusion(IIfcSurfaceOfLinearExtrusion f, IfcRahmen raum, double laenge)
        {
            if (!(f.SweptCurve is IIfcArbitraryOpenProfileDef profil) || !(profil.Curve is IIfcPolyline linie))
                return new Flaeche { Fehler = Typ(f) + "/" + Typ(f.SweptCurve) };
            IfcRahmen lage = f.Position != null ? IfcPlatzierung.Lokal(f.Position) : IfcRahmen.Welt;
            double[] richtung = IfcPlatzierung.Normiert(IfcPlatzierung.Richtung(f.ExtrudedDirection));
            double tiefe = IfcEigenschaften.Wert(f.Depth);
            if (richtung == null || !(tiefe > 0.0)) return new Flaeche { Fehler = Typ(f) };
            var profilpunkte = new List<double[]>();
            foreach (IIfcCartesianPoint q in linie.Points)
            {
                double[] k = IfcPlatzierung.Punkt(q);
                profilpunkte.Add(IfcPlatzierung.Abbilden(lage, new[] { k[0], k[1], 0.0 }));
            }
            if (profilpunkte.Count < 2) return new Flaeche { Fehler = Typ(f) };
            double[] versatz = IfcPlatzierung.Drehen(lage, IfcPlatzierung.Mal(richtung, tiefe));
            var ring = new List<double[]>(profilpunkte);
            for (int i = profilpunkte.Count - 1; i >= 0; i--) ring.Add(IfcPlatzierung.Plus(profilpunkte[i], versatz));
            Flaeche e = Auswerten(ring, raum, laenge);
            if (e.Fehler != null) e.Fehler = Typ(f);
            return e;
        }

        /// <summary>
        /// Die Punkte einer Randkurve im System des Raums (Längeneinheit der Datei); <c>null</c>, wenn die
        /// Kurve nicht aus geraden Stücken besteht (<paramref name="fehler"/> nennt den Typ).
        /// </summary>
        private static List<double[]> Punkte(IIfcCurve kurve, IfcRahmen ebene, out string fehler)
        {
            fehler = null;
            var punkte = new List<double[]>();
            if (!Sammeln(kurve, ebene, punkte, ref fehler)) return null;
            // Doppelte Folgepunkte und ein roh angehängter Schlusspunkt zählen nicht.
            var ring = new List<double[]>();
            foreach (double[] q in punkte)
                if (ring.Count == 0 || !Gleich(ring[ring.Count - 1], q)) ring.Add(q);
            if (ring.Count > 1 && Gleich(ring[0], ring[ring.Count - 1])) ring.RemoveAt(ring.Count - 1);
            return ring.Count >= 3 ? ring : null;
        }

        private static bool Sammeln(IIfcCurve kurve, IfcRahmen ebene, List<double[]> ziel, ref string fehler)
        {
            switch (kurve)
            {
                case IIfcPolyline linie:
                    foreach (IIfcCartesianPoint q in linie.Points) ziel.Add(Raumpunkt(q, ebene));
                    return true;
                case IIfcCompositeCurve verbund:
                    foreach (IIfcCompositeCurveSegment s in verbund.Segments.OfType<IIfcCompositeCurveSegment>())
                    {
                        var teil = new List<double[]>();
                        if (!Sammeln(s.ParentCurve, ebene, teil, ref fehler)) return false;
                        if (!s.SameSense) teil.Reverse();
                        ziel.AddRange(teil);
                    }
                    return true;
                case IIfcIndexedPolyCurve indiziert:
                    return Indiziert(indiziert, ebene, ziel, ref fehler);
                default:
                    fehler = Typ(kurve);
                    return false;
            }
        }

        private static bool Indiziert(IIfcIndexedPolyCurve kurve, IfcRahmen ebene, List<double[]> ziel, ref string fehler)
        {
            var liste = new List<double[]>();
            switch (kurve.Points)
            {
                case IIfcCartesianPointList2D p2:
                    foreach (var k in p2.CoordList)
                    {
                        double[] w = k.Select(x => (double)x.Value).ToArray();
                        liste.Add(IfcPlatzierung.Abbilden(ebene, new[] { w[0], w.Length > 1 ? w[1] : 0.0, 0.0 }));
                    }
                    break;
                case IIfcCartesianPointList3D p3:
                    foreach (var k in p3.CoordList)
                    {
                        double[] w = k.Select(x => (double)x.Value).ToArray();
                        liste.Add(new[] { w[0], w.Length > 1 ? w[1] : 0.0, w.Length > 2 ? w[2] : 0.0 });
                    }
                    break;
                default:
                    fehler = Typ(kurve);
                    return false;
            }
            if (kurve.Segments == null || !kurve.Segments.Any())
            {
                ziel.AddRange(liste);
                return true;
            }
            // Nur gerade Stücke (IfcLineIndex); ein Bogen (IfcArcIndex) ist gekrümmt.
            foreach (IIfcSegmentIndexSelect s in kurve.Segments)
            {
                if (s == null || s.GetType().Name != "IfcLineIndex" || !(s is Xbim.Common.IExpressComplexType stueck))
                {
                    fehler = Typ(kurve) + "/" + (s?.GetType().Name ?? "");
                    return false;
                }
                foreach (object o in stueck.Properties)
                {
                    long i = Convert.ToInt64(o is Xbim.Common.IExpressValueType w ? w.Value : o, System.Globalization.CultureInfo.InvariantCulture);
                    if (i < 1 || i > liste.Count) { fehler = Typ(kurve); return false; }
                    ziel.Add(liste[(int)i - 1]);
                }
            }
            return true;
        }

        /// <summary>Ein Punkt der Randkurve im System des Raums: 2D in der Ebene, 3D wie gelesen.</summary>
        private static double[] Raumpunkt(IIfcCartesianPoint q, IfcRahmen ebene)
        {
            double[] k = IfcPlatzierung.Punkt(q);
            return q.Coordinates.Count >= 3 ? k : IfcPlatzierung.Abbilden(ebene, new[] { k[0], k[1], 0.0 });
        }

        private static bool Gleich(double[] a, double[] b)
            => Math.Abs(a[0] - b[0]) < 1e-9 && Math.Abs(a[1] - b[1]) < 1e-9 && Math.Abs(a[2] - b[2]) < 1e-9;

        /// <summary>
        /// Inhalt, Schwerpunkt und Normale eines ebenen Rings (Punkte im System des Raums, Längeneinheit
        /// der Datei) in Weltkoordinaten und SI — über die Newell-Summe; nicht eben → Fehler.
        /// </summary>
        internal static Flaeche Auswerten(IReadOnlyList<double[]> ring, IfcRahmen raum, double laenge)
        {
            var welt = ring.Select(q => IfcPlatzierung.Mal(IfcPlatzierung.Abbilden(raum, q), laenge)).ToList();
            double[] n = { 0.0, 0.0, 0.0 };
            for (int i = 0; i < welt.Count; i++)
            {
                double[] a = welt[i], b = welt[(i + 1) % welt.Count];
                n[0] += (a[1] - b[1]) * (a[2] + b[2]);
                n[1] += (a[2] - b[2]) * (a[0] + b[0]);
                n[2] += (a[0] - b[0]) * (a[1] + b[1]);
            }
            double doppelt = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
            double[] einheit = IfcPlatzierung.Normiert(n);
            if (einheit == null || doppelt < 1e-12) return new Flaeche { Fehler = "Fläche null" };
            // Eben: Die Abstände der Randpunkte zur Ebene streuen um höchstens die Toleranz.
            double tief = double.MaxValue, hoch = double.MinValue;
            foreach (double[] q in welt)
            {
                double d = IfcPlatzierung.Punktprodukt(IfcPlatzierung.Minus(q, welt[0]), einheit);
                tief = Math.Min(tief, d);
                hoch = Math.Max(hoch, d);
            }
            if (hoch - tief > EBEN_TOLERANZ_M) return new Flaeche { Fehler = "nicht eben" };

            // Schwerpunkt über die Dreiecke eines Fächers ab dem ersten Punkt, vorzeichenrichtig.
            double summe = 0.0;
            double[] s = { 0.0, 0.0, 0.0 };
            for (int i = 1; i + 1 < welt.Count; i++)
            {
                double[] a = welt[0], b = welt[i], c = welt[i + 1];
                double t = 0.5 * IfcPlatzierung.Punktprodukt(IfcPlatzierung.Kreuz(IfcPlatzierung.Minus(b, a), IfcPlatzierung.Minus(c, a)), einheit);
                summe += t;
                for (int k = 0; k < 3; k++) s[k] += t * (a[k] + b[k] + c[k]) / 3.0;
            }
            double[] schwerpunkt = Math.Abs(summe) > 1e-12 ? new[] { s[0] / summe, s[1] / summe, s[2] / summe } : welt[0];
            return new Flaeche { FlaecheM2 = doppelt / 2.0, SchwerpunktM = schwerpunkt, Normale = einheit };
        }
    }
}
