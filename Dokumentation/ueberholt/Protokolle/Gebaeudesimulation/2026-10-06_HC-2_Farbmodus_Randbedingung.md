# Protokoll HC-2 — Gebäudeansicht: Farbmodus „Randbedingung“ (06.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#746**. Zwei Opus-Agenten im Worktree. Teil A: `5dc3bc5d` Randgruppen in DTO, Hülle und Texten, `b696ca49` Grundriss, Legende, Tests, `b74d6db6` Test Raumgrenzen. Teil B: `481ed1db` Körper färben je Dreieck, `22686e53` Bauteilschalter, Klick, Tests. Merge `aa223199`, Fix `8c048d02`. Kein Schemaschritt, keine Persistenz, keine Rechengröße, Basis unverändert.
**Entscheid:** E87 (F5); Konzept [HottCAD-Verbund](../../../aktuell/Gebaeudesimulation/2026-10-05_Konzept_HottCAD_Verbund_IFC_Projektdatei_Viewer.md) 4 und Welle 6.4; Vorwelle [HC-1](2026-10-05_HC-1_IFC_Randbedingung_Bauteilkoerper_Flaechen.md).

## 1 Auftrag

Die Gruppen R0–R7 der Hüllflächen aus HC-1 werden in der Gebäudeansicht sichtbar: Umschalter „Zonen | Randbedingung“, Legende mit Flächen und Schaltern, Kanten und Schraffur im Grundriss, Dreiecksfarben und Bauteilkörper in 3D, Klick mit Infozeile.

## 2 Zuschnitt

| Teil | Modell | Inhalt |
|---|---|---|
| A | Opus (Worktree) | DTO, Hülle (`GebaeudeImportAnsicht`), Texte, Grundriss mit Kanten, Schraffur und Legende, Tests |
| B | Opus (Worktree) | Körperansicht: Dreiecke je Gruppe, Transparenz R0, Bauteilkörper und Schalter, Klick, Tests |

## 3 Gebaut

- **Umschalter** im Reiterkopf; gesperrt mit benanntem Grund ohne Klassifikation oder über der Dreiecksgrenze 300 000.
- **Legende** je Gruppe R0–R7: Farbe, Name, Fläche in m², Schalter; R5 und R6 „nur Legende und Bilanz“; Schalter „Bauteilkörper“.
- **Grundriss:** Kanten in Gruppenfarbe, R7 als Marke auf der Kante, Boden schraffiert nach R3/R4.
- **3D:** Dreiecke je Gruppe in Geometriegruppen mit eigenem Material; R0 halbtransparent (Deckung 0,28, ohne `depthWrite`); ausgeblendete Gruppen fallen weg; Bauteilkörper als eigene Netze in Gruppenfarbe; Klick, auch durch R0, meldet Raum, Gruppe und Bauteil in einer Infozeile. Modus „Zonen“ unverändert.
- **Farbtafel** `GebaeudeAnsichtRandgruppen.FARBEN` für 2D und 3D.
- **Texte:** 21 neue Schlüssel `GANS_*` in beiden Sprachen.

## 4 Vertrag Bytefeld und Szene

Bytefeld: Raumkörper unverändert, dann Bauteilkörper, dann ein Gruppenbyte je Dreieck (255 = keine Gruppe). Ohne Klassifikation byteweise wie bisher.

## 5 Abweichungen vom Konzept

1. Farben als C#-Konstante statt Hausblatt-Token mit Dunkelfassung.
2. Grundriss schematischer Umrisse (Datei ohne Raumgrenzen): Die Kante bekommt die flächengrößte Gruppe gleicher Außenrichtung ±15°, keine Fenstermarken.
3. Umrisse aus Raumgrenzen: Abstand ≤ 0,15 m, Richtung ±15°, Fenstermarke bei Schwerpunkt ≤ 0,6 m.
4. R7 hat Fläche, aber 0 Dreiecke.
5. 3D mit Geometriegruppen statt Farbattribut; Transparenz nur für R0.
6. Prismen und Exportmodell sind im Randmodus neutral grau.
7. Randkanten ausgeblendeter Gruppen bleiben stehen.

## 6 Prüfungen der Agenten

Kern-Filter und Windows-Schale (Linux) je 0 Fehler. Teil A: Filter UI 94/94, Kern 121/121. Teil B: Filter UI 96/96, Kern 119/119, voller Lauf `EPOS.UI.Tests` 7 565/7 565. Browserprobe mit Chromium (Scratchpad, nicht committet): Abschnitte, Farben, Deckungen, Ausblenden, 20 Wechsel ohne Leck (9 Geometrien, 6 Programme), echte Klicks im Rand- und Zonenmodus; node-Lauf der reinen Aufbaufunktion `randaufbau`.

## 7 Gate 746

Hauptbaum, Kopf `aa223199`, 48 min. Kern-Build grün. KiKern 549/549, SpeicherEngine 397/397, SpeicherPlanung 28 (27 grün, 1 übersprungen), EPOS.UI 7 577/7 577, EPOS.Kern 11 240 (11 236 grün, 3 übersprungen, 1 rot). Zusammen 19 791 Tests, 19 786 grün, 4 übersprungen. Dokumentationswachen 35/35. ChartProben 211 Hashes gleich `Messlatte_2026-10-05`. Referenzlauf 21/21 gegen `2026-10-05_R38_Vorlaufwahl` PASS, 0 Abweichungen, 646/646 CSV byte-gleich; Störlauf ulp PASS.

## 8 Merge-Konflikt und Fix

- **Konflikt** nur in `Resource.resx` und `Resource.en-US.resx`: beide Seiten angefügt (NP3a und HC-2), 15 267 Schlüssel ohne Doppel, Designer neu erzeugt.
- **Rot im Gate:** `KulturwaechterTests.Jede_Kulturvorrichtung_wird_entsorgt` an HC-2-Testcode (Kulturvorrichtung im `foreach`). Fix `8c048d02` (eigene `using`-Anweisung, wie `17d1e02c`); Wächter- und Testklasse danach 36/36 grün.

## 9 Offen

- **Sichtabnahme unter Windows:** Lesbarkeit der Farben (Licht, Dunkelmodus, Gelb R4 auf weißem Boden), Strichbreiten 4/8 px, Lage des Umschalters und der Hinweiszeile bei gbXML-Importen, R0-Hülle ohne Flackern beim Drehen, Bauteilkörper auf Wandflächen (Z-Fighting), Klick durch R0 und Infozeile, Tempo nahe der Dreiecksgrenze.
- **Logbuch-Entwurf** (Version beim Anwender erfragen): „Die Gebäudeansicht des Imports zeigt die Hüllflächen wahlweise nach Randbedingung gefärbt, mit Legende und Flächensummen.“
- **Wiki-Upload** „Gebäudeimport“ gebündelt.
- Nächste Welle HC-4 („Datei erneut lesen“) nach Zuruf; iPad-Sichtprobe hängt an Probe 31.
