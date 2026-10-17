# SK-3 — Oberfläche des Energieflussbilds in „Ergebnisse – Energie“

Welle SK, Schritt SK-3, 16.10.2026, Zweig `sk3` (von `sk12`, Kopf `695cac7ee`). Konzept:
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../2026-10-16_Konzept_Energieflussbild_Sankey.md)
(Abschnitte 4, 7, 8, 10). Vorgänger: [`2026-10-16_SK-12_Abbildung_Energiefluss.md`](2026-10-16_SK-12_Abbildung_Energiefluss.md).
Anwenderentscheide 16.10.2026: dritte Stellung „Fluss“, ein Bild für eine gewählte Version, SK-Q1 bis SK-Q11 nach
Empfehlung, SK-Q12 Werte nur im Zeigertext (keine sichtbare Wertetabelle, keine Zahlen im Bild, verborgene
Textalternative über `aria-describedby`).

## 1. Umfang

- **Umschalter** (`UebersichtErgebnisse.razor`): Stellungen aus einer Liste (`ANSICHT_TABELLE`, `ANSICHT_GRAFIK`, neu
  `ANSICHT_FLUSS = "fluss"`); Pfeiltasten laufen über alle drei (rechts/unten vor, links/oben zurück, umlaufend),
  Pos1/Ende an die Ränder, Fokus folgt der Stellung. Ablage bleibt `Uebersicht.Ergebnisse.Ansicht`; „tabelle“ und
  „grafik“ gelten weiter, ein unbekannter Wert fällt auf die Tabelle. **Kein Delegat, kein Knopf:** ohne `FlussLaden`
  gibt es nur zwei Stellungen, ein gespeichertes „fluss“ zeigt dann die Tabelle.
- **Komponente** `UebersichtErgebnisFluss.razor` mit DTO `UebersichtErgebnisFlussDaten.cs` (`ErgebnisFlussVersion`,
  `ErgebnisFlussStand`, `ErgebnisFlussZeile`, `ErgebnisFlussKnotenzeile`, Textbündel `ErgebnisFlussTexte`):
  `Auswahlfeld` „Version“ (Versionen der Vergleichswahl in Spaltenreihenfolge, Stamm zuerst, Vorgabe Stamm; nicht
  simulierte Version mit Zusatz „(nicht simuliert)“, wählbar, Karte `BK_ERG_FLUSS_NICHT_SIMULIERT` statt Bild; leere
  Bilanz mit dem Grund des Kerns). Bild: zwei `DiagrammSvg` (`OhneZoom`, `LegendeSchaltbar=false`), Kennungen
  `ergebnis-fluss-<id>-breit|schmal`, in einer `figure` mit `aria-label` und `aria-describedby` auf die verborgene
  Textalternative (`.epos-komponenten-sprachausgabe`: je Band „Von → Nach: x MWh/a (y %) · Herkunft“, danach je Knoten
  „Zufluss · Abfluss · Rest“). Leise Zeilen: Bedienung des Zeigers, Hinweise des Kerns (nicht geschlossene Knoten),
  gebündelte Knoten, Konvention Umweltanteil; darunter die Quelle des Abschnitts. „nur Unterschiede“ und „Anzeige
  anpassen…“ wirken nicht auf das Bild, der Knopf bleibt im Kopf.
- **Hülle** `UebersichtErgebnisGaben`: `Laden` füllt `UebersichtErgebnisStand.FlussVersionen` und merkt Zeile und
  Spaltenname je Version; `FlussLaden(id)` nimmt die Kennzahlen aus **demselben Puffer** wie die Tabelle →
  `Energiefluss.Aus` → `EnergieflussAbbildung.Bild` → `ChartRenderer.EnergieflussModell` breit und schmal
  (`knotenwerte: false`); Ergebnis je Version, Simulationsstand, Sprache und Name zwischengespeichert (dieselbe
  Referenz, kein Neuaufbau des Baums). Versionswahl je Sitzung (`s_flussVersion`, wie die Klappgruppen). Neue Gaben:
  `FlussLaden`, `FlussVersionLesen`, `FlussVersionGewechselt`, `FlussTexte`. `UebersichtSeiteGaben` unverändert (reicht
  den Satz durch). Plattformfrei, keine Datenbank in der Oberfläche.
- **KI-Sicht:** `UebersichtErgebnisse.Text()` beginnt in der Stellung Fluss mit
  `KI_ANSICHT_FLUSS = "Ansicht: Fluss (Energieflussbild der Version {0})"`, führt die Gruppenzeilen wie bisher und hängt
  `EnergieflussBilanz.KiText()` an (`[fluss] Energiefluss — <Version>`, je Band eine Zeile, Hinweise; Zeilenenden LF);
  nicht simuliert: die Karte. Weg bleibt `UebersichtSeiteKiSicht.ErgebnisseLesen`.
- **Stilblatt** (`epos-ui.css`, Block nach `.epos-ergebnis-diagramm`): `.epos-ergebnis-fluss`, `-kopf`, `-bild`
  (höchstens 1 180 px), `-mass--breit|--schmal`; `@media (max-width: 899.98px)` tauscht schmal gegen breit. Keine
  Wertetabelle des Flusses im Stilblatt (Konzept 4.3 nannte noch `-wertetabelle`; durch SK-Q12 entfallen).
- **Ressourcen** beider Sprachen (11): `BK_ERG_ANSICHT_FLUSS`, `BK_ERG_FLUSS_VERSION`, `BK_ERG_FLUSS_NICHT_SIMULIERT`,
  `BK_ERG_FLUSS_BILD`, `BK_ERG_FLUSS_ZEILE`, `BK_ERG_FLUSS_KNOTEN`, `BK_ERG_FLUSS_TEXT_FLUESSE`,
  `BK_ERG_FLUSS_TEXT_KNOTEN`, `BK_ERG_FLUSS_ZEIGER`, `BK_ERG_FLUSS_HINWEIS_UMWELT`, `BK_ERG_FLUSS_HINWEIS_GEBUENDELT`;
  `designer_neu.py schreiben`: 19 013 Einträge (+11), zweiter Lauf +0.
- **Wachen:** `KiMaskenabdeckungWacheTests` — Wirt `UebersichtErgebnisFluss` → `UebersichtSeite` und
  `EINGABESTELLEN` 1 mit Vermerk (Versionswahl ist Anzeigezustand je Sitzung, kein Einstellwert, kein Katalogfeld);
  `VorlagenfeldAbdeckungWacheTests` — Ausnahme für die zwei Bilder bis SK-4 (`stand.bild.energiefluss`, Katalogfassung
  25), danach trägt das Bild das Vorlagenfeld.
- **Wiki-Quelle** `Programm Dokumentation - Varianten.wiki`: Abschnitt „Tabelle, Grafik oder Fluss“ mit einem Absatz
  zum Bild (Version, keine Zahlen im Bild, Zeiger, Verlust, Umweltanteil, BHKW-Einspeisung bilanziert, nicht
  zugeordnet, gebündelt, schmale Fassung, Sprachausgabe). Gegenlesemuster ohne Treffer.
- **Probe:** `diagrammprobe.mjs --fall fluss` samt Gegenprobe; Wirt `/uebersichtprobe?ansicht=fluss[&version=<id>]`;
  LIESMICH nachgezogen.

## 2. Abnahme

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale
  `dotnet build WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Release -p:Platform=x64 -p:EnableWindowsTargeting=true`:
  0 Fehler (keine Hülle der Schale betroffen).
- `EPOS.UI.Tests` vollständig (`xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`): **9 657 bestanden,
  0 fehlgeschlagen** (2 min 37 s). Neu `UebersichtErgebnisFlussTests` (10 Fälle: dritte Stellung samt Merken, Pfeiltasten
  und Pos1/Ende, alte und unbekannte Werte, Versionswahl mit nicht simulierter Version, Modell bleibt dieselbe Referenz
  und derselbe Puffer, keine sichtbare Wertetabelle und keine Zahl im Bild, Textalternative über `aria-describedby`,
  Hinweise, KI-Text, Parametersatz der Hülle, ohne Gaben); `UebersichtErgebnisseTests` (drei Stellungen mit dem Satz der
  Hülle), `StilblattTests` (neuer Fall SK3), `KiMaskenabdeckungWacheTests`, `VorlagenfeldAbdeckungWacheTests` angepasst.
- `EPOS.Kern.Tests` gefiltert `Energiefluss|Wache|Uebersicht|Ki`: **1 128 bestanden, 0 fehlgeschlagen** (darin
  `WikiProduktdatenWacheTests`, `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests`).
- `diagrammprobe.mjs --fall fluss` (Chromium headless, Wirt Blazor Server): 24 Elemente, **12 Bänder und 12 Knoten**
  mit Zeigertext, alle in der Textalternative (24 Zeilen); Gegenprobe ohne Zeigerereignisse verfehlt (24 von 24 ohne
  Text). Fall `zoom` unverändert grün (×12, 41 Schritte, größter Abstand 0,50 px, 0 Mutationen; Gegenprobe verfehlt).
  Sichtprobe: breit ab 900 px, schmal darunter (`display` block/none gemessen).
- Keine SQL-Anweisung, kein Schemaschritt, Rechenweg und Referenzlauf unberührt; Renderer (`ChartRenderer.Energiefluss.cs`),
  ChartProben und Messlatte nicht angefasst.

## 3. Offen und Hinweise

- Die synthetischen Ergebnisse des Wirts tragen einen Brennstoffträger ohne Kesselzuordnung; das Bild zeigt deshalb
  zwei Bänder „nicht zugeordnet“ und zwei Hinweiszeilen — Verhalten des Kerns nach SK-Q5, kein Fehler der Oberfläche.
- Das Vorlagenfeld am Bild folgt mit SK-4 (Ausnahme in `VorlagenfeldAbdeckungWacheTests` dann entfernen).
- Konzept 4.3 nennt noch die Klasse `-wertetabelle`; SK-5 zieht das Konzept beim Verschieben nach `ueberholt/` nach.

## 4. Logbuch-Entwurf (Versionsnummer beim Anwender erfragen)

> **<Version>** — Im Reiter „Übersicht“ unter Berichte & Kosten zeigt der Abschnitt „Ergebnisse – Energie“ mit der
> Darstellung „Fluss“ das Energieflussbild einer gewählten Version; die Werte erscheinen beim Überfahren.
