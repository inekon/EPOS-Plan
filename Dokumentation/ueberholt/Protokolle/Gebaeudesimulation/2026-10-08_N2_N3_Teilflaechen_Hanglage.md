# Protokoll N2/N3 — Teilflächen, Öffnungen über zwei Räume, Hanglage (08.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#820**. Commits: `ebcc1a94b`, `79171c15a`, `7f9955fcf`, `af4a61c7d`; Merge in die Integration `8212a008a`, Merge origin `618496530`.
**Entscheid:** Anwenderwunsch, den IFC-Körperweg für Gebäude ohne Raumgrenzen zu vervollständigen.

## 1 Anlass

Bauteilflächen je Raum aus Körpern ergaben eine Zeile je Bauteil, auch bei gegliederten Wänden und mehreren Dachflächen; Öffnungen über zwei Räume und Wände am Hang wurden nicht getrennt. Kein Schemaschritt, Basis unverändert.

## 2 Gebaut

- `Teilflaechen.cs` (Toleranz 5°): je Richtung eine Zeile im Zonen- und Einzonenweg, gilt auch für IFC mit Raumgrenzen.
- Öffnungen über zwei Räume nach Überdeckung geteilt; Anteile unter 2 % entfallen.
- Hanglage: Geländehöhe aus `IfcBuilding.ElevationOfTerrain` (sonst z = 0, Info `IMP_IFC_PROT_GELAENDE_DATEI`/`…_NULL`); Wandstücke am Gelände in Erdreich und Außenluft geteilt; Kellerfenster über Gelände an der Außenluft; `GeschossLageM` am Raum belegt. Die Erdreichrechnung bekommt die Tiefe über Fläche durch Umfang, ein neues Feld gibt es nicht.
- Raumgrenzen der Datei gehen vor.

## 3 Prüfung

Tests `TeilflaechenTests` (5), `HanglageTests` (8). Keine Erwartung geändert; Heizwärme der Kleinhausprobe unverändert 10,09 MWh/a.

## 4 Gate 819

GATEZAHLEN

## 5 Offen

- Hanglage im Einzonenweg nicht geteilt.
- Sichtabnahme unter Windows: Satteldach und Hanghaus in Ansicht und Zuordnungsdialog.
- Logbuch-Eintrag (Version beim Anwender erfragen) und Wiki-Upload gebündelt.
