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
| `App.cs`, `HauptSeite.cs` | ein Fenster, eine Seite, eine `BlazorWebView` mit `EPOS.UI.Seiten.AppWurzel` — seit iU9‑W16c ist das die **gemeinsame Wurzel beider Plattformen** (Entscheid E‑1: eine Wurzel, zwei Schalen). Die Schale ist ein `RenderFragment` (`Kopfleiste`): Unter Windows steht dort das `Menueband` mit seinen 55 Punkten, **auf iOS bleibt sie leer** — iL5 sagt „kein MDI, keine modalen Ketten", und eine Menüleiste wäre auf Touch unbedienbar. Ohne Angabe macht die Wurzel mit `PROJEKTLISTE` auf, wie bisher |
| `wwwroot/index.html` | die Startseite der WebView — zeichengleich zur Windows-Fassung bis auf `EPOS.iOS.styles.css` und `viewport-fit=cover` |
| `Dienste/` | 12 Dateien: die neun Umgebungsdienste des Kerns als `Ios*`, dazu `IosHilfeDienst` und `IosProjektQuelle` (die beiden `EPOS.UI`-Schnittstellen) und der plattformfreie `Dateifilter`. **iU9‑W16c hat hier NICHTS geändert**: Die drei neuen Glieder — `IProjektQuelle.StartseiteGaben`/`BerichteKostenGaben` (K7) und `IDateiDienst.AdresseOeffnen` (der Browserstart) — sind Standardumsetzungen (`null` bzw. `false`), damit die Hülle durch die Erweiterung nicht bricht. Wer Startseite, Berichte und die Online-Dokumentation auf dem iPad will, legt die drei Fassungen mit **iU11** nach; bis dahin sagt `AppWurzel` es im Banner, statt leer zu bleiben |
| `Datenbankbereitstellung.cs` | Seed-Kopie beim Erststart, `DataRepository.PfadUeberschreibung`, die Gate-Zeilen `SQLite …`/`STRICT=…` und `VACUUM INTO` für die Sicherung |
| `Pruefung/Prueflauf.cs` | der Prüfmodus für die CI (`EPOS_PRUEFLAUF`); `Ergebnisexport.cs` und `Protokoll.cs` sind aus `Referenzlauf/` **verlinkt**, nicht kopiert |
| `Platforms/iOS/` | `Main.cs`, `AppDelegate.cs`, `Info.plist` |
| `Resources/` | Programmsymbol und Startbild (Platzhalter bis iU13) |

## Die neun Adapter auf einen Blick

| Schnittstelle | iOS-Fassung | Technik | Besonderheit |
|---|---|---|---|
| `IPfade` | `IosPfade` | `NSSearchPath` | `Library/Application Support` statt `Environment.SpecialFolder` (auf Apple-Mobile lieferte das `~/.config`); Unterordnernamen zeichengleich zum Bestand |
| `IEinstellungen` | `IosEinstellungen` | `Preferences` | Präfix `wp-plan.`; `LiesMaschine` liefert die Vorgabe (MDM erst iU11) |
| `ILizenzAblage` | `IosLizenzAblage` | `SecureStorage` | Geltungsbereich als **Namenssuffix**; `Vorhanden`/`Loeschen` betrachten beide Einträge |
| `IGeraeteId` | `IosGeraeteId` | `UIDevice` | `identifierForVendor` + Modell — neuer Abdruck, also neues Gerät am Lizenzserver |
| `ISprache` | `IosSprache` | `NSLocale` + `Dienste.Einstellungen` | ohne gespeicherten Wert entscheidet die Gerätesprache; Umstellung wirkt sofort (kein Neustart) |
| `IDateiDienst` | `IosDateiDienst` | `FilePicker`, `Share` | `DateiSpeichern` liefert einen Pfad unter `Documents`; `OrdnerWaehlen` = `""` |
| `IDialogDienst` | `IosDialogDienst` | `Page.DisplayAlert` | vom Hauptfaden aus wird **nicht** gefragt, sondern „nein"/„Abbruch" geantwortet (iR-f) |
| `INavigation` | `IosNavigation` | — | reicht an `EPOS.UI.Dienste.Navigationsziel` weiter |
| `IProjektKontext` | `IosProjektKontext` | — | **dünne Weiterleitung auf `EPOS.Kern/Controller/ProjektKontextCtrl`** (Anwenderentscheid W16b‑O‑3, 04.09.2026): dieselbe Klasse wie unter Windows, dieselbe Antwort. Bis dahin führte sie das Projekt selbst — und las die Klimazone als einzige aus dem STAMM (`Tab_Klimaregion_STAMM.Name`), während Windows die Projektkopie nahm (Befund W16b‑B2). **Die Messung zum Entscheid hat gezeigt, dass das ein Fehler dieser Hülle war, kein zweiter Weg**: `Tab_Projekt.ID_Klimaregion` trägt die Id der PROJEKTKOPIE, die Abfrage hielt sie gegen den STAMM-Schlüssel — zwei getrennte Schlüsselräume, Antwort immer leer. Vereinheitlicht ist deshalb auf die Projektkopie, und die eigene Abfrage ist ersatzlos weg. **iOS-eigen bleibt allein das `try/catch` um `Uebernehmen`** — der Kern lässt eine Ausnahme aus dem Datenzugriff durch, und hier liegt die Datenbank in der Sandbox und wird beim Erststart erst kopiert |

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
> **Regel:** Der iOS-Job läuft nur auf Abruf (*Actions → iOS → Run workflow*) und nur nach
> Rückfrage beim Anwender — der macOS-Läufer zählt zehnfach auf das Kontingent.

dotnet workload install maui-ios --version 10.0.400.1 --skip-sign-check
dotnet build EPOS.iOS/EPOS.iOS.csproj -c Release -f net10.0-ios -r iossimulator-arm64 \
  -p:SeedDb=../Referenzlaeufe/Kenndaten_Test.sqlite
```

Was sich **ohne** Mac prüfen lässt, steht in
[`../Umsetzung_iU10_Nachweise.md`](../Umsetzung_iU10_Nachweise.md): eine Restore-Probe mit
`net10.0`-Stub gegen die echte `Directory.Packages.props`, ein Übersetzungslauf der
plattformfreien Dateien, ein Übersetzungslauf der **ganzen** Hülle gegen Attrappen der
Plattform-API und ein Prüfstand der Datenseite gegen `Referenzlaeufe/Kenndaten_Test.sqlite`.

**Der Prüfmodus.** Mit gesetzter Umgebungsvariablen `EPOS_PRUEFLAUF` rechnet die App beim Start
das Referenzprojekt **1030** und legt CSV, `protokoll.txt` und zuletzt `fertig.txt` unter
`Documents/pruefung/` ab. Der Simulator reicht Variablen mit dem Präfix `SIMCTL_CHILD_` durch:

```bash
SIMCTL_CHILD_EPOS_PRUEFLAUF=1 xcrun simctl launch --console-pty <udid> de.inekon.eposplan
```

Die Kultur wird dabei fest auf **de-DE** gestellt — wortgleich zu
`EPOS.Referenzlauf.Program.KulturSetzen`. Ohne das mäße der Vergleich Kulturdrift statt
Plattformdrift.
