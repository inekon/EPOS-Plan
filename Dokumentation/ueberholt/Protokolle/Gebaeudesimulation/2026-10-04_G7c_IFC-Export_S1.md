# Protokoll G7c — IFC-Export S1 semantisch (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle G7c (E67, E69), Teilzweige als Worktrees, Merges `bd5ab2d`, `9c2e423`, `8e683c9`, `3db43cc`. Statuszeile #709.
**Entscheid:** keiner neu. Kein Schemaschritt, Basis unverändert (Referenzlauf nicht betroffen).

## 1 Auftrag

IFC-Export Stufe S1 (semantisch, ohne Geometrie) mit Formatwahl im Exportdialog, Ergebnissen des letzten Laufs, IDS-Exportzusage und Beipackzettel.

## 2 Vorgehen

Vier Opus-Agenten nacheinander (75, 117, 63 und 49 Aufrufe: Schreiber, Abbild und Ergebnisse, Hülle und Auslieferung, Oberfläche und Papiere); vier konfliktfreie Merges.

## 3 Ergebnis je Teil

- **Schreiber** (`3050b6a`, `587a9a9`, `ba7e8ab`): `IfcSchreiber`, `IfcExportKennung` (UUID v5), `IfcErgebnisse` unter `EPOS.Kern/Allgemein/Export/Ifc/`; Räume, Zonen, Bauteile, Öffnungen, Raumgrenzen 2. Ebene ohne Geometrie, Schichtaufbau, Psets; Validator vor dem Schreiben.
- **Abbild und Ergebnisse** (`9f3e1f4`, `24e64e3`, `06c5abc`): `GebaeudeExportSatz.Lesen` liest den letzten Lauf, Umrechnung MWh/kW → kWh/W an einer Stelle, `EPOS_Ergebnis` am Gebäude und je beheiztem Raum.
- **Formatwahl, Hülle, IDS** (`48eeecc`, `62b8b4d`, `d3d9024`, `e874634`): `GebaeudeExportProfil.Format`, Vorschaumeldungen je Format, IDS `Setup/Vorlage/EPOS_Export.ids` mit Wache, Auslieferung (Setup, iOS, Windows-Entwicklungsbau).
- **Oberfläche und Papiere** (`66c9b9b`, `52ddca5`, `16d7935`): Exportdialog mit gbXML/IFC, Knopf für die Exportzusage, iOS-Teilen `.ifc`, Texte angepasst.

## 4 Entscheide der Orchestrierung

- **Zeitstempel an zwei Pflichtplätzen:** Die Datei ist deterministisch (GlobalId aus UUID v5), nur der Zeitstempel an den zwei von IFC verlangten Plätzen unterscheidet Läufe.
- **Öffnungselement ohne Geometrie:** Öffnungen gehen über ein geometrieloses `IfcOpeningElement`; Raumgrenzen sind logisch.
- **Beipackzettel als Meldungen:** Die Grenzen (logische Raumgrenzen, ohne MVD, IDS, Kälte sensibel ohne Entfeuchtung) erscheinen als Bilanzmeldungen im Dialog, nicht als Begleitdatei.

## 5 Abweichungen

Nicht abgebildet, weil die Quelle fehlt: Kältelast-Spitze, `AnzahlTeilflaechen` im Klassenweg, `RaumtemperaturMax`; das Rechenjahr ist nicht gespeichert, `startTime` nimmt den 1. Januar des Rechenjahrs.

## 6 Abnahme

Tests: `IfcSchreiberTests` (21), `GebaeudeExportErgebnisTests` (15), `GebaeudeExportReferenzprojektTests` (2, Projekt 1052), `IdsWacheTests` (4), `GebaeudeExportFormatHuelleTests` (10), bunit Exportdialog (7); Exportfilter 466/467 (1 übersprungen, Bestand), UI 7 436/7 436. Gate: siehe Statuszeile #709; CI: Kern-Lauf nach dem Push nachzutragen.

## 7 Offen

- Beim Anwender: Proben 15 (bSI-Validierungsdienst) und 16 (Revit, Archicad, HiCAD) mit `Referenzprojekt_1052.ifc` und `.xml`, Sichtabnahme des Exportdialogs, Logbuch-Version.
- Nächste Wellen: G7d (Round-Trip-Anreicherung), G7e, KU3.

## 8 Aufwand

Opus 75 + 117 + 63 + 49 Aufrufe.
