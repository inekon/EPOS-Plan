# WP-Plan / EPOS-Plan — Projektkontext

Windows-Desktop-Anwendung zur Planung und Simulation von Energie- und Wärmeversorgungskonzepten
(Wärmebedarf, Brauchwasser, Prozesswärme, Heizkessel, BHKW, Wärmepumpe, Solarthermie, Photovoltaik,
Speicher, Klimadaten) mit Wizard-Workflow, Herstellerdaten-Import, Simulation, Berichten und
Wirtschaftlichkeitsrechnung.

Diese Datei beschreibt **Fachdomäne, Datenmodell, Migration und Umgang mit der Datenbank**.
Alles zu Code, Build und Architektur steht in
[`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md).

Der **Rechenkern liegt seit dem 03.09.2026 (Paket iU4) in einem eigenen Projekt**
[`EPOS.Kern`](EPOS.Kern/CLAUDE.md) — inzwischen **271 `.cs`-Dateien**, `net10.0` **ohne**
WinForms und **ohne `System.Data.OleDb`**: Simulation, Wirtschaftlichkeit, Modelle,
Zugriffsschicht (`IDatenzugriff`/`SqliteDatenzugriff`), Bericht mit Ausgabe **und**
Diagramm-Renderer, Lizenz, Import, Katalog, Export, das KI-**Wissen** und 82 Controller. Die
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
**Arbeitsregel seit dem Stichtag iZ5: Jeder neue und jeder ohnehin anzufassende Dialog entsteht
als Razor-Komponente in `EPOS.UI`, seine WinForms-Fassung wird im selben Schritt gelöscht** —
nie zwei Fassungen derselben Maske. Die Datenbankseite gehört dabei in einen Controller im Kern,
die Texte in `MyResource.Resource.*`. Erster umgestellter Dialog: „Energieträger anlegen"
(`EnergietraegerVarianteDialog`). Voraussetzung beim Anwender ist die **WebView2-Laufzeit**; das
Setup installiert sie nach.

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

**Regel für die CI (Anwender, 03.09.2026): Vor dem Aufrufen des macOS-Läufers jeweils
nachfragen, um das Actions-Kontingent nicht unnötig zu erhöhen.** Der macOS-Läufer zählt
zehnfach. Deshalb laufen `kern.yml` bei Push nur auf ubuntu und `ios.yml` gar nicht von
selbst; beide bauen auf macOS nur über *Actions → Run workflow* (bei `kern.yml` mit dem
Häkchen „macos"), und dieser Aufruf wird vorher mit dem Anwender abgestimmt — auch nach
Abschluss der Migration.

**Werkzeuge, die vor der Arbeit an einer Maske oder am Rechenweg zu kennen sind:**

| Werkzeug | Wofür | Aufruf |
|---|---|---|
| `Werkzeuge/Formularkarte` | Feldkarte einer WinForms-Maske aus `InitializeComponent` und `.resx` — Name, Typ, Beschriftung beider Sprachen, Wertebereiche, Tab-Reihenfolge, Ereignishandler; dazu ein Razor-Sektionsskelett. **Vor jeder Maskenumstellung ziehen**, von Hand vergisst man ein Feld | `dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`, Stapellauf mit `--alle` |
| `Proben/ChartProben` | zeichnet die neun Berichtsbilder aus synthetischen Reihen und prüft Maße, Farben und Determinismus — ohne Datenbank, ohne Oberfläche. Fällt rot aus, sobald der Renderer eine Windows-API braucht oder sich ein Bild ändert | `dotnet run --project Proben/ChartProben -c Release` |
| `EPOS.Referenzlauf` | der plattformfreie Rechennachweis gegen die eingefrorene Basis; läuft auf Linux, macOS und in der CI | `dotnet run --project EPOS.Referenzlauf -- lauf …` bzw. `… vergleich <ref> <neu>` |
| `Referenzlauf` (Windows) | die vollständige Suite mit den Modi `lauf`, `projekt`, `vergleich`, `pruefen`, `bildvergleich` (dazu `liste` und `migration`). `bildvergleich` stellt die neuen SkiaSharp-Bilder den alten GDI+-Bildern gegenüber — nur unter Windows | `Referenzlauf.exe <modus> …` |
| `Werkzeuge/SqlDialektPruefer` | hält **jeden** SQL-Text des Bestands mit `EXPLAIN` gegen die Testdatenbank und gegen die Access-Verbotsliste (`UPDATE … JOIN`, `Nz`, `TOP n`, `LIKE '*'`, `&`, Umlaut-Schreibweise). **Nach jeder neuen oder geänderten SQL-Anweisung ziehen** — der Referenzlauf deckt nur den Rechenweg ab, nicht die Dialog- und Pflegepfade. Regeln in [`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md) Abschnitt 6 | `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite` |

**Das Regressionsnetz ist die Abnahme, nicht die Meinung.** Jede Änderung am Rechenweg wird
gegen `Referenzlaeufe/2026-08-30_B3-Kaskade` gehalten (13 Projekte, 332 CSV); die CI rechnet bei
jedem Push die Projekte 1030, 1007 und 1017 gegen dieselbe Basis.

C#, `net10.0-windows` (Anhebung am 02.09.2026, Paket iU1), WinForms (MDI), Build zwingend
**x64**. Bis 22.08.2026 x86; Umstellungsplan, offene Pakete und Rückweg
(Git-Tag `letzter-x86-stand`) in
[`Konzept_Umstellung_64Bit_EPOS-Plan.md`](Konzept_Umstellung_64Bit_EPOS-Plan.md).

Die Datenhaltung ist seit dem 02.09.2026 **SQLite** (`Kenndaten.sqlite`, siehe
[`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md)); die native Bibliothek bringt
`Microsoft.Data.Sqlite` mit. Die Access-Engine (ACE OLEDB, 64-Bit-Fassung) wird nur noch für die
**Erststart-Migration vorhandener `.accdb`-Bestände** gebraucht — für einen Neustand ist sie
nicht mehr erforderlich.


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
