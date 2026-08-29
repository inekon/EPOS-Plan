# D3 — Kosmetikreste der Dialogprüfung

**Auftrag:** 28.08.2026, Abschlusspaket zu [D-Check](DCheck_Dialoge_Protokoll.md) (Prio 3/4) und
[D2](D2_FusszeilenNorm_Protokoll.md) (Abschnitt 7, offene Punkte).
**Stand vor dem Paket:** Branch `Pufferspeicher`, HEAD `eb0957c`.
**Kein Commit, kein Push, kein Branchwechsel.** Die produktive `Kenndaten.accdb` wurde
ausschließlich gelesen.

**Keine Designer- und keine Formular-`.resx` angefasst.** Alle Korrekturen laufen zur Laufzeit.
Der eine neue Anzeigetext (Punkt E) steht im zentralen Katalog `MyResource/Resource*.resx`
(de + en) — nicht in `Form_WP.resx`.

---

## 0. Ergebnis auf einen Blick

| Punkt | Rest aus D-Check/D2 | Ergebnis |
|---|---|---|
| **A** | `Form_WP`: unterste 28 px auf 1280×800 unerreichbar | **erledigt** — Bildlaufbereich 729 → 741 px, Fußzeile vollständig erreichbar |
| **B** | Bildlauf-Ratchet: „die Rollfläche wird nie wieder kleiner" | **erledigt** — folgt dem Sollmaß in beide Richtungen (826 ↔ 976) |
| **C** | drei 1-px-Berührungen (`Form_Simulation_Detail`, Bedarfsseite) | **erledigt** — je 2 px Luft, gemessen statt festgeschrieben |
| **D** | Kartenrand 19 vs. Fußzeile 12 in `Form_Simulation_Config` | **erledigt** — rechte Flucht durchgehend 12 px (der 19er stammte NICHT vom Rollbalken) |
| **E** | `Form_WP.btn_Beenden` trägt „OK" | **erledigt** — Aufschrift „Beenden" / „Finish", Verhalten unverändert |
| **F** | 26 Fremdschriftgruppen | **teilweise** — 5 Gruppen (24 Steuerelemente) angeglichen, 21 begründet stehen geblieben |

**Messlauf über alle 12 Prüffälle, gleiche Bedingungen vorher/nachher:**

| Klasse | vorher | nachher |
|---|---|---|
| **a** Überlappung | **3** | **0** |
| **b** Beschneidung | 0 | 0 |
| **c** Abschnitt | 0 | 0 |
| **d** Fenstermaß > Arbeitsfläche | 6 | 6 (unverändert, systembedingt) |
| **e** Fremdschrift | **26** | **21** |
| **f** Tabreihenfolge | 0 | 0 |
| **Summe** | **35** | **27** |

Berichte: `dev\dcheck_befunde_VORHER752.txt` (HEAD `eb0957c`) gegen
`dev\dcheck_befunde_D3.txt` (Arbeitsstand).

---

## 1. Eine Korrektur am Messinstrument vorweg: 1280×800 → 1280×752

Der Messlauf erzwang bisher eine **Arbeitsfläche** von 1280 × 800. Das ist die **Bildschirm**-,
nicht die Arbeitsfläche eines 1280×800-Notebooks — die Taskleiste nimmt 48 px, es bleiben 752.
Genau gegen **752** prüft `KlemmungPruefen` seit dem D-Check die Klasse d; der Lauf gestand
`FensterEinpassung` also 48 px mehr zu, als der Anwender wirklich hat.

Für `Form_WP` war das der Unterschied zwischen „fällt auf" und „fällt durch": Bei 800 ist der
erreichbare Client 761 px hoch, der Entwurf 741 — der Dialog passt, es entsteht kein Bildlauf und
kein Befund. Bei den echten 752 sind es 713 erreichbare gegen 741 nötige Pixel.

Der Lauf steht jetzt auf **1280 × 752**. Alle Zahlen dieses Protokolls sind unter dieser Fläche
gemessen, vorher wie nachher — der Vorher-Lauf ist ein Bau aus `git archive HEAD` mit demselben
Prüfprogramm (`dev\ab_head\harness_vorher\`).

---

## 2. Punkt A — `Form_WP`: die untersten Pixel waren nicht erreichbar

### Befund

`FensterEinpassung` maß ihr Entwurfsmaß erst im `Load`. Zu diesem Zeitpunkt hat Windows das
Fenster längst auf Bildschirmgröße geklemmt — gemessen wurde damit der **Ausschnitt**, nicht der
Entwurf. Bei `Form_WP` kommt hinzu, dass die Schriftskalierung erst nach dem Konstruktor läuft und
aus 877 × 642 die tatsächlichen **1023 × 741** macht.

Der Bildlaufbereich fiel dadurch auf **1023 × 729** — das ist die Hüllfläche der Kindelemente
(Unterkante der Fußzeile bei y = 729), nicht der Entwurf. Gemessen am gerollten Dialog:

```
[ROLL] vorher : Anzeige 1023x729, ans Ende gerollt -> btn_Beenden ("OK") bei y = 682,
                Client-Höhe 696  =>  ABGESCHNITTEN
[ROLL] nachher: Anzeige 1023x741, ans Ende gerollt -> btn_Beenden ("Beenden") bei y = 654,
                Client-Höhe 696  =>  ERREICHBAR
```

### Lösung

Dieselbe Bezugsrahmen-Mechanik, die `FusszeilenNorm` in D2 bekommen hat, jetzt auch in
`FensterEinpassung`:

* neue Methode **`FensterEinpassung.EntwurfMerken(Form)`** — merkt die Client-Größe sofort und
  zieht sie bis zum `Load` bei **jeder Vergrößerung** nach (`ClientSizeChanged` + `Layout`).
  Ab `Load` endet die Beobachtung: danach kommen nur noch Klemmung und Bildlauf, die verkleinern
  würden.
* `Einhaengen` ruft sie selbst.
* **`BaseForm` ruft sie im Konstruktor** — dort läuft sie VOR `InitializeComponent` des Nachfahren
  und nimmt damit erst die Entwurfsgröße und danach die Schriftskalierung mit. `Form_WP` ruft
  `Einhaengen` gar nicht; seine Einpassung kommt seit jeher aus `BaseForm.OnLoad`. `BaseForm` ist
  die „gemeinsame Stelle für alle Nachfahren", die der Kommentar dort schon benennt.

### Regressionsprobe an den anderen fünf über-800-px-Dialogen

| Dialog | Bildlaufbereich vorher | nachher | Entwurf |
|---|---|---|---|
| `Form_WP` (beide Modi) | 1023 × **729** | 1023 × **741** | 1023 × 741 |
| `Form_Waermesenke` | 620 × **809** | 620 × **821** | 620 × 821 |
| `Form_PufferSp_Projekt` zu | 700 × **816** | 700 × **826** | 700 × 826 |
| `Form_PufferSp_Projekt` auf (N = 5) | 700 × **964** | 700 × **976** | 700 × 976 |
| `Form_Simulation_Detail` | 1474 × **773** | 1474 × **821** | 1474 × 821 |

Jeder der fünf rollte bisher **bis zur Hüllfläche der Kindelemente**, nicht bis zur
Entwurfsunterkante — die letzten 10 bis 48 px (der untere Rand unter der Fußzeile) fehlten. Jetzt
deckt der Bildlauf in allen Fällen genau den Entwurf ab.

**Auf einem großen Schirm bleibt alles unverändert:** Im Vergleichslauf mit erzwungener
Arbeitsfläche 1920 × 1040 steht `AutoScrollMinSize` bei **allen** Dialogen auf 0 × 0 — vorher wie
nachher. Die Zusicherung der Klasse („auf großen Schirmen ändert sie NICHTS") gilt weiter.

---

## 3. Punkt B — der Bildlauf-Ratchet

### Befund

`BildlaufSichern` rechnete `soll = max(bisher, inhalt)` und kehrte, wenn der Inhalt passte, ohne
Schreiben zurück. Der Bildlaufbereich konnte damit nur wachsen. Klappte der Anwender die
Schichtgruppe von `Form_PufferSp_Projekt` wieder zu, blieb die Rollfläche auf dem größten je
erreichten Maß stehen und der Rollbalken 150 px länger als nötig.

### Lösung

1. **`BildlaufSichern` führt `AutoScrollMinSize` in beide Richtungen.** Der Bereich folgt dem
   aktuellen Inhaltsmaß; **Untergrenze ist ausschließlich der Wert, den das Formular selbst
   mitgebracht hat** (`Zustand.BildlaufVorher`, einmalig vor dem ersten eigenen Schreiben
   erfasst). Zurückgenommen wird also nur der eigene Aufschlag. Passt der Inhalt wieder
   vollständig, fällt der Bereich auf genau diesen Ausgangswert zurück.
2. **`FensterEinpassung.SollmassSetzen(Form, Size)`** — neu, das Gegenstück zu
   `FusszeilenNorm.BezugSetzen`. Damit gibt `Form_PufferSp_Projekt` seine **ungeklemmte**
   Sollhöhe `_schichtSollHoehe` ausdrücklich mit, bevor es `Anwenden` ruft. Ohne das läge das
   Inhaltsmaß bei der Hüllfläche (964) statt beim Sollmaß (976), und beim Zuklappen bliebe das
   einmal gemerkte Entwurfsmaß stehen.

### Doppelprobe (neuer Harness-Modus `Ratchetprobe`, Arbeitsfläche 1280 × 752)

| Schritt | vorher `AutoScrollMinSize` | vorher Client-Breite | nachher `AutoScrollMinSize` | nachher Client-Breite |
|---|---|---|---|---|
| Start (N = 1) | 700 × 816 | 683 | 700 × **826** | 683 |
| N = 5 | 700 × 964 | 666 | 700 × **976** | 683 |
| N = 1 | 700 × **964** ✗ | 649 | 700 × **826** ✓ | 683 |
| N = 5 | 700 × 964 | 632 | 700 × **976** ✓ | 683 |
| N = 1 | 700 × **964** ✗ | 615 | 700 × **826** ✓ | 683 |

Der Abstand der Fußzeile zur Unterkante der Rollfläche war vorher **150 px** im zugeklappten
Zustand (der Leerraum des Ratchets) und **0 px** im aufgeklappten; nachher steht er in **jedem**
Zustand auf den 12 px der Norm.

Auf 1920 × 1040 bleibt `AutoScrollMinSize` in allen fünf Schritten 0 × 0 — vorher wie nachher.

### Beifang der Doppelprobe: das Fenster wurde bei jedem Umschalten 17 px schmaler

Die Spalte „Client-Breite" oben zeigt es: `SchichtSichtbarkeitSetzen` schrieb die Breite mit
`this.ClientSize.Width` zurück. Bei sichtbarer senkrechter Bildlaufleiste ist dieser Wert um deren
Breite (17 px) kleiner als das Fenster — fünf Umschaltvorgänge kosteten gemessen **68 px**
(716 → 648). Neues Feld `_schichtSollBreite` (Gegenstück zu `_schichtSollHoehe`); geschrieben wird
`Math.Max(ClientSize.Width, _schichtSollBreite)`, ein vom Anwender aufgezogenes Fenster bleibt also
breit. Nach der Korrektur steht die Breite über alle fünf Schritte konstant bei 683.

### Größenprobe aus D2 unverändert

Zwei Dialoge auf vier Fenstergrößen: **8 von 8** Messungen weiter bei 12/12 und 110 × 30,
Anker `Bottom, Right` (`[PROBE]`-Zeilen in `dev\dcheck_befunde_D3.txt`).

---

## 4. Punkt C — die drei 1-px-Berührungen

Alle drei auf der Bedarfsseite von `Form_Simulation_Detail`, Register „Simulation":

| Paar | Schnitt vorher |
|---|---|
| `label20` ↔ `textBox_MaxStrombedarf` | 85 × 1 px |
| `label1` ↔ `textBox_MaxWaermelast` | 70 × 1 px |
| `label1` ↔ `label14` | 24 × 1 px |

**Ursache ist keine krumme Entwurfskoordinate, sondern die Schriftskalierung.** Im Designer sind
die Beschriftungen 13 px hoch (445 + 13 = 458 gegen Feldoberkante 463), auf dem Bildschirm mit
`AutoSize` 19 px (445 + 19 = 464 gegen 463). Ein fester Zahlenwert im Designer könnte das nicht
auffangen.

Neue Methode `BedarfMaximalzeilenEntzerren()` in `Form_Simulation_Detail`: Sie misst die
**tatsächliche** Unterkante der Beschriftung und rückt die Zeile darunter als **Gruppe** um
denselben Betrag nach, sodass 2 px Luft bleiben. Verschoben werden je Zeile Feld **und**
Einheitenzeichen (`label19` berührte mit 0 px Schnittfläche und war formal kein Befund, gehört
aber zur selben Zeile) — der 1-px-Versatz des Entwurfs bleibt damit erhalten und die Zeile bleibt
in der Flucht. Nach der Korrektur bleiben bis zur nächsten Zeile 11 bis 12 px.

Gerufen wird sie im `Load` (vor jedem Rücksprung, damit sie auch im blockierten Zustand läuft) und
zusätzlich an `FontChanged` von `label1`/`label20` — die Seitenleiste leiht die Steuerelemente der
Seite in ein Panel mit anderer Schrift aus, Muster D8 des D-Checks. Die Rechnung ist absolut und
damit beliebig oft aufrufbar.

Beleg: `dev\dcheck_bilder\Form_Simulation_Detail_SEITE_tabPage_Bedarf_D3.png`.

---

## 5. Punkt D — Kartenrand gegen Fußzeile in `Form_Simulation_Config`

**Der 19er-Rand stammte NICHT aus dem Platz für den Rollbalken** — den zieht
`KartenBreiteAnpassen` innerhalb der Spalte ab (`SystemInformation.VerticalScrollBarWidth`, mit
eigener Begründung gegen das Pendeln des Layouts). `KARTEN_RAND = 19` war schlicht dieselbe Zahl
wie links, wo sie zu den Entwurfselementen passt (`label11` bei 18, `groupBox_Tools` bei 19).
Der Punkt ist damit nicht „begründet", sondern zu ändern.

**Geändert wurde nur die rechte Seite.** Links bleibt alles bei 19 — sonst stünde die Kartenspalte
nicht mehr unter ihrer Überschrift. Rechts gilt jetzt das Randmaß der Norm
(`KARTEN_RAND_RECHTS = FusszeilenNorm.RAND` = 12), und zwar für die Kartenfläche
(`tableLayout_Karten`), für den Ansichtsumschalter (`UmschalterPlatzieren`) und über
`flow_Speicher.Margin` auch für die graue Fläche der rechten Spalte selbst — die 8 px dort sind
der ZWISCHENraum der beiden Spalten und wären außen ein zusätzlicher Rand gewesen.

Messung (neuer Harness-Modus `Randprobe`, Client 1120 × 620):

| Element | rechter Abstand vorher | nachher |
|---|---|---|
| `btn_AnsichtSchema` (Umschalter) | 19 | **12** |
| `tableLayout_Karten` | 19 | **12** |
| `flow_Speicher` (graue Fläche) | 27 | **12** |
| `btn_OK` (Fußzeile) | 12 | 12 |

Die Schema-Ansicht folgt automatisch: `panel_Schema` übernimmt Rechteck und Verankerung von
`tableLayout_Karten`.

Sichtprobe: `dev\dcheck_bilder\Form_Simulation_Config_Start_D3.png` — Umschalter, Kartenfläche und
Knopfreihe enden in einer Flucht (vorher `…_Start_D2.png`).

---

## 6. Punkt E — die Knopfrolle in `Form_WP`

### Was der Knopf heute war

| | |
|---|---|
| Name | `btn_Beenden` |
| Text | **„OK"** — in `Form_WP.resx` UND `Form_WP.de-DE.resx`; `Form_WP.en-US.resx` hat keinen Eintrag, englisch stand also ebenfalls „OK" |
| `DialogResult` | `None` |
| Behandler | `butt_Beenden_Click` → `CloseWithOK = true; Close();` |

**`Form_WP.CloseWithOK` liest niemand.** Die beiden Aufrufer (`Controller/MenueCtrl.cs:252` und
`Views/Wizard/Wizard_WPItem.cs:599`) werten das Feld nicht aus; die Treffer in
`Form_WPAuswahl.cs:245/272` gehören zum gleichnamigen Feld von `Wizard_WPItem`. Der Knopf
**schließt also nur** — gespeichert wird ausschließlich über „Speichern".

### Was geändert wurde

Nur der **Text**: „Beenden" / „Finish". Damit stimmen Aufschrift, Knopfname und Verhalten
überein, und der Dialog liest sich wie die beiden baugleichen Fälle im Bestand
(`Form_Simulation_Config.btn_OK` und `Form_Simulation_Detail.btn_Beenden` zeigen beide „Beenden",
englisch „Finish", bei ebenfalls `DialogResult = None`).

**Die Reihenfolge bleibt.** Nach der Norm steht die Primäraktion rechts — bei einem
Verwaltungsdialog mit „Speichern" und „Beenden" ist das im Bestand einheitlich der
**Abschlussknopf** (so normiert D2 auch `Form_Simulation_Config`: von rechts „Beenden", links
daneben „Konfiguration speichern"). `Form_WP` erfüllt das bereits.

**Kein Verhalten geändert:** kein `DialogResult`, kein Behandler, kein Speicherweg.

**Kein Eingriff in die Formular-`.resx`.** Der Text kommt aus dem zentralen Katalog
(`MyResource.Resource.WP_BTN_BEENDEN`, de „Beenden" / en „Finish") und wird in `FusszeileNormen()`
gesetzt — vor `FusszeilenNorm.Einhaengen`, damit die Norm die Mindestbreite am neuen Text misst.
Die Fußzeile bleibt unverändert bei 110 × 30 / 12 / 12.

Der Katalogeintrag `KiDialoge.Waermepumpe()` behält die Rolle „ok" (wie
`Form_Simulation_Config.btn_OK`, das ebenfalls „Beenden" zeigt); sein Kommentar ist nachgezogen.

> **Fallstrick beim Ressourcenschlüssel.** Visual Studio hatte `Resource.Designer.cs` bereits
> selbst um `WP_BTN_BEENDEN` ergänzt, bevor der erste Build lief — zusammen mit der
> Hand-Einfügung gab das CS0102. Wie in `CLAUDE.md` beschrieben: die Hand-Einfügung entfernt, die
> generierte behalten.

Beleg: `dev\dcheck_bilder\Form_WP_Pflege_Gerollt_D3.png` — der ans Ende gerollte Dialog mit der
vollständigen Fußzeile Speichern / Neu / Löschen / **Beenden**.

---

## 7. Punkt F — die 26 Fremdschriftgruppen

**Regel für dieses Paket.** Geändert wird eine Gruppe nur, wenn (1) die Familie dieselbe ist,
(2) das Steuerelement keine Titel-, Kennzahlen- oder Hervorhebungsrolle hat, (3) es ein BLATT ist
(kein Behälter, von dem ganze Seiten erben) und (4) die Nachmessung keine neuen Befunde der
Klassen a/b/c bringt. Die Nachmessung ist der eigentliche Prüfstein: Ein Schriftwechsel ändert
Textmaße und Steuerelementhöhen.

**Ergebnis der Nachmessung: a = 0, b = 0, c = 0 — kein einziger neuer Befund.**

### 7.1 Geändert — 5 Gruppen, 24 Steuerelemente

| Dialog | Gruppe (Formularschrift) | Steuerelemente | Änderung |
|---|---|---|---|
| `Form_PufferSp` | Segoe UI **10** (Formular 9,75) | `listBox_Pufferspeicher`, `listBox_Pufferspeicher_DB`, `textBox_Hersteller`, `textBox_Name`, `label6`, `label7`, `label11`, `label12`, `label16`, `label18`, `btn_PufferSp_Hinzu`, `btn_PufferSp_Entfernen` (12) | `Font = null` → erbt 9,75 |
| `Form_PufferSp` | Segoe UI **8** (Formular 9,75) | `btn_OK`, `btn_Abbrechen`, `textBox_Investitionskosten` (3) | `Font = null` → erbt 9,75 |
| `Form_Prozesswaerme` | Segoe UI **8** (Formular 10) | `textBox_Verbrauch`, `textBox_Prozess_Name`, `textBox_Jahres_Verbrauch`, `textBox_Beschreibung`, `textBox_Prozess_Type`, `textBox_SummeProzesswaerme` (6) | `Font = null` → erbt 10 |
| `Form_Prozesswaerme` | Segoe UI **9,75 fett** (Formular 10) | `btn_Hinzu`, `btn_Entfernen` (2) | `new Font(this.Font, Bold)` — **Fettung bleibt**, nur die Größe folgt |
| `Form_Simulation_Detail` | Segoe UI **8** (Formular 9) | `ueb_textBox_WPWaermeproduktion` (1) | `Font = null` → erbt die 9,75 der Übersichtsseite |

**Warum `Font = null` und nicht `Font = this.Font`.** Null setzt die Eigenschaft auf „nicht
gesetzt" zurück; das Steuerelement erbt danach dauerhaft von seinem Elternteil. Ein zugewiesenes
Font-Objekt wäre eine neue feste Schrift, die einer späteren Änderung nicht mehr folgt.

**Warum namentlich und nicht über eine Heuristik.** „Alles, was nicht der Formularschrift
entspricht" hätte in beiden Dialogen `label_Type` (das Kopfband) mitgerissen.

**Die engste Stelle nach dem Wachsen der Textfelder** (8 → 10 pt heißt +3 px Höhe) liegt bei
`Form_Prozesswaerme`: `textBox_Jahres_Verbrauch` endet bei 499, `textBox_SummeProzesswaerme`
beginnt bei 501 — 2 px. Alle übrigen liegen bei 5 px und mehr; `textBox_Beschreibung` ist
mehrzeilig und behält seine Höhe ohnehin. `ueb_textBox_WPWaermeproduktion` wächst auf 25 px, die
Höhe seiner drei Nachbarn in derselben Spalte, und behält 4 px zur Zeile darunter.

Der Schriftbild-Nachweis (neuer Harness-Abschnitt „Schriftverteilung je Dialog"):

```
vorher  Form_PufferSp:       Segoe UI 9.75 x16 | Segoe UI 10 x12 | Segoe UI 8 x3  | Segoe UI 12 fett x1
nachher Form_PufferSp:       Segoe UI 9.75 x31                                   | Segoe UI 12 fett x1
vorher  Form_Prozesswaerme:  Segoe UI 10   x26 | Segoe UI 8  x6  | Segoe UI 9.75 fett x2 | Segoe UI 12 fett x1
nachher Form_Prozesswaerme:  Segoe UI 10   x32 | Segoe UI 10 fett x2                     | Segoe UI 12 fett x1
```

### 7.2 Begründet stehen geblieben — 21 Gruppen

| Dialog | Gruppe | Steuerelemente | Einstufung |
|---|---|---|---|
| `Form_Prozesswaerme` | Segoe UI 12 fett | `label_Type` | **Absicht** — Kopfband der Maske |
| `Form_PufferSp` | Segoe UI 12 fett | `label_Type` | **Absicht** — Kopfband |
| `Form_Waermebedarf` | Segoe UI 12 fett | `label_Type` | **Absicht** — Kopfband |
| `Form_Simulation_Config` | Segoe UI 12 fett | `label11` | **Absicht** — Überschrift „Erzeuger definieren …" |
| `Form_Simulation_Config` | Segoe UI 9 fett | namenloses `Label` | **Absicht** — Kartentitel (`KartenStil`), bewusst hervorgehoben |
| `Form_WP_Pflege` / `_Ansicht` | Segoe UI 12 | `label1` | **Absicht** — Kopfband „Verwaltung Daten zu Wärmepumpen …" |
| `Form_WP_Pflege` / `_Ansicht` | Segoe UI 10 (je 30) | `btn_Speichern`, `btn_Neu`, `btn_Loeschen`, `btn_Beenden`, `label7`, `label8`, … | **Bestandsschrift, nicht der Ausreißer.** Der Dialog hat 42 Steuerelemente, **30 davon tragen die 10 pt** und nur 11 die Formularschrift 9. Die Mehrheit trägt die Maske; sie auf 9 pt zu ziehen würde 30 von 42 Beschriftungen verkleinern und ein Designer-Layout umbrechen. Der eigentliche Ausreißer ist die Formularschrift — und die zu ändern hieße, das Formular mit `AutoScaleMode.Font` neu zu skalieren |
| `Form_Simulation_Detail` | Segoe UI 9,75 (57) | `tabPage_Uebersicht`, `ueb_label*`, `ueb_chart`, … | **Behälterschrift** — die ganze Übersichtsseite erbt sie |
| `Form_Simulation_Detail` | Segoe UI Semibold 12 fett (13) | `tabControl_Simulation`, `tabPage_Parameter`, `tabControl_Einstellungen*`, Panels | **Behälterschrift** — Register und Panels; ein Wechsel schlägt auf > 100 Kinder durch |
| `Form_Simulation_Detail` | Segoe UI 12 (36) | `listViewQuellen`, `btn_Beenden`, `chart2`, SplitterPanel, … | **Behälterschrift** der Seitenleiste und der Diagrammfläche |
| `Form_Simulation_Detail` | Segoe UI 10 (120) | `chk_BedarfKanal*`, `label17`, `label18`, … | **Bestandsschrift ganzer Seiten** — ein Umstellen wäre eine Neugestaltung, kein Kosmetikrest |
| `Form_Simulation_Detail` | Segoe UI 10 fett (38) | `textBox_gesStrombedarf`, `label60`, … | wie vor, zusätzlich **Hervorhebung** der Summenzeilen |
| `Form_Simulation_Detail` | Segoe UI 10,5 fett (12) | `label_SpKernWert_*` | **Absicht** — Kennzahlenkacheln des Stromspeichers (Wertzeile) |
| `Form_Simulation_Detail` | Segoe UI 7,75 (12) | `label_SpKernTitel_*` | **Absicht** — Kennzahlenkacheln (Titelzeile, bewusst klein) |
| `Form_Simulation_Detail` | Segoe UI 12 fett (2) | `ueb_label1`, `ueb_label13` | **Absicht** — Überschriften der Übersichtsseite |
| `Form_Simulation_Detail` | Segoe UI 9,75 fett (4) | `ueb_textBox_gesStrombedarf`, `…_Reststrombedarf`, `…_gesWaermebedarf`, `…_Restwaermebedarf` | **Absicht** — die vier Summenfelder der Übersicht |
| `Form_Simulation_Detail` | Segoe UI Semibold 8 (4) | `ueb_textBox_SPKStromverbrauch`, `…_PVStromproduktion`, `…_HeizstabStromverbrauch`, `…_WPStromverbrauch` | **Absicht** — die Stromspalte der Übersicht ist als Gruppe abgesetzt (siehe 7.1: der EINE Ausreißer daneben wurde geheilt) |
| `Form_Simulation_Detail` | Segoe UI Semibold 9,75 fett (1) | `btn_Simulation` | **Absicht** — der Startknopf ist bewusst hervorgehoben |
| `Form_Simulation_Detail` | Segoe UI 8,25 (1) | namenloses `Label` aus `SpHinweisAnlegen` | **Absicht** — die Methode heißt „Kleingedruckter Hinweis (grau, mehrzeilig)" und setzt zusätzlich `ForeColor` Grau |

Die beiden `Form_WP`-Einträge und die beiden `Form_WP`-Kopfbänder erscheinen doppelt, weil beide
Konstruktormodi (Pflege / Ansicht) als eigene Prüffälle gemessen werden — physisch ist es je eine
Gruppe.

**Erwartungsgemäß bleibt ein Teil stehen.** Die verbliebenen 21 Gruppen sind zu zwei Dritteln
Behälter- und Bestandsschriften eines einzigen Formulars (`Form_Simulation_Detail`, 12 der 21);
ein „Schriftbild vereinheitlichen" dort ist eine Neugestaltung mit eigenem Auftrag, kein
Kosmetikrest.

---

## 8. Verifikation

| Prüfung | Ergebnis |
|---|---|
| Rebuild der Solution (`WP-Plan.sln`, Debug × x64, eigener `OutDir` im Scratch) | **0 Fehler**, 5 Warnungen — alle fünf unverändert aus dem Bestand (`WErzeugerModel`, `KlimaregionStammCtrl` ×2, `StromverbraucherStammCtrl`, `MDIMainForm`) |
| Harness über alle 12 Prüffälle, vorher/nachher unter identischer Arbeitsfläche | **a 3 → 0, b 0 → 0, c 0 → 0, f 0 → 0**, keine neuen Befunde; e 26 → 21; d 6 → 6 |
| Ratchet-Doppelprobe (Abschnitt 3) | 826 ↔ 976 in **beide** Richtungen, Fensterbreite konstant |
| Rollprobe `Form_WP` (Abschnitt 2) | Fußzeile von ABGESCHNITTEN auf ERREICHBAR |
| Randprobe `Form_Simulation_Config` (Abschnitt 5) | vier Elemente, rechte Flucht durchgehend 12 px |
| Größenprobe aus D2 | **8 von 8** unverändert bei 12/12 und 110 × 30 |
| Fußzeilentabelle | `dev\dcheck_fusszeilen.tsv` — Geometrie unverändert; einzige Änderung: `Form_WP.btn_Beenden` Text „OK" → „Beenden"; `Form_PufferSp_Projekt` aufgeklappt jetzt Abstand unten 12 statt 0 |
| PNG-Belege | `dev\dcheck_bilder\*_D3.png` — 41 Bilder, darunter `Form_WP_Pflege_Gerollt_D3.png` |
| Byte-Gate A/B gegen den unveränderten HEAD | **9 von 9 Projekten PASS** (2 427 709 Werte in Toleranz), **226 von 226 CSV byte-gleich**, 0 abweichend, 0 fehlend |
| Produktive `Kenndaten.accdb` | MD5 `CD03076D39E8436D2497544B74246890`, mtime 28.08.2026 12:36:09 — **vor dem A-Lauf, zwischen A und B und nach dem B-Lauf identisch** |
| Kodierungen | 13 geänderte Dateien, alle mit unveränderter Kodierung, durchgehend CRLF, kein U+FFFD |

### 8.1 Byte-Gate

A = `git archive HEAD` (`eb0957c`) nach `dev\ab_head\`, dort `Referenzlauf` gebaut (Debug × x64,
eigener `OutDir`) und gelaufen (`lauf --ziel dev\ab_csv_A`); B = Arbeitsstand, gebaut in den
Scratch und gelaufen (`--ziel dev\ab_csv_B`). A/B statt Vergleich gegen einen eingefrorenen
Referenzstand, weil die produktive Quelle durch den arbeitenden Anwender driftet — beide Läufe
lagen 23 Sekunden auseinander und die Quelle war dazwischen nachweislich unverändert.

```
Projekt_1011: PASS (29 Dateien, 324241 Werte)      Projekt_1030: PASS (22 Dateien, 236667 Werte)
Projekt_1017: PASS (21 Dateien, 254152 Werte)      Projekt_1038: PASS (18 Dateien, 201546 Werte)
Projekt_1021: PASS (21 Dateien, 227854 Werte)      Projekt_1040: PASS (30 Dateien, 306763 Werte)
Projekt_1023: PASS (25 Dateien, 262935 Werte)      Projekt_1042: PASS (34 Dateien, 341836 Werte)
Projekt_1024: PASS (26 Dateien, 271715 Werte)
GESAMT: PASS (2427709 Werte innerhalb der Toleranz)   ·   226 CSV je Seite, 226 byte-gleich
```

Die Zusage „UI-only" ist damit belegt: **keine einzige Ergebniszahl hat sich geändert.**

### 8.2 Kodierung

`Views/Prozesswärme/Form_Prozesswaerme.cs` ist Windows-1252, nicht UTF-8. Bearbeitet über den
iconv-Hinweg mit Rundprobe in beide Richtungen (Hinweg byte-identisch zum Original, Rückweg
byte-identisch zur bearbeiteten Fassung). Der eingefügte Text ist bewusst reines ASCII; nach dem
Paket 591 CR / 591 LF, kein Ersatzzeichen. Alle übrigen zwölf Dateien sind UTF-8 mit BOM und
bleiben es.

---

## 9. Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/FensterEinpassung.cs` | **A + B** — `EntwurfMerken()`, `SollmassSetzen()`, `Nachziehen()`, `BeobachtungBeenden()` neu; `Einhaengen()`, `Einpassen()` und `BildlaufSichern()` geändert; `Zustand` um Beobachtung und Bildlauf-Untergrenze erweitert |
| `Allgemein/BaseForm.cs` | **A** — `FensterEinpassung.EntwurfMerken(this)` im Konstruktor |
| `Views/Pufferspeicher/Form_PufferSp_Projekt.cs` | **B** — `SollmassSetzen` vor `Anwenden`; neues Feld `_schichtSollBreite` gegen das Schrumpfen der Fensterbreite |
| `Views/Simulation/Form_Simulation_Detail.cs` | **C** — `BedarfMaximalzeilenEntzerren()` + `ZeileUnterBeschriftung()` neu, Aufruf im `Load` und an `FontChanged`; **F** — `ueb_textBox_WPWaermeproduktion.Font = null` |
| `Views/Simulation/Form_Simulation_Config.Karten.cs` | **D** — `KARTEN_RAND_RECHTS`, Kartenfläche und rechte Spalte auf das Normmaß |
| `Views/Simulation/Form_Simulation_Config.Schema.cs` | **D** — Ansichtsumschalter auf dasselbe Randmaß |
| `Views/Wärmepumpe/Form_WP.cs` | **E** — `btn_Beenden.Text` aus dem zentralen Katalog, Begründung am Kommentar |
| `Allgemein/KI/Dialoge/KiDialoge.cs` | **E** — Kommentarnachzug zur neuen Aufschrift |
| `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | **E** — neuer Schlüssel `WP_BTN_BEENDEN` („Beenden" / „Finish") |
| `Views/Pufferspeicher/Form_PufferSp.cs` | **F** — `SchriftAngleichen()` neu |
| `Views/Prozesswärme/Form_Prozesswaerme.cs` | **F** — `SchriftAngleichen()` neu (CP1252) |
| `Allgemein/Simulation/D3_Kosmetikreste_Protokoll.md` | dieses Protokoll |

Werkzeugseitig (unter `dev/`, gitignored): `dev\harness_dcheck\Program.cs` um die Ratchet-Doppelprobe,
die Rollprobe, die Randprobe und die Schriftverteilung erweitert; Prüfarbeitsfläche auf 1280 × 752
korrigiert. `dev\ab_head\harness_vorher\` ist derselbe Messcode gegen den unveränderten HEAD.

---

## 10. Was offen bleibt

| Prio | Punkt | Warum |
|---|---|---|
| **3** | Sechs Dialoge sind größer als eine 1280×752-Arbeitsfläche (Klasse d) | Bekannt und **abgefangen** — seit diesem Paket rollt der Bildlauf bei allen sechs bis zur Entwurfsunterkante. Ein echter Umbau wäre eine Neugestaltung |
| **4** | 21 Fremdschriftgruppen (Abschnitt 7.2) | Behälter- und Bestandsschriften, Titel und Kennzahlenkacheln. Zwölf der 21 sitzen in `Form_Simulation_Detail`; dort wäre „Schriftbild vereinheitlichen" ein eigenes Gestaltungspaket |
| **4** | `Form_WP` trägt seine Maske auf Segoe UI 10 bei Formularschrift 9 | Umzustellen wäre die FORMULARschrift — mit `AutoScaleMode.Font` eine Neuskalierung des ganzen Dialogs |
