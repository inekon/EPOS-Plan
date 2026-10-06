# BN — Reiterzeile „Berichte & Kosten" als Bereichszeile hervorgehoben (Protokoll)

Nachzug zu [`BN_A_Navigation_Protokoll.md`](BN_A_Navigation_Protokoll.md) (Variante A, #590): die Reiterzeile selbst
bleibt, sie wird hier nur deutlicher gezeichnet. Statusnummer **#757**. Der gültige Stand steht im Code und im
Stilblatt, hier steht, wie es geworden ist. Siehe [Statusdatei](../../../aktuell/Status_iOS_Migration.md).

Sitzung „Berichterstellung“, 06.10.2026, Opus 5.5 im Worktree, orchestriert von Fable 5.1. Kein Schemaschritt, kein
Rechenweg, keine Referenzbasis berührt; `EPOS.iOS/` nicht berührt.

| Commit | Inhalt |
|---|---|
| `f93146040` | Berichte & Kosten: Reiterzeile als Bereichszeile hervorgehoben |
| (Merge) | `bc6e7d3d0` — Zusammenführung mit origin/`ios_migration_september` |


## 1 Anlass

Anwenderwunsch 06.10.2026 mit Bildschirmfoto: die zweite Menüleiste (Übersicht, Kosten, Wirtschaftlichkeit, Bericht)
sollte deutlicher und mit etwas größerer Schrift erscheinen. Keine fachliche Änderung, reine Hervorhebung.


## 2 Befund Ist-Stand

Die Reiterzeile von `BerichteKostenSeite.razor` nutzte denselben Baustein `Reiter` wie die übrige Oberfläche
(Startseite, Dialoge, Simulationsseite) ohne eigene Hervorhebung: Knopf mit Statuszeile 52 px, Titel 13 px in leiser
Farbe, Statuszeile 12 px. Für eine Bereichsleiste mit vier Hauptreitern war das zu unauffällig.


## 3 Umsetzung

Neuer Modifikator `epos-reiter--bereich`, gesetzt nur am Wurzelelement von `BerichteKostenSeite.razor`
(`<div class="epos-berichtekosten epos-reiter--bereich">`); die Regeln stehen in `EPOS.UI/wwwroot/epos-ui.css` als
Kindselektoren (`.epos-reiter--bereich > .epos-reiter > …`), neuer Block direkt nach `epos-reiter--senkrecht`. Alle
übrigen Reiter des Hauses bleiben bei 12 px/52 px/leiser Farbe — nichts an `Reiter` oder `Reiterblatt` selbst ändert
sich. Neues Token `--epos-schriftgroesse-bereichsreiter: 15px` im `:root`.

Maße und Farben ausschließlich über vorhandene Token:

| Stelle | Vorher | Nachher (nur in `.epos-reiter--bereich`) |
|---|---|---|
| Kopfzeile | ohne eigene Fläche | eigene Fläche `--epos-flaeche`, Innenabstand 4/8/0/4 px, untere Linie 2 px `--epos-rahmen` |
| Knopfform | eckig | oben abgerundet (`--epos-ecke`) |
| Knopf mit Statuszeile | 52 px, Innenabstand 6/18/4 px | 56 px, Innenabstand 6/18/4 px |
| Titel | 13 px, leise Farbe | 15 px (`--epos-schriftgroesse-bereichsreiter`), Gewicht 600, `--epos-text` |
| Hover | — | `--epos-karte-flaeche-hover` |
| Aktiver Reiter | — | Hintergrund `--epos-flaeche-hell`, Text `--epos-quelle-text`, Unterstrich 3 px `--epos-quelle-rahmen` |
| Gesperrter Reiter | — | `--epos-text-sehr-leise` |
| Statuszeile | 12 px, leise | 13 px, weiter leise |
| unter 900 px | wie Hausleiste | Leistenende eigene Zeile, Abstand 4/0/6/4 px, Kurzform-Regel unverändert |
| `forced-colors` | — | Linie `CanvasText`, Unterstrich des aktiven Reiters `Highlight` |

Hausleiste (Startseite, Dialoge, Simulationsseite) unverändert bei 12 px/52 px/leiser Farbe.


## 4 Tests

- `EPOS.UI.Tests/StilblattTests.cs`, neuer Fall `Die_Bereichszeile_hebt_die_Reiterzeile_ueber_Token_hervor`: Token und
  Regeln vorhanden, keine Festfarben im Modifikatorblock, `forced-colors`-Zweig geprüft, Hausleiste unverändert.
- `EPOS.UI.Tests/Seiten/BerichteKostenSeiteTests.cs`, neuer Fall
  `Der_Wirt_der_Reiterzeile_traegt_den_Modifikator_der_Bereichszeile`: Modifikator genau einmal am Wirt, darunter die
  Tabliste mit den vier `bk`-Reitern.
- `EPOS.UI.Tests` 7.605 von 7.605 bestanden. Kern-Filter Release 0 Fehler.
- Kein ChartProben-, SQL- oder Referenzlauf nötig (kein Rechenweg, kein SQL, kein Renderer); Windows-Schale nicht
  berührt; kein Schemaschritt; keine neuen Ressourcen.


## 5 Sichtprobe

`Proben/Rasterprobe/berichtekostenprobe.mjs` (Chromium headless über Playwright-Node, Wirt auf Port 5311, sechs Fälle
bei 1 280 und 820 px): vorher und nachher Rückgabe 0, kein Verstoß. Fotos liegen beim Anwender außerhalb des Repos.


## 6 Wiki und Logbuch

Die Wiki-Seiten „Wirtschaftlichkeit“ (Anker `reiterzeile`) und „Berichtsvorlagen“ beschreiben nur Inhalt und
Anordnung der Reiterzeile, nichts davon ist falsch geworden — keine Änderung nötig. Kein Logbuch-Satz: reine
Hervorhebung ohne neue Bedienung ist eine Kleinigkeit (Konzept Hilfesystem 13.4).


## 7 Offen

- Sichtabnahme beim Anwender in der Windows-Anwendung (WebView2), auch im Hochkontrastmodus.
- Push-SHA und CI-Kennung werden nach dem Push nachgetragen.
