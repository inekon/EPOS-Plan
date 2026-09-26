using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Vokabular von gbXML</b> — die Aufzählungswerte und Einheitennamen des Schemas, die
    /// Leser (<see cref="GbxmlLeser"/>, Stufe G4c) und Schreiber (<c>GbxmlSchreiber</c>, Stufe G7a)
    /// teilen, an EINER Stelle. Die Werte folgen der Schemakopie
    /// <c>GreenBuildingXML_Ver8.01.xsd</c> (Datenaustauschkonzept 3.2, 5.2; Befund R 1.5), die nur lokal
    /// beiliegt (<c>Referenzlaeufe/Schemakopien/</c>, D17).
    ///
    /// <para><b>Die Konstanten tragen den Wert als Namen</b> (<see cref="WPerMeterK"/> ist
    /// <c>"WPerMeterK"</c>): So steht am Schreibort kein Literal, und eine Suche nach dem Schemawert
    /// findet die Konstante.</para>
    ///
    /// <para><b>Die Tabellen des Lesers</b> (<see cref="Flaechenarten"/>, <see cref="UebergangeneFlaechenarten"/>,
    /// <see cref="Oeffnungsarten"/>) sind die Leserichtung (Datenaustauschkonzept 3.4, 3.5); die
    /// Gegenrichtung steht in <c>GbxmlUmkehrung</c> und wird am Leser gemessen.</para>
    /// </summary>
    internal static class GbxmlVokabular
    {
        // ------------------------------------------------------------------
        //  Flächenarten (surfaceTypeEnum)
        // ------------------------------------------------------------------

        /// <summary>Außenwand an Außenluft.</summary>
        public const string ExteriorWall = "ExteriorWall";
        /// <summary>Außenwand an Erdreich.</summary>
        public const string UndergroundWall = "UndergroundWall";
        /// <summary>Wand zwischen Räumen.</summary>
        public const string InteriorWall = "InteriorWall";
        /// <summary>Dach an Außenluft.</summary>
        public const string Roof = "Roof";
        /// <summary>Decke zwischen Räumen, aus Sicht des unteren Raums.</summary>
        public const string Ceiling = "Ceiling";
        /// <summary>Decke zwischen Räumen, aus Sicht des oberen Raums (sein Boden).</summary>
        public const string InteriorFloor = "InteriorFloor";
        /// <summary>Decke an Erdreich.</summary>
        public const string UndergroundCeiling = "UndergroundCeiling";
        /// <summary>Bodenplatte auf Erdreich.</summary>
        public const string SlabOnGrade = "SlabOnGrade";
        /// <summary>Bodenplatte im Erdreich.</summary>
        public const string UndergroundSlab = "UndergroundSlab";
        /// <summary>Boden über Außenluft.</summary>
        public const string ExposedFloor = "ExposedFloor";
        /// <summary>Aufgeständerter Boden.</summary>
        public const string RaisedFloor = "RaisedFloor";
        /// <summary>In die Hülle eingebettete Stütze.</summary>
        public const string EmbeddedColumn = "EmbeddedColumn";
        /// <summary>Verschattungsfläche.</summary>
        public const string Shade = "Shade";
        /// <summary>Luftgrenze (auch als Öffnungsart).</summary>
        public const string Air = "Air";
        /// <summary>Freistehende Stütze im Raum.</summary>
        public const string FreestandingColumn = "FreestandingColumn";

        // ------------------------------------------------------------------
        //  Öffnungsarten (openingTypeEnum)
        // ------------------------------------------------------------------

        /// <summary>Festverglasung.</summary>
        public const string FixedWindow = "FixedWindow";
        /// <summary>Öffenbares Fenster.</summary>
        public const string OperableWindow = "OperableWindow";
        /// <summary>Festes Oberlicht.</summary>
        public const string FixedSkylight = "FixedSkylight";
        /// <summary>Öffenbares Oberlicht.</summary>
        public const string OperableSkylight = "OperableSkylight";
        /// <summary>Schiebetür.</summary>
        public const string SlidingDoor = "SlidingDoor";
        /// <summary>Drehtür.</summary>
        public const string NonSlidingDoor = "NonSlidingDoor";

        // ------------------------------------------------------------------
        //  Nutzungszustand eines Raums (conditionTypeEnum)
        // ------------------------------------------------------------------

        /// <summary>Beheizt.</summary>
        public const string Heated = "Heated";
        /// <summary>Gekühlt.</summary>
        public const string Cooled = "Cooled";
        /// <summary>Beheizt und gekühlt.</summary>
        public const string HeatedAndCooled = "HeatedAndCooled";
        /// <summary>Unbeheizt.</summary>
        public const string Unconditioned = "Unconditioned";
        /// <summary>Belüftet, unbeheizt.</summary>
        public const string Vented = "Vented";
        /// <summary>Nur natürlich belüftet, unbeheizt.</summary>
        public const string NaturallyVentedOnly = "NaturallyVentedOnly";

        /// <summary>Die sechs Werte von <c>conditionTypeEnum</c>.</summary>
        internal static readonly IReadOnlyList<string> Nutzungszustaende = new[]
        {
            Heated, Cooled, HeatedAndCooled, Unconditioned, Vented, NaturallyVentedOnly,
        };

        // ------------------------------------------------------------------
        //  Gebäudearten (buildingTypeEnum)
        // ------------------------------------------------------------------

        /// <summary>Die Gebäudeart, wenn keine Regel trifft.</summary>
        public const string Unknown = "Unknown";

        /// <summary>Die 35 Werte von <c>buildingTypeEnum</c> in Schemareihenfolge.</summary>
        internal static readonly IReadOnlyList<string> Gebaeudearten = new[]
        {
            "AutomotiveFacility", "ConventionCenter", "Courthouse", "DataCenter", "DiningBarLoungeOrLeisure",
            "DiningCafeteriaFastFood", "DiningFamily", "Dormitory", "ExerciseCenter", "FireStation",
            "Gymnasium", "HospitalOrHealthcare", "Hotel", "Library", "Manufacturing", "Motel",
            "MotionPictureTheatre", "MultiFamily", "Museum", "Office", "ParkingGarage", "Penitentiary",
            "PerformingArtsTheater", "PoliceStation", "PostOffice", "ReligiousBuilding", "Retail",
            "SchoolOrUniversity", "SingleFamily", "SportsArena", "TownHall", "Transportation", Unknown,
            "Warehouse", "Workshop",
        };

        // ------------------------------------------------------------------
        //  Einheiten (Wurzelattribute und unit-Attribute)
        // ------------------------------------------------------------------

        /// <summary>Grad Celsius (<c>temperatureUnitEnum</c>).</summary>
        public const string Celsius = "C";
        /// <summary>Meter (<c>lengthUnitEnum</c>).</summary>
        public const string Meters = "Meters";
        /// <summary>Quadratmeter (<c>areaUnitEnum</c>).</summary>
        public const string SquareMeters = "SquareMeters";
        /// <summary>Kubikmeter (<c>volumeUnitEnum</c>).</summary>
        public const string CubicMeters = "CubicMeters";
        /// <summary>Wärmeleitfähigkeit W/(mK) (<c>conductivityUnitEnum</c>).</summary>
        public const string WPerMeterK = "WPerMeterK";
        /// <summary>Rohdichte kg/m³ (<c>densityUnitEnum</c>).</summary>
        public const string KgPerCubicM = "KgPerCubicM";
        /// <summary>Rohdichte g/cm³ (<c>densityUnitEnum</c>); 1 g/cm³ = 1000 kg/m³.</summary>
        public const string GramsPerCubicCm = "GramsPerCubicCm";
        /// <summary>Spezifische Wärmekapazität J/(kgK) (<c>specificHeatUnitEnum</c>).</summary>
        public const string JPerKgK = "JPerKgK";
        /// <summary>U-Wert W/(m²K) (<c>uValueUnitEnum</c>).</summary>
        public const string WPerSquareMeterK = "WPerSquareMeterK";
        /// <summary>Wärmedurchlasswiderstand m²K/W (<c>resistanceUnitEnum</c>).</summary>
        public const string SquareMeterKPerW = "SquareMeterKPerW";
        /// <summary>Anteil [–] (<c>unitlessUnitEnum</c>).</summary>
        public const string Fraction = "Fraction";
        /// <summary>Personenzahl (<c>peopleNumberUnitEnum</c>).</summary>
        public const string NumberOfPeople = "NumberOfPeople";
        /// <summary>Fläche je Person m² (<c>peopleNumberUnitEnum</c>).</summary>
        public const string SquareMPerPerson = "SquareMPerPerson";
        /// <summary>Leistung je Fläche W/m² (<c>powerPerAreaUnitEnum</c>).</summary>
        public const string WattPerSquareMeter = "WattPerSquareMeter";

        // ------------------------------------------------------------------
        //  Mengen
        // ------------------------------------------------------------------

        /// <summary>
        /// Die kleinste Zahl an <c>Surface</c> je <c>Campus</c> (<c>minOccurs="4"</c>,
        /// Datenaustauschkonzept 5.2) — mit weniger ist die Datei schemawidrig.
        /// </summary>
        public const int MINDESTZAHL_FLAECHEN = 4;

        // ------------------------------------------------------------------
        //  Die Tabellen des Lesers
        // ------------------------------------------------------------------

        /// <summary>Flächenart → Bauteilart und Randbedingung aus dem Typ (Datenaustauschkonzept 3.5).</summary>
        internal static readonly IReadOnlyDictionary<string, (Bauteilart Art, Randbedingung Rand)> Flaechenarten =
            new Dictionary<string, (Bauteilart, Randbedingung)>(StringComparer.Ordinal)
            {
                [ExteriorWall] = (Bauteilart.Aussenwand, Randbedingung.Aussenluft),
                [UndergroundWall] = (Bauteilart.Aussenwand, Randbedingung.Erdreich),
                [InteriorWall] = (Bauteilart.Innenwand, Randbedingung.Innen),
                [Roof] = (Bauteilart.Dach, Randbedingung.Aussenluft),
                [Ceiling] = (Bauteilart.Decke, Randbedingung.Innen),
                [InteriorFloor] = (Bauteilart.Decke, Randbedingung.Innen),
                [UndergroundCeiling] = (Bauteilart.Decke, Randbedingung.Erdreich),
                [SlabOnGrade] = (Bauteilart.Bodenplatte, Randbedingung.Erdreich),
                [UndergroundSlab] = (Bauteilart.Bodenplatte, Randbedingung.Erdreich),
                [ExposedFloor] = (Bauteilart.Bodenplatte, Randbedingung.Aussenluft),
                [RaisedFloor] = (Bauteilart.Bodenplatte, Randbedingung.Aussenluft),
                // In die Hülle eingebettete Stütze: sonstige Fläche, nie Außenwand (3.5).
                [EmbeddedColumn] = (Bauteilart.Sonstiges, Randbedingung.Aussenluft),
            };

        /// <summary>Flächenarten ohne Fläche für die Hülle: Verschattung, Luftgrenze, freistehende Stütze im Raum.</summary>
        internal static readonly IReadOnlySet<string> UebergangeneFlaechenarten = new HashSet<string>(StringComparer.Ordinal)
        {
            Shade, Air, FreestandingColumn,
        };

        /// <summary>Öffnungsart → Bauteilart (3.4).</summary>
        internal static readonly IReadOnlyDictionary<string, Bauteilart> Oeffnungsarten =
            new Dictionary<string, Bauteilart>(StringComparer.Ordinal)
            {
                [FixedWindow] = Bauteilart.Fenster,
                [OperableWindow] = Bauteilart.Fenster,
                [FixedSkylight] = Bauteilart.Fenster,
                [OperableSkylight] = Bauteilart.Fenster,
                [SlidingDoor] = Bauteilart.Tuer,
                [NonSlidingDoor] = Bauteilart.Tuer,
            };
    }
}
