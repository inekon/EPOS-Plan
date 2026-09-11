# WP-Plan / EPOS-Plan — Projektkontext

Windows-Desktop-Anwendung zur Planung und Simulation von Energie- und Wärmeversorgungskonzepten
(Wärmebedarf, Brauchwasser, Prozesswärme, Heizkessel, BHKW, Wärmepumpe, Solarthermie, Photovoltaik,
Speicher, Klimadaten) mit Wizard-Workflow, Herstellerdaten-Import, Simulation, Berichten und
Wirtschaftlichkeitsrechnung.

Diese Datei beschreibt **Fachdomäne, Datenmodell, Migration und Umgang mit der Datenbank**.
Alles zu Code, Build und Architektur steht in
[`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md).

Der **Rechenkern liegt seit dem 03.09.2026 (Paket iU4) in einem eigenen Projekt**
[`EPOS.Kern`](EPOS.Kern/CLAUDE.md) — inzwischen **403 `.cs`-Dateien**, `net10.0` **ohne**
WinForms und **ohne `System.Data.OleDb`**: Simulation, Wirtschaftlichkeit, Modelle,
Zugriffsschicht (`IDatenzugriff`/`SqliteDatenzugriff`), Bericht mit Ausgabe **und**
Diagramm-Renderer, Lizenz, Import, Katalog, Export, das KI-**Wissen** und 127 Controller. Die
Windows-Anwendung referenziert das Projekt und übersetzt diese Dateien nicht mehr. **Eine
Fachänderung am Rechenkern wird dort gemacht, nicht in `WindowsFormsApplication1/`.**

Die **Umgebung erreicht der Kern nur über `Dienste.*`** (Paket iU5): neun Schnittstellen in
`EPOS.Kern/Allgemein/Dienste/` — Dialog, Datei, Pfade, Einstellungen, Lizenzablage, GeräteId,
Sprache, Navigation, Projekt — mit stillen Standardfassungen; die Windows-Fassungen legt
`Program.Main` ein. **`Program.*`, `MessageBox`, `Registry`, DPAPI und `SpecialFolder` sind im
Kern verboten**; zwei Wächter (in `EPOS.Kern/CLAUDE.md`) müssen leer bleiben.

Die **Oberfläche wächst seit dem 03.09.2026 (Paket iU8) in [`EPOS.UI`](EPOS.UI/)**, einer
Razor-Klassenbibliothek ohne Windows-Bindung; die WinForms-Anwendung stellt nur noch die Hülle
(`WindowsFormsApplication1/Allgemein/Blazor/`, ein `BlazorWebView` in einem modalen Fenster).
Seit dem 04.09.2026 (Welle iU9‑W10b) ist die **Simulationskonfiguration** eine Razor-SEITE —
die erste Fachseite, die auch die iOS-Wurzel `AppWurzel` erreicht. **Seit iU9‑W16b ist auch die
STARTSEITE eine Razor-Seite** (`EPOS.UI/Seiten/Start/Startseite.razor`): Kopfband, sechs Reiter
mit 21 Kacheln, Fußleiste; der `Hauptfensterrahmen` hängt sie als `BlazorSeite<Startseite>` ein, das offene
Projekt führt `EPOS.Kern/Controller/ProjektKontextCtrl`. Damit sind auch die zwei
Simulationsseiten aus ihren modalen Hüllen heraus — die Konfiguration als freie Ansicht, das
Ergebnis als `Ueberlagerung` derselben WebView (Entscheid E‑5; R‑W10b‑1 und R‑W11‑1 geschlossen).
**Seit iU9‑W16c ist auch das HAUPTFENSTER eine Razor-Seite**
(`EPOS.UI/Seiten/Hauptfenster.razor`): Menüband mit 59 Punkten in **vier Köpfen** aus der
erzeugten `Menuetabelle`, Kopfband PRODUKTNAME/GATTUNG/CLAIM/Version und darunter
`AppWurzel` — **die gemeinsame Wurzel von Windows und iOS** (Entscheid E‑1: eine Wurzel,
zwei Schalen). Zwei Anwenderentscheide vom 04.09.2026 stecken darin: die zwei Sprachpunkte
hängen unter einem Kopf **„Sprache"** (W16c‑E‑2), und **„Varianten und Bericht…" wechselt
die Ansicht** auf `BERICHTE_KOSTEN`, statt den sechsten Reiter der Startseite nach vorn zu
holen (W16c‑E‑3) — das Reiterblatt bleibt, nur der Menüweg führt in die Ansicht.
Ein dritter kam am 06.09.2026 dazu: **W16c‑E‑6** ordnet den Kopf **„Administration"**
um — BHKW und Solarkollektoren zu „Wärmebedarf & Heizung", Pufferspeicher zu
„Energiesysteme", die drei Zeitreihen in die neue Unterrubrik **„Profile & Lastgänge"** —
und löst die zwei Untermenüs mit dem einzigen Punkt „Bearbeiten" auf. Ein vierter kam
am 07.09.2026 dazu: **W16c‑E‑7** stellt das Paar Modul/Wechselrichter an BEIDEN Stellen
des Kopfs „Administration" unter einen Zwischenknoten **„Photovoltaik"** — unter
„Energiesysteme" die zwei Kataloge („PV Module", „Wechselrichter"), unter „Daten & Import"
die zwei Einlesewege („PV Module (CEC, PAN)…", „Wechselrichter (CEC, OND)…"); Ziel,
Argument und Kennung der vier Punkte bleiben, es wandert ihre Lage im Baum. Ein fünfter
kam am selben Tag dazu: **W16c‑O‑7** löst das LETZTE Ein-Punkt-Untermenü auf — der Knoten
„Klimadaten" führte ein einziges Kind derselben Beschriftung, und der Punkt steht seither
unmittelbar im Kopf, mit dem Bild `Menu4` des gefallenen Knotens; damit gilt die Regel aus
W16c‑E‑6 („kein Untermenü mit nur EINEM Punkt") ohne Ausnahme. Am selben Tag kam der
erste NEUE Weg seit W6‑E‑2 dazu: **W13‑E‑2** hängt den **Stromspeicherimport**
(„Stromspeicher (CEC, bslib)…") in „Daten & Import"; **SIM‑Q3** (11.09.2026, Auftrag
#207) hängt „Simulation…" in den Kopf „Projekt" — damit handeln 46 der 59 Punkte.
**Das Menü ist
Daten, und `Menuetabelle.cs` ist seit W16c die Quelle:** Der Designer ist gelöscht, das
Erzeugerskript liegt nicht im Repository — wer das Menü ändert, ändert diese Datei.
Die WinForms-Seite ist seither die **Hülle ohne Designer** (129 Zeilen) und heißt seit dem
Anwenderentscheid **E‑10** vom 04.09.2026 `Hauptfensterrahmen`
(`WindowsFormsApplication1/Views/Hauptformular/Hauptfensterrahmen.cs`, vorher `MDIMainForm` —
`IsMdiContainer` stand seit jeher auf `false`): **drei Namen, drei Dinge** — der RAHMEN ist das
Fenster mit `Application.Run`, dem `BlazorWebView`, F1 und dem Sprachwechsel, `Hauptfenster` die
Razor-SEITE darin, `HauptfensterHuelle` deren Blazor-Hülle. Die Anwendung läuft
„Per Monitor V2" (E‑6 / iF21), die `DpiInsel` ist gefallen. `WindowsFormsApplication1` führt
damit **eine** Maske (`Form_HelpPopup`, bleibt bis iU11), **keine Fachmaske** und **null**
Inline-SQL — die Mischphase ist zu Ende (M9).
**Arbeitsregel seit dem Stichtag iZ5: Jeder neue und jeder ohnehin anzufassende Dialog entsteht
als Razor-Komponente in `EPOS.UI`, seine WinForms-Fassung wird im selben Schritt gelöscht** —
nie zwei Fassungen derselben Maske. Die Datenbankseite gehört dabei in einen Controller im Kern,
die Texte in `MyResource.Resource.*`. Erster umgestellter Dialog: „Energieträger anlegen"
(`EnergietraegerVarianteDialog`). Voraussetzung beim Anwender ist die **WebView2-Laufzeit**; das
Setup installiert sie nach.

Die **Datenseite der Oberfläche wächst seit dem 11.09.2026 (Auftrag #208) in
[`EPOS.UI.Daten`](EPOS.UI.Daten/)** — dem dritten plattformfreien Projekt neben Kern und
Oberfläche. Dort liegen die **Hüllen**: der Programmtext, der aus Kern-Controllern die DTO der
Razor-Seiten baut. **Warum ein eigenes Projekt:** Eine Hülle braucht BEIDES — den Kern (sie
lädt) und die Oberfläche (sie baut deren DTO). In `EPOS.Kern` kann sie deshalb nicht liegen
(`EPOS.UI` kennt den Kern, nicht umgekehrt), in `EPOS.UI` ebenso wenig (Hausregel „Keine
Datenbank"). Bis #208 lagen sie sämtlich in `WindowsFormsApplication1/Views/` — und damit war
jede Fachseite auf iOS unerreichbar, auch wenn ihre Hülle keine einzige Windows-Zeile führte:
Von den **5 490 Zeilen** der zwei Simulationshüllen waren genau **sechs** Windows. Verlegt
sind mit #208 die **18 Dateien / 8 115 Zeilen** der Simulation samt ihren Unterdialogen;
`EnableWindowsTargeting=false` hält das Projekt sauber, und was die Plattform beisteuern muss,
kommt als **benannte Naht** herein (`Simulation/SimulationPlattformwege.cs`, `Katalogwege.cs`).
Die Windows-Hülle `Views/Simulation/SimulationHuelle.cs` ist seither ein **Adapter von
64 Zeilen** (vorher 122), und `EPOS.iOS/Dienste/IosProjektQuelle.SimulationGaben` liefert
denselben Parametersatz aus derselben Quelle — **die Simulation ist damit die erste Fachseite,
die auf dem iPad wirklich rechnet** (Stufe S2 des Konzepts
[`Projekte/Konzept_Simulationsablauf_EPOS-Plan.md`](Projekte/Konzept_Simulationsablauf_EPOS-Plan.md)).

Die **iOS-Hülle steht seit dem 03.09.2026 (Paket iU10) in [`EPOS.iOS`](EPOS.iOS/CLAUDE.md)** — eine
MAUI-Blazor-Hybrid-App mit **einer** Seite und darin **einer** `BlazorWebView`, die
`EPOS.UI.Seiten.AppWurzel` zeigt. Sie trägt nur, was die Plattform beisteuert: die neun
`Dienste.*`-Adapter (Schlüsselbund, `identifierForVendor`, `Preferences`, Sandbox-Pfade,
Dokumentenwähler, Teilen-Blatt), die Seed-Kopie der Datenbank beim Erststart und den Prüfmodus für
die CI. **Sie hat eine eigene Projektmappe `EPOS.iOS/EPOS.iOS.sln`** und steht bewusst weder in
`WP-Plan.sln` noch im Solution-Filter — auf Windows und Linux gibt es die iOS-Workload nicht, jeder
Restore dort bräche mit `NETSDK1147`. Gebaut und im Simulator geprüft wird sie **ausschließlich** im
CI-Job `.github/workflows/ios.yml` (`macos-26`, Workload-Set `10.0.400.1`, Xcode 26.6), den man von
Hand auslöst: GitHub → Actions → iOS → *Run workflow*. Was ohne Mac nachweisbar ist und was nicht,
steht in [`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md).

Die **Mehrspeicherrechnung** liegt seit dem 11.09.2026 in **zwei eigenen Projekten**:
[`SpeicherEngine`](SpeicherEngine/) rechnet die Flotte — AC-Physik, Verteilung,
Wirtschaftlichkeit, Rainflow-Zyklen und die begrenzte Rastersuche (`Flotten*.cs`) — ohne
Datenbank und ohne Oberfläche, und **`SpeicherPlanung`** bindet **Google OR-Tools 9.15.6755
(SCIP)** als gemischt-ganzzahligen Fahrplaner an. Die Naht dazu führt der Kern
(`SpeicherFlottenStudieCtrl`, `SpeicherFlottenProjektCtrl`, `SpeicherAuslegungCtrl` samt
`.Rechnung`, `SpeicherZeitreihenImport`, `SpeicherFlottenCsvImport`), die Oberfläche seit **#192** (11.09.2026, Paket P3 des Konzepts `Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`) die **freie Ansicht `STROMSPEICHER_AUSLEGUNG`** der `AppWurzel` (`EPOS.UI/Seiten/Strom/StromspeicherAuslegungSeite.razor` mit Ablaufleiste, Diagnosebanner und Peak-Ziel-Block; die Überlagerungsdialoge `SpeicherFlottenDialog` und `SpeicherOptimierungDialog` sind gefallen), die Editoren und Bausteine unter `EPOS.UI/Dialoge/Strom/` — darunter seit **#193** die Größen-Sicht `SpeicherFlottenGroessenAnsicht` (Paket P4) — und den Reiter `StromspeicherReiter.razor`;
**Schemaschritt 73** legt `Tab_SpeicherAuslegung` für die gespeicherten Auslegungsprofile,
Suchbereiche und importierten Zeitreihen an; **Schemaschritt 74** (Auftrag #178, 11.09.2026)
baut dieselbe Tabelle als **STRICT**-Tabelle neu auf — sie war die einzige Fachtabelle des
Zielschemas ohne `STRICT`, und SQLite kennt kein `ALTER TABLE … STRICT`. Was umgesetzt ist, steht in
[`Doku_Mehrspeicher_Konzept_und_Umsetzung.md`](Doku_Mehrspeicher_Konzept_und_Umsetzung.md),
die Fachgrundlage in
[`Projekte/Spezifikation_Stromspeicher_Optimierung.md`](Projekte/Spezifikation_Stromspeicher_Optimierung.md)
(Fassung 1.2 vom 11.09.2026, 14 Kapitel); der ältere Einzelspeicherstand bleibt als
[`Doku_Speicherauslegung_Kosten_Zeitreihen.md`](Doku_Speicherauslegung_Kosten_Zeitreihen.md)
datiert liegen.

**Regel: `Google.OrTools` hängt NUR an `SpeicherPlanung`** — und damit nur an der
Windows-Anwendung, die als einziges Projekt `SpeicherPlanung` referenziert und in
`Program.Main` die Fabrik `SpeicherFlottenProjektCtrl.PlanerFactory` setzt. **Nie an
`EPOS.Kern`, `EPOS.UI`, `SpeicherEngine` oder `EPOS.iOS`**; dort ist der Planer ausschließlich
die Schnittstelle `IFlottenPlaner` aus `SpeicherEngine/FlottenModel.cs`. Daraus folgt, und es
ist gewollt: **Ohne registrierten `IFlottenPlaner` sind die drei planenden Betriebsziele
`PvPlanung`, `Arbitrage` und `MultiUse` nicht verfügbar** — `SpeicherFlottenProjektCtrl.Planer`
bricht dann mit einer benannten Meldung ab. Die zwei reaktiven Ziele `PvGreedy` und
`PeakShaving` rechnen ohne Planer und stehen deshalb auf jeder Plattform.

`WP-Plan.Kern.slnf` führt seit Auftrag #208 **elf** Projekte — `EPOS.Kern`, `EPOS.UI`,
`EPOS.UI.Daten`, `KiKern`, `SpeicherEngine`, `SpeicherPlanung` und die fünf zugehörigen
Testprojekte (`EPOS.UI.Daten` prüft `EPOS.Kern.Tests`) —, `WP-Plan.sln`
zusätzlich die Windows-Anwendung und die Werkzeuge. `EPOS.iOS` steht weiterhin in keiner von
beiden (eigene Projektmappe, siehe oben).

Der Python-Referenzkern unter `Projekte/Speichersimulation/code/` ist **Referenz, kein
Werkzeug**: Er gehört zur Spezifikation, wird nicht gebaut, von keiner CI gerufen und steht in
keiner Projektmappe. Er belegt den Algorithmus, gegen den die C#-Umsetzung geprüft wurde —
deshalb steht er nicht in der Werkzeugtabelle unten.

**Regel für die CI (Anwender, 03.09.2026): Vor dem Aufrufen des macOS-Läufers jeweils
nachfragen, um das Actions-Kontingent nicht unnötig zu erhöhen.** Der macOS-Läufer zählt
zehnfach. Deshalb laufen `kern.yml` bei Push nur auf ubuntu und `ios.yml` gar nicht von
selbst; beide bauen auf macOS nur über *Actions → Run workflow* (bei `kern.yml` mit dem
Häkchen „macos"). Die pauschale Freigabe „immer ja bis Abschluss aller Migrationsschritte"
(Anwender, 03.09.2026) galt bis zum 09.09.2026 und ist **zurückgenommen (Anwender,
09.09.2026: „die iOS-Läufe sollten nur wenn unbedingt nötig gestartet werden")**. Seither
wird `ios.yml` nur ausgelöst, wenn eine Änderung die iOS-Hülle SELBST trifft — `EPOS.iOS/`,
`.github/workflows/ios.yml`, eine `Dienste.*`-Schnittstelle, die ein iOS-Adapter
implementiert, der Prüfmodus oder die Seed-Kopie beim Erststart — oder wenn der Anwender ihn
verlangt. Eine Änderung an Kern, Oberfläche, Testdatenbank oder Doku, die `kern.yml` auf
ubuntu schon prüft, ist KEIN Grund; der Nachweis dafür ist der grüne Kern-Lauf. Der Nachzug
der Testdatenbank auf Schemastand 72 (#154) war der erste Fall dieser Regel: Lauf 40 auf
`9999d51` wurde nach vier Minuten abgebrochen und zählt nicht als Nachweis. Der Nachzug auf
**Schemastand 73** (Stromspeicher-Sync vom 11.09.2026) ist der zweite: Er trifft Kern,
Oberfläche und Testdatenbank, nicht die Hülle — geprüft hat ihn `kern.yml` auf ubuntu, ein
iOS-Lauf wurde nicht ausgelöst. Der Nachzug auf **Schemastand 74** (Auftrag #178 vom
11.09.2026, `Tab_SpeicherAuslegung` als STRICT) ist der dritte Fall derselben Regel: Er trifft
Kern, Werkzeug und Testdatenbank, nicht die Hülle; das STRICT-Gate der iOS-CI zieht seine
Erwartung aus der Seed-Datenbank selbst, es war nichts am Workflow zu ändern. Dieselbe Zurückhaltung gilt seit dem 11.09.2026
(Anwenderentscheid „#160‑E‑1: CI") für den **Setup-Lauf**, der das Installationsprogramm auf
`windows-latest` baut: Der Windows-Läufer zählt **doppelt**, deshalb hat der Lauf
**nur** `workflow_dispatch` — nicht bei Push (gemessen am 11.09.2026, Lauf 34592377805:
4 min 14 s für Veröffentlichung, Auslieferungsvorlage und Inno-Setup-Übersetzung von 186 MB
Herstellerdaten, Installer 153,5 MB — die befürchtete Stunde war es nicht). Der Job (`installer`) lag zunächst in einer
eigenen Datei `setup.yml` (#176) und steht seit **#177** als zweiter Job in
`.github/workflows/windows.yml`, weil GitHub eine Workflow-Datei erst registriert, wenn sie
auf dem Standardzweig `main` liegt — ein `workflow_dispatch` auf eine neue Datei eines
Nebenzweigs scheitert bis dahin mit 404, während `windows.yml` bereits registriert ist und ein
`workflow_dispatch` mit `ref = <Zweig>` dessen Fassung dieses Zweigs benutzt. Ausgelöst wird
er über *Actions → Windows → Run workflow* mit Häkchen „setup" (Häkchen „schnell" schaltet
zusätzlich auf `lzma2/normal`); ohne „setup" läuft nur der bisherige Job `build-test`, die
beiden schließen sich über ihre `if`-Bedingungen gegenseitig aus. Im Zweifel vor dem Auslösen
nachfragen.

**Werkzeuge, die vor der Arbeit an einer Maske oder am Rechenweg zu kennen sind:**

| Werkzeug | Wofür | Aufruf |
|---|---|---|
| `Werkzeuge/Formularkarte` | Feldkarte einer WinForms-Maske aus `InitializeComponent` und `.resx` — Name, Typ, Beschriftung beider Sprachen, Wertebereiche, Tab-Reihenfolge, Ereignishandler; dazu ein Razor-Sektionsskelett. **Vor jeder Maskenumstellung ziehen**, von Hand vergisst man ein Feld | `dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`, Stapellauf mit `--alle` |
| `Proben/ChartProben` | zeichnet die **43** Bilder (seit #222 der Ring des Simulations-Dashboards ohne Legende und der graue Vollring bei 0 % Deckung, seit #193 die Rasterkarte der Flotte mit Schraffur und die Schnittkurve über der Leistung, seit #184 die Jahresprojektion der Speicherflotte, Bericht, Eingabemasken, seit iU9‑W11a die Ergebnisseite, seit iU9‑W14c die zwei Klimadiagramme, seit der Windows-Abnahme 05.09.2026 die zwei Bilder des Datenzooms und die Wochen- und Tagesstufe des Jahresverlaufs, W8‑E‑2, seit der Abnahme V2 vom 07.09.2026 die Rasterkarte und die Schnittkurve der Auslegungsoptimierung, W11b‑B‑5) aus synthetischen Reihen und prüft Maße, Farben und Determinismus — dazu **zehn Gegenproben**, die belegen, dass ein Achsenausschnitt, die Marke des Optimums, eine Ersatzjahrmarke, die Schraffur unzulässiger Zellen beziehungsweise der Wegfall der Ringlegende das Bild wirklich ändert; ohne Datenbank, ohne Oberfläche. Fällt rot aus, sobald der Renderer eine Windows-API braucht oder sich ein Bild ändert | `dotnet run --project Proben/ChartProben -c Release` |
| `EPOS.Referenzlauf` | der plattformfreie Rechennachweis gegen die eingefrorene Basis; läuft auf Linux, macOS und in der CI | `dotnet run --project EPOS.Referenzlauf -- lauf …` bzw. `… vergleich <ref> <neu>` |
| `Referenzlauf` (Windows) | die vollständige Suite mit den Modi `lauf`, `projekt`, `vergleich`, `pruefen` (dazu `liste` und `migration`). Der frühere Modus `bildvergleich` ist mit iF23 (03.09.2026) samt dem GDI+-Renderer gelöscht | `Referenzlauf.exe <modus> …` |
| `Werkzeuge/ResourceDesigner` | erzeugt `EPOS.Kern/MyResource/Resource.Designer.cs` vollständig neu aus der neutralen `.resx` (Format des StronglyTypedResourceBuilder). **Nach jedem neuen Ressourcenschlüssel ziehen** statt die Designer-Datei von Hand zu ergänzen — ohne Visual Studio gibt es keinen anderen Generator. Der Lauf ist **wiederholbar** (seit #152, 07.09.2026): Ändert sich kein Schlüssel, lässt ein zweiter Lauf die Datei byte-gleich liegen, und jeder Aufruf prüft das selbst | `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben` (ohne Argument: nur prüfen — nennt die Zeichenbilanz) |
| `Werkzeuge/Auslieferungsvorlage` | erzeugt aus einer produktiven `Kenndaten.sqlite` die **bereinigte Auslieferungsdatenbank** samt Beispielprojekten (#157‑E‑2, Auftrag #160) — Projektdaten weg, Kataloge vollständig (Vorgabe `alle` seit #160‑E‑1a, `readonly` wählbar), `Tab_Applikation` ohne Kundennamen, `VACUUM`, `journal_mode = WAL`, Prüfbericht `<ziel>.bericht.txt` daneben. Die Tabellenliste kommt aus dem SCHEMA, nicht aus einer gepflegten Liste; die Quelle bleibt byte-gleich. **Vor jeder Auslieferung ziehen** statt die vier Handgriffe aus Setup-Konzept 6.1 zu wiederholen. Rückgabe 0 = erzeugt und abgenommen; 2 Aufruf, 3 Schreibort, 4 Katalogwächter (nur bei `readonly`), 5 fachlich | `dotnet run --project Werkzeuge/Auslieferungsvorlage -c Release -- <quelle.sqlite> <ziel.sqlite> [--beispiele <ordner-oder-liste>] [--trocken]`, Proben mit `dotnet test Werkzeuge/Auslieferungsvorlage/Auslieferungsvorlage.sln -c Release` |
| `Werkzeuge/SqlDialektPruefer` | hält **jeden** SQL-Text des Bestands mit `EXPLAIN` gegen die Testdatenbank und gegen die Access-Verbotsliste (`UPDATE … JOIN`, `Nz`, `TOP n`, `LIKE '*'`, `&`, Umlaut-Schreibweise). **Nach jeder neuen oder geänderten SQL-Anweisung ziehen** — der Referenzlauf deckt nur den Rechenweg ab, nicht die Dialog- und Pflegepfade. Regeln in [`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md) Abschnitt 6 | `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite` |

**Das Regressionsnetz ist die Abnahme, nicht die Meinung.** Jede Änderung am Rechenweg wird
gegen `Referenzlaeufe/2026-09-11_R7_Speicherflotte` gehalten (**dreizehn Projekte, 345 CSV,
1 937 Skalare**, aus dem plattformfreien `EPOS.Referenzlauf` gegen `Kenndaten_Test.sqlite` auf
**Schemastand 74** — die Basis selbst entstand auf Schemastand 73; **Schritt 74 macht
`Tab_SpeicherAuslegung` zur STRICT-Tabelle und ändert keinen Rechenwert**, die Testdatenbank ist
am 11.09.2026 byte-gleich darauf nachgezogen, 13/13 Projekte byte-gleich) nach dem
Anwenderentscheid **SP‑O‑8** vom 11.09.2026: Das dreizehnte
Projekt **1046 „Prüfprojekt Speicherflotte"** — eine Tiefkopie von 1007 mit einem aktivierten
Stand `@Projektflotte` (zwei Einheiten, 24 kWh an 10/12 kW und 16 kWh an 6/7 kW, getrennte
Richtungswirkungsgrade und SoC-Bänder, Ziel `PeakShaving` gegen 16 kW, Verteilung `Kaskade`,
kein Solver) — ist das **einzige**, das den Flottenpfad des Projektlaufs betritt; die zwölf
übrigen fahren die Einzelanlage. **Sie sind byte-gleich zur Vorgängerbasis R6**, der Wechsel
ist reine Erweiterung. Die Wirkung der Flotte: Bezugsspitze 19,7762 → **16,7428 kW**
(−15,3 %), Netzbezug 50 538,7 → 51 611,0 kWh (+2,1 %, davon 122,4 kWh Umwandlungsverlust);
der Export führt dafür 42 neue Skalare unter `Flotte.*` und vier Ganglinien je Einheit, alle
nur, wenn die Flotte gerechnet hat. Herleitung, Wahl des Peak-Ziels und die drei Gegenproben
stehen im `protokoll.txt` der Basis; das Projekt entsteht wiederholbar aus
`Referenzlaeufe/Skripte/pruefprojekt_1046_speicherflotte.py`.
Die Vorgängerbasis `2026-09-07_R6_PvKoeffizienten` (zwölf Projekte, 312 CSV, 1 792 Skalare)
liegt seit dem 11.09.2026 (Anwenderentscheid SYNC‑Q1) nicht mehr im Arbeitsbaum, sondern nur noch
in der Git-Geschichte (Stand `1e71d30`) — sie entstand nach dem
Befund **W6‑B‑5** mit den Entscheiden
**Q1–Q3**: Schemaschritt 69 repariert die verdorbenen PV-Modulkoeffizienten aus der CEC-Liste.
**Elf der zwölf Projekte waren byte-gleich zu IHRER Vorgängerbasis; nur 1007 wich ab**, und dort
nur in den acht Dateien der PV-Kette — die Ursache ist genau eine Spalte: `T_NOCT` springt vom
Rückfall 45 °C auf den Katalogwert 47,4 °C, das sind −0,69 % theoretische PV-Erzeugung. Der
Gegenbeweis (allein `T_NOCT` zurück auf 45 → 29 von 29 Dateien byte-gleich zu R5) steht im
`protokoll.txt` der Basis, abrufbar mit
`git show 1e71d30:Referenzlaeufe/2026-09-07_R6_PvKoeffizienten/protokoll.txt`. Die Vorgängerbasis
`2026-09-07_R5_Zahlenrand` liegt ebenso seit dem 11.09.2026 (SYNC‑Q1) nur noch in der
Git-Geschichte (Stand `1e71d30`) — sie entstand nach den drei
Anwenderentscheiden **W8‑O‑5d‑Q1** (Zahlenrand), **W8‑O‑5d‑Q2** („keine Treue zur alten DLL") und **Em‑9.8** (zehn Emissionsskalare). **Elf der zwölf Projekte wichen damals gewollt von IHRER Vorgängerbasis R4 ab, und die Ursache war Q2:** Die drei Physik-Funktionen des BHKW-Plan-Ports schnitten ihre Ergebnisse auf ganze Zahlen ab — der spezifische Wärmeverlustkoeffizient landete dadurch auf ganzen W/K (194,5722 → 194 im Projekt 1007) und die Tagesheizlast auf ganzen Wattstunden. Ohne das Raster verschiebt sich die Jahressumme des Gebäudewärmebedarfs um −0,12 % … +0,44 %, an einzelnen milden Tagen um bis zu 13 %; die Zahlen und der Gegenbeweis (Projekt **1030** ohne Gebäudebedarf ist byte-gleich zu R4) stehen im `protokoll.txt` der Basis, abrufbar mit
`git show 1e71d30:Referenzlaeufe/2026-09-07_R5_Zahlenrand/protokoll.txt`. Q1 gibt den zwei Schwellen, die vorher am letzten Bit entschieden — die Speicherhysterese `SOC >= Q_max · SchwelleAus` und die Volllast/Modulations-Grenze des BHKW —, einen benannten Zahlenrand (`EPOS.Kern/Allgemein/Simulation/Rechenrand.cs`). Die Vorgängerbasis `2026-09-07_R4_Double` (Entscheid **W8‑O‑5d**, „alles in double") liegt ebenso seit dem 11.09.2026 (SYNC‑Q1) nur noch in der Git-Geschichte (Stand `1e71d30`), ebenso `2026-09-06_R3_Straenge`, `2026-09-05_R2_Zeitbasis` und `2026-08-30_B3-Kaskade`, deren Projekte 1011 und 1021 nicht in der Testdatenbank stehen; die CI rechnet bei
jedem Push die Projekte 1030, 1007, 1017, 1045 **und 1046** gegen die aktuelle Basis.

**Die Basis führt seit dem Anwenderentscheid Em‑9.8 (07.09.2026) auch zehn
Emissionsskalare** je Projekt mit Kessel- bzw. BHKW-Stufe (`Em.Kessel.Co2T` in t/a,
`…So2Kg`/`…NoxKg`/`…CoKg`/`…StaubKg` in kg/a, dieselben fünf für `Em.Bhkw.`). Damit ist die
Testdatenbank an einer Stelle regressionsrelevant, an der sie es vorher nicht war — den
Emissionsfaktoren —, und daraus folgt die **Einfrierregel (Em‑9.8‑Q4): Wer einen gesäten
Emissionsfaktor der Testdatenbank ändert — `emissionsart`, einen AKTIVEN `emissionswert`,
`Tab_Brennstoff_Stamm.CO2/SO2/NOx/Staub`, `energy_project_settings.co2/so2/nox` oder den
Berechnungsmodus eines Referenzprojekts —, friert im selben Schritt die Basis neu ein und
begründet den Wechsel in [`Referenzlaeufe/LIESMICH.md`](Referenzlaeufe/LIESMICH.md).
Vorlagen (`ist_aktiv = falsch`) bleiben ausdrücklich frei.**

**Seit dem Befund W6‑B‑5 (07.09.2026) gilt dieselbe Regel für die
PV-MODULKOEFFIZIENTEN.** Schemaschritt 69 hat `alpha_SC`, `beta_OC`, `gamma_PMP` und
`T_NOCT` in `Tab_PV_STAMM` (6 Sätze) und `Tab_PV` (9 Sätze) auf die Werte der CEC-Liste
gezogen; **`T_NOCT` geht in beide PV-Modelle** (`SimulationPV.NoctDesModuls` nimmt den
Katalogwert, sobald er im Fenster 20…60 °C liegt, sonst den Rückfall 45 °C). Ein einziger
geänderter NOCT verschiebt damit die Jahreserzeugung eines Referenzprojekts. Daraus folgt die
**Einfrierregel (W6‑B‑5‑Q3): Wer einen gesäten Modulkoeffizienten der Testdatenbank ändert —
oder ein neues PV-Modul anlegt, das ein Referenzprojekt benutzt —, friert im selben Schritt
die Basis neu ein und begründet den Wechsel in `Referenzlaeufe/LIESMICH.md`.** `alpha_SC`
und `beta_OC` liest kein Rechenweg (nur die Strangampel und die Importprüfung); der Beleg
dafür steht als Gegenprobe im `protokoll.txt` der Basis R6.

**Seit dem Anwenderentscheid SP‑O‑8 (11.09.2026) gilt dieselbe Regel für die
FLOTTENPARAMETER des Projekts 1046.** Der Stand `@Projektflotte` in `Tab_SpeicherAuslegung`
(ID_Projekt 1046, Anlagenbezug `NULL`) ist der einzige aktivierte Flottenstand der
Testdatenbank; er schaltet den zweiten Speicherpfad ein, und `aggregate.csv` führt dafür
42 Skalare und vier Ganglinien. Daraus folgt die **Einfrierregel (SP‑O‑8): Wer diesen Stand
ändert — Einheitenzahl, Kapazität, Lade-/Entladeleistung, Richtungswirkungsgrade,
SoC-Grenzen, Start-SoC, Peak-Reserve, Hilfsverbrauch, Betriebsziel, Verteilung, Peak-Ziel,
Netzladung, Batterieexport, Energie-Ausgleichswert oder Lebensdauerkurve — oder eine
Projektzeile von 1046, friert im selben Schritt die Basis neu ein und begründet den Wechsel
in `Referenzlaeufe/LIESMICH.md`.** Auslegungs- und Arbeitsstände unter einem ANDEREN
Bezeichner als `@Projektflotte` erreichen den Projektlauf nicht und bleiben frei.

C#, `net10.0-windows` (Anhebung am 02.09.2026, Paket iU1), WinForms (MDI), Build zwingend
**x64**. Bis 22.08.2026 x86; Umstellungsplan, offene Pakete und Rückweg
(Git-Tag `letzter-x86-stand`) in
[`Konzept_Umstellung_64Bit_EPOS-Plan.md`](Konzept_Umstellung_64Bit_EPOS-Plan.md).

Die Datenhaltung ist seit dem 02.09.2026 **SQLite** (`Kenndaten.sqlite`, siehe
[`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md)); die native Bibliothek bringt
`Microsoft.Data.Sqlite` mit. **Seit dem Anwenderentscheid `#157‑E‑1` (Weg W3,
09.09.2026) kommt die Access-Engine im ausgelieferten Programm nicht mehr vor:** Das
Setup installiert sie nicht mehr nach, und der Erststart-Assistent ist gefallen. Eine
Neuinstallation bekommt ihre Datenbank aus der ausgelieferten Vorlage
`{app}\Vorlage\Kenndaten.sqlite`, die der Kern beim ersten Start in den Datenordner
kopiert (`EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs`, gerufen aus
`Program.DatenbankBereitstellen()`; auf iOS tut `EPOS.iOS/Datenbankbereitstellung.cs`
dasselbe aus dem Anwendungspaket). Die Übernahme eines `.accdb`-Altbestands ist seither
ein **Hauswerkzeug** — die Konsolenfassung `EposSqliteMigrator.exe` samt
`SchemaMigration.HebeAltbestand`; sie braucht die ACE-Engine auf dem Rechner, auf dem sie
läuft (BETRIEB_SQLITE.md Abschnitt 1.1 und 7).


## Datenhaltung

> Dieser Abschnitt beschreibt den Stand **vor** der SQLite-Umstellung vom 02.09.2026. Er gilt
> weiterhin für Altbestände und für das Verständnis des Schemas; der laufende Betrieb steht in
> [`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md).

Alles in einer einzigen Access-Datei `Kenndaten.accdb` — Kataloge **und** Projektdaten. Eine
separate Projektdatei gibt es nicht.

Der Rechenkern arbeitet mit fest verdrahteten Feldgrößen: 8760 Stunden, 168 Wochenwerte, 365 Tage,
12 Monate. Profile und Ganglinien im Datenmodell müssen zu diesem Raster passen.

## Namenskonventionen im Schema

`Tab_*` sind Stamm- und Projektdaten, `Tab_*_STAMM` der Auslieferungskatalog, `Z_*` die Zuordnung
Projekt ↔ Katalogobjekt, `Abfrage_*` gespeicherte Access-Abfragen. Verknüpfungen laufen vielfach über
Textfelder (`Bezeichner`, `Typname`) statt über IDs — **bei neuen Beziehungen IDs verwenden.**

Das Feld `ReadOnly` in den `_STAMM`-Tabellen bedeutet faktisch „gehört zur Auslieferung": Das
Migrationsskript behält `ReadOnly = TRUE` aus der Vorlage und ersetzt alles Übrige durch die
Anwenderdaten.


## Migration

Die DB Migration ist eine separate Anwendung. Das sql migrationsskript wird jedes mal neu erstellt und ist daher nicht als Referenz geeignet.


## Umgang mit der Datenbank

Vor jedem Schreibzugriff prüfen, ob `Kenndaten.laccdb` existiert (dann ist die DB geöffnet), und
vorher eine datierte Kopie anlegen. `C:\ProgramData\EPOS_PLAN` erlaubt normalen Benutzern nur das
Anlegen neuer Dateien, nicht das Ändern vorhandener — eine vom Installer angelegte `Kenndaten.accdb`
ist deshalb schreibgeschützt, bis sie einmal über „Komprimieren und reparieren" neu geschrieben
wurde. Dieselbe ACL blockiert den Start auf einem **zweiten Windows-Konto**, solange das erste das
Programm offen hat (Sperrdatei nicht beschreibbar) — Ursache, `icacls`-Lösung und Installer-Hinweis
in [`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md).

`.accdb` ist in `.gitignore` ausgeschlossen: Änderungen an der Datenbank landen nie in einem Commit
und müssen separat gesichert werden (`DB-Backup/`).


## Brauchwasser / TWW-Profile

Der Brauchwasserkatalog wurde am 02.08.2026 um 11 Wochen-Stundenprofile und 13 Monatswertsätze nach
VDI 6002 erweitert. Alles dazu — Datenmodell, sämtliche Zahlenwerte, Herleitung, Werkzeugkette zum
Bearbeiten der `.accdb` ohne Access und die offene Migrationsbaustelle — steht in
[`KONTEXT_Brauchwassertypen_VDI6002.md`](KONTEXT_Brauchwassertypen_VDI6002.md).


## Grundlagen- und Konzeptdokumente

Nehme aktuelle .md Dateien jeweils zur bearbeteten Thematik. Teilweise gibt es ältere .MD Dateien, die nur teilweise noch Gültigkeit haben.
Lizenzierungskonzept als
`EPOS-Plan_Konzept_Lizenzierung.md` in der Wurzel.


## Compact instructions

Beim Verdichten des Gesprächs (`/compact` wie automatische Verdichtung) bleibt erhalten:

- **Auftrag und Stand**: der Arbeitsauftrag im Wortlaut, der Zweig, der zuletzt zusammengeführte
  und der zuletzt gepushte Commit (SHA), die laufende Welle bzw. der laufende Schritt und was
  davon noch offen ist.
- **Laufende Arbeiten**: Kennungen und Worktree-Pfade laufender Agenten samt Auftrag, armierte
  Check-ins (Trigger-Kennung, Uhrzeit), laufende CI- und iOS-Läufe (Run-Kennung, Commit).
- **Entscheide des Anwenders**: jeder in der Sitzung getroffene Entscheid mit Kennung
  (z. B. W15a‑O‑3), Inhalt und Umsetzungsstand; jede noch offene Anwenderfrage mit Kennung.
- **Arbeitsregeln der Sitzung**: Git-Regeln (Zweig, Attribution-Trailer, kein Pull Request, kein
  Tag-Push), die Reihenfolge Merge → Gate → Statusblock → Push → iOS-Lauf → Nachweis, die Regeln an
  Agenten (Modellwahl, Kultur pinnen, eigener Worktree, kein Push, kein CI-Aufruf, Aufräumen).
- **Fehler und Behebung**: jede gefundene Fehlerursache und der Commit, der sie behebt.
- **Dateien und Muster**: Pfade der Scratchpad-Skripte und Arbeitsanweisungen sowie die Muster,
  nach denen Dokumente fortgeschrieben werden (Aufbau eines Statusblocks, Anker im
  Nachweisdokument, Übersichtszeile im Konzept).

Weglassen darf die Verdichtung: vollständige Dateiinhalte, Build- und Testausgaben, die bereits in
ein grünes Gate oder einen Commit gemündet sind, und die Zwischenschritte erledigter Wellen jenseits
von Commit und Ergebnis.

Während der iOS-Migration (Arbeitszweig seit dem 11.09.2026 `ios_migration_september`, davor `ios_migration`;
Anwenderentscheid 11.09.2026 „ios_migration_september wird der Arbeitszweig") gilt zusätzlich: Der dauerhafte Stand steht in den
Statusblöcken von [`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md) und in
[`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md). Nach einer Verdichtung wird der
Wellenstand von dort und aus `git log origin/ios_migration_september` nachgelesen, nicht aus dem Gedächtnis.
