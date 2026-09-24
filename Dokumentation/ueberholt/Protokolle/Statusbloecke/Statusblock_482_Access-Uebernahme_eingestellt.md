# Statusblock #482 — Übernahme aus Access endgültig eingestellt (24.09.2026)

Der ausführliche Block zur Statuszeile #482 in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Der Auftrag lautete:
den Widerspruch zwischen `BETRIEB_SQLITE.md` (Abschnitte 1.1 und 7 beschreiben das Hauswerkzeug
`EposSqliteMigrator.exe` als einzigen Weg, einen `.accdb`-Altbestand zu übernehmen) und dem
Repository (der Ordner `EposSqliteMigrator/` fehlt seit dem 13.09.2026) mit dem Anwender klären
und dann umsetzen — entweder das Werkzeug zurückholen oder die Übernahme endgültig streichen.

## 1 Befund

- **Wie das Werkzeug verschwand.** Nicht durch einen Entscheid, sondern durch den Sync-Commit
  `43aaf985` vom 13.09.2026 („Synchronisation vom 13.09.2026 13:26:02,98“, GitHub_Sync), der die
  zehn Dateien (1 493 Zeilen) entfernte. Einen Tag zuvor (`921f6fc8`) hielt das Aufräumkonzept noch
  „Entscheid: behalten, solange ein Altbestand denkbar ist“ fest. Der Commit `769a8fcb` bereinigte
  danach nur `WP-Plan.sln`, `windows.yml` und zwei `CLAUDE.md`; BETRIEB_SQLITE.md und sieben
  weitere Papiere unter `aktuell/` beschrieben das Werkzeug weiter als vorhanden.
- **Rückholung wäre einfach gewesen.** Letzter Stand mit Werkzeug: `b0647c7e`. Im Scratch-Ordner
  gegen die heutigen Paketfassungen gebaut: 0 Fehler, 0 Warnungen (beide Pakete stehen weiter in
  `Directory.Packages.props`, die eingebetteten Schemaskripte unter `sql/schema/`). Nur der in
  Abschnitt 7 genannte Ausgabepfad war veraltet (`net8.0` statt `net10.0`).
- **Die zwei Wurzeldateien gehörten nicht zum Migrator.** `migration.config.json` (1,1 KB) und
  `migration.manuell.sql` (65 KB) sind die Skripte der Access-nach-Access-Übernahme vom Juli 2026;
  ihr Leser ist die GUI des Werkzeugs `AccessMigration` im Ordner `C:\Waermeplan\DB_Migration`
  neben dem Repo (eigenes Git, letzter Commit 26.07.2026, .NET 8, ACE OLE DB). Der
  `EposSqliteMigrator` hat sie nie gelesen, im Repo gab es nie einen Leser; letzte Änderung
  20.08.2026 (K6b). Mit der SQLite-Umstellung hat dieser Weg kein Ziel mehr. ADR-001 sagte das so.
- **BETRIEB_SQLITE.md Abschnitt 6** zitierte die Dateien entgegen der Auftragsannahme nicht.
- **Weitere Verweise:** fünf Papiere unter `aktuell/` (Entscheidungsregister, Aufräumkonzept mit
  „bleibt“, Setup-Konzept, Wechselrichter-Konzept, Umsetzungskonzept iOS), im Code drei
  Meldungstexte des Migrationsberichts (`SchemaMigration.cs`), die Probe in
  `Proben/ZugriffsschichtProben/Program.cs`, die den Werkzeugnamen im Bericht verlangte, und rund
  zwanzig Kommentare in vierzehn Dateien; sachlich falsch war der Kommentar in
  `WindowsFormsApplication1.csproj` („bleibt in WP-Plan.sln und wird weitergebaut“).

## 2 Entscheid

Anwender, 24.09.2026: **Weg 2 — endgültig einstellen**, Codeseite mit anpassen. Gründe: Access
war beim Kunden nie produktiv (`#157‑E‑1`, 09.09.2026) und ist seit dem 06.09.2026 als nicht
mehr relevant festgehalten; es gibt keinen Kundenbestand, die Hausbestände sind umgestellt; elf
Tage ohne das Werkzeug haben nichts gebrochen; die Rückholung bleibt aus `b0647c7e` möglich.

## 3 Umsetzung

**Papiere (`Dokumentation/aktuell/`):**

- `BETRIEB_SQLITE.md`: Stand 24.09.2026; Einleitung; Abschnitt 1.1 „Übernahme eines
  Access-Altbestands — eingestellt“; Abschnitt 7 „Kundenbestände: keine Übernahme aus Access
  mehr“ mit dem, was gilt, und dem Hinweis auf den Git-Stand `b0647c7e`, das
  Implementierungskonzept (Abschnitt 4) und das S7-Protokoll für den Fall eines dennoch
  auftauchenden Bestands.
- `ADR-001_Schema-Ausrollung.md`: Absatz zu den Wurzeldateien auf Geschichte (Leser
  `AccessMigration`, entfernt 24.09.2026, letzter Stand `6d022f6d`).
- `KONTEXT_Brauchwassertypen_VDI6002.md`: Kasten im Kopf, Abschnitt 6 als Geschichte markiert
  (Punkte 1 und 3 gegenstandslos, Punkt 2 gilt unabhängig vom Skript), Tabelle in Abschnitt 8.
- `Konzept_Stromspeicher_EPOS-Plan.md`, `Entscheidungsregister_iOS_EPOS-Plan.md`,
  `Konzept_Repository_Aufraeumen_EPOS-Plan.md` (Inventarzeile und „Access im Code“),
  `Konzept_Setup_InnoSetup_EPOS-Plan.md`, `Konzept_Wechselrichter_EPOS-Plan.md`,
  `Umsetzungskonzept_iOS_EPOS-Plan.md` (Zeilen 132, 280, 873, 1160): je ein Satz auf Geschichte.
- `Werkzeuge/SqlDialektPruefer/LIESMICH.md` und `pruefer.py`: Hinweis auf das entfernte
  Werkzeug, `Referenzlauf/` als verbliebener Access-Sprecher außerhalb der Wurzeln.
- `WindowsFormsApplication1/CLAUDE.md`: zwei Sätze zum Übernahmewerkzeug (x64-Begründung,
  `Microsoft.Data.Sqlite`) auf den gültigen Stand.

**Wurzeldateien:** `migration.config.json` und `migration.manuell.sql` per `git rm` entfernt.

**Code:**

- `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`: die beiden Abbruchtexte des
  Migrationsberichts (kein Schemamarker; Stand unter 61) nennen jetzt „die Übernahme aus Access
  ist eingestellt“ und verweisen auf BETRIEB_SQLITE.md 1.1 und 7; Klassen-, Freeze-Stand-,
  Einstiegs- und `SCHRITTE_SQLITE`-Kommentare sowie der Kommentar zu `energy_conversion`
  nachgezogen.
- `Proben/ZugriffsschichtProben/Program.cs`: Fall „Stand 60“ prüft den neuen Text
  („aus Access ist eingestellt“) statt des Werkzeugnamens; Kommentar nachgezogen.
- Kommentare: `WindowsFormsApplication1.csproj` (zwei Stellen), `Directory.Packages.props`,
  `Setup/EPOS-Plan.iss`, `Setup/build-setup.ps1`, `EPOS.Kern/Allgemein/DataRepository.cs`,
  `DbParam.cs`, `Datenbank/Erstbereitstellung.cs`, `Update/SchemaKatalog.cs`,
  `Controller/ApplikationCtrl.cs`, `WindowsFormsApplication1/Program.cs` (zwei Stellen).
- **Bewusst unverändert:** die wörtlichen Zitate der Schritt-65-Begründung in
  `EPOS.Kern/Allgemein/Update/AnlageStrangSchema.cs` und `WechselrichterSchema.cs`
  („eingebettete Ressource des EposSqliteMigrator“), die Vergleichskommentare zum eigenständigen
  `AccessMigration` in `ProjektExportImportCtrl.cs`, die historischen Tabellen des
  Umsetzungskonzepts (Zeilen 112, 609, 774, 787, 1284) und alles unter `Dokumentation/ueberholt/`.

## 4 Nachweis

- **Gate auf dem Änderungsstand `19002091`** (Worktree `mig2`, Log `GATE482.log` unter
  `C:\Waermeplan\.claude\gate\`): Kern-Filter 0 Fehler; Windows-Schale
  (`WindowsFormsApplication1.csproj`, Debug x64) 0 Fehler; ChartProben 151/151 gleich der
  Windows-Messlatte; voller Testlauf **12 970 bestanden, 0 Fehler, 1 übersprungen**
  (EPOS.Kern.Tests 6 012, EPOS.UI.Tests 5 996, KiKern.Tests 549, SpeicherEngine.Tests 386,
  SpeicherPlanung.Tests 27/1); Dokumentationswachen 29/29.
- **Gate auf dem Merge-Stand `fe434d0b`** (nach `origin` = `09cf662d`, Log
  `GATE482_merge.log`): Kern-Filter 0 Fehler; Windows-Schale 0 Fehler; ChartProben 151/151
  gleich; voller Lauf **12 976 bestanden, 0 Fehler, 1 übersprungen** (Kern 6 018, UI 5 996,
  KiKern 549, SpeicherEngine 386, SpeicherPlanung 27/1); Dokumentationswachen 29/29.
- **ZugriffsschichtProben** (Bau `ZugriffsschichtProben.sln` Release x64 0 Fehler; Lauf gegen
  eine Kopie der Testdatenbank, Ausgabe `GATE482/zugriffsschichtproben.txt`): Erster Lauf
  7/14 — alle sieben roten Fälle mit „Lesemodus: Die Lizenz erlaubt derzeit keine Änderungen“,
  weil der Probe seit iF30 (06.09.2026) die Werkzeug-Freigabe der Schreibnaht fehlte, die Tests
  und beide Referenzläufe als eine benannte Zeile setzen; Commit `8e1134df` holt sie nach.
  Zweiter Lauf 10/14: **Fall 13 (Schemapflege — Stand 0, Stand 60 mit dem neuen Text „aus
  Access ist eingestellt“, Freeze-Stand) und Fall 14 grün**; rot bleiben die Fälle 3, 5, 6
  (`Tab_Projekt` 25 statt 26 Zeilen) und 9 (Schema-Auskunft: der Fremdschlüssel von
  `Tab_Gebaeude` zeigt seit dem FK-Umbau auf `Tab_Projekt` statt `Z_ProjektGebaeude`) —
  Schemadrift der Probe gegenüber der Testdatenbank seit dem 09.09.2026, nicht Teil dieses
  Auftrags (Nebenbefund).
- Kein Referenzlauf (kein Rechenweg berührt), kein Schemaschritt, Testdatenbank unverändert.

## 5 Git

- Zweig `mig2` (Worktree `.claude/worktrees/mig2`) von `origin/ios_migration_september`
  = `6d022f6d`.
- `19002091` — Access-Uebernahme endgueltig eingestellt: Doku, Wurzelskripte, Meldungen
  (24 Dateien geändert, 2 entfernt).
- `fe434d0b` — Merge `origin/ios_migration_september` (`09cf662d`, Katalogimport-Zeilenenden
  #481) nach `mig2`, konfliktfrei.
- `8e1134df` — ZugriffsschichtProben: Werkzeug-Freigabe der Schreibnaht im Einstieg.
- Papiere (Statuszeile #482, dieses Protokoll) im Folgecommit; Push aus dem Worktree auf
  `ios_migration_september` und `main` (Fast-Forward).

## 6 Nebenbefunde

- Die ZugriffsschichtProben hinken der Testdatenbank hinterher: Die Fälle 3, 5 und 6
  erwarten 26 Projekte (heute 25), Fall 9 den Fremdschlüssel von `Tab_Gebaeude` auf
  `Z_ProjektGebaeude` (seit dem FK-Umbau `Tab_Projekt`); während Fall 13 melden Prüfabfragen
  eines Schritts fehlende `KWKG_*`-Spalten auf `Tab_ProjektWerte`, weil das Nachspielen ab
  Stand 61 auf einer strukturell fertigen Datei läuft. Eigener Auftrag, nicht hier.

- Der Hauptbaum `C:\Waermeplan\EPOS-Plan` stand während des Auftrags auf `main` bei `5ae3019f`,
  zwölf Commits hinter `origin` (origin/main = origin/ios_migration_september = `6d022f6d`);
  laut Regel bleibt er auf `ios_migration_september`. Nicht gewechselt, nur gemeldet; die
  Arbeit lief im Worktree `mig2`.
- Access spricht im Repository weiterhin die Windows-Suite `Referenzlauf/` (Modus `migration`,
  eigene `System.Data.OleDb`-Referenz, Fallback `C:\Waermeplan\Kenndaten.accdb`); deshalb
  bleibt `System.Data.OleDb` in `Directory.Packages.props`. Eigenes Thema, nicht Teil des
  Entscheids.
- Das Werkzeug `AccessMigration` in `C:\Waermeplan\DB_Migration` liegt außerhalb des
  Repositoriums und wurde nicht angefasst.
- Die Statusnummer #481 war während der Arbeit von der Katalogimport-Sitzung belegt worden
  (`09cf662d`); dieser Block trägt deshalb #482.
