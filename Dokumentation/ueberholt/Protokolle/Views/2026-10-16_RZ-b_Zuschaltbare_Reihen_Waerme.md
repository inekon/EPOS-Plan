# RZ-b — Zuschaltbare Reihen in drei Wärmebildern

Welle RZ, Teil b (Kandidaten 4 und 5 der Inventur), Anwenderentscheid 16.10.2026 „Kandidaten 1–5 als Welle RZ“.
Zweig `rzb` ab `origin/ios_migration_september` (`7f7d82694`). Teil a (Stromgang, Wärmepumpen-Strom,
Peak-Shaving) läuft getrennt auf `rza`; gemeinsame Dateien sind nur additiv berührt.

## Umsetzung

| Kandidat | Bild | Schalter | Ab Werk | Weg |
|---|---|---|---|---|
| 4 | Gebäude: ''Raumtemperatur'' (Wärmebedarf Gebäude, auch je Zone) | „Außentemperatur“ über dem Bild (`epos-leiste`) | aus | Kern-DTO `GebaeudeBedarfErgebnis.AussentemperaturC` (Stundenreihe des Klimakalenders des Laufs); Hülle-Delegat `BildauftragRaumtemperaturReihen(platz, reihen)` |
| 5a | Wärmepumpe: ''Speichertemperaturen'' | je Speicher ein Schalter (`epos-simerg-schalter`, Muster „Produktion“), erst ab zwei Speichern | alle an | `SimulationErgebnisDaten.Speicherwahl`; Schlüssel `PUFFER_…` in `Bildauftrag.Reihen`, alle gewählt = `null` |
| 5b | Quellprofil: Grafikblatt | „Außentemperatur“ über dem Bild | aus | `KlimaregionCtrl.Aussentemperatur` (Zwischenspeicher der Konfigurationshülle); Delegat `JahresbildReihen(betriebsart, werte, reihen)` |

- Schlüssel der Zusatzreihe: `Zusatzreihen.Aussentemperatur` (`AUSSENTEMPERATUR`) neben `Bilder`.
- Beschriftung = Legendentext: vorhandener Schlüssel `CHART_SERIE_AUSSENTEMPERATUR` (de/en), kein neuer Ressourcentext,
  Designer unverändert; Speicherschalter tragen den Speichernamen des Laufs.
- Ohne Wahl bleibt jeder Bildauftrag gleich (`Reihen = null`, gleicher Zwischenspeicherschlüssel) und jedes Bild das
  Bestandsbild. Mit Außentemperatur zeichnet die Raumtemperatur über `ChartRenderer.SpeicherbetriebModell` (dasselbe
  Verlaufsbild mit Achsentitel; Mindestspanne 0 statt 5 K wirkt nicht), das Quellprofil über `JahresgangModell` wie die
  Erdreichvorschau. `ChartRenderer`, `DiagrammSvg`, Stilblatt und `epos-simerg-*`-Regeln unverändert (keine Tabellenprobe nötig).
- Die Speicherfarbe bleibt bei Abwahl anderer Speicher dieselbe (Rollen über alle vergeben); Quelltemperaturen bleiben stehen.
- CSV am Bild folgt der Wahl, weil sie das gezeigte Modell schreibt. `ErgebnisbildPlan` (Bericht) bleibt unverändert: Keines
  der drei Bilder steht darin, und wie bei „Produktion“ folgt der Bericht keiner Schalterstellung der Oberfläche.
- KI-Eingabebilanz: drei Anzeigeschalter mit Vermerk (`GebaeudeBedarfDialog` 4, `QuellprofilDialog` 7, `WaermepumpeReiter` 10).
- Wiki-Quellen: je ein Satz in „Programm Dokumentation - Gebäudemodell VDI 6007“ (Bild Raumtemperatur) und
  „Programm Dokumentation - Pufferspeicher“ (Bild Speichertemperaturen). Die Wärmepumpenseite zählt die Reihen des
  Quellprofilbilds nicht auf und bleibt.

## Abnahme

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| Windows-Schale (`EnableWindowsTargeting=true`, Release x64) | 0 Fehler |
| EPOS.UI.Tests, gefiltert (Gebäudebedarf, Quellprofil, Wärmepumpenreiter, Ergebnisseite, Speicher, Csv, Wache, KiMaskenabdeckung, Stilblatt) | 1 391/0 |
| EPOS.Kern.Tests, gefiltert (ZuschaltbareReihen, GebaeudeBedarf, Zonenliste, Raum-/Speichertemperatur, Quellprofil, Bildauftrag, Ergebnisbild, Csv, SimulationKonfigKern, Wache) | 733/0 |
| ChartProben | 257 Bilder, 0 Verstöße; Messlatte `Messlatte_2026-10-14.sha256`: 225/225 Zeilen unverändert in der Hashliste |

Neue Tests: `EPOS.Kern.Tests/ZuschaltbareReihenWaermeTests` (drei Fälle: Hüllen an/aus, CSV-Spalten, PNG byte-gleich ohne
Wahl), bunit in `GebaeudeBedarfDialogTests` (2), `QuellprofilDialogTests` (2), `WaermepumpeReiterTests` (3).

## Offen

Sichtprüfung unter Windows; Logbuch-Entwurf mit dem Sammel-Upload: „In den Bildern Raumtemperatur und Quellprofil lässt sich
die Außentemperatur einblenden, im Bild Speichertemperaturen jeder Speicher einzeln aus- und einblenden.“

## Integration

Zweig `integ-dk`, 16.10.2026, zusammen mit `kd8a`, `mu`, `rza`, `rzb` und `origin` (Schritt 237, Basis R57). `KiMaskenabdeckungWacheTests`: `WaermepumpeReiter` zusammen mit RZ-a 11; in `WaermepumpeReiterTests` beide Testblöcke erhalten. EPOS.UI.Tests 9645 grün. Gate gegen R57: Referenzlauf 38 Projekte ohne Abweichung (13 025 956 Werte, CSV 1233 von 1233 byte-gleich), KiKern 553, SpeicherEngine 397, SpeicherPlanung 27, Dokumentationswachen 35, Werkzeugtests 250, ChartProben 257 Bilder 0 Verstöße (Messlatte unverändert), SQL-Dialektprüfer grün, Windows-Schale 0 Fehler.
