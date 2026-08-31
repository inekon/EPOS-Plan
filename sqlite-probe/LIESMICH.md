# Beispieldatenbank zum Erproben von SQLiteStudio

**Stand 31.08.2026** · gehört zu
[`Konzept_DB-Migration_SQLite_EPOS-Plan.md`](../../Konzept_DB-Migration_SQLite_EPOS-Plan.md) (Rev. 2)

| | |
|---|---|
| Datenbank | `EPOS_Beispiel.sqlite` (76 KB) |
| Aufbauskript | `aufbau.sql` — jederzeit reproduzierbar |
| Erzeugt mit | SQLite **3.44.4** |
| SQLiteStudio | liegt bereits unter `C:\Program Files (x86)\SQLiteStudio` |

**Kein Produktivdatenbestand.** Ein verkleinerter Nachbau des echten Modells (8 Tabellen,
5 Views, 4 Indizes, 36 Zeilen) mit erfundenen Werten. Er ist so gebaut, dass er genau die Punkte
zeigt, die im Konzept entschieden werden müssen — man kann sie hier anfassen, statt sie zu glauben.

Neu aufbauen, wenn beim Ausprobieren etwas kaputtgeht:

```bash
cd C:\Waermeplan\WP_Plan\dev\sqlite-probe && del EPOS_Beispiel.sqlite && "C:\Program Files (x86)\Android\android-sdk\platform-tools\sqlite3.exe" EPOS_Beispiel.sqlite ".read aufbau.sql"
```

---

## Öffnen

Die Oberfläche von SQLiteStudio ist **englisch** (es sind keine Übersetzungsdateien installiert) —
die Menüpunkte heißen deshalb so:

1. **Database → Add a database** (oder `Ctrl` + `O`)
2. Im Dialog: *Database type* = `SQLite 3`
3. *File*: über das Ordnersymbol
   `C:\Waermeplan\WP_Plan\dev\sqlite-probe\EPOS_Beispiel.sqlite` wählen
4. *Name (on the list)* füllt sich selbst; Haken bei *Permanent* stehen lassen, damit die
   Datenbank nach dem Neustart in der Liste bleibt → **OK**
5. In der Liste links **doppelklicken** — erst dann ist die Verbindung offen und der Baum
   klappt auf

**Vor Übung 3 prüfen:** SQLite hat Fremdschlüssel **standardmäßig aus**. Im SQL-Fenster
(`Alt` + `E` oder *Tools → Open SQL editor*) einmal absetzen:

```sql
PRAGMA foreign_keys;      -- 0 = aus, 1 = an
```

Steht dort `0`, gilt es je Sitzung mit `PRAGMA foreign_keys = ON;` einzuschalten. Dauerhaft geht
es über *Tools → Open Configuration Dialog → Database*.

---

## Was zuerst anschauen

1. **Struktur links im Baum** — 8 Tabellen, 5 Views. Bei `Tab_Gebaeude` auf *DDL* klicken:
   Dort stehen `STRICT`, die Umlautspalten und die `FOREIGN KEY … ON DELETE CASCADE`-Zeilen.
2. **Datenblatt** — Doppelklick auf `Tab_Projekt`, Reiter **Data**. Das ist der Ersatz für die
   Access-Datenblattansicht; Zellen sind direkt editierbar.
3. **Einzelsatz** — im Reiter *Data* oben auf die Formularansicht umschalten. Das kommt der
   Access-Einzelsatzansicht am nächsten.
4. **`Pruefung_Umgebung`** ausführen — zeigt, welche SQLite-Fassung SQLiteStudio selbst benutzt.
   Sie muss **≥ 3.37** sein, sonst versteht das Werkzeug keine `STRICT`-Tabellen.

---

## Sechs Übungen — je eine Konzeptentscheidung

### Übung 1 — Typtreue (`STRICT`, Konzept 5.1)

Im SQL-Fenster:

```sql
INSERT INTO Tab_Gebaeude (ID_Projekt, Gebaeudename, Wohnflaeche_gesamt)
VALUES (1, 'Fehlertest', 'viel');
```

**Erwartet:** `cannot store TEXT value in REAL column`. Genau das war der alte Einwand gegen
SQLite („schwache Typisierung") — mit `STRICT` ist er gegenstandslos.

### Übung 2 — Der Access-Boolean `-1` (Konzept 5.5)

```sql
INSERT INTO Tab_Heizkessel (Bezeichner, Leistung_kW, ReadOnly)
VALUES ('Test', 10.0, -1);
```

**Erwartet:** `CHECK constraint failed: ReadOnly IN (0,1)`.
Access speichert True als **−1**. Der `CHECK` ist damit das Sicherheitsnetz, das eine vergessene
`−1 → 1`-Wandlung im Migrator sofort auffliegen lässt, statt sie stillschweigend durchzulassen.

### Übung 3 — Fremdschlüssel sind standardmäßig AUS (Konzept 5.3)

```sql
PRAGMA foreign_keys = OFF;
INSERT INTO Tab_Gebaeude (ID_Projekt, Gebaeudename) VALUES (9999, 'Waise');   -- geht durch!
PRAGMA foreign_keys = ON;
INSERT INTO Tab_Gebaeude (ID_Projekt, Gebaeudename) VALUES (8888, 'Waise2');  -- scheitert
DELETE FROM Tab_Gebaeude WHERE ID_Projekt = 9999;                              -- aufräumen
```

**Erwartet:** Der erste `INSERT` legt eine Waise an, der zweite scheitert.
Das ist der wichtigste Fallstrick der ganzen Migration: Ohne `PRAGMA foreign_keys = ON` je
Verbindung sind alle 90 Beziehungen wirkungslose Dekoration. Im Programm gehört die Zeile an
genau eine Stelle — ins Öffnen der Verbindung in `DataRepository`.

### Übung 4 — Die Umlaut-Kollationsfalle (Konzept 11 / R1)

```sql
SELECT * FROM Pruefung_Kollation_Ergebnis;
```

**Erwartet:**

| Fall | BINARY | NOCASE | Bewertung |
|---|---|---|---|
| ASCII, andere Schreibung (`Erdgas` / `ERDGAS`) | 0 | **1** | ok |
| Umlaut, andere Schreibung (`Erdwärme` / `ERDWÄRME`) | 0 | **0** | **ABWEICHUNG zu Access** |
| Umlaut, nur ASCII anders (`Wärmepumpe` / `wärmepumpe`) | 0 | **1** | ok |
| Scharfes S (`Grünstraße` / `GRÜNSTRASSE`) | 0 | **0** | **ABWEICHUNG zu Access** |

SQLites `NOCASE` faltet **nur ASCII A–Z**. Solange sich die Werte lediglich in ASCII-Zeichen
unterscheiden, verhält es sich wie Access — sobald ein Umlaut die Schreibweise wechselt, nicht mehr.

Das ist relevant, weil in der echten Datenbank **14 von 30** Projektnamen und **10 von 25**
Kesselbezeichnern Umlaute tragen und beide Spalten Schlüssel sind. Die Übung zeigt zugleich, dass
der Fall seltener eintritt als befürchtet — deshalb steht im Konzept „erst messen, ob der Code
sich überhaupt darauf verlässt" (D5).

### Übung 5 — `IIf` → `CASE WHEN` (Konzept 3.3)

```sql
SELECT * FROM Abfrage_Energietraeger_Effektiv ORDER BY ID_Projekt, code;
```

Dies ist die **echte** Übersetzung einer der beiden Access-Abfragen, die `IIf` benutzen. Die
Testdaten decken alle drei Fälle ab: `custom_hi` ist NULL, ist 0, oder ist gesetzt — in den ersten
beiden Fällen muss der Katalogwert erscheinen, im dritten der eigene.

Zum Vergleich das Original in Access-SQL:

```sql
IIf(s.custom_hi Is Null Or s.custom_hi = 0, ec.hi_kwh_per_unit, s.custom_hi) AS eff_hi
```

### Übung 6 — Access-Schreibweisen laufen unverändert (Konzept 7.1)

```sql
SELECT * FROM Pruefung_EckigeKlammern;   -- [Feld] als Bezeichnergrenze
SELECT * FROM Abfrage_Projektgebaeude;   -- geklammerter Join: a JOIN (b JOIN c ON …) ON …
```

Beide Views laufen. SQLite akzeptiert `[eckige Klammern]` ausdrücklich zur
Access-Kompatibilität — der gesamte SQL-Bestand der Anwendung (`[Wirkungsgrad_Öl]`,
`[Bezeichner]`, `[user_edited]`) braucht deshalb **keine** Anpassung. Ebenso die geklammerte
Join-Schreibweise aus `Abfrage_Kostenfaktoren`.

---

## Wonach beim Erproben zu urteilen ist

Die eigentliche Frage ist nicht „läuft SQLite", sondern **„ersetzt das Werkzeug die
Access-Oberfläche für die tägliche Arbeit?"**. Beim Durchklicken lohnt der Blick auf:

| Access-Tätigkeit | in SQLiteStudio 3.4 (installiert) |
|---|---|
| Datenblatt ansehen/bearbeiten | Reiter **Data** — direkt vergleichbar |
| Einzelsatz bearbeiten | Formularansicht im Reiter *Data* |
| Abfrage zusammenklicken | **kein QBE-Entwurf** — nur SQL-Editor mit Autovervollständigung. Der deutlichste Rückschritt gegenüber Access |
| Tabelle entwerfen | Reiter **Structure**; erzeugt den nötigen Tabellenneubau selbst |
| Beziehungen als Bild | **fehlt in 3.4** — siehe Hinweis unten |
| Daten importieren/exportieren | *Tools → Import / Export*, CSV und mehr |

> **Hinweis (31.08.2026):** SQLiteStudio heißt seit dem Versionssprung **Letos** und steht in
> **4.0.3** (erschienen 12.08.2026, weiterhin GPL und kostenlos). Letos&nbsp;4 bringt einen
> **ERD-Editor** mit, der ein Diagramm aus einer bestehenden Datenbank erzeugt, es bearbeitet und
> Änderungen zurückschreibt — also genau das Access-Beziehungsfenster. Die hier installierte
> **3.4.21** hat das noch nicht. Wer nicht aufrüsten will, nimmt **DBeaver Community** fürs
> Diagramm (zeichnet aus den Fremdschlüsseln, ändert nichts).

Überzeugt der fehlende QBE-Entwurf nicht, ist der Rückweg im Konzept vorgesehen: Access als
**Frontend** über den sqliteodbc-Treiber weiterbenutzen und nur die Datenhaltung tauschen. In der
echten `Kenndaten.accdb` stehen noch vier verwaiste ODBC-Verknüpfungen auf eine SQLite-Datei — der
Weg wurde hier also schon einmal beschritten (Konzept 2.4).

**Hinweis zur Bitness:** Das installierte SQLiteStudio ist die **x86**-Fassung. Für einen Test mit
Access-Frontend muss der ODBC-Treiber zur Bitness von **Office** passen, nicht zu der von
SQLiteStudio.

---

## Inhalt der Datenbank

| Tabelle | Zeilen | zeigt |
|---|---|---|
| `Tab_Projekt` | 5 | Text-Schlüssel mit Umlauten, Boolean, ISO-Datum |
| `Tab_Gebaeude` | 5 | Umlaute **in Spaltennamen** (`k_Wert_Außenwand`), FK mit Kaskade |
| `Tab_Energieanlagen` | 5 | `Rücklauf`, Boolean, Wertebereich per `CHECK` |
| `Tab_Heizkessel` | 4 | trägt die bekannte **U+FFFD-Altlast** in Satz 3 |
| `energy_carrier` | 5 | Katalog für Übung 5 |
| `energy_project_settings` | 5 | `ID_Energieträger`, die drei `custom_hi`-Fälle |
| `Z_AnlageSenke` | 6 | Rangliste je Anlage, Kaskadenlöschung |
| `Pruefung_Kollation` | 6 | Testpaare für Übung 4 |

**Views:** `Abfrage_Energietraeger_Effektiv` · `Abfrage_Projektgebaeude` ·
`Pruefung_EckigeKlammern` · `Pruefung_Kollation_Ergebnis` · `Pruefung_Umgebung`

Der Ordner liegt unter `dev\` und ist damit von `.gitignore` erfasst — er landet in keinem Commit.
