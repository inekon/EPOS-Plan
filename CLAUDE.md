# EPOS-Plan — Projektkontext

EPOS-Plan ist die Planungs- und Simulationssoftware von INEKON für Energie- und
Wärmeversorgungskonzepte: Wärmebedarf, Brauchwasser, Prozesswärme, Heizkessel, BHKW,
Wärmepumpe, Solarthermie, Photovoltaik, Wärme- und Stromspeicher, Klimadaten — mit
Herstellerdaten-Import, Simulation, Berichten und Wirtschaftlichkeitsrechnung. Ein
plattformfreier Rechenkern und eine Razor-Oberfläche laufen in zwei Schalen: einer
Windows-Anwendung (`EPOS_Plan.exe`) und einer iOS-App (MAUI Blazor Hybrid).

Diese Datei beschreibt den **gültigen Stand** und die Arbeitsregeln. Sie führt keine
Geschichte: Was war, warum es geändert wurde und wie es geworden ist, steht unter
[`Dokumentation/ueberholt/`](Dokumentation/ueberholt/). Je Projekt gibt es eine weitere
`CLAUDE.md` mit Einzelheiten: [`EPOS.Kern`](EPOS.Kern/CLAUDE.md), [`EPOS.UI`](EPOS.UI/CLAUDE.md),
[`EPOS.iOS`](EPOS.iOS/CLAUDE.md), [`WindowsFormsApplication1`](WindowsFormsApplication1/CLAUDE.md).
Antworten, Bezeichner und Kommentare auf Deutsch.


## Modellwahl und Agenten

- **Fable 5.1 nur, wenn es unbedingt nötig ist:** Es schneidet Aufträge, wählt das Modell,
  nimmt Ergebnisse ab, entscheidet bei widersprüchlichen Befunden, bereitet Anwenderentscheide
  vor und berichtet — in knappen Zügen. Alles andere wird delegiert, auch Merges und
  Konfliktlösung, Gates, CI-Prüfung und CI-Vermerke, Status- und Protokollpflege, Hüllen,
  Nachzüge und Konzeptabsätze; Fable fasst Dateien nur an, wenn ein Agentenaufruf teurer wäre
  als die Änderung selbst (eine Zeile, ein Verweis). Geplante Check-ins (Routinen) lösen
  einen Agenten aus, statt Fable arbeiten zu lassen.
- **Für jede delegierte Aufgabe das geeignete, günstigste Modell wählen** — das spart Token
  und Zeit:

  | Modell | Aufgaben |
  |---|---|
  | `model: opus` (Opus 5.5, `opus-umsetzung`) | Rechenweg, Schema, Tests, Hüllen und Dialoge, Fehlersuche im Code, Konfliktauflösung mit Fachinhalt, Konzeptabsätze mit Fachinhalt, Referenzprojekte und Basen einfrieren |
  | `model: sonnet` (`sonnet-mechanik`) | Inventare und Suchen, Status-, Protokoll- und Indexpflege nach Vorlage, Ressourcen in beiden Sprachen, Wiki-Quellen und Logbuch-Entwürfe, Gate fahren und Zahlen melden, Merges ohne Fachkonflikt, CI-Protokolle auswerten, Vergleichstabellen aus vorliegenden Zahlen |
  | `model: haiku` (`haiku-pruefung`) | Zählungen, BOM-, Encoding- und Zeilenendenprüfungen, Konfliktmarker, LFS-Zeiger, Dateilisten |
  | `model: fable` | nur, wenn die Orchestrierung selbst eine schwierige Analyse in einem frischen Kontext braucht |

  Das Modell bei jedem Agentenaufruf **ausdrücklich** setzen, nie erben lassen.
- **Agentenaufträge** sind vollständig und repo-relativ formuliert (keine absoluten Pfade —
  sie lenken Worktree-Sitzungen in den Hauptbaum), nennen das Ziel, die Abnahme (Build,
  Tests, Referenzlauf) und die Regeln dieser Datei, die gelten. Agenten arbeiten im eigenen
  Worktree oder in klar abgegrenzten Dateien, committen ihren Stand sofort auf ihrem Zweig,
  pushen nicht und lösen keinen CI-Lauf aus. Ihr Bericht enthält Befund und Ergebnis, keine
  Dateiabzüge.
- Unabhängige Agenten und Werkzeugaufrufe parallel starten; Ergebnisse abnehmen, indem alle
  plausiblen Schreiborte geprüft werden (Hauptbaum, Worktree, Commits — auch Sync-Commits).
- **Aufwand nach Aufgabe:** Die Kosten einer Welle wachsen mit Runden × Kontextgröße. Deshalb
  die Agentendefinitionen unter `.claude/agents/` nutzen (`opus-umsetzung` mittlerer, `sonnet-mechanik`
  und `haiku-pruefung` niedriger Denkaufwand), einen Auftrag so schneiden, dass ein Agent mit
  höchstens rund 150 Werkzeugaufrufen auskommt (sonst in Teilaufträge mit frischem Kontext zerlegen),
  Agenten kein vollständiges Gate fahren lassen — sie bauen, prüfen den SQL-Dialekt und laufen die
  betroffenen Tests mit `--filter`; das Gate fährt die Orchestrierung einmal nach dem Merge —, und
  lange Läufe mit genau einem wartenden Befehl im Hintergrund abwarten statt das Protokoll wiederholt zu lesen.
- **Vor Agentenarbeit im Hauptbaum** die Datei `AGENT_LAEUFT` in der Repowurzel anlegen
  (Auftrag, Sitzung, Beginn; sie steht in `.gitignore`) und **nach der Abnahme löschen**.
  `GitHub_Sync.bat` bricht ab, solange sie liegt — so wandert kein halbfertiger Stand in
  einen Sync-Commit. Eine liegen gebliebene Datei ohne laufenden Agenten wird gelöscht.


## Aufbau des Repositoriums

| Projekt / Ordner | Inhalt | Regel |
|---|---|---|
| `EPOS.Kern` | Rechenkern (`net10.0`, ohne Windows-Bindung): Simulation, Wirtschaftlichkeit, Modelle, Datenzugriff (`IDatenzugriff`/`SqliteDatenzugriff`), Schema-Migration, Bericht samt Diagramm-Renderer, Lizenz, Import, Katalog, Export, KI-Wissen, Controller | **Jede Fachänderung wird einmal gemacht — hier.** Die Umgebung erreicht der Kern nur über die neun Schnittstellen in [`EPOS.Kern/Allgemein/Dienste/`](EPOS.Kern/Allgemein/Dienste/) (Dialog, Datei, Pfade, Einstellungen, Lizenzablage, GeräteId, Sprache, Navigation, Projektkontext). `Program.*`, `MessageBox`, `Registry`, DPAPI und `SpecialFolder` sind hier verboten; die zwei Wächter in `EPOS.Kern/CLAUDE.md` bleiben leer |
| `EPOS.UI` | Razor-Klassenbibliothek: Seiten (`Seiten/AppWurzel.razor` als gemeinsame Wurzel beider Plattformen, `Hauptfenster.razor`, `Start/Startseite.razor`), Dialoge, Bausteine, Standards, `wwwroot` | **Keine Datenbank in der Oberfläche.** Jeder neue oder ohnehin anzufassende Dialog ist eine Razor-Komponente; Datenbankseite in einen Controller des Kerns, Texte in `MyResource.Resource.*` (beide Sprachen). Das Menü ist Daten: Quelle ist `Menuetabelle.cs`, kein Untermenü mit nur einem Punkt |
| `EPOS.UI.Daten` | Die Hüllen: bauen aus Kern-Controllern die DTO der Razor-Seiten; plattformfrei (`EnableWindowsTargeting=false`) | Was die Plattform beisteuern muss, kommt als benannte Naht herein (`SimulationPlattformwege`, `Katalogwege`); was eine Plattform nicht kann, wird benannt abgelehnt, nie still übergangen |
| `WindowsFormsApplication1` | Die Windows-Schale: Assembly/Prozess **`EPOS_Plan`**, `net10.0-windows`, **x64**, SDK `Microsoft.NET.Sdk.Razor`. `Hauptfensterrahmen` (`Views/Hauptformular/`) trägt eine `BlazorWebView`; `Program.Main` belegt die `Dienste.*` und registriert den Flottenplaner | Keine Fachmaske, kein Inline-SQL. Hier steht nur, was Windows braucht (WebView2-Laufzeit, Verknüpfungen, Hilfefenster). Namensraum bleibt `WindowsFormsApplication1` |
| `EPOS.iOS` | Die iOS-Schale: MAUI-App mit einer Seite und einer `BlazorWebView` auf `AppWurzel`, neun `Dienste.*`-Adapter, Seed-Kopie der Datenbank, Prüfmodus für die CI | Eigene Projektmappe `EPOS.iOS/EPOS.iOS.sln`, bewusst nicht in `WP-Plan.sln`; baut **nur auf macOS** (`ios.yml`). Nichts Fachliches |
| `SpeicherEngine`, `SpeicherPlanung` | Mehrspeicherrechnung: Flottenphysik, Verteilung, Wirtschaftlichkeit, Rastersuche (Engine, ohne Datenbank und Oberfläche); MILP-Fahrplaner mit Google OR-Tools (Planung) | **`Google.OrTools` hängt nur an `SpeicherPlanung`**, und nur die Windows-Schale referenziert es. Kern, UI, Engine und iOS kennen allein `IFlottenPlaner`; ohne registrierten Planer sind `PvPlanung`, `Arbitrage`, `MultiUse` benannt nicht verfügbar, `PvGreedy` und `PeakShaving` rechnen überall |
| `KiKern` | Kern des KI-Assistenten (Gemini-Zugang, Aufgabensteuerung) | Schlüssel liegen beim Anwender (DPAPI bzw. Schlüsselbund), nie im Repository |
| `EPOS.Referenzlauf`, `Referenzlauf` | Rechennachweis gegen die eingefrorene Basis: plattformfrei (`lauf`, `vergleich`) bzw. Windows-Suite (`lauf`, `projekt`, `vergleich`, `pruefen`, `liste`, `migration`) | Siehe „Regressionsnetz“ |
| `Werkzeuge/`, `Proben/` | Werkzeuge und Prüfstände, siehe Tabelle unter „Bauen und prüfen“ | Eigene Projektmappen, teils in der CI |
| `Setup/` | Inno-Setup-Kette (`build-setup.ps1`): Veröffentlichung win-x64, Auslieferungsvorlage, Installer | Läuft in der CI nur auf Zuruf (`windows.yml`, Schalter „setup“) |
| `Referenzlaeufe/` | Testdatenbank `Kenndaten_Test.sqlite` (Git LFS), aktuelle Referenzbasis, Skripte | [`Referenzlaeufe/LIESMICH.md`](Referenzlaeufe/LIESMICH.md) |
| `Dokumentation/` | Alle Markdown-Papiere: `aktuell/` gilt, `ueberholt/` ist Geschichte | Index [`Dokumentation/LIESMICH.md`](Dokumentation/LIESMICH.md) |
| `Lizenzserver/`, `Projekte/`, `Quellen/`, `VDI-3805-Daten/`, `sql/` | Lizenzserver-Plugin, Wiki-Quellen und Referenzpakete, Fremdquellen, Herstellerdaten (LFS), SQL-Skripte | `Projekte/Speichersimulation/code/` ist Referenz, kein Werkzeug: nicht gebaut, in keiner CI |

Projektmappen: `WP-Plan.sln` (Windows-Anwendung, alle Bibliotheken, Werkzeuge) und der
Filter `WP-Plan.Kern.slnf` (die plattformfreien Projekte samt Tests — Grundlage der CI).


## Datenhaltung: SQLite

- **Eine Datei, Kataloge und Projektdaten zusammen:** `Kenndaten.sqlite` unter
  `%ProgramData%\EPOS_PLAN` (Windows) bzw. `Library/Application Support/WP-Plan/EPOS_PLAN`
  (iOS). Eine Neuinstallation bekommt sie aus der Vorlage `{app}\Vorlage\Kenndaten.sqlite`,
  die der Kern beim ersten Start kopiert (`EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs`).
  Betrieb, Sicherung (`VACUUM INTO` im laufenden Betrieb), Wiederherstellung und Werkzeuge:
  [`BETRIEB_SQLITE.md`](Dokumentation/aktuell/BETRIEB_SQLITE.md).
- **Schema:** `Tab_*` Stamm- und Projektdaten, `Tab_*_STAMM` Auslieferungskatalog (`ReadOnly`
  = gehört zur Auslieferung), `Z_*` Zuordnung Projekt ↔ Katalog. Fachtabellen sind `STRICT`.
  **Neue Beziehungen über IDs**, nicht über Textfelder. Schemaänderungen laufen als
  nummerierte Schritte über `SchemaMigration`
  ([`ADR-001`](Dokumentation/aktuell/ADR-001_Schema-Ausrollung.md)); die nächste Nummer meldet jede Sitzung **vor dem Bau** in der
  Zeile „Schemaschritt angemeldet" im Kopf der Statusdatei an und pusht allein diese Zeile sofort —
  eine angemeldete Nummer vergibt keine andere Sitzung, die Kette hängt immer über `+ 1` an der
  Vorgängerklasse, und beim Push der Welle wird die Zeile auf die nächste freie Nummer gesetzt; der Rechenkern arbeitet
  mit festen Rastern (8760 Stunden, 168 Wochenstunden, 365 Tage, 12 Monate, kein Schaltjahr).
- **SQL-Dialekt:** Regeln in BETRIEB_SQLITE.md Abschnitt 6 (Umlautregel, Verbotsliste der
  Access-Schreibweisen, Boolean-Spalten als 0/1 — neue Spalten mit `CHECK (spalte IN (0,1))`, Sortierung über `IIF`/`CASE`). Zugriffe über `DataRepository` mit
  `?`-Parametern, nie mit zusammengesetzten SQL-Texten. Nach jeder neuen oder geänderten
  SQL-Anweisung den `SqlDialektPruefer` ziehen.
- **Testdatenbank** `Referenzlaeufe/Kenndaten_Test.sqlite` ist die einzige Datenbank im
  Repository (Git LFS) und zugleich Messlatte für Tests, Referenzlauf und CI. Sie darf nur
  mit aktivem LFS-Filter committet werden.
- Datenbankkopien und -sicherungen (`*.sqlite`, `*.accdb`) gehören nie ins Repository;
  Sicherungen liegen in `DB-Backup/` neben der Datenbank des Anwenders.
- Brauchwasser-/TWW-Profile nach VDI 6002:
  [`KONTEXT_Brauchwassertypen_VDI6002.md`](Dokumentation/aktuell/KONTEXT_Brauchwassertypen_VDI6002.md).


## Bauen und prüfen

SDK-Fassung aus `global.json` (10.0.400), gemeinsame Eigenschaften in `Directory.Build.props`,
Paketversionen in `Directory.Packages.props`.

```powershell
dotnet build WP-Plan.sln -c Debug -p:Platform=x64          # Windows-Anwendung samt allem
dotnet build WP-Plan.Kern.slnf -c Release                  # nur die plattformfreien Projekte
dotnet test  WP-Plan.Kern.slnf -c Release --no-build -- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2
dotnet run --project Proben/ChartProben -c Release          # Diagramm-Renderer ohne Windows
dotnet run --project EPOS.Referenzlauf -c Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017,1045,1046,1047,1049,1051 --ziel <ordner>
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich <basis> <neu>
```

- Der Anwender-Build liegt unter `WindowsFormsApplication1\bin\x64\Debug\net10.0-windows\EPOS_Plan.exe`.
  **Eine laufende Anwendung oder ein offenes Visual Studio sperrt diesen Ordner**; MSB3027
  nennt die sperrenden Prozesse. Der Hauptbaum wird nicht gebaut, solange ein Agent darin
  Dateien ändert — dann den committeten Stand in einem Worktree oder Nebenordner bauen.
- Die Windows-Schale baut auch auf Linux:
  `dotnet build WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Debug -p:Platform=x64 -p:EnableWindowsTargeting=true`
  (0 Fehler, Warnungen Bestand). Wer eine Hülle oder Naht der Schale anfasst, prüft sie so
  kompiliert, bevor der Auftrag abgenommen wird — der Kern-Filter sieht diese Dateien nicht.
- Testsammlungen laufen **nicht parallel** (Kulturpinnung in vielen Testklassen), deshalb die
  xUnit-Schalter oben; das Gate und beide Workflows nehmen dieselben. Alle fünf Testprojekte
  (`EPOS.Kern.Tests`, `EPOS.UI.Tests`, `KiKern.Tests`, `SpeicherEngine.Tests`,
  `SpeicherPlanung.Tests`) tragen dieselben Werte als `xunit.runner.json` — Läufe ohne
  Schalter sind damit reihenfest, die Schalter bleiben.
- Alle fünf Testprojekte laufen unter der Standardkultur **en-US** (`StandardkulturEnUs.cs`
  je Projekt, wie der Windows-Läufer). Tests mit deutschen Ressourcentexten oder Zahlformaten
  pinnen de-DE — in `EPOS.Kern.Tests` und `EPOS.UI.Tests` mit der `Kulturvorrichtung`, in den
  drei übrigen threadgebunden mit Rückstellung —, sonst sind sie rot; kein Lauf unter de-DE
  ist mehr ein Nachweis. Ein neues Testprojekt bekommt beide Dateien.
- Ein roter Build kann fremd sein: Fehler nach Dateien aufschlüsseln, bevor man ihn sich
  zuschreibt. Quelltexte: `.cs`, `.csproj`, `.resx` UTF-8 **mit** BOM und CRLF; Markdown
  UTF-8 **ohne** BOM (`.editorconfig`). Ältere Dateien können noch Windows-1252 ohne BOM
  sein — vor dem Bearbeiten die Bytes messen und byte-erhaltend schreiben.
- **Absturz der Windows-Anwendung ohne Meldung** (Ereignisanzeige: `KERNELBASE.dll`,
  `0xc000041d`): Die Ursache steht im Ausnahmeprotokoll `Logs\Ausnahmen.txt` neben der
  Datenbank — zuerst diese Datei anfordern; Vorgehen in
  [`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md) unter „Fallstricke“.
- Die Python-Werkzeuge der Tabelle unten laufen auf Windows über den Starter `py` (`python3`
  gibt es dort nicht), mit `PYTHONIOENCODING=utf-8` davor, weil sie Unicode ausgeben.
  `Proben/ChartProben` und `EPOS.Referenzlauf` stehen **nicht** im Kern-Filter — wer sie mit
  `--no-build` laufen lässt, baut sie vorher ausdrücklich, sonst laufen alte Binaries.

**Werkzeuge, die vor der Arbeit an Maske, Rechenweg oder Auslieferung zu kennen sind:**

| Werkzeug | Wofür | Aufruf |
|---|---|---|
| `Proben/ChartProben` | zeichnet alle Diagrammbilder aus synthetischen Reihen und prüft Maße, Farben und Determinismus, mit Gegenproben; rot, sobald der Renderer eine Windows-API braucht oder sich ein Bild ändert | `dotnet run --project Proben/ChartProben -c Release` |
| `Proben/Rasterprobe` | misst die virtualisierte `Katalogliste` (QuickGrid `Virtualize`) im echten Browser — Zeilenhöhe, Abstandshalter, Rollbehälter, Sichtbarkeitsmelder. **Vor jeder Änderung an `Raster`, `Katalogliste` oder den `.epos-raster*`-Regeln ziehen**; bunit allein misst das nicht. Dazu `fensterprobe.mjs`: Kopf und Schlussleiste der Dialoge im eigenen Fenster (haften, Fokus, Überlagerung, Katalogdialog, mit Gegenprobe) — **vor jeder Änderung an Dialogkopf, Schlussleiste oder dem Abschnitt „Dialog im eigenen Fenster" des Hausblatts ziehen**; `bannerprobe.mjs`: der Meldungsbanner im eigenen Fenster haftet unter dem Kopf (mit Gegenprobe) — **vor jeder Änderung an `Warnbanner` oder seiner Haftregel ziehen** | siehe [`Proben/Rasterprobe/LIESMICH.md`](Proben/Rasterprobe/LIESMICH.md) |
| `EPOS.Referenzlauf` | plattformfreier Rechennachweis gegen die eingefrorene Basis (Linux, macOS, CI) | `dotnet run --project EPOS.Referenzlauf -- lauf …` / `… vergleich <ref> <neu>` |
| `Referenzlauf` (Windows) | die vollständige Suite (`lauf`, `projekt`, `vergleich`, `pruefen`, `liste`, `migration`) | `Referenzlauf.exe <modus> …` |
| `Werkzeuge/ResourceDesigner` | erzeugt `EPOS.Kern/MyResource/Resource.Designer.cs` aus der neutralen `.resx`; wiederholbar. **Nach jedem neuen Ressourcenschlüssel ziehen** | `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben` (ohne Argument: nur prüfen) |
| `Werkzeuge/Standardlastprofile` | leitet aus der BDEW-Excel unter `Quellen/Standardlastprofile/` die Konstanten `EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs` ab; wiederholbar | `py Werkzeuge/Standardlastprofile/ableiten.py schreiben` (ohne Argument: nur prüfen) |
| `Werkzeuge/Auslieferungsvorlage` | erzeugt aus einer produktiven `Kenndaten.sqlite` die bereinigte Auslieferungsdatenbank samt Prüfbericht und schreibt das Katalogpaket der Fassung (`Katalogpaket.json`) daneben. **Vor jeder Auslieferung ziehen** | `dotnet run --project Werkzeuge/Auslieferungsvorlage -c Release -- <quelle> <ziel> [--beispiele …] [--katalogfassung <n>] [--trocken]` |
| `Werkzeuge/Berichtsvorlage` | bereinigt die Word-Stilvorlage des Berichts (doppelte Stile, Format „EPOS Kapitelkopf“) und baut daraus die Beispielvorlage mit Platzhaltern und mit `--standard` die ausgelieferte Standardvorlage `Berichtsvorlage_Standard.docx`, mit `kurzbericht` den ausgelieferten Kurzbericht je Sprache, alles nur bei grünem `OpenXmlValidator`; `BerichtsvorlageDateiWacheTests` und `AuslieferungsvorlagenWacheTests` halten die Dateien und beide Lieferwege. **Nach jeder Änderung an einer der Vorlagen ziehen**; Einzelheiten in [`LIESMICH.md`](Werkzeuge/Berichtsvorlage/LIESMICH.md) | `dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- bereinigen <docx>` / `… beispiel <quelle.docx> <ziel.docx> [--standard] [--katalogfassung <n>]` / `… kurzbericht <quelle.docx> <ziel.docx> --sprache de\|en` |
| `Werkzeuge/ZapfprofilValidierung` | hält den Zapfprofilgenerator gegen **gemessene** Reihen: je Objekt eine Ampel nach den Abnahmekriterien der Stufe Z5 (Band der Dauerlinie, Formabgleich, Energie nach Kalibrierung), dazu die √N-Skalierung über alle Objekte; Bericht in Markdown und CSV. Datenbankfrei, der Katalog kommt aus einem Paketordner oder einer SQLite. **Die Messreihen liegen nie im Repositorium** — eine Wache im Werkzeug hält jeden Absolutwert und jede Mengeneinheit aus dem Bericht; Ablage und Konverter für offen lizenzierte Fremddaten in [`LIESMICH.md`](Werkzeuge/ZapfprofilValidierung/LIESMICH.md) | `dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- <ordner> --ziel <berichtordner> [--katalog <sqlite\|paketordner>] [--realisierungen N] [--seed S] [--trocken]` |
| `Werkzeuge/SqlDialektPruefer` | hält jeden SQL-Text des Bestands mit `EXPLAIN` gegen die Testdatenbank und die Verbotsliste | `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite` |
| `Werkzeuge/Formularkarte` | Feldkarte einer WinForms-Maske aus Designer und `.resx` samt Razor-Sektionsskelett; ihre Tests laufen in `kern.yml` | `dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>` |
| `Werkzeuge/Testdatenbankschema`, `Werkzeuge/KlimazonenPfade` | Schemawerkzeug der Testdatenbank; Klimazonenkarte (`Zonenkarte_Klimazonen.svg`) erzeugen | `dotnet run --project Werkzeuge/Testdatenbankschema`; `python3 Werkzeuge/KlimazonenPfade/erzeugen.py` |


## Regressionsnetz

**Die Abnahme ist der Vergleich gegen die Basis, nicht die Meinung.** Jede Änderung am
Rechenweg wird gegen die aktuelle Basis unter `Referenzlaeufe/` gehalten (gegenwärtig
`2026-10-07_R42_Vorlaufinterpolation_AK3`, dreiundzwanzig Projekte; die Photovoltaik bilanziert je Viertelstunde, die Gebäude rechnen nach VDI 6007 und laufen
ohne wirksame Kühlung frei, Projekt 1017 rechnet Kälte und deckt sie mit einer Wärmepumpe im
Kühlbetrieb, Projekt 1047 rechnet als Kopie von 1017 mit Anlagenkopplung AK1 — Heizkreis und
Kühlübergabe gekoppelt —, beide rechnen ihren Strombedarf mit der gepflegten Jahressumme ihrer
Stromverbraucher-Zuordnung (über die ID, gehalten von `EPOS.Kern.Tests/StromverbraucherZuordnungTests`),
Projekt 1045 rechnet sein Brauchwasser über den Zapfprofilgenerator,
gehalten von `EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests`, Projekt 1049 rechnet als
Kopie von 1018 ein Kollektorfeld vor BHKW und Kessel, das direkt und über den Puffer deckt —
mit der Nachrang-Vorgabe 30 % am Puffer und der Arbeitstemperatur aus der untersten Zone des
Puffers, gehalten von `EPOS.Kern.Tests/SolarWaermeMonateTests` —,
1018 und 1049 rechnen ihr BHKW mit der unteren Grenzleistung ihres Anlagenfelds (Rangfolge Anlage,
Katalog, Projekt, gehalten von `EPOS.Kern.Tests/BhkwLeistungsgrenzeTests`),
Projekt 1050 rechnet als Kopie von 1023 einen Brennwertkessel mit gepflegter Kennlinie,
Brennwertkennlinie am Rückfall-Rücklauf und gepflegten Taktwerten, gehalten von
`EPOS.Kern.Tests/KesselKennlinieTests`, und steht nicht in der CI-Auswahl,
die Gebäude rechnen Bauteile am Erdreich mit dem Erdreichwiderstand nach DIN EN ISO 13370,
Projekt 1051 rechnet als Kopie von 1007 einen Bürobau mit Konditionierungskalendern (Nachtzeile der
Lüftung, Ferien, Heizperiode) und Aufheizoptimierung, gehalten von
`EPOS.Kern.Tests/KonditionierungReferenzprojektWacheTests`, Projekt 1052 rechnet als Kopie von 1018 das Hotel in
drei Zonen mit Zonenkalendern, gehalten von `EPOS.Kern.Tests/ZonenReferenzprojektWacheTests`, und steht nicht in
der CI-Auswahl,
Projekt 1054 rechnet als Kopie von 1052 mit Anlagenkopplung AK1, Radiator und Heizkurve am Gebäude und einer Zone mit eigener Übergabe (Konvektor 70/50 °C, Proportionalband 2 K) — die Übergabe je Zone am gemeinsamen Vorlauf, Rücklauf massenstromgewichtet —, gehalten von `EPOS.Kern.Tests/ZonenHeizkreisReferenzprojektWacheTests`, und steht nicht in der CI-Auswahl,
Projekt 1055 rechnet als Kopie von 1017 seine Kälte mit einer Kältemaschine mit Trocken-Rückkühler und eigenem Zähler und einem Kältespeicher (die Wärmepumpe heizt nur), gehalten von `EPOS.Kern.Tests/KaeltemaschineReferenzprojektWacheTests`, und steht nicht in der CI-Auswahl,
Projekt 1056 rechnet als Kopie von 1047 mit Anlagenfahrplan — Nachtsperre der Wärmepumpe 0 bis 6 Uhr, Zeitprogramm 0 in denselben Stunden an Kessel und BHKW, `Vorlauf_Max` 50 °C — und schreibt Komfortstunden, gehalten von `EPOS.Kern.Tests/FahrplanReferenzprojektWacheTests`, und steht nicht in der CI-Auswahl,
Projekt 1057 rechnet als Kopie von 1029 eine Erdsonde (4 × 90 m, Mergel/Lehm, Klimazone 6) mit Entzugsrückwirkung im Betrachtungsjahr 10, gehalten von `EPOS.Kern.Tests/ErdsondeReferenzprojektWacheTests`, und steht nicht in der CI-Auswahl,
Projekt 1058 rechnet als Kopie von 1056 auf Stufe AK3 mit Heizungspuffer an der Wärmepumpe und Raumeinfluss der Heizkurve — der geschlossene Kreis zwischen Gebäude und Erzeugern, der Puffer überbrückt die Sperrstunden — und schreibt Komfortstunden und die Kennzahlen des Kreises, gehalten von `EPOS.Kern.Tests/Ak3ReferenzprojektWacheTests`,
die Sole-Wärmepumpen der Referenzprojekte rechnen mit Erdreichquelle (Erdsonde), deren Entzug ohne Taktstrom
und deren Prüfung je Anlage mit Erdreichquelle, die Wärmepumpen rechnen ihre Kennlinie über den Vorlauf interpoliert,
1047 und 1056 weisen die Vorlaufwahl ihrer Wärmepumpe aus, 1051 und 1052 weisen ihre Auslegungsheizlast aus,
allein Projekt 1040 dauerhaft auf dem Tagesbilanz-Weg (der Altweg bleibt wählbar), gehalten von `EPOS.Kern.Tests/GebaeudeRueckwegTests`;
Aufbau, Herleitung und Schemastand in
[`Referenzlaeufe/LIESMICH.md`](Referenzlaeufe/LIESMICH.md)). Die CI rechnet die Projekte
1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051 und 1058; Toleranz: Betrag ≥ 1 relativ 1e‑4, sonst absolut 0,01;
der Byte-Vergleich ist nur Information.

**Einfrierregeln** — wer eines davon ändert, friert im selben Schritt die Basis neu ein und
begründet den Wechsel in `Referenzlaeufe/LIESMICH.md`:

- gesäte Emissionsfaktoren der Testdatenbank (`emissionsart`, aktive `emissionswert`,
  `Tab_Brennstoff_Stamm.CO2/SO2/NOx/Staub` und die Projektkopien `Tab_Brennstoff` der
  Referenzprojekte, `energy_project_settings.co2/so2/nox`, Berechnungsmodus eines Referenzprojekts);
- gesäte PV-Modulkoeffizienten (`alpha_SC`, `beta_OC`, `gamma_PMP`, `T_NOCT`) oder ein neues
  Modul, das ein Referenzprojekt benutzt;
- der Flottenstand `@Projektflotte` des Projekts 1046 in `Tab_SpeicherAuslegung` und dessen
  Projektzeilen;
- gesäte Gebäudedaten: `Tab_Gebaeude(_STAMM)` mit `Bauweise`, U-Werten, Flächen, Sollwerten,
  `Luftwechselrate`, `Fensterdurchlassgrad`, `Gebaeude_Modell` und den übrigen Spalten des
  Gebäudemodells, die Gebäudezuordnungen der Referenzprojekte, die Konditionierung ihrer Gebäude
  und Zonen (übernommene Vorlagen sind Kopien am Gebäude) und
  das Anlegen oder Entfernen eines ihrer Gebäude;
- gesäte Kältedaten: der Projektschalter `Tab_Einstellungen.Kuehlbetrieb` eines Referenzprojekts,
  die Kühleingaben seiner Gebäude (`Kuehlung_Aktiv`, `Kuehl_Sollwert`, `Kuehl_Sollwert_Nacht`,
  `Kuehlleistung_Max`), der Kanal „Kühlung“ eines seiner Lastgänge und sein Kälteerzeuger: der
  Kaskadenplatz der Wärmepumpe, ihr Kühlbetrieb, `Kuehl_Vorlauf` und `Kuehl_Hilfsstromanteil`,
  Kühlträger und Abrechnungsart ihrer Anlagenzeile (`Kuehl_ID_Carrier`, `Kuehl_EigenerZaehler`)
  und die Kühlkennlinie des Projektgeräts samt Vorlauf-Stützstellen (`Tab_Kenndaten_Kuehlung`);
- gesäte Kältemaschinendaten eines Referenzprojekts: die Anlagenzeile seiner Kältemaschine (`Kaeltemaschine_Anzahl`, `Kuehl_ID_Carrier`, `Kuehl_EigenerZaehler`), ihre Projektkopie `Tab_Kaeltemaschine` (Nennkälteleistung, EER, Rückkühlart, Mindestteillast, Hilfsstrom der Rückkühlung, `Kuehl_Vorlauf`, `Kuehl_Hilfsstromanteil`) samt `Tab_Kenndaten_Kaeltemaschine`, sein Kältespeicher (Pufferzeile mit Verwendung Kälte, Volumen, Temperaturpaar `Vorlauf`/`Ruecklauf`, Bereitschaftsverlust, Schwellen) und die Kaskade (`Tool_1` bis `Tool_6`), dazu das Anlegen oder Entfernen eines solchen Referenzprojekts;
- gesäte Auslegungsdaten der Übergabe: die Kopplungsstufe `Tab_Einstellungen.Anlagenkopplung`
  eines Referenzprojekts (auch die Stufe AK3), an seinen Gebäuden `Heizkreis_Aktiv` und die Übergabespalten (Art,
  Exponent, Nennleistung, Auslegungspunkt, Heizkurve, `Heizkurve_Raumeinfluss`, `Regler_Proportionalband`,
  `Sollwertprofil`), `Kuehluebergabe_Aktiv`, die Spalten `Kuehl_Uebergabe_*` und
  `Kuehl_Auslegung_*` und `Kuehl_Vorlaufgrenze`, an seinen Zonen die sieben Übergabespalten von `Tab_Zone` (`Uebergabe_Art`, `Uebergabe_Exponent`, `Uebergabe_Leistung_Nenn`, `Auslegung_Vorlauf`, `Auslegung_Ruecklauf`, `Auslegung_Raumtemperatur`, `Regler_Proportionalband`), die Kaskade eines gekoppelten Referenzprojekts
  (`Tab_Einstellungen.Tool_1` bis `Tool_4`, sie entscheidet, ob die Wärmepumpe am gerechneten
  Vorlauf Wärme liefert), ihr Anlagenfahrplan in `Tab_Energieanlagen` (Sperrzeit `Sperrung`, `Sperrzeit_von`,
  `Sperrzeit_bis` samt `Tab_Sperrfenster`, `Zeitprogramm`, `Vorlauf_Max`), die Heizungspuffer eines gekoppelten
  Referenzprojekts (Volumen, Temperaturpaar `Vorlauf`/`Ruecklauf`, Schwellen, Lade- und Entladeleistung, Senkenzuordnung
  in `Z_AnlageSenke`, Lade-Prioritäten der Erzeuger), die Heizkennlinie `Tab_Kenndaten` seiner Projektwärmepumpe samt
  Vorlauf-Stützstellen, `Ptherm` seiner Projektkopien `Tab_Heizkessel` und `Tab_BHKW`, dazu das Anlegen oder Entfernen eines
  gekoppelten Referenzprojekts;
- gesäte Zapfprofil-Eingaben eines Referenzprojekts: `Tab_TwwProjekt` (`Weg`, Seed,
  Realisierungen, Temperaturen, Bilanzgrenze), seine Zonen (`Tab_TwwZone`) und Wohnungstypen,
  die Katalogzeilen (`Tab_Tww*_STAMM`), die sie benutzen, und das Umstellen eines
  Referenzprojekts auf den Generator;
- gesäte Solardaten des Referenzprojekts 1049: das Kollektorfeld (Kollektorsatz in
  `Tab_Solarkollektoren` samt `Kdfu` und `Bezugsflaeche`, Modulanzahl, Neigung, Azimut, Senken in
  `Z_AnlageSenke`, die Felder des Solarkreises `Arbeitstemperatur_Weg`, `Uebertrager_Graedigkeit_K`,
  `Kollektor_Spreizung_K`, `Solarkreisverluste_Prozent`, `Pumpenleistung_W`), sein Puffer
  (Volumen, Temperaturpaar `Vorlauf`/`Ruecklauf`, `Schwelle_Aus`, `Schwelle_Aus_Nachrang` leer),
  die Lade-Prioritäten der Erzeuger an diesem Puffer und die Kaskade (`Tool_1` bis `Tool_4`),
  dazu das Anlegen oder Entfernen eines Referenzprojekts mit Solarthermie;
- gesäte Kesseldaten: `Tab_Einstellungen.Kessel_Heizgrenze` und `Kessel_Betriebsbereitschaft`
  eines Referenzprojekts, die Bereitschaftsleistung seines Kessels, dessen fünf Kennlinienspalten
  (`Wirkungsgrad_Teillast30`, `Kennlinie_Brennwert`, `Mindestleistung`, `Anfahrverlust_kWh`,
  `Mindestlaufzeit_min`), sein Schalter `Brennwert` und die Bauart in `Beschreibung`, die über die
  Normvorgaben von η₃₀ und Mindestleistung entscheiden, beim Referenzprojekt 1050 dazu, was den Rücklauf seiner
  Brennwertkennlinie bestimmt (Temperaturpaar an Anlage und Kessel, Senken des Kessels,
  Kopplungsstufe), und das Anlegen oder Entfernen des Referenzprojekts 1050;
- gesäte Bedarfsdaten eines Referenzprojekts: seine Zuordnungszeilen für Stromverbraucher, Brauchwasser und
  Prozesswärme (`Z_Projekt_Stromverbraucher`, `Z_Projekt_Brauchwasser`, `Z_Projekt_Prozesswaerme` — ID der
  Projektkopie und gepflegte `Summe`), die zugeordneten Projektkopien samt Typsätzen (`Tab_Stromverbraucher` und
  `Tab_Stromverbrauchertyp`, `Tab_Brauchwasser` und `Tab_Brauchwassertyp`, `Tab_Prozesswaerme` und `Tab_Prozesstyp`)
  und das Anlegen oder Entfernen einer solchen Zuordnung;
- gesäte BHKW-Grenzleistungen der Referenzprojekte: das Anlagenfeld `Tab_Energieanlagen.Grenzleistung`
  ihrer BHKW-Zeilen, die Katalogspalte `Tab_BHKW.Grenzleistung` ihrer Projektmodule und der Projektwert
  `Tab_Einstellungen.Leistungsgrenze`;
- gesäte Stromfelder der Referenzprojekte: die Einspeisegrenze `Tab_Einstellungen.Einspeisegrenze_Wert` und
  `Einspeisegrenze_Einheit`, an den Projektkopien ihrer Stromspeicher `Standby_Verbrauch` und
  `Selbstentladung_Prozent_Monat`;
- gesäte Zonendaten eines Referenzprojekts: seine Zonen in `Tab_Zone` (jede Spalte), ihre Bauteile in `Tab_Bauteil`
  samt Fläche, U-Wert, Neigung, Azimut, Randbedingung, ψ·L, Aufbau und den Trennflächen (`ID_Nachbarzone`,
  `Trennflaeche_Zuordnung`), die Luftströme in `Tab_Zonenluftstrom`, die Zonenkalender (Vorgabezeilen, Kalender und
  Perioden mit `ID_Zone`) und die Aufheizvorgabe des Projekts (`Tab_Einstellungen.Aufheizoptimierung` und
  `Aufheiz_*`), dazu das Anlegen oder Entfernen eines Zonen-Referenzprojekts;
- gesäte Konditionierungsdaten eines Referenzprojekts: der Referenzkatalogbau „Referenzbau Konditionierung“ (Bauwerte,
  `Kuehlung_Aktiv`, `Sommerlueftung`, die Kalender mit Perioden und Vorgabezeilen — Ferien, Heizperiode, Nachtzeile der
  Lüftung mit `Bedingt_K`), das Gebäude von 1051 (alle Gebäudezellen, Zuordnung, manuelle Aufheizzeit), an
  `Tab_Einstellungen` die Aufheizvorgabe (`Aufheizoptimierung`, `Aufheiz_*`), die Ferien, die Heizperiode und der
  Schalter `Kuehlbetrieb`, dazu das Anlegen oder Entfernen eines Referenzprojekts mit Konditionierung;
- gesäte Erdreichdaten der Referenzgebäude: Grundfläche und `k_Wert_Grundflaeche` sowie die U-Werte der Bauteile am
  Erdreich, die Randbedingung der Grundfläche, das Umfangsfeld `Abmessung_Anschluß_Außenwand_Kellerdecke` und die
  Wände am Erdreich (Neigung 45° bis 135°); dazu die Festwerte λ_Erd, w und R_se und die Abschnittsobergrenze 8 in
  `GebaeudeFestwerte`;
- gesäte Erdreichquellen der Referenzprojekte: an ihren Wärmepumpen-Anlagen in `Tab_Energieanlagen` die Quellfelder
  `WQ_Typ`, `WQ_Quellsystem`, `WQ_Tiefe`, `WQ_Anzahl`, `WQ_Flaeche`, `WQ_Bodentyp`, `WQ_Spreizung`; die Klimazone
  `Tab_Klimaregion.Klimazone_DIN4710` ihrer Klimaregion; der Bodenkatalog (λ, ρ·c_p) der benutzten Bodentypen; die
  Sondenfeldspalten derselben Anlagen (`WQ_Sondenabstand`, `WQ_Bohrlochdurchmesser`, `WQ_Bohrlochwiderstand`,
  `WQ_Kopfueberdeckung`, `WQ_Betrachtungsjahr`, `WQ_Sondenanordnung` — leer heißt Vorgabe); die Normgeometrie von
  `Sondenfeldgeometrie` samt den Festwerten in `Erdsondenfeld` (Abstand 6 m, Bohrlochradius 0,075 m, R_b 0,10 m·K/W,
  Kopfüberdeckung 2 m, Betrachtungsjahr 10, Heizgrenze 15 °C der Startschätzung, quadratische Anordnung); dazu das
  Anlegen oder Entfernen eines Referenzprojekts mit Erdreichquelle.

Frühere Basen liegen nicht mehr im Repository; ihre Protokolle stehen unter
[`Dokumentation/ueberholt/Referenzbasen/`](Dokumentation/ueberholt/Referenzbasen/LIESMICH.md).
Gerechnet wird ausschließlich gegen die aktuelle Basis.


## CI und Läufer-Kontingent

| Workflow | Läuft von selbst | Nur auf Zuruf (*Actions → Run workflow*) |
|---|---|---|
| [`kern.yml`](.github/workflows/kern.yml) | bei jedem Push und Pull Request auf **ubuntu**: Bau und Tests des Filters, Werkzeugtests, SQL-Dialekt-Prüfer, ChartProben mit Hash-Messlatte (jede Zeile der jüngsten `Proben/ChartProben/Messlatte_*.sha256` muss unverändert in der Liste des Laufs stehen; Vergleich nur auf ubuntu, Liste als Artefakt), Referenzlauf der neun Projekte der CI-Auswahl gegen die Basis. Ein neuer Lauf desselben Zweigs bricht den überholten ab; Änderungen nur unter `Projekte/Wiki/`, `Dokumentation/aktuell/Mockups/`, `Quellen/`, `Lizenzserver/` lösen keinen Lauf aus | Häkchen „macos“: zusätzlich auf macOS (**zählt zehnfach**) |
| [`windows.yml`](.github/workflows/windows.yml) | Job `build-test` bei Push auf `main` und nächtlich 03:00 UTC auf dem Arbeitszweig `ios_migration_september` (der geplante Lauf nimmt den Workflow von `main` und checkt den Arbeitszweig aus; **zählt doppelt**); Pushes auf Arbeitszweige lösen ihn nicht aus, der Kern-Lauf auf ubuntu prüft sie | Häkchen „setup“: Job `installer` baut das Installationsprogramm (rund 4 Minuten, Installer als Artefakt) |
| [`ios.yml`](.github/workflows/ios.yml) | nie | baut die iOS-Hülle auf `macos-26`, startet sie im Simulator und rechnet Projekt 1030 gegen die Basis; 15–20 Minuten, **zählt zehnfach** |

**Regeln:**

- **Vor jedem Aufruf eines macOS-Läufers (`ios.yml`, `kern.yml` mit „macos“) beim Anwender
  nachfragen — jedes Mal, ohne Ausnahme.** Dasselbe gilt für den Setup-Lauf.
- Ein iOS-Lauf ist nur begründet, wenn die Änderung **die iOS-Hülle selbst** trifft
  (`EPOS.iOS/`, `ios.yml`, eine `Dienste.*`-Schnittstelle mit iOS-Adapter, Prüfmodus,
  Seed-Kopie) oder der Anwender ihn verlangt. Änderungen an Kern, Oberfläche, Testdatenbank
  oder Doku prüft `kern.yml` auf ubuntu — der grüne Kern-Lauf ist der Nachweis.
- Vor einem Aufruf prüfen, ob derselbe Stand schon läuft oder lief
  (`gh run list --workflow ios.yml --limit 3`); ein abgebrochener Lauf ist kein Nachweis.
- Ergebnisse token-sparsam lesen: `gh run view <id> --log-failed` statt des ganzen
  Protokolls; von Artefakten nur `protokoll.txt` und den Vergleich, nicht das App-Paket.
- Die Workflows holen aus LFS gezielt nur die Testdatenbank (Actions-Cache); allein der
  Setup-Job zieht alles.


## Git

- Remote `origin` = `github.com/inekon/EPOS-Plan`, Standardzweig `main`, **Arbeitszweig
  `ios_migration_september`**. Der dauerhafte Stand steht in
  [`Status_iOS_Migration.md`](Dokumentation/aktuell/Status_iOS_Migration.md) — eine Zeile je
  Schritt; der ausführliche Block dazu ist ein Protokoll unter `Dokumentation/ueberholt/Protokolle/`.
  Das Konzept dahinter ist
  [`Umsetzungskonzept_iOS_EPOS-Plan.md`](Dokumentation/aktuell/Umsetzungskonzept_iOS_EPOS-Plan.md),
  die Nachweise stehen in [`Umsetzung_iU10_Nachweise.md`](Dokumentation/aktuell/Umsetzung_iU10_Nachweise.md).
- **Git LFS:** `Referenzlaeufe/Kenndaten_Test.sqlite` und `VDI-3805-Daten/**/*.zip|*.vdi|*.VDI`
  liegen in LFS. Einmal je Rechner `git lfs install`; eine Zeigerdatei von 130 Byte statt der
  Datenbank ist ein Einrichtungsfehler, kein Repofehler.
- **Die Git-Geschichte ist am 12.09.2026 einmal umgeschrieben worden.** Jede Commit-Kennung
  aus einem Papier von davor ist eine alte Kennung und wird über
  [`commit-map_2026-09-12.txt`](Dokumentation/ueberholt/Geschichte/commit-map_2026-09-12.txt)
  übersetzt. Jeder Rechner klont neu; aus einem alten Klon wird nie gepusht, nie mit Force.
- **Der Anwender synchronisiert selbst** mit [`GitHub_Sync.bat`](GitHub_Sync.bat): `add -A`,
  Commit „Synchronisation vom …“, Pull (Merge), Push des aktuellen Zweigs. Folge: Alles, was
  im Arbeitsbaum liegt, wird binnen Minuten committet und veröffentlicht — auch halbfertige
  Agentenstände. Deshalb: Agenten in zusammenhängenden Schritten arbeiten lassen, ihre Stände
  sofort committen, nichts halbfertig liegen lassen; vor dem Hauptbaum-Build `origin` mergen.
  Das Skript bricht ab, solange `AGENT_LAEUFT` liegt, ein Merge oder Rebase offen ist, Pfade
  unaufgelöst sind oder Konfliktmarker in den Änderungen stehen.
- **Regeln für Claude:** kein Commit und kein Push ohne Auftrag; beauftragte Commits sofort,
  atomar und mit genauen Pfaden (`git add <pfad>`, nie `-A`); Betreff kurz (höchstens
  72 Zeichen), Einzelheiten im Rumpf; Trailer mit dem arbeitenden Modell, etwa
  `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>` oder `… Claude Opus 5.5 …`;
  keine Pull Requests, kein Tag-Push. Reihenfolge einer Welle: **Merge → Gate → Statuszeile und Protokoll →
  Push (auf Zuruf) → iOS-Lauf (nur nach Rückfrage) → Nachweis.**
- Nach Runden mit parallelen Sitzungen repoweit nach Konfliktmarkern suchen (`^<{7}`, `^={7}$`,
  `^>{7}`) und `UU`-Einträge in `git status` prüfen; beide Seiten inhaltlich zusammenführen.
- Token-sparsame Git-Befehle: `git status --short`, `git diff --stat` vor jedem vollen Diff,
  `git log --format='%h %<(72,trunc)%s' -n 20` statt `--oneline` (die Betreffzeilen sind
  lang), `git show --stat`; nie `git log -p` ohne Pfad und Grenze.


## Token-sparsam arbeiten

- Große Papiere nie ganz lesen: Überschriften mit `grep -n '^#'`, Statusblöcke und Anker
  gezielt mit `grep -n` suchen, dann nur den Abschnitt lesen. Das gilt besonders für
  `Umsetzungskonzept_iOS_EPOS-Plan.md`, `Referenzlaeufe/LIESMICH.md` und die vier
  Projekt-`CLAUDE.md`.
- Build- und Testausgaben kürzen: `dotnet build … -nologo -v q -clp:ErrorsOnly`,
  `dotnet test … --logger "console;verbosity=minimal"`; nur bei Rot die Einzelheiten.
- Bevorzugt `WP-Plan.Kern.slnf` bauen und testen; die volle `WP-Plan.sln` nur, wenn die
  Windows-Schale betroffen ist. Tests mit `--filter` auf die betroffene Klasse einschränken,
  den vollen Lauf als Gate am Ende.
- Suchen und Zählen an einen `sonnet`-Agenten geben, statt Dateien in den Hauptkontext zu
  holen; Ergebnisse als Befund, nicht als Abzug.
- Keine Wiederholung von Fakten, die diese Datei oder der Index schon nennt; keine
  Geschichte in Antworten — der gültige Stand genügt.


## Dokumentation

- **Alle Markdown-Papiere liegen unter [`Dokumentation/`](Dokumentation/LIESMICH.md):**
  `aktuell/` ist die Arbeitsgrundlage (lesen und fortschreiben), `ueberholt/` die Geschichte
  (nie Regelquelle; bei Widerspruch gilt `aktuell/`). Ein neues Konzept entsteht in
  `aktuell/` mit Indexzeile; ist sein Gegenstand umgesetzt oder abgelöst, wandert es im
  selben Schritt per `git mv` nach `ueberholt/`. Protokolle entstehen gleich unter
  `ueberholt/Protokolle/`. Die Wache `EPOS.Kern.Tests/DokumentationLinkWacheTests` prüft
  jeden relativen Verweis — auch in dieser Datei —, jede Indexzeile und dass in der Wurzel
  nur `CLAUDE.md` und `README.md` liegen.
- **Diese Datei beschreibt nur den gültigen Stand.** Keine Datums-, Entscheid- oder
  Protokollvermerke („seit …“, „vorher …“, Auftrags- und Wellenkürzel); was sich geändert
  hat, steht in der Statusdatei, im Protokoll und in `ueberholt/`.
- **Wiki (`wiki.epos-plan.de`):** Die Seiten der Rubrik „Programm Dokumentation“ — und
  sinngemäß alle Hilfe- und Grundlagenseiten — beschreiben ausschließlich die Funktion, so wie
  sie jetzt ist. **Änderungskommentare gehören nur in die Seite „Update-Logbuch“**, dort
  mit Datum und Text („Seit 01.09.2026 gilt …“, „… wurde hinzugefügt“, „… ist nicht mehr
  vorhanden“), geordnet nach Version (neueste oben). Tabu auf Fachseiten: „seit …“,
  „bisher/früher/vorher“, „Anwenderentscheid“, „Befund“, Auftrags-, Wellen- und
  Commit-Kürzel, „in Umsetzung, Stand …“ — auch nicht in HTML-Kommentaren. Zu jeder
  veröffentlichten Funktionsänderung einen Logbuch-Eintrag vorschlagen und die Versionsnummer
  beim Anwender erfragen; **Einträge knapp: ein Satz je wesentlicher, sichtbarer Änderung,
  ohne Einzelheiten und Begründung; Kleinigkeiten bekommen keinen Eintrag (Regel: Konzept
  Hilfesystem 13.4)**. Wiki-Entwürfe vor dem Veröffentlichen mit
  `seit (dem|der|W)|geändert|Entscheid|Befund|W\d+[a-z]?[‑-][A-Z][‑-]\d+|Stand:? *\d|bisher|früher|vorher|Bis dahin|Migrationsschritt`
  gegenlesen.
  **Keine Hersteller- und Produktdaten im Wiki:** kein Herstellername, keine Typbezeichnung,
  keine Kennwerte, Preise oder Datenblattangaben eines konkreten Produkts — Beispiele tragen
  neutrale Namen mit runden Werten („Speicher 1, 100 kWh"); Datenquellen, Normen und Formate
  (VDI 3805, CEC-Liste, PVsyst-Formate) dürfen genannt werden. Der Wächter
  `EPOS.Kern.Tests/WikiProduktdatenWacheTests` hält die Repo-Quellen gegen die Katalognamen
  der Testdatenbank; Regel und Prüfung: Konzept Hilfesystem, Abschnitt 13.
  **Veröffentlichung gebündelt:** Repo-Quellen werden je Auftrag fortgeschrieben und geprüft;
  ins Wiki geladen wird höchstens einmal je Woche, gesammelt für alle seither geänderten
  Seiten. Früher nur bei einer wesentlichen Änderung: eine neue oder geänderte Bedienung, die
  ein Anwender schon in Händen hat, ein neuer Rechenweg oder eine Aussage, die nicht mehr
  zutrifft. Ausstehende Uploads stehen in der Statusdatei; die Logbuch-Einträge werden mit dem
  Auftrag entworfen und mit dem Upload veröffentlicht (Regel: Konzept Hilfesystem 13.3).
  Repo-Quellen der Bedienungsseiten: `Projekte/Wiki/*.wiki`; Konzept und
  Zuordnung der Hilfe: [`Konzept_Hilfesystem_Wikidokumentation.md`](Dokumentation/aktuell/Konzept_Hilfesystem_Wikidokumentation.md).
- Weitere Einstiege: Lizenzierung
  [`EPOS-Plan_Konzept_Lizenzierung.md`](Dokumentation/aktuell/EPOS-Plan_Konzept_Lizenzierung.md),
  Setup [`Konzept_Setup_InnoSetup_EPOS-Plan.md`](Dokumentation/aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md),
  Simulationsablauf [`Konzept_Simulationsablauf_EPOS-Plan.md`](Dokumentation/aktuell/Konzept_Simulationsablauf_EPOS-Plan.md),
  Mehrspeicher [`Doku_Mehrspeicher_Konzept_und_Umsetzung.md`](Dokumentation/aktuell/Doku_Mehrspeicher_Konzept_und_Umsetzung.md),
  Entscheidungsregister iOS [`Entscheidungsregister_iOS_EPOS-Plan.md`](Dokumentation/aktuell/Entscheidungsregister_iOS_EPOS-Plan.md).


## Aufräumen

Ins Repository gehören keine Arbeitsordner (`.work/`), keine Datenbankkopien, keine
Sicherungskopien von Quelltexten (`*.bak`, `*.orig`), keine Spikes. Was seine Aufgabe erfüllt
hat, wird im selben Auftrag entfernt, der es überflüssig macht. Die Wache
`EPOS.Kern.Tests/RepositoryOrdnungWacheTests` meldet jeden Treffer der verbotenen Muster;
Regel, Inventar und Stufenplan stehen in
[`Konzept_Repository_Aufraeumen_EPOS-Plan.md`](Dokumentation/aktuell/Konzept_Repository_Aufraeumen_EPOS-Plan.md).


## Compact instructions

Beim Verdichten des Gesprächs (`/compact` wie automatische Verdichtung) bleibt erhalten:

- **Auftrag und Stand:** Arbeitsauftrag im Wortlaut, Zweig, zuletzt zusammengeführter und
  zuletzt gepushter Commit (SHA), laufende Welle und was offen ist. Kennungen aus Papieren
  von vor dem 12.09.2026 sind alte Kennungen (Commit-Karte, siehe „Git“).
- **Laufende Arbeiten:** Kennungen, Modelle und Worktree-Pfade laufender Agenten samt Auftrag;
  laufende CI- und iOS-Läufe (Run-Kennung, Commit).
- **Entscheide des Anwenders:** jeder Entscheid mit Kennung, Inhalt und Umsetzungsstand; jede
  offene Anwenderfrage.
- **Arbeitsregeln der Sitzung:** Git-Regeln, Modellwahl, Rückfragepflicht vor macOS- und
  Setup-Läufen, die Reihenfolge Merge → Gate → Statuszeile und Protokoll → Push → iOS-Lauf → Nachweis.
- **Fehler und Behebung:** jede gefundene Ursache und der Commit, der sie behebt.
- **Dateien und Muster:** Pfade der Scratchpad-Skripte und die Muster, nach denen Statusdatei,
  Protokoll, Nachweisdokument und Logbuch fortgeschrieben werden.

Weglassen darf die Verdichtung: vollständige Dateiinhalte, Build- und Testausgaben, die in ein
grünes Gate oder einen Commit gemündet sind, und Zwischenschritte erledigter Wellen. Nach einer
Verdichtung wird der Wellenstand aus der Statusdatei und `git log origin/ios_migration_september`
nachgelesen, nicht aus dem Gedächtnis.
