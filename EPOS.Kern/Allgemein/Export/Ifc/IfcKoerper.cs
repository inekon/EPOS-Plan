using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die schematischen Körper des IFC-Exports, Stufe S3</b> (Stufe G7e; Datenaustauschkonzept 6.7, 8.4, 14.3)
    /// — die Geometrie aus dem Zonengeometrie-Modell (<see cref="Zonenkoerper"/>), dieselbe Quelle wie die
    /// <c>PolyLoop</c> der gbXML-Stufe 2 und die 3D-Ansicht. Nur <c>Xbim.Ifc4</c>-Typen, keine Geometrie-Engine,
    /// keine boolesche Operation.
    ///
    /// <para><b>Was entsteht:</b> ein <c>IfcGeometricRepresentationContext</c> „Model“ (3D, <c>TrueNorth</c> auf der
    /// Vorgabe +Y = Nord) mit dem Unterkontext „Body“; je schematischem Raum das Prisma über seinem Rechteck
    /// (<c>IfcRectangleProfileDef</c> → <c>IfcExtrudedAreaSolid</c> → <c>IfcShapeRepresentation</c> „Body“/„SweptSolid“
    /// → <c>IfcProductDefinitionShape</c>); je Wand-, Boden- und Deckenfläche eine Platte von
    /// <see cref="PLATTE_M"/> nach außen (aus dem Ring der Fläche — rechteckig als <c>IfcRectangleProfileDef</c>, sonst
    /// <c>IfcArbitraryClosedProfileDef</c> mit <c>IfcPolyline</c>); je Fenster und Tür eine Platte von
    /// <see cref="OEFFNUNG_M"/> vor der Außenseite der Wandplatte, nicht ausgeschnitten.</para>
    ///
    /// <para><b>Platzierung:</b> Jede <c>IfcLocalPlacement</c> liegt im Ursprung ohne Drehung; die Kette
    /// ist Grundstück → Gebäude → Raum bzw. Bauteil. Das Modell steht schon in Weltkoordinaten [m] — die Lage und
    /// die Richtung eines Körpers trägt allein die <c>Position</c> seines <c>IfcExtrudedAreaSolid</c>, so
    /// überlagert sich keine zweite Drehung.</para>
    /// </summary>
    internal sealed class IfcKoerper
    {
        /// <summary>Die Dicke der Platte einer Wand-, Boden- oder Deckenfläche [m], nach außen.</summary>
        internal const double PLATTE_M = 0.1;

        /// <summary>Die Dicke der Platte eines Fensters oder einer Tür [m], vor der Wandplatte.</summary>
        internal const double OEFFNUNG_M = 0.05;

        /// <summary>Der Kennzeichner der Körperdarstellung.</summary>
        internal const string BODY = "Body";

        /// <summary>Die Art der Körperdarstellung.</summary>
        internal const string SWEPT_SOLID = "SweptSolid";

        /// <summary>Toleranz der Rechteckprüfung eines Rings [m].</summary>
        private const double RECHTECK_TOLERANZ_M = 1e-6;

        private readonly IModel _m;
        private IfcCartesianPoint _ursprung;
        private IfcAxis2Placement3D _ursprungLage;

        /// <summary>Legt Kontext und Unterkontext an; nur innerhalb einer offenen Transaktion.</summary>
        internal IfcKoerper(IModel m)
        {
            _m = m ?? throw new ArgumentNullException(nameof(m));
            Kontext = Neu<IfcGeometricRepresentationContext>(k =>
            {
                k.ContextIdentifier = "Model";
                k.ContextType = "Model";
                k.CoordinateSpaceDimension = 3;
                k.Precision = 1e-5;
                k.WorldCoordinateSystem = Neu<IfcAxis2Placement3D>(a => a.Location = Ursprung());
                // TrueNorth auf der Vorgabe: +Y = Nord (6.7).
                k.TrueNorth = Neu<IfcDirection>(d => d.SetXY(0, 1));
            });
            Koerperkontext = Neu<IfcGeometricRepresentationSubContext>(k =>
            {
                k.ContextIdentifier = BODY;
                k.ContextType = "Model";
                k.ParentContext = Kontext;
                k.TargetView = IfcGeometricProjectionEnum.MODEL_VIEW;
            });
        }

        /// <summary>Der Darstellungskontext des Projekts.</summary>
        internal IfcGeometricRepresentationContext Kontext { get; }

        /// <summary>Der Unterkontext „Body“, an dem jede Körperdarstellung hängt.</summary>
        internal IfcGeometricRepresentationSubContext Koerperkontext { get; }

        /// <summary>Die Zahl der geschriebenen Körper (<c>IfcExtrudedAreaSolid</c>).</summary>
        internal int Koerper { get; private set; }

        private T Neu<T>(Action<T> belegen) where T : IInstantiableEntity => _m.Instances.New(belegen);

        private IfcCartesianPoint Ursprung() => _ursprung ??= Neu<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));

        /// <summary>Eine Platzierung im Ursprung ohne Drehung, relativ zu <paramref name="bezug"/> (<c>null</c> = Welt).</summary>
        internal IfcLocalPlacement Platzierung(IfcObjectPlacement bezug)
        {
            _ursprungLage ??= Neu<IfcAxis2Placement3D>(a => a.Location = Ursprung());
            IfcAxis2Placement3D lage = _ursprungLage;
            return Neu<IfcLocalPlacement>(p =>
            {
                if (bezug != null) p.PlacementRelTo = bezug;
                p.RelativePlacement = lage;
            });
        }

        // ------------------------------------------------------------------
        //  Körper
        // ------------------------------------------------------------------

        /// <summary>Das Prisma eines Raums über seinem Rechteck; <c>null</c> = entartet.</summary>
        internal IfcProductDefinitionShape Raum(Raumkoerper k)
        {
            if (k != null && k.AusGrundriss) return Grundriss(k);
            if (k == null || k.Schale.Count < 2 || !(k.HoeheM > 0.0)) return null;
            // Die Decke der Schale trägt die Ecken des Rechtecks in Umlaufrichtung c0, c1, c2, c3.
            IReadOnlyList<double[]> decke = k.Schale[1];
            if (decke.Count != 4) return null;
            double[] c0 = { decke[0][0], decke[0][1], k.BodenM };
            double[] u = Einheit(Differenz(decke[1], decke[0]), nurXY: true);
            if (u == null) return null;
            var ecken = decke.Select(p => new[] { p[0], p[1], k.BodenM }).ToList();
            return Darstellung(Prisma(ecken, c0, new[] { 0.0, 0.0, 1.0 }, u, k.HoeheM, 0.0));
        }

        /// <summary>
        /// <b>Die Prismen aus Grundrissen</b> (HC-5, F10): je Prisma ein <c>IfcExtrudedAreaSolid</c> über einem
        /// <c>IfcArbitraryClosedProfileDef</c> — mit Löchern <c>IfcArbitraryProfileDefWithVoids</c> —, das Profil in den Koordinaten
        /// des Modells, die Lage im Punkt (0, 0, Boden) ohne Drehung, längs +z um die Höhe; alle als Träger derselben Darstellung
        /// „Body“. <c>null</c> = kein Prisma.
        /// </summary>
        private IfcProductDefinitionShape Grundriss(Raumkoerper k)
        {
            var koerper = new List<IfcExtrudedAreaSolid>();
            foreach (Grundrissprisma p in k.Prismen)
            {
                if (!(p.HoeheM > 0.0) || p.Aussen.Count < 3) continue;
                IfcPolyline aussen = Linienzug(p.Aussen);
                List<IfcPolyline> loecher = p.Loecher.Where(l => l.Count >= 3).Select(Linienzug).ToList();
                IfcProfileDef profil = loecher.Count == 0
                    ? Neu<IfcArbitraryClosedProfileDef>(r =>
                    {
                        r.ProfileType = IfcProfileTypeEnum.AREA;
                        r.OuterCurve = aussen;
                    })
                    : Neu<IfcArbitraryProfileDefWithVoids>(r =>
                    {
                        r.ProfileType = IfcProfileTypeEnum.AREA;
                        r.OuterCurve = aussen;
                        foreach (IfcPolyline l in loecher) r.InnerCurves.Add(l);
                    });
                Koerper++;
                double boden = p.BodenM, hoehe = p.HoeheM;
                koerper.Add(Neu<IfcExtrudedAreaSolid>(e =>
                {
                    e.SweptArea = profil;
                    e.Position = Neu<IfcAxis2Placement3D>(a => a.Location = Neu<IfcCartesianPoint>(c => c.SetXYZ(0, 0, Z(boden))));
                    e.ExtrudedDirection = Neu<IfcDirection>(x => x.SetXYZ(0, 0, 1));
                    e.Depth = hoehe;
                }));
            }
            if (koerper.Count == 0) return null;
            IfcShapeRepresentation darstellung = Neu<IfcShapeRepresentation>(s =>
            {
                s.ContextOfItems = Koerperkontext;
                s.RepresentationIdentifier = BODY;
                s.RepresentationType = SWEPT_SOLID;
                foreach (IfcExtrudedAreaSolid x in koerper) s.Items.Add(x);
            });
            return Neu<IfcProductDefinitionShape>(p => p.Representations.Add(darstellung));
        }

        /// <summary>Ein geschlossener Linienzug in der Ebene (Schlusspunkt = Anfangspunkt).</summary>
        private IfcPolyline Linienzug(IReadOnlyList<double[]> ring)
            => Neu<IfcPolyline>(l =>
            {
                foreach (double[] p in ring.Concat(new[] { ring[0] }))
                    l.Points.Add(Neu<IfcCartesianPoint>(c => c.SetXY(Z(p[0]), Z(p[1]))));
            });

        /// <summary>Die Platte einer Wand-, Boden- oder Deckenfläche, <see cref="PLATTE_M"/> nach außen; <c>null</c> = entartet.</summary>
        internal IfcProductDefinitionShape Flaeche(Koerperflaeche f)
            => f == null ? null : Platte(f.EckenM, PLATTE_M, 0.0);

        /// <summary>
        /// HC-5c: <b>die Platten EINES Bauteils</b> an den Kanten eines Grundriss-Prismas — eine Darstellung „Body“ mit je Platte einem
        /// <c>IfcExtrudedAreaSolid</c>. Mit einer Platte dasselbe wie <see cref="Flaeche"/>.
        /// </summary>
        internal IfcProductDefinitionShape Flaechen(IReadOnlyList<Koerperflaeche> flaechen)
        {
            if (flaechen == null || flaechen.Count == 0) return null;
            if (flaechen.Count == 1) return Flaeche(flaechen[0]);
            List<IfcExtrudedAreaSolid> koerper = flaechen.Select(f => Plattenkoerper(f.EckenM, PLATTE_M, 0.0)).Where(k => k != null).ToList();
            if (koerper.Count == 0) return null;
            IfcShapeRepresentation darstellung = Neu<IfcShapeRepresentation>(s =>
            {
                s.ContextOfItems = Koerperkontext;
                s.RepresentationIdentifier = BODY;
                s.RepresentationType = SWEPT_SOLID;
                foreach (IfcExtrudedAreaSolid x in koerper) s.Items.Add(x);
            });
            return Neu<IfcProductDefinitionShape>(p => p.Representations.Add(darstellung));
        }

        /// <summary>
        /// HC-5c: <b>Die Nordrichtung des Kontexts</b> aus der Drehung des Modells gegen Nord [°] (wahrer Azimut = Modellazimut −
        /// Drehung): <c>TrueNorth</c> = (sin d, cos d) — die Umkehrung von <c>IfcPlatzierung.DrehungAusTrueNorth</c>.
        /// </summary>
        internal void Nordrichtung(double drehungGrad)
        {
            double r = drehungGrad * Math.PI / 180.0;
            double x = Math.Round(Math.Sin(r), 12), y = Math.Round(Math.Cos(r), 12);
            Kontext.TrueNorth = Neu<IfcDirection>(d => d.SetXY(Z(x), Z(y)));
        }

        /// <summary>
        /// Die Platte einer Öffnung vor der Wand: der Ring aus <see cref="Zonenkoerper.Oeffnung"/>, um
        /// <see cref="PLATTE_M"/> nach außen versetzt (vor der Wandplatte), <see cref="OEFFNUNG_M"/> dick.
        /// </summary>
        internal IfcProductDefinitionShape Oeffnung(IReadOnlyList<double[]> ring)
            => ring == null ? null : Platte(ring, OEFFNUNG_M, PLATTE_M);

        /// <summary>Eine Platte über einem ebenen Ring, Normale nach außen (Umlaufsinn), versetzt um <paramref name="versatz"/>.</summary>
        private IfcProductDefinitionShape Platte(IReadOnlyList<double[]> ring, double dicke, double versatz)
            => Darstellung(Plattenkoerper(ring, dicke, versatz));

        /// <summary>Der Körper einer Platte über einem ebenen Ring (<see cref="Platte"/>); <c>null</c> = keiner.</summary>
        private IfcExtrudedAreaSolid Plattenkoerper(IReadOnlyList<double[]> ring, double dicke, double versatz)
        {
            if (ring == null || ring.Count < 3) return null;
            double[] n = Normale(ring);
            if (n == null) return null;
            double[] u = null;
            for (int i = 1; i < ring.Count && u == null; i++) u = Einheit(Differenz(ring[i], ring[0]), nurXY: false);
            if (u == null) return null;
            // u senkrecht zur Normalen machen (der Ring ist eben; das hält Rundungsreste aus der Achse).
            u = Einheit(Differenz(u, Mal(n, Punkt(u, n))), nurXY: false);
            if (u == null) return null;
            return Prisma(ring, ring[0], n, u, dicke, versatz);
        }

        /// <summary>
        /// Ein <c>IfcExtrudedAreaSolid</c>: Profil in der Ebene durch <paramref name="ursprung"/> mit den Achsen
        /// <paramref name="u"/> und Achse × u, extrudiert längs <paramref name="achse"/> um <paramref name="tiefe"/>,
        /// die Ebene um <paramref name="versatz"/> längs der Achse verschoben.
        /// </summary>
        private IfcExtrudedAreaSolid Prisma(IReadOnlyList<double[]> ecken, double[] ursprung, double[] achse, double[] u, double tiefe, double versatz)
        {
            if (!(tiefe > 0.0)) return null;
            double[] v = Kreuz(achse, u);
            List<double[]> eben = ecken.Select(p =>
            {
                double[] d = Differenz(p, ursprung);
                return new[] { Punkt(d, u), Punkt(d, v) };
            }).ToList();
            IfcProfileDef profil = Profil(eben);
            if (profil == null) return null;
            double[] lage = { ursprung[0] + achse[0] * versatz, ursprung[1] + achse[1] * versatz, ursprung[2] + achse[2] * versatz };
            Koerper++;
            return Neu<IfcExtrudedAreaSolid>(e =>
            {
                e.SweptArea = profil;
                e.Position = Neu<IfcAxis2Placement3D>(a =>
                {
                    a.Location = Neu<IfcCartesianPoint>(c => c.SetXYZ(Z(lage[0]), Z(lage[1]), Z(lage[2])));
                    a.Axis = Richtung(achse);
                    a.RefDirection = Richtung(u);
                });
                e.ExtrudedDirection = Neu<IfcDirection>(d => d.SetXYZ(0, 0, 1));
                e.Depth = tiefe;
            });
        }

        /// <summary>Das Profil eines ebenen Rings: ein Rechteck mit Kante längs der x-Achse, sonst ein Polygon.</summary>
        private IfcProfileDef Profil(List<double[]> eben)
        {
            if (eben.Count == 4 && Rechteck(eben, out double breite, out double hoehe, out double[] mitte))
                return Neu<IfcRectangleProfileDef>(r =>
                {
                    r.ProfileType = IfcProfileTypeEnum.AREA;
                    r.XDim = breite;
                    r.YDim = hoehe;
                    r.Position = Neu<IfcAxis2Placement2D>(a => a.Location = Neu<IfcCartesianPoint>(c => c.SetXY(Z(mitte[0]), Z(mitte[1]))));
                });
            if (!(Math.Abs(Flaeche2D(eben)) > RECHTECK_TOLERANZ_M * RECHTECK_TOLERANZ_M)) return null;
            return Neu<IfcArbitraryClosedProfileDef>(r =>
            {
                r.ProfileType = IfcProfileTypeEnum.AREA;
                r.OuterCurve = Neu<IfcPolyline>(l =>
                {
                    foreach (double[] p in eben.Concat(new[] { eben[0] }))
                        l.Points.Add(Neu<IfcCartesianPoint>(c => c.SetXY(Z(p[0]), Z(p[1]))));
                });
            });
        }

        /// <summary>Ist der Ring ein Rechteck mit erster Kante längs x? Dann Breite, Höhe und Mitte.</summary>
        internal static bool Rechteck(IReadOnlyList<double[]> p, out double breite, out double hoehe, out double[] mitte)
        {
            breite = p[1][0] - p[0][0];
            hoehe = p[3][1] - p[0][1];
            mitte = new[] { (p[0][0] + p[2][0]) / 2.0, (p[0][1] + p[2][1]) / 2.0 };
            bool ok = Math.Abs(p[1][1] - p[0][1]) <= RECHTECK_TOLERANZ_M && Math.Abs(p[3][0] - p[0][0]) <= RECHTECK_TOLERANZ_M
                      && Math.Abs(p[2][0] - p[1][0]) <= RECHTECK_TOLERANZ_M && Math.Abs(p[2][1] - p[3][1]) <= RECHTECK_TOLERANZ_M;
            breite = Math.Abs(breite);
            hoehe = Math.Abs(hoehe);
            return ok && breite > RECHTECK_TOLERANZ_M && hoehe > RECHTECK_TOLERANZ_M;
        }

        private IfcProductDefinitionShape Darstellung(IfcExtrudedAreaSolid koerper)
        {
            if (koerper == null) return null;
            IfcShapeRepresentation darstellung = Neu<IfcShapeRepresentation>(s =>
            {
                s.ContextOfItems = Koerperkontext;
                s.RepresentationIdentifier = BODY;
                s.RepresentationType = SWEPT_SOLID;
                s.Items.Add(koerper);
            });
            return Neu<IfcProductDefinitionShape>(p => p.Representations.Add(darstellung));
        }

        private IfcDirection Richtung(double[] r) => Neu<IfcDirection>(d => d.SetXYZ(Z(r[0]), Z(r[1]), Z(r[2])));

        // ------------------------------------------------------------------
        //  Vektorrechnung
        // ------------------------------------------------------------------

        /// <summary>Ohne negative Null: dieselbe Eingabe gibt dieselben Bytes.</summary>
        private static double Z(double v) => v + 0.0;

        private static double[] Differenz(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };

        private static double[] Mal(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };

        private static double Punkt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        private static double[] Kreuz(double[] a, double[] b)
            => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        private static double[] Einheit(double[] a, bool nurXY)
        {
            double[] x = nurXY ? new[] { a[0], a[1], 0.0 } : a;
            double l = Math.Sqrt(Punkt(x, x));
            return l > 1e-12 ? new[] { x[0] / l, x[1] / l, x[2] / l } : null;
        }

        /// <summary>Die Normale eines ebenen Rings nach Newell (Umlaufsinn rechtshändig), normiert; <c>null</c> = entartet.</summary>
        internal static double[] Normale(IReadOnlyList<double[]> ring)
        {
            double nx = 0.0, ny = 0.0, nz = 0.0;
            for (int i = 0; i < ring.Count; i++)
            {
                double[] a = ring[i], b = ring[(i + 1) % ring.Count];
                nx += (a[1] - b[1]) * (a[2] + b[2]);
                ny += (a[2] - b[2]) * (a[0] + b[0]);
                nz += (a[0] - b[0]) * (a[1] + b[1]);
            }
            return Einheit(new[] { nx, ny, nz }, nurXY: false);
        }

        private static double Flaeche2D(List<double[]> p)
        {
            double s = 0.0;
            for (int i = 0; i < p.Count; i++)
            {
                double[] a = p[i], b = p[(i + 1) % p.Count];
                s += a[0] * b[1] - b[0] * a[1];
            }
            return s / 2.0;
        }
    }
}
