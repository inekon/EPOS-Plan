# Projektdialoge vereinheitlichen — Umsetzungsprotokoll Schnitt 1 (P1 + P2 + P3)

Stand: 29.08.2026. Grundlage: [`Konzept_Projektdialoge_Vereinheitlichung.md`](../../../Konzept_Projektdialoge_Vereinheitlichung.md)
(HEAD 296dd15), Abschnitte 4 (Entscheidungen E1–E6), 5 (Pakete P1–P3) und 8 (Fallstricke).

**Kern-Leitplanke eingehalten:** Alles Neue und Umgebaute ist ein im Visual-Studio-Designer
bearbeitbares `Form`/`UserControl` mit `.Designer.cs` und Satelliten-`.resx`. Kein
Laufzeit-Aufbau von Layout, keine Fabriken. Die Musterkonformität jeder geänderten
`.Designer.cs` ist maschinell geprüft (Abschnitt „Designer-Lint").

---

## 1. Was neu ist, was sich geändert hat

### Neue Dateien

| Datei | Rolle |
|---|---|
| `Allgemein/GrafikTools/KartenStil.cs` | zentrale Token-Sammlung (Farben, `RAND`, `ECKE`, **eine** `Rundeck`-Bogensemantik) |
| `Views/GemeinsameBausteine/AktionsKarte.cs` / `.Designer.cs` / `.resx` | Karten-`UserControl` (Bild, Titel, Beschreibung, Statuspunkt, Hover, `Geklickt`) |
| `Views/Projekt/ProjektAuswahl.cs` / `.Designer.cs` / `.resx` / `.de-DE.resx` / `.en-US.resx` | Projektliste mit Suche, Sortierung, Doppelklick-Auswahl |
| `Views/Projekt/Form_ProjektAuswahl.cs` / `.Designer.cs` / `.resx` / `.de-DE.resx` / `.en-US.resx` | schlanke Hüllform „Projekt öffnen" (OK/Abbrechen) |

### Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Views/Simulation/ErzeugerKarte.cs` | `KartenStil` **umgezogen** (7 Zeilen Verweiskommentar statt 85 Zeilen Klasse); `ErzeugerKarte`/`KartenChip` selbst unangetastet |
| `Views/Hauptformular/Form_Start.Designer.cs` | 6 `PictureBox` + 12 `Label` → 6 `AktionsKarte`; `BeginInit`/`EndInit` und Felddeklarationen nachgezogen |
| `Views/Hauptformular/Form_Start.cs` | Verteilereinträge der 6 Kacheln entfernt, `karte_*` statt `pBox_*`, „Zuletzt geöffnet" auf `Form_ProjektAuswahl`, „Speichern unter" über `MenueCtrl`, toter `FensterEinpassung`-Aufruf entfernt |
| `Views/Hauptformular/Form_Start.resx` / `.de-DE.resx` / `.en-US.resx` | Alt-Kachelschlüssel raus, `karte_*`-Schlüssel rein, Doppel-Leerzeichen behoben |
| `Properties/Resources.resx` + `Resources.Designer.cs` | die **sechs** Projektkachel-JPG entfernt (E6: übrige Reiter behalten ihre) |
| `Controller/MenueCtrl.cs` | „Öffnen…" öffnet wirklich; **ein** Ladeweg statt zwei; `:158`-Bug entfällt; `ProjektSpeichernUnter()` als ehrlicher Duplizier-Einstieg |
| `Views/Projekt/Form_ProjektSpeichernUnter.cs` / `.Designer.cs` / `.en-US.resx` | Relikt-Handlername umbenannt, en-US-Titel richtiggestellt |
| `Views/Wizard/Wizard_Komponenten.resx` / `.de-DE.resx` | Tippfehler und Doppel-Leerzeichen behoben |

**Nicht angefasst** (Vorgabe): `help_mapping.txt`, `DbWerte.cs`, `SchemaMigration.cs`,
`SchemaKatalog.cs`, `KiKern`, `CLAUDE.md`. Die Verwendungen von `KartenStil` in
`ErzeugerKarte`/`SpeicherKarte` sind zeilengleich geblieben.

---

## 2. P1 — Fundament

### 2.1 `KartenStil` zentral (Entscheidung mit Begründung)

Das Konzept verlangt eine zentrale Token-Klasse, **ohne** die Simulationskarten
anzufassen. Eine zweite Klasse gleichen Namens im selben Namensraum ist in C# nicht
möglich, und eine gleichnamige Klasse in einem Unternamensraum hätte genau das erzeugt,
was vermieden werden soll: **zwei auseinanderlaufende Farbtabellen**.

**Umgesetzt:** Die Klasse ist als Ganzes und **wertgleich** von
`Views/Simulation/ErzeugerKarte.cs` nach `Allgemein/GrafikTools/KartenStil.cs`
umgezogen. Kein einziger Verwendungsort wurde geändert (gleicher Name, gleicher
Namensraum, gleiche Werte); in `ErzeugerKarte.cs` steht an der alten Stelle ein
Verweiskommentar. Der Compile-Beweis: `SpeicherKarte`, `ErzeugerKarte`, `KartenChip`
übersetzen unverändert; der Prüfstand liest `ECKE = 6` und `RAND = 10` zur Laufzeit
zurück.

Neu **hinzugekommen** sind die Einstiegskarten-Token (Werte aus `EinstiegsKarte`,
`Views/Kosten`):

| Token | Wert | Herkunft |
|---|---|---|
| `KARTE_RAHMEN` | `209,213,219` | EinstiegsKarte.RAHMEN |
| `KARTE_RAHMEN_HOVER` | `59,130,246` | EinstiegsKarte.RAHMEN_HOVER |
| `KARTE_FLAECHE` | `White` | EinstiegsKarte.FLAECHE |
| `KARTE_FLAECHE_HOVER` | `239,246,255` | EinstiegsKarte.FLAECHE_HOVER (= Hinweisfläche der Startmaske) |
| `KARTE_TITEL` | `15,31,61` | EinstiegsKarte.TITEL |
| `KARTE_TEXT` | `90,98,112` | EinstiegsKarte.INFO |
| `KARTE_STATUS` | `90,0,255,0` | Statusgrün der Startmasken-Kacheln |
| `KARTE_RAND` | 16 | EinstiegsKarte-Padding |
| `KARTE_STATUSPUNKT` | 14 | neu (Durchmesser des Statuspunkts) |

`Rundeck` ist die verbindliche Bogensemantik (`d = radius * 2`). Die abweichende
zweite Lesart (`RoundedPanel`, `ChartManager.Kacheln`) wurde bewusst **nicht**
nachgebaut — die Ablösung dieser Kopien gehört in einen späteren Schnitt.

### 2.2 `AktionsKarte`

Designer-`UserControl`, Standardgröße 404×185, `Cursor = Hand`,
`[ToolboxItem(true)]`, `[DefaultEvent("Geklickt")]`, `[DefaultProperty("Titel")]`.

| Eigenschaft | Typ | Attribute |
|---|---|---|
| `KartenBild` | `Image` | `[Category("Darstellung")] [Description] [DefaultValue(null)]` |
| `Titel` | `string` | `… [DefaultValue("")] [Localizable(true)]` |
| `Beschreibung` | `string` | `… [DefaultValue("")] [Localizable(true)]` |
| `StatusSichtbar` | `bool` | `… [DefaultValue(false)]` |
| `StatusFarbe` | `Color` | `… [DefaultValue(typeof(Color), "90, 0, 255, 0")]` |
| `Geklickt` | `EventHandler` | `[Category("Aktion")] [Description]` |

`Localizable(true)` auf `Titel`/`Beschreibung` ist der Grund, warum die sechs Kacheltexte
über `resources.ApplyResources` je Sprache aus den `Form_Start`-`.resx` kommen — genau der
Weg, den der Designer selbst nimmt.

**Zwei Festlegungen, die beim Nachbauen wichtig sind:**

1. **Farben stehen NICHT in `InitializeComponent`**, sondern im Konstruktor. Der Designer
   serialisiert Farben als `Color.FromArgb`-Literale zurück und hätte die Token damit
   sofort wieder dupliziert. Geometrie und Schrift stehen dagegen im Designer, damit die
   Karte dort so aussieht wie zur Laufzeit.
2. **Klick/Hover hängen nur an `this` plus `OnControlAdded`.** Eine zusätzliche Schleife
   über `Controls` im Konstruktor hängte alles ein **zweites** Mal ein —
   `Geklickt` feuerte pro Kachelklick doppelt (vom Prüfstand als „7 Auslösungen bei 3
   Kindern + Karte" gefunden und behoben; jetzt 4/4).

Die Anordnung (`NeuAnordnen`) setzt Bild/Titel/Beschreibung als **senkrecht mittigen
Block**; die Höhe der Beschreibung wird mit `TextRenderer.MeasureText` gemessen. Bei
404×185 ohne Bild ergibt das Titel bei y = 56 und Beschreibung bei y = 92 (Alt-Kachel:
50 bzw. 108) — dieselbe Optik, ohne Bitmap.

---

## 3. P2 — Startmaske, Reiter „Projekt"

### 3.1 Fundstellen vorher / nachher

| Kachel | vorher (`Form_Start.Designer.cs`) | nachher | Klickziel (unverändert) |
|---|---|---|---|
| Neues Projekt | `pBox_ProjektNeu` + `label_…` + `label2_…`, JPG `PProjektNeu` | `karte_ProjektNeu` (18,134 / 404×185) | `pBox_ProjektNeu_Click` → `MenueCtrl.ProjektNeu()` |
| Projekt öffnen/bearbeiten | `pBox_ProjektOeffnen` + 2 Labels, JPG `PProjektOeffnen` | `karte_ProjektOeffnen` (422,134) | `pBox_ProjektOeffnen_Click` → `MenueCtrl.ProjektBearbeiten()` (**E2**) |
| Zuletzt geöffnet | `pBox_ProjektZuletzt` + 2 Labels, JPG `PProjektZuletzt` | `karte_ProjektZuletzt` (834,134) | `pBox_ProjektZuletzt_Click` (Rumpf umgestellt, siehe 4.4) |
| Speichern unter | `pBox_SpeichernUnter` + 2 Labels, JPG `PProjektBearbeiten` | `karte_SpeichernUnter` (18,325) | `pBox_SpeichernUnter_Click` → `MenueCtrl.ProjektSpeichernUnter()` |
| Projekt löschen | `pBox_Delete` + 2 Labels, JPG `PDelete` | `karte_Delete` (423,325) | `pBox_Delete_Click` → `MenueCtrl.ProjektDelete()` |
| Projekt Details | `pBox_ProjektDetails` + 2 Labels, JPG `PProjektDetails` | `karte_ProjektDetails` (835,325) | `pBox_ProjektDetails_Click` → `MenueCtrl.ProjektOeffnen(true)` |

Alle sechs Handler heißen **unverändert** `pBox_*_Click` — sie sind im Designer an
`AktionsKarte.Geklickt` gehängt. Die 18 Einträge des `CentralControl_Click`-Dictionarys
(`Form_Start.cs`) sind entfernt; der Verteiler bleibt für die Bildkacheln der Reiter 2/3/4
bestehen. `label1`/`label3` (Reiterüberschriften) und `btn_Help_Kurzanleitung` bleiben,
ihre `ZOrder` in der `.resx` wurde auf die neue Einfügereihenfolge gezogen (9→1, 10→2).

### 3.2 resx — Schlüsselzahl-Delta

| Datei | vorher | nachher | entfernt | neu | wertgeändert |
|---|---|---|---|---|---|
| `Form_Start.resx` | 1440 | 1298 | 202 | 60 | 3 (`label1.Text`, `>>label1.ZOrder`, `>>label3.ZOrder`) |
| `Form_Start.de-DE.resx` | 2 | 14 | 0 | 12 | 0 |
| `Form_Start.en-US.resx` | 140 | 131 | 21 | 12 | 0 |
| `Properties/Resources.resx` | 66 | 60 | 6 | 0 | 0 |

Ein Mengenvergleich der Schlüssel vor/nach dem Umbau belegt: **keine einzige fremde
Entfernung** — alle 202 bzw. 21 entfernten Schlüssel gehören zu den 18 Alt-Steuerelementen,
alle 6 aus `Resources.resx` zu den Projektkachel-JPG.

Je Karte stehen in der neutralen Datei zehn Schlüssel: `Location`, `Margin`, `Size`,
`TabIndex`, `Titel`, `Beschreibung` sowie die vier `>>`-Metadaten (`Name`, `Type`,
`Parent = tabPage1`, `ZOrder`). In `de-DE`/`en-US` je Karte nur `Titel` und
`Beschreibung`. Damit sind die Kacheltexte **erstmals in allen drei Dateien vollständig**
(die `de-DE`-Datei hatte bis dahin genau zwei Einträge).

### 3.3 Bildressourcen (E6)

Entfernt aus `Properties/Resources.resx` **und** `Resources.Designer.cs`:
`PProjektNeu`, `PProjektOeffnen`, `PProjektZuletzt`, `PProjektBearbeiten`, `PDelete`,
`PProjektDetails`. Vorher per Grep über den gesamten Haupt-Checkout belegt: Diese sechs
Schlüssel waren **ausschließlich** in `Form_Start.Designer.cs` referenziert.

Die Dateien `Resources\P*.jpg` bleiben auf der Platte liegen (nicht Teil des Auftrags,
keine Buildwirkung). Verbliebene Bildverweise in `Form_Start.Designer.cs` (26 Stück,
22 verschiedene) gehören zu den übrigen Reitern und zum Kopfband — `PBHKW`,
`PDetailSim`, `PEETipp`, `PGebaeude`, `POptimierung`, `PProjektPV`,
`PProjektSolarthermie`, `PPufferSpeicher`, `PSSpeicher`, `PStdLastProfil`,
`PStromMessdaten`, `PStromProfilEigenes`, `PTitel`, `PWP`, `PZusammenfassung_pg`,
`Unbenannt2/3/4`, `globe`, `globe1`, `help_icon`, `save_icon_36513`.

### 3.4 Textfehler

| Fundstelle | vorher | nachher |
|---|---|---|
| `Form_Start.resx` `label1.Text` | „… Projekt oder␣␣öffnen …" | einfaches Leerzeichen |
| `Form_Start.resx` `label2_pBox_ProjektOeffnen.Text` | „… bearbeiten␣␣Sie …" | als `karte_ProjektOeffnen.Beschreibung` neu gesetzt, ohne Doppel-Leerzeichen |
| `Wizard_Komponenten.de-DE.resx` `label1.Text` | „Projekt-Erstellung**k**onfiguration" | „Projekt-Erstellung**sk**onfiguration" |
| `Wizard_Komponenten.resx` + `.de-DE.resx` `label3.Text` | „… Energieerzeuger␣␣Komponenten …" | einfaches Leerzeichen |

Alle `.resx`-Änderungen liefen über PowerShell/`Edit`, nie über das Bash-Werkzeug.
**Falle dabei:** Windows PowerShell 5.1 liest `.ps1`-Dateien **ohne BOM als ANSI** — der
erste Lauf schrieb „Projekt Ã¶ffnen" in die Dateien. Die `.resx` wurden aus der Sicherung
zurückgeholt, die Skripte mit UTF-8-BOM gespeichert und der Lauf wiederholt; die Umlaute
sind nachweislich korrekt (Prüfstand liest „Projekt öffnen/bearbeiten" zurück).

### 3.5 Toter `FensterEinpassung`-Aufruf

`Form_Start.cs:69` `FensterEinpassung.Einhaengen(this)` ist ersatzlos entfernt, mit
Begründungskommentar an derselben Stelle: `FensterEinpassung.Zustaendig` schließt
Formulare mit `TopLevel == false` aus, und `MDIMainForm` bettet `Form_Start` genau so ein.
Den Bildlauf, den die Einpassung sonst sichern würde, setzt `Form_Start_Load` ohnehin
selbst (`AutoScroll = true` je Reiter).

---

## 4. P3 — Menü-Ehrlichkeit und Projektauswahl

### 4.1 `ProjektAuswahl` (UserControl)

* Spalten **Projektname / Kunde / Geändert**, Datenweg ausschließlich
  `ProjektCtrl.ReadAll()` → `ProjektModel` (kein neues SQL).
* Der Bestand wird **einmal** gelesen; Suche und Sortierung arbeiten örtlich auf der
  gelesenen Liste — beim Tippen wird die Datenbank nicht angefasst.
* Suche filtert über Projektname, Kunde **und** Beschreibung (Groß-/Kleinschreibung egal).
* Sortierung per Spaltenklick: gleiche Spalte kehrt die Richtung um, neue Spalte beginnt
  aufsteigend — beim Datum absteigend („zuletzt geändert zuerst"). Gleichstand wird immer
  über den Namen aufgelöst, damit die Reihenfolge bei gleichem Datum nicht springt.
* Ereignisse `ProjektGewaehlt(int id, string name)` (eigener Delegattyp
  `ProjektGewaehltHandler`) und `Abgebrochen`, beide `[Category("Aktion")]`.
* Die Zählzeile („{0} von {1} Projekten" / „{0} of {1} projects") holt ihren **Satzbau**
  aus dem Entwurfstext von `label_Anzahl` in den drei `.resx` — kein neuer
  MyResource-Schlüssel nötig.
* **Fallstrick, der real getroffen hat:** Eine `ListView` führt ihre Auswahlsammlung erst
  mit Fensterhandle. Beim Vorauswählen im `Load` (Dialog noch nicht sichtbar) blieb
  `SelectedItems` leer und OK meldete „Bitte auswählen!" trotz sichtbarer Markierung.
  `ProjektAuswahl` merkt sich die gesetzte Zeile deshalb zusätzlich selbst
  (`MarkierungUebernehmen`).

Für die Meldung „nichts gewählt" wird der vorhandene Schlüssel
`MyResource.Resource.Text_Select` verwendet („Bitte auswählen!" / „Please select!") —
**kein neuer Schlüssel**, damit `Resource.Designer.cs` unberührt bleibt.

### 4.2 `Form_ProjektAuswahl` (Hüllform)

Titel „Projekt öffnen" / „Open project", OK + Abbrechen, `AcceptButton`/`CancelButton`
gesetzt, `ucAuswahl` vierseitig verankert. Ergebnisfelder heißen bewusst wie in
`Form_ProjektSpeichernUnter` (`m_szProjekt`, `m_ID_Projekt`), damit der Aufrufer in
`MenueCtrl` unverändert weiterarbeitet. `ZuletztGeaendertZuerst(name)` schaltet die
Vorsortierung nach „Geändert" absteigend und die Vorauswahl.

Kein `btn_Help*` auf den neuen Formularen: `help_mapping.txt` bleibt in diesem Schnitt
unverändert (Hilfe für neue Masken erst in P6).

### 4.3 `MenueCtrl` — „Öffnen…" öffnet, `:158` aufgelöst

**Vorher** (`MenueCtrl.cs:81–184`): `ProjektOeffnen(false)` zeigte
`Form_ProjektSpeichernUnter`, verlangte einen **neuen** Namen und **duplizierte**; erst
danach wurde das *Ausgangs*projekt geöffnet. Die rund 40 Zeilen Ladeweg standen
**zweimal wortgleich** (Zweig „gewählt" und Zweig „zuletzt").

**Nachher:**

* `ProjektOeffnen(false)` zeigt `Form_ProjektAuswahl` und lädt das gewählte Projekt.
* `ProjektOeffnen(true)` liest weiter `Tab_Applikation` und lädt ohne Dialog.
* Beide rufen **einen** neuen privaten Ladeweg `ProjektInFormMainLaden(name, id)` —
  Reihenfolge und Umfang der `Set*`/`Add_*`-Aufrufe sind aus den bisherigen Zweigen
  wörtlich übernommen (alle betroffenen `FormMain.Set*` sind reine Property-Setter,
  daher ordnungsunabhängig — nachgelesen in `FormMain.cs:36–70`).
* `ProjektSpeichernUnter()` ist der ehrliche, einzige Einstieg in die Duplizierung;
  die Kachel „Speichern unter" ruft ihn.

**Befund `MenueCtrl.cs:158` — behoben, nicht entfernt.** Der Zweig ist lebendig (Menü
„zuletzt geöffnet" und Kachel „Projekt Details" laufen darüber). Er übergab
`frmmain.SetWaermebedarfExternControl(frm.m_szProjekt)`, wobei `frm` das **nie
angezeigte** `Form_ProjektSpeichernUnter` war — der Name war garantiert leer, die Liste
„Wärmebedarf einlesen" im Detailformular blieb dadurch leer. Mit dem gemeinsamen Ladeweg
gibt es die Fehlstelle nicht mehr; der Prüfstand belegt, dass
`SetWaermebedarfExternControl(frm.m_szProjekt)` im Quelltext nicht mehr vorkommt.

**Zwei Nebenbefunde des alten „Öffnen"-Zweigs sind damit ebenfalls weg:**
`frm.m_szKlimaregion` war immer `""` (das Feld wurde nie befüllt) und `frm.m_ID_Projekt`
immer `0`. Der gemeinsame Ladeweg liest beides korrekt aus dem Projekt — genau wie es der
Zweig „zuletzt geöffnet" schon tat.

**Menü-Beschriftungen geprüft (beide Sprachen):** `MenuItem_ProjektOeffnen.Text` =
„Öffnen…" / „Open…" — nach dem Umbau sachlich richtig, keine Änderung nötig.
„Speichern unter" existiert weiterhin **nur** als Kachel, nicht im Menü; das Duplizieren
ist damit nirgends mehr hinter „Öffnen…" versteckt. Ein eigener Menüpunkt „Speichern
unter…" wäre eine Erweiterung der Menüstruktur und ist in diesem Schnitt nicht enthalten
(offener Punkt, siehe 7).

### 4.4 Kachel „Zuletzt geöffnet" — Abwägung

**Heutiger Zustand laut Inventar (3.2):** Die Kachel öffnete **keinen** Dialog. Sie las
`Tab_Applikation`, übernahm den Projektkontext, zeichnete 200 ms lang einen grünen
Aufblitz und zeigte danach ein `Form_Hinweis`.

**Zusatznutzen des Alten:** genau ein Klick, ohne Auswahl.
**Nachteil:** keine Sicht auf die Alternativen und kein Ausweg, wenn das gemerkte Projekt
nicht das gesuchte ist; der Doku-Anspruch „Öffnen zeigt eine Projektliste" (D5) blieb hier
unerfüllt.

**Entschieden (wie im Auftrag vorgegeben):** Die Kachel zeigt jetzt
`Form_ProjektAuswahl`, vorsortiert nach „Geändert" absteigend, mit dem zuletzt
geöffneten Projekt **vorausgewählt**. Der Ein-Klick-Charakter bleibt weitgehend erhalten —
die Eingabetaste genügt, weil die Vorauswahl schon steht. Danach läuft unverändert
`ProjektKontextUebernehmen`, das Fortschreiben von `Tab_Applikation` (wie bei den Kacheln
„Neues Projekt" und „Projekt öffnen/bearbeiten") und der `Form_Hinweis`.

**Der grüne Aufblitz entfällt.** Er zeichnete mit `CreateGraphics()` an der Kachel vorbei
(die `AktionsKarte` malt sich selbst) und blockierte dafür mit `Task.Wait()` den
UI-Faden — der als Befund 5.3/14 im Inventar steht. Die Rückmeldung übernimmt der
`Form_Hinweis`, der ohnehin schon da war.

### 4.5 `Form_ProjektSpeichernUnter`

* Load-Handler `Form_ProjektOpen_Load` → `Form_ProjektSpeichernUnter_Load` (`.cs` und
  `.Designer.cs`), mit Begründungskommentar.
* `en-US.resx` `$this.Text`: „Open Project" → „Save project as".

---

## 5. Beweise

### 5.1 Designer-Struktur-Lint

Eigenes Prüfwerkzeug (`dev\p13probe`, Teil A). Regeln je `InitializeComponent`:
nur serielle Zuweisungen/Aufrufe, jede Anweisung endet mit `;`, keine offene Klammer am
Zeilenende, keine Schlüsselwörter (`if`/`for`/`while`/`try`/`return`/`var`/`static`/
Sichtbarkeiten …), keine Lambdas, Kommentare nur im VS-Muster (`//` + ein Bezeichner),
`SuspendLayout`/`ResumeLayout`-Klammer vorhanden, Felddeklarationen erst nach
`#endregion`.

| Datei | Ergebnis |
|---|---|
| `AktionsKarte.Designer.cs` | **OK** — 40 Anweisungen, 12 VS-Kommentare, 3 Felder |
| `ProjektAuswahl.Designer.cs` | **OK** — 40 Anweisungen, 24 VS-Kommentare, 7 Felder |
| `Form_ProjektAuswahl.Designer.cs` | **OK** — 28 Anweisungen, 12 VS-Kommentare, 3 Felder |
| `Form_ProjektSpeichernUnter.Designer.cs` | **OK** — 76 Anweisungen, 33 VS-Kommentare, 10 Felder |
| `Form_Start.Designer.cs` | **OK** — 868 Anweisungen, 369 VS-Kommentare, 122 Felder |

### 5.2 Prüfstand `dev\p13probe` — 93 Prüfungen, alle grün

* **B — `Form_Start` real gebaut:** genau 6 `AktionsKarte` auf `tabPage1`, 0 `PictureBox`,
  0 Alt-Kachel-Labels; Lage/Größe jeder Karte deckungsgleich mit der Alt-Kachel
  (18/422/834 × 134 und 18/423/835 × 325, je 404×185); jedes `Geklickt` hat **genau
  einen** Abonnenten, und dessen Methodenname ist der alte Handler (Delegatvergleich,
  ohne Dialogaufruf und ohne DB-Wirkung); alle sechs Titel deutsch korrekt und im
  `en-US`-Betrieb 6/6 englisch; Klick-Verteiler ohne die sechs Kacheln; `label1` ohne
  Doppel-Leerzeichen; `btn_Help_Kurzanleitung` weiterhin aktiv.
* **AktionsKarte einzeln:** Klick auf **jedes** Kind (3) **und** auf die Karte selbst löst
  `Geklickt` aus → 4/4; `Cursor = Hand`; `StatusSichtbar` Vorgabe `false`; Fläche
  = `KartenStil.KARTE_FLAECHE`; `KartenStil.ECKE = 6` / `RAND = 10` unverändert;
  Titel/Beschreibung mittig und innerhalb der Karte.
* **C — `ProjektAuswahl` gegen `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`, nur lesend:**
  30 Projekte gelesen, 30 Zeilen, drei Spalten „Projektname / Kunde / Geändert";
  Vorgabesortierung Name aufsteigend; Suche „Bei" filtert 30 → 4, Suche ohne Treffer → 0,
  Zurücknehmen → 30/30; Sortierung „Geändert" absteigend 29.08.2026 … 18.03.2026;
  Spaltenklick kehrt die Richtung um; `ProjektGewaehlt` feuert mit `id=1026` /
  „Beispiel WP WG 1"; `Vorauswaehlen` trifft. `Form_ProjektAuswahl` baubar, OK/Abbrechen
  und eingebettetes UserControl vorhanden, Ergebnisfelder leer vorbelegt.
  *(Nur `SELECT` über `ProjektCtrl.ReadAll()`; keine Schreiboperation im Prüfstand.)*
* **D — resx:** 13 Dateien wohlgeformt; Schlüsselzahlen wie in 3.2; keine
  Alt-Kachelschlüssel mehr; die sechs JPG aus `Resources.resx` entfernt und in
  `Form_Start.Designer.cs` nicht mehr referenziert; Textfehler behoben;
  `FensterEinpassung.Einhaengen` nur noch im Begründungskommentar; `MenueCtrl` zeigt
  `Form_ProjektAuswahl`, erzeugt `Form_ProjektSpeichernUnter` genau einmal (in
  `ProjektSpeichernUnter()`) und enthält
  `SetWaermebedarfExternControl(frm.m_szProjekt)` nicht mehr; Relikt-Handlername und
  en-US-Titel richtiggestellt.

Läufe: `dev\p13probe\lauf_final.txt`.

### 5.3 Regressionsnetz

| Prüfstand | Lauf gegen `build_p13` | Ergebnis |
|---|---|---|
| `h7probe` (Info-/Hilfeknöpfe, Zuordnung Form_Start) | `dev\h7probe\lauf_p13.txt` | **ALLES GRUEN** |
| `h11probe` (Sammelpaket H11) | `dev\h11probe\lauf_p13.txt` | **ALLES GRUEN** |
| `h12probe` (feldgenaue Hilfe) | `dev\h12probe\lauf_p13.txt` | **ALLES GRUEN** |

`h7probe`/`h11probe` tragen ihren Build-Ordner als Konstante; sie wurden dafür auf
`build_p13` umgestellt (Sicherung je `Probe.cs.p13bak`). `h12probe` nimmt den Ordner als
Argument.

### 5.4 Build

`MSBuild WP-Plan.sln -t:Rebuild -p:Configuration=Debug -p:Platform=x64
-p:OutDir=C:\Waermeplan\WP_Plan\dev\build_p13\`
→ **0 Fehler, 5 Warnungen** — dieselben fünf wie vor dem Schnitt
(CS0108 `WErzeugerModel.ID_Projekt`, 2× CS0109 `KlimaregionStammCtrl`,
CS0108 `StromverbraucherStammCtrl.items`, CS1998 `MDIMainForm.cs:489`).
Keine neue Warnung.

---

## 6. Offene UI-Prüfpunkte (nur am Rechner des Nutzers zu erledigen)

1. **Jede geänderte/neue Maske einmal im VS-Designer öffnen** — Kern-Abnahmekriterium
   (Konzept 6/1). Betroffen: `Form_Start`, `AktionsKarte`, `ProjektAuswahl`,
   `Form_ProjektAuswahl`, `Form_ProjektSpeichernUnter`. Erwartung: öffnet ohne Fehler, die
   sechs Karten sind einzeln anklickbar, `Titel`/`Beschreibung`/`KartenBild`/
   `StatusSichtbar`/`StatusFarbe` erscheinen im Eigenschaftenfenster unter „Darstellung",
   `Geklickt` unter „Aktion".
2. **`AktionsKarte` in der Toolbox** — nach einem Build sollte sie unter den Komponenten
   des Projekts erscheinen und per Ziehen platzierbar sein.
3. **Optik der sechs Kacheln in DE und EN** — Titel/Beschreibung dürfen bei
   „Projekt öffnen/bearbeiten" (längster Text) nicht abgeschnitten wirken.
4. **Menü „Projekt → Öffnen…"** — zeigt die Liste, öffnet das gewählte Projekt im
   Detailformular, dupliziert nichts.
5. **Kachel „Zuletzt geöffnet"** — Liste nach „Geändert" sortiert, gemerktes Projekt
   vorausgewählt, Eingabetaste genügt.
6. **Reiter „Komponenten auswählen" im Assistenten** — Überschrift lautet jetzt
   „Projekt-Erstellungskonfiguration", der graue Balken hat kein Doppel-Leerzeichen.

---

## 7. Offene Punkte / bewusst nicht enthalten

| # | Punkt | Warum offen |
|---|---|---|
| O1 | Menüpunkt „Speichern unter…" | Duplizieren ist heute nur Kachel; ein neuer Menüeintrag wäre eine Menü-Erweiterung, nicht eine Beschriftungskorrektur. Vorschlag für P4/P6. |
| O2 | `Form_ProjektSpeichernUnter.en-US.resx` ohne `label2.Text` | im Englischen steht dort weiter „Neuer Projektname:". Auftrag nannte nur den Fenstertitel; einzeilig in P4/P6 nachziehbar. |
| O3 | `Form_ProjektSpeichernUnter.cs:52` `listView_Projekt.Items[0].Selected` außerhalb der `if`-Prüfung | wirft bei **null** Projekten. Altbestand, außerhalb dieses Schnitts. |
| O4 | `help_mapping.txt` für `Form_ProjektAuswahl`/`ProjektAuswahl` | ausdrücklich P6. |
| O5 | `Resources\P*.jpg` der sechs Kacheln liegen unbenutzt auf der Platte | keine Buildwirkung; Löschen erst, wenn kein Rückweg mehr gebraucht wird. |
| O6 | `RoundedPanel` / `ChartManager.Kacheln` mit abweichender Bogensemantik | die Ablösung der sechs Rundeck-Kopien gehört in einen späteren Schnitt; `KartenStil.Rundeck` ist ab jetzt die Vorgabe für Neues. |
| O7 | Kachel-Statuspunkt (`StatusSichtbar`) noch ungenutzt | Die sechs Projektkacheln tragen keinen Status. Der Baustein steht bereit, sobald die Fach-Reiter nachziehen (E6). |
| O8 | `Wizard_Komponenten` `$this.Text` = `frm1`/`ab1`/`from 1` | Wizard-Rahmen ist P4. |

---

## 8. Werkzeuge (Wegwerf, unter `dev\`)

| Datei | Zweck |
|---|---|
| `dev\p13probe\p13probe.csproj`, `Probe.cs` | Prüfstand A–D |
| `dev\p13probe\resx_umbau.ps1` | Alt-Kachelschlüssel raus, `karte_*` rein, `label1`-Text, ZOrder |
| `dev\p13probe\resx_neu.ps1` | erzeugt die sechs neuen `.resx` im VS-Format |
| `dev\p13probe\resx_texte.ps1` | Textfehler `Wizard_Komponenten` |
| `dev\p13probe\resx_bilder.ps1` | entfernt die sechs JPG aus `Resources.resx`/`.Designer.cs` |
| `dev\p13_resx_vorher\` | Sicherung der vier `.resx` vor dem Umbau |

**Merke für Nachfolger:** Alle vier `.ps1` **müssen** mit UTF-8-BOM gespeichert sein —
Windows PowerShell 5.1 liest BOM-lose Skripte als ANSI und schreibt sonst Mojibake in die
`.resx`.
