# iU9 Welle 7 — Wärmepumpe und Solarthermie — Portprotokoll

> Umsetzung 03.09.2026 auf `ios_migration`, Basis `198506f` (nach dem Merge der
> Welle 6), zusammengeführt mit `origin/ios_migration` (`98ebe81`). Vorbild in Aufbau
> und Tiefe: das Protokoll der Welle 6 im selben Ordner. Regeln: Wellenplan
> Abschnitt F, `EPOS.UI/CLAUDE.md`, `EPOS.Kern/CLAUDE.md`.

---

## 1. Auftrag und Ergebnis

**Acht WinForms-Masken der Gewerke Wärmepumpe (5) und Solarthermie (3)** sind
Razor-Komponenten in `EPOS.UI/Dialoge/Waermepumpe/` und
`EPOS.UI/Dialoge/Solarthermie/`; ihre WinForms-Fassungen sind im selben Commit
gelöscht (Regel M1). Zusammen **3 065 Zeilen** Oberflächencode, **43
`MessageBox`**-Aufrufe und **178 Kartenzeilen**.

| Maske | Zeilen | Komponente | Hülle |
|---|---|---|---|
| `Form_WpFilterAuswahl` | 325 | `WaermepumpenKatalogDialog` | keine (nur Überlagerung) |
| `Kenndaten` | 190 | `KennlinienEditorDialog` | keine (nur Überlagerung) |
| `Form_WP` | 585 | `WaermepumpeStammDialog` | `Views/Wärmepumpe/WaermepumpeStammHuelle.cs` |
| `Wizard_WPItem` | 690 | `WaermepumpeAnlageDialog` | `Views/Wärmepumpe/WaermepumpeAnlageHuelle.cs` |
| `Form_WPAuswahl` | 341 | `WaermepumpenDialog` | `Views/Wärmepumpe/WaermepumpenHuelle.cs` |
| `Form_SolarDB` | 286 | `SolarkollektorKatalogDialog` | `Views/Solarthermie/SolarkollektorHuelle.cs` |
| `Form_SolarKollektoren` | 516 | `SolarkollektorenDialog` | dieselbe Datei |
| `Form_Solarganglinie` | 132 | `SolarganglinieDialog` | `Views/Solarthermie/SolarganglinieHuelle.cs` |

**Zwei davon sind zugleich Assistentenseiten** (Wärmepumpen-Verwaltung, Seite 7;
Solarkollektoren, Seite 8) — damit laufen sechs der dreizehn Assistentenseiten als
Razor-Komponente.

**Neu im Kern**: der Renderer `ChartRenderer.Kennlinien` (Bausteinlücke 12) samt zwei
Proben in `ChartProben`, der Umzug von `WPCtrl`, die Filterlogik des Katalogs, der
transaktionale Kennlinien-Abgleich und sieben Controller-Methoden.

### Commits

| Hash | Betreff |
|---|---|
| `2cd898a` | iU9-W7.0a: `WPCtrl` in den Kern, `FillListBox` entfällt |
| `0872196` | iU9-W7.0b: Katalogzeile, Katalogfilter und `KatalogZeilen` im Kern |
| `7fc9419` | iU9-W7.0c: `ChartRenderer.Kennlinien` samt Datenseite und zwei Proben |
| `0d1e6e4` | iU9-W7.0d: `KenndatenCtrl.Abgleichen` — Kennlinien in EINER Transaktion |
| `7da4f33` | iU9-W7.0e: sieben Datenwege der acht Masken in Kern-Controller |
| `808837b` | iU9-W7.0f: Sprungziel `SolarganglinieAdmin` |
| `29a1bf3` | iU9-W7.1: `WaermepumpenKatalogDialog` — die Filtersuche als Razor-Komponente |
| `555e770` | iU9-W7.2: `KennlinienEditorDialog` — die Stützstellen als Razor-Komponente |
| `5e71c49` | iU9-W7.6: `SolarkollektorKatalogDialog`, `Form_SolarDB` gelöscht |
| `b30f6bd` | iU9-W7.3: `WaermepumpeStammDialog`, `Form_WP` und `Kenndaten` gelöscht |
| `b98cf35` | iU9-W7.4: `WaermepumpeAnlageDialog`, `Wizard_WPItem` gelöscht |
| `a371328` | iU9-W7.5: `WaermepumpenDialog`, `Form_WPAuswahl` und `Form_WPFilterAuswahl` gelöscht |
| `0ad0a59` | iU9-W7.7: `SolarkollektorenDialog`, `Form_SolarKollektoren` gelöscht |
| `3655bce` | iU9-W7.8: `SolarganglinieDialog`, `Form_Solarganglinie` gelöscht |
| `35188f7` | iU9-W7.9: 157 Textschlüssel für die acht Masken, de und en |
| `0077533` | iU9-W7.10: Formularkarte-Tests auf den Stand nach Welle 7 |
| `8904cd7` | Merge `origin/ios_migration` (Statusblock W6, iU10-Nachweise) in Welle 7 |

**Blätter vor Wurzeln:** Erst die drei Blätter (W7.1, W7.2, W7.6), dann die
Katalogpflege (W7.3), die Anlagenmaske (W7.4), die beiden Hosts vom Startbild (W7.5,
W7.7) und zuletzt W7.8. Zwei Blätter — der Katalogfilter und der Kennlinien-Editor —
konnten erst mit ihrem letzten WinForms-Aufrufer gelöscht werden; ein früheres Löschen
hätte den Bau gebrochen.

---

## 2. Bauweise

### 2.1 Ein neues Bild im Kern: `ChartRenderer.Kennlinien`

Die vier `Chart`-Steuerelemente von `Form_WP` (`InitChart`:243-331) und
`Wizard_WPItem` (`listBox_WP_SelectedIndexChanged`:333-383) zeigten dasselbe
Bildpaar — COP und Leistung über der Außentemperatur, eine Linie je
Vorlauftemperatur, in zwei Reiterblättern. Sie sind **eine** Methode geworden:

```
ChartRenderer.Kennlinien(titel, yTitel, xTitel, reihen, marke) → byte[] PNG
ChartRenderer.KennlinienReihe(int Vorlauf, IReadOnlyList<(double Temperatur, double Wert)> Punkte)
ChartRenderer.Kennlinienmarke { Kreis, Kreuz }
```

Drei Entscheidungen mit Grund:

* **Bildmaß 968 × 520.** Der breiteste der vier Vorläufer-Charts maß 484 × 195;
  doppelte Zielauflösung wie bei allen Bildern dieser Datei ergibt 968 × 390, dazu
  130 px für die Legende. Sie steht hier **unter** dem Diagramm statt darin — bei acht
  Reihen verdeckte sie im WinForms-Chart die Linien.
* **Die x-Achse trägt echte Temperaturen**, keine Stützstellennummern: Zwei
  Vorlauf-Kennlinien müssen nicht dieselben Außentemperaturen haben, und bei
  ungleichen Reihen läge sonst −15 °C der einen über −7 °C der anderen. Die „schöne"
  Stufung ist dafür aus `KapitalwertVerlauf` als `Stufe(ref min, ref max)`
  herausgezogen — beide Achsen brauchen sie.
* **Punktmarken wie im Vorläufer**: Kreis für den COP (`MarkerStyle.Circle`), Kreuz
  für die Leistung (`MarkerStyle.Cross`), Radius 5 px bei doppelter Auflösung =
  dieselbe optische Größe wie `MarkerSize = 5`.

Die Datenseite liefert `KenndatenCtrl.Reihen` (Wärme) bzw.
`KenndatenKuehlungCtrl.Reihen` (Kühlung) als **einen** `KennlinienSatz` mit beiden
Reihenlisten — der Vorläufer teilte dafür dieselbe `DataTable` mit
`DataTable.Select("Vorlauf=…")` auf. Die Kühlung nimmt wie bisher nur die höchste
Laststufe (`MAX(Last)`) und fällt ohne Laststufe auf alle Zeilen zurück.

### 2.2 Der Filter liegt im Kern, nicht in der Komponente

`Form_WpFilterAuswahl` filtert **im Speicher**: `LoadData` liest den ganzen Katalog
einmal, jeder Klapplistenwechsel und jeder Tastendruck lässt ein LINQ-`Where` darüber
laufen. Das bleibt so — bei einigen hundert Stammsätzen ist es schneller als neun
Abfragen. Die **Logik** zieht trotzdem in den Kern (Regel F4): Dort ist sie ohne
Datenbank prüfbar und auf iOS unverändert verwendbar.

| Was | Wo | Herkunft |
|---|---|---|
| `WaermepumpenKatalogZeile` | `EPOS.Kern/Model/` | `WPData`, das AM ENDE der Formulardatei stand |
| `WaermepumpenKatalogFilter` | `EPOS.Kern/Allgemein/` | `ApplyFilter`:66-130 und `FillCombo`:211 |
| `WPStammCtrl.KatalogZeilen` | `EPOS.Kern/Controller/` | `WPDataCtrl.ReadAll`:281-323 |

### 2.3 Die Datenseite im Kern

| Was | Wo | Herkunft |
|---|---|---|
| `WPCtrl` (ganze Klasse) | `EPOS.Kern/Controller/` | `WindowsFormsApplication1/Controller/` — der `partial`-Teil `WPCtrl.WinForms.cs` ist ersatzlos entfallen |
| `KenndatenCtrl.Reihen`, `.LiesStamm`, `.Abgleichen` | dito | `InitChart`:243, `btn_Kenndaten_Click`:479 und :495-553 |
| `KenndatenKuehlungCtrl.Reihen`, `.HatKenndaten` | dito | `InitChart`:256-271, `HatKuehlKenndaten`:177 |
| `WPStammCtrl.GesperrtDurchProjekt`, `.Speichern` | dito | `btn_Loeschen_Click`:442, `btn_Speichern_Click`:372 |
| `WaermepumpeGeraeteCtrl` (neu) | dito | `Form_WPAuswahl.GeraetedatenFuellen`:95 (Ä22) |
| `WErzeugerCtrl.AnlagenzeileNachziehen` | dito | `Wizard_WPItem`:466-510 (Ä25) |
| `KostenSummenCtrl.AnlagenSumme` | dito | `Wizard_WPItem`:551-564 (Ä20) |
| `Z_ProjektSolarganglinieCtrl.LiesProjekt` | dito | `Form_Start.pBox_Solarthermie_Click`:1416-1431 |
| `SolarkollektorenStammCtrl.IdZu`, `.ReadById` | dito | `Form_SolarKollektoren.btn_Hinzzu_Click`:199, :214 |

**`WPCtrl` konnte bis hierher nicht umziehen.** Ein `partial` über zwei Dateien, davon
eine mit WinForms, geht nicht über die Assemblygrenze (Lehre `WizardSeite`). Die
zweite Hälfte trug genau eine Methode — `FillListBox(ListBox)` —, und die hatte im
gesamten Bestand **keinen Aufrufer**. Sie ist ersatzlos entfallen.

### 2.4 Vier Ebenen Überlagerung

Die tiefste Schachtelung des ganzen Pakets iU9 steht in dieser Welle, und sie läuft in
EINEM Fenster (Risiko R2):

```
WaermepumpenDialog (Verwaltung)
  └─ WaermepumpeAnlageDialog (Detailansicht)
       ├─ WaermepumpeStammDialog („Parameter Bearbeiten…")
       │    ├─ KennlinienEditorDialog („Kennliniendaten…")
       │    └─ WaermepumpenKatalogDialog („Modul-Katalog…")
       └─ WaermepumpenKatalogDialog („Modul-Katalog…")
```

Dazu auf der Solarseite `SolarkollektorenDialog` → `SolarkollektorKatalogDialog` →
`NamensDialog`. Esc schließt immer nur die oberste Ebene; jeder Wirt prüft dafür seine
eigenen Überlagerungsschalter, bevor er Esc für sich auswertet.

**Ein zweites Fenster bleibt an genau einer Stelle**: „Kosten bearbeiten…" der
Detailansicht führt in `KostenKomponenteHuelle`, die selbst eine Blazor-Hülle ist
(A‑1 aus Welle 6, unverändert).

### 2.5 Zwei Komponenten ohne Hülle

`WaermepumpenKatalogDialog` und `KennlinienEditorDialog` haben **keine** Windows-Hülle:
Sie erscheinen ausschließlich als Überlagerung in einem Wirt. Ihre Texte nehmen sie
deshalb unmittelbar aus `Resource.*` statt über einen Parametersatz — ein Wirt, der 26
Texte durchreicht, wäre ein zweiter Ort für dieselbe Zuordnung. Dasselbe Muster wie
`SpeichernLeiste.SpeichernText` seit Welle 1.

### 2.6 Eine neue Fähigkeit: `NurLesen` in der Detailansicht

`WaermepumpeAnlageDialog` führt seit W7.5 `NurLesen`: Alle Bedienelemente sind
gesperrt, die Schlussleiste wird ein einzelner Knopf „Schließen". Der Weg „Ansicht"
der Verwaltung braucht das — der Vorläufer öffnete dort denselben Dialog voll
bedienbar und **warf sein Ergebnis weg** (siehe A‑22).

---

## 3. Feldkarten-Abgleich

Die acht Karten wurden am 03.09.2026 neu gezogen (Stand nach W6) und liegen unter
`scratchpad/iU9/karten_w7/`. Abgeglichen ist je Maske der **Feldbestand nach Zahl und
Beschriftung** (bunit-Test „Der Feldbestand …").

| Maske | Karte | Komponente | Anmerkung |
|---|---|---|---|
| WpFilterAuswahl | 29 Zeilen, 7 ComboBox, 4 NumericUpDown, 1 TextBox | 7 Klapplisten, 5 Eingabefelder, 4 Knöpfe, 8 Spalten | die 13 Laufzeit-Panels trägt der Designer bereits |
| Kenndaten | 12 Zeilen, 4 TextBox, 4 Button | Vorlaufliste, Zeilenraster, 3 Felder, 4 Knöpfe | +1 Löschknopf je Zeile (A‑9) |
| WP | 28 Zeilen, 7 TextBox, 4 ComboBox, 2 Chart, 2 RadioButton | 6 Felder, 4 Klapplisten, 2 Reiterblätter, 6 Knöpfe | −1 = die verborgene Modulkostenzeile (Ä19) |
| Wizard_WPItem | 47 Zeilen, 3 GroupBox, 2 Reiter, 15 TextBox | 3 Gruppen, 2 Reiterblätter, Kostenzeile | −7 = die Pufferspeichergruppe, nicht gezeichnet (Ä19) |
| WPAuswahl | 8 Zeilen, ListView mit 5 Spalten | 7 Spalten, 5 Knöpfe | +2 Spalten = Zeilenwahl und Aktion |
| SolarDB | 27 Zeilen, 14 TextBox, 4 Button | 4 Textfelder, 10 Zahlen, 4 Knöpfe | **Karte falsch** — siehe A‑12 |
| SolarKollektoren | 26 Zeilen + Gruppe „Kollektor" | 2 Listen, 2 Pfeile, 2 Gruppen, 7 Knöpfe | die `PictureBox` des Bildblitzes entfällt (A‑24) |
| Solarganglinie | 9 Zeilen, 2 ListBox, 2 TextBox | 2 Listen, 2 Pfeile, 2 Anzeigefelder, 3 Knöpfe | — |

**Laufzeitfelder von Hand ergänzt** (die Karte liest nur `InitializeComponent`): die
Kostenzeile `btnKosten`/`lblKostenSummen` von `Wizard_WPItem`
(`KostenAnzeigeEinrichten`:385).

---

## 4. Abweichungen (mit Begründung)

| # | Was | Warum |
|---|---|---|
| **A‑1** | Kein KI-Aufrufknopf (`KiAufrufKnopf.Anbringen` in `Form_WP`), kein `FensterEinpassung`, kein `FusszeilenNorm`, kein `MakeSmoothButton`, kein `SetPlaceholder`, keine Zebra-Farben, kein `Paint`-Rahmen | Wie A‑2 aus Welle 6: Der KI-Einstieg hat in `EPOS.UI` noch keinen Baustein (W15b). Die übrigen sind WinForms-Layoutkorrekturen; das erledigen die Hülle (`AnBildschirmGeklemmt`) und das CSS |
| **A‑2** | „Alle" ist ein **NULL-Steuerwert**, kein deutsches Literal | Der Vorläufer verglich `cbHersteller.Text == "Alle"`. Das ging, solange die Maske einsprachig war; in der zweisprachigen Fassung ist „Alle" ein Anzeigetext und der Steuerwert `null` — dieselbe Trennung wie bei `Sprungziel` und `DbWerte` |
| **A‑3** | `WaermepumpenKatalogZeile.Auslegung` bleibt deutsch („Heizen" / „Heizen/Kühlen") | Der Wert entsteht aus DATEN (`Kuehlleistung > 0`) und wird mit ihnen verglichen — dieselbe Lage wie `DbWerte.WP_BETRIEBSART_*`. Ein übersetzter Wert träfe seinen eigenen Filter nicht mehr |
| **A‑4** | Die y-Achse der Kennlinien schließt die Null ein | COP und Leistung sind positiv; ein Diagramm, das erst bei 2,8 beginnt, macht aus einem Unterschied von 10 % optisch einen von 80 %. Das WinForms-Chart skalierte selbsttätig |
| **A‑5** | Die Kennlinien werden in EINER Transaktion zurückgeschrieben | Der Vorläufer las die drei Fälle aus `DataRow.RowState` und schrieb sie in einer Schleife OHNE Transaktion — ein Fehler in der Mitte hinterließ einen HALBEN Stand. Und `Tab_Kenndaten_STAMM` ist Simulationseingang |
| **A‑6** | Eine neu angelegte Wärmepumpe erbt NICHT Bauart, Kühlleistung und `maxPtherm` der zuvor markierten | `btn_Speichern_Click` las vor dem Schreiben `ReadSingle("… Bezeichner='" + item.WPName + "'")`. Im Neu-Fall zeigt `item` noch auf den ALTEN Satz (`btn_Neu_Click` setzt ihn nicht zurück) — der neue Satz übernahm dessen drei nicht bearbeitete Felder |
| **A‑7** | Die Trefferzahl steht in einer `Herleitungszeile` statt in der Fensterüberschrift | Der Vorläufer schrieb „WP-Filter Auswahl (n Wärmepumpen gefunden)" in den Fenstertitel. Eine Überlagerung hat keine Fensterleiste, und über dem Raster ist die Zahl beim Filtern im Blick |
| **A‑8** | Der Doppelklick auf eine Katalogzeile wird ein Knopf (`Zeilenwahl`) | Ein Doppelklick ist kein Berührungsziel (M2/iL4) — dieselbe Umstellung wie W5 A‑3 |
| **A‑9** | Jede Kennlinien-Stützstelle bekommt einen Löschknopf | Der Vorläufer nahm die Entf-Taste des `DataGridView`; ein Tastendruck ohne sichtbares Bedienelement ist auf einem Berührungsgerät nicht erreichbar |
| **A‑10** | Die vier `MessageBox` des Kennlinien-Editors werden EIN Warnbanner mit Feldnamen | Folgepaket zu `ab5bf32`: Ablehnungen bleiben stehen und lassen den Dialog offen |
| **A‑11** | Im Modus „Neu" des Kollektor-Katalogeditors bleiben die Zahlenfelder LEER statt auf 0 | So fordert die Pflichtprüfung sie ein, statt still acht Nullen zu speichern |
| **A‑12** | Die Beschriftungen von `Form_SolarDB` kommen aus dem **Designer**, nicht aus der Feldkarte | Die Karte ordnet „k2 :" dem Feld `textBox_Kosten` zu und lässt `textBox_k2` ohne Beschriftung. Die Koordinaten sagen: `Label15` „k2:" (80,327) LINKS von `textBox_k2` (111,327), `Label25` „Investitionskosten:" (356,297) ÜBER `textBox_Kosten` (359,319). Ein Test hält beides fest (Klasse R‑W6‑7) |
| **A‑13** | 43 `MessageBox` werden `Warnbanner`, `Rueckfrage` oder Meldungstext eines Ergebnis-Records | Wie A‑20 aus Welle 6: Bestätigungen schließen den Dialog wie bisher, Ablehnungen bleiben als Banner stehen |
| **A‑14** | Die Modulkostenzeile des Stammdialogs ist nicht gezeichnet, ihre Pflichtprüfung entfällt | Ä19: Gerätekosten laufen über die Kostenverwaltung. Der Vorläufer prüfte `leerErlaubt: false` auf ein Feld, das er selbst zuvor aus dem Formular ENTFERNT hatte; der Wert läuft im Datensatz unverändert mit |
| **A‑15** | Die Baujahrliste ist lückenlos 2025…2016 | Befund W7‑O‑2: Der Vorläufer trug „2024" ZWEIMAL und „2022" nie. In der frei beschreibbaren `ComboBox` ließ sich 2022 noch tippen; in einer geschlossenen Klappliste wäre es unerreichbar |
| **A‑16** | Ein Bestandswert außerhalb einer festen Liste wird ihr VORANGESTELLT | Der Vorläufer hatte frei beschreibbare `ComboBox`en und zeigte auch einen Wert, den die Liste nicht führt. Ein `select` würde ihn still verwerfen. Betrifft Typ, Leistungsstufen, Aufstellung, Baujahr und den Vorlauf der Anlage |
| **A‑17** | Der Stammdialog aus der Detailansicht zeigt die GANZE Liste | `Form_WP(wpname)` war ein zweiter Modus derselben Maske: Liste auf einen Satz gefiltert, Name/Neu/Löschen gesperrt. Die Liste ist die eine Wahrheit; in W7.4 wird daraus ohnehin eine Überlagerung |
| **A‑18** | Der Rücklauf ist ein Zahlenfeld; die Vorschlagswerte stehen als Herleitungszeile darunter | Der Etappe‑4-Kommentar nennt `RUECKLAUF_VORSCHLAEGE` ausdrücklich eine „reine VORSCHLAGSLISTE ohne Grenzwirkung" — eine geschlossene Liste machte 26 oder 27 °C unmöglich |
| **A‑19** | Der Grund der gesperrten Kostenzeile steht ZUSÄTZLICH als Herleitungszeile | Der Tooltip des Vorläufers ist auf einem Berührungsgerät nicht erreichbar |
| **A‑20** | „Löschen" in der Verwaltung trifft die ZEILE, nicht ihren Anzeigeindex | `list_werzmodel.RemoveAt(listView_WP.SelectedIndex)`: Im Assistenten führt dieselbe Liste ALLE Erzeugertypen, die Anzeige aber nur die Wärmepumpen — der Index traf dort eine fremde Anlage (dieselbe Fehlerklasse wie W6 A‑5) |
| **A‑21** | Der Rückschreibblock von `WPKontextMenuCtrl.ContextMenuItemBearbeiten` ist ersatzlos entfallen | Er kopierte 21 Felder von `frm_wpitem.item` nach `list_alle[index]` — und das war DASSELBE Objekt. Zwanzig Zuweisungen waren wirkungslos, die einundzwanzigste nicht (Befund W7‑O‑4) |
| **A‑22** | „Ansicht" nimmt die Zeile, die dasteht, und zeigt sie NUR LESEND | Der Vorläufer las die Anlage per Bezeichner aus der Datenbank (projektübergreifend mehrdeutig, ungespeicherte Zeilen unauffindbar) und öffnete denselben Dialog voll bedienbar, wertete `CloseWithOK` aber nicht aus — ein Formular, dessen Eingaben still verfallen |
| **A‑23** | Der Doppelklick der Verwaltung wird ein Knopf in der Zeile | wie A‑8 |
| **A‑24** | Der 500‑ms-Bildblitz von „Übernehmen" wird ein Warnbanner | Der Vorläufer hielt dafür den Oberflächenfaden mit `Thread.Sleep(500)` an — in der WebView wäre das ein eingefrorener Dialog |
| **A‑25** | „▶" bei den Solarkollektoren trifft die GEWÄHLTE Zeile | Die Regel „Projektkopie nur ohne zweite Referenz entfernen" bleibt unverändert in der Hülle |
| **A‑26** | Der gestrichelte `Paint`-Rahmen um die Kollektorgruppe wird die Umrandung des `Gruppenkopf` | `Form_SolarKollektoren_Paint`:413 zeichnete vier Linien von Hand; das kann CSS |
| **A‑27** | Das Beschreibungsfeld der Solarganglinien wird GEFÜLLT | Der Vorläufer hatte es, setzte aber nur den Namen — es blieb in jedem Zustand leer, obwohl `SolarganglinieModel.m_szBeschreibung` sie führt |
| **A‑28** | „▶" bei den Solarganglinien entfernt die GEWÄHLTE Zeile | Der Vorläufer nahm die erste Zeile gleichen Namens (`btn_Entfernen_Click`:89) |
| **A‑29** | `DateiListe` führt MODELLE statt Controller | Der Vorläufer legte in einer `List<Z_ProjektSolarganglinieModel>` `Z_ProjektSolarganglinieCtrl`-Objekte ab — Datenbankzugriffsobjekte in einer Datenliste; der Controller erbt das Modell, also ging es |
| **A‑30** | Die Positionierung an der Knopfposition (`PointToScreen`) entfällt bei allen vier Aufrufwegen | Eine Blazor-Hülle kennt kein `PointToScreen` und erscheint mittig über dem Besitzer — dieselbe Umstellung wie iU9‑W2.1 |

---

## 5. Texte

**157 Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` und —
von Hand, weil hier kein Visual Studio läuft — `Resource.Designer.cs`. Alle drei
Dateien geprüft: kein Schlüssel fehlt in einer von ihnen (3 472 de = 3 472 en).

| Präfix | Zahl | Wofür |
|---|---|---|
| `WPK_*` | 26 | Katalogfilter |
| `WPKL_*` | 17 | Kennlinien-Editor |
| `WPS_*` | 33 | Stammdialog |
| `WPA_*` | 29 | Anlage |
| `WPV_*` | 11 | Verwaltung |
| `SKK_*` | 19 | Kollektor-Katalog |
| `SKV_*` | 18 | Solarkollektoren |
| `SGL_*` | 4 | Solarganglinie |

**Sieben der acht Masken waren lokalisiert** (`.en-US.resx`: Form_WP 21,
Form_WPAuswahl 10, Kenndaten 12, Wizard_WPItem 39, Form_SolarDB 12,
Form_SolarKollektoren 22, Form_Solarganglinie 6 = **122**). Alle Texte sind
**wörtlich** übernommen; `Form_WpFilterAuswahl` trug deutsche Literale und ist neu
übersetzt. Die Zahl der lokalisierten Masken sinkt dadurch von 54 auf **47**.

**Wiederverwendet statt neu angelegt:** `WP_BTN_BEENDEN`, `WPI_BTN_KOSTEN`,
`WPI_TIP_KOSTEN`, `WPI_TIP_KOSTEN_NEU`, `WPI_KOSTEN_KEINE`, `WPI_KOSTEN_SUMMEN`,
`HZK_TIP_HINZU`, `HZK_TIP_ENTFERNEN`, `HZK_GRP_MODUL`, `HZK_LBL_NAME`,
`HZK_BTN_BEARBEITEN`, `KFAK_SP_WAHL`, `BHKWV_SP_NAME`, `BHKWV_SP_EIGENSCHAFTEN`,
`ADM_BTN_SPEICHERN`, `ALLG_BTN_OK/_ABBRECHEN/_JA/_NEIN`.

**Nicht übersetzt sind die Steuerwerte:** die drei Betriebsarten
(`DbWerte.WP_BETRIEBSART_*`), die vier Wärmepumpentypen, die drei Leistungsstufen und
die vier Aufstellungsarten — sie stehen so in `Tab_WP_STAMM` und werden mit dem
Datenbankinhalt verglichen. Ebenso `WaermepumpenKatalogZeile.Auslegung` (A‑3).

**`help_mapping.txt` bleibt unverändert.** Die acht Zeilen `Form_X.btn_Help` gelten
weiter — der Schlüssel benennt die Wikiseite, nicht die Klasse; jede Komponente trägt
ihren alten Schlüssel als `HilfeSchluessel`.

**`Allgemein/KI/HilfeKontext.cs`:** die acht Einträge der gelöschten Masken entfernt,
jeweils im Commit ihrer Maske (Regel F10).

---

## 6. WinForms-Seite

**Gelöscht** (34 Dateien):

```
Views/Wärmepumpe/Form_WPFilterAuswahl.{cs,Designer.cs,resx}
Views/Wärmepumpe/Kenndaten.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}
Views/Wärmepumpe/Form_WP.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}
Views/Wärmepumpe/Form_WPAuswahl.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Solarthermie/Form_SolarDB.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Solarthermie/Form_SolarKollektoren.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Solarthermie/Form_Solarganglinie.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Controller/WPCtrl.WinForms.cs
```

**Verschoben** (5 Dateien): `Views/Wizard/Wizard_WPItem.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}`
→ `Werkzeuge/Formularkarte.Tests/Pruefmuster/Wizard/`. Die Maske ist aus der Anwendung
verschwunden, bleibt aber als eingefrorener Analysegegenstand: Drei Abschnitt-Tests
brauchen einen Behälterbaum aus `GroupBox`, `TabControl` und `TabPage`, und den gibt es
im Bestand so nicht mehr.

**Neu auf der Windows-Seite** (5): `Views/Wärmepumpe/WaermepumpeStammHuelle.cs`,
`Views/Wärmepumpe/WaermepumpeAnlageHuelle.cs`,
`Views/Wärmepumpe/WaermepumpenHuelle.cs`,
`Views/Solarthermie/SolarkollektorHuelle.cs`,
`Views/Solarthermie/SolarganglinieHuelle.cs`.

**Aufrufer umgestellt:** `Form_Start` (`pBox_WP_Click`, beide Zweige von
`pBox_Solarthermie_Click`), `Form_Simulation_Detail` (nur die eine Stelle :5048,
Wachstumsstopp iR10), `FormMain` (der iF29-Altzweig), `WinFormsNavigation`
(`Masken.WpAdministration`), `WPKontextMenuCtrl` (drei Wege), `SolarKontextMenuCtrl`
(ein Weg), `Form_SolarKollektorenAdmin` (zwei Wege), `AssistentSeiten` (zwei Zeilen
und die `_typen`-Liste), `WizardParent` (zwei Zweige weg, keiner dazu — beide laufen
über `IAssistentErzeugerSeite`).

**Zwei unbenutzte `new Form_X()` gestrichen**: `Form_Gebaeude` im WP-Löschweg und
`Form_SolarKollektoren` im Solar-Löschweg (dieselbe Aufräumung wie bei den fünf
Kontextmenüs der Welle 6).

**Keine Typverwendung ist übrig:**

```
grep -rnE "(new|typeof|:)\s*(Form_WpFilterAuswahl|Kenndaten|Form_WP|Wizard_WPItem|
    Form_WPAuswahl|Form_SolarDB|Form_SolarKollektoren|Form_Solarganglinie|
    WPDataCtrl|WPData)\b" --include=*.cs .
→ 0 Treffer im Code
```

Restfundstellen der alten Namen sind ausschließlich (a) `HilfeSchluessel`- und
Prüfmuster-Zeichenketten, (b) Kommentare, die die Herkunft nennen, (c) der
KI-Dialogkatalog `KiDialoge` (Maskennamen, keine Typen) und (d) die Nachbarmasken
`Form_WP_einlesen`, `Form_SolarKollektorenAdmin`, `Form_SolarKollektoren_einlesen`
und `Form_Solarganglinie_Admin`, die alle bleiben.

---

## 7. Nachweise

### 7.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 -t:Rebuild
→ 0 Fehler, 20 Warnungen
```

Gleichauf mit der Basis nach Welle 6 (20). Aufteilung unverändert: 14 WFO1000,
2 CS0108, 2 CS0109, 1 WFO0003, 1 CA2255 — keine der acht gelöschten Masken trug eine
WFO1000-Fundstelle, und keine neue ist dazugekommen.

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ KiKern.Tests        450 gruen
  SpeicherEngine.Tests 337 gruen
  EPOS.UI.Tests        940 gruen   (+155 aus Welle 7)
  EPOS.Kern.Tests       93 gruen   (+ 29 aus Welle 7)
  zusammen           1 820 gruen, 0 rot
```

**155 neue bunit-Fälle** je Komponente: `WaermepumpenKatalogDialog` 18,
`KennlinienEditorDialog` 18, `SolarkollektorKatalogDialog` 16,
`WaermepumpeStammDialog` 25, `WaermepumpeAnlageDialog` 27, `WaermepumpenDialog` 18,
`SolarkollektorenDialog` 20, `SolarganglinieDialog` 12, `SprungzielTests` +1. Jeder
Satz prüft Feldbestand (Zahl UND Beschriftungen), die englischen Texte, Vorbelegung,
Prüfregeln, Rückrufe und Tastatur; die Kultur ist auf de‑DE gepinnt.

**29 neue Kern-Fälle**: `WaermepumpenKatalogFilterTests` 14 (je Kriterium, beide
Bereichsfilter, Wildcard- und Teilsuche, Reset-Vorbelegung),
`KatalogZeilenTests` 3 (gegen die Testdatenbank), `KenndatenAbgleichTests` 9
(löschen / anlegen / ändern / alles drei in einem Durchgang, gegen eine
Arbeitskopie), `ChartRendererTests` +3 (Maß, Determinismus, Kreis ≠ Kreuz).

**Ein Test deckte einen echten Fehler auf**: Ohne
`@using EPOS.UI.Dialoge.Allgemein` übersetzte der Razor-Compiler `<NamensDialog>`
in `SolarkollektorenDialog` als LITERALES HTML-Element — der Bau blieb grün, die
Namensabfrage wäre am Gerät leer geblieben.

### 7.3 Formularkarte

```
dotnet test Werkzeuge/Formularkarte/Formularkarte.sln -c Release
→ 123 gruen
```

Fünf Anker hingen an gelöschten Masken:

| Test | vorher | jetzt | hält bis |
|---|---|---|---|
| `WpItem_HatDreiGruppenUndZweiReiter` | `Views/Wizard/Wizard_WPItem` | Prüfmuster `Pruefmuster/Wizard/` | eingefroren |
| `WpItem_ErkenntGanzzahlfelderAusDerFormCs` | dito | dito | eingefroren |
| `WpItem_GruppenkoepfeSindDasZielDerBehaelter` | dito | dito | eingefroren |
| `DieSprungtabelleLoestDieMaskenschluesselAuf` | `Form_WP` (`Masken.WpAdministration`) | `Form_AdminStromspeicher` (`Masken.StromspeicherAdmin`) | W14a |
| `UmlauteImOrdnernamenWerdenUmschrieben` | `Form_WPFilterAuswahl` | `Form_WP_einlesen` (derselbe Ordner) | Importmasken |

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1 --erreichbarkeit
→ Masken 73 (81 nach W6), lokalisiert 47 (54), erreichbar 71,
  unerreichbar 0, verwaist 0
```

**73 = 81 − 8.**

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 272 SQL-Texte geprueft: 0 Fundstellen, 173 dynamisch, 1 099 in Ordnung
python3 … --selbsttest
→ 32 Anweisungen, 0 Abweichungen
```

Keine Nachbesserung am Prüfer nötig. Die Zahl der geprüften Texte sinkt von 1 291 auf
1 272, weil die acht Masken ihre SQL entweder verloren (Kern-Controller schreiben sie
jetzt) oder mitgenommen haben.

### 7.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 12 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

**Zwei Bilder mehr als nach Welle 6**: `kennlinien_cop` und `kennlinien_leistung`,
je 968 × 520, aus drei synthetischen Reihen (35/45/55 °C über −15…+20 °C) — beide
deterministisch und mit den drei erwarteten `C_SERIEN`-Farben.

### 7.7 Referenzlauf

**Pflicht in dieser Welle**, weil sieben Kern-Controller angefasst werden
(`WPCtrl` als Umzug, `WPStammCtrl`, `KenndatenCtrl`, `KenndatenKuehlungCtrl`,
`WErzeugerCtrl`, `KostenSummenCtrl`, `Z_ProjektSolarganglinieCtrl`,
`SolarkollektorenStammCtrl`) und der Renderer eine neue Methode bekommt.

```
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w7
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/w7
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

**Byte-gleich, nicht nur innerhalb der Toleranz.** Das ist der Nachweis, dass der
Umzug von `WPCtrl` und die Verlagerung der übrigen SQL-Anweisungen zeichengleich
waren.

### 7.8 Veröffentlichung

```
dotnet publish WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Release -p:Platform=x64
→ wwwroot/index.html, wwwroot/_framework/blazor.webview.js,
  wwwroot/_content/EPOS.UI/epos-ui.css — vollstaendig
```

---

## 8. Grenzen

* **Am Gerät ungeprüft.** Alles hier ist ohne Windows entstanden. Die Abnahmeliste in
  § 9 ist der Prüfplan.
* **Vier Ebenen Überlagerung** (R‑W7‑3) sind das tiefste, was iU9 bisher baut. Die
  Fokusfalle je Ebene ist konstruktiv da (`Ueberlagerung` mit zwei Fokusfallen), am
  Gerät gemessen ist sie nicht — das ist Abnahmepunkt 4.
* **Das Kennlinienbild ist neu gezeichnet, nicht nachgebaut.** Der Modus
  `bildvergleich` der Referenzlauf-Suite ist mit iF23 gelöscht; ein Vergleich mit dem
  GDI-Bild ist nur noch von Hand am Gerät möglich (Abnahmepunkt 2).
* **Sechs WebViews im Assistenten** (R‑W6‑1 wächst): PV, Speicher, Kessel, BHKW und
  jetzt Wärmepumpe und Solar. Die verzögerte Bauweise von `BlazorAssistentSeite` ist
  die Gegenmaßnahme, gemessen ist sie nicht.
* **Die Kostenleiste öffnet weiter ein zweites Blazor-Fenster** (A‑1 aus Welle 6,
  W6‑O‑5). Die Verschmelzung nach W16.
* **Fünf Bestandsbefunde bleiben stehen** (§ 10) — sie sind Fachentscheidungen, keine
  Portfragen.

---

## 9. Abnahmeliste Windows (iZ5) für diese acht Masken

Je Dialog: öffnen mittig, kein weißes Aufblitzen, ziehbar und maximierbar, Tabellen
ohne Umbruch, de **und** en (`HKCU\Software\wp-plan\Language`), Hochkontrast, 125 %
und 150 % scharf, Maus **und** Finger (44 px), Tab-Zyklus bleibt im Dialog, Esc
schließt, Enter schließt NICHT, Infoknopf zeigt die Wikiseite.

| # | Aufrufweg | Was besonders zu prüfen ist |
|---|---|---|
| 1 | Startbild → Kachel **Wärmepumpe** | Zeilen nur `WP_TYP`; „Neu.." zeigt erst den Katalog, dann die Detailansicht, und erst deren OK hängt die Zeile an; „Ändern.." ersetzt die Zeile; „Löschen" trifft die markierte, nicht den Index; „Ansicht" ist NUR LESEND und hat einen einzigen Knopf |
| 2 | In 1 → **Detailansicht** | Zwei Bilder mit Kreis- und Kreuzmarken, eine Reihe je Vorlauf, Legende „35°C"; Bivalent an → Betriebsart; Alternativ/Teilparallel → Bivalenztemperatur; Vorlauf ≤ Rücklauf meldet aus `TemperaturenPruefen`; leere Pflichtzahl nennt ihr Feld; die Pufferspeichergruppe ist NICHT da |
| 3 | In 2 → **„Kosten bearbeiten…"** | Gesperrt bei ungespeicherter Anlage (Grund als Zeile lesbar), sonst „Invest … · Betrieb …"; öffnet ein ZWEITES Fenster und kehrt sauber zurück (A‑1, R‑W6‑3) |
| 4 | In 2 → **„Parameter Bearbeiten…" → „Kennliniendaten…"** | **Vier Ebenen** in einem Fenster: Verwaltung → Anlage → Stammdialog → Editor. Esc schließt nur die oberste; der Tabulator bleibt je Ebene gefangen; nach dem Editor sind die Bilder neu gezeichnet |
| 5 | Menü → **Wärmepumpen-Datenbank** (`Masken.WpAdministration`) | Liste zeigt Auslieferungssätze gedimmt; Wahl füllt Felder und Bilder; Umschalter Wärme/Kühlung nur mit Kühl-Kenndaten und fällt bei jedem Zeilenwechsel auf Wärme zurück; Speichern/Löschen auf einem ReadOnly-Satz nennt den Grund; Löschen mit Projektzuordnung nennt das Projekt |
| 6 | In 5 → **„Modul-Katalog…"** | Sieben Filter greifen einzeln und zusammen; `CS*7*` und `CS-0?0`; Klartext ist Teilsuche; Reset stellt VLT Max und Leist. Max auf die Höchstwerte der Daten zurück; die Trefferzahl steht über dem Raster |
| 7 | Übersichtsliste → **Kontextmenü** (Anzeigen / Bearbeiten / Neu) | Dreimal dieselbe Detailansicht; „Bearbeiten" LÖSCHT die Leistungsstufen nicht mehr (W7‑O‑4); auch auf der REF-Liste |
| 8 | **Simulation-Detail** → Doppelklick WP-Liste | Dieselbe Verwaltung wie 1; die Datei ist sonst unberührt |
| 9 | **Assistent** → Seiten 7 und 8 | Seitenwechsel unter 1 s, kein Aufblitzen, Rückkehr zeigt den aktuellen Listenstand, keine OK/Abbrechen-Leiste; „Löschen" trifft im Assistenten die richtige Zeile (A‑20); Speicher der Browserprozesse im Auge behalten (R‑W6‑1) |
| 10 | Startbild → Kachel **Solarthermie**, Zweig Kollektorprofil | ◀ ohne Katalogwahl gesperrt; ◀ legt eine Zeile mit Anzahl 1 an; ▶ entfernt die Projektkopie erst ohne zweite Referenz; Katalogzeile → Detail OHNE Kollektorgruppe, Projektzeile MIT; Anzahl ändern → Aperturfläche live; „Übernehmen" meldet statt zu blinken |
| 11 | In 10 → **„Kollektor in DB ändern/neu/löschen"** | Der Editor steht IM Fenster; „neu" fragt erst den Namen; „löschen" fragt nach; nach jedem Editorlauf ist die Katalogliste neu gezogen |
| 12 | Startbild → Kachel **Solarthermie**, Zweig Ganglinie | ◀/▶ auf zwei Zuordnungen DERSELBEN Ganglinie treffen die markierte; Name UND Beschreibung stehen da (A‑27); „Bearbeiten…" öffnet die WinForms-Verwaltung DARÜBER und die Liste ist danach neu |
| 13 | Menü **Kataloge → Solarkollektoren → ändern / neu** | Derselbe Katalogeditor als eigenes Fenster (aus `Form_SolarKollektorenAdmin`, die WinForms bleibt); „k2" und „Investitionskosten" stehen an den richtigen Feldern (A‑12) |

---

## 10. Offene Punkte

| # | Was | Vorschlag |
|---|---|---|
| **W7‑O‑1** | Der Auffangzweig „ungültiges Suchmuster = kein Filter" im Katalogfilter ist **nicht erreichbar**. `Regex.Escape` maskiert jedes Sonderzeichen, und die beiden Ersetzungen danach machen aus `\*` und `\?` wieder gültige Bausteine — was ankommt, ist immer ein übersetzbarer Ausdruck. Eine offene Klammer wird deshalb WÖRTLICH gesucht und trifft nichts | Wörtlich übernommen (Regel F3), ein Kern-Test hält es fest. Der Zweig kostet nichts; wer ihn streichen will, entscheidet das als Fachfrage |
| **W7‑O‑2** | Die Baujahrliste des Stammdialogs trug „2024" ZWEIMAL und „2022" nie (`SetControls`:230-239) — eine Zahlendreherei in einer sonst lückenlosen absteigenden Reihe | Mit A‑15 behoben. **Entscheid des Anwenders**, falls die Lücke Absicht war |
| **W7‑O‑3** | `Form_WPAuswahl.btn_Ansicht_Click` war ein VERWAISTER Handler — der Designer kennt kein `btn_Ansicht`, weder als Steuerelement noch als Ereignisbindung | Mit W7.4 ersatzlos entfallen; der Ansichtsweg lief über den Doppelklick und ist jetzt der Knopf „Ansicht" |
| **W7‑O‑4** | `WPKontextMenuCtrl.ContextMenuItemBearbeiten` schrieb `list_alle[index].Regelung = frm_wpitem.item.Leistungsstufen`. `WPModel.Leistungsstufen` wird im ganzen Bestand NIE geschrieben und steht auf `""` — **jedes „Bearbeiten" aus diesem Kontextmenü löschte die Leistungsstufen der Anlage** | Mit A‑21 behoben (der ganze Block war ein Selbstkopierblock). Am Gerät auf einer Anlage mit gepflegter Regelung nachsehen |
| **W7‑O‑5** | `Form_SolarKollektoren.textBox_Vorlauf_Validating` und `…Ruecklauf_Validating` prüfen `ID_Type == BHKW_TYP` und trafen in dieser Maske NIE zu — geschrieben hat faktisch immer nur „Übernehmen" | Wörtlich übernommen: In der Razor-Fassung schreibt allein „Übernehmen". **Entscheid des Anwenders**, ob die beiden Felder auch beim Verlassen schreiben sollen |
| **W7‑O‑6** | Der KI-Aufrufknopf fehlt im Wärmepumpen-Stammdialog (A‑1) | Mit W15b, wenn `Gespraechsverlauf` steht — wie W6‑O‑6 |
| **W7‑O‑7** | Der Kennlinien-Renderer ist neu gezeichnet; ein Bildvergleich mit dem abgelösten WinForms-Chart ist nur von Hand möglich (`bildvergleich` ist mit iF23 gelöscht) | Abnahmepunkt 2. Die ChartProben sichern Maß, Farben und Determinismus |

---

## 11. Geänderte und neue Dateien

**Neu in `EPOS.UI`** (11): `Dialoge/Waermepumpe/WaermepumpenKatalogDialog.razor`,
`KennlinienZeile.cs`, `KennlinienEditorDialog.razor`, `WaermepumpeStammDaten.cs`,
`WaermepumpeStammDialog.razor`, `WaermepumpeAnlageDaten.cs`,
`WaermepumpeAnlageDialog.razor`, `WaermepumpenDialog.razor`;
`Dialoge/Solarthermie/SolarkollektorKatalogDaten.cs`,
`SolarkollektorKatalogDialog.razor`, `SolarkollektorenDialog.razor`,
`SolarganglinieDialog.razor`.

**Geändert in `EPOS.UI`**: `Dialoge/Allgemein/Sprungziel.cs` (ein Ziel).

**Neu im Kern** (3): `Model/WaermepumpenKatalogZeile.cs`, `Model/KennlinienSatz.cs`,
`Allgemein/WaermepumpenKatalogFilter.cs`, `Controller/WaermepumpeGeraeteCtrl.cs`;
dazu der Umzug `Controller/WPCtrl.cs`.
**Geändert im Kern** (7 Controller + Renderer): `WPStammCtrl`, `KenndatenCtrl`,
`KenndatenKuehlungCtrl`, `WErzeugerCtrl`, `KostenSummenCtrl`,
`Z_ProjektSolarganglinieCtrl`, `SolarkollektorenStammCtrl`,
`Allgemein/Bericht/ChartRenderer.cs`; dazu die drei Ressourcendateien.

**Neu in der Anwendung** (5): die fünf Hüllen.

**Neu in den Tests** (10): acht bunit-Klassen in `EPOS.UI.Tests/Dialoge/`,
`EPOS.Kern.Tests/WaermepumpenKatalogTests.cs`,
`EPOS.Kern.Tests/KenndatenAbgleichTests.cs`; dazu das Prüfmuster
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Wizard/`.

**Geändert in den Proben**: `Proben/ChartProben/Program.cs` (zwei Bilder).
