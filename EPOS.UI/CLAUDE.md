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

`Dateiwahl` (Pfadfeld + Knopf „Durchsuchen…") **öffnet nichts**: Der Wähler kommt als
`Func<string, Task<string?>>` herein — unter Windows aus `Dienste.Datei.DateiOeffnen`, auf iOS
aus der Dokumentenauswahl. Ohne Delegat bleibt der Knopf weg.

## Dienste (`Dienste/`)

Drei Schnittstellen nach außen — mehr sieht diese Bibliothek von der Umgebung nicht.

| Schnittstelle | Wofür | Windows | iOS | ohne Umgebung |
|---|---|---|---|---|
| `IHilfeDienst` | Hilfetext und Wikiseite zu einem Schlüssel (`InfoKnopf`) | `WindowsHilfeDienst` am `HelpCatalog` | `IosHilfeDienst` (iU10-5) | `KeineHilfe` |
| `IProjektQuelle` | die Daten der **Seiten**: Projektliste, Energieträgerliste, BHKW-Parametersatz, Übernahme des Anlegeergebnisses | — (dort ist die Startmaske der Einstieg) | `IosProjektQuelle` (iU10-7) | `KeineProjekte` |
| `INavigationsZiel` | die **Gegenrichtung** zu `WindowsFormsApplication1.INavigation`: Was eine Oberfläche anbieten muss, damit ein Plattformadapter sie öffnen kann | `WinFormsNavigation` braucht sie nicht | `IosNavigation` reicht dorthin weiter | `Navigationsziel.Aktuell = null` |

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
| `Seitenschluessel` | die drei sprachneutralen ASCII-Schlüssel (`PROJEKTLISTE`, `ENERGIETRAEGER_VARIANTE`, `BHKW_WIRTSCHAFTLICHKEIT`) |

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
