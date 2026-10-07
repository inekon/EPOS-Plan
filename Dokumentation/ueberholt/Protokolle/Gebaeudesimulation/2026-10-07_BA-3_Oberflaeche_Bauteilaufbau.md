# Protokoll BA-3 — Oberfläche Bauteilaufbau mit Bauteilsteckbrief (07.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#788**. Commits `c258878f`, `2c1e4835`, `84772ee0`, `fc84edad`; Zusammenführung mit BA-4b in `ade24c26`, `6564d6a1`, `ea4dd254` (Steckbrief zeigt Projektdatei und Rang, Typwahl nur auf Ersatz).
**Entscheid:** E98 (Steckbrief), E95-4 (Typwahl); Konzept [Bauteilaufbau beim Gebäudeimport](../../../aktuell/Gebaeudesimulation/2026-10-06_Konzept_Bauteilaufbau_Import.md) Welle BA-3.

## 1 Auftrag

Die Zuordnungsstufen und Aufbauten aus BA-1 bis BA-4b werden in der Oberfläche sichtbar: Farbmodus, Steckbrief je Bauteil, Aufbauliste im Import und Spalte im Zonendialog.

## 2 Gebaut

- **Farbmodus „Aufbau“:** dritter Knopf neben Zonen und Randbedingung. Farbtafel: A grün `#2ca02c`, B gelb `#e5c100`, C orange `#f07f13`, transparent blaugrau `#8fa3ad`, ohne Bauteil `#d6d3cc`. Legende je Stufe mit Zahl und Fläche, außen und innen getrennt, Schalter je Stufe. 3D: Raumflächen über das Bauteil je Dreieck; 2D: Kanten.
- **Bauteilsteckbrief:** Klick auf Bauteilkörper, Raumfläche oder Grundrisskante. Kopf mit Stufe, Fläche, Azimut, Neigung, Randbedingung; U mit Herkunft (Datei, Schichten, Vorgabe, Projektdatei, Katalog, Manuell) und Hinweis bei Abweichung über 10 %; R₁, C₁ und Σ d·ρ·c (nur Anzeige über `Bauteilreduktion`); Schichtliste mit Herkunft, weggelassene Schichten am Ende ausgegraut mit Grund; Ersatzaufbau mit Typ und Dämmdicke; Fenster mit U, g, Rahmenanteil; Rang 1 bis 4 aus BA-4b. Im Import und in „Datei erneut lesen“; schreibt nichts.
- **Liste „Bauteilaufbauten“** im Import: je Aufbau Stufe, Art, Zahl, Fläche, U Datei, U Schichten, C₁,korr je m², was fehlt, Typaufbau, Rang bzw. Herkunft; Filter „Nur Stufe B und C“; Sprung zur Baustoffzeile; **Typwahl je Ersatzaufbau** (E95-4), wirkt nur auf Rang 4.
- **Zonendialog:** Spalte „Aufbau“ mit Stufe und Filter „Ohne vollständige Zuordnung“.
- 85 + 4 Texte in beiden Sprachen.

## 3 Abweichungen vom Konzept

- Weggelassene Schichten stehen am Ende der Liste.
- „Katalogaufbau übernehmen“ nur über den Bauteildialog.
- Flächenanteile je Stufe in der Zonenzeile nicht gebaut.
- In „Datei erneut lesen“ kein Sprung „Bauteil bearbeiten“.

## 4 Prüfung

Kern-Filter und Windows-Schale bauen mit 0 Fehlern. Neu sind `GebaeudeAufbauTests` (11 Fälle), `GebaeudeAufbauHuelleTests` (6) und drei Fälle in `GebaeudeImportDialogTests`; im Auftrag liefen EPOS.UI.Tests 541/541 und EPOS.Kern.Tests 1 252/1 252 der betroffenen Klassen. Nach dem Merge mit BA-4b: Kern 724/724, UI 405/405 der betroffenen Klassen, SQL-Dialekt 0 Fundstellen, Referenzlauf 8/8 PASS gegen R39. Die Liste „Bauteilaufbauten“ ist eine schlichte Tabelle ohne `Raster` oder `Katalogliste`; Raster- und Fensterprobe sind nicht betroffen.

## 5 Gate 787

Gemeinsam mit BA-4b und dem Fix des Raumabgleichs, Hauptbaum auf `ea4dd254`, 43 min: 20 389 Tests, 20 384 grün, 4 übersprungen, 1 rot (KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 772, EPOS.Kern 11 643 mit 3 übersprungen); rot war die Einheitenwache (J→kJ der Wärmekapazität im Steckbrief), behoben in `26242e21`, Wache 9/9 nachgeprüft; Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 21/21 gegen `2026-10-06_R39_Auslegungsheizlast` PASS, 646/646 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 471 Texte, 0 Fundstellen; Windows-Schale 0 Fehler; Auslieferungsvorlage-Tests 60/60.

## 6 Offen

- **Sichtabnahme unter Windows:** Farbmodus Aufbau in 3D und 2D, Klick auf Fläche und Kante, Steckbrief neben dem Bild bzw. darunter unter 900 px, Gelb auf hellem Grund, Typwahl, Sprung zur Baustoffzeile, Spalte im Zonendialog.
- Logbuch-Entwürfe (Version beim Anwender erfragen): „Die Gebäudeansicht zeigt im Farbmodus ‚Aufbau‘, wie vollständig die Bauteile beschrieben sind; ein Klick auf ein Bauteil öffnet seinen Steckbrief mit U-Wert, Aufbau und Wärmekapazität.“ und „Beim Import mit HottCAD-Projektdatei übernimmt EPOS-Plan die Bauteilaufbauten samt Baustoffwerten aus der Projektdatei.“
- Wiki-Upload gebündelt (Quellen `Projekte/Wiki/Programm Dokumentation - Gebäudeimport.wiki` und `… - Gebäude.wiki` fortgeschrieben).
