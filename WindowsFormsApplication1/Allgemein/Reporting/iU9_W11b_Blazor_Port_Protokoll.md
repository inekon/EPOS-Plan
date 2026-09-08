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

## 10a. Nachtrag zur Windows-Abnahme vom 05.09.2026 — die drei Diagrammbefunde

Der Anwender hat die Seite am Gerät gesehen. Drei Befunde betreffen die
Diagramme; alle drei sind behoben.

| # | Befund (Wortlaut / Fundstelle) | Ursache | Behebung |
|---|---|---|---|
| **A‑1** | „Allgemein bei Charts: das Zoomen funktioniert nicht" | **Kein Fehler, ein bewusster Verlust.** § 5, A‑7 dieses Protokolls hat ihn festgehalten: Zoom und Cursor hingen an `chart1`/`chart2` und entfielen mit dem Wechsel vom `Chart`-Steuerelement zum PNG (Risiko R‑W11‑5). Der Anwender vermisst ihn — und zwar bei **allen** Diagrammen, nicht nur bei den zweien, die ihn hatten | Neuer Baustein `EPOS.UI/Bausteine/Diagramm.razor` mit `wwwroot/epos-diagramm.js`. `ChartBild` setzt sein `<img>` seither in diesen Rahmen; damit ist **jedes** Renderer-Bild zoombar. Dazu der **Datenzoom** für die Jahresganglinien (unten) |
| **W11b‑B‑2** | „Charts der Detaillierten Simulation zu klein" (S. 8): „Wärmelast Jahresganglinie" und „Strombedarf Jahresganglinie" sind briefmarkengroß, die Legende unleserlich | Die Bilder standen in einer **halben Spalte** von `.epos-simerg-spalten`. Ein 1 240 Bildpunkte breites Diagramm kam dort auf etwa 480 an — die Legende ist dann acht Bildpunkte hoch | **Eine** Stilblattregel: `.epos-simerg-diagrammzeile` spannt das Diagramm über ALLE Spalten des Rasters; die Zahlenblöcke bleiben nebeneinander. Angewandt auf Bedarf, Solarthermie, Photovoltaik, Stromspeicher, Übersicht und Wärmepumpe. Dazu `.epos-chartbild` auf `width: 100 %` statt `max-width` und `min-width: 0` auf den Rasterfeldern |
| **W11b‑B‑3** | „Wärmepumpen-Chart nicht korrekt" (S. 9): die x-Achse trägt „−18,2 … −5,3 … 7,7", das Bild ist rechts beschnitten und überlappt die Modultabelle, Legende und Achsentitel kleben an der Kante | Zwei Ursachen. Im **Renderer** (`ChartRenderer.Streuwolke`, B4): fünf Marken, die den vorkommenden Wertebereich in vier gleiche Teile schnitten — daher die krummen Zahlen; rechts nur 40 Bildpunkte Rand, auf denen die letzte Marke steht; Legende bei y = 66 und y-Achsentitel bei y = 86, deren Schriftzeilen sich berühren. Im **Markup**: dieselbe halbe Spalte wie bei B‑2 | Renderer: **runde Achsenteilung** (1 / 2 / 2,5 / 5 × 10^k, dieselbe Stufenfolge wie `Jahresgang`), Wertebereich auf diese Stufen aufgerundet, rechter Rand 90 statt 40, Legende auf y = 56, y-Titel auf `rc.Top − 26`, x-Titel vier Bildpunkte tiefer. Markup: `epos-simerg-diagrammzeile`. **Die Serien selbst waren richtig** — x = Außentemperatur, y = Leistung, die drei halbtransparenten Reihen und beide Achsentitel (`CHART_ACHSE_TEMPERATUR`, `SIM_SPALTE_LEISTUNG`) sind unverändert die des Vorbilds `chart4` |

### Der Zoom in zwei Stufen

**BILDZOOM — für jedes der 36 Renderer-Bilder.** Mausrad zoomt um den Zeiger,
Ziehen verschiebt, Doppelklick und Taste `0` stellen zurück, `+`/`−` zoomen über
die Tastatur, zwei Finger kneifen (iPad). Die Zoomstufe steht als „×2,5" in der
Leiste. Er läuft ganz im Browser — CSS-Transform, kein Neuzeichnen, kein
Interop-Aufruf je Radrast.

**DATENZOOM — für die Jahresganglinien.** Bei 8 760 Stunden auf 1 100
Bildpunkten liegen acht Werte auf einem Bildpunkt, im Viertelstundenraster
dreißig; ein vergrößerter Bildpunkt zeigt keine Stunde. Ein aufgezogenes
Rechteck (Umschalt + Ziehen mit der Maus, sonst der Umschalter „Bereich") lässt
den Kern das Bild mit **diesem Achsenbereich neu zeichnen** — der Achsenzoom des
WinForms-Vorbilds. Er steht auf **Bedarf** (beide Ganglinien), **Wärmegang** und
**Stromgang**; alle übrigen Bilder übergehen ihn, und der Knopf „Bereich"
erscheint dort gar nicht erst.

Zwei Festlegungen stehen im Kopf der Renderer-Methoden:

* **Die Null bleibt unten.** Alle Ganglinienbilder zählen von null aufwärts. Die
  obere Kante des Rechtecks wird die neue Obergrenze; der Nullpunkt bleibt. Ein
  Ausschnitt ohne Null wäre als Leistungsbild nicht mehr zu lesen.
* **B1 nimmt nur den Zeitausschnitt.** Seine Prozentachse ist per Definition
  0 bis 100 % des **Jahreshöchstwerts**; der Bezugswert bleibt deshalb der der
  ganzen Reihe. Sonst hieße „100 %" in jedem Ausschnitt etwas anderes.

Der Weg des Rechtecks geht durch vier Schichten, und jede kennt nur, was sie
wissen kann: Der Baustein meldet **Anteile des Bildes** (mehr lässt sich an
einem PNG nicht messen), `Bildauftrag` trägt sie weiter (sein Schlüssel trennt
zwei Ausschnitte im Zwischenspeicher), die Hülle ruft
`ChartRenderer.FensterAusBild`, und erst der Renderer — der die Lage seiner
Zeichenfläche kennt — macht daraus Stunden und Kilowatt.

**Der Knopf „1:1" stellt BEIDES zurück**, Bildzoom und Achsenbereich.
Doppelklick und Taste `0` stellen nur den Bildzoom zurück; sie laufen im
Browser und sollen ohne Zeichenlauf auskommen.

### Abnahmepunkte (Nachtrag zu § 10)

| # | Aufrufweg | Was besonders zu prüfen ist |
|---|---|---|
| 18 | **Mausrad über einem beliebigen Diagramm** | Das Bild wird um den Zeiger größer, die Anzeige zählt mit („×2,5"), das Bild bleibt IM Rahmen und läuft nie über die Nachbartabelle. Gilt für jedes Bild — auch Kuchen, Ringe und Kennlinien |
| 19 | **Ziehen im vergrößerten Bild** | Der Ausschnitt verschiebt sich, der Zeiger wechselt zur Faust, an den Rändern ist Schluss (kein weißer Streifen) |
| 20 | **Doppelklick, Taste 0, Knopf „1:1"** | Alle drei stellen 1:1 her. Der KNOPF verwirft zusätzlich einen aufgezogenen Achsenbereich, Doppelklick und Taste nicht |
| 21 | **Rechteck aufziehen** auf Bedarf, Wärmegang, Stromgang (Umschalt + Ziehen oder Knopf „Bereich") | Das Bild wird NEU gezeichnet: Die x-Achse trägt jetzt die wirklichen Jahresstunden des Ausschnitts in runden Schritten, die y-Achse endet an der Oberkante des Rechtecks und beginnt weiter bei null. Auf dem Bedarfsreiter bleibt die Prozentachse 0…100 % |
| 22 | **iPad: Kneifen und Ein-Finger-Verschieben** | Zwei Finger vergrößern, einer verschiebt, und die SEITE zoomt dabei nicht mit. Nur am Gerät prüfbar — der Weg über `gesturestart` und `touch-action: none` lässt sich ohne WKWebView nicht nachweisen |
| 23 | **Bedarfsreiter, Fenster in normaler Breite** | Beide Ganglinien füllen die Breite; die Legende ist lesbar. Dasselbe auf Solarthermie, Photovoltaik, Stromspeicher, Übersicht (Kuchen) und Wärmepumpe |
| 24 | **Wärmepumpe → „Leistung über Außentemperatur"** | Runde x-Marken (z. B. −20 / −10 / 0 / 10 / 20), nichts ragt über den Bildrand, Legende und Achsentitel berühren sich nicht, das Bild liegt NICHT über der Modultabelle |

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

## Windows-Abnahme 05.09.2026 — Formularraster, Paket P3 (iU8‑E‑2)

**Der Wortlaut** (Anwender, 05.09.2026): „Darstellung der Dialoge kompakter und
übersichtlicher — Parameterblöcke rechts. Genauso für andere Dialoge prüfen."
Aufgabe #90 hat daraus die hausweite Regel gemacht (Bausteine
`Formularraster`/`Formulargruppe`, Regel in `epos-ui.css`, Bestandsaufnahme aller
92 Dateien im Protokoll `iU9_W14a`); Paket **P3** hängt Bedarf, Simulation und
Projekt ein. **Kein Feld umbenannt, kein Text geändert, keine Regel je Dialog** —
ein Dialog stellt nur seinen vorhandenen Feldlauf in den Raster.

| Datei | Felder | Raster | Einspaltig | Klasse‑B‑Entscheid |
|---|---|---|---|---|
| `Seiten/Simulation/ParameterReiter.razor` | 23 | 9 | **ja, alle neun** | **Klasse B, umgestellt.** Jeder der neun Feldkästen (`epos-simerg-felder`) stellt seine Felder in den Hausraster; der Kasten selbst **bleibt** — er trägt den Deckel von 640 px und den Abstand, der Raster ordnet darin. **Einspaltig** aus zwei Gründen: Der Parameterblock ist die schmale Spalte der Ergebnisseite, und unter mancher Zahl steht ihre Entsprechung (`epos-simerg-aequivalent`, „= 4,8 kWh") bzw. ein Hinweisabsatz, der zu dem Feld ÜBER ihm gehört — zweispaltig fiele beides neben ein fremdes Feld. Die Begründung steht **einmal** im Dateikopf, an den neun Blöcken nur die Kennung. |

**Nicht angefasst:** das gerechnete Reiterband selbst (`Daten.Unterblaetter`) und
die Kennzahlenlisten (`epos-simerg-werte`, eine `dl`) — sie zeigen Ergebnisse, sie
nehmen keine Eingabe.

**Probe.** `Der_Parameterblock_steht_im_einspaltigen_Formularraster`: so viele
einspaltige Raster wie Raster, und mindestens ein kurzes Feld mit seiner Einheit
in der Feldzeile.

**Eine Zeile Stilblatt kam dazu** — der Unterblock „Formularraster — Paket P3" in
`epos-ui.css`: Eine `Herleitungszeile` als Rasterkind spannt über **alle** Spalten.
Sie gehört zu dem Feld ÜBER ihr („Vorgabe 0,6", „aus dem Kesselwirkungsgrad");
als gewöhnliches Rasterkind fiele sie im zweispaltigen Raster **neben** ein fremdes
Feld und läse sich wie dessen Erläuterung. Sonst kein CSS, keine Inline‑Stile.

---

## Windows-Abnahme V3 07.09.2026 — der Photovoltaik-Reiter (W11b‑B‑6 bis B‑10)

**Anlass.** Bildschirmfoto des Anwenders, Projekt mit einer PV-Anlage (20 Module
„Philadelphia Solar PS‑M144(HCBF)‑530W") und ohne Strombedarf: „Die PV-Simulation
scheint nicht zu funktionieren bzw. wird im Dialog nicht dargestellt." Auf dem Reiter
standen **Gesamte Stromerzeugung 0,00**, **Überschuss 13,26**, in der Modultabelle
**Stromprod. 13,26** und **Fläche 0,00**, die Jahresganglinie war leer, die Einheit
hinter „Maximale solare Leistung [W/m²]" hieß **kW**; dazu „Verbesserte Darstellung:
doppelte Bezeichnung *Detaillierte Simulation* (Info‑Button muss bleiben), Dialog zu
groß, Grafik zu groß."

**Der Kern rechnet richtig; die Anzeige griff auf die falsche Reihe.** `SimulationPV`
trennt seit April (`c3b3e44`) `Stromproduktion_Theoretisch` (Erzeugung nach
Wechselrichter) von `Stromproduktion` = min(Erzeugung, Bedarf), dem GENUTZTEN Anteil.
Ohne Strombedarf ist der genutzte Anteil 0 — und genau den zeigten die Zeile „Gesamte
Stromerzeugung" (`SimulationErgebnisCtrl.Photovoltaik`, `Stromproduktion.Sum()`) und die
Kurve „Photovoltaik" (`BildPv`, `Stromproduktion_viertelstunde`), während Überschuss und
Modultabelle die Erzeugung summierten. Der Vorläufer `Form_Simulation_Detail` (:4551,
:4574) hatte dieselben Reihen: **Der Port war wörtlich, die Beschriftung war es nie.**
Keine Merge-5-Regression — die Merge-4-Basis `b0d3d86` trägt dieselben Zeilen.

| Befund | Was war | Was ist |
|---|---|---|
| **W11b‑B‑6** Erzeugung/Kurve | genutzter Anteil unter der Beschriftung „Gesamte Stromerzeugung"; Kurve leer ohne Bedarf | DTO: `StromproduktionMwh` = Erzeugung, neu `GenutztMwh` = genutzt; Reiter zeigt beide Zeilen; `BildPv` zeichnet die Erzeugung (`Stundenwerte_zu_viertelstunden(Stromproduktion_Theoretisch)`). Deckungsgrad bleibt am genutzten Anteil |
| **W11b‑B‑7** Einheit | Beschriftung W/m², Einheitsspalte kW, Feld `MaxLeistungKw` | Feld `MaxEinstrahlungWm2`, Einheit W/m², Text „Maximale solare Einstrahlung [W/m²]:" (de/en) — `MaxPSolar` ist die Einstrahlung auf die Modulebene |
| **W11b‑B‑8** Fläche 0,00 | Katalog ohne Länge/Breite (CEC-Import führt nur `A_c`), Fläche = 0 × 0 × 20 | `SimulationPV.FlaecheZurAnzeige`: Katalogmaße, sonst A = P_STC / (η · 1 kW/m²) = 51,2 m²; als geschätzt markiert (`≈`, Tooltip). **Nur Anzeige** — Rechenweg und Referenzlauf unberührt |
| **W11b‑B‑9** doppelter Titel | Seite und Überlagerung trugen beide „Detaillierte Simulation" | Seite ohne eigenen Titel; Hilfeknopf bleibt, rechts (`epos-simerg-kopf--ohnetitel`) |
| **W11b‑B‑10** Größe | Bild in voller Zeilenbreite; auf 1280 × 800 bei 150 % (853 × 501 logische Bildpunkte) füllt es den sichtbaren Reiter | `.epos-simerg-diagrammzeile .epos-diagramm { max-width: min(var(--epos-diagramm-breit), 75%) }` — 590 × 267 auf diesem Schirm; Zoom (A‑1) für mehr. Die Überlagerung selbst ist auf diesem Schirm ohnehin durch den Bildschirm begrenzt (`min(92vw, 900px)`, 90vh) |

**Zum „Dialog zu groß".** Die Ergebnisseite steht seit E‑5 in der Überlagerung der
Startseite; deren Maß ist `min(92vw, 900px)` bei `max-height: 90vh`. Auf dem Rechner
der Abnahme (1280 × 800, 150 %) ist das die ganze Arbeitsfläche — kleiner wird der Rahmen
nur mit weniger Inhalt, deshalb greift B‑10 am Bild. `SimulationErgebnisHuelle.MASS`
(1474 × 821) ist seit E‑5 ohne Wirkung und bleibt als Vermerk stehen.

**Nachweis.** `SimulationErgebnisCtrlTests.Photovoltaik_1030…` prüft jetzt Erzeugung,
genutzten Anteil und Einstrahlung getrennt; `PvModulparameterTests` drei Fälle zu
`FlaecheZurAnzeige` (Katalogmaße, Schätzung 51,2 m², kein Wert ohne Nennleistung oder
Wirkungsgrad); `ErzeugerReiterTests` drei Proben (zwei Zeilen, W/m² ohne kW, `≈` mit
Tooltip). `StilblattTests` über die zwei neuen Regeln. Referenzlauf unberührt: kein
Rechenweg geändert.

---

## Windows-Abnahme 08.09.2026 — W11b‑B‑11: Übersichtsreiter der Simulation

**Wortlaut:** „Zahlen fehlen im Diagramm (Prozent und absolut). Wärmebedarfsdeckung ist doppelt
als Diagramm. Stelle die Diagramme nebeneinander. Das Fenster lässt sich nicht vergrößern."

| Punkt | Befund | Änderung |
|---|---|---|
| doppelt | Der Kuchen (`ueb_chart`, Zeile über beide Spalten) und der Ring „Wärmebedarfsdeckung" zeigten dieselbe Deckung | Der Kuchen entfällt (`UebersichtReiter`: keine `epos-simerg-diagrammzeile` mehr; der Parameter `Kuchen` bleibt für die Hülle, wird nicht gezeichnet) |
| Zahlen | Die Ringlegende nannte nur die Namen der Segmente | `SimulationErgebnisHuelle.Bilder.MitZahlen`: je Segment „Name  12,34 MWh  (56,7 %)" — Prozent = Anteil an der Summe der gezeichneten Segmente; Nullsegmente behalten den Namen (der Renderer lässt sie ohnehin aus) |
| nebeneinander | Die Ringe standen bereits in `.epos-simerg-spalten` nebeneinander, darüber der Kuchen | Ohne Kuchen stehen die zwei Ringe als einzige Zeile nebeneinander |
| vergrößern | Die Ergebnisseite lief in der Standard-Überlagerung (min(92 vw, 900 px)) | `Startseite`: `Zusatzklasse="epos-ueberlagerung--breit"` (96 vw bis 1 400 px, 94 vh) — dieselbe Klasse wie die Detailansicht der Wärmepumpe (W7‑E‑2) |

**Nachweis:** `UebersichtReiterTests` angepasst (zwei Bilder statt drei, keine
Diagrammzeile, eine Spaltenzeile; ohne Bedarf kein Bild; beide Ringe rund bemessen).
Sandbox: Kern **2062/2062**, UI **3278/3278**. Abnahme am Gerät: Simulation → Übersicht: zwei
Ringe nebeneinander, Legende mit MWh und Prozent, Fenster fast bildschirmbreit.

---

## Anwenderwunsch 08.09.2026 — W11b‑B‑12: Restwärme unter dem Wärmering, Reststrom unter dem Stromring

**Wortlaut:** „Vertausche Reststrombedarf und Restwärmebedarf im Dialog Detaillierte Simulation."

**Änderung:** Im Navigatorteil des Übersichtsreiters (`UebersichtReiter.razor`) stand die Kachelreihe unter den zwei Ringen (links Wärmebedarfsdeckung, rechts Strombedarfsdeckung) verkehrt: links „Reststrombedarf", rechts „Restwärmebedarf". Getauscht wurde nur die Reihenfolge der zwei `<Kennzahlkachel>`-Elemente im `Kachelraster` — kein Text, keine neue Ressource: links jetzt „Restwärmebedarf" (unter dem Wärmering), rechts „Reststrombedarf" (unter dem Stromring).

**Nachweis:** Neuer Fall `Die_Kacheln_stehen_in_der_Reihenfolge_der_Ringe` (`UebersichtReiterTests`) sichert die Reihenfolge: erste Kachel „Restwärmebedarf", zweite „Reststrombedarf". Sandbox: Kern **2064/2064**, UI **3285/3285** (Build 0 Fehler).

---

## Anwenderwunsch 08.09.2026 — W11b‑B‑13: Kennzahlenlisten der Ergebnisreiter

**Wortlaut:** „Verbessere die Darstellung im Dialog Detaillierte Simulation." (Bildschirmfoto: die Kennzahlenliste
des Wärmepumpenreiters.)

**Befund.** Die Listen aller Ergebnisreiter (`dl.epos-simerg-werte` in `Bedarf-`, `Bhkw-`, `Heizkessel-`,
`Photovoltaik-`, `Solarthermie-`, `Uebersicht-` und `WaermepumpeReiter`) waren ein dreispaltiges Raster mit
`gap: 2px 10px`, **ohne jede Zeilenlinie**, mit der Beschriftung in `--epos-text-leise` und dem Wert daneben in
der Textfarbe. Zwei Fehler auf einmal: Bei zehn Zeilen ohne Trennung fand das Auge die Zahl zur Beschriftung nur
noch mit dem Finger, und leise war ausgerechnet die Beschriftung — also die Information, während die Einheit
gleich laut danebenstand wie der Wert. Dazu formatierten alle Reiter mit `F2`/`F0`: **ohne Tausendertrennung**, im
Bildschirmfoto „4485 h/a".

| Punkt | Was war | Was ist |
|---|---|---|
| Zeilenraster | `gap: 2px 10px`, keine Linie | `padding: 4px 0 5px` je Zelle, `border-bottom: 1px solid var(--epos-rahmen-leise)` |
| Beschriftung | `--epos-text-leise` | `--epos-text` — sie ist die Information |
| Einheit | `--epos-text-sehr-leise`, rechtsbündig | bleibt leise, jetzt linksbündig an ihrer Spalte (ein kurzes „%" wanderte sonst von der Zahl weg, sobald irgendwo „MWh/a" stand) |
| Spalten | `minmax(180px, max-content) max-content max-content` | `minmax(180px, 1fr) minmax(90px, max-content) max-content` — die Wertspalte hat eine Mindestbreite, die Liste nutzt die Blockbreite |
| Zahlen | `F2` bzw. `F0` | `N2` bzw. `N0`, Kultur unverändert (`Kultur` = `CultureInfo.CurrentCulture`) |

Betroffen vom Formatwechsel sind die `Zahl()`-Helfer der fünf Reiter mit eigenem Helfer, die Wertzeile des
`BedarfReiter`, `Zahl(wert, "N2")` im `UebersichtReiter`, die Stundenzeilen (Vollbenutzungsstunden,
Vbh thermisch/elektrisch, Volllaststunden AC) und die kWh-Spalten der Pufferrubrik. Nichts Buntes, kein neuer
Text, keine neue Ressource — die Änderung ist Ruhe.

**Nachweis.** Die vier Proben, die auf dem alten Format bestanden, sind nachgezogen und prüfen jetzt die
Tausendertrennung: `WaermepumpeReiterTests` („1.856" statt „1856"), `BedarfReiterTests` („1.234,50"),
`ErzeugerReiterTests` zweimal („1.505" Vbh thermisch, „1.058,93" W/m²). Alle übrigen Zahlen der Proben liegen
unter 1 000 und sind unverändert. `StilblattTests` (keine Verschachtelung, ausgeglichene Klammern) über die neuen
Regeln.

---

## Anwenderwunsch 08.09.2026 — W11b‑B‑14: Stromspeicher, Wirtschaftsblock

**Wortlaut:** „verbessere die Darstellung Detaillierte Simulation → Stromspeicher, insbesondere der
Wirtschaftlichkeit."

### Befund 1 — die ganze Tabelle war grau

`StromspeicherReiter.Stufenklasse` gab auch für `KennzahlStufe.Unbestimmt` eine Klasse zurück
(`epos-stufe-unbestimmt`), und `epos-ui.css` legte darunter `rgb(240,240,240)`. **Unbestimmt ist die Vorgabe von
`KennzahlStufe`** und damit die Stufe von 37 der 39 Zeilen: Das Grau lag unter der gesamten Tabelle, und die drei
Warnfarben (grün/gelb/rot der Zyklen- und der Budgetzeile) — die einzigen, um die es überhaupt geht — gingen darin
unter. **Änderung:** `Unbestimmt` bekommt gar keine Klasse (leerer String → Blazor lässt das Attribut weg), die
CSS-Regel `tr.epos-stufe-unbestimmt` ist gestrichen. Keine Aussage ist keine Warnung.

### Befund 2 — vierzehn Wirtschaftszeilen ohne Gliederung

Die Zeilen standen in der Reihenfolge des Vorläufers, in der die **Verschleißkosten mitten zwischen den
Summanden** lagen und weder Summe noch Ergebnis erkennbar war. Der Kern (`SpeicherKennzahlenBlock.Zeile`) trägt
jetzt drei zusätzliche Angaben mit Vorgabewerten — `Untergruppe` (fertig übersetzte Zwischenüberschrift),
`Art` (`KennzahlArt`: Normal/Summe/Ergebnis/Nachrichtlich) und `Hinweis` (Werkzeugtipp) —, sodass **jeder
bisherige Aufruf unverändert bleibt**. Die neue Reihenfolge:

| # | Zeile | Unterabschnitt | Art |
|---|---|---|---|
| 1 | Ertrag: vermiedener Netzbezug | Referenzjahr | Posten (+) |
| 2 | Abzug: entgangene Einspeisevergütung | Referenzjahr | Posten (−) |
| 3 | Ertrag: Verkauf ins Netz | Referenzjahr | Posten (+) |
| 4 | Kosten: Netzladung | Referenzjahr | Posten (**neu −**) |
| 5 | Ertrag: Leistungspreisersparnis | Referenzjahr | Posten (+, bis AP7 fest 0) |
| 6 | **Ertrag Referenzjahr E_a,1** | Referenzjahr | **Summe** |
| 7 | Investition I | Über die Nutzungsdauer | Posten |
| 8 | Ertrag degradationsäquivalent E_a,äq | Über die Nutzungsdauer | Posten (+) |
| 9 | Annuität A | Über die Nutzungsdauer | Posten (**neu −**) |
| 10 | **Jahresüberschuss ΔJ** | Über die Nutzungsdauer | **Summe** |
| 11 | Amortisation statisch | Über die Nutzungsdauer | Posten |
| 12 | Amortisation dynamisch | Über die Nutzungsdauer | Posten |
| 13 | **Kapitalwert (NPV)** | Über die Nutzungsdauer | **Ergebnis** |
| 14 | Betriebskosten: Verschleiß K_ver | Nachrichtlich | **Nachrichtlich** |

**Beleg aus dem Kern für die Vorzeichen — nur, was der Kern wirklich rechnet.**
`SpeicherEngine.Arbitrage` (:309‑325) baut die Zeitschrittbewertung als
`eur[k] = +Bezugsersparnis − Vergütung − Ladekosten + Netzerlös`; `E_a,1 = summeF = Σ eur[k]`
(`Wirtschaftlichkeit` :139/:153). Ohne Preissteuerung teilt `StromspeicherSimCtrl.AlsErgebnismodell` (:1487‑1510)
dieselbe Reihe in ihren positiven und ihren negativen Anteil, und Netzerlös, Ladekosten und
Leistungspreisersparnis sind 0. **Die Netzladung geht also negativ in E_a,1 ein** und steht deshalb jetzt mit
Minuszeichen — die Beschriftung „Kosten: Netzladung" bleibt, wie sie im Katalog steht, genau wie „Abzug:
entgangene Einspeisevergütung", die es schon immer so hielt. Ebenso `ΔJ = E_a,äq − A` (`Wirtschaftlichkeit` :158)
→ die Annuität steht mit Minuszeichen. **Nicht als Summe gekennzeichnet ist der Kapitalwert**
(`NPV = E_a,1 · RBF_deg − I`, :161): Er ist ein Ergebnis derselben Rechnung, aber keine Summe der Spalte darüber —
dafür gibt es `KennzahlArt.Ergebnis`. K_ver bleibt außerhalb: „K_ver fliesst NICHT in summeF und nicht in ΔJ"
(`Dauernutzung` :379, `Arbitrage` :396, `Nachtnutzung` :331).

### Befund 3 — „0,0 a" und „−0,0 a"

`Wirtschaftlichkeit.StatischeAmortisation` liefert bei `I = 0` den Jahreswert `0/E = 0`, die dynamische über
`−ln(1−0)/ln(1+i)` ein **negatives Null** (IEEE 754: `−0.0 / x` bleibt negativ), und `"N1"` schreibt dafür
„−0,0". Beides ist keine Aussage: Ohne Investition gibt es nichts zurückzuverdienen, und `I = 0` heißt in der
Sache fast immer, dass die Kosten nicht gepflegt sind. **Änderung:** `SpeicherAnzeigeCtrl.AmortisationText`
normalisiert Beträge unter einem halben Zehntel **vor** dem Formatieren auf glatt 0 (dieselbe Zahl, die `"N1"`
ohnehin anzeigt, nur ohne das irreführende Minus); die neue Überladung
`AmortisationText(Amortisation, double investitionEur)` gibt bei `I ≤ 0` den Gedankenstrich `UNBESTIMMT` zurück,
den die Seite für jede andere unbestimmte Kennzahl führt, samt Werkzeugtipp „Ohne Investition (I = 0) ist die
Amortisation nicht bestimmbar." **Die Amortisationskachel des Kernblocks nutzt dieselbe Überladung**, sonst sagte
die Kachel „0,0" und die Zeile darunter „–".

### Befund 4 — Werkzeugtipps, Ampel, Breite

| Punkt | Was war | Was ist |
|---|---|---|
| Kürzel | ΔJ, E_a,1, E_a,äq, N_zyk, n_zyk, K_ver, NPV standen unerklärt da | sieben Werkzeugtipps als `title` an der Zeile, Text aus dem Kern (`SP_ERG_TIP_*`, de/en) |
| Zyklenampel | loser Absatz `p.epos-simerg-hinweis` **unter der ganzen Tabelle** — also unter der Wirtschaft, über die sie nichts sagt | Zeile über alle Spalten am Ende der Gruppe **Speicher**; Warnfärbung und `white-space: pre-line` (der Text kann mehrzeilig sein) unverändert |
| Breite | die Liste saß in **einer** Spalte des `auto-fit`-Rasters `minmax(320px, 1fr)` und war auf 320 Bildpunkte gequetscht, rechts daneben stand nichts | `.epos-simerg-kennzahlenzeile { grid-column: 1 / -1 }` — dieselbe Lösung wie beim Diagramm (W11b‑B‑2) |
| Tabellenform | keine Linien, `padding: 3px 8px` | Zeilenlinien (Zebra hätte mit den drei Warnflächen gestritten), `4px 10px`, Gruppen- und Unterabschnittskopf unterscheidbar, Zahlenspalten `min-width: 96px`, Einheit leise mit `width: 1%` |
| Summen | nicht erkennbar | Summe fett mit Linie darüber, Ergebnis fett und eingerahmt, Nachrichtliches leise/kursiv |

Die **Vergleichsspalte** (`Daten.MitVergleich`) trägt weiter alle vier Spalten und ist mitgeprüft.

### Neue Ressourcenschlüssel (de/en)

`SP_ERG_UG_REFERENZJAHR`, `SP_ERG_UG_NUTZUNGSDAUER`, `SP_ERG_UG_NACHRICHTLICH`, `SP_ERG_TIP_E_A1`,
`SP_ERG_TIP_E_AEQ`, `SP_ERG_TIP_DELTA_J`, `SP_ERG_TIP_NPV`, `SP_ERG_TIP_K_VER`, `SP_ERG_TIP_N_ZYK`,
`SP_ERG_TIP_N_ZYK_AEQ`, `SP_ERG_TIP_AMORT_OHNE_INVEST` — elf Schlüssel in `Resource.resx`,
`Resource.en-US.resx` und `Resource.Designer.cs`.

### Nachweis

**Kern** (`SpeicherKennzahlenBlockTests`): `Der_Wirtschaftsblock_steht_in_drei_Unterabschnitten` (14 Zeilen,
6/7/1, jeder Abschnitt in einem Stück), `Summe_Ergebnis_und_Nachrichtliches_sind_gekennzeichnet`,
`Die_Posten_des_Referenzjahrs_addieren_sich_zur_Summenzeile` (die fünf Posten ergeben E_a,1),
`Abzuege_und_Kosten_stehen_mit_Minuszeichen` (−60 Vergütung, −20 Netzladung, −700 Annuität),
`Ohne_Investition_ist_die_Amortisation_unbestimmt`, `Die_Kuerzel_tragen_ihren_Werkzeugtipp` (sieben Zeilen),
`Energie_und_Speicher_bleiben_ungegliedert` (25 Zeilen ohne Untergruppe und mit `KennzahlArt.Normal` — der
Rückwärtsnachweis der Vorgabewerte). `SpeicherAnzeigeCtrlTests`:
`AmortisationText_schreibt_kein_negatives_Null` (mit der Zahl, die die Engine wirklich liefert),
`AmortisationText_ohne_Investition_ist_unbestimmt`.

**UI** (`StromspeicherReiterTests`): `Die_Warnstufe_faerbt_die_Zeile` nachgezogen (keine Stufenklasse bei
unbestimmt), neu `Jeder_Unterabschnitt_bekommt_eine_Zwischenueberschrift` (genau eine je Abschnitt, auch bei
mehreren Zeilen), `Summe_Ergebnis_und_Nachrichtliches_tragen_ihre_Klasse`,
`Der_Werkzeugtipp_steht_an_seiner_Zeile` (kein `title=""` ohne Hinweis),
`Die_Zyklenampel_steht_unter_der_Gruppe_Speicher` (zweiter `tbody`, kein loser Absatz mehr),
`Ohne_Ampeltext_bleibt_die_Ampelzeile_weg`, `Die_Kennzahlenliste_steht_ueber_die_ganze_Zeile`.

Sandbox: Build **0 Fehler**, Kern **2073/2073**, UI **3291/3291** (vorher 2064 bzw. 3285; +9 Kern, +6 UI).
Kein Rechenweg geändert — der Referenzlauf ist unberührt: Die Vorzeichen sind eine Sache der Anzeige, die
Engine rechnete schon vorher so.

---

## Anwender-Rückmeldung 08.09.2026 — W11b‑B‑15: Kennzahlenlisten kompakt und nach Wärme/Strom gruppiert

**Wortlaut:** „Die Darstellung ist schlechter geworden — zu weit auseinandergezogen zwischen Text und Zahl. Es
sollte nach Kategorie Strom und Wärme gruppiert werden und die Darstellung übersichtlicher werden. Prüfe auch die
anderen Dialoge." (Dialog „Detaillierte Simulation", Reiter Übersicht.)

**Befund — zwei Sachen auf einmal.** Die Nacharbeit W11b‑B‑13 hatte die Listen mit
`grid-template-columns: minmax(180px, 1fr) …` auf die **ganze Blockbreite** gestellt. Auf einem breiten Fenster
liegen zwischen „Strombedarf:" und „120,50" mehrere hundert Bildpunkte, und die frisch eingeführte Zeilenlinie
lief quer durch die leere Mitte — sie machte den Riss erst sichtbar. Zweitens standen im Übersichtsreiter zwei
Balken **untereinander**: „Energiebedarf" mit zwei Zeilen und „Ergebnisse" mit neun, in denen sich Wärme- und
Stromgrößen abwechselten (WP-Wärme, BHKW-Wärme, Solar, SPK-Wärme, dann WP-Strom, Heizstab, BHKW-Strom, PV,
SPK-Strom). Wer wissen wollte, was die Wärme macht, las jede zweite Zeile — und darunter standen zwei Ringe, die
genau diese Trennung schon vorführten: links Wärme, rechts Strom.

### Änderung 1 — die Liste ist nur so breit wie ihr Inhalt

| Punkt | Was war (B‑13) | Was ist (B‑15) |
|---|---|---|
| Breite | `width: auto` (volle Blockbreite) | `width: max-content; max-width: 100%` |
| Spalten | `minmax(180px, 1fr) minmax(90px, max-content) max-content` | `max-content minmax(7ch, max-content) max-content` |
| Spaltenluft | `gap: 0 12px` | `gap: 0 20px` |
| Beschriftung | durfte umbrechen | `white-space: nowrap` — „durchschnittliche Vollbenutzungsstunden:" bleibt einzeilig, die Wertspalte bündig |
| Zu schmaler Block | Umbruch bzw. Überlagerung | `overflow-x: auto` — die Liste rollt in sich selbst, statt sich zu überlagern |
| Zeilenlinie | über die Blockbreite | nur unter der Liste selbst |
| Abschlusszeile | gab es nicht | `.epos-simerg-abschluss`: fett, `border-top: 2px solid var(--epos-rahmen)` — dieselbe Sprache wie `tr.epos-simerg-summe` der Stromspeichertabelle |

Zeilenlinien, Luft (`4px 0 5px`), `tabular-nums`, Textfarbe der Beschriftung und die leise Einheit bleiben
unverändert — sie waren nicht der Befund.

### Änderung 2 — die Reiter, Punkt für Punkt

| Reiter | Was geändert |
|---|---|
| **Übersicht** (`UebersichtReiter`) | Statt „Energiebedarf"/„Ergebnisse" untereinander **zwei Gruppen nebeneinander** im vorhandenen Raster `.epos-simerg-spalten`: **Wärme** (Bedarf Nahwärmenetz → WP → BHKW → solare Wärme → Spitzenkessel → **Restwärmebedarf**) und **Strom** (Strombedarf → Verbrauch WP → Heizstab → SPK → Produktion BHKW → PV → **Reststrombedarf**). Gruppenköpfe sind die vorhandenen dunklen Balken (`Gruppenkopf`) mit den neuen Texten. Der Stromverbrauch SPK stand vorher hinter den Erzeugerzeilen; er gehört zu den Verbrauchern, aus denen sich die Restzahl ergibt. Keine Zahl entfällt, keine Beschriftung ändert sich, die Präsenzregel bleibt. |
| **Wärmepumpe** (`WaermepumpeReiter`) | Zehn Zeilen in **drei Unterabschnitten** (`h3.epos-untergruppe`, **kein** zweiter Balken): Wärme (Deckung, Wärmebedarf, Produktion WP, **Restwärme**), Strom (Verbrauch WP, Heizstab), Auslegung (Bivalenzpunkt, Vollbenutzungsstunden, min. Spitzenkesselleistung, Pufferkapazität). |
| **Heizkessel** (`HeizkesselReiter`) | Drei Unterabschnitte: Wärme (Deckung, Wärmebedarf, Produktion SPK, Quellwärme, **Restwärme**), Strom (Strombedarf, **Reststrom**), Auslegung (Gesamtleistung, max. Gasbezug). Die Restwärme stand vorher **vor** der Produktion, aus der sie sich ergibt. Der Brennstoffblock behält seinen dunklen Balken — er ist eine eigene Gruppe, kein Unterabschnitt. |
| **BHKW** (`BhkwReiter`) | Drei Unterabschnitte: Wärme (Bedarf, Produktion, Überschuß, Speicherladung, Speicherdeckung, Deckung, **Restwärme**), Strom (Bedarf, Produktion, Deckung, **Reststrom**), Betrieb (Vbh thermisch Summe/Mittel, Vbh elektrisch). Vorher fünfzehn Zeilen in einer Liste. Brennstoffblock unverändert. |
| **Bedarf** (`BedarfReiter`) | Die zwei Spalten trugen ihre Ordnung nur als Quelltextkommentar; sie bekommen die **gleichen zwei Balken** „Wärme"/„Strom" wie die Übersicht. „Wärmebedarf je Bedarfsart" wird dadurch zum **Unterabschnitt** (`h3`) der Wärme — ein Balken im Balken wäre eine Hierarchie, die es nicht gibt. Keine Zeile, keine Zahl geändert. |
| **Solarthermie** (`SolarthermieReiter`) | **Nicht gruppiert.** Fünf Zeilen, alle Wärme — nichts zu trennen. Nur die kompakte Liste (CSS). |
| **Photovoltaik** (`PhotovoltaikReiter`) | **Nicht gruppiert.** Sieben Zeilen, alle Strom bzw. Einstrahlung. Nur die kompakte Liste (CSS). |
| **Stromspeicher** (`StromspeicherReiter`) | Unberührt — seine Kennzahlen stehen in `table.epos-simerg-kennzahlen` und waren mit W11b‑B‑14 gerade gegliedert worden. |

### Neue Ressourcenschlüssel (de/en)

`SIMERG_GRP_WAERME` (Wärme / Heat), `SIMERG_GRP_STROM` (Strom / Electricity), `SIMERG_GRP_AUSLEGUNG`
(Auslegung / Sizing), `SIMERG_GRP_BETRIEB` (Betrieb / Operation) — vier Schlüssel in `Resource.resx`,
`Resource.en-US.resx` und `Resource.Designer.cs`. `SIMERG_LBL_ENERGIEBEDARF` und `SIMERG_LBL_ERGEBNISSE` bleiben
als Text im Katalog stehen (keine Umbenennung), werden von der Oberfläche aber nicht mehr gerufen.

### Bauweise

`Wertzeile` bekommt in den vier geänderten Reitern einen vierten Parameter `betont` (Vorgabe `false`). Die
Klasse kommt aus `Zeilenklasse(bool)`, das **`null`** zurückgibt, wenn nicht betont — Blazor lässt das Attribut
dann ganz weg, die gewöhnliche Zeile bleibt zeichengleich `<dt>…</dt><dd>…</dd>` wie vorher (die Probe
`Bhkw_zeigt_ohne_Nennleistung_einen_Gedankenstrich` prüft `<dd>—</dd>` wörtlich und bleibt grün).

### Nachweis

**UI**, neu — `UebersichtReiterTests`: `Die_Kennzahlen_stehen_in_den_zwei_Gruppen_Waerme_und_Strom` (zwei
Balken „Wärme"/„Strom", die alten zwei Texte nirgends mehr), `Die_zwei_Gruppen_stehen_nebeneinander` (beide
Listen in EINER Spaltenzeile), `Die_Waermegruppe_fuehrt_Bedarf_Erzeuger_und_Rest` und
`Die_Stromgruppe_fuehrt_Bedarf_Verbraucher_Erzeuger_und_Rest` (die Zeilenfolge je Gruppe, wörtlich),
`Die_Restzeile_schliesst_jede_Gruppe_betont_ab` (zwei `dt.epos-simerg-abschluss`, vier `dd`, neun Zeilen im
Ganzen, und **kein** leeres Klassenattribut an den übrigen). `WaermepumpeReiterTests`:
`Die_zehn_Felder_stehen_in_drei_Unterabschnitten` (drei `h3`, kein Balken, die drei Zeilenfolgen wörtlich),
`Die_Restwaerme_schliesst_die_Waermegruppe_betont_ab`. `ErzeugerReiterTests`:
`Kessel_gliedert_seine_Felder_in_Waerme_Strom_und_Auslegung`,
`Bhkw_gliedert_seine_Felder_in_Waerme_Strom_und_Betrieb` (je vier Listen: drei Gruppen + Brennstoffblock).
`BedarfReiterTests`: `Die_zwei_Spalten_tragen_die_Koepfe_Waerme_und_Strom`. `StilblattTests` über die neuen
Regeln (ausgeglichene Klammern, keine Verschachtelung).

Sandbox: Build **0 Fehler**, Kern **2073/2073**, UI **3301/3301** (vorher 2073 bzw. 3291; +10 UI, Kern
unverändert). Kein Rechenweg berührt — dieselben Zahlen aus demselben DTO, nur anders sortiert und anders gesetzt.

## Anwenderwunsch 08.09.2026 — W11b‑B‑16: EIN Schalter „sortiert" im Reiter „Wärme-/Strombedarf"

**Wortlaut:** „Schiebe die Checkbox unten nach oben — es braucht nur eine Checkbox sortiert." (Dialog
„Detaillierte Simulation", Reiter Bedarf. Davor gemeldet als: die Checkbox „funktioniert nicht".)

**Befund — zwei Schalter mit demselben Namen in zwei Spalten.** Der Reiter führte ZWEI Zustände
`SIM_CHK_SORTIERT`: `_waermeSortiert` in der Schalterzeile der Wärmespalte (bei „Gesamt" und den
Bedarfskanälen) und `_stromSortiert` allein in der Stromspalte. Das Zwei-Spalten-Raster stellt die zweite
Spalte NEBEN die erste — der Strom-Schalter kam damit optisch neben den Unterabschnitt „Wärmebedarf je
Bedarfsart" zu stehen. Der Anwender hielt ihn für den der Wärme, hakte ihn an und sah die Wärmeganglinie
unverändert: Aus seiner Sicht war der Schalter kaputt. Er war es nicht — er gehörte nur sichtbar zum falschen
Bild. Zwei gleich beschriftete Schalter in einem Reiter sind schon für sich eine Zumutung; welcher welchen
Graphen meint, stand nirgends.

### Änderung

| Punkt | Was war | Was ist |
|---|---|---|
| Zustand | `_waermeSortiert` **und** `_stromSortiert` | **ein** `_sortiert` |
| Ort | je einer in seiner Spalte | **einer ganz oben**, über beiden Spalten (erstes Kind von `.epos-simerg-block`, vor `.epos-simerg-spalten`) |
| Wirkung | je ein Bildauftrag | **beide** Bildaufträge (`Bilder.BedarfWaerme`, `Bilder.BedarfStrom`) tragen `Sortiert = true` |
| Prüfhilfe | keine | `BedarfReiter.Sortiert` |
| Reihenschalter | „Gesamt / Heizung / Brauchwasser" bei der Wärme | unverändert dort — sie wählen REIHEN, nicht die Darstellungsart |

Beschriftung (`SIM_CHK_SORTIERT`), Bildaufträge, Zwischenspeicherschlüssel, Datenzoom und der CSV-Export
bleiben, wie sie waren; kein neuer Ressourcenschlüssel, keine CSS-Regel. Die Schalterzeile steht außerhalb des
Rasters und ist damit schon von sich aus so breit wie der Reiter — `.epos-simerg-schalter` genügt.

### Nachweis

**UI**, `BedarfReiterTests`: `Der_Sortiertschalter_wechselt_den_Bildauftrag` heißt jetzt
`Der_eine_Sortiertschalter_wechselt_beide_Bildauftraege` und prüft dreierlei — genau EIN Kästchen mit der
Beschriftung „sortiert" (vorher zwei), `Instance.Sortiert` nach dem Klick, und **beide** Bildaufträge mit
`Sortiert = true`. `Die_Kanalschalter_stehen_im_Bildauftrag` hält die Indexordnung fest: `[0]` sortiert (der
eine, ganz oben), `[1]` Gesamt, `[2]` Heizung, `[3]` Brauchwasser — und dass der Reiter **vier** Kästchen hat,
nicht mehr fünf.

## Anwenderwunsch 08.09.2026 — W11b‑B‑17: Reihen der zwei Wärmepumpen-Diagramme wählbar

**Wortlaut:** „Die Graphen der Diagramme sollten auswählbar sein (select) für beide Grafiken." (Dialog
„Detaillierte Simulation", Reiter Wärmepumpe.)

**Befund.** Der Reiter zeigt zwei Bilder mit drei bzw. vier Reihen: die Streuwolke „Leistung über
Außentemperatur" (Wärmebedarf, Heizstab, Wärmeproduktion) und die Jahresganglinie „Wärmelast" (Heizwärmebedarf,
Warmwasserbedarf, Wärmeproduktion, Heizstab). Beide zeichneten IMMER alles. In der Streuwolke liegen drei
Punktwolken zu je 8 760 Punkten übereinander — halbtransparent, aber wer die Wärmeproduktion allein sehen will,
kann sie nicht freistellen. Der Bedarfsreiter (Paket E2) und der Photovoltaikreiter konnten das längst; hier
fehlte es.

### Änderung

| Punkt | Was war | Was ist |
|---|---|---|
| Auswahl | keine | je Bild eine **Schalterzeile direkt über dem Bild**, je Reihe ein `Schalter`, **alle vorbelegt an** |
| Beschriftung | — | **dieselbe Ressource wie die Legende** des Bildes: `CHART_LEGENDE_WAERMEBEDARF`, `CHART_SEGMENT_HEIZSTAB`, `CHART_LEGENDE_WAERMEPRODUKTION` bzw. `CHART_LEGENDE_HEIZWAERMEBEDARF`, `CHART_LEGENDE_WARMWASSERBEDARF`, `CHART_LEGENDE_WAERMEPRODUKTION`, `CHART_SEGMENT_HEIZSTAB` |
| Bildauftrag | `new Bildauftrag(Bilder.WpLeistungTemperatur)` bzw. `(Bilder.WpProduktion, _sortiert)` | dazu `Reihen` — sprachneutral `WAERMEBEDARF`/`HEIZSTAB`/`WAERMEPRODUKTION` bzw. `HEIZWAERMEBEDARF`/`WARMWASSERBEDARF`/`WAERMEPRODUKTION`/`HEIZSTAB` |
| Hülle | `BildStreuwolke()`, `BildWpProduktion(bool sortiert)` | `BildStreuwolke(Bildauftrag a)`, `BildWpProduktion(Bildauftrag a)` — die Reihen entstehen nur, wenn sie gewählt sind |
| Prüfhilfen | `Sortiert` | dazu `GewaehlteReihenStreuwolke`, `GewaehlteReihenProduktion` |
| „sortiert" | in der Schalterzeile des Produktionsblattes | bleibt dort, aber in EIGENER Zeile über den Reihen — dieselbe Trennung wie in W11b‑B‑16: der eine wählt die Darstellungsart, die anderen die Reihen |

**Leer heißt keine, nicht alle.** Die übrigen Bilder werten `Bildauftrag.Reihen` nach dem Muster
`wahl.Count == 0 || wahl.Contains(…)` aus — eine leere Liste heißt dort „alle". Das ginge hier nicht: Wer das
letzte Häkchen wegnimmt, bekäme das volle Bild zurück. Die zwei neuen Helfer `Alle(a)`/`Gewaehlt(a, alle, …)`
der Hülle unterscheiden deshalb **`null`** (keine Angabe → alle Reihen; so rufen die Bilder ohne Auswahl und so
rief die Wärmepumpenseite bis heute) von der **leeren Liste** (der Anwender hat alles abgewählt → keine Reihe).
Der Renderer zeichnet daraus seinen Leerhinweis (`ErzeugerStapel` bzw. `Streuwolke` prüfen das selbst) — kein
Sonderfall, keine Ausnahme. Achsen, Skalierung, Farben, Halbtransparenz (`WithAlpha(120)`) und die kumulierte
Heizstabwolke bleiben unangetastet; der Zwischenspeicherschlüssel trennt die Auswahlstände schon
(`Bildauftrag.Schluessel` führt die Reihenliste mit).

### Nachweis

**UI**, `WaermepumpeReiterTests` — neu: `Beide_Diagramme_tragen_je_Reihe_einen_Schalter` (drei bzw. vier
Beschriftungen wörtlich in der Reihenfolge des Bildes, alle Reihen-Kästchen an, „sortiert" aus, die zwei
Prüfhilfen), `Die_Abwahl_nimmt_die_Reihe_aus_dem_Bildauftrag` (Heizstab der Streuwolke und Heizwärmebedarf der
Ganglinie ab — jedes Bild führt seine eigene Wahl), `Alle_Reihen_abgewaehlt_geben_eine_leere_Liste` (leere
Reihenliste im Auftrag, keine Ausnahme). Angepasst: `Der_Sortiertschalter_wechselt_nur_den_Bildauftrag` greift
das Kästchen jetzt über seine Schalterzeile (die Streuwolke steht davor) und prüft zusätzlich, dass die vier
Reihen beim Umschalten dieselben bleiben — Befund W11-B18 unverändert gewahrt.

**Nicht durch Tests gedeckt:** die Reihenfilterung IN der Hülle. `SimulationErgebnisHuelle.Bilder.cs` liegt in
`WindowsFormsApplication1`; es gibt kein Testprojekt, das dort ein Bild zeichnet (auch die vorhandene
Reihenwahl von `BildBedarfWaerme`, `BildPv`, `BildWaermegang` und `BildStromgang` ist nur über die Seite
geprüft). Die Filterung folgt zeichengleich diesen vier Vorbildern; die Sichtabnahme am Programm steht aus.

### Zahlen

Sandbox: Build **0 Fehler**, Kern **2073/2073**, UI **3304/3304** (vorher 3301; +3 UI aus W11b‑B‑17,
W11b‑B‑16 kommt ohne neuen Fall aus). Kein Rechenweg berührt — dieselben Reihen aus demselben Lauf, nur
wählbar.

## Anwenderbefund 09.09.2026 — W11b‑B‑18: Jahresganglinie mit Bedarf und Produktion

**Wortlaut:** „Die Darstellung Heizwärmebedarf und Wärmeproduktion stimmt nicht, wenn beide
gleichzeitig dargestellt werden.“ (Dialog „Detaillierte Simulation“, Reiter Wärmepumpe, Bild
„Wärmelast Jahresganglinie“.)

**Befund — zwei Stapelgruppen standen NEBENEINANDER statt übereinander.**
`ChartRenderer.ErzeugerStapel` zeichnete zwei Stapelgruppen (`Stapelart.Flaeche` = Bedarf,
`Stapelart.Saeule` = Produktion) IMMER nebeneinander: `StapelZeichnen(…, versatz −0,22/+0,22,
breite 0,5)` schob jede Gruppe in eine Hälfte der Zeichenfläche. Für Kategorieachsen mit wenigen
Werten (Gruppensäulen) ist das richtig; für eine Jahresganglinie mit 8 760 Stundenwerten ist es
falsch, weil beide Gruppen für JEDE Stunde gelten: Der Bedarf erschien in der linken, die
Produktion in der rechten Bildhälfte, statt für dieselbe Stunde übereinanderzuliegen. Betroffen ist
`BildWpProduktion` (Bild „Wärmelast Jahresganglinie“ des Wärmepumpenreiters) — die EINZIGE Stelle
im Bestand, die zwei Stapelgruppen zugleich führt (`SimulationErgebnisHuelle.Bilder.cs`,
geprüft mit `grep Stapelart.Flaeche`). Heizkessel, BHKW und Stromgang (`BildKessel`, `BildBhkw`,
`BildStromgang`) tragen je nur EINE Stapelgruppe (`Stapelart.Saeule`) und waren nicht betroffen —
bei ihnen war `zweiGruppen` schon vorher `false`.

### Änderung

| Punkt | Was war | Was ist |
|---|---|---|
| Bedingung | nebeneinander IMMER bei zwei Gruppen (`zweiGruppen`) | nebeneinander NUR, wenn zusätzlich `achse != Achse.Jahresstunden` UND `n <= 60` (Kategorieachse, wenige Stützstellen) |
| Stundenachse / viele Stützstellen | nebeneinander (falsch) | beide Gruppen über der VOLLEN Breite (versatz 0, breite 1) — sie liegen übereinander |
| Zeichenreihenfolge | Fläche, dann Säule, je in ihrer Hälfte | unverändert: zuerst die Fläche (Bedarf), dann die Säule (Produktion) — bei Überlagerung jetzt übereinander |
| Deckkraft der Säulengruppe | 210 (wie jede Stapelfläche) | 210 nebeneinander, **150** bei Überlagerung — die Fläche (Bedarf) bleibt darunter sichtbar |
| `StapelZeichnen`/`ZeichneFlaeche` | keine Alpha-Überschreibung | neuer Parameter `alpha` (Vorgabe 210), von `ErzeugerStapel` gesetzt |
| Obergrenze (`max`) | Höchste Stapelsumme je Gruppe | unverändert — Bedarf und Produktion bleiben unabhängige Größen mit gemeinsamer Nulllinie, ihre Werte werden nicht aufeinandergerechnet |
| Dauerlinie (sortiert) | ohne Stapel, je Reihe eine Linie | unverändert |

Die Grenze `n <= 60` liegt bewusst über typischen Monatsbildern (12 Stützstellen) und weit unter
Jahresganglinien (8 760 bzw. 35 040 Werten je nach Raster) — sie trifft heute NUR die
Kategoriedarstellung; kein Aufrufer im Bestand nutzt `Achse.Monate` mit zwei Stapelgruppen und mehr
als 60 Werten.

### Nachweis

**Kern**, neu in `ErgebnisbilderTests.cs`:
`ErzeugerStapel_Jahresganglinie_ueberlagert_Bedarf_und_Produktion_ganzflaechig` — zwei FLACHE
(konstante) Reihen, Bedarf 100 als Fläche, Produktion 50 als Säule, `Achse.Jahresstunden`, 8 760
Werte. Bei einer Höhe, die NUR der Bedarf erreicht, ist die Pixelfarbe im linken UND im rechten
Drittel der Zeichenfläche GLEICH (vorher stand rechts nichts, weil die Fläche nur die linke Hälfte
füllte); bei einer Höhe, die beide Gruppen erreichen, ist die (halbtransparent gemischte) Farbe
ebenfalls links und rechts GLEICH, und sie unterscheidet sich sichtbar von der reinen Bedarfsfarbe
(die Produktion liegt also tatsächlich, aber durchscheinend, darüber).
`ErzeugerStapel_Monatsbild_bleibt_nebeneinander` — Gegenfall mit zwölf Monatswerten
(`Achse.Monate`): links (Bedarfshälfte) trägt bei derselben Höhe Farbe, rechts (Produktionshälfte)
bleibt leer — die Nebeneinander-Darstellung bleibt für echte Kategorieachsen mit wenigen
Stützstellen erhalten.

### Zahlen

Sandbox: Build **0 Fehler**, Kern **2075/2075**, UI **3304/3304** (vorher Kern
2073, UI 3304; +2 Kern aus W11b‑B‑18, UI unverändert — die Änderung liegt allein im Kern-Renderer).
Kein Rechenweg berührt — nur die Zeichenlage zweier Stapelgruppen bei Stundenachsen mit vielen
Stützstellen.

## Anwenderwunsch 09.09.2026 — W11b‑B‑19: Reihen der Diagramme von Solarthermie und Photovoltaik wählbar

**Wortlaut:** „Reihen der Diagramme wählbar." (Dialog „Detaillierte Simulation", Reiter Solarthermie und
Photovoltaik.) Es ist derselbe Wunsch, den W11b‑B‑17 für die zwei Wärmepumpen-Diagramme erfüllt hat, nur für
die zwei verbliebenen Erzeugerbilder.

**Befund — zwei ungleiche Ausgangslagen.**

* **Solarthermie** (`BildSolar()`, „Wärmelast Jahresganglinie"): zwei Linien, Wärmebedarf und
  Wärmeproduktion, und **gar keine Auswahl**. Der Reiter trug bis dahin überhaupt keinen Schalter — der
  Kopfkommentar sagte es wörtlich: „Kein Umschalter, kein Export, kein Zoom".
* **Photovoltaik** (`BildPv(a)`, „Strombedarf, Photovoltaik Jahresganglinie"): vier Reihen, aber nur **zwei
  Schalter** — „Überschuß anzeigen" und „Speicherfüllung anzeigen". Die zwei Grundreihen `STROMBEDARF` und
  `PHOTOVOLTAIK` waren **fest an**: Die Hülle prüfte sie mit `wahl.Count == 0 || wahl.Contains(…)`, und die
  Seite legte sie in `Reihen()` unbedingt in die Liste. Wer nur die Erzeugungskurve sehen wollte, konnte den
  Strombedarf nicht wegnehmen.

### Änderung

| Punkt | Was war | Was ist |
|---|---|---|
| Solarthermie, Auswahl | keine | Schalterzeile **direkt über dem Bild**, je Reihe ein `Schalter`, **beide vorbelegt an** |
| Solarthermie, Beschriftung | — | dieselbe Ressource wie die Legende: `CHART_LEGENDE_WAERMEBEDARF`, `CHART_LEGENDE_WAERMEPRODUKTION` |
| Solarthermie, Bildauftrag | `new Bildauftrag(Bilder.Solarthermie)` (Reihen = `null`) | dazu `Reihen` — sprachneutral `WAERMEBEDARF`, `WAERMEPRODUKTION` |
| Solarthermie, Hülle | `BildSolar()` | `BildSolar(Bildauftrag a)` mit `Alle(a)`/`Gewaehlt(a, alle, …)` |
| Photovoltaik, Auswahl | zwei Schalter „… anzeigen" für die zwei Zusatzreihen | **EINE** Zeile mit **allen vier** Reihen; die zwei alten Schalter gehen darin auf |
| Photovoltaik, Beschriftung | `SIMERG_CHK_PV_UEBERSCHUSS`, `SIMERG_CHK_SPEICHERFUELLUNG` | die Legendenressourcen des Bildes: `CHART_ACHSE_STROMBEDARF`, `SIM_PHOTOVOLTAIK`, `CHART_LEGENDE_UEBERSCHUSS`, `PSP_CHECKBOX_SPEICHERFUELLSTAND` |
| Photovoltaik, Vorbelegung | Grundreihen fest an, Zusatzreihen aus | **unverändert**: Grundreihen an, Zusatzreihen aus (wörtlich :4676‑4679) — nur sind die Grundreihen jetzt abwählbar |
| Photovoltaik, Hülle | `wahl.Count == 0 \|\| wahl.Contains("STROMBEDARF")` bzw. `…("PHOTOVOLTAIK")`, `wahl.Contains("SPEICHERFUELLSTAND")` | durchgehend `Gewaehlt(a, alle, …)` — der `Count == 0`-Rückfall entfällt |
| Prüfhilfen | PV: `GewaehlteReihen` | dazu Solarthermie: `GewaehlteReihen` |
| Zweite Y-Achse | Speicherfüllstand in kWh rechts | unverändert |

**Leer heißt keine, nicht alle.** Beide Bilder folgen jetzt der Regel aus W11b‑B‑17: `Bildauftrag.Reihen`
= **`null`** heißt „keine Angabe → alle Reihen" (so rufen die Bilder ohne Auswahl), eine **leere Liste** heißt
„der Anwender hat alles abgewählt → keine Reihe". Der Renderer zeichnet daraus seinen Leerhinweis — kein
Sonderfall, keine Ausnahme. Beim PV-Bild ist das die eigentliche Verhaltensänderung: Vorher gab die leere
Liste die zwei Grundreihen zurück. Da die Seite immer eine Liste mitgibt (`Reihen()` liefert nie `null`) und
kein anderer Aufrufer `Bilder.Photovoltaik` bestellt (geprüft mit `grep Bilder.Photovoltaik`), ist die
`null`-Bedeutung „alle" hier nur die dokumentierte Vorgabe der Hülle. Achsen, Farben, Reihenfolge der Reihen,
Halbtransparenz und die zweite Y-Achse bleiben unangetastet; der Zwischenspeicherschlüssel trennt die
Auswahlstände schon (`Bildauftrag.Schluessel` führt die Reihenliste mit).

**Zwei Ressourcenschlüssel werden damit unbenutzt:** `SIMERG_CHK_PV_UEBERSCHUSS` und
`SIMERG_CHK_SPEICHERFUELLUNG`. Sie bleiben im Katalog stehen (de + en, unverändert) — entfernt wird nichts,
was ein anderer Strang noch aufgreifen könnte.

### Nachweis

**UI**, `ErzeugerReiterTests` — neu für die Solarthermie: `Solarthermie_traegt_je_Reihe_einen_Schalter`
(beide Beschriftungen wörtlich in der Reihenfolge des Bildes, beide Kästchen an, Prüfhilfe und Bildauftrag
tragen `WAERMEBEDARF`/`WAERMEPRODUKTION`), `Solarthermie_nimmt_die_abgewaehlte_Reihe_aus_dem_Bildauftrag`,
`Solarthermie_ohne_gewaehlte_Reihe_gibt_eine_leere_Liste`. Neu für die Photovoltaik:
`Photovoltaik_traegt_eine_Schalterzeile_mit_allen_vier_Reihen` (EINE Zeile, vier Legendenbeschriftungen, die
zwei Grundreihen an, die zwei Zusatzreihen aus),
`Photovoltaik_nimmt_die_abgewaehlte_Grundreihe_aus_dem_Bildauftrag` (neu: auch `STROMBEDARF` ist abwählbar),
`Photovoltaik_ohne_gewaehlte_Reihe_gibt_eine_leere_Liste`. Angepasst:
`Photovoltaik_startet_mit_zwei_abgeschalteten_Reihen` prüft die Reihen jetzt namentlich statt nur ihre Anzahl,
`Photovoltaik_nimmt_den_Speicherfuellstand_ueber_seinen_Haken_dazu` greift das vierte Kästchen der Zeile (es
war das zweite).

**Nicht durch Tests gedeckt:** die Reihenfilterung IN der Hülle — `SimulationErgebnisHuelle.Bilder.cs` liegt
in `WindowsFormsApplication1`, und es gibt kein Testprojekt, das dort ein Bild zeichnet (dieselbe Lage wie bei
W11b‑B‑17). Die Filterung folgt zeichengleich den Vorbildern `BildStreuwolke`/`BildWpProduktion`; die
Sichtabnahme am Programm steht aus.

## Anwenderwunsch 09.09.2026 — W11b‑B‑20: Kennzahlenlisten Solarthermie und Photovoltaik wie im Bedarfsreiter

**Wortlaut:** „Kennzahlenlisten Solarthermie und Photovoltaik wie im Bedarfsreiter." Dazu zwei benannte
Beobachtungen am PV-Reiter: die Beschriftungen tragen „die Einheit doppelt", und „die Liste läuft rechts aus
dem Raster (Einheitsspalte abgeschnitten, Rollbalken)".

**Befund 1 — die Einheit stand zweimal da.** Alle sieben PV-Beschriftungen führten ihre Einheit im Text mit,
obwohl die Liste seit W11b‑B‑15 eine eigene, leise gesetzte Einheitenspalte hat: „Gesamte Stromerzeugung der
Module **[MWh/a]**:" gefolgt von der Spalte „MWh/a". Zwei Schreibweisen waren im Umlauf — fünf Schlüssel mit
eckigen Klammern, zwei ohne („Strombedarf **MWh/a**:", „Reststrombedarf **MWh/a**:").

**Befund 2 — die Liste war rechts abgeschnitten.** Sie stand in einer Spalte des `auto-fit`-Rasters
`.epos-simerg-spalten` (`minmax(320px, 1fr)`), **obwohl neben ihr nichts steht**: Der PV- wie der
Solarthermie-Reiter führt genau einen Zahlenblock, und das Diagramm darunter spannt mit
`.epos-simerg-diagrammzeile` ohnehin schon über die ganze Zeile. `.epos-simerg-werte` ist seit W11b‑B‑15
`width: max-content` mit `max-width: 100%` und `overflow-x: auto` — der Deckel ist also die **Spalten**breite,
und die langen PV-Beschriftungen sprengten sie: Die Einheitenspalte fiel aus dem Sichtfeld, darunter erschien
ein Rollbalken.

**Befund 3 — die Reihenfolge war die der WinForms-Maske.** Photovoltaik: sieben Zeilen in EINER Liste, in der
sich Erzeugung, Bedarf und Einstrahlung abwechselten (Erzeugung, genutzt, Überschuß, Deckung, Strombedarf,
Rest, Einstrahlung). Solarthermie: fünf Zeilen, Deckungsgrad zuerst und der Restwärmebedarf **vor** der
Produktion, aus der er sich ergibt. Die Erzeugerreiter Wärmepumpe, Heizkessel und BHKW hatten mit W11b‑B‑15
längst Unterabschnitte und eine betonte Restzeile.

### Änderung

| Punkt | Was war | Was ist |
|---|---|---|
| PV-Beschriftungen | Einheit im Text **und** in der Spalte | Einheit **nur** in der Spalte; sieben Ressourcentexte gekürzt (de + en), Werte und Einheitenspalte unverändert |
| PV-Gliederung | eine Liste mit sieben Zeilen | drei `h3.epos-untergruppe`: **„Erzeugung"** (Gesamte Stromerzeugung, davon direkt genutzt, Überschuß), **„Bedarf und Deckung"** (Strombedarf, Reststrombedarf *betont*, Strombedarfsdeckung), **„Einstrahlung"** (Maximale solare Einstrahlung) |
| Solarthermie-Gliederung | eine Liste mit fünf Zeilen, Maskenreihenfolge | ein `h3.epos-untergruppe` **„Wärme"**, fachlich geordnet: Bedarf → Erzeugung → Überschuß → Rest *betont* → Deckung |
| Betonte Zeile | keine | `Wertzeile(…, betont: true)` wie im Wärmepumpen- und Kesselreiter (`dt`/`dd` mit `epos-simerg-abschluss`) — je Reiter genau eine |
| Breite | Liste in EINER Rasterspalte | `section` trägt zusätzlich **`epos-simerg-kennzahlenzeile`** (`grid-column: 1 / -1`) — die Liste bekommt die ganze Zeile, bleibt aber `max-content` schmal |
| Wärmepumpe | dasselbe Muster, dieselbe Falle | **ebenfalls** `epos-simerg-kennzahlenzeile`: Auch dort steht der Zahlenblock allein in seiner Zeile, und „durchschnittliche Vollbenutzungsstunden:" ist lang |
| Heizkessel, BHKW, Übersicht, Bedarf | zwei Blöcke nebeneinander | **unverändert** — dort steht neben der Kennzahlenliste ein zweiter Block (Brennstoffe bzw. die zweite Kategorie); die zwei Spalten sind gewollt |

**Warum die Deckung UNTER dem Rest steht.** Die betonte Zeile ist der Rest, und die Ordnung folgt dem
Rechenweg: Bedarf, was die Anlage davon deckt, was übrig bleibt — und zuletzt derselbe Rest noch einmal als
Prozentzahl. Das ist die Reihenfolge, die der Anwender vorgegeben hat (Bedarf → Erzeugung → Überschuss → Rest
→ Deckung); sie weicht bewusst von Heizkessel und BHKW ab, wo die Deckung vor dem Rest steht.

### Gekürzte Ressourcenschlüssel (de/en)

| Schlüssel | vorher (de) | jetzt (de) | jetzt (en) |
|---|---|---|---|
| `SIMERG_LBL_PV_GESAMT` | Gesamte Stromerzeugung der Module [MWh/a]: | Gesamte Stromerzeugung der Module: | Total electricity generation of the modules: |
| `SIMERG_LBL_PV_GENUTZT` | davon direkt genutzt [MWh/a]: | davon direkt genutzt: | of which used directly: |
| `SIMERG_LBL_PV_UEBERSCHUSS` | Überschuß [MWh/a]: | Überschuß: | Surplus: |
| `SIMERG_LBL_PV_DECKUNG` | Strombedarfsdeckung [%]: | Strombedarfsdeckung: | Electricity requirement coverage: |
| `SIMERG_LBL_PV_STROMBEDARF` | Strombedarf MWh/a: | Strombedarf: | Power requirement: |
| `SIMERG_LBL_PV_REST` | Reststrombedarf MWh/a: | Reststrombedarf: | Residual power requirement: |
| `SIMERG_LBL_MAX_SOLARE_LEISTUNG` | Maximale solare Einstrahlung [W/m²]: | Maximale solare Einstrahlung: | Maximum solar irradiance: |

### Neue Ressourcenschlüssel (de/en)

`SIMERG_GRP_ERZEUGUNG` „Erzeugung"/„Generation" · `SIMERG_GRP_BEDARF_DECKUNG` „Bedarf und
Deckung"/„Demand and coverage" · `SIMERG_GRP_EINSTRAHLUNG` „Einstrahlung"/„Irradiance". Die
Solarthermie kommt mit dem vorhandenen `SIMERG_GRP_WAERME` aus.

### Nachweis

**UI**, `ErzeugerReiterTests` — neu: `Photovoltaik_nennt_die_Einheit_nur_in_ihrer_Spalte` (keine
Beschriftung enthält „MWh", kein „[W/m²]" im Markup, die Einheitenspalte trägt weiter „MWh/a"),
`Photovoltaik_gliedert_seine_Felder_in_Erzeugung_Bedarf_und_Einstrahlung` (drei Unterabschnitte, drei Listen,
jede Zeile wörtlich in ihrer Reihenfolge, „Reststrombedarf:" als einzige betonte Zeile),
`Solarthermie_gliedert_ihre_Felder_und_betont_den_Rest` (ein Unterabschnitt „Wärme", fünf Zeilen in der
fachlichen Ordnung, „Restwärmebedarf:" betont), dazu je Reiter
`…_gibt_der_Kennzahlenliste_die_ganze_Rasterzeile`. In `WaermepumpeReiterTests` neu:
`Die_Kennzahlenliste_nimmt_die_ganze_Rasterzeile`.

**Nicht durch Tests gedeckt:** die Breite selbst — bUnit rendert ohne Layout, `grid-column` wird nicht
gerechnet. Geprüft ist, dass die Klasse an der richtigen `section` steht; die Sichtabnahme am Programm steht
aus.

### Zahlen

Sandbox: Build **0 Fehler**, Kern **2075/2075**, UI **3316/3316** (vorher 3304; +12 UI, davon 6 aus
W11b‑B‑19 und 6 aus W11b‑B‑20). Kein Rechenweg berührt — dieselben Zahlen aus demselben Lauf, nur anders
geordnet, anders beschriftet und in wählbaren Reihen.

## Anwenderwunsch 09.09.2026 — W11b‑B‑21: Reihen der Kessel-Jahresganglinie wählbar

**Wortlaut:** „Reihenwahl der Wärmelast-Jahresganglinie des Heizkessels … mit Schalterzeile über
dem Bild, vorbelegt alle an; ebenso für jedes weitere Bild des Reiters." (Dialog „Detaillierte
Simulation", Reiter Heizkessel.) Derselbe Wunsch, den W11b‑B‑17 für die zwei Wärmepumpen-Bilder
und W11b‑B‑19 für Solarthermie und Photovoltaik erfüllt hat.

**Befund.** `BildKessel(bool sortiert)` zeichnete IMMER alle drei Reihen: die Säulen der
Wärmeproduktion Heizkessel, die Linie Restwärme und darüber den Wärmebedarf gesamt. Der Reiter
trug nur den Schalter „sortiert"; die Hülle bekam gar keinen Bildauftrag zu sehen (die Weiche
reichte `a.Sortiert` weiter, nicht `a`). **Ein weiteres Bild hat der Reiter nicht** — es bleibt
bei diesem einen.

### Änderung

| Punkt | Was war | Was ist |
|---|---|---|
| Auswahl | keine | eine **zweite** Schalterzeile unter „sortiert", je Reihe ein `Schalter`, **alle vorbelegt an** |
| Reihenfolge der Zeilen | — | OBEN „sortiert" (die Darstellungsart), DARUNTER die Reihen — dieselbe Trennung wie im Wärmepumpenreiter (W11b‑B‑17) und im Bedarfsreiter (W11b‑B‑16) |
| Beschriftung | — | **dieselbe Ressource wie die Legende**: `CHART_LEGENDE_WAERMEPRODUKTION_HEIZKESSEL`, `CHART_SEGMENT_RESTWAERME`, `CHART_LEGENDE_WAERMEBEDARF_GESAMT` |
| Bildauftrag | `new Bildauftrag(Bilder.Heizkessel, _sortiert)` | dazu `Reihen` — sprachneutral `WAERMEPRODUKTION`/`RESTWAERME`/`WAERMEBEDARF` |
| Hülle | `BildKessel(bool sortiert)`, Weiche `BildKessel(a.Sortiert)` | `BildKessel(Bildauftrag a)` mit `Alle(a)`/`Gewaehlt(a, alle, …)`; die Weiche reicht `a` |
| Prüfhilfen | `Sortiert` | dazu `GewaehlteReihen` |

**Leer heißt keine, nicht alle** — wörtlich die Regel aus W11b‑B‑17: `Reihen = null` heißt „ohne
Angabe → alle", die **leere Liste** heißt „der Anwender hat alles abgewählt → keine Reihe". Der
Renderer zeichnet daraus seinen Leerhinweis; `ErzeugerStapel` kommt mit leerem Stapel **und**
leerer Linienliste zurecht (dieselbe Lage wie beim Solarbild, W11b‑B‑19). Achsen, Farben,
Stapelart, die Strichstärke im sortierten Zweig (`4f`) und die Ordnung der Reihen bleiben
unangetastet; der Zwischenspeicherschlüssel trennt die Auswahlstände schon
(`Bildauftrag.Schluessel` führt die Reihenliste mit).

### Nachweis

**UI**, `ErzeugerReiterTests` — neu: `Kessel_traegt_je_Reihe_einen_Schalter` (Zeile 0 trägt genau
„sortiert", Zeile 1 die drei Legendentexte in der Reihenfolge des Bildes, alle Kästchen an,
Prüfhilfe und Bildauftrag tragen die drei Schlüssel),
`Kessel_nimmt_die_abgewaehlte_Reihe_aus_dem_Bildauftrag`,
`Kessel_ohne_gewaehlte_Reihe_gibt_eine_leere_Liste`. Angepasst:
`Kessel_wechselt_den_Bildauftrag_mit_dem_Sortiertschalter` greift das Kästchen jetzt über seine
Schalterzeile (`Kasten(seite, 0, 0)`) und prüft zusätzlich, daß die drei Reihen beim Umschalten
dieselben bleiben.

**Nicht durch Tests gedeckt:** die Reihenfilterung IN der Hülle — `SimulationErgebnisHuelle.Bilder.cs`
liegt in `WindowsFormsApplication1`, und es gibt kein Testprojekt, das dort ein Bild zeichnet
(dieselbe Lage wie bei W11b‑B‑17/19). Die Filterung folgt zeichengleich den Vorbildern
`BildSolar`/`BildPv`; die Sichtabnahme am Programm steht aus.

## Anwenderwunsch 09.09.2026 — W11b‑B‑22: Kennzahlen des Heizkessels wie im Bedarfsreiter

**Wortlaut:** „Kennzahlen wie im Bedarfsreiter: zwei Spalten nebeneinander mit den dunklen Balken
‚Wärme' und ‚Strom' (statt der `h3`-Unterabschnitte untereinander), darunter ‚Auslegung' und
‚Brennstoffverbrauch der Spitzenkessel' als eigene Gruppen in einer zweiten Zeile (heute steht der
Brennstoffblock oben rechts versetzt); die Kesseltabelle über die volle Breite."

**Befund — drei Überschriftenarten in einem Block.** W11b‑B‑15 hatte die neun Zeilen in drei
`h3.epos-untergruppe` gegliedert — aber alle drei UNTEREINANDER in EINER Rasterspalte, während
der Brennstoffblock als einzige Gruppe mit dunklem Balken **daneben** stand. Damit lag der
Brennstoffverbrauch optisch auf der Höhe der Wärme, über die er nichts sagt, und die zwei
Kategorien, die der Reiterstapel sonst überall nebeneinander stellt (Übersicht, Bedarf, die zwei
Ringe), standen hier hintereinander.

### Änderung

| Punkt | Was war | Was ist |
|---|---|---|
| Gruppenart | `h3.epos-untergruppe` für Wärme/Strom/Auslegung, `Gruppenkopf` nur für den Brennstoff | **vier gleichrangige Hauptgruppen**, jede mit `Gruppenkopf` |
| Anordnung | eine Spalte mit drei Abschnitten, daneben der Brennstoffblock | **zwei Rasterzeilen** (`div.epos-simerg-spalten` je Zeile): oben `Wärme` \| `Strom`, unten `Auslegung` \| `Brennstoffverbrauch der Spitzenkessel` |
| Kesseltabelle | außerhalb des Rasters, volle Breite | **unverändert** — sie stand schon so da; der Kommentar sagt es jetzt |
| Balkentitel | `SIMERG_LBL_BRENNSTOFFVERBRAUCH_SPK` („… der Spitzenkessel**:**"), `SIMERG_LBL_WAERMEPRODUKTION_MODULE_SPK` („… Spitzenkessel**:**") | `SIMERG_GRP_BRENNSTOFF_SPK`, `SIMERG_GRP_MODULE_SPK` — **ohne Doppelpunkt**, wie jeder andere Gruppentitel |
| Zeilenfolge je Gruppe | W11b‑B‑15 | **unverändert**, keine Zahl fällt weg |
| Quellwärme, Einheit | `SIM_KESSEL_QUELLWAERME_EINHEIT` = „MWh" | `MWh/a` wie jede andere Wärmezeile (siehe W11b‑B‑23) |
| Leerer Brennstoffblock | dunkler Balken, darunter nichts | `Warnbanner` mit `SIM_MSG_KEIN_BRENNSTOFF_SPK` — dieselbe Form wie im BHKW-Reiter |

### Nachweis

**UI**, `ErzeugerReiterTests`: `Kessel_gliedert_seine_Felder_in_Waerme_Strom_und_Auslegung` heißt
jetzt `Kessel_gliedert_seine_Felder_in_vier_Gruppen_mit_Balken` und prüft die **fünf** Balkentexte
(die vier Gruppen plus den Titel über der Kesseltabelle, alle ohne Doppelpunkt), daß **kein** `h3`
übrig ist, daß **zwei** Rasterzeilen je zwei Listen führen, und unverändert die Zeilenfolge jeder
Gruppe samt den zwei betonten Restzeilen. Neu: `Kessel_meldet_einen_leeren_Brennstoffblock` und
`Kessel_nennt_die_Quellwaerme_in_MWh_je_Jahr`.

## Anwenderwunsch 09.09.2026 — W11b‑B‑23: Konsistenz der Darstellung über alle Reiter

**Wortlaut:** „Prüfe insgesamt die Inhalte der Tabs (Detaillierte Simulation) auf Konsistenz der
Darstellung und optimiere."

### 1. Bestandsaufnahme (Stand vor dieser Nacharbeit)

| Reiter | Kennzahlen | Gruppenart | Betonte Zeile | Zahlen | Tabellen | Diagrammsteuerung | Leerhinweis | Export |
|---|---|---|---|---|---|---|---|---|
| **Parameter** | Feldkästen (`epos-simerg-felder`), keine Kennzahlenliste | `Gruppenkopf` (4) | — | Eingabefelder | — | — | — | — |
| **Übersicht** | Wärme \| Strom nebeneinander | **Balken** | Rest je Gruppe | N2 | Eigenanteil (`epos-raster`), Namenszelle mit `class=""` | zwei Ringe, keine Schalter | `SIMERG_MSG_OHNE_BEDARF` je Ring | Sprungknopf |
| **Bedarf** | Wärme \| Strom nebeneinander, „je Bedarfsart" als `h3` | **Balken** + `h3` | **keine** — `Wertzeile` kannte `betont` nicht | N2; „Gesamt…" in **MWh**, die Zeile darüber in MWh/a | — | 1× „sortiert" ganz oben, Reihen bei der Wärme, **Datenzoom** an beiden Bildern | — | CSV + 2× „Details…" |
| **Wärmepumpe** | Wärme/Strom/Auslegung **untereinander** | `h3` | Rest | N2, Vbh N0 | Module, Puffer (`F1`) | „sortiert" + Reihen im Unterblatt, Reihen an der Streuwolke | `…_WAERMEPUMPE` | CSV |
| **Heizkessel** | Wärme/Strom/Auslegung untereinander, Brennstoff **daneben** | `h3` **und** Balken | Rest, Reststrom | N2, Nutzungsgrad `F1`; Quellwärme in **MWh** | Module, Balken darüber (mit Doppelpunkt) | nur „sortiert", **keine Reihenwahl** | `…_HEIZKESSEL` | CSV |
| **BHKW** | Wärme/Strom/Betrieb untereinander, Brennstoff daneben | `h3` **und** Balken | Rest, Reststrom | N2, Vbh N0; zwei Vbh-Beschriftungen **ohne Doppelpunkt** | Module | nur „sortiert", **keine Reihenwahl** | `…_SIMULATION` (allgemein) | — |
| **Solarthermie** | eine Gruppe „Wärme" | `h3` | Rest | N2 | Kollektoren | Reihen, kein „sortiert" | `…_SIMULATION` (allgemein) | — |
| **Photovoltaik** | Erzeugung/Bedarf+Deckung/Einstrahlung untereinander | `h3` | Rest | N2, Volllast N0, WR-Nutzungsgrad `F4` | WR, Module | Reihen (4), kein „sortiert" | `…_SIMULATION` (allgemein) | — |
| **Stromspeicher** | Tabelle `epos-simerg-kennzahlen` mit Gruppen-/Untergruppenzeilen | Tabellenzeilen | Summe/Ergebnis (Kern) | aus dem Kern | Kennzahlentabelle | kein Schalter | Kopfzeile `epos-simerg-status` | CSV + Vergleich |
| **Ergebnis** | Autarkiekacheln + `meter` | — | — | **F1** für die zwei Quoten, N0 für kg/kWh | — | ein Bild ohne Schalter | — | — |
| **Wärmegang** | keine | — | — | — | — | Auswahlfeld + „sortiert" + Bedarfslinie, Mehrfachauswahl, **Datenzoom** | — | CSV |
| **Stromgang** | keine | — | — | — | — | „sortiert" oben, Mehrfachauswahl, **Datenzoom** | — | CSV |

Quer durch alle Tabellen: **`.epos-simerg-zahl` war in `.epos-raster` ohne Regel.** Die einzige
Regel dazu stand unter `.epos-simerg-kennzahlen td.epos-simerg-zahl` und galt damit nur für die
Kennzahlentabelle des Stromspeichers — elf Tabellen behaupteten mit ihrer Klasse eine
Rechtsbündigkeit, die es nicht gab.

### 2. Das Muster

Es steht kurz und vollständig in **`Doku_Simulationsergebnis_Darstellung.md`** (Repowurzel) und
gilt für jeden künftigen Reiter. Kern in sieben Sätzen:

1. Reihenfolge im Reiter: Leerhinweis → Kennzahlen → Tabellen → Diagramme → Export.
2. Jede fachliche Gruppe ist eine **Hauptgruppe mit dunklem Balken** (`Gruppenkopf`), Titel aus
   `SIMERG_GRP_*` **ohne** Doppelpunkt. `h3.epos-untergruppe` nur INNERHALB einer Hauptgruppe.
3. **Erste Rasterzeile `Wärme` | `Strom`**, alles Weitere in einer **zweiten** Rasterzeile
   (eigenes `div.epos-simerg-spalten`). Steht eine Gruppe allein in ihrer Zeile, trägt ihre
   `section` `epos-simerg-kennzahlenzeile`.
4. Kennzahlenliste dreispaltig: Beschriftung (`SIMERG_LBL_*`, **mit** Doppelpunkt) · Zahl ·
   Einheit — die Einheit **nur** in ihrer Spalte.
5. Zahlen **N2**, Stunden und Zyklen **N0**, kein `F2`; die **Rest- bzw. Summenzeile** schließt
   ihre Gruppe betont ab (`betont: true`), sonst **kein** Klassenattribut (`null`, nicht `""`).
6. Je Bild dieselbe Steuerzeile: „sortiert" → Reihenwahl (Legendenressourcen, `Bildauftrag.Reihen`,
   `null` = alle / leer = keine) → `ChartBild` (die Zoomleiste bringt der Baustein mit).
7. Tabellen `epos-raster` über die volle Breite, Zahlenzellen `epos-simerg-zahl`; die
   Kennzahlentabelle des Stromspeichers bleibt `epos-simerg-kennzahlen`.

### 3. Abweichungen und was daraus wurde

| Reiter | Abweichung | Änderung |
|---|---|---|
| **alle mit Tabelle** | `.epos-simerg-zahl` ohne Regel in `.epos-raster` — elf Tabellen linksbündig | **eine** neue Stilregel `.epos-raster td/th.epos-simerg-zahl { text-align: right; font-variant-numeric: tabular-nums }` |
| **Heizkessel** | siehe W11b‑B‑21/22 | vier Balken in zwei Zeilen, Reihenwahl, Leerhinweis im Brennstoffblock, Quellwärme in MWh/a |
| **BHKW** | Wärme/Strom/Betrieb als `h3` untereinander; einziges Bild ohne Reihenwahl; Brennstofftitel mit Doppelpunkt; zwei Vbh-Beschriftungen ohne Doppelpunkt; allgemeiner Leerhinweis | vier Balken in zwei Zeilen wie beim Kessel; **Reihenwahl mit vier Reihen** (`WAERMEPRODUKTION`, `SPEICHERLADUNG`, `RESTWAERME`, `WAERMEBEDARF`) unter „sortiert"; `SIMERG_GRP_BRENNSTOFF`; Doppelpunkte ergänzt; `SIM_MSG_KEINE_DATEN_BHKW` |
| **Wärmepumpe** | Wärme/Strom/Auslegung als `h3` untereinander | Balken; `Wärme` \| `Strom` in Zeile 1, `Auslegung` (weiter mit `epos-simerg-kennzahlenzeile`) über der Streuwolke in Zeile 2 |
| **Solarthermie** | Gruppe „Wärme" als `h3`; allgemeiner Leerhinweis | Balken; `SIM_MSG_KEINE_DATEN_SOLARTHERMIE` |
| **Photovoltaik** | drei Gruppen als `h3` untereinander; allgemeiner Leerhinweis | Balken; `Erzeugung` \| `Bedarf und Deckung` in Zeile 1, `Einstrahlung` (mit `kennzahlenzeile`) über dem Bild in Zeile 2; `SIM_MSG_KEINE_DATEN_PHOTOVOLTAIK` |
| **Bedarf** | keine betonte Zeile (die `Wertzeile` kannte den Schalter nicht); „Gesamter Wärme-/Strombedarf" in **MWh**, obwohl dieselbe DTO-Größe eine Zeile höher MWh/a trägt; vier Beschriftungen ohne Doppelpunkt | `betont` samt `Zeilenklasse`/`Einheitsklasse` wie in den übrigen Reitern; die zwei Summenzeilen **betont** und in MWh/a; Doppelpunkte ergänzt |
| **Übersicht** | Namenszelle des Eigenanteilsrasters mit `class=""` | `null` statt `""` — dieselbe Regel wie an der betonten Zeile |
| **Ergebnis** | die zwei Autarkiequoten in `F1`, während Übersichtskacheln und alle Listen N2 setzen | `N2`; der `meter`-Wert bleibt `F1`/invariant (Attribut, keine Anzeige) |
| **Stromspeicher, Parameter, Wärmegang, Stromgang** | keine Abweichung vom Muster gefunden | unverändert |

### 4. Ressourcen

**Neu (de/en):** `SIMERG_GRP_BRENNSTOFF` (Brennstoffverbrauch / Fuel consumption) ·
`SIMERG_GRP_BRENNSTOFF_SPK` (Brennstoffverbrauch der Spitzenkessel / Fuel consumption of the top
boilers) · `SIMERG_GRP_MODULE_SPK` (Wärmeproduktion der einzelnen Spitzenkessel / Heat production
of the individual top boilers) · `SIM_MSG_KEIN_BRENNSTOFF_SPK` · `SIM_MSG_KEINE_DATEN_BHKW` ·
`SIM_MSG_KEINE_DATEN_SOLARTHERMIE` · `SIM_MSG_KEINE_DATEN_PHOTOVOLTAIK`.

**Geändert (Doppelpunkt ergänzt, de + en):** `SIMERG_LBL_MAX_WAERMELAST`,
`SIMERG_LBL_GESAMT_WAERMEBEDARF`, `SIMERG_LBL_MAX_STROMBEDARF`, `SIMERG_LBL_GESAMT_STROMBEDARF`,
`SIM_BHKW_VBH_TH_SUMME`, `SIM_BHKW_VBH_TH_MITTEL`. `SIMERG_LBL_MAX_WAERMELAST` steht auch im
Gebäudebedarfsdialog (`GebaeudeHuelle` → `GebaeudeBedarfDialog`) — dort tragen die
Nachbarbeschriftungen („Wärmebedarf Heizung:", „Vollbenutzungsstunden:") den Doppelpunkt
ebenfalls, die Änderung räumt also auch dort auf.

**Unbenutzt geworden, bleiben im Katalog:** `SIMERG_LBL_BRENNSTOFFVERBRAUCH`,
`SIMERG_LBL_BRENNSTOFFVERBRAUCH_SPK`, `SIMERG_LBL_WAERMEPRODUKTION_MODULE_SPK`,
`SIM_KESSEL_QUELLWAERME_EINHEIT` — entfernt wird nichts, was ein anderer Strang aufgreifen könnte
(dieselbe Handhabung wie in W11b‑B‑19).

`Resource.Designer.cs` ist mit `python Werkzeuge/ResourceDesigner/designer_neu.py schreiben` neu
erzeugt (5 294 Einträge, +7 neu, 6 geändert, zweiter Lauf ±0).

### 5. Nachweis

**UI**, neu — `ErzeugerReiterTests`: `Kessel_traegt_je_Reihe_einen_Schalter`,
`Kessel_nimmt_die_abgewaehlte_Reihe_aus_dem_Bildauftrag`,
`Kessel_ohne_gewaehlte_Reihe_gibt_eine_leere_Liste`, `Kessel_meldet_einen_leeren_Brennstoffblock`,
`Kessel_nennt_die_Quellwaerme_in_MWh_je_Jahr`, `Bhkw_traegt_je_Reihe_einen_Schalter`,
`Bhkw_nimmt_die_abgewaehlte_Reihe_aus_dem_Bildauftrag`,
`Die_Leerhinweise_nennen_die_fehlende_Komponente`. `BedarfReiterTests`:
`Die_Summenzeile_schliesst_jede_Spalte_betont_ab`,
`Jede_Beschriftung_der_zwei_Gruppen_endet_auf_einen_Doppelpunkt` (die Kanalzeilen bleiben
ausgenommen — ihre Beschriftungen sind Namen aus dem Lauf). `UebersichtReiterTests`:
`Das_Eigenanteilsraster_traegt_kein_leeres_Klassenattribut`. `GangUndErgebnisReiterTests`:
`Die_Autarkiequoten_stehen_mit_zwei_Nachkommastellen`. `StilblattTests`:
`W11bB23_Die_Zahlenspalten_der_Ergebnistabellen_stehen_rechts` (bunit rechnet keine Stilblätter
aus — der Fall liest die REGEL, wie die Wachen zu W6‑B‑1 und W6‑B‑4).

**Umbenannt und angepasst:** `Kessel_gliedert_seine_Felder_in_Waerme_Strom_und_Auslegung` →
`Kessel_gliedert_seine_Felder_in_vier_Gruppen_mit_Balken`,
`Die_zehn_Felder_stehen_in_drei_Unterabschnitten` →
`Die_zehn_Felder_stehen_in_drei_Gruppen_mit_Balken` (WaermepumpeReiterTests). **Angepasst:**
`Bhkw_gliedert_seine_Felder_in_Waerme_Strom_und_Betrieb`,
`Solarthermie_gliedert_ihre_Felder_und_betont_den_Rest`,
`Photovoltaik_gliedert_seine_Felder_in_Erzeugung_Bedarf_und_Einstrahlung`,
`Kessel_wechselt_den_Bildauftrag_mit_dem_Sortiertschalter`.

### 6. Bewußt NICHT vereinheitlicht

* **Der Datenzoom** („Bereich"-Knopf, aufgezogenes Rechteck) steht weiter nur an den drei
  Ganglinien, deren Hülle ein `Achsenfenster` an den Renderer reicht (Bedarf, Wärmegang,
  Stromgang). Die **Zoomleiste** selbst (`×1`, `1:1`) hat jedes Bild — sie kommt aus dem Baustein
  `Diagramm`, und das ist die Hausregel seit A‑1. Die Streuwolke „Leistung über
  Außentemperatur" hat gar keine Zeitachse; die übrigen Ganglinien könnten den Zoom bekommen
  (`ErzeugerStapel` nimmt `fenster` bereits entgegen), das ist aber ein Eingriff in die
  Bildbestellung der Hülle und keine Frage der Darstellung mehr. **Offener Punkt.**
* **Zahlenformate in Tabellen** (`F1` für Jahresnutzungsgrad, Speicherkapazität, Vollzyklen und
  Füllstand, `F4` für den Wechselrichter-Nutzungsgrad): Das sind Genauigkeiten des Vorbilds. `N2`
  würde „91,70 %" schreiben, wo der Kessel 91,7 % liefert, und die vierte Stelle des
  Wechselrichters ganz verlieren.
* **Gruppenbalken über Tabellen**: nur die Kesseltabelle trägt einen
  (`SIMERG_GRP_MODULE_SPK`). Die übrigen Modultabellen nennen sich in ihrer **ersten
  Spaltenüberschrift** (`SIM_SPALTE_MODUL`, `SIM_ERZEUGERNAME_BHKW`, `SIM_SPALTE_SOLARKOLLEKTOR`,
  `SIM_PHOTOVOLTAIK`, `SIMERG_TITEL_PV_WR`) — sechs neue Titel zu erfinden, nur damit überall ein
  Balken steht, hätte Text ohne Aussage erzeugt. **Offener Punkt**, falls der Anwender die Balken
  will.
* **Die Knopfzeile des Stromspeichers** (CSV + „Vergleichen") steht in einem
  `div.epos-simerg-schalter` unter dem SoC-Bild statt am Blockende. Der Name der Klasse paßt
  nicht, die Anordnung aber schon: Die zwei Knöpfe gehören zum Bild darüber und wurden mit
  W11b‑B‑14 gerade dorthin gestellt.
* **Die Prozentzeichen der Ergebniskacheln** stehen im Wert („38,10 %"), nicht in einer
  Einheitsspalte — eine `Kennzahlkachel` hat keine.

### Zahlen

Sandbox: Build **0 Fehler**, Kern **2137/2137**, UI **3337/3337** (vorher 3324; +13 UI, Kern
unverändert). Kein Rechenweg berührt — dieselben Zahlen aus demselben Lauf, nur anders gruppiert,
anders gesetzt und in wählbaren Reihen. Die Sichtabnahme am Programm steht aus.
