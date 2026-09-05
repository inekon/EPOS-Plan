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
| `ecbbdc0` | iU9-W9.8: 207 Textschlüssel für die acht Masken, de und en |
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
| **W9‑O‑8** — **erledigt** | Das Anordnungsschema der Projekt↔DB-Dialoge: neben- oder untereinander? Der Gebäudedialog und seine zwei Geschwister `WaermebedarfExternDialog` und `BedarfsProfileDialog` standen untereinander, die acht Dialoge der Wellen 6, 7 und 12 nebeneinander — zwei Schemata für dasselbe Muster, dazu elf eigene Markups | **Entscheid #76 (Anwender, 05.09.2026): nebeneinander wie im BHKW-PLAN, auf schmalem Schirm untereinander.** Umgesetzt für alle elf Dialoge über den gemeinsamen Baustein `EPOS.UI/Bausteine/Zweispaltenauswahl.razor`; siehe den Abschnitt „Anwenderentscheid #76" am Ende dieses Protokolls |

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

### 12.2 Befund W9‑B‑2 — „Liste zu lang"

**Beobachtung.** Die Liste „Gebäude in DB" läuft unbegrenzt in die Länge; die Seite
wird meterlang. Filter, Detailblock „Gebäude: Verbrauch" und die Schlussleiste
stehen erst weit unterhalb des Sichtfensters — um an „OK" zu kommen, muss der
Anwender die ganze SEITE rollen. Dasselbe gilt für jede andere Katalogliste der
Projekt↔DB-Dialoge.

**Ursache.** Die Hüllenklasse `.epos-raster-huelle` rollte nur **waagerecht**
(`overflow-x: auto`, Befund vom 03.09.2026, BHKW-Wirtschaftlichkeit in 914 px
Breite). Senkrecht wuchs jede Tabelle mit ihrem Bestand. Eine Höhenbegrenzung gab
es nur an `--hoch`, und die war ausdrücklich für die **virtualisierten** Listen
gedacht (20 746 CEC-Zeilen, iU9‑W13.0l) — für alle anderen also nirgends.

**Anwenderregel.** *Listen stehen in einem festen Rahmen mit Rollbalken.*

**Behebung — an EINER Stelle, nicht in zwanzig Dialogen.** Die DB-Listen tragen
weder `Zeilenwahl` noch `Zeilenraster` noch `ProjektListe`; sie stehen in drei
Bauarten nebeneinander — handgeschriebene `<table class="epos-raster">` (Gebäude,
Stromganglinie, Solarkollektoren, Wärmepumpe, Wärmebedarf extern, Bedarfsprofile,
Gebäudetyp), das QuickGrid des Bausteins `Raster` (Heizkessel, BHKW, Photovoltaik,
Stromspeicher, Pufferspeicher) und die `ProjektListe`. **Gemeinsam ist ihnen genau
eines: die Hüllenklasse `.epos-raster-huelle`.** Dort steht die Regel jetzt:

| Was | Wert | Warum |
|---|---|---|
| `max-height` | `var(--epos-listenhoehe)` = **22 rem** | rund neun Zeilen samt Kopf; passt in das kleinste Dialogmaß des Bestands (520 × 360). In `rem`, damit sie mit der Schriftgröße mitwächst |
| `overflow` | `auto` | senkrecht **und** waagerecht — die waagerechte Rolle bleibt, wie sie war |
| `thead th` | `position: sticky; top: 0` | eine gerollte Liste ohne stehenden Spaltenkopf ist nicht mehr zuzuordnen |
| `--frei` | `max-height: none` | der **benannte Rückweg** für eine Tabelle, die als Ganzes gelesen wird. Heute setzt ihn kein Wirt |

Es ist eine **Höchsthöhe**: Eine Liste mit drei Zeilen bleibt drei Zeilen hoch —
kurze Listen werden nicht künstlich hoch. Der **Tastaturfokus** rollt die markierte
Zeile von selbst ins Bild, weil jede Zeilenwahl ein `<button>` ist und der Browser
ein fokussiertes Element in seinen Rollbehälter zieht; dafür braucht es kein
JavaScript (und diese Bibliothek hat außer dem Gesprächsverlauf keines).

Dazu **ein Parameter mit sinnvoller Vorgabe** an den zwei Bausteinen, die die Hülle
selbst zeichnen: `Raster.Begrenzt` und `ProjektListe.Begrenzt`, beide `true`.

**Wache.** `EPOS.UI.Tests/ListenrahmenTests` (8 Fälle) prüft **die Regel im
Stilblatt** (Token in `:root`, Höchsthöhe, Rollbalken, stehender Kopf, `--frei`)
**und das Markup**, das sie treffen muss (Raster, ProjektListe, beide Listen des
Gebäudedialogs). Eine bunit-Probe allein sieht eine Stilregel nicht — Lehre
W6‑B‑1.

**Abnahmepunkt A‑W9‑B‑2.** Gebäudedialog mit einem vollen Katalog: Beide Listen
stehen in einem Rahmen von rund 350 px, der Spaltenkopf bleibt beim Rollen stehen,
Filter und Detailblock sind ohne Seitenrollen erreichbar. Eine Liste mit zwei
Zeilen ist zwei Zeilen hoch. Dasselbe bei Stromganglinien, Solarkollektoren,
Wärmepumpe, Heizkessel-/BHKW-/PV-/Speicherverwaltung und in den drei
Projektdialogen.

### 12.3 Befund W9‑B‑3 — „nicht so recht klar, auf was sich die oberen 2 Buttons beziehen"

**Beobachtung.** Zwischen der Projektliste und der DB-Liste des Gebäudedialogs stehen
zwei Knöpfe mit den blanken Zeichen **◀** und **▶**. Der Anwender kann ihnen nicht
ansehen, worauf sie sich beziehen.

**Ursache.** Die Zeichen sind aus `Form_Gebaeude` unverändert übernommen — und dort
sagten sie die Wahrheit: Der Vorläufer stellte die beiden Listen **nebeneinander**
(links „ausgewählte Gebäude im Projekt", rechts „Gebäude in DB", dazwischen die
Pfeilspalte), und „nach links" hieß dann „in das Projekt". In der Razor-Fassung stehen
die beiden Listen **untereinander**: Der Behälter des Dialogs heißt
`epos-auswahlspalten`, und für diesen Klassennamen gibt es keine Stilregel — die zwei
`epos-auswahlspalte`-Blöcke stapeln sich als gewöhnliche Blockelemente. (Die Reihe mit
der Pfeilspalte ist `epos-auswahlpaar`/`epos-auswahlpfeile`, das Muster der fünf
Erzeugerdialoge aus Welle 6.) Ein **waagerechter** Pfeil zeigt bei untereinander
stehenden Listen ins Leere.

**Behebung.** Beide Knöpfe tragen ihre Aufgabe im **Klartext** und dazu einen
**Kurztext** (`title`), der die Herkunft der Zeile nennt; das Zeichen zeigt in die
Richtung, in die die Zeile wandert.

| Knopf | Beschriftung | Kurztext |
|---|---|---|
| übernehmen | `GEB_BTN_UEBERNEHMEN` — „▲ In das Projekt übernehmen" / „▲ Add to project" | `GEB_BTN_UEBERNEHMEN_HINWEIS` — „Das in „Gebäude in DB" markierte Gebäude in die Projektliste übernehmen" |
| entfernen | `GEB_BTN_ENTFERNEN` — „▼ Aus dem Projekt entfernen" / „▼ Remove from project" | `GEB_BTN_ENTFERNEN_HINWEIS` — „Das in der Projektliste markierte Gebäude aus dem Projekt entfernen" |

Vier Schlüssel in **beiden** Sprachkatalogen (`EPOS.Kern/MyResource/Resource.resx` und
`…en-US.resx`, dazu die vier Eigenschaften im `Resource.Designer.cs`).

**Was ausdrücklich NICHT geändert ist: das Anordnungsschema.** Ob die zwei Listen
neben- oder untereinander stehen, ist ein **offener Anwenderentscheid (#76)**. Diese
Änderung macht die Knöpfe nur bei der HEUTIGEN Anordnung verständlich; entscheidet der
Anwender sich für nebeneinander, wechseln die zwei Zeichen wieder auf ◀/▶ — die zwei
Ressourcenwerte, sonst nichts.

**Die zwei Geschwister bleiben vorerst.** `WaermebedarfExternDialog` und
`BedarfsProfileDialog` stehen in derselben Bauart (`epos-auswahlspalten` mit ◀/▶) und
haben denselben Befund. Sie sind hier bewusst nicht angefasst: Der Anwenderentscheid
#76 gilt für alle drei gemeinsam, und `BedarfsProfileHuelle` liegt in derselben
Sitzung bei einem anderen Bearbeiter. **Offener Punkt W9‑O‑8** — mit dem Anwenderentscheid #76 vom selben Tag **geschlossen**: Alle drei stehen seither im Baustein `Zweispaltenauswahl` und wieder nebeneinander (Abschnitt „Anwenderentscheid #76" am Ende dieses Protokolls).

**Wachen.** `EPOS.UI.Tests/Dialoge/GebaeudeDialogTests`:
`Die_zwei_Richtungsknoepfe_sagen_was_sie_tun` (Klartext, ▲/▼, kein ◀/▶ mehr im Markup),
`Die_zwei_Richtungsknoepfe_tragen_einen_Kurztext`.

**Abnahmepunkt A‑W9‑B‑3.** Gebäudedialog auf Deutsch **und** auf Englisch: Beide
Knöpfe tragen ihren Satz, das Zeichen passt zur Anordnung, der Kurztext erscheint beim
Verweilen. Der Knopf bleibt gesperrt, solange in der jeweils anderen Liste nichts
markiert ist.

## Windows-Abnahme 05.09.2026 — Bedarfsrechnung

> Kennungen abgestimmt mit dem parallelen Port der Assistenten- und
> Projektdialoge, der **W9‑B‑1 … W9‑B‑3** führt. Beide Befunde dieses Abschnitts
> haben **eine** Ursache und **eine** Behebung; sie stehen getrennt, weil der
> Anwender sie an zwei Kacheln gesehen hat.

### W9‑B‑4 — Prozesswärme: „Simulation bringt Ergebnis 0 (monatlicher Verlauf), Grafik bleibt leer"
### W9‑B‑5 — Standardlastprofil: „gleich wie Prozesswärme"

**Behoben mit `b8090b0`** (Kern), Zeuge `66c80b6`, auf dem Stand `d3abd94`.
Der Ergebnisdialog selbst ist entlastet — siehe **W8‑B‑1** im Protokoll der
Welle 8.

**Meldung** (PDF „iOS_Migration_Probleme", S. 4–5). Kachel „Prozesswärme" →
Dialog „Prozesswärme" → „Simulation…" → Überlagerung **Bedarfsergebnis**: Die
zwölf Monatswerte stehen auf 0, das Bild „Prozesswärme [MWh]" zeigt leere Achsen
0–5. Dasselbe an der Kachel **„Standardlastprofil"** (W9‑B‑5).

**Nicht die Ursache: die Einheit.** Der Verdacht lag auf den beiden Nachträgen
vom 04.09.2026 (W8‑O‑5 `e665c41`, W9‑O‑3 `a3906ca`) — eine doppelt angewandte
Umrechnung kWh→MWh ergäbe 10⁻⁶ und damit gerundet 0. Sie ist es nicht:
`ProzesssummeUebernehmen()` wird **genau einmal** gerufen
(`BedarfsProfileHuelle`:398), `Energieeinheit.MWh.AusMWh` ist bitgleich die
Identität, und der Prozesszweig rechnet mit dem Projekt 1041 der Testdatenbank
korrekt 30 MWh und 2,548 MWh im Januar. Die Reihe ist nicht zu klein — **sie ist
leer**.

**Ursache: die Namensauflösung.** Der Dialog listet die Zuordnungen des Projekts,
und ihre Namen kommen aus der **Projektkopie** — `Z_Projekt*Ctrl.LiesProjekt`
liest `Tab_Prozesswaerme.Bezeichner` bzw. `Tab_Stromverbraucher.Bezeichner`, nicht
`Z_Projekt_*.Bezeichner`. Eine Projektkopie heißt aber nicht zwingend wie ihr
Katalogeintrag: In `Referenzlaeufe/Kenndaten_Test.sqlite` tragen **acht** von
ihnen den Zusatz „ (P‹Projekt›)".

Die Vorschau schlug diesen Namen bis hierher **ausschließlich im
`_STAMM`-Katalog** nach. Der Grund ist die Ableitung, die in allen drei
Bedarfszweigen stand:

```csharp
ProfilQuellmodus modus = (list == null) ? ProfilQuellmodus.Projektrechnung
                                        : ProfilQuellmodus.Katalogvorschau;
```

Genau das nennt der Kopf von `ProfilQuellmodus` seit V0‑4 „zweierlei in einer
Angabe": Ob eine **Namensliste** mitkommt, sagt nichts darüber, ob die Namen aus
einem **Katalog** oder aus einem **Projekt** stammen. `KopfLesen` fand nichts, und
weil die Katalogvorschau bewusst **still** übergeht („dort ist die Auswahl des
Anwenders die Ursache, nicht die Projektdatenlage"), gab es nicht einmal eine
Protokollwarnung. Übrig blieb eine Nullreihe — und an ihr hängen Kennzahl,
Monatstabelle und Säulenbild gleichermaßen.

Gemessen an Projekt 1017 (Zuordnung `EFH_3_Pers (P1017)`, W9‑B‑5):

| Weg | Summe | Januar |
|---|---|---|
| Vorschau des Dialogs (Katalogvorschau) | **0** | **0,000** |
| Projektlauf (`list == null`) | 672 000,4 kWh | 67,462 MWh |

Eine Vorschau, die etwas anderes zeigt als der Lauf, ist keine.

**Behebung.** Die Regel steht jetzt einmal, in
`ProfilBedarf.Vorschaumodus(namen, idProjekt)`:

| Aufruf | Modus | |
|---|---|---|
| ohne Liste | `Projektrechnung` | unverändert — hier hängt der Referenzlauf |
| mit Liste, **ohne** Projekt | `Katalogvorschau` | unverändert — die drei Katalogverwaltungen |
| mit Liste **und** Projekt | `Projektvorschau` | **neu** — der Bedarfsprofil-Dialog (Reihenfolge seit W9‑O‑3c: Kopie zuerst) |

`Projektvorschau` liest **beide** Quellen — die Liste des Dialogs ist **gemischt**:
Eine gespeicherte Zuordnung trägt den Namen ihrer Projektkopie, eine eben erst
aufgenommene Zeile den ihres Katalogeintrags; deren Kopie entsteht erst beim
Speichern (`WizardCtrl.Add_Projekt_*` → `CopyFromStamm`).

Welche der beiden zuerst kommt, ist der **Anwenderentscheid W9‑O‑3c** (unten). Die
erste Fassung las den Katalog zuerst und fiel auf die Projektkopie zurück, damit
jede damals richtige Zahl zeichengleich blieb; **seit dem Entscheid vom 05.09.2026
ist es umgekehrt** — Projektkopie zuerst, Katalog als Rückfall
(`ProfilQuelle.Rueckfall`). Wird der Rückfall gezogen, liefert er **Kopf und
Typprofil** — ihre Vermischung war der Befund V0‑4. Der Kalender bleibt bei der
Altkonvention wie in der Katalogvorschau: `WochentagJan1` entsteht erst in
`Waermebedarf_berechnen` aus den geladenen Klimadaten, die der Dialog nie lädt.

**Die Grafik war nur die Folge.** `ChartRenderer.MonatsSaeulen` nimmt bei einer
reinen Nullreihe `maxWert = 0`, bekommt daraus die Vorgabeskala 0–5 und zeichnet
zwölf Säulen der Höhe 0 — genau das Bild des Bildschirmfotos. Der Renderer ist
**unberührt** geblieben; `Proben/ChartProben` bleibt bei 32 Bildern und
0 Verstößen.

**Wache.** `EPOS.Kern.Tests/BedarfsProfilVorschauTests.cs`, fünf Fälle — drei auf
dem Bestand rot (Projekt 1017 gegen den Projektlauf, seine zwölf Monatswerte, und
dieselbe Ausprägung an der **Prozesswärme** über eine umbenannte Projektkopie),
zwei als eingefrorene Wache: `Bekannte_Katalognamen_liefern_unveraenderte_Monatswerte`
(Prozesswärme 1041, Brauchwasser 1007, Stromverbraucher 1024) und
`Die_Katalogverwaltung_rechnet_unveraendert_ohne_Projekt`. Der schreibende Fall
steht in einer eigenen Klasse — `TestDatenbank` gibt jeder Klasse ihre eigene
Arbeitskopie. **Mit W9‑O‑3c sind es sieben** (siehe dort).

**Referenzlauf byte-gleich.** `EPOS.Referenzlauf lauf` + `vergleich` gegen
`Referenzlaeufe/2026-08-30_B3-Kaskade`: **alle elf rechenbaren Projekte der Basis
PASS** (1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042), dazu
`diff -r` über die CSV-Ordner ohne Unterschied. 1011 und 1021 stehen nicht mehr in
`Tab_Projekt` der Testdatenbank — das ist Bestand und unabhängig von dieser
Änderung.

**Abnahmepunkte (Windows).**

| # | Weg | Erwartung |
|---|---|---|
| 1 | Projekt öffnen → Kachel **„Prozesswärme"** → Eintrag wählen → „Simulation…" | Reiter „Übersicht monatlich" zeigt **Werte ≠ 0**, Reiter „Grafik" zeigt eine **Säulenkurve** (W9‑B‑4) |
| 2 | dort auf **kWh** umschalten und zurück auf **MWh** | Zahlen und Bild folgen der Wahl; MWh bleibt die Vorgabe |
| 3 | Kachel **„Standardlastprofil"** → „Simulation…" | dito für den Strombedarf (W9‑B‑5) |
| 4 | Kachel **„Brauchwasser"** → „Simulation…" | Zahlen **unverändert** gegenüber dem Stand vor der Behebung — **überholt durch W9‑O‑3c**, siehe A‑W9‑O‑3c unten |
| 5 | Menü **Administration** → die drei Katalogverwaltungen → „Grafik" | Zahlen **unverändert** — sie öffnen ohne Projekt |
| 6 | einen Katalogeintrag neu aufnehmen und **vor** dem Speichern „Simulation…" | rechnet wie bisher aus dem Katalog |

### W9‑O‑3c — Anwenderentscheid „Empfehlung" (05.09.2026): die Projektkopie zuerst

**Entschieden am 05.09.2026: „Empfehlung"** — die Vorschau des
Bedarfsprofil-Dialogs bevorzugt die **Projektkopie**; der Katalog bleibt der
Rückfall. **Umgesetzt im Kern** (`ProfilBedarf`/`ProfilQuelle`).

**Die Frage.** Eine im Projekt **geänderte** Kopie wurde in dieser Vorschau mit der
**Katalogverteilung** angezeigt, weil der Katalog zuerst gelesen wurde. Beispiel
Brauchwasser, Projekt 1007 („Haushalt-3"): Katalog 1,900 MWh im Januar,
Projektkopie 0,552 MWh — die Jahressumme ist in beiden Fällen dieselbe (4 059,7),
nur die Verteilung über die Monate nicht. Der **Projektlauf** rechnet mit der
Projektkopie. Die Reihenfolge umzudrehen bringt die Vorschau überall mit dem Lauf
zur Deckung, **ändert aber angezeigte Zahlen** in jedem Projekt mit bearbeiteter
Kopie — kein Fehlerfall, sondern eine Entscheidung, deshalb vorgelegt statt
nebenbei getroffen.

**Die neue Regel.** Der Modus `Projektvorschau` liest **zuerst die PROJEKTKOPIE**
(dieselben Tabellen und denselben Projektfilter wie der Lauf) und fällt für einen
Namen, den das Projekt nicht kennt, auf den **`_STAMM`-Katalog** zurück. Der
Rückfall trägt damit genau den Fall, für den es ihn braucht: die eben aufgenommene,
noch nicht gespeicherte Zeile, die den Namen ihres Katalogeintrags führt (ihre
Kopie entsteht erst beim Speichern). Kopf **und** Typprofil kommen weiterhin aus
derselben Quelle (V0‑4), der Kalender bleibt die Altkonvention. Die Modi
`Projektrechnung` und `Katalogvorschau` sind unberührt.

**Vorher / nachher** — Brauchwasser, Projekt 1007 („Haushalt-3"):

| | Januar | Februar | Jahr |
|---|---|---|---|
| vorher (Katalog zuerst) | 1,900 MWh | 0,340 MWh | 4 059,7 kWh |
| **nachher (Kopie zuerst)** | **0,552 MWh** | **0,553 MWh** | 4 059,7 kWh |

Die Jahressumme ist unverändert: Sie kommt aus `Z_Projekt_Brauchwasser` und wird
auf beide Verteilungen gleich aufskaliert. Prozesswärme 1041 und
Stromverbraucher 1024 bleiben **zeichengleich** — dort tragen Kopie und Katalog
dieselben zwölf Monatswerte.

**Wache.** `EPOS.Kern.Tests/BedarfsProfilVorschauTests.cs` führt jetzt **sieben**
Fälle. Neu sind `Brauchwasser_Vorschau_zeigt_die_Verteilung_der_Projektkopie`
(auf dem Bestand **rot**: Januar 0,552 statt 1,900 MWh bei gleicher Jahressumme)
und `Ein_nur_im_Katalog_bekannter_Name_kommt_aus_dem_Katalog` („Haushalt-3 neu"
steht im Katalog und in keiner Projektkopie von 1007 — Januar 0,400 MWh, Jahr
2,5597 MWh, ohne Skalierung). Nachgezogen ist
`Bekannte_Katalognamen_liefern_unveraenderte_Monatswerte`: Ihr Satz „der Katalog
bleibt die erste Quelle" ist mit dem Entscheid Geschichte; sie führt nur noch die
zwei Proben, in denen Kopie und Katalog übereinstimmen.

**Referenzlauf byte-gleich.** `EPOS.Referenzlauf lauf` + `vergleich` gegen
`Referenzlaeufe/2026-08-30_B3-Kaskade`: **alle elf rechenbaren Projekte PASS**
(1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042), dazu `diff -r`
über die CSV-Ordner ohne Unterschied. Die Vorschau ist nicht Teil des Laufs — genau
das zeigt der Vergleich.

**Abnahmepunkt A‑W9‑O‑3c (Windows).**

| # | Weg | Erwartung |
|---|---|---|
| 1 | Projekt **1007** öffnen → Kachel **„Brauchwasser"** → „Simulation…" → Reiter „Übersicht monatlich" | **Januar 0,552 MWh** (vorher 1,900), Jahressumme unverändert; dieselbe Zahl wie im Simulationslauf |
| 2 | im selben Dialog eine Zeile **neu aufnehmen** und **vor** dem Speichern „Simulation…" | rechnet aus dem Katalog wie bisher |

## Anwenderentscheid #76 (05.09.2026) — ein Schema für alle Projekt↔DB-Auswahldialoge

**Der Entscheid.** Nach der Windows-Abnahme (PDF „iOS_Migration_Probleme", S. 2, 6–8)
hat der Anwender festgelegt: *Alle* Dialoge, in denen links „im Projekt ausgewählt" und
rechts „aus der Datenbank/Katalog" mit Pfeilknöpfen dazwischen stehen, folgen dem alten
**BHKW-PLAN-Schema NEBENEINANDER** — Projektliste links, Katalogliste rechts, die zwei
Pfeilknöpfe in einer schmalen Mittelspalte. Auf **schmalem Schirm** (iPad hochkant,
schmales Fenster) bricht das Paar automatisch **untereinander** um; dann gilt das
Schema, das der Gebäudedialog seit Welle 9 hatte (Projektliste oben, Pfeile dazwischen,
Katalog unten). Listen sind in beiden Fällen höhenbegrenzt mit Rollbalken (Befund
W9‑B‑2, `.epos-raster-huelle` / `--epos-listenhoehe`).

**Ein Baustein statt elf Markups.** `EPOS.UI/Bausteine/Zweispaltenauswahl.razor` trägt
drei benannte Bereiche — `Links` (Projekt), `Mitte` (die zwei Knöpfe), `Rechts`
(Katalog) — dazu die Überschriften, die vier Texte der Knöpfe, ihre Sperrzustände und
Rückrufe sowie `NurRechts` für die Verwaltungsbetriebsart. Der Stilblock
„Zweispaltenauswahl" in `EPOS.UI/wwwroot/epos-ui.css` steht direkt hinter dem alten
Block AUSWAHLPAAR; die alte Klasse `.epos-auswahlpfeile` ist entfallen,
`.epos-auswahlpaar`/`.epos-auswahlspalte` bleiben für die fünf Masken **ohne** Pfeile
(`GebaeudetypDialog`, `TypProfilDialog`, `KennlinienEditorDialog`,
`WaermepumpeAnlageDialog`, `WaermepumpeStammDialog`).

**Das Zeichen hängt an der Anordnung, nicht am Text.** Ein Pfeil im Ressourcentext kann
nicht wissen, wie die Listen gerade stehen. Jeder Knopf trägt deshalb **beide** Zeichen
im Markup (`aria-hidden`, damit eine Sprachausgabe den Satz liest und nicht das
Dreieck), und das Stilblatt zeigt je Breite genau eines: nebeneinander **◀/▶** (die
Zeile wandert nach links ins Projekt bzw. nach rechts in den Katalog zurück),
untereinander **▲/▼**. Kein JavaScript.

> **Zur Pfeilrichtung.** Der Entscheidtext nennt in der Klammer „▶ In das Projekt
> übernehmen, ◀ Aus dem Projekt entfernen". Umgesetzt ist es **umgekehrt** — ◀
> übernimmt, ▶ entfernt —, weil derselbe Satz die Projektliste ausdrücklich **links**
> verortet und weil das Vorbild es so hält: `Form_Gebaeude.resx` `btn_Hinzu` = „◀",
> `btn_Entfernen` = „▶"; `Form_Heizkessel.resx` `btn_Kessel_Hinzu` = „◀",
> `btn_Kessel_Entfernen` = „▶". Bei Projektliste links zeigt „übernehmen" nach links.
> Soll es doch andersherum sein, sind es zwei Zeichen in
> `Bausteine/Zweispaltenauswahl.razor` — sonst nichts.

**Der Umbruch ist eine Medienabfrage, kein `flex-wrap`.** Nur so weiß das Stilblatt,
welches Zeichen gerade gilt; bei `flex-wrap` käme die Reihe um, ohne dass eine Regel es
merkt, und die Pfeile zeigten ins Leere. Die Umbruchbreite steht als Token
`--epos-zweispalten-umbruch` (900 px) **und** — weil eine Medienabfrage kein Token
lesen kann — ein zweites Mal in der Abfrage; die Wache
`ZweispaltenauswahlTests.Die_Umbruchbreite_steht_als_Token` hält beide Werte
gegeneinander. Die Breite der Mittelspalte ist `--epos-zweispalten-mitte` (10 rem; im
Bestand 63 px bei `Form_Gebaeude`, 88 px bei `Form_Heizkessel` — hier etwas mehr, weil
die Knöpfe seit Befund W9‑B‑3 ihre Aufgabe im Klartext tragen).

**Texte.** Neu in beiden Sprachkatalogen und im `Resource.Designer.cs`:
`AUSWAHL_BTN_UEBERNEHMEN`, `AUSWAHL_BTN_UEBERNEHMEN_HINWEIS`, `AUSWAHL_BTN_ENTFERNEN`,
`AUSWAHL_BTN_ENTFERNEN_HINWEIS`, `AUSWAHL_GRP_PFEILE` (der Name der Knopfgruppe für die
Sprachausgabe). Aus `GEB_BTN_UEBERNEHMEN` / `GEB_BTN_ENTFERNEN` sind die Zeichen
**▲/▼ entfernt**; die acht nebeneinander stehenden Dialoge nehmen weiter
`HZK_TIP_HINZU` / `HZK_TIP_ENTFERNEN` — jetzt als **Beschriftung** statt nur als
Kurztext.

**Tastaturweg und Sprachausgabe.** Die drei Bereiche stehen in der Reihenfolge links –
Mitte – rechts im Markup; der Tabulator läuft damit von der Projektliste über die zwei
Knöpfe in den Katalog. Jede Spalte ist eine `role="group"` mit ihrer Überschrift als
`aria-label`, die Knopfgruppe ebenso.

### Vorbild → Umsetzung je Dialog

| Dialog | Vorbild (Geometrie im `.resx`) | Umsetzung |
|---|---|---|
| `Bedarf/GebaeudeDialog` | `Form_Gebaeude`: `listView_Gebaeude` x=26 **w=252** („ausgewählte Gebäude im Projekt:"), `btn_Hinzu` „◀" x=284 **w=63**, `btn_Entfernen` „▶" x=293, `dataGridView1` x=364 **w=436** („Gebäude in DB:"); `groupBox1` „Filter Gebäude DB" und die vier DB-Knöpfe **unter/rechts** vom Katalog, `groupBox2` „Gebäude: Verbrauch" unten links | Von untereinander auf `Zweispaltenauswahl` **umgestellt**. Der Filterblock steht **über** der Katalogliste in der rechten Spalte (dort steht die Liste, auf die er wirkt), die vier DB-Knöpfe darunter, der Detailblock unter dem Paar über die volle Breite. Die Verwaltungsbetriebsart (`Form_Gebaeude_Load:608‑620`) wird zu `NurRechts`: linke Spalte und Pfeile weg, Katalog über die ganze Breite |
| `Bedarf/WaermebedarfExternDialog` | `Form_Waermebedarf`: links „Ausgewählt im Projekt" mit der Kanalklappliste darunter, rechts „Wärmebedarf aus DB", dazwischen ◀/▶ | Von untereinander auf `Zweispaltenauswahl` umgestellt. Die **Kanalklappliste bleibt in der linken Spalte** — sie wirkt auf die markierte Projektzeile, nicht auf den Katalog |
| `Bedarf/BedarfsProfileDialog` | `Form_Prozesswaerme` / `Form_Stromverbraucher` / `Form_Brauchwasser`: zwei Listen, dazwischen ◀/▶, darunter der Infoblock | Von untereinander auf `Zweispaltenauswahl` umgestellt; die vier DB-Knöpfe unter der Katalogliste, Infoblock und Simulationsteil unter dem Paar |

**Damit ist der offene Punkt W9‑O‑8 geschlossen.** Er lautete: „Die zwei Geschwister
`WaermebedarfExternDialog` und `BedarfsProfileDialog` stehen in derselben Bauart und
haben denselben Befund; der Anwenderentscheid #76 gilt für alle drei gemeinsam." Alle
drei sind umgestellt.

**Nachtrag zu Befund W9‑B‑3.** Der Abschnitt 12.3 hielt fest, das Anordnungsschema
bleibe unberührt und „entscheidet der Anwender sich für nebeneinander, wechseln die zwei
Zeichen wieder auf ◀/▶ — die zwei Ressourcenwerte, sonst nichts." Das ist die Stelle, an
der es genau **nicht** bei zwei Ressourcenwerten bleiben durfte: Weil die Anordnung
seither von der Fensterbreite abhängt, kann kein fester Text das richtige Zeichen
tragen. Die Zeichen sind deshalb aus `GEB_BTN_UEBERNEHMEN` / `GEB_BTN_ENTFERNEN`
entfernt und stehen im Baustein; Beschriftung und Kurztext aus W9‑B‑3 bleiben Wort für
Wort erhalten.

### Wachen

`EPOS.UI.Tests/Bausteine/ZweispaltenauswahlTests` (14 Fälle) prüft drei Ebenen: den
**Baustein** (Reihenfolge der drei Bereiche = Tastaturweg, `aria`-Beschriftungen, beide
Zeichen je Knopf mit `aria-hidden`, Klartext, Kurztext, Sperrzustände, Rückrufe,
`NurRechts`), die **Regel im Stilblatt** (nebeneinander ist die Vorgabe, kein
`flex-wrap`, Token gegen Medienabfrage, je Anordnung genau ein Zeichen) und den
**Bestand** (alle elf Projekt↔DB-Dialoge nehmen den Baustein; keine Komponente baut die
Pfeilspalte noch selbst). Eine bunit-Probe sieht eine Stilregel nicht — Lehre W6‑B‑1.

### Abnahmepunkte A‑#76

1. **Breit** (Fenster ≥ 900 px): Projektliste **links**, Katalog **rechts**, die zwei
   Knöpfe in einer schmalen Spalte dazwischen; die Zeichen sind ◀ (übernehmen) und ▶
   (entfernen).
2. **Schmal** (Fenster < 900 px, iPad hochkant): Projektliste **oben**, Knöpfe
   darunter nebeneinander, Katalog **unten**; die Zeichen sind ▲ und ▼.
3. **Listen begrenzt**: Beide Listen rollen in ihrem Rahmen, der Spaltenkopf bleibt
   stehen; Filter, Detailblock und Schlussleiste bleiben erreichbar, ohne die ganze
   Seite zu rollen.
4. **Knöpfe**: Beide tragen ihren Satz im Klartext — auf Deutsch **und** auf Englisch —
   und einen Kurztext, der die Herkunft der Zeile nennt. Jeder bleibt gesperrt, solange
   in der jeweils anderen Liste nichts markiert ist.

## Anwenderwunsch W9‑E‑2 (05.09.2026) — „Simulation…" im Gebäude-Dialog

**Der Wunsch, wörtlich.** „Der Wärmebedarf vom Gebäude (Screenshot 1) sollte aus diesem
Dialog (mit Button Simulation) aufgerufen werden können — analog wie aus dem
Simulationsbereich (Screenshot 2)." Nachtrag desselben Tages: „(ohne Brauchwasser und
ohne gesamt)".

* **Screenshot 1** ist `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`, Detailblock
  **„Gebäude: Verbrauch"** — Gebäudename, Gebäudeart, Beschreibung, Wohn-/Nutzfläche,
  Art der Angabe und der Knopf **„Ändern"**.
* **Screenshot 2** ist der Reiter **Wärme-/Strombedarf** der Ergebnisseite
  (`EPOS.UI/Seiten/Simulation/BedarfReiter.razor`): Kennzahlen, der Block „Wärmebedarf
  je Bedarfsart", die Schalter „sortiert / Gesamt / Heizung / Brauchwasser" und darunter
  die **Wärmelast-Jahresganglinie** (Renderer-Bild B1).

Umgesetzt als **Teilwelle W9.8** (die Nummern W9.6 und W9.7 sind vergeben).

| Hash | Betreff |
|---|---|
| `b429d36` | iU9-W9.8: Wärmebedarf EINES Gebäudes im Kern |
| `3df47c8` | iU9-W9.8: Knopf „Simulation…" im Gebäudedialog |
| `d9c7ee1` | iU9-W9.8: bunit-Fälle zum Gebäudebedarf |

### Das Vorbild — es gibt keines

`Form_Gebaeude` (gelöscht mit iU9‑W9.2, Commit `f960151`) trug im Detailblock **nur**
`btn_Aendern`; ein Knopf „Simulation" kommt in ihrem Designer nicht vor. Die einzige
Maske des Bestands mit diesem Namen war
`Views/Simulation/Form_Simulation_Kurz` — sie ist mit **iF29** (iU9‑W0.2) stillgelegt,
weil sie verwaist war, und rechnete etwas ganz anderes: `btn_Simulation_Click` las die
Konfiguration des PROJEKTS, prüfte Klimaregion und Kaskadenplätze, rechnete
`SimulationWaermebedarf` **und** `SimulationStrombedarf`, fuhr `Do_Simulation` und zeigte
Restwärme, Reststrom und eine Deckungstorte. Das ist der ganze Lauf, nicht ein Gebäude.

**Neu ist deshalb die AUSKUNFT, nicht die Rechnung.** Die Rechnung gab es längst — sie
stand nur in der Gebäudeschleife des Laufs und war von außen nicht aufrufbar.

### Der Rechenweg

Zwei Methoden von `SimulationWaermebedarf` sind aus `Waermebedarf_berechnen`
**herausgezogen**, Anweisung für Anweisung:

| Methode | Was sie trägt | Woher |
|---|---|---|
| `KlimakalenderLesen(idKlimaregion)` | die 365 Tagessätze (`Sol_*`, `A_Temp`, `WE`, `TagTyp_W/NW`), die 8 760 Stundentemperaturen und `WochentagJan1` | der Block `// if (!DBGelesen)` samt der F3-Zeile darunter |
| `HeizwaermeEinesGebaeudes(item, index, ziel)` | Bewohner/Fläche, `Berechnung_Gebaeude_Tageswerte`, `DBTagesVeteilung` und `StdWerte` in einen genullten Puffer | der Rumpf der Gebäudeschleife bis einschließlich `StdWerte` |

Die Schleife des Laufs ruft jetzt genau diese Methode; `false` heißt „keine
Tagesverteilung hinterlegt", und der Abbruch der Bedarfsrechnung bleibt an derselben
Stelle wie bisher. **Es ist eine Auslagerung, keine zweite Rechnung** — deshalb ist der
Referenzlauf byte-gleich.

**Der Index ist ein Merkplatz, kein Rang.** `index` trägt allein
`HeizwaermebedarfGeb[index]`, den Jahreswert für die Flächenrückrechnung in
`Bewohner_und_Flaeche_berechnen`. Eine Rechnung für EIN Gebäude nimmt 0 und bekommt
bitgleich dasselbe Ergebnis wie dieses Gebäude im Lauf.

**`EPOS.Kern/Controller/GebaeudeBedarfCtrl`** setzt beides zusammen: Klimakalender
lesen, das eine Gebäude aus `Abfrage_Projektgebaeude` heraussuchen, rechnen,
`WattToKw`, und daraus Jahressumme (MWh), Höchstlast (kW), zwölf Monatswerte (MWh) und
die Vollbenutzungsstunden (h/a). Ergebnistyp `GebaeudeBedarfErgebnis`.

**Der Schlüssel ist die ZUORDNUNG**, nicht die Stamm-Id: Zwei gleiche Gebäude im Projekt
teilen sich eine Stamm-Id, und der Dialog unterscheidet sie seit W9.2 über
`Z_ProjektGebaeude.ID` (`GebaeudeProjektZeile.IdZ`, Befund W9‑B‑1). Die Sicht
`Abfrage_Projektgebaeude` gibt diese Spalte nicht aus — sie führt `Tab_Gebaeude.ID` —,
deshalb gibt es **eine** neue Abfrage:
`SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?`. `ID_ProjektGebaeude` IST der
Verweis auf `Z_ProjektGebaeude.ID` (derselbe Weg wie `Z_ProjGebCtrl.LiesProjekt`), und
`Tab_Gebaeude.ID` ist zugleich der Schlüssel, mit dem der Lauf die Tagesverteilung sucht.

**Die Jahressumme steht als `werte.Sum() / 1000` da** — eine float-Division wie im Lauf
(`kanalHeizung.Sum() / 1000`), nicht `/ 1000.0`. Der Anwender legt die zwei Zahlen
nebeneinander; eine double-Division ergäbe eine andere neunte Stelle.

**Was NICHT eingeht**, weil es nicht am Gebäude hängt: die externen Wärmelastgänge
(`Z_ProjektWaermebedarf`), Brauchwasser- und Prozessprofile und die anteilig verteilten
Netzverluste. Die Zahl des Dialogs ist der reine Gebäudeanteil.

### Was der Anwender sieht

Im Detailblock „Gebäude: Verbrauch" steht neben „Ändern" der Knopf **„Simulation…"**.
Er ist frei, sobald ein Gebäude der **Projektliste** markiert ist, und geht als
**vierte Überlagerung** des Gebäudedialogs auf — kein zweites Fenster (Risiko R2). In
der Katalogverwaltung (`Masken.GebaeudeAdmin`) gibt es ihn nicht: Dort steht kein
Projekt hinter der Maske.

`EPOS.UI/Dialoge/Bedarf/GebaeudeBedarfDialog.razor` zeigt:

| Zeile | Einheit | Herkunft |
|---|---|---|
| Wärmebedarf Heizung | MWh (kWh wählbar) | `GebaeudeBedarfErgebnis.HeizwaermeMwh` |
| max. Wärmelast | kW | Höchstwert der Stundenreihe |
| Vollbenutzungsstunden | h/a | Jahresarbeit ÷ Höchstlast; ohne Höchstlast „—" |

darunter der Schalter **„sortiert"** (Jahresdauerlinie), die **Jahresganglinie** als
Renderer-Bild im Baustein `Diagramm` — Bildzoom und Datenzoom wie auf der Ergebnisseite
— und die **Monatsübersicht** mit zwölf Werten.

**Es ist dasselbe Bild wie B1 der Ergebnisseite.**
`ChartRenderer.GanglinieNormiert(…, Achse.Monate | Achse.Jahresstunden, sortiert,
fenster)` mit der Farbe `F_BEDARF` (Rot) — wörtlich der Aufruf aus
`SimulationErgebnisHuelle.BildBedarfWaerme`. Nur die Reihe ist eine andere: hier steht
**genau eine**, die Heizwärme dieses Gebäudes. **Kein neuer Renderer, kein neues Bild** —
`ChartProben` bleibt bei 34 + 2.

**Weg bleiben, wie ausdrücklich gewünscht** („ohne Brauchwasser und ohne gesamt"): der
Block „Wärmebedarf je Bedarfsart" und die Schalter „Gesamt", „Heizung", „Brauchwasser".
Der Dialog kennt EINE Reihe; der bunit-Fall
`Es_gibt_weder_Brauchwasser_noch_Gesamt` ist die Wache dafür.

**Die Einheit** folgt dem Anwenderentscheid **W8‑O‑5**: MWh als Vorgabe, kWh wählbar,
gemerkt in `BedarfEinheitWahl` — derselben Ablage wie im Bedarfsprofil- und im
Bedarfsergebnisdialog. Umgerechnet werden nur die Energiemengen; die Last bleibt kW, die
Vollbenutzungsstunden bleiben Stunden (Hausregel: eine Energiemenge wird GENAU EINMAL
umgerechnet, an der Anzeigekante).

**Ein Schalterwechsel verwirft den Achsenausschnitt.** Ganglinie und Dauerlinie tragen
an derselben Bildstelle verschiedene Stunden; ein mitgeschleppter Ausschnitt zeigte
danach etwas anderes, als der Anwender aufgezogen hat.

**Ohne Zahl eine Meldung, kein leerer Kasten.** Liefert die Hülle `null` — die Zeile ist
eben erst aufgenommen und hat noch keine Projektkopie (`IdZ` ab 100 000), oder das
Projekt führt keine Klimaregion —, meldet der Dialog `GEB_MSG_KEIN_BEDARF`, statt eine
leere Überlagerung aufzumachen.

### iOS: nichts zu tun — noch

`GebaeudeDialog` ist auf iOS **nicht erreichbar**: `AppWurzel` kennt ihn nur als
Assistentenseite 2 (`AssistentSeite.Seitentyp(2)`), und der Assistent geht dort über
`IProjektQuelle.AssistentGaben` — die Standardumsetzung liefert `null`, und
`EPOS.iOS/Dienste/IosProjektQuelle` überschreibt sie nicht („Solange die iOS-Hülle den
Assistenten nicht bedient (iU11)…", `AppWurzel` :368). Solange das so ist, gibt es dort
keinen Knopf und also auch nichts durchzureichen. **Wer den Assistenten auf iOS
verdrahtet, denkt an `BedarfGaben`** — die Gabe ist dieselbe wie unter Windows und liegt
vollständig im Kern.

Die zwei Parametersatz-Wachen brauchen keinen Eintrag: `Parametersatzwache` (am Gerät)
und `EPOS.UI.Tests/ParametersatzTests` (im Gate) führen keine Schlüsselliste, sondern
halten die Gaben je Hülle per Reflexion gegen die `[Parameter]` ihrer Komponente. Die
vier neuen Schlüssel (`BedarfGaben`, `BtnSimulationText`, `BtnSimulationHinweis`,
`MeldungKeinBedarf`) laufen damit von selbst mit.

### Texte

Neu in beiden Sprachkatalogen **und** in `Resource.Designer.cs`: `GEB_BTN_SIMULATION`,
`GEB_BTN_SIMULATION_HINWEIS`, `GEB_MSG_KEIN_BEDARF`, `GEBB_TITEL`,
`GEBB_GRP_KENNZAHLEN`, `GEBB_LBL_HEIZWAERME`, `GEBB_LBL_VOLLBENUTZUNG`,
`GEBB_EINHEIT_STUNDEN`. Wiederverwendet sind `SIMERG_LBL_MAX_WAERMELAST`,
`SIM_CHK_SORTIERT`, `ALLG_LBL_EINHEIT`, `BERG_GRP_MONAT`,
`CHART_TITEL_WAERMELAST_JAHRESGANGLINIE`, `CHART_ACHSE_WAERMELAST`, `ALLG_BTN_OK` und
die zwölf `ALLG_MONAT_*`.

### Nachweise

**Kern** — `EPOS.Kern.Tests/GebaeudeBedarfCtrlTests` (13 Fälle): Reihe über null und
keine Stunde über der Höchstlast; Monatssumme = Jahressumme; Vollbenutzungsstunden im
Jahr; der Name kommt aus der Projektkopie; vier Leerfälle (kein Projekt, keine
Klimaregion, keine Zuordnung, unbekannte Zuordnung) liefern kein Ergebnis und keine
Ausnahme; zwei Zuordnungen desselben Gebäudes liefern verschiedene Zahlen.

**Die Abnahmeprobe** ist der Vergleich mit dem LAUF, nicht mit einer eingefrorenen Zahl:

| Projekt | Gebäude | Probe |
|---|---|---|
| 1007, 1017 | je **eines** | `HeizwaermeMwh` ist **bitgleich** zu `Waermebedarf_Gebaeude_Gesamt` des Laufs |
| 1008 (zwei), 1039 (drei) | mehrere | die **Summe** der Einzelrechnungen ist dieselbe Zahl (relative Toleranz 1e‑5 — der Lauf summiert alle Gebäude als `float` und teilt danach) |

**Oberfläche** — 20 bunit-Fälle: sieben in `GebaeudeDialogTests` (ohne Delegat kein
Knopf; gesperrt ohne Markierung; die `IdZ` der markierten Zeile geht an den Kern; ohne
Ergebnis eine Meldung; Esc schließt den Wirt nicht; in der Verwaltung kein Knopf) und
dreizehn in `GebaeudeBedarfDialogTests` (Feldbestand, kein Brauchwasser-/Gesamt-Schalter
und genau EIN Kontrollkästchen, „—" ohne Höchstlast, keine Monatstabelle ohne
Monatswerte, Platzhalter ohne Bildauftrag, „sortiert" zeichnet neu, Datenzoom hinein und
beim Umschalten wieder heraus, Einheitenwahl mit Umrechnung und Rückmeldung, Leistung
bleibt kW, OK/Esc/Enter). Beide Klassen pinnen die Kultur auf de-DE.

**Referenzlauf byte-gleich** (1030/1007/1017, `EPOS.Referenzlauf lauf` + `vergleich`):
GESAMT PASS über 815 043 Werte, dazu `diff -r` über beide Ordner — alle **73**
Ergebnisdateien identisch; nur `protokoll.txt` trägt Uhrzeit und Zielordner.
**`SqlDialektPruefer`**: 1 200 SQL-Texte, **0 Fundstellen**.
**`ChartProben`**: 36 Bilder, 0 Verstöße.

### Abnahmepunkte A‑W9‑E‑2 (Windows)

| # | Weg | Erwartung |
|---|---|---|
| 1 | Projekt **1007** öffnen → Kachel **„Gebäude"** → ein Gebäude in „ausgewählte Gebäude im Projekt" markieren | Der Knopf **„Simulation…"** steht neben „Ändern" und ist **frei** |
| 2 | Zeile in „Gebäude in DB" markieren (die Projektmarkierung fällt weg) | „Simulation…" ist **gesperrt**, wie „Ändern" |
| 3 | „Simulation…" drücken | Überlagerung mit Gebäudenamen, drei Kennzahlen und der Jahresganglinie; **kein** Brauchwasser, **kein** „Gesamt" |
| 4 | Schalter **„sortiert"** | Das Bild wird zur **Dauerlinie**, die x-Achse trägt die Stundenmarken 2000/4000/6000/8000 |
| 5 | Im Bild ein Rechteck aufziehen → „Bereich"; danach „1:1" | Das Bild zeigt den Ausschnitt und danach wieder das ganze Jahr |
| 6 | Einheit auf **kWh** stellen | Wärmebedarf und Monatswerte × 1 000; **max. Wärmelast bleibt kW**, Vollbenutzungsstunden bleiben h/a |
| 7 | Dialog schließen, Kachel **„Simulation Konfiguration…"** → Lauf → Reiter „Wärme-/Strombedarf" → „Details…" | **„Wärmebedarf Gebäude"** trägt dieselbe Zahl wie der Dialog (Projekt 1007 hat genau ein Gebäude) |
| 8 | Katalogverwaltung („Gebäudedaten verwalten") öffnen | Es gibt **keinen** Knopf „Simulation…" |
| 9 | Im Gebäudedialog eine Zeile **neu aufnehmen** und **vor** dem Speichern „Simulation…" | Meldung „Für dieses Gebäude lässt sich kein Wärmebedarf berechnen…", **keine** leere Überlagerung |

### Hausregel

**Eine Auskunft, die eine Zahl des Laufs wiederholt, RUFT den Rechenweg des Laufs — sie
schreibt ihn nicht ab.** Wo das nicht geht, weil der Weg in einer Schleife steckt, wird
der Schleifenrumpf ausgelagert und von beiden Seiten gerufen; die Probe ist dann kein
eingefrorener Wert, sondern der **Vergleich gegen den Lauf selbst** (bei einem Gebäude
bitgleich, bei mehreren als Summe). Ein zweiter Rechenweg für dieselbe Zahl läuft
irgendwann auseinander, und dann sieht der Anwender zwei Zahlen und weiß nicht, welche
gilt.

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
| `Dialoge/Bedarf/GebaeudeKatalogDialog.razor` | 41 | 9 | nein | Klasse A, der **dickste Dialog des Hauses**. Alle sieben Gruppenköpfe beider Reiter plus der Luftwechsel; die acht handgebauten `epos-feldpaar`‑Kästen der zwei Ferienblöcke entfallen — Tag und Monat stehen als eigene Rasterkinder nebeneinander. Der Knopf zu den Brauchwasserprofilen bleibt unter dem Raster. |
| `Dialoge/Bedarf/GebaeudeWohnflaecheDialog.razor` | 9 | 2 | nein | Klasse A. Beide Blöcke; der Verbrauch ist ein kurzes Feld mit „m³/a" unmittelbar dahinter, die Herleitungszeile zum Jahresnutzungsgrad spannt über beide Spalten. |

Der schon in Aufgabe #90 umgestellte `GebaeudeDialog` (Detailblock „Verbrauch")
bleibt, wie er ist — er war die Stichprobe der Regel.

**Probe.** `Die_Bloecke_des_Gebaeudekatalogs_stehen_im_Formularraster` und
`Beide_Bloecke_stehen_im_Formularraster`.

**Eine Zeile Stilblatt kam dazu** — der Unterblock „Formularraster — Paket P3" in
`epos-ui.css`: Eine `Herleitungszeile` als Rasterkind spannt über **alle** Spalten.
Sie gehört zu dem Feld ÜBER ihr („Vorgabe 0,6", „aus dem Kesselwirkungsgrad");
als gewöhnliches Rasterkind fiele sie im zweispaltigen Raster **neben** ein fremdes
Feld und läse sich wie dessen Erläuterung. Sonst kein CSS, keine Inline‑Stile.

---

## Windows-Abnahme 05.09.2026 — Wärmebedarf Extern nach dem Stromganglinien-Muster (W9‑E‑3)

**Der Wortlaut des Anwenders** (Bildschirmfoto „Wärmebedarf Extern"): „Gestalte den Dialog
bei Wärmebedarf → Daten importieren analog zum Import des Strombedarf → Messdaten
importieren (mit grafischer Darstellung etc. wie kürzlich vorgenommen)." Gemeint ist
`StromganglinieDialog` nach den Anwenderwünschen **W12‑E‑1** (vier Knöpfe, Formathinweis)
und **W12‑E‑2** (Grafik) desselben Abnahmetags.

### Was der Dialog vorher hatte — und was nicht

Die Feldkarte von `Form_Waermebedarf` (Stand vor W9.4, 828 × 443, 11 Kartenzeilen) führt
zwei Listen, „◀"/„▶", das zur Laufzeit gebaute Kanalfeld, „Einlesen/Bearbeiten..",
„DB Ganglinie löschen", „OK" und „Abbrechen" — **keinen Import, kein Speichern unter, keine
Grafik**. Der Port hat also nichts vergessen: Der Wunsch ist eine ERWEITERUNG. Zwei ihrer
drei Teile gab es im Bestand eine Maske weiter:

| Weg | Stand vorher |
|---|---|
| **„Einlesen/Bearbeiten.."** | Öffnet seit W13.2 `WaermebedarfAdminDialog` als Überlagerung (bis Welle 13 ein Sprung über die `Sprungbruecke` in `Form_AdminWaermeeinlesen`). Dort und **nur** dort lag der Dateiimport — der Weg dahin war zweistufig und unbeschriftet |
| **Der Import selbst** | `WaermebedarfAdminHuelle.Einlesen`: `GanglinienTextDatei.Lies(pfad, mitKopfzeile: false)` + Dublettenprüfung + `WaermebedarfStammCtrl.ImportGanglinie(name, List<string>)`. Eine **zweite, viel engere Kette** neben der AP5‑Kette des Stroms: eine Textzeile je Wert, Dezimaltrenner **Punkt**, keine Kopfzeile, kein Trennzeichen, keine Einheitenwahl (kW/kWh), kein Prüfprotokoll, kein Excel — und **nur 8 760 Werte**, obwohl `SimulationWaermebedarf` seit V0‑5 auch 35 040 liest |
| **„Speichern unter"** | gab es im ganzen Wärmezweig **nicht** |
| **Die Grafik** | gab es **nirgends**; als Kurve erschien der Wärmebedarf nur im Bedarfsreiter der Ergebnisseite (`BildBedarfWaerme`) und seit W9.8 im `GebaeudeBedarfDialog` |

### Die Umsetzung

**1. EIN Import statt zwei — die Ausprägung als DATEN.** `GanglinienImportAblauf` bekommt
mit **`GanglinienZiel`** eine Ausprägung nach dem Muster von `KatalogImportProfil` (W13) und
`KatalogBrowserProfil` (W14a): vier Werte — Ablageordner, Katalogschlüssel und die zwei
Schreibwege `Anlegen`/`Ersetzen`. `MitAblage` nimmt sie als ersten Parameter; die alte
Signatur bleibt als Strom‑Fassade stehen, damit kein Aufrufer bricht. **Der Ablauf kennt
seither weder Tabelle noch Controller.** Der Wärmebedarf hängt sich mit
`GanglinienZiel.Waermebedarf` ein — Ordner `<BenutzerLokal>\Waermebedarf` (derselbe Pfad wie
vorher), Katalogschlüssel `WAERMEBEDARF` (leeres `ImportSpalten`‑Array = Namensprüfung ohne
Inhaltsvergleich, W13.0g) und die zwei neuen Schreibwege im Stammcontroller.

**Das Zeitinterval fällt dabei weg**, und das ist keine Lücke: `Tab_Waermebedarf_STAMM`
führt die Spalte nicht, und `SimulationWaermebedarf` leitet das Raster seit V0‑5 aus der
WERTZAHL ab (8 760 oder 35 040). Die Kette liefert beide Raster, und beide laufen im
Bestand — **der Wärmeimport kann seither Viertelstundenwerte**, was er vorher nicht konnte.

**2. Beide Wärmemasken fahren dieselbe Kette.** Der Baustein `GanglinienImportLauf` (die drei
Überlagerungen Optionen/Protokoll/Konflikte) zieht von `Dialoge/Strom` nach `Bausteine/` und
wird jetzt von **vier** Masken eingehängt: `StromganglinieAdminDialog`, `StromganglinieDialog`,
`WaermebedarfAdminDialog` und `WaermebedarfExternDialog`. `WaermebedarfAdminDialog` verliert
dafür seine eigene Konfliktüberlagerung samt `TaskCompletionSource` und sein `IProgress` (die
Kette meldet keinen Anteil — der Balken läuft unbestimmt, ehrlicher als eine erfundene
Prozentzahl); `WaermebedarfImportRueckrufe` und `WaermebedarfImportErgebnis` sind ersatzlos
entfallen.

> **Ein stiller Fallstrick beim Verschieben eines Bausteins.** `GanglinienImportLauf`
> zeichnet `<GanglinieImportOptionenDialog>` und `<GanglinieProtokollDialog>`; die stehen in
> `EPOS.UI.Dialoge.Strom` und waren im alten Ordner ohne `@using` sichtbar. Nach dem Umzug
> nach `Bausteine/` hält der Razor-Übersetzer eine unbekannte Marke für ein **HTML-Element**
> und zeichnet sie still als leeres Tag — kein Fehler, keine Warnung, nur zwei
> Überlagerungen, die leer bleiben. Aufgefallen ist es an zwei bestehenden
> `StromganglinieAdminDialogTests`, die in ihren `WaitForAssertion` liefen. Das `@using`
> steht jetzt mit dieser Begründung im Kopf des Bausteins.

**3. Die Knopfleiste unter der Katalogliste** trägt vier statt zwei Knöpfe, in dieser
Reihenfolge: **„CSV-Datei importieren…" · „Speichern unter…" · „DB Ganglinie löschen" ·
„Einlesen/Bearbeiten..".** Sie ist die `epos-leiste`; **kein Delegat, kein Knopf**.
Der Import holt den Pfad über den erwarteten `DateiWaehlen`-Delegaten und **`await`et ihn**
(W13‑B‑1) und ruft dann `Starten(pfad, GanglinienRaster.Unbekannt)` — die Maske gibt keine
Rastervorgabe, die Kette erkennt es selbst und der Optionendialog lässt es übersteuern.

**Löschen prüft ZWEI Sperren** und beide MELDEN ihren Grund: die Projektzuordnung
(`WaermebedarfStammCtrl.HatProjektzuordnung`, seit W9.0d im Kern) und das
Auslieferungskennzeichen `ReadOnly`, dessen Grund zusätzlich als `title` am Knopf hängt
(Staffelung W16b‑E‑6). **Das zweite ist neu**: Dieser Dialog holte bis hierher nur eine
NAMENSLISTE, kannte `ReadOnly` also gar nicht — ein Auslieferungssatz führte kommentarlos
durch die Rückfrage und scheiterte erst im Controller mit einer `MessageBox`. Er zieht seinen
Katalog jetzt aus **derselben Quelle wie die Verwaltung** (`WaermebedarfAdminHuelle.KatalogLesen`),
statt die Schleife ein zweites Mal zu schreiben — derselbe Weg, den W12‑E‑1 beim Strom
genommen hat.

**Speichern unter** ist die Kopie unter neuem Namen. Der `NamensDialog` schlägt
„&lt;Name&gt; - Kopie" vor und prüft die Dublette **VOR** dem Einfügen gegen den geladenen
Katalog; im Kern legt **`WaermebedarfStammCtrl.KopiereStamm`** Kopf und Werte in **einer**
Transaktion an, in Stamm-Reihenfolge (`ORDER BY ID`), immer mit `ReadOnly = false` — eine
Kopie ist Anwenderbestand, auch die eines Auslieferungssatzes. Auch dort steht die
Dublettenprüfung vor dem `INSERT` (`Exists`), damit kein SQLite-UNIQUE-Fehler den Anwender
erreicht. Der Schreibsatz ist mit `ImportGanglinie` **geteilt** (neues privates
`EinfuegenStamm`).

**4. Der Formathinweis** steht einzeilig unter der Leiste (`WBX_HINWEIS_FORMAT_KURZ`), der
volle Wortlaut hängt am `InfoKnopf` (`WBX_HINWEIS_FORMAT`, neue Zeile
`Form_Waermebedarf.btn_Help_Import` in `help_mapping.txt`). Er nennt genau das, was die Kette
wirklich auswertet — Dateiarten, 8 760 bzw. 35 040 Werte in Zeitfolge ab dem 1. Januar, die
vier zugelassenen Feldtrennzeichen und den einspaltigen Fall, die erkannte Kopfzeile, Komma
**oder** Punkt als Dezimaltrennzeichen ohne Tausendertrennung, kW oder kWh je Intervall, die
zulässige aber nicht nötige Zeitstempelspalte und den Bezeichner = Dateiname ohne Erweiterung.
Denselben Hinweis samt Infoknopf bekommt die Verwaltung; ihr `WBAD_LBL_STUNDENWERTE` lautete
bis hierher „Stundenwerte über 1 Jahr als Textdatei (Dezimaltrennzeichen '.')" — für die alte
Kette richtig (Befund W13‑B56), für die neue zu eng.

**5. Die Grafik** steht — sobald LINKS ODER RECHTS eine Zeile markiert ist — über die volle
Breite unter den beiden Spalten, vor der Fußleiste: Kennzahlen, Schalter „sortiert",
Einheitenwahl und das Bild **B1** (`ChartRenderer.GanglinieNormiert`, Titel
`CHART_TITEL_WAERMELAST_JAHRESGANGLINIE`, Achse `CHART_ACHSE_WAERMELAST`, Farbe Rot) im
Baustein `Diagramm` mit Bild- und Datenzoom. **Es ist dasselbe Bild** wie im Bedarfsreiter der
Ergebnisseite und im `GebaeudeBedarfDialog` (W9.8), nur mit EINER Reihe — **kein neues
Renderer-Bild**, `ChartProben` bleibt bei 36 + 4.

**Die Zahlen kommen aus dem Kern, und es gibt sie nur EINMAL.**
`StromganglinieAuswertungCtrl` ist zu **`GanglinienAuswertungCtrl`** verallgemeinert; die drei
Tabellennamen und der Weg Bezeichner → Kopf-Id stehen als **`GanglinienQuelle`** (`Strom`,
`Waermebedarf`) daneben. Gelesen wird dieselbe Wertspalte wie im Lauf
(`… WHERE ID_Ganglinie = ? ORDER BY ID`) und als `float`; 35 040 Viertelstundenwerte gehen
durch **dieselbe** Methode, die der Lauf benutzt
(`SimulationControl.Viertelstunden_zu_Stundenwerte_Mittelwert`). Daraus fallen Jahresarbeit
[MWh], Spitze [kW] und Vollbenutzungsstunden [h/a].

**Eine Falle steckt in der Projektzeile**, und sie ist beim Strom nicht vorhanden:
`Z_ProjektWaermebedarf.ID_Ganglinie` zeigt auf die PROJEKTKOPIE (`Tab_Waermebedarf.ID`) —
eine im Dialog eben erst aufgenommene Zeile trägt dagegen die **STAMM**-Id
(`WaermebedarfExternHuelle.Aufnehmen`), und ihre Kopie entsteht erst beim Speichern
(`WizardCtrl.Add_WaermebedarfExtern` → `ApplyGanglinieToProjekt`). Als Kopie-Id gelesen,
zeigte die Grafik die Werte einer **fremden** Ganglinie. Der Dialog gibt deshalb nur für eine
GESPEICHERTE Zuordnung (`IdZ > 0`) die Id weiter und sonst `0` — dann fällt der Kern über den
Bezeichner auf den Katalogsatz zurück, aus dem die Kopie entstehen wird. Eigener Testfall.

**6. Der Kanal** bleibt, was er war (Steuerwert je Zuordnung, Vorbelegung Heizung, gesperrt
ohne Markierung) und steht jetzt im Baustein **`Formularraster`** (Hausregel iU8‑E‑2) —
Beschriftung neben dem Feld, einspaltig, in der LINKEN Spalte: Er gehört zur Zuordnung, nicht
zum Katalog (Regel des Bausteins `Zweispaltenauswahl`).

**Kein Inline-CSS, keine neue Regel.** Der Dialog benutzt `.epos-formathinweis` und
`.epos-ganglinie-grafik` aus W12‑E‑2 unverändert; `epos-ui.css` ist **nicht angefasst**, der
Formularraster-Block bleibt der letzte im Blatt (Wache `FormularrasterTests`).

### Geänderte und neue Dateien

| Datei | Was |
|---|---|
| `EPOS.Kern/Controller/GanglinienAuswertungCtrl.cs` | **neu** — der verallgemeinerte Leseweg samt `GanglinienAuswertung` und `GanglinienQuelle` |
| `EPOS.Kern/Controller/StromganglinieAuswertungCtrl.cs` | **gelöscht** — vollständig in den obigen aufgegangen |
| `EPOS.Kern/Allgemein/Import/GanglinienImportAblauf.cs` | `GanglinienZiel` (Strom/Wärmebedarf), `MitAblage(ziel, …)`, `AblageOrdner(ziel)` |
| `EPOS.Kern/Controller/WaermebedarfStammCtrl.cs` | `Exists`, `KopiereStamm`, `ImportGanglinie(IList<double>)`, `ErsetzeGanglinie`, privates `EinfuegenStamm` |
| `EPOS.UI/Bausteine/GanglinienGrafik.razor` | **verschoben** aus `Dialoge/Strom`, Namensraum `EPOS.UI.Bausteine`, ohne Fachbezug |
| `EPOS.UI/Bausteine/GanglinienGrafikDaten.cs` | **neu** — `GanglinienWahl` und `GanglinienKennzahlen`, aus `StromganglinieDaten.cs` herausgezogen |
| `EPOS.UI/Bausteine/GanglinienImportLauf.razor` | **verschoben**, dazu das `@using` auf die zwei Zwischendialoge |
| `EPOS.UI/Dialoge/Bedarf/WaermebedarfExternDialog.razor` | vier Knöpfe, Formathinweis, Grafik, Kanal im Formularraster, Katalog mit `ReadOnly` |
| `EPOS.UI/Dialoge/Bedarf/WaermebedarfAdminDialog.razor` | `GanglinienImportLauf` statt eigener Konfliktkette; Infoknopf am Formathinweis; ReadOnly-Text berichtigt |
| `EPOS.UI/Dialoge/Bedarf/WaermebedarfAdminDaten.cs` | die zwei Importtypen entfallen; `AblageErgebnis` bleibt |
| `EPOS.UI/Dialoge/Strom/StromganglinieDaten.cs` | zwei Records abgegeben |
| `EPOS.UI/CLAUDE.md` | zwei Bausteinzeilen |
| `WindowsFormsApplication1/Views/Wärmebedarf/WaermebedarfExternHuelle.cs` | sieben neue Delegaten, `Grafikvorrat` (eine gelesene Reihe je Dialog) |
| `WindowsFormsApplication1/Views/Wärmebedarf/WaermebedarfAdminHuelle.cs` | `Einlesen`/`Vorschau` über die gemeinsame Kette; `KatalogLesen`/`DateiWaehlen` `internal` |
| `WindowsFormsApplication1/Views/Stromverbraucher/StromganglinieHuelle.cs` | auf `GanglinienAuswertungCtrl` + `GanglinienQuelle.Strom` gezogen |
| `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt` | `Form_Waermebedarf.btn_Help_Import` |
| `EPOS.Kern/MyResource/Resource*.resx` + `Resource.Designer.cs` | **7 neue** Schlüssel (de/en), **2 geänderte** |
| `EPOS.Kern.Tests/WaermebedarfKatalogTests.cs` | **neu** — 17 Fälle |
| `EPOS.Kern.Tests/StromganglinieAuswertungTests.cs` | auf die neuen Namen gezogen (Zahlen unverändert) |
| `EPOS.UI.Tests/Dialoge/WaermebedarfExternDialogTests.cs` | 16 → 36 Fälle |
| `EPOS.UI.Tests/Dialoge/WaermebedarfAdminDialogTests.cs` | vier Fälle auf die gemeinsame Kette gezogen |

**Die neuen Textschlüssel** (de/en): `WBX_HINWEIS_FORMAT`, `WBX_HINWEIS_FORMAT_KURZ`,
`WBX_TITEL_KOPIE`, `WBX_MSG_NAME_VERGEBEN`, `WBX_MSG_KOPIERT`, `WBX_MSG_GELOESCHT`,
`WBAD_MSG_SCHREIBGESCHUETZT`. **Geändert**: `WBAD_LBL_STUNDENWERTE` (der Hinweis nennt jetzt
beide Raster) und `WBAD_DATEIFILTER` (`(*.txt)` → derselbe Filter wie beim Lastgang).
Wo der Text ohnehin fachneutral ist, wird der vorhandene Schlüssel WEITERBENUTZT statt
verdoppelt: `STROMGL_BTN_IMPORTIEREN`, `…_SPEICHERN_UNTER`, `…_LBL_JAHRESARBEIT`,
`…_LBL_SPITZE`, `…_FRAGE_KOPIE`, `…_KOPIE_ZUSATZ`, `…_MSG_NAME_LEER`, `…_MSG_KOPIE_FEHLER`.

### Wachen

**Kern** — `EPOS.Kern.Tests/WaermebedarfKatalogTests` (17 Fälle, neu): die drei Kennzahlen
zweier Katalogsätze **eingefroren**; die Projektkopie trägt dieselben Zahlen wie ihr
Katalogsatz; ohne Projektkopie greift der Rückfall über den Bezeichner; **die Ausprägung
entscheidet über die Tabelle** (der Name `test` steht in BEIDEN Katalogen mit
verschiedenen Reihen — wäre `GanglinienQuelle` wirkungslos, käme zweimal dieselbe Zahl);
ein unbekannter Name ergibt keine Auswertung; die zwei Löschsperren; `Exists` prüft den
ganzen Namen und nicht seinen Anfang (der Fehler, der beim Solarkatalog Befund W14‑B70 war);
die Kopie trägt dieselben Werte unter neuem Namen und ist frei, auch die eines
Auslieferungssatzes; ein vergebener Name wird abgewiesen statt zu werfen (auch getrimmt);
`ImportGanglinie` legt Kopf und Werte an, eine leere Reihe wird nicht geschrieben, und
`ErsetzeGanglinie` **behält die Kopf-Id** (der alte Wärmeweg löschte den Satz und legte ihn
neu an — die Id wechselte dabei). Der IMPORTweg selbst braucht keine neue Wache: Es ist
dieselbe Kette, und die steht seit W12 in `GanglinienImportAblaufTests` und
`GanglinienProbenTests`.

**Oberfläche** — `EPOS.UI.Tests/Dialoge/WaermebedarfExternDialogTests` (16 → 36 Fälle): die
vier Knöpfe in ihrer Reihenfolge; „kein Delegat, kein Knopf" samt dem Halbfall Dateiwähler
ohne Kette; der Formathinweis nennt seine Angaben und der Infoknopf trägt den vollen
Wortlaut; der Dateiwähler DARF warten und die Kette läuft danach mit `Raster.Unbekannt`; ein
abgebrochener Wähler liest nichts; der Kanal steht im `Formularraster` der linken Spalte;
Löschen ohne Auswahl gesperrt, mit Zuordnung und mit `ReadOnly` je mit Grund und **ohne**
Rückfrage, frei mit Rückfrage „Ja"/„Nein"; Speichern unter mit Vorschlag „ - Kopie",
vergebenem Namen (Dialog bleibt offen), freiem Namen und gescheiterter Kopie; die Grafik
erscheint bei Markierung links wie rechts, bleibt ohne Markierung und ohne brauchbare Reihe
weg, „sortiert" zeichnet neu, **und eine neu aufgenommene Zeile wird über den KATALOG
gelesen** (die Fallgrube oben); dazu ein Fall in **englischer** Oberfläche.

### Nachweise

| Prüfung | Ergebnis |
|---|---|
| `dotnet test EPOS.Kern.Tests -c Release` | **1 207 grün** (1 190 + 17 neue), auch unter `LANG=en_US.UTF-8` |
| `dotnet test EPOS.UI.Tests -c Release` | **2 668 grün** (2 648 + 20 neue), auch unter `LANG=en_US.UTF-8` |
| `dotnet build WindowsFormsApplication1 -p:EnableWindowsTargeting=true` | 0 Fehler, 6 Warnungen (unverändert) |
| `dotnet run --project Proben/ChartProben -c Release` | 40 Bilder (36 + 4 Gegenproben), 0 Verstöße |
| `SqlDialektPruefer` | 1 212 SQL-Texte, **0 Fundstellen** |
| Kern-Wächter (`Program.*`, Plattform) | beide leer |
| Referenzlauf | nicht nötig — der Rechenweg ist unberührt; gelesen wird nur, und die Verdichtung ist DIESELBE Methode wie im Lauf |

**Eingefrorene Kennzahlen** (`Referenzlaeufe/Kenndaten_Test.sqlite`):

| Katalogsatz | Raster | Jahresarbeit | Spitze | Vollbenutzung |
|---|---|---|---|---|
| `Wärmebedarf_Laurentiuskirche` | 8 760 Stundenwerte | 65,430 MWh | 47,649 kW | 1 373,16 h/a |
| `Nestle_Sprühturm-Wärmebedarf-1098kW-4300h-4724MWh.txt` | 8 760 Stundenwerte | 4 724,694 MWh | 1 098,00 kW | 4 303,00 h/a |

### Abnahmepunkte A‑W9‑E‑3

1. **Die vier Knöpfe.** Startseite → Kachel „Wärmebedarf" → „Daten importieren" (oder
   Assistentenseite 3): Unter „Wärmebedarf aus DB" stehen „CSV-Datei importieren…",
   „Speichern unter…", „DB Ganglinie löschen" und „Einlesen/Bearbeiten..", jeder so breit
   wie sein Text. Darunter der Formathinweis mit dem Fragezeichenknopf rechts daneben; der
   Knopf öffnet die Wikiseite „Wärmebedarf", sein Tooltip zeigt den vollen Formattext.
2. **Import mit einer sauberen Datei.** Eine CSV mit 8 760 Zeilen, ein Wert je Zeile, Komma
   als Dezimaltrennzeichen, ohne Kopfzeile — z. B. `123,4` / `118,9` / … Der Dateiwähler
   geht auf (kein Absturz, W13‑B‑1), der Optionendialog zeigt „kein Trennzeichen /
   Dezimaltrenner Komma / keine Kopfzeile / Spalte 1", das Protokoll bleibt weg, und die
   neue Ganglinie steht unter dem Dateinamen ohne Erweiterung in der rechten Liste. Grünes
   Banner mit Name und Wertezahl.
3. **Import mit Semikolon, Kopfzeile und Viertelstundenwerten.** `Zeit;Leistung` mit
   Kopfzeile und 35 040 Zeilen (`01.01.2024 00:00;123.4`): Der Optionendialog erkennt
   Semikolon, Kopfzeile „ja", Zeitspalte 1, Wertspalte 2 und Punkt als Dezimaltrennzeichen.
   **Das konnte der Wärmeimport vorher nicht** — er hätte die Datei abgelehnt.
4. **Import in kWh je Intervall.** Im Optionendialog die Einheit auf „kWh je Intervall"
   stellen: Die Kette rechnet in kW um; die Grafik zeigt danach dieselbe Spitze wie eine
   inhaltsgleiche kW-Datei.
5. **Import mit einer Datei, die es schon gibt.** Dieselbe Datei ein zweites Mal wählen: Der
   Konfliktdialog kommt (Auslassen / Überschreiben / Umbenennen) — genau derselbe wie hinter
   „Einlesen/Bearbeiten..". „Überschreiben" tauscht die Werte und **behält die Kopf-Id**:
   Ein Projekt, das die Ganglinie schon führt, verliert seinen Bezug nicht.
6. **Löschen — zugeordnet.** Eine Ganglinie wählen, die im Projekt steht: „DB Ganglinie
   löschen" meldet „Es existiert eine Projektzuordnung, Löschen nicht möglich!" — **keine**
   Rückfrage, nichts gelöscht.
7. **Löschen — Auslieferung.** Ein Katalogeintrag mit `ReadOnly`: Der Knopf zeigt den Grund
   schon als Tooltip; der Klick meldet „Diese Wärmebedarfsganglinie ist schreibgeschützt
   (ReadOnly)…", ohne Rückfrage. **Bis hierher kam an dieser Stelle eine `MessageBox` mit dem
   Wort „Stromganglinie".**
8. **Löschen — frei.** Eine freie Ganglinie: Rückfrage „Soll … wirklich gelöscht werden ?" →
   „Ja"; die Zeile verschwindet, grünes Banner. „Nein" lässt alles stehen.
9. **Speichern unter.** Eine Ganglinie wählen → „Speichern unter…": Der Name steht mit
   „ - Kopie" vorbelegt. Erst einen vergebenen Namen tippen: Der Dialog bleibt offen und
   sagt, warum. Dann den Vorschlag nehmen: Die Kopie erscheint in der Liste, grünes Banner
   mit beiden Namen. Die Kopie ist frei, auch wenn die Quelle Auslieferung war.
10. **Die Grafik.** Beim Öffnen steht sie nur, wenn links eine Zuordnung markiert ist.
    Rechts eine Ganglinie anklicken: Darunter erscheinen Name, Jahresarbeit, Spitzenlast,
    Vollbenutzungsstunden und die Jahresganglinie. Schalter **„sortiert"** macht daraus die
    Dauerlinie (x-Achse 2000/4000/6000/8000). Im Bild ein Rechteck aufziehen → „Bereich",
    danach „1:1". Einheit auf **kWh** stellen: Jahresarbeit × 1 000, **Spitze bleibt kW**,
    Vollbenutzungsstunden bleiben h/a.
11. **Die Zahlen stimmen mit dem Lauf überein.** `Wärmebedarf_Laurentiuskirche` markieren
    (Projekt 1041): 65,43 MWh / 47,65 kW / 1 373 h/a. Danach Simulation rechnen → Reiter
    „Wärmebedarf": Der externe Anteil trägt dieselbe Jahresarbeit.
12. **Eine eben übernommene Zeile zeigt schon ihre Grafik.** Rechts eine Ganglinie wählen,
    „In das Projekt übernehmen": Die neue Zeile links ist markiert, und darunter steht die
    Grafik mit denselben Zahlen wie eben rechts — **noch bevor** gespeichert wurde.
13. **Der Kanal.** Bei markierter Projektzeile steht „Kanal:" **neben** der Klappliste (nicht
    darüber); ohne Markierung ist sie gesperrt. Umschalten auf „Brauchwasser" wirkt auf
    genau diese Zeile, OK speichert, Wiederöffnen zeigt sie unverändert.
14. **Nichts sonst hat sich bewegt.** „Einlesen/Bearbeiten.." zeigt unverändert die
    Verwaltung als Überlagerung — dort ist der Fortschrittsbalken jetzt unbestimmt und der
    Konflikt-, Optionen- und Protokolldialog derselbe wie im Hauptdialog. „In das Projekt
    übernehmen"/„Aus dem Projekt entfernen", OK und Abbrechen verhalten sich wie bisher.
    Esc schließt immer nur die oberste Ebene.

### Hausregel

**Wer einen Baustein in einen anderen Namensraum verschiebt, prüft seine KIND-Marken.**
Ein unbekanntes Element ist in Razor kein Fehler, sondern HTML — die Komponente
verschwindet still, und das Markup sieht bis auf ein leeres Tag richtig aus. Eine
bunit-Probe, die nur auf Text prüft, sieht es nicht; gesehen haben es hier zwei Proben,
die auf die KLASSE des Unterdialogs warteten (`.epos-importoptionen`). Dieselbe Lehre wie
W6‑B‑1 beim Stilblatt, nur eine Ebene höher.
