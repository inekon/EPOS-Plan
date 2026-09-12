# Prüfrezepte zur DB-Migration Access → SQLite

**Stand: 02.09.2026, nach Arbeitspaket S5 (Dialekt-Sweep) und der S7-Nacharbeit
(Rezepte 11 und 12 — beide sind erst durch den Referenzlauf entstanden, nicht durch
den Sweep).**
Messwerkzeuge: `sql/tools/sql_dialekt_inventur.py` und `sql/tools/typ_rueckweg_scan.py`.

Diese Datei ist die **Vollständigkeitsurkunde** des Dialekt-Sweeps: Je Rezept steht,
was nach S5 gelten muss (Sollwert), was gemessen wurde (Istwert) und welche
Fundstellen als **bewusste Ausnahme** stehen bleiben — mit Begründung.

---

## 0. Wie man die Rezepte ausführt

```
cd sql/tools
python sql_dialekt_inventur.py > befund_dialekt.txt
python typ_rueckweg_scan.py    > befund_typen.txt
```

Beide Skripte sind **rein lesend** und schreiben nichts in den Quellbaum
(`typ_rueckweg_scan.py` legt neben sich eine `typ_rueckweg_ergebnis.json` ab).
Unter Windows-Konsolen `PYTHONIOENCODING=utf-8` setzen.

**Warum nicht `grep`.** Beide Rezepte extrahieren zuerst mit einem C#-Lexer die
**echten String-Literale** (Kommentare, Char-Literale, Verbatim- und interpolierte
Strings korrekt behandelt) und messen erst darin. Ein `grep` über den Rohtext zählt
Kommentare und Dokumentation mit und liefert für fast jedes Rezept zu hohe Zahlen —
bei `@@IDENTITY` etwa 17 statt 0. Wer eine Zahl aus dieser Datei nachprüft, muss
deshalb die Skripte benutzen, nicht `grep`.

### Zwei stehende Ausnahmebereiche

| Bereich | Warum er ausgenommen ist |
|---|---|
| `Allgemein/Update/SchemaMigration.cs` (+ `SchemaKatalog.cs`) | **Eingefrorener Access-Zweig.** Diese Klasse liest die Alt-`.accdb` über ACE OLE DB und wird erst in **S6** abgelöst. Ihr SQL muss Access-Dialekt BLEIBEN — ein Umbau nach SQLite würde den Migrationspfad zerstören. |
| `Allgemein/Update/GeraeteWaisen.cs` | **Bewusster Zweiweg-Bau (S4b).** Zwei Aufruferkreise: der reguläre Programmpfad ruft ohne Verbindung auf (→ SQLite-Zugriffsschicht), `SchemaMigration` reicht seine **eigene, offene `OleDbConnection`** auf die Alt-`.accdb` herein. Der Unterschied steckt nur in den drei Zugriffsprimitiven (`Spalte`, `SpalteOhneTabelle`, `Loeschen`): `conn == null` → Zugriffsschicht, `conn != null` → auf dieser Verbindung wie bisher. |

---

## 1. `SELECT TOP n` → `LIMIT n`

| | |
|---|---|
| **Sollwert nach S5** | **0** ausführbare `SELECT TOP` außerhalb `SchemaMigration.cs` |
| **Istwert 02.09.2026** | **0** außerhalb / **3** in `SchemaMigration.cs` |
| **Rezept** | Abschnitt `d)` der Dialekt-Inventur |

**Bewusste Ausnahmen (3, alle S6):**

- `SchemaMigration.cs:5664` — `SELECT TOP 1 * FROM [<name>]` (Leseprobe: kann ACE die Abfrage ausführen?)
- `SchemaMigration.cs:13132` — `SELECT TOP 1 Pendelspeicher FROM Tab_Einstellungen WHERE …`
- `SchemaMigration.cs:13364` — `SELECT TOP 1 * FROM [<tabelle>]`

**Vier Fehltreffer, die das Rezept selbst ausweist** (englischer Berichtstext, kein SQL):
`BerichtTexte.cs:182,183` („T top mean/minimum") und `KennzahlenKatalog.cs:298,300`
(„Storage top temperature"). Das Rezept listet sie getrennt unter
„TOP ohne folgende Zahl" — sie dürfen nicht mitgezählt werden.

**Umbauregel:** `TOP n` hinter dem `SELECT` entfernen, `LIMIT n` ans **Statement-Ende**
(hinter ein etwaiges `ORDER BY`). Die Sortierung bleibt maßgeblich; die Semantik
„erste Zeile der Sortierung" ist identisch. In S5 sind so **26 Abfragen** in 13 Dateien
umgestellt und **einzeln gegen die Probendatenbank ausgeführt** worden (S5-Protokoll, Teil 1).

---

## 2. `SELECT @@IDENTITY`

| | |
|---|---|
| **Sollwert nach S5** | **0** ausführbare Stellen |
| **Istwert 02.09.2026** | **0** |
| **Rezept** | Abschnitt `e)` der Dialekt-Inventur |

Erledigt in S4: Der Rückgabewert kommt jetzt zentral aus
`DataRepository.ExecuteInsertAndGetId` bzw. `DbVorgang.EinfuegenUndId` — **auf derselben
Verbindung** wie das `INSERT`. Damit sind zugleich die beiden Altfehler behoben, die
`@@IDENTITY` auf einer **frischen** Verbindung lasen (`BrauchwasserCtrl`, `HeizkesselCtrl`).

**Achtung bei der Nachprüfung:** Ein `grep` findet weiterhin **17** Treffer — sämtlich
Kommentare und Markdown-Dokumentation, die den früheren Weg beschreiben
(„frueher SELECT @@IDENTITY …"). Nur die Inventur zählt richtig.

---

## 3. Boolean-Literale `= TRUE` / `= FALSE`

| | |
|---|---|
| **Sollwert nach S5** | **Kein Umbau.** SQLite kennt `TRUE`/`FALSE` seit 3.23 als Schlüsselwörter mit Wert 1/0; nach der Access-Wandlung −1 → 1 treffen sie den Bestand. Nachweis durch **Ausführung**. |
| **Istwert 02.09.2026** | 53 Treffer / 46 distinkte Zeilen: **24** `SchemaMigration` · **2** kein SQL · **20 Laufzeit-SQL-Zeilen** |
| **Rezept** | Abschnitt `f)` der Dialekt-Inventur |

**Die 2 Nicht-SQL-Treffer** (dürfen nicht mitgezählt werden):
`DataRepository.cs:191` und `:217` — `Foreign Keys=True` in der **Verbindungszeichenfolge**.

**Die 20 Laufzeitzeilen** (alle in S5 ausgeführt, Ergebnis im S5-Protokoll Teil 4b):

| Datei | Zeilen |
|---|---|
| `Allgemein/Simulation/Warnkriterien.cs` | 713 |
| `Controller/EmissionenCtrl.cs` | 641 |
| `Controller/EmissionskatalogCtrl.cs` | 67, 289 |
| `Controller/EnergieEinheitenPruefung.cs` | 492, 579, 635 |
| `Controller/KostenPositionCtrl.cs` | 170, 185, 201, 260 |
| `Controller/PufferSpCtrl.cs` | 1034, 1035 |
| `Controller/StromspeicherVarianteCtrl.cs` | 76, 234, 239 |
| `Views/Kosten/Form_KostenAdmin.cs` | 35, 119 |
| `Views/Kosten/Form_KostenfaktorItem.cs` | 29 |
| `Views/Kosten/ucFuelSettings.cs` | 888 |

**Schreibvarianten mitzählen:** Das Rezept erfasst `= TRUE`, `= FALSE`, `<> TRUE`,
`<> FALSE` **und** die Kleinschreibungen. Im Bestand stehen tatsächlich gemischte
Formen — `IsMainComponent = True` (KostenPositionCtrl), `IsMainComponent=false`
ohne Leerzeichen (Form_KostenfaktorItem:29). Beide laufen.

### 3b. `TRUE`/`FALSE` in **Wertposition** (INSERT … VALUES / SET)

Eine zweite, in der ursprünglichen Messung nicht getrennt geführte Gruppe:
30 distinkte Zeilen, davon 8 `SchemaMigration`, 10 kein SQL (C#-`true`/`false` in
JSON- und Textliteralen) → **12 Laufzeit-SQL-Zeilen** in
`EmissionenCtrl` (615, 618), `EmissionskatalogCtrl` (121, 318, 319, 415),
`EnergietraegerKatalogCtrl` (71), `KostenVorlagenCtrl` (248),
`KostenVorlagenUebernahmeCtrl` (480), `WPStammCtrl` (198),
`Views/BHKW/Form_DBBHKW.cs` (450), `ucFuelSettings` (911).

Belegt durch `SELECT TRUE, FALSE` → `(1, 0)`, `typeof` je `'integer'`:
Ein `INSERT … VALUES(TRUE)` schreibt also **1** und trifft damit genau die
0/1-Konvention des migrierten Bestands.

---

## 4. `LIKE`

| | |
|---|---|
| **Sollwert nach S5** | **Kein Umbau.** SQLite-`LIKE` ist per Vorgabe ASCII-case-insensitiv und kollationsunabhängig → verhaltensgleich zu ACE. Nachweis durch Ausführung. |
| **Istwert 02.09.2026** | 27 distinkte Zeilen: **3** `SchemaMigration` · **2** RowFilter (keine DB) · **22 Laufzeit-SQL** |
| **Rezept** | Abschnitt `g)` der Dialekt-Inventur |

**Die 2 RowFilter-Treffer** (`Views/Wärmepumpe/Kenndaten.cs:40, 47`) sind
**`DataTable.DefaultView.RowFilter`**-Ausdrücke — `Convert([{0}], 'System.String') LIKE …`.
Die wertet **ADO.NET im Speicher** aus, nicht die Datenbank; sie sind von der Migration
unberührt und dürfen im DB-Rezept nicht mitgezählt werden.

Von den 22 Laufzeitstellen sind 17 `Like '%'`-**Allesfilter** der Geräte-Dialoge
(BHKW, Heizkessel, PV, Pufferspeicher) und 5 die in S4 bereits nach SQLite gebauten
`sqlite_master`-Abfragen (`name NOT LIKE 'sqlite_%'`, in `ProjektDuplizierenCtrl` und
`ProjektExportImportCtrl`).

**Semantik-Nachweis:** `Like '%'` lässt NULL aus — unter SQLite wie unter ACE.
Gemessen: `Tab_Pufferspeicher_STAMM.Hersteller` 5 = 5 + 0 NULL,
`Tab_Heizkessel.Ptherm` 20 = 20 + 0 NULL. `PufferSpFilter.cs:47` behandelt NULL
ohnehin ausdrücklich (`Gesamtvolumen IS NULL OR Gesamtvolumen Like '%'`).

---

## 5. `IIf` und die übrigen Access-Funktionen

| | |
|---|---|
| **Sollwert nach S5** | `IIf`: **3** Laufzeitstellen, **unverändert**, Lauffähigkeit bewiesen. Alle **anderen** Access-Funktionen: **0** im Laufzeitcode. |
| **Istwert 02.09.2026** | `IIf` 17 gesamt = **14** `SchemaMigration` + **3** Laufzeit · `UCase` 3 und `Trim` 15 **nur** `SchemaMigration` · alle übrigen **0** |
| **Rezept** | Abschnitt `h)` der Dialekt-Inventur |

`iif()` gibt es in SQLite seit **3.32**. Die App bringt Microsoft.Data.Sqlite **8.0.11**
mit (e_sqlite3 ≈ 3.44) — die Funktion ist also vorhanden. Die drei Stellen sind in S5
einzeln ausgeführt worden (S5-Protokoll Teil 4a):

- `Allgemein/Simulation/Ladeordnung.cs:77` — `SqlAnlagenprio`
- `Controller/EmissionskatalogCtrl.cs:266` — `ORDER BY IIF(ist_aktiv, 0, 1), …`
- `Controller/ProjektDuplizierenCtrl.cs:740` — `IIF([col] > 0, [col] + <offset>, [col])` im `INSERT … SELECT`

**Auf 0 gemessen** (keine Fundstelle im ganzen Baum): `Nz`, `Format`, `Switch`, `Choose`,
`Val`, `Str`, `CStr`, `CInt`, `CDbl`, `CDate`, `CLng`, `CBool`, `DLookup`, `DCount`,
`DSum`, `DMax`, `DMin`, `DAvg`, `DateAdd`, `DateDiff`, `DatePart`, `DateSerial`,
`InStr`, `InStrRev`, `LCase`.

Ebenfalls **0**: `#Datum#`-Literale, `&`-Verkettung in SQL, `DISTINCTROW`, `TRANSFORM`,
`PIVOT`, `PARAMETERS` (Abschnitt `i)`).

---

## 6. `?`-Platzhalter

| | |
|---|---|
| **Sollwert nach S5** | **Bleibt.** Die Zugriffsschicht übersetzt `?` → `@pN` in Reihenfolge. |
| **Istwert 02.09.2026** | 1582 Quelltextzeilen mit `?` in einem Literal, davon 1112 SQL-verdächtig |
| **Rezept** | Abschnitte `a)`/`b)` der Dialekt-Inventur |

Die Übersetzung ist durch **Probe 1** der Probensuite `Proben/ZugriffsschichtProben`
laufend abgesichert („`?`→`@pN`-Uebersetzung").

Abschnitt `b)` sucht die **Falle**, dass ein `?` **innerhalb eines SQL-Texthochkommas**
steht und dann fälschlich als Platzhalter gezählt würde. Istwert: **2** Treffer, beide
harmlos — deutsche Rückfragetexte in `Form_KostenAdmin.cs:112` und
`Form_Variantentest.cs:281` („… wirklich löschen?"), kein SQL.

---

## 7. OleDb-Restnutzung

| | |
|---|---|
| **Sollwert nach S5** | Keine **Ausführung** mehr über OleDb außerhalb der Ausnahmebereiche. `OleDbParameter`/`OleDbCommand` dürfen als **reine Datenträger** bleiben. |
| **Istwert 02.09.2026** | 16 Dateien nennen `OleDbConnection`/`Command`/`DataAdapter`/`DataReader`; **ausführend** nur `SchemaMigration.cs` und `GeraeteWaisen.cs` |

**Warum die Datenträger bleiben.** `OleDbParameter` ist in 141 Dateien der Transporttyp
der Aufrufe — er wird an der Zugriffsschicht in `SqliteParameter` übersetzt. Ihn zu
ersetzen hieße, 2710 Aufrufstellen anzufassen; der Nutzen wäre null. Ebenso trägt
`RecordSet` (und je ein Controller in BHKW, BHKW-Stamm, PV, Pufferspeicher,
Solarkollektoren) ein Feld `public OleDbCommand DBCommand`, das **nie eine Verbindung
bekommt**: Es sammelt nur Parameter, die dann als `OleDbParameter[]` an die
Zugriffsschicht gehen (`RecordSet.cs:67-69`). Das ist kein Rest, sondern der bewusste
S4-Bau.

Aufräumen der Datenträgertypen wäre ein eigener, rein kosmetischer Schnitt — **nicht** Teil von S5.

---

## 8. Harte Casts auf DB-Werte (Typ-Rückweg)

| | |
|---|---|
| **Sollwert nach S5** | **0** harte `(int)`- und `(bool)`-Casts auf DB-Werte |
| **Istwert 02.09.2026** | **0** `(int)`, **0** `(bool)` · verbleibend 8 × `(double)`, 5 × `(string)` |
| **Rezept** | Abschnitt `A)` der Typ-Rückweg-Vermessung |

In S5 umgestellt auf `Convert.ToInt32` / `Convert.ToBoolean`:
`Controller/GebäudeKontextMenuCtrl.cs` (ID, ID_ProjektGebaeude, dezWarmwasserbereitung)
und `Views/Wärmepumpe/Kenndaten.cs` (`dr[1]`).

**Die 13 verbleibenden Casts sind kein Migrationsrisiko:**

- **8 × `(double)`** auf REAL-Spalten (`ProfilBedarf` 426/552, `GebäudeKontextMenuCtrl`
  110/112, `Form_Kosten_Auswahl` 74/75, `Form_Kosten_VarAuswahl` 85/86). SQLite REAL →
  `double`, wie ACE Double → `double`. Das Rezept markiert sie selbst als
  „ok (REAL→Double)".
- **5 × `(string)`** auf TEXT-Spalten (`GebäudeKontextMenuCtrl` 109, 111, 114, 115, 116).
  Verhaltensgleich: Ein `(string)`-Cast auf `DBNull` warf unter ACE genauso wie unter
  SQLite. Die Migration ändert daran nichts — Umbau wäre eine eigenständige
  Robustheitsverbesserung, kein Dialektthema.

**Zentrale Absicherung (D9).** Der eigentliche Grund, warum der Rückweg trägt, ist die
Typangleichung in `DataRepository.LadeTabelle`: INTEGER → `Int32`, Boolean- und
Datumsspalten über den generierten `SchemaTypKatalog` zurück auf `bool` bzw. `DateTime`.
Sie greift auch bei **leerem Ergebnis** (`WHERE 1 = 0`), weil sie den **deklarierten**
Spaltentyp auswertet (`TypAusDeklaration`) und nicht den beobachteten Wert. Deshalb
liefert `ProjektExportImportCtrl.ZielTypen` korrekte .NET-Zieltypen.

**Sichere Konsumenten als Gegenzahl:** 1363 `Convert.ToXxx(`-Aufrufe, davon 652 DB-nah.
`LINQ .Field<T>()`: 0 Fundstellen.

---

## 9. Kodierungsfalle (Pflichtprüfung vor jeder Dateiänderung)

Kein Dialektrezept, aber die Bedingung dafür, dass ein Sweep nichts zerstört.

Der Baum ist **gemischt kodiert**: 502 Dateien UTF-8 (mit oder ohne BOM), **68 Dateien
cp1252 ohne BOM**. Ein Editor, der eine BOM-lose cp1252-Datei als UTF-8 liest und
zurückschreibt, zerstört jeden Umlaut darin.

**Vorgehen je Datei, vor der ersten Änderung:**

1. BOM (`EF BB BF`) vorhanden → normal editierbar.
2. Sonst `utf-8`-**strict**-Probe: gelingt sie → UTF-8 ohne BOM, normal editierbar.
3. Schlägt sie fehl → **cp1252**: **nur byte-treu** über Python/latin-1 bearbeiten.
4. Danach **Umlaut-Stichprobe**: Anzahl Nicht-ASCII-Bytes und CRLF-Zahl vor/nach
   vergleichen — sie müssen gleich bleiben, solange nur ASCII geändert wurde.

In S5 war **keine** der 21 berührten Dateien cp1252; alle Änderungen sind zusätzlich
auf Byte-Ebene gegengerechnet worden (S5-Protokoll, Abschnitt „Kodierungsbefunde").

---

## 10. Endstand-Übersicht S5

| Rezept | Soll | Ist | Status |
|---|---|---|---|
| `SELECT TOP` außerhalb SchemaMigration | 0 | **0** | erfüllt |
| `SELECT TOP` in SchemaMigration (S6) | 3 | **3** | bewusste Ausnahme |
| `@@IDENTITY` ausführbar | 0 | **0** | erfüllt |
| Boolean-Vergleiche Laufzeit | laufen | **20/20 ausgeführt** | erfüllt |
| Boolean-Wertposition Laufzeit | schreibt 1/0 | **12 Zeilen, Beweis `TRUE`→1** | erfüllt |
| `LIKE` Laufzeit | verhaltensgleich | **22, 9 Muster ausgeführt** | erfüllt |
| `IIf` Laufzeit | 3, lauffähig | **3/3 ausgeführt** | erfüllt |
| übrige Access-Funktionen Laufzeit | 0 | **0** | erfüllt |
| harte `(int)`/`(bool)`-Casts | 0 | **0** | erfüllt |
| OleDb **ausführend** außerhalb Ausnahmen | 0 | **0** | erfüllt |
| Probensuite `ZugriffsschichtProben` | 12/12 | **12/12** | erfüllt |

---

## 11. `DELETE <Spalte> FROM …` (Jet-Idiom) — Nachtrag S7, Befund B2

| | |
|---|---|
| **Sollwert** | **0** Fundstellen außerhalb der beiden Ausnahmebereiche |
| **Istwert 02.09.2026 (nach Fix)** | **0** — Vorkommen gesamt 0, distinkte Zeilen 0 |
| **Rezept** | Abschnitt `k)` der Dialekt-Inventur |

`DELETE <feld> FROM <tabelle> WHERE …` ist Jet-Syntax. ACE **verwirft den Feldnamen
stillschweigend** und löscht ganze Zeilen; SQLite bricht mit
`SQLite Error 1: 'near "<feld>": syntax error'` ab.

**Regex über SQL-Literale** (nicht über den Rohtext — sonst zählen Kommentare mit, die
das alte Idiom beschreiben):

```
\bDELETE\s+(?!FROM\b)[A-Za-z_]\w*\s+FROM\b
```

Das `(?!FROM\b)` ist der ganze Trick: ohne die Negativ-Vorschau trifft das Muster auch
das **korrekte** `DELETE FROM Tab_X`. Gegenprobe beim Einbau gefahren — das Muster fängt
`DELETE ID_Projekt FROM …` und lässt `DELETE FROM …` sowie
`DELETE FROM … WHERE … NOT IN (SELECT …)` unberührt.

**Die eine behobene Fundstelle:** `Controller/ErgebnisCtrl.cs:150` (jetzt :156).
Sie stand **innerhalb der Transaktion** von `ErgebnisCtrl.Save`, riss sie in den
Rollback, `Save` lieferte `-1`, der Ergebnisexport brach ab → **0 CSV**. Dieselbe Datei
benutzte 90 Zeilen weiter oben bereits die richtige Form.

**Warum S5 das nicht gefunden hat:** Das Muster stand nicht auf der Rezeptliste. Es ist
auch keines der klassischen Dialektthemen (kein `TOP`, kein `@@IDENTITY`, keine
Access-Funktion), sondern eine **Syntaxvariante des DELETE selbst**. Gefunden hat es
erst der Referenzlauf (S7) — und zwar nur, weil er den **Schreibpfad** befährt: eine
reine Leseprobe hätte es nie ausgelöst.

---

## 12. Tabellenqualifizierte Spalten an einer **Sicht** — Nachtrag S7, Befund B1

| | |
|---|---|
| **Sollwert** | **0** Literale, die aus einer `Abfrage_*`-Sicht lesen und darin einen `Tab_*.`/`Z_*.`-Qualifizierer führen |
| **Istwert 02.09.2026 (nach Fix)** | **0** — vorher **4** |
| **Rezept** | Abschnitt `l)` der Dialekt-Inventur |

Eine Sicht hat in SQLite **nur ihre eigenen Ausgabespalten**. Wer eine Spalte über den
Namen der zugrunde liegenden **Tabelle** anspricht, bekommt
`SQLite Error 1: 'no such column: Tab_Waermebedarf.ID'`. Jet löst das auf, weil es die
gespeicherte Abfrage beim Ausführen aufmacht.

**Messung:** Ein Literal ist verdächtig, wenn es **beides** enthält —
`FROM [Abfrage_<x>]` und irgendwo einen `Tab_<y>.`- oder `Z_<y>.`-Qualifizierer:

```
(?i)\bFROM\s+\[?(Abfrage_[A-Za-z0-9_]+)\]?      und      \b(Tab_[A-Za-z0-9_]+|Z_[A-Za-z0-9_]+)\s*\.
```

**Die vier behobenen Stellen** (alle am 02.09.2026 umgestellt):

| Datei | vorher | nachher |
|---|---|---|
| `Allgemein/Simulation/SimulationWaermebedarf.cs:305` | `… where Tab_Waermebedarf.ID=… order by Tab_WaermebedarfDaten.ID` | `… where ID=… order by ID_Daten` |
| `Allgemein/Simulation/SimulationWaermebedarf.cs:602` | `… and Tab_DBTagV.ID=…` | `… and ID=…` |
| `Allgemein/Simulation/SimulationStrombedarf.cs:121` | `… where Tab_Stromganglinie.ID=… order by Tab_StromganglinieDaten.ID` | `… where ID=… order by ID_Daten` |
| `Allgemein/StromTestClass.cs:48` | `… where Tab_Stromganglinie.ID=… order by ID` | `… where ID=… order by ID_Daten` |

**Die zweite Hälfte des Befunds — Doppelnamen.** Drei der 14 Sichten selektierten die
`ID` **beider** verbundener Tabellen; SQLite entdoppelte das zu `ID` und **`ID:1`**.
Der zweite Name ist für Konsumenten unbrauchbar. Deshalb heißt die zweite ID in
`sql/schema/002_views.sql` jetzt **`ID_Daten`** — in
`Abfrage_ProjektGebaeudeGanglinie`, `Abfrage_ProjektStromGanglinie` und
`Abfrage_Tagverteilung`. Alle übrigen Ausgabespalten behalten ihren Namen.
**Prüfgriff an der migrierten Datei:** kein Sichtspaltenname darf einen Doppelpunkt
tragen —

```sql
-- je Sicht: PRAGMA table_info(<sicht>);  kein Spaltenname darf ':' enthalten
```

gemessen an `Kenndaten_S7_v2.sqlite`: **14 Sichten, 0 Doppelnamen**.

**Wie der Referenzlauf sie fängt.** Beide Schadensbilder sind nur im Lauf sichtbar, und
sie sind ungleich gefährlich:

- **laut:** die Stromganglinie meldet „Ganglinie … hat 0 Werte … bitte neu einlesen"
  und bricht das Projekt ab — das fällt sofort auf.
- **still:** die Tagesverteilung liefert nur eine Warnung („Zum Tagesverteilungstyp …
  sind keine Daten hinterlegt") und rechnet weiter. **Das Ergebnis sieht vollständig
  aus und ist falsch.** Nur der Wertevergleich gegen die Access-Seite deckt das auf.

Daraus die Regel für künftige Pakete: **Ein Dialekt-Sweep über den Quelltext ersetzt
den Referenzlauf nicht.** Die statische Messung findet, was sie kennt; der Lauf findet,
was rechnet. Rezepte 11 und 12 sind beide erst nach dem Lauf entstanden — sie sichern
den Befund für die Zukunft ab, hätten ihn aber nicht selbst gefunden.

---

---

## 13. Endstand-Nachtrag S7

Die beiden Rezepte oben sind nach dem Fix gemessen worden; dazu die Laufbelege, die
sie abstützen.

| Rezept | Soll | Ist 02.09.2026 | Status |
|---|---|---|---|
| `DELETE <Spalte> FROM` (Abschnitt `k)`) | 0 | **0** | erfüllt |
| qualifizierte Sichtspalten (Abschnitt `l)`) | 0 | **0** (vorher 4) | erfüllt |
| Doppelnamen `ID:1` in Sichten (`PRAGMA table_info`) | 0 | **0** von 14 Sichten | erfüllt |
| Probensuite `ZugriffsschichtProben` | 15/15 | **15/15** | erfüllt |
| Referenzlauf B (SQLite), 10 Projekte | 10/10 | **10/10, Exit 0** | erfüllt |
| Wertevergleich A↔B ohne Toleranz | 0 Abweichungen | **0** (234/234 Dateien byte-identisch) | erfüllt |

Belege: `sql/S7_Protokoll_2026-09-02.md`, Abschnitt 10 (Nacharbeit).
