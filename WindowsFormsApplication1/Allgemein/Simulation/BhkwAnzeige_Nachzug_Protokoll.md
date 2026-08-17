# BHKW-Anzeige — Nachzug auf den Speicherstufen-Rechenweg (Umsetzungsprotokoll)

Stand: 17.08.2026 · Grundlage: zwei Meldungen des Anwenders aus dem Live-Test vom
17.08.2026 · Vorarbeit: [`PaketBHKW_Regulaer_Protokoll.md`](PaketBHKW_Regulaer_Protokoll.md),
dort **offener Punkt 2** („Anzeige-Altformeln `Form_Simulation_Detail.cs:2953-2960` sind
nicht nachgezogen") — er ist mit diesem Nachzug geschlossen.

**Nicht committet.** Der Anwender synct selbst.

**Kernaussage in einem Satz.** Die Ergebnisseite „Detaillierte Simulation → BHKW" rechnet
ab jetzt mit denselben Größen wie `Tab_ErgebnisBHKW` — Restwärme und Deckungsgrad aus dem
Eigenanteil statt aus der Vektordifferenz —, sie weist den Pufferbeitrag als eigene
Chartserie und zwei Kennzahlen aus, und ihre Ganglinie ist wieder eine Ganglinie, mit dem
Umschalter „sortiert" der Heizkessel-Seite.

**Das Projekt des Live-Tests ist Projekt 1018.** „BHKW Test München" ist derselbe
Datensatz, der im Paket BHKW-Regulär als Referenzprojekt diente — nachgewiesen über
`Referenzlauf liste` (Kapitel 6.1). Ein neu angelegtes Projekt war es nicht; die
DB-Kopie musste nicht neu gezogen werden. Die Zahlen des Anwender-Screenshots
(„EC_Power_20kw.el_Gas · 25,61 MWh/a Wärme · 13,23 MWh/a Strom") sind im Messlauf
bitgenau reproduziert.

---

## 1. Die beiden Meldungen — und was wirklich dahinter stand

### 1.1 „Der Pufferspeicher wird noch nicht berücksichtigt"

**Er wird berücksichtigt — der Kern tut es, die Seite zeigte es nicht.** Gemessen an
Projekt 1018:

| Größe | Wert | stand auf der Seite? |
|---|---|---|
| `waermeproduktion` (brutto) | 25 605,69 kWh | ja |
| davon `Direktdeckung_gesamt` | 11 284,64 kWh | nein |
| davon `Speicherladung_gesamt` | **14 321,04 kWh** (56 % der Produktion) | **nein** |
| davon `Waermeueberschuss` | 0,0003 kWh | ja |
| `Speicherentladung_Anteil` | **14 106,72 kWh** | **nein** |

56 % der Jahresproduktion gingen in den Puffer, und 14,11 MWh Deckung kamen aus ihm — keine
dieser Zahlen war ablesbar. Die beiden Kennzahlen, die die Seite ausgewiesen hat, waren
zudem mit den Altpfad-Formeln gebildet:

- **Restwärme** als Vektordifferenz `SubVectors(waermebedarf, waermeproduktion)` — der
  Bilanzfehler aus Konzept 6.5. Geladene Wärme deckt noch keinen Bedarf, entladene deckt
  Bedarf, ohne in der Produktionsstunde zu stehen.
- **Deckungsgrad** als `Produktion / Projektbedarf` — damit wies die Seite Wärme als
  Deckung aus, die noch im Speicher lag.

`Tab_ErgebnisBHKW` rechnet seit Paket 6 richtig (`SimulationRunner.cs:381-449`); nur die
Seite tat es nicht. Die Anzeige wich also von der Ergebnistabelle desselben Laufs ab.

### 1.2 „Die Darstellung ‚sortiert' fehlt"

Sie fehlte nicht nur — **sie war die einzige.** `Form_Simulation_Detail.cs:2941-2944`
sortierte Bedarf und Produktion unbedingt absteigend (`OrderByDescending`), trug darüber
aber den Titel „Wärmelast Jahresganglinie" und beschriftete die X-Achse mit
„Jahresstunden".

**Damit ist der gemeldete „harte Abfall auf 0 nach etwa 1460 h" erklärt:** Das BHKW von
1018 hat genau **1505 Stunden** mit Produktion > 0. Eine Dauerlinie MUSS dort auf 0 fallen.
Ein Speicherfehler war das nicht — wohl aber eine Darstellung, die als Ganglinie gelesen
werden musste und keine war.

---

## 2. Das Vorbild: wie „sortiert" auf der Heizkessel-Seite umgesetzt ist

Analysiert an `Form_Simulation_Detail.cs` (Zeilennummern im Stand VOR diesem Nachzug):

| Element | Umsetzung | Datei:Zeile |
|---|---|---|
| Steuerelement | **CheckBox**, programmatisch (`checkBox_Kessel_sortiert`), Designer und .resx unangetastet | `:462-482` |
| Wortlaut | `MyResource.Resource.SIM_CHK_SORTIERT` = „sortiert" — wortgleich mit der Bestands-CheckBox der Wärmepumpen-Seite | `:468` |
| Schrift | `checkBox_WP_sortiert.Font` (übernommen, nicht neu gesetzt) | `:470` |
| Platz | rechts oben AM Diagramm: `chart.Right - 90`, `chart.Top + 8`; `BackColor = chart.BackColor` (WinForms-Transparenz nimmt den Hintergrund des Elternelements, nicht des Nachbarn); `BringToFront()` | `:476-482` |
| Sichtbarkeit | nur wenn das Ergebnis die Komponente führt (`ErgebnisPraesenz.Ermitteln(sim).Heizkessel`) | `:700` |
| **Grundzustand** | **chronologisch** — die CheckBox ist ungesetzt, „sortiert" ist die Umschaltung | `:479` |
| Umschalten | `CheckedChanged` → Diagramm komplett neu aufbauen; Wächterfeld `_kesselChartAktiv` statt `chart.Visible` (dessen Getter liefert false, solange ein Elternelement nicht sichtbar ist) | `:1002-1006`, `:53-63` |
| Aufbaufolge | `XAxisAsNumber = sortiert` → `HardReset()` → `Init()` → Serien neu | `:776-789` |
| Achsentitel | sortiert `CHART_ACHSE_JAHRESSTUNDEN`, chronologisch `CHART_ACHSE_MONATE` | `:777-779` |
| Sortierregel | **ausschließlich** `GanglinienDarstellung.Anzeigewerte(v, sortiert)` — jede Serie **für sich** absteigend, auf einer **Kopie**; chronologisch kommt der Originalvektor zurück | `GanglinienDarstellung.cs:23-49` |
| Serientyp | `GanglinienDarstellung.Stapeltyp(sortiert)`: chronologisch `StackedColumn`, sortiert `FastLine` (in der Dauerlinie wäre eine Summe frei erfunden) | `GanglinienDarstellung.cs:51-72` |
| Lesbarkeit | in der Dauerlinie `BorderWidth = 4` für die untere von zwei möglicherweise punktgleichen Linien (Strichelung greift bei `FastLine` nicht) | `:798-806` |

**Antwort auf die Auftragsfrage:** Der Kessel hat einen **Umschalter**, nicht „immer
sortiert" — und er sortiert **jede Serie unabhängig**. Beides ist übernommen.

---

## 3. Die Umstellung

### 3.1 Geänderte Stellen

| Datei:Zeile (Endstand) | Änderung |
|---|---|
| `Form_Simulation_Detail.cs:144-151` | neuer Serienschlüssel `S_SPEICHERLADUNG` mit Begründung, warum er nicht stapelt |
| `Form_Simulation_Detail.cs:66-91` | Felder: `checkBox_BHKW_sortiert`, `_chartBhkwManager`, `_bhkwChartAktiv`, sechs Steuerelemente der zwei Kennzahlzeilen |
| `Form_Simulation_Detail.cs:335-337` | Aufruf `InitBhkwChart()` im Konstruktor, direkt hinter `InitKesselChart()` |
| `Form_Simulation_Detail.cs:1081-1133` | Blockkommentar: Ausgangslage, Bedienmuster, Bezugsgröße des Bedarfs |
| `Form_Simulation_Detail.cs:1135-1163` | `InitBhkwChart()` — der Umschalter, Steuerelement für Steuerelement wie beim Kessel |
| `Form_Simulation_Detail.cs:1165-1211` | `InitBhkwSpeicherzeilen()` — die zwei Kennzahlzeilen |
| `Form_Simulation_Detail.cs:1213-1240` | `BhkwZeilenNachruecken()` — die rechte Spalte rückt um zwei Zeilenhöhen nach |
| `Form_Simulation_Detail.cs:1242-1310` | `BhkwKennzahlZeile()` — eine Zeile (Beschriftung/Feld/Einheit) aus den Maßen der Nachbarzeile |
| `Form_Simulation_Detail.cs:1312-1330` | `BreiteMessen()` — gemessene Beschriftungsbreite (siehe 3.5) |
| `Form_Simulation_Detail.cs:1332-1364` | `TextAusFormResx()` + `_formTexte` — die drei neuen Texte aus der formulareigenen .resx |
| `Form_Simulation_Detail.cs:1366-1404` | `BhkwErgebnisAnzeigen()`, `BhkwSpeicherzeilenSichtbar()` — Präsenzregel wie beim Kessel |
| `Form_Simulation_Detail.cs:1406-1490` | `BhkwSerienAufbauen()` — die vier Serien |
| `Form_Simulation_Detail.cs:1492-1500` | `checkBox_BHKW_sortiert_CheckedChanged` |
| `Form_Simulation_Detail.cs:3359-3452` | der Anzeigeblock: `_chartManager[10]`-Aufbau ersetzt durch `BhkwErgebnisAnzeigen()`; Stufeneingang, Eigenanteil, Restwärme, Deckungsgrad, die zwei Speicher-Kennzahlen |
| `Form_Simulation_Detail.cs:3454-3461` | Modultabelle: geprüft und **unverändert** (Begründung im Kommentar) |
| `Form_Simulation_Detail.resx` | drei Textknoten `SIMDET_BHKW_SPEICHERLADUNG`, `SIMDET_BHKW_SPEICHERDECKUNG`, `SIMDET_BHKW_SERIE_SPEICHERLADUNG` (deutsch, vor `</root>`) |
| `Form_Simulation_Detail.en-US.resx` | dieselben drei Knoten englisch |

**Nicht angefasst:** `Form_Simulation_Detail.Designer.cs`, `MyResource\Resource.*`, jede
Datei des Rechenkerns. Der Kern brauchte **keine** neue Größe — alle vier Ganglinien und
alle sechs Skalare der neuen Anzeige waren bereits öffentliche Felder von
`SimulationBHKW`.

### 3.2 Was die Seite jetzt zeigt

**Vier Chartserien**, alle unmittelbar aus dem Kern, Zeichenreihenfolge von unten nach oben:

| Serie | Quelle | Typ chronologisch / sortiert | Farbe |
|---|---|---|---|
| Wärmeproduktion | `waermeproduktion` (BRUTTO: Direktdeckung + Speicherladung + Überschuss) | `StackedColumn` (Gruppe „Produktion") / `FastLine` (BorderWidth 4) | Blau |
| **Speicherladung** (neu) | `Speicherladung_stuendlich` | `FastLine` / `FastLine` | DarkOrange |
| Restwärme | `waermerestbedarf` — **die Ganglinie des Kerns** statt der Vektordifferenz | `FastLine` | Grün |
| Wärmebedarf | `waermebedarf` (Stufeneingang), zuletzt und damit oben | `FastLine` | Rot |

**Warum die Speicherladung keine Stapelserie ist.** Sie ist ein **Teil** der Produktion,
nicht ihre Ergänzung. In derselben Stapelgruppe zeigte das Bild „Produktion + Ladung" und
damit die Ladung doppelt; in einer zweiten Stapelgruppe stellt MS-Chart die Säulen
NEBENEINANDER — bei 8760 Punkten auf 575 Bildpunkten verschwinden dann beide in der
Rasterung (derselbe Befund, den die Heizkessel-Seite dokumentiert). Als Linie über den
Säulen ist sie ablesbar: Sie liegt zwischen 0 und der Oberkante der Produktion, und der
Abstand nach oben ist die unmittelbar gedeckte Wärme.

**Kennzahlen (rechte Spalte).** Geändert sind drei Bestandsfelder, neu sind zwei Zeilen:

| Feld | vorher | jetzt |
|---|---|---|
| `textBox_Waermebedarf_BHKW` | `waermebedarf.Sum()/1000` | `Waermebedarf_gesamt/1000` (double-Jahressumme statt 8760 float-Additionen — wortgleich mit `SimulationRunner:381-383`) |
| `textBox_Restwaermebedarf_BHKW` | `SubVectors(Bedarf, Produktion).Sum()/1000` | `Stufeneingang − Eigenanteil`, auf 0 geklemmt |
| `textBox_Waermedeckung` | `Produktion · 100 / Projektbedarf` | `Eigenanteil · 100 / Projektbedarf`, auf 0…100 geklemmt |
| `tb_BhkwSpeicherladung` | — | **„davon in den Speicher"** = `Speicherladung_gesamt/1000` |
| `tb_BhkwSpeicherdeckung` | — | **„aus dem Speicher gedeckt"** = `Speicherentladung_Anteil/1000` |

`Eigenanteil = Direktdeckung_gesamt + Speicherentladung_Anteil` — dieselbe Größe, aus der
`Tab_ErgebnisBHKW.Restwaermebedarf` und `.Waermebedarfsdeckung` entstehen. Restbedarf und
Deckung sind damit zwei Seiten derselben Rechnung.

**Die Modultabelle blieb unverändert** — geprüft, nicht unterstellt: `s_waerme_MWh` und
`s_strom_MWh` sind genau die Felder, aus denen der Runner `Tab_ErgebnisBHKWModul` füllt
(`SimulationRunner:498-511`). Sie deckt sich also ohne Eingriff mit dem Kern.

### 3.3 Der Altpfad-Zweig bleibt stehen

Alle drei geänderten Formeln sind an `sim.KaskadeZweikanalig` gehängt und fallen sonst auf
den alten Ausdruck zurück — genau wie im Runner. Der Zweig ist auf einem BHKW-Projekt seit
dem Paket BHKW-Regulär **unerreichbar** (die Weiche setzt das Feld für jedes BHKW-Projekt),
er ist aber die Symmetrie zum Runner: Beide Stellen entscheiden nach demselben Feld nach
derselben Regel. Ein Ausdruck ohne diese Bedingung hätte im Altpfad 0 geliefert
(`Direktdeckung_gesamt` und `Speicherentladung_Anteil` sind dort exakt 0).

### 3.4 Bezugsgröße des Bedarfs — bewusste Abweichung vom Kessel

Die Heizkessel-Seite zeigt als Bedarfslinie den **Projektbedarf**. Die BHKW-Seite behält
den **Stufeneingang** (`waermebedarf`). Grund: Die Restwärme-Ganglinie des Kerns ist
stundenweise als `Stufeneingang − Direktdeckung − zugerechnete Entladung` definiert
(`SimulationBHKW.Stunde_Ende`). Mit dem Projektbedarf als Linie stünde im Bild eine
Bezugsgröße, gegen die die anderen Serien nicht gerechnet sind — die Summe
„Rest + Direktdeckung + Entladung" ginge sichtbar nicht auf. Der PROJEKTbedarf bleibt die
Bezugsgröße des Deckungsgrades (so weist ihn der Kern aus) und steht als Zahl auf der
Seite. Steht dem BHKW ein anderer Erzeuger voran, liegt die gezeigte Bedarfslinie unter der
Projektwärmelast — bei 1018 fallen beide zusammen (46,87 MWh gegen 46,87 MWh), weil das
BHKW erste Stufe ist. Als offener Punkt vermerkt.

### 3.5 Layout

Der Wärmeblock der rechten Spalte endet im Entwurf bei y≈205, die nächste Zeile
(„Stromproduktion") beginnt bei y=236. Für EINE Zeile ist Platz, für zwei nicht. Die Zeilen
darunter rücken deshalb um zwei Zeilenhöhen (2 × 32 px) nach — derselbe Eingriff wie das
„+32" der Pufferspeicher-Maske, nur **zur Laufzeit** und damit ohne .resx-Änderung.

- Die Spalte wird über die **Waagerechte** abgegrenzt (`Left >= chart.Right + 8`), nicht
  über eine Liste von Steuerelementnamen: Diagramm und Modultabelle links reichen bis
  y≈693 und dürfen sich nicht bewegen; eine Namensliste wäre bei der nächsten
  Designer-Änderung still falsch (dieselbe Begründung wie bei `KesselSeiteAnordnen`).
- Maße, Schrift und Farben der neuen Zeilen kommen **gemessen** von der Nachbarzeile
  („Wärmeüberschuß") über `NachbarZeile` — Muster `InitKesselQuellwaerme`.
- Die **Einheit** wird von der Nachbarzeile ÜBERNOMMEN statt neu getextet; sie kann damit
  nicht von ihr abweichen.
- Die Beschriftungsbreite wird mit `TextRenderer.MeasureText` **gemessen**: „aus dem
  Speicher gedeckt:" ist länger als jede Entwurfsbeschriftung der Spalte (137 px) und wäre
  in deren Breite abgeschnitten. Die rechte Kante bleibt die der Nachbarzeile (dort endet
  der rechtsbündige Text), nach links wird höchstens bis an das Diagramm herangerückt.

Gemessen nach dem Nachrücken (Kapitel 6.4): **0 Überlappungen** sichtbarer Elemente,
unterster Rand y=696 gegen eine Entwurfshöhe von 721.

### 3.6 Die drei neuen Texte

Sie stehen in `Form_Simulation_Detail.resx` (deutsch, neutral) und
`Form_Simulation_Detail.en-US.resx` (englisch) und werden über
`ComponentResourceManager(typeof(Form_Simulation_Detail)).GetString(...)` gelesen — derselbe
Basisname, den `InitializeComponent` benutzt (`Designer.cs:31`), also dieselbe
.resx-Familie und derselbe Satellitenmechanismus beim Sprachwechsel.

**Warum nicht der Katalog `MyResource\Resource.resx`.** Programmatische Steuerelemente
nehmen dort sonst ihren Text (so macht es `InitKesselChart`). Der Katalog war zur
Umsetzungszeit für parallele Arbeit gesperrt; die formulareigene .resx ist der zweite
vorgesehene Ort. Jeder Aufruf trägt denselben deutschen Wortlaut als **Rückfall im Code**:
`GetString` liefert `null`, wenn ein Eintrag fehlt, und ein leeres Etikett wäre eine stille
Fehlanzeige. Der Umzug in den Katalog steht als offener Punkt.

Der Text des Umschalters ist **kein** neuer Text: `MyResource.Resource.SIM_CHK_SORTIERT`
wird nur gelesen — wortgleich mit Kessel- und Wärmepumpen-Seite.

---

## 4. Was NICHT geändert wurde, und warum

| Punkt | Begründung |
|---|---|
| Rechenkern (`SimulationBHKW.cs`, `SimulationControl.cs`, `Kaskadenschleife.cs`, `SimulationPufferspeicher.cs`) | keine Anzeigegröße fehlte — alle vier Ganglinien und alle sechs Skalare waren schon öffentlich. Nur gelesen. |
| `Form_Simulation_Detail.Designer.cs` | der Umschalter und die zwei Zeilen entstehen programmatisch, wie beim Kessel |
| `MyResource\Resource.*` | parallele Arbeit; die drei neuen Texte gingen in die formulareigene .resx (3.6) |
| Serie „Überschuss" | die Jahressumme steht als Zahl auf der Seite und ist bei 1018 praktisch 0 (0,0003 kWh). Eine fünfte Serie hätte das Bild belastet, ohne etwas zu sagen. Offener Punkt. |
| CSV-Ausgabe der BHKW-Seite | es gibt keine; der Kessel hat eine. Nicht Teil des Auftrags, als offener Punkt vermerkt. |
| `_chartManager`-Array (Größe 11) | Index 10 ist jetzt unbenutzt. Das Array zu verkleinern hätte alle zehn übrigen Indizes verschoben — Risiko ohne Nutzen. |

---

## 5. Encoding

Je Datei vor und nach jedem Eingriff gemessen (`BOM`, Anzahl `CRLF`, Anzahl reiner `LF`):

| Datei | vorher | nachher |
|---|---|---|
| `Views/Simulation/Form_Simulation_Detail.cs` | BOM, 6031 CRLF, 0 LF | BOM, 6520 CRLF, **0 LF** |
| `Views/Simulation/Form_Simulation_Detail.resx` | BOM, 9259 CRLF, 0 LF | BOM, 9273 CRLF, **0 LF** |
| `Views/Simulation/Form_Simulation_Detail.en-US.resx` | BOM, 916 CRLF, 0 LF | BOM, 926 CRLF, **0 LF** |
| `Allgemein/Simulation/BhkwAnzeige_Nachzug_Protokoll.md` (diese Datei) | — | **kein BOM, reine LF** |

Die .resx-Eingriffe liefen byteweise (Einfügen vor `</root>`, Lesen/Schreiben im
Binärmodus), damit BOM und Zeilenenden der 387-KB-Datei unangetastet bleiben.

---

## 6. Verifikation

### 6.1 Projektzuordnung

`Referenzlauf.exe liste %TEMP%\wpk9` auf der frisch aus
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` gezogenen Kopie:

```
1018   BHKW Test München    Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer
```

„BHKW Test München" **ist** Projekt 1018 — kein neues Projekt, keine zweite DB-Kopie
nötig. Die Produktiv-DB wurde ausschließlich **gelesen und kopiert**.

Migration auf der Kopie: `Tab_Pufferspeicher 8/8 Spalten vorhanden`, Schemastand bereits
13. Die zwei bekannten Datenstand-Vorbefunde stehen unverändert
(`PufferHeizung ohne WS_ID_Puffer: 2`, `Anlagen ohne Ladeprio-Vorgabe: 1`).

### 6.2 Zahlenbeleg Projekt 1018 — alt gegen neu gegen Kern

Messwerkzeug: `%TEMP%\wpk9\BhkwAnzeigeHarness` (Muster `%TEMP%\wpk5`, x86, Debug,
Projektreferenz auf `WindowsFormsApplication1.csproj`, `DialogWaechter` des
Referenzlauf-Werkzeugs mitkompiliert). Ergebnis-Kopf 171.

| Anzeigewert | ALT-Anzeige | **NEU-Anzeige** | `Tab_ErgebnisBHKW` |
|---|---|---|---|
| Wärmebedarf [MWh/a] | 46,8693 | **46,87** | 46,87 |
| Wärmeproduktion [MWh/a] | 25,6057 | **25,61** | 25,61 |
| Restwärmebedarf [MWh/a] | 21,4731 | **21,48** | **21,48** |
| Wärmebedarfsdeckung [%] | 54,6321 | **54,17** | **54,17** |
| davon in den Speicher [MWh/a] | *nicht vorhanden* | **14,32** | (`Speicherladung_gesamt`) |
| aus dem Speicher gedeckt [MWh/a] | *nicht vorhanden* | **14,11** | (`Speicherentladung_Anteil`) |
| Stromproduktion [MWh/a] | 13,2329 | **13,23** | 13,23 |
| Betriebsstunden [h] | 661,65 | **662** | 661,65 |

**Die alte Anzeige wich in zwei von vier Werten von der Ergebnistabelle desselben Laufs
ab** (Restwärme 21,47 gegen 21,48; Deckung 54,63 gegen 54,17). Die neue Anzeige stimmt in
allen Werten überein.

Gegenprobe der Restwärme über den zweiten Weg: `waermerestbedarf.Sum()/1000` = **21,4779**
gegen die Skalarformel **21,4779** — Ganglinie und Kennzahl bilden dieselbe Rechnung ab
(Befund N4 des Pakets 6).

Energieprobe des Kerns (Befund N8):
`Direktdeckung 11 284,64 + Speicherladung 14 321,04 + Überschuss 0,0003 = 25 605,69 kWh`
gegen `waermeproduktion.Sum() = 25 605,69 kWh` — Abweichung **0,0003 kWh**.

`Tab_ErgebnisPufferspeicher` (Puffer „Stora B 1000-6 ER 1 B"):
`Ladung_gesamt = 36 297,00 kWh`, `Entladung_gesamt = 35 128,35 kWh`, `Q_max = 11,19 kWh`,
`Vollzyklen = 3 242,54`. Der Puffer trägt mehr Ladung als das BHKW liefert (14,32 MWh),
weil der Heizkessel des Projekts ihn ebenfalls lädt — die BHKW-Kennzahl weist deshalb
richtig den **Eigenanteil** aus, nicht die Speicherbilanz.

### 6.3 Belegte Anzeigewerte kommen aus der ECHTEN Oberfläche

Nicht nachgerechnet, sondern **abgelesen**: Der Harness öffnet
`Form_Simulation_Detail` unsichtbar (`Opacity = 0`), setzt den abgeschlossenen Laufzustand
des Runners in ihre Felder und ruft ihre eigene Anzeigemethode
`Endergebniss_Simulation()`. Danach werden die Steuerelemente gelesen:

```
   textBox_Waermebedarf_BHKW              =    "46,87"
   textBox_Waermeproduktion_gesamt_BHKW   =    "25,61"
   textBox_Restwaermebedarf_BHKW          =    "21,48"
   textBox_Waermeueberschuss_BHKW         =     "0,00"
   tb_BhkwSpeicherladung                  =    "14,32"
   tb_BhkwSpeicherdeckung                 =    "14,11"
   textBox_Waermedeckung                  =    "54,17"
   label_BhkwSpeicherladung               = "davon in den Speicher:"    bei y=209
   label_BhkwSpeicherdeckung              = "aus dem Speicher gedeckt:" bei y=241
   label_BhkwSpeicherladungEinheit        = "MWh/a"                     bei x=1053
   checkBox_BHKW_sortiert                 = "sortiert"  checked=False  bei {X=517,Y=30}
```

Der Umschalter steht bei `chart.Right - 90 = 517` / `chart.Top + 8 = 30` — dieselbe
Rechnung wie beim Kessel. Die drei .resx-Texte werden gefunden (nicht der Rückfall).

### 6.4 Ganglinienform — der gemeldete harte Abfall

Ausgelesen aus den Chartserien der geöffneten Form, drei Phasen:

| Phase | X-Achse | Serie | Typ | Punkte > 0 | letzter Punkt > 0 | Summe |
|---|---|---|---|---|---|---|
| **chronologisch** (Grundzustand) | Monate | Wärmeproduktion | `StackedColumn` | 1505 | **8759** | 25 605,69 |
| " | " | Speicherladung | `FastLine` | 1505 | **8759** | 14 321,04 |
| " | " | Restwärme | `FastLine` | 6061 | 8759 | 21 477,95 |
| " | " | Wärmebedarf | `FastLine` | 6216 | 8759 | 46 869,31 |
| **sortiert** | Jahresstunden | Wärmeproduktion | `FastLine` | 1505 | **1504** | 25 605,69 |
| " | " | Speicherladung | `FastLine` | 1505 | 1504 | 14 321,04 |
| " | " | Restwärme | `FastLine` | 6061 | 6060 | 21 477,95 |
| " | " | Wärmebedarf | `FastLine` | 6216 | 6215 | 46 869,31 |
| **zurück chronologisch** | Monate | alle vier | wie oben | wie oben | **8759** | **identisch** |

**Der geforderte Nachweis:** Im Grundzustand reicht die Produktionsserie bis Stunde 8759 —
sie fällt **nicht mehr hart auf 0**, sondern zeigt den echten Jahresverlauf. Erst der
Umschalter erzeugt die Dauerlinie, und die bricht bei h=1504 ab — jetzt aber unter der
Achsenbeschriftung „Jahresstunden", wo das die richtige Aussage ist.

**Speicherbeitrag sichtbar:** Die Serie „Speicherladung" trägt in genau denselben 1505
Stunden Werte wie die Produktion und summiert auf 14 321,04 kWh = `Speicherladung_gesamt`.

**Restwärme-Ganglinie korrigiert:** Der Kernvektor hat **6061** Stunden > 0, die alte
Vektordifferenz nur **4711**. Die alte Kurve war also auch in der Form falsch, nicht nur in
der Summe.

**Umschalten ist verlustfrei:** Nach dem Zurückschalten sind alle vier Summen und alle
Punktzahlen identisch mit dem Grundzustand — `GanglinienDarstellung.Dauerlinie` arbeitet
auf einer Kopie.

### 6.5 Layout nach dem Nachrücken

`Überlappungen sichtbarer Elemente: 0 | unterster Rand y=696 (Entwurfshöhe 721)`

Geprüft über alle Paare der Steuerelemente von `tabPage_BHKW` (Schnittfläche > 2 × 2 px).

### 6.6 Build

| Nachweis | Ergebnis |
|---|---|
| Prüfbuild `WindowsFormsApplication1` (Debug/x86) **vor** dem Nachzug | 0 Fehler / **6 Warnungen** (Baseline) |
| Prüfbuild **nach** dem Nachzug | 0 Fehler / **6 Warnungen** — Baseline gehalten |
| Referenzlauf-Werkzeug (Debug/x86) | 0 Fehler / 0 Warnungen |
| Harness (Debug/x86) | 0 Fehler |

Vorbefund während der Umsetzung: Ein Prüfbuild schlug mit **6 Fehlern in
`WaermesenkeClass.cs`** fehl (`Resource.SIM_VERBUND_KONFLIKT_*` noch nicht im
Ressourcen-Designer). Alle sechs liegen in einer Datei, die dieser Nachzug **nicht**
anfasst; sie stammen aus paralleler Arbeit am Ressourcenkatalog. Keine Fehlermeldung in
`Form_Simulation_Detail.cs` oder den beiden .resx.

---

## 7. Offene Punkte

| Nr. | Punkt | Bewertung |
|---|---|---|
| 1 | **Die drei neuen Texte gehören in `MyResource\Resource.resx`**, sobald die parallele Arbeit dort abgeschlossen ist — dann wie `SIM_CHK_SORTIERT` und alle übrigen programmatischen Texte. Bis dahin liegen sie in der formulareigenen .resx; der WinForms-Designer könnte Knoten ohne Steuerelementbezug beim Speichern verwerfen. **Die Anzeige bleibt in jedem Fall deutsch lesbar** (Rückfall im Code), nur die englische Fassung ginge verloren | offen, Nachzug |
| 2 | **Bezugsgröße der Bedarfslinie** (3.4): Stufeneingang statt Projektbedarf, anders als beim Kessel. Bei 1018 fallen beide zusammen. Ob die Seite zusätzlich die Projektwärmelast zeigen soll, ist eine Anwenderentscheidung | Anwenderentscheidung |
| 3 | **Serie „Überschuss"** ist nicht gezeichnet (Jahressumme steht als Zahl, bei 1018 praktisch 0). Bei einem Projekt mit echtem Überschuss wäre sie die vierte Komponente der Energieprobe | offen |
| 4 | **Kein CSV-Export auf der BHKW-Seite** — die Heizkessel- und die Wärmepumpen-Seite haben einen. Die vier Vektoren liegen jetzt beieinander; der Knopf wäre eine kleine Ergänzung | offen |
| 5 | **`Betriebsstunden` wird mit `F0` gerundet** (662 gegen 661,65 im Kern). Bestandsverhalten, nicht angefasst | Bestand |
| 6 | **`Reststrombedarf` ist negativ** (−13,23 MWh), weil das Projekt keinen Strombedarf führt (`Projektstrombedarf = 0`). Bestandsverhalten der Stromseite, außerhalb dieses Auftrags — die Stromzeilen sind nicht angefasst | Bestand |
| 7 | **Neue Referenzbasis einfrieren** — offener Punkt 1 des Pakets BHKW-Regulär, unverändert. Dieser Nachzug ändert **keine** Ergebnisgröße und keine CSV-Spalte; er berührt die Referenzläufe nicht | offen, für den Anwender |
