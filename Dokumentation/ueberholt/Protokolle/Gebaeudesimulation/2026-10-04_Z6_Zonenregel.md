# Protokoll Z6 — Zonenregel „nach Raumtemperatur und Nutzung“ (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle Z6 (Entscheid E72), ein Opus-Agent in zwei Durchgängen, Commits `3edb7885`, `e65b7a17`, `a262cd86`, `caf29801`, Merge `34a5e27a`. Statuszeile #726.
**Entscheid:** E72 (Anwender, 04.10.2026); ein Entscheid der Orchestrierung (Abschnitt 4). Kein Schemaschritt, Basis unverändert, Referenzlauf nicht betroffen.

## 1 Auftrag

Die HottCAD-IFC-Exporte (sechs Dateien unter `Quellen/`, IFC2X3 und IFC4) tragen weder `IfcZone` noch `IfcSpatialZone`, keine Klassifikation, keinen `LongName` und keine Raumgrenzen; wählbar waren nur Z4 und Z5. Je Raum stehen aber Solltemperatur (`HSETU_RaumAllgemein.InsideTemperature`), Beheizungsart (`HeatingType`) und Raumtyp (`RoomType`) in der Datei. Z6 bildet daraus Zonen gebäudeweit, auch ohne Raumgrenzen.

## 2 Vorgehen

Ein Opus-Agent (76 + 10 Aufrufe) im Worktree `z6`: Regel in `GebaeudeZonierung`, Raumtyp und Raumtemperatur in `IfcAbbildBauer`, Ressourcen de/en, Tests `ZonierungZ6Tests`, zwei Importproben, Diagnoseausgabe, Mehrzonenkonzept 6.1, Wiki-Quelle „Gebäudeimport“; Nachzug der Mindestgrößenregel nach Abschnitt 4. Merge ohne Konflikt.

## 3 Ergebnis

- **Regel Z6** (`IfcImportProfil.ZONENREGEL_Z6`): wählbar bei IFC mit mehr als einem Raum, ohne Bedingung an Raumgrenzen, wenn die Gruppenbildung mehr als eine und weniger als `Raeume.Count` Gruppen ergibt; Gruppenschlüssel Beheizung (wirksam, mit Übersteuerung) und auf ganze °C gerundete Raumtemperatur; Räume ohne Temperatur zur Gruppe gleicher Beheizung und Nutzungsklasse. Liste Z4, Z6, Z5; Vorgabe nach M7 unverändert.
- **Raumtemperatur:** `InsideTemperature` wird nicht als Sollwert (`SollHeizenC`, fließt in den Rechenweg) übernommen, sondern als `AbbildRaum.RaumtemperaturC` nur für Z6; Z6 nimmt `SollHeizenC`, sonst `RaumtemperaturC`.
- **Nutzungsklassen** (`GebaeudeZonierung.Nutzungsklasse`): Büro, Wohnen, Schlafen, Gastronomie, Küche, Sport, Verkehr, Sanitär, Lager, Technik, Sonstige aus `Raumtyp` (CAD-Aufzählung ohne Präfix) oder dem Raumnamen; `mrtConnection` ist in den Quelldateien ein Anschlussraum und zählt zu Technik. Der Zonenname nennt Temperatur und bis zu drei flächengewichtet häufigste Klassen.
- **Mindestgröße M8 unter Z6:** stets zur Zone mit der nächstliegenden Solltemperatur gleicher Beheizung; bei Gleichstand die größere gemeinsame Fläche, dann die größere Zone. Unter Z1–Z5 unverändert.
- **Zahlen an den sechs Quelldateien** (Z6 überall wählbar): MFH-Klein 4 Zonen (20/20 unbeheizt/15/10 °C), MFH_mittel 4 (24/20/16/15 °C; „10 °C – Lager, Technik“ entfällt ohne Flächen), Produktion 3 (24/20/15 °C), Sportheim 4 (20 °C 474,72 m² 26 Räume, 16 °C Sport 114,7 m², 15 °C 129,58 m² 14 Räume, 10 °C unbeheizt 464,56 m² 15 Räume; 24 °C Sanitär zugeschlagen), Verwaltung 4 (20 °C 2 003,27 m² 63 Räume, 17 °C 766,73 m², 15 °C 1 573,09 m² 26 Räume, 10 °C unbeheizt 287,09 m² 22 Räume), WG-EH55 4 (24/20/16/15 °C). Warnung `GRENZEN_ENTKOPPELT(Z6)` in MFH_mittel, Sportheim und Verwaltung, bis G7f-4 die Trennflächen aus den Raumkörpern bringt (E73).
- **Tests:** `ZonierungZ6Tests` 29 + 1 Fälle; zwei Proben `ifc4_z6_sollwerte.ifc` (Standard-Sollwerte) und `ifc4_z6_cad.ifc` (CAD-Muster mit Satz `CAD_RaumAllgemein`, Dusche 3 m² 24 °C als Fall für die Mindestgröße); vier bestehende Tests um Z6 in der Regelliste ergänzt; Proben byte-gleich neu erzeugbar. Oberfläche unverändert (kein Regelschlüssel fest aufgezählt).

## 4 Entscheide der Orchestrierung

1. Mindestgröße unter Z6 nach der Temperatur vor der Nachbarregel — sonst legte die Verwaltung ihre 24-°C-Sanitärzone in die 15-°C-Zone, weil die Trenndecken aus Raumbezügen dort einen Nachbarn liefern. Widerspruch möglich.

## 5 Abweichungen

Satzname der CAD-Probe `CAD_RaumAllgemein` statt des Herstellernamens (neutral wie die vorhandenen CAD-Proben); „(unbeheizt)“ unter Z6 aus der Ressource `GIMP_ZONE_UNBEHEIZT`.

## 6 Abnahme

Gate 725 im Hauptbaum auf `34a5e27a`: Kern-Filter 0 Fehler; ChartProben 208 Hashes gleich der Messlatte; Tests Kern 10 907 grün, 2 übersprungen, 1 rot fremd (`KiDialogaufrufTests`: Hilfeschlüssel der Kältemaschine ohne KI-Bereich, aus der Welle KU3 einer anderen Sitzung, auf origin mit `dfdeee45` behoben), UI 7 516, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen); Dokumentationswachen 35/35; Referenzlauf 19/19 PASS gegen R35, 576/576 CSV byte-gleich; gestörter Lauf PASS. Nachlauf auf dem Merge mit origin `838299c1`: Kern-Filter 0 Fehler, 586 gefilterte Kern-Tests (Ifc, Zonierung, Zonenimport, GebaeudeImport, KiDialogaufruf, Importproben, Ressourcen) grün, 1 übersprungen, 59 UI-Importtests grün, Designer ohne Abweichung.

## 7 Offen

- Z6 vergibt der Zone keinen Heizsollwert aus `RaumtemperaturC` (Rechenweg; Anwenderentscheid).
- Logbuch-Satz (Version offen): „Der Gebäudeimport aus IFC bietet die Zonenregel ‚Z6 – nach Raumtemperatur und Nutzung‘, die die Räume nach Beheizung und Raumtemperatur zu Zonen zusammenfasst, auch ohne Raumgrenzen der Datei.“
- Windows-Sichtabnahme der Klappliste und der Zonen an den HottCAD-Dateien (Anwender).

## 8 Aufwand

Rund 86 Agentenaufrufe, 0,5 PT.
