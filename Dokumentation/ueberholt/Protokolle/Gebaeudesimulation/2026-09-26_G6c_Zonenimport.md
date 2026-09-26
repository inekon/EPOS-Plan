# Protokoll G6c — Zonenimport mit mehreren Zonen aus IFC und gbXML (26.09.2026)

**Auftrag.** Stufe G6c der Gebäudesimulation, beauftragt mit **E50** (Anwender, 26.09.2026, Konzept
N1.57) im Wortlaut „Kleine Nacharbeiten an G4b, dann G6c, Zonenimport mit mehreren Zonen aus IFC/gbXML,
U-Wert-Vorgaben für Neubauten ab 2021“. E50 entscheidet M7 (Zonen je Geschoss, Rückfall auf eine Zone ohne
Raumgrenzen), M8 (Mindestgröße max(2 m², 2 %) mit Zuschlag zum Nachbarn), M12 (50 Zonen als Vorgabe,
Warnung mit Rückfrage) und M13 (Nachbarschaften vollständig über die Geometrie) nach Empfehlung. Nach D16
(E27) gehören die gbXML-Regeln X1…X3 dazu. Grundlage sind Mehrzonenkonzept 6 und 9 und
Datenaustauschkonzept 3.3; der Wellenplan steht in der
[Übergabe](../../../aktuell/Gebaeudesimulation/2026-09-26_Uebergabe_G6c_Katalog_M_A.md) 2.1. Nicht in G6c:
das Referenzprojekt mit Zonen und der unbeheizte Keller gegen Erdreich (G6d).

Dieses Protokoll wird je Welle fortgeschrieben; **Stand: Welle A.**

## 1 Wellen

| Welle | Inhalt | Commits | Stand |
|---|---|---|---|
| A | Zonierung im Kern: Raumgrenzen, Beheizungsregeln und Zonen im IFC-Abbild (A1), formatfreie Zonierung (A2), Bauteilvorschlag und Schreibweg für mehrere Zonen (A3), Importproben (A4) | `d2a54a33`, `95fd838e`, `bc6b065e`, `dce58b72`; Merges `87782e97`, `ef1b2856` | umgesetzt und gepusht (26.09.2026) |
| B | Vorschlag mehrerer Zonen | in A3 aufgegangen | erledigt mit A |
| C | Zuordnungsdialog mit mehreren Zonen (Mehrzonenkonzept 6.4) | — | in Arbeit |
| D | Zonengeometrie-Modell und 2D-Grundriss (E11, 6.7) | — | offen |
| E | Papiere und Wiki | — | offen |

## 2 Welle A — Zonierung im Kern

**A1 — Raumgrenzen, Beheizungsregeln und Zonen im IFC-Abbild** (`d2a54a33`):

- `AbbildGrenze` trägt Raum, Lage, „virtuell“, das Gegenstück der Datei, Fläche, Ausschnitt,
  Schwerpunkt und Normale in Weltkoordinaten.
- `AbbildRaum` trägt Beheizungsregel, Klassifikation, Zonenname und Mehrfachzuordnung.
- `IfcGrenzgeometrie` rechnet die Fläche ohne Geometriekern: `IfcCurveBoundedPlane` mit Polyline,
  CompositeCurve oder IndexedPolyCurve ohne Bögen; `IfcSurfaceOfLinearExtrusion` mit gerader Kurve;
  Ebenheit auf 1 mm.
- Beheizung nach B1–B6; B5 wertet die Raumgrenzen aus; neuer Wert `BeheiztQuelle.Lage`.
- Z1 aus `IfcSpatialZone` mit `PredefinedType = THERMAL`, sonst aus `IfcZone`, entschachtelt; ein Raum
  in mehreren Zonen kommt in keine (`RAUM_MEHRFACH`).

**A2 — `GebaeudeZonierung`, formatfrei** (`95fd838e`):

- Regeln Z1–Z5 und X1–X4, Vorgabe nach M7.
- Flächen je Zone: außen, Trennfläche, innere Masse, unbeheizt, Gebäudetrennung.
- Paarbildung nach M13, Gegenprobe der Trennflächen ab 2 %, Mindestgröße nach M8, Obergrenze nach M12
  mit Vorschlag einer gröberen Regel.
- `VIRTUAL`-Grenzen zwischen Zonen als Hinweis auf Luftaustausch; der gbXML-Leser liest Zonen- und
  Geschossnamen.

**A3 — `GebaeudeBauteilvorschlag.BildenMitZonen`** (`bc6b065e`):

- Trennflächen als `ZONE` mit `ID_Nachbarzone`; Fenster und Türen in der Zone ihres Teils; Aufbauten,
  Namensabgleich und U-Vorgaben wie in G4b.
- Innere Masse nach E45; über 50 Zonen, eine Zone ohne Nutzfläche oder eine abweichende Flächensumme
  werden benannt abgelehnt.
- `VorschlagSchreiben` schreibt N Zonen in einem Vorgang; unter Z5 bzw. X4 bleibt der G4b-Vorschlag
  zeilengleich.

**A4 — Importproben** (`dce58b72`) unter `Referenzlaeufe/Importproben/`:

- `ifc4_zonen.ifc`: drei Geschosse, Polygone, Gegenstück, fehlendes Paar, geschachtelte und mehrfache
  `IfcZone`, Klassifikation, B3 und B5, ein zu kleiner Raum.
- `gbxml_zonen_viele.xml`: 60 Räume, über 50 Zonen.

## 3 Festlegungen der Umsetzung — benannt, nicht entschieden

Wo Auftrag und Papiere schwiegen, hat die Umsetzung festgelegt; der Orchestrator hat die Welle
abgenommen. Die Liste steht auch im Konzept N1.57; Widerspruch ist möglich und würde ein eigener
Entscheid.

| # | Festlegung | Bezug |
|---|---|---|
| 1 | **Gebäudegrundfläche für M8** = Σ der Raumflächen aller Räume; damit können höchstens 50 Zonen die Mindestgröße erfüllen, passend zu M12 | Mehrzonenkonzept 6.1 |
| 2 | **Beheizung trennt Zonen:** Unbeheizte Räume einer Gruppe bilden eine eigene, frei schwingende Zone; eine zu kleine Zone geht nur an einen Nachbarn gleicher Beheizung | Mehrzonenkonzept 6.1, 2.5 |
| 3 | **Paarbildung:** eindeutig bei genau zwei Zonen am Bauteil, sonst das Gegenstück der Datei, dann die Geometrie — Schwerpunktabstand ≤ Dicke + 1 cm (ohne Dicke 0,6 m), Normalen entgegengesetzt, genau ein Kandidat | Mehrzonenkonzept 6.2, Schritte (1)–(4) |
| 4 | **Flächen ohne Polygone:** Ein Außenbauteil an mehreren Zonen wird nach der Zahl der Grenzen geteilt, ein mehrdeutiges Innenbauteil zu je 2/n; Meldung `FLAECHE_AUFGETEILT` | Mehrzonenkonzept 6.2 |
| 5 | **Öffnungsabzug:** Innenränder vor der Öffnungssumme, höchstens einmal | Mehrzonenkonzept 6.2, 6.6 (Überlappung) |
| 6 | **Trennflächen:** Die beheizte Zone führt vor der unbeheizten, sonst die mit dem kleineren Rang; gerechnet wird mit der größeren Beschreibung | Mehrzonenkonzept 6.6 (Bilanzlücke) |
| 7 | **Innenweg** je Gebäude entschieden; das Band gilt gegen die Σ der Zonenflächen | E45 (Konzept N1.49); Mehrzonenkonzept 5.3 |
| 8 | **Ohne Raumgrenzen** sind nur Z4 und Z5 wählbar; Z4 dann mit der Warnung `GRENZEN_ENTKOPPELT`, die Bauteile gehen an die Zone ihres Geschosses | Mehrzonenkonzept 6.5 |

Zu Nr. 3: Mehrzonenkonzept 6.2 nennt im geometrischen Schritt einen Schwerpunktabstand kleiner als die
Bauteildicke; die Umsetzung fasst das mit 1 cm Spiel, einem Ersatzwert ohne Dicke und der Normalenprobe.
Zu Nr. 8: Mehrzonenkonzept 6.5 sieht Z4 ohne Raumgrenzen nur mit einer vom Anwender eingetragenen
Trenndecke vor; die Umsetzung lässt Z4 zu und warnt.

## 4 Befund: unbeheizter Keller gegen Erdreich

Im gbXML-Haus `gbxml_haus_si.xml` lehnt die Probe unter X2 den Vorschlag benannt ab (`BAUTEILWEG`,
„Keller:“). Die unbeheizte Kellerzone hat nur Beton ohne Dämmung gegen Erdreich, und VDI 6007 Blatt 1
Gl. (28) hat dafür keinen Setzwert (R_Rest < 0). Im Einzonenweg liegt der Keller außerhalb der Zone und
der Fall tritt nicht auf. Die Grenze liegt im Rechenweg aus G3 und G6b, nicht im Import. Für G6d
(unbeheizter Keller mit Erdreich) wichtig: Dort braucht es vermutlich eine Erdreichschicht.

## 5 Nachweise

- **Bau:** Kern-Filter, Windows-Schale und `EPOS.Referenzlauf` je 0 Fehler.
- **Tests:** Kern 8 342 (1 übersprungen), UI 6 681, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27
  (1 übersprungen), Auslieferungsvorlage 38.
- **Referenzlauf:** 14/14 PASS gegen `2026-09-26_R21_BhkwDeckung`; keine neue Basis, kein
  Referenzprojekt trägt importierte Zonen.
- **SQL-Dialekt-Prüfer:** 1 998 Texte, 0 Funde.
- **Durchgang:** Das Probenhaus nach Z4 geht in Projekt 1045; G6b rechnet die drei Zonen.
- **Nach dem Merge `ef1b2856`:** Build 0 Fehler, betroffene Tests 565/565 grün.

**Nicht abgedeckt:** Die Proben 13–16 und 18 des Mehrzonenkonzepts 8.2 brauchen FZK-Haus und DigitalHub;
beide liegen nicht im Repositorium, die Lizenz ist nach M10 offen. Die eigenen Proben aus A4 halten die
Regeln ersatzweise.

## 6 Aufwand

Welle A in vier Commits am 26.09.2026. Die Bezifferung der Stufe samt X1…X3 folgt mit dem Abschluss
(Welle E).

## 7 Offen

- **Welle C** — Zuordnungsdialog mit mehreren Zonen (in Arbeit), danach **Welle D** (Zonengeometrie-Modell,
  2D-Grundriss) und **Welle E** (Papiere, Wiki-Quelle „Gebäudeimport“, Logbuch-Entwurf).
- Die Proben 13–16 und 18 mit den lizenzgeklärten Dateien (M10), mit ihnen die Messungen aus der Übergabe
  2.1 (`IfcSpatialZone`, `ParentBoundary`, Profiltyp der zwei `IfcSurfaceOfLinearExtrusion`).
- Die Trenndecke ohne Raumgrenzen (Festlegung 8 gegen Mehrzonenkonzept 6.5) im Zuordnungsdialog.
- Der Keller-Befund (Abschnitt 4) für G6d.
- Windows-Sichtabnahme mit Welle C; kein iOS-Lauf ohne ausdrücklichen Zuruf des Anwenders.
