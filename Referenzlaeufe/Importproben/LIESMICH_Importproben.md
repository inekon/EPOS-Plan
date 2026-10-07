# Importproben — Herkunft und Lizenzstand

Die Dateien dieses Ordners sind die Proben der Importleser in `EPOS.Kern.Tests`. Sie liegen als
gewöhnliche Blobs (kein LFS) und sind in `.gitattributes` mit `-text` von der
Zeilenenden-Normierung ausgenommen: Kodierung und Zeilenenden bleiben byteweise, wie sie sind
(Windows-1252 bei den VDI-3805-Ausschnitten, UTF-16LE bei `gbxml_haus_utf16.xml`).

Eine Zeile je Probe. „Eigenes Werk" heißt: im Projekt selbst geschrieben oder erzeugt, ohne
Fremddaten. Wo die Herkunftspapiere keine Lizenz nennen, steht „nicht belegt".

## Gebäudeimport gbXML (Stufe G4c)

Selbst erzeugt am 24.09.2026 nach Datenaustauschkonzept 8.3 (keine gbxml.org-Beispieldateien, keine
Werkzeugexporte): neutrale Bezeichner, runde Werte, keine Normzahlen und keine Herstellerdaten.
Die Fuß- und die UTF-16-Datei entstanden aus derselben Gebäudedefinition wie die SI-Datei.

| Datei | Zweck | Herkunft | Lizenzstand |
|---|---|---|---|
| `gbxml_haus_si.xml` | Probenhaus in Meter/Celsius, UTF-8: drei beheizte Räume über einem unbeheizten Keller, Außenwände in vier Himmelsrichtungen mit 23 m² Fenstern und einer Außentür, Flachdach, Kellerdecke, Innenwand, Geschossdecken; unsymmetrische Außenwand mit Innendämmung, Ostwand des Obergeschosses nur als `PolyLoop`, ein Fenstertyp mit zwei g-Werten (0° und 60°) | selbst erzeugt | eigenes Werk |
| `gbxml_haus_fuss.xml` | dasselbe Gebäude in Feet/SquareFeet/CubicFeet/°F und BTU-Einheiten, exakt umgerechnet; ein Heizsollwert mit lokalem `unit="C"`, ein Raum mit Fläche je Person (Probe 6) | selbst erzeugt | eigenes Werk |
| `gbxml_haus_utf16.xml` | die SI-Datei als UTF-16LE mit BOM (Probe 5) | selbst erzeugt | eigenes Werk |
| `gbxml_ohne_konstruktionen.xml` | ohne `Construction`, `Material` und `WindowType` — U-Werte aus der Baualtersklasse (Probe 7) | selbst erzeugt | eigenes Werk |
| `gbxml_rwert_schicht.xml` | ein Aufbau mit einer Schicht nur mit R-Wert — masselos (Probe 8) | selbst erzeugt | eigenes Werk |
| `gbxml_nachbar_leer.xml` | ein `spaceIdRef` ohne Ziel — gilt als unbeheizt | selbst erzeugt | eigenes Werk |
| `gbxml_ohne_nachbar.xml` | eine Außenwand ohne `AdjacentSpaceId` und eine Verschattungsfläche | selbst erzeugt | eigenes Werk |
| `gbxml_norddrehung.xml` | `CADModelAzimuth` 60° — gemeldet, nicht aufaddiert (Probe 21) | selbst erzeugt | eigenes Werk |
| `gbxml_kennung_lang.xml` | eine Flächenkennung mit 80 Zeichen (Probe 24) | selbst erzeugt | eigenes Werk |
| `gbxml_zwei_gebaeude.xml` | zwei Gebäude in einer Datei, eines je Lauf (U13) | selbst erzeugt | eigenes Werk |
| `gbxml_nettoflaeche_negativ.xml` | ein Fenster größer als seine Wand — Nettofläche 0 (U14) | selbst erzeugt | eigenes Werk |
| `gbxml_innenflaechen_teilweise.xml` | drei beheizte Räume (60 m², 3 m hoch) mit vollständigen Außenaufbauten, zwei massiven Innenwänden und einer Ständerwand nur mit R-Wert — die Innenflächen sind nicht vollständig (Bauteilvorschlag G4b, innere Masse nach Datenlage) | selbst erzeugt | eigenes Werk |
| `gbxml_zonen_viele.xml` | 60 beheizte Räume zu je 10 m² auf zwei Geschossen, je eine Außenwand und Bodenplatte bzw. Dach, keine Innenflächen — je Raum eine Zone (X3) ergibt 60 Zonen über der Obergrenze 50: Warnung mit dem Vorschlag der Geschossregel X2 (Zonenimport G6c, M12) | selbst erzeugt | eigenes Werk |

## Gebäudeimport IFC (Stufe G4a)

Selbst erzeugt am 25.09.2026 mit xBIM (`MemoryModel` im Schreibmodus) durch die Testhilfe
`EPOS.Kern.Tests/IfcProbenErzeuger.cs` — deterministisch (feste `GlobalId`s, fester Zeitstempel,
feste Kopfangaben, CRLF); `IfcProbenTests` hält fest, dass eine erneute Erzeugung byte-gleich wäre
(beim `.ifczip` der entpackte Inhalt). Neutrale Bezeichner, runde Werte, keine Normzahlen und keine
Herstellerdaten; nichts aus dem Netz. Längen in Millimetern, Flächen in m² ohne Prefix. Die
KIT-Probe `AC20-FZK-Haus.ifc` ist nicht aufgenommen (entscheidet der Anwender).

| Datei | Zweck | Herkunft | Lizenzstand |
|---|---|---|---|
| `ifc4_haus.ifc` | Probenhaus IFC4: ein Gebäude, zwei beheizte Vollgeschosse über einem Kellergeschoss mit unbeheiztem „Keller"; vier beheizte Räume (130 m², Höhen 2,6/2,4 m), Außenwände in vier Richtungen mit `BaseQuantities` bzw. `Qto_WallBaseQuantities` (eine nur mit Länge × Höhe), U-Wert am Wandtyp und zwei Vorkommniswerte, fünf Fenster mit `Pset_DoorWindowGlazingType` (eines nur mit Breite × Höhe), Haustür über `OverallWidth`/`OverallHeight`, Flachdach, Kellerdecke, Geschossdecke, Bodenplatte, Innenwand; 28 Raumgrenzen als Basisklasse mit `Name='2ndLevel'`/`Description='2a'`; TrueNorth [−2, 1, 0]; `Pset_BuildingCommon.YearOfConstruction = 'ca. 1965'`; Sollwert als Bereich mit `SetPointValue` | selbst erzeugt | eigenes Werk |
| `ifc2x3_haus.ifc` | dasselbe Haus in IFC2X3 (Sollwert als `SpaceTemperatureMin`, soweit abbildbar) — dieselben Zahlen | selbst erzeugt | eigenes Werk |
| `ifc2x3_enthaltensein.ifc` | IFC2X3 nach dem Muster eines CAD-Exports: Geschosse und Räume über `IfcRelContainedInSpatialStructure` statt `IfcRelAggregates` (ein Raum über beide Wege), Raumname in `Name`, Mengen im fremden Satz `CAD_RaumQuantities` als `Area`/`Volume`/`Height`; ein Raum mit `BaseQuantities` und fremdem Satz (der Standard geht vor), ein „Abstellraum“ nach dem Namen unbeheizt; beheizt 100 m², 246 m³; dazu Bauteile ohne `IsExternal` und ohne Raumgrenze mit Angrenzung `AdjacentType` (`btaOutside`, `btaGround`, `btaHeated`, `btaUnHeated`, `btaCellarCeiling`, `btaUppermostStorey`, `btaNone`), Hüllkennung, Himmelsrichtung `Orientation (°)`, U-Wert `UValue (W/(m² K))` neben einem Wert mit falscher Einheit `ThermalTransmittance (W/(m K))`, Flächen `GrossArea`/`NetArea` im fremden Satz `CAD_BauteilQuantities` neben Lockwerten `Width`/`Length` und einem Dachfenster als Teil des Dachs — Enthaltensein, Mengenrückfall und Bauteilrückfälle | selbst erzeugt | eigenes Werk |
| `ifc2x3_referenzen.ifc` | IFC2X3 nach dem Muster eines CAD-Exports ohne Raumgrenzen, der je Raum ein `IfcRelReferencedInSpatialStructure` mit den angrenzenden Bauteilen schreibt: Geschosse KG/EG/OG, Räume über das Enthaltensein (Keller und Abstellraum nach dem Namen unbeheizt, beheizt je Geschoss 60 m²), Gebäudename „Gebäude“ (Platzhalter), kein Baujahr; eine Decke EG/OG und eine Kellerdecke, die Räume zweier Geschosse referenzieren (Trenndecken), eine oberste Decke eines Geschosses, ein Vordach außen über zwei Geschosse, Innenwände zwischen zwei Räumen eines Geschosses und Innenwände ohne Nachbarraum (zwei Geschosse, ein Raum, kein Bezug) — Trenndecken, innere Masse, Z4 ohne Raumgrenzen, Platzhaltername | selbst erzeugt | eigenes Werk |
| `ifc4_haus.ifczip` | `ifc4_haus.ifc` im ZIP-Behälter — Größengrenze gegen die entpackte Größe | selbst erzeugt | eigenes Werk |
| `ifc4x1_kopf.ifc` | Kopf mit dem nicht angenommenen Schema IFC4X1 | selbst erzeugt | eigenes Werk |
| `ifc4_zwei_gebaeude.ifc` | zwei Gebäude, eines je Lauf (U13) | selbst erzeugt | eigenes Werk |
| `ifc4_ohne_mengen.ifc` | Raum und Außenwände ohne jeden Mengensatz, die Wände mit Körper (Extrusion) — Fläche aus dem Bauteilkörper (G5-1) | selbst erzeugt | eigenes Werk |
| `ifc4_mapconversion.ifc` | Kontext mit TrueNorth UND `IfcMapConversion` (90°) — die Umrechnung gilt, TrueNorth wird nicht addiert | selbst erzeugt | eigenes Werk |
| `ifc4_schichten.ifc` | Schichtenhaus: ein beheizter Raum, vier Außenwände, Dach- und Bodenplatte ohne U-Werte, aber mit `IfcMaterialLayerSetUsage` und Stoffwerten in `Pset_MaterialThermal`/`Pset_MaterialCommon`; zwei Wände zählen innen zuerst gegen die Achse (`NEGATIVE`), zwei außen zuerst längs der Achse (`POSITIVE`) — Bauart aus den raumseitigen Schichten und Schichtfolge aus der Nutzung | selbst erzeugt | eigenes Werk |
| `ifc4_schichten_nullwerte.ifc` | dasselbe Haus, die Dämmung mit ρ = 0 und c = 0 — Stoffwerte ≤ 0 als Fehlstelle | selbst erzeugt | eigenes Werk |
| `ifc2x3_schichten.ifc` | dasselbe Haus in IFC2X3 mit `IfcThermalMaterialProperties`, `IfcGeneralMaterialProperties` und einem `IfcExtendedMaterialProperties` — die Stoffwerte werden benannt nicht gelesen | selbst erzeugt | eigenes Werk |
| `ifc4_schichtdicken_mm.ifc` | Längeneinheit `METRE`, die Schichtdicken der Außenwand aber in Millimetern (Folie 0.2, Blech 0.9, Dämmung 160., Beton 200.), das Dach in Metern (0.2, 0.016) als Gegenprobe — Rückfall „Schichtdicke in Millimetern“, das Übergehen einer Folie unter 0,5 mm und das Halten eines Blechs ab 0,5 mm | selbst erzeugt | eigenes Werk |
| `ifc4_rueckfaelle.ifc` | zwei Geschosse, Räume ohne Höhe und Volumen, Dach- und Bodenplatte ohne Mengen, keine Tür, Obergeschoss mit `GrossFloorArea` — die Vorgabe-Rückfälle für Raumhöhe, Dach-, Grund- und sonstige Fläche | selbst erzeugt | eigenes Werk |
| `ifc4_vorhangfassade.ifc` | Fassadenhaus: ein beheizter Raum (80 m²), zwei Vorhangfassaden (`IfcCurtainWall`, Süd 25 m² mit U-Wert 1,3, West 20 m² ohne U-Wert), Ost- und Nordwand mit U-Wert am Wandtyp, ein Fenster, Dach- und Bodenplatte — Vorhangfassaden im Bauteilvorschlag transparent, in den Summenfeldern unter „Sonstige Flächen" | selbst erzeugt | eigenes Werk |
| `ifc4_haus_materialnamen.ifc` | das Probenhaus (`ifc4_haus.ifc`) mit Schichtsätzen an Außenwänden, Dach, Keller- und Geschossdecke und Innenwand: Materialnamen, wie Autorensysteme sie schreiben (angehängte Kennungen, Marken wie „bewehrt"/„Verputzt", Namen deutscher und englischer Vorlagen, `Air`, eine Schraffur als Schicht, ein Sammelname ohne Treffer), die Stoffwerte in `Pset_MaterialThermal`/`Pset_MaterialCommon` voller Nullen — die Probe des Namensabgleichs N1…N7 im Bauteilvorschlag | selbst erzeugt | eigenes Werk |
| `ifc4_zonen.ifc` | Zonenhaus: Keller, Erd- und Obergeschoss mit sechs Räumen, Raumgrenzen der 2. Ebene, Polygone an einer Fassade über zwei Geschosse und an der Geschossdecke, ein Gegenstück der Datei, eine Grenze ohne Gegenstück, geschachtelte und mehrfache `IfcZone`, Klassifikation, ein Raum unter der Mindestgröße, Beheizungsregeln B3 und B5 — die Probe der Zonierung Z1, Z2, Z4 und des Vorschlags mehrerer Zonen (G6c) | selbst erzeugt | eigenes Werk |
| `ifc4_z6_sollwerte.ifc` | Zonenregel Z6 mit Heizsollwerten des Standards (`Pset_SpaceThermalRequirements.SpaceTemperature`): zwei Geschosse, acht Räume ohne Raumgrenzen und ohne Bauteile, Sollwerte 20/20,4/19,6 °C, 15 °C und 24 °C (ein WC unter der Mindestgröße), zwei Räume ohne Sollwert — Gruppen nach gerundeter Temperatur, Räume ohne Temperatur nach der Nutzung, Mindestgröße zur nächstliegenden Temperatur | selbst erzeugt | eigenes Werk |
| `ifc4_z6_cad.ifc` | Zonenregel Z6 nach dem Muster eines CAD-Exports: Satz `CAD_RaumAllgemein` mit `HeatingType`, `InsideTemperature (°C)` und `RoomType` (Präfix `mrt`), Räume über das Enthaltensein, Bauteile mit Raumbezügen, keine Raumgrenzen; beheizt, getrennt beheizt und unbeheizt, Räume ohne Temperatur — Raumtyp, Raumtemperatur ohne Sollwert, Übersteuerung der Beheizung | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_extrusion_polygon.ifc` | Raumkörper (G7f-1, Probe 29): ein Raum mit `IfcExtrudedAreaSolid` über `IfcArbitraryClosedProfileDef` mit `IfcPolyline` (L-Form, 14 m² × 3 m), Längen in mm | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_extrusion_bogen.ifc` | Raumkörper mit Bögen: Verbundkurve aus `IfcPolyline` und getrimmtem `IfcCircle`, dieselbe Form als `IfcIndexedPolyCurve` mit `IfcArcIndex`, ein `IfcCircleProfileDef` — Vermerk „Bogen“ | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_extrusion_loch.ifc` | Raumkörper aus `IfcArbitraryProfileDefWithVoids`: außen `IfcIndexedPolyCurve` 6 m × 4 m, ein Loch 2 m × 2 m — Brückenkante | selbst erzeugt | eigenes Werk |
| `ifc2x3_koerper_brep.ifc` | Raumkörper als `IfcFacetedBrep` in IFC2X3 (Quader 4 × 3 × 2,5 m, eine Fläche mit `Orientation = false`) | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_dreiecksnetz.ifc` | Raumkörper als `IfcTriangulatedFaceSet` (Quader, zwölf Dreiecke) — Randkanten ohne Diagonalen | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_vieleckssatz.ifc` | Raumkörper als `IfcPolygonalFaceSet` (Quader, sechs Vierecke) | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_abgebildet.ifc` | Raumkörper als `IfcMappedItem` mit `IfcCartesianTransformationOperator3DnonUniform` (Versatz, Maßstab 2 : 1 : 3) | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_beschnitt.ifc` | Raumkörper als `IfcBooleanClippingResult` (Quader minus Halbraum) — nur der erste Operand, Vermerk „ohne Beschnitt“ | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_offen.ifc` | Raumkörper als `IfcShellBasedSurfaceModel` mit `IfcOpenShell` (Quader ohne Decke) — Vermerk „offen“, nicht geschlossen | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_advancedbrep.ifc` | Raumkörper als `IfcAdvancedBrep` — benannt nicht lesbar (`IMP_IFC_PROT_KOERPER_ART`), der Raum bleibt ohne Körper | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_platzierung.ifc` | Raumkörper hinter einer Placement-Kette: Gebäude um 90° gedreht und versetzt, Geschoss 3 m höher, Raum versetzt; Längen in mm | selbst erzeugt | eigenes Werk |
| `ifc4_g5_wand_extrusion.ifc` | Bauteilkörper als Rechengröße (G5-1): Raum 10 × 8 m und vier Außenwände als `IfcExtrudedAreaSolid` ohne Mengensätze und Raumgrenzen, eine gegliederte Wand mit L-Grundriss (`IfcArbitraryClosedProfileDef`); Gebäude um 90° gedreht und versetzt, TrueNorth gesetzt, Längen in mm | selbst erzeugt | eigenes Werk |
| `ifc4_g5_wand_brep_mapped.ifc` | Bauteilkörper als Rechengröße (G5-1): Wände als `IfcFacetedBrep`, `IfcMappedItem` mit Streckung, `IfcShellBasedSurfaceModel` und `IfcTriangulatedFaceSet`, geneigtes Dach (`IfcSlab` ROOF in `IfcRoof`) als schräge Extrusion, Bodenplatte — ohne Mengensätze | selbst erzeugt | eigenes Werk |
| `ifc4_g5_mengen_gegenprobe.ifc` | Gegenprobe zu `ifc4_g5_wand_extrusion.ifc` mit Mengensätzen: eine Wand 5 % über dem Körper (Meldung), die übrigen innerhalb 2 % | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_nachbarn.ifc` | Trennflächen aus Raumkörpern (G7f-4): drei Räume als `IfcFacetedBrep` auf zwei Geschossen ohne Raumgrenzen, mit Raumbezügen — „Büro“ und „Flur“ (EG) durch eine Wand von 0,24 m (`Pset_WallCommon.ThermalTransmittance` 1,2) getrennt, „Büro 2“ (OG) 0,3 m über dem Büro; zwei Temperaturen für Z6 | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_nachbarn_grenzen.ifc` | Gegenprobe zu `ifc4_koerper_nachbarn.ifc`: dieselben Räume mit Raumgrenzen der 2. Ebene für Wand und Decke (Gegenstücke) — die Raumgrenzen gehen vor, die Körperpaare werden nur gezählt | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_grundriss_stufe.ifc` | Grundriss je Raum aus dem Dateikörper (HC-5): ein Raum als `IfcFacetedBrep` in L-Form (4 × 3 m mit Boden auf 0, 2 × 3 m mit Boden auf 0,3 m, Decke auf 3 m), Mengensatz 15 m² — Herleitung `KoerperBoden`, 18 m², Vermerke `Stufen` und `Flaeche` (+20 %) | selbst erzeugt | eigenes Werk |
| `ifc4_koerper_grundriss_ohne_boden.ifc` | Grundriss je Raum aus dem Dateikörper (HC-5): eine Schale 4 × 3 × 3 m ohne Boden als `IfcShellBasedSurfaceModel` mit `IfcOpenShell` — Herleitung `KoerperDecke`, 12 m² | selbst erzeugt | eigenes Werk |
| `ifc4_verlust.ifc` | von Hand geschriebene Kleinstdatei (< 5 KB), absichtlich beschädigt: ein unbekannter Entitätstyp und ein Verweis ins Leere — beide Verlustkanäle | von Hand geschrieben | eigenes Werk |

## HottCAD-Projektdatei (Stufe SQ-1, Proben 33–36, Zonierungswahl HC-3)

Keine Datei in diesem Ordner: Eine Projektdatei (`.sqproj`) ist SQLite und gehört nach der `*.sqlite`-Regel nie ins
Repositorium. Die Proben entstehen **zur Laufzeit** im Test durch `EPOS.Kern.Tests/SqprojProbenErzeuger.cs` unter einem
temporären Pfad — deterministisch (feste Kennungen, feste Reihenfolge), nur die Tabellen und Spalten, die der Leser liest,
neutrale Raum- und Profilnamen, runde Werte: `Standard()` (Regelfall: Typ 5 und Typ 6 decken je alle Räume, Kennung,
Name je Geschoss, Raum ohne Gegenstück, Tagesarten 4, 5, 6 und ein unbekannter Code, Betriebsart 2 in der Nacht,
Abschnitte mit Wochentagsschaltern und über den Jahreswechsel, übersprungene Klassen und Zonentypen) und
`Unvollstaendig()` (eine Simulationszone mit einem Teil der Räume). Probe 36 (`SqprojZonenTests`) hält die Zonierungswahl: beide Zonierungen mit der Vorgabe DIN-V-18599-Zonen und der Wahl Simulationszonen, nur eine Zonierung ohne Wahl und den Protokollsatz, der beide Zonierungen nennt. Probe 33 (`SqprojLeserTests`) hält die eigene Größengrenze der Projektdatei: 250 MB Windows, 100 MB iOS (`SqprojProfil.MAX_BYTES`, `MAX_BYTES_IOS`). Anwenderdateien liegen nur lokal unter
`Quellen/*.sqproj` (`.gitignore`) und laufen allein in `SqprojQuelldateienDiagnoseTests`.

**Zuordnung Profilnummer → Nutzungsprofil** (Konzept Nutzungsprofile 5.4, NP-F12): Die Nummer nach DIN/TS 18599-10:2025-10
(70 und 71 die Wohnzeilen der Projektdatei) führt über die Zuordnung `DIN_NUMMER` in
`Tab_Raumnutzungszuordnung` auf ein Profil des Katalogs; fehlt die Zeile oder liegt kein Katalog vor, gilt die Vorgabe im
Code (`EPOS.Kern/Allgemein/Import/Sqproj/Din18599Nutzung.cs`), deren Kennung auf das EPOS-Muster gleicher Nutzung führt
(`Raumnutzungsvorbelegung`). Jede andere Nummer ergibt keine Nutzung und steht im Beleg. Ausgeliefert:

| Nr. | Normname | Profil (EPOS-Muster) | Vorgabe im Code |
|---|---|---|---|
| 1 | Einzelbüro | Büro | BUERO |
| 2 | Gruppenbüro | Büro | BUERO |
| 3 | Großraumbüro | Büro | BUERO |
| 4 | Besprechung, Sitzung, Seminar | Büro | BUERO |
| 5 | Schalterhalle | Büro | BUERO |
| 8 | Klassenzimmer | Schule | SCHULE |
| 9 | Hörsaal, Auditorium | Schule | SCHULE |
| 12 | Kantine | Gastronomie | — |
| 13 | Restaurant | Gastronomie | — |
| 19 | Verkehrsflächen | Verkehr | — |
| 20 | Lager, Technik, Archiv | Lager | — |
| 30 | Bibliothek – Lesesaal | Schule | SCHULE |
| 31 | Bibliothek – Freihandbereich | Schule | SCHULE |
| 33 | Turnhalle (ohne Zuschauerbereich) | Sport | — |
| 37 | Fitnessraum | Sport | — |
| 43 | Lagerhallen, Logistikhallen | Lager | — |
| 70 | Wohnen (Einfamilienhaus) | Wohnen | WOHNEN |
| 71 | Wohnen (Mehrfamilienhaus) | Wohnen | WOHNEN |

**Zählung.** Die Schlüssel sind Nummern der Projektdatei. HottCAD zählt nach der DIN/TS 18599-10:2025-10 wie die
Kategorie DIN des Katalogs (Anwender, 06.10.2026, E96; in HottCAD hat „Turnhalle“ die Nummer 33): Die Tabelle nennt ab 22
Nummer und Namen der Ausgabe 2025 (in der DIN V 18599-10:2018-09 trugen dieselben Nutzungen 28, 29, 31, 35 und 41), 1 bis 21
zählen in beiden Ausgaben gleich. 44 bis 47 sind in keiner Ausgabe eine Nummer der Norm und bleiben ohne Zuordnung; 70 und
71 sind die Wohnzeilen der Projektdatei. Die Proben dieses Ordners entstehen synthetisch mit den Nummern 1, 20 und 71 und
runden Werten (Konzept Nutzungsprofile 5.4). 19 und 20 sind in beiden Ausgaben gleich (E96): Der Keller des Zonenhauses (Nummer 20) bekommt mit Katalog das Muster Lager samt Kalender aus dem
Profil (`SqprojDatenbankTests`); ohne Katalog bleibt er ohne Nutzung und mit Beleg (`SqprojZonenTests`), weil die Muster
Lager und Verkehr keine alte Kennung haben.

Unter der Zonenregel Z6 belegt der Zonenplan eine beheizte Zone mit dem Profil ihrer Nutzungsklasse vor (Zuordnung
`IFC_KLASSE`, Vorgabe `Zonenplan.NutzungAusKlasse`): Büro → Büro; Wohnen, Schlafen, Küche → Wohnen; Sport, Gastronomie,
Lager, Verkehr und Technik → das gleichnamige EPOS-Muster (ohne Katalog: keine); Sanitär und Sonstige → keine. Eine Zeile
`HOTTCAD_RAUMTYP` (ausgeliefert keine) geht der Nutzungsklasse des Raumtyps vor. `EPOS_Zone.Nutzung` einer IFC-Datei von
EPOS-Plan wird als Profilname gelesen, eine alte Kennung führt auf das EPOS-Muster gleicher Nutzung, jeder andere Text
bleibt mit dem Befund „nicht im Katalog“ (`ZonenplanProfilTests`, `RaumnutzungVorbelegungTests`, `ZonenimportZuordnungTests`).

## Katalog-, Geräte-, Ganglinien- und Klimaimporte

| Datei | Zweck | Herkunft | Lizenzstand |
|---|---|---|---|
| `cec_module_50.csv` | 50 PV-Module mit Kopf-, Einheiten- und `[0]`-Zeile (Modulimport) | Ausschnitt aus `VDI-3805-Daten/PV/CEC Modules_UTC.csv` (iU9-W13) | CEC-Verwaltungsangabe, NREL-SAM-Fassung unter BSD-3-Clause (`VDI-3805-Daten/PV/LIESMICH_CEC_Inverters.md`) |
| `cec_module_gegenprobe.csv` | Kommentarzeile, Leerzeile, Komma im Anführungsfeld | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `cec_wechselrichter_21.csv` | 21 Wechselrichter der CEC-Liste (Wechselrichterimport, Konzept Wechselrichter S1.5) | Ausschnitt aus `VDI-3805-Daten/PV/CEC Inverters.csv` | wie `cec_module_50.csv` |
| `Katalogpaket_Probe.json` | Ausschnitt des Katalogpakets der Testdatenbank (Formatversion 2, Fassung 1): Stufe 1 die acht ausgelieferten Prozesswärme-Betriebsweisen samt Wochenprofilen, Stufe 2 die 14 Konditionierungsvorlagen samt Vorgaben, Kalendern und Perioden (Enkelzeilen) und der Muster-Wechselrichter — Probe des Katalogabgleichs (KU1 Stufe 1 und 2) und Messlatte des Pakets im Werkzeug Auslieferungsvorlage | erzeugt von `Werkzeuge/Auslieferungsvorlage` aus der gehobenen Testdatenbank; die Sätze stammen aus den Saaten `ProzesstypSaat` und `KonditionierungsvorlagenSaat` und dem Mustersatz der Testdatenbank (neutrale Namen, runde Werte) | eigenes Werk |
| `dwd_try_synthetisch_72h.dat` | 72 Stunden im DWD-TRY-Format, ausdrücklich keine amtlichen Daten (Konzept Klimadatenquellen) | selbst erzeugt, synthetisch | eigenes Werk |
| `ganglinie_mit_kopfzeile.txt` | Ganglinie mit Kopfzeile für den Kopfzeilenschalter (iU9-W14b) | Projektbestand (iU9-W13) | nicht belegt |
| `heizkessel_buderus.vdi` | drei Sätze mit Emissionswerten und Öl-Brennstoffindex (VDI 3805 Blatt 3) | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand unter `VDI-3805-Daten/`, nicht belegt |
| `heizkessel_sonderfaelle.vdi` | vier Sätze für Satz 710.01 (Konzept Kesselkennlinie 3.4): ohne Teillastwert, kleinste und größte Leistung vertauscht, Niedertemperaturkessel, Elektrokessel ohne 710.01-Wirkungsgrad (Rückfall auf Satz 700 Spalte 26) | byteechter Ausschnitt (Kopf, Sätze 700, 710.xx, 720) aus dem Herstellerkatalog unter `VDI-3805-Daten/` | wie der Katalogbestand, nicht belegt |
| `heizkessel_vaillant.vdi` | fünf Sätze, Nennlastwert aus 710.01 Spalte 6, Satz 700 Spalte 26 als Rückfall | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `ond_muster_10000tl_3profile.ond` | Wechselrichter mit drei ProfilPIO-Fassungen | selbst erzeugt, synthetisch (Konzept Wechselrichter, Anhang A) | eigenes Werk |
| `ond_muster_2500tl.ond` | Wechselrichter mit den Zahlen des Anhangs A | selbst erzeugt, synthetisch (Konzept Wechselrichter, Anhang A) | eigenes Werk |
| `pan_jinko_jkm260p.pan` | Moduldatei im PAN-Format | Kopie der PAN-Datei des Bestands unter `VDI-3805-Daten/PV/` (iU9-W13) | Herstellerdatei, nicht belegt |
| `pan_lg_320n1k.pan` | Moduldatei im PAN-Format | Kopie der PAN-Datei des Bestands unter `VDI-3805-Daten/PV/` (iU9-W13) | Herstellerdatei, nicht belegt |
| `pan_panasonic_vbhn325.pan` | Moduldatei im PAN-Format | Kopie der PAN-Datei des Bestands unter `VDI-3805-Daten/PV/` (iU9-W13) | Herstellerdatei, nicht belegt |
| `pan_trina_tsm650.pan` | Moduldatei im PAN-Format, bifazial ohne `Bifacial`-Schlüssel | Kopie der PAN-Datei des Bestands unter `VDI-3805-Daten/PV/` (iU9-W13) | Herstellerdatei, nicht belegt |
| `pufferspeicher_vaillant.vdi` | Trinkwasserabschnitt und zehn Blöcke, neun Sätze (VDI 3805) | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `pufferspeicher_weishaupt.vdi` | Solarspeicher neben Pufferspeicher | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `pvgis_tmy_stuttgart_72h.json` | 72 Stunden in PVGIS-Form, deterministisch gerechnet — kein mitgeschnittener PVGIS-Lauf (iU9-W14c) | selbst erzeugt, synthetisch | eigenes Werk |
| `solarganglinie_8760.txt` | Kopfzeile und 8 760 Werte (Ganglinienimport, iU9-W14b) | Projektbestand | nicht belegt |
| `solarkollektoren_gegenprobe.vdi` | alle vier Bauarten und der Bezugsflächen-Rückfall | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `solarkollektoren_vaillant.vdi` | Flach-, Röhrenkollektor und leere Bruttofläche | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `stromspeicher_bslib_7.csv` | die vollständige `bslib_database.csv`, unverändert (Konzept Stromspeicherimport) | bslib, HTW Berlin / FZ Jülich, DOI 10.5281/zenodo.6514527 | CC BY 4.0 (Datenbank) |
| `stromspeicher_cec_ess_23.csv` | echte Zeilen der CEC Energy Storage System List, unverändert: Titel-, Stand- und Kopfzeilen, 23 Geräte (Konzept Stromspeicherimport) | California Energy Commission | Public Records Act, Namensnennung, kommerzielle Nutzung eingeschränkt (Konzept Stromspeicherimport 1.4) |
| `waermebedarf_8760.txt` | 8 760 Stundenwerte, ein Wert je Zeile (Ganglinienimport, iU9-W13) | Projektbestand | nicht belegt |
| `waermebedarf_gegenprobe_komma.txt` | Dezimalkomma | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `waermebedarf_gegenprobe_leerzeile.txt` | Leerzeile in der Reihe | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `waermebedarf_gegenprobe_semikolon.txt` | Semikolon als Trenner | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `waermepumpen_gegenprobe_aufstellung.vdi` | Aufstellungsindex 7 außerhalb der Tabelle | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `waermepumpen_hoval.vdi` | drei Sätze, Voll- und Teillast getrennt (VDI 3805 Blatt 22) | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `waermepumpen_hoval_ohne_abschluss.vdi` | drei Blöcke ohne Abschluss, ein Satz | gekürzter Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
