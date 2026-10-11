# Protokoll AS3: Stufe 3 Administration und Support — Über, Diagnose, Supportpaket

**Auftrag des Anwenders (11.10.2026):** Stufe 3 des Konzepts umsetzen (3a Dialog „Über EPOS-Plan“
mit Diagnosedaten und Geste, 3b Supportpaket). Konzept:
[`Konzept_Administration_Support_EPOS-Plan.md`](../../../aktuell/Konzept_Administration_Support_EPOS-Plan.md),
Entwicklerseite:
[`Entwicklerhandbuch_Administration_Support_EPOS-Plan.md`](../../../aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.md).
Statuszeile #977. Zweig `claude/gracious-brown-qx5kb2`, Stand `98b15126`. Kein Schemaschritt, kein
Rechenweg (kein Referenzlauf nötig). iOS unverändert (kein Menüband, *Version* ist dort nicht
erreichbar).

## 1 Zuschnitt

Zwei Wellen nacheinander, je Opus 5.5 im eigenen Worktree: 3a (Zweig `as3-ueber`) baut Dialog,
Diagnoseblock, Geste und Windows-Fenster; 3b (Zweig `as3b-supportpaket` ab `as3-ueber`) baut das
Supportpaket in Kern und Dialog. Sequenziell, weil 3b den Dialog aus 3a erweitert.

## 2 Rahmen

- `b0ae1bf0`: Umnummerierung der eigenen Statuszeilen #966–#969 auf **#970–#973**; danach Merge
  `34d17c82` von `origin/ios_migration_september` (brachte die Referenzbasis R52). Die Zeilen zogen
  später noch einmal um (Endstand #974–#977 für Stufen 1 und 2, Nachzug und Stufe 3).
- CI der Stufe 2: Kern-Lauf 38140084461 auf dem Push `830694ad` grün (13:50 UTC).

## 3 Welle 3a — Dialog, Diagnose, Geste (`as3-ueber`)

- `3b83506f`: Ressourcen `UEB_*` (14 Schlüssel je Sprache), `Diagnosedaten.ProtokollordnerPfad()`.
- `1bd38735`: `EPOS.UI/Dialoge/Hilfe/UeberDialog.razor`, `UeberDaten.cs` (`UeberDaten`,
  `UeberAnstoss`, `UeberTexte`), `EPOS.UI.Daten/Hilfe/UeberHuelle.cs`, CSS `.epos-ueber-*`,
  `UeberDialogTests` (11).
- `e1d7b0c0`: `WindowsFormsApplication1/Views/Help/UeberFenster.cs`, Haken
  `BlazorDialogForm.Befehlstaste`, `EntwicklungHuelle.GesteUmschalten` ersetzt
  `EinschaltenNachRueckfrage`, Fall `Seitenschluessel.Version` in `HauptfensterHuelle` auf das
  Fenster, `Versionsmeldung()` und die Geste im `Hauptfensterrahmen` entfernt; Marke und
  Titelzusatz bleiben.
- `cd1762d2`: Konzept 3.2/5/8, Handbuch 1.1/1.3/3.3/6/7.
- Merge `fbb0d2c1`.

Verhalten: *Hilfe → Version* öffnet „Über EPOS-Plan“ (Kopf: Produktname, Claim, Version mit
Baudatum = Schreibzeit der Programmdatei, INEKON; Diagnoseblock nur lesbar mit „Diagnosedaten
kopieren“ und „Protokollordner öffnen“; Kontakt mit `mailto:`-Link und Knopf „Fernwartung…“;
Fußleiste Schließen). Die Geste Strg+Umschalt+Alt+E wirkt nur noch im Über-Fenster und schaltet in
beide Richtungen: Einschalten nach Rückfrage, Ausschalten ohne.

## 4 Welle 3b — Supportpaket (`as3b-supportpaket`)

- `fca5d691`: `EPOS.Kern/Allgemein/Support/Supportpaket.cs`, `SupportpaketTests` (9), Ressourcen
  `SUP_PAKET_*` (11 Schlüssel).
- `e654de31`: Knopf „Supportpaket speichern…“ und Haken „KI-Protokoll beilegen“ (Vorgabe aus) im
  `UeberDialog`, Anzeigeform `SupportpaketAnzeige`, Rückruf `Func<bool, Task<SupportpaketAnzeige>>`,
  `UeberFenster` mit `SaveFileDialog` (Vorgabename `EPOS-Plan_Supportpaket_<Datum>.zip`),
  `UeberDialog` in `KiDialogAusnahmen`, `UeberDialogTests` +4.
- `bb3eace3`: Konzept 5/8, Handbuch 3/5/6/7.
- Merge `98b15126`.

Inhalt des Pakets: `Diagnosedaten.txt`, `Ausnahmen.txt`, `Ausnahmen.alt.txt` (Ordner `Logs` neben der
Datenbank), `migration_protokoll.txt` (neben der Datenbank, Rückfall `Logs`), `Katalogabgleich.txt`
(jüngste 2000 Zeilen über `Katalogabgleich.Protokoll()`, kein neues SQL), `ki_aktionen.txt` nur mit
Haken; nie Projektdaten, nie `.sqlite`; fehlende Quellen stehen als Liste im Paket.

## 5 Abweichungen vom Auftrag

- Kein InfoKnopf am Dialog (keine Wache verlangt ihn).
- Haken `BlazorDialogForm.Befehlstaste`, damit das Fenster die Geste vor dem Dialog sieht.
- Die Geste schaltet in beide Richtungen (das Konzept nannte nur das Einschalten).
- Die Versionen sind getrennt: Anzeigeversion und Baudatum kommen aus der Programmdatei.
- Die Signatur von `Supportpaket` verlangt keinen `IDatenzugriff`; Pfade kommen über die
  Pfadschnittstelle.
- Der Name des Migrationsprotokolls steht als `Supportpaket.MIGRATIONSPROTOKOLL`, weil
  `SchemaMigration.PROTOKOLL_DATEI` in der Schale liegt.
- `SupportpaketAnzeige` als Anzeigeform und ein asynchroner Rückruf statt eines Rückgabewerts.
- `UeberDialog` steht in `KiDialogAusnahmen` (Export; der Haken ist ein Eingabefeld).

## 6 Merges und Lehre

Der Merge-Commit `c30269d2` (Zapfprofil-Nachzug) hatte Konzept und Handbuch bei der
Konfliktauflösung auf 12 bzw. 7 Zeilen verstümmelt; `fbb0d2c1` baute beide Papiere aus `34d17c82`,
`4d1ec875` und `as3-ueber` neu auf. Seither gilt bei jeder Konfliktauflösung in Papieren die
Pflichtprüfung von Zeilenzahl und Überschriftenliste.

## 7 Gate (Orchestrierung)

Das Gate lief am 11.10. im Hauptbaum nach dem Merge aller drei Wellen (`98b15126`), über den SDK-Umweg (10.0.112 statt 10.0.400) und mit der echten Testdatenbank:

| Prüfung | Ergebnis |
|---|---|
| `WP-Plan.Kern.slnf` Release | 0 Fehler |
| Windows-Schale auf Linux (`EnableWindowsTargeting`) | 0 Fehler |
| `EPOS.UI.Tests` | 8864 von 8864 |
| `KiKern.Tests` | 553 von 553 |
| `SpeicherEngine.Tests` | 397 von 397 |
| `SpeicherPlanung.Tests` | 27 von 28, 1 übersprungen (Bestand) |
| `EPOS.Kern.Tests` nach Filter (Supportpaket, Ausnahmeprotokoll, Einstellungen, KiDialog, Zapf, Ueber, Fernwartung, Lizenz, Menue, Hilfe, Ressource, Wache, Entwickler, Diagnosedaten, Dokumentation, Wiki, HelpMapping, Auslieferungskennzeichen) | 2479 von 2479 |

Der volle Kern-Lauf (rund 57 Minuten) ist die CI; Push-SHA und CI-Kennung stehen im Statuspunkt „Nach #977“ (e).

Zahlen der Agenten: `EPOS.UI.Tests` 8836/8836 (3a) und 8840/8840 (3b), Kern-Filter 1127/1127 (3a)
und 159/159 (3b), SqlDialektPruefer 2839 Texte/0 Fundstellen, Windows-Schale auf Linux 0 Fehler.

Nachtrag: gepusht `aa90b58b` (nach dem Merge von `origin/ios_migration_september`), Kern-Lauf 38150657481 (kern.yml, ubuntu) grün, abgeschlossen 16:30 UTC.

## 8 Umgebung

Bau über den Helfer außerhalb des Repositoriums; die Windows-Schale wurde auf Linux mit
`EnableWindowsTargeting` gebaut.

## 9 Offene Punkte

- Sichtabnahme des Anwenders unter Windows: Über-Dialog mit Geste (beide Richtungen),
  Explorer-Aufruf, Zwischenablage, Speicherdialog des Supportpakets.
- Wiki-Upload gebündelt (Einstellungen-Seite mit Rubrik Zapfprofil, „Über EPOS-Plan“ mit Hinweis
  auf Hilfe → Version) und Logbuch-Sätze; Versionsnummer beim Anwender erfragen.
- AS-Q6 (a) nur halb: `ki_aktionen.txt` wird weiter neben der Datenbank geschrieben, nicht in
  `Logs`; der `%TEMP%`-Rückfall der Ausnahmeprotokolle ist nicht im Paket (eigene Aufträge).
- Stufe 4 (Supportvertrag im Token) auf Zuruf.
- Push-SHA und CI-Kennung nach dem Push in der Statuszeile nachtragen.
