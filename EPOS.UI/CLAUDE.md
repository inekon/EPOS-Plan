# EPOS.UI — plattformfreie Oberflächenbibliothek

Razor-Klassenbibliothek (`net10.0`, `Microsoft.NET.Sdk.Razor`) mit den Bausteinen, Standardfeldern
und Dialogen von EPOS-Plan. Unter Windows laufen die Komponenten in einer `BlazorWebView`, auf
iOS/MAUI später unverändert weiter (Umsetzungskonzept iOS, Paket iU8).

## Zweck

Ein Dialog wird **einmal** als Razor-Komponente geschrieben und von der jeweiligen Plattformhülle
angezeigt. Die Hülle liefert Parameter hinein und nimmt das Ergebnis über einen `EventCallback`
entgegen — sie ist damit austauschbar.

## Regeln

- **Kein WinForms.** Kein `System.Windows.Forms`, kein `MessageBox`, kein `DialogResult`.
  `EnableWindowsTargeting=false` in der `.csproj` lässt jeden Verstoß den Build brechen.
- **Kein `System.Drawing`.** Farben und Maße stehen als CSS Custom Properties in
  `wwwroot/epos-ui.css` (Herkunft: `WindowsFormsApplication1/Allgemein/GrafikTools/KartenStil.cs`).
- **Keine Datenbank.** Kein `DataRepository`, kein `RecordSet`, kein `DbParam`, kein SQL. Daten
  kommen ausschließlich als `[Parameter]` herein; das Laden erledigt ein Controller in der Hülle.
- **Texte über Ressourcen.** `@using WindowsFormsApplication1.MyResource` steht in `_Imports.razor`,
  also `@Resource.KAUSW_TITEL`. Solange ein Schlüssel fehlt, steht der deutsche Literaltext als
  Standardwert eines `[Parameter] string`-Textes in der Komponente — die Hülle kann ihn dann ohne
  Änderung der Komponente auf den Ressourcenschlüssel umstellen.
- **Zahlen komma- und punkttolerant**, kein Tausendertrennzeichen, invariant geparst — dieselbe
  Regel wie `WindowsFormsApplication1/Program.cs` (`ZahlParsen`/`GanzzahlParsen`). Eine Fehleingabe
  **färbt** das Feld (`epos-fehleingabe`), sie meldet nicht.
- **Beruhrungsziele mindestens 44 px** (`--epos-touchziel`), Warnfarben mit
  `@media (forced-colors: active)` absichern.
- Bezeichner und Kommentare deutsch; neue `.razor`/`.cs` UTF-8 **mit** BOM, LF.
- Jeder Baustein bekommt einen `bunit`-Test in `EPOS.UI.Tests` (Darstellung, Callback,
  Zustandsklasse).

## Bausteine (`Bausteine/`)

| Komponente | Zweck | Vorbild in WinForms |
|---|---|---|
| `Gruppenkopf` | Abschnittsbalken mit Titel, Symbol und Summe | `Views/Kosten/SectionPanel.cs` |
| `Warnbanner` | Hinweis / Warnung / Fehler, `role="alert"` | `KartenStil.WARN_*` |
| `SpeichernLeiste` | OK / Abbrechen und optional „Speichern" ohne Schließen samt Statuszeile | `Allgemein/SpeichernLeiste.cs` |
| `InfoKnopf` | 28×28-Fragezeichen, ruft `IHilfeDienst.Oeffnen` | `Allgemein/Hilfe/InfoKnopf.cs` |
| `Kachel` | Anklickbare Einstiegskarte mit Titel, Beschreibung, Status | `Views/Kosten/EinstiegsKarte.cs` |
| `Herleitungszeile` | Leise Erläuterung, optional mit Formel | Inline-Labels der Kostenmasken |
| `Kohaerenzzeile` | Text mit Zustand „stimmig" / „abweichend" | Inline-Labels der Kostenmasken |
| `Optionsgruppe` | Sich ausschließende Optionen (`fieldset role="radiogroup"`), einzeln sperrbar | die 45 `RadioButton` des Bestands |
| `Zeilenwahl` | Der runde Wahlknopf einer Rasterzeile (`aria-pressed`, 44 px) | die Zeilenmarkierung von `ListView`/`DataGridView`, die ein `Raster` nicht kennt |
| `Ueberlagerung` | Modaler Bereich **innerhalb** der Komponente — Abdunkelung, `role="dialog"`, Esc, Fokusfalle ohne JS | ein zweites modales `Form`, das es in der WebView nicht geben darf (R2) |
| `Rueckfrage` | Ja / Nein / Abbrechen über der `Ueberlagerung` | die ≈ 500 `MessageBox`-Rückfragen des Bestands |
| `Zeilenraster` | Spaltenkopf, Bearbeitungszeilen, Abschlusszeile, Summenfuß — CSS-Raster mit `display:contents` | `Views/Kosten/Form_KostenKomponente` (pnlRasterKopf + pnlZeilen + pnlFuss) |
| `Mehrfachauswahl` | Liste mit Haken samt „Alle"/„Keine" | `CheckedListBox` (`Form_Energietraeger.KatalogUebernahme`) |
| `Reiter` + `Reiterblatt` | Reiterleiste; die Blätter melden sich selbst an, ein ungewähltes wird **gar nicht** gezeichnet (`role="tablist"/"tab"/"tabpanel"`, ←/→, Pos1/Ende, 44 px) | die 21 `TabControl` mit 74 `TabPage` |
| `Kachelraster` | Reihe gleich breiter Karten, `auto-fit`/`minmax` statt gerechneter Prozentspalten | `UcBkKosten.pnlKacheln`, `UcWirtschaftlichkeit.KachelnBauen` |
| `Kennzahlkachel` | Überschrift, großer Wert, leise Herkunftszeile — **Anzeige, kein Knopf**; leerer Wert = „—" | `UcBkKosten.Kachel` |
| `Bildkarte` | Anklickbare Landkarte: ein Bild plus benannte SVG-Flächen darüber (Zeigen, Wählen, Übernehmen per Doppelklick) — **mit Tastatur**, jede Fläche ein Fokusziel | `Allgemein/GrafikTools/KlimazonenKarte.cs` (Regex über eine eingebettete SVG, iU9‑W10a.0e) |
| `Fortschritt` | Balken, Text und Abbrechen einer laufenden Rechnung. `Anteil = null` heißt **unbestimmt** (der Balken läuft) — ehrlicher als eine erfundene Prozentzahl; **ohne `Abbrechen`-Rückruf kein Knopf** (iU9‑W11a.7) | `Views/Stromspeicher/Form_SpeicherOptimierung.cs` (`bar_Fortschritt`, `lbl_Status`, `btn_Abbruch` — die einzige nebenläufige Rechnung des Bestands) |
| `Schema` | Das Hydraulikschema als **SVG**: vier Spalten, Knoten als Rundeck, Kanten als Bézier mit Pfeilspitze und Prioritätskreis, Kaskadenband und Legende. Die Anordnung kommt fertig aus dem Kern (`SchemaLayout`), die Farben aus `epos-ui.css`; jeder Kasten und jedes Bandglied ist ein Fokusziel | `Views/Simulation/SchemaAnsicht.cs` (789 Z. GDI+, iU9‑W10b.0c) |
| `ErzeugerKachel` | Eine Anlage der Simulationskonfiguration: Rang, Titel, Chips in sechs Stilen mit sechs Editorzielen, ▲▼✎+× und ein Aufklappbereich — **acht Ereignisse** | `Views/Simulation/ErzeugerKarte.cs` (781 Z., iU9‑W10b.0d) |
| `SpeicherKachel` | Ein Projekt-Pufferspeicher, zugeklappt eine Zeile: Badges, Flächenchips, Kurzbilanz; aufgeklappt die Detailzeilen und das Schwellenband (Inline-SVG) | `Views/Simulation/SpeicherKarte.cs` (551 Z. samt `SchwellenBand`, iU9‑W10b.0d) |

## Standards (`Standards/`)

`Zahlenfeld`, `Ganzzahlfeld`, `Textfeld`, `Auswahlfeld`, `Datumsfeld`, `Schalter`,
`Dateiwahl`, `Raster<TZeile>` (um `QuickGrid`), `ChartBild` (PNG aus dem Kern-Renderer als
`data:`-URL).

`Zahlenfeld`, `Ganzzahlfeld`, `Auswahlfeld` und `Schalter` führen `Aktiv` (Vorgabe `true`):
Ein gesperrtes Feld bleibt **sichtbar und lesbar**. Der Tarifdialog sperrt damit den Block des
nicht gewählten Rechenmodells, statt ihn auszublenden — die Werte des anderen Modells gehen so
nicht verloren (iU9‑W2.3, Vorbild `Form_Tarifstruktur.ModusUebernehmen`).

`Textfeld` führt seit iU9‑W3.0 `Mehrzeilig`/`Zeilen`/`NurLesen` und (seit W3.2) `Festbreite`:
dasselbe Feld als `textarea` — der Ersatz für die MultiLine-`TextBox` (Protokolle). `NurLesen`
lässt den Inhalt markierbar, anders als ein gesperrtes Feld.

`Raster` führt `Bearbeitbar`: Bedienelemente in den Zellen stehen in `TemplateColumn`s
(`Schalter`, `Zahlenfeld`); die Klasse nimmt der Zelle nur die senkrechte Polsterung, damit ein
44‑px‑Feld die Zeile nicht auf 60 px treibt.

`Zahlenfeld` und `Ganzzahlfeld` führen seit iU9‑W6.1 `Feldname` und `FehlerZustand`:
Der WinForms-Bestand prüft beim Speicherknopf jedes Zahlenfeld einzeln
(`Program.ZahlPruefen(feld, "Thermische Leistung", …)`) und nennt in der Meldung genau
das Feld, an dem es hängt. Das Feld färbt weiterhin während der Eingabe und meldet
zusätzlich SEINEN NAMEN an den Dialog — so bleibt die Regel, ohne dass ein Dialog
fünfzehn `@ref` auf seine Felder halten muss.

`Dateiwahl` (Pfadfeld + Knopf „Durchsuchen…") **öffnet nichts**: Der Wähler kommt als
`Func<string, Task<string?>>` herein — unter Windows aus `Dienste.Datei.DateiOeffnen`, auf iOS
aus der Dokumentenauswahl. Ohne Delegat bleibt der Knopf weg.

## Dienste (`Dienste/`)

Drei Schnittstellen nach außen — mehr sieht diese Bibliothek von der Umgebung nicht.

| Schnittstelle | Wofür | Windows | iOS | ohne Umgebung |
|---|---|---|---|---|
| `IHilfeDienst` | Hilfetext und Wikiseite zu einem Schlüssel (`InfoKnopf`) | `WindowsHilfeDienst` am `HelpCatalog` | `IosHilfeDienst` (iU10-5) | `KeineHilfe` |
| `IProjektQuelle` | die Daten der **Seiten**: Projektliste, Energieträgerliste, BHKW-Parametersatz, Übernahme des Anlegeergebnisses, seit iU9‑W10b der Parametersatz der Simulationskonfiguration (**mit Standardumsetzung** `null`, damit eine vorhandene Quelle durch die Erweiterung nicht bricht) | — (dort ist die Startmaske der Einstieg) | `IosProjektQuelle` (iU10-7) | `KeineProjekte` |
| `INavigationsZiel` | die **Gegenrichtung** zu `WindowsFormsApplication1.INavigation`: Was eine Oberfläche anbieten muss, damit ein Plattformadapter sie öffnen kann | `WinFormsNavigation` braucht sie nicht | `IosNavigation` reicht dorthin weiter | `Navigationsziel.Aktuell = null` |

`SeitenZustand` (iU9‑W5.0) ist keine Schnittstelle, sondern ein **Objekt mit
Änderungsereignis**: Eine `BlazorDialogForm` setzt ihre Parameter einmal, beim Aufbau —
ein Dialog lebt kurz. Eine **Seite** lebt so lange wie ihre Maske, und unter ihr wechselt
das Projekt. Die Hülle schreibt (`ProjektSetzen`, `Auffrischen`), die Komponente hängt
sich an `Geaendert` und zeichnet neu; die WebView bleibt dieselbe. Gelesen wird im
Blazor-Verteiler, geschrieben aus dem Oberflächenfaden — die Komponente ruft deshalb
`InvokeAsync`, bevor sie zeichnet.

`Navigationsziel` ist der statische Halter der zuletzt gezeichneten Wurzel — dasselbe Muster
wie `Dienste` im Kern, aus demselben Grund: Der Adapter entsteht beim Programmstart, die
Komponente erst beim Zeichnen.

## Seiten (`Seiten/`)

Was eine **Hülle ohne eigene Fenster** braucht (Paket iU10, iOS). Ein Dialog wird dort nicht in
einem zweiten Fenster geöffnet, sondern löst die Ansicht ab.

| Komponente | Zweck |
|---|---|
| `AppWurzel` | die Wurzelkomponente der iOS-Hülle: eine Zustandsmaschine über `Seitenschluessel` (Liste ↔ Dialog), Registrierung als `INavigationsZiel`, Statuszeile nach einem Dialog. **Noch kein Router** — der Wizard nach iL5 ist iU10-9 |
| `Projektliste` | der Einstieg: Nr., Projekt, Klimaregion, Ausstattung im `Raster` und je Zeile zwei Knöpfe, die einen Maskenschlüssel melden |
| `Seitenschluessel` | die fünf sprachneutralen ASCII-Schlüssel (`PROJEKTLISTE`, `ENERGIETRAEGER_VARIANTE`, `BHKW_WIRTSCHAFTLICHKEIT`, `SIMULATION_KONFIGURATION`, `SIMULATION_ERGEBNIS`) |
| `Simulation/SimulationKonfigSeite` | die **Simulationskonfiguration** (iU9‑W10b.1) — die erste FACHSEITE, die iOS über `AppWurzel` erreicht. Unter Windows steht dieselbe Komponente bis W16 in einer modalen Dialoghülle (Entscheid R‑W10b‑1), weil ihre beiden Aufrufer die modale Rückkehr brauchen. Datenseite: `Views/Simulation/SimulationKonfigHuelle.cs` |
| `Simulation/SimulationErgebnisSeite` | das **Simulationsergebnis** (iU9‑W11b.13) — die zweite Fachseite für `AppWurzel`, unter Windows ebenfalls bis W16 modal (Entscheid R‑W11‑1, 1 474 × 821). **Ein `Reiter` für zehn Blätter**: Parameter (mit fünf Unterblättern), Übersicht, Bedarf, Wärmepumpe, Heizkessel, Solarthermie, BHKW, Photovoltaik, Stromspeicher, Ergebnis — dazu vier Überlagerungen. Datenseite: `Views/Simulation/SimulationErgebnisHuelle.cs` in vier Teildateien |
| `Simulation/ParameterReiter` … `SpeicherVariantenVergleich` | die **zwölf Reiterkomponenten** der Ergebnisseite. Sie nehmen die DTO aus `EPOS.Kern/Controller/SimulationErgebnisCtrl` unmittelbar entgegen — die sind für genau diese Seite gebaut worden (iU9‑W11a.3) und wären als zweite Datenform eine zweite Wahrheit. Die Bilder kommen als PNG aus dem Kern-Renderer, **erst beim Betreten eines Reiters** und je Schalterstellung zwischengespeichert |

**`Seiten/Berichte/` — der Reiter „Berichte & Kosten" (iU9‑W5).** Die erste Gruppe von
Seiten, die unter **Windows** läuft: `Form_Start.tabPage6` trägt eine
`BlazorSeite<BerichteKostenSeite>` — eine WebView für alle vier Seiten (Risiko R5).

| Komponente | Vorbild in WinForms | Datenseite |
|---|---|---|
| `BerichteKostenSeite` | `UcBerichteKosten` (810 Z., K4) | `Views/BerichteKosten/BerichteKostenHuelle.cs` |
| `UebersichtSeite` | `UcBkUebersicht` (1 552 Z., K4) | `Views/BerichteKosten/UebersichtSeiteGaben.cs` |
| `KostenSeite` | `UcBkKosten` (1 311 Z., K4) | `Views/BerichteKosten/KostenSeiteGaben.cs` |
| `WirtschaftlichkeitSeite` | `UcWirtschaftlichkeit` (831 Z.) | `Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` |
| `BerichtSeite` | `UcBericht` (508 Z.) | `Views/Bericht/BerichtSeiteGaben.cs` |

**Eine Seite ist kein Dialog.** Sie hat keine `Geschlossen`-Rückgabe und keine
Schlussleiste; sie lädt über `Laden`, meldet über Rückrufe und frischt sich selbst auf.
Wo ihre Spalten zur **Laufzeit** entstehen (die Vergleichstabelle je Version, die zehn
Trägerspalten, die Zeilenfarben der Kostenseite), steht eine gewöhnliche `<table>` mit der
Hausklasse `epos-raster` statt eines `Raster` — ein `QuickGrid` braucht seine Spalten zur
Übersetzungszeit.

## Dialoge (`Dialoge/`)

Je Fachbereich ein Ordner. `Dialoge/Kosten/EnergietraegerVarianteDialog.razor` ist der erste
migrierte Dialog (Vorbild `Views/Kosten/Form_Kosten_Auswahl`).

| Dialog | Vorbild in WinForms | Datenseite |
|---|---|---|
| `Kosten/EnergietraegerVarianteDialog` | `Form_Kosten_Auswahl` (iU8‑9) | `EnergietraegerVarianteCtrl`, Aufrufer inline |
| `Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog` | `Form_BhkwWirtschaftlichkeit` (B5b) | `BhkwWirtschaftlichkeitHuelle` |
| `Kosten/VorlagenPositionDialog` | `Form_VorlagenPosition` (iU9‑W1.1) | Aufrufer inline |
| `Allgemein/NamensDialog` | **fünf** Masken: `Form_VariantenName`, `Form_KostenItemNeu` (iU9‑W1.2), `Form_StromspeicherItemNeu` mit 28 Aufrufern, `Form_GebaeudetypNeu`, `Form_AlsVariante` (iU9‑W2.1) | keine; Windows-Helfer `NamensDialogHuelle` (`Bezeichner`, `BezeichnerUndBeschreibung`, `FragenMitHinweis`) |
| `Kosten/CaseEingabeDialog` | `Form_CaseEingabe` (iU9‑W1.3) | Aufrufer inline |
| `Kosten/VorlagenUebernahmeDialog` | `Form_VorlagenUebernahme` (iU9‑W1.4) | `VorlagenUebernahmeHuelle` (3 Delegaten) |
| `Kosten/KostenfaktorKatalogDialog` | `Form_KostenAdmin` (iU9‑W1.5) | `KostenfaktorKatalogHuelle` → `KostenfaktorCtrl` |
| `Wirtschaftlichkeit/KapitalwertVerlaufDialog` | `Form_WirtschaftlichkeitVerlauf` (iU9‑W1.6) | `KapitalwertVerlaufHuelle` (`Task.Run` + `ChartRenderer`) |
| `Wirtschaftlichkeit/TarifstrukturDialog` | `Form_Tarifstruktur` (iU9‑W2.3, K4) | `TarifstrukturHuelle` → `WirtschaftlichkeitCtrl.LadeTarif`/`SpeichereTarif` |
| `Wirtschaftlichkeit/PhotovoltaikVerguetungDialog` | `Form_PhotovoltaikVerguetung` (iU9‑W2.4) | `PhotovoltaikVerguetungHuelle` (4 Delegaten, u. a. Gesetzeskatalog und Marktwert-Import) |
| `Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog` | `Form_WirtschaftlichkeitParameter` (iU9‑W2.5, K4) | `WirtschaftlichkeitParameterHuelle` + **Sprungbrücke** |
| `Kosten/LeistungspreisReiheDialog` | `Form_LeistungspreisReihe` (iU9‑W3.1) | `LeistungspreisReiheHuelle` → `PreisreiheCtrl` |
| `Kosten/SpotpreisImportDialog` | `Form_SpotpreisImport` (iU9‑W3.2) | `SpotpreisImportHuelle` (Dateiwahl über `Dienste.Datei`, Prüfen und Schreiben in `Task.Run`) |
| `Kosten/EmissionskatalogDialog` | `Form_Emissionskatalog` (iU9‑W3.3) | `EmissionskatalogHuelle` → `EmissionskatalogCtrl`/`EmissionenCtrl`; die beiden Untereditoren sind eingerückte Blöcke, keine zweiten Fenster (R2) |
| `Kosten/KostenprofilDialog` | `Form_Kostenprofil` (iU9‑W3.4) | `KostenprofilHuelle` (`PreisModell.AusMonatsUndWochenwerten` + `ChartRenderer.Kostenprofil` in `Task.Run`) |
| `Kosten/VorlagenZeile` | `ucVorlagenZeile` (iU9‑W4.1) | keine; eine Zeile des Positionsrasters, der Wirt hält die Werte |
| `Kosten/ErtragBonus` | `ucErtragBonus` (iU9‑W4.1) | `ErtragBonusGaben` (Gesetzeskatalog → fertige Sätze) + Sprungbrücke |
| `Kosten/KostenKomponenteDialog` | `Form_KostenKomponente` (iU9‑W4.2) | `KostenKomponenteHuelle` → `KostenVorlagenCtrl`/`KostenProjektPositionenCtrl`; **fünf** Unterdialoge in Überlagerungen |
| `Kosten/StromAufschlaege` | `ucStromAufschlaege` (iU9‑W4.3) | im Wirt; Summen aus `StromAufschlagCtrl.AlsAufschlagssatz` |
| `Kosten/BrennstoffBestandteile` | `ucBrennstoffBestandteile` (iU9‑W4.3) | im Wirt; Summen aus `BrennstoffBestandteilCtrl` |
| `Kosten/EnergietraegerEinstellungen` | `ucFuelSettings` (iU9‑W4.4, 2 103 Z.) | `EnergietraegerHuelle` → **`EnergietraegerPreisCtrl`** (neun SQL-Anweisungen, neu im Kern) |
| `Kosten/EnergietraegerDialog` | `Form_Energietraeger` (iU9‑W4.4) | dieselbe Hülle; **vier** Unterdialoge in Überlagerungen |
| `Berichte/BkUebernahmeDialog` | `Form_BkUebernahme` (iU9‑W5.1) | `UebersichtSeiteGaben` — ein Dialog, zwei Füllungen (Wertgegenüberstellung oder Klartext) |
| `Kosten/KostenKnoepfeLeiste` | `Views/Kosten/KostenKnoepfe.Leiste` (iU9‑W6.0f) | keine; zwei Delegaten, ohne sie kein Knopf |
| `Erzeuger/HeizkesselKatalogDialog` | `Form_Heizkessel_Bearbeiten` (iU9‑W6.1) | `HeizkesselHuelle` → `HeizkesselStammCtrl.Ueberschreiben`/`Anlegen`, `EmissionsVorgaben` |
| `Erzeuger/BhkwKatalogDialog` | `Form_DBBHKW` (iU9‑W6.2) | `BhkwHuelle` → `BHKWStammCtrl.Ueberschreiben`/`Anlegen`, `BHKWKosten` |
| `Erzeuger/HeizkesselDialog` | `Form_Heizkessel` (iU9‑W6.3) | dieselbe Hülle; **zwei** Unterdialoge in Überlagerungen, Sprungbrücke zur Katalogverwaltung |
| `Erzeuger/BhkwDialog` | `Form_BHKWEing` (iU9‑W6.4) | dieselbe Hülle; **drei** Unterdialoge in Überlagerungen |
| `Erzeuger/PhotovoltaikDialog` | `Form_PV` (iU9‑W6.5) | `PhotovoltaikHuelle` → `PhotovoltaikStammCtrl` |
| `Erzeuger/StromspeicherDialog` | `Form_Stromspeicher` (iU9‑W6.6) | `StromspeicherHuelle` → `StromspeicherStammCtrl` (keine neue SQL) |
| `Erzeuger/PufferspeicherDialog` | `Form_PufferSp` (iU9‑W6.7) | `PufferspeicherHuelle` → `PufferSpStammCtrl`/`PufferSpCtrl`, `AnlagenEindeutigkeit` |
| `Waermepumpe/WaermepumpenKatalogDialog` | `Form_WpFilterAuswahl` (iU9‑W7.1) | keine Hülle — `WaermepumpenKatalogFilter` im Kern |
| `Waermepumpe/KennlinienEditorDialog` | `Kenndaten` (iU9‑W7.2) | keine Hülle — `KenndatenCtrl.Abgleichen` im Kern |
| `Waermepumpe/WaermepumpeStammDialog` | `Form_WP` (iU9‑W7.3) | `WaermepumpeStammHuelle` → `WPStammCtrl`, `KenndatenCtrl`, `ChartRenderer.Kennlinien`; **zwei** Unterdialoge in Überlagerungen |
| `Waermepumpe/WaermepumpeAnlageDialog` | `Wizard_WPItem` (iU9‑W7.4) | `WaermepumpeAnlageHuelle` → `WErzeugerCtrl`, `KostenSummenCtrl`, `ProjektPuffer.TemperaturenPruefen`; **zwei** Unterdialoge, `NurLesen` für den Ansichtsweg |
| `Waermepumpe/WaermepumpenDialog` | `Form_WPAuswahl` (iU9‑W7.5) | `WaermepumpenHuelle`; Assistentenseite 7 |
| `Solarthermie/SolarkollektorKatalogDialog` | `Form_SolarDB` (iU9‑W7.6) | `SolarkollektorHuelle` → `SolarkollektorenStammCtrl` |
| `Solarthermie/SolarkollektorenDialog` | `Form_SolarKollektoren` (iU9‑W7.7) | dieselbe Hülle; Assistentenseite 8 |
| `Solarthermie/SolarganglinieDialog` | `Form_Solarganglinie` (iU9‑W7.8) | `SolarganglinieHuelle` → `SolarganglinieStammCtrl`, `Z_ProjektSolarganglinieCtrl`; Sprungbrücke zur Ganglinienverwaltung |
| `Bedarf/TypStammDialog` | **drei** Masken: `Form_EingDBStromverbraucher`, `Form_EingDBProzess`, `Form_EingDBBrauchwasser` (iU9‑W8.1) | `Views/Bedarf/TypStammHuelle.cs` → `BedarfStammCtrl` |
| `Bedarf/BedarfErgebnisDialog` | **drei** Masken: `Form_ErgStromverbraucher`, `Form_ErgProzesswaerme`, `Form_ErgBrauchwasserwaerme` (iU9‑W8.2) | `Views/Bedarf/BedarfErgebnisHuelle.cs`; keine Datenbank — die Hülle friert das Rechenobjekt ein und rendert die Bilder vorab, seit dem Entscheid W8‑O‑5 **je Einheit eine Fassung** |
| `Bedarf/TypProfilDialog` | **drei** Masken: `Form_EingStromTyp`, `Form_EingProzTyp`, `Form_EingBrauchwasserTyp` (iU9‑W8.3) | dieselbe Hüllendatei wie W8.1 → `TypProfilCtrl`, `ChartRenderer.Stundenprofil` |
| `Bedarf/GebaeudetypDialog` | `Form_EingGebTyp` (iU9‑W8.4) | `Views/Bedarf/GebaeudetypHuelle.cs` → `TagVCtrl` |
| `Bedarf/GebaeudeWohnflaecheDialog` | `Form_GebWohnflaeche` (iU9‑W9.3) | keine Datenseite; Ergebnis-Record |
| `Bedarf/GebaeudeKatalogDialog` | **zwei** Masken: `Form_Gebaeude1`, `Form_Gebaeude2` (iU9‑W9.1) | `Views/Gebäude/GebaeudeKatalogHuelle.cs` → `GebaeudeStammCtrl`, `Ferienzeit` |
| `Bedarf/GebaeudeDialog` | `Form_Gebaeude` (iU9‑W9.2) | `Views/Gebäude/GebaeudeHuelle.cs`; Assistentenseite 2, Admin-Modus, **drei** Überlagerungen |
| `Bedarf/WaermebedarfExternDialog` | `Form_Waermebedarf` (iU9‑W9.4) | `Views/Wärmebedarf/WaermebedarfExternHuelle.cs`; Assistentenseite 3, Sprungbrücke |
| `Bedarf/BedarfsProfileDialog` | **drei** Masken: `Form_Prozesswaerme`, `Form_Stromverbraucher`, `Form_Brauchwasser` (iU9‑W9.5) | `Views/Bedarf/BedarfsProfileHuelle.cs`; Assistentenseiten 4 und 5, **vier** Überlagerungen aus Welle 8 |
| `Simulation/WertAbfrage` | die Zahlenabfrage von `Form_Quellprofil` (iU9‑W10a.0f) | keine; Überlagerung im Wirt — ersetzt `Eingabefrage` für einen Aufrufer |
| `Simulation/BetriebsmodusDialog` | `Form_Betriebsmodus` (iU9‑W10a.1) | `Views/Simulation/BetriebsmodusHuelle.cs`; reiner Entscheidungsdialog, Enter belegt |
| `Simulation/KlimazonenkarteDialog` | `Form_Klimazonenkarte` + das Steuerelement `KlimazonenKarte` (iU9‑W10a.2) | keine Hülle — `KlimazonenPfade` im Kern; erscheint als Überlagerung im Erdreichdialog |
| `Simulation/QuelleErdreichDialog` | `Form_QuelleErdreich` (iU9‑W10a.3) | `Views/Simulation/QuelleErdreichHuelle.cs` → `ErdreichTemperatur`, `ErdreichAuswertung`, `VDI4640Pruefung`, `ChartRenderer.Jahresgang`; der **Simulationslauf** läuft in `Task.Run` |
| `Simulation/PufferSpProjektDialog` | `Form_PufferSp_Projekt` (iU9‑W10a.4) | `Views/Pufferspeicher/PufferSpProjektHuelle.cs` → `PufferSpCtrl`/`PufferSpStammCtrl`/`ProjektPuffer`; **16 Delegaten**, drei Rollen (Fenster + zwei Überlagerungen), Sprungbrücke |
| `Simulation/QuellePufferspeicherDialog` | `Form_QuellePufferspeicher` (iU9‑W10a.5) | `Views/Simulation/QuellePufferspeicherHuelle.cs`; WP- und Kesselzweig in EINER Maske, Pufferverwaltung als Überlagerung |
| `Simulation/QuellprofilDialog` | `Form_Quellprofil` (iU9‑W10a.6) | `Views/Simulation/QuellprofilHuelle.cs` → `QuellprofilCtrl`; **virtualisiertes** Raster mit 8 760 Zeilen |
| `Simulation/WaermesenkeDialog` | `Form_Waermesenke` (iU9‑W10a.7) | `Views/Simulation/WaermesenkeHuelle.cs` → `Z_AnlageSenkeCtrl`, `AnlagePufferVerbundCtrl`, `Ladeordnung`, `Warnkriterien`; **11 Delegaten**, Pufferverwaltung als Überlagerung |
| `Strom/GanglinieProtokollDialog` | `Form_GanglinieProtokoll` (iU9‑W12.1) | keine Hülle — `GanglinienProtokollText` im Kern; erscheint als Überlagerung in beiden Wirten der Importkette |
| `Strom/GanglinieImportOptionenDialog` | `Form_GanglinieImportOptionen` (iU9‑W12.2) | keine Hülle — `GanglinienOptionenModell` im Kern; die Vorschau kommt über einen Delegaten aus `GanglinienDatei.Vorschau` |
| `Import/ImportKonflikteDialog` | `Form_ImportKonflikte` (iU9‑W12.3) | `Views/Import/ImportKonflikteHuelle.cs` → `ImportKonfliktModell`; die Hülle bedient die **vier W13-Importmasken** und fällt mit Welle 13 |
| `Strom/StromganglinieAdminDialog` | `Form_Stromganglinie_Admin` (iU9‑W12.4) | `Views/Stromverbraucher/StromganglinieAdminHuelle.cs` → `StromganglinieStammCtrl`, `GanglinienImportAblauf`; **drei** Überlagerungen der Importkette |
| `Strom/StromganglinieDialog` | `Form_Stromganglinie` (iU9‑W12.5) | `Views/Stromverbraucher/StromganglinieHuelle.cs` → `Z_ProjektStromganglinieCtrl`, `StromganglinieStammCtrl`; Verwaltung als Überlagerung, Assistentenschnitt für W16 |
| `Strom/PeakShavingDialog` | `Form_PeakShaving` (iU9‑W12.6) | `Views/Stromspeicher/PeakShavingHuelle.cs` → `PeakShavingCtrl`, `PeakShavingEingaben`, `PeakShavingKennzahlenBlock`, `PeakShavingBild`; **beide Rechenläufe** in `Task.Run` mit `Fortschritt` |

**Fünf Masken, ein Muster** (iU9‑W6): Die Projektdialoge der Erzeuger teilen einen
Aufbau — links „ausgewählt im Projekt", rechts „aus Datenbank", dazwischen ◀ und ▶,
unten ein Detailblock — und damit **eine** Datenform,
`Dialoge/Erzeuger/ErzeugerAuswahlDaten.cs`: `ErzeugerZeile`, `KatalogZeile`,
`ErzeugerDetail`, `TraegerVorbereitung`, `AufnahmeErgebnis`. Die Zeile trägt
`Schluessel` (die Zeile) und `GeraetId` (das Gerät) GETRENNT: Zwei gleiche Kessel im
Projekt teilen sich eine Kopie in `Tab_Heizkessel`, und daran hängt die Regel, dass
„▶" die Kopie nur entfernt, wenn keine zweite Zeile mehr darauf verweist. Die
geteilte Liste gehört der Hülle und wird **an Ort und Stelle** bearbeitet; jede
Änderung geht über einen Delegaten sofort ins Modell zurück.

**Zwei Gewerke mehr am selben Muster** (iU9‑W7): `SolarkollektorenDialog` und
`SolarganglinieDialog` teilen sich `ErzeugerAuswahlDaten` mit den fünf Masken der
Welle 6 — die Trennung von `Schluessel` und `GeraetId` ist dort dieselbe Fachlage:
Zwei gleiche Kollektoren teilen sich eine Kopie in `Tab_Solarkollektoren`, und
dieselbe Ganglinie darf einem Projekt mehrfach zugeordnet sein. Die
Wärmepumpenseite hat ihre eigene Form (`WaermepumpeAnlageDaten`), weil ihre Zeile
vierzehn Felder trägt und nicht zwei.

**Zehn Masken, vier Komponenten** (iU9‑W8): Die drei Bedarfsblätter —
Stromverbraucher, Prozesswärme, Brauchwasser — sind DRILLINGE desselben Blatts.
Ihr Stammkopf, ihr Ergebnisdialog und ihr Wochen-Stundenprofil unterscheiden
sich in Titel, Typbeschriftung, Zieltabelle und einer Handvoll Meldungen, nicht
im Aufbau. Die Ausprägung ist deshalb ein **Aufzählungstyp**
(`WindowsFormsApplication1.BedarfsArt`, im Kern, weil ihn beide Seiten
brauchen) und keine Zeichenkette: Wo sie ein Text wäre, könnte eine Übersetzung
oder ein Tippfehler sie still ins Leere laufen lassen. Jede Ausprägung hat einen
EIGENEN bunit-Feldbestandstest — der Abgleich mit der Feldkarte läuft je
Ausprägung, nicht je Komponente.

**Acht Masken, fünf Komponenten** (iU9‑W9): Die vier Bedarfskacheln des
Startbilds. Zwei Muster wiederholen sich: `Form_Gebaeude1` und
`Form_Gebaeude2` bearbeiteten mit `frm.model = model` DENSELBEN Satz — sie
werden zwei `Reiterblatt` und kein Unterdialog; die drei Bedarfsblätter sind
wie in Welle 8 DRILLINGE und werden EINE Komponente mit der Ausprägung
`BedarfsArt`. **Nach Welle 9 laufen zehn der dreizehn Assistentenseiten als
Razor-Komponente** — die Seiten 2 bis 5 kamen dazu, und die
Assistentenschnittstelle trägt seither jeden Listentyp
(`IAssistentListenSeite<T>`, W9.0a).

**Eine Einheit, zwei Dialoge** (Anwenderentscheid W8‑O‑5 / W9‑O‑3 vom 04.09.2026):
Energiemengen zeigen **MWh als Vorgabe, kWh wählbar** — im `BedarfsProfileDialog` und im
`BedarfErgebnisDialog`, der aus ihm als Überlagerung kommt. Die Einheit ist deshalb KEIN
Text mehr neben der Zahl: Die Hülle nennt je Wert die **Quelleneinheit**
(`WindowsFormsApplication1.Energieeinheit`, im Kern), die Komponente rechnet auf die
gewählte Anzeigeeinheit um. Damit ist der nackte Teiler 1000 verschwunden, den nur eine
der beiden Ergebnisansichten zog (Befund W8‑B4). Beide Dialoge lesen dieselbe gemerkte
Wahl (`BedarfEinheitWahl` über `Dienste.Einstellungen`) und melden eine Änderung über
`EinheitGewaehlt` an die Hülle zurück — die Komponenten greifen selbst auf keine
Einstellungsablage zu. **Ein PNG lässt sich nicht umrechnen**: Das Säulenbild kommt in
zwei Fassungen aus der Hülle (`Monatssicht.Bild` und `.BildKWh`), weil hier kein Renderer
gerufen wird. Ein Datensatz ohne Zahl und Quelleneinheit bleibt, wie er ist — dann
erscheint auch kein Wahlfeld.

**Sieben Masken, sieben Komponenten** (iU9‑W10a): die Dialoge, die
`Form_Simulation_Config` öffnet. Hier wiederholt sich kein Muster — jede Maske
ist ein eigener Gegenstand —, dafür wandert **eine** Komponente in drei Rollen:
`PufferSpProjektDialog` erscheint als eigenes Fenster, als Überlagerung im
Quellendialog und als Überlagerung im Senkendialog, immer mit demselben
Delegatensatz. Zwei der sieben Masken hatten **keinen Designer** (Befund
W10‑B38); ihr Feldabgleich läuft gegen den Quelltext. Der Wirt
`Form_Simulation_Config` bleibt bis **W10b** WinForms.

**Eine Maske, drei Bausteine, zwei Ebenen** (iU9‑W10b): Der Wirt der sieben
Dialoge — `Form_Simulation_Config` mit 4 558 Zeilen in vier Teildateien — ist die
Seite `Seiten/Simulation/SimulationKonfigSeite`. Ihre drei Steuerelement-Klassen
werden Bausteine (`Schema`, `ErzeugerKachel`, `SpeicherKachel`), ihr Zeichenmodell
und dessen ANORDNUNG ziehen in den Kern (`SchemaModell`, `SchemaLayout`). Die Seite
führt selbst ZWEI Überlagerungsebenen — Editoren (Modus, Priorität, Quellenwahl,
Senke, Pufferverwaltung) und darunter die drei Quellendialoge; die tieferen bringen
die Dialoge der Welle 10a selbst mit. Damit steht die Kette Seite → Quelle →
Pufferverwaltung → Klimazonenkarte in EINEM Fenster.

**Sechs Masken, eine Seite** (iU9‑W11b): Die Ergebnisansicht der Simulation —
`Form_Simulation_Detail` (7 629 Z. + 3 082 Designer) mit `DashboardForm`,
`NavigatorUebersicht`, `NavigatorStrom`, `NavigatorWaerme` und
`Form_SpeicherVariantenVergleich`, zusammen 11 031 Zeilen und 21 `MessageBox` — ist
die Seite `Seiten/Simulation/SimulationErgebnisSeite` mit zwölf Reiterkomponenten.
**Gelöscht wurde maskenweise, in EINEM Schritt** (Regel R‑W11‑2): reiterweise
stünden zwei WebViews in einem Fenster. Der Vorläufer führte DREI Navigationen für
dieselbe Sache — Reiterleiste, Menüliste mit Steuerelement-Ausleihe und
`TabListMapper`, zusammen rund 700 Zeilen; hier ist es **ein** `Reiter`. Die
17 Zeichenflächen bedienen **sieben** Renderer-Bilder aus W11a. Der SIMULATIONSLAUF
läuft in `Task.Run` und meldet seine fünf Phasen an den Baustein `Fortschritt`;
er startet beim Öffnen von selbst, wie eh und je. **Eine Komponente in zwei
Rollen:** `UebersichtReiter` ist der Hauptreiter „Übersicht" UND — mit
`NurNavigator` — das erste Blatt des Ergebnisreiters.

**Sechs Masken, eine Kette** (iU9‑W12): Die AP5-Importkette der
Stromganglinien stand ZWEIMAL wörtlich im Bestand — einmal mit Ablage
(`Form_Stromganglinie_Admin`), einmal ohne (`Form_PeakShaving`). Sie ist
jetzt EIN Kern-Ablauf (`GanglinienImportAblauf`) mit zwei Ausprägungen und
drei RÜCKRUFEN; die drei Zwischenmasken — Optionen, Protokoll, Konflikte —
erscheinen als `Ueberlagerung` desselben Fensters, und jeder Rückruf wartet
auf eine `TaskCompletionSource`, die der Unterdialog beim Schließen auflöst.
`ImportKonflikteDialog` ist **Blatt vor Host mit Hülle**: Vier seiner fünf
Aufrufer sind bis Welle 13 WinForms, und die `Sprungbruecke` kann keine
Nutzlast zurückgeben (Schlüssel → `Form` → `bool`). Der Nachweis der Welle ist
der **bitgleiche Import**: zwölf Proben mit eingefrorenen Erwartungswerten,
vor jedem Umbau angelegt. `PeakShavingDialog` bringt die zweite nebenläufige
Rechnung der Oberfläche mit (`Task.Run` + `Fortschritt`, wie W11a) und das
erste Bild, das eine SEKUNDÄRACHSE braucht — dafür genügt der
Bestandsrenderer `ChartRenderer.ErzeugerStapel`, ein neuer wäre eine zweite
Wahrheit über dieselbe Zeichnung.

**Vier Ebenen Überlagerung** (iU9‑W7.5): Verwaltung → Anlage → Stammdialog →
Kennlinien-Editor, alles in EINEM Fenster. Jeder Wirt prüft seine eigenen
Überlagerungsschalter, bevor er Esc für sich auswertet — so schließt Esc immer nur
die oberste Ebene.

**Ein Dialog IN einem Dialog** (iU9‑W4.0): Seit es `Ueberlagerung` gibt, öffnet ein
Blazor-Dialog seine Unterdialoge **im selben Fenster** statt in einer zweiten
`BlazorWebView` (Risiko R2). Der Wirt hält den Parametersatz des Unterdialogs
(`IReadOnlyDictionary<string, object>`, von der Hülle als `Gaben()` geliefert) und splattet
ihn mit `@attributes`; `Geschlossen` setzt er selbst. So läuft es in
`KostenKomponenteDialog` (fünf Unterdialoge) und `EnergietraegerDialog` (vier). Ein
zweites Fenster bleibt nur, wo der Wirt selbst WinForms ist.

**Ein Dialog gibt sein Ergebnis über `EventCallback<T?> Geschlossen` zurück**, `null` bei Abbruch;
geschlossen wird das Fenster von der Hülle. Wo der Aufrufer Werte in ein Fachobjekt zurückschreibt,
liefert der Dialog einen **Ergebnis-Record** (`*Ergebnis.cs`) statt in das übergebene Objekt zu
schreiben — die Komponente kennt die Fachklassen des Kerns nicht.

**Weiterführen aus einem Dialog** (iU9‑W2.2): Ein Dialog, der ein anderes Fenster öffnen soll,
nimmt `[Parameter] Func<string, Task<bool>>? Sprung` und ruft ihn mit einem Schlüssel aus
`Dialoge/Allgemein/Sprungziel.cs`. Was erscheint, entscheidet die Plattformhülle — unter
Windows `Sprungbruecke` (Schlüssel → `Form`, modal über dem Dialog). **Nur für
WinForms-Ziele.** Ist das Ziel selbst eine Blazor-Hülle, bleibt der Sprung *nachgelagert*
(schließen → Ziel → wieder öffnen, Muster `BhkwWirtschaftlichkeitHuelle.TarifOeffnen`): zwei
WebViews übereinander sind Risiko R2 des Wellenplans. Kein Delegat = kein Knopf.

**Tastatur:** Esc schließt überall. **Enter** bestätigt nur in reinen OK-Dialogen; wo ein Knopf
sofort schreibt (Übernahme, Katalog, Verlauf), bleibt Enter unbelegt — ein versehentliches Enter
wäre dort kein Bestätigen, sondern ein Zufall.

Was ein Dialog beim Port an seinem Vorläufer ändert, steht als Abweichungsliste im jeweiligen
Protokoll unter `WindowsFormsApplication1/Allgemein/Reporting/` (`B5b_…`, `iU9_W1_…`,
`iU9_W2_…`).
