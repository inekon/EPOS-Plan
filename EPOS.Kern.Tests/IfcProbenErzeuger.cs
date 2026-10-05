using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.UtilityResource;
using Xbim.IO.Memory;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Erzeuger der IFC-Importproben</b> (Stufe G4a, Wellen 1 und 2): schreibt mit xBIM
    /// (<c>MemoryModel</c> im Schreibmodus) die Probendateien unter <c>Referenzlaeufe/Importproben/ifc*</c>
    /// — selbst erzeugt, nichts aus dem Netz (Datenaustauschkonzept 8.3). Neutrale Namen, runde Werte,
    /// keine Hersteller- oder Produktdaten, keine Normzahlen.
    ///
    /// <para><b>Deterministisch:</b> feste <c>GlobalId</c>s (fortlaufend aus einem Zähler), fester
    /// Zeitstempel und feste Kopfangaben — derselbe Aufruf liefert dieselben Bytes
    /// (<see cref="IfcProbenTests.Die_abgelegten_Proben_sind_byte_gleich_neu_erzeugbar"/>). Ein Weg für
    /// beide Schemata: Die Entitäten werden über ihren EXPRESS-Namen angelegt und über die
    /// <c>IIfc*</c>-Schnittstellen beschrieben; was IFC2X3 nicht kennt (Bereichswert mit Sollwert,
    /// Koordinatenumrechnung), fällt dort weg bzw. wird durch das IFC2X3-Gegenstück ersetzt.</para>
    ///
    /// <para><b>Das Probenhaus</b> (<see cref="Haus"/>): ein Gebäude, zwei beheizte Vollgeschosse (EG, OG)
    /// über einem Kellergeschoss mit einem unbeheizten Raum „Keller"; vier Außenwandrichtungen, das
    /// Modell gegen Nord gedreht (TrueNorth [−2, 1, 0], also 63,43°), Längen in Millimetern, Flächen in
    /// m² ohne Prefix. Die Erwartungswerte stehen in <c>IfcImportTests</c> und folgen aus den Zahlen
    /// hier.</para>
    /// </summary>
    internal static partial class IfcProbenErzeuger
    {
        /// <summary>Der feste Zeitstempel im Kopf jeder Probe.</summary>
        public const string ZEITSTEMPEL = "2026-09-25T00:00:00";

        /// <summary>Die erzeugten Proben: Dateiname → Inhalt.</summary>
        public static IReadOnlyDictionary<string, byte[]> Alle()
        {
            byte[] haus = Haus(XbimSchemaVersion.Ifc4, "ifc4_haus.ifc");
            return new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["ifc4_haus.ifc"] = haus,
                ["ifc2x3_haus.ifc"] = Haus(XbimSchemaVersion.Ifc2X3, "ifc2x3_haus.ifc"),
                ["ifc2x3_enthaltensein.ifc"] = Enthaltensein(),
                ["ifc2x3_referenzen.ifc"] = Referenzen(),
                ["ifc4_haus.ifczip"] = Zip("ifc4_haus.ifc", haus),
                ["ifc4x1_kopf.ifc"] = Ifc4x1Kopf(),
                ["ifc4_zwei_gebaeude.ifc"] = ZweiGebaeude(),
                ["ifc4_ohne_mengen.ifc"] = OhneMengen(),
                ["ifc4_mapconversion.ifc"] = MapConversion(),
                ["ifc4_schichten.ifc"] = Schichten(XbimSchemaVersion.Ifc4, "ifc4_schichten.ifc", nullwerte: false),
                ["ifc4_schichten_nullwerte.ifc"] = Schichten(XbimSchemaVersion.Ifc4, "ifc4_schichten_nullwerte.ifc", nullwerte: true),
                ["ifc2x3_schichten.ifc"] = Schichten(XbimSchemaVersion.Ifc2X3, "ifc2x3_schichten.ifc", nullwerte: false),
                ["ifc4_schichtdicken_mm.ifc"] = SchichtdickenMillimeter(),
                ["ifc4_rueckfaelle.ifc"] = Rueckfaelle(),
                ["ifc4_vorhangfassade.ifc"] = Fassadenhaus(),
                ["ifc4_haus_materialnamen.ifc"] = Haus(XbimSchemaVersion.Ifc4, "ifc4_haus_materialnamen.ifc", materialnamen: true),
                ["ifc4_zonen.ifc"] = Zonenhaus(),
                ["ifc4_z6_sollwerte.ifc"] = Z6Sollwerte(),
                ["ifc4_z6_cad.ifc"] = Z6Cad(),
            };
        }

        // ==================================================================
        //  Zonen (Stufe G6c)
        // ==================================================================

        /// <summary>
        /// <b>Das Zonenhaus</b> (Stufe G6c): drei Geschosse, Grundriss 10 m × 8 m, Längen in mm. KG: „Lager"
        /// (80 m², ohne Heizsollwert, nur Grenzen gegen Erdreich — Regel B5). EG: „Wohnen" (48 m²),
        /// „Küche" (32 m²); OG: „Schlafen" (48 m²), „Bad" (30 m²), „Abstellraum" (1,5 m², Heizsollwert 20 °C —
        /// B3 vor der Namensregel). Raumgrenzen der 2. Ebene (<c>IfcRelSpaceBoundary2ndLevel</c>); Polygone an
        /// der Fassade Süd (ein Bauteil über EG und OG) und an der Geschossdecke (0,2 m dick): Wohnen/Schlafen
        /// je 48 m² übereinander, Küche 32 m² gegen Bad 30 m² als Gegenstücke der Datei, Abstellraum 1,5 m²
        /// ohne Gegenstück. Kellerdecke, Außen- und Innenwände ohne Geometrie. Zonen (<c>IfcZone</c>):
        /// „Wohnbereich" {Wohnen}, „Küchenbereich" {Küche}, „Obergeschoss" mit den geschachtelten
        /// „Schlafbereich" {Schlafen} und „Nassbereich" {Bad}; der Abstellraum liegt in „Abstellzone" UND
        /// „Nebenräume" (mehrfach), das Lager in keiner. Klassifikation „Probenklassifikation": WO, SL, NB.
        /// </summary>
        public static byte[] Zonenhaus()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_zonen.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Zonenhaus", "2010");
                IIfcBuildingStorey kg = b.Geschoss(g, "Kellergeschoss", -3000);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcBuildingStorey og = b.Geschoss(g, "Obergeschoss", 3000);

                IIfcSpace lager = b.Raum(kg, "K.01", "Lager", 100, 100, 80, 2600, 208, beheizt: false);
                IIfcSpace wohnen = b.Raum(eg, "0.01", "Wohnen", 100, 100, 48, 2800, 134.4, beheizt: true);
                IIfcSpace kueche = b.Raum(eg, "0.02", "Küche", 6100, 100, 32, 2800, 89.6, beheizt: true);
                IIfcSpace schlafen = b.Raum(og, "1.01", "Schlafen", 100, 100, 48, 2800, 134.4, beheizt: true);
                IIfcSpace bad = b.Raum(og, "1.02", "Bad", 6100, 100, 30, 2800, 84, beheizt: true);
                IIfcSpace abstell = b.Raum(og, "1.03", "Abstellraum", 6100, 7600, 1.5, 2800, 4.2, beheizt: true);
                IIfcSpace[] keine = new IIfcSpace[0];
                IIfcWallType typ = b.Wandtyp("Außenwand Typ Z", 0.28);
                var ext = IfcInternalOrExternalEnum.EXTERNAL;
                var erde = IfcInternalOrExternalEnum.EXTERNAL_EARTH;
                var innen = IfcInternalOrExternalEnum.INTERNAL;

                // Kellergeschoss: Wände und Bodenplatte gegen Erdreich.
                foreach (Wandlage l in Wandlage.Alle)
                {
                    IIfcWall w = b.Wand(kg, "KG " + l.Name, l, typ, null, "BaseQuantities", l.Lang ? 30.0 : 24.0, null, null, keine);
                    b.Grenze2(lager, w, erde);
                }
                IIfcSlab bp = b.Platte(kg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, aussen: true, u: 0.3, brutto: 80.0, raeume: keine, grenze: erde);
                b.Grenze2(lager, bp, erde);
                IIfcSlab kd = b.Platte(eg, "Kellerdecke", IfcSlabTypeEnum.FLOOR, aussen: false, u: 0.3, brutto: 80.0, raeume: keine, grenze: innen);
                foreach (IIfcSpace r in new[] { lager, wohnen, kueche }) b.Grenze2(r, kd, innen);

                // Die Fassade Süd über zwei Geschosse, je Raum ein Polygon (Außenseite −y).
                double[] sued = { 0, -1, 0 };
                IIfcWall fs = b.Wand(eg, "Fassade Süd", Wandlage.Sued, typ, null, "BaseQuantities", 60.0, null, null, keine);
                b.Grenze2(wohnen, fs, ext, sued, new double[] { 0, 0, 0 }, new double[] { 6000, 0, 0 }, new double[] { 6000, 0, 2800 }, new double[] { 0, 0, 2800 });
                b.Grenze2(kueche, fs, ext, sued, new double[] { 6000, 0, 0 }, new double[] { 10000, 0, 0 }, new double[] { 10000, 0, 2800 }, new double[] { 6000, 0, 2800 });
                b.Grenze2(schlafen, fs, ext, sued, new double[] { 0, 0, 0 }, new double[] { 6000, 0, 0 }, new double[] { 6000, 0, 2800 }, new double[] { 0, 0, 2800 });
                b.Grenze2(bad, fs, ext, sued, new double[] { 6000, 0, 0 }, new double[] { 10000, 0, 0 }, new double[] { 10000, 0, 2800 }, new double[] { 6000, 0, 2800 });
                b.Grenze2(wohnen, b.Fenster(fs, eg, "F-S-EG", flaeche: 4.0), ext);
                b.Grenze2(schlafen, b.Fenster(fs, og, "F-S-OG", flaeche: 3.0), ext);

                // Die übrigen Außenwände je Geschoss, ohne Geometrie.
                IIfcWall egN = b.Wand(eg, "EG Nord", Wandlage.Nord, typ, null, "BaseQuantities", 28.0, null, null, keine);
                b.Grenze2(wohnen, egN, ext);
                b.Grenze2(kueche, egN, ext);
                b.Grenze2(kueche, b.Wand(eg, "EG Ost", Wandlage.Ost, typ, null, "BaseQuantities", 22.4, null, null, keine), ext);
                b.Grenze2(wohnen, b.Wand(eg, "EG West", Wandlage.West, typ, null, "BaseQuantities", 22.4, null, null, keine), ext);
                IIfcWall ogN = b.Wand(og, "OG Nord", Wandlage.Nord, typ, null, "BaseQuantities", 28.0, null, null, keine);
                b.Grenze2(schlafen, ogN, ext);
                b.Grenze2(bad, ogN, ext);
                b.Grenze2(bad, b.Wand(og, "OG Ost", Wandlage.Ost, typ, null, "BaseQuantities", 22.4, null, null, keine), ext);
                b.Grenze2(schlafen, b.Wand(og, "OG West", Wandlage.West, typ, null, "BaseQuantities", 22.4, null, null, keine), ext);

                // Die Geschossdecke mit Polygonen: EG-Decken bei z = 2,8 m (Normale nach oben), OG-Böden bei z = 0.
                IIfcSlab gd = b.Platte(og, "Geschossdecke", IfcSlabTypeEnum.FLOOR, aussen: false, u: 0.5, brutto: 80.0, raeume: keine, grenze: innen);
                b.Dicke(gd, 200);
                double[] hoch = { 0, 0, 1 }, runter = { 0, 0, -1 };
                b.Grenze2(wohnen, gd, innen, hoch, new double[] { 0, 0, 2800 }, new double[] { 6000, 0, 2800 }, new double[] { 6000, 8000, 2800 }, new double[] { 0, 8000, 2800 });
                IIfcRelSpaceBoundary kuecheOben = b.Grenze2(kueche, gd, innen, hoch,
                    new double[] { 6000, 0, 2800 }, new double[] { 10000, 0, 2800 }, new double[] { 10000, 8000, 2800 }, new double[] { 6000, 8000, 2800 });
                b.Grenze2(schlafen, gd, innen, runter, new double[] { 0, 0, 0 }, new double[] { 0, 8000, 0 }, new double[] { 6000, 8000, 0 }, new double[] { 6000, 0, 0 });
                IIfcRelSpaceBoundary badUnten = b.Grenze2(bad, gd, innen, runter,
                    new double[] { 6000, 0, 0 }, new double[] { 6000, 7500, 0 }, new double[] { 10000, 7500, 0 }, new double[] { 10000, 0, 0 });
                b.Gegenstuecke(kuecheOben, badUnten);
                b.Grenze2(abstell, gd, innen, runter, new double[] { 6000, 7500, 0 }, new double[] { 6000, 9000, 0 }, new double[] { 7000, 9000, 0 }, new double[] { 7000, 7500, 0 });

                // Innenwände ohne Geometrie; das Dach über allen OG-Räumen.
                IIfcWall iwEg = b.Innenwand(eg, "IW EG", 6000, 0, 22.4, keine);
                b.Grenze2(wohnen, iwEg, innen);
                b.Grenze2(kueche, iwEg, innen);
                IIfcWall iwOg = b.Innenwand(og, "IW OG", 6000, 0, 22.4, keine);
                b.Grenze2(schlafen, iwOg, innen);
                b.Grenze2(bad, iwOg, innen);
                IIfcWall iwAbstell = b.Innenwand(og, "IW Abstellraum", 6000, 7500, 4.2, keine);
                b.Grenze2(bad, iwAbstell, innen);
                b.Grenze2(abstell, iwAbstell, innen);
                IIfcRoof dach = b.Dach(og, "Dach", u: 0.2, brutto: 80.0, raeume: keine);
                foreach (IIfcSpace r in new[] { schlafen, bad, abstell }) b.Grenze2(r, dach, ext);

                // Zonen der Datei, geschachtelt und einmal mehrfach; Klassifikation.
                b.Zone("Wohnbereich", wohnen);
                b.Zone("Küchenbereich", kueche);
                b.Zone("Obergeschoss", b.Zone("Schlafbereich", schlafen), b.Zone("Nassbereich", bad));
                b.Zone("Abstellzone", abstell);
                b.Zone("Nebenräume", abstell);
                foreach ((IIfcSpace r, string k) in new[] { (lager, "NB"), (wohnen, "WO"), (kueche, "WO"), (schlafen, "SL"), (bad, "SL"), (abstell, "SL") })
                    b.Klasse(r, "Probenklassifikation", k);
                return b.Speichern();
            }
        }

        // ==================================================================
        //  Vorhangfassaden (Stufe G4b)
        // ==================================================================

        /// <summary>
        /// <b>Das Fassadenhaus</b>: ein Gebäude, ein beheizter Raum (80 m², 2,5 m, 200 m³); im Süden eine
        /// Vorhangfassade (<c>IfcCurtainWall</c>) von 25 m² mit U-Wert 1,3 W/(m²K), im Westen eine von 20 m²
        /// ohne U-Wert; Ost- und Nordwand (20 und 25 m²) mit U-Wert 0,3 am Wandtyp, ein Fenster 5 m² in der
        /// Nordwand; Dach- und Bodenplatte je 80 m² mit U-Wert 0,2 und 0,35. Kein Nordwinkel.
        /// </summary>
        public static byte[] Fassadenhaus()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_vorhangfassade.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Fassadenhaus", null);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(eg, "0.01", "Büro", 150, 150, 80, 2500, 200, beheizt: true);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ F", 0.3);
                b.Vorhangfassade(eg, "Glasfassade Süd", Wandlage.Sued, 1.3, 25.0, new[] { r });
                b.Wand(eg, "Ost", Wandlage.Ost, typ, null, "BaseQuantities", 20.0, null, null, new[] { r });
                IIfcWall nord = b.Wand(eg, "Nord", Wandlage.Nord, typ, null, "BaseQuantities", 25.0, null, null, new[] { r });
                b.Fenster(nord, eg, "F-N", flaeche: 5.0);
                b.Vorhangfassade(eg, "Glasfassade West", Wandlage.West, null, 20.0, new[] { r });
                b.Platte(eg, "Dach", IfcSlabTypeEnum.ROOF, aussen: true, u: 0.2, brutto: 80.0,
                         raeume: new[] { r }, grenze: IfcInternalOrExternalEnum.EXTERNAL);
                b.Platte(eg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, aussen: true, u: 0.35, brutto: 80.0,
                         raeume: new[] { r }, grenze: IfcInternalOrExternalEnum.EXTERNAL_EARTH);
                return b.Speichern();
            }
        }

        // ==================================================================
        //  Schichten und Rückfälle (Stufe G4a Welle 2)
        // ==================================================================

        /// <summary>
        /// <b>Das Schichtenhaus</b>: ein Gebäude, ein beheizter Raum (80 m², 2,5 m, 200 m³), vier Außenwände
        /// (brutto 25/20/25/20 m², ein Fenster 5 m² in der Südwand), Dach- und Bodenplatte je 80 m² — ohne
        /// U-Werte, alles über <c>IfcMaterialLayerSetUsage</c> mit Stoffwerten in <c>Pset_MaterialThermal</c>
        /// und <c>Pset_MaterialCommon</c> (IFC2X3: <c>IfcThermalMaterialProperties</c>,
        /// <c>IfcGeneralMaterialProperties</c> und für die Dämmung zusätzlich
        /// <c>IfcExtendedMaterialProperties</c>). Süd- und Nordwand zählen ihre Schichten innen zuerst und
        /// legen sie gegen die lokale y-Achse (<c>NEGATIVE</c>), Ost- und Westwand außen zuerst längs der
        /// Achse (<c>POSITIVE</c>) — physikalisch dieselbe Wand; die Annahme „erste Schicht außen" läge bei
        /// Süd und Nord falsch. Mit <paramref name="nullwerte"/> trägt die Dämmung ρ = 0 und c = 0
        /// (Befund P). Kein Nordwinkel (TrueNorth [0, 1]).
        /// </summary>
        public static byte[] Schichten(XbimSchemaVersion schema, string dateiname, bool nullwerte, bool ohneWaermekapazitaet = false)
        {
            using (var b = new Bau(schema, dateiname))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Schichtenhaus", null);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(eg, "0.01", "Wohnen", 150, 150, 80, 2500, 200, beheizt: true);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ S", null);

                // Abwandlung „ohne Wärmekapazität" (nur im Speicher): Putz und Mauerwerk führen Dichte und λ, aber kein c.
                IIfcMaterial putz = b.Baustoff("Putz", 0.7, 1400, ohneWaermekapazitaet ? double.NaN : 1000);
                IIfcMaterial mauer = b.Baustoff("Mauerwerk", 0.5, 1200, ohneWaermekapazitaet ? double.NaN : 1000);
                IIfcMaterial daemm = nullwerte ? b.Baustoff("Dämmung", 0.04, 0, 0) : b.Baustoff("Dämmung", 0.04, 30, 1500, erweitert2x3: true);
                IIfcMaterial beton = b.Baustoff("Beton", 2.0, 2400, 1000);
                IIfcMaterialLayerSet innenZuerst = b.Schichtsatz("Außenwand innen zuerst", (putz, 15), (mauer, 240), (daemm, 100));
                IIfcMaterialLayerSet aussenZuerst = b.Schichtsatz("Außenwand außen zuerst", (daemm, 100), (mauer, 240), (putz, 15));
                IIfcMaterialLayerSet dach = b.Schichtsatz("Dach", (beton, 200), (daemm, 200));
                IIfcMaterialLayerSet boden = b.Schichtsatz("Bodenplatte", (daemm, 100), (beton, 200));

                foreach (Wandlage l in Wandlage.Alle)
                {
                    IIfcWall w = b.Wand(eg, l.Name, l, typ, null, "BaseQuantities", l.Lang ? 25.0 : 20.0, null, null, new[] { r });
                    bool innen = l == Wandlage.Sued || l == Wandlage.Nord;
                    b.Schichten(w, innen ? innenZuerst : aussenZuerst, IfcLayerSetDirectionEnum.AXIS2,
                                innen ? IfcDirectionSenseEnum.NEGATIVE : IfcDirectionSenseEnum.POSITIVE);
                    if (l == Wandlage.Sued) b.Fenster(w, eg, "F-S", flaeche: 5.0);
                }
                IIfcSlab d = b.Platte(eg, "Dach", IfcSlabTypeEnum.ROOF, aussen: true, u: null, brutto: 80.0,
                                      raeume: new[] { r }, grenze: IfcInternalOrExternalEnum.EXTERNAL);
                b.Schichten(d, dach, IfcLayerSetDirectionEnum.AXIS3, IfcDirectionSenseEnum.POSITIVE);
                IIfcSlab p = b.Platte(eg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, aussen: true, u: null, brutto: 80.0,
                                      raeume: new[] { r }, grenze: IfcInternalOrExternalEnum.EXTERNAL_EARTH);
                b.Schichten(p, boden, IfcLayerSetDirectionEnum.AXIS3, IfcDirectionSenseEnum.POSITIVE);
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Das Millimeterhaus</b> (Rückfall „Schichtdicke in Millimetern"): Längeneinheit <c>METRE</c> ohne
        /// Präfix, aber die Schichtdicken der Außenwand in Millimetern geschrieben, wie CAD-Exporte es tun —
        /// Folie 0.2, Blech 0.9, Dämmung 160., Beton 200.; das Dach dagegen in Metern (Beton 0.2, Dämmung 0.016), die
        /// Gegenprobe. Ein beheizter Raum (80 m², 2,5 m, 200 m³), Südwand 25 m², Dach 80 m², ohne U-Werte.
        /// </summary>
        public static byte[] SchichtdickenMillimeter()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_schichtdicken_mm.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false, meter: true);
                IIfcBuilding g = b.Gebaeude("Millimeterhaus", null);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(eg, "0.01", "Wohnen", 0.15, 0.15, 80, 2.5, 200, beheizt: true);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ M", null);

                IIfcMaterial folie = b.Baustoff("Folie", 0.2, 900, 1800);
                IIfcMaterial blech = b.Baustoff("Blech", 50, 7800, 500);
                IIfcMaterial daemm = b.Baustoff("Dämmung", 0.04, 30, 1500);
                IIfcMaterial beton = b.Baustoff("Beton", 2.0, 2400, 1000);
                IIfcMaterialLayerSet wand = b.Schichtsatz("Außenwand Millimeter", (folie, 0.2), (blech, 0.9), (daemm, 160), (beton, 200));
                IIfcMaterialLayerSet dach = b.Schichtsatz("Dach Meter", (beton, 0.2), (daemm, 0.016));

                IIfcWall w = b.Wand(eg, Wandlage.Sued.Name, Wandlage.Sued, typ, null, "BaseQuantities", 25.0, null, null, new[] { r });
                b.Schichten(w, wand, IfcLayerSetDirectionEnum.AXIS2, IfcDirectionSenseEnum.POSITIVE);
                IIfcSlab d = b.Platte(eg, "Dach", IfcSlabTypeEnum.ROOF, aussen: true, u: null, brutto: 80.0,
                                      raeume: new[] { r }, grenze: IfcInternalOrExternalEnum.EXTERNAL);
                b.Schichten(d, dach, IfcLayerSetDirectionEnum.AXIS3, IfcDirectionSenseEnum.POSITIVE);
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Das Rückfallhaus</b> (Umsetzungskonzept 3.4, Spalte „Rückfall"): zwei Geschosse mit je einem
        /// beheizten Raum (60 und 50 m²) ohne Höhe und ohne Volumen, das Obergeschoss mit der
        /// Bruttogrundfläche 70 m²; zwei Außenwände mit Mengen, Dach- und Bodenplatte OHNE Mengen, keine
        /// Tür — Raumhöhe, Dach-, Grund- und sonstige Fläche fallen auf ihre Vorgaben.
        /// </summary>
        public static byte[] Rueckfaelle()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_rueckfaelle.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Rückfallhaus", null);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcBuildingStorey og = b.Geschoss(g, "Obergeschoss", 2800, bruttoM2: 70.0);
                IIfcSpace unten = b.Raum(eg, "0.01", "Wohnen", 150, 150, 60, null, null, beheizt: true);
                IIfcSpace oben = b.Raum(og, "1.01", "Schlafen", 150, 150, 50, null, null, beheizt: true);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ R", 0.3);
                b.Wand(eg, "EG Süd", Wandlage.Sued, typ, null, "BaseQuantities", 25.0, null, null, new[] { unten });
                b.Wand(og, "OG Süd", Wandlage.Sued, typ, null, "BaseQuantities", 25.0, null, null, new[] { oben });
                b.Platte(og, "Dach", IfcSlabTypeEnum.ROOF, aussen: true, u: 0.2, brutto: null,
                         raeume: new[] { oben }, grenze: IfcInternalOrExternalEnum.EXTERNAL);
                b.Platte(eg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, aussen: true, u: 0.4, brutto: null,
                         raeume: new[] { unten }, grenze: IfcInternalOrExternalEnum.EXTERNAL_EARTH);
                return b.Speichern();
            }
        }

        // ==================================================================
        //  Das Probenhaus
        // ==================================================================

        /// <summary>
        /// Das Probenhaus im Schema <paramref name="schema"/> (IFC4 oder IFC2X3). Mit
        /// <paramref name="materialnamen"/> tragen Außenwände, Dach, Keller- und Geschossdecke und die
        /// Innenwand Schichtsätze mit den Materialnamen, wie Autorensysteme sie schreiben (Befund P, § 3.6:
        /// Kennungsschwänze, Marken, deutsche und englische Vorlagen, Luftschicht, Schraffur, ein Name ohne
        /// Treffer), und — wie in den gemessenen Dateien — mit <c>Pset_MaterialThermal</c> voller Nullen:
        /// die Probe des Namensabgleichs. Ohne den Schalter bleibt die Datei, wie sie war.
        /// </summary>
        public static byte[] Haus(XbimSchemaVersion schema, string dateiname, bool materialnamen = false)
        {
            using (var b = new Bau(schema, dateiname))
            {
                b.Anfang(new[] { -2.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding haus = b.Gebaeude("Probenhaus", "ca. 1965");
                IIfcBuildingStorey kg = b.Geschoss(haus, "Kellergeschoss", -2500);
                IIfcBuildingStorey eg = b.Geschoss(haus, "Erdgeschoss", 0);
                IIfcBuildingStorey og = b.Geschoss(haus, "Obergeschoss", 2800);

                IIfcSpace keller = b.Raum(kg, "K.01", "Keller", 150, 150, 70, 2300, 161, beheizt: false);
                IIfcSpace wohnen = b.Raum(eg, "0.01", "Wohnen", 150, 150, 40, 2600, 104, beheizt: true);
                IIfcSpace kueche = b.Raum(eg, "0.02", "Küche", 6150, 150, 25, 2600, 65, beheizt: true);
                IIfcSpace schlafen = b.Raum(og, "1.01", "Schlafen", 150, 150, 45, 2400, 108, beheizt: true);
                IIfcSpace bad = b.Raum(og, "1.02", "Bad", 6150, 150, 20, 2400, 48, beheizt: true);

                // Der Wandtyp trägt IsExternal und den U-Wert 0,4 — das Vorkommnis kann ihn überschreiben.
                IIfcWallType typ = b.Wandtyp("Außenwand Typ A", 0.4);

                // Kellergeschoss: vier Außenwände nur am unbeheizten Keller — sie zählen nicht zur Hülle.
                foreach (Wandlage l in Wandlage.Alle)
                    b.Wand(kg, "KG " + l.Name, l, typ, u: null, satz: "BaseQuantities",
                           brutto: l.Lang ? 25.0 : 20.0, netto: null, laengeHoehe: null,
                           raeume: new[] { keller }, grenze: IfcInternalOrExternalEnum.EXTERNAL_EARTH);

                // Erdgeschoss. Modell-Süd (−y) → geografisch West, Ost (+x) → Süd, Nord (+y) → Ost, West (−x) → Nord.
                IIfcWall egS = b.Wand(eg, "EG Süd", Wandlage.Sued, typ, null, "BaseQuantities", 28.0, 24.0, null, new[] { wohnen, kueche });
                IIfcWall egO = b.Wand(eg, "EG Ost", Wandlage.Ost, typ, null, "BaseQuantities", 22.4, 16.4, null, new[] { kueche });
                IIfcWall egN = b.Wand(eg, "EG Nord", Wandlage.Nord, typ, null, "BaseQuantities", 28.0, 25.0, null, new[] { wohnen, kueche });
                IIfcWall egW = b.Wand(eg, "EG West", Wandlage.West, typ, null, "BaseQuantities", 22.4, 20.4, null, new[] { wohnen });
                b.Fenster(egS, eg, "F-EG-S", flaeche: 4.0);
                b.Fenster(egO, eg, "F-EG-O", flaeche: 6.0);
                b.Fenster(egN, eg, "F-EG-N", flaeche: 3.0);
                b.Tuer(egW, eg, "Haustür", breiteMm: 1000, hoeheMm: 2000);

                // Obergeschoss: Mengensatz unter dem Vorlagennamen; die Südwand nur mit Länge × Höhe (mm).
                IIfcWall ogS = b.Wand(og, "OG Süd", Wandlage.Sued, typ, null, "Qto_WallBaseQuantities", null, null, (10000.0, 2800.0), new[] { schlafen, bad });
                IIfcWall ogO = b.Wand(og, "OG Ost", Wandlage.Ost, typ, 0.25, "Qto_WallBaseQuantities", 22.4, 18.4, null, new[] { bad });
                IIfcWall ogN = b.Wand(og, "OG Nord", Wandlage.Nord, typ, null, "Qto_WallBaseQuantities", 28.0, 26.0, null, new[] { schlafen, bad });
                IIfcWall ogW = b.Wand(og, "OG West", Wandlage.West, typ, 0.25, "Qto_WallBaseQuantities", 22.4, 21.4, null, new[] { schlafen });
                b.Fenster(ogO, og, "F-OG-O", flaeche: 4.0);
                b.Fenster(ogN, og, "F-OG-N", flaeche: 2.0);
                b.Fenster(ogW, og, "F-OG-W", flaeche: null, breiteHoeheMm: (1000.0, 1000.0));
                _ = ogS;

                // Innenwand zwischen zwei beheizten Räumen: innere Masse, keine Hülle.
                IIfcWall innen = b.Innenwand(eg, "EG Innenwand", 6000, 0, 20.8, new[] { wohnen, kueche });

                // Decken und Dach.
                IIfcSlab kellerdecke = b.Platte(eg, "Kellerdecke", IfcSlabTypeEnum.FLOOR, aussen: false, u: 0.35, brutto: 80.0,
                                                raeume: new[] { wohnen, kueche, keller }, grenze: IfcInternalOrExternalEnum.INTERNAL);
                IIfcSlab geschossdecke = b.Platte(og, "Geschossdecke", IfcSlabTypeEnum.FLOOR, aussen: false, u: null, brutto: 80.0,
                                                  raeume: new[] { wohnen, kueche, schlafen, bad }, grenze: IfcInternalOrExternalEnum.INTERNAL);
                b.Platte(kg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, aussen: true, u: 0.5, brutto: 80.0,
                         raeume: new[] { keller }, grenze: IfcInternalOrExternalEnum.EXTERNAL_EARTH);
                IIfcRoof dach = b.Dach(og, "Dach", u: 0.2, brutto: 80.0, raeume: new[] { schlafen, bad });

                if (materialnamen)
                {
                    // Stoffwerte wie in den gemessenen Dateien: vorhanden, aber voller Nullen (Befund P, § 3.4).
                    IIfcMaterial putzKz = b.Baustoff("Putz, Kalk-Zement", 0, 0, 0);
                    IIfcMaterial mineralwolle = b.Baustoff("Mineralwolle 102890377", 0, 0, 0);
                    IIfcMaterial ks = b.Baustoff("Kalksandstein 2816491304", 0, 0, 0);
                    IIfcMaterial gipsputz = b.Baustoff("Gipsputz", 0, 0, 0);
                    IIfcMaterial stucco = b.Baustoff("Stucco", 0, 0, 0);
                    IIfcMaterial rigid = b.Baustoff("Insulation / Thermal Barriers - Rigid insulation", 0, 0, 0);
                    IIfcMaterial luft = b.Baustoff("Air", 0, 0, 0);
                    IIfcMaterial ziegel = b.Baustoff("Masonry - Brick", 0, 0, 0);
                    IIfcMaterial gk = b.Baustoff("Gypsum Wall Board", 0, 0, 0);
                    IIfcMaterial bitumen = b.Baustoff("Abdichtung - Bitumen", 0, 0, 0);
                    IIfcMaterial eps = b.Baustoff("EPS 035", 0, 0, 0);
                    IIfcMaterial stahlbeton = b.Baustoff("Stahlbeton 65690", 0, 0, 0);
                    IIfcMaterial schraffur = b.Baustoff("Radial Gradient Fill 1515460218", 0, 0, 0);
                    IIfcMaterial fussboden = b.Baustoff("Fußbodenaufbau", 0, 0, 0);
                    IIfcMaterial ortbeton = b.Baustoff("Ortbeton - bewehrt", 0, 0, 0);
                    IIfcMaterial estrich = b.Baustoff("Estrich", 0, 0, 0);
                    IIfcMaterial tritt = b.Baustoff("Trittschalldämmung", 0, 0, 0);
                    IIfcMaterial ortbetonPutz = b.Baustoff("Ortbeton - bewehrt Verputzt", 0, 0, 0);
                    IIfcMaterial leichtbeton = b.Baustoff("Leichtbeton 102890359", 0, 0, 0);
                    IIfcMaterial solid = b.Baustoff("Solid 397409098", 0, 0, 0);

                    // Außenwand EG (deutsche Vorlage) und OG (englische Vorlage), erste Schicht außen.
                    IIfcMaterialLayerSet awEg = b.Schichtsatz("AW Kalksandstein WDVS", (putzKz, 20), (mineralwolle, 140), (ks, 175), (gipsputz, 15));
                    IIfcMaterialLayerSet awOg = b.Schichtsatz("Exterior - Brick on Rigid", (stucco, 20), (rigid, 100), (luft, 40), (ziegel, 175), (gk, 12.5));
                    foreach (IIfcWall w in new[] { egS, egO, egN, egW })
                        b.Schichten(w, awEg, IfcLayerSetDirectionEnum.AXIS2, IfcDirectionSenseEnum.POSITIVE);
                    foreach (IIfcWall w in new[] { ogS, ogO, ogN, ogW })
                        b.Schichten(w, awOg, IfcLayerSetDirectionEnum.AXIS2, IfcDirectionSenseEnum.POSITIVE);

                    // Flachdach von oben: Abdichtung, Dämmung, Stahlbeton — und eine Schraffur.
                    b.Schichten(dach, b.Schichtsatz("Flachdach", (bitumen, 10), (eps, 200), (stahlbeton, 200), (schraffur, 1)),
                                IfcLayerSetDirectionEnum.AXIS3, IfcDirectionSenseEnum.POSITIVE);
                    // Kellerdecke von oben: ein Sammelname ohne Treffer, dann Ortbeton und Dämmung.
                    b.Schichten(kellerdecke, b.Schichtsatz("Kellerdecke", (fussboden, 80), (ortbeton, 180), (mineralwolle, 60)),
                                IfcLayerSetDirectionEnum.AXIS3, IfcDirectionSenseEnum.POSITIVE);
                    // Geschossdecke von oben.
                    b.Schichten(geschossdecke, b.Schichtsatz("Geschossdecke", (estrich, 50), (tritt, 30), (ortbetonPutz, 200)),
                                IfcLayerSetDirectionEnum.AXIS3, IfcDirectionSenseEnum.POSITIVE);
                    // Innenwand: Leichtbeton und eine Schraffur als Schicht.
                    b.Schichten(innen, b.Schichtsatz("IW Leichtbeton", (leichtbeton, 115), (solid, 1)),
                                IfcLayerSetDirectionEnum.AXIS2, IfcDirectionSenseEnum.POSITIVE);
                }

                return b.Speichern();
            }
        }

        // ==================================================================
        //  Fehlerbilder
        // ==================================================================

        /// <summary>Ein kleines IFC4-Modell, dessen Kopf das nicht angenommene Schema IFC4X1 nennt.</summary>
        public static byte[] Ifc4x1Kopf()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4x1_kopf.ifc"))
            {
                b.Anfang(null, karte: false);
                b.Gebaeude("Probengebäude", null);
                string text = Encoding.ASCII.GetString(b.Speichern());
                return Encoding.ASCII.GetBytes(text.Replace("FILE_SCHEMA (('IFC4'));", "FILE_SCHEMA (('IFC4X1'));"));
            }
        }

        /// <summary>
        /// <b>Räume über das Enthaltensein, Mengen unter fremdem Satznamen</b> (IFC2X3, Mehrzonenkonzept 6.5) —
        /// nach dem Muster eines CAD-Exports: Die Geschosse „EG" (0 mm) und „OG" (2800 mm) hängen über
        /// <c>IfcRelContainedInSpatialStructure</c> am Gebäude, die Räume ebenso am Geschoss; der Raumname
        /// steht in <c>Name</c>, <c>LongName</c> fehlt. Die Mengen stehen im Satz <c>CAD_RaumQuantities</c>
        /// als <c>Area</c>/<c>Volume</c>/<c>Height</c>: EG „Wohnen" 40 m², 100 m³, 2,5 m; „Küche" 20 m², 50 m³,
        /// 2,5 m — zusätzlich über <c>IfcRelAggregates</c> am EG (beide Wege, einmal gezählt); OG „Schlafen"
        /// 30 m², 72 m³, 2,4 m; „Abstellraum" 5 m², 12 m³, 2,4 m (nach dem Namen unbeheizt); „Bad" mit
        /// <c>BaseQuantities</c> (<c>NetFloorArea</c> 10 m², <c>NetVolume</c> 24 m³, <c>Height</c> 2,4 m) UND
        /// einem fremden Satz mit <c>Area</c> 999 m² — der Standard geht vor. Dazu eine Außenwand Süd im EG
        /// (U 0,3, brutto = netto 30 m²). Beheizt: 100 m², 246 m³.
        ///
        /// <para><b>Die Bauteile des CAD-Exports</b> (Mehrzonenkonzept 6.5, <see cref="Bau.CadBauteil{T}"/>): ohne
        /// <c>IsExternal</c> und ohne Raumgrenze, Platzierung im Ursprung des Geschosses; Angrenzung in
        /// <c>CAD_BauteilAllgemein.AdjacentType</c> (Aufzählung), Himmelsrichtung als Text
        /// <c>Orientation (°)</c>, Hüllkennung <c>ElementEnergyConsultingProperties.CladdingSurface</c>, der
        /// U-Wert als <c>UValue (W/(m² K))</c> in <c>CAD_Bauteilreferenzen</c> und mit falscher Einheit als
        /// <c>ThermalTransmittance (W/(m K))</c> in <c>Pset_*Common</c> (ein anderer Wert, der nie gelten darf),
        /// die Flächen als <c>GrossArea</c>/<c>NetArea</c> in <c>CAD_BauteilQuantities</c> neben Lockwerten
        /// <c>Width</c> (= Länge) und <c>Length</c> (= Höhe). EG: „Nord CAD" außen, 0°, U 0,25 (falsch 0,5),
        /// brutto 30, netto 24 m², mit zwei Fenstern über Öffnungen — „F1 CAD" 2 × 2 m über
        /// <c>OverallWidth</c>/<c>OverallHeight</c>, „F2 CAD" nur <c>GrossArea</c> 2 m² neben Width = Length =
        /// 5 m, beide U 1,2 (falsch 9,9) —; „Kellerdecke CAD" <c>btaCellarCeiling</c>, U 0,4, 60 m²;
        /// „Kellerwand CAD" <c>btaGround</c>, 90°, U 0,5, 10 m²; „Innenwand CAD" <c>btaHeated</c>, U 1,5, 12 m²;
        /// „Wand unbeheizt CAD" <c>btaUnHeated</c>, U 0,6, 6 m²; „Hilfswand CAD" <c>btaNone</c> ohne Mengen
        /// und U-Wert. OG: „Dach CAD" außen, U 0,2 (falsch 0,9), brutto 35 m², mit dem Dachfenster
        /// „DF CAD" (1 × 1 m, U 1,4) über <c>IfcRelAggregates</c>; „Oberste Decke CAD"
        /// <c>btaUppermostStorey</c>, U 0,3, 20 m²; „Spitzbodenwand CAD" außen, 270°, U 0,3, 8 m², aber
        /// Hüllkennung FALSE.</para>
        ///
        /// <para><b>Das Baujahr des CAD-Exports</b> (Baujahr-Rückfall, Mehrzonenkonzept 6.5): kein
        /// <c>Pset_BuildingCommon</c>; im Satz <c>CAD_GebaeudeAllgemein</c> erst <c>Constructed</c> = 1983
        /// (Ganzzahl), dann <c>YearOfConstruction (Datum)</c> = „01.01.1984 00:00:00" — der Standardname geht
        /// in der Rangfolge vor, das Baujahr ist 1984 (Klasse H), nicht 1983 (G).</para>
        /// </summary>
        public static byte[] Enthaltensein()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc2X3, "ifc2x3_enthaltensein.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Probengebäude", null);
                b.Eigenschaften(g, "CAD_GebaeudeAllgemein", ("Constructed", new IfcInteger(1983)),
                                ("YearOfConstruction (Datum)", new IfcLabel("01.01.1984 00:00:00")));
                IIfcBuildingStorey eg = b.GeschossEnthalten(g, "EG", 0);
                IIfcBuildingStorey og = b.GeschossEnthalten(g, "OG", 2800);
                b.RaumEnthalten(eg, "Wohnen", 150, 150, 40, 100, 2500, zerlegt: false);
                b.RaumEnthalten(eg, "Küche", 6150, 150, 20, 50, 2500, zerlegt: true);
                b.RaumEnthalten(og, "Schlafen", 150, 150, 30, 72, 2400, zerlegt: false);
                b.RaumEnthalten(og, "Abstellraum", 6150, 150, 5, 12, 2400, zerlegt: false);
                IIfcSpace bad = b.RaumEnthalten(og, "Bad", 6150, 4150, null, null, null, zerlegt: false);
                b.RaumMengen(bad, "BaseQuantities", 10, 24, 2400, standard: true);
                b.RaumMengen(bad, "CAD_RaumQuantities", 999, 999, 9999, standard: false);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ E", 0.3);
                b.Wand(eg, "EG Süd", Wandlage.Sued, typ, null, "BaseQuantities", 30.0, 30.0, null, new IIfcSpace[0]);

                // Die Bauteile des CAD-Exports (Klassenkopf).
                IIfcWall nord = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Nord CAD", "btaOutside", true, 0.25, 0.5, 0, 30, 24, 10000, 3000);
                IIfcWindow f1 = b.CadFenster(nord, eg, "F1 CAD", (2000, 2000), null, 1.2, 9.9);
                b.Rahmenanteil(f1, "30");   // Prozent, wie der CAD-Export
                b.CadFenster(nord, eg, "F2 CAD", null, 2.0, 1.2, 9.9);
                b.CadBauteil<IIfcSlab>(eg, "IfcSlab", "Kellerdecke CAD", "btaCellarCeiling", true, 0.4, null, null, 60, 60, 7500, 8000);
                b.CadBauteil<IIfcWall>(eg, "IfcWall", "Kellerwand CAD", "btaGround", true, 0.5, null, 90, 10, 10, 4000, 2500);
                b.CadBauteil<IIfcWall>(eg, "IfcWall", "Innenwand CAD", "btaHeated", false, 1.5, null, null, 12, 12, 4800, 2500);
                b.CadBauteil<IIfcWall>(eg, "IfcWall", "Wand unbeheizt CAD", "btaUnHeated", true, 0.6, null, null, 6, 6, 2400, 2500);
                b.CadBauteil<IIfcWall>(eg, "IfcWall", "Hilfswand CAD", "btaNone", false, null, null, null, null, null, null, null);
                IIfcRoof dach = b.CadBauteil<IIfcRoof>(og, "IfcRoof", "Dach CAD", "btaOutside", true, 0.2, 0.9, null, 35, 34, 7000, 5000);
                IIfcWindow df = b.CadFenster(dach, og, "DF CAD", (1000, 1000), null, 1.4, null, teil: true);
                b.Rahmenanteil(df, "0,2");  // Anteil
                b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Oberste Decke CAD", "btaUppermostStorey", true, 0.3, null, null, 20, 20, 5000, 4000);
                b.CadBauteil<IIfcWall>(og, "IfcWall", "Spitzbodenwand CAD", "btaOutside", false, 0.3, null, 270, 8, 8, 3200, 2500);
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Trenndecken über die Raumbezüge</b> (IFC2X3, Mehrzonenkonzept 6.5) — ein CAD-Export ohne Raumgrenzen, der
        /// je Raum ein <c>IfcRelReferencedInSpatialStructure</c> mit den angrenzenden Bauteilen schreibt (HottCAD-Muster).
        /// Das Gebäude heißt „Gebäude" (Platzhalter) und führt kein Baujahr. Geschosse KG (−2600 mm), EG (0), OG (2800),
        /// Räume über das Enthaltensein mit Mengen in <c>CAD_RaumQuantities</c>: KG „Keller" 50 m² (nach dem Namen
        /// unbeheizt); EG „Wohnen" 40 m², „Küche" 20 m², „Abstellraum" 5 m² (unbeheizt); OG „Schlafen" 35 m²,
        /// „Kind" 25 m² — beheizt je Geschoss 60 m², zusammen 120 m². Höhe 2,5 m.
        ///
        /// <para><b>Die Bauteile</b> (<see cref="Bau.CadBauteil{T}"/>, ohne <c>IsExternal</c> und ohne Raumgrenze) und wer
        /// sie referenziert: „Decke EG/OG" <c>btaHeated</c>, U 1,0, 60 m² (bzw. 10 m² mit <paramref name="schwach"/>) —
        /// Wohnen, Küche, Schlafen, Kind: die Trenndecke EG/OG; „Kellerdecke" <c>btaCellarCeiling</c>, U 0,4, 65 m² —
        /// Keller, Wohnen, Küche, Abstellraum: die Trenndecke KG/EG; „Oberste Decke" <c>btaUppermostStorey</c>, U 0,3,
        /// 60 m² — nur Schlafen und Kind (ein Geschoss, keine Trenndecke); „Vordach" außen, U 0,5, 6 m² — Wohnen und
        /// Schlafen (außen, nie Trenndecke); „Innenwand EG" <c>btaHeated</c>, 12 m² — Wohnen, Küche (innere Masse);
        /// „Wand Abstellraum" <c>btaHeated</c>, 6 m² — Küche, Abstellraum (gegen unbeheizt); „Innenwand OG"
        /// <c>btaHeated</c>, 10 m² — Schlafen, Kind; „Treppenhauswand" <c>btaHeated</c>, 9 m² — Wohnen und Schlafen
        /// (zwei Geschosse: kein Nachbar, einseitig); „Innenwand einzeln" <c>btaHeated</c>, 4 m² — nur Wohnen
        /// (einseitig); „Innenwand frei" <c>btaHeated</c>, 8 m², ohne Bezug (einseitig); „Außenwand Süd" (EG, 180°) und
        /// „Außenwand Nord" (OG, 0°) außen, U 0,3, je 40 m² — Wohnen bzw. Schlafen; „Kellerwand" <c>btaGround</c>, 30 m²,
        /// Hüllkennung FALSE — Keller.</para>
        ///
        /// <para>Zwei Abwandlungen, nur im Speicher (keine Probendatei): <paramref name="schwach"/> — die Decke EG/OG trägt
        /// nur 10 m² (die Datei referenziert nicht alle Deckenteile; die Fläche wird aus den Raummengen geschätzt);
        /// <paramref name="spitzboden"/> — ein Geschoss DG (5200 mm) mit dem Raum „Spitzboden" (20 m², nach dem Namen
        /// beheizt), der die „Oberste Decke" mitreferenziert: Die Datei erklärt sie gegen unbeheizt, die Erklärung gilt vor
        /// dem Bezug und wird benannt.</para>
        ///
        /// <para>Drei weitere Abwandlungen nach den Anwenderdateien eines CAD-Exports (Mehrzonenkonzept 6.5):
        /// <paramref name="beheizungsart"/> — die Beheizungsart <c>HeatingType</c> im Satz <c>CAD_RaumAllgemein</c>: Keller
        /// <c>bhtUnHeated</c>, Wohnen und Abstellraum <c>bhtHeated</c>, Kind <c>bhtSeparatelyHeated</c>;
        /// <paramref name="kellerTeil"/> — ein „Hobbyraum" im KG (15 m², <c>bhtHeated</c>), der nur die Kellerwand
        /// referenziert: Das KG hat beheizte Räume, die Kellerdecke grenzt aber nur an den unbeheizten Keller;
        /// <paramref name="bodenoeffnung"/> — eine „Bodenöffnung" (Platte, <c>btaHeated</c>, U 0, ohne Aufbau, 8 m²), die
        /// Wohnen und Schlafen referenzieren; <paramref name="speicher"/> — ein „Speicher" im OG (6 m², <c>bhtUnHeated</c>)
        /// ohne jeden Bezug: unter Z4 eine unbeheizte Zone ohne Fläche.</para>
        ///
        /// <para><paramref name="kindTemperatur"/> (nur mit <paramref name="beheizungsart"/>) — der getrennt beheizte Raum
        /// „Kind" führt im selben Satz die Raumtemperatur <c>InsideTemperature (°C)</c>.</para>
        /// </summary>
        public static byte[] Referenzen(bool schwach = false, bool spitzboden = false, bool beheizungsart = false,
                                        bool kellerTeil = false, bool bodenoeffnung = false, bool speicher = false,
                                        double? kindTemperatur = null)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc2X3, "ifc2x3_referenzen.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Gebäude", null);
                IIfcBuildingStorey kg = b.GeschossEnthalten(g, "KG", -2600);
                IIfcBuildingStorey eg = b.GeschossEnthalten(g, "EG", 0);
                IIfcBuildingStorey og = b.GeschossEnthalten(g, "OG", 2800);
                IIfcSpace keller = b.RaumEnthalten(kg, "Keller", 150, 150, 50, 125, 2500, zerlegt: false);
                IIfcSpace wohnen = b.RaumEnthalten(eg, "Wohnen", 150, 150, 40, 100, 2500, zerlegt: false);
                IIfcSpace kueche = b.RaumEnthalten(eg, "Küche", 6150, 150, 20, 50, 2500, zerlegt: false);
                IIfcSpace abstell = b.RaumEnthalten(eg, "Abstellraum", 6150, 4150, 5, 12.5, 2500, zerlegt: false);
                IIfcSpace schlafen = b.RaumEnthalten(og, "Schlafen", 150, 150, 35, 87.5, 2500, zerlegt: false);
                IIfcSpace kind = b.RaumEnthalten(og, "Kind", 6150, 150, 25, 62.5, 2500, zerlegt: false);

                IIfcSlab decke = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Decke EG/OG", "btaHeated", false, 1.0, null, null,
                                                        schwach ? 10 : 60, schwach ? 10 : 60, null, null);
                IIfcSlab kellerdecke = b.CadBauteil<IIfcSlab>(eg, "IfcSlab", "Kellerdecke", "btaCellarCeiling", true, 0.4, null, null, 65, 65, null, null);
                IIfcSlab oberste = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Oberste Decke", "btaUppermostStorey", true, 0.3, null, null, 60, 60, null, null);
                IIfcSlab vordach = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Vordach", "btaOutside", true, 0.5, null, null, 6, 6, null, null);
                IIfcWall iwEg = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Innenwand EG", "btaHeated", false, 1.5, null, null, 12, 12, null, null);
                IIfcWall abstellwand = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Wand Abstellraum", "btaHeated", false, 1.5, null, null, 6, 6, null, null);
                IIfcWall iwOg = b.CadBauteil<IIfcWall>(og, "IfcWall", "Innenwand OG", "btaHeated", false, 1.5, null, null, 10, 10, null, null);
                IIfcWall treppe = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Treppenhauswand", "btaHeated", false, 1.5, null, null, 9, 9, null, null);
                IIfcWall einzeln = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Innenwand einzeln", "btaHeated", false, 1.5, null, null, 4, 4, null, null);
                b.CadBauteil<IIfcWall>(eg, "IfcWall", "Innenwand frei", "btaHeated", false, 1.5, null, null, 8, 8, null, null);
                IIfcWall sued = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Außenwand Süd", "btaOutside", true, 0.3, null, 180, 40, 40, null, null);
                IIfcWall nord = b.CadBauteil<IIfcWall>(og, "IfcWall", "Außenwand Nord", "btaOutside", true, 0.3, null, 0, 40, 40, null, null);
                IIfcWall kellerwand = b.CadBauteil<IIfcWall>(kg, "IfcWall", "Kellerwand", "btaGround", false, 0.5, null, 90, 30, 30, null, null);

                b.Bezug(keller, kellerdecke, kellerwand);
                b.Bezug(wohnen, decke, kellerdecke, vordach, iwEg, treppe, einzeln, sued);
                b.Bezug(kueche, decke, kellerdecke, iwEg, abstellwand);
                b.Bezug(abstell, kellerdecke, abstellwand);
                b.Bezug(schlafen, decke, oberste, vordach, iwOg, treppe, nord);
                b.Bezug(kind, decke, oberste, iwOg);
                if (spitzboden)
                {
                    IIfcBuildingStorey dg = b.GeschossEnthalten(g, "DG", 5200);
                    IIfcSpace sb = b.RaumEnthalten(dg, "Spitzboden", 150, 150, 20, 30, 1500, zerlegt: false);
                    b.Bezug(sb, oberste);
                }
                if (beheizungsart)
                {
                    b.Beheizungsart(keller, "bhtUnHeated");
                    b.Beheizungsart(wohnen, "bhtHeated");
                    b.Beheizungsart(abstell, "bhtHeated");
                    b.Beheizungsart(kind, "bhtSeparatelyHeated", kindTemperatur);
                }
                if (kellerTeil)
                {
                    IIfcSpace hobby = b.RaumEnthalten(kg, "Hobbyraum", 6150, 150, 15, 37.5, 2500, zerlegt: false);
                    b.Beheizungsart(hobby, "bhtHeated");
                    b.Bezug(hobby, kellerwand);
                }
                if (speicher)
                {
                    IIfcSpace sp = b.RaumEnthalten(og, "Speicher", 6150, 4150, 6, 15, 2500, zerlegt: false);
                    b.Beheizungsart(sp, "bhtUnHeated");
                }
                if (bodenoeffnung)
                {
                    IIfcSlab loch = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Bodenöffnung", "btaHeated", false, 0.0, null, null, 8, 8, null, null);
                    b.Bezug(wohnen, loch);
                    b.Bezug(schlafen, loch);
                }
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Zonenregel Z6 mit Sollwerten des Standards</b> (Mehrzonenkonzept 6.1): IFC4, zwei Geschosse, Räume über die
        /// Zerlegung, keine Raumgrenzen, keine Bauteile; Heizsollwert je Raum als <c>Pset_SpaceThermalRequirements.SpaceTemperature</c>.
        /// EG: „Büro 1" 30 m² 20 °C, „Büro 2" 25 m² 20,4 °C, „Flur" 15 m² 15 °C, „WC" 2,5 m² 24 °C (unter der Mindestgröße),
        /// „Lager 1" 20 m² 15 °C; OG: „Besprechung" 20 m² 19,6 °C, „Lager 2" 10 m² und „Archiv" 12 m² ohne Sollwert.
        /// </summary>
        public static byte[] Z6Sollwerte()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_z6_sollwerte.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Zonenprobe", null);
                IIfcBuildingStorey eg = b.Geschoss(g, "EG", 0);
                IIfcBuildingStorey og = b.Geschoss(g, "OG", 3000);
                (IIfcBuildingStorey S, string Nr, string Name, double M2, double? C)[] raeume =
                {
                    (eg, "1", "Büro 1", 30, 20.0), (eg, "2", "Büro 2", 25, 20.4), (eg, "3", "Flur", 15, 15.0),
                    (eg, "4", "WC", 2.5, 24.0), (eg, "5", "Lager 1", 20, 15.0),
                    (og, "6", "Besprechung", 20, 19.6), (og, "7", "Lager 2", 10, null), (og, "8", "Archiv", 12, null),
                };
                double x = 0.0;
                foreach (var r in raeume)
                {
                    IIfcSpace raum = b.Raum(r.S, r.Nr, r.Name, x, 0, r.M2, 2500, r.M2 * 2.5, beheizt: false);
                    if (r.C.HasValue) b.Solltemperatur(raum, r.C.Value);
                    x += 1000;
                }
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Zonenregel Z6 nach dem Muster eines CAD-Exports</b> (Mehrzonenkonzept 6.1, 6.5): IFC4, Räume über das
        /// Enthaltensein, keine Raumgrenzen; je Raum ein Satz <c>CAD_RaumAllgemein</c> mit <c>HeatingType</c>,
        /// <c>InsideTemperature (°C)</c> und <c>RoomType</c> (Aufzählung mit Präfix <c>mrt</c>), Bauteile mit Raumbezügen.
        /// EG: „Büroraum" 40 m² beheizt 20 °C Office, „Büroraum 2" 30 m² beheizt 20 °C Office, „Flur" 20 m² beheizt 15 °C Hall,
        /// „WC-Raum" 6 m² beheizt 20 °C WC, „Lagerraum" 25 m² unbeheizt ohne Temperatur Store; OG: „Wohnraum" 35 m² getrennt
        /// beheizt 20 °C Living, „Raum 7" 12 m² beheizt ohne Temperatur Office, „Abstellraum" 10 m² unbeheizt 10 °C Store; dazu im EG
        /// „Dusche" 3 m² beheizt 24 °C Shower unter der Mindestgröße, deren einzige Grenzfläche („Wand Dusche") am Flur (15 °C) liegt.
        /// </summary>
        public static byte[] Z6Cad()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_z6_cad.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Zonenprobe", null);
                IIfcBuildingStorey eg = b.GeschossEnthalten(g, "EG", 0);
                IIfcBuildingStorey og = b.GeschossEnthalten(g, "OG", 2800);
                IIfcSlab decke = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Decke EG/OG", "btaHeated", false, 1.0, null, null, 140, 140, null, null);
                IIfcSlab boden = b.CadBauteil<IIfcSlab>(eg, "IfcSlab", "Bodenplatte", "btaGround", true, 0.4, null, null, 121, 121, null, null);
                IIfcSlab dach = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Oberste Decke", "btaUppermostStorey", true, 0.3, null, null, 57, 57, null, null);
                IIfcWall sued = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Außenwand Süd", "btaOutside", true, 0.3, null, 180, 40, 40, null, null);
                IIfcWall nord = b.CadBauteil<IIfcWall>(og, "IfcWall", "Außenwand Nord", "btaOutside", true, 0.3, null, 0, 40, 40, null, null);
                IIfcWall duschwand = b.CadBauteil<IIfcWall>(eg, "IfcWall", "Wand Dusche", "btaHeated", false, 1.5, null, null, 5, 5, null, null);
                // Die zwei Seiten (HSETU_Bauteilreferenzen, Konzept HottCAD-Verbund 3.1); die Wand Dusche ohne — der Rückfall.
                const string OHNE = "botNone (-987654321,99)";
                b.Seiten(decke, ("btaHeated", OHNE), ("btaHeated", OHNE));
                b.Seiten(boden, ("btaGround", OHNE));
                b.Seiten(dach, ("btaHeated", OHNE), ("btaUppermostStorey", OHNE));
                b.Seiten(sued, ("btaOutside", "botS (180)"), ("btaHeated", "botN (-987654321,99)"));
                b.Seiten(nord, ("btaOutside", "botN (0)"), ("btaNone", OHNE));
                (IIfcBuildingStorey S, string Name, double M2, string Art, double? C, string Typ, IIfcProduct[] Bauteile)[] raeume =
                {
                    (eg, "Büroraum", 40, "bhtHeated", 20.0, "mrtOffice", new IIfcProduct[] { boden, decke, sued }),
                    (eg, "Büroraum 2", 30, "bhtHeated", 20.0, "mrtOffice", new IIfcProduct[] { boden, decke, sued }),
                    (eg, "Flur", 20, "bhtHeated", 15.0, "mrtHall", new IIfcProduct[] { boden, decke, duschwand }),
                    (eg, "WC-Raum", 6, "bhtHeated", 20.0, "mrtWC", new IIfcProduct[] { boden, decke }),
                    (eg, "Lagerraum", 25, "bhtUnHeated", null, "mrtStore", new IIfcProduct[] { boden, decke, sued }),
                    (og, "Wohnraum", 35, "bhtSeparatelyHeated", 20.0, "mrtLiving", new IIfcProduct[] { decke, dach, nord }),
                    (og, "Raum 7", 12, "bhtHeated", null, "mrtOffice", new IIfcProduct[] { decke, dach }),
                    (og, "Abstellraum", 10, "bhtUnHeated", 10.0, "mrtStore", new IIfcProduct[] { decke, dach, nord }),
                    (eg, "Dusche", 3, "bhtHeated", 24.0, "mrtShower", new IIfcProduct[] { boden, duschwand }),
                };
                double x = 150;
                foreach (var r in raeume)
                {
                    IIfcSpace raum = b.RaumEnthalten(r.S, r.Name, x, 150, r.M2, r.M2 * 2.5, 2500, zerlegt: false);
                    b.Raumangaben(raum, r.Art, r.C, r.Typ);
                    b.Bezug(raum, r.Bauteile);
                    x += 1000;
                }
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Zwei Räume übereinander ohne Raumgrenzen und ohne Raumbezug</b> (Mehrzonenkonzept 6.5, Trenndecke ohne
        /// Raumgrenzen): EG „Wohnen“ 6 × 5 m im Ursprung, OG „Schlafen“ 6 × 5 m um 2 m nach Osten versetzt — die
        /// Grundrisse überdecken sich auf 4 × 5 = 20 m². Mit <paramref name="grundriss"/> tragen beide Räume eine
        /// Körperdarstellung (Extrusion eines Polylinienprofils; das OG-Profil als L mit Ausschnitt außerhalb der
        /// Überdeckung), mit <paramref name="decke"/> steht im OG eine Platte innen (U 1,0, 40 m²) ohne Bezug.
        /// </summary>
        public static byte[] Uebereinander(bool grundriss, bool decke)
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_uebereinander.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Gebäude", null);
                IIfcBuildingStorey eg = b.GeschossEnthalten(g, "EG", 0);
                IIfcBuildingStorey og = b.GeschossEnthalten(g, "OG", 2800);
                IIfcSpace wohnen = b.RaumEnthalten(eg, "Wohnen", 0, 0, 30, 75, 2500, zerlegt: false);
                IIfcSpace schlafen = b.RaumEnthalten(og, "Schlafen", 2000, 0, 29, 72.5, 2500, zerlegt: false);
                if (grundriss)
                {
                    b.Grundriss(wohnen, (0, 0), (6000, 0), (6000, 5000), (0, 5000));
                    // L-förmig: der Ausschnitt 1 × 1 m liegt außerhalb des EG (x 6…7 m).
                    b.Grundriss(schlafen, (0, 0), (6000, 0), (6000, 4000), (5000, 4000), (5000, 5000), (0, 5000));
                }
                b.CadBauteil<IIfcWall>(eg, "IfcWall", "Außenwand", "btaOutside", true, 0.3, null, 180, 60, 60, null, null);
                b.CadBauteil<IIfcWall>(og, "IfcWall", "Außenwand OG", "btaOutside", true, 0.3, null, 0, 60, 60, null, null);
                b.CadBauteil<IIfcSlab>(eg, "IfcSlab", "Bodenplatte", "btaGround", true, 0.4, null, null, 30, 30, null, null);
                b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Oberste Decke", "btaOutside", true, 0.2, null, null, 30, 30, null, null);
                if (decke)
                    b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Decke EG/OG", "btaHeated", false, 1.0, null, null, 40, 40, null, null);
                return b.Speichern();
            }
        }

        /// <summary>
        /// <b>Die Befunde der Importproben 13–18</b> (Mehrzonenkonzept 6.1, 6.2, 6.5) in einer Datei: EG „Wohnen“ 30 m² mit
        /// Flächenmenge und einem Grundriss von 36 m², „Arbeitsraum“ ohne Flächenmenge mit Grundriss 4 × 5 m, „Büro“ mit
        /// <c>PredefinedType = INTERNAL</c> und <c>IsExternal = TRUE</c>, „Terrasse“ nur mit <c>IsExternal = TRUE</c>; OG
        /// „Schlafen“ 30 m². Ohne <c>IsExternal</c> an den Bauteilen: die Geschossdecke (FLOOR) mit zwei inneren Grenzen je
        /// 30 m² und einem äußeren Randstreifen 6 × 0,1 m, die Kragplatte (FLOOR) mit zwei inneren Grenzen je 30 m² und einer
        /// äußeren von 10 m², die Bodenplatte (BASESLAB) mit einer Grenze <c>EXTERNAL</c>; ein Dach ohne Flächenmenge aus einer
        /// Platte (ROOF), dessen Grenze (30 m²) am Dach hängt; eine Stütze mit einer Grenze von 0,6 m².
        /// </summary>
        public static byte[] Importbefunde()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_importbefunde.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Befundhaus", "2010");
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcBuildingStorey og = b.Geschoss(g, "Obergeschoss", 2800);
                var ext = IfcInternalOrExternalEnum.EXTERNAL;
                var innen = IfcInternalOrExternalEnum.INTERNAL;
                double[] hoch = { 0, 0, 1 }, runter = { 0, 0, -1 }, sued = { 0, -1, 0 };

                IIfcSpace wohnen = b.Raum(eg, "0.01", "Wohnen", 0, 0, 30, 2500, 75, beheizt: true);
                b.Grundriss(wohnen, (0, 0), (6000, 0), (6000, 6000), (0, 6000));
                IIfcSpace arbeit = b.RaumEnthalten(eg, "Arbeitsraum", 7000, 0, null, null, null, zerlegt: true);
                b.Grundriss(arbeit, (0, 0), (4000, 0), (4000, 5000), (0, 5000));
                IIfcSpace buero = b.RaumEnthalten(eg, "Büro", 12000, 0, 20, 50, 2500, zerlegt: true);
                buero.PredefinedType = IfcSpaceTypeEnum.INTERNAL;
                b.Eigenschaft(buero, "Pset_SpaceCommon", "IsExternal", new IfcBoolean(true));
                IIfcSpace terrasse = b.RaumEnthalten(eg, "Terrasse", 17000, 0, 12, 30, 2500, zerlegt: true);
                b.Eigenschaft(terrasse, "Pset_SpaceCommon", "IsExternal", new IfcBoolean(true));
                IIfcSpace schlafen = b.Raum(og, "1.01", "Schlafen", 0, 0, 30, 2500, 75, beheizt: true);

                // Die Geschossdecke: innen, mit einem äußeren Randstreifen von 0,6 m² (1 % der Grenzfläche).
                IIfcSlab decke = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Geschossdecke", "btaHeated", false, 0.5, null, null, 30, 30, null, null);
                decke.PredefinedType = IfcSlabTypeEnum.FLOOR;
                b.Grenze2(wohnen, decke, innen, hoch, new double[] { 0, 0, 2500 }, new double[] { 6000, 0, 2500 }, new double[] { 6000, 5000, 2500 }, new double[] { 0, 5000, 2500 });
                b.Grenze2(schlafen, decke, innen, runter, new double[] { 0, 0, 0 }, new double[] { 0, 5000, 0 }, new double[] { 6000, 5000, 0 }, new double[] { 6000, 0, 0 });
                b.Grenze2(schlafen, decke, ext, runter, new double[] { 0, 5000, 0 }, new double[] { 0, 5100, 0 }, new double[] { 6000, 5100, 0 }, new double[] { 6000, 5000, 0 });

                // Die Kragplatte: zwei innere Grenzen und eine äußere von 10 m² (14 %) — bleibt außen.
                IIfcSlab krag = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Kragplatte", "btaHeated", false, 0.5, null, null, 40, 40, null, null);
                krag.PredefinedType = IfcSlabTypeEnum.FLOOR;
                b.Grenze2(wohnen, krag, innen, hoch, new double[] { 0, 0, 2500 }, new double[] { 6000, 0, 2500 }, new double[] { 6000, 5000, 2500 }, new double[] { 0, 5000, 2500 });
                b.Grenze2(schlafen, krag, innen, runter, new double[] { 0, 0, 0 }, new double[] { 0, 5000, 0 }, new double[] { 6000, 5000, 0 }, new double[] { 6000, 0, 0 });
                b.Grenze2(schlafen, krag, ext, runter, new double[] { 6000, 0, 0 }, new double[] { 6000, 5000, 0 }, new double[] { 8000, 5000, 0 }, new double[] { 8000, 0, 0 });

                // Die Bodenplatte mit einer Grenze EXTERNAL statt EXTERNAL_EARTH.
                IIfcSlab boden = b.CadBauteil<IIfcSlab>(eg, "IfcSlab", "Bodenplatte", "btaGround", true, 0.4, null, null, 30, 30, null, null);
                boden.PredefinedType = IfcSlabTypeEnum.BASESLAB;
                b.Grenze2(wohnen, boden, ext, runter, new double[] { 0, 0, 0 }, new double[] { 0, 5000, 0 }, new double[] { 6000, 5000, 0 }, new double[] { 6000, 0, 0 });

                // Das zerlegte Dach ohne Flächenmenge: die Grenze hängt am Dach, nicht an seiner einzigen Platte.
                IIfcRoof dach = b.CadBauteil<IIfcRoof>(og, "IfcRoof", "Dach", "btaOutside", true, 0.2, null, null, null, null, null, null);
                IIfcSlab dachplatte = b.CadBauteil<IIfcSlab>(og, "IfcSlab", "Dachplatte", "btaOutside", true, 0.2, null, null, null, null, null, null);
                dachplatte.PredefinedType = IfcSlabTypeEnum.ROOF;
                b.Teil(dach, dachplatte);
                b.Grenze2(schlafen, dach, ext, hoch, new double[] { 0, 0, 2500 }, new double[] { 6000, 0, 2500 }, new double[] { 6000, 5000, 2500 }, new double[] { 0, 5000, 2500 });

                // Eine Stütze mit Raumgrenze — kein Bauteil der Hülle.
                IIfcColumn stuetze = b.CadBauteil<IIfcColumn>(eg, "IfcColumn", "Stütze", "btaHeated", false, null, null, null, null, null, null, null);
                b.Grenze2(wohnen, stuetze, innen, sued, new double[] { 1000, 1000, 0 }, new double[] { 1300, 1000, 0 }, new double[] { 1300, 1000, 2000 }, new double[] { 1000, 1000, 2000 });
                return b.Speichern();
            }
        }

        /// <summary>Zwei Gebäude mit je einem beheizten Raum, einer Außenwand und einem Fenster (U13).</summary>
        public static byte[] ZweiGebaeude()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_zwei_gebaeude.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ B", 0.3);
                foreach ((string name, double flaeche, double fenster) in new[] { ("Gebäude A", 50.0, 5.0), ("Gebäude B", 80.0, 8.0) })
                {
                    IIfcBuilding g = b.Gebaeude(name, null);
                    IIfcBuildingStorey s = b.Geschoss(g, name + " EG", 0);
                    IIfcSpace r = b.Raum(s, name + " R1", "Büro", 150, 150, flaeche, 2500, flaeche * 2.5, beheizt: true);
                    IIfcWall w = b.Wand(s, name + " Süd", Wandlage.Sued, typ, null, "BaseQuantities", 30.0, 30.0 - fenster, null, new[] { r });
                    b.Fenster(w, s, name + " F1", flaeche: fenster);
                }
                return b.Speichern();
            }
        }

        /// <summary>Ein Gebäude mit Raum und Außenwänden, aber ohne jeden Mengensatz (3.5 Nr. 5).</summary>
        public static byte[] OhneMengen()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_ohne_mengen.ifc"))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Probengebäude", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Wohnen", 150, 150, null, null, null, beheizt: true);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ C", 0.3);
                foreach (Wandlage l in Wandlage.Alle)
                    b.Wand(s, "EG " + l.Name, l, typ, null, null, null, null, null, new[] { r });
                return b.Speichern();
            }
        }

        /// <summary>
        /// Eine Wand nach Modell-Nord in einem Kontext mit TrueNorth [−2, 1, 0] UND einer Koordinatenumrechnung
        /// mit der Drehung 90° (XAxisAbscissa 0, XAxisOrdinate 1): Es gilt die Umrechnung, TrueNorth wird nicht
        /// addiert — die Wand zeigt nach West (270°), nicht nach Ost (63,43°).
        /// </summary>
        public static byte[] MapConversion()
        {
            using (var b = new Bau(XbimSchemaVersion.Ifc4, "ifc4_mapconversion.ifc"))
            {
                b.Anfang(new[] { -2.0, 1.0, 0.0 }, karte: true);
                IIfcBuilding g = b.Gebaeude("Probengebäude", null);
                IIfcBuildingStorey s = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(s, "0.01", "Wohnen", 150, 150, 60, 2500, 150, beheizt: true);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ D", 0.3);
                IIfcWall w = b.Wand(s, "EG Nord", Wandlage.Nord, typ, null, "BaseQuantities", 28.0, 24.0, null, new[] { r });
                b.Fenster(w, s, "F-N", flaeche: 4.0);
                return b.Speichern();
            }
        }

        /// <summary>Ein <c>.ifczip</c>-Behälter mit genau einem Eintrag (Zeitstempel fest).</summary>
        public static byte[] Zip(string eintragsname, byte[] inhalt)
        {
            var ziel = new MemoryStream();
            using (var zip = new ZipArchive(ziel, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry e = zip.CreateEntry(eintragsname, CompressionLevel.Optimal);
                e.LastWriteTime = new DateTimeOffset(2026, 9, 25, 0, 0, 0, TimeSpan.Zero);
                using (Stream s = e.Open()) s.Write(inhalt, 0, inhalt.Length);
            }
            return ziel.ToArray();
        }

        // ==================================================================
        //  Wandlagen des Probenhauses (Achse in der lokalen x-Achse)
        // ==================================================================

        internal sealed class Wandlage
        {
            private Wandlage(string name, double x, double y, double rx, double ry, bool lang)
            {
                Name = name; X = x; Y = y; Rx = rx; Ry = ry; Lang = lang;
            }

            public string Name { get; }
            public double X { get; }
            public double Y { get; }
            public double Rx { get; }
            public double Ry { get; }
            public bool Lang { get; }

            // Umlauf gegen den Uhrzeigersinn um den Grundriss 10 m × 8 m: Die lokale y-Achse (z × x)
            // zeigt jeweils nach innen, die Außenseite liegt auf −y.
            public static readonly Wandlage Sued = new Wandlage("Süd", 0, 0, 1, 0, true);
            public static readonly Wandlage Ost = new Wandlage("Ost", 10000, 0, 0, 1, false);
            public static readonly Wandlage Nord = new Wandlage("Nord", 10000, 8000, -1, 0, true);
            public static readonly Wandlage West = new Wandlage("West", 0, 8000, 0, -1, false);
            public static readonly Wandlage[] Alle = { Sued, Ost, Nord, West };
        }

        // ==================================================================
        //  Der Bauhelfer — schemafrei über die IIfc*-Schnittstellen
        // ==================================================================

        private sealed partial class Bau : IDisposable
        {
            private readonly MemoryModel _m;
            private readonly ITransaction _t;
            private readonly string _dateiname;
            private readonly bool _ifc2x3;
            private int _guid;
            private IIfcProject _projekt;
            private IIfcSite _site;
            private IIfcGeometricRepresentationContext _kontext;
            private IIfcRelAggregates _projektZerlegung;
            private IIfcRelAggregates _siteZerlegung;

            public Bau(XbimSchemaVersion schema, string dateiname)
            {
                _m = new MemoryModel(MemoryModel.GetFactory(schema), NullLoggerFactory.Instance, 0);
                _t = _m.BeginTransaction("Probe");
                _dateiname = dateiname;
                _ifc2x3 = schema == XbimSchemaVersion.Ifc2X3;
            }

            public void Dispose()
            {
                _t.Dispose();
                _m.Dispose();
            }

            private T N<T>(string typ) where T : class, IPersistEntity
                => (T)_m.Instances.New(_m.Metadata.ExpressType(typ.ToUpperInvariant()).Type);

            private T Wurzel<T>(string typ, string name) where T : class, IIfcRoot
            {
                T e = N<T>(typ);
                _guid++;
                e.GlobalId = new IfcGloballyUniqueId(IfcGloballyUniqueId.ConvertToBase64(
                    new Guid("00000000-0000-4000-8000-" + _guid.ToString("D12", CultureInfo.InvariantCulture))));
                if (name != null) e.Name = new IfcLabel(name);
                return e;
            }

            private IIfcCartesianPoint Punkt(double x, double y, double z)
            {
                IIfcCartesianPoint p = N<IIfcCartesianPoint>("IfcCartesianPoint");
                p.Coordinates.Add(new IfcLengthMeasure(x));
                p.Coordinates.Add(new IfcLengthMeasure(y));
                p.Coordinates.Add(new IfcLengthMeasure(z));
                return p;
            }

            private IIfcDirection Richtung(double x, double y, double z)
            {
                IIfcDirection d = N<IIfcDirection>("IfcDirection");
                d.DirectionRatios.Add(new IfcReal(x));
                d.DirectionRatios.Add(new IfcReal(y));
                d.DirectionRatios.Add(new IfcReal(z));
                return d;
            }

            private IIfcLocalPlacement Platzierung(IIfcObjectPlacement relTo, double x, double y, double z, double rx = 1, double ry = 0)
            {
                IIfcAxis2Placement3D a = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                a.Location = Punkt(x, y, z);
                if (rx != 1 || ry != 0)
                {
                    a.Axis = Richtung(0, 0, 1);
                    a.RefDirection = Richtung(rx, ry, 0);
                }
                IIfcLocalPlacement p = N<IIfcLocalPlacement>("IfcLocalPlacement");
                p.PlacementRelTo = relTo;
                p.RelativePlacement = a;
                return p;
            }

            private IIfcSIUnit Einheit(IfcUnitEnum art, IfcSIUnitName name, IfcSIPrefix? prefix)
            {
                IIfcSIUnit u = N<IIfcSIUnit>("IfcSIUnit");
                u.UnitType = art;
                u.Name = name;
                u.Prefix = prefix;
                return u;
            }

            /// <summary>
            /// Projekt, Einheiten, Modellkontext (mit Nordrichtung und ggf. Koordinatenumrechnung), Grundstück.
            /// Längen in Millimetern, mit <paramref name="meter"/> in Metern.
            /// </summary>
            public void Anfang(double[] nord, bool karte, bool meter = false)
            {
                _projekt = Wurzel<IIfcProject>("IfcProject", "Importprobe");
                IIfcUnitAssignment e = N<IIfcUnitAssignment>("IfcUnitAssignment");
                e.Units.Add(Einheit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, meter ? (IfcSIPrefix?)null : IfcSIPrefix.MILLI));
                e.Units.Add(Einheit(IfcUnitEnum.AREAUNIT, IfcSIUnitName.SQUARE_METRE, null));
                e.Units.Add(Einheit(IfcUnitEnum.VOLUMEUNIT, IfcSIUnitName.CUBIC_METRE, null));
                e.Units.Add(Einheit(IfcUnitEnum.PLANEANGLEUNIT, IfcSIUnitName.RADIAN, null));
                e.Units.Add(Einheit(IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT, IfcSIUnitName.DEGREE_CELSIUS, null));
                _projekt.UnitsInContext = e;

                _kontext = N<IIfcGeometricRepresentationContext>("IfcGeometricRepresentationContext");
                _kontext.ContextType = new IfcLabel("Model");
                _kontext.CoordinateSpaceDimension = new Xbim.Ifc4.GeometryResource.IfcDimensionCount(3);
                _kontext.Precision = new IfcReal(1e-5);
                IIfcAxis2Placement3D wcs = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                wcs.Location = Punkt(0, 0, 0);
                _kontext.WorldCoordinateSystem = wcs;
                if (nord != null) _kontext.TrueNorth = Richtung(nord[0], nord[1], nord[2]);
                _projekt.RepresentationContexts.Add(_kontext);

                if (karte && !_ifc2x3)
                {
                    IIfcProjectedCRS crs = N<IIfcProjectedCRS>("IfcProjectedCRS");
                    crs.Name = new IfcLabel("EPSG:25832");
                    IIfcMapConversion mc = N<IIfcMapConversion>("IfcMapConversion");
                    mc.SourceCRS = _kontext;
                    mc.TargetCRS = crs;
                    mc.Eastings = new IfcLengthMeasure(0);
                    mc.Northings = new IfcLengthMeasure(0);
                    mc.OrthogonalHeight = new IfcLengthMeasure(0);
                    mc.XAxisAbscissa = new IfcReal(0);
                    mc.XAxisOrdinate = new IfcReal(1);
                    mc.Scale = new IfcReal(1);
                }

                _site = Wurzel<IIfcSite>("IfcSite", "Grundstück");
                _site.CompositionType = IfcElementCompositionEnum.ELEMENT;
                _site.ObjectPlacement = Platzierung(null, 0, 0, 0);
                _projektZerlegung = Wurzel<IIfcRelAggregates>("IfcRelAggregates", null);
                _projektZerlegung.RelatingObject = _projekt;
                _projektZerlegung.RelatedObjects.Add(_site);
            }

            public IIfcBuilding Gebaeude(string name, string baujahr)
            {
                IIfcBuilding g = Wurzel<IIfcBuilding>("IfcBuilding", name);
                g.CompositionType = IfcElementCompositionEnum.ELEMENT;
                g.ObjectPlacement = Platzierung(_site.ObjectPlacement, 0, 0, 0);
                if (_siteZerlegung == null)
                {
                    _siteZerlegung = Wurzel<IIfcRelAggregates>("IfcRelAggregates", null);
                    _siteZerlegung.RelatingObject = _site;
                }
                _siteZerlegung.RelatedObjects.Add(g);
                if (baujahr != null) Satz(g, "Pset_BuildingCommon", ("YearOfConstruction", new IfcLabel(baujahr)));
                return g;
            }

            private readonly Dictionary<int, IIfcRelAggregates> _zerlegung = new Dictionary<int, IIfcRelAggregates>();
            private readonly Dictionary<int, IIfcRelContainedInSpatialStructure> _enthalten = new Dictionary<int, IIfcRelContainedInSpatialStructure>();

            private void Zerlegen(IIfcObjectDefinition ganzes, IIfcObjectDefinition teil)
            {
                if (!_zerlegung.TryGetValue(ganzes.EntityLabel, out IIfcRelAggregates r))
                {
                    r = Wurzel<IIfcRelAggregates>("IfcRelAggregates", null);
                    r.RelatingObject = ganzes;
                    _zerlegung[ganzes.EntityLabel] = r;
                }
                r.RelatedObjects.Add(teil);
            }

            /// <summary>
            /// Die Raumbezüge eines Raums nach dem Muster eines CAD-Exports ohne Raumgrenzen: ein
            /// <c>IfcRelReferencedInSpatialStructure</c> je Raum mit den angrenzenden Bauteilen.
            /// </summary>
            /// <summary>Der Rahmenanteil eines Fensters nach dem Muster des CAD-Exports (<c>FractionOfFrame</c> als Text).</summary>
            public void Rahmenanteil(IIfcElement e, string wert)
                => SatzMit(e, "CAD_EcoCad", new List<IIfcProperty> { Einzel("FractionOfFrame", new IfcLabel(wert)) });

            /// <summary>Die Seiten eines Bauteils nach dem Muster des CAD-Exports: je Seite Angrenzung (Aufzählung) und Orientierung (Text).</summary>
            public void Seiten(IIfcElement e, params (string Angrenzung, string Orientierung)[] seiten)
            {
                var liste = new List<IIfcProperty>();
                for (int i = 0; i < seiten.Length; i++)
                {
                    liste.Add(Aufzaehlung("ElementReferences[" + i + "].AdjacentType", seiten[i].Angrenzung));
                    liste.Add(Einzel("ElementReferences[" + i + "].Orientation (°)", new IfcLabel(seiten[i].Orientierung)));
                }
                SatzMit(e, "CAD_Bauteilreferenzen", liste);
            }

            public void Bezug(IIfcSpace raum, params IIfcProduct[] bauteile)
            {
                IIfcRelReferencedInSpatialStructure r = Wurzel<IIfcRelReferencedInSpatialStructure>("IfcRelReferencedInSpatialStructure",
                    "Spatial references of space " + raum.Name);
                r.RelatingStructure = raum;
                foreach (IIfcProduct p in bauteile) r.RelatedElements.Add(p);
            }

            private void Enthalten(IIfcSpatialElement ort, IIfcProduct teil)
            {
                if (!_enthalten.TryGetValue(ort.EntityLabel, out IIfcRelContainedInSpatialStructure r))
                {
                    r = Wurzel<IIfcRelContainedInSpatialStructure>("IfcRelContainedInSpatialStructure", null);
                    r.RelatingStructure = ort;
                    _enthalten[ort.EntityLabel] = r;
                }
                r.RelatedElements.Add(teil);
            }

            public IIfcBuildingStorey Geschoss(IIfcBuilding g, string name, double hoeheMm, double? bruttoM2 = null)
            {
                IIfcBuildingStorey s = Wurzel<IIfcBuildingStorey>("IfcBuildingStorey", name);
                s.CompositionType = IfcElementCompositionEnum.ELEMENT;
                s.Elevation = new IfcLengthMeasure(hoeheMm);
                s.ObjectPlacement = Platzierung(g.ObjectPlacement, 0, 0, hoeheMm);
                Zerlegen(g, s);
                if (bruttoM2.HasValue) Mengen(s, "Qto_BuildingStoreyBaseQuantities", Flaeche("GrossFloorArea", bruttoM2.Value));
                return s;
            }

            public IIfcSpace Raum(IIfcBuildingStorey s, string nummer, string name, double x, double y,
                                  double? nettoM2, double? hoeheMm, double? volumenM3, bool beheizt)
            {
                IIfcSpace r = Wurzel<IIfcSpace>("IfcSpace", nummer);
                r.LongName = new IfcLabel(name);
                r.CompositionType = IfcElementCompositionEnum.ELEMENT;
                r.ObjectPlacement = Platzierung(s.ObjectPlacement, x, y, 0);
                _raumUrsprung[r.EntityLabel] = (x, y);
                Zerlegen(s, r);
                if (nettoM2.HasValue)
                {
                    var mengen = new List<IIfcPhysicalQuantity> { Flaeche("NetFloorArea", nettoM2.Value) };
                    if (hoeheMm.HasValue) mengen.Add(Laenge("Height", hoeheMm.Value));
                    if (volumenM3.HasValue) mengen.Add(Volumen("NetVolume", volumenM3.Value));
                    Mengen(r, "BaseQuantities", mengen.ToArray());
                }
                Satz(r, "Pset_SpaceCommon", ("IsExternal", new IfcBoolean(false)));
                if (beheizt)
                {
                    if (_ifc2x3)
                        Satz(r, "Pset_SpaceThermalRequirements", ("SpaceTemperatureMin", new IfcThermodynamicTemperatureMeasure(20)));
                    else
                    {
                        IIfcPropertyBoundedValue band = N<IIfcPropertyBoundedValue>("IfcPropertyBoundedValue");
                        band.Name = new IfcIdentifier("SpaceTemperature");
                        band.LowerBoundValue = new IfcThermodynamicTemperatureMeasure(18);
                        band.UpperBoundValue = new IfcThermodynamicTemperatureMeasure(26);
                        band.SetPointValue = new IfcThermodynamicTemperatureMeasure(20);
                        SatzAus(r, "Pset_SpaceThermalRequirements", band);
                    }
                }
                return r;
            }

            /// <summary>
            /// Die Körperdarstellung eines Raums: senkrechte Extrusion (2,5 m) eines geschlossenen Polylinienprofils mit den
            /// Punkten <paramref name="punkteMm"/> im System des Raums [mm].
            /// </summary>
            public void Grundriss(IIfcSpace r, params (double X, double Y)[] punkteMm)
            {
                IIfcPolyline linie = N<IIfcPolyline>("IfcPolyline");
                foreach ((double x, double y) in punkteMm.Concat(new[] { punkteMm[0] }))
                {
                    IIfcCartesianPoint q = N<IIfcCartesianPoint>("IfcCartesianPoint");
                    q.Coordinates.Add(new IfcLengthMeasure(x));
                    q.Coordinates.Add(new IfcLengthMeasure(y));
                    linie.Points.Add(q);
                }
                IIfcArbitraryClosedProfileDef profil = N<IIfcArbitraryClosedProfileDef>("IfcArbitraryClosedProfileDef");
                profil.ProfileType = IfcProfileTypeEnum.AREA;
                profil.OuterCurve = linie;
                IIfcAxis2Placement3D lage = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                lage.Location = Punkt(0, 0, 0);
                IIfcExtrudedAreaSolid koerper = N<IIfcExtrudedAreaSolid>("IfcExtrudedAreaSolid");
                koerper.SweptArea = profil;
                koerper.Position = lage;
                koerper.ExtrudedDirection = Richtung(0, 0, 1);
                koerper.Depth = new IfcPositiveLengthMeasure(2500);
                IIfcShapeRepresentation darstellung = N<IIfcShapeRepresentation>("IfcShapeRepresentation");
                darstellung.ContextOfItems = _kontext;
                darstellung.RepresentationIdentifier = new IfcLabel("Body");
                darstellung.RepresentationType = new IfcLabel("SweptSolid");
                darstellung.Items.Add(koerper);
                IIfcProductDefinitionShape form = N<IIfcProductDefinitionShape>("IfcProductDefinitionShape");
                form.Representations.Add(darstellung);
                r.Representation = form;
            }

            /// <summary>Ein Geschoss, das über das Enthaltensein (nicht die Zerlegung) am Gebäude hängt.</summary>
            public IIfcBuildingStorey GeschossEnthalten(IIfcBuilding g, string name, double hoeheMm)
            {
                IIfcBuildingStorey s = Wurzel<IIfcBuildingStorey>("IfcBuildingStorey", name);
                s.CompositionType = IfcElementCompositionEnum.ELEMENT;
                s.Elevation = new IfcLengthMeasure(hoeheMm);
                s.ObjectPlacement = Platzierung(g.ObjectPlacement, 0, 0, hoeheMm);
                Enthalten(g, s);
                return s;
            }

            /// <summary>
            /// Ein Raum, der über das Enthaltensein am Geschoss hängt (mit <paramref name="zerlegt"/> zusätzlich
            /// über die Zerlegung), Name in <c>Name</c>, ohne <c>LongName</c>; Mengen — wenn angegeben — im
            /// fremden Satz <c>CAD_RaumQuantities</c> als <c>Area</c>, <c>Volume</c>, <c>Height</c>.
            /// </summary>
            public IIfcSpace RaumEnthalten(IIfcBuildingStorey s, string name, double x, double y,
                                           double? flaecheM2, double? volumenM3, double? hoeheMm, bool zerlegt)
            {
                IIfcSpace r = Wurzel<IIfcSpace>("IfcSpace", name);
                r.CompositionType = IfcElementCompositionEnum.ELEMENT;
                r.ObjectPlacement = Platzierung(s.ObjectPlacement, x, y, 0);
                _raumUrsprung[r.EntityLabel] = (x, y);
                Enthalten(s, r);
                if (zerlegt) Zerlegen(s, r);
                if (flaecheM2.HasValue) RaumMengen(r, "CAD_RaumQuantities", flaecheM2.Value, volumenM3.Value, hoeheMm.Value, standard: false);
                return r;
            }

            /// <summary>
            /// <b>Ein Bauteil nach dem Muster eines CAD-Exports</b> (Mehrzonenkonzept 6.5): kein <c>IsExternal</c>,
            /// keine Raumgrenze, Platzierung im Ursprung des Geschosses. Die Angaben stehen in fremden Sätzen
            /// (Klassenkopf von <see cref="Enthaltensein"/>); <paramref name="breiteMm"/> und <paramref name="laengeMm"/>
            /// sind Lockwerte <c>Width</c>/<c>Length</c> im fremden Mengensatz, aus denen nichts abgeleitet werden darf.
            /// </summary>
            public T CadBauteil<T>(IIfcBuildingStorey s, string express, string name, string angrenzung, bool huelle,
                                   double? u, double? uFalscheEinheit, double? orientierungGrad, double? brutto, double? netto,
                                   double? breiteMm, double? laengeMm) where T : class, IIfcElement
            {
                T e = Wurzel<T>(express, name);
                e.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, e);
                CadEigenschaften(e, express.Substring(3), angrenzung, huelle, u, uFalscheEinheit, orientierungGrad);
                var mengen = new List<IIfcPhysicalQuantity>();
                if (brutto.HasValue) mengen.Add(Flaeche("GrossArea", brutto.Value));
                if (netto.HasValue) mengen.Add(Flaeche("NetArea", netto.Value));
                if (breiteMm.HasValue) mengen.Add(Laenge("Width", breiteMm.Value));
                if (laengeMm.HasValue) mengen.Add(Laenge("Length", laengeMm.Value));
                if (mengen.Count > 0) Mengen(e, "CAD_BauteilQuantities", mengen.ToArray());
                return e;
            }

            /// <summary>
            /// Ein Fenster des CAD-Exports: über eine Öffnung im <paramref name="wirt"/> oder — mit
            /// <paramref name="teil"/> — als Teil des Wirts (<c>IfcRelAggregates</c>, Dachfenster). Die Fläche über
            /// <c>OverallWidth</c>/<c>OverallHeight</c> oder nur als <c>GrossArea</c> im fremden Satz, neben dem
            /// Lockwert Width = Length = 5 m.
            /// </summary>
            public IIfcWindow CadFenster(IIfcElement wirt, IIfcBuildingStorey s, string name, (double B, double H)? gesamtMm,
                                         double? grossArea, double u, double? uFalscheEinheit, bool teil = false)
            {
                IIfcWindow f = Wurzel<IIfcWindow>("IfcWindow", name);
                f.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                if (gesamtMm.HasValue)
                {
                    f.OverallWidth = new IfcPositiveLengthMeasure(gesamtMm.Value.B);
                    f.OverallHeight = new IfcPositiveLengthMeasure(gesamtMm.Value.H);
                }
                if (teil) Zerlegen(wirt, f);
                else
                {
                    Enthalten(s, f);
                    Fuellen(Oeffnung(wirt, name), f);
                }
                CadEigenschaften(f, "Window", "btaOutside", true, u, uFalscheEinheit, null);
                if (grossArea.HasValue)
                    Mengen(f, "CAD_BauteilQuantities", Flaeche("GrossArea", grossArea.Value), Laenge("Width", 5000), Laenge("Length", 5000));
                return f;
            }

            /// <summary>Die Beheizungsart eines Raums nach dem Muster des CAD-Exports (<c>HeatingType</c>, Aufzählung).</summary>
            /// <summary>Der Heizsollwert eines Raums als <c>Pset_SpaceThermalRequirements.SpaceTemperature</c> [°C].</summary>
            public void Solltemperatur(IIfcSpace r, double temperaturC)
                => Satz(r, "Pset_SpaceThermalRequirements", ("SpaceTemperature", new IfcThermodynamicTemperatureMeasure(temperaturC)));

            /// <summary>Beheizungsart, Raumtemperatur und Raumtyp eines CAD-Exports in einem Satz <c>CAD_RaumAllgemein</c>.</summary>
            public void Raumangaben(IIfcSpace r, string beheizungsart, double? temperaturC, string raumtyp)
            {
                var liste = new List<IIfcProperty> { Aufzaehlung("HeatingType", beheizungsart) };
                if (temperaturC.HasValue) liste.Add(Einzel("InsideTemperature (°C)", new IfcReal(temperaturC.Value)));
                liste.Add(Aufzaehlung("RoomType", raumtyp));
                SatzMit(r, "CAD_RaumAllgemein", liste);
            }

            public void Beheizungsart(IIfcSpace r, string wert, double? temperaturC = null)
            {
                var liste = new List<IIfcProperty> { Aufzaehlung("HeatingType", wert) };
                if (temperaturC.HasValue) liste.Add(Einzel("InsideTemperature (°C)", new IfcReal(temperaturC.Value)));
                SatzMit(r, "CAD_RaumAllgemein", liste);
            }

            private void CadEigenschaften(IIfcElement e, string klasse, string angrenzung, bool huelle, double? u,
                                          double? uFalscheEinheit, double? orientierungGrad)
            {
                var allgemein = new List<IIfcProperty> { Aufzaehlung("AdjacentType", angrenzung) };
                if (orientierungGrad.HasValue)
                    allgemein.Add(Einzel("Orientation (°)",
                        new IfcLabel(orientierungGrad.Value.ToString("0.###", CultureInfo.GetCultureInfo("de-DE")))));
                SatzMit(e, "CAD_BauteilAllgemein", allgemein);
                Satz(e, "CAD_BauteilEnergetik", ("ElementEnergyConsultingProperties.CladdingSurface", new IfcBoolean(huelle)));
                if (u.HasValue) Satz(e, "CAD_Bauteilreferenzen", ("UValue (W/(m² K))", new IfcReal(u.Value)));
                if (uFalscheEinheit.HasValue)
                    Satz(e, "Pset_" + klasse + "Common", ("ThermalTransmittance (W/(m K))", new IfcReal(uFalscheEinheit.Value)));
            }

            /// <summary>
            /// Eine Aufzählungseigenschaft mit einem Textwert. Die Schnittstelle führt die Werte nur lesend;
            /// angelegt wird über die Klasse des Schemas (Testhilfe, nicht Kern).
            /// </summary>
            private IIfcProperty Aufzaehlung(string name, string wert)
            {
                if (_ifc2x3)
                {
                    var p2 = _m.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertyEnumeratedValue>();
                    p2.Name = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(name);
                    p2.EnumerationValues.Add(new Xbim.Ifc2x3.MeasureResource.IfcLabel(wert));
                    return p2;
                }
                var p4 = N<Xbim.Ifc4.PropertyResource.IfcPropertyEnumeratedValue>("IfcPropertyEnumeratedValue");
                p4.Name = new IfcIdentifier(name);
                p4.EnumerationValues.Add(new IfcLabel(wert));
                return p4;
            }

            private void SatzMit(IIfcObject o, string name, IEnumerable<IIfcProperty> eigenschaften)
            {
                IIfcPropertySet ps = Wurzel<IIfcPropertySet>("IfcPropertySet", name);
                foreach (IIfcProperty p in eigenschaften) ps.HasProperties.Add(p);
                Definieren(o, ps);
            }

            /// <summary>Raummengen unter den Standardnamen (<paramref name="standard"/>) oder den Namen des CAD-Exports.</summary>
            public void RaumMengen(IIfcSpace r, string satz, double flaecheM2, double volumenM3, double hoeheMm, bool standard)
                => Mengen(r, satz, Flaeche(standard ? "NetFloorArea" : "Area", flaecheM2),
                          Volumen(standard ? "NetVolume" : "Volume", volumenM3), Laenge("Height", hoeheMm));

            public IIfcWallType Wandtyp(string name, double? u)
            {
                IIfcWallType t = Wurzel<IIfcWallType>("IfcWallType", name);
                t.PredefinedType = IfcWallTypeEnum.STANDARD;
                IIfcPropertySet ps = Wurzel<IIfcPropertySet>("IfcPropertySet", "Pset_WallCommon");
                ps.HasProperties.Add(Einzel("IsExternal", new IfcBoolean(true)));
                if (u.HasValue) ps.HasProperties.Add(Einzel("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u.Value)));
                t.HasPropertySets.Add(ps);
                return t;
            }

            /// <summary>
            /// Ein Baustoff mit λ [W/(mK)], ρ [kg/m³] und c [J/(kgK)] — IFC4 in <c>Pset_MaterialThermal</c> und
            /// <c>Pset_MaterialCommon</c> (<c>IfcMaterialProperties</c>); IFC2X3 als Attribute von
            /// <c>IfcThermalMaterialProperties</c> und <c>IfcGeneralMaterialProperties</c>, mit
            /// <paramref name="erweitert2x3"/> zusätzlich ein <c>IfcExtendedMaterialProperties</c>
            /// „Pset_MaterialThermal". Die IFC2X3-Sätze sind über die Schnittstellen nicht anzulegen; hier
            /// (Testhilfe, nicht Kern) über die Schemaklassen.
            /// </summary>
            public IIfcMaterial Baustoff(string name, double lambda, double rho, double cp, bool erweitert2x3 = false)
            {
                IIfcMaterial m = N<IIfcMaterial>("IfcMaterial");
                m.Name = new IfcLabel(name);
                if (_ifc2x3)
                {
                    var m2 = (Xbim.Ifc2x3.MaterialResource.IfcMaterial)m;
                    _m.Instances.New<Xbim.Ifc2x3.MaterialPropertyResource.IfcThermalMaterialProperties>(t =>
                    {
                        t.Material = m2;
                        t.ThermalConductivity = new Xbim.Ifc2x3.MeasureResource.IfcThermalConductivityMeasure(lambda);
                        t.SpecificHeatCapacity = new Xbim.Ifc2x3.MeasureResource.IfcSpecificHeatCapacityMeasure(cp);
                    });
                    _m.Instances.New<Xbim.Ifc2x3.MaterialPropertyResource.IfcGeneralMaterialProperties>(t =>
                    {
                        t.Material = m2;
                        t.MassDensity = new Xbim.Ifc2x3.MeasureResource.IfcMassDensityMeasure(rho);
                    });
                    if (erweitert2x3)
                        _m.Instances.New<Xbim.Ifc2x3.MaterialPropertyResource.IfcExtendedMaterialProperties>(t =>
                        {
                            t.Material = m2;
                            t.Name = new Xbim.Ifc2x3.MeasureResource.IfcLabel("Pset_MaterialThermal");
                            t.ExtendedProperties.Add(_m.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(v =>
                            {
                                v.Name = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier("ThermalConductivity");
                                v.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcThermalConductivityMeasure(lambda);
                            }));
                        });
                    return m;
                }
                // Die Schnittstelle führt Properties nur lesend; angelegt wird über die IFC4-Klasse.
                var thermisch = N<Xbim.Ifc4.MaterialResource.IfcMaterialProperties>("IfcMaterialProperties");
                thermisch.Name = new IfcIdentifier("Pset_MaterialThermal");
                thermisch.Material = (Xbim.Ifc4.MaterialResource.IfcMaterialDefinition)m;
                thermisch.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)Einzel("ThermalConductivity", new IfcThermalConductivityMeasure(lambda)));
                if (!double.IsNaN(cp))   // NaN = die Eigenschaft fehlt (masselose IFC4-Schicht)
                    thermisch.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)Einzel("SpecificHeatCapacity", new IfcSpecificHeatCapacityMeasure(cp)));
                var allgemein = N<Xbim.Ifc4.MaterialResource.IfcMaterialProperties>("IfcMaterialProperties");
                allgemein.Name = new IfcIdentifier("Pset_MaterialCommon");
                allgemein.Material = (Xbim.Ifc4.MaterialResource.IfcMaterialDefinition)m;
                allgemein.Properties.Add((Xbim.Ifc4.PropertyResource.IfcProperty)Einzel("MassDensity", new IfcMassDensityMeasure(rho)));
                return m;
            }

            /// <summary>Ein Schichtsatz; die Schichten in der Reihenfolge der Datei, Dicken in mm.</summary>
            public IIfcMaterialLayerSet Schichtsatz(string name, params (IIfcMaterial Stoff, double DickeMm)[] schichten)
            {
                IIfcMaterialLayerSet s = N<IIfcMaterialLayerSet>("IfcMaterialLayerSet");
                s.LayerSetName = new IfcLabel(name);
                foreach ((IIfcMaterial stoff, double dicke) in schichten)
                {
                    IIfcMaterialLayer l = N<IIfcMaterialLayer>("IfcMaterialLayer");
                    l.Material = stoff;
                    l.LayerThickness = new IfcNonNegativeLengthMeasure(dicke);
                    s.MaterialLayers.Add(l);
                }
                return s;
            }

            /// <summary>Hängt einen Schichtsatz über eine <c>IfcMaterialLayerSetUsage</c> an das Bauteil (Bezugslinie ohne Versatz).</summary>
            public void Schichten(IIfcElement e, IIfcMaterialLayerSet satz, IfcLayerSetDirectionEnum achse, IfcDirectionSenseEnum sinn)
            {
                IIfcMaterialLayerSetUsage nutzung = N<IIfcMaterialLayerSetUsage>("IfcMaterialLayerSetUsage");
                nutzung.ForLayerSet = satz;
                nutzung.LayerSetDirection = achse;
                nutzung.DirectionSense = sinn;
                nutzung.OffsetFromReferenceLine = new IfcLengthMeasure(0);
                IIfcRelAssociatesMaterial rel = Wurzel<IIfcRelAssociatesMaterial>("IfcRelAssociatesMaterial", null);
                rel.RelatingMaterial = nutzung;
                rel.RelatedObjects.Add(e);
            }

            private readonly Dictionary<int, IIfcRelDefinesByType> _typisiert = new Dictionary<int, IIfcRelDefinesByType>();

            public IIfcWall Wand(IIfcBuildingStorey s, string name, Wandlage l, IIfcWallType typ, double? u, string satz,
                                 double? brutto, double? netto, (double L, double H)? laengeHoehe, IIfcSpace[] raeume,
                                 IfcInternalOrExternalEnum grenze = IfcInternalOrExternalEnum.EXTERNAL)
            {
                IIfcWall w = Wurzel<IIfcWall>("IfcWall", name);
                w.PredefinedType = IfcWallTypeEnum.STANDARD;
                w.ObjectPlacement = Platzierung(s.ObjectPlacement, l.X, l.Y, 0, l.Rx, l.Ry);
                Enthalten(s, w);
                if (!_typisiert.TryGetValue(typ.EntityLabel, out IIfcRelDefinesByType rel))
                {
                    rel = Wurzel<IIfcRelDefinesByType>("IfcRelDefinesByType", null);
                    rel.RelatingType = typ;
                    _typisiert[typ.EntityLabel] = rel;
                }
                rel.RelatedObjects.Add(w);

                if (u.HasValue) Satz(w, "Pset_WallCommon", ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u.Value)));
                var mengen = new List<IIfcPhysicalQuantity>();
                if (brutto.HasValue) mengen.Add(Flaeche("GrossSideArea", brutto.Value));
                if (netto.HasValue) mengen.Add(Flaeche("NetSideArea", netto.Value));
                if (laengeHoehe.HasValue)
                {
                    mengen.Add(Laenge("Length", laengeHoehe.Value.L));
                    mengen.Add(Laenge("Height", laengeHoehe.Value.H));
                }
                if (satz != null && mengen.Count > 0) Mengen(w, satz, mengen.ToArray());
                foreach (IIfcSpace r in raeume) Grenze(r, w, grenze);
                return w;
            }

            /// <summary>
            /// Eine Vorhangfassade als Ganzes (<c>IfcCurtainWall</c>) in der Lage einer Außenwand: außen erklärt,
            /// mit Bruttofläche und — wenn angegeben — U-Wert in <c>Pset_CurtainWallCommon</c>.
            /// </summary>
            public IIfcCurtainWall Vorhangfassade(IIfcBuildingStorey s, string name, Wandlage l, double? u, double brutto, IIfcSpace[] raeume)
            {
                IIfcCurtainWall w = Wurzel<IIfcCurtainWall>("IfcCurtainWall", name);
                w.ObjectPlacement = Platzierung(s.ObjectPlacement, l.X, l.Y, 0, l.Rx, l.Ry);
                Enthalten(s, w);
                if (u.HasValue)
                    Satz(w, "Pset_CurtainWallCommon", ("IsExternal", new IfcBoolean(true)),
                         ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u.Value)));
                else
                    Satz(w, "Pset_CurtainWallCommon", ("IsExternal", new IfcBoolean(true)));
                Mengen(w, "BaseQuantities", Flaeche("GrossSideArea", brutto));
                foreach (IIfcSpace r in raeume) Grenze(r, w, IfcInternalOrExternalEnum.EXTERNAL);
                return w;
            }

            public IIfcWall Innenwand(IIfcBuildingStorey s, string name, double x, double y, double brutto, IIfcSpace[] raeume)
            {
                IIfcWall w = Wurzel<IIfcWall>("IfcWall", name);
                w.PredefinedType = IfcWallTypeEnum.PARTITIONING;
                w.ObjectPlacement = Platzierung(s.ObjectPlacement, x, y, 0, 0, 1);
                Enthalten(s, w);
                Satz(w, "Pset_WallCommon", ("IsExternal", new IfcBoolean(false)));
                Mengen(w, "BaseQuantities", Flaeche("GrossSideArea", brutto));
                foreach (IIfcSpace r in raeume) Grenze(r, w, IfcInternalOrExternalEnum.INTERNAL);
                return w;
            }

            public IIfcWindow Fenster(IIfcWall wirt, IIfcBuildingStorey s, string name, double? flaeche, (double B, double H)? breiteHoeheMm = null)
            {
                IIfcOpeningElement o = Oeffnung(wirt, name);
                IIfcWindow f = Wurzel<IIfcWindow>("IfcWindow", name);
                Enthalten(s, f);
                Fuellen(o, f);
                if (flaeche.HasValue) Mengen(f, "Qto_WindowBaseQuantities", Flaeche("Area", flaeche.Value));
                else if (breiteHoeheMm.HasValue)
                    Mengen(f, "Qto_WindowBaseQuantities", Laenge("Width", breiteHoeheMm.Value.B), Laenge("Height", breiteHoeheMm.Value.H));
                Satz(f, "Pset_WindowCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(1.1)));
                Satz(f, "Pset_DoorWindowGlazingType", ("SolarHeatGainTransmittance", new IfcNormalisedRatioMeasure(0.6)));
                return f;
            }

            // ----------------------------------------------------------
            //  Stufe G6c: Raumgrenzen mit Geometrie, Zonen, Klassifikation
            // ----------------------------------------------------------

            /// <summary>
            /// Eine Raumgrenze der 2. Ebene (<c>IfcRelSpaceBoundary2ndLevel</c>, nur IFC4) — mit
            /// <paramref name="punkteMm"/> samt <c>IfcCurveBoundedPlane</c>: Ebene mit der Normalen
            /// <paramref name="normale"/>, Außenrand als <c>IfcPolyline</c> aus 3D-Punkten im System des Raums.
            /// </summary>
            public IIfcRelSpaceBoundary Grenze2(IIfcSpace r, IIfcElement e, IfcInternalOrExternalEnum art,
                                                double[] normale = null, params double[][] punkteMm)
            {
                IIfcRelSpaceBoundary2ndLevel g = Wurzel<IIfcRelSpaceBoundary2ndLevel>("IfcRelSpaceBoundary2ndLevel", "2ndLevel");
                g.Description = new IfcText("2a");
                g.RelatingSpace = r;
                g.RelatedBuildingElement = e;
                g.PhysicalOrVirtualBoundary = IfcPhysicalOrVirtualEnum.PHYSICAL;
                g.InternalOrExternalBoundary = art;
                if (punkteMm != null && punkteMm.Length >= 3)
                {
                    IIfcAxis2Placement3D lage = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                    lage.Location = Punkt(0, 0, 0);
                    lage.Axis = Richtung(normale[0], normale[1], normale[2]);
                    lage.RefDirection = Math.Abs(normale[2]) > 0.5 ? Richtung(1, 0, 0) : Richtung(0, 0, 1);
                    IIfcPlane ebene = N<IIfcPlane>("IfcPlane");
                    ebene.Position = lage;
                    // Die Punkte sind im System des Geschosses angegeben; die Datei führt sie im System des Raums.
                    (double ux, double uy) = _raumUrsprung[r.EntityLabel];
                    IIfcPolyline rand = N<IIfcPolyline>("IfcPolyline");
                    foreach (double[] q in punkteMm) rand.Points.Add(Punkt(q[0] - ux, q[1] - uy, q[2]));
                    rand.Points.Add(Punkt(punkteMm[0][0] - ux, punkteMm[0][1] - uy, punkteMm[0][2]));
                    IIfcCurveBoundedPlane flaeche = N<IIfcCurveBoundedPlane>("IfcCurveBoundedPlane");
                    flaeche.BasisSurface = ebene;
                    flaeche.OuterBoundary = rand;
                    IIfcConnectionSurfaceGeometry geo = N<IIfcConnectionSurfaceGeometry>("IfcConnectionSurfaceGeometry");
                    geo.SurfaceOnRelatingElement = flaeche;
                    g.ConnectionGeometry = geo;
                }
                return g;
            }

            /// <summary>Hängt <paramref name="teil"/> über <c>IfcRelAggregates</c> an <paramref name="ganzes"/>.</summary>
            public void Teil(IIfcObjectDefinition ganzes, IIfcObjectDefinition teil) => Zerlegen(ganzes, teil);

            /// <summary>Eine einzelne Eigenschaft in einem eigenen Satz <paramref name="satz"/>.</summary>
            public void Eigenschaft(IIfcObject o, string satz, string name, IIfcValue wert) => Satz(o, satz, (name, wert));

            /// <summary>Zwei Grenzen als Gegenstücke (<c>CorrespondingBoundary</c> beidseitig).</summary>
            public void Gegenstuecke(IIfcRelSpaceBoundary a, IIfcRelSpaceBoundary b)
            {
                ((IIfcRelSpaceBoundary2ndLevel)a).CorrespondingBoundary = (IIfcRelSpaceBoundary2ndLevel)b;
                ((IIfcRelSpaceBoundary2ndLevel)b).CorrespondingBoundary = (IIfcRelSpaceBoundary2ndLevel)a;
            }

            /// <summary>Eine Zone (<c>IfcZone</c>) mit ihren Gliedern — Räume oder Zonen — über <c>IfcRelAssignsToGroup</c>.</summary>
            public IIfcZone Zone(string name, params IIfcObjectDefinition[] glieder)
            {
                IIfcZone z = Wurzel<IIfcZone>("IfcZone", name);
                IIfcRelAssignsToGroup rel = Wurzel<IIfcRelAssignsToGroup>("IfcRelAssignsToGroup", null);
                rel.RelatingGroup = z;
                foreach (IIfcObjectDefinition o in glieder) rel.RelatedObjects.Add(o);
                return z;
            }

            private readonly Dictionary<string, IIfcClassification> _klassifikationen = new Dictionary<string, IIfcClassification>();
            private readonly Dictionary<int, (double X, double Y)> _raumUrsprung = new Dictionary<int, (double X, double Y)>();

            /// <summary>Eine Klassifikation am Raum: Quelle (<c>IfcClassification</c>) und Kennung (<c>IfcClassificationReference</c>).</summary>
            public void Klasse(IIfcSpace r, string quelle, string kennung)
            {
                if (!_klassifikationen.TryGetValue(quelle, out IIfcClassification c))
                {
                    c = N<IIfcClassification>("IfcClassification");
                    c.Name = new IfcLabel(quelle);
                    _klassifikationen[quelle] = c;
                }
                IIfcClassificationReference k = N<IIfcClassificationReference>("IfcClassificationReference");
                k.Identification = new IfcIdentifier(kennung);
                k.ReferencedSource = c;
                IIfcRelAssociatesClassification rel = Wurzel<IIfcRelAssociatesClassification>("IfcRelAssociatesClassification", null);
                rel.RelatingClassification = k;
                rel.RelatedObjects.Add(r);
            }

            /// <summary>Die Dicke eines Bauteils als Menge <c>Width</c> [mm].</summary>
            public void Dicke(IIfcElement e, double mm) => Mengen(e, "BaseQuantities", Laenge("Width", mm));

            public void Tuer(IIfcWall wirt, IIfcBuildingStorey s, string name, double breiteMm, double hoeheMm)
            {
                IIfcOpeningElement o = Oeffnung(wirt, name);
                IIfcDoor t = Wurzel<IIfcDoor>("IfcDoor", name);
                t.OverallWidth = new IfcPositiveLengthMeasure(breiteMm);
                t.OverallHeight = new IfcPositiveLengthMeasure(hoeheMm);
                Enthalten(s, t);
                Fuellen(o, t);
                Satz(t, "Pset_DoorCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(1.8)));
            }

            private IIfcOpeningElement Oeffnung(IIfcElement wirt, string name)
            {
                IIfcOpeningElement o = Wurzel<IIfcOpeningElement>("IfcOpeningElement", "Öffnung " + name);
                IIfcRelVoidsElement v = Wurzel<IIfcRelVoidsElement>("IfcRelVoidsElement", null);
                v.RelatingBuildingElement = wirt;
                v.RelatedOpeningElement = o;
                return o;
            }

            private void Fuellen(IIfcOpeningElement o, IIfcElement e)
            {
                IIfcRelFillsElement f = Wurzel<IIfcRelFillsElement>("IfcRelFillsElement", null);
                f.RelatingOpeningElement = o;
                f.RelatedBuildingElement = e;
            }

            public IIfcSlab Platte(IIfcBuildingStorey s, string name, IfcSlabTypeEnum art, bool aussen, double? u, double? brutto,
                                   IIfcSpace[] raeume, IfcInternalOrExternalEnum grenze)
            {
                IIfcSlab p = Wurzel<IIfcSlab>("IfcSlab", name);
                p.PredefinedType = art;
                p.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 0);
                Enthalten(s, p);
                if (u.HasValue)
                    Satz(p, "Pset_SlabCommon", ("IsExternal", new IfcBoolean(aussen)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u.Value)));
                else
                    Satz(p, "Pset_SlabCommon", ("IsExternal", new IfcBoolean(aussen)));
                if (brutto.HasValue) Mengen(p, "BaseQuantities", Flaeche("GrossArea", brutto.Value));
                foreach (IIfcSpace r in raeume) Grenze(r, p, grenze);
                return p;
            }

            public IIfcRoof Dach(IIfcBuildingStorey s, string name, double u, double brutto, IIfcSpace[] raeume)
            {
                IIfcRoof d = Wurzel<IIfcRoof>("IfcRoof", name);
                d.PredefinedType = IfcRoofTypeEnum.FLAT_ROOF;
                d.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 2800);
                Enthalten(s, d);
                Satz(d, "Pset_RoofCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u)));
                Mengen(d, "Qto_RoofBaseQuantities", Flaeche("GrossArea", brutto));
                foreach (IIfcSpace r in raeume) Grenze(r, d, IfcInternalOrExternalEnum.EXTERNAL);
                return d;
            }

            /// <summary>Eine Raumgrenze als BASISKLASSE mit Name '2ndLevel' und Description '2a' (Archicad-Muster, 3.5 Nr. 4).</summary>
            private void Grenze(IIfcSpace r, IIfcElement e, IfcInternalOrExternalEnum art)
            {
                IIfcRelSpaceBoundary g = Wurzel<IIfcRelSpaceBoundary>("IfcRelSpaceBoundary", "2ndLevel");
                g.Description = new IfcText("2a");
                g.RelatingSpace = r;
                g.RelatedBuildingElement = e;
                g.PhysicalOrVirtualBoundary = IfcPhysicalOrVirtualEnum.PHYSICAL;
                g.InternalOrExternalBoundary = art;
            }

            private IIfcPropertySingleValue Einzel(string name, IIfcValue wert)
            {
                IIfcPropertySingleValue p = N<IIfcPropertySingleValue>("IfcPropertySingleValue");
                p.Name = new IfcIdentifier(name);
                p.NominalValue = wert;
                return p;
            }

            /// <summary>Ein Eigenschaftssatz mit Einzelwerten an <paramref name="o"/>.</summary>
            public void Eigenschaften(IIfcObject o, string name, params (string Name, IIfcValue Wert)[] werte) => Satz(o, name, werte);

            private void Satz(IIfcObject o, string name, params (string Name, IIfcValue Wert)[] werte)
            {
                IIfcPropertySet ps = Wurzel<IIfcPropertySet>("IfcPropertySet", name);
                foreach ((string n, IIfcValue w) in werte) ps.HasProperties.Add(Einzel(n, w));
                Definieren(o, ps);
            }

            private void SatzAus(IIfcObject o, string name, IIfcProperty eigenschaft)
            {
                IIfcPropertySet ps = Wurzel<IIfcPropertySet>("IfcPropertySet", name);
                ps.HasProperties.Add(eigenschaft);
                Definieren(o, ps);
            }

            private void Mengen(IIfcObject o, string name, params IIfcPhysicalQuantity[] mengen)
            {
                IIfcElementQuantity q = Wurzel<IIfcElementQuantity>("IfcElementQuantity", name);
                foreach (IIfcPhysicalQuantity m in mengen) q.Quantities.Add(m);
                Definieren(o, q);
            }

            private void Definieren(IIfcObject o, IIfcPropertySetDefinition d)
            {
                IIfcRelDefinesByProperties rel = Wurzel<IIfcRelDefinesByProperties>("IfcRelDefinesByProperties", null);
                rel.RelatedObjects.Add(o);
                rel.RelatingPropertyDefinition = d;
            }

            private IIfcQuantityArea Flaeche(string name, double wert)
            {
                IIfcQuantityArea q = N<IIfcQuantityArea>("IfcQuantityArea");
                q.Name = new IfcLabel(name);
                q.AreaValue = new IfcAreaMeasure(wert);
                return q;
            }

            private IIfcQuantityLength Laenge(string name, double wert)
            {
                IIfcQuantityLength q = N<IIfcQuantityLength>("IfcQuantityLength");
                q.Name = new IfcLabel(name);
                q.LengthValue = new IfcLengthMeasure(wert);
                return q;
            }

            private IIfcQuantityVolume Volumen(string name, double wert)
            {
                IIfcQuantityVolume q = N<IIfcQuantityVolume>("IfcQuantityVolume");
                q.Name = new IfcLabel(name);
                q.VolumeValue = new IfcVolumeMeasure(wert);
                return q;
            }

            /// <summary>Schließt ab und schreibt STEP mit festem Kopf.</summary>
            public byte[] Speichern()
            {
                _t.Commit();
                IStepFileHeader kopf = _m.Header;
                kopf.FileDescription.Description.Clear();
                kopf.FileDescription.Description.Add("ViewDefinition [ReferenceView]");
                kopf.FileName.Name = _dateiname;
                kopf.FileName.TimeStamp = ZEITSTEMPEL;
                kopf.FileName.AuthorName.Clear();
                kopf.FileName.AuthorName.Add("");
                kopf.FileName.Organization.Clear();
                kopf.FileName.Organization.Add("");
                kopf.FileName.PreprocessorVersion = "EPOS-Plan Importprobe (EPOS.Kern.Tests)";
                kopf.FileName.OriginatingSystem = "EPOS-Plan Importprobe, selbst erzeugt";
                kopf.FileName.AuthorizationName = "";
                var ziel = new MemoryStream();
                _m.SaveAsStep21(ziel, null, true);
                // Die Bibliothek schreibt die Zeilenenden der Plattform; die Probe führt immer CRLF, damit
                // die Erzeugung unter Windows und Linux dieselben Bytes liefert.
                string text = Encoding.ASCII.GetString(ziel.ToArray()).Replace("\r\n", "\n").Replace("\n", "\r\n");
                return Encoding.ASCII.GetBytes(text);
            }
        }
    }
}
