# Protokoll G7f — Raumkörper aus der IFC-Datei: Leser, Ansicht, Nachbarschaft (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Stufe G7f (Entscheide E71 bestätigt, E73), drei Opus-Agenten in zwei Worktrees: G7f-1 `2196866a`, `0f6a0bfd`, `c253ea93`, `ca42688f`; G7f-2 `5934a956`, `f7d86cf1`, `63c963ee`, `5fc67d10`, `79d09401`; G7f-4 `e34fe21e`, `d5221490`, `093e0c2d`, `edc737c0`, `c1f69a53`; Merges `6947cc38` (g7f in g7f4) und `7e21e5df` (g7f4 in den Hauptbaum); Papiere (G7f-3, Sonnet) `5d4f6580`, `222132f8`. Statuszeile #727.
**Entscheid:** E71 (bestätigt 04.10.2026: Raummodell mit Zuordnung Raum–Zone, ohne Wanddicke), E73 (Trennflächen aus Raumkörpern); drei Entscheide der Orchestrierung (Abschnitt 4). Kein Schemaschritt, Basis unverändert, Referenzlauf nicht betroffen (kein Referenzprojekt importiert).

## 1 Auftrag

HottCAD zeigt sein Modell räumlich richtig, EPOS-Plan zeigte importierte Gebäude nur schematisch: Die HottCAD-Exporte tragen keine Raumgrenzen, aber je Raum einen Körper (`IfcFacetedBrep`). Der Betrachter soll die wirkliche Form der Räume mit ihrer Zone zeigen (E71), und aus den Körpern soll die Nachbarschaft der Räume kommen, die ohne Raumgrenzen fehlte (E73).

## 2 Vorgehen

G7f-1 Kern-Leser (75 Aufrufe), G7f-2 Ansicht (55), G7f-4 Nachbarschaft (102), je im Worktree, Aufträge nach Datenaustauschkonzept Kapitel 15 und Mehrzonenkonzept 6.2; ein Konflikt in der Probenliste beidseitig aufgelöst; Gate 727 im Hauptbaum.

## 3 Ergebnis

- **Leser** `EPOS.Kern/Allgemein/Import/Ifc/IfcRaumkoerper.cs`, Modell `Simulation/Gebaeude/Dateikoerper.cs` (Punkte [m], Dreiecke, Normalen, Randkanten, Art, Vermerke `Bogen`, `Loch`, `Uneben`, `OhneBeschnitt`, `Offen`, `Mehrschale`; `DREIECKSGRENZE` 300 000), Transport `AbbildRaum.Koerper` → `Umrissraum` → `Raumumriss.Koerper`; nur `Xbim.Ifc4.Interfaces`, ADR-003 bleibt. Lesbar: Extrusionen (Rechteck, Polygon, Bögen mit 32 Sehnen je Vollkreis, Löcher über Brückenkanten), `IfcFacetedBrep`, Flächenmodelle, `IfcTriangulatedFaceSet`, `IfcPolygonalFaceSet`, `IfcMappedItem`, Clipping als erster Operand; benannt nicht lesbar `IfcAdvancedBrep`, `IfcCsgSolid`, `IfcSweptDiskSolid`, `IfcRevolvedAreaSolid` (`IMP_IFC_PROT_KOERPER_ART`), dazu `IMP_IFC_PROT_KOERPER_GELESEN`. Proben 28 (Rundlauf über den eigenen Schreiber S3, byteweise gleich unter de-DE und en-US) und 29 (elf Kleinstdateien `ifc*_koerper_*.ifc`). Messung an den sechs HottCAD-Dateien: alle Räume `IfcFacetedBrep` ohne Vermerk, 558 bis 2 259 Dreiecke je Gebäude, Körperlesen 1 bis 8 ms.
- **Ansicht** (`GebaeudeAnsicht.razor`, `epos-gebaeude-koerper.js`, DTO `GebaeudeAnsichtDaten.cs`, Hülle `GebaeudeImportAnsicht.cs`): Umschalter „Dateikörper | Exportmodell“ im Reiter „Körper“, nur wenn ein Dateikörper vorliegt, Vorgabe Dateikörper; Kennzeichenzeile „n aus Datei, m aus Umriss, k schematisch“ und „vereinfacht: …“; Herkunft je Raum in einer aufklappbaren Raumliste; über 300 000 Dreiecken der benannte Hinweis und Prismen statt Körper. Übergabe als ein `byte[]` je Gebäude (float32 relativ zum Bezugspunkt, int32-Indizes), `BufferGeometry` je Raum, Randkanten als `LineSegments`, beidseitiges Material; Moduswechsel ohne Neuladen des Moduls. Proben 26 und 30 (bunit). 17 Texte de/en.
- **Nachbarschaft** (`Import/Gebaeude/Koerpernachbarschaft.cs`): ebene Flächen je Körper, Paare gegenläufiger Flächen verschiedener Räume (Winkel ≤ 1°, Abstand ≤ 0,8 m, Überlappung bis 0,05 m), Schnittfläche als Summe der Dreiecksschnitte, ≥ 0,1 m²; senkrecht = Trennwand, sonst Trenndecke; je Paar zwei `AbbildGrenze` mit `Grenzherkunft.Koerper`. Rangfolge Raumgrenzen vor Körpern vor Raumbezügen: mit Raumgrenzen nur gezählt (`KOERPERPAARE_GEZAEHLT`); ohne sie ersetzen die Körperdecken die geschätzten Trenndecken, Bauteile der Datei geben die Fläche ihrer Paare ab (keine doppelten Flächen), U-Wert und Aufbau aus dem von beiden Räumen referenzierten Bauteil, sonst freie Decke des Geschosspaars, sonst Vorgabe; Beleg `GIMP_BELEG_KOERPER`, Anzeige „IFC-Datei (Körper)“; Meldungen `GRENZEN_AUS_KOERPER`, `KOERPERPAAR_SCHWACH`, `KOERPER_OHNE_PAAR`. Tests `KoerpernachbarschaftTests` (9), `KoerpertrennflaechenTests` (2), Proben `ifc4_koerper_nachbarn(.ifc|_grenzen.ifc)`.
- **Zahlen HottCAD** (Körperpaare / Σ Trennwand / Σ Trenndecke m²): MFH_mittel 100 / 280 / 224; MFH-Klein 120 / 305 / 339; Sportheim 168 / 953 / 575; Verwaltung 467 / 2 577 / 3 994; WG-EH55 79 / 321 / 219; Produktion 195 / 5 847 / 8 749. `GRENZEN_ENTKOPPELT` erscheint in keiner Datei mehr, die Vorgabe wird überall Z4 (M7), die Gegenprobe meldet 0 Zonenpaare über 2 %; die Verwaltung meldet EG/OG2 als schwach gekoppelt (25 %).
- **Folge für importierte Gebäude:** Auch der Einzonenweg ändert sich, weil die Flächen gegen unbeheizte Räume jetzt vollständig aus den Körpern kommen statt zum Teil aus Raumbezügen: Heizwärme Z5 MFH_mittel 42,2 → 48,1 MWh/a, MFH-Klein 44,6 → 53,9, Sportheim 62,2 → 71,1, Verwaltung 220,4 → 227,8, Produktion 1 864,4 → 1 868,0, WG unverändert 17,5. Erwartungen der Diagnose- und Durchgangstests nachgezogen. Referenzprojekte unberührt.

## 4 Entscheide der Orchestrierung

1. G7f-1 liest `Dateikoerper.Art` als obersten Träger (etwa `MappedItem`), nicht als innersten Körper; gleiche Punkte werden zusammengelegt; Bogenzwischenpunkte bekommen keine senkrechte Randkante.
2. G7f-2: Herkunft je Raum in einer Raumliste statt als `title` am Canvas (keine Texte im Modul); Umschalter als Knopfpaar mit `aria-pressed`, damit das Canvas beim Wechsel bleibt.
3. G7f-4: Flächen gegen unbeheizte Räume aus den Körpern gelten auch im Einzonenweg (Rangfolge aus E73 angewandt); zur Bestätigung beim Anwender vorgelegt (Abschnitt 7).

## 5 Abweichungen

Englische Texte in `Resource.en-US.resx` (die Datei des Bestands). Herkunft „IFC-Datei (Körper)“ über Beleg, nicht als neuer Wert von `Importherkunft` (kein Schemaschritt). Probe mit Raumgrenzen ohne Bodenplatte und Dach (16-KB-Grenze der Kleinstdateien).

## 6 Abnahme

Gate 727 im Hauptbaum auf `222132f8`: Kern-Filter 0 Fehler, ChartProben 208 gleich, Kern 10 953 grün, 3 übersprungen, 2 rot — einer eigen (`GebaeudeImportAnsichtDateikoerperTests`: Kulturvorrichtung im `foreach` ohne Klammern, vom Kulturwächter nicht als `using` erkannt, behoben und nachgeprüft, 30 Tests grün) und einer fremd (`KiWissensdeckungTests`: KI-Bereich „Kältemaschine“ ohne Wissensabschnitt, aus der Welle KU3 einer anderen Sitzung), UI 7 521, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), Dokumentationswachen 35/35, Referenzlauf 19/19 PASS gegen R35, 576/576 byte-gleich, gestörter Lauf PASS; Windows-Schale auf Linux 0 Fehler. Windows-Schale auf Linux 0 Fehler (G7f-2).

## 7 Offen

- Anwender: Bestätigung, dass die Flächen gegen unbeheizte Räume aus den Körpern auch im Einzonenweg gelten (sonst Rückbau auf Zonen-Trennflächen); Windows-Sichtabnahme der Körperansicht an den sechs HottCAD-Dateien; iPad-Probe 31 nur auf Zuruf.
- Leser gegen Revit, Archicad, HiCAD ungemessen (keine lokalen Dateien); `IfcRoundedRectangleProfileDef`, `IfcRectangleHollowProfileDef`, `IfcCircleHollowProfileDef` benannt nicht gelesen statt vereinfacht.
- Zähler `INNEN_EINSEITIG` zählt in Körperpaaren aufgegangene Wände mit; `Grenzherkunft.Raumbezug` angelegt, nicht gesetzt; der Beleg „Körper“ wird nicht gespeichert.
- Nordpfeil im Modus Dateikörper unverändert (Modell-Nord).
- Logbuch-Sätze (Version offen): „Die Körperansicht des Gebäudeimports zeigt die Räume in ihrer Form aus der IFC-Datei, mit Umschalter zum Exportmodell.“ — „IFC-Import: Führt eine Datei keine Raumgrenzen, kommen die Trennwände und Trenndecken zwischen den Zonen aus den gemeinsamen Flächen der Raumkörper.“

## 8 Aufwand

Rund 232 Agentenaufrufe, 3,5 PT.
