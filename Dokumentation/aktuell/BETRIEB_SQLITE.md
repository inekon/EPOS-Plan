# Betrieb: die SQLite-Datenbank von EPOS-Plan

**Stand:** 24.09.2026 · Arbeitspaket S8 des
[`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](../ueberholt/Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md)
(dort Abschnitt 8), Abschnitt 1 neu nach dem Anwenderentscheid `#157‑E‑1` (Weg W3),
Abschnitte 1.1 und 7 nach der Einstellung der Access-Übernahme (24.09.2026)

EPOS-Plan hält seine Daten in **einer** SQLite-Datei. **Access und die ACE-Engine kommen
im Programm nicht mehr vor** (Weg W3, 09.09.2026): Eine Neuinstallation bekommt ihre
Datenbank aus der ausgelieferten Vorlage (Abschnitt 1); eine Übernahme eines
`.accdb`-Altbestands gibt es nicht mehr (Abschnitt 1.1 und 7).

| | |
|---|---|
| Datenbank | `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` |
| Beidateien im Betrieb | `Kenndaten.sqlite-wal`, `Kenndaten.sqlite-shm` |
| Ordner änderbar über | Administration → Datenbankpfad (Einstellung `DBPath`) |
| Dateiname | Einstellung `DBName`, Vorgabe `Kenndaten.sqlite` |
| Journalmodus | **WAL**, dateipersistent (einmalig vom Migrator gesetzt) |
| je Verbindung | `PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;` |

---

## 1. Erststart: Woher die Datenbank kommt

**Anwenderentscheid `#157‑E‑1`, Weg W3 (09.09.2026) — umgesetzt.** Auf einem frischen
Rechner liegt im Datenordner nichts. Findet EPOS-Plan beim Start keine
`Kenndaten.sqlite`, **kopiert es die ausgelieferte Vorlage dorthin** und startet danach
normal weiter. Das ist der einzige Weg, auf dem eine Datenbank entsteht.

| | |
|---|---|
| Vorlage (nur lesen) | `%ProgramFiles%\EPOS-Plan\Vorlage\Kenndaten.sqlite` |
| Ziel (Arbeitsdatenbank) | `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` |
| Wer legt die Vorlage hin | das Setup (`Setup/EPOS-Plan.iss`, `#define VorlageDb`) |
| Wer kopiert | `EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs` |
| Wer ruft | `WindowsFormsApplication1/Program.cs`, `DatenbankBereitstellen()` |

Der Ablauf, mit Datei und Fundstelle:

| Schritt | Fundstelle | Ergebnis |
|---|---|---|
| Startprüfung | `Program.Main` → `DataRepository.DatenbankVorhanden()` | `false` — es gibt keine Datenbank |
| Bereitstellung | `Program.DatenbankBereitstellen()` | ruft `Erstbereitstellung.Sicherstellen(Ziel, Vorlage)` |
| Pfad der Vorlage | `EPOS.Kern/Allgemein/Dienste/StandardPfade.cs`, `Auslieferungsvorlage` | Aufstieg von `AppContext.BaseDirectory`, `Vorlage\Kenndaten.sqlite` bzw. `Setup\Vorlage\…` |
| Kopie und Prüfung | `Erstbereitstellung.Sicherstellen` | `PRAGMA integrity_check` **und** `Tab_Applikation.SchemaVersion` |
| zweite Startprüfung | `DataRepository.DatenbankVorhanden()` | erst `true` lässt den Start weiterlaufen |
| Schemapflege | `SchemaMigration.Ausfuehren` | hebt eine ältere Vorlage auf den benötigten Stand |

**Drei Zusicherungen.**

1. **Nie überschreiben.** Liegt am Ziel schon eine Datei, wird sie nicht angefasst — auch
   dann nicht, wenn sie beschädigt ist. Dort stehen die Projekte des Anwenders; eine
   „Reparatur" durch Überschreiben wäre Datenverlust.
2. **Nie halb liegen lassen.** Bricht das Kopieren ab oder hält die Kopie der Prüfung
   nicht stand, wird die Zieldatei wieder entfernt. Sonst stünde beim nächsten Start eine
   Ruine da, die als „vorhanden" gälte und nie wieder ersetzt würde.
3. **Erst prüfen, dann melden.** Beide Prüfungen zusammen belegen, dass die Vorlage eine
   vollständige EPOS-Plan-Datenbank ist und nicht bloß eine Datei mit dem richtigen Namen.

**Die Vorlage darf älter sein als der aktuelle Schemastand.** Sie wird beim Bauen des
Setups eingefroren; bis zur Auslieferung können Schemaschritte dazugekommen sein.
Angehoben wird sie beim selben Start von der Schemapflege — die Bereitstellung verlangt
nur, dass der Schemamarker überhaupt da ist.

**Fehlt auch die Vorlage**, bleibt es bei der bisherigen Meldung `START_DB_FEHLT`;
sie nennt seither **beide** Orte — wo die Datenbank erwartet wurde und wo die Vorlage
gesucht wurde (`START_VORLAGE_FEHLT`). Das Programm startet dann nicht.

> **Der Ordner muss beschreibbar sein.** Die Kopie entsteht im Datenbankordner. Unter
> `C:\ProgramData` sorgt dafür der `[Dirs]`-Eintrag des Setups (`users-modify`); bei einem
> Altbestand hilft die `icacls`-Zeile aus Abschnitt 4.

**Auf iOS gilt derselbe Gedanke.** `EPOS.iOS/Datenbankbereitstellung.cs` kopiert die
mitgelieferte `Kenndaten.sqlite` beim ersten Start aus dem (schreibgeschützten)
Anwendungspaket in die Sandbox — dieselbe Regel „liegt sie schon da, ist nichts zu tun",
derselbe Ordnername `EPOS_PLAN`.

### 1.1 Übernahme eines Access-Altbestands — eingestellt

**Rahmen (Anwender, 09.09.2026):** Access wurde beim Kunden **nie produktiv eingesetzt**.
Mit dem Entscheid `#157‑E‑1` (Weg W3) sind deshalb **gefallen**: der Übernahme-Assistent
im Programmstart, die Access-Engine im Setup und die `.accdb`-Vorlage. In der
Anwenderdokumentation kommt Access nicht mehr vor.

**Seit dem 24.09.2026 gibt es auch das Hauswerkzeug nicht mehr** (Anwenderentscheid
24.09.2026): Die Konsolenfassung `EposSqliteMigrator.exe`, die einen `.accdb`-Bestand auf
Schemastand 61 nach SQLite übertrug, ist am 13.09.2026 aus dem Repository entfernt worden
(Sync-Commit `43aaf985`; letzter Stand mit dem Werkzeug: `b0647c7e`), und die Übernahme aus
Access ist damit endgültig eingestellt — weder Kundenweg noch Hausweg. Es gibt keinen
Kundenbestand in Access, und die Hausbestände sind umgestellt. Was für einen dennoch
auftauchenden `.accdb`-Bestand bliebe, steht in Abschnitt 7.

**Was das Programm mit einer Datei unterhalb Stand 61 tut:** Es weist sie im
Migrationsbericht ab und sperrt die Simulation; gehoben wird sie nicht. Die Schritte 1
bis 61 stehen nicht im Programm, und im heutigen Programm gibt es keinen Access-Zweig
mehr — weder die Alt-Hebung noch `System.Data.OleDb`.

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

## 2a. Ein gelöschtes Projekt nimmt seine Zeilen mit

Ab **Schemastand 96** tragen die achtundzwanzig Projekttabellen, die ihre Beziehung zu
`Tab_Projekt` bis dahin nur dem Namen nach führten, einen echten Fremdschlüssel mit
`ON DELETE CASCADE ON UPDATE CASCADE` — zu den zwanzig, die ihn seit der
Access-Übernahme haben. Wer ein Projekt löscht, löscht damit auch seine Gebäude,
Erzeuger, Ganglinien, Kennlinien, Ergebnisse und Berichtskonfiguration; liegen bleiben
kann nichts mehr. Die Liste steht bei `ProjektFremdschluessel.Katalog`.

Zwei Tabellen bleiben benannt ausgenommen: `Tab_Applikation` (die Spalte merkt sich das
zuletzt geöffnete Projekt, 0 = keines) und `Tab_Kenndaten_Kuehlung_STAMM`
(Katalogtabelle der Auslieferung).

**Für den Betrieb heißt das zweierlei.** Erstens: Eine Sicherung *vor* dem Löschen eines
Projekts ist ab hier wirksamer als danach — was die Kaskade mitnimmt, ist weg. Zweitens:
Der Migrationsschritt selbst **entfernt Zeilen**, nämlich die, die schon vorher zu keinem
Projekt mehr gehörten und deshalb von keiner Maske und keinem Rechenweg erreichbar waren.
Wo eine ungepflegte Projektspalte an einem gültigen Elternsatz hängt (Kennlinien an ihrer
Wärmepumpe, Typzeilen an ihrem Verbraucher), wird sie nachgezogen statt die Zeile zu
verlieren. Der Migrationsbericht nennt für jede Tabelle, wie viele Zeilen geheilt, wie
viele gelöscht und wie viele abhängige Zeilen mitgenommen wurden.

---

## 2b. Änderungsstempel: Trigger als Teil des Schemas

Zwei Spalten setzt nicht die Anwendung, sondern **die Datenbank selbst** — über Trigger
(Schemaschritt 159, Quelle `EPOS.Kern/Allgemein/Update/KostenStempelSchema.cs`):

| Spalte | Was sie festhält |
|---|---|
| `Tab_Projekt.Kosten_Geaendert` | die letzte Änderung an Kosten, Preisen oder Wirtschaftlichkeitsparametern **dieses Projekts** |
| `Tab_Applikation.Kostenkatalog_Geaendert` | die letzte Änderung am **Kostenkatalog**, der in jedem Projekt gilt |

Beide sind nullbares `TEXT`; leer heißt „keine Änderung festgehalten". Die Trigger schreiben
`datetime('now','localtime')` — Ortszeit `JJJJ-MM-TT hh:mm:ss`, dasselbe Format wie der
Zeitstempel eines gespeicherten Wirtschaftlichkeitsergebnisses
(`Tab_ErgebnisWirtschaftlichkeit.Zeitstempel`, gesetzt am Ende der Rechnung). Ist der jüngste
Stempel der Vergleichsgruppe (Stamm und Varianten über `Tab_Variante.ID_ProjektRef`) oder der
Katalogstempel **strikt jünger** als das Ergebnis, gilt es als veraltet, und die Seiten Kosten und
Wirtschaftlichkeit zeigen das Band „bitte neu berechnen" mit dem Grund
(`KostenAenderungsstempel`, `WirtschaftlichkeitCtrl.Veraltung`). Eine Änderung in derselben Sekunde
wie das Speichern des Ergebnisses zählt als davor.

**Was stempelt** — Anlegen, Ändern und Löschen einer Zeile, je Zeile:

| Stempel | Tabellen |
|---|---|
| das Projekt der Zeile | `Tab_ProjektWerte`, `Tab_ProjektWirtschaftlichkeit`, `Tab_ProjektTarif`, `energy_project_settings`, `energy_price`, `Tab_ProjektPhotovoltaik` |
| Variante **und** Stamm | `Tab_Variante` |
| das Projekt der Anlage | `Tab_Energieanlagen` — Anlegen und Löschen immer, Ändern nur an den Kostenspalten (`ID_Carrier`, die elf `KWKG_*`, `Energiesteuer_Wahl`, `Aufteilung_Methode`, `Hilfsenergie_Anteil`, `Kuehl_ID_Carrier`, `Kuehl_EigenerZaehler`) |
| das Projekt selbst | `Tab_Projekt` — nur ein geänderter `Emission_Berechnungsmodus` |
| das Projekt, als Stammreihe der Katalog | `Tab_Preisreihe`, `Tab_PreisreiheDaten` — eine Reihe ohne Projekt gilt in jedem Projekt |
| der Katalog | `energy_carrier`, `pricing_model`, `energy_conversion`, `Tab_Brennstoff_Stamm`, `Tab_BrennstoffKategorien`, `emissionswert`, `Tab_Gesetzesparameter`, `Tab_Kostenfaktor`, `Tab_KostenKomponente`, `Tab_Nutzungsdauer`; `emissionsart` nur bei geändertem `co2_aequivalent`, `Tab_Applikation` nur bei geändertem `Emission_Berechnungsmodus` |

**Was nicht stempelt**, mit Absicht: Geräte-, Gebäude- und Einstellungstabellen — sie wirken über
die Simulation, deren Lauf die Frage nach dem Ergebnis ohnehin prüft —, `Tab_Kraftwerkspark`,
`Tab_ProjektWirkung`, die Kostenvorlagen, `Tab_KostenGruppenKatalog` und alle Ergebnistabellen.
Die Trigger an `Tab_Projekt` und `Tab_Applikation` lösen sich nicht selbst aus: Sie hören nur auf
den Emissionsmodus, ihr eigenes `UPDATE` setzt allein die Stempelspalte.

**Trigger sind Teil des Schemas.** Die 63 Trigger (`trg_Kostenstempel_*`, `trg_Katalogstempel_*`)
stehen in `sqlite_master` neben Tabellen und Indizes; sie reisen mit jeder Dateikopie, mit
`VACUUM INTO` (Abschnitt 3.2) und mit der Auslieferungsvorlage. Daraus folgt:

* **Wer eine der Tabellen oben neu baut** (umbenennen, neu anlegen, umkopieren, löschen — der Weg
  der Schemaschritte für Spaltentypen und Fremdschlüssel), verliert ihre Trigger mit der alten
  Tabelle. Danach legt `KostenStempelSchema.Ausfuehren` bzw. dessen Anweisungen sie wieder an
  (`CREATE TRIGGER IF NOT EXISTS`); die Wache `EPOS.Kern.Tests/KostenStempelSchemaTests` hält die
  Testdatenbank gegen die volle Liste. Ein Neubau von `Tab_Projekt`, `Tab_Applikation` oder
  `Tab_Preisreihe` ohne `PRAGMA legacy_alter_table = ON` scheitert mit „error in trigger …: no such
  table“, weil die Trigger der anderen Tabellen diese Namen nennen — die bestehenden Neubauten laufen
  mit `legacy_alter_table`, allein `SpeicherAuslegungStrict` (Schritt 74) ohne, und keine Trigger
  nennt `Tab_SpeicherAuslegung`.
* **Wer eine der genannten Spalten entfernt oder umbenennt**, prüft die Trigger mit — ihre
  Spaltenlisten (`UPDATE OF …`) und Rümpfe stehen im Text des Triggers.
* **Eine Änderung von Hand** (etwa mit `sqlite3` in einer der Tabellen oben) stempelt genauso;
  die Seiten zeigen danach das Band. Eine reine Abfrage stempelt nicht.
* Nachsehen: `SELECT name, tbl_name FROM sqlite_master WHERE type = 'trigger';`

---

## 3. Sicherung

Gesichert wird immer die **ganze Datei**: Kataloge und Projektdaten stehen in derselben
`Kenndaten.sqlite`, und dazu gehören seit Schemastand 73 (seit 74 als STRICT-Tabelle) auch die gespeicherten
Speicherauslegungsprofile in `Tab_SpeicherAuslegung` (Kostensätze, Suchbereiche und die
zugeordneten CSV-Zeitreihen als Projektdaten, mit `ON DELETE CASCADE` am Projekt und an der
Energieanlage). Einen getrennten Export einzelner Tabellen gibt es nicht — wer ein Profil
retten will, sichert die Datei. Dasselbe gilt umgekehrt für Abschnitt 8: Wer eine ältere
Sicherung zurücklegt, nimmt den Auslegungsstand von damals mit.

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

**Genau diesen Weg nutzt seit Auftrag #158 auch das Programm selbst** —
`EPOS.Kern/Allgemein/Datenbank/Datenbanksicherung.KopieAnlegen`, gerufen vom Sicherungspunkt
des Hilfe-Assistenten und von `MenueCtrl.DatenbankKopieAnlegen` (Projekte löschen,
Projektimport); eine reine `File.Copy` der Hauptdatei kommt dort seither nicht mehr vor.

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
[`BETRIEB_Mehrbenutzer_Datenbank.md`](../ueberholt/BETRIEB_Mehrbenutzer_Datenbank.md) **bleibt
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
| **`Werkzeuge/Auslieferungsvorlage`** | erzeugt aus einer produktiven `Kenndaten.sqlite` die **bereinigte Auslieferungsdatenbank** samt Beispielprojekten (`.wpx`) und legt einen Prüfbericht daneben: Projektdaten entfernt, Kataloge auf den Auslieferungsstand, `Tab_Applikation` ohne Kundennamen, `VACUUM`, `journal_mode = WAL`, Schemastand, `integrity_check`, Datenschutzwächter. Die Quelle bleibt byte-gleich (`VACUUM INTO` über `Datenbanksicherung.KopieAnlegen`). Aufruf: `dotnet run --project Werkzeuge/Auslieferungsvorlage -c Release -- <quelle.sqlite> <ziel.sqlite> [--beispiele <ordner-oder-liste>] [--trocken]`; Rückgabe 0 = erzeugt und abgenommen, alles andere ein Abbruch mit Grund auf `stderr`. Einzelheiten in [`Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md`](Konzept_Setup_InnoSetup_EPOS-Plan.md) § 6.1 |

**Mindestens SQLite 3.37** — darunter versteht das Werkzeug die `STRICT`-Tabellen des
Zielschemas nicht. `VACUUM INTO` gibt es ab 3.27.

Der frühere Übungsordner `sqlite-probe\` (8 Tabellen, erfundene Werte) ist mit #242 entfernt —
seit dem 02.09.2026 durch dieses Betriebspapier und die Migration ersetzt (nicht mehr im
Repository; Stand `a598b564` in der Git-Geschichte).

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
Einzelheiten in [`Werkzeuge\SqlDialektPruefer\LIESMICH.md`](../../Werkzeuge/SqlDialektPruefer/LIESMICH.md).

**Eine einzelne Anweisung von Hand prüfen** — ohne die Datenbank zu verändern:

```
sqlite3 -readonly C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite "EXPLAIN SELECT …;"
```

`EXPLAIN` bereitet die Anweisung nur vor und führt sie nicht aus. Sie durchläuft dabei
Syntax- **und** Objektprüfung: „near …: syntax error" und „no such column: …" fallen beide
hier auf, nicht erst beim Anwender.

### 6.5 Die Messlatte selbst — `Referenzlaeufe/Kenndaten_Test.sqlite`

**Schemastand 173** (`Tab_Applikation.SchemaVersion`; jüngste Schritte 156 Kessel-Kennlinie, 157 Saat der Konditionierungsvorlagen, 158 Brennwert in Projekten, 159 Änderungsstempel — Abschnitt 2b, 160 und 161 Aufheizoptimierung (Projekteinstellung, Ergebnisspalten), 162 Einheit des Bereitschaftsverlusts am Heizkessel (`Bereitschaft_Einheit`, kW oder %, Vorgabe kW), 163 Bodenalbedo je Photovoltaik- und Solarthermie-Anlage (`Tab_Energieanlagen.Albedo`, 0 bis 1, leer = 0,2), 164 Temperaturpaar je Prozess (`Vorlauf`, `Ruecklauf` an `Tab_Prozesswaerme(_STAMM)`, REAL, nullbar, 0 … 250 °C, paarweise) samt Saat der acht ausgelieferten Betriebsweisen (`Tab_Prozesswaerme_STAMM`, `Tab_Prozesstyp_STAMM`, `ReadOnly = 1`), 165 Felder des Kollektorfelds an `Tab_Energieanlagen` (`Pumpenleistung_W`, `Solarkreisverluste_Prozent`, `Uebertrager_Graedigkeit_K`, `Kollektor_Spreizung_K`, `Arbeitstemperatur_Weg`, nullbar mit Prüfklausel) und Bezugsfläche der Kennwerte an `Tab_Solarkollektoren(_STAMM)` (`Bezugsflaeche`, apertur oder brutto, Vorgabe apertur), 166 Netzverluste je Kanal und Zirkulation im Bestandsweg (acht nullbare Spalten `Netzverluste_Heizung`/`_Brauchwasser`/`_Prozess` samt `…_Einheit`, `Zirkulation_Leistung_kW`, `Zirkulation_Laufzeit_h_d` an `Tab_Einstellungen`) und Betriebskalender der Bedarfsprofile (`Tab_Betriebskalender`, STRICT, und `ID_Betriebskalender` an `Z_Projekt_Brauchwasser`, `Z_Projekt_Prozesswaerme`, `Z_Projekt_Stromverbraucher`, nullbar, `ON DELETE SET NULL`), 167 Teillastfelder der Wärmepumpe an `Tab_WP(_STAMM)` (`Mindestleistung_kW` 0 … 1 000, `Taktverlustfaktor_Cd` 0 … 1) und des BHKW an `Tab_BHKW(_STAMM)` (`Wirkungsgrad_el_Teillast50`, `Wirkungsgrad_th_Teillast50` als Faktor 0 … 1, `Anfahrverlust_kWh` 0 … 100, `Mindestlaufzeit_min` 0 … 60), nullbar mit Prüfklausel, leer = Rechnung wie zuvor, 168 Einspeisegrenze des Projekts an `Tab_Einstellungen` (`Einspeisegrenze_Wert` ≥ 0, `Einspeisegrenze_Einheit` kW oder %, beide nullbar, leer = keine Grenze) und Selbstentladung an `Tab_Stromspeicher(_STAMM)` (`Selbstentladung_Prozent_Monat`, 0 … 20, leer = keine), 169 Pufferspeicher-Auslegung (`Tab_PufferAuslegung`, STRICT, leer, und `Tab_PufferAuslegungParameter_STAMM`, STRICT, mit gesäten Vorgabewerten), 170 Katalogempfehlung der Hilfsenergie von BHKW und Heizkessel auf Weg B (reines DML an `Tab_KostenVorlagePosition` der Auslieferungsvorlagen, `ReadOnly = 1`), 171 Optionen des Pufferspeichers an `Tab_Pufferspeicher` (`Bereitschaft_Weg` tag oder temperatur, `Aufstellraum_Temperatur_C` 0 … 35, `Schicht_Anteile` als Text, `Frischwassermodul` 0/1, `FWM_Graedigkeit_K` 0 … 20) und thermische Desinfektion an `Tab_Einstellungen` (`Desinfektion_Aktiv` 0/1, `Desinfektion_Intervall_Tage` 1 … 31, `Desinfektion_Stunde` 0 … 23, `Desinfektion_Zieltemperatur_C` 55 … 90, `Desinfektion_Volumen_l` 0 … 100 000), nullbar mit Prüfklausel, leer = Rechnung wie zuvor, 172 Katalogfassung der ausgelieferten Sätze (`Katalog_Schluessel`, `Katalog_Pruefsumme`, `Katalog_Ausgelaufen` an acht Katalogtabellen, `Tab_Applikation.Katalogfassung`) samt Protokoll des Abgleichs (`Tab_Katalogabgleich`, STRICT) und gespeicherter Erdreichprüfung (`Tab_ErgebnisErdreich`, STRICT), 173 dieselben Katalogspalten an den sechzehn Katalogen der Stufe 2 samt Saat — Abschnitt 8a; die Schritte bis 76 im Einzelnen: 65 Wechselrichterkatalog,
66 Stränge, 67 BHKW-Leistungsgrenze, 68 `Firma` im Stromspeicherkatalog, 69 PV-Koeffizienten
repariert, 70 PV-Strangprüfung — Kurzschlussstrom je MPPT, `Ausleg_T_Kalt`/`Ausleg_T_Heiss` an
`Tab_Einstellungen` —, 71 zwölf nullbare Szenario-Spalten an `Tab_ProjektWirtschaftlichkeit`,
72 p_I in drei Spalten und der Freitext „Nicht monetäre Wirkungen" an derselben Tabelle;
73 `Tab_SpeicherAuslegung` für projekt- und anlagenbezogene Kostenprofile, Suchbereiche
und importierte Zeitreihen samt Zuordnung; **74 dieselbe Tabelle als STRICT-Tabelle neu
aufgebaut** — sie war die einzige Fachtabelle des Zielschemas ohne `STRICT`, und SQLite kennt
kein `ALTER TABLE … STRICT`; **75 die Nutzungsdauertabelle `Tab_Nutzungsdauer`** mit 28
Auslieferungszeilen, der nullbaren Verweisspalte `NutzungsdauerID` an
`Tab_KostenVorlagePosition` und `Tab_ProjektWerte` und der Saat-Zuordnung von 31 der 53
Investitionspositionen; **76 der eindeutige Index `idx_EnergyProjectSettings_Traeger`** über
`energy_project_settings (ID_Projekt, ID_Energieträger)` — ein Preis- und Emissionssatz je
Energieträger und Projekt, gehalten von der Datenbank statt allein von der Anwendungslogik,
dem eine Entdoppelung des Bestands vorausgeht), **70 766 592 Byte (67,5 MB)**,
**120 Tabellen, davon 119 STRICT**
(die 120. ist die Systemtabelle `sqlite_sequence`), **25 Projekte**. Zu keinem Projekt
außerhalb von `Tab_Projekt` stehen noch Gerätezeilen: `GeraeteWaisen.Aufraeumen` findet in
den sieben Gerätetabellen keine verwaiste Zeile mehr, `PRAGMA foreign_key_check` bleibt leer.
Die Tabelle `Tab_SpeicherAuslegung` führt seit dem Prüfprojekt 1046 (Basis R7) genau eine
Zeile, den reservierten Flottenstand `@Projektflotte`; Schritt 74 hat sie byte-gleich übernommen.
Nachzusehen ist der Stand jederzeit:

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
(`SchemaKatalog`), die Typübersetzung (`StilleDb.SqliteSpaltenTyp`), für Schritt 62 die
`DELETE`-Texte aus `KlimaWaisenBereinigung` und für Schritt 74 die sechs Anweisungen aus
`SpeicherAuslegungStrict` —, setzt den Marker auf `SchemaStand.Zielversion` und
verdichtet mit `VACUUM`. Es ist **idempotent** (vorhandene Spalte = nichts zu tun; Schritt 74
fragt `sqlite_master`, ob die Tabelle überhaupt noch ohne `STRICT` steht), läuft auf
Linux und kennt `--trocken` für den Blick vor dem Griff. Von Hand angelegte Spalten wären eine
zweite Schreibweise derselben Spalte — genau das, was die Typübersetzung verhindern soll.

> **Drei Spalten liegen AUSSERHALB dieses Wegs, mit Absicht: die Ergebnisspalten
> `StromsteuerBefreiungModus`, `ErsatzBarwert` und `Nachweis_Json` von
> `Tab_ErgebnisWirtschaftlichkeit`.** Sie führt weder das Grundschema noch ein Schemaschritt;
> `WirtschaftlichkeitCtrl.StelleTabellenSicher()` zieht sie additiv nach, sobald sie fehlen
> (Begründung dort). Alles Übrige der Wirtschaftlichkeit hat genau EINEN DDL-Ort: Die fünf
> Tabellen stehen im Grundschema, STRICT und mit Fremdschlüssel auf `Tab_Projekt` (drei davon
> aus Schritt 96), jede Eingabespalte in ihrem Schemaschritt; der Controller legt weder Tabellen
> an noch zieht er Eingabespalten nach (Wache `WirtschaftlichkeitCtrlTabellenTests`). Das
> Werkzeug oben trägt `StromsteuerBefreiungModus` und `Nachweis_Json` nach; `ErsatzBarwert`
> (DOUBLE, Etappe W5‑B‑10) wurde EINZELN per `ALTER TABLE … ADD COLUMN … REAL` (die wortgleiche
> Ausgabe der Typübersetzung für `DOUBLE`) nachgezogen — nicht über den vollen Aufruf von
> `StelleTabellenSicher()`, der am Ende `GesetzKatalog.StelleKatalogSicher()` anhängt, das bei
> veralteter Generation neue Gesetzesparameter-Zeilen einfügen würde, ein Dateninhalt jenseits
> einer reinen Schema-Nachführung. **Wer eine weitere Ergebnisspalte ohne Schemaschritt
> einführt, trägt sie im Werkzeug Testdatenbankschema nach und prüft die Testdatenbank mit — der
> SQL-Dialektprüfer zeigt die Lücke an, das Schließen bleibt Handarbeit.**

> **Danach ist der Referenzlauf Pflicht, nicht Kür.** Eine Schemamigration darf keinen
> Rechenwert verschieben; belegt wird das, indem
> `EPOS.Referenzlauf lauf --projekte 1030,1007,1017` **vor und nach** dem Nachziehen läuft und
> `diff -r` über beide Zielordner nur `protokoll.txt` meldet (Zeitstempel, Zielordner,
> Dateigröße, Laufdauer). Jede abweichende CSV ist ein Befund.
>
> **Die eine Ausnahme, und sie ist benannt: Schritt 69** (Befund W6‑B‑5, Anwenderentscheide
> Q1–Q3 vom 07.09.2026). Er repariert die verdorbenen PV-Modulkoeffizienten, und `T_NOCT`
> geht in beide PV-Modelle — Projekt 1007 weicht dadurch gewollt ab. Für einen solchen
> Schritt heisst die Abnahme nicht „byte-gleich", sondern „**erklärte** Abweichung plus neu
> eingefrorene Basis": `Referenzlaeufe/2026-09-07_R6_PvKoeffizienten` mit der Herleitung im
> `protokoll.txt`. Wer den Weg geht, braucht dafür einen Anwenderentscheid — die stillschweigende
> Regel bleibt: eine Schemamigration verschiebt keinen Rechenwert.

Wächst die Datei über die Zeit, ist der Weg zurück
[`sql/tools/Reduziere-Testdatenbank.sql`](../../sql/tools/Reduziere-Testdatenbank.sql): Es schneidet
eine Kopie der produktiven Datenbank auf die dreizehn Referenzprojekte zurück (iE6, iF14). Das
Nachziehen des Schemas ersetzt es nicht — es setzt es voraus.

---

## 7. Kundenbestände: keine Übernahme aus Access mehr

**Seit dem 24.09.2026 gibt es keinen Weg mehr, einen `.accdb`-Altbestand zu übernehmen.**
Weg W3 (09.09.2026) hatte die Übernahme aus dem Programm genommen und auf das Hauswerkzeug
`EposSqliteMigrator.exe` verlegt; das Werkzeug ist am 13.09.2026 aus dem Repository
entfernt (Sync-Commit `43aaf985`) und die Übernahme am 24.09.2026 vom Anwender endgültig
eingestellt worden (Abschnitt 1.1).

**Was gilt:**

* Jeder Bestand ist eine `Kenndaten.sqlite` auf Schemastand 61 oder höher; die Schemapflege
  des Programmstarts bringt ihn auf den Zielstand (Abschnitt 1).
* Eine Datei ohne Schemamarker oder unterhalb Stand 61 weist das Programm ab
  (Migrationsbericht, Simulation gesperrt). Sie ist kein Bestand von EPOS-Plan; einen Weg,
  sie zu heben, gibt es nicht mehr.
* Eine **Auslieferungsvorlage** entsteht aus einem gepflegten Katalogstand über
  `Werkzeuge/Auslieferungsvorlage` und enthält keine Kundenprojekte
  (`Setup/build-setup.ps1`, Parameter `-Quelldatenbank`).

**Falls doch einmal ein `.accdb`-Bestand auftaucht** — Geschichte, kein Betriebsweg: Das
Werkzeug samt Aufruf, Rückgabewerten und Berichtsformat liegt im Git-Stand `b0647c7e`
(13.09.2026) unter `EposSqliteMigrator/` und baut auch heute gegen die Paketfassungen des
Repositoriums (geprüft am 24.09.2026). Die Quelle müsste auf Schemastand 61 stehen, was die
letzte Access-Fassung von EPOS-Plan leistet (Git-Zweig `version_august_2026`); das Werkzeug
braucht die 64-Bit-ACE-Engine. Bauplan und Betriebsablauf stehen im
[`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](../ueberholt/Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md)
(Abschnitt 4) und im
[`S7_Protokoll_2026-09-02.md`](../ueberholt/Protokolle/sql/S7_Protokoll_2026-09-02.md).
Die Windows-Suite `Referenzlauf/` (Modus `migration`) ist davon unabhängig.

### 7.1 Trägerzuordnung der Kessel prüfen

Ein Kessel ohne Energieträger (`Tab_Energieanlagen.ID_Carrier` leer oder 0) rechnet weiter: Seine
Emissionen nimmt der Lauf ersatzweise aus dem Brennstoffstamm des Geräts, und das
Simulationsprotokoll meldet das einmal je Kessel (Hinweis `EMISSION_OHNE_TRAEGER_KESSEL_*`: „… hat
keinen Energieträger zugeordnet — es gilt ersatzweise …“). Fehlt dem Projekt zudem die Projektzeile
des Brennstoffs mit Preis (`energy_project_settings`, für Erdgas mit Preisstand in `energy_price`),
hat eine frische Wirtschaftlichkeitsrechnung für diesen Kessel keine Energiekosten und damit
**keinen Kapitalwert**.

**In einem Bestand wird das nicht automatisch nachgezogen.** Wer Kessel ohne Träger findet, pflegt
sie nur nach Entscheid des Anwenders: vorher sichern (Abschnitt 3), den Vorzustand prüfen, in einer
Transaktion schreiben — nach dem Muster des einmaligen dotnet-Dateiskripts `e24_pflege`, mit dem die
Testdatenbank gepflegt wurde (Träger 63 „Erdgas E“, Projektzeile und Preisstand als Kopie einer
gepflegten Erdgaszeile; Protokoll
[`E24_Datenpflege_1018_1023_R17_Protokoll.md`](../ueberholt/Protokolle/Reporting/E24_Datenpflege_1018_1023_R17_Protokoll.md)).
Ändern können sich dabei die ausgewiesenen Emissionen: Mit Träger, aber ohne CO₂-Wert in der
Projektzeile gilt die aktive Katalogzeile des Trägers (für Erdgas E 201 statt der 240 g/kWh des
Rückfalls).

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

**Vor jeder Wiederherstellung aus diesem Abschnitt prüfen, ob es den Ordner überhaupt
noch gibt**: Die Deinstallation fragt seit Auftrag #161 (09.09.2026), ob
`%ProgramData%\EPOS_PLAN` samt `DB-Backup` gelöscht werden soll (Vorgabe *Nein*, aber
ein bestätigtes *Ja* nimmt Datenbank und Sicherungsordner unwiederbringlich mit).

---

## 8a. Katalogabgleich nach einem Update

**Schemaschritt 172** (`KatalogfassungSchema`, nach 170 Katalogempfehlung der Hilfsenergie und 171 Optionen des Pufferspeichers): An den acht Katalogen der Stufe 1 — Wärmepumpen
(`Tab_WP_STAMM` samt `Tab_Kenndaten_STAMM` und `Tab_Kenndaten_Kuehlung_STAMM`), Heizkessel, BHKW,
PV-Module, Brauchwasser- und Prozesswärmeprofile samt Typen — stehen `Katalog_Schluessel` (TEXT,
eindeutig über einen Teilindex `UX_<Tabelle>_Katalog_Schluessel … WHERE Katalog_Schluessel IS NOT
NULL`), `Katalog_Pruefsumme` (TEXT, 64 Hexzeichen) und `Katalog_Ausgelaufen` (INTEGER 0/1, Vorgabe 0);
an `Tab_Applikation` die `Katalogfassung` (INTEGER, leer = noch nie abgeglichen); dazu die
STRICT-Tabellen `Tab_Katalogabgleich` (Protokoll) und `Tab_ErgebnisErdreich` (gespeicherte
Erdreichprüfung je Lauf und Anlage, am Projekt und an der Energieanlage mit `ON DELETE CASCADE`).
Die Saat des Schritts belegt Schlüssel und Prüfsumme jedes gesperrten Satzes (`ReadOnly = 1`); ein
eigener Satz (`ReadOnly = 0`) bleibt ohne Schlüssel. Kein Fachwert ändert sich, der Referenzlauf
bleibt byte-gleich. Die Messlatte aus 6.5 hebt `Werkzeuge/Testdatenbankschema` wie jeden Schritt.

**Schemaschritt 173** (`KatalogfassungStufe2Schema`): dieselben drei Spalten samt Teilindex und
dieselbe Saat an den sechzehn Katalogen der Stufe 2 — Baustoffe, Bauteilaufbauten, Brennstoffe
(`Tab_Brennstoff_Stamm`), Tagesverteilungen, Gebäude, Konditionierungsvorlagen, Pufferspeicher,
Vorgaben der Pufferauslegung, Solarkollektoren, Solar-, Strom- und Wärmebedarfsganglinien,
Stromspeicher, Stromverbraucherprofile samt Wochenprofilen, Wechselrichter. Ihre Kindtabellen
(Synonyme, Schichten, Ganglinien- und Verteilungswerte, Konditionierungsvorgaben, -kalender und
-perioden) bekommen keine Spalte; ihre Zeilen gehören über den Fremdschlüssel zum Kopfsatz. Nicht
abgeglichen werden der **Klimakatalog** (`Tab_Klimaregion_STAMM`, `Tab_Klimadaten_STAMM`,
`Tab_Solar_STAMM` — Pflege über den Klimaimport) und der **Zapfprofilkatalog** (`Tab_Tww*_STAMM` —
Pflege über sein eigenes Paket mit Katalogversion). Kein Fachwert ändert sich, der Referenzlauf
bleibt byte-gleich.

**Das Paket.** Die Auslieferung legt neben `{app}\Vorlage\Kenndaten.sqlite` die Datei
`{app}\Vorlage\Katalogpaket.json` (geschrieben von `Werkzeuge/Auslieferungsvorlage`, Fassung über
`--katalogfassung`): alle ausgelieferten Sätze der Stufen 1 und 2 mit Schlüssel, Prüfsumme und
Werten, Ganglinien als Wertelisten (Formatversion 2; Größe im Bericht des Werkzeugs, Warnung ab
20 MB). Sie liegt nicht im Repository (`.gitignore: Setup/Vorlage/Katalogpaket.json`).

Brennstoffe, Konditionierungsvorlagen und die Vorgaben der Pufferauslegung haben keine Projektkopie:
Ein Projekt, das einen aktualisierten Satz davon benutzt, rechnet nach dem Abgleich mit dem neuen
Auslieferungsstand. Wer das für einen Satz nicht will, entsperrt oder ändert ihn vor dem Update — dann
bleibt er stehen.

**Ablauf beim Start.** Nach einer erfolgreichen Schemamigration vergleicht EPOS-Plan die Fassung
des Pakets mit `Tab_Applikation.Katalogfassung`. Ist das Paket neuer:

1. Sicherung per `VACUUM INTO` (Abschnitt 3.2) als `Kenndaten_Katalogabgleich_<Zeitstempel>.sqlite`
   in `DB-Backup` neben der Datenbank (gibt es den Ordner nicht: daneben). Scheitert die Sicherung,
   gleicht EPOS-Plan nicht ab.
2. Abgleich in EINER Transaktion: fehlender Satz eingefügt, unveränderter ausgelieferter Satz
   aktualisiert, angepasster oder entsperrter Satz behalten, entfallener Satz als ausgelaufen
   gekennzeichnet (nie gelöscht). Projektkopien und eigene Sätze fasst er nicht an.
3. Je Aktion eine Zeile in `Tab_Katalogabgleich`, dazu die Zusammenfassung; danach steht die
   Fassung des Pakets in `Tab_Applikation.Katalogfassung`. Ein Fenster nennt das Ergebnis.

Ohne Paket oder mit einem unlesbaren Paket bleibt der Katalog, wie er ist (beim unlesbaren Paket mit
der Zeile `KEIN_PAKET` im Protokoll).

**Nachsehen und wiederherstellen.**

```sql
SELECT Zeitpunkt, Fassung, Tabelle, Schluessel, Aktion, Hinweis FROM Tab_Katalogabgleich ORDER BY ID DESC;
SELECT Katalogfassung FROM Tab_Applikation;
```

Den Auslieferungsstand EINES behaltenen Satzes stellt Administration → Daten & Import → „Katalog
aktualisieren…" wieder her (Aktion `WIEDERHERGESTELLT`). Den Stand VOR dem Abgleich insgesamt holt
die Sicherung aus Schritt 1 zurück (Abschnitt 8); beim nächsten Start gleicht EPOS-Plan dann erneut ab.

---

## 9. Wo was steht

| Thema | Datei |
|---|---|
| Schema, Generator, Prüfrezepte, Arbeitspaket-Protokolle | [`sql\LIESMICH.md`](../../sql/LIESMICH.md) |
| Mehrbenutzerbetrieb, `icacls` | [`BETRIEB_Mehrbenutzer_Datenbank.md`](../ueberholt/BETRIEB_Mehrbenutzer_Datenbank.md) |
| Installer-Hinweise | [`BETRIEB_Installer_Hinweise.md`](../ueberholt/BETRIEB_Installer_Hinweise.md) |
| Gesamtkonzept der Umstellung | [`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](../ueberholt/Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md) |
| SQL-Dialekt-Prüfer (Aufruf, Regeln, Ausnahmen) | [`Werkzeuge\SqlDialektPruefer\LIESMICH.md`](../../Werkzeuge/SqlDialektPruefer/LIESMICH.md) |
