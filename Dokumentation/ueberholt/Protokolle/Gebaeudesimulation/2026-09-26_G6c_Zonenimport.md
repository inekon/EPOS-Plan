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

Dieses Protokoll wird je Welle fortgeschrieben; **Stand: Welle D.**

## 1 Wellen

| Welle | Inhalt | Commits | Stand |
|---|---|---|---|
| A | Zonierung im Kern: Raumgrenzen, Beheizungsregeln und Zonen im IFC-Abbild (A1), formatfreie Zonierung (A2), Bauteilvorschlag und Schreibweg für mehrere Zonen (A3), Importproben (A4) | `d2a54a33`, `95fd838e`, `bc6b065e`, `dce58b72`; Merges `87782e97`, `ef1b2856` | umgesetzt und gepusht (26.09.2026) |
| B | Vorschlag mehrerer Zonen | in A3 aufgegangen | erledigt mit A |
| C | Zuordnungsdialog mit mehreren Zonen (Mehrzonenkonzept 6.4): Datenseite (C1), Dialog (C2), Durchgang bis zur Rechnung (C3), Rasterprobe (C4), Papiere (C5) | `bdc3ba04`, `85a80b74`, `f2576ac6`, `cddc61fb`, dazu der Papiercommit (C5); Merge `67ff538d` | umgesetzt (26.09.2026) |
| D | Zonengeometrie-Modell und 2D-Grundriss (E11, Mehrzonenkonzept 6.7): Kern mit Zonengeometrie, Randpunkten und Zuordnung von Hand (D1), Grundrissansicht und Klickzuordnung im Dialog (D2), Merge, Gate und Papiere (D3) | `b66df135`, `c9c7c99f`, `dc107f1d`, `e4c62761`, dazu der Papiercommit (D3); Merges `3ac80535`, `2fb0ce71` | umgesetzt (26.09.2026) |
| E | Wiki-Quelle „Gebäudeimport“ mit Abschnitt „Mehrere Zonen“ samt Grundriss | — | offen; die Papiere kamen mit den Wellen C und D, die Wiki-Arbeit ist in dieser Sitzung auf Anweisung des Anwenders gestoppt |

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

## 2b Welle D — Zonengeometrie, Grundriss und Zuordnung von Hand

**D1 — Kern und Hülle** (`b66df135`, `c9c7c99f`):

- **Randpunkte:** `IfcGrenzgeometrie` gibt je Raumgrenze den Außenrand als Ring in Weltkoordinaten
  (Meter, Reihenfolge der Datei, nur mit Raumplatzierung) in `AbbildGrenze.RandpunkteM`; der gbXML-Leser
  liest den `PolyLoop` jeder Fläche und Öffnung leise in `AbbildBauteil.RandpunkteM` und die Höhenlage
  der Geschosse aus `BuildingStorey/Level` in `AbbildRaum.GeschossLageM`. Flächen, Azimute, Neigungen und
  die Paarbildung bleiben unverändert.
- **`Zonengeometrie`** (`EPOS.Kern/Allgemein/Simulation/Gebaeude/Zonengeometrie.cs`): der formatfreie
  Eingang `Umrisseingang` mit `Umrissraum`, `Umrissseite`, `Umrisszone` und `Umrissgeschoss`; die
  Fabrikwege `AusRaumgrenzen(Umrisseingang)` und `AusFlaechen(Umrisseingang)`; heraus je Raum ein
  `Raumumriss` (Polygone als `Umrisspolygon`, Kanten als `Umrisskante` mit ihren `Grenzverweis`en, Boden
  und Decke am Raum, `OhneKante`), je Zone und Geschoss ein `Zonenumriss`, dazu `Geschossangabe`,
  `Zonenangabe`, die Herkunft als `Geometrieherkunft` und `Umrissherleitung` und die Meldungen `ZGEO_*`;
  `Ebenheit` mit `EBEN_TOLERANZ_M` (1 mm) für beide Formate.
- **`GebaeudeGrundriss`** (`EPOS.Kern/Allgemein/Import/Gebaeude/`): bildet aus `GebaeudeAbbild` und
  `GebaeudeZonierung` den Eingang (`Eingang`) und die Geometrie (`Bilden`) — Räume mit Zone und wirksamer
  Beheizung, Seiten je Raumgrenze bzw. Nachbar, Stellung, Himmelsrichtung und Fläche je Seite.
- **Zuordnung von Hand in `GebaeudeZonierung`:** `Raumumhaengung(Raum, Zielzone)` als geordnete Liste,
  `Umhaengen(raum, zielzone)` gibt eine neue Zonierung; `RaumBeheizt`, `Handgeaendert` je Zone und
  `Handzuordnung`; die Mindestgröße ist geteilt in den Zuschlag (M8) und die Meldung nach der Hand.
- **Hülle:** `GebaeudeImportHuelle` bildet je Anfrage Zonierung samt Zuordnungen, Vorschlag und
  Zonengeometrie; `GebaeudeImportAnsicht` (`EPOS.UI.Daten/Bedarf/`) übersetzt die Geometrie in das DTO
  `GebaeudeAnsichtDaten` (`EPOS.UI/Dialoge/Bedarf/`); Anfrage und Ergebnis tragen die Zuordnungen
  (`GebaeudeRaumumhaengung`, `MitUmhaengung`), die Zonenzeile Schlüssel und „von Hand“, der Stand die
  Ansicht.
- 21 Texte in beiden `.resx`: `IMP_IFC_PROT_*` und `IMP_GBXML_PROT_*` zu umgehängtem und abgetrenntem Raum,
  entfallener Zone, den vier Ablehnungen und der Handzone unter der Mindestgröße;
  `IMP_GBXML_PROT_UMRISS_NICHT_EBEN`; `ZGEO_SCHEMATISCH`, `ZGEO_SEITENVERHAELTNIS`, `ZGEO_QUADRAT`,
  `ZGEO_OHNE_FLAECHE`. `ResourceDesigner` neu.

**D2 — Oberfläche** (`dc107f1d`, `e4c62761`):

- **`GebaeudeAnsicht`** (`EPOS.UI/Bausteine/GebaeudeAnsicht.razor`, Zeichenrechnung in
  `GebaeudeAnsichtZeichnung.cs`): je Geschoss die Räume als SVG-Polygone in Metern, Nord oben, Farbe je
  Zone, ohne Zone grau; Name und Fläche im Raum; Legende und Hinweise; Reiter „Grundriss | Körper“ und die
  Geschosswahl. Parameter `Daten`, `GewaehlteZone`, `RaumUmgehaengt`, `Geschoss`/`GeschossChanged`,
  `Aktiv`, `Texte`; das DTO trägt dazu `GeschossOderVorgabe`, `Zone`, `Raum` und das Textbündel
  `GebaeudeAnsichtTexte`.
- **Stilblatt** `epos-ui.css`: Token `--epos-grundriss-zone-0` bis `-9`, `--epos-grundriss-ohnezone` und
  `--epos-grundriss-rand`, Regeln `.epos-gebansicht-*` samt Kontrastmodus, Andockung
  `.epos-gebimport-zonenblock` und `.epos-gebimport-raumweg`.
- **Wirt `GebaeudeImportDialog`:** Zonenliste und Grundriss als ein Block, im schmalen Fenster der Grundriss
  darunter; ohne Zonenliste ein eigener Abschnitt „Grundriss“. Im Kopf „Räume zuordnen zu“ mit den Zonen und
  „als eigene Zone“; Klick, Tastatur oder Raumwahl mit „Umhängen“ ordnen über den Kern neu zu; Ablehnungen
  stehen als Banner, bei ungleicher Beheizung mit dem Raumhaken als Ausweg; ein Regelwechsel mit
  Zuordnungen fragt zurück. `GebaeudeZonierungDaten.Ablehnungen` (gefüllt in `GebaeudeImportZonen`) trägt
  die Ablehnungen des Kerns, das Ergebnis die `Umhaengungen`.
- 34 Texte in beiden `.resx` (22 `GIMP_ANS_*` der Ansicht, 12 `GIMP_DLG_*` des Wirts); der Zonenhinweis
  unter der Liste (`GIMP_DLG_ZONEN_HINWEIS`) verweist auf den Grundriss. `ResourceDesigner` neu.

**D3 — Merge, Gate und Papiere:** zwei Merges von `origin/ios_migration_september` ohne Textkonflikt
(`3ac80535` auf `0a483bd4`, `2fb0ce71` auf `ab7b0979`); beide `.resx` ohne Doppelnamen mit je 12 745
Schlüsseln, `ResourceDesigner` ohne Abweichung; die Testdatenbank ist die Fassung von origin
(`09b6c523…`), Welle D hat keinen Schemaschritt. Papiere: dieses Protokoll (1, 2b, 3, 5, 6, 7), Konzept
N1.57, Statusdatei, Softwarearchitektur (1.2, 1.3, 3.2, 3.4, 3.8, 5), Mehrzonenkonzept 6.7 und 9,
Datenaustauschkonzept 14.1, Register (M8, M12), Übergabe 2.1, der Logbuch-Satz im Update-Papier unter
1.2.0.5 und die Statuszeile in `Status_iOS_Migration.md`. Was offen bleibt, steht in Abschnitt 7.

**Sichtabnahme unter Windows** (steht aus) mit `Referenzlaeufe/Importproben/ifc4_zonen.ifc` über
„Importieren (gbXML, IFC)…“ im Gebäudedialog:

1. Import → Regel Z4, drei Zonen; der Grundriss steht neben der Zonenliste mit den Reitern
   „Grundriss | Körper“ und „Kellergeschoss (schematisch) | Erdgeschoss | Obergeschoss“.
2. Kellergeschoss vorn: Lager gestrichelt, die Zeile „Schematisch: …“ über dem Bild, Legende
   „Kellergeschoss · schematisch · unbeheizt“, darunter die Hinweisliste.
3. „Körper“: Tooltip mit dem Grund; ein Klick meldet eine leise Zeile, die Ansicht bleibt.
4. Erdgeschoss: Wohnen und Küche in einer Farbe, Name und Fläche im Raum, Nord oben, Legende „aus
   Raumgrenzen“.
5. Zielzone „als eigene Zone“, Küche antippen → vier Zonen, Bilanz 4, neue Farbe, Legende „von Hand“;
   Zielzone „Erdgeschoss“, Küche erneut antippen → die Handzone entfällt.
6. Zielzone „Erdgeschoss“, Kellergeschoss, Lager antippen → Banner zur Beheizung mit dem Haken „Beheizt:
   Lager“; Haken setzen → Lager ins Erdgeschoss, das Kellergeschoss entfällt, die Raumliste zeigt
   „umgestellt“.
7. Tastatur: Tab in den Grundriss, Fokusrand, Enter und Leertaste ordnen zu; die Raumwahl mit „Umhängen“
   tut dasselbe.
8. Regelwechsel mit Zuordnungen → Rückfrage, „Nein“ vorbelegt (Nein oder Esc: alles bleibt; Ja: neue
   Regel, die Zuordnungen sind weg).
9. Fenster schmal: der Grundriss rückt unter die Zonenliste, die Flächenliste rollt sauber.
10. OK übernimmt (gespeichert wird erst mit der Gebäudeliste), Abbrechen verwirft.
11. Kontrastdesign: Flächen in der Systemfarbe mit Rand, gestrichelt bleibt gestrichelt, der Fokus in der
    Hervorhebungsfarbe.
12. Englisch: „Floor plan | Solids“, „Assign spaces to“, „Reassign“.

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
| 11 | **Handänderungen an Zonen:** Zusammenlegen und Trennen trägt der Kern nicht und bietet der Dialog nicht an — der Hinweis unter den Zonen nennt das; der Haken „beheizt“ einer Zone stellt alle ihre Räume über die Raumhaken um. **Überholt mit Welle D** (Nr. 27–29, 40–48): Räume wandern einzeln in eine andere Zone gleicher Beheizung oder werden eigene Zone, eine leer gewordene Zone entfällt; der Hinweis verweist auf den Grundriss | Mehrzonenkonzept 6.4 |
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

Mit **Welle D, Kern und Hülle** (D1):

| # | Festlegung | Bezug |
|---|---|---|
| 17 | **Ort und Fabrikwege:** `Zonengeometrie` in `Simulation/Gebaeude/` mit `AusRaumgrenzen(Umrisseingang)` und `AusFlaechen(Umrisseingang)` über einen formatfreien Eingang; `GebaeudeGrundriss` in `Import/Gebaeude/` bildet den Eingang aus Abbild und Zonierung | Softwarearchitektur 1.2, 1.3 |
| 18 | **Randpunkte IFC:** der Außenrand der Raumgrenze in Weltkoordinaten (Meter, Reihenfolge der Datei), nur mit Raumplatzierung; Innenränder nicht | Mehrzonenkonzept 6.2, 6.7 |
| 19 | **Randpunkte gbXML:** der `PolyLoop` je Fläche und Öffnung, eben auf 1 mm, sonst die Info `UMRISS_NICHT_EBEN` und kein Ring; die Höhenlage aus `Level` ordnet nur die Geschosse | Datenaustauschkonzept 3.3, 14.1 |
| 20 | **Umriss je Raum** aus den Bodengrenzen, ohne auswertbare aus den Deckengrenzen, in die Grundrissebene projiziert; ein Polygon je Grenze ohne Vereinigung, gegen den Uhrzeigersinn; eine Zone ist die Folge der Polygone ihrer Räume | Mehrzonenkonzept 6.7 |
| 21 | **Stellung einer Grenze** nach der Normalen (senkrechte Komponente höchstens 0,5: Wand, sonst Boden oder Decke), sonst nach Art, Neigung, Sicht der Datei und Höhenlage der Geschosse; bleibt sie offen, ist sie unbestimmt und am Raum benannt | Datenaustauschkonzept 14.1 |
| 22 | **Kanten:** Eine Wand steht an der Kante, auf der ihr projizierter Ring liegt (Abstand höchstens 5 cm, Überdeckung mehr als 1 cm); was nirgends passt, steht in `OhneKante`; der Azimut einer Kante folgt derselben Nordregel wie die Bauteile | Datenaustauschkonzept 14.1 |
| 23 | **Rechteckersatz je Raum,** nicht je Zone: h = V/A, l = A_NS/(2h), b = A_OW/(2h) aus den Bruttoflächen je Sektor; weicht l·b um mehr als 10 % von der Fläche ab, gilt die Fläche mit dem Seitenverhältnis; ohne Wände einer Richtung oder ohne Höhe das Quadrat; ohne Fläche kein Umriss (`ZGEO_OHNE_FLAECHE`) | Mehrzonenkonzept 6.7; Datenaustauschkonzept 5.5, 14.1 |
| 24 | **Reihung je Geschoss:** die Rechtecke 2 m rechts neben den Umrissen aus Raumgrenzen, je Zone in der Reihenfolge der Datei, in Zeilen auf 3 : 2 mit 0,5 m Abstand; Nord oben, die Kanten Süd, Ost, Nord und West tragen die Wände ihres Sektors | Datenaustauschkonzept 5.5, 14.1 |
| 25 | **Herkunft je Raum, Zone und Geschoss** (`Geometrieherkunft` samt `Umrissherleitung`); Geschosse nach Höhenlage, dann Name, dann Kennung, ohne Geschoss zuletzt | Datenaustauschkonzept 14.4 Nr. 4 |
| 26 | **Einzonenfall:** dieselben Polygone; Räume außerhalb der Zonen (Stelle −1, unter Z5 und X4 die unbeheizten) stehen grau | Mehrzonenkonzept 6.7 |
| 27 | **Umhängen:** `Raumumhaengung(Raum, Zielzone)` als geordnete Liste, angewandt nach dem Zuschlag M8; Ziel ist der Zonenschlüssel, ohne Ziel entsteht eine neue Zone (Schlüssel aus `HAND`, Raum und Beheizung); eine leer gewordene Zone entfällt, Seiten, Trennflächen und Gegenprobe werden neu gebildet | Mehrzonenkonzept 6.4 |
| 28 | **Nur gleiche Beheizung:** Ein Raum wandert nur in eine Zone gleicher Beheizung (`UMHAENGEN_BEHEIZUNG`); unbekannter Raum und unbekannte Zone werden benannt übergangen; unter Z5 und X4 wird nicht umgehängt (`UMHAENGEN_EINZONIG`) | Nr. 2 |
| 29 | **M8 und M12 nach der Hand:** Eine Handzone unter der Mindestgröße bleibt stehen und wird mit `ZONE_ZU_KLEIN_HAND` gemeldet, nicht zugeschlagen; über 50 Zonen gilt die bestehende Warnung mit dem Vorschlag der gröberen Regel, der Vorschlag lehnt darüber benannt ab | M8, M12 |
| 30 | **Persistenz:** Die Zuordnungen reisen in Anfrage und Ergebnis, gespeichert wird nur über die Gebäudeliste; `Tab_Importquelle.Zonenregel` bleibt die Regel, die Zuordnung von Hand steht in den Raumpaarungen — kein Schemaschritt | Datenaustauschkonzept 7; Nr. 16 |

Mit **Welle D, Oberfläche** (D2):

| # | Festlegung | Bezug |
|---|---|---|
| 31 | **Baustein ohne Maske:** `GebaeudeAnsicht` liegt unter `EPOS.UI/Bausteine/`, ohne Maskenschlüssel, ohne Katalogeintrag und ohne Eingabefeld; die KI-Wache prüft nur Dialoge und Seiten, ihr Wirt `GebaeudeImportDialog` steht in `KiDialogAusnahmen` | Softwarearchitektur 3.2, 3.8 |
| 32 | **Reiter** „Grundriss \| Körper“: „Körper“ ist weich gesperrt und nennt den Grund („kommt mit G7b“) als Tooltip und nach einem Klick als leise Zeile; die Geschosswahl steht als zweite Reiterzeile, bindbar über `Geschoss`/`GeschossChanged` | E11; Mehrzonenkonzept 6.7 |
| 33 | **Vorbelegung** ist das unterste Geschoss mit einem Umriss, ohne jeden Umriss das unterste (`GeschossOderVorgabe`); eine unbekannte Wahl fällt auf die Vorbelegung zurück; Ansicht und Raumwahl fragen dieselbe Stelle | Mehrzonenkonzept 6.7 |
| 34 | **Farben:** zehn Token `--epos-grundriss-zone-0…9` nach der Stelle der Zone modulo zehn, Kontrast zur Schrift mindestens 6,8 : 1; ohne Zone grau (`--epos-grundriss-ohnezone`); im Kontrastmodus `Canvas`/`CanvasText` mit Rand | Mehrzonenkonzept 6.7 |
| 35 | **Ausschnitt:** `viewBox` = Ausdehnung des Geschosses mit einem Rand von 4 % der größeren Seite, jede Seite mindestens 1 m; y gespiegelt (Nord oben); Zahlen invariant mit drei Nachkommastellen, nie „-0“ | Softwarearchitektur 1.7 (Determinismus) |
| 36 | **Strich:** Rand 1 px mit `vector-effect`; schematische Räume gestrichelt; Räume der Zielzone mit kräftigerem Rand | Mehrzonenkonzept 6.7 |
| 37 | **Beschriftung:** Name und Fläche im Schwerpunkt des größten Polygons eines Raums, Schrift 2,8 % der größeren Seite, nur wenn der Textkasten ganz im Polygon liegt; sonst tragen Kurztext und Beschreibung den Raum | — |
| 38 | **„schematisch“ an vier Stellen:** am Reiter des Geschosses, als Zeile über dem Bild, je Raum (gestrichelt, in Kurztext und Beschreibung) und in der Legende — gelesen aus dem Wert der Herkunft, nie aus einem Anzeigetext | Datenaustauschkonzept 14.4 Nr. 1 und 4 |
| 39 | **Legende:** nur die Zonen des gewählten Geschosses, dazu „ohne Zone“ und „schematisch“, wo sie vorkommen; die Zielzone fett; die Marken „unbeheizt“ und „von Hand“ | Mehrzonenkonzept 6.7 |
| 40 | **Klick** nur mit Umhängbarkeit, `Aktiv` und Rückruf; Enter und Leertaste wirken wie der Klick; gemeldet werden die Raumkennung und `GewaehlteZone` (`null` = als eigene Zone); sonst ist das Bild reine Anzeige mit Hinweis | Mehrzonenkonzept 6.4, 6.7 |
| 41 | **Andockung im Wirt:** Zonenliste und Grundriss als ein Block mit `flex-wrap`, ohne Medienabfrage — im schmalen Fenster steht der Grundriss unter der Liste; die Zonenliste behält Spalten und Zeilen, „von Hand“ zeigt allein die Legende | Hausregel W6-B-2 |
| 42 | **Ohne Zonenliste** (eine Zone) steht der Grundriss als eigener Abschnitt „Grundriss“, nur zur Anzeige | Nr. 10 |
| 43 | **Zielzone** „Räume zuordnen zu“: die Zonen in der Rangfolge und „als eigene Zone“, vorgewählt die erste Zone; gesteuert wird über den Schlüssel, nie über den Namen | Mehrzonenkonzept 6.4 |
| 44 | **Ablauf:** Klick, Tastatur oder Raumwahl reihen die Zuordnung per `MitUmhaengung` hinter die bisherigen, der Kern bildet Zonenliste, Bilanz und Grundriss neu; „liegt schon dort“ fragt den Kern nicht | Nr. 27 |
| 45 | **Ablehnungen des Kerns** (unbekannter Raum, unbekannte Zone, Beheizung, eine Zone) stehen als Warnbanner am Grundriss (`GebaeudeZonierungDaten.Ablehnungen`); die Zuordnung fällt heraus und der Stand wird ohne sie gebildet — nie still entfernt | Nr. 28 |
| 46 | **Ausweg bei ungleicher Beheizung:** der Raumhaken „Beheizt: …“ des Raums am Banner (`data-kennung`); erst sein Umstellen setzt `BeheiztUebersteuert`, danach folgt die Zuordnung | Nr. 28; Nr. 11 |
| 47 | **Regelwechsel mit Zuordnungen** fragt zurück (`Rueckfrage`, Vorgabe „Nein“, Esc wie „Nein“; „Nein“ stellt die Klappliste zurück); der Knopf der gröberen Regel geht denselben Weg; neue Datei und anderes Gebäude leeren ohne Frage | Nr. 9 |
| 48 | **Weg ohne Grundriss:** Raumwahl „Name – Zone“ (Räume ohne Umriss mit „(ohne Umriss)“) und Knopf „Umhängen“ — für die Tastatur und für Räume ohne Umriss | Mehrzonenkonzept 6.4 |
| 49 | **Ergebnis:** `Umhaengungen` sind die wirksamen Zuordnungen, `null` ohne solche; der Zonenhinweis unter der Liste verweist auf den Grundriss | Nr. 30 |

Zu Nr. 17: Die Softwarearchitektur 1.3 nannte `AusRaumgrenzen(raumgrenzen, zonen)` und
`AusFlaechen(zonen, bauteile)` und kannte `GebaeudeGrundriss`, `GebaeudeImportAnsicht` und
`GebaeudeAnsichtDaten` nicht; beide Wege nehmen denselben formatfreien Eingang, weil Raumgrenzen und
Zonen aus zwei Formaten kommen und `GebaeudeGrundriss` den Formatteil trägt — nachgezogen mit D3. Zu
Nr. 23 und 24: Mehrzonenkonzept 6.7 und Datenaustauschkonzept 14.1 sprachen vom Rechteck und von der
Reihung je Zone; umgesetzt ist beides je Raum, damit jeder Raum anklickbar bleibt. Das Aneinanderlegen von
Zonen mit gemeinsamer Trennfläche (Datenaustauschkonzept 5.5, Punkt 3) bleibt G7b. Zu Nr. 22: Ein
`Grenzverweis` trägt das Gegenstück der Datei, nicht das nach M13 gebildete Paar; G7b zieht das nach.
Zu Nr. 41: Eine Marke „von Hand“ in der Zonenliste verlangte eine weitere Spalte oder eine höhere Zeile;
D3 lässt es bei der Legende des Grundrisses — Widerspruch wäre ein eigener Entscheid.

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

**Welle D** (nach den Merges `3ac80535` und `2fb0ce71`):

- **Bau:** Kern-Filter, Windows-Schale (Debug x64) und `EPOS.Referenzlauf` je 0 Fehler.
- **Tests:** Kern 8 482 (1 übersprungen), UI 6 793, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27
  (1 übersprungen), alle grün.
- **Referenzlauf:** 15/15 PASS gegen `2026-09-26_R22_Solarthermie` (GESAMT: PASS, 4 899 465 Werte),
  460/460 CSV byte-gleich; keine neue Basis, kein Referenzprojekt trägt importierte Zonen.
- **Ressourcen:** beide `.resx` ohne Doppelnamen, `ResourceDesigner` ohne Abweichung; keine neue
  SQL-Anweisung, der SQL-Dialekt-Prüfer hatte keinen Anlass.
- **Gate der Welle D2** (vor den Merges, mit D1): Kern 8 423 (1 übersprungen), UI 6 753, KiKern 549,
  SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen); Referenzlauf 14/14 byte-gleich gegen
  `2026-09-26_R21_BhkwDeckung`.
- **Neue Tests:** `ZonengeometrieTests` (11, samt Determinismus: zweimal gebildet, tief gleich),
  `GebaeudeZonierungUmhaengenTests` (10), `GebaeudeImportAnsichtHuelleTests` (6, ohne Datenbank), der
  Durchgang „Küche abgetrennt → vier Zonen“ bis zur Rechnung in `GebaeudeImportZonenDatenbankTests`,
  `GebaeudeAnsichtTests` (18, bunit), `GebaeudeImportZonenDialogTests` (+7, jetzt 16, samt Durchgang mit der
  echten Hülle und `ifc4_zonen.ifc`), `StilblattTests` (+1).
- **Nicht gelaufen:** der Bau ohne xBIM (`-p:OhneXbim=true`, nur in `ios.yml`) — per Suche geprüft, keine
  neue Datei außerhalb von `Import/Ifc/` nennt einen IFC-Typ; die Rasterprobe der Fälle GI und GJ
  (Abschnitt 7).

## 6 Aufwand

Welle A in vier Commits am 26.09.2026, Welle C in fünf, Welle D in vier samt zwei Merges und dem
Papiercommit. Die Bezifferung der Stufe samt X1…X3 folgt mit dem Abschluss.

## 7 Offen

- **Welle E** — nur noch die Wiki-Quelle „Gebäudeimport“ mit dem Abschnitt „Mehrere Zonen“ samt Grundriss;
  die Wiki-Arbeit ist in dieser Sitzung auf Anweisung des Anwenders gestoppt, die Logbuch-Sätze stehen im
  Update-Papier unter 1.2.0.5.
- **Windows-Sichtabnahme** der Wellen C und D (Abschnitt 2b, Schritte 1–12); kein iOS-Lauf ohne
  ausdrücklichen Zuruf des Anwenders — die iOS-Hülle und die `Dienste.*` sind unberührt.
- **Rasterprobe** der Fälle GI und GJ nachmessen: Der Grundriss schiebt die Flächenliste bei 400 px unter
  die Zonenliste; auf dem Rechner der Welle D fehlte node.
- **Körperansicht** (Reiter „Körper“, weich gesperrt) mit G7b; dort auch das Aneinanderlegen von Zonen
  (Nr. 23, 24) und die Paare nach M13 an den Kanten (`Grenzverweis`, Nr. 22).
- **Innenränder** der Raumgrenzen zeichnet der Grundriss nicht (Nr. 18).
- **Ab zehn Zonen** wiederholen sich die Farben (Nr. 34); Name, Legende und Kurztext unterscheiden die
  Zonen weiter.
- **„von Hand“** steht nur in der Legende, nicht in der Zonenliste (Nr. 41) — festgelegt mit D3; wer die
  Marke in der Liste will, entscheidet eigens.
- Die Proben 13–16 und 18 mit den lizenzgeklärten Dateien (M10), mit ihnen die Messungen aus der Übergabe
  2.1 (`IfcSpatialZone`, `ParentBoundary`, Profiltyp der zwei `IfcSurfaceOfLinearExtrusion`).
- Die Trenndecke ohne Raumgrenzen (Festlegung 8 gegen Mehrzonenkonzept 6.5): weder Kern noch Dialog tragen
  ihre Eingabe; Z4 bleibt dort wählbar mit Warnung.
- Der Keller-Befund (Abschnitt 4) für G6d.
