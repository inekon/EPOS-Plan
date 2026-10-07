# Abstimmung G5 — Sitzung Gebäudesimulation und Sitzung IFC / Gebäudeimport

Stufe **G5** (Geometrieableitung aus IFC-Körpern) folgt nach **E101** (Anwender, 07.10.2026) auf AK3 und ist nicht mehr
„nur bei Bedarf“. Gebaut wird sie von der Sitzung Gebäudesimulation. Der Gegenstand liegt zum Teil in Dateien, die die Sitzung
IFC / Gebäudeimport fortlaufend bearbeitet (HC-*, BA-*, G6c). Der Anwender hat verlangt, G5 mit dieser Sitzung abzustimmen,
damit sich die Wellen nicht überschneiden. Dieses Papier ist der Ort der Abstimmung: Die Sitzung IFC trägt ihre Antwort in die
Spalte „IFC-Sitzung“ ein und pusht sie; erst danach entsteht der Entwurf G5.

## 1 Was G5 umfasst

Gebaut sind bereits der Raumkörper-Leser ohne Geometriekern (G7f, #727: Extrusion, Tessellation, BRep, `IfcMappedItem`,
Placement-Kette), die Bauteilkörper als Anzeige (HC-1) und der gespeicherte Grundriss je Raum aus dem Dateikörper (HC-5, #784).
Gerechnet wird mit Körpern heute nur bei den Trennflächen zwischen Räumen einer Datei ohne Raumgrenzen. Flächen, Dicke und
Schichten der Bauteile kommen aus Mengensätzen, Raumgrenzen und Katalog ([ADR-003](../ADR-003_IFC_xBIM_ohne_Geometriekernel.md),
[Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 15.1).

G5 ergänzt drei Teile:

| Teil | Inhalt |
|---|---|
| G5-1 | **Bauteilkörper als Rechengröße:** Fläche und Orientierung von `IfcWall`, `IfcSlab`, `IfcRoof` (und `IfcCovering`, soweit Hülle) aus Extrusion, BRep und `IfcMappedItem` samt Placement-Kette — als Quelle, wenn die Datei keine Mengensätze oder Raumgrenzen für das Bauteil liefert |
| G5-2 | **Abzug der Öffnungen:** Fläche der Fenster und Türen aus `IfcOpeningElement`, `IfcWindow`, `IfcDoor` und ihr Abzug von der Wand- bzw. Dachfläche, wo die Mengensätze die Nettofläche nicht nennen |
| G5-3 | **Rückfall für Dateien ohne Mengensätze und Raumgrenzen:** Bauteilflächen je Raum und Zone aus den Körpern statt des Rückfalls „schematisch“; Herkunft je Fläche sichtbar (Mengensatz / Raumgrenze / Körper / schematisch) |

Nicht Teil von G5: Reparatur offener oder überlappender Netze, Dachschrägen im Export, Anlagentechnik, der gbXML-Weg
(`Space/ShellGeometry`, eigener Zuruf).

## 2 Dateien und Zuständigkeit

| Bereich | Dateien (Stand 07.10.2026) | Gebäudesimulation (G5) | IFC-Sitzung |
|---|---|---|---|
| Körper-Leser | `EPOS.Kern/Allgemein/Import/Ifc/IfcRaumkoerper.cs`, `IfcRaumgrundriss.cs`, `EPOS.Kern/Allgemein/Simulation/Gebaeude/Dateikoerper.cs` | liest und nutzt; Erweiterung um Bauteil- und Öffnungskörper nur nach Absprache, sonst neue Klassen daneben | — |
| Importablauf und Bauteilvorschlag | `EPOS.Kern/Allgemein/Import/Ifc/IfcImportAblauf.cs`, Bauteilvorschlag und Schreibweg der Zonen (G6c), Bauteilstufen und Aufbauten (BA-1 bis BA-4) | eine benannte Naht für die Flächenquelle „Körper“; sonst keine Änderung | — |
| Export | `EPOS.Kern/Allgemein/Export/Ifc/*`, Exportmodell | nur lesend | — |
| Oberfläche | Gebäudeansicht, Zuordnungsdialog, Bauteilsteckbrief (BA-3) | Herkunft „Körper“ als Farbmodus bzw. Steckbriefzeile nur nach Absprache | — |
| Schema | Schritte nach Anmeldung in der Kopfzeile der Statusdatei | Anmeldung vor dem Bau | — |
| Testdatenbank, Importproben | `Referenzlaeufe/Kenndaten_Test.sqlite`, `Referenzlaeufe/Importproben/` | neue Proben ohne Mengensätze nur nach Absprache | — |

## 3 Fragen an die Sitzung IFC

| Nr. | Frage | IFC-Sitzung |
|---|---|---|
| F1 | Welche laufenden oder geplanten Wellen berühren dieselben Dateien oder denselben Gegenstand (offen sind laut Statusdatei u. a. Konzept 5.3 des HottCAD-Verbunds, Codes für Putz, Folie und Luftschicht, Richtung bei Wänden gegen Erdreich)? | — |
| F2 | Gibt es Teile von G5-1 bis G5-3, die die Sitzung IFC ohnehin plant oder lieber selbst baut? | — |
| F3 | Welche Dateien bleiben während G5 allein bei der Sitzung IFC? | — |
| F4 | Gibt es Importproben ohne Mengensätze oder Raumgrenzen, an denen G5 gemessen werden kann? | — |

## 4 Regeln bis zur Antwort

- G5 ändert nichts unter `EPOS.Kern/Allgemein/Import/Ifc/`, bevor die Spalte „IFC-Sitzung“ gefüllt ist; vor dem Abschluss von AK3
  beginnt ohnehin kein Bau.
- Leser und Entwurf G5 dürfen den Code lesen und an Kopien messen.
- Beide Sitzungen melden Schemaschritte wie üblich in der Kopfzeile der Statusdatei an und prüfen Status- und Entscheidnummern vor
  jedem Push gegen origin.
