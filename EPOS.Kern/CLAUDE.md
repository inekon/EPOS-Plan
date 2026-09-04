# CLAUDE.md — `EPOS.Kern`, der Rechenkern

Der plattformfreie Kern von EPOS-Plan: **291 `.cs`-Dateien** (168 aus iU4, dazu
`IDatenzugriff`/`SqliteDatenzugriff` aus iU6, `ChartRenderer` aus iU7-5, die 22 Dienste-Dateien
aus iU5, `EnergietraegerVarianteCtrl` aus iU8-8b, die **74 Dateien des zweiten Umzugs**
iU5-U1…U5 und die sechs Dateien der Ergebnisseite aus iU9‑W11a), `net10.0` **ohne**
`-windows`, AnyCPU.
Seit Paket iU4 (03.09.2026) liegen sie physisch hier; bis dahin waren sie aus
`../WindowsFormsApplication1/` verlinkt. Seit Paket iU6 (03.09.2026) **ohne jeden Verweis
auf `System.Data.OleDb`** — weder im Quelltext noch als `PackageReference`; **CA1416 steht
bei 0**. Fachdomäne und Datenmodell stehen in der
[`CLAUDE.md` der Repo-Wurzel](../CLAUDE.md), die Windows-Anwendung in
[`../WindowsFormsApplication1/CLAUDE.md`](../WindowsFormsApplication1/CLAUDE.md).

**Die eine Regel: Eine Fachänderung am Rechenkern wird EINMAL gemacht — hier.** Die Anwendung
übersetzt diese Dateien nicht mehr mit, sie referenziert das Projekt.

```powershell
dotnet build ..\EPOS.Kern\EPOS.Kern.csproj -c Release   # 0 Fehler, 3 Warnungen
dotnet test  ..\WP-Plan.Kern.slnf -c Release            # 2 406 Tests (Stand iU9-W11a)
```

Die dritte Warnung ist mit `Controller\StromverbraucherStammCtrl.cs` aus der Anwendung
mitgewandert (CS0108, `items` verdeckt `StromverbraucherModel.items`) — sie ist nicht neu. Die
Gesamtzahl der Lösung liegt bei **34** (sie war 36, bis iU8-9 das Formular `Form_Kosten_Auswahl`
mit seinen beiden WFO1000 löschte).

## Was hier liegt

| Ordner | Inhalt |
|---|---|
| `Allgemein/` (20) | `BhkwPlan.cs` (der Rechenkern selbst, Namespace `WPPlan.Core`), Zugriffsschicht (`IDatenzugriff`, `SqliteDatenzugriff`, `DataRepository` als Fassade, `DbParam`, `DbVorgang`, `DbWerte`, `RecordSet`), `Meldung` (Melde-Haken), `Sprache`, `ZahlText`, `Zeilenumbruch`, `SolarPVGISCalculator`, `WizardItemClass` (Typ- und Nummernkatalog) — seit iU5-U5 dazu `ToolsClass`, `FileDlgClass`, `chart_test`, seit iU9-W6.1 `EmissionsVorgaben` (die Vorgabewerte der beiden Katalogeditoren, vorher dreimal im Oberflächencode; seit iU9‑W11a.5 zusätzlich die beiden SUBSTITUTIONSFAKTOREN der Autarkiekachel — `CO2_NETZSTROM_KG_JE_KWH` 0,42 und `CO2_WAERME_KG_JE_KWH` 0,20, wörtlich aus `DashboardForm.cs:355`, Befund W11‑B31), seit iU9-W9 `Ferienzeit` (die vier Ferienregeln des Gebäudekatalogs samt der Umrechnung Tag/Monat ↔ Jahrestag) und `Suchmuster` (die Platzhaltersuche, die zuvor zweimal wortgleich dastand) |
| `Allgemein/Simulation/` (28) | die vollständige Engine außer `SchemaModell.cs` — `SimulationControl` (beide `partial`-Hälften), `Kaskadenschleife`, `SimulationKanaele`, `Init`, `SimulationRunner`, die Module je Erzeuger/Bedarf, `WaermequelleClass`/`WaermesenkeClass`, `Warnkriterien`, `ProfilBedarf`, `StilleDb`. **Mit iU9‑W10a** kommen die Rechen- und Anzeigewege der sieben Simulationsdialoge dazu: `WaermesenkeClass.SenkeAnzeige`/`SENKE_LEER` (sie war eine STATISCHE Methode auf `Form_Waermesenke` mit drei fremden Aufrufern, Befund W10‑B22), `VDI4640Pruefung.Sondenmeter`/`Volllaststunden`, `ErdreichAuswertung.ErdreichLaufErgebnis`/`ErgebnisZuordnen` (die Zuordnung stand doppelt in Maske und Aufrufer, W10‑B8) und die **erzeugte** Datei `KlimazonenPfade.cs` — 15 Zonen als SVG-Pfade, gebaut von `../Werkzeuge/KlimazonenPfade/erzeugen.py`, weil der Vorläufer die Karte zur Laufzeit mit einem Regex aus einer eingebetteten SVG las (W10‑B5). **Mit iU9‑W11a** kommen `ErgebnisPraesenz` (war `internal` in `Views/Simulation/` und steuert fünf der sechs Ergebnismasken), `Ganglinie` (`Dauerlinie`/`Anzeigewerte` aus `GanglinienDarstellung`; `Stapeltyp`/`StapelEinstellen` arbeiten auf einer WinForms-`Series` und bleiben) und `LaufFortschritt` dazu. **`SimulationControl.Do_Simulation` nimmt seither `IProgress<LaufFortschritt>` und `CancellationToken` entgegen** — ohne die beiden Zusatzangaben unverändert; der Abbruch wird ZWISCHEN den fünf Phasen geprüft (Start, Kaskade, Photovoltaik, Stromspeicher, Abschluss). Eine Meldung je Erzeuger gibt der Rechenweg nicht her: Die Kaskade läuft stundenweise und bedient in jeder Stunde alle Erzeuger nacheinander. **Die vier EIGENANTEILE** (`SimulationRunner.EigenanteilWpMwh`/`…KesselMwh`/`…SolarKwh`/`…BhkwMwh`) und die zwei Ableitungen `RestNachEigenanteil`/`DeckungProzent` sind aus `BaueErgebnis` herausgezogen: Dieselben Ausdrücke standen wortgleich in `Form_Simulation_Detail` |
| `Allgemein/Wirtschaftlichkeit/` (20) | alle 20 Dateien — `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl`, die KWKG-/EEG-/Steuer-Rechner |
| `Allgemein/Bericht/` (13 + 4) | die **DATEN**-Hälfte: `BerichtTexte`, `BerichtsDaten`, `EmissionsAusweis`, `KostenEmissionRechner`, `ProjektDetails`, `KennzahlenKatalog`, `AbweichungsErmittler`; seit iU7-5 der **Renderer** `ChartRenderer` (seit iU9‑W10a mit `Jahresgang` — 1 304 × 440, zwei Reihen, Monatsachse 0…12, vorzeichenfähige y-Achse, für das Quelltemperaturbild des Erdreichdialogs); seit iU5-U3 die **AUSGABE** `WordBerichtGenerator`, `ExcelBerichtGenerator`, `IBerichtsBaustein`, `BerichtsKonfiguration`, `ZeitreihenExtraktor` und `Bausteine/` (4 Dateien) |
| `Allgemein/Dienste/` (22) | die **neun Umgebungsdienste** (iU5): `Dienste` (Halter), `IDialogDienst`, `IDateiDienst`, `IPfade`, `IEinstellungen`, `ILizenzAblage`, `IGeraeteId`, `ISprache`, `INavigation`, `IProjektKontext`, ihre Standardfassungen (`StilleDialoge`, `KeineDateiwahl`, `StandardPfade`, `FluechtigeEinstellungen`, `KeineAblage`, `KeineGeraeteId`, `StandardSprache`, `KeineNavigation`, `LeererProjektKontext`) und die sprachneutralen Schlüssel `Gewerke`, `Masken`, `Ansichten`, `Projektwahl` |
| `Allgemein/Update/` (5) | `Anlagenzeilen`, `ProjektPuffer`, `SchemaKatalog`, `SchemaStand` (Ergebniszustand der Migration und die DDL-Konstanten, die Controller zur Selbstanlage brauchen) — seit iU5-U5 dazu `AnlagenEindeutigkeit`, seit iU9‑W10a `ProjektPuffer.NutzbareKapazitaetKWh` (Volumen × 1,16 × Spreizung ÷ 1000 — die Formel stand in ZWEI Masken, Befund W10‑B12; die Leerregeln bleiben je Maske) |
| `Allgemein/Lizenz/` (4) | seit iU5-U1: `LizenzManager`, `LizenzToken` (Ed25519 über BouncyCastle), `LizenzServerClient`, `GeraeteId` — die Umgebung kommt über `Dienste.Lizenzablage`, `Dienste.Pfade`, `Dienste.Einstellungen` und `Dienste.GeraeteId` |
| `Allgemein/Import/` (13) | seit iU5-U1: `AnsiEncoding`, `CsvReader` (NReco, MIT), `GanglinienDatei` (CSV/TXT und Excel über ClosedXML), `CEC/` (3), `Pan/` (2), `VDI 3805/` (5 — Heizkessel, Pufferspeicher, Solarkollektoren, Wärmepumpen, `VdiAuswahlFilter`) |
| `Allgemein/Katalog/` (3) | seit iU5-U1: `DublettenPruefung`, `KatalogBereinigung`, `KatalogRegistry` |
| `Allgemein/Export/` (1) | seit iU5-U1: `CsvExportClass` |
| `Allgemein/KI/` (11) | seit iU5-U2 das, was der Assistent **weiß**: `HilfeWissen` (`WissensAbschnitt`), `WikiWissen`, `SemantikIndex`, `SemantikModell` (ONNX), `KiSchreibschutz`, `KiSicherungspunkt`, `KiEinwilligung`, `KiTextlieferant`, `Aktionen/KiAktionsTexte`, `Dialoge/KiDialoge`, `Dialoge/KiDialogTexte`. Was er **bedient**, bleibt bei der Oberfläche |
| `Allgemein/Hilfe/` (1) | seit iU5-U5: `DokuUebersetzung` (Wiki-URL durch den Übersetzungs-Proxy) |
| `Controller/` (91) | 91 Controller ohne Oberflächenbezug — 50 aus iU4, 29 aus iU5-U4, `EnergietraegerVarianteCtrl` aus iU8-8b (die Datenseite des ersten Blazor-Dialogs), `KostenfaktorCtrl` aus iU9-W1.5, `KostenSummenCtrl` aus iU9-W0.1 und `EnergietraegerPreisCtrl` aus iU9-W4.4 (die neun SQL-Anweisungen der Trägerkarte). **Mit iU9-W6 hat die Erzeugerseite ihre Datenseite bekommen:** `EnergietraegerVarianteCtrl.Anlegen`/`VariantenDerGruppe`/`TraegerUmhaengen` (die 185 Zeilen `CreateNewEnergyCarrier`, die ZWEIMAL wortgleich in der Oberfläche standen), die Katalogfilter und Detailblöcke in `HeizkesselStammCtrl`/`HeizkesselCtrl`/`BHKWStammCtrl`/`BHKWCtrl`/`PhotovoltaikStammCtrl`/`PufferSpStammCtrl`/`PufferSpCtrl` sowie die beiden Schreibeinstiege `Ueberschreiben`/`Anlegen` je Katalogeditor. **Mit iU9‑W7** kommen `WPCtrl` (Umzug), `WaermepumpeGeraeteCtrl` (die zweistufige Geräteauskunft Ä22) und die Datenwege der acht Wärmepumpen- und Solarmasken dazu: `WPStammCtrl.KatalogZeilen`/`GesperrtDurchProjekt`/`Speichern`, `KenndatenCtrl.Reihen`/`LiesStamm`/`Abgleichen` (transaktional), `KenndatenKuehlungCtrl.Reihen`/`HatKenndaten`, `WErzeugerCtrl.AnlagenzeileNachziehen`, `KostenSummenCtrl.AnlagenSumme`, `Z_ProjektSolarganglinieCtrl.LiesProjekt` und `SolarkollektorenStammCtrl.IdZu`/`ReadById`. **Mit iU9‑W8** kommen die drei Bedarfsblätter dazu: `BedarfStammCtrl` und `TypProfilCtrl` (neu — EINE Schnittstelle für drei Tabellen mit zwei verschiedenen Schlüsselspalten), die Schreibwege `ProzesswaermeStammCtrl.Exists`/`SaveHead`/`TypIsReadOnly`/`TypNew`/`TypDelete` (sie standen inline in zwei Masken) und die vollständige Gebäudetyp-Verwaltung in `TagVCtrl` (`Typen`, `Lies`, `Speichern`, `Anlegen`, `Loeschen`, `KurvenNamen`). **Mit iU9‑W10a** kommen `PufferSpStammCtrl.Katalogzeilen` (das inline-SQL auf `Tab_Pufferspeicher_STAMM`, das in der Maske stand, Befund W10‑B27) und die drei Serialisierungswege des Quellprofils dazu — `QuellprofilCtrl.MonatswerteParsen`, `MonatswerteText` und `WochenwerteParsen` (der Text `"t1;…;t12"` und die 168 Wochenwerte wurden im FORMULAR zerlegt, W10‑B21). **Mit iU9‑W11a** kommen vier Controller der Ergebnisseite dazu: `SimulationErgebnisCtrl` (sieben DTO je Erzeuger — die rund 600 Zeilen Fachrechnung, die in `Form_Simulation_Detail` standen), `SimulationLaufCtrl` (`Vorpruefen`/`Bedarf`/`Bestuecken`/`Laufen`/`Abbruchgrund`/`ErgebnisSpeichern` — der Lauf als Kernvorgang, Fehler als RÜCKGABE statt als Dialog), `SpeicherKennzahlenBlock` (die 39 Kennzahlzeilen des Stromspeichers samt `KennzahlStufe` statt vier `Color.FromArgb`) und `SpeicherAnzeigeCtrl` (`BetriebsartText`/`BerechnungsartText`/`AmortisationText` — sie standen dreifach im Oberflächencode). Erweitert sind `KonfigurationCtrl.LiesProjekt`/`ProjektLesen` (die achtmal string-konkatenierte Lesung von `Tab_Einstellungen`), `HeizkesselStammCtrl.BrennstoffartenJeProjekt`, `WErzeugerCtrl.AnlagenJeTyp`/`ModelleJeTyp`/`AnlagenBezeichner` und `StromspeicherStammCtrl.KapazitaetUndLeistung`/`KapazitaetJeProjekt` |
| `Model/` (47) | alle 47 Modelle; seit iU9‑W8 dazu der Aufzählungstyp `BedarfsArt` — er liegt hier und nicht in `EPOS.UI`, weil ihn BEIDE Seiten brauchen: Die Controller verteilen danach auf drei Tabellen, die Razor-Komponenten wählen danach ihre Beschriftungen |
| `MyResource/` | `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` — der Anzeigetext-Katalog beider Sprachen |
| `Properties/` | `Settings.settings`, `Settings.Designer.cs`, `Settings.cs` |

`RecordSet` ist seit iU6-T1 ein reiner vorwärtslaufender Zeilenzeiger: `DBCommand`, `_cmd`,
`MerkeSql()` und `Parameter()` sind ersatzlos gestrichen (iR8 — repositoryweit gab es **0**
externe Nutzer). Wer parametrisiert arbeiten will, nimmt `DataRepository` oder `DbVorgang`.

Verlinkt statt verschoben ist genau eine Datei: `../sql/schema/SchemaTypKatalog.g.cs` — ihre
Quelle ist `sql/tools/Erzeuge-Schema.ps1`, nicht dieses Projekt.

## Was mit Absicht NICHT hier liegt

Nach dem zweiten Umzug (iU5-U1…U5) sind es noch **62 Dateien** unter
`../WindowsFormsApplication1/Allgemein/` (42) und `../WindowsFormsApplication1/Controller/` (20).
Jede steht auf dieser Liste, weil der Kernbau sie ablehnt — nicht, weil sie übersehen worden wäre:

| Was | Warum |
|---|---|
| `BaseForm`, `Form_Hinweis` (+ Designer), `FensterEinpassung`, `SpeichernLeiste`, `GrafikTools/*`, `Hilfe/HilfeAutomatik`, `Hilfe/InfoKnopf`, `Hilfe/HelpCatalog` (mit `HelpExtender`) | Oberflächenbausteine — WinForms und GDI+ |
| `Blazor/BlazorDialogForm`, `Blazor/BlazorDienste`, `Hilfe/WindowsHilfeDienst` | die Blazor-Hülle selbst (iU8-6/iU8-7): ein modales `Form` mit `BlazorWebView`, sein Dienstverzeichnis und die Windows-Fassung von `EPOS.UI.Dienste.IHilfeDienst`. Sie **sind** die Oberfläche und können nie in den Kern |
| `Simulation/SchemaModell.cs` | Schema-**Ansicht**; ruft `Form_Waermesenke` |
| `Update/SchemaMigration`, `GeraeteWaisen`, `ErststartMigration`, `SchemaVersionAccess`, `DbParamOleDb` | der eingefrorene Access-Zweig — `System.Data.OleDb` |
| `Bericht/BerichtsDatenSammler` | `EnergieMengen` aus `Views/Varianten/` |
| `KI/KiDialogZugriff`, `KiAusfuehrer`, `HilfeKontext`, `KiAufrufKnopf` | greifen auf lebende `Control`/`Form` zu |
| `KI/KiChatService`, `KiAktionen` (trägt `KiHilfe`), `KiAktionenDialog`, `-Energie`, `-Lastgang`, `-Projekt`, `-Schreiben`, `-Sitzung`, `-Uebernahme`, `-Wirtschaft` | hängen an den vier obigen, an `HelpEntry` oder an `OleDbException` |
| `IAssistentRahmen`, `StromTestClass` | `WizardSeite` aus `Views/Wizard/` bzw. Testgerüst am Rechenweg |
| die 12 `*KontextMenuCtrl` | `ListView`/`ContextMenuStrip` |
| `KlimaregionStammCtrl` | `ComboBox`/`ListBox` in `FillComboBox`/`FillListBox` |
| `WizardCtrl`, `MenueCtrl` | `WizardParent` aus `Views/Wizard/` |
| `EnergietraegerKatalogCtrl` | `EnergyCarrier`, deklariert in `Views/Kosten/Form_Kosten.cs` |
| `PeakShavingCtrl`, `ProjektExportImportCtrl` | `OleDbException` bzw. `SchemaMigration` |

Ebenfalls dort, aber keine Quelldatei: `Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx`. Sie
wird über `<None Update … CopyToOutputDirectory>` neben die EXE gelegt, und genau dort sucht sie
`WordBerichtGenerator.FindeVorlage()` (`AppDomain.CurrentDomain.BaseDirectory`).

**Die `partial`-Falle.** Vor jedem weiteren Umzug prüfen, ob die Klasse noch eine zweite Hälfte
in der Anwendung hat. `SimulationControl` liegt mit beiden Hälften hier; `WPCtrl` lag mit beiden
dort, bis iU9‑W7.0a seine WinForms-Hälfte STRICH — `WPCtrl.WinForms.cs` trug genau eine Methode
(`FillListBox(ListBox)`), und die hatte im ganzen Bestand keinen Aufrufer. Erst danach konnte
die Klasse hierher; dazwischen gibt es nichts.

## Regeln für Änderungen hier

**Kein WinForms-Code, kein `System.Data.OleDb`.** `EnableWindowsTargeting=false` ist der
Wächter: Jede WinForms-Berührung bricht den Build sofort, nicht erst zur Laufzeit auf dem
iPad. `System.Data.OleDb` ist seit **iU6** ganz weg — kein `using`, kein Typ, keine
`PackageReference`. **CA1416 steht bei 0** (Verlauf 87 → 78 → 0);
**kein `NoWarn`**, damit eine neu hereingetragene Windows-API sofort als Warnung auffällt.

**Die Pakete des Kerns** — alle plattformfrei, Fassungen zentral in `Directory.Packages.props`:

| Paket | Wofür | Seit |
|---|---|---|
| `Microsoft.Data.Sqlite` | Zugriffsschicht | iU4 |
| `System.Configuration.ConfigurationManager` | `Properties\Settings` erbt von `ApplicationSettingsBase` | iU4 |
| `SkiaSharp` (+ die bedingten Nativen) | `ChartRenderer` | iU7-5 |
| `BouncyCastle.Cryptography` | Ed25519-Prüfung in `LizenzToken` | iU5-U1 |
| `ClosedXML` | Excel — lesend in `GanglinienDatei`, schreibend im `ExcelBerichtGenerator` | iU5-U1 |
| `Microsoft.ML.OnnxRuntime`, `Microsoft.ML.Tokenizers` | `SemantikModell` | iU5-U2 |
| `DocumentFormat.OpenXml` | `WordBerichtGenerator` | iU5-U3 |
| `SixLabors.Fonts` | Spaltenbreiten für ClosedXML; **auf 1.0.1 gepinnt** (ab 2.x gilt die Six-Labors-Split-Lizenz) | iU5-U3 |

Dazu zwei `ProjectReference`: `SpeicherEngine` (iU4) und `KiKern` (iU5-U2, UI- und DB-frei,
ohne eigene Pakete).

**Kein iOS-Sonderpaket mehr (iU10-1).** Bis iU10 stand hier eine bedingte `PackageReference` auf
`SQLitePCLRaw.bundle_green` für die Ziele `net10.0-ios`/`net10.0-maccatalyst`. Sie ist gestrichen:
Die Fassung 2.1.12 gibt es nicht (`bundle_green` endet bei 2.1.11, NU1102), `bundle_e_sqlite3`
lädt auf iOS ohnehin nichts dynamisch (`provider.internal`, statisch gelinkte `e_sqlite3.a`), und
die System-SQLite des Geräts wäre für die **114 STRICT-Tabellen** der Datenbank nicht steuerbar.
Der Kern bekommt auch kein zweites `TargetFramework` — die iOS-Hülle `EPOS.iOS` referenziert ihn
als `net10.0`-Bibliothek und zieht ihre Nativen selbst.

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

## Bericht: alles hier bis auf den GDI+-Stand

**Der Diagramm-Renderer liegt seit iU7-5 hier** — `Allgemein/Bericht/ChartRenderer.cs`,
SkiaSharp statt GDI+ (iU7-2), ohne eine einzige Windows-API. Er ist die Vorlage für iF16
(`EPOS.UI/Standards/ChartBild`): Der Kern liefert PNG-Bytes, die Oberfläche zeigt sie an —
ein Chart-Stack für Bericht *und* Bildschirm.

**Er zeichnet seit iU9-W3.4 auch für EINGABEMASKEN.** `ChartRenderer.Kostenprofil` (samt der
Palettenfarbe `C_PROFIL`) ist die erste neue Methode seit der SkiaSharp-Portierung: das aus
zwölf Monatsniveaus und 168 Wochenwerten konstruierte Jahresprofil (8 760 Stunden) über einer
Monatsachse 0…12, Bildmaß **1296 × 780** — die doppelte Zielauflösung des abgelösten
WinForms-Chart aus `Form_Kostenprofil` (648 × 390). Die y-Achse ist **vorzeichenfähig** wie
beim Kapitalwert-Verlauf und aus demselben Grund: Ein Wochenwert ist eine *Abweichung* und
darf den Monatswert unter null ziehen; die Nulllinie wird dann gestrichelt hervorgehoben. Der
Dialog dazu ist `EPOS.UI/Dialoge/Kosten/KostenprofilDialog.razor`, gerechnet wird in
`Views/Kosten/KostenprofilHuelle.cs` (`PreisModell.AusMonatsUndWochenwerten` + Renderer, beides
in `Task.Run`). Damit trägt der Weg „Diagramm im Kern zeichnen, in der Oberfläche nur das PNG
zeigen" auch außerhalb des Berichts.

**Seit iU9‑W7.0c zeichnet er die WÄRMEPUMPEN-KENNLINIEN.** `ChartRenderer.Kennlinien`
ist die zweite Methode für eine EINGABEMASKE: COP bzw. Leistung über der
Außentemperatur, eine Linie je Vorlauftemperatur, Bildmaß **968 × 520** (die doppelte
Zielauflösung des breitesten der vier abgelösten WinForms-Charts, 484 × 195, plus
130 px für die Legende — sie steht hier UNTER dem Diagramm statt darin, weil sie bei
acht Reihen die Linien verdeckte). Punktmarken wie im Vorläufer: Kreis für den COP,
Kreuz für die Leistung. Die x-Achse trägt echte Temperaturen statt
Stützstellennummern — zwei Vorlauf-Kennlinien müssen nicht dieselben
Außentemperaturen haben; die „schöne" Achsenstufung ist dafür aus
`KapitalwertVerlauf` als `Stufe(ref min, ref max)` herausgezogen. Die Datenseite
liefern `KenndatenCtrl.Reihen` und `KenndatenKuehlungCtrl.Reihen` als **ein**
`KennlinienSatz` mit beiden Reihenlisten. Die Dialoge dazu sind
`EPOS.UI/Dialoge/Waermepumpe/WaermepumpeStammDialog.razor` und
`…/WaermepumpeAnlageDialog.razor`, gezeichnet wird in den Hüllen
`Views/Wärmepumpe/WaermepumpeStammHuelle.cs` (`BilderZu`).

**Seit iU9‑W8.0c zeichnet er die BEDARFSBILDER.** Drei Methoden lösen die neun
`Chart`-Steuerelemente der zehn Bedarfsmasken ab (Bausteinlücke 12):
`ChartRenderer.MonatsSaeulen` (978 × 542 — die doppelte Zielauflösung des
größten Vorläufers 489 × 271; x-Achse starr 1…12, y ab 0),
`ChartRenderer.Stundenprofil` (1244 × 464 — EIN Bild für 168 Wochenstunden UND
24 Tagesstunden; der Unterschied Fläche/Linie der beiden Vorläufer war keine
Entscheidung, sondern die Voreinstellung zweier Diagrammverwalter) und
`ChartRenderer.Jahresverlauf` (978 × 542, 8 760 Stunden über Monatsgrenzen —
OHNE den Mausrad-Zoom des Vorläufers, weil ein PNG nicht spreizen kann).
Die „schönen Schritte" der y-Achse sind wörtlich aus `SkaliereYAchse`
übernommen und eine ANDERE Reihe als beim Kapitalwert-Verlauf
(0,1/0,2/0,25/0,5/1/2/2,5/5/10 statt 1/2/2,5/5/10): Bedarfswerte brauchen auch
Zehntel. Die Dialoge dazu stehen in `EPOS.UI/Dialoge/Bedarf/`, gezeichnet wird
in den drei Hüllen unter `Views/Bedarf/`.

**Seit iU9‑W11a.6 zeichnet er die ERGEBNISBILDER der Simulation.** Sieben Methoden
lösen die **17 Zeichenflächen** der sechs Ergebnismasken ab:
`ChartRenderer.GanglinieNormiert` (1240 × 560 — ein bis vier Linien, alle auf DENSELBEN
Höchstwert normiert, y-Achse 0…100,2 % wie `init_Chart`, x wahlweise Monatsgrenzen oder
die vier Stundenmarken 2000/4000/6000/8000),
`ErzeugerStapel` (1240 × 560 — **das Arbeitspferd**: es trägt SECHS der siebzehn Flächen.
Zwei Stapelgruppen wie `StackedGroupName` im Vorläufer, Linien darüber in
Zeichenreihenfolge, die Konturlinie „Gesamt" UNTER dem Stapel, sortiert ohne Stapel, eine
Reihe auf einer zweiten y-Achse),
`Streuwolke` (1240 × 560 — halbtransparente XY-Punkte über einer vorzeichenfähigen
x-Achse), `Ring` (720 × 560 — Kuchen mit Innenloch, Zahl in der Mitte und einer Legende,
die nur Segmente > 0 nennt), `MonatsStapel` (978 × 542) und `Temperaturverlauf`
(1240 × 560 — gestrichelte Zwillingsreihe je Speicher, y-Achse OHNE Nullpunkt mit einer
Mindestspanne von 5 K). `Reihe` trägt dafür seit W11a.6 `Stapelgruppe`, `Gestrichelt` und
`Breite`; der alte Konstruktor ist unverändert.

**Die vier BERICHTSBILDER bleiben unangetastet.** `JahresverlaufWaerme` und
`DauerlinieWaerme` sind zwei feste Ausprägungen von `ErzeugerStapel`,
`StrombilanzMonate`/`MonatsSaeulen` zwei von `MonatsStapel`, `Speichertemperaturen` eine
von `Temperaturverlauf` — sie nehmen aber einen `ZeitreihenSatz` und tragen feste deutsche
Titel im Quelltext. Ihre Zusammenführung mit den neuen ist ein eigener Schritt mit eigenem
Nachweis (offener Punkt W11a‑O‑3), keine Nebenarbeit.

**Die AUSGABE liegt seit iU5-U3 ebenfalls hier:** `WordBerichtGenerator` (OpenXML),
`ExcelBerichtGenerator` (ClosedXML), `IBerichtsBaustein`, `BerichtsKonfiguration`,
`ZeitreihenExtraktor` und `Bausteine/`. Word und Excel sind Dateiformate, keine Windows-APIs —
der Bericht entsteht damit auch auf dem iPad. In der Anwendung blieb nur
`BerichtsDatenSammler`, weil er `EnergieMengen` aus `Views/Varianten/` ruft. Der eingefrorene
GDI+-Stand `ChartRendererGdi` und der Modus `bildvergleich` der Referenzlauf-Suite sind mit
Entscheid **iF23** am 03.09.2026 gelöscht — der Anwender hat die Löschung ohne den
Windows-Bildvergleich angeordnet; die Berichtskette hat keine GDI+-Stelle mehr.

**Die Fußzeilen-Fassung des Word-Berichts.** `Bausteine/BausteineStandard.cs` las die
Programmfassung bis iU5-U3 über `System.Windows.Forms.Application.ProductVersion`. An ihrer
Stelle steht jetzt `DeckblattBaustein.ProduktFassung()` mit derselben Reihenfolge wie WinForms:
`AssemblyInformationalVersionAttribute` des **Einstiegs**-Assemblies, sonst
`FileVersionInfo(...).ProductVersion` derselben Datei, sonst `"1.0.0.0"`. Der Bestand nimmt den
zweiten Zweig — die Anwendung setzt `GenerateAssemblyInfo=false` und deklariert nur
`AssemblyVersion`/`AssemblyFileVersion` `1.1.0.0`; das Deckblatt zeigt unter Windows deshalb
unverändert `1.1.0.0`.

**Die Vorlage bleibt neben der EXE.** `WordBerichtGenerator.FindeVorlage()` sucht
`Vorlagen\Berichtsvorlage.docx` über `AppDomain.CurrentDomain.BaseDirectory` — die `.docx`
selbst liegt weiterhin im Anwendungsprojekt und wird von dort ins Ausgabeverzeichnis kopiert.

**Die Dateiwahl der Berichtsansicht läuft seit iU7-9 über `Dienste.Datei`** —
`OrdnerWaehlen`, `DateiSpeichern` und `MitSystemOeffnen` statt `FolderBrowserDialog`,
`SaveFileDialog` und `Process.Start` (`Views/Bericht/UcBericht.cs`,
`Views/Varianten/Form_Variantentest.cs`).

**Schriftregel iF19 — Systemschrift, flexibel.** Der Renderer bindet keine Schrift ein,
sondern fragt `SKFontManager` eine Rückfallkette ab: Calibri (Windows) → Carlito/Liberation
Sans/DejaVu Sans (Linux) → Helvetica/Arial (macOS/iOS). Das Layout ist **metrikgetrieben**:
Umbrüche und Legendenbreiten folgen den gemessenen Textmaßen, nicht festen Pixelwerten.
Folge, und das ist Absicht: **Textbreiten dürfen je Plattform abweichen.** Ein Vergleich
Windows↔Linux ist deshalb ein Struktur- und Histogrammvergleich, kein Pixelvergleich; ein
Pixelvergleich wäre nur *innerhalb* einer Plattform sinnvoll (das tat der Modus
`bildvergleich` gegen den GDI+-Stand — beide mit iF23 gelöscht).

**Nachweis in drei Stufen.** `EPOS.Kern.Tests/ChartRendererTests.cs` (iU7-8) prüft die
Verdichtungen exakt und dass gezeichnet wird — seit iU9-W3.4 fünf Tests (die zwei neuen
sichern Maß und Determinismus des Kostenprofils), in jedem Kern-Lauf dabei.
`Proben/ChartProben` (eigene `.sln`, referenziert dieses Projekt) zeichnet seit iU9‑W11a.6 **dreißig** Bilder und
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

**Seit iU9-W6 prüft `EPOS.Kern.Tests` auch SCHREIBENDE Wege — mit Datenbank.** Bis dahin
galt dort ausschließlich, was ohne Datenbank entscheidbar ist. Mit Welle 6 sind jedoch
Schreibwege aus der Oberfläche hierher gewandert, deren Ausgang darüber entscheidet, ob
ein Erzeuger aufgenommen wird (`EnergietraegerVarianteCtrl.Anlegen`, vier Ausgänge); der
Referenzlauf sieht davon nichts, weil er einen BESTEHENDEN Projektstand nachrechnet.
`EPOS.Kern.Tests/TestDatenbank.cs` legt je Testklasse eine Arbeitskopie von
`Referenzlaeufe/Kenndaten_Test.sqlite` an und biegt `DataRepository.PfadUeberschreibung`
darauf um — dasselbe Vorgehen wie `EPOS.Referenzlauf`, damit die Vergleichsbasis
unberührt bleibt. Fehlt die Datei, schweigen die Fälle statt rot zu werden. Alle Klassen
dieser Art tragen `[Collection("Testdatenbank")]`: `PfadUeberschreibung` ist statisch, und
xunit fährt Testklassen sonst nebeneinander.

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

**Der Plattform-Wächter — muss ebenfalls leer bleiben:**

```bash
git grep -nE 'System\.Windows\.Forms|System\.Drawing|MessageBox\.|\bProgram\.|\bRegistry\.|ProtectedData|OleDb' \
    -- 'EPOS.Kern/*.cs' | grep -vP ':\s*(///|//|\*)'
```

`\bRegistry\.` mit Wortgrenze — ohne sie trifft das Muster `speicherRegistry.` in
`SimulationControl.cs` und meldet zwölf falsche Treffer.
