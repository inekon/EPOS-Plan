# Konzept: Repository aufräumen — was bleibt, was geht, und die Regel danach

**Stand 12.09.2026 — Anwenderauftrag: „Räume alte nicht mehr genutzte Läufe/Verzeichnisse auf" und „Plane: nicht benötigte
Dateien/Verzeichnisse löschen, wenn nicht mehr benötigt. Räume auf."**

## 1. Die Regel

Eine Datei oder ein Verzeichnis bleibt im Repository, solange **eines** davon zutrifft: ein Build, ein Test, ein Workflow, das Setup
oder ein Werkzeug liest es; ein gültiges Konzept oder eine Auslieferung braucht es als Quelle; ein Anwenderentscheid verlangt es.
Trifft nichts davon zu, geht es per `git rm` — die Git-Geschichte behält es, ein Zurückholen ist jederzeit möglich
(`git show <stand>:<pfad>`). Der Beleg für „nicht mehr benötigt" ist immer derselbe: `git grep` ohne Treffer außerhalb der Protokolle,
kein Eintrag in `.sln`, `.slnf`, `.csproj`, `.yml`, `.iss`, `.ps1`, kein Anwenderentscheid dagegen.

Drei Dinge gehören **nie** ins Repository: Arbeitsordner (`.work/`), Kopien oder Sicherungen von Datenbanken (`*.accdb`, `*.sqlite` außer
der Testdatenbank) und Sicherungskopien von Quelltexten (`*.bak`, `*.orig`, `*.original-*`). Die `.gitignore` schließt sie aus; eine Wache
prüft, dass nichts davon versioniert ist. Was seine Aufgabe erfüllt hat — ein Spike, ein Prüfprogramm, ein Gerüst-Archiv, eine
Sicherung — wird **in demselben Auftrag** entfernt, der es überflüssig macht, nicht „später".

## 2. Inventar (Stand 32bdb84d, versionierte Dateien)

| Verzeichnis | Zweck | Umfang | Wird gebraucht von | Einstufung |
|---|---|---|---|---|
| `EPOS.Kern`, `EPOS.UI`, `EPOS.UI.Daten`, `KiKern`, `SpeicherEngine`, `SpeicherPlanung`, `EPOS.Referenzlauf`, `Referenzlauf`, `WindowsFormsApplication1`, `EPOS.iOS` und ihre Testprojekte | Programm | ~33 MB | Projektmappen, CI | bleibt |
| `Referenzlaeufe/` | Basis R7 (346 Dateien), `Kenndaten_Test.sqlite` (67,5 MB), Importproben, Skripte | 114 MB | Gate, `kern.yml`, 13 Testklassen | bleibt; historische Basen seit 11.09. nur in der Geschichte (SYNC‑Q1) |
| `VDI-3805-Daten/` | Auslieferung der Herstellerdaten (Setup-Komponente, W6‑O‑9), CEC-Listen, bslib | 186 MB | Setup, Importe, 40 Verweise | bleibt |
| `Werkzeuge/`, `Proben/`, `Setup/`, `sql/` (Schema- und Reparaturskripte), `EposSqliteMigrator/` | Hauswerkzeuge | 5 MB | CLAUDE.md-Werkzeugtabelle, Setup, Gate | bleibt |
| `Projekte/` | Konzepte (→ Dokumentation seit #241), Wiki-Quellen, Referenzpaket `Speichersimulation/`, fünf docx, Mockup, `.wpx` | 9 MB | Konzepte, Wiki-Upload; docx nach SP‑O‑9 | bleibt |
| `BHKWPlan/`, `PV-Konzept_PV-Now/`, `Quellen/`, `VALERI/`, `Mockups/` | Fremdquellen und Entwürfe, die Konzepte und Tests zitieren | 10 MB | Konzepte, zwei Testklassen | bleibt (Stufe 2: unter `Quellen/` sammeln, Vorschlag) |
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

**Stufe 1 — umgesetzt #242 (Commit `46f1f0ab`).** `.work/` (71 MB), `DB-Backup/`, vier `.bak`, `sqlite-probe/`, vier Lizenzserver-Originale,
`Reporting_Geruest.zip` entfernen; Entwurf nach `Mockups/`, Scraper-Reste nach `Dokumentation/ueberholt/`; `.work/` in die `.gitignore`;
Verweise kennzeichnen; **Wache `RepositoryOrdnungWacheTests`** (kein `*.bak`, `*.orig`, `*.original-*`, `*.accdb`, `*.laccdb`, kein
`.work/`, kein `DB-Backup/`, `*.sqlite` nur auf der Weißliste `Referenzlaeufe/Kenndaten_Test.sqlite`); Aufräumregel als Abschnitt in der
Wurzel-`CLAUDE.md`. Ergebnis: Arbeitsbaum um ~71 MB kleiner, keine Kundendaten mehr im Baum.

**Stufe 2 — Vorschläge, je Punkt ein Anwenderentscheid.**
- **Git-Geschichte verkleinern.** Die 24 Referenzbasen (1 GB) und die 70-MB-Datenbankkopie bleiben in der Geschichte; jeder Klon
  trägt sie. Ein Umschreiben der Geschichte (`git filter-repo`) brächte den Klon von ~1,5 GB auf einen Bruchteil, verlangt aber einen
  Force-Push aller Zweige und ein frisches Klonen auf beiden Rechnern. Empfehlung: nur, wenn die Klongröße wirklich stört; dann als
  eigener, angekündigter Schritt an einem Tag ohne Sync.
- **Git-LFS — entschieden 12.09.2026 (AUF‑Q2 „Nehme VDI-Archive und Testdatenbanken in git-lfs"), Umsetzung Stufe 3 / #243.**
  Ab dem LFS-Commit liegen `Referenzlaeufe/Kenndaten_Test.sqlite` (68 MB, bisher 12 Fassungen in der Geschichte) und die 68 VDI-Archive
  (`VDI-3805-Daten/**/*.zip|*.vdi|*.VDI`, 97 MB) als LFS-Objekte; CSV, PAN, PDF, XLSX und die Importproben bleiben normale Blobs. Die
  alten Blobs bleiben in der Geschichte (kein Umschreiben, AUF‑Q1 offen). Die Workflows ziehen GEZIELT (Testläufe nur die Testdatenbank,
  mit Cache; der Installer-Job alles), Tests und Wache brechen mit klarer Meldung ab, wenn eine Zeigerdatei statt der Datenbank liegt.
  Zu wissen: GitHub gibt frei 1 GB LFS-Speicher und 1 GB Bandbreite je Monat — jede neue Fassung der Testdatenbank kostet 68 MB Speicher,
  jeder Abruf ohne Cache 68 MB Bandbreite; bei Bedarf ein Datenpaket (50 GB / 5 US-$ je Monat). Jeder Rechner braucht einmal
  `git lfs install`, sonst kommen Zeigerdateien an (Git für Windows bringt LFS mit).
- **Fremdquellen sammeln — entschieden 12.09.2026 (AUF‑Q3 „Empfehlung umsetzen"), Umsetzung #243.** `BHKWPlan/`, `PV-Konzept_PV-Now/`,
  `VALERI/` ziehen unter `Quellen/` (dort liegen die Emissionsfaktoren schon), `Mockups/` bleibt die eine Adresse für Entwürfe. Reine
  Verschiebung, Verweise nachziehen.
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
| AUF‑Q1 | Git-Geschichte umschreiben (Klongröße)? | offen |
| AUF‑Q2 | LFS-Regeln entfernen; Testdatenbank/VDI-Archive nach LFS? | offen |
| AUF‑Q3 | Fremdquellen unter `Quellen/` sammeln? | offen |
| AUF‑Q4 | `retention-days` 14 in den Workflows? | offen |
