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

Stand nach dem Nachzug (Abschnitt „Nachzug“); geänderte Werte mit dem Wert des ersten Laufs in Klammern.

| Kennzahl | FZK-Haus | DigitalHub | DigitalHub mit SB |
|---|---|---|---|
| Lesen | 0,9 s, 0 Fehler | 2,1 s, 0 Fehler, W `KEINE_MENGEN`, `SEITE_UNBESTIMMT` | 2,2 s, 0 Fehler, dieselben W, dazu W `AUSSEN_WIDERSPRUCH` (53 Räume) |
| Geschosse / Räume / beheizt | 2 / 7 / 7 | 3 / 64 / 58 | 3 / 59 / **56** (6) |
| Räume mit Grundriss (Extrusion) / Fläche aus Grundriss | 0 / 0 | 61 / **61** (0) | 0 / 0 |
| Raumfläche Σ (beheizt) | 173,3 (173,3) m² | **2 913,7 (2 886,4) m²** (0,0) | 2 780,2 (**2 563,5**; vorher 75,6) m² |
| Raumgrenzen (2. Ebene / mit Polygon / mit Gegenstück) | 81 (81 / 70 / 0) | 1 123 (1 123 / 875 / 0) | 2 943 (1 819 / **2 329** / **1 562**; vorher 2 288 / 1 526) |
| Bauteile + Öffnungen | 17 + 16 | 223 + 117 | 223 + 117 |
| mit U-Wert / mit Aufbau / Schichten | 33 / 17 / 17 | 296 / 194 / 412 | 296 / 194 / 412 |
| **P13** Vorgabe, Zonen (beheizt) | Z4, 2 (2) | Z4, 4 (3) | Z4, **4 (3)** (5 (2)) |
| Seiten / Paare (Fläche) | 19 / 1 (84,9 m²) | 275 / 2 (78,1 m²) | **486 / 6 (2 629,3 m²)** (555 / 7 (2 035,9 m²)) |
| Vorschlag unter der Vorgabe | 26 Zeilen, 0 Fehler | 239 Zeilen, **2 Fehler** (6) | **370 Zeilen**, 2 Fehler (428) |
| **P14** Polygonflächen Σ (Bauteile + Öffnungen) | 707,5 m² (666,9 + 40,6) | 9 510,4 m² | **22 432,1 m²** (19 558,0) |
| **P14** Grenzen ohne Bauteil (Zahl / Fläche) | 6 `IfcVirtualElement` 36,6 m², 5 virtuell ohne Bauteil 34,1 m² | 184 virtuell ohne Bauteil 3 494,8 m², 64 `IfcColumn` 48,0 m² | 14 `IfcRoof` (Glasdächer) 228,9 m², 42 `IfcBeam` 107,6 m², 150 `IfcColumn` 91,0 m² |
| **P16** Z5: Außenwand / Dach / Grund / Fenster [m², U] | 136,3 (0,40) / **165,1 (0,30)** / **120,0 (0,40)** / 23,2 (1,40); vorher Dach 280,8 (0,38), Grund 86,7 (0,25, Vorgabe) | Ablehnung (2 Fehler) | Ablehnung (2 Fehler; vorher 1) |
| **P17** Außenbauteile mit Polygon: Raumseite / Brutto | **379,6 / 451,4 m² (−15,9 %)** an 11 Bauteilen (550,2 / 567,0 m², −3,0 % an 12) | keine Bruttomengen | keine Bruttomengen |
| **P18** Flächen ohne Gegenstück | 0 | 68 | **179** (261) |

Die Fehler der Bauteilvorschläge sind benannte Ablehnungen, kein stilles Übergehen: `AZIMUT_FEHLT` (2 bzw. 3
Bauteile) und `UWERT_FEHLT` (DigitalHub unter Z4 32, unter Z5 8 Bauteile; mit SB unter Z4 32 Bauteile (vorher 111),
unter Z5 10 (vorher die zwei Vorgaben von Dach und Grund)). `ZONE_OHNE_NUTZFLAECHE` (DigitalHub) entfällt mit der Raumfläche aus dem Grundriss. Unter Z5
führen die DigitalHub-Fassungen keine Bauteilmengen (`KEINE_MENGEN`); der Einzonenweg rechnet mit Bruttomengen und
lehnt deshalb benannt ab.

## Bewertung je Probe

- **13 Zählproben:** Alle drei Dateien laufen ohne Leserfehler bis zum Bauteilvorschlag; Zonen, Grenzen,
  Paare, U-Werte und Schichten werden gezählt. Erfüllt für das FZK-Haus; die DigitalHub-Fassungen enden in
  benannten Ablehnungen (fehlende Bauteilmengen und U-Werte der Datei).
- **14 Polygonflächen:** Der Leser liest **jedes** Polygon: Alle Grenzen der drei Dateien tragen eine
  `IfcCurveBoundedPlane` mit `IfcPolyline` bzw. `IfcCompositeCurve` aus Polylinien, keine Darstellungsart fehlt. Die
  Lücke zu den Sollwerten sind Grenzen, die zu keinem Bauteil der Hülle gehören — jetzt je Art mit Fläche benannt
  (`GRENZEN_OHNE_BAUTEIL`). FZK-Haus: 707,5 + 36,6 + 34,1 = **778,2 m²** = Sollwert. DigitalHub mit SB: die größte
  Lücke war das Flachdach (`IfcRoof` aus einer Platte, die Grenzen am Dach) mit 2 874,1 m² — jetzt gelesen; 22 432,1 +
  228,9 + 107,6 + 91,0 = 22 859,6 m² gegen den Sollwert 22 862,1 m² der Textauszählung (Nachzählung aller Grenzen der
  2. Ebene: 22 859,7 m²; Δ 2,4 m² = 0,01 % bleibt der Textauszählung). DigitalHub ohne SB: kein Sollwert; Σ aller
  Grenzen 13 053,2 m² = 9 510,4 + 3 494,8 + 48,0.
- **15 mit und ohne Anreicherung:** Für das FZK-Haus nicht prüfbar (keine angereicherte Fassung gefunden).
  DigitalHub: ohne Raumgrenzen 58 von 64 Räumen beheizt mit 2 886,4 m² (Fläche aus dem Grundriss), mit Raumgrenzen 56
  von 59 mit 2 563,5 m². Die Ursache der 6 von 59 war ein Widerspruch der angereicherten Datei: Sie setzt an 53
  Räumen `Pset_SpaceCommon.IsExternal = TRUE` bei `PredefinedType = INTERNAL` (die Fassung ohne Raumgrenzen führt
  keinen `Pset_SpaceCommon`). Die Raumzahl unterscheidet sich (64 gegen 59), weil die Anreicherung Räume
  zusammenlegt.
- **16 Einzonenfall:** Z5 rechnet für das FZK-Haus durch; Dach 165,1 m² (U 0,30) = `Dach-1` + `Dach-2` (je
  82,6 m² `GrossArea`), Grund 120,0 m² (U 0,40) = `Bodenplatte` — so, wie Befund N 4.3 die Felder füllt (Dach aus
  `ROOF`, Grundfläche aus `BASESLAB`). Ursache der 280,8 m²: die Geschossdecke `Slab-033` (FLOOR, 115,6 m², U 0,5)
  galt wegen eines äußeren Randstreifens von 0,82 m² neben 14 inneren Grenzen (169,8 m²) als Außenbauteil und zählte
  als Dach; die Bodenplatte trägt Grenzen `EXTERNAL` statt `EXTERNAL_EARTH` und zählte als Boden über Außenluft unter
  „Sonstige“, die Grundfläche kam deshalb aus der Vorgabe. Keine doppelte Zählung, keine Projektion.
- **17 Raumseitenmaß gegen Bruttomaß:** FZK-Haus **−15,9 %** (Raumseite 379,6 m² gegen Brutto 451,4 m² an 11
  Außenbauteilen). Die −3,0 % des ersten Laufs zählten die Geschossdecke mit beiden Seiten (170,6 m² Raumseite gegen
  115,6 m² Brutto) mit und glichen so die übrigen Bauteile aus. Die Bodenplatte trägt raumseitig 163,3 m² gegen
  120,0 m² brutto (die Galerie grenzt mit 61,9 m² an die Bodenplatte — eine Eigenheit der Datei). Ohne die Bodenplatte
  liegen die Außenwände und Dächer raumseitig 216,3 gegen 331,4 m² brutto. Die DigitalHub-Dateien führen keine
  Bruttomengen.
- **18 Archicad-Rekonstruktion:** Das FZK-Haus führt keine `CorrespondingBoundary` (0 von 81); die Paarbildung
  (Regel 1 und Geometrie) findet für jede innere Seite ein Gegenüber — **0 Flächen ohne Gegenstück** statt der
  erwarteten sechs mehrdeutigen Bauteile. Die DigitalHub-Fassungen nennen 68 bzw. 179 Flächen ohne Gegenstück
  benannt (Geschossdecken und Innenwände).

## Trenndecke ohne Raumgrenzen an den Dateien

Keine der drei Dateien übt die neue Regel aus: Das FZK-Haus und beide DigitalHub-Fassungen führen Raumgrenzen
(dann greift die Regel nicht), und das FZK-Haus beschreibt seine Räume nicht als Extrusion. Geprüft ist die Regel
an der erzeugten Probe `IfcProbenErzeuger.Uebereinander` (`IfcTrenndeckeGrundrissTests`).

## Offen

- P17 am FZK-Haus: Der Abstand Raumseite gegen Brutto ist mit −15,9 % größer als im ersten Lauf; ob Weg 1 aus 6.2
  für diese Datei tragbar bleibt, ist mit dem Anwender zu bewerten (E27 hält das Raumseitenmaß).
- Mehrteilige Dächer, deren Grenzen am Dach hängen (DigitalHub mit SB: zwei Glasdächer, 228,9 m²), bleiben ohne
  Geometrie unzugeordnet und sind nur benannt.
- Unter Z5 lehnen beide DigitalHub-Fassungen ab, weil die Datei keine Bauteilmengen führt; eine Bruttofläche aus den
  Polygonen ist nicht Teil des Einzonenwegs.

## Nachzug (04.10.2026)

Die vier offenen Befunde des ersten Laufs sind im Kern behoben; die Regeln stehen im
[Mehrzonenkonzept](../../../aktuell/Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.1, 6.2 und 6.5, gehalten von der
erzeugten Probe `IfcProbenErzeuger.Importbefunde` (`EPOS.Kern.Tests/IfcImportbefundeTests`, sieben Tests).

| Befund | Ursache | Regel | vorher → nachher |
|---|---|---|---|
| Raumfläche ohne Flächenmenge | DigitalHub führt keine Raummengen; der Grundriss war nur für die Trenndecke gelesen | Fehlt jede Flächenmenge, gilt die Fläche des Grundrisses (Trapezformel), gekennzeichnet `FlaecheAusGrundriss`, Meldung `FLAECHE_GRUNDRISS` (I) | DigitalHub 0,0 → 2 913,7 m² (61 Räume), beheizt 2 886,4 m²; `ZONE_OHNE_NUTZFLAECHE` 3 → 0 |
| Beheizung der angereicherten Fassung | Die Datei setzt `IsExternal = TRUE` an 53 Innenräumen (`PredefinedType = INTERNAL`) — ein Widerspruch der Datei | B2 nicht gegen ein ausdrückliches `INTERNAL`; Warnung `AUSSEN_WIDERSPRUCH` | beheizt 6 → 56 von 59 (3 nach B4: Namensregel) |
| P14 Grenzen ohne Polygon | Keine Darstellungsart fehlt; die Grenzen hängen an Elementen, die kein Bauteil werden — größter Posten das Flachdach (`IfcRoof` aus einer Platte, Grenzen am Dach) | Grenzen eines zerlegten Dachs gelten für seine einzige Platte (`GRENZEN_DACHPLATTE`, I); übrige Grenzen ohne Bauteil je Art mit Fläche (`GRENZEN_OHNE_BAUTEIL`, I) | DigitalHub mit SB 19 558,0 → 22 432,1 m²; FZK-Haus und DigitalHub unverändert, Lücke benannt |
| P16 Dach unter Z5 | Ein äußerer Randstreifen (0,82 m²) machte die Geschossdecke zum Außenbauteil; die Bodenplatte an Grenzen `EXTERNAL` galt als Boden über Außenluft | Äußere Grenzen unter 5 % der Grenzfläche entscheiden nicht (`AUSSEN_SPLITTER`, I); Bodenplatte an `EXTERNAL` erdberührt wie unter `IsExternal` | Dach 280,8 → 165,1 m², Grund 86,7 (Vorgabe) → 120,0 m² |

Unverändert: die sechs Anwenderdateien unter `Quellen/` (Ausgaben der Quelldiagnose zeilengleich; keine führt
Raumgrenzen oder `IsExternal` an Räumen) und die Importproben 1–12. Unter Z4 bleibt das FZK-Haus zeilengleich
(Dach 124,6 m², Grund 84,9 m²).
