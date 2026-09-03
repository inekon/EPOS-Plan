# CLAUDE.md — `EPOS.iOS`, die iOS-Hülle

MAUI-App (`net10.0-ios`, `Microsoft.NET.Sdk.Razor`, `UseMaui`) mit **einer** `ContentPage` und
darin **einer** `BlazorWebView`. Der Rechenweg liegt in [`EPOS.Kern`](../EPOS.Kern/CLAUDE.md), die
Oberfläche in [`EPOS.UI`](../EPOS.UI/CLAUDE.md). Diese Hülle trägt nur, was die Plattform
beisteuert (Umsetzungskonzept iOS, Paket iU10).

**Die eine Regel: Hier steht nichts Fachliches.** Kein Rechenweg, keine Maske, kein SQL. Wer hier
etwas über Wärmepumpen oder Kapitalwerte schreiben will, ist im falschen Projekt.

## Warum MAUI und nicht ein reines `Microsoft.iOS`-Projekt

Für `net10.0-ios` gibt es außerhalb von `Microsoft.AspNetCore.Components.WebView.Maui` **keinen**
BlazorWebView-Host — `Microsoft.AspNetCore.Components.WebView` allein ist abstrakt, ein eigener
`WebViewManager` über `WKWebView` wäre ungestützter Eigenbau. Dazu decken die MAUI-Essentials sechs
der neun Dienstadapter ab. Die MAUI-Fläche bleibt trotzdem minimal: **keine Shell, kein XAML, keine
MAUI-Navigation** — die Navigation lebt in Blazor (`EPOS.UI/Seiten/AppWurzel`).

## Was hier liegt

| Datei / Ordner | Inhalt |
|---|---|
| `EPOS.iOS.csproj` | das Projekt; **eigene `EPOS.iOS.sln`**, bewusst nicht in `WP-Plan.sln` und nicht im Solution-Filter — auf ubuntu und windows gibt es die iOS-Workload nicht (NETSDK1147) |
| `MauiProgram.cs` | der Aufbau: die neun `Dienste.*` des Kerns belegen, die Datenbank bereitstellen, das DI-Verzeichnis der WebView füllen. Das iOS-Gegenstück zu `WindowsFormsApplication1/Program.cs` |
| `App.cs`, `HauptSeite.cs` | ein Fenster, eine Seite, eine `BlazorWebView` mit `EPOS.UI.Seiten.AppWurzel` |
| `wwwroot/index.html` | die Startseite der WebView — zeichengleich zur Windows-Fassung bis auf `EPOS.iOS.styles.css` und `viewport-fit=cover` |
| `Dienste/` | die iOS-Fassungen der neun Umgebungsdienste und der beiden `EPOS.UI`-Schnittstellen |
| `Datenbankbereitstellung.cs` | Seed-Kopie beim Erststart und `DataRepository.PfadUeberschreibung` |
| `Pruefung/` | der Prüfmodus für die CI (`EPOS_PRUEFLAUF`) |
| `Platforms/iOS/` | `Main.cs`, `AppDelegate.cs`, `Info.plist` |
| `Resources/` | Programmsymbol und Startbild (Platzhalter bis iU13) |

## Regeln für Änderungen hier

- **Nichts, was auch woanders stehen kann.** Eine Fachänderung gehört in `EPOS.Kern`, eine Maske
  in `EPOS.UI`. Hier bleibt nur, was `UIKit`, `Foundation` oder `Microsoft.Maui.*` braucht.
- **Die neun Dienste werden an genau EINER Stelle belegt** — `MauiProgram.CreateMauiApp`, in
  derselben Reihenfolge wie `Program.Main` unter Windows (`Program.cs:93–122`) und **vor** dem
  ersten Datenbankzugriff.
- **Zwei Verzeichnisse, nicht eins.** Die Umgebungsdienste des Kerns liegen im statischen Halter
  `Dienste` (iU5); das DI-Verzeichnis von MAUI trägt nur, was die `BlazorWebView` und die
  Komponenten von `EPOS.UI` brauchen. Dieselbe Aufteilung wie in `BlazorDienste.cs`.
- **Die Datenbank wird nie im Anwendungspaket beschrieben.** Das Paket ist unter iOS
  schreibgeschützt; gearbeitet wird auf der Kopie in `Library/Application Support/WP-Plan/EPOS_PLAN`.
- **`bundle_e_sqlite3`, nicht `bundle_green`** — dieselbe SQLite 3.53.3 wie auf Windows, Linux und
  im macOS-CI. Begründung in `Directory.Packages.props`.
- **Ein Namensraum für die ganze Hülle: `EPOS.iOS`** — auch für die Dateien unter `Dienste/`.
  Ein Unter-Namensraum `EPOS.iOS.Dienste` verdeckte den statischen Halter
  `WindowsFormsApplication1.Dienste` des Kerns: `Dienste.Pfade` löste dann gegen den eigenen
  Ordner auf und der Build brach. Die Windows-Hülle hält es genauso — `WindowsFormsApplication1/Dienste/*.cs`
  liegen alle im Namensraum `WindowsFormsApplication1`.
- Bezeichner und Kommentare deutsch; neue `.cs` UTF-8 **mit** BOM, LF; `.csproj`, `.plist`, `.html`
  ohne BOM.

## Bauen und prüfen

Nur auf macOS mit installierter Workload — **auf Linux und Windows ist der Bau unmöglich**
(NETSDK1147). Der Nachweis läuft deshalb im CI-Job `.github/workflows/ios.yml`, der von Hand
ausgelöst wird (GitHub → Actions → iOS → *Run workflow*).

```bash
dotnet workload install maui-ios --version 10.400.1 --skip-sign-check
dotnet build EPOS.iOS/EPOS.iOS.csproj -c Release -f net10.0-ios -r iossimulator-arm64 \
  -p:SeedDb=../Referenzlaeufe/Kenndaten_Test.sqlite
```

Was sich **ohne** Mac prüfen lässt, steht in `../Umsetzung_iU10_Nachweise.md`: eine Restore-Probe
mit `net10.0`-Stub gegen die echte `Directory.Packages.props` und ein Übersetzungslauf der
plattformfreien Dateien dieser Hülle.
