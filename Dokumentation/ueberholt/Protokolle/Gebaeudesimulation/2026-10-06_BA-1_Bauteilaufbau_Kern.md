# Protokoll BA-1 — Bauteilaufbau beim Import, Kern (06.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#780**. Ein Opus-Agent im Worktree: `115f1cb8`. Kein Schemaschritt, kein Eingriff in `Bauteilreduktion` und `ErsatzparameterRC`, Referenzlauf byte-gleich.
**Entscheid:** E95; Konzept [Bauteilaufbau beim Gebäudeimport](../../../aktuell/Gebaeudesimulation/2026-10-06_Konzept_Bauteilaufbau_Import.md) 5 und Welle BA-1.

## 1 Auftrag

Das U der Datei bleibt neben dem Aufbau stehen, unerhebliche Schichten werden nach einer festen Regel benannt weggelassen, und jedes Bauteil erhält eine Zuordnungsstufe.

## 2 Gebaut

- **U der Datei neben dem Aufbau:** das Modell nimmt `uWirksam` (das U der Zeile in Gl. (27)), R₁ und C₁ kommen aus den Schichten (`ErsatzparameterRC.AusBauteilweg`). Der Import hatte das U der Datei bei vollständigem Aufbau verworfen. Hinweis ab 10 % Abweichung zwischen U der Schichten und U der Datei.
- **Relevanzregel** `Schichtrelevanz`: Schichten bis 5 mm und je unter 2 % an R und C weg; Sperren unter 2 % an R weg. Benannt in `GebaeudeAufbauzeile.Weggelassen` und im Protokoll `IMP_BAUTEIL_PROT_SCHICHT_UNERHEBLICH`.
- **Zuordnungsstufe** `Bauteilzuordnung` (A, B, C, transparent, `Aufbauluecke`); Summen je Stufe im Importprotokoll.

## 3 Befund an den Anwenderdateien

- Keine Stufe C. Fläche in Stufe B: MFH 1964 100 %, MFH 1984 69 %, Sportheim 77 %, Verwaltung 92 %, WG-EH55 94 %, Produktion 99 %.
- Wirkung des U der Datei auf drei Dateien: MFH 1984 45,17 → 46,27 MWh/a, Sportheim 59,29 → 59,15 MWh/a, Verwaltung 236,04 → 238,31 MWh/a. Die Sollwerte in `IfcQuelldateienDurchgangTests` sind nachgezogen.

## 4 Abweichung vom Konzept

Die Protokollmeldung zur U-Abweichung gilt ab 10 % statt ab 5 % (Konzept 5.2).

## 5 Gate 779

Gemeinsam mit HC-5: ⟨GATE779⟩

## 6 Offen

- **BA-2** (Typaufbauten, Schemaschritt 192, Ersatzaufbau) in Arbeit: schließt die masselosen Stufe-B-Außenbauteile neben Stufe A (69 bis 99 % der Fläche in fünf Dateien).
- **BA-3** Oberfläche; **BA-4a** Diagnose der Projektdateien beim Anwender (lokal, `Quellen/*.sqproj`).
- Kein Logbuch-Satz für BA-1 allein: außer im Importprotokoll nicht sichtbar.
