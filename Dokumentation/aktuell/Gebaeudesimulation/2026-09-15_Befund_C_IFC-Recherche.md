# Befund C — IFC-Import für das VDI-6007-Gebäudemodell (15.09.2026)

**Protokoll.** Befund eines Recherche-Agenten (Modell Opus, mit Quellenprüfung im Netz und
Auszählung öffentlicher Beispieldateien) im Auftrag des Konzepts
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Sitzung vom 15.09.2026. Wortlaut wie geliefert; das Gegenlesen hat die Lizenzbewertung der
CDDL-1.0, die Zeile `Xbim.Geometry` (Vorabversion, LGPL-Abhängigkeit OCCT) und die
Zahlenangaben zur Interoperabilitätsstudie korrigiert — bei Widerspruch gilt das Konzept
(Kapitel 7).

---

**Kurzantwort:** Ja, technisch geht es — aber IFC liefert die VDI-6007-Daten **nicht fertig**, sondern nur Rohmaterial wechselnder Qualität. Ein reiner C#-Kern ohne Geometrie-Kernel kommt für Stufe 1 weit genug, plattformfrei bis iOS. Die eigentliche Arbeit liegt nicht im Parsen, sondern im Zuordnungsdialog und in den Defaults.

## 1. IFC-Schema: was thermisch drinsteht

Geprüft gegen IFC 4.3.2.0 (IFC4X3_ADD2, = ISO 16739-1:2024, buildingSMART, ISO) und IFC4 ADD2 TC1.

**Räume/Zonen.** `Qto_SpaceBaseQuantities` trägt `Height`, `NetFloorArea`, `GrossFloorArea`, `NetVolume`, `GrossVolume`, `NetWallArea`, `GrossWallArea` — genau die Größen für Wohnfläche, Raumhöhe und beheiztes Volumen. `IfcSpace` hat die inverse Relation `BoundedBy` und `ElevationWithFlooring`; `IfcZone` gruppiert Räume über `IfcRelAssignsToGroup` (ein Raum kann in mehreren Zonen liegen — beim Import zu beachten).

**Space Boundaries.** `IfcRelSpaceBoundary` hat `RelatingSpace`, `RelatedBuildingElement` (seit IFC4 **Pflicht**), `ConnectionGeometry` (OPTIONAL — Kurve 2D oder Fläche 3D), `PhysicalOrVirtualBoundary`, `InternalOrExternalBoundary`. Die Subtypen `1stLevel` und `2ndLevel` sind **neu in IFC4** — IFC2x3 kennt nur die Basisklasse. Die Norm ist deutlich: 1st-Level-Boundaries *"cannot be directly used for thermal analysis"*; erst 2nd Level definiert *"the heat transfer surfaces on both sides of building elements"*, mit Typ 2a (Raum gegenüber, verknüpft über `CorrespondingBoundary`) und 2b (Bauteil gegenüber). Fenster/Türen hängen über `ParentBoundary`/`InnerBoundaries` an der Wand-Boundary — **die Elternfläche wird nicht ausgeschnitten, beide überlappen**. Wer Bruttoflächen addiert, doppelt sonst.

**U-Werte und g-Werte.** `ThermalTransmittance` (`IfcThermalTransmittanceMeasure`) und `IsExternal` (`IfcBoolean`) stehen in `Pset_WallCommon`, `Pset_WindowCommon`, `Pset_SlabCommon`, `Pset_RoofCommon`, `Pset_DoorCommon`. Fenster und Türen führen zusätzlich `Infiltration` (m³/h bei 50 Pa) und `GlazingAreaFraction`. Der g-Wert liegt in `Pset_DoorWindowGlazingType` als `SolarHeatGainTransmittance` — die Spezifikation sagt wörtlich *"The SHGC is referred to also as g-value (g = τe + qi)"* —, dazu `ThermalTransmittanceSummer`/`Winter`, `GlassLayers`, `FillGas`.

**Thermische Masse.** `IfcMaterialLayerSet`/`IfcMaterialLayer` liefert den Schichtaufbau (Dicken, gestapelt ohne Lücken; Luftschichten als eigene Layer mit `IsVentilated`). Die Stoffwerte hängen als `Pset_MaterialThermal` (`ThermalConductivity`, `SpecificHeatCapacity`) und `Pset_MaterialCommon` (`MassDensity`, `Porosity`) am `IfcMaterial`. Damit wäre λ·ρ·c je Schicht vollständig — exakt der Input für das VDI-6007-RC-Ersatzmodell.

**Mengen.** `Qto_WallBaseQuantities`: `GrossSideArea` (Ansichtsfläche der Wandmittelebene *ohne* Öffnungsabzug), `NetSideArea` (*mit*), `Length`, `Height`, `Width`. `Qto_WindowBaseQuantities`: nur `Width`, `Height`, `Perimeter`, `Area` (Außenmaß der Zarge). `Qto_SlabBaseQuantities`: `GrossArea`/`NetArea`, `Width` (= Dicke).

**Orientierung und Ort.** `TrueNorth` in `IfcGeometricRepresentationContext` ist OPTIONAL, 2D-Richtung, Default `[0,1]`; bei vorhandenem `IfcMapConversion` ist sie *nur informativ*. `IfcSite.RefLatitude`/`RefLongitude` sind `IfcCompoundPlaneAngleMeasure` — **Integer-Tupel Grad/Minuten/Sekunden/Millionstel**, nicht Dezimalgrad; eine klassische Importer-Falle. `RefElevation` ist eine Länge.

**Nutzung.** `Pset_SpaceThermalRequirements` (in IFC4 ADD2 TC1: `SpaceTemperature`, `SpaceTemperatureWinterMin/Max`, `SpaceTemperatureSummerMin/Max`). **In IFC 4.3 ist dieses Pset nicht mehr enthalten.** `Pset_SpaceOccupancyRequirements` liefert `OccupancyNumber`, `AreaPerOccupant`; `Pset_SpaceThermalLoad` liefert `People`, `Lighting`, `EquipmentSensible`, `InfiltrationSensible`, `AirExchangeRate` — allerdings sind dort *alle* Werte als `IfcPowerMeasure` typisiert, auch die Luftwechselrate. Das ist ein bekannter Schemafehler.

**Baualter.** `Pset_BuildingCommon`: `YearOfConstruction` (als `IfcLabel`, also Text — nicht Zahl!), `NumberOfStoreys`, `GrossPlannedArea`, `IsLandmarked`.

**Was ist MVD-Pflicht?** Space Boundaries sind in keiner der Haupt-MVDs enthalten. Die MVD-Datenbank von buildingSMART führt dafür eine eigene MVD: Space Boundary Add-on View (SBAV), Status Final, v1.1 — historisch ein Add-on zur IFC2x3 Coordination View 2.0. Die Reference View 1.2 nennt in ihrem Scope räumliche Elemente mit Geometrie, Properties, Quantities und Klassifikation — Boundaries kommen im gesamten Dokument nicht vor. Design Transfer View ist eine Obermenge der RV, aber immer noch Status Draft. Praktische Folge: **ein zertifizierungskonformer IFC4-Export enthält weder 2nd-Level-Space-Boundaries noch garantiert U-Werte.**

## 2. Was die Autorensysteme tatsächlich exportieren

| System | IfcSpace | Space Boundaries | Therm. Psets | Default-MVD |
|---|---|---|---|---|
| Revit | ja (Option „rooms/areas/spaces in 3D views") | Option None/1st/2nd Level | `ThermalTransmittance` nicht automatisch | IFC2x3 CV 2.0 |
| Archicad | ja (Zonen) | nur an/aus — immer 2nd Level | über Übersetzer möglich | IFC2x3 CV 2.0 |
| Allplan | ja (gefiltert) | nicht dokumentiert / nein | begrenzt | wählbares Austauschprofil |
| Vectorworks | ja | nicht dokumentiert | — | gbXML-Pfad für Energie |
| SketchUp | unklar | nicht belegt | nein | IFC4 |

Revits Option ist im Exporter-Hilfetext beschrieben; der Exporter ist Open Source (Autodesk/revit-ifc). Der kritische Punkt: `Pset_WallCommon.ThermalTransmittance` muss in Revit als Shared Parameter mit exakt passendem Namen angelegt oder gemappt werden — es kommt sonst nichts. Archicad exportiert Boundaries zusammen mit den Zonen und teilt sie an anschließenden Bauteilen und Öffnungen auf; ein 1st-Level-only ist nicht wählbar. Für Allplan belegt ein RWTH-Paper: *"ALLPLAN Version 2023 is unable to export SBs to IFC"* (Automation in Construction 179 (2025)).

**Der zentrale Erfahrungsbefund** aus derselben Quelle: buildingSMART-Zertifizierung garantiert weder exportfähige noch vollständige Energiedaten. Revit- und Archicad-Exporte lieferten zwar 2nd-Level-Geometrie, *"but missed information on the corresponding boundaries in adjacent thermal zones"*. Ergänzend aus der deutschen Praxis (build-ing.de zu Plancal nova): *"das Architekturmodell ist in der Regel für eine thermische Betrachtung nicht geeignet"* — mehrschalige Wandsysteme als mehrere Elemente, Putz und Bodenbeläge als eigene Bauteile, U-Werte fehlen.

**Wie es die Etablierten machen:** IDA ICE liest gar keine Space Boundaries, sondern rekonstruiert Geometrie selbst aus `IfcSpace` und Wänden — und liest keine U-Werte aus Psets, sondern nur Konstruktions-*namen*, die der Anwender manuell auf die eigene Datenbank mappt. Hottgenroth/HottCAD importiert IFC 2x3/4 mit Konvertierungs- und Anreicherungsmodus, ohne Space Boundaries zu fordern. Solar-Computer löst es über den GBIS IFC-Manager als Revit-Plugin. DesignBuilder importiert gbXML, nicht IFC. bim2sim (LGPL-3.0) erzeugt 2nd-Level-Boundaries selbst und reichert fehlende Bauteildaten statistisch über das Baujahr an; IFC2SB (RWTH-E3D, GPL-3.0) macht dasselbe auf OpenCascade-Basis. TEASER hat gar keinen IFC-Import. **Das Muster ist eindeutig: kein ernsthaftes Energiewerkzeug verlässt sich auf die Exporte. Alle rekonstruieren und reichern an.**

## 3. gbXML

Aktuell Schema 8.01, Januar 2026 (gbxml.org, Repo GreenBuildingXML/gbXML_Schemas). Der große Vorteil: die thermische Zonentopologie ist Teil des Schemas statt optionaler Beigabe — Archicad exportiert gbXML nativ inklusive Raumbegrenzungsdaten der 2. Ebene, Revit über die Energy Settings. Aber: kein ISO-Standard, eine explizite Lizenzangabe war weder auf gbxml.org noch im Schema-Repo auffindbar, und die Tool-Liste ist erkennbar veraltet. Im DACH-Raum finden sich Solar-Computer (GBIS), DDS-CAD, AX3000; für Hottgenroth und ZUB Helena kein dokumentierter gbXML-Import, wohl aber IFC. Der deutsche Nachweisweg ist DIN V 18599, und die Normungsarbeit (VDI 2552 Blatt 11.9 „Bauphysik") läuft auf IFC. **Fazit: gbXML ist Ergänzung, nicht Ersatz — und gehört in Stufe 3, nicht Stufe 1.**

## 4. .NET-Bibliotheken (Stand 15.09.2026, aus NuGet-/GitHub-API verifiziert)

| Bibliothek | Version | Lizenz | TFMs | Nativ? | IFC | Geometrie | Aktivität |
|---|---|---|---|---|---|---|---|
| Xbim.Essentials (Xbim.Ifc4 / Ifc2x3 / Ifc4x3 / Common / IO.MemoryModel) | 6.1.605 | CDDL-1.0 | net10.0, net8.0, netstandard2.0/2.1 | rein verwaltet | 2x3, 4, 4.3 | nein | push 28.08.2026, 575 Sterne, ~1,5 Mio Downloads |
| Xbim.Geometry | 6.3.891-netcore | CDDL-1.0 (OCCT-Abhängigkeit LGPL-2.1, siehe Konzept) | net472, net8.0 | ja, Windows-only | dito | ja (OCCT) | push 10.07.2026 |
| GeometryGymIFC_Core | 26.8.17 | MIT | netstandard2.0, net6/7/8 | rein verwaltet | 2x3, 4, 4.3, 4.4 | nein | push 14.08.2026, 309 Sterne |
| ara3d/IFC-toolkit | pre-release | MIT | net8.0 | optional (web-ifc-DLL) | STEP | via C++-DLL | 76 Commits, kein NuGet |
| Hypar.IFC4 | 1.2.0 | MIT | net6.0 | nein | IFC4 | nein | ifc-gen: letzter Push 2023 |
| Ifc4Net | 1.0 | — | — | — | — | — | NuGet-Blob existiert nicht mehr |
| IfcOpenShell | — | LGPL-3.0 | C++/Python | ja | alle | ja | sehr aktiv, keine gepflegte .NET-Anbindung |
| web-ifc | 0.0.77 | MPL-2.0 | WASM/C++ | ja | alle | ja | sehr aktiv |
| BIMserver | — | AGPL-3.0 | Java | Server | alle | ja | push 03.2026 |

**Die entscheidende Erkenntnis aus dem Paketinhalt:** `Xbim.Geometry.Engine.Interop` liefert `lib/net8.0/win-x64/Xbim.Geometry.Engine.dll` plus `Ijwhost.dll` — das ist C++/CLI Mixed-Mode, und das gibt es nur unter Windows. `Xbim.Geometry.Occt 7.8.1` enthält ausschließlich `lib/native/v143/win-x64/…`. Für iOS ist das endgültig disqualifiziert. **Umgekehrt: `Xbim.Ifc4 6.1.605` liefert echte `lib/net10.0`-Assemblies** — ein direkter Treffer für `EPOS.Kern` (`net10.0`) und über die Kompatibilitätskette auch für `EPOS.iOS`. Die Entity-Factory ist generierter Code mit 1438 `case`-Zweigen, kein `Reflection.Emit` — also AOT-tauglich. Das verbleibende iOS-Risiko ist Trimming: `ExpressMetaData` nutzt `module.GetTypes()`, was der Trimmer nicht statisch auflösen kann. Lösbar mit einem `TrimmerRootDescriptor`, aber einzuplanen.

**Lizenzbewertung (im Konzept präzisiert):** CDDL-1.0 ist file-level copyleft — die Einbindung in ein proprietäres Produkt ist zulässig; die Auflagen (Quelltextverfügbarkeit aller ausgelieferten CDDL-Dateien, Vermerke, Lizenztext) stehen im Konzept, Kapitel 7.4. IfcOpenShell (LGPL-3.0) und IFC2SB (GPL-3.0) sind für statisches Einbetten in ein geschlossenes Produkt heikel bzw. ausgeschlossen. BIMserver (AGPL-3.0) scheidet aus.

**Empfehlung Bibliothek:** Xbim.Essentials für den Kern (net10.0, IFC 4.3, größtes Ökosystem) — GeometryGymIFC_Core als MIT-Alternative, falls die CDDL-Frage nicht abgeräumt werden soll. Xbim.Geometry nie im Kern.

## 5. Ohne Geometrie-Kernel?

**Ja, für Stufe 1 — mit klar benannten Grenzen.** Was ohne Kernel funktioniert: U-Werte und `IsExternal` aus den Psets; Bruttoflächen aus `Qto_WallBaseQuantities.GrossSideArea`; Fensterflächen aus `Qto_WindowBaseQuantities.Area`; beheiztes Volumen als Summe `Qto_SpaceBaseQuantities.NetVolume`; thermische Masse aus `IfcMaterialLayerSet` × `Pset_MaterialThermal`/`MaterialCommon`; Azimut aus der Kette `IfcLocalPlacement` → Wand-`RefDirection`, kombiniert mit `TrueNorth`. Die Wandachse ist spezifiziert: bei `IfcMaterialLayerSetUsage` gibt es zwingend eine `'Axis'`-Repräsentation, deren Kurve auf der x/y-Ebene liegt und parallel zur x-Achse des Objektkoordinatensystems ist. Der Azimut ist damit reine Matrixmultiplikation der verschachtelten Placements.

Die Robustheitsprobleme: (1) Quantities fehlen oft — Revit exportiert Basismengen nur bei gesetztem Häkchen; ohne `GrossSideArea` bleibt nur Länge × Höhe aus `IfcExtrudedAreaSolid`. (2) Fensterabzug: `GrossSideArea` ist ohne, `NetSideArea` mit Abzug; bei Space Boundaries überlappen Eltern- und Kind-Boundary. (3) Gedrehte Gebäude: `TrueNorth` fehlt oft oder es existiert zusätzlich `IfcMapConversion`. (4) `IsExternal` ist optional und wird unzuverlässig gesetzt; Fallback: eine Wand ist außen, wenn nur eine Space Boundary auf sie zeigt. (5) Mehrschalige Wände als mehrere Elemente. (6) Geschosse: drei Ebenen relativer Höhen, jede optional. (7) `YearOfConstruction` ist Text. (8) Nachbarbebauung fehlt praktisch immer. **Bewertung:** für EFH/MFH mit rechteckigem Grundriss reicht das; für gegliederte Nichtwohngebäude ohne Quantities wird es unzuverlässig.

## 6. Testdateien und Praxisrelevanz

Die folgenden Angaben wurden durch tatsächlichen Download und Grep verifiziert.

| Datei | Spaces | Boundaries | Therm. Psets |
|---|---|---|---|
| AC20-FZK-Haus.ifc (KIT/IAI, EFH, IFC4/Archicad 20, 2,5 MB) | 8 | 81 × `'2ndLevel','2a'` | ja: 33× ThermalTransmittance, 7× Pset_SpaceThermalRequirements |
| AC-20-Smiley-West-10-Bldg (KIT/IAI, Reihenhaus, 10 WE) | 140 | 1689 × `'2a'` | ja: 180× ThermalTransmittance |
| AC20-Institute-Var-2.ifc (KIT/IAI, Büro, 5 Geschosse) | 83 | 1000 | nein (0× ThermalTransmittance) |
| FM_ARC_DigitalHub_with_SB.ifc (RWTH E3D GitLab, IFC4/Revit 2019) | 59 | 1560/2582 echte 1st-/2nd-Level-Entities | 719× ThermalTransmittance, 85× Pset_DoorWindowGlazingType; keine Lizenzdatei |
| Duplex Apartment (buildingSMART Community, IFC2x3/Revit 2011, Git LFS) | 21 | 265, ausschließlich 1st Level | 0× ThermalTransmittance, keine BaseQuantities |

KIT-Dateien: Nutzung „uneingeschränkt" mit Namensnennung. **Wichtiger Fund:** Die Archicad-Dateien schreiben trotz IFC4 die Basisklasse `IFCRELSPACEBOUNDARY`, nicht `IFCRELSPACEBOUNDARY2NDLEVEL`; das 2nd-Level-Merkmal steckt nur in `Name='2ndLevel'` / `Description='2a'`. Ein Importer, der auf den Entity-Typ prüft, findet hier nichts. bim2sim-test-resources: Lizenz laut GitHub-API `null`. Das openifcmodel-Repository der Universität Auckland ist faktisch tot.

**Praxisrelevanz:** Für die EPOS-Zielgruppe — Heizungserneuerung im Bestand nach VDI 4645, EFH/MFH — ist die Ausbeute gering. Für den Wohngebäudebestand existiert praktisch kein Modellbestand. IFC-Modelle kommen realistisch aus Neubau/großer Sanierung im Nichtwohnbereich und aus Scan-to-BIM-Aufnahmen. **IFC ist damit ein Komfort-Feature für den Nichtwohn- und Quartiersteil, kein Ersatz für die manuelle Eingabe.**

## 7. Empfehlung

`EPOS.Kern` ist `net10.0`, `EPOS.iOS` ist `net10.0-ios`, und unter `EPOS.Kern/Allgemein/Import/` existiert bereits ein etabliertes Muster (`KatalogImportAblauf`, `KlimaImportAblauf`, `…ImportProfil`). Ein `IfcImportAblauf` + `IfcImportProfil` fügt sich dort ohne Architekturbruch ein.

**Stufe 1 — IFC-Lesen ohne Geometrie-Kernel, reines C# im Kern (25–40 PT).** Xbim.Ifc4 + Xbim.IO.MemoryModel in `EPOS.Kern`. Extraktion: Spaces/Zonen → Wohnfläche, Raumhöhe, Volumen; Psets → U-Werte je Bauteilgruppe (flächengewichtet auf die fünf EPOS-Kategorien); Fenster über Placement-Azimut in die Sektoren; `SolarHeatGainTransmittance` → `Fensterdurchlassgrad`; Schichtaufbau → `Bauweise`; `YearOfConstruction` → `Baualtersklasse`. Boundaries sowohl über Entity-Typ als auch über `Name`/`Description` erkennen (Archicad-Fall). Zuordnungsdialog mit Herkunftskennzeichnung je Feld (IFC / Default / manuell) und Vorbelegung aus TABULA/IWU. Risiko: iOS-Trimming (Mitigation: TrimmerRootDescriptor, früh testen); große Modelle im Speicher — für Blazor Hybrid Größenlimit setzen.

**Stufe 2 — Geometrieableitung (30–60 PT).** Eigene, schlanke Auswertung von `IfcExtrudedAreaSolid` + Placement-Kette für Flächen und Azimute, wenn Quantities fehlen; Öffnungsabzug über `IfcRelVoidsElement`. Kein externer Kernel im Kern. Nur angehen, wenn Stufe 1 in der Praxis zu oft leer ausgeht.

**Stufe 3 — gbXML (10–15 PT).** Reines XSD-Deserialisieren, plattformfrei. Lohnt sich, sobald Archicad-Anwender im Feld sind.

**Was zu entscheiden ist:** Lizenz (CDDL-1.0 xBIM freigeben oder MIT GeometryGym), iOS-Umfang, Pflichtfelder (Vorschlag: nur Wohnfläche und Raumhöhe Pflicht, alles andere TABULA-Defaults mit sichtbarer Herkunftsmarkierung), Testlizenzen (RWTH-GitLab- und bim2sim-Dateien vor Aufnahme in ein Regressionstest-Repository klären).
