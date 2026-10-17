using System;
using System.Collections.Generic;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Importproben der Fensterrichtung aus dem eigenen Körper</b> und des Einheitenhinweises am U-Wert. Selbst erzeugt,
    /// neutrale Namen, runde Werte, Längen in Millimetern; abgelegt unter <c>Referenzlaeufe/Importproben/</c>, gehalten von
    /// <see cref="IfcFensterrichtungTests"/> (byte-gleich neu erzeugbar).
    ///
    /// <para><b>Das Fensterhaus</b>: TrueNorth [0, 1, 0], nicht gedreht. Ein beheizter Raum 10 × 8 m mit Körper (y 0 … 8 m),
    /// 2 m südlich davon ein unbeheizter Raum 10 × 8 m mit Körper (y −10 … −2 m) — der Gebäudeschwerpunkt liegt damit südlich
    /// der Südwand. Süd- und Westwand außen erklärt, ohne Körper, ohne Raumgrenze, nur mit Bruttofläche: Sie bleiben ohne
    /// Azimut. In der Südwand ein Fenster (Öffnung 1,5 × 1,2 m, 0,3 m tief, mit Körper), in der Westwand eine Tür (Öffnung
    /// 1,0 × 2,0 m mit Körper). Ein Dach aus einer Platte ohne Körper (nur Mengensatz) mit einem Dachfenster, dessen Öffnung
    /// in der Ebene 3 : 4 nach Süd liegt. Das Fenster nennt <c>Pset_WindowCommon.ThermalTransmittance (W/(m K))</c> 1,1 und
    /// denselben Wert in einem Herstellersatz, die Tür <c>Pset_DoorCommon.ThermalTransmittance (W/(m K))</c> 1,8 und im
    /// Herstellersatz 1,6.</para>
    ///
    /// <para><b>Die Gegenprobe</b> „unbestimmt“: ein Raum ohne Körper, die Südwand ohne Körper und ohne Raumgrenze mit dem
    /// Fenster — weder Raum noch Gebäudeschwerpunkt bestimmen die Außenseite.</para>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        /// <summary>Die Proben der Fensterrichtung: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Fensterrichtungsproben()
            => new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["ifc4_fensterrichtung.ifc"] = Fensterhaus("ifc4_fensterrichtung.ifc", koerper: true),
                ["ifc4_fensterrichtung_unbestimmt.ifc"] = Fensterhaus("ifc4_fensterrichtung_unbestimmt.ifc", koerper: false),
            };

        private static byte[] Fensterhaus(string datei, bool koerper)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, datei))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuildingStorey s = b.Geschoss(b.Gebaeude("Fensterhaus", null), "Erdgeschoss", 0);
                IIfcSpace wohnen = b.Raum(s, "0.01", "Wohnen", 0, 0, null, null, null, beheizt: true);
                if (koerper)
                {
                    b.Grundriss(wohnen, (0, 0), (10000, 0), (10000, 8000), (0, 8000));
                    IIfcSpace lager = b.Raum(s, "0.02", "Lager", 0, -10000, null, null, null, beheizt: false);
                    b.Grundriss(lager, (0, 0), (10000, 0), (10000, 8000), (0, 8000));
                }

                IIfcWall sued = b.Huellwand(s, "Wand Süd", 0, 0, 1, 0, 25.0);
                IIfcOpeningElement of = b.Wandoeffnung(sued, "Fenster Süd", 1000, 900, 1500, 1200, 300, null);
                b.Richtungsfuellung<IIfcWindow>(s, of, "IfcWindow", "Fenster Süd", 1500, 1200, "Pset_WindowCommon", 1.1, 1.1);
                if (!koerper) return b.Speichern();

                IIfcWall west = b.Huellwand(s, "Wand West", 0, 8000, 0, -1, 20.0);
                IIfcOpeningElement ot = b.Wandoeffnung(west, "Tür West", 3000, 0, 1000, 2000, 300, null);
                b.Richtungsfuellung<IIfcDoor>(s, ot, "IfcDoor", "Tür West", 1000, 2000, "Pset_DoorCommon", 1.8, 1.6);

                IIfcSlab dachplatte = b.Koerperplatte(s, "Dachplatte", IfcSlabTypeEnum.ROOF, 0);
                b.Plattenmengen(dachplatte, 100.0, 98.8);
                b.Dachganzes(s, "Dach", dachplatte);
                IIfcOpeningElement od = b.Dachoeffnung(dachplatte, "Dachfenster", 1000, 1200, 200, 2500);
                b.Oeffnungsfenster(s, dachplatte, od, "Dachfenster", null, 1.2, null);
                return b.Speichern();
            }
        }

        private sealed partial class Bau
        {
            /// <summary>
            /// Ein Fenster bzw. eine Tür, die die Öffnung <paramref name="o"/> füllt, ohne eigenen Körper, mit Gesamtmaßen [mm]:
            /// in <paramref name="satz"/> der U-Wert <paramref name="uDatei"/> unter dem Namen mit der Einheitenangabe
            /// „(W/(m K))“, im Satz „Herstellerdaten“ der U-Wert <paramref name="uHersteller"/> ohne Einheitenangabe.
            /// </summary>
            public T Richtungsfuellung<T>(IIfcBuildingStorey s, IIfcOpeningElement o, string typ, string name, double breite, double hoehe,
                                          string satz, double uDatei, double uHersteller) where T : class, IIfcElement
            {
                T f = Wurzel<T>(typ, name);
                Enthalten(s, f);
                Fuellen(o, f);
                if (f is IIfcWindow fenster)
                {
                    fenster.OverallWidth = new IfcPositiveLengthMeasure(breite);
                    fenster.OverallHeight = new IfcPositiveLengthMeasure(hoehe);
                }
                else if (f is IIfcDoor tuer)
                {
                    tuer.OverallWidth = new IfcPositiveLengthMeasure(breite);
                    tuer.OverallHeight = new IfcPositiveLengthMeasure(hoehe);
                }
                Satz(f, satz, ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance (W/(m K))", new IfcThermalTransmittanceMeasure(uDatei)));
                Satz(f, "Herstellerdaten", ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(uHersteller)));
                return f;
            }
        }
    }
}
