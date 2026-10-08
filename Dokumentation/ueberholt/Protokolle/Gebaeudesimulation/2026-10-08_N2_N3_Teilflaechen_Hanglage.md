# Protokoll N2/N3 — Teilflächen, Öffnungen über zwei Räume, Hanglage (08.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#822**. Commits: `ebcc1a94b`, `79171c15a`, `7f9955fcf`, `af4a61c7d`; Merge in die Integration `8212a008a`, Merge origin `618496530`.
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

## 4 Gate 821

auf `e3ad59f20` (64 min): KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 830, EPOS.Kern 12 272 mit 6 übersprungen, 0 rot; Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 24/24 gegen `2026-10-07_R43_Kaelteseite_AK3K` PASS (8 073 156 Werte), 759/759 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 522 Texte, 0 Fundstellen; Windows-Schale 0 Fehler; Auslieferungsvorlage-Tests 61/61.

## 5 Offen

- Hanglage im Einzonenweg nicht geteilt.
- Sichtabnahme unter Windows: Satteldach und Hanghaus in Ansicht und Zuordnungsdialog.
- Logbuch-Eintrag (Version beim Anwender erfragen) und Wiki-Upload gebündelt.
