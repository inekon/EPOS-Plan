# CLAUDE.md — WP-Plan / EPOS-Plan, Anwendungsprojekt

Kontext zum **Code** dieses Projekts. Fachdomäne, Datenmodell, Migration und Umgang mit
`Kenndaten.accdb` stehen in der [`CLAUDE.md` der Repo-Wurzel](../CLAUDE.md) — hier nicht wiederholen.
Antworten und Code-Kommentare auf Deutsch.

> **Der Rechenkern steht nicht mehr hier.** Seit Paket iU4 (03.09.2026) liegen 168 Kerndateien —
> `Allgemein/Simulation/`, `Allgemein/Wirtschaftlichkeit/`, `Model/`, die Zugriffsschicht, die
> Daten-Hälfte des Berichts und 50 Controller — im Projekt
> [`../EPOS.Kern/`](../EPOS.Kern/CLAUDE.md). Dieses Projekt referenziert es und übersetzt sie
> nicht mehr mit. **Regel: eine Fachänderung am Rechenkern wird EINMAL gemacht, im Kern.** Wer
> eine Datei hier sucht und nicht findet, sucht sie dort — die Ordnerstruktur ist dieselbe
> geblieben.

## Build

`net10.0-windows`, WinForms, `WinExe`. Namespace `WindowsFormsApplication1`;
**Assembly/EXE/Prozess `EPOS_Plan`** (Umbenennung Stufe 0 am 29.08.2026 — nur der
Ausgabename; Stufe 1 = Namespace-Umstellung bräuchte ein eigenes Konzept).
Solution: `..\WP-Plan.sln` (Debug/Release × x64).

```powershell
dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64
```

**Das ist seit dem 02.09.2026 der Standardbefehl** — er funktioniert in VS 2026, auf der
Kommandozeile und in der CI. Die beiden COM-Referenzen (`Microsoft.Office.Interop.Excel`,
`VBIDE`), an denen der Bau bis dahin in `ResolveComReference` abbrach und deshalb das MSBuild von
Visual Studio verlangte, wurden am 02.09.2026 mit Paket iU1-P1.1 **entfernt**; der Excel-Import
läuft seither über ClosedXML. Die SDK-Fassung ist in `..\global.json` gepinnt (10.0.400),
gemeinsame Buildeigenschaften stehen in `..\Directory.Build.props`.

Alternative, falls doch einmal das MSBuild von Visual Studio gebraucht wird:

```powershell
# MSBuild versionsunabhaengig ueber vswhere finden (VS 2026 liegt unter ...\18\, nicht ...\2022\)
$msb = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -prerelease -products * -requires Microsoft.Component.MSBuild `
        -find 'MSBuild\**\Bin\MSBuild.exe' | Where-Object { $_ -notmatch '\\amd64\\' } | Select-Object -First 1
& $msb ..\WP-Plan.sln -p:Configuration=Debug -p:Platform=x64
```

Fester Pfad je Fassung, falls `vswhere` nicht greift:
`…\Microsoft Visual Studio\18\Community\…` (VS 2026) bzw. `…\2022\Community\…` (VS 2022).

Build seit 22.08.2026 **x64** (Paket P2; davor x86). Ist-Stand, Entscheidungen und offene
Pakete (P3–P5) in
[`../Konzept_Umstellung_64Bit_EPOS-Plan.md`](../Konzept_Umstellung_64Bit_EPOS-Plan.md);
Rückweg: Git-Tag `letzter-x86-stand`.

**WFO1000 ist in .NET 10 standardmäßig ein *Fehler*** (WinForms-Designer-Serialisierung). Die
`..\.editorconfig` stuft ihn auf `warning` herab, damit der Bestand baut, lässt ihn aber sichtbar:
**60 Fundstellen**, Schwerpunkt `Form_Gesetzesparameter` (10), `Form_Kosten_VarAuswahl` (8) und die
Karten-Controls des Kostenmoduls. Die Annotation je Property ist eine Fachentscheidung und gehört
zu Paket iU9 — nicht nebenbei miterledigen.

Das Setup (`..\Setup\build-setup.ps1`) wird derzeit von VS-MSBuild auf `dotnet publish`
umgestellt (Paket iU1-P1.10, erledigt 02.09.2026, `ce2dc9e`).


## Architektur

Grob MVC, verschaltet über prozessweite Statics in `Program`:

- **`Program.cs`** — `Main` startet die MDI-Oberfläche. Hält `mdifrm`, `mainfrm`, `startfrm`,
  `menuectrl`, `wizardctrl`, `HelpCatalog`, `ApplicationPath_Common/_User`, `nLanguage`
  (Sprache aus Registry `HKCU\Software\wp-plan`). Globaler veränderlicher Zustand — Seiteneffekte
  bei Änderungen mitdenken. `nLanguage`, `ZahlParsen` und `GanzzahlParsen` sind seit iU4-1 nur
  noch **Weiterleitungen** auf `Sprache` bzw. `ZahlText` im Kern; `Main` setzt außerdem die Haken
  `Meldung.*` und `WErzeugerCtrl.GeraetewaisenAufraeumen`, über die Kern-Code Meldungen absetzt
  und den Geräte-Aufräumlauf anstößt, ohne WinForms zu kennen.
- **`Controller/`** (**49** Dateien, `*Ctrl.cs`) — was Oberfläche braucht: Kontextmenüs als
  `*KontextMenuCtrl`, `MenueCtrl`, `WizardCtrl`, die Stamm-Controller mit `MessageBox` und
  `WPCtrl`. Die übrigen **50** liegen seit iU4 in `../EPOS.Kern/Controller/`.
- **`Model/`** — **keine `.cs` mehr**; alle 46 Modelle liegen in `../EPOS.Kern/Model/`.
- **`Views/`** (185 `.cs`, 384 Dateien) — `Form_*` in Domänen-Unterordnern (BHKW, Photovoltaik,
  Wärmepumpe, Simulation, Wizard, Bericht, Wirtschaftlichkeit, Varianten, Admin, Help …).
- **`Allgemein/`** (**82** Dateien) — geteilte Infrastruktur, siehe unten. `Simulation/` (bis auf
  `SchemaModell.cs`), `Wirtschaftlichkeit/` (vollständig) und die Daten-Hälfte von `Bericht/`
  sind mit iU4 in den Kern gezogen; hier bleiben die Bericht-AUSGABE (7 Dateien),
  `Update/` (Schemamigration, Access-Zweig), `Katalog/`, `Import/`, `KI/`, `Hilfe/`,
  `GrafikTools/`, `Export/` und `Lizenz/`.

Die Aufteilung im Einzelnen — was im Kern liegt und was mit Absicht hier geblieben ist — steht im
Kopfkommentar von [`../EPOS.Kern/EPOS.Kern.csproj`](../EPOS.Kern/EPOS.Kern.csproj) und in
[`../EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md).

## Module in `Allgemein/`

| Ordner | Inhalt |
|---|---|
| `Bericht/` | Die **AUSGABE**-Hälfte des Berichtsmoduls: `WordBerichtGenerator` (OpenXML, Vorlage `Vorlagen/Berichtsvorlage.docx`), `ExcelBerichtGenerator` (ClosedXML), `ChartRenderer` (GDI+/PNG), `Bausteine/` (konfigurierbare Berichtsteile), `BerichtsDatenSammler`, `BerichtsKonfiguration`, `ZeitreihenExtraktor`, `IBerichtsBaustein`. Sie zieht bis iU7 nicht um. Die **DATEN**-Hälfte (`BerichtTexte` de/en, `BerichtsDaten`, `EmissionsAusweis`, `KostenEmissionRechner`, `ProjektDetails`, `KennzahlenKatalog`, `AbweichungsErmittler`) liegt seit iU4 in `../EPOS.Kern/Allgemein/Bericht/` |
| `Wirtschaftlichkeit/` | **vollständig in `../EPOS.Kern/Allgemein/Wirtschaftlichkeit/`** (iU4): `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl` und 16 weitere |
| `Simulation/` | **bis auf `SchemaModell.cs` (Schema-Ansicht) in `../EPOS.Kern/Allgemein/Simulation/`** (iU4). Engine: `SimulationControl`, `Init`, `SimulationRunner` + Module je Erzeuger/Bedarf (`SimulationWaermebedarf`, `…Waermepumpe`, `…BHKW`, `…PV`, `…Solarthermie`, `…SPK`, `…SSP`, `…Pufferspeicher`). Seit der Konzeptumsetzung 27./28.08.2026 (**ein Rechenweg, dreikanalig** Heizung/Brauchwasser/Prozess): `Kaskadenschleife` (Stundenschleife Phasen A–G, Ladeaufträge je Rang), `SimulationKanaele` (`Kanal`/`Kanalsatz`/`Senkenliste`/`Ladeordnung`-Umfeld), `WaermequelleClass`/`WaermesenkeClass`, `Warnkriterien` (Katalog W1–W6 + harte Guards, eine Wahrheit für Dialog und Laufstart), `ProfilBedarf`, `SchemaModell` (Schema-Ansicht), `StilleDb`; Schichtspeichermodell (N = 1…10, SOC führend) vollständig in `SimulationPufferspeicher`; Booster-Quelltemperatur stundengekoppelt, Lesepunkt je Projekt wählbar (`Tab_Einstellungen.Booster_Lesepunkt`, Default „Davor" = Stundenanfang; Paket B2); Kessel-Temperaturbezug je Anlage `Tab_Energieanlagen.WQ_TemperaturModus` („Berechnet" = Bezugskette Senkenspeicher→Katalog→70/50, Default, ohne Pflegezwang; „Fest" = Vorgabe, Warnung nur wenn Paar fehlt). Historie und Invarianten je Paket: `*_Protokoll.md` im selben Ordner |
| `Lizenz/` | `LizenzManager`, `LizenzToken`, `LizenzServerClient`, `GeraeteId` — signiertes Token, DPAPI-Ablage, Zustände von `NichtAktiviert` bis `Lesemodus` |
| `KI/` | `KiChatService` (Gemini 2.5 Flash-Lite über REST), `HilfeKontext`, `HilfeWissen`, seit 29.08.2026 `WikiWissen` (Wiki-Suche + Klartext-Auszüge + 24-h-Cache `%APPDATA%\wp-plan\wiki-wissen\`, speist die „Hilfeabschnitte" des Prompts; Chatfenster ohne KI = Online-Doku-Suche; Protokoll `H4H5_Umsetzung_Protokoll.md`); API-Key als DPAPI-Datei `%APPDATA%\wp-plan\ki-schluessel.dat` (Registry-Altwert wird einmalig migriert und gelöscht) |
| `Import/` | `VDI 3805/` (Kessel, Puffer, Kollektoren, WP), `CEC/` + `Pan/` (PV-Module), `CsvReader` |
| `GrafikTools/` | `ChartManager`, `RoundedPanel` |
| `Hilfe/` | `WikiHelpCatalog` (in `HelpCatalog.cs`) — lädt die Rubrik `Programm Dokumentation/` von `wiki.epos-plan.de` (Action-API `allpages`+`apprefix`, Basis-URL aus `Settings.WordPressUrl`, Not-Rückfall `Program.WIKI_STANDARD`); `HilfeAutomatik`, `help_mapping.txt`/`help_cache.json` (Ziele = Kurznamen der Rubrik-Unterseiten, optional `#anker`), `DokuUebersetzung` (EN über translate.goog). Umsetzung 29.08.2026, Protokoll `H1H2_Umsetzung_Protokoll.md` im selben Ordner |
| `Reporting/`, `Waermespeicher/` | **nur Konzept-/Standdokumente**, kein Code |

**Datenzugriff:** `DataRepository.cs` — Standard, in ~160 Dateien; die Datei liegt seit iU4 in `../EPOS.Kern/Allgemein/`. Seit 02.09.2026 (`6486c36`)
spricht sie **SQLite** über `Microsoft.Data.Sqlite` (`Data Source=<Pfad>\Kenndaten.sqlite`,
`PRAGMA foreign_keys = ON` je Verbindung); den Verbindungsstring baut zentral `GetConnectionString()`.
Die Ausführungsmethoden nehmen seit 02.09.2026 `params DbParam[]` (`Allgemein/DbParam.cs`, eigener
Parametertyp mit `DbParamTyp`-Enum) — **nicht mehr `OleDbParameter`**, das auf Nicht-Windows schon im
Konstruktor `PlatformNotSupportedException` wirft. Übersetzt wird innen (`?` → `@pN`,
`UebersetzeParameterzeichen`). Eine implizite Brücke `OleDbParameter → DbParam` (und `DbParam.Von(…)`
für Arrays) hält die 432 Altaufrufe unter `Views/` lauffähig, bis sie mit ihren Masken umgestellt
werden (iU9); **neuer Code nutzt ausschließlich `new DbParam(…)`**. `System.Data.OleDb` bleibt nur
noch wegen der Brücke, `RecordSet.DBCommand` (iR8) und des `.accdb`-Migrationspfads referenziert. Transaktionen
laufen über `DbVorgang`. `RecordSet.cs` (string-konkateniertes SQL, 47 echte Nutzer) ist Altbestand,
innen ebenfalls auf SQLite, die Property `DBCommand` bleibt `OleDbCommand`. Neuer Code
ausschließlich über `DataRepository`; das Ziel `IDatenzugriff`/`DbParam` steht im
`Umsetzungskonzept_iOS_EPOS-Plan.md` (iU6). Betrieb: `BETRIEB_SQLITE.md`.

**Rechenkern:** vollständig verwaltet in `../EPOS.Kern/Allgemein/BhkwPlan.cs` (Namespace `WPPlan.Core`, seit iU4 dort), aufgerufen
aus den `Simulation*`-Klassen und einigen Eingabeformularen. Keine native DLL, kein COM-Server, kein
`DllImport`. Der frühere Weg über `..\CSExeCOMServer` ist abgelöst; das Projekt wurde am
02.09.2026 aus dem Repo **entfernt** (Paket iU0-P0.1). Historie:
`git show 922228a:CSExeCOMServer/`.

Der Port bildet das Verhalten des Vorgängers bewusst genau nach: Feldgrößen fest auf 8760 Stunden,
168 Wochenwerte, 365 Tage, 12 Monate, 24 Tagesstunden; Vektoren `float` mit Zwischenrechnung in
`double`; Arrays werden **in-place** überschrieben, der Rückgabewert wird fast überall ignoriert.
Diese Konventionen beim Erweitern beibehalten.

## Konventionen

- Root-Namespace `WindowsFormsApplication1` für alles, trotz Domänen-Ordnern.
- Suffixe: `*Ctrl`, `*Model`, `Form_*`. Bezeichner, Kommentare und UI-Texte deutsch.
- Pro Formular bis zu 5 Dateien: `X.cs`, `X.Designer.cs`, `X.resx`, `X.de-DE.resx`, `X.en-US.resx`.
  **Designer- und `.resx`-Dateien nicht von Hand editieren** — über den WinForms-Designer pflegen,
  Strings über die Satelliten-`.resx` lokalisieren.
- Vom Build ausgeschlossen (`.csproj`): `ChartManagerNeu.cs`, `Form_Simulation_Kurz.*`
  und die „- Kopie"-Dateien unter `Views/Simulation/`.
- **Drei-Schichten-Regel für Texte** (Konzept 13.6, umgesetzt mit Paket 9): **Persistenz** —
  alles, was in `Kenndaten.accdb` steht oder in SQL damit verglichen wird, bleibt **deutsch und
  eingefroren**; die Werte stehen zentral in `Allgemein/DbWerte.cs`, nie als Literal im Code.
  **Schlüssel** — Chart-Serien, ComboBox-Steuerwerte, Filter-Tokens: sprachneutral und ASCII
  (`PUFFER_12`, `WAERMEBEDARF`). **Anzeige** — ausschließlich über `MyResource.Resource.*`
  (Katalog in beiden Sprachen, Fundstellen in
  [`Allgemein/Simulation/Lokalisierung_Katalog.md`](Allgemein/Simulation/Lokalisierung_Katalog.md)).
  Kein Anzeigetext darf Steuerwert sein — Prüfrezeptur:
  [`Allgemein/Simulation/Lokalisierung_Pruefung.md`](Allgemein/Simulation/Lokalisierung_Pruefung.md).

## Wichtige Pakete

`WinForms.DataVisualization` (Chart-Port mit Original-Namespace) · `ScottPlot.WinForms` + `SkiaSharp`
· `MathNet.Numerics` · `DocumentFormat.OpenXml` und `ClosedXML` (Berichte ohne Office) ·
`BouncyCastle.Cryptography` + `System.Security.Cryptography.ProtectedData` (Lizenz) ·
`System.Data.OleDb` ·
`Mscc.GenerativeAI`.

**`SixLabors.Fonts` ist bewusst auf 1.0.1 gepinnt** — ab 2.x gilt die Six Labors Split License.
Vor Releases `dotnet list package --include-transitive` prüfen.

## Fallstricke

- Die früheren Altkopien `..\WindowsFormsApplication1 - Kopie` und
  `..\mit_Puffer_KI_Lösungsversuch` (alte Vollkopien mit fast identischen Dateinamen) wurden am
  29.08.2026 entsorgt — die Verwechslungsgefahr beim Suchen/Greppen besteht nicht mehr.
- **Alle `.cs`-Dateien sind seit dem 02.09.2026 UTF-8** (Paket iU1-P1.12: 68 cp1252-Dateien
  umkodiert). Die `.editorconfig` verlangt für neue `.cs` UTF-8 **mit** BOM; im Bestand tragen
  455 von 573 Dateien eine BOM, 118 nicht — das ist unschädlich (UTF-8 ohne BOM ist eindeutig),
  beim Bearbeiten den vorhandenen Zustand je Datei beibehalten. Die frühere Kodierungsfalle
  (cp1252 ohne BOM, Umlautschaden beim Speichern) ist damit Geschichte.
- **DPI:** faktisch DpiUnaware (`app.manifest` `dpiAware=false` + `Application.SetHighDpiMode(DpiUnaware)`
  in `Program.cs`). Der `PerMonitorV2`-Kommentar im `.csproj` ist falsch.
- **`app.config`** enthält einen toten absoluten Beispielpfad zur `.accdb`; der echte Pfad wird zur
  Laufzeit über `DataRepository.GetDBPath()` gesetzt.
- **Lokalisierung lückenhaft:** `Admin`, `BHKW`, `Bericht`, `Brauchwasser`, `Help`, `Klimadaten`,
  `Photovoltaik`, `Varianten`, `Wirtschaftlichkeit` haben keine `de-DE.resx`. Bei neuen sichtbaren
  Texten in bestehenden Ordnern beide Satelliten-Dateien pflegen.
- `.gitignore` schließt `*.accdb` aus — Datenbankänderungen landen nie im Commit.
  `..\GitHub_Sync.bat` committet mit `git add -A` und synchronisiert den aktuell
  ausgecheckten Branch mit seinem GitHub-Gegenstück (seit 26.08.2026; vorher fest `origin/main`).
- **Wegwerf-Harnesse nur unter `..\dev\` (Repo-Wurzel, gitignored).** Die `.csproj` sammelt
  `**\*.cs` ein — eine `.cs`-Datei unterhalb von `WindowsFormsApplication1\` (auch in einem
  eigenen Unterordner) bricht den Build sofort (CS0017, zweites `Main`). Dasselbe gilt seit iU4-5
  für `..\EPOS.Kern\`, das ebenfalls per Globbing aufnimmt — dort bricht zusätzlich jede
  WinForms-Berührung den Build (`EnableWindowsTargeting=false`).
  Ein Harness unter `..\dev\` erbt `..\Directory.Build.props` (und künftig
  `..\Directory.Packages.props`) — dort deshalb
  `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` setzen, sonst scheitert
  der Restore an Paketreferenzen mit eigener Version (NU1008).
- **Läuft die Anwendung, ist `bin\` gesperrt** (EXE + DLL geladen) — Verifikations-Builds dann
  mit `-p:OutDir=<Ordner außerhalb>` umleiten; der Compile-Beweis bleibt vollwertig.
- **Assembly heißt `EPOS_Plan`, der Namespace weiter `WindowsFormsApplication1`**
  (Stufe 0, 29.08.2026). Folgen: In `bin\` kann eine alte `WindowsFormsApplication1.exe`
  als Leiche liegen (einmal bereinigen); Wegwerf-Harnesse unter `..\dev\` referenzieren
  künftig `EPOS_Plan.dll`; die `user.config`-Ablage wandert mit dem Namen (Bestand war
  leer — geprüft, keine Übernahme nötig); das DLL-Tausch-Rezept der Referenzläufe
  tauscht künftig `EPOS_Plan.dll`.
- **Visual Studio regeneriert `../EPOS.Kern/MyResource/Resource.Designer.cs` selbst** (die Datei ist mit iU4-5 dorthin gezogen), sobald es eine
  `.resx`-Änderung bemerkt (alphabetische Einordnung). Wer den Designer parallel von Hand
  ergänzt hat, baut Duplikate (CS0102) — vor dem Build prüfen und die Hand-Einfügung
  entfernen, die generierte behalten.

## Stand & Konzepte

Aktueller Umsetzungsstand von Bericht und Wirtschaftlichkeit:
[`Allgemein/Reporting/UMSETZUNGSSTAND.md`](Allgemein/Reporting/UMSETZUNGSSTAND.md).
Konzepte daneben im selben Ordner (`Konzept_Berichtserstellung_EPOS-Plan.md`,
`Konzept_Wirtschaftlichkeit.md`, `Konzept_Variantenbericht.md`), Phasen-Historie in
`Allgemein/Bericht/LIESMICH_Phase1.md`. Simulationskonzepte in `Allgemein/Simulation/`:
`Konzept_Simulation_QuellenSenken.md` (umgesetzt) und
`Konzept_Brauchwasser_Heizung_Pufferspeicher.md` — **vollständig umgesetzt 27./28.08.2026**
(Dreikanalbilanz, Senkentabelle, Warnkriterien, Altpfad-Abriss, Schichtspeicher, Booster,
Quellprofile; Migrationsschritte 48–54; inzwischen vergeben bis 58 (55 = B2-Temperaturbezug,
56/57 = CO₂-Saat/Emissionsarten, 58 = E6-Quellensaat), neue ab 59). Je Paket ein Umsetzungsprotokoll
(`V0_…` bis `L_Aufraeumen_Protokoll.md` + Nachträge `E2_…`, `DCheck_…`) — das L-Protokoll
trägt die Abschlusstabelle aller offenen Punkte. Regressionsnetz: `..\Referenzlaeufe\`
(aktuelle Basis siehe dortiges `LIESMICH.md`; das Werkzeug `..\Referenzlauf\` ist
Messinstrument und wird nie zusammen mit Engine-Änderungen umgebaut).
