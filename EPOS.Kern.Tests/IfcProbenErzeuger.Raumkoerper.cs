using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Importproben der Raumkörper</b> (Stufe G7f-1, Probe 29; Datenaustauschkonzept 15.2 und 15.7): je
    /// Darstellungsart eine Kleinstdatei mit einem Gebäude, einem Geschoss und einem Raum (der Bogen-Probe: drei Räume)
    /// samt Darstellung „Body“, Längen in Millimetern. Selbst erzeugt, neutrale Namen, runde Werte; abgelegt unter
    /// <c>Referenzlaeufe/Importproben/</c>, gehalten von <see cref="IfcRaumkoerperTests"/> (byte-gleich neu erzeugbar).
    /// Der Raum steht bei (1000, 2000, 0) mm im Geschoss; die Platzierungsprobe dreht das Gebäude um 90° und versetzt
    /// es, das Geschoss liegt 3000 mm höher.
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        /// <summary>Die Raumkörperproben: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Raumkoerperproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["ifc4_koerper_extrusion_polygon.ifc"] = Koerperprobe("ifc4_koerper_extrusion_polygon.ifc", XbimSchemaVersion.Ifc4, "SweptSolid",
                    b => new[] { b.Extrusion(b.Polygonprofil(new[] { (0.0, 0.0), (5000.0, 0.0), (5000.0, 2000.0), (2000.0, 2000.0), (2000.0, 4000.0), (0.0, 4000.0) }), 3000) }),
                ["ifc4_koerper_extrusion_bogen.ifc"] = Bogenprobe(),
                ["ifc4_koerper_extrusion_loch.ifc"] = Koerperprobe("ifc4_koerper_extrusion_loch.ifc", XbimSchemaVersion.Ifc4, "SweptSolid",
                    b => new[] { b.Extrusion(b.Lochprofil(), 3000) }),
                ["ifc2x3_koerper_brep.ifc"] = Koerperprobe("ifc2x3_koerper_brep.ifc", XbimSchemaVersion.Ifc2X3, "Brep",
                    b => new[] { b.Brep(4000, 3000, 2500, offen: false) }),
                ["ifc4_koerper_dreiecksnetz.ifc"] = Koerperprobe("ifc4_koerper_dreiecksnetz.ifc", XbimSchemaVersion.Ifc4, "Tessellation",
                    b => new[] { b.Dreiecksnetz(4000, 3000, 2500) }),
                ["ifc4_koerper_vieleckssatz.ifc"] = Koerperprobe("ifc4_koerper_vieleckssatz.ifc", XbimSchemaVersion.Ifc4, "Tessellation",
                    b => new[] { b.Vieleckssatz(4000, 3000, 2500) }),
                ["ifc4_koerper_abgebildet.ifc"] = Koerperprobe("ifc4_koerper_abgebildet.ifc", XbimSchemaVersion.Ifc4, "MappedRepresentation",
                    b => new[] { b.Abgebildet() }),
                ["ifc4_koerper_beschnitt.ifc"] = Koerperprobe("ifc4_koerper_beschnitt.ifc", XbimSchemaVersion.Ifc4, "Clipping",
                    b => new[] { b.Beschnitten() }),
                ["ifc4_koerper_offen.ifc"] = Koerperprobe("ifc4_koerper_offen.ifc", XbimSchemaVersion.Ifc4, "SurfaceModel",
                    b => new[] { b.Brep(4000, 3000, 2500, offen: true) }),
                ["ifc4_koerper_advancedbrep.ifc"] = Koerperprobe("ifc4_koerper_advancedbrep.ifc", XbimSchemaVersion.Ifc4, "AdvancedBrep",
                    b => new[] { b.AdvancedBrep() }),
                ["ifc4_koerper_platzierung.ifc"] = Koerperprobe("ifc4_koerper_platzierung.ifc", XbimSchemaVersion.Ifc4, "SweptSolid",
                    b => new[] { b.Extrusion(b.Rechteckprofil(4000, 3000, 2000, 1500), 2500) }, gedreht: true),
                ["ifc4_koerper_nachbarn.ifc"] = Koerpernachbarn("ifc4_koerper_nachbarn.ifc", grenzen: false),
                ["ifc4_koerper_nachbarn_grenzen.ifc"] = Koerpernachbarn("ifc4_koerper_nachbarn_grenzen.ifc", grenzen: true),
                ["ifc4_koerper_bauteile.ifc"] = Koerperbauteile(),
                ["ifc4_koerper_grundriss_stufe.ifc"] = Grundrissprobe("ifc4_koerper_grundriss_stufe.ifc", 15, 45,
                    b => b.Flaechenkoerper(STUFENKOERPER, geschlossen: true)),
                ["ifc4_koerper_grundriss_ohne_boden.ifc"] = Grundrissprobe("ifc4_koerper_grundriss_ohne_boden.ifc", 12, 36,
                    b => b.Flaechenkoerper(KASTEN_OHNE_BODEN, geschlossen: false)),
            };

        /// <summary>
        /// <b>L-Form mit Stufe</b> (HC-5, Konzept HottCAD-Verbund 11.2) [mm, im System des Raums]: Teil A 4 × 3 m mit Boden auf
        /// 0, Teil B 2 × 3 m nördlich daran mit Boden auf 0,3 m, Decke durchgehend auf 3 m; Umlauf jeder Fläche nach außen.
        /// Grundriss 18 m² gegen 15 m² des Mengensatzes (+20 %).
        /// </summary>
        private static readonly (double X, double Y, double Z)[][] STUFENKOERPER =
        {
            new[] { (0.0, 0.0, 0.0), (0.0, 3000.0, 0.0), (4000.0, 3000.0, 0.0), (4000.0, 0.0, 0.0) },          // Boden A
            new[] { (0.0, 3000.0, 300.0), (0.0, 6000.0, 300.0), (2000.0, 6000.0, 300.0), (2000.0, 3000.0, 300.0) }, // Boden B
            new[] { (0.0, 3000.0, 0.0), (0.0, 3000.0, 300.0), (2000.0, 3000.0, 300.0), (2000.0, 3000.0, 0.0) }, // Setzstufe
            new[] { (0.0, 0.0, 3000.0), (4000.0, 0.0, 3000.0), (4000.0, 3000.0, 3000.0), (0.0, 3000.0, 3000.0) }, // Decke A
            new[] { (0.0, 3000.0, 3000.0), (2000.0, 3000.0, 3000.0), (2000.0, 6000.0, 3000.0), (0.0, 6000.0, 3000.0) }, // Decke B
            new[] { (0.0, 0.0, 0.0), (4000.0, 0.0, 0.0), (4000.0, 0.0, 3000.0), (0.0, 0.0, 3000.0) },          // Süd
            new[] { (4000.0, 0.0, 0.0), (4000.0, 3000.0, 0.0), (4000.0, 3000.0, 3000.0), (4000.0, 0.0, 3000.0) }, // Ost A
            new[] { (4000.0, 3000.0, 0.0), (2000.0, 3000.0, 0.0), (2000.0, 3000.0, 3000.0), (4000.0, 3000.0, 3000.0) }, // Nord A
            new[] { (2000.0, 3000.0, 300.0), (2000.0, 6000.0, 300.0), (2000.0, 6000.0, 3000.0), (2000.0, 3000.0, 3000.0) }, // Ost B
            new[] { (2000.0, 6000.0, 300.0), (0.0, 6000.0, 300.0), (0.0, 6000.0, 3000.0), (2000.0, 6000.0, 3000.0) }, // Nord B
            new[] { (0.0, 3000.0, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, 3000.0), (0.0, 3000.0, 3000.0) },          // West A
            new[] { (0.0, 6000.0, 300.0), (0.0, 3000.0, 300.0), (0.0, 3000.0, 3000.0), (0.0, 6000.0, 3000.0) }, // West B
        };

        /// <summary>Eine Schale 4 × 3 × 3 m ohne Boden [mm] (HC-5): Wände und Decke, Umlauf nach außen.</summary>
        private static readonly (double X, double Y, double Z)[][] KASTEN_OHNE_BODEN =
        {
            new[] { (0.0, 0.0, 3000.0), (4000.0, 0.0, 3000.0), (4000.0, 3000.0, 3000.0), (0.0, 3000.0, 3000.0) }, // Decke
            new[] { (0.0, 0.0, 0.0), (4000.0, 0.0, 0.0), (4000.0, 0.0, 3000.0), (0.0, 0.0, 3000.0) },          // Süd
            new[] { (4000.0, 0.0, 0.0), (4000.0, 3000.0, 0.0), (4000.0, 3000.0, 3000.0), (4000.0, 0.0, 3000.0) }, // Ost
            new[] { (4000.0, 3000.0, 0.0), (0.0, 3000.0, 0.0), (0.0, 3000.0, 3000.0), (4000.0, 3000.0, 3000.0) }, // Nord
            new[] { (0.0, 3000.0, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, 3000.0), (0.0, 3000.0, 3000.0) },          // West
        };

        /// <summary>Ein Raum (Raumfläche und Volumen des Mengensatzes gegeben) mit einem Körper aus Flächen, ohne Raumgrenzen.</summary>
        private static byte[] Grundrissprobe(string datei, double flaeche, double volumen, Func<Bau, IIfcRepresentationItem> koerper)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(null, karte: false);
                IIfcBuilding g = b.Gebaeude("Probengebäude", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Raum", 1000, 2000, flaeche, 3000, volumen, beheizt: true);
                b.Koerper(r, "Brep", koerper(b));
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Körper der Hüllbauteile</b> (Konzept HottCAD-Verbund 3.2 und 4.2): ein Geschoss, Nord = +y, zwei Räume als
        /// <c>IfcFacetedBrep</c> nach dem Muster eines CAD-Exports (Raumbezüge, keine Raumgrenzen): „Wohnen“ 5 × 4 × 3 m im
        /// Ursprung (beheizt, 20 °C) und „Kammer“ 4 × 4 × 3 m bei x = 5,24 m (unbeheizt). Die „Außenwand Süd“ (0,3 m vor
        /// beiden Räumen, außen, Orientierung 180°) trägt einen Körper als <c>IfcShellBasedSurfaceModel</c> aus
        /// <c>IfcPolyLoop</c>, ebenso das „Fenster Süd“ (1,5 × 1,2 m) in ihr; die „Innenwand“ (gegen unbeheizt)
        /// und die „Bodenplatte“ (Erdreich) tragen keinen; ein Dach fehlt (die Decken fallen auf die Normale zurück).
        /// </summary>
        private static byte[] Koerperbauteile()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_koerper_bauteile.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Bauteilkörper", null);
                IIfcBuildingStorey eg = b.GeschossEnthalten(g, "EG", 0);
                IIfcWall sued = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Außenwand Süd", "btaOutside", true, 0.3, null, 180, 27.72, 26.82, null, null);
                b.Koerper(sued, "SurfaceModel", b.Kasten(0, -300, 0, 9240, 0, 3000));
                IIfcWindow fenster = b.CadFenster(sued, eg, "Fenster Süd", (1500, 1200), 1.8, 1.1, null);
                b.Koerper(fenster, "SurfaceModel", b.Kasten(1000, -300, 900, 2500, 0, 2100));
                IIfcWall innen = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Innenwand", "btaUnHeated", true, 0.5, null, null, 12, 12, null, null);
                IIfcSlab boden = b.CadBauteil<IIfcSlab>(eg, "IfcSlab", "Bodenplatte", "btaGround", true, 0.4, null, null, 36, 36, null, null);

                IIfcSpace wohnen = b.RaumEnthalten(eg, "Wohnen", 0, 0, 20, 60, 3000, zerlegt: false);
                IIfcSpace kammer = b.RaumEnthalten(eg, "Kammer", 5240, 0, 16, 48, 3000, zerlegt: false);
                b.Koerper(wohnen, "Brep", b.Brep(5000, 4000, 3000, offen: false));
                b.Koerper(kammer, "Brep", b.Brep(4000, 4000, 3000, offen: false));
                b.Raumangaben(wohnen, "bhtHeated", 20.0, "mrtLiving");
                b.Raumangaben(kammer, "bhtUnHeated", null, "mrtStore");
                b.Bezug(wohnen, sued, fenster, innen, boden);
                b.Bezug(kammer, sued, innen, boden);
                return b.Speichern();
            }
        }

        private static byte[] Koerperprobe(string datei, XbimSchemaVersion schema, string typ, Func<Bau, IIfcRepresentationItem[]> koerper, bool gedreht = false)
        {
            using (var b = new Bau(schema, datei))
            {
                b.Anfang(null, karte: false);
                IIfcBuilding g = b.Gebaeude("Probengebäude", null);
                if (gedreht) b.Gedreht(g);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", gedreht ? 3000 : 0);
                IIfcSpace r = b.Raum(s, "0.01", "Raum", 1000, 2000, 20, 3000, 60, beheizt: true);
                b.Koerper(r, typ, koerper(b));
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Nachbarschaft aus Raumkörpern</b> (Stufe G7f-4, Mehrzonenkonzept 6.2): drei Räume als <c>IfcFacetedBrep</c> auf
        /// zwei Geschossen, nach dem Muster eines CAD-Exports (Raumbezüge, keine Raumgrenzen). EG (0 mm): „Büro“ 5 × 4 × 3 m
        /// im Ursprung (20 °C, Büro), „Flur“ 3 × 4 × 3 m bei x = 5,24 m (15 °C, Flur) — dazwischen die Wand „Innenwand
        /// Büro/Flur“ (0,24 m, <c>Pset_WallCommon.ThermalTransmittance</c> 1,2, von beiden referenziert); OG (3300 mm):
        /// „Büro 2“ 5 × 4 × 3 m über dem Büro (20 °C, Büro) — dazwischen die Decke „Decke EG/OG“ (0,3 m, U 1,0, von Büro
        /// und Büro 2 referenziert). Mit <paramref name="grenzen"/> trägt dieselbe Datei
        /// Raumgrenzen der 2. Ebene mit Polygonen für Wand und Decke als Gegenstücke (Gegenprobe: Raumgrenzen gehen vor).
        /// </summary>
        private static byte[] Koerpernachbarn(string datei, bool grenzen)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Körpernachbarn", null);
                IIfcBuildingStorey eg = b.GeschossEnthalten(g, "EG", 0);
                IIfcBuildingStorey og = b.GeschossEnthalten(g, "OG", 3300);
                IIfcWall wand = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Innenwand Büro/Flur", "btaHeated", false, null, null, null, 12, 12, null, null);
                b.Eigenschaften(wand, "Pset_WallCommon", ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(1.2)));
                IIfcSlab decke = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Decke EG/OG", "btaHeated", false, 1.0, null, null, 20, 20, null, null);

                IIfcSpace buero = b.RaumEnthalten(eg, "Büro", 0, 0, 20, 60, 3000, zerlegt: false);
                IIfcSpace flur = b.RaumEnthalten(eg, "Flur", 5240, 0, 12, 36, 3000, zerlegt: false);
                IIfcSpace buero2 = b.RaumEnthalten(og, "Büro 2", 0, 0, 20, 60, 3000, zerlegt: false);
                b.Koerper(buero, "Brep", b.Brep(5000, 4000, 3000, offen: false));
                b.Koerper(flur, "Brep", b.Brep(3000, 4000, 3000, offen: false));
                b.Koerper(buero2, "Brep", b.Brep(5000, 4000, 3000, offen: false));
                b.Raumangaben(buero, "bhtHeated", 20.0, "mrtOffice");
                b.Raumangaben(flur, "bhtHeated", 15.0, "mrtHall");
                b.Raumangaben(buero2, "bhtHeated", 20.0, "mrtOffice");
                b.Bezug(buero, wand, decke);
                b.Bezug(flur, wand);
                b.Bezug(buero2, decke);
                if (grenzen)
                {
                    b.Gegenstuecke(
                        b.Grenze2(buero, wand, IfcInternalOrExternalEnum.INTERNAL, new[] { 1.0, 0.0, 0.0 },
                                  new[] { 5000.0, 0, 0 }, new[] { 5000.0, 4000, 0 }, new[] { 5000.0, 4000, 3000 }, new[] { 5000.0, 0, 3000 }),
                        b.Grenze2(flur, wand, IfcInternalOrExternalEnum.INTERNAL, new[] { -1.0, 0.0, 0.0 },
                                  new[] { 5240.0, 0, 0 }, new[] { 5240.0, 0, 3000 }, new[] { 5240.0, 4000, 3000 }, new[] { 5240.0, 4000, 0 }));
                    b.Gegenstuecke(
                        b.Grenze2(buero, decke, IfcInternalOrExternalEnum.INTERNAL, new[] { 0.0, 0.0, 1.0 },
                                  new[] { 0.0, 0, 3000 }, new[] { 5000.0, 0, 3000 }, new[] { 5000.0, 4000, 3000 }, new[] { 0.0, 4000, 3000 }),
                        b.Grenze2(buero2, decke, IfcInternalOrExternalEnum.INTERNAL, new[] { 0.0, 0.0, -1.0 },
                                  new[] { 0.0, 0, 0 }, new[] { 0.0, 4000, 0 }, new[] { 5000.0, 4000, 0 }, new[] { 5000.0, 0, 0 }));
                }
                return b.Speichern();
            }
        }

        /// <summary>Drei Räume mit Bögen: Verbundkurve mit Kreisbogen, indizierte Kurve mit Bogenstück, Kreisprofil.</summary>
        private static byte[] Bogenprobe()
        {
            const string DATEI = "ifc4_koerper_extrusion_bogen.ifc";
            using (var b = new Bau(XbimSchemaVersion.Ifc4, DATEI))
            {
                b.Anfang(null, karte: false);
                IIfcBuilding g = b.Gebaeude("Probengebäude", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r1 = b.Raum(s, "0.01", "Raum 1", 1000, 2000, 14, 3000, 42, beheizt: true);
                b.Koerper(r1, "SweptSolid", b.Extrusion(b.Verbundprofil(), 3000));
                IIfcSpace r2 = b.Raum(s, "0.02", "Raum 2", 10000, 2000, 14, 3000, 42, beheizt: true);
                b.Koerper(r2, "SweptSolid", b.Extrusion(b.Indexprofil(), 3000));
                IIfcSpace r3 = b.Raum(s, "0.03", "Raum 3", 20000, 2000, 3, 3000, 9, beheizt: true);
                b.Koerper(r3, "SweptSolid", b.Extrusion(b.Kreisprofil(1000), 3000));
                return b.Speichern();
            }
        }

        private sealed partial class Bau
        {
            /// <summary>Die Darstellung „Body“ eines Produkts mit den Trägern in Reihenfolge.</summary>
            public void Koerper(IIfcProduct p, string typ, params IIfcRepresentationItem[] traeger)
            {
                IIfcShapeRepresentation d = N<IIfcShapeRepresentation>("IfcShapeRepresentation");
                d.ContextOfItems = _kontext;
                d.RepresentationIdentifier = new IfcLabel("Body");
                d.RepresentationType = new IfcLabel(typ);
                foreach (IIfcRepresentationItem t in traeger) d.Items.Add(t);
                IIfcProductDefinitionShape form = N<IIfcProductDefinitionShape>("IfcProductDefinitionShape");
                form.Representations.Add(d);
                p.Representation = form;
            }

            /// <summary>Das Gebäude um 90° gedreht (x-Achse nach +y) und um (10000, 5000, 0) mm versetzt.</summary>
            public void Gedreht(IIfcBuilding g) => g.ObjectPlacement = Platzierung(_site.ObjectPlacement, 10000, 5000, 0, 0, 1);

            private IIfcCartesianPoint Punkt2(double x, double y)
            {
                IIfcCartesianPoint p = N<IIfcCartesianPoint>("IfcCartesianPoint");
                p.Coordinates.Add(new IfcLengthMeasure(x));
                p.Coordinates.Add(new IfcLengthMeasure(y));
                return p;
            }

            private IIfcPolyline Linienzug(IEnumerable<(double X, double Y)> punkte)
            {
                IIfcPolyline l = N<IIfcPolyline>("IfcPolyline");
                foreach ((double x, double y) in punkte) l.Points.Add(Punkt2(x, y));
                return l;
            }

            private IIfcAxis2Placement3D Lage3(double x, double y, double z)
            {
                IIfcAxis2Placement3D a = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                a.Location = Punkt(x, y, z);
                return a;
            }

            public IIfcProfileDef Polygonprofil(IEnumerable<(double X, double Y)> ring)
            {
                IIfcArbitraryClosedProfileDef p = N<IIfcArbitraryClosedProfileDef>("IfcArbitraryClosedProfileDef");
                p.ProfileType = IfcProfileTypeEnum.AREA;
                List<(double X, double Y)> r = ring.ToList();
                p.OuterCurve = Linienzug(r.Append(r[0]));
                return p;
            }

            public IIfcProfileDef Rechteckprofil(double x, double y, double mx, double my)
            {
                IIfcRectangleProfileDef p = N<IIfcRectangleProfileDef>("IfcRectangleProfileDef");
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.XDim = new IfcPositiveLengthMeasure(x);
                p.YDim = new IfcPositiveLengthMeasure(y);
                IIfcAxis2Placement2D lage = N<IIfcAxis2Placement2D>("IfcAxis2Placement2D");
                lage.Location = Punkt2(mx, my);
                p.Position = lage;
                return p;
            }

            public IIfcProfileDef Kreisprofil(double radius)
            {
                IIfcCircleProfileDef p = N<IIfcCircleProfileDef>("IfcCircleProfileDef");
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.Radius = new IfcPositiveLengthMeasure(radius);
                return p;
            }

            /// <summary>Rechteck 4000 × 2000 mm mit Halbkreis (r = 2000 mm) oben: Linienzug und getrimmter Kreis.</summary>
            public IIfcProfileDef Verbundprofil()
            {
                IIfcCompositeCurve k = N<IIfcCompositeCurve>("IfcCompositeCurve");
                k.SelfIntersect = false;
                IIfcCompositeCurveSegment s1 = N<IIfcCompositeCurveSegment>("IfcCompositeCurveSegment");
                s1.Transition = IfcTransitionCode.CONTINUOUS;
                s1.SameSense = true;
                s1.ParentCurve = Linienzug(new[] { (0.0, 2000.0), (0.0, 0.0), (4000.0, 0.0), (4000.0, 2000.0) });
                IIfcCircle kreis = N<IIfcCircle>("IfcCircle");
                IIfcAxis2Placement2D mitte = N<IIfcAxis2Placement2D>("IfcAxis2Placement2D");
                mitte.Location = Punkt2(2000, 2000);
                kreis.Position = mitte;
                kreis.Radius = new IfcPositiveLengthMeasure(2000);
                IIfcTrimmedCurve bogen = N<IIfcTrimmedCurve>("IfcTrimmedCurve");
                bogen.BasisCurve = kreis;
                bogen.Trim1.Add(Punkt2(4000, 2000));
                bogen.Trim2.Add(Punkt2(0, 2000));
                bogen.SenseAgreement = true;
                bogen.MasterRepresentation = IfcTrimmingPreference.CARTESIAN;
                IIfcCompositeCurveSegment s2 = N<IIfcCompositeCurveSegment>("IfcCompositeCurveSegment");
                s2.Transition = IfcTransitionCode.CONTINUOUS;
                s2.SameSense = true;
                s2.ParentCurve = bogen;
                k.Segments.Add(s1);
                k.Segments.Add(s2);
                IIfcArbitraryClosedProfileDef p = N<IIfcArbitraryClosedProfileDef>("IfcArbitraryClosedProfileDef");
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.OuterCurve = k;
                return p;
            }

            /// <summary>Dieselbe Form als <c>IfcIndexedPolyCurve</c> mit Linien- und Bogenstück (IFC4).</summary>
            public IIfcProfileDef Indexprofil()
            {
                IIfcIndexedPolyCurve k = N<IIfcIndexedPolyCurve>("IfcIndexedPolyCurve");
                k.Points = Punktliste2(new[] { (0.0, 0.0), (4000.0, 0.0), (4000.0, 2000.0), (2000.0, 4000.0), (0.0, 2000.0) });
                k.Segments.Add(new Xbim.Ifc4.GeometryResource.IfcLineIndex(Indizes(1, 2, 3)));
                k.Segments.Add(new Xbim.Ifc4.GeometryResource.IfcArcIndex(Indizes(3, 4, 5)));
                k.Segments.Add(new Xbim.Ifc4.GeometryResource.IfcLineIndex(Indizes(5, 1)));
                k.SelfIntersect = false;
                IIfcArbitraryClosedProfileDef p = N<IIfcArbitraryClosedProfileDef>("IfcArbitraryClosedProfileDef");
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.OuterCurve = k;
                return p;
            }

            private static List<IfcPositiveInteger> Indizes(params long[] i) => i.Select(x => new IfcPositiveInteger(x)).ToList();

            private IIfcCartesianPointList2D Punktliste2(IEnumerable<(double X, double Y)> punkte)
            {
                IIfcCartesianPointList2D l = N<IIfcCartesianPointList2D>("IfcCartesianPointList2D");
                int i = 0;
                foreach ((double x, double y) in punkte)
                {
                    l.CoordList.GetAt(i++).AddRange(new[] { new IfcLengthMeasure(x), new IfcLengthMeasure(y) });
                }
                return l;
            }

            /// <summary>Außen 6000 × 4000 mm als indizierte Kurve, innen ein Loch 2000 × 2000 mm als Linienzug.</summary>
            public IIfcProfileDef Lochprofil()
            {
                IIfcIndexedPolyCurve aussen = N<IIfcIndexedPolyCurve>("IfcIndexedPolyCurve");
                aussen.Points = Punktliste2(new[] { (0.0, 0.0), (6000.0, 0.0), (6000.0, 4000.0), (0.0, 4000.0) });
                aussen.Segments.Add(new Xbim.Ifc4.GeometryResource.IfcLineIndex(Indizes(1, 2, 3, 4, 1)));
                aussen.SelfIntersect = false;
                IIfcArbitraryProfileDefWithVoids p = N<IIfcArbitraryProfileDefWithVoids>("IfcArbitraryProfileDefWithVoids");
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.OuterCurve = aussen;
                p.InnerCurves.Add(Linienzug(new[] { (2000.0, 1000.0), (4000.0, 1000.0), (4000.0, 3000.0), (2000.0, 3000.0), (2000.0, 1000.0) }));
                return p;
            }

            public IIfcExtrudedAreaSolid Extrusion(IIfcProfileDef profil, double tiefe)
            {
                IIfcExtrudedAreaSolid e = N<IIfcExtrudedAreaSolid>("IfcExtrudedAreaSolid");
                e.SweptArea = profil;
                e.Position = Lage3(0, 0, 0);
                e.ExtrudedDirection = Richtung(0, 0, 1);
                e.Depth = new IfcPositiveLengthMeasure(tiefe);
                return e;
            }

            private static readonly (double, double, double)[] ECKEN =
            {
                (0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1),
            };

            /// <summary>Die sechs Flächen eines Quaders als Eckenindizes, Umlauf nach außen: Boden, Decke, Süd, Ost, Nord, West.</summary>
            private static readonly int[][] FLAECHEN =
            {
                new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 },
            };

            /// <summary>
            /// Ein Quader als <c>IfcFacetedBrep</c> (geschlossen) bzw. — mit <paramref name="offen"/> — ohne Decke als
            /// <c>IfcShellBasedSurfaceModel</c> mit <c>IfcOpenShell</c>. Die Westfläche steht mit umgekehrtem Ring und
            /// <c>Orientation = false</c>.
            /// </summary>
            public IIfcRepresentationItem Brep(double x, double y, double z, bool offen)
            {
                IIfcConnectedFaceSet schale = N<IIfcConnectedFaceSet>(offen ? "IfcOpenShell" : "IfcClosedShell");
                for (int f = 0; f < FLAECHEN.Length; f++)
                {
                    if (offen && f == 1) continue;
                    bool kehren = f == 5;
                    IIfcPolyLoop ring = N<IIfcPolyLoop>("IfcPolyLoop");
                    IEnumerable<int> folge = kehren ? FLAECHEN[f].Reverse() : FLAECHEN[f];
                    foreach (int e in folge) ring.Polygon.Add(Punkt(ECKEN[e].Item1 * x, ECKEN[e].Item2 * y, ECKEN[e].Item3 * z));
                    IIfcFaceOuterBound rand = N<IIfcFaceOuterBound>("IfcFaceOuterBound");
                    rand.Bound = ring;
                    rand.Orientation = !kehren;
                    IIfcFace flaeche = N<IIfcFace>("IfcFace");
                    flaeche.Bounds.Add(rand);
                    schale.CfsFaces.Add(flaeche);
                }
                if (offen)
                {
                    IIfcShellBasedSurfaceModel m = N<IIfcShellBasedSurfaceModel>("IfcShellBasedSurfaceModel");
                    m.SbsmBoundary.Add((IIfcShell)schale);
                    return m;
                }
                IIfcFacetedBrep b = N<IIfcFacetedBrep>("IfcFacetedBrep");
                b.Outer = (IIfcClosedShell)schale;
                return b;
            }

            /// <summary>
            /// Ein Körper aus ebenen Flächen [mm] (HC-5): geschlossen als <c>IfcFacetedBrep</c>, sonst als
            /// <c>IfcShellBasedSurfaceModel</c> mit <c>IfcOpenShell</c>; je Fläche ein <c>IfcPolyLoop</c> im Umlauf der Liste.
            /// </summary>
            public IIfcRepresentationItem Flaechenkoerper(IEnumerable<(double X, double Y, double Z)[]> flaechen, bool geschlossen)
            {
                IIfcConnectedFaceSet schale = N<IIfcConnectedFaceSet>(geschlossen ? "IfcClosedShell" : "IfcOpenShell");
                foreach ((double X, double Y, double Z)[] f in flaechen)
                {
                    IIfcPolyLoop ring = N<IIfcPolyLoop>("IfcPolyLoop");
                    foreach ((double x, double y, double z) in f) ring.Polygon.Add(Punkt(x, y, z));
                    IIfcFaceOuterBound rand = N<IIfcFaceOuterBound>("IfcFaceOuterBound");
                    rand.Bound = ring;
                    rand.Orientation = true;
                    IIfcFace flaeche = N<IIfcFace>("IfcFace");
                    flaeche.Bounds.Add(rand);
                    schale.CfsFaces.Add(flaeche);
                }
                if (!geschlossen)
                {
                    IIfcShellBasedSurfaceModel m = N<IIfcShellBasedSurfaceModel>("IfcShellBasedSurfaceModel");
                    m.SbsmBoundary.Add((IIfcShell)schale);
                    return m;
                }
                IIfcFacetedBrep b = N<IIfcFacetedBrep>("IfcFacetedBrep");
                b.Outer = (IIfcClosedShell)schale;
                return b;
            }

            /// <summary>
            /// Ein achsparalleler Kasten [mm] als <c>IfcShellBasedSurfaceModel</c> aus <c>IfcPolyLoop</c> mit
            /// <c>IfcOpenShell</c> — das Muster der Hüllbauteile eines CAD-Exports.
            /// </summary>
            public IIfcRepresentationItem Kasten(double x0, double y0, double z0, double x1, double y1, double z1)
            {
                IIfcConnectedFaceSet schale = N<IIfcConnectedFaceSet>("IfcOpenShell");
                for (int f = 0; f < FLAECHEN.Length; f++)
                {
                    IIfcPolyLoop ring = N<IIfcPolyLoop>("IfcPolyLoop");
                    foreach (int e in FLAECHEN[f])
                        ring.Polygon.Add(Punkt(x0 + ECKEN[e].Item1 * (x1 - x0), y0 + ECKEN[e].Item2 * (y1 - y0), z0 + ECKEN[e].Item3 * (z1 - z0)));
                    IIfcFaceOuterBound rand = N<IIfcFaceOuterBound>("IfcFaceOuterBound");
                    rand.Bound = ring;
                    rand.Orientation = true;
                    IIfcFace flaeche = N<IIfcFace>("IfcFace");
                    flaeche.Bounds.Add(rand);
                    schale.CfsFaces.Add(flaeche);
                }
                IIfcShellBasedSurfaceModel m = N<IIfcShellBasedSurfaceModel>("IfcShellBasedSurfaceModel");
                m.SbsmBoundary.Add((IIfcShell)schale);
                return m;
            }

            private IIfcCartesianPointList3D Quaderpunkte(double x, double y, double z)
            {
                IIfcCartesianPointList3D l = N<IIfcCartesianPointList3D>("IfcCartesianPointList3D");
                for (int i = 0; i < ECKEN.Length; i++)
                    l.CoordList.GetAt(i).AddRange(new[] { new IfcLengthMeasure(ECKEN[i].Item1 * x), new IfcLengthMeasure(ECKEN[i].Item2 * y), new IfcLengthMeasure(ECKEN[i].Item3 * z) });
                return l;
            }

            /// <summary>Ein Quader als <c>IfcTriangulatedFaceSet</c>: zwölf Dreiecke, je Fläche eine Diagonale.</summary>
            public IIfcRepresentationItem Dreiecksnetz(double x, double y, double z)
            {
                IIfcTriangulatedFaceSet t = N<IIfcTriangulatedFaceSet>("IfcTriangulatedFaceSet");
                t.Coordinates = Quaderpunkte(x, y, z);
                t.Closed = true;
                int k = 0;
                foreach (int[] f in FLAECHEN)
                {
                    t.CoordIndex.GetAt(k++).AddRange(Indizes(f[0] + 1, f[1] + 1, f[2] + 1));
                    t.CoordIndex.GetAt(k++).AddRange(Indizes(f[0] + 1, f[2] + 1, f[3] + 1));
                }
                return t;
            }

            /// <summary>Ein Quader als <c>IfcPolygonalFaceSet</c>: sechs Vierecke.</summary>
            public IIfcRepresentationItem Vieleckssatz(double x, double y, double z)
            {
                IIfcPolygonalFaceSet s = N<IIfcPolygonalFaceSet>("IfcPolygonalFaceSet");
                s.Coordinates = Quaderpunkte(x, y, z);
                s.Closed = true;
                foreach (int[] f in FLAECHEN)
                {
                    IIfcIndexedPolygonalFace v = N<IIfcIndexedPolygonalFace>("IfcIndexedPolygonalFace");
                    v.CoordIndex.AddRange(Indizes(f.Select(i => (long)i + 1).ToArray()));
                    s.Faces.Add(v);
                }
                return s;
            }

            /// <summary>
            /// Ein <c>IfcMappedItem</c>: die Quelle ist ein Würfel 1000 mm (Rechteckprofil), der Ursprung der Abbildung
            /// liegt bei (0, 0, 0); das Ziel versetzt um (100, 200, 0) mm und streckt ungleichmäßig 2 : 1 : 3.
            /// </summary>
            public IIfcRepresentationItem Abgebildet()
            {
                IIfcShapeRepresentation quelle = N<IIfcShapeRepresentation>("IfcShapeRepresentation");
                quelle.ContextOfItems = _kontext;
                quelle.RepresentationIdentifier = new IfcLabel("Body");
                quelle.RepresentationType = new IfcLabel("SweptSolid");
                quelle.Items.Add(Extrusion(Rechteckprofil(1000, 1000, 500, 500), 1000));
                IIfcRepresentationMap karte = N<IIfcRepresentationMap>("IfcRepresentationMap");
                karte.MappingOrigin = Lage3(0, 0, 0);
                karte.MappedRepresentation = quelle;
                IIfcCartesianTransformationOperator3DnonUniform ziel = N<IIfcCartesianTransformationOperator3DnonUniform>("IfcCartesianTransformationOperator3DnonUniform");
                ziel.LocalOrigin = Punkt(100, 200, 0);
                ziel.Scale = new IfcReal(2);
                ziel.Scale2 = new IfcReal(1);
                ziel.Scale3 = new IfcReal(3);
                IIfcMappedItem m = N<IIfcMappedItem>("IfcMappedItem");
                m.MappingSource = karte;
                m.MappingTarget = ziel;
                return m;
            }

            /// <summary>Ein <c>IfcBooleanClippingResult</c>: Quader 4000 × 3000 × 3000 mm minus Halbraum über 2500 mm.</summary>
            public IIfcRepresentationItem Beschnitten()
            {
                IIfcPlane ebene = N<IIfcPlane>("IfcPlane");
                ebene.Position = Lage3(0, 0, 2500);
                IIfcHalfSpaceSolid halbraum = N<IIfcHalfSpaceSolid>("IfcHalfSpaceSolid");
                halbraum.BaseSurface = ebene;
                halbraum.AgreementFlag = false;
                IIfcBooleanClippingResult b = N<IIfcBooleanClippingResult>("IfcBooleanClippingResult");
                b.Operator = IfcBooleanOperator.DIFFERENCE;
                b.FirstOperand = Extrusion(Rechteckprofil(4000, 3000, 2000, 1500), 3000);
                b.SecondOperand = halbraum;
                return b;
            }

            /// <summary>Ein <c>IfcAdvancedBrep</c> (Flächenauswerter nötig — benannt nicht lesbar), Schale ohne Flächen.</summary>
            public IIfcRepresentationItem AdvancedBrep()
            {
                IIfcAdvancedBrep b = N<IIfcAdvancedBrep>("IfcAdvancedBrep");
                b.Outer = N<IIfcClosedShell>("IfcClosedShell");
                return b;
            }
        }
    }
}
