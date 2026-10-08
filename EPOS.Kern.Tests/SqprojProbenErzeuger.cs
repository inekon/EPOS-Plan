#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Erzeugt HottCAD-Projektdateien im Kleinstformat</b> (Datenaustauschkonzept 16.7, Proben 33–36) — zur Laufzeit,
    /// deterministisch, unter einem temporären Pfad: eine <c>.sqproj</c> ist SQLite und gehört nie ins Repositorium. Die
    /// Tabellen tragen nur die Spalten, die der Leser liest (Befund Kapitel 2 und 3.1–3.4); Namen sind neutral, Werte rund.
    /// Ohne Kerntypen, damit <c>EPOS.UI.Tests</c> die Datei verlinken kann; das passende IFC-Abbild steht in
    /// <c>SqprojProbenErzeuger.Ifc.cs</c>.
    /// </summary>
    internal sealed partial class SqprojProbenErzeuger
    {
        private readonly List<(string Tabelle, object[] Werte)> _zeilen = new List<(string, object[])>();
        private readonly HashSet<string> _ohne = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Die Spalten je Tabelle — in der Reihenfolge der Werte von <see cref="Zeile"/>.</summary>
        internal static readonly IReadOnlyDictionary<string, string[]> SPALTEN = new Dictionary<string, string[]>
        {
            ["XmTables"] = new[] { "UUID", "Name", "Version" },
            ["BmBuilding"] = new[] { "UUID", "ShortDesc", "ProfileGroupUUID", "YearOfConstruction", "SiteUUID", "Constructed" },
            ["BmFloor"] = new[] { "UUID", "ShortDesc", "BuildingUUID", "ElevationOfRefHeight", "Height" },
            ["BmRoom"] = new[] { "UUID", "ShortDesc", "FloorUUID", "GId", "BIMUUID", "Area", "Volume", "RoomType", "HeatingType", "Height" },
            ["SmSite"] = new[] { "UUID", "Location", "PostalCode", "Latitude", "Longitude" },
            ["BmZone"] = new[] { "UUID", "ShortDesc", "ZoneType", "ProfileGroupUUID", "Area", "Volume" },
            ["BmZoneReference"] = new[] { "UUID", "ReferenceToUUID", "ReferenceClass" },
            ["PdProfile"] = new[] { "UUID", "ShortDesc", "ProfileType", "ProfileGroupUUID" },
            ["PdProfileUsage"] = new[]
            {
                "UUID", "ProfileUsageType", "PeriodOfOperationFrom", "PeriodOfOperationTo", "HeatedFrom", "HeatedTo", "UserCount",
                "NominalRoomTemperature", "DropOfTemperatureSetback", "SupplyAirChange", "MinimumExternalAirFlowBasedOnPersons",
                "MinimumExternalAirFlowBasedOnArea", "DailyEffectiveLoadHoursOfPersons", "DailyEffectiveLoadHoursOfDevices",
                "SpecificThermalOutputPowerOfPersons", "SpecificThermalOutputOfDevices",
            },
            ["PdProfileHeating"] = new[] { "UUID" },
            ["PdProfileCooling"] = new[] { "UUID" },
            ["PdProfileVentilation"] = new[] { "UUID" },
            ["PdProfileDevice"] = new[] { "UUID", "SpecificRatedPower" },
            ["PdProfilePerson"] = new[] { "UUID", "RatedPersonOccupancyRate", "SpecificRatedDryHeatEmission" },
            ["PdProfileTimeCurve"] = new[] { "UUID", "ProfileUUID", "HourType", "OperatingModeType", "Ratio", "Temperature", "SpecificRatedAirChange" },
            ["PdProfileReference"] = new[] { "UUID", "ReferenceToUUID", "ReferenceClass" },
            ["PdProfileGroup"] = new[] { "UUID", "ShortDesc", "ProfileUsageDayType", "ProfileUsageType", "ProfileGroupType" },
            ["PdProfileGroupReference"] = new[] { "UUID", "ReferenceToUUID", "ReferenceClass" },
            ["PdProfileTaskSerial"] = new[]
            {
                "UUID", "TaskPeriodType", "TaskStartDay", "TaskEndDay", "TaskMonday", "TaskTuesday", "TaskWednesday", "TaskThursday",
                "TaskFriday", "TaskSaturday", "TaskSunday",
            },
            ["PdProfileTaskSerialReference"] = new[] { "UUID", "ReferenceToUUID", "ReferenceClass" },
            ["BmData"] = new[] { "UUID", "ReferenceUUID", "ClassValue" },
        };

        /// <summary>
        /// Die Bauteiltabellen (BA-4b) — nur geschrieben, wenn die Probe eine Bauteilzeile trägt (<see cref="MitBauteilen"/>);
        /// sonst fehlen sie wie in einer Datei ohne Hülle.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, string[]> BAUTEIL_SPALTEN = new Dictionary<string, string[]>
        {
            ["BmElement"] = new[]
            {
                "UUID", "GId", "RepositoryLevel", "ElementType", "AdjacentType", "CatalogDimUUID", "UValue", "NetArea",
                "GrossArea", "Orientation", "Slope", "ParentUUID", "RepositoryElementUUID", "GeoDesc", "Thickness",
            },
            ["BmElementWindow"] = new[] { "UUID", "GValue", "FractionOfFrame" },
            ["BmElementReference"] = new[] { "UUID", "Id", "SortNum", "ReferenceFromUUID", "ReferenceToUUID", "ReferenceType" },
            ["TcBuildingElementDimension"] = new[]
            {
                "UId", "ShortDesc", "LongDesc", "UValue", "Thickness", "InternalCoefficientOfHeatTransfer", "ExternalCoefficientOfHeatTransfer",
            },
            ["TcBuildingElementDimensionLayer"] = new[]
            {
                "UId", "DimensionUId", "SortNum", "ShortDesc", "LayerType", "MaterialType", "MaterialGroupType", "Thickness",
                "ThermalConductivity", "Density", "HeatCapacity",
            },
        };

        /// <summary>Der Platzhalter „nicht gesetzt“ der Projektdatei.</summary>
        internal const double PLATZHALTER = -987654321.99;

        /// <summary>Trägt die Probe Bauteiltabellen?</summary>
        internal bool MitBauteilen { get; set; }

        /// <summary>Eine Level-3-Hüllfläche (<c>BmElement</c>).</summary>
        internal SqprojProbenErzeuger Huellflaeche(string uuid, string gid, int elementtyp, string aufbau, double? u, double? netto, int nachbarart = 1)
        {
            MitBauteilen = true;
            return Zeile("BmElement", uuid, gid, 3, elementtyp, nachbarart, aufbau, u, netto);
        }

        /// <summary>
        /// Eine Level-3-Hüllfläche samt Lage (<c>BmElement</c>): Bruttofläche, Orientierung, Neigung (<c>Slope</c>), Wirt
        /// (<c>ParentUUID</c>) und CAD-Objekt (<c>RepositoryElementUUID</c>); fehlende Werte stehen als Platzhalter wie in der Datei.
        /// </summary>
        internal SqprojProbenErzeuger Flaeche(string uuid, int elementtyp, int nachbarart, double netto, double? brutto = null,
                                              double? orientierung = null, double? neigung = null, string eltern = null, string cad = null,
                                              string aufbau = null, double? u = null, string gid = null)
        {
            MitBauteilen = true;
            return Zeile("BmElement", uuid, gid, 3, elementtyp, nachbarart, aufbau, u ?? PLATZHALTER, netto, brutto ?? PLATZHALTER,
                         orientierung ?? PLATZHALTER, neigung ?? PLATZHALTER, eltern ?? NULLKENNUNG, cad);
        }

        /// <summary>Die Fensterwerte einer Öffnung (<c>BmElementWindow</c>): g-Wert und Rahmenanteil in % wie in der Datei.</summary>
        internal SqprojProbenErzeuger Fenster(string uuid, double? g, double? rahmenProzent)
        {
            MitBauteilen = true;
            return Zeile("BmElementWindow", uuid, g ?? PLATZHALTER, rahmenProzent ?? PLATZHALTER);
        }

        /// <summary>Die Null-Kennung „nicht gesetzt“ der Projektdatei.</summary>
        internal const string NULLKENNUNG = "{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}";

        /// <summary>Das Baujahr des Gebäudes als <c>DATE</c>-Text (<c>YearOfConstruction</c>); <c>null</c> = keines.</summary>
        internal string Baujahr { get; set; }

        /// <summary>Der Standort des Gebäudes (<c>SiteUUID</c>); <c>null</c> = keiner.</summary>
        internal string Standort { get; set; }

        /// <summary>Ein Standort (<c>SmSite</c>).</summary>
        internal SqprojProbenErzeuger Ort(string uuid, string ort, string plz, double breite, double laenge)
            => Zeile("SmSite", uuid, ort, plz, breite, laenge);

        /// <summary>Ein Raumbezug einer Hüllfläche (<c>BmElementReference</c>).</summary>
        internal SqprojProbenErzeuger Bezug(string uuid, string raum, string flaeche, int rolle, int sort = 0)
        {
            MitBauteilen = true;
            return Zeile("BmElementReference", uuid, 0, sort, raum, flaeche, rolle);
        }

        /// <summary>Ein Aufbau (<c>TcBuildingElementDimension</c>); Rsi/Rse als Widerstände wie in der Datei.</summary>
        internal SqprojProbenErzeuger Aufbau(string uid, string name, double u, double rsi = 0.13, double rse = 0.04)
        {
            MitBauteilen = true;
            return Zeile("TcBuildingElementDimension", uid, name, null, u, null, rsi, rse);
        }

        /// <summary>Eine Schicht (<c>TcBuildingElementDimensionLayer</c>); c in kJ/(kg·K) wie in der Datei.</summary>
        internal SqprojProbenErzeuger Schicht(string uid, string aufbau, int sort, string name, double d, double lambda, double rho, double cKj,
                                               bool daemmung = false)
        {
            MitBauteilen = true;
            return Zeile("TcBuildingElementDimensionLayer", uid, aufbau, sort, name, daemmung ? 3 : 0, daemmung ? 1 : 0, daemmung ? 5 : 2,
                         d, lambda, rho, cKj);
        }

        /// <summary>Die Delphi-Nullzeit — „keine Angabe“.</summary>
        internal const string NULLZEIT = "1899-12-30 00:00:00";

        /// <summary>Eine Uhrzeit als <c>DATE</c>-Text der Projektdatei.</summary>
        internal static string Uhr(int stunde) => "1899-12-30 " + stunde.ToString("00", CultureInfo.InvariantCulture) + ":00:00";

        /// <summary>Lässt eine Tabelle weg (Probe „fehlende Tabelle“).</summary>
        internal SqprojProbenErzeuger Ohne(string tabelle)
        {
            _ohne.Add(tabelle);
            return this;
        }

        /// <summary>Eine Zeile in Spaltenreihenfolge (<see cref="SPALTEN"/>); fehlende Werte am Ende sind <c>NULL</c>.</summary>
        internal SqprojProbenErzeuger Zeile(string tabelle, params object[] werte)
        {
            _zeilen.Add((tabelle, werte));
            return this;
        }

        internal SqprojProbenErzeuger Geschoss(string uuid, string name, double? lage = null, double? hoehe = null)
            => Zeile("BmFloor", uuid, name, "B1", lage, hoehe);

        /// <summary>Ein Raum; <paramref name="gid"/> ist die GUID, die der IFC-Export als GlobalId trägt; <c>BIMUUID</c> ist die Gebäude-GUID.</summary>
        internal SqprojProbenErzeuger Raum(string uuid, string name, string geschoss, string gid, double flaeche, int? raumart = null,
                                           int? heizung = null, double? hoehe = null)
            => Zeile("BmRoom", uuid, name, geschoss, gid, GEBAEUDE_GUID, flaeche, flaeche * 3.0, raumart, heizung, hoehe);

        /// <summary>Die Gebäude-GUID in <c>BIMUUID</c> jedes Raums — sie trifft keinen IFC-Raum.</summary>
        internal const string GEBAEUDE_GUID = "{99999999-8888-7777-6666-555555555555}";

        /// <summary>Die Gebäudegruppe (<c>BmBuilding.ProfileGroupUUID</c>); <c>null</c> = keine.</summary>
        internal string Gebaeudegruppe { get; set; }

        internal SqprojProbenErzeuger Zone(string uuid, string name, int typ, string gruppe, params string[] raeume)
        {
            Zeile("BmZone", uuid, name, typ, gruppe, 0.0, 0.0);
            foreach (string r in raeume) Zeile("BmZoneReference", uuid, r, "TModelRoom");
            return this;
        }

        /// <summary>Ein Nutzungsprofil (Klasse 2) an einer Zone.</summary>
        internal SqprojProbenErzeuger Nutzung(string uuid, string name, string zone, int nummer, object von, object bis, double? personen,
                                              double? temperatur, double? absenkung, double? zuluft, double? luftJePerson,
                                              double? luftJeFlaeche, double? vollPersonen, double? vollGeraete, double? personenWm2,
                                              double? geraeteWm2)
        {
            Zeile("PdProfile", uuid, name, 2, null);
            Zeile("PdProfileUsage", uuid, nummer, von, bis, NULLZEIT, NULLZEIT, personen, temperatur, absenkung, zuluft, luftJePerson,
                  luftJeFlaeche, vollPersonen, vollGeraete, personenWm2, geraeteWm2);
            return Zeile("PdProfileReference", uuid, zone, "TModelZone");
        }

        internal SqprojProbenErzeuger Gruppe(string uuid, string name, int tagesart, int? nummer = null, int art = 5)
            => Zeile("PdProfileGroup", uuid, name, tagesart, nummer, art);

        /// <summary>Ein Zeitprofil einer Klasse in einer Gruppe mit 24 Stundenwerten (<c>null</c> = Stunde ohne Wert).</summary>
        internal SqprojProbenErzeuger Zeitprofil(string uuid, string name, int klasse, string gruppe, Func<int, double?> stunde,
                                                 int betriebsart = 1, double? personen = null, double? wattJePerson = null, double? geraeteWm2 = null,
                                                 Func<int, int> betriebsartJeStunde = null)
        {
            Zeile("PdProfile", uuid, name, klasse, null);
            Zeile("PdProfileGroupReference", gruppe, uuid, "TModelProfile");
            switch (klasse)
            {
                case 4: Zeile("PdProfileDevice", uuid, geraeteWm2); break;
                case 6: Zeile("PdProfileHeating", uuid); break;
                case 7: Zeile("PdProfileCooling", uuid); break;
                case 8: Zeile("PdProfilePerson", uuid, personen, wattJePerson); break;
                case 10: Zeile("PdProfileVentilation", uuid); break;
            }
            string spalte = klasse == 6 || klasse == 7 ? "Temperature" : klasse == 10 ? "SpecificRatedAirChange" : "Ratio";
            for (int h = 1; h <= 24; h++)
            {
                double? w = stunde(h - 1);
                Zeile("PdProfileTimeCurve", uuid + "-" + h.ToString("00", CultureInfo.InvariantCulture), uuid, h, betriebsartJeStunde?.Invoke(h - 1) ?? betriebsart,
                      spalte == "Ratio" ? w : null, spalte == "Temperature" ? w : null, spalte == "SpecificRatedAirChange" ? w : null);
            }
            return this;
        }

        /// <summary>Ein Abschnitt (<c>PdProfileTaskSerial</c>) eines Zeitprofils; Wochentage Mo … So als Zeichenkette „1100000“.</summary>
        internal SqprojProbenErzeuger Abschnitt(string uuid, string profil, int beginn, int ende, string wochentage = "0000000", int art = 1)
        {
            var werte = new List<object> { uuid, art, beginn, ende };
            werte.AddRange(wochentage.Select(c => (object)(c == '1' ? 1 : 0)));
            Zeile("PdProfileTaskSerial", werte.ToArray());
            return Zeile("PdProfileTaskSerialReference", uuid, profil, "TModelProfile");
        }

        /// <summary><b>Schreibt die Datei</b> (neu) — Register zuerst, dann die Tabellen in fester Reihenfolge.</summary>
        internal string Schreiben(string pfad)
        {
            if (File.Exists(pfad)) File.Delete(pfad);
            var b = new SqliteConnectionStringBuilder { DataSource = pfad, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false };
            using (var c = new SqliteConnection(b.ToString()))
            {
                c.Open();
                using (SqliteTransaction t = c.BeginTransaction())
                {
                    foreach (KeyValuePair<string, string[]> tab in SPALTEN.Concat(MitBauteilen ? BAUTEIL_SPALTEN : Enumerable.Empty<KeyValuePair<string, string[]>>())
                                                                          .Concat(_zeilen.Any(z => JOURNAL_SPALTEN.ContainsKey(z.Tabelle)) ? JOURNAL_SPALTEN
                                                                                  : Enumerable.Empty<KeyValuePair<string, string[]>>())
                                                                          .Where(p => !_ohne.Contains(p.Key)))
                    {
                        using (SqliteCommand k = c.CreateCommand())
                        {
                            k.Transaction = t;
                            k.CommandText = "CREATE TABLE " + tab.Key + " (" + string.Join(", ", tab.Value) + ")";
                            k.ExecuteNonQuery();
                        }
                        if (tab.Key != "XmTables")
                            Einfuegen(c, t, "XmTables", new object[] { "X-" + tab.Key, tab.Key, tab.Key.StartsWith("Bm", StringComparison.Ordinal) ? "16.7" : "15.1" });
                    }
                    if (!_ohne.Contains("BmBuilding")) Einfuegen(c, t, "BmBuilding", new object[] { "B1", "Probegebäude", Gebaeudegruppe, Baujahr, Standort });
                    foreach ((string tabelle, object[] werte) in _zeilen.Where(z => !_ohne.Contains(z.Tabelle)))
                        Einfuegen(c, t, tabelle, werte);
                    GeometrieNachtragen(c, t);
                    t.Commit();
                }
            }
            return pfad;
        }

        private static void Einfuegen(SqliteConnection c, SqliteTransaction t, string tabelle, object[] werte)
        {
            string[] spalten = SPALTEN.TryGetValue(tabelle, out string[] sp) ? sp
                             : JOURNAL_SPALTEN.TryGetValue(tabelle, out string[] js) ? js : BAUTEIL_SPALTEN[tabelle];
            using (SqliteCommand k = c.CreateCommand())
            {
                k.Transaction = t;
                k.CommandText = "INSERT INTO " + tabelle + " (" + string.Join(", ", spalten) + ") VALUES (" +
                                string.Join(", ", spalten.Select((_, i) => "$p" + i.ToString(CultureInfo.InvariantCulture))) + ")";
                for (int i = 0; i < spalten.Length; i++)
                    k.Parameters.AddWithValue("$p" + i.ToString(CultureInfo.InvariantCulture), i < werte.Length && werte[i] != null ? werte[i] : DBNull.Value);
                k.ExecuteNonQuery();
            }
        }

        // ==================================================================
        //  Die Standardprobe (Proben 33–36)
        // ==================================================================

        /// <summary>Die GUID (<c>GId</c>) des Raums D — trifft den IFC-Raum „Raum X“ über die GlobalId.</summary>
        internal const string GID_D = "{0A1B2C3D-4E5F-6071-8293-A4B5C6D7E8F9}";

        /// <summary>
        /// <b>Die Standardprobe nach dem Regelfall</b> (Befund Kapitel 6: Typ 5 und Typ 6 decken je alle Räume ab): Geschosse EG
        /// und OG; Räume A, B (EG), C, D, E (OG) — D nur über die GId abgleichbar, E ohne Gegenstück, C mit abweichender
        /// Raumart; Simulationszonen (Typ 6) „Simulation EG“ (A, B; Gruppe Tagesart 4 mit Heizen, Personen, Lüftung, Geräten,
        /// Beleuchtung) und „Simulation OG“ (C, D, E; Gruppe Tagesart 5, Profilnummer 71, Heizen mit Betriebsart 2 in der Nacht),
        /// Nutzungszonen (Typ 5) „Nutzung EG“ (A, B; Profil 1) und „Nutzung OG“ (C, D, E; Profil 71), „Leer“ (Typ 6 ohne Raum,
        /// Gruppe mit unbekannter Tagesart 9 und Kühlen), je eine Zone der Typen 2 und 10 und die Gebäudegruppe (Typ 4).
        /// </summary>
        internal static SqprojProbenErzeuger Standard()
        {
            var e = new SqprojProbenErzeuger { Gebaeudegruppe = "GB" }
                .Geschoss("F1", "EG").Geschoss("F2", "OG")
                .Raum("R1", "Raum A", "F1", "{11111111-1111-1111-1111-111111111111}", 20.0, 14)
                .Raum("R2", "Raum B", "F1", null, 30.0)
                .Raum("R3", "Raum C", "F2", null, 40.0, 1)
                .Raum("R4", "Raum D", "F2", GID_D, 10.0)
                .Raum("R5", "Raum E", "F2", null, 10.0)
                .Zone("Z1", "Simulation EG", 6, "G1", "R1", "R2")
                .Zone("Z7", "Simulation OG", 6, "G3", "R3", "R4", "R5")
                .Zone("Z2", "Nutzung EG", 5, null, "R2", "R1")
                .Zone("Z3", "Nutzung OG", 5, null, "R3", "R4", "R5")
                .Zone("Z4", "Leer", 6, "G2")
                .Zone("Z5", "Darstellung", 2, null)
                .Zone("Z6", "Sonstige", 10, null)
                .Nutzung("U1", "Profil Buero", "Z2", 1, Uhr(7), Uhr(18), 2.0, 21.0, 4.0, null, null, 4.0, 5.5, 11.0, 5.0, 7.0)
                .Nutzung("U3", "Profil Wohnen", "Z3", 71, NULLZEIT, NULLZEIT, null, 20.0, null, null, null, null, null, null, null, null)
                .Gruppe("G1", "Gruppe EG", 4, 1)
                .Zeitprofil("H1", "Heizen EG", 6, "G1", h => h == 12 ? 40.0 : h >= 6 && h < 18 ? 21.0 : 17.0, betriebsart: 1)
                .Zeitprofil("P1", "Personen EG", 8, "G1", h => h >= 8 && h < 17 ? 1.0 : 0.0, betriebsart: 2, personen: 4.0, wattJePerson: 80.0)
                .Zeitprofil("V1", "Lueftung EG", 10, "G1", _ => 2.0, betriebsart: 1)
                .Zeitprofil("D1", "Geraete EG", 4, "G1", _ => 0.5, betriebsart: 1, geraeteWm2: 10.0)
                .Zeitprofil("L1", "Licht EG", 9, "G1", _ => 1.0)
                .Abschnitt("S1", "H1", 1, 365)
                .Abschnitt("S2", "P1", 1, 365)
                .Abschnitt("S3", "P1", 182, 243, "1100000")
                .Abschnitt("S4", "V1", 300, 60, "0000000", art: 4)
                .Gruppe("G3", "Gruppe OG", 5, 71)
                .Zeitprofil("H3", "Heizen OG", 6, "G3", h => h == 15 ? 16.0 : h < 9 || h >= 21 ? 18.0 : 20.0,
                            betriebsartJeStunde: h => h < 9 || h >= 21 ? 2 : 1)
                .Gruppe("G2", "Gruppe Leer", 9)
                .Zeitprofil("K2", "Kuehlen Leer", 7, "G2", _ => 26.0)
                .Gruppe("GB", "Gruppe Gebaeude", 6, null, 4);
            return e;
        }

        /// <summary>
        /// <b>Beide Zonierungen</b> (E87, F1): Räume A, B (EG) und C (OG); DIN-V-18599-Zonen (Typ 5) „DIN Büro“ (A, B; Profil 1)
        /// und „DIN Wohnen“ (C; Profil 71), eine Simulationszone (Typ 6) „Simulation Haus“ (A, B, C; Gruppe mit Heizen als
        /// Ganglinie, ohne Profilnummer) — jeder Raum liegt in beiden Zonierungen; dazu eine Zone vom Typ 2.
        /// </summary>
        internal static SqprojProbenErzeuger ZweiZonierungen()
            => new SqprojProbenErzeuger()
                .Geschoss("F1", "EG").Geschoss("F2", "OG")
                .Raum("R1", "Raum A", "F1", "{11111111-1111-1111-1111-111111111111}", 20.0)
                .Raum("R2", "Raum B", "F1", null, 30.0)
                .Raum("R3", "Raum C", "F2", null, 40.0)
                .Zone("Z1", "DIN Büro", 5, null, "R1", "R2")
                .Zone("Z2", "DIN Wohnen", 5, null, "R3")
                .Zone("Z3", "Simulation Haus", 6, "G1", "R1", "R2", "R3")
                .Zone("Z4", "Darstellung", 2, null)
                .Nutzung("U1", "Profil Buero", "Z1", 1, Uhr(7), Uhr(18), null, 21.0, null, null, null, null, null, null, null, null)
                .Nutzung("U2", "Profil Wohnen", "Z2", 71, NULLZEIT, NULLZEIT, null, 20.0, null, null, null, null, null, null, null, null)
                .Gruppe("G1", "Gruppe Haus", 6)
                .Zeitprofil("H1", "Heizen Haus", 6, "G1", h => h >= 6 && h < 22 ? 21.0 : 17.0);

        /// <summary>
        /// <b>Nur DIN-V-18599-Zonen</b> (wie das Wohngebäude EH55, E87): Räume A, B (EG), C (OG) in einer Zone vom Typ 5
        /// „Wohnen“ (Profil 71), keine Simulationszone; dazu je eine Zone der Typen 2 und 10.
        /// </summary>
        internal static SqprojProbenErzeuger NurDinZonen()
            => new SqprojProbenErzeuger()
                .Geschoss("F1", "EG").Geschoss("F2", "OG")
                .Raum("R1", "Raum A", "F1", "{11111111-1111-1111-1111-111111111111}", 20.0)
                .Raum("R2", "Raum B", "F1", null, 30.0)
                .Raum("R3", "Raum C", "F2", null, 40.0)
                .Zone("Z1", "Wohnen", 5, null, "R1", "R2", "R3")
                .Zone("Z2", "Darstellung", 2, null)
                .Zone("Z3", "Sonstige", 10, null)
                .Nutzung("U1", "Profil Wohnen", "Z1", 71, NULLZEIT, NULLZEIT, null, 20.0, null, null, null, null, null, null, null, null);

        /// <summary>
        /// <b>Die Zonierung unvollständig</b> (Zwischenstand einer Projektdatei): nur eine Simulationszone mit einem Teil der
        /// Räume, eine Nutzungszone ohne Raumbezug.
        /// </summary>
        internal static SqprojProbenErzeuger Unvollstaendig()
            => new SqprojProbenErzeuger()
                .Geschoss("F1", "EG").Geschoss("F2", "OG")
                .Raum("R1", "Raum A", "F1", null, 20.0).Raum("R2", "Raum B", "F1", null, 30.0).Raum("R3", "Raum C", "F2", null, 40.0)
                .Zone("Z1", "Simulation", 6, "G1", "R1")
                .Zone("Z2", "Nutzung", 5, null)
                .Nutzung("U1", "Profil Buero", "Z2", 1, Uhr(7), Uhr(18), null, 21.0, null, null, null, null, null, null, null, null)
                .Gruppe("G1", "Gruppe", 6, 1)
                .Zeitprofil("H1", "Heizen", 6, "G1", h => h >= 6 && h < 18 ? 21.0 : 17.0);

        /// <summary>
        /// <b>Das Zonenhaus</b> zur IFC-Probe <c>ifc4_zonen.ifc</c> (Lager im Keller; Wohnen, Küche im EG; Schlafen, Bad,
        /// Abstellraum im OG): Simulationszonen EG (Ganglinie Heizen und Personen, Profil 71), OG (Profil 1) und Keller
        /// (Profil 20: mit Katalog das Muster Lager, ohne Katalog keine Nutzung), dazu die Nutzungszonen. Gemeinsam für den
        /// Datenbanktest und die Dialogprobe 37.
        /// </summary>
        internal static SqprojProbenErzeuger Zonenhaus()
            => new SqprojProbenErzeuger()
                .Geschoss("FK", "Kellergeschoss").Geschoss("FE", "Erdgeschoss").Geschoss("FO", "Obergeschoss")
                .Raum("R1", "Lager", "FK", null, 20.0).Raum("R2", "Wohnen", "FE", null, 30.0).Raum("R3", "Küche", "FE", null, 10.0)
                .Raum("R4", "Schlafen", "FO", null, 20.0).Raum("R5", "Bad", "FO", null, 8.0).Raum("R6", "Abstellraum", "FO", null, 4.0)
                .Zone("Z1", "Simulation EG", 6, "G1", "R2", "R3")
                .Zone("Z2", "Simulation OG", 6, "G2", "R4", "R5", "R6")
                .Zone("Z3", "Keller", 6, null, "R1")
                .Zone("Z4", "Nutzung EG", 5, null, "R2", "R3")
                .Zone("Z5", "Nutzung OG", 5, null, "R4", "R5", "R6")
                .Zone("Z6", "Nutzung Keller", 5, null, "R1")
                .Nutzung("U4", "Profil Wohnen", "Z4", 71, NULLZEIT, NULLZEIT, null, 20.0, null, 0.5,
                         null, null, null, null, null, null)
                .Nutzung("U5", "Profil Buero", "Z5", 1, Uhr(7), Uhr(18), null, 21.0, 4.0, null,
                         null, null, null, null, null, null)
                .Nutzung("U6", "Profil Lager", "Z6", 20, NULLZEIT, NULLZEIT, null, null, null, null,
                         null, null, null, null, null, null)
                .Gruppe("G1", "Gruppe EG", 6, 71)
                .Zeitprofil("H1", "Heizen EG", 6, "G1", h => h >= 6 && h < 22 ? 20.0 : 17.0)
                .Zeitprofil("P1", "Personen EG", 8, "G1", _ => 1.0, personen: 3.0, wattJePerson: 80.0)
                .Gruppe("G2", "Gruppe OG", 4, 1);

        /// <summary>
        /// Schreibt die IFC-Probe <c>ifc4_zonen.ifc</c> als HottCAD-Export (<c>ObjectType</c> <c>TModelBuilding</c> am Gebäude)
        /// unter einen temporären Pfad (der Aufrufer löscht ihn).
        /// </summary>
        internal static string HottcadZonenhaus(string wurzel)
        {
            string quelle = File.ReadAllText(Path.Combine(wurzel, "Referenzlaeufe", "Importproben", "ifc4_zonen.ifc"));
            const string ALT = "'Zonenhaus',$,$,";
            if (!quelle.Contains(ALT, StringComparison.Ordinal)) throw new InvalidOperationException("ifc4_zonen.ifc: IfcBuilding nicht gefunden.");
            string ziel = Path.Combine(Path.GetDirectoryName(TempPfad("ifc")), "zonenhaus-" + Guid.NewGuid().ToString("N") + ".ifc");
            File.WriteAllText(ziel, quelle.Replace(ALT, "'Zonenhaus',$,'TModelBuilding',", StringComparison.Ordinal));
            return ziel;
        }

        /// <summary>Ein temporärer Pfad für eine Probe (der Aufrufer löscht ihn).</summary>
        internal static string TempPfad(string name)
        {
            string ordner = Path.Combine(Path.GetTempPath(), "epos-sqproj-test");
            Directory.CreateDirectory(ordner);
            return Path.Combine(ordner, name + "-" + Guid.NewGuid().ToString("N") + ".sqproj");
        }
    }
}
