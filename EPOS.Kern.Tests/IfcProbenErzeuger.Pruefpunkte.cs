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
    /// <item><b>Gleichnamige Teile einer anderen Klasse</b> (wie „Fremdnamige Teile“): „Platte D“ (Körper 80 m², Mengensatz
    /// 8 m²) mit dem Dachteil „Platte D-1“ (30 m²) und der Platte „Platte D-2“ (<c>IfcSlab</c>, 42 m²) — zusammen genau der
    /// Körper. „Platte E“ (Körper 40 m², 4 m²) mit „Platte E-1“ (6 m²) und „Platte E-2“ (<c>IfcSlab</c>, 50 m²), die den
    /// Körper überdecken würde (Gegenprobe).</item>
    /// <item><b>Satteldach aus Platten mit Mengensatz am Dach</b> (ein Geschoss, keine Raumgrenzen): Räume, Traufe, First
    /// und Neigung 3 : 4 wie <see cref="TEILFLAECHEN_SATTEL"/>; das <c>IfcRoof</c> „Dach“ ohne eigene Darstellung trägt den
    /// Mengensatz 75 m² und U = 0,25 W/(m²·K) und über <c>IfcRelAggregates</c> die Platten „Dachfläche Süd“ und „Dachfläche Nord“ mit je einem
    /// Körper (10 × 3,75 m schräg, 0,25 m lotrecht dick) und ohne Mengensatz.</item>
    /// <item><b>Dieselbe Probe mit dem U-Wert an den Platten:</b> ohne U am Dach mit 0,2 an beiden Platten (gleich), mit 0,2
    /// und 0,3 an Platten mit den Mengensätzen 50 und 25 m² (verschieden), mit 0,2 an nur einer Platte (Lücke); mit 0,25 am
    /// Dach und 0,2 bzw. 0,3 an den Platten (eigen); ohne jeden U-Wert, mit einem gleichen Schichtsatz an beiden Platten
    /// (Aufbau).</item>
    /// </list>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        internal const string PRUEFPUNKT_FREMDTEILE = "ifc4_pruefpunkt_fremdteile.ifc";
        internal const string PRUEFPUNKT_FREMDKLASSE = "ifc4_pruefpunkt_fremdklasse.ifc";
        internal const string PRUEFPUNKT_SATTEL_MENGE = "ifc4_pruefpunkt_satteldach_menge.ifc";
        internal const string PRUEFPUNKT_SATTEL_U_GLEICH = "ifc4_pruefpunkt_satteldach_u_gleich.ifc";
        internal const string PRUEFPUNKT_SATTEL_U_VERSCHIEDEN = "ifc4_pruefpunkt_satteldach_u_verschieden.ifc";
        internal const string PRUEFPUNKT_SATTEL_U_LUECKE = "ifc4_pruefpunkt_satteldach_u_luecke.ifc";
        internal const string PRUEFPUNKT_SATTEL_U_EIGEN = "ifc4_pruefpunkt_satteldach_u_eigen.ifc";
        internal const string PRUEFPUNKT_SATTEL_AUFBAU = "ifc4_pruefpunkt_satteldach_aufbau.ifc";

        /// <summary>Die Proben der Prüfpunkte: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Pruefpunktproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [PRUEFPUNKT_FREMDTEILE] = Fremdteilhaus(PRUEFPUNKT_FREMDTEILE),
                [PRUEFPUNKT_FREMDKLASSE] = Fremdklassenhaus(PRUEFPUNKT_FREMDKLASSE),
                [PRUEFPUNKT_SATTEL_MENGE] = SatteldachMitMenge(PRUEFPUNKT_SATTEL_MENGE, 0.25, 0.25, 0.25, null, null),
                [PRUEFPUNKT_SATTEL_U_GLEICH] = SatteldachMitMenge(PRUEFPUNKT_SATTEL_U_GLEICH, null, 0.2, 0.2, null, null),
                [PRUEFPUNKT_SATTEL_U_VERSCHIEDEN] = SatteldachMitMenge(PRUEFPUNKT_SATTEL_U_VERSCHIEDEN, null, 0.2, 0.3, 50.0, 25.0),
                [PRUEFPUNKT_SATTEL_U_LUECKE] = SatteldachMitMenge(PRUEFPUNKT_SATTEL_U_LUECKE, null, 0.2, null, null, null),
                [PRUEFPUNKT_SATTEL_U_EIGEN] = SatteldachMitMenge(PRUEFPUNKT_SATTEL_U_EIGEN, 0.25, 0.2, 0.3, null, null),
                [PRUEFPUNKT_SATTEL_AUFBAU] = SatteldachMitMenge(PRUEFPUNKT_SATTEL_AUFBAU, null, null, null, null, null, aufbau: true),
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

        private static byte[] Fremdklassenhaus(string datei)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Plattenhaus", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Halle", 0, 0, null, null, null, beheizt: true);
                b.Grundriss(r, (0, 0), (20000, 0), (20000, 12000), (0, 12000));

                IIfcRoof d = b.Flachdachteil(s, "Platte D", 3000, 8, null);
                b.Koerper(d, "SurfaceModel", b.Kasten(0, 0, 0, 10000, 8000, 300));
                b.Flachdachteil(s, "Platte D-1", 3000, 30, null);
                b.Platte(s, "Platte D-2", IfcSlabTypeEnum.ROOF, true, null, 42.0, new IIfcSpace[0], IfcInternalOrExternalEnum.EXTERNAL);
                IIfcRoof e = b.Flachdachteil(s, "Platte E", 3000, 4, null);
                b.Koerper(e, "SurfaceModel", b.Kasten(12000, 0, 0, 17000, 8000, 300));
                b.Flachdachteil(s, "Platte E-1", 3000, 6, null);
                b.Platte(s, "Platte E-2", IfcSlabTypeEnum.ROOF, true, null, 50.0, new IIfcSpace[0], IfcInternalOrExternalEnum.EXTERNAL);
                return b.Speichern();
            }
        }

        private static byte[] SatteldachMitMenge(string datei, double? uDach, double? uSued, double? uNord, double? mengeSued, double? mengeNord,
                                                 bool aufbau = false)
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
                IIfcSlab sued = b.Dachplatte(eg, "Dachfläche Süd", uSued, mengeSued,
                                             b.Prisma(Rechteck(0, 0, 10000, 3000), SattelSued, y => SattelSued(y) + 250.0));
                IIfcSlab nord = b.Dachplatte(eg, "Dachfläche Nord", uNord, mengeNord,
                                             b.Prisma(Rechteck(0, 3000, 10000, 6000), SattelNord, y => SattelNord(y) + 250.0));
                b.DachganzesMitMenge(eg, "Dach", 75.0, uDach, sued, nord);
                if (aufbau)
                {
                    // Je Platte ein eigener Schichtsatz gleichen Namens und Inhalts (so schreiben Autorensysteme).
                    IIfcMaterial holz = b.Baustoff("Holz", 0.13, 500, 1600), daemmung = b.Baustoff("Dämmung", 0.04, 30, 1500);
                    foreach (IIfcSlab p in new[] { sued, nord })
                        b.Schichten(p, b.Schichtsatz("Dachaufbau", (holz, 25.0), (daemmung, 200.0), (holz, 25.0)),
                                    IfcLayerSetDirectionEnum.AXIS3, IfcDirectionSenseEnum.POSITIVE);
                }
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
            /// <paramref name="bruttoM2"/> und dem U-Wert <paramref name="u"/> (<c>null</c> = keiner), das seine Platten über
            /// <c>IfcRelAggregates</c> trägt.
            /// </summary>
            public IIfcRoof DachganzesMitMenge(IIfcBuildingStorey s, string name, double bruttoM2, double? u, params IIfcSlab[] platten)
            {
                IIfcRoof d = Wurzel<IIfcRoof>("IfcRoof", name);
                d.PredefinedType = IfcRoofTypeEnum.GABLE_ROOF;
                d.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, d);
                if (u.HasValue) Satz(d, "Pset_RoofCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u.Value)));
                else Satz(d, "Pset_RoofCommon", ("IsExternal", new IfcBoolean(true)));
                Mengen(d, "Qto_RoofBaseQuantities", Flaeche("GrossArea", bruttoM2));
                foreach (IIfcSlab p in platten) Zerlegen(d, p);
                return d;
            }

            /// <summary>
            /// Eine Dachplatte (<c>IfcSlab</c> <c>ROOF</c>, außen) mit Körper, dem U-Wert <paramref name="u"/> und der Fläche
            /// <paramref name="mengeM2"/> im Mengensatz (je <c>null</c> = keiner).
            /// </summary>
            public IIfcSlab Dachplatte(IIfcBuildingStorey s, string name, double? u, double? mengeM2, params IIfcRepresentationItem[] koerper)
            {
                IIfcSlab p = Wurzel<IIfcSlab>("IfcSlab", name);
                p.PredefinedType = IfcSlabTypeEnum.ROOF;
                p.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, p);
                if (u.HasValue) Satz(p, "Pset_SlabCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u.Value)));
                else Satz(p, "Pset_SlabCommon", ("IsExternal", new IfcBoolean(true)));
                if (mengeM2.HasValue) Mengen(p, "Qto_SlabBaseQuantities", Flaeche("GrossArea", mengeM2.Value));
                Koerper(p, "Brep", koerper);
                return p;
            }
        }
    }
}
