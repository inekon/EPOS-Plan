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

Dieses Protokoll wird je Welle fortgeschrieben; **Stand: Welle C.**

## 1 Wellen

| Welle | Inhalt | Commits | Stand |
|---|---|---|---|
| A | Zonierung im Kern: Raumgrenzen, Beheizungsregeln und Zonen im IFC-Abbild (A1), formatfreie Zonierung (A2), Bauteilvorschlag und Schreibweg für mehrere Zonen (A3), Importproben (A4) | `d2a54a33`, `95fd838e`, `bc6b065e`, `dce58b72`; Merges `87782e97`, `ef1b2856` | umgesetzt und gepusht (26.09.2026) |
| B | Vorschlag mehrerer Zonen | in A3 aufgegangen | erledigt mit A |
| C | Zuordnungsdialog mit mehreren Zonen (Mehrzonenkonzept 6.4): Datenseite (C1), Dialog (C2), Durchgang bis zur Rechnung (C3), Rasterprobe (C4), Papiere (C5) | `bdc3ba04`, `85a80b74`, `f2576ac6`, `cddc61fb`, dazu der Papiercommit (C5); Merge `67ff538d` | umgesetzt (26.09.2026) |
| D | Zonengeometrie-Modell und 2D-Grundriss (E11, 6.7) | — | offen |
| E | Papiere und Wiki | — | Papiere mit Welle C; Wiki in dieser Sitzung auf Anweisung des Anwenders gestoppt |

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

## 2a Welle C — Zuordnungsdialog mit mehreren Zonen

**C1 — Datenseite** (`bdc3ba04`): `GebaeudeImportHuelle` bildet je Anfrage die Zonierung nach der gewählten
Regel und den Vorschlag darauf; `GebaeudeImportZonen` (`EPOS.UI.Daten`, plattformfrei) übersetzt beides in
die DTO des Dialogs — wählbare Regeln, Bilanz, schwerste Meldung, Obergrenze mit gröberer Regel, Zonen samt
Räumen, Flächen je Zone als Zeilen einer `Katalogliste` mit Befunden. Anfrage und Ergebnis tragen die Regel;
die Herkunft merkt sie bei mehreren Zonen in `Tab_Importquelle.Zonenregel`. 39 Texte in beiden `.resx`,
`ResourceDesigner` neu.

**C2 — Dialog** (`85a80b74`): Im Kopf die Klappliste der Regeln (nur die für das Gebäude gültigen,
vorbelegt nach M7), die Bilanz (Zonen, beheizte Fläche, beheiztes Volumen, Σ Außenfläche, Σ Trennfläche)
und ein Warnbanner mit der schwersten Meldung; über 50 Zonen der Knopf „Gröbere Regel übernehmen: …“ (M12).
Bei mehreren Zonen heißt der Schalter „Als Zonen mit Bauteilen übernehmen“; statt der Bauteiltabelle stehen
die Zonen (Name, Regel, Räume, Fläche, Volumen, beheizt, Hinweis; aufgeklappt die Räume mit Geschoss,
Fläche, Beheizungsregel und Beleg) und die Flächen je Zone als virtualisierte `Katalogliste` (Zone,
Bauteil, Art, Fläche, Azimut auf eine Nachkommastelle, Neigung, Randbedingung, Nachbarzone, U-Wert, Aufbau,
Herkunft, Befund) mit den Filtern „nur Fehler“, „nur ohne Gegenstück“, „nur ohne U-Wert“. Ohne Wahl und
unter einer Regel mit einer Zone bleibt der Einzonenweg unverändert.

**C3 — Durchgang** (`f2576ac6`): `ifc4_zonen.ifc` über den Importweg des Gebäudedialogs mit Z4 und Schalter,
vorbelegtem Editor und Speichern der Gebäudeliste — drei Zonen, Trennflächen auf die endgültigen
Nachbarzonen, Quelle mit Regel Z4, Räume auf ihre Zone gepaart, alles in dem Vorgang, der Projektkopie,
Herkunft und Baustoff-Zuordnungen schreibt; G6b rechnet das Gebäude.

**C4 — Rasterprobe** (`cddc61fb`): Seite `/gebaeudeimport?datei=ifc4_zonen.ifc&flaechen=2400` des Wirts (die Flächen der
Probe reihum vervielfacht), Fälle GI (1 088 × 624) und GJ (400 × 624): Zeile 46 px gleich `ItemSize`,
Rollbehälter die Hülle der Liste, keine Platzhalter, 4 Sichtbarkeitsmeldungen nach dem Rollen.

**C5 — Papiere:** dieses Protokoll, Statuszeile G6c, Konzept N1.57, Übergabe 2.1, Logbuch-Satz im
Update-Papier.

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

Mit **Welle C** (Zuordnungsdialog):

| # | Festlegung | Bezug |
|---|---|---|
| 9 | **Vorgabe im Dialog** ist die Vorgabe der Datei nach M7; eine Regel, die das Gebäude nicht trägt, gilt als Vorgabe. Neue Datei und anderes Gebäude setzen auf die Vorgabe zurück; eine andere Regel lässt Raumhaken, Handwerte und Baustoffe stehen | M7; Mehrzonenkonzept 6.4 |
| 10 | **Einzonenweg:** Trägt die Datei nur eine Regel, zeigt der Dialog keine Zonenelemente; ergibt die gewählte Regel eine Zone (Z5, X4 oder eine einzige Gruppe), ist es der Vorschlag aus G4b, zeilengleich | Auftrag Welle C |
| 11 | **Handänderungen an Zonen:** Zusammenlegen und Trennen trägt der Kern nicht und bietet der Dialog nicht an — der Hinweis unter den Zonen nennt das; der Haken „beheizt“ einer Zone stellt alle ihre Räume über die Raumhaken um | Mehrzonenkonzept 6.4 |
| 12 | **Bilanz:** beheizte Fläche und beheiztes Volumen aus den beheizten Zonen; Σ Außen- und Σ Trennfläche aus den Zeilen des Vorschlags (Öffnungen eingeschlossen, je Paar eine Seite), ohne Zeilen aus den Flächen der Zonierung (brutto, je Paar die größere Beschreibung) | Mehrzonenkonzept 6.4 |
| 13 | **Schwerste Meldung:** Fehler vor Warnungen; über der Obergrenze die Warnung mit dem Vorschlag, damit Banner und Knopf dasselbe sagen; unter den Warnungen geht `GRENZEN_ENTKOPPELT` vor | Mehrzonenkonzept 6.4, 6.5 |
| 14 | **Befunde der Flächen:** „Fehler“ heißt jeder Befund — ohne Gegenstück, ohne U-Wert (weder Wert noch Aufbau), Fläche geschätzt; eine Öffnung trägt die Befunde ihres Wirts. Weil die `Katalogliste` keine Zeilen färbt, steht der Befund als Spalte statt als rote bzw. gelbe Zeile; die drei Filter schränken die Zeilen vor der Liste ein | Mehrzonenkonzept 6.4, 6.6 |
| 15 | **Flächenliste:** immer die `Katalogliste`, die Zeile ist die Wahl (46 px), virtualisiert ab 120 Zeilen; Zonen-, Raum-, Baustoff- und Meldungslisten bleiben schlichte Tabellen | Auftrag Welle C; Hausregel W6-B-2 |
| 16 | **Quelle:** `Tab_Importquelle.Zonenregel` trägt die gewählte Regel nur, wenn mehrere Zonen mit Schalter übernommen werden; sonst die Regel des Profils (Z5 bzw. X4) | Datenaustauschkonzept 7 |

Zu Nr. 3: Mehrzonenkonzept 6.2 nennt im geometrischen Schritt einen Schwerpunktabstand kleiner als die
Bauteildicke; die Umsetzung fasst das mit 1 cm Spiel, einem Ersatzwert ohne Dicke und der Normalenprobe.
Zu Nr. 8: Mehrzonenkonzept 6.5 sieht Z4 ohne Raumgrenzen nur mit einer vom Anwender eingetragenen
Trenndecke vor; die Umsetzung lässt Z4 zu und warnt. Auch der Dialog der Welle C bietet die Eingabe der
Trenndecke nicht an — der Kern trägt sie nicht; das Banner nennt die Entkopplung (Nr. 13), die Vorgabe
bleibt Z5. Offen (Abschnitt 7).

Die G4b-Fälle, die den Einzonenweg prüfen, wählen die Regel „eine Zone je Gebäude“ seit Welle C
ausdrücklich, weil die Vorgabe jetzt je Geschoss ist (M7).

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

**Welle C** (nach dem Merge `67ff538d`):

- **Bau:** Kern-Filter, Windows-Schale, Wirt der Rasterprobe und `EPOS.Referenzlauf` je 0 Fehler.
- **Tests:** Kern 8 378 (1 übersprungen), UI 6 722, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27
  (1 übersprungen), alle grün.
- **Referenzlauf:** 14/14 PASS gegen `2026-09-26_R21_BhkwDeckung` (GESAMT: PASS); keine neue Basis, kein
  Referenzprojekt trägt importierte Zonen.
- **SQL-Dialekt-Prüfer:** 1 997 Texte, 0 Funde; Auslieferungsvorlage 38/38.
- **Rasterprobe:** 29 von 29 Fällen erfüllt, darunter GI und GJ.
- **Neue Tests:** `GebaeudeImportZonenHuelleTests` (10, Datenseite ohne Datenbank), `GebaeudeImportZonenDatenbankTests`
  (Durchgang), `GebaeudeImportZonenDialogTests` (10, bunit, samt echter Hülle); die G4b-Fälle wählen die
  Einzonenregel ausdrücklich.

## 6 Aufwand

Welle A in vier Commits am 26.09.2026, Welle C in fünf. Die Bezifferung der Stufe samt X1…X3 folgt mit dem
Abschluss.

## 7 Offen

- **Welle D** (Zonengeometrie-Modell, 2D-Grundriss E11) als nächster Schritt; **Welle E** (Wiki-Quelle
  „Gebäudeimport“ mit Abschnitt „Mehrere Zonen“) — die Wiki-Arbeit ist in dieser Sitzung auf Anweisung des
  Anwenders gestoppt; der Logbuch-Satz steht im Update-Papier unter 1.2.0.5.
- Die Proben 13–16 und 18 mit den lizenzgeklärten Dateien (M10), mit ihnen die Messungen aus der Übergabe
  2.1 (`IfcSpatialZone`, `ParentBoundary`, Profiltyp der zwei `IfcSurfaceOfLinearExtrusion`).
- Die Trenndecke ohne Raumgrenzen (Festlegung 8 gegen Mehrzonenkonzept 6.5): weder Kern noch Dialog tragen
  ihre Eingabe; Z4 bleibt dort wählbar mit Warnung.
- Zusammenlegen und Trennen von Zonen von Hand (Festlegung 11) — nur mit einer Erweiterung des Kerns.
- Der Keller-Befund (Abschnitt 4) für G6d.
- Windows-Sichtabnahme mit Welle C; kein iOS-Lauf ohne ausdrücklichen Zuruf des Anwenders.
