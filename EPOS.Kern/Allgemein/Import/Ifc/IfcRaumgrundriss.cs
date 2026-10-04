using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Grundriss eines Raums</b> aus seiner Körperdarstellung (Mehrzonenkonzept 6.5, Trenndecke ohne
    /// Raumgrenzen): der Profilring einer senkrechten Extrusion (<c>IfcExtrudedAreaSolid</c> mit
    /// <c>IfcArbitraryClosedProfileDef</c> aus <c>IfcPolyline</c> oder <c>IfcIndexedPolyCurve</c> ohne Bögen, oder
    /// <c>IfcRectangleProfileDef</c>), in Weltkoordinaten waagerecht projiziert [m]. Andere Darstellungen (Brep,
    /// Tessellierung, Bögen, schräge Extrusion) liefern keinen Grundriss — ohne Geometriekern (ADR-003), benannt
    /// über das Fehlen.
    /// </summary>
    internal static class IfcRaumgrundriss
    {
        /// <summary>Kleinster Betrag der senkrechten Komponente der Extrusionsrichtung, damit der Profilring der Grundriss ist.</summary>
        internal const double SENKRECHT_MIN = 0.99;

        /// <summary>
        /// Der Grundrissring eines Produkts — je Punkt x, y [m] in Weltkoordinaten, ohne Schlusspunkt; <c>null</c> = keiner.
        /// </summary>
        /// <param name="p">Das Produkt (ein Raum).</param>
        /// <param name="rahmen">Der Weltrahmen seiner Platzierung.</param>
        /// <param name="laenge">Der Faktor der Längeneinheit nach Meter.</param>
        internal static IReadOnlyList<double[]> Lesen(IIfcProduct p, IfcRahmen rahmen, double laenge)
        {
            if (p?.Representation == null) return null;
            foreach (IIfcRepresentation darstellung in p.Representation.Representations)
            {
                string kennung = darstellung.RepresentationIdentifier?.ToString();
                if (kennung != null && !string.Equals(kennung, "Body", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (IIfcRepresentationItem item in darstellung.Items)
                {
                    List<double[]> ring = Extrusion(item as IIfcExtrudedAreaSolid, rahmen, laenge);
                    if (ring != null) return ring;
                }
            }
            return null;
        }

        private static List<double[]> Extrusion(IIfcExtrudedAreaSolid x, IfcRahmen rahmen, double laenge)
        {
            if (x?.SweptArea == null || x.ExtrudedDirection == null) return null;
            IfcRahmen lage = x.Position == null ? rahmen : IfcPlatzierung.Verketten(rahmen, IfcPlatzierung.Lokal(x.Position));
            double[] d = x.ExtrudedDirection.DirectionRatios.Select(r => (double)r).Concat(new[] { 0.0, 0.0, 0.0 }).Take(3).ToArray();
            double[] w = IfcPlatzierung.Drehen(lage, d);
            double betrag = Math.Sqrt(w[0] * w[0] + w[1] * w[1] + w[2] * w[2]);
            if (betrag <= 0.0 || Math.Abs(w[2]) / betrag < SENKRECHT_MIN) return null;
            List<double[]> profil = Profil(x.SweptArea);
            if (profil == null) return null;
            var ring = new List<double[]>();
            foreach (double[] q in profil)
            {
                double[] welt = IfcPlatzierung.Abbilden(lage, new[] { q[0], q[1], 0.0 });
                double[] punkt = { welt[0] * laenge, welt[1] * laenge };
                if (ring.Count == 0 || !Gleich(ring[ring.Count - 1], punkt)) ring.Add(punkt);
            }
            if (ring.Count > 1 && Gleich(ring[0], ring[ring.Count - 1])) ring.RemoveAt(ring.Count - 1);
            return ring.Count >= 3 && Math.Abs(Grundrissueberlappung.Flaeche(ring)) > 0.0 ? ring : null;
        }

        /// <summary>Die Punkte des Profils im Profilsystem (Längeneinheit der Datei); <c>null</c> = nicht aus geraden Stücken.</summary>
        private static List<double[]> Profil(IIfcProfileDef profil)
        {
            switch (profil)
            {
                case IIfcRectangleProfileDef r:
                {
                    double a = r.XDim / 2.0, b = r.YDim / 2.0;
                    IfcRahmen lage = r.Position == null ? IfcRahmen.Welt : IfcPlatzierung.Lokal(r.Position);
                    return new[] { new[] { -a, -b }, new[] { a, -b }, new[] { a, b }, new[] { -a, b } }
                        .Select(q => IfcPlatzierung.Abbilden(lage, new[] { q[0], q[1], 0.0 })).ToList();
                }
                case IIfcArbitraryClosedProfileDef a:
                    return Kurve(a.OuterCurve);
                default:
                    return null;
            }
        }

        private static List<double[]> Kurve(IIfcCurve kurve)
        {
            switch (kurve)
            {
                case IIfcPolyline linie:
                    return linie.Points.Select(q => IfcPlatzierung.Punkt(q)).ToList();
                case IIfcIndexedPolyCurve indiziert:
                    if (indiziert.Segments != null && indiziert.Segments.Any(s => s?.GetType().Name == "IfcArcIndex")) return null;
                    if (!(indiziert.Points is IIfcCartesianPointList2D liste)) return null;
                    var punkte = liste.CoordList.Select(k => k.Select(v => (double)v).ToArray()).Where(k => k.Length >= 2).ToList();
                    // Nur gerade Stücke: die Punktliste in ihrer Reihenfolge (Exporte führen die Segmente fortlaufend).
                    return punkte;
                default:
                    return null;
            }
        }

        private static bool Gleich(double[] a, double[] b) => Math.Abs(a[0] - b[0]) < 1e-6 && Math.Abs(a[1] - b[1]) < 1e-6;
    }
}
