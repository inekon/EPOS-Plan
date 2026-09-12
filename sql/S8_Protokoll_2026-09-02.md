# S8 — Erststart-Assistent, Settings-Fixup, Betriebsdoku

**Datum:** 02.09.2026 · **Repo:** `WP-Plan`, Branch `sqlite`
**Referenz:** `Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`, Abschnitt 8
**Vorstand:** S0–S7 fertig (S7 bitgleich bestanden, Proben 15/15)
**Nicht Gegenstand:** der Cutover selbst. `C:\ProgramData\EPOS_PLAN\` wurde **nicht
angefasst** — dort liegt nach wie vor keine `Kenndaten.sqlite`, nichts wurde umbenannt,
und die Einstellungen des Anwenders wurden nicht geschrieben.

**Ergebnis in einem Satz:** Der Erststart-Assistent steht headless und kopfüber
nachgewiesen (Probe **Fall 16: 16/16**, Migration **114/114**, `integrity_check: ok`,
`foreign_key_check: keine Verletzung`), die Anwendung baut mit der neuen Referenz auf den
Migrationskern (**0 Fehler, exakt 5 Bestandswarnungen** — sowohl über die Proben als auch
über `WP-Plan.sln`), und `BETRIEB_SQLITE.md` liegt vor.

---

## 1. Was gebaut wurde

### 1.1 `WindowsFormsApplication1\Allgemein\Update\ErststartMigration.cs` (neu)

Die gesamte Entscheidungs- und Ablauflogik, **ohne jede Oberfläche** — die Klasse kennt
weder `Form` noch `MessageBox`. Genau deshalb ist Fall 16 möglich.

| Bestandteil | Bedeutung |
|---|---|
| `enum ErststartLage` | `SqliteVorhanden` · `NurAccdbVorhanden` · `BeidesFehlt` |
| `Pruefe(dbOrdner)` | reine Dateiprüfung, öffnet nichts, verändert nichts |
| `Fuehredurch(dbOrdner, IProgress<string>, settingsFixup, out berichtPfad)` | der Ablauf (a)–(d) |
| `LetzteMeldung` | Klartext zum letzten Aufruf — Erfolg, Grund des Fehlschlags oder „Nichts zu tun" |
| `LetzteTabellen` / `LetzteTabellenOk` / `LetzteZeilen` | Kennzahlen des Datenbeweises |
| `StandardOrdner()` | Ordner aus `DataRepository.GetDBPath()` (trägt `DBPath` **und** den Proben-Haken `PfadUeberschreibung` mit) |

**Ablauf `Fuehredurch`:**

* **(a)** `SchemaMigration.HebeAltbestand(<ordner>\Kenndaten.accdb, out bericht)` — hebt
  in-place auf Stand 61. Fehlschlag ⇒ `false`, Protokollpfad in der Meldung, **nichts
  angelegt**.
* **(b)** `EposSqliteMigrator.Kern.Migrator` nach `<ordner>\Kenndaten.sqlite`,
  `orphanPolicy = Abbruch`, Bericht daneben. Fehlschlag ⇒ Kern löscht das Ziel, `.accdb`
  bleibt, `false` mit dem Fehlertext des Kerns.
* **(c)** Erst nach Erfolg: `Kenndaten.accdb` → **`Kenndaten.vor-sqlite.accdb`**.
* **(d)** Nur bei `settingsFixup == true`: `Properties.Settings.Default.DBName =
  "Kenndaten.sqlite"` + `Save()`.

**Drei bewusste Entscheidungen, die im Bauplan offen waren:**

1. **Eine misslungene Umbenennung (c) kippt den Lauf nicht.** Die Migration ist zu diesem
   Zeitpunkt nachgewiesen gelungen; die Umbenennung ist Beleg und Rückfallebene, nicht
   Voraussetzung. Sie wird als Hinweis gemeldet (Fortschritt **und** `LetzteMeldung`),
   der Rückgabewert bleibt `true`. Andernfalls bräche der Start ab, obwohl die Datenbank
   in Ordnung ist — und der nächste Start liefe wegen `SqliteVorhanden` sowieso durch.
2. **Zwischen (a) und (b) wird die ACE-Sitzung erzwungen freigegeben**
   (`OleDbConnection.ReleaseObjectPool()` + GC + Warten auf das Verschwinden der
   `.laccdb`, höchstens ~4 s). Ohne das hielte der Verbindungspool die `.accdb` offen,
   die Sperrdatei bliebe liegen und der Migrator bräche — zu Recht — mit
   `ExitCode.SitzungOffen` ab. Gewartet, **nie gelöscht**: Hält ein anderer Prozess die
   Datei, ist der Abbruch die richtige Antwort.
3. **Eine bereits vorhandene `Kenndaten.vor-sqlite.accdb` führt zum Abbruch**, statt die
   Rückfallebene eines früheren Laufs zu überschreiben. Aus demselben Grund zählt sie in
   `Pruefe` **nicht** als Altbestand: Wer sie erneut migrieren will, benennt sie von Hand
   zurück.

**Der `IProgress<string>`-Haken im Kern war nicht nötig.** `EposSqliteMigrator.Kern.Migrator`
nimmt seinen Fortschrittsempfänger seit S3 als `Action<string>` im Konstruktor entgegen
(`new Migrator(Console.WriteLine)` in der Konsolenfassung). Das `IProgress<string>` des
Assistenten wird schlicht daran gehängt (`new Migrator(z => fortschritt.Report(z))`). Ein
zweiter Haken für dieselbe Sache wäre eine zweite Wahrheit — **`EposSqliteMigrator\` wurde
in S8 nicht angefasst**.

### 1.2 `WindowsFormsApplication1\Views\Admin\Form_Erststart.cs` (neu)

Titel **„Datenbankumstellung"**, Kopftext mit Ordner und dreischrittigem Ablauf,
Statuszeile, **Marquee**-Fortschrittsbalken, mitlaufendes Protokollfeld, Schaltflächen
„Jetzt umstellen" / „Beenden".

* **Kein Abbrechen während des Laufs:** beide Schaltflächen aus, `ControlBox = false`,
  `FormClosing` verweigert `CloseReason.UserClosing`.
* Die Arbeit läuft auf einem eigenen Strang; `Progress<string>` wird auf dem
  Oberflächenstrang erzeugt und stellt damit von selbst dort zu.
* **Kein Designer, keine `.resx`** — Hausmuster wie `Form_KiHinweis`/`Form_KiChat`. Damit
  kann die bekannte Designer-DPI-Falle (150 % verschreibt die AutoScale-Basis) hier gar
  nicht greifen.
* Texte als deutsche Literale, wie schon die Startprüfung in `Program.Main`. Der
  Ressourcenbestand (`MyResource.Resource`, Schlüssel `START_ACE_FEHLT_*`) wird mit dem
  übrigen Textbestand der Umstellung in einem Zug nachgezogen — **offener Punkt**.

### 1.3 `WindowsFormsApplication1\Program.cs` — die Verdrahtung

**Befund zum Ist-Zustand:** `DataRepository.DatenbankVorhanden()` prüft seit S4a die
**SQLite**-Datei. Auf einem Bestandsrechner gibt es die beim allerersten Start dieser
Fassung noch gar nicht — die Prüfung schlüge also fehl, bevor der Assistent überhaupt zum
Zug käme. Die Reihenfolge musste deshalb minimal umgestellt werden:

```
if (!DataRepository.DatenbankVorhanden())
{
    if (!ErststartAnbieten()) return;
}
```

`ErststartAnbieten()` (neu, direkt unter `Main`):

| Lage | Verhalten |
|---|---|
| `NurAccdbVorhanden` | `Form_Erststart.Zeigen(ordner, settingsFixup: true, out berichtPfad)` |
| sonst | **unverändert** die bisherige Meldung „Datenbankdatei nicht gefunden/lesbar: …" und Ende |

„Sonst" ist `BeidesFehlt` — und der Randfall `SqliteVorhanden`: Die Datei liegt da,
lässt sich aber nicht öffnen (beschädigt, gesperrt, kein Leserecht). Dann ist die alte
Meldung genau die richtige; eine Migration wäre dort falsch.

* Erfolg ⇒ die Startprüfung wird **wiederholt**; erst ein zweites `DatenbankVorhanden()`
  beweist, dass die neue Datei auch zu öffnen ist. Danach läuft alles Weitere unverändert:
  Lizenzzustimmung, `KiTextlieferant`, `SchemaMigration.Ausfuehren` (jetzt auf der
  frischen SQLite-Datei), MDI-Oberfläche.
* Fehlschlag ⇒ eine Meldung mit `ErststartMigration.LetzteMeldung` **und dem
  Berichtspfad**, dann Ende.

Damit ist beides erfüllt: Der Assistent greift **vor** der Fehlermeldung der
`DatenbankVorhanden`-Logik und **vor** `SchemaMigration.Ausfuehren`.

Der Settings-Fixup läuft hier mit `true`. Der Vorgriff in `DataRepository.GetDBPath()`
(`*.accdb` ⇒ `Kenndaten.sqlite`) **bleibt als Netz bestehen** — kommt der Fixup nicht
durch (schreibgeschütztes Profil), startet der Bestand trotzdem.

### 1.4 Neuer Vorgabewert `DBName = Kenndaten.sqlite`

Abschnitt 8 verlangt „neuer Default ebenso". Geändert in den drei Dateien, die den
Vorgabewert tragen — jeweils genau ein Vorkommen:

* `WindowsFormsApplication1\Properties\Settings.settings`
* `WindowsFormsApplication1\Properties\Settings.Designer.cs` (`DefaultSettingValueAttribute`)
* `WindowsFormsApplication1\app.config`

**Das sind Werksvorgaben im Repo, keine Anwenderwerte.** Auf diesem Rechner gab es noch
nie eine gespeicherte `user.config` (Prüfstand 29.08.2026, siehe Kopfkommentar der
`.csproj`), der Vorgabewert ist hier also der wirksame. Verhalten ändert sich dadurch
nichts: `GetDBPath()` bog `Kenndaten.accdb` schon vorher auf `Kenndaten.sqlite` um.

### 1.5 Projektverdrahtung

`WindowsFormsApplication1.csproj` bekommt

```xml
<ProjectReference Include="..\EposSqliteMigrator\Kern\EposSqliteMigrator.Kern.csproj" />
```

Beide Projekte sind SDK-Stil, `net8.0-windows` bzw. `net8.0`, beide `x64` — die Referenz
löst ohne Zutun auf. `Microsoft.Data.Sqlite 8.0.11` ist in beiden Projekten dieselbe
Version (Vorgabe aus S4a).

---

## 2. `WP-Plan.sln` — Befund und Entscheidung

**Befund:** MSBuild baut die Referenz **auch ohne Eintrag in der Projektmappe**.
Nachgewiesen durch den Probenbau: `Proben\ZugriffsschichtProben\ZugriffsschichtProben.csproj`
wird direkt (ohne jede `.sln`) gebaut, und `EposSqliteMigrator.Kern.dll` (1 142 272 Bytes,
mit eingebettetem Schema) landet im Ausgabeordner. Die `ProjectReference` genügt.

**Entscheidung: trotzdem eintragen** — für die Bedienung in Visual Studio. Ohne Eintrag
zeigt der Projektmappen-Explorer ein Projekt, das gebaut wird, aber nicht in der Mappe
steht; Öffnen, Debuggen und Umbenennen im Kern gehen dann an der Mappe vorbei.

**GUID-Konvention:** `WP-Plan.sln` führt **alle** ihre (durchweg SDK-Stil-)Projekte unter
dem klassischen C#-Typ-GUID `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`. Der neue Eintrag
folgt dieser Konvention. Die **Projekt-GUID** ist bewusst dieselbe wie in
`EposSqliteMigrator\EposSqliteMigrator.sln`:
`{5B2C1F41-9A34-4D6E-8F21-1C0E7A3B4D01}` — so bezeichnen beide Mappen dieselbe Identität.
Konfigurationszuordnung `Debug|x64` → `Debug|x64` (das Kern-Projekt führt
`Platforms=x64;AnyCPU`), gleich wie bei `WindowsFormsApplication1`; die vier
AnyCPU-Projekte bleiben unberührt.

Das Kern-Projekt steht damit in **zwei** Mappen. Das ist gewollt: `EposSqliteMigrator.sln`
bleibt die Mappe für die Konsolenfassung (Kundenbestände), `WP-Plan.sln` braucht den Kern
als Baustein der Anwendung.

---

## 3. `BETRIEB_SQLITE.md` (Repo-Wurzel, neu)

Stil und Ton wie `BETRIEB_Mehrbenutzer_Datenbank.md`, mit den echten Pfaden und
Kommandos. Acht Abschnitte:

1. **Erststart** — Assistent, die drei Schritte, Bericht, Rückfallebene
   `Kenndaten.vor-sqlite.accdb`, Verhalten im Fehlerfall, „läuft genau einmal je Bestand".
2. **Die drei Dateien** — `.sqlite` / `-wal` / `-shm`, warum der aktuelle Stand die Summe
   aus Hauptdatei **und** WAL ist, und dass `-wal`/`-shm` nie einzeln kopiert, verschoben
   oder gelöscht werden.
3. **Sicherung** — geschlossene App: Dateikopie (mit der Probe „liegt ein `-wal`
   daneben?"); im Betrieb `VACUUM INTO` (Zieldatei darf nicht existieren); Ablage
   `DB-Backup\` trägt unverändert, Berichte nach `sql\`.
4. **Zwei Windows-Konten** — WAL + `busy_timeout = 5000` (je Verbindung in
   `DataRepository.OeffneVerbindung`), die `icacls`-Zeile aus
   `BETRIEB_Mehrbenutzer_Datenbank.md` **bleibt nötig** und wird mit WAL sogar wichtiger,
   weil `-wal`/`-shm` zusätzlich angelegt werden. Netzlaufwerk ausgeschlossen.
5. **Werkzeuge** — SQLiteStudio (`C:\Program Files (x86)\SQLiteStudio`), DBeaver (ER als
   Ersatz fürs Beziehungsfenster), `sqlite3.exe`; SQLite **≥ 3.37** wegen `STRICT`;
   Verweis auf `sqlite-probe\`; Warnung, dass Fremdschlüssel in Werkzeugen je Sitzung aus
   sind.
6. **Kundenbestände** — `EposSqliteMigrator.exe` mit allen Schaltern, `orphanPolicy`
   ausführlich (`AlsProtokollAussetzen` lässt den Constraint **bestehen** und hält die
   Verletzung aus), Voraussetzungen (Stand 61, keine `.laccdb`, nur lesend, Ziel wird nie
   überschrieben, 64-Bit-ACE), Exit-Codes 0–4, „der Bericht ist die Abnahme", Cutover je
   Rechner getrennt.
7. **Wiederherstellung** — Sicherung zurückholen (umbenennen statt löschen), Rückweg auf
   Access samt der klaren Ansage, dass alles seit der Umstellung Erfasste dabei verloren
   geht, Verhalten nach einem Absturz.
8. **Wo was steht** — Verweise auf `sql\LIESMICH.md`, `BETRIEB_Mehrbenutzer_Datenbank.md`,
   `BETRIEB_Installer_Hinweise.md`, das Implementierungskonzept und `sqlite-probe\`.

**Nebenbei in `sql\LIESMICH.md` nachgetragen:** Die Protokolltabelle dort endete bei S5.
Da `BETRIEB_SQLITE.md` auf diese Datei als Wegweiser verweist, sind die drei fehlenden
Zeilen ergänzt worden — `S3_Migrationsbericht_Rechner1_2026-09-02.md`,
`S7_Protokoll_2026-09-02.md` und dieses Protokoll. Sonst nichts an der Datei geändert.

---

## 4. Probe Fall 16 — der Erststart-Assistent, kopfüber

`Proben\ZugriffsschichtProben\Program.cs`, neuer `Fall16Erststart(args, arbeitsordner)`,
Aufruf direkt nach Fall 15. Schalter wie dort: `--altbestand=<Pfad>`, Vorgabe
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`; SKIP **mit Grund**, wenn die Datei fehlt oder
`Microsoft.ACE.OLEDB.12.0` nicht verfügbar ist.

**Wächter vor der Kopie:** keine `.laccdb` neben dem Altbestand — sonst wäre die Kopie
ein halber Stand; sonst SKIP mit Grund. Gearbeitet wird ausschließlich auf der Kopie in
`<Arbeit>\fall16\`; die Live-Datenbank wird nur **gelesen**.

**Geprüft wird:**

| Prüfung | Erwartung |
|---|---|
| `Pruefe` vor dem Lauf | `NurAccdbVorhanden` |
| `Fuehredurch(…, settingsFixup: **false**, …)` | `true`, mindestens eine Fortschrittsmeldung |
| `Kenndaten.sqlite` | entstanden |
| `Kenndaten.accdb` | **weg** |
| `Kenndaten.vor-sqlite.accdb` | da |
| Migrationsbericht | Pfad geliefert, Datei vorhanden, enthält „Datenbeweis bestanden" |
| Datenbeweis | `LetzteTabellenOk == LetzteTabellen`, > 0 |
| `Pruefe` nach dem Lauf | `SqliteVorhanden` |
| **zweiter** `Fuehredurch` | `false`, kein Berichtspfad, Meldung enthält „Nichts zu tun" |
| Wirkung des zweiten Aufrufs | Größe **und** Schreibzeitpunkt der `.sqlite` unverändert, Dateinamen unverändert |
| gespeicherter `DBName` | vorher == nachher (`settingsFixup` war `false`) |

Zwei Kleinigkeiten dazu:

* **Kein `Progress<T>` in der Probe.** Ohne Oberflächenkontext stellte der seine Meldungen
  über den Threadpool zu — die Liste wäre beim Prüfen noch nicht fertig. Statt dessen ein
  vierzeiliger `Sammler : IProgress<string>`, der synchron anhängt.
* **Der `DBName`-Vergleich läuft per Reflexion** (`Properties.Settings` ist `internal`),
  **nur lesend**, und ist tolerant: Lässt sich der Wert nicht lesen, ist das kein
  Fehlschlag, der Vergleich wird dann eben wirkungslos. Der ausgegebene Wert
  `Kenndaten.sqlite` ist die neue Werksvorgabe aus 1.4 — der Probenprozess hat keine
  eigene `user.config`.

Der Ordner `fall16` wird danach **vollständig geräumt** (144-MB-Kopie, `.sqlite`,
Bericht). Die Kennzahlen des Migrators stehen vorher in der Konsolenausgabe — deshalb
zitiert `BerichtKennzahlen` die Kopfzeilen des Berichts, bevor er verschwindet.
Nachgemessen: `<Arbeit>\fall16\` existiert nach dem Lauf nicht mehr.

### 4.1 Ausgabe des Probenlaufs (wörtlich)

```
Quelle       : …\scratchpad\s7\Kenndaten_S7_v2.sqlite
Arbeitskopie : …\scratchpad\s8_proben\Kenndaten_Probe.sqlite

PASS  1  ?->@pN-Uebersetzung
PASS  2  NormalisiereWert
PASS  3  GetDataTable Tab_Projekt (Zeilen, Int32, Umlaut, Datum)
PASS  4  Boolean-Spalte kommt als bool an (Tab_Energieanlagen.Bivalenter_Betrieb)
PASS  5  Namensdubletten aus Joins entdoppelt (ID, ID1)
PASS  6  ExecuteScalar und GetIdByName
PASS  7  ExecuteInsertAndGetId auf energy_carrier (Schreiben auf der Kopie)
PASS  8  DbVorgang: Rollback / Commit / Dispose ohne Commit
PASS  9  Schema-Auskunft
Datenbankfehler im Simulationslauf (ohne Dialog): Datenbankfehler: SQLite Error 19: 'FOREIGN KEY constraint failed'.
PASS  10 Fremdschluessel greifen (INSERT mit unbekanntem Elternwert)
PASS  11 DataRepository besitzt keine Methode BeginTransaction mehr
PASS  12 DatenbankVorhanden
PASS  13 SchemaMigration.Ausfuehren faehrt NUR den SQLite-Zweig
PASS  14 synthetischer SQLite-Schritt 62 ueber SqliteDdl (Marker + Idempotenz)
       (Fall 15 kopiert 144 MB - das dauert.)
PASS  15 HebeAltbestand auf einer Kopie der Live-.accdb (Access-Zweig)
       (Fall 16 kopiert 144 MB und migriert sie - das dauert einige Minuten.)
        Bericht: …\scratchpad\s8_proben\fall16\Migrationsbericht_Kenndaten_20260902_142441.md
          | Schemastand | 61 |
          | Tabellen migriert | 114 |
          | Zeilen gesamt (Ziel) | 1.392.013 |
          | Exit-Code | 0 (Erfolg) |
          **Datenbeweis bestanden:** alle 114 Tabellen mit gleicher Zeilenzahl und gleicher Inhaltspruefsumme.
          - `PRAGMA integrity_check`: **ok**
          - `PRAGMA foreign_key_check`: **keine Verletzung**
        Migrator: 114/114 Tabellen bewiesen, 1.392.013 Zeilen, Zieldatei 64 MB.
        Settings.DBName unveraendert: Kenndaten.sqlite
PASS  16 Erststart-Assistent auf einer Kopie der Live-.accdb (S8)

Still gesammelte Datenbankmeldungen (1):
  * Datenbankfehler: SQLite Error 19: 'FOREIGN KEY constraint failed'.

Ergebnis: 16/16 bestanden.
```

Rückgabewert des Probenlaufs: **0**. Die eine still gesammelte Meldung ist die
**erwartete** Meldung aus Fall 10 (der Fremdschlüssel greift) — unverändert gegenüber
S5/S7.

**Die Kennzahlen decken sich mit dem S3-Echtlauf** auf demselben Bestand: 114 Tabellen,
1 392 013 Zeilen, Zieldatei 64,6 MB, Exit 0. Der Assistent liefert also dasselbe wie die
Konsolenfassung — es ist derselbe Kern.

---

## 5. Bau und Abnahme

Werkzeug durchgängig **VS-2022-MSBuild 17.14.51.32402**
(`C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe`).
`bin\x64\Debug` wurde **nie** überschrieben — jeder Bau schreibt per `OutputPath` in den
Scratchpad.

| Bau | Ziel | Ergebnis |
|---|---|---|
| `Proben\ZugriffsschichtProben\ZugriffsschichtProben.csproj`, `/t:Restore`, dann `/p:Configuration=Debug /p:Platform=x64 /p:OutputPath=<Scratch>\s8_bin\` | 5 Projekte wiederhergestellt (vorher 4 — der Kern ist dazugekommen) | **0 Fehler**, **5 Warnung(en)** |
| `WP-Plan.sln`, `/t:Restore`, dann `/p:Configuration=Debug /p:Platform=x64 /p:OutputPath=<Scratch>\s8_sln_bin\` | 6 Projekte | **0 Fehler**, **5 Warnung(en)** |

**Die 5 Warnungen sind exakt die bekannten Bestandswarnungen** — dieselben wie in S7,
keine neue:

| Code | Anzahl | Stelle |
|---|---|---|
| CS0108 | 2 | `Model\WErzeugerModel.cs(6,20)`, `Controller\StromverbraucherStammCtrl.cs(25,44)` |
| CS0109 | 2 | `Controller\KlimaregionStammCtrl.cs(22,24)` und `(23,48)` |
| CS1998 | 1 | `MDIMainForm.cs(489,28)` |

**Nachweis, dass der Kern im Solution-Bau mitläuft** (aus dem Buildprotokoll):

```
Das Projekt "…\WindowsFormsApplication1.csproj" (2) erstellt
"…\EposSqliteMigrator\Kern\EposSqliteMigrator.Kern.csproj" (5:2) auf Knoten "1"
  EposSqliteMigrator.Kern -> …\s8_sln_bin\EposSqliteMigrator.Kern.dll
```

Ausgabeordner beider Bauten enthalten `EposSqliteMigrator.Kern.dll` (1 142 272 Bytes)
neben `EPOS_Plan.dll`, `KiKern.dll`, `SpeicherEngine.dll` und den
`Microsoft.Data.Sqlite`/`SQLitePCLRaw`-Beilagen.

**Probenlauf:** `16/16 bestanden`, Rückgabewert 0 (Abschnitt 4.1).

---

## 6. Kodierungsbefunde (Rezept 9)

Vor der ersten Änderung wurde **jede** Datei gemessen: BOM-Probe, strikte
`utf-8`-Dekodierprobe, CRLF-/LF-Zählung. **Keine einzige der berührten Dateien war
cp1252** — das Edit-Werkzeug kam damit nur auf unbedenkliche Dateien zum Einsatz. Die
neuen Dateien wurden byte-treu über Python geschrieben (BOM und Zeilenenden gesetzt,
nicht dem Editor überlassen).

| Datei | Art | BOM | Zeilenenden | Nicht-ASCII-Bytes nachher |
|---|---|:--:|---|---:|
| `WindowsFormsApplication1\Allgemein\Update\ErststartMigration.cs` | **neu** | ja | rein CRLF (424) | 153 |
| `WindowsFormsApplication1\Views\Admin\Form_Erststart.cs` | **neu** | ja | rein CRLF (273) | 65 |
| `WindowsFormsApplication1\Program.cs` | geändert | ja | rein CRLF (620 → 692) | 391 |
| `WindowsFormsApplication1\WindowsFormsApplication1.csproj` | geändert | ja | rein CRLF (279 → 288) | 64 |
| `WindowsFormsApplication1\Properties\Settings.settings` | geändert | ja | rein CRLF (32) | 3 |
| `WindowsFormsApplication1\Properties\Settings.Designer.cs` | geändert | ja | rein CRLF (134) | 7 |
| `WindowsFormsApplication1\app.config` | geändert | nein | rein CRLF (43) | 0 |
| `WP-Plan.sln` | geändert | ja | rein CRLF (49 → 55) | 3 |
| `Proben\ZugriffsschichtProben\Program.cs` | geändert | ja | rein CRLF (985 → 1219) | 9 |
| `BETRIEB_SQLITE.md` | **neu** | nein | rein CRLF (262) | 322 |
| `sql\S8_Protokoll_2026-09-02.md` | **neu** | nein | rein LF | — |
| `sql\LIESMICH.md` | 3 Zeilen ergänzt | nein | rein LF (28 → 31) | unverändert utf-8 |

**Umlaut-Stichproben nach jeder Änderung** — alle intakt: `Rückfallebene`, `über`, `für`,
`ließ`, `„Datenbankdatei"` (ErststartMigration) · `hinterließe`, `Fußzeile`, `Während`,
`läuft` (Form_Erststart) · `verfügbar`, `lässt sich aber nicht`, `Oberfläche`, `nötig`
(Program.cs) · `Fremdschlüssel`, `künftige` (BETRIEB_SQLITE). In
`Proben\…\Program.cs` sind die einzigen Nicht-ASCII-Zeichen nach wie vor die drei `ö` aus
`"Wöhler WP"` (Zeilen 195/283) — die Ergänzung selbst ist reines ASCII, wie der
Bestandsstil dort.

Zwei Zeilenenden-Konventionen wurden bewusst getrennt gehalten: Wurzel-`BETRIEB_*.md`
tragen **CRLF** (wie `BETRIEB_Mehrbenutzer_Datenbank.md` und
`BETRIEB_Installer_Hinweise.md`), `sql\*.md` tragen **LF** (wie S2/S3/S5/S7).

---

## 7. Was nicht angefasst wurde

* **`C:\ProgramData\EPOS_PLAN\`** — Verzeichnisstand vor und nach allen Läufen
  nachgemessen: acht Einträge, gleiche Größen, gleiche Zeitstempel
  (`Kenndaten.accdb` 151 949 312 Bytes · 01.09.2026 15:16:48). **Keine
  `Kenndaten.sqlite` angelegt, nichts umbenannt.**
* **Die echten Anwender-Einstellungen** — `Settings.Save()` wurde nie ausgeführt; Fall 16
  fährt mit `settingsFixup = false` und misst genau das nach.
* **`bin\x64\Debug`** — beide Bauten schreiben per `OutputPath` in den Scratchpad.
* **`SchemaMigration.cs`** — kein Schrittkörper, keine Zeile.
* **`EposSqliteMigrator\`** — der Kern brauchte keinen zusätzlichen Haken (1.1).
* **Kein `git`-Kommando**, keine UI-App gestartet.

---

## 8. Offene Punkte

| Nr. | Punkt | Stand |
|---|---|---|
| **O1** | **Testinstallation auf einer sauberen VM** (Abnahme aus Abschnitt 9 des Konzepts) | **manuell durch den Nutzer** — der Assistent ist headless bewiesen, die Oberfläche selbst nicht |
| **O2** | **Cutover je Rechner** (Migration von `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`, Umbenennung, `DBName`) | **auf Freigabe** — bewusst nicht ausgeführt |
| **O3** | Ressourcenschlüssel `START_ACE_FEHLT_*` und die Texte des Assistenten in `MyResource.Resource` überführen (heute deutsche Literale) | offen, zusammen mit dem übrigen Textbestand |
| **O4** | **S6-Befunde in den Schrittkörpern 42 und 43** — `BrennstoffStammId(…)` und `Schritt_43_VdiTraeger(…)` lesen über `DataRepository`, also über die SQLite-Datei statt über die gerade gehobene `.accdb` | **unverändert offen.** Beides reine Leseproben mit Rückfall (`0` bzw. `"GASEOUS_FUEL"`). Im Erststart-Ablauf existiert die SQLite-Datei zum Zeitpunkt der Hebung noch nicht — die Proben laufen dann in den stillen Fehlerpfad und liefern eben diesen Rückfall. Betrifft nur Bestände **unterhalb** Stand 43; die beiden Rechner stehen auf 61, Fall 15 und Fall 16 melden durchgehend „bereits erledigt". |
| **O5** | Bedienbeweis (7.3) und Frontend-Beweis (7.4) aus dem Konzept | weiter offen, nicht Gegenstand von S8 |

---

## 9. Pfade

| Was | Wo |
|---|---|
| Proben-Bau | `<Scratch>\s8_bin\ZugriffsschichtProben.exe` |
| Probenlauf-Protokoll | `<Scratch>\s8_probenlauf.txt` |
| Arbeitsordner der Proben | `<Scratch>\s8_proben\` (`fall16\` nach dem Lauf geräumt) |
| Quelle des Probenlaufs | `<Scratch>\s7\Kenndaten_S7_v2.sqlite` (die S7-Fassung) |
| Solution-Bau | `<Scratch>\s8_sln_bin\` |
| Buildprotokolle | `C:\Users\DirkEngelmann\s8_build_proben.log`, `…\s8_build_sln.log` |

`<Scratch>` = `C:\Users\DirkEngelmann\AppData\Local\Temp\claude\C--Users-DirkEngelmann-Documents-Tools-epos-downloads-EPOS-Plan-Tools-Aktionsplan\462d1d2b-408a-4539-ae35-f477e5650afd\scratchpad`
