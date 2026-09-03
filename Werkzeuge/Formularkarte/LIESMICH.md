# Formularkarte — der Formular-Generator

Konsolenwerkzeug (`net10.0`, Roslyn) zum Umsetzungskonzept iOS, **Paket iU8‑12**, Grundlage A7 /
iF7. Es liest eine WinForms-Designer-Datei des Bestands und schreibt daraus

1. die **Feldkarte** (`Form_X.karte.md`) — eine Markdown-Tabelle je Abschnitt der Maske, letzte
   Spalte eine Checkbox: die **Abnahmecheckliste** für die Umstellung dieser Maske,
2. ein **Razor-Skelett** (`Form_X.razor`) — den Rohbau des Dialogs für `EPOS.UI/Dialoge/`, gebaut
   aus den Standards und Bausteinen von `EPOS.UI`.

Zweck ist das **Vollständigkeitsnetz für iU9**: 120 Masken werden umgestellt, und kein Feld darf
dabei verlorengehen. Das Werkzeug entscheidet nichts fachlich — es zählt, ordnet zu und macht
sichtbar, was es nicht sicher weiß („prüfen").

Das Werkzeug hat eine **eigene Projektmappe** `Formularkarte.sln` und gehört bewusst **nicht** in
`WP-Plan.sln` (Muster: `Proben/ZugriffsschichtProben/`). Es referenziert nichts aus dem Bestand,
sondern liest dessen Quelltext.

## Aufruf

```bash
# eine Maske: Karte nach stdout
dotnet run --project Werkzeuge/Formularkarte -- WindowsFormsApplication1/Views/Kosten/Form_Kosten_Auswahl.Designer.cs

# eine Maske: Karte und Skelett in Dateien
dotnet run --project Werkzeuge/Formularkarte -- \
    WindowsFormsApplication1/Views/Kosten/Form_Kosten_Auswahl.Designer.cs \
    --karte  Form_Kosten_Auswahl.karte.md \
    --razor  Form_Kosten_Auswahl.razor

# Stapellauf über einen ganzen Baum (Karte + Skelett je Maske, dazu UEBERSICHT.md)
dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1 --ziel dev/karten
```

Der Stapellauf schreibt neben `UEBERSICHT.md` die Befundliste `ERREICHBARKEIT.md` — die
Stilllegungsliste K6 (siehe Abschnitt „Öffner erreichbar").

| Angabe | Bedeutung |
|---|---|
| `<Form_X.Designer.cs>` | die zu lesende Designer-Datei; Groß-/Kleinschreibung der Endung egal |
| `--resx <pfad>` | abweichende neutrale `.resx`; ohne Angabe die Datei gleichen Stamms daneben |
| `--karte <datei>` | Feldkarte als Markdown (ohne `--karte`/`--razor` geht sie nach stdout) |
| `--razor <datei>` | Razor-Skelett |
| `--alle <ordner>` | Stapellauf über alle `*.Designer.cs` unterhalb des Ordners |
| `--ziel <ordner>` | Ausgabeordner des Stapellaufs; ohne ihn wird nur gezählt |
| `--wurzel <ordner>` | Projektordner für die Suche nach `ShowDialog`-Aufrufern (sonst der nächste Ordner mit `.csproj`) |
| `--erreichbarkeit` | die Spalte „Öffner erreichbar" mitrechnen — **Vorgabe**, die Angabe ist nur zur Deutlichkeit da |
| `--ohne-erreichbarkeit` | sie weglassen; spart den Roslyn-Lauf über den ganzen Projektbaum |

Rückgabewert: `0` in Ordnung, `1` Lesefehler, `2` falscher Aufruf.

## Was gelesen wird

**Aus `InitializeComponent` (Roslyn, `CSharpSyntaxTree`)** — beide Schreibweisen des Bestands, die
alte mit `this.` (`Form_KostenfaktorItem`) und die neue ohne (`Form_Kosten_Auswahl`):

* Steuerelemente mit Name und Typ; der Typ stammt aus der Felddeklaration der partiellen Klasse,
  damit auch unqualifiziertes `new Button()` richtig ankommt;
* `Location`, `Size`, `Text`, `TabIndex`, `Enabled`, `Visible`, `ReadOnly`, `Multiline`, `Checked`,
  `MaxLength`, `DropDownStyle`, `Format`, `Anchor`, `Dock`;
* `Minimum`, `Maximum`, `DecimalPlaces`, `Increment` — auch in der Designer-Schreibweise
  `new decimal(new int[] { … })`, die wieder in eine Zahl zurückgerechnet wird;
* `Items.Add` / `Items.AddRange` als Auswahlliste;
* Ereignisse aus `+=`, mit und ohne `new EventHandler(…)`;
* die Elternbeziehung aus `X.Controls.Add(Y)` / `AddRange` — daraus entstehen die **Abschnitte**;
* Eigenschaften des Fensters (`Text`, `ClientSize`, `Font`, `StartPosition`, …) und seine Ereignisse.

**Aus den `.resx` (lokalisierte Masken).** 63 der 120 Masken versorgen ihre Steuerelemente über
`resources.ApplyResources(ctrl, "name")`; Koordinaten, Größen, TabIndex und Texte stehen dann in
`Form_X.resx` als `<data name="ctrl.Location">`. Gelesen werden die neutrale Datei sowie
`Form_X.de-DE.resx` und `Form_X.en-US.resx`; deren Texte füllen die Spalten **Text de** und
**Text en**. Gelesen wird mit einem XML-Leser, nicht mit einem Suchmuster — der Kopf jeder `.resx`
enthält in einem Kommentar Beispielzeilen (`<data name="Name1">`), die sonst als echte Einträge
mitkämen.

**Aus der `Form_X.cs` daneben:**

* `Program.ZahlPruefen` / `ZahlFaerben` / `ZahlParsen` und die Ganzzahlfassungen → welche `TextBox`
  eine Zahl bzw. Ganzzahl führt. Das ist die Vorgabe für `Zahlenfeld` / `Ganzzahlfeld`;
* Anzahl `MessageBox.Show` (jede wird beim Umbau ein `Warnbanner`);
* Zeile und Zeilenumfang jedes Ereignishandlers — sie stehen im Skelett als `TODO`.

**Aus dem Projektbaum:** wer die Maske mit `ShowDialog` öffnet. Gesucht wird im Geltungsbereich der
jeweiligen Erzeugung, nicht über den Variablennamen allein — im Bestand trägt derselbe Name
(`dlg`, `frm`) in einer Datei nacheinander verschiedene Masken.

## Die Zeilenregel für Beschriftungen

Das im Konzept vermutete Raster „Label bei x = 28, Steuerelement bei x = 270" **gibt es im Bestand
nicht**: Über alle Designer liegen die Label-x-Werte bei 12 bis 18, die Feld-x-Werte je Maske ganz
verschieden (114, 159, 250, 278, 350 …). Tragfähig ist stattdessen die Zeilenregel:

1. das nächste `Label` **links in derselben Zeile** — |Δy| ≤ 8 px, kleineres x, **gleicher
   Abschnitt**; bei mehreren gewinnt das nächstliegende;
2. sonst das Label **direkt darüber** — Δy ≤ 24 px, gleiches x ± 8 px;
3. sonst keine Beschriftung (die Karte weist das Feld dann unter „Felder ohne Beschriftung" aus).

Ein Label wird nur **einmal** vergeben. Ein Label, das so zur Beschriftung wird, bekommt keine
eigene Zeile in der Karte — es steht in der Spalte „Label/Text de" seines Feldes. Ein Label ohne
Feld bleibt eine eigene Zeile (Ziel: Text). **Knöpfe** bekommen keine Beschriftung; ihre Aufschrift
steht in `Text`.

## Zielkomponenten

| WinForms-Typ | Feldtyp | Komponente in `EPOS.UI` |
|---|---|---|
| `TextBox` mit `Program.Zahl*` in der Form_X.cs | Zahl | `Zahlenfeld` |
| `TextBox` mit `Program.Ganzzahl*` | Ganzzahl | `Ganzzahlfeld` |
| `TextBox`, `RichTextBox`, `MaskedTextBox` sonst | Text | `Textfeld` |
| `ComboBox`, `ListBox` | Auswahl | `Auswahlfeld` |
| `NumericUpDown`, `DomainUpDown` | Zahl | `Zahlenfeld` (Min/Max/Nachkommastellen aus dem Designer) |
| `CheckBox` | Schalter | `Schalter` |
| `RadioButton` | Auswahl | `Auswahlfeld (Gruppe prüfen)` — die Gruppe wird **ein** Feld |
| `DateTimePicker`, `MonthCalendar` | Datum | `Datumsfeld` |
| `DataGridView`, `ListView` | Raster | `Raster` |
| `Chart` | Diagramm | `ChartBild` |
| `GroupBox`, `TabPage` | Sektion | `Gruppenkopf` |
| `TabControl`, `Panel`, `FlowLayoutPanel`, `TableLayoutPanel`, `SplitContainer` | Sektion | Aufteilung (kein eigener Baustein) |
| `Button` OK / Abbrechen / Speichern / Übernehmen / Schließen | Knopf | `SpeichernLeiste` |
| `Button` `btn_Help` | Hilfe | `InfoKnopf` |
| `Button` sonst | Knopf | eigener Knopf — prüfen |
| `Label` als Beschriftung eines Feldes | — | Spalte „Label/Text de", keine eigene Zeile |
| `Label`, `LinkLabel` ohne Feld | Text | Text |
| `PictureBox`, `ProgressBar`, `TrackBar`, Menü- und Leistenteile, unbekannte Typen | — | **prüfen** |

Ein Knopf gilt als Schließknopf, wenn sein Name nach dem Abstreifen der Vorsilbe (`btn`, `button`,
`cmd`) auf `ok`, `abbrechen`, `cancel`, `speichern`, `save`, `uebernehmen`, `schliessen`, `close`,
`beenden` lautet **oder** seine Aufschrift so heißt.

## Das Razor-Skelett

Vorbild ist der erste fertige Dialog `EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor`:

* `@namespace EPOS.UI.Dialoge.<Fachbereich>` — der Ordnername der Designer-Datei, Umlaute
  umschrieben (`Wärmepumpe` → `Waermepumpe`). Masken außerhalb eines Fachordners (`MDIMainForm`,
  `Form_StromTest`) bekommen `Allgemein`;
* Wurzel-`div` mit `tabindex="-1"`, `@ref` und `@onkeydown` — Enter und Esc beantwortet die
  Komponente selbst, denn eine `BlazorWebView` sieht `AcceptButton` und `CancelButton` nicht;
* Kopfzeile mit Titel und — wenn der Designer einen Hilfeknopf hat — `<InfoKnopf>`;
* je Abschnitt ein `<Gruppenkopf>` mit `<KindInhalt>` (der Baustein nimmt seinen Inhalt als
  **benannten** Parameter, nicht als `ChildContent`);
* die Felder in TabIndex-Reihenfolge als Standardkomponenten, gebunden an ein Werte-Record
  (`Form_XWerte`, eine Eigenschaft je Feld, Typ nach Feldtyp) mit `@bind-Wert` / `@bind-Auswahl`;
* `<Warnbanner>` für die Prüfung, `<SpeichernLeiste>` mit
  `EventCallback<Form_XErgebnis?> Geschlossen`;
* alle Texte als `[Parameter] string` mit dem deutschen Wortlaut als Vorgabe und dem Vermerk
  `// TODO Ressourcenschluessel` — genau wie im Referenzdialog;
* jeder Ereignishandler des Vorbilds als `// TODO: <Handler> aus Form_X.cs:<Zeile> (<n> Zeilen)`.

Der Dateiname bestimmt den Komponentennamen; er bekommt deshalb einen großen Anfangsbuchstaben
(`ucKostenItem` → `UcKostenItem.razor`), sonst weist Razor die Komponente mit **RZ10011** ab.

## Grenzen

* **Kein Fach.** Das Skelett bindet und benennt; Prüfungen, Datenwege und Wirkung der Knöpfe bleiben
  beim Menschen. Es ist ein Rohbau, kein fertiger Dialog.
* **Zahlenfelder erkennt es nur an der Prüfung.** Ein numerisches Feld ohne `Program.Zahl*`
  (z. B. `Form_KostenfaktorItem.textBox_Wert`, das der Aufrufer mit `Convert.ToDouble` liest) landet
  als `Textfeld` in der Karte. Das ist beabsichtigt: Der Leser rät nicht, er zeigt den Bestand.
* **Zur Laufzeit erzeugte Steuerelemente fehlen.** `Form_Kostenprofil` legt 36 Textfelder in der
  `Form_X.cs` an — im Designer stehen sie nicht, also auch nicht in der Karte.
* **Die Abschnitte im Skelett stehen flach nebeneinander**, auch wenn die Maske sie schachtelt; die
  Schachtelung steht in der Karte (Überschriftenebene) und in der Spalte `Elter`.
* **Optionsgruppen** (`RadioButton`) werden feldweise ausgegeben; sie zu **einem** `Auswahlfeld`
  zusammenzufassen ist Handarbeit — das Skelett sagt es als TODO.
* **Menü- und Leistenteile** (`MenuStrip`, `ToolStripMenuItem` …) zählt die Karte, ordnet sie aber
  keiner Komponente zu; die Menüführung ist Sache der Hülle, nicht eines Dialogs.
* **Der Fachbereich kommt aus dem Ordnernamen.** Liegt eine Maske woanders, ist der `@namespace` von
  Hand zu setzen.
* **Wo der Erreichbarkeitsgraph aufhört**, steht im Abschnitt „Öffner erreichbar".

## Öffner erreichbar

Die Karte nennt seit jeher die `ShowDialog`-Aufrufer einer Maske. Sie sagte bis iU8‑12e aber
nicht, ob diese Aufrufer **selbst** noch von einem Menüpunkt, einer Kachel oder einem Reiter aus
zu erreichen sind. Genau daran ist iU8‑9 vorbeigelaufen: `Form_Kosten` hat den ersten
Blazor-Dialog geöffnet, war aber seit KD6a ohne Einstieg (Befund vom 03.09.2026,
Entscheidungsregister § 2.8) — die Umstellung musste mit iU9‑1 an
`Form_Heizkessel`/`Form_BHKWEing` nachgeholt werden.

Seit **iU8‑12f** rechnet das Werkzeug deshalb einen Erreichbarkeitsgraphen über den ganzen
Projektbaum (`Erreichbarkeit.cs`). Er steht als Zeile **„Öffner erreichbar"** im Kopf jeder
Feldkarte, als Spalte in `UEBERSICHT.md` und vollständig in `ERREICHBARKEIT.md` — der
Stilllegungsliste **K6**. Der Befund vom 03.09.2026 liegt als
[`Erreichbarkeit_2026-09-03.md`](Erreichbarkeit_2026-09-03.md) daneben.

### Knoten, Kanten, Wurzeln

**Knoten** sind die Masken: Klassen, die von `Form`, `UserControl` oder `BaseForm` abstammen —
über beliebig viele Stufen. Das sind mehr als die 118 Designer-Masken; die Reiter, Kacheln und
Navigatoren des Hauptfensters (`Views/BerichteKosten/UcBk*`, `Views/Simulation/Navigator*`,
`Views/Hauptformular/*`) sind Zwischenknoten und tragen den Weg mit.

**Kanten** heißen „A öffnet B" und entstehen aus

* `new B(…)` — die Maske wird erzeugt;
* `B.Irgendwas(…)` und `variable.Irgendwas(…)` — angesteuert wird das **Mitglied**, nicht die
  Maske. `Form_ImportKonflikte.Zeigen(…)` und `Form_KiChat.Oeffnen(…)` erzeugen ihre Maske im
  Rumpf, dort greift die erste Regel; `Form_Kosten.LiesAnlagenSummen(…)` liest nur eine Tabelle
  und macht `Form_Kosten` deshalb **nicht** erreichbar;
* `Dienste.Navigation.OeffneMaske(Masken.X)` — der Schlüssel wird über die Sprungtabelle in
  `WindowsFormsApplication1/Dienste/WinFormsNavigation.cs` aufgelöst: je `case Masken.X:` die
  Masken und Methoden, die dieser Zweig anfasst. Die Tabelle selbst ist keine Kante, sonst
  öffnete jeder Aufruf von `OeffneMaske` alle Masken auf einmal.

Wer eine Maske öffnet, ist häufig kein Formular, sondern ein **Vermittler**: `MenueCtrl`, die
`*KontextMenuCtrl`, `AssistentSeiten`. Solche Klassen sind Zwischenknoten — wer einen von ihnen
mit `new` erzeugt, erbt **alle** seine Mitglieder (ein Kontextmenü-Controller meldet seine
Menüpunkte selbst an). Feldinitialisierer laufen mit, sobald irgendein Mitglied der Klasse läuft;
nur so werden die dreizehn Assistentenseiten gefunden, die in `AssistentSeiten` als statisches
Erzeugerfeld stehen.

**Wurzeln** sind `MDIMainForm` und `Form_Start`. Erst wenn von dort nichts mehr zu holen ist,
kommt die Einsprungklasse `Program` dazu — sie zeigt den Erststart-Dialog, bevor es ein Fenster
gibt. Die Reihenfolge ist Absicht: So nennt der Pfad den Weg, den der Anwender geht, und nicht
den Umweg über `Program.Main`.

### Was abgezogen wird

Ein Weg, den es zur Laufzeit nicht mehr gibt, zählt nicht:

* **Handler eines entfernten Steuerelements.** `Form_Start.BaueBerichteKostenSeite` nimmt
  `btn_Kosten` und `btn_Varianten` mit `EntferneAltknopf` aus der Maske. Erkannt wird sowohl
  `X.Controls.Remove(y)` unmittelbar als auch über eine Hilfsmethode, die ihren Parameter
  entfernt.
* **Handler, die nirgends angemeldet sind.** `btn_Kosten_Click` steht noch in `Form_Start.cs`,
  wird aber weder im Designer noch im Quelltext je erwähnt. Nennt der Handlername ein
  entferntes Steuerelement (`<Steuerelement>_<Ereignis>`), sagt der Grund beides.
* **Dauerhaft abgeschaltete Steuerelemente** machen den Weg nicht ungültig, sondern **unklar**:
  `Visible`/`Enabled = false` ohne späteres `= true` irgendwo in derselben Klasse. Im Zweifel
  wird nicht behauptet, die Maske sei erreichbar.
* **Dateien mit `Compile Remove`** in der `.csproj` öffnen gar nichts — sie werden nicht
  übersetzt. Das ist der härteste Befund: `Form_Simulation_Kurz` ist so verwaist.

### Die vier Zustände

| Zustand | Bedeutung | Was damit zu tun ist |
|---|---|---|
| **ja** | Es gibt einen Weg von einer Wurzel; er steht daneben (`Form_Start → btnTraeger → Form_Energietraeger`) | umstellen |
| **nein** | Öffner stehen im Quelltext, sind aber selbst nicht zu erreichen; die Öffner werden mit Grund genannt | stilllegen — **nicht** umstellen |
| **verwaist** | Gar kein Öffner im Quelltext | löschen |
| **unklar** | Nur über einen zweifelhaften Weg (verborgener oder dauerhaft gesperrter Knopf) | vor der Umstellung klären |

Ein langer Pfad wird in der Mitte mit `…` gekürzt; die vollen Öffnerlisten stehen im Programm
(`Maskenknoten.Oeffner`), in der Ausgabe die ersten drei bzw. vier.

### Grenzen

* **Reflexion sieht der Graph nicht.** Im Bestand gibt es sie an dieser Stelle nicht
  (`AssistentSeiten` sagt selbst, warum es `Func<Form>` statt `Activator.CreateInstance` nutzt) —
  käme sie dazu, fiele die Maske fälschlich als „verwaist" auf.
* **Zeichenketten löst er nur bei `Masken.*` auf.** Ein Maskenschlüssel, der über eine Variable
  oder aus der Datenbank käme, bliebe unsichtbar.
* **Er zählt großzügig in Richtung „ja".** Ein `new Vermittler()` schaltet alle Mitglieder des
  Vermittlers frei, ein unbekanntes Mitglied einer bekannten Vermittlerklasse ebenso. Das ist
  Absicht: Eine Maske, die man fälschlich für erreichbar hält, bleibt stehen — eine, die man
  fälschlich für tot hält, wird abgeräumt.
* **Er prüft nicht, ob der Knopf sichtbar ist.** Ob ein Menüpunkt zur Laufzeit freigeschaltet
  wird (Projektkontext, Lizenz), steht nicht im Graphen; „ja" heißt „es gibt einen Weg im
  Quelltext", nicht „der Anwender kommt heute dorthin".
* **Er kennt keine Bedingungen.** Ein `if`, das den Öffner nie durchlässt, sieht er nicht.

## Prüfmuster

Die Tests lesen die **echten** Designer-Dateien des Bestands — das ist der Sinn der Sache und bleibt
so. Nur: Eine Maske, die umgestellt ist, gibt es nicht mehr. Mit **iU8‑9 (Stichtag iZ5)** hat
`Form_Kosten_Auswahl` ihre WinForms-Fassung verloren (Regel M1: keine zweite Fassung derselben
Maske) und läuft seither als `EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor`. Genau
diese Maske war aber die **Handkarte aus dem Umsetzungsplan iU8, Abschnitt D**, an der 19 Tests die
Grundmechanik prüfen: Designer ohne `this.`, Zeilenregel, Zielkomponenten, Kopf der Feldkarte,
Aufbau des Razor-Skeletts.

Deshalb liegt ihr letzter Stand **eingefroren** unter

```
Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/
    Form_Kosten_Auswahl.Designer.cs     Stand 92380ea^, unverändert
    Form_Kosten_Auswahl.cs              dito
    Form_Kosten_Auswahl.resx            dito
    Form_Kosten.Auszug.cs               die eine Methode, die den Dialog modal öffnete
```

Jede Datei nennt im Kopfkommentar Herkunft und Nachfolge. `Form_Kosten.Auszug.cs` ist
`CreateNewEnergyCarrier` aus `Views/Kosten/Form_Kosten.cs` (Zeilen 2089–2196 im Stand `92380ea^`),
Rumpf unverändert, ergänzt nur um Namensraum und Klassenhülle — sonst wäre es kein gültiges C# und
Roslyn fände den Aufrufer nicht. So findet die Aufrufersuche im Prüfmuster **genau einen** Treffer,
wie im Bestand vor dem Stichtag auch.

Drei Regeln halten die Muster von allem anderen fern:

* **Sie werden nie übersetzt.** `Formularkarte.Tests.csproj` nimmt `Pruefmuster/**` aus `Compile`
  und `EmbeddedResource` heraus; sie wandern nur als Inhalt ins Ausgabeverzeichnis.
* **Sie zählen nicht zum Bestand.** `Stapel.Dateien` übergeht jeden Pfad mit einem Ordner
  `Pruefmuster` — genau wie `bin` und `obj`. Sonst meldete das Vollständigkeitsnetz mehr Masken,
  als das Programm hat.
* **Der Ordnername ist der Fachbereich.** Er wird zum `@namespace` des Skeletts, deshalb heißt der
  Ordner `Kosten` und nicht wie die Maske. Das Prüfmuster liegt damit so, wie die Maske im Bestand
  lag.

`PruefmusterTests` hält den Stichtag fest: die Blazor-Nachfolge steht im Repo, die WinForms-Fassung
nicht mehr, das Prüfmuster ist vollständig da und zählt nicht mit.

### Ein weiteres Muster anlegen

Wenn die nächste Maske umgestellt und ihre WinForms-Fassung gelöscht ist:

1. Die drei Dateien aus dem letzten Stand **vor** dem Löschcommit holen (`<sha>^:<pfad>` aus der
   Historie), unverändert bis auf einen Kopfkommentar „Prüfmuster für Formularkarte — Stand vor
   \<Paket\> (\<sha\>^); die Maske wurde durch \<Pfad der Razor-Komponente\> ersetzt". In der
   `.resx` steht er als XML-Kommentar hinter der XML-Deklaration, sonst als `//`-Zeilen ganz oben.
   Ziel ist `Werkzeuge/Formularkarte.Tests/Pruefmuster/<Fach>/`, wobei `<Fach>` der **Fachordner**
   des Bestands ist, nicht der Maskenname.
2. Gab es einen `ShowDialog`-Aufrufer, dessen Methode als `<Aufrufer>.Auszug.cs` danebenlegen, in
   `namespace` und Klasse gehüllt.
3. Die betroffenen Tests auf `Repowurzel.Pruefmuster(...)` umstellen und als **Suchwurzel**
   `Repowurzel.PruefmusterWurzel` mitgeben; Fundstellen prüft
   `Fundstelle.Enthaelt(Repowurzel.PruefmusterBezug, …)`.
4. Tests, die die Maske nur als Beispiel brauchen (Stapellauf über alle Masken), auf eine
   **lebende** Maske umhängen statt auf das Muster. Beim Stichtag iZ5 war das
   `Form_Kosten_VarAuswahl`, die zeichengleiche Schwester; die ist mit **iU9-1** selbst gelöscht,
   seither steht dort `Form_KostenKomponente`. **Nimm dafür eine Maske, deren Öffner erreichbar
   ist** — sonst hängt der Test an der nächsten Löschung wieder; die Spalte „Öffner erreichbar"
   sagt es. `Form_KostenKomponente` erfüllt das (`UcBkKosten.btnVerwaltung_Click`, `MDIMainForm`,
   `KostenKnoepfe`, `Wizard_WPItem`); `Form_KostenfaktorItem` läge im selben Ordner, steht aber
   selbst auf „nein" — es hängt am einstiegslosen `Form_Kosten`.
5. `PruefmusterTests` um die neue Maske ergänzen.

## Nachweis

`dotnet build Werkzeuge/Formularkarte/Formularkarte.sln -c Release` → 0 Fehler, 0 Warnungen.
`dotnet test Werkzeuge/Formularkarte/Formularkarte.sln -c Release` → **117 Tests grün** (101 vor
iU8‑12f, 16 dazu für den Erreichbarkeitsgraphen). Die Tests
laufen gegen die **echten** Designer-Dateien des Repos, nicht gegen Nachbauten — mit der einen
Ausnahme, die der Abschnitt „Prüfmuster" beschreibt.

**Übersetzt das Skelett?** Nachgewiesen am 03.09.2026: eine Kopie von `EPOS.UI/` im Scratchpad, alle
**120** erzeugten Skelette in `Dialoge/` gelegt, `dotnet build -c Release` → **0 Fehler,
0 Warnungen**.

### Stapellauf über das ganze Repo (03.09.2026)

| Kennzahl | Wert |
|---|---|
| Designer-Dateien gefunden | 123 |
| davon Masken (mit `InitializeComponent`) | **120** |
| ohne `InitializeComponent` (Resource/Settings/Resources) | 3 |
| nicht lesbar | **0** |
| lokalisiert (`ApplyResources`) | 63 |
| Kartenzeilen gesamt | 2377 |
| Felder ohne Beschriftung | 178 |

**Nachgemessen nach dem Stichtag iZ5** (Löschung von `Form_Kosten_Auswahl`, iU8‑9): 122
Designer-Dateien, **119** Masken, 3 ohne `InitializeComponent`, 0 nicht lesbar, 63 lokalisiert,
2373 Kartenzeilen, 178 Felder ohne Beschriftung; unter `WindowsFormsApplication1/Views` allein 117
Masken. Die vier Dateien des Prüfmusters sind darin **nicht** enthalten. Jede weitere umgestellte
Maske senkt diese Zahl um eins — das ist der Fortschritt von iU9, nicht ein Loch im Netz.

**Nachgemessen nach iU9‑1** (Löschung von `Form_Kosten_VarAuswahl`): **121** Designer-Dateien,
**118** Masken, 3 ohne `InitializeComponent`, 0 nicht lesbar, 63 lokalisiert, 2369 Kartenzeilen,
178 Felder ohne Beschriftung; unter `WindowsFormsApplication1/Views` allein **116** Masken (62
davon lokalisiert).

**Erreichbarkeit, gemessen am 03.09.2026 (iU8‑12f)** über
`--alle WindowsFormsApplication1` — **118** Masken:

| Öffner erreichbar | Masken |
|---|---|
| ja | 111 |
| nein | 4 (`Form_Kosten`, `Form_KostenfaktorItem`, `ucKostenItem`, `Form_Variantentest`) |
| verwaist | 1 (`Form_Simulation_Kurz` — steht unter `Compile Remove`) |
| unklar | 2 (`Form_GebWohnflaeche`, `Form_PufferSp_Bearbeiten`) |

Der vollständige Befund mit Pfad bzw. Öffner je Maske steht in
[`Erreichbarkeit_2026-09-03.md`](Erreichbarkeit_2026-09-03.md).

Steuerelemente je Typ (Auszug): `Label` 1551, `TextBox` 732, `Button` 504, `ComboBox` 108,
`GroupBox` 83, `TabPage` 74, `Panel` 69, `CheckBox` 59, `NumericUpDown` 57, `ListBox` 50,
`RadioButton` 45, `ToolStripMenuItem` 45, `ListView` 38, `PictureBox` 34, `Chart` 27,
`TabControl` 21, `DataGridView` 16, `DateTimePicker` 4.

Zielkomponenten über alle 2377 Zeilen: Text 671, `Textfeld` 584, eigener Knopf 286,
`SpeichernLeiste` 185, `Zahlenfeld` 169, `Auswahlfeld` 158, prüfen 65, `Schalter` 59, `Raster` 54,
Optionsgruppe 45, `Ganzzahlfeld` 37, `InfoKnopf` 33, `ChartBild` 27, `Datumsfeld` 4.

Unbekannte Typen — und damit die einzigen echten Lücken der Tabelle — sind die vier
selbstgebauten Steuerelemente des Hauses: `AktionsKarte` (2 Masken), `ProjektAuswahl` (2),
`HeaderGradientPanel` (1), `KlimazonenKarte` (1). Sie stehen in der Karte als „sonstig" mit ihrem
Typnamen und als „prüfen".

> **Korrektur zum Umsetzungsplan iU8, Abschnitt D:** Dort stehen „79 Designer-Dateien (74 unter
> `Views/`), 21 davon lokalisiert". Das war eine Messung mit
> Beachtung der Groß-/Kleinschreibung — der Bestand schreibt aber beides,
> `Form_Kosten_Auswahl.Designer.cs` **und** `Form_BHKWEing.designer.cs`. Richtig sind **123**
> Designer-Dateien (118 unter `Views/`), **120** Masken, **63** davon lokalisiert. Die im Konzept
> genannte Zahl 118 war damit näher an der Wahrheit als die Nachmessung.
