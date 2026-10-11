using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Importproben des Körperwegs der Bauteilflächen</b> (Abstimmung G5, Teil G5-3). Selbst erzeugt, neutrale Namen,
    /// runde Werte, Längen in Millimetern; abgelegt unter <c>Referenzlaeufe/Importproben/</c>, gehalten von
    /// <see cref="KoerperflaechenTests"/> (byte-gleich neu erzeugbar).
    ///
    /// <para><b>Das Kleinhaus</b> (außen 10 × 6 m, Wände 0,3 m, alle Körper als <c>IfcFacetedBrep</c> im System ihres
    /// Geschosses, TrueNorth [0, 1, 0]): Kellergeschoss (Lage −2,5 m) mit dem unbeheizten Raum „Keller“ 9,4 × 5,4 × 2,2 m
    /// auf der Bodenplatte (<c>BASESLAB</c>, 0,3 m) unter der Kellerdecke (0,3 m, innen); Erdgeschoss (Lage 0) mit „Wohnen“
    /// (x 0,3 … 5,88 m) und „Küche“ (x 6,12 … 9,7 m), je 5,4 m tief und 2,5 m hoch, getrennt durch die „Innenwand“
    /// (0,24 m, innen), darüber die Geschossdecke (0,3 m, innen); Obergeschoss (Lage 2,8 m) mit „Schlafen“ 9,4 × 5,4 m unter
    /// dem Pultdach (Unterseite z = 3,575 m + 0,75 · y, Neigung 3 : 4 nach Süd, 0,25 m lotrecht dick). Die Außenwände Süd und
    /// Nord laufen über die ganze Länge, Ost und West zwischen ihnen; im Obergeschoss enden alle unter dem Dach. Fenster
    /// (Öffnungskörper quer durch die Wand, Gesamtmaße): „Fenster Wohnen“ 1,5 × 1,2 m und „Fenster Küche“ 1,0 × 1,2 m in
    /// der Südwand des Erdgeschosses, „Fenster Schlafen“ 1,0 × 1,2 m in der Ostwand des Obergeschosses.</para>
    ///
    /// <para><b>Ohne Mengen</b> trägt die Datei weder Mengensätze noch Raumgrenzen noch Raumbezüge; <b>mit Mengen</b> ist
    /// dasselbe Haus mit Mengensätzen an Räumen, Wänden, Platten und Fenstern und Raumgrenzen der 2. Ebene mit Polygonen auf
    /// den Raumflächen (Kellerwände und Bodenplatte <c>EXTERNAL_EARTH</c>, Gegenstücke an Innenwand und Decken; Keller- und Geschossdecke je Raum darüber bei x = 6 m geteilt).</para>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        /// <summary>Die Proben des Körperwegs: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Kleinhausproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["ifc4_g5_kleinhaus_ohne_mengen.ifc"] = Kleinhaus("ifc4_g5_kleinhaus_ohne_mengen.ifc", mengen: false),
                ["ifc4_g5_kleinhaus_mit_mengen.ifc"] = Kleinhaus("ifc4_g5_kleinhaus_mit_mengen.ifc", mengen: true),
            };

        /// <summary>Die Unterseite des Dachs im System des Obergeschosses [mm]: z = 775 + 0,75 · y.</summary>
        internal static double KleinhausDach(double y) => 775.0 + 0.75 * y;

        private static byte[] Kleinhaus(string datei, bool mengen)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Kleinhaus", null);
                IIfcBuildingStorey kg = b.Geschoss(g, "Kellergeschoss", -2500);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcBuildingStorey og = b.Geschoss(g, "Obergeschoss", 2800);
                Func<double, double> eben0 = y => 0.0;

                // Räume (System des Geschosses, Ursprung im Geschoss).
                IIfcSpace keller = b.Raum(kg, "K.01", "Keller", 0, 0, mengen ? 50.76 : (double?)null, 2200, 111.672, beheizt: false);
                IIfcSpace wohnen = b.Raum(eg, "0.01", "Wohnen", 0, 0, mengen ? 30.132 : (double?)null, 2500, 75.33, beheizt: true);
                IIfcSpace kueche = b.Raum(eg, "0.02", "Küche", 0, 0, mengen ? 19.332 : (double?)null, 2500, 48.33, beheizt: true);
                IIfcSpace schlafen = b.Raum(og, "1.01", "Schlafen", 0, 0, mengen ? 50.76 : (double?)null, 3025, 153.549, beheizt: true);
                b.Koerper(keller, "Brep", b.Prisma(Rechteck(300, 300, 9700, 5700), y => 0.0, y => 2200.0));
                b.Koerper(wohnen, "Brep", b.Prisma(Rechteck(300, 300, 5880, 5700), eben0, y => 2500.0));
                b.Koerper(kueche, "Brep", b.Prisma(Rechteck(6120, 300, 9700, 5700), eben0, y => 2500.0));
                b.Koerper(schlafen, "Brep", b.Prisma(Rechteck(300, 300, 9700, 5700), eben0, KleinhausDach));

                // Kellergeschoss.
                IIfcSlab bodenplatte = b.Kleinplatte(kg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, true, Rechteck(0, 0, 10000, 6000), y => -300.0, y => 0.0,
                                                     mengen ? (60.0, 60.0) : ((double, double)?)null);
                IIfcWall kgSued = b.Kleinwand(kg, "Wand Süd KG", true, Rechteck(0, 0, 10000, 300), eben0, y => 2500.0, mengen ? (25.0, 25.0) : ((double, double)?)null);
                IIfcWall kgNord = b.Kleinwand(kg, "Wand Nord KG", true, Rechteck(0, 5700, 10000, 6000), eben0, y => 2500.0, mengen ? (25.0, 25.0) : ((double, double)?)null);
                IIfcWall kgWest = b.Kleinwand(kg, "Wand West KG", true, Rechteck(0, 300, 300, 5700), eben0, y => 2500.0, mengen ? (13.5, 13.5) : ((double, double)?)null);
                IIfcWall kgOst = b.Kleinwand(kg, "Wand Ost KG", true, Rechteck(9700, 300, 10000, 5700), eben0, y => 2500.0, mengen ? (13.5, 13.5) : ((double, double)?)null);
                IIfcSlab kellerdecke = b.Kleinplatte(kg, "Kellerdecke", IfcSlabTypeEnum.FLOOR, false, Rechteck(300, 300, 9700, 5700), y => 2200.0, y => 2500.0,
                                                     mengen ? (50.76, 50.76) : ((double, double)?)null);

                // Erdgeschoss.
                IIfcWall egSued = b.Kleinwand(eg, "Wand Süd EG", true, Rechteck(0, 0, 10000, 300), eben0, y => 2800.0, mengen ? (28.0, 25.0) : ((double, double)?)null);
                IIfcWall egNord = b.Kleinwand(eg, "Wand Nord EG", true, Rechteck(0, 5700, 10000, 6000), eben0, y => 2800.0, mengen ? (28.0, 28.0) : ((double, double)?)null);
                IIfcWall egWest = b.Kleinwand(eg, "Wand West EG", true, Rechteck(0, 300, 300, 5700), eben0, y => 2800.0, mengen ? (15.12, 15.12) : ((double, double)?)null);
                IIfcWall egOst = b.Kleinwand(eg, "Wand Ost EG", true, Rechteck(9700, 300, 10000, 5700), eben0, y => 2800.0, mengen ? (15.12, 15.12) : ((double, double)?)null);
                IIfcWall innenwand = b.Kleinwand(eg, "Innenwand", false, Rechteck(5880, 300, 6120, 5700), eben0, y => 2500.0, mengen ? (13.5, 13.5) : ((double, double)?)null);
                IIfcSlab decke = b.Kleinplatte(eg, "Geschossdecke", IfcSlabTypeEnum.FLOOR, false, Rechteck(300, 300, 9700, 5700), y => 2500.0, y => 2800.0,
                                               mengen ? (50.76, 50.76) : ((double, double)?)null);
                b.Kleinfenster(eg, egSued, "Fenster Wohnen", Quader(1000, -10, 900, 2500, 310, 2100), (1500, 1200), mengen ? 1.8 : (double?)null);
                b.Kleinfenster(eg, egSued, "Fenster Küche", Quader(7000, -10, 900, 8000, 310, 2100), (1000, 1200), mengen ? 1.2 : (double?)null);

                // Obergeschoss: Wände bis unter das Dach, Pultdach als Platte in einem Dach.
                IIfcWall ogSued = b.Kleinwand(og, "Wand Süd OG", true, Rechteck(0, 0, 10000, 300), eben0, KleinhausDach, mengen ? (9.875, 9.875) : ((double, double)?)null);
                IIfcWall ogNord = b.Kleinwand(og, "Wand Nord OG", true, Rechteck(0, 5700, 10000, 6000), eben0, KleinhausDach, mengen ? (51.625, 51.625) : ((double, double)?)null);
                IIfcWall ogWest = b.Kleinwand(og, "Wand West OG", true, Rechteck(0, 300, 300, 5700), eben0, KleinhausDach, mengen ? (16.335, 16.335) : ((double, double)?)null);
                IIfcWall ogOst = b.Kleinwand(og, "Wand Ost OG", true, Rechteck(9700, 300, 10000, 5700), eben0, KleinhausDach, mengen ? (16.335, 15.135) : ((double, double)?)null);
                IIfcSlab dachplatte = b.Kleinplatte(og, "Dachplatte", IfcSlabTypeEnum.ROOF, true, Rechteck(0, 0, 10000, 6000), KleinhausDach, y => KleinhausDach(y) + 250.0,
                                                    mengen ? (75.0, 75.0) : ((double, double)?)null);
                b.Dachganzes(og, "Dach", dachplatte);
                b.Kleinfenster(og, ogOst, "Fenster Schlafen", Quader(9690, 2000, 500, 10010, 3000, 1700), (1000, 1200), mengen ? 1.2 : (double?)null);

                if (mengen)
                {
                    // Raumgrenzen auf den Raumflächen (System des Geschosses, Raumursprung im Geschoss).
                    var aussen = IfcInternalOrExternalEnum.EXTERNAL;
                    var innen = IfcInternalOrExternalEnum.INTERNAL;
                    var erde = IfcInternalOrExternalEnum.EXTERNAL_EARTH;
                    b.Wandgrenze(keller, kgSued, erde, 300, 300, 9700, 300, 0, 2200);
                    b.Wandgrenze(keller, kgOst, erde, 9700, 300, 9700, 5700, 0, 2200);
                    b.Wandgrenze(keller, kgNord, erde, 9700, 5700, 300, 5700, 0, 2200);
                    b.Wandgrenze(keller, kgWest, erde, 300, 5700, 300, 300, 0, 2200);
                    b.Deckengrenze(keller, bodenplatte, erde, false, 300, 300, 9700, 5700, 0);
                    IIfcRelSpaceBoundary kdW = b.Deckengrenze(keller, kellerdecke, innen, true, 300, 300, 6000, 5700, 2200);
                    IIfcRelSpaceBoundary kdK = b.Deckengrenze(keller, kellerdecke, innen, true, 6000, 300, 9700, 5700, 2200);

                    b.Wandgrenze(wohnen, egSued, aussen, 300, 300, 5880, 300, 0, 2500);
                    b.Wandgrenze(wohnen, egNord, aussen, 5880, 5700, 300, 5700, 0, 2500);
                    b.Wandgrenze(wohnen, egWest, aussen, 300, 5700, 300, 300, 0, 2500);
                    IIfcRelSpaceBoundary iwW = b.Wandgrenze(wohnen, innenwand, innen, 5880, 300, 5880, 5700, 0, 2500);
                    b.Gegenstuecke(kdW, b.Deckengrenze(wohnen, kellerdecke, innen, false, 300, 300, 5880, 5700, 0));
                    IIfcRelSpaceBoundary gdW = b.Deckengrenze(wohnen, decke, innen, true, 300, 300, 5880, 5700, 2500);

                    b.Wandgrenze(kueche, egSued, aussen, 6120, 300, 9700, 300, 0, 2500);
                    b.Wandgrenze(kueche, egOst, aussen, 9700, 300, 9700, 5700, 0, 2500);
                    b.Wandgrenze(kueche, egNord, aussen, 9700, 5700, 6120, 5700, 0, 2500);
                    b.Gegenstuecke(iwW, b.Wandgrenze(kueche, innenwand, innen, 6120, 5700, 6120, 300, 0, 2500));
                    b.Gegenstuecke(kdK, b.Deckengrenze(kueche, kellerdecke, innen, false, 6120, 300, 9700, 5700, 0));
                    IIfcRelSpaceBoundary gdK = b.Deckengrenze(kueche, decke, innen, true, 6120, 300, 9700, 5700, 2500);

                    b.Gegenstuecke(gdW, b.Deckengrenze(schlafen, decke, innen, false, 300, 300, 6000, 5700, 0));
                    b.Gegenstuecke(gdK, b.Deckengrenze(schlafen, decke, innen, false, 6000, 300, 9700, 5700, 0));
                    b.Grenze2(schlafen, ogSued, aussen, new[] { 0.0, -1.0, 0.0 },
                              P3(300, 300, 0), P3(9700, 300, 0), P3(9700, 300, KleinhausDach(300)), P3(300, 300, KleinhausDach(300)));
                    b.Grenze2(schlafen, ogNord, aussen, new[] { 0.0, 1.0, 0.0 },
                              P3(9700, 5700, 0), P3(300, 5700, 0), P3(300, 5700, KleinhausDach(5700)), P3(9700, 5700, KleinhausDach(5700)));
                    b.Grenze2(schlafen, ogOst, aussen, new[] { 1.0, 0.0, 0.0 },
                              P3(9700, 300, 0), P3(9700, 5700, 0), P3(9700, 5700, KleinhausDach(5700)), P3(9700, 300, KleinhausDach(300)));
                    b.Grenze2(schlafen, ogWest, aussen, new[] { -1.0, 0.0, 0.0 },
                              P3(300, 5700, 0), P3(300, 300, 0), P3(300, 300, KleinhausDach(300)), P3(300, 5700, KleinhausDach(5700)));
                    b.Grenze2(schlafen, dachplatte, aussen, new[] { 0.0, -0.6, 0.8 },
                              P3(300, 300, KleinhausDach(300)), P3(9700, 300, KleinhausDach(300)), P3(9700, 5700, KleinhausDach(5700)), P3(300, 5700, KleinhausDach(5700)));
                }
                return b.Speichern();
            }
        }

        private static double[] P3(double x, double y, double z) => new[] { x, y, z };

        /// <summary>Ein achsparalleles Rechteck gegen den Uhrzeigersinn [mm].</summary>
        private static (double X, double Y)[] Rechteck(double x0, double y0, double x1, double y1)
            => new[] { (x0, y0), (x1, y0), (x1, y1), (x0, y1) };

        private sealed partial class Bau
        {
            /// <summary>
            /// Ein Prisma über dem Ring <paramref name="ring"/> (gegen den Uhrzeigersinn) zwischen den Ebenen
            /// <paramref name="unten"/> und <paramref name="oben"/> (z als lineare Funktion von y) [mm], als geschlossener
            /// <c>IfcFacetedBrep</c> mit Außennormalen.
            /// </summary>
            public IIfcRepresentationItem Prisma((double X, double Y)[] ring, Func<double, double> unten, Func<double, double> oben)
            {
                var flaechen = new List<(double X, double Y, double Z)[]>
                {
                    ring.Reverse().Select(p => (p.X, p.Y, unten(p.Y))).ToArray(),
                    ring.Select(p => (p.X, p.Y, oben(p.Y))).ToArray(),
                };
                for (int i = 0; i < ring.Length; i++)
                {
                    (double X, double Y) p = ring[i], q = ring[(i + 1) % ring.Length];
                    flaechen.Add(new[] { (p.X, p.Y, unten(p.Y)), (q.X, q.Y, unten(q.Y)), (q.X, q.Y, oben(q.Y)), (p.X, p.Y, oben(p.Y)) });
                }
                return Flaechenkoerper(flaechen, geschlossen: true);
            }

            /// <summary>Eine Wand als Prisma im System des Geschosses; <c>IsExternal</c> nach <paramref name="aussen"/>, wahlweise mit Mengensatz.</summary>
            public IIfcWall Kleinwand(IIfcBuildingStorey s, string name, bool aussen, (double X, double Y)[] ring, Func<double, double> unten,
                                      Func<double, double> oben, (double Brutto, double Netto)? mengen)
            {
                IIfcWall w = Wurzel<IIfcWall>("IfcWall", name);
                w.PredefinedType = IfcWallTypeEnum.STANDARD;
                w.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, w);
                Satz(w, "Pset_WallCommon", ("IsExternal", new IfcBoolean(aussen)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(aussen ? 0.3 : 1.5)));
                Koerper(w, "Brep", Prisma(ring, unten, oben));
                if (mengen.HasValue)
                    Mengen(w, "Qto_WallBaseQuantities", Flaeche("GrossSideArea", mengen.Value.Brutto), Flaeche("NetSideArea", mengen.Value.Netto));
                return w;
            }

            /// <summary>Eine Platte als Prisma im System des Geschosses; <c>IsExternal</c> nach <paramref name="aussen"/>, wahlweise mit Mengensatz.</summary>
            public IIfcSlab Kleinplatte(IIfcBuildingStorey s, string name, IfcSlabTypeEnum art, bool aussen, (double X, double Y)[] ring,
                                        Func<double, double> unten, Func<double, double> oben, (double Brutto, double Netto)? mengen)
            {
                IIfcSlab p = Wurzel<IIfcSlab>("IfcSlab", name);
                p.PredefinedType = art;
                p.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, p);
                Satz(p, "Pset_SlabCommon", ("IsExternal", new IfcBoolean(aussen)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(aussen ? 0.25 : 0.8)));
                Koerper(p, "Brep", Prisma(ring, unten, oben));
                if (mengen.HasValue) Plattenmengen(p, mengen.Value.Brutto, mengen.Value.Netto);
                return p;
            }

            /// <summary>Ein Fenster in einer Öffnung mit Quaderkörper (System des Geschosses) und Gesamtmaßen [mm].</summary>
            public IIfcWindow Kleinfenster(IIfcBuildingStorey s, IIfcElement wirt, string name, (double X, double Y, double Z)[][] oeffnung,
                                           (double B, double H) gesamtMm, double? flaecheM2)
            {
                IIfcOpeningElement o = Oeffnung(wirt, name);
                o.PredefinedType = IfcOpeningElementTypeEnum.OPENING;
                o.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Koerper(o, "Brep", Flaechenkoerper(oeffnung, geschlossen: true));
                return Oeffnungsfenster(s, wirt, o, name, gesamtMm, flaecheM2, null);
            }

            /// <summary>Die Raumgrenze einer Wand: Rechteck von (x0, y0) nach (x1, y1) zwischen z0 und z1 [mm], Normale aus dem Raum heraus.</summary>
            public IIfcRelSpaceBoundary Wandgrenze(IIfcSpace r, IIfcElement e, IfcInternalOrExternalEnum art, double x0, double y0, double x1, double y1,
                                                   double z0, double z1)
            {
                double dx = x1 - x0, dy = y1 - y0, l = Math.Sqrt(dx * dx + dy * dy);
                return Grenze2(r, e, art, new[] { dy / l, -dx / l, 0.0 }, P3(x0, y0, z0), P3(x1, y1, z0), P3(x1, y1, z1), P3(x0, y0, z1));
            }

            /// <summary>Die Raumgrenze einer Decke (<paramref name="oben"/>: Normale nach oben) bzw. eines Bodens: Rechteck auf der Höhe z [mm].</summary>
            public IIfcRelSpaceBoundary Deckengrenze(IIfcSpace r, IIfcElement e, IfcInternalOrExternalEnum art, bool oben, double x0, double y0,
                                                     double x1, double y1, double z)
                => oben
                    ? Grenze2(r, e, art, new[] { 0.0, 0.0, 1.0 }, P3(x0, y0, z), P3(x1, y0, z), P3(x1, y1, z), P3(x0, y1, z))
                    : Grenze2(r, e, art, new[] { 0.0, 0.0, -1.0 }, P3(x0, y0, z), P3(x0, y1, z), P3(x1, y1, z), P3(x1, y0, z));
        }
    }
}
