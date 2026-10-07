# Protokoll Erdwärme B und C — Sondenfeld, Erdreichquellen, Schemaschritt 195, Basis R40 (07.10.2026)

**Sitzung:** Dialoge und Korrekturen, Zweig `erdwaerme-basis-r40` (Kopf `cdddd0f9`), Merge im Worktree `21400509`.
**Anwenderentscheide:** 06.10.2026 Betrachtungsjahr 10, zweiter Feldlauf, Regeneration jetzt; 07.10.2026 Vorschlag der Quellen übernommen, Zone 6, Energiegrenze bei der Bemessung, Spreizung 5 K.

## 1 Erdsondenfeld mit Entzugsrückwirkung

- `2fe7a210` Entwurf im Konzept Simulationsablauf, Abschnitt 23.
- `7cb25a63` Klasse `EPOS.Kern/Allgemein/Simulation/Erdsondenfeld.cs` nach Claesson–Javed (endliche Linienquelle, über das Feld gemittelt), stündlich mit Vorstundenkopplung; 68 ms je Jahr.
- `678ae7d2` Erdreichdialog und Wiki zum Sondenfeld.

## 2 Zweiter Feldlauf und Regeneration

- `68e65c59` Betrachtungsjahr 10; die neun Vorjahre tragen die stündliche Nettolast eines ersten Laufs.
- `aa7fb4ed` Kühlwärme von Wärmepumpen im Kühlbetrieb geht als Rückspeisung ins Feld; Kältemaschinen mit Trockenkühler speisen nicht zurück.
- `3359493e` Papiere; `89a2bc14` Wiki Kühlung (freie Kühlung und Regeneration über die Sonde).

## 3 Saat der Erdreichquellen

- `a6c1d2c4` Sonden bei den Sole-Wärmepumpen von 1008 (6 × 110 m), 1023, 1050 und 1019 (3 × 100 m), 1039 (16 × 105 m), 1017, 1047, 1055 und 1056 (5 × 120 m), 1027 (8 × 100 m); Mergel/Lehm, Zone 6. Neues Referenzprojekt 1057 als Kopie von 1029 (4 × 90 m). Skript `Referenzlaeufe/Skripte/erdreichquellen_referenzprojekte.py`.
- `9a01511d` Einfrierregel „gesäte Erdreichquellen“ in `CLAUDE.md`; `4400c451` Wache `ErdsondeReferenzprojektWacheTests`.

## 4 Datenbank und Schemaschritt 195

- `4827896d` origin-Datenbank (194) neu gesät.
- `33547bba` `ErdsondenfeldSchema` (195): sechs Spalten an `Tab_Energieanlagen` — `WQ_Sondenabstand`, `WQ_Bohrlochdurchmesser`, `WQ_Bohrlochwiderstand`, `WQ_Kopfueberdeckung`, `WQ_Betrachtungsjahr`, `WQ_Sondenanordnung`; NULL bedeutet Normvorgabe; neue Anordnung „Reihe“. Testdatenbank auf 195, alle 22 Projekte vorher und nachher byte-gleich.
- `9f88e729` Controller `ErdsondenfeldCtrl`; `41164eae` Felder im Erdreichdialog nur bei Sonde; `6195ea1f` Papiere.

## 5 Basis R40

`Referenzlaeufe/2026-10-07_R40_Erdreichquellen` (22 Projekte, 677 CSV; R39 entfernt), `ec8cad9e`, `2f454114`, `2f0c4b90`, `cdddd0f9`. Gegen R39 weichen 1008, 1017, 1023, 1039, 1047, 1050, 1055, 1056 ab; 1057 ist neu. Zweiter Lauf 677/677 byte-gleich, gestörter Lauf (ulp) PASS.

| Projekt | JAZ alt → neu | WP-Strom alt → neu (MWh) |
|---|---|---|
| 1008 | 4,15 → 3,94 | 18,86 → 20,27 |
| 1023, 1050 | 2,38 → 2,38 | 43,01 → 43,23 |
| 1039 | 3,25 → 3,06 | 58,59 → 58,82 |
| 1047 | 3,86 → 4,45 | 2,44 → 2,12 |
| 1056 | 3,73 → 4,33 | 3,25 → 2,80 |
| 1057 (neu) | 3,07 | 22,12 |

Projekt 1029: Jahr 10, Sole Mittel 1,34 °C, JAZ 3,06.

## 6 Merge und Gate

Merge mit origin: Konflikt nur in `Resource.resx` und `Resource.en-US.resx` (beide Seiten hängen am Dateiende an); nach Schlüsseln vereinigt (19 neue je Datei, `SIMQ_ERDREICH_HINWEIS_SONDE_KONSTANT` der Zweigfassung), `Resource.Designer.cs` neu erzeugt (15 915 Einträge). Testdatenbank ohne Konflikt, Index-Zeiger 133 Byte, Datei 89,7 MB.
Gate 796 auf `21400509`: Kern-Filter 0 Fehler; ChartProben 211 Hashes gleich Messlatte; Tests Kern 11 753 (3 übersprungen), UI 7 775, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen); Dokumentationswachen 35; SQL-Prüfer 2 482 Texte, 0 Fundstellen; Referenzlauf 22/22 gegen R40 GESAMT PASS (7 214 064 Werte), 677/677 CSV byte-gleich; gestörter Lauf PASS; Windows-Schale 0 Fehler.

## 7 Offen

- Der Erdreichblock der Ergebnisansicht zeigt bei Projekten mit zwei Wärmepumpen (1008, 1023, 1050) Jahresentzug 0.
- Der Taktstrom mindert den Entzug (fachlich offen).
- Sichtabnahme der Dialogfelder unter Windows.
- Wiki-Upload ausstehend: `Grundlagen - Wärmequelle Erdreich.wiki`, `Programm Dokumentation - Kühlung.wiki`, `Programm Dokumentation - Wärmequelle Erdreich.wiki` (gesammelt).
