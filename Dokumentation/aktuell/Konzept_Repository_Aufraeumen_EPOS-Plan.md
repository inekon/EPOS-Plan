# Konzept: Repository aufräumen — was bleibt, was geht, und die Regel danach

**Stand 12.09.2026 — Anwenderauftrag: „Räume alte nicht mehr genutzte Läufe/Verzeichnisse auf" und „Plane: nicht benötigte
Dateien/Verzeichnisse löschen, wenn nicht mehr benötigt. Räume auf."**

## 1. Die Regel

Eine Datei oder ein Verzeichnis bleibt im Repository, solange **eines** davon zutrifft: ein Build, ein Test, ein Workflow, das Setup
oder ein Werkzeug liest es; ein gültiges Konzept oder eine Auslieferung braucht es als Quelle; ein Anwenderentscheid verlangt es.
Trifft nichts davon zu, geht es per `git rm` — die Git-Geschichte behält es, ein Zurückholen ist jederzeit möglich
(`git show <stand>:<pfad>`). **Ausnahme seit AUF‑Q1 (12.09.2026):** Was Stufe 4 aus der Geschichte entfernt hat, ist auch so nicht mehr
abrufbar; deshalb gehört zu jedem solchen Schritt die Sicherung dessen, was erklärt, warum etwas so wurde, wie es ist (die Protokolle
der 24 Referenzbasen liegen darum unter `Dokumentation/ueberholt/Referenzbasen/`).
Der Beleg für „nicht mehr benötigt" ist immer derselbe: `git grep` ohne Treffer außerhalb der Protokolle,
kein Eintrag in `.sln`, `.slnf`, `.csproj`, `.yml`, `.iss`, `.ps1`, kein Anwenderentscheid dagegen.

Drei Dinge gehören **nie** ins Repository: Arbeitsordner (`.work/`), Kopien oder Sicherungen von Datenbanken (`*.accdb`, `*.sqlite` außer
der Testdatenbank) und Sicherungskopien von Quelltexten (`*.bak`, `*.orig`, `*.original-*`). Die `.gitignore` schließt sie aus; eine Wache
prüft, dass nichts davon versioniert ist. Was seine Aufgabe erfüllt hat — ein Spike, ein Prüfprogramm, ein Gerüst-Archiv, eine
Sicherung — wird **in demselben Auftrag** entfernt, der es überflüssig macht, nicht „später".

## 2. Inventar (Stand 32bdb84d, versionierte Dateien)

| Verzeichnis | Zweck | Umfang | Wird gebraucht von | Einstufung |
|---|---|---|---|---|
| `EPOS.Kern`, `EPOS.UI`, `EPOS.UI.Daten`, `KiKern`, `SpeicherEngine`, `SpeicherPlanung`, `EPOS.Referenzlauf`, `Referenzlauf`, `WindowsFormsApplication1`, `EPOS.iOS` und ihre Testprojekte | Programm | ~33 MB | Projektmappen, CI | bleibt |
| `Referenzlaeufe/` | Basis R7 (346 Dateien), `Kenndaten_Test.sqlite` (67,5 MB), Importproben, Skripte | 114 MB | Gate, `kern.yml`, 13 Testklassen | bleibt; die 24 historischen Basen sind seit 11.09. aus dem Arbeitsbaum (SYNC‑Q1) und seit 12.09. aus der Geschichte (AUF‑Q1, Stufe 4) — ihre Protokolle unter `Dokumentation/ueberholt/Referenzbasen/` |
| `VDI-3805-Daten/` | Auslieferung der Herstellerdaten (Setup-Komponente, W6‑O‑9), CEC-Listen, bslib | 186 MB | Setup, Importe, 40 Verweise | bleibt |
| `Werkzeuge/`, `Proben/`, `Setup/`, `sql/` (Schema- und Reparaturskripte), `EposSqliteMigrator/` | Hauswerkzeuge | 5 MB | CLAUDE.md-Werkzeugtabelle, Setup, Gate | bleibt |
| `Projekte/` | Konzepte (→ Dokumentation seit #241), Wiki-Quellen, Referenzpaket `Speichersimulation/`, fünf docx, Mockup, `.wpx` | 9 MB | Konzepte, Wiki-Upload; docx nach SP‑O‑9 | bleibt |
| `Quellen/` (seit #243 mit `BHKWPlan/`, `PV-Now/`, `VALERI/`, `Emissionsfaktoren/`), `Mockups/` | Fremdquellen und Entwürfe, die Konzepte und Tests zitieren | 10 MB | Konzepte, zwei Testklassen | bleibt; zusammengezogen mit #243 (AUF‑Q3) |
| `Lizenzserver/` | WordPress-Plugin 1.4.1 + Einbauanleitung | 268 KB | Lizenzkonzept | bleibt; die vier `*.original-2026-08-19` gehen (Stufe 1) |
| `EPOS-Plan_Beispiele_Geruest/` | Gerüst der Projektbeispiele | 52 KB | Anwenderentscheid 12.09.2026 „ist wichtig" | bleibt |
| `.work/` | Arbeitsordner der Windows-Seite: Einmal-Prüfprogramm, Bericht, **70-MB-Kopie der Produktivdatenbank** | 71 MB | nichts (Bericht liegt seit #241 in `Dokumentation/ueberholt`) | **geht (Stufe 1)** — Anwenderentscheid 12.09.2026 „Lösche .work", Rücknahme von SP‑O‑9 |
| `DB-Backup/` | 16 Git-LFS-Zeiger auf Access-Sicherungen | 2 KB im Baum | nichts; seit 02.09. per `.gitignore` ausgeschlossen | **geht (Stufe 1)** |
| `sqlite-probe/` | Spike vor der SQLite-Umstellung (31.08.) | 104 KB | nichts | **geht (Stufe 1)** |
| `WindowsFormsApplication1/**/*.bak` (4), `Allgemein/Reporting/Reporting_Geruest.zip` | Sicherungskopien, Gerüst-Archiv | 60 KB | nichts | **geht (Stufe 1)** |
| `WindowsFormsApplication1/Allgemein/Simulation/Entwurf_Hydraulikuebersicht_Konfiguration.html`, `Allgemein/vdi_3805_importer/*` | Entwurf, Überreste eines nicht mehr versionierten Scrapers | klein | Protokolle | **verschieben (Stufe 1)** nach `Mockups/` bzw. `Dokumentation/ueberholt/` |
| 303 Markdown-Dokumente | Konzepte, Doku, Protokolle | 12 MB | Claude, Anwender | seit #241 unter `Dokumentation/aktuell` und `Dokumentation/ueberholt` |

Außerhalb des Arbeitsbaums: elf alte Fernzweige (Anwender 12.09.2026: „vorerst nicht" löschen); alte GitHub-Actions-Läufe (mit den
hier verfügbaren Werkzeugen nicht löschbar — im Browser unter Actions je Lauf, oder Aufbewahrungsfrist `retention-days` in den Workflows);
das Sitzungs-Scratchpad der Orchestrierung (12.09. von 3,4 GB auf 220 MB bereinigt).

## 3. Stufenplan

**Stufe 0 — erledigt.** Historische Referenzbasen aus dem Arbeitsbaum (SYNC‑Q1, 11.09.2026, 1 GB); Scratchpad bereinigt (12.09.2026).

**Stufe 1 — umgesetzt #242 (Commit `f6464b88`, Merge `d2e15bb1`).** `.work/` (71 MB), `DB-Backup/`, vier `.bak`, `sqlite-probe/`, vier Lizenzserver-Originale,
`Reporting_Geruest.zip` entfernen; Entwurf nach `Mockups/`, Scraper-Reste nach `Dokumentation/ueberholt/`; `.work/` in die `.gitignore`;
Verweise kennzeichnen; **Wache `RepositoryOrdnungWacheTests`** (kein `*.bak`, `*.orig`, `*.original-*`, `*.accdb`, `*.laccdb`, kein
`.work/`, kein `DB-Backup/`, `*.sqlite` nur auf der Weißliste `Referenzlaeufe/Kenndaten_Test.sqlite`); Aufräumregel als Abschnitt in der
Wurzel-`CLAUDE.md`. Ergebnis: Arbeitsbaum um ~71 MB kleiner, keine Kundendaten mehr im Baum.

**Stufe 3 — umgesetzt #243 (12.09.2026).** Die zwei entschiedenen Punkte der Stufe 2:

- **Git LFS (AUF‑Q2).** Die `.gitattributes` trägt vier neue Regeln — `Referenzlaeufe/Kenndaten_Test.sqlite` und
  `VDI-3805-Daten/**/*.zip|*.vdi|*.VDI`; die vier Access-Zeilen sind gefallen (seit #242 gibt es keine `.accdb` mehr im Repository).
  Der Bestand ist **ohne Geschichtsumschreibung** überführt (`git rm --cached` + `git add` mit aktivem Clean-Filter): 69 LFS-Dateien,
  rund 165 MB; die Arbeitsdateien sind byte-gleich geblieben, die alten Blobs blieben zunächst in der Geschichte und sind mit Stufe 4
  (AUF‑Q1) daraus entfernt worden. Die drei
  Workflows checken **ohne** `lfs: true` aus und ziehen gezielt (`kern.yml`, `windows.yml`/`build-test`, `ios.yml` nur die
  Testdatenbank, mit `actions/cache` auf `.git/lfs`; allein `windows.yml`/`installer` vollständig, weil das Setup `VDI-3805-Daten\*`
  einpackt) und prüfen je Job, dass keine Zeigerdatei liegengeblieben ist. Denselben Schutz tragen die drei Öffnungsstellen im Code
  (`EPOS.Kern.Tests/TestDatenbank.cs`, `Referenzlauf/DbUmgebung.cs`, `Werkzeuge/Auslieferungsvorlage/Argumente.cs`) und zwei neue Fälle
  der Wache. Einrichtung und Bandbreitenregel: `Referenzlaeufe/LIESMICH.md`, Abschnitt „Git LFS".
- **Fremdquellen (AUF‑Q3).** `BHKWPlan/`, `PV-Konzept_PV-Now/` und `VALERI/` liegen als `Quellen/BHKWPlan/`, `Quellen/PV-Now/` und
  `Quellen/VALERI/` neben den Emissionsfaktoren — elf Dateien, reine Verschiebung mit `git mv`, kein Inhalt geändert. Nachgezogen sind
  die Pfadangaben in vier Konzepten (Kosten/Energieträger, Wirtschaftlichkeit konsolidiert, Photovoltaik-Wirtschaftlichkeit und dieses);
  die übrigen Fundstellen nennen nur Dateinamen („`VALERI_Vorlage_V7.xlsx`") oder einen fremden Ablageort (`Z:\…\BHKWPlan\…`) und
  bleiben, wie sie sind. `Mockups/` ist unberührt die eine Adresse für Entwürfe.

Dazu kam als Nachzug aus dem Gate zu #242: Die Wache `RepositoryOrdnungWacheTests` prüft seither nur noch **versionierte** Dateien
(`git ls-files -z`) — über das Dateisystem traf sie die Arbeitskopie des Referenzlaufs und die Agenten-Arbeitsbäume unter
`.claude/worktrees/`, beides gitignored.

**Stufe 4 — die Git-Geschichte umschreiben. Anwenderentscheid AUF‑Q1 vom 12.09.2026: „ausführen". Umsetzung Auftrag #244.**

Das ist der einzige Schritt dieses Konzepts, der **nicht** rückholbar ist: Er entfernt Inhalte aus **jedem** Commit, nicht nur aus dem
Arbeitsbaum. Deshalb steht er zuletzt, deshalb bekommt er einen eigenen Tag ohne Sync, und deshalb geht ihm eine Sicherung voraus.

**Was entfernt wird.** Vier Gruppen, zusammen der Löwenanteil der ~1,5 GB Klongröße:

1. die **24 historischen Referenzbasen** unter `Referenzlaeufe/` (rund 7 700 CSV, 1 016,7 MB) — seit SYNC‑Q1 ohnehin aus dem Arbeitsbaum;
2. der Arbeitsordner **`.work/`** mit der 70-MB-Kopie der Produktivdatenbank (30 Projekte mit Kundennamen) — seit #242 aus dem Arbeitsbaum;
3. die LFS-Zeiger unter **`DB-Backup/`** — seit #242 aus dem Arbeitsbaum;
4. die **historischen Fassungen** von `Referenzlaeufe/Kenndaten_Test.sqlite` und der VDI-Archive (`VDI-3805-Daten/**/*.zip|*.vdi|*.VDI`)
   als Blobs. Die AKTUELLEN Fassungen bleiben — sie liegen seit #243 (AUF‑Q2) als LFS-Zeiger in der Geschichte; entfernt werden nur die
   älteren Blob-Fassungen, die vor dem LFS-Wechsel entstanden sind.

**Was vorher gesichert wird (#244A).** Die **Protokolle** aller 24 Basen — `lauf_protokoll.md` beziehungsweise `protokoll.txt`, bei
`2026-09-07_M7_nach-Merge7` zusätzlich `vergleich_M5_zu_M7.txt` — byte-gleich nach
[`Dokumentation/ueberholt/Referenzbasen/`](../ueberholt/Referenzbasen/LIESMICH.md): 25 Dateien, 603 913 Byte. Damit bleibt die Herleitung
jeder Basiswahl im Repository; die **Messdaten** (CSV) sind endgültig weg. Im selben Schritt sind alle Aussagen „liegt in der
Git-Geschichte (Stand …)" in den Regelquellen richtiggestellt worden.

**Wie umgeschrieben wird.** Auf einem **Spiegelklon** (`git clone --mirror`), nie im Arbeitsbaum, und ausschließlich mit
**`git filter-repo`** — `git filter-branch` ist langsam, fehleranfällig und von Git selbst abgeraten und wird hier nicht benutzt. Erst
die Pfade der Gruppen 1–3, dann der Blob-Strip der Gruppe 4.

**Verifikation vor dem Force-Push.** Der **Baum** des Arbeitszweigs muss vorher und nachher derselbe sein
(`git rev-parse <zweig>^{tree}` im alten und im neuen Klon) — nur die Geschichte darf sich ändern, nicht der Stand. Dazu: Zahl der
Commits je Zweig gleich, kein Zweig und kein Tag verloren, die neue Klongröße gemessen.

**Force-Push.** Alle **13 Zweige** und das Tag `vor-W16`, jeder mit `--force-with-lease`, damit ein zwischenzeitlicher fremder Push nicht
still überschrieben wird.

**Die Commit-Karte.** `git filter-repo` schreibt die Zuordnung alt → neu; sie kommt als
`Dokumentation/ueberholt/Geschichte/commit-map_2026-09-12.txt` ins Repository (eine Textdatei, kein Papier — sie braucht deshalb keine
Zeile im Dokumentationsindex, wohl aber diese Erwähnung).

**Die vier Folgen — sie gelten dauerhaft:**

- **(a) Jede Commit-Kennung in einem Dokument von vor dem 12.09.2026 ist eine ALTE Kennung** und trifft im heutigen Repository nichts.
  Übersetzt wird sie über die Commit-Karte. Statusblöcke, Protokolle und Nachweisdokumente werden deswegen **nicht** umgeschrieben —
  sie sind datierte Geschichte; die Karte ist der Schlüssel dazu.
- **(b) Die Signaturen der umgeschriebenen Commits entfallen.** Eine Signatur gilt für einen bestimmten Commit-Inhalt; wird der Inhalt
  neu geschrieben, ist sie ungültig und wird entfernt. Neue Commits sind wieder signiert.
- **(c) Jeder Rechner klont neu.** Aus einem alten Klon wird **nie wieder gepusht** — schon gar nicht mit Force: Das brächte die alte
  Geschichte samt der 1 GB und der Kundendatenbank zurück. Wer einen alten Klon noch braucht, benennt ihn um und behandelt ihn als
  Archiv.
- **(d) GitHub gibt den Speicher erst nach seiner eigenen Bereinigung frei.** Die alten Objekte bleiben dort noch eine Weile erreichbar
  (unreferenziert, über die API auffindbar); die Klongröße sinkt sofort, der Serverplatz später.

**Stufe 2 — Vorschläge, je Punkt ein Anwenderentscheid.**
- **Git-Geschichte verkleinern — entschieden 12.09.2026 („AUF‑Q1: ausführen"), umgesetzt Stufe 4 / #244.** Die 24 Referenzbasen (1 GB)
  und die 70-MB-Datenbankkopie trug bis dahin jeder Klon; ein Umschreiben der Geschichte (`git filter-repo`) bringt ihn von ~1,5 GB auf
  einen Bruchteil, verlangt aber einen Force-Push aller Zweige und ein frisches Klonen auf jedem Rechner. Verfahren und Folgen:
  Abschnitt „Stufe 4".
- **Git-LFS — entschieden 12.09.2026 (AUF‑Q2 „Nehme VDI-Archive und Testdatenbanken in git-lfs"), umgesetzt #243 (Stufe 3).**
  Ab dem LFS-Commit liegen `Referenzlaeufe/Kenndaten_Test.sqlite` (68 MB, bisher 12 Fassungen in der Geschichte) und die 68 VDI-Archive
  (`VDI-3805-Daten/**/*.zip|*.vdi|*.VDI`, 97 MB) als LFS-Objekte; CSV, PAN, PDF, XLSX und die Importproben bleiben normale Blobs. Die
  alten Blobs blieben dabei in der Geschichte (kein Umschreiben in #243); Stufe 4 hat sie mit AUF‑Q1 entfernt. Die Workflows ziehen GEZIELT (Testläufe nur die Testdatenbank,
  mit Cache; der Installer-Job alles), Tests und Wache brechen mit klarer Meldung ab, wenn eine Zeigerdatei statt der Datenbank liegt.
  Zu wissen: GitHub gibt frei 1 GB LFS-Speicher und 1 GB Bandbreite je Monat — jede neue Fassung der Testdatenbank kostet 68 MB Speicher,
  jeder Abruf ohne Cache 68 MB Bandbreite; bei Bedarf ein Datenpaket (50 GB / 5 US-$ je Monat). Jeder Rechner braucht einmal
  `git lfs install`, sonst kommen Zeigerdateien an (Git für Windows bringt LFS mit).
- **Fremdquellen sammeln — entschieden 12.09.2026 (AUF‑Q3 „Empfehlung umsetzen"), umgesetzt #243 (Stufe 3).** `BHKWPlan/`,
  `PV-Konzept_PV-Now/` und `VALERI/` sind unter `Quellen/` gezogen (dort lagen die Emissionsfaktoren schon), `Mockups/` bleibt die eine
  Adresse für Entwürfe. Reine Verschiebung, Verweise nachgezogen.
- **`WindowsFormsApplication1/Allgemein/Update/SchemaVersionAccess.cs` und `EposSqliteMigrator/`.** Die letzten Access-Bezüge im Code;
  nach der Anwenderregel „nichts zu Access" prüfen, ob das Hauswerkzeug noch gebraucht wird (BETRIEB_SQLITE 1.1/7 sagt: für die Übernahme
  eines Altbestands). Entscheid: behalten, solange ein Altbestand denkbar ist.
- **Fernzweige.** Zehn Zweige ohne Bewegung seit August bzw. dem Zweigwechsel (`Pufferspeicher`, `b5b_lokal`, `kostenformulare`,
  `lokal_dirk`, `pv-ertragsmodell-rechner2`, `sicherung-lokal`, `sqlite`, `version_august_2026`, `ios_migration`, zwei `claude/`-Zweige):
  vorerst nicht (Anwender 12.09.2026).
- **GitHub Actions.** Die Aufbewahrungsfrist der Artefakte steht in allen drei Workflows bereits auf `retention-days: 14` (Vorgabe
  wären 90) — der ursprüngliche Vorschlag war gegenstandslos, nichts zu tun. Alte Läufe löscht nur der Anwender im Browser.

## 4. Entscheide und offene Fragen

| Kennung | Inhalt | Stand |
|---|---|---|
| SP‑O‑9 (11.09.2026) | `.work/` und fünf docx im Zweig belassen | **`.work/` zurückgenommen 12.09.2026** („Lösche .work"); docx bleiben |
| AUF‑E‑1 (12.09.2026) | `EPOS-Plan_Beispiele_Geruest/` ist wichtig | bleibt |
| AUF‑E‑2 (12.09.2026) | Fernzweige löschen | vorerst nicht |
| AUF‑Q1 | Git-Geschichte umschreiben (Klongröße)? | **entschieden 12.09.2026** („AUF‑Q1: ausführen"), **umgesetzt #244** (Stufe 4; Belege vorher gesichert mit #244A) |
| AUF‑Q2 | LFS-Regeln entfernen; Testdatenbank/VDI-Archive nach LFS? | **entschieden 12.09.2026** („Nehme VDI-Archive und Testdatenbanken in git-lfs"), **umgesetzt #243** |
| AUF‑Q3 | Fremdquellen unter `Quellen/` sammeln? | **entschieden 12.09.2026** („Setze Empfehlung um"), **umgesetzt #243** |
| AUF‑Q4 | `retention-days` 14 in den Workflows? | **gegenstandslos** — stand bereits in allen drei Workflows (12.09.2026) |
