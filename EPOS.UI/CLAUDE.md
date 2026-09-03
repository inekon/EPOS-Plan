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

## Standards (`Standards/`)

`Zahlenfeld`, `Ganzzahlfeld`, `Textfeld`, `Auswahlfeld`, `Datumsfeld`, `Schalter`,
`Raster<TZeile>` (um `QuickGrid`), `ChartBild` (PNG aus dem Kern-Renderer als `data:`-URL).

## Dienste (`Dienste/`)

`IHilfeDienst` — die einzige Schnittstelle nach außen. Die Windows-Fassung hängt am
`HelpCatalog`, für Tests gibt es `KeineHilfe`.

## Dialoge (`Dialoge/`)

Je Fachbereich ein Ordner. `Dialoge/Kosten/EnergietraegerVarianteDialog.razor` ist der erste
migrierte Dialog (Vorbild `Views/Kosten/Form_Kosten_Auswahl`).

| Dialog | Vorbild in WinForms | Datenseite |
|---|---|---|
| `Kosten/EnergietraegerVarianteDialog` | `Form_Kosten_Auswahl` (iU8‑9) | `EnergietraegerVarianteCtrl`, Aufrufer inline |
| `Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog` | `Form_BhkwWirtschaftlichkeit` (B5b) | `BhkwWirtschaftlichkeitHuelle` |
| `Kosten/VorlagenPositionDialog` | `Form_VorlagenPosition` (iU9‑W1.1) | Aufrufer inline |
| `Allgemein/NamensDialog` | `Form_VariantenName` **und** `Form_KostenItemNeu` (iU9‑W1.2) | keine; Windows-Helfer `NamensDialogHuelle.Fragen` |
| `Kosten/CaseEingabeDialog` | `Form_CaseEingabe` (iU9‑W1.3) | Aufrufer inline |
| `Kosten/VorlagenUebernahmeDialog` | `Form_VorlagenUebernahme` (iU9‑W1.4) | `VorlagenUebernahmeHuelle` (3 Delegaten) |
| `Kosten/KostenfaktorKatalogDialog` | `Form_KostenAdmin` (iU9‑W1.5) | `KostenfaktorKatalogHuelle` → `KostenfaktorCtrl` |
| `Wirtschaftlichkeit/KapitalwertVerlaufDialog` | `Form_WirtschaftlichkeitVerlauf` (iU9‑W1.6) | `KapitalwertVerlaufHuelle` (`Task.Run` + `ChartRenderer`) |

**Ein Dialog gibt sein Ergebnis über `EventCallback<T?> Geschlossen` zurück**, `null` bei Abbruch;
geschlossen wird das Fenster von der Hülle. Wo der Aufrufer Werte in ein Fachobjekt zurückschreibt,
liefert der Dialog einen **Ergebnis-Record** (`*Ergebnis.cs`) statt in das übergebene Objekt zu
schreiben — die Komponente kennt die Fachklassen des Kerns nicht.

**Tastatur:** Esc schließt überall. **Enter** bestätigt nur in reinen OK-Dialogen; wo ein Knopf
sofort schreibt (Übernahme, Katalog, Verlauf), bleibt Enter unbelegt — ein versehentliches Enter
wäre dort kein Bestätigen, sondern ein Zufall.

Was ein Dialog beim Port an seinem Vorläufer ändert, steht als Abweichungsliste im jeweiligen
Protokoll unter `WindowsFormsApplication1/Allgemein/Reporting/` (`B5b_…`, `iU9_W1_…`).
