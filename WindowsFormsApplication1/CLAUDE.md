# CLAUDE.md — WP-Plan / EPOS-Plan, Anwendungsprojekt

Kontext zum **Code** dieses Projekts. Fachdomäne, Datenmodell, Migration und Umgang mit
`Kenndaten.accdb` stehen in der [`CLAUDE.md` der Repo-Wurzel](../CLAUDE.md) — hier nicht wiederholen.
Antworten und Code-Kommentare auf Deutsch.

> **Der Rechenkern steht nicht mehr hier.** Seit Paket iU4 (03.09.2026) liegen die Kerndateien
> im Projekt [`../EPOS.Kern/`](../EPOS.Kern/CLAUDE.md) — erst 168, nach dem zweiten Umzug
> (iU5-U1…U5), `ChartRenderer` (iU7-5) und `KostenfaktorCtrl` (iU9-W1.5) **269 `.cs`**:
> `Allgemein/Simulation/`,
> `Allgemein/Wirtschaftlichkeit/`, `Model/`, die Zugriffsschicht, der **ganze** Bericht
> (Daten, Ausgabe und Renderer), `Lizenz/`, `Import/`, `Katalog/`, `Export/`, das KI-**Wissen**
> und 81 Controller. Dieses Projekt referenziert es und übersetzt sie nicht mehr mit.
> **Regel: eine Fachänderung am Rechenkern wird EINMAL gemacht, im Kern.** Wer eine Datei hier
> sucht und nicht findet, sucht sie dort — die Ordnerstruktur ist dieselbe geblieben.

## Build

`net10.0-windows`, WinForms, `WinExe`. Namespace `WindowsFormsApplication1`;
**Assembly/EXE/Prozess `EPOS_Plan`** (Umbenennung Stufe 0 am 29.08.2026 — nur der
Ausgabename; Stufe 1 = Namespace-Umstellung bräuchte ein eigenes Konzept).
Solution: `..\WP-Plan.sln` (Debug/Release × x64).

**Das Projekt-SDK ist seit iU8 `Microsoft.NET.Sdk.Razor`**, nicht mehr
`Microsoft.NET.Sdk`. Das macht aus der Anwendung keine Webanwendung — sie bleibt `WinExe`
und WinForms. Gebraucht wird es, damit die statischen Web-Anteile von
[`../EPOS.UI/`](../EPOS.UI/CLAUDE.md) überhaupt hier ankommen: Mit dem einfachen SDK
übersetzt alles fehlerfrei, der Veröffentlichungsordner enthält aber **kein `wwwroot`** —
weder `index.html` noch `_content` noch `_framework/blazor.webview.js`, und jeder
Blazor-Dialog bliebe beim Anwender leer. Der Bau ändert sich dadurch nicht; der Razor-SDK
steckt im .NET-SDK, ein Workload ist nicht nötig.

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
nach iU9-W5 noch **14 Fundstellen** (16 nach iU9-W4, 20 nach iU9-W3, 22 nach iU9-W2,
24 nach iU9-W1, 30 davor; die Warnzahl der ganzen Mappe steht bei **20**: 14 WFO1000,
2 CS0108, 2 CS0109, 1 WFO0003, 1 CA2255), Schwerpunkt
`Form_Gesetzesparameter` (5) und die Karten-Controls des Kosten- und
Simulationsmoduls (`SpeicherKarte`, `ErzeugerKarte`; `EinstiegsKarte` ist mit iU9-W4
gelöscht). Die
Annotation je Property ist eine Fachentscheidung und gehört zu Paket iU9 — nicht nebenbei
miterledigen; **mit jeder umgestellten Maske fällt die Zahl von selbst.**

Das Setup (`..\Setup\build-setup.ps1`) wird derzeit von VS-MSBuild auf `dotnet publish`
umgestellt (Paket iU1-P1.10, erledigt 02.09.2026, `ce2dc9e`).


## Architektur

Grob MVC, verschaltet über prozessweite Statics in `Program`:

- **`Program.cs`** — `Main` startet die MDI-Oberfläche. Hält `mdifrm`, `mainfrm`, `startfrm`,
  `menuectrl`, `wizardctrl`. Globaler veränderlicher Zustand — Seiteneffekte bei Änderungen
  mitdenken. **Weiterleitungen** (kein eigener Zustand mehr): `nLanguage`, `ZahlParsen` und
  `GanzzahlParsen` auf `Sprache` bzw. `ZahlText` (iU4-1); `ApplicationPath_Common/_User` auf
  `Dienste.Pfade`, `HelpCatalog` auf `WikiHelpCatalog.Aktueller`, `WIKI_STANDARD` auf
  `WikiWissen.WIKI_STANDARD` (iU5). `Main` legt als **erstes** die neun Umgebungsdienste ein
  (siehe `Dienste/`) und setzt danach den Haken
  `WErzeugerCtrl.GeraetewaisenAufraeumen`. Die vier `Meldung.*`-Haken werden **nicht mehr**
  belegt — sie zeigen seit iU5 selbst auf `Dienste.Dialog`.

- **`Dienste/`** (**10** Dateien, iU5) — die **Windows-Fassungen** der neun Umgebungsdienste aus
  `../EPOS.Kern/Allgemein/Dienste/`. Hier — und nur hier — kennt die Anwendung `MessageBox`,
  `OpenFileDialog`/`SaveFileDialog`/`FolderBrowserDialog`, `Process.Start`, `Registry`,
  `ProtectedData`, `Application.ProductName` und die Zuordnung von Masken- und
  Gewerksschlüsseln zu Formularklassen:
  `WindowsDialogDienst`, `WindowsDateiDienst`, `WindowsPfade`, `RegistryEinstellungen`,
  `SettingsEinstellungen` (Brücke zu `Properties.Settings`), `DpapiLizenzAblage`,
  `WindowsGeraeteId`, `WindowsSprache`, `WinFormsNavigation`, `FormStartProjektKontext`.
  **Neue plattformabhängige Aufrufe gehören hierher, nicht in `Allgemein/` oder `Controller/`** —
  dort läuft der Wächter aus `../EPOS.Kern/CLAUDE.md`.
  Zwei benannte Ausnahmen seit iU8, beide sind ihrem Wesen nach Windows-Hülle und tauchen
  deshalb im Wächter W2 auf: `Allgemein/Blazor/` (`ShowDialog` — die Hülle *ist* das modale
  Fenster) und `Allgemein/Hilfe/WindowsHilfeDienst.cs` (`new Form_HelpPopup` — die
  Windows-Fassung von `EPOS.UI.Dienste.IHilfeDienst`, die absichtlich neben dem Hilfekatalog
  liegt, den sie bedient). Sie stehen nicht unter `Dienste/`, weil sie keine Kern-Schnittstelle
  bedienen, sondern die Oberfläche selbst.
- **`Controller/`** (**20** Dateien, `*Ctrl.cs`) — was Oberfläche braucht: die zwölf
  `*KontextMenuCtrl`, `MenueCtrl`, `WizardCtrl`, `KlimaregionStammCtrl`,
  `EnergietraegerKatalogCtrl`, `PeakShavingCtrl`, `ProjektExportImportCtrl` und `WPCtrl`
  (+ `.WinForms.cs`). Die übrigen **82** liegen in `../EPOS.Kern/Controller/` — 50 seit iU4,
  29 seit iU5-U4, einer seit iU8-8b, einer seit iU9-W1.5 (`KostenfaktorCtrl`), einer seit
  iU9-W0.1 (`KostenSummenCtrl`).
- **`Model/`** — **keine `.cs` mehr**; alle 47 Modelle liegen in `../EPOS.Kern/Model/`
  (`EnergietraegerModel.cs` mit `EnergyCarrier`/`EnergyConversion` seit iU9-W0.1).
- **`Views/`** (**229 `.cs`**, davon 140 ohne Designer-Datei; **419 Dateien** mit `.resx`) —
  `Form_*` in Domänen-Unterordnern (BHKW, Photovoltaik, Wärmepumpe, Simulation, Wizard,
  Bericht, Wirtschaftlichkeit, Varianten, Admin, Help …). **Mit iU9-W1 sind sieben Masken
  verschwunden** (Kostenvorlagen-Kleindialoge und der Kapitalwert-Verlauf); an ihrer Stelle
  stehen drei Hüllen — `Views/Kosten/VorlagenUebernahmeHuelle.cs`,
  `Views/Kosten/KostenfaktorKatalogHuelle.cs`,
  `Views/Wirtschaftlichkeit/KapitalwertVerlaufHuelle.cs`.
  **Mit iU9-W0 sind neun weitere stillgelegt** (Anwenderentscheid iF29, 25 Dateien):
  `Form_Kosten` samt `Form_KostenfaktorItem`, `ucKostenItem` und `Form_Betriebskosten` —
  **Nachfolger sind `Views/BerichteKosten/UcBkKosten.cs` und
  `Views/Kosten/Form_KostenKomponente.cs`**, die von außen genutzte Leselogik steht als
  `EPOS.Kern/Controller/KostenSummenCtrl.cs` im Kern —, dazu `Form_Variantentest` mit den zwei
  K4-Hüllen `Form_Wirtschaftlichkeit`/`Form_Bericht`, `Form_Simulation_Kurz` (mit
  `Form_Simulation_Detail - Kopie.cs` und `Allgemein/GrafikTools/ChartManagerNeu.cs`) und
  `Form_KwkgModule`. Damit ist auch die `Compile Remove`-Liste der `.csproj` entfallen: Was
  nicht übersetzt werden soll, liegt nicht mehr im Baum.
  **Mit iU9-W2 sind sechs weitere Masken verschwunden** — die drei letzten zeichengleichen
  Namensabfragen (`Form_StromspeicherItemNeu` mit 28 Aufrufern, `Form_GebaeudetypNeu`,
  `Form_AlsVariante`) laufen über die eine Razor-Komponente `NamensDialog`, die drei
  Wirtschaftlichkeitsmasken (`Form_Tarifstruktur`, `Form_PhotovoltaikVerguetung`,
  `Form_WirtschaftlichkeitParameter`) über eigene Komponenten. An ihrer Stelle stehen vier
  Hüllen — `Views/Varianten/AlsVarianteHuelle.cs` (der Variantenablauf),
  `Views/Wirtschaftlichkeit/TarifstrukturHuelle.cs`,
  `Views/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs`,
  `Views/Wirtschaftlichkeit/WirtschaftlichkeitParameterHuelle.cs`.
  **Mit iU9-W3 sind vier weitere Masken verschwunden** — die vier Kleindialoge am
  Energieträger: `Form_LeistungspreisReihe` (zwölf Monatssätze), `Form_SpotpreisImport`
  (Dateiwahl, Prüfprotokoll, 8 760 Werte), `Form_Emissionskatalog` (767 Z., zwei Raster und
  zwei zur Laufzeit gebaute Unterdialoge) und `Form_Kostenprofil` (36 Laufzeitfelder und ein
  Chart). An ihrer Stelle stehen vier Hüllen —
  `Views/Kosten/LeistungspreisReiheHuelle.cs`, `Views/Kosten/SpotpreisImportHuelle.cs`,
  `Views/Kosten/EmissionskatalogHuelle.cs`, `Views/Kosten/KostenprofilHuelle.cs`.
  Keine der vier brachte einen neuen Controller mit: Alle riefen schon vorher ausschließlich
  Kern-Controller (Hausmuster Ä9).
  **Mit iU9‑W4 sind sieben weitere Masken verschwunden** — die beiden **Hosts** der
  Kostenseite samt ihren fünf Unterbausteinen: `Form_KostenKomponente` (918 Z.) mit
  `ucVorlagenZeile` und `ucErtragBonus`, `Form_Energietraeger` (535 Z.) mit
  `ucFuelSettings` (2 103 Z.), `ucStromAufschlaege` und `ucBrennstoffBestandteile`.
  An ihrer Stelle stehen drei Dateien —
  `Views/Kosten/KostenKomponenteHuelle.cs`, `Views/Kosten/ErtragBonusGaben.cs`,
  `Views/Kosten/EnergietraegerHuelle.cs`. Die neun SQL-Anweisungen von `ucFuelSettings`
  stehen seither als `EPOS.Kern/Controller/EnergietraegerPreisCtrl.cs` im Kern.
  Ohne Nutzer geblieben und mitgelöscht: `Views/Kosten/EinstiegsKarte.cs` (Nachfolge
  `Kachel`) und `Views/Kosten/SectionPanel.cs` (Nachfolge `Gruppenkopf`) — **`Views/Kosten`
  führt seither keine Designer-Maske mehr.** Sechs Hüllen der Wellen 1 bis 3 liefern statt
  eines Fensters ihren Parametersatz (`Gaben`): Die neun Unterdialoge der Kostenseite
  erscheinen jetzt in einer `Ueberlagerung` desselben Fensters (Risiko R2).
  **Mit iU9‑W5 ist der ganze Reiter „Berichte &amp; Kosten" Blazor** — sechs Masken,
  5 192 Zeilen: `Form_BkUebernahme` (180 Z.), `UcBericht` (508 Z.),
  `UcWirtschaftlichkeit` (831 Z.), `UcBkKosten` (1 311 Z., K4),
  `UcBkUebersicht` (1 552 Z., K4) und ihr Behälter `UcBerichteKosten` (810 Z., K4).
  An ihrer Stelle stehen **fünf Datenseiten** —
  `Views/BerichteKosten/BerichteKostenHuelle.cs` (der geteilte Zustand),
  `Views/BerichteKosten/UebersichtSeiteGaben.cs`,
  `Views/BerichteKosten/KostenSeiteGaben.cs`,
  `Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs`,
  `Views/Bericht/BerichtSeiteGaben.cs` — und in `Form_Start.tabPage6` **eine
  `BlazorSeite<BerichteKostenSeite>`**: eine WebView für alle vier Seiten (Risiko R5).
  Kein neuer Kern-Controller: Alle vier riefen schon vorher ausschließlich
  Kern-Controller (Hausmuster Ä9).
  Der **Stapellauf der Formularkarte zählt seither 88 Masken** (91 nach iU9‑W4, 98 nach
  iU9‑W3, 102 nach iU9-W2, 105 nach iU9-W0, 111 nach iU9-W1, 118 davor), und die
  Erreichbarkeit steht auf **0 × „nein", 0 × „verwaist"**; jede weitere Welle senkt die
  Zahl.
- **`Allgemein/`** (**43** Dateien) — geteilte Infrastruktur, siehe unten. Seit iU5 frei von
  `Program.*`, `MessageBox`, Registry, DPAPI und `SpecialFolder`; die Ausnahmen
  (`Update/ErststartMigration.cs`, `Update/SchemaMigration.cs`, der Oberflächenbaustein
  `HelpExtender` in `Hilfe/HelpCatalog.cs`) sind im iU5-Statusblock des Umsetzungskonzepts
  begründet. `Simulation/` (bis auf `SchemaModell.cs`), `Wirtschaftlichkeit/`, `Bericht/`
  (Daten **und** Ausgabe), `Lizenz/`, `Import/`, `Katalog/`, `Export/` und das KI-Wissen sind
  in den Kern gezogen. Hier bleiben: `Blazor/` (die Hülle, iU8), `Update/` (Schemamigration und
  Access-Zweig samt `DbParamOleDb`, `SchemaVersionAccess`, `GeraeteWaisen`), `GrafikTools/`,
  `Hilfe/`, die WinForms-nahen Teile von `KI/` (was der Assistent **bedient**),
  `Bericht/BerichtsDatenSammler.cs` (`ChartRendererGdi.cs` ist mit iF23 gelöscht), dazu `BaseForm`,
  `Form_Hinweis`, `FensterEinpassung`, `SpeichernLeiste`, `IAssistentRahmen`, `StromTestClass`
  und `Simulation/SchemaModell.cs`. Die vollständige Begründung je Datei steht in
  [`../EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md).

Die Aufteilung im Einzelnen — was im Kern liegt und was mit Absicht hier geblieben ist — steht im
Kopfkommentar von [`../EPOS.Kern/EPOS.Kern.csproj`](../EPOS.Kern/EPOS.Kern.csproj) und in
[`../EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md).

## Module in `Allgemein/`

| Ordner | Inhalt |
|---|---|
| `Bericht/` | **Nur noch eine Datei.** `BerichtsDatenSammler`, weil er `EnergieMengen` aus `Views/Varianten/` ruft. Alles andere liegt in `../EPOS.Kern/Allgemein/Bericht/`: die DATEN-Hälfte seit iU4, der Renderer `ChartRenderer` (SkiaSharp) seit iU7-5, die AUSGABE (`WordBerichtGenerator`, `ExcelBerichtGenerator`, `Bausteine/`, `BerichtsKonfiguration`, `ZeitreihenExtraktor`, `IBerichtsBaustein`) seit iU5-U3. Die `.docx`-Vorlage bleibt hier und wird neben die EXE kopiert | `ChartRendererGdi` (GDI+-Stand) und `Referenzlauf/Bildvergleich.cs` sind mit iF23 am 03.09.2026 gelöscht |
| `Wirtschaftlichkeit/` | **vollständig in `../EPOS.Kern/Allgemein/Wirtschaftlichkeit/`** (iU4): `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl` und 16 weitere |
| `Simulation/` | **bis auf `SchemaModell.cs` (Schema-Ansicht) in `../EPOS.Kern/Allgemein/Simulation/`** (iU4). Engine: `SimulationControl`, `Init`, `SimulationRunner` + Module je Erzeuger/Bedarf (`SimulationWaermebedarf`, `…Waermepumpe`, `…BHKW`, `…PV`, `…Solarthermie`, `…SPK`, `…SSP`, `…Pufferspeicher`). Seit der Konzeptumsetzung 27./28.08.2026 (**ein Rechenweg, dreikanalig** Heizung/Brauchwasser/Prozess): `Kaskadenschleife` (Stundenschleife Phasen A–G, Ladeaufträge je Rang), `SimulationKanaele` (`Kanal`/`Kanalsatz`/`Senkenliste`/`Ladeordnung`-Umfeld), `WaermequelleClass`/`WaermesenkeClass`, `Warnkriterien` (Katalog W1–W6 + harte Guards, eine Wahrheit für Dialog und Laufstart), `ProfilBedarf`, `SchemaModell` (Schema-Ansicht), `StilleDb`; Schichtspeichermodell (N = 1…10, SOC führend) vollständig in `SimulationPufferspeicher`; Booster-Quelltemperatur stundengekoppelt, Lesepunkt je Projekt wählbar (`Tab_Einstellungen.Booster_Lesepunkt`, Default „Davor" = Stundenanfang; Paket B2); Kessel-Temperaturbezug je Anlage `Tab_Energieanlagen.WQ_TemperaturModus` („Berechnet" = Bezugskette Senkenspeicher→Katalog→70/50, Default, ohne Pflegezwang; „Fest" = Vorgabe, Warnung nur wenn Paar fehlt). Historie und Invarianten je Paket: `*_Protokoll.md` im selben Ordner |
| ~~`Lizenz/`~~ | **seit iU5-U1 in `../EPOS.Kern/Allgemein/Lizenz/`**: `LizenzManager`, `LizenzToken`, `LizenzServerClient`, `GeraeteId` — signiertes Token, Zustände von `NichtAktiviert` bis `Lesemodus`. Die Ablage läuft über `Dienste.Lizenzablage`; **Geltungsbereich Gerät** (DPAPI `LocalMachine`) für Token und Zeitanker, **Benutzer** (`CurrentUser`) für den KI-Schlüssel — ein Wechsel entwertet jede installierte Lizenz |
| `KI/` | **geteilt seit iU5-U2:** Was der Assistent **weiß** (`HilfeWissen`, `WikiWissen`, `SemantikIndex`, `SemantikModell`, `KiEinwilligung`, `KiSchreibschutz`, `KiSicherungspunkt`, die Textkataloge) liegt im Kern; was er **bedient**, bleibt hier — `KiChatService`, `KiDialogZugriff`, `KiAusfuehrer`, `HilfeKontext`, `KiAufrufKnopf` und die `Aktionen/`. Inhaltlich unverändert: `KiChatService` (Gemini 2.5 Flash-Lite über REST), `HilfeKontext`, `HilfeWissen`, seit 29.08.2026 `WikiWissen` (Wiki-Suche + Klartext-Auszüge + 24-h-Cache `%APPDATA%\wp-plan\wiki-wissen\`, speist die „Hilfeabschnitte" des Prompts; Chatfenster ohne KI = Online-Doku-Suche; Protokoll `H4H5_Umsetzung_Protokoll.md`); API-Key als DPAPI-Datei `%APPDATA%\wp-plan\ki-schluessel.dat` (Registry-Altwert wird einmalig migriert und gelöscht) |
| ~~`Import/`~~ | **seit iU5-U1 in `../EPOS.Kern/Allgemein/Import/`**: `VDI 3805/` (Kessel, Puffer, Kollektoren, WP), `CEC/` + `Pan/` (PV-Module), `CsvReader`, `GanglinienDatei`, `AnsiEncoding`. Ebenso `Katalog/` und `Export/` |
| `GrafikTools/` | `ChartManager`, `RoundedPanel` |
| `Hilfe/` | `WikiHelpCatalog` (in `HelpCatalog.cs`) — lädt die Rubrik `Programm Dokumentation/` von `wiki.epos-plan.de` (Action-API `allpages`+`apprefix`, Basis-URL aus `Settings.WordPressUrl`, Not-Rückfall `Program.WIKI_STANDARD`); `HilfeAutomatik`, `help_mapping.txt`/`help_cache.json` (Ziele = Kurznamen der Rubrik-Unterseiten, optional `#anker`), `DokuUebersetzung` (EN über translate.goog). Umsetzung 29.08.2026, Protokoll `H1H2_Umsetzung_Protokoll.md` im selben Ordner |
| `Blazor/` | **Die Hülle für Razor-Dialoge und -Seiten (iU8 / iU9-W5).** `BlazorDialogForm<T>` — ein modales `Form` mit `BlazorWebView`, das eine Komponente aus `EPOS.UI` zeigt und ihr Ergebnis als `DialogResult` zurückgibt; `DpiInsel` (P/Invoke `SetThreadDpiAwarenessContext`); `BlazorDienste` — das Dienstverzeichnis der WebView, einmal gebaut; seit iU9-W1.2 `NamensDialogHuelle` für die fünf zeichengleichen Namensabfragen des Bestands (seit iU9-W2.1 alle fünf umgestellt: `Bezeichner`, `BezeichnerUndBeschreibung`, `FragenMitHinweis`); seit iU9-W2.2 `Sprungbruecke` — Schlüssel → `Form`, **modal aus dem Rückruf einer Razor-Komponente heraus** (nur WinForms-Ziele). Seit iU9-W4.0 gilt für Blazor-Ziele nicht mehr der nachgelagerte Sprung, sondern der Baustein `Ueberlagerung`: ein modaler Bereich IM selben Fenster, also ohne zweite WebView (Risiko R2). Die Hülle liefert dafür `Gaben()` statt `Oeffnen()`. **Seit iU9-W5.0 gibt es die zweite Hüllenform: `BlazorSeite<T> : UserControl`** — nicht-modal, für eine Seite, die in einer vorhandenen Maske sitzt und dort bleibt (`Form_Start.tabPage6`). Sie trägt dieselben `CreationProperties` wie die Dialoghülle, insbesondere denselben `UserDataFolder`: ein gemeinsamer Browserprozess für Dialoge und Seiten. **Eine WebView je Fenster** (Risiko R5) — umgeschaltet wird in der Komponente (Baustein `Reiter` bzw. die Navigation von `BerichteKostenSeite`), nicht durch eine zweite Hülle. Der Projektwechsel läuft über `EPOS.UI.Dienste.SeitenZustand`, ein Objekt mit Änderungsereignis, damit die WebView **nicht** neu gebaut wird. **DPI:** Die `DpiInsel` wirkt nur für den modalen Lauf; eine eingebettete Seite sitzt im Fenster der DpiUnaware-`Form_Start` und wird ab 125 % bitmapskaliert — `BlazorSeite` versucht es deshalb gar nicht erst und dokumentiert den Befund (offener Entscheid iF21). Die **einzige** Stelle, an der WinForms und Blazor aufeinandertreffen |
| `Reporting/`, `Waermespeicher/` | **nur Konzept-/Standdokumente**, kein Code — darunter die Portprotokolle `B5b_Blazor_Port_Protokoll.md`, `iU9_W1_Blazor_Port_Protokoll.md`, `iU9_W2_Blazor_Port_Protokoll.md`, `iU9_W3_Blazor_Port_Protokoll.md`, `iU9_W4_Blazor_Port_Protokoll.md` und `iU9_W5_Blazor_Port_Protokoll.md` (Feldkartenabgleich, Abweichungen A-n, Windows-Abnahme je Welle) |

**Datenzugriff:** `DataRepository.cs` — Standard, in ~160 Dateien; die Datei liegt seit iU4 in `../EPOS.Kern/Allgemein/`. Seit 02.09.2026 (`6486c36`)
spricht sie **SQLite** über `Microsoft.Data.Sqlite` (`Data Source=<Pfad>\Kenndaten.sqlite`,
`PRAGMA foreign_keys = ON` je Verbindung); den Verbindungsstring baut zentral `GetConnectionString()`.
Die Ausführungsmethoden nehmen seit 02.09.2026 `params DbParam[]` (`Allgemein/DbParam.cs`, eigener
Parametertyp mit `DbParamTyp`-Enum) — **nicht mehr `OleDbParameter`**, das auf Nicht-Windows schon im
Konstruktor `PlatformNotSupportedException` wirft. Übersetzt wird innen (`?` → `@pN`,
`UebersetzeParameterzeichen`). Die frühere implizite Brücke `OleDbParameter → DbParam` ist mit **iU6-T3a** gegenstandslos
geworden: Ein Skript hat die 434 Altaufrufe in 46 Views maschinell auf `DbParam` gezogen, T3b hat
die Brücke aus dem Kern genommen. **Neuer Code nutzt ausschließlich `new DbParam(…)`.**
`System.Data.OleDb` wird in diesem Projekt nur noch vom eingefrorenen **Access-Zweig** gebraucht
(`DbParamOleDb`, `SchemaVersionAccess`, `SchemaMigration`, `GeraeteWaisen`, `ErststartMigration`);
`../EPOS.Kern` nennt es **gar nicht mehr**, weder im Quelltext noch als `PackageReference`
(CA1416: 87 → 0). Transaktionen laufen über `DbVorgang`. `RecordSet.cs` (string-konkateniertes
SQL) ist Altbestand, innen ebenfalls auf SQLite und seit **iU6-T1** ein reiner vorwärtslaufender
Zeilenzeiger — `DBCommand`, `MerkeSql()` und `Parameter()` sind ersatzlos gestrichen (iR8: null
externe Nutzer). Seit **iU6-T4** ist `DataRepository` eine **Fassade** vor `IDatenzugriff`
(`SqliteDatenzugriff`); für die rund 160 Aufruferdateien ändert das nichts. Neuer Code
ausschließlich über `DataRepository`. Betrieb: `BETRIEB_SQLITE.md`.

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
- **Jeder NEUE und jeder ohnehin anzufassende Dialog entsteht in `../EPOS.UI/` als
  Razor-Komponente; diese Anwendung liefert nur noch die Hülle.** Das ist die Arbeitsregel seit
  iU8/iZ5, nicht eine Empfehlung: Ein zweites Mal dieselbe Maske zu bauen, heißt sie zweimal zu
  pflegen — und beim ersten Fachwechsel laufen die beiden Fassungen auseinander. Ein
  umgestellter Dialog wird im **selben Schritt** gelöscht (Regel M1), es gibt keinen Schalter
  und kein „bis auf Weiteres". Vorbild ist `Views/Heizkessel/Form_Heizkessel.cs`
  (`CreateNewEnergyCarrier`): Parameterwörterbuch bauen, `Geschlossen`-Rückruf auf
  `BlazorDialogForm.Schliessen` legen, `ShowDialog()` wie bisher auswerten. Die Datenbankseite
  gehört dabei in einen Controller im Kern, nicht in die Komponente
  (`EPOS.Kern/Controller/EnergietraegerVarianteCtrl.cs`), und die Texte in
  `MyResource.Resource.*`.
- **Vor jeder Maskenumstellung die Feldkarte ziehen:**
  `dotnet run --project ../Werkzeuge/Formularkarte -- <Designer.cs>`. Sie listet aus
  `InitializeComponent` (und aus der `.resx`, wenn die Maske mit `ApplyResources` lokalisiert
  ist) Name, Typ, Beschriftung beider Sprachen, Wertebereiche, Tab-Reihenfolge und die
  Ereignishandler mit Fundstelle — die Abnahmecheckliste der Umstellung. Von Hand vergisst man
  ein Feld; die Karte nicht.
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

`WinForms.DataVisualization` (Chart-Port mit Original-Namespace) · `ScottPlot.WinForms` —
seit iU7/iU8 nur noch für **interaktive** Bildschirm-Charts, heute genau eine Maske
(`Form_SpeicherOptimierung`); Bericht und Blazor bekommen PNG-Bytes aus dem Kern-Renderer ·
`SkiaSharp` · `MathNet.Numerics` · `System.Security.Cryptography.ProtectedData` (DPAPI hinter
`Dienste.Lizenzablage`) · `System.Data.OleDb` (**nur** noch für den Access-Zweig der
Erststart-Migration) · `Mscc.GenerativeAI` · seit iU8
`Microsoft.AspNetCore.Components.WebView.WindowsForms` (10.0.100) und die Projektreferenz auf
`../EPOS.UI`. `DocumentFormat.OpenXml`, `ClosedXML`, `BouncyCastle.Cryptography` und
`SixLabors.Fonts` stehen seit iU5-U auch im Kern — die konsumierenden Dateien sind dorthin
gewandert, die Referenzen hier sind noch nicht aufgeräumt.

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
  in `Program.cs`). Der `PerMonitorV2`-Kommentar im `.csproj` ist falsch. **Ausnahme seit iU8:**
  `BlazorDialogForm.ShowDialog()` stellt den Faden für die Dauer des modalen Laufs auf
  `PER_MONITOR_AWARE_V2` und danach zurück (`DpiInsel`) — sonst wäre der WebView2-Inhalt bei
  125–200 % bitmapskaliert und sichtbar unscharf. Die WinForms-Masken dahinter bleiben
  unberührt. Auf einem Windows vor 10 (1803) greift die Insel nicht; das ist ein
  Schönheitsfehler, kein Fehlschlag.
- **WebView2 ist ab iU8 eine Laufzeitvoraussetzung.** `dotnet publish` bringt nur das SDK
  (`Microsoft.Web.WebView2.Core.dll`, `WebView2Loader.dll`); die Evergreen-Laufzeit installiert
  das Setup nach (`../Setup/EPOS-Plan.iss`, `WebView2Vorhanden`). Fehlt sie, startet die
  Anwendung — nur die Blazor-Dialoge bleiben leer. Der Profilordner der WebView2 liegt
  ausdrücklich unter `%LOCALAPPDATA%\WP-Plan\WebView2`; die Vorgabe „neben der EXE" ist unter
  `C:\Program Files` für Standardbenutzer nicht beschreibbar.
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
- **`System.Management` ist mit iU5 aus der `.csproj` geflogen** — null Codetreffer im Repo, die
  Geräte-ID lief nie über WMI. Der `PackageVersion`-Eintrag in `..\Directory.Packages.props`
  steht noch; er schadet nicht.
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
