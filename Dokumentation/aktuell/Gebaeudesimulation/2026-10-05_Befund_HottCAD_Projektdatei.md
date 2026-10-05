# Befund: HottCAD-Projektdatei (`.sqproj`) und IFC-Export — wo Räume, Zonen, Nutzungsprofile, Bauteile und Kalender liegen

**Stand:** 05.10.2026 · **Auftrag:** Struktur der Projektdatei
`2026-06-24_SV-Vaihingen Altbau Bestand test.sqproj` und des zugehörigen Ordners
`IFC_Ganglinie_Beispiele` auf Y: analysieren (Container, Formate, Tabellen, XML-Knoten),
ohne Produktdaten zu kopieren; dazu die IFC-Datei `Sportheim_1970_unsaniert.ifc` lesen.
**Methode:** Kopie der Projektdatei im Scratchpad, geöffnet mit `immutable=1`; Schema über
`sqlite_master` und `PRAGMA table_info`, Verknüpfungen über Zähl-Joins, XML- und
Medien-BLOBs nur auf Knoten- und Attributnamen ausgewertet. Die IFC-Datei wurde als
STEP-Text mit Regex zerlegt. Es wurden **keine Werte** übernommen: keine Hersteller- und
Typangaben, keine Kennwerte, keine Adressen; Raumnamen und Profilbezeichnungen stehen hier
nicht. Alle Zahlen sind Zeilen- und Knotenzählungen.

Vorarbeiten, auf die dieser Befund aufsetzt: [Befund C (IFC-Recherche)](2026-09-15_Befund_C_IFC-Recherche.md),
[Befund N (IFC-Import-Entwurf)](2026-09-15_Befund_N_IFC-Import_Entwurf.md),
[Befund P (Zonen und Materialien in IFC)](2026-09-15_Befund_P_IFC_Zonen_Materialien.md),
[Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kapitel 6 und die Entscheide
E72, E73 und E79 der [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md).


## 0. Das Ergebnis in acht Punkten

1. **Die `.sqproj` ist eine SQLite-3-Datenbank** (Seitengröße 4096, 5 262 Seiten, 21,5 MB,
   Journal `delete`, kein `user_version`/`application_id`), kein ZIP, kein XML-Container.
   Erzeuger laut `PrProject`: Hottgenroth „Energieberater 18599 3D PLUS“, Fassung 12.4.3.1.
   688 Tabellen und 805 Indizes, davon **99 Tabellen belegt**; das Schema ist ein
   generisches Produktschema mit Präfixfamilien (`Bm` Gebäude, `Pd` Profile, `Pm` Anlage,
   `Tc` Katalog, `Sm` Standort, `Pr` Projekt, `Xm` Verwaltung …).
2. **Räume liegen relational:** `BmBuilding` (1) → `BmFloor` (3) → `BmRoom` (56) über
   `FloorUUID`; je Raum eine Geometrie als **XML in `BmData.ClassValue`** (Polygon, Höhen,
   Einfügepunkt, Attribute `temperature`, `air_change`, `heating_type`, `room_type`).
   Daneben eine DIN-277-Flächenzeile je Raum in `BmBuildingSpace` und eine anlagenseitige
   Spiegelung `PmSpace`/`PmSpaceArea`, verknüpft über `PmBuildingReference`.
3. **Zonen liegen in `BmZone`** (35 Zeilen, alle mit `ParentUUID` = Gebäude, fünf
   `ZoneType`-Codes). Die Zuordnung Raum → Zone steht in **`BmZoneReference`**
   (`UUID` = Zone, `ReferenceToUUID` = Raum, `ReferenceClass` `TModelRoom`); in dieser
   Datei hat genau eine Zone alle 56 Räume. Zwölf Zonen tragen über `PdProfileReference`
   ein **DIN-V-18599-Nutzungsprofil**, vierzehn Zonen über `ProfileGroupUUID` eine
   **Profilgruppe mit zehn Zeitprofilklassen**.
4. **Nutzungsprofile liegen in `PdProfile`** (372 Zeilen) mit 1:1-Zusatztabellen je
   Profilklasse: `PdProfileUsage` (DIN-V-18599-10-Nutzungsprofil, die Profilnummer steht in
   `ProfileUsageType`), `PdProfileHeating`, `PdProfileCooling`, `PdProfilePerson`,
   `PdProfileVentilation`, `PdProfileDevice`, `PdProfileLighting`, `PdProfileElectrical`,
   `PdProfileDrinkingWater`, `PdProfileHumidity`, `PdProfileSunShading`. Die **Tagesganglinie**
   jedes Zeitprofils liegt in **`PdProfileTimeCurve`: genau 24 Zeilen je Profil**
   (`HourType` 1–24, `Ratio`, `Temperature`, `SpecificRatedAirChange`, `OperatingModeType`).
5. **Kalender liegen in `PdProfileTaskSerial`**: je Zeitprofil eine Zeile mit `TaskStartDay`/
   `TaskEndDay` (Tag 1–365), sieben Wochentagsschaltern, `TaskPeriodType`, Start- und
   Enddatum; die Verknüpfung läuft über `PdProfileTaskSerialReference`. In dieser Datei gilt
   jedes Profil ganzjährig ohne Wochentagsunterschied (alle Schalter 0, Tag 1–365); die
   Tagesart steht an der Profilgruppe (`PdProfileGroup.ProfileUsageDayType`, drei Werte).
6. **Bauteile liegen nur zum Teil relational:** `BmElement` (47) enthält Geschossdecken (8,
   Zusatztabelle `BmElementStorey`), Dächer (37, `BmElementRoof`) und Bodenplatten (2,
   `BmElementBaseSlap`), mit `UValue`, Schichtdicke, Fläche, `CatalogUUID`. **Wände, Fenster
   und Türen gibt es als Tabellenzeilen nicht.** Sie stecken in Binär- und XML-Strömen:
   Raumpolygone (XML in `BmData`), 242 „externals“ (Fenster, Türen, Verschattung) als ZIP mit
   `externals.xml` in `BmMedia`, das 3D-Grafikmodell als gzip-serialisierte .NET-Objekte in
   `GmMedia` (1,5 MB), und das **DIN-18599-Rechenmodell samt Hüllflächen** als proprietärer
   Binärstrom `WDIN18599DataModel` (575 KB) und als ZIP mit neun `*.BDExit`-Tabellen
   (`TableHuellfl` 638 KB) in `PrMedia`. Für die Hülle ist deshalb der **IFC-Export die
   lesbare Quelle**, nicht die Projektdatei.
7. **Klimadaten liegen in `SmDiagram.DiagramMedia` als JSON** (UTF-8 mit BOM, 1,1 MB):
   neun Stundenreihen à 8 760 Werte (Bewölkung, Wind, Trockentemperatur, relative Feuchte,
   Direkt-, Diffus-, Global-, langwellige und atmosphärische Gegenstrahlung) mit `StartTime`,
   `TimeBetweenValues`, `Unit`, dazu `Site` (Länge, Breite, Höhe, Zeitzone, TRY-Region) und
   `DataSource` (`Provider`, `ClimateExtremeType`, `PeriodStart`). Der Standort selbst steht
   in `SmSite` (Koordinaten, `TRYRegion`, Norm-Außentemperaturen nach mehreren Normen).
8. **Der IFC-Export** (`Sportheim_1970_unsaniert.ifc`, IFC4, Xbim-Exporter) gehört zu
   derselben Art Projekt: 56 `IfcSpace` in 3 `IfcBuildingStorey` wie die 56 Räume in 3
   Geschossen der Projektdatei. Er trägt Geometrie, Bauteile mit Schichtaufbauten und
   U-Werten und Raum-Sollwerte in Hottgenroth-eigenen Eigenschaftssätzen `HSETU_*`, aber
   **keine Zonen, keine Nutzungsprofile, keine Tagesganglinien, keine Kalender, keine
   Klimareihen** — das bestätigt E72. Wer Nutzung und Zeitverhalten eines HottCAD-Projekts
   übernehmen will, muss die `.sqproj` lesen.


## 1. Der Ordner `IFC_Ganglinie_Beispiele`

| Inhalt | Format | Befund |
|---|---|---|
| 1 Projektdatei `*.sqproj` | SQLite 3 | Gegenstand von Kapitel 2 bis 4 |
| 8 IFC-Dateien in der Wurzel (Typologie-Beispiele: Mehrfamilienhaus klein/mittel, Sportheim in drei Fassungen, Produktion mit Verwaltung, Verwaltung mit Montage, Wohngebäude EH55) | IFC4 (4) und IFC2X3 (4), alle vom Xbim-Exporter „Hottgenroth Model“, 3,3–14 MB | 20 bis 113 `IfcSpace`, 2 bis 7 Geschosse; nur die Sportheim-IFC4-Datei trägt 134 `IfcSolarDevice` (PV-Module) |
| 4 Ordner je Bauvorhaben (Straßennamen; hier nicht wiedergegeben) | je 1 IFC4 (3,8–23,5 MB, 33 bis 272 Räume, 5–6 Geschosse) + 3 CSV | IFC-Export und drei **Ergebnis-Ganglinien** derselben Simulation |
| `temperaturverlauf.txt` | 15 Byte Text | Vermerk „TRY 2024 Normal“ — der Klimadatensatz der Ganglinien |

**Die CSV-Ganglinien** (je 8 766 Zeilen) sind Windows-1252, Trenner `;`, Dezimalkomma,
mit abschließendem `;` je Zeile. Fünf Kopfzeilen (`Projekt:`, `Zeitraum: Jahresansicht`,
`Darstellung: Stundenmittelwerte`, `Diagramm: …`, Leerzeile), dann die Spaltenzeile, dann
8 760 Stundenzeilen mit `Zeit[Stunde]` 1–8760:

| Datei | Spalten |
|---|---|
| `Heizung_Kühlung-*.csv` | `Zeit[Stunde];Heizung[kW];Kühlung[kW];` |
| `Temperatur-*.csv` | `Zeit[Stunde];Außenluft[°C];` |
| `thermische_Lasten-*.csv` | `Zeit[Stunde];Lüftungswärme[kW];Solare Last[kW];Beleuchtung[kW];Personen[kW];Geräte[kW];` |

Das ist das Format, das ein `GanglinienImportAblauf` für den Vergleich HottCAD ↔ EPOS-Plan
lesen müsste: Stundenmittel in kW, ohne Zeitstempel, Stunde 1 = 1. Januar 0–1 Uhr.


## 2. Container und Schema der Projektdatei

### 2.1 Container

SQLite 3 (`SQLite format 3\0`, geschrieben mit SQLite 3.40), 4096-Byte-Seiten, 5 262 Seiten,
Freiliste leer, Journal `delete`, `user_version` 0. Die Datei lässt sich mit jedem
SQLite-Treiber lesen, `immutable=1` genügt (keine WAL-Dateien). Fremdschlüssel sind im
Schema **nicht** deklariert (`PRAGMA foreign_key_list` leer); alle Beziehungen laufen über
`UUID`-Textspalten, die Verknüpfungen in Kapitel 3 sind durch Zähl-Joins belegt.

### 2.2 Schemaregister und Familien

Die Tabelle `XmTables` (688 Zeilen) ist das **Register des Schemas**: je Tabelle `Name`,
`Type` („2.000“), eine **Tabellenfassung** `Version` (14.4 bis 17.6 — 515 Tabellen stehen auf
14.4, die Gebäude- und Profiltabellen auf 15.1 bis 16.7), `TableType`, `ContainerType`
(15 Werte, die Familien) und Stempel. Daraus lässt sich ein Leser **versionsabhängig**
machen, ohne das Schema zu raten.

| Präfix | Tabellen | Familie (aus Spaltennamen erschlossen) | hier belegt |
|---|---|---|---|
| `Tc` | 198 | Katalog (Bauteile, Geräte, PV-Module, Hersteller, VDI-3805-Datensätze, Textbausteine) | 11 |
| `Pm` | 107 | Anlage (Räume anlagenseitig, Erzeuger, Verteilung, Kessel, Zeitreihen) | 8 |
| `Nm` | 77 | Netz (Rohr-, Abgas-, Leitungsnetz) | 3 |
| `Bm` | 63 | Gebäudemodell (Gebäude, Geschoss, Raum, Zone, Bauteil, Daten, Medien) | 23 |
| `Em` | 54 | Elektro | 3 |
| `Fm` | 45 | Facility/Termine (unter anderem `FmAppointment`) | 0 |
| `Pd` | 38 | Profile (Nutzung, Zeitkurven, Kalender) | 20 |
| `Xm` | 24 | Verwaltung (Register, Projektinhalt, Einstellungen) | 6 |
| `Cm` | 23 | Kontakte, Adressen | 4 |
| `Pr` | 15 | Projekt (Einstellungen, Journal, Medien) | 8 |
| `Sm` | 12 | Standort und Klima | 2 |
| `Fc`, `Ec`, `Gm` | 12 / 11 / 9 | Finanzen, Wirtschaftlichkeit, Grafikmodell | 0 / 5 / 3 |

**Gemeinsames Spaltenmuster** jeder Tabelle: `UUID` (Text, Primärschlüssel), `GId`, `Id`,
`SeqNum`, `SortNum`, `ShortDesc`, `LongDesc`, `InfoDesc`, `StampCreate/Edit/Sync`, `MId`,
`ProjectUUID`, ein `Sync*`-Block (sieben Spalten) und meist `GeneratorType`, `StateType`,
`VariantType`, `VariantRoot`, `EditModeType`. Katalogtabellen (`Tc*`) nutzen `UId`/`OId`
statt `UUID`. Fachspalten sind überwiegend `INTEGER`-Codes (`…Type`) und `FLOAT`; Zeiten
sind `DATE` als Text, Delphi-Nullzeit `1899-12-30 00:00:00` bedeutet „leer“.

### 2.3 Belegte Tabellen (Auszug nach Zeilen)

| Zeilen | Tabelle | Inhalt |
|---|---|---|
| 7 320 | `PdProfileTimeCurve` | 305 Zeitprofile × 24 Stunden |
| 688 | `XmTables` | Schemaregister |
| 372 / 305 / 305 / 305 | `PdProfile` / `PdProfileGroupReference` / `PdProfileTaskSerial` / `PdProfileTaskSerialReference` | Profile, Gruppenzuordnung, Kalender |
| 113 | `BmData` | Geometrie-XML und Farben je Objekt |
| 90 / 56 | `PmSpaceArea` / `PmSpace` | Heizflächen und Räume anlagenseitig |
| 76 | `PrProjectSetting` | Berechnungsvorgaben und Seitenschalter |
| 62 / 56 / 56 / 56 | `BmBuildingSpace` / `BmRoom` / `BmRoomData` / `BmZoneReference` | Flächen, Räume, Raumdaten, Raum→Zone |
| 56 / 38 / 39 / 31 / 31 / 30 / 29 ×6 | `PdProfileUsage` / `PdProfileHeating` / `PdProfilePerson` / `PdProfileCooling` / `PdProfileVentilation` / `PdProfileSunShading` / übrige Profilklassen | Zusatztabellen je Profilklasse |
| 47 / 47 / 47 / 37 / 34 / 8 / 2 | `BmElement` / `BmElementDimensioning` / `BmElementEnergyConsulting` / `BmElementRoof` / `BmElementReference` / `BmElementStorey` / `BmElementBaseSlap` | Bauteile (Decken, Dächer, Bodenplatten) |
| 35 / 35 / 19 | `BmZone` / `BmZoneData` / `BmPropertySet` | Zonen, Zonendaten, Zonenfarben |
| 40 / 18 | `PrJournalEntry` / `PrJournal` | Änderungsjournal (106 KB `JournalData`) |
| 16 / 13 | `PdProfileGroup` / `PdProfileReference` | Profilgruppen, Profil→Zone |
| 13 / 2 / 1 / 1 | `PrMedia` / `BmMedia` / `GmMedia` / `SmDiagram` | eingebettete Medien, siehe 3.7 |
| 3 / 3 / 1 / 1 / 1 | `BmFloor` / `BmFloorData` / `BmBuilding` / `BmBuildingData` / `SmSite` | Geschosse, Gebäude, Standort |
| 3 / 1 / 1 / 1 / 8 | `PmPlant` / `PmHeatingBoiler` / `PmDevice` / `NmNet` / `EmCable` | Anlage |
| 2 / 2 / 1 / 1 / 1 / 1 | `TcCatalog` / `TcCatalogContent` / `TcBasis` / `TcManufacturer` / `TcPVModuleTechData` / `TcProfileElement` | **Katalogauszug mit Herstellerdaten** (nicht wiedergegeben) |

Nicht belegt, aber vorhanden und für einen Leser erwähnenswert: `PmTimeSeries` (BLOB,
Zeitreihen der Anlage), `TcPropertySet`, `XmSetting` (BLOB), `FmAppointment` (einzige
Tabelle mit einer Kalenderspalte im Wortsinn, `GoogleCalendarIdentifier`).


## 3. Wo liegt was

### 3.1 Gebäude, Geschoss, Raum

```
BmBuilding (1) ──BuildingUUID── BmFloor (3) ──FloorUUID── BmRoom (56) ──UUID── BmRoomData (56)
      │                                                        │
      ├── BmBuildingSpace (62): je Raum (56), je Geschoss (3), je Gebäude (3)
      │       Flächen nach DIN 277: UsableAreaN1…N7, TechnicalArea, TrafficArea, NetArea,
      │       ConstructionArea, GrossArea, Gross-/Net-/ConstructionVolume — je Wert, Vorgabe, Zustand
      └── BmData (113, ReferenceUUID → Objekt): 56 × Raumgeometrie-XML, 3 × Geschoss-XML,
              13 × Bauteil-XML, 6 × Gebäude (XML building_data, PlotOptions, print_layouts; 3 Skalare),
              32 × Zone (nur Farbwerte „A;R;G;B“, kein XML)
```

`BmRoom` hat 192 Spalten: Geometrie (`Area`, `Volume`, `Height`, `ClearHeight`, Boden-,
Wand- und Deckenflächen je mit Berechnungsregel), Codes (`RoomType`, `SpaceType`,
`HeatingType`, `CoolingType`, `VentilationType`), **Sollwerte** (`InsideTemperature`,
`HeatingRatedInsideTemperature`, `CoolingRatedInsideTemperature`, Luftwechselraten,
`AirExchangeRate50`), **Ergebnisse** der Heiz- und Kühllastrechnung (`HeatingLoad`,
`HeatingRatedHeatingLoad`, `TransmissionLoad`-Aufteilung auf Fußboden/Radiator/Wand/
Decke/Elektro/Lüftung, `CoolingLoad`, `HeatDemand`, `CoolingDemand`) und Lüftung nach
DIN 1946-6. `BmRoom.ProfileGroupUUID` und `RoomGroupUUID` zeigen in dieser Datei auf
nichts Vorhandenes — die Nutzung hängt an der Zone, nicht am Raum. `BmFloor` und `BmBuilding`
tragen dieselben Last- und Lüftungsspalten aggregiert; `BmBuilding` dazu Baujahr,
Gebäudetyp-Codes, Wärmeschutzstandard, Hüllkennwerte (`UmValue`, `Cwirk`).

**Raumgeometrie-XML** in `BmData.ClassValue` (Wurzel `<geometry>`): je Raum
`Room/room[@GUID, @external_guid, @floor, @name, @short_name, @number, @room_type,
@component_type, @heating_type, @temperature, @air_change, @height, @area,
@userDefinedLivingArea]` mit `points/p` (Grundrisspolygon, hier 4–6 Punkte je Raum),
`polygons/plg[@height, @type, @with_holes]/points/p`, `heights/height_ext`,
`insertation_point/p`, `transform_origin/p`, `LivingAreaCalculationRule/rule(minZ,
percentage)`. Die `GUID` der Räume ist das Bindeglied zu `external_guid` der Bauteile
(Dachsegmente nennen in `refs/item[@room]` den Raum).

Anlagenseitig gibt es dieselben Räume noch einmal: `PmSpace` (56, 139 Spalten, Heizflächen-
und Auslegungsspalten), `PmSpaceArea` (90 Heizflächen, `PlantSpaceUUID` → `PmSpace`) und
`PmBuildingReference` (56 Zeilen `ReferenceFromUUID` = `PmSpace`, `ReferenceToUUID` =
`BmRoom`; eine Zeile Anlage → Gebäude).

### 3.2 Zonen

`BmZone` (35 Zeilen, 195 Spalten, wie `BmRoom` plus `ZoneType`, `ZoneSubType`,
`EscapeRouteType`, `FacadeType`, `LCANonResidentialType`, `BWZId`, `BIMUUID`) hängt mit
`ParentUUID` am Gebäude (`ParentClassType` 4). Fünf `ZoneType`-Codes, unterschieden nur
durch ihre Verknüpfungen:

| `ZoneType` | Zeilen | Verknüpfung | Deutung |
|---|---|---|---|
| 5 | 12 | je Zone ein DIN-V-18599-Nutzungsprofil über `PdProfileReference` (`ReferenceClass` `TModelZone`); 2 mit Farbsatz | **Nutzungszonen nach DIN V 18599** |
| 6 | 14 | je Zone eine `PdProfileGroup` (`ProfileGroupUUID`) mit den zehn Zeitprofilklassen; 12 mit Farbsatz; **eine davon** zeigt in `BmZoneReference` auf alle 56 Räume | **Simulationszonen** mit Tagesganglinien |
| 2 | 5 | nur Farbsatz in `BmPropertySet` und `BmData` | Darstellungs- oder Bauteilgruppen |
| 10 | 3 | nur Farbe in `BmData` | unbekannt |
| 8 | 1 | keine | unbekannt |

**Raum → Zone:** `BmZoneReference` (`UUID` = Zone, `ReferenceToUUID` = Raum,
`ReferenceClassType` 1, `ReferenceClass` `TModelRoom`), hier 56 Zeilen einer einzigen Zone.
Welche Räume zu den anderen 13 Simulationszonen und zu den 12 Nutzungszonen gehören, steht
**nicht** in den Tabellen dieser Datei — entweder sind diese Zonen unbelegt (Vorlagen aus
dem Zonierungsdialog), oder die Zuordnung liegt im DIN-18599-Binärstrom (3.7). Für den
Import nach E79 heißt das: Die relationale Zuordnung ist lesbar, aber nicht garantiert
vollständig; sie muss gegen die Raumliste geprüft werden („nicht zugeordnete Räume“).

`BmZone.BIMUUID` und `BmElement.BIMUUID` sind GUIDs in Klammerschreibweise (38 Zeichen);
die IFC-`GlobalId` ist die 22-stellige Base64-Form. Ein Abgleich Projektdatei ↔ IFC muss
also umkodieren; ob `BIMUUID` dieselbe GUID wie der IFC-Export trägt, ist **nicht geprüft**
(offen, Kapitel 5).

### 3.3 Nutzungsprofile und Tagesganglinien

```
BmZone(ZoneType 5) ◄──ReferenceToUUID── PdProfileReference ──UUID──► PdProfile(ProfileType 2) ──UUID── PdProfileUsage
BmZone(ZoneType 6) ──ProfileGroupUUID──► PdProfileGroup ◄──UUID── PdProfileGroupReference ──ReferenceToUUID──► PdProfile(ProfileType 4…21)
                                                                                                                       │ UUID
                                                                             PdProfileTimeCurve (24 Zeilen) ◄──ProfileUUID──┤
                                                                             PdProfile<Klasse> (1:1) ◄──UUID────────────────┤
                                                                             PdProfileTaskSerialReference ──ReferenceToUUID─┘
```

**`PdProfile`** (372 Zeilen): `ProfileType` (Klassencode), `ProfileSourceType` (0 oder 4 —
bei den Nutzungsprofilen 44 × 4 und 12 × 0, gelesen als Norm- gegen Eigenprofil),
`StandardType`, `Active`, `PeriodStartDate`/`PeriodEndDate`, `RoomType`,
`CapacityLimitingType`, `FloorType`, `ProfileGroupUUID`, `PDMUUID`. Die Klassencodes und
ihre 1:1-Zusatztabellen (gleicher `UUID`), wie sie die Zähl-Joins belegen:

| `ProfileType` | Zeilen | Zusatztabelle | `ReferenceClass` in den Verweistabellen | Zeitkurve |
|---|---|---|---|---|
| 2 | 56 | `PdProfileUsage` | — (nur `PdProfileReference` → Zone) | nein |
| 4 | 29 | `PdProfileDevice` | `TModelProfileDevice` | ja, `Ratio` 0–1 |
| 5 | 30 | `PdProfileSunShading` | `TModelProfileSunShading` | ja |
| 6 | 38 | `PdProfileHeating` | `TModelProfileHeating` | ja, `Temperature` (Sollwert) |
| 7 | 31 | `PdProfileCooling` | `TModelProfileCooling` | ja, `Temperature` (Sollwert) |
| 8 | 39 | `PdProfilePerson` | `TModelProfilePerson` | ja, `Ratio` 0–1 |
| 9 | 29 | `PdProfileLighting` | `TModelProfileLighting` | ja, `Ratio` 0–1 |
| 10 | 31 | `PdProfileVentilation` | `TModelProfileVentilation` | ja, `SpecificRatedAirChange` |
| 14 | 29 | `PdProfileHumidity` | `TModelProfileHumidity` | ja, `Ratio` |
| 16 | 29 | `PdProfileElectrical` | `TModelProfileElectrical` | ja, `Ratio` |
| 21 | 29 | `PdProfileDrinkingWater` | `TModelProfileDrinkingWater` | ja, `Ratio` 0–1 |
| 13 / 20 | 1 / 1 | `PdProfileBuildingElement` / `PdProfileValve` | — | nein |

**`PdProfileUsage`** (56 Spalten) ist das **DIN-V-18599-10-Nutzungsprofil**: `ProfileUsageType`
trägt die Profilnummer (hier Werte 1–47, 70, 71 — die Nummern der Norm-Tabelle),
`PeriodOfOperationFrom/To`, `HeatedFrom/To`, `CoolingOperatingTimeFrom/To`,
`HVACOperatingTimeFrom/To` (als `DATE`, hier alle auf der Nullzeit), `UserCount`,
`NominalRoomTemperature` (dazu je einmal `…DinEn12831` und `…OenEn12831`),
`DropOfTemperatureSetback`, `SupplyAirChange`, `MinimumExternalAirFlowBasedOnPersons/Area`,
`AnnualEffectiveLoadHoursOfPersons/Devices`, `DailyEffectiveLoadHoursOfPersons/Devices`,
`SpecificThermalOutputPowerOfPersons`, `SpecificThermalOutputOfDevices`,
`MaintenanceIllumination`, `RelativeAbsence`, `PersonActivityClassType`. Das ist genau der
Spaltensatz der Norm-Tabelle 4 — ein Leser kann die Profile **mit der Profilnummer** auf
die eigenen Konditionierungsvorlagen abbilden (E79: „DIN-V-18599-Profile als spätere Welle“).

**`PdProfileTimeCurve`** (7 320 = 305 × 24 Zeilen): `ProfileUUID`, `CurveId` und `HourType`
(beide 1–24, Stunde des Tages), `HourValue` (dasselbe als Gleitzahl), `OperatingModeType`
(1 oder 2, je Profil einheitlich; 242 Profile mit 1, 63 mit 2 — die Semantik ist aus der
Datei nicht ableitbar), `Ratio` (0–1, bei Feuchte bis 0,4, bei Elektro bis 0,073),
`Temperature` (nur Heizen 15–21 und Kühlen 25–28), `SpecificRatedAirChange` (nur Lüftung,
0–5), `MinSpecificRatedAirChange`, `MaxSpecificRatedAirChange`. **Eine Kurve je Profil,
keine Unterscheidung Werktag/Wochenende innerhalb des Profils** — die Tagesart ist an der
Gruppe (`PdProfileGroup.ProfileUsageDayType`: 13 Gruppen mit 4, je eine mit 5 und 6).

Die klassenspezifischen Zusatztabellen tragen die Nennwerte, auf die `Ratio` wirkt:
`PdProfilePerson` (`RatedPersonOccupancyRate`, `SpecificRatedDryHeatEmission`,
`SpecificRatedHumitHeatEmission`, Aktivitätsklasse), `PdProfileHeating`
(`RatedFlowTemperature`, `RatedReturnFlowTemperature`, `SpecificRatedThermalCapacity`,
`AmountOfRatedConvectiveThermalOutput`), analog Kühlung, Lüftung, Geräte, Beleuchtung,
Elektro, Trinkwasser, Feuchte, Sonnenschutz; alle mit `ProfileTimeType` und
`ProfileDayType` (hier 0).

### 3.4 Kalender

**`PdProfileTaskSerial`** (305 Zeilen, eine je Zeitprofil): `TaskType`, `TaskSerialType`,
`TaskPeriodType` (304 × 1, 1 × 4), `TaskStartDate`/`TaskEndDate`/`TaskHasFinishDate`,
`TaskStartDay`/`TaskEndDay` (**Tag im Jahr**, hier überall 1–365), die sieben Schalter
`TaskMonday … TaskSunday` (hier überall 0), `TaskOperatingSerialDesc`. Die Zuordnung zum
Profil läuft über **`PdProfileTaskSerialReference`** (`UUID` = Kalenderzeile,
`ReferenceToUUID` = Profil, `ReferenceClass` wie oben). Das Modell erlaubt also je Profil
**mehrere Gültigkeitsabschnitte** (Jahresabschnitt × Wochentage); diese Datei nutzt nur den
Ganzjahresabschnitt. Zusätzlich trägt `PdProfile` selbst `PeriodStartDate`/`PeriodEndDate`.
Ferien-, Feiertags- oder Heizperiodenkalender des Gebäudes (wie EPOS-Plans
Konditionierungskalender) gibt es als eigene Tabelle **nicht**; die Heizperiode wird über
`PrProjectSetting` (`SettingKey` `Nutzungsprofile_Kalender`, Seitenschalter) und die
Rechenvorgaben gesteuert.

### 3.5 Bauteile

**`BmElement`** (47 Zeilen, 149 Spalten): `ElementType` (4 = Geschossdecke 8×, 5 = Dach 37×,
11 = Bodenplatte 2×), `ElementSubType`, `ElementUsageType`, `AdjacentType`,
`AdjacentEntityType`, `RepositoryType`/`RepositoryLevel`, Maße (`Height`, `Width`,
`Thickness`, `Orientation`, `Slope`, `GrossArea`, `NetArea`, `Volume`, `Perimeter`),
Bauphysik (`UValue`, `UValueMarker`, `R`, `RMinDIN4108`, `AlphaI`, `AlphaE`,
`SpecificComponentMass`, `Cp03`, `Cp10`, `EmissionCoefficient`, `AbsorptionCoefficient`,
`TransmissionCoefficient`, `GTot`, `AmountOfFrame`, `SunShadingCoefficient`,
`ReductionFactor`), Zusatzdämmung innen/außen (`AddIns…`), Akustik (`Acoustic…`),
Katalogbezug (`CatalogUUID`, `CatalogSystemUUID`, `CatalogBasisUUID`, `CatalogDimUUID` —
der Katalog selbst ist **nicht** in der Datei, die UUIDs zeigen ins Leere), `BIMUUID`.
Je Zeile eine 1:1-Zusatztabelle: `BmElementStorey` (`StoreyType`, `ElevationOfRefHeight`,
`GroundArea`, `GroundPerimeter`), `BmElementRoof` (`RoofType`, `RidgeHeight`, `RoofSlope`,
`GableSlope`, `Overhang`, `H1`, `H2`, `S1`, `S2`), `BmElementBaseSlap`; dazu
`BmElementDimensioning` und `BmElementEnergyConsulting` (47 je). **`BmElementReference`**
(34 Zeilen) verknüpft Dachsegmente mit Räumen (`ReferenceFromType` 1, `ReferenceToType` 16,
`AdjacentType`, `Rsi`, `Rse`, `Fx`, `Bu`, `TransmissionHeatLossCoefficient`); die 34
entsprechen den 34 `segments/item` der Dach-XML.

**Bauteil-XML** in `BmData` (13 Zeilen, Wurzel `<geometry>`): `Platform/platform[@GUID,
@external_guid, @floor, @thickness, @u_value, @u_value_default_typo, @against_air,
@against_soil, @against_unheated, @is_hole, @is_platform, @is_auto_generated,
@building_hull_added]` mit `points/p` und `repositories/val` (10 Decken/Platten) und
`Roof/roof[@GUID, @type, @ridge_height, @thickness, @gable_mode]` mit
`external_roof_parts/part[@type]/points/p`, `segments/item[@guid,
@object_type]/refs/item[@guid, @room]` und `rooms/val` (3 Dächer).

**Wände, Fenster, Türen, Verschattung:** keine Tabellenzeilen. Sie liegen in

- `BmMedia` (MediaType 1, ZIP 54 KB) → `externals.xml`, Wurzel `<topitem>`, **242 ×
  `externals/external[@GUID, @external_type, @floor, @is_standalone, @name, @parentGuid]`**
  mit `points/p` — die Öffnungen und Anbauteile der Räume (der IFC-Export zählt 80 Fenster,
  4 Türen, 81 Öffnungen, 49 Verschattungen);
- `BmMedia` (MediaType 11, XML 188 KB) → `<topitem>` mit `application_control`
  (Umgebung, Gelände), `layer_management` (963 Layer) und `settings` (CAD-Einstellungen,
  Schattenrechnung, Fenster- und Türbreiten) — die **CAD-Projektdatei**;
- `GmMedia` (gzip, entpackt 1,5 MB) → serialisierte .NET-Objektgraphen des 3D-Modells
  (Typnamen `Hstac.GemeinsameSchicht.…`), nicht ohne die Anwendungsklassen lesbar;
- `PrMedia` (MediaType 0, 575 KB) → Binärstrom mit Kopf `WDIN18599DataModel`
  (.NET-`BinaryFormatter`-Signatur) — das **Rechenmodell der DIN-V-18599-Bilanz** samt
  Hüllflächen; `PrMedia` (MediaType 1, ZIP 180 KB) → neun Tabellen `TableProjekt`,
  `TableHuellfl` (638 KB), `TableHausDat`, `TableAnbauDat`, `TableAnlage`, `TableResult`,
  `TableZusatz` (602 KB), `TableHeizung`, `TableWasser` im Format `.BDExit` (binärer
  Tabellenexport mit Feldkopf `PROJEKT_ID`, `PROJEKT_NR`, `SUBTYPE` …) — ein Austauschformat
  der Energieberater-Produktfamilie.

Die Hülle mit Wänden, Fenstern, Schichtaufbauten und U-Werten ist deshalb aus der
Projektdatei **nur über proprietäre Formate** zu bekommen. Der IFC-Export liefert sie offen
(Kapitel 4) — das bleibt der Weg des Mehrzonenkonzepts.

### 3.6 Standort und Klima

`SmSite` (1 Zeile, 76 Spalten): Ort, Postleitzahl, `Latitude`/`Longitude` (auch als
Rahmen Nordost/Südwest), `PlaceID`, `HeightOverNN`, Geländehöhen, `AmbientTemperature`,
`AnnualMeanExternalTemperature`, `ExternalDesignTemperature` je in Fassungen EN 12831,
EN 12831:2017, Meteonorm 7, OIB, `GroundWaterLevel`, `AnnualMeanGroundTemperature`,
`AccumulatedPrecipitation`, **`TRYRegion`**, `Timezone`, `RegionalFactor`,
`CoolingLoadZoneType`, ISO-3166-Codes, `HorizonUUID`, `TerrainUUID`.

**Die Klimareihen** stehen in `SmDiagram.DiagramMedia` (JSON, UTF-8 mit BOM, 1,1 MB) —
Wurzelschlüssel `ClimateValues`, `Site`, `DataSource`:

```
ClimateValues.{CloudCoverage, WindSpeed, DryBulbTemperature, RelativeHumidity,
               DirectIrradiation, DiffuseIrradiation, GlobalIrradiation,
               LongWaveIrradiation, AtmosphericCounterIrradiation}
   .{StartTime: str, TimeBetweenValues: str, Unit: str, Values: [8760 Zahlen]}
Site.{Longitude, Latitude, HeightOverNN, Timezone: float, TryRegion: int}
DataSource.{Provider: int, ClimateExtremeType: int, PeriodStart: str}
```

Das ist ein vollständiger Stundenklimasatz im Raster 8 760 — direkt vergleichbar mit der
Klimatabelle von EPOS-Plan (Trockentemperatur, Direkt-, Diffus-, Globalstrahlung); Wind,
Feuchte, Bewölkung und langwellige Strahlung liegen zusätzlich vor. `SmDiagramPoint` und
`SmDiagramRow` (leer) tragen die TRY-Spalten `TRYT`, `TRYP`, `TRYWR`, `TRYWG`, `TRYN`,
`TRYX`, `TRYRF`, `TRYB`, `TRYD`, `TRYA`, `TRYE`, `TRYIL` — die Spaltenkennungen des
DWD-TRY-Formats.

### 3.7 Ergebnisse, Einstellungen, Anlage, Katalog

- **Ergebnisse:** auf Raum-, Geschoss- und Gebäudeebene in den Lastspalten von `BmRoom`,
  `BmFloor`, `BmBuilding`; als Zusammenfassung in `PrMedia` (MediaType 11, 766 Byte XML,
  Namensraum `hottgenroth.de/VariantData.xsd`): `SimulationData/Building[@HeatDemand,
  @HeatingLoad, @CoolingDemand, @CoolingLoad]` mit `HeatGainSources[@Constructions,
  @Devices, @Heater, @Illumination, @People, @Solar, @Ventilation]`,
  `HeatLossSources[@Constructions, @Cooler, @Ventilation]` und
  `SummerHeatProtection[@DegreeHoursOfExcessiveTemperature…]`; ein LCCA-Textstrom
  (73 KB, `<LCCA BuildingElements=…>`); neun JPEG-Bilder (0,27–0,57 MB, Diagramme und
  Ansichten). **Stundenreihen der Simulation (die CSV-Ganglinien) liegen nicht in der
  Projektdatei**; `PmTimeSeries` ist leer.
- **Einstellungen:** `PrProjectSetting` (76 Zeilen; `SettingType`, `SettingKey`,
  `SettingValue`, `SettingData`, Min/Max/Default) mit Rechenvorgaben
  (`HeatLoadCalculationType`, `ThermalBridgeCalculationMethod`, `ThermalBridgeValue`,
  `InternalTemperatureCorrectionMethod`, `AltitudeCorrection`, `UseTGAVentilation`,
  `PS_SIM_*`, `PS_HUMIDITY_IS_ENABLED`) und Seitenschaltern des Programms
  (`Nutzungsprofile`, `Nutzungsprofile_Kalender`, `Klimadaten`, `Gebaeude_Bauteile`,
  `Ergebnisse_Zonen`, `PV_Anlage`, `WP_Anlage`, `Wirtschaftlichkeit` …);
  `PrProjectOption` (9 typisierte Optionen); `XmProjectContent` (51 Inhaltsarten).
- **Anlage:** `PmPlant` (3), `PmPlantData`, `PmHeatingBoiler` (1, 72 Spalten), `PmDevice`
  (1), `PmDistribution`, `PmConnection`, `NmNet`/`NmNetSetting`/`NmNetProperty`,
  `EmPlant`, `EmCable` (8), `CmConnect` (7); Wirtschaftlichkeit `EcCalculation`,
  `EcCalculationParam`, `EcCost`, `EcFinancing`.
- **Katalogauszug:** `TcCatalog`, `TcCatalogContent`, `TcBasis` (212 Spalten),
  `TcManufacturer`, `TcPVModuleTechData`, `TcProfileElement` (183 Spalten mit
  `VDI3805DataSet*`-Feldern), `TcMedia` (ein BMP). Hier liegen Hersteller- und Typdaten
  der verwendeten PV-Module — **nicht wiedergegeben**, und für einen Import ohne Belang.


## 4. Die IFC-Datei `Sportheim_1970_unsaniert.ifc`

**Kopf:** ISO-10303-21, `FILE_SCHEMA IFC4`, Exporter `Xbim.IO.MemoryModel` („Processor
version 6.0.0.0“), Projektname „Hottgenroth Model“, Organisation Hottgenroth Software AG,
74 202 Zeilen, 7,6 MB. 17 `IfcSIUnit` (m, m², m³, °C, W, Pa, kg, s, rad, lx, lm, cd, A, V,
N, Hz, sr); Längen in Metern, Schichtdicken im `IfcMaterialLayer` in Millimetern.

**Räumliche Struktur:** `IfcProject` → `IfcSite` (Name = Ort) → `IfcBuilding`
(`ObjectType` `TModelBuilding`) → 3 `IfcBuildingStorey` (`TModelFloor`, Elevation gesetzt)
→ 56 `IfcSpace` (`TModelRoom`, Name = Raumname, kein `LongName`), aggregiert über
`IfcRelAggregates` (21/15/20 Räume je Geschoss). Die `ObjectType`-Werte sind die
**Klassennamen des HottCAD-Objektmodells** (`TModel…`) — dieselben Bezeichner wie die
`ReferenceClass`-Spalten der Projektdatei. Das erlaubt eine robuste Klassifikation beim
Import unabhängig von der IFC-Klasse.

**Bauteile:** 205 `IfcWall` (`TModelBuildingElementWall`), 112 `IfcSlab` (90
`TModelBuildingElementStorey`, 22 `TModelBuildingElementBaseSlap`), 39 `IfcRoof`, 80
`IfcWindow`, 4 `IfcDoor`, 81 `IfcOpeningElement` (Voids/Fills), 49
`IfcBuildingElementProxy` als `TModelBuildingElementShadingDevice` (41 davon als Kind eines
Fensters über `IfcRelAggregates`), 134 `IfcSolarDevice` `.SOLARPANEL.` (`TModelPVModule`,
der Name trägt die Hersteller-Typbezeichnung) in einem `IfcDistributionSystem`
(`IfcRelAssignsToGroup`, `IfcRelServicesBuildings`). Typobjekte: 7 `IfcSlabType`, 4
`IfcWallType`, 3 `IfcWindowType`, 3 `IfcRoofType`, 3 `IfcDoorType`, 1 `IfcSolarDeviceType`.
Elemente hängen über `IfcRelContainedInSpatialStructure` am Geschoss; die PV-Module am
Gebäude.

**Geometrie:** Räume als `Body`/`Brep` (`IfcFacetedBrep`, 56). Von den Bauteilen haben nur
die **Hüllbauteile** eine Körperdarstellung (`Body`/`SurfaceModel`,
`IfcShellBasedSurfaceModel`): 92 von 205 Wänden, 10 von 112 Platten, 3 von 39 Dächern, 78
von 80 Fenstern, alle 134 PV-Module; 113 Wände, 102 Platten, 36 Dächer und alle 49
Verschattungen sind **geometrielos** (Innenbauteile und Raumanteile, nur Mengen und
Eigenschaften). Schichtaufbauten (`IfcMaterialLayerSetUsage` → `IfcMaterialLayerSet`,
1–3 Schichten, 105 Sätze) nur an den 92 + 10 + 3 Hüllbauteilen; je `IfcMaterial`
`Pset_MaterialCommon.MassDensity` und `Pset_MaterialThermal.ThermalConductivity`
(in der IFC2X3-Fassung als `IfcGeneralMaterialProperties`/`IfcThermalMaterialProperties`).
**Keine `IfcRelSpaceBoundary`**, keine `IfcRelConnectsElements`; die Raum-Bauteil-Beziehung
steht in **56 `IfcRelReferencedInSpatialStructure`** („Spatial references of space …“):
je Raum die Liste seiner Wände, Platten, Dächer, Fenster, Türen (typisch 4 Wände, 1–2
Platten, 0–1 Dach, 0–5 Fenster). Das ist der Raumbezug, den E73 meint — er nennt das
Bauteil, nicht den Nachbarraum.

**Eigenschaftssätze** (3 296 `IfcPropertySet`, 12 822 `IfcPropertySingleValue`, 7 356
`IfcPropertyEnumeratedValue`; Enumerationen als `IfcLabel` mit Code-Präfix, etwa
`mrt…` für Raumtyp, `bht…` für Beheizung):

| Pset | an | Eigenschaften (Namen) |
|---|---|---|
| `HSETU_BauteilAllgemein` | allen Bauteilen, Räumen, Geschossen, Gebäude | `ElementType`, `AdjacentType`, `LoadBearingType`, `Orientation (°)`, `Orientation.Type`, `RadiatorPositionType`, `EmbeddedSystemPositionType`, `EcoIndexType`, `ColourType`, `GUID`, `ID`, `Identifier`, `Name`, `Number`, `Generator`, `Comments`, Stempel |
| `HSETU_Bauteilreferenzen` | Bauteilen | `ElementReferences[0].AdjacentType`, `…[0].Orientation (°)`, `…[1].AdjacentType`, `…[1].Orientation (°)`, `ElementAssignmentDirectionType`, `ElementAssignmentFlowDirectionType`, `UValue (W/(m² K))` |
| `HSETU_BauteilEnergetischeBewertung` | Bauteilen | `ElementEnergyConsultingProperties.FxElementType`, `.CladdingSurface` (Wert, Vorgabe, Zustand) |
| `HSETU_EcoCad` | Bauteilen, Räumen, Geschossen, Gebäude | `ElementType`, `NetArea`, `CoatingArea`, `FractionOfFrame`, `Dimensions.DIN18599V2011.CharacteristicNetArea` |
| `HSETU_BauteilSchallschutzbetrachtungen` | Bauteilen | `Acoustic.BodyText` |
| `HSETU_DachAllgemein` / `HSETU_SlabAllgemein` | Dach / Platten | `RoofType`, `Slope (°)`, `Orientation` / `BaseSlapType`, `GroundType`, `InstallationType`, `ElevationOfRefHeight (m)`, Perimeterdämmung λ |
| `HSETU_RaumAllgemein` | Räumen | `RoomType`, `HeatingType`, **`InsideTemperature (°C)`**, `RatedMinAirExchangeRate (1/h)`, `RoomVentilationType`, `LevelOfBasicFloor (m)` |
| `HSETU_RaumHeizung` (+ `…Fußboden`, `…Radiator`, `…Wand`) | Räumen | `Heating.HeatingLoad (W)`, `Heating.RatedHeatingLoad (W)`, `Heating.AdditionalHeatingLoad (W)`, `Heating.HeatingLoadType`, Anteile je Übergabeart |
| `HSETU_RaumMappingNachDIN12831` | Räumen | `DesignLoads.Load/RatedLoad/TransmissionLoad/VentilationLoad/AdditionalLoad (W)`, `Ventilation.InfiltrationHeatLoss`, `.SupplyHeatLoss`, `.SurplusHeatLoss`, `.SupplyAirTemperature (°C)`, `Info` |
| `HSETU_RaumMappingNachDIN1946-6` / `HSETU_RaumLüftungstechnik` | Räumen | Luftvolumenströme (Feuchteschutz, reduziert, Nenn, Intensiv, je Durchlass), `Ventilation.VentilationLoadType`, `CombustionAirSupply.AirSupplyType` |
| `HSETU_RaumWandflächen` / `HSETU_RaumDeckenflächen` | Räumen | `AreaCalculationRule (m)` |
| `HSETU_GeschossAllgemein` | Geschossen | `FloorType`, `ElevationOfRefHeight (m)` |
| `HSETU_Gebäude…` (Allgemein, Information, Berechnungsvorgaben, Lüftungstechnik, EnergetischeBewertung…) | Gebäude | `BuildingType`, `BuildingUsageType`, `BuildingHeatInsulationStandardType`, `BuildingEnergyEfficientType`, `YearOfConstruction (Datum)`, `Constructed`, `Calculation.BuildingCalcType`, `.BuildingCalcBasisType`, `.ThermalBridgeCalcType`, `Ventilation.AirExchangeRate50`, `.PressureExponent`, Anzahl Geschosse, Belegung, Wärmeschutz |
| `HSETU_EIMBauteilAllgemein` | PV-Modulen | `SeqNum`, `SymbolId` |
| `Pset_WallCommon`, `Pset_SlabCommon`, `Pset_RoofCommon`, `Pset_WindowCommon`, `Pset_DoorCommon` | Bauteilen | `LoadBearing`, **`ThermalTransmittance`** (hier mit der Einheit `W/(m K)` beschriftet — der Wert ist ein U-Wert) |

**Mengen** (`IfcElementQuantity`, 673): `HSETU_BauteilQuantities` (440: `Height`, `Width`,
`Length`, `Thickness`, `GrossArea`, `NetArea`, `CoatingArea`, `CoatingThickness`, `Volume`),
`HSETU_RaumQuantities` + `…Fußbodenfläche-`, `…Wandflächen-`, `…DeckenflächenQuantities` (je
56: `Area`, `Volume`, `Height`, `ClearHeight`, `FloorHeight`, `Perimeter`, `UsefulArea`,
`LivingArea`, `EnclosureSurface`, `FloorGeometry.*`), `HSETU_GeschossQuantities`,
`HSETU_Gebäude*Quantities`.

**Was in der IFC-Datei fehlt** (geprüft über alle Eigenschaftsnamen): `IfcZone`,
`IfcSpatialZone`, `IfcGroup` außer dem PV-System, `IfcTimeSeries`, `IfcWorkCalendar`,
Nutzungsprofile (kein Name mit „Profil“, „Nutzung“, „Usage“, „Schedule“, „Occupancy“ außer
`BuildingUsageType` am Gebäude), g-Werte (`GTot`, `SolarHeatGain`), Nachtabsenkung,
Personen-, Geräte-, Beleuchtungslasten, Klimareihen. Je Raum stehen nur **eine
Solltemperatur, der Mindestluftwechsel, Raumtyp und Beheizungsart** — der Befund hinter E72.

**IFC2X3-Fassungen** desselben Gebäudes (`Sportheim_Bestand-1970_test_ifc3.ifc`,
`…1997_test_ifc4.ifc`, beide `FILE_SCHEMA IFC2X3` trotz Namenszusatz): gleiche
Entitätszahlen (205/112/80/56/3), ohne PV-Module, Materialeigenschaften in den
IFC2X3-Klassen, `IfcPresentationStyleAssignment` statt direktem `IfcStyledItem`.


## 5. Folgerungen für EPOS-Plan und offene Punkte

**Für den IFC-Import (Mehrzonenkonzept Kapitel 6, Wellen Z6/ZB-1):**

- `ObjectType` = `TModel…` ist ein verlässlicher Klassifikator für HottCAD-Dateien; die
  Raum-Bauteil-Liste in `IfcRelReferencedInSpatialStructure` ersetzt die fehlenden
  Raumgrenzen für die Zuordnung Bauteil → Raum (nicht für Nachbarschaften, E73).
- `HSETU_RaumAllgemein.InsideTemperature`, `.HeatingType`, `.RoomType` tragen die
  Z6-Eingaben; `Pset_*Common.ThermalTransmittance` den U-Wert trotz falscher Einheitsangabe.
- Schichtaufbauten gibt es nur an Hüllbauteilen mit Körper; Innenbauteile kommen ohne
  Aufbau — die Rangfolge „Aufbau vor U-Wert“ fällt dort auf den U-Wert zurück.

**Für einen späteren `.sqproj`-Leseweg** (nicht beauftragt, hier nur die Lage):

- Mit `Microsoft.Data.Sqlite`, das der Kern ohnehin nutzt, sind **Nutzungsprofile mit
  DIN-V-18599-Profilnummer, 24-Stunden-Tagesganglinien je Profilklasse, Kalenderabschnitte
  und der 8 760-Stunden-Klimasatz** direkt lesbar — plattformfrei, ohne Fremdbibliothek.
  Das wäre der Weg, die „DIN-V-18599-Profile als spätere Welle“ (E79) aus Anwenderprojekten
  zu übernehmen statt sie nachzutippen.
- Räume, Geschosse, Zonen und die Raum→Zone-Zuordnung sind relational; **Wände, Fenster,
  Türen und Aufbauten sind es nicht** (3.5). Ein Leseweg Projektdatei + IFC-Export desselben
  Projekts ergänzt sich: Hülle aus IFC, Nutzung und Zeit aus der Projektdatei. Der
  Abgleich der Räume müsste über den Raumnamen oder — zu prüfen — über `BIMUUID` ↔
  `GlobalId` laufen.
- `XmTables.Version` erlaubt eine Fassungsprüfung vor dem Lesen.

**Offen (aus einer Datei nicht entscheidbar):** die Bedeutung der Codes `ZoneType`,
`ProfileType`, `ProfileSourceType`, `OperatingModeType`, `ProfileUsageDayType`,
`TaskPeriodType`; ob ungenutzte Zonen Vorlagen sind oder die Zuordnung im Binärstrom liegt;
ob `BIMUUID` die IFC-`GlobalId` ist; der Aufbau von `.BDExit`. Weitere Projektdateien mit
mehreren belegten Zonen und Wochentagskalendern würden die Codes festlegen.

**Was dieser Befund nicht enthält:** Werte. Keine Raumnamen, keine Profilbezeichnungen,
keine Kennzahlen, keine Hersteller- oder Typangaben, keine Adressen; die Skripte der
Auswertung lagen im Scratchpad und sind nicht im Repositorium.
