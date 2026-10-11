# Protokoll AS3b: Geste Strg+Umschalt+F12 greift bei Fokus in der WebView2

**Anlass:** Anwenderbefund „Strg+Umschalt+F12 funktioniert nicht“ nach der Umstellung der Geste
(#997, Sitzung Gebäudesimulation). Konzept:
[`Konzept_Administration_Support_EPOS-Plan.md`](../../../aktuell/Konzept_Administration_Support_EPOS-Plan.md) 3.2,
Entwicklerseite:
[`Entwicklerhandbuch_Administration_Support_EPOS-Plan.md`](../../../aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.md).
Statuszeile #1003. Kein Schemaschritt, kein Rechenweg.

## 1 Analyse

Opus, mit Dekompilat von `Microsoft.Web.WebView2.WinForms` 1.0.3179.45. Liegt der Fokus im
Browserfenster der WebView2 (eigener Prozess `msedgewebview2.exe`), laufen Tastenmeldungen nie
durch `PreTranslateMessage`/`ProcessCmdKey` der Form; der Fenster-Weg greift nur bei Fokus auf der
WinForms-Seite (die 100 bis 300 ms vor der Initialisierung). Das Steuerelement reicht Tasten über
`CoreWebView2Controller.AcceleratorKeyPressed` an sein eigenes `OnKeyDown` weiter. Der Dialog-Weg
(`@onkeydown` am Wurzel-`div`, `tabindex="-1"`, Fokus nur beim ersten Render) greift nur, solange
`document.activeElement` im `div` liegt; `.epos-dialog` hatte keine Mindesthöhe, ein Klick auf die
freie Fläche legte den Fokus auf `body`, danach waren Geste und Escape tot. Dieselbe Lücke betraf
F1 (Hilfe) im `Hauptfensterrahmen`. Die Kommentare in `UeberFenster.cs` und `Hauptfensterrahmen.cs`
(„vor der WebView abgefangen“ über `ProcessCmdKey`) stimmten für ein prozessfremdes Kindfenster nicht.

## 2 Umsetzung

Opus 5.5 im Worktree, Zweig `eg3-geste`, Basis `6bfe6a9c`.

- `f0511ca6`: `BlazorDialogForm.Befehlstaste` zusätzlich am `KeyDown` der WebView2 (im Konstruktor,
  `Handled` und `SuppressKeyPress`); `ProcessCmdKey` bleibt für Fokus auf der WinForms-Seite;
  Kommentare in `BlazorDialogForm` und `UeberFenster` berichtigt.
- `31d44984`: Über-Dialog mit CSS-Regel `#app:has(> .epos-fenstermarke) > .epos-dialog.epos-ueber`
  und `min-height` (100vh abzüglich sicherer Abstände und Bannerhöhe `--epos-banner-oben`), Fokus
  nach jedem Schalten erneut auf die Wurzel, bunit-Fall `Nach_dem_Schalten_bekommt_die_Wurzel_den_Fokus_erneut`.
- `e71feebe`: F1: `BlazorSeite` mit eigenem Haken `Befehlstaste` am `KeyDown` ihrer WebView2,
  `Hauptfensterrahmen` belegt ihn für F1 (Hilfe-Assistent per `BeginInvoke`), Kommentar berichtigt.
- `0f1f76ff`: Wache `EPOS.Kern.Tests/BlazorDialogFormTastenWacheTests` (4 Fälle: Dialogform in
  `ProcessCmdKey`, Dialogform am `KeyDown` mit `Handled`, Seite am `KeyDown`, F1 auf beiden Wegen
  samt `BeginInvoke`).
- `76bef87a`: Handbuch (Tabelle „Tastenfolge“, Fundstellen) und Konzept 3.2; Merge `3e1b2005`.

## 3 Abweichungen

- Commit `31d44984` wurde per amend nachgebessert: die Wache
  `LizenzbannerTests.Jede_Hoehenregel_in_voller_Fensterhoehe_zieht_das_Banner_ab` verlangte den
  Abzug der Bannerhöhe in der neuen Höhenregel.
- Die Fensterprobe wurde nicht vom Agenten gefahren, sondern von der Orchestrierung.

## 4 Gate und Fensterprobe

Zahlen der Welle: Kern-Filter 0 Fehler; Windows-Schale auf Linux 0 Fehler (17 Warnungen Bestand);
`EPOS.UI.Tests` 9166 von 9166; `EPOS.Kern.Tests` Filter Wache/Dokumentation 512 von 512.
Gate der Orchestrierung: `Kern-Filter 0 Fehler, Windows-Schale auf Linux 0 Fehler, `EPOS.UI.Tests` 9166/9166, `KiKern.Tests` 553/553, `SpeicherEngine.Tests` 397/397, `SpeicherPlanung.Tests` 27/28 (1 übersprungen, Bestand), `EPOS.Kern.Tests` nach Filter 2499/2499; der volle Kern-Lauf ist die CI`. Fensterprobe (`fensterprobe.mjs`, `bannerprobe.mjs`): ``fensterprobe.mjs` und `bannerprobe.mjs` grün mit Gegenproben (drei Fenstermaße; der Über-Dialog ist in den Proben nicht enthalten); Ersatzmessung des Über-Dialogs mit Playwright in vier Fenstermaßen: Kopf und Schlussleiste haften bei langem Inhalt am Fensterrand wie ohne die Regel, die Regel ändert nur die Boxhöhe; bei kurzem Inhalt steht die Schlussleiste wie bisher unter dem Inhalt, nicht am unteren Fensterrand (`margin-top` 10 px statt `auto` — Gestaltungsfrage, Anwenderentscheid)`.

## 5 Offene Punkte

- Sichtabnahme des Anwenders unter Windows: Geste sofort nach dem Öffnen; nach Klick auf die freie
  Fläche; nach Klick auf „Kopieren“; Escape nach Klick auf die Fläche; F12 allein öffnet die
  DevTools nur im Debug-Build; F1 im Hauptfenster bei Fokus in einem Eingabefeld öffnet den
  Hilfe-Assistenten genau einmal; kein senkrechter Rollbalken im Über-Dialog bei kurzem Inhalt.
- Push-SHA und CI-Kennung nachtragen (Statuspunkt „Nach #1003“).

## 6 Lehre

Ein WebView2-Kindfenster erreicht `ProcessCmdKey` nicht: Tastenhaken gehören an das `KeyDown` des
Steuerelements, `ProcessCmdKey` bleibt nur für Fokus auf der WinForms-Seite.
