# EPOS.UI — plattformfreie Oberflächenbibliothek

Razor-Klassenbibliothek (`net10.0`) mit den Bausteinen, Standardfeldern, Seiten und Dialogen von
EPOS-Plan — unter Windows in einer `BlazorWebView`, auf iOS/MAUI dieselben Komponenten. Ein
Dialog wird **einmal** geschrieben und von der Plattformhülle angezeigt: Sie reicht Parameter
hinein und nimmt das Ergebnis über einen `EventCallback` entgegen; die Datenseite (DTO aus
Kern-Controllern) liegt in [`EPOS.UI.Daten`](../EPOS.UI.Daten/). Aufbau, Bauen, CI, Git und
Doku-Regeln stehen in [`../CLAUDE.md`](../CLAUDE.md); hier nur Oberflächenspezifisches.

## Hausregeln

- **Kein WinForms, kein `System.Drawing`.** Kein `System.Windows.Forms`, kein `MessageBox` (dafür
  `Rueckfrage`), kein `DialogResult`; `EnableWindowsTargeting=false` bricht jeden Verstoß.
- **Keine Datenbank.** Kein `DataRepository`, kein `RecordSet`, kein `DbParam`, kein SQL: Daten
  kommen als `[Parameter]` herein, geladen und geschrieben wird über einen Kern-Controller in der
  Hülle. Auch die **Fachklassen** des Kerns kennt die Komponente nicht — sie gibt einen
  Ergebnis-Record (`*Ergebnis.cs`) zurück.
- **Texte über Ressourcen** — `@Resource.KAUSW_TITEL` (`@using` in `_Imports.razor`), gepflegt in
  **beiden** Sprachen. Fehlt ein Schlüssel, steht der deutsche Literaltext als Vorgabewert eines
  `[Parameter] string`; jeder Ressourcentext hat einen deutschen **Rückfall**, ein leerer
  Schlüssel bleibt leer.
- **Ab etwa zehn Anzeigetexten ein BÜNDEL statt einzelner Parameter** — eine `*Texte`-Klasse, EIN
  `[Parameter]`, jede Eigenschaft mit ihrem Ressourcenschlüssel im Kommentar. Es trägt
  **Beschriftungen**, keinen Zustand — der steht in `*Gaben`.
- **Zahlen komma- und punkttolerant**, ohne Tausendertrennzeichen, invariant geparst
  (`Standards/Zahlen.cs`); eine Fehleingabe **färbt** das Feld (`epos-fehleingabe`), sie meldet
  nicht. Ein `Zahlenfeld` zeigt ohne Vorgabe höchstens **vier** Nachkommastellen.
- **Energiemengen nur über `Energieeinheit`**, nie mit nacktem Faktor 1 000: Die Zahl kommt in
  ihrer Einheit herein, der Name sagt welche, gerechnet wird im Kern (Wache
  `EinheitenWacheTests`; [`../EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md)).
- **Bezeichner und Kommentare deutsch;** neue `.razor`/`.cs` UTF-8 **mit** BOM, LF.

### Bedienung

- **Berührungsziele mindestens 44 px** (`--epos-touchziel`), Warnfarben mit
  `@media (forced-colors: active)` absichern; Kompaktheit kommt aus der Anordnung, nie aus
  kleineren Bedienelementen. **Aktionsknöpfe einer Tabellenzeile sind IMMER sichtbar**, nie bei
  `:hover` allein.
- **Kein `display: flex` auf `<td>`/`<th>`** (es nimmt der Zelle ihre Rolle): Der Flexkasten
  gehört **in** die Zelle, die Aktionsspalte braucht einen Kopf mit Beschriftung.
- **Ein gesperrtes Bedienelement, das seinen Grund erklären soll, darf nicht `disabled` sein** —
  sein Tooltip erschiene nie; die **weiche Sperre** ist `aria-disabled="true"` plus ein Handler,
  der den Versuch MELDET. Beide Bauarten stehen in EINER Stilregel, denn **zwei Zustände müssen
  SICHTBAR verschieden sein**.
- **Tastatur:** Esc schließt überall, wobei jeder Wirt erst seine Überlagerungsschalter prüft;
  **Enter** bestätigt nur in reinen OK-Dialogen — wo ein Knopf sofort schreibt, bleibt es
  unbelegt. **Kein Delegat, kein Knopf.**

### Stilblatt

- **Jede Änderung am Stilblatt läuft durch `StilblattTests`;** **kein CSS-Nesting im Haus** —
  eine Regel in einer Regel ist eine verlorene Klammer.
- **Eine Farbe steht als TOKEN in `:root`, nicht als Rückfall in der Regel**
  (`var(--epos-marke, #005aa0)` ist danach nirgends mehr änderbar); Ausnahme ist ein Wert, den
  der Aufrufer je Element setzt.
- **Meint ein Vorbild nur EINE Seite, bekommt sie ein eigenes Token**, ein gemeinsames wird nicht
  gekippt: `--epos-start-*` gehört nur `Seiten/Start/*`.
- **Jede neue Schrift-auf-Fläche-Paarung hält 4,5:1** (große fette Schrift 3:1).
- **bunit misst weder Farbe noch Breite noch Höhe:** Eine Stilregel wird als REGEL geprüft oder
  im Browser gemessen.

### Zustand, Meldung, Leerzustand

- **Ein dauerhaftes Banner nur für einen Zustand, den der Anwender beheben MUSS und sonst nicht
  sieht.** Sonst gestaffelt: eine LEISE Zeile, wo er hinsieht; der GRUND am Bedienelement
  (`title` + `aria-disabled`); das Banner erst NACH dem Versuch, mit `Verfaellt`.
- **Eine Meldung, die der Anwender nicht versteht, bekommt eine `Kennung`**
  (`KiMeldungskennung`) — „erklären lassen" führt aufs Aktionswissen.
- **Ein Reiter zeichnet nie ein VORBELEGTES DTO als Ergebnis:** Ein Feld, das es ohne Ergebnis
  nicht gibt, ist **nullbar**, der Stand trägt einen benannten **Zustand** samt Anlass
  (`ErgebnisZustand`) statt eines `bool`, und an seiner Stelle steht eine Karte mit Grund.
- **Jede Seite, die aus einer Kachel oder einem Menüpunkt aufgeht, muss AUCH OHNE GABEN
  zeichnen** — jeder Delegat `null`, jede Liste leer, jeder Text der Rückfall.
- **Ein Parametersatz aus einer Hülle trifft nur `[Parameter]`** — ein unbekannter Schlüssel
  bricht beim ERSTEN Zeichnen im Blazor-Verteiler, ohne Namen.

### Anordnung: ein Dialog baut kein Hausmuster selbst nach, er nimmt den Baustein

- **Eine LISTE steht in einem festen Rahmen mit Rollbalken:** `.epos-raster-huelle` trägt
  `max-height: var(--epos-listenhoehe)`, `overflow: auto` und einen stehenden Spaltenkopf — eine
  Höchsthöhe, Rückweg `Begrenzt="false"`.
- **Projekt ↔ Datenbank immer über `Zweispaltenauswahl`:** Projektliste oben mit Höhengrenze,
  darunter die Übernahmeleiste (je ein Zeichen ▲/▼ als `aria-hidden`-Element, nie im
  Ressourcentext), darunter die Katalogliste über die ganze Breite; Filter darüber, Detailblöcke
  darunter.
- **Ein KATALOGDIALOG nutzt die Höhe:** Wurzel `epos-katalog-dialog`, Baustein `Katalograhmen`
  mit `Liste` und `Eingabe` (`Gestapelt`, wo sie untereinander gehören); nur hier fällt
  `--epos-listenhoehe`, Umbruch bei **900 CSS-Pixeln**.
- **Ein PARAMETERBLOCK steht im `Formularraster`:** Beschriftung neben dem Feld, und ein Feld
  sagt **selbst**, wie lang es ist (`epos-feld--kurz` an Zahlenfeldern, `epos-feld--breit` am
  mehrzeiligen Textfeld; ein `Datumsfeld` nie kurz).
- **Ein Kachelraster gehört in einen Reiter mit DREI Spalten; ein Zweispalten-Reiter bekommt
  einen BEDIENBLOCK:** feste Breite, **ein** Hauptknopf (`epos-knopf--primaer`) mit leiser
  Erklärzeile, darunter Zweitknöpfe und Sperrgründe. Die Kachel fällt nur als BAUFORM; Text und
  Bild bleiben im Kachelregister.
- **Ein Titel, eine Stelle:** Trägt die `Ueberlagerung` einen Titel, zeigt die eingebettete
  Komponente keinen eigenen (`epos-dialog-kopf--ohnetitel`) — über `TitelText=""` oder ein
  eigenes `TitelAnzeigen`.
- **Ein Dialog IN einem Dialog:** Unterdialoge erscheinen als `Ueberlagerung` im selben Fenster,
  nie als zweite `BlazorWebView`; der Wirt splattet ihren Parametersatz aus `Gaben()`.
- **Jedes Renderer-Bild steht im Baustein `Diagramm`**, einziger Weg dorthin ist `ChartBild` —
  sonst fehlt der Zoom; `Rund="true"` reicht `OhneZoom` durch.

### Zeichenläufe, Fokus, JS-Interop

- **Ein Baustein hängt sein Schließen NICHT an `focusout`** (es feuert auch bei Fokuswechseln
  INNERHALB, und Berührung setzt keinen Fokus): Beim Klick daneben schließt eine
  **Schließfläche** (`position: fixed; inset: 0`) mit **drei z-Ebenen**.
- **Jede Ebene einer verschachtelten Aufklapp-Struktur führt ihren EIGENEN Offen-Zustand — am
  besten als PFAD** (`_pfad[0]` erste Ebene, `_pfad[1]` zweite): Das gibt Ausschluss je Ebene und
  das Mitfallen ganzer Untermenüs.
- **In einer Schleife mit veränderlicher Tiefe: `builder.OpenRegion(index)`** — ein Zähler
  verschöbe alle folgenden Folgenummern.
- **Die gewählte `<option>` trägt `selected`, und jede `<option>` ein `@key`** — Blazor setzt
  `element.value` nur beim Erzeugen nach.
- **`@key` nie auf ein Objekt, das je Änderung neu erzeugt wird — Wertidentität nehmen**
  (eine Kennung wie `ErsetztEinheitId ?? Vorlage.Id`, sonst die Zeilennummer innerhalb der
  schon keyed Gruppe; doppelte Schlüssel brechen den Zeichenlauf ab). Ein Objektschlüssel
  vergleicht die Referenz: Schreibt die Seite ihren Stand je Eingabe als Tiefenkopie zurück, ist
  jede Zeile ein neuer Schlüssel, Blazor baut sie samt `<input>` neu, der Fokus geht verloren und
  eine angefangene Dezimalzahl wird gekürzt. Ein Objektschlüssel bleibt nur, wo der Neuaufbau
  gewollt ist (`WaermepumpenDialog`, `_gewaehlt`). Wachen: `OptimierungStationTests` (Zeile und
  Felder bleiben stehen, angefangene Dezimalzahl bleibt), `StromspeicherAuslegungFlotteTests`
  (Lebensdauerkurve); bunit misst dabei die Identität der Komponente, nicht des DOM-Knotens.
- **Wer eine Kopie nur macht, damit eine Referenz sich ändert, nimmt eine Fassungsnummer** (ein
  `int _fassung` auf der Seite, als Parameter `Fassung` an die Blätter, die ihren Stand neu lesen, sobald
  Referenz ODER Fassung wechselt): Die Konfiguration bleibt dieselbe Instanz, statt je Tastendruck als
  JSON-Tiefenkopie neu zu entstehen. Eine Kopie bleibt nur, wo sie fachlich entkoppelt (Rechenlauf,
  Speichern, Übernahme eines Kandidaten); ein meldendes Blatt reicht seinen Stand als eigene Kopie
  heraus, der Wirt kopiert nicht ein zweites Mal. Wachen: `StromspeicherAuslegungFlotteTests` (keine
  neue Flotteninstanz je Eingabe, Netzblock und Betriebseditor schreiben denselben Stand).
- **Eine Prüfung JE TASTENDRUCK fragt keine Datenbank; die teure Stufe läuft entprellt — und
  sofort vor dem Lauf.** Was aus der Konfiguration allein entscheidbar ist (leeres Feld, gültiges
  Raster, Kandidatenzahl gegen die Grenze, die Sperre des Rechenknopfs), steht im selben
  Zeichenlauf. Was Datenbank, Zeitreihen oder einen gerechneten Lauf braucht, läuft erst rund
  400 ms nach dem letzten Zeichen (`CancellationTokenSource` + `Task.Delay` +
  `InvokeAsync(StateHasChanged)`, **kein `Task.Run`**) und außerdem unverzüglich beim Öffnen, beim
  Blattwechsel, nach neuen Vorgaben und vor dem Start — sonst begleitet ein veralteter Befund
  einen Lauf. Die Entprellzeit ist ein `[Parameter]` der Seite (Vorgabe 400 ms, 0 = sofort),
  damit bunit sie abschaltet statt auf eine Wanduhr zu warten; `Dispose` bricht eine offene
  Entprellung ab. **Die Meldungsliste bleibt EINE Liste mit stabiler Reihenfolge:** Die teure
  Stufe ERSETZT sie, sie ergänzt sie nicht — sonst steht ein Hinweis zweimal da. Beide Stufen
  fahren **dieselben Regeln** aus dem Kern; eine zweite Regelsammlung in der Oberfläche gibt es
  nicht. Wache: `VorpruefungEntprelltTests`, dazu der Zähler des Kerns
  (`StromspeicherAuslegungCtrl.Beschaffungen`) statt einer Zeitschranke.
- **Ein Delegat, der Oberfläche der Plattform öffnet, wird `await`et und nie synchron
  ausgewertet** — synchron stürzt die WebView2 ab.
- **Jede Wurzelkomponente steht in der `Fehlerschranke`** (über `Bausteine/Wurzel<T>`; eine
  `ErrorBoundary` fängt nur NACHFAHREN); `async void` und lose `Task` gehen vorbei.
- **Der `SeitenZustand` wird im Blazor-Verteiler gelesen, aus dem Oberflächenfaden geschrieben**
  — die Komponente ruft zuerst `InvokeAsync(StateHasChanged)`.

## Ordner

**`Bausteine/`** — wiederverwendbare Oberflächenteile ohne Fachwissen: kennen weder Datenbank
noch Fachklasse, nehmen ihre Daten als Parameter, melden über `EventCallback`. Wer ein Muster
zweimal sieht, macht daraus einen Baustein. Das **Menü ist Daten** (`Menuetabelle.cs`), kein
Untermenü mit nur einem Punkt.

**`wwwroot/`** — Stilblatt `epos-ui.css` (`.epos-*`, `--epos-*`), Skripte, Bilder. Statische
Web-Assets erreichen den Anwender über `_content/EPOS.UI/…` ohne Zutun einer Hülle; Bildpfade
tragen immer dieses Präfix. **Ein Bild wird nicht umkodiert und nicht zugeschnitten** —
zugeschnitten wird im Stilblatt (`epos-kachel-bild--ausschnitt`); ein Bild ist DEKORATION mit
`alt=""`.

**`Standards/`** — die Eingabefelder des Hauses und `Raster<TZeile>` um `QuickGrid`; kein Dialog
baut ein eigenes `<input>`. `Aktiv` sperrt, ohne auszublenden; `Feldname`/`FehlerZustand` melden,
an welchem Feld eine Prüfung hängt; `Dateiwahl` bekommt die Wähler als Delegaten. **Die Liste am
`Auswahlfeld` gehört dem Wirt** und schuldet je Wert einen Eintrag, stabile Ids (gemeldet wird
die **Id**) und den gespeicherten Wert als Eintrag.

**`Dienste/`** — die einzigen Nähte nach außen: `IHilfeDienst` (Hilfe zu einem Schlüssel),
`IProjektQuelle` (Daten der Seiten) und `INavigationsZiel` (was ein Plattformadapter zum Öffnen
braucht), je mit Windows- und iOS-Adapter und einem Rückfall ohne Umgebung (`KeineHilfe`,
`KeineProjekte`); dazu die Halter `SeitenZustand`, `Navigationsziel`, `KiAssistentWeg`. **Keine
Komponente ruft eine Plattformansicht unmittelbar**, und was eine Plattform nicht kann, wird
benannt abgelehnt statt still übergangen.

**`Seiten/`** — Ansichten für eine Hülle **ohne eigene Fenster**: Ein Dialog löst die Ansicht ab,
statt ein Fenster zu öffnen. `Seiten/AppWurzel.razor` ist die **gemeinsame Wurzel beider
Plattformen** — eine Zustandsmaschine über `Seitenschluessel`, registriert als `INavigationsZiel`;
ihre `Kopfleiste` trägt unter Windows das `Menueband` und ist auf iOS leer, `Startansicht` ist
Einstieg und Ziel des Rückwegs. Eine neue Ansicht braucht einen `Seitenschluessel`, den Fall in
`AppWurzel` und, im Menü, eine Zeile in `Menuetabelle.cs`. **Eine Seite ist kein Dialog:** kein
Ergebnis, keine Schlussleiste — sie lädt über `Laden`, meldet über Rückrufe, frischt sich selbst
auf; als Ansicht der `AppWurzel` bekommt sie ein **parameterloses** `Geschlossen`. Spalten, die
zur **Laufzeit** entstehen, brauchen `<table class="epos-raster">` statt eines `Raster`.

**`Dialoge/`** — je Fachbereich ein Ordner; ein Dialog besteht aus vier Teilen: **`*Daten.cs`**
(DTO und Ergebnis-Records, ohne Fachklasse des Kerns), **`*Texte`** (das Textbündel),
**`<Name>Dialog.razor`** (`EventCallback<T?> Geschlossen`, `null` bei Abbruch) und die **Hülle in
[`EPOS.UI.Daten`](../EPOS.UI.Daten/)**, die den Parametersatz (`Gaben()`) aus Kern-Controllern
baut und das Ergebnis zurückschreibt. Bei mehrfach wiederholten Masken ist die Ausprägung ein
**Aufzählungstyp im Kern** (`BedarfsArt`), keine Zeichenkette. **Ein Katalogdialog holt seinen
Filterstand aus dem Kern** (`Katalogfilterregister.Stand(art)`, je Katalog einer über die
Sitzung), denn ein `static`-Feld wäre Zustand, den keine Probe zurücksetzt; Rückweg ist
`Filterstandvorgabe`.

**Jeder Dialog bietet den Hilfe-Assistenten an — und kein Dialog setzt ihn selbst:** Der KI-Knopf
steckt im `InfoKnopf`, weil der den Hilfeschlüssel trägt, aus dem der Kern den Bereich ableitet;
`<InfoKnopf Schluessel="…" Dialogname="…" />` genügt, `MitAssistent="false"` braucht einen Grund.
**Soll der Dialog seine Feldwerte mitgeben, meldet er sie an** — die Feldliste steht
im Kern (`KiDialoge`), der Dialog meldet in `OnInitialized` nur, wo sie liegen
(`KiMaskenanmeldung.Fuer(name, () => Daten, KiHaken())`): als **Delegat**, nicht als Instanz, und
**der WIRT meldet an, nicht das Blatt darin**. `KiMaskenhaken` lassen den Assistenten MITARBEITEN
— `Pruefen` ist **dieselbe** Prüfung wie am Speicherknopf, ein `Rechenweg` ruft unter dem
AKTIONSNAMEN denselben Weg wie der Knopf.

## Tests (`EPOS.UI.Tests`)

- **Jeder Baustein und jeder Dialog bekommt einen `bunit`-Test.** Mindestens: Feldbestand der
  Maske, Rückweg (Ergebnis **und** `null` bei Abbruch), Zustandsklassen und der Fall **ohne
  Gaben**; bei Familien je Ausprägung, nicht je Komponente.
- **Kulturpinnung ist Pflicht** — der CI-Läufer läuft unter `en-US`, `Resource.*` löst über
  `CultureInfo.CurrentUICulture` auf: Jede Klasse mit deutschem Text-Assert nimmt die
  Hausvorrichtung (`EposBunitContext`); Sammlungen laufen nicht parallel.
- `RenderCount` taugt **nicht** als Zähler für Zeichenläufe.
- **bunits synchrones `Input()`/`Click()` wartet den Zeichenlauf nicht ab.** Läuft in der
  Komponente ein Zeitgeber (Entprellung), belegt dessen Fortsetzung den Verteiler, und ein
  Sofort-Assert liest den Stand vor dem Zeichen: nach jeder Eingabe auf den gezeichneten Zustand
  warten (`WaitForAssertion`/`WaitForState`) und die Entprellung im Prüfstand ausdrücklich setzen.

## Fallstricke der Virtualisierung

`Raster` führt `Virtualisiert` und `Zeilenhoehe`; ein `IQueryable` allein virtualisiert
**nichts** — ohne `Virtualize` zeichnet QuickGrid jede Zeile.

- **GEFILTERT WIRD VOR DEM RASTER, nie im Raster** (QuickGrid filtert nicht): Der Wirt legt den
  Ausdruck in einen `Katalogfilterstand`, lässt `Katalogfilter.Anwenden` im Kern einschränken und
  reicht die Liste als `Zeilen` weiter — **kein `RefreshDataAsync()`**. Der Filterstand lebt im
  Wirt, die **Markierung hängt am Bezeichner**.
- **Eine neue Zeilenmenge bekommt ein neues QuickGrid** — `Raster` hängt dafür selbst ein `@key`
  an; sonst zeigt der flache Zwischenspeicher den Altstand.
- **Kein Zeichenlauf rechnet, was sich nicht geändert hat, und was je ZEILE gefragt wird, wird in
  O(1) beantwortet:** `Katalogliste` entscheidet über einen **Abzug** aus Zeilenliste (Referenz
  **und** Anzahl), Profil und Filterstand; `IstGewaehlt` mit Schleife zahlt n².
- **Das Zeilenmaß wird GESETZT, nicht gerechnet:** Weicht `ItemSize` von der wirklichen
  Zeilenhöhe ab, schieben die Sichtbarkeitsmelder das Fenster endlos gegeneinander — das
  „Blinken". Dieselbe Zahl geht als `ItemSize` und als `--epos-rasterzeile` ins Stilblatt.

Zwei Stilfallen: Die Hausregel `.epos-raster th, td` verliert gegen QuickGrids scoped Blatt
und steht deshalb ein zweites Mal höher gewichtet da (ein umbrechender Spaltenkopf braucht
`th.klasse, td.klasse`); und **eine Klappliste in einer Tabelle wird gedeckelt** (voller Name im
`title`), **elastische Spalten stehen HINTEN**.

**Vor jeder Änderung an `Raster`, `Katalogliste` oder den `.epos-raster*`-Regeln
`Proben/Rasterprobe` ziehen** (Playwright, in keiner CI): bunit hat kein Layout und misst weder
Zeilenhöhe noch die `IntersectionObserver`.

---

Die ausführliche Fassung mit Herleitungen und Bausteininventar:
[`EPOS.UI_CLAUDE_2026-09-12.md`](../Dokumentation/ueberholt/Protokolle/CLAUDE-Historie/EPOS.UI_CLAUDE_2026-09-12.md).
