using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Wie ein Format ein Inventardatum trägt (Konzept Datenaustausch 3, E138 F5).</summary>
    internal enum Austauschweg
    {
        /// <summary>Ein Element des Formats; der Export schreibt es, der Import liest es.</summary>
        BeideRichtungen = 0,
        /// <summary>Nur der Import liest es; der Export schreibt es nicht.</summary>
        NurGelesen = 1,
        /// <summary>Nur der Export schreibt es; der Import liest es nicht.</summary>
        NurGeschrieben = 2,
        /// <summary>Kein Element im Format (oder kein Weg im Code) — Verlust bzw. Rückfall nach Konzept 3.3.</summary>
        OhneElement = 3,
    }

    /// <summary>Ein Datum des Inventars (Konzept Datenaustausch, Abschnitt 2) mit seiner Einstufung je Format.</summary>
    /// <param name="Nr">Die Kennung des Konzepts (A1 … G4).</param>
    /// <param name="Datum">Die Bezeichnung.</param>
    /// <param name="Gbxml">Der Weg in gbXML 6.01.</param>
    /// <param name="Elemente">Die gbXML-Elemente bzw. -Attribute (lokale Namen), die das Datum tragen; leer ohne Element.</param>
    internal sealed record Inventardatum(string Nr, string Datum, Austauschweg Gbxml, params string[] Elemente);

    /// <summary>
    /// <b>Das Inventar der simulationsrelevanten Daten als Tafel</b> (Konzept Datenaustausch, Abschnitt 2; E138 F5) mit der
    /// Einstufung je Format. Die Welle DA-2 stuft gbXML ein, so wie der Code schreibt und liest; DA-3 (IFC) und DA-4 (Rest)
    /// knüpfen hier an. Die Wache <c>DatenaustauschInventarWacheTests</c> hält Zählung, Kennungen und die Deckung mit
    /// Schreiber und Leser.
    /// </summary>
    internal static class DatenaustauschInventar
    {
        private const Austauschweg B = Austauschweg.BeideRichtungen;
        private const Austauschweg L = Austauschweg.NurGelesen;
        private const Austauschweg S = Austauschweg.NurGeschrieben;
        private const Austauschweg O = Austauschweg.OhneElement;

        /// <summary>Die 71 Daten in der Reihenfolge des Konzepts.</summary>
        internal static readonly IReadOnlyList<Inventardatum> Alle = new[]
        {
            new Inventardatum("A1", "Breite, Länge", L, "Latitude", "Longitude"),
            new Inventardatum("A2", "Klimaregion, Klimazone", O),
            new Inventardatum("A3", "Höhe über NN", O),
            new Inventardatum("A4", "Nordrichtung", B, "CADModelAzimuth"),
            new Inventardatum("A5", "Baujahr, Baualtersklasse", O),
            new Inventardatum("A6", "Gebäudeart", B, "buildingType"),
            new Inventardatum("A7", "Bauweise, Masseanteil", O),
            new Inventardatum("A8", "Wochenendtage", O),
            new Inventardatum("A9", "Feiertagsland", O),
            new Inventardatum("A10", "Ferien (Perioden FERIEN)", B, "YearSchedule", "BeginDate", "EndDate"),

            new Inventardatum("B1", "Bezeichner", B, "Name"),
            new Inventardatum("B2", "Nutzfläche", B, "Area"),
            new Inventardatum("B3", "Volumen, Raumhöhe", B, "Volume"),
            new Inventardatum("B4", "beheizt", B, "conditionType"),
            new Inventardatum("B5", "Nutzungsprofil", L, "spaceType"),
            new Inventardatum("B6", "Heizsollwert Tag", B, "DesignHeatT"),
            new Inventardatum("B7", "Heizsollwert Nacht", B, "heatSchedIdRef"),
            new Inventardatum("B8", "Sollwert Wochenende, Ferien", B, "heatSchedIdRef"),
            new Inventardatum("B9", "Kühlsollwert Tag, Nacht", B, "DesignCoolT", "coolSchedIdRef"),
            new Inventardatum("B10", "Höchsttemperatur", O),
            new Inventardatum("B11", "Heiz- und Kühlleistung höchstens", O),
            new Inventardatum("B12", "Infiltrationsluftwechsel", B, "AirChangesPerHour"),
            new Inventardatum("B13", "Nutzerluftwechsel", B, "AirChangesPerHour", "airChangesSchedIdRef"),
            new Inventardatum("B14", "Außenluft je Person und je Fläche", B, "OAFlowPerPerson"),
            new Inventardatum("B15", "Belegung", B, "PeopleNumber"),
            new Inventardatum("B16", "Wärmeabgabe je Person", B, "PeopleHeatGain"),
            new Inventardatum("B17", "Geräteleistung", B, "EquipPowerPerArea", "equipmentScheduleIdRef"),
            new Inventardatum("B18", "Beleuchtungsleistung", B, "LightPowerPerArea", "lightScheduleIdRef"),
            new Inventardatum("B19", "Strahlungsanteil der Heizung", O),
            new Inventardatum("B20", "Vorheizzeit", O),
            new Inventardatum("B21", "Übergabe Heizen je Zone", O),
            new Inventardatum("B22", "Übergabe Kühlen je Zone", O),

            new Inventardatum("C1", "Kalender Heizsollwert", B, "Schedule", "heatSchedIdRef"),
            new Inventardatum("C2", "Kalender Kühlsollwert", B, "Schedule", "coolSchedIdRef"),
            new Inventardatum("C3", "Kalender Lüftung", B, "Schedule", "airChangesSchedIdRef"),
            new Inventardatum("C4", "Kalender Geräte", B, "Schedule", "equipmentScheduleIdRef"),
            new Inventardatum("C5", "Kalender Personen", B, "Schedule", "peopleScheduleIdRef"),
            new Inventardatum("C6", "Feiertage", B, "dayType"),
            new Inventardatum("C7", "Betriebspause, Heiz- und Kühlperiode", B, "YearSchedule", "BeginDate", "EndDate"),
            new Inventardatum("C8", "Zeilenbild", O),
            new Inventardatum("C9", "benannte Wochen", O),
            new Inventardatum("C10", "Beleuchtungszeitplan", O),

            new Inventardatum("D1", "Bauteilart", B, "surfaceType"),
            new Inventardatum("D2", "Fläche", B, "RectangularGeometry"),
            new Inventardatum("D3", "Neigung", B, "Tilt"),
            new Inventardatum("D4", "Azimut", B, "Azimuth"),
            new Inventardatum("D5", "Randbedingung", B, "AdjacentSpaceId"),
            new Inventardatum("D6", "Nachbarzone, Trennfläche", B, "AdjacentSpaceId"),
            new Inventardatum("D7", "U-Wert", B, "U-value"),
            new Inventardatum("D8", "ψ·L", O),

            new Inventardatum("E1", "Schichtfolge, Dicke", B, "LayerId", "Thickness"),
            new Inventardatum("E2", "λ", B, "Conductivity"),
            new Inventardatum("E3", "ρ", B, "Density"),
            new Inventardatum("E4", "c", B, "SpecificHeat"),
            new Inventardatum("E5", "Luftschicht, R der Schicht", B, "R-value"),
            new Inventardatum("E6", "inhomogene Abschnitte", O),
            new Inventardatum("E7", "Übergangswiderstände", O),

            new Inventardatum("F1", "U_w", B, "U-value"),
            new Inventardatum("F2", "g-Wert", B, "SolarHeatGainCoeff"),
            new Inventardatum("F3", "Rahmenanteil", O),
            new Inventardatum("F4", "Verschattung, Sonnenschutz", O),
            new Inventardatum("F5", "Lichttransmission", O),
            new Inventardatum("F6", "winkelabhängiger g-Wert", L, "SolarHeatGainCoeff"),
            new Inventardatum("F7", "Emissionsgrad der Verglasung", O),
            new Inventardatum("F8", "Absorptionsgrad opaker Außenflächen (Konstante, durchgereicht)", B, "Absorptance"),
            new Inventardatum("F9", "langwelliger Emissionsgrad außen (Konstante, durchgereicht)", B, "Absorptance"),
            new Inventardatum("F10", "Albedo der Umgebung", O),

            new Inventardatum("G1", "Luftströme zwischen Zonen", O),
            new Inventardatum("G2", "Bodenplatte und Randbedingung", B, "surfaceType"),
            new Inventardatum("G3", "Umfang P", O),
            new Inventardatum("G4", "wirksamer U-Wert Erdreich, Kellertemperatur", O),
        };

        /// <summary>Die Zahl je Gruppe des Konzepts (A 10, B 22, C 10, D 8, E 7, F 10, G 4).</summary>
        internal static IReadOnlyDictionary<char, int> JeGruppe => Alle.GroupBy(d => d.Nr[0]).ToDictionary(g => g.Key, g => g.Count());
    }
}
