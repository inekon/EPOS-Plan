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
eingesammelt und eingesetzt: `SchemaKatalog.TAB_*`, `SPALTE_*`, das `TABLE` der
Controller, `SchemaStand.SQL_*`. Ein **Kurzname zählt nur, wenn es ihn in genau einer
Klasse gibt** — sonst zöge `TABLE` die Tabelle einer fremden Klasse herein. Die
Vereinbarung selbst ist ein Baustein und wird nicht geprüft; geprüft wird jede
**Verwendung**, denn erst dort steht der ganze Satz
(`felder + "Tab_Pufferspeicher WHERE …"`).

> **Der Katalog kommt seit dem Befund #195 (11.09.2026) NUR aus dem Prüfbereich**
> (`dateien_im_bereich()`, also `EPOS.Kern` und `WindowsFormsApplication1`) — nicht mehr
> aus dem gesamten Arbeitsbaum. Eine Konstante eines Testprojekts (`EPOS.UI.Tests`,
> `Werkzeuge`, `Proben`, …) darf keinen Kurznamen im Produktcode auflösen. Genau das tat
> `EPOS.UI.Tests/UeberlagerungstitelTests.cs`: Sie legte mit `const string kind = "<div
> class=\"epos-dialog-kopf\"> …"` den ersten und einzigen Kurznamen `kind` im gesamten Baum
> an, und der Prüfer löste damit die Schleifenvariable `kind` aus
> `KomponentenUebernahmeCtrl.foreach (string kind in plan.Kindtabellen)` fälschlich darüber
> auf — Meldung „no such table: <div class=\"epos-dialog-kopf\"> …" an
> `KomponentenUebernahmeCtrl.cs:344`, obwohl `kind` dort zur Laufzeit einen echten
> Tabellennamen trägt (`Tab_Kenndaten`, `Tab_Kenndaten_Kuehlung`, …) und damit **dynamisch**
> ist, keine Fundstelle. Die Gegenprobe zum Merge von #187 steht im Protokoll zu #195: Mit
> der alten, unbeschränkten `alle_cs(basis)` UND der reparierten Namensauflösung (Absatz
> „Lokale Namen" unten) löst keine Konstante mehr etwas im Prüfbereich auf, das sie vorher
> nicht schon aufgelöst hätte — „in Ordnung"/„dynamisch" bleiben Zeile für Zeile gleich. Die
> Einschränkung auf den Prüfbereich ist damit reiner **Schutz gegen künftige Testfixtures**
> dieser Art, keine Korrektur eines zweiten Fundes.

> **Lokale Namen** (`_lokale_namen()`) sind eine gewöhnliche Variable (`string x = …`,
> `var x = …`), eine Deklaration ohne Zuweisung (`string x;`), eine
> `foreach`-Schleifenvariable (`foreach (string x in …)`), ein Methodenparameter
> (`string x`, auch mit `out`/`ref`/`in`/`params`/`this`) und — soweit einfach — Tupel- und
> Musterdeklarationen (`(string a, string b) = …`, `var (a, b) = …`, `x is string s`). Der
> Befund #195 war genau die `foreach`-Lücke: Bis dahin erfasste `_lokale_namen()` nur
> `string x = …`/`var x = …`, und `kind` blieb unerfasst — folgenlos, solange es im Bestand
> keinen zweiten Kurznamen `kind` gab.
>
> **Drei Stufen, in dieser Reihenfolge** (`_konstante()`): **(1)** die **eigene Klasse**
> geht immer vor — ein lokaler Name sperrt sie NICHT, denn „lokal" heißt hier nur „für DIESE
> eine Verwendungsstelle etwas anderes als die Konstante", nicht „nirgends in der Klasse eine
> Konstante". **(2)** Erst wenn die eigene Klasse keinen Treffer liefert, sperrt ein lokaler
> Name die Auflösung. **(3)** Sonst zählt der (eindeutige) Kurzname einer fremden Klasse. Das
> ist die Hausregel aus dem Bestandskommentar „Befund iU9-W6.7" wörtlich: Ein lokaler Name
> darf nicht über den Kurznamen einer FREMDEN Klasse auflösen — der Bezug innerhalb der
> EIGENEN ist davon nicht gemeint.
>
> **Nachbesserung zu #195 (11.09.2026):** Die erste Fassung des Befunds prüfte `lokal` VOR
> der eigenen Klasse und sperrte damit versehentlich auch echte Verwendungen der eigenen
> Konstante — sechs Fundstellen in `Z_ProjGebCtrl.cs`/`Z_ProjektGebGanglinieCtrl.cs`
> verschwanden dadurch aus der Prüfung, statt korrekt als „in Ordnung" durchzugehen (beide
> Klassen führen `const string sql = "SELECT …"` UND `ReadAll(string sql)` mit demselben
> Kurznamen). Mit der Dreistufenordnung kommen diese sechs zurück — **und mit ihnen 15
> weitere**, in vier zuvor nie geprüften Klassen (`StromspeicherSimCtrl.cs`,
> `Z_ProjektBrauchwasserCtrl.cs`, `Z_ProjektProzesswaermeCtrl.cs`,
> `Z_ProjektStromverbraucherCtrl.cs`): Sie führen dieselbe Bauart — eine `const string sql`
> in einer Methode, ein gewöhnliches `string sql = …` in einer anderen —, und die
> Lokal-zuerst-Ordnung hatte den eigenen Bezug dort schon VOR #195 stillschweigend
> unterdrückt (die schmale ursprüngliche `_lokale_namen()` erfasste `string sql = …` bereits).
> Die Zahl liegt damit bei **1342** SQL-Texten statt der zunächst erwarteten 1327 — 21 mehr
> als vor #195 (1327), nicht nur die sechs der ersten Fassung —, **0 Fundstellen bleiben**,
> „dynamisch" bleibt bei 215. Keine der 21 wiederhergestellten Stellen verliert dabei etwas:
> Der CSV-Vergleich gegen den Stand vor #195 zeigt **null** verschwundene, nur wiederhergestellte
> und neue Zeilen.

**3. `EXPLAIN`.** Der fertige Text geht an die Testdatenbank. Das fängt Syntax **und**
Objekte: „near …: syntax error", „no such table/column: …". `?`-Platzhalter bleiben
stehen (die richtige Anzahl Bindungen wird nachgereicht), `@name` wird zu `?`.

**4. Lücken.** Bleibt eine Lücke, wird sie nacheinander mit `0`, mit einem Bezeichner und
mit nichts belegt. Besteht **eine** Belegung die Syntaxprüfung, liegt es nicht an der
Syntax — der Text zählt als **dynamisch** und erscheint nur unter `--dynamisch`. Nennt
SQLite dagegen einen Namen, der **wörtlich im Quelltext** steht, ist auch ein dynamischer
Text falsch (so fiel `Tab_WP … WHERE WPName = …` auf).

> **Eine Ausnahme davon** (05.09.2026): eine `SELECT`-**Spaltenliste ohne jedes `FROM`**.
> Dort scheitert ohne Tabellenbezug *jeder* Spaltenname, auch der richtige — die Meldung
> sagt etwas über den Ausschnitt, nicht über den Quelltext. Der Fall entsteht, wenn der
> Rumpf in einer Schleife wächst und das `FROM` in einer **anderen** Anweisung dazukommt;
> einziger Vertreter im Bestand ist `WizardCtrl.FachspaltenSelect`. `_spaltenliste_ohne_tabelle`
> stuft solche Texte als *dynamisch* ein. Eng gehalten: nur bei fehlender **Spalte** und nur
> ohne `FROM` — mit `FROM`, bei fehlender **Tabelle** und bei `UPDATE`/`INSERT` bleibt es
> ein Fund.

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

**Der eingefrorene Access-Zweig der Schemapflege** — dort ist Access-SQL richtig, und
die ACE-Engine führt es aus. Seit W3 (#157‑E‑1, 09.09.2026) ist er ein **Hauswerkzeug**:
`ErststartMigration.cs` ist gelöscht, die Anwendung übernimmt keinen `.accdb`-Bestand
mehr; was bleibt, hebt einen Altbestand für `EposSqliteMigrator` auf Stand 61.

```
WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
WindowsFormsApplication1/Allgemein/Update/GeraeteWaisen.cs
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
`= True` auf einer 0/1-Spalte, ein Umlautbezeichner in richtiger Schreibweise) — dazu seit
dem Befund #195 (11.09.2026) **drei Tokenfälle ohne SQL-Text**: `_selbsttest_lokale_namen()`
prüft `tokenize()` + `_lokale_namen()` unmittelbar an einem synthetischen Quelltext nach dem
Muster von `KomponentenUebernahmeCtrl` (eine `foreach`-Variable `kind` darf ein gleichnamiger
Kurzname nicht auflösen), `_selbsttest_katalogbereich()` legt in einem Wegwerf-Verzeichnis
eine `.cs` unter `EPOS.Kern` neben eine unter `EPOS.UI.Tests` und prüft, dass
`dateien_im_bereich()`/`sammle_konstanten()` die Konstante der zweiten nicht in den Katalog
lässt, und `_selbsttest_eigene_klasse()` (Nachbesserung) hält die Dreistufenordnung von
`_konstante()` unmittelbar gegen einen konstruierten Katalog: ein gleichnamiger Parameter UND
eine klasseneigene Konstante lösen über die eigene Klasse auf, derselbe Parameter OHNE eigene
Konstante bleibt gegen den Kurznamen einer fremden Klasse gesperrt.

Ein Prüfer, der nichts findet, ist erst dann eine gute Nachricht, wenn er belegen kann,
dass er etwas finden **würde**. Wer eine Regel hinzufügt, legt beide Beispiele mit dazu.
