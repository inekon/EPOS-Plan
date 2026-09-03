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
| `IProjektQuelle` | die Daten der **Seiten**: Projektliste, Energieträgerliste, BHKW-Parametersatz, Übernahme des Anlegeergebnisses | — (dort ist die Startmaske der Einstieg) | `IosProjektQuelle` (iU10-7) | `KeineProjekte` |
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
| `Seitenschluessel` | die drei sprachneutralen ASCII-Schlüssel (`PROJEKTLISTE`, `ENERGIETRAEGER_VARIANTE`, `BHKW_WIRTSCHAFTLICHKEIT`) |

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
