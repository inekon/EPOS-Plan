using System;
using System.Collections.Generic;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Koordinatenrahmen im Weltsystem: Ursprung und drei Achsen (rechtshändig, normiert).</summary>
    internal readonly struct IfcRahmen
    {
        /// <summary>Legt einen Rahmen an.</summary>
        public IfcRahmen(double[] ursprung, double[] x, double[] y, double[] z)
        {
            Ursprung = ursprung;
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Ursprung in Längeneinheiten der Datei.</summary>
        public double[] Ursprung { get; }

        /// <summary>Lokale x-Achse im Weltsystem.</summary>
        public double[] X { get; }

        /// <summary>Lokale y-Achse im Weltsystem.</summary>
        public double[] Y { get; }

        /// <summary>Lokale z-Achse im Weltsystem.</summary>
        public double[] Z { get; }

        /// <summary>Das Weltsystem selbst.</summary>
        public static IfcRahmen Welt => new IfcRahmen(new[] { 0.0, 0.0, 0.0 }, new[] { 1.0, 0.0, 0.0 },
                                                      new[] { 0.0, 1.0, 0.0 }, new[] { 0.0, 0.0, 1.0 });
    }

    /// <summary>
    /// <b>Azimut ohne Geometriekern — reine Matrixrechnung</b> (Umsetzungskonzept 3.4, Stufe G4-3).
    ///
    /// <para><b>Die Kette:</b> <c>IIfcProduct.ObjectPlacement</c> → <c>IIfcLocalPlacement.RelativePlacement</c>
    /// (<c>IIfcAxis2Placement3D</c> mit <c>Axis</c> und <c>RefDirection</c>, oder 2D) → über
    /// <c>PlacementRelTo</c> aufwärts bis zum Weltsystem. <c>IIfcGridPlacement</c> und
    /// <c>IIfcLinearPlacement</c> werden benannt übergangen (<c>IMP_IFC_PROT_PLATZIERUNGSART</c>) — das
    /// Bauteil hat dann keinen Azimut.</para>
    ///
    /// <para><b>Die Seite bestimmt die Raumgrenze:</b> Senkrecht zur Wandachse (lokale x-Achse) liegen
    /// in der Waagerechten ZWEI Richtungen, und die Spezifikation sagt nicht, welche außen ist. Die
    /// Außennormale zeigt von den Räumen weg, die die Wand begrenzen — geprüft am Vorzeichen des
    /// Abstands zwischen Raum- und Wandplatzierung entlang der lokalen y-Achse.</para>
    ///
    /// <para><b>Die Nordrichtung:</b> <c>TrueNorth</c> des Modellkontexts (Vorgabe [0,1]) dreht das
    /// Ergebnis — AUSSER der Kontext trägt eine <c>IIfcMapConversion</c>; dann gilt deren Drehung
    /// atan2(XAxisOrdinate, XAxisAbscissa), und <c>TrueNorth</c> wird NICHT addiert. Der Azimut im Abbild
    /// folgt der Datenbankkonvention: 0° = Nord, im Uhrzeigersinn.</para>
    /// </summary>
    internal static class IfcPlatzierung
    {
        /// <summary>Kleinster waagerechter Anteil einer Achse, unter dem sie keine Himmelsrichtung hat.</summary>
        internal const double WAAGERECHT_MIN = 1e-6;

        // ==================================================================
        //  Kette
        // ==================================================================

        /// <summary>
        /// Der Weltrahmen einer Platzierung. <paramref name="fremdeArt"/> nennt den Typ einer
        /// Platzierung, die kein <c>IIfcLocalPlacement</c> ist (dann <c>null</c> als Ergebnis).
        /// </summary>
        public static IfcRahmen? Weltrahmen(IIfcObjectPlacement platzierung, IfcRahmen wurzel, out string fremdeArt)
        {
            fremdeArt = null;
            var kette = new List<IIfcAxis2Placement>();
            var besucht = new HashSet<int>();
            IIfcObjectPlacement p = platzierung;
            while (p != null)
            {
                if (!besucht.Add(p.EntityLabel)) break;   // Kreis — ein Fehler der Datei, kein Endlosweg
                if (p is IIfcLocalPlacement lokal)
                {
                    kette.Add(lokal.RelativePlacement);
                    p = lokal.PlacementRelTo;
                }
                else
                {
                    fremdeArt = p.ExpressType?.ExpressName ?? p.GetType().Name;
                    return null;
                }
            }
            if (platzierung == null) return null;

            IfcRahmen r = wurzel;
            for (int i = kette.Count - 1; i >= 0; i--)
                r = Verketten(r, Lokal(kette[i]));
            return r;
        }

        /// <summary>Der lokale Rahmen einer Achsplatzierung (3D oder 2D); ohne Angabe das Einheitssystem.</summary>
        public static IfcRahmen Lokal(IIfcAxis2Placement platzierung)
        {
            switch (platzierung)
            {
                case IIfcAxis2Placement3D p3:
                    return Achsen3D(Punkt(p3.Location), Richtung(p3.Axis), Richtung(p3.RefDirection));
                case IIfcAxis2Placement2D p2:
                {
                    double[] x = Richtung(p2.RefDirection);
                    return Achsen3D(Punkt(p2.Location), null, x == null ? null : new[] { x[0], x[1], 0.0 });
                }
                default:
                    return IfcRahmen.Welt;
            }
        }

        /// <summary>
        /// Der Rahmen aus Ursprung, z-Achse (<c>Axis</c>, Vorgabe [0,0,1]) und Bezugsrichtung
        /// (<c>RefDirection</c>, Vorgabe [1,0,0]): x = Bezugsrichtung ohne ihren Anteil längs z, y = z × x.
        /// </summary>
        public static IfcRahmen Achsen3D(double[] ursprung, double[] achse, double[] bezug)
        {
            double[] z = Normiert(achse) ?? new[] { 0.0, 0.0, 1.0 };
            double[] xr = Normiert(bezug) ?? new[] { 1.0, 0.0, 0.0 };
            double[] x = Normiert(Minus(xr, Mal(z, Punktprodukt(xr, z))));
            if (x == null)   // Bezugsrichtung parallel zu z: die Vorgabe, notfalls y
            {
                x = Normiert(Minus(new[] { 1.0, 0.0, 0.0 }, Mal(z, z[0])))
                    ?? Normiert(Minus(new[] { 0.0, 1.0, 0.0 }, Mal(z, z[1])));
            }
            double[] y = Kreuz(z, x);
            return new IfcRahmen(ursprung ?? new[] { 0.0, 0.0, 0.0 }, x, y, z);
        }

        /// <summary>Setzt einen lokalen Rahmen in einen Elternrahmen: Welt = Eltern.O + Eltern.R · Lokal.</summary>
        public static IfcRahmen Verketten(IfcRahmen eltern, IfcRahmen lokal)
        {
            double[] o = Plus(eltern.Ursprung, Drehen(eltern, lokal.Ursprung));
            return new IfcRahmen(o, Drehen(eltern, lokal.X), Drehen(eltern, lokal.Y), Drehen(eltern, lokal.Z));
        }

        /// <summary>Ein Punkt des Rahmens <paramref name="r"/> im Elternsystem: Ursprung + R · p.</summary>
        internal static double[] Abbilden(IfcRahmen r, double[] p) => Plus(r.Ursprung, Drehen(r, p));

        /// <summary>Eine Richtung des Rahmens <paramref name="r"/> im Elternsystem: R · v (ohne Verschiebung).</summary>
        internal static double[] Drehen(IfcRahmen r, double[] v)
            => new[]
            {
                r.X[0] * v[0] + r.Y[0] * v[1] + r.Z[0] * v[2],
                r.X[1] * v[0] + r.Y[1] * v[1] + r.Z[1] * v[2],
                r.X[2] * v[0] + r.Y[2] * v[1] + r.Z[2] * v[2],
            };

        // ==================================================================
        //  Azimut
        // ==================================================================

        /// <summary>
        /// Der Azimut einer waagerechten Richtung im Modellsystem [°]: gegen +y, im Uhrzeigersinn
        /// (+x = 90°), in [0, 360).
        /// </summary>
        public static double ModellAzimut(double dx, double dy) => Normieren(Math.Atan2(dx, dy) * 180.0 / Math.PI);

        /// <summary>Die Drehung aus <c>TrueNorth</c> [°]: der Modellazimut der Nordrichtung.</summary>
        public static double DrehungAusTrueNorth(double nordX, double nordY) => ModellAzimut(nordX, nordY);

        /// <summary>
        /// Die Drehung aus einer <c>IfcMapConversion</c> [°] = atan2(XAxisOrdinate, XAxisAbscissa): der
        /// Winkel der lokalen x-Achse gegen die Ostachse der Karte, gegen den Uhrzeigersinn — um genau ihn
        /// liegt die Kartennordrichtung im Modellsystem im Uhrzeigersinn von +y.
        /// </summary>
        public static double DrehungAusMapConversion(double abszisse, double ordinate)
            => Normieren(Math.Atan2(ordinate, abszisse) * 180.0 / Math.PI);

        /// <summary>
        /// Die Drehung, die gilt: mit <c>IfcMapConversion</c> deren Drehung und <c>TrueNorth</c> NICHT
        /// addiert; sonst <c>TrueNorth</c>; sonst 0 (Vorgabe [0,1]).
        /// </summary>
        public static double Drehung(double? trueNorthGrad, double? mapConversionGrad)
            => mapConversionGrad ?? trueNorthGrad ?? 0.0;

        /// <summary>Der geografische Azimut einer Modellrichtung [°]: Modellazimut minus Drehung, 0° = Nord, im Uhrzeigersinn.</summary>
        public static double Azimut(double dx, double dy, double drehungGrad) => Normieren(ModellAzimut(dx, dy) - drehungGrad);

        /// <summary>
        /// Das Vorzeichen der Außennormalen längs der lokalen y-Achse einer Wand: −1, wenn die Räume auf
        /// +y liegen, +1, wenn sie auf −y liegen; <c>null</c> = unbestimmt (kein Raum, Räume auf beiden
        /// Seiten, Abstand null oder die y-Achse ohne waagerechten Anteil).
        /// </summary>
        public static int? Aussenseite(IfcRahmen wand, IEnumerable<double[]> raumpunkte)
        {
            double[] yw = Normiert(new[] { wand.Y[0], wand.Y[1], 0.0 });
            if (yw == null) return null;
            int plus = 0, minus = 0;
            foreach (double[] p in raumpunkte)
            {
                double d = (p[0] - wand.Ursprung[0]) * yw[0] + (p[1] - wand.Ursprung[1]) * yw[1];
                if (d > WAAGERECHT_MIN) plus++;
                else if (d < -WAAGERECHT_MIN) minus++;
            }
            if (plus > 0 && minus == 0) return -1;
            if (minus > 0 && plus == 0) return +1;
            return null;
        }

        /// <summary>
        /// Der Azimut der Außennormalen einer Wand [°] (0° = Nord, im Uhrzeigersinn); <c>null</c> = die Seite
        /// ist nicht zu bestimmen.
        /// </summary>
        public static double? Wandazimut(IfcRahmen wand, IEnumerable<double[]> raumpunkte, double drehungGrad)
        {
            int? seite = Aussenseite(wand, raumpunkte);
            if (!seite.HasValue) return null;
            return Azimut(seite.Value * wand.Y[0], seite.Value * wand.Y[1], drehungGrad);
        }

        /// <summary>
        /// Die Umrechnung in die Konvention des Eingangsbauers (Süd 0°, Ost −90°, West +90°, Nord 180°) —
        /// <c>az_EPOS = az_IFC − 180</c>, auf (−180°, 180°] normiert; dieselbe Rechnung wie
        /// <see cref="GebaeudeKlimaweg.AzimutAusDatenbank"/>.
        /// </summary>
        public static double NachSuedkonvention(double azimutNordGrad) => GebaeudeKlimaweg.AzimutAusDatenbank(azimutNordGrad);

        /// <summary>Die Gegenrichtung: aus der Süd-Konvention zurück nach 0° = Nord, in [0, 360).</summary>
        public static double AusSuedkonvention(double azimutSuedGrad) => Normieren(azimutSuedGrad + 180.0);

        /// <summary>Ein Winkel in [0, 360).</summary>
        public static double Normieren(double grad)
        {
            double a = grad % 360.0;
            if (a < 0.0) a += 360.0;
            if (a >= 360.0) a -= 360.0;
            return a;
        }

        // ==================================================================
        //  Vektoren
        // ==================================================================

        /// <summary>Die drei Koordinaten eines Punkts (fehlende = 0); <c>null</c> = kein Punkt.</summary>
        public static double[] Punkt(IIfcCartesianPoint p)
        {
            if (p == null) return null;
            var c = new double[3];
            int i = 0;
            foreach (var k in p.Coordinates)
            {
                if (i >= 3) break;
                c[i++] = IfcEigenschaften.Wert(k);
            }
            for (int j = 0; j < 3; j++) if (double.IsNaN(c[j])) c[j] = 0.0;
            return c;
        }

        /// <summary>Die Richtungsverhältnisse einer Richtung (fehlende = 0); <c>null</c> = keine Richtung.</summary>
        public static double[] Richtung(IIfcDirection d)
        {
            if (d == null) return null;
            var c = new double[3];
            int i = 0;
            foreach (var k in d.DirectionRatios)
            {
                if (i >= 3) break;
                c[i++] = IfcEigenschaften.Wert(k);
            }
            for (int j = 0; j < 3; j++) if (double.IsNaN(c[j])) c[j] = 0.0;
            return c;
        }

        internal static double[] Normiert(double[] v)
        {
            if (v == null) return null;
            double l = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
            return l < 1e-12 ? null : new[] { v[0] / l, v[1] / l, v[2] / l };
        }

        internal static double Punktprodukt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        internal static double[] Kreuz(double[] a, double[] b)
            => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        internal static double[] Mal(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };

        internal static double[] Minus(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };

        internal static double[] Plus(double[] a, double[] b) => new[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] };
    }
}
