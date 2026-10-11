# Protokoll UB-E4 — Bivalenzdiagramm, Reiter, Kennzahlen, Bericht, Export, Vorlagen (09.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#855**. Commits E4-a `4036f1164`, `2a2faeac8`, `90926dc67`, `862214236`; E4-b `6f35a6f32`, `eb991487b`,
`c50d9c24a`, `6748bd80a`, `6f830d05b`, `1ab6b3f57`, `b263588f0`, `a07a69d73`; E4-c `f9420562a`; Merge ``5c7bbb89c` (E4 in den Hauptbaum), danach `0e5599923` und `6aa2452b8` (origin ZK und ZK-b), Schemaschritt der Welle 205`. **Entscheid:** E109.
Konzepte: Fachkonzept Übergabegrenze/Bivalenz (Fassung 2) und Umsetzungskonzept Abschnitt 10.

## 1 Auftrag und Entscheidlage

- **E109** (Anwender, 08.10.2026): Reihenfolge UB-E1 → E2 → E3 → E4 → E5; E4 bringt das Bivalenzdiagramm im Diagramm-Renderer, die Kachelzeile der
  Betriebsbereiche im Wärmepumpen-Reiter, die Kennzahlen `wp.bivalenz.*`, Bild und Tafel im Bericht, die Export- und KI-Felder und die neu gebauten Vorlagen.
- Entscheide der Orchestrierung: Das Bild braucht keine vollständigen Daten (Platzhalter statt Fehler); der Bericht leitet die Bivalenz aus den Projektdaten her,
  die berechneten Punkte kommen vorrangig aus dem gespeicherten Lauf. Keine Rechenwirkung: Die Referenzbasis R46 bleibt.

## 2 Wellen

| Welle | Commits | Ergebnis |
|---|---|---|
| E4-a | `4036f1164`, `2a2faeac8`, `90926dc67`, `862214236` | 17 Ressourcenschlüssel `BER_BILD_BIVALENZ_*` in beiden Sprachen. `BivalenzdiagrammModell` (Kurven, Bereichsflächen mit Grenzen per Bisektion, Marken, Stundenpunkte, Achsen in runden Schritten) und `ChartRenderer.Bivalenzdiagramm` (zeichnet nur, Farben aus der Tafel, Platzhalter ohne Übergabedaten); `Bivalenzherleitung.Betrieb` liefert Bereich und Leistung der Wärmepumpe je Außentemperatur. `InternalsVisibleTo` für ChartProben. `Proben/ChartProben`: drei Maß- und vier Gegenproben, 11 neue Bilder, Messlatte `Messlatte_2026-10-09.sha256` mit 222 Hashes, alle 211 Zeilen der Vorgängerlatte unverändert. `BivalenzdiagrammTests` (9 Fälle) |
| E4-b | `6f35a6f32`, `eb991487b`, `c50d9c24a`, `6748bd80a`, `6f830d05b`, `1ab6b3f57`, `b263588f0`, `a07a69d73` | 36 Ressourcenschlüssel je Sprache. Reiter `WaermepumpeReiter.razor`: Kachelzeile Betriebsbereiche (Stunden und Wärme je Bereich, Bivalenzpunkte, Übergabegrenze; nur mit `Daten.Bereiche`), leise Zeilen Spreizung und Rücklauf nur bei Werten > 0, Hinweiszeile Stundenmodell; Feld `WaermepumpeErgebnis.Bereiche` in `SimulationErgebnisCtrl`. 13 Kennzahlen `wp.bivalenz.*` (Stunden und MWh je Bereich B1–B4, `punkt_1`, `punkt_2`, `uebergabe_max`, `spreizung_unterschritten`, `ruecklauf_ueberschritten`), Katalogfassung 16 → 17. Bericht: `BivalenzBerichtswerte.cs`, `VariantenDaten.Bivalenz`, Sammlerschritt 6b, Tafel `Bivalenz(...)`, Bild `stand.bild.wp_bivalenz` (nur Word), Tafel `stand.tabelle.bivalenz` (Word und Excel), zwei Schalter; Prüfhinweise Anfahrgrenze und Mindestrücklauf unter der Tafel. Export: CSV-Kopfzeilen `Spaltenname;Wert` vor den Stundenspalten, KI-Sicht `wp_betriebsbereiche` (lesend); Mengenszenario `SzenarioMengen.cs` übernimmt die Bereiche unverändert (sonst fielen die Kennzahlen in den Szenarien auf null). Tests: 4 bunit `WaermepumpeReiterBivalenzTests`, 11 `BivalenzBerichtTests` (3 an der Testdatenbank), `KiSimulationMaskeTests` 70 → 71 Felder, Fassungspins (17) in `VorlagenfeldkatalogWacheTests`, `AufheizBerichtTests`/`AufheizAufschlagErgebnisTests` (`>= 16`), Messlatte `Vorlagenfeldkatalog_v17.txt` |
| E4-c | `f9420562a` | Zehn Vorlagen unter `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/` (Standard, Beispiel, Kurzbericht de/en, Ausführlich de/en, Bausteine de/en `.dotx`, Excel Ausführlich de/en) mit `Werkzeuge/Berichtsvorlage -- alle` auf Fassung 17 neu gebaut; Validator 0 Fehler, Stilvorlage unverändert |
| Merge | ``5c7bbb89c` (E4 in den Hauptbaum), danach `0e5599923` und `6aa2452b8` (origin ZK und ZK-b), Schemaschritt der Welle 205` | Zusammenführung der Zweige `ub-e4a`, `ub-e4b`, `ub-e4c` in den Arbeitszweig |

## 3 Dateien

- Kern: `EPOS.Kern/Allgemein/Bericht/Diagramme/` (`BivalenzdiagrammModell.cs`, `ChartRenderer.Bivalenzdiagramm.cs`), `Bivalenzherleitung`, `EPOS.Kern/Allgemein/Bericht/` (`BivalenzBerichtswerte.cs`, `BerichtsDaten.cs`, `BerichtsDatenSammler.cs`, `Tabellen/Berichtstabellen.cs`, `Vorlagen/Vorlagenfeldkatalog*.cs`), `SzenarioMengen.cs`, `ErgebnisModel.cs`, `SimulationErgebnisCtrl.cs`, `KiDialoge`, Ressourcen in beiden Sprachen, `EPOS.Kern.csproj`.
- Oberfläche: `EPOS.UI/Seiten/Simulation/WaermepumpeReiter.razor`, `SimulationKiSicht.cs`, `SimulationErgebnisHuelle.Wege.cs`.
- Vorlagen: zehn Dateien unter `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/`.
- Proben: `Proben/ChartProben` mit `Messlatte_2026-10-09.sha256`.
- Tests: `BivalenzdiagrammTests`, `WaermepumpeReiterBivalenzTests`, `BivalenzBerichtTests`, `BivalenzBerichtsquelleTests`, `KiSimulationMaskeTests`, `VorlagenfeldkatalogWacheTests` samt `Messlatten/Vorlagenfeldkatalog_v17.txt`, `AufheizBerichtTests`, `AufheizAufschlagErgebnisTests`.

## 4 Proben

| Probe | Ergebnis |
|---|---|
| ChartProben, Bivalenzdiagramm | 3 Maß- und 4 Gegenproben, 11 neue Bilder; 254 Bilder grün |
| Messlatte | 222 Hashes; die 211 Zeilen der Vorgängerlatte unverändert |
| Kennzahlen | 13 Kennzahlen `wp.bivalenz.*`, Katalogfassung 17 |
| Bericht an der Testdatenbank | 3 Proben (`BivalenzBerichtTests`) und Quelle `BivalenzBerichtsquelleTests`: ohne Einbindung nichts, Projekt 1058 mit Einbindung mit Herleitung und Bild |
| Vorlagen | Validator 0 Fehler, Stilvorlage unverändert |
| Kern-Filter nach E4-c | 360/360 |
| UI nach E4-c | 55/55 |

## 5 Festlegungen

**E4-a:** Der Renderer zeichnet nur; die Bereichsregel der Zeichnung ist dieselbe wie die der Stunde (statisch je Außentemperatur). Ohne Übergabedaten steht ein Platzhalter.

**E4-b:** Bild ohne Datenbedarf: Stundenpunkte nur, wenn `TEMPERATUR` und `WP_WAERME` vorliegen; ohne Wärmepumpe mit Einbindung bleibt das Bild leer mit Grund; ohne Übergabedaten steht der Platzhalter.
Die Herleitung im Bericht läuft aus den Projektdaten für die erste Wärmepumpe mit Einbindung, berechnete Punkte kommen vorrangig aus dem gespeicherten Lauf. Hybrid-Anteil als `HybridAnteil` × 100, der Mindestanteil als eigene Zeile.
Prüfhinweis Anfahrgrenze: Außenluft-Wärmepumpe mit Auslegungsraum unter 15 °C. Prüfhinweis Mindestrücklauf: Niedertemperaturkessel nach Bauart oder Brennstoff 12/15; Elektrokessel (Brennstoff 13) ausgenommen.
Die Kachelzeile trägt bewusst keine Vorlagenfeld-Markierung (die Wertzeilen des Reiters tragen keine). Die KI-Sicht liest `Spaltenname=Wert`.

**E4-c:** Alle zehn Vorlagen in einem Sammelbefehl auf Fassung 17 gebaut.

## 6 Offen

- Kein Referenzprojekt hat eine gesetzte `Einbindung`: Bild und Tafel sind in den Referenzberichten leer.
- Wiki und Logbuch in E5.

## 7 Gate und CI

`Rest-Gate auf `bc579cb1a` grün: Kern-Filter-Bau 0 Fehler; EPOS.UI.Tests 7962/7962, KiKern.Tests 549/549, SpeicherEngine.Tests 397/397, SpeicherPlanung.Tests 27/28 (1 übersprungen), Dokumentationswachen (EPOS.Kern.Tests-Filter) 35/35; Windows-Schale auf Linux 0 Fehler; Designer wiederholbar (16482 Einträge); SQL-Dialekt 2575 Texte, 0 Fundstellen; Werkzeugtests Formularkarte 124/124, Auslieferungsvorlage 61/61, Gebaeudevergleich 24/24, ZapfprofilValidierung 39/39; keine BOM, keine Konfliktmarker; Referenzlauf 27 von 27, GESAMT PASS (9335471 Werte), CSV byte-gleich 879 von 879; ChartProben und volle EPOS.Kern.Tests liefert der Kern-Lauf der CI`
CI: `Kern-Lauf der CI auf dem Sitzungszweig: Run 37915060058 auf bc579cb1a grün (11:07 UTC)`
