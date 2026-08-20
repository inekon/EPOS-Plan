# Konzept: Dublettenprüfung beim Datenimport und Katalog-Dublettensuche

Stand: 20.08.2026. Analysebasis: vollständige Kartierung der Importpfade, der `*StammCtrl`-
Landschaft und des Admin-Bereichs unter `WindowsFormsApplication1` (ohne Altkopien und
Worktrees). Dieses Dokument ist reine Konzeption — es wurde noch keine Zeile Code geändert.

**Anlass.** Beim Import von Herstellerdaten (VDI 3805, CEC/PAN, Ganglinien) entstehen
Dubletten in den Katalogtabellen — teils identisch, teils geringfügig abweichend
(anderer Name, gleicher Inhalt). Künftig soll beim Import geprüft, der Anwender
hingewiesen und ihm die Wahl gelassen werden. Zusätzlich soll eine nachträgliche
Dublettenprüfung im Admin-Menü möglich sein.

---

## 1. Anforderung

### 1.1 Namenskonflikt beim Import (Vorgabe des Anwenders)

Beim Laden von Dateien (z. B. `.vdi`) und generell beim Datenimport wird je Eintrag mit
bereits vorhandenem Namen unterschieden:

1. **Überschreiben** — der vorhandene Eintrag wird durch den neuen ersetzt.
2. **Auslassen** — der Eintrag wird beim Import übersprungen.
3. **Umbenennen** — der Anwender vergibt für den neuen Eintrag einen anderen Namen.

**Leitregel (Invariante): Gleiche Namen dürfen im Katalog nicht vorhanden sein.**
Ein Import darf nie einen zweiten Satz mit demselben `Bezeichner` erzeugen — der
`Bezeichner` ist in diesem Bestand faktisch der Schlüssel (Verknüpfungen laufen über
Textfelder, nicht über IDs; `CLAUDE.md` der Wurzel).

### 1.2 Inhaltsdublette

Zusätzlich wird erkannt, wenn ein Import-Eintrag **inhaltlich** einem vorhandenen Satz
gleicht, obwohl der Name abweicht (typisch: Herstellerdatei-Update mit geänderter
Namensschreibweise, OEM-Zweitnamen, zweiter Importlauf einer umbenannten Datei).
Solche Einträge werden gemeldet; der Anwender entscheidet: **Auslassen** (Vorschlag)
oder **trotzdem importieren**.

### 1.3 Nachträgliche Prüfung

Unter **Administration** wird eine Dubletten-Suche über die Kataloge angeboten
(Namens- und Inhaltsdubletten im Bestand), mit geführter Bereinigung.

---

## 2. Ist-Zustand

### 2.1 Importpfade und ihre heutige Duplikatprüfung

Alle sechs schreibenden Importpfade prüfen bereits auf Namensgleichheit — aber mit
**fünf verschiedenen Mechanismen**, ausschließlich als Exaktvergleich auf `Bezeichner`,
und stets nur mit dem Ergebnis „auslassen":

| Pfad | Formular | Speicherweg → Zieltabelle | Prüfung heute | Fundstelle |
|---|---|---|---|---|
| Wärmepumpe (VDI 3805) | `Form_WP_einlesen` | `WPStammCtrl.Insert()` → `Tab_WP_STAMM` + `Tab_Kenndaten_STAMM` + `Tab_Kenndaten_Kuehlung_STAMM` | `RecordSet` mit **String-SQL** (nicht parametrisiert) | `Form_WP_einlesen.cs:215` |
| Heizkessel (VDI 3805) | `Form_Heizkessel_einlesen` | formular­eigenes `Insert(model, conn, tx)` → `Tab_Heizkessel_STAMM` | `SELECT COUNT(*)` in der Transaktion | `Form_Heizkessel_einlesen.cs:254` |
| Pufferspeicher (VDI 3805) | `Form_PufferSp_einlesen` | `PufferSpStammCtrl.InsertFrom()` → `Tab_Pufferspeicher_STAMM` | `Ctrl.Exists(textBox_Name.Text)` | `Form_PufferSp_einlesen.cs:235` |
| Solarkollektoren (VDI 3805) | `Form_SolarKollektoren_einlesen` | `SolarkollektorenStammCtrl.InsertFrom()` → `Tab_Solarkollektoren_STAMM` | `SELECT COUNT(*)` am Listeneintrag | `Form_SolarKollektoren_einlesen.cs:225` |
| PV-Module (CEC + PAN) | `Form_CECImport` (`Main_PV_Test`) | `PhotovoltaikStammCtrl.InsertFrom()` → `Tab_PV_STAMM` | `SELECT COUNT(*)` | `Form_CECImport.cs:449` |
| Stromganglinie (CSV/Datei) | `Form_Stromganglinie_Admin` | `StromganglinieStammCtrl.ImportGanglinie()` → `Tab_Stromganglinie_STAMM` + `…Daten_STAMM` | **nur ListBox-Suche, keine DB-Prüfung** | `Form_Stromganglinie_Admin.cs:124` |

Mehrfachimport (Mehrfachauswahl in der Liste, Schleife über markierte Einträge) gibt es
bei WP, Heizkessel, Pufferspeicher und Solarkollektoren; das gemeinsame Ergebnis-Enum
`VdiUebernahmeErgebnis { Gespeichert, Duplikat, Fehler }` und die Sammelmeldung
`VdiAuswahlFilter.LadeMeldung(…)` existieren bereits.

### 2.2 Warum trotzdem Dubletten entstehen

1. **Kein Inhaltsvergleich.** Gleiches Gerät unter leicht anderem Namen (neue
   Herstellerdatei, andere Schreibweise) wird anstandslos neu angelegt.
2. **Exaktvergleich ohne Normalisierung.** `"Vitocal 200-A "` (Leerzeichen am Ende,
   doppelte Leerzeichen innen) gilt als neuer Name.
3. **Uneinheitliche Prüfquelle.** Heizkessel und Pufferspeicher prüfen
   `textBox_Name.Text`, gespeichert wird ebenfalls die Textbox — beim Mehrfachimport
   hängt die Korrektheit daran, dass `ZeigeDetails(i)` je Schleifendurchlauf gelaufen
   ist; WP und Solar prüfen den Listeneintrag. Fünf Implementierungen für eine Frage.
4. **Ganglinien prüfen nur die Oberfläche**, nicht die Datenbank.
5. **Kein eindeutiger Index** auf `Bezeichner` in den Katalogtabellen — die Datenbank
   verhindert nichts (dokumentiert in `PhotovoltaikStammCtrl.cs:143` und
   `HeizkesselStammCtrl.cs:234`).
6. **Anzeige-Nebeneffekt PAN:** `PanDataService._allModules` ist `static` und wird nie
   geleert — mehrfaches Einlesen derselben `.pan`-Datei füllt die Auswahlliste doppelt.
7. Die WP-Prüfung per String-SQL bricht bei einem Apostroph im Typnamen.

### 2.3 Vorhandene Bausteine (werden wiederverwendet, nicht neu erfunden)

- **`SchemaMigration`, Schritt 24 „Katalogdubletten"** (`Allgemein\Update\SchemaMigration.cs`,
  Begründungsblock ab `:331`, Implementierung `Schritt_24_KatalogDubletten` /
  `KatalogBereinigen` ab `:3163`): bereinigt Namensdubletten im Bestand — je Namensgruppe
  bleibt die kleinste ID; gelöscht wird eine Dublette nur, wenn sie in jeder abweichenden
  Spalte den Leerwert trägt und nicht `ReadOnly` ist; sonst bleibt sie stehen und wird
  gemeldet. Geltungsbereich heute nur `Tab_Heizkessel_STAMM` und `Tab_PV_STAMM`
  (`KATALOGE_MIT_NAMEN`, `:3141`). Messung 18.08.2026: Kessel 21 → 13 Zeilen,
  PV 11 → 6 Zeilen.
- **Umbenenn-Sperren** bei doppeltem Zielnamen existieren in `HeizkesselStammCtrl.Update()`
  (`:396`) und `PhotovoltaikStammCtrl.Update(int id)` (`:228`) — bei Wärmepumpe,
  Pufferspeicher und Solarkollektoren fehlen sie.
- **`Exists`-Methoden** (`SELECT COUNT(*) … WHERE Bezeichner = ?`) in fünf Stamm-Controllern.
- **Admin-Formular-Muster:** `Views\Admin\Form_Gesetzesparameter.cs` — reine Code-Form ohne
  Designer/`.resx`, programmatisch per `InitGesetzeMenue()` aus dem `MDIMainForm`-Konstruktor
  unter **Administration** direkt nach „Einstellungen" eingehängt (`MDIMainForm.cs:39,50-75`);
  identisches Muster für die Lizenzverwaltung (`:291`).
- **Rückfrage-Muster für Auslieferungssätze:** `ADM_SCHUTZ_FRAGE`/`ADM_SCHUTZ_TITEL`
  (zweisprachig vorhanden) und `SchreibschutzUebergehen` im BHKW-Zweig; die geplante
  zentrale Wache und die Ausweitung der Bezeichner-Eindeutigkeit sind in
  [`KONTEXT_Stammdaten_Aenderbarkeit.md`](KONTEXT_Stammdaten_Aenderbarkeit.md) (P1, P4)
  beschrieben — dieses Konzept docken dort an, es entsteht **kein** zweiter,
  konkurrierender Prüfmechanismus.

### 2.4 Bestand in der Datenbank (gemessen)

Bestandsaufnahme vom 20.08.2026, rein lesend an einer Dateikopie der Produktiv-DB
(`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`, Stand 19.08.2026). Migrationsschritt 24
war auf dieser DB noch nicht gelaufen. Inhaltsvergleich über alle Fachspalten ohne
`ID`, `Bezeichner`, `ReadOnly`, `Beschreibung`:

| Tabelle | Sätze | Namensdubletten (Gruppen/Sätze) | Inhaltsdubletten (Gruppen/Sätze) | davon anderer Name |
|---|---|---|---|---|
| `Tab_WP_STAMM` | 51 | 0 | 4 / 8 | 4 |
| `Tab_Heizkessel_STAMM` | 20 | **8 / 16** | 2 / 4 | 0 |
| `Tab_Pufferspeicher_STAMM` | 4 | 0 | 0 | 0 |
| `Tab_Solarkollektoren_STAMM` | 7 | 0 | 1 / 2 | 1 |
| `Tab_PV_STAMM` | 11 | **5 / 10** | 5 / 10 | 0 |
| `Tab_BHKW_STAMM` | 79 | 0 | 1 / 2 | 1 |
| `Tab_Stromspeicher_STAMM` | 5 | 0 | 0 | 0 |

Lesarten und Auffälligkeiten:

- **PV ist praktisch doppelt vorhanden** (5 feldweise identische Volldubletten, 10 von
  11 Sätzen), **Heizkessel zu 80 % betroffen** (8 gleichnamige Paare in zwei
  lückenlosen ID-Blöcken 226–234/235–243 — eindeutig ein zweiter Importlauf). Beide
  Fälle räumt der nächste Lauf von Migrationsschritt 24 nach dessen Leerwert-Regel ab.
- **Beleg für die Überschreiben-Feldregel (4.2):** Bei 6 der 8 Heizkessel-Paare steht
  im jüngeren Block `Brennwert = FALSE` gegen `TRUE` im älteren — der Import befüllt
  `Brennwert` nicht (2.1), der Reimport hat das Kennzeichen also faktisch verloren.
- **Wärmepumpe:** keine Namensdubletten; von den 4 Inhaltsgruppen sind 2 Testdaten
  (`test3/5/6/7`), 1 ist eine echte Volldublette (`WP CHA-Monoblock 20/24` mit/ohne
  E-Heizelement — auch die Kennlinien sind identisch) und 1 löst sich über die
  Kennlinien auf. Generell gilt: 3 der 4 Gruppen haben **unterschiedliche Kennlinien**
  trotz identischem Kopfsatz — der Kennlinien-Hash (3.2) ist also notwendig, sonst
  produziert der Scan falsche Treffer.
- **Ein Teil der Inhaltsgruppen ist gewollt:** Solarkollektor `auroTHERM … H` vs. `… V`
  (Horizontal-/Vertikalvariante), BHKW `Dachs Gen2` deutsch/englisch. Inhaltsgleichheit
  ist ein **Hinweis**, kein Fehler — das bestimmt die Vorbelegung in 3.3.
- **Schreibweisen-Normalisierung findet heute null zusätzliche Gruppen** (kein
  Trim-/Groß-Klein-/Leerzeichen-Fall im ganzen Bestand). Sie bleibt trotzdem Teil der
  Prüfung — als billige Absicherung gegen künftige Quelldateien.
- **Nebenbefund `ReadOnly`:** Heizkessel, Pufferspeicher, Solarkollektoren, PV und
  Stromspeicher stehen **durchgängig auf FALSE** (nur BHKW 79/79 TRUE, WP 8/51 —
  darunter 2 Testsätze); die 1.960 WP-Kennlinienzeilen stehen alle auf FALSE, auch zu
  `ReadOnly`-Köpfen. Die Auslieferungs-Kennzeichnung ist damit in fünf Gewerken faktisch
  ungepflegt — relevant für die Migrations-Semantik aus
  `KONTEXT_Stammdaten_Aenderbarkeit.md` (4.1), dort zu adressieren; dieses Konzept
  hängt nicht davon ab.
- **Testdatensätze im Produktivkatalog:** `test3/5/6/7` (WP, 2 davon `ReadOnly=TRUE`),
  `Test` (Heizkessel ID 252), `test` (Solarkollektor ID 1) — Aufräumfall für die
  Admin-Suche (5), kein Automatikfall.
- `Tab_Kenndaten_STAMM` (1.960 Zeilen, 50 Wärmepumpen): 0 Waisen, 0 doppelte
  Stützstellen — referenziell sauber; 3 Paare identischer Kennlinienblöcke, 1 WP ohne
  Kennlinien (`test7`).

---

## 3. Begriffe und Prüfregeln

### 3.1 Namenskonflikt

Zwei Namen gelten als gleich, wenn sie **normalisiert** übereinstimmen:
führende/anhängende Leerzeichen entfernt, innere Mehrfach-Leerzeichen auf eines
reduziert, Groß-/Kleinschreibung ignoriert (Access vergleicht ohnehin case-insensitiv —
die C#-seitige Prüfung muss es explizit genauso tun). Keine weitergehende Unschärfe
(keine Ähnlichkeitsmaße) — die Prüfung bleibt deterministisch und erklärbar. Die
Messung (2.4) zeigt, dass die Normalisierung im heutigen Bestand keinen einzigen
Zusatztreffer erzeugt — sie ist reine Vorsorge und kostet nichts.

Geprüft wird gegen den Zielkatalog **und innerhalb der Importauswahl selbst** (eine
VDI-Datei kann denselben Typ mehrfach führen; die PAN-Liste kann durch den
`static`-Befund aus 2.2/6 Doppeleinträge zeigen).

### 3.2 Inhaltsdublette

Zwei Sätze gelten als inhaltsgleich, wenn alle **Vergleichsfelder** übereinstimmen.
Vergleichsfelder sind je Katalog definiert: alle fachlichen Kennwerte, **ohne** `ID`,
`Bezeichner`, `ReadOnly` und freie Text-/Beschreibungsfelder. Zahlen werden invariant
formatiert verglichen (kulturunabhängig, `float`-Rundung neutralisiert), Texte
getrimmt und case-insensitiv, `NULL` als eigener Marker. Der Vergleich ist **exakt,
ohne Toleranzband** (Entscheidung 9.1).

| Katalog | Vergleichsfelder (Basis: heutige Import-Feldlisten) |
|---|---|
| Wärmepumpe | `Firma, Typ, Baujahr, Aufstellung, Nennleistung, maxPtherm, Heizung, Regelung, Bauart, Kuehlleistung` **plus Kennlinien-Hash** über `Tab_Kenndaten_STAMM` (+ `Tab_Kenndaten_Kuehlung_STAMM`) je `ID_WP`, sortiert nach `Vorlauf, Temperatur` — zwei WP mit gleichen Stammfeldern, aber verschiedenen Kennlinien sind **keine** Dublette |
| Heizkessel | `Firma, Ptherm, Brennstoff, Wirkungsgrad_Gas, Wirkungsgrad_Öl, Raumbedarf, CO2, SO2, NOx, CO, Staub, Betriebsbereitschaftverlust` |
| Pufferspeicher | `Hersteller, Speichertyp, Bereitschaftsverluste, Gesamtvolumen` |
| Solarkollektoren | `Firma, Kollektortyp, Modulflaeche, Aperturflaeche, h0, k1, k2, Kdir, Kdfu, Vorlauf, Ruecklauf` |
| PV-Module | `Firma, Leistung, Wirkungsgrad, U_Mpp, U_Leerlauf, I_Mpp, I_Kurzschluss, alpha_SC, beta_OC, gamma_PMP, T_NOCT, Laenge, Breite` |
| Stromganglinie | `Zeitinterval` **plus Werte-Hash** über die zugehörigen `…Daten_STAMM`-Zeilen (8.760/35.040 Werte) |

Kostenfelder (`Modulkosten`, `Investitionskosten`, `Wartungskosten`, …) und
`Beschreibung` zählen **nicht** zum Vergleich — sie werden vom Anwender gepflegt und
unterscheiden sonst zwei fachlich identische Sätze künstlich.

### 3.3 Kombinationsfälle

| Befund | Einstufung | Vorbelegung im Dialog |
|---|---|---|
| Name neu, Inhalt neu | **Neu** | Importieren |
| Name vorhanden, Inhalt gleich | **bereits vorhanden (identisch)** | Auslassen |
| Name vorhanden, Inhalt abweichend | **Namenskonflikt** | Auslassen; Auflösung Überschreiben/Auslassen/Umbenennen |
| Name neu, Inhalt gleich wie Satz „X" | **Inhaltsdublette** (Hinweis auf „X") | **Importieren**, „Auslassen" wählbar |

Begründung der letzten Vorbelegung: Die Messung (2.4) zeigt, dass Inhaltsgleichheit
bei abweichendem Namen im Bestand überwiegend **gewollte Varianten** sind
(H-/V-Kollektor, Sprachfassungen, mit/ohne Zusatzheizung, wenn der Unterschied in
keinem Kennfeld abgebildet ist). Der Fall wird deshalb deutlich angezeigt, aber nicht
standardmäßig blockiert. Die Namensfälle bleiben konservativ vorbelegt (Auslassen),
denn dort ist der Reimport derselben Datei der Regelfall.

---

## 4. Soll-Verhalten beim Import

### 4.1 Ablauf (alle Importpfade einheitlich)

1. Anwender markiert Einträge und klickt „Übernehmen" (bzw. wählt die Datei beim
   Ganglinien-Import).
2. **Vorprüfung** der gesamten Auswahl gegen den Zielkatalog und gegen sich selbst —
   noch ohne Schreibzugriff.
3. Gibt es keinerlei Konflikte: Import läuft durch wie heute, Sammelmeldung am Ende.
4. Sonst erscheint **ein** Konfliktdialog für die ganze Auswahl (keine
   MessageBox-Kaskade je Satz — Regel aus `KONTEXT_Stammdaten_Aenderbarkeit.md`,
   Prüfliste 7, bleibt gewahrt):

```
┌─ Import: 12 Einträge, davon 4 mit Konflikt ───────────────────────────────┐
│ Eintrag (Name editierbar)   │ Befund                    │ Aktion          │
│ Vitocal 200-A A04           │ Neu                       │ [Importieren ▼] │
│ Vitocal 250-A A08           │ Name bereits vorhanden    │ [Auslassen   ▼] │
│                             │ (Inhalt abweichend)       │  Überschreiben  │
│                             │                           │  Umbenennen     │
│ Logatherm WLW196i           │ bereits vorhanden         │ [Auslassen   ▼] │
│                             │ (identisch)               │                 │
│ aroTHERM plus VWL 75/6      │ inhaltsgleich mit         │ [Importieren ▼] │
│                             │ „VWL 75/6 A S2"           │  Auslassen      │
│                    [Alle Konflikte auslassen]  [OK]  [Abbrechen]          │
└───────────────────────────────────────────────────────────────────────────┘
```

5. Ausführung der gewählten Aktionen, dann erweiterte Sammelmeldung
   (`n` importiert, `n` überschrieben, `n` umbenannt, `n` ausgelassen, `n` Fehler).

Der Einzelimport (CEC/PAN, Ganglinie) verwendet **denselben Dialog** mit einer Zeile —
ein Verhalten, ein Text, eine Pflegestelle.

### 4.2 Aktion „Überschreiben"

- **`UPDATE` auf den vorhandenen Satz über dessen `ID`** — kein Löschen + Neuanlegen.
  Die ID bleibt stehen (wichtig für `Tab_Kenndaten_STAMM.ID_WP` und dafür, dass
  `MAX(ID)+1`-Vergaben nicht kollidieren), der Name bleibt stehen, Projektverknüpfungen
  über den `Bezeichner` bleiben intakt.
- Aktualisiert werden **genau die Felder, die der jeweilige Import liefert**. Vom
  Anwender gepflegte Felder (Kosten, Beschreibung, beim Heizkessel auch
  `Wartungskosten_Einheit, Brennwert, Vorlauf, Ruecklauf` — der Import befüllt sie
  nicht) bleiben unangetastet. Dass diese Regel nötig ist, zeigt der Bestand: Der
  bisherige Doppelimport hat beim Heizkessel das `Brennwert`-Kennzeichen auf 6 von 8
  Paaren verloren (2.4) — mit einem Voll-`UPDATE` passierte dasselbe bei jedem
  Überschreiben wieder.
- Zur Kontrolle zeigt die Dialogzeile bei „Inhalt abweichend" die abweichenden Felder
  an (Kurzform in einer Spalte bzw. Tooltip) — der Anwender sieht vor dem
  Überschreiben, was sich ändern würde.
- Wärmepumpe: Kennlinien werden getauscht (Delete der Zeilen zu `ID_WP`, Insert der
  neuen) — zusammen mit dem Stammsatz-`UPDATE` **in einer Transaktion**.
- Trifft der Name im Katalog heute noch **mehrfach** (Altbestand vor Bereinigung),
  wird nicht überschrieben, sondern mit Verweis auf die Admin-Bereinigung abgebrochen
  (Muster `ADM_MEHRDEUTIG_TEXT`, vorhanden).
- **`ReadOnly`-Sätze:** Überschreiben ist erlaubt, aber mit dem Hinweis aus dem
  Stammdaten-Konzept (Auslieferungssatz; Änderung übersteht das nächste
  Datenbank-Update nicht — `KONTEXT_Stammdaten_Aenderbarkeit.md` 4.1). Technisch läuft
  das über dieselbe zentrale Wache (`StammSchreibschutz`, dort P1) bzw. bis zu deren
  Umsetzung über `SchreibschutzUebergehen`-Logik je Controller. Das `ReadOnly`-Flag
  selbst wird nicht verändert (Löschschutz bleibt).

### 4.3 Aktion „Umbenennen"

- Namensfeld in der Dialogzeile wird editierbar, Vorbelegung „`<Name> (2)`".
- Validierung beim Bestätigen: neuer Name normalisiert weder im Katalog noch in der
  übrigen Auswahl vorhanden, nicht leer. Solange verletzt: Zeile rot, OK gesperrt.

### 4.4 Ganglinien-Import

Gleiche Systematik: `Bezeichner` = Dateiname ohne Erweiterung; die heutige reine
ListBox-Prüfung wird durch die DB-Prüfung ersetzt (`GetStammId()` existiert bereits).
Überschreiben = Kopfsatz behalten, Datenzeilen in einer Transaktion tauschen.
Inhaltsvergleich (Werte-Hash) ist hier Stufe 2 (Paket D5) — der Namenskonflikt-Teil
kommt zuerst.

---

## 5. Soll-Verhalten: Dubletten-Suche im Admin-Menü

### 5.1 Einstieg und Aufbau

Neues Formular `Views\Admin\Form_KatalogDubletten` nach dem Muster
`Form_Gesetzesparameter` (reine Code-Form, Texte über `MyResource`, Einhängung per
`InitDublettenMenue()` im `MDIMainForm`-Konstruktor unter **Administration**, nach
„Einstellungen"; Eintrag in `HilfeKontext.BEREICH_JE_TYP` → `B_ADMIN` nicht vergessen).

Aufbau:
- Katalog-Auswahl (Wärmepumpe, Heizkessel, Pufferspeicher, Solarkollektoren, PV-Module,
  BHKW, Stromspeicher; „alle") + Knopf „Prüfen".
- Ergebnisliste, gruppiert: erst Namensdubletten (sollten nach Absicherung der
  Invariante nur Altbestand sein), dann Inhaltsdubletten (gleicher Inhalt, andere
  Namen). Je Gruppe: Sätze mit ID, Name, `ReadOnly`-Kennzeichen, abweichende Spalten.
- Detailbereich: Feld-für-Feld-Gegenüberstellung der Gruppe (nur abweichende Felder).

### 5.2 Aktionen

| Aktion | Regel |
|---|---|
| **Leere Kopien bereinigen** | wendet die vorhandene Schritt-24-Regel an (kleinste ID bleibt; Dublette nur löschbar, wenn alle abweichenden Spalten leer und nicht `ReadOnly`) — jetzt für **alle** Kataloge, bei WP mit Kaskade auf die Kennlinientabellen |
| **Satz löschen** | nur nach Einzelbestätigung; `ReadOnly`-Sätze bleiben gesperrt (die 24 bestehenden Löschsperren bleiben unangetastet); vorher **Verwendungsprüfung** (5.3) |
| **Satz umbenennen** | mit derselben Namensvalidierung wie 4.3; für `ReadOnly`-Sätze gesperrt (Begründung in `KONTEXT_Stammdaten_Aenderbarkeit.md` 4.3: die Migration fände den Satz nicht wieder) |
| **Protokoll** | Ergebnis und durchgeführte Aktionen als Textprotokoll speicherbar (Muster `Form_GanglinieProtokoll`) |

Kein Automatik-Modus darüber hinaus: Zusammenführen zweier **gefüllter** Sätze mit
abweichenden Werten bleibt bewusst Handarbeit des Anwenders (ansehen → entscheiden →
löschen oder behalten).

### 5.3 Verwendungsprüfung vor dem Löschen

Da Projekte Katalogeinträge über Textfelder referenzieren, prüft das Formular vor jedem
Löschen, ob der `Bezeichner` in Projektdaten verwendet wird (je Katalog hinterlegte
Prüfabfragen auf die zugehörigen `Z_*`-/Projekttabellen). Treffer ⇒ Löschen nur mit
deutlichem Hinweis, welche Projekte betroffen sind. Die konkreten Verwendungsstellen je
Gewerk werden bei der Umsetzung erhoben und in der Katalog-Registry (6.1) hinterlegt —
offener Punkt 9.3.

---

## 6. Technische Umsetzung

### 6.1 Zentrale Bausteine (neu)

```
Allgemein\Katalog\KatalogRegistry.cs   — je Katalog: Zieltabelle, Schlüsselspalte,
                                         Import-Feldliste, Vergleichsfelder,
                                         Detailtabellen (Kennlinien/Ganglinienwerte),
                                         Verwendungs-Prüfabfragen
Allgemein\Katalog\DublettenPruefung.cs — NormalisiereName(); PruefeAuswahl(katalog,
                                         kandidaten) → je Kandidat Befund (Neu /
                                         NameVorhanden / Identisch / Inhaltsgleich zu X);
                                         ScanKatalog(katalog) → Dublettengruppen;
                                         InhaltsHash(satz)
Views\Import\Form_ImportKonflikte.cs   — der Sammeldialog aus 4.1 (Code-Form)
Views\Admin\Form_KatalogDubletten.cs   — die Admin-Suche aus 5
```

Die Bereinigungsregel aus `SchemaMigration.KatalogBereinigen` wird in die zentrale
Klasse gezogen (oder von dort aufgerufen); `KATALOGE_MIT_NAMEN` geht in der
`KatalogRegistry` auf, damit Migration, Import und Admin-Suche **eine** Katalogliste
teilen. Die Migration bleibt idempotent und in ihrem Verhalten unverändert.

### 6.2 Integration in die sechs Importpfade

Die fünf heterogenen Namensprüfungen (2.1) werden durch den Aufruf der zentralen
Vorprüfung ersetzt; geprüft wird immer der Wert, der tatsächlich gespeichert wird
(Modellwert, nicht wechselnd Textbox/Listeneintrag). Die `RecordSet`-Stelle im
WP-Import entfällt dabei (Altbestand, String-SQL). `VdiUebernahmeErgebnis` wird um
`Ueberschrieben` und `Umbenannt` erweitert, `VdiAuswahlFilter.LadeMeldung` entsprechend.

Für „Überschreiben" fehlen teils ID-basierte Update-Wege: vorhanden bei Heizkessel und
PV; bei Wärmepumpe, Pufferspeicher und Solarkollektoren werden sie ergänzt (Muster
`PhotovoltaikStammCtrl.Update(int id)`), einschließlich Umbenenn-Sperre — das deckt
sich mit Paket P4 aus `KONTEXT_Stammdaten_Aenderbarkeit.md` und wird dort angerechnet,
nicht doppelt gebaut.

### 6.3 Konventionen und Fallstricke

- **Datenzugriff** ausschließlich `DataRepository` mit `?`-Parametern; kein `RecordSet`.
- **Drei-Schichten-Regel:** Tabellen-/Spaltennamen aus den vorhandenen
  `TABLE`-Konstanten der Controller bzw. `DbWerte`; alle neuen Anzeigetexte über
  `MyResource.Resource.*` in **beiden** Sprachdateien; Aktions-Schlüssel im Dialog
  sprachneutral (`UEBERSCHREIBEN`, `AUSLASSEN`, `UMBENENNEN`).
- **Neue Formulare als reine Code-Forms** (Muster `Form_Gesetzesparameter`),
  `TexteSetzen()` nach dem Aufbau; kein Designer-/`.resx`-Handeditieren.
- **Kodierung:** mehrere zu ändernde Bestandsdateien sind nicht UTF-8
  (u. a. `Form_WP_einlesen.cs`-Umfeld) — vorhandene Kodierung beibehalten.
- **Transaktionen:** Prüfung und Schreiben je Eintrag klammern (heute nur beim
  Heizkessel der Fall); WP-Überschreiben (Stammsatz + Kennlinien) zwingend atomar.
- **`KiSchreibschutz` bleibt unberührt:** Der KI-Assistent erhält durch die neuen
  Klassen keinen Schreibweg auf `_STAMM`-Tabellen.
- **`MAX(ID)+1`-Vergabe** bleibt in diesem Paket wie sie ist (nicht atomar, aber
  Bestand); das Konzept verschärft sie nicht und hängt nicht von ihr ab.

---

## 7. Absicherung der Invariante „ein Name, ein Satz"

Reihenfolge ist wichtig — erst Bestand säubern, dann zusperren:

1. **Migrationsschritt (neu, Schema-Version 25):** `KATALOGE_MIT_NAMEN` um
   `Tab_WP_STAMM` (mit Kennlinien-Kaskade), `Tab_Pufferspeicher_STAMM`,
   `Tab_Solarkollektoren_STAMM` erweitern — gleiche Löschregel wie Schritt 24
   (nur leere Kopien; Rest wird gemeldet). Nebenbefund: die Protokollzeile
   `SchemaMigration.cs:1325` nennt noch „Schritt 19" — bei der Gelegenheit korrigieren.
2. **Restdubletten** (gefüllte Kopien) löst der Anwender über die Admin-Suche (5) auf.
3. **Import und Pflegedialoge** verhindern ab diesem Paket jede neue Namensdublette
   (Import über den Konfliktdialog; Pflegedialoge: fehlende Namensprüfung bei der
   Wärmepumpe nachrüsten, fehlende Umbenenn-Sperren bei Pufferspeicher und
   Solarkollektoren — 6.2).
4. **Schlussstein (beschlossen, 9.4):** eindeutiger Index auf `Bezeichner` je
   Katalogtabelle — eigener Migrationsschritt, erst wenn 1–3 im Feld gelaufen sind
   (auf einem Bestand mit Restdubletten schlägt die Indexanlage fehl).

---

## 8. Pakete und Aufwand

| Paket | Inhalt | Abhängig von | Aufwand |
|---|---|---|---|
| **D1** | `KatalogRegistry` + `DublettenPruefung` (Normalisierung, Inhalts-Hash, Vorprüfung, Scan) | — | 5–7 h |
| **D2** | Konfliktdialog `Form_ImportKonflikte` + Integration in die 6 Importpfade, Überschreiben/Umbenennen, ID-Updates für WP/PSP/ST, erweiterte Sammelmeldung, Lokalisierung | D1 | 12–16 h |
| **D3** | Admin-Suche `Form_KatalogDubletten` (Scan, Gegenüberstellung, geführtes Bereinigen, Verwendungsprüfung, Protokoll, Menü-Einbindung) | D1 | 9–12 h |
| **D4** | Migration: Bereinigung auf alle Kataloge ausweiten (Version 25, WP-Kaskade), Protokolltext „Schritt 19" richtigstellen | D1 | 4–5 h |
| **D5** | Ganglinien-Inhaltsvergleich (Werte-Hash), PAN-`static`-Aufräumen, UNIQUE-Index als Schlussstein (7.4, beschlossen) | D1–D4 | 4–6 h |

Die Anwenderanforderung (1.1) ist mit **D1 + D2** erfüllt; die Admin-Prüfung (1.3) mit
**D3**. Gesamtrahmen 34–46 h.

---

## 9. Offene Entscheidungen

1. **Zahlenvergleich beim Inhalts-Hash** — **entschieden (20.08.2026): exakt.**
   Vergleich nach invarianter Formatierung, ohne Toleranzband. Begründung: gleiche
   Quelle liefert identische Werte; eine Toleranz meldete echte Modellvarianten als
   Verdacht.
2. **Überschreiben von `ReadOnly`-Sätzen:** wie in 4.2 vorgeschlagen erlauben mit
   Migrations-Hinweis — oder vorerst sperren, bis P1–P3 aus
   `KONTEXT_Stammdaten_Aenderbarkeit.md` umgesetzt sind?
3. **Verwendungsstellen je Gewerk** (5.3): bei Umsetzung von D3 erheben — welche
   `Z_*`-/Projekttabellen referenzieren welchen Katalog-`Bezeichner`.
4. **UNIQUE-Index** auf `Bezeichner` — **entschieden (20.08.2026): ja, als
   Schlussstein** (7.4): eigener Migrationsschritt, nachdem Bereinigung (7.1–7.2) und
   Import-Absicherung (7.3) im Feld gelaufen sind.
5. **Geltungsbereich Admin-Scan:** Vorschlag V1 = die fünf Herstellerkataloge + BHKW +
   Stromspeicher; Brauchwasser/Stromverbraucher/Prozesswärme (Kopf+Typprofile) und
   Ganglinien in einer zweiten Stufe.

---

## 10. Abnahme-Prüfliste (Auszug)

| # | Prüfung | Erwartung |
|---|---|---|
| 1 | VDI-Import, Auswahl ohne Konflikte | läuft wie bisher durch, eine Sammelmeldung |
| 2 | Import desselben Bestands ein zweites Mal | alle Einträge „identisch", Vorbelegung Auslassen, kein neuer Satz |
| 3 | Namenskonflikt → Überschreiben | Satz behält ID und Name, Import-Felder neu, nicht importierte Felder (Kosten, Beschreibung, Kessel-`Brennwert`) unverändert; WP: Kennlinien getauscht |
| 4 | Namenskonflikt → Umbenennen mit vergebenem Namen | OK gesperrt, bis der Name frei ist |
| 5 | Inhaltsdublette (anderer Name) → „trotzdem importieren" | Satz wird angelegt |
| 6 | Mehrfachimport mit 10 Konflikten | **ein** Dialog, keine MessageBox je Satz |
| 7 | Überschreiben eines `ReadOnly`-Satzes | Hinweis erscheint; `ReadOnly` bleibt TRUE; Löschen bleibt gesperrt |
| 8 | Admin-Suche auf Katalog mit Altdubletten | Gruppen korrekt; „leere Kopien bereinigen" löscht nur Leerkopien; `ReadOnly` nie gelöscht |
| 9 | Löschen eines in einem Projekt verwendeten Satzes | Warnung mit Projektnennung |
| 10 | Migration auf Bestands-DB | Schritt 25 idempotent; Kessel/PV-Verhalten von Schritt 24 unverändert |
| 11 | Sprachumschaltung Englisch | alle neuen Texte übersetzt |
| 12 | KI-Assistent | schreibt weiterhin auf keine `_STAMM`-Tabelle |

Vor Abnahmetests: `Kenndaten.laccdb` prüfen, datierte Kopie der `.accdb` nach
`DB-Backup\` (Regel aus `CLAUDE.md`).

---

## 11. Verwandte Dokumente

- [`KONTEXT_Stammdaten_Aenderbarkeit.md`](KONTEXT_Stammdaten_Aenderbarkeit.md) —
  ReadOnly-Semantik, zentrale Wache (P1), Bezeichner-Eindeutigkeit (P4),
  Migrations-Kollision
- `WindowsFormsApplication1\Allgemein\Update\SchemaMigration.cs` — Schritt 24
  „Katalogdubletten" (Begründung ab `:331`), `KATALOGE_MIT_NAMEN`
- `WindowsFormsApplication1\Allgemein\Update\Anlagenzeilen_Eindeutigkeit_Protokoll.md` —
  Schritt 17: dort **Umbenennen** statt Löschen, weil echte zweite Geräte
- [`CLAUDE.md`](CLAUDE.md) (Wurzel) — Textschlüssel-Konvention, Umgang mit der Datenbank
- `WindowsFormsApplication1\CLAUDE.md` — Drei-Schichten-Regel, Kodierungs-Fallstrick
