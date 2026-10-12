# Übergabe der Sitzung „Administration und Support“ an ein anderes Konto (12.10.2026)

Dieses Papier übergibt die Sitzung „Administration und Support“ nach den Stufen 1 bis 3 des Konzepts
[`Konzept_Administration_Support_EPOS-Plan.md`](Konzept_Administration_Support_EPOS-Plan.md) und den
Nachzügen Zapfprofil, Quick Assist und EG3 an eine neue Sitzung in einem anderen Konto. Es nennt den
Stand, die geltenden Entscheide, die Regeln der Sitzung über [`CLAUDE.md`](../../CLAUDE.md) hinaus,
die Cloud-Umgebung und die offenen Punkte; der Einstiegs-Prompt steht in Abschnitt 5. Regelquelle
bleibt `CLAUDE.md`. Fachstand und Fundstellen stehen im Konzept (Abschnitte 3 bis 8) und im
Entwicklerhandbuch
[`Entwicklerhandbuch_Administration_Support_EPOS-Plan.md`](Entwicklerhandbuch_Administration_Support_EPOS-Plan.md)
(Word-Fassung daneben, erzeugt mit dem Werkzeug
[`Werkzeuge/Entwicklerhandbuch`](../../Werkzeuge/Entwicklerhandbuch/LIESMICH.md)); dieses Papier
schreibt beides nicht ab.

## 1 Stand

- **Zweige:** Arbeitszweig `ios_migration_september`; Sitzungszweig `claude/gracious-brown-qx5kb2`
  (altes Konto). Dessen Code-Stand ist der Merge `6f348bdb` von `origin/ios_migration_september`
  (`97f72a67`, #1032) in EG3 und Quick Assist; die Zweigspitze ist der Commit dieses Papiers. Der
  Arbeitszweig trägt den Quick-Assist-Stand bereits (Merge `d31d5192` der Sitzung
  „Gebäudesimulation“ vom 12.10., 01:20 UTC, dort als #1012 eingeordnet); **EG3 (#1003), der
  CI-Vermerk zu #1012 und dieses Papier fehlen ihm noch** — sie kommen mit dem Sitzungszweig
  (Abschnitt 4 c). Keine laufenden Agenten, keine Worktrees, keine Routinen.
- **Kennung** der Statuszeilen: `Sitzung „Administration und Support“`.
- **Statuszeilen dieser Sitzung** (11./12.10.2026): #972 Konzept (Erstfassung), #973 Konzept mit den
  Entscheiden AS-Q1 bis AS-Q10, #974 Stufe 1 (Entwicklermodus, Menükopf „Entwicklung“,
  Entwicklerbereiche verborgen), #985 Stufe 2 (Fernwartung: Menüpunkt *Hilfe → Fernwartung…*,
  Menüsperre, Dialog, Lizenzbedingung), #986 Nachzug Zapfprofil (Rubrik „Zapfprofil“ im
  Einstellungsdialog ohne Entwicklermodus), #987 Stufe 3 (Dialog „Über EPOS-Plan“, Diagnosedaten,
  Geste, Supportpaket), #1003 EG3 (Geste Strg+Umschalt+F12 bei Fokus in der WebView2, F1 im
  Hauptfenster), #1012 Nachzug Quick Assist (Fernwartung über das Windows-Bordmittel statt RustDesk),
  #1033 diese Übergabe. Die Geste selbst (EG/EG2, #997) kam aus der Sitzung „Gebäudesimulation“.
- **CI-Vermerke:** gesetzt für #974 (Lauf 38134668642), #985 (38140084461), #986/#987 (38150657481),
  #1003/#1012 (38183856323 auf `0636dcef`). **Offen:** der Lauf des Sitzungszweigs auf der
  Zweigspitze mit diesem Papier (Vermerk in Zeile #1033, Punkt b).
- **Gate dieses Stands** (Hauptbaum, nach dem Merge `6f348bdb`): Kern-Filter 0 Fehler, Windows-Schale auf Linux 0 Fehler, `EPOS.UI.Tests` 9323/9323, `KiKern.Tests` 553/553, `SpeicherEngine.Tests` 397/397, `SpeicherPlanung.Tests` 27/28 (1 übersprungen, Bestand), `EPOS.Kern.Tests` nach Filter 2524/2524 (12.10.2026, 13:33–13:55 UTC); der volle Kern-Lauf ist die CI.
- **Basis** R52 `2026-10-11_R52_Vorheizen`, unberührt; kein Schemaschritt dieser Sitzung; die
  Kopfzeile „Schemaschritt angemeldet“ ist von dieser Sitzung nicht belegt.
- **Papiere:** Konzept (Stand-Block; 3.2 Geste; 3.4 Zapfprofil als Anwenderrubrik; 4 Fernwartung mit
  Quick Assist, RustDesk nur als Reserve; 7 Entscheide; 8 Stufenplan mit Logbuch-Entwürfen),
  Entwicklerhandbuch (Markdown und Word), sechs Protokolle unter
  [`ueberholt/Protokolle/Administration_Support/`](../ueberholt/Protokolle/Administration_Support/)
  (AS1, AS2, AS2b, AS3, AS3b, Nachzug Zapfprofil). Wiki-Quellen geändert, Upload offen:
  `Projekte/Wiki/Programm Dokumentation - Fernwartung.wiki` (neu), `… - Einstellungen.wiki`
  (Reiter Zapfprofil), `Über EPOS-Plan.wiki`, `Installation und Update.wiki`,
  `Werkzeuge/WikiUpload/seiten.tsv`, `help_mapping.txt` (`Form_Fernwartung.btn_Help`).
- **Code-Landkarte** (Fundstellen vollständig im Handbuch, Abschnitt 6): Entwicklermodus
  `EPOS.Kern/Allgemein/Freigabe/` (`Freigabestufe`, `Entwicklermodus`, `Entwicklerbereiche` mit acht
  Schlüsseln), Menü `EPOS.UI/Bausteine/Menuetabelle.cs` und `Menueband.razor` (`Sperrgrund`),
  Fernwartung `EPOS.Kern/Allgemein/Support/Fernwartung.cs`, `LizenzCtrl.FernwartungErlaubt`,
  `EPOS.UI/Dialoge/Hilfe/FernwartungDialog.razor`, `EPOS.UI.Daten/Hilfe/FernwartungHuelle.cs`,
  `WindowsFormsApplication1/Allgemein/Support/QuickAssistStart.cs`; Über-Dialog
  `EPOS.UI/Dialoge/Hilfe/UeberDialog.razor`, `UeberHuelle.cs`,
  `WindowsFormsApplication1/Views/Help/UeberFenster.cs` (Geste `GESTE`, Supportpaket-Speichern);
  Diagnose `EPOS.Kern/Allgemein/Diagnose/Diagnosedaten.cs`, Supportpaket
  `EPOS.Kern/Allgemein/Support/Supportpaket.cs`; Tastenweg der Schale
  `WindowsFormsApplication1/Allgemein/Blazor/BlazorDialogForm.cs` (Haken `Befehlstaste` in
  `ProcessCmdKey` **und** am `KeyDown` der WebView2, Wache `BlazorDialogFormTastenWacheTests`).
- **Logbuch-Entwürfe** stehen in Konzept 8 (Veröffentlichung mit dem nächsten Sammel-Upload, Versionsnummer beim Anwender erfragen).

## 2 Geltende Entscheide des Anwenders (11.10.2026)

Die Tabelle in Konzept 7 ist die Quelle; hier die Kurzfassung mit den Nachträgen vom Abend des 11.10.:

- **AS-Q1 (a):** ein Baustein, vier Quellen (HKLM-Sperre schlägt alles, Umgebungsvariable
  `EPOS_ENTWICKLER=1`, Startargument `--entwickler`, HKCU-Einstellung). **AS-Q2 (a):** Geste im
  Dialog „Über EPOS-Plan“ mit Rückfrage; **Tastenfolge Strg+Umschalt+F12** (Entscheid „nehme
  Strg+Umschalt+F12“, statt Strg+Umschalt+Alt+E), Befund „funktioniert nicht“ mit EG3 behoben.
- **AS-Q3 (a):** verborgene Elemente am Fachort, Kopf „Entwicklung“ nur für Werkzeuge ohne Fachort.
  **AS-Q4 wie empfohlen, Ausnahme:** die Zapfprofil-Rechenvorgaben sind Anwenderrubrik „Zapfprofil“
  („das Zapfprofil soll nicht gesperrt sein für Benutzer“). **AS-Q5 (a):** Bestand aus dem Modus
  bleibt ohne Modus lesbar. **AS-Q6 (a):** `ki_aktionen.txt` nach `Logs\`, Supportpaket als ZIP ohne
  Projektdaten (Stufe 3b gebaut; der Umzug der Datei ist offen, Abschnitt 4).
- **AS-Q7:** erst RustDesk, dann **„nehme Quick Assist als Standardweg“** (Windows-Bordmittel; RustDesk
  quelloffen bleibt Reserve ohne Betrieb); „kein TeamViewer als Support“ im Wiki. **AS-Q8 (b):**
  Fernwartung bei bezahlter Lizenz, Laufzeit egal (Lesart im Kern: Token `person`/`firma`, Status
  weder `NichtAktiviert` noch `UhrManipuliert`). **AS-Q9 (c):** Link auf die Fernwartungsseite
  `https://epos-plan.de/support/fernwartung`; der Dialog startet zusätzlich Quick Assist.
  **AS-Q10:** iPad-Fernwartung zurückgestellt („wird später bearbeitet“).
- Entwicklerhandbuch als Word-Dokument (aus dem Markdown erzeugt); Papiere liegen unter
  `Dokumentation/aktuell/`, Protokolle unter `Dokumentation/ueberholt/Protokolle/Administration_Support/`.
- Modellwahl der Orchestrierung: Fable nur für Orchestrierung und schwierige Analysen, sonst
  `opus-umsetzung`, `sonnet-mechanik`, `haiku-pruefung` je Aufgabe.

## 3 Regeln der Sitzung (über `CLAUDE.md` hinaus)

**Agenten und Wellen**

- Fable orchestriert nur. Umsetzung `opus-umsetzung`, Mechanik `sonnet-mechanik`, Zählungen
  `haiku-pruefung`; Modell bei jedem Aufruf ausdrücklich setzen. Agenten arbeiten in Worktrees
  `../wt-<name>` auf Zweigen ab `origin/ios_migration_september` (oder ab dem genannten Zweig),
  committen sofort, pushen nicht. Papiere, Protokolle, Wiki-Quellen und Merges ohne Fachkonflikt
  an `sonnet-mechanik`; Code, Tests, Hüllen an `opus-umsetzung`.
- Wellenfolge: Merge in den Hauptbaum → Gate → Statuszeile und Protokoll → Push → CI-Vermerk.
  Das Gate dieser Sitzung (ein wartender Hintergrundbefehl, rund 30 bis 40 Minuten): Kern-Filter
  `WP-Plan.Kern.slnf` Release, Windows-Schale auf Linux
  (`-p:EnableWindowsTargeting=true`), `EPOS.UI.Tests`, `KiKern.Tests`, `SpeicherEngine.Tests`,
  `SpeicherPlanung.Tests` vollständig, `EPOS.Kern.Tests` mit Filter
  (`Supportpaket|Ausnahmeprotokoll|Einstellungen|KiDialog|Zapf|Ueber|Fernwartung|Lizenz|Menue|Hilfe|Ressource|Wache|Entwickler|Diagnosedaten|Dokumentation|Wiki|HelpMapping|Auslieferungskennzeichen`),
  alle mit den xUnit-Schaltern aus `CLAUDE.md`. Der volle Kern-Lauf ist die CI (kern.yml, rund
  70 Minuten). Wer Dialogkopf, Schlussleiste, Banner oder den Über-Dialog anfasst, zieht vorher
  `fensterprobe.mjs` und `bannerprobe.mjs` (`Proben/Rasterprobe/LIESMICH.md`).
- CI-Vermerk rund 75 Minuten nach dem Push als Routine (`send_later`), Vorlage: Statuspunkt
  „~~Push-SHA und CI-Kennung nach dem Push nachtragen~~ erledigt: gepusht `<sha>`, Kern-Lauf
  `<id>` auf ubuntu grün“ und ein „Nachtrag:“-Absatz im Gate-Abschnitt des Protokolls.
- **Reine Papier-Pushes mit `[skip ci]`:** jeder Push ohne den Vermerk bricht den laufenden Kern-Lauf
  desselben Zweigs ab (zweimal passiert). Merge-Commits mit Code tragen keinen `[skip ci]`.

**Statusdatei und Papiere**

- Statusnummern vergeben mehrere Sitzungen parallel; die Nummer unmittelbar vor dem Commit gegen
  `origin/ios_migration_september` messen (`git show origin/ios_migration_september:…Status… |
  grep -oE '#10[0-9]{2}' | sort -u`); Umnummerierung ist die Regel (der Quick-Assist-Nachzug zog
  von #995 über #998, #1001, #1005 und #1009 auf #1012). Umzüge treffen Statusdatei, Konzept,
  Handbuch (Markdown und Word) und Protokoll — mit `grep -rn '#<nr>'` vollständig suchen.
- Statusdatei-Konflikt: origin-Fassung nehmen, die eigene Tabellenzeile sortiert einordnen, die
  eigenen Punkte „Nach #…“ am Ende von Abschnitt 4 übernehmen; eigene Zeilen behalten die eigene
  Fassung, wenn sie nur um Vermerke erweitert ist. Fremde Zeilen nie umordnen.
- **Nach jeder Konfliktauflösung in einem Papier Zeilenzahl und Überschriftenliste gegen `HEAD`
  prüfen** (ein Merge hatte Konzept und Handbuch auf 12 bzw. 7 Zeilen verstümmelt).
- `.resx` mit Konflikt: die neuen Blöcke stehen am Dateiende („Administration und Support, …“);
  „ours-Block + `</data>` + theirs-Block“ vereinigen, XML parsen, Dubletten prüfen, dann
  `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben`. Neue Schlüssel immer in beiden
  Sprachen.
- Word-Fassung: nach jeder Änderung am Handbuch-Markdown
  `NODE_PATH="$(npm root -g)" node Werkzeuge/Entwicklerhandbuch/erzeugen.js` und die `.docx` mit
  committen; ohne Markdown-Änderung nicht neu erzeugen (Zeitstempel).
- Wiki: Fachseiten ohne „seit/bisher/Entscheid“, ohne Hersteller- und Produktnamen (Wache
  `WikiProduktdatenWacheTests`); Logbuch nur für sichtbare Bedienänderungen (Regel 13.4). Der Upload
  ist nie Sache eines Agenten (Zugangsdaten nur beim Anwender bzw. bei der Orchestrierung).
- Trailer `Co-Authored-By: Claude <Modell> <noreply@anthropic.com>` und `Claude-Session: <Link der
  Sitzung>`; Betreff bis 72 Zeichen; `git add <pfad>`, nie `-A`.

**Abstimmung mit den anderen Sitzungen**

- Im alten Konto laufen die Sitzungen „Gebäudesimulation“ (`session_01X2CX69qnhXXtc6epxnPUv6`),
  „Kälteanlagen“ und „Dialoge und Korrekturen“ (Konto 3). Sitzungsnachrichten (`send_message`)
  erreichen nur Sitzungen desselben Kontos — aus dem neuen Konto läuft die Abstimmung allein über
  origin (Statusdatei, Zweige) und den Anwender. Die Mitnahme des Sitzungszweigs in den
  Arbeitszweig hatte die Sitzung „Gebäudesimulation“ übernommen; ab jetzt macht es die neue
  Sitzung selbst (Abschnitt 4 c).

### Cloud-Umgebung

- **.NET-SDK:** `global.json` verlangt 10.0.400 (`latestFeature`). Das Ubuntu-Paket bringt nur
  10.0.112; diese Sitzung hat damit gebaut und getestet, indem `dotnet` **außerhalb** des Repos
  startete (`cd /tmp && dotnet build /…/WP-Plan.Kern.slnf …`, Projekt- und Lösungspfade absolut —
  der Muxer liest `global.json` nur vom Arbeitsverzeichnis aufwärts). Die Sitzung „Dialoge und
  Korrekturen“ beschreibt in
  [`Dialoge/2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md`](Dialoge/2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md)
  (Abschnitt 3, Cloud-Umgebung) den Bezug eines SDK 10.0.401 aus conda-forge, das `global.json`
  erfüllt; die Microsoft-Downloadhosts sind gesperrt. Erst `dotnet --version` im Repo prüfen.
- **LFS:** nach jedem Klon, Merge oder Wechsel
  `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite` (ersatzweise `git lfs fetch origin
  origin/<zweig> --include=… && git lfs checkout`); die Datei hat rund 96 MB, ein 130-Byte-Zeiger
  macht Tests und Referenzlauf rot.
- **GitHub:** hier war `gh` verfügbar (`gh run list --repo inekon/EPOS-Plan --branch <zweig>
  --workflow kern.yml --json databaseId,status,conclusion,headSha`, `gh run view <id>
  --log-failed`); sonst die GitHub-MCP-Werkzeuge (`actions_list`, `get_job_logs`).
- **Node:** Playwright und Chromium vorinstalliert (`PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers`, nie
  `playwright install`); das Modul `docx` global installieren und per `NODE_PATH` finden.
- **Stop-Hook:** mahnt ungepushte Commits an; während einer Welle ist das der laufende Merge —
  nicht pushen, bis Gate und Statuszeile stehen.

**Nie ohne Rückfrage:** iOS-, macOS- und Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags.
Keine Zugangsdaten ins Repositorium. **Antworten:** auf Deutsch, knapp; Tabellen für Stände.

## 4 Offene Punkte (mit Empfehlung)

**(a) Beim Anwender**

| Punkt | Empfehlung |
|---|---|
| Sichtabnahme unter Windows nach einem Neubau des Sitzungszweigs: die sieben Fälle aus dem Statuspunkt „Nach #1003“ (Geste sofort nach dem Öffnen des Über-Dialogs, nach Klick auf die freie Fläche, nach Klick auf „Kopieren“; Escape; F12 allein nur im Debug-Build; F1 im Hauptfenster bei Fokus im Eingabefeld genau einmal; kein Rollbalken bei kurzem Inhalt), dazu „Quick Assist starten“ mit und ohne Quick Assist, Menüsperre je Lizenzstatus, Rubrik „Zapfprofil“ ohne Entwicklermodus, Supportpaket-ZIP (Inhalt, kein `.sqlite`), „Diagnosedaten kopieren“ | Der Anwender prüft; Befunde als eigener Auftrag |
| Microsoft-Konto des Supports für Quick Assist (Konzept 4.8) | Anwender/INEKON; bis dahin „Fernwartungsseite öffnen“ |
| Fernwartungsseite `https://epos-plan.de/support/fernwartung` als Anleitung (Schritte mit Bildern, Telefon 0711 9959 826 50, werktags 9–16 Uhr, kein Download) | INEKON; der Dialog und das Wiki verweisen schon darauf |
| Wiki-Sammelupload: Fernwartung (neu), Einstellungen (Reiter Zapfprofil), Über EPOS-Plan, Installation und Update; Logbuch-Sätze aus Konzept 8 | Nur auf Zuruf, Versionsnummer erfragen; vorher Live-Stand gegen die Repo-Quelle lesen (`Werkzeuge/WikiUpload/LIESMICH.md`) |
| Entscheid: deutscher Produktname „Remotehilfe“ neben „Quick Assist“ in Dialog und Wiki | Empfehlung: „Quick Assist (Remotehilfe)“ in Knopf-Hinweis und Wiki ergänzen — kleine Welle, beide Sprachen |
| Entscheid: Schlussleiste des Über-Dialogs steht bei kurzem Inhalt unter dem Inhalt, nicht am Fensterrand (`margin-top` 10 px; gilt für alle Dialoge im eigenen Fenster) | Empfehlung: belassen, bis die Sichtabnahme es als störend zeigt; sonst `margin-top: auto` mit Fensterprobe |
| AS-Q6 (a), Rest: `ki_aktionen.txt` neben die übrigen Protokolle nach `Logs\` mit `%TEMP%`-Rückfall | Auf Zuruf; das Supportpaket liest die Datei schon an beiden Orten |
| Stufe 4: Supportvertrag im Lizenz-Token (`support_bis`, AS-Q8 d) | Auf Zuruf; Serverseite rollt der Anwender aus |
| iPad-Fernwartung (AS-Q10) | Zurückgestellt; Befund für dann in Konzept 4.5 |
| sqproj-Befund „Import bleibt stehen“ (#996) | Liegt bei der Sitzung „Gebäudesimulation“ |

**(b) Technisch**

| Punkt | Empfehlung |
|---|---|
| Das Wiki nennt bei „Über EPOS-Plan“ den Menüpunkt *Hilfe → Version*; die iOS-Hülle zeigt diesen Punkt nicht und kennt `Fernwartung` nicht als Ziel (`AppWurzel.FuehrtZiel`) | Belassen, bis das iPad an der Reihe ist |
| Alte Dubletten in der Statustabelle (#302, #304, #328, #331, #556) und die fremden Zeilen #1000/#1001, die außerhalb der Reihenfolge stehen | Fremd; nicht anfassen, nur bekannt |

**(c) Nächste Arbeit der neuen Sitzung**

1. Umgebung prüfen (SDK, LFS, `gh` oder GitHub-MCP, Modul `docx`); den Sitzungszweig der neuen
   Umgebung auf `origin/claude/gracious-brown-qx5kb2` aufsetzen (Merge, kein Rebase) und ab dann
   mitziehen.
2. Kern-Lauf (kern.yml) des alten Sitzungszweigs auf seiner Zweigspitze prüfen; grün → CI-Vermerk in
   Zeile #1033 (b) mit `[skip ci]`; rot → nach Dateien aufschlüsseln, Eigenes beheben, Fremdes melden.
3. Mit Freigabe des Anwenders den Stand per Merge in `ios_migration_september` nehmen und pushen —
   vorher die Statusnummern gegen origin messen (#1003 ist eine Lücke der Tabelle und bleibt; #1033
   zieht um, wenn vergeben).
4. Die offenen Punkte aus (a) gesammelt mit Empfehlung vorlegen; dann auf Zuruf: „Remotehilfe“,
   Schlussleiste, `ki_aktionen.txt`, Stufe 4, Wiki-Upload.

## 5 Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Administration und Support“ für EPOS-Plan (Repo inekon/EPOS-Plan,
Arbeitszweig ios_migration_september) in einem neuen Konto. Lies zuerst CLAUDE.md und
Dokumentation/aktuell/2026-10-12_Uebergabe_Administration_Support_Kontowechsel.md ganz; aus
Dokumentation/aktuell/Status_iOS_Migration.md nur den Kopf und die Zeilen #974, #985, #986, #987,
#1003, #1012 und #1033 samt den Punkten „Nach #…“ am Ende von Abschnitt 4; Konzept und
Entwicklerhandbuch nur gezielt (Überschriften mit grep -n '^#').

Stand: Der alte Sitzungszweig claude/gracious-brown-qx5kb2 trägt den Merge des Arbeitszweigs
(#1032) in EG3 und Quick Assist (6f348bdb, Gate grün) und das Übergabepapier; er ist noch nicht im
Arbeitszweig. Setze deinen Sitzungszweig (den die Umgebung vorgibt) darauf auf:
git fetch origin claude/gracious-brown-qx5kb2 ios_migration_september &&
git checkout -B <dein Zweig> origin/claude/gracious-brown-qx5kb2; danach LFS-Testdatenbank holen.

Erste Schritte: (1) Umgebung prüfen: dotnet --version (global.json verlangt 10.0.400/latestFeature;
reicht das SDK nicht, Abschnitt 3 „Cloud-Umgebung“ des Papiers), LFS-Datenbank rund 96 MB (kein
130-Byte-Zeiger), Node-Modul docx für Werkzeuge/Entwicklerhandbuch/erzeugen.js. (2) Mit
git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september prüfen, was seit dem
Papier hinzukam, und die Statusnummern gegen origin messen (#1003 bleibt, #1033 zieht um, wenn
vergeben). (3) Den Kern-Lauf (kern.yml) des alten Sitzungszweigs auf seiner Spitze prüfen und den
CI-Vermerk in Zeile #1033 (b) setzen — bei Rot zuerst beheben. (4) Mit meiner Freigabe den Stand per
Merge in ios_migration_september nehmen und beide Zweige pushen; reine Papier-Pushes mit [skip ci].
(5) Den Stand in höchstens fünf Zeilen melden und die offenen Punkte aus Abschnitt 4 (a) mit
Empfehlung vorlegen.

Arbeitsweise und Grenzen: Fable orchestriert nur; Umsetzung opus-umsetzung, Mechanik
sonnet-mechanik, Zählungen haiku-pruefung, Modell immer ausdrücklich setzen; Agenten in Worktrees,
Gate und Wellenfolge nach Abschnitt 3; Statusnummer unmittelbar vor dem Push gegen origin messen;
Übergabepapier bei jedem Push nachführen. Die Sitzungen „Gebäudesimulation“, „Kälteanlagen“ und
„Dialoge und Korrekturen“ laufen im alten Konto und sind per Sitzungsnachricht nicht erreichbar —
Abstimmung nur über origin und mich. Nie ohne Rückfrage: iOS-, macOS-, Setup-Läufe, Wiki-Upload,
Pull Requests, Force-Push, Tags. Keine Zugangsdaten ins Repositorium. Antworten auf Deutsch, knapp;
Tabellen für Stände.
```
