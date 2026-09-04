# iU9 Welle 11b — Simulationsergebnis II: die Ergebnisseite — Portprotokoll

> Umsetzung 04.09.2026 im Arbeitsbaum `agent-af078f97ed75c2ba6`, Basis `81a04ec`
> (`ios_migration` nach dem Merge der Welle 11a). Vorbild in Aufbau und Tiefe: die
> Protokolle der Wellen 10a und 10b im selben Ordner. Regeln: Arbeitsanweisung
> `iU9_W11b_Arbeitsanweisung.md`, Vermessung `iU9_W11_Vermessung.md` (50 Befunde),
> dazu `EPOS.UI/CLAUDE.md`, `EPOS.Kern/CLAUDE.md`, `WindowsFormsApplication1/CLAUDE.md`.
>
> **Das Gate dieser Welle ist wie in W11a der Referenzlauf: byte-gleich für
> 1030/1007/1017.** Er wurde nach jedem Teilschritt gefahren und war jedes Mal grün.

---

## 1. Auftrag und Ergebnis

**Sechs WinForms-Masken — 11 031 Zeilen `.cs`, 4 201 Zeilen Designer, 21 `MessageBox`,
17 Zeichenflächen — sind EINE Razor-Seite mit zwölf Komponenten.** Alle sechs sind im
**selben Commit** gelöscht (Regel R‑W11‑2: maskenweise, nicht reiterweise; reiterweise
stünden zwei WebViews in einem Fenster, Risiko R5).

| Was | ersetzt | Zeilen |
|---|---|---:|
| `EPOS.UI/Seiten/Simulation/SimulationErgebnisSeite.razor` | `Form_Simulation_Detail.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}`, `TabNavigationManager`, `TabListMapper` | 7 629 + 3 082 + 688 |
| `Seiten/Simulation/ParameterReiter.razor` | R1 samt P1…P5 | ~600 |
| `Seiten/Simulation/UebersichtReiter.razor` | R2 **und** `NavigatorUebersicht` | ~180 + 433 + 119 |
| `Seiten/Simulation/BedarfReiter.razor` | R4 | ~500 |
| `Seiten/Simulation/WaermepumpeReiter.razor` | R5 samt drei Unterblättern | ~900 |
| `Seiten/Simulation/HeizkesselReiter.razor` | R6 | ~740 |
| `Seiten/Simulation/SolarthermieReiter.razor` | R7 | ~90 |
| `Seiten/Simulation/BhkwReiter.razor` | R8 | ~700 |
| `Seiten/Simulation/PhotovoltaikReiter.razor` | R9 | ~130 |
| `Seiten/Simulation/StromspeicherReiter.razor` | R10 | ~1 400 |
| `Seiten/Simulation/ErgebnisReiter.razor` | R11 **und** `DashboardForm` | 226 + 488 + 206 |
| `Seiten/Simulation/WaermegangReiter.razor` | `NavigatorWaerme` | 1 083 + 251 |
| `Seiten/Simulation/StromgangReiter.razor` | `NavigatorStrom` | 417 + 248 |
| `Seiten/Simulation/SpeicherVariantenVergleich.razor` | `Form_SpeicherVariantenVergleich` | 832 + 295 |

**Neu auf der Windows-Seite** (vier Teildateien, eine Hülle):
`Views/Simulation/SimulationErgebnisHuelle.cs` (Öffnen, Lauf, Schreibwege),
`…Anzeige.cs` (die Abbildung eines Laufs auf zehn Blätter), `…Bilder.cs`
(17 Zeichenflächen → sieben Renderer-Bilder), `…Wege.cs` (sechs CSV-Exporte, zwei
Überlagerungen mit Rückgabe, der Variantenvergleich).

**Ebenfalls gelöscht, ohne Designer und deshalb nie im Stapellauf:**
`TabNavigationManager` (226 Z.), `TabListMapper` (462 Z.),
`GanglinienDarstellung` (97 Z., der WinForms-Rest nach W11a),
`SchluesselEintrag` (37 Z., letzter Nutzer war `NavigatorWaerme`).

**Getrimmt statt gelöscht:** `Allgemein/GrafikTools/ChartManager.cs` verliert
`DonutChartDrawer` (125 Z.) und `Kacheln` (57 Z.) — ihr einziger Nutzer war
`NavigatorUebersicht`. **Der `ChartManager` selbst bleibt** (A‑12).

### Commits

| Hash | Betreff |
|---|---|
| `5ac1703` | iU9-W11b.0: Vorarbeiten — 78 Ressourcenschlüssel, Sprungbrücke zur Auslegungsoptimierung |
| `28b3e4e` | iU9-W11b.1: der Parameter-Reiter (R1 samt P1…P5) |
| `ef46f11` | iU9-W11b.2: der Übersichts-Reiter (R2 + `NavigatorUebersicht`) |
| `f73e715` | iU9-W11b.3: der Bedarfs-Reiter (R4) |
| `e4d0244` | iU9-W11b.4: der Wärmepumpen-Reiter (R5) samt drei Unterblättern |
| `8949d5f` | iU9-W11b.5: die vier Erzeuger-Reiter (R6–R9) |
| `5ae2359` | iU9-W11b.6: der Stromspeicher-Reiter (R10) |
| `1c7419b` | iU9-W11b.7: Wärmegang, Stromgang und der Ergebnis-Reiter samt Autarkie |
| `0738179` | iU9-W11b.8: der Variantenvergleich (Maske 6) |
| `d28592c` | **iU9-W11b.9: die Ergebnisseite — sechs Masken in EINEM Schritt gelöscht** |
| `326c529` | iU9-W11b.10: Formularkarte auf den Stand nach W11b |
| `874d826` | **iU9-W11b.11: Anwenderentscheid zu W11a‑O‑1 — EINE Restwärmezahl, aus der Deckung gerechnet** |
| `9d9d2f6` | iU9-W11b.12: Protokoll und die drei CLAUDE.md |
| `7553216` | Merge `origin/ios_migration` (`da420f3`, Anwenderentscheide) |

**Eine Abweichung von der Schrittfolge der Arbeitsanweisung, begründet.** Die
Anweisung nennt dreizehn Reitercommits; gebaut sind acht. Zusammengelegt sind die
vier Erzeugerreiter (R6–R9: derselbe Aufbau — Feldblock, Modultabelle, Diagramm —
und eine gemeinsame Probendatei) sowie Wärmegang, Stromgang und Ergebnisreiter
(die drei hängen aneinander: der Ergebnisreiter reicht die beiden anderen als
`RenderFragment` herein, ein Commit ohne sie wäre eine leere Navigation). Die
Ressourcen stehen **vorn** statt hinten — ohne sie übersetzt keine Komponente.

---

## 2. Die Hosting-Entscheidung R‑W11‑1

**Die Komponente ist eine SEITE. Unter Windows steht sie bis W16 in einer modalen
Dialoghülle.**

Umgesetzt wie vorgegeben: `SeitenZustand` für den Projektwechsel, ein Eintrag
`Seitenschluessel.SimulationErgebnis`, ein Zweig in `AppWurzel` und die passende
Methode in `IProjektQuelle` (mit Standardumsetzung `null`, wie in W10b — sonst
bräche `EPOS.iOS`, das weder in `WP-Plan.sln` noch im Solution-Filter steht).
Damit ist sie die **zweite Fachseite**, die iOS über die Wurzelkomponente
erreicht.

Unter Windows zeigt `SimulationErgebnisHuelle.Oeffnen` sie in
`BlazorDialogForm<SimulationErgebnisSeite>` — **1 474 × 821, `Sizable`**, die Maße
des Vorläufers. Die drei Gründe der Anweisung stehen weiter:

1. **Die beiden Bedarfsobjekte gehören `Form_Start`** (Befund W11‑B3) und werden
   hier weitergeschrieben; dort speisen sie die Kachelbeschriftungen. Zwei
   nebeneinander offene Fenster wären im Streit um dasselbe Objekt.
2. Dieselbe Entscheidung wie W10b; W16 legt die endgültige Navigation fest.
3. Die Nebenläufigkeit des Laufs braucht kein nicht-modales Fenster, nur
   `Task.Run` in der Hülle — die modale Hülle bleibt bedienbar.

### R‑W11‑6 beantwortet: `SeitenZustand` wird NICHT zweimal gebraucht

Die Konfiguration (W10b) erscheint als **Überlagerung** der Ergebnisseite, also
Blazor über Blazor im selben Fenster (Regel seit W4.0, Risiko R2). Beide sind
Seiten — trotzdem gibt es **einen** `SeitenZustand`, den der Ergebnisseite.

Grund: `SimulationKonfigSeite` führt `Zustand` als **optionalen** Parameter und
fällt ohne ihn auf `StartProjekt` zurück (`IdProjekt => Zustand?.ProjektId ??
StartProjekt`). Als Überlagerung lebt sie so lange wie die Überlagerung — also
kurz, wie ein Dialog —, und das Projekt wechselt darunter nicht. Der neue
statische Weg `SimulationKonfigHuelle.Gaben(int idProjekt)` liefert deshalb den
Parametersatz **ohne** Zustand; die Ergebnisseite setzt `Geschlossen` selbst und
lädt danach neu (die Reiterleiste hängt an `Tool_1..6`). Ein zweiter
`SeitenZustand` hätte nichts, was er tragen könnte — dieselbe Antwort wie in
W10b § 3.6 für die sieben Unterdialoge.

---

## 3. Bauweise

### 3.1 Drei Navigationen werden eine (Befund W11‑B11)

Der Vorläufer führte für dieselbe Sache drei Wege:

* `tabControl_Simulation` — die Reiterleiste, auf **drei** Reiter reduziert
  (`UpdateTabPages` :2813 räumte sie leer und hängte nur Parameter, Übersicht und
  Simulation wieder ein);
* `listViewQuellen` — die Menüliste mit **acht** Fachreitern, gezeigt über die
  **Steuerelement-Ausleihe**: Die Steuerelemente der Ziel-`TabPage` wurden
  physisch aus ihr entfernt und in `splitContainer_Parameter.Panel2` eingesetzt
  (:5267–5333);
* `TabListMapper` (462 Z.) — der `tabControl_Einstellungen` ein zweites Mal als
  Menüliste zeichnete.

Zusammen rund **700 Zeilen**. In Blazor ist es **ein** `<Reiter>` mit **zehn**
Blättern. Elf waren es nur, weil R3 „Simulation" kein Fachreiter war, sondern der
Behälter der Menüliste; er entfällt mit ihr (**A‑1**).

### 3.2 Die Steuerelement-Ausleihe und ihre sechs Sonderbehandlungen (Risiko R‑W11‑1)

Die Ausleihe war der Grund für sechs Sonderbehandlungen im Quelltext. Jede ist
einzeln geprüft — keine ist mitgewandert:

| Sonderbehandlung | Wo | Was daraus wird |
|---|---|---|
| `_kesselChartAktiv`, `_bhkwChartAktiv` | :65, :85 | Ein Blatt, das nicht vorn steht, wird in Blazor **gar nicht gezeichnet** (`Reiterblatt`). Kein Ersatzmerker nötig. |
| `_bedarfKanalDa[3]`, `_bedarfKanalImAufbau` | :1712, :1725 | Präsenz steht im DTO (`BedarfDaten.KanalDa`), die Ereignissperre entfällt: Ein Modellfeld löst kein Ereignis aus. |
| feste Positionen ohne Rechtsanker | :616–618, :497–499 | CSS-Raster. |
| `SpSeiteEinpassen` und Geschwister | :7098–7158 | rund 120 Zeilen Einpassung, ersatzlos. |
| `InitTextBoxen` (nur direkte Kinder, Befund W11‑B7) | :5124 | Ein neuer Lauf liefert ein neues DTO; es gibt nichts zu leeren. |
| `VereinheitlichePageSchriftarten` (Befund W11‑B4) | :5345 | Die Schrift steht in `epos-ui.css`, einmal. |

### 3.3 Der Lauf — nebenläufig, mit Balken und Abbrechen

Der Ablauf ist der aus W11a.4, nur jetzt in der Hülle statt in der Maske:

| Faden | Was |
|---|---|
| **Bedienfaden** | Schemasperre, `KonfigurationCtrl.ProjektLesen`, `SimulationLaufCtrl.Vorpruefen`, `…Bedarf`, `…Bestuecken` (der letzte Datenbankzugriff vor dem Lauf) |
| **Hintergrund** | ausschließlich `SimulationLaufCtrl.Laufen` in `Task.Run` |
| **Marshalling** | `Progress<LaufFortschritt>`, auf dem Bedienfaden erzeugt |

Der **Automatikstart beim Öffnen** (Befund W11‑B48) bleibt wörtlich: Ist das
Projekt gesperrt, geht es ohne Lauf auf die „Übersicht"; sonst läuft die
Simulation an, und die sichtbare **Endlage ist ebenfalls die „Übersicht"** —
genau wie vor W11a. Der `_laufAusLoad`-Merker des Vorläufers wird dabei
gegenstandslos: Die Seite wählt das Blatt **nach** dem `await`, nicht davor.

Die Zustandsmaschine „Ergebnis speichern" (Nacharbeit Paket 8, Befund N1) steht
unverändert: `_ergebnisGueltig = false` als **erste** Anweisung des Laufs, `true`
erst nach `Abbruchgrund == null`. Jeder Frühausstieg lässt den Knopf gesperrt
zurück.

### 3.4 Siebzehn Zeichenflächen, sieben Bilder — und wann sie entstehen

Die sieben Renderer-Bilder aus W11a.6 decken alle siebzehn Flächen:

| Fläche | Bild |
|---|---|
| `ueb_chart` (Torte) | `Kuchen` |
| die zwei GDI-Donuts der `NavigatorUebersicht` | `Ring` (B5) |
| `chart1`, `chart2` | `GanglinieNormiert` (B1) |
| `chart3`, `chart_Kessel`, `chart8`, `chart_BHKW_Waerme`, `chart_Waerme`, `chart7`, `chart_PV` | `ErzeugerStapel` (B2, mit B3 als zweiter Achse) |
| `chart4` | `Streuwolke` (B4) |
| `chart_Speichertemperatur` | `Temperaturverlauf` (B7) |
| `chartSolar` (Dashboard) | `MonatsStapel` (B6) |
| `chart6`, `chart_Speicher` | `Jahresverlauf` (vorhanden) |

**Gerendert wird erst beim Betreten eines Reiters** und dann je
Schalterstellung zwischengespeichert (`Bildauftrag.Schluessel` trägt Bild,
Sortierung, Kanal, Zahl und Reihenliste). Zwölf PNG je Lauf im Voraus zu rechnen
wäre zu teuer (Risiko der Vermessung § 11.5); ein Testfall hält fest, dass ein
zweites Betreten desselben Reiters **keinen** neuen Auftrag an die Hülle stellt.

---

## 4. Feldkarten-Abgleich (Risiko R‑W11‑7)

Die Karten der sechs Masken wurden vor Wellenbeginn **neu gezogen** (Stand
`81a04ec`) — die Karten der Vermessung stammen von `aef9509`, und W11a hat die
Masken seither umgehängt. Der Abgleich lief **je Reiter**, nicht je Maske; für
`Form_Simulation_Detail` ist die Karte ohnehin nur die halbe Wahrheit (Befund
W11‑B2: 58 unmittelbare `new`-Stellen plus zwölf Fabrikmethoden mit 67 Aufrufen
bauen rund 130 Steuerelemente zur Laufzeit).

| Reiter | Karte (Designer) | zusätzlich zur Laufzeit | Seite | Anmerkung |
|---|---|---|---|---|
| R1 Parameter | 2 (`tabControl_Einstellungen`, `tabControl3`) | 29 auf P3 | 5 Unterblätter, P3 mit 18 Feldern | `tabControl3` ist tot (W11‑B50) und nicht portiert |
| R2 Übersicht | 42 (1 Chart, 28 Label, 13 TextBox) | — | 13 Wertzeilen, 1 Kuchen, 2 Ringe, 2 Kacheln, 1 Raster | Zeilen nach Präsenz |
| R4 Bedarf | 24 | 12 (Kanalzeilen und -schalter) | 6 Wertzeilen, 3 Kanalzeilen, 5 Schalter, 2 Bilder, 3 Knöpfe | |
| R5 Wärmepumpe | 33 | 3 Blöcke (Puffer, Erdreich, Temperaturblatt) | 10 Wertzeilen, 2 Raster, 1 Banner, 3 Unterblätter | |
| R6 Heizkessel | 57 | Chart, Umschalter, Exportknopf, Quellwärmezeile | 9 Wertzeilen, 10 Brennstoffzeilen, 1 Raster, 1 Bild | |
| R7 Solarthermie | 16 | `listView_SimSolar` | 5 Wertzeilen, 1 Raster, 1 Bild | |
| R8 BHKW | 37 | 3 Kennzahlzeilen, 10 Brennstoffzeilen | 15 Wertzeilen, Brennstoffblock, 1 Raster, 1 Bild | |
| R9 Photovoltaik | 15 | `listView_SimPV` | 6 Wertzeilen, 2 Schalter, 1 Raster, 1 Bild | |
| R10 Stromspeicher | **0** | die ganze Seite | Kopf, 12 Kacheln, 1 Bild, 39 Kennzahlzeilen, Ampel, 2 Knöpfe | |
| R11 Ergebnis | **0** | `TabNavigationManager` | innerer Reiter mit 4 Blättern | |
| `DashboardForm` | 12 | — | 3 Kacheln, 2 Balken, 1 Zahlenfeld, 1 Bild | |
| `NavigatorUebersicht` | 4 | — | in R2 aufgegangen | |
| `NavigatorStrom` | 8 | 1 Exportknopf | 1 Schalter, 1 Mehrfachauswahl, 1 Bild, 1 Knopf | Sortiert ist NEU (W11‑B41) |
| `NavigatorWaerme` | 8 | 6 | 3 Schalter/Auswahl, 2 Mehrfachauswahl, 1 Bild, 1 Knopf | |
| `Form_SpeicherVariantenVergleich` | 9 (ListView mit 12 Spalten) | — | 12-spaltige Tabelle, Protokoll, 3 Knöpfe | |

---

## 5. Abweichungen (mit Begründung)

| # | Was | Warum |
|---|---|---|
| **A‑1** | **Zehn Blätter statt elf.** R3 „Simulation" ist nicht portiert | Er war kein Fachreiter, sondern der Behälter der Menüliste mit der Steuerelement-Ausleihe (§ 3.1, Befund W11‑B11). Seine acht Fachreiter sind jetzt Blätter derselben Leiste |
| **A‑2** | **Befund W11‑B14 behoben:** `chart2` hat EINE Fülllogik | Der Vorläufer füllte es auf zwei Wegen mit zwei Schrittweiten — dieselbe Schalterstellung ergab zwei verschiedene Bilder. Jetzt trägt ein Bildauftrag die Schalterstellung, und der Renderer untertastet selbst |
| **A‑3** | **Befund W11‑B18 behoben:** der Heizstab ist in beiden Zweigen derselbe Anteil | Sortiert lief er als kumulierte Kurve „WP-Produktion + Heizstab", chronologisch als eigener Anteil — zwei Größen unter demselben Serienschlüssel und derselben Legende |
| **A‑4** | **Befund W11‑B36 behoben:** ohne Bedarf steht KEIN Ring | Der Vorläufer setzte den Mittelwert hart auf 100 — das Bild behauptete 100 % Deckung, wo es gar keinen Bedarf gibt. Jetzt steht dort der Satz `SIMERG_MSG_OHNE_BEDARF` |
| **A‑5** | **Befund W11‑B41 behoben:** der Stromgang bekommt einen Sortiertumschalter | Er fehlte als einziger; `ErzeugerStapel` kann es ohnehin |
| **A‑6** | **Befund W11‑B40 behoben:** BHKW-Strom bekommt eine eigene Farbe | Lastgangprofil und BHKW-Strom trugen beide `Color.Brown` — im Stapel unten und als Linie darüber nicht zu unterscheiden. Jetzt `SaddleBrown` für das BHKW |
| **A‑7** | **Zoom, Cursor und die zwei Maus-ToolTips entfallen** (Risiko R‑W11‑5) | Sie hingen an `chart1`/`chart2`, und die ToolTips waren fehlerhaft (Befund W11‑B13: Der Y-Wert stand in der FORMATZEICHENKETTE von `DateTime.ToString`). Für Einzelwerte bleibt der CSV-Export, für die Form der Umschalter Ganglinie/Dauerlinie |
| **A‑8** | Die 21 `MessageBox` werden **Warnbanner und Rückfragen** | Wie A‑10 aus den Wellen 9 und 10a: Die Meldungen bleiben **wörtlich**, nur der Träger ist ein anderer. Die Rückfrage des Variantenvergleichs bleibt eine Rückfrage (`Rueckfrage`, Ja/Nein) |
| **A‑9** | Die Laufmeldungen sind ein **anklickbares Banner** statt einer Zeile mit ToolTip | Der Volltext stand im ToolTip und beim Klick in einer `MessageBox` (:3862). Jetzt öffnet der Klick eine Überlagerung mit demselben Text als mehrzeiliges Feld — er ist damit **markierbar**, was ein ToolTip nie war |
| **A‑10** | Der **Optimierungsknopf** steht auf dem Parameterblatt (P3), nicht auf dem Stromspeicher-Reiter | Dort stand er im Vorläufer (`InitStromspeicherParameter` :5974–5989). Die Komponententabelle der Arbeitsanweisung nennt ihn beim Stromspeicher-Reiter; genommen ist die Fassung des Bestands (Regel „bei Unsicherheit wörtlich") |
| **A‑11** | Die Auslegungsoptimierung läuft über die **Sprungbrücke** mit ZWEI zusätzlichen Parametern | `Form_SpeicherOptimierung` ist das erste Brückenziel, das etwas braucht: den gerechneten Lauf. Und ihre Antwort heißt nicht „mit OK geschlossen", sondern `AuslegungUebernommen` — die Maske hat kein `DialogResult`. `Sprungbruecke.Fuer/Zeigen` nehmen deshalb `lauf` und `idProjekt` mit Vorgabewert; die bestehenden acht Ziele sind unberührt |
| **A‑12** | **`ChartManager` wird NICHT gelöscht** | Die Arbeitsanweisung sagt „prüfen: kein anderer Nutzer". Geprüft: `Form_Klimadaten` (2 ×) und `Form_PeakShaving` führen weiter interaktive WinForms-Charts. Gelöscht sind nur `DonutChartDrawer` und `Kacheln` — deren einziger Nutzer war `NavigatorUebersicht` |
| **A‑13** | `UebersichtReiter` erscheint in **zwei Rollen** | „R2 + `NavigatorUebersicht`" heißt: dieselbe Anzeige an zwei Stellen (Hauptreiter und erstes Blatt des Ergebnisreiters). Zwei Fassungen wären zwei Wahrheiten — dieselbe Überlegung wie beim `PufferSpProjektDialog` in drei Rollen (W10a) |
| **A‑14** | Der `ErgebnisReiter` nimmt Wärmegang, Stromgang und den Navigatorteil als **`RenderFragment`** herein | Er ist die Navigation und die Autarkie-Analyse; wer die drei Fremdinhalte verdrahtet, ist die Seite. So bleibt er prüfbar, ohne die halbe Datenseite zu kennen |
| **A‑15** | Die Kapazität der Autarkiekachel **nennt sich jetzt flüchtig** | Befund W11‑B32: Sie wurde nie zurückgeschrieben, und der Vorläufer sagte das nirgends. Der Wert und sein Rückfall (5 kWh, jetzt in `StromspeicherStammCtrl.KapazitaetJeProjekt`) sind unverändert; darunter steht der Satz `SIMERG_LBL_KAPAZITAET_HINWEIS` |
| **A‑16** | Kein KI-Aufrufknopf, keine Pixelarithmetik, keine Laufzeit-Steuerelemente | Wie A‑12 aus Welle 10b: Der KI-Einstieg hat in `EPOS.UI` noch keinen Baustein (W15b); die rund 130 Laufzeit-Steuerelemente und die rund 400 Zeilen Geometrie erledigen Hülle und CSS |
| **A‑17** | Die **elf Reitertitel** haben erstmals eine englische Fassung | Befund W11‑B29: Von 20 `TabPage` hatten nur drei eine. Sechs neue Schlüssel (`SIMERG_TAB_*`), die übrigen über die vorhandenen `SIM_ERZEUGERNAME_*`/`SIM_PHOTOVOLTAIK`/`SIM_STROMSPEICHER`/`SIM_ERGEBNIS` |
| **A‑19** | **Der Ergebnisblock führt die DECKUNG je Erzeuger statt der Produktion, und die Restwärme ist EINE Zahl** | **Anwenderentscheid 04.09.2026** zu W11a‑O‑1 (§ 5a). Zahlenwirkung: 1030 von −1,76 auf **0,00 MWh**; 1007 und 1017 unverändert. Auch das Eigenanteilsraster zeigt in der Spalte „Ergebnis [MWh/a]" jetzt die Deckung — sie ist damit die Summe ihrer drei Kanalspalten, was sie vorher nicht war |
| **A‑18** | Die **Umlautschlüssel** entfallen | Befund W11‑B30: `"tabPage_Wärmepumpe"` und `"tabPage_Wärmepumpe_Parameter"` waren Steuerschlüssel mit Umlaut. Die neuen heißen `WAERMEPUMPE` und stehen in `ParameterBlatt` bzw. `SimulationErgebnisSeite.Blatt` |

### 5a — Der Anwenderentscheid zu W11a‑O‑1 (04.09.2026)

W11a hatte den Ergebnisblock auf die Fassung „Produktion **mit** BHKW-Term"
gestellt (Befund W11‑B35) und dabei einen offenen Punkt hinterlassen: Für
Projekt 1030 ergab `Bedarf − Produktion` **−1,76 MWh**, weil „Produktion" nicht
„Deckung" ist — geladene Speicherwärme steht in der Produktion und deckt
trotzdem keinen Bedarf.

**Der Anwender hat entschieden:** Der Restwärmebedarf ist in **beiden** Ansichten
derselbe Wert, das BHKW zählt mit, und eine **negative Restwärme darf rechnerisch
nicht entstehen** — sie zeigt eine falsche Zuordnung zu den Erzeugern. Also nicht
klemmen, sondern richtig rechnen.

Umgesetzt in `SimulationErgebnisCtrl.Uebersicht` (Signatur unverändert):

* Die sechs Summen führen die **DECKUNG** je Erzeuger — Direktdeckung plus
  zugerechnete Speicherentladung, je Kanal, genau die Summanden, aus denen
  `NavigatorUebersicht.FillTableWithData` über `SimulationRunner.Summiere` seine
  drei Kanalspalten bildete. Der Heizstab behält seine eigene Zeile.
* `RestwaermebedarfMwh = RestwaermeMwh = sim.Restwaerme` — die Bilanzgröße des
  Laufs, gespeichert als `Tab_Ergebnis.Waermerestbedarf`. Damit gilt
  `Bedarf − Summe Deckung = Restwärme ≥ 0` **per Konstruktion**.
* Übersteigt die Produktion eines Erzeugers seine Deckung, ist das ein
  **Überschuss** (Feld `Wärmeüberschuss`, wie beim BHKW) — nicht Restwärme.

**Zahlenabzug** (`EPOS.Kern.Tests/W11bZahlenabzug.cs`, drei Fälle):

| Projekt | Wärmebedarf | Summe Deckung | Restwärme |
|---|---:|---:|---:|
| 1030 | 6 137,56 | 6 137,56 | **0,00** (vorher −1,76) |
| 1007 | 56,90 | 50,85 | **6,04** (unverändert) |
| 1017 | 62,91 | 62,91 | **0,00** (unverändert) |

Bedarf minus Deckung trifft die Bilanzgröße in allen drei Projekten **exakt**.
Der Referenzlauf ist unberührt: `SimulationRunner.BaueErgebnis` schreibt
unverändert `sim.Restwaerme` — die Änderung betrifft ausschließlich die Anzeige.

---

## 6. Texte

**79 neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`,
`Resource.en-US.resx` und — von Hand, weil hier kein Visual Studio läuft —
`Resource.Designer.cs`:

* **76 × `SIMERG_*`**: die Feldbeschriftungen, Reitertitel, Knopftexte, der
  BHKW-Erläuterungstext und vier Sätze, die es vorher nicht gab
  (`SIMERG_MSG_OHNE_BEDARF`, `SIMERG_LBL_KAPAZITAET_HINWEIS`,
  `SIMERG_LAUF_LAEUFT`, `SIMERG_LBL_SERIENAUSWAHL`). Die deutschen Texte stehen
  **wörtlich** wie im Vorläufer; die englischen kommen aus
  `Form_Simulation_Detail.en-US.resx`, soweit sie dort standen (70 `.Text`),
  sonst neu übersetzt.
* **3 × `SIMDET_BHKW_*`**: Sie lagen in der Form-`.resx` und wurden über einen
  **zweiten** `ResourceManager` gelesen (Befund W11‑B21, `TextAusFormResx`
  :2148–2174). Sie stehen jetzt im gemeinsamen Katalog.

**Probe:** `Resource.resx` und `Resource.en-US.resx` führen je **3 912**
Einträge, **0 Dubletten**, **0 Schlüssel nur in einer Sprache**.

**Nicht übersetzt sind die Steuerwerte:** die Erzeuger-DB-Werte
(`DbWerte.ERZEUGER_*`), die Betriebs- und Berechnungsarten des Speichers
(`DbWerte.SP_*`), die Preisquellen und die Serienschlüssel der Diagramme
(`GESAMT`, `KANAL_0`, `PUFFER_1018023`, …). Drei-Schichten-Regel.

**Hartkodierte deutsche Zeichenketten im Vorläufer: 6** — alle erledigt.
`"Monat"` (:4776, **sichtbarer** Achsentitel, Befund W11‑B12) setzt jetzt der
Renderer; `"MWh/a"` (:7676) und `"Komponente"` (:2900, unsichtbarer Spaltenkopf)
sind Einheit bzw. entfallen mit der Menüliste; die drei Umlautschlüssel siehe
A‑18. Der Hilfebereich `"Detaillierte Simulation"` (:436) stand **neben** dem
Katalogeintrag `HilfeKontext.cs:159` — jetzt gibt es ihn einmal, in der Hülle
(Befund W11‑B5).

**`help_mapping.txt` bleibt unverändert.** Beide `btn_Help`-Zeilen behalten ihren
Nutzer: Die Seite trägt `Form_Simulation_Detail.btn_Help` als `HilfeSchluessel`
ihres `InfoKnopf`, die Vergleichsüberlagerung
`Form_SpeicherVariantenVergleich.btn_Help` — der Schlüssel benennt die Wikiseite,
nicht die Klasse (Regel seit W10a). Feldzeilen gab es für diese Masken keine.

**`HilfeKontext.cs`** verliert die **sechs** Einträge der gelöschten Masken
(Regel F10); der Bereich `B_SIM_DETAIL` bleibt und wird von der Hülle beim
Aktivieren des Fensters gemeldet.

---

## 7. Die fünfzig Befunde der Vermessung

Kompakt gruppiert; „W11a" heißt: bereits in Welle 11a erledigt.

**Behoben in dieser Welle (10):** B11 (drei Navigationen → eine), B12 (der
hartkodierte Achsentitel „Monat"), B13 (die zwei fehlerhaften Maus-ToolTips),
B14 (zwei Fülllogiken für `chart2`), B18 (der Heizstab als zwei verschiedene
Größen), B21 (der zweite `ResourceManager`), B29 (elf Reitertitel ohne
Englisch), B30 (Umlaute in Steuerschlüsseln), B36 (Ringmittelwert hart auf 100),
B40/B41 (gleiche Farbe für zwei Reihen; fehlender Sortiertumschalter).

**Behoben in W11a, hier nur noch Anzeige (9):** B15, B16, B19, B20, B22, B24,
B31, B42, B45. **B35** (die sechs Summen standen zweimal) ist in W11a behoben und
in dieser Welle durch den Anwenderentscheid **neu beantwortet**: nicht Produktion
mit BHKW-Term, sondern Deckung (§ 5a, A‑19).

**Entfallen ersatzlos mit dem Port (19):** B1 (fünf statt vier Unterreiter — die
Zahl steht jetzt in `ParameterBlatt`), B2 (130 Laufzeit-Steuerelemente),
B4 (`VereinheitlichePageSchriftarten`), B5 (doppelte Wahrheit des
Hilfebereichs), B6 (drei Regeln für eine Präsenzfrage — die drei Zweige sind
wörtlich in die Hülle gewandert und dort **eine** Zeile), B7 (`InitTextBoxen`),
B8 (Ereignisrückkopplung der Betriebsart), B10 (vier leere Handler),
B17 (40 Zeilen totes Programm), B23 (totes Feld `simulation_wp`), B25 (elf
`ChartManager`, fünf belegt), B26 (leeres `SetControls`), B27/B28 (die zwei
Satelliten-`.resx` samt Vorlagenresten und verschobenen Steuerelementen),
B33 (Arial im Achsentitel), B34 (zweimal gesetzte Rasterschrift), B37 (toter
`ContainsKey`-Block), B38 (drei Wege für `SetControl`), B39 (fester Bildpunkt),
B43/B44/B46/B47 (die vier Befunde des `TabNavigationManager`), B48 (der
synchrone Automatikstart), B50 (`tabControl3`).

**Wörtlich übernommen (3):** B3 (die Bedarfsobjekte gehören dem Aufrufer — sie
sind Eingang und Ausgang zugleich, und genau deshalb bleibt die Hülle modal),
B32 (die Was-wäre-wenn-Kapazität; jetzt benannt, A‑15), B49 (die 14
`Console.WriteLine` in `catch`-Zweigen — siehe W11b‑O‑1).

---

## 8. Nachweise

### 8.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64
→ 0 Fehler, 12 Warnungen
```

**Unverändert 12.** Die sechs gelöschten Masken trugen keine WFO1000-Fundstelle;
die Aufteilung bleibt 6 WFO1000, 2 CS0108, 2 CS0109, 1 WFO0003, 1 CA2255.
`dotnet build EPOS.UI -c Release` → 0 Fehler, **0** Warnungen.

### 8.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ KiKern.Tests          450 grün
  SpeicherEngine.Tests   337 grün
  EPOS.Kern.Tests        379 grün   (+4 aus Welle 11b.11)
  EPOS.UI.Tests        1 448 grün   (+108 aus Welle 11b)
  zusammen             2 614 grün, 0 rot
```

**Beide Sprachen** (Regel seit Welle 8):

```
LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 dotnet test WP-Plan.Kern.slnf -c Release
→ dieselben 2 614 grün, 0 rot
```

**112 neue Fälle**, je Komponente eine Probendatei:

| Datei | Fälle |
|---|---|
| `Seiten/ParameterReiterTests.cs` | 11 |
| `Seiten/UebersichtReiterTests.cs` | 10 |
| `Seiten/BedarfReiterTests.cs` | 8 |
| `Seiten/WaermepumpeReiterTests.cs` | 10 |
| `Seiten/ErzeugerReiterTests.cs` (R6–R9) | 19 |
| `Seiten/StromspeicherReiterTests.cs` | 10 |
| `Seiten/GangUndErgebnisReiterTests.cs` | 14 |
| `Seiten/SpeicherVariantenVergleichTests.cs` | 10 |
| `Seiten/SimulationErgebnisSeiteTests.cs` | 15 |
| `Dialoge/SprungzielTests.cs` (erweitert) | +1 Zählwert |
| `EPOS.Kern.Tests/W11bZahlenabzug.cs` (Anwenderentscheid) | 3 |
| `EPOS.Kern.Tests/SimulationErgebnisCtrlTests.cs` (erweitert) | +1 |

**Die Sprache ist in jeder Probendatei festgelegt** — und wo Zahlen geprüft
werden, zusätzlich die Zahlenkultur: Die Beschriftungen folgen
`CurrentUICulture`, die Formatierung `ToString("F2")` folgt `CurrentCulture`, und
das tat der Vorläufer genauso.

### 8.3 Formularkarte

```
dotnet test Werkzeuge/Formularkarte.Tests -c Release
→ 123 grün (auch unter LANG=en_US)

dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1 --erreichbarkeit
→ Designer-Dateien 44, davon Masken 43, lokalisiert 27, Kartenzeilen 671,
  erreichbar 42, unerreichbar 0, verwaist 0, unklar 1
```

**49 → 43 = −6.** Genau die sechs Masken der Welle; die vier Steuerklassen hatten
nie einen Designer. **28 → 27:** `Form_Simulation_Detail` war die einzige
lokalisierte der Welle.

### 8.4 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 233 SQL-Texte geprueft: 0 Fundstellen, 171 dynamisch, 1 062 in Ordnung
```

**Unverändert 1 233.** Die Welle bringt keine neue SQL mit — die elf
inline-Anweisungen der Masken sind schon in W11a in Kern-Controller gezogen.

### 8.5 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 30 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

**Unverändert 30.** Die Welle bringt kein neues Renderer-Bild — die sieben aus
W11a decken alle siebzehn Zeichenflächen.

### 8.6 Referenzlauf

```
dotnet run --project EPOS.Referenzlauf -c Release -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w11b
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/w11b
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

**Byte-gleich, nicht nur innerhalb der Toleranz** — wie es die Welle verlangt.
Sie fasst den Rechenweg nicht an: Was gerechnet wird, stand nach W11a schon im
Kern.

### 8.7 Alles noch einmal auf dem zusammengeführten Stand

`origin/ios_migration` ist seit der Basis `81a04ec` um **einen** Commit gewachsen
(`da420f3` — die Anwenderentscheide zu W10b‑O‑3, W11a‑O‑1 und W11a‑O‑2 in den
Protokollen). **Ein Konflikt**, in `iU9_W11a_Kern_Protokoll.md` unter W11a‑O‑1:
Beide Seiten haben denselben Entscheid vermerkt — `origin` als Kurztext im
Wortlaut des Anwenders, W11b als ausführlichen Block mit den gemessenen Zahlen
der Umsetzung. **Beide bleiben**, in dieser Reihenfolge: erst der Entscheid, dann
seine Umsetzung.

| Tor | Ergebnis |
|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 12 Warnungen |
| `dotnet test WP-Plan.Kern.slnf -c Release` | 2 614 grün, 0 rot |
| dasselbe mit `LANG=en_US.UTF-8` | 2 614 grün, 0 rot |
| `dotnet test Werkzeuge/Formularkarte.Tests` | 123 grün |
| Stapellauf Formularkarte | 44 Designer, 43 Masken, 27 lokalisiert, 42 erreichbar, 0 unerreichbar, 0 verwaist, 1 unklar |
| SQL-Dialektprüfer | 1 233 Texte, 0 Fundstellen |
| ChartProben | 30 Bilder, 0 Verstöße |
| Referenzlauf 1030/1007/1017 | PASS, 815 043 Werte, **alle drei byte-gleich** |
| iU5-Wächter (`Program.*`) | leer |
| Plattform-Wächter (WinForms/Drawing/OleDb im Kern) | leer |

### 8.8 Keine Typverwendung ist übrig

```
grep -rnE "(new|typeof|:)\s*(Form_Simulation_Detail|DashboardForm|NavigatorUebersicht|
    NavigatorStrom|NavigatorWaerme|Form_SpeicherVariantenVergleich|TabNavigationManager|
    TabListMapper|GanglinienDarstellung|SchluesselEintrag|DonutChartDrawer)\b"
    --include=*.cs --include=*.razor .
→ 1 Treffer, und der ist ein Kommentar (HilfeKontext.cs:156)
```

Restfundstellen der alten Namen sind ausschließlich (a) die beiden
`HilfeSchluessel`-Zeichenketten, (b) Kommentare, die die Herkunft einer Regel
nennen, und (c) der datierte Erreichbarkeitsbericht.

---

## 9. Grenzen

* **Am Gerät ungeprüft.** Alles hier ist ohne Windows entstanden. Die
  Abnahmeliste in § 10 ist der Prüfplan.
* **Ohne Bildschirmfotos des Bestands.** Die Arbeitsanweisung verlangte je
  Zeichenfläche ein Foto vor Beginn; ohne Windows war keines zu bekommen. Als
  Ersatz stehen die dreißig `ChartProben`-Bilder (sie prüfen Maße, Farben und
  Determinismus der sieben Renderer-Bilder byte-genau) — **was daraus nicht
  folgt: ob die siebzehn Flächen genauso AUSSEHEN wie vorher.** Das ist
  Abnahmepunkt 4 und ist am Windows-Gerät nachzuholen.
* **Die Seite ist der zweite Blazor-Wirt mit vielen Kindern** — vier
  Überlagerungen (Konfiguration, Bedarfsergebnis, Wärmepumpendialog,
  Variantenvergleich), davon eine, die selbst eine Seite ist. Ob das auf einem
  älteren Gerät flüssig bleibt, ist Abnahmepunkt 1.
* **Der Projektwechsel über `SeitenZustand` ist unter Windows unbenutzt** — die
  modale Hülle setzt das Projekt einmal. Erst iOS und W16 fahren den Weg.
* **iOS erreicht die Seite über `AppWurzel`, aber `IosProjektQuelle` liefert
  ihren Parametersatz noch nicht** (Standardumsetzung `null` → die Liste bleibt
  stehen und sagt warum). Genau derselbe Stand wie bei der
  Simulationskonfiguration nach W10b; der Ergebnisseite fehlt dort ein
  gerechneter `SimulationControl`, und den gibt es auf iOS bis heute nicht.

---

## 10. Abnahmeliste Windows (iZ5)

Grundsätzlich je Ansicht: öffnet mittig, kein weißes Aufblitzen, ziehbar und
maximierbar, de **und** en (`HKCU\Software\wp-plan\Language`), Hochkontrast,
125 % und 150 % scharf, Maus **und** Finger (44 px), Tab-Zyklus bleibt im
Fenster, Esc schließt die oberste Ebene, Infoknopf zeigt die Wikiseite.

| # | Aufrufweg | Was besonders zu prüfen ist |
|---|---|---|
| 1 | **Startbild → Kachel „Detaillierte Simulation"** | Die Seite öffnet in einem modalen Fenster (R‑W11‑1), 1 474 × 821; der Lauf startet von selbst, der **Fortschrittsbalken** ist sichtbar, das Fenster bleibt bedienbar, die Titelzeile meldet **kein** „Keine Rückmeldung". Die Zeit bis zum ersten Bild messen |
| 2 | **Abbrechen** während des Laufs | Der Knopf wirkt (spätestens an der nächsten Phasengrenze), „Ergebnis speichern" bleibt gesperrt, ein zweiter Lauf geht durch |
| 3 | **Nach dem Automatikstart** | Die „Übersicht" steht vorn — die Endlage wie vor der Welle |
| 4 | **Alle zehn Reiter, je Ausprägung mit und ohne Erzeuger** | Dieselben Zahlen wie vor W11b (die drei Zahlenänderungen aus W11a bleiben: Restwärme mit BHKW, PV-Deckungsgrad 0,00 statt NaN, Mindest-Spitzenkesselleistung). **Die siebzehn Zeichenflächen gegen ein Bildschirmfoto des Bestands halten** — der offene Punkt aus § 9 |
| 5 | **Reiterleiste nach `Tool_1..6`** | Ein Projekt ohne BHKW hat kein BHKW-Blatt; die Parameter-Unterblätter stehen in der Reihenfolge von `Tool_1..6`, „Bedarf" immer zuerst |
| 6 | **Parameterseite** | Jedes Feld schreibt SOFORT (kein Speichernknopf); Ladeleistung und Kapazität sind sichtbar und gesperrt; ohne aktive Variante sind die Speicherfelder Attrappen und die Fußzeile sagt warum |
| 7 | **Konfiguration als Überlagerung** (W10b) | Sie öffnet **im selben Fenster**, Esc schließt nur sie, und danach steht die Ergebnisseite auf demselben Blatt — mit neu gelesener Reiterleiste, wenn sich `Tool_1..6` geändert hat |
| 8 | **Doppelklick auf eine WP-Modulzeile** | Der Wärmepumpendialog (W7) als Überlagerung; nach „Übernehmen" sind die Anlagen des Projekts neu geschrieben |
| 9 | **„Details…" auf Bedarf und Übersicht** | Der Bedarfsergebnisdialog (W8) als Überlagerung, Wärme mit Brauchwasser und Startreiter 1 |
| 10 | **Stromspeicher-Reiter** | Zwölf Kacheln, 39 Kennzahlzeilen in drei Gruppen, die Zyklenzeile gefärbt wie zuvor, die Vergleichsspalte nur mit Vergleichslauf, die Ampel mit ihren beiden Vorsätzen |
| 11 | **Variantenvergleich** (nur ab zwei Varianten) | Der Fortschritt zählt ECHT („3 / 7"); beste Zeile grün, aktive fett, nicht rechenbare in Firebrick mit Grund im Mouseover; „Als aktiv setzen" fragt zurück und rechnet danach NICHT neu |
| 12 | **Auslegungsoptimierung** (Parameterblatt Stromspeicher) | Die WinForms-Maske erscheint modal über der WebView (Risiko R1); nach „Übernehmen" liest die Parameterseite die Variante neu, ohne zu rechnen |
| 13 | **Die sechs CSV-Exporte** | Bedarf, Wärmepumpe, Heizkessel, Stromspeicher, Wärmegang, Stromgang. Der Wärmegang ist **immer chronologisch**, auch bei „sortiert"; er nimmt nur die angehakten Reihen mit |
| 14 | **Ergebnis speichern** | Nur nach einem vollständigen Lauf; danach zeigt die Startmaske die neuen Kachelzahlen |
| 15 | **Sperrzustand** | Mit einem Projekt auf halb migriertem Schema öffnen: Der Grund steht als Banner, alles ist gesperrt, „Beenden" muss trotzdem gehen |
| 16 | **Sprache auf en umstellen** und 1–15 stichprobenartig wiederholen | Die elf Reitertitel sind jetzt englisch (A‑17); die Steuerwerte (Erzeuger, Betriebsart, Berechnungsart, Preisquelle) dürfen sich **nicht** mit übersetzen |
| 17 | **iOS-Job** (`Actions → iOS → Run workflow`) | Die Seite ist die zweite Fachseite für `AppWurzel`. Der Job baut `EPOS.iOS` gegen die erweiterte `IProjektQuelle`; die Standardumsetzung muss ihn tragen, ohne dass `IosProjektQuelle` angefasst wurde |

---

## 11. Offene Punkte

| # | Was | Vorschlag |
|---|---|---|
| **W11b‑O‑1** | **Die 14 `Console.WriteLine` in `catch`-Zweigen** (Befund W11‑B49) sind wörtlich mitgewandert: Preisreihen, Preisvorschau, Speichervariante lesen und schreiben, Startmaske auffrischen, Bild zeichnen. Der Anwender sieht sie nicht | **Frage an den Anwender:** Welche davon gehören als Warnbanner auf die Seite? Der Kandidat mit der größten Wirkung ist „die Speichervariante konnte nicht geschrieben werden" — dort geht eine Eingabe still verloren |
| **W11b‑O‑2** | **Die siebzehn Flächen sind ohne Bildvergleich portiert** (§ 9). Der GDI+-Bildvergleich ist mit iF23 gelöscht, ein Foto des Bestands war ohne Windows nicht zu bekommen | Abnahmepunkt 4: am Gerät gegen ein Foto der letzten WinForms-Fassung halten. Die Bilder selbst sind in `ChartProben` byte-genau geprüft und ändern sich dabei nicht |
| **W11b‑O‑3** | **`ErgebnisReiter` zeigt vier Blätter, obwohl `NavigatorUebersicht` in R2 aufgegangen ist** (A‑13/A‑14): Das erste Blatt zeigt denselben Navigatorteil wie der Hauptreiter „Übersicht" | **Frage an den Anwender:** Braucht es das erste Blatt noch? Der Vorläufer hatte vier Knöpfe, weil die Übersicht dort ihr einziges Zuhause hatte; heute steht sie zusätzlich als Hauptreiter |
| **W11b‑O‑4** | **`Form_SpeicherOptimierung` ist die letzte WinForms-Maske im Simulationsbereich.** Sie bleibt (iF22, ScottPlot), wird aber jetzt aus einer WebView heraus geöffnet | Risiko R1: Am Gerät prüfen, ob sie wirklich modal über der WebView liegt und die Seite danach unverändert dasteht |
| **W11b‑O‑5** | **iOS erreicht die Seite, bekommt aber keinen Parametersatz** (§ 9). Ihr fehlt dort ein gerechneter `SimulationControl` | Mit dem Paket, das den Lauf auf iOS bringt. Bis dahin ist der Zustand „die Liste bleibt stehen und sagt warum" der richtige |
| **W11b‑O‑6** | Der KI-Aufrufknopf fehlt (A‑16) | Mit W15b, wenn `Gespraechsverlauf` steht — wie W6‑O‑6 bis W10b‑O‑6 |

**W11a‑O‑1 ist mit dieser Welle GESCHLOSSEN** (§ 5a, A‑19) — der Anwender hat am
04.09.2026 entschieden.

**Offen aus W11a, hier wörtlich gelassen:** W11a‑O‑2 (die zwei CO₂-Faktoren),
W11a‑O‑3 (Zusammenführung der Berichtsbilder), W11a‑O‑5 (die Netzverluste sind
faktisch 0 % — Bedingung des Referenzlaufs).

---

## 12. Geänderte und neue Dateien

**Neu in `EPOS.UI`** (14): `Seiten/Simulation/SimulationErgebnisDaten.cs`,
`…/SimulationErgebnisSeite.razor`, `…/ParameterReiter.razor`,
`…/UebersichtReiter.razor`, `…/BedarfReiter.razor`, `…/WaermepumpeReiter.razor`,
`…/HeizkesselReiter.razor`, `…/SolarthermieReiter.razor`, `…/BhkwReiter.razor`,
`…/PhotovoltaikReiter.razor`, `…/StromspeicherReiter.razor`,
`…/ErgebnisReiter.razor`, `…/WaermegangReiter.razor`, `…/StromgangReiter.razor`,
`…/SpeicherVariantenVergleich.razor`.
**Geändert in `EPOS.UI`** (4): `Seiten/AppWurzel.razor`,
`Seiten/Seitenschluessel.cs`, `Dienste/IProjektQuelle.cs`,
`Dialoge/Allgemein/Sprungziel.cs`, `wwwroot/epos-ui.css`.

**Neu in der Anwendung** (4): `Views/Simulation/SimulationErgebnisHuelle.cs`,
`…Anzeige.cs`, `…Bilder.cs`, `…Wege.cs`.
**Geändert in der Anwendung** (5): `Views/Hauptformular/Form_Start.cs`,
`Views/Simulation/SimulationKonfigHuelle.cs` (statischer `Gaben`-Weg),
`Allgemein/Blazor/Sprungbruecke.cs`, `Allgemein/GrafikTools/ChartManager.cs`,
`Allgemein/KI/HilfeKontext.cs`.
**Gelöscht in der Anwendung** (25 Dateien).

**Geändert im Kern** (4): die drei Ressourcendateien und
`Controller/SimulationErgebnisCtrl.cs` (Anwenderentscheid, § 5a).

**Neu in den Tests** (10 Probendateien, dazu `EPOS.Kern.Tests/W11bZahlenabzug.cs`).
**Geändert in den Tests** (4): `EPOS.Kern.Tests/SimulationErgebnisCtrlTests.cs`, `EPOS.UI.Tests/Dialoge/SprungzielTests.cs`,
`Werkzeuge/Formularkarte.Tests/StapelTests.cs`,
`Werkzeuge/Formularkarte.Tests/ErreichbarkeitTests.cs`; dazu
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md`.
