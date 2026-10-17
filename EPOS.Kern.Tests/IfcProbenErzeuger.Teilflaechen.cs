using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Proben der Teilflächen</b> (Körperweg ohne Raumgrenzen): selbst erzeugt, neutrale Namen, runde Werte, Längen in
    /// Millimetern, TrueNorth [0, 1, 0] (Nord = +y, West = −x); nur im Speicher, gehalten von <see cref="TeilflaechenTests"/>.
    /// <list type="bullet">
    /// <item><b>Flachbau</b> (zwei Geschosse, Lage 0 und 2,8 m, Regel Z4 ergibt zwei Zonen): im Erdgeschoss „Wohnen“ (x 0,3 …
    /// 5,3 m, y 0,3 … 4,3 m), Innenwand (x 5,3 … 5,54 m) und „Bad“ (x 5,54 … 9,7 m), im Obergeschoss „Schlafen“ (x 0,3 … 9,7 m),
    /// Räume je 2,5 m hoch, Wände 0,3 m. Die Außenwand „Wand Eck“ ist ein Bauteil aus zwei Körpern (Nord x 0 … 5,3 m und West
    /// y 0,3 … 4,3 m vor Wohnen) mit „Fenster Nord“ 1,5 × 1,2 m und „Fenster West“ 1,0 × 1,2 m. Die „Wand Süd“ läuft über beide
    /// Geschosse (0 … 5,6 m); das „Fensterband“ (Gesamtmaß 4,0 × 2,8 m, x 4,0 … 8,0 m, z 1,5 … 4,3 m) reicht über Wohnen, Bad
    /// und Schlafen.</item>
    /// <item><b>Satteldach</b> (ein Geschoss, Regel Z5): Wohnen (x 0,3 … 5,88 m) und Bad (x 6,12 … 9,7 m), y 0,3 … 5,7 m,
    /// Traufe 2,5 m, First bei y = 3,0 m auf 4,525 m (Neigung 3 : 4); die „Dachplatte“ ist ein Bauteil aus zwei Körpern (Süd- und
    /// Nordfläche, 0,25 m lotrecht dick), Giebelwände und Innenwand je aus zwei Körpern bis unter das Dach.</item>
    /// </list>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        internal const string TEILFLAECHEN_FLACHBAU = "ifc4_teilflaechen_flachbau.ifc";
        internal const string TEILFLAECHEN_SATTEL = "ifc4_teilflaechen_satteldach.ifc";

        /// <summary>Die Unterseite der Südfläche des Satteldachs [mm]: z = 2275 + 0,75 · y.</summary>
        internal static double SattelSued(double y) => 2275.0 + 0.75 * y;

        /// <summary>Die Unterseite der Nordfläche des Satteldachs [mm]: z = 2275 + 0,75 · (6000 − y).</summary>
        internal static double SattelNord(double y) => 2275.0 + 0.75 * (6000.0 - y);

        /// <summary>Die Proben der Teilflächen: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Teilflaechenproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [TEILFLAECHEN_FLACHBAU] = Flachbau(TEILFLAECHEN_FLACHBAU),
                [TEILFLAECHEN_SATTEL] = Satteldach(TEILFLAECHEN_SATTEL),
            };

        private static byte[] Flachbau(string datei)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Flachbau", null);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcBuildingStorey og = b.Geschoss(g, "Obergeschoss", 2800);
                Func<double, double> null0 = y => 0.0, raum = y => 2500.0, geschoss = y => 2800.0;
                IIfcSpace wohnen = b.Raum(eg, "0.01", "Wohnen", 0, 0, null, 2500, null, beheizt: true);
                IIfcSpace bad = b.Raum(eg, "0.02", "Bad", 0, 0, null, 2500, null, beheizt: true);
                IIfcSpace schlafen = b.Raum(og, "1.01", "Schlafen", 0, 0, null, 2500, null, beheizt: true);
                b.Koerper(wohnen, "Brep", b.Prisma(Rechteck(300, 300, 5300, 4300), null0, raum));
                b.Koerper(bad, "Brep", b.Prisma(Rechteck(5540, 300, 9700, 4300), null0, raum));
                b.Koerper(schlafen, "Brep", b.Prisma(Rechteck(300, 300, 9700, 4300), null0, raum));

                // Erdgeschoss: Bodenplatte, die Wand über Eck (Nord und West vor Wohnen), Innenwand, Geschossdecke.
                b.Kleinplatte(eg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, true, Rechteck(0, 0, 10000, 4600), y => -300.0, null0, null);
                IIfcWall eck = b.Teilwand(eg, "Wand Eck", true,
                                          b.Prisma(Rechteck(0, 4300, 5300, 4600), null0, geschoss), b.Prisma(Rechteck(0, 300, 300, 4300), null0, geschoss));
                b.Kleinwand(eg, "Wand Nord EG", true, Rechteck(5300, 4300, 10000, 4600), null0, geschoss, null);
                b.Kleinwand(eg, "Wand Ost EG", true, Rechteck(9700, 300, 10000, 4300), null0, geschoss, null);
                b.Kleinwand(eg, "Innenwand", false, Rechteck(5300, 300, 5540, 4300), null0, raum, null);
                b.Kleinplatte(eg, "Geschossdecke", IfcSlabTypeEnum.FLOOR, false, Rechteck(300, 300, 9700, 4300), raum, geschoss, null);
                b.Kleinfenster(eg, eck, "Fenster Nord", Quader(1000, 4290, 900, 2500, 4610, 2100), (1500, 1200), null);
                b.Kleinfenster(eg, eck, "Fenster West", Quader(-10, 1500, 900, 310, 2500, 2100), (1000, 1200), null);

                // Die Südwand über beide Geschosse mit dem Fensterband über Wohnen, Bad und Schlafen.
                IIfcWall sued = b.Kleinwand(eg, "Wand Süd", true, Rechteck(0, 0, 10000, 300), null0, y => 5600.0, null);
                b.Kleinfenster(eg, sued, "Fensterband", Quader(4000, -10, 1500, 8000, 310, 4300), (4000, 2800), null);

                // Obergeschoss: Wände und Flachdach.
                b.Kleinwand(og, "Wand Nord OG", true, Rechteck(0, 4300, 10000, 4600), null0, geschoss, null);
                b.Kleinwand(og, "Wand Ost OG", true, Rechteck(9700, 300, 10000, 4300), null0, geschoss, null);
                b.Kleinwand(og, "Wand West OG", true, Rechteck(0, 300, 300, 4300), null0, geschoss, null);
                IIfcSlab dach = b.Kleinplatte(og, "Dachplatte", IfcSlabTypeEnum.ROOF, true, Rechteck(0, 0, 10000, 4600), raum, geschoss, null);
                b.Dachganzes(og, "Dach", dach);
                return b.Speichern();
            }
        }

        private static byte[] Satteldach(string datei)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Giebelhaus", null);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                Func<double, double> null0 = y => 0.0;
                IIfcSpace wohnen = b.Raum(eg, "0.01", "Wohnen", 0, 0, null, null, null, beheizt: true);
                IIfcSpace bad = b.Raum(eg, "0.02", "Bad", 0, 0, null, null, null, beheizt: true);
                b.Koerper(wohnen, "Brep", b.Flaechenkoerper(Giebelraum(300, 5880), geschlossen: true));
                b.Koerper(bad, "Brep", b.Flaechenkoerper(Giebelraum(6120, 9700), geschlossen: true));

                b.Kleinplatte(eg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, true, Rechteck(0, 0, 10000, 6000), y => -300.0, null0, null);
                IIfcSlab dach = b.Teilplatte(eg, "Dachplatte", IfcSlabTypeEnum.ROOF, true,
                                             b.Prisma(Rechteck(0, 0, 10000, 3000), SattelSued, y => SattelSued(y) + 250.0),
                                             b.Prisma(Rechteck(0, 3000, 10000, 6000), SattelNord, y => SattelNord(y) + 250.0));
                b.Dachganzes(eg, "Dach", dach);
                b.Kleinwand(eg, "Wand Süd", true, Rechteck(0, 0, 10000, 300), null0, SattelSued, null);
                b.Kleinwand(eg, "Wand Nord", true, Rechteck(0, 5700, 10000, 6000), null0, SattelNord, null);
                foreach ((string name, bool aussen, double x0, double x1) in new[] { ("Wand West", true, 0.0, 300.0), ("Wand Ost", true, 9700.0, 10000.0),
                                                                                     ("Innenwand", false, 5880.0, 6120.0) })
                    b.Teilwand(eg, name, aussen, b.Prisma(Rechteck(x0, 300, x1, 3000), null0, SattelSued),
                               b.Prisma(Rechteck(x0, 3000, x1, 5700), null0, SattelNord));
                return b.Speichern();
            }
        }

        /// <summary>Der Körper eines Raums unter dem Satteldach zwischen x0 und x1 [mm]: Boden, Traufwände, Giebel, zwei Dachflächen.</summary>
        private static (double X, double Y, double Z)[][] Giebelraum(double x0, double x1)
        {
            const double s = 300, n = 5700, f = 3000, t = 2500, h = 4525;
            return new[]
            {
                new[] { (x0, n, 0.0), (x1, n, 0.0), (x1, s, 0.0), (x0, s, 0.0) },
                new[] { (x0, s, 0.0), (x1, s, 0.0), (x1, s, t), (x0, s, t) },
                new[] { (x1, s, 0.0), (x1, n, 0.0), (x1, n, t), (x1, f, h), (x1, s, t) },
                new[] { (x1, n, 0.0), (x0, n, 0.0), (x0, n, t), (x1, n, t) },
                new[] { (x0, n, 0.0), (x0, s, 0.0), (x0, s, t), (x0, f, h), (x0, n, t) },
                new[] { (x0, s, t), (x1, s, t), (x1, f, h), (x0, f, h) },
                new[] { (x0, f, h), (x1, f, h), (x1, n, t), (x0, n, t) },
            };
        }

        private sealed partial class Bau
        {
            /// <summary>Eine Wand aus mehreren Körpern im System des Geschosses (eine gegliederte Wand), ohne Mengensatz.</summary>
            public IIfcWall Teilwand(IIfcBuildingStorey s, string name, bool aussen, params IIfcRepresentationItem[] koerper)
            {
                IIfcWall w = Wurzel<IIfcWall>("IfcWall", name);
                w.PredefinedType = IfcWallTypeEnum.STANDARD;
                w.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, w);
                Satz(w, "Pset_WallCommon", ("IsExternal", new IfcBoolean(aussen)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(aussen ? 0.3 : 1.5)));
                Koerper(w, "Brep", koerper);
                return w;
            }

            /// <summary>Eine Platte aus mehreren Körpern im System des Geschosses (etwa die Flächen eines Satteldachs), ohne Mengensatz.</summary>
            public IIfcSlab Teilplatte(IIfcBuildingStorey s, string name, IfcSlabTypeEnum art, bool aussen, params IIfcRepresentationItem[] koerper)
            {
                IIfcSlab p = Wurzel<IIfcSlab>("IfcSlab", name);
                p.PredefinedType = art;
                p.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, p);
                Satz(p, "Pset_SlabCommon", ("IsExternal", new IfcBoolean(aussen)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(aussen ? 0.25 : 0.8)));
                Koerper(p, "Brep", koerper);
                return p;
            }
        }
    }
}
