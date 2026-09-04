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
nach iU9-W8 unverändert **14 Fundstellen** (dieselben wie nach iU9-W5, W6 und W7 — weder
die acht Masken der Welle 7 noch die zehn der Welle 8 trugen eine; 16 nach iU9-W4, 20 nach
iU9-W3, 22 nach iU9-W2, 24 nach iU9-W1, 30 davor — die sieben Masken der Welle 6 trugen
keine. Die Warnzahl der ganzen Mappe steht bei **20**: 14 WFO1000, 2 CS0108, 2 CS0109,
1 WFO0003, 1 CA2255), Schwerpunkt
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
- **`Controller/`** (**17** Dateien, `*Ctrl.cs`) — was Oberfläche braucht: die zwölf
  `*KontextMenuCtrl`, `MenueCtrl`, `WizardCtrl`, `KlimaregionStammCtrl`,
  `EnergietraegerKatalogCtrl` und `ProjektExportImportCtrl`.
  **`PeakShavingCtrl` ist mit iU9‑W12.0a in den Kern gezogen** — er war
  vollständig oberflächenfrei, und die Peak-Shaving-Komponente in `EPOS.UI`
  erreichte ihn hier nicht (Befund W12‑B23).
  **`WPCtrl` ist mit iU9-W7.0a in den Kern gezogen**, samt der Streichung seiner
  WinForms-Hälfte `WPCtrl.WinForms.cs` — `FillListBox(ListBox)` hatte im ganzen Bestand
  keinen Aufrufer, und ein partieller Typ geht nicht über die Assemblygrenze.
  Die übrigen **85** liegen in `../EPOS.Kern/Controller/` — 50 seit iU4,
  29 seit iU5-U4, einer seit iU8-8b, einer seit iU9-W1.5 (`KostenfaktorCtrl`), einer seit
  iU9-W0.1 (`KostenSummenCtrl`), dazu `WPCtrl` und `WaermepumpeGeraeteCtrl` aus iU9-W7.
- **`Model/`** — **keine `.cs` mehr**; alle 47 Modelle liegen in `../EPOS.Kern/Model/`
  (`EnergietraegerModel.cs` mit `EnergyCarrier`/`EnergyConversion` seit iU9-W0.1).
- **`Views/`** (**152 `.cs`**; **246 Dateien** mit `.resx`) —
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
  **Mit iU9‑W6 sind sieben weitere Masken verschwunden** — die Erzeugereingaben der
  Startkacheln, zusammen 4 202 Zeilen: die beiden Katalogeditoren
  `Form_Heizkessel_Bearbeiten` (714 Z.) und `Form_DBBHKW` (712 Z.), die beiden Hosts
  `Form_Heizkessel` (767 Z.) und `Form_BHKWEing` (940 Z.) sowie `Form_PV` (338 Z.),
  `Form_Stromspeicher` (354 Z.) und `Form_PufferSp` (377 Z.). An ihrer Stelle stehen
  **fünf Hüllen** — `Views/Heizkessel/HeizkesselHuelle.cs`, `Views/BHKW/BhkwHuelle.cs`,
  `Views/Photovoltaik/PhotovoltaikHuelle.cs`,
  `Views/Stromspeicher/StromspeicherHuelle.cs`,
  `Views/Pufferspeicher/PufferspeicherHuelle.cs` —, dazu
  `Allgemein/Blazor/BlazorAssistentSeite.cs` und
  `Views/Wizard/IAssistentErzeugerSeite.cs`. **Vier der sieben sind zugleich
  Assistentenseiten** (PV, Stromspeicher, Heizkessel, BHKW) und damit die ersten, die
  als Razor-Komponente im Assistentenrahmen laufen; `WizardParent` hängt sie über EINE
  Schnittstelle ein statt über je zwei Zeilen mit hartem Typumbruch, und
  `WizardParent.Aktiver` entfällt für sie. Die Datenseite ist in acht Kern-Controller
  gewandert (Portprotokoll § 2.2); neu im Kern ist `Allgemein/EmissionsVorgaben.cs`.
  **Mit iU9‑W7 sind acht weitere Masken verschwunden** — die Gewerke Wärmepumpe und
  Solarthermie, zusammen 3 065 Zeilen: `Form_WpFilterAuswahl` (325 Z., samt `WPData` und
  `WPDataCtrl`, die dort am Dateiende standen), `Kenndaten` (190 Z.), `Form_WP` (585 Z.),
  `Wizard_WPItem` (690 Z.), `Form_WPAuswahl` (341 Z.), `Form_SolarDB` (286 Z.),
  `Form_SolarKollektoren` (516 Z.) und `Form_Solarganglinie` (132 Z.). An ihrer Stelle
  stehen **fünf Hüllen** — `Views/Wärmepumpe/WaermepumpeStammHuelle.cs`,
  `Views/Wärmepumpe/WaermepumpeAnlageHuelle.cs`,
  `Views/Wärmepumpe/WaermepumpenHuelle.cs`,
  `Views/Solarthermie/SolarkollektorHuelle.cs`,
  `Views/Solarthermie/SolarganglinieHuelle.cs`. **Zwei davon sind zugleich
  Assistentenseiten** (7 = Wärmepumpen-Verwaltung, 8 = Solarkollektoren); damit laufen
  sechs der dreizehn Seiten als Razor-Komponente, und `WizardParent` führt für sie keinen
  Typumbruch-Zweig mehr. Die Datenseite ist in sieben Kern-Controller gewandert; neu im
  Kern sind `Model/WaermepumpenKatalogZeile.cs`, `Model/KennlinienSatz.cs`,
  `Allgemein/WaermepumpenKatalogFilter.cs`, `Controller/WaermepumpeGeraeteCtrl.cs` und die
  Renderer-Methode `ChartRenderer.Kennlinien` (968 × 520, zwei neue ChartProben).
  `Wizard_WPItem` ist nicht gelöscht, sondern nach
  `Werkzeuge/Formularkarte.Tests/Pruefmuster/Wizard/` verschoben — drei Abschnitt-Tests
  brauchen ihren Behälterbaum aus GroupBox, TabControl und TabPage als Analysegegenstand.
  **Mit iU9‑W8 sind zehn weitere Masken verschwunden** — die drei Bedarfsblätter
  (Stromverbraucher, Prozesswärme, Brauchwasser) in je drei Ausprägungen und die
  Gebäudetypen-Verwaltung, zusammen 2 569 Zeilen und 41 MessageBox:
  `Form_EingDBStromverbraucher` (146 Z.), `Form_EingDBProzess` (174 Z.),
  `Form_EingDBBrauchwasser` (139 Z.), `Form_ErgStromverbraucher` (169 Z.),
  `Form_ErgProzesswaerme` (215 Z.), `Form_ErgBrauchwasserwaerme` (425 Z.),
  `Form_EingStromTyp` (334 Z.), `Form_EingProzTyp` (366 Z.),
  `Form_EingBrauchwasserTyp` (257 Z.) und `Form_EingGebTyp` (344 Z.). **Zehn Masken
  werden VIER Komponenten**: Die drei Blätter sind Drillinge, ihre Ausprägung ist ein
  Aufzählungstyp (`BedarfsArt`) und keine dritte Fassung — der Feldkartenabgleich läuft
  deshalb je AUSPRÄGUNG. An ihrer Stelle steht der neue Ordner `Views/Bedarf/` mit
  **drei Hüllen** — `BedarfErgebnisHuelle.cs`, `TypStammHuelle.cs` (trägt zwei
  Komponenten, weil „DB ändern" und „Typ ändern" derselbe Aufrufweg sind),
  `GebaeudetypHuelle.cs`. Die Datenseite ist in vier Kern-Controller gewandert
  (`BedarfStammCtrl` und `TypProfilCtrl` neu, `ProzesswaermeStammCtrl` und `TagVCtrl`
  erweitert); neu im Kern sind außerdem `Model/BedarfsArt.cs` und die drei
  Renderer-Methoden `ChartRenderer.MonatsSaeulen`/`Stundenprofil`/`Jahresverlauf`
  (ChartProben 12 → 15).
  **Mit iU9‑W9 sind acht weitere Masken verschwunden** — die vier
  Bedarfskacheln des Startbilds samt ihren Katalogeditoren, zusammen 3 289
  Zeilen und 42 MessageBox: `Form_Gebaeude` (695 Z.), `Form_Gebaeude1`
  (338 Z.), `Form_Gebaeude2` (400 Z.), `Form_GebWohnflaeche` (66 Z.),
  `Form_Waermebedarf` (324 Z.), `Form_Prozesswaerme` (592 Z.),
  `Form_Stromverbraucher` (422 Z.) und `Form_Brauchwasser` (452 Z.).
  **Acht Masken werden FÜNF Komponenten**: `Form_Gebaeude1` und
  `Form_Gebaeude2` bearbeiteten mit `frm.model = model` DENSELBEN Satz und
  werden zwei Reiter; die drei Bedarfsblätter sind Drillinge wie in Welle 8.
  An ihrer Stelle stehen **fünf Hüllen** — `Views/Gebäude/GebaeudeHuelle.cs`,
  `Views/Gebäude/GebaeudeKatalogHuelle.cs`,
  `Views/Gebäude/GebaeudeWohnflaecheHuelle.cs`,
  `Views/Wärmebedarf/WaermebedarfExternHuelle.cs`,
  `Views/Bedarf/BedarfsProfileHuelle.cs`. **Vier davon sind zugleich
  Assistentenseiten** (2 = Gebäude, 3 = Wärmebedarf extern, 4 = Prozesswärme,
  5 = Stromverbraucher); damit laufen ZEHN der dreizehn Seiten als
  Razor-Komponente, und `WizardParent` führt für sie keinen Typumbruch-Zweig
  mehr. Die Assistentenschnittstelle trägt seit W9.0a jeden Listentyp
  (`IAssistentListenSeite<T>`, `BlazorAssistentSeite<TKomponente, TModell>`).
  Neu im Kern sind `Allgemein/Ferienzeit.cs` und `Allgemein/Suchmuster.cs`;
  erweitert sind `GebaeudeStammCtrl` (Listen, Katalogfilter, zwei
  Ableitungen), fünf `Z_Projekt*Ctrl` (`LiesProjekt`),
  `WaermebedarfStammCtrl.HatProjektzuordnung`, `ProjektCtrl.Existiert` und
  `BedarfStammCtrl.Jahressumme`.
  **Mit iU9‑W10a sind sieben weitere Masken verschwunden** — alle
  Dialoge, die `Form_Simulation_Config` öffnet, zusammen 7 803 Zeilen
  und 30 MessageBox: `Form_Betriebsmodus` (144 Z.),
  `Form_Klimazonenkarte` (96 Z.), `Form_QuelleErdreich` (1 273 Z.),
  `Form_QuellePufferspeicher` (1 089 Z.), `Form_Quellprofil`
  (1 084 Z., ohne Designer), `Form_Waermesenke` (2 050 Z., ohne
  Designer) und `Form_PufferSp_Projekt` (2 067 Z.). Mit der
  Klimazonenkarte fällt ihr einziges Steuerelement
  `Allgemein/GrafikTools/KlimazonenKarte.cs` (326 Z.) samt den beiden
  eingebetteten Ressourcen `Ressourcen/Zonenkarte_Klimazonen.png|svg`
  und dem `EmbeddedResource`-Block der `.csproj`. An ihrer Stelle
  stehen **sechs Hüllen** — `Views/Simulation/BetriebsmodusHuelle.cs`,
  `Views/Simulation/QuelleErdreichHuelle.cs`,
  `Views/Simulation/QuellePufferspeicherHuelle.cs`,
  `Views/Simulation/QuellprofilHuelle.cs`,
  `Views/Simulation/WaermesenkeHuelle.cs`,
  `Views/Pufferspeicher/PufferSpProjektHuelle.cs`. Die Klimazonenkarte
  bekommt keine eigene Hülle: Sie hat genau einen Aufrufer und ist
  dort eine **Überlagerung**. Neu ist das Sprungziel
  `PufferSpAdminNurLesen` — der Projektdialog schaut den
  Auslieferungskatalog nur an, `Masken.PufferSpAdmin` kannte dieses
  Kennzeichen nicht (Befund W10‑B28). **`Form_Simulation_Config`
  selbst bleibt WinForms** und ist mit **W10b** an der Reihe.
  Die Welle nimmt nur FÜNF Designer-Dateien mit, weil Quellprofil und
  Wärmesenke nie einen hatten (Befund W10‑B38).
  **Mit iU9‑W10b ist auch ihr Wirt weg** — `Form_Simulation_Config` mit
  ihren vier Teildateien (678 + 2 248 + 433 + 1 199 = 4 558 Z.), dem
  Designer (337 Z.) und der einzigen `.resx` der ganzen Welle; dazu die
  drei Steuerelement-Klassen `ErzeugerKarte` (781 Z.), `SpeicherKarte`
  (551 Z.) und `SchemaAnsicht` (789 Z.) sowie `Eingabefrage` (49 Z.,
  letzter Nutzer). An ihrer Stelle steht **eine Hülle** —
  `Views/Simulation/SimulationKonfigHuelle.cs` — und in `EPOS.UI` die
  **Seite** `Seiten/Simulation/SimulationKonfigSeite` samt den drei neuen
  Bausteinen `Schema`, `ErzeugerKachel` und `SpeicherKachel`.
  **Entscheid R‑W10b‑1:** Die Komponente ist eine SEITE (mit
  `SeitenZustand`, `Seitenschluessel.SimulationKonfiguration` und einem
  Zweig in `AppWurzel` — die erste Fachseite, die iOS erreicht), erscheint
  unter Windows aber **bis W16 in der modalen Dialoghülle**: Beide
  Aufrufer brauchen die modale Rückkehr, `Form_Simulation_Detail` springt
  danach auf seinen gemerkten Reiter zurück. Die Datenseite ist in den
  Kern gewandert (`SchemaModell` verschoben, `SchemaLayout`, `Kaskade`,
  `AnlagenInfo`, `WaermequelleClass.QuelleSchreiben` und fünf
  Controller-Wege, Befund W10‑B35). Die sechs Hüllen der Welle 10a
  verlieren ihren FENSTERweg und behalten ihren Parametersatz — die sieben
  Dialoge sind jetzt Überlagerungen der Seite (Risiko R2).
  Der **Stapellauf der Formularkarte zählt seither 38 Masken** (43 nach iU9‑W11b, 49 nach iU9‑W10b, 50 nach iU9‑W10a, 55 nach iU9‑W9, 63 nach iU9‑W8, 73 nach iU9‑W7, 81 nach
  iU9‑W6, 88 nach iU9‑W5, 91 nach iU9‑W4, 98 nach iU9‑W3, 102 nach iU9-W2, 105 nach
  iU9-W0, 111 nach iU9-W1, 118 davor), lokalisiert sind noch **25** (27 nach W11b, 28 nach W10b, 29 nach W10a, 37 nach W8,
  47 nach W7), und die
  Erreichbarkeit steht auf **37 von 38, 0 × „nein", 0 × „verwaist"**; jede weitere Welle senkt die
  Zahl. **Der Anker des Erreichbarkeitstests hängt seit iU9‑W12 an
  `Form_AdminSettings`** (`MDIMainForm → MenuItem_Einstellungen`): Von den zwölf
  Masken mit einem Pfad ab `Form_Start` fällt keine erst in W13 oder W14
  (Befund W12‑B26), der Test kann seine Form „über die Startseite" also nicht
  behalten.
  **Mit iU9‑W11b sind sechs weitere Masken verschwunden — die letzten des
  Simulationsbereichs**, zusammen 11 031 Zeilen `.cs`, 4 201 Zeilen Designer,
  21 MessageBox und 17 Zeichenflächen: `Form_Simulation_Detail` (7 629 Z. +
  3 082 Designer + drei `.resx` mit 3 049/8/248 Einträgen), `DashboardForm`
  (488 Z.), `NavigatorUebersicht` (433 Z.), `NavigatorStrom` (417 Z.),
  `NavigatorWaerme` (1 083 Z.) und `Form_SpeicherVariantenVergleich` (832 Z.).
  **Alle sechs im SELBEN Commit** (Regel R‑W11‑2: maskenweise, nicht reiterweise —
  reiterweise stünden zwei WebViews in einem Fenster). Ohne Designer und deshalb
  nie im Stapellauf, aber ebenfalls gelöscht: `TabNavigationManager` (226 Z.),
  `TabListMapper` (462 Z.), `GanglinienDarstellung` (97 Z., der WinForms-Rest nach
  W11a) und `SchluesselEintrag` (37 Z., letzter Nutzer war `NavigatorWaerme`).
  An ihrer Stelle steht **eine Hülle in vier Teildateien** —
  `Views/Simulation/SimulationErgebnisHuelle.{cs,Anzeige.cs,Bilder.cs,Wege.cs}` —
  und in `EPOS.UI` die **Seite** `Seiten/Simulation/SimulationErgebnisSeite` mit
  ihren zwölf Reiterkomponenten.
  **Entscheid R‑W11‑1:** Die Komponente ist eine SEITE (mit `SeitenZustand`,
  `Seitenschluessel.SimulationErgebnis` und einem Zweig in `AppWurzel` — die
  zweite Fachseite, die iOS erreicht), erscheint unter Windows aber **bis W16 in
  der modalen Dialoghülle**, 1 474 × 821: Die beiden Bedarfsobjekte gehören
  `Form_Start` und werden hier weitergeschrieben (Befund W11‑B3); zwei
  nebeneinander offene Fenster wären im Streit. **Der Simulationslauf läuft in
  `Task.Run`** (seit W11a) und meldet seine fünf Phasen an den Baustein
  `Fortschritt`; er startet beim Öffnen von selbst, wie eh und je.
  `Allgemein/GrafikTools/ChartManager.cs` verliert `DonutChartDrawer` und
  `Kacheln` (ihr einziger Nutzer war `NavigatorUebersicht`), **bleibt aber**:
  `Form_Klimadaten` und `Form_PeakShaving` führen weiter interaktive
  WinForms-Charts. **`Views/Simulation` führt seither KEINE Designer-Maske mehr**;
  `Form_SpeicherOptimierung` bleibt WinForms (iF22) und ist über die
  **Sprungbrücke** (`Sprungziel.SpeicherOptimierung`) zu erreichen — das erste
  Brückenziel mit Parameter und mit einer fachlichen Rückgabe statt `DialogResult`.
  Protokoll:
  [`Allgemein/Reporting/iU9_W11b_Blazor_Port_Protokoll.md`](Allgemein/Reporting/iU9_W11b_Blazor_Port_Protokoll.md).
  **Mit iU9‑W11a ist KEINE Maske verschwunden** — die Welle verlegt, was ohne
  Oberfläche geht, in den Kern und hängt die sechs Ergebnismasken schon daran;
  W11b baut danach die Ergebnisseite in EINEM Schritt und löscht sie (Empfehlung
  der Vermessung § 11.5: maskenweise, nicht reiterweise — sonst zwei WebViews in
  einem Fenster). Angefasst sind `Form_Simulation_Detail`, `DashboardForm`,
  `NavigatorUebersicht`, `TabNavigationManager`, `Form_SpeicherVariantenVergleich`,
  `Form_SpeicherOptimierung` und `Form_Start`: elf inline-SQL-Stellen, rund
  600 Zeilen Fachrechnung, die 39 Kennzahlzeilen des Stromspeichers und die drei
  dreifachen Anzeigetexte stehen jetzt im Kern
  (`SimulationErgebnisCtrl`, `SimulationLaufCtrl`, `SpeicherKennzahlenBlock`,
  `SpeicherAnzeigeCtrl`, `ErgebnisPraesenz`, `Ganglinie`). **Der offene Punkt
  W11a‑O‑1 ist mit W11b entschieden** (Anwender, 04.09.2026): Die sechs Summen des
  Ergebnisblocks führen die DECKUNG je Erzeuger statt der Produktion, und die
  Restwärme ist EINE Zahl — `sim.Restwaerme`, per Konstruktion nicht negativ.
  **`Form_Simulation_Detail.btn_Simulation_Click` läuft seither NEBENLÄUFIG** —
  siehe „Nebenläufigkeit" unten. `KonfigurationCtrl.LiesProjekt` haben W10b und
  W11a gleichzeitig gebraucht; es gibt sie EINMAL (siehe dort). Protokoll:
  [`Allgemein/Reporting/iU9_W11a_Kern_Protokoll.md`](Allgemein/Reporting/iU9_W11a_Kern_Protokoll.md).
  **Mit iU9‑W12 sind sechs weitere Masken verschwunden — die Stromganglinien,
  die Lastspitzenkappung und der gemeinsame Konfliktdialog des Imports**,
  zusammen 2 134 Zeilen `.cs`, 1 409 Zeilen Designer, 10 `MessageBox` und
  13 indirekte über `Program.ZahlPruefen`: `Form_GanglinieProtokoll` (148 Z.),
  `Form_GanglinieImportOptionen` (383 Z.), `Form_ImportKonflikte` (441 Z., ohne
  Designer), `Form_Stromganglinie_Admin` (276 Z.), `Form_Stromganglinie`
  (125 Z.) und `Form_PeakShaving` (761 Z.). An ihrer Stelle stehen **vier
  Hüllen** — `Views/Import/ImportKonflikteHuelle.cs`,
  `Views/Stromverbraucher/StromganglinieAdminHuelle.cs`,
  `Views/Stromverbraucher/StromganglinieHuelle.cs`,
  `Views/Stromspeicher/PeakShavingHuelle.cs`.
  **Der rote Faden ist die AP5-Importkette**, die zweimal wörtlich im Bestand
  stand (mit Ablage in der Verwaltung, ohne in der Lastspitzenkappung); sie ist
  jetzt EIN Kern-Ablauf `GanglinienImportAblauf` mit drei Rückrufen, und die
  drei Zwischenmasken erscheinen als **Überlagerung** desselben Fensters.
  `ImportKonflikteHuelle` ist **Zwischenstand**: Sie bedient die vier
  Importmasken der Welle 13 (je eine geänderte Zeile) und wird dort gelöscht.
  Neu im Kern sind `Allgemein/Import/GanglinienImportAblauf.cs`,
  `Allgemein/Import/GanglinienOptionenModell.cs`,
  `Allgemein/Import/GanglinienProtokollText.cs` (verschoben),
  `Allgemein/Katalog/ImportKonfliktModell.cs`,
  `Allgemein/Bericht/PeakShavingBild.cs`,
  `Controller/PeakShavingKennzahlenBlock.cs`,
  `Controller/PeakShavingEingaben.cs` und `Controller/PeakShavingCtrl.cs`
  (verschoben) — **`Controller/` führt damit noch 17 Dateien**.
  **Der Rechenlauf der Lastspitzenkappung läuft seither NEBENLÄUFIG** (siehe
  „Nebenläufigkeit"), und das Vorher/Nachher-Bild kommt als PNG aus
  `ChartRenderer.ErzeugerStapel` — kein neuer Renderer, die ChartProben bleiben
  bei 30. Der **Nachweis der Welle ist der bitgleiche Ganglinien-Import**:
  zwölf Proben unter `EPOS.Kern.Tests/Proben/Ganglinien/` mit eingefrorenen
  Erwartungswerten, angelegt VOR dem Umbau (dabei fiel Befund W12‑B27 — der
  Excel-Zweig war überhaupt nicht benutzbar). **`Views/Import` führt seither
  keine Maske mehr**, `Views/Stromverbraucher` und `Views/Stromspeicher` je
  eine. Protokoll:
  [`Allgemein/Reporting/iU9_W12_Blazor_Port_Protokoll.md`](Allgemein/Reporting/iU9_W12_Blazor_Port_Protokoll.md).
- **`Allgemein/`** (**42** `.cs`; 43 vor iU9‑W10b — `Simulation/SchemaModell.cs` ist in den Kern gezogen; 44 vor iU9‑W10a — `GrafikTools/KlimazonenKarte.cs` ist mit seiner einzigen Maske gefallen) — geteilte Infrastruktur, siehe unten. Seit iU5 frei von
  `Program.*`, `MessageBox`, Registry, DPAPI und `SpecialFolder`; die Ausnahmen
  (`Update/ErststartMigration.cs`, `Update/SchemaMigration.cs`, der Oberflächenbaustein
  `HelpExtender` in `Hilfe/HelpCatalog.cs`) sind im iU5-Statusblock des Umsetzungskonzepts
  begründet. `Simulation/`, `Wirtschaftlichkeit/`, `Bericht/`
  (Daten **und** Ausgabe), `Lizenz/`, `Import/`, `Katalog/`, `Export/` und das KI-Wissen sind
  in den Kern gezogen. Hier bleiben: `Blazor/` (die Hülle, iU8), `Update/` (Schemamigration und
  Access-Zweig samt `DbParamOleDb`, `SchemaVersionAccess`, `GeraeteWaisen`), `GrafikTools/`,
  `Hilfe/`, die WinForms-nahen Teile von `KI/` (was der Assistent **bedient**),
  `Bericht/BerichtsDatenSammler.cs` (`ChartRendererGdi.cs` ist mit iF23 gelöscht), dazu `BaseForm`,
  `Form_Hinweis`, `FensterEinpassung`, `SpeichernLeiste`, `IAssistentRahmen` und `StromTestClass`.
  **`Simulation/` führt seit iU9‑W10b keine `.cs` mehr** — `SchemaModell.cs` war die
  letzte und liegt jetzt im Kern; im Ordner bleiben die Konzept- und
  Umsetzungsprotokolle. Die vollständige Begründung je Datei steht in
  [`../EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md).

Die Aufteilung im Einzelnen — was im Kern liegt und was mit Absicht hier geblieben ist — steht im
Kopfkommentar von [`../EPOS.Kern/EPOS.Kern.csproj`](../EPOS.Kern/EPOS.Kern.csproj) und in
[`../EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md).

## Module in `Allgemein/`

| Ordner | Inhalt |
|---|---|
| `Bericht/` | **Nur noch eine Datei.** `BerichtsDatenSammler`, weil er `EnergieMengen` aus `Views/Varianten/` ruft. Alles andere liegt in `../EPOS.Kern/Allgemein/Bericht/`: die DATEN-Hälfte seit iU4, der Renderer `ChartRenderer` (SkiaSharp) seit iU7-5, die AUSGABE (`WordBerichtGenerator`, `ExcelBerichtGenerator`, `Bausteine/`, `BerichtsKonfiguration`, `ZeitreihenExtraktor`, `IBerichtsBaustein`) seit iU5-U3. Die `.docx`-Vorlage bleibt hier und wird neben die EXE kopiert | `ChartRendererGdi` (GDI+-Stand) und `Referenzlauf/Bildvergleich.cs` sind mit iF23 am 03.09.2026 gelöscht |
| `Wirtschaftlichkeit/` | **vollständig in `../EPOS.Kern/Allgemein/Wirtschaftlichkeit/`** (iU4): `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl` und 16 weitere |
| `Simulation/` | **vollständig in `../EPOS.Kern/Allgemein/Simulation/`** (iU4; `SchemaModell.cs` als letzte Datei mit iU9‑W10b). Engine: `SimulationControl`, `Init`, `SimulationRunner` + Module je Erzeuger/Bedarf (`SimulationWaermebedarf`, `…Waermepumpe`, `…BHKW`, `…PV`, `…Solarthermie`, `…SPK`, `…SSP`, `…Pufferspeicher`). Seit der Konzeptumsetzung 27./28.08.2026 (**ein Rechenweg, dreikanalig** Heizung/Brauchwasser/Prozess): `Kaskadenschleife` (Stundenschleife Phasen A–G, Ladeaufträge je Rang), `SimulationKanaele` (`Kanal`/`Kanalsatz`/`Senkenliste`/`Ladeordnung`-Umfeld), `WaermequelleClass`/`WaermesenkeClass`, `Warnkriterien` (Katalog W1–W6 + harte Guards, eine Wahrheit für Dialog und Laufstart), `ProfilBedarf`, `SchemaModell` (Schema-Ansicht), `StilleDb`; Schichtspeichermodell (N = 1…10, SOC führend) vollständig in `SimulationPufferspeicher`; Booster-Quelltemperatur stundengekoppelt, Lesepunkt je Projekt wählbar (`Tab_Einstellungen.Booster_Lesepunkt`, Default „Davor" = Stundenanfang; Paket B2); Kessel-Temperaturbezug je Anlage `Tab_Energieanlagen.WQ_TemperaturModus` („Berechnet" = Bezugskette Senkenspeicher→Katalog→70/50, Default, ohne Pflegezwang; „Fest" = Vorgabe, Warnung nur wenn Paar fehlt). Historie und Invarianten je Paket: `*_Protokoll.md` im selben Ordner |
| ~~`Lizenz/`~~ | **seit iU5-U1 in `../EPOS.Kern/Allgemein/Lizenz/`**: `LizenzManager`, `LizenzToken`, `LizenzServerClient`, `GeraeteId` — signiertes Token, Zustände von `NichtAktiviert` bis `Lesemodus`. Die Ablage läuft über `Dienste.Lizenzablage`; **Geltungsbereich Gerät** (DPAPI `LocalMachine`) für Token und Zeitanker, **Benutzer** (`CurrentUser`) für den KI-Schlüssel — ein Wechsel entwertet jede installierte Lizenz |
| `KI/` | **geteilt seit iU5-U2:** Was der Assistent **weiß** (`HilfeWissen`, `WikiWissen`, `SemantikIndex`, `SemantikModell`, `KiEinwilligung`, `KiSchreibschutz`, `KiSicherungspunkt`, die Textkataloge) liegt im Kern; was er **bedient**, bleibt hier — `KiChatService`, `KiDialogZugriff`, `KiAusfuehrer`, `HilfeKontext`, `KiAufrufKnopf` und die `Aktionen/`. Inhaltlich unverändert: `KiChatService` (Gemini 2.5 Flash-Lite über REST), `HilfeKontext`, `HilfeWissen`, seit 29.08.2026 `WikiWissen` (Wiki-Suche + Klartext-Auszüge + 24-h-Cache `%APPDATA%\wp-plan\wiki-wissen\`, speist die „Hilfeabschnitte" des Prompts; Chatfenster ohne KI = Online-Doku-Suche; Protokoll `H4H5_Umsetzung_Protokoll.md`); API-Key als DPAPI-Datei `%APPDATA%\wp-plan\ki-schluessel.dat` (Registry-Altwert wird einmalig migriert und gelöscht) |
| ~~`Import/`~~ | **seit iU5-U1 in `../EPOS.Kern/Allgemein/Import/`**: `VDI 3805/` (Kessel, Puffer, Kollektoren, WP), `CEC/` + `Pan/` (PV-Module), `CsvReader`, `GanglinienDatei`, `AnsiEncoding`. Ebenso `Katalog/` und `Export/` |
| `GrafikTools/` | `ChartManager`, `RoundedPanel` (`KlimazonenKarte` ist mit iU9‑W10a.3 gelöscht — der Baustein `Bildkarte` in `EPOS.UI` tritt an seine Stelle) |
| `Hilfe/` | `WikiHelpCatalog` (in `HelpCatalog.cs`) — lädt die Rubrik `Programm Dokumentation/` von `wiki.epos-plan.de` (Action-API `allpages`+`apprefix`, Basis-URL aus `Settings.WordPressUrl`, Not-Rückfall `Program.WIKI_STANDARD`); `HilfeAutomatik`, `help_mapping.txt`/`help_cache.json` (Ziele = Kurznamen der Rubrik-Unterseiten, optional `#anker`), `DokuUebersetzung` (EN über translate.goog). Umsetzung 29.08.2026, Protokoll `H1H2_Umsetzung_Protokoll.md` im selben Ordner |
| `Blazor/` | **Die Hülle für Razor-Dialoge und -Seiten (iU8 / iU9-W5).** `BlazorDialogForm<T>` — ein modales `Form` mit `BlazorWebView`, das eine Komponente aus `EPOS.UI` zeigt und ihr Ergebnis als `DialogResult` zurückgibt; `DpiInsel` (P/Invoke `SetThreadDpiAwarenessContext`); `BlazorDienste` — das Dienstverzeichnis der WebView, einmal gebaut; seit iU9-W1.2 `NamensDialogHuelle` für die fünf zeichengleichen Namensabfragen des Bestands (seit iU9-W2.1 alle fünf umgestellt: `Bezeichner`, `BezeichnerUndBeschreibung`, `FragenMitHinweis`); seit iU9-W2.2 `Sprungbruecke` — Schlüssel → `Form`, **modal aus dem Rückruf einer Razor-Komponente heraus** (nur WinForms-Ziele; seit iU9-W6.0d auch die vier Katalogverwaltungen der Erzeuger, seit iU9-W7.0f die Stammdaten der Solarthermieganglinien, seit iU9-W10a.0c die Pufferspeicher-Verwaltung NUR ZUM ANSEHEN — `PufferSpAdminNurLesen`, ein eigener Schlüssel, weil derselbe Sprung ohne das Kennzeichen aus dem Nachschlagen das Bearbeiten des Auslieferungskatalogs machte); seit iU9-W6.0e `BlazorAssistentSeite<T>` — dasselbe für eine ASSISTENTENSEITE: randlos, `TopLevel = false`-tauglich, die WebView verzögert in `Bestuecken` gebaut (Risiko R5), beim Wiederbesuch wird die Wurzelkomponente getauscht statt der WebView. Seit iU9-W4.0 gilt für Blazor-Ziele nicht mehr der nachgelagerte Sprung, sondern der Baustein `Ueberlagerung`: ein modaler Bereich IM selben Fenster, also ohne zweite WebView (Risiko R2). Die Hülle liefert dafür `Gaben()` statt `Oeffnen()`. **Seit iU9-W5.0 gibt es die zweite Hüllenform: `BlazorSeite<T> : UserControl`** — nicht-modal, für eine Seite, die in einer vorhandenen Maske sitzt und dort bleibt (`Form_Start.tabPage6`). Sie trägt dieselben `CreationProperties` wie die Dialoghülle, insbesondere denselben `UserDataFolder`: ein gemeinsamer Browserprozess für Dialoge und Seiten. **Eine WebView je Fenster** (Risiko R5) — umgeschaltet wird in der Komponente (Baustein `Reiter` bzw. die Navigation von `BerichteKostenSeite`), nicht durch eine zweite Hülle. Der Projektwechsel läuft über `EPOS.UI.Dienste.SeitenZustand`, ein Objekt mit Änderungsereignis, damit die WebView **nicht** neu gebaut wird. **DPI:** Die `DpiInsel` wirkt nur für den modalen Lauf; eine eingebettete Seite sitzt im Fenster der DpiUnaware-`Form_Start` und wird ab 125 % bitmapskaliert — `BlazorSeite` versucht es deshalb gar nicht erst und dokumentiert den Befund (offener Entscheid iF21). Die **einzige** Stelle, an der WinForms und Blazor aufeinandertreffen |
| `Reporting/`, `Waermespeicher/` | **nur Konzept-/Standdokumente**, kein Code — darunter die Portprotokolle `B5b_Blazor_Port_Protokoll.md`, `iU9_W1_Blazor_Port_Protokoll.md`, `iU9_W2_Blazor_Port_Protokoll.md`, `iU9_W3_Blazor_Port_Protokoll.md`, `iU9_W4_Blazor_Port_Protokoll.md`, `iU9_W5_Blazor_Port_Protokoll.md`, `iU9_W6_Blazor_Port_Protokoll.md`, `iU9_W7_Blazor_Port_Protokoll.md`, `iU9_W8_Blazor_Port_Protokoll.md`, `iU9_W9_Blazor_Port_Protokoll.md`, `iU9_W10a_Blazor_Port_Protokoll.md`, `iU9_W10b_Blazor_Port_Protokoll.md`, `iU9_W11a_Kern_Protokoll.md`, `iU9_W11b_Blazor_Port_Protokoll.md` und `iU9_W12_Blazor_Port_Protokoll.md` (Feldkartenabgleich, Abweichungen A-n, Windows-Abnahme je Welle) |

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
- **Nebenläufigkeit: DREI Rechnungen laufen im Hintergrund, sonst keine.**
  `Form_SpeicherOptimierung` seit iF22 (Rastersuche), seit **iU9‑W11a.4** der
  Simulationslauf der Ergebnisseite und seit **iU9‑W12.6** die
  Lastspitzenkappung (`PeakShavingHuelle`: Kappungslauf, Schwellensuche, das
  Lesen der Ganglinienwerte und das Zeichnen — Befund W12‑B22; in einer WebView
  ist der Renderfaden derselbe Faden). Beide folgen derselben Aufteilung
  (Klassenkopf `Form_SpeicherOptimierung.cs:29–46`): **Der Bedienfaden liest die
  Datenbank**, der Hintergrund rechnet, das Marshalling besorgt `Progress<T>` (auf
  dem Bedienfaden erzeugt, übernimmt dessen `SynchronizationContext`). In der
  Detailansicht heißt das: `SimulationLaufCtrl.Vorpruefen`/`Bedarf`/`Bestuecken`
  auf dem Bedienfaden, `Laufen` in `Task.Run`, danach die Anzeige wieder auf dem
  Bedienfaden. Ein `Entsorgt()`-Test steht in **jedem** Zweig nach dem `await` —
  der häufigste Grund für einen Abbruch ist, dass der Anwender die Maske zumacht,
  und ein Zugriff auf ein Steuerelement landete dann als
  `ObjectDisposedException` in einer `async void`-Fortsetzung, also unbehandelt.
  **`DataRepository.EngineModus` ist prozessweit** — zwei gleichzeitige Läufe sind
  ausgeschlossen, der Startknopf ist für die Dauer gesperrt.
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
