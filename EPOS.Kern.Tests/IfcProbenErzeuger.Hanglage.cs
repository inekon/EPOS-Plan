using System;
using System.Collections.Generic;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Proben der Hanglage</b> (Körperweg ohne Raumgrenzen): selbst erzeugt, neutrale Namen, runde Werte, Längen in
    /// Millimetern, TrueNorth [0, 1, 0]; nur im Speicher, gehalten von <see cref="HanglageTests"/>.
    /// <para>Das „Hanghaus“ hat zwei Geschosse: das Untergeschoss (Lage −1,5 m) mit dem beheizten Raum „Hobby“ (x 0,3 … 9,7 m,
    /// y 0,3 … 5,7 m, z −1,5 … 1,0 m) und das Erdgeschoss (Lage 1,3 m) mit „Wohnen“ (gleicher Grundriss, z 1,3 … 3,8 m). Wände
    /// 0,3 m, Bodenplatte unter dem Untergeschoss, Decke zwischen den Geschossen, Flachdach. In der Südwand des Untergeschosses
    /// das „Kellerfenster“ 1,0 × 0,8 m (z 0 … 0,8 m, über Gelände). Die Geländehöhe nennt das Gebäude über
    /// <c>ElevationOfTerrain</c> gegen <c>ElevationOfRefHeight</c> = 250 m.</para>
    /// <list type="bullet">
    /// <item><b>am Hang</b>: Gelände 249,5 m, in den Gebäudekoordinaten −0,5 m — das Untergeschoss liegt 1,0 m im Erdreich.</item>
    /// <item><b>ohne Gelände</b>: keine Geländehöhe in der Datei, es gilt z = 0.</item>
    /// <item><b>tief</b>: Gelände 251,2 m (+1,2 m) — das Untergeschoss liegt ganz unter, das Erdgeschoss ganz über Gelände.</item>
    /// </list>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        internal const string HANGLAGE_HANG = "ifc4_hanglage_hang.ifc";
        internal const string HANGLAGE_OHNE = "ifc4_hanglage_ohne_gelaende.ifc";
        internal const string HANGLAGE_TIEF = "ifc4_hanglage_tief.ifc";

        /// <summary>Die Proben der Hanglage: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Hanglageproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [HANGLAGE_HANG] = Hanghaus(HANGLAGE_HANG, 249500.0),
                [HANGLAGE_OHNE] = Hanghaus(HANGLAGE_OHNE, null),
                [HANGLAGE_TIEF] = Hanghaus(HANGLAGE_TIEF, 251200.0),
            };

        private static byte[] Hanghaus(string datei, double? gelaendeMm)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Hanghaus", null);
                if (gelaendeMm.HasValue)
                {
                    g.ElevationOfRefHeight = new IfcLengthMeasure(250000.0);
                    g.ElevationOfTerrain = new IfcLengthMeasure(gelaendeMm.Value);
                }
                IIfcBuildingStorey ug = b.Geschoss(g, "Untergeschoss", -1500);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 1300);
                Func<double, double> null0 = y => 0.0, raum = y => 2500.0, geschoss = y => 2800.0;
                IIfcSpace hobby = b.Raum(ug, "U.01", "Hobby", 0, 0, null, 2500, null, beheizt: true);
                IIfcSpace wohnen = b.Raum(eg, "0.01", "Wohnen", 0, 0, null, 2500, null, beheizt: true);
                b.Koerper(hobby, "Brep", b.Prisma(Rechteck(300, 300, 9700, 5700), null0, raum));
                b.Koerper(wohnen, "Brep", b.Prisma(Rechteck(300, 300, 9700, 5700), null0, raum));

                // Untergeschoss: Bodenplatte, vier Außenwände, Decke; das Kellerfenster über Gelände.
                b.Kleinplatte(ug, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, true, Rechteck(0, 0, 10000, 6000), y => -300.0, null0, null);
                IIfcWall sued = b.Kleinwand(ug, "Wand Süd UG", true, Rechteck(0, 0, 10000, 300), null0, geschoss, null);
                b.Kleinwand(ug, "Wand Nord UG", true, Rechteck(0, 5700, 10000, 6000), null0, geschoss, null);
                b.Kleinwand(ug, "Wand West UG", true, Rechteck(0, 300, 300, 5700), null0, geschoss, null);
                b.Kleinwand(ug, "Wand Ost UG", true, Rechteck(9700, 300, 10000, 5700), null0, geschoss, null);
                b.Kleinplatte(ug, "Decke UG", IfcSlabTypeEnum.FLOOR, false, Rechteck(300, 300, 9700, 5700), raum, geschoss, null);
                b.Kleinfenster(ug, sued, "Kellerfenster", Quader(4000, -10, 1500, 5000, 310, 2300), (1000, 800), null);

                // Erdgeschoss: vier Außenwände und Flachdach.
                b.Kleinwand(eg, "Wand Süd EG", true, Rechteck(0, 0, 10000, 300), null0, geschoss, null);
                b.Kleinwand(eg, "Wand Nord EG", true, Rechteck(0, 5700, 10000, 6000), null0, geschoss, null);
                b.Kleinwand(eg, "Wand West EG", true, Rechteck(0, 300, 300, 5700), null0, geschoss, null);
                b.Kleinwand(eg, "Wand Ost EG", true, Rechteck(9700, 300, 10000, 5700), null0, geschoss, null);
                IIfcSlab dach = b.Kleinplatte(eg, "Dachplatte", IfcSlabTypeEnum.ROOF, true, Rechteck(0, 0, 10000, 6000), raum, geschoss, null);
                b.Dachganzes(eg, "Dach", dach);
                return b.Speichern();
            }
        }
    }
}
