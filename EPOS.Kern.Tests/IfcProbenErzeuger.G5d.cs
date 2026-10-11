using System;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Proben der Korrektur G5-3d</b> (doppelt gezählte Decke über unbeheizt, Fenster an zu kleinen Wänden). Selbst
    /// erzeugt, neutrale Namen, runde Werte, Längen in Millimetern; nur im Speicher (keine Probendatei), gehalten von
    /// <see cref="IfcG5dKorrekturTests"/>.
    ///
    /// <para><b>Das Kellerdeckenhaus</b> (ohne Raumgrenzen, Körper als <c>IfcFacetedBrep</c> im System ihres Geschosses):
    /// Kellergeschoss (Lage −2,5 m) mit dem unbeheizten „Keller“, Erdgeschoss (Lage 0) mit „Wohnen“, Obergeschoss (Lage 2,8 m)
    /// mit „Schlafen“, je 9,4 × 5,4 m im Grundriss 0,3 … 9,7 × 0,3 … 5,7 m. Die „Kellerdecke“ steht im Erdgeschoss
    /// (Körper z −0,3 … 0), erklärt gegen unbeheizt (<c>btaCellarCeiling</c>, Hülle, U 0,4) mit 50,76 m² und einem Raumbezug
    /// nur zu „Wohnen“ — ein Einraumbauteil wie im CAD-Export; die Körper von Keller und Wohnen liegen 0,3 m übereinander.
    /// Die „Geschossdecke“ (innen, Körper z 2,5 … 2,8 m, Mengensatz 45 m²) und das „Podest“ (innen, Körper z 1,0 … 1,2 m,
    /// Mengensatz 50,76 m² — der Paarfläche näher, aber in falscher Höhe) referenziert ebenfalls nur „Wohnen“. Außenwände
    /// im Erdgeschoss und Obergeschoss mit Mengensatz, ein Flachdach.</para>
    ///
    /// <para><b>Das Öffnungslagehaus</b> (ein Geschoss, Raum 10 × 8 m mit Grundriss): Die „Wand Süd“ hat einen Körper
    /// 10 × 2,5 m, aber einen Mengensatz von nur 3 m²; ihre Teile ohne Darstellung „Wand Süd-1“ (12 m²) und „Wand Süd-2“
    /// (10 m²) tragen den Rest. Laut Datei hängen an ihr „Fenster A“ 1,5 × 1,2 m, „Fenster B“ 1,2 × 1,0 m, „Fenster C“
    /// 1,0 × 1,2 m (alle in ihrer Ebene) und „Fenster D“ 1,0 × 1,0 m, dessen Öffnungskörper in der Ostwand liegt. Ost-,
    /// Nord- und Westwand mit Körper und Mengensatz (20, 25, 20 m²). Mit <c>schwebend</c> kommt „Fenster E“ 2,0 × 2,0 m dazu,
    /// dessen Öffnung über der Südwand liegt (in keiner Wand).</para>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        /// <summary>Das Kellerdeckenhaus (G5-3d, Befund 1).</summary>
        public static byte[] Kellerdeckenhaus()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_g5d_kellerdecke.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Kellerdeckenhaus", null);
                IIfcBuildingStorey kg = b.Geschoss(g, "Kellergeschoss", -2500);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcBuildingStorey og = b.Geschoss(g, "Obergeschoss", 2800);
                Func<double, double> eben0 = y => 0.0;
                (double X, double Y)[] innen = Rechteck(300, 300, 9700, 5700);

                IIfcSpace keller = b.Raum(kg, "K.01", "Keller", 0, 0, 50.76, 2200, 111.672, beheizt: false);
                IIfcSpace wohnen = b.Raum(eg, "0.01", "Wohnen", 0, 0, 50.76, 2500, 126.9, beheizt: true);
                IIfcSpace schlafen = b.Raum(og, "1.01", "Schlafen", 0, 0, 50.76, 2500, 126.9, beheizt: true);
                b.Koerper(keller, "Brep", b.Prisma(innen, eben0, y => 2200.0));
                b.Koerper(wohnen, "Brep", b.Prisma(innen, eben0, y => 2500.0));
                b.Koerper(schlafen, "Brep", b.Prisma(innen, eben0, y => 2500.0));

                IIfcSlab kellerdecke = b.Huellplatte(eg, "Kellerdecke", innen, -300.0, 0.0, "btaCellarCeiling", 0.4, 50.76);
                IIfcSlab decke = b.Kleinplatte(eg, "Geschossdecke", IfcSlabTypeEnum.FLOOR, false, innen, y => 2500.0, y => 2800.0, (45.0, 45.0));
                IIfcSlab podest = b.Kleinplatte(eg, "Podest", IfcSlabTypeEnum.FLOOR, false, innen, y => 1000.0, y => 1200.0, (50.76, 50.76));
                b.Bezug(wohnen, kellerdecke, decke, podest);

                foreach ((IIfcBuildingStorey s, string z) in new[] { (eg, "EG"), (og, "OG") })
                {
                    b.Kleinwand(s, "Wand Süd " + z, true, Rechteck(0, 0, 10000, 300), eben0, y => 2800.0, (28.0, 28.0));
                    b.Kleinwand(s, "Wand Nord " + z, true, Rechteck(0, 5700, 10000, 6000), eben0, y => 2800.0, (28.0, 28.0));
                    b.Kleinwand(s, "Wand West " + z, true, Rechteck(0, 300, 300, 5700), eben0, y => 2800.0, (15.12, 15.12));
                    b.Kleinwand(s, "Wand Ost " + z, true, Rechteck(9700, 300, 10000, 5700), eben0, y => 2800.0, (15.12, 15.12));
                }
                b.Kleinplatte(og, "Dachplatte", IfcSlabTypeEnum.ROOF, true, Rechteck(0, 0, 10000, 6000), y => 2500.0, y => 2800.0, (60.0, 60.0));
                return b.Speichern();
            }
        }

        /// <summary>Das Öffnungslagehaus (G5-3d, Befund 2); mit <paramref name="schwebend"/> das „Fenster E“ in keiner Wand.</summary>
        public static byte[] Oeffnungslagehaus(bool schwebend)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, schwebend ? "ifc4_g5d_oeffnungslage_schwebend.ifc" : "ifc4_g5d_oeffnungslage.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuildingStorey s = b.Geschoss(b.Gebaeude("Öffnungslagehaus", null), "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Wohnen", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (10000, 0), (10000, 8000), (0, 8000));

                IIfcWall sued = b.Oeffnungswand(s, "Wand Süd", 0, 0, 1, 0, 10000, 2500, (3.0, 3.0));
                b.Huellwand(s, "Wand Süd-1", 0, 0, 1, 0, 12.0);
                b.Huellwand(s, "Wand Süd-2", 5000, 0, 1, 0, 10.0);
                IIfcWall ost = b.Oeffnungswand(s, "Wand Ost", 10000, 0, 0, 1, 8000, 2500, (20.0, 20.0));
                b.Oeffnungswand(s, "Wand Nord", 10000, 8000, -1, 0, 10000, 2500, (25.0, 25.0));
                b.Oeffnungswand(s, "Wand West", 0, 8000, 0, -1, 8000, 2500, (20.0, 20.0));

                foreach ((string name, double x0, double z0, double br, double h) in new[]
                         { ("Fenster A", 1000.0, 900.0, 1500.0, 1200.0), ("Fenster B", 4000.0, 1000.0, 1200.0, 1000.0), ("Fenster C", 7000.0, 900.0, 1000.0, 1200.0) })
                {
                    IIfcOpeningElement o = b.Wandoeffnung(sued, name, x0, z0, br, h, 300, null);
                    b.Oeffnungsfenster(s, sued, o, name, (br, h), null, null);
                }
                // Laut Datei in der Südwand, der Körper der Öffnung liegt in der Ostwand.
                IIfcOpeningElement od = b.Fremdoeffnung(sued, ost, "Fenster D", 3000, 1000, 1000, 1000);
                b.Oeffnungsfenster(s, sued, od, "Fenster D", (1000, 1000), null, null);
                if (schwebend)
                {
                    IIfcOpeningElement oe = b.Wandoeffnung(sued, "Fenster E", 2000, 6000, 2000, 2000, 300, null);
                    b.Oeffnungsfenster(s, sued, oe, "Fenster E", (2000, 2000), null, null);
                }
                return b.Speichern();
            }
        }

        private sealed partial class Bau
        {
            /// <summary>
            /// Eine Platte des CAD-Exports gegen unbeheizt: Körper als Prisma zwischen <paramref name="unten"/> und
            /// <paramref name="oben"/> [mm] im System des Geschosses, Angrenzung und Hüllkennung in den CAD-Sätzen, U-Wert und
            /// Fläche im Mengensatz.
            /// </summary>
            public IIfcSlab Huellplatte(IIfcBuildingStorey s, string name, (double X, double Y)[] ring, double unten, double oben,
                                        string angrenzung, double u, double flaecheM2)
            {
                IIfcSlab p = Wurzel<IIfcSlab>("IfcSlab", name);
                p.PredefinedType = IfcSlabTypeEnum.FLOOR;
                p.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, p);
                CadEigenschaften(p, "Slab", angrenzung, true, u, null, null);
                Koerper(p, "Brep", Prisma(ring, y => unten, y => oben));
                Plattenmengen(p, flaecheM2, flaecheM2);
                return p;
            }

            /// <summary>
            /// Eine Öffnung, die laut Datei die Wand <paramref name="wirt"/> durchbricht, deren Körper aber im System der Wand
            /// <paramref name="lage"/> liegt: Rechteck ab (<paramref name="x0"/>, <paramref name="z0"/>) [mm], 300 mm tief wie
            /// <see cref="Wandoeffnung"/>.
            /// </summary>
            public IIfcOpeningElement Fremdoeffnung(IIfcWall wirt, IIfcWall lage, string name, double x0, double z0, double breite, double hoehe)
            {
                IIfcOpeningElement o = Oeffnung(wirt, name);
                o.PredefinedType = IfcOpeningElementTypeEnum.OPENING;
                o.ObjectPlacement = Platzierung(lage.ObjectPlacement, 0, 0, 0);
                IIfcAxis2Placement3D achse = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                achse.Location = Punkt(0, 0, 0);
                achse.Axis = Richtung(0, -1, 0);
                achse.RefDirection = Richtung(1, 0, 0);
                IIfcExtrudedAreaSolid e = N<IIfcExtrudedAreaSolid>("IfcExtrudedAreaSolid");
                e.SweptArea = Rechteckprofil(breite, hoehe, x0 + breite / 2.0, z0 + hoehe / 2.0);
                e.Position = achse;
                e.ExtrudedDirection = Richtung(0, 0, 1);
                e.Depth = new IfcPositiveLengthMeasure(300);
                Koerper(o, "SweptSolid", e);
                return o;
            }
        }
    }
}
