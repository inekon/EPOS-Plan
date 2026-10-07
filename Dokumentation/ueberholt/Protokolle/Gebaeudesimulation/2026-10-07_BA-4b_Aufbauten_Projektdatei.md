# Protokoll BA-4b und Fix Raumabgleich — Aufbauten aus der Projektdatei (07.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#791**. Commits `495df62d` (Fix Raumabgleich), `47cb920c`, `6887149f`, `eeff8f26` (BA-4b); Zusammenführung mit BA-3 in `ade24c26`, `6564d6a1`, `ea4dd254`.
**Entscheid:** E98; Konzept [Bauteilaufbau beim Gebäudeimport](../../../aktuell/Gebaeudesimulation/2026-10-06_Konzept_Bauteilaufbau_Import.md) 5.5 und Welle BA-4; Befund [HottCAD-Projektdatei](../../../aktuell/Gebaeudesimulation/2026-10-05_Befund_HottCAD_Projektdatei.md), Nachtrag BA-4a.

## 1 Auftrag

Der Import übernimmt die Bauteilaufbauten samt Stoffwerten aus der HottCAD-Projektdatei und rechnet damit statt mit Ersatzaufbauten. Vorab ist der Raumabgleich zwischen Projektdatei und IFC-Datei zu reparieren.

## 2 Gebaut

### 2.1 Fix Raumabgleich

- Abgleich der Räume über die HottCAD-Eigenschaft `GUID` (`HSETU_BauteilAllgemein`), dann über die dekodierte `GlobalId`, zuletzt über den Raumnamen je Geschoss; `IfcAbbildBauer.GuidNormalform`, Felder `HottcadGuid` an Raum und Bauteil.
- Eine mehrdeutige GUID wird nicht geraten (Warnung `IMP_SQ_PROT_GUID_MEHRDEUTIG`); die Herkunft je Treffer steht in `IMP_SQ_PROT_RAUM_HERKUNFT`.
- **Wirkung am Sportheim:** vorher 0 von 56 Räumen zugeordnet, nachher 56 von 56 — alle 56 Räume in 12 Zonen mit Sollwerten, Lüftung, Geräten und Personen aus der Projektdatei; der Rückfall über den Namen trug 0.

### 2.2 Leser der Bauteiltabellen

- Vier Tabellen der Projektdatei, jede optional: `BmElement` (Level 3), `BmElementReference`, `TcBuildingElementDimension`, `TcBuildingElementDimensionLayer`. Platzhalter −987654321,99 und `{aaaaaaaa-…}` gelten als fehlend; Wärmekapazität c × 1000 bei Werten unter 50; Schichtfolge innen → außen; Rsi/Rse nach Lage gemäß DIN EN ISO 6946; `AddIns…` wird nicht gelesen.
- **Zuordnung** über `HottcadGuid` = Level-3-`GId`, ausweichend über die `GlobalId`; keine mehrdeutigen Zuordnungen; abgeleitete Deckenstücke erben die GUID.
- **Rangfolge (E98):** 1 Aufbau der Projektdatei bei U auf 1 % gleich dem IFC-U; 2 Katalogaufbau der Projektdatei mit dem U der IFC (gleiches U entscheidet bei mehreren Folgen, sonst kein Raten; Katalog ohne Lagefilter); 3 IFC-Schichten; 4 Ersatzaufbau. Meldungen `IMP_BAUTEIL_PROT_PD_RANG1` bis `PD_RANG4`, `PD_ZUORDNUNG`, `PD_U_ABWEICHUNG`, `PD_KATALOG_MEHRDEUTIG`.
- Stoffe als Projektkopie `Tab_Baustoff` (Herkunft IFC, Quelle „Projektdatei“); kein Schemaschritt.

## 3 Befund am Sportheim

| Größe | Wert |
|---|---|
| Bauteile über GUID zugeordnet | 448, 1 ohne Gegenstück |
| Rang 1 | 55 Zeilen, 586,6 m² |
| Rang 2 | 128 Zeilen, 1 768,4 m² |
| Rang 4 | 2 Zeilen, 7,7 m² |
| U-Abweichungen (IFC-Stand gilt) | 198 |
| Aufbauten / Stoffe | 11 / 20 |
| Stufe A | 41 → 183 Bauteile (554 → 2 355 m²) |
| Stufe B | 144 → 2 Bauteile (1 809 → 7,7 m²) |
| Jahresheizwärme | 92,58 → 92,52 MWh/a |
| Spitze | 51,24 → 51,37 kW |

Einzonenweg, Arbeitskopie von Projekt 1045.

## 4 Abweichungen vom Konzept

- „Gleiches U entscheidet“ als Präzisierung der Rangfolge bei mehreren Katalogfolgen.
- Der Katalog der Projektdatei wird ohne Lagefilter gelesen.
- Wände gegen Erdreich: die Richtung bleibt offen.
- `MaterialGroupType` wird nicht abgebildet.

## 5 Prüfung

Proben mit synthetischer Projektdatei (keine Anwenderdatei im Repositorium); Referenzlauf unberührt, keine Einfrierregel betroffen.

## 6 Gate 789

Gemeinsam mit BA-3, Hauptbaum auf `10c23a85` nach dem Merge mit SLP25 (Schritt 193), 71 min: 20 404 Tests, 20 400 grün, 4 übersprungen, 0 rot (KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 772, EPOS.Kern 11 658 mit 3 übersprungen); Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 21/21 gegen `2026-10-06_R39_Auslegungsheizlast` PASS, 646/646 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 479 Texte, 0 Fundstellen; Windows-Schale 0 Fehler; Auslieferungsvorlage-Tests 61/61. Im Vorlauf Gate 787 auf `ea4dd254` war einzig die Einheitenwache rot (J→kJ der Wärmekapazität im Steckbrief), behoben in `26242e21`.

## 7 Offen

- Codes für Putz, Folie und Luftschicht fehlen.
- Richtung bei Wänden gegen Erdreich.
- Ob `GUID` in allen HottCAD-Exporten die `GlobalId` ersetzt.
- `AddIns…` mit Werten.
- Weitere `.sqproj` der übrigen Gebäude für die Diagnose.
