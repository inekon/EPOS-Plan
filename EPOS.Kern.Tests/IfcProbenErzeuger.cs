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
    internal static class IfcProbenErzeuger
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
                ["ifc4_haus.ifczip"] = Zip("ifc4_haus.ifc", haus),
                ["ifc4x1_kopf.ifc"] = Ifc4x1Kopf(),
                ["ifc4_zwei_gebaeude.ifc"] = ZweiGebaeude(),
                ["ifc4_ohne_mengen.ifc"] = OhneMengen(),
                ["ifc4_mapconversion.ifc"] = MapConversion(),
                ["ifc4_schichten.ifc"] = Schichten(XbimSchemaVersion.Ifc4, "ifc4_schichten.ifc", nullwerte: false),
                ["ifc4_schichten_nullwerte.ifc"] = Schichten(XbimSchemaVersion.Ifc4, "ifc4_schichten_nullwerte.ifc", nullwerte: true),
                ["ifc2x3_schichten.ifc"] = Schichten(XbimSchemaVersion.Ifc2X3, "ifc2x3_schichten.ifc", nullwerte: false),
                ["ifc4_rueckfaelle.ifc"] = Rueckfaelle(),
            };
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
        public static byte[] Schichten(XbimSchemaVersion schema, string dateiname, bool nullwerte)
        {
            using (var b = new Bau(schema, dateiname))
            {
                b.Anfang(new[] { 0.0, 1.0, 0.0 }, karte: false);
                IIfcBuilding g = b.Gebaeude("Schichtenhaus", null);
                IIfcBuildingStorey eg = b.Geschoss(g, "Erdgeschoss", 0);
                IIfcSpace r = b.Raum(eg, "0.01", "Wohnen", 150, 150, 80, 2500, 200, beheizt: true);
                IIfcWallType typ = b.Wandtyp("Außenwand Typ S", null);

                IIfcMaterial putz = b.Baustoff("Putz", 0.7, 1400, 1000);
                IIfcMaterial mauer = b.Baustoff("Mauerwerk", 0.5, 1200, 1000);
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

        /// <summary>Das Probenhaus im Schema <paramref name="schema"/> (IFC4 oder IFC2X3).</summary>
        public static byte[] Haus(XbimSchemaVersion schema, string dateiname)
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
                b.Innenwand(eg, "EG Innenwand", 6000, 0, 20.8, new[] { wohnen, kueche });

                // Decken und Dach.
                b.Platte(eg, "Kellerdecke", IfcSlabTypeEnum.FLOOR, aussen: false, u: 0.35, brutto: 80.0,
                         raeume: new[] { wohnen, kueche, keller }, grenze: IfcInternalOrExternalEnum.INTERNAL);
                b.Platte(og, "Geschossdecke", IfcSlabTypeEnum.FLOOR, aussen: false, u: null, brutto: 80.0,
                         raeume: new[] { wohnen, kueche, schlafen, bad }, grenze: IfcInternalOrExternalEnum.INTERNAL);
                b.Platte(kg, "Bodenplatte", IfcSlabTypeEnum.BASESLAB, aussen: true, u: 0.5, brutto: 80.0,
                         raeume: new[] { keller }, grenze: IfcInternalOrExternalEnum.EXTERNAL_EARTH);
                b.Dach(og, "Dach", u: 0.2, brutto: 80.0, raeume: new[] { schlafen, bad });

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

        private sealed class Bau : IDisposable
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

            /// <summary>Projekt, Einheiten, Modellkontext (mit Nordrichtung und ggf. Koordinatenumrechnung), Grundstück.</summary>
            public void Anfang(double[] nord, bool karte)
            {
                _projekt = Wurzel<IIfcProject>("IfcProject", "Importprobe");
                IIfcUnitAssignment e = N<IIfcUnitAssignment>("IfcUnitAssignment");
                e.Units.Add(Einheit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, IfcSIPrefix.MILLI));
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

            public void Innenwand(IIfcBuildingStorey s, string name, double x, double y, double brutto, IIfcSpace[] raeume)
            {
                IIfcWall w = Wurzel<IIfcWall>("IfcWall", name);
                w.PredefinedType = IfcWallTypeEnum.PARTITIONING;
                w.ObjectPlacement = Platzierung(s.ObjectPlacement, x, y, 0, 0, 1);
                Enthalten(s, w);
                Satz(w, "Pset_WallCommon", ("IsExternal", new IfcBoolean(false)));
                Mengen(w, "BaseQuantities", Flaeche("GrossSideArea", brutto));
                foreach (IIfcSpace r in raeume) Grenze(r, w, IfcInternalOrExternalEnum.INTERNAL);
            }

            public void Fenster(IIfcWall wirt, IIfcBuildingStorey s, string name, double? flaeche, (double B, double H)? breiteHoeheMm = null)
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
            }

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

            public void Dach(IIfcBuildingStorey s, string name, double u, double brutto, IIfcSpace[] raeume)
            {
                IIfcRoof d = Wurzel<IIfcRoof>("IfcRoof", name);
                d.PredefinedType = IfcRoofTypeEnum.FLAT_ROOF;
                d.ObjectPlacement = Platzierung(s.ObjectPlacement, 0, 0, 2800);
                Enthalten(s, d);
                Satz(d, "Pset_RoofCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u)));
                Mengen(d, "Qto_RoofBaseQuantities", Flaeche("GrossArea", brutto));
                foreach (IIfcSpace r in raeume) Grenze(r, d, IfcInternalOrExternalEnum.EXTERNAL);
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
