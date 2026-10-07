# Abstimmung G5 — Auftrag an die Sitzung IFC / Gebäudeimport, Anforderungen der Sitzung Gebäudesimulation

Stufe **G5** (Geometrieableitung aus IFC-Körpern) ist nach **E101** (Anwender, 07.10.2026) nicht mehr „nur bei Bedarf“. Der
Geometrieteil gehört in den Auftrag der **Sitzung IFC / Gebäudeimport**: Sie baut G5, weil die Dateien des Imports, der
Körper-Leser und die Bauteilstufen bei ihr liegen. Die **Sitzung Gebäudesimulation** baut an G5 nichts; sie nennt hier, was der
Rechenweg von G5 braucht, und nimmt das Ergebnis am Rechenweg ab. Damit überschneiden sich die Wellen beider Sitzungen nicht. Die
Sitzung IFC trägt ihre Antwort in die Spalte „IFC-Sitzung“ ein und pusht sie.

## 1 Was G5 umfasst

Gebaut sind bereits der Raumkörper-Leser ohne Geometriekern (G7f, #727: Extrusion, Tessellation, BRep, `IfcMappedItem`,
Placement-Kette), die Bauteilkörper als Anzeige (HC-1) und der gespeicherte Grundriss je Raum aus dem Dateikörper (HC-5, #784).
Gerechnet wird mit Körpern heute nur bei den Trennflächen zwischen Räumen einer Datei ohne Raumgrenzen. Flächen, Dicke und
Schichten der Bauteile kommen aus Mengensätzen, Raumgrenzen und Katalog ([ADR-003](../ADR-003_IFC_xBIM_ohne_Geometriekernel.md),
[Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 15.1).

G5 ergänzt drei Teile:

| Teil | Inhalt |
|---|---|
| G5-1 | **Bauteilkörper als Rechengröße:** Fläche und Orientierung von `IfcWall`, `IfcSlab`, `IfcRoof` (und `IfcCovering`, soweit Hülle) aus Extrusion, BRep und `IfcMappedItem` samt Placement-Kette — als Quelle, wenn die Datei für das Bauteil keine Mengensätze oder Raumgrenzen liefert |
| G5-2 | **Abzug der Öffnungen:** Fläche der Fenster und Türen aus `IfcOpeningElement`, `IfcWindow`, `IfcDoor` und ihr Abzug von der Wand- bzw. Dachfläche, wo die Mengensätze die Nettofläche nicht nennen |
| G5-3 | **Rückfall für Dateien ohne Mengensätze und Raumgrenzen:** Bauteilflächen je Raum und Zone aus den Körpern statt des Rückfalls „schematisch“; Herkunft je Fläche sichtbar (Mengensatz / Raumgrenze / Körper / schematisch) |

Nicht Teil von G5: Reparatur offener oder überlappender Netze, Dachschrägen im Export, Anlagentechnik, der gbXML-Weg
(`Space/ShellGeometry`, eigener Zuruf).

## 2 Anforderungen des Rechenwegs (Sitzung Gebäudesimulation)

Der Rechenweg nach VDI 6007 (Bauteilweg, Zonenmodell, Erdreich nach DIN EN ISO 13370) liest je Zone Bauteile aus `Tab_Bauteil`.
Was G5 dort ablegt, muss diese Größen tragen:

| Nr. | Größe | Anforderung |
|---|---|---|
| A1 | Fläche | Nettofläche des Bauteils je Zone in m² (opake Fläche ohne Öffnungen); Öffnungen als eigene Bauteile (Fenster/Tür) mit ihrer Fläche, wie es der Bauteilweg heute aus Mengensätzen erhält |
| A2 | Orientierung | Azimut (0° = Nord, im Uhrzeigersinn, nach `TrueNorth` bzw. `IfcMapConversion`) und Neigung (0° = waagrecht nach oben, 90° = senkrecht) je Bauteilfläche; bei gegliederten Wänden je ebener Teilfläche oder flächengewichtet zusammengefasst, benannt |
| A3 | Randbedingung | Außenluft, Erdreich, Nachbarzone (mit Trennflächenzuordnung), unbeheizt — wie der heutige Importweg (Raumgrenzen, R0–R7) |
| A4 | Herkunft | je Fläche sichtbar und gespeichert, damit Prüfregeln und Bericht sie nennen können |
| A5 | Abweichung | wo Mengensätze vorliegen, bleiben sie Quelle; der Körperwert wird nur verglichen und bei Abweichung über einer Schwelle gemeldet (Vorschlag: 2 %, wie die Gegenprobe der Trennflächen) |
| A6 | Abnahme am Rechenweg | Referenzlauf aller 21 Projekte gegen die geltende Basis byte-gleich (G5 berührt keine Referenzprojekte); an den Importproben mit Mengensätzen Körperfläche gegen Mengensatz innerhalb A5; an einer Probe ohne Mengensätze Heizwärme gegenüber dem Rückfall „schematisch“ benannt |

## 3 Dateien und Zuständigkeit

| Bereich | Dateien (Stand 07.10.2026) | Gebäudesimulation | IFC-Sitzung |
|---|---|---|---|
| Körper-Leser und IFC-Abbild | `EPOS.Kern/Allgemein/Import/Ifc/` (`IfcRaumkoerper.cs`, `IfcRaumgrundriss.cs`, `IfcGrenzgeometrie.cs`, `IfcPlatzierung.cs`, `IfcAbbildBauer.cs`, `IfcGebaeudeAbbild.cs`), `EPOS.Kern/Allgemein/Simulation/Gebaeude/Dateikoerper.cs` | keine Änderung | — |
| Importablauf, Bauteilvorschlag, Aufbauten | `EPOS.Kern/Allgemein/Import/Gebaeude/` (`GebaeudeImportAblauf.cs`, `GebaeudeBauteilvorschlag.cs`, `Bauteilzuordnung.cs`, `Ersatzaufbau.cs`, `GebaeudeZuordnungsModell.cs`, `GebaeudeNeulesen.cs`), `EPOS.Kern/Allgemein/Import/Sqproj/` | keine Änderung | — |
| Rechenweg | `EPOS.Kern/Allgemein/Simulation/Gebaeude/` außer `Dateikoerper.cs` (Bauteilweg, Zonenmodell, Erdreich) | ändert nur bei Bedarf aus A1–A4, nach Absprache | — |
| Export | `EPOS.Kern/Allgemein/Export/Ifc/*`, Exportmodell | keine Änderung | — |
| Oberfläche | Gebäudeansicht, Zuordnungsdialog, Bauteilsteckbrief | keine Änderung | — |
| Schema | Schritte nach Anmeldung in der Kopfzeile der Statusdatei | — | — |
| Testdatenbank, Importproben | `Referenzlaeufe/Kenndaten_Test.sqlite`, `Referenzlaeufe/Importproben/` | Referenzprojekte bleiben unberührt | — |

## 4 Fragen an die Sitzung IFC

| Nr. | Frage | IFC-Sitzung |
|---|---|---|
| F1 | Übernehmt ihr G5 in euren Auftrag, und wann (nach welchen laufenden Wellen — offen sind laut Statusdatei u. a. Konzept 5.3 des HottCAD-Verbunds, Codes für Putz, Folie und Luftschicht, Richtung bei Wänden gegen Erdreich)? | — |
| F2 | Wellenzuschnitt und Aufwand für G5-1 bis G5-3; braucht G5 einen Schemaschritt (Herkunft je Fläche, A4)? | — |
| F3 | Sind die Anforderungen A1–A6 erfüllbar, oder braucht es Änderungen am Rechenweg (dann Absprache nach Abschnitt 3)? | — |
| F4 | Welche Importproben ohne Mengensätze oder Raumgrenzen gibt es oder werden beschafft? | — |

## 5 Regeln

- Die Sitzung Gebäudesimulation ändert nichts unter `EPOS.Kern/Allgemein/Import/Ifc/` und `EPOS.Kern/Allgemein/Import/Gebaeude/`.
- Braucht G5 eine Änderung am Rechenweg, meldet die Sitzung IFC sie hier an; die Sitzung Gebäudesimulation baut sie oder gibt sie frei.
- Beide Sitzungen melden Schemaschritte in der Kopfzeile der Statusdatei an und prüfen Status- und Entscheidnummern vor jedem Push
  gegen origin.
