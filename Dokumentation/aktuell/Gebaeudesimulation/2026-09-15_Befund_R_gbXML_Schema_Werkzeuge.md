# Befund R — gbXML: Schema, Import- und Exportverhalten der Werkzeuge (15.09.2026)

**Protokoll.** Befund R eines Recherche-Agenten (Modell Opus) im Auftrag des Konzepts
[`../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md),
Sitzung vom 15.09.2026.

Vertieft Abschnitt 3 von [`2026-09-15_Befund_C_IFC-Recherche.md`](2026-09-15_Befund_C_IFC-Recherche.md)
und wiederholt ihn nicht. Alle Schema-, Datei- und .NET-Aussagen dieses Befundes sind durch
tatsächlichen Download, Auszählung, XSD-Validierung und Kompilierlauf auf diesem Rechner belegt;
die Befehle stehen jeweils dabei. Wo nur Fremddokumentation vorlag, ist das kenntlich gemacht.

---

**Kurzantwort:** gbXML ist deutlich leichter zu lesen als IFC, aber es ist kein gepflegter Standard
mehr — die Fassung „8.01“ ist byteweise identisch mit 7.04, das Schema validiert seine eigene
Versionsnummer nicht, und **keine einzige der vier offiziellen Beispieldateien ist schemagültig**.
Für EPOS heißt das: Import ja, aber mit tolerantem Parser statt XSD-Validierung. Export ja, aber
nur als *Datenblatt*, nicht als übernahmefähiges Simulationsmodell — denn alle ernstzunehmenden
Zielwerkzeuge rechnen aus `PolyLoop`-Polygonen und geschlossenen Raumvolumen, nicht aus
`RectangularGeometry`. Ein EPOS-Export ohne echte Geometrie wird von IES VE, OpenStudio und
DesignBuilder **nicht** zu einem lauffähigen Modell.

---

## 1. Das Schema — was wirklich drinsteht

Grundlage: [`GreenBuildingXML_Ver8.01.xsd`](https://raw.githubusercontent.com/GreenBuildingXML/gbXML_Schemas/master/GreenBuildingXML_Ver8.01.xsd)
aus dem Repositorium [GreenBuildingXML/gbXML_Schemas](https://github.com/GreenBuildingXML/gbXML_Schemas),
heruntergeladen am 15.09.2026: 387.450 Bytes, 518 globale Elemente, 18 benannte `complexType`,
157 `simpleType` mit zusammen 1.155 Aufzählungswerten. Namensraum `http://www.gbxml.org/schema`,
`elementFormDefault="unqualified"`.

### 1.1 Version 8.01 ist keine neue Fassung — und validiert sich selbst nicht

Zwei Funde, beide reproduzierbar:

**(a) 8.01 ist inhaltlich 7.04.** Ein Zeilenvergleich von `GreenBuildingXML_Ver8.01.xsd` gegen
[`GreenBuildingXML_Ver7.04.xsd`](https://raw.githubusercontent.com/GreenBuildingXML/gbXML_Schemas/master/GreenBuildingXML_Ver7.04.xsd)
ergibt genau drei Abweichungen: das `version`-Attribut des `xsd:schema`-Elements, die Zeile
`gbXML version 8.01 — maintained by gbXML.org`, und den Änderungsprotokoll-Text. **Kein einziges
Element, Attribut oder Enum unterscheidet sich.** Beide Dateien sind 387.4xx Bytes groß und
erzeugen über `xsd.exe` byteidentischen C#-Code (704.311 Bytes, `diff -q` meldet „identisch“).
Die im Protokoll zu 8.01 aufgezählten obXML-Neuerungen (Occupant Behavior aus ASHRAE RP-1815)
waren bereits in 7.04 enthalten; das Protokoll wurde lediglich umsortiert. Die
[Ankündigung auf gbxml.org](https://www.gbxml.org/WhatsNewWith_GreenBuildingXML_gbXML) beschreibt
8.01 als Ergebnis eines 90-tägigen Review von Mai bis August 2025 — am XSD ist davon nichts
angekommen.

**(b) `version="8.01"` ist nach dem eigenen Schema ungültig.** Das Wurzelattribut `version` ist
`use="required"` und hat den Typ `versionEnum`. Dieser Typ zählt auf:
`0.35, 0.36, 0.37, 5.00, 5.01, 5.10, 5.11, 5.12, 6.00, 6.01` — **Ende bei 6.01.** Im
Änderungsprotokoll zu 7.03/7.04 steht der Punkt „30. Added new version number to versionEnum
enumeration“; ausgeführt wurde er nie. Gegenprobe mit `System.Xml.Schema` unter .NET 10:

```
--- leeres gbXML, version=8.01 ---
  Error: The 'version' attribute is invalid - The value '8.01' is invalid according to
  its datatype 'http://www.gbxml.org/schema:versionEnum' - The Enumeration constraint failed.
--- leeres gbXML, version=6.01 ---
  GUELTIG
```

**Folge für EPOS:** Ein Export muss `version="6.01"` schreiben, wenn er gegen das offizielle XSD
validieren soll. Real schreiben die Werkzeuge ohnehin `0.37` (siehe 2.1).

### 1.2 Struktur

```
gbXML                                  [Pflichtattribute: version, temperatureUnit,
 ├─ Campus            (id)              lengthUnit, areaUnit, volumeUnit,
 │   ├─ Location                        useSIUnitsForResults]
 │   ├─ Building      (id, buildingType!)
 │   │   ├─ Area, Space*, BuildingStorey
 │   │   └─ Space     (id, zoneIdRef, conditionType, spaceType, buildingStoreyIdRef)
 │   │        └─ Area, Volume, AirChangesPerHour, PeopleNumber,
 │   │           LightPowerPerArea, EquipPowerPerArea, InfiltrationFlow,
 │   │           PlanarGeometry, ShellGeometry, SpaceBoundary*
 │   └─ Surface       (id, surfaceType!, constructionIdRef!, exposedToSun)
 │        ├─ AdjacentSpaceId (1..2)
 │        ├─ RectangularGeometry | PlanarGeometry
 │        └─ Opening (id, openingType!, windowTypeIdRef)
 ├─ Construction / Layer / Material / WindowType     (alle auf Wurzelebene, per IDREF verknüpft)
 ├─ Schedule → YearSchedule → WeekSchedule → Day → DaySchedule → ScheduleValue
 ├─ Zone              (id) — DesignHeatT, DesignCoolT, AirChangesPerHour, OAFlow…
 └─ Results, Weather, AirLoop, HydronicLoop, AirSystem, Occupants, Behaviors
```

Wichtig: `Surface` hängt **nicht** unter `Space`, sondern unter `Campus`. Die Zuordnung läuft
ausschließlich über `AdjacentSpaceId/@spaceIdRef`. `Construction`, `Layer`, `Material` und
`WindowType` stehen flach auf Wurzelebene und werden über `constructionIdRef`, `LayerId/@layerIdRef`,
`MaterialId/@materialIdRef`, `windowTypeIdRef` verkettet. Ein Importer muss also zweistufig
arbeiten: erst alle Wurzelkataloge einlesen, dann die Flächen auflösen.

`Zone` ist **kein** Container für Räume, sondern ein Sollwert- und Anlagenobjekt; die Verknüpfung
geht umgekehrt über `Space/@zoneIdRef`. Mehrere `Space` können auf dieselbe `Zone` zeigen — das ist
die gbXML-Entsprechung des Mehrzonenmodells.

### 1.3 Pflichtangaben — empirisch bestimmt

Das XSD ist außerordentlich locker gebaut: fast jeder Inhalt steht in
`<xsd:choice minOccurs="0" maxOccurs="unbounded">`. Damit ist die Kindreihenfolge frei und fast
jedes Kindelement optional. Was tatsächlich erzwungen wird (mit `XmlReaderSettings.ValidationType
= ValidationType.Schema` nachgemessen):

| Ebene | Wirklich erzwungen |
|---|---|
| `gbXML` | `version`, `temperatureUnit`, `lengthUnit`, `areaUnit`, `volumeUnit`, `useSIUnitsForResults` |
| `Campus` | `id`; **mindestens 4 `Surface`** (`minOccurs="4"` — wird geprüft) |
| `Building` | `id`, `buildingType` |
| `Space` | `id` |
| `Surface` | `id`, `surfaceType`, **`constructionIdRef`** |
| `Opening` | `id`, `openingType` |
| `Construction`/`Layer`/`Material`/`WindowType`/`Zone` | `id` |
| `AdjacentSpaceId` | `spaceIdRef` |
| `Location` | **`ZipcodeOrPostalCode`** (`xsd:all`, `minOccurs="1"`) — `Latitude`/`Longitude` dagegen optional |
| Stoffwerte | `unit` ist Pflicht bei `Conductivity`, `Density`, `SpecificHeat`, `U-value`, `R-value`, `SolarHeatGainCoeff`, `Transmittance`, `PeopleNumber`, `LightPowerPerArea`, `EquipPowerPerArea`; optional bei `Area`, `Volume`, `Width`, `Height`, `Thickness`, `Temperature` |

Zwei Kuriositäten für den deutschen Einsatz: `Location` verlangt eine Postleitzahl, aber keine
Koordinaten — das Schema ist erkennbar auf US-Klimazonen zugeschnitten. Und `Campus` verlangt
vier Flächen, was ein Einzonen-Quader mit sechs Bauteilgruppen zwar erfüllt, ein reduziertes
Testmodell aber nicht.

Umgekehrt ist alles andere erlaubt. Gegenprobe: ein `<gbXML>` **ohne jeden Inhalt** (nur die sechs
Pflichtattribute) validiert fehlerfrei, und eine `Surface` **ganz ohne Geometrie** ebenfalls.
Schemagültigkeit sagt über die Brauchbarkeit einer gbXML-Datei praktisch nichts aus.

Die IDREF-Integrität wird dagegen geprüft: ein `windowTypeIdRef` auf einen nicht vorhandenen
`WindowType` meldet `Reference to undeclared ID`.

### 1.4 `RectangularGeometry` allein — schemagültig, aber praktisch wertlos

Das ist die zentrale Frage für einen Exporteur ohne Geometriekernel, und die Antwort fällt
zweigeteilt aus.

`RectangularGeometry` trägt `Azimuth` (0…360, einheitenlos, Grad), `Tilt` (0…180), `Width`,
`Height` und einen `CartesianPoint` als Einfügepunkt; `PolyLoop` ist darin optional. Im XSD stehen
`RectangularGeometry` und `PlanarGeometry` als gleichrangige Zweige derselben `choice` — beide
sind formal optional.

**Schemaseitig:** Ein vollständiges EPOS-Einzonenmodell mit sechs Bauteilgruppen, nur
`RectangularGeometry`, ohne ein einziges `PolyLoop`, Konstruktion mit Schicht und Baustoff,
`WindowType` mit U-Wert und g-Wert, `Zone` mit `DesignHeatT`/`DesignCoolT` — validiert
**fehlerfrei** gegen das 8.01-XSD.

**Werkzeugseitig sieht es anders aus.** Der gbXML-Importer von OpenStudio
([`src/gbxml/ReverseTranslator.cpp`](https://raw.githubusercontent.com/NREL/OpenStudio/develop/src/gbxml/ReverseTranslator.cpp),
1.131 Zeilen, heruntergeladen und durchsucht) enthält **null** Vorkommen von
`RectangularGeometry`, aber je 4 von `PlanarGeometry` und `PolyLoop` und 11 von
`AdjacentSpaceId`. Der *Exporter* derselben Bibliothek
([`ForwardTranslator.cpp`](https://raw.githubusercontent.com/NREL/OpenStudio/develop/src/gbxml/ForwardTranslator.cpp))
schreibt `RectangularGeometry` 18-mal. OpenStudio **schreibt** das Element also und **liest es
nicht**. Gleiches Bild bei IES VE: die Herstellerdokumentation
[„What is needed for a successful gbXML import into the VE?“](https://www.iesve.com/support/ve/knowledgebase_faq/faq/what-is-needed-for-a-successful-gbxml-import-into-the-ve/1382)
nennt als Pflicht, dass je Raum entweder `ShellGeometry/ClosedShell/PolyLoop` oder
`Surface/PlanarGeometry/PolyLoop` vorliegen muss, dass die Begrenzungsflächen den Raum
lückenlos umschließen und dass jede Flächenkante auf eine Kante einer anderen Fläche trifft
(1 mm Toleranz). `AdjacentSpaceId` kommt in dieser Pflichtliste gar nicht vor.

**Damit ist die Frage beantwortet: `RectangularGeometry` allein ist schemakonform, aber kein
Zielwerkzeug baut daraus ein Modell.** Das Element existiert für DOE-2-artige Rechenkerne, die
Flächen ohnehin nur als Rechtecke mit Orientierung führen — nicht für die heutigen Importer.

### 1.5 Einheiten, Nordrichtung, Ort

Die vier Einheiten-Pflichtattribute an der Wurzel (`lengthUnit`, `areaUnit`, `volumeUnit`,
`temperatureUnit`) gelten global; einzelne Elemente dürfen abweichen, wenn sie ein eigenes
`unit`-Attribut tragen. `lengthUnitEnum` kennt `Meters` und `Feet`, `temperatureUnitEnum` kennt
`F, C, K, R`, `uValueUnitEnum` nur `WPerSquareMeterK` und `BtuPerHourSquareFtF`,
`conductivityUnitEnum` `WPerCmC, WPerMeterK, BtuPerHourFtF`, `densityUnitEnum` u. a. `KgPerCubicM`,
`specificHeatUnitEnum` `JPerKgK` und `BTUPerLbF`. Ein Importer muss also jede Größe zweimal
prüfen: globales Attribut und lokales `unit`. Praktisch relevant, weil alle untersuchten
US-Beispieldateien in `Feet`/`SquareFeet`/`CubicFeet` geschrieben sind.

`Azimuth` und `Tilt` sind einheitenlos in Grad, mit XSD-Wertebereich 0…360 bzw. 0…180.
Die Nordrichtung steckt in `Location/CADModelAzimuth` (`xsd:double`, optional) — die Drehung des
CAD-Modells gegen Nord. `Location` führt außerdem `Latitude`, `Longitude`, `Elevation`,
`StationId` und die schon genannte Pflicht-Postleitzahl. Anders als bei IFC
(`IfcCompoundPlaneAngleMeasure`, Grad/Minuten/Sekunden-Tupel, siehe Befund C, Abschnitt 1) sind
Breite und Länge hier schlichte Dezimalgrad — das ist eine echte Erleichterung.

Zusätzlich gibt es an der Wurzel `SurfaceReferenceLocation` mit den Werten `Centerline` und
`InteriorSurface`. Das entscheidet, ob die Flächen auf Wandmittelebene oder auf Innenoberfläche
liegen, und damit über Brutto- oder Nettobezug der Flächen. Optional — und in den untersuchten
Beispieldateien nicht gesetzt.

### 1.6 Aufbauten, Schichten, Baustoffe

`Construction` trägt `Name`, ein optionales `U-value`, `Absorptance`, `Roughness`, `Emittance` und
beliebig viele `LayerId`. `Layer` trägt genau eine `MaterialId` (plus optional
`InsideAirFilmResistance`, `HOutside`). `Material` trägt `Thickness`, `Conductivity`, `Density`,
`SpecificHeat`, alternativ `R-value`, dazu `Absorptance`, `Roughness`, `Porosity`, `Permeance`.

Damit ist λ·ρ·c·d je Schicht vollständig abbildbar — genau der Eingang für das
VDI-6007-RC-Ersatzmodell. Wichtig ist die **Alternative**: `Material` kann statt der vier
Stoffwerte nur ein `R-value` führen. Wie ein Zielwerkzeug damit umgeht, zeigt der Quelltext von
OpenStudio ([`src/gbxml/MapEnvelope.cpp`](https://raw.githubusercontent.com/NREL/OpenStudio/develop/src/gbxml/MapEnvelope.cpp),
516 Zeilen): liegen `Density`, `Conductivity`, `Thickness` und `SpecificHeat` vor, entsteht ein
`StandardOpaqueMaterial` mit Speichermasse; liegt nur `R-value` vor, entsteht ein
`MasslessOpaqueMaterial`; liegt gar nichts vor, ein Stummel mit R = 0,001.

**Das ist die wichtigste Konsequenz für einen EPOS-Export:** Wer nur U-Werte je Bauteilgruppe
schreibt, erzeugt im Zielwerkzeug ein **masseloses** Gebäude. Für eine stationäre Heizlast ist das
richtig, für jede dynamische Rechnung — und damit für alles, was VDI 6007 ausmacht — ist es falsch.
Ein EPOS-Export muss die Schichten mitschreiben, sonst ist er für die Zielwerkzeuge wertlos.

`WindowType` trägt `U-value`, `SolarHeatGainCoeff` (mit optionalem `solarIncidentAngle`),
`Transmittance`, `Reflectance`, `Emittance`, `ShadingCoeff` sowie die Detailobjekte `Glaze`, `Gap`,
`Frame`, `Blind`. OpenStudio liest daraus `U-value` (nur in `WPerSquareMeterK`),
`SolarHeatGainCoeff` und die sichtbare `Transmittance` und baut ein `SimpleGlazing`. Dieselben drei
Größen stehen wahlweise auch direkt am `Opening` — ein Importer muss beide Orte prüfen.

### 1.7 `surfaceType`, `AdjacentSpaceId`, Randbedingungen

`surfaceTypeEnum` kennt 15 Werte:
`InteriorWall, ExteriorWall, Roof, InteriorFloor, ExposedFloor, Shade, UndergroundWall,
UndergroundSlab, Ceiling, Air, UndergroundCeiling, RaisedFloor, SlabOnGrade, FreestandingColumn,
EmbeddedColumn`.

`openingTypeEnum`: `FixedWindow, OperableWindow, FixedSkylight, OperableSkylight, SlidingDoor,
NonSlidingDoor, Air`.

`conditionTypeEnum` am `Space`: `Heated, Cooled, HeatedAndCooled, Unconditioned, Vented,
NaturallyVentedOnly`.

Die Randbedingung ergibt sich aus der Kombination von `surfaceType` und der Anzahl der
`AdjacentSpaceId`:

| Fall | `AdjacentSpaceId` | Bedeutung |
|---|---|---|
| Außenbauteil | 1 | Fläche grenzt an Außenluft (`ExteriorWall`, `Roof`, `ExposedFloor`) |
| Innenbauteil | 2 | Fläche zwischen zwei Zonen (`InteriorWall`, `InteriorFloor`, `Ceiling`, `Air`) |
| Erdreich | 1 | `SlabOnGrade`, `UndergroundWall`, `UndergroundSlab`, `UndergroundCeiling` |
| Verschattung | 0 | `Shade` — gehört zu keinem Raum |

Diese Regel hält in den Beispieldateien: in `ARCH_ASHRAE_Headquarters_r16_detached.xml` haben
1.432 von 1.775 Flächen zwei Nachbarn und 343 einen; in
`gbXMLExport_ASHRAEHQ_Revit2017.xml` haben exakt die 79 `Shade`-Flächen null Nachbarn.
Zusätzlich kann `AdjacentSpaceId` ein eigenes `surfaceType`-Attribut tragen — die Sicht *dieses*
Raumes auf die Fläche (Boden der einen, Decke der anderen Zone).

`exposedToSun` (`xsd:boolean`, Vorgabe `true`) ist die zweite, davon unabhängige Angabe zur
Außenlage.

### 1.8 `SpaceBoundary`, `Schedule`, `Results`

`SpaceBoundary` ist in gbXML ein schmales Element: es enthält **nur** `PlanarGeometry` und trägt
die Attribute `surfaceIdRef`, `oppositeIdRef`, `isSecondLevelBoundary` und `ifcGUID` — es hat nicht
einmal eine eigene `id`. Es ist also eine Ergänzung zur `Surface`, keine eigenständige Topologie
wie `IfcRelSpaceBoundary2ndLevel` (vgl. Befund C, Abschnitt 1). Die eigentliche thermische
Topologie trägt in gbXML die `Surface` mit ihren ein bis zwei `AdjacentSpaceId` — und das ist der
wesentliche Vorteil gegenüber IFC: sie ist Pflichtbestandteil des Modells, nicht optionale Beigabe
einer selten exportierten MVD.

`Schedule` → `YearSchedule` → `WeekSchedule` → `Day` → `DaySchedule` → `ScheduleValue` ist eine
vierstufige Kette; `scheduleTypeEnum` kennt u. a. `Temp`, `Fraction`, `OnOff`, `ResetTemp`,
`ActivityLevel`. `Space` verweist über `scheduleIdRef`, `lightScheduleIdRef`,
`equipmentScheduleIdRef`, `peopleScheduleIdRef`, `Zone` über `heatSchedIdRef`, `coolSchedIdRef`,
`airChangesSchedIdRef` und weitere. Für ein VDI-6007-Modell mit Wochenprofilen ist das ausreichend
ausdrucksstark.

**Ja, gbXML hat Ergebnisfelder.** `Results` trägt `Value`-Elemente, `ObjectId`, einen
`CartesianPoint` und die Pflichtattribute `id`, `unit`, `resourceType`, `startTime` sowie optional
`resultsType`, `timeIncrement`, `valueType`. `resultsTypeEnum` enthält unter anderem `HeatLoad`,
`CoolingLoad`, `Energy`, `Power`, `DryBulbTemperature`, `Flow`, `Capacity`, `Cost` — also genau die
Kategorien, die EPOS erzeugt (Heizwärme je Zone und Jahr, Spitzenlast, Raumtemperatur). Über
`ObjectId`/`timeIncrement` lassen sich Zeitreihen je Zone hinterlegen. Der praktische Wert ist
allerdings begrenzt: die `Results`-Blöcke der Revit-Beispieldateien sind der mit Abstand größte
Fehlerherd bei der Validierung (siehe 2.3), weil sie mit längst entfernten Attributen wie
`DOE22objectType`, `DOE22resultType`, `timeUnit` und `objectIdRef` arbeiten. Ergebnisse über gbXML
zurückzuliefern ist ein schlecht gepflegter Pfad.

### 1.9 Was für Wohngebäude fehlt

`buildingTypeEnum` enthält `SingleFamily` und `MultiFamily` — soweit gut. Aber `spaceTypeEnum` hat
126 Werte, und **kein einziger** enthält die Zeichenfolge „Residential“. Die Liste ist erkennbar aus
den Raumtypen der ASHRAE-90.1-Beleuchtungstabellen abgeleitet; die einzigen wohnähnlichen Werte
sind `LivingQuartersDormitory`, `LivingQuartersHotel`, `LivingQuartersMotel` und
`DormitoryBedroom`. Für die EPOS-Zielgruppe — Heizungserneuerung im Wohngebäudebestand nach
VDI 4645 — gibt es also keine passende Raumnutzung. Beim Export bleibt nur, `spaceType` wegzulassen
(es ist optional) und die Nutzung über `PeopleNumber`, `LightPowerPerArea`, `EquipPowerPerArea`,
`AirChangesPerHour` und die Sollwerte an der `Zone` auszudrücken. Das ist tragfähig, aber die
semantische Nutzungskennzeichnung geht verloren.

### 1.10 Lizenz: keine

Dreifach geprüft, dreimal negativ:

1. **Im XSD selbst:** `grep -io 'licen[a-z]*\|copyright'` über die aufbereitete
   `GreenBuildingXML_Ver8.01.xsd` liefert **null** Treffer. Kein Lizenzkopf, kein Copyright-Vermerk.
2. **Im Repositorium:** Die GitHub-API meldet für `gbXML_Schemas` `"license": null`. Dasselbe gilt
   für `Sample_gbXML_Files`, `TestCaseValidator_DOE`, `TestCaseValidator_PNNL`,
   `gbXMLReadWriteSDK_CSharp`, `GenericgbXMLValidator_601` und `gbXML-to-IDF-Test-Cases`. Nur
   `spider-gbxml-tools` und `gbXMLValidatorLogic` tragen MIT.
3. **Auf der Website:** Die Seite [About gbXML](https://www.gbxml.org/About_GreenBuildingXML_gbXML)
   nennt Trägerschaft (eigenständige Organisation seit 2009, Vorstand mit Autodesk, Bentley, Trane),
   Förderer (DOE, NREL, ASHRAE) — aber keine Lizenz- oder Nutzungsbedingung. Ebenso wenig die
   Testfallseite ([Test Cases](https://www.gbxml.org/TestCases_for_GreenBuildingXML_gbXML), 20 Fälle
   aus ASHRAE RP-1810 mit PDF, gbXML- und Revit-Dateien).

**Bewertung:** Das Schema wird durchgängig als „open and free“ beworben
([Wikipedia](https://en.wikipedia.org/wiki/Green_Building_XML)), aber ohne explizite Lizenz ist die
Rechtslage bei Weitergabe unklar. Für EPOS praktisch entschärft: ein XSD ist eine
Schnittstellenbeschreibung, und man muss es nicht mitliefern — der Importer/Exporter kann ohne
eingebettetes XSD arbeiten (siehe 4.4). **Beispieldateien** dagegen sollten **nicht** ungeprüft in
ein EPOS-Regressionstest-Repositorium übernommen werden; hier ist dieselbe Vorsicht geboten wie bei
den IFC-Testdateien (Befund C, Abschnitt 6). Ladybug/Honeybee scheidet als Testdatenquelle ohnehin
aus: [AGPL-3.0](https://github.com/ladybug-tools).

---

## 2. Import — was die Autorensysteme tatsächlich liefern

### 2.1 Vier Dateien, ausgezählt

Heruntergeladen aus [GreenBuildingXML/Sample_gbXML_Files](https://github.com/GreenBuildingXML/Sample_gbXML_Files)
(letzter Push 20.06.2020) und mit LINQ to XML ausgezählt:

| | ARCH_ASHRAE_HQ_r16 | Revit2017 ASHRAE HQ | Single Family Res. | Urban_House_MEP |
|---|---|---|---|---|
| Größe / Kodierung | 16,3 MB / **UTF-16LE** | 12,6 MB / UTF-8 | 1,5 MB / UTF-8 | 2,3 MB / **UTF-16LE** |
| `version` | 0.37 | 0.37 | 0.37 | 0.37 |
| Einheiten | Feet / F | Feet / C | **Meters / C** | Feet / F |
| `Space` | 76 | 93 | 12 | 26 |
| `Surface` | 1.775 | 2.130 | 237 | 245 |
| davon mit `RectangularGeometry` | 1.775 | 2.130 | 237 | 245 |
| davon mit `PlanarGeometry` | 1.775 | 2.130 | 237 | 245 |
| ohne `constructionIdRef` | **1.775** | 0 | 0 | **54** |
| `AdjacentSpaceId` 0 / 1 / 2 | 0 / 343 / 1.432 | 79 / 1.073 / 978 | 36 / 121 / 80 | 26 / 86 / 133 |
| `Construction` | **0** | 8 | 8 | 9 |
| davon mit `LayerId` | – | 8 | 8 | 9 |
| davon mit `U-value` | – | 1 | 1 | 9 |
| `Material` | **0** | 18 | 21 | 16 |
| davon **voll λ/ρ/c/d** | – | **11** | **12** | **14** |
| davon nur `R-value` | – | 7 | 9 | 2 |
| `WindowType` | **0** | 4 | 5 | 2 |
| `Zone` | **0** | 93 | 12 | 1 |
| `SpaceBoundary` | 2.184 | 2.882 | 262 | 320 |
| `Results` | 0 | 463 | 211 | 0 |

Daraus vier belastbare Aussagen:

**(1) Der reine Architekturexport liefert null Bauphysik.** `ARCH_ASHRAE_Headquarters_r16_detached.xml`
hat 1.775 Flächen mit vollständiger Geometrie und sauberer Nachbarschaftstopologie — und **keine
einzige** Konstruktion, kein Material, keinen Fenstertyp, keine Zone, kein `constructionIdRef`.
Das ist das gbXML-Gegenstück zu dem Befund aus der deutschen IFC-Praxis, den Befund C zitiert
(„das Architekturmodell ist in der Regel für eine thermische Betrachtung nicht geeignet“). Ein
EPOS-Import muss diesen Fall abfangen und auf TABULA/IWU-Vorbelegung umschalten, statt zu scheitern.

**(2) Wo Konstruktionen da sind, sind sie geschichtet — aber die Stoffwerte lückenhaft.**
Alle Konstruktionen der drei bestückten Dateien führen `LayerId`; U-Werte auf
`Construction`-Ebene sind dagegen die Ausnahme (1 von 8 bei den Revit-Exporten). Von den
Materialien haben aber nur 11 von 18, 12 von 21 bzw. 14 von 16 die vollen vier Stoffwerte; der
Rest trägt nur `R-value` und ist damit masselos. **Für EPOS heißt das: pro Aufbau prüfen, ob alle
Schichten vollständig sind — eine einzige R-Wert-Schicht macht die Wärmespeicherfähigkeit des
ganzen Aufbaus unbrauchbar.** Fallback: U-Wert aus der Schichtung rechnen und die Masse aus der
Baualtersklasse vorbelegen, wie in Befund C für den IFC-Weg vorgesehen.

**(3) Alle Werkzeuge schreiben beide Geometrien.** In jeder der vier Dateien trägt **jede** Fläche
sowohl `RectangularGeometry` als auch `PlanarGeometry`. Ein EPOS-Import kann deshalb bequem die
Rechteckfassung nehmen (Azimut, Neigung, Breite × Höhe) und braucht die Polygone nicht auszuwerten —
für das VDI-6007-Modell ist das genau die richtige Abstraktion und erspart den Geometriekernel.
Das ist der entscheidende Vorteil von gbXML gegenüber IFC für EPOS.

**(4) Kodierung.** Zwei der vier Dateien sind UTF-16LE mit BOM. `XmlReader.Create(Stream)` und
`XDocument.Load(string)` kommen damit zurecht; `File.ReadAllText` mit erzwungener UTF-8-Kodierung
oder ein `XDocument.Parse(string)` nach falscher Dekodierung nicht. Der Importer muss die Datei als
**Strom** öffnen und die Kodierungserkennung dem XML-Leser überlassen.

### 2.2 Die Autorensysteme im Einzelnen

**Revit.** Export über die Energy Settings bzw. `Datei → Exportieren → gbXML`
([Autodesk-Hilfe](https://help.autodesk.com/cloudhelp/2022/ENU/Revit-DocumentPresent/files/GUID-76FA41DE-D14A-4708-AE25-D4FCEE05F3C8.htm)).
Die Praxisfallen sind gut dokumentiert: ohne gesetztes **„Export Default Values“** erscheinen die
`Construction`-, `Layer`- und `Material`-Knoten gar nicht erst, und die Option **„Include Thermal
Properties“** greift nur bei `Export Category = Rooms`
([Autodesk-Forum](https://forums.autodesk.com/t5/revit-architecture-forum/construction-layer-and-material-properties-not-appear-in-gbxml/td-p/3393747)).
Autodesk selbst dokumentiert zudem, dass Materialien im gbXML auftauchen können, die im Modell gar
nicht vorkommen, während die tatsächlich verwendeten fehlen — weil ohne gepflegte thermische
Materialeigenschaften die konzeptionellen Vorgabetypen geschrieben werden
([Autodesk-Supportartikel](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/Incorrect-material-information-displayed-when-exporting-gbXML-from-Revit.html)).
Die ausgezählte Revit-2017-Datei bestätigt das Bild: acht Konstruktionen für 2.130 Flächen, also
weitgehend Vorgabewerte. **Anders als IFC ist die Zonentopologie aber vollständig** — 978 Flächen
mit zwei Nachbarn, alle mit `surfaceType` und `constructionIdRef`.

**Archicad.** gbXML-Export ist Standardfunktion in jedem Archicad
([Graphisoft-Hilfe, Interoperability with Green Software](https://help.graphisoft.com/AC/27/INT/_AC27_Help/110_EnergyEvaluation/110_EnergyEvaluation-26.htm)).
Archicad erzeugt die Raumbegrenzungen selbst, indem es die sichtbaren Bauteile und Öffnungen nach
Orientierung und Lage zu den Zonen auswertet, und gruppiert Zonen nach Orientierung, Nutzungsprofil
und Sollwerten zu **thermischen Blöcken**
([Graphisoft, Energy Evaluation](https://help.graphisoft.com/AC/25/INT/_AC25_Help/110_EnergyEvaluation/110_EnergyEvaluation-2.htm),
[Graphisoft Support, EcoDesigner STAR](https://support.graphisoft.com/hc/en-us/articles/38414383007377-EcoDesigner-STAR-and-Archicad-energy-evaluation)).
Das ist konzeptionell **genau das EPOS-Mehrzonenmodell** und der beste Ausgangspunkt, den es gibt.
Einschränkung: die Hilfeseite verweist für die konkrete Feldliste auf eine Grafik, die per
Textabruf nicht auswertbar ist; die Zuordnung ist also nicht aus der Primärquelle belegbar. In der
Graphisoft-Community läuft außerdem ein Fehlerbericht
[„Issues with gbXML Export from Archicad persists even in AC28“](https://community.graphisoft.com/t5/Graphisoft-Technology-Preview/Issues-with-gbXML-Export-from-Archicad-persists-even-in-AC28/td-p/615291)
— der Inhalt war nicht abrufbar (HTTP 403), der Titel belegt aber, dass der Export auch in der
aktuellen Fassung als fehlerbehaftet gilt. **Für EPOS: Archicad ist der aussichtsreichste
Quellkandidat, aber vor einer Zusage im Feld mit einer echten Kundendatei zu prüfen.**

**Vectorworks.** Der gbXML-Export umfasst laut Hersteller „spaces, walls, slabs, windows, doors,
columns, and roofs“; Voraussetzung ist ein Modell mit Geschossen, bei dem alle Raumobjekte
vollständig von Wänden, Platten und Dächern umschlossen sind
([Vectorworks-Hilfe, Exporting in gbXML Format](https://app-help.vectorworks.net/2016/eng/VW2016_Guide/ImportExport/Exporting_in_gbXML_Format.htm)).
Die hauseigene Energieauswertung *Energos* rechnet nach Passivhaus-Methode und hat einen eigenen
Exportweg ([Energos](https://app-help.vectorworks.net/2022/eng/VW2023_Guide/EnergyAnalysis/Energos_energy_analysis_module.htm)).
Aussagen zu Schichtaufbauten im gbXML-Export waren nicht auffindbar — hier besteht eine echte
Belegslücke.

**SketchUp/OpenStudio.** OpenStudio kann gbXML sowohl lesen (`File → Import`) als auch schreiben
([OpenStudio Coalition, Import gbXML](https://openstudiocoalition.org/tutorials/tutorial_gbxmlimport/)).
Als *Quelle* ist der Weg für EPOS wenig interessant, weil ein OpenStudio-Modell bereits ein
Energiemodell ist. Bekannte Importlücken: `UndergroundWall` wird nicht korrekt übernommen
([NREL/OpenStudio #3121](https://github.com/NREL/OpenStudio/issues/3121)), und der Übersetzer
behandelt das `<Name>`-Element wie einen Bezeichner, was bei mehrfach vergebenen oder für
EnergyPlus unzulässigen Namen bricht ([#4457](https://github.com/NREL/OpenStudio/issues/4457)).

### 2.3 Keine einzige Beispieldatei ist schemagültig

Alle vier Dateien gegen `GreenBuildingXML_Ver8.01.xsd` validiert (.NET 10, `XmlReaderSettings`):

| Datei | Meldungen | häufigste Ursachen |
|---|---|---|
| ARCH_ASHRAE_HQ_r16 | **1.775** | 1.775 × fehlendes Pflichtattribut `constructionIdRef` |
| Revit2017 ASHRAE HQ | **2.930** | 463 × `DOE22objectType`, 463 × `DOE22resultType`, 463 × `timeUnit` nicht deklariert; 349 × fehlendes `resourceType`; 343 × `objectIdRef` nicht deklariert; 151 × Einheit `kBtuPerHour` unbekannt |
| Single Family Residential | **1.221** | dieselben `Results`-Attribute; 32 × fehlendes `scheduleType`; 20 × Einheit `kW` unbekannt |
| Urban_House_MEP | **88** | 54 × fehlendes `constructionIdRef`; 16 × fehlendes `scheduleType` |

Die Meldungen „fehlendes `surfaceType`“ betreffen **nicht** die `Surface`-Elemente (alle 4.387
Flächen aller vier Dateien tragen `surfaceType`), sondern die optischen Kennwerte `Absorptance`,
`Reflectance`, `Transmittance` und `Emittance`, die laut XSD ein `surfaceType` aus
`surfaceDescriptionEnum` verlangen.

**Schlussfolgerung für EPOS:** Eine XSD-Validierung beim Import ist **wertlos** — sie würde jede
reale Datei ablehnen. Der Importer muss tolerant lesen und je Feld entscheiden, ob es brauchbar
ist. (Für den *Export* bleibt die Validierung dagegen sinnvoll, siehe 4.4.)

---

## 3. Export — was die Zielwerkzeuge annehmen

| Zielwerkzeug | gbXML-Import | Geometrieanforderung | Belegt durch |
|---|---|---|---|
| **IES VE** | ja | **PolyLoop Pflicht**, geschlossenes Raumvolumen, Kantenübereinstimmung 1 mm | [IESVE-Wissensdatenbank](https://www.iesve.com/support/ve/knowledgebase_faq/faq/what-is-needed-for-a-successful-gbxml-import-into-the-ve/1382) |
| **OpenStudio / EnergyPlus** | ja | nur `PlanarGeometry/PolyLoop`; `RectangularGeometry` wird **nicht gelesen** | [ReverseTranslator.cpp](https://raw.githubusercontent.com/NREL/OpenStudio/develop/src/gbxml/ReverseTranslator.cpp), 0 Treffer |
| **DesignBuilder** | ja, zentraler Weg | toleranteste Implementierung: repariert „missing, misformed and misaligned BIM surfaces“, importiert auch Modelle „with gaps in geometry, missing surfaces“ | [DesignBuilder, 3-D CAD Model Import](https://designbuilder.co.uk/3-d-cad-model-import-gbxml) |
| **IDA ICE** | nachrangig | Hersteller dokumentiert nur den IFC-Weg als BIM-Import | [EQUA, BIM Import](https://www.equa.se/en/ida-ice/extensions/bim-import) |
| **Revit** | **nein** | Export-only; keine Importfunktion dokumentiert | [Autodesk-Hilfe](https://help.autodesk.com/cloudhelp/2018/ENU/Revit-DocumentPresent/files/GUID-586B9574-64DA-47BC-B8EC-DEF2D565928F.htm) |
| **Solar-Computer (GBIS)** | ja, bidirektional | liest gbXML aus AutoCAD/Revit, erkennt Bauteile und schreibt berechnete U-Werte und Lasten ins 3D-Modell zurück | [Solar-Computer, Arbeitsablauf Gebäude](https://www.solar-computer.de/hilfe/de/gbs/revit/Documents/arbeitsablaufgebude.html) |
| **EVEBI (ENVISYS)** | ja | gbXML-Import aus AutoCAD/Revit, in EVEBI 10.1 überarbeitet | [ENVISYS, EVEBI 10.1](https://www.envisys.de/neueversion/evebi-101/) |
| **DDS-CAD, AX3000, RaumGEO** | ja | in der Herstellerliste geführt | [gbxml.org, Supported Software](https://www.gbxml.org/Software_Tools_that_Support_GreenBuildingXML_gbXML) |
| **Hottgenroth/HottCAD** | nicht belegt | Importwege dokumentiert für PDF, JPEG, DWG/DXF und Allplan; gbXML nicht als eigener Weg belegt | [Hottgenroth, HottCAD](https://www.hottgenroth.de/S/HottCAD/Geb%C3%A4udeerfassung/CAD/Seite.html,176955) |

Die gbxml.org-Werkzeugliste ist ungepflegt (Einträge zu abgekündigten Produkten wie „Green Building
Studio (Legacy)“, kein Aktualisierungsdatum) und taugt nur als Hinweis, nicht als Beleg.

### Was ein Export ohne echte Geometrie taugt — nüchtern

EPOS hat keinen Geometriekernel. Ein Einzonenmodell kennt Bauteilgruppen mit Fläche, Azimut,
Neigung und U-Wert — aber keinen Grundriss, keine Koordinaten, keine Kanten. Was daraus wird:

- **IES VE, OpenStudio/EnergyPlus:** Der Import scheitert oder erzeugt ein leeres Modell. IES
  verlangt ausdrücklich geschlossene Volumen mit kantengenauen Polygonen; OpenStudio liest das
  Rechteck gar nicht erst. **Kein brauchbares Ergebnis.**
- **DesignBuilder:** Am ehesten aussichtsreich, weil die Reparaturalgorithmen auf lückenhafte
  Geometrie ausgelegt sind — aber auch DesignBuilder repariert *Polygone*, es erfindet keine.
  **Ohne PolyLoop ebenfalls kein Modell.**
- **Solar-Computer (GBIS), EVEBI:** Diese Werkzeuge rechnen nach DIN V 18599 bzw. Heizlast und
  brauchen primär Flächen, U-Werte und Orientierungen. Hier ist ein geometriearmer Export am
  ehesten verwertbar. Belegt ist das nicht — die Hersteller dokumentieren den Import aus
  AutoCAD/Revit, nicht aus beliebigen Quellen.

**Es gibt aber einen gangbaren Mittelweg**, und er ist der einzige, der einen EPOS-Export sinnvoll
macht: **synthetische Quadergeometrie.** Aus Zonenfläche und Volumen folgt die Raumhöhe; aus der
Fläche ein Rechteckgrundriss (Seitenverhältnis als Vorgabe oder aus den Bauteilgruppen je
Himmelsrichtung abgeleitet); daraus ein Quader mit sechs Flächen, deren Azimute zu den
EPOS-Bauteilgruppen passen. Die `PolyLoop`-Polygone sind dann reine Rechenarbeit — vier
`CartesianPoint` je Fläche, kantenschlüssig konstruiert, **ohne jeden Geometriekernel.** Fenster
werden als Rechtecke in die zugehörige Wandfläche eingesetzt.

Was dieser Export **leistet**: korrekte Flächen, korrekte Orientierungen, korrekte U-Werte,
vollständige Schichtaufbauten mit λ/ρ/c, korrekte Zonensollwerte, korrekte Luftwechsel und innere
Gewinne. Damit rechnet ein Zielwerkzeug eine plausible Heizlast und einen plausiblen Jahresbedarf.

Was er **nicht leistet**: echte Verschattung durch Eigen- und Nachbarbebauung, echte Wärmebrücken,
echte Raumgeometrie, jede Tageslichtrechnung. Und er darf **nie** als „Gebäudemodell“ ausgegeben
werden, sondern als das, was er ist: ein Ersatzmodell mit gleicher Bilanz und erfundener Gestalt.
Das gehört sichtbar in die Datei (`Building/Description`, `Campus/Description`) und in die
Oberfläche.

---

## 4. .NET-Umsetzung

### 4.1 `xsd.exe` erzeugt übersetzbaren, aber unbrauchbaren Code

`xsd.exe GreenBuildingXML_Ver8.01.xsd /classes /language:CS` (Windows-SDK v10.0A, NETFX 4.8 Tools)
läuft durch und liefert **704.311 Bytes, 25.094 Zeilen, 328 Klassen, 205 Enums**. Der Code übersetzt
unter `net10.0` mit `<IsAotCompatible>true</IsAotCompatible>` **ohne eine einzige Warnung** zu einer
260-KB-Assembly.

Nur: er ist nicht benutzbar. Weil das Schema durchgängig
`<xsd:choice minOccurs="0" maxOccurs="unbounded">` verwendet, erzeugt `xsd.exe` **keine typisierten
Eigenschaften**, sondern für jede Klasse ein Paar

```csharp
public partial class Space {
    private object[] itemsField;                        // Items
    private ItemsChoiceType3[] itemsElementNameField;   // ItemsElementName
    …
}
```

Um die Fläche einer Zone zu lesen, muss man das `Items`-Feld durchlaufen und parallel in
`ItemsElementName` prüfen, ob der Eintrag `ItemsChoiceType3.Area` ist. Es gibt kein
`space.Area`. Für ein Schema mit 518 Elementen ist das kein Datenmodell, sondern ein
typisierter Zeiger auf eine Liste von `object`.

### 4.2 `XmlSerializer` scheidet aus — schon auf Windows

Der Versuch, die generierten Klassen zu benutzen, scheitert bereits beim Erzeugen des Serialisierers,
auf Windows x64 mit JIT:

```
System.InvalidOperationException: CodeGenError(IsNotAssignableFrom):
  Cannot convert source type [Equation] to target type [PointData].
   at System.Xml.Serialization.XmlSerializationWriterILGen.WriteElement(…)
   at System.Xml.Serialization.TempAssembly.GenerateRefEmitAssembly(…)
   at System.Xml.Serialization.XmlSerializer..ctor(Type type, …)
```

Ursache ist die obXML-Erweiterung: `Equation` und `PointData` erscheinen in derselben
`choice`-Gruppe, und der IL-Erzeuger des `XmlSerializer` kommt damit nicht zurecht. Das betrifft
7.04 genauso wie 8.01 (identischer Code). **`new XmlSerializer(typeof(gbXML))` über den
xsd.exe-Code ist kaputt, bevor iOS überhaupt ins Spiel kommt.**

Und iOS wäre der zweite K.-o. Derselbe Übersetzungslauf mit `PublishTrimmed`/`IsAotCompatible`
meldet:

```
warning IL3050: Using member 'System.Xml.Serialization.XmlSerializer.XmlSerializer(Type)'
  which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling.
  XML serializer relies on dynamic code generation which is not available with
  Ahead of Time compilation.
warning IL2026: … 'RequiresUnreferencedCodeAttribute' … Members from serialized types
  may be trimmed if not referenced directly.
```

Das deckt sich mit der Microsoft-Dokumentation, die für Native AOT ausdrücklich „No runtime code
generation, for example, `System.Reflection.Emit`“ als Grenze nennt
([Native AOT deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)).

**Ergebnis: `XmlSerializer` ist für EPOS nicht verwendbar — weder generiert noch handgeschrieben.**

### 4.3 Der richtige Weg: LINQ to XML, handgeschriebenes Modell

`System.Xml.Linq` ist Teil der BCL, braucht kein Paket, verwendet keine Laufzeitcodeerzeugung und
ist damit ohne Auflagen AOT- und trimmingfest — plattformfrei bis iOS. Für ein Schema mit dieser
Struktur ist es zudem *angemessener*: EPOS braucht nicht 518 Elemente, sondern rund 25.

Gemessen auf diesem Rechner (.NET 10, Windows x64):

| Vorgang | Datei | Zeit |
|---|---|---|
| `XmlSchemaSet.Compile()` des 8.01-XSD | 387 KB | **4 ms** (481 globale Elemente, 176 Typen) |
| Validierendes Durchlesen | 16,3 MB, 648.885 Knoten | **1.656 ms** |
| Validierendes Durchlesen | 1,5 MB, 119.307 Knoten | **94 ms** |

Auch die größte Beispieldatei ist also in gut anderthalb Sekunden durch — und die läge mit 16 MB
weit über allem, was EPOS im Wohngebäudebestand zu erwarten hat. Für Blazor Hybrid und iOS empfiehlt
sich dennoch eine Größengrenze mit klarer Meldung, wie in Befund C für IFC vorgesehen.

Vorgeschlagener Aufbau, angelehnt an das vorhandene Muster unter
`EPOS.Kern/Allgemein/Import/` (`KatalogImportAblauf`/`KatalogImportProfil`,
`KlimaImportAblauf`, `GanglinienImportAblauf`):

```
EPOS.Kern/Allgemein/Import/gbXML/
    GbXmlDatei.cs          XDocument laden (als Strom! wegen UTF-16), Namensraum, Einheiten
    GbXmlEinheiten.cs      globale + lokale unit-Attribute → SI
    GbXmlModell.cs         schlankes Lesemodell: Campus/Building/Space/Surface/Opening/
                           Construction/Layer/Material/WindowType/Zone
    GbXmlImportAblauf.cs   Aggregation auf das VDI-6007-Zonenmodell
    GbXmlImportProfil.cs   Zuordnung und Herkunftskennzeichnung je Feld
    GbXmlExportAblauf.cs   Schreibrichtung inkl. synthetischer Quadergeometrie
```

`EPOS.Kern` (`net10.0`) verwendet bislang **keine** XML-API — weder `XDocument` noch `XmlReader`
(geprüft per `grep` über alle `.cs`). gbXML wäre die erste Nutzung; ein Paket wird dafür nicht
gebraucht, `System.Xml.Linq` ist Teil des Frameworks.

### 4.4 Validierung: nur beim Export, und mit einem Vorbehalt

Import: **nicht validieren** (Abschnitt 2.3 — jede reale Datei fällt durch).

Export: **validieren, aber nur in Entwicklung und Test.** `XmlSchemaSet` + `XmlReaderSettings`
kostet 4 ms Ladezeit und fängt Tippfehler in Aufzählungswerten und Einheiten zuverlässig ab. Der
Vorbehalt: das XSD müsste dafür mitgeliefert werden, und seine Lizenzlage ist ungeklärt (1.10).
**Vorschlag: das XSD nicht ausliefern, sondern nur im Regressionstest von `EPOS.Kern.Tests`
verwenden** (dort aus einer lokalen Kopie, nicht aus dem Netz). Die Auslieferung braucht es nicht,
weil der Exporteur handgeschrieben ist und die Enums als C#-Konstanten führt.

### 4.5 Vorhandene .NET-Bibliotheken: keine

| Projekt | Stand | Lizenz | Bewertung |
|---|---|---|---|
| [gbXMLReadWriteSDK_CSharp](https://github.com/GreenBuildingXML/gbXMLReadWriteSDK_CSharp) | letzter Push **19.04.2016**, 34 KB, 9 Sterne, XSD-Fassung **5.11** | keine | tot, drei Schemageneration zurück |
| [GenericgbXMLValidator_601](https://github.com/GreenBuildingXML/GenericgbXMLValidator_601) | letzter Push **15.07.2016**, 2 Sterne | keine | tot |
| [gbXMLValidatorLogic](https://github.com/GreenBuildingXML) | — | MIT | Validator, kein Lesemodell |
| [spider-gbxml-tools](https://github.com/GreenBuildingXML/spider-gbxml-tools) | — | MIT | JavaScript/Three.js, Betrachter |
| NuGet | — | — | kein gbXML-Paket auffindbar |

**Es gibt keine gepflegte .NET-gbXML-Bibliothek.** Das ist anders als bei IFC, wo mit
Xbim.Essentials und GeometryGymIFC zwei lebendige Optionen existieren (Befund C, Abschnitt 4).
Für gbXML ist die Eigenimplementierung nicht die schlechtere, sondern die einzige Wahl — und bei
25 relevanten Elementen auch die kleinere Aufgabe.

---

## 5. Abbildung EPOS ↔ gbXML

### 5.1 Import: gbXML → EPOS

| EPOS (VDI 6007) | gbXML | Verlust / Anmerkung |
|---|---|---|
| Zone | `Space` (+ `Zone` über `zoneIdRef`) | mehrere `Space` je `Zone` möglich → aggregieren |
| Zonenfläche | `Space/Area` | Einheit aus `areaUnit` oder lokalem `unit` |
| Zonenvolumen | `Space/Volume` | dito |
| Nutzung | `Space/@spaceType` | **kein Wohnwert im Enum** (1.9) → auf EPOS-Nutzung abbilden oder Vorgabe |
| Sollwert Heizen / Kühlen | `Zone/DesignHeatT`, `Zone/DesignCoolT` | fehlt oft → Vorgabe 20 °C / 26 °C |
| Innere Gewinne Personen | `Space/PeopleNumber` (+ `PeopleHeatGain`) | drei Einheiten möglich (`NumberOfPeople`, `SquareMPerPerson`, `SquareFtPerPerson`) |
| Innere Gewinne Licht / Geräte | `Space/LightPowerPerArea`, `Space/EquipPowerPerArea` | W/m², Einheit Pflicht |
| Luftwechsel | `Space/AirChangesPerHour` (dimensionslos) | `InfiltrationFlow` nur `Loose/Average/Tight` + `BlowerDoorValue` — unbrauchbar für 1/h |
| Bauteil | `Surface` | hängt an `Campus`, Zuordnung nur über `AdjacentSpaceId` |
| Bauteilart | `Surface/@surfaceType` | 15 Werte, direkt abbildbar |
| Bruttofläche | `RectangularGeometry/Width × Height` | in allen Beispielen vorhanden; Fensterabzug **nicht** enthalten → selbst abziehen |
| Azimut / Neigung | `RectangularGeometry/Azimuth`, `/Tilt` | Grad; Nordbezug über `Location/CADModelAzimuth` prüfen |
| Randbedingung | Anzahl `AdjacentSpaceId` + `surfaceType` | 1 = außen/Erdreich, 2 = Nachbarzone, 0 = Verschattung (1.7) |
| U-Wert | `Construction/U-value`, sonst aus Schichten | in den Revit-Dateien nur **1 von 8** Konstruktionen mit U-Wert → meist rechnen |
| Aufbau | `Construction` → `LayerId` → `Layer` → `MaterialId` → `Material` | zweistufige IDREF-Auflösung |
| Schichtdicke / λ / ρ / c | `Material/Thickness`, `/Conductivity`, `/Density`, `/SpecificHeat` | **je Aufbau prüfen, ob alle Schichten vollständig** — sonst masselos (2.1) |
| Fenster | `Opening` mit `@openingType` | `FixedWindow`/`OperableWindow`/Skylight/Türen |
| Fensterfläche | `Opening/RectangularGeometry/Width × Height` | Außenmaß |
| Fenster-U-Wert | `WindowType/U-value` oder `Opening/U-value` | **beide Orte prüfen**; nur `WPerSquareMeterK` und `BtuPerHourSquareFtF` |
| g-Wert | `WindowType/SolarHeatGainCoeff` | ggf. mehrere mit `solarIncidentAngle` → den bei 0° nehmen |
| Ort, Klima | `Location/Latitude`, `/Longitude`, `/Elevation` | Dezimalgrad, optional; `ZipcodeOrPostalCode` ist Pflicht, aber US-Format |
| Baualtersklasse | — | **fehlt in gbXML vollständig** (IFC hat `Pset_BuildingCommon.YearOfConstruction`) → TABULA-Vorbelegung über Dialog |
| Wärmebrückenzuschlag | — | fehlt |
| Nachbarbebauung | `Surface` mit `surfaceType="Shade"` | in den Beispielen vorhanden (36–79 Flächen), für EPOS derzeit nicht auswertbar |

### 5.2 Export: EPOS → gbXML

| EPOS | gbXML | Was EPOS aus einem Einzonenmodell schreiben kann |
|---|---|---|
| — | `gbXML/@version` | `6.01` (nicht `8.01`, siehe 1.1) |
| — | `@lengthUnit="Meters"`, `@areaUnit="SquareMeters"`, `@volumeUnit="CubicMeters"`, `@temperatureUnit="C"`, `@useSIUnitsForResults="true"` | Pflicht, unproblematisch |
| Gebäudeart | `Building/@buildingType` | `SingleFamily` / `MultiFamily` — Pflichtattribut, passt |
| Standort | `Location` | Breite/Länge aus EPOS; `ZipcodeOrPostalCode` mit deutscher PLZ füllen |
| Nordrichtung | `Location/CADModelAzimuth` | `0`, da EPOS-Azimute bereits geografisch |
| Zone | `Space` + `Zone`, verbunden über `zoneIdRef` | 1:1 |
| Zonenfläche, Volumen | `Space/Area`, `Space/Volume` | direkt |
| Sollwerte | `Zone/DesignHeatT`, `/DesignCoolT` | direkt |
| Luftwechsel | `Space/AirChangesPerHour` | direkt |
| Innere Gewinne | `Space/PeopleNumber`, `/LightPowerPerArea`, `/EquipPowerPerArea` | direkt, Einheiten Pflicht |
| Nutzung | `Space/@spaceType` | **weglassen** — kein passender Wert (1.9) |
| Bauteilgruppe | `Surface` je Gruppe, `@surfaceType`, `@constructionIdRef` (Pflicht!) | direkt; **mindestens 4 Flächen je `Campus`** (1.3) |
| Fläche, Azimut, Neigung | `RectangularGeometry/Width, Height, Azimuth, Tilt` | direkt — Breite aus Fläche/Höhe |
| **Geometrie** | `PlanarGeometry/PolyLoop` | **muss synthetisch erzeugt werden** (Abschnitt 3), sonst kein Zielwerkzeug |
| Randbedingung | `AdjacentSpaceId` 1× außen/Erdreich, 2× Nachbarzone | direkt aus dem EPOS-Zonenmodell |
| Aufbau | `Construction` + `LayerId` → `Layer` + `MaterialId` → `Material` | **vollständig schreiben** — sonst masselos im Ziel (1.6) |
| Schichtwerte | `Thickness`, `Conductivity` (`WPerMeterK`), `Density` (`KgPerCubicM`), `SpecificHeat` (`JPerKgK`) | direkt |
| U-Wert | `Construction/U-value` (`WPerSquareMeterK`) | zusätzlich zur Schichtung schreiben |
| Fenster | `Opening` + `WindowType` | U-Wert und g-Wert direkt |
| Ergebnisse | `Results` (`resultsType="HeatLoad"`, `"Energy"`, `"DryBulbTemperature"`) | technisch möglich, praktisch fragwürdig (1.8) — **Stufe 2** |
| Regelung, Anlagentechnik | `AirLoop`, `HydronicLoop`, `AirSystem`, `ZoneHVACEquipment` | **nicht schreiben** — EPOS-Anlagenmodell passt nicht auf das US-HVAC-Schema |
| Wärmebrücken, Baualter, Verschattung | — | **kein Ziel im Schema** |

**Verluste zusammengefasst.** Beim **Import** gehen Baualter, Wärmebrückenzuschlag und
Bauteilfeinheiten verloren, und die Stoffwerte sind in rund einem Drittel der Fälle unvollständig.
Beim **Export** geht die echte Raumgeometrie verloren (es gab nie eine), ebenso die Nutzungssemantik
und die Anlagentechnik; **erhalten bleiben Bilanzgrößen**: Flächen, Orientierungen, U-Werte,
Schichtaufbauten mit Speichermasse, Sollwerte, Luftwechsel, innere Gewinne. Für eine Übergabe an
einen Energieberater oder Fachplaner ist das genau die richtige Menge — für eine
Weiterverarbeitung als Architekturmodell nicht.

---

## 6. Empfehlung

### Import — Pflicht in G4

**14–22 PT.** Deutlich weniger als der IFC-Weg (Befund C: 25–40 PT für Stufe 1), aus drei Gründen:
keine Bibliothek einzubinden und keine Lizenzfrage zu klären, kein Geometriekernel-Ersatz nötig
(`RectangularGeometry` liefert Fläche, Azimut und Neigung fertig), und die thermische Topologie ist
Pflichtbestandteil des Schemas statt optionaler Beigabe.

Aufteilung: Lesemodell und Einheitenumrechnung 4–6 PT; Aggregation auf das VDI-6007-Zonenmodell
inklusive Nachbarschaftsauflösung und Fensterabzug 5–8 PT; Zuordnungsdialog mit
Herkunftskennzeichnung je Feld (gbXML / Vorgabe / manuell) und TABULA-Vorbelegung 3–5 PT — hier
lässt sich der für den IFC-Import ohnehin geplante Dialog mitbenutzen; Tests gegen die vier
Beispieldateien 2–3 PT.

**Risiken.** (1) *Leere Bauphysik* — der Architekturexport-Fall aus 2.1 ist kein Sonderfall, er ist
ein Viertel der Stichprobe; der Importer muss ohne Konstruktionen sinnvoll weiterlaufen.
(2) *Unvollständige Stoffwerte* — ein Aufbau mit einer einzigen R-Wert-Schicht ist für VDI 6007
unbrauchbar; die Prüfung je Aufbau statt je Material ist einzuplanen. (3) *Kodierung* — die Datei
als Strom öffnen, nie als Text lesen. (4) *Einheiten* — global und lokal; die US-Dateien in Fuß und
Fahrenheit gehören in die Testfälle. (5) *Datenlage* — dieselbe Einschränkung wie bei IFC gilt auch
hier: für den deutschen Wohngebäudebestand gibt es kaum gbXML-Dateien. Der Import ist ein
Komfort-Feature für den Neubau- und Nichtwohnteil, kein Ersatz für die manuelle Eingabe.

### Export — G7

**Zweistufig, 8–12 PT plus 8–14 PT.**

*Stufe 1 (8–12 PT) — schemagültiger Datenexport.* Alles außer Geometrie: `Campus`, `Building`,
`Space`, `Zone`, `Surface` mit `RectangularGeometry`, `Opening`, vollständige
`Construction`/`Layer`/`Material`/`WindowType`. Validierung gegen das XSD im Regressionstest.
Nutzen: Übergabe an Solar-Computer, EVEBI und andere bilanzierende Werkzeuge; Beleg- und
Archivformat; Grundlage für den Rundlauf Export→Import als Regressionstest.

*Stufe 2 (8–14 PT) — synthetische Quadergeometrie.* Erzeugung kantenschlüssiger
`PlanarGeometry/PolyLoop` je Fläche aus Zonenfläche, -volumen und Bauteilgruppen; Fenster als
Rechtecke in der Wandfläche. Erst damit wird der Export für OpenStudio/EnergyPlus, IES VE und
DesignBuilder überhaupt lesbar. **Ohne Stufe 2 hat der Export keinen Wert für die
Simulationswerkzeuge** — das ist die zentrale Aussage dieses Befundes und sollte die Entscheidung
tragen, ob G7 sich lohnt.

**Risiken beim Export.** (1) *Falsches Versprechen* — ein Quader mit richtiger Bilanz und erfundener
Gestalt darf nicht als Gebäudemodell auftreten; Kennzeichnung in Datei und Oberfläche ist Pflicht.
(2) *Masselose Aufbauten* — wenn EPOS in einem Projekt nur U-Werte führt (Bauteilgruppen ohne
Schichtung), erzeugt der Export im Zielwerkzeug ein masseloses Gebäude; dieser Fall gehört
abgefangen und dem Anwender gemeldet. (3) *Versionsnummer* — `6.01`, nicht `8.01`; ein Kommentar im
Quelltext mit Verweis auf 1.1 verhindert, dass das jemand „korrigiert“. (4) *Mindestens vier
Flächen je `Campus`*.

### Was zu entscheiden ist

1. **Lohnt G7 überhaupt?** Nur mit Stufe 2 (synthetische Geometrie). Ohne sie ist der Export ein
   Datenblatt in XML-Form — was legitim sein kann, aber keine Interoperabilität mit
   Simulationswerkzeugen bedeutet. Das sollte vor der Aufwandsfreigabe klargestellt sein.
2. **XSD ausliefern oder nicht?** Vorschlag: nicht ausliefern, nur im Test verwenden (4.4) — damit
   ist die ungeklärte Lizenzlage (1.10) für das Produkt gegenstandslos.
3. **Testdateien.** Die vier gbxml.org-Beispieldateien stehen ohne Lizenz; vor Aufnahme in ein
   EPOS-Regressionstest-Repositorium klären oder durch selbst erzeugte Dateien ersetzen. Die
   ASHRAE-RP-1810-Testfälle haben ebenfalls keine ausgewiesenen Nutzungsbedingungen.
4. **Reihenfolge gegenüber IFC.** gbXML ist billiger, liefert die thermische Topologie zuverlässiger
   und braucht keinen Geometriekernel. Befund C ordnete gbXML in Stufe 3 ein — nach diesem Befund
   spricht mehr dafür, den **gbXML-Import vor dem IFC-Import** zu bauen: er ist die kleinere
   Aufgabe, er schafft das Import-Zuordnungsgerüst, das der IFC-Weg dann mitbenutzt, und er
   erschließt mit Archicad den Autorensystem-Weg, der die thermischen Daten am vollständigsten
   liefert.
