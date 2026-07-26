# CLAUDE.md — Projektgedächtnis WP-Plan (WindowsFormsApplication1)

Diese Datei gibt Claude (und künftigen Cowork-Sessions) den nötigen Kontext, um in
diesem Repository sofort produktiv zu arbeiten. Antworten und Code-Kommentare auf
Deutsch, da Codebasis und Domäne deutschsprachig sind.

## Überblick

**WP-Plan / „Wärmeplan"** ist eine Windows-Desktop-Anwendung zur Planung und
Simulation von Energie- und Wärmeversorgungskonzepten für Gebäude und Projekte.
Abgedeckte Gewerke u. a.: Wärmebedarf, Brauchwasser, Prozesswärme, Heizkessel,
BHKW, Wärmepumpe, Solarthermie, Photovoltaik, Stromspeicher, Pufferspeicher,
Klimadaten. Es gibt einen Wizard-geführten Workflow, Import von Herstellerdaten
(VDI 3805, CEC, PAN) und eine Simulations-Engine mit Ganglinien-/Chartauswertung.

## Tech-Stack

- **Sprache/Runtime:** C#, `net8.0-windows` (migriert von .NET Framework — siehe
  `WindowsFormsApplication1.csproj.netfx-backup`). `LangVersion=latest`,
  `Nullable=disable`.
- **UI:** Windows Forms (`UseWindowsForms=true`), zusätzlich WPF aktiviert
  (`UseWPF=true`). MDI-Oberfläche.
- **Ausgabe:** `WinExe`. Namespace & Assembly: `WindowsFormsApplication1`.
- **Plattform:** primär **x86** (zwingend wegen `Microsoft.ACE.OLEDB.12.0`,
  bitness-gebunden). Zusätzliche Configs: `x64`, `AnyCPU`.
- **Datenbank:** Microsoft Access (`.accdb`) über ODBC (`OdbcConnection`) und
  OLEDB (ACE 12.0). Pakete `System.Data.Odbc`, `System.Data.OleDb`.
- **Wichtige NuGet-Pakete:** `WinForms.DataVisualization` (Chart-Port),
  `ScottPlot.WinForms`, `SkiaSharp(*)`, `MathNet.Numerics`,
  `Mscc.GenerativeAI` (Google Gemini), `Humanizer.Core`, `JsonSchema.Net(.Generation)`,
  `System.Configuration.ConfigurationManager`, `Microsoft.Win32.Registry`,
  `System.Management`, `Microsoft.Extensions.Http/Logging`.
- **COM-Interop:** `Microsoft.Office.Interop.Excel`, `VBIDE`, sowie eine
  Geschwister-EXE `..\CSExeCOMServer.exe` (.NET-Framework-COM-Server).
- **Lokalisierung:** Satellitenkulturen `de-DE` und `en-US` (Standard: Deutsch).
- **DPI:** DpiUnaware (in `app.manifest` `dpiAware=false` und zur Laufzeit
  `Application.SetHighDpiMode(HighDpiMode.DpiUnaware)` in `Program.cs`). Der
  anderslautende PerMonitorV2-Kommentar im `.csproj` ist veraltet.

## Build & Ausführen

> Voraussetzung: **Windows** (WinForms/WPF, COM-Interop, ACE-OLEDB-Provider).
> Lässt sich nicht auf Linux/macOS bauen oder ausführen. Für den ACE-OLEDB-Zugriff
> muss die **32-bit Access Database Engine (ACE 12.0)** installiert sein.

Es existiert **keine `.sln`** — Projekt direkt über die `.csproj` öffnen/bauen.

```powershell
# Bauen (x86 wegen ACE OLEDB)
dotnet build WindowsFormsApplication1.csproj -c Debug -p:Platform=x86

# Ausführen
dotnet run --project WindowsFormsApplication1.csproj -p:Platform=x86

# Release-Build
dotnet build WindowsFormsApplication1.csproj -c Release -p:Platform=x86
```

In Visual Studio: die `.csproj` öffnen und die Plattform **x86** wählen.

## Architektur

Grob nach **MVC** getrennt; Verschaltung über globale Statics in `Program`:

- **Einstieg:** `Program.cs` → `static Main` startet die MDI-Oberfläche
  (`MDIMainForm`). `Program` hält globale, prozessweite Statics:
  `mdifrm`, `mainfrm`, `startfrm`, `menuectrl`, `wizardctrl`, `DBConnection`,
  `HelpCatalog`, Pfade (`ApplicationPath_Common/_User`), `nLanguage`.
  Sprache wird aus der Registry `HKCU\Software\wp-plan` (`Language`) gelesen.
- **Controller/** (~51 Dateien, Suffix `*Ctrl.cs`): Anwendungslogik je Gewerk
  (z. B. `WaermebedarfCtrl`, `BHKWCtrl`, `PhotovoltaikCtrl`, `WPCtrl`,
  `ProjektCtrl`, `WizardCtrl`, `MenueCtrl`). Kontextmenüs als `*KontextMenuCtrl`.
- **Model/** (~33 Dateien, Suffix `*Model.cs`): Datenmodelle je Gewerk
  (z. B. `WaermebedarfModel`, `PhotovoltaikModel`, `ProjektModel`).
  Projektbezogene Verknüpfungstabellen mit Präfix `Z_Projekt*`.
- **Views/** (~362 Dateien): WinForms-Formulare, organisiert in Domänen-Unterordnern
  (`BHKW`, `Photovoltaik`, `Solarthermie`, `Wärmepumpe`, `Simulation`, `Wizard`,
  `Hauptformular`, `Projekt`, `Admin`, `Help`, …). Formulare heißen `Form_*`.
- **Allgemein/** (~44 Dateien): geteilte Infrastruktur:
  - Datenzugriff: `DbClass`, `DataRepository`, `RecordSet`, `Update/`
  - Grafik: `GrafikTools/` (`ChartManager`, `RoundedPanel`, `Form_ChartZoom`)
  - Import: `Import/` → `VDI 3805/`, `CEC/`, `Pan/`, `CsvReader`, `IniFileParser`
  - Simulation: `Simulation/` (Engine, siehe unten)
  - Hilfe: `Hilfe/HelpCatalog` (WordPress-basiert)
  - Tools: `ToolsClass`, `SolarPVGISCalculator`, `WizardItemClass`

## Namens- & Code-Konventionen

- Einheitlicher Root-Namespace `WindowsFormsApplication1` (trotz Domänen-Ordnern).
- Klassensuffixe: Controller `*Ctrl`, Modelle `*Model`, Formulare `Form_*`.
- Bezeichner, Kommentare und UI-Texte überwiegend **deutsch**.
- Pro Formular bis zu 5 Dateien: `X.cs`, `X.Designer.cs`, `X.resx`,
  `X.de-DE.resx`, `X.en-US.resx`. **Designer- und `.resx`-Dateien nicht von Hand
  editieren** — über den WinForms-Designer pflegen; Strings über die
  Satelliten-`.resx` lokalisieren.
- `Properties/AssemblyInfo.cs` wird manuell gepflegt
  (`GenerateAssemblyInfo=false`).
- Vom Build ausgeschlossen (siehe `.csproj`): `ChartManagerNeu.cs`,
  `WPTestCtrl.cs` sowie die „- Kopie"-Dateien unter `Views/Simulation/`.

## Datenzugriff

- Backend: **MS Access** (`.accdb`). Zugriff via `OdbcConnection` (`DbClass`,
  DSN-/Connection-String) und ACE-OLEDB (`app.config` `connectionStrings`).
- Die in `app.config` hinterlegte Beispiel-Verbindung verweist auf einen alten
  absoluten Pfad (`...\WP-Plan\Kenndaten.accdb`); der tatsächliche Pfad wird zur
  Laufzeit gesetzt. Beim Arbeiten an DB-Code beachten.
- Schema-/Datenpflege über `Allgemein/Update/UpdateDatabaseFromScript.cs`
  (Skripte: TABELLEN, SPALTEN, DATENTYPEN, IMPORT, DELETE).

## Simulation (`Allgemein/Simulation/`)

Engine mit `SimulationControl` und `Init`, plus Module je Erzeuger/Bedarf:
`SimulationWaermebedarf`, `SimulationStrombedarf`, `SimulationWaermepumpe`,
`SimulationBHKW`, `SimulationPV`, `SimulationSolarthermie`, `SimulationSPK`,
`SimulationSSP`. Auswertung als Ganglinien/Charts in den Views.

## Import (`Allgemein/Import/`)

- **VDI 3805:** Heizkessel, Pufferspeicher, Solarkollektoren, Wärmepumpen.
- **CEC / PAN:** PV-Moduldatenbanken (`CECDataService`, `PanDataService`,
  `UnifiedModule`).
- **Generisch:** `CsvReader`, `IniFileParser`.

## Externe Dienste & Integration

- **PVGIS** (`https://re.jrc.ec.europa.eu/api/tmy`) für TMY-/Solardaten
  (`SolarPVGISCalculator`).
- **Nominatim / OpenStreetMap** für Geokodierung.
- **WordPress-Hilfe** (`HelpCatalog`, `WordPressUrl`, Standard `localhost:8080`).
- **Google Gemini** via `Mscc.GenerativeAI`.
- **Excel/VBIDE-COM** und **`CSExeCOMServer.exe`** (Geschwisterordner) — nur unter
  Windows mit installiertem Office/COM-Server verfügbar.
- Einstellungen: `Properties/Settings.settings` + `app.config` `userSettings`
  (`VDI3805Path`, `PVGISUrl`, `GeoKodierung`, `WordPressUrl/Prefix`, Import/Export-Pfade).

## Fallstricke / Wichtige Hinweise

- **Immer x86 bauen**, sonst schlägt der ACE-OLEDB-Zugriff fehl.
- **Kein `.sln`** und (aktuell) **kein Git-Repo** im Ordner.
- `Nullable` ist deaktiviert — keine projektweiten NRT-Annahmen.
- COM-/Office-Interop und der externe COM-Server machen den Build
  **Windows-gebunden**.
- Viele veraltete/Legacy-Warnungen sind im `.csproj` bewusst per `NoWarn`
  unterdrückt (CA1416, MSB3568, CS1701/1702, NU1701, NETSDK1206, WFAC010).
- Globale, veränderliche Statics in `Program` — Zustand ist prozessweit; bei
  Änderungen Seiteneffekte bedenken.
- UI zweisprachig (de/en) — neue sichtbare Texte in beiden Satelliten-`.resx`
  pflegen.

## Verzeichnisstruktur (Kurzform)

```
WindowsFormsApplication1/
├─ Program.cs                 # Einstieg, globale Statics, Sprache/Registry
├─ MDIMainForm.*              # MDI-Hauptfenster (+ de-DE/en-US .resx)
├─ WindowsFormsApplication1.csproj
├─ app.config / app.manifest
├─ Controller/                # *Ctrl.cs — Logik je Gewerk
├─ Model/                     # *Model.cs — Datenmodelle
├─ Views/                     # Form_*.cs — Formulare (Domänen-Unterordner)
├─ Allgemein/                 # Geteilte Infrastruktur
│  ├─ DbClass / DataRepository / RecordSet / Update
│  ├─ GrafikTools / Hilfe
│  ├─ Import (VDI 3805, CEC, Pan, CSV, INI)
│  └─ Simulation
├─ Properties/                # AssemblyInfo, Settings, Resources
├─ Resources/ · MyResource/   # Ressourcen (Bilder, lokalisierte Strings)
└─ bin/ · obj/                # Build-Artefakte (nicht versionieren)
```
