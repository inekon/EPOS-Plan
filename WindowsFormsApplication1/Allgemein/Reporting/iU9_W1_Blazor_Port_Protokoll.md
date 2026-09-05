# iU9 Welle 1 — Port der Kostenvorlagen-Kleindialoge und des Kapitalwert-Verlaufs (Umsetzungsprotokoll)

> Muster: [`B5b_Blazor_Port_Protokoll.md`](B5b_Blazor_Port_Protokoll.md) — Feldkarten-Abgleich je
> Maske, Abweichungsliste A‑n, Entscheidungen, Windows-Abnahmepunkte.
>
> Basis `aef9509` (Branch `ios_migration`), Arbeitsstand 03.09.2026.
> Plan: Wellenplan iU9, Abschnitt D.

---

## 1. Auftrag und Ergebnis

**Sieben WinForms-Masken → sechs Razor-Komponenten**, jede WinForms-Fassung im selben Schritt
gelöscht (Regel M1). Genau **ein** neuer Baustein (`Optionsgruppe`), **ein** neuer Kern-Controller
(`KostenfaktorCtrl`), **zwei** neue Hüllen mit Datenseite und **ein** wiederverwendbarer
Windows-Helfer (`NamensDialogHuelle`).

| # | Maske (Zeilen) | Komponente | Hülle | Aufrufer (Datei:Zeile nach dem Umbau) |
|---|---|---|---|---|
| W1.1 | `Form_VorlagenPosition` (89) | `EPOS.UI/Dialoge/Kosten/VorlagenPositionDialog.razor` | inline | `Views/Kosten/Form_KostenKomponente.cs:622` (`Zeile_EditorAngefordert`) |
| W1.2 | `Form_VariantenName` (41) + `Form_KostenItemNeu` (43) | `EPOS.UI/Dialoge/Allgemein/NamensDialog.razor` | `Allgemein/Blazor/NamensDialogHuelle.cs` | `Form_KostenKomponente.cs:801` (`NeueVariante`), `Views/Kosten/Form_Energietraeger.cs:319` |
| W1.3 | `Form_CaseEingabe` (301) | `EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor` | inline | `Form_KostenKomponente.cs:450` (`Zeile_WorstBestAngefordert`), `Views/Kosten/ucKostenItem.cs:162` (K6) |
| W1.4 | `Form_VorlagenUebernahme` (253) | `EPOS.UI/Dialoge/Kosten/VorlagenUebernahmeDialog.razor` | `Views/Kosten/VorlagenUebernahmeHuelle.cs` | `Form_KostenKomponente.cs:845` (`btnUebernahme_Click`) |
| W1.5 | `Form_KostenAdmin` (128) | `EPOS.UI/Dialoge/Kosten/KostenfaktorKatalogDialog.razor` | `Views/Kosten/KostenfaktorKatalogHuelle.cs` | `Form_KostenKomponente.cs:901` (`btnKatalog_Click`) |
| W1.6 | `Form_WirtschaftlichkeitVerlauf` (289) | `EPOS.UI/Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufDialog.razor` | `Views/Wirtschaftlichkeit/KapitalwertVerlaufHuelle.cs` | `Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs:491` (`btnVerlauf_Click`) |

**Commits** (ein Commit je Nummer, Reihenfolge des Plans):

```
9e4fa37  iU9-W1.0  Baustein Optionsgruppe
0d92c89  iU9-W1.1  VorlagenPositionDialog
e94978a  iU9-W1.2  NamensDialog + NamensDialogHuelle
f6e9264  iU9-W1.3  CaseEingabeDialog
584be20  iU9-W1.4  VorlagenUebernahmeDialog + Hülle
8c40854  iU9-W1.5  KostenfaktorKatalogDialog + Hülle + KostenfaktorCtrl
9a5df28  iU9-W1.6  KapitalwertVerlaufDialog + Hülle
e6a613e  iU9-W1.7  Ressourcen-Sammelnachtrag (43 Schlüssel, de + en)
21e399c  iU9-W1.8  Formularkarte-Tests (Prüfmuster, Zähler)
```

---

## 2. Bauweise

### 2.1 Der neue Baustein — `EPOS.UI/Bausteine/Optionsgruppe.razor`

45 `RadioButton` im Bestand; die Feldkarte wies sie bisher als „Auswahlfeld (Gruppe prüfen)" aus.
`<fieldset role="radiogroup">` mit `<legend>` als Gruppentitel, je Eintrag ein `<label>` um ein
`<input type="radio">` — die Beschriftung gehört damit zum Bedienelement, und die Zeile ist 44 px
hoch (M2). Alle Einträge einer Instanz teilen einen je Instanz eindeutigen Namen; sonst schalteten
zwei Gruppen auf derselben Seite einander um.

Über den Plan hinaus trägt der Baustein `Gesperrt` (einzelne Ids). Beide Masken der Welle brauchen
das: `rbQuelleVorlage.Enabled = _vorlagen.Count > 0` (Übernahme) und
`_rbProzent.Enabled = _daten.Betrag != 0` (Worst/Best Case). Eine gesperrte Option bleibt sichtbar
und leise grau — sie sagt, dass es die Möglichkeit gibt und dass sie hier gerade nicht offensteht.

### 2.2 Der neue Kern-Controller — `EPOS.Kern/Controller/KostenfaktorCtrl.cs`

Die drei SQL-Anweisungen aus `Form_KostenAdmin`, zeichengleich, mit `DbParam`-Platzhaltern:
`Alle()`, `Neu(bezeichnung)`, `Loeschen(stammId)`. Beide Schutzfilter aus Befund B4 (11.08.2026)
bleiben: gelesen und gelöscht wird nur, was `IsMainComponent = False` trägt. IDs entstehen per
MAX+1 (ADR-001).

### 2.3 Die Hüllen

*Inline* (Muster `Form_Heizkessel.CreateNewEnergyCarrier`): W1.1 und W1.3 — die Datenseite stand
schon vorher im Aufrufer, sie bleibt dort.

*Eigene Hülle* (Muster `BhkwWirtschaftlichkeitHuelle`): W1.4, W1.5, W1.6. Sie laden mit denselben
Controllern und denselben Filterparametern wie zuvor die Maske (Regel F4) und geben der Komponente
**Delegaten** statt Daten, wo die Antwort von der Auswahl abhängt:

| Hülle | Delegaten |
|---|---|
| `VorlagenUebernahmeHuelle` | `AnlagenZu` (Projektwechsel), `Vorschau` (Klartext **und** Knopfzustand — beides fällt aus denselben Zählungen), `Uebernehmen` |
| `KostenfaktorKatalogHuelle` | `NeuLaden`, `Neu`, `Loeschen`, `Rueckfrage` (→ `Dienste.Dialog.Frage`) |
| `KapitalwertVerlaufHuelle` | `Berechnen` (Jahre, Szenario, `CancellationToken`) → zwei PNG und zwei Textzeilen |

*Windows-Helfer*: `NamensDialogHuelle.Fragen(besitzer, titel, frage, vorbelegung, meldungLeer)` →
der getrimmte Name oder `null`. Er ist für W2 gedacht (28 Aufrufer von
`Form_StromspeicherItemNeu`) und schon hier von drei Stellen benutzt.

---

## 3. Feldkarten-Abgleich

Der Abgleich ist **als Test ausgeführt** (`EPOS.UI.Tests/Dialoge/*Tests.cs`), nicht als einmalige
Messung: Je Dialog prüft ein Test den Feldbestand, ein zweiter die Beschriftungen gegen die
Feldkarte. Fällt ein Feld weg, wird der Test rot. Die Karten wurden vor Beginn frisch gezogen
(`Werkzeuge/Formularkarte`, Stand `aef9509`).

| Maske | Soll (Feldkarte) | Ist (Komponente) | Deckung |
|---|---|---|---|
| `Form_VorlagenPosition` | 5 Felder + Kopftitel + OK/Abbrechen | Textfeld · Auswahlfeld · Schalter · 2 Zahlenfelder | **5/5** |
| `Form_VariantenName` | 1 Feld + Kopftitel + OK/Abbrechen | Textfeld | **1/1** |
| `Form_KostenItemNeu` | 1 Feld + OK/Abbrechen | dasselbe Textfeld (eine Komponente für beide) | **1/1** |
| `Form_CaseEingabe` | 4 Drehfelder + 2 Gruppentexte | 4 Zahlenfelder + 2 `Gruppenkopf` — **plus 4 Laufzeitfelder** (siehe unten) | **4/4 + 4** |
| `Form_VorlagenUebernahme` | 10 Zeilen + Kopftitel | Kontextzeile · Auswahlfeld Ziel · **Optionsgruppe** · 3 Auswahlfelder · Vorschauzeile · Übernehmen/Abbrechen | **10/10** |
| `Form_KostenAdmin` | 5 Zeilen (Liste, Neu, Löschen, OK, Einleitung) | `Raster` mit Wahlspalte · Anlegezeile · Löschen · OK · Herleitungszeile | **5/5** + Anlegefeld (A‑13) |
| `Form_WirtschaftlichkeitVerlauf` | 8 Zeilen | Zahlenfeld · Auswahlfeld · Aktualisieren · 2 `ChartBild` · Restwertzeile · Statuszeile · Schließen | **7/8** (A‑17: ProgressBar) |

**Die vier Laufzeitfelder von `Form_CaseEingabe`** stehen in keiner Feldkarte — der Designer kennt
sie nicht, sie entstanden in der `.cs` (Regel F1 verlangt, sie von Hand nachzutragen):

| Feld im Vorläufer | Herkunft | Ziel in der Komponente |
|---|---|---|
| `rbCaseAbsolut` / `rbCaseProzent` | KD6 § 11 | `Optionsgruppe` mit Einzelsperre |
| `lblCaseUmrechnung` | KD6 § 11 | `Herleitungszeile`, nur im %-Modus |
| `lblStartJahr` + `numStartJahr` | KD6 § 11, FK10 | `Ganzzahlfeld` 0…50 |
| `chkZuschuss` + `lblZuschussHinweis` | K5 § 7.4 | `Schalter` + `Herleitungszeile`, nur wenn der Aufrufer sie anbietet |

**Kein Feld einer Karte fehlt.** Die einzige Auslassung ist die `ProgressBar` (A‑17).

---

## 4. Abweichungen (mit Begründung)

| # | Abweichung | Begründung |
|---|---|---|
| **A‑1** | W1.1: Eine leere Bezeichnung **meldet** sich, statt still den alten Namen zurückzuholen | `btnOk_Click` schrieb den Namen nur, wenn er nicht leer war (`if (name.Length > 0)`). Ein geleertes Feld holte damit kommentarlos den alten Namen zurück — das sieht wie ein Fehler aus. Neuer Schlüssel `VPOS_MSG_NAME_FEHLT` |
| **A‑2** | W1.1: Der Dialog hat einen **Infoknopf**, den die Maske nicht hatte | Alle übrigen Kostendialoge haben einen; er zeigt dieselbe Wikiseite. Neue Zeile `Form_VorlagenPosition.btn_Help = Kosten` in `help_mapping.txt` |
| **A‑3** | W1.2: Erstfokus im **Textfeld** statt auf dem Wurzel-`div` (A‑7 aus B5b); `txtName.SelectAll()` ist **nicht** nachgebildet | Bei einem einzigen Feld gibt es nichts, was der Fokus sonst erklären könnte — die Masken taten es ebenso. Text markieren braucht JS-Interop, und `EPOS.UI` hat (Stand iU9) keine JS-Schicht. Der Vorschlag steht sichtbar im Feld |
| **A‑4** | W1.2: Der Anlegedialog erscheint **mittig** statt an der Knopfposition | `Form_KostenAdmin` setzte `frmLabel.Location = PointToScreen(btnNeuKostenfaktor.Location)`. Die Blazor-Hülle ist seit iU8 durchgängig mittig über dem Besitzerfenster; ein Sonderweg für einen Dialog wäre die zweite Regel |
| **A‑5** | W1.2: `NamensDialog` hat **keinen** Infoknopf | Er ist je nach Aufrufer eine andere Maske; ein Knopf, der immer dieselbe Wikiseite zeigt, wäre in vier von fünf Fällen falsch |
| **A‑6** | W1.3: `Form_KostenKomponente` bietet den **Zuschuss-Schalter nicht mehr** an | **Befund.** Die Maske zeigte ihn dort (der Konstruktor bekam eine frische `KostenPosition` mit leerer Kostenart, und eine leere Kostenart zählt als Investition), aber der Aufrufer las `daten.IstZuschuss` **nie zurück** — er schreibt über `KostenProjektPositionenCtrl.Zeile`, die die Größe nicht führt. Ein Haken ohne Wirkung ist eine Behauptung, die nicht stimmt. In `ucKostenItem` gilt die volle Regel unverändert |
| **A‑7** | W1.3: Absolutmodus 0…99 999, Prozentmodus ±1000 | Der Vorläufer setzte beim **Zurück**schalten `Maximum = 100 000 000` — einen Bereich, den er beim Öffnen nie hatte; ein Betrag über 99 999 riss `NumericUpDown.Value` in eine Ausnahme. Die Blazor-Fassung zeigt einen solchen Wert und färbt erst bei einer Eingabe außerhalb des Bereichs |
| **A‑8** | Alle: Eine ungültige Zahl **färbt** das Feld, statt zu melden | Hausregel `EPOS.UI/CLAUDE.md`; übernommen aus B5b (dort A‑8). Ersetzt die MessageBox von `Program.ZahlPruefen` |
| **A‑9** | W1.3: Neue **Herleitungszeile für Erlöspositionen** | Der Vorläufer bekam `IstErloes` übergeben und sagte dazu nichts; das Vorzeichen blieb unerklärt. Neuer Schlüssel `KCASE_ERLOES_HINWEIS` |
| **A‑10** | W1.4: Der Dialog bleibt nach einer erfolgreichen Übernahme **offen** und zeigt die Meldungen des Controllers als Hinweisbanner | Der Vorläufer zeigte eine MessageBox und schloss unmittelbar danach — wer sie wegklickte, sah nicht mehr, was übernommen wurde. Die Vorschau wird neu gezogen, der Knopf sperrt sich damit von selbst. Geschlossen wird mit „Abbrechen"; das tut dasselbe wie vorher (die Übernahme ist geschrieben, Abbrechen hat sie nie zurückgenommen) |
| **A‑11** | W1.4: Die drei Quelllisten tragen eigene **Beschriftungen** | In WinForms standen sie ohne Beschriftung rechts neben den Optionsknöpfen; untereinander gestellt wären sie nicht mehr zuzuordnen. Neue Schlüssel `KUEB_LBL_*` |
| **A‑12** | W1.4: Ohne Vorlage im Katalog sind die **Projektlisten bedienbar** | **Befund.** Der Vorläufer setzte in diesem Fall `rbQuelleProjekt.Checked = true`, während `_fuellt` noch `true` war — `Auswahl_Geaendert` kehrte sofort um, und die Listen blieben im Designer-Zustand „gesperrt". Die Quelle „Projekt" war gewählt, aber nicht bedienbar |
| **A‑13** | W1.5: Das Anlegen braucht **keinen Unterdialog** mehr | `Form_KostenAdmin` öffnete dafür `Form_KostenItemNeu` — ein zweites Fenster für ein Textfeld. Ein zweites Blazor-Fenster über dem ersten wäre eine zweite WebView (Risiko R2). Feld und Knopf stehen jetzt über der Liste |
| **A‑14** | W1.5: Die Liste hat eine **Wahlspalte** (○/●) | Eine `ListView` markiert die gewählte Zeile selbst, ein `Raster` (QuickGrid) nicht — dasselbe Muster wie A‑6 aus B5b, mit `aria-pressed` |
| **A‑15** | W1.5: Gelöscht wird über die **StammID**, nicht über die Bezeichnung | Die `ListView` führte nur den Text und konnte gar nicht anders; bei zwei gleichnamigen Sätzen traf der Löschbefehl beide. Der Schutzfilter `IsMainComponent = False` bleibt unverändert |
| **A‑16** | W1.5: Die MessageBox „konnte nicht angelegt werden" wird ein **Warnbanner**; die Rückfrage vor dem Löschen bleibt ein echter modaler Ja/Nein-Dialog | `EPOS.UI` kennt keine MessageBox. Für die Rückfrage gibt es bis Welle 4 keinen Baustein (Lücke 8) — die Hülle reicht sie mit demselben Text an `Dienste.Dialog.Frage` weiter |
| **A‑17** | W1.6: Die **ProgressBar entfällt**; der Sammler bekommt `null` als `IProgress`-Melder | Ein Fortschrittsbaustein entsteht erst in Welle 11 (Lücke 13). Das Sperrwerk von `SetBusy` bleibt vollständig: Eingaben gesperrt, Statuszeile „Berechnung läuft …", Schließen-Knopf heißt „Abbrechen" |
| **A‑18** | W1.6: Nach einem Abbruch **bleibt der Dialog stehen** | Der Vorläufer schloss sich selbst (`_schliessenNachAbbruch`), weil `FormClosing` den Abbruch erzwingen musste; ein versehentliches Abbrechen nahm damit zugleich das Fenster |
| **A‑19** | W1.6: Der Rechenfehler erscheint als **Warnbanner** statt als MessageBox | Hausregel, wie A‑9 aus B5b |

**Enter** ist in den drei schreibenden Dialogen (W1.4, W1.5, W1.6) **nicht** belegt — dieselbe
Begründung wie A‑7 aus B5b: Wo ein Knopf sofort schreibt, ist ein versehentliches Enter kein
Bestätigen, sondern ein Zufall. In den drei reinen OK-Dialogen (W1.1, W1.2, W1.3) bestätigt Enter
wie bisher. **Esc** schließt überall.

---

## 5. Texte

**43 neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` und — von Hand,
weil hier kein Visual Studio läuft — `Resource.Designer.cs` (alphabetisch zwischen den Nachbarn,
im Muster der erzeugten Datei; die Änderung ist in allen drei Dateien rein additiv):

| Präfix | Zahl | Dialog |
|---|---|---|
| `VPOS_*` | 7 | Zeileneditor der Vorlagenposition |
| `NAMD_*` | 1 | Namensabfrage |
| `KCASE_*` | 8 | Worst/Best Case |
| `KUEB_*` | 5 | Übernahme ins Projekt |
| `KFAK_*` | 11 | Katalog der Kostenfaktoren |
| `WVERL_*` | 11 | Kapitalwert-Verlauf |

**Wiederverwendet statt neu angelegt:** `KDLG_MSG_NEU_TITEL`, `KDLG_MSG_KOPIE_TITEL`,
`KDLG_MSG_NEU_NAME`, `KDLG_ET_NEU_TITEL/_NAME/_VORGABE`, `KDLG_KAT_INVEST`, `KDLG_KAT_BETRIEB`,
`KDLG_UEB_QUELLE_VORLAGE/_PROJEKT/_LOSE`, `KDLG_UEB_VORSCHAU`, `KDLG_ET_BTN_UEBERNEHMEN`,
`KOSTEN_CASE_ABSOLUT/_PROZENT/_UMRECHNUNG/_STARTJAHR`, `KOSTEN_CHK_ZUSCHUSS/_HINT`, `KOSTENART_*`,
`ALLG_BTN_OK/_ABBRECHEN`.

**Zugriff** über `Resource.ResourceManager.GetString` mit deutschem Rückfall im Code (B5b‑O4) — die
Hülle setzt die Texte, die Komponente trägt den deutschen Literaltext als Parametervorgabe. Damit
läuft jeder Dialog auch dann richtig, wenn ein Schlüssel fehlt.

**Befund zur Plananweisung:** Eine `Form_KostenItemNeu.en-US.resx` gibt es im Bestand **nicht**. Die
Maske ist zwar lokalisiert (`ApplyResources`), führt aber nur die neutrale (deutsche) `.resx`
(„Bezeichner", „Bezeichner eingeben") und eine `.de-DE.resx` mit Vorlagenresten. Es ging also keine
Übersetzung verloren; der englische Text entsteht jetzt als `KFAK_LBL_NEU` = „Identifier".

**`help_mapping.txt`:** Die vier vorhandenen Zeilen bleiben unverändert — die Komponenten tragen
denselben `HilfeSchluessel` wie die Masken (`Form_CaseEingabe.btn_Help`,
`Form_KostenAdmin.btn_Help`, `Form_VorlagenUebernahme.btn_Help`,
`Form_WirtschaftlichkeitVerlauf.btn_Help`). Neu ist nur `Form_VorlagenPosition.btn_Help` (A‑2).
Dass die Schlüssel weiter die alten Maskennamen tragen, ist dasselbe Vorgehen wie bei
`Form_Kosten_Auswahl.btn_Help` seit iU8‑9: Der Schlüssel benennt die Wikiseite, nicht die Klasse.

---

## 6. WinForms-Seite

**Gelöscht** (17 Dateien):

```
Views/Kosten/Form_VorlagenPosition.{cs,Designer.cs}
Views/Kosten/Form_VariantenName.{cs,Designer.cs}
Views/Kosten/Form_KostenItemNeu.{cs,Designer.cs,resx,de-DE.resx}
Views/Kosten/Form_CaseEingabe.{cs,Designer.cs,resx}
Views/Kosten/Form_VorlagenUebernahme.{cs,Designer.cs}
Views/Kosten/Form_KostenAdmin.{cs,Designer.cs,resx}
Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitVerlauf.{cs,Designer.cs}
```

**`Allgemein/KI/HilfeKontext.cs`:** die sieben Einträge der gelöschten Masken entfernt — jeweils im
Commit ihrer Maske (Regel F10).

**Restfundstellen der alten Namen** sind ausschließlich (a) `HilfeSchluessel`-Zeichenketten
(`"Form_X.btn_Help"` — Schlüssel des Hilfekatalogs, siehe § 5), (b) Kommentare, die die Herkunft
nennen, und (c) das Prüfmuster der Formularkarte. **Keine Typverwendung** ist übrig:

```
git grep -nE "(new|typeof|:)\s*Form_(VorlagenPosition|VariantenName|KostenItemNeu|CaseEingabe|VorlagenUebernahme|KostenAdmin|WirtschaftlichkeitVerlauf)\b" -- '*.cs' '*.razor'
→ 0 Treffer
```

---

## 7. Nachweise

### 7.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental
→ 0 Fehler, 30 Warnungen
```

Basis (`aef9509`): 34 Warnungen. **WFO1000 sinkt von 30 auf 24** — die sechs Fundstellen der
gelöschten Masken sind weg; der Rest ist unverändert (4 × CS0108, 4 × CS0109, 2 × WFO0003,
2 × CA2255).

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ EPOS.Kern.Tests     35 grün
  KiKern.Tests       450 grün
  SpeicherEngine.Tests 337 grün
  EPOS.UI.Tests      202 grün   (107 vorher, 95 neu)
  ────────────────────────────
  1024 grün, 0 rot   (929 vorher)
```

Die 95 neuen bunit-Tests je Dialog:

| Datei | Tests | Prüft |
|---|---|---|
| `Bausteine/OptionsgruppeTests.cs` | 10 | Feldbestand, Legende, `role`, genau eine Wahl, Meldung, gemeinsamer/eindeutiger Name, `Aktiv`, Einzelsperre |
| `Dialoge/VorlagenPositionDialogTests.cs` | 12 | Feldbestand, Beschriftungen, Liste von außen, Vorbelegung, Rückfall „sonstige", Ergebnis, leere Empfehlung, Färbung, leere Bezeichnung, Abbrechen, Enter/Esc, Hilfeschlüssel |
| `Dialoge/NamensDialogTests.cs` | 11 | Feldbestand, Titel/Frage vom Aufrufer, Vorbelegung, getrimmtes Ergebnis, leerer Name hält offen, Meldung mit/ohne Text, Meldung verschwindet, Abbrechen, Enter/Esc, kein Infoknopf |
| `Dialoge/CaseEingabeDialogTests.cs` | 20 | Feldbestand (Karte **und** Laufzeitfelder), Beschriftungen, Vorbelegung, Start absolut, Sperre ohne Erwartungswert, Umschalten hin/zurück, Null bleibt Null, Umrechnungszeile, Ergebnis in beiden Modi, Startjahrregel (4 Fälle), Zuschuss mit/ohne Schalter, Erlöszeile, Abbrechen/Esc, Hilfeschlüssel |
| `Dialoge/VorlagenUebernahmeDialogTests.cs` | 16 | Feldbestand, Beschriftungen, Sperrwerk beidseitig, Fall ohne Vorlage, festes Ziel, Vorschau beim Aufbau und nach jeder Änderung, Anlagenliste beim Projektwechsel, Knopfregel, Wahl an die Hülle, Hinweis-/Fehlerbanner, Rückgabe, Esc/Enter, Hilfeschlüssel |
| `Dialoge/KostenfaktorKatalogDialogTests.cs` | 13 | Feldbestand, Liste, Löschsperre, Wahlspalte, leerer Name, Anlegen samt Neuladen, Fehlerbanner, Rückfrage mit Namen, Nein löscht nicht, Löschen meldet die Id, ohne Rückfragedelegat, OK/Esc/Enter, Hilfeschlüssel |
| `Dialoge/KapitalwertVerlaufDialogTests.cs` | 13 | Feldbestand, Beschriftungen, Vorgabe und Klemmung, Rechnen beim Öffnen, Bilder als `data:`-URL, Restwert-/Statuszeile, Aktualisieren, Sperrwerk, Abbrechen ohne Schließen, Fehlerbanner, Schließen/Esc/Enter, Hilfeschlüssel |

Die drei Tests mit Zahlen in der Anzeige (`CaseEingabeDialogTests`) pinnen `de-DE` wie
`SpeichernLeisteTests` — die CI-Läufer laufen englisch.

### 7.3 Formularkarte

```
dotnet build Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 0 Fehler, 0 Warnungen
dotnet test  Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 119 grün (117 vorher)
```

Drei Tests hingen an gelöschten Masken (Risiko R8). `Form_CaseEingabe` hat ein **Prüfmuster**
bekommen (`Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/`, Stand `f6e9264^`) — ihre vier
Drehfelder mit `Maximum` sind der einzige Beleg für die Bereichsspalte einer `NumericUpDown`. Die
beiden Stapellauf-Schranken stehen jetzt auf 113 Dateien und 111 Masken. `PruefmusterTests` führt
die Muster als `Theory`; daher 119 statt 117.

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -c Release -- --alle WindowsFormsApplication1 --erreichbarkeit
```

| Kennzahl | vor W1 | nach W1 |
|---|---|---|
| Designer-Dateien | 119 | **112** |
| davon Masken | 118 | **111** |
| lokalisiert | 63 | **62** |
| Kartenzeilen | 2 369 | 2 322 |
| Felder ohne Beschriftung | 178 | 172 |
| Öffner erreichbar | 111 | 104 |

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1333 SQL-Texte geprüft: 0 Fundstellen, 149 dynamisch, 1184 in Ordnung
```

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

`diff -rq` gegen die Basis meldet für diese drei Ordner **keinen** Unterschied; repoweit bleibt nur
`protokoll.txt` bzw. `lauf_protokoll.md` (die Laufprotokolle) und die zehn nicht gerechneten
Projekte. Der Kern wurde in dieser Welle einmal angefasst (`KostenfaktorCtrl`, W1.5) — er liegt
nicht im Rechenweg, der Nachweis bestätigt das.

### 7.7 Veröffentlichung

```
dotnet publish WindowsFormsApplication1 -c Release -r win-x64 --self-contained -p:Platform=x64 -o artifacts/publish-probe
```

`wwwroot` vollständig: `index.html`, `_framework/blazor.webview.js`, `_framework/blazor.modules.json`,
`_content/EPOS.UI/{epos-ui.css,help_icon.png}` (samt `.br`/`.gz`),
`_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js`. Die drei neuen
CSS-Klassen (`epos-optionsgruppe`, `epos-neuzeile`, `epos-kontextzeile`) sind in der
ausgelieferten `epos-ui.css` enthalten.

---

## 8. Grenzen

* **Keine Windows-Sicht.** Alles hier ist auf Linux gemessen: Build, Tests, Referenzlauf,
  Veröffentlichung. Ob die Dialoge in der WebView2 richtig aussehen, sagt erst die Abnahme (§ 9).
* **Der K6-Aufrufer `ucKostenItem` bleibt.** Er hängt an `Form_Kosten`, das seit KD6a keinen
  Einstieg mehr hat; nach dem Anwenderentscheid iF29 wird die Maske stillgelegt (Welle 0). Bis
  dahin ist er funktionsgleich umgestellt und der einzige Aufrufer, der das Zuschuss-Kennzeichen
  wirklich zurückliest.
* **Die Rückfrage vor dem Löschen ist noch ein WinForms-Fenster** (A‑16). Auf iOS gibt es sie
  nicht, bis Welle 4 den Baustein `Rueckfrage` bringt.
* **Kein Fortschritt im Verlaufsdialog** (A‑17), bis Welle 11 den Baustein `Fortschritt` bringt.

---

## 9. Abnahmeliste Windows (iZ5) für diese sieben Dialoge

Weg zu den Dialogen: **Menü → Kostenvorlagen → Kostenverwaltung** (`Form_KostenKomponente`) für
W1.1–W1.5; **Berichte & Kosten → Wirtschaftlichkeit → Verlauf** für W1.6; **Menü → Energieträger**,
Knopf „Neu…" für den zweiten Aufrufer von W1.2.

| # | Punkt | W1.1 | W1.2 | W1.3 | W1.4 | W1.5 | W1.6 |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|
| 1 | Öffnet mittig, kein weißes Aufblitzen | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| 2 | Fenster ziehbar **und** maximierbar | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| 3 | Tabellen ohne Umbruch (Befund 03.09.) | – | – | – | – | ☐ | – |
| 4 | Deutsch **und** Englisch (`HKCU\Software\wp-plan\Language`) | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| 5 | Hochkontrast: Warnbanner und Fehleingabe bleiben unterscheidbar | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| 6 | 125 % und 150 % scharf (DPI-Insel greift) | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| 7 | Maus **und** Finger (44 px), Optionsgruppe mit den Pfeiltasten | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| 8 | Tab-Zyklus bleibt im Dialog, Esc schließt | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| 9 | Infoknopf zeigt die Wikiseite „Kosten" bzw. „Wirtschaftlichkeit" | ☐ | – | ☐ | ☐ | ☐ | ☐ |

**Fachliche Proben:**

| # | Probe |
|---|---|
| F‑1 | W1.1: Position ✏️ öffnen, Kostenart wechseln, Erlös setzen, Empfehlung leeren → nach OK steht alles im Raster; Empfehlung bleibt leer (nicht 0) |
| F‑2 | W1.2: „Neue Variante" schlägt „‹Komponente› — Variante n" vor; leerer Name meldet sich und hält offen; Esc verwirft |
| F‑3 | W1.3: „+/−" mit Betrag ≠ 0 → „in %" wählbar, Umrechnungszeile stimmt, OK schreibt **Beträge**; mit Betrag 0 bleibt „in %" gesperrt |
| F‑4 | W1.3: Startjahr 1 wird zu 0, Startjahr 5 bleibt 5 |
| F‑5 | W1.4: Im Projektmodus ist das Ziel gesperrt; Quelle „Projekt/Anlage" schaltet die beiden Listen frei; Vorschau nennt Quell- und Zielzahl; nach „Übernehmen" steht die Meldung im Dialog und die Vorschau zählt neu |
| F‑6 | W1.5: „Neu" mit leerem Feld tut nichts; ein Name legt an und markiert den neuen Satz; „Löschen" fragt mit Namen und löscht **genau** den markierten Satz; eine Hauptkomponente lässt sich nicht löschen |
| F‑7 | W1.6: Der Dialog rechnet beim Öffnen; „Aktualisieren" mit anderem Zeitraum/Szenario zeichnet neu; „Abbrechen" hält die Rechnung an und das Fenster offen; nach dem Schließen frischt die Wirtschaftlichkeitsseite auf, wenn neu simuliert wurde |

---

## 10. Offene Punkte

| # | Punkt |
|---|---|
| **W1‑O1** | **A‑6 dem Anwender vorlegen:** Der Zuschuss-Schalter ist im Projektraster ersatzlos weg, weil sein Wert dort nie gespeichert wurde. Soll `KostenProjektPositionenCtrl.Zeile` die Größe künftig führen (dann kommt der Schalter zurück und wirkt), oder bleibt Zuschuss eine Sache des Investitionsreiters? |
| **W1‑O2** | **A‑10 sichtprüfen:** Bleibt der Übernahme-Dialog nach dem Lauf offen stehen, oder erwartet der Anwender das alte „MessageBox und zu"? |
| **W1‑O3** | **A‑17:** Der Verlaufsdialog rechnet ohne Fortschrittsanzeige. Bei großen Projekten dauert das erste Sammeln spürbar — reicht die Statuszeile bis Welle 11? |
| **W1‑O4** | **A‑3:** `SelectAll()` in der Namensabfrage braucht eine JS-Schicht in `EPOS.UI`. Lohnt sie sich für diesen einen Fall, oder wartet sie auf den ersten echten Bedarf? |
| **W1‑O5** | `KostenfaktorCtrl.Loeschen` löscht über die `StammID` (A‑15). Die Alt-Datenbank kann gleichnamige Kostenfaktoren führen — sie sind jetzt einzeln löschbar; ob das gewollt ist, sagt der Anwender |
| **W1‑O6** | Die Szenarioliste des Verlaufsdialogs zeigt weiter die **Persistenzwerte** als Anzeigetext (unverändert zum Vorläufer). Ein Verstoß gegen die Drei-Schichten-Regel, den erst eine Übersetzung heilt — Kandidat für den nächsten Sammelnachtrag |
| **W1‑O7** | `NamensDialogHuelle` ist für die 28 Aufrufer von `Form_StromspeicherItemNeu` gebaut (Welle 2). Vor der Umstellung prüfen, ob dort ein Titel je Aufrufer nötig ist |

---

## 11. Geänderte und neue Dateien

```
NEU
  EPOS.UI/Bausteine/Optionsgruppe.razor                                        86 Zeilen
  EPOS.UI/Dialoge/Kosten/VorlagenPositionDialog.razor                         213
  EPOS.UI/Dialoge/Kosten/VorlagenPositionErgebnis.cs                           28
  EPOS.UI/Dialoge/Allgemein/NamensDialog.razor                                135
  EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor                              310
  EPOS.UI/Dialoge/Kosten/CaseEingabeErgebnis.cs                                30
  EPOS.UI/Dialoge/Kosten/VorlagenUebernahmeDialog.razor                       340
  EPOS.UI/Dialoge/Kosten/VorlagenUebernahmeDaten.cs                            45
  EPOS.UI/Dialoge/Kosten/KostenfaktorKatalogDialog.razor                      230
  EPOS.UI/Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufDialog.razor           250
  EPOS.UI/Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufBilder.cs               26
  EPOS.Kern/Controller/KostenfaktorCtrl.cs                                    105
  WindowsFormsApplication1/Allgemein/Blazor/NamensDialogHuelle.cs              75
  WindowsFormsApplication1/Views/Kosten/VorlagenUebernahmeHuelle.cs           230
  WindowsFormsApplication1/Views/Kosten/KostenfaktorKatalogHuelle.cs           95
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/KapitalwertVerlaufHuelle.cs 215
  EPOS.UI.Tests/Bausteine/OptionsgruppeTests.cs                               137  (10 Tests)
  EPOS.UI.Tests/Dialoge/VorlagenPositionDialogTests.cs                        215  (12)
  EPOS.UI.Tests/Dialoge/NamensDialogTests.cs                                  155  (11)
  EPOS.UI.Tests/Dialoge/CaseEingabeDialogTests.cs                             295  (20)
  EPOS.UI.Tests/Dialoge/VorlagenUebernahmeDialogTests.cs                      275  (16)
  EPOS.UI.Tests/Dialoge/KostenfaktorKatalogDialogTests.cs                     230  (13)
  EPOS.UI.Tests/Dialoge/KapitalwertVerlaufDialogTests.cs                      230  (13)
  Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/Form_CaseEingabe.{cs,Designer.cs,resx}
  WindowsFormsApplication1/Allgemein/Reporting/iU9_W1_Blazor_Port_Protokoll.md  dieses Protokoll

GEÄNDERT
  EPOS.UI/wwwroot/epos-ui.css                        + Optionsgruppe, Neuzeile, Kontextzeile
  EPOS.Kern/MyResource/Resource.resx                 + 43 Schlüssel
  EPOS.Kern/MyResource/Resource.en-US.resx           + 43
  EPOS.Kern/MyResource/Resource.Designer.cs          + 43 (von Hand)
  WindowsFormsApplication1/Views/Kosten/Form_KostenKomponente.cs     5 Aufrufstellen
  WindowsFormsApplication1/Views/Kosten/Form_Energietraeger.cs       1
  WindowsFormsApplication1/Views/Kosten/ucKostenItem.cs              1 (K6)
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs  1
  WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs             − 7 Einträge
  WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt         + 1 Zeile
  Werkzeuge/Formularkarte.Tests/{FeldkarteSchreiberTests,StapelTests,PruefmusterTests}.cs
  Werkzeuge/Formularkarte/LIESMICH.md                Zähler und zweites Prüfmuster

GELÖSCHT
  17 Dateien der sieben WinForms-Masken (Regel M1) — Liste in § 6
```

---

# Nachtrag W0 — Stilllegung nach Anwenderentscheid iF29 (03.09.2026)

> Basis `908926a` (Branch `ios_migration`). Plan: Wellenplan iU9, Abschnitt B.
> Dieser Nachtrag steht hier und nicht in einem eigenen Protokoll, weil W0 dieselbe
> Werkzeugkette und dieselben Prüfmuster berührt wie Welle 1.

## W0.1 Auftrag

Der Erreichbarkeitsgraph (Paket iU8‑12f) hatte vier unerreichbare und eine verwaiste Maske
gemeldet. **iF29** entscheidet: Sie werden **nicht** umgestellt, sondern stillgelegt — dazu drei
Masken, die nur an ihnen hingen, und `Form_KwkgModule`, deren Knopf seit B5b ausgeblendet ist und
deren Felder vollständig im `BhkwWirtschaftlichkeitDialog` stehen.

| Commit | Inhalt |
|---|---|
| `bb0474c` | **W0.1** Statics gerettet: `EPOS.Kern/Controller/KostenSummenCtrl.cs` und `EPOS.Kern/Model/EnergietraegerModel.cs` |
| `16b106a` | **W0.2** 25 Dateien / 10 625 Zeilen gelöscht, Aufrufer und Ablagen angepasst |
| `43452a7` | **W0.3** Formularkarte-Tests, drittes Prüfmuster, Befundliste neu gezogen |

## W0.2 Gelöscht

| Maske | Warum | Nachfolge |
|---|---|---|
| `Views/Kosten/Form_Kosten.{cs,Designer.cs,resx}` | seit KD6a ohne Einstieg | `Views/BerichteKosten/UcBkKosten.cs` (Seite) und `Views/Kosten/Form_KostenKomponente.cs` (Dialog) |
| `Views/Kosten/Form_KostenfaktorItem.{cs,Designer.cs,resx}` | hing allein an `Form_Kosten.AddKostenItem` | — (Prüfmuster erhalten) |
| `Views/Kosten/ucKostenItem.{cs,Designer.cs,resx}` (Klasse `ucKostenZeile`) | hing allein an `Form_Kosten.UpdateDetailPanel` | `ucVorlagenZeile` |
| `Views/Kosten/Form_Betriebskosten.cs` (K4) | einziger Öffner war `Form_Kosten` | `BetriebskostenCtrl` + `UcBkKosten` |
| `Views/Varianten/Form_Variantentest.{cs,Designer.cs,resx}` | `btn_Varianten` war entfernt | `Views/BerichteKosten/UcBkUebersicht.cs` |
| `Views/Wirtschaftlichkeit/Form_Wirtschaftlichkeit.cs` (K4) | einziger Öffner war `Form_Variantentest` | `UcWirtschaftlichkeit` im Reiter |
| `Views/Bericht/Form_Bericht.cs` (K4) | dito | `UcBericht` im Reiter |
| `Views/Simulation/Form_Simulation_Kurz.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}` | verwaist, unter `Compile Remove` | `Form_Simulation_Detail` |
| `Views/Simulation/Form_Simulation_Detail - Kopie.cs` | dito | — |
| `Allgemein/GrafikTools/ChartManagerNeu.cs` | dito | `ChartManager` |
| `Views/Wirtschaftlichkeit/Form_KwkgModule.{cs,Designer.cs,resx}` | Knopf seit B5b ausgeblendet | `BhkwWirtschaftlichkeitDialog`, Gruppe 1b/2 |

**Gerettet in den Kern** (Rümpfe wörtlich, `DbParam`, SQLite-Dialekt):
`KostenSummenCtrl.KATEGORIE_INVESTITION`/`KATEGORIE_BETRIEB`, `GetAllCarriers`,
`LiesKomponentenSummen`, `LiesAnlagenSummen`; dazu `EnergyCarrier` und `EnergyConversion` als
`EPOS.Kern/Model/EnergietraegerModel.cs`. Umgestellte Aufrufer: `EnergietraegerKatalogCtrl`,
`Form_Energietraeger`, `VorlagenUebernahmeHuelle`, `Form_KostenKomponente`, `Wizard_WPItem`,
`Form_PhotovoltaikVerguetung`, `UcBkKosten`.

**Mitgezogen:** die zwei Altknöpfe der Startseite (`btn_Kosten`, `btn_Varianten`) samt Handlern,
Designer-Feldern und 24 `.resx`-Einträgen; der Modul-Knopf der Wirtschaftlichkeitsparameter; die
`Compile Remove`-Liste der `.csproj`; sieben `HilfeKontext`-Einträge; neun Zeilen
`help_mapping.txt`; `AlsDialog` in `UcBericht`/`UcWirtschaftlichkeit` (nur die gelöschten Hüllen
setzten es); 26 Kommentar- und cref-Verweise.

## W0.3 Zähler

| Kennzahl | nach W1 | nach W0 |
|---|---:|---:|
| Designer-Dateien (Repo) | 114 | **108** |
| Masken | 111 | **105** |
| lokalisiert | 62 | **61** |
| Kartenzeilen | 2 322 | **2 231** |
| Felder ohne Beschriftung | 172 | **168** |
| Öffner erreichbar („ja") | 104 | **103** |
| unerreichbar („nein") | 4 | **0** |
| verwaist | 1 | **0** |
| unklar | 2 | **2** |

## W0.4 Nachweise

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` | 0 Fehler, **28** Warnungen (30 vorher) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **1 036** grün (35 + 450 + 337 + 214) |
| `dotnet build`/`test Werkzeuge/Formularkarte/Formularkarte.sln -c Release` | 0/0 Warnungen, **119** Tests grün |
| Stapellauf `--alle WindowsFormsApplication1 --erreichbarkeit` | **105** Masken, 0 nein, 0 verwaist |
| `python3 Werkzeuge/SqlDialektPruefer/pruefer.py` | 1 303 SQL-Texte, **0** Fundstellen |
| `EPOS.Referenzlauf lauf`/`vergleich` 1030, 1007, 1017 gegen `2026-08-30_B3-Kaskade` | **PASS/PASS/PASS** (815 043 Werte); `diff -rq` ohne Unterschied |
| `dotnet publish -c Release -p:Platform=x64` | `wwwroot/` vollständig (`_framework`, `_content/EPOS.UI`, `index.html`) |

## W0.5 Abnahmeliste Windows

Alles Obige ist auf Linux gemessen. Am Windows-Rechner nachzuziehen:

| # | Punkt | Erwartung | ✓ |
|---|---|---|:--:|
| W0‑1 | Startseite, Reiter „Berichte & Kosten" | Die zwei Altknöpfe „Kosten" und „Varianten" sind **weg** — nicht nur unsichtbar; die vierseitige Navigation steht wie bisher | ☐ |
| W0‑2 | Berichte & Kosten → Kosten | Kacheln, Komponententabelle und Trägerliste zeigen dieselben Zahlen wie vor W0; „Kostenverwaltung öffnen…" führt in `Form_KostenKomponente` | ☐ |
| W0‑3 | Wirtschaftlichkeit → Parameter | Der Knopf „⚙ Werte je BHKW-Modul…" ist weg; die Felder stehen im Dialog „BHKW-Wirtschaftlichkeit" | ☐ |
| W0‑4 | Hilfe | Infoknopf auf der Kostenseite, im Kostendialog und im Energieträgerdialog zeigt weiter die Wikiseite „Kosten"; der KI-Assistent nennt für diese Masken denselben Bereich | ☐ |
| W0‑5 | Menü → Projekte → Projektdetail | `FormMain` und `Form_StromTest` bleiben erreichbar (Anwenderentscheid iF29) | ☐ |

## W0.6 Offene Punkte

| # | Punkt |
|---|---|
| **W0‑O1** | `EPOS.iOS/Dienste/IosProjektQuelle.cs` nennt in einer Meldung noch `Views/Kosten/Form_Kosten.CreateNewEnergyCarrier`. Die Datei gehört zur iOS-Hülle und wird dort beim nächsten Anfassen nachgezogen. |
| **W0‑O2** | Die Erkennung „Knopf wird zur Laufzeit entfernt" (`EntferneAltknopf`) im Erreichbarkeitswerkzeug hat seit W0 keinen Beleg mehr im Bestand. Sie bleibt stehen; ein Prüfmuster dafür lohnt erst, wenn der Fall wiederkommt. |
| **W0‑O3** | `SchliessenAngefordert` in `UcBericht`/`UcWirtschaftlichkeit` hat keinen Abonnenten mehr (die Dialoghüllen sind weg). Das Ereignis bleibt, weil die Seiten es beim Umbau nach Blazor (Welle 5) wieder brauchen. |


## Windows-Abnahme 05.09.2026 — Formularraster, Paket P2 (iU8‑E‑2)

**Der Wortlaut.** „Darstellung der Dialoge kompakter und übersichtlicher — Parameterblöcke
rechts. Genauso für andere Dialoge prüfen." Die hausweite Regel dazu steht seit Aufgabe #90 in
`EPOS.UI/Bausteine/Formularraster.razor` und `Formulargruppe.razor`; sie ist in
[`iU9_W14a_Blazor_Port_Protokoll.md`](iU9_W14a_Blazor_Port_Protokoll.md), Abschnitt
„Kompaktes Formularraster", hergeleitet und gemessen. Paket **P2** (Kosten und
Wirtschaftlichkeit, Wellen 1–5) hängt die Parameterblöcke dieser Welle ein — **kein Feld
umbenannt, kein Text geändert, keine Regel je Dialog, kein neues CSS**.

**Die Arbeitsregel des Pakets.** Der Raster umschließt den **Feldlauf**. Eine
`Herleitungszeile`, eine `Kohaerenzzeile`, ein `Warnbanner` oder ein Knopf am **Ende** des
Blocks bleibt außerhalb — sie sind Sätze, keine Felder, und fielen in einer Rasterzelle auf
halbe Breite. Steht so eine Zeile **mitten** im Feldlauf, wird der Raster dort **geteilt**;
zwei Raster derselben Breite tragen dieselbe Beschriftungsspalte, die Kante bleibt also
durchgehend. `Einspaltig` bekommt, wer eine Reihenfolge trägt oder ein breites Pfadfeld führt.

**Umgestellt und bewusst nicht umgestellt.**

| Datei | Felder | Raster | Gruppen | Einspaltig | Klasse‑B‑Entscheid |
|---|---|---|---|---|---|
| `Dialoge/Kosten/CaseEingabeDialog.razor` | 6 | 3 | – | nein | A — Kosten, Nutzungsdauer und der Startjahr‑Block sind Formularblöcke; der Zuschusshinweis steht hinter dem Raster |
| `Dialoge/Kosten/VorlagenPositionDialog.razor` | 5 | 1 | – | nein | A — ein Feldlauf, ein Raster |
| `Dialoge/Kosten/VorlagenUebernahmeDialog.razor` | 5 | 1 | – | **ja** | A — der Block trägt eine **Reihenfolge**: Ziel, dann Quelle, dann die Quelle im Einzelnen. Nebeneinander gestellt liest sie sich nicht mehr als Weg |
| `Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor` | 29 | 4 | – | nein | **B, geteilt.** Die vier Parameterblöcke (Anlagenangaben 1b, KWK‑Zuschlag, Energiesteuer, Stromsteuer) sind Formularblöcke → Raster. Das **Anlagenraster** der Gruppe 1 bleibt ein Datenraster (Felder in Tabellenzellen); Kohärenzprüfung, Hilfsstrom und Vorschau tragen **kein** Feld |
| `Dialoge/Kosten/VorlagenZeile.razor` | 4 | – | – | – | **B, bleibt.** Eine **Bearbeitungszeile in einer Tabelle** (`epos-zr-zeile`, sieben `epos-zr-zelle`): Ihre Felder gehören in Tabellenspalten, nicht in einen Formularblock — der Raster darf dort nicht hinein |

Der Vorlagenübernahme‑Dialog ist die Stelle, an der `Einspaltig` seinen Namen verdient: Er
steht **nicht** für „wie vorher" — die Beschriftung bleibt neben dem Feld —, sondern dafür,
dass eine Kette von Wahlen eine Kette bleibt.

**Nachweis.** `EPOS.UI.Tests` **2 562** grün (2 546 + 16 neue Fälle des Pakets), unter `de-DE`
**und** `LANG=en_US.UTF-8`; `Werkzeuge/Formularkarte` **122** grün. `FormularrasterTests`,
`StilblattTests`, `ParametersatzTests`, `KatalograhmenTests` und `KatalogdialogTests`
unverändert grün — das Stilblatt ist nicht angefasst worden, die Klammerbilanz also
unberührt.
