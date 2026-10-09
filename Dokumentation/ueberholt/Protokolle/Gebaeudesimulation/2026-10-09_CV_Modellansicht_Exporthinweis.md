# Protokoll CV — Modellansicht (MVD) beim IFC-Import, Exporthinweis, Prüfplan IFC2x3 gegen IFC4 (09.10.2026)

**Sitzung:** „IFC / Gebäudeimport“, Statuszeile **#850**, Zweig `cv-mvd-hinweis`. Kein Schemaschritt, Basis unverändert.

## 1 Auftrag und Entscheide

Anwenderauftrag 09.10.2026: Prüfplan, Exporthinweis (a) und MVD-Erkennung (b).

- **(d)** Halbraumschnitt für Clipping-Körper zurückgestellt, bis eine Datei mit Clipping vorliegt.
- **(c)** Der Export bleibt unverändert.
- Die IFC2x3-Datei des Prüfplans bleibt unversioniert.
- Das CAD-Programm des Anwenders bietet keine Exportoptionen für Basismengen und Raumgrenzen 2. Ebene; für seine Dateien bleibt der Körperweg der Normalfall. Der Hinweis gilt allgemein.

## 2 Analyse

Sechs versionierte Anwenderdateien (Datei A–F), anonym, nur lesend untersucht:

| Datei | Schema | MVD-Angabe | Raumgrenzen | Basismengen | Geometrie |
|---|---|---|---|---|---|
| A–C | IFC2X3 | keine | keine | keine (nur eigene Mengensätze als Rückfall) | Brep, Flächenmodell; kein Clipping |
| D–F | IFC4 | keine | keine | keine (nur eigene Mengensätze als Rückfall) | Brep, Flächenmodell; kein Clipping |

Keine Nordrichtung in den Dateien; alle laufen über den Körperweg. In EPOS liest IFC2x3 keine Stoffwerte (xBIM), die Anreicherung lehnt IFC2x3 ab, es gibt keine Gegenstückpaarung der Raumgrenzen, und Clipping-Körper werden nur mit dem ersten Operanden gelesen („OhneBeschnitt“).

## 3 Prüfplan und Ergebnis

Exporte desselben Projekts als IFC4 und als IFC2x3 „Coordination View 2.0“ aus demselben CAD-Programm.

- Beide ohne MVD-Angabe im Kopf; Struktur identisch in Geometrie, Mengensätzen, Schichten, U-Werten, Öffnungen und Räumen.
- Im Diagnose-Import unterscheiden sich 5 von 92 Zeilen, nur bei den Stoffwerten: bei IFC2x3 `STOFFWERTE_NICHT_GELESEN`, 149 statt 128 Ersatzaufbauten, keine U-Wert-Plausibilisierung gegen die Schichten.

## 4 Umsetzung

- **Kern:** `EPOS.Kern/Allgemein/Import/Ifc/IfcModellansicht.cs` liest `ViewDefinition [...]` aus `FILE_DESCRIPTION` und legt sie unter `IfcGebaeudeAbbild.Modellansichten` ab. Erste Info-Zeile des Protokolls: `IMP_IFC_PROT_DATEI_MVD` bzw. `IMP_IFC_PROT_DATEI_OHNE_MVD`. Fehlen Raumgrenzen 2. Ebene und Basismengen, folgt `IMP_IFC_PROT_EXPORT_OHNE_GRENZEN_MENGEN`; bei IFC2x3 nennt `_IFC2X3` die nicht gelesenen Stoffwerte, `_IFC2X3_RUECKGABE` die Rückgabe, die IFC4 braucht.
- **Oberfläche:** Im Dialogkopf unter „Schema“ die Zeile „Modellansicht (MVD)“; an der Dateiwahl für IFC der leise Hinweis `GIMP_DLG_IFC_EXPORTHINWEIS`, sichtbar ohne Quellenwahl sowie bei „IFC“ und „IFC + Projektdatei“, nicht bei gbXML und „nur Projektdatei“.
- **Papiere:** Wiki-Quelle Gebäudeimport (Abschnitt „Empfohlene Exporteinstellung“), Logbuchsatz unter 1.2.0.6, Datenaustauschkonzept (Abschnitt 4).

## 5 Prüfung

`IfcModellansichtTests` (22) und `GebaeudeImportExporthinweisDialogTests` (2). Gefiltert grün: Kern 1225 (5 übersprungen), UI 393, Wachen Kern 77 und UI 173. Die Windows-Schale baut auf Linux mit 0 Fehlern. Gate: folgt.

## 6 Offen

- (d) Halbraumschnitt für Clipping-Körper bei Bedarf, sobald eine Datei mit Clipping vorliegt.
- Sichtabnahme unter Windows für Hinweis und Kopfzeile.
