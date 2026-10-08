using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Importprobe des Befunds je Bauteil</b> (Abstimmung G5, B1): Hüllwände mit lesbarem, teilweise lesbarem und
    /// unlesbarem Körper, alle mit Mengensatz (die Fläche kommt aus ihm). Selbst erzeugt, neutrale Namen, runde Werte, Längen
    /// in Millimetern; abgelegt unter <c>Referenzlaeufe/Importproben/</c> mit den Bauteilkörperproben, gehalten von
    /// <see cref="BauteilbefundTests"/>.
    ///
    /// <para><b>Das Befundhaus</b> (<see cref="Befundhaus"/>): ein Raum 10 × 8 m; sechs außen erklärte Wände ohne U-Wert —
    /// „Wand Süd“ als <c>IfcAdvancedBrep</c> (nicht lesbar, 25 m²), „Wand Ost“ als <c>IfcBooleanClippingResult</c> (nur der
    /// erste Operand, 20 m²), „Wand Nord“ als <c>IfcShellBasedSurfaceModel</c> mit offener Schale (25 m²), „Wand West“ als
    /// schlichte Extrusion (20 m²), „Wand Teil“ aus einer Extrusion und einem <c>IfcAdvancedBrep</c> (teilweise lesbar,
    /// 10 m²) und „Wand Platte“ als geschlossener <c>IfcFacetedBrep</c> mit nur einer waagerechten Fläche (entartet: keine
    /// senkrechte Seite, 12 m²). TrueNorth [0, 1, 0].</para>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        private static byte[] Befundhaus()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_g5_befund.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuildingStorey s = b.Geschoss(b.Gebaeude("Befundhaus", null), "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Wohnen", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (10000, 0), (10000, 8000), (0, 8000));

                IIfcWall sued = b.Huellwand(s, "Wand Süd", 0, 0, 1, 0, 25.0);
                b.Koerper(sued, "AdvancedBrep", b.AdvancedBrep());
                IIfcWall ost = b.Huellwand(s, "Wand Ost", 10000, 0, 0, 1, 20.0);
                b.Koerper(ost, "Clipping", b.Beschnitten());
                IIfcWall nord = b.Huellwand(s, "Wand Nord", 10000, 8000, -1, 0, 25.0);
                b.Koerper(nord, "SurfaceModel", b.Brep(10000, 300, 2500, offen: true));
                IIfcWall west = b.Huellwand(s, "Wand West", 0, 8000, 0, -1, 20.0);
                b.Koerper(west, "SweptSolid", b.Extrusion(b.Rechteckprofil(8000, 300, 4000, -150), 2500));
                IIfcWall teil = b.Huellwand(s, "Wand Teil", 0, 4000, 1, 0, 10.0);
                b.Koerper(teil, "SweptSolid", b.Extrusion(b.Rechteckprofil(4000, 300, 2000, -150), 2500), b.AdvancedBrep());
                IIfcWall platte = b.Huellwand(s, "Wand Platte", 5000, 4000, 1, 0, 12.0);
                b.Koerper(platte, "Brep", b.Flaechenkoerper(new[] { new[] { (0.0, 0.0, 0.0), (3000.0, 0.0, 0.0), (3000.0, 300.0, 0.0), (0.0, 300.0, 0.0) } },
                                                            geschlossen: true));
                return b.Speichern();
            }
        }
    }
}
