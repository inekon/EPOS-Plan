# H11 — Sammelpaket: Popup-Kurzbeschreibungen, Infoknopf-Doppelung, Aufräumen

Umsetzungsprotokoll vom **29.08.2026**. Ausgangsstand `e02bb77` (Branch `Pufferspeicher`).
Vorgängerpakete: `H1H2_Umsetzung_Protokoll.md`, `H7_InfoButtons_Protokoll.md`,
`../KI/H4H5_Umsetzung_Protokoll.md`, `../KI/H10_SemantikIndex_Protokoll.md`.
Konzeptbezug: `../../../Konzept_Hilfesystem_Wikidokumentation.md`, Entscheide **A5** und **7.6**.

Reihenfolge wie im Auftrag: 1 Popup-Kurzbeschreibungen, 2 Infoknöpfe „Berichte & Kosten",
3 Designer-Aufräumen, 4 Kleinbefunde, 5 unerreichbarer Altpfad, 6 Build und Prüfstand.

---

## 0. Kurzfassung

| # | Aufgabe | Ergebnis |
|---|---|---|
| 1 | Popup-Kurzbeschreibungen (7.6) | umgesetzt — **32 von 32** Rubrikseiten tragen einen Einleitungssatz |
| 2 | Zwei Infoknöpfe auf „Berichte & Kosten", einer tot | **Ursache gefunden und behoben**; Doppelung entschärft (Abstand 7…15 px → 63…260 px) |
| 3 | `txt_WPPrefix` / `lbl_WPPrefix` / `WordPressPrefix` | restlos entfernt, 0 Treffer außerhalb erklärender Kommentare |
| 4a | Kachel „Optimierung" | per Code ausgeblendet, Designer unberührt |
| 4b | `Form_Gebaeude2` ohne Fenstertitel | Titel per Code aus neuen MyResource-Schlüsseln (de/en) |
| 4c | vier `.cs.bak` | gelöscht |
| 4d | `ucKategorieHeader` | gelöscht (3 Dateien) |
| 4e | Antwort-Cache des Assistenten | Verfallsdatum 30 Minuten |
| 5 | `Form_Variantentest` / `_Wirtschaftlichkeit` / `_Bericht` | **konservativ behalten** — Begründung in § 5, keine Wiki-Streichung |
| 6 | Build + Prüfstand | 0 Fehler, 5 bekannte Warnungen; `h11probe` **34/34 grün**, h7/h9/h10 grün |

---

## 1. Popup-Kurzbeschreibungen (Entscheid 7.6)

### 1.1 Empirischer Vorlauf — wie viele Auszüge liefert eine Anfrage?

Der Auftrag verlangte ausdrücklich die Messung, weil der Volltext-Modus serverseitig auf
**einen** Auszug je Anfrage gedeckelt ist (Befund aus H9, `WikiWissen.cs`: die Seiten werden
deshalb einzeln geholt).

Gemessen am 29.08.2026 gegen `wiki.epos-plan.de`:

| Anfrage | Titel | gelieferte Auszüge | `continue` |
|---|---|---|---|
| `prop=extracts&exintro=1&explaintext=1&exsentences=2` | 10 | **10** | — |
| dieselbe Form | 29 | **20** | `{"excontinue":20,"continue":"||"}` |

**Der Deckel im Einleitungs-Modus liegt bei 20.** Er wird nicht über `excontinue` fortgesetzt,
sondern durch **Stückelung** vermieden: `ExtraktStapel = 20`
(`HelpCatalog.cs:637`). Bei 32 Rubrikseiten sind das **zwei Anfragen**, und keine Antwort kann
stillschweigend beschnitten sein. Die Messung samt Begründung steht als Bemerkung an der
Konstanten.

Nebenbefund derselben Messung: Die Rubrik führt **32** Unterseiten
(`action=query&list=allpages&apprefix=Programm%20Dokumentation/`).

### 1.2 Datenmodell

`HelpEntry.Beschreibung` (`HelpCatalog.cs:47`), Vorgabewert `""`.

**Abwärtskompatibilität**, wie gefordert:

* Der **eingebettete Startbestand** `Allgemein/Hilfe/help_cache.json` ist **unangetastet**
  (`git diff` leer). Er kennt das Feld nicht — beim Einlesen bleibt es leer, und das Popup
  verhält sich exakt wie vor H11.
* Eine **ältere lokale Sicherung** ohne das Feld liest sich unverändert: `System.Text.Json`
  lässt eine fehlende Eigenschaft auf dem Initialisierungswert stehen.
* Die **neue Sicherung** trägt das Feld. Nachgewiesen an
  `%APPDATA%\EPOS-Plan\help_cache.json` nach einem Prüfstandslauf: 15.104 Bytes,
  **32 von 32** Einträgen mit nichtleerer `Beschreibung`.

`MitAnker` kopiert das Feld mit (`HelpCatalog.cs`, Kopierzweig) — sonst verlöre jede
Zuordnung mit Sprungmarke ihre Kurzbeschreibung.

### 1.3 Beschaffung — nachgelagert, bester Wille

`BeschreibungenNachladenAsync` (`HelpCatalog.cs:664`) und `StapelBeschreibungenAsync`
(`:733`), Textnormalisierung `TextSaeubern` (`:797`).

Der Lauf startet **ohne `await`** (`HelpCatalog.cs:603-604`), und zwar **nach**
`IndizesAufbauen` und **nach** dem ersten Schreiben der Sicherung. Begründung im Code:
Der Katalog ist an dieser Stelle vollständig; würde hier gewartet, verzögerten zwei weitere
HTTP-Abrufe den Zeitpunkt, ab dem `IsLoaded` gilt — und damit den Moment, in dem die
Infobuttons ihr Ziel kennen (F3-Wettlauf aus H1/H2).

Wer warten muss, nimmt `BeschreibungenGeladen` (`:150`). Diese Zusage wird **immer**
eingelöst: entweder vom Nachladelauf selbst (`finally`), oder — wenn er gar nicht erst
anlief (offline, Serverfehler, leere Rubrik) — vom `finally` in `LoadAllAsync` über den
Merker `_beschreibungenGestartet`. Ohne diese zweite Stelle wartete ein Aufrufer ewig.

Weitere Feinheiten:

* Die Eintragsobjekte werden **direkt** beschrieben. `IndizesAufbauen` legt die übergebenen
  `HelpEntry`-Objekte selbst ab, nicht Kopien davon — wer hier `Beschreibung` setzt, ändert
  denselben Eintrag, den `Get` später herausgibt.
* Der Wiki-Titel wird aus `RubrikPraefix + Slug` rekonstruiert; der Eintrag führt ihn nicht mit.
* Vor `IndizesAufbauen` werden Beschreibungen aus dem bisherigen Bestand **gerettet**, damit
  sie zwischen Katalogaufbau und Ende des Nachladelaufs nicht kurzzeitig verschwinden.
* Nach erfolgreichem Nachladen wird die Sicherung **ein zweites Mal** geschrieben — nur dann
  kennt der nächste Start ohne Netz die Sätze.
* Jeder Fehler bleibt folgenlos: Beschreibungen bleiben leer, Zeitschranke 15 s je Stapel.

### 1.4 Anzeige im Popup

`Views/Help/Form_HelpPopup.cs`:

* `ShowHelp` / `ShowHelpAngeheftet` tragen einen zusätzlichen Parameter `beschreibung`
  (`:64`, `:73`). Beide Aufrufstellen liegen in `HelpExtender` (Klick und Überfahren).
* Aufbau: Kapitelzeile → **Kurzbeschreibung** → `➔ Verweis` → ggf. Esc-Zeile.
* `BeschreibungUmbrechen` (`:184`): Umbruch an Wortgrenzen auf höchstens
  `BESCHREIBUNG_ZEILEN = 2` Zeilen zu je rund `BESCHREIBUNG_ZEICHEN = 70` Zeichen; was nicht
  mehr hineinpasst, endet mit „…". Ein einzelnes überlanges Wort wird **nicht** getrennt.
* **`LinkArea`** (`:114-116`): Ist eine Beschreibung da, ist nur noch die Verweiszeile Link —
  sonst wäre die Kurzbeschreibung blau und unterstrichen. Ist keine da, wird der Linkbereich
  **ausdrücklich auf den ganzen Text zurückgestellt**. Das ist das Verhalten vor H11 (der
  Standard eines `LinkLabel` ohne gesetztes `LinkArea`); einmal gesetzt, wandert der Bereich
  beim nächsten Textwechsel nicht von selbst mit. Damit gilt die Zusage „ohne Beschreibung
  exakt heutiges Verhalten" wörtlich und in beide Richtungen.
* Die **Randklemmung** aus dem Vortagespaket (Bildschirmarbeitsbereich, `PerformLayout` vor
  der Messung) bleibt unverändert wirksam und rechnet nun mit der größeren Höhe.

### 1.5 Messung

Aus `dev/h11probe/lauf6.txt` (Live gegen `wiki.epos-plan.de`):

```
OK   Katalog fuehrt 32 Rubrikseiten                      -> 32 Seiten, 268 ms
OK   Nachladelauf der Beschreibungen meldet sich fertig  -> nach 641 ms gesamt
OK   mindestens 25 Seiten mit Kurzbeschreibung           -> 32 von 32
OK   keine Beschreibung ueber 2 Zeilen                   -> 0 Faelle
OK   keine Zeile ueber 71 Zeichen                        -> 0 Faelle
     gekappt: 32, ohne Beschreibung: 0
```

Der Katalog stand nach 268 ms; die Beschreibungen kamen 373 ms später nach — der Nachlauf
verzögert den Programmstart also nicht.

Alle 32 Sätze werden gekappt: Zwei Wiki-Sätze sind regelmäßig länger als 140 Zeichen. Das ist
gewollt — das Popup ist eine Kurzinfo, die vollständige Seite steht einen Klick entfernt.

Randfälle der Kappung (unabhängig vom Wiki, alle grün): leere Eingabe → leer; nur Leerraum →
leer; kurzer Satz bleibt einzeilig; ein 200 Zeichen langes Wort wird **nicht** getrennt;
60 Wörter → 2 Zeilen mit Auslassungszeichen.

---

## 2. Zwei Infoknöpfe auf „Berichte & Kosten" — Ursache und Behebung

### 2.1 Der Befund des Nutzers

Auf **Berichte & Kosten → Bericht** stehen zwei blaue i-Knöpfe fast übereinander; einer
reagiert nicht.

Nachgemessen (Reiterbreite 1265, Seitenbreite 1057):

| Knopf | Rechteck im Reiter | Elternelement |
|---|---|---|
| `UcBerichteKosten.btn_Help` | x 1229…1253, **y 3…27**, 24×24 | `lblKopf` (Kopfzeile, Dock Top, 30 hoch) |
| `UcBericht.btn_Help` | x 1225…1253, **y 34…62**, 28×28 | `UcBericht` |

Gleiche Spalte auf **4 px** genau, senkrechte Luft **7 px**. Bei den beiden anderen Seiten
15 px. Der obere Knopf war `Enabled = false`.

### 2.2 Die Ursache — Prüfung in der beauftragten Reihenfolge

**(a) Registrierungs-Präfix der träge erzeugten Seiten — nicht die Ursache, aber geprüft.**

`UcBerichteKosten` erzeugt seine vier Seiten erst beim Navigieren
(`UcBerichteKosten.cs`, Eigenschaften `Uebersicht`/`Kosten`/`Wirtschaftlichkeit`/`Bericht`)
und hängt sie in `pnlInhalt` ein. Der `Name` wird **nicht** beim Erzeugen gesetzt, sondern
jeweils in der eigenen `InitializeComponent` — er stimmt also und lautet exakt so, wie die
Zuordnung ihn führt (`UcBkUebersicht`, `UcBkKosten`, `UcWirtschaftlichkeit`, `UcBericht`).

Der `HilfeAutomatik`-Weg trägt ebenfalls: `BehaelterUeberwachen` hängt an **jeden** Behälter
im Baum einen `ControlAdded`-Haken; `KindHinzugefuegt` merkt den Behälter vor,
`NachgeladenesErfassen` sucht die Wurzel und ruft `BaumErneuern` → `RegisterBaum` über den
**ganzen** Baum. `UnterPraefixeAnwenden` wendet danach die Zeilen jedes eingebetteten
`Form`/`UserControl` unter dessen **eigenem** Namen an. Bis hierher ist alles richtig.

**(b) Verdeckung/Klickfläche — nicht die Ursache.** Der Knopf war nicht verdeckt, sondern
abgeschaltet (`Enabled = false`, `Cursor = Default`).

**(c) Katalogauflösung — nicht die Ursache.** Beide Zielseiten („Berichte und Kosten",
„Bericht") existieren im Wiki; `h11probe` löst alle 99 Zuordnungen gegen den Livekatalog auf.

**Die Ursache lag in der Steuerelementsuche.**
`HelpExtender.FindControlByNameDeep` (`HelpCatalog.cs:1632`) suchte **ohne Grenze** über alle
Ebenen — auch in eingebettete Masken hinein. Entscheidend ist die Reihenfolge der
`Controls`-Sammlung des Behälters (`UcBerichteKosten.InitializeComponent`):

```csharp
this.Controls.Add(this.pnlInhalt);   // Index 0  <-- enthaelt die Unterseite
this.Controls.Add(this.lblKopf);     // Index 1  <-- enthaelt den EIGENEN btn_Help
this.Controls.Add(this.lvNav);       // Index 2
```

Damit lief die Auflösung der Zeile `UcBerichteKosten.btn_Help = Berichte und Kosten` so:

1. Tiefensuche ab `UcBerichteKosten`, erstes Kind `pnlInhalt`,
2. hinein in die eingeblendete Unterseite (`UcBericht` &c.),
3. **Treffer auf deren `btn_Help`** — `lblKopf` wurde nie erreicht.
4. `SetHelpKey(Seitenknopf, "Berichte und Kosten")`.
5. Danach `UnterPraefixeAnwenden` → Zeile `UcBericht.btn_Help = Bericht` trifft **denselben**
   Knopf und überschreibt den Schlüssel. Die Seite behielt also ihr richtiges Ziel.
6. Der **eigene** Knopf des Behälters bekam nie einen Schlüssel und wurde von
   `InfobuttonsOhneZuordnungAbschalten` abgeschaltet.

Es gab **keinen Weg zurück**: `SteuerelementWiederEinschalten` wird nur für Steuerelemente
aus der Liste `registrierte` gerufen — der verwaiste Knopf landet dort nie und blieb für die
gesamte Programmlaufzeit grau.

Der Fehler widersprach einer bereits im Bestand niedergeschriebenen Zusage:
`InfoKnopf.Vorhandenen` zieht die Grenze seit H7 ausdrücklich („an einem eingebetteten `Form`
oder `UserControl` bricht die Suche ab, weil dessen Infoknopf IHM gehoert"), und
`UnterPraefixeAnwenden` zieht sie beim Präfix. Nur `FindControlByNameDeep` zog sie nicht.

### 2.3 Die Behebung

`FindControlByNameDeep` (`HelpCatalog.cs:1632`) bekommt dieselbe Grenze:

```csharp
if (child is Form || child is UserControl)
{
    // Getroffen werden darf es, betreten nicht.
    if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase)) return child;
    continue;
}
```

**Getroffen, nicht betreten** — damit bleibt ein Punktpfad über eine eingebettete Maske
(`Behaelter.UcSeite.btn_Help`) weiterhin möglich: `FindControlRecursive` findet die
Zwischenstufe und setzt die Suche in ihr als neuer Wurzel fort. In `help_mapping.txt` gibt es
heute **keinen** mehrsegmentigen Pfad (geprüft: 0 von 99), der Weg bleibt aber offen.

**Der Fix wirkt für alle vier Seiten** — und darüber hinaus: Dieselbe Falle drohte überall,
wo ein Formular ein `Form`/`UserControl` einbettet, das seinerseits einen `btn_Help` trägt.
Betroffen wären insbesondere `WizardParent` (bettet `Wizard_WPItem` mit `TopLevel = false`
in `pnlContent` ein) und `Form_Start` (bettet `UcBerichteKosten` in `tabPage6` ein, trägt
selbst `btn_Help` bei 23/130).

### 2.4 Rot-Nachweis und Grün-Nachweis

`dev/h11probe` stellt den Altzustand mit einer wortgleichen Kopie der grenzenlosen Suche nach
(`AlteSucheOhneGrenze`) und misst danach den Istzustand. Für **alle drei im Prüfstand
baubaren Seiten**:

```
Altzustand (Suche ohne Grenze): greift den Knopf der SEITE
                                -> Behaelterknopf bliebe ohne Schluessel
OK   …: Altfehler war reproduzierbar    -> Rot-Nachweis erbracht
OK   …: Behaelterknopf aktiv            -> Enabled=True, Cursor=[Cursor: Hand]
OK   …: Seitenknopf aktiv               -> Enabled=True, Cursor=[Cursor: Hand]
```

`UcWirtschaftlichkeit` ist im Prüfstand nicht baubar
(`PlatformNotSupportedException: System.Data.OleDb is not supported on this platform` — dem
Prüfprozess fehlt der OLE-DB-Anbieter). Das ist eine Grenze des Messwerkzeugs, kein Befund
am Code; die Seite läuft über denselben Weg wie die drei gemessenen.

**Regress:** `h7probe` (72 Masken, 73 Knöpfe) bleibt vollständig grün —
57 gefundene Knöpfe, **57 aktiv**, kein Knopf verdeckt ein Bedienelement, Anker 57/57.
Die Grenze hat also keine bestehende Zuordnung gebrochen.

### 2.5 Die Doppelung entschärfen

**Richtlinie des Auftrags:** Der Container-Knopf bleibt als Reiter-Hilfe oben rechts in der
Kopfzeile; die Seiten-Knöpfe rücken eindeutig zu ihrem Inhalt, „sodass nie zwei Knöpfe
optisch als Paar erscheinen".

**Warum kein fester Platz genügt.** Gemessen wurde die Belegung des senkrechten Streifens
des Knopfes bei echter Laufzeitgröße (Seite 1057×530):

| Seite | frei im Streifen x 1017…1045 |
|---|---|
| `UcBkKosten` | y 30…152 (darunter die beiden großen Listen bis y 500) |
| `UcBkUebersicht` | y 197…269 (davor Kontrollkästchen, Eingabefeld, drei Schaltflächen) |
| `UcBericht` | ab y 195 (davor die Bausteinliste y 32…195) |

**Eine gemeinsame freie Zeile gibt es nicht.** Ein fest gesetzter Abstand von oben träfe auf
jeder Seite etwas anderes; die linke Seitenkante scheidet ebenso aus (dort stehen überall die
Seitenüberschriften und die Variantenliste).

**Gewählter Weg:** Der **Behälter** rückt den Knopf der eingebetteten Seite ab —
`UcBerichteKosten.SeitenknopfAbruecken` (`:405`), aufgerufen aus `BaueSeiteAuf` (`:330`) und
aus dem `Layout`-Ereignis der Inhaltsfläche (`:155`, über `SeitenknoepfeEinmessen` `:400`).

Begründungen, alle im Code niedergeschrieben:

* **Warum der Behälter und nicht die Seiten.** `UcWirtschaftlichkeit` und `UcBericht` laufen
  auch außerhalb dieses Reiters in einer eigenen Dialoghülle. Dort gibt es keine Kopfzeile und
  keine Doppelung — dort ist der Regelplatz rechts oben richtig. Die Enge entsteht erst durch
  die Einbettung, also behebt sie der Einbettende. Die vier Seiten bleiben unverändert.
* **Warum gesucht statt gesetzt.** Dieselbe Regel wie `InfoKnopf.FreiesOben`, aber auf der
  **echten** Laufzeitgröße statt der Entwurfsgröße: erster freier Platz ab
  `SEITENKNOPF_OBEN = 60` (`:351`) abwärts, in zwei Runden (streng: jedes Blatt und jedes
  Bedienelement; nachgiebig: nur Bedienelemente — Beschriftungen dürfen im Notfall überlagert
  werden). Ein Bedienelement wird auch dann als Hindernis gezählt, wenn es Kinder führt: Eine
  Liste trägt ihre Bildlaufleiste als Kind, ihre obere rechte Ecke ist gerade **nicht** frei.
* **Warum `Layout` und nicht `SizeChanged`.** Wenn `Layout` feuert, hat die Layout-Maschine
  die gedockte Seite bereits auf ihre neue Größe gebracht (`Control.OnLayout` ruft erst die
  `LayoutEngine`, dann die Ereignisliste). Bei `SizeChanged` stünde die Seite noch auf der
  alten. Eine Schleife entsteht nicht: Das Versetzen des Knopfes löst ein Layout der **Seite**
  aus, nicht der Inhaltsfläche, und ein bereits richtig sitzender Knopf wird gar nicht erst
  angefasst.

**Ergebnis (gemessen, Reiter 1265×560):**

| Seite | Seitenknopf vorher | Seitenknopf nachher | Abstand zum Behälterknopf |
|---|---|---|---|
| `UcBkUebersicht` | y 42…70 | **y 227…255** | 15 px → **200 px** |
| `UcBkKosten` | y 42…70 | **y 90…118** | 15 px → **63 px** |
| `UcBericht` | y 34…62 | **y 287…315** | 7 px → **260 px** |

Der Behälterknopf steht unverändert bei y 3…27 in der Kopfzeile. Zwischen beiden liegt in
jedem Fall die Kante der Kopfzeile (y 30).

**Nebenwirkung, ausdrücklich zum Guten:** Vorher deckte der Seitenknopf auf zwei Seiten ein
Bedienelement zu — `UcBkUebersicht` das Kontrollkästchen `chkNurStaemme` (918/10, 129×22),
`UcBkKosten` die Schaltfläche bei 887/8, 160×22. Nach dem Abrücken verdeckt keiner der drei
gemessenen Knöpfe noch ein Bedienelement (im Prüfstand **tief** geprüft, nicht nur auf der
obersten Ebene — bei den beiden Tabellenseiten ist das einzige direkte Kind ein füllendes
Tabellenfeld, eine flache Prüfung sähe dort grundsätzlich nichts).

### 2.6 Dasselbe Muster anderswo

Geprüft wurden alle Stellen, an denen ein Behälter **und** eine eingebettete Seite je einen
`btn_Help` tragen:

| Stelle | Lage | Bewertung |
|---|---|---|
| `UcBerichteKosten` × 4 Seiten | siehe oben | behoben |
| `WizardParent` + `Wizard_WPItem` | Behälterknopf in `pnlLeft` bei **14/162** (linke Spalte, unten), Seitenknopf rechts oben der eingebetteten Maske | **kein Paar** — mehrere hundert Bildpunkte auseinander, kein Handlungsbedarf. Der Suchfehler aus § 2.2 hätte hier ebenfalls zuschlagen können und ist mit behoben. |
| `Form_Start.btn_Help` (23/130) neben `btn_Help_Energieerzeuger` / `_Simulation` / `_Kurzanleitung` / `_Waermebedarf` / `_Strombedarf` | verschiedene Reiter, verschiedene Namen | keine Schlüsselkollision, keine Nachbarschaft |
| `Form_Kosten` + `ucFuelSettings` | `ucFuelSettings` trägt keinen `btn_Help` | kein Paar |
| `Form_Bericht` / `Form_Wirtschaftlichkeit` (Dialoghüllen) | tragen selbst **keinen** `btn_Help` | kein Paar |

---

## 3. Designer-Aufräumen: `txt_WPPrefix` / `lbl_WPPrefix` / `WordPressPrefix`

Ausdrücklich beauftragte Ausnahme von der Designer-Regel, chirurgisch ausgeführt.

**Vorher** — `Views/Admin/Form_AdminSettings.Designer.cs`, 514 Zeilen, 18 Fundstellen in vier
Blöcken: Erzeugung (Z. 30-31), `panel_Internet.Controls.Add` (Z. 119-120),
`InitializeComponent`-Block (Z. 179-194), Felddeklarationen (Z. 493-494).

**Nachher** — 491 Zeilen, **0 Fundstellen**.

Geprüft und für unkritisch befunden: Die Datei nutzt **kein** `ApplyResources` und **keinen**
`ComponentResourceManager` (0 Treffer); sie arbeitet durchgehend mit direkten
Property-Zuweisungen, die deutschen Klartexte stehen im Code. In **keiner** der drei
Sprachdateien gibt es einen Eintrag zu diesen Steuerelementen. Es war also nichts nachzuziehen.

Kodierung vor der Änderung geprüft: **UTF-8 mit BOM**, CRLF — beides erhalten.

Weiter entfernt:

* `Views/Admin/Form_AdminSettings.cs`: die beiden `Visible = false`-Zeilen samt Begründung
  („das Entfernen der Steuerelemente ist eine spätere Aufgabe im WinForms-Designer") —
  ersetzt durch eine Zeile, die den jetzigen Zustand festhält.
* `Properties/Settings.settings`: `<Setting Name="WordPressPrefix" …>`.
* `Properties/Settings.Designer.cs`: die Eigenschaft samt
  `DefaultSettingValueAttribute("pages")`.
* `app.config`: `<setting name="WordPressPrefix" …>`.

**Beweis.** `grep -rn "WPPrefix\|WordPressPrefix"` über `*.cs *.resx *.settings *.config
*.txt *.csproj` liefert nur noch **vier erklärende Kommentarzeilen** — drei in
`Form_AdminSettings.cs` (Z. 147, 221-222, 242), eine in `HelpCatalog.cs:463` („wird seit H1
nirgends mehr gelesen, Entscheid 7.3"). Keine Codestelle, keine Konfiguration, keine
Ressource. Build grün.

---

## 4. Kleinbefunde

### 4a — Kachel „Optimierung" auf dem Reiter Simulation

Der Handler `pBox_Optimierung_Click` war leer; alle drei Steuerelemente der Kachel trugen
`Cursors.Hand` und standen gleichrangig neben der Kachel „Simulation", die einen echten
Dialog öffnet. Ein Bedienelement, das bedienbar aussieht und nichts tut, ist schlechter als
keines.

Neu: `OptimierungskachelVerbergen()` (`Views/Hauptformular/Form_Start.cs:1610`), aufgerufen
im Konstruktor (`:64`). Setzt `Visible = false` und `Cursor = Cursors.Default` auf:

| Control | Rolle |
|---|---|
| `pBox_Optimierung` | Bildkachel |
| `label_pBox_Optimierung` | Titelzeile „Optimierung" / „Optimisation" |
| **`label2_pBox_Optinierung`** | Untertitel „Automatische Systemoptimierung" |

**Der Designer bleibt unberührt** — die Startmaske führt die Koordinaten ihrer Kacheln je
Sprache in eigenen `.resx`-Dateien; ein Entfernen müsste in allen nachgezogen werden. Das
Ausblenden ist umkehrbar: Wird die Optimierung umgesetzt, fällt nur dieser eine Aufruf weg.

**Offener Nebenbefund (bewusst nicht behoben):** Die Untertitelzeile heißt im Designer
`label2_pBox_Optinierung` — mit „n" statt „m". Der Tippfehler steht so auch in
`Form_Start.resx` und `Form_Start.en-US.resx`. Eine Berichtigung zöge eine Designer- und drei
Ressourcenänderungen nach sich; er ist im Code dokumentiert und der Grund, warum die drei
Namen einzeln stehen statt über ein Namensmuster gesucht zu werden.

### 4b — Fenstertitel für `Form_Gebaeude2`

Der Dialog lief **ohne Titel**: Weder der Designer noch eine der drei `.resx` setzt
`$this.Text` (geprüft — auch `Form_Gebaeude2.resx` führt nur `ClientSize`, `Font`, `Margin`,
`AutoScaleDimensions`, `Language`, `Localizable`).

Zweck aus `SetControls()` abgeleitet: Raumsolltemperaturen (Tag, Nachtabsenkung, Wochenende,
Ferien, Maximum), Wärmebrückenverlustkoeffizienten samt Anschlussmaßen, vier Ferienzeiträume
(Winter/Ostern/Sommer/Herbst) und die Luftwechselrate.

Neue MyResource-Schlüssel:

| Schlüssel | de | en |
|---|---|---|
| `GEB2_TITEL` | „Gebäudedaten – Raumtemperaturen, Wärmebrücken und Ferienzeiten" | „Building data – room temperatures, thermal bridges and holiday periods" |

Gesetzt im Konstruktor (`this.Text = MyResource.Resource.GEB2_TITEL;`), nicht im Designer —
so kommt der Titel in beiden Sprachen aus einer Quelle statt in jeder Satellitendatei einzeln
gepflegt werden zu müssen (Drei-Schichten-Regel, Anzeigeschicht).

**Kodierung.** `Form_Gebaeude2.cs` ist die einzige angefasste Datei in **CP1252 ohne BOM**
(die Modellbezeichner tragen `ß` und `ä`: `…Anschluß_Außenwand_Kellerdecke`). Vorgehen:
Hinweg `iconv -f CP1252 -t UTF-8` mit vorheriger **Rundprobe** (`cmp` gegen den Rückweg,
byteidentisch), Bearbeitung, Rückweg `iconv -f UTF-8 -t CP1252`. Nachweis danach:
`file` meldet wieder `ISO-8859 text, with CRLF line terminators`, die Umlaut-Rückleseprobe
zeigt `Anschluß`/`Außenwand` intakt, und `diff --strip-trailing-cr` gegen das Original weist
**genau den beabsichtigten Einschub** und sonst nichts aus.

### 4c — `.cs.bak`

Gelöscht (alle vier waren getrackt, keine hatte einen `.csproj`-Eintrag, keine wurde
übersetzt — der SDK-Glob greift `**/*.cs`):

```
Views/BHKW/Form_BHKWAdmin.cs.bak
Views/BHKW/Form_BHKWEing.cs.bak
Views/BHKW/Form_DBBHKW.cs.bak
Views/Heizkessel/Form_Heizkessel.cs.bak
```

**Nicht angefasst** (außerhalb des Auftrags, für ein Folgepaket vorgemerkt): fünf weitere
getrackte `.bak` — `Controller/BHKWCtrl.cs.bak`, `Controller/BHKWStammCtrl.cs.bak`,
`Controller/ProjektDuplizierenCtrl.bak`, `Model/BHKWModel.cs.bak`,
`Views/Hauptformular/Form_Start.cs.bak` (letztere hat als einzige einen
`<None Remove=…>`-Eintrag in der `.csproj`, der dann mit weg müsste).

### 4d — `ucKategorieHeader`

Referenzen erneut geprüft: **5 Treffer im ganzen Repo, alle fünf Selbstreferenzen**
(Klassendeklaration, Konstruktor, `partial class`, Designer-Kommentar, `this.Name`). Kein
`new ucKategorieHeader(...)`, kein Treffer in `.resx`, in `help_mapping.txt` oder in
`HilfeKontext.cs`. Der einzige Konstruktor hat einen Pflichtparameter — das Control kann
weder vom Designer noch per `Activator.CreateInstance()` ohne Argument entstehen.

Gelöscht: `Views/Kosten/ucKategorieHeader.cs`, `.Designer.cs`, `.resx`.

### 4e — Verfallsdatum für den Antwort-Cache des Assistenten

`Allgemein/KI/KiChatService.cs` (UTF-8 **ohne** BOM — beibehalten).

Vorher: `private static readonly Dictionary<string, KiAntwort> _cache` — prozessweit,
**ohne** Verfallsdatum, ohne Größenbegrenzung, ohne `Remove`. Eine einmal gegebene Antwort
blieb bis zum Programmende stehen.

Nachher:

* `CACHE_GUELTIG = TimeSpan.FromMinutes(30)` (`:313`).
* Eigene kleine Hüllklasse `CacheEintrag { DateTime Angelegt; KiAntwort Antwort; }` (`:326`).
* `Dictionary<string, CacheEintrag> _cache` (`:334`).
* Lesen (`:518`) jetzt mit **einem** Zugriff (`TryGetValue` statt `ContainsKey` + Indexer) und
  mit **Entfernen** des abgelaufenen Eintrags — sonst wüchse das Verzeichnis weiter.
* Schreiben (`:569`) mit `Angelegt = DateTime.UtcNow`.

**Warum kein Zeitstempel in `KiAntwort`** (im Code begründet): Der Cache-Treffer wird beim
Herausgeben in eine **frische** `KiAntwort` kopiert (`AusCache = true`). Ein Stempel im
Ergebnistyp würde dabei auf „jetzt" zurückgesetzt und der Eintrag liefe nie ab.

**Warum 30 Minuten:** Der Zweck des Caches ist die wiederholte Frage innerhalb einer
Arbeitssitzung — jemand blättert zurück, fragt anders herum, öffnet den Chat neu. Dieses
Fenster ist Minuten, nicht Stunden. Eine halbe Stunde deckt es ab und ist kurz genug, dass ein
zwischenzeitlicher Datenwechsel den Anwender nicht mit einer veralteten Antwort sitzen lässt.

Die beiden Stellen, die bisher „der Cache kennt kein Verfallsdatum" als Begründung dafür
anführten, dass **werkzeugführende** Anfragen den Cache umgehen (`:199` und `:1049`), wurden
nachgezogen: Das Verfallsdatum begrenzt das Problem, hebt es nicht auf — Projektdaten ändern
sich in Sekunden, nicht in Minuten. Diese Anfragen gehen weiterhin **nie** über den Cache.

**Offen geblieben (Befund, nicht behoben):** `_cache` ist weiterhin ein einfaches `Dictionary`
ohne Sperre und kein `ConcurrentDictionary`, obwohl der Zugriff aus einer `async`-Methode
erfolgt. Das neue `Remove` im Lesepfad macht auch das Lesen strukturell schreibend und erhöht
dieses Risiko leicht. Eigener Befund für ein Folgepaket.

---

## 5. `Form_Variantentest` / `Form_Wirtschaftlichkeit` / `Form_Bericht` — behalten

### 5.1 Referenzlage, vollständig erhoben

**Dateien.** `Form_Variantentest` drei Dateien (`.cs`, `.Designer.cs`, `.resx`; die `.resx` ist
eine leere VS-Vorlage, der Designer nutzt keinen `ComponentResourceManager`);
`Form_Wirtschaftlichkeit` und `Form_Bericht` je **eine** Datei ohne Designer und ohne `.resx`.
Alle über den SDK-Standard-Glob erfasst — ein Löschen bräuchte keine `.csproj`-Pflege.

**Instanziierungen — genau drei im ganzen Repo:**

| Fundstelle | Code |
|---|---|
| `Views/Hauptformular/Form_Start.cs:2083` | `new Form_Variantentest(m_ID_Projekt).ShowDialog();` |
| `Views/Varianten/Form_Variantentest.cs:356` | `using (Form_Wirtschaftlichkeit dlg = new Form_Wirtschaftlichkeit(stamm.Id))` |
| `Views/Varianten/Form_Variantentest.cs:368` | `using (Form_Bericht dlg = new Form_Bericht(stamm.Id, stamm.Name))` |

Kein `typeof`, kein `as`, kein `is`, kein Menüeintrag (der Menüpunkt „Projekte › Varianten und
Bericht…" in `MDIMainForm.cs` führt auf `Program.startfrm?.ZeigeBerichteKosten(...)`, also auf
den **neuen** Weg). In `help_mapping.txt` steht nur `Form_Variantentest.btn_Help = Varianten`
(Z. 157); `Form_Bericht`/`Form_Wirtschaftlichkeit` stehen dort **nicht** — für sie greifen
bereits die UserControl-Präfixe.

**`EntferneAltknopf`** (`Form_Start.cs:2155`) entfernt `btn_Kosten` und `btn_Varianten` beim
ersten Aufbau der Reiterseite (`BaueBerichteKostenSeite`, `:2113/2114`). `btn_Varianten` liegt
auf `tabPage6` und wird genau in dem Moment entfernt und entsorgt, in dem dieser Reiter zum
ersten Mal betreten wird — er ist damit praktisch nie anklickbar.

### 5.2 Bewertung — und warum konservativ entschieden wurde

`Form_Wirtschaftlichkeit` und `Form_Bericht` sind **eindeutig** unerreichbar, solange
`Form_Variantentest` fällt; sie hängen ausschließlich an dessen zwei Aufrufen.

`Form_Variantentest` selbst ist **de facto, aber nicht de jure** unerreichbar. Vier Punkte
sprechen gegen ein Löschen in diesem Paket:

1. **Die Verdrahtung lebt.** `Form_Start.Designer.cs:1210` hängt weiterhin
   `btn_Varianten.Click += btn_Varianten_Click`. Dass der Knopf nie klickbar wird, ist eine
   **Laufzeit**-Garantie (Reihenfolge von Reiteraufbau und Sichtbarwerden), keine
   Compilezeit-Garantie. Wer den Reiteraufbau umbaut, legt ihn versehentlich wieder frei.
2. **Funktionale Amputation.** `Form_Variantentest` enthält Bedienung, die in
   `UcBerichteKosten` **nicht** abgebildet ist: Variantenanlage und -löschung über
   `VariantenCtrl`, den Simulationslauf mit Sammelmeldung (`btnSimulieren_Click`) und den
   Altbericht `btnVergleich_Click`. Mit dem Formular würde
   `Views/Varianten/ProjektvergleichBericht.cs` (46.849 Bytes) referenzlos — das ist eine
   deutlich größere Amputation als „ein toter Dialog".
3. **Es ist eine bewusste Entscheidung, kein Versehen.** An zwei Stellen ausdrücklich als
   Rückfallnetz erklärt: `Form_Start.cs` im Handler selbst („Handler und Formular bleiben
   stehen, falls der Dialog wieder gebraucht wird") und
   `Allgemein/Reporting/UMSETZUNGSSTAND.md:241` („der alte Dialogweg (`Form_Variantentest`)
   bleibt als Rückfallnetz stehen, ist aber nicht mehr verdrahtet").
4. Die Datei wurde zuletzt am **27.08.2026** angefasst — nicht verwaist liegengelassen.

Der Auftrag sagt: *„Im Zweifel konservativ."* Es besteht Zweifel. **Alle drei Formulare
bleiben.**

### 5.3 Folge für das Wiki

**Es gibt keine Streichliste.** Die Zuordnungszeile `Form_Variantentest.btn_Help = Varianten`
bleibt in `help_mapping.txt` (99 Zeilen unverändert), und die Rubrikseite **„Dialog
Projektvarianten" bleibt bestehen**. Die im Auftrag vorsorglich angekündigte Streichung
entfällt.

### 5.4 Rezept, falls später doch entfernt wird

In dieser Reihenfolge, und erst nachdem geklärt ist, wo Variantenanlage, Simulationsstart und
`ProjektvergleichBericht` künftig hängen:

1. `Form_Start.cs` Handler `btn_Varianten_Click` (`:2077-2083`), die Designer-Verdrahtung
   `Form_Start.Designer.cs:1210`, das Feld `btn_Varianten` und seine Ressourceneinträge
   (`Form_Start.resx:3851-3880`, `Form_Start.en-US.resx:623`); danach ist `EntferneAltknopf`
   nur noch für `btn_Kosten` da.
2. `Views/Varianten/Form_Variantentest.*` (drei Dateien).
3. `Views/Bericht/Form_Bericht.cs`, `Views/Wirtschaftlichkeit/Form_Wirtschaftlichkeit.cs`.
4. Mitzupflegen:
   * `help_mapping.txt:157` streichen → **dann** auch die Wiki-Rubrikseite „Dialog
     Projektvarianten", und die Sollzahl in `h7probe`/`h11probe` von 99 auf 98 setzen.
   * `Allgemein/KI/HilfeKontext.cs:103/224/239` — die drei Einträge in `BEREICH_JE_TYP`
     (reiner Laufzeit-Namensabgleich, kein Compilerverweis). **Achtung:** `UcWirtschaftlichkeit`
     fehlt in dieser Tabelle; Zeile 239 muss durch
     `{ "UcWirtschaftlichkeit", B_WIRTSCHAFT }` **ersetzt** werden, sonst verliert der
     Wirtschaftlichkeitsbereich seine Kontextzuordnung.
   * Sieben `<see cref="…"/>`-Verweise in `UcBericht.cs` (17, 19, 61),
     `UcWirtschaftlichkeit.cs` (24, 26, 76) und `UcBkUebersicht.cs` (18) — sonst CS1574.
   * Entscheidung über `Views/Varianten/ProjektvergleichBericht.cs`.
   * Der Dialogzweig der UserControls (`AlsDialog`, Ereignis `SchliessenAngefordert`) wird
     dann toter Code. **Die UserControls selbst bleiben** — sie sind über `UcBerichteKosten`
     klar produktiv.

---

## 6. Build und Prüfstand

### 6.1 Build

```
MSBuild WP-Plan.sln -p:Configuration=Debug -p:Platform=x64
        -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_h11\
```

**0 Fehler.** Warnungen: **5** — unverändert dieselben wie vor dem Paket:

| Warnung | Fundstelle |
|---|---|
| CS0108 | `Model/WErzeugerModel.cs(6,20)` |
| CS0108 | `Controller/StromverbraucherStammCtrl.cs(25,44)` |
| CS0109 | `Controller/KlimaregionStammCtrl.cs(22,24)` |
| CS0109 | `Controller/KlimaregionStammCtrl.cs(23,48)` |
| CS1998 | `MDIMainForm.cs(489,28)` |

**Durch die Löschungen ist keine Warnung weggefallen** — weder die vier `.cs.bak` (werden
nicht übersetzt) noch `ucKategorieHeader` (warnungsfrei) trugen eine bei.

`MyResource/Resource.Designer.cs` wurde von Visual Studio selbst um `GEB2_TITEL` ergänzt
(alphabetisch eingeordnet); von Hand wurde dort **nichts** eingefügt, entsprechend kein
CS0102.

### 6.2 `dev/h11probe` — 34 Prüfungen, alle grün

Aufbau: A) Kurzbeschreibungen live, B) die vier Seiten von „Berichte & Kosten" mit
Rot-Nachstellung des Altzustands, C) Lage beider Knöpfe, D) Zuordnungsregress.
Protokoll: `dev/h11probe/lauf6.txt`, Exitcode **0**, `ALLES GRUEN`, 0 `FEHL`.

| Prüfung | Zahl |
|---|---|
| Rubrikseiten im Katalog | 32 / 32 |
| Seiten mit Kurzbeschreibung | **32 / 32** (gefordert ≥ 25) |
| Beschreibungen über 2 Zeilen | 0 |
| Zeilen über 71 Zeichen | 0 |
| Zeit bis Katalog / bis Beschreibungen | 268 ms / 641 ms |
| Randfälle der Kappung | 5 / 5 |
| Seiten mit Rot-Nachweis des Altfehlers | 3 / 3 baubare |
| Behälter- und Seitenknöpfe aktiv | 6 / 6 |
| „kein optisches Paar" | 3 / 3 |
| „verdeckt kein Bedienelement" | 3 / 3 |
| Zuordnungszeilen | 99 / 99 aufgelöst, 32 verschiedene Zielseiten |

`UcWirtschaftlichkeit` wird übersprungen (kein OLE-DB-Anbieter im Prüfprozess) und ist damit
**nicht** maschinell belegt — der einzige offene Punkt aus B/C, siehe § 7.

### 6.3 Regress der Vorgängerpakete

Die Bauordner `dev/build_h7`, `dev/build_h9`, `dev/build_h10` wurden mit den frischen
Dateien aus `dev/build_h11` überschrieben; die Prüfstände laden ihre Assembly zur Laufzeit
über `AssemblyLoadContext.Default.Resolving` aus diesen Ordnern und messen damit den **neuen**
Stand.

| Prüfstand | Exitcode | OK | FEHL | Ergebnis |
|---|---|---|---|---|
| `h7probe` (Zuordnung, 72 Masken, 73 Knöpfe) | 0 | 9 + Sammelzeilen | 0 | `ALLES GRUEN` |
| `h9probe` (Stoppwörter, Suchkaskade) | 0 | 59 | 0 | `ALLES GRUEN` |
| `h10probe` (Semantikindex) | 0 | 61 | 0 | `ALLES GRUEN` |

`h7probe` im Einzelnen: 99 Zuordnungen, **99/99** aufgelöst, keine doppelte linke Seite;
56/72 Masken instanziierbar, 57/73 Knöpfe gefunden, **57 aktiv**, 0 verdecken ein
Bedienelement, Anker 57/57, kein zweiter Knopf bei erneutem Anbringen (56/56).

Die Zeilenzahl bleibt bei **99** — die im Auftrag als möglich genannte Streichung auf 98
entfällt, weil `Form_Variantentest` erhalten bleibt (§ 5.3).

---

## 7. Offene Prüfpunkte an der laufenden Oberfläche

Maschinell nicht belegbar, beim nächsten Programmlauf mit Blick zu prüfen:

1. **`UcWirtschaftlichkeit`** — die einzige der vier Seiten, die der Prüfstand nicht bauen
   kann. Zu sehen: Sitzt ihr Infoknopf nach dem Abrücken an einer sinnvollen Stelle, und ist
   er aktiv? Ihr Knopf stammt als einziger aus dem Designer (Location 858/2), nicht aus
   `InfoKnopf.Anbringen` — die Abrück-Regel greift trotzdem, weil sie nur `Top` verändert.
2. **Popup mit Beschreibung** — Höhe, Zeilenumbruch und die Randklemmung an einem Knopf ganz
   rechts bzw. ganz unten am Bildschirm. Und: Ist bei gesetztem `LinkArea` wirklich nur die
   Zeile „➔ …" unterstrichen?
3. **Popup ohne Beschreibung** — beim allerersten Start ohne Netz (nur eingebetteter
   Startbestand). Muss aussehen wie vor H11, inklusive: der **ganze** Text ist Link.
4. **Reiter „Berichte & Kosten"** — durch alle vier Seiten navigieren und dabei die
   Fenstergröße ändern: Der Seitenknopf muss sich neu einmessen und darf nie in die Kopfzeile
   zurückspringen.
5. **`Form_Gebaeude2`** — Fenstertitel in beiden Sprachen (Registry `HKCU\Software\wp-plan`,
   `nLanguage`).
6. **Reiter Simulation** — die Kachel „Optimierung" ist weg; die Kachel „Simulation" steht
   allein und die Fläche daneben wirkt nicht verwaist.
7. **Einstellungen → Web-Schnittstellen** — nach dem Entfernen von Feld und Beschriftung
   bleibt unter „Online-Dokumentation" Leerraum; ggf. Folgeaufgabe für die Anordnung.

---

## 8. Geänderte Dateien

**Geändert (14):**

```
Allgemein/Hilfe/HelpCatalog.cs           Beschreibung, Nachladelauf, Suchgrenze
Allgemein/KI/KiChatService.cs            Verfallsdatum des Antwort-Caches
MyResource/Resource.resx                 GEB2_TITEL (de)
MyResource/Resource.en-US.resx           GEB2_TITEL (en)
MyResource/Resource.Designer.cs          von VS erzeugt
Properties/Settings.settings             WordPressPrefix entfernt
Properties/Settings.Designer.cs          WordPressPrefix entfernt
app.config                               WordPressPrefix entfernt
Views/Admin/Form_AdminSettings.cs        Visible-Krücke entfernt
Views/Admin/Form_AdminSettings.Designer.cs  txt_/lbl_WPPrefix entfernt
Views/BerichteKosten/UcBerichteKosten.cs Seitenknopf abrücken
Views/Gebäude/Form_Gebaeude2.cs          Fenstertitel (CP1252 erhalten)
Views/Hauptformular/Form_Start.cs        Optimierungskachel ausblenden
Views/Help/Form_HelpPopup.cs             Kurzbeschreibung + Umbruch + LinkArea
```

**Gelöscht (7):**

```
Views/BHKW/Form_BHKWAdmin.cs.bak
Views/BHKW/Form_BHKWEing.cs.bak
Views/BHKW/Form_DBBHKW.cs.bak
Views/Heizkessel/Form_Heizkessel.cs.bak
Views/Kosten/ucKategorieHeader.cs
Views/Kosten/ucKategorieHeader.Designer.cs
Views/Kosten/ucKategorieHeader.resx
```

**Unverändert, obwohl in Reichweite:** `Allgemein/Hilfe/help_cache.json` (eingebetteter
Startbestand — ausdrücklich nicht anzufassen), `Allgemein/Hilfe/help_mapping.txt`
(99 Zeilen), `Allgemein/Hilfe/InfoKnopf.cs`, `Allgemein/Hilfe/HilfeAutomatik.cs`, die vier
Seiten des Reiters „Berichte & Kosten", `KiKern/`, `KiSchreibschutz.cs`, `SchemaMigration`,
`DbWerte`, `CLAUDE.md`.

**Prüfstand:** `dev/h11probe/` (`h11probe.csproj`, `Probe.cs`, `lauf1.txt` … `lauf6.txt`).
