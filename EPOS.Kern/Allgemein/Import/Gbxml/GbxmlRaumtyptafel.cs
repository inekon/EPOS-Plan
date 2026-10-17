using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Raumtyptafel von gbXML</b> (Welle DA-5, Konzept Datenaustausch Simulationsdaten 4.3, E135): der Wert des Attributs
    /// <c>Space/@spaceType</c> (Aufzählung <c>spaceTypeEnum</c> des Schemas 6.01) → Nummer des Profils der DIN/TS 18599-10:2025-10,
    /// Tabelle 6. Die Tafel trägt nur Namen und Nummern, keine Kennwerte. Der Import löst die Nummer über den Katalog auf
    /// (<see cref="Raumnutzungsvorbelegung.DinTreffer"/>) — wie eine DIN-Nummer der Datei (Rangstufe 1).
    ///
    /// <para><b>Mehrdeutig</b> (<see cref="MEHRDEUTIG"/>) heißt: Der Typ passt auf mehr als ein Profil (etwa „Lobby“ auf
    /// Verkehrsfläche oder Foyer, „ParkingArea…“ auf Parkhaus privat oder öffentlich) oder auf keine Nummer der Tabelle
    /// (Wohnen). Er ist benannt und bekommt keine Nummer; die Zone fällt auf die nächste Rangstufe (Raumtyp, Gebäudeart).
    /// Ein Typ, der weder in der Tafel noch in der Liste steht, ist unbekannt und wird ebenso behandelt.</para>
    ///
    /// <para><b>Herkunft der Namen:</b> Die Schemakopie liegt nur lokal bei (<c>Referenzlaeufe/Schemakopien/</c>, D17). Die
    /// Namen sind aus dem Gedächtnis gesetzt; der Test <c>GbxmlRaumtyptafelTests</c> gleicht sie gegen <c>spaceTypeEnum</c>
    /// ab, sobald die Kopie beiliegt, und nennt jeden Namen, den das Schema nicht kennt.</para>
    /// </summary>
    internal static class GbxmlRaumtyptafel
    {
        /// <summary>gbXML-Raumtyp → Nummer des DIN-Profils (ohne Unterschied der Schreibung).</summary>
        internal static readonly IReadOnlyDictionary<string, int> NUMMER_JE_RAUMTYP = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // Büro (1 Einzelbüro, 3 Großraumbüro, 4 Besprechung, 5 Schalterhalle)
            ["OfficeEnclosed"] = 1,
            ["OfficeOpenPlan"] = 3,
            ["ConferenceMeetingOrMultipurpose"] = 4,
            ["BankCustomerArea"] = 5,
            // Handel (6 Einzelhandel/Kaufhaus)
            ["SalesAreaRetail"] = 6,
            // Bildung (8 Klassenzimmer, 9 Hörsaal)
            ["ClassroomOrLectureOrTraining"] = 8,
            ["ClassroomOrLectureOrTrainingPenitentiary"] = 8,
            ["AudienceOrSeatingAreaPermanentAuditorium"] = 9,
            // Beherbergung und Pflege (10 Bettenzimmer, 11 Hotelzimmer)
            ["PatientRoomHospitalOrHealthcare"] = 10,
            ["GuestRoomsHotel"] = 11,
            ["GuestRoomsMotel"] = 11,
            // Gastronomie (12 Kantine, 13 Restaurant, 14 Küche)
            ["DiningAreaCafeteriaOrFastFoodDining"] = 12,
            ["DiningAreaFamilyDining"] = 13,
            ["DiningAreaBarLoungeOrLeisureDining"] = 13,
            ["FoodPreparation"] = 14,
            // Neben- und Verkehrsflächen (16 WC, 17 sonstige Aufenthaltsräume, 19 Verkehrsfläche, 20 Lager/Technik)
            ["Restrooms"] = 16,
            ["LoungeOrRecreation"] = 17,
            ["CorridorOrTransition"] = 19,
            ["Stairway"] = 19,
            ["ElectricalOrMechanical"] = 20,
            ["ActiveStorage"] = 20,
            ["InactiveStorage"] = 20,
            // Halle (24 leichte Arbeit)
            ["ManufacturingFacilityDetailedManufacturing"] = 24,
            // Veranstaltung und Kultur (25 Zuschauerbereich, 26 Foyer, 28 Messe/Kongress, 29 Ausstellung)
            ["AudienceOrSeatingAreaPermanentPerformingArtsTheatre"] = 25,
            ["AudienceOrSeatingAreaPermanentMotionPictureTheatre"] = 25,
            ["LobbyPerformingArtsTheatre"] = 26,
            ["ExhibitSpaceConventionCenter"] = 28,
            ["MuseumGeneralExhibition"] = 29,
            // Bibliothek (30 Lesesaal)
            ["LibraryReadingArea"] = 30,
            // Sport (33 Turnhalle, 37 Fitnessraum)
            ["PlayingAreaGymnasium"] = 33,
            ["ExerciseCenter"] = 37,
            // Gesundheit (38 Labor, 39 Untersuchung, 40 Spezialpflege, 41 Flure Pflege)
            ["LaboratoryOffice"] = 38,
            ["ExamOrTreatmentHospitalOrHealthcare"] = 39,
            ["OperatingRoomHospitalOrHealthcare"] = 40,
            ["CorridorOrTransitionHospitalOrHealthcare"] = 41,
            // Lagerhalle (43)
            ["FineMaterialWarehouse"] = 43,
            ["MediumOrBulkyMaterialWarehouse"] = 43,
        };

        /// <summary>Die benannten mehrdeutigen Raumtypen — ohne Zuordnung (siehe Klassenkopf).</summary>
        internal static readonly IReadOnlyCollection<string> MEHRDEUTIG = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DiningArea",                           // Kantine (12) oder Restaurant (13)
            "Lobby",                                // Verkehrsfläche (19) oder Foyer (26)
            "AtriumFirstThreeFloors",               // Verkehrsfläche (19) oder Foyer (26)
            "AtriumEachAdditionalFloor",            // Verkehrsfläche (19) oder Foyer (26)
            "LockerRoom",                           // WC und Sanitär (16) oder Nebenfläche (18)
            "Workshop",                             // Halle leicht (24) oder mittelschwer (23)
            "ManufacturingFacilityLowBay",          // Halle leicht bis schwer (22 bis 24)
            "ManufacturingFacilityHighBay",         // Halle leicht bis schwer (22 bis 24)
            "LibraryStacks",                        // Freihandbereich (31) oder Magazin (32)
            "ParkingAreaAttendantOnlyParkingGarage", // Parkhaus privat (34) oder öffentlich (35)
            "ParkingAreaPedestrianParkingGarage",   // Parkhaus privat (34) oder öffentlich (35)
            "DormitoryLivingQuarters",              // Wohnen: keine Nummer der Tabelle 6
        };

        /// <summary>Die DIN-Nummer eines gbXML-Raumtyps; <c>null</c> für einen mehrdeutigen, unbekannten oder leeren Typ.</summary>
        internal static int? DinNummer(string raumtyp)
        {
            string t = raumtyp?.Trim();
            return string.IsNullOrEmpty(t) || MEHRDEUTIG.Contains(t) ? null
                 : NUMMER_JE_RAUMTYP.TryGetValue(t, out int n) ? n : null;
        }

        /// <summary>Ist der Raumtyp als mehrdeutig benannt?</summary>
        internal static bool Mehrdeutig(string raumtyp) => !string.IsNullOrWhiteSpace(raumtyp) && MEHRDEUTIG.Contains(raumtyp.Trim());

        /// <summary>Alle Namen der Tafel und der Liste (für den Schemaabgleich).</summary>
        internal static IEnumerable<string> AlleNamen => NUMMER_JE_RAUMTYP.Keys.Concat(MEHRDEUTIG);
    }
}
