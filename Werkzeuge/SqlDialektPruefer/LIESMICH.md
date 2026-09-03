# SQL-Dialekt-Prüfer

Hält **jeden SQL-Text des Quellbestands gegen SQLite** — Syntax, Objekte und die
Access-Eigenheiten, die SQLite klaglos annimmt und anders auslegt.

**Stand:** 03.09.2026 · reines Python 3 (nur Standardbibliothek), läuft auf Linux,
macOS und Windows.

---

## Warum es das gibt

Der Rechenkern ist durch den Referenzlauf abgesichert — 13 Projekte, 332 CSV, bei jedem
Push. **Dialog- und Pflegepfade deckt er nicht ab.** Genau dort saßen die zwei Altlasten,
die erst Wochen nach der SQLite-Umstellung beim Anwender auffielen:

| | |
|---|---|
| `c288e1c` | `ucFuelSettings.GetProjectPrice` verglich `id_ENERGIETRÄGER` gegen die Spalte `ID_Energieträger`. SQLite faltet Groß/Klein **nur bei ASCII** — das große `Ä` passt nicht zum kleinen `ä`. Meldung: „no such column: id_ENERGIETRÄGER" |
| `dd4113f` | `KostenProjektPositionenCtrl` führte `UPDATE … INNER JOIN … SET …` aus. Access-Syntax; SQLite kennt kein JOIN im UPDATE. Meldung: „near INNER: syntax error" |

Beide wären an einem einzigen Lauf dieses Werkzeugs aufgefallen. Es steht deshalb seit
dem 03.09.2026 als Schritt **„SQL-Dialekt gegen SQLite"** in `.github/workflows/kern.yml`
(nur ubuntu — das Werkzeug ist plattformfrei, ein zweiter Lauf auf macOS zeigte nichts
Neues und kostet Minutenkontingent).

---

## Aufruf

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
```

Die Datenbank wird **nur lesend** geöffnet (`file:…?mode=ro`) und nicht verändert; geprüft
wird ausschließlich mit `EXPLAIN`, das eine Anweisung vorbereitet, aber nicht ausführt.

| Schalter | Wirkung |
|---|---|
| *(ohne)* | nur die Fundstellen, dazu die Schlusszeile mit den Zahlen |
| `--alle` | auch die fehlerfreien und die dynamischen Texte |
| `--dynamisch` | nur die Texte, deren Tabellen- oder Spaltenname erst zur Laufzeit feststeht |
| `--csv DATEI` | die vollständige Liste (Art, Datei, Zeile, SQL, Befund) als CSV mit `;` |
| `--selbsttest` | prüft nur die Regeln gegen 32 eingebaute Beispiele |
| `--basis PFAD` | Wurzel des Arbeitsbaums, Vorgabe `.` |

**Rückgabewert 1**, sobald eine Fundstelle bleibt — daran hängt der CI-Schritt.

Der Lauf dauert rund eine Minute; die Testdatenbank ist der einzige Fremdteil und liegt
nicht in jedem Checkout (der CI-Schritt überspringt sich dann selbst).

---

## Wie geprüft wird

**1. Zusammensetzen.** Jede `.cs`-Datei der Bäume `EPOS.Kern` und
`WindowsFormsApplication1` wird in Token zerlegt. Die Zeichenketten einer Verkettung
werden wieder zusammengefügt — über `+`, über `sql += " …"`, über
`sql = sql + " …"` und über `sb.Append("…").Append(x).Append("…")`. Interpolierte
Zeichenketten (`$"…{x}…"`) und `string.Format`-Platzhalter (`{0}`) zählen als Lücke.

**2. Konstanten auflösen.** `const string`- und `static readonly string`-Werte werden
über den ganzen Baum eingesammelt und eingesetzt: `SchemaKatalog.TAB_*`, `SPALTE_*`, das
`TABLE` der Controller, `SchemaStand.SQL_*`. Ein **Kurzname zählt nur, wenn es ihn in
genau einer Klasse gibt** — sonst zöge `TABLE` die Tabelle einer fremden Klasse herein.
Die Vereinbarung selbst ist ein Baustein und wird nicht geprüft; geprüft wird jede
**Verwendung**, denn erst dort steht der ganze Satz
(`felder + "Tab_Pufferspeicher WHERE …"`).

**3. `EXPLAIN`.** Der fertige Text geht an die Testdatenbank. Das fängt Syntax **und**
Objekte: „near …: syntax error", „no such table/column: …". `?`-Platzhalter bleiben
stehen (die richtige Anzahl Bindungen wird nachgereicht), `@name` wird zu `?`.

**4. Lücken.** Bleibt eine Lücke, wird sie nacheinander mit `0`, mit einem Bezeichner und
mit nichts belegt. Besteht **eine** Belegung die Syntaxprüfung, liegt es nicht an der
Syntax — der Text zählt als **dynamisch** und erscheint nur unter `--dynamisch`. Nennt
SQLite dagegen einen Namen, der **wörtlich im Quelltext** steht, ist auch ein dynamischer
Text falsch (so fiel `Tab_WP … WHERE WPName = …` auf).

**5. Musterregeln**, unabhängig vom `EXPLAIN`, in zwei Klassen:

* **leise** — SQLite nimmt es klaglos an und tut etwas anderes als Access:
  `&` als Verkettung (in SQLite bitweises UND), `LIKE 'Haus*'` (in SQLite ein normales
  Sternchen). Diese Regeln melden **immer**.
* **laut** — SQLite bricht ab: `UPDATE … JOIN`, `Nz(`, `DISTINCTROW`, `TOP n`,
  `#Datum#`, `Left/Right/Mid(`, `UCase/LCase(`, `IsNull(`, `CDbl(`, `Val(`, `Str(`,
  `Int(`, `Switch/Choose(`, `First/Last(`, `Now()`, `Year(`, `DateAdd(`, `TRANSFORM`,
  `SELECT … INTO`, `ALTER COLUMN`, `ADD CONSTRAINT`, `@@IDENTITY`, `Expr1000`.
  Sie melden **nur dort, wo `EXPLAIN` nicht abschließend urteilen konnte** — sonst wären
  sie die zweite Meldung derselben Sache.

`= True` / `= False` schlägt nur an, wenn die verglichene Spalte in der Testdatenbank
etwas anderes als 0/1/NULL führt: SQLite kennt `TRUE` seit 3.23 als Alias von 1, Access
führte WAHR als −1. Alle 96 Boolean-Spalten der Testdatenbank führen 0/1 — die
20 Vergleiche im Bestand sind deshalb in Ordnung und werden nicht gemeldet.

**6. Umlaute.** Jeder Bezeichner mit Nicht-ASCII wird **buchstabengetreu** gegen das
Schema gehalten. Das ist die Regel aus `c288e1c`, jetzt automatisch.

---

## Was nicht geprüft wird

**Der Access-Zweig der Erststart-Migration** — dort ist Access-SQL richtig, und die
ACE-Engine führt es aus:

```
WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
WindowsFormsApplication1/Allgemein/Update/GeraeteWaisen.cs
WindowsFormsApplication1/Allgemein/Update/ErststartMigration.cs
WindowsFormsApplication1/Allgemein/Update/SchemaVersionAccess.cs
WindowsFormsApplication1/Allgemein/DbParamOleDb.cs
```

Die Liste steht als `AUSGENOMMEN` im Kopf des Skripts. Wer sie erweitert, schreibt dazu,
**warum** eine Datei Access sprechen darf.

**Die rund 150 dynamischen Texte** lassen sich nicht abschließend beurteilen, weil ihr
Tabellen- oder Spaltenname erst zur Laufzeit entsteht (`KomponentenUebernahmeCtrl`,
`ProjektExportImportCtrl`, `DublettenPruefung`, `AnlagePufferVerbundCtrl` …). Für sie sind
die Musterregeln das Netz: Ein `UPDATE … JOIN` fällt auch dann auf, wenn der Tabellenname
eine Lücke ist. `--dynamisch` listet sie auf; die Liste ist kurz genug, um sie bei einer
größeren Änderung einmal durchzusehen.

---

## Wenn der Prüfer rot wird

1. Zeile lesen: `FUND Datei:Zeile`, darunter der zusammengesetzte SQL-Text und der Befund.
2. **`MUSTER …`** → die Entsprechung steht in
   [`BETRIEB_SQLITE.md`](../../BETRIEB_SQLITE.md), Abschnitt 6.2.
3. **`UMLAUT x -> Schema schreibt y`** → Schreibweise aus dem Schema übernehmen.
4. **`SYNTAX …`** / **`OBJEKT …`** → die Meldung stammt wörtlich von SQLite. Zum
   Nachfassen die Anweisung von Hand vorbereiten:
   `sqlite3 -readonly Referenzlaeufe/Kenndaten_Test.sqlite "EXPLAIN …;"`
5. Erst danach an eine Ausnahme denken. Es gibt heute keine — jede Fundstelle ist
   entweder behoben oder als dynamisch eingestuft.

**Falschalarm?** Dann fehlt dem Werkzeug ein Stück Auflösung (eine Konstante, die es nicht
findet; eine Bauweise, die es nicht kennt). Das gehört im Werkzeug repariert, nicht mit
einer Ausnahme zugedeckt — und der `--selbsttest` bekommt ein Beispiel dafür.

---

## Selbsttest

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite --selbsttest
```

21 Anweisungen, die auffallen **müssen** (darunter beide Befunde vom 03.09.2026), gegen
11, die durchgehen müssen (`IIF(…)`, `COALESCE`, `substr`, `||`, `LIKE '…%'`, `LIMIT`,
`= True` auf einer 0/1-Spalte, ein Umlautbezeichner in richtiger Schreibweise).

Ein Prüfer, der nichts findet, ist erst dann eine gute Nachricht, wenn er belegen kann,
dass er etwas finden **würde**. Wer eine Regel hinzufügt, legt beide Beispiele mit dazu.
