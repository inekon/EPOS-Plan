# Betrieb: die SQLite-Datenbank von EPOS-Plan

**Stand:** 02.09.2026 · Arbeitspaket S8 des
[`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md)
(dort Abschnitt 8)

Ab dem Cutover hält EPOS-Plan seine Daten in **einer** SQLite-Datei. Access und die
ACE-Engine werden für den laufenden Betrieb nicht mehr gebraucht — nur noch für die
einmalige Übernahme eines Altbestands.

| | |
|---|---|
| Datenbank | `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` |
| Beidateien im Betrieb | `Kenndaten.sqlite-wal`, `Kenndaten.sqlite-shm` |
| Ordner änderbar über | Administration → Datenbankpfad (Einstellung `DBPath`) |
| Dateiname | Einstellung `DBName`, Vorgabe `Kenndaten.sqlite` |
| Journalmodus | **WAL**, dateipersistent (einmalig vom Migrator gesetzt) |
| je Verbindung | `PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;` |

---

## 1. Erststart auf einem Bestandsrechner

Findet EPOS-Plan beim Start **keine** `Kenndaten.sqlite`, aber daneben eine
`Kenndaten.accdb`, öffnet sich der Assistent **„Datenbankumstellung"**. Er nennt den
Ordner und den Ablauf und wartet auf „Jetzt umstellen"; mit „Beenden" passiert nichts
und der Altbestand bleibt unangetastet liegen.

Nach dem Start gibt es **kein Abbrechen mehr** — ein Abbruch mitten in der Übertragung
hinterließe eine halbe Zieldatei. Das ist Absicht; der Ablauf räumt bei jedem Fehler
selbst auf.

Drei Schritte, in dieser Reihenfolge:

1. **Alt-Hebung.** `Kenndaten.accdb` wird an Ort und Stelle auf den letzten
   Access-Schemastand **61** gebracht — genau das, was die Access-Fassung von EPOS-Plan
   bei jedem Start ohnehin tat. Protokoll: `migration_protokoll.txt` neben der
   Datenbank.
2. **Übertragung.** Alle 114 Tabellen wandern nach `Kenndaten.sqlite`. Jede Tabelle wird
   auf beiden Seiten gezählt **und** über eine Inhaltsprüfsumme verglichen; dazu laufen
   `PRAGMA integrity_check` und `PRAGMA foreign_key_check`. Daneben entsteht
   `Migrationsbericht_Kenndaten_<Datum>_<Uhrzeit>.md` mit allen Zahlen.
3. **Rückfallebene.** Erst nach nachgewiesenem Erfolg wird der Altbestand in
   **`Kenndaten.vor-sqlite.accdb`** umbenannt. Die Datei bleibt liegen: Sie ist die
   Rückfallebene und zugleich der Beleg, dass dieser Bestand umgestellt wurde.

Anschließend stellt die Anwendung die gespeicherte Einstellung `DBName` einmalig auf
`Kenndaten.sqlite` und startet normal weiter.

**Bei einem Fehler** wird die halbfertige `Kenndaten.sqlite` gelöscht, die `.accdb`
behält ihren Namen und bleibt gültig; die Meldung nennt den Grund und den Pfad des
Berichts. Der nächste Start bietet die Umstellung erneut an.

**Der Assistent läuft genau einmal je Bestand.** Liegt eine `Kenndaten.sqlite` da, gibt
es nichts zu tun. Und liegt bereits eine `Kenndaten.vor-sqlite.accdb` da, verweigert er
die Arbeit, statt die vorhandene Rückfallebene zu überschreiben.

> **Der Ordner muss beschreibbar sein.** Umbenennen und Anlegen passieren im
> Datenbankordner. Unter `C:\ProgramData` ist dafür die `icacls`-Zeile aus Abschnitt 4
> nötig.

---

## 2. Die drei Dateien — und warum man zwei davon nie einzeln anfasst

| Datei | Was drin steht |
|---|---|
| `Kenndaten.sqlite` | die Datenbank |
| `Kenndaten.sqlite-wal` | **Write-Ahead-Log**: alle Änderungen, die noch nicht in die Hauptdatei eingecheckpointet sind |
| `Kenndaten.sqlite-shm` | gemeinsamer Index in das WAL, den sich alle offenen Verbindungen teilen |

Solange EPOS-Plan läuft, ist der **aktuelle Datenstand die Summe aus `.sqlite` und
`-wal`**. Daraus folgt:

* **Niemals** nur die `.sqlite` kopieren, während die Anwendung läuft — die Kopie wäre
  auf dem Stand des letzten Checkpoints, alles danach fehlte.
* **Niemals** `-wal` oder `-shm` einzeln löschen, verschieben oder in eine Sicherung
  hineinkopieren. Ein `-wal` ohne die zugehörige `.sqlite` ist wertlos; eine `.sqlite`
  mit einem fremden `-wal` ist beschädigt.
* Beim ordentlichen **Beenden von EPOS-Plan** wird das WAL in die Hauptdatei
  eingecheckpointet; `-wal` und `-shm` verschwinden. **Sind sie weg, ist die `.sqlite`
  für sich vollständig.**

---

## 3. Sicherung

### 3.1 Anwendung geschlossen — Dateikopie genügt

Das ist der Normalfall und der einfachste Weg:

```bash
copy "C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite" "D:\Sicherung\Kenndaten_2026-09-02.sqlite"
```

Vorher prüfen, dass **kein** `Kenndaten.sqlite-wal` daneben liegt. Liegt doch eines da,
läuft die Anwendung noch (oder ist abgestürzt) — dann Abschnitt 3.2 nehmen.

### 3.2 Im laufenden Betrieb — `VACUUM INTO`

`VACUUM INTO` schreibt eine in sich geschlossene, defragmentierte Kopie, **ohne** die
laufende Anwendung zu stören und ohne WAL-Beidateien:

```sql
VACUUM INTO 'D:\Sicherung\Kenndaten_2026-09-02.sqlite';
```

Abzusetzen aus einem SQLite-Werkzeug (Abschnitt 5) oder von der Befehlszeile:

```bash
sqlite3.exe "C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite" "VACUUM INTO 'D:\Sicherung\Kenndaten_2026-09-02.sqlite';"
```

Das Ziel darf **nicht** schon existieren — SQLite überschreibt hier nichts.

### 3.3 Ablage

Das bisherige Verfahren mit dem Ordner `DB-Backup\` trägt unverändert. Was sich ändert:
Die alten **90-MB-`.accdb`-Stände gehören nicht mehr ins Repo**; eine `.sqlite` ist
kleiner (rund 65 MB gegenüber 145 MB), hat im Repo aber ebenso wenig zu suchen.
**Migrationsberichte dagegen schon** — sie liegen als Markdown unter `sql\` neben den
Arbeitspaket-Protokollen.

---

## 4. Zwei Windows-Konten auf einem Rechner

Zwei Konten können sich die Datenbank teilen; SQLite ist dafür eingerichtet:

* **WAL** erlaubt Lesen und Schreiben gleichzeitig — Leser blockieren den Schreiber
  nicht und umgekehrt.
* **`busy_timeout = 5000`** lässt eine Verbindung fünf Sekunden auf eine belegte
  Schreibsperre warten, statt sofort mit „database is locked" abzubrechen.

Was das **nicht** löst, ist die NTFS-Seite: Unter `C:\ProgramData` darf ein normaler
Benutzer eigene Dateien anlegen, fremde aber nur lesen. Genau daran scheiterte schon der
Access-Betrieb. Die einmalige Rechtevergabe aus
[`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md) **bleibt
nötig** — und wird mit WAL sogar wichtiger, weil jetzt zusätzlich `-wal` und `-shm`
angelegt und beschrieben werden müssen:

```bash
icacls "C:\ProgramData\EPOS_PLAN" /grant "*S-1-5-32-545:(OI)(CI)M" /T
```

`S-1-5-32-545` ist die sprachneutrale SID der Gruppe „Benutzer"; `(OI)(CI)` vererbt auf
künftige Dateien und erfasst damit auch die WAL-Beidateien. Gehört dauerhaft in den
Installer (Post-Install-Schritt mit erhöhten Rechten).

**Über ein Netzlaufwerk gehört die Datenbank nicht.** WAL braucht gemeinsamen
Arbeitsspeicher (`-shm`) und funktioniert auf SMB-Freigaben nicht zuverlässig.

---

## 5. Werkzeuge

| Werkzeug | Hinweis |
|---|---|
| **SQLiteStudio** | liegt bereits unter `C:\Program Files (x86)\SQLiteStudio`; kommt der Access-Datenblatt- und Einzelsatzansicht am nächsten |
| **DBeaver** | stärker bei ER-Diagramm und Datenexport; das ER-Fenster ist der Ersatz für das Access-Beziehungsfenster |
| **`sqlite3.exe`** | Befehlszeile, u. a. für `VACUUM INTO` |

**Mindestens SQLite 3.37** — darunter versteht das Werkzeug die `STRICT`-Tabellen des
Zielschemas nicht. `VACUUM INTO` gibt es ab 3.27.

Zum gefahrlosen Üben liegt eine kleine Beispieldatenbank samt Aufbauskript und
Anleitung unter [`sqlite-probe\`](sqlite-probe/LIESMICH.md) — 8 Tabellen, erfundene
Werte, jederzeit neu aufzubauen. **Kein Produktivdatenbestand.**

> Fremdschlüssel sind in SQLite je Sitzung **standardmäßig aus**. EPOS-Plan schaltet sie
> bei jeder Verbindung ein; ein Werkzeug tut das nicht von selbst. Wer mit einem Werkzeug
> löscht, prüft vorher `PRAGMA foreign_keys;` — steht dort `0`, greift kein einziger der
> 88 Fremdschlüssel.

---

## 6. SQL-Dialekt: Regeln für neuen Code

**Stand 03.09.2026.** Access und SQLite sprechen nicht dieselbe Sprache. Der Bestand ist
in Access aufgewachsen, und zwei Altlasten sind erst Wochen nach dem Cutover aufgefallen —
beide in Pfaden, die der Referenzlauf nicht berührt (`ucFuelSettings.GetProjectPrice`,
`KostenProjektPositionenCtrl`). Dieser Abschnitt hält fest, was beim Schreiben neuer
Anweisungen zu beachten ist, und wie man es prüft, statt es zu hoffen.

### 6.1 Die Umlautregel — die wichtigste, weil sie lautlos zuschlägt

SQLite vergleicht Bezeichner **ohne Rücksicht auf Groß- und Kleinschreibung, aber nur bei
ASCII-Buchstaben.** `id_energietraeger` findet `ID_Energietraeger`; `id_ENERGIETRÄGER`
findet `ID_Energieträger` **nicht** — das große `Ä` ist für SQLite ein anderer Buchstabe
als das kleine `ä`. Unter Access war die Schreibweise gleichgültig.

> **Regel:** Jeder Bezeichner mit Umlaut, `ß` oder sonstigem Nicht-ASCII wird
> **buchstabengetreu** so geschrieben, wie er im Schema steht. Im Zweifel nachsehen:
> `PRAGMA table_info("Tab_…");`

Das Schema führt heute **elf** solche Bezeichner — `ID_Energieträger`, `Rücklauf`,
`Wirkungsgrad_Öl`, `Flaeche_Außenwand`, `k_Wert_Außenwand` und je drei
`Abmessung_Anschluß_…`/`WBVK_Anschluß_…`. Im Quelltext stehen sie an 86 Stellen, allen
voran `ID_Energieträger` (43-mal). Der Prüfer aus Abschnitt 6.4 hält jede einzelne davon
gegen das Schema.

### 6.2 Verbotsliste — Access-Schreibweisen und ihre SQLite-Entsprechung

| Access | SQLite | Bemerkung |
|---|---|---|
| `UPDATE a INNER JOIN b ON … SET …` | `UPDATE a SET x = (SELECT … FROM b WHERE …) WHERE EXISTS (SELECT … )` | SQLite kennt kein JOIN im UPDATE; Meldung „near INNER: syntax error" |
| `DELETE a.* FROM a INNER JOIN b …` | `DELETE FROM a WHERE EXISTS (SELECT 1 FROM b WHERE …)` | dasselbe für DELETE |
| `IIf(b, x, y)` | `CASE WHEN b THEN x ELSE y END` | `IIF(b,x,y)` gibt es in SQLite seit 3.32 auch — dann ist es erlaubt |
| `Nz(x, y)` | `COALESCE(x, y)` | |
| `SELECT TOP 10 …` | `SELECT … LIMIT 10` | |
| `SELECT DISTINCTROW …` | `SELECT DISTINCT …` | |
| `#2026-01-31#` | `'2026-01-31'` | SQLite kennt kein Datumsliteral, nur Text/Zahl |
| `a & b` (Verkettung) | `a \|\| b` | **lautlos falsch:** `&` ist in SQLite das bitweise UND |
| `LIKE 'Haus*'` | `LIKE 'Haus%'` | **lautlos falsch:** `*` ist in SQLite ein normales Zeichen |
| `LIKE 'H?us'` | `LIKE 'H_us'` | dito für `?` |
| `Left(s,n)` / `Mid(s,p,n)` / `Right(s,n)` | `substr(s,1,n)` / `substr(s,p,n)` / `substr(s,-n)` | |
| `UCase(s)` / `LCase(s)` | `upper(s)` / `lower(s)` | |
| `IsNull(x)` | `x IS NULL` | Achtung: T-SQLs zweistelliges `ISNULL` ist wieder etwas anderes |
| `CDbl(x)` / `CInt(x)` / `CStr(x)` | `CAST(x AS REAL / INTEGER / TEXT)` | |
| `Val(s)` / `Str(n)` | `CAST(s AS REAL)` / `CAST(n AS TEXT)` | |
| `Int(x)` | `CAST(x AS INTEGER)` | |
| `Now()` / `Date()` | `datetime('now','localtime')` / `date('now','localtime')` | Access liefert einen Datumswert, SQLite Text |
| `Year(d)` / `Month(d)` | `strftime('%Y', d)` / `strftime('%m', d)` | Ergebnis ist **Text**, nicht Zahl |
| `DateAdd/DateDiff/DatePart` | `date(d, '+1 day')`, `julianday(a)-julianday(b)`, `strftime(…)` | |
| `Switch(…)` / `Choose(…)` | `CASE WHEN … END` | |
| `First(x)` / `Last(x)` | `min/max` mit `ORDER BY` + `LIMIT 1` | |
| `SELECT … INTO Neu FROM …` | `CREATE TABLE Neu AS SELECT … FROM …` | |
| `ALTER TABLE t ALTER COLUMN c …` | Tabelle neu anlegen und umkopieren | SQLite kann Spalten nur anfügen/umbenennen/löschen |
| `ALTER TABLE t ADD CONSTRAINT … FOREIGN KEY …` | Fremdschlüssel **ins `CREATE TABLE`** | SQLite hängt einer bestehenden Tabelle keinen an |
| `SELECT @@IDENTITY` | `last_insert_rowid()` **auf derselben Verbindung** | in EPOS-Plan: `ExecuteInsertAndGetId` |
| `Expr1000`, `Expr1001` … | Ausdrucksspalte mit `AS Name` benennen | Access vergibt diese Namen selbst, SQLite nicht |
| `TRANSFORM … PIVOT …` | von Hand mit `CASE WHEN`/`SUM` | Kreuztabellen gibt es nicht |

**Erlaubt und unverändert:** `[Tabelle].[Feld]` in eckigen Klammern, `<>` als
Ungleichheit, `INNER/LEFT JOIN` im `SELECT`, geklammerte Joins
(`FROM (a LEFT JOIN b ON …) LEFT JOIN c ON …`), `?` als Platzhalter.

### 6.3 `= True` / `= False`

SQLite kennt `TRUE` und `FALSE` seit 3.23 — aber nur als **Alias für 1 und 0**. Access
führte WAHR als **−1**. Das geht hier gut, weil die Migration jede Boolean-Spalte auf
0/1 normalisiert (geprüft: alle 96 Boolean-Spalten der Testdatenbank führen ausschließlich
0, 1 oder NULL). `WHERE Aktiv = TRUE` ist damit richtig. Wer eine **neue** Spalte anlegt,
gibt ihr `INTEGER NOT NULL DEFAULT 0 CHECK (spalte IN (0,1))` — dann bleibt das so.

Für die **Sortierung** nach einer Boolean-Spalte nicht `ORDER BY aktiv DESC` schreiben,
sondern die Absicht ausdrücken: `ORDER BY IIF(aktiv, 0, 1)` oder
`ORDER BY CASE WHEN aktiv THEN 0 ELSE 1 END`. Sonst hängt die Reihenfolge an der
Kodierung, und die hat sich mit der Umstellung geändert.

### 6.4 Der Prüfbefehl

Der Prüfer hält **jeden** SQL-Text des Quellbestands gegen eine echte SQLite-Datenbank
(nur lesend geöffnet) und gegen die Verbotsliste oben:

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
```

Rückgabewert 1, sobald eine Fundstelle bleibt; die CI (`.github/workflows/kern.yml`,
Schritt „SQL-Dialekt gegen SQLite", nur ubuntu) hängt daran. Nützliche Schalter:
`--alle` (auch die fehlerfreien Texte), `--dynamisch` (nur die Texte, deren Tabellen- oder
Spaltenname erst zur Laufzeit feststeht), `--csv DATEI`, `--selbsttest` (hält die Regeln
gegen 32 eingebaute Beispiele — der Beleg, dass der Prüfer etwas finden *würde*).
Einzelheiten in [`Werkzeuge\SqlDialektPruefer\LIESMICH.md`](Werkzeuge/SqlDialektPruefer/LIESMICH.md).

**Eine einzelne Anweisung von Hand prüfen** — ohne die Datenbank zu verändern:

```
sqlite3 -readonly C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite "EXPLAIN SELECT …;"
```

`EXPLAIN` bereitet die Anweisung nur vor und führt sie nicht aus. Sie durchläuft dabei
Syntax- **und** Objektprüfung: „near …: syntax error" und „no such column: …" fallen beide
hier auf, nicht erst beim Anwender.

### 6.5 Die Messlatte selbst — `Referenzlaeufe/Kenndaten_Test.sqlite`

**Stand 05.09.2026: Schemastand 64** (`Tab_Applikation.SchemaVersion`), 68 157 440 Byte
(65,0 MB). Nachzusehen ist er jederzeit:

```
sqlite3 -readonly Referenzlaeufe/Kenndaten_Test.sqlite "SELECT SchemaVersion FROM Tab_Applikation;"
```

**Warum das zählt.** Der Prüfer aus 6.4 hält jede Anweisung gegen **genau diese Datei**. Bleibt
sie hinter dem Quelltext zurück, meldet er „no such column" für Spalten, die es im Programm
längst gibt — nach der Zusammenführung der Rechner-2-Linie (Merge 5, Schritte 63/64 mit zehn
PV-Spalten) waren das neun Fundstellen auf einen Schlag. Die Kern-Tests decken das **nicht** auf:
`EPOS.Kern.Tests/TestDatenbank` zieht die Spalten auf ihrer Arbeitskopie nach und bleibt darum
grün. **Ein neuer Schemaschritt heißt deshalb: die Testdatenbank mitziehen.**

**Wie sie nachgezogen wird** — reproduzierbar, nie von Hand:

```
dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite
```

Das Werkzeug fährt **dieselben Quellen wie `SchemaMigration`** — den Spaltenkatalog
(`SchemaKatalog`), die Typübersetzung (`StilleDb.SqliteSpaltenTyp`) und für Schritt 62 die
`DELETE`-Texte aus `KlimaWaisenBereinigung` —, setzt den Marker auf `SchemaStand.Zielversion` und
verdichtet mit `VACUUM`. Es ist **idempotent** (vorhandene Spalte = nichts zu tun), läuft auf
Linux und kennt `--trocken` für den Blick vor dem Griff. Von Hand angelegte Spalten wären eine
zweite Schreibweise derselben Spalte — genau das, was die Typübersetzung verhindern soll.

> **Danach ist der Referenzlauf Pflicht, nicht Kür.** Eine Schemamigration darf keinen
> Rechenwert verschieben; belegt wird das, indem
> `EPOS.Referenzlauf lauf --projekte 1030,1007,1017` **vor und nach** dem Nachziehen läuft und
> `diff -r` über beide Zielordner nur `protokoll.txt` meldet (Zeitstempel, Zielordner,
> Dateigröße, Laufdauer). Jede abweichende CSV ist ein Befund.

Wächst die Datei über die Zeit, ist der Weg zurück
[`sql/tools/Reduziere-Testdatenbank.sql`](sql/tools/Reduziere-Testdatenbank.sql): Es schneidet
eine Kopie der produktiven Datenbank auf die dreizehn Referenzprojekte zurück (iE6, iF14). Das
Nachziehen des Schemas ersetzt es nicht — es setzt es voraus.

---

## 7. Kundenbestände außerhalb des Erststarts

Derselbe Migrationskern steckt auch in einem Konsolenwerkzeug. Es ist der Weg für
Bestände, die nicht am eigenen Rechner liegen — eingeschickte Datenbanken, Prüfläufe,
Wiederholungen:

```bash
EposSqliteMigrator.exe --ziel D:\Uebernahme\Kenndaten.sqlite ^
                       --quelle D:\Uebernahme\Kenndaten.accdb ^
                       [--orphanPolicy Abbruch|AlsProtokollAussetzen] ^
                       [--bericht D:\Uebernahme\Bericht.md]
```

Gebaut wird es aus `EposSqliteMigrator\` (eigene Projektmappe); das Ergebnis liegt unter
`EposSqliteMigrator\Konsole\bin\x64\Release\net8.0\win-x64\EposSqliteMigrator.exe`.

* `--quelle` ohne Angabe: `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`
* `--bericht` ohne Angabe: `Migrationsbericht_<Quellname>_<Zeit>.md` neben dem Ziel
* **`--orphanPolicy`** entscheidet über verwaiste Fremdschlüssel:
  * `Abbruch` (Vorgabe) — jede Verletzung beendet den Lauf, die Zieldatei wird gelöscht,
    die Liste steht im Bericht.
  * `AlsProtokollAussetzen` — die Zieldatei bleibt erhalten. **Der Fremdschlüssel bleibt
    dabei bestehen**; die vorhandene Verletzung wird ausgehalten und namentlich
    protokolliert. Nur mit Blick in den Bericht verwenden.

**Voraussetzungen und Zusicherungen**

* Die Quelle muss auf **Schemastand 61** stehen. Sonst bricht der Lauf ab mit dem
  Hinweis, zuerst die letzte Access-Fassung von EPOS-Plan zu starten. (Der
  Erststart-Assistent erledigt genau diese Hebung selbst.)
* Liegt eine `.laccdb` neben der Quelle, ist der Bestand geöffnet — der Lauf bricht ab.
  EPOS-Plan und Access schließen, auch auf anderen Rechnern.
* Die `.accdb` wird **ausschließlich gelesen** (nur `SELECT`, nach Möglichkeit sogar
  `Mode=Read`). Sie bleibt das Rollback.
* Eine bereits vorhandene Zieldatei wird **nie** überschrieben.
* Bei jedem Fehler löscht das Werkzeug die selbst angelegte Zieldatei.
* Es braucht die **64-Bit-ACE-Engine** (`Microsoft.ACE.OLEDB.12.0`) auf dem Rechner, auf
  dem es läuft.

**Rückgabewerte:** `0` Erfolg · `1` Fehler · `2` Quelle geöffnet (`.laccdb`) ·
`3` Fremdschlüsselverletzungen bei `orphanPolicy=Abbruch` · `4` Datenbeweis
fehlgeschlagen.

**Der Bericht ist die Abnahme**, nicht der Rückgabewert allein: Kopfdaten, Zeilenzahlen
und Prüfsummen je Tabelle, nicht migrierte Quelltabellen, Autowert-Stände,
`integrity_check`, `foreign_key_check` und der Case-Drift-Messlauf. Ein Lauf gilt als
sauber, wenn dort **„Datenbeweis bestanden"** steht.

**Cutover je Rechner getrennt.** Jeder Bestand bekommt seinen eigenen Migrationslauf und
seinen eigenen Bericht — eine an einem Rechner erzeugte `.sqlite` ist keine Vorlage für
einen anderen.

---

## 8. Wiederherstellung

**Eine Sicherung zurückholen** (EPOS-Plan vorher schließen):

1. Prüfen, dass im Datenbankordner **kein** `-wal`/`-shm` mehr liegt. Liegt doch etwas
   da, läuft noch eine Sitzung.
2. `Kenndaten.sqlite` **umbenennen** statt löschen (z. B. `Kenndaten.defekt.sqlite`) —
   solange nicht feststeht, dass die Sicherung trägt.
3. Die gesicherte Datei als `Kenndaten.sqlite` in den Ordner legen.
4. EPOS-Plan starten. Die Schemapflege bringt einen älteren Stand von selbst nach.

**Zurück auf Access** (nur solange keine Arbeit in der SQLite-Datei steckt, die nicht
verloren gehen darf): `Kenndaten.vor-sqlite.accdb` wieder in `Kenndaten.accdb`
umbenennen, `Kenndaten.sqlite` samt Beidateien wegräumen und die letzte Access-Fassung
von EPOS-Plan starten. **Alles, was seit der Umstellung in SQLite erfasst wurde, ist
damit weg** — es gibt keinen Rückweg von SQLite nach Access.

**Nach einem Absturz** liegt ein `-wal` neben der Datei. Nichts von Hand löschen: Der
nächste Start von EPOS-Plan (oder ein SQLite-Werkzeug) spielt es von selbst ein. Danach
`PRAGMA integrity_check;` absetzen — steht dort `ok`, ist die Datei in Ordnung.

---

## 9. Wo was steht

| Thema | Datei |
|---|---|
| Schema, Generator, Prüfrezepte, Arbeitspaket-Protokolle | [`sql\LIESMICH.md`](sql/LIESMICH.md) |
| Mehrbenutzerbetrieb, `icacls` | [`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md) |
| Installer-Hinweise | [`BETRIEB_Installer_Hinweise.md`](BETRIEB_Installer_Hinweise.md) |
| Gesamtkonzept der Umstellung | [`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md) |
| Beispieldatenbank zum Üben | [`sqlite-probe\LIESMICH.md`](sqlite-probe/LIESMICH.md) |
| SQL-Dialekt-Prüfer (Aufruf, Regeln, Ausnahmen) | [`Werkzeuge\SqlDialektPruefer\LIESMICH.md`](Werkzeuge/SqlDialektPruefer/LIESMICH.md) |
