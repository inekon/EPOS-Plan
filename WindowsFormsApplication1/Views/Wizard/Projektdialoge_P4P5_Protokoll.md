# Projektdialoge vereinheitlichen — Umsetzungsprotokoll Schnitt 2 (P4 + P5)

Stand: 29.08.2026. Grundlage:
[`Konzept_Projektdialoge_Vereinheitlichung.md`](../../../Konzept_Projektdialoge_Vereinheitlichung.md)
Abschnitte 4 (Entscheidungen E1–E6), 5 (Pakete P4/P5) und 8 (Fallstricke); Vorgänger:
[`Projektdialoge_P1P3_Protokoll.md`](../Hauptformular/Projektdialoge_P1P3_Protokoll.md)
(HEAD 0a270a1).

**Entscheidungen dieses Schnitts:** E1(b) Kachelauswahl aus dem Anlagenbestand · E2 die
Kachel „öffnen/bearbeiten" bleibt beim Assistenten · E3 Rückfrage mit Klartext,
Vorbelegung **Nein**, Kessel-Lücke geschlossen · **E4 Option (a): der Logo-Klick ist
ersatzlos entfernt** (Nutzerentscheid 29.08.2026, während der Umsetzung nachgereicht) ·
E5 „Projektassistent" statt „Projekt Wizard".

**Kern-Leitplanke eingehalten:** Alles Umgebaute ist im Visual-Studio-Designer
bearbeitbar — `.Designer.cs` im VS-Serialisierungsmuster (maschinell geprüft),
Lokalisierung über die Satelliten-`.resx`, kein Laufzeitaufbau von Layout.

---

## 1. Was neu ist, was sich geändert hat

### Neue Dateien

| Datei | Rolle |
|---|---|
| `Allgemein/IAssistentRahmen.cs` | Vertrag zwischen Assistentenseiten und Rahmen (`Seiten`, `Betriebsart`, `ProjektID`) |
| `Views/Wizard/AssistentSeiten.cs` | die **eine** Definition der dreizehn Assistentenseiten |
| `Views/Wizard/KomponentenBestand.cs` | der Komponentenbestand eines Projekts — die eine Wahrheit hinter den Kacheln |

### Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Views/Wizard/WizardParent.cs` | `IAssistentRahmen`; statische Anmeldung `Aktiver`; linke Spalte = `ProjektAuswahl`; `btnOeffnen_Click`, `FillProjektList`, `SetProjektLabel`, `SetKompCheckBoxes` und **`pictureBox_App_Click` (E4)** entfernt; `KomponentenAusBestandSetzen`; Kessel-Zeile in der Löschroutine; `EntferneNichtAktiveZuordnungen`; Rückfrage vor „Neues Projekt…" |
| `Views/Wizard/WizardParent.designer.cs` | `listBox_Projekte` → `ucProjektAuswahl`; `btnOeffnen` entfernt; `pictureBox_App.Click`-Verdrahtung entfernt |
| `Views/Wizard/WizardParent.resx` / `.de-DE.resx` / `.en-US.resx` | Titel „Projektassistent"; Weiter/Zurück/Abbrechen in beiden Sprachen; Spalte 219 → 300 px; `tableLayoutPanel`-Leichen weg |
| `Views/Wizard/Wizard_Komponenten.cs` | elf Häkchen-Handler und einundzwanzig tote `Get*/Set*CheckBox` → Kachellogik mit E3-Rückfrage |
| `Views/Wizard/Wizard_Komponenten.designer.cs` | elf `CheckBox` → **dreizehn `AktionsKarte`** + `panel_Textvorlagen` |
| `Views/Wizard/Wizard_Komponenten.resx` / `.de-DE.resx` / `.en-US.resx` | Kacheltitel, Satzbausteine, Erklärtext auf „Assistent", Titel-Relikt `frm1`/`ab1`/`from 1` behoben |
| `Controller/MenueCtrl.cs` | die zwei 13-Zeilen-Kopien → `AssistentSeiten.Erzeugen()`; `ProjektNeu`/`ProjektBearbeiten` rufen `AssistentZeigen(betriebsart)` |
| `Views/GemeinsameBausteine/AktionsKarte.cs` | neue Eigenschaft `TitelSchrift` (+ `ShouldSerialize`/`Reset`) |
| `Views/Projekt/ProjektAuswahl.cs` / `.resx` | neu: `NurNamensspalte`, `AutomatischeVorauswahl`, Ereignis `MarkierungGeaendert`; Suchfeld verankert |
| 10 Fachformulare (`getWizardPage()`) | Namenssuche in `Application.OpenForms` → `WizardParent.Aktiver` |

**Nicht angefasst** (Vorgabe): `help_mapping.txt`, `DbWerte.cs`, `SchemaMigration.cs`,
`SchemaKatalog.cs`, `KiKern`, `KiSchreibschutz`, `BetriebskostenCtrl`,
`KostenVorlagenCtrl`, `CLAUDE.md`, `MyResource/Resource*`. Kein Git-Schreibbefehl.

> **Hinweis für den Abgleich.** Während dieses Schnitts liefen Parallelsessions. Die
> hier ergänzte Eigenschaft `AktionsKarte.TitelSchrift` ist bereits in einen fremden
> Commit geraten (`ce12371` „AktionsKarte: Logo-Anordnung wie die frueheren Kacheln",
> ein `git add -A`-Sync) — dieselbe Datei wurde dort zusätzlich verbessert
> (Logo-Anordnung, `Visible`-Getter-Falle). Beides steht widerspruchsfrei
> nebeneinander: Die Komponentenkacheln laufen durch den „ohne Bild"-Zweig, und der
> abschließende Prüfstandslauf misst gegen genau diesen Stand. Alle übrigen Dateien
> dieses Schnitts sind unversioniert geblieben.

---

## 2. P4 — der Assistenten-Rahmen

### 2.1 Begriff (E5)

| Ort | vorher | nachher (de) | nachher (en) |
|---|---|---|---|
| `WizardParent.resx` `$this.Text` | `Projekt Wizard` | **`Projektassistent`** | `Project assistant` |
| `Wizard_Komponenten` `$this.Text` | `frm1` / `ab1` / `from 1` | `Komponenten auswählen` | `Select components` |
| `Wizard_Komponenten` `label2` (Erklärtext) | „Mit dem **Projekt Wizard** … Navigieren Sie mit **Next und Back**" (neutral) bzw. „Weiter und Zurück" (de-DE) | „Der **Projektassistent** führt Sie … Navigieren Sie mit **Weiter** und **Zurück**" | „The **project assistant** guides you … Navigate with **Next** and **Back**" |

Klassen- und Dateinamen bleiben `WizardParent` / `Wizard_*` — kein Umbenennungssturm
(die Namen stehen in `help_mapping.txt`, `HilfeKontext` und der Wiki-Vertragstabelle).

### 2.2 Sprachlücke geschlossen

Vorher: `WizardParent.de-DE.resx` hatte **einen** Eintrag, `en-US` dreißig (davon acht
Geometrie-Leichen der entfernten `tableLayoutPanel1/2`). Im **deutschen** Programm
standen `Next ▶` / `◀ Back`, im **englischen** `Abbrechen ❌` / `📂 Öffnen`.

Nachher — beide Satelliten tragen **nur Texte**, die Geometrie steht einmal in der
neutralen Datei:

| Schlüssel | neutral = de-DE | en-US |
|---|---|---|
| `$this.Text` | Projektassistent | Project assistant |
| `btnNext.Text` | `Weiter ▶` | `Next ▶` |
| `btnBack.Text` | `◀ Zurück` | `◀ Back` |
| `btnCancel.Text` | `Abbrechen` | `Cancel` |
| `btnSpeichern.Text` | Speichern | Save |
| `button_NeuProjekt.Text` | Neues Projekt... | New project... |
| `label_Projekt.Text` | Bestehendes Projekt auswählen | Select existing project |

Die Begriffe sind mit der Startmaske deckungsgleich (`Form_Start.de-DE.resx`:
`btn_Weiter = Weiter ▶`, `btn_Zurueck = ◀ Zurück`). Das Kreuz-Emoji hinter „Abbrechen"
ist weg — es stand nur in einer der beiden Sprachen.

### 2.3 Seitenliste einmalig

**Vorher:** dieselben dreizehn Zeilen wortgleich in `MenueCtrl.cs:28–41`
(`ProjektNeu`) und `:56–69` (`ProjektBearbeiten`).

**Nachher:** `Views/Wizard/AssistentSeiten.cs`. Der Bauplan steht als
`Func<Form>[]` — vom Übersetzer geprüft, anders als eine `Type`-Liste mit
`Activator.CreateInstance`. `Seitentypen` ist eine `ReadOnlyCollection<Type>` in
**einer** Instanz und dient dem Nachweis. `MenueCtrl.ProjektNeu()` und
`…ProjektBearbeiten()` sind auf je eine Zeile geschrumpft und rufen beide
`AssistentZeigen(betriebsart)`; **`new WizardItemClass(` kommt in `MenueCtrl.cs`
nicht mehr vor** (Prüfstand C).

Reihenfolge und Inhalt sind unverändert: Index 0…12 = `KOMPONENTEN_ITEM` …
`BHKW_ITEM`.

### 2.4 Typisierte Rahmen-Erkennung

**Vorher** (elf wortgleiche Kopien):

```csharp
foreach (Form form in Application.OpenForms)
    if (form.Name == "WizardParent") return form;
```

**Nachher:**

```csharp
return WizardParent.Aktiver as Form;
```

`WizardParent` setzt `IAssistentRahmen` um und trägt sich im **Konstruktor** ein
(die erste Fachseite wird schon aus `WizardParent_Load` heraus bestückt, also vor
jedem `Shown`). Abgemeldet wird in `OnFormClosed` **und** über `Disposed` — ein
gebauter, aber nie gezeigter Rahmen (abgebrochener Einstieg, Prüfstand) hinterlässt
sonst einen Eintrag, der auf ein totes Fenster zeigt. Ein zuvor eingetragener Rahmen
wird dabei wiederhergestellt.

Die elf Fundstellen (Kodierung je Datei geprüft, **byte-schonend** ersetzt mit
Rundprobe):

| Datei | Kodierung | Bytes vorher → nachher |
|---|---|---|
| `Views/BHKW/Form_BHKWEing.cs` | UTF-8 | 43768 → 43825 |
| `Views/Stromspeicher/Form_Stromspeicher.cs` | UTF-8 | 14012 → 14069 |
| `Views/Wizard/Wizard_Stromlastgang.cs` | UTF-8 | 4194 → 4251 |
| `Views/Pufferspeicher/Form_PufferSp.cs` | UTF-8 | 17709 → 17766 |
| `Views/Heizkessel/Form_Heizkessel.cs` | UTF-8 | 36792 → 36849 |
| `Views/Photovoltaik/Form_PV.cs` | UTF-8 | 13080 → 13137 |
| `Views/Wärmepumpe/Form_WPAuswahl.cs` | UTF-8 | 15183 → 15240 |
| `Views/Gebäude/Form_Gebaeude.cs` | UTF-8 | 28711 → 28768 |
| **`Views/Solarthermie/Form_SolarKollektoren.cs`** | **CP1252** | 21876 → 21933 |
| **`Views/Solarthermie/Form_SolarKollektorenAdmin.cs`** | **CP1252** | 7692 → 7749 |
| `Views/Wizard/Wizard_Komponenten.cs` | UTF-8 | (neu geschrieben) |

Die beiden CP1252-Dateien wurden über Latin-1 (byte-treu) gelesen und geschrieben,
je mit Rundprobe Byte für Byte; `git diff` zeigt dort **genau** den ersetzten
Methodenrumpf, und der Prüfstand belegt, dass beide Dateien weiterhin **kein**
gültiges UTF-8 sind (die Kodierung ist also erhalten geblieben).

> Der Auftrag nannte neun Fachformulare und vier CP1252-Dateien. Tatsächlich waren es
> **elf** Fundstellen; von den vier genannten CP1252-Dateien führt nur
> `Form_SolarKollektoren.cs` eine — `Form_Waermebedarf.cs`, `Form_Prozesswaerme.cs`
> und `Form_Stromverbraucher.cs` haben gar kein `getWizardPage()`. Dafür kam
> `Form_SolarKollektorenAdmin.cs` (ebenfalls CP1252) hinzu; dort ist die Methode
> unbenutzt, wurde aber gleich mit umgestellt, damit keine zwölfte Kopie stehen bleibt.

### 2.5 Linke Spalte, „📂 Öffnen", „Neues Projekt…"

**Projektliste → `ProjektAuswahl`.** Die alte `ListBox` (`WizardParent.cs:265–273`,
Designer + `resx`) ist durch das UserControl aus Schnitt 1 ersetzt — im Designer
eingebettet, mit Suche und Sortierung. Damit sie in eine Seitenspalte passt, hat
`ProjektAuswahl` zwei neue Designer-Eigenschaften bekommen:

| Eigenschaft | Vorgabe | im Assistenten | warum |
|---|---|---|---|
| `NurNamensspalte` | `false` | `true` | drei Spalten (220 + 150 + 120 px) passen nicht in 272 px; die Namensspalte wird auf die volle Breite gezogen |
| `AutomatischeVorauswahl` | `true` | `false` | im Dialog soll OK sofort etwas tun; im Assistenten darf „Weiter" erst wirken, wenn der Anwender ausdrücklich gewählt hat (wie bisher) |

Neu ist außerdem das Ereignis `MarkierungGeaendert(id, name)` — der Assistent hängt
daran das Nachladen der Komponentenkacheln (an der Stelle des alten
`listBox_Projekte_SelectedIndexChanged`). `ProjektGewaehlt` (Doppelklick/OK) bleibt
unverändert, `Form_ProjektAuswahl` ist nicht berührt (p13probe grün).

Die Spalte `pnlLeft` ist von **219 auf 300 px** verbreitert, damit Suchfeld und Liste
lesbar bleiben; `pnlContent` bekommt entsprechend 946 statt 1027 px. Fensterbreite,
`MinimumSize` und alle Knopfpositionen sind unverändert.

**„📂 Öffnen" entfällt ersatzlos.** Begründung: Der Knopf trug seit jeher den
TODO-Kommentar „Aktion fuer 'Oeffnen' festlegen" (`WizardParent.cs:422`), wurde nie
ein- oder ausgeblendet und stand deshalb auch im Neu-Modus da, wo er nur die
MessageBox „Projekt auswählen!" erzeugen konnte. Seine einzige sinnvolle Wirkung —
ein bestehendes Projekt wählen — leistet jetzt die Spalte daneben, und zwar mit
Liste, Suche und Sortierung. Zwei Knöpfe für dieselbe Sache wären genau die
Doppelung, die dieses Konzept auflöst. Das Duplizieren („Speichern unter…") und das
echte Öffnen ins Detailformular liegen seit P3 an ihren ehrlichen Stellen.

**„Neues Projekt…" bleibt, fragt aber nach.** Der Knopf schaltet den laufenden
Bearbeiten-Assistenten mitten im Betrieb auf den Neu-Modus um und leerte dabei
kommentarlos alle sechs Modelllisten. Ob etwas zu verlieren ist, **ist erkennbar**:
`projektID > 0` (ein Projekt ist gewählt), `bBereitsGeladen` (die Bestandsdaten sind
nachgeladen, der Anwender war also mindestens auf Seite 1) oder eine gefüllte
Modellliste. Genau danach fragt `HatVerwerfbareEingaben()`; nur dann erscheint die
Rückfrage (Vorbelegung **Nein**), sonst wird stillschweigend umgeschaltet.

### 2.6 E4 — Logo-Klick ersatzlos entfernt

`pictureBox_App_Click` (`WizardParent.cs:741–761`) öffnete beim Klick auf das
INEKON-Logo einen `OpenFileDialog` und schrieb den gewählten Pfad **dauerhaft als
Anwendungs-Icon** nach `Tab_Applikation` — ohne Hinweis in der Oberfläche und ohne
Rückfrage. Entfernt sind:

* die Handler-Methode (mit Begründungskommentar an ihrer Stelle),
* die Verdrahtung `pictureBox_App.Click += pictureBox_App_Click;` im Designer.

Eine Hand-Optik gab es am Logo nie (kein `Cursor`-Eintrag in Designer oder `.resx`) —
der Prüfstand belegt `Cursor = Default`. Das Logo bleibt als reines Bild stehen.

**Nichts ist dadurch tot geworden — belegt per Grep über den Haupt-Checkout:**

* `ApplikationCtrl.Update()` hat weitere Aufrufer (`Form_Start.cs:404` und `:873`,
  Projektkontext fortschreiben) — die Methode bleibt.
* Das Feld `m_icon` wird nach der Entfernung **nirgends mehr beschrieben**: Die
  einzigen verbliebenen Fundstellen sind `ApplikationCtrl.Update()` (Parameter,
  schreibt den gelesenen Wert unverändert zurück), `ApplikationCtrl.ReadSingle()`
  (Zeile 149, füllt es), `ApplikationModel` (Deklaration) und
  `WizardParent.cs:157` (`SetImageFromFile(ctrl.m_icon)`, **Lesepfad**).
* `SetImageFromFile` bleibt deshalb stehen — sie ist der Lesepfad, den der
  Konstruktor braucht.

Es gibt also keine Hilfsmethode, die mit dem Handler mit entfernt werden müsste.

---

## 3. P5 — der Komponentenschritt

### 3.1 Bestandsaufnahme (Auftragspunkt 6)

#### (a) Wie die Startmaske den Anlagenbestand liest

`Form_Start.UpdateWizardSymbole()` (`Views/Hauptformular/Form_Start.cs:1657–1718`)
füllt die öffentliche Bitmaske `status` (`Form_Start.cs:14`); die Paint-Handler der
Kacheln malen daraus den grünen Punkt.

| Bit | Komponente | Kriterium (wörtlich aus `UpdateWizardSymbole`) |
|---:|---|---|
| 1 | Spitzenkessel | `WErzeugerCtrl.ReadAllFilter("ID_Projekt=… and ID_Type=10")` |
| 2 | Wärmepumpe | `… ID_Type=1` |
| 4 | Stromspeicher | `… ID_Type=4` |
| 8 | Gebäude | `Z_ProjGebCtrl` → `Z_ProjektGebaeude` |
| 16 | Wärmebedarf | `Z_ProjektGebGanglinieCtrl` → `Z_ProjektWaermebedarf` |
| 32 | Prozesswärme | `Z_ProjektProzesswaermeCtrl` → `Z_Projekt_Prozesswaerme` |
| 64 | Stromverbraucher | `Z_ProjektStromverbraucherCtrl` → `Z_Projekt_Stromverbraucher` |
| 128 | Stromlastgang | `Z_ProjektStromganglinieCtrl` → `Z_ProjektStromganglinie` |
| 256 | BHKW | `… ID_Type=11` |
| 512 | Solarthermie | `… ID_Type=2` **oder** `Z_ProjektSolarganglinie` |
| 1024 | Photovoltaik | `… ID_Type=3` |
| 2048 | Pufferspeicher | `… ID_Type=12` |
| 4096 | Brauchwasser | `Z_ProjektBrauchwasserCtrl` → `Z_Projekt_Brauchwasser` |

Der Assistent führte dafür **eine zweite, abweichende Ermittlung**
(`WizardParent.SetKompCheckBoxes`, `:437–514`): Sie verlangte bei den Anlagen
zusätzlich `ID_WP > 0`, `ID_Solar > 0`, `ID_PV > 0`, `ID_SP > 0`, `ID_Kessel > 0`
bzw. `ID_BHKW > 0`, kannte die Solarganglinie nicht und ließ Brauchwasser und
Pufferspeicher ganz aus. Ab P5 gilt **die Bitmaske**; `KomponentenBestand` bildet
sie Zeile für Zeile nach (maschinell nachgewiesen, Abschnitt 4.3).

#### (b) Was `entferne_nicht_aktive_elemente` löschte — und warum der Kessel fehlte

Der Bearbeiten-Zweig von `btnSpeichern_Click` schreibt Anlagen als **Löschen +
Neuanlegen**: `Del_Projekt_Waermeerzeuger(projektID)` entfernt alle Zeilen des
Projekts außer `ID_Type = 12`, danach legt `Add_WP_Waermeerzeuger(projektID,
list_werzmodel)` genau das wieder an, was in der Liste steht. **Alles, was das
Prädikat aus der Liste nimmt, ist damit gelöscht.**

Das Prädikat (`WizardParent.cs:724–739`, vorher) führte:

```
ID_Type 12 (Puffer)  immer            -> aus der Liste, aber NICHT geloescht (FR-1)
SOLAR_ITEM  inaktiv  -> ID_Type 2     -> geloescht
SP_ITEM     inaktiv  -> ID_Type 4     -> geloescht
PV_ITEM     inaktiv  -> ID_Type 3     -> geloescht
WP_ITEM     inaktiv  -> ID_Type 1     -> geloescht
BHKW_ITEM   inaktiv  -> ID_Type 11    -> geloescht
                        ID_Type 10    -> FEHLTE
```

**Warum der Kessel fehlte:** Die fünf Zeilen decken genau die fünf Häkchen ab, die
`Wizard_Komponenten` in seiner ersten Fassung als *Erzeuger* führte (WP, Solar, PV,
Stromspeicher, BHKW). `checkBox_Kessel` kam später dazu; es setzte
`KESSEL_ITEM.aktiv` (`Wizard_Komponenten.cs:154–160`) und schaltete damit die Seite
`Form_Heizkessel` frei — die passende Prädikatzeile hat aber nie jemand nachgetragen.
Folge: Ein abgewählter Spitzenkessel blieb in `list_werzmodel`, wurde nach dem Löschen
sofort wieder angelegt und stand danach weiter im Projekt, obwohl der Assistent ihn
als „nicht enthalten" zeigte — Startmaske, Detailformular und Assistent widersprachen
sich.

Zwei weitere Ungleichheiten sind dabei aufgefallen (siehe Lösch-Matrix 3.3):

* **Gebäude, Wärmebedarf, Prozesswärme** wurden beim Abwählen *zufällig* gelöscht:
  `btnSpeichern_Click` liest diese drei Listen aus der jeweiligen **Seite**
  (`Form_Gebaeude.list_gebmodel` usw.). Nie besucht = leere Liste = gelöscht. Wer die
  Kachel aber erst einschaltete, die Seite besuchte und sie danach wieder ausschaltete,
  behielt seine Daten.
* **Stromlastgang und Stromverbraucher** wurden **nie** gelöscht: Ihre Listen führt der
  Rahmen selbst und leerte sie nicht.

#### (c) Welche Assistentenseite je Komponente

| Komponente | Seitenindex (`WizardItemClass`) | Formular |
|---|---|---|
| Gebäude | `GEBAEUDE_ITEM` = 2 | `Form_Gebaeude` |
| Wärmebedarfsdaten | `WAERMEBEDARF_ITEM` = 3 | `Form_Waermebedarf` |
| Prozesswärme | `PROZESS_ITEM` = 4 | `Form_Prozesswaerme` |
| **Brauchwasser** | — | **keine Seite** |
| Standard-Stromlastprofil | `STROMSTD_ITEM` = 5 | `Form_Stromverbraucher` |
| Stromlastgang | `STROMLASTGANG_ITEM` = 6 | `Wizard_Stromlastgang` |
| Wärmepumpe | `WP_ITEM` = 7 | `Form_WPAuswahl` |
| Solarthermie | `SOLAR_ITEM` = 8 | `Form_SolarKollektoren` |
| Photovoltaik | `PV_ITEM` = 9 | `Form_PV` |
| Stromspeicher | `SP_ITEM` = 10 | `Form_Stromspeicher` |
| Spitzenkessel | `KESSEL_ITEM` = 11 | `Form_Heizkessel` |
| BHKW | `BHKW_ITEM` = 12 | `Form_BHKWEing` |
| **Pufferspeicher** | `PUFFER_ITEM` = 13 | **Konstante ohne Formular** |

### 3.2 Umbau

**Elf Häkchen → dreizehn Kacheln.** `Wizard_Komponenten` trägt jetzt dreizehn
`AktionsKarte`-Instanzen, im Designer platziert (3 Spalten × 5 Reihen, je 250 × 80 px):

| Reihe | Kacheln |
|---|---|
| 1 | Gebäude · Wärmebedarfsdaten · Prozesswärme |
| 2 | **Brauchwasser** · Standard-Stromlastprofil · Stromlastgang |
| 3 | Wärmepumpe · BHKW · Spitzenkessel |
| 4 | Solarthermie · Photovoltaik · Stromspeicher |
| 5 | **Pufferspeicher** |

`StatusSichtbar` ist immer an; `StatusFarbe` ist das Grün der Startmaske
(`KartenStil.KARTE_STATUS`, dasselbe `90,0,255,0` wie dort) für „im Projekt" und
`KartenStil.KARTE_RAHMEN` (hellgrau) für „nicht im Projekt". Die Beschreibung
darunter nennt zusätzlich die Anzahl („3 im Projekt" / „nicht im Projekt").

Damit Überschrift und Beschreibung in eine 80 px hohe Karte passen, hat
`AktionsKarte` die Eigenschaft **`TitelSchrift`** bekommen (Vorgabe wie bisher
„Segoe UI Semibold 13 pt fett"; die Komponentenkacheln setzen 10 pt). `ShouldSerialize`
und `Reset` sind gesetzt, damit der Designer die Vorgabe nicht serialisiert — die
sechs Startmasken-Kacheln bleiben dadurch unverändert (p13probe grün).

**Die Seite skaliert jetzt wie ihr Rahmen.** `Wizard_Komponenten` führte
`AutoScaleDimensions = 6, 13`, der Rahmen `7, 15` — jede Größe wurde zur Laufzeit um
7/6 bzw. 15/13 gedehnt. Die neue Geometrie ist in **7, 15** ausgelegt, und zwar mit
genau den Werten, die der Altstand zur Laufzeit ergab (Kopfbild 712 × 106 → 831 × 122
usw.). Optisch ändert sich am Kopfbereich damit nichts.

**Eine Quelle für den Zustand.** `KomponentenBestand.Lesen(idProjekt)` liest den
Bestand mit den Kriterien der Startmaske und liefert je Komponente `Vorhanden`,
`Anzahl`, `Namen`, `Bitwert` und `SeitenIndex`. Es gibt **keine** parallele Merkliste:
`Wizard_Komponenten.BestandAnzeigen(bestand)` setzt daraus die Kacheln **und** die
`aktiv`-Flags der Seiten. Kein neues SQL — jede Abfrage steht wortgleich schon im
Bestand (`WErzeugerCtrl.ReadAllFilter("ID_Projekt=…")` aus `LoadWEFromDB`, die fünf
`Z_*`-Abfragen aus `SetKompCheckBoxes`, die Brauchwasser-Abfrage aus
`UpdateWizardSymbole`, der Gebäude-Verbund aus `LoadZGeb`).

**Brauchwasser und Pufferspeicher sind Anzeigekacheln** — sie zeigen den Bestand
ehrlich an, lassen sich aber nicht umschalten (Beschreibung: „… · nur Anzeige").
Begründung: Der Assistent führt für beide keine Seite, es gäbe also nichts
freizuschalten; und ein Ausschalten müsste einen **neuen** Löschweg erfinden, den es
bisher nicht gibt (`Del_Projekt_Waermeerzeuger` verschont `ID_Type 12` ausdrücklich,
Brauchwasser fasst der Assistent nirgends an). Damit ist Prüfpunkt 9 der Abnahmeliste
erfüllt — sichtbar und wirksam in dem Sinn, dass der Bestand angezeigt wird und der
Assistent ihn nicht mehr stillschweigend übergeht.

**Tote Getterei bereinigt.** Entfernt sind die fünf `Get*Status`-, die fünf
`Get*CheckBox`- und die elf `Set*CheckBox`-Methoden (21 Stück), das Feld
`parentForm`, der `Shown`-Handler mit der Namenssuche sowie der auskommentierte
Klimazonen-Block in `WizardParent.btnSpeichern_Click` (`:590–601`), der genau diese
Getter rief. Grep über den Haupt-Checkout: keiner der 21 Namen hatte einen Aufrufer
außerhalb von `Views/Wizard/`.

### 3.3 Lösch-Matrix je Komponente

Was beim **Abwählen** im Bearbeiten-Modus geschieht — vorher und nachher. „E3" heißt:
es wird vorher gefragt (Klartext mit Anzahl und Namen, Vorbelegung **Nein**); ohne
„Ja" ändert sich nichts.

| Komponente | Tabelle | vorher | nachher |
|---|---|---|---|
| Gebäude | `Z_ProjektGebaeude` (+`Tab_Gebaeude`) | gelöscht — **aber nur**, wenn die Seite nie besucht war | E3, dann gelöscht (`EntferneNichtAktiveZuordnungen`) |
| Wärmebedarfsdaten | `Z_ProjektWaermebedarf` | dito | E3, dann gelöscht |
| Prozesswärme | `Z_Projekt_Prozesswaerme` | dito | E3, dann gelöscht |
| Brauchwasser | `Z_Projekt_Brauchwasser` | nicht abwählbar (fehlte) | Anzeigekachel — **nie** gelöscht |
| Standard-Stromlastprofil | `Z_Projekt_Stromverbraucher` | **nie gelöscht** (Rahmenliste blieb gefüllt) | E3, dann gelöscht |
| Stromlastgang | `Z_ProjektStromganglinie` | **nie gelöscht** | E3, dann gelöscht |
| Wärmepumpe | `Tab_Energieanlagen` `ID_Type 1` | still gelöscht | E3, dann gelöscht |
| BHKW | `… ID_Type 11` | still gelöscht | E3, dann gelöscht |
| **Spitzenkessel** | `… ID_Type 10` | **gar nicht gelöscht** (Lücke) | E3, dann gelöscht |
| Solarthermie | `… ID_Type 2` | still gelöscht | E3, dann gelöscht |
| Photovoltaik | `… ID_Type 3` | still gelöscht | E3, dann gelöscht |
| Stromspeicher | `… ID_Type 4` | still gelöscht | E3, dann gelöscht |
| Pufferspeicher | `… ID_Type 12` | nicht abwählbar (fehlte) | Anzeigekachel — **nie** gelöscht (FR-1 unverändert) |

Umgesetzt an **zwei** Stellen, beide im bestehenden Speicherweg — kein neues SQL:

1. `entferne_nicht_aktive_elemente` bekam **eine** Zeile für `KESSEL_TYP`.
2. `EntferneNichtAktiveZuordnungen()` (neu) leert vor dem Speichern die fünf
   Zuordnungslisten abgewählter Komponenten. Damit verhalten sich alle elf gleich,
   und die oben beschriebene Zufälligkeit („Seite besucht oder nicht") ist weg.

Die Solarganglinie (`Z_ProjektSolarganglinie`) wird vom Assistenten weiterhin **nicht**
angefasst. Sie kann die Solar-Kachel allein auf „im Projekt" setzen (Kriterium der
Bitmaske); in diesem Fall meldet der Bestand 0 Anlagen, es wird nicht gefragt, und es
geht auch nichts verloren.

### 3.4 Texte

| Fundstelle | vorher | nachher |
|---|---|---|
| `Wizard_Komponenten` `label3` (grauer Balken) | Doppel-Leerzeichen (in Schnitt 1 behoben) | unverändert, Prüfstand belegt: kein Doppel-Leerzeichen in neutral **und** de-DE |
| `label2` (Erklärtext) | „Mit dem Projekt Wizard …" / „… mit Next und Back" | „Der Projektassistent führt Sie …" / „… mit Weiter und Zurück" |
| `$this.Text` | `frm1` / `ab1` / `from 1` | „Komponenten auswählen" / „Select components" |
| `checkBox_Kessel` en-US | `Top boiler` (die Startmaske sagte `Boiler`) | `Boiler` — beide Masken sagen jetzt dasselbe |
| `checkBox_Solar` | „Solar" | „Solarthermie" / „Solar thermal" (wie die Startmaske) |
| en-US-Stand | vollständig, aber mit den beiden Fehlgriffen oben | **24 Schlüssel**, alle dreizehn Kacheltitel und alle sieben Satzbausteine |

**Wo die Satzbausteine stehen.** Die Kachelbeschreibung und die beiden Rückfragen
brauchen übersetzbare Texte mit Platzhaltern. Sie stehen als Entwurfstexte von sieben
unsichtbaren Vorlage-Label in `panel_Textvorlagen` — dasselbe Muster, mit dem
`ProjektAuswahl` seit Schnitt 1 seine Zählzeile führt (`label_Anzahl.Text =
"{0} von {1} Projekten"`). Der Weg wurde bewusst dem Eintrag in
`MyResource/Resource.resx` vorgezogen: Neue Schlüssel dort zögen eine Hand-Änderung an
`Resource.Designer.cs` nach sich, und genau die ist im Projektgedächtnis als
Duplikat-Falle (CS0102) vermerkt, weil Visual Studio die Datei selbst regeneriert. Die
Vorlage-Label sind dagegen ganz normale Designer-Steuerelemente und überstehen jeden
Designer-Durchlauf.

| Vorlage | de | en |
|---|---|---|
| `label_TextEnthalten` | `{0} im Projekt` | `{0} in project` |
| `label_TextOhne` | `nicht im Projekt` | `not in project` |
| `label_TextNurAnzeige` | `nur Anzeige` | `display only` |
| `label_TextFrage` | „`{0}`" wird aus dem Projekt genommen. / Beim Speichern werden `{1}` Einträge gelöscht: / `{2}` / Wirklich entfernen? | analog |
| `label_TextFrageTitel` | Komponente entfernen | Remove component |
| `label_TextNeuFrage` | Der Assistent wechselt auf ein neues Projekt. / Die Eingaben dieses Durchlaufs werden verworfen. / Fortfahren? | analog |
| `label_TextNeuTitel` | Neues Projekt beginnen | Start new project |

Mehrzeilige `.resx`-Werte kommen je nach Leser mit LF oder CRLF an; `Vorlage()` schickt
sie deshalb durch `Zeilenumbruch.Normalisieren` (Projektgedächtnis-Regel).

---

## 4. Beweise

### 4.1 Designer-Struktur-Lint

Regelwerk unverändert aus `dev\p13probe` übernommen (nur serielle Zuweisungen und
Aufrufe, jede Anweisung mit `;`, keine offene Klammer am Zeilenende, keine
Schlüsselwörter, keine Lambdas, Kommentare nur im VS-Muster,
`SuspendLayout`/`ResumeLayout`-Klammer, Felder erst nach `#endregion`).

| Datei | Ergebnis |
|---|---|
| `WizardParent.designer.cs` | **OK** — 96 Anweisungen, 39 VS-Kommentare, 12 Felder |
| `Wizard_Komponenten.designer.cs` | **OK** — 158 Anweisungen, 78 VS-Kommentare, 25 Felder |
| `ProjektAuswahl.Designer.cs` | **OK** — 40 / 24 / 7 |
| `AktionsKarte.Designer.cs` | **OK** — 40 / 12 / 3 |
| `Form_ProjektAuswahl.Designer.cs` | **OK** — 28 / 12 / 3 |
| `Form_Start.Designer.cs` | **OK** — 874 / 369 / 122 |

### 4.2 Prüfstand `dev\p45probe` — 115 Prüfungen, alle grün

Lauf: `dev\p45probe\lauf_final.txt`.

* **B — Rahmen:** Titel „Projektassistent" (de) / „Project assistant" (en);
  `Weiter ▶`/`◀ Zurück`/`Abbrechen` bzw. `Next ▶`/`◀ Back`/`Cancel` je Kultur;
  `ProjektAuswahl` liegt in `pnlLeft`; keine `ListBox`, kein `btnOeffnen`;
  Spalte 300 px; `btn_Help` unberührt.
  **E4:** Das Logo hat **null** Click-Abonnenten (gemessen über die
  `EventHandlerList` des Controls), trägt `Cursor = Default`, und die Methoden
  `pictureBox_App_Click` und `btnOeffnen_Click` existieren nicht mehr; im Designer
  steht keine `pictureBox_App.Click`-Zeile.
* **C — Seitenliste:** `AssistentSeiten.Seitentypen` ist bei jedem Zugriff **dieselbe
  Referenz**; 13 Seiten in unveränderter Reihenfolge; zwei Aufrufe von `Erzeugen()`
  liefern denselben Bauplan (Typ und `formtype` je Zeile), aber **eigene**
  Formularinstanzen; `MenueCtrl.cs` enthält `new WizardItemClass(` **null**-mal.
* **D — typisierte Erkennung:** 11/11 Dateien ohne `form.Name == "WizardParent"`,
  11/11 mit `WizardParent.Aktiver`; `IAssistentRahmen` ist umgesetzt; ohne Rahmen ist
  `Aktiver` null, mit Rahmen die richtige Instanz, nach dem Schließen wieder null;
  `Form_PV.getWizardPage()` und `Form_Heizkessel.getWizardPage()` finden den Rahmen —
  bei **null** Fenstern in `Application.OpenForms`.
* **E — Kacheln:** 13 `AktionsKarte`, 0 `CheckBox`; alle dreizehn Namen vorhanden
  (inkl. Brauchwasser und Pufferspeicher); Vorlage-Panel unsichtbar; 21 tote
  `Get*/Set*CheckBox` weg. Geometrie: keine Kachel ragt aus der Seitenfläche
  (831 × 744), keine überlappt eine andere, kein Kacheltitel wird abgeschnitten
  (13 Titel gemessen). Balkentext ohne Doppel-Leerzeichen, Erklärtext ohne „Wizard",
  Seitentitel-Relikt behoben.
* **E — Gleichheitsbeweis:** Für **jedes** Projekt der Datenbank gilt
  `KomponentenBestand.Bitmaske == Form_Start.status` (die Bitmaske wird dafür real
  über `Form_Start.UpdateWizardSymbole()` erzeugt). Zusätzlich: Kachelzustand 13/13
  deckungsgleich mit dem Bestand, und die elf Assistentenseiten stehen 11/11 passend
  auf `aktiv`.
* **F — E3-Pfad (Wegwerf-Datenbank):**
  Prädikat je Anlagenart (WP, Solar, PV, Stromspeicher, **Spitzenkessel**, BHKW):
  bleibt bei „an", fällt bei „aus" — sechs von sechs. Puffer bleibt unverändert immer
  aus der Liste (FR-1). `EntferneNichtAktiveZuordnungen`: alle Kacheln an → keine
  Zuordnung fällt weg; Gebäude und Stromlastgang aus → genau diese zwei Listen leer.
  Danach zweimal der **echte** Speicherweg (`Del_Projekt_Waermeerzeuger` +
  `Add_WP_Waermeerzeuger`) gegen die Kopie, mit Zählung je `ID_Type` vorher/nachher.
  Gemessen an Projekt **1009 „Heinestr 15A"** (Spitzenkessel vorhanden, keine
  Gerätedublette — siehe Falle 2 in Abschnitt 7):

  | Antwort | WP(1) | Solar(2) | PV(3) | SP(4) | **Kessel(10)** | BHKW(11) | Puffer(12) |
  |---|---|---|---|---|---|---|---|
  | **Nein** (Kachel bleibt an) | 3 → 3 | 0 → 0 | 0 → 0 | 0 → 0 | **2 → 2** | 0 → 0 | 0 → 0 |
  | **Ja** (Kessel abgewählt) | 3 → 3 | 0 → 0 | 0 → 0 | 0 → 0 | **2 → 0** | 0 → 0 | 0 → 0 |

  „Nein" ändert **keine einzige** Anlagenzeile; „Ja" entfernt genau die zwei
  Kesselzeilen und lässt alles andere stehen. Vor dem Umbau wäre auch die zweite
  Zeile `2 → 2` gewesen — das ist die geschlossene Kessel-Lücke.
* **G — Quelltext und Ressourcen:** neun `.resx` wohlgeformt (Schlüsselzahlen unten);
  `WizardParent.resx` ohne `btnOeffnen`- und `listBox_Projekte`-Schlüssel, mit
  `ucProjektAuswahl`; `en-US` ohne `tableLayoutPanel`-Leichen; `help_mapping.txt`
  unverändert; Kodierungen wie erwartet.

| `.resx` | Schlüssel |
|---|---|
| `WizardParent.resx` | 121 |
| `WizardParent.de-DE.resx` | 7 |
| `WizardParent.en-US.resx` | 7 (vorher 30, davon 8 Leichen) |
| `Wizard_Komponenten.resx` | 214 |
| `Wizard_Komponenten.de-DE.resx` | 24 |
| `Wizard_Komponenten.en-US.resx` | 24 |
| `ProjektAuswahl.resx` | 51 |

### 4.3 Nur Kopien — die Produktivdatenbank bleibt unberührt

Der Prüfstand legt **eine** Kopie unter `dev\p45probe\db\Kenndaten.accdb` an und biegt
`Properties.Settings.Default.DBPath` (ohne `Save()`) darauf um; jeder Lese- **und**
Schreibzugriff läuft danach auf die Kopie. Die Produktivdatenbank wird ausschließlich
mit `File.Copy` gelesen. Eine vorhandene `Kenndaten.laccdb` wird nur **gemeldet** und
mit einem Exklusivtest als „verwaist" oder „echter Schreiber" eingeordnet — beim Lauf
war sie verwaist (die Sitzung des Anwenders war beendet, die Datei lag noch da).

### 4.4 Regressionsnetz

| Prüfstand | Lauf gegen `build_p45` | Ergebnis |
|---|---|---|
| `p13probe` (Schnitt 1: Karten, Startmaske, ProjektAuswahl, MenueCtrl) | `dev\p13probe\lauf_p45.txt` | **ALLES GRUEN (93 Prüfungen)** |
| `h7probe` (Info-/Hilfeknöpfe) | `dev\h7probe\lauf_p45.txt` | **ALLES GRUEN** |
| `h11probe` (Sammelpaket H11) | `dev\h11probe\lauf_p45.txt` | **ALLES GRUEN** |
| `h12probe` (feldgenaue Hilfe) | `dev\h12probe\lauf_p45.txt` | **ALLES GRUEN** |

`h7probe`/`h11probe` tragen ihren Build-Ordner als Konstante; sie wurden dafür auf
`build_p45` umgestellt (Sicherung je `Probe.cs.p45bak`). `p13probe` und `h12probe`
nehmen den Ordner als Argument.

### 4.5 Build

```
MSBuild WP-Plan.sln -t:Rebuild -p:Configuration=Debug -p:Platform=x64
        -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_p45\
```
→ **0 Fehler, 5 Warnungen** — dieselben fünf wie vor dem Schnitt
(CS0108 `WErzeugerModel.ID_Projekt`, 2× CS0109 `KlimaregionStammCtrl`,
CS0108 `StromverbraucherStammCtrl.items`, CS1998 `MDIMainForm.cs:489`).
Keine neue Warnung.

---

## 5. Offene UI-Prüfpunkte (nur am Rechner des Nutzers)

1. **Im VS-Designer öffnen** (Kern-Abnahmekriterium, Konzept 6/1):
   * `Views\Wizard\WizardParent.cs`
   * `Views\Wizard\Wizard_Komponenten.cs`
   * `Views\Projekt\ProjektAuswahl.cs` (neue Eigenschaften)
   * `Views\GemeinsameBausteine\AktionsKarte.cs` (neue Eigenschaft `TitelSchrift`)
   * zur Sicherheit `Views\Hauptformular\Form_Start.cs` und
     `Views\Projekt\Form_ProjektAuswahl.cs` (unverändert, aber von `AktionsKarte`
     bzw. `ProjektAuswahl` abhängig)

   Erwartung: öffnet ohne Fehler; die dreizehn Kacheln sind einzeln anklickbar;
   `TitelSchrift` erscheint im Eigenschaftenfenster unter „Darstellung",
   `NurNamensspalte` ebenfalls, `AutomatischeVorauswahl` unter „Verhalten".
2. **Assistent „Neu" komplett durchlaufen** (deutsch und englisch): Weiter/Zurück
   korrekt beschriftet, Titel „Projektassistent", Kacheln schalten die Seiten frei.
3. **Assistent „Bearbeiten"**: linke Spalte zeigt Liste + Suche; ein Klick auf ein
   Projekt füllt die Kacheln; „Weiter" bleibt gesperrt, solange nichts gewählt ist.
4. **E3 am echten Projekt**: eine belegte Kachel abwählen → Rückfrage mit Anzahl und
   Namen, Vorbelegung Nein; „Nein" ändert nichts, „Ja" entfernt beim Speichern auch
   den **Spitzenkessel**.
5. **Dreifachvergleich** (Konzept-Prüfliste 8): Assistent, Startmasken-Kacheln und
   `FormMain` zeigen denselben Bestand.
6. **„Neues Projekt…"** im Bearbeiten-Modus: Rückfrage erscheint, sobald ein Projekt
   gewählt war.
7. **Logo im Assistenten anklicken**: nichts passiert, das Anwendungs-Icon bleibt (E4).
8. **Fensterhöhe**: Der Komponentenschritt ist höher als der Inhaltsbereich; der
   Assistent wächst beim Öffnen um rund 30 px. Auf kleinen Schirmen greift
   `FensterEinpassung` und der Bildlauf der Seite — einmal auf einem 1366×768-Schirm
   ansehen.
9. **Optik der Kacheln in DE und EN** — „Standard-Stromlastprofil" ist der längste
   Titel; er wurde gemessen (passt bei 10 pt in 218 px), sollte aber einmal
   angesehen werden.

---

## 6. Offene Punkte / bewusst nicht enthalten

| # | Punkt | Warum offen |
|---|---|---|
| P1 | `help_mapping.txt`/`HilfeKontext` für die umgebauten Masken | ausdrücklich P6; die Datei ist unverändert (Prüfstand belegt `WizardParent.btn_Help`) |
| P2 | Wiki-Angleich (D1–D10) | P6 |
| P3 | Menüpunkt „Speichern unter…" (O1 aus Schnitt 1) | Menüerweiterung, nicht Teil von P4/P5 |
| P4 | `Form_ProjektSpeichernUnter.en-US.resx` ohne `label2.Text` (O2) | fremde Maske, außerhalb des Auftragsumfangs |
| P5 | `Form_ProjektSpeichernUnter.cs:52` greift ohne Prüfung auf `Items[0]` (O3) | Altbestand |
| P6 | `Grenzfehler in GetNextUpIndex` (`index < pagecount - 1`) | Altbefund der Inventur; unverändert gelassen, weil die Schleife bei 13 Seiten trotzdem auf Index 12 endet und jede Änderung die Navigation berührt |
| P7 | Brauchwasser/Pufferspeicher ohne Assistentenseite | bewusst: siehe 3.2. Eine echte Seite wäre ein neuer Assistentenschritt (Konzeptfrage, nicht P5) |
| P8 | Zwei parallele Speicherwege (Assistent gesammelt ↔ Startmasken-Kacheln sofort) | Befund 3.3 der Inventur, außerhalb dieses Schnitts |
| P9 | `RoundedPanel` / `ChartManager.Kacheln` mit abweichender Bogensemantik (O6) | späterer Schnitt |

---

## 7. Werkzeuge (Wegwerf, unter `dev\`)

| Datei | Zweck |
|---|---|
| `dev\p45probe\p45probe.csproj`, `Probe.cs` | Prüfstand A–G |
| `dev\p45probe\rahmen_erkennung.ps1` | byte-schonende Ersetzung der elf `getWizardPage()`-Rümpfe (Latin-1, Rundprobe je Datei) |
| `dev\p45probe\resx_p45.ps1` | erzeugt die sieben `.resx` im VS-Format |
| `dev\p45_resx_vorher\` | Sicherung der sechs `.resx` vor dem Umbau |
| `dev\p45probe\db\` | Wegwerf-Kopie der Datenbank |
| `dev\h7probe\Probe.cs.p45bak`, `dev\h11probe\Probe.cs.p45bak` | Sicherung vor der Umstellung auf `build_p45` |
| `dev\p45probe\crlf.ps1` | vereinheitlicht die Zeilenenden der geschriebenen Dateien auf CRLF (byteweise, kodierungsneutral) |

**Zwei Fallen, die wirklich zugeschlagen haben:**

1. **PowerShell:** Der Komma-Operator bindet **stärker** als `+`.
   `@('x', 'a' + $NL + 'b', 'c' + $NL + 'd')` liefert stillschweigend Unsinn — die
   mehrzeiligen Rückfragetexte landeten als leere Werte in der `.resx`. Verkettete
   Zeichenketten vor dem Array bilden. (Die BOM-Regel aus Schnitt 1 gilt weiter: alle
   `.ps1` mit UTF-8-BOM speichern.)
2. **Der Speicherweg fragt modal.** Verweisen zwei Anlagenzeilen desselben Projekts
   auf dasselbe Gerät, zeigt `Add_WP_Waermeerzeuger` über
   `AnlagenEindeutigkeit.Aufnehmen` die MessageBox „Gerät bereits im Projekt" — ein
   Prüfstand bleibt daran hängen (zweimal passiert, jeweils erst am modalen Fenstertitel
   des Prozesses erkannt). `WErzeugerModel.GeraetekopieErzwingen = true` hilft **nicht**:
   Es überspringt zwar die Frage, läuft dann aber in `ProjektkopieAnlegen` und meldet
   den Fehlschlag mit **derselben** Überschrift wieder modal.
   Lösung im Prüfstand: Der Löschweg wird an einem Projekt gemessen, das **keine**
   Gerätedublette führt (die Auswahl prüft das über die Namenslisten des
   `KomponentenBestand`). Das Verhalten selbst ist Altbestand und nicht Gegenstand
   dieses Schnitts. Für Nachfolger: Wer den Speicherweg im Prüfstand fährt, sollte den
   Prozess auf `MainWindowTitle` überwachen — ein hängender Lauf sieht sonst aus wie
   ein langsamer.
