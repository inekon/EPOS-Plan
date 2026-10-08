using System;
using System.Collections.Generic;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Proben der offenen Prüfpunkte des Körperwegs</b>: selbst erzeugt, neutrale Namen, runde Werte, Längen in
    /// Millimetern, TrueNorth [0, 1, 0]; nur im Speicher, gehalten von <see cref="IfcKoerperPruefpunkteTests"/>.
    /// <list type="bullet">
    /// <item><b>Fremdnamige Teile</b> (Flachdächer ohne Raumgrenzen): „Platte A1“ als Kasten 10 × 8 m (Oberseite 80 m²) mit
    /// eigenem Mengensatz 8 m², ihre Teile ohne Darstellung heißen „Platte B1-1“ (40 m²) und „Platte B1-2“ (32 m²) — der
    /// Körper deckt die ganze Platte, die Mengensätze je Teil. „Platte C“ ist ein Kasten 5 × 8 m (40 m²) mit 4 m² im
    /// Mengensatz; die übrige Gruppe „Platte X-1“ (30 m²) deckt ihn nicht (Gegenprobe).</item>
    /// <item><b>Satteldach aus Platten mit Mengensatz am Dach</b> (ein Geschoss, keine Raumgrenzen): Räume, Traufe, First
    /// und Neigung 3 : 4 wie <see cref="TEILFLAECHEN_SATTEL"/>; das <c>IfcRoof</c> „Dach“ ohne eigene Darstellung trägt den
    /// Mengensatz 75 m² und U = 0,25 W/(m²·K) und über <c>IfcRelAggregates</c> die Platten „Dachfläche Süd“ und „Dachfläche Nord“ mit je einem
    /// Körper (10 × 3,75 m schräg, 0,25 m lotrecht dick) und ohne Mengensatz.</item>
    /// </list>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        internal const string PRUEFPUNKT_FREMDTEILE = "ifc4_pruefpunkt_fremdteile.ifc";
        internal const string PRUEFPUNKT_SATTEL_MENGE = "ifc4_pruefpunkt_satteldach_menge.ifc";

        /// <summary>Die Proben der Prüfpunkte: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Pruefpunktproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [PRUEFPUNKT_FREMDTEILE] = Fremdteilhaus(PRUEFPUNKT_FREMDTEILE),
                [PRUEFPUNKT_SATTEL_MENGE] = SatteldachMitMenge(PRUEFPUNKT_SATTEL_MENGE),
            };

        private static byte[] Fremdteilhaus(string datei)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Plattenhaus", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Halle", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (20000, 0), (20000, 12000), (0, 12000));

                IIfcRoof a = b.Flachdachteil(s, "Platte A1", 3000, 8, null);
                b.Koerper(a, "SurfaceModel", b.Kasten(0, 0, 0, 10000, 8000, 300));
                b.Flachdachteil(s, "Platte B1-1", 3000, 40, null);
                b.Flachdachteil(s, "Platte B1-2", 3000, 32, null);
                IIfcRoof c = b.Flachdachteil(s, "Platte C", 3000, 4, null);
                b.Koerper(c, "SurfaceModel", b.Kasten(12000, 0, 0, 17000, 8000, 300));
                b.Flachdachteil(s, "Platte X-1", 3000, 30, null);
                return b.Speichern();
            }
        }

        private static byte[] SatteldachMitMenge(string datei)
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
                IIfcSlab sued = b.Teilplatte(eg, "Dachfläche Süd", IfcSlabTypeEnum.ROOF, true,
                                             b.Prisma(Rechteck(0, 0, 10000, 3000), SattelSued, y => SattelSued(y) + 250.0));
                IIfcSlab nord = b.Teilplatte(eg, "Dachfläche Nord", IfcSlabTypeEnum.ROOF, true,
                                             b.Prisma(Rechteck(0, 3000, 10000, 6000), SattelNord, y => SattelNord(y) + 250.0));
                b.DachganzesMitMenge(eg, "Dach", 75.0, sued, nord);
                b.Kleinwand(eg, "Wand Süd", true, Rechteck(0, 0, 10000, 300), null0, SattelSued, null);
                b.Kleinwand(eg, "Wand Nord", true, Rechteck(0, 5700, 10000, 6000), null0, SattelNord, null);
                foreach ((string name, bool aussen, double x0, double x1) in new[] { ("Wand West", true, 0.0, 300.0), ("Wand Ost", true, 9700.0, 10000.0),
                                                                                     ("Innenwand", false, 5880.0, 6120.0) })
                    b.Teilwand(eg, name, aussen, b.Prisma(Rechteck(x0, 300, x1, 3000), null0, SattelSued),
                               b.Prisma(Rechteck(x0, 3000, x1, 5700), null0, SattelNord));
                return b.Speichern();
            }
        }

        private sealed partial class Bau
        {
            /// <summary>
            /// Ein Dach (<c>IfcRoof</c>) ohne eigene Darstellung mit Mengensatz <c>Qto_RoofBaseQuantities.GrossArea</c>
            /// <paramref name="bruttoM2"/>, das seine Platten über <c>IfcRelAggregates</c> trägt.
            /// </summary>
            public IIfcRoof DachganzesMitMenge(IIfcBuildingStorey s, string name, double bruttoM2, params IIfcSlab[] platten)
            {
                IIfcRoof d = Wurzel<IIfcRoof>("IfcRoof", name);
                d.PredefinedType = IfcRoofTypeEnum.GABLE_ROOF;
                d.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, d);
                Satz(d, "Pset_RoofCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(0.25)));
                Mengen(d, "Qto_RoofBaseQuantities", Flaeche("GrossArea", bruttoM2));
                foreach (IIfcSlab p in platten) Zerlegen(d, p);
                return d;
            }
        }
    }
}
