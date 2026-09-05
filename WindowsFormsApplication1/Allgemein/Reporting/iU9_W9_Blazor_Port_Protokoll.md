# iU9 Welle 9 — Bedarfsmasken vom Startbild — Portprotokoll

> Umsetzung 03.09.2026 auf `ios_migration`, Basis `8995d3e` (nach dem Merge der
> Welle 8). Vorbild in Aufbau und Tiefe: das Protokoll der Welle 8 im selben
> Ordner. Regeln: Wellenplan Abschnitt F, `EPOS.UI/CLAUDE.md`,
> `EPOS.Kern/CLAUDE.md`.

---

## 1. Auftrag und Ergebnis

**Acht WinForms-Masken** — die vier Bedarfskacheln des Startbilds mit ihren
Katalog-Unterdialogen — sind **fünf** Razor-Komponenten in
`EPOS.UI/Dialoge/Bedarf/`; ihre WinForms-Fassungen sind im selben Commit
gelöscht (Regel M1). Zusammen **3 289 Zeilen** Oberflächencode, **42
`MessageBox`** und **229 Kartenzeilen**.

**Nach Welle 9 ist der Assistent bis auf seinen Rahmen und die drei
`Wizard_*`-Seiten Blazor**: Die Seiten 2 bis 5 (Gebäude, Wärmebedarf extern,
Prozesswärme, Stromverbraucher) laufen als Razor-Komponente, zusammen mit den
sechs Erzeugerseiten der Wellen 6 und 7 sind das **zehn von dreizehn**.

| Komponente | ersetzt | Zeilen | Hülle |
|---|---|---|---|
| `GebaeudeKatalogDialog` | `Form_Gebaeude1` (338), `Form_Gebaeude2` (400) | 738 | `Views/Gebäude/GebaeudeKatalogHuelle.cs` |
| `GebaeudeDialog` | `Form_Gebaeude` (695) | 695 | `Views/Gebäude/GebaeudeHuelle.cs` |
| `GebaeudeWohnflaecheDialog` | `Form_GebWohnflaeche` (66) | 66 | `Views/Gebäude/GebaeudeWohnflaecheHuelle.cs` |
| `WaermebedarfExternDialog` | `Form_Waermebedarf` (324) | 324 | `Views/Wärmebedarf/WaermebedarfExternHuelle.cs` |
| `BedarfsProfileDialog` | `Form_Prozesswaerme` (592), `Form_Stromverbraucher` (422), `Form_Brauchwasser` (452) | 1 466 | `Views/Bedarf/BedarfsProfileHuelle.cs` |

**Neu im Kern**: `Ferienzeit`, `Suchmuster`, die W9.0b-Erweiterungen des
`GebaeudeStammCtrl` (Listen, Katalogfilter, zwei Ableitungen), fünf
`LiesProjekt`-Wege, `WaermebedarfStammCtrl.HatProjektzuordnung`,
`ProjektCtrl.Existiert` und `BedarfStammCtrl.Jahressumme`. **Kein neuer
Baustein, kein neues Renderer-Bild.**

### Commits

| Hash | Betreff |
|---|---|
| `b24e5da` | iU9-W9.0a: Assistentenseiten mit beliebigem Listentyp |
| `53aa3f1` | iU9-W9.0b: Listen, Katalogfilter und Ableitungen der Gebäudemasken im Kern |
| `af4dba5` | iU9-W9.0c: `Ferienzeit` im Kern |
| `fd97501` | iU9-W9.0d: Die Projektlisten der Bedarfsgewerke im Kern |
| `01701c6` | iU9-W9.0e: Ein Suchmuster im Haus |
| `04ce2ba` | iU9-W9.0f: Sprungziel `WaermebedarfExternAdmin` |
| `e384853` | iU9-W9.3: `GebaeudeWohnflaecheDialog`, `Form_GebWohnflaeche` gelöscht |
| `7f59398` | iU9-W9.1: `GebaeudeKatalogDialog`, `Form_Gebaeude1` und `Form_Gebaeude2` gelöscht |
| `f960151` | iU9-W9.2: `GebaeudeDialog`, `Form_Gebaeude` gelöscht |
| `ae1c097` | iU9-W9.4: `WaermebedarfExternDialog`, `Form_Waermebedarf` gelöscht |
| `3c89b6c` | iU9-W9.5: `BedarfsProfileDialog`, drei Bedarfsmasken gelöscht |
| `59d984d` | iU9-W9.5a: Brauchwasser-Überlagerung im Gebäudekatalog |
| `ecbbdc0` | iU9-W9.6: 207 Textschlüssel für die acht Masken, de und en |
| `6c174e3` | iU9-W9.7: Formularkarte-Tests auf den Stand nach Welle 9 |

**Das Blatt zuerst.** `GebaeudeWohnflaecheDialog` (W9.3) hat genau einen
Aufrufer und keine Delegaten; sie ist vor dem Katalogeditor und vor dem Host
entstanden, damit beide sie schon als Überlagerung einsetzen konnten. Der
Zwischenschritt kostete eine Hülle mit Fensterweg, die W9.2 nicht mehr braucht
— sie liefert seither nur noch `Gaben()`.

---

## 2. Bauweise

### 2.1 Die Assistentenschnittstelle trägt jetzt jeden Listentyp

Welle 6 hat die Erzeugerseiten über **eine** Schnittstelle eingehängt, weil
ihre je zwei Zeilen in `WizardParent.LoadNewForm` wortgleich waren. Die vier
Bedarfsseiten machen dasselbe — mit **vier anderen Listentypen**
(`Z_ProjGebModel`, `Z_ProjWaermebedarfModel`, `Z_ProjektProzesswaermeModel`,
`Z_ProjektStromverbraucherModel`). Vier weitere Schnittstellen wären viermal
derselbe Text; also trägt sie den Listentyp als Typparameter:

```
IAssistentListenSeite<T>            { List<T> Modelle; void Bestuecken(id, name); }
IAssistentErzeugerSeite : IAssistentListenSeite<WErzeugerModel>   (nur noch ein Name)
BlazorAssistentSeite<TKomponente, TModell> : Form, IAssistentListenSeite<TModell>
BlazorAssistentSeite<TKomponente>          : BlazorAssistentSeite<TKomponente, WErzeugerModel>
```

Die einparametrige Fassung bleibt, damit `AssistentSeiten` und die sechs
Erzeugerhüllen unverändert bauen. `WizardParent` hat danach **fünf** Zweige
über Schnittstellen statt neun mit hartem Typumbruch — und das **Rücklesen der
drei Listen** in `SpeichernAusfuehren` ist entfallen: Die Seite bekommt die
Liste des Assistenten herein und bearbeitet sie an Ort und Stelle, die
Zuweisung traf also schon vorher dasselbe Objekt.

### 2.2 Die Datenseite im Kern

| Was | Wo | Herkunft |
|---|---|---|
| `Ferienzeit` | `EPOS.Kern/Allgemein/` | `Form_Gebaeude2.JahrestagUmrechner`:73, `BerechneJahrestag`:83, `btn_Speichern_Click`:177-198 |
| `Suchmuster` | dito | `Form_Gebaeude.ApplyGridFilter`:637 **und** `WaermepumpenKatalogFilter` (W7.0b) — zweimal dieselbe Übersetzung |
| `GebaeudeStammCtrl.Gebaeudearten/Gebaeudetypen/Katalognamen/Baualtersklassen/KlassenBuchstabe/KlassenIndex/FilterAusdruck/Filtern/BauartAusBauweise/BauweiseAusBauart` | `EPOS.Kern/Controller/` | `Form_Gebaeude`:97/185/219/329-419, `Form_Gebaeude1`:70/88/97/107/188 |
| `Z_ProjGebCtrl.LiesProjekt` | dito | `Form_Start`:312, `GebäudeKontextMenuCtrl`:87, `WizardParent.LoadZGeb` — **dreimal** wortgleich |
| `Z_ProjektGebGanglinieCtrl.LiesProjekt` | dito | `Form_Start`:264 und `WaermebedarfExternKontextMenuCtrl` |
| `Z_ProjektProzesswaermeCtrl/StromverbraucherCtrl/BrauchwasserCtrl.LiesProjekt` | dito | `Form_Start`:213/494/1863 und die beiden Kontextmenüs |
| `WaermebedarfStammCtrl.HatProjektzuordnung` | dito | `Form_Waermebedarf.btn_Loeschen_Click`:304 |
| `ProjektCtrl.Existiert` | dito | `Form_Prozesswaerme.ProjektIstGespeichert`:387 und `Form_Stromverbraucher`:254 |
| `BedarfStammCtrl.Jahressumme` | dito | `Prozesssumme` der drei Bedarfsmasken (:212 / :99 / :151) |

**Alle sechs neuen SQL-Anweisungen sind parametrisiert** (`DbParam`), keine
Zeichenkettenverkettung mehr; der SQL-Dialektprüfer meldet 0 Fundstellen.

### 2.3 Ein Katalogsatz, zwei Reiter

`Form_Gebaeude2` bekam mit `frm.model = model` **dasselbe** `GebaeudeModel` in
die Hand wie `Form_Gebaeude1`. Sie war nie ein eigener Datensatz, sondern die
zweite Hälfte desselben — deshalb zwei `Reiterblatt` statt eines Unterdialogs,
und deshalb entfällt der Knopf „Weitere Eingaben…".

**Der zweite Reiter behält seinen Übernahmeknopf** (A‑6). Sein Vorläufer war
ein modales Fenster mit OK, und dieses OK tat mehr als schließen: Es prüfte
alle Zahlen, leitete vier Größen ab (`Maximaleraumtemperatur < 1 → 24`, die
Flags `Wochenende` und `Ferien` aus `> 0`, `WW_Bedarf = 0`, Winterferienbeginn
`0 → 366`) und prüfte die vier Ferienregeln. Wer das Fenster nie öffnete,
änderte dort auch nichts — und genau das bliebe ohne den Knopf nicht erhalten:
Ein Satz, dessen Maximaltemperatur im Katalog 0 ist, würde beim bloßen Ändern
einer Fläche stillschweigend auf 24 gehoben.

### 2.4 Drei Betriebsarten in einer Komponente

`GebaeudeDialog` trägt, was der Vorläufer über zwei Felder und ein
`Load`-Ereignis löste, das die halbe Maske versteckte:

| Betriebsart | Projektliste | Pfeile | „Ändern" | Schlussleiste |
|---|---|---|---|---|
| Projekt | ja | ja | ja | OK / Abbrechen |
| Assistent (Seite 2) | ja | ja | ja | — (der Rahmen hat sie) |
| Verwaltung (`Masken.GebaeudeAdmin`) | — | — | — | OK / Abbrechen |

Jede hat einen eigenen bunit-Feldbestandsfall (Risiko R‑W9‑2).

### 2.5 Die Hülle rechnet, die Komponente zeigt

`BedarfsProfileDialog` löst drei Masken ab, die ein **lebendes**
`SimulationWaermebedarf` bzw. `SimulationStrombedarf` als Feld hielten und es
dem Ergebnisdialog in die Hand gaben. Hier bleibt es in der Hülle
(`BedarfsProfileHuelle.Rechenstand`): Sie rechnet, baut den Parametersatz des
Ergebnisdialogs und reicht nur den herein. Die Komponente kennt die
Simulationsklassen nicht (Risiko R‑W9‑4, dasselbe Vorgehen wie W8.2).

„monatlicher Verlauf" zeigt denselben Rechenstand noch einmal — mit den
Startreitern der Vorläufer: 1 bei Prozess und Strom, 2 (Grafik samt
Brauchwassersicht) beim Brauchwasser.

### 2.6 Neun Überlagerungen statt neun Fenster

| Wirt | Überlagerung | Herkunft |
|---|---|---|
| `GebaeudeDialog` | `GebaeudeKatalogDialog` (W9.1) | „Gebäude in DB ändern/neu…" |
| | `GebaeudeWohnflaecheDialog` (W9.3) | „Ändern" |
| | `GebaeudetypDialog` (W8.4) | „Gebäudetyp in DB ändern…" |
| | `Rueckfrage` | „Gebäude in DB löschen" |
| `GebaeudeKatalogDialog` | `BedarfsProfileDialog` (W9.5) | „Brauchwasser…" auf Reiter 2 |
| `WaermebedarfExternDialog` | `Rueckfrage` | „DB Ganglinie löschen" (neu, A‑8) |
| `BedarfsProfileDialog` | `BedarfErgebnisDialog` (W8.2) | „Simulation" / „monatlicher Verlauf" |
| | `TypStammDialog` (W8.1) | „DB ändern" und „DB neu" |
| | `TypProfilDialog` (W8.3) | „Typ in DB ändern" |
| | `NamensDialog` + `Rueckfrage` | „DB neu" und „DB löschen" |

Dafür haben `BedarfErgebnisHuelle`, `TypStammHuelle` und `GebaeudetypHuelle`
neben ihrem `Oeffnen()` ein `Gaben()` bekommen — dieselbe Trennung wie in
Welle 4.

---

## 3. Feldkarten-Abgleich

Die acht Karten wurden am 03.09.2026 neu gezogen (Stand nach W8) und liegen
unter `scratchpad/iU9/karten_w9/`. Abgeglichen ist der **Feldbestand nach Zahl
und Beschriftung**; bei `BedarfsProfileDialog` je **Ausprägung**, nicht je
Komponente (Risiko R‑W8‑1).

| Maske | Karte | Komponente | Anmerkung |
|---|---|---|---|
| Form_Gebaeude1 | 37 Zeilen, 18 TextBox, 6 ComboBox, 5 Button | Reiter 1: 17 Zahlenfelder, 6 Klapplisten, 1 Textbereich | „Fläschen" im Titel berichtigt (A‑4) |
| Form_Gebaeude2 | 41 Zeilen, 28 TextBox, 6 GroupBox | Reiter 2: 12 Zahlenfelder, 16 Ganzzahlfelder, 5 Gruppen | die 6 unbeschrifteten Monatsfelder tragen jetzt „Monat :" |
| Form_Gebaeude | 27 Zeilen, 8 TextBox, 2 ComboBox, 2 RadioButton, 10 Button | 2 Klapplisten, 1 Optionsgruppe, 1 Suchfeld, 5 gesperrte Felder, 10 Knöpfe | −3 = die drei verborgenen Hilfsfelder (A‑7) |
| Form_GebWohnflaeche | 15 Zeilen, 8 TextBox, 1 ListBox, 1 CheckBox | 2 Zahlenfelder, 1 Klappliste, 1 Schalter, 5 gesperrte Felder | Kopf vollständig nur lesend |
| Form_Waermebedarf | 11 Zeilen + 2 Laufzeitfelder | 2 Raster, 1 Klappliste (Kanal), 5 Knöpfe | das Kanalfeld steht jetzt im Markup |
| Form_Prozesswaerme | 21 + 4 Zeilen | 1 Zahlenfeld, 4 gesperrte Felder, 10 Knöpfe, Katalograster MIT Typspalte | |
| Form_Stromverbraucher | 22 + 3 Zeilen | dieselbe Komponente, Ausprägung `Stromverbraucher` | Katalog OHNE Typspalte (Bestand: `ListBox`) |
| Form_Brauchwasser | 21 + 4 Zeilen | Ausprägung `Brauchwasser` | einzige der acht ohne Satelliten-`.resx` |

**Beschriftungen aus dem DESIGNER, nicht aus der Karte.** Die Karte nennt bei
`Form_Gebaeude2` sechs Ferienfelder ohne Text (`Ostern_Monat_A` …) — die `.resx`
setzt dort nur die Beschriftung des ERSTEN Feldes jeder Spalte („Monat :"), die
übrigen erben sie optisch aus der Spaltenanordnung. In der Razor-Fassung
bekommt **jedes** Feld seine Beschriftung, weil ein Formularraster keine
stillschweigende Spaltenzuordnung kennt. Bei `Form_Gebaeude1` steht im Titel
„Fläschen"; das ist ein Tippfehler (englisch „bottles") und ist berichtigt.

---

## 4. Abweichungen (mit Begründung)

| # | Was | Warum |
|---|---|---|
| **A‑1** | Der 500‑ms-Bildblitz nach „Übernehmen" (drei Masken) entfällt; an seiner Stelle steht eine Meldung | Der Vorläufer hielt dafür den Oberflächenfaden mit `Thread.Sleep(500)` an — in der WebView wäre das ein eingefrorener Dialog (wie W8 A‑5) |
| **A‑2** | **Befund W9‑B3 behoben:** `GebaeudeWohnflaecheDialog` schreibt die dezentrale Warmwasserbereitung zurück | `btn_OK_Click`:42‑49 las die Checkbox und verwarf sie, obwohl `Z_ProjektGebaeude.dezWarmwasserbereitung` gespeichert wird. Wer den Schalter umlegte und mit OK schloss, sah ihn danach wieder auf dem alten Stand — ein **stiller Datenverlust in der Bedienung**, keine Fachentscheidung. Dieselbe Klasse wie W7‑O‑4 |
| **A‑3** | **Befund W9‑B4 behoben:** die beiden Zahlen der Wohnflächenangabe sind Pflichtfelder mit Warnbanner | `Double.Parse` ohne Prüfung warf bei leerer oder ungültiger Eingabe |
| **A‑4** | Der Titel des Katalogeditors heißt „Gebäudedaten: **Flächen**, U‑Werte" | Der Designer schreibt „Fläschen", die englische Satellitendatei übersetzt es folgerichtig mit „bottles". Ein Tippfehler in einer Beschriftung, kein Steuerwert (wie W8 A‑2) |
| **A‑5** | **Befund W9‑B8 behoben:** Steuerwert und Anzeigetext der Verwendung sind getrennt | `comboBox_Verwendung` führte die beiden STEUERWERTE „Wohngebaeude"/„Nicht Wohngebaeude" als Anzeigetext, und `Form_Gebaeude1.en-US.resx` übersetzte sie („residential buildings"). In englischer Oberfläche schrieb der Vorläufer damit englischen Text in `Wohngebaeude_Nicht_Wohngebaeude`, und **jeder Filter lief danach ins Leere**. Drei-Schichten-Regel, Persistenzschicht |
| **A‑6** | Der zweite Reiter des Katalogeditors behält einen eigenen Knopf „Werte übernehmen" | Sein Vorläufer war ein modales Fenster, dessen OK vier Größen ableitet. Ohne den Knopf liefen diese Ableitungen bei JEDEM Speichern — auch für Sätze, deren zweite Seite der Anwender nie geöffnet hat (§ 2.3). Dasselbe Muster wie `TypProfilDialog` in W8 |
| **A‑7** | Die drei verborgenen Hilfsfelder von `Form_Gebaeude` (Baujahr, Jahresnutzungsgrad, dezWarmwasser) haben kein Gegenstück | Sie waren `Visible = false` und dienten als Wertträger zwischen Liste und Ändern-Dialog. Das ist jetzt die Zeile selbst |
| **A‑8** | „DB Ganglinie löschen" fragt nach | Der Vorläufer löschte auf einen Klick, und der Katalog gilt für ALLE Projekte (wie W6 A‑4). Die Sperre „Es existiert eine Projektzuordnung" bleibt und steht als Warnbanner |
| **A‑9** | „▶" entfernt die MARKIERTE Zeile statt der ersten gleichen Namens | `Form_Waermebedarf.btn_Entfernen_Click`:240 suchte den ersten Treffer über den Bezeichner; bei zwei Zuordnungen derselben Ganglinie traf er die falsche. Dieselbe Fehlerklasse wie W7 A‑21 — und der Kommentar in `Form_Gebaeude`:283 benennt sie für das Gebäude selbst |
| **A‑10** | 42 `MessageBox` werden `Warnbanner`, `Rueckfrage` oder Meldungstext | Wie A‑4 aus Welle 8: Bestätigungen bleiben Bestätigungen, Ablehnungen bleiben als Banner stehen und lassen den Dialog offen |
| **A‑11** | Der Erfolgstext nach dem Löschen bleibt auf die Prozessmaske beschränkt | Nur `Form_Prozesswaerme` meldete „Prozess erfolgreich gelöscht."; Strom und Brauchwasser schwiegen. Wörtlich übernommen |
| **A‑12** | Ein Bestandswert außerhalb einer Klappliste wird ihr VORANGESTELLT (Gebäudetyp, Gebäudeart, Bedarfsart) | Die `ComboBox` des Vorläufers war frei beschreibbar; ein `select` würde ihn still verwerfen (wie W7 A‑16) |
| **A‑13** | Der zweite Reiter erscheint erst beim Betreten | Der Baustein `Reiterblatt` zeichnet ein ungewähltes Blatt gar nicht — seine Felder stehen sonst im Tabulatorzyklus |
| **A‑14** | Kein KI-Aufrufknopf, kein `FusszeilenNorm`, kein `SchriftAngleichen`, keine Zebrafarben, kein ToolTip-Zeichnen, keine `pictureBox1` | Wie A‑14 aus Welle 8: Der KI-Einstieg hat in `EPOS.UI` noch keinen Baustein (W15b); die übrigen sind WinForms-Layoutkorrekturen, die Hülle und CSS erledigen |
| **A‑15** | `Form_Start.pBox_StdLastProfil_Click` rief `SetControls` ZWEIMAL — jetzt einmal | Vor und nach dem Aufbau der Liste; der erste Aufruf hatte keine Wirkung |

---

## 5. Texte

**207 Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`,
`Resource.en-US.resx` und — von Hand, weil hier kein Visual Studio läuft —
`Resource.Designer.cs`. Alle drei Dateien geprüft: **3 818 de = 3 818 en**,
0 Dubletten, 0 Schlüssel nur in einer Sprache.

| Präfix | Zahl | Wofür |
|---|---|---|
| `GEBK_*` | 84 | Katalogeditor (beide Reiter) |
| `BPF_*` | 47 | Bedarfsprofile, je Ausprägung mit `_PROZ`/`_STROM`/`_BW` |
| `GEB_*` | 22 | Gebäudeverwaltung |
| `GEB_BAK_A…U` | 21 | die 21 Baualtersklassen |
| `GEBW_*` | 12 | Wohnflächenangabe |
| `WBX_*` | 7 | Wärmebedarf extern |

**Die englischen Fassungen sind, wo es sie gab, WÖRTLICH aus den gelöschten
`en-US`-Satellitendateien übernommen** (sieben der acht Masken waren
lokalisiert, zusammen 159 englische Texte). Neu übersetzt sind nur die
Brauchwassertexte, die Meldungen und die 21 Baualtersklassen.

**Wiederverwendet** statt neu angelegt: `GEB2_TITEL` (der Titel der zweiten
Gebäudemaske wird der Titel des zweiten Reiters), `KANAL_LABEL`,
`KANAL_*_ANZEIGE`, `ALLG_BTN_*`, `BPRO_FRAGE_LOESCHEN`, `BTYP_LBL_NAME`,
`BTYP_LBL_BESCHREIBUNG`, `BTYP_MSG_NAME_LEER`, `KFAK_SP_WAHL`,
`BHKWV_SP_NAME`.

**Nicht übersetzt sind die Steuerwerte:** die sechs Bedarfsarten („Wohnfläche
[m²]" …, samt der beiden Leerzeichen in „Verbrauch  [MWh/a]") landen in
`Z_ProjektGebaeude.Einheit_Waermebedarf_Wohnflaeche` und werden beim nächsten
Öffnen wieder mit der Liste verglichen; ebenso die beiden Verwendungen
(A‑5) und die drei Kanäle.

**`help_mapping.txt` bleibt unverändert.** Die Zeilen `Form_X.btn_Help` gelten
weiter — der Schlüssel benennt die Wikiseite, nicht die Klasse; jede Komponente
trägt ihren alten Schlüssel als `HilfeSchluessel`.

**`Allgemein/KI/HilfeKontext.cs`:** die acht Einträge der gelöschten Masken
entfernt, jeweils im Commit ihrer Maske (Regel F10).

---

## 6. WinForms-Seite

**Gelöscht** (38 Dateien):

```
Views/Gebäude/Form_Gebaeude.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Gebäude/Form_Gebaeude1.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Gebäude/Form_Gebaeude2.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Gebäude/Form_GebWohnflaeche.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Wärmebedarf/Form_Waermebedarf.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Prozesswärme/Form_Prozesswaerme.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Stromverbraucher/Form_Stromverbraucher.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Brauchwasser/Form_Brauchwasser.{cs,designer.cs,resx}
```

**Neu auf der Windows-Seite** (5): `Views/Gebäude/GebaeudeHuelle.cs`,
`Views/Gebäude/GebaeudeKatalogHuelle.cs`,
`Views/Gebäude/GebaeudeWohnflaecheHuelle.cs`,
`Views/Wärmebedarf/WaermebedarfExternHuelle.cs`,
`Views/Bedarf/BedarfsProfileHuelle.cs`.

**Aufrufer umgestellt** (17 Stellen in 8 Dateien): `Form_Start` (5 Kacheln),
`GebäudeKontextMenuCtrl` (3), `WaermebedarfExternKontextMenuCtrl` (2),
`ProzesswaermeKontextMenuCtrl` (2), `StrombedarfKontextMenuCtrl` (2),
`WinFormsNavigation` (1, `Masken.GebaeudeAdmin`), `WizardParent` (4 Zweige
gestrichen), `AssistentSeiten` (4 Zeilen).

**Zwei tote Anlagen gestrichen:** `GebäudeKontextMenuCtrl.ContextMenuItemLoeschen_Click`
legte ein `Form_Gebaeude` an, das es weder füllte noch zeigte;
`WaermebedarfExternKontextMenuCtrl.ContextMenuItemLoeschen_Click` benutzte ein
`Form_Waermebedarf` nur als Listenträger.

**Keine Typverwendung ist übrig:**

```
grep -rn "(new|typeof|:)\s*(Form_Gebaeude|Form_Gebaeude1|Form_Gebaeude2|
    Form_GebWohnflaeche|Form_Waermebedarf|Form_Prozesswaerme|
    Form_Stromverbraucher|Form_Brauchwasser)\b" --include=*.cs .
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
dotnet build WP-Plan.sln -c Release -p:Platform=x64
→ 0 Fehler, 20 Warnungen
```

Gleichauf mit der Basis nach Welle 8 (20). Aufteilung unverändert: 14 WFO1000,
2 CS0108, 2 CS0109, 1 WFO0003, 1 CA2255 — keine der acht gelöschten Masken trug
eine WFO1000-Fundstelle, und keine neue ist dazugekommen.

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ KiKern.Tests         450 gruen
  SpeicherEngine.Tests  337 gruen
  EPOS.UI.Tests       1 104 gruen   (+98 aus Welle 9)
  EPOS.Kern.Tests       175 gruen   (+62 aus Welle 9)
  zusammen            2 066 gruen, 0 rot
```

**98 neue bunit-Fälle**: `GebaeudeWohnflaecheDialog` 12,
`GebaeudeKatalogDialog` 24, `GebaeudeDialog` 21, `WaermebedarfExternDialog` 15,
`BedarfsProfileDialog` 25, dazu ein Zählwert in `SprungzielTests`. Jeder Satz
prüft den Feldbestand (Zahl UND Beschriftungen), die Vorbelegung, die
Prüfregeln, die Rückrufe und die Tastatur; die Kultur ist auf de‑DE gepinnt.

**62 neue Kern-Fälle**: `FerienzeitTests` 18 (Jahrestag ↔ Tag/Monat, die vier
Regeln je Fall, Winter über die Jahresgrenze, das Heben des Winterbeginns),
`GebaeudeKatalogTests` 33 (Baualtersklassen und ihre Umkehrbarkeit,
`BauartAusBauweise`/`BauweiseAusBauart` je Stufe, die vier Filterzweige samt
Befund W9‑B1, die drei Listen gegen `Kenndaten_Test.sqlite`, sechs
`Suchmuster`-Fälle), `ProjektlistenTests` 8 (`LiesProjekt` ×5,
`HatProjektzuordnung`, `Existiert`, `Jahressumme`).

### 7.3 Formularkarte

```
dotnet test Werkzeuge/Formularkarte.Tests -c Release
→ 123 gruen
```

Zwei Anker mussten umgehängt werden (W9.7): `Form_Gebaeude` als Zeuge für „über
die Startseite erreichbar" wird `Form_Stromganglinie` (Welle 12), und der
„unklar"-Zeuge `Form_GebWohnflaeche` wird `Form_PufferSp_Bearbeiten`
(Welle 14a). Drei Zähler haben sich bewegt: 56 Designer-Dateien (66),
55 Masken (63), 29 lokalisierte (37) und 54 von 55 erreichbar (61 von 63).

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1 --erreichbarkeit
→ Masken 55 (63 nach W8), lokalisiert 29 (37), erreichbar 54,
  unerreichbar 0, verwaist 0, unklar 1
```

**55 = 63 − 8.**

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 241 SQL-Texte geprueft: 0 Fundstellen, 170 dynamisch, 1 071 in Ordnung
python3 … --selbsttest
→ 32 Anweisungen, 0 Abweichungen
```

Keine Nachbesserung am Prüfer nötig. Die Zahl der geprüften Texte sinkt von
1 254 auf 1 241, weil die acht Masken ihre SQL verloren oder — parametrisiert —
mitgenommen haben. Gezogen wurde er nach **jedem** der sechs Kern-Schritte.

### 7.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 15 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

Unverändert 15 — Welle 9 bringt kein neues Renderer-Bild.

### 7.7 Referenzlauf

**Pflicht in dieser Welle**, weil Projektzuordnungen und Katalogsätze
Simulationseingang sind und acht Kern-Controller angefasst wurden.

```
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w9
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/w9
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

**Byte-gleich, nicht nur innerhalb der Toleranz.**

---

## 8. Grenzen

* **Am Gerät ungeprüft.** Alles hier ist ohne Windows entstanden. Die
  Abnahmeliste in § 9 ist der Prüfplan.
* **Zehn WebViews im Assistenten** (Risiko R‑W9‑5). Der verzögerte Aufbau aus
  W6.0e trägt: Eine Seite baut ihre WebView erst in `Bestuecken`. Wie viel
  Speicher zehn besuchte Seiten kosten, ist nur am Gerät messbar
  (Abnahmepunkt 9).
* **Der Admin-Modus des Katalogeditors ist unerreichbar** (Befund W9‑B10):
  `Form_Gebaeude1.m_bAdmin` hatte im ganzen Bestand keinen Aufrufer. Er ist
  übernommen, weil er vollständig ausformuliert dastand; erreichbar wird er
  erst, wenn ihn jemand aufruft.
* **Fünf Bestandsbefunde bleiben stehen** (§ 10) — sie sind Fachentscheidungen,
  keine Portfragen.

---

## 9. Abnahmeliste Windows (iZ5) für diese acht Masken

Je Dialog: öffnen mittig, kein weißes Aufblitzen, ziehbar und maximierbar,
Tabellen ohne Umbruch, de **und** en (`HKCU\Software\wp-plan\Language`),
Hochkontrast, 125 % und 150 % scharf, Maus **und** Finger (44 px), Tab-Zyklus
bleibt im Dialog, Esc schließt, Infoknopf zeigt die Wikiseite.

| # | Aufrufweg | Was besonders zu prüfen ist |
|---|---|---|
| 1 | Startbild → **Gebäude** | Umschalter „Wohngebäude"/„Gewerbe+Sonstige" lädt Katalog UND Artenliste neu; die vier Filterkombinationen; Wildcard-Suche „Haus*_1990*"; ◀ legt eine Zeile mit „Wohnfläche [m²]", Nutzungsgrad 1 an; ▶ trifft die markierte Zeile |
| 2 | In 1 → **„Ändern"** | Kopf nur lesbar; Bedarfsart wechselt die Einheitsanzeige; leeres Feld meldet; OK schreibt VIER Werte zurück — **auch den Schalter „Dezentrale Warmwasserbereitung" (A‑2)** |
| 3 | In 1 → **„Gebäude in DB ändern…"** | Zwei Reiter; Baujahr ↔ Buchstabe; Bauart aus Bauweise beim Laden UND Bauweise aus Bauart beim Speichern (W9‑O‑2); 17 Pflichtzahlen melden ihren Feldnamen; „Überschreiben" trifft den Ursprungsnamen |
| 4 | In 3 → Reiter **„Gebäudedaten – Raumtemperaturen …"** | „Werte übernehmen" leitet ab und prüft die vier Ferienregeln; ohne den Knopf bleibt der Satz unberührt; Winter über die Jahresgrenze |
| 5 | In 4 → **„Brauchwasser…"** | Die Brauchwasserliste des laufenden Projekts als Überlagerung; OK schreibt die Zuordnung |
| 6 | In 1 → **„Gebäude in DB neu…" / „…löschen" / „Gebäudetyp in DB ändern…"** | Neu ohne Markierung; Löschen fragt nach und meldet „Gebäude gelöscht!"; der Gebäudetyp ist die W8.4-Komponente |
| 7 | Menü → **Gebäudeverwaltung** (`Masken.GebaeudeAdmin`) | Kein Projektteil, keine Pfeile, kein „Ändern" — nur der Katalog |
| 8 | Startbild → **Wärmebedarf extern** | Kanalwahl wirkt auf die markierte Zeile; eine neue Zeile steht auf Heizung; „DB Ganglinie löschen" fragt nach (A‑8) und meldet bei Projektzuordnung; „Einlesen/Bearbeiten.." öffnet die Verwaltung über der Komponente (Sprungbrücke) |
| 9 | Assistent → **Seiten 2 bis 5** | Jetzt zehn WebViews im Assistenten (R‑W6‑1/R‑W9‑5): Speicher am Gerät messen; Vor/Zurück behält den Listenstand |
| 10 | Startbild → **Prozesswärme / Stromverbraucher / Brauchwasser** | Je Ausprägung: Katalogzeile zeigt Σ Monate, Projektzeile die Summe der Zeile; „Übernehmen" ohne Zeile oder mit negativem Wert meldet — in **allen dreien** in der gewählten Einheit (Entscheid W9‑O‑3). Das Wahlfeld „Einheit" steht auf MWh; Umschalten auf kWh nimmt Jahresverbrauch, Summe, das Eingabefeld und die Meldung mit, der gespeicherte Wert bleibt MWh |
| 11 | In 10 → **„Simulation"** | Prozess und Strom rechnen ALLE Zuordnungen, Brauchwasser NUR die gewählte; danach ist „monatlicher Verlauf" frei und zeigt denselben Stand ; bei der Prozesswärme steht „Wärmebedarf Prozess" seit dem Nachtrag zu W9‑O‑3 in **MWh** und damit um den Faktor 1000 kleiner als bisher — „davon Brauchwasser" bleibt zeichengleich |
| 12 | In 10 im **Assistenten** ohne gespeichertes Projekt | Der Hinweis „Vorschau ohne Projektwerte" kommt genau EINMAL — und beim Brauchwasser gar nicht |
| 13 | In 10 → **„DB ändern" / „DB neu" / „Typ in DB ändern"** | Die drei W8-Komponenten als Überlagerung; „DB neu" fragt erst den Namen |
| 14 | **Kontextmenüs** der vier Übersichtslisten | Bearbeiten, Neu und Löschen führen zu denselben Dialogen und schreiben danach `OeffneGewerk` |

---

## 10. Offene Punkte

| # | Was | Vorschlag |
|---|---|---|
| **W9‑B1 / W9‑O‑1** | Der Katalogfilter „Gebäudeart gewählt, Baujahr Alle" filtert im Gebäudeart-Handler **ohne**, im Baujahr-Handler **mit** der Verwendung (`Form_Gebaeude`:359 gegen :392). Welche Liste erscheint, hängt davon ab, welche Klappliste zuletzt angefasst wurde | Wörtlich übernommen (Regel F3), sichtbar im Parameter `ausBaujahrwahl` und mit einem eigenen Testfall festgehalten. **Entscheid des Anwenders:** Soll der Zweig die Verwendung immer mitfiltern? Es wäre eine Zeile in `GebaeudeStammCtrl.FilterAusdruck` |
| **W9‑B6 / W9‑O‑2** — **erledigt** | Die gespeicherte **Bauweise** (`Wohnfläche × 20/50/100`) hing am Index der **Gebäudeart**-Klappliste, nicht an der Bauart-Klappliste — obwohl die Bauart aus derselben Größe abgeleitet ANGEZEIGT wurde (`InitModelFromControls`:188‑191) | **Entscheid (Anwender, 04.09.2026): Bauart bestimmt die Bauweise — umgesetzt in Commit `iU9-W9: Bauart bestimmt die Bauweise im Gebaeudekatalog (Anwenderentscheid W9-O-2)`.** Die Anzeige ist damit zum ersten Mal auch die Eingabe: `GebaeudeKatalogDialog` führt `Daten.Bauweise` bei jeder Bauartwahl und vor jedem Schreiben nach (`Gebaeudebauweise.BauweiseAusBauart`), beim Laden kommt die Bauart weiterhin aus der gespeicherten Bauweise (`Gebaeudebauweise.BauartAusBauweise`) — der Rundweg bleibt konsistent. Die Gebäudeart-Klappliste behält nur ihre eigene Bedeutung; die Hülle reicht die Größe nur noch durch. Die Rechnung steht jetzt — wie `Ferienzeit` und `Suchmuster` — in der öffentlichen Kernklasse `Allgemein/Gebaeudebauweise.cs`, weil `GebaeudeStammCtrl` `internal` ist; dessen beide Namen aus W9.0b bleiben als Durchreiche stehen. Vier bunit-Fälle halten die Regel fest |
| **W9‑B7 / W9‑O‑3** — **erledigt** | Die Meldung bei ungültigem Jahresverbrauch nannte beim Stromverbraucher **kWh**, bei Prozess und Brauchwasser **MWh** — für dieselbe Größe | **Entscheid (Anwender, 04.09.2026):** MWh als Vorgabe, kWh wählbar, konsistent in den Ansichten — umgesetzt in `20ccf77` (ein Text `BPF_MSG_WERT` mit dem Platzhalter `{0}` statt dreier Texte) und `7eac01d` (Wahlfeld „Einheit" im Dialog). MWh war die richtige Einheit: `Z_Projekt_*.Summe` und die zwölf Monatsfelder der Katalogtabellen stehen in MWh — `BhkwPlan.StromWocheToJahr` nimmt sie mal 1 000 und rechnet ab da in kWh weiter (Stichprobe: „Wohnen groß (VDI 6002), 1 Person" = 0,5943, also 594 kWh je Person und Jahr). Jahresverbrauch, Gesamtsumme, die Einheit hinter „neuer Wert" und der Meldungstext folgen der Wahl; der **Speicherweg bleibt MWh und bei der Vorgabe bitgleich**, umgerechnet wird nur für Anzeige und Eingabe. Die Wahl liegt in `BedarfEinheitWahl` — derselbe Schlüssel, den der Ergebnisdialog liest (W8‑O‑5) |
| **W9‑O‑3** — Nachtrag, **erledigt** | Derselbe Entscheid, eine Ebene tiefer: Der W9-Weg WIES die Prozesssumme selbst in kWh aus. `BedarfsProfileHuelle.Rechenstand` setzte `Waermebedarf_Prozess = prozesswerte.Sum()` — die blanke Summe der Stundenwerte, wörtlich aus `Form_Prozesswaerme` übernommen —, während der Kern (`SimulationWaermebedarf`:384), die Prozesswärme-Verwaltung (`Form_Prozesswaerme_Admin`:97) und die Ergebnishülle (`Energieeinheit.MWh`) dasselbe Feld in **MWh** führen. „Wärmebedarf Prozess" stand unter „Grafik"/„Ergebnisse" damit um den Faktor 1000 zu groß | **Entscheid (Anwender, 04.09.2026):** Prozesswärme im W9-Weg ebenfalls über die Einheitenklasse — umgesetzt in `a3906ca`. Die Umrechnung steht jetzt einmal an dem Feld, das sie betrifft: `SimulationWaermebedarf.ProzesssummeUebernehmen()` setzt `Energieeinheit.MWh.AusKWh(prozesswerte.Sum())` — kein nackter Teiler im Aufrufer. Der **Rechenweg ist unberührt**: `Waermebedarf_berechnen` behält seine eigene Zeile, der Referenzlauf bleibt byte-gleich (1030/1007/1017 gegen `2026-08-30_B3-Kaskade`). Der **Brauchwasserzweig bleibt bewusst unsymmetrisch** — dort liegt der Wert in kWh, und genau so nimmt die Hülle ihn an (W8‑O‑5); die Zahl stimmt heute. Vereinheitlichen ginge nur, indem man die Annahme der Hülle mitdreht, und die gilt auch für den Simulationsweg, der das Feld aus dem Kern schon in MWh bekommt — eigener Anwenderentscheid, notiert als **W8‑O‑5b**. Vier Fälle in `EPOS.Kern.Tests/ProzesssummeEinheitTests.cs` halten die Regel fest (8760 × 1 kWh = 8,76 MWh) |
| **W9‑B9 / W9‑O‑4** | „Überschreiben" im Katalogeditor trifft den URSPRUNGSNAMEN. Wer den Namen im Feld ändert und dann überschreibt, trifft nichts: Der Vorläufer setzte `Gebaeudename` aus dem Feld und schrieb `UPDATE … SET Bezeichner = ? WHERE Bezeichner = ?` mit demselben Wert — **0 Zeilen, stille Erfolgsmeldung** | Behoben, soweit es ohne Fachentscheid geht: Die Hülle schreibt jetzt gegen den Ursprungsnamen, ein Umbenennen wirkt also. **Frage an den Anwender:** Soll „Überschreiben" umbenennen dürfen, oder soll das Namensfeld im Modus Bearbeiten gesperrt sein? |
| **W9‑B10 / W9‑O‑5** | Der Admin-Modus des Katalogeditors (`Form_Gebaeude1.m_bAdmin`) hat im ganzen Bestand keinen Aufrufer | Übernommen, weil vollständig ausformuliert. **Entscheid des Anwenders:** Soll er über einen Menüpunkt erreichbar werden, oder fällt er ersatzlos weg? |
| **W9‑O‑6** | Der KI-Aufrufknopf fehlt in allen fünf Dialogen (A‑14) | Mit W15b, wenn `Gespraechsverlauf` steht — wie W6‑O‑6, W7‑O‑6 und W8‑O‑6 |
| **W9‑O‑7** | Der Assistent führt jetzt **zehn** WebViews (R‑W9‑5) | Der verzögerte Aufbau trägt; der Speicherbedarf ist Abnahmepunkt 9. Fällt er durch, wäre der Rückweg eine gemeinsame WebView für alle Seiten (Muster `BerichteKostenSeite`, W5) |

### Entfallene Befunde

* **W9‑B2:** `Form_Waermebedarf.Einlesen`:276‑298 hatte keinen Aufrufer — ein
  toter Importpfad mit `Form_Hinweis`. Ersatzlos entfallen.
* **W9‑B5:** `Form_Gebaeude2.SetControls`:45‑60 setzte zuerst **alle 16**
  Ferienfelder auf `Ferienbeginn_1` und überschrieb sie unmittelbar danach über
  `JahrestagUmrechner`. Wirkungslos, entfallen.

---

## 11. Geänderte und neue Dateien

**Neu in `EPOS.UI`** (9): `Dialoge/Bedarf/GebaeudeWohnflaecheDialog.razor`,
`GebaeudeWohnflaecheErgebnis.cs`, `GebaeudeKatalogDialog.razor`,
`GebaeudeKatalogDaten.cs`, `GebaeudeKatalogModus.cs`, `GebaeudeDialog.razor`,
`GebaeudeDaten.cs`, `WaermebedarfExternDialog.razor`,
`WaermebedarfExternDaten.cs`, `BedarfsProfileDialog.razor`,
`BedarfsProfileDaten.cs`.

**Neu im Kern** (3): `Allgemein/Ferienzeit.cs`, `Allgemein/Suchmuster.cs`,
`Allgemein/Gebaeudebauweise.cs` (04.09.2026, Entscheid W9‑O‑2).
**Geändert im Kern** (8 + Ressourcen): `Controller/GebaeudeStammCtrl.cs`,
`Controller/BedarfStammCtrl.cs`, `Controller/Z_ProjGebCtrl.cs`,
`Controller/Z_ProjektGebGanglinieCtrl.cs`,
`Controller/Z_ProjektProzesswaermeCtrl.cs`,
`Controller/Z_ProjektStromverbraucherCtrl.cs`,
`Controller/Z_ProjektBrauchwasserCtrl.cs`,
`Controller/WaermebedarfStammCtrl.cs`, `Controller/ProjektCtrl.cs`,
`Allgemein/WaermepumpenKatalogFilter.cs`; dazu die drei Ressourcendateien.

**Neu in der Anwendung** (5): die fünf Hüllen.
**Geändert in der Anwendung** (10): `Allgemein/Blazor/BlazorAssistentSeite.cs`,
`Allgemein/Blazor/Sprungbruecke.cs`, `Allgemein/KI/HilfeKontext.cs`,
`Views/Wizard/IAssistentErzeugerSeite.cs`, `Views/Wizard/WizardParent.cs`,
`Views/Wizard/AssistentSeiten.cs`, `Views/Hauptformular/Form_Start.cs`,
`Views/Bedarf/BedarfErgebnisHuelle.cs`, `Views/Bedarf/TypStammHuelle.cs`,
`Views/Bedarf/GebaeudetypHuelle.cs`, `Dienste/WinFormsNavigation.cs` und die
vier Kontextmenü-Controller.

**Neu in EPOS.UI** außerdem: `Views/Wizard/IAssistentListenSeite.cs`
(Anwendung).

**Neu in den Tests** (8): fünf bunit-Klassen in `EPOS.UI.Tests/Dialoge/`,
`EPOS.Kern.Tests/FerienzeitTests.cs`,
`EPOS.Kern.Tests/GebaeudeKatalogTests.cs`,
`EPOS.Kern.Tests/ProjektlistenTests.cs`.

---

## 12. Windows-Abnahme 05.09.2026 (Befunde W9‑B‑1 bis W9‑B‑3)

Der Anwender hat den Stand `d3abd94` am Gerät gefahren und drei Dinge zum
Gebäudedialog gemeldet. Alle drei sind Oberflächenbefunde; der Rechenweg ist
unberührt (Referenzlauf nicht angefasst).

### 12.1 Befund W9‑B‑1 — „Im Projekt gespeichertes Gebäude wird nicht angezeigt bzw. in der Liste selektiert"

**Beobachtung.** Ein bestehendes Projekt im Assistenten öffnen
(`AssistentHuelle.Oeffnen(…, BETRIEBSART_BEARBEITEN)`), im linken Band das Projekt
markieren, zweimal „Weiter ▶" bis Seite 2 „Gebäude". Die Liste „ausgewählte Gebäude
im Projekt" zeigt das gespeicherte Gebäude **nicht markiert**; der Detailblock
„Gebäude: Verbrauch" steht auf einem Satz, der zu keiner sichtbaren Zeile gehört.

**Ursache — zwei Hälften, die zusammen den Befund ergeben.**

1. **`AssistentSeite.SchritteBauen` zog den Parametersatz der STEHENDEN Seite bei
   jedem `OnParametersSet` neu**, also bei jedem Neuzeichnen des Wirtes (Statuszeile,
   Sprachwechsel, der `AppWurzel`-Zweig auf iOS). Der Kopfkommentar der Seite sagt
   „bei JEDEM Betreten neu erfragt" — gemeint war das Betreten, gebaut war jedes
   Zeichnen. Die Hüllen bauen in `Gaben` aber jedesmal eine **neue** Anzeigeliste aus
   ihrer Fachliste auf (`GebaeudeHuelle.Gaben` :113‑114, und ebenso die zehn
   Geschwister). Der lebenden Komponente wurde die Liste damit unter den Füßen
   ausgetauscht.
2. **`GebaeudeDialog` machte seine Markierung an der OBJEKTGLEICHHEIT fest**
   (`z == _gewaehlt`) und stellte sie **nur in `OnInitialized`** her. Nach einem
   Austausch zeigte `_gewaehlt` auf ein Objekt, das in der neuen Liste nicht mehr
   steht — keine Zeile trug `epos-zeile--markiert`, und `_gewaehlt` war trotzdem
   nicht `null`: der Detailblock, „Ändern" und „▶" hingen an einer toten Zeile.
   Dieselbe Stelle trägt die zweite Hälfte des Befundes: Kommt die Projektliste erst
   **nach** dem ersten Zeichnen (der Ladeweg `AssistentCtrl.Laden` läuft im
   `SeiteVerlassen` der Projektkopfseite), blieb der Dialog für immer ohne
   Markierung.

**Nachgestellt.** Ein bunit-Lauf über `AssistentSeite` mit den Seitengaben eines
Projekts mit einem gespeicherten Gebäude: markieren, zweimal „Weiter", dann den Wirt
neu zeichnen lassen. Vorher: `SeiteGaben(2)` zweimal gerufen, `epos-zeile--markiert`
weg. Nachher: einmal gerufen, Markierung steht.

**Behebung.**

* `EPOS.UI/Seiten/Assistent/AssistentSeite.razor` merkt sich den Seiteninhalt
  (`_inhalt` / `_inhaltSchritt` / `_inhaltQuelle`) und erfragt ihn nur noch, wenn der
  **Schritt wechselt** (`BeiSchritt`), ein **anderes Projekt markiert** wird
  (`BeiMarkierung`) oder der Wirt einen **anderen Gabendelegaten** hereinreicht. Das
  ist genau das, was der Kopfkommentar seit W16a.5 behauptet — und was
  `WizardParent.Next` tat, während `WizardParent.Back` die Seite gar nicht neu
  bestückte.
* `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor` vergleicht über die **`IdZ`**
  (`Z_ProjektGebaeude.ID` — derselbe Schlüssel, an dem „▶" hängt, siehe § 4) und
  zieht die Markierung in `OnParametersSet` nach: dieselbe Zuordnung, sonst die erste
  Zeile, sonst keine. Steht der Anwender im **Katalog**, bleibt seine Wahl unberührt.

**Wachen.**
`EPOS.UI.Tests/Seiten/AssistentTests`:
`Der_Parametersatz_einer_Seite_wird_beim_Betreten_geholt_und_nicht_beim_Neuzeichnen`,
`Beim_Betreten_und_beim_Wiederbesuch_wird_der_Parametersatz_neu_geholt`,
`Ein_Projektwechsel_erfragt_den_Parametersatz_neu`.
`EPOS.UI.Tests/Dialoge/GebaeudeDialogTests`:
`Die_Markierung_ueberlebt_einen_Austausch_der_Zeilenliste`,
`Eine_spaeter_gefuellte_Projektliste_wird_markiert`,
`Eine_Katalogwahl_wird_vom_Nachziehen_nicht_ueberschrieben`.

**Abnahmepunkt A‑W9‑B‑1.** Bestehendes Projekt im Assistenten öffnen, Seite 2
„Gebäude": Das gespeicherte Gebäude steht in der linken Liste **und ist markiert**,
der Detailblock zeigt seine Werte. „▶" entfernt genau diese Zeile. Vor und zurück
über die Seiten behält Liste und Markierung.
