# WP-Plan — Ist-Analyse und Integrationsstellen für ein TWW-Zapfprofil-Modul

**Stand:** 29.07.2026 · Analyse der Arbeitskopie `C:\Users\DirkEngelmann\Documents\WP_Plan` (Repo `github.com/inekon/WP-Plan`) über die Dateibrücke, ~45 gezielte Zugriffe.

## 1. Technik-Steckbrief

| Aspekt | Befund | Beleg |
|---|---|---|
| Framework | .NET Framework **4.8**, C# `LangVersion latest`, WinForms (`OutputType WinExe`) | `WindowsFormsApplication1\WindowsFormsApplication1.csproj` |
| Plattform | Default `x86`; x64-Konfigurationen existieren, `Prefer32Bit=false` | ebd. |
| Paketverwaltung | klassische `packages.config` (kein PackageReference) | `WindowsFormsApplication1\packages.config` |
| Pakete | ScottPlot 5.1.57 + ScottPlot.WinForms, SkiaSharp 3.119, HarfBuzzSharp, OpenTK 3.1.0 + GLControl, MathNet.Numerics 5.0.0, **Mscc.GenerativeAI 3.1.0** (Google Gemini), JsonSchema.Net, System.Text.Json 10.0.3, Microsoft.Extensions.* 10.0.3, Humanizer | ebd. |
| Charting (tatsächlich genutzt) | **System.Windows.Forms.DataVisualization.Charting** (MS Chart), gekapselt in `ChartManagerNeu` | `Allgemein\GrafikTools\ChartManagerNeu.cs`, `Views\Brauchwasser\Form_ErgBrauchwasserwaerme.cs`, `Views\Hauptformular\FormMain.cs` |
| ScottPlot/OpenTK/Skia | referenziert, in allen gelesenen Views/Ctrl **nicht** verwendet – vermutlich experimenteller/neuer Pfad *(unsicher, nicht alle Views gelesen)* | – |
| WpfControlLibrary1 | Gerüst-Projekt, enthält nur `UserControl1.xaml` mit einer TextBox – praktisch ungenutzt | `WpfControlLibrary1\UserControl1.xaml` |
| Excel | `Microsoft.Office.Interop.Excel` im Hauptformular | `Views\Hauptformular\FormMain.cs` |
| Mehrsprachigkeit | `HKCU\Software\wp-plan` Wert `Language` (0=de, 1=en) → `CurrentUICulture`; Satelliten-resx je Form (`.de-DE.resx`/`.en-US.resx`) + `MyResource\Resource.*.resx`; ResXManager im Repo | `Program.cs`, `ResXManager.config.xml` |
| UI-Rahmen | MDI: `Application.Run(mdifrm)` mit `MDIMainForm`; Kindfenster über `MenueCtrl.OpenForm()` / `MDIHelperClass` | `Program.cs`, `Controller\MenueCtrl.cs` |

### Datenzugriff — zwei parallele Schichten

**(a) Legacy, ODBC über System-DSN „TEST":**
```csharp
DBConnection = db.openDB("DSN=TEST");   // Program.cs
```
Globale `Program.DBConnection` (OdbcConnection); Zugriff über den Mini-Wrapper `Allgemein\RecordSet.cs` (`rs.Open(sql)` / `rs.Next()` / `rs.Read("Spalte")`). Die meisten Controller (`WaermebedarfCtrl`, `Z_ProjektBrauchwasserCtrl`, `Z_ProjektGebGanglinieCtrl`, …) und die gesamte Simulation arbeiten so — mit stringkonkateniertem SQL.

**(b) Neu, OLE DB (ACE.OLEDB.12.0) über `Allgemein\DataRepository.cs`:** statische Klasse mit `GetDataTable/ExecuteSQL/ExecuteScalar/BeginTransaction/GetMaxID`, parametrisiert (`?`-Platzhalter, `OleDbParameter`). Der DB-Pfad wird **aus der Registry des DSN** gelesen:
```csharp
string userPath = $@"SOFTWARE\ODBC\ODBC.INI\TEST";
db = key.GetValue("DBQ")?.ToString() ?? key.GetValue("Database")?.ToString();
```
`app.config` enthält daneben noch einen toten, hartkodierten ConnectionString auf `C:\Users\wg008\…`.

### Rechenkern: native DLL über Out-of-Proc-COM-Server

Alle numerischen Kernoperationen liegen **nicht** in C#, sondern in `bhkwplan.dll`, eingebunden per `DllImport` in einem eigenen COM-Server (`CSExeCOMServer.exe`):
```csharp
[DllImport("bhkwplan.dll")]
public static extern int strom_wochetojahr(Single[] wo_strombedarf, Single[] monatsverbrauch,
                                           Single[] Strombedarf, int[] mo_anfang, int[] mo_ende);
```
`CSExeCOMServer\SimpleObject.cs` / `ISimpleObject.cs`. Verfügbar sind u. a. `I_vector_init`, `CSharp_I_vectoren_addieren`, `I_vector_summe`, `I_monats_summe`, `I_normieren`, `I_heapsort`, `I_netzverlustec`, `I_Watt_To_Kw`, `I_StdWerte`, `I_TaeglHeizlastWG`, `I_strom_wochetojahr`. **Alle Signaturen sind fest auf 8760 / 168 / 365 / 12 Elemente ausgelegt.** Verwendung in der Anwendung über `public CSExeCOMServer.SimpleObject com = new CSExeCOMServer.SimpleObject();`.

## 2. Ist-Analyse: Brauchwasser-Pfad

### 2.1 Datenmodell (aus SQL-Strings rekonstruiert)

| Tabelle | Spalten (belegt) | Bedeutung |
|---|---|---|
| `Tab_Brauchwasser` | `ID`, `Bezeichner`, `Typ`, `Beschreibung`, `Monat_1` … `Monat_12` | Katalog der Brauchwasser-Profile, 12 Monatswerte |
| `Tab_Brauchwassertyp` | `ID`, `Typname`, `Beschreibung`, Spalten `"1"` … `"168"` | **Wochen-Stundenprofil 7 × 24** |
| `Z_Projekt_Brauchwasser` | `ID`, `ID_Projekt`, `ID_Brauchwasser`, `Bezeichner`, `Summe` | Projektzuordnung + überschriebener Jahresverbrauch |
| `Abfrage_Monatswaerme_Brauchwasser` | Access-Query | liefert `Bezeichner` je `ID_Projekt` |

Das 168er-Wochenprofil ist bereits heute die „Zapfprofil-nächste" Struktur; `Views\Brauchwasser\Form_EingBrauchwasserTyp.cs` editiert es:
```csharp
public double[,] arr = new double[7, 24];
...
arr[Tag, stunde] = (double)rs.Read(Tag * 24 + stunde + 3);   // Spalten ab Ordinal 3
```

### 2.2 Modelle und Controller

- `Model\BrauchwasserModel.cs` — nur Kopfdaten + `double[] m_Monat` (12).
- `Model\Z_ProjektBrauchwasserModel.cs` — `ID_Z, ID_Projekt, ID_Brauchwasser, szBezeichner, Summe` (ohne `m_`-Präfix — die Z_-Modelle brechen die Konvention).
- `Controller\BrauchwasserCtrl.cs` — neu geschrieben auf `DataRepository` (OleDb, parametrisiert), mit „Kompatibilitäts-Layer" (`rows`, `items`).
- `Controller\Z_ProjektBrauchwasserCtrl.cs` — alt, ODBC, `items = new Z_ProjektBrauchwasserModel[1000]` (feste Obergrenze, Muster überall im Projekt).

**Belegbarer Bug:** `BrauchwasserCtrl.Insert()/Update()` schreiben auf `M1 … M12`:
```csharp
string sql = @"INSERT INTO Tab_Brauchwasser (Bezeichner, Typ, Beschreibung, M1, M2, ... M12) ...";
```
gelesen wird aber `Monat_n`:
```csharp
string colName = "Monat_" + (i + 1);   // FillModelFromRow
```
Alle anderen Stellen (`Form_EingDBBrauchwasser.cs`, `SimulationWaermebedarf.cs`) verwenden ebenfalls `Monat_n`. Die Save-Pfade in `BrauchwasserCtrl` können gegen die vorhandene Tabelle also nicht funktionieren — sie werden von den gesichteten Views auch nicht aufgerufen (die schreiben stattdessen über `OdbcDataAdapter` + `OdbcCommandBuilder`).

### 2.3 UI

`Views\Brauchwasser\Form_Brauchwasser.cs` — repräsentative Maske:
- links `dataGridView1` = Katalog (`BrauchwasserCtrl.ReadAll()`), Spalten Name/Typ, Zebra-Farben;
- rechts `listView_Prozess_Auswahl` = Projektauswahl, gehalten in `List<Z_ProjektBrauchwasserModel> list_pwmodel`;
- Jahressumme = Σ der 12 Monatswerte (`Prozesssumme()`), überschreibbar (`btn_neuerWert_Click`);
- Wizard-Modus: `SetControls(string szProjekt, bool bWizard)` blendet OK/Abbrechen aus und setzt `FormBorderStyle.None`.

Die Copy-Paste-Herkunft aus der Prozesswärme ist überall sichtbar (`listView_Prozess_Auswahl`, `textBox_SummeProzesswaerme`, `Form_Prozesswaerme_Load`, `btn_ErgebnisseVerbrauch` öffnet `Form_ErgProzesswaerme`).

Katalogpflege: `Form_EingDBBrauchwasser.cs` (12 Textboxen `Wert1..Wert12`, Speichern über DataAdapter/CommandBuilder). Ergebnis: `Form_ErgBrauchwasserwaerme.cs` (12 Einzel-Textboxen `Monat_1..Monat_12` + MS-Chart via `chart1.Series[0].Points.DataBindY(simulation.Waermebedarf_Brauchwasser_Monat)`).

### 2.4 Rechenweg (Kernstelle)

`Allgemein\Simulation\SimulationWaermebedarf.cs`, `Brauchwasserwaerme_berechnen(List<string> list = null)`:
1. Profilnamen aus `Abfrage_Monatswaerme_Brauchwasser` (oder als Parameterliste aus der UI).
2. 12 Monatswerte aus `Tab_Brauchwasser` lesen; falls im Projekt eine abweichende `Summe` hinterlegt ist, linear skalieren:
```csharp
if (pjv > 0) for (int i = 0; i < 12; i++) monats_waerme[i] = monats_waerme[i] * pjv / jv;
```
3. 168 Wochenwerte aus `Tab_Brauchwassertyp` zum `Typ` des Profils.
4. Erzeugung der Jahresganglinie im nativen Kern:
```csharp
temp = com.I_strom_wochetojahr(wochen_waerme, monats_waerme, mo_anfang, mo_ende);
com.CSharp_I_vectoren_addieren(temp, brauchwasserwerte);
```
5. Einbindung in die Bilanz (`Waermebedarf_berechnen`, feste Reihenfolge):
```csharp
Brauchwasserwaerme_berechnen();
Waermebedarf_Brauchwasser = com.I_vector_summe(brauchwasserwerte);
com.I_monats_summe(brauchwasserwerte, Waermebedarf_Brauchwasser_Monat, mo_anfang, mo_ende);
com.CSharp_I_vectoren_addieren(brauchwasserwerte, Waermebedarf);
```
Gesamtkette: Gebäude → externe Ganglinien → Prozesswärme → **Brauchwasser** → Netzverluste → Normierung → `I_heapsort` + `Array.Reverse` = Jahresdauerlinie.

**Einheit:** Die Gebäude-Stundenwerte werden per `com.I_Watt_To_Kw(ref Waermebedarf)` umgerechnet; die Monatswerte aus der DB werden ohne weitere Umrechnung addiert, also implizit in derselben Einheit (kWh) erwartet. Eine explizite Einheitenangabe existiert im Code nicht — **unsicher**.

### 2.5 Lücken im Ist-Zustand (relevant für die Planung)

- **Brauchwasser fehlt im Wizard.** `Allgemein\WizardItemClass.cs` kennt Konstanten von `KOMPONENTEN_ITEM=0` bis `PUFFER_ITEM=13`, aber kein `BRAUCHWASSER_ITEM`; `MenueCtrl.ProjektNeu()`/`ProjektBearbeiten()` listen Gebäude, Wärmebedarf, Prozesswärme, Strom … aber kein Brauchwasser. Erreichbar ist es nur über `FormMain` *(genaue Einsprungstelle nicht verifiziert — FormMain nur teilweise gelesen)*.
- **Keine Lokalisierung:** `Views\Brauchwasser\` enthält nur `.resx`, keine `.de-DE.resx`/`.en-US.resx` — anders als alle anderen View-Ordner.
- **`Tab_Brauchwasser`, `Tab_Brauchwassertyp` und `Z_Projekt_Brauchwasser` fehlen in `UpdateDB.ini`** (weder in `[IMPORT]` noch `[DELETE]`) → bei einem Software-/DB-Update gehen die Brauchwasserdaten des Anwenders verloren.
- `Form_Brauchwasser.btn_Prozess_loeschen`: `"DELETE Bezeichner FROM Tab_Brauchwasser WHERE Bezeichner='" + … + "'"` — ungültige Access-Syntax-Variante plus Stringkonkatenation.

## 3. Ganglinien-Systematik

**Auflösungen**
- Wärme: durchgängig **8760 Stundenwerte** (`float[8760]`), inkl. Dauerlinie.
- Strom: **35040 Viertelstundenwerte** (`float[8760 * 4]`), `SimulationControl.Rest_Strombedarf_viertelstuendlich`, Umsetzung Stunde→Viertelstunde über `Stundenwerte_zu_viertelstunden()`.
- Klima/Gebäude: 365 Tageswerte (`Tab_Klimadaten`) × Tagesverteilung 24 h je Tagtyp (`Abfrage_Tagverteilung`, `float[192]`/`[240]`) → `com.I_StdWerte(...)` erzeugt 8760.
- Prozess/Brauchwasser: 12 Monatswerte × 168 Wochenwerte → `I_strom_wochetojahr` → 8760.

**Persistenzmuster für importierte Zeitreihen** (Header/Daten-Paar, dreifach vorhanden):

| Kopf | Daten | Projektzuordnung |
|---|---|---|
| `Tab_Waermebedarf(ID, ID_GanglinieDaten, Bezeichner)` | `Tab_WaermebedarfDaten(ID_GanglinieDaten, Wert)` | `Z_ProjektWaermebedarf(ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner)` |
| `Tab_Stromganglinie` (Model hat zusätzlich `m_Zeitinterval`) | `Tab_StromganglinieDaten(ID_Ganglinie, Wert)` | `Z_ProjektStromganglinie` |
| `Tab_Solarganglinie` | `Tab_SolarganglinieDaten` | `Z_ProjektSolarganglinie` |

Je Zeitschritt eine Zeile. Bemerkenswert: `Model\StromganglinieModel.cs` führt bereits ein Feld `m_Zeitinterval` mit — d. h. eine variable Zeitauflösung ist im Datenmodell schon vorgedacht.

**Import:** `Views\Wärmebedarf\Form_AdminWaermeeinlesen.cs` liest `.txt` (ein Wert pro Zeile) aus `%LOCALAPPDATA%\WP-Plan\Waermebedarf`, legt den Header an (`WaermebedarfCtrl.Insert()`) und schreibt die Werte. Zwei Qualitätsstufen:
- `Controller\WaermebedarfDatenCtrl.cs`: Einzel-INSERT je Zeile über ODBC — langsam.
- `Controller\StromganglinieDatenCtrl.cs`: **gebündelt in einer `OleDbTransaction` mit wiederverwendeten Parametern** — das ist die Vorlage für Neuentwicklung.

**Bilanz/Dauerlinie:** `Allgemein\Simulation\SimulationControl.cs` verkettet die Erzeuger in der per `KonfigurationModel.m_Tool_1..m_Tool_6` konfigurierten Reihenfolge:
```csharp
Eingang = simulation_Waermebedarf.Waermebedarf;   // Startpunkt der Simulation
// "Wärmepumpe" → "Heizkessel" → "Solarthermie"  (jeweils Rest-Wärmebedarf als neuer Eingang)
// danach "Photovoltaik" und "Stromspeicher" auf der Stromseite
```
Ergebnisse landen (laut `UpdateDB.ini`) in `Tab_Simulation_Ergebnis`. Einzelsimulationen: `SimulationWaermepumpe`, `SimulationSPK`, `SimulationSolarthermie`, `SimulationPV`, `SimulationSSP`, `SimulationStrombedarf`.

## 4. Kenndaten- und Persistenz-Architektur

- **Es gibt keine Projektdatei.** `Kenndaten.accdb` enthält Kataloge *und* Projektdaten. Ein Projekt = eine Zeile in `Tab_Projekt` (`ProjektModel`: `m_ID`, `m_szProjektname`, `m_szBearbeiter`, `m_szKunde`, `m_ID_Klimaregion`, `m_Erstelldatum`, `m_Aenderungsdatum`).
- **Namenskonventionen im Schema:**
  - `Tab_*` = Stamm-/Katalogdaten **und** Projektobjekte (`Tab_Projekt`, `Tab_Energieanlagen`)
  - `Z_*` = Zuordnung Projekt ↔ Katalogobjekt, Schema stets `ID/ID_Z`, `ID_Projekt`, `ID_<Katalog>`, `Bezeichner`, optional `Summe`
  - `DB*` = ältere Tabellen (`DBGebaeude`, `DBTagV`, `DBTagVDaten`, `DB-Heizung`)
  - `Abfrage_*` = gespeicherte Access-Abfragen (`Abfrage_Tagverteilung`, `Abfrage_Monatswaerme_Brauchwasser`, `Abfrage_ProjektGebaeudeGanglinie`)
- **Achtung Begriff:** `KenndatenModel`/`Tab_Kenndaten` ist **nicht** der allgemeine Katalog, sondern das **WP-Kennfeld** (`ID_WP, Vorlauf, Temperatur, COP, Ptherm`); analog `Tab_Kenndaten_Kuehlung`. Ein „Kenndaten-Katalog-Framework" gibt es nicht — jedes Fachobjekt hat eigene Tabellen und ein eigenes Model/Ctrl-Paar.
- **Projekt-Speichern** läuft zentral über `Controller\WizardCtrl.cs` — dort liegen alle `Add_*`/`Del_*`-Methoden (`Add_Projekt`, `Add_Projekt_ZuordungGebäude`, `Add_WaermebedarfExtern`, `Add_Projekt_Prozess`, `Add_Projekt_Brauchwasser`, `Add_Stromganglinie`, …), durchgängig über `DataRepository` mit `OleDbParameter`. IDs per `DataRepository.GetMaxID(tabelle) + 1`.
- **Versionierung/Migration:** `UpdateDB.ini` + `Allgemein\DbClass.cs` + `Views\Update\Form_Update`. Beim SW-Update wird die Anwender-DB nach `%LOCALAPPDATA%\WP-Plan` gesichert; die INI enthält `[TABELLEN]`/`[SPALTEN]`/`[DATENTYPEN]` als DDL-Skripte (auf die **gesicherte** DB angewendet), `[IMPORT]` (37 Tabellen, die zurückgespielt werden) und `[DELETE]` (23 Tabellen, die in der neuen DB zuvor geleert werden). `DB-Backup\` enthält manuelle Stände — keine automatische Versionierung.
- `Klima\` enthält ~300 TRY-Datensätze als `.xls` je PLZ/Ort für 2015 und 2045.

## 5. Integrationsvorschlag Zapfprofil-Modul

### 5.1 Neue Klassen (Ablageorte und Namen konventionskonform)

**`WindowsFormsApplication1\Model\`**
- `ZapfprofilModel.cs` — Kopfobjekt: `m_ID`, `m_szBezeichner`, `m_szNutzungsart`, `m_szBeschreibung`, `m_nZeitinterval` (Minuten), `m_nAnzahlWerte`, `m_dJahresvolumen`, `m_dKaltwassertemperatur`, `m_dZapftemperatur` (Präfixe analog `BrauchwasserModel`).
- `ZapfprofilDatenModel.cs` — `m_ID_ZapfprofilDaten`, `m_Wert` (analog `WaermebedarfDatenModel`, `StromganglinieDatenModel`).
- `NutzungsartModel.cs` — Katalog der Nutzungsarten mit Kennwerten.
- `Z_ProjektZapfprofilModel.cs` — `ID_Z`, `ID_Projekt`, `ID_Zapfprofil`, `szBezeichner`, `Summe`, optional `ID_Gebaeude` (naheliegend, weil `Z_ProjGebModel` bereits `DezentralWarmwasser` führt).

**`WindowsFormsApplication1\Controller\`**
- `ZapfprofilCtrl.cs`, `ZapfprofilDatenCtrl.cs`, `NutzungsartCtrl.cs`, `Z_ProjektZapfprofilCtrl.cs` — konsequent über `DataRepository` (OleDb, parametrisiert), **nicht** über `Program.DBConnection`/`RecordSet`. Massen-Insert der Zeitreihe exakt nach `StromganglinieDatenCtrl.Insert()` (eine `OleDbTransaction`, Parameter einmal definiert).
- `WizardCtrl.cs` erweitern um `Add_Projekt_Zapfprofil` / `Del_Projekt_Zapfprofil` (1:1 analog `Add_Projekt_Brauchwasser`).

**`WindowsFormsApplication1\Views\Zapfprofil\`**
- `Form_Zapfprofil.cs` (Projektzuordnung, Muster `Form_Brauchwasser` inkl. `SetControls(string szProjekt, bool bWizard = false)`), `Form_EingDBZapfprofil.cs` (Katalogpflege), `Form_ErgZapfprofil.cs` (Ergebnis + Chart).
- **Von Anfang an mit `.resx` + `.de-DE.resx` + `.en-US.resx`** — hier den Brauchwasser-Fehler nicht wiederholen.

**`WindowsFormsApplication1\Allgemein\Simulation\`**
- `SimulationZapfprofil.cs` oder — minimalinvasiver — eine Methode `Zapfwaerme_berechnen()` in `SimulationWaermebedarf`, die `zapfwerte[8760]` erzeugt.

### 5.2 Was ersetzt, was erweitert wird

**Erweitern, nicht ersetzen.** `Tab_Brauchwasser`/`Tab_Brauchwassertyp`/`Z_Projekt_Brauchwasser` bleiben (Bestandsprojekte referenzieren sie). Das Zapfprofil wird zweite, parallele Quelle des TWW-Anteils.

Konkrete Eingriffsstelle — heute steht in `Waermebedarf_berechnen()` unbedingt:
```csharp
// Brauchwasserwärme
Brauchwasserwaerme_berechnen();
Waermebedarf_Brauchwasser = com.I_vector_summe(brauchwasserwerte);
```
Dort eine Weiche einbauen (Monatswert-Pfad **oder** Zapfprofil-Pfad, je Projekt umschaltbar über ein neues Feld in `KonfigurationModel`), damit TWW nicht doppelt zählt. Die Ergebnisfelder `Waermebedarf_Brauchwasser` und `Waermebedarf_Brauchwasser_Monat[12]` unbedingt beibehalten — dann laufen `Form_ErgBrauchwasserwaerme`, `Form_Simulation_*` und die Navigatoren unverändert weiter.

Ersetzt wird nur die *Profilbildung*: statt `I_strom_wochetojahr(168 Wochenwerte, 12 Monatswerte)` eine echte Zapf-Zeitreihe. Die lineare Skalierung auf den projektspezifischen Jahreswert (`monats_waerme[i] * pjv / jv`) beibehalten — die Bediener kennen dieses Verhalten.

### 5.3 Nutzungsarten-Katalog: Access oder JSON?

**Empfehlung: Access (`Kenndaten.accdb`).** Neue Tabellen:
- `Tab_Zapf_Nutzungsart` (Stammwerte, wenige Zeilen)
- `Tab_Zapfprofil` (Kopf, inkl. `Zeitinterval`) + `Tab_ZapfprofilDaten` (Werte) — Header/Daten-Muster wie bei allen anderen Ganglinien
- `Z_Projekt_Zapfprofil` (Zuordnung)

Begründung aus der vorgefundenen Praxis:
1. **Ausnahmslos alle** Fachkataloge liegen in der Access-DB; es gibt im Repo kein einziges JSON-Beispiel für Fachdaten (`System.Text.Json`/`JsonSchema.Net` sind Transitiv-Abhängigkeiten von `Mscc.GenerativeAI`).
2. Nur DB-Tabellen werden vom Migrationsmechanismus erfasst (`UpdateDB.ini [IMPORT]`) und vom Backup-Konzept (`DB-Backup\`) abgedeckt. Eine JSON-Datei im Programmverzeichnis würde bei jedem Update Anwenderänderungen verlieren.
3. Die gesamte Admin-UI (`Form_*_Admin`, `Form_Eing*`) ist auf Recordsets ausgelegt; ein JSON-Katalog hätte keinen Editor.

Gegenargument, falls der Nutzungsartenkatalog **INEKON-fest** und für den Anwender nicht editierbar sein soll: dann wäre eine JSON-Embedded-Resource unter `Resources\` vertretbar (auslieferungsfest, in Git diffbar). Dann aber bewusst read-only und ohne Editor. Für Anwender-editierbare Profile bleibt Access die konsistente Wahl.

**In beiden Fällen zwingend:** `UpdateDB.ini` erweitern — `[TABELLEN]` um die `CREATE TABLE`-Skripte, `[IMPORT]`/`[DELETE]` um die neuen Tabellen (Zähler `ANZAHL` hochsetzen). Bei der Gelegenheit die **fehlenden Brauchwasser-Tabellen nachtragen**.

### 5.4 Wiederverwendbare Infrastruktur

| Baustein | Datei | Eignung |
|---|---|---|
| Zeitreihen-Persistenz mit Transaktion | `Controller\StromganglinieDatenCtrl.cs` | direkt als Vorlage kopierbar |
| Textdatei-Import (ein Wert je Zeile) | `Views\Wärmebedarf\Form_AdminWaermeeinlesen.cs` + `ToolsClass.OpenText` | direkt übernehmbar |
| Chart inkl. Zoom/Tooltip | `Allgemein\GrafikTools\ChartManagerNeu.cs` | `AddSeries(name, color, float[])`, `IsQuarterHourly`, Datums-/Zahlenachse, Mausrad-Zoom |
| Vektoralgebra / Dauerlinie / Monatssummen | `CSExeCOMServer.SimpleObject` | nur solange das Ergebnis ein `float[8760]` ist |
| Stunde → Viertelstunde | `SimulationControl.Stundenwerte_zu_viertelstunden()` | Vorbild für die umgekehrte Aggregation |

Für 15-Minuten-Auflösung ist `ChartManagerNeu` bereits vorbereitet (`IsQuarterHourly`); für 1-Minuten-Zapfprofile `IsQuarterHourly` (bool) zu einem allgemeinen Intervall-Parameter (Minuten pro Schritt) verallgemeinern — kleiner, lokaler Eingriff an zwei Klassen.

**Vorsicht:** `ChartManager.cs`, `ChartManagerNeu.cs`, `Form_ChartZoom.cs` und `TextBoxExtensionsClass.cs` existieren **doppelt** in `Allgemein\Chart\` und `Allgemein\GrafikTools\`. Vor Wiederverwendung anhand der `.csproj`-Compile-Items klären, welche Kopie tatsächlich gebaut wird.

### 5.5 Risiken und Altlasten

1. **Nativer Kern `bhkwplan.dll` über Out-of-Proc-COM.** x86-Bindung, Registrierung erforderlich, kein Quellcode im Repo gesichtet, nicht unit-testbar. Alle Signaturen auf 8760/168/365/12 fixiert — **jede feinere Zeitauflösung verlässt diesen Kern.** Empfehlung: Zapfprofil intern feiner (1/5/15 min) in reinem C# rechnen (MathNet.Numerics vorhanden) und erst am Übergabepunkt auf `float[8760]` aggregieren.
2. **Zwei parallele DB-Schichten.** Neues Modul ausschließlich über `DataRepository`; die Simulation bleibt auf `RecordSet`/ODBC — Mischbetrieb unvermeidbar. Der DSN „TEST" ist harte Voraussetzung: fehlt er, bricht `Program.Main` mit MessageBox ab.
3. **Access/ACE 32-vs-64-Bit** (`AccessDatabaseEngine.exe` im Repo); DB-Größenlimit 2 GB. 8760/35040 Zeilen je Profil unkritisch, aber zeilenweise Inserts ohne Transaktion sind zu langsam.
4. **`UpdateDB.ini`-Lücke bei Brauchwasser** — bei Nichtbeachtung stille Datenverluste auch beim neuen Modul.
5. **Keine Tests, keine Schichtentrennung.** Kein Testprojekt in der Solution; `Ctrl` erbt vom `Model`, Views enthalten SQL. Eingriffe in `SimulationWaermebedarf.Waermebedarf_berechnen()` sind regressionsanfällig — Weiche additiv, Default „alt".
6. **Verknüpfung über `Bezeichner` statt IDs** in nahezu allen SQL-Strings. Apostrophe/Sonderzeichen in Namen brechen Abfragen. Neues Modul konsequent über IDs.
7. **Kodierung.** Etliche `.cs`-Dateien nicht in UTF-8 (Umlaute als `�`, z. B. `BrauchwasserCtrl.cs`). Neue Dateien konsequent UTF-8 mit BOM.
8. **MS Chart ist eingefroren** (in .NET 5+ nicht enthalten). Für Konsistenz trotzdem MS Chart + `ChartManagerNeu` im neuen Modul; ScottPlot 5 (vorhanden) ist der spätere Migrationspfad, nicht punktuell einführen.
9. **Wizard-Konstanten sind Listenindizes** (`listPages[WizardItemClass.KOMPONENTEN_ITEM]`). Neue Konstante `ZAPFPROFIL_ITEM` **ans Ende** (= 14) und in `MenueCtrl.ProjektNeu()` **und** `ProjektBearbeiten()` einfügen — sonst verschieben sich alle bestehenden Indizes.

## Kennzeichnung von Unsicherheiten

- **Einheiten** (kWh vs. MWh) der Monatswerte in `Tab_Brauchwasser`/`Tab_Prozesswaerme` aus dem Code nicht eindeutig belegbar.
- **`FormMain.cs`/`MDIMainForm.cs` nur teilweise gelesen** — genaue Einsprungstelle für `Form_Brauchwasser` nicht verifiziert.
- **DB-Schema ausschließlich aus SQL-Strings rekonstruiert** (die `.accdb` wurde nicht geöffnet): Spaltentypen, Indizes, Beziehungen und Inhalt der `Abfrage_*`-Queries unbekannt.
- **ScottPlot/OpenTK/SkiaSharp**: in den gelesenen Dateien keine Verwendung gefunden; könnte in ungelesenen Views vorkommen.
- Ob `WpfControlLibrary1` als ProjectReference eingebunden ist, wurde nicht geprüft.
