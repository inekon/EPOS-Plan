# Nachweisliste iU0 / iU1 — Abnahme auf Windows

**Stand 02.09.2026 · Branch `ios_migration` · `c3a8233`..`ce2dc9e` (+ P1.11)**

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

## Reihenfolge der Abnahme

Von billig nach teuer — jeder Schritt setzt den vorigen voraus.

1. **VS 2026 öffnet `WP-Plan.sln`** und zeigt **7 Projekte**; `Debug|x64` baut durch.
   Danach `WP-Plan.Kern.slnf` öffnen: genau vier Projekte.
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
