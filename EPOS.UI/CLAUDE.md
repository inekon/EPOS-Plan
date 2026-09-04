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
| `Warnbanner` | Hinweis / Warnung / Fehler, `role="alert"`. Seit iU9‑W15b.1 mit **Selbstverfall**: `Verfaellt` (TimeSpan?), `Verfallen` und einer austauschbaren `Uhr` — der Ersatz für `Form_Hinweis`, den Kurzhinweis, der sich nach drei Sekunden selbst schloss. Eine NEUE Meldung setzt den Verfall zurück; eine Frist ≤ 0 heißt „kein Verfall", nicht „sofort weg" | `KartenStil.WARN_*`; Selbstverfall: `Allgemein/Form_Hinweis.cs` |
| `SpeichernLeiste` | OK / Abbrechen und optional „Speichern" ohne Schließen samt Statuszeile | `Allgemein/SpeichernLeiste.cs` |
| `InfoKnopf` | 28×28-Fragezeichen, ruft `IHilfeDienst.Oeffnen` | `Allgemein/Hilfe/InfoKnopf.cs` |
| `Kachel` | Anklickbare Einstiegskarte mit Titel, Beschreibung, Status. Seit iU9‑W16a.2 mit **`Zustand`** (`Kachelstand.Aus`/`An` — grauer oder grüner Statuspunkt, der Punkt in beiden Fällen sichtbar) und **`Aktiv`** (`<button disabled>`, der Ersatz für die vierzehn Zeilen `Cursors.Default` in `AktionsKarte`). **Befund W16a‑B1:** „nur Anzeige" ist KEIN dritter Zustand — Farbe und Anklickbarkeit sind zwei unabhängige Achsen (Brauchwasser ist „nur Anzeige" UND grün oder grau) | `Views/Kosten/EinstiegsKarte.cs`, `Views/GemeinsameBausteine/AktionsKarte.cs` |
| `Assistent` | Der Rahmen eines mehrstufigen Ablaufs (iU9‑W16a.5): linkes Band, Inhaltsfläche, Fußleiste `[Abbrechen] [◀ Zurück] [Weiter ▶ / Speichern]`. `Seiten` ist eine Liste aus `AssistentSchritt` (Titel, Inhalt als `RenderFragment`, `Aktiv`); `NaechsteAktive(richtung)` ersetzt `Next`/`Back`/`GetNextUpIndex`/`GetNextDownIndex`/`lastIndex` (rund 190 Zeilen), und `LoadNewForm` (32 Zeilen gerechnete Fenstergröße) entfällt ersatzlos — CSS | `Views/Wizard/WizardParent.cs` |
| `Herleitungszeile` | Leise Erläuterung, optional mit Formel | Inline-Labels der Kostenmasken |
| `Kohaerenzzeile` | Text mit Zustand „stimmig" / „abweichend" | Inline-Labels der Kostenmasken |
| `Optionsgruppe` | Sich ausschließende Optionen (`fieldset role="radiogroup"`), einzeln sperrbar | die 45 `RadioButton` des Bestands |
| `Zeilenwahl` | Der runde Wahlknopf einer Rasterzeile (`aria-pressed`, 44 px); seit iU9‑W13.0l mit **Mehrfachmodus** — `Mehrfach` macht daraus ein Kontrollkästchen (`role="checkbox"`), und `Tastenwahl` meldet `Strg`/`Umschalt` mit | die Zeilenmarkierung von `ListView`/`DataGridView`, die ein `Raster` nicht kennt |
| `Zeilenmarkierung` | **kein Markup, eine Regel**: die Markierung einer Rasterliste über Anzeigeindizes — Klick wählt eine, `Strg` nimmt dazu oder weg, `Umschalt` wählt den Bereich ab dem Anker; `AufAnzahlBegrenzen` wirft nach einem Filterwechsel hinaus, was hinter der neuen Liste liegt, `QuellIndizes` bildet auf die Importliste ab (Zwilling von `VdiAuswahlFilter.QuellIndizes`) | `ListBox.SelectionMode = MultiExtended` der vier Einlesemasken (iU9‑W13.0l) |
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
| `ProjektListe` | Die EINE Projektliste des Hauses: Suche ueber Name, Kunde **und die unsichtbare Beschreibung**, Sortierung per Spaltenklick mit Gleichstandsaufloesung ueber den Namen, Zaehlzeile als FORMATSTRING (im Vorlaeufer als Steuerelementtext getarnt), Zeilenmarkierung, Doppelklick, zwei Spaltensaetze (`Auswahl` fuer die Projektdialoge, `Einstieg` fuer die iOS-Seite) und `NurName`/`AutoVorauswahl` je Wirt. Eine gewoehnliche Tabelle mit der Hausklasse `epos-raster`, kein QuickGrid — die Spaltenzahl entsteht zur Laufzeit | **vier** Listen des Bestands: `ProjektAuswahl` (uc), `Form_ProjektSpeichernUnter.listView_Projekt`, `Form_ProjektDelete.comboBox_Projekte`, `Form_ProjektExportImport.cbProjekt` (iU9‑W15a.1, Befund W15a‑B52) |
| `Gespraechsverlauf` + `Gespraechszeile`/`Gespraechsrolle` | Die geordnete, rollenbehaftete **Nachrichtenliste** des KI-Assistenten: zehn Rollen (`role="log"`, `aria-live="polite"`, `aria-relevant="additions"`), `@key` je Zeile, `Beschaeftigt`-Zustand, `Fussbereich` als `RenderFragment` (dort steht der Bestätigungsblock — E‑3), Kopieren-Rückruf (den Text liefert die Komponente, die **Hülle** schreibt die Zwischenablage), Autoscroll **nur wenn der Anwender unten steht** (E‑12). **Kein Streaming** (E‑7 — der Bestand streamt nicht), **kein Markdown** (E‑7b — ein Wandler wäre eine neue Abhängigkeit UND eine Angriffsfläche für Modelltext), **keine Link-Erkennung** (nur eine Zeile mit `Adresse` ist ein Verweis — die Komponente rät nie), **nichts in `localStorage`** (der Verlauf ist personenbezogen). Er ist der EINZIGE Ort mit JavaScript in dieser Bibliothek: `wwwroot/epos-verlauf.js`, zwei Funktionen, über `import()` geladen — keine Wirtsseite braucht eine `<script>`-Zeile | `Form_KiChat._verlaufAnzeige` — eine `RichTextBox` mit GENAU EINER Ausgabemethode `SchreibeZeile(text, farbe, fett)`; ihre acht Farben und zwei Schriftschnitte sind die zehn Rollen (iU9‑W15b.6) |
| `KiKnopf` | Der Einstieg in den KI-Assistenten aus einer Maske: `Beschriftung`, `Kurztext`, `Sichtbar` (aus `KiEinwilligung.Abgeschaltet` — der Abschalter blendet ihn AUS, statt ihn zu sperren), `Gewaehlt`. **Links neben dem `InfoKnopf`** (Kollisionsregel `InfoKnopf.cs:99` — in einer Knopfleiste ist das die Reihenfolge, kein gerechneter Pixelabstand). Er ist nicht tabulierbar und zieht den Fokus nicht aus dem bearbeiteten Feld; **er öffnet nichts**, er meldet | `Allgemein/KI/KiAufrufKnopf.cs` (270 Z., mit W14a ohne Aufrufer; iU9‑W15b.5) |
| `Baumansicht` + `Baumknoten` | Ein vierstufiger Baum: `role="tree"/"treeitem"/"group"`, `aria-level`/`setsize`/`posinset`, `aria-expanded` NUR an Knoten mit Kindern, roving `tabindex` über die **abgeflachte Sichtliste** (↓↑ → ← Pos1 Ende Enter/Leertaste, kein Typeahead), Einrückung per CSS, `forced-colors`. Das Dreieck ist ein eigenes 44‑px‑Klickziel und **wählt nicht**; das Kennzeichen (»[Auslieferung]«) steht als eigenes `<span>` neben dem Text, nicht darin. Der Aufklappzustand kommt aus den DATEN (`VonVornOffen`) und überlebt einen Neuaufbau, solange die Schlüssel gleich bleiben. **Kein Kontextmenü, keine Mehrfachauswahl** — die kleinste tragfähige Fassung für den einen Nutzer (R‑W14c‑8) | `Views/Admin/Form_KatalogDubletten._tree` — der **einzige** `TreeView` des Bestands (iU9‑W14c.4); die Daten kommen als `DublettenBaum` aus dem Kern |

## Standards (`Standards/`)

`Zahlenfeld`, `Ganzzahlfeld`, `Textfeld`, `Auswahlfeld`, `Datumsfeld`, `Schalter`,
`Dateiwahl`, `Raster<TZeile>` (um `QuickGrid`), `ChartBild` (PNG aus dem Kern-Renderer als
`data:`-URL).

`Raster` führt seit iU9‑W13.0l `Virtualisiert` und `Zeilenhoehe`: Ein `IQueryable` allein
virtualisiert **nichts** — QuickGrid zeichnet ohne `Virtualize` jede Zeile. Für die 20 746 Zeilen
der CEC-Modulliste setzt der Wirt den Schalter; die Hülle bekommt damit die Klasse
`epos-raster-huelle--hoch` (feste Höhe, stehender Spaltenkopf), ohne die es nichts zu rollen und
also nichts zu virtualisieren gäbe.

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

`Dateiwahl` fuehrt seit iU9‑W15a.5 zusaetzlich `Speichern`/`Namensvorschlag`/`Zielwaehlen`: Der Bestand kannte nur „Datei oeffnen“, der Projektexport braucht „Datei speichern unter“ MIT einem Namensvorschlag (`<Projekt>.wpx`). Der Unterschied ist nicht kosmetisch — ein Speichern-Dialog laesst einen Namen zu, den es noch nicht gibt. Auch hier gilt: kein Delegat, kein Knopf.

## Dienste (`Dienste/`)

Drei Schnittstellen nach außen — mehr sieht diese Bibliothek von der Umgebung nicht.

| Schnittstelle | Wofür | Windows | iOS | ohne Umgebung |
|---|---|---|---|---|
| `IHilfeDienst` | Hilfetext und Wikiseite zu einem Schlüssel (`InfoKnopf`) | `WindowsHilfeDienst` am `HelpCatalog` | `IosHilfeDienst` (iU10-5) | `KeineHilfe` |
| `IProjektQuelle` | die Daten der **Seiten**: Projektliste, Energieträgerliste, BHKW-Parametersatz, Übernahme des Anlegeergebnisses, seit iU9‑W10b der Parametersatz der Simulationskonfiguration und seit iU9‑W16b (K6) `Startkacheln(int)` — die 21 Kacheln der Startseite (**alle mit Standardumsetzung**, damit eine vorhandene Quelle durch die Erweiterung nicht bricht) | die Startseite bekommt ihre Daten von `StartseiteHuelle`, nicht von hier | `IosProjektQuelle` (iU10-7) | `KeineProjekte` |
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
| `Seitenschluessel` | die sprachneutralen ASCII-Schlüssel (`PROJEKTLISTE`, `ENERGIETRAEGER_VARIANTE`, `BHKW_WIRTSCHAFTLICHKEIT`, `SIMULATION_KONFIGURATION`, `SIMULATION_ERGEBNIS`, `KI_ASSISTENT`, seit iU9‑W16a.5 `ASSISTENT`). Die drei übrigen der Zusammenlegung (`STARTSEITE`, `BERICHTE_KOSTEN` und die verbleibenden `Masken`-Werte, K7) kommen mit W16c |
| `Simulation/SimulationKonfigSeite` | die **Simulationskonfiguration** (iU9‑W10b.1) — die erste FACHSEITE, die iOS über `AppWurzel` erreicht. Unter Windows steht dieselbe Komponente bis W16 in einer modalen Dialoghülle (Entscheid R‑W10b‑1), weil ihre beiden Aufrufer die modale Rückkehr brauchen. Datenseite: `Views/Simulation/SimulationKonfigHuelle.cs` |
| `Simulation/SimulationErgebnisSeite` | das **Simulationsergebnis** (iU9‑W11b.13) — die zweite Fachseite für `AppWurzel`, unter Windows ebenfalls bis W16 modal (Entscheid R‑W11‑1, 1 474 × 821). **Ein `Reiter` für zehn Blätter**: Parameter (mit fünf Unterblättern), Übersicht, Bedarf, Wärmepumpe, Heizkessel, Solarthermie, BHKW, Photovoltaik, Stromspeicher, Ergebnis — dazu vier Überlagerungen. Datenseite: `Views/Simulation/SimulationErgebnisHuelle.cs` in vier Teildateien |
| `Assistent/AssistentSeite` | der **PROJEKTASSISTENT** (iU9‑W16a.5, S3) — dreizehn Schritte in EINER Komponente, in der bitgleichen Reihenfolge des Bestands (`AssistentSeiten.ERZEUGER` = Nummernkatalog `WizardItemClass`). Je Seite kommt ein fertiger Parametersatz herein, bei JEDEM Betreten neu erfragt (das tat `BlazorAssistentSeite.Bestuecken` ebenso). Das linke Band steht nur in Betriebsart BEARBEITEN auf Schritt 0 — `InfoKnopf`, `ProjektListe`, „Projekt öffnen". Die dritte Fachseite, die iOS über `AppWurzel` erreicht; unter Windows steht sie in einer MODALEN Hülle, weil beide Aufrufer auswerten, ob gespeichert wurde. Datenseite: `Views/Wizard/AssistentHuelle.cs` → `EPOS.Kern/Controller/AssistentCtrl` |
| `Start/Startseite` | **DIE STARTSEITE** (iU9‑W16b.2, S1) — die Wurzel der Anwendung aus Anwendersicht, Nachfolge von `Views/Hauptformular/Form_Start` (2 300 Z. + 1 381 Designer, 108 Kartenzeilen). Kopfband (Produktgattung, Projekt/Varianten, Statuszeichen, Klimaregion mit Speicherknopf), **sechs Reiter mit 21 Kacheln**, Fußleiste ◀/▶. Die Reiter 2 bis 6 sind gesperrt, solange kein Projekt offen ist (`Reiterblatt Bedienbar`), und ein dauerhaftes `Warnbanner` sagt warum — der Ersatz für den `Form_Hinweis`, den der Vorläufer erst NACH dem Klickversuch zeigte. Der Statuspunkt je Kachel kommt aus der EINEN Bitmaske des Kerns (`KomponentenBestandCtrl`, E‑3/N6); die 13 `Paint`-Handler des Vorläufers sind eine CSS-Klasse geworden, die drei Bindemuster für den Kachelklick (Wörterbuch mit 24 Einträgen, 14 Weiterleitungshandler, sechs `Geklickt`) EIN `@onclick` mit einem sprachneutralen Schlüssel. Seit W16b.4 trägt sie zwei weitere Ansichten: die **Simulationskonfiguration als freie Ansicht** (sie löst die Startseite ab) und das **Simulationsergebnis als `Ueberlagerung`** — beide ohne zweites Fenster (E‑5; die Entscheide R‑W10b‑1 und R‑W11‑1 sind damit geschlossen). Datenseite: `Views/Hauptformular/StartseiteHuelle.cs` → `ProjektKontextCtrl`, `StartseiteCtrl`, `KomponentenBestandCtrl`, `BedarfsZustand` |
| `Start/ProjektReiter` … `SimulationReiter` | die **fünf Reiterkomponenten** der Startseite (5 / 4 / 3 / 7 / 2 Kacheln); der sechste Reiter ist die `BerichteKostenSeite` aus W5. `ErzeugerReiter` trägt zusätzlich die **Weiche** der Solarthermiekachel (Profil / Ganglinie) — sie entscheidet, welcher der beiden Dialoge aufgeht, und stand im Bestand als zwei `RadioButton`, die ZUGLEICH den Status trugen; die Farbe sagt jetzt allein der Statuspunkt. `SimulationReiter` trägt die Projektzusammenfassung und den Knopf „Simulation Konfiguration…", der als 21. Kachel zählt |
| `Start/Kachelschluessel` + `Reiterschluessel` | die sprachneutralen ASCII-Schlüssel der 21 Kacheln und der sechs Reiter |
| `Assistent/ProjektKopfSeite` | die **erste Assistentenseite** (iU9‑W15a.6) — neun Verwaltungsfelder des Projekts. Sie ist die einzige Seite mit einem ERGEBNIS: eine EINELEMENTIGE geteilte Liste `ProjektKopfDaten`, die die Seite an Ort und Stelle beschreibt (Weg (a), Befund W15a‑B42). Bis iU9‑W16a.5 trug sie eine `BlazorAssistentSeite<…>`; seither hält der Assistent selbst die Liste. Datenseite: `Views/Wizard/ProjektKopfHuelle.cs` |
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
| `Bedarf/KomponentenauswahlDialog` | `Wizard_Komponenten` (iU9‑W16a.3) — Schritt 0 des Assistenten: dreizehn `Kachel` über `Kachelraster`, gespeist aus `KomponentenBestandCtrl`. Die Rückfrage beim Abwählen einer belegten Komponente ist **wörtlich** übernommen, Vorbelegung „Nein" | `Views/Wizard/KomponentenauswahlHuelle.cs` → `KomponentenBestandCtrl` |
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
| `Import/KatalogImportDialog` | **vier** Masken: `Form_Heizkessel_einlesen`, `Form_PufferSp_einlesen`, `Form_SolarKollektoren_einlesen`, `Form_WP_einlesen` (iU9‑W13.1) | `Views/Import/KatalogImportHuelle.cs` → `KatalogImportProfil`, `KatalogImportAblauf`; EINE Hülle für alle vier Maskenschlüssel, Lesen und Schreiben in `Task.Run` |
| `Bedarf/WaermebedarfAdminDialog` | `Form_AdminWaermeeinlesen` (iU9‑W13.2) | `Views/Wärmebedarf/WaermebedarfAdminHuelle.cs` → `WaermebedarfStammCtrl`, `GanglinienTextDatei`, `DublettenPruefung`; erscheint auch als Überlagerung in `WaermebedarfExternDialog` |
| `Photovoltaik/PvModulImportDialog` | `Form_CECImport` / Klasse `Main_PV_Test` (iU9‑W13.3) | `Views/Photovoltaik/PvModulImportHuelle.cs` → `CECDataService`, `PanDataService`, `PhotovoltaikStammCtrl`; Netzabruf mit `Fortschritt` und Abbrechen, 20 746 Zeilen im virtualisierten `Raster` |
| `Bedarf/BedarfAdminDialog` | **drei** Masken: `Form_Stromverbraucher_Admin`, `Form_Prozesswaerme_Admin`, `Form_Brauchwasser_Admin` (iU9‑W14b.1) | `Views/Bedarf/BedarfAdminHuelle.cs` → `BedarfStammCtrl`, `BedarfsVorschauCtrl`; EINE Hülle für drei Maskenschlüssel, **vier** Überlagerungen aus Welle 8 |
| `Solarthermie/SolarganglinieAdminDialog` | `Form_Solarganglinie_Admin` (iU9‑W14b.2) | `Views/Solarthermie/SolarganglinieAdminHuelle.cs` → `SolarganglinieStammCtrl`, `GanglinienTextDatei` (mit Kopfzeile); erscheint auch als Überlagerung in `SolarganglinieDialog` |
| `Erzeuger/KatalogBrowserDialog` | **vier** Masken: `Form_Heizkessel_Admin`, `Form_BHKWAdmin`, `Form_SolarKollektorenAdmin`, `Form_PufferSp_Admin` (iU9‑W14a.1) | vier Hüllen mit gemeinsamem Kern (`Views/Erzeuger/KatalogBrowserHuelle.cs`) → `KatalogBrowserProfil`; der Katalogeditor und die Namensabfrage sind Überlagerungen, `NurLesen` ist der Lesemodus des Pufferspeichers |
| `Erzeuger/PufferSpKatalogDialog` | `Form_PufferSp_Bearbeiten` (iU9‑W14a.2) | `Views/Pufferspeicher/PufferSpAdminHuelle.cs` → `PufferSpStammCtrl.Anlegen`/`Ueberschreiben`, `SpeichertypAbbildung`; erscheint als Überlagerung im Browser |
| `Erzeuger/ModulKatalogDialog` | **zwei** Masken: `Form_AdminStromspeicher`, `Form_AdminPV` (iU9‑W14a.3) | `Views/Stromspeicher/StromspeicherAdminHuelle.cs`, `Views/Photovoltaik/PvAdminHuelle.cs` → `ModulKatalogProfil`; Browser und Editor in EINER Komponente |
| `Wirtschaftlichkeit/GesetzeskatalogDialog` | `Form_Gesetzesparameter` (iU9‑W14c.2) | `Views/Admin/GesetzeskatalogHuelle.cs` → `GesetzKatalog`; er erscheint als eigenes Fenster (Menü) UND als Überlagerung in `KostenKomponenteDialog` und `WirtschaftlichkeitParameterDialog` — die letzten zwei `Sprungziel`e fallen damit |
| `Wirtschaftlichkeit/GesetzeskatalogZeileDialog` | `Form_GesetzparameterZeile` (iU9‑W14c.1) | keine Hülle — eine Überlagerung im Katalog; Schlüssel und Klasse sind beim Ändern gesperrt, ein leeres Wertfeld ist NULL und nicht 0 |
| `Admin/KatalogDublettenDialog` | `Form_KatalogDubletten` (iU9‑W14c.5, ohne Designer) | `Views/Admin/KatalogDublettenHuelle.cs` → `DublettenPruefung`, `DublettenBaum`, `DublettenBefundText`, `KatalogBereinigung`; der Scan läuft in `Task.Run` mit `Fortschritt`, das Umbenennen ist der `NamensDialog` **mit Prüfung** |
| `Admin/EinstellungenDialog` | `Form_AdminSettings` (iU9‑W14c.6) | `Views/Admin/EinstellungenHuelle.cs` → `EinstellungenCtrl`; die Rubrikenliste ist ein SENKRECHTER `Reiter` mit vier Blättern, der KI-Abschalter läuft über `KiEinwilligung` |
| `Projekt/ProjektWahlDialog` | **zwei** Masken: `Form_ProjektAuswahl` und `Form_ProjektDelete` (iU9‑W15a.2) | `Views/Projekt/ProjektWahlHuelle.cs` → `ProjektCtrl.NamenListe`; der Zweck (Öffnen/Löschen) entscheidet über Titel, Knopftext und die Sicherheitsabfrage |
| `Projekt/ProjektKopieDialog` | `Form_ProjektSpeichernUnter` (iU9‑W15a.4) | `Views/Projekt/ProjektKopieHuelle.cs` → `ProjektDuplizierenCtrl.PruefeNamen`/`Duplizieren`/`VerwaltungsfelderSetzen`; der Kopierlauf läuft in `Task.Run` mit `Fortschritt` und Abbrechen |
| `Projekt/ProjektTransferDialog` | `Form_ProjektExportImport` (iU9‑W15a.5, ohne Designer) | `Views/Projekt/ProjektTransferHuelle.cs` → `ProjektExportImportCtrl` (seit W15a.0e im Kern); vier Pfaddelegaten — Dateiwahl lesend und schreibend, Sicherungskopie, Importbericht |
| `Hilfe/TextAnzeige` | `Form_TextAnzeige` (iU9‑W15b.2) | keine Datenseite; sie erscheint als Überlagerung im Chat (Aktionsprotokoll und Sendevorschau) |
| `Hilfe/KiHinweisDialog` | `Form_KiHinweis` (iU9‑W15b.3) | `Views/Help/KiHinweisHuelle.cs`; sie hängt `KiEinwilligung.Nachfragen` in `Program.Main` ein — ohne diesen Aufruf gibt es keinen Weg zu einer Einwilligung und damit keine Übertragung |
| `Hilfe/KiEinstellungenDialog` | `Form_KiEinstellungen` (iU9‑W15b.4) | `Views/Help/KiEinstellungenHuelle.cs`; der Schlüssel geht als Vorbelegung hinein und über `KiEinstellungenErgebnis` heraus (Regel S‑1), das Feld ist `type="password"` (S‑2), „Modell neu erkennen" trägt den Seiteneffekt der Vorlage (E‑5) |
| `Hilfe/KiChatDialog` (+ `KiBestaetigungBlock`, `KiWerkzeugliste`, `KiEingabezeile`) | `Form_KiChat` (1 704 Z., iU9‑W15b.7) | `Views/Help/KiChatHuelle.{cs,Gaben.cs}` — **nicht-modal mit Besitzer** (E‑6); die Komponente kennt weder `KiChatService` noch `KiAusfuehrer` noch das Netz, sie bekommt Delegaten. Zwei getrennte Listen: die Anzeige (Klarnamen aufgelöst) und der Prompt-Verlauf (platzgehalten, H8) |
| `Lizenz/LizenzVerwaltungDialog` | `Form_LizenzVerwaltung` (iU9‑W15c.5) | `Views/Admin/LizenzVerwaltungHuelle.cs` → `LizenzCtrl`; die einzige Maske des Bestands, die den Lizenzserver anspricht — die Komponente kennt ihn NICHT, sie bekommt fünf Anzeigewerte (`LizenzGaben`) und sechs Delegaten (Regel S‑2). Sie erscheint als eigenes Fenster UND als Überlagerung im Lizenzdialog |
| `Lizenz/ErststartDialog` | `Form_Erststart` (iU9‑W15c.7) | `Views/Admin/ErststartHuelle.cs` → `ErststartCtrl` (der bleibt in der Windows-App: `ErststartMigration` bringt OleDb mit). **Die erste besitzerlose Hülle des Bestands** — sie läuft in `Program.Main`, vor jedem anderen Fenster |
| `Lizenz/LizenzDialog` | `Form_Lizenz` (iU9‑W15c.11) | `Views/Help/LizenzHuelle.cs` → `LizenzTextCtrl`, `ZustimmungCtrl`; **zwei Gesichter, eine Komponente**: Menü „Hilfe → Lizenz" und die EULA-Abfrage beim ersten Start (besitzerlos). Die Verwaltung erscheint darin als Überlagerung (E‑11) |
| `Klimadaten/KlimadatenDialog` | `Form_Klimadaten` (iU9‑W14c.7) | `Views/Admin/KlimadatenHuelle.cs` → `KlimaregionStammCtrl`, `SolardatenCtrl`, `KlimaImportAblauf`, `ChartRenderer.Jahresgang`; der Import läuft in `Task.Run` mit Fortschritt und Abbrechen |

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

**Sechs Masken, drei Komponenten** (iU9‑W13): Die vier VDI-3805-Katalogimporte
— Heizkessel (Blatt 3), Pufferspeicher (20), Solarkollektoren (19) und
Wärmepumpen (22) — sind VIERLINGE: Dreizehn Bausteine standen viermal
WORTGLEICH im Bestand, bis hin zum falschen Handlernamen
`Liste_WP_SelectedIndexChanged` in drei von vier. Was sie trennt, sind sieben
WERTE — Katalogschlüssel, Unterordner, Dateifilter, Filtergröße samt
Vorbelegung, Detailfeldliste, Vergleichswerte, Schreibweg —, und die stehen als
`KatalogImportProfil` im Kern, mit `KatalogImportArt` als Aufzählungstyp
(Muster `BedarfsArt` aus W8). Der Feldkartenabgleich läuft deshalb je
AUSPRÄGUNG, nicht je Komponente. Der Ablauf selbst ist `KatalogImportAblauf`:
Lesen, Filtern, Vorprüfen, Ausführen — der Konfliktdialog ist dort **kein
Rückruf**, sondern eine Zäsur zwischen zwei Aufrufen, damit der Fadenwechsel
auf zwei klare Stellen beschränkt bleibt. Der Nachweis der Welle sind zwanzig
IMPORT-PROBEN mit eingefrorenen Erwartungswerten, angelegt VOR jeder portierten
Zeile. Die Wärmebedarfsverwaltung folgt dem W12-Zwilling
`StromganglinieAdminDialog`; ihr Sprung über die `Sprungbruecke` ENTFÄLLT, weil
das Ziel selbst Blazor wird — aus dem zweiten Fenster wird eine Überlagerung.

**Vier Masken, zwei Komponenten** (iU9‑W14b): Die drei Bedarfs-KATALOGVERWALTUNGEN
— Stromverbraucher, Prozesswärme, Brauchwasser — sind DRILLINGE wie ihre
Projektblätter aus W8 und W9: Designer zeichengleich bis auf die Bezeichner,
`SetControls`, `SetProzessInfo` und `Prozesssumme` dreimal WORTGLEICH. Von
dreizehn Unterschieden sind **vier echte Ausprägung** — `BedarfsArt`,
Simulationsklasse, Engine-Methode, Teiler —, und die drei letzten liegen ohnehin
hinter `BedarfsArt`; alles andere ist Nachzug oder Zufall. Der
Feldkartenabgleich läuft deshalb je AUSPRÄGUNG. **Fünf von sieben Knöpfen führten
schon vorher in Blazor** (`TypStammDialog`, `TypProfilDialog`,
`BedarfErgebnisDialog` — bis dahin als zweites modales Fenster ÜBER der
WinForms-Maske); sie werden Überlagerungen derselben Komponente, dazu die
Namensabfrage. Der SECHSTE Knopf war eine Täuschung: `btn_ErgebnisseVerbrauch_Click`
stand in allen drei Masken, ein Knopf dazu in KEINEM Designer (Befund W14‑B78) —
damit sind zwei seit Welle 8 offene Entscheide gegenstandslos. Die
Solarganglinien-Verwaltung folgt dem W13-Zwilling `WaermebedarfAdminDialog`, nur
liest sie MIT Kopfzeile (`GanglinienTextDatei.Lies(…, mitKopfzeile: true)` — die
Klasse ist mit W13.0h für genau diesen zweiten Aufrufer so gebaut worden); ihr
Sprung entfällt aus demselben Grund wie beim Wärmebedarf, **`Sprungziel` führt
danach acht Konstanten** (nach W14a noch drei). Der Nachweis der Welle sind 37 EINGEFRORENE Fälle,
angelegt VOR der ersten portierten Zeile: Für diese vier Masken gab es weder
Referenzlauf noch ChartProbe noch Kern-Test.
**Sieben Masken, zwei Komponenten** (iU9‑W14a): Die Erzeuger-Katalogverwaltung.
Die vier Admin-Masken Heizkessel, BHKW, Solarkollektoren und Pufferspeicher sind
BEHÄLTER um Editoren, die seit W6/W7 schon Razor sind — sie tun nichts, was
`HeizkesselKatalogDialog`, `BhkwKatalogDialog` und `SolarkollektorKatalogDialog`
nicht könnten, außer Liste, Filter und Löschen. Was sie trennt, sind acht Werte;
die stehen als `KatalogBrowserProfil` im Kern, der Feldkartenabgleich läuft je
AUSPRÄGUNG. Der fehlende VIERTE Katalogeditor entsteht dabei
(`PufferSpKatalogDialog`) und erscheint als Überlagerung im Browser, nicht als
zweites Fenster. Die zwei Modulkataloge (Stromspeicher, Photovoltaik) sind
Browser UND Editor in einem und werden eine zweite Komponente mit zwei
Ausprägungen — die gepflegte Fassung zieht die liegengebliebene mit. **Mit dieser
Welle fällt der LETZTE „unklar"-Zustand des Bestands** (`Form_PufferSp_Bearbeiten`
hinter zwei dauerhaft gesperrten Knöpfen): Der Erreichbarkeitsbefund zählt
seither 0 nein / 0 verwaist / 0 unklar. Die fünf verbliebenen
Erzeuger-`Sprungziel`e fallen mit ihr — ihre Ziele sind selbst Blazor, und aus
jedem Sprung wird eine Überlagerung (Risiko R2).

**Fünf Masken, fünf Komponenten, vier Fenster** (iU9‑W14c): Gesetzeskatalog,
Klimaregionen, Einstellungen und Dublettensuche. Hier wiederholt sich **kein
Muster** — jede Maske ist ein eigener Gegenstand; was zweimal vorkam, war der
Anzeigeträger `KlasseItem`/`KatalogItem`, und der ist in beiden Fällen eine
schlichte `(Wert, Anzeige)`-Liste am `Auswahlfeld`. Der Befund der Welle:
**vier der fünf Fachteile lagen schon im Kern**, die Vorarbeit war Zuschnitt und
kein neuer Rechenweg. Drei Dinge nimmt die Welle trotzdem mit: die **letzten zwei
ablösbaren `Sprungziel`e** (beide Aufrufer waren schon Razor — aus jedem Sprung
wird eine Überlagerung, im Kostendialog die SECHSTE), **alle sechs WFO1000** der
Mappe und den **letzten MS-Chart-Nutzer** (`ChartManager`, 560 Z.). Neu ist der
Baustein `Baumansicht` für den einzigen `TreeView` des Bestands. Was bleibt, ist
ein Entscheid: `Sprungziel` führt danach EINE Konstante
(`SpeicherOptimierung`) — sie steht bis Welle 16, und wer sie aufräumt, bricht
`Form_SpeicherOptimierung` (iF22).

**Sechs Bauteile, fünf Komponenten, vier Fenster** (iU9‑W15a): Die Projektdialoge, der
Transfer und der Assistentenkopf. Der Befund der Welle ist eine Zahl: **der Bestand führte
VIER Projektlisten nebeneinander** (ListView mit drei Spalten und Suche, ListView mit zwei
Spalten, ComboBox über eine Erweiterungsmethode, ComboBox mit eigener Schleife) — dazu als
fünfte die fertige Razor-Seite `Seiten/Projektliste`. Sie werden EIN Baustein, und das
Konzept „Eine Projektauswahl für alle" ist damit eingelöst. Zwei Masken werden dabei EINE
Komponente: „Projekt öffnen" und „Projekt löschen" taten dasselbe — ein Projekt auswählen —,
sie unterscheiden sich in Titel, Knopftext und der Sicherheitsabfrage. **Diese Masken sind
als einzige der ganzen Reihe LOKALISIERT** (461 `.resx`-Einträge, aber nur sechs
`MyResource`-Zugriffe); der Port hebt 83 Texte in den Katalog, davon 27 für eine Maske, die
**gar nicht übersetzt war**. Der Nachweis der Welle entsteht ZUERST und findet dabei den
Befund W15a‑B55: **der Projektimport war seit der SQLite-Umstellung kaputt** — zwei Stellen
trugen benannte Platzhalter im SQL-Text, und die Zugriffsschicht bindet nach Position.
`ProjektAuswahl` (das UserControl) BLEIBT bis Welle 16 im Bestand: Es lebt in zwei Wirten,
und der zweite ist der Assistentenrahmen — für genau eine Welle gibt es zwei Fassungen
derselben Liste (ausdrückliche Ausnahme von iZ5, Muster W4‑O1).

**Drei Masken, drei Komponenten** (iU9‑W15c): Lizenz und Erststart. Der Befund der
Welle ist eine Null: **es gab bis dahin keinen einzigen Lizenztest** — der Lizenzkern
liegt seit iU5‑U1 plattformfrei im Kern (sechs Zustände, zwei Fristen, Kulanz, Karenz,
Uhrschutz), geprüft hatte davon nichts. Der Wellennachweis ist deshalb eine
**Erstanlage**: 14 Zustands- und 4 Tokenfälle in `EPOS.Kern.Tests`, angelegt VOR der
ersten Maske. Die drei Komponenten kennen den Lizenzkern nicht (Regel S‑2): Auf iOS
liest `LizenzManager.Pruefe()` den Schlüsselbund SYNCHRON, und eine Komponente ruft
immer vom Zeichenfaden. **Kein Token, kein Zeitanker, kein Schlüssel als `[Parameter]`**
(S‑3), und der eingetippte Lizenzschlüssel verlässt die Komponente nur Richtung
„Aktivieren" (S‑4, Feld leer nach Erfolg). Zwei der drei Masken laufen **besitzerlos**
in `Program.Main`, vor jedem anderen Fenster — dafür hat `BlazorDialogForm<T>` vier
Zusätze bekommen (`ImTaskbar`, `AufBildschirmMittig`, `SchliessenGesperrt`,
`Mindestmass`), alle mit dem heutigen Vorgabewert. **Damit hängt der Start an der
WebView2-Laufzeit**: `Program.Main` prüft sie seit W15c.6a selbst und meldet ihr Fehlen
mit der Bezugsquelle, statt den Anwender vor einem leeren Fenster stehen zu lassen.
Die 27 Rechtstexte sind **maschinell** umgezogen und Zeichen für Zeichen
zurückverglichen (26 zeichengleich, 1 sachlich berichtigt: .NET 10, SQLite, WebView2).

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
