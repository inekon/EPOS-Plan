# K4 · `Form_Kosten` — Reiter „Kostenprofil", Kopfzeile, Energie-Summenlabel (HF4)

**Stand: 20.08.2026.** Umsetzung der Etappe **K4** aus
[`Konzept_Kosten_Energietraeger_EPOS-Plan.md`](../../../Konzept_Kosten_Energietraeger_EPOS-Plan.md)
(§ 6 HF4, § 9, § 10). Ausgangsstand `a0c67e6`. Vorgängeretappen:
[`K1_Aufraeumung_Protokoll.md`](K1_Aufraeumung_Protokoll.md),
[`K2_Einheitenpruefung_Protokoll.md`](K2_Einheitenpruefung_Protokoll.md),
[`K3_Seeds_Dialog_Protokoll.md`](K3_Seeds_Dialog_Protokoll.md).

**Ergebnis in drei Sätzen.** `Form_Kosten` hat einen **vierten Reiter „Kostenprofil"**
mit zwei anklickbaren Karten statt der beiden Knöpfe, die bisher als graues Panel unter
der Energieträgerliste hingen; die Karten zeigen den gepflegten Bestand schon vor dem
Klick. Die **blaue Kopfleiste über der Trägerliste** (`panel9` + `label4`) ist weg, die
Liste rückt um deren 30 px nach oben. Die **Fußzeile des Energie-Reiters** kommt nicht
mehr aus der toten Kategorie-3-Summe, sondern aus dem `KostenEmissionRechner`; fehlt ein
Simulationsergebnis oder ein Trägerpreis, steht dort „—" statt einer 0, die nach Zahl
aussieht.

Die Etappe ist **rein oberflächlich** — kein Migrationsschritt, keine geänderte
Rechenvorschrift, kein Schreibzugriff auf neue Felder. Die einzige inhaltliche Änderung
ist die Quelle des Summen-Labels im Energie-Reiter, und die ersetzt eine Zahl, die seit
HF1/L1 konstant „0,00 €" war.

---

## 1 Was geändert wurde

| Datei | Zeilen | Änderung |
|---|---|---|
| `Views\Kosten\Form_Kosten.cs` | :18-26 | Kategorie-Doktrin im Kopfkommentar: Index-Arithmetik gilt nur noch für die drei Bestandsreiter, alles andere über den Wächter |
| | :55-66 | Neue Felder `tabKostenprofil`, `_karteKostenprofil`, `_karteSpotpreise`, Konstante `STRICH` |
| | :112-121 | Konstruktor: `KopfzeileEnergietraegerEntfernen()` vor dem Befüllen, `BaueKostenprofilReiter()` statt `BauePreisreihenEinstieg()` |
| | :127-157 | **neu** `AktuelleKategorieOderNull()` — der Wächter |
| | :159-231 | **neu** `BaueKostenprofilReiter()` — vierte TabPage mit zwei Karten |
| | :233-269 | **neu** `AktualisiereKostenprofilKarte()` |
| | :271-311 | **neu** `AktualisiereSpotpreisKarte()` |
| | :313-335 | **neu** `MonatsniveauSpanne()` — min/max aus „m1;…;m12" |
| | :337-380 | **neu** `KopfzeileEnergietraegerEntfernen()` — `panel9`/`label4` weg, Liste nach oben |
| | :513-544 | **neu** `LiesEnergiekostenProJahr()` — Energiekosten p. a. aus `KostenEmissionRechner` |
| | :545-621 | `Gesamtkosten()` — Wächter-Klammer, Energie-Zweig aus dem Rechner, Kategorie-3-Lesestelle stillgelegt |
| | :779-784 | `btnDeleteGroup_Click()` — Wächter vor dem kategoriegefilterten DELETE |
| | :1062-1069 | `EnsureMainComponentExists()` — Wächter statt `SelectedIndex + 1` |
| | :1154-1160, :1202 | `AddKostenItem()` — Wächter vor dem Dialog, `kat.Value` im INSERT |
| | :1342-1359 | `tabMain_SelectedIndexChanged()` — neuer Zweig für den vierten Reiter |
| | *entfällt* | `BauePreisreihenEinstieg()` (alt :122-165) samt Aufrufstelle — **restlos entfernt** |
| `Views\Kosten\EinstiegsKarte.cs` | **neu**, 180 Z. | Karten-Klasse: Titel/Beschreibung/Status, Rahmen, Hover, Klick auf ganzer Fläche |
| `Views\Hauptformular\Form_Start.cs` | :2001 | Querverweis nachgezogen: zeigte auf die entfernte Methode, zeigt jetzt auf `BaueKostenprofilReiter` |
| `MyResource\Resource.resx` | +11 / −2 | 11 `KPROF_*` additiv, 2 tote `PREIS_BTN_*` entfernt |
| `MyResource\Resource.en-US.resx` | +11 / −2 | dito, englisch |
| `MyResource\Resource.Designer.cs` | +11 / −2 | Properties von Hand nachgezogen (Muster K3) |

`Form_Kosten.Designer.cs` ist **unverändert** — Begründung in § 4.

---

## 2 Der Kategorie-Wächter

### 2.1 Warum überhaupt

Bis K4 galt `KategorieID = tabMain.SelectedIndex + 1` an mehreren Stellen roh. Mit einem
vierten Reiter liefert dieselbe Rechnung **Kategorie 4** — die es in
`Tab_KostenKategorie` nicht gibt. Ein Datensatz mit `KategorieID = 4` wäre in keiner
Auswertung sichtbar und in keiner Summe enthalten; er fiele erst Jahre später auf. Der
Wächter gibt es deshalb genau einmal, und er darf ausdrücklich „keine Kategorie" sagen:

```csharp
private int? AktuelleKategorieOderNull()
{
    if (tabMain == null || tabMain.SelectedTab == null) return null;
    if (tabKostenprofil != null && ReferenceEquals(tabMain.SelectedTab, tabKostenprofil))
        return null;
    int index = tabMain.SelectedIndex;
    if (index < 0 || index > 2) return null;      // nur die drei Bestandsreiter
    return index + 1;
}
```

Geprüft wird über die **Identität** der Reiterseite, nicht über ihren Text: Die
Beschriftungen sind übersetzbar, die Seite ist es nicht. Die zusätzliche Bereichsprüfung
fängt einen künftigen fünften Reiter mit ab.

### 2.2 Alle Fundstellen und ihre Absicherung

| # | Stelle | Zeile | Vorher | Absicherung |
|---|---|---|---|---|
| 1 | `Form_Kosten` (Ktor) | :76-78 | `SelectedIndex = 0`, `kategorieID = 1` | unverändert korrekt — Reiter 0 ist gesetzt, bevor der vierte existiert |
| 2 | `AktuelleKategorieOderNull()` | :147 | — | **die eine Umrechnungsstelle** |
| 3 | `Gesamtkosten()` | :550-558 | las `kategorieID` blind | Wächter-Klammer: ohne Kategorie „—" im Fuß, `return` |
| 4 | `Gesamtkosten()` Energie-Zweig | :580-592 | `LiesKomponentenSummen(…, 3)` | `kat.Value == KATEGORIE_ENERGIE` → `KostenEmissionRechner` |
| 5 | `Gesamtkosten()` Sonst-Zweig | :594-606 | `kategorieID` | `kat.Value` |
| 6 | `btnDeleteGroup_Click()` | :779-784 | ungeprüft, DELETE auf `kategorieID` | `if (!…HasValue) return;` **vor** MessageBox und DELETE |
| 7 | `EnsureMainComponentExists()` | :1065-1069 | `tabMain.SelectedIndex + 1` | Wächter, `return` ohne Kategorie |
| 8 | `AddKostenItem()` | :1156-1160 | — | Wächter **vor** dem Eingabedialog |
| 9 | `AddKostenItem()` INSERT | :1202 | `tabMain.SelectedIndex + 1` | `kat.Value` |
| 10 | `tabMain_SelectedIndexChanged()` | :1352-1359 | drei Textvergleiche, sonst Durchfall | neuer **erster** Zweig: `kategorieID = 0`, Karten neu lesen, `return` |
| 11 | `UpdateDetailPanel()` | :670, :691 | `kategorieID == KATEGORIE_*` | unkritisch — Gleichheitsvergleich, fällt bei 0 durch |
| 12 | `btnTest_KostenUebernahme_Click()` | :1410 | `!= KATEGORIE_INVESTITION → return` | bereits abgesichert |
| 13 | `btnBetriebskostenVdi_Click()` | :1459 | `!= KATEGORIE_BETRIEB → return` | bereits abgesichert |
| 14 | `HinweiszeileAnlegen()` | :1486, :1497 | Gleichheitsvergleiche | unkritisch, fällt bei 0 durch |
| 15 | `LoadKostenFaktoren()` | :973 | `kategorieID` an `LiesZusatz` | nur aus den Listen der Reiter 1/2 erreichbar; bei 0 liefert die Abfrage leer |
| 16 | `Zeile_DeleteRequested()` | :902 | `DELETE … WHERE ID = ?` | kategorieunabhängig — kein Wächter nötig |
| 17 | `btn_Carrier_Click()` / `btn_Delete_Click()` | :1819, :1831 | Energieträger-Verwaltung | kategorieunabhängig — kein Wächter nötig |

**Doppelter Boden.** Zweig 10 setzt `kategorieID = 0`, sobald der vierte Reiter betreten
wird. Alle Gleichheitsvergleiche gegen `KATEGORIE_*` (Zeilen 11, 14) fallen dadurch von
selbst durch, und ein DELETE mit `KategorieID = 0` träfe auch dann keine Zeile, wenn ein
Pfad den Wächter einmal umginge. Das ersetzt den Wächter nicht, es sichert ihn ab.

---

## 3 Der Reiter „Kostenprofil"

### 3.1 Warum ein eigener Reiter und warum Karten

Kostenprofil und Spotmarktpreise sind **Preisverläufe über das Jahr** und damit etwas
anderes als die Arbeits- und Grundpreise je Energieträger, die der Reiter
„Energiekosten" pflegt. Bis K4 hingen sie als graues Panel mit zwei Knöpfen unter der
Trägerliste — an einer Stelle, an der sie zur darüberstehenden Liste zu gehören
schienen, obwohl sie projektweit gelten und keinem einzelnen Träger zugeordnet sind.

Karten statt Knöpfe, weil beide Einstiege einen **Zustand tragen, der vor dem Klick
interessiert**: „Ist schon ein Profil da, und wie sieht es aus?" Ein Knopf kann nur
beschriftet werden; die Karte zeigt den Bestand mit und erspart das Öffnen zum
Nachsehen. Dieselbe Überlegung steht hinter den KonfigUI-Karten
(`Views\Simulation\ErzeugerKarte.cs`).

### 3.2 Die Karten-Klasse

`SectionPanel.cs` wurde geprüft und **nicht** verwendet: Das ist eine
`Dock=Top`-Abschnittsüberschrift mit dunklem Balken, ohne Klick- und Hover-Verhalten,
ohne Beschreibungs- und Statuszeile — ein anderer Baustein. Es bleibt unberührt (und ist
BOM-los cp1252, § 5). Stattdessen `Views\Kosten\EinstiegsKarte.cs`, neu und schlank:
Titel fett, Beschreibung, Statuszeile, gezeichneter Rundeck-Rahmen, Hover (Fläche
`#EFF6FF`, Rahmen `#3B82F6`, 2 px), `Cursor.Hand`, **keine Buttons**. Klick und Hover
sind auf Karte *und* alle Beschriftungen gelegt — sonst fangen die Labels die
Mausereignisse ab und die Karte flackert beim Überfahren.

### 3.3 Die Statuszeilen

| Karte | Quelle | Anzeige |
|---|---|---|
| Kostenprofil | `KostenprofilCtrl.ReadAllByProjekt()` | `‹Bezeichner› — Monatsniveau X–Y ct/kWh`; ohne Profil `KPROF_STATUS_KEIN_PROFIL`; Lesefehler „—" |
| Spotmarktpreise | `PreisreiheCtrl.ReadVerfuegbare()` | `N Reihen, 2023–2025` / `N Reihen, 2024` / `N Reihen, Jahr nicht gepflegt`; ohne Reihe `KPROF_STATUS_KEINE_REIHEN`; Lesefehler „—" |

Das Monatsniveau wird aus dem Ablageformat `m1;…;m12` gelesen — `Split(';')` +
`double.TryParse(…, InvariantCulture)`, genau wie `Form_Kostenprofil` es schreibt.
Reihen ohne gepflegtes Jahr (`Jahr <= 0`) spannen den Zeitraum nicht auf.

**Lesefehler bleiben still.** Ist `Tab_Kostenprofil` oder `Tab_Preisreihe` in einer
Bestands-DB noch nicht migriert, zeigt die Karte „—". Eine MessageBox beim bloßen Öffnen
des Kostendialogs wäre hier nur im Weg — der Reiter ist ein Einstieg, kein Prüfbericht.

### 3.4 Verhalten

Klick auf „Kostenprofil" öffnet `Form_Kostenprofil` über die **unveränderte**
Bestandslogik aus `KostenprofilBearbeiten()` (erstes Projektprofil oder neu). Klick auf
„Spotmarktpreise" öffnet `Form_SpotpreisImport(m_ID_Projekt)`. Nach Dialogschluss wird
die jeweilige Statuszeile neu gelesen; beim **Betreten** des Reiters werden beide neu
gelesen, damit ein Import aus einer anderen Maske hier sichtbar wird.

---

## 4 Kopfzeile entfernen — Weg-Entscheid

**Gewählt: programmatisch**, `KopfzeileEnergietraegerEntfernen()` (:337-380), aufgerufen
im Konstruktor **vor** `FillCarrierComboBox()`.

**Parent-Kette selbst verifiziert** (`Form_Kosten.Designer.cs`, unverändert):

```
tabEnergie (:284)
└── panel8 (:293-303)            LightGray, 355 × 601 @ (17,18)
    ├── btn_Delete   (:305)      @ (220,557)
    ├── btn_Carrier  (:316)      @ (6,557)
    ├── listBox_Energieträger (:327)  342 × 514 @ (6,37)
    └── panel9 (:338-347)        NavyMid #1A3261, 343 × 25 @ (6,7)
        └── label4 (:349-356)    „Energieträger"
```

Der Vorgängerbefund ist damit bestätigt: Die blaue Leiste über der Trägerliste ist
**`panel9` + `label4`**. Die in der Bestandsaufnahme vermuteten `panel2`/`label5` hängen
an `panel3` (:105), `label1` an `panel5` (:219) — **andere Reiter**, unberührt.

**Warum nicht im Designer.** `WindowsFormsApplication1\CLAUDE.md` untersagt das Editieren
von Designer-Dateien von Hand; das Konzept wiederholt die Hausregel für HF4 ausdrücklich
(§ 6.1, „Hausregel Designer unberührt"). Ein Designer-Eingriff hätte vier Stellen der
`InitializeComponent` treffen müssen (Felddeklaration, `new`, `SuspendLayout`/
`ResumeLayout`, `Controls.Add`) und wäre beim nächsten Öffnen im WinForms-Designer erneut
zu verteidigen gewesen. Das Entfernen zur Laufzeit steht an **einer** Stelle, ist dort
begründet und rückstandsfrei umkehrbar. Die Datei war zwar UTF-8 **mit** BOM und damit
technisch gefahrlos editierbar (§ 5) — den Ausschlag gab die Hausregel, nicht das
Encoding.

**Layout-Nachzug.** `panel9` sitzt bei `y = 7` und ist 25 px hoch, die Liste beginnt bei
`y = 37`. Nach dem Entfernen rückt die Liste auf `y = 7` und wächst um die gewonnenen
30 px auf 544 — **die Unterkante bleibt bei 551**, der Abstand von 6 px zu den Knöpfen
bei `y = 557` bleibt erhalten. `label4` ist ein Kind von `panel9` und wird mit
`Dispose()` mit entsorgt. Verwaiste `.resx`-Einträge der entfernten Beschriftung bleiben
stehen (auftragsgemäß).

---

## 5 Encoding-Befunde je angefasster Datei

Vor jedem Edit gemessen (utf-8-Decodeversuch + BOM-Prüfung):

| Datei | Bytes vorher | BOM | Kodierung | Weg |
|---|---|---|---|---|
| `Views\Kosten\Form_Kosten.cs` | 79.557 | ja | UTF-8 | Edit-Tool zulässig; ein Eingriff byte-erhaltend per Python (§ 6.1) |
| `Views\Kosten\Form_Kosten.Designer.cs` | 29.602 | ja | UTF-8 | **nicht angefasst** (§ 4) |
| `Views\Hauptformular\Form_Start.cs` | 92.973 | ja | UTF-8 | Edit-Tool |
| `MyResource\Resource.resx` | 286.461 | ja | UTF-8 | byte-erhaltend per Python |
| `MyResource\Resource.en-US.resx` | 281.542 | ja | UTF-8 | byte-erhaltend per Python |
| `MyResource\Resource.Designer.cs` | 725.698 | ja | UTF-8 | byte-erhaltend per Python |
| `Views\Kosten\SectionPanel.cs` | 1.707 | **nein** | **cp1252** (0x80 an Pos. 344) | **nicht angefasst** — Falle umgangen, indem die Karte neu geschrieben wurde |
| `Views\Kosten\EinstiegsKarte.cs` | neu | ja | UTF-8, CRLF | neu angelegt wie die Geschwisterdateien |

Alle geschriebenen Dateien behalten BOM-Status und CRLF. Nachkontrolle: beide `.resx`
mit `xml.dom.minidom` als wohlgeformt geprüft, `Resource.Designer.cs` mit ausgeglichener
Klammerbilanz (4738 / 4738).

**Die cp1252-Falle wurde nicht entschärft, sondern umgangen.** `SectionPanel.cs` bleibt
unverändert BOM-los cp1252. Wer es später anfasst, muss byte-erhaltend arbeiten
(Memo `wp-plan-cp1252-edit-falle`).

---

## 6 Zwei eigene Fehler, gefunden vom Compiler

Beide traten im ersten Build nach den Ressourcen-Edits auf und sind behoben. Sie stehen
hier, weil beide eine wiederkehrende Falle sind.

### 6.1 Deutsche Anführungszeichen in einem C#-**String**

```csharp
Console.WriteLine("Der Reiter „Kostenprofil" konnte nicht aufgebaut werden: " + ex.Message);
```

Der Hausstil schreibt `„Text"` mit **U+201E** als öffnendem und einem **ASCII-`"`** als
schließendem Zeichen — in Kommentaren seit jeher und harmlos. In einem
**Zeichenkettenliteral** beendet das ASCII-`"` die Zeichenkette (CS1003/CS1010/CS1026,
6 Folgefehler). Repoweiter Scan über alle Zeilen mit U+201E: **nur diese eine** Stelle
lag in einem Literal, alle übrigen 30 Treffer sind Kommentare. Behoben durch Weglassen
der Anführungszeichen in der Meldung. Der Eingriff lief byte-erhaltend über Python, weil
das Edit-Werkzeug die Zeile wegen des Sonderzeichens nicht eindeutig traf.

### 6.2 `find('        }')` trifft auch `            }`

Beim Entfernen der zwei toten Properties aus `Resource.Designer.cs` suchte der erste
Versuch das Blockende mit `t.find('        }\r\n', i)` — acht Leerzeichen plus Klammer.
Die Zeile `            }` (zwölf Leerzeichen, das Ende des `get`-Blocks) **enthält** diese
Folge ab Position 4. Der Schnitt endete deshalb eine Ebene zu früh und ließ je eine
verwaiste `        }` stehen → CS1022. Behoben durch **zeilenweises** Arbeiten mit
`zeilen.index(...)` und exaktem Zeilenvergleich statt Teilstringsuche; Datei vorher per
`git checkout` sauber zurückgesetzt, damit keine Reste der ersten Fassung bleiben.

---

## 7 Energie-Summenlabel aus dem `KostenEmissionRechner`

**Vorher.** Die Fußzeile „PROJEKT GESAMT (Energiekosten)" summierte
`LiesKomponentenSummen(m_ID_Projekt, 3)` — die Kategorie-3-Zeilen aus
`Tab_ProjektWerte`. Seit HF1/L1 (19.08.2026) wird auf Kategorie 3 **nichts mehr
geschrieben**; die Zeile stand konstant auf „0,00 €".

**Jetzt.** `LiesEnergiekostenProJahr()` (:513-544) geht denselben Weg wie
`BetriebskostenCtrl.LiesBrennstoffkosten()`:

```csharp
ErgebnisModel erg = new ErgebnisCtrl().Load(m_ID_Projekt);
if (erg == null) return null;
VariantenDaten v = new VariantenDaten { IdProjekt = m_ID_Projekt, Ergebnis = erg };
KostenEmissionRechner.Berechne(v);
return v.Energiekosten;        // €/a, Brennstoffe + Netzstrom inkl. Grundpreise
```

`KostenEmissionRechner` ist die **eine** Stelle, die Verbrauchsmengen mit Trägerpreisen
und Heizwerten verrechnet. Eine zweite Preisverrechnung nur für ein Label wäre eine
doppelte Wahrheit.

**Der Leerfall bleibt ein Leerfall.** Der Rechner liefert bewusst `null` statt einer
Teilsumme, wenn für einen Träger mit Verbrauch der Preis fehlt. Diese Aussage wird nicht
eingeebnet: kein Simulationsergebnis, kein vollständiger Preissatz oder ein Lesefehler →
**„—"**, keine MessageBox, keine 0.

**Kategorie-3-Lesestelle stillgelegt.** `LiesKomponentenSummen(…, KATEGORIE_ENERGIE)`
wird für diesen Reiter nicht mehr aufgerufen; der Kommentar an Ort und Stelle
(:582-592) vermerkt: *Altzeilen-Löschung folgt K6/E3*. Die Methode selbst und die
Konstante `KATEGORIE_ENERGIE` bleiben — beides wird vom Migrationsschritt und von
`UcBkKosten` noch gebraucht.

---

## 8 Ressourcen — additiv, drei Hotspot-Dateien

11 neue Schlüssel, in `Resource.resx`, `Resource.en-US.resx` und (von Hand)
`Resource.Designer.cs`, jeweils **am Dateiende** angehängt — dasselbe Muster wie bei den
K3-Schlüsseln (`cf4e320`, Hunk `@@ -17641`).

| Schlüssel | de | en |
|---|---|---|
| `KPROF_TAB_TITEL` | Kostenprofil | Cost profile |
| `KPROF_KARTE_PROFIL_TITEL` | Kostenprofil | Cost profile |
| `KPROF_KARTE_PROFIL_INFO` | Preisniveau je Monat und Tagesgang je Woche [ct/kWh] … | Monthly price level and weekly daily pattern … |
| `KPROF_KARTE_SPOT_TITEL` | Spotmarktpreise | Spot market prices |
| `KPROF_KARTE_SPOT_INFO` | Stündliche Börsenpreise als Jahresreihe … | Import and manage hourly exchange prices … |
| `KPROF_STATUS_KEIN_PROFIL` | Noch kein Kostenprofil angelegt | No cost profile created yet |
| `KPROF_STATUS_PROFIL` | `{0} — Monatsniveau {1}–{2} ct/kWh` | `{0} — monthly level {1}–{2} ct/kWh` |
| `KPROF_STATUS_KEINE_REIHEN` | Noch keine Preisreihe importiert | No price series imported yet |
| `KPROF_STATUS_REIHEN` | `{0} Reihen, {1}–{2}` | `{0} series, {1}–{2}` |
| `KPROF_STATUS_REIHEN_EINJAHR` | `{0} Reihen, {1}` | `{0} series, {1}` |
| `KPROF_STATUS_REIHEN_OHNE_JAHR` | `{0} Reihen, Jahr nicht gepflegt` | `{0} series, year not maintained` |

**Zwei Schlüssel entfernt.** `PREIS_BTN_SPOTIMPORT` und `PREIS_BTN_KOSTENPROFIL` hatten
nach dem Wegfall von `BauePreisreihenEinstieg()` repoweit **keinen Verwender** mehr
(Grep-Beleg § 9); das Konzept sieht ihren Wegfall ausdrücklich vor (§ 6.1). Entfernt aus
allen drei Dateien.

Zählprobe: `KPROF_` — resx de **11**, resx en **11**, Designer **22** (Property +
`GetString`-Argument). `PREIS_BTN_` repoweit **0**.

---

## 9 Verifikation

### 9.1 Grep-Belege

| Prüfung | Ergebnis |
|---|---|
| `BauePreisreihenEinstieg` in `*.cs` | **0** Treffer im Code. Verbleibend: 1 Konzeptzeile (beschreibt die Entfernung) und 1 Doc-Kommentar in `Form_Kosten.cs:169`, der erklärt, was der Reiter ablöst. Der Querverweis in `Form_Start.cs:2001` zeigte auf die entfernte Methode und wurde auf `BaueKostenprofilReiter` nachgezogen. |
| `PREIS_BTN_` in `*.cs`/`*.resx` | **0** Treffer |
| `tabMain.SelectedIndex + 1` | **0** Treffer — beide Fundstellen laufen über den Wächter |
| `AktuelleKategorieOderNull` | **4** Aufrufstellen (:550, :784, :1067, :1159) + Definition (:147) |
| `panel9` | nur noch in `Designer.cs` (Deklaration/Aufbau) und in `KopfzeileEnergietraegerEntfernen()` |
| `<<<<<<<` in `*.cs`/`*.md`/`*.resx` | **kein echter Treffer** (Anfang und Ende). Die drei Fundstellen sind Prosa in `Konzept_Kosten_Energietraeger…md` und `K1_Aufraeumung_Protokoll.md`, die den Sweep selbst beschreiben. Ausgeschlossen: `mit_Puffer_KI_Lösungsversuch\`, `Tempkib2\`, `* - Kopie`, `.claude\worktrees\`, `*.bak` |

### 9.2 Build — Baseline gegen Ende

Gebaut wird **nur** `WindowsFormsApplication1.csproj`, inkrementell, `-m -v:m`, Debug/x86
(VS-2022-MSBuild 17.14.51). `dotnet build` funktioniert am Hauptprojekt nicht (MSB4803,
COMReference).

| | Fehler | Warnungen | aus K4-Dateien |
|---|---|---|---|
| Baseline `a0c67e6` | 0 | 6 (vorbestehend) | — |
| Ende K4 | **0** | **6**, identisch | **0** |

Die sechs Warnungen sind unverändert dieselben und stammen sämtlich aus Dateien, die K4
nicht anfasst: `StromverbraucherStammCtrl.cs:25` (CS0108), `KlimaregionStammCtrl.cs:22`
und `:23` (CS0109), `WErzeugerModel.cs:6` (CS0108), `MDIMainForm.cs:359` (CS4014) und
`:348` (CS1998). **Aus `Form_Kosten.cs`, `EinstiegsKarte.cs`, `Form_Start.cs` und den
drei `MyResource`-Dateien kommt keine einzige Diagnose.**

**Anmerkung zur Build-Regel.** Zwei Anläufe dieser Etappe rissen ab, einer davon an einem
vollen `-t:Rebuild`, der das Watchdog-Fenster von 600 s überschritt. Die Regel lautet
seither: nur das Anwendungsprojekt, inkrementell, lange Läufe im Hintergrund; das
Abnahmekriterium ist **nicht** ein vollständiges Warnungsinventar, sondern „keine neuen
Diagnosen aus K4-berührten Dateien". Ein rein inkrementeller Lauf ohne geänderte Quellen
überspringt `CoreCompile` und zeigt gar keine Warnungen — die Baseline oben ist deshalb
der Lauf **mit** neu übersetztem Projekt.

### 9.3 Kein Programmstart

Auftragsgemäß nicht gestartet, keine DB-Verifikation nötig (K4 fasst kein Schema an).
Die Sichtprüfung übernimmt Philipp:

---

## 10 Sichtprüfliste für Philipp

Kostendialog eines Projekts mit gepflegten Energieträgern öffnen
(Seite „Berichte & Kosten" → Kosten).

1. **Vierter Reiter da.** Hinter „Energiekosten" steht „Kostenprofil". Die Reiter 1–3
   heißen unverändert Investitionskosten / Betriebskosten / Energiekosten.
2. **Beide Karten öffnen ihren Dialog und aktualisieren den Status.** Karte
   „Kostenprofil" → `Form_Kostenprofil`; nach Speichern und Schließen steht in der Karte
   Name und Monatsniveau. Karte „Spotmarktpreise" → `Form_SpotpreisImport`; nach einem
   Import steht dort die neue Reihenzahl. Beim Überfahren hebt sich die Karte hervor,
   der Zeiger wird zur Hand — **Knöpfe gibt es keine mehr**.
3. **Kopfleisten weg, Listen oben — auf ALLEN DREI Reitern.** Der blaue Balken
   „Energieträger" über der jeweils linken Liste ist auf **Investitionskosten**,
   **Betriebskosten** und **Energiekosten** verschwunden; jede Liste beginnt jetzt oben
   am grauen Feld, ihre Unterkante und die Abstände zu allem darunter
   („➕ Hinzufügen…" / „🗑️ Löschen" bzw. „➕ Position Hinzufügen") sind unverändert.
   Das graue Panel mit den zwei alten Knöpfen unter der Energieträgerliste ist weg.
   Der große Platzhalter „Energieträger auswählen" mitten im rechten Bereich bleibt
   bewusst stehen — er sagt, was zu tun ist, solange nichts gewählt ist.
4. **Fußzeile zeigt Wert bzw. „—".** Im Reiter „Energiekosten" steht unten
   „PROJEKT GESAMT (Energiekosten): ‹Betrag› €" mit den Energiekosten p. a. aus dem
   Simulationsergebnis — **nicht mehr konstant 0,00 €**. Ohne Simulationslauf oder bei
   unvollständigen Trägerpreisen steht dort „—", ohne Fehlermeldung. Auf dem Reiter
   „Kostenprofil" steht ebenfalls „—".
5. **Reiter 1–3 verhalten sich unverändert.** Komponente wählen, Position hinzufügen,
   Gruppe löschen, „Planwert übernehmen…", „Betriebskosten VDI 2067…" — alles wie
   vorher, und die Summen in der Fußzeile stimmen mit dem gewählten Reiter überein.

**Besonders sehenswert:** auf den Reiter „Kostenprofil" wechseln und dort *versuchen*,
etwas zu erfassen — es gibt dort bewusst keine Eingabe. Anschließend zurück auf
„Investitionskosten": Die Fußzeile muss sofort wieder die Investitionssumme zeigen.

---

## 11 Offene Punkte

1. **Kategorie-3-Altzeilen** stehen weiter in `Tab_ProjektWerte` von Bestands-DBs. Das
   Summen-Label liest sie nicht mehr; die Löschung ist Entscheidung **E3** und gehört zu
   **K6** (Migrationsschritt nach der Label-Umstellung — genau diese Reihenfolge ist mit
   K4 jetzt erfüllt).
2. **Verwaiste `.resx`-Einträge** der entfernten Beschriftung `label4` bleiben in
   `Form_Kosten.*.resx` stehen (auftragsgemäß). Sie kosten nichts und ihr Entfernen hieße
   `.resx`-Dateien von Hand zu editieren.
3. **`SectionPanel.cs` bleibt ungenutzt** und BOM-los cp1252. Entweder es findet in K5
   eine Aufgabe, oder es gehört auf die Aufräumliste — bewusst **nicht** in K4 entschieden,
   weil das Löschen einer Datei kein Nebenzweig einer UI-Etappe sein sollte.
4. **UI-Abnahme steht aus** (§ 10). Das Abnahmekriterium der Etappe ist laut Konzept § 10
   „UI-Abnahme Philipp am Screenshot-Fall; Kategorie-Wächter getestet" — der Wächter ist
   statisch belegt (§ 2.2), die Sichtprüfung fehlt noch.
5. **Kartenbreite ist fest** (440 px, zwei Karten nebeneinander bei 24/488 px). Auf sehr
   schmalen Fenstern greift der `AutoScroll` des Reiters. Ein Fließlayout wäre möglich,
   war aber für zwei Karten Beiwerk.

---

## 12 Nachtrag 20.08.2026 — Kopfleisten auch auf den Reitern 1 und 2

**Anlass.** Die Sichtabnahme durch Philipp bestätigte den vierten Reiter, zeigte aber,
dass K4 nur **eine** der drei blauen Kopfleisten entfernt hatte. Auf
**Investitionskosten** und **Betriebskosten** stand sie noch — dort obendrein **sachlich
falsch**: Die Liste führt GEWERKE (Heizkessel, Pufferspeicher, BHKW), keine
Energieträger. Eine falsche Überschrift ist schlechter als keine; da die Listen in ihrem
Zusammenhang selbsterklärend sind, fällt die Zeile ersatzlos weg statt umbenannt zu
werden.

**Parent-Ketten, je Reiter frisch verifiziert** (`Form_Kosten.Designer.cs`, unverändert).
Die drei Reiter sind baugleich aufgebaut — dieselbe Geometrie, dieselbe Farbe `#1A3261`,
dieselbe Beschriftung:

| Reiter | Container | Kopfleiste | Beschriftung | Liste |
|---|---|---|---|---|
| Investitionskosten (:87) | `panel3` (:101) @ (17,18) 355 × 200 | `panel2` (:121) @ (6,7) 343 × 25 | `label5` (:131) | `listBox_Erzeuger` (:111) @ (6,37) 342 × 157 |
| Betriebskosten (:184) | `panel4` (:196) @ (17,18) 355 × 200 | `panel5` (:216) @ (6,7) 343 × 25 | `label1` (:226) | `listBox_Betriebskosten` (:206) @ (6,37) 342 × 157 |
| Energiekosten (:281) | `panel8` (:293) @ (17,18) 355 × 601 | `panel9` (:338) @ (6,7) 343 × 25 | `label4` (:349) | `listBox_Energieträger` (:327) @ (6,37) 342 × 514 |

Damit ist auch die Vermutung der Bestandsaufnahme endgültig eingeordnet: `panel2`/`label5`
und `panel5`/`label1` sind **nicht** die Leiste des Energie-Reiters, sondern deren
Geschwister auf den beiden anderen Reitern. Alle drei mussten weg, nur eben je an ihrem
eigenen Ort.

**Was bleibt: `label3` / `label2` / `label6`.** Die drei Beschriftungen
„Energieträger auswählen" (18 pt, @ (209, 246), 205 × 74, `label2`/`label6` zentriert)
sitzen in `panel1` (:153), `panel6` (:235) und `panel7` (:358) — den **rechten**
Detailbereichen, nicht in den Kopfleisten. Sie sind Platzhalter mitten in der leeren
Fläche und sagen dort, was zu tun ist, solange nichts gewählt ist. Sie bleiben stehen;
K4 hatte `label6` aus demselben Grund bereits stehen lassen, der Nachtrag bleibt dabei
konsistent.

**Umsetzung** — `Views\Kosten\Form_Kosten.cs`:

| Zeilen | Änderung |
|---|---|
| :112-116 | Aufrufstelle im Konstruktor: `KopfzeilenEntfernen()` statt `KopfzeileEnergietraegerEntfernen()`, weiterhin **vor** dem Befüllen der Listen |
| :338-387 | `KopfzeileEnergietraegerEntfernen()` → **`KopfzeilenEntfernen()`**: dokumentiert jetzt alle drei Leisten in einer Tabelle und ruft dreimal den Helfer |
| :389-418 | **neu** `KopfleisteEntfernen(Panel leiste, Control liste, string reiter)` — die eigentliche Mechanik, je Leiste einmal |

Die Mechanik ist unverändert die aus K4: Elternpanel merken, `Top` der Leiste als neue
Oberkante der Liste, Höhengewinn = `liste.Top − leiste.Top` (überall 30 px),
`Controls.Remove` + `Dispose()` (nimmt die Beschriftung als Kind mit), dann Liste hoch
und um den Gewinn höher — **die Unterkante bleibt, wo sie war**, damit die Abstände zu
allem darunter erhalten bleiben. Für Investitions- und Betriebskosten heißt das
157 → 187 px bei gleichbleibender Unterkante 194 (Panelhöhe 200, 6 px Rand), für
Energiekosten unverändert 514 → 544 bei Unterkante 551.

Der Helfer greift auf `null` und auf bereits entfernte Leisten nicht zu und fängt je
Leiste einzeln ab: Scheitert eine, stehen die anderen beiden trotzdem richtig.

**Build.** Inkrementell, nur `WindowsFormsApplication1.csproj`, `-m -v:m`, Debug/x86:
**0 Fehler**, dieselben **6 vorbestehenden Warnungen** wie in § 9.2, **keine Diagnose aus
`Form_Kosten.cs`**.

**Offen.** Erneute Sichtprüfung der Punkte 3 und 5 aus § 10 durch Philipp; die übrigen
Punkte aus § 11 bleiben unverändert bestehen.
