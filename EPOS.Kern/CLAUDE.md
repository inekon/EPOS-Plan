# CLAUDE.md — `EPOS.Kern`, der Rechenkern

Der plattformfreie Kern von EPOS-Plan: **170 Dateien**, `net10.0` **ohne** `-windows`, AnyCPU.
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
dotnet test  ..\WP-Plan.Kern.slnf -c Release            # 805 Tests
```

## Was hier liegt

| Ordner | Inhalt |
|---|---|
| `Allgemein/` | `BhkwPlan.cs` (der Rechenkern selbst, Namespace `WPPlan.Core`), Zugriffsschicht (`IDatenzugriff`, `SqliteDatenzugriff`, `DataRepository` als Fassade, `DbParam`, `DbVorgang`, `DbWerte`, `RecordSet`), `Meldung` (Melde-Haken), `Sprache`, `ZahlText`, `Zeilenumbruch`, `SolarPVGISCalculator`, `WizardItemClass` (Typ- und Nummernkatalog), `Import/AnsiEncoding.cs` |
| `Allgemein/Simulation/` | die vollständige Engine außer `SchemaModell.cs` — `SimulationControl` (beide `partial`-Hälften), `Kaskadenschleife`, `SimulationKanaele`, `Init`, `SimulationRunner`, die Module je Erzeuger/Bedarf, `WaermequelleClass`/`WaermesenkeClass`, `Warnkriterien`, `ProfilBedarf`, `StilleDb` |
| `Allgemein/Wirtschaftlichkeit/` | alle 20 Dateien — `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl`, die KWKG-/EEG-/Steuer-Rechner |
| `Allgemein/Bericht/` | die **DATEN**-Hälfte: `BerichtTexte`, `BerichtsDaten`, `EmissionsAusweis`, `KostenEmissionRechner`, `ProjektDetails`, `KennzahlenKatalog`, `AbweichungsErmittler` |
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
`ErststartMigration`, `AnlagenEindeutigkeit`, die Bericht-**Ausgabe** (`ChartRenderer`,
`Bausteine/`, `BerichtsDatenSammler`, `BerichtsKonfiguration`, `ExcelBerichtGenerator`,
`WordBerichtGenerator`, `ZeitreihenExtraktor`, `IBerichtsBaustein` — **bis iU7**), `Katalog/`,
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

**Meldungen und Oberflächenaufgaben über Haken.** Das Muster: ein `static Action<…>`-Feld hier,
belegt von `Program.Main` in der Anwendung, mit einer folgenlosen oder auf die Konsole
schreibenden Vorbelegung.

| Haken | Wofür | Vorbelegung |
|---|---|---|
| `Meldung.Zeigen` / `.Hinweis` / `.Warnung` / `.Warten` | Dialog statt `MessageBox.Show` bzw. Sanduhr | Konsole; `Warten` folgenlos |
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

Dieselben drei Projekte rechnet die CI (`.github/workflows/kern.yml`) auf `ubuntu-latest` und
`macos-latest`. 1007 und 1017 führen aktive Stromspeicher-Varianten und decken damit den
K8-Haken ab; ohne sie fiele ein stillgelegter Haken nicht auf.
