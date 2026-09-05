# iU9 Welle 8 — Bedarfsblätter (W8a + W8b) — Portprotokoll

> Umsetzung 03.09.2026 auf `ios_migration`, Basis `e5114e1` (nach dem Merge der
> Welle 7). Vorbild in Aufbau und Tiefe: das Protokoll der Welle 7 im selben
> Ordner. Regeln: Wellenplan Abschnitt F, `EPOS.UI/CLAUDE.md`,
> `EPOS.Kern/CLAUDE.md`.

---

## 1. Auftrag und Ergebnis

**Zehn WinForms-Masken der drei Bedarfsblätter** (Stromverbraucher,
Prozesswärme, Brauchwasser) und die Gebäudetypen-Verwaltung sind **vier**
Razor-Komponenten in `EPOS.UI/Dialoge/Bedarf/`; ihre WinForms-Fassungen sind im
selben Commit gelöscht (Regel M1). Zusammen **2 569 Zeilen** Oberflächencode,
**41 `MessageBox`**-Aufrufe und **369 Kartenzeilen**.

**Zehn Masken, vier Komponenten** — das ist der Ertrag dieser Welle. Die drei
Bedarfsblätter sind Drillinge desselben Blatts: Sie unterscheiden sich in Titel,
Typbeschriftung, Zieltabelle und in einer Handvoll Meldungen, nicht im Aufbau.
Die Ausprägung ist deshalb ein Aufzählungstyp (`BedarfsArt`) und keine dritte
Fassung.

| Komponente | ersetzt | Zeilen | Hülle |
|---|---|---|---|
| `TypStammDialog` | `Form_EingDBStromverbraucher` (146), `Form_EingDBProzess` (174), `Form_EingDBBrauchwasser` (139) | 459 | `Views/Bedarf/TypStammHuelle.cs` |
| `BedarfErgebnisDialog` | `Form_ErgStromverbraucher` (169), `Form_ErgProzesswaerme` (215), `Form_ErgBrauchwasserwaerme` (425) | 809 | `Views/Bedarf/BedarfErgebnisHuelle.cs` |
| `TypProfilDialog` | `Form_EingStromTyp` (334), `Form_EingProzTyp` (366), `Form_EingBrauchwasserTyp` (257) | 957 | dieselbe Datei wie W8.1 |
| `GebaeudetypDialog` | `Form_EingGebTyp` (344) | 344 | `Views/Bedarf/GebaeudetypHuelle.cs` |

**Neu im Kern**: der Aufzählungstyp `BedarfsArt`, zwei Controller
(`BedarfStammCtrl`, `TypProfilCtrl`), die Gebäudetyp-Verwaltung in `TagVCtrl`,
die Schreibwege des `ProzesswaermeStammCtrl` und **drei** Renderer-Bilder
(Bausteinlücke 12) samt drei Proben in `ChartProben`.

### Commits

| Hash | Betreff |
|---|---|
| `e9d7ad6` | iU9-W8.0a: `ProzesswaermeStammCtrl` auf den Schnitt seiner beiden Zwillinge |
| `fec0a20` | iU9-W8.0b: `BedarfsArt`, `BedarfStammCtrl` und `TypProfilCtrl` im Kern |
| `c046c07` | iU9-W8.0c: drei Bedarfsbilder im `ChartRenderer` samt drei Proben |
| `b1d8a4b` | iU9-W8.0d: `TagVCtrl` trägt die Gebäudetyp-Verwaltung |
| `34e69ff` | iU9-W8.2: `BedarfErgebnisDialog`, drei Ergebnismasken gelöscht |
| `1e9c8fc` | iU9-W8.1: `TypStammDialog`, drei Stammkopfmasken gelöscht |
| `6b65f2e` | iU9-W8.3: `TypProfilDialog`, drei Typprofilmasken gelöscht |
| `2119e18` | iU9-W8.4: `GebaeudetypDialog`, `Form_EingGebTyp` gelöscht |
| `cbb358e` | iU9-W8.5: 130 Textschlüssel für die zehn Masken, de und en |
| `04dd413` | iU9-W8.6: Formularkarte-Tests auf den Stand nach Welle 8 |

**Anzeige vor Eingabe:** Erst der Ergebnisdialog (W8.2) — er ist reine Anzeige,
hat 15 Aufrufstellen und braucht keinen einzigen Schreibweg —, dann die drei
Eingabeblätter. Das W8.0e-Datenobjekt ist mit W8.2 entstanden, weil es nur dort
gebraucht wird.

---

## 2. Bauweise

### 2.1 Drei neue Bilder im Kern

Die zehn Masken zeigten zusammen **drei** Diagramme, jedes mehrfach. Sie sind
drei Methoden geworden:

| Methode | Maß | Vorbild | Wer zeigt es |
|---|---|---|---|
| `ChartRenderer.MonatsSaeulen` | 978 × 542 | `ZeigeStromGrafik`:83 / `ZeigeMonatsGrafik` | die drei Ergebnismasken, vier Sichten |
| `ChartRenderer.Stundenprofil` | 1244 × 464 | `ChartAktualisieren`:37 (168 h) **und** `init_Chart`:171 (24 h) | die drei Typprofilmasken und der Gebäudetyp |
| `ChartRenderer.Jahresverlauf` | 978 × 542 | `ZeigeJahresGrafik`:166 | die Brauchwasser-Ergebnismaske |

Drei Entscheidungen mit Grund:

* **Die „schönen Schritte" sind wörtlich übernommen.** `SkaliereYAchse` stand
  dreimal gleichlautend in den drei Ergebnismasken: Schrittweiten
  0,1/0,2/0,25/0,5/1/2/2,5/5/10, Zielschrittweite `max × 1,1 / 4,5`, Obergrenze
  `ceil(max × 1,05 / Schritt) × Schritt`, Nachkommaformat N0/N1/N2 — samt dem
  Rückfall „Maximum 5, Intervall 1", wenn alle zwölf Werte null sind. Das ist
  eine **andere** Reihe als die des Kapitalwert-Verlaufs (dort 1/2/2,5/5/10):
  Bedarfswerte brauchen auch Zehntel.
* **Ein Bild für zwei Vorbilder.** Der Unterschied Fläche (Typprofil) gegen
  Linie (Gebäudetyp) war keine Entscheidung, sondern die Voreinstellung zweier
  verschiedener Diagrammverwalter. `Stundenprofil` zeichnet immer die Fläche mit
  ihrer Randlinie und nimmt das Intervall der x-Beschriftung als Parameter
  (24 = Tagesgrenzen, 2 = jede zweite Stunde).
* **Der Jahresverlauf trägt Monatsgrenzen statt Stundenzahlen.** Der Vorläufer
  ließ die Achse am Mausrad spreizen und passte die Beschriftung mit; ein PNG
  kann das nicht (A-1). „Stunde 5 832" sagt nichts, „Sep" schon.

### 2.2 Die Datenseite im Kern

| Was | Wo | Herkunft |
|---|---|---|
| `BedarfsArt` | `EPOS.Kern/Model/` | neu — die Ausprägung als Aufzählungstyp |
| `BedarfStammCtrl` | `EPOS.Kern/Controller/` | die Konstruktoren und `SetControls` der drei Stammkopfmasken |
| `TypProfilCtrl` | dito | `DatenEinlesen`, die Speicher-, Neu- und Löschwege der drei Typprofilmasken |
| `ProzesswaermeStammCtrl.Exists/SaveHead/TypIsReadOnly/TypNew/TypDelete` | dito | `Form_EingDBProzess`:82-174 und `Form_EingProzTyp`:252-357 (dort inline) |
| `TagVCtrl.Typen/Lies/Speichern/Anlegen/Loeschen/KurvenNamen` | dito | `Form_EingGebTyp`:32-336, fast die ganze Maske |
| `ChartRenderer.MonatsSaeulen/Stundenprofil/Jahresverlauf` | `EPOS.Kern/Allgemein/Bericht/` | die neun `Chart`-Steuerelemente der zehn Masken |

**Die drei Stammcontroller sind jetzt gleich geschnitten** (W8.0a). Zwei von
ihnen trugen `Exists`/`SaveHead`/`TypIsReadOnly`/`TypNew`/`TypDelete` schon; die
Prozessmaske hatte ihr SQL inline. Die **Anweisungen** bleiben die des
Vorläufers (Regel F3): Der Prozesskatalog überlässt seine Id weiterhin der
Datenbank (`AUTOINCREMENT`), während Strom und Brauchwasser sie mit
`GetMaxID + 1` selbst vergeben.

**Die ReadOnly-Sperre prüft die HÜLLE, nicht der Controller.** `SaveHead` und
`TypDelete` melden sie über `Meldung.Hinweis`, und das wäre in einer WebView ein
modaler Kasten über dem Dialog. Die Hüllen fragen deshalb vorher (`IstReadOnly`)
und geben die Meldung als Ergebnis zurück, wo sie als Warnbanner stehen bleibt.

### 2.3 Drei Transaktionen mehr

`Tab_*typ_STAMM` und `Tab_DBTagVDaten_STAMM` sind **Simulationseingang**. Drei
Schreibwege liefen bisher ohne Klammer und konnten bei einem Fehler in der Mitte
einen halben Stand hinterlassen:

| Weg | vorher | jetzt |
|---|---|---|
| Typprofil speichern (Strom, Brauchwasser) | 169 Einzelanweisungen | eine Transaktion (wie `Form_EingProzTyp` schon) |
| Gebäudetyp speichern | bis zu 192 Einzelanweisungen | eine Transaktion |
| Gebäudetyp anlegen | 193 Einzelanweisungen — ein Fehler ließ einen Kopf OHNE Verteilungen zurück | eine Transaktion |

Alle drei sind ergebnisgleich; der Referenzlauf ist byte-gleich (§ 7.7).

### 2.4 Ein eingefrorenes Rechenobjekt

Die drei Ergebnismasken bekamen das **lebende** `SimulationStrombedarf` bzw.
`SimulationWaermebedarf` in die Hand und lasen bei jedem Optionswechsel neu
daraus. Sie sind reine Anzeigen — nichts schreibt zurück. Also baut die Hülle
einmal `BedarfErgebnisDaten`, rendert die bis zu vier Bilder vorab und reicht
PNGs hinein; die Komponente kennt die Simulationsklassen nicht (Risiko R-W8-2).
`Form_Simulation_Detail` und `NavigatorUebersicht` sind dabei nur an ihren
Aufrufstellen angefasst (iR10).

### 2.5 Zwei Stände statt einem

`TypProfilDialog` und `GebaeudetypDialog` führen **zwei** Stände: die
übernommenen Werte (7 × 24 bzw. n × 24) und die 24 Felder eines Tages. Genau so
arbeiteten die Vorläufer, und genau daran hängen zwei Verhaltensweisen, die
sonst verloren gegangen wären:

* **Im Typprofil verwirft ein Tageswechsel nicht übernommene Eingaben.**
  `Tagesdaten` überschrieb die 24 Felder aus `arr`, ohne zu fragen.
* **Im Gebäudetyp überträgt ein Kurvenwechsel STILL.** `RefreshArrayValues`:145
  schrieb die Felder der vorigen Kurve zurück; ein leeres oder ungültiges Feld
  ließ den bisherigen Wert stehen, ohne zu melden. Gemeldet wird erst am
  Speichern-Knopf.

Der Unterschied ist Absicht des Bestands und keine Unachtsamkeit: Das Typprofil
hat einen eigenen Übernahmeknopf, der Gebäudetyp nicht.

---

## 3. Feldkarten-Abgleich — je AUSPRÄGUNG

Die zehn Karten wurden am 03.09.2026 neu gezogen (Stand nach W7) und liegen
unter `scratchpad/iU9/karten_w8/`. Abgeglichen ist der **Feldbestand nach Zahl
und Beschriftung**, und zwar je Ausprägung, nicht je Komponente (Risiko
R-W8-1): Jede der zehn Masken hat einen eigenen bunit-Fall.

| Maske | Karte | Komponente | Anmerkung |
|---|---|---|---|
| EingDBStromverbraucher | 31 Zeilen, 13 TextBox, 1 ComboBox, 4 Button | 12 Zahlenfelder, 1 Klappliste, 2 Textfelder, 4 Knöpfe | „Novmember" berichtigt (A-2) |
| EingDBProzess | 31 Zeilen, dieselbe Form | dieselbe Komponente, Ausprägung `Prozesswaerme` | Titel und Typbeschriftung je Ausprägung |
| EingDBBrauchwasser | 31 Zeilen, dieselbe Form | Ausprägung `Brauchwasser` | einzige der drei ohne Satelliten-`.resx` |
| ErgStromverbraucher | 35 Zeilen, 16 TextBox, 3 TabPage | 4 Kennzahlen, 12 Monate, 1 Bild, 3 Reiter | keine Optionsgruppe — eine Reihe |
| ErgProzesswaerme | 45 Zeilen, 19 TextBox, 4 RadioButton | 7 Kennzahlen, 2 Sichten | zwei Optionsgruppen, wie im Vorläufer |
| ErgBrauchwasserwaerme | 48 Zeilen, 6 RadioButton, 1 CheckBox | 7 Kennzahlen, 3 Sichten, Jahresschalter | der Schalter nur bei der Brauchwassersicht |
| EingStromTyp | 39 Zeilen, 24 `st*`, `listBox_Tag` mit 7 Einträgen | 24 Zahlenfelder, 7 Optionen, 2 Reiter, 8 Knöpfe | +2 = Tagknöpfe mit Wirkung (A-6) |
| EingProzTyp | 38 Zeilen, dieselbe Form | Ausprägung `Prozesswaerme` | Stundenfelder ohne festes Format |
| EingBrauchwasserTyp | 38 Zeilen, dieselbe Form | Ausprägung `Brauchwasser` | Stundenfelder `F4` |
| EingGebTyp | 33 Zeilen, 25 TextBox, 2 ListBox, 4 Button | 24 Zahlenfelder, 2 Listen, 1 Bild, 4 Knöpfe | −1 = das Beschreibungsfeld ist Anzeige |

**Beschriftungen aus dem DESIGNER, nicht aus der Karte.** Die Karte ordnet bei
den Typprofilmasken die Stundenbeschriftungen um eine Zeile versetzt zu (`st10`
ohne Text, `st18` mit „10", `listBox_Tag` mit „18") und beim Gebäudetyp
`listBox_Kurve` die „18" statt „Kurvenverlauf für den Tag:". Die `.resx` sagt es
richtig; die Tests halten die Designer-Fassung fest.

---

## 4. Abweichungen (mit Begründung)

| # | Was | Warum |
|---|---|---|
| **A‑1** | Der Mausrad-Zoom der Jahresansicht entfällt; die x-Achse trägt stattdessen Monatsgrenzen | Ein PNG kann nicht spreizen. Zoomen ist W3‑O2/W11 — dieselbe Lage wie beim Kostenprofil. Die Monatsgrenzen sind der Ersatz: Sie sagen an jeder Stelle, wo im Jahr man ist |
| **A‑2** | „Novmember" heißt jetzt „November" | Ein Tippfehler im Designer der Strommaske; die beiden Zwillinge schreiben es richtig. Der Text ist eine Beschriftung, kein Steuerwert |
| **A‑3** | Die Positionierung an der Knopfposition (`PointToScreen`) entfällt bei vier Aufrufwegen | Eine Blazor-Hülle kennt kein `PointToScreen` und erscheint mittig über dem Besitzer — dieselbe Umstellung wie iU9‑W2.1 |
| **A‑4** | 41 `MessageBox` werden `Warnbanner`, `Rueckfrage` oder Meldungstext | Wie A‑13 aus Welle 7: Bestätigungen bleiben Bestätigungen, Ablehnungen bleiben als Banner stehen und lassen den Dialog offen |
| **A‑5** | Der 500‑ms-Bildblitz von „Änderungen Übernehmen" wird eine Meldung samt neu gezeichnetem Diagramm | Der Vorläufer hielt dafür den Oberflächenfaden mit `Thread.Sleep(500)` an — in der WebView wäre das ein eingefrorener Dialog (wie W7 A‑24). Dass das Bild mitgeht, ist der zweite Teil: Ein Diagramm, das nach einer ausdrücklichen Übernahme veraltete Werte zeigt, sieht wie ein Fehler aus |
| **A‑6** | „Tag kopieren" und „Tag einfügen" tun etwas | Befund W8‑B1: Die beiden Knöpfe stehen im Designer aller drei Typprofilmasken, haben dort aber KEINEN Handler — sie waren sichtbar und wirkungslos. Sie sind jetzt ein Kopierpuffer in die FELDER; fest wird der Tag erst mit „Übernehmen", also auf demselben Weg wie beim Tippen |
| **A‑7** | Der gelbe ToolTip mit dem Sperrgrund des Gebäudetyps wird eine `Herleitungszeile` | Ein Tooltip ist auf einem Berührungsgerät nicht erreichbar (wie W7 A‑19) |
| **A‑8** | „Typ Löschen" im Gebäudetyp fragt nach | Der Vorläufer löschte Kopf und 192 Datenzeilen auf einen Klick, ohne Rückfrage — und der Katalog gilt für alle Projekte (wie W6 A‑4) |
| **A‑9** | Die Typprofile werden in EINER Transaktion geschrieben, ebenso Anlegen und Löschen des Gebäudetyps | Siehe § 2.3. Ergebnisgleich, Referenzlauf byte-gleich |
| **A‑10** | Ein Bestandswert außerhalb der Typliste wird ihr VORANGESTELLT | Der Vorläufer hatte eine frei beschreibbare `ComboBox` und zeigte auch einen Typ, den der Katalog nicht (mehr) führt. Ein `select` würde ihn still verwerfen (wie W7 A‑16) |
| **A‑11** | Die tote Wochenansicht `ZeigeWochenGrafik`/`ExtrahiereWoche` entfällt ersatzlos | Befund W8‑B2: 64 Zeilen ohne Aufrufer (`Form_ErgBrauchwasserwaerme`:222‑285) |
| **A‑12** | Der Ergebnisdialog schließt auch mit **Enter** | Er zeigt nur an und trägt genau einen Knopf; hier kann Enter nichts versehentlich schreiben. Die übrigen drei Dialoge lassen Enter unbelegt (Hausregel) |
| **A‑13** | Die zwölf Monatsfelder stehen im Modus „Neu" LEER statt auf 0 | So fordert die Pflichtprüfung sie ein, statt still zwölf Nullen zu speichern (wie W7 A‑11). Der Vorläufer tat dasselbe — er fand keinen Satz und ließ die Felder unberührt |
| **A‑14** | Kein KI-Aufrufknopf, kein `FensterEinpassung`, kein `Paint`-Rahmen, keine `pictureBox1` | Wie A‑1 aus Welle 7: Der KI-Einstieg hat in `EPOS.UI` noch keinen Baustein (W15b); die übrigen sind WinForms-Layoutkorrekturen, die Hülle und CSS erledigen |

---

## 5. Texte

**130 Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`,
`Resource.en-US.resx` und — von Hand, weil hier kein Visual Studio läuft —
`Resource.Designer.cs`, dazu die 13 Kurvennamen aus W8.0d. Alle drei Dateien
geprüft: **3 615 de = 3 615 en**, 143 neue Schlüssel in allen dreien.

| Präfix | Zahl | Wofür |
|---|---|---|
| `BERG_*` | 32 | Ergebnisdialog |
| `BTYP_*` | 26 | Typstamm |
| `BPRO_*` | 30 | Typprofil |
| `GTYP_*` | 12 (+13 aus W8.0d) | Gebäudetyp |
| `ALLG_MONAT_*` | 12 | die Monatsnamen — vier Dialoge brauchen sie |
| `ALLG_MONAT_KURZ_*` | 12 | die x-Achse der Monatssäulen („Mrz" wie im Vorläufer) |
| `ALLG_WOCHENTAG_*` | 7 | die `listBox_Tag`-Einträge der drei Typprofilmasken |

**Alle zehn Masken zeichneten über `ApplyResources`**, sieben mit englischen
Satelliten (`.en-US.resx`: 15 + 15 + 20 + 27 + 40 + 16 + 34 = **167**). Alle
Texte sind **wörtlich** übernommen; die drei Brauchwassermasken trugen deutsche
Literale und sind neu übersetzt. Die Zahl der lokalisierten Masken sinkt
dadurch von 47 auf **37**.

**Nicht übersetzt sind die Steuerwerte:** die Typnamen selbst kommen aus
`Tab_Stromverbrauchertyp_STAMM`, `Tab_Prozesstyp_STAMM` und
`Tab_Brauchwassertyp_STAMM` und werden mit dem Datenbankinhalt verglichen.

**`help_mapping.txt` bleibt unverändert.** Die zehn Zeilen `Form_X.btn_Help`
gelten weiter — der Schlüssel benennt die Wikiseite, nicht die Klasse; jede
Komponente trägt ihren alten Schlüssel als `HilfeSchluessel`, je Ausprägung den
richtigen.

**`Allgemein/KI/HilfeKontext.cs`:** die zehn Einträge der gelöschten Masken
entfernt, jeweils im Commit ihrer Maske (Regel F10).

---

## 6. WinForms-Seite

**Gelöscht** (44 Dateien):

```
Views/Stromverbraucher/Form_EingDBStromverbraucher.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Stromverbraucher/Form_ErgStromverbraucher.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Stromverbraucher/Form_EingStromTyp.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Prozesswärme/Form_EingDBProzess.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Prozesswärme/Form_ErgProzesswaerme.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Prozesswärme/Form_EingProzTyp.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Brauchwasser/Form_EingDBBrauchwasser.{cs,designer.cs,resx}
Views/Brauchwasser/Form_ErgBrauchwasserwaerme.{cs,designer.cs,resx}
Views/Brauchwasser/Form_EingBrauchwasserTyp.{cs,designer.cs,resx}
Views/Gebäude/Form_EingGebTyp.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
```

**Neu auf der Windows-Seite** (3) — ein neuer Ordner `Views/Bedarf/`:
`BedarfErgebnisHuelle.cs`, `TypStammHuelle.cs` (trägt W8.1 **und** W8.3, weil
„DB ändern" und „Typ ändern" derselbe Aufrufweg sind), `GebaeudetypHuelle.cs`.

**`Form_Simulation_Detail - Kopie.cs` gibt es nicht mehr** — sie ist mit iU9‑W0
(iF29) gelöscht worden; Risiko R‑W8‑5 ist damit gegenstandslos.

**Aufrufer umgestellt** (36 Stellen in 11 Dateien): `Form_Stromverbraucher`
(5), `Form_Stromverbraucher_Admin` (5), `Form_Prozesswaerme` (5),
`Form_Prozesswaerme_Admin` (5), `Form_Brauchwasser` (5),
`Form_Brauchwasser_Admin` (5), `Form_Start` (1, die Kachel „eigenes
Stromprofil"), `Form_Simulation_Detail` (2 — nur diese beiden Stellen,
Wachstumsstopp iR10), `NavigatorUebersicht` (1), `Form_Gebaeude` (1),
`WinFormsNavigation` (1, `Masken.GebaeudetypenAdmin`).

**Keine Typverwendung ist übrig:**

```
grep -rn "(new|typeof|:)\s*(Form_EingDBStromverbraucher|Form_EingDBProzess|
    Form_EingDBBrauchwasser|Form_ErgStromverbraucher|Form_ErgProzesswaerme|
    Form_ErgBrauchwasserwaerme|Form_EingStromTyp|Form_EingProzTyp|
    Form_EingBrauchwasserTyp|Form_EingGebTyp)\b" --include=*.cs .
→ 0 Treffer im Code
```

Restfundstellen der alten Namen sind ausschließlich (a) `HilfeSchluessel`- und
`help_mapping`-Zeichenketten, (b) Kommentare, die die Herkunft nennen, und
(c) der eingefrorene Erreichbarkeitsbericht
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md` (ein datierter
Messstand, kein Code).

---

## 7. Nachweise

### 7.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 -t:Rebuild
→ 0 Fehler, 20 Warnungen
```

Gleichauf mit der Basis nach Welle 7 (20). Aufteilung unverändert: 14 WFO1000,
2 CS0108, 2 CS0109, 1 WFO0003, 1 CA2255 — keine der zehn gelöschten Masken trug
eine WFO1000-Fundstelle, und keine neue ist dazugekommen.

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ KiKern.Tests         450 gruen
  SpeicherEngine.Tests  337 gruen
  EPOS.UI.Tests       1 006 gruen   (+66 aus Welle 8)
  EPOS.Kern.Tests       113 gruen   (+20 aus Welle 8)
  zusammen            1 906 gruen, 0 rot
```

**66 neue bunit-Fälle**: `BedarfErgebnisDialog` 12, `TypStammDialog` 17,
`TypProfilDialog` 22, `GebaeudetypDialog` 15. Jeder Satz prüft den Feldbestand
(Zahl UND Beschriftungen) **je Ausprägung**, die Vorbelegung, die Prüfregeln,
die Rückrufe und die Tastatur; die Kultur ist auf de‑DE gepinnt.

**20 neue Kern-Fälle**: `BedarfProfilTests` 9 (Typliste und Monatswerte je Art,
`ProzesswaermeStammCtrl.SaveHead` neu/überschreiben/ReadOnly, `TypProfilCtrl`
Lies/Neu/Speichern/Löschen je Art, `SpeichernUnter` auf der Ausprägung mit der
abweichenden Schlüsselspalte), `TagVCtrlTests` 8 (`KurvenNamen(4)/(5)/(8)`,
`Typen`, `Lies`, `Anlegen` mit Kopf **und** 192 Datenzeilen, `Speichern`,
`Loeschen`), `ChartRendererTests` +3 (Maß und Determinismus der drei Bilder,
der Rückfall „alles null", leere Reihen).

### 7.3 Formularkarte

```
dotnet test Werkzeuge/Formularkarte/Formularkarte.sln -c Release
→ 123 gruen
```

Kein Test hing an einer der zehn Masken — nur drei Zähler haben sich bewegt:
66 Designer-Dateien (76), 63 Masken (73), 37 lokalisierte (47) und 61 von 63
erreichbar (71 von 73).

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1 --erreichbarkeit
→ Masken 63 (73 nach W7), lokalisiert 37 (47), erreichbar 61,
  unerreichbar 0, verwaist 0
```

**63 = 73 − 10.**

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 254 SQL-Texte geprueft: 0 Fundstellen, 174 dynamisch, 1 080 in Ordnung
python3 … --selbsttest
→ 32 Anweisungen, 0 Abweichungen
```

Keine Nachbesserung am Prüfer nötig. Die Zahl der geprüften Texte sinkt von
1 272 auf 1 254, weil die zehn Masken ihre SQL verloren oder mitgenommen haben.
Gezogen wurde er nach **jedem** der vier Kern-Schritte.

### 7.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 15 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

**Drei Bilder mehr als nach Welle 7**: `monatssaeulen` (978 × 542, zwölf Werte
mit EINEM Nullmonat — der Rückfall „alles null" darf davon nicht ausgelöst
werden), `stundenprofil_woche` (1244 × 464, fünf Werktage und zwei ruhigere
Wochenendtage) und `jahresverlauf_bedarf` (978 × 542, 8 760 Stunden). Alle drei
deterministisch; geprüft wird bei der Fläche die deckende Randlinie **und** die
Mischfarbe der halbtransparenten Füllung über Weiß.

### 7.7 Referenzlauf

**Pflicht in dieser Welle**, weil vier Kern-Controller angefasst werden
(`ProzesswaermeStammCtrl`, `BedarfStammCtrl`, `TypProfilCtrl`, `TagVCtrl`) und
der Renderer drei neue Methoden bekommt.

```
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w8
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/w8
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

**Byte-gleich, nicht nur innerhalb der Toleranz.** Das ist der Nachweis, dass
die drei neuen Transaktionen und die Verlagerung der Schreibwege
zeichengleich waren.

### 7.8 Veröffentlichung

```
dotnet publish WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Release -p:Platform=x64
→ wwwroot/index.html, wwwroot/_framework/blazor.webview.js,
  wwwroot/_content/EPOS.UI/epos-ui.css — vollstaendig
```

---

## 8. Grenzen

* **Am Gerät ungeprüft.** Alles hier ist ohne Windows entstanden. Die
  Abnahmeliste in § 9 ist der Prüfplan.
* **Die drei Bilder sind neu gezeichnet, nicht nachgebaut.** Der Modus
  `bildvergleich` der Referenzlauf-Suite ist mit iF23 gelöscht; ein Vergleich
  mit den GDI-Bildern ist nur von Hand am Gerät möglich (Abnahmepunkt 3). Die
  ChartProben sichern Maß, Farben und Determinismus.
* **Die ID-Vergabe `GetMaxID + 1` bleibt** (R‑W8‑4): Zwei gleichzeitige
  Schreiber auf `Tab_DBTagV_STAMM` würden dieselbe Id ziehen. Wörtlich
  übernommen und jetzt wenigstens in einer Transaktion; siehe W8‑O‑4.
* **Drei Bestandsbefunde bleiben stehen** (§ 10) — sie sind Fachentscheidungen,
  keine Portfragen.

---

## 9. Abnahmeliste Windows (iZ5) für diese zehn Masken

Je Dialog: öffnen mittig, kein weißes Aufblitzen, ziehbar und maximierbar,
Tabellen ohne Umbruch, de **und** en (`HKCU\Software\wp-plan\Language`),
Hochkontrast, 125 % und 150 % scharf, Maus **und** Finger (44 px), Tab-Zyklus
bleibt im Dialog, Esc schließt, Infoknopf zeigt die Wikiseite.

| # | Aufrufweg | Was besonders zu prüfen ist |
|---|---|---|
| 1 | Startbild → **Stromverbraucher** → „Simulation" | Ergebnis mit Startreiter „monatlich"; vier Kennzahlen; EINE Monatsreihe, also KEINE Optionsgruppe; die Säulen sind gelbgrün |
| 2 | Startbild → **Prozesswärme** → „Simulation" | Sieben Kennzahlen, über der Reiterleiste das Wahlfeld „Einheit" auf MWh; zwei Optionsgruppen, die sich NICHT gegenseitig umschalten; Säulen rot bzw. blau. Umschalten auf kWh nimmt Kennzahlen, Monatstabelle UND Säulenbild mit (Entscheid W8‑O‑5) |
| 3 | Startbild → **Brauchwasser** → „Berechnen" | Titel trägt „ - ‹Name›"; Startreiter „Grafik" UND Brauchwassersicht; „Wärmebedarf Brauchwasser" in MWh (der Wert liegt in kWh vor, siehe W8‑O‑5); der Schalter „Jahresverlauf" erscheint nur hier und zeigt 8 760 Stunden über Monatsgrenzen |
| 4 | In 1–3 → **„DB ändern"** | Name gesperrt, Typliste gefüllt, zwölf Monatswerte mit vier Nachkommastellen; „Speichern" gesperrt; ein leeres Monatsfeld meldet den Monatsnamen (Strom) bzw. „Monat n" (Prozess/Brauchwasser) |
| 5 | In 4 → **„Speichern unter"** | Namensabfrage IM Fenster mit Vorbelegung; belegter Name meldet; danach zeigt das Namensfeld den neuen Namen, „Überschreiben" trifft aber weiterhin den ALTEN Satz |
| 6 | In 1–3 → **„DB neu"** | Erst der Name, dann der Dialog im Modus Neu: zwölf LEERE Felder, nur „Speichern" frei; leerer Typ meldet vor den Zahlen |
| 7 | In 1–3 → **„Typ ändern"** | Typliste links, 24 Stundenfelder, sieben Wochentage; Tagwechsel verwirft nicht übernommene Eingaben; „Änderungen Übernehmen" meldet und zeichnet das 168‑h‑Bild neu; „Tag kopieren"/„Tag einfügen" wirken (A‑6) |
| 8 | In 7 → **„Speichern in DB" / „Neu" / „Speichern unter" / „Löschen"** | Auslieferungstyp meldet und schreibt nicht; „Löschen" fragt nach; nach „Neu" steht der neue Typ gewählt mit 168 Nullen; „Speichern unter" nimmt die aktuellen Werte mit |
| 9 | Startbild → Kachel **„eigenes Stromprofil"** | Derselbe Profildialog, Ausprägung Stromverbraucher |
| 10 | Menü → **Gebäudetypen** (`Masken.GebaeudetypenAdmin`) und Gebäude → „Gebäudetyp ändern" | Fünf bzw. acht Kurvennamen je nach KURVENZAHL; Kurvenwechsel überträgt still; Katalogtyp sperrt „Typ Speichern" und nennt den Grund als Zeile; „Typ hinzufügen" fragt Name UND Beschreibung; „Typ Löschen" fragt nach (A‑8) |
| 11 | **Simulation-Detail** → „Strom-Details" und „Wärmebedarf-Details" | Strom ohne Startreiter (Kennzahlen vorn), Wärme mit Reiter „monatlich"; die Datei ist sonst unberührt |
| 12 | **Navigator Übersicht** → „Wärmebedarf" | Startreiter „Grafik" und Brauchwassersicht |
| 13 | Die drei **Admin-Masken** (W14b) → alle Knöpfe | Besonders `Form_Brauchwasser_Admin` → „Ergebnisse Verbrauch": Dort öffnet sich wie bisher die PROZESS-Ansicht ohne Brauchwassersicht (Befund W8‑B3) — „davon Brauchwasser" zeigt seit dem Entscheid W8‑O‑5 aber DIESELBE Zahl wie Punkt 3 und nicht mehr das Tausendfache |

---

## 10. Offene Punkte

| # | Was | Vorschlag |
|---|---|---|
| **W8‑O‑1** | Die Prüfmeldungen heißen je Ausprägung anders: „Monatswert Januar" gegen „Monat 1", „Stundenwert 7" gegen „Stunde 7". Das ist keine Fachlage, sondern gewachsen | Wörtlich übernommen (Regel F3), je Ausprägung ein Test. **Entscheid des Anwenders**, ob alle drei Blätter denselben Wortlaut bekommen sollen — es wäre eine Zeile je Hülle |
| **W8‑O‑2** | „Speichern in DB" der drei Typprofilmasken ruft KEIN `VerteilungUebernehmen`: Was im Feld steht und nicht mit „Änderungen Übernehmen" festgeschrieben wurde, geht nicht mit. Der Gebäudetyp macht es umgekehrt (`btn_Speichern_Click` übernimmt zuerst) | Wörtlich übernommen. **Entscheid des Anwenders**: Soll „Speichern" die Felder mitnehmen? Dann fällt der Unterschied zwischen den beiden Masken weg — der Übernahmeknopf bliebe für die Vorschau |
| **W8‑B3 / W8‑O‑3** | `Form_Brauchwasser_Admin`:95 („Ergebnisse Verbrauch") öffnete den PROZESS-Ergebnisdialog aus der BRAUCHWASSER-Verwaltung — also ohne Brauchwassersicht und ohne den Teiler 1000 | Wörtlich übernommen (`mitBrauchwasser: false`). **Frage an den Anwender:** War das Absicht, oder soll dort dieselbe Ansicht stehen wie unter Punkt 3 der Abnahmeliste? |
| **W8‑O‑4** | `Tab_DBTagV_STAMM` und `Tab_DBTagVDaten_STAMM` bekommen ihre Id über `GetMaxID + 1`; zwei gleichzeitige Schreiber zögen dieselbe (R‑W8‑4) | Wörtlich übernommen, jetzt in einer Transaktion. Die saubere Lösung wäre `AUTOINCREMENT` wie beim Prozesskatalog — das ist eine Schemafrage und gehört in ein eigenes Paket |
| **W8‑B4 / W8‑O‑5** — **erledigt** | Der Brauchwasserwert wurde NUR in der Brauchwassermaske durch 1000 geteilt (`Form_ErgBrauchwasserwaerme.Init`:36 gegen `Form_ErgProzesswaerme.Init`:32) — dieselbe Größe, zwei Anzeigen | **Entscheid (Anwender, 04.09.2026):** MWh als Vorgabe, kWh wählbar, konsistent in den Ansichten — umgesetzt in `e665c41` (Kernklasse `Energieeinheit`/`BedarfEinheitWahl` in `7d8eb4f`). Der nackte Teiler ist weg; die Hülle nennt je Kennzahl die **Einheit, in der ihr Wert vorliegt** (`Waermebedarf_Brauchwasser` in **kWh** — es kommt aus `brauchwasserwerte.Sum()` —, jede andere Energiemenge in **MWh**, die beiden Spitzenwerte in kW, die zwölf Monatswerte in MWh), und der Dialog rechnet auf die gewählte Anzeigeeinheit um. Bei der Vorgabe MWh sind alle Zahlen **zeichengleich zum Bestand**; die einzige Ausnahme ist genau die Inkonsistenz, die der Entscheid beseitigt — der Weg aus W8‑B3 (`Form_Brauchwasser_Admin` → „Ergebnisse Verbrauch") zeigte „davon Brauchwasser" ohne den Teiler und damit um Faktor 1000 größer als derselbe Wert unter „Simulation". Das Säulenbild kommt in zwei Fassungen aus der Hülle — ein PNG lässt sich nicht umrechnen |
| **W8‑O‑5b** | Der Entscheid W8‑O‑5 legte die Quelleneinheit je Kennzahl in der Hülle fest — `Waermebedarf_Brauchwasser` in **kWh**, weil es aus `brauchwasserwerte.Sum()` kommt. Für die beiden Vorschauwege stimmt das (`Form_Brauchwasser_Admin`:84 und der Bedarfsprofildialog setzen die blanke Summe). Der **Simulationsweg** liefert dasselbe Feld aber schon in MWh: `SimulationWaermebedarf.Waermebedarf_berechnen`:393 rechnet `brauchwasserwerte.Sum() / 1000`, und `SimulationErgebnisHuelle`:193 reicht genau dieses Objekt in dieselbe Hülle. Unter Simulation → „Wärmebedarf-Details" wird der Wert deshalb ein **zweites Mal** geteilt und steht um den Faktor 1000 zu klein | **Anwenderentscheid ausstehend.** Ein Feld, vier Schreiber, zwei Einheiten — die Hülle kann nur eine annehmen. Sauber wäre, dass alle Schreiber `Waermebedarf_Brauchwasser` in MWh führen (so wie es der Kern tut und wie es seit dem Nachtrag zu W9‑O‑3 für `Waermebedarf_Prozess` durchgehend gilt); dann nennt die Hülle für **jede** Energiekennzahl MWh und kennt nur noch eine Regel. Das ändert zwei Anzeigen: Der Simulationsweg würde richtig, die beiden Vorschauwege müssten im selben Schritt auf MWh gehoben werden — sonst kippt dort die heute richtige Zahl. Der Nachtrag zu W9‑O‑3 hat den Brauchwasserzweig deshalb bewusst NICHT mitgedreht |
| **W8‑O‑6** | Der KI-Aufrufknopf fehlt in allen vier Dialogen (A‑14) | Mit W15b, wenn `Gespraechsverlauf` steht — wie W6‑O‑6 und W7‑O‑6 |
| **W8‑O‑7** | Die drei Renderer-Bilder sind neu gezeichnet; ein Bildvergleich mit den abgelösten WinForms-Charts ist nur von Hand möglich (`bildvergleich` ist mit iF23 gelöscht) | Abnahmepunkt 3. Die ChartProben sichern Maß, Farben und Determinismus |

---

## 11. Geänderte und neue Dateien

**Neu in `EPOS.UI`** (8): `Dialoge/Bedarf/BedarfErgebnisDaten.cs`,
`BedarfErgebnisDialog.razor`, `TypStammDaten.cs`, `TypStammDialog.razor`,
`TypProfilDaten.cs`, `TypProfilDialog.razor`, `GebaeudetypDaten.cs`,
`GebaeudetypDialog.razor`.

**Neu im Kern** (3): `Model/BedarfsArt.cs`, `Controller/BedarfStammCtrl.cs`,
`Controller/TypProfilCtrl.cs`.
**Geändert im Kern** (3 + Renderer + Ressourcen):
`Controller/ProzesswaermeStammCtrl.cs`, `Controller/TagVCtrl.cs`,
`Allgemein/Bericht/ChartRenderer.cs`; dazu die drei Ressourcendateien.

**Neu in der Anwendung** (3): die drei Hüllen unter `Views/Bedarf/`.

**Neu in den Tests** (6): vier bunit-Klassen in `EPOS.UI.Tests/Dialoge/`,
`EPOS.Kern.Tests/BedarfProfilTests.cs`, `EPOS.Kern.Tests/TagVCtrlTests.cs`.

**Geändert in den Proben**: `Proben/ChartProben/Program.cs` (drei Bilder).

---

## Windows-Abnahme 05.09.2026 — Bedarfsrechnung

### W8‑B‑1 — Bedarfsergebnis zeigt 0 und ein leeres Bild — **Ursache liegt nicht hier**

**Gemeldet** im PDF „iOS_Migration_Probleme", S. 4–5: Kachel „Prozesswärme" →
„Simulation…" → Überlagerung **Bedarfsergebnis** (`BedarfErgebnisDialog`,
`BedarfErgebnisDaten`) mit Einheit MWh: „Simulation bringt Ergebnis 0
(monatlicher Verlauf), Grafik bleibt leer" — das Bild „Prozesswärme [MWh]" zeigt
leere Achsen 0–5. Dasselbe beim Standardlastprofil.

**Der Ergebnisdialog ist entlastet, und die Einheitenwahl auch.** Der Verdacht
lag auf den beiden Nachträgen vom 04.09.2026 — W8‑O‑5 (`e665c41`, Einheit am Wert
statt Sonderteiler) und W9‑O‑3 (`a3906ca`, Prozesssumme über die
Einheitenklasse) —, weil eine zweimal angewandte Umrechnung kWh→MWh den Faktor
10⁻⁶ ergäbe und damit gerundet 0. Geprüft und ausgeschlossen:

* `ProzesssummeUebernehmen()` hat **genau einen** Aufrufer
  (`BedarfsProfileHuelle`:398).
* `Energieeinheit.MWh.AusMWh` ist die **bitgleiche Identität** — bei der Vorgabe
  MWh wird überhaupt nicht gerechnet (`EnergieeinheitTests`).
* Kein Feldname der Ergebnis-DTOs ist vertauscht: `Waermebedarf_Prozess_Monat`
  steht in der Sicht „Prozesse", `Waermebedarf_Gebaeude_Monat` in „Gebäude".
* Der Ergebnisdialog bekommt die Reihe **schon leer** — die Monatswerte sind
  bereits im Rechenobjekt 0.

**Die eigentliche Ursache** ist die Namensauflösung der Vorschau im
Bedarfsprofil-Dialog (Welle 9): Der Dialog gibt die Namen der **Projektkopien**
weiter, die Vorschau schlug sie ausschließlich im `_STAMM`-Katalog nach. Analyse,
Behebung, Wache und Abnahmepunkte stehen im Protokoll der Welle 9 unter
**W9‑B‑4** (Prozesswärme) und **W9‑B‑5** (Standardlastprofil); behoben mit
`b8090b0`, Zeuge `66c80b6`.

**Auch der Renderer ist entlastet.** Die leeren Achsen 0–5 sind das korrekte Bild
einer reinen Nullreihe: `ChartRenderer.MonatsSaeulen` ermittelt `maxWert = 0`,
bekommt daraus die Vorgabeskala und zeichnet zwölf Säulen der Höhe 0
(`hoehe > 0` ist die Zeichenbedingung). Bei einer Reihe mit Werten zeichnet er
die Säulen — nachgewiesen durch `Proben/ChartProben` (32 Bilder, 0 Verstöße). Am
Renderer wurde **nichts geändert**.

**Was in Welle 8 unverändert bleibt:** die Einheitenwahl MWh/kWh, das doppelt
gerenderte Säulenbild (eine Fassung je Einheit), die Einheit am Wert und die
Sonderstellung des Brauchwassers in kWh (offener Punkt W8‑O‑5b).
