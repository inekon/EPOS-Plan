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

## 6. Kundenbestände außerhalb des Erststarts

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

## 7. Wiederherstellung

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

## 8. Wo was steht

| Thema | Datei |
|---|---|
| Schema, Generator, Prüfrezepte, Arbeitspaket-Protokolle | [`sql\LIESMICH.md`](sql/LIESMICH.md) |
| Mehrbenutzerbetrieb, `icacls` | [`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md) |
| Installer-Hinweise | [`BETRIEB_Installer_Hinweise.md`](BETRIEB_Installer_Hinweise.md) |
| Gesamtkonzept der Umstellung | [`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md) |
| Beispieldatenbank zum Üben | [`sqlite-probe\LIESMICH.md`](sqlite-probe/LIESMICH.md) |
