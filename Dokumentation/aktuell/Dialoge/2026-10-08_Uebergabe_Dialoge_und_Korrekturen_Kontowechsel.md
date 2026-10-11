# Übergabe der Sitzung „Dialoge und Korrekturen“ auf ein anderes Konto (08.10.2026)

Dieses Papier übergibt die Sitzung „Dialoge und Korrekturen“ an eine neue Sitzung unter einem
anderen Konto. Es nennt den Stand, die geltenden Entscheide, die Regeln der Sitzung über
`CLAUDE.md` hinaus und die offenen Punkte; der Einstiegs-Prompt steht in Abschnitt 5. Regelquelle
bleibt `CLAUDE.md`. Es löst das Papier vom 05.10.2026
([`2026-10-05_Uebergabe_Dialoge_und_Korrekturen.md`](../../ueberholt/2026-10-05_Uebergabe_Dialoge_und_Korrekturen.md)) ab.

## 1 Stand

- **Zweige:** Arbeitszweig `ios_migration_september`. Der Sitzungszweig des alten Kontos,
  `claude/dialoge-korrekturen-okt`, ist nach jedem Push gleich dem Arbeitszweig; das neue Konto
  bekommt einen eigenen Sitzungszweig und pusht beide.
- **Statuszeilen dieser Sitzung seit 06.10.** in `Status_iOS_Migration.md`: #797 Erdsondenfeld
  (Schritt 195), #798 Basis R40 Erdreichquellen, #803 Fachspalten bei Komponentenübernahme und
  Flottenstudie, #806 Erdwärme-Nachzug (Basis R41), #809 Komponentenübernahme mit Kindzeilen,
  #812 Kostenpositionen folgen der Anlage (mit Selbstheilung ohne Änderungsstempel), #829 Schloss
  in Projektdialogen und Vorlagenverwaltung, #830 Reiter „Kosten“ zweizeilig, #834 Katalogauswahl
  (Befund, fünf spielbare Mockups), dazu die beiden Zeilen vom Kontowechsel: Katalogauswahl V1
  gewählt (#835) und diese Übergabe (#836).
- **Basis:** gültig ist R44 `2026-10-08_R44_Kuehlkurve` (Sitzung Gebäudesimulation, #827).
  Kopfzeile „Schemaschritt angemeldet“: zuletzt gebaut 202 (#827), 203 frei.
- Es laufen keine Agenten, es gibt keine offenen Worktrees dieser Sitzung.

## 2 Geltende Entscheide (Auswahl)

**Gebäude**

- Löschen aus der Datenbank ist unabhängig von der Projektzuordnung; vor Löschen und Bearbeiten
  wird still gespeichert; die Löschregel ist überall gleich.
- Nutzfläche statt Wohnfläche; Baustoff-Zuordnungen beim Import.

**Oberfläche**

- Fokusregel `FocusAsync(preventScroll: true)` und Bannerregel (das Warnbanner haftet im
  Fensterdialog), beide in `EPOS.UI/CLAUDE.md`. Kachel „Kühlung und Kälteanlagen“.

**Erdwärme**

- Sondenmodell nach Claesson–Javed, Betrachtungsjahr 10, zweiter Feldlauf, Regeneration durch
  Kühlwärme, Schema 195.
- Entzug ohne Taktstrom; Erdreichprüfung je Anlage mit Erdreichquelle (Basis R41).

**Komponentenübernahme**

- Fachspalten über `AnlagenFachspalten`.
- Kindzeilen werden auf die gleichnamige Gegenstelle umgeschlüsselt; ein Wechselrichter ohne
  Gegenstelle wird als Projektkopie angelegt; Strangmodule werden mitkopiert.
- Kostenpositionen der ersetzten Anlagen werden der Reihe nach umgehängt, der Rest bleibt lose mit
  Hinweis. Kältemaschine und Puffer löschen räumen ihre Kosten ab.
- Die Wirtschaftlichkeit heilt die Zuordnung vor dem Lesen, ohne den Änderungsstempel zu setzen.
  Die 13 losen Positionen der Testdatenbank bleiben.

**Flottenstudie**

- Vollständige Kopie, Kosten nach Kapazität skaliert, auch das erste Stück; Prozentarten
  ausgenommen.

**Schloss**

- In allen Projektdialogen mit Katalogliste und in der Vorlagenverwaltung der Konditionierung.
- Die Projektverwendung sperrt nichts; gesperrte Sätze werden nie überschrieben (beide
  BHKW-Rückfragen entfernt); die Löschsperren bei Zeitreihen, Tagesverteilungen und Baustoffen
  bleiben.

**Katalogauswahl** (Anwenderentscheid 08.10.2026; Konzept
[`Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md`](../Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md),
Abschnitt 3, KA‑E‑1 bis KA‑E‑11)

- **Variante V1 „Gerahmt und gestapelt“** für alle zwölf Projektdialoge mit Katalogauswahl und die
  Kältemaschine, ein gemeinsamer Baustein: oben „Im Projekt“, darunter „Katalog (Datenbank)“,
  unten die Zeile „gewählter Satz“; der Dialogkörper rollt nicht, nur die Listen, nie ineinander.
- Ziehbare Trennlinie, Höhe je Dialog über `Dienste.Einstellungen` gemerkt; Detailzeile auf- und
  zuklappbar; Mehrfachauswahl in beiden Bereichen; Doppelklick und Enter übernehmen.
- Katalogpflege bleibt im Projektdialog. „Bearbeiten…“ je Bereich (Projektkopie bzw. Katalogsatz),
  Mehrfach-Bearbeiten mit Blätterleiste und „für alle gewählten setzen“.
- Rückweg „In die Datenbank übernehmen…“ (neuer Katalogsatz oder Ursprung überschreiben, nie bei
  gesperrtem oder fehlendem Ursprung); technische Daten und Kosten gehen mit.
- Kennfarben Projekt grün, Katalog blau, gewählter Satz orange; breite Projektsätze öffnen als
  Überlagerung im selben Fenster.

**Auslieferung**

- Kataloge „jetzt nicht sperren“ (08.10.2026).

## 3 Regeln der Sitzung (über `CLAUDE.md` hinaus)

**Agenten**

- Die Orchestrierung arbeitet nur selbst, wenn ein Agentenaufruf teurer wäre: eine Zeile, eine
  Anmeldezeile, ein Merge ohne Konflikt.
- Rollen: `opus-umsetzung`, `sonnet-mechanik`, `haiku-pruefung`; das Modell immer ausdrücklich
  setzen; höchstens zwei Agenten parallel.
- Erster Schritt im Agenten-Worktree:
  `git fetch origin ios_migration_september && git reset --hard origin/ios_migration_september`.
  Agenten pushen nicht.

**Wellenfolge und Status**

- Merge → Gate → Statuszeile und Logbuchsatz → Push beider Zweige → CI-Vermerk allein mit
  `[skip ci]`. Logbuchsätze in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` unter 1.2.0.6.
- Statusnummer unmittelbar vor dem Commit gegen origin messen.
- Schemaschritt vor dem Bau anmelden, die Kopfzeile allein und mit `[skip ci]` pushen; nicht
  gebrauchte Nummern zurückgeben.

**Gate**

- `GATE_ABLAGE=<scratchpad>/gate bash Werkzeuge/Gate/gate_linux.sh <Nr> <worktree>`, Nummern 79xx;
  Dauer rund 1 bis 1,5 Stunden, mit einem wartenden Hintergrundbefehl.
- Nie zwei Gates zugleich, das verdoppelt die Zeit. Auf fremde Gates nur mit
  `pgrep -f "[g]ate_linux.sh"` warten; die Klammer ist Pflicht, sonst findet die Schleife ihre
  eigene Shell und endet nie.

**Umgebung**

- Ein Neustart des Containers beendet Agenten und Gates. Danach Worktrees prüfen und Agenten per
  SendMessage fortsetzen; sie behalten ihr Protokoll.
- CI über `gh api` (Läufe) und `mcp__github__get_job_logs`; Artefakte lassen sich nicht laden.

**Arbeitsweise**

- Vor jedem Auftrag prüfen, ob der Wunsch schon gebaut ist (am 07.10. waren zwei vorgemerkte
  Aufträge längst gebaut: Warnband #647 und Kalendervorlagen #648).
- Mockups für Dialoge als spielbares HTML unter `Dokumentation/aktuell/Mockups/`. Der Anwender
  wählt nach dem Ausprobieren, dann folgen Detailfragen, dann das Konzept. Umsetzung nur auf
  ausdrücklichen Auftrag.
- Anwenderfragen mit AskUserQuestion, Empfehlung zuerst. Klickt der Anwender eine Frage weg,
  warten.

**Nie ohne Rückfrage:** iOS-, macOS- und Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags.

**Antworten:** auf Deutsch, knapp; Zwischenstände nur auf Nachfrage.

## 4 Offene Punkte (mit Empfehlung)

1. **Katalogauswahl V1:** Umsetzung auf Zuruf, als eigene Welle nach dem Stufenplan des Konzepts
   (Abschnitt 8, Stufe 1 und 2 zuerst, erster Dialog Heizkessel). Offene Punkte des Konzepts
   (Abschnitt 7): (1) Umsetzung beauftragen; (2) erster Dialog Heizkessel, dann BHKW; (3) Ort der
   Katalogkosten, Empfehlung ein Schemaschritt „Katalogkosten und Ursprung“ (`ID_KostenVorlage`
   an den Katalogen, `ID_Stamm` an Kopien ohne Verweis; die Nummer meldet die Umsetzungssitzung
   vor dem Bau an); (4) Trennlinienhöhe über `Dienste.Einstellungen`, ein Schlüssel je Dialog, ohne
   Schemaschritt; (5) Rückweg bei Kindzeilen: technische Kindzeilen gehen vollständig mit,
   anlagenbezogene bleiben im Projekt; (6) Katalogpaket: Schlüssel stehen lassen, Prüfsumme nicht
   nachführen.
2. **Ergebnisübersicht der Simulation:** Befund: Die Legende neben den Ringen bricht bei schmaler
   Spalte Buchstabe für Buchstabe um (`EPOS.UI/Seiten/Simulation/UebersichtReiter.razor`,
   `epos-ui.css` `.epos-simueb-ringzeile`/`.epos-simueb-legende`). Vorschlag: Container-Abfrage
   untereinander unter etwa 520 px, Umbruch nur an Wortgrenzen, zweizeilige Legende, gleiche Regel
   für Wärme, Strom und Kälte, Browserprobe. Der Anwender hat die Rückfrage weggeklickt;
   Anweisung abwarten.
3. **Herstellerimport:** `UpdateImport` in sieben Stamm-Controllern überschreibt gesperrte Sätze
   nach Bestätigung im Konfliktdialog (Entscheid 9.2). Empfehlung: so lassen; die Bestätigung des
   Anwenders steht aus.
4. **Aufräumen:** toter Parameter `schutzUebergehen` an `KatalogBrowserDaten.Speichern`.
5. **Auslieferung:**
   - Setup-Version 1.2.0.3 anheben.
   - Probelauf der Auslieferungsvorlage auf der produktiven Quelle (Schema 198): Rückgabe 6, zehn
     Registerkataloge ohne gesperrten Satz (Heizkessel 67, PV 7, Wechselrichter 2 347, Puffer 33,
     Solarkollektoren 7, Stromspeicher 7, Brennstoffe 25, DBTagV 12, Solarganglinie 1,
     Stromganglinie 3).
   - Entscheid des Anwenders: jetzt nicht sperren. 2 979 ungesperrte Zeilen in 20 Katalogen,
     darunter 272 von 278 Gebäuden. Empfehlung: eigene Gebäude vor einer Auslieferung prüfen.
   - Die CI-Ausnahmeliste (`windows.yml`, `Werkzeuglauf.LEERE_PAKETTEILE_DER_TESTDATENBANK`)
     umfasst neun Kataloge, ohne Wechselrichter.
6. **Sichtabnahmen unter Windows:** Meldungsbanner, Kühlungskachel, Steckbrief im
   Projektassistenten, Felder des Erdreichdialogs, Komponentenübernahme mit Kindzeilen und Kosten,
   Schloss in den Projektdialogen, Reiter „Kosten“.
7. **Wiki-Sammelupload** auf Zuruf; die Logbuchsätze 1.2.0.6 liegen in `Wiki_Update_2026-09-26.md`.

## 5 Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Dialoge und Korrekturen“ für EPOS-Plan (Repo inekon/EPOS-Plan,
Arbeitszweig ios_migration_september). Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Dialoge/2026-10-08_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md,
dann den Kopf und die letzten zwanzig Zeilen der Tabelle in
Dokumentation/aktuell/Status_iOS_Migration.md und
`git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september`.

Rolle: du orchestrierst nur; opus-umsetzung für Dialoge, Hüllen, Rechenweg, Tests, Fehlersuche
und Konfliktauflösung mit Fachinhalt; sonnet-mechanik für Merge, Gate, Statuszeilen,
Logbuchsätze, Push und CI-Vermerk; haiku-pruefung für Zählungen. Modell bei jedem Agentenaufruf
ausdrücklich setzen, höchstens zwei Agenten parallel. Agenten im eigenen Worktree, erster
Schritt `git fetch origin ios_migration_september && git reset --hard
origin/ios_migration_september`, sofort committen, nicht pushen, bin/ und obj/ löschen.

Wellenfolge: Merge → Gate (Werkzeuge/Gate/gate_linux.sh im Hintergrund; nie zwei Gates
gleichzeitig; auf fremde Gates nur mit `pgrep -f "[g]ate_linux.sh"` warten) → Statuszeile und
Logbuchsatz (Wiki_Update_2026-09-26.md, 1.2.0.6) → Push beider Zweige (ios_migration_september
und dein Sitzungszweig) → CI-Vermerk allein mit [skip ci]. Statusnummer unmittelbar vor dem
Commit gegen origin messen; Schemaschritte vor dem Bau in der Kopfzeile der Statusdatei anmelden
und allein pushen. Nie ohne Rückfrage: iOS-, macOS-, Setup-Läufe, Wiki-Upload, Pull Requests,
Force-Push, Tags. Commit-Trailer: Co-Authored-By des arbeitenden Modells und Claude-Session mit
der URL deiner Sitzung. Vor jedem Auftrag prüfen, ob der Wunsch schon gebaut ist (Statusdatei,
Konzepte). Dialogwünsche zuerst als spielbare Mockups; umgesetzt wird erst auf ausdrücklichen
Auftrag. Anwenderfragen mit AskUserQuestion, Empfehlung zuerst. Antworten auf Deutsch, knapp;
Zwischenstände nur auf Nachfrage.

Parallel arbeiten andere Sitzungen (Gebäudesimulation, IFC/Gebäudeimport, Berichterstellung,
Wirtschaftlichkeit) auf demselben Arbeitszweig; Statusnummern, Basen und Schemaschritte werden
schnell vergeben.

Erste Schritte: (1) Stand prüfen: `git status` sauber, keine Konfliktmarker, keine liegen
gebliebene AGENT_LAEUFT, die Zeilen der Übergabe tragen ihren CI-Vermerk. (2) Den Stand in
höchstens fünf Zeilen melden. (3) Die offenen Punkte aus Abschnitt 4 der Übergabe mit dem
Anwender abstimmen, zuerst: Soll die Katalogauswahl V1 umgesetzt werden (Konzept
`Dokumentation/aktuell/Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md`, spielbare Mockups
`Dokumentation/aktuell/Mockups/Projektdialog_Katalogauswahl_Uebersicht.html`)?
```
