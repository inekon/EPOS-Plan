using System;
using System.Collections.Generic;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Importproben der Bauteilkörper als Rechengröße</b> (Abstimmung G5, Teil G5-1): Hüllbauteile ohne bzw. mit
    /// Mengensatz, deren Fläche und Orientierung aus dem Körper folgen. Selbst erzeugt, neutrale Namen, runde Werte,
    /// Längen in Millimetern; abgelegt unter <c>Referenzlaeufe/Importproben/</c>, gehalten von
    /// <see cref="IfcBauteilkoerperTests"/> (byte-gleich neu erzeugbar).
    ///
    /// <para><b>Das Wandhaus</b> (<see cref="Wandhaus"/>): ein Raum 10 × 8 m (Körper 2,5 m hoch) und vier Außenwände als
    /// <c>IfcExtrudedAreaSolid</c> (2,5 m hoch, 0,3 m dick, außen erklärt, ohne Raumgrenzen): Süd, West und Nord mit
    /// Rechteckprofil, Ost als gegliederte Wand mit L-Grundriss (<c>IfcArbitraryClosedProfileDef</c>: 8 m Wand und 2 m
    /// Flügel nach Ost am Nordende). Das Gebäude ist um 90° gedreht (x nach +y) und um (10, 5) m versetzt, das Geschoss
    /// liegt 3 m hoch, TrueNorth [−2, 1, 0]. Mit Mengensätzen ist dieselbe Datei die Gegenprobe: Nord mit 26,25 m² statt
    /// 25 m² (5 %), die übrigen innerhalb 2 %.</para>
    ///
    /// <para><b>Das Körperhaus</b> (<see cref="Koerperhaus"/>): Raum 10 × 8 m; Süd als <c>IfcFacetedBrep</c>, Ost als
    /// <c>IfcMappedItem</c> (Würfel 1 m, gestreckt 8 : 1 : 2,5 bei 0,3 m Dicke), Nord als
    /// <c>IfcShellBasedSurfaceModel</c>, West als <c>IfcTriangulatedFaceSet</c>; ein Dach (<c>IfcRoof</c>) aus einer Platte
    /// <c>ROOF</c> als schräge Extrusion (Neigung 3 : 4, 10 × 10 m in der Dachebene, nach Süd geneigt) und eine Bodenplatte
    /// 10 × 8 m — ohne Mengensätze, TrueNorth [0, 1, 0].</para>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        /// <summary>Die Bauteilkörperproben: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Bauteilkoerperproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["ifc4_g5_wand_extrusion.ifc"] = Wandhaus("ifc4_g5_wand_extrusion.ifc", mengen: false),
                ["ifc4_g5_mengen_gegenprobe.ifc"] = Wandhaus("ifc4_g5_mengen_gegenprobe.ifc", mengen: true),
                ["ifc4_g5_wand_brep_mapped.ifc"] = Koerperhaus(),
                ["ifc4_g5_flachdach_teile.ifc"] = Flachdachhaus(),
            };

        /// <summary>Die Oberlichter des Flachdachs [mm]: eine Spalte x = 8 … 10 m, drei Löcher 2 × 2 m.</summary>
        internal static readonly (double X0, double Y0, double X1, double Y1)[] OBERLICHTER =
        {
            (8000, 1000, 10000, 3000), (8000, 5000, 10000, 7000), (8000, 9000, 10000, 11000),
        };

        /// <summary>
        /// <b>Das Flachdachhaus</b> (Teile ohne Darstellung, Löcher in der Fläche): Raum 20 × 12 m; „Dach A“ als
        /// <c>IfcShellBasedSurfaceModel</c> mit <c>IfcOpenShell</c> — Ober- und Unterseite 20 × 12 m mit drei Oberlichtern
        /// 2 × 2 m als <c>IfcFaceBound</c> (gleiche größte x-Koordinate, die Oberseite beginnt mit einer Kante längs x),
        /// vier Seitenflächen, 0,3 m dick: netto 228 m², brutto 240 m². Sein Mengensatz trägt nur einen Teil
        /// (100/96 m²), die Teile „Dach A-1“ (80/76 m²) und „Dach A-2“ (60/56 m²) stehen als <c>IfcRoof</c> ohne
        /// Darstellung daneben. „Dach B“ ist ein Kasten 5 × 8 m ohne Teile mit 41 m² im Mengensatz (2,4 %).
        /// </summary>
        private static byte[] Flachdachhaus()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_g5_flachdach_teile.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Flachdachhaus", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Halle", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (20000, 0), (20000, 12000), (0, 12000));

                const double Z0 = 0, Z1 = 300;
                var oben = new List<(double, double, double)[]>();
                var unten = new List<(double, double, double)[]>();
                foreach ((double x0, double y0, double x1, double y1) in OBERLICHTER)
                {
                    oben.Add(new[] { (x0, y0, Z1), (x0, y1, Z1), (x1, y1, Z1), (x1, y0, Z1) });
                    unten.Add(new[] { (x0, y0, Z0), (x1, y0, Z0), (x1, y1, Z0), (x0, y1, Z0) });
                }
                var flaechen = new List<((double, double, double)[] Aussen, List<(double, double, double)[]> Loecher)>
                {
                    (new[] { (0.0, 0.0, Z1), (20000.0, 0.0, Z1), (20000.0, 12000.0, Z1), (0.0, 12000.0, Z1) }, oben),
                    (new[] { (0.0, 0.0, Z0), (0.0, 12000.0, Z0), (20000.0, 12000.0, Z0), (20000.0, 0.0, Z0) }, unten),
                    (new[] { (0.0, 0.0, Z0), (20000.0, 0.0, Z0), (20000.0, 0.0, Z1), (0.0, 0.0, Z1) }, null),
                    (new[] { (20000.0, 0.0, Z0), (20000.0, 12000.0, Z0), (20000.0, 12000.0, Z1), (20000.0, 0.0, Z1) }, null),
                    (new[] { (20000.0, 12000.0, Z0), (0.0, 12000.0, Z0), (0.0, 12000.0, Z1), (20000.0, 12000.0, Z1) }, null),
                    (new[] { (0.0, 12000.0, Z0), (0.0, 0.0, Z0), (0.0, 0.0, Z1), (0.0, 12000.0, Z1) }, null),
                };
                IIfcRoof dach = b.Flachdachteil(s, "Dach A", 3000, 100, 96);
                b.Koerper(dach, "SurfaceModel", b.Lochflaechenmodell(flaechen));
                b.Flachdachteil(s, "Dach A-1", 3000, 80, 76);
                b.Flachdachteil(s, "Dach A-2", 3000, 60, 56);
                IIfcRoof dachB = b.Flachdachteil(s, "Dach B", 3000, 41, null);
                b.Koerper(dachB, "SurfaceModel", b.Kasten(25000, 0, 0, 30000, 8000, 300));
                return b.Speichern();
            }
        }

        /// <summary>Der L-Grundriss der Ostwand im System der Wand [mm]: Wand 8 m, Flügel 2 m nach außen am Ende.</summary>
        internal static readonly (double X, double Y)[] OSTWAND_L =
        {
            (0, 0), (0, -300), (7700, -300), (7700, -2000), (8000, -2000), (8000, 0),
        };

        private static byte[] Wandhaus(string datei, bool mengen)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { -2.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Wandhaus", null);
                b.Gedreht(g);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 3000);
                IIfcSpace r = b.Raum(s, "0.01", "Wohnen", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (10000, 0), (10000, 8000), (0, 8000));

                IIfcWall sued = b.Huellwand(s, "Wand Süd", 0, 0, 1, 0, mengen ? 25.2 : (double?)null);
                b.Koerper(sued, "SweptSolid", b.Extrusion(b.Rechteckprofil(10000, 300, 5000, -150), 2500));
                IIfcWall ost = b.Huellwand(s, "Wand Ost", 10000, 0, 0, 1, mengen ? 24.25 : (double?)null);
                b.Koerper(ost, "SweptSolid", b.Extrusion(b.Polygonprofil(OSTWAND_L), 2500));
                IIfcWall nord = b.Huellwand(s, "Wand Nord", 10000, 8000, -1, 0, mengen ? 26.25 : (double?)null);
                b.Koerper(nord, "SweptSolid", b.Extrusion(b.Rechteckprofil(10000, 300, 5000, -150), 2500));
                IIfcWall west = b.Huellwand(s, "Wand West", 0, 8000, 0, -1, mengen ? 19.9 : (double?)null);
                b.Koerper(west, "SweptSolid", b.Extrusion(b.Rechteckprofil(8000, 300, 4000, -150), 2500));
                return b.Speichern();
            }
        }

        private static byte[] Koerperhaus()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_g5_wand_brep_mapped.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Körperhaus", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Wohnen", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (10000, 0), (10000, 8000), (0, 8000));

                IIfcWall sued = b.Huellwand(s, "Wand Süd", 0, 0, 1, 0, null);
                b.Koerper(sued, "Brep", b.Flaechenkoerper(Quader(0, -300, 0, 10000, 0, 2500), geschlossen: true));
                IIfcWall ost = b.Huellwand(s, "Wand Ost", 10000, 0, 0, 1, null);
                b.Koerper(ost, "MappedRepresentation", b.Gestreckt(8, 1, 2.5));
                IIfcWall nord = b.Huellwand(s, "Wand Nord", 10000, 8000, -1, 0, null);
                b.Koerper(nord, "SurfaceModel", b.Kasten(0, -300, 0, 10000, 0, 2500));
                // Die Westwand mit der lokalen y-Achse nach außen: der Quader des Dreiecksnetzes liegt bei y = 0 … 300.
                IIfcWall west = b.Huellwand(s, "Wand West", 0, 0, 0, 1, null);
                b.Koerper(west, "Tessellation", b.Dreiecksnetz(8000, 300, 2500));

                IIfcSlab dachplatte = b.Koerperplatte(s, "Dachplatte", IfcSlabTypeEnum.ROOF, 0);
                b.Koerper(dachplatte, "SweptSolid", b.Schraeg(10000, 10000, 200, 2500));
                b.Dachganzes(s, "Dach", dachplatte);
                IIfcSlab boden = b.Koerperplatte(s, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, -300);
                b.Koerper(boden, "SweptSolid", b.Extrusion(b.Rechteckprofil(10000, 8000, 5000, 4000), 300));
                return b.Speichern();
            }
        }

        /// <summary>Die sechs Flächen eines achsparallelen Quaders [mm], Umlauf nach außen.</summary>
        internal static (double X, double Y, double Z)[][] Quader(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            (double, double, double) P(int i, int j, int k) => (i == 0 ? x0 : x1, j == 0 ? y0 : y1, k == 0 ? z0 : z1);
            return new[]
            {
                new[] { P(0, 0, 0), P(0, 1, 0), P(1, 1, 0), P(1, 0, 0) },
                new[] { P(0, 0, 1), P(1, 0, 1), P(1, 1, 1), P(0, 1, 1) },
                new[] { P(0, 0, 0), P(1, 0, 0), P(1, 0, 1), P(0, 0, 1) },
                new[] { P(1, 0, 0), P(1, 1, 0), P(1, 1, 1), P(1, 0, 1) },
                new[] { P(1, 1, 0), P(0, 1, 0), P(0, 1, 1), P(1, 1, 1) },
                new[] { P(0, 1, 0), P(0, 0, 0), P(0, 0, 1), P(0, 1, 1) },
            };
        }

        private sealed partial class Bau
        {
            /// <summary>Eine Außenwand (<c>Pset_WallCommon.IsExternal</c>) ohne Raumgrenze, im Geschoss enthalten.</summary>
            public IIfcWall Huellwand(IIfcBuildingStorey s, string name, double x, double y, double rx, double ry, double? bruttoM2)
            {
                IIfcWall w = Wurzel<IIfcWall>("IfcWall", name);
                w.PredefinedType = IfcWallTypeEnum.STANDARD;
                w.ObjectPlacement = Platzierung(s.ObjectPlacement, x, y, 0, rx, ry);
                Enthalten(s, w);
                Satz(w, "Pset_WallCommon", ("IsExternal", new IfcBoolean(true)));
                if (bruttoM2.HasValue) Mengen(w, "Qto_WallBaseQuantities", Flaeche("GrossSideArea", bruttoM2.Value));
                return w;
            }

            /// <summary>Eine außen erklärte Platte ohne Mengensatz und ohne Raumgrenze auf der Höhe <paramref name="z"/> [mm].</summary>
            public IIfcSlab Koerperplatte(IIfcBuildingStorey s, string name, IfcSlabTypeEnum art, double z)
            {
                IIfcSlab p = Wurzel<IIfcSlab>("IfcSlab", name);
                p.PredefinedType = art;
                p.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, z);
                Enthalten(s, p);
                Satz(p, "Pset_SlabCommon", ("IsExternal", new IfcBoolean(true)));
                return p;
            }

            /// <summary>
            /// Ein Flachdach (<c>IfcRoof FLAT_ROOF</c>, außen erklärt) auf der Höhe <paramref name="z"/> [mm] ohne Darstellung
            /// und ohne Raumgrenze, Mengensatz <c>Qto_RoofBaseQuantities</c> mit <c>GrossArea</c> und, wenn angegeben,
            /// <c>NetArea</c>.
            /// </summary>
            public IIfcRoof Flachdachteil(IIfcBuildingStorey s, string name, double z, double bruttoM2, double? nettoM2)
            {
                IIfcRoof d = Wurzel<IIfcRoof>("IfcRoof", name);
                d.PredefinedType = IfcRoofTypeEnum.FLAT_ROOF;
                d.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, z);
                Enthalten(s, d);
                Satz(d, "Pset_RoofCommon", ("IsExternal", new IfcBoolean(true)));
                if (nettoM2.HasValue) Mengen(d, "Qto_RoofBaseQuantities", Flaeche("GrossArea", bruttoM2), Flaeche("NetArea", nettoM2.Value));
                else Mengen(d, "Qto_RoofBaseQuantities", Flaeche("GrossArea", bruttoM2));
                return d;
            }

            /// <summary>
            /// Ein <c>IfcShellBasedSurfaceModel</c> mit <c>IfcOpenShell</c> aus ebenen Flächen [mm]: je Fläche der äußere Ring
            /// als <c>IfcFaceOuterBound</c> und die Löcher als <c>IfcFaceBound</c> (Umlauf gegen den äußeren Ring), alle mit
            /// <c>Orientation</c> = wahr.
            /// </summary>
            public IIfcRepresentationItem Lochflaechenmodell(IEnumerable<((double X, double Y, double Z)[] Aussen, List<(double X, double Y, double Z)[]> Loecher)> flaechen)
            {
                IIfcPolyLoop Ring((double X, double Y, double Z)[] punkte)
                {
                    IIfcPolyLoop ring = N<IIfcPolyLoop>("IfcPolyLoop");
                    foreach ((double x, double y, double z) in punkte) ring.Polygon.Add(Punkt(x, y, z));
                    return ring;
                }
                IIfcConnectedFaceSet schale = N<IIfcConnectedFaceSet>("IfcOpenShell");
                foreach (((double X, double Y, double Z)[] aussen, List<(double X, double Y, double Z)[]> loecher) in flaechen)
                {
                    IIfcFace flaeche = N<IIfcFace>("IfcFace");
                    IIfcFaceOuterBound rand = N<IIfcFaceOuterBound>("IfcFaceOuterBound");
                    rand.Bound = Ring(aussen);
                    rand.Orientation = true;
                    flaeche.Bounds.Add(rand);
                    foreach ((double X, double Y, double Z)[] loch in loecher ?? new List<(double X, double Y, double Z)[]>())
                    {
                        IIfcFaceBound innen = N<IIfcFaceBound>("IfcFaceBound");
                        innen.Bound = Ring(loch);
                        innen.Orientation = true;
                        flaeche.Bounds.Add(innen);
                    }
                    schale.CfsFaces.Add(flaeche);
                }
                IIfcShellBasedSurfaceModel m = N<IIfcShellBasedSurfaceModel>("IfcShellBasedSurfaceModel");
                m.SbsmBoundary.Add((IIfcShell)schale);
                return m;
            }

            /// <summary>Ein Dach (<c>IfcRoof</c>) ohne eigene Darstellung, das die Platte über <c>IfcRelAggregates</c> trägt.</summary>
            public IIfcRoof Dachganzes(IIfcBuildingStorey s, string name, IIfcSlab platte)
            {
                IIfcRoof d = Wurzel<IIfcRoof>("IfcRoof", name);
                d.PredefinedType = IfcRoofTypeEnum.SHED_ROOF;
                d.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, d);
                Satz(d, "Pset_RoofCommon", ("IsExternal", new IfcBoolean(true)));
                Zerlegen(d, platte);
                return d;
            }

            /// <summary>
            /// Eine schräge Extrusion: Rechteck <paramref name="breite"/> × <paramref name="tiefe"/> in der Ebene mit der
            /// Normalen (0, −0,6, 0,8) — Neigung 3 : 4 nach Süd —, ab (0, 0, <paramref name="z"/>), um <paramref name="dicke"/>
            /// längs der Normalen extrudiert.
            /// </summary>
            public IIfcExtrudedAreaSolid Schraeg(double breite, double tiefe, double dicke, double z)
            {
                IIfcAxis2Placement3D lage = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                lage.Location = Punkt(0, 0, z);
                lage.Axis = Richtung(0, -0.6, 0.8);
                lage.RefDirection = Richtung(1, 0, 0);
                IIfcExtrudedAreaSolid e = N<IIfcExtrudedAreaSolid>("IfcExtrudedAreaSolid");
                e.SweptArea = Rechteckprofil(breite, tiefe, breite / 2.0, tiefe / 2.0);
                e.Position = lage;
                e.ExtrudedDirection = Richtung(0, 0, 1);
                e.Depth = new IfcPositiveLengthMeasure(dicke);
                return e;
            }

            /// <summary>
            /// Ein <c>IfcMappedItem</c>: Quelle ein Quader 1000 × 300 × 1000 mm (Rechteckprofil bei y = −300 … 0), Ziel ohne
            /// Versatz, gestreckt <paramref name="sx"/> : <paramref name="sy"/> : <paramref name="sz"/>.
            /// </summary>
            public IIfcRepresentationItem Gestreckt(double sx, double sy, double sz)
            {
                IIfcShapeRepresentation quelle = N<IIfcShapeRepresentation>("IfcShapeRepresentation");
                quelle.ContextOfItems = _kontext;
                quelle.RepresentationIdentifier = new IfcLabel("Body");
                quelle.RepresentationType = new IfcLabel("SweptSolid");
                quelle.Items.Add(Extrusion(Rechteckprofil(1000, 300, 500, -150), 1000));
                IIfcRepresentationMap karte = N<IIfcRepresentationMap>("IfcRepresentationMap");
                IIfcAxis2Placement3D ursprung = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                ursprung.Location = Punkt(0, 0, 0);
                karte.MappingOrigin = ursprung;
                karte.MappedRepresentation = quelle;
                IIfcCartesianTransformationOperator3DnonUniform ziel = N<IIfcCartesianTransformationOperator3DnonUniform>("IfcCartesianTransformationOperator3DnonUniform");
                ziel.LocalOrigin = Punkt(0, 0, 0);
                ziel.Scale = new IfcReal(sx);
                ziel.Scale2 = new IfcReal(sy);
                ziel.Scale3 = new IfcReal(sz);
                IIfcMappedItem m = N<IIfcMappedItem>("IfcMappedItem");
                m.MappingSource = karte;
                m.MappingTarget = ziel;
                return m;
            }
        }
    }
}
