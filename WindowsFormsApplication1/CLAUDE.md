# CLAUDE.md — `WindowsFormsApplication1`, die Windows-Schale

Dieses Projekt ist **nur die Schale**: Assembly, EXE und Prozess `EPOS_Plan`,
`net10.0-windows`, Plattform **x64**, Projekt-SDK `Microsoft.NET.Sdk.Razor`,
`UseWindowsForms`. Sie trägt eine `BlazorWebView` mit der Razor-Oberfläche aus
[`../EPOS.UI/`](../EPOS.UI/CLAUDE.md), belegt die Windows-Fassungen der Kern-Schnittstellen
und liefert, was nur Windows beantworten kann. **Fachmasken gibt es hier keine mehr** — die
einzigen `Form`-Ableitungen sind `Allgemein/BaseForm`, `Allgemein/Blazor/BlazorDialogForm`,
`Views/Hauptformular/Hauptfensterrahmen` und `Views/Help/Form_HelpPopup`.

Aufbau des Repositoriums, Datenhaltung, Regressionsnetz, CI, Git und Dokumentationsregeln
stehen in der [`CLAUDE.md` der Wurzel](../CLAUDE.md) — hier nicht wiederholen. Index aller
Papiere: [`../Dokumentation/LIESMICH.md`](../Dokumentation/LIESMICH.md). Bezeichner,
Kommentare und Antworten deutsch.


## Bauen

```powershell
dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64
```

- **Razor-SDK statt `Microsoft.NET.Sdk`.** Das macht keine Webanwendung daraus — sie bleibt
  `WinExe` und WinForms. Er holt die statischen Web-Anteile von `EPOS.UI` herüber: mit dem
  einfachen SDK übersetzt alles fehlerfrei, der Veröffentlichungsordner enthält aber **kein
  `wwwroot`** (kein `index.html`, `_content`, `_framework/blazor.webview.js`), und jeder
  Blazor-Dialog bliebe beim Anwender leer.
- **x64**, kein AnyCPU: native 64-Bit-Anteile (OR-Tools über `SpeicherPlanung`,
  ONNX-Laufzeit, SkiaSharp).
- Ergebnis: `bin\x64\Debug\net10.0-windows\EPOS_Plan.exe`. Laufende Anwendung oder offenes
  Visual Studio sperren den Ordner (Regel in der Wurzel-`CLAUDE.md`) — Verifikations-Builds
  dann mit `-p:OutDir=<Ordner außerhalb>` umleiten, der Compile-Beweis bleibt vollwertig.
- Ausweichweg, falls doch einmal das MSBuild von Visual Studio gebraucht wird:

```powershell
$msb = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -prerelease -products * -requires Microsoft.Component.MSBuild `
        -find 'MSBuild\**\Bin\MSBuild.exe' | Where-Object { $_ -notmatch '\\amd64\\' } | Select-Object -First 1
& $msb ..\WP-Plan.sln -p:Configuration=Debug -p:Platform=x64
```

- `..\.editorconfig` stuft **WFO1000** (in .NET 10 ein *Fehler*) auf `warning` herab; die
  Zeile darf erst fallen, wenn keine WinForms-Maske mehr steht.
- **Den DPI-Modus setzt allein `app.manifest`** (`dpiAware=true/pm`,
  `dpiAwareness=PerMonitorV2`) — nicht zusätzlich `ApplicationHighDpiMode` im csproj, das
  gibt WFAC010. `Program.Main` hält den verwalteten Zustand nur deckungsgleich.
- Auslieferung: [`../Setup/`](../Setup/) mit
  [`Konzept_Setup_InnoSetup_EPOS-Plan.md`](../Dokumentation/aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md).


## Aufbau der Schale

- **`Program.cs`** — `Main` in dieser Reihenfolge: die neun `Dienste.*`-Schnittstellen des
  Kerns mit ihren Windows-Fassungen belegen (**vor** allem, was eine Meldung absetzen könnte,
  sonst ginge die erste Meldung eines Startfehlers auf die Konsole), Fabrik des
  Flottenplaners registrieren (`SpeicherFlottenProjektCtrl.PlanerFactory`), DPI-Modus,
  Sprache, **WebView2-Laufzeit prüfen**, Datenbank bereitstellen (`Erstbereitstellung` im
  Kern), Lizenzzustimmung, Windows-Haken des KI-Assistenten, Schemamigration,
  `Application.Run(rahmen)`. Die verbliebenen `Program`-Statics sind reine Weiterleitungen —
  **neuer Code nimmt den Dienst, nicht `Program`**.
- **`Dienste/`** — die Windows-Fassungen der Kern-Schnittstellen aus
  `../EPOS.Kern/Allgemein/Dienste/`: `WindowsDialogDienst`, `WindowsDateiDienst`,
  `WindowsPfade`, `RegistryEinstellungen`, `SettingsEinstellungen`, `DpapiLizenzAblage`,
  `WindowsGeraeteId`, `WindowsSprache`, `WinFormsNavigation` (zugleich die EINE Tabelle
  Maskenschlüssel → Hülle). **Hier — und nur hier — kennt die Anwendung** `MessageBox`, die
  Dateidialoge, `Process.Start`, `Registry`, `ProtectedData` und `SpecialFolder`; **neue
  plattformabhängige Aufrufe gehören hierher**, nicht nach `Allgemein/` oder `Controller/`
  (dort läuft der Wächter aus [`../EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md)). Zwei
  Ausnahmen, weil sie die Oberfläche selbst sind: `Allgemein/Blazor/` und
  `Allgemein/Hilfe/WindowsHilfeDienst.cs`.
- **`Allgemein/Blazor/`** — die **einzige** Stelle, an der WinForms und Blazor aufeinander
  treffen: `BlazorDialogForm<T>` (modales `Form` mit `BlazorWebView`, Ergebnis als
  `DialogResult`), `BlazorSeite<T> : UserControl` (nicht-modal, bleibt im Fenster),
  `BlazorDienste` (das **DI-Verzeichnis der WebView** — nicht der statische Halter `Dienste`
  des Kerns), `Blazorsprung`, `Blazornachlauf`, `WebViewWache`, `Parametersatzwache`,
  `NamensDialogHuelle`. Beide Hüllenformen tragen **denselben `UserDataFolder`**, also einen
  gemeinsamen Browserprozess. **Eine WebView je Fenster**; umgeschaltet wird in der
  Komponente.
- **`Views/`** — nur noch **dünne Adapter**: `*Huelle.cs` baut den Parametersatz einer
  Razor-Komponente, zeigt sie in einer `BlazorDialogForm`/`BlazorSeite` und wertet das
  Ergebnis aus; `*Gaben.cs` liefert nur den Parametersatz, wenn die Komponente als
  `Ueberlagerung` im selben Fenster erscheint. Dazu `Hauptformular/Hauptfensterrahmen` — das
  Fenster, das `Application.Run` trägt: Geometrie und Titel, die EINE `BlazorWebView`,
  Besitzer jeder `BlazorDialogForm`, `ProcessCmdKey` samt F1, Sprachwechsel über
  `Application.Restart` —, die Verteiler `HauptfensterHuelle`/`StartseiteHuelle` und
  `Help/Form_HelpPopup`.
- **`Controller/`** — `MenueCtrl` (die zusammengesetzten Abläufe des Menüs). Die
  Menüpunkte selbst sind **Daten**: `../EPOS.UI/Bausteine/Menuetabelle.cs`.
- **`Allgemein/` sonst** — `Update/` (`SchemaMigration` samt dem eingefrorenen Zweig für
  Altbestände); `Hilfe/` (`WikiHelpCatalog` lädt die Rubrik „Programm Dokumentation" von
  `wiki.epos-plan.de`, Basis-URL aus dem Einstellwert `WordPressUrl`; dazu `HilfeAutomatik`,
  `InfoKnopf`, `WindowsHilfeDienst`. `help_mapping.txt` und `help_cache.json` sind
  **eingebettet** und bewusst nicht im Ausgabeordner — eine gleichnamige Datei neben der EXE
  übersteuert sie); `KI/` (nur was an lebenden
  `Control`/`Form` hängt: `HilfeKontext`, `KiAusfuehrungWindows` — Wissen und Aktionen des
  Assistenten liegen im Kern); `Bericht/Vorlagen/` mit der `.docx`-Rahmenvorlage;
  `GrafikTools/`; `BaseForm`, `FensterEinpassung`, `Programmsymbol`.
- **Schemapflege:** `SchemaMigration` liegt hier, ein neuer nummerierter Schritt entsteht
  hier; die Zielnummer steht als `SchemaStand.Zielversion` im Kern
  ([`ADR-001`](../Dokumentation/aktuell/ADR-001_Schema-Ausrollung.md)). Sie läuft einmal je
  Start vor der Oberfläche; bei Fehlschlag eine Meldung, das Programm startet trotzdem, der
  Simulationsbereich bleibt gesperrt.

**Was hier NICHT hingehört:** Fachlogik, SQL und Modelle (sie liegen im
[`../EPOS.Kern/`](../EPOS.Kern/CLAUDE.md) — eine Fachänderung wird EINMAL gemacht), Dialoge
und Seiten (`../EPOS.UI/`), deren Datenseite (`../EPOS.UI.Daten/`). **Jeder neue und jeder
ohnehin anzufassende Dialog ist eine Razor-Komponente; hier entsteht nur die Hülle.**


## Regeln der Hüllenschicht

Wer eine weitere Hüllenform baut, hält sie alle ein.

- **(a) Beide Wachen bedienen.** `WebViewWache` hängt an
  `CoreWebView2InitializationCompleted`/`BlazorWebViewInitialized` und legt eine Frist
  darüber: Der `BlazorWebView` führt kein `UnhandledException`-Ereignis, eine gescheiterte
  Initialisierung bliebe sonst still und zeigte nur eine leere Fläche. `Parametersatzwache`
  hält jeden Schlüssel des Wörterbuchs gegen die `[Parameter]` der Komponente — sonst bricht
  ein unbekannter Schlüssel erst beim ERSTEN Zeichnen; Komponenten in einer `BlazorSeite<T>`
  führen zusätzlich `[Parameter] SeitenZustand? Zustand`. Gegenwache auf Linux:
  `EPOS.UI.Tests/ParametersatzTests`.
- **(b) Kein `ShowDialog` und kein modales Systemfenster unmittelbar aus einem
  Blazor-Ereignis.** Kachelklick, Menüpunkt und Komponentenrückruf kommen aus dem
  `WebMessageReceived`-Rückruf der ersten WebView2; ein zweites modales Fenster — mit eigener
  WebView2 ebenso wie `OpenFileDialog` oder `MessageBox` — pumpt dort eine verschachtelte
  Nachrichtenschleife, während Blazor zeichnet. `Blazorsprung.Verzoegert` lässt das Ereignis
  zu Ende laufen, `Blazornachlauf.Nachgelagert` dasselbe mit Rückgabewert. **Wer aus einer
  Razor-Komponente wählt oder meldet, ruft die `…Async`-Form der Dienste und `await`et sie**;
  **nie blockierend warten** (`.Result`, `.Wait()`) — der Nachlauf braucht genau den Faden,
  den man anhält.
- **(c) Der Riegel von `Blazorsprung` fällt VOR dem Sprung, nicht danach** — sonst trifft ein
  Verteiler, der aus einem verzögerten Sprung einen zweiten anstößt, den Riegel der ersten
  Stufe und kehrt stumm zurück. Ein verwaister Riegel verfällt nach kurzer Frist.
- **(d) Jede Wurzel steht in der Fehlerschranke:** die Hüllen mounten
  `EPOS.UI.Bausteine.Wurzel<T>` statt `T`. Eine `ErrorBoundary` fängt nur Nachfahren, und
  ohne sie beendet eine Ausnahme aus Ereignis oder Lebenszyklus den **Prozess**.
- **(e) Tastatur:** Eine Taste des Hauptfensters fängt **`ProcessCmdKey`, nicht
  `KeyPreview`** (das wirkt nur im `WndProc` eines WinForms-Steuerelements, und der
  Tastaturzeiger sitzt praktisch immer in der WebView2). Ein nicht-modales Fenster braucht
  die Eingabe ausdrücklich — `BlazorDialogForm.TastaturUebergeben()`, zweimal gerufen: sofort
  und nach `CoreWebView2InitializationCompleted`.
- **(f) Ein Fachdialog öffnet im Anteil des Arbeitsbereichs.** Die Rechnung steht
  plattformfrei in `EPOS.UI/Dienste/Fenstermass.cs`, die Hülle besorgt nur den
  Arbeitsbereich; unter „Per Monitor V2" stehen `Screen.WorkingArea` und `Form.ClientSize` im
  selben Raum, ohne Skalierungsfaktor. `Dialogart.Fachdialog` ist die Vorgabe,
  `Dialogart.Klein` wächst nicht mit, `Dialogart.Inhaltsmass` (die Projektdialoge,
  `Fenstermass.Projektdialog`) wünscht in CSS-Pixeln und wächst nur mit der Skalierung; die Verwaltungshüllen reichen das Wunschmaß ihrer
  Import-Überlagerung an `ModulKatalogHuelle.Oeffnen` herein.
- **(g) Nebenläufigkeit:** **Der Bedienfaden liest die Datenbank**, der Hintergrund rechnet
  (`Task.Run`), das Marshalling besorgt ein auf dem Bedienfaden erzeugtes `Progress<T>`;
  Meldungen aus paralleler Rechnung werden **gedrosselt**. Nach jedem `await` steht in
  **jedem** Zweig ein `Entsorgt()`-Test, sonst landet der Zugriff auf die zugemachte Maske
  als `ObjectDisposedException` in einer unbehandelten `async void`-Fortsetzung.
  `DataRepository.EngineModus` ist prozessweit: zwei gleichzeitige Läufe sind ausgeschlossen.


## Konventionen

- Root-Namensraum `WindowsFormsApplication1` für alles, trotz Domänen-Ordnern. Suffixe:
  `*Huelle`, `*Gaben`, `*Ctrl`.
- **Designer- und `.resx`-Dateien nicht von Hand editieren.**
- **Drei-Schichten-Regel für Texte** (Konzept 13.6): **Persistenz** — alles, was in der
  Datenbank steht oder in SQL damit verglichen wird, bleibt **deutsch und eingefroren**; die
  Werte stehen zentral in `EPOS.Kern/Allgemein/DbWerte.cs`, nie als Literal im Code.
  **Schlüssel** — Diagrammserien, Steuerwerte, Filter-Tokens: sprachneutral und ASCII
  (`PUFFER_12`, `WAERMEBEDARF`). **Anzeige** — ausschließlich über `MyResource.Resource.*` in
  beiden Sprachen (Fundstellen:
  [`Lokalisierung_Katalog.md`](../Dokumentation/aktuell/Lokalisierung_Katalog.md)).
  **Kein Anzeigetext darf Steuerwert sein.** Prüfrezeptur:
  [`Lokalisierung_Pruefung.md`](../Dokumentation/aktuell/Lokalisierung_Pruefung.md).
- **Programmsymbol — eine Quelle:** das mehrstufige ICO `Resources/EPOS-Plan.ico`. Das csproj
  setzt `<ApplicationIcon>`, `Setup/EPOS-Plan.iss` nimmt für `SetupIconFile` **dieselbe**
  Datei. Das färbt nur die EXE — jedes `Form` braucht sein eigenes `Form.Icon`, deshalb ruft
  **jede neue `Form`-Unterklasse `Programmsymbol.Anwenden(this)` im Konstruktor.** Wächter:
  `EPOS.Kern.Tests/ProgrammsymbolWacheTests`.


## Wichtige Pakete

Nur, was die Schale selbst braucht; alles Übrige kommt über `EPOS.Kern`.

- `Microsoft.AspNetCore.Components.WebView.WindowsForms` — die `BlazorWebView`; zieht
  `Microsoft.Web.WebView2` mit (SDK und Loader), **nicht** die Laufzeit.
- `SkiaSharp` samt `…Views.WindowsForms`, `…NativeAssets.Win32`,
  `HarfBuzzSharp.NativeAssets.Win32` — die Win32-Nativteile für die Bilder des Renderers.
- `System.Security.Cryptography.ProtectedData` — DPAPI hinter `Dienste.Lizenzablage`.
- `System.Configuration.ConfigurationManager` — `Properties.Settings` hinter
  `SettingsEinstellungen`.
- `Microsoft.Data.Sqlite` — die Fassung steht zentral in `Directory.Packages.props`.
- `Google.OrTools` hängt am Projekt `SpeicherPlanung`, das nur von hier referenziert wird;
  ohne die registrierte Fabrik sind `PvPlanung`, `Arbitrage`, `MultiUse` nicht verfügbar.
- `SixLabors.Fonts` bewusst auf 1.0.x gepinnt — ab 2.x gilt die Six Labors Split License.


## Fallstricke

- **WebView2 ist eine harte Laufzeitvoraussetzung.** `dotnet publish` bringt nur das SDK; die
  Evergreen-Laufzeit installiert das Setup nach. Schon zwei Startschritte laufen über eine
  Blazor-Hülle, deshalb prüft `Program.Main` die Laufzeit
  (`CoreWebView2Environment.GetAvailableBrowserVersionString()` in `try/catch`) und beendet
  bei Fehlschlag mit einer nativen zweisprachigen `MessageBox` samt Bezugsquelle. **Keine
  WinForms-Rückfallmasken.** Der Profilordner liegt ausdrücklich unter
  `%LOCALAPPDATA%\WP-Plan\WebView2` — „neben der EXE" ist unter `C:\Program Files` für
  Standardbenutzer nicht beschreibbar.
- **DPI:** „Per Monitor V2" — Abnahme am Gerät bei 100 / 125 / 150 %, besonders
  `Form_HelpPopup`.
- **Kodierung:** Die Quelltexte hier sind UTF-8; ein Teil trägt eine BOM, ein Teil nicht —
  beim Bearbeiten den Zustand der Datei **erhalten**. Die `.editorconfig` verlangt für neue
  `.cs` UTF-8 **mit** BOM, für Markdown UTF-8 **ohne** BOM und CRLF.
- **Wegwerf-Harnesse nur unter `..\dev\`** (Repo-Wurzel, gitignored). Die `.csproj` sammelt
  `**\*.cs` ein — eine `.cs`-Datei unterhalb dieses Projekts bricht den Build sofort (CS0017,
  zweites `Main`). Ein Harness dort erbt die `Directory.*.props` der Wurzel; deshalb
  `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` setzen, sonst
  scheitert der Restore (NU1008).
- **Kultur:** Die Anzeigekultur setzt `WindowsSprache` beim Start; ein Sprachwechsel läuft
  über `Application.Restart`, weil Masken ihre Texte beim Aufbau lesen.
- **Visual Studio regeneriert `../EPOS.Kern/MyResource/Resource.Designer.cs` selbst**, sobald
  es eine `.resx`-Änderung bemerkt; wer parallel von Hand ergänzt hat, baut Duplikate
  (CS0102) — dann die Hand-Einfügung entfernen.

---

Die ausführliche Fassung mit Herleitungen und der Geschichte der Maskenumstellung:
[`WindowsFormsApplication1_CLAUDE_2026-09-12.md`](../Dokumentation/ueberholt/Protokolle/CLAUDE-Historie/WindowsFormsApplication1_CLAUDE_2026-09-12.md).
