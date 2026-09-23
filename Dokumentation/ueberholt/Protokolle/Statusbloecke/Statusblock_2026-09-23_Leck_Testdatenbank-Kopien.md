# Statusblock ohne Nummer — Leck der Testdatenbank-Kopien geschlossen (23.09.2026)

Der ausführliche Block zur Statuszeile „Leck der Testdatenbank-Kopien“ (ohne Nummer) in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Der Auftrag lautete:
die Ursache finden, warum die Arbeitskopien der Testdatenbank unter `%TEMP%` liegen bleiben;
so beheben, dass ein voller Gate-Lauf keine Kopie zurücklässt, ohne das Verhalten der Tests
sonst zu ändern; eine Probe ergänzen, die das Leck künftig auffallen lässt; die verwaisten
Altkopien nur dort löschen, wo sicher kein Testlauf einer anderen Sitzung sie benutzt.

## 1 Befund

- Unter `%LOCALAPPDATA%\Temp` lagen **1 170 Ordner** `epos-kerntest-<8 Hex>` mit je einer
  Kopie der Testdatenbank (65–77 MB), zusammen **77,5 GB**, der älteste vom 05.09.2026.
  Beim vollen Gate lief Laufwerk C: voll, 199 Fälle fielen mit Schreibfehlern.
- Nach Entstehungszeit gruppiert ergaben sich 17 Läufe. Die zwei Läufe vom 05.09. ließen
  je rund 210 Ordner liegen, 329 davon mit `-wal`/`-shm` — das ist der Stand vor
  `SqliteConnection.ClearAllPools` in `Dispose` (Windows-Abnahme 05.09.2026). **Seit dem
  22.09. ließ jeder Lauf genau 37, ab dem 23.09. genau 40 Ordner liegen**, ohne `-wal`:
  deterministisch, also bestimmte Instanzen und keine Zufallssperren.
- Halbe Kopien gab es nicht (alle Dateien mindestens 67,6 MB); der Abbruch beim vollen
  Laufwerk hatte also keine zusätzlichen Reste erzeugt.

## 2 Ursache

Vier Testklassen hielten die Vorrichtung als Feld, trugen aber kein `IDisposable`:

| Klasse | Fälle | angelegt |
|---|---:|---|
| `BhkwWirkungsgradAnteileTests` | 16 | 20.09.2026 |
| `BhkwWirkungsgradFaktorTests` | 9 | 19.09.2026 |
| `ProjektFremdschluesselTests` | 12 | 19.09.2026 |
| `KwkgAnlagenartLeerTests` | 3 | 23.09.2026 |

xunit legt je Testfall eine neue Instanz der Klasse an und entsorgt sie nur, wenn sie
`IDisposable` ist. So entstand je Fall eine Kopie, die niemand löschte: 37 Fälle, mit der
jüngsten Klasse 40. Nebenbei blieben `DataRepository.PfadUeberschreibung` und
`Schreibnaht.Schreibrecht` für den Rest des Laufs auf der letzten dieser Kopien stehen.

**Messung** mit eigenem `TEMP`, damit fremde Läufe nicht mitzählen: die vier Klassen
**+40 Ordner** (21 davon mit `-wal`, weil am Prozessende kein Pool mehr geleert wurde), die
Vergleichsklasse `FremdschluesselVorgabeTests` (7 Fälle, mit `Dispose`) **+0**; nach der
Behebung dieselben vier Klassen samt neuen Proben **+0**.

## 3 Umsetzung

- **Die vier Klassen** tragen `: IDisposable` und `public void Dispose() => _db.Dispose();`
  — dieselbe Form wie ihre Nachbarn.
- **`EPOS.Kern.Tests/TestDatenbank.cs`:**
  - **Besitzmarke** `besitz.sperre` im Kopieordner, vom Anlegen bis zum Löschen exklusiv
    offen. Die Datenbankdatei selbst taugt als Zeichen nicht: Vor der ersten Verbindung ist
    sie frei, und der Pool von Microsoft.Data.Sqlite schließt eine untätige Verbindung nach
    zwei bis acht Minuten. Die Marke ist frei, sobald der Prozess ihres Besitzers endet —
    auch wenn er abgeschossen wurde.
  - **Rückbau bei abgebrochenem Aufbau:** Bricht der Konstruktor ab (volles Laufwerk beim
    Kopieren, LFS-Zeiger), ruft niemand `Dispose`. Er setzt deshalb selbst Pfad und
    Schreibrecht zurück und löscht den angefangenen Ordner.
  - **Löschen mit Wiederholung** (`OrdnerLoeschen`): Der erste Versuch ist der gewohnte Weg
    (Pool leeren, löschen). Jeder weitere sammelt vorher den Speicher ein, wartet kurz
    (zusammen rund 1,5 s) und hebt einen Schreibschutz auf. Das fängt eine Datei, die ein
    Virenscanner gerade liest, und die Griffe einer ungepoolten Verbindung, die erst der
    Finalisierer schließt.
  - **Aufräumlauf einmal je Prozess** vor der ersten eigenen Kopie
    (`VerwaisteKopienAufraeumen`): Er fasst nur Ordner an, die genau dem Muster
    `epos-kerntest-<8 Hex>` folgen, in denen keine Datei gesperrt ist und deren Besitzer fort
    ist — mit Marke: die Marke ist frei; ohne Marke (Stand vor der Marke): letzte Regung
    mindestens 2 h her.
  - Ein **interner Konstruktor** (Quelle, Wurzel) dient allein der Abbruchprobe; xunit sieht
    für eine Klassenvorrichtung nur öffentliche Konstruktoren.
- **Grenze.** Eine GEPOOLTE Verbindung, die nie entsorgt wurde, hält die Kopie bis zum
  Prozessende: Schon das erste `ClearAllPools` löst ihren Pool ab
  (`SqliteConnectionPoolGroup.Clear` setzt ihn auf `null`), ein zweites erreicht ihn nicht
  mehr, und abgelöste Pools räumt nur der interne Takt der Bibliothek (alle 30 s). Diese
  Kopie nimmt der nächste Lauf über die freie Besitzmarke mit.
- **`ErstbereitstellungTests`:** `Die_Kopie_ist_nicht_schreibgeschuetzt` gibt der Vorlage
  bewusst den Schreibschutz; `Directory.Delete` scheiterte daran unter Windows still, und je
  Lauf blieb ein Ordner `epos-erstbereitstellung-*` (8 KB) liegen. `Dispose` nimmt jetzt
  `TestDatenbank.OrdnerLoeschen`.
- **Regel** in `EPOS.Kern/CLAUDE.md` (Abschnitt Nachweis, „Tests mit Datenbank“).

## 4 Probe

- **`TestDatenbankEntsorgungWacheTests`** (6 Fälle, ohne Datenbank) liest die Quelldateien
  und die Typen der Assembly: Jedes `new TestDatenbank()` steht in einem `using` oder wird
  einem Namen zugewiesen, dessen `Dispose()` in derselben Datei gerufen wird; jede Klasse
  mit einer Arbeitskopie im Feld trägt `IDisposable` oder `IAsyncLifetime`, es sei denn, sie
  bekommt die Kopie als `IClassFixture<TestDatenbank>`. Gegenproben für Leser,
  Kommentarschonung, Typprüfung und Bestand.
- **`TestDatenbankAufraeumenTests`** (5 Fälle): `Dispose` löscht die Kopie und stellt den
  Pfad zurück; es löscht sie auch hinter einer nie geschlossenen ungepoolten Verbindung;
  ein abgebrochener Aufbau hinterlässt weder Ordner noch Zustand; die Besitzmarke ist belegt,
  solange die Vorrichtung lebt; der Aufräumlauf nimmt in einem eigenen Probenordner genau die
  verwaisten Kopien mit — nie eine mit belegter Marke, nie eine mit gesperrter Datei, nie
  einen fremden Ordner. Mit verschobener Uhr läuft er nie über `%TEMP%`.

## 5 Altbestand

Gelöscht wurde nur, was dem Muster folgt, mindestens 10 min vor dem ältesten laufenden
Testprozess entstand und keine gesperrte Datei enthielt.

| Durchgang | vorher | gelöscht | stehen gelassen |
|---|---|---|---|
| 1 (13:41) | 1 250 Ordner, 82,5 GB | 1 210 Ordner, 80,0 GB | 40 — jünger als die Grenze |
| 2 (14:07) | 120 Ordner, 7,6 GB | 80 Ordner, 5,0 GB | 40 — jünger als die Grenze |

Kein Ordner war gesperrt, kein Löschen schlug fehl. Die stehen gelassenen und alle später
hinzugekommenen Reste ohne Besitzmarke stammen aus Läufen im Hauptbaum mit einem Stand vor
dem Fix; sie räumt der erste Lauf nach dem nächsten Sync selbst weg, sobald sie älter als
2 h sind. Die rund 70 alten Ordner `epos-erstbereitstellung-*` (zusammen 0,5 MB) blieben
unberührt.

## 6 Abnahme

- Bau `WP-Plan.Kern.slnf` Release: 0 Fehler.
- Volles Gate auf jedem Stand grün, zuletzt auf dem Merge `4df10282` über `c1028989`:
  KiKern.Tests 524, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 (+1 übersprungen),
  EPOS.UI.Tests 5 392, EPOS.Kern.Tests 5 035.
- **Keine neue Kopie aus den eigenen Läufen.** Die Zuordnung gelingt über die Besitzmarke:
  Jeder in den Gate-Fenstern neu entstandene Ordner trug keine Marke und stammte aus dem
  Hauptbaum. Im Gate auf dem Merge entstand überhaupt kein neuer Ordner.
- `%ProgramData%\EPOS_PLAN\Kenndaten.sqlite` blieb unberührt (letzter Schreibzugriff
  21.09.2026): Kein Test ist nach der Rückstellung von `PfadUeberschreibung` auf die
  Anwenderdatenbank ausgewichen.
- `kern.yml` auf ubuntu, Lauf 35870449221: grün.

## 7 Git

- Commits `a202ad65` (Leck, acht Pfade) und `522372b4` (Erstbereitstellung, ein Pfad) auf
  dem Worktree-Zweig `claude/festive-shannon-dabd29`, Merge `4df10282` (erster Elternteil
  `c1028989` = `origin/ios_migration_september`), gepusht als Fast-Forward
  `c1028989..4df10282`.
- Gemergt und geprüft im Worktree `festive-shannon-dabd29`: Der Hauptbaum war die ganze Zeit
  durch die `AGENT_LAEUFT` der Sitzung „Gebaeudesimulation“ gesperrt. Beim ersten Anlauf zog
  `origin` mit der Kühlungswelle KU1 (Schemaschritte 108–110) weiter; der Merge wurde darauf
  neu gesetzt und neu geprüft. `TestDatenbank.cs` ließ sich automatisch zusammenführen, und
  die beiden neuen Testklassen der Welle entsorgen ihre Vorrichtung.

## 8 Nebenbefunde

- `%TEMP%\claude` belegte rund 134 GB, fast alles in zwei Sitzungen vom 25.08.2026 eines
  anderen Projekts (`…EPOS-Plan-Tools-Aktionsplan`, 54,8 und 44,5 GB) — nicht angefasst.
- Jeder Aufruf von `dotnet test` legt im `TEMP` zwei leere Zufallsordner und ein
  Workload-Protokoll der `dotnet`-Kommandozeile an — nicht aus dem Testcode, harmlos.
- In frisch angelegten Worktrees liegt `Referenzlaeufe/Kenndaten_Test.sqlite` zunächst als
  LFS-Zeiger; `git lfs checkout Referenzlaeufe/Kenndaten_Test.sqlite` holt die Datei ohne Netz
  aus dem gemeinsamen LFS-Speicher.
