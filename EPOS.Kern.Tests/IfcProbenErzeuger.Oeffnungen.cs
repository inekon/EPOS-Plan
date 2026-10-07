using System;
using System.Collections.Generic;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Importproben des Öffnungsabzugs</b> (Abstimmung G5, Teil G5-2). Selbst erzeugt, neutrale Namen, runde Werte,
    /// Längen in Millimetern; abgelegt unter <c>Referenzlaeufe/Importproben/</c>, gehalten von <see cref="IfcOeffnungenTests"/>
    /// (byte-gleich neu erzeugbar).
    ///
    /// <para><b>Das Öffnungshaus</b>: ein Raum 10 × 8 m, vier Außenwände als <c>IfcExtrudedAreaSolid</c> (2,5 m hoch, 0,3 m
    /// dick, außen erklärt, ohne Raumgrenzen), TrueNorth [0, 1, 0], nicht gedreht. In der Südwand Fenster A
    /// (<c>OverallWidth × OverallHeight</c> 1,5 × 1,2 m, Öffnungskörper ebenso) und Fenster B (nur Körper: Öffnung
    /// 1,2 × 1,0 m quer durch die Wand, Fensterkörper 1,1 × 0,9 m); in der Westwand eine Tür (Öffnung ohne Körper, nur
    /// Türkörper 1,0 × 2,0 m); in der Nordwand ein Loch ohne Füllung (1,0 × 1,0 m, durchgehend) und eine Nische
    /// (1,0 × 1,0 m, 0,1 m tief); ein geneigtes Dach (<c>IfcSlab</c> ROOF in <c>IfcRoof</c>, Neigung 3 : 4 nach Süd,
    /// 10 × 10 m) mit einem Dachfenster (Öffnung 1,0 × 1,2 m in der Dachebene, nur Körper); dazu die „Wand Gaube“
    /// (2,0 × 1,0 m) mit einem Fenster 2,0 × 1,2 m — mehr Öffnung als Wand.</para>
    ///
    /// <para><b>Die Gegenprobe</b> ist dasselbe Haus mit Mengensätzen: <c>GrossSideArea</c> und <c>NetSideArea</c> an den
    /// Wänden, <c>GrossArea</c> und <c>NetArea</c> an der Dachplatte, <c>Area</c> an Fenstern und Tür, Fläche und Tiefe an
    /// Loch und Nische.</para>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        /// <summary>Die Öffnungsproben: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Oeffnungsproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["ifc4_g5_oeffnungen.ifc"] = Oeffnungshaus("ifc4_g5_oeffnungen.ifc", mengen: false),
                ["ifc4_g5_oeffnungen_mengen.ifc"] = Oeffnungshaus("ifc4_g5_oeffnungen_mengen.ifc", mengen: true),
                ["ifc4_g5_aussparung.ifc"] = Aussparungshaus(),
            };

        /// <summary>
        /// <b>Das Aussparungshaus</b> (G5-N): ein Raum 10 × 8 m, vier Außenwände ohne Mengensatz, 2,5 m hoch, 0,3 m dick.
        /// Die Südwand ist eine Extrusion ihrer Ansicht (<c>IfcArbitraryProfileDefWithVoids</c>) mit ausgesparter Öffnung
        /// für Fenster A (1,5 × 1,2 m, Gesamtmaße) — Fenster B (1,2 × 1,0 m) sitzt in einer Öffnung, die der Körper nicht
        /// ausspart; die Ostwand spart ein Loch ohne Füllung (1,0 × 1,0 m) aus. Die Nordwand hat einen Körper ohne Mengensatz
        /// und zwei Teile ohne Darstellung mit Mengensatz („Wand Nord-1“ 15 m², „Wand Nord-2“ 8 m²); die Westwand ist eine
        /// schlichte Extrusion ohne Öffnung.
        /// </summary>
        private static byte[] Aussparungshaus()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_g5_aussparung.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuildingStorey s = b.Geschoss(b.Gebaeude("Aussparungshaus", null), "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Wohnen", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (10000, 0), (10000, 8000), (0, 8000));

                IIfcWall sued = b.Aussparungswand(s, "Wand Süd", 0, 0, 1, 0, 10000, 2500, (1000, 900, 1500, 1200));
                IIfcWall ost = b.Aussparungswand(s, "Wand Ost", 10000, 0, 0, 1, 8000, 2500, (3000, 1000, 1000, 1000));
                b.Oeffnungswand(s, "Wand Nord", 10000, 8000, -1, 0, 10000, 2500, null);
                b.Huellwand(s, "Wand Nord-1", 10000, 8000, -1, 0, 15.0);
                b.Huellwand(s, "Wand Nord-2", 4000, 8000, -1, 0, 8.0);
                b.Oeffnungswand(s, "Wand West", 0, 8000, 0, -1, 8000, 2500, null);

                IIfcOpeningElement oa = b.Wandoeffnung(sued, "Fenster A", 1000, 900, 1500, 1200, 300, null);
                b.Oeffnungsfenster(s, sued, oa, "Fenster A", (1500, 1200), null, null);
                IIfcOpeningElement ob = b.Wandoeffnung(sued, "Fenster B", 6000, 1000, 1200, 1000, 300, null);
                b.Oeffnungsfenster(s, sued, ob, "Fenster B", (1200, 1000), null, null);
                b.Wandoeffnung(ost, "Loch", 3000, 1000, 1000, 1000, 300, null);
                return b.Speichern();
            }
        }

        private static byte[] Oeffnungshaus(string datei, bool mengen)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Öffnungshaus", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Wohnen", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (10000, 0), (10000, 8000), (0, 8000));

                IIfcWall sued = b.Oeffnungswand(s, "Wand Süd", 0, 0, 1, 0, 10000, 2500, mengen ? (25.0, 22.0) : null);
                b.Oeffnungswand(s, "Wand Ost", 10000, 0, 0, 1, 8000, 2500, mengen ? (20.0, 20.0) : null);
                IIfcWall nord = b.Oeffnungswand(s, "Wand Nord", 10000, 8000, -1, 0, 10000, 2500, mengen ? (25.0, 24.0) : null);
                IIfcWall west = b.Oeffnungswand(s, "Wand West", 0, 8000, 0, -1, 8000, 2500, mengen ? (20.0, 18.0) : null);
                IIfcWall gaube = b.Oeffnungswand(s, "Wand Gaube", 10000, 2000, 0, 1, 2000, 1000, mengen ? (2.0, 0.0) : null);

                // Südwand: Fenster A mit Gesamtmaßen, Fenster B nur mit Körpern (die Öffnung geht vor).
                IIfcOpeningElement oa = b.Wandoeffnung(sued, "Fenster A", 1000, 900, 1500, 1200, 300, mengen ? 1.8 : (double?)null);
                b.Oeffnungsfenster(s, sued, oa, "Fenster A", (1500, 1200), mengen ? 1.8 : (double?)null, null);
                IIfcOpeningElement ob = b.Wandoeffnung(sued, "Fenster B", 6000, 1000, 1200, 1000, 300, mengen ? 1.2 : (double?)null);
                b.Oeffnungsfenster(s, sued, ob, "Fenster B", null, mengen ? 1.2 : (double?)null,
                    IfcProbenErzeuger.Quader(6050, -200, 1050, 7150, -100, 1950));

                // Westwand: eine Tür, deren Öffnung keinen Körper hat — es gilt der Türkörper.
                IIfcOpeningElement ot = b.Wandoeffnung(west, "Tür", 3000, 0, 1000, 2000, null, mengen ? 2.0 : (double?)null);
                b.Oeffnungstuer(s, west, ot, "Tür", mengen ? 2.0 : (double?)null, IfcProbenErzeuger.Quader(3000, -200, 0, 4000, -100, 2000));

                // Nordwand: ein Loch ohne Füllung (durchgehend) und eine Nische (0,1 m tief, von innen).
                b.Wandoeffnung(nord, "Loch", 2000, 1000, 1000, 1000, 300, mengen ? 1.0 : (double?)null, tiefeImMengensatz: true);
                b.Wandoeffnung(nord, "Nische", 6000, 500, 1000, 1000, 100, mengen ? 1.0 : (double?)null, tiefeImMengensatz: true);

                // Gaube: mehr Öffnung als Wand.
                IIfcOpeningElement og = b.Wandoeffnung(gaube, "Fenster Gaube", 0, 0, 2000, 1000, null, null);
                b.Oeffnungsfenster(s, gaube, og, "Fenster Gaube", (2000, 1200), mengen ? 2.4 : (double?)null, null);

                // Dach: Platte 3 : 4 nach Süd mit einem Dachfenster, dessen Öffnung in der Dachebene liegt.
                IIfcSlab dachplatte = b.Koerperplatte(s, "Dachplatte", IfcSlabTypeEnum.ROOF, 0);
                b.Koerper(dachplatte, "SweptSolid", b.Schraeg(10000, 10000, 200, 2500));
                if (mengen) b.Plattenmengen(dachplatte, 100.0, 98.8);
                b.Dachganzes(s, "Dach", dachplatte);
                IIfcOpeningElement od = b.Dachoeffnung(dachplatte, "Dachfenster", 1000, 1200, 200, 2500);
                b.Oeffnungsfenster(s, dachplatte, od, "Dachfenster", null, mengen ? 1.2 : (double?)null, null);
                return b.Speichern();
            }
        }

        private sealed partial class Bau
        {
            /// <summary>
            /// Eine Außenwand <paramref name="laenge"/> × <paramref name="hoehe"/> [mm] ohne Mengensatz, 300 mm dick (lokal
            /// y = −300 … 0), als Extrusion ihrer Ansicht nach außen: Profil mit den <paramref name="aussparungen"/>
            /// (x0, z0, Breite, Höhe [mm]) als Löcher (<c>IfcArbitraryProfileDefWithVoids</c>).
            /// </summary>
            public IIfcWall Aussparungswand(IIfcBuildingStorey s, string name, double x, double y, double rx, double ry,
                                            double laenge, double hoehe, params (double X0, double Z0, double B, double H)[] aussparungen)
            {
                IIfcWall w = Huellwand(s, name, x, y, rx, ry, null);
                IIfcArbitraryProfileDefWithVoids p = N<IIfcArbitraryProfileDefWithVoids>("IfcArbitraryProfileDefWithVoids");
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.OuterCurve = Linienzug(new[] { (0.0, 0.0), (laenge, 0.0), (laenge, hoehe), (0.0, hoehe), (0.0, 0.0) });
                foreach ((double x0, double z0, double br, double h) in aussparungen)
                    p.InnerCurves.Add(Linienzug(new[] { (x0, z0), (x0, z0 + h), (x0 + br, z0 + h), (x0 + br, z0), (x0, z0) }));
                IIfcAxis2Placement3D lage = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                lage.Location = Punkt(0, 0, 0);
                lage.Axis = Richtung(0, -1, 0);
                lage.RefDirection = Richtung(1, 0, 0);
                IIfcExtrudedAreaSolid e = N<IIfcExtrudedAreaSolid>("IfcExtrudedAreaSolid");
                e.SweptArea = p;
                e.Position = lage;
                e.ExtrudedDirection = Richtung(0, 0, 1);
                e.Depth = new IfcPositiveLengthMeasure(300);
                Koerper(w, "SweptSolid", e);
                return w;
            }

            /// <summary>
            /// Eine Außenwand <paramref name="laenge"/> × <paramref name="hoehe"/> [mm], 300 mm dick (lokal y = −300 … 0, außen
            /// bei −y), als Extrusion; mit <paramref name="mengen"/> Brutto- und Nettofläche in <c>Qto_WallBaseQuantities</c>.
            /// </summary>
            public IIfcWall Oeffnungswand(IIfcBuildingStorey s, string name, double x, double y, double rx, double ry,
                                          double laenge, double hoehe, (double Brutto, double Netto)? mengen)
            {
                IIfcWall w = Huellwand(s, name, x, y, rx, ry, null);
                Koerper(w, "SweptSolid", Extrusion(Rechteckprofil(laenge, 300, laenge / 2.0, -150), hoehe));
                if (mengen.HasValue)
                    Mengen(w, "Qto_WallBaseQuantities", Flaeche("GrossSideArea", mengen.Value.Brutto), Flaeche("NetSideArea", mengen.Value.Netto));
                return w;
            }

            /// <summary>
            /// Eine Öffnung der Wand <paramref name="wirt"/>: Rechteck ab (<paramref name="x0"/>, <paramref name="z0"/>) [mm]
            /// im System der Wand, mit <paramref name="tiefeMm"/> als Extrusion von der Innenseite (y = 0) nach außen; ohne
            /// Tiefe ohne Körper. Mit <paramref name="flaecheM2"/> die Fläche in <c>Qto_OpeningElementBaseQuantities</c>.
            /// </summary>
            public IIfcOpeningElement Wandoeffnung(IIfcWall wirt, string name, double x0, double z0, double breite, double hoehe,
                                                   double? tiefeMm, double? flaecheM2, bool tiefeImMengensatz = false)
            {
                IIfcOpeningElement o = Oeffnung(wirt, name);
                o.PredefinedType = IfcOpeningElementTypeEnum.OPENING;
                o.ObjectPlacement = Platzierung(wirt.ObjectPlacement, 0, 0, 0);
                if (tiefeMm.HasValue)
                {
                    IIfcAxis2Placement3D lage = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                    lage.Location = Punkt(0, 0, 0);
                    lage.Axis = Richtung(0, -1, 0);
                    lage.RefDirection = Richtung(1, 0, 0);
                    IIfcExtrudedAreaSolid e = N<IIfcExtrudedAreaSolid>("IfcExtrudedAreaSolid");
                    e.SweptArea = Rechteckprofil(breite, hoehe, x0 + breite / 2.0, z0 + hoehe / 2.0);
                    e.Position = lage;
                    e.ExtrudedDirection = Richtung(0, 0, 1);
                    e.Depth = new IfcPositiveLengthMeasure(tiefeMm.Value);
                    Koerper(o, "SweptSolid", e);
                }
                if (flaecheM2.HasValue && tiefeImMengensatz && tiefeMm.HasValue)
                    Mengen(o, "Qto_OpeningElementBaseQuantities", Flaeche("Area", flaecheM2.Value), Laenge("Depth", tiefeMm.Value));
                else if (flaecheM2.HasValue) Mengen(o, "Qto_OpeningElementBaseQuantities", Flaeche("Area", flaecheM2.Value));
                return o;
            }

            /// <summary>
            /// Eine Öffnung der Dachplatte: Rechteck <paramref name="breite"/> × <paramref name="tiefe"/> [mm] in der Dachebene
            /// (Mitte bei 5 m / 5 m), so dick wie die Platte, im System der Platte wie <see cref="Schraeg"/>.
            /// </summary>
            public IIfcOpeningElement Dachoeffnung(IIfcSlab wirt, string name, double breite, double tiefe, double dicke, double z)
            {
                IIfcOpeningElement o = Oeffnung(wirt, name);
                o.PredefinedType = IfcOpeningElementTypeEnum.OPENING;
                o.ObjectPlacement = Platzierung(wirt.ObjectPlacement, 0, 0, 0);
                IIfcAxis2Placement3D lage = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                lage.Location = Punkt(0, 0, z);
                lage.Axis = Richtung(0, -0.6, 0.8);
                lage.RefDirection = Richtung(1, 0, 0);
                IIfcExtrudedAreaSolid e = N<IIfcExtrudedAreaSolid>("IfcExtrudedAreaSolid");
                e.SweptArea = Rechteckprofil(breite, tiefe, 5000, 5000);
                e.Position = lage;
                e.ExtrudedDirection = Richtung(0, 0, 1);
                e.Depth = new IfcPositiveLengthMeasure(dicke);
                Koerper(o, "SweptSolid", e);
                return o;
            }

            /// <summary>
            /// Ein Fenster, das die Öffnung <paramref name="o"/> füllt: wahlweise mit Gesamtmaßen [mm], <c>Area</c> im
            /// Mengensatz [m²] und einem Quaderkörper im System des Wirts.
            /// </summary>
            public IIfcWindow Oeffnungsfenster(IIfcBuildingStorey s, IIfcProduct wirt, IIfcOpeningElement o, string name, (double B, double H)? gesamtMm,
                                               double? flaecheM2, (double X, double Y, double Z)[][] koerper)
            {
                IIfcWindow f = Wurzel<IIfcWindow>("IfcWindow", name);
                Enthalten(s, f);
                Fuellen(o, f);
                if (gesamtMm.HasValue)
                {
                    f.OverallWidth = new IfcPositiveLengthMeasure(gesamtMm.Value.B);
                    f.OverallHeight = new IfcPositiveLengthMeasure(gesamtMm.Value.H);
                }
                if (koerper != null)
                {
                    f.ObjectPlacement = Platzierung(wirt.ObjectPlacement, 0, 0, 0);
                    Koerper(f, "Brep", Flaechenkoerper(koerper, geschlossen: true));
                }
                if (flaecheM2.HasValue) Mengen(f, "Qto_WindowBaseQuantities", Flaeche("Area", flaecheM2.Value));
                Satz(f, "Pset_WindowCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(1.1)));
                return f;
            }

            /// <summary>Eine Tür, die die Öffnung <paramref name="o"/> füllt, ohne Gesamtmaße: <c>Area</c> oder ein Quaderkörper.</summary>
            public IIfcDoor Oeffnungstuer(IIfcBuildingStorey s, IIfcProduct wirt, IIfcOpeningElement o, string name, double? flaecheM2,
                                          (double X, double Y, double Z)[][] koerper)
            {
                IIfcDoor t = Wurzel<IIfcDoor>("IfcDoor", name);
                Enthalten(s, t);
                Fuellen(o, t);
                t.ObjectPlacement = Platzierung(wirt.ObjectPlacement, 0, 0, 0);
                Koerper(t, "Brep", Flaechenkoerper(koerper, geschlossen: true));
                if (flaecheM2.HasValue) Mengen(t, "Qto_DoorBaseQuantities", Flaeche("Area", flaecheM2.Value));
                Satz(t, "Pset_DoorCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(1.8)));
                return t;
            }

            /// <summary>Brutto- und Nettofläche einer Platte in <c>Qto_SlabBaseQuantities</c> [m²].</summary>
            public void Plattenmengen(IIfcSlab p, double brutto, double netto)
                => Mengen(p, "Qto_SlabBaseQuantities", Flaeche("GrossArea", brutto), Flaeche("NetArea", netto));
        }
    }
}
