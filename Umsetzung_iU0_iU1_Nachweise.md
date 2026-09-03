# Nachweisliste iU0 / iU1 / iU4 / iU6 — Abnahme auf Windows

**Stand 03.09.2026 · Branch `ios_migration` · `c3a8233`..`ce2dc9e` (+ P1.11), iU4: `4a0a4e2`..`616dff4`, iU6: `9cf6f86`..`27bc634`**

Die Pakete iU0 und iU1 des [`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md)
(§ 4, Rev. 2.1) sind umgesetzt. **Alle Nachweise wurden auf Linux geführt** — SDK 10.0.400, kein
Visual Studio, keine Datenbank. Was dort nicht prüfbar ist, steht hier als abhakbare Liste: das
Starten der Anwendung, das Rechnen gegen echte Projektdaten, der Designer, das Setup.

Ergebnis der Linux-Nachweise in einem Satz: **`dotnet build WP-Plan.sln -c Release -p:Platform=x64`
übersetzt alle 7 Projekte mit 0 Fehlern, `dotnet test WP-Plan.Kern.slnf` meldet 787/787, und der
CI-Lauf `kern.yml` ist auf ubuntu *und* macos grün.**

Dazu der Nachweis auf einem echten Windows-Runner: **Der Workflow `windows.yml` (`dotnet build
WP-Plan.sln`, Migrator-sln, Proben-sln, 787 Tests) ist auf `0ddc417` und `dab063a` grün** — die
Solution baut auf Windows mit .NET 10 ohne Visual Studio. Offen bleibt nur, was ausgeführt werden
muss.

---

## Nachweise je Commit

### `c3a8233` — iU0-P0.1: CSExeCOMServer und `csproj.netfx-backup` entfernt

**Nachweis hier:** `git grep CSExeCOMServer` ohne `.md` findet nur noch den Historienkommentar in
`BhkwPlan.cs:9`; die Solution ist unverändert, weil das Projekt nie darin stand. Beides bleibt über
`git show 922228a:<pfad>` erreichbar.

**Nachweis Windows:**

- [ ] keiner nötig

---

### `1ab062d` — iU0-P0.2: Entscheidungsregister iF1–iF18, Referenzbasis, Chart-/Grid-Auszählung

**Nachweis hier:** `Entscheidungsregister_iOS_EPOS-Plan.md` liegt vor; die Zahlen sind mit den dort
genannten Befehlen reproduzierbar — **18 Chart-Masken (32 Steuerelemente)** und **19 Grid-Masken
(22 Steuerelemente)** im Build, Referenzbasis `Referenzlaeufe/2026-08-30_B3-Kaskade` festgeschrieben.

**Nachweis Windows:**

- [ ] Anwender trägt Entscheide und Termine ein

---

### `e0df744` — iU1-P1.2: `global.json` und `Directory.Build.props` angelegt

**Nachweis hier:** `dotnet --version` in der Wurzel meldet 10.0.400; `SpeicherEngine` baut; das
Hauptprojekt scheitert ohne `-p:EnableWindowsTargeting` nur noch an den beiden COM-Referenzen, nicht
mehr am Windows-Ziel.

**Nachweis Windows:**

- [ ] `dotnet --list-sdks` zeigt ein 10.0.4xx-Band
- [ ] VS 2026 öffnet die Solution

---

### `577701c` — iU1-P1.5: `EposSqliteMigrator` Kern und Konsole auf `net10.0`, Sqlite/OleDb 10.0.11

**Nachweis hier:** `dotnet build EposSqliteMigrator/EposSqliteMigrator.sln -c Release
-p:Platform=x64` → Build succeeded, 0 Fehler, 0 Warnungen.

**Nachweis Windows:**

- [ ] `EposSqliteMigrator.exe --hilfe`
- [ ] Probelauf gegen eine **KOPIE** einer `.accdb` nach `BETRIEB_SQLITE.md` (Bericht,
      `integrity_check`/`foreign_key_check` grün)
- [ ] Proben Fall 15/16

---

### `469aa3c` — iU1-P1.7: `Referenzlauf` in `WP-Plan.sln`, neuer Filter `WP-Plan.Kern.slnf`

**Nachweis hier:** `dotnet build WP-Plan.Kern.slnf -c Release` baut genau 4 Projekte, 0 Warnungen,
0 Fehler; `dotnet test WP-Plan.Kern.slnf -c Release --no-build` → 450 + 337 = **787 bestanden**;
`dotnet sln WP-Plan.sln list` zeigt **7 Projekte**, darunter `Referenzlauf/Referenzlauf.csproj`.

**Nachweis Windows:**

- [ ] `dotnet build WP-Plan.sln -c Release -p:Platform=x64` (nach P1.1/P1.5)
- [ ] `WP-Plan.sln` in VS 2026 öffnen — erscheint `Referenzlauf` im Solution Explorer?
- [ ] `WP-Plan.Kern.slnf` in VS 2026 öffnen — lädt der Filter genau die vier Kernprojekte?

> Offen geblieben: Der Kopfkommentar in `Referenzlauf.csproj` sagt weiterhin „BEWUSST NICHT IN
> WP-Plan.sln aufgenommen" und ist damit überholt — die Korrektur gehört zum Paket des Werkzeugs.

---

### `b4fd34d` — iU1-P1.9: GitHub Actions `kern.yml` (ubuntu + macos) und `windows.yml`

**Nachweis hier:** beide Workflows sind gültiges YAML; die `run`-Zeilen des ubuntu-Pfads 1:1
nachgefahren (nach Löschen aller `bin`/`obj`) → `dotnet build WP-Plan.Kern.slnf -c Release`
0 Warnungen / 0 Fehler, `dotnet test` **787 bestanden**.

**Nachweis Windows:**

- [ ] der komplette `windows.yml`-Pfad — die drei Build-Schritte (WP-Plan, EposSqliteMigrator,
      ZugriffsschichtProben) sind hier nicht prüfbar; der erste echte Nachweis ist der erste Lauf
      auf GitHub
- [ ] ob `macos-latest` die `net10.0`-Kernprojekte ohne Zusatzschritt baut (hier stand nur Linux
      zur Verfügung)

---

### `d4b72c8` — iU1-P1.1: COM-Referenzen entfernt, `GanglinienDatei` auf ClosedXML

**Nachweis hier:** Der Bau des Hauptprojekts brach vorher in `ResolveComReference` ab, ohne dass der
C#-Compiler je anlief; danach ist dieser Abbruch weg. In einer Wegwerf-Kopie mit normalisierter
Kodierung (dem Vorgriff auf P1.12) bauen Hauptprojekt und `Referenzlauf` mit **0 Fehlern**.
`git grep -nE "Interop\.Excel|VBIDE|COMReference|ReleaseComObject"` → **0 Treffer**.

**Verhaltensänderung, die auf Windows zu prüfen ist:** `.xls`/`.xlsb` sind nicht mehr lesbar
(ClosedXML liest nur OOXML) und führen gezielt auf `IMPORT_PROT_EXCEL_FEHLT`; Formelzellen liefern
den in der Mappe gespeicherten Wert statt einer Neuberechnung durch Excel; ein installiertes Office
ist nicht mehr nötig.

**Nachweis Windows:**

- [ ] Ganglinien-Import in `Form_PeakShaving` mit **`.xlsx`**
- [ ] dasselbe mit **`.xlsm`**
- [ ] dasselbe mit **`.xls`** — muss die neue Meldung bringen
- [ ] dasselbe mit einer **parallel in Excel geöffneten Mappe**
- [ ] dasselbe mit einem **falsch geschriebenen Blattnamen** — muss die Warnung
      `IMPORT_PROT_EXCEL_BLATT` bringen und auf dem ersten Blatt weiterlesen
- [ ] dieselbe Ganglinie einmal als `.xlsx` und einmal als CSV importieren und die Reihen
      vergleichen — Werte und Zeitstempel müssen deckungsgleich sein
- [ ] Referenzlauf 332/332

---

### `8dbea83` — iU1-P1.3: `UseWPF` entfernt

**Nachweis hier:** WPF war nachweislich ungenutzt (0 XAML-Dateien, 0 Dateien mit
`System.Windows.Media/Controls/…`). Der Bau in der normalisierten Wegwerf-Kopie liefert **dieselben**
Werte wie vor dem Commit: Hauptprojekt 0 Fehler / 4 Warnungen (Altbestand: 2 × `CS0108`,
2 × `CS0109`), `Referenzlauf` 0 Fehler / 0 Warnungen. Das Entfernen kostet keine Übersetzung.

**Nachweis Windows:**

- [ ] App-Start
- [ ] ein Formular mit Kontextmenü öffnen und das Menü benutzen
- [ ] Referenzlauf 332/332

---

### `a81fc1b` — iU1-P1.4: `SpeicherEngine`, `KiKern` und beide Testprojekte auf `net10.0`

**Nachweis hier:** ohne `DOTNET_ROLL_FORWARD` — `dotnet test SpeicherEngine.Tests` → **337/337**,
`dotnet test KiKern.Tests` → **450/450**, beides in Release **und** Debug grün. Testpakete auf
`Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5 (bewusst nicht
auf Test.Sdk 18.x / runner 4.x — die verlangen die Microsoft.Testing.Platform-Kette).

**Nachweis Windows:**

- [ ] Bau und Testlauf beider Projekte unter Windows mit VS 2026 / SDK 10.0.400
- [ ] `KiKern.Tests` wie im Kopfkommentar mit `-p:ArtifactsPath=C:\Temp\kibart`
- [ ] wird `xunit.runner.visualstudio` 3.1.5 im Test-Explorer von VS 2026 erkannt?

---

### `3ba7d54` — iU1-P1.12: 68 cp1252-Quelldateien auf UTF-8 mit BOM normalisiert (M10)

**Nachweis hier:** `file --mime-encoding` über alle `.cs` → **552 utf-8, 20 us-ascii, 0 andere**;
der Diff zeigt ausschließlich Zeilen mit Nicht-ASCII-Zeichen sowie die BOM auf Zeile 1. Vor der
Umwandlung war geprüft, dass keine Datei Mischkodierung trägt; nach der Umwandlung, dass der
Rückweg textgleich ist. Zeilenenden unverändert. **Reine Kodierungsänderung, kein Inhalt.**

**Nachweis Windows:**

- [ ] Referenzlauf 332/332 byte-gleich (eine Kodierung darf kein Ergebnis bewegen)
- [ ] VS 2026 öffnet die Dateien ohne Umlautschaden

---

### `0ddc417` — iU1-P1.6: Hauptprojekt, `Referenzlauf` und Proben auf `net10.0-windows`, Pakete 10.0.11

**Nachweis hier:** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → **Build succeeded,
0 Fehler, 7 Projekte**; Hauptprojekt 70 Warnungen (60 × `WFO1000`, 4 × `CS0108`, 4 × `CS0109`
Altbestand, 2 × `WFO0003`), `Referenzlauf` 0 Fehler, `ZugriffsschichtProben.sln` 0 Fehler.

**Nachweis Windows:**

- [ ] VS 2026 baut Debug|x64
- [ ] App: Projekt laden
- [ ] App: Simulation
- [ ] App: Bericht **Word**
- [ ] App: Bericht **Excel**
- [ ] App: `Form_SpeicherOptimierung`
- [ ] App: ein `DataVisualization`-Chart
- [ ] App: Lizenzdialog (`ProtectedData` 10.x)
- [ ] Referenzlauf **332/332 byte-gleich** (iT1)
- [ ] Proben **16/16**
- [ ] optional `EPOS_REFLAUF_UICULTURE=en-US` byte-identisch (iT7)

---

### `dab063a` — iU1-P1.8: `Directory.Packages.props` — Paketversionen zentral (CPM)

**Nachweis hier:** `dotnet list <projekt> package` über alle vier Projekte mit Paketreferenzen vor
und nach dem Umbau, sortiert und dedupliziert → **Diff leer** (28 Zeilen identisch, reiner Umzug);
`dotnet build WP-Plan.sln -c Release -p:Platform=x64` → Build succeeded; `EposSqliteMigrator.sln`
0 Fehler; kein `NU1008`/`NU1010`. CI `windows.yml` auf `dab063a` grün.

**Nachweis Windows:**

- [ ] VS 2026 baut Debug|x64
- [ ] NuGet-Paket-Manager zeigt die Pakete als „zentral verwaltet"
- [ ] falls ein `dev\`-Harness mit eigenen `Version=` existiert: dort `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` setzen

---

### `ce2dc9e` — iU1-P1.10: Setup auf `dotnet publish` und `EPOS_Plan.exe` umgestellt

**Nachweis hier:** `dotnet publish WindowsFormsApplication1.csproj -c Release -r win-x64
--self-contained true -p:Platform=x64 …` läuft mit SDK 10.0.400 durch und erzeugt **`EPOS_Plan.exe`
(163.328 Bytes)**, 283 Dateien, eigenständig, keine `.pdb` — belegt zugleich die COM-Freiheit.
`grep MSBuild|vswhere|MSB4803|WindowsFormsApplication1.exe` in `Setup/` → nur noch erklärende
Kommentare. PowerShell nur gelesen, nicht ausgeführt (kein `pwsh` auf Linux).

**Nachweis Windows:**

- [ ] `.\Setup\build-setup.ps1 -Schnell` läuft durch → `Setup\Ausgabe\EPOS-Plan_Setup_1.1.0.0.exe`
- [ ] Installation auf einer VM, App-Start aus dem Startmenü (`EPOS_Plan.exe`)
- [ ] Update über eine bestehende Installation: genau **ein** Eintrag in „Apps und Features"

---

## Nachweise iU4 — `EPOS.Kern` herauslösen

**Stand 03.09.2026 · Branch `ios_migration` · `4a0a4e2`..`616dff4` auf Basis `9fe9c71`.**

Auch hier gilt: alle Nachweise auf Linux geführt (SDK 10.0.400, kein Visual Studio). Nach **jedem**
der sieben Commits wurden Build (`WP-Plan.sln`, Release, x64, `--no-incremental`), Tests
(`WP-Plan.Kern.slnf`) und der Referenzlauf 1030 gegen `Referenzlaeufe/2026-08-30_B3-Kaskade`
gefahren: durchgehend **0 Fehler**, **PASS** und **byte-gleich**. Die Warnsumme der Lösung ist am
Ende dieselbe wie am Anfang: **123** (Verteilung verschoben, siehe iU4-5).

### `4a0a4e2` — iU4-1: `Sprache` und `ZahlText`

**Nachweis hier:** 123 Warnungen, 787 Tests, 1030 byte-gleich.

**Nachweis Windows:**

- [ ] Start mit Registry `Language=0` und `Language=1` — Oberfläche und Bericht in der jeweiligen
      Sprache wie bisher
- [ ] Eine Zahleneingabe (Emissionen, Kostenprofil): Komma **und** Punkt gültig, `1.234,5`
      abgelehnt, Färbung und Prüfmeldung unverändert

### `fb5374e` — iU4-2: Kanten kappen

**Nachweis hier:** 123 Warnungen, 787 Tests, 1030 byte-gleich.

**Nachweis Windows:**

- [ ] Projekt löschen **und** Variante löschen → die verwaisten Gerätezeilen verschwinden weiterhin
      (der Haken `WErzeugerCtrl.GeraetewaisenAufraeumen` ist in `Program.Main` belegt)
- [ ] Menü „Export/Import" öffnet und schließt unverändert
- [ ] Stromspeicher-Stammdaten, schreibgeschützter Satz: Meldung erscheint — **jetzt ohne
      Info-Symbol** (`Meldung.Hinweis` = `MessageBox.Show(text, titel)`), Text und Titel
      unverändert. Einzige sichtbare Abweichung der Etappe, so im Konzept vorgegeben
- [ ] Projekt duplizieren mit leerem/unbekanntem Namen → die fünf Meldungen erscheinen weiter
- [ ] Preisreihen- und Kostenprofildialog auf einer Datenbank ohne diese Tabellen → stille
      Selbstanlage greift weiter

### `0ae589f` — iU4-3: `WizardSeite` und `ControllerListen`

**Nachweis hier:** 123 Warnungen, 787 Tests, 1030 byte-gleich.

**Nachweis Windows:**

- [ ] Projektassistent vollständig durchlaufen, **beide** Einstiege (Neu und Bearbeiten):
      Seitenfolge, Vor/Zurück über inaktive Seiten hinweg, Komponentenseite mit dem Ein- und
      Ausschalten einzelner Gewerke
- [ ] BHKW-Stammdatendialog (`Form_DBBHKW`) — Namensliste gefüllt
- [ ] Projekt löschen (`Form_ProjektDelete`) — Projektliste gefüllt

### `09cd975` — iU4-4: Linkliste auf 169 Dateien, `InternalsVisibleTo`

**Nachweis hier:** `EPOS.Kern` allein: 0 Fehler, 89 Warnungen (87 CA1416, 1 CS0108, 1 CA2255 neu).
Lösung 124 Warnungen (die eine CA2255 mehr). 787 Tests, 1030 byte-gleich.

**Nachweis Windows:**

- [ ] Reiner Projektdatei-Schritt — die Lösung muss in VS 2026 unverändert bauen

### `b1a73af` — iU4-5: Big-Bang, 168 Dateien verschoben

**Nachweis hier:** `git diff -M`: 171 R100-Umbenennungen + 2 `.csproj`, sonst nichts.
`WindowsFormsApplication1` 585 → 417 `.cs`. Lösung 123 Warnungen (Kern 89, App 34).
`EPOS.Kern/bin/Release/net10.0/en-US/EPOS.Kern.resources.dll` vorhanden und in der
Anwendungsausgabe. Proben-sln 0 Fehler. 787 Tests, 1030 byte-gleich.

**Nachweis Windows:**

- [ ] VS 2026 öffnet `WP-Plan.sln` und zeigt jetzt **9 Projekte**; Projektmappen-Explorer zeigt
      `EPOS.Kern` mit den verschobenen Ordnern; `Debug|x64` baut durch
- [ ] Anwendung starten, ein Projekt laden, durch die Bereiche gehen — die Anzeigetexte kommen
      jetzt aus `EPOS.Kern.dll`
- [ ] **Sprachumschaltung auf Englisch** — `en-US\EPOS.Kern.resources.dll` muss neben der EXE
      liegen und greifen
- [ ] Einstellungen (`Properties.Settings`, u. a. `WordPressUrl`) lesen **und** schreiben
- [ ] **Referenzlauf 332/332 byte-gleich** — der eigentliche iZ4-Nachweis
- [ ] Setup bauen (`.\Setup\build-setup.ps1 -Schnell`) und prüfen, dass `EPOS.Kern.dll` **und**
      der `en-US`-Ordner mit ausgeliefert werden; einmal installieren und starten
- [ ] WinForms-Designer öffnet ein Formular, das Kerntypen benutzt (z. B. `Form_Quellprofil`)

### `b6efc76` — iU4-6: `EPOS.Kern.Tests`

**Nachweis hier:** `dotnet test WP-Plan.Kern.slnf` → **796** (787 + 9), 0 Fehler.

**Nachweis Windows:**

- [ ] Test-Explorer in VS 2026 und `dotnet test` auf der Windows-Buildmaschine: dieselben 796 grün

### `616dff4` — iU4-7: CI-Referenzlauf auf drei Projekte

**Nachweis hier:** derselbe Lauf lokal — 1030 PASS (236.670 Werte), 1007 PASS (324.219),
1017 PASS (254.154), **GESAMT PASS (815.043 Werte)**, alle drei byte-gleich.

**Nachweis Windows:**

- [ ] nichts — reine Workflow-Änderung für ubuntu/macos; `windows.yml` bleibt unverändert

### Wenn der Referenzlauf auf Windows abweicht

Auch hier halbieren. Die Reihenfolge der Verdächtigen ist eine andere als bei iU1:

- **`b1a73af` (iU4-5)** ist der einzige Commit, der Dateien bewegt — aber er ändert **keinen
  Quelltext** (171 R100-Umbenennungen). Wenn er etwas bewegt, dann über die Ressourcen: Der
  Anzeigetext-Katalog wird jetzt aus `EPOS.Kern.dll` statt aus `EPOS_Plan.dll` geladen. Prüfpunkt
  ist der `LogicalName` in `EPOS.Kern.csproj`.
- **`fb5374e` (iU4-2)** ist der einzige Commit mit einer bewusst in Kauf genommenen sichtbaren
  Änderung (das fehlende Info-Symbol) und mit dem neuen Haken für den Aufräumlauf. Ein
  Ergebnisunterschied im Referenzlauf kann von hier nicht kommen — ein Unterschied im
  Datenbestand nach dem Löschen eines Projekts schon.
- **`0ae589f` (iU4-3)** betrifft ausschließlich den Assistenten und zwei Auswahllisten.
- `4a0a4e2`, `09cd975` und `616dff4` können ein Rechenergebnis nicht bewegen.

---

## Nachweise iU6 — Datenzugriff plattformfrei

**Stand 03.09.2026 · Branch `ios_migration` · `9cf6f86`..`27bc634` auf Basis `18f515f`.**

Alle Nachweise auf Linux geführt (SDK 10.0.400, kein Visual Studio). Nach **jeder** der sechs
Tranchen wurden gefahren: Build (`WP-Plan.sln`, Release, x64, `--no-incremental`), Kern-Build
allein mit CA1416-Zählung, Tests (`WP-Plan.Kern.slnf`), Referenzlauf **1030/1007/1017** gegen
`Referenzlaeufe/2026-08-30_B3-Kaskade` und die Übersetzung von
`Proben/ZugriffsschichtProben`. Durchgehend **0 Fehler**, **GESAMT PASS** und **byte-gleich**.

| Tranche | Warnungen Lösung | Warnungen `EPOS.Kern` | CA1416 | Tests |
|---|---|---|---|---|
| Basis `18f515f` | 123 | 89 | 87 | 796 |
| iU6-T1 `9cf6f86` | 114 | 80 | 78 | 796 |
| iU6-T2 `5836b8c` | 36 | 2 | **0** | 796 |
| iU6-T3a `99e5a68` | 36 | 2 | 0 | 796 |
| iU6-T3b `7fb4bfd` | 36 | 2 | 0 | 796 |
| iU6-T4 `64c06d7` | 36 | 2 | 0 | **805** |
| iU6-T5 `27bc634` | 36 | 2 | 0 | 805 |

Die zwei verbleibenden Kern-Warnungen stammen aus dem Bestand: CA2255
(`SimulationControl.Stromspeicher.cs:24`, `ModuleInitializer`) und CS0108
(`WErzeugerModel.cs:6`).

Ein Hinweis zum `diff -rq`: Das Laufwerkzeug legt im Zielordner ein `protokoll.txt` an, das der
eingefrorene Referenzstand nicht enthält. Das ist der einzige gemeldete Unterschied; alle
Ergebnisdateien sind byte-gleich.

### `9cf6f86` — iU6-T1: `RecordSet.DBCommand` ersatzlos gestrichen (iR8)

**Nachweis hier:** 114 Warnungen, CA1416 87 → 78, 796 Tests, 1030/1007/1017 byte-gleich.

**Nachweis Windows:**

- [ ] `FormMain` — die 13 `RecordSet`-Stellen: Anlagenlisten aller Register füllen sich wie bisher
- [ ] `Form_Start` (10 Stellen) — Projektliste, Öffnen, Löschen
- [ ] `Form_PV` (6), `Form_Gebäude` (6), `Form_WP` (4) — Listen und Auswahlfelder
- [ ] `Form_DBBHKW` — Speichern eines Katalogeintrags: `Form_DBBHKW.cs:436` und `:450` sind die
      **einzigen** Nutzer der `DbVorgang`-Überladungen `Open(sql, vorgang)` / `Insert(sql, vorgang)`;
      ein Abbruch mitten im Speichern muss weiterhin vollständig zurückrollen

### `5836b8c` — iU6-T2: toter OleDb-Code in drei Kern-Controllern

**Nachweis hier:** 36 Warnungen (Lösung) bzw. 2 (Kern), **CA1416 = 0**, 796 Tests,
1030/1007/1017 byte-gleich.

**Nachweis Windows:**

- [ ] Solarthermie-Katalog: `Form_SolarKollektoren` (Übernahme ins Projekt, Löschen aus dem
      Projekt, Löschen im Katalog) und `Form_SolarKollektorenAdmin` — Verhalten **unverändert**;
      geschrieben wird über `SolarkollektorenStammCtrl`
- [ ] Pufferspeicher-Katalog: `Form_PufferSp` und `Form_PufferSp_Admin` — dasselbe über
      `PufferSpStammCtrl`
- [ ] `FormMain` Register Solarthermie und Pufferspeicher (`FormMain.cs:1204/1237`) — Listen
      unverändert
- [ ] **Erststart-Migration eines vorhandenen `.accdb`-Bestands** bis Zielstand 61: Der
      Schemamarker wird jetzt über `SchemaVersionAccess` (neue App-Datei) gelesen und je Schritt
      fortgeschrieben. Der Lauf muss dieselbe Schrittfolge und dasselbe Protokoll liefern wie vorher

### `99e5a68` — iU6-T3a: Masken-Sweep `OleDbParameter` → `DbParam` (46 Views)

**Nachweis hier:** 36 Warnungen, CA1416 = 0, 796 Tests, byte-gleich. BOM- und
Zeilenenden-Zustand der 281 View-Dateien vor und nach dem Lauf identisch (237 mit BOM, 44 ohne,
0 mit CRLF).

**Nachweis Windows** — der Sweep berührt reine Bedienpfade, die der Referenzlauf nicht fährt.
Nach Dichte der Änderungen:

- [ ] `Form_Kosten.cs` (83 Stellen) — Kostenpositionen anlegen, ändern, löschen; Energieträger
- [ ] `ucFuelSettings.cs` (80) — Brennstoffeinstellungen und **Preishistorie** (dort stehen die
      Stellen mit ausdrücklichem `DbParamTyp.Date`)
- [ ] `Form_BHKWEing.cs` (50) — Katalogeintrag anlegen und speichern
- [ ] `Form_Heizkessel.cs` (46) und `Form_Heizkessel_einlesen.cs` (20) — Katalog und Import
- [ ] `Form_EingGebTyp` — Gebäudetyp anlegen: die dichteste Stelle mit typisierten Parametern
      (`Boolean`, `Double`, `Integer`, `VarWChar`) und NULL-fähigen Spalten
- [ ] Stichprobe über die übrigen Masken: Speichern, Anlegen, Löschen, Katalogimport

### `7fb4bfd` — iU6-T3b: Brücke aus dem Kern, OleDb-Paket raus

**Nachweis hier:** `dotnet list EPOS.Kern package | grep -c OleDb` → **0**; im Kern kein `using`,
kein Typ, keine `PackageReference` mehr. 36 Warnungen, CA1416 = 0, 796 Tests, byte-gleich.

**Nachweis Windows:**

- [ ] **Erststart-Migration aus `.accdb`** — der einzige verbliebene Nutzer der Brücke:
      `SchemaMigration.NonQuery/Skalar/Abfrage` und `GeraeteWaisen.Ids` binden ihre `DbParam`
      jetzt über `DbParamOleDb.Nach`. Besonders die Schritte mit ausdrücklichem Typ und
      Feldlänge (dreiargumentiger `OleDbParameter`-Konstruktor)
- [ ] Aufräumlauf „Gerätewaisen" nach dem Löschen eines Projekts — beide Wege: der reguläre über
      die Zugriffsschicht **und** der aus der Migration hereingereichte über eine offene
      `OleDbConnection`

### `64c06d7` — iU6-T4: `IDatenzugriff`, `SqliteDatenzugriff`, Fassade

**Nachweis hier:** `dotnet test WP-Plan.Kern.slnf` → **805** (796 + 9 neue in
`EPOS.Kern.Tests/DatenzugriffTests.cs`). `git diff --stat` der Tranche zeigt genau vier Dateien:
`DataRepository.cs` und die drei neuen — **keine** der rund 160 Aufruferdateien wurde angefasst.
Referenzlauf byte-gleich; damit ist zugleich belegt, dass `PfadUeberschreibung` weiterhin alles
schlägt (der Lauf arbeitet ausschließlich über diesen Haken).

**Nachweis Windows:**

- [ ] Ein voller Bediendurchlauf: Projekt öffnen, Anlagen anlegen/ändern/löschen, Simulation,
      Bericht. Jeder Datenbankzugriff läuft jetzt durch eine Weiterleitung — fällt etwas aus,
      fällt es sofort auf
- [ ] Ein **provozierter Datenbankfehler in der Bedienung** muss die MessageBox wie bisher zeigen,
      derselbe Fehler **im Simulationslauf** sie wie bisher unterdrücken und in der Sammelliste
      landen (Engine-Modus)
- [ ] Der Diagnosezusatz „Abfrage: …" hängt weiterhin an den Meldungen von `GetDataTable` und
      `ExecuteScalar`
- [ ] `Form_AdminSettings`: ein geänderter Datenbankordner greift weiterhin (`GetDBPath`
      unverändert übernommen)
- [ ] Test-Explorer in VS 2026: dieselben 805 grün

### `27bc634` — iU6-T5: `bundle_green` vorbereitet

**Nachweis hier:** `dotnet list EPOS.Kern package` zeigt unverändert nur
`Microsoft.Data.Sqlite 10.0.11` und `System.Configuration.ConfigurationManager 10.0.11` — die
Bedingung `-ios`/`-maccatalyst` ist auf `net10.0` falsch. Build und Tests unverändert.

**Nachweis Windows:**

- [ ] nur Restore und Build der Anwendung; fachlich ändert sich nichts. Der eigentliche Nachweis
      (bundle_green lädt, `Batteries_V2.Init()` greift) gehört zu iU10

### Wenn der Referenzlauf auf Windows abweicht

Auch hier halbieren. Die Reihenfolge der Verdächtigen:

- **`64c06d7` (iU6-T4)** ist der einzige Commit, der den Rechenpfad anfasst — jeder
  Datenbankzugriff geht seitdem durch eine Weiterleitung. Sollte per Konstruktion nichts bewegen
  (die Rümpfe sind wörtlich verschoben); wenn doch, dann in `NormalisiereWert`,
  `UebersetzeParameterzeichen` oder im Typ-Rückweg `LadeTabelle`.
- **`5836b8c` (iU6-T2)** kann ein Rechenergebnis nicht bewegen, wohl aber den **Datenbestand**
  nach einer Erststart-Migration (`SchemaVersionAccess`).
- **`99e5a68` (iU6-T3a)** betrifft ausschließlich Bedienpfade; ein Referenzlauf kann davon nicht
  abweichen, eine gespeicherte Eingabe schon.
- `9cf6f86`, `7fb4bfd` und `27bc634` können ein Rechenergebnis nicht bewegen — gestrichener toter
  Code, verschobene Brücke, eine unwirksame Paketzeile.

---

## Reihenfolge der Abnahme

Von billig nach teuer — jeder Schritt setzt den vorigen voraus.

1. **VS 2026 öffnet `WP-Plan.sln`** und zeigt seit iU4 **9 Projekte** (bei iU1 waren es 7);
   `Debug|x64` baut durch. Danach `WP-Plan.Kern.slnf` öffnen: seit iU4-6 genau **sechs**
   Projekte (bei iU1 vier).
2. **Anwendung starten, ein Projekt laden.** Bricht es hier ab, ist alles Weitere sinnlos.
3. **Referenzlauf gegen `Referenzlaeufe\2026-08-30_B3-Kaskade` → 332/332 byte-gleich (iT1).**
   *Das ist der eigentliche iZ1-Nachweis.* Frameworksprung, Kodierungswechsel und `UseWPF`-Rückbau
   dürfen kein einziges Ergebnis bewegen — sitzt dieser Schritt, sind die Pakete P1.3, P1.4, P1.6
   und P1.12 als Ganzes abgenommen.
4. **Proben 16/16** (`Proben/ZugriffsschichtProben`) — belegt, dass sich `Microsoft.Data.Sqlite`
   10.0.11 wie die 8er-Fassung verhält.
5. **Excel-Import-Tests** aus der Liste zu `d4b72c8` (P1.1) — die einzige echte
   Verhaltensänderung der ganzen Serie.
6. **Setup** (P1.10) — `.\Setup\build-setup.ps1 -Schnell`, einmal installieren.
7. **`EPOS_REFLAUF_UICULTURE=en-US`** setzen und den Referenzlauf wiederholen (iT7); das Ergebnis
   muss byte-identisch bleiben.

### Wenn der Referenzlauf abweicht

Nicht suchen, sondern halbieren: **jeder Commit dieser Serie ist einzeln zurücknehmbar**
(`git revert <sha>`), keiner baut inhaltlich auf einem anderen auf.

Zuerst die beiden Verdächtigen prüfen:

- **`3ba7d54` (P1.12, Kodierung)** — betrifft 68 Quelldateien und damit jedes Modul. Sollte per
  Konstruktion nichts bewegen; wenn doch, steckt in einer Datei ein Zeichen, das vorher anders
  gelesen wurde.
- **`0ddc417` (P1.6, Framework und Pakete)** — Frameworksprung plus Anhebung von
  `Microsoft.Data.Sqlite`, `System.Data.OleDb` und `ProtectedData`. Der wahrscheinlichste Ort für
  eine Verhaltensänderung in Zahlen, Formatierung oder Sortierung.

Danach `8dbea83` (`UseWPF`) und `a81fc1b` (Kernbibliotheken). `d4b72c8` (P1.1) betrifft nur den
Excel-Import und kann einen Referenzlauf nur bewegen, wenn dieser Ganglinien aus einer Mappe zieht.

Beim Zurücknehmen die Reihenfolge nicht verdrehen: `0ddc417` vor `3ba7d54` zurücknehmen, sonst
bricht der Bau an der Kombination aus `net10.0`-Bibliotheken und Hüllen auf dem alten Framework
(`NU1201`).
