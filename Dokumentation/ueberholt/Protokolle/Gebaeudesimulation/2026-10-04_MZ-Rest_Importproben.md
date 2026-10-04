# MZ-Rest — Importproben 13 bis 18 an öffentlichen Beispieldateien (04.10.2026)

Welle MZ-Rest der Gebäudesimulation, Teil 2. Proben nach dem
[Mehrzonenkonzept](../../../aktuell/Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md), Kapitel 8, Nr. 13 bis 18.

## Dateien und Weg

- **Quelle:** Die Dateien liegen nicht im buildingSMART-Repositorium `Sample-Test-Files` (dort nur
  `Simple-Scene` und ISO-Beispiele); `ifcwiki.org` ist gesperrt. Gefunden über die GitHub-Codesuche als
  LFS-Dateien öffentlicher Testsammlungen: `AC20-FZK-Haus.ifc` (IFC4, 2,57 MB) und `FM_ARC_DigitalHub.ifc`
  (IFC4, 14,3 MB) aus `bldrs-ai/test-models`, `FM_ARC_DigitalHub_with_SB_neu.ifc` (IFC4, 17,6 MB, mit
  Raumgrenzen angereichert) aus `andoludo/ifctrano`. Die FZK-Fassung mit Raumgrenzen-Anreicherung ist nicht
  auffindbar. Die Dateien lagen nur im Scratchpad, nie im Repositorium.
- **Werkzeug:** `EPOS.Kern.Tests/IfcBeispieldateienProbenTests` liest jede `*.ifc` des Ordners aus
  `EPOS_IFC_BEISPIELE` über `IfcLeser` → `GebaeudeZonierung` → `GebaeudeBauteilvorschlag`; ohne Variable ist die
  Probe übersprungen. Diagnose ohne Messlatte, Release, Linux.

## Ergebnis

| Kennzahl | FZK-Haus | DigitalHub | DigitalHub mit SB |
|---|---|---|---|
| Lesen | 0,9 s, 0 Fehler | 2,1 s, 0 Fehler, W `KEINE_MENGEN`, `SEITE_UNBESTIMMT` | 2,2 s, 0 Fehler, dieselben W |
| Geschosse / Räume / beheizt | 2 / 7 / 7 | 3 / 64 / 58 | 3 / 59 / 6 |
| Räume mit Grundriss (Extrusion) | 0 | 61 | 0 |
| Raumfläche Σ (beheizt) | 173,3 (173,3) m² | 0,0 m² | 2 780,2 (75,6) m² |
| Raumgrenzen (2. Ebene / mit Polygon / mit Gegenstück) | 81 (81 / 70 / 0) | 1 123 (1 123 / 875 / 0) | 2 943 (1 819 / 2 288 / 1 526) |
| Bauteile + Öffnungen | 17 + 16 | 223 + 117 | 223 + 117 |
| mit U-Wert / mit Aufbau / Schichten | 33 / 17 / 17 | 296 / 194 / 412 | 296 / 194 / 412 |
| **P13** Vorgabe, Zonen (beheizt) | Z4, 2 (2) | Z4, 4 (3) | Z4, 5 (2) |
| Seiten / Paare (Fläche) | 19 / 1 (84,9 m²) | 275 / 2 (78,1 m²) | 555 / 7 (2 035,9 m²) |
| Vorschlag unter der Vorgabe | 26 Zeilen, 0 Fehler | 239 Zeilen, 6 Fehler | 428 Zeilen, 2 Fehler |
| **P14** Polygonflächen Σ (Bauteile + Öffnungen) | 707,5 m² (666,9 + 40,6) | 9 510,4 m² | 19 558,0 m² |
| **P16** Z5: Außenwand / Dach / Grund / Fenster [m², U] | 136,3 (0,40) / 280,8 (0,38) / 86,7 (0,25) / 23,2 (1,40) | Ablehnung (2 Fehler) | Ablehnung (1 Fehler) |
| **P17** Außenbauteile mit Polygon: Raumseite / Brutto | 550,2 / 567,0 m² (−3,0 %) | keine Bruttomengen | keine Bruttomengen |
| **P18** Flächen ohne Gegenstück | 0 | 68 | 261 |

Die Fehler der Bauteilvorschläge sind benannte Ablehnungen, kein stilles Übergehen:
`ZONE_OHNE_NUTZFLAECHE` (DigitalHub, Räume ohne Flächenmenge), `AZIMUT_FEHLT` (2 bzw. 3 Bauteile) und
`UWERT_FEHLT` (8 bzw. 111 Bauteile, unter Z5 die Vorgaben von Dach und Grund).

## Bewertung je Probe

- **13 Zählproben:** Alle drei Dateien laufen ohne Leserfehler bis zum Bauteilvorschlag; Zonen, Grenzen,
  Paare, U-Werte und Schichten werden gezählt. Erfüllt für das FZK-Haus; die DigitalHub-Fassungen enden in
  benannten Ablehnungen (fehlende Mengen und U-Werte der Datei).
- **14 Polygonflächen:** Die Sollwerte (778,2 / 1 358,1 / 22 862,1 m²) werden **nicht** getroffen: FZK-Haus
  707,5 m², DigitalHub mit SB 19 558,0 m². Ein Teil der Grenzen trägt kein lesbares Polygon (FZK 11 von 81,
  DigitalHub mit SB 655 von 2 943); die Sollzahlen der Textauszählung zählen sie mit. Offen: Welche
  Randkurven (`IfcCompositeCurve` mit Bögen, 3D-Randpunkte) fehlen, ist je Datei aufzuschlüsseln.
- **15 mit und ohne Anreicherung:** Für das FZK-Haus nicht prüfbar (keine angereicherte Fassung gefunden).
  Am DigitalHub weicht die beheizte Fläche ab: ohne Raumgrenzen 58 von 64 Räumen beheizt, aber ohne
  Flächenmengen (Σ 0 m²); mit Raumgrenzen 6 von 59 beheizt (75,6 von 2 780,2 m²). **Befund:** Die
  Beheizungsregeln greifen in beiden Fassungen verschieden (Ursache offen), und die ungereicherte Fassung
  führt keine Raumflächen, obwohl 61 Räume einen Grundriss tragen.
- **16 Einzonenfall:** Z5 rechnet für das FZK-Haus durch. **Befund:** Die Dachfläche unter Z5 (280,8 m²) ist
  größer als unter Z4 (124,6 m²) plus Trennfläche (84,9 m²); die Hülle des Einzonenwegs ist an dieser Datei
  gegen die Zahlen aus Befund N zu prüfen (offen).
- **17 Raumseitenmaß gegen Bruttomaß:** FZK-Haus **−3,0 %** (Raumseite 550,2 m² gegen Brutto 567,0 m² an 12
  Außenbauteilen). Damit ist der Abstand beziffert; Weg 1 aus 6.2 ist für diese Datei tragbar. Die
  DigitalHub-Dateien führen keine Bruttomengen.
- **18 Archicad-Rekonstruktion:** Das FZK-Haus führt keine `CorrespondingBoundary` (0 von 81); die Paarbildung
  (Regel 1 und Geometrie) findet für jede innere Seite ein Gegenüber — **0 Flächen ohne Gegenstück** statt der
  erwarteten sechs mehrdeutigen Bauteile. Die DigitalHub-Fassungen nennen 68 bzw. 261 Flächen ohne Gegenstück
  benannt (Geschossdecken und Innenwände).

## Trenndecke ohne Raumgrenzen an den Dateien

Keine der drei Dateien übt die neue Regel aus: Das FZK-Haus und beide DigitalHub-Fassungen führen Raumgrenzen
(dann greift die Regel nicht), und das FZK-Haus beschreibt seine Räume nicht als Extrusion. Geprüft ist die Regel
an der erzeugten Probe `IfcProbenErzeuger.Uebereinander` (`IfcTrenndeckeGrundrissTests`).

## Offen

- Raumfläche aus dem Grundriss als Rückfall, wenn die Datei keine Flächenmenge führt (DigitalHub: 61 Räume).
- Beheizungsregel der angereicherten DigitalHub-Fassung (6 von 59 beheizt).
- P14: Grenzen ohne lesbares Polygon aufschlüsseln; P16: Dachfläche unter Z5 am FZK-Haus.
