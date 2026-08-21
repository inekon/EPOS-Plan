# CLAUDE.md — WP-Plan / EPOS-Plan, Anwendungsprojekt

Kontext zum **Code** dieses Projekts. Fachdomäne, Datenmodell, Migration und Umgang mit
`Kenndaten.accdb` stehen in der [`CLAUDE.md` der Repo-Wurzel](../CLAUDE.md) — hier nicht wiederholen.
Antworten und Code-Kommentare auf Deutsch.

## Build

`net8.0-windows`, WinForms + WPF, `WinExe`, Namespace/Assembly `WindowsFormsApplication1`.
Solution: `..\WP-Plan.sln` (Debug/Release × x86/x64).

```powershell
dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x86
```

Die Umstellung auf x64 ist analysiert und geplant (noch nicht umgesetzt) — Ist-Stand,
Entscheidungsfragen und Pakete in
[`../Konzept_Umstellung_64Bit_EPOS-Plan.md`](../Konzept_Umstellung_64Bit_EPOS-Plan.md).


## Architektur

Grob MVC, verschaltet über prozessweite Statics in `Program`:

- **`Program.cs`** — `Main` startet die MDI-Oberfläche. Hält `mdifrm`, `mainfrm`, `startfrm`,
  `menuectrl`, `wizardctrl`, `HelpCatalog`, `ApplicationPath_Common/_User`, `nLanguage`
  (Sprache aus Registry `HKCU\Software\wp-plan`). Globaler veränderlicher Zustand — Seiteneffekte
  bei Änderungen mitdenken.
- **`Controller/`** (70 Dateien, `*Ctrl.cs`) — Logik je Gewerk; Kontextmenüs als `*KontextMenuCtrl`,
  Katalogpflege als `*StammCtrl`, Projektzuordnungen als `Z_Projekt*Ctrl`.
- **`Model/`** (36 Dateien, `*Model.cs`) — Datenmodelle je Gewerk.
- **`Views/`** (185 `.cs`, 384 Dateien) — `Form_*` in Domänen-Unterordnern (BHKW, Photovoltaik,
  Wärmepumpe, Simulation, Wizard, Bericht, Wirtschaftlichkeit, Varianten, Admin, Help …).
- **`Allgemein/`** (73 Dateien) — geteilte Infrastruktur, siehe unten.

## Module in `Allgemein/`

| Ordner | Inhalt |
|---|---|
| `Bericht/` | Berichtsmodul: `WordBerichtGenerator` (OpenXML, Vorlage `Vorlagen/Berichtsvorlage.docx`), `ExcelBerichtGenerator` (ClosedXML), `ChartRenderer` (GDI+/PNG), `Bausteine/` (konfigurierbare Berichtsteile), `BerichtTexte` (de/en) |
| `Wirtschaftlichkeit/` | `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl` |
| `Simulation/` | Engine: `SimulationControl`, `Init`, `SimulationRunner` + Module je Erzeuger/Bedarf (`SimulationWaermebedarf`, `…Waermepumpe`, `…BHKW`, `…PV`, `…Solarthermie`, `…SPK`, `…SSP`, `…Pufferspeicher`) |
| `Lizenz/` | `LizenzManager`, `LizenzToken`, `LizenzServerClient`, `GeraeteId` — signiertes Token, DPAPI-Ablage, Zustände von `NichtAktiviert` bis `Lesemodus` |
| `KI/` | `KiChatService` (Gemini 2.5 Flash-Lite über REST), `HilfeKontext`, `HilfeWissen`; API-Key als DPAPI-Datei `%APPDATA%\wp-plan\ki-schluessel.dat` (Registry-Altwert wird einmalig migriert und gelöscht) |
| `Import/` | `VDI 3805/` (Kessel, Puffer, Kollektoren, WP), `CEC/` + `Pan/` (PV-Module), `CsvReader` |
| `GrafikTools/` | `ChartManager`, `RoundedPanel` |
| `Hilfe/` | `HelpCatalog` — WordPress-basiert, Standard `https://epos-plan.de` |
| `Reporting/`, `Waermespeicher/` | **nur Konzept-/Standdokumente**, kein Code |

**Datenzugriff:** `DataRepository.cs` (OLE DB, `?`-Parameter) — Standard, in ~140 Dateien; den
ConnectionString (`Provider=Microsoft.ACE.OLEDB.12.0`) baut zentral `GetConnectionString()`
(einzige Abweichung mit eigenem Provider-String: `Views/Kosten/Form_KostenfaktorItem.cs`).
`RecordSet.cs` (string-konkateniertes SQL, ~60 Dateien) ist Altbestand, läuft aber ebenfalls über
OLE DB — ODBC ist vollständig abgelöst: `Program.DBConnection` existiert nicht mehr, einziger
ODBC-Rest ist die vom Build ausgeschlossene `Controller/WPTestCtrl.cs`, das Paket
`System.Data.Odbc` ist ungenutzt. Neuer Code ausschließlich über `DataRepository`.

**Rechenkern:** vollständig verwaltet in `Allgemein/BhkwPlan.cs` (Namespace `WPPlan.Core`), aufgerufen
aus den `Simulation*`-Klassen und einigen Eingabeformularen. Keine native DLL, kein COM-Server, kein
`DllImport` — der frühere Weg über `..\CSExeCOMServer` ist abgelöst, die verbliebenen
`CSExeCOMServer.SimpleObject`-Zeilen in den Simulationsklassen sind auskommentierter Altbestand und
können weg.

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
- Vom Build ausgeschlossen (`.csproj`): `ChartManagerNeu.cs`, `WPTestCtrl.cs`, `Form_Simulation_Kurz.*`
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
`System.Data.OleDb` (das ebenfalls referenzierte `System.Data.Odbc` ist ungenutzt) ·
`Mscc.GenerativeAI`.

**`SixLabors.Fonts` ist bewusst auf 1.0.1 gepinnt** — ab 2.x gilt die Six Labors Split License.
Vor Releases `dotnet list package --include-transitive` prüfen.

COM-Referenzen: `Microsoft.Office.Interop.Excel`, `VBIDE` (`EmbedInteropTypes=True`).

## Fallstricke

- **Suchen/Greppen nur in diesem Ordner.** `..\WindowsFormsApplication1 - Kopie` und
  `..\mit_Puffer_KI_Lösungsversuch` sind je ~210 MB alte Vollkopien mit fast identischen Dateinamen —
  Treffer daraus sind wertlos und führen zu Änderungen am falschen Code.
- **93 von 372 `.cs`-Dateien sind nicht UTF-8** kodiert (Umlaute als Ersatzzeichen). Beim Bearbeiten
  die vorhandene Kodierung beibehalten, sonst zerschießt der Diff die Datei.
- **DPI:** faktisch DpiUnaware (`app.manifest` `dpiAware=false` + `Application.SetHighDpiMode(DpiUnaware)`
  in `Program.cs`). Der `PerMonitorV2`-Kommentar im `.csproj` ist falsch.
- **`app.config`** enthält einen toten absoluten Beispielpfad zur `.accdb`; der echte Pfad wird zur
  Laufzeit über `DataRepository.GetDBPath()` gesetzt.
- **Lokalisierung lückenhaft:** `Admin`, `BHKW`, `Bericht`, `Brauchwasser`, `Help`, `Klimadaten`,
  `Photovoltaik`, `Varianten`, `Wirtschaftlichkeit` haben keine `de-DE.resx`. Bei neuen sichtbaren
  Texten in bestehenden Ordnern beide Satelliten-Dateien pflegen.
- `.gitignore` schließt `*.accdb` aus — Datenbankänderungen landen nie im Commit.
  `..\GitHub_Sync.bat` committet mit `git add -A` und pusht nach `origin/main`.

## Stand & Konzepte

Aktueller Umsetzungsstand von Bericht und Wirtschaftlichkeit:
[`Allgemein/Reporting/UMSETZUNGSSTAND.md`](Allgemein/Reporting/UMSETZUNGSSTAND.md).
Konzepte daneben im selben Ordner (`Konzept_Berichtserstellung_EPOS-Plan.md`,
`Konzept_Wirtschaftlichkeit.md`, `Konzept_Variantenbericht.md`), Phasen-Historie in
`Allgemein/Bericht/LIESMICH_Phase1.md`, Simulationskonzept in
`Allgemein/Simulation/Konzept_Simulation_QuellenSenken.md`.
