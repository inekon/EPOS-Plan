# iU9 Welle 2 — Namensdialog-Ausrollung, Sprungbrücke und die drei Wirtschaftlichkeitsmasken (Umsetzungsprotokoll)

> Muster: [`iU9_W1_Blazor_Port_Protokoll.md`](iU9_W1_Blazor_Port_Protokoll.md) und
> [`B5b_Blazor_Port_Protokoll.md`](B5b_Blazor_Port_Protokoll.md) — Feldkarten-Abgleich
> je Maske, Abweichungsliste A‑n, Entscheidungen, Windows-Abnahmepunkte.
>
> Basis `b0d3d86` (Branch `ios_migration`), Arbeitsstand 03.09.2026.
> Plan: Wellenplan iU9, Abschnitt C Zeile W2, E Priorität 2–3, F, G (R1/R3/R6/R7).

---

## 1. Auftrag und Ergebnis

**Sechs WinForms-Masken → vier Razor-Komponenten** (drei neue, eine erweiterte), jede
WinForms-Fassung im selben Schritt gelöscht (Regel M1). Dazu **ein neues Muster** — die
Sprungbrücke —, **drei neue Hüllen mit Datenseite**, **ein Windows-Ablauf** (die
Variantenanlage) und **zwei erweiterte Standardfelder**.

| # | Maske (Zeilen) | Komponente | Hülle | Aufrufer nach dem Umbau |
|---|---|---|---|---|
| W2.1 | `Form_StromspeicherItemNeu` (44, Klasse `Form_Sp_ItemNeu`) | `EPOS.UI/Dialoge/Allgemein/NamensDialog.razor` (erweitert) | `Allgemein/Blazor/NamensDialogHuelle.cs` | **28** Stellen in 24 Dateien |
| W2.1 | `Form_GebaeudetypNeu` (38) | dieselbe Komponente, zweites Feld | `NamensDialogHuelle.BezeichnerUndBeschreibung` | `Views/Gebäude/Form_EingGebTyp.cs:258` |
| W2.1 | `Form_AlsVariante` (K4, 196) | dieselbe Komponente, Hinweiszeile | `NamensDialogHuelle.FragenMitHinweis`; Ablauf in `Views/Varianten/AlsVarianteHuelle.cs` | `MDIMainForm.cs:568` |
| W2.2 | — (neues Muster) | `EPOS.UI/Dialoge/Allgemein/Sprungziel.cs` | `Allgemein/Blazor/Sprungbruecke.cs` | Ersteinsatz W2.5 |
| W2.3 | `Form_Tarifstruktur` (K4, 588) | `EPOS.UI/Dialoge/Wirtschaftlichkeit/TarifstrukturDialog.razor` | `Views/Wirtschaftlichkeit/TarifstrukturHuelle.cs` | `UcWirtschaftlichkeit.cs:213/440`, `BhkwWirtschaftlichkeitHuelle.cs:184` |
| W2.4 | `Form_PhotovoltaikVerguetung` (Designer + 422) | `EPOS.UI/Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor` | `Views/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs` | `UcWirtschaftlichkeit.cs:226`, `Views/Kosten/ucErtragBonus.cs:178` |
| W2.5 | `Form_WirtschaftlichkeitParameter` (K4, 740) | `EPOS.UI/Dialoge/Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog.razor` | `Views/Wirtschaftlichkeit/WirtschaftlichkeitParameterHuelle.cs` | `UcWirtschaftlichkeit.cs:449` |

**Commits** (ein Commit je Nummer, Reihenfolge des Plans):

```
f9b5016  iU9-W2.1  Namensdialog-Ausrollung - die letzten drei Namensmasken
41db247  iU9-W2.2  Sprungbruecke - ein Blazor-Dialog oeffnet ein WinForms-Fenster
938947a  iU9-W2.3  TarifstrukturDialog - der Tarifdialog als Razor-Komponente
a684fcd  iU9-W2.4  PhotovoltaikVerguetungDialog - die PV-Verguetung als Komponente
8ef5b60  iU9-W2.5  WirtschaftlichkeitParameterDialog + Ersteinsatz der Sprungbruecke
a2b3bd2  iU9-W2.6  Ressourcen-Sammelnachtrag - 78 Schluessel in de, en und Designer
3fd320e  iU9-W2.7  Formularkarte-Tests - viertes Pruefmuster und neue Zaehler
(dieses Protokoll)  iU9-W2.8
```

---

## 2. Bauweise

### 2.1 Die Sprungbrücke (W2.2) — Entscheid zu B5b‑O1 und Risiko R1

**Der Befund aus B5b.** Ein Blazor-Dialog, der weiterführt, konnte bis hierher nur
**nachgelagert** springen: Die Komponente meldete den Wunsch im Ergebnis, die Hülle
schloss den Dialog, öffnete das Ziel und brachte den Dialog danach zurück
(`BhkwWirtschaftlichkeitHuelle.TarifOeffnen`). Für den Anwender verschwindet dabei das
Fenster und kommt wieder — und alles Ungespeicherte ist fort.

**Die Lösung.** Ein Delegat mit sprachneutralem Schlüssel:

| Seite | Datei | Inhalt |
|---|---|---|
| plattformfrei | `EPOS.UI/Dialoge/Allgemein/Sprungziel.cs` | die ASCII-Schlüssel; der Dialog nimmt `[Parameter] Func<string, Task<bool>>? Sprung` |
| Windows | `Allgemein/Blazor/Sprungbruecke.cs` | `Fuer(besitzer)` → Delegat; ein `switch` über die Schlüssel, eine Maske je Zweig (Muster `WinFormsNavigation.OeffneMaske`) |

**Warum das trägt.** Der Blazor-Verteiler der `BlazorWebView` läuft im
WinForms-Oberflächenfaden. `ShowDialog()` aus einem Komponentenrückruf öffnet dort dieselbe
verschachtelte Nachrichtenschleife wie ein `OpenFileDialog` in einem `Click`-Ereignis: Der
Blazor-Dialog bleibt stehen und pumpt weiter, das Ziel liegt modal darüber. Die Zusicherung
„Rückruf im Oberflächenfaden" steht allerdings nirgends geschrieben — die Brücke prüft den
Faden deshalb und wechselt notfalls über `Control.Invoke`.

**Die Grenze (R1/R2).** Die Brücke führt **ausschließlich WinForms-Masken**. Ist das Ziel
selbst eine Blazor-Hülle, bleibt es beim nachgelagerten Sprung: Zwei WebViews übereinander
kosten Speicher und Aufbauzeit und verwirren die Fokusreihenfolge (Risiko R2), und dafür gibt
es bis Welle 4 (`Ueberlagerung`) keinen Baustein. In dieser Welle heißt das:

| Sprung | Ziel | Weg |
|---|---|---|
| Wirtschaftlichkeits-Parameter → Gesetzeskatalog | `Form_Gesetzesparameter` (WinForms, bleibt bis W14c) | **Brücke**, modal über dem Dialog |
| PV-Vergütung → Marktwert-Import | `OpenFileDialog` | **Brücke-Mechanik**, Delegat der Hülle |
| PV-Vergütung → Tarifstruktur | Blazor-Hülle | **nachgelagert** |
| Wirtschaftlichkeits-Parameter → BHKW-Wirtschaftlichkeit | Blazor-Hülle | **nachgelagert** |
| BHKW-Wirtschaftlichkeit → Tarifstruktur | Blazor-Hülle (seit W2.3) | **nachgelagert**, unverändert |

**Am Gerät zu prüfen** (Abnahmepunkt W2‑7): Öffnet sich der Katalog wirklich über dem
Dialog, bleibt er modal, und steht der Dialog danach unverändert da? Fällt das durch, ist der
Rückweg der nachgelagerte Sprung — eine Zeile in der Hülle, kein Umbau der Komponente.

### 2.2 Der erweiterte Namensdialog (W2.1)

Drei Zusätze, jeder mit genau einem Vorbild; leer bzw. `false` ist jeweils der Bestand, die
vier Aufrufer aus W1 ändern sich nicht:

| Parameter | Vorbild | Wozu |
|---|---|---|
| `HinweisText` | `Form_AlsVariante.lblHinweis` | der erklärende Satz über dem Feld |
| `ZusatzFrageText`/`-Vorbelegung`/`-Geschlossen` | `Form_GebaeudetypNeu.textBox_Beschreibung` | ein zweites, freiwilliges Textfeld |
| `OkNurMitText` | `btnAnlegen.Enabled = Bezeichner.Length > 0` | OK bleibt gesperrt, solange das Feld leer ist |

Die Hülle bekommt zwei Einstiege mit den Texten der gelöschten Maske —
`Bezeichner(besitzer, vorbelegung)` für die 28 Aufrufer und
`BezeichnerUndBeschreibung(besitzer, out beschreibung)` für `Form_EingGebTyp` — sowie
`FragenMitHinweis(...)` für den Variantenablauf. **Alle 28 Aufrufstellen sind damit
Einzeiler**; keine trägt mehr `PointToScreen`, `SetControl()` oder ein zweites `ShowDialog`.

### 2.3 Die Hüllen mit Datenseite

Muster durchweg `BhkwWirtschaftlichkeitHuelle`: laden mit denselben Controllern und in
derselben Reihenfolge wie zuvor der Maskenkonstruktor, schreiben über einen Rückruf.

| Hülle | Lädt | Schreibt | Delegaten |
|---|---|---|---|
| `TarifstrukturHuelle` | `WirtschaftlichkeitCtrl.LadeTarif` | `SpeichereTarif` | `Speichern` |
| `PhotovoltaikVerguetungHuelle` | `ProjektPhotovoltaikCtrl.LiesOderVorbelegt`, `PhotovoltaikCtrl.KwpDesProjekts`, `ErgebnisCtrl.Load`, `KostenSummenCtrl.LiesKomponentenSummen`, `WirtschaftlichkeitCtrl.StromArbeitspreisEurJeKwh` + `LadeParameter` | `ProjektPhotovoltaikCtrl.Speichern` | `Katalog`, `Jahresmarktwert`, `Speichern`, `MarktwerteImportieren` |
| `WirtschaftlichkeitParameterHuelle` | `LadeParameter`, `ErzeugerDerGruppe`, `EmissionsBilanzRechner.LadeKatalog`, `LiesReferenzkessel`, `GesetzKatalog.AlleDerKlasse` | `SpeichereParameter` | `Sprung` (Brücke), `Speichern` |

### 2.4 Zwei erweiterte Standardfelder

`Zahlenfeld` und `Ganzzahlfeld` haben jetzt `Aktiv` (additiv, Vorgabe `true`) — dieselbe
Eigenschaft, die `Schalter` und `Auswahlfeld` schon führten. Gebraucht wird sie vom
Tarifdialog: Er sperrt den Block des **nicht gewählten** Rechenmodells, statt ihn
auszublenden, damit die Werte des anderen Modells lesbar und erhalten bleiben — wortgleich
zu `Form_Tarifstruktur.ModusUebernehmen` (`Enabled`, nicht `Visible`).

Neue CSS-Klasse `epos-untergruppe`: der fette Blocktitel **innerhalb** einer Gruppe, mit dem
die programmatisch gebauten Masken gliederten (`Form_Tarifstruktur.Gruppe`,
`Form_WirtschaftlichkeitParameter.Gruppe`). Ein zweiter `Gruppenkopf` wäre ein Balken im
Balken und damit eine Hierarchie, die es fachlich nicht gibt.

---

## 3. Feldkarten-Abgleich

Der Abgleich ist **als Test ausgeführt** (`EPOS.UI.Tests/Dialoge/*Tests.cs`), nicht als
einmalige Messung. Die Karten der drei Designer-Masken wurden vor Beginn frisch gezogen
(`Werkzeuge/Formularkarte`, Stand `b0d3d86`); für die drei **K4-Masken ohne Designer** ist
die Karte von Hand aus `InitializeComponent` aufgenommen (Regel F1).

| Maske | Soll (Feldkarte) | Ist (Komponente) | Deckung |
|---|---|---|---|
| `Form_StromspeicherItemNeu` | 1 Feld + Kopftitel + OK/Abbrechen | dasselbe Textfeld der W1-Komponente | **1/1** |
| `Form_GebaeudetypNeu` | 2 Felder + OK/Abbrechen | Textfeld + zweites Textfeld | **2/2** |
| `Form_AlsVariante` (Handkarte) | Hinweis, Bezeichnerfeld, „Variante anlegen"/„Abbrechen" | `Herleitungszeile` + Textfeld + `SpeichernLeiste` mit eigenen Knopftexten | **1/1 + Hinweis** |
| `Form_Tarifstruktur` (Handkarte) | 52 Bedienfelder: Kopf 3 · Zeitzonen 4 · Zonenmodell 11 · Rollenmodell 2 × 16 · Einspeisung 2 | 1 `Schalter`, 1 `Datumsfeld`, 3 `Auswahlfeld`, 4 `Ganzzahlfeld`, 43 `Zahlenfeld` | **52/52** |
| `Form_PhotovoltaikVerguetung` | 36 Kartenzeilen: 19 Bedienfelder, 4 Knöpfe, 13 Anzeigelabels | 3 `Schalter`, 6 `Zahlenfeld`, 1 `Ganzzahlfeld`, 1 `Datumsfeld`, 2 `Auswahlfeld`, 2 `Optionsgruppe` (2 + 4 Optionen), 4 Knöpfe, 13 `Herleitungs-`/`Warnzeilen` | **36/36** |
| `Form_WirtschaftlichkeitParameter` (Handkarte) | Allgemein 4 · Strom 3 · Brennstoff 7 + 2 Anzeigezeilen + Katalogknopf · BHKW-Verweis + Knopf | dieselben, gruppenweise nach `ErzeugerFlags` | **14/14 + 3 Zeilen** |

**Die zwölf ausgeblendeten BHKW-Felder des Parameterdialogs** stehen in keiner Ist-Spalte —
sie stehen seit Etappe B5 im Dialog „BHKW-Wirtschaftlichkeit". Die WinForms-Fassung baute sie
trotzdem weiter auf und blendete sie unmittelbar danach aus (`Bw9Ausblenden`), damit ihr
Speicherweg dieselben Steuerelemente auslesen konnte; sie schrieb sie damit wertgleich
zurück. Die Blazor-Fassung baut sie gar nicht mehr und schreibt sie folglich auch nicht —
das Ergebnis ist dasselbe, weil der geladene Parametersatz vollständig hereinkommt und
vollständig zurückgeht (Test:
`Die_ausgezogenen_BHKW_Werte_bleiben_unberuehrt`).

**Kein Feld einer Karte fehlt.**

---

## 4. Abweichungen (mit Begründung)

| # | Abweichung | Begründung |
|---|---|---|
| **A‑1** | W2.1: Der Name kommt **getrimmt** zurück | `Form_Sp_ItemNeu.m_szName` war der rohe Feldinhalt; ein Bezeichner mit führendem Leerzeichen ist in einer Katalogtabelle eine Fehlerquelle, und `StromspeicherKontextMenuCtrl` trimmte schon von Hand nach |
| **A‑2** | W2.1: Der Dialog erscheint **mittig** statt an der Knopfposition | 23 der 28 Aufrufer setzten `frm.Location = PointToScreen(btn.Location)`. Die Blazor-Hülle ist seit iU8 durchgängig mittig über dem Besitzerfenster; ein Sonderweg für 23 Aufrufer wäre die zweite Regel (dieselbe Entscheidung wie A‑4 in W1) |
| **A‑3** | W2.1: Eine leere Eingabe meldet sich **im Dialog** statt in einer MessageBox | Hausregel `EPOS.UI/CLAUDE.md`; der Text ist derselbe (`NAMD_MSG_BEZEICHNUNG` = „Bezeichnung eingeben!") |
| **A‑4** | W2.1: `Form_EingBrauchwasserTyp.btn_Neu_Click` öffnet den Namensdialog nur noch **einmal** | **Befund.** Der Vorläufer rief `frm.ShowDialog()` zweimal — der Dialog ging zweimal auf, und ausgewertet wurde der zweite Lauf. Dieselbe Stelle war in `Form_EingProzTyp` und `Form_EingStromTyp` schon einmal korrigiert („BUGFIX: Nur ein Aufruf von ShowDialog()"); hier war sie stehen geblieben |
| **A‑5** | W2.1: `Form_AlsVariante` hat als einzige Namensabfrage einen **gesperrten OK-Knopf**, die übrigen vier melden | Beides steht so im Bestand. `OkNurMitText` macht die Unterscheidung zu einem Parameter statt zu zwei Dialogen |
| **A‑6** | W2.3: Die vierstufige Staffel steht als **beschriftete Felderfolge** („Stufe 2 — Sommer [€/kW·a]") statt als Raster Grenze \| Sommer \| Winter | Eine dreispaltige Zahlentabelle ist auf einem Telefon nicht zu bedienen (M2), und ein Feld, dessen Bedeutung nur aus einem Spaltenkopf hervorgeht, ist für ein Sprachausgabeprogramm namenlos. Die Spaltenköpfe wandern in die Feldbeschriftung |
| **A‑7** | W2.3/W2.5: Ein **geleertes** Zahlenfeld behält den geladenen Wert | Eine `NumericUpDown` konnte nicht leer sein, ein Eingabefeld schon. Eine 0 setzt man weiterhin, indem man 0 schreibt; so kann ein versehentlich geleertes Feld keine Null in die Datenbank tragen |
| **A‑8** | W2.3: Der abwählbare Kalender (`DateTimePicker` mit `ShowCheckBox`) wird ein **leeres Datumsfeld** | Leer heißt dort dasselbe wie der abgehakte Kasten: „kein Preisstand gepflegt". Ein zweites Bedienelement für dieselbe Aussage wäre eine Erklärung, die niemand braucht |
| **A‑9** | W2.3: Die beiden weichen Prüfungen („aktiv, aber kein Preis gepflegt") erscheinen als **Warnbanner und halten den ersten Klick an**; der zweite speichert | Der Vorläufer zeigte eine MessageBox und speicherte unmittelbar danach. Die Reihenfolge ist dieselbe — sehen, bestätigen, schreiben —, nur ohne zweites Fenster. Das leere HT-Fenster bleibt eine **harte** Sperre, wie bisher |
| **A‑10** | W2.4: Aus zwei `RadioButton`-Paaren werden zwei `Optionsgruppe`n; die Vermarktung sperrt **einzelne** Optionen | Der Baustein aus W1 kann das (`Gesperrt`), und die Zulässigkeitsregel des Vorläufers (`rbEv.Enabled`, `rbKeine.Enabled`) meint genau das. Die Umschaltung auf Marktprämie, wenn die gewählte Form unzulässig wird, ist wortgleich übernommen |
| **A‑11** | W2.4: Die Einspeiseart bekommt einen **Gruppentitel** („Einspeiseart", `PVV_G_EINSPEISEART`) | In WinForms standen die zwei Knöpfe ohne gemeinsamen Namen in `grpAnlage`; untereinander in einer `fieldset`-Gruppe brauchen sie eine `legend`, sonst ist die Gruppe für die Sprachausgabe namenlos |
| **A‑12** | W2.4: Die Pflichtprüfung „Inbetriebnahme angeben" wird **zum ersten Mal wirksam** | **Befund.** `if (m.Aktiv && m.Inbetriebnahme == DateTime.MinValue)` konnte in WinForms nie zutreffen — ein `DateTimePicker` hat immer ein Datum. Ein `Datumsfeld` kann leer sein; die Prüfung greift jetzt und meldet als Fehlerbanner |
| **A‑13** | W2.4: Die Meldungen des Marktwert-Imports werden **Banner**; ein abgebrochener Dateidialog sagt nichts | Hausregel; die MessageBox unterbrach auch dann, wenn nichts passiert war |
| **A‑14** | W2.5: Die zwölf ausgezogenen BHKW-Felder werden **gar nicht mehr gebaut** | Siehe § 3. Der Umweg „bauen, ausblenden, auslesen, wertgleich zurückschreiben" hatte genau einen Grund — den Speicherweg über Steuerelemente. Den gibt es hier nicht mehr |
| **A‑15** | W2.5: Der Sprung in den BHKW-Dialog **schließt den Parameterdialog** (nachgelagert) und sagt es vorher | In WinForms lag der Sammeldialog modal darüber, der Parameterdialog blieb stehen. Beide sind jetzt Blazor-Hüllen (R2). Neu ist auch: Ein Speichern im Sammeldialog zählt als Speichern — er schreibt denselben Parametersatz |
| **A‑16** | W2.3/W2.4/W2.5: **Enter** bleibt in allen drei Großdialogen unbelegt, **Esc** schließt | A‑7 aus B5b: In einer Maske mit fünfzig Zahlenfeldern wäre ein versehentliches Enter kein Bestätigen, sondern ein Zufall |
| **A‑17** | Alle: Der Speicherfehler wird ein **Fehlerbanner** statt einer MessageBox | Hausregel, wie A‑19 in W1 |
| **A‑18** | W2.4: Der Knopf „Marktwerte importieren…" **fehlt**, wenn die Umgebung keinen Dateiwähler mitgibt; ebenso der Katalogknopf in W2.5 ohne Sprungbrücke | Ein Knopf, der nichts tut, ist eine Behauptung, die nicht stimmt. Auf iOS gibt es beide Ziele (noch) nicht |

**Drei Rückstände**, bewusst stehen gelassen: In `Form_DBBHKW`, `Form_PufferSp_Bearbeiten`
und `Form_SolarDB` steht hinter der Namensabfrage weiterhin ein
`if (string.IsNullOrEmpty(szName))` mit eigener Meldung. Die Hülle liefert nie einen leeren
Namen; die Prüfung ist damit tot, aber harmlos, und ihr Entfernen wäre eine Änderung an
Speicherwegen, die zu dieser Welle nicht gehört.

---

## 5. Texte

**78 neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` und —
von Hand, weil hier kein Visual Studio läuft — `Resource.Designer.cs` (alphabetisch zwischen
den Nachbarn, im Muster der erzeugten Datei; die Änderung ist in allen drei Dateien rein
additiv):

| Präfix | Zahl | Dialog |
|---|---|---|
| `NAMD_*` | 4 | Namensabfrage (Bezeichner, Beschreibung, Leermeldung) |
| `TARIF_*` | 50 | Tarifstruktur |
| `PVV_*` | 3 | PV-Vergütung — nur die Texte, die es in WinForms nicht gab |
| `WPAR_*` | 21 | Wirtschaftlichkeits-Parameter |

**Wiederverwendet statt neu angelegt:** die **63** vorhandenen `PVW_*`-Schlüssel der
PV-Vergütung, `WIRT_DLG_CO2`/`_KONSTANT_ZEILE`/`_PFAD_ZEILE`/`_KATALOG`, alle `BILANZ_DLG_*`,
`BHW_PARAM_GRUPPE`/`_VERWEIS`/`_KNOPF`, `WIRT_DLG_KWKG_HINWEIS`,
`WIRT_DLG_STEUER_FORMULARE`, `ALLG_BTN_OK`/`_ABBRECHEN`, `VAR_DLG_TITEL`/`_HINWEIS`,
`BK_LBL_BEZEICHNER`, `BK_BTN_ANLEGEN`, `SIM_BTN_ABBRECHEN`.

**Zugriff** über `BhwTexte.T` (`ResourceManager.GetString` mit deutschem Rückfall, Muster
B5b‑O4). Der Rückfall bleibt stehen, greift aber nicht mehr — ein fehlender Schlüssel würde
den Dialog nicht mitreißen.

**Nicht angelegt:** `TARIF_SP_STUFE`. Die Spaltenüberschrift „Staffelstufe" gibt es in der
Blazor-Fassung nicht mehr (A‑6); die ungenutzte Eigenschaft ist aus `TarifstrukturTexte`
entfernt.

**`help_mapping.txt` bleibt unverändert.** Die drei Zeilen
`Form_Tarifstruktur.btn_Help`, `Form_PhotovoltaikVerguetung.btn_Help` und
`Form_WirtschaftlichkeitParameter.btn_Help` gelten weiter — der Schlüssel benennt die
Wikiseite, nicht die Klasse (dasselbe Vorgehen wie bei `Form_Kosten_Auswahl.btn_Help` seit
iU8‑9). Die drei Namensmasken hatten nie einen Infoknopf.

**`Allgemein/KI/HilfeKontext.cs`:** die sieben Einträge der sechs gelöschten Masken entfernt
(`Form_Sp_ItemNeu` **und** `Form_StromspeicherItemNeu` standen beide darin) — jeweils im
Commit ihrer Maske (Regel F10).

---

## 6. WinForms-Seite

**Gelöscht** (11 Dateien):

```
Views/Gebäude/Form_GebaeudetypNeu.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}
Views/Varianten/Form_AlsVariante.cs
Views/Wirtschaftlichkeit/Form_Tarifstruktur.cs
Views/Wirtschaftlichkeit/Form_PhotovoltaikVerguetung.{cs,Designer.cs}
Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs
```

**Verschoben** (5 Dateien) — `Views/Stromspeicher/Form_StromspeicherItemNeu.*` nach
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Stromspeicher/`: An ihr hängen sechs Testbezüge
des Werkzeugs; sie ist der einzige Beleg für den lokalisierten Weg (§ 8).

**Neu** auf der Windows-Seite: `Allgemein/Blazor/Sprungbruecke.cs`,
`Views/Varianten/AlsVarianteHuelle.cs`, `Views/Wirtschaftlichkeit/TarifstrukturHuelle.cs`,
`Views/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs`,
`Views/Wirtschaftlichkeit/WirtschaftlichkeitParameterHuelle.cs`.

**Keine Typverwendung ist übrig:**

```
git grep -nE "(new|typeof|:)\s*Form_(StromspeicherItemNeu|Sp_ItemNeu|GebaeudetypNeu|AlsVariante|Tarifstruktur|PhotovoltaikVerguetung|WirtschaftlichkeitParameter)\b" -- '*.cs' '*.razor'
→ 0 Treffer
```

Restfundstellen der alten Namen sind ausschließlich (a) `HilfeSchluessel`-Zeichenketten
(`"Form_X.btn_Help"` — Schlüssel des Hilfekatalogs, § 5), (b) Kommentare, die die Herkunft
nennen, und (c) die Prüfmusterbezüge der Formularkarte-Tests.

---

## 7. Nachweise

### 7.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental
→ 0 Fehler, 28 Warnungen
```

Basis (`b0d3d86`): ebenfalls 28. Die sechs gelöschten Masken trugen keine
WFO1000-Fundstelle; das Warnungsbild ist unverändert — 22 × WFO1000, 2 × CS0108,
2 × CS0109, 1 × WFO0003, 1 × CA2255.

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ EPOS.Kern.Tests       35 grün
  KiKern.Tests         450 grün
  SpeicherEngine.Tests 337 grün
  EPOS.UI.Tests        288 grün   (214 vorher, 74 neu)
  ────────────────────────────────
  1 110 grün, 0 rot    (1 036 vorher)
```

Die 74 neuen bunit-Tests:

| Datei | Tests | Prüft |
|---|---|---|
| `Dialoge/NamensDialogTests.cs` | +7 | Hinweiszeile, zweites Feld mit eigener Vorbelegung, Meldereihenfolge (Zusatz vor Schließen), gesperrter OK-Knopf, Enter bei gesperrtem OK |
| `Dialoge/SprungzielTests.cs` | 4 | ASCII-Schlüssel, Eindeutigkeit, feste Werte als Paar zur Windows-Brücke |
| `Standards/ZahlenfeldTests.cs` | +2 | ohne Angabe bedienbar; `Aktiv=false` sperrt und lässt den Wert lesbar |
| `Standards/GanzzahlfeldTests.cs` | +2 | dasselbe |
| `Dialoge/TarifstrukturDialogTests.cs` | 20 | Feldbestand (52), Gruppentitel, Vorbelegung, drei Sichten, „nur gebaute Felder überschreiben", Sperrwerk beidseitig, HT/NT im Rollenmodell, Speichern, geleertes Feld, HT-Sperre, Warnung-dann-Speichern, Speicherfehler, Abbrechen/Esc, Enter unbelegt, Hilfeschlüssel, Leistungsmodell als Steuerwert, Staffelbeschriftungen |
| `Dialoge/PhotovoltaikVerguetungDialogTests.cs` | 22 | Feldbestand (36), sieben Gruppen in Reihenfolge, Vorbelegung, Ausschreibungswarnung, EV-Sperre über 100 kW samt Umschaltung, PPA-Felder, § 51-Status in vier Fällen, Kappungsstatus in drei, Vorschau mit und ohne Lauf, Nullsemantik, Speichern, IBN-Pflicht, Speicherfehler, Tarifsprung, Import (Erfolg/Fehler/Abbruch), Abbrechen/Esc, Enter, Hilfeschlüssel |
| `Dialoge/WirtschaftlichkeitParameterDialogTests.cs` | 18 | Feldbestand je Erzeugerlage, Aufschlagshaken nur Anzeige, BHKW-Verweis statt Gruppen, Emissionsgruppe vollständig, Referenzkesselzeile, Vorbelegung, CO₂-Zeile (Pfad/konstant), Nullsemantik, geleertes Feld, Speichern, ausgezogene Werte unberührt, beide Sprünge, Speicherfehler, Abbrechen/Esc, Enter, Hilfeschlüssel, wachsender Schlusshinweis |

Die Tests mit Zahlen in der Anzeige pinnen `de-DE` wie `SpeichernLeisteTests` — die
CI-Läufer laufen englisch.

### 7.3 Formularkarte

```
dotnet build Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 0 Fehler, 0 Warnungen
dotnet test  Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 120 grün (119 vorher)
```

Sechs Testbezüge hingen an `Form_StromspeicherItemNeu` (Risiko R8) — sie ist als **viertes
Prüfmuster** eingefroren, ausnahmsweise mit fünf Dateien: Der lokalisierte Weg braucht alle
drei Ressourcendateien. `PruefmusterTests` führt sie als dritte `Theory`-Zeile; daher 120
statt 119.

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -c Release -- --alle WindowsFormsApplication1 --erreichbarkeit
```

| Kennzahl | nach W1 | nach W0 | **nach W2** |
|---|---:|---:|---:|
| Designer-Dateien (Repo) | 114 | 108 | **105** |
| davon Masken | 111 | 105 | **102** |
| lokalisiert | 62 | 61 | **59** |
| Kartenzeilen | 2 322 | 2 231 | **2 188** |
| Felder ohne Beschriftung | 172 | 168 | **168** |
| Öffner erreichbar („ja") | 104 | 103 | **100** |
| unerreichbar / verwaist / unklar | 4/1/2 | 0/0/2 | **0/0/2** |

Die drei K4-Masken der Welle hatten nie eine Designer-Datei und sind in dieser Zählung nie
erschienen; ihr Verschwinden steht in
[`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md`](../../../Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md).

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 303 SQL-Texte geprüft: 0 Fundstellen, 149 dynamisch, 1 154 in Ordnung
```

Die Welle hat keine SQL-Anweisung angefasst — die drei Hüllen rufen ausschließlich
vorhandene Controller.

### 7.6 Referenzlauf

```
dotnet run --project EPOS.Referenzlauf -c Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 --ziel <ordner>
dotnet run --project EPOS.Referenzlauf -c Release -- vergleich Referenzlaeufe/2026-08-30_B3-Kaskade <ordner>
```

| Projekt | Ergebnis |
|---|---|
| 1007 | **PASS** (29 Dateien, 324 219 Werte) |
| 1017 | **PASS** (21 Dateien, 254 154 Werte) |
| 1030 | **PASS** (22 Dateien, 236 670 Werte) |

`diff -rq` gegen die Basis meldet für diese drei Ordner **keinen** Unterschied. Der Lauf ist
Pflicht, weil der Parameterdialog `Tab_ProjektWirtschaftlichkeit` schreibt und der
Tarifdialog `Tab_ProjektTarif` — beide gehen in den Rechenweg ein. Der Kern selbst wurde in
dieser Welle **nicht** angefasst.

### 7.7 Veröffentlichung

```
dotnet publish WindowsFormsApplication1 -c Release -r win-x64 --self-contained -p:Platform=x64 -o <ordner>
```

`wwwroot` vollständig: `index.html`, `_framework/blazor.webview.js`,
`_framework/blazor.modules.json`, `_content/EPOS.UI/{epos-ui.css,help_icon.png}` (samt
`.br`/`.gz`), `_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js`. Die
neue CSS-Klasse `epos-untergruppe` ist in der ausgelieferten `epos-ui.css` enthalten.

---

## 8. Grenzen

* **Keine Windows-Sicht.** Alles hier ist auf Linux gemessen: Build, Tests, Referenzlauf,
  Veröffentlichung. Ob die Dialoge in der WebView2 richtig aussehen — und vor allem, **ob die
  Sprungbrücke ihre verschachtelte Nachrichtenschleife sauber trägt** —, sagt erst die
  Abnahme (§ 9).
* **Drei Sprünge bleiben nachgelagert** (R2). Sie werden erst mit dem Baustein
  `Ueberlagerung` (Welle 4) zu einem Fenster über dem anderen.
* **Die Rückfrage vor dem Löschen** und **der Fortschritt** fehlen weiterhin (A‑16/A‑17 aus
  W1) — in dieser Welle kommt keine der beiden Stellen vor.
* **`Form_Gesetzesparameter` bleibt WinForms** bis Welle 14c. Auf iOS ist der Katalog damit
  bis dahin nicht erreichbar; der Knopf fehlt dort schlicht (A‑18).

---

## 9. Abnahmeliste Windows (iZ5) für diese sechs Dialoge

Wege: **Menü → Projekte → Als Variante speichern…** (W2.1c) · jede Katalogmaske mit „Neu…"
bzw. „Speichern unter…" (W2.1a, 28 Stellen — Stichprobe BHKW-Admin, Heizkessel-Admin,
Solarkollektoren, Brauchwassertyp) · **Menü → Gebäudetypen bearbeiten → Neuer Typ**
(W2.1b) · **Berichte & Kosten → Wirtschaftlichkeit** für „Tarifstruktur…", „Strombezug…",
„PV-Vergütung…" und „Parameter…" (W2.3–W2.5) · **Kostenverwaltung → Erträge/Boni → PV**
für den zweiten Aufrufer von W2.4.

| # | Punkt | W2.1 | W2.3 | W2.4 | W2.5 |
|---|---|:--:|:--:|:--:|:--:|
| 1 | Öffnet mittig, kein weißes Aufblitzen | ☐ | ☐ | ☐ | ☐ |
| 2 | Fenster ziehbar **und** maximierbar | ☐ | ☐ | ☐ | ☐ |
| 3 | Deutsch **und** Englisch (`HKCU\Software\wp-plan\Language`) | ☐ | ☐ | ☐ | ☐ |
| 4 | Hochkontrast: Warnbanner und Fehleingabe bleiben unterscheidbar | ☐ | ☐ | ☐ | ☐ |
| 5 | 125 % und 150 % scharf (DPI-Insel greift) | ☐ | ☐ | ☐ | ☐ |
| 6 | Maus **und** Finger (44 px), Optionsgruppen mit den Pfeiltasten | ☐ | ☐ | ☐ | ☐ |
| 7 | Tab-Zyklus bleibt im Dialog, Esc schließt | ☐ | ☐ | ☐ | ☐ |
| 8 | Infoknopf zeigt die Wikiseite | – | ☐ | ☐ | ☐ |
| 9 | Gesperrte Felder sind als gesperrt **erkennbar**, nicht nur blass | – | ☐ | ☐ | – |

**Fachliche Proben:**

| # | Probe |
|---|---|
| **W2‑1** | W2.1a: In `Form_BHKWAdmin` „Neu…" — der Namensdialog erscheint mittig, ein leerer Name meldet sich und hält offen, Esc verwirft, ein Name führt in `Form_DBBHKW` |
| **W2‑2** | W2.1a: In `Form_EingBrauchwasserTyp` „Neu" — der Dialog geht **einmal** auf (A‑4), danach steht der neue Typ in der Liste |
| **W2‑3** | W2.1b: Gebäudetyp anlegen — Bezeichner **und** Beschreibung stehen danach in `Tab_DBTagV_STAMM` |
| **W2‑4** | W2.1c: „Als Variante speichern…" — der Hinweis nennt das Stammprojekt, der Knopf „Variante anlegen" bleibt gesperrt, bis ein Bezeichner steht |
| **W2‑5** | W2.3: Modell umschalten — der andere Block wird grau, seine Zahlen bleiben lesbar; HT-von/bis sperren im Rollenmodell; „Speichern" mit von ≥ bis meldet und speichert **nicht** |
| **W2‑6** | W2.3: In der PV-Sicht nur die Einspeisepreise ändern und speichern — Bezugspreise und Staffel stehen danach unverändert in `Tab_ProjektTarif` (Ä18) |
| **W2‑7** | **W2.5: Der Katalogknopf.** „Gesetzliche Parameter (CO₂-Preispfad)…" öffnet `Form_Gesetzesparameter` **modal über** dem Blazor-Dialog; nach dem Schließen steht der Dialog unverändert da, die Eingaben sind erhalten. **Das ist der Prüfpunkt für die Sprungbrücke (R1).** |
| **W2‑8** | W2.5: „⚙ BHKW-Wirtschaftlichkeit…" schließt den Parameterdialog, zeigt den Sammeldialog und bringt den Parameterdialog danach mit frischen Werten zurück |
| **W2‑9** | W2.4: kWp-Override auf 250 setzen — „Feste Einspeisevergütung" wird grau und die Wahl springt auf Marktprämie; die Warnzeile erscheint erst über 1 MW |
| **W2‑10** | W2.4: „Marktwerte importieren…" — Dateiwähler erscheint über dem Dialog, Abbrechen sagt nichts, ein gültiges CSV meldet den Bericht als Hinweisbanner |
| **W2‑11** | W2.4: Ohne Inbetriebnahmedatum und mit aktiver Vergütung meldet „Übernehmen" (A‑12) |

---

## 10. Offene Punkte

| # | Punkt |
|---|---|
| **W2‑O1** | **Sprungbrücke am Gerät** (W2‑7). Trägt die verschachtelte Nachrichtenschleife? Falls nicht: `WirtschaftlichkeitParameterDialog` bekommt statt `Sprung` einen zweiten Wert in `WirtParameterSprung`, und die Hülle behandelt ihn wie den BHKW-Sprung — eine Zeile je Seite |
| **W2‑O2** | **A‑6 sichtprüfen:** Die Staffel als 24 einzeln beschriftete Felder je Rolle ist barrierefrei richtig, aber lang. Wenn der Anwender die Tabellenform vermisst, ist ein `Raster` mit Eingabespalten der nächste Schritt (Welle 4 bringt dafür ohnehin Bausteine) |
| **W2‑O3** | **A‑12 dem Anwender vorlegen:** Die Pflichtangabe „Inbetriebnahme" greift zum ersten Mal wirklich. Bestandsprojekte mit `DateTime.MinValue` in `Tab_ProjektPhotovoltaik` lassen sich damit nicht mehr speichern, solange „Vergütung anwenden" steht — gewollt? |
| **W2‑O4** | **A‑7:** „Leeres Feld behält den geladenen Wert" ist eine neue Hausregel für Zahlenfelder mit Bereichsvorgabe. Sie sollte in `EPOS.UI/CLAUDE.md` stehen, sobald die zweite Welle sie bestätigt hat |
| **W2‑O5** | Die drei toten Leerprüfungen hinter der Namensabfrage (§ 4, Rückstände) verschwinden, wenn `Form_DBBHKW`, `Form_PufferSp_Bearbeiten` und `Form_SolarDB` selbst umgestellt werden (Wellen 6, W14a, W12) |
| **W2‑O6** | `Sprungziel.Gesetzesparameter` (ohne Vorwahl) hat noch keinen Aufrufer. Er steht da, weil der Katalog auch von `ucErtragBonus` aus geöffnet wird — diese Maske kommt erst in Welle 4 |
| **W2‑O7** | Der Tarifdialog zeigt in der Sicht „Photovoltaik" nur sechs Felder, hat aber weiter die Fensterbreite eines Großdialogs. Ob die Hülle je Sicht ein eigenes Maß bekommen sollte, entscheidet die Abnahme |

---

## 11. Geänderte und neue Dateien

```
NEU
  EPOS.UI/Dialoge/Allgemein/Sprungziel.cs                                        52 Zeilen
  EPOS.UI/Dialoge/Wirtschaftlichkeit/TarifstrukturDaten.cs                      202
  EPOS.UI/Dialoge/Wirtschaftlichkeit/TarifstrukturDialog.razor                  486
  EPOS.UI/Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDaten.cs             170
  EPOS.UI/Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor         549
  EPOS.UI/Dialoge/Wirtschaftlichkeit/WirtschaftlichkeitParameterDaten.cs        153
  EPOS.UI/Dialoge/Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog.razor    305
  WindowsFormsApplication1/Allgemein/Blazor/Sprungbruecke.cs                    119
  WindowsFormsApplication1/Views/Varianten/AlsVarianteHuelle.cs                 123
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/TarifstrukturHuelle.cs       95
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs 218
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/WirtschaftlichkeitParameterHuelle.cs 190
  EPOS.UI.Tests/Dialoge/SprungzielTests.cs                                       45  (4 Tests)
  EPOS.UI.Tests/Dialoge/TarifstrukturDialogTests.cs                             364  (20)
  EPOS.UI.Tests/Dialoge/PhotovoltaikVerguetungDialogTests.cs                    387  (22)
  EPOS.UI.Tests/Dialoge/WirtschaftlichkeitParameterDialogTests.cs               320  (18)
  Werkzeuge/Formularkarte.Tests/Pruefmuster/Stromspeicher/Form_StromspeicherItemNeu.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}
  WindowsFormsApplication1/Allgemein/Reporting/iU9_W2_Blazor_Port_Protokoll.md  dieses Protokoll

GEÄNDERT
  EPOS.UI/Dialoge/Allgemein/NamensDialog.razor        + Hinweis, zweites Feld, OkNurMitText
  EPOS.UI/Standards/Zahlenfeld.razor                  + Aktiv
  EPOS.UI/Standards/Ganzzahlfeld.razor                + Aktiv
  EPOS.UI/wwwroot/epos-ui.css                         + epos-untergruppe
  EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDaten.cs  Begründung von BhkwSprung
  EPOS.Kern/MyResource/Resource.resx                  + 78 Schlüssel
  EPOS.Kern/MyResource/Resource.en-US.resx            + 78
  EPOS.Kern/MyResource/Resource.Designer.cs           + 78 (von Hand)
  WindowsFormsApplication1/Allgemein/Blazor/NamensDialogHuelle.cs   + 3 Einstiege
  WindowsFormsApplication1/MDIMainForm.cs                            1 Aufrufstelle
  WindowsFormsApplication1/Controller/StromspeicherKontextMenuCtrl.cs 1
  22 weitere Views mit zusammen 27 Namensabfragen
  WindowsFormsApplication1/Views/Gebäude/Form_EingGebTyp.cs           1
  WindowsFormsApplication1/Views/Kosten/ucErtragBonus.cs              1
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs      4
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.cs 1
  WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs             − 7 Einträge
  EPOS.UI.Tests/Dialoge/NamensDialogTests.cs                        + 7 Tests
  EPOS.UI.Tests/Standards/{Zahlenfeld,Ganzzahlfeld}Tests.cs         + je 2
  Werkzeuge/Formularkarte.Tests/{Stapel,Erreichbarkeit,FeldkarteSchreiber,ResxLeser,Pruefmuster}Tests.cs
  Werkzeuge/Formularkarte/{LIESMICH.md,Erreichbarkeit_2026-09-03.md}

GELÖSCHT
  11 Dateien der fünf WinForms-Masken (Regel M1) — Liste in § 6
  5 Dateien verschoben (Form_StromspeicherItemNeu → Prüfmuster)
```
