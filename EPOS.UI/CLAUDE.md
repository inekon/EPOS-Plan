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
  **Und die Hülle hat seit Auftrag #208 ein eigenes Projekt: [`EPOS.UI.Daten`](../EPOS.UI.Daten/).**
  Es referenziert diese Bibliothek (und damit den Kern), ist ebenso plattformfrei
  (`EnableWindowsTargeting=false`) und trägt den Programmtext, der aus Kern-Controllern die DTO
  dieser Komponenten baut — bis dahin lag der samt Datenbankzugriff in
  `WindowsFormsApplication1/Views/`, und genau deshalb war keine Fachseite auf iOS erreichbar.
  **An dieser Regel ändert das nichts:** Hier kommen die Daten weiterhin fertig herein.
- **Texte über Ressourcen.** `@using WindowsFormsApplication1.MyResource` steht in `_Imports.razor`,
  also `@Resource.KAUSW_TITEL`. Solange ein Schlüssel fehlt, steht der deutsche Literaltext als
  Standardwert eines `[Parameter] string`-Textes in der Komponente — die Hülle kann ihn dann ohne
  Änderung der Komponente auf den Ressourcenschlüssel umstellen.
- **Ab etwa zehn Anzeigetexten ein BÜNDEL statt einzelner Parameter.** Ein Satz Beschriftungen
  als `*Texte`-Klasse, EIN `[Parameter]`; jede Eigenschaft nennt ihren Ressourcenschlüssel im
  Kommentar. Sonst verstecken dreißig `[Parameter] string` die Fachparameter zwischen
  Knopfbeschriftungen und treiben die Komponente über die Hausgrenze von 400 Zeilen. Zwei
  Bauarten: **`KiChatTexte`** (W15b) lässt die Hülle füllen — dort hängen Werte an
  Fallunterscheidungen (Hilfe-Betrieb, Einwilligung, Modellname); **`LizenzTexte`** (W15c‑O‑2,
  ein Bündel für `LizenzDialog` **und**, unter `.Verwaltung`, für `LizenzVerwaltungDialog`) füllt
  sich SELBST aus `MyResource` in der Oberflächensprache, weil es reine Katalogeinträge sind —
  die Hülle überschreibt nur, was sie zusammensetzt. Ein fehlender Schlüssel fällt auf den
  deutschen Wortlaut zurück, ein vorhandener **leerer** bleibt leer (`LIZR_HINWEIS_SPRACHE` ist
  auf Deutsch mit Absicht leer). Ein Bündel trägt **Beschriftungen**, keinen Zustand — der steht
  weiter in `*Gaben` (`LizenzGaben`, `LizenzTextGaben`).
- **Zahlen komma- und punkttolerant**, kein Tausendertrennzeichen, invariant geparst — dieselbe
  Regel wie `WindowsFormsApplication1/Program.cs` (`ZahlParsen`/`GanzzahlParsen`). Eine Fehleingabe
  **färbt** das Feld (`epos-fehleingabe`), sie meldet nicht.
- **Ein Dialog rechnet Energiemengen NUR über `Energieeinheit` um** — nie mit einem nackten
  Faktor 1 000 (Anwenderentscheid **W8‑O‑5c**, 07.09.2026). Die Zahl kommt in ihrer Einheit
  herein, der Name sagt welche (`…Kwh`, `…Mwh`, `…Kw`); gerechnet wird im Kern, an den zwei
  Nähten `SimulationErgebnisCtrl` und `SimulationRunner`. Wächter:
  `EPOS.Kern.Tests/EinheitenWacheTests.cs` liest `EPOS.UI/**` auf die sieben Schreibweisen
  `/ 1000`, `* 1000`, `/= 1000`, `*= 1000`, `/ 4000`, `0.001`, `1e-3` und führt eine begründete
  Ausnahmeliste — heute nur Leistungen (W → kW). Die vollständige Regel steht in
  [`EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md), Abschnitt „Einheiten".
- **Beruhrungsziele mindestens 44 px** (`--epos-touchziel`), Warnfarben mit
  `@media (forced-colors: active)` absichern.
- **Aktionsknöpfe in einer Tabellenzeile sind IMMER sichtbar**, nie erst bei `:hover` —
  Berührung kennt kein Hover (iL4). Und: **kein `display: flex` auf einem `<td>` oder `<th>`.**
  Das nimmt der Zelle ihre Rolle als Tabellenzelle (CSS 2.1, 17.2.1) — der Browser schiebt eine
  anonyme `table-cell` darunter, die Spaltenbreite hängt nicht mehr an der Zelle, und jede
  Zellenregel (Polsterung, Trennlinie, Zeilenfarbe) trifft einen Kasten, der die Zelle nicht
  mehr ist. Der Flexkasten gehört **in** die Zelle (`.epos-zellenaktionen-inhalt`), und die
  Aktionsspalte bekommt einen **beschrifteten** Kopf, sonst hat sie nichts, woraus sie ihre
  Breite nehmen kann. Beides ist Befund **W5‑B‑1** der Windows-Abnahme vom 04.09.2026 —
  die Spalte war schlicht nicht zu sehen. Wache: `KostenSeiteTests`.
- **GEFILTERT WIRD VOR DEM RASTER, nie im Raster** (Anwenderentscheid **W14a‑E‑10** vom
  07.09.2026, Konzept_Katalogfilter 5.6.6). `Raster.razor` ist eine dünne Hülle um QuickGrid,
  und **QuickGrid filtert nicht**: `ColumnOptions` ist nur ein Ort im Kopf, an dem ein
  Bedienelement stehen darf. Der Wirt legt den Ausdruck in einen `Katalogfilterstand`, lässt
  `Katalogfilter.Anwenden` im Kern die Menge einschränken und reicht die **bereits
  eingeschränkte** Liste als `Zeilen` weiter — genau so, wie es der Wärmepumpenkatalog seit W7
  mit `WaermepumpenKatalogFilter` tut. **Kein `RefreshDataAsync()`**: Der Aufruf landet in dem
  QuickGrid-Zweig, der den Fehler W6‑B‑2 trägt, und rührt den flachen Zwischenspeicher nicht
  an (die Begründung steht im Kopf von `Raster.razor`). Für die 20 749 PV-Module heißt das:
  Das Raster bekommt nach dem Filtern 15 Zeilen und virtualisiert gar nicht mehr — der
  Wirt schaltet `Virtualisiert` an der **gefilterten** Zeilenzahl, und genau diesen Wechsel
  deckt das `@key` ab. Zwei Folgen, die man kennen muss: Ein neues QuickGrid heißt auch ein
  **geschlossenes Popover** (deshalb lebt der Filterstand im Wirt, nicht im `Spaltenfilter`),
  und die **Markierung hängt am Bezeichner** statt an der Zeilennummer — sonst zeigte sie nach
  jedem Filterschritt auf eine andere Zeile.
- **Jede Änderung am Stilblatt läuft durch `StilblattTests`** (`EPOS.UI.Tests`): Sie prüft alle
  `.css` unter `wwwroot` auf Klammerbilanz, Verschachtelung und `&` — und meldet Zeile **und**
  Selektor. **Kein CSS-Nesting im Haus:** Eine Regel in einer Regel ist nie Absicht, sondern eine
  verlorene Klammer. Befund **W6‑B‑1** der Windows-Abnahme vom 04.09.2026 — dem fehlenden `}`
  hinter `.epos-mehrzeilig { white-space: pre-line;` fielen **414 der 569 Blöcke** zum Opfer
  (Chromium las alles Folgende als darin verschachtelt), das Hauptfenster erschien als ungestyltes
  HTML. Der Browser meldet dabei nichts, und eine bunit-Probe sieht es nicht: Das Markup ist
  richtig, nur das Blatt ist es nicht.
- **Eine Farbe steht als TOKEN in `:root`, nicht als Rückfall in der Regel.**
  `var(--epos-marke, #005aa0)` sieht harmlos aus, schreibt den Wert aber an jede Stelle, an
  der das Token benutzt wird — er lässt sich dann nirgends mehr ändern. Wer einen Rückfall
  schreibt, legt das Token an. Einzige Ausnahme ist ein Wert, den der Aufrufer je Element
  setzt (`--epos-kachel-min` aus `Kachelraster`); dort IST der Rückfall die Vorgabe.
- **Sieben Token gehören NUR der Startseite** (`--epos-start-*`, Anwenderwunsch **W16b‑E‑5**
  vom 05.09.2026): `--epos-start-kasten-rahmen` (das kühle Blaugrau der zwei Kopfkästen),
  `--epos-start-leiste-flaeche` (der Grund unter den Reiterzungen),
  `--epos-start-reiter-aktiv` und `--epos-start-reiter-aktiv-text` (die gefüllte Zunge),
  `--epos-start-text-leise` (DimGray für jede Erläuterung), `--epos-start-knopf-flaeche`
  (LightGray der Fußknöpfe) und `--epos-start-zusammenfassung` (die Fläche des
  Zusammenfassungskastens). **Benutzen darf sie nur `Seiten/Start/*`.** Grund: `Form_Start`
  hatte eine eigene Handschrift — größere Schrift, kühlere Rahmen, eine gefüllte
  Reiterzunge, graue Knöpfe —, und die galt im Haus nirgends sonst. Wer sie in den
  gemeinsamen Farbsatz zöge (`--epos-rahmen-leise`, `--epos-text-leise`, `--epos-flaeche`),
  färbte sechzig Dialoge mit um. **Die Regel gilt allgemein:** Meint ein Vorbild nur EINE
  Seite, bekommt sie ein sprechendes eigenes Token — ein gemeinsames wird nicht gekippt.
  Wache: `Seiten/StartseiteAnmutungTests`.
- **Ein Kachelraster gehört in einen Reiter mit DREI Spalten; ein Zweispalten-Reiter
  bekommt einen BEDIENBLOCK** (Anwenderrückmeldung 12.09.2026, Auftrag **#233**). Das feste
  Raster aus W16b‑E‑7 ist auf drei Spalten zu 404 px gebaut. Steht es in einer schmalen
  Spalte, hat es nichts, woran es sich ausrichtet: Die einzelne Kachel wird zum Streifen, ihr
  84-px-Sinnbild nimmt die halbe Breite, und der Untertitel bricht dreimal um — genau der
  Befund am Reiter „Simulation“ (drei Elemente in drei Breiten: Kasten 600 px, Knopf 355 px,
  Kachel 190 px). **Der Bedienblock ist die Gegenform:** EINE feste Breite
  (`minmax(320px, 360px)`, sie wächst nicht mit — der Platz gehört der rechten Spalte), alles
  darin von Rand zu Rand, der Rang durch die Bauform: **ein** Hauptknopf in der Hauptfarbe
  (`epos-knopf--primaer`, 44 px) mit einer leisen Erklärzeile darunter, darunter die neutralen
  Zweitknöpfe, darunter Fortschritt und Sperrgründe. **Die Kachel fällt dabei nur als
  BAUFORM:** Beschriftung und Erläuterung kommen weiter aus dem Kachelregister
  (`StartKachel`, `Kachelschluessel`, `Kachelbilder`), es verschwindet kein Schlüssel und kein
  Bild — sonst brächen Hilfeschlüssel, Startfragen und `KiDialogKatalog`. Umgekehrt gilt:
  **Ein gesperrter Knopf muss gesperrt AUSSEHEN** — `.epos-knopf:disabled` graut nur die
  Schrift ab; wo ein Knopf allein auf einer Fläche steht („Ergebnis speichern“ ohne
  Ergebnis), gehört die leise Hausfläche dazu und ein `title` mit dem Grund. Wachen:
  `Seiten/StartreiterSimulationTests`, `Seiten/StartseiteAnmutungTests`.
- **Jede neue Schrift-auf-Fläche-Paarung hält 4,5:1.** Drei Farben des Vorläufers sind
  deshalb bewusst NICHT übernommen (weiß auf `#6876df` bei kleiner Schrift 3,76:1, die
  Zusammenfassungswerte in 128,128,255 auf `#f9fafc` 3,12:1, die Versionsfarbe 150,156,162
  auf Weiß 2,77:1); der Ersatz bleibt in derselben Farbfamilie. `#6876df` trägt weiterhin
  die Gattungszeile — 26 px fett ist GROSSE Schrift und braucht nur 3:1.
  `StartseiteAnmutungTests.Jede_neue_Paarung_haelt_den_Hauskontrast` rechnet die Werte aus
  dem Stilblatt nach und fällt rot aus, sobald jemand ein Token aufhellt.
- **Zwei Zustände desselben Bedienelements müssen SICHTBAR verschieden sein.** Befund
  **W16b‑B‑2b** der Windows-Abnahme vom 05.09.2026: Der Anwender meldete die Reiter der
  Startseite als „ausgegraut", obwohl ein Projekt offen und die Leiste frei war. Gemessen:
  `.epos-reiter-knopf` trägt `--epos-text-leise` (#5f5e5a), gesperrt
  `--epos-text-sehr-leise` (#888780) — 0x29 Unterschied je Kanal, bei 16 px halbfett nicht
  zu unterscheiden; das Vorbild zeichnete den freien Reiter **schwarz**
  (`tabControl_Wizard_DrawItem`, `Form_Start.cs` :129‑141). Wer einen Zustand allein über
  zwei benachbarte Grautöne trägt, hat ihn nicht getragen. Und: **eine bunit-Probe sieht das
  nicht** (Lehre W6‑B‑1) — geprüft wird die REGEL, so wie in
  `StartseiteTests.Ein_freier_Reiter_der_Startseite_traegt_die_Textfarbe` und
  `KostenSeiteTests.Die_Aktionszelle_traegt_im_Stilblatt_kein_display_flex`.
- **Ein Parametersatz aus einer Hülle trifft nur `[Parameter]`.** Ein Schlüssel ohne
  Entsprechung bricht beim ERSTEN Zeichnen — im Blazor-Verteiler, also ohne Namen und ohne
  Ort (Befund **W16c‑B12**, der Startabsturz). Zwei Wachen halten das fern:
  `EPOS.UI.Tests/ParametersatzTests` liest den Quelltext aller `Views/**/*Huelle.cs` und hält
  jede Stelle `new BlazorDialogForm<T>`/`BlazorSeite<T>` samt der gerufenen
  `*Gaben*`-Methoden gegen die `[Parameter]` von `T` (61 Stellen, dazu die 13
  Assistentenseiten), und `WindowsFormsApplication1/Allgemein/Blazor/Parametersatzwache`
  dasselbe am Gerät. **Wer eine Komponente umbenennt oder einen Parameter streicht, prüft die
  Hülle mit.**
- **Ein Titel, eine Stelle: Trägt die Überlagerung einen Titel, zeigt die Komponente keinen
  eigenen** (`epos-dialog-kopf--ohnetitel`, Hausregel **W11b‑B‑9**, Vorbild
  `WaermepumpeAnlageDialog.razor:96`; als eigene Regel benannt und auf **22** Stellen im Haus
  angewandt durch **Auftrag #187**, Abnahmeliste vom 11.09.2026). Befund: `<Ueberlagerung
  Titel="@X">` zeichnet ihren Kopf (`epos-ueberlagerung-titel`) UNBEDINGT — trägt die
  eingebettete Komponente denselben Text ODER denselben Ressourcenschlüssel unbedingt in ihrem
  eigenen `<h1 class="epos-dialog-titel">`, steht er zweimal übereinander. **Zwei Bauarten, je
  nachdem, wozu die Komponente `TitelText` sonst noch braucht:**
  (a) **`TitelText` bleibt leer** — der Regelfall: `<div class="epos-dialog-kopf
  @(string.IsNullOrEmpty(TitelText) ? "epos-dialog-kopf--ohnetitel" : "")">`, der `<h1>` nur
  `@if (!string.IsNullOrEmpty(TitelText))`, der Hilfeknopf bleibt immer. Die einbettende Stelle
  setzt `TitelText=""` — im Markup unmittelbar (elf `NamensDialog`-Einbettungen, z. B.
  `KatalogDublettenDialog.razor`) oder in der Hülle, wenn der Parametersatz aus einer
  `*Gaben()`-Methode kommt (`KostenKomponenteHuelle.CaseGaben`/`EditorGaben`,
  `KostenfaktorKatalogHuelle.Gaben`). **Speist dieselbe `Gaben()`-Methode AUCH ein
  eigenständiges Fenster** (z. B. `BhkwHuelle.KatalogGaben` für `KatalogBearbeiten` UND für die
  Überlagerung in `BhkwDialog`), bleibt sie UNVERÄNDERT — ein kleiner Helfer `OhneTitel(…)`
  kopiert den Satz NUR an der einbettenden Stelle und überschreibt darin `TitelText` (so in
  `BhkwHuelle`, `HeizkesselHuelle`, `KostenKomponenteHuelle.GesetzeGaben` für die geteilte
  `GesetzeskatalogHuelle.Gaben`). (b) **ein eigener `[Parameter] public bool TitelAnzeigen { get;
  set; } = true`** — wenn `TitelText` noch für etwas ANDERES im Rumpf gebraucht wird und deshalb
  nicht leer werden darf: `KlimazonenkarteDialog` speist daraus zugleich
  `Bildkarte.Bildbeschreibung`, `PufferSpProjektDialog` zugleich die Überschrift seines
  `Gruppenkopf`-Bestandsblocks — beide bekommen `TitelAnzeigen="false"` NUR an der
  Einbettungsstelle (`QuelleErdreichDialog`, `QuellePufferspeicherDialog`, `WaermesenkeDialog`,
  seit Auftrag **#194** auch die DRITTE Rolle von `PufferSpProjektDialog` direkt in
  `Seiten/Simulation/SimulationKonfigSeite.razor`), `TitelText` bleibt unverändert. **Wache:**
  `EPOS.UI.Tests/UeberlagerungstitelTests` — Bauart A rein am Markup (derselbe Bezeichner in
  `Titel=` und `TitelText=`; seit #194 prüft sie an jeder Einbettung mit Überlagerungstitel
  ZUSÄTZLICH den Wert von `TitelAnzeigen` selbst, wenn die eingebettete Komponente diesen
  Parameter führt — unabhängig davon, ob überhaupt ein `TitelText=` in der Einbettung steht),
  Bauart B an einer `Huelle.cs`-Methode, die `TitelText` auf denselben Schlüssel wie ihr eigenes
  `…Titel`-Feld setzt; alle mit Gegenprobe, Ausnahmeliste leer.
- **Jeder Dialog bietet den Assistenten an — und kein Dialog setzt ihn selbst.**
  Hausregel seit Auftrag **#199** (Stufe S1 des Konzepts „Der Hilfe-Assistent im Dialog").
  Der KI-Knopf steht nicht in 88 Masken einzeln, sondern **im `InfoKnopf`**: Er trägt
  ohnehin den Hilfeschlüssel — die einzige Stelle, die einen Dialog fachlich benennt —,
  und genau daraus leitet der Kern den Bereich ab
  (`KiChatKontext.BereichFuerHilfeschluessel`). Wer einen neuen Dialog baut, setzt seinen
  `<InfoKnopf Schluessel="…" />` wie bisher und bekommt den Assistenten geschenkt;
  **`Dialogname` dazuzugeben ist die einzige Kür** (er steht dann in der Kontextzeile des
  Chats). Ein `MitAssistent="false"` ist eine AUSNAHME und braucht einen Grund: Heute sind
  es vier — der Chat selbst, seine Werkzeugliste und die zwei Lizenzmasken; der Wächter
  `InfoknopfSchluesselWacheTests` hält beides (kein Info-Knopf ohne Schlüssel, keine
  fünfte Ausnahme). **Eine Meldung, die der Anwender nicht versteht, bekommt eine
  `Kennung`** (`KiMeldungskennung`) statt einer längeren Erklärung im Banner: Der Link
  „erklären lassen" führt dann auf den Abschnitt des Aktionswissens, und der steht auch
  ohne Netz und ohne Modell bereit. Der Öffnungsweg ist auf beiden Plattformen derselbe
  (`EPOS.UI.Dienste.KiAssistentWeg`), und **keine Komponente ruft `KiChatHuelle` oder eine
  Ansicht unmittelbar**.
- **Ein Dialog, der seine Feldwerte mitgeben soll, meldet sie an — in DREI Zeilen.**
  Hausregel seit Auftrag **#200** (Stufe S2, Weg 4). Die Feldliste steht im Kern
  (`KiDialoge`, der EINE Dialogkatalog), der Dialog steuert nur bei, WO seine Werte
  liegen:
  ```csharp
  private KiMaskenanmeldung? _kiMaske;
  protected override void OnInitialized()
      => _kiMaske = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL, () => Daten);
  public void Dispose() => _kiMaske?.Dispose();
  ```
  SECHS Komponenten tun das heute: `HeizkesselKatalogDialog`, `PhotovoltaikDialog`,
  `PufferSpKatalogDialog`, `WaermepumpeStammDialog`, `StromspeicherAuslegungSeite` und —
  seit Auftrag **#221** (KI‑D‑E‑1) — `Simulation/SimulationSeite` mit 18 Feldern aus
  `SimulationKiSicht`. **Der WIRT meldet an, nicht das Blatt darin:** Die
  Simulationsansicht führt vier Stände (Konfiguration, Laufparameter, Ergebnis, Schritt
  und Reiter), und nur sie sieht alle vier; `SimulationKonfigSeite` und
  `SimulationErgebnisSeite` melden nichts eigenes an.
  **Die Quelle ist ein DELEGAT und keine Instanz** — der Photovoltaik-Dialog meldet die
  GEWÄHLTE Zeile an, und die wechselt mit jedem Klick; ein festgehaltenes Objekt zeigte
  dem Assistenten die Zeile von vorhin. **Der zweite Parameter des Katalogs ist der
  Eigenschaftspfad** `Typ.Eigenschaft` (`HeizkesselKatalogDaten.Ptherm`), aufgelöst per
  Reflection; genau zwei Stufen, weil ein tieferer Pfad über eine Kette von
  `null`-Stellen liefe. Wo eine Ansicht Tiefe braucht, bekommt sie ein flaches
  **Sichtmodell** (`Seiten/Strom/StromspeicherKiSicht`), das die Kette EINMAL an einer
  benannten Stelle auflöst und bei jedem Zugriff neu rechnet — dasselbe tut seit #221
  `Seiten/Simulation/SimulationKiSicht`. **Angemeldet heißt nicht
  übertragen:** Ob ein Feldwert hinausgeht, entscheiden der Schalter „Feldwerte
  mitsenden" im Chat und die Einwilligungsstufe „Dialogdaten" (KI‑D‑Q2) — der Chat
  bekommt beides als Delegaten und kennt die Brücke nicht. Wächter:
  `EPOS.UI.Tests/Dialoge/Hilfe/KiDialogkatalogTests` (jeder Eigenschaftsname existiert
  am Daten-Objekt, mit Gegenprobe) und `KiFeldwerteTests` (Anmelden, Schalter, Vorschau,
  „ohne Einwilligung nichts").
- **Ein Dialog, in dem der Assistent MITARBEITEN soll, meldet seine HAKEN mit an.**
  Hausregel seit Auftrag **#201** (Stufe S3, Weg 5). Die Feldliste allein reicht zum
  Setzen nicht: Der neue Wert muss SICHTBAR werden, der Dialog prüft seine Eingaben
  SELBST, ein Katalogsatz kann schreibgeschützt sein, und Speichern und Rechnen sind
  Wege der Maske. Das alles steuert `KiMaskenhaken` bei — ein zweiter Parameter an
  derselben Zeile:
  ```csharp
  protected override void OnInitialized()
      => _kiMaske = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL, () => Daten, KiHaken());

  private KiMaskenhaken KiHaken() => new KiMaskenhaken
  {
      Auffrischen = () => _ = InvokeAsync(StateHasChanged),
      Pruefen     = () => EingabenPruefen() ? "" : _meldung,
      Speichern   = KiSpeichern
  };
  ```
  **Alles ist freiwillig, aber nichts ist beliebig.** Ohne `Auffrischen` stünde in der
  Maske die alte Zahl, während der Assistent „gesetzt" meldet. `Pruefen` ist **dieselbe**
  Prüfung, die der Speicherknopf ruft — der Assistent ersetzt sie nicht, er löst sie aus,
  und ihr Befund geht als Meldung in den Chat. `Schreibgeschuetzt` meldet das `ReadOnly`
  des Auslieferungskatalogs (heute nur `WaermepumpeStammDaten.NurLesen`), und dann lehnt
  schon `feld_setzen` ab statt erst der Speicherknopf. Wer keinen `Speichern`-Haken
  anmeldet, bekommt eine benannte Ablehnung statt eines stillen Nichts. **Ein Rechenweg
  trägt den AKTIONSNAMEN als Schlüssel** (`Rechenweg("flotte_bewerten", …)`), nicht einen
  eigenen — zwei Namensräume für dieselbe Sache laufen auseinander; er ruft denselben Weg
  wie der Knopf daneben, damit der Assistent nicht etwas anderes rechnet als die Ansicht.
  **Der Wert landet in der Eigenschaft, nicht an ihr vorbei:** Der Setzer der Anmeldung
  schreibt über dieselbe `PropertyInfo`, an der das Eingabefeld hängt, und die Umsetzung
  Text → Wert macht der Kern (`KiFeldwandler`, nach `ZahlText.Parsen`) — dafür geht der
  `PropertyType` mit in den `KiFeldzugang`. Wächter:
  `EPOS.UI.Tests/Dialoge/Hilfe/KiMaskenhakenTests` (jede der fünf Masken meldet ihre
  Haken) und `KiFeldSetzenTests` (Bestätigung → Feld geändert, je ein Fall pro Maske,
  dazu die Ablehnungen: abgeleitetes Feld, Typfehler, Schreibschutz, ohne Bestätigung).
- **Ein dauerhaftes Banner nur für einen Zustand, den der Anwender beheben MUSS und
  sonst nicht sieht.** Anwenderwunsch **W16b‑E‑6** vom 05.09.2026: Über der Startseite
  stand, solange kein Projekt offen war, ein `Warnbanner` mit den zwei Sätzen der
  Reitersperre — richtig gemeint (ein gesperrter Reiterknopf konnte seinen Grund nicht
  sagen) und trotzdem falsch. Eine Warnfläche über der ganzen Seite meldet einen
  FEHLER; hier fehlte nur der Anfang, und sie stand ausgerechnet im ersten Augenblick,
  in dem der Anwender das Programm sieht. **Die Staffelung, die stattdessen gilt:** eine
  LEISE Zeile dort, wo der Anwender ohnehin hinsieht (Erläuterungsschrift, kein Kasten);
  der GRUND am Bedienelement selbst (`title` + `aria-disabled`); und das Banner erst
  NACH dem Versuch, mit `Verfaellt` = 3 s. Ein Zustand, den der Anwender auf der Seite
  ablesen kann, braucht kein Banner — er braucht einen Satz.
- **Ein gesperrtes Bedienelement, das erklären soll, warum es gesperrt ist, darf nicht
  `disabled` sein.** Ein `disabled`-Knopf nimmt keine Zeigerereignisse an: Der Browser
  zeigt seinen `title` nicht, er feuert kein `click`, und eine Sprachausgabe sagt „nicht
  verfügbar" ohne den Grund. Wer erklären will, nimmt `aria-disabled="true"` und einen
  Handler, der den Versuch MELDET statt zu handeln — die **weiche Sperre**
  (`Reiterblatt.Sperrgrund` / `Reiter.Verweigert`, W16b‑E‑6). Beide Bauarten müssen
  **gleich aussehen**: Ein Anwender unterscheidet „geht nicht" nicht nach der Bauart,
  also stehen `:disabled` und `[aria-disabled="true"]` in EINER Stilregel und nicht in
  zwei mit denselben Werten.
- **Ein Baustein hängt sein Schließen NICHT an `focusout`.** Befund **W16c‑B13** der
  Windows-Abnahme vom 05.09.2026: Die Untermenüs des Menübands ließen sich nicht
  aufklappen. Am `<nav>` hing `@onfocusout`, um beim Klick daneben zu schließen —
  aber `focusout` **blast nach oben** und feuert AUCH, wenn der Fokus **innerhalb**
  des Bausteins wandert. Der Zeigerdruck auf eine Untermenüzeile nimmt dem
  Kopfknopf den Fokus, das Ereignis steigt zum Wirt auf, der räumt die Klappe weg —
  und weil die gedrückte Zeile damit **aus dem DOM** ist, kommt beim Loslassen gar
  kein `click` mehr an. Zu unterscheiden ist das nicht: `FocusEventArgs` kennt kein
  `relatedTarget`, und **Berührung setzt überhaupt keinen Fokus** — auf dem iPad
  schloss dasselbe Band nie. Wer beim Klick DANEBEN schließen will, nimmt eine
  **Schließfläche**: einen durchsichtigen Deckel über der ganzen Ansicht
  (`position: fixed; inset: 0`), der nur so lange steht, wie etwas offen ist. Er
  arbeitet auf Maus, Finger und Stift gleich und braucht kein JavaScript. Dazu
  gehören **drei z-Ebenen**, sonst finge der Deckel den Klick auf das Bedienelement
  selbst ab: Wirt über Deckel, Klappe im Wirt (`41 / 40 / 39` beim Menüband).
  Für die Tastatur bleibt `Esc` — und `Tab`, der ohnehin hinausführt. Wache:
  `MenuebandTests.Ein_Fokuswechsel_im_Band_schliesst_nichts_mehr`.
- **Jede Ebene einer verschachtelten Aufklapp-Struktur führt ihren EIGENEN
  Offen-Zustand — am besten als PFAD.** Zweiter Teil von **W16c‑B13**: Das Menüband
  führte ein Feld `_offen` für die oberste Ebene und daneben ein flaches `HashSet`
  über **Namen** für alle tieferen. Damit gab es keine Ebene mehr, auf der ein
  Geschwister das andere hätte schließen können — zwei Untermenüs standen
  gleichzeitig offen —, und ein Name, den es zweimal gäbe, öffnete zwei Stellen.
  Die tragfähige Form ist EINE Liste: `_pfad[0]` das offene Element der ersten
  Ebene, `_pfad[1]` das der zweiten, und so fort. `Offen(p, ebene)` fragt, ob `p`
  auf **seiner** Ebene im Pfad steht; Umschalten kürzt den Pfad auf diese Ebene und
  hängt `p` an. Daraus folgt beides von selbst: **gegenseitiger Ausschluss** je
  Ebene, und mit einem Kopf fällt sein ganzes Untermenü. Die Zeichenmethode
  bekommt die Ebene als Parameter — ohne sie weiß eine Zeile nicht, welche Stelle
  des Pfades sie meint.
- **In einer Schleife mit veränderlicher Tiefe: `builder.OpenRegion(index)`.** Ein
  durchlaufender Zähler als Folgenummer verschiebt beim Aufgehen eines
  Unterbaums **alle** folgenden Nummern; Blazor baut den Rest der Liste dann neu
  auf — samt Fokus. Und ein `AddElementReferenceCapture`, den es mal gibt und mal
  nicht, ist eine Rahmenart, die der Abgleich gar nicht entfernen kann
  (`NotImplementedException: Unexpected frame type during RemoveOldFrame`) — die
  Verweise werden für ALLE Elemente gefangen, ausgewählt wird beim Benutzen.
- **Jede Seite, die aus einer Kachel oder einem Menüpunkt aufgeht, muss AUCH OHNE GABEN
  zeichnen.** `StartkachelDialogeTests` rendert alle 21 Kachelziele mit einem LEEREN
  Wörterbuch — jeder Delegat `null`, jede Liste leer, jeder Text der deutsche Rückfall. Das ist
  der härtere Fall und der Zeuge dafür, dass eine leere Fläche beim Anwender **nicht** aus
  dieser Bibliothek kommt (Befund **W16b‑B‑1**).
- **Eine LISTE steht in einem festen Rahmen mit Rollbalken** (Anwenderregel 05.09.2026,
  Befund **W9‑B‑2** „Liste zu lang"). Die Liste „Gebäude in DB" wuchs mit ihrem Bestand
  ins Endlose und schob Filter, Detailblock und Schlussleiste meterweit nach unten — um
  an „OK" zu kommen, musste der Anwender die ganze SEITE rollen. Die Regel steht an der
  EINEN Stelle, die alle Listen des Hauses tragen: der Hüllenklasse
  `.epos-raster-huelle` — sie umschließt die handgeschriebenen Tabellen der
  Projekt/DB-Dialoge, das QuickGrid des Bausteins `Raster` und die `ProjektListe`.
  Sie trägt `max-height: var(--epos-listenhoehe)` (22 rem, ein Token in `:root`, in
  `rem` damit es mit der Schrift mitwächst), `overflow: auto` und einen **stehenden
  Spaltenkopf**; der Tastaturfokus rollt die Zeile von selbst ins Bild, weil jede
  Zeilenwahl ein `<button>` ist. Es ist eine **Höchsthöhe**: Eine Liste mit drei Zeilen
  bleibt drei Zeilen hoch. Wer eine Liste ausdrücklich ungebremst braucht, setzt
  `Begrenzt="false"` (`Raster`, `ProjektListe`) bzw. die Klasse
  `epos-raster-huelle--frei` — der benannte Rückweg, damit niemand die Regel still
  aufweicht. Wache: `EPOS.UI.Tests/ListenrahmenTests` (Regel **und** Markup — eine
  bunit-Probe allein sieht eine Stilregel nicht, Lehre W6‑B‑1).
- **Projekt ↔ Datenbank immer über `Zweispaltenauswahl`** (Anwenderentscheid **#76**
  vom 05.09.2026, **durch W14a‑E‑10‑Q2 vom 07.09.2026 ergänzt**). Ein Dialog mit einer
  Projektliste, einer Katalogliste und Pfeilknöpfen dazwischen baut das Muster **nicht**
  selbst: Er nimmt den Baustein und füllt `Links`, `Mitte` (die zwei Knöpfe als
  Parameter) und `Rechts`. **Was Q2 ändert, ist die ANORDNUNG, nicht die Aussage:** Aus
  nebeneinander (BHKW-PLAN — Projekt links, Katalog rechts, zwei Knöpfe in einer
  schmalen Mittelspalte) wird **untereinander** — Projektliste oben mit Höhengrenze
  (`--epos-projektlistenhoehe`, 12 rem = vier Zeilen), darunter die **Übernahmeleiste**
  mit den zwei Knöpfen als Textzeile, darunter die Katalogliste über die **ganze
  Breite**. Der Anwender hat es begründet: „Liste wie zuvor über ganze Breite, sonst zu
  schmale Liste" — nebeneinander rollte die Katalogliste schon mit sechs
  Parameterspalten in sich, untereinander trägt sie neun und rollt um 0 px. Die
  Medienabfrage bei 900 px **entfällt damit ersatzlos**: Es gibt nur noch EINE
  Anordnung, und untereinander war schon der schmale Fall. Mit ihr fallen die schmale
  Mittelspalte samt Token `--epos-zweispalten-mitte` und das **waagerechte Pfeilpaar
  ◀▶** — jeder Knopf trug bis dahin BEIDE Paare im Markup, weil eine Komponente nicht
  weiß, wie breit sie gezeichnet wird; jetzt genügt **ein** Zeichen je Knopf (▲ hinauf
  ins Projekt, ▼ hinunter in den Katalog), und es steht weiterhin **nie im
  Ressourcentext**, sondern als `aria-hidden`-Element daneben. Unverändert gilt:
  **Filter gehören unmittelbar über die Katalogliste** — dort steht die Liste, auf die
  sie wirken —, **Detailblöcke unter das Paar** über die volle Breite; was nur die
  Projektzeile betrifft (Summenzeile, Kanalwahl), bleibt oben. Die Parameternamen
  `Links`/`Rechts` **bleiben**: Sie benennen die Rolle (Projekt- und Katalogblock),
  nicht den Platz — aus demselben Grund heißt der Baustein weiter
  `Zweispaltenauswahl`. Elf Dialoge folgen der Regel, **sechs** mit Katalog
  (die Erzeuger-Projektdialoge samt Solarkollektoren) und **fünf** ohne
  (Gebäude, Bedarfsprofile, Wärmebedarf extern, Stromganglinie,
  Solarganglinie); **eine** Liste plus Detailblock ist etwas anderes und bleibt
  es (`KatalogBrowserDialog`, `WaermepumpenDialog`). Wache:
  `EPOS.UI.Tests/Bausteine/ZweispaltenauswahlTests` — sie führt die Liste der elf und
  fällt rot aus, sobald eine Komponente die Pfeilspalte wieder selbst baut.
- **Der Filterstand eines Katalogs gehört dem KERN, nicht der Komponente**
  (Anwenderentscheid **W14a‑E‑10‑Q2**, Stufe S2.5). Ein Katalogdialog holt seinen
  `Katalogfilterstand` aus `Katalogfilterregister.Stand(art)` — je Katalog **einer**,
  über die **Sitzung**, gemeinsam für Verwaltung und Projektdialog: Wer im
  Projektdialog nach Gas filtert, die Verwaltung öffnet und zurückkommt, findet seinen
  Filter wieder, denn es ist derselbe Katalog. Ein `static`-Feld in der Komponente wäre
  prozessweiter Zustand an einem Ort, den weder eine Kern- noch eine bunit-Probe
  zurücksetzen kann. Jeder der neun Katalogdialoge führt dafür `Filterstandvorgabe` als
  benannten Rückweg (Muster `ProfilVorgabe`) — **die Prüfstände nehmen ihn**, weil
  xunit Testklassen nebeneinander fährt und ein geteiltes Register sie sonst
  gegeneinander laufen ließe.
- **Ein KATALOGDIALOG nutzt die Höhe** (Anwenderwunsch 05.09.2026, „Admin-Menüs sind
  nicht an Größe Bildschirm angepasst"). Der Befund war „Administration
  Solarkollektoren": Überschrift, Balken „Auswahl in DB:", eine Liste im eigenen kleinen
  Rollrahmen — und darunter, nur über den SEITENrollbalken erreichbar, der Balken
  „Eingabe der Solarkollektoren". Alle sechs Verwaltungsmasken des Bestands stellten
  Liste und Eingabe **nebeneinander** (`Form_Heizkessel_Admin` 726 × 383,
  `Form_BHKWAdmin` 856 × 517, `Form_SolarKollektorenAdmin` 825 × 494,
  `Form_PufferSp_Admin` 721 × 330, `Form_AdminPV` 607 × 489,
  `Form_AdminStromspeicher` 614 × 367). Ein Dialog mit **einer** Liste und **einem**
  Eingabe- oder Detailblock baut das nicht selbst: Er setzt an seine Wurzel
  `epos-katalog-dialog` und füllt den Baustein **`Katalograhmen`** (`Liste`, `Eingabe`,
  dazu `Gestapelt` für die Masken, deren Vorbild schon untereinander stand —
  `Form_AdminWaermeeinlesen` 676 × 433, `Form_Stromverbraucher_Admin` 542 × 489).
  **Hier und nur hier fällt die Höchsthöhe aus W9‑B‑2**: Im Rahmen nimmt die Liste die
  verbleibende Höhe, weil der Eingabeblock daneben steht und selbst rollt; überall sonst
  gilt `--epos-listenhoehe` weiter. Der Umbruch ist eine **Medienabfrage bei 900 px**
  wie bei `Zweispaltenauswahl` — nicht 1100 px: Die WebView rechnet in CSS-Pixeln, das
  Fenstermaß in Gerätepixeln, und bei 150 % sind 1632 Gerätepixel nur 1088 CSS-Pixel.
  Eine Verwaltung **ohne** Eingabeblock (Gesetzeskatalog) nimmt keinen Rahmen, sondern
  eine Listenspalte `epos-katalog-liste epos-katalog-fuellend` über die volle Breite.
  Wachen: `EPOS.UI.Tests/Bausteine/KatalograhmenTests` (Markup **und** Stilblatt) und
  `EPOS.UI.Tests/KatalogdialogTests` (sieben Dialoge, Kopfzeile „Wahl").
- **Ein PARAMETERBLOCK steht im `Formularraster`** (Anwenderwunsch **iU8‑E‑2 /
  W14a‑E‑7**, 05.09.2026: „Verbessere die Darstellung der Dialoge, insbesondere der
  Parameter auf der rechten Seite: kompakter, übersichtlicher"). Der Befund war
  „Administration Photovoltaik Module": Im Katalograhmen aus W14a‑E‑6 nahm rechts
  JEDES Feld die volle Breite, die Beschriftung stand DARÜBER, die Zahlenfelder waren
  so breit wie die Textfelder, und die Einheit stand am rechten Rand des Blocks statt
  hinter dem Wert — der Block war doppelt so hoch wie der Dialog und rollte. Das
  Vorbild tat es anders, **gemessen** an `Form_AdminPV.resx` (607 × 489):
  Beschriftungsspalte **178 px** (Label x = 253, Feld x = 431), Zahlenfeld **62 px**,
  Einheit **4 px dahinter** (x = 497), Textfeld 250 px, Beschreibung 250 × 48.
  **Die Regel:** Ein Dialog baut das nicht selbst, er hängt seinen vorhandenen Feldlauf
  in `<Formularraster>` — dann steht die Beschriftung neben dem Feld, die Felder ordnen
  sich in ein oder zwei Spalten (`auto-fill`/`minmax`, nach der Breite des RASTERS), und
  ein Feld sagt **selbst**, wie lang es ist: `Zahlenfeld`/`Ganzzahlfeld` tragen
  `epos-feld--kurz`, ein mehrzeiliges `Textfeld` `epos-feld--breit`. Ein `Datumsfeld` ist
  **nicht** kurz — `input type="date"` schneidet unter rund 130 px sein Kalendersymbol ab.
  Die GRUPPEN kommen aus dem Dialog (`Formulargruppe`), der Baustein liefert nur das
  Raster. **Nichts davon greift außerhalb** von `.epos-formularraster`: Ein Feld sonst im
  Haus behält seine Form, sonst hätte ein Baustein 92 Dateien auf einmal verschoben —
  dieselbe Vorsicht wie bei den sieben `--epos-start-*`-Token (W16b‑E‑5). Und die
  Kompaktheit kommt aus der **Anordnung**, nicht aus kleineren Bedienelementen:
  `--epos-touchziel` (44 px) bleibt die Mindesthöhe jedes Feldes. Wachen:
  `EPOS.UI.Tests/Bausteine/FormularrasterTests` (Markup, Selbstmeldung der Felder,
  fünf Stilblattfälle **und** ein Fall, der jede Selektorzeile des Blocks auf
  `.epos-formularraster` prüft) und zwei Fälle in `KatalogdialogTests`.
- **Das Vorgabemaß eines Fensters rechnet `Dienste/Fenstermass.cs`**, nicht die
  Windows-Hülle: Es ist Arithmetik auf vier ganzen Zahlen und deshalb hier prüfbar
  (`FenstermassTests`). Ein `Dialogart.Fachdialog` öffnet im **Anteil des
  Arbeitsbereichs** (85 % Breite × 90 % Höhe, gedeckelt auf 92 %), eine
  `Dialogart.Klein`e Maske bleibt bei ihrem Wunschmaß. Wer eine Größe braucht, holt sie
  hier — eine zweite Fassung der Zahlen läuft irgendwann auseinander.
- **Eine Klappliste IN einer Tabelle wird gedeckelt und schneidet ab** (Hausregel seit
  Befund **W6‑B‑4**, Windows-Abnahme 07.09.2026). Ein `<select>` ist so breit wie sein
  längster Eintrag — „SMA America: SB30-1SP-US-40 {240V}" sind 302 px —, und in einer
  Tabelle schiebt es damit jede Spalte rechts von sich aus dem Bild. Die Spalte bekommt
  deshalb eine Klasse mit `max-width`, `overflow: hidden` und `text-overflow: ellipsis`,
  und die Zelle trägt den vollen Namen im `title`. **Elastische Spalten stehen HINTEN**,
  Zahlenfelder vorn: Was gedeckelt werden muss, kostet am Ende am wenigsten, und keine
  Zahl kann davon nach rechts geschoben werden. Zwei Fallen dabei: Die Hausregel
  `.epos-raster th, .epos-raster td { white-space: nowrap }` wiegt **(0,1,1)** und
  schlägt eine blosse Klasse — wer einen Spaltenkopf umbrechen lassen will, schreibt
  `th.klasse, td.klasse`; und ein `<input>` ist ohne Vorgabe rund 196 px breit, also
  bekommt jedes Zahlenfeld in einer Tabelle eine feste kleine `width`. **bunit misst
  keine Breite** (Lehre W6‑B‑1): Der Nachweis ist eine Playwright-Probe im Laufordner,
  im Repository steht die REGEL als `StilblattTests`-Fall und die Spaltenfolge als
  bunit-Fall auf das Markup. Die Regel zum Zahlenfeld gilt auch im Zeilenraster (#186):
  `.epos-zr-zelle .epos-eingabe` trägt seither dieselben zwei Zeilen (`min-width: 0`,
  `width: 100%`), sonst quillt die Einheit eines Zahlenfelds in die Nachbarspalte — dort
  Satz → Betrag der Kostenverwaltung.
- **Die gewählte `<option>` trägt `selected`, und jede `<option>` trägt ein `@key`**
  (`Standards/Auswahlfeld.razor`, seit W6‑B‑4). Ein `<select>` hat im DOM kein
  `value`-Attribut; Blazor merkt sich den Wert beim Einhängen und setzt `element.value`
  danach nach — **genau einmal, beim Erzeugen des Elements**. Wird die Eintragsliste
  später ausgetauscht (ein Herstellerfilter tut das), patcht Blazor die `<option>` an Ort
  und Stelle, der Browser behält seinen `selectedIndex`, und die Zeile zeigt den Namen,
  der zufällig an diesem Platz gelandet ist. Wer eine eigene Liste baut, macht es wie
  `Auswahlfeld`: `selected` ins Markup, `@key` an jede Option.
- Bezeichner und Kommentare deutsch; neue `.razor`/`.cs` UTF-8 **mit** BOM, LF.
- Jeder Baustein bekommt einen `bunit`-Test in `EPOS.UI.Tests` (Darstellung, Callback,
  Zustandsklasse).
- **Der Windows-Läufer der CI läuft unter der Systemkultur `en-US`**, nicht `de-DE`, und
  `Resource.*` löst ohne eigene Vorgabe über `CultureInfo.CurrentUICulture` auf — jede
  `EPOS.UI.Tests`-Klasse mit einem deutschen Text-Assert führt deshalb die Hausvorrichtung
  (`Kulturvorrichtung`/`EposBunitContext`, `EPOS.UI.Tests/Kulturvorrichtung.cs`, seit #168).
  Das Gate prüft `EPOS.UI.Tests` seit Auftrag #230b (11.09.2026) zusätzlich unter
  `LANG=en_US.UTF-8`, sequenziell **und** parallel.

## Bausteine (`Bausteine/`)

| Komponente | Zweck | Vorbild in WinForms |
|---|---|---|
| `Gruppenkopf` | Abschnittsbalken mit Titel, Symbol und Summe | `Views/Kosten/SectionPanel.cs` |
| `Warnbanner` | Hinweis / Warnung / Fehler, `role="alert"`. Seit iU9‑W15b.1 mit **Selbstverfall**: `Verfaellt` (TimeSpan?), `Verfallen` und einer austauschbaren `Uhr` — der Ersatz für `Form_Hinweis`, den Kurzhinweis, der sich nach drei Sekunden selbst schloss. Eine NEUE Meldung setzt den Verfall zurück; eine Frist ≤ 0 heißt „kein Verfall", nicht „sofort weg". **Seit Auftrag #199 mit `Kennung`** (Stufe S1, Weg 2): Ist sie gesetzt und der Assistent möglich (`KiAssistentWeg.Moeglich`), steht rechts der Link „erklären lassen"; er öffnet den Hilfe-Assistenten mit der Kennung, dem Bereich aus `Hilfeschluessel` und der vorbelegten Frage `KI_FRAGE_<Kennung>` (sonst dem allgemeinen Satz mit dem Bannertext). Die Kennung ist SPRACHNEUTRAL (`KiMeldungskennung`) und nicht der Meldungstext — der wechselt mit jeder Zahl darin | `KartenStil.WARN_*`; Selbstverfall: `Allgemein/Form_Hinweis.cs` |
| `SpeichernLeiste` | OK / Abbrechen und optional „Speichern" ohne Schließen samt Statuszeile | `Allgemein/SpeichernLeiste.cs` |
| `Fehlerschranke` + `Wurzel<T>` | **Das Sicherheitsnetz um jede Wurzelkomponente** (Befund **W13‑B‑1**, Windows-Abnahme 05.09.2026). Die Schranke ist eine `ErrorBoundary` auf `ErrorBoundaryBase` (ohne DI, damit sie den Fehler ZEIGT statt ihn zu loggen): Wirft ein Nachfahr, steht statt der Maske ein markierbarer Kasten mit Typ, Wortlaut und innerster Ausnahme, dazu „Weiter"/„Schließen" (beide `Recover()`); derselbe Satz geht nach `Debug`/`Trace`, Kopf `[Fehlerschranke]`. `Wurzel<T>` ist das nötige Zwischenglied — eine Boundary fängt ihre NACHFAHREN, eine Wurzelkomponente hat aber nichts über sich; die drei Hüllen mounten seither `Wurzel<T>` statt `T` und reichen den Parametersatz unverändert durch (`CaptureUnmatchedValues`). `Wurzel` zeichnet **nichts Eigenes**. Ohne dieses Netz beendete eine Ausnahme aus Ereignis oder Lebenszyklus den PROZESS: Der WinForms-`BlazorWebView` (10.0.100) führt kein `UnhandledException` | — (neu; die Lücke, die `WebViewWache` im Klassenkopf als ihre Grenze nennt) |
| `InfoKnopf` | 28×28-Fragezeichen, ruft `IHilfeDienst.Oeffnen`. **Seit Auftrag #199 trägt er den `KiKnopf`** (Stufe S1, Weg 1): `MitAssistent` (Vorgabe `true`) zeichnet ihn RECHTS daneben, sobald `KiVerfuegbarkeit.Moeglich` es sagt; der Klick ruft `KiAssistentWeg.AusDialog(Schluessel, Dialogname)`. Damit tragen **110 Einbaustellen** den Assistenten in EINEM Schritt — der Bereich folgt aus demselben Hilfeschlüssel. **Seit Auftrag #218 (Anwenderentscheid 11.09.2026, „KI-Knopf: Variante C + Variante D") sind beide EIN Element**: eine Pille `epos-hilfepille` mit zwei Feldern `epos-hilfepille__feld` in einem gemeinsamen Rahmen (16 px Rundung, Trennlinie zwischen den Feldern) — links unverändert das i (jetzt Inline-SVG statt `help_icon.png`), rechts, wenn der Assistent möglich ist, die EPOS-Plan-Marke in ihren drei Farben (Variante C, nachgezeichnet aus `Resources/Logo125_125.jpg`). Ohne Assistenten bleibt nur das linke Feld — die Pille sieht dann wie ein gewöhnlicher runder Knopf aus. Ein neuer Parameter `Aktiv` (Vorgabe `false`, ohne Pflicht für einen Wirt) füllt das rechte Feld mit dem Blau der Marke und einem weißen Blitz, sobald die Ansicht `KI_ASSISTENT` steht — gesetzt werden darf er von einem Kopf, der das weiß (`AppWurzel`, `SimulationSeite`, `StromspeicherAuslegungSeite`). **Seit Auftrag #221 (Anwenderentscheid KI‑D‑E‑1) gilt: EINE PILLE JE BILDSCHIRM** — der Anwender meldete „zwei KI-Buttons ohne unterschiedliche Funktion im Kontext". Unter Windows trägt sie das Kopfband des `Hauptfenster`s, und ihr `Schluessel` FOLGT der aktiven Ansicht (`AppWurzel.AktiverHilfekontext`); die Ansichten mit eigener Pille (`SimulationSeite`, `StromspeicherAuslegungSeite`, `AssistentSeite`, `BerichteKostenSeite` als freie Ansicht, dazu der Kopf von `SimulationKonfigSeite`) zeichnen ihre nur, wenn der `CascadingValue` **`HilfePilleImKopfband`** fehlt — also auf iOS. **`Aktiv` ist seither verdrahtet:** `KiChatKontext.AssistentStehtFuer(schluessel)` sagt, ob der Chat gerade FÜR DIESE Ansicht steht, und `KiChatKontext.AufrufGeaendert` meldet das Auf- und Zugehen des eigenen Chatfensters in die WebView hinein. **Inline-Knöpfe im Inhalt behalten ihre Pille** („Berechnungsweg…", `Form_*.Berechnung`): Sie tragen einen anderen Schlüssel und führen auf eine andere Hilfeseite — die Wache `HilfePilleTests` prüft nicht „eine Pille", sondern **kein Schlüssel zweimal**. Die Ressource `KI_KNOPF_HILFE` ist mit der Textbeschriftung entfallen (kein Leser mehr). **Seit Auftrag #227** trägt das rechte Feld einen `title`/`aria-label` mit dem BILDSCHIRMNAMEN, sobald `Dialogname` gesetzt ist („Simulation vom Hilfe-Assistenten erklären lassen"), und `KiChatDialog` zeigt bei jedem Aufruf mit Hilfeschlüssel über dem leeren Verlauf eine **Startzeile** mit zwei Knöpfen, die die vorbereitete oder eine allgemeine Erklärfrage sofort abschicken | `Allgemein/Hilfe/InfoKnopf.cs` |
| `Kachel` | Anklickbare Einstiegskarte mit Titel, Beschreibung, Status. Seit iU9‑W16a.2 mit **`Zustand`** (`Kachelstand.Aus`/`An` — grauer oder grüner Statuspunkt, der Punkt in beiden Fällen sichtbar) und **`Aktiv`** (`<button disabled>`, der Ersatz für die vierzehn Zeilen `Cursors.Default` in `AktionsKarte`). **Befund W16a‑B1:** „nur Anzeige" ist KEIN dritter Zustand — Farbe und Anklickbarkeit sind zwei unabhängige Achsen (Brauchwasser ist „nur Anzeige" UND grün oder grau). Seit dem Anwenderwunsch 05.09.2026 (**W16b‑E‑3**) dazu **`Bildklasse`** — eine Zusatzklasse am `<img>`, mit der die Startseite ihre Kachelbilder zuschneidet; die Kachel entscheidet das nicht, sie nimmt die Klasse entgegen | `Views/Kosten/EinstiegsKarte.cs`, `Views/GemeinsameBausteine/AktionsKarte.cs` |
| `Menueband` + `Menuepunkt`/`Menuetabelle` | Die **Windows-Schale** des Hauptfensters (iU9‑W16c.1): eine Menüleiste mit **59 Punkten in VIER Ebenen unter VIER Köpfen** und 8 Trennstrichen, `role="menubar"/"menuitem"/"menu"`, ← → ↑ ↓ Pos1/Ende/Esc/Tab, roving `tabindex`, 44 px je Zeile. Der Offen-Zustand ist **EIN Pfad** über alle Ebenen — er ist tiefenunabhängig und trug die vierte Ebene aus W16c‑E‑7 ohne eine Zeile Änderung —, geschlossen wird über eine **Schließfläche** und nicht über `focusout` — beides ist Befund **W16c‑B13** der Windows-Abnahme vom 05.09.2026 (die Untermenüs klappten nicht auf), und beides steht als Hausregel oben. Die Tabelle ist **Daten** — `Menuetabelle.cs` ist EINMAL aus `MDIMainForm.Designer.cs` und den drei `.resx` **erzeugt** worden (Auflage R‑W16‑8, Skript `w16c_menue.py`), nicht abgetippt; **seither ist sie die Quelle**, denn der Designer ist gelöscht und das Erzeugerskript liegt nicht im Repository. **Wer das Menü ändert, ändert diese Datei** — sie sagt das im Kopfkommentar und führt dort jeden Anwenderentscheid, den ein neuer Lauf des Skripts wieder zunichte machte. Drei stehen dort: der Kopf **„Sprache"** (`MENU_SPRACHE`, en „Language") aus **W16c‑E‑2** vom 04.09.2026 — die zwei Sprachpunkte standen bis dahin als eigene Köpfe neben „Hilfe" und hängen seither unter ihm, mit unveränderten Namen, Bildern und Seitenschlüsseln — und die **Umordnung des Kopfes „Administration"** aus **W16c‑E‑6** vom 06.09.2026: BHKW und Solarkollektoren von „Energiesysteme" nach „Wärmebedarf & Heizung", Pufferspeicher die Gegenrichtung, die drei Zeitreihen (Wärmebedarf Lastgang, Prozesswärme, Solarthermieganglinie) in die neue Unterrubrik **„Profile & Lastgänge"** (`MENU_PROFILE_LASTGAENGE`, en „Profiles & load curves"), und die zwei Untermenüs mit dem einzigen Punkt „Bearbeiten" aufgelöst — `MenuItem_PV` und `MenuItem_Solarkollektoren` tragen jetzt selbst das Ziel ihres früheren Kindes — und, als drittes, die zwei Zwischenknoten **„Photovoltaik"** (`MENU_PHOTOVOLTAIK`, en „Photovoltaics") aus **W16c‑E‑7** vom 07.09.2026: Das Paar Modul/Wechselrichter steht an BEIDEN Stellen des Kopfs „Administration" unter einem eigenen Knoten — als Katalog unter „Energiesysteme" („PV Module", `MENU_PV_MODULE`, und „Wechselrichter"), als Import unter „Daten & Import" („PV Module (CEC, PAN)…" und „Wechselrichter (CEC, OND)…"); Ziel, Argument und Kennung der vier Punkte sind unverändert, es wandert ihre Lage im Baum. Und als viertes **W16c‑O‑7** vom selben Tag, der einzige Entscheid, der eine Zeile WEGNIMMT statt eine anzulegen: Der Knoten `MenuItem_Klima` führte als einziges Kind `MenuItem_Klimadaten` — dieselbe Beschriftung zweimal, ein Klick zuviel —, und der Punkt steht seither unmittelbar im Kopf „Administration", an der Stelle des Knotens und mit dessen Bild `Menu4`; `MENU_KLIMA` bleibt wie `MENU_PC_BEARBEITEN` und `MENU_PV` ungelesen im Katalog stehen. Als fünftes steht dort **W13‑E‑2** vom selben Tag — der EINZIGE Entscheid dieser Reihe, der einen NEUEN Weg anlegt: `MenuItem_SP_Import` („Stromspeicher (CEC, bslib)…", `MENU_SP_IMPORT`, en „Battery storage (CEC, bslib)…") in „Daten & Import", hinter dem Knoten „Photovoltaik" und vor den Solarkollektoren. **Die Zahl, die dabei steht, ist 44:** So viele Punkte handeln, vor und nach jedem der vier UMORDNENDEN Entscheide; kein Ziel ist entfallen und keines hinzugekommen, es steht nur woanders im Baum. Gewachsen ist sie dreimal, und jedes Mal um einen echten neuen Weg: mit **W6‑E‑2** um den Wechselrichterkatalog und seinen Import (42 → 44), mit **W13‑E‑2** um den Stromspeicherimport (44 → 45) und mit **SIM‑Q3** (Auftrag #207, 11.09.2026) um „Simulation…" (`MenuItem_Simulation`, `MENU_SIMULATION`) im Kopf „Projekt", unmittelbar hinter „Varianten und Bericht…" (45 → **46**) — der erste Menüweg, den die Simulation überhaupt hat, und kein Untermenü. Und eine Regel steht mit W16c‑E‑6/E‑7 fest: **kein Untermenü mit nur EINEM Punkt** — je zwei; seit W16c‑O‑7 gilt sie OHNE Ausnahme, und `MenuebandTests` hält die Liste der Einzelgänger leer. Ein Punkt trägt den sprachneutralen Namen des Vorläufers (der Anker für `help_mapping.txt`), einen `MyResource`-Textschlüssel, ein **Ziel als `Seitenschluessel`**, ein Argument, ein Bild und ein Kürzel; das Band kennt weder Maske noch Delegat, es **meldet den Schlüssel**. Damit ersetzt ein Handler 34 Ereignishandler und neun `Init*`-Lambdas. Ein Kopf kann **rechtsbündig** stehen (`Menuepunkt.RechtsBuendig`, Anwenderwunsch 05.09.2026 / **W16c‑E‑4**): Das Band hängt daran `margin-left: auto`, der Punkt bleibt aber an SEINER Stelle im Markup — Tastaturweg, Sprachausgabe und N4 bleiben unberührt. Genau ein Kopf trägt es: „Sprache“, dort wo im Bestand die zwei Sprachpunkte sassen. Die elf Bildchen sind dieselben PNG (`wwwroot/bilder/menue/`) | `MDIMainForm.menuToolbar` (45 `ToolStripMenuItem` + 6 `ToolStripSeparator` im Designer, 9 Punkte und 2 Trenner aus den acht `Init*`-Methoden) |
| `Assistent` | Der Rahmen eines mehrstufigen Ablaufs (iU9‑W16a.5): linkes Band, Inhaltsfläche, Fußleiste `[Abbrechen] [◀ Zurück] [Weiter ▶ / Speichern]`. `Seiten` ist eine Liste aus `AssistentSchritt` (Titel, Inhalt als `RenderFragment`, `Aktiv`); `NaechsteAktive(richtung)` ersetzt `Next`/`Back`/`GetNextUpIndex`/`GetNextDownIndex`/`lastIndex` (rund 190 Zeilen), und `LoadNewForm` (32 Zeilen gerechnete Fenstergröße) entfällt ersatzlos — CSS | `Views/Wizard/WizardParent.cs` |
| `Herleitungszeile` | Leise Erläuterung, optional mit Formel | Inline-Labels der Kostenmasken |
| `Kohaerenzzeile` | Text mit Zustand „stimmig" / „abweichend" | Inline-Labels der Kostenmasken |
| `Optionsgruppe` | Sich ausschließende Optionen (`fieldset role="radiogroup"`), einzeln sperrbar | die 45 `RadioButton` des Bestands |
| `Zeilenwahl` | Der runde Wahlknopf einer Rasterzeile (`aria-pressed`, 44 px); seit iU9‑W13.0l mit **Mehrfachmodus** — `Mehrfach` macht daraus ein Kontrollkästchen (`role="checkbox"`), und `Tastenwahl` meldet `Strg`/`Umschalt` mit. Seit **W6‑E‑5** (07.09.2026) dazu `Doppelklick` — der schnelle Weg „wählen und übernehmen“ der Importe; der Browser schickt davor zwei Klicks, die sich mit der Umschaltregel aufheben, weshalb der Wirt die Zeile im Doppelklick mit `Zeilenmarkierung.Hinzufuegen` in die Wahl nimmt statt sie umzuschalten. Seit iU9‑W4 (W4‑E‑1) dazu die **breite Zeile**: `Beschriftung` macht aus dem runden Rasterziel eine volle, linksbündige Listenzeile mit dem Zeichen vorn (`epos-zeilenwahl--breit`), `Zusatzklasse` hängt eine Klasse des Wirtes an. Für schmale Auswahlspalten, in denen die **Zeile ihren Namen selbst trägt** statt neben einer Namensspalte zu stehen (Trägerliste der Energieträgerverwaltung) — **ein** Klickziel und **ein** Tabulatorhalt je Zeile. Ohne `Beschriftung` bleibt das Markup unverändert | die Zeilenmarkierung von `ListView`/`DataGridView`, die ein `Raster` nicht kennt |
| `Zeilenmarkierung` | **kein Markup, eine Regel**: die Markierung einer Rasterliste über Anzeigeindizes. **Seit W6‑E‑5 (07.09.2026) schaltet der einfache Klick UM** — `Strg` tut dasselbe, `Umschalt` nimmt den Bereich ab dem Anker DAZU, `Hinzufuegen` setzt eine Zeile ohne Umschalten (Doppelklick, Wiederherstellen nach einem Filterwechsel). Vorher galt die Semantik der `ListBox` mit `MultiExtended`, in der ein Klick die Wahl ersetzte; die Spalte zeigt aber ein Kontroll­kästchen, und wer zwei Zeilen anklickte, hatte am Ende eine („die Mehrfachauswahl funktioniert nicht“). Weil die Regel hier steht, gilt sie für alle sechs Importe auf einen Schlag. `AufAnzahlBegrenzen` wirft nach einem Filterwechsel hinaus, was hinter der neuen Liste liegt, `QuellIndizes` bildet auf die Importliste ab (Zwilling von `VdiAuswahlFilter.QuellIndizes`) | `ListBox.SelectionMode = MultiExtended` der vier Einlesemasken (iU9‑W13.0l) |
| `Ueberlagerung` | Modaler Bereich **innerhalb** der Komponente — Abdunkelung, `role="dialog"`, Esc, Fokusfalle ohne JS; `Zusatzklasse` (seit W7‑E‑2, 06.09.2026) hängt eine CSS-Klasse an den Rahmen, etwa `epos-ueberlagerung--breit` für die Dreispalten-Detailansicht der Wärmepumpe | ein zweites modales `Form`, das es in der WebView nicht geben darf (R2) |
| `Rueckfrage` | Ja / Nein / Abbrechen über der `Ueberlagerung` | die ≈ 500 `MessageBox`-Rückfragen des Bestands |
| `Zeilenraster` | Spaltenkopf, Bearbeitungszeilen, Abschlusszeile, Summenfuß — CSS-Raster mit `display:contents` | `Views/Kosten/Form_KostenKomponente` (pnlRasterKopf + pnlZeilen + pnlFuss) |
| `Mehrfachauswahl` | Liste mit Haken samt „Alle"/„Keine" | `CheckedListBox` (`Form_Energietraeger.KatalogUebernahme`) |
| `Reiter` + `Reiterblatt` | Reiterleiste; die Blätter melden sich selbst an, ein ungewähltes wird **gar nicht** gezeichnet (`role="tablist"/"tab"/"tabpanel"`, ←/→, Pos1/Ende, 44 px). Seit iU9‑W16b (**W16b‑E‑6**) **zwei Bauarten der Sperre**: `Bedienbar="false"` sperrt HART (`<button disabled>`, der Stand seit W5.0); nennt das Blatt dazu einen **`Sperrgrund`**, sperrt es WEICH — der Knopf bleibt ein Knopf, trägt `aria-disabled` und den Grund als `title`, und der Versuch meldet sich beim Wirt als **`Verweigert`**. Die Pfeiltasten überspringen beide gleichermaßen | die 21 `TabControl` mit 74 `TabPage` |
| `Kachelraster` | Reihe gleich breiter Karten, `auto-fit`/`minmax` statt gerechneter Prozentspalten; mit `Hoechstbreite` (W16b‑E‑7, 05.09.2026) drei feste Spalten von höchstens `--epos-kachel-max`, linksbündig, ohne Mitwachsen — die Startreiter setzen 404. **Nur in einem Reiter, der drei Spalten hat** (#233): In einer schmalen Spalte wird die einzelne Kachel zum Streifen, dort steht ein Bedienblock (siehe Regeln) | `UcBkKosten.pnlKacheln`, `UcWirtschaftlichkeit.KachelnBauen` |
| `Kennzahlkachel` | Überschrift, großer Wert, leise Herkunftszeile — **Anzeige, kein Knopf**; leerer Wert = „—" | `UcBkKosten.Kachel` |
| `Bildkarte` | Anklickbare Landkarte: ein Bild plus benannte SVG-Flächen darüber (Zeigen, Wählen, Übernehmen per Doppelklick) — **mit Tastatur**, jede Fläche ein Fokusziel | `Allgemein/GrafikTools/KlimazonenKarte.cs` (Regex über eine eingebettete SVG, iU9‑W10a.0e) |
| `Fortschritt` | Balken, Text und Abbrechen einer laufenden Rechnung. `Anteil = null` heißt **unbestimmt** (der Balken läuft) — ehrlicher als eine erfundene Prozentzahl; **ohne `Abbrechen`-Rückruf kein Knopf** (iU9‑W11a.7) | `Views/Stromspeicher/Form_SpeicherOptimierung.cs` (`bar_Fortschritt`, `lbl_Status`, `btn_Abbruch` — die einzige nebenläufige Rechnung des Bestands) |
| `Schema` | Das Hydraulikschema als **SVG**: vier Spalten, Knoten als Rundeck, Kanten als Bézier mit Pfeilspitze und Prioritätskreis, Kaskadenband und Legende. Die Anordnung kommt fertig aus dem Kern (`SchemaLayout`), die Farben aus `epos-ui.css`; jeder Kasten und jedes Bandglied ist ein Fokusziel | `Views/Simulation/SchemaAnsicht.cs` (789 Z. GDI+, iU9‑W10b.0c) |
| `ErzeugerKachel` | Eine Anlage der Simulationskonfiguration: Rang, Titel, Chips in sechs Stilen mit sechs Editorzielen, ▲▼✎+× und ein Aufklappbereich — **acht Ereignisse**. Seit **#216** dazu ein **`Parameterbereich`** (`RenderFragment`, `null` = keiner): der Platz für die Simulationsparameter DIESES Erzeugers, immer sichtbar und nicht erst aufgeklappt — eine Einstellung ist kein Gerätedatum. Der Baustein weiß nicht, was darin steht; ein Klick darin wählt die Karte nicht aus und klappt sie nicht um (`@onclick:stopPropagation`) | `Views/Simulation/ErzeugerKarte.cs` (781 Z., iU9‑W10b.0d) |
| `SpeicherKachel` | Ein Projekt-Pufferspeicher, zugeklappt eine Zeile: Badges, Flächenchips, Kurzbilanz; aufgeklappt die Detailzeilen und das Schwellenband (Inline-SVG) | `Views/Simulation/SpeicherKarte.cs` (551 Z. samt `SchwellenBand`, iU9‑W10b.0d) |
| `ProjektListe` | Die EINE Projektliste des Hauses: Suche ueber Name, Kunde **und die unsichtbare Beschreibung**, Sortierung per Spaltenklick mit Gleichstandsaufloesung ueber den Namen, Zaehlzeile als FORMATSTRING (im Vorlaeufer als Steuerelementtext getarnt), Zeilenmarkierung, Doppelklick, zwei Spaltensaetze (`Auswahl` fuer die Projektdialoge, `Einstieg` fuer die iOS-Seite) und `NurName`/`AutoVorauswahl` je Wirt. Eine gewoehnliche Tabelle mit der Hausklasse `epos-raster`, kein QuickGrid — die Spaltenzahl entsteht zur Laufzeit | **vier** Listen des Bestands: `ProjektAuswahl` (uc), `Form_ProjektSpeichernUnter.listView_Projekt`, `Form_ProjektDelete.comboBox_Projekte`, `Form_ProjektExportImport.cbProjekt` (iU9‑W15a.1, Befund W15a‑B52) |
| `Gespraechsverlauf` + `Gespraechszeile`/`Gespraechsrolle` | Die geordnete, rollenbehaftete **Nachrichtenliste** des KI-Assistenten: zehn Rollen (`role="log"`, `aria-live="polite"`, `aria-relevant="additions"`), `@key` je Zeile, `Beschaeftigt`-Zustand, `Fussbereich` als `RenderFragment` (dort steht der Bestätigungsblock — E‑3), Kopieren-Rückruf (den Text liefert die Komponente, die **Hülle** schreibt die Zwischenablage), Autoscroll **nur wenn der Anwender unten steht** (E‑12). **Kein Streaming** (E‑7 — der Bestand streamt nicht), **kein Markdown** (E‑7b — ein Wandler wäre eine neue Abhängigkeit UND eine Angriffsfläche für Modelltext), **keine Link-Erkennung** (nur eine Zeile mit `Adresse` ist ein Verweis — die Komponente rät nie), **nichts in `localStorage`** (der Verlauf ist personenbezogen). Er ist der EINZIGE Ort mit JavaScript in dieser Bibliothek: `wwwroot/epos-verlauf.js`, zwei Funktionen, über `import()` geladen — keine Wirtsseite braucht eine `<script>`-Zeile | `Form_KiChat._verlaufAnzeige` — eine `RichTextBox` mit GENAU EINER Ausgabemethode `SchreibeZeile(text, farbe, fett)`; ihre acht Farben und zwei Schriftschnitte sind die zehn Rollen (iU9‑W15b.6) |
| `KiKnopf` | Der Einstieg in den KI-Assistenten aus einer Maske: `Kurztext`, `Sichtbar` (aus `KiEinwilligung.Abgeschaltet` — der Abschalter blendet ihn AUS, statt ihn zu sperren), `Aktiv`, `Gewaehlt`. **Seit Auftrag #199 zeichnete ihn der `InfoKnopf` selbst**, und zwar RECHTS neben sich; **seit Auftrag #218 (Anwenderentscheid 11.09.2026, „KI-Knopf: Variante C + Variante D") zeichnet `InfoKnopf` das rechte Feld seiner Pille SELBST** (er ist ihr alleiniger Wirt) — dieser Baustein bleibt für einen Wirt OHNE Info-Knopf und zeichnet dann denselben RING (28 px, Variante C: die EPOS-Plan-Marke in ihren drei Farben, `Aktiv` füllt ihn stattdessen mit dem Blau der Marke und einem weißen Blitz). Die frühere Textbeschriftung (`Beschriftung`, „KI") ist entfallen — keine Zeile trägt mehr Text, nur noch das Inline-SVG. Er ist nicht tabulierbar und zieht den Fokus nicht aus dem bearbeiteten Feld; **er öffnet nichts**, er meldet | `Allgemein/KI/KiAufrufKnopf.cs` (270 Z., mit W14a ohne Aufrufer; iU9‑W15b.5) |
| `Diagramm` + `Diagrammbereich` | **Der Rahmen, in dem JEDES Renderer-Bild steht** (Windows-Abnahme 05.09.2026, Befund A‑1: „Allgemein bei Charts: das Zoomen funktioniert nicht"). Zwei Zoomstufen: der **Bildzoom** — Mausrad um den Zeiger, Ziehen, Doppelklick, Kneifgeste, Tasten `+ − 0`, Anzeige „×2,5", Knopf „1:1" — läuft ganz im Browser (CSS-Transform, kein Neuzeichnen); der **Datenzoom** meldet ein aufgezogenes Rechteck als `Diagrammbereich` (Anteile 0…1 des BILDES, mehr lässt sich an einem PNG nicht messen), und der Wirt lässt den Kern das Bild mit diesem Achsenbereich neu zeichnen. Ohne `BereichGewaehlt` gibt es den Knopf „Bereich" nicht. Zweiter Ort mit JavaScript in dieser Bibliothek: `wwwroot/epos-diagramm.js`, über `import()` geladen — keine Wirtsseite braucht eine `<script>`-Zeile. Lädt das Modul nicht, steht das Bild starr da wie vorher. **`OhneZoom` ist die EINE Ausnahme (Auftrag #222): RING und KUCHEN.** `ChartBild Rund="true"` setzt den Schalter — keine Leiste, kein Modul, kein Greifzeiger. Ein Ring trägt eine Handvoll Segmente statt 8 760 Stützstellen, ein Achsenausschnitt ist dort undenkbar, und auf ×1,2 schnitt `.epos-diagramm-flaeche { overflow: hidden }` den Kreis an (Anwenderfoto „Heinestr 15"). Der RAHMEN bleibt: Die Regel „jedes Bild durch `ChartBild`" gilt unverändert | `System.Windows.Forms.DataVisualization.Charting.Chart` — Achsenzoom, Rollbalken und Zurücksetzen der siebzehn Zeichenflächen; mit iU9‑W11b (A‑7, Risiko R‑W11‑5) entfallen und hier zurückgeholt |
| `Baumansicht` + `Baumknoten` | Ein vierstufiger Baum: `role="tree"/"treeitem"/"group"`, `aria-level`/`setsize`/`posinset`, `aria-expanded` NUR an Knoten mit Kindern, roving `tabindex` über die **abgeflachte Sichtliste** (↓↑ → ← Pos1 Ende Enter/Leertaste, kein Typeahead), Einrückung per CSS, `forced-colors`. Das Dreieck ist ein eigenes 44‑px‑Klickziel und **wählt nicht**; das Kennzeichen (»[Auslieferung]«) steht als eigenes `<span>` neben dem Text, nicht darin. Der Aufklappzustand kommt aus den DATEN (`VonVornOffen`) und überlebt einen Neuaufbau, solange die Schlüssel gleich bleiben. **Kein Kontextmenü, keine Mehrfachauswahl** — die kleinste tragfähige Fassung für den einen Nutzer (R‑W14c‑8) | `Views/Admin/Form_KatalogDubletten._tree` — der **einzige** `TreeView` des Bestands (iU9‑W14c.4); die Daten kommen als `DublettenBaum` aus dem Kern |
| `Zweispaltenauswahl` | Die EINE Projekt/Datenbank-Auswahl des Hauses (Anwenderentscheid **#76**, 05.09.2026): `Links` (Projektliste), `Mitte` (die zwei Richtungsknöpfe als Parameter — Text, Kurztext, Sperrzustand, Rückruf), `Rechts` (Katalog samt Filtern und Katalogknöpfen), dazu `NurRechts` für eine Verwaltungsbetriebsart ohne Projekt. **Seit dem Anwenderentscheid W14a‑E‑10‑Q2 vom 07.09.2026 stehen sie UNTEREINANDER** — Projektliste oben, höhenbegrenzt auf `--epos-projektlistenhoehe` (12 rem = vier Zeilen; ohne die Grenze schöbe eine lange Projektliste den Katalog beliebig weit nach unten), darunter die **Übernahmeleiste** mit den zwei Knöpfen als Textzeile, darunter die Katalogliste über die **ganze Breite**. Die Medienabfrage bei 900 px, die schmale Mittelspalte und das waagerechte Pfeilpaar ◀▶ sind damit gefallen: Es gibt nur noch EINE Anordnung, und jeder Knopf trägt **ein** Zeichen (▲/▼). Die Parameternamen bleiben — sie benennen die Rolle, nicht den Platz. **Elf** Dialoge nutzen ihn, **sechs** mit Katalog und **fünf** ohne; die Liste steht in `ZweispaltenauswahlTests` | die elf handgeschriebenen Fassungen von `epos-auswahlpaar`/`epos-auswahlspalten` aus den Wellen 6, 7, 9 und 12; im Bestand `Form_Gebaeude` (252/63/436 px) und `Form_Heizkessel` (316/88/313 px) |
| `Spaltenfilter` | Das POPOVER eines Spaltenfilters (Anwenderentscheid **W14a‑E‑10**, 07.09.2026): Spaltenname, **EIN** Eingabefeld, der Knopf „Filter löschen" — und bei einer Zahlenspalte der Formenhinweis darunter. Nichts sonst; kein „Übernehmen", keine Werteliste. Ein Feld auch für Zahlen ist Frage **Q1 = ja**: Was es versteht (`>10`, `>=10`, `<60`, `<=60`, `=15`, `10..60`, die bloße Zahl), steht als `Zahlenausdruck` im Kern und ist dort ohne Oberfläche geprüft. **`@oninput` übernimmt NICHT** — das ist die Entscheidung zum offenen Punkt **O‑6**: Der `@key`-Fix W6‑B‑2 baut das Raster neu auf, sobald die Zeilenzahl wechselt, und beim Filtern wechselt sie praktisch immer; gemessen in `SpaltenfilterTests.O6_*` ist ein QuickGrid-eigenes `ColumnOptions`-Popover danach **weg**. Deshalb wirkt das Feld bei **Enter, Feldwechsel oder Verlassen** und schließt dabei; Esc schließt ohne zu übernehmen. „Filter löschen" hält `mousedown` an, sonst nähme `onfocusout` den Knopf unter dem Klick weg | keines — der Bestand hatte Klapplisten und Bereichsfeldpaare |
| `Katalogliste` | **Die EINE Katalogliste des Hauses** (W14a‑E‑10): Zone A — EIN Suchfeld über alle Spalten links, die Trefferzahl rechts („15 von 20.749 Sätzen"), „Filter zurücksetzen" **nur, solange ein Spaltenfilter gesetzt ist**, „Kein Treffer." statt einer leeren Liste — und darunter das `Raster` mit den Spalten des `Katalogfilterprofil`: Sortierzyklus **auf → ab → aus** (immer höchstens eine Spalte), Trichter **gefüllt gegen Umriss** im MARKUP (`fill="none"` / `fill="currentColor"` — er trägt auch in Graustufen), Tönung der gefilterten Spalte über `ColumnBase.Class`. **Keine Filterzeile, keine Chips** — der Anwender hat sie abgewählt („nicht separat, außer ‚Suche' über alle Felder"). Die Markierung hängt am **Bezeichner**, nicht an der Zeilennummer: Deshalb bleibt die gewählte Zeile gewählt, auch wenn der Filter sie ausblendet (W4). `Virtualisiert` schaltet sie selbst an der GEFILTERTEN Zeilenzahl (≥ 120). **Seit Stufe S2 (07.09.2026) tragen auch die sieben PROJEKTDIALOGE sie** — Heizkessel, BHKW, Pufferspeicher, PV, Stromspeicher, Solarkollektoren und die Wärmepumpe: dasselbe Profil, derselbe Filter, dieselbe Suchzeile, keine zweite Fassung. Zwei Dinge kommen dort dazu: die Spalte **„im Projekt verwendet"** (`Katalogfilterprofil.MitVerwendung`, sortierbar ohne Trichter — bei jedem Zeichnen aus der LEBENDEN Projektliste gestempelt, weil die Dialoge erst beim OK zurückschreiben) und der geteilte Filterstand aus dem `Katalogfilterregister`. Der **Wärmepumpenkatalog** ist dabei der Kern des Entscheids: Seine elf Bedienelemente — sieben Klapplisten und vier Zahlenfelder — sind neun Spalten geworden; Bauart (45 von 51 Sätzen leer), Auslegung (dieselbe Aussage wie „Kühlen"), Regelung und Aufstellung bleiben ausdrücklich im Kenndatenblock. Damit sind es **fünfzehn** Dialoge — acht in der Verwaltung, sieben im Projekt — in **zehn** Komponenten; die Fälle stehen in `KataloglisteTests`. **Mit Stufe S3 (07.09.2026) kommen acht weitere Wirte dazu** und zwei Betriebsarten. Die Wirte: die drei BEDARFSkataloge (`BedarfAdminDialog`, `BedarfsProfileDialog`) und die drei ZEITREIHENkataloge (`WaermebedarfAdminDialog`/`WaermebedarfExternDialog`, `StromganglinieAdminDialog`/`StromganglinieDialog`, `SolarganglinieAdminDialog`/`SolarganglinieDialog`) — ihre Profile stehen als `Katalogfilterprofil.FuerBedarf`/`FuerZeitreihe` im Kern, und die Zeitreihen holen Jahresarbeit und Spitze aus **einer** `GROUP BY`-Abfrage je Katalog — dazu die zwei IMPORTMASKEN `KatalogImportDialog` (fünf Ausprägungen) und `ModulImportDialog` (zwei). Die zwei Betriebsarten: **der VERGLEICH** (S3.3, Frage Q3) — `Strg`- oder `Umschalt`-Klick auf den vorhandenen Wahlknopf MARKIERT bis zu drei Zeilen und lässt die Wahl in Ruhe (`Zeilenwahl.Tastenwahl` läuft **vor** `Gewaehltwerden`), zwei bis drei Markierungen machen den Knopf „Vergleichen" frei, und die `Ueberlagerung` zeigt EINE Zeile je Parameter und EINE Spalte je Gerät, abweichende gekennzeichnet **mit Worten**, nicht nur mit Farbe; die Zeilen kommen über `Vergleichsparameter` aus `ParameterUebersichtCtrl.Werte` (W14a‑E‑8) und fallen ohne den Delegaten auf die Profilspalten zurück. Die **VIERTE** Markierung wird abgewiesen, statt die älteste wegzunehmen — wer drei Geräte nebeneinandergelegt hat, hat sie ausgesucht. Und **`Mehrfach`** (S3.4) — die Wahlspalte trägt Kontrollkästchen, `IstGewaehlt` fragt den Wirt, `Mehrfachwahl` meldet ihm Zeile, ANZEIGEindex und Zusatztasten, `Doppelklick` den schnellen Weg; die REGEL (W6‑E‑5) bleibt beim Wirt, weil sie an seiner `Zeilenmarkierung` und an den Quellindizes hängt, über die die Wahl einen Filterwechsel überlebt. In dieser Betriebsart gibt es den Vergleich nicht: `Strg` und `Umschalt` bedeuten dort die Mehrfachwahl, und zwei Bedeutungen auf einer Taste wären keine. **Damit sind es 21 Dialoge in 16 Komponenten**, und die Filtermechanik gibt es im Haus genau einmal; die Fälle stehen in `KataloglisteTests`, `KatalogVergleichTests` und `KatalogfilterS3Tests`. **Seit Befund #212 (11.09.2026) rechnet sie nur noch bei einer ÄNDERUNG** — siehe die Regel unter der Tabelle | die zwei Filterklapplisten der vier Erzeugerbrowser, die Herstellerklappliste des Wechselrichters — und in zehn von vierzehn Katalogverwaltungen: nichts |
| `Katalograhmen` | Der Rahmen eines KATALOGDIALOGS (Anwenderwunsch 05.09.2026, „Admin-Menüs sind nicht an Größe Bildschirm angepasst"): `Liste` und `Eingabe`. **Seit dem Anwenderentscheid W14a‑E‑10 vom 07.09.2026 stehen sie UNTEREINANDER** — Liste über die ganze Breite, Eingabe darunter; der Anwender hat es begründet: „Liste wie zuvor über ganze Breite, sonst zu schmale Liste." Im Mockup gemessen: neben dem Eingabeblock hatten **fünf** Parameterspalten Platz, über die ganze Breite sind es **sechs bis neun**, und die neunspaltige Liste rollt dabei um **0 px** statt um 118 px in sich. Damit **kehrt die Höchsthöhe aus W9‑B‑2 zurück** — mit einem größeren Maß: **1,3 × `--epos-listenhoehe`** = 458 px = elf Zeilen, und die Liste rollt in sich; ohne diese Grenze schöbe eine lange Liste den Eingabeblock beliebig weit nach unten. Der Eingabeblock rollt **nicht** mehr selbst; was über die Fensterhöhe hinausgeht, nimmt der Rollbalken der Maske (offener Punkt **O‑7** des Katalogfilterkonzepts: gemessen 1 152 bis 1 228 px Dialoghöhe bei 1 366 px Breite — der Preis der Anordnung). Die **Medienabfrage bei 900 px entfällt** ersatzlos: Es gibt nur noch EINE Anordnung, und untereinander war schon der schmale Fall; `Gestapelt` bleibt als benannter Parameter der zwei Masken stehen, die ihn setzen, und ändert nichts mehr. **Sieben** Dialoge nutzen ihn; die Liste steht in `KatalogdialogTests`, die Regeln in `KatalograhmenTests` | die sechs Verwaltungsmasken des Bestands, die **alle** Liste und Eingabe nebeneinander stellten: `Form_Heizkessel_Admin` (726 × 383), `Form_BHKWAdmin` (856 × 517), `Form_SolarKollektorenAdmin` (825 × 494), `Form_PufferSp_Admin` (721 × 330), `Form_AdminPV` (607 × 489), `Form_AdminStromspeicher` (614 × 367) |
| `Formularraster` | Die EINE Anordnung eines PARAMETERBLOCKS (Anwenderwunsch **iU8‑E‑2 / W14a‑E‑7**, 05.09.2026: „Verbessere die Darstellung der Dialoge, insbesondere der Parameter auf der rechten Seite: kompakter, übersichtlicher"). Ein CSS-Raster und sonst nichts: Es stellt die Beschriftung **neben** das Feld (`--epos-beschriftung-breite`, 12 rem), ordnet die Felder über `auto-fill`/`minmax` in **eine oder zwei Spalten** — gemessen an der Breite des RASTERS, nicht des Fensters, weshalb die rechte Spalte eines `Katalograhmen` genauso richtig liegt wie ein freistehender Dialog —, und macht Zahlenfelder KURZ, sodass die Einheit unmittelbar hinter dem Wert steht. `Einspaltig` ist der benannte Rückweg für Blöcke, die eine Reihenfolge tragen oder neben einem Diagramm nur wenig Breite haben. Unter 900 px (`--epos-zweispalten-umbruch`) fällt die Beschriftung wieder über das Feld. Seit den Paketen P0–P3 und dem Nachzug iU8‑O‑1 (06.09.2026) nutzen ihn alle Dialoge mit Parameterblock (über 30 Dateien); die Liste steht im Protokoll und in `FormularrasterTests` | die sechs Verwaltungsmasken, die Beschriftung und Feld **nebeneinander** stellten — gemessen an `Form_AdminPV` (607 × 489): Beschriftungsspalte 178 px, Zahlenfeld 62 px, Einheit 4 px dahinter, Textfeld 250 px, Beschreibung 250 × 48 |
| `GanglinienGrafik` | Die GRAFIK einer gewählten Ganglinie (W12‑E‑2): Namenszeile, die drei Kennzahlen (Jahresarbeit, Spitze, Vollbenutzungsstunden), der Schalter „sortiert", die Einheitenwahl MWh/kWh (W8‑O‑5) und das Bild im Baustein `Diagramm` mit Bild- und Datenzoom. **Sie rechnet nichts und zeichnet nichts** — die Kennzahlen kommen fertig herein (`GanglinienAuswertungCtrl` mit der Ausprägung `GanglinienQuelle`), das Bild holt ein Delegat aus dem Kern (`ChartRenderer.GanglinieNormiert`, Regel R‑W8‑2). Sie stand bis zum Anwenderwunsch **W9‑E‑3** (05.09.2026) in `Dialoge/Strom` und trägt seither **keinen Fachbezug** mehr: Was auf der Achse steht, sagt der Wirt, die Farbe entscheidet sein Bildauftrag. **Drei** Dialoge nutzen sie | keines — der Bestand zeigte weder Strom- noch Wärmeganglinie als Kurve; übernommen ist der Bedarfsreiter der Ergebnisseite (`BildBedarfStrom`/`BildBedarfWaerme`) |
| `GanglinienImportLauf` | Die OBERFLÄCHENSEITE der AP5‑Importkette (W12‑E‑1): die drei Entscheidungen — Optionen, Protokoll, Konflikte — als `Ueberlagerung`en im selben Fenster, jede mit ihrer `TaskCompletionSource`, dazu `Starten(pfad, raster)`, `EtwasOffen` für die Esc‑Staffelung und `StufeZu(ergebnis)` für die Bannerstufe. Er **zeichnet nichts**, solange nichts läuft, und hat keinen Knopf — den setzt der Wirt. Seit **W9‑E‑3** hängen ihn **vier** Masken ein (die zwei Strom‑ und die zwei Wärmebedarfsmasken): EIN Import, vier Wirte | `Form_Stromganglinie_Admin.btn_Einlesen_Click` (:93‑261) und `Form_PeakShaving.Datei_Click` (:322‑396) — dieselbe Kette, zweimal geschrieben |
| `Formulargruppe` | Die leise Zwischenüberschrift IM Formularraster („Stammdaten", „Kenndaten", „Elektrik") — kein Kasten, kein zweiter `Gruppenkopf`; ein Balken im Balken wäre eine Hierarchie, die es fachlich nicht gibt. Ihr Wirt trägt **`display: contents`**, damit die Felder DIREKTE Rasterkinder bleiben: Ein zwischengeschobener Kasten setzte alle Felder einer Gruppe in EINE Rasterzelle und ließe die Beschriftungsspalten zweier Gruppen auseinanderlaufen. Ohne Titel zeichnet sie nichts Eigenes — die erste Gruppe eines Blocks steht üblicherweise so | `Form_Tarifstruktur.Gruppe`, `Form_WirtschaftlichkeitParameter.Gruppe` — der fette Label, mit dem die programmatisch gebauten Masken ihre Blöcke gliederten (`.epos-untergruppe`) |

## Bilder (`wwwroot/bilder/`)

Statische Web-Assets der Bibliothek; sie kommen beim Anwender über
`_content/EPOS.UI/bilder/…` an — auf beiden Plattformen, ohne dass eine Hülle etwas beisteuert.
Drei Bestände:

| Ordner | Inhalt |
|---|---|
| `bilder/` | `Zonenkarte_Klimazonen.png` — die Klimazonenkarte der `Bildkarte` (seit iU9‑W10a.3, vorher eine eingebettete Ressource der Anwendung) |
| `bilder/menue/` | die **elf** Menübildchen des `Menueband` (32 × 32), Pfad `_content/EPOS.UI/bilder/menue/<Bild>.png` — den baut `Menueband` selbst aus `Menuepunkt.Bild` |
| `bilder/start/` | die **21 Sinnbilder** der Startkacheln und der Globus des Klimakastens (seit dem Anwenderwunsch 05.09.2026, **W16b‑E‑3**), Pfad `_content/EPOS.UI/bilder/start/<Datei>` aus `Kachelbilder.Quelle` |

**Regel für ein Kachelbild.** Es ist DIESELBE Datei wie im Vorläufer — unverändert per `git mv`
aus `WindowsFormsApplication1/Resources/` hierher und dort aus `Properties/Resources.resx` und
`Resources.Designer.cs` ausgetragen; **JPG bleibt JPG, PNG bleibt PNG**, es wird nichts
umkodiert und nichts zugeschnitten. Zugeschnitten wird im **Stilblatt**: Die fünfzehn
Kachel-JPG sind die GANZE Kachel des Vorläufers (eine weiße Karte von rund 554 × 260 mit dem
Sinnbild oben links, über die `Form_Start` seine Beschriftungen legte), und drei Klassen zeigen
davon nur das Sinnbild — `epos-kachel-bild--ausschnitt` (Fenster 84 × 84 ab (40,40)),
`…--ausschnitt-flach` (ab (36,18), für die zwei halbhohen Kacheln 554 × 117) und
`…--symbol` (die fünf fertig zugeschnittenen `*_Symbol.png` der Aktionskarten). Die Zahlen sind
an allen Bildern **gemessen**, nicht geschätzt. Ein Bild ist DEKORATION: `alt=""`, der Text
steht daneben.

## Standards (`Standards/`)

`Zahlenfeld`, `Ganzzahlfeld`, `Textfeld`, `Auswahlfeld`, `Datumsfeld`, `Schalter`,
`Dateiwahl`, `Raster<TZeile>` (um `QuickGrid`), `ChartBild` (PNG aus dem Kern-Renderer als
`data:`-URL).

**Hausregel seit der Windows-Abnahme 05.09.2026: Jedes Renderer-Bild steht im Baustein
`Diagramm`.** `ChartBild` ist die eine Stelle, durch die alle 38 Bilder des
`ChartRenderer` gehen, und es setzt sein `<img>` seither in den Zoomrahmen. Wer irgendwo
ein `<img>` mit einer `data:image`-URL an `ChartBild` vorbei schreibt, baut ein Diagramm
ohne Zoom — genau den Zustand, den der Anwender beanstandet hat. Der Wächter dazu ist der
bunit-Fall `ChartBildTests.Jedes_Bild_steht_im_Baustein_Diagramm`.

**Die eine AUSNAHME davon seit Auftrag #222: RING und KUCHEN tragen keine Zoomleiste.**
`ChartBild Rund="true"` reicht `OhneZoom` an den Rahmen durch; der zeichnet dann weder die
Leiste „×1 · 1:1" noch bindet er `epos-diagramm.js`. Begründung des Anwenderentscheids vom
11.09.2026 (Punkt c): Ein Ring hat nichts zu zoomen — fünf Segmente statt 8 760 Stützstellen,
und einen Achsenbereich gibt es an einem Kreis nicht. Praktisch war die Leiste sogar schädlich:
Auf ×1,2 schnitt `.epos-diagramm-flaeche { overflow: hidden }` den Kreis an. **Die Hausregel
selbst bleibt** — jedes Renderer-Bild geht weiter durch `ChartBild` und steht im Rahmen
`Diagramm`; nur die BEDIENUNG fällt an der runden Gestalt weg. Der Wächter dafür ist
`UebersichtReiterTests.Die_Ringe_tragen_keine_Zoomleiste` (er prüft zugleich, dass der Rahmen
steht).

`Raster` führt seit iU9‑W13.0l `Virtualisiert` und `Zeilenhoehe`: Ein `IQueryable` allein
virtualisiert **nichts** — QuickGrid zeichnet ohne `Virtualize` jede Zeile. Für die 20 746 Zeilen
der CEC-Modulliste setzt der Wirt den Schalter; die Hülle bekommt damit die Klasse
`epos-raster-huelle--hoch` (feste Höhe, stehender Spaltenkopf), ohne die es nichts zu rollen und
also nichts zu virtualisieren gäbe.

**Eine neue Zeilenmenge bekommt ein neues QuickGrid** (Befund **W6‑B‑2**, Windows 07.09.2026:
„der Filter bei der Herstellerauswahl funktioniert nicht"). Das Raster hängt seither ein
`@key` an `Rasterstand` = (`Virtualisiert`, Zeilenzahl); ändert sich einer der beiden Werte,
baut Blazor das Gitter neu auf. **Kein Wirt muss etwas dafür tun** — der Standard rechnet die
Kennung selbst. Grund: QuickGrid trägt den virtualisierten und den flachen Weg in EINER Instanz,
und der flache Zwischenspeicher (`_currentNonVirtualizedViewItems`) wird genau einmal gefüllt —
beim ersten Datenabruf, als das `@ref` auf das `Virtualize`-Kind noch `null` war, ohne
Pagination also mit der GANZEN Liste. Danach ist das `@ref` gesetzt und wird beim Entfernen des
Kindes nicht zurückgesetzt; jeder weitere Abruf läuft ins Leere. Fällt der Schalter deshalb
unter der Schwelle der Wirte (≥ 120 Zeilen) von `true` auf `false` — 2 343 Wechselrichter → 109
nach dem Herstellerfilter —, zeichnet QuickGrid den uralten Stand: die ungefilterte Liste. Der
Schlüssel hängt bewusst an der ZAHL und nicht an der Zeilenmenge: Beim Tippen in einer Zelle
(`Bearbeitbar`) ändert sie sich nicht, beim Filtern praktisch immer. Wächter: die drei Fälle in
`RasterTests` („Der_Wechsel_des_Virtualisierungsschalters…", „Eine_andere_Zeilenzahl…",
„Dieselbe_Zeilenzahl_behaelt_die_Rasterinstanz") und
`ModulImportDialogTests.Der_Herstellerfilter_zeigt_nur_noch_die_Zeilen_des_Herstellers`.

**Eine virtualisierte Liste muss BILLIG zu zeichnen sein — und ihr Zeilenmaß muss stimmen**
(Befund **#212**, Anwender 11.09.2026: „die Liste blinkt", Stromspeicherimport mit 6 654
Sätzen). Der `@key`-Fix W6‑B‑2 und die stabile Datenquelle aus W13‑B‑6 reichen nicht: QuickGrid
holt seine virtualisierten Zeilen hinter `await Task.Delay(100)`
(`ProvideVirtualizedItemsAsync`, seine Entprellung) auf demselben Zeichenfaden und stellt die
Anforderung bei JEDEM Zeichenlauf neu. Solange geladen wird, zeichnet `Virtualize`
Platzhalterzeilen („…" in jeder Zelle) und die Tabelle trägt `loading`, was den Körper auf
`opacity: .25` abblendet — **das ist das Blinken**. Daraus folgen drei Regeln:

* **Kein Zeichenlauf rechnet, was sich nicht geändert hat.** `Katalogliste` filterte und
  **sortierte** bis #212 den ganzen Katalog je Zeichenlauf; gemessen an 6 654 Zeilen kostete das
  1,8 ms ohne Sortierung, **21 ms mit Sortierung** und **39 ms mit Sortierung und Suche** — je
  Lauf. Seither entscheidet ein **Abzug** über Zeilenliste (Referenz **und** Anzahl), Profil und
  Filterstand, ob gerechnet wird. Die ANZAHL ist der Unterschied zum ersten, gescheiterten
  Anlauf von W13‑B‑6: Mehrere Wirte ändern ihre Liste an Ort und Stelle (`RemoveAll` im
  Ganglinienverwalter), und das sieht ein reiner Referenzvergleich nicht.
* **Was je ZEILE gefragt wird, wird in O(1) beantwortet.** `IstGewaehlt` ruft die Liste für jede
  gezeichnete Zeile — und der Alle-Schalter (W13‑B‑5) für **jede sichtbare**. Ein Wirt, der
  darauf mit einem Durchlauf durch seine Anzeigeliste antwortet, zahlt n² : Mit 6 654 gewählten
  Sätzen kostete ein Zeichenlauf **89 ms** (Katalogimport) bzw. **108 ms** (Modulimport, dort
  `List.Contains`), nach dem Fix 3 ms bzw. 1,4 ms. Wörterbuch oder Menge, nicht Schleife — und
  die Liste selbst bestimmt den Schalterstand seither **einmal** je Zeichenlauf statt dreimal.
* **Das Zeilenmaß wird GESETZT, nicht gerechnet** (berichtigt durch **#235**, siehe unten).
  `Raster.Zeilenhoehe` ist das Maß, mit dem `Virtualize` rechnet: Es teilt die Höhe des
  Rollbehälters dadurch und setzt danach seine zwei Abstandshalter. #212 leitete daraus
  53 px ab (44 px Berührungsziel + 2 × 4 px Zellenpolsterung + 1 px Trennlinie) — **die
  Rechnung stimmte nie**: Die Polsterung kommt von QuickGrid
  (`.quickgrid[theme=default] > tbody > tr > td`, Spezifität 0‑2‑3 gegen 0‑1‑1 von
  `.epos-raster td`) und beträgt 1,6 px; die Zeile war 48,2 px hoch. Seit #235 geht dieselbe
  Zahl als `ItemSize` **und** als `--epos-rasterzeile` ins Stilblatt, das sie jeder Zeile gibt.

Zwei Pfähle dazu, damit niemand sie neu suchen muss: Der **Behälter** war in Ordnung
(`.epos-raster-huelle--hoch`, `max-height: 420px`, `overflow-y: auto` — der nächste
Rollcontainer über den Abstandshaltern), und **bunits `RenderCount` taugt nicht als Zähler für
Zeichenläufe** — er zählt die aktualisierten Komponenten im ganzen Unterbaum, nicht die Läufe
der einen Komponente; `Katalogliste` führt dafür `Zeichenlaeufe` und `Neurechnungen` als
Prüfhilfen. Wachen: die vier #212-Fälle in `KataloglisteTests`, die `[Theory]` über alle fünf
Ausprägungen in `KatalogImportDialogTests` und
`ModulImportDialogTests.Der_Wechselrichterimport_kommt_mit_6654_Geraeten_zur_Ruhe`.

**Ein virtualisiertes Raster wird im BROWSER gemessen, nicht in bunit** (Befund **#235**,
Anwender 12.09.2026: „die Auswahlliste flackert bei großen Datenlisten immer noch"). W13‑B‑6
und #212 haben je etwas Richtiges beseitigt und beide nur in bunit gemessen — das hat kein
Layout, kein JavaScript und keinen Kaskadenrechner, also weder die Pixelhöhe einer Zeile noch
die `IntersectionObserver`, an denen `Virtualize` hängt; die #212-Wache prüfte deshalb eine
falsche Rechnung und war grün. **Die Ursache:** `Virtualize` lässt je einen
Sichtbarkeitsmelder auf seinen zwei Abstandshaltern laufen; weicht `ItemSize` von der
wirklichen Zeilenhöhe ab, kommen die beiden auf verschiedene Anfangszeilen und schieben das
Fenster endlos gegeneinander — gemessen **alle 33 ms**, 370 Meldungen in drei Sekunden. Jeder
Sprung stellt QuickGrids Datenanforderung neu an, und die fällt hinter dessen
100‑ms‑Entprellung: Keine wird je fertig, die Liste bleibt in ihren Platzhalterzeilen stehen
— und die sind mit **21,9 px** (kein Bedienelement darin, nur QuickGrids `:after`-Zeichen)
so viel kürzer als die echten **48,2 px**, dass sie den Streit selbst am Leben halten. Nicht
beteiligt und ausdrücklich ausgeschlossen: der Rollbehälter (richtig gefunden), das klebende
`thead`, der `@key` über die Zeilenzahl, Fortschrittsmeldungen des Wirts, und die Klasse
`loading` — sie schaltet im virtualisierten Zweig **nie** (0 Umschaltungen in neun Fällen),
das „Blinken" sind die Platzhalter selbst. **Die Regel:** Wer virtualisiert, gibt EIN Maß an,
und dieses Maß gilt im Baum — `Raster` legt es als `--epos-rasterzeile` an die Hülle, und
`epos-ui.css` gibt es **beiden** Zeilenarten (`… > tbody > tr` und `… > td`), Platzhalter
eingeschlossen. Es muss ÜBER der natürlichen Zeilenhöhe liegen, denn `height` ist an einer
Tabellenzeile ein Mindestmaß. **Die Probe dazu ist dauerhaft:**
`Proben/Rasterprobe` (minimaler Blazor-Server-Wirt + Playwright, neun Fälle samt Gegenprobe,
in keiner Projektmappe und in keiner CI) — sie gehört vor jede Änderung an
`Raster`/`Katalogliste`/`.epos-raster*` gezogen.

`Zahlenfeld`, `Ganzzahlfeld`, `Auswahlfeld` und `Schalter` führen `Aktiv` (Vorgabe `true`):
Ein gesperrtes Feld bleibt **sichtbar und lesbar**. Der Tarifdialog sperrt damit den Block des
nicht gewählten Rechenmodells, statt ihn auszublenden — die Werte des anderen Modells gehen so
nicht verloren (iU9‑W2.3, Vorbild `Form_Tarifstruktur.ModusUebernehmen`).

**Die Liste am `Auswahlfeld` gehört dem Wirt — und muss dublettenfrei sein und den
gespeicherten Wert enthalten.** Die Komponente zeigt `Eintraege` eins zu eins und meldet die
**Id** zurück, nicht die Position; sie ordnet nichts, faltet nichts zusammen und rät keinen
Rückfall. Wer die Liste baut, schuldet deshalb dreierlei: **je Wert genau einen Eintrag**,
**stabile Ids über einen Neuaufbau** (sonst zeigt die gemerkte Id danach auf eine andere Zeile)
und **den gespeicherten Wert als Eintrag** — steht er nicht drin, hat `Auswahl` keine passende
Option, und ein `<select>` ohne Treffer zeigt **nichts** an. Beleg ist der Befund W4‑B‑1: Die
Klappliste „Preisbasis" führte „Nm³" doppelt, weil zwei Umrechnungsregeln dieselbe Zieleinheit
tragen, und blieb bei Trägern ohne Regel leer (`iU9_W4_Blazor_Port_Protokoll.md` § 9a). Der
Zuschnitt gehört in den **Kern**, nicht in die Hülle: Dort erreicht ihn das Gate
(`WP-Plan.Kern.slnf`), und die zweite Schale bekommt ihn geschenkt.

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

**`Zahlenfeld` zeigt höchstens VIER Nachkommastellen** (Auftrag #224, Konzept 7.8 Punkt 2):
Ohne gesetzte `Nachkommastellen` formatierte das Feld mit `"0.##########"` — eine 0,1, die als
Summe von Gleitkommazahlen entstanden ist, stand dann als `0,1000000000000001` im Feld und sah
aus wie ein Datenfehler. Die Schranke ist eine Konstante in `Standards/Zahlen.cs`
(`HOECHSTE_NACHKOMMASTELLEN = 4`), gilt hausweit und für JEDES Feld ohne eigene Vorgabe; wer mehr
Stellen braucht, setzt `Nachkommastellen` ausdrücklich. **Gerundet wird nur die ANZEIGE** — der
gebundene Wert bleibt unverändert, bis der Anwender selbst etwas eintippt.

`Dateiwahl` (Pfadfeld + Knopf „Durchsuchen…") **öffnet nichts**: Der Wähler kommt als
`Func<string, Task<string?>>` herein — unter Windows aus `Dienste.Datei.DateiOeffnen`, auf iOS
aus der Dokumentenauswahl. Ohne Delegat bleibt der Knopf weg.

`Dateiwahl` fuehrt seit iU9‑W15a.5 zusaetzlich `Speichern`/`Namensvorschlag`/`Zielwaehlen`: Der Bestand kannte nur „Datei oeffnen“, der Projektexport braucht „Datei speichern unter“ MIT einem Namensvorschlag (`<Projekt>.wpx`). Der Unterschied ist nicht kosmetisch — ein Speichern-Dialog laesst einen Namen zu, den es noch nicht gibt. Auch hier gilt: kein Delegat, kein Knopf.

**Der Wähler DARF warten, und die Komponente muss das aushalten** (Befund **W13‑B‑1**,
Windows-Abnahme 05.09.2026). `Waehlen` ist ein `Task` und kein Wert — bis dahin gaben alle
Hüllen ihn als `Task.FromResult(Dienste.Datei.DateiOeffnen(…))` herein, und damit lief der
`OpenFileDialog` synchron IM Blazor-Ereignis, mitten im `WebMessageReceived`-Rückruf der
WebView2. Das ist der Absturz, den der Anwender am VDI-3805-Import gemeldet hat. Die
Hüllen rufen seither `Dienste.Datei.DateiOeffnenAsync`, und das Fenster geht eine
geposteten Nachricht später auf. Für die Komponenten ändert sich nichts — sie `await`eten
ihren Delegaten von jeher —, aber die Regel gilt jetzt ausdrücklich: **Ein Delegat, der
Oberfläche der Plattform öffnet, wird `await`et und nie synchron ausgewertet.** Zeugen:
`KatalogImportDialogTests.Der_Dateiwaehler_darf_warten_und_die_Liste_kommt_danach` und
`…Ein_abgebrochener_Waehler_liest_nichts` — beide lösen den Wähler von Hand auf.

**Jede Wurzelkomponente steht in der `Fehlerschranke`** (derselbe Befund, zweiter Teil).
Eine Ausnahme aus einem Ereignis oder aus dem Lebenszyklus ging bis dahin ungefangen an
den Renderer, und unter Windows beendete sie den PROZESS: Der WinForms-`BlazorWebView`
(10.0.100) führt kein `UnhandledException`-Ereignis. `Bausteine/Fehlerschranke.razor` (auf
`ErrorBoundaryBase`, ohne DI) zeigt statt der Maske einen markierbaren Kasten mit Typ,
Wortlaut und innerster Ausnahme; `Bausteine/Wurzel<T>` ist das nötige Zwischenglied, weil
eine `ErrorBoundary` nur ihre NACHFAHREN fängt und eine Wurzel nichts über sich hat. Alle
drei Hüllen — `BlazorDialogForm`, `BlazorSeite`, `EPOS.iOS/HauptSeite` — mounten
`Wurzel<T>` statt `T`. **`Wurzel` zeichnet nichts Eigenes**: ohne Wurf ist im DOM kein
Unterschied; ein Wirt, der Maße verschöbe, hätte sechzig Dialoge verschoben. Wer eine
vierte Hüllenform baut, mountet ebenfalls `Wurzel<T>`. Das Netz hat eine Lücke, die man
kennen muss: Eine Ausnahme, die NICHT durch den Renderer läuft — `async void`, ein
unbeobachteter `Task`, ein Wurf auf einem Arbeitsfaden ohne `await` —, geht daran vorbei.
Wache: `Bausteine/FehlerschrankeTests` (zehn Fälle samt Gegenprobe ohne Schranke).

## Dienste (`Dienste/`)

Drei Schnittstellen nach außen — mehr sieht diese Bibliothek von der Umgebung nicht.

| Schnittstelle | Wofür | Windows | iOS | ohne Umgebung |
|---|---|---|---|---|
| `IHilfeDienst` | Hilfetext und Wikiseite zu einem Schlüssel (`InfoKnopf`) | `WindowsHilfeDienst` am `HelpCatalog` | `IosHilfeDienst` (iU10-5) | `KeineHilfe` |
| `IProjektQuelle` | die Daten der **Seiten** (unter Windows als `KeineProjekte` eingetragen — die Ansichten bekommen ihren Parametersatz von der Hülle, aber ein `@inject`-Ziel muss stehen, Befund W16c‑B11): Projektliste, Energieträgerliste, BHKW-Parametersatz, Übernahme des Anlegeergebnisses, seit iU9‑W10b der Parametersatz der Simulationskonfiguration seit iU9‑W16b (K6) `Startkacheln(int)` — die 21 Kacheln der Startseite — und seit iU9‑W16c.2 (K7) `StartseiteGaben(int)`/`BerichteKostenGaben(int)` (**alle mit Standardumsetzung**, damit eine vorhandene Quelle durch die Erweiterung nicht bricht) | die Startseite bekommt ihre Daten von `StartseiteHuelle` und geht als Parameter durch `Hauptfenster`, nicht über diese Schnittstelle | `IosProjektQuelle` (iU10-7) | `KeineProjekte` |
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

`KiAssistentWeg` (Auftrag **#199**) ist keine Schnittstelle, sondern der **eine Weg aus einer
Maske in den Hilfe-Assistenten**: `Moeglich` fragt die Kernauskunft `KiVerfuegbarkeit`,
`AusDialog(schluessel, dialogname)` trägt Weg 1 (KI-Knopf im Dialogkopf) und
`AusMeldung(kennung, frage, …)` Weg 2 (erklaeren lassen). Beide melden den Aufruf ZUERST im
Kern (`KiChatKontext.AufrufMelden`) und öffnen DANN
`Dienste.Navigation.OeffneMaske(Masken.KiAssistent, kontext)` — stünden die zwei Schritte in
`InfoKnopf` und noch einmal in `Warnbanner`, liefe die Reihenfolge beim ersten Umbau
auseinander. Welche Oberfläche daraus wird, entscheidet die Plattform: Windows die
nicht-modale `KiChatHuelle`, iOS ein Ansichtswechsel der `AppWurzel` mit Rückweg.

## Seiten (`Seiten/`)

Was eine **Hülle ohne eigene Fenster** braucht (Paket iU10, iOS). Ein Dialog wird dort nicht in
einem zweiten Fenster geöffnet, sondern löst die Ansicht ab.

| Komponente | Zweck |
|---|---|
| `AppWurzel` | **die gemeinsame Wurzel beider Plattformen** (seit iU9‑W16c.2, Entscheid E‑1): eine Zustandsmaschine über `Seitenschluessel`, Registrierung als `INavigationsZiel`, Statuszeile nach einem Dialog. **Eine Wurzel, zwei Schalen** — `Kopfleiste` (`RenderFragment`) trägt unter Windows das `Menueband` samt Markenkopf und steht über JEDER Ansicht, auf iOS ist sie leer; `Startansicht` ist die Ansicht beim Aufmachen **und** das Ziel des Rückwegs (iOS `PROJEKTLISTE`, Windows `STARTSEITE`). Mit W16c kamen die Zweige `STARTSEITE` und `BERICHTE_KOSTEN` dazu (K7) — der zweite war seit W5 fertig und bloß nicht verdrahtet. Seit dem Anwenderentscheid **W16c‑E‑3** (04.09.2026) ist `BERICHTE_KOSTEN` der Weg **beider** Plattformen: „Varianten und Bericht…" wechselt die Ansicht, statt unter Windows den sechsten Reiter der Startseite nach vorn zu holen; das Reiterblatt bleibt, und die Ansicht trägt einen Rückwegknopf (`Geschlossen` → `ZurueckZurListe`), den das Reiterblatt nicht hat. **Seit iF30 (06.09.2026) trägt sie das LIZENZBANNER** (`.epos-lizenzbanner` über JEDER Ansicht): im Lesemodus dauerhaft und in Warnfarbe — der eine Zustand, den die Regel W16b‑E‑6 für ein Dauerbanner gelten lässt —, in den Warnstufen 30/14/7 Tage vor Ablauf als verfallender Hinweis. Sie kennt den Lizenzkern NICHT (Regel S‑2): Das Lagebild kommt fertig herein, unter Windows als `[Parameter] Lizenzlage` aus `HauptfensterHuelle`, auf iOS über `IProjektQuelle.Lizenzlage()`. **Noch kein Router**. Seit #62b (11.09.2026) trägt sie den PROJEKTASSISTENTEN als freie Ansicht — mit `AssistentGaben` als Delegat je Betriebsart (ein Lauf beginnt beim Betreten, er steht nicht) und `DarfVerlassen()` als dem EINEN Weg, auf dem ein Wirt fragt, ob die Ansicht verlassen werden darf (62b‑E‑1; unter Windows ruft ihn `Hauptfensterrahmen.FormClosing`). **Seit #207 (Anwenderentscheid SIM‑Q4) führt sie EINEN RÜCKWEGSTAPEL** statt dreier Felder: eine Liste aus `(Seitenschlüssel, Marke)`, höchstens drei Einträge (Startseite → Simulation → Auslegung → KI-Assistent), geleert beim Wechsel auf die Startansicht. `Zeige(ziel, marke)` legt die stehende Ansicht samt ihrer MARKE ab, wenn das Ziel eine Ansicht mit Rückkehr ist (Simulation, Stromspeicher-Auslegung, KI-Assistent); `Zurueck()` holt den obersten Eintrag, und lässt sich keiner mehr aufbauen, gilt wie bisher die Startansicht. `_auslegungRueckweg` (#192) und `_kiRueckweg` (#199) sind damit gefallen. **Die MARKE ist alles, was er wiederherstellt** — Schritt und Reiterblatt (`schritt=3;blatt=STROMSPEICHER`), nicht der innere Zustand einer Komponente (Regel aus #199); der Datenstand kommt aus der Hülle. **Seit Auftrag #220 trägt sie zusätzlich eine WIRTKENNUNG** (`wirt=START;schritt=3;blatt=STROMSPEICHER`): Schritt ③ steht seither an zwei Stellen — als Blatt der Ansicht und in der rechten Spalte des Startseiten-Reiters —, und wer aus dem REITER in die Stromspeicher-Auslegung ging, kommt dorthin zurück; `StehendeMarke()` fragt dafür auch die `Startseite`, und `Zeige(STARTSEITE, marke)` reicht sie ihr als `Marke` herein. **Ein Router ist er nicht und soll es nicht werden.** Dazu die vierte freie Ansicht `SIMULATION` mit `SimulationGaben` als Delegat je Betreten. **Seit Auftrag #221 (KI‑D‑E‑1) führt sie den `AktiverHilfekontext`** — Hilfeschlüssel, Name, Schritt und Reiterblatt der aktiven Ansicht (`EPOS.UI/Dienste/Hilfekontext.cs`). Sie ERMITTELT ihn nicht: Jede Ansicht meldet ihn über den `Hilfekontextmelder`, den die Wurzel als `CascadingValue` (`IsFixed`) nach unten gibt, und die Senke vergleicht — eine Ansicht meldet nach jedem Zeichenlauf. Nach oben geht er über `HilfekontextGeaendert`; `Hauptfenster` bindet Schlüssel, Dialognamen und `Aktiv` seiner EINEN Pille daran. Dieselbe Meldung löst `KiChatKontext.AufrufGeaendert` aus — unter Windows steht der Chat in einem eigenen Fenster, und sein Schliessen löst in dieser WebView sonst nichts aus |
| `Hauptfenster` | **DAS HAUPTFENSTER** (iU9‑W16c.2, S2) — die Windows-Schale um `AppWurzel`: `Menueband` (**59 Punkte**, vier Köpfe; der 59. ist „Simulation…" aus SIM‑Q3/#207), Kopfband (PRODUKTNAME · GATTUNG · CLAIM · Version, dazu die **Fensterhilfe** `Hauptfenster.btn_Help`) und darunter die Ansicht. **`Springe(punkt)` ist der EINZIGE Handler**: erst der `Weg`-Delegat der Hülle, dann die Wurzel — die Reihenfolge des Bestands (was `MenueCtrl` bediente, öffnete ein Fenster; nur was dort nicht stand, wechselte die Ansicht). Ohne `Weg` — also auf iOS — entscheidet allein die Wurzel. Datenseite: `Views/Hauptformular/HauptfensterHuelle.cs`. **Drei Namen, drei Dinge** (Anwenderentscheid **E‑10**, 04.09.2026): `Hauptfenster` ist DIESE Seite, `HauptfensterHuelle` ihre Blazor-Hülle, und `Hauptfensterrahmen` (vorher `MDIMainForm`) ist das WinForms-Fenster mit `Application.Run`, dem `BlazorWebView`, F1 und dem Sprachwechsel. **Seit Auftrag #221 (KI‑D‑E‑1) ist seine Pille die EINE Pille des Bildschirms:** Sie setzt den `CascadingValue` `HilfePilleImKopfband` (die Ansichten lassen ihre daraufhin weg) und trägt den Schlüssel der aktiven Ansicht — `KopfbandSchluessel` ist der des gemeldeten `Hilfekontext`s, der Fensterschlüssel nur, wo keine Ansicht einen meldet |
| `Projektliste` | der Einstieg: Nr., Projekt, Klimaregion, Ausstattung im `Raster` und je Zeile zwei Knöpfe, die einen Maskenschlüssel melden |
| `Seitenschluessel` | **die EINE Schlüsseltabelle beider Plattformen** (seit iU9‑W16c.0, K7 / Entscheide E‑1 und E‑2): die sieben Ansichten der iOS-Wurzel, `STARTSEITE`, `BERICHTE_KOSTEN`/`VARIANTEN`, seit #192 `STROMSPEICHER_AUSLEGUNG`, seit #207 `SIMULATION` (mit `Masken.Simulation`-Zwilling; die zwei alten Simulationsschlüssel bleiben als Einstiegsmarken ①/③), die **25 Maskenschlüssel** des Kerns und die **19 Wege des Hauptfensters** (Projekt neu/öffnen/bearbeiten/zuletzt/löschen/Transfer/Variante, Klimadaten, Kostenverwaltung, Energieträger, Einstellungen, Gesetzeskatalog, Dubletten, Lizenzverwaltung, Lizenztext, Version, Dokumentation, Sprache de/en) — **57** Werte in `Alle` (die Zahl stand bis #192 auf dem Stand von W16c; gezählt wird die Liste, nicht die Erinnerung). **Eine Wahrheit, keine Abschrift:** Die übernommenen Konstanten sind Verweise auf `Masken`/`Ansichten` im Kern (`= WindowsFormsApplication1.Masken.X`), damit `INavigation.OeffneMaske` unverändert gültig bleibt. Nachweis: `EPOS.UI.Tests/Seiten/SeitenschluesselTests` |
| `Simulation/SimulationSeite` | die **ANSICHT „SIMULATION"** (Auftrag #207, Stufe S1 des Konzepts `Projekte/Konzept_Simulationsablauf_EPOS-Plan.md`, Anwenderentscheid **SIM‑Q1**) — die fünfte Fachseite an `AppWurzel`, Schlüssel `SIMULATION` mit `Masken.Simulation`-Zwilling. Eine **`Ablaufleiste`** mit DREI Stationen: ① Konfiguration · ② „2 Simulation starten ▶" · ③ Ergebnis; **② ist ein KNOPF, kein Blatt** (ein Lauf ist nichts, das man ansieht) und gesperrt, solange die Konfiguration ungespeichert oder die Vorprüfung rot ist — der Grund steht als `title` am Knopf UND als Banner darunter (die weiche Hälfte der Regel W16b‑E‑6). ③ ist gesperrt, bis einmal gerechnet wurde. ① und ③ betten die zwei bestehenden Seiten ein und **halten sie montiert**, sobald sie einmal standen: Ein `@if` entsorgte den gerechneten Lauf, den Bilderspeicher und das offene Reiterblatt; sichtbar ist immer genau eines (`epos-simansicht-blatt--aus`). Der **`Marke`**-Parameter (`schritt=1|3;blatt=<Reiterschlüssel>`, gelesen von `SimulationMarke`) stellt Schritt und Reiterblatt her — er wirkt bei jeder ÄNDERUNG, nicht nur beim Aufbau, weil derselbe Menüpunkt die stehende Ansicht noch einmal ruft. **Seit der Windows-Abnahme #216 (11.09.2026) ist der Kopf EINE rechtsbündige WERKZEUGLEISTE** (`.epos-simansicht-werkzeugleiste`, Punkt 2: „Bringe die Elemente aus dem Dialog auf die rechte Seite mit besserem Design"): Titel und Projektzeile links, rechts die Schrittgruppe als **kompakte** `Ablaufleiste` (`Kompakt="true"` — ohne die Zäsur vor dem Rechenknopf und ohne die untere Trennlinie, die drei Knöpfe in EINEM Rahmen), daneben **„Ergebnis speichern"** (`.epos-simansicht-speichern`, nur in ③ und nur bei `_ergebnisSeite.SpeichernMoeglich` frei), dann das EINE Paar `InfoKnopf` (Hilfeschlüssel je Schritt) und das EINE „← zurück" (`FLOTTE_SEITE_ZURUECK`) mit der Rückfrage aus 62b‑E‑1. Die Leiste bricht auf schmalem Schirm um, die Schrittgruppe bleibt als EIN Flexelement zusammen. **Die Marke `schritt=2`** (`SimulationMarke.SCHRITT_LAUF`) ist der Anwenderentscheid **SIM‑E‑1**: Sie zeigt kein Blatt, sondern tut, was der Rechenknopf tut — bei Sperre bleibt die Ansicht bei ① und nennt den Grund. **`Parameter`** reicht `SimulationParameterDienste` an Schritt ① durch (die fünf Laufparameter, #216). Sie löst DREI Wirte ab — die Konfig-Einbettung und die Ergebnis-`Ueberlagerung` der Startseite und die Konfig-Überlagerung IN der Ergebnisseite („Fenster im Fenster im Fenster"); die Wache `SimulationOhneUeberlagerungTests` hält das fest. **Seit Auftrag #221 (KI‑D‑E‑1) meldet sie sich beim HILFE-ASSISTENTEN an** — als sechste Maske des Dialogkatalogs (`KiMaskennamen.SIMULATION`) mit 18 Feldern aus `SimulationKiSicht`: Kaskade und nicht aufgenommene Anlagen aus Schritt ① (lesend), die fünf Laufparameter (lesbar UND setzbar, Schreibweg `SimulationParameterDienste`, Plausibilitätsgrenzen im `KiMaskenhaken`) und die sieben Kennzahlen des Laufs samt SoC-Band, offenem Reiterblatt und Laufhinweisen aus Schritt ③ (lesend). Ihre Pille zeichnet sie nur ohne Kopfband; ihren `Hilfekontext` (Schlüssel je Schritt, Ansicht · Schritt · Reiter) meldet sie über den `Hilfekontextmelder`. **Seit Auftrag #220 teilt sie ihren Schritt ② mit dem Startseiten-Reiter** (SIM‑E‑2): Sperrgrund, „darf starten?" und der Startweg stehen EINMAL in `Seiten/Simulation/SimulationLaufsteuerung.cs`, und die `SimulationLaufsperre` — EINE je Projekt in der Quelle, beigelegt in JEDEM Parametersatz — sorgt dafür, dass immer nur EIN Lauf rechnet („ein Lauf zur Zeit"); der zweite Wirt bekommt den benannten Grund `SIM_LAUF_ANDERSWO`. Datenseite: `SimulationAnsichtDienste` (zwei fertige Parametersätze, Sperrgrund, „gibt es einen Lauf?", seit #221 `Ergebnisstand` — der zuletzt geladene Stand der Ergebnishülle, KEIN zweiter Ladeweg —, seit #220 `Laufsperre` und der Leser `Aus(gaben)`) → `EPOS.UI.Daten/Simulation/SimulationAnsichtQuelle.cs` |
| `Simulation/SimulationKonfigSeite` | die **Simulationskonfiguration** (iU9‑W10b.1) — die erste FACHSEITE, die iOS über `AppWurzel` erreicht. Unter Windows steht dieselbe Komponente bis W16 in einer modalen Dialoghülle (Entscheid R‑W10b‑1), weil ihre beiden Aufrufer die modale Rückkehr brauchen. **Seit #207 ist sie Schritt ① der `SimulationSeite`**: Ihr „Beenden"-Knopf ist gefallen (die Fußzeile trägt nur noch „Konfiguration speichern"), und sie MELDET über `UngespeichertGeaendert`, ob die Kaskade ungespeichert ist — daran hängt die Sperre von Schritt ② und die Rückfrage beim Verlassen. Ungespeichert sind genau drei Wege (Kaskadenplatz, Aufnehmen, Entfernen); alles Übrige schreibt sofort. `Speichern()` ist öffentlich, weil die Rückfrage denselben Weg geht wie der Knopf. **Seit #216 trägt sie die FÜNF LAUFPARAMETER** des gefallenen Reiters „Parameter" (`[Parameter] SimulationParameterDienste`): die **Netzverluste** im Abschnitt „Wärmebedarf" über den zwei Spalten (`section.epos-simkonfig-bedarf`, mit Herleitungszeile „wirkt nur bei vorhandenem Wärmebedarf", Vorgabe 0 %) und die drei Erzeugerwerte im `Parameterbereich` **ihrer** Karte — BHKW-Betriebsart samt unterer Leistungsgrenze, Heizstab der Wärmepumpe, Betriebsbereitschaft des Heizkessels. **Nur an der ERSTEN Karte ihrer Art** (`ErsteIhrerArt`): Die Werte gelten projektweit, an zwei BHKW-Modulen stünde dieselbe Betriebsart zweimal — dieselbe Regel wie bei ▲▼ und ×. Jedes Feld schreibt SOFORT, über die Delegaten der **Ergebnis**hülle (`KonfigSchreiben`/`BetriebsartSchreiben`) — sie hält `_bhkwBetriebsart` und `_grenzleistungBhkw`, mit denen `SimulationLaufCtrl.Bestuecken` den Lauf bestückt; ein eigener Weg über die Konfigurationshülle schriebe dieselben Spalten und ließe diese Felder stehen. Datenseite: `Views/Simulation/SimulationKonfigHuelle.cs` |
| `Simulation/SimulationErgebnisSeite` | das **Simulationsergebnis** (iU9‑W11b.13) — die zweite Fachseite für `AppWurzel`, unter Windows ebenfalls bis W16 modal (Entscheid R‑W11‑1, 1 474 × 821). **Ein `Reiter` für NEUN Blätter**: Übersicht, Bedarf, Wärmepumpe, Heizkessel, Solarthermie, BHKW, Photovoltaik, Stromspeicher, Ergebnis — dazu vier Überlagerungen (Laufmeldungen, Bedarf, Wärmepumpe, Variantenvergleich). **Seit #207 ist sie Schritt ③ der `SimulationSeite`**: Die Fußknöpfe „Konfiguration …" und „Beenden" und die Konfigurations-`Ueberlagerung` sind gefallen (die Ablaufleiste und das eine „← zurück" ersetzen sie). **Mit der Windows-Abnahme #216 (11.09.2026) ist die Seite ganz ohne eigenen Rand:** Der Reiter „Parameter" ist gefallen (Punkt 3, seine Felder stehen in Schritt ①), die **Übersicht** steht vorn (Punkt 4: „Stelle die Übersicht als erstes dar"), und die **Fußleiste** samt dem zweiten Paar [i] [KI] ist gefallen (Punkt 2) — beides trug seit #207 dieselben zwei Knöpfe ein zweites Mal. `LaufStarten()`, `ErgebnisSpeichern()` und `SpeichernMoeglich` sind öffentlich (die Werkzeugleiste der Ansicht ruft sie, auf beiden Plattformen), `StandGeaendert` meldet dem Wirt, wenn sich daran etwas ändert; `StartBlatt`/`BlattWaehlen()` nehmen das Reiterblatt der MARKE entgegen, und `Automatikstart` schaltet die Ansicht AB (der Lauf ist ein bewusster Klick). **Seit Auftrag #220 darf ein Wirt ihren Fortschrittsbalken an SEINER Stelle zeichnen**: `FortschrittZeigen` (Vorgabe `true`) schaltet den eigenen ab, `Anteil`, `Fortschrittstext`, `AbbruchMoeglich` und `LaufAbbrechen()` geben dieselben Werte heraus — EIN Lauf, EIN Fortschritt, nur woanders gezeichnet. Datenseite: `Views/Simulation/SimulationErgebnisHuelle.cs` in fünf Teildateien |
| `Strom/StromspeicherAuslegungSeite` | die **STROMSPEICHER-AUSLEGUNG** (Paket P3 / Auftrag #192, 11.09.2026) — die vierte Fachseite an `AppWurzel`, Schlüssel `STROMSPEICHER_AUSLEGUNG`. Eine **Ablaufleiste** mit fünf Stationen (1 Speicher · 2 Daten & Kosten · 3 Betriebsführung · **4 Optimierung** · 5 Ergebnis), seit **Auftrag #225** (Anwenderwunsch 11.09.2026) mit `Buendig="true"` — alle fünf stehen linksbündig in EINER Reihe ohne Lücke, ohne dass sich die kompakte Fassung der Simulationsseite (`Kompakt`) dabei ändert. **Seit Auftrag #224 (Anwenderentscheid SD‑E‑9, Option A) sind alle fünf Stationen BLÄTTER**: `MitAktion="false"` nimmt der Leiste ihren Aktionsplatz, die Station 4 heißt nicht mehr „4 Bewerten"/„4 Größen optimieren", sondern **„Optimierung"** und trägt den Rechenknopf auf ihrem Blatt. Die Nummer steht seither im **Kreis** vor dem Namen (`.epos-ablaufleiste-stufe`, ✓ für erledigt), weshalb die Stationstitel ihre führende Ziffer abgeben — auch die der Simulationsansicht. Die Blätter sind die vorhandenen Bausteine `SpeicherFlottenEditor`, `SpeicherAuslegungEditor`, `SpeicherFlottenBetriebEditor` und `SpeicherFlottenErgebnisAnsicht` — **keine Kopien**. **Seit Auftrag #206 gibt es genau EINEN Weg** (Anwenderentscheid **SD‑E‑8** vom 11.09.2026: „Es ist nicht sinnvoll, einen Unterschied zwischen Einzelspeicher und Flotte zu machen“): Die Ansicht rechnet immer die Flotte, und ein Einzelspeicher ist eine Flotte mit genau EINER Einheit — die Vorbelegung macht der Kern (`SpeicherFlottenStudieCtrl.Vorbelegung`, seit #210 je Speicheranlage des Projekts eine Einheit), nicht die Seite. Der **Modusschalter Flotte / Einzelspeicher** (SD‑Q1) und die drei Einzelspeicher-Blätter `EinzelspeicherSuchraum`, `EinzelspeicherBetrieb`, `EinzelspeicherErgebnis` sind damit gefallen, ebenso der Suchraum-Teil des `SpeicherAuslegungEditor` samt `NurQuellenKostenProfile` und der `Modusknopf` der `Ablaufleiste`. Die fünf Betriebsziele, das Peak-Ziel, die Diagnose und die Größen-Sicht gelten für jede Einheitenzahl; die **Verteilung erscheint erst ab zwei Einheiten** (`SpeicherFlottenBetriebEditor.VerteilungZeigen`, Vorgabe `true` — der zweite Wirt `StromspeicherReiter` bleibt unberührt; ausgeblendet statt gesperrt, mit einer Erklärzeile). **Zwei Wege des Einzelspeichers blieben, weil der Projektlauf weiter zwei Pfade führt (SD‑Q2):** das Rückschreiben in die Projektanlage in Schritt 5 (Knopf mit Rückfrage, nur für die eine Einheit mit Anlagenbezug — `Dienste.AuslegungUebernehmen`) und der **Leistungspreis als EINE Eingabe** in Schritt 2 (`LeistungspreisBlock` → Suchraum, `FlottenTarif.LeistungspreisEuroProKw` und Projektvariante, W11b‑E‑3). Der Einzelspeicher-OPTIMIERER ist nicht gelöscht: `SpeicherOptimierungCtrl`/`SpeicherOptimierer` tragen weiter das Betriebsbild des Berichts und die KI-Aktion `speicher_optimieren`. Dazu das `FlottenDiagnosebanner` über den Kacheln (warum eine Flotte arbeitslos blieb, mit Abhilfeknöpfen) und der `PeakZielBlock` (Vorschlag aus der Reihe, „Peak-Ziel bestimmen…“, Netzladung je Ziel — **SD‑Q3/SD‑Q5**). Überlagerungen gibt es nur noch für die CSV-Spaltenzuordnung, die Prognosentabelle und die Rückfrage beim Verlassen (Muster 62b‑E‑1). **Mit #224 verteilen sich die Blätter neu:** Schritt 1 trägt nur noch die Einheiten, „Netz und Planung" wird `Dialoge/Strom/SpeicherFlottenNetzBlock` in Schritt 3, die wirtschaftliche Jahresprojektion `Dialoge/Strom/SpeicherFlottenWirtschaftBlock` in Schritt 2 (SD‑Q12), der Suchraum zieht nach Schritt 4. Weil damit **vier** Blätter denselben `FlottenStudieKonfiguration` beschreiben, der Einheiteneditor aber eine eigene Kopie hält, setzt `FlotteGeschrieben()` nach jeder Änderung von außen eine frische Kopie ein — sonst überschriebe ein späteres Editor-Speichern die Eingaben der anderen Blätter. Die Vorprüfung steht seither als `Seiten/Strom/Hinweiszeilen` über der Leiste (eine Zeile je Hinweis mit Symbol, ab zwei Hinweisen ein aufklappbarer Block „n Hinweise") statt als ein `Warnbanner` je Hinweis. **Seit Auftrag #196 trägt die Ansicht die `SpeicherFlottenGroessenAnsicht`** (Paket P4) — sobald ein Rastersuchergebnis vorliegt, VOR der Ergebnisansicht (Konzept 2.5); die einfache Kandidatentabelle der `SpeicherFlottenErgebnisAnsicht` ist damit gefallen, Empfehlung und CSV-Export sind mitgewandert. **Seit #224 steht sie in Schritt 4** — bei der Suche, aus der sie stammt; Schritt 5 bewertet EINE Bestückung und sagt in einer Zeile welche (`FLOTTE_SEITE_ERGEBNIS_KANDIDAT`). „Kandidat übernehmen“ geht über `SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration` DENSELBEN Weg wie „Beste Flotte übernehmen“ (`FlotteSetzen`): Achsen leeren, Suchlauf aus, Schritt 5 veraltet, zurück auf Schritt 1 mit Hinweisbanner — mit der Rückfrage 62b‑E‑1 davor, wenn Schritt 1 ungespeicherte Eingaben trägt. **Mit ihr sind `SpeicherFlottenDialog` und `SpeicherOptimierungDialog` gelöscht** (Regel iZ5). Datenseite: `Views/Stromspeicher/StromspeicherAuslegungHuelle.cs` → `EPOS.Kern/Controller/StromspeicherAuslegungCtrl` — die Hülle behält nur Dateiwähler, `Task.Run` und `CancellationTokenSource`. **Woher der Simulationslauf kommt:** Die Ergebnisseite reicht ihren Lauf vor dem Ansichtswechsel herein (`Anmelden`); ohne Anmeldung bietet die Seite den eigenen Lauf an (Muster W11a). **Wohin „← zurück" führt, entscheidet seit #207 der Rückwegstapel der `AppWurzel`** (SIM‑Q4): Wer aus der Ansicht `SIMULATION` kam, steht danach wieder in Schritt ③ auf dem Reiter „Stromspeicher" — das war die Lücke, die `_auslegungRueckweg` unter Windows offen ließ, weil das Ergebnis dort gar keine Ansicht war |
| `Strom/OptimierungBlock` | **STATION 4 „OPTIMIERUNG"** der Stromspeicher-Auslegung (Auftrag #224, Anwenderentscheid **SD‑E‑9** Option A, Konzept `Konzept_Stromspeicher_Dialoge` 7.4): genanntes Ziel (Kapitalwert), die `Optionsgruppe` *bewerten* / *beste Größe suchen* (der frühere Schalter „Größen optimieren" aus Schritt 1), der **Suchraum je Einheit** als `table.epos-flotte-suchraum` (sieben Spalten, eine Zeile je `FlottenAuslegungsAchse`, „—" für die aus der Größenkopplung abgeleitete Spalte), die **live mitzählende Kandidatenzeile**, der Feinraster-Schalter mit `Herleitungszeile`, der Rechenknopf, der Kasten **„Bestes Ergebnis"** (drei Karten, dazu die Marke Grob-/Feinraster) und darunter die eingebettete `SpeicherFlottenGroessenAnsicht`. **Die Kandidatenzeile rechnet NICHT selbst**: `FlottenOptimierer.Kandidatenzahl` ist die EINE Zählregel, mit der auch der Lauf annimmt oder abweist — eine zweite Rechnung in der Maske könnte davon abweichen. Reißt das Raster die Grenze, sperrt `FlotteEingabenPruefen` den Knopf mit benanntem Grund |
| `Strom/Hinweiszeilen` | die **kompakte Vorprüfung** (Auftrag #224, Konzept 7.8): eine Zeile je Hinweis mit Symbol und inlinem „erklären lassen"; ab **zwei** Hinweisen ein aufklappbarer Block „n Hinweise" (anfangs offen). Sie löst die Reihe einzelner `Warnbanner` über der Ablaufleiste ab — fünf Bänder schoben die Leiste aus dem Bild |
| `Assistent/AssistentSeite` | der **PROJEKTASSISTENT** (iU9‑W16a.5, S3) — dreizehn Schritte in EINER Komponente, in der bitgleichen Reihenfolge des Bestands (`AssistentSeiten.ERZEUGER` = Nummernkatalog `WizardItemClass`). Je Seite kommt ein fertiger Parametersatz herein, bei JEDEM Betreten neu erfragt (das tat `BlazorAssistentSeite.Bestuecken` ebenso). Das linke Band steht nur in Betriebsart BEARBEITEN auf Schritt 0 — `InfoKnopf`, `ProjektListe`, „Projekt öffnen". Die dritte Fachseite, die iOS über `AppWurzel` erreicht — und **seit dem 11.09.2026 (Anwenderentscheid W16a‑E‑1 / W16b‑O‑5, Aufgabe #62b) auch unter Windows eine FREIE ANSICHT**: die letzte Fachseite, die ihre modale Hülle verliert. Modal war sie, weil ihre zwei Aufrufer auswerteten, OB gespeichert wurde, und der Rahmen danach den Projektkontext nachzog; beides hängt jetzt am Speicherlauf selbst (`AssistentHuelle.ProjektkontextNachziehen` → `ProjektKontextCtrl.Gewechselt`), und **kein `DialogResult` wird mehr gelesen**. Die drei Schlüssel `ASSISTENT`, `PROJEKT_NEU` und `PROJEKT_BEARBEITEN` führen auf dieselbe Ansicht; die Betriebsart sagt entweder der Schlüssel oder das Argument (`OeffneMaske(Masken.Assistent, 0|1)`). Dazu gehört die **Rückfrage beim Verlassen** (Anwenderentscheid **62b‑E‑1**, 11.09.2026): Wer den Assistenten mit ungespeicherten Eingaben verlässt — Menüpunkt, Kachel, „Abbrechen", „Projekt öffnen" oder das Schließen des Programms —, bekommt drei Wege in einer `Ueberlagerung`: **Speichern** (derselbe Weg wie der Knopf, bei Fehlschlag bleibt der Assistent stehen), **Verwerfen** (Wechsel ohne zu schreiben) und **Bleiben**; Esc heißt „Bleiben". **Ohne Änderung gibt es keine Rückfrage** — was „geändert" heißt, sagt der ZUSTAND (`AssistentCtrl.HatAenderungen`, ein Abdruck über Projektkopf, die sechs Listen und die dreizehn Seitenschalter), nicht ein Ereigniszähler. Datenseite: `Views/Wizard/AssistentHuelle.cs` → `EPOS.Kern/Controller/AssistentCtrl` |
| `Start/Startseite` | **DIE STARTSEITE** (iU9‑W16b.2, S1) — die Wurzel der Anwendung aus Anwendersicht, Nachfolge von `Views/Hauptformular/Form_Start` (2 300 Z. + 1 381 Designer, 108 Kartenzeilen). Kopfband (Produktgattung, Projekt/Varianten, Statuszeichen, Klimaregion mit Speicherknopf), **sechs Reiter mit 21 Kacheln**, Fußleiste ◀/▶. Die Reiter 2 bis 6 sind gesperrt, solange kein Projekt offen ist (`Reiterblatt Bedienbar`). **Warum, sagt seit dem Anwenderwunsch W16b‑E‑6 (05.09.2026) eine Staffelung statt eines Dauerbanners:** eine leise Zeile im Reiter „Projekt" (`START_EINSTIEG`, mit dem ⚠ des Kopfbandes davor — mit Projekt verschwindet sie), der Grund als `title` am weich gesperrten Reiterknopf (`Sperrgrund`, `START_SPERRE_TIPP`) und das Banner erst NACH dem Versuch, drei Sekunden lang — genau so, wie der Vorläufer seinen `Form_Hinweis` zeigte. Auch „Weiter ▶" bleibt ohne Projekt anklickbar und meldet; er ist der Weg der Tastatur zu derselben Auskunft. Der Statuspunkt je Kachel kommt aus der EINEN Bitmaske des Kerns (`KomponentenBestandCtrl`, E‑3/N6); die 13 `Paint`-Handler des Vorläufers sind eine CSS-Klasse geworden, die drei Bindemuster für den Kachelklick (Wörterbuch mit 24 Einträgen, 14 Weiterleitungshandler, sechs `Geklickt`) EIN `@onclick` mit einem sprachneutralen Schlüssel. Von W16b.4 bis #207 trug sie zwei weitere Ansichten: die **Simulationskonfiguration als freie Ansicht** (sie löste die Startseite ab) und das **Simulationsergebnis als `Ueberlagerung`** — beide ohne zweites Fenster (E‑5; die Entscheide R‑W10b‑1 und R‑W11‑1 sind damit geschlossen). **Mit #207 ist beides gefallen** (Anwenderentscheid SIM‑Q1): Die Seite bettet KEINE Simulationsseite mehr ein und trägt keine Überlagerung dafür — der Knopf „Simulation Konfiguration…" und die Kachel „Simulation" melden über `Dienste.Navigation` die Ansicht `SIMULATION` mit einer MARKE (`schritt=1` bzw. `schritt=3`), auf beiden Plattformen derselbe Weg; meldet sich niemand zuständig, geht der Klick den gewöhnlichen Weg über `Geklickt`. Die Parameter `SimulationKonfigGaben`/`SimulationErgebnisGaben`/`ErgebnisTitelText` sind damit entfallen. **Anwenderwunsch 05.09.2026** („Icons fehlen … Design ähnlich zum WinForms"): Jede Kachel trägt wieder ihr Sinnbild (`Kachelbilder`, **W16b‑E‑3**), das Kopfband ist ZWEI Zeilen wie im Designer — Gattungsband oben, darunter Klimakasten links und Projektkasten rechts (`panelKlima` x=79/677 breit, `panelVariante` x=776/489 breit) mit dem Statuszeichen VOR der Beschriftung —, das Kachelraster nimmt die Kachelbreite 404 px des Vorläufers, und die **Gattungszeile steht nur ohne `Kopfleiste`** (`KopfbandZeigen`, **W16b‑E‑4**): unter Windows nennt sie das Kopfband des Hauptfensters, auf iOS ist die Kopfleiste leer. Datenseite: `Views/Hauptformular/StartseiteHuelle.cs` → `ProjektKontextCtrl`, `StartseiteCtrl`, `KomponentenBestandCtrl`, `BedarfsZustand` |
| `Start/ProjektReiter` … `SimulationReiter` | die **fünf Reiterkomponenten** der Startseite (5 / 4 / 3 / 7 / 2 Kacheln); der sechste Reiter ist die `BerichteKostenSeite` aus W5. `ProjektReiter` trägt seit **W16b‑E‑6** den **Einstiegshinweis** (`HinweisText`/`HinweisZeichen`) — leer = keiner. `ErzeugerReiter` trägt zusätzlich die **Weiche** der Solarthermiekachel (Profil / Ganglinie) — sie entscheidet, welcher der beiden Dialoge aufgeht, und stand im Bestand als zwei `RadioButton`, die ZUGLEICH den Status trugen; die Farbe sagt jetzt allein der Statuspunkt. `SimulationReiter` trägt die Projektzusammenfassung und den Knopf „Simulation Konfiguration…", der als 21. Kachel zählt — und **seit Auftrag #220 (Anwenderentscheid SIM‑E‑2, 11.09.2026) RECHNET er**: ZWEI Spalten (`.epos-simreiter`, `minmax(0, 1fr) minmax(0, 2fr)`, unter 1100 px untereinander). **Links** Zusammenfassung, Konfigurationsknopf und die Kachel „Simulation starten" — ihr Klick startet den Lauf AN ORT UND STELLE, ohne Ansichtswechsel —, darunter der Baustein `Fortschritt` mit „Abbrechen"; der Sperrgrund steht AN der Kachel (Statuszeile, grauer Punkt) UND als `Warnbanner` darunter (die weiche Hälfte der Regel W16b‑E‑6). **Rechts** dieselbe `SimulationErgebnisSeite` wie Schritt ③ mit „Ergebnis speichern" im Spaltenkopf, ohne gerechneten Lauf der Hinweis „Noch kein Ergebnis — Simulation starten.". Die Dienste kommen aus DERSELBEN Quelle wie die Ansicht (`Startseite.SimulationGaben` ← `AppWurzel.SimulationGabenHolen`, beim BETRETEN des Reiters geholt) — keine zweite Hülle, und damit rechnet der Reiter auch auf iOS. **Ohne Parametersatz bleibt alles wie vor #220** (`.epos-simreiter--allein`, die Kachel meldet ihren Schlüssel). Der Reiter meldet seinen `Hilfekontext` („Startseite · Simulation · <Blatt>") und zeichnet unter Windows keine eigene Pille (#221) |
| `Start/Kachelschluessel` + `Reiterschluessel` | die sprachneutralen ASCII-Schlüssel der 21 Kacheln und der sechs Reiter |
| `Start/Kachelbilder` | **das Sinnbild JE KACHEL** (Anwenderwunsch 05.09.2026, **W16b‑E‑3**) — Dateiname und Stilklasse zu jedem der 21 Kachelschlüssel, abgelesen aus `pBox_*.BackgroundImage` und `karte_*.KartenBild` des eingefrorenen `Pruefmuster/Hauptformular/Form_Start.Designer.cs`. **Das Bild hängt am SCHLÜSSEL, nicht an den Daten**: Stünde es in `StartKachel`, müssten es die Windows-Hülle und der iOS-Weg getrennt setzen, und die zwei Listen liefen beim ersten neuen Bild auseinander — dieselbe Bauart wie `Menuetabelle`. Nachweis: `EPOS.UI.Tests/Seiten/KachelbilderTests` (jede Datei wird im Dateisystem nachgesehen) |
| `Assistent/ProjektKopfSeite` | die **erste Assistentenseite** (iU9‑W15a.6) — neun Verwaltungsfelder des Projekts. Sie ist die einzige Seite mit einem ERGEBNIS: eine EINELEMENTIGE geteilte Liste `ProjektKopfDaten`, die die Seite an Ort und Stelle beschreibt (Weg (a), Befund W15a‑B42). Bis iU9‑W16a.5 trug sie eine `BlazorAssistentSeite<…>`; seither hält der Assistent selbst die Liste. Datenseite: `Views/Wizard/ProjektKopfHuelle.cs` |
| `Simulation/UebersichtReiter` … `SpeicherVariantenVergleich` | die **elf Reiterkomponenten** der Ergebnisseite (`ParameterReiter` ist mit #216 gelöscht). Sie nehmen die DTO aus `EPOS.Kern/Controller/SimulationErgebnisCtrl` unmittelbar entgegen — die sind für genau diese Seite gebaut worden (iU9‑W11a.3) und wären als zweite Datenform eine zweite Wahrheit. Die Bilder kommen als PNG aus dem Kern-Renderer, **erst beim Betreten eines Reiters** und je Schalterstellung zwischengespeichert. Der `StromspeicherReiter` zeigt den `SpeicherParameterBlock` seit **#216** unter der Überschrift **„Einzelanlage (klassischer Projektlauf)"** (`SP_GRP_EINZELANLAGE`) und nur, solange `Daten.FlotteImProjektAktiv` falsch ist — bis dahin hing er an `FlottenEinstiegMoeglich`, also an der Frage, ob die PLATTFORM eine Flotte rechnen kann, und war unter Windows damit unerreichbar |

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
Wo dieselbe Seite **zugleich eine Ansicht der `AppWurzel`** ist, bekommt sie ein
parameterloses `Geschlossen` — den Rückweg auf die `Startansicht`, ohne Ergebnis;
`BerichteKostenSeite` zeigt den Knopf **nur**, wenn der Rückruf gesetzt ist, und steht
als sechstes Reiterblatt der Startseite unverändert ohne ihn (W16c‑E‑3).
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
| `Bedarf/GebaeudeDialog` | `Form_Gebaeude` (iU9‑W9.2) | `Views/Gebäude/GebaeudeHuelle.cs`; Assistentenseite 2, Admin-Modus, **vier** Überlagerungen (seit W9.8 der Wärmebedarf) |
| `Bedarf/GebaeudeBedarfDialog` | **keines** — der Bestand kannte den Knopf nicht (Anwenderwunsch **W9‑E‑2**, 05.09.2026, iU9‑W9.8): `Form_Gebaeude` trug im Detailblock nur „Ändern", und die einzige Maske mit einem Knopf „Simulation" war `Form_Simulation_Kurz` (mit iF29 stillgelegt) — die rechnete das GANZE Projekt. Neu ist die AUSKUNFT, nicht die Rechnung | dieselbe Hülle → `EPOS.Kern/Controller/GebaeudeBedarfCtrl`, `ChartRenderer.GanglinieNormiert` (dasselbe Bild wie B1 der Ergebnisseite); erscheint als Überlagerung im Gebäudedialog |
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
| `Strom/SpeicherOptimierungDialog` — **gefallen mit #192**; sein Inhalt wurde zu den drei Blättern des Modus „Einzelspeicher“ in `Seiten/Strom/StromspeicherAuslegungSeite`, und die sind mit **#206** (SD‑E‑8) ebenfalls gefallen: Die Ansicht kennt nur noch EINEN Weg. Der RECHENWEG lebt weiter — `SpeicherOptimierungCtrl` trägt das Betriebsbild des Berichts und die KI-Aktion `speicher_optimieren` | `Form_SpeicherOptimierung` (1 325 Z., **W11b‑B‑5**, Windows-Abnahme V2 vom 07.09.2026) — die LETZTE WinForms-Fachmaske. Der Anwender gab sie mit zwei Befunden zurück: „Texte überschneiden sich" (zwei Labels derselben Zeile lagen übereinander, der Erklärtext ragte aus seiner GroupBox) und „Dialog stürzt nach kurzer Zeit ab" (jeder Lauf hängte über `Plot.Add.ColorBar` eine weitere ScottPlot-Farbskala an denselben Plot; `Plot.Clear()` räumt Plottables, aber keine Panels — die Zeichenfläche schrumpfte je Lauf um rund 78 Bildpunkte und war ab dem achten Lauf null). Beides ist hier strukturell weg: Die Erklärung ist eine `Herleitungszeile` UNTER der Gruppe, die zwei Bilder kommen als fertige PNG aus dem Kern, und es gibt keinen Zeichenzustand, der sich ansammeln könnte | `Views/Simulation/SimulationErgebnisHuelle.Optimierung.cs` → `EPOS.Kern/Controller/SpeicherOptimierungCtrl`, `ChartRenderer.Optimierungsraster`/`.Schnittkurve`; er erscheint als **Überlagerung** der Ergebnisseite, der Lauf geht in `Task.Run` und sein Fortschritt kommt GEDROSSELT an (höchstens jeder 10. Punkt, höchstens alle 100 ms) |
| `Import/KatalogImportDialog` | **vier** Masken: `Form_Heizkessel_einlesen`, `Form_PufferSp_einlesen`, `Form_SolarKollektoren_einlesen`, `Form_WP_einlesen` (iU9‑W13.1) — und seit **W13‑E‑2** (07.09.2026, Stufe S1) eine **FÜNFTE Ausprägung ohne Vorläufer: der Stromspeicher**. Sie bringt fünf Profilteile mit, die bei den vier VDI-Ausprägungen leer bleiben und dort nichts ändern: **`Quellen`** (drei Knöpfe statt des Dateiwählers — „CEC-Liste abrufen" über `CecSpeicherDienst` aus dem Netz, „CEC-Datei laden" für XLSX/CSV, „bslib laden" für die Auslieferungsdatei `VDI-3805-Daten/Stromspeicher/bslib_database.csv`), **`Listenspalten`** (Quelle, Hersteller, Modell, kWh, kW, η_RT, Chemie — zwei Spalten reichen für 6 654 Geräte nicht), **`Zweitfilter`** (ein zweiter Zahlenbereich: kWh UND kW), **`HerstellerFilter`** (130 Hersteller in EINER Datei) und **`Hinweis`** (die Zeile „Die Quelle liefert keine Kosten…" aus Entscheid Q3 — dorthin ist auch der Stufenhinweis der Wärmepumpe gewandert, damit es nicht zwei Wege zu einer Zeile gibt). **Der QUELLSCHLÜSSEL wählt den Zerleger, nicht die Dateiendung** — `bslib_database.csv` und eine ausgeleitete CEC-CSV sehen gleich aus; deshalb trägt der `Lesen`-Delegat ihn als ersten Parameter. Mehrfachwahl, Doppelklick, Konfliktdialog und Sammelmeldung kommen unverändert aus dem Wirt (W6‑E‑5) | `Views/Import/KatalogImportHuelle.cs` → `KatalogImportProfil`, `KatalogImportAblauf`; EINE Hülle für alle fünf Maskenschlüssel, Lesen und Schreiben in `Task.Run`. **Beim Stromspeicher beschafft die HÜLLE die Datei** für die zwei Quellen ohne Wähler: `CecSpeicherDienst.LadenAsync` (30‑Tage‑Zwischenspeicher) bzw. der Herstellerdatenpfad mit Rückfall auf den Wähler — Plattformsache, nicht Sache der Komponente |
| `Bedarf/WaermebedarfAdminDialog` | `Form_AdminWaermeeinlesen` (iU9‑W13.2) | `Views/Wärmebedarf/WaermebedarfAdminHuelle.cs` → `WaermebedarfStammCtrl`, `GanglinienTextDatei`, `DublettenPruefung`; erscheint auch als Überlagerung in `WaermebedarfExternDialog` |
| `Photovoltaik/ModulImportDialog` | `Form_CECImport` / Klasse `Main_PV_Test` (iU9‑W13.3) **und** `WechselrichterImportDialog` (W6‑E‑2/S1.5) — seit **W6‑O‑1** (06.09.2026) EINE Komponente mit ZWEI Ausprägungen: Modul (CEC, CEC-Datei, PAN) und Wechselrichter (CEC, CEC-Datei, OND) | `Views/Photovoltaik/ModulImportHuelle.cs` → `CECDataService`, `PanDataService`, `CecWechselrichterDienst`, `OndWechselrichterDienst`, `PhotovoltaikStammCtrl`, `WechselrichterStammCtrl`; Spalten, Detailfelder, Reiter, Filter und Quellen als DATEN in `ModulImportProfil` (Zwilling zu `ModulKatalogProfil`), eine Zeile ist eine `ImportZeile`; Netzabruf mit `Fortschritt` und Abbrechen, 20 746 bzw. 2 343 Zeilen im virtualisierten `Raster`. **Seit W6‑E‑5** (07.09.2026) Mehrfachwahl mit Kontrollkästchen: „Übernehmen“ schreibt alle gewählten Sätze in einem Zug (EINE Rückfrage für alle Warnungen, EIN Konfliktdialog, Bilanz „n übernommen, m übersprungen“), Doppelklick übernimmt sofort NUR diese Zeile, und die Wahl hängt an den **Sätzen** — sie überlebt das Umfiltern, damit man über mehrere Hersteller hinweg sammeln kann |
| `Bedarf/BedarfAdminDialog` | **drei** Masken: `Form_Stromverbraucher_Admin`, `Form_Prozesswaerme_Admin`, `Form_Brauchwasser_Admin` (iU9‑W14b.1) | `Views/Bedarf/BedarfAdminHuelle.cs` → `BedarfStammCtrl`, `BedarfsVorschauCtrl`; EINE Hülle für drei Maskenschlüssel, **vier** Überlagerungen aus Welle 8 |
| `Solarthermie/SolarganglinieAdminDialog` | `Form_Solarganglinie_Admin` (iU9‑W14b.2) | `Views/Solarthermie/SolarganglinieAdminHuelle.cs` → `SolarganglinieStammCtrl`, `GanglinienTextDatei` (mit Kopfzeile); erscheint auch als Überlagerung in `SolarganglinieDialog` |
| `Erzeuger/KatalogBrowserDialog` | **vier** Masken: `Form_Heizkessel_Admin`, `Form_BHKWAdmin`, `Form_SolarKollektorenAdmin`, `Form_PufferSp_Admin` (iU9‑W14a.1) | vier Hüllen mit gemeinsamem Kern (`Views/Erzeuger/KatalogBrowserHuelle.cs`) → `KatalogBrowserProfil`; der Katalogeditor und die Namensabfrage sind Überlagerungen, `NurLesen` ist der Lesemodus des Pufferspeichers |
| `Erzeuger/PufferSpKatalogDialog` | `Form_PufferSp_Bearbeiten` (iU9‑W14a.2) | `Views/Pufferspeicher/PufferSpAdminHuelle.cs` → `PufferSpStammCtrl.Anlegen`/`Ueberschreiben`, `SpeichertypAbbildung`; erscheint als Überlagerung im Browser |
| `Erzeuger/ModulKatalogDialog` | **zwei** Masken: `Form_AdminStromspeicher`, `Form_AdminPV` (iU9‑W14a.3) | `Views/Stromspeicher/StromspeicherAdminHuelle.cs`, `Views/Photovoltaik/PvAdminHuelle.cs` → `ModulKatalogProfil`; Browser und Editor in EINER Komponente |
| `Wirtschaftlichkeit/GesetzeskatalogDialog` | `Form_Gesetzesparameter` (iU9‑W14c.2) | `Views/Admin/GesetzeskatalogHuelle.cs` → `GesetzKatalog`; er erscheint als eigenes Fenster (Menü) UND als Überlagerung in `KostenKomponenteDialog` und `WirtschaftlichkeitParameterDialog` — die letzten zwei `Sprungziel`e fallen damit |
| `Wirtschaftlichkeit/GesetzeskatalogZeileDialog` | `Form_GesetzparameterZeile` (iU9‑W14c.1) | keine Hülle — eine Überlagerung im Katalog; Schlüssel und Klasse sind beim Ändern gesperrt, ein leeres Wertfeld ist NULL und nicht 0 |
| `Admin/KatalogDublettenDialog` | `Form_KatalogDubletten` (iU9‑W14c.5, ohne Designer) | `Views/Admin/KatalogDublettenHuelle.cs` → `DublettenPruefung`, `DublettenBaum`, `DublettenBefundText`, `KatalogBereinigung`; der Scan läuft in `Task.Run` mit `Fortschritt`, das Umbenennen ist der `NamensDialog` **mit Prüfung** |
| `Admin/EinstellungenDialog` | `Form_AdminSettings` (iU9‑W14c.6) | `Views/Admin/EinstellungenHuelle.cs` → `EinstellungenCtrl`; die Rubrikenliste ist ein SENKRECHTER `Reiter` mit vier Blättern, der KI-Abschalter läuft über `KiEinwilligung` |
| `Projekt/ProjektWahlDialog` | **zwei** Masken: `Form_ProjektAuswahl` und `Form_ProjektDelete` (iU9‑W15a.2) | `Views/Projekt/ProjektWahlHuelle.cs` → `ProjektCtrl.NamenListe`; der Zweck (Öffnen/Löschen) entscheidet über Titel, Knopftext und die Sicherheitsabfrage. **Befund #217** (11.09.2026, Anwenderbefund „Absturz Projekt löschen"): `FrageMehrereFormat` trägt seit der Ressource `PDLG_RUECKFRAGE` ZWEI Platzhalter (`{0}` Anzahl, `{1}` Namensliste) — die Razor-Vorgabe führte bis dahin nur `{0}`, der Code formatierte mit einem Argument, und jede echte Installation stürzte beim Löschen mehrerer Projekte mit `FormatException` ab; die bunit-Fälle blieben grün, weil ihre Vorgabe denselben Fehler trug wie der Code. Die Namensliste (höchstens zwölf Namen, Varianten gekennzeichnet, sonst „… und n weitere") entsteht seither als eigene Zeichenkette und geht als ZWEITES Argument in `string.Format`. Wache: `EPOS.UI.Tests/Dialoge/ProjektWahlPlatzhalterWacheTests` — für jeden `…Format`-Parameter mit Platzhaltern in der Vorgabe muss der zugeordnete Ressourcentext (Zuordnung aus `ProjektWahlHuelle.Gaben`) dieselbe höchste Platzhalternummer tragen |
| `Projekt/ProjektKopieDialog` | `Form_ProjektSpeichernUnter` (iU9‑W15a.4) | `Views/Projekt/ProjektKopieHuelle.cs` → `ProjektDuplizierenCtrl.PruefeNamen`/`Duplizieren`/`VerwaltungsfelderSetzen`; der Kopierlauf läuft in `Task.Run` mit `Fortschritt` und Abbrechen |
| `Projekt/ProjektTransferDialog` | `Form_ProjektExportImport` (iU9‑W15a.5, ohne Designer) | `Views/Projekt/ProjektTransferHuelle.cs` → `ProjektExportImportCtrl` (seit W15a.0e im Kern); vier Pfaddelegaten — Dateiwahl lesend und schreibend, Sicherungskopie, Importbericht |
| `Hilfe/TextAnzeige` | `Form_TextAnzeige` (iU9‑W15b.2) | keine Datenseite; sie erscheint als Überlagerung im Chat (Aktionsprotokoll und Sendevorschau) |
| `Hilfe/KiHinweisDialog` | `Form_KiHinweis` (iU9‑W15b.3) | `Views/Help/KiHinweisHuelle.cs`; sie hängt `KiEinwilligung.Nachfragen` in `Program.Main` ein — ohne diesen Aufruf gibt es keinen Weg zu einer Einwilligung und damit keine Übertragung |
| `Hilfe/KiEinstellungenDialog` | `Form_KiEinstellungen` (iU9‑W15b.4) | `Views/Help/KiEinstellungenHuelle.cs`; der Schlüssel geht als Vorbelegung hinein und über `KiEinstellungenErgebnis` heraus (Regel S‑1), das Feld ist `type="password"` (S‑2), „Modell neu erkennen" trägt den Seiteneffekt der Vorlage (E‑5) |
| `Hilfe/KiChatDialog` (+ `KiBestaetigungBlock`, `KiWerkzeugliste`, `KiEingabezeile`) | `Form_KiChat` (1 704 Z., iU9‑W15b.7) | `Views/Help/KiChatHuelle.{cs,Gaben.cs}` — **nicht-modal mit Besitzer** (E‑6); die Komponente kennt weder `KiChatService` noch `KiAusfuehrer` noch das Netz, sie bekommt Delegaten. Zwei getrennte Listen: die Anzeige (Klarnamen aufgelöst) und der Prompt-Verlauf (platzgehalten, H8). **Seit Auftrag #214 führt sie auch den LAUFENDEN RECHENVORGANG** (`KiChatDialog.Lauf.cs`, der Restpunkt aus #201): Sie hält je Anforderung eine `CancellationTokenSource`, meldet Senke und Marke über `KiChatSteuerung` an den Wirt (der die Senke an `KiAusfuehrung.Fortschritt` hängt und die Marke unmittelbar vor jedem Aufruf abholt) und zeigt zwischen Verlauf und Eingabe den Baustein `Fortschritt` — Balken, Schritttext, „Abbrechen"; nach Ende oder Abbruch verschwindet er, und im Verlauf steht die Sache **benannt und mit Dauer**. Der Balken kommt erst mit dem ERSTEN gemeldeten Schritt (eine gewöhnliche Frage beantwortet der Verlauf mit „denkt nach…"), und solange etwas läuft, sind Eingabe, Senden und die Aktionsknöpfe gesperrt — die Einläufigkeit selbst steht im Ausführer, der Dialog spiegelt sie nur. **Die Hülle hält keinen Fortschrittszustand**, iOS erbt es ohne Hüllenarbeit |
| `Lizenz/LizenzVerwaltungDialog` | `Form_LizenzVerwaltung` (iU9‑W15c.5) | `Views/Admin/LizenzVerwaltungHuelle.cs` → `LizenzCtrl`; die einzige Maske des Bestands, die den Lizenzserver anspricht — die Komponente kennt ihn NICHT, sie bekommt fünf Anzeigewerte (`LizenzGaben`) und sechs Delegaten (Regel S‑2). Sie erscheint als eigenes Fenster UND als Überlagerung im Lizenzdialog |
| ~~`Lizenz/ErststartDialog`~~ | `Form_Erststart` (iU9‑W15c.7) | **mit W3 (#157‑E‑1, 09.09.2026) GELÖSCHT**, samt `Views/Admin/ErststartHuelle.cs`, `ErststartCtrl` und den 18 bunit-Fällen: Der Übernahme-Assistent aus Access ist gefallen (Access wurde beim Kunden nie produktiv eingesetzt). Die Datenbank einer Neuinstallation entsteht seither OHNE Oberfläche aus der ausgelieferten Vorlage (`EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs`). Die besitzerlose Hülle bleibt als Bauart erhalten — `LizenzHuelle` fährt sie weiter |
| `Lizenz/LizenzDialog` | `Form_Lizenz` (iU9‑W15c.11) | `Views/Help/LizenzHuelle.cs` → `LizenzTextCtrl`, `ZustimmungCtrl`; **zwei Gesichter, eine Komponente**: Menü „Hilfe → Lizenz" und die EULA-Abfrage beim ersten Start (besitzerlos). Die Verwaltung erscheint darin als Überlagerung (E‑11) |
| `Klimadaten/KlimadatenDialog` | `Form_Klimadaten` (iU9‑W14c.7) | `Views/Admin/KlimadatenHuelle.cs` → `KlimaregionStammCtrl`, `SolardatenCtrl`, `KlimaImportAblauf`, `ChartRenderer.Jahresgang`; der Import läuft in `Task.Run` mit Fortschritt und Abbrechen |
| `Strom/SpeicherFlottenNetzBlock` | **kein Vorläufer** (Auftrag #224) — „Netz und Planung": Netzbezugs- und Netzeinspeisegrenze, Prognoseplanung (nur bei planendem Betriebsziel, `HatPrognoseplanung`) und die Endenergieziele mit den NAMEN der Einheiten. Ausgezogen aus dem `SpeicherFlottenEditor` und in **Schritt 3 Betriebsführung** eingehängt: Anschlussgrenzen sind harte Grenzen des Anschlusses, die Prognose sagt, mit welchem Wissen ein Fahrplan entsteht — beides beschreibt den BETRIEB, nicht eine Einheit. Schreibt am Ort (`FlottenSimulationOptionen`) und meldet über `EventCallback Geaendert` |
| `Strom/SpeicherFlottenWirtschaftBlock` | **kein Vorläufer** (Auftrag #224, SD‑Q12) — die **wirtschaftliche Jahresprojektion** in Schritt 2 „Daten & Kosten": Energie-Ausgleichswert, Kalkulationszins, Projektionsart und Projektjahre, Restwert der Studie und die Grenze „Maximale Auslegungskandidaten". Die drei Expertenfelder tragen je eine `Herleitungszeile` aus Konzept 7.3 — bis #224 standen sie ohne jede Erklärung im Einheiteneditor |
| `Strom/SpeicherFlottenGroessenAnsicht` | **kein Vorläufer** (Auftrag #193, Paket P4 des `Konzept_Stromspeicher_Dialoge`, Abschnitt 2.5) — die GRÖSSEN-Sicht der Speicherflotte: Rasterkarte mit **Schraffur** der unzulässigen Kandidaten und SP‑O‑4-Fußzeile, daneben zwei Schnittkurven mit je einem Schieber über die STELLEN der Achse. **Welche Größen an den Achsen stehen, sagt seit Auftrag #226 die GRÖSSENKOPPLUNG der Suche** (`FlottenAuslegungErgebnis.Achsenmodus`): Kapazität × Entladeleistung, Kapazität × C-Rate oder Leistung × C-Rate; Schieberbeschriftung, Einheit und `alt`-Text wechseln mit, und eine Zelle ohne Kandidaten ist hellgrau statt in der Minimumfarbe (Anwenderbefund 11.09.2026), darunter die **Kandidatentabelle** mit Durchsatz, Vollzyklen, Bezugsspitze und Ersparnis. Bis #193 stand davon eine Texttabelle mit höchstens 50 Zeilen da, und ein arbeitsloser Kandidat war von einem arbeitenden nicht zu unterscheiden (Konzept 1.5). Sortierung und **Spaltenfilter nach dem Katalogfilter-Muster**: Profil und Zeilen baut der Kern, eingeschränkt wird mit `Katalogfilter.Anwenden` VOR der Tabelle (W14a‑E‑10) — damit gelten für die Kandidaten Zahlenausdruck, Verknüpfung und Sortierzyklus der fünfzehn Katalogwirte. Die Optimum-Zeile trägt Fläche UND Marke, der arbeitslose Kandidat ein benanntes Kennzeichen — Farbe allein trägt keine Aussage. **Eingehängt seit #196**: Schritt 5 der Ansicht `STROMSPEICHER_AUSLEGUNG` zeigt ihn, sobald ein Rastersuchergebnis vorliegt; mit ihm kamen die **Empfehlung** des Laufs und der **CSV-Export** des Variantenvergleichs aus der Ergebnisansicht herüber (deren einfache Kandidatentabelle dafür gefallen ist — keine zwei Tabellen) | keine Hülle — `SpeicherFlottenAnzeigeCtrl.Rasterdaten`/`SchnittdatenBeiSpalte`/`SchnittdatenBeiZeile` und die drei Bilder `Rasterbild`/`SchnittbildBeiSpalte`/`SchnittbildBeiZeile` im Kern (bis #226 `Schnittdaten`/`SchnittdatenLeistung`); dort stehen auch die Texte je Kopplung (`Rastertitel`, `Achsentext`, `Schiebertext`, `Werttext`, `Schnitttitel`, `Schnittbeschreibung`) — eine Quelle für Bild und Markup; sie rechnen NICHTS nach, die Kennzahlen hat der `FlottenOptimierer` während der Rastersuche gefüllt. Das Ereignis `KandidatUebernehmen` meldet den gewählten Kandidaten nach außen |

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
er startet beim Öffnen von selbst, wie eh und je. **Der `UebersichtReiter` stand in
ZWEI Rollen** — als Hauptreiter „Übersicht" UND, mit `NurNavigator`, als erstes Blatt
des Ergebnisreiters; **mit Auftrag #222 ist die zweite gefallen** (Anwenderentscheid
11.09.2026: „eine Übersicht"). Zwei Reiter derselben Seite zeigten dieselben Zahlen.
Der Hauptreiter ist seither das **Dashboard** — zwei Spalten Wärme | Strom, je Spalte
Kopfband mit Abzeichen, drei Kennzahlen (Bedarf · Deckung · Rest), der Deckungsring mit
**HTML-Legende** daneben, die Erzeugertabelle und ein Fuß mit Nullzeilenschalter und
Bedarfsknopf —, und der **`ErgebnisReiter` führt DREI Blätter**: Autarkie-Analyse,
Wärme- und Stromproduktion. Startblatt der Seite bleibt die „Übersicht" (#216).
**Regel seit Auftrag #234 (Anwenderrückmeldung 12.09.2026): Ein Ganglinienreiter des
Ergebnisses belegt beim ERSTEN Aufbau seine Reihen vor** — der Wärmegang alle
vorhandenen Erzeuger, alle Speicher und die Bedarfslinie, der Stromgang wie bisher „nur
Gesamt" — **und merkt die Wahl über die SITZUNG** (`Seiten/Simulation/Ganglinienstand`
mit `Ganglinienregister`, Muster „Filterstand je Katalog", S2.5): `Reiterblatt` zeichnet
`@if (Sichtbar)`, der Reiter entsteht bei jedem Blattwechsel neu und verlöre seine
Schalterstellung sonst. Gemerkt werden Bedarfsart, „sortiert", Bedarfslinie und die
Reihen als SCHLÜSSEL — nicht der Datenzoom, und nichts davon dauerhaft; für Proben
tauscht der Parameter `Gedaechtnis` den Stand aus. Was die
Umstellung im Einzelnen behebt — den leeren Ring bei 0 % (`SKPath.ArcTo` zieht bei 360°
nichts), die Zahl weit weg von ihrem Kopf und die abgeschnittene Legende —, steht in
[`Projekte/Konzept_Simulationsablauf_EPOS-Plan.md`](../Projekte/Konzept_Simulationsablauf_EPOS-Plan.md)
Abschnitt 8.

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
Windows bis W11b‑B‑5 die `Sprungbruecke` (Schlüssel → `Form`, modal über dem Dialog).
**Nur für WinForms-Ziele.** Ist das Ziel selbst eine Blazor-Hülle, wird daraus eine
`Ueberlagerung` im selben Fenster: zwei WebViews übereinander sind Risiko R2 des
Wellenplans. Kein Delegat = kein Knopf.

**Seit W11b‑B‑5 (Windows-Abnahme V2, 07.09.2026) ist `Sprungziel` LEER und
`Sprungbruecke.cs` gelöscht.** Zehn Ziele hat der Mechanismus getragen; jedes ist
gefallen, sobald sein Ziel selbst Razor wurde. Das letzte war
`SpeicherOptimierung` — das einzige mit einem Parameter (dem gerechneten Lauf) und
das einzige, dessen Antwort nicht „mit OK geschlossen" hieß. Die Klasse bleibt als
Registerstelle des Musters stehen, damit ein künftiges WinForms-Ziel seinen
Schlüssel wieder dort anlegt; solange `WindowsFormsApplication1` keine Fachmaske
mehr führt, gibt es dafür keinen Anlass.

**Tastatur:** Esc schließt überall. **Enter** bestätigt nur in reinen OK-Dialogen; wo ein Knopf
sofort schreibt (Übernahme, Katalog, Verlauf), bleibt Enter unbelegt — ein versehentliches Enter
wäre dort kein Bestätigen, sondern ein Zufall.

Was ein Dialog beim Port an seinem Vorläufer ändert, steht als Abweichungsliste im jeweiligen
Protokoll unter `WindowsFormsApplication1/Allgemein/Reporting/` (`B5b_…`, `iU9_W1_…`,
`iU9_W2_…`).
