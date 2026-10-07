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
**Zweiter Durchgang:** Die Zahlencodes wurden an allen **sieben** Projektdateien des Ordners
geprüft (21 bis 211 MB, lokale Kopien unter `C:\Temp\IFC_Ganglinie_Beispiele`; die Dateien
tragen dasselbe Schema, `XmTables` bis Fassung 17.6). Kapitel 6 enthält die daraus belegten
Codetabellen; Kapitel 3.2 und 3.5 sind danach berichtigt — die zuerst untersuchte Datei war
in zwei Punkten ein Sonderfall.

Vorarbeiten, auf die dieser Befund aufsetzt: [Befund C (IFC-Recherche)](2026-09-15_Befund_C_IFC-Recherche.md),
[Befund N (IFC-Import-Entwurf)](2026-09-15_Befund_N_IFC-Import_Entwurf.md),
[Befund P (Zonen und Materialien in IFC)](2026-09-15_Befund_P_IFC_Zonen_Materialien.md),
[Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kapitel 6 und die Entscheide
E72, E73 und E79 der [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md).


## 0. Das Ergebnis in neun Punkten

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
3. **Zonen liegen in `BmZone`** (alle mit `ParentUUID` = Gebäude). `ZoneType` trennt
   mehrere Zonierungen desselben Gebäudes, die nebeneinander bestehen: **5 = DIN-V-18599-Zone**
   (trägt über `PdProfileReference` ein Nutzungsprofil), **6 = Simulationszone** (trägt über
   `ProfileGroupUUID` eine Profilgruppe mit zehn Zeitprofilklassen), **2 = Nutzungs- oder
   Wohneinheit** (Heizlast je Einheit), 7 = Lüftungszone, 10 = drei feste Systemzonen. Die
   Zuordnung Raum → Zone steht in **`BmZoneReference`** (`UUID` = Zone, `ReferenceToUUID` =
   Raum); in sechs der sieben Dateien ist sie vollständig — jeder Raum liegt in genau einer
   18599-Zone und einer Simulationszone, in Wohngebäuden zusätzlich in einer Wohneinheit.
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
6. **Bauteile liegen relational — in sechs von sieben Dateien vollständig.** `BmElement`
   führt jedes Bauteil auf zwei Ebenen: `RepositoryLevel` 2 ist das **CAD-Objekt** (eine
   Wand, Decke, Öffnung mit Geometrie-XML in `BmData`), `RepositoryLevel` 3 die
   **raumbezogene Hüllfläche** (ein Wandstück je angrenzendem Raum, mit `UValue`, Netto- und
   Bruttofläche, Orientierung, `AdjacentType` und dem Aufbau über `CatalogDimUUID`).
   `ElementType` unterscheidet Wand (1), Tür (2), Fenster (3), Geschossdecke (4), Dach (5),
   Bodenplatte (11), Öffnung (13), Verschattung (18), Luftdurchlass (19), je mit
   1:1-Zusatztabelle (`BmElementWall`, `BmElementWindow` mit g-Wert und Rahmenanteil,
   `BmElementDoor`, `BmElementStorey`, `BmElementRoof`, `BmElementBaseSlap`,
   `BmElementShadingDevice`, `BmElementOpening`, `BmElementAirPassage`). **Raum → Bauteil**
   steht in `BmElementReference` (`ReferenceFromUUID` = Raum, `ReferenceToUUID` = Hüllfläche,
   `ReferenceType` = Rolle am Raum, `AdjacentType`, `UValue`, `Fx`, Transmissionsverlust).
   **Aufbauten** liegen in `TcBuildingElementDimension` (U-Wert) mit Schichten in
   `TcBuildingElementDimensionLayer` (Dicke, λ, Dichte, Wärmekapazität). Die zuerst
   untersuchte Sportheim-Datei enthielt davon nur Decken, Dächer und Bodenplatten — ein
   Zwischenstand ohne gerechnete Hülle. Proprietär bleiben das DIN-18599-Rechenmodell
   (`WDIN18599DataModel`, `*.BDExit`-Tabellen in `PrMedia`) und das 3D-Grafikmodell (gzip in
   `GmMedia`); beides braucht ein Leser nicht.
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
9. **Der Schlüssel zwischen beiden Dateien ist `GId`:** Die IFC-`GlobalId` (22 Zeichen
   Base64) ist die kodierte Form der GUID in `BmRoom.GId` bzw. `BmElement.GId` (und des
   `@GUID` der Geometrie-XML). Am Wohngebäude geprüft: 20 von 20 Räumen, 3 von 3 Geschossen,
   alle 75 CAD-Wände und 31 von 32 Fenstern treffen. Raum- und Bauteilzuordnung zwischen
   IFC-Export und Projektdatei ist damit ohne Namensabgleich möglich.


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
`ParentUUID` am Gebäude (`ParentClassType` 4). `ZoneType` trennt **mehrere Zonierungen
desselben Gebäudes**, die nebeneinander bestehen; die Deutung ist an sieben Dateien belegt
(Kapitel 6.4):

| `ZoneType` | Verknüpfung | Deutung | Beleg |
|---|---|---|---|
| 5 | je Zone ein Nutzungsprofil (`PdProfile` Typ 2) über `PdProfileReference`; Räume über `BmZoneReference` | **DIN-V-18599-Zone** | in sechs Dateien decken die Typ-5-Zonen alle Räume genau einmal ab (1 bis 12 Zonen je Gebäude) |
| 6 | je Zone eine `PdProfileGroup` (`ProfileGroupUUID`) mit den zehn Zeitprofilklassen; Räume über `BmZoneReference`; `HeatingLoad` > 0 | **Simulationszone** mit Tagesganglinien | decken die Räume ebenfalls vollständig ab (2 bis 14 Zonen); Produktionsbau: 11 Typ-5- und 11 Typ-6-Zonen |
| 2 | Räume über `BmZoneReference` (4 bis 14 je Zone, kein Raum doppelt), Farbsatz, `Area` und `HeatingLoad` > 0 | **Nutzungs- oder Wohneinheit** (Heizlast je Einheit) | nur in den Wohngebäuden: 44 Einheiten im großen Mehrfamilienhaus, 30 und 12 in den anderen; im Produktionsbau keine |
| 7 | Nutzungs-, Personen- und Lüftungsprofil über `PdProfileReference`, wenige Räume | Lüftungszone (DIN 1946-6) | nur eine Datei (3 Zonen), Deutung aus den Profilklassen |
| 10 | nichts außer Farbe | feste Systemzonen | in **jeder** Datei genau 3 Zeilen |
| 0 | `SpaceType` 4, `ParentClassType` 17 (Geschoss) | Geschosszone, unbekannt | nur eine Datei, 3 Zeilen = 3 Geschosse mit Zonen |
| 8 | keine | unbekannt | nur Sportheim, 1 Zeile |

**Raum → Zone:** `BmZoneReference` (`UUID` = Zone, `ReferenceToUUID` = Raum,
`ReferenceClassType` 1, `ReferenceClass` `TModelRoom`); ein Raum steht mehrfach darin, einmal
je Zonierung. In sechs Dateien ist jeder Raum einer 18599-Zone und einer Simulationszone
zugeordnet. Die Sportheim-Datei ist der Sonderfall: 12 Typ-5- und 14 Typ-6-Zonen angelegt,
aber nur eine Simulationszone mit Räumen — ein Zwischenstand der Zonierung. Für den Import
nach E79 heißt das: Die Zuordnung ist relational lesbar, aber gegen die Raumliste zu prüfen
(„nicht zugeordnete Räume“).

**Schlüssel zum IFC-Export:** `BmRoom.GId`, `BmZone.GId`, `BmElement.GId` und das `@GUID` der
Geometrie-XML sind GUIDs; die IFC-`GlobalId` ist dieselbe GUID in der 22-stelligen
Base64-Kodierung (Kapitel 6.6). `BIMUUID` ist ein anderer Wert (Gebäude-GUID in
Klammerschreibweise) und **nicht** der IFC-Schlüssel.

### 3.3 Nutzungsprofile und Tagesganglinien

```
BmZone(ZoneType 5) ◄──ReferenceToUUID── PdProfileReference ──UUID──► PdProfile(ProfileType 2) ──UUID── PdProfileUsage
BmZone(ZoneType 6) ──ProfileGroupUUID──► PdProfileGroup ◄──UUID── PdProfileGroupReference ──ReferenceToUUID──► PdProfile(ProfileType 4…21)
                                                                                                                       │ UUID
                                                                             PdProfileTimeCurve (24 Zeilen) ◄──ProfileUUID──┤
                                                                             PdProfile<Klasse> (1:1) ◄──UUID────────────────┤
                                                                             PdProfileTaskSerialReference ──ReferenceToUUID─┘
```

**`PdProfile`** (372 Zeilen): `ProfileType` (Klassencode), `ProfileSourceType` (**4 = Normprofil**:
in sechs Dateien dieselben 44 Nutzungsprofile mit den Nummern 1 bis 71 der DIN V 18599-10 —
der eingebettete Normkatalog; **0 = Projektprofil**; die mit dem Altprojekte-Viewer gespeicherte
Datei trägt den Katalog nicht),
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
| 3 | 0 (2 in einer anderen Datei) | `PdProfileTariff`, dazu `PdProfileContractRateTariff` | — | nein |

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
(je Stunde **1 = Betriebsstunde**, **2 = außerhalb der Nutzungszeit**: Profile mit beiden
Werten führen 2 nachts und 1 von 10 bis 20 Uhr; durchgehend 2 heißt „nie in Betrieb“, etwa
Kühlung oder Sonnenschutz ohne Anlage, Kapitel 6.5), `Ratio` (0–1, bei Feuchte bis 0,4, bei Elektro bis 0,073),
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

**Vorbemerkung nach dem Vergleich der sieben Dateien:** Die Sportheim-Datei enthält nur
Decken, Dächer und Bodenplatten (47 Zeilen) und ist damit ein **Zwischenstand ohne
gerechnete Hülle**; die sechs anderen Dateien führen 337 bis 3 032 Bauteile mit Wänden,
Fenstern, Türen, Verschattungen und Aufbauten vollständig in Tabellen. Das Modell ist in allen
Dateien dasselbe und wird in **Kapitel 6.2 und 6.3** beschrieben (zwei Ebenen `RepositoryLevel`
2 und 3, Raumbezug in `BmElementReference`, Aufbauten in `TcBuildingElementDimension`). Der
folgende Absatz beschreibt die Spalten anhand der Sportheim-Datei; die Aussage „Wände nur in
Binärströmen“ gilt **allein für diesen Zwischenstand**.

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

In diesem Zwischenstand ist die Hülle aus der Projektdatei nur über proprietäre Formate zu
bekommen. In den sechs gerechneten Dateien liegt sie relational (Kapitel 6.2); der IFC-Export
liefert sie zusätzlich offen (Kapitel 4) und bleibt der Weg des Mehrzonenkonzepts. Der
IFC-Export des Sportheims (205 Wände) stammt vom 01.10.2026 und damit von einem späteren
Stand als diese Projektdatei vom 24.06.2026.

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
- Räume, Geschosse, Zonen, die Raum→Zone-Zuordnung **und in gerechneten Dateien auch die
  Hülle** (raumbezogene Bauteile mit U-Wert, Fläche, Orientierung, Randbedingung und Aufbau,
  Kapitel 6.2) sind relational. Ein `.sqproj`-Leser könnte damit mehr als der IFC-Import:
  Zonen, Nutzung, Zeit, Klima und Nachbarschaft (`AdjacentType` je Hüllfläche) in einem Zug.
  IFC-Export und Projektdatei desselben Projekts sind über `GId` ↔ `GlobalId` je Raum und
  Bauteil verknüpfbar (Kapitel 6.6).
- `XmTables.Version` erlaubt eine Fassungsprüfung vor dem Lesen.

**Geklärt am Vergleich der sieben Dateien (Kapitel 6):** `ZoneType`, `ProfileType`,
`ProfileSourceType`, `OperatingModeType`, `ElementType`, `RepositoryLevel`, `AdjacentType`
1/2/3/5, `ReferenceType`, `ReferenceClassType`, `RoomType`, `HeatingType`, der Schlüssel
`GId`. **Offen bleibt:** `ProfileUsageDayType` (4, 5, 6 — hängt nicht eindeutig an der
Profilnummer), `TaskPeriodType` 4 (je Datei höchstens ein Elektroprofil), `ZoneType` 0 und 8,
`AdjacentType` 6 und 7 (nur an Geschossdecken, vermutlich Dachraum und Keller), die
Herkunft der `PdProfileReference`-Zeilen auf Räume mit Quell-UUID außerhalb der Datei
(vermutlich globaler Profilkatalog), der Aufbau von `.BDExit`. Keine der sieben Dateien
nutzt Wochentags- oder Jahresabschnitte des Kalenders.

**Was dieser Befund nicht enthält:** Werte. Keine Raumnamen, keine Profilbezeichnungen,
keine Kennzahlen, keine Hersteller- oder Typangaben, keine Adressen; die Skripte der
Auswertung lagen im Scratchpad und sind nicht im Repositorium.


## 6. Codes, an sieben Projektdateien geprüft

### 6.1 Die sieben Dateien

Alle sieben tragen dasselbe Schema (688 Tabellen, `XmTables` bis 17.6) und wurden zuletzt am
05.10.2026 geöffnet (Journal). Sechs stammen aus dem „Energieberater 18599 3D PLUS“, eine
(bv4) wurde mit dem „Altprojekte-Viewer“ 12.4.7.2 gespeichert und trägt deshalb weder den
Normprofil-Katalog noch die 44 Standardprofile.

| Datei | Geschosse | Räume | Zonen (Typ 2 / 5 / 6 / 7 / 10) | Bauteile `BmElement` | Zeitprofile | Besonderheit |
|---|---|---|---|---|---|---|
| Sportheim (Kapitel 2 bis 4) | 3 | 56 | 5 / 12 / 14 / – / 3 (+1 Typ 8) | 47 | 305 | nur Decken, Dächer, Bodenplatten; eine Zone mit Räumen |
| Produktionsbau mit Verwaltung | 2 | 49 | – / 11 / 11 / – / 3 | 832 | 226 | PV-Anlage mit 1 107 Modulen in `EmPVModule`/`EmDevice` |
| Wohngebäude EH55 | 3 | 20 | – / 1 / – / – / 3 | 337 | 0 | keine Simulationszone, keine Zeitprofile, kein Klimasatz |
| Bauvorhaben 1 (bv1) | 6 | 106 | 12 / 1 / 2 / 3 / 3 (+3 Typ 0) | 1 671 | 50 | Lüftungszonen, 240 Raum-Profile (Person, Lüftung), Luftdurchlässe, Balkone |
| Bauvorhaben 2 (bv2) | 6 | 272 | 44 / 2 / 2 / – / 3 | 3 032 | 47 | 211 MB, davon rund 120 MB PNG-Bilder in `BmMedia` |
| Bauvorhaben 3 (bv3) | 6 | 196 | 30 / 1 / 2 / – / 3 | 2 489 | 35 | Rohrnetz-Schema (`NmSchemaElement`) |
| Bauvorhaben 4 (bv4) | 5 | 33 | – / 1 / 7 / – / 3 | 481 | 116 | Altprojekte-Viewer, Tarifprofile (`PdProfileTariff`) |

Die Dateigröße machen die eingebetteten Bilder (`BmMedia`, MediaType 15 = PNG, bis 31 MB je
Bild) und das gzip-Grafikmodell (`GmMedia`) aus, nicht die Fachtabellen.

### 6.2 Bauteile: zwei Ebenen, Raumbezug, Aufbau

```
BmElement (RepositoryLevel 2, RepositoryType 3)  ─ CAD-Objekt: eine Zeile je Wand, Decke, Dach, Öffnung,
    │   Dachfenster; Geometrie-XML in BmData (geometry/Wall, Platform, Roof, RoomElements, RoofWindow);
    │   ParentUUID → Trägerbauteil (Fenster/Tür → Wand, Dachfenster → Dach, Verschattung → Fenster)
    │
    └─ RepositoryElementUUID ◄── BmElement (RepositoryLevel 3, RepositoryType 1)  ─ raumbezogene Hüllfläche:
                                   ein Stück je angrenzendem Raum; UValue, GrossArea, NetArea, Orientation,
                                   AdjacentType; CatalogDimUUID → TcBuildingElementDimension (Aufbau)
                                   ▲
         BmRoom ──ReferenceFromUUID── BmElementReference ──ReferenceToUUID──┘
                 (ReferenceType = Rolle am Raum, AdjacentType, Orientation, UValue, Fx, Bu,
                  ThermalBridgeValue, TransmissionHeatLossCoefficient, TransmissionHeatLoss)

TcBuildingElementDimension (UValue, RValue, Thickness, FractionOfFrame, GValue, …)
    └─ DimensionUId ◄── TcBuildingElementDimensionLayer (LayerType, MaterialType, Thickness,
                         ThermalConductivity, Density, HeatCapacity, Emissivity, Diffusionswiderstand, MaterialUId)
```

Belegt an allen sechs gerechneten Dateien: jede Level-3-Zeile (außer Verschattungen) zeigt
mit `RepositoryElementUUID` auf genau eine Level-2-Zeile; jede Level-3-Zeile hat eine
`BmElementReference` von einem Raum; `UValue`, `NetArea`, `GrossArea` sind auf Level 3
durchgehend gefüllt, `CatalogDimUUID` trifft `TcBuildingElementDimension` bei 74 bis 100 %
der Hüllflächen; alle Aufbauten haben `UValue` > 0, alle Schichten `ThermalConductivity` und
`Thickness` > 0 (1 bis 24 Schichten je Aufbau). `AdjacentTemperature` trägt überall den
Platzhalter −987654321,99 („nicht gesetzt“). Eine Wand mit zwei Räumen hat zwei Level-3-Zeilen
mit demselben CAD-Objekt; ihr `AdjacentType` nennt die Beheizung des **anderen** Raums.

### 6.3 Codetabellen Bauteile

| Spalte | Code | Bedeutung | Beleg |
|---|---|---|---|
| `BmElement.ElementType` | 1 | Wand | 1:1 `BmElementWall`; XML `geometry/Wall` |
| | 2 | Tür | 1:1 `BmElementDoor`; XML `opening[@type='Door']` |
| | 3 | Fenster, auch Dachfenster | 1:1 `BmElementWindow`; `ParentElementType` 1 (Wand) oder 5 (Dach) |
| | 4 | Geschossdecke | 1:1 `BmElementStorey`; XML `Platform` |
| | 5 | Dach | 1:1 `BmElementRoof` |
| | 11 | Bodenplatte | 1:1 `BmElementBaseSlap`; `AdjacentType` 5 |
| | 13 | Öffnung ohne Füllung | 1:1 `BmElementOpening`; XML `opening[@type='Hole']` |
| | 18 | Verschattung | 1:1 `BmElementShadingDevice`; nur Level 3, `ParentElementType` 3 |
| | 19 | Luftdurchlass | 1:1 `BmElementAirPassage` |
| `RepositoryLevel` / `RepositoryType` | 2 / 3 | CAD-Objekt | hat Geometrie-XML, kein `BmElementReference` |
| | 3 / 1 | raumbezogene Hüllfläche | hat `BmElementReference`, `RepositoryElementUUID` → Level 2 |
| `AdjacentType` (Level 3, `BmElementReference`) | 1 | beheizter Nachbarraum | Nachbarraum `HeatingType` 1 in 926 von 960 Paaren |
| | 2 | unbeheizter Nachbarraum | Nachbarraum `HeatingType` 2 überwiegend; auch Pufferräume |
| | 3 | Außenluft | einseitige Wände, alle Fenster, alle Dächer; XML `outer_wall='True'` |
| | 5 | Erdreich | alle Bodenplatten, Kellerwände |
| | 6, 7 | weitere Randbedingungen | nur an Geschossdecken (6) bzw. Decken und wenigen Wänden (7); vermutlich Dachraum und Keller — **offen** |
| | 0 | ohne (CAD-Objekte, Verschattungen) | |
| `BmElementReference.ReferenceType` | 1 / 2 | Außenwand / Innenwand | Wand mit `AdjacentType` 3, 5 / 1, 2, 7 |
| | 3 / 4 | Außentür / Innentür | |
| | 5 / 6 | Fenster außen / Fenster gegen unbeheizt | |
| | 7 / 8 | Decke oben / Boden unten | 8 auch für Bodenplatten |
| | 9 | Dach | |
| | 10, 25 | Dachfenster (zwei Rollen) | je 36 im Produktionsbau = 36 `RoofWindow` |
| | 0 / 12 | Verschattung, Öffnung / Luftdurchlass | |
| `ReferenceClassType` (alle `*Reference`-Tabellen) | 1 / 2 / 4 / 11 / 16 / 17 | `TModelRoom` / `TModelZone` / `TModelBuilding` / `TModelProfile*` / `TModelBuildingElement` / `TModelFloor` | `ReferenceClass`-Text daneben; 16 aus `BmElementReference.ReferenceToType`, 17 aus `BmBuildingSpace.ParentElementType` |
| XML `wall/@wall_type` | `Inner`, `OuterContour`, `OuterSingle` | Innenwand, Außenwand der Kontur, freistehende Außenwand | 1 624 Wände |
| XML `roof/@type` | `Polygonal`, `Flat`, `Saddleback`, `External` | Dachform | |
| XML `floor/@type` | `Standard`, `Cellar`, `Roof` | Geschossart | |
| XML `opening/@type`, `@form` | `Window`, `Door`, `Hole`; `formRechteck`, `formKreis`, `formPolygon` | | |
| XML `platform/@against_air`, `@against_soil`, `@against_unheated` | `True`/`False` | Randbedingung der Platte | deckt sich mit `AdjacentType` 3 / 5 / 2 |

### 6.4 Codetabellen Räume und Zonen

`BmRoom.RoomType` ist der Zahlencode zum String `room_type` der Geometrie-XML (Präfix `mrt`);
derselbe String steht im IFC-Export in `HSETU_RaumAllgemein.RoomType`. Belegt an 732 Räumen:

| Code | `mrt…` | Code | `mrt…` | Code | `mrt…` |
|---|---|---|---|---|---|
| 0 | None | 11 | Bath | 31 | Fitness |
| 1 | Living | 12 | WC | 33 | Locker |
| 2 | Sleeping | 14 | Office | 34 | Store |
| 3 | Child | 15 | Conference | 35 | Storage |
| 4 | Kitchen | 23 | Basement | 36 | Connection |
| 5 | Eating | 24 | CentralHeating | 41 | Workshop |
| 6 | Hall | 25 | Roof | 42 | Garage |
| 7 | Guests | 26 | Stairway | 44 | Wintergarden |
| 9 | AdjoiningRoom | 30 | Sauna | 52 | HallWay |
| 10 | StorageRoom | | | 53 | Shaft |

`BmRoom.HeatingType`: **1 = `Heated`, 2 = `Unheated`, 4 = `SeparatelyHeated`** (XML
`heating_type`, IFC `bht…`). `BmRoom.SpaceType` ist immer 1, `BmZone.SpaceType` immer 2
(Raum gegen Zone), `BmZone.ParentClassType` 4 = Gebäude, 17 = Geschoss. `ZoneType` siehe 3.2.
`BmBuildingSpace.ParentElementType`: 1 Raum, 4 Gebäude, 17 Geschoss.

### 6.5 Codetabellen Profile und Kalender

| Spalte | Code | Bedeutung | Beleg |
|---|---|---|---|
| `PdProfile.ProfileType` | 2 | Nutzungsprofil DIN V 18599-10 | 1:1 `PdProfileUsage`, Verweis von Typ-5-Zonen |
| | 3 | Tarif | 1:1 `PdProfileTariff` (eine Datei) |
| | 4 / 5 / 6 / 7 / 8 / 9 / 10 | Beleuchtung / Sonnenschutz / Heizung / Kühlung / Personen / Geräte / Lüftung | 1:1-Zusatztabelle und `ReferenceClass` in allen Dateien gleich |
| | 14 / 16 / 21 | Trinkwasser / Elektro / Feuchte | ebenso |
| | 13 / 20 | Bauteil-Vorgabeprofil / Ventil | je eine Zeile, nur Sportheim |
| `ProfileSourceType` | 4 / 0 | Normprofil des eingebetteten Katalogs / Projektprofil | 44 Normprofile mit identischen Nummern in sechs Dateien |
| `PdProfileUsage.ProfileUsageType` | 1 … 71 | Profilnummer nach DIN V 18599-10 | 44 verschiedene Nummern, Bereich 1–47 und 70–71 |
| `PdProfileTimeCurve.OperatingModeType` | 1 / 2 | Betriebsstunde / außerhalb der Nutzungszeit | gemischte Profile: 2 in den Stunden 1–9 und 21–24, 1 von 10 bis 20; Mode-2-Stunden haben `Ratio` 0 und Absenk- oder Nullwerte |
| `PdProfileTimeCurve.HourType`, `CurveId` | 1 … 24 | Stunde des Tages | in allen Dateien genau 24 Zeilen je Profil |
| `PdProfileGroup.ProfileGroupType` | 4 / 5 | Gebäudegruppe (eine je Datei, `BmBuilding.ProfileGroupUUID`) / Zonengruppe (`BmZone.ProfileGroupUUID`) | |
| `PdProfileGroup.ProfileUsageType` | 1 … 71 | Profilnummer der Zone | |
| `PdProfileGroup.ProfileUsageDayType` | 4 / 5 / 6 | Tagesart, vermutlich 5-, 6-, 7-Tage-Woche | **offen**: Profilnummer 19 kommt mit 4 und 6 vor |
| `PdProfileTaskSerial.TaskPeriodType` | 1 / 4 | Jahresabschnitt / unbekannt | 4 nur je ein Elektroprofil in zwei Dateien |
| `TaskStartDay`, `TaskEndDay`, `TaskMonday … TaskSunday` | | Gültigkeit | in allen sieben Dateien 1–365 und alle Schalter 0 — Wochentags- und Jahresabschnitte sind im Modell vorgesehen, aber ungenutzt |

### 6.6 Schlüssel zum IFC-Export

Die IFC-`GlobalId` ist die 22-stellige Base64-Form (Zeichensatz `0–9 A–Z a–z _ $`) einer
128-Bit-GUID. Dekodiert trifft sie am Wohngebäude EH55 (IFC4, 20 Räume) **`BmRoom.GId`** bei
20 von 20 Räumen, das `@GUID` der Geometrie-XML bei 20 Räumen und 3 Geschossen, **`BmElement.GId`**
bei 75 von 75 CAD-Wänden (Level 2), 31 von 32 Fenstern, 11 Platten und 8 Dächern. Die 117
`IfcWall` des Exports sind also die 75 CAD-Wände plus abgeleitete Stücke mit eigenen GUIDs.
`BmRoom.UUID` und `BIMUUID` treffen nichts. Ein Leser, der Projektdatei und IFC-Export
zusammenführt, schlüsselt über `GId`.


## Nachtrag BA-4a — Bauteilaufbauten (06.10.2026)

**Auftrag:** Diagnose vor BA-4 nach [Konzept Bauteilaufbau](2026-10-06_Konzept_Bauteilaufbau_Import.md)
3.2 und 5.5 (Entscheid E95 (6)): Codes von `LayerType` und `MaterialType`, Einheit von
`HeatCapacity`, Richtung der Schichtfolge, Rolle der Zusatzdämmung `AddIns…`, Deckung der
Zuordnung zum IFC-Export je Bauteilart, U-Abgleich, Belegung der Stoffwerte und Fassung — an den
lokalen Projektdateien neben den sechs IFC-Dateien unter `Quellen/`.
**Methode:** Projektdatei nur lesend geöffnet (`mode=ro&immutable=1`), Schema über `sqlite_master`
und `PRAGMA table_info`. Ein Auswerteskript außerhalb des Repositoriums läuft über alle
`Quellen/*.sqproj` und paart über den Dateistamm; IFC-Dateien als STEP-Text mit Regex zerlegt,
`GlobalId` in beiden Bytefolgen zur GUID dekodiert. Dazu der Diagnosetest
`SqprojQuelldateienDiagnoseTests`. Wiedergegeben sind nur Zählungen, Anteile, Spannweiten und
Abweichungen — keine Material-, Produkt- oder Raumnamen.

**Das Ergebnis in sechs Punkten:**

1. **Eine Projektdatei:** Nur zum Sportheim gibt es eine `.sqproj`. Sie ist ein gerechneter Stand
   mit Wänden, Fenstern und Aufbauten in Tabellen, nicht die Datei aus Kapitel 2 bis 4. Alle
   Zahlen dieses Nachtrags stammen aus ihr.
2. **Codes und Einheiten:** `LayerType`/`MaterialType` 3/1 = Dämmschicht, 0/0 = übrige Schicht;
   `MaterialGroupType` 2 = Beton, 4 = Mauerwerk oder Leichtbeton, 5 = Dämmstoff. `HeatCapacity`
   steht in **kJ/(kg·K)**; `InternalCoefficientOfHeatTransfer` und
   `ExternalCoefficientOfHeatTransfer` des Aufbaus tragen **Rsi und Rse in m²K/W**.
3. **Schichtfolge:** nach `SortNum` aufsteigend **von innen nach außen**; die Zeilenfolge der
   Tabelle ist nicht die Schichtfolge.
4. **U geht auf:** U = 1/(Rsi + Σ d/λ + Rse) mit Rsi und Rse des Aufbaus trifft alle 24
   Aufbauten mit Schichten und das `UValue` aller 356 opaken Hüllflächen. `AddIns…` ist in keiner
   Zeile gesetzt; seine Rolle bleibt offen.
5. **Schlüssel zum IFC-Export ist die Eigenschaft `HSETU_BauteilAllgemein.GUID`, nicht die
   `GlobalId`:** Sie trifft 439 von 440 Hüllflächen (Level 3) und 56 von 56 Räumen, die
   `GlobalId` nur 3 CAD-Decken. Der Export schreibt ein IFC-Bauteil je Hüllfläche. Alle sechs
   IFC-Dateien tragen die Eigenschaft an allen Wänden, Platten, Dächern, Fenstern, Türen und
   Räumen.
6. **Der U-Abgleich zeigt einen Standunterschied:** 189 von 439 Paaren stimmen auf 1 %; die
   IFC-Datei bildet einen späteren Planungsstand mit anderen Aufbauten ab. Der Vorrang der
   Projektdatei (E95 (6)) gilt deshalb erst nach einer Standprüfung je Bauteil.

### N.1 Datenlage und Paarung

| Gebäude | IFC-Datei | Projektdatei |
|---|---|---|
| MFH 1964 | IFC2X3 | Projektdatei nicht vorhanden |
| MFH 1984 | IFC4 | Projektdatei nicht vorhanden |
| Sportheim | IFC4, geschrieben am 01.10.2026 | vorhanden (34 MB), Bauteile zuletzt am 23.06.2026 bearbeitet |
| Verwaltung | IFC2X3 | Projektdatei nicht vorhanden |
| WG-EH55 | IFC4 | Projektdatei nicht vorhanden |
| Produktion | IFC2X3 | Projektdatei nicht vorhanden |

Zu den fünf übrigen IFC-Dateien gibt es nach Auskunft des Anwenders keine Projektdatei. Die
Sportheim-Datei unter `Quellen/` ist nicht die Datei aus Kapitel 2 bis 4 (21,5 MB, 47
Bauteilzeilen), sondern ein gerechneter Stand desselben Projekts:

- `BmElement`: 186 CAD-Objekte (Level 2: 92 Wände, 78 Fenster, 3 Türen, 8 Geschossdecken,
  3 Dächer, 2 Bodenplatten), 440 Hüllflächen (Level 3) und 49 Verschattungen — diese hier mit
  `RepositoryLevel` 0, nicht 3 wie in Kapitel 6.3;
- jede Hüllfläche zeigt auf ihr CAD-Objekt, jedes CAD-Objekt hat mindestens eine Hüllfläche;
- ein Innenwand- oder Deckenstück zwischen zwei Räumen ist **eine** Level-3-Zeile mit **zwei**
  `BmElementReference` (120 von 120 Innenwandstücken, 75 von 89 Deckenstücken), nicht zwei
  Zeilen wie in Kapitel 6.2;
- 29 Aufbauten in `TcBuildingElementDimension`: 24 mit Schichten (4 ein-, 15 zwei-,
  5 dreischichtig, zusammen 49 Schichten) und 5 ohne Schichten (Fenster und Türen: nur U, g,
  Rahmenanteil). Nur 8 Aufbauten sind Hüllflächen zugeordnet, 21 stehen ohne Zuordnung in der
  Datei.

### N.2 Schichtcodes und Stoffgruppen (a)

Die Bedeutung folgt aus Dicke, λ und Rohdichte (Spannweiten Min–Max):

| `LayerType` | `MaterialType` | `MaterialGroupType` | Schichten | Stoffgruppe | Dicke in m | λ in W/(m·K) | Rohdichte in kg/m³ |
|---|---|---|---|---|---|---|---|
| 0 | 0 | 2 | 18 | Beton | 0,16–0,50 | 1,65–2,5 | 2 200–2 400 |
| 0 | 0 | 4 | 6 | Mauerwerk oder Leichtbeton | 0,12–0,24 | 0,27–0,28 | 550–600 |
| 3 | 1 | 5 | 25 | Dämmstoff | 0,04–0,20 | 0,024–0,045 | 25–60 |

- `LayerType` und `MaterialType` teilen die Schichten gleich: **3/1 = Dämmschicht,
  0/0 = übrige Schicht**. `MaterialGroupType` nennt die Stoffgruppe.
- `ScreedType`, `VentilatedType` und `ThicknessType` sind überall 0, `MaterialClassDesc` ist
  leer. `IsVapourDiffusionTight` ist bei 29 Schichten aus allen drei Gruppen gesetzt und kein
  Gruppencode.
- Putz, Estrich, Beläge, Holz, Luftschichten, Folien und Abdichtungen kommen nicht vor. Die
  Aufbauten dieser Datei sind ein- bis dreischichtige Rechenaufbauten aus tragender Schicht und
  Dämmung; die Codes der übrigen Stoffgruppen bleiben offen.

### N.3 Einheit von `HeatCapacity` (b)

| Stoffgruppe | Schichten | `HeatCapacity` Min / Median / Max | plausibel in J/(kg·K) |
|---|---|---|---|
| Beton | 18 | 1,0 / 1,0 / 1,0 | rund 1 000 |
| Mauerwerk oder Leichtbeton | 6 | 1,0 / 1,0 / 1,0 | rund 1 000 |
| Dämmstoff | 25 | 1,0 / 1,0 / 1,5 | rund 1 000 bis 1 500, je nach Dämmstoffart |

- **`HeatCapacity` steht in kJ/(kg·K).** Die Spalte kennt nur 1,0 (41 Schichten) und 1,5
  (8 Schichten, alle Dämmstoff). In J/(kg·K) gelesen wären die Werte um den Faktor 1 000 zu
  klein; das Band 100–5 000 J/(kg·K) aus Konzept 5.5 fängt das, der Leser rechnet × 1 000.
- Die übrigen Einheiten: Dicke in m (die Schichtdicken summieren sich bei 24 von 24 Aufbauten
  zur `Thickness` des Aufbaus), λ in W/(m·K), Rohdichte in kg/m³, `RValue` der Schicht = d/λ
  in m²K/W (49 von 49).
- **`InternalCoefficientOfHeatTransfer` und `ExternalCoefficientOfHeatTransfer` tragen trotz des
  Namens Wärmeübergangswiderstände in m²K/W:** Rsi 0,10, 0,13 oder 0,17, Rse 0, 0,04, 0,10 oder
  0,13. Fenster- und Türaufbauten haben dort den Platzhalter −987654321,99; `WValue` und
  `UnValue` stehen bei allen Aufbauten auf dem Platzhalter, `RawThickness` und `FinalThickness`
  auf Platzhalter oder 0.

### N.4 Richtung der Schichtfolge (c)

Die Folge steht in `SortNum` (0, 1, 2 …); die Zeilenfolge der Tabelle weicht in 13 von 20
mehrschichtigen Aufbauten davon ab. Lage der Dämmschichten in den 20 mehrschichtigen Aufbauten,
geordnet nach Rsi und Rse des Aufbaus:

| Lage (Rsi / Rse in m²K/W) | Aufbauten | Dämmung nur bei `SortNum` 0 | Dämmung nur zuletzt | Dämmung an beiden Enden |
|---|---|---|---|---|
| Wand gegen Außenluft (0,13 / 0,04) | 2 | – | 2 | – |
| Dach oder Decke gegen Außenluft (0,10 / 0,04) | 5 | – | 5 | – |
| Boden gegen Außenluft (0,17 / 0,04) | 1 | – | 1 | – |
| Wand gegen Erdreich (0,13 / 0) | 7 | 2 | 2 | 3 |
| Boden gegen Erdreich (0,17 / 0) | 1 | – | 1 | – |
| Decke zwischen Innenräumen (0,10 / 0,10) | 4 | 2 | – | 2 |

- **Lesart: `SortNum` 0 ist die Innenseite (Rsi), die Folge läuft nach außen (Rse).** In allen
  8 mehrschichtigen Aufbauten gegen Außenluft steht die tragende Schicht bei `SortNum` 0 und die
  Dämmung zuletzt: Außendämmung der Wand, Dämmung über der Dachdecke, Dämmung unter der Decke
  über Außenluft. Umgekehrt gelesen wären alle acht innen gedämmt, das Dach mit der tragenden
  Decke außen.
- Erdberührte Wände tragen Dämmung außen, innen oder auf beiden Seiten (Zusatzdämmung innen vor
  einer äußeren Dämmung); das ist mit der Lesart verträglich, belegt sie aber nicht. Bei den
  Decken zwischen Innenräumen steht die Dämmschicht bei `SortNum` 0 — als Dämmung auf der
  Rohdecke gelesen, ist die Decke von oben beschrieben.
- Ein zweites Merkmal wie Innenputz oder Belag fehlt, weil die Aufbauten keine solchen Schichten
  tragen: Die Richtung ist plausibel belegt, nicht bewiesen.

### N.5 Zusatzdämmung `AddIns…` und U aus den Schichten (d)

- **`AddIns…` ist in dieser Datei nicht belegt.** Dicke und λ innen und außen stehen in allen 675
  Zeilen von `BmElement` auf dem Platzhalter −987654321,99. `AddInsInSideUUID` trägt in allen
  Zeilen denselben Wert, `AddInsOutSideUUID` 617 verschiedene Werte ohne Treffer in
  `TcBuildingElementDimension`; die Verweise `…CatalogInsulationDimensionUUID` treffen keinen
  Aufbau.
- U = 1/(Rsi + Σ d/λ + Rse) nach DIN EN ISO 6946 in drei Varianten:

| Rechnung | verglichen mit | Ergebnis |
|---|---|---|
| Rsi und Rse des Aufbaus, ohne Zusatzdämmung | U des Aufbaus (24 Aufbauten mit Schichten) | 24 von 24, Abweichung < 10⁻¹⁴ |
| dasselbe | `UValue` der Hüllfläche (356 opake Hüllflächen) | 356 von 356, Abweichung < 10⁻¹⁴ |
| Rsi und Rse nach Lage (Wand 0,13/0,04, Dach 0,10/0,04, Boden 0,17/0,04, erdberührt Rse 0, zwischen Innenräumen Rse = Rsi) | `UValue` der Hüllfläche | 332 von 356 innerhalb 1 %; 24 bis 14,6 % daneben: 12 erdberührte Wandstücke mit dem Außenwandaufbau (Rse 0,04), 6 Außenwandstücke mit dem Innenwandaufbau (Rse 0,13), 6 Dachstücke mit einem Deckenaufbau (Rse 0,10) |
| mit Zusatzdämmung | — | nicht prüfbar, keine gesetzt |

- **`UValue` der Hüllfläche ist das U ihres Aufbaus** mit dessen eigenem Rsi und Rse, nicht nach
  der Lage am Raum nachgerechnet. Das `UValue` des CAD-Objekts ist dasselbe (403 von 440
  Hüllflächen; bei 37 steht am CAD-Objekt der Platzhalter).
- Ob die Zusatzdämmung im Aufbau steckt oder dazukommt, lässt sich an dieser Datei nicht
  entscheiden. Ein Hinweis: Der Beschreibungstext des CAD-Satzes im IFC-Export nennt den U-Wert
  „inklusive der Zusatzdämmungen“ (Konzept 3.1) — das spräche für „kommt dazu“.

### N.6 Zuordnung zum IFC-Export (e)

| Bauteilart (Level 3) | Hüllflächen | → CAD-Objekt | → Aufbau | davon mit Schichten | → IFC über `HSETU_BauteilAllgemein.GUID` | → IFC über `GlobalId` |
|---|---|---|---|---|---|---|
| Außenwand (`AdjacentType` 3, 5) | 85 | 85 | 85 | 85 | 84 | 0 |
| Innenwand (`AdjacentType` 1, 2) | 120 | 120 | 120 | 120 | 120 | 0 |
| Dach | 40 | 40 | 40 | 40 | 40 | 0 |
| Geschossdecke | 89 | 89 | 89 | 89 | 89 | 12 |
| Bodenplatte | 22 | 22 | 22 | 22 | 22 | 0 |
| Fenster | 80 | 80 | 80 | – | 80 | 0 |
| Tür | 4 | 4 | 4 | – | 4 | 0 |
| **Summe** | **440** | **440** | **440** | **356** | **439** | **12** |

- **Die `GlobalId` trägt in diesem Export nicht:** In beiden Bytefolgen dekodiert trifft sie nur
  3 der 112 `IfcSlab` (CAD-Decken, darüber die 12 Deckenstücke der Tabelle) und den Standort,
  aber keine `IfcWall`, `IfcRoof`, `IfcWindow`, `IfcDoor` und keinen `IfcSpace`.
- **Jede `IfcWall`, `IfcSlab`, `IfcRoof`, `IfcWindow`, `IfcDoor` und jeder `IfcSpace` trägt die
  Eigenschaft `HSETU_BauteilAllgemein.GUID`** mit der GUID im Klartext (die 49
  `IfcBuildingElementProxy` tragen sie nicht). Sie ist das `GId` der **Hüllfläche (Level 3)**,
  nicht des CAD-Objekts: 204 von 205 `IfcWall`, 112 von 112 `IfcSlab` (89 Decken-,
  22 Bodenplatten- und 1 Dachstück), 39 von 39 `IfcRoof`, 80 von 80 `IfcWindow`, 4 von 4
  `IfcDoor`; an den 56 `IfcSpace` ist es `BmRoom.GId` (56 von 56). Der Export schreibt also ein
  IFC-Bauteil je Hüllfläche: 205 Wandstücke, 205 `IfcWall`.
- **IFC-Wandstücke ohne eigene GId (Kapitel 6.6):** über die `GlobalId` 205 von 205, über die
  Eigenschaft 1 von 205.
- Gegenprobe an allen sechs IFC-Dateien, ohne Projektdatei:

| IFC | Schema | Wände mit Eigenschaft | davon `GlobalId` = Eigenschaft | Räume mit Eigenschaft | davon `GlobalId` = Eigenschaft | Platten, Dächer, Fenster, Türen mit Eigenschaft | davon `GlobalId` = Eigenschaft |
|---|---|---|---|---|---|---|---|
| MFH 1964 | IFC2X3 | 125 von 125 | 0 | 30 von 30 | 0 | 91 von 91 | 0 |
| MFH 1984 | IFC4 | 120 von 120 | 5 | 29 von 29 | 29 | 104 von 104 | 2 |
| Sportheim | IFC4 | 205 von 205 | 0 | 56 von 56 | 0 | 235 von 235 | 0 |
| Verwaltung | IFC2X3 | 428 von 428 | 0 | 113 von 113 | 0 | 375 von 375 | 0 |
| WG-EH55 | IFC4 | 117 von 117 | 0 | 20 von 20 | 20 | 95 von 95 | 1 |
| Produktion | IFC2X3 | 269 von 269 | 0 | 49 von 49 | 0 | 246 von 246 | 0 |

  Die Eigenschaft steht in allen sechs Exporten an jedem dieser Bauteile und Räume; die
  `GlobalId` gleicht ihr nur bei den Räumen zweier IFC4-Exporte. Beim WG-EH55 trifft die
  `GlobalId` nach Kapitel 6.6 die 75 CAD-Wände, die Eigenschaft weicht bei allen 117 Wänden von
  der `GlobalId` ab — vermutlich ist sie auch dort das `GId` der Hüllfläche; prüfbar erst mit der
  Projektdatei.
- Folge für den Raumabgleich: `SqprojRaumabgleich` (`GId` ↔ `GlobalId`, dann Raumname) paart im
  Diagnosetest 0 von 56 Räumen; über die Eigenschaft wären es 56 von 56. Weder der IFC-Leser noch
  der Raumabgleich lesen die Eigenschaft heute.

### N.7 U-Abgleich Projektdatei ↔ IFC (f)

Je Paar aus N.6 (über die Eigenschaft) das `UValue` der Hüllfläche gegen das U des IFC-Bauteils:
`Pset_*Common.ThermalTransmittance`, bei 90 Platten ohne diesen Satz
`HSETU_Bauteilreferenzen.UValue` — wo beide stehen, sind sie gleich (350 von 350). Abweichung
|U(IFC) / U(Projektdatei) − 1|:

| Datei, Bauteilart | Paare | Median | 90-%-Wert | innerhalb 1 % |
|---|---|---|---|---|
| Sportheim, Außenwand | 84 | 84,8 % | 84,8 % | 5 |
| Sportheim, Innenwand | 120 | 0 | 0 | 120 |
| Sportheim, Dach | 40 | 72,6 % | 342 % | 0 |
| Sportheim, Geschossdecke | 89 | 73,7 % | 342 % | 30 |
| Sportheim, Bodenplatte | 22 | 0 | 0 | 22 |
| Sportheim, Fenster | 80 | 52,6 % | 52,6 % | 9 |
| Sportheim, Tür | 4 | 0 | 8,8 % | 3 |
| **Sportheim gesamt** | **439** | **20,2 %** | **84,8 %** | **189** |
| MFH 1964, MFH 1984, Verwaltung, WG-EH55, Produktion | – | – | – | Projektdatei nicht vorhanden |

- **Die Abweichungen sind nach allen Anzeichen ein Standunterschied, kein Lesefehler.** Die
  IFC-Datei ist am 01.10.2026 geschrieben, die Bauteile der Projektdatei wurden zuletzt am
  23.06.2026 bearbeitet. Alle 15 verschiedenen U-Werte der IFC-Datei sind U-Werte von Aufbauten
  der Projektdatei, 9 davon nur von Aufbauten ohne Zuordnung zu einer Hüllfläche. Außenwände,
  Dach, Fenster und ein Teil der Decken tragen im IFC-Stand andere Aufbauten; Innenwände und
  Bodenplatten sind unverändert.

### N.8 Belegung und Innenaufbauten (g)

| Größe je Schicht | Schichten mit Wert | Anteil |
|---|---|---|
| Dicke | 49 von 49 | 100 % |
| λ | 49 von 49 | 100 % |
| Rohdichte | 49 von 49 | 100 % |
| spezifische Wärmekapazität | 49 von 49 | 100 % |
| Emissionsgrad, Diffusionswiderstandszahl | 0 von 49 | 0 % |

- **Innenaufbauten: ja.** 120 Innenwandstücke und 89 Deckenstücke tragen einen Aufbau mit
  Schichten (ein Innenwand- und zwei Deckenaufbauten), dazu Bodenplatten und erdberührte Wände.
- Fenster und Türen haben Aufbauten ohne Schichten (U, g, Rahmenanteil).

### N.9 Fassung (h) und Diagnosetest

| Projektdatei | Tabellen | `BmRoom` (Fassung des Lesers) | `BmElement` | `BmElementReference` | `TcBuildingElementDimension` | `TcBuildingElementDimensionLayer` | höchste in `XmTables` | Programmfassung |
|---|---|---|---|---|---|---|---|---|
| Sportheim | 686 | 15.3 | 16.7 | 16.2 | 15.5 | 15.2 | 17.3 | 12.4.3.1 |
| MFH 1964, MFH 1984, Verwaltung, WG-EH55, Produktion | Projektdatei nicht vorhanden | – | – | – | – | – | – | – |

- Der Leser nimmt die Datei an („Fassung 15.3“). Die sieben Dateien aus Kapitel 6 tragen
  688 Tabellen bis Fassung 17.6, diese 686 bis 17.3; der Leser für BA-4 prüft daher die
  benötigten Spalten über `PRAGMA table_info`, nicht feste Fassungsnummern.
- Diagnosetest `SqprojQuelldateienDiagnoseTests` (grün): 56 Räume, 25 Zonen vom Typ 5 und 6
  (24 mit Raum), 66 Zeitprofile, 66 Abschnitte. Raumabgleich mit der IFC-Datei: 0 von 56 (über
  die Kennung 0, über den Namen 0), 56 IFC-Räume ohne Gegenstück. Zonenübernahme: keine Zone
  übernommen, 12 Zonen leer, 56 Räume nicht zugeordnet — Folge des fehlenden Raumabgleichs.

### N.10 Folgerung für den Leser (BA-4)

**Die Diagnose beruht auf einer einzigen Projektdatei.** Übertragbar sind die Eigenschaften des
Datenmodells: Schema und Verknüpfungen (Level 2 und 3, `CatalogDimUUID`, `DimensionUId`,
`SortNum`), die Einheiten und — mit dem Vorbehalt aus N.4 — die Richtung der Schichtfolge;
ebenso die Eigenschaft `HSETU_BauteilAllgemein.GUID`, die alle sechs Exporte tragen. **Nicht
übertragbar** sind die Zahlen dieser Datei: die Deckungsquoten (hier 100 % Aufbau je Hüllfläche,
439 von 440 IFC-Paaren), die U-Abweichungen (hier ein Standunterschied), die Codeliste (hier nur
drei Stoffgruppen) und die Rolle von `AddIns…` (hier nicht belegt).

1. **Einheiten:** Dicke in m, λ in W/(m·K), Rohdichte in kg/m³, c = `HeatCapacity` × 1 000 in
   J/(kg·K); Rsi und Rse aus `InternalCoefficientOfHeatTransfer` und
   `ExternalCoefficientOfHeatTransfer`; −987654321,99 heißt „nicht gesetzt“.
2. **Schichtfolge:** nach `SortNum` sortieren, `SortNum` 0 ist innen.
3. **Zuordnung:** IFC-Bauteil → Hüllfläche über `HSETU_BauteilAllgemein.GUID` = `BmElement.GId`
   (Level 3), erst danach über die dekodierte `GlobalId` (Level 2 oder 3); der Aufbau kommt
   direkt aus `CatalogDimUUID` der Hüllfläche. Die Vererbung über Raum und Rolle (Konzept 5.5)
   braucht nur ein Stück ohne beide Treffer (hier 1 von 205 Wänden). Derselbe Schlüssel gehört
   in den Raumabgleich.
4. **Standprüfung je Paar:** Weicht das U des IFC-Bauteils um mehr als 1 % vom `UValue` der
   Hüllfläche ab, beschreibt die Projektdatei einen anderen Stand: Der Aufbau wird nicht
   übernommen, das Bauteil bleibt auf seiner IFC-Stufe, das Protokoll nennt es. Ein Aufbau der
   Projektdatei mit passendem U wird nicht geraten.
5. **Zusatzdämmung:** Ist `AddIns…` gesetzt, prüft der Leser selbst — U aus den Schichten mit und
   ohne Zusatzdämmung gegen `UValue`; trifft genau eine Variante, gilt sie, sonst nennt das
   Protokoll das Bauteil.
6. **Rangfolge (E95 (6)):** bestätigt, mit Standprüfung. Die Projektdatei liefert c,
   Innenaufbauten und Schichten, deren U exakt aufgeht. Im Sportheim-Paar bestünden 177 von 355
   opaken Paaren die Standprüfung (Innenwände, Bodenplatten, 30 Deckenstücke, 5
   Außenwandstücke); die übrigen blieben auf der IFC-Stufe.

### N.11 Offene Punkte

1. **Weitere Projektdateien im selben Stand wie ihr IFC-Export** (beide aus demselben
   gespeicherten Stand): Deckung und U-Abgleich ohne Standunterschied, Codes weiterer
   Stoffgruppen (Putz, Estrich, Holz, Luftschicht, Folie), Rolle von `AddIns…`, Richtung an
   Aufbauten mit Putz.
2. **WG-EH55:** ob `HSETU_BauteilAllgemein.GUID` dort das `GId` der Hüllfläche ist (Kapitel 6.6
   fand die `GlobalId` gleich dem `GId` der CAD-Wand).
3. **Raumabgleich über die Eigenschaft** (`SqprojRaumabgleich`, am Sportheim heute 0 von 56) —
   eigener Auftrag, gehört vor BA-4.
4. **Abweichungen gegenüber Kapitel 6:** Verschattungen mit `RepositoryLevel` 0 statt 3,
   Innenwand- und Deckenstücke als eine Zeile mit zwei Raumbezügen, 686 statt 688 Tabellen —
   Fassungs- oder Projektunterschied, offen.
5. `AdjacentType` 6 und 7 (Kapitel 6.3) bleiben offen; hier an 2 und 36 Deckenstücken.

Umgesetzt in BA-4b (#788) und Fix Raumabgleich (#788), siehe [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-07_BA-4b_Aufbauten_Projektdatei.md).
