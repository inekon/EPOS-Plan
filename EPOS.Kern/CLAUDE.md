# CLAUDE.md — `EPOS.Kern`, der Rechenkern

Der plattformfreie Kern von EPOS-Plan: **193 Dateien** (168 aus iU4, dazu `IDatenzugriff`/`SqliteDatenzugriff`
aus iU6, `ChartRenderer` aus iU7-5 und die 22 Dienste-Dateien aus iU5), `net10.0` **ohne** `-windows`, AnyCPU.
Seit Paket iU4 (03.09.2026) liegen sie physisch hier; bis dahin waren sie aus
`../WindowsFormsApplication1/` verlinkt. Seit Paket iU6 (03.09.2026) **ohne jeden Verweis
auf `System.Data.OleDb`** — weder im Quelltext noch als `PackageReference`; **CA1416 steht
bei 0**. Fachdomäne und Datenmodell stehen in der
[`CLAUDE.md` der Repo-Wurzel](../CLAUDE.md), die Windows-Anwendung in
[`../WindowsFormsApplication1/CLAUDE.md`](../WindowsFormsApplication1/CLAUDE.md).

**Die eine Regel: Eine Fachänderung am Rechenkern wird EINMAL gemacht — hier.** Die Anwendung
übersetzt diese Dateien nicht mehr mit, sie referenziert das Projekt.

```powershell
dotnet build ..\EPOS.Kern\EPOS.Kern.csproj -c Release   # 0 Fehler, 2 Warnungen
dotnet test  ..\WP-Plan.Kern.slnf -c Release            # 886 Tests
```

## Was hier liegt

| Ordner | Inhalt |
|---|---|
| `Allgemein/` | `BhkwPlan.cs` (der Rechenkern selbst, Namespace `WPPlan.Core`), Zugriffsschicht (`IDatenzugriff`, `SqliteDatenzugriff`, `DataRepository` als Fassade, `DbParam`, `DbVorgang`, `DbWerte`, `RecordSet`), `Meldung` (Melde-Haken), `Sprache`, `ZahlText`, `Zeilenumbruch`, `SolarPVGISCalculator`, `WizardItemClass` (Typ- und Nummernkatalog), `Import/AnsiEncoding.cs` |
| `Allgemein/Simulation/` | die vollständige Engine außer `SchemaModell.cs` — `SimulationControl` (beide `partial`-Hälften), `Kaskadenschleife`, `SimulationKanaele`, `Init`, `SimulationRunner`, die Module je Erzeuger/Bedarf, `WaermequelleClass`/`WaermesenkeClass`, `Warnkriterien`, `ProfilBedarf`, `StilleDb` |
| `Allgemein/Wirtschaftlichkeit/` | alle 20 Dateien — `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl`, die KWKG-/EEG-/Steuer-Rechner |
| `Allgemein/Bericht/` | die **DATEN**-Hälfte: `BerichtTexte`, `BerichtsDaten`, `EmissionsAusweis`, `KostenEmissionRechner`, `ProjektDetails`, `KennzahlenKatalog`, `AbweichungsErmittler` — dazu seit iU7-5 der **Renderer** `ChartRenderer` |
| `Allgemein/Dienste/` | die **neun Umgebungsdienste** (iU5): `Dienste` (Halter), `IDialogDienst`, `IDateiDienst`, `IPfade`, `IEinstellungen`, `ILizenzAblage`, `IGeraeteId`, `ISprache`, `INavigation`, `IProjektKontext`, ihre Standardfassungen (`StilleDialoge`, `KeineDateiwahl`, `StandardPfade`, `FluechtigeEinstellungen`, `KeineAblage`, `KeineGeraeteId`, `StandardSprache`, `KeineNavigation`, `LeererProjektKontext`) und die sprachneutralen Schlüssel `Gewerke`, `Masken`, `Ansichten`, `Projektwahl` |
| `Allgemein/Update/` | `Anlagenzeilen`, `ProjektPuffer`, `SchemaKatalog`, `SchemaStand` (Ergebniszustand der Migration und die DDL-Konstanten, die Controller zur Selbstanlage brauchen) |
| `Controller/` | 50 Controller ohne Oberflächenbezug |
| `Model/` | alle 46 Modelle |
| `MyResource/` | `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` — der Anzeigetext-Katalog beider Sprachen |
| `Properties/` | `Settings.settings`, `Settings.Designer.cs`, `Settings.cs` |

`RecordSet` ist seit iU6-T1 ein reiner vorwärtslaufender Zeilenzeiger: `DBCommand`, `_cmd`,
`MerkeSql()` und `Parameter()` sind ersatzlos gestrichen (iR8 — repositoryweit gab es **0**
externe Nutzer). Wer parametrisiert arbeiten will, nimmt `DataRepository` oder `DbVorgang`.

Verlinkt statt verschoben ist genau eine Datei: `../sql/schema/SchemaTypKatalog.g.cs` — ihre
Quelle ist `sql/tools/Erzeuge-Schema.ps1`, nicht dieses Projekt.

## Was mit Absicht NICHT hier liegt

`SchemaModell.cs` (Schema-**Ansicht**), `SchemaMigration.cs` samt Access-Zweig, `GeraeteWaisen`,
`ErststartMigration`, `AnlagenEindeutigkeit`, die Bericht-**Ausgabe** (`Bausteine/`,
`BerichtsDatenSammler`, `BerichtsKonfiguration`, `ExcelBerichtGenerator`,
`WordBerichtGenerator`, `ZeitreihenExtraktor`, `IBerichtsBaustein` — **bis iU7**),
`ChartRendererGdi` (der eingefrorene GDI+-Stand, nur noch Gegenpart des
Windows-Bildvergleichs), `Katalog/`,
`Import/` außer `AnsiEncoding`, `WizardCtrl`, die `*KontextMenuCtrl`, `MenueCtrl`, die
Stamm-Controller mit `MessageBox`, `KI/`, `Hilfe/`, `GrafikTools/`, `Export/`, `Lizenz/`,
`WPCtrl` und alle `*.WinForms.cs`.

## Regeln für Änderungen hier

**Kein WinForms-Code, kein `System.Data.OleDb`.** `EnableWindowsTargeting=false` ist der
Wächter: Jede WinForms-Berührung bricht den Build sofort, nicht erst zur Laufzeit auf dem
iPad. `System.Data.OleDb` ist seit **iU6** ganz weg — kein `using`, kein Typ, keine
`PackageReference`; die verbliebenen Kern-Pakete sind `Microsoft.Data.Sqlite` und
`System.Configuration.ConfigurationManager`. **CA1416 steht bei 0** (Verlauf 87 → 78 → 0);
**kein `NoWarn`**, damit eine neu hereingetragene Windows-API sofort als Warnung auffällt.

**Datenzugriff ausschließlich über `DataRepository` mit `new DbParam(…)`.** `DataRepository`
ist seit iU6-T4 eine **Fassade**: Die Arbeit macht `SqliteDatenzugriff` hinter
`IDatenzugriff` (sechs Ausführungs-, fünf Schemamethoden, `DatenbankVorhanden`,
`DatenbankPfad`). Für die rund 160 Aufruferdateien ändert das nichts — Signaturen,
Fehlerwortlaute und Rückgabewerte im Fehlerfall sind dieselben. Auf der Fassade bleiben mit
Absicht: der Engine-Modus (`FehlerMelden`, `EngineModus`, `StilleFehlerAbholen` — eine
Meldeentscheidung für das ganze Programm), die Pfadauflösung (`PfadUeberschreibung`,
`GetDBPath` — bekommt in iU5 ihr `IPfade`; **`PfadUeberschreibung` schlägt alles**, der
Referenzlauf hängt daran) und die vier Bequemlichkeiten (`GetMaxID`,
`DeleteWithDependencies`, `GetIdByName`, `GetValueById`).

**Die Brücke nach OleDb steht in der ANWENDUNG**, nicht hier:
`WindowsFormsApplication1/Allgemein/DbParamOleDb.cs` (`Aus`, `Von`, `Nach`,
`[SupportedOSPlatform("windows")]`). Getragen wird sie nur noch vom eingefrorenen
Access-Zweig der Erststart-Migration — `SchemaMigration`, `GeraeteWaisen` und
`SchemaVersionAccess` (die aus `ApplikationCtrl` ausgelagerten Schemamarker-Methoden).
Wer hier eine neue Zugriffsstelle schreibt, nimmt `DbParam` — sonst nichts.

**Die Umgebung ausschließlich über `Dienste.*` — nie über `Program.*`.** Seit iU5 (03.09.2026)
liegen neun Umgebungsdienste in `Allgemein/Dienste/`. Neuer Kerncode, der eine Meldung absetzt,
einen Ablageort braucht, eine Einstellung liest, die Sprache kennen will oder eine Maske öffnen
soll, ruft `Dienste.Dialog`, `Dienste.Datei`, `Dienste.Pfade`, `Dienste.Einstellungen`,
`Dienste.Lizenzablage`, `Dienste.GeraeteId`, `Dienste.Sprache`, `Dienste.Navigation` bzw.
`Dienste.Projekt`. **`Program.*` ist im Kern und in allen Kernkandidaten verboten** — der Wächter
steht unten unter „Nachweis".

| Dienst | Wofür | Vorbelegung ohne Oberfläche |
|---|---|---|
| `Dialog` | Meldung, Warnung, Fehler, Rückfrage, Dreifachwahl, Wartekurve | `StilleDialoge` — Konsole; Rückfrage = nein |
| `Datei` | Datei-/Ordnerwahl, Öffnen mit der Systemanwendung | `KeineDateiwahl` — `""` bzw. `false` |
| `Pfade` | `%APPDATA%\wp-plan`, `%APPDATA%\<Produkt>`, `LocalApplicationData[\WP-Plan]`, `CommonApplicationData\WP-Plan`, Dokumente | `StandardPfade` — `Environment.SpecialFolder` |
| `Einstellungen` | Schlüssel-Wert-Ablage, dazu ein maschinenweiter Leser | `FluechtigeEinstellungen` — Wörterbuch im Speicher |
| `Lizenzablage` | Geheimnisse; Geltungsbereich Gerät **oder** Benutzer als Parameter | `KeineAblage` — merkt nichts |
| `GeraeteId` | Gerätemerkmale für die Lizenzbindung | `KeineGeraeteId` — leer |
| `Sprache` | Kürzel, `IstEnglisch`, Umschalten | `StandardSprache` — hält `Sprache.Nummer` |
| `Navigation` | Gewerksliste auffrischen, Maske öffnen, Ansicht auffrischen | `KeineNavigation` — Leerlauf, `OeffneMaske` = `false` |
| `Projekt` | das offene Projekt (Id, Name, Klimazone, Wechsel) | `LeererProjektKontext` — `Vorhanden` = `false` |

Belegt werden alle neun an genau EINER Stelle: `Program.Main`, vor
`DataRepository.DatenbankVorhanden()`. Die Windows-Fassungen liegen in
`../WindowsFormsApplication1/Dienste/`. Ein Prüfstand tauscht ein Feld, fährt seinen Fall und legt
die Standardfassung zurück (`EPOS.Kern.Tests/DiensteTests.cs`).

**Maskennamen und Gewerke sind sprachneutrale ASCII-Schlüssel** (`Gewerke.Bhkw`,
`Masken.PufferSpAdmin`, `Ansichten.Varianten`) nach der Drei-Schichten-Regel — nie ein
Anzeigetext.

**Meldungen und Oberflächenaufgaben über Haken.** Das ältere Muster, das weiterhin gilt: ein
`static Action<…>`-Feld hier, belegt von `Program.Main` in der Anwendung, mit einer folgenlosen
oder auf die Konsole schreibenden Vorbelegung.

| Haken | Wofür | Vorbelegung |
|---|---|---|
| `Meldung.Zeigen` / `.Hinweis` / `.Warnung` / `.Warten` | Dialog statt `MessageBox.Show` bzw. Sanduhr | **seit iU5 `Dienste.Dialog`** — ohne Oberfläche damit Konsole, `Warten` folgenlos. `Program.Main` belegt diese vier Haken **nicht mehr** |
| `SimulationControl.Speicherlauf` | der Stromspeicherzweig (K8) | wird vom `[ModuleInitializer]` in `SimulationControl.Stromspeicher.cs` gesetzt, sobald diese Assembly lädt |
| `SimulationRunner.Speicherergebnismodell` | dasselbe für das Ergebnismodell | wie oben |
| `WErzeugerCtrl.GeraetewaisenAufraeumen` | Aufräumlauf nach dem Löschen eines Projekts | `null` = kein Lauf; zulässig, weil er ohnehin nach dem erfolgreichen DELETE läuft und der Migrationsschritt nachholt |
| `DataRepository.Zugriff` | die Umsetzung hinter `IDatenzugriff` (iU6-T4) | `new SqliteDatenzugriff()`; wird in iU5 an `Dienste.Daten` gehängt |

**ResX und Settings pflegen.** Der Anzeigetext-Katalog liegt jetzt hier; `Resource.Designer.cs`
ist **eingecheckter Quelltext** und muss beim Ergänzen von Schlüsseln mitgepflegt werden. Nur die
neutrale `Resource.resx` trägt den Code-Generator, die Satellitendatei nicht. Der `LogicalName`
beider Dateien ist im `.csproj` festgeschrieben
(`WindowsFormsApplication1.MyResource.Resource[.en-US].resources`), damit der Ressourcenname nicht
am Ordnerpfad hängt — der Basisname in `Resource.Designer.cs` bleibt dadurch gültig. Visual Studio
regeneriert die Designer-Datei bei jeder `.resx`-Änderung selbst; wer parallel von Hand ergänzt
hat, baut Duplikate (CS0102).

**`InternalsVisibleTo`.** Etliche Typen sind ohne Zugriffsangabe deklariert und damit `internal`
(`ProjektCtrl`, `KlimaregionCtrl`, `WPStammCtrl`, `Properties.Settings`, `Init` …). Das `.csproj`
gibt sie für `EPOS_Plan` und `EPOS.Kern.Tests` frei. Neue Typen brauchen deshalb **keine**
Sichtbarkeitsanhebung, nur weil die Anwendung sie sieht.

**Namespace bleibt `WindowsFormsApplication1`** — die Umbenennung ist eine eigene Entscheidung
(iF13), nicht Teil dieser Etappe. Bezeichner und Kommentare deutsch.

**Die Feldgrößen sind fest verdrahtet:** 8760 Stunden, 168 Wochenwerte, 365 Tage, 12 Monate, 24
Tagesstunden; Vektoren `float` mit Zwischenrechnung in `double`; Arrays werden **in-place**
überschrieben, der Rückgabewert fast überall ignoriert. Diese Konventionen beim Erweitern
beibehalten.

## Bericht: Renderer hier, Ausgabe in der Anwendung

**Der Diagramm-Renderer liegt seit iU7-5 hier** — `Allgemein/Bericht/ChartRenderer.cs`,
SkiaSharp statt GDI+ (iU7-2), ohne eine einzige Windows-API. Er ist die Vorlage für iF16
(`EPOS.UI/Standards/ChartBild`): Der Kern liefert PNG-Bytes, die Oberfläche zeigt sie an —
ein Chart-Stack für Bericht *und* Bildschirm.

**Die AUSGABE bleibt bis zu ihrer Etappe in der Anwendung:** `WordBerichtGenerator`,
`ExcelBerichtGenerator`, `Bausteine/`, `BerichtsDatenSammler`, `ZeitreihenExtraktor`. Sie
hängen an `IDateiDienst`/`ITeilen` und ziehen erst mit dem Rest des Berichts um. Der
eingefrorene GDI+-Stand `ChartRendererGdi` bleibt ebenfalls dort — er ist nur noch der
Gegenpart des Windows-Bildvergleichs (`Referenzlauf/Bildvergleich.cs`, iU7-1).

**Schriftregel iF19 — Systemschrift, flexibel.** Der Renderer bindet keine Schrift ein,
sondern fragt `SKFontManager` eine Rückfallkette ab: Calibri (Windows) → Carlito/Liberation
Sans/DejaVu Sans (Linux) → Helvetica/Arial (macOS/iOS). Das Layout ist **metrikgetrieben**:
Umbrüche und Legendenbreiten folgen den gemessenen Textmaßen, nicht festen Pixelwerten.
Folge, und das ist Absicht: **Textbreiten dürfen je Plattform abweichen.** Ein Vergleich
Windows↔Linux ist deshalb ein Struktur- und Histogrammvergleich, kein Pixelvergleich; ein
Pixelvergleich ist nur *innerhalb* einer Plattform sinnvoll (genau das macht der Modus
`bildvergleich` der Referenzlauf-Suite gegen `ChartRendererGdi`).

**Nachweis in drei Stufen.** `EPOS.Kern.Tests/ChartRendererTests.cs` (iU7-8) prüft die
Verdichtungen exakt und dass gezeichnet wird — drei Tests, in jedem Kern-Lauf dabei.
`Proben/ChartProben` (eigene `.sln`, referenziert dieses Projekt) zeichnet neun Bilder und
prüft Maße, Farbvorkommen und Determinismus; seit iU7-7 läuft die Probe in
`.github/workflows/kern.yml` auf ubuntu **und** macos, die PNG gehen als Artefakt mit. Der
Pixelvergleich gegen GDI+ läuft unter Windows.

**Die nativen SkiaSharp-Bibliotheken sind bedingt** — `Condition="$([MSBuild]::IsOSPlatform(…))"`
in `EPOS.Kern.csproj` und in `EPOS.Kern.Tests.csproj`. Welche Native passt, entscheidet die
Bauumgebung und nicht das TargetFramework; jede Umgebung zieht genau ihre eigene statt aller
drei. Win32 steht mit dabei, weil `windows.yml` `dotnet test WP-Plan.Kern.slnf` fährt.

## Nachweis

Jede Änderung hier wird gegen die eingefrorene Windows-Basis geprüft:

```bash
dotnet build EPOS.Referenzlauf/EPOS.Referenzlauf.csproj -c Release
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/neu
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/neu     # GESAMT: PASS
```

**Der iU5-Wächter — muss leer bleiben:**

```bash
git grep -nE '\bProgram\.[A-Za-z]' -- 'EPOS.Kern/*.cs' \
    '../WindowsFormsApplication1/Allgemein/*.cs' \
    '../WindowsFormsApplication1/Controller/*.cs' \
    '../WindowsFormsApplication1/Model/*.cs' | grep -vP ':\s*(///|//|\*)'
```

Dieselben drei Projekte rechnet die CI (`.github/workflows/kern.yml`) auf `ubuntu-latest` und
`macos-latest`. 1007 und 1017 führen aktive Stromspeicher-Varianten und decken damit den
K8-Haken ab; ohne sie fiele ein stillgelegter Haken nicht auf.
