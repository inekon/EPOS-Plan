# Paket 1 — Schema und Migration: Umsetzungsprotokoll

Umsetzung von [`ADR-001_Schema-Ausrollung.md`](ADR-001_Schema-Ausrollung.md) und der
Datenmodell-Kapitel des [Konzepts Quellen/Senken](Konzept_Simulation_QuellenSenken.md)
(5.1, 5.3, 6.6, 12, 13.7).

| Etappe | Umfang | Stand |
|---|---|---|
| **1** | Migrationsmechanismus + Schemaschritte 1–4 | **umgesetzt (14.08.2026)** |
| **2** | Schritt 5: Projektdatenmigration Quellen/Senken (Konzept 5.5) | **umgesetzt (14.08.2026)** |
| **3** | Stilllegung des Alt-Parameters `Tab_Einstellungen.Pendelspeicher` | **umgesetzt (14.08.2026)** |
| **4** | Vor-/Rücklauf je Puffer vorgebbar, Vorbelegung aus den Systemvorgaben, Niedertemperatur zugelassen | **umgesetzt (14.08.2026)** |
| **Nacharbeit** | [Zwölf Befunde aus dem Review](#nacharbeit-zum-review-14082026): Bootstrap, sichtbare Blockade, Schreibpfad, Validierung, Rücknahme, R1/R6, Schritt-4-Reihenfolge, R4-Reparatur, Projektlöschung, Dokumentation | **umgesetzt (14.08.2026)** |

---

## Etappe 1 (14.08.2026)

### 1. Der Mechanismus

Neuer Ordner `Allgemein/Update/` mit zwei Dateien:

| Datei | Inhalt |
|---|---|
| `SchemaKatalog.cs` | **Eine** Quelle für alle additiv angelegten Spalten (ADR-Aufgabe 4). `Bestand` (14) + `Schritt1_Energieanlagen` (15) + `Schritt2_Speicher` (9) = 38 Spalten, überschneidungsfrei. Dazu `SchemaVersionSpalte` als Bootstrap-Definition. |
| `SchemaMigration.cs` | Schrittregister, Marker-Handling, Sammelfehlerbericht, Protokolldatei, Blockade-Abfrage. |

**Ablauf von `SchemaMigration.Ausfuehren(out string fehlerbericht)`**

1. `DataRepository.GetDBPath()` ermitteln — der Pfad ist die **erste Zeile** jedes
   Berichts. Fehlt die Datei, endet der Lauf mit einer sprechenden Meldung statt mit
   einer Ausnahme.
2. Eigene, **stille** `OleDbConnection` über `DataRepository.GetConnectionString()`.
   Bewusst nicht über die `DataRepository`-Methoden: die zeigen bei Fehlern
   MessageBoxen und verschlucken den Fehlertext, womit sich „Spalte existiert schon"
   nicht von „Datei schreibgeschützt" unterscheiden ließe. Der Pfad kommt trotzdem von
   dort, damit der offene Punkt O6 gegenstandslos bleibt.
3. **Bootstrap:** `Tab_Applikation.SchemaVersion LONG DEFAULT 0` anlegen; existiert
   keine Zeile in der Einzelzeilen-Statustabelle, eine anlegen; leere Marker auf 0
   ziehen. Wie die Zeile angelegt wird, steht in
   [Nacharbeit, Abschnitt 1](#1-bootstrap-der-statuszeile-fix-1) — die ursprüngliche
   Fassung (`ID = 1`, Rückfallweg ohne `ID`) konnte auf einer leeren Tabelle nicht
   gelingen und ist korrigiert.
4. `ApplikationCtrl.GetSchemaVersion()` lesen, alle Schritte mit `Nr > Version` in
   Reihenfolge ausführen.
5. Marker **nach** jedem nachgewiesen erfolgreichen Schritt anheben
   (`ApplikationCtrl.SetSchemaVersion`). Schlägt schon das Fortschreiben fehl, gilt der
   Schritt als gescheitert.
6. **Beim ersten Fehler anhalten** — ein halb migriertes Schema wird nie als fertig
   fortgeschrieben.
7. Best-effort-Protokoll `migration_protokoll.txt` neben der Datenbank. Schreibfehler
   dort blockieren nichts (im Schreibschutzfall ist genau das der Normalfall).

**„Existiert bereits" ist kein Fehler.** Erkannt wird das primär an der
sprachunabhängigen Jet-/ACE-Fehlernummer (`OleDbError.SQLState`), ersatzweise am
Meldungstext (deutsch und englisch):

| SQLState | Bedeutung |
|---|---|
| 3010 | Tabelle existiert bereits |
| 3283 | Primärschlüssel existiert bereits |
| 3375 | Index existiert bereits |
| 3378 | Beziehung dieses Namens existiert bereits |
| 3380 | Feld existiert bereits |

Zur Abgrenzung: 3379 („Existing data … violates referential integrity rules") gilt
ausdrücklich **als Fehler** — an der Datenlage darf nichts stillschweigend vorbeilaufen.

**Zustand nach dem Lauf** (statisch, für die Blockade):
`SchemaMigration.MigrationOk`, `.Fehlerbericht`, `.Ausgefuehrt`, `.StandVorher`,
`.StandNachher`, `.IdPufferGemappt`, `.IdPufferGenullt`.
`MigrationOk` ist **vor** dem ersten Lauf `true` — Werkzeuge, die die Migration gar
nicht anstoßen (Referenzlauf-Suite), werden dadurch nicht blockiert.

### 2. Der gemeinsame Spaltenkatalog (ADR-Aufgabe 4)

`WaermequelleClass.SchemaSicherstellen()` bleibt als Rückfallebene bestehen —
**unveränderte öffentliche API**, weiterhin still (nur `Console.WriteLine`),
idempotent, ohne Rückgabewert. Geändert hat sich nur die Innenseite: statt einer
zweiten, handgepflegten Spaltenliste iteriert die Methode jetzt über
`SchemaKatalog.Alle` und liest das Tabellenschema je Tabelle einmal.

Nebenbei behoben (Konzept 5.6): die private Überladung hatte `Tab_Energieanlagen`
**hartkodiert** und war damit für `Tab_Pufferspeicher`, `Tab_Klimaregion` und
`Tab_Einstellungen` unbrauchbar. Sie bekommt die Tabelle jetzt übergeben.

**Kein `DEFAULT 0` auf den FK-Spalten.** Konzept 12 nennt für `WS_ID_Puffer`,
`WS_ID_Puffer2` und `WQ_ID_Puffer` „Default 0". Eine 0 verletzt aber die in Schritt 4
angelegte erzwungene Beziehung (0 ist keine gültige `Tab_Pufferspeicher.ID`, `NULL`
dagegen ist zulässig). „Nicht gesetzt" wird deshalb durch `NULL` ausgedrückt; lesender
Code behandelt `NULL` wie 0.

### 3. Die Schritte

| Nr | Name | Inhalt | Marker danach |
|---|---|---|---|
| 1 | Spalten in `Tab_Energieanlagen` (Konzept 5.3) | 15 Spalten: `WS_Ziel` TEXT(50), `WS_ID_Puffer` LONG, `WS_Ladeprio` LONG, `WS_Ladegrenze` DOUBLE, `WS_Ladeprio_PV` LONG, `WS_Ziel2` TEXT(50), `WS_ID_Puffer2` LONG, `WS_Ladeprio2` LONG, `WS_Ladegrenze2` DOUBLE, `WQ_ID_Puffer` LONG, `WQ_Tiefe` DOUBLE, `WQ_Flaeche` DOUBLE, `WQ_Anzahl` LONG, `WQ_Bodentyp` TEXT(50), `WQ_Quellsystem` TEXT(50) | 1 |
| 2 | Spalten in `Tab_Pufferspeicher`, `Tab_Klimaregion`, `Tab_Einstellungen` (5.1/12) | 7 Puffer-Spalten (`Verwendung`, `Vorlauf`, `Ruecklauf`, `Schwelle_Ein`, `Schwelle_Aus`, `Schwelle_Aus_Nachrang`, `Entladeprio`) + `Tab_Klimaregion.Klimazone_DIN4710` LONG DEFAULT 0 + `Tab_Einstellungen.Extrapolation_erlaubt` YESNO | 2 |
| 3 | Ergebnistabelle (Konzept 6.6) | `CREATE TABLE Tab_ErgebnisPufferspeicher` (13 Spalten), `CREATE INDEX idx_ErgPuffer`, `ADD CONSTRAINT FK_ErgPuffer … ON DELETE CASCADE` | 3 |
| 4 | Beziehungen (5.3 / B0-6b) | Datenbereinigung + 4 restriktive FKs auf `Tab_Pufferspeicher.ID` + 1 kaskadierender FK `Tab_Projekt.ID → Tab_Pufferspeicher.ID_Projekt` | 4 |
| 5 | *Datenmigration Quellen/Senken (5.5)* | in Etappe 1 **nicht registriert**; nachgereicht in Etappe 2 | (5) |

`ZIEL_VERSION = 4` (seit Etappe 2: **5**). Die Schritte 1 und 2 gehen idempotent über die fünf
Erdreich-Spalten aus Paket 3 und über `Klimazone_DIN4710` hinweg, die in gepflegten
Datenbanken bereits existieren.

**`Extrapolation_erlaubt` wird angehängt, nicht eingefügt.** Nachgeprüft: nach der
Migration hat `Tab_Einstellungen` 24 Spalten, die neue steht an Position 23 (0-basiert)
— das positionsbasierte `row[0]…row[22]` in `KonfigurationCtrl.ReadSingle` bleibt
unberührt. `ALTER TABLE … ADD COLUMN` hängt in Access grundsätzlich hinten an; der
Nachweis prüft das trotzdem bei jedem Testlauf mit.

### 4. Bewusste Abweichung vom Konzept-Wortlaut: restriktive Beziehungen

Konzept 5.3 verlangt für die drei neuen ID-Spalten „eine erzwungene Beziehung auf
`Tab_Pufferspeicher.ID` **mit Aktualisierungs- und Löschweitergabe** — Vorbild ist
`Z_ProjektPufferSp.ID_Pufferspeicher`".

**Umgesetzt wurde stattdessen: erzwungene Beziehung OHNE Löschweitergabe (restriktiv)**
— für `WS_ID_Puffer`, `WS_ID_Puffer2`, `WQ_ID_Puffer` **und** die Nachrüstung von
`ID_PUFFER`.

**Begründung.** Das Vorbild passt nicht. Bei `Z_ProjektPufferSp` steht auf der Kindseite
eine reine *Zuordnungszeile*; verschwindet sie mit dem Speicher, geht nichts verloren.
Bei `Tab_Energieanlagen` steht dort die **Erzeuger-Anlage selbst**. Eine
`DEL-CASCADE` würde beim Löschen eines Pufferspeichers stillschweigend die
referenzierende Wärmepumpe (bzw. BHKW oder Heizkessel) mitlöschen — Datenverlust ohne
Rückfrage, aus der Oberfläche nicht nachvollziehbar, und genau in dem Moment, in dem
ein Anwender „nur den Speicher" entfernen wollte.

Am Schema belegt (Testlauf, Abschnitt 6): der Versuch, einen referenzierten Puffer zu
löschen, endet mit *„The record cannot be deleted or changed because table
'Tab_Energieanlagen' includes related records"* — die Anlage bleibt erhalten.

**Ausnahme B0-6b:** `Tab_Projekt.ID → Tab_Pufferspeicher.ID_Projekt` bekommt sehr wohl
`ON DELETE CASCADE`. Dort ist die Puffer-Projektkopie das Kind und soll mit dem Projekt
verschwinden — das ist der eigentliche Zweck der Beziehung.

**Begleitänderung, damit die restriktiven Beziehungen nichts blockieren.**
`PufferSpCtrl.ProjektWaisenEntfernen()` und `PufferSpCtrl.DeleteFromProjekt()` ermitteln
vor dem `DELETE` die betroffenen Puffer-IDs und setzen `ID_PUFFER`, `WS_ID_Puffer`,
`WS_ID_Puffer2` und `WQ_ID_Puffer` der referenzierenden Anlagen auf `NULL`
(`ReferenzenLoesen`). Das ist heute **verhaltensneutral**: kein Engine-Code liest diese
Spalten. Fehlt eine Spalte (Datenbank noch nicht migriert), wird der Fehler still
übergangen.

**Zweite Begleitänderung — `WizardCtrl` (unvermeidlich).** `Form_PufferSp.cs:101`
schreibt die **STAMM**-ID in `WErzeugerModel.ID_PUFFER` (Konzept 2.3);
`WizardCtrl.Add_WP_Waermeerzeuger` repariert das über `CopyFromStamm`. Schlug die
Auflösung bisher fehl, überlebte die falsche ID stillschweigend — mit der erzwungenen
Beziehung würde stattdessen das `INSERT` der Anlage scheitern. Zwei Zeilen:
`item.ID_PUFFER = (idPuf > 0) ? idPuf : 0;` und im Parameter
`(… PUFFER_TYP && item.ID_PUFFER > 0) ? item.ID_PUFFER : DBNull.Value`. Die
uncommitteten `ID_Carrier`-Änderungen derselben Datei sind nicht berührt.

### 5. `ID_PUFFER`-Bereinigung

Vor dem `ADD CONSTRAINT` müssen die Altwerte stimmen, sonst scheitert es mit
Jet-Fehler 3379. Regeln, je Zeile in `Tab_Energieanlagen`:

| Fall | Behandlung |
|---|---|
| `ID_PUFFER = 0` | → `NULL` (0 ist keine gültige ID; `NULL` verletzt die Beziehung nicht) |
| Wert zeigt auf eine Zeile in `Tab_Pufferspeicher` **mit demselben `ID_Projekt`** wie die Anlage | unverändert |
| sonst: der `Bezeichner` der Anlage identifiziert **genau eine** Projektkopie des Projekts | → auf deren `ID` umgesetzt |
| sonst | → `NULL` |

Der dritte Fall repariert genau die STAMM-IDs aus Konzept 2.3. Der zweite Fall prüft
bewusst **projektgleich**: ein Verweis auf den Speicher eines fremden Projekts verletzt
die Beziehung zwar nicht, wäre in Paket 2 aber ein stiller Datenfehler.

Analog für `Tab_Pufferspeicher` vor der B0-6b-Beziehung: `ID_Projekt = 0` → `NULL`,
danach `DELETE` aller Zeilen, deren `ID_Projekt` auf kein existierendes Projekt zeigt.

**Zählung auf der Arbeitskopie vom 14.08.2026** (122 Puffer-Zeilen, 5 Anlagen mit
gesetztem `ID_PUFFER`):

| Vorgang | Anzahl |
|---|---|
| `ID_PUFFER` auf die Projektkopie umgesetzt (gemappt) | **1** |
| `ID_PUFFER` geleert (genullt) | **0** |
| verwaiste `Tab_Pufferspeicher`-Zeilen entfernt | **4** |

Der eine gemappte Fall: Anlage 10362 (Projekt 1021, „Vitocell 140-E 600 Ltr") trug
`ID_PUFFER = 1022903805` — kein existierender Wert. Im Projekt 1021 gibt es genau eine
Projektkopie dieses Bezeichners (`ID = 1018013`), also wurde darauf umgesetzt. Die
übrigen vier Werte waren bereits gültig und projektgleich.

Die vier entfernten Waisen gehören zu Projekt 1015, das es nicht mehr gibt
(`ID = 1015007, 1015008, 1018010, 1018011`). Keine Zeile in `Z_ProjektPufferSp` und
kein `ID_PUFFER` zeigte darauf.

### 6. Verdrahtung und Blockade (ADR-Aufgabe 6)

| Ort | Änderung |
|---|---|
| `Program.Main` | `SchemaMigration.Ausfuehren()` **nach** der Sprach-/Kultur-Initialisierung, **vor** `new MDIMainForm()`. Bei Fehlschlag **genau eine** MessageBox mit dem Fehlerbericht-Kopf und dem Protokollpfad. Das Programm startet trotzdem. |
| `Form_Simulation_Config.SetControls` | Ganz am Anfang: `SchemaMigration.SimulationGesperrt(out grund)` → Hinweis + `SimulationsbereichSperren()` (deaktiviert die Kindsteuerelemente, **nicht** das Formular selbst — sonst ließe es sich nicht mehr schließen) + `return`. `btn_Speichern_Click` ist nicht angefasst. |
| `SimulationRunner.Simuliere` | Erste Prüfung: Abbruch mit `fehler = grund`, `return false`. |
| `SimulationControl.Do_Simulation` | Früher `return`, Grund in dem neuen Feld `Sperrgrund`; bewusst **ohne** MessageBox (Konzept 13.4: Engine bleibt dialogfrei). |

Fehlt die Datenbankdatei ganz, greift dieselbe Blockade mit der Meldung „Die
Datenbankdatei wurde nicht gefunden. …" — kein Absturz.

### 7. Weitere Punkte der Etappe

**`FK_MAP` in `ProjektDuplizierenCtrl`** (Konzept 5.3/12): `WS_ID_Puffer`,
`WS_ID_Puffer2` und `WQ_ID_Puffer` → `Tab_Pufferspeicher` eingetragen. Seit Schritt 4
sind das echte Access-Beziehungen, die `_echteFks` ohnehin erkennt; der Eintrag ist
Gürtel und Hosenträger für Datenbanken, in denen die Migration noch nicht lief.

**Ordinal-Leser `Form_PufferSp_Bearbeiten.SetControls`** (Paket-1-Restpunkt): `row[2]`
bis `row[6]` auf Namenszugriff umgestellt (`Hersteller`, `Speichertyp`,
`Gesamtvolumen`, `Bereitschaftsverluste`, `Investitionskosten`) über die Helfer
`SetzeText`/`SetzeZahl`, die fehlende Spalten und `DBNull` verkraften. Die Zuordnung
war sachlich korrekt — geprüft an `Tab_Pufferspeicher_STAMM` (ID, Bezeichner,
Hersteller, Speichertyp, Bereitschaftsverluste, Gesamtvolumen, Investitionskosten,
ReadOnly) —, hing aber an der Spaltenreihenfolge.
**Kodierung:** die Datei lag als ISO-8859-1 vor und wurde nach **UTF-8 mit BOM**
konvertiert (drei Umlaute in Kommentar und Meldungstexten geprüft und korrekt
übernommen). Das ist im Diff sichtbar und hier bewusst dokumentiert.

**Testmodus im Referenzlauf-Werkzeug** (`Referenzlauf/Migrationslauf.cs`, neuer Modus
`migration`):

```
Referenzlauf.exe migration <quellDb> <zielOrdner> [--nokopie] [--schreibschutz]
```

Legt eine Kopie an, biegt den DB-Pfad über `Properties.Settings.DBPath` (Reflection)
um und **prüft ihn hart über `DataRepository.GetDBPath()` nach**, führt
`SchemaMigration.Ausfuehren` aus und weist das Ergebnis über
`OleDbConnection.GetOleDbSchemaTable` (Tables/Columns/Indexes/Foreign_Keys) nach —
Spalten gegen `SchemaKatalog.Alle`, die 13 Spalten und den Index der neuen Tabelle,
alle sechs erwarteten Beziehungen samt Löschregel, dazu den Restbestand ungültiger
`ID_PUFFER`-Werte und verwaister Puffer-Zeilen. `--nokopie` ermöglicht den
No-op-Zweitlauf auf derselben Kopie, `--schreibschutz` den Abnahmefall aus
ADR-Aufgabe 8.

### 8. Testresultate (14.08.2026)

Basis: frische Kopien von
`C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`.
Die produktive `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde zu keinem Zeitpunkt
geöffnet oder beschrieben.

| # | Lauf | Ergebnis |
|---|---|---|
| 0 | Build `WP-Plan.sln` Debug/x86 über MSBuild 2022 | **0 Fehler** (nur Bestandswarnungen). `Referenzlauf.csproj` ebenfalls 0 Fehler. |
| 1 | **Erstlauf** auf frischer Kopie | Alle vier Schritte OK, `SchemaVersion` 0 → **4**. Schritt 1: 10 Spalten angelegt, 5 bereits vorhanden. Schritt 2: 7 + 0 + 1 angelegt, `Klimazone_DIN4710` bereits vorhanden. Schritt 3: Tabelle, Index, FK angelegt. Schritt 4: 1 `ID_PUFFER` gemappt, 4 Waisen entfernt, 5 Beziehungen angelegt. **Schema-Nachweis: 0 Abweichungen.** |
| 2 | **No-op** (`--nokopie`, dieselbe Kopie) | Alle vier Schritte „bereits erledigt", `SchemaVersion` bleibt **4**, `MigrationOk = true`, Schema-Nachweis erneut 0 Abweichungen. Kein Schreibzugriff außer dem Bootstrap-Leerlauf. |
| 3 | **Schreibschutz** (`--schreibschutz`, weitere Kopie) | Bericht beginnt mit dem DB-Pfad, dann: *„Der Schemamarker Tab_Applikation.SchemaVersion konnte nicht angelegt werden. Die Datenbank ist vermutlich schreibgeschützt oder von einem anderen Programm exklusiv geöffnet."* + *„Meldung der Datenbank: Cannot modify the design of table 'Tab_Applikation'. It is in a read-only database."* **Marker nicht angehoben (0), `MigrationOk = false`, kein Absturz.** `migration_protokoll.txt` konnte geschrieben werden (nur die `.accdb` war schreibgeschützt). |
| 4 | **Regression**: 8 Referenzprojekte auf einer **migrierten** Kopie, `vergleich` gegen `Referenzlaeufe/2026-08-14_B0` | **GESAMT: PASS** — 8/8 Projekte, 2 033 047 Werte innerhalb der Toleranz. Nachweis: `Referenzlaeufe/2026-08-14_Paket1_Migration/`. |

**Zusätzlicher Nachweis der Beziehungssemantik** (SQL auf der migrierten Kopie):

| Prüfung | Ergebnis |
|---|---|
| Gültige Referenz `WS_ID_Puffer` setzen | OK |
| Ungültige Referenz (`999999999`) setzen | abgelehnt — *„a related record is required in table 'Tab_Pufferspeicher'"* |
| Referenzierten Puffer löschen **ohne** vorheriges Lösen | abgelehnt — *„includes related records"*; **die Anlage bleibt erhalten** (kein Kaskadenlöschen) |
| Referenz lösen (wie `PufferSpCtrl.ReferenzenLoesen`), dann löschen | OK, Anlage weiterhin vorhanden |
| Projekt löschen (B0-6b) | Puffer-Projektkopien werden mit abgeräumt (1 → 0) |

### 9. Geänderte und neue Dateien

**Neu**

- `Allgemein/Update/SchemaKatalog.cs` (UTF-8 BOM)
- `Allgemein/Update/SchemaMigration.cs` (UTF-8 BOM)
- `../Referenzlauf/Migrationslauf.cs` (UTF-8 BOM)
- `Allgemein/Simulation/Paket1_SchemaMigration_Protokoll.md` (diese Datei)

**Geändert**

| Datei | Änderung |
|---|---|
| `Controller/ApplikationCtrl.cs` | `SPALTE_SCHEMAVERSION`, `GetSchemaVersion()`, `SetSchemaVersion(int)` — tolerant, still, eigene Verbindung |
| `Allgemein/Simulation/WaermequelleClass.cs` | `SchemaSicherstellen()` iteriert den Katalog; private Überladung bekommt die Tabelle übergeben; API unverändert. **Kodierung UTF-8 ohne BOM beibehalten.** |
| `Controller/PufferSpCtrl.cs` | `ReferenzenLoesen`/`BetroffeneIds`; Aufruf in `ProjektWaisenEntfernen` und `DeleteFromProjekt` |
| `Controller/ProjektDuplizierenCtrl.cs` | `FK_MAP` um die drei Puffer-Referenzen ergänzt |
| `Controller/WizardCtrl.cs` | zwei Zeilen `ID_PUFFER`-Absicherung (siehe 4.) |
| `Program.cs` | Migrationsaufruf + eine MessageBox |
| `Views/Simulation/Form_Simulation_Config.cs` | Blockade am Anfang von `SetControls`, `SimulationsbereichSperren()` |
| `Allgemein/Simulation/SimulationRunner.cs` | Blockade in `Simuliere` |
| `Allgemein/Simulation/SimulationControl.cs` | Blockade in `Do_Simulation`, Feld `Sperrgrund` |
| `Views/Pufferspeicher/Form_PufferSp_Bearbeiten.cs` | Namenszugriff statt `row[2..6]`; **ISO-8859-1 → UTF-8 mit BOM** |
| `../Referenzlauf/Program.cs` | Modus `migration` verdrahtet |

Kodierungen aller berührten Bestandsdateien geprüft: BOM-Zustand unverändert (Ausnahme
`Form_PufferSp_Bearbeiten.cs`, dokumentiert), keine Ersatzzeichen (`U+FFFD`), Zeilenenden
**je Datei unverändert**.

> **Korrektur (Review-Nacharbeit).** Hier stand „Zeilenenden durchgehend CRLF". Das ist
> falsch und war es schon in Etappe 1: `SchemaMigration.cs` wird seit Anlage mit **LF**
> geschrieben, ebenso `Migrationslauf.cs`, und im Bestand liegen
> `Form_Simulation_Config.cs`, `Wizard_WPItem.cs` und `WaermequelleClass.cs` ebenfalls mit
> LF. Die Regel dieses Projekts lautet nicht „CRLF", sondern **„je Datei beibehalten"** —
> die Etappen 3 und 4 formulieren es bereits richtig. Nachgemessen am 14.08.2026
> (CR-/LF-Byteszählung je Datei), Stand nach der Nacharbeit:
>
> | Datei | Zeilenende | BOM |
> |---|---|---|
> | `Allgemein/Update/SchemaMigration.cs` | LF | ja |
> | `Allgemein/Update/ProjektPuffer.cs` | CRLF | ja |
> | `Allgemein/Update/SchemaKatalog.cs` | CRLF | ja |
> | `Controller/PufferSpCtrl.cs` | CRLF | ja |
> | `Controller/ProjektCtrl.cs` | CRLF | ja |
> | `Views/Simulation/Form_Simulation_Config.cs` | LF | ja |
> | `Views/Simulation/Form_Simulation_Detail.cs` | CRLF | ja |
> | `Views/Wizard/Wizard_WPItem.cs` | LF | ja |
> | `Allgemein/Simulation/WaermequelleClass.cs` | LF | **nein** |
> | `../Referenzlauf/Migrationslauf.cs` | LF | ja |
> | dieses Protokoll | LF | ja |

---

## Etappe 2 (14.08.2026) — Schritt 5: Datenmigration Quellen/Senken

Umsetzung der Migrationstabelle aus [Konzept 5.5](Konzept_Simulation_QuellenSenken.md),
Zeile für Zeile. `ZIEL_VERSION` steigt von **4 auf 5**, der auskommentierte Platzhalter
im Schrittregister ist durch den echten Eintrag ersetzt.

### 1. Die sechs Regeln

Alles läuft je Projekt (`SELECT ID FROM Tab_Projekt ORDER BY ID`) in dieser Reihenfolge:

| Regel | Altbestand | Übernahme |
|---|---|---|
| **R1** | `Z_ProjektPufferSp` mit `Erzeuger = 'Wärmepumpe'`, **erster** Eintrag nach `Prioritaet` | Der referenzierte Projekt-Puffer erhält `Verwendung = 'Heizung'` sowie `Vorlauf`, `Ruecklauf`, `Schwelle_Ein`, `Schwelle_Aus` aus der Zuordnung (NULL-tolerant) und `Schwelle_Aus_Nachrang = Schwelle_Aus`. **Alle** WP-Anlagen des Projekts (`ID_Type = 1`): `WS_Ziel = 'PufferHeizung'`, `WS_ID_Puffer` = diese Puffer-ID. Jeder weitere WP-Eintrag → Hinweis. |
| **R2** | Zuordnung mit anderem `Erzeuger` | **keine** Übernahme (war wirkungslos), je Eintrag ein Hinweis |
| **R3** | `WQ_Typ = 'Pufferspeicher'` mit `WQ_Puffer` (Bezeichner) | Projekt-Puffer gleichen Bezeichners → `WQ_ID_Puffer`; nicht auflösbar → Hinweis „Quell-Puffer im Projekt anlegen", Feld bleibt NULL, `WQ_Puffer` unverändert lesbar |
| **R6** | `Tab_Einstellungen.Pendelspeicher > 0` **und** mindestens eine BHKW-Anlage | Projekt-Puffer `BHKW-Pendelspeicher` (`Verwendung = 'Heizung'`, Volumen aus dem Parameter), alle BHKW-Anlagen auf `WS_Ziel = 'PufferHeizung'`. Vorhandener gleichnamiger Puffer wird wiederverwendet. |
| **R4** | `Tab_Pufferspeicher`-Zeile ohne Anlagenzeile | Anlagenzeile `ID_Type = 12` nachtragen, damit der Puffer im Projektbaum erscheint |
| **R5** | alles Übrige | `WS_Ziel = 'Heizkreis'` für Erzeuger ohne Senke; `WS_Ladeprio*`, `WS_Ladeprio_PV`, `WS_Ladegrenze*` = 0; `Entladeprio` = 0; `Schwelle_Aus_Nachrang = Schwelle_Aus` |

**Warum R6 vor R4?** R6 legt einen neuen Puffer an, der ebenfalls eine Anlagenzeile
braucht. Läuft R4 danach, gibt es genau **eine** Stelle, die Anlagenzeilen erzeugt
(`AnlagenzeileAnlegen`) — der Pendelspeicher wird automatisch mitgezogen.
R5 läuft zuletzt, weil es nur dort `Heizkreis` setzt, wo R1/R6 noch keine Senke
geschrieben haben.

**Verhaltensneutralität.** `Z_ProjektPufferSp` wird ausschließlich **gelesen**
(Konzept 5.4) — weder geändert noch gelöscht; nachgewiesen über die unveränderte
Zeilenzahl 13. Zusammen damit, dass die Engine die neuen Spalten noch nicht liest, kann
sich am Ergebnis nichts ändern (Regressionsnachweis in Abschnitt 7).

**Run-once ohne Heuristik.** Die Einmaligkeit kommt allein vom
`SchemaVersion`-Marker. Es gibt bewusst **keine** Prüfung „ist `WS_Ziel` schon gesetzt?"
als Auslöser — die würde bei jedem Programmstart eine bewusste Anwenderentscheidung
(z. B. ein zurückgesetztes `WS_Ziel = 'Heizkreis'`) wieder überschreiben. Der Schritt ist
davon unabhängig **in sich idempotent** (alle Einfügungen sind durch Existenzprüfungen
gedeckt, alle Aktualisierungen schreiben denselben Wert), damit ein Wiederholungslauf
nach einem Abbruch mitten im Schritt keinen Schaden anrichtet — nachgewiesen in
Abschnitt 6.

### 2. Auslegungsentscheidungen

**R1, Sortierung `Prioritaet, ID`.** `SimulationControl.Do_Simulation`
(`SimulationControl.cs:98-157`, Stand 14.08.2026 nach der Nacharbeit) liest über
`Z_ProjektPufferSpCtrl.ReadAll` (`ORDER BY Prioritaet`), überspringt Nicht-WP-Zeilen mit
`continue` und bricht nach dem ersten WP-Treffer mit `break` ab. Die Migration bildet das
nach; das ergänzte `, ID` ist die einzige Abweichung und macht die Auswahl bei gleicher
Priorität reproduzierbar. In der Arbeitskopie tragen die Dubletten (10058/10072 im
Projekt 1008) ohnehin identische Werte.

**R1, Auflösung des Puffers.** Vorrang hat `ID_Pufferspeicher`, aber nur wenn die Zeile
zum **selben Projekt** gehört; sonst greift der Bezeichner-Rückfallweg wie in
`SimulationControl`. Ein Verweis auf den Speicher eines fremden Projekts wäre derselbe
stille Datenfehler, den Schritt 4 für `ID_PUFFER` bereinigt hat.

**R4, eine Zeile je (Projekt, Bezeichner).** Die Zuordnung Anlagenzeile ↔ Puffer läuft im
Bestand über den **Bezeichner** (`PufferSpCtrl.ProjektWaisenEntfernen`, `GetProjektId`),
nicht über die ID. Projekt 1023 hat 79 Puffer-Zeilen mit nur vier verschiedenen
Bezeichnern; eine Zeile je Puffer-ID ergäbe Dutzende identischer Baumeinträge. Verknüpft
wird mit der kleinsten Puffer-ID des Bezeichners.

**R5, 0 ist kein Fremdschlüssel.** `WS_Ladeprio`, `WS_Ladeprio2`, `WS_Ladeprio_PV`,
`WS_Ladegrenze`, `WS_Ladegrenze2` bekommen die 0 als Konzept-Default („nach Vorgabe" bzw.
„nicht gesetzt"). Die **ID**-Spalten `WS_ID_Puffer`, `WS_ID_Puffer2`, `WQ_ID_Puffer`
bleiben dagegen NULL, wo nichts gesetzt ist — eine 0 verletzt die erzwungenen Beziehungen
aus Schritt 4. Nachgewiesen: 0 Zeilen mit `WS_ID_Puffer = 0 OR WS_ID_Puffer2 = 0 OR
WQ_ID_Puffer = 0`.

**R5, Schwellen.** `Schwelle_Aus_Nachrang = Schwelle_Aus` wird nur dort gesetzt, wo
`Schwelle_Aus` gepflegt ist. In der Arbeitskopie ist die Spalte durchgehend leer, also
bleiben beide NULL — damit greifen später die Engine-Vorgaben 10 % / 95 %.

### 3. Einheit des BHKW-Pendelspeichers (R6)

**Entscheidung: `Gesamtvolumen[l] = Pendelspeicher × 1000`.**

| Größe | Einheit | Beleg |
|---|---|---|
| `Tab_Einstellungen.Pendelspeicher` | **m³** | Anzeigetext `label56.Text` = „Volumen Pendelspeicher [m³]" in `Views/Simulation/Form_Simulation_Detail.resx`; `Form_Simulation_Config` kennt den Parameter gar nicht |
| Kapazitätsformel | — | `SimulationControl.Simulation_BHKW_Ctrl`, **Stand vor Etappe 3** (damals `:341`, heute `:383` mit der Liter-Formel): `kapazitaetPendelspeicher = Volumen * 20000 / 860` = 23,26 kWh je Einheit = 1000 l · 1,163 Wh/(l·K) · **20 K**. Deckungsgleich mit `SetKapPendelspeicher()` in `Form_BHKWEing.cs.bak`: `Volumen * 20 * 1.163` |
| `Tab_Pufferspeicher.Gesamtvolumen` | **Liter** | Katalog: „Vitocell 140-E 600 Liter" → `Gesamtvolumen = 600`; „allSTOR exclusiv VPS 800/3-7" → 778 |

Das UI-Label weist also **nicht** Liter aus; der Faktor 1000 ist zwingend. Der neue
Puffer bekommt zusätzlich `Speichertyp = 'Pufferspeicher'`,
`Bereitschaftsverluste = 0` und `Investitionskosten = 0` — der Alt-Pendelspeicher kennt
weder Verluste noch Kosten, 0 hält das Ergebnis unverändert.

`Tab_Einstellungen.Pendelspeicher` bleibt **unverändert** stehen (nicht genullt, nicht
gelöscht): bis Engine und UI umgestellt sind, ist der Parameter die Rückfallebene.

### 4. Zwei Fallstricke, die im Testlauf zuschlugen

**`new OleDbParameter(name, 0)` bindet an die falsche Überladung.** Das Literal `0` ist
nach C#-Regeln implizit in **jeden** Enum-Typ konvertierbar, also auch in `OleDbType`
(`OleDbType.Empty` hat den Wert 0). Der Aufruf landet damit bei
`OleDbParameter(string, OleDbType)` — der Parameter hat einen Typ, aber keinen Wert, und
das INSERT scheitert mit *„Parameter[5]: the OleDbType property is uninitialized"*.
Alle Parameter der Datenmigration laufen deshalb über den Helfer
`Par(name, OleDbType, wert)` mit ausdrücklichem Typ. Der ist ohnehin nötig, weil aus
einem reinen `DBNull` kein Spaltentyp ableitbar ist.

**Die Komponenten-Fremdschlüssel haben in Access den Spalten-Default 0.** Ein
`INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Type, ID_PUFFER)` scheitert
mit *„ein Datensatz in der Tabelle 'Tab_BHKW' muss in Beziehung stehen"*, weil `ID_BHKW`
still auf 0 vorbelegt wird und `Tab_BHKW` keine ID 0 hat. `ID_WP`, `ID_SP`, `ID_PV`,
`ID_Solar`, `ID_Kessel` und `ID_BHKW` müssen deshalb **ausdrücklich auf NULL** gesetzt
werden — genau das tut `WizardCtrl.Add_WP_Waermeerzeuger` mit seinen `DBNull`-Parametern.
`Tab_Energieanlagen.ID` ist ein **AutoWert** und wird nicht mitgeschrieben (geprüft:
INSERT ohne `ID` vergibt die nächste Nummer). `Tab_Pufferspeicher.ID` ist dagegen **kein**
AutoWert — R6 vergibt sie nach dem `GetMaxID + 1`-Muster aus `PufferSpCtrl.CopyFromStamm`.
`ID_Carrier` wird NULL geschrieben wie bei allen fünf vorhandenen Puffer-Anlagenzeilen.

### 5. Zählungen und Belege (frische Kopie der Arbeitskopie, 14.08.2026)

Erstlauf auf einer frischen Kopie von
`Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`: `SchemaVersion` **0 → 5**, Schritte 1–4
wie in Etappe 1, Schritt 5 OK, Schema-Nachweis **0 Abweichungen**.

| Regel | Zählung | Beleg aus der Arbeitskopie |
|---|---|---|
| **R1** | **3** Puffer mit Verwendung/Betriebsparametern, **6** Anlagen auf `PufferHeizung` | Projekt **1008**: Zuordnung 10058 (Prio 2) → Puffer **1008007** „Vitocell 140-E 600 Liter": `Verwendung='Heizung'`, `Vorlauf=35`, `Ruecklauf=45`, Schwellen NULL → `Schwelle_Aus_Nachrang` NULL; WP-Anlagen **10132** und **10133** auf `PufferHeizung`/1008007. Projekt **1019**: Zuordnung 10359 → Puffer 1018009, 65/45, Anlagen 10647+10648. Projekt **1023**: Zuordnung 10362 → Puffer 1018023, 65/45, Anlagen 11203+11204. |
| **R1**, Dublette | **1** Hinweis | Projekt 1008: zweite WP-Zuordnung **10072** (identische Werte) — schon bisher wirkungslos, nicht übernommen |
| **R2** | **9** Hinweise, **0** Übernahmen | 1007: 10060+10172 (`Solarthermie`); 1008: 10057+10071 (`BHKW`), 10059+10073 (`Heizkessel`); 1011: 10061+10206 (`Gesamtsystem`); 1018: 10289 (`BHKW`) |
| **R3** | **1** aufgelöst, **0** Hinweise | Projekt 1021, Anlage **10361** („CS7800iLW 12", `WQ_Typ='Pufferspeicher'`, `WQ_Puffer='allSTOR exclusiv VPS 800/3-7'`) → `WQ_ID_Puffer = 1018014`; `WQ_Puffer` unverändert |
| **R4** | **17** Anlagenzeilen nachgetragen (5 vorhanden → **22**) | z. B. Projekt 1023: `test` → 1018022, `allSTOR exclusiv VPS 800/3-7` → 1018024, `Vitocell 140-E 600 Liter` → 1024050; `Vitocell 140-E 600 Ltr` hatte bereits Zeile 11202. Neue IDs 11238…11254 (AutoWert) |
| **R5** | **37** Anlagen auf `Heizkreis` | Verteilung danach: `ID_Type` 1 → 19 Heizkreis + 6 PufferHeizung, 2 → 2, 10 → 12, 11 → 4. Typen 3/4/12 bleiben ohne `WS_Ziel` (keine Wärmeerzeuger). |
| **R5**, Vorgaben | **78/78** Anlagen mit `WS_Ladeprio = WS_Ladeprio2 = WS_Ladeprio_PV = WS_Ladegrenze = WS_Ladegrenze2 = 0`; **118/118** Projekt-Puffer mit `Entladeprio = 0` | ID-Spalten weiterhin NULL: 72 × `WS_ID_Puffer` NULL, 78 × `WS_ID_Puffer2` NULL, 77 × `WQ_ID_Puffer` NULL; **0** Zeilen mit 0 in einer ID-Spalte |
| **R6** | **0** im Echtbestand | `Tab_Einstellungen.Pendelspeicher` ist in der Arbeitskopie durchgehend 0 oder leer — auch bei den BHKW-Projekten 1017 (leer), 1018 (0) und 1024 (0). Die Regel greift daher nirgends; Nachweis siehe unten. |
| **Hinweise gesamt** | **10** | 9 × R2 + 1 × R1-Dublette |

Bestandsgrößen: 61 → **78** Zeilen in `Tab_Energieanlagen`, **118** Projekt-Puffer
(122 minus die 4 Waisen aus Schritt 4), `Z_ProjektPufferSp` unverändert **13** Zeilen.

**R6 — Nachweis an einem eigens präparierten Bestand.** Weil kein Projekt der
Arbeitskopie einen Pendelspeicher > 0 trägt, wurde auf einer **weiteren** Kopie
`Pendelspeicher` gesetzt: 1017 = 0,8 m³, 1018 = 1,5 m³ (beide mit BHKW) und
1010 = 2 m³ (Wärmepumpe, **kein** BHKW).

| Projekt | Ergebnis |
|---|---|
| 1017 | Puffer **1054164** `BHKW-Pendelspeicher`, `Gesamtvolumen = 800` l, `Verwendung='Heizung'`; BHKW-Anlage 10260 → `PufferHeizung`/1054164; Anlagenzeile 11243 über R4 nachgetragen |
| 1018 | Puffer **1054165**, `Gesamtvolumen = 1500` l; BHKW-Anlagen 10370 **und** 10371 → `PufferHeizung`/1054165; Anlagenzeile 11244 |
| 1010 | kein Puffer, stattdessen Hinweis „Pendelspeicher 2 m³ eingetragen, aber keine BHKW-Anlage im Projekt" |
| 1024 | BHKW vorhanden, `Pendelspeicher = 0` → nichts, wie erwartet |
| — | `Tab_Einstellungen.Pendelspeicher` in allen drei Projekten unverändert (2 / 0,8 / 1,5) |

### 6. No-op, Wiederholbarkeit und Anwenderschutz

| # | Prüfung | Ergebnis |
|---|---|---|
| 1 | **No-op**: zweiter Lauf auf derselben Kopie (`--nokopie`) | Alle fünf Schritte „bereits erledigt", `SchemaVersion` bleibt **5**, `MigrationOk = true`. Prüfsumme über `Tab_Energieanlagen` (17 Spalten), `Tab_Pufferspeicher` (11 Spalten), `Z_ProjektPufferSp` (10 Spalten) und `Tab_Einstellungen.Pendelspeicher` vorher = nachher: **`1dd4465cc4dbc63f24567b33bc611b6d`**, keine einzige Zeile geändert. |

**Das Prüfsummen-Werkzeug.** Die Prüfsumme stammt **nicht** aus der Referenzlauf-Suite,
sondern aus einem Wegwerf-PowerShell-Skript (`snapshot.ps1` im Arbeitsordner der
Sitzung, außerhalb des Repos): es liest die genannten Spalten über
`Microsoft.ACE.OLEDB.12.0` nach `ORDER BY ID` sortiert aus, schreibt sie zeilenweise als
Text und bildet darüber ein **MD5** (`Get-FileHash -Algorithm MD5`). Der Wert ist damit
nur innerhalb dieses einen Vergleichs (vorher/nachher, gleiche Kopie, gleiches Skript)
aussagekräftig — er ist keine über Läufe hinweg stabile Kennzahl und taugt nicht als
Abnahmekriterium. Wer ihn reproduzieren will, braucht dasselbe Skript; die belastbare
Zusicherung ist der Schema-Nachweis der Suite (`0 Abweichungen`) plus die Regression.
| 2 | **Anwenderschutz / Run-once**: an Anlage **10132** `WS_Ziel` von Hand auf `'Heizkreis'` und `WS_ID_Puffer` auf NULL zurückgesetzt, dann `Ausfuehren` erneut | Schritt 5 meldet „bereits erledigt", der Wert bleibt **`Heizkreis`**, `WS_ID_Puffer` bleibt NULL. Die Migration greift die Anwenderentscheidung nicht wieder an. |
| 3 | **Idempotenz des Schritts selbst**: auf der R6-Kopie `SchemaVersion` künstlich auf 4 zurückgesetzt und Schritt 5 ein zweites Mal ausgeführt | Kein Duplikat: beide `BHKW-Pendelspeicher` werden **wiederverwendet** (Protokoll: „vorhandener Puffer … wiederverwendet"), 0 neue Anlagenzeilen, 0 neue Puffer. Prüfsumme über alle betroffenen Spalten vorher = nachher, **keine Unterschiede**. |

### 7. Regression

`Referenzlaeufe/2026-08-14_Paket1_Migration/` neu erzeugt, Vergleich gegen
`Referenzlaeufe/2026-08-14_B0`:

```
Projekt_1007: PASS (29 Dateien, 324209 Werte)
Projekt_1008: PASS (21 Dateien, 227832 Werte)
Projekt_1010: PASS (18 Dateien, 201539 Werte)
Projekt_1011: PASS (29 Dateien, 324231 Werte)
Projekt_1017: PASS (20 Dateien, 245377 Werte)
Projekt_1018: PASS (19 Dateien, 210342 Werte)
Projekt_1023: PASS (25 Dateien, 262902 Werte)
Projekt_1024: PASS (22 Dateien, 236615 Werte)

GESAMT: PASS (2033047 Werte innerhalb der Toleranz)
```

Gerechnet wurde auf einer **auf SchemaVersion 5 migrierten** Kopie, also mit gesetzten
`WS_Ziel`/`WS_ID_Puffer`, den 17 zusätzlichen Anlagenzeilen und den drei Puffern mit
`Verwendung`. Wertgleich zum Bestand — wie erwartet, denn die Engine liest die neuen
Spalten nicht und `Z_ProjektPufferSp` ist unverändert.

**Zum Weg:** die acht Projekte liefen über den Modus `projekt`, nicht über `lauf`.
`lauf` legt `Referenzlaeufe\Arbeitskopie` bei jedem Start neu aus der produktiven
Datenbank an und würde die Migration damit wieder verwerfen; die migrierte Datenbank lag
in einem Arbeitsordner außerhalb des Repos. Die produktive
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde zu keinem Zeitpunkt geöffnet oder
beschrieben.

### 8. Geänderte Dateien (Etappe 2)

| Datei | Änderung |
|---|---|
| `Allgemein/Update/SchemaMigration.cs` | `ZIEL_VERSION` 4 → 5; Schritt 5 registriert; `Schritt_5_ProjektdatenQuellenSenken` mit `ProjektMigrieren` und `Regel1…Regel6`; Helfer `AnlagenzeileAnlegen`, `PufferAufloesen`, `Hinweis`, `Par`, `Wert`, `Txt`, `Kommazahl`, `Anzeige`; sieben neue Zählwerk-Eigenschaften; Zusammenfassungszeile im Bericht. **UTF-8 mit BOM, LF — unverändert.** |
| `../Referenzlauf/Migrationslauf.cs` | Zählwerk von Schritt 5 im Lauf-Protokoll; neuer Abschnitt `DatenNachweis` mit vier Invarianten (`PufferHeizung` ohne `WS_ID_Puffer`, ID-Spalten mit 0, Anlagen ohne Ladeprio-Vorgabe, Projekt-Puffer ohne Anlagenzeile — jeweils erwartet 0). **UTF-8 mit BOM, LF — unverändert.** |
| `../Referenzlaeufe/2026-08-14_Paket1_Migration/` | neu gerechnet (8 Projekte) + `vergleich_protokoll.md` |
| `Allgemein/Simulation/Paket1_SchemaMigration_Protokoll.md` | dieser Abschnitt |

Kein Formular, kein Controller und kein Engine-Modul wurde angefasst. Die Dateien der
Parallelarbeit (`Form_Heizkessel*`, `SimulationSPK`, `RecordSet`, `FormMain`,
`Form_BHKWEing`, `WizardParent`, `WErzeugerModel`) sind unberührt — `WizardCtrl` und
`WErzeugerModel` wurden nur **gelesen**, um das INSERT-Muster nachzubilden.

---

## Etappe 3 (14.08.2026) — Stilllegung von `Tab_Einstellungen.Pendelspeicher`

Nutzerentscheidung vom 14.08.2026: Engine und Oberfläche beziehen das Volumen des
BHKW-Pendelspeichers künftig aus dem Projekt-Puffer `BHKW-Pendelspeicher` in **Litern**.
Der Alt-Parameter bleibt physisch stehen, wird aber **nirgends mehr gelesen**.

### 1. Eine gemeinsame Quelle

Neu: `Allgemein/Update/ProjektPuffer.cs` — die Bausteine, aus denen ein Projekt-Puffer
und seine Anlagenzeile bestehen, stehen jetzt genau einmal da:

| Baustein | Inhalt |
|---|---|
| Konstanten | `BEZ_PENDELSPEICHER`, `VERWENDUNG_HEIZUNG`, `WS_ZIEL_PUFFER_HEIZUNG`, `SPEICHERTYP_PUFFER`, `TYP_BHKW`, `TYP_PUFFER`, `M3_IN_LITER` |
| `SQL_PUFFER_INSERT` + `PufferParameter` | Zeile in `Tab_Pufferspeicher` (ID kein AutoWert → `GetMaxID + 1`) |
| `SQL_ANLAGENZEILE_INSERT` + `AnlagenzeileParameter` | Zeile in `Tab_Energieanlagen`, `ID_Type = 12` (Komponenten-FKs ausdrücklich NULL) |
| `SQL_BHKW_AUF_PUFFER` + `BhkwAufPufferParameter` | `WS_Ziel`/`WS_ID_Puffer` an allen BHKW-Anlagen |

Bewusst **nur SQL-Text und Parameterlisten, keine Ausführung**: die Migration arbeitet
auf ihrer eigenen, stillen Verbindung (sie muss Fehlertexte auswerten und darf keine
Dialoge zeigen), der Controller auf seiner. Gemeinsam ist die Struktur, nicht der Weg
zur Datenbank. `SchemaMigration` R4 und R6 sind darauf umgestellt; die Zählungen des
Migrationslaufs sind danach unverändert (Nachweis in Abschnitt 5).

Neu in `PufferSpCtrl` (beide `static`, beide **still** — nur `Console.WriteLine`, weil
sie im Engine-Pfad hängen, Konzept 13.4):

| Methode | Verhalten |
|---|---|
| `PendelspeicherVolumenLiter(int idProjekt)` | Gesamtvolumen der Zeile `Bezeichner = 'BHKW-Pendelspeicher'` des Projekts, **0** wenn keine existiert. Gelesen wird die Zeile mit der kleinsten `ID` — dieselbe Auswahl, mit der R6 einen vorhandenen Puffer wiederverwendet. |
| `SetPendelspeicherVolumenLiter(int idProjekt, int liter)` | Zeile vorhanden → `UPDATE Gesamtvolumen` (auch auf 0; die Zeile **bleibt**, damit ein bewusst geleerter Speicher nicht samt Betriebsparametern verschwindet). Keine Zeile und `liter > 0` → Puffer + Anlagenzeile + BHKW-Senke nach dem R6-Muster. Keine Zeile und `liter = 0` → nichts. `liter < 0` oder `idProjekt <= 0` → `false`, nichts geschrieben. |

Die schreibenden Wege gehen bewusst **nicht** über `DataRepository`: dessen Methoden
zeigen im Fehlerfall eine MessageBox, und im Engine-Pfad wäre das ein hängender Lauf.

### 2. Engine

| Ort | Vorher | Nachher |
|---|---|---|
| `SimulationControl.VolumenPendelspeicherBHKW` | m³ | **Liter** (Kommentar am Feld) |
| `SimulationControl.Simulation_BHKW_Ctrl` | `(float)V * 20000 / 860` | `(float)V * 20 / 860` |
| `SimulationRunner.Simuliere` | `(int)ctrl.model.Pendelspeicher` | `PufferSpCtrl.PendelspeicherVolumenLiter(idProjekt)` |
| `Form_Simulation_Detail.btn_Simulation_Click` | `(int)numericUpDown_Volumen.Value` (m³) | dieselbe Quelle wie der Runner |

Weitere Nutzer des Feldes gibt es nicht (`grep`): nur die Deklaration, die eine
Rechenstelle und die beiden Zuweisungen. `Views/Simulation/Form_Simulation_Detail - Kopie.cs`
enthält eine dritte Zuweisung, ist aber im `.csproj` vom Build ausgeschlossen.

**Formeläquivalenz.** `Liter · 20/860` ist dieselbe Zahl wie `m³ · 20000/860`, sobald die
Migration mit dem Faktor 1000 umgerechnet hat. Die Zwischenprodukte sind gleich groß und
in `float` exakt darstellbar (bis rund 838 m³ bzw. 838 000 l bleibt das Produkt unter
2²⁴), das Ergebnis ist damit **bitgleich**:

| Alt-Wert | Liter | alt `m³·20000/860` | neu `l·20/860` | bitgleich |
|---|---|---|---|---|
| 0,8 m³ | 800 | 18,6046512 kWh | 18,6046512 kWh | ja |
| 1,0 m³ | 1000 | 23,2558136 kWh | 23,2558136 kWh | ja |
| 1,5 m³ | 1500 | 34,8837204 kWh | 34,8837204 kWh | ja |
| 2,0 m³ | 2000 | 46,5116272 kWh | 46,5116272 kWh | ja |

### 3. Befund: der Alt-Pfad hat auf ganze m³ abgeschnitten

`SimulationControl.VolumenPendelspeicherBHKW` war **schon immer `int`**, der Alt-Parameter
dagegen `double`. Beide Zuweisungen casteten hart: `(int)ctrl.model.Pendelspeicher` bzw.
`(int)numericUpDown_Volumen.Value`. C# **schneidet ab**, es rundet nicht — die
Nachkommastelle des Eingabefeldes (`DecimalPlaces = 1`, `Increment = 0,1`) war also
wirkungslos:

| Eingabe | Alt gerechnet mit | Alt-Kapazität | Neu (nach Migration) | Neu-Kapazität |
|---|---|---|---|---|
| 0,8 m³ | 0 m³ | **0 kWh** | 800 l | 18,60 kWh |
| 1,5 m³ | 1 m³ | 23,26 kWh | 1500 l | 34,88 kWh |
| 2,0 m³ | 2 m³ | 46,51 kWh | 2000 l | 46,51 kWh |

Damit ist die Umstellung **nicht** in jedem Fall ergebnisneutral: Projekte, deren
Alt-Parameter keine ganze Zahl war, rechnen jetzt mit dem tatsächlich eingegebenen
Volumen. Das ist die Behebung eines Altfehlers, keine neue Abweichung — im Echtbestand
der Arbeitskopie trägt ohnehin **kein** Projekt einen Wert > 0 (siehe Etappe 2, R6).
Belegt ist der Effekt in Abschnitt 4, Lauf 2.

### 4. Verifikation

Basis: eigene Kopien von `Referenzlaeufe\Arbeitskopie\Kenndaten.accdb` in
`C:\Waermeplan\Etappe3_Test` — **außerhalb des Repos**, Modus `projekt` statt `lauf`
(der legt die Arbeitskopie neu an und wird parallel von einer anderen Sitzung benutzt).
Die produktive `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde zu keinem Zeitpunkt
geöffnet oder beschrieben.

Präpariert wie in Etappe 2, um einen dritten, ganzzahligen Fall erweitert:
`Pendelspeicher` 1017 = 0,8 m³, 1018 = 1,5 m³, **1024 = 2,0 m³** (alle drei mit BHKW),
danach Migration → Puffer mit 800 / 1500 / **2000** l.

| # | Lauf | Ergebnis |
|---|---|---|
| 0 | Build `WP-Plan.sln` Debug/x86 über MSBuild 2022 | **0 Fehler** (nur Bestandswarnungen). `Referenzlauf.csproj` ebenfalls 0 Fehler. |
| 1 | **Äquivalenz**, Projekt **1024** (2,0 m³ → 2000 l): Simulation vor der Umstellung gegen Simulation nach der Umstellung, identische migrierte Datenbank | **PASS**, 22 Dateien, **236 615 Werte** identisch. Das ist der Kernbeleg: wo der Alt-Pfad nicht abschnitt, ist die Umstellung wertgleich. |
| 2 | **Trunkierungsbeleg**: Alt-Code mit `Pendelspeicher = 0,8` gegen Alt-Code mit `Pendelspeicher = 0` (Projekt 1017) | **PASS**, 245 377 Werte identisch → `(int)0,8 = 0`, die Alt-Eingabe war wirkungslos. Projekt 1018 (1,5 gegen 0) erwartungsgemäß FAIL. |
| 3 | **Gegenprobe zur Trunkierung**: neuer Code mit 1017 = **0 l** und 1018 = **1000 l** gegen den Alt-Lauf (0,8 bzw. 1,5 m³) | **beide PASS** (245 377 + 210 342 Werte). Der neue Pfad reproduziert den alten exakt, sobald man ihm den abgeschnittenen Wert gibt. |
| 4 | **Wirkung der Umstellung**: 1017 (800 l) und 1018 (1500 l) neu gegen alt | FAIL — **erwartet**: 15 385 bzw. 2 238 Abweichungen, ausschließlich in den BHKW-Größen. Ursache ist ausschließlich der in Abschnitt 3 belegte Altfehler. |
| 5 | **Regression** gegen `Referenzlaeufe/2026-08-14_B0`, alle **8** Referenzprojekte auf einer eigenen, migrierten Kopie **ohne** Präparierung | **GESAMT: PASS** — 8/8, **2 033 047 Werte** innerhalb der Toleranz. Die Referenzprojekte tragen `Pendelspeicher = 0`/leer, bekommen also keinen Puffer und rechnen mit Volumen 0. |
| 6 | **Migration nach dem Umbau** (R4/R6 über `ProjektPuffer`), frische präparierte Kopie | Zählwerk **unverändert** gegenüber Etappe 2: 3 Puffer mit Verwendung, 10 Anlagen auf Puffer, 33 auf Heizkreis, 1 Quell-Puffer, 20 Anlagenzeilen, 3 Pendelspeicher, 10 Hinweise, **0 Abweichungen** im Schema-Nachweis. |

**Lese-/Schreibpfad des Controllers** (eigenes Wegwerfwerkzeug `UiProbe` außerhalb des
Repos, ruft die `internal`-Klasse über Reflection auf einer eigenen Kopie auf):

| Fall | Ergebnis |
|---|---|
| Vorhandene Zeile lesen | 1017 → **800** l |
| `Set(950)` → `Get()` | **950**, ein `UPDATE`, keine zweite Zeile |
| `Set(0)` bei vorhandener Zeile | `true`, `Get() = 0`, **Zeile bleibt** stehen |
| `Set(-5)` | **`false`**, Wert unverändert |
| `Set(0)` ohne vorhandene Zeile | `true`, **nichts angelegt** |
| `Set(1200)` ohne vorhandene Zeile, Projekt 1024 (BHKW) | Puffer angelegt (`Pufferspeicher`, Verluste 0, Kosten 0, `Verwendung='Heizung'`), Anlagenzeile `ID_Type = 12` mit `ID_PUFFER`, `ID_Carrier` NULL, BHKW-Anlage auf `WS_Ziel='PufferHeizung'` + `WS_ID_Puffer` — **zeilengleich zu den von R6 erzeugten Datensätzen** |
| `Set(1200)` ein zweites Mal | kein Duplikat, weder Puffer noch Anlagenzeile |
| Projekt ohne BHKW (1011) | Puffer + Anlagenzeile angelegt, keine Anlage umgestellt |
| `Get(999999)`, `Set(0, 100)` | **0** bzw. **`false`** |

### 5. Oberfläche

`Form_Simulation_Detail`, neue Methode `PendelspeicherFeldEinrichten()` (aufgerufen aus
`Form_Simulation_Detail_Load` direkt nach `LeseKonfiguration`):

- `label56.Text = "Volumen Pendelspeicher [l]"` — **im Code-Behind**, nicht in der `.resx`.
  Der Designer würde die Änderung beim nächsten Öffnen zurückschreiben, und die
  Satellitendateien dieses Ordners kennen `label56.Text` ohnehin nicht (geprüft:
  weder in `de-DE` noch in `en-US`). Die durchgängige Lokalisierung der
  Simulationsformulare ist **Paket 9**; dort gehört der Text in die Satelliten-`.resx`.
  Der neutrale `.resx`-Eintrag „Volumen Pendelspeicher [m³]" bleibt unangetastet und
  wird zur Laufzeit überschrieben.
- `DecimalPlaces = 0`, `Increment = 50`, `Minimum = 0`, `Maximum = 1 000 000` — ebenfalls
  im Code-Behind. Der Designer-Stand (eine Nachkommastelle, Schrittweite 0,1) stammt aus
  der m³-Zeit, und das **Maximum lag bei 100** (Vorgabewert von `NumericUpDown`, in der
  `.resx` nicht gesetzt): in Litern hätte das jede sinnvolle Eingabe abgeschnitten und
  beim Laden eines migrierten Werts eine `ArgumentOutOfRangeException` ausgelöst.
- Laden über `PendelspeicherVolumenLiter`, Speichern in `numericUpDown_Volumen_Leave`
  über `SetPendelspeicherVolumenLiter` — dieselbe Stelle, an der bisher
  `SpeichereKonfigurationsAenderung(model => model.Pendelspeicher = …)` stand.
  Eingabe ganzzahlig, `int.TryParse` invariant, negative Werte werden auf 0 gezogen.
- `LeseKonfiguration()` setzt `numericUpDown_Volumen` **nicht** mehr.

### 6. Die tote Spalte

`Tab_Einstellungen.Pendelspeicher` bleibt physisch bestehen und wird von
`KonfigurationCtrl` weiterhin **positionsbasiert** gelesen (`row[22]`) und in
`Insert`/`Update` mitgeschrieben — beides absichtlich unangetastet, damit weder die
Ordinalzugriffe `row[0…22]` noch die Spaltenlisten der Anweisungen kippen. Nur die
**Konsumenten** sind weggefallen. `KonfigurationModel.Pendelspeicher` trägt jetzt einen
Kommentar, der das festhält.

`grep` über das Projekt nach `.Pendelspeicher`: **0 auswertende Lesestellen**. Übrig
bleiben `KonfigurationCtrl.cs:66` (Mapping), `:113` und `:182` (Schreiben) sowie
Kommentare. Ebenso `grep` nach `20000 / 860` und `[m³]` im Simulationsbereich: nur noch
Fließtext in Kommentaren, Konzept und diesem Protokoll — und der neutrale
`.resx`-Eintrag, den die Oberfläche zur Laufzeit ersetzt.

### 7. Geänderte und neue Dateien (Etappe 3)

**Neu**

- `Allgemein/Update/ProjektPuffer.cs` (UTF-8 mit BOM, CRLF)

**Geändert**

| Datei | Änderung |
|---|---|
| `Allgemein/Update/SchemaMigration.cs` | Konstanten als Aliase auf `ProjektPuffer`; R4 `AnlagenzeileAnlegen` und R6 (Puffer-INSERT, BHKW-Senke) auf die gemeinsamen Bausteine umgestellt; R6-Doku nachgezogen. **UTF-8 mit BOM, LF — unverändert.** |
| `Controller/PufferSpCtrl.cs` | `PendelspeicherVolumenLiter`, `SetPendelspeicherVolumenLiter`, Helfer `PendelspeicherId`, `AnlagenzeileVorhanden`, `StillScalar`, `StillNonQuery` |
| `Allgemein/Simulation/SimulationControl.cs` | Feld `VolumenPendelspeicherBHKW` führt Liter (dokumentiert); Kapazitätsformel `· 20 / 860` |
| `Allgemein/Simulation/SimulationRunner.cs` | Volumen aus `PufferSpCtrl.PendelspeicherVolumenLiter`; Kommentar |
| `Views/Simulation/Form_Simulation_Detail.cs` | `PendelspeicherFeldEinrichten()`; Simulationsstart und Speichern auf die neue Quelle; `LeseKonfiguration` ohne Pendelspeicher; `using System.Globalization` |
| `Model/KonfigurationModel.cs` | Kommentar „tot seit Etappe 3" an `Pendelspeicher` |
| `Allgemein/Simulation/Paket1_SchemaMigration_Protokoll.md` | dieser Abschnitt |

Kodierungen aller berührten Dateien geprüft: BOM-Zustand unverändert, Zeilenenden je
Datei einheitlich (`SchemaMigration.cs` weiterhin LF, alle übrigen CRLF), keine
Ersatzzeichen (`U+FFFD`). Kein Designer- und kein `.resx`-Eintrag von Hand geändert.
Die Dateien der Parallelarbeit (`Form_Heizkessel*`, `SimulationSPK`, `RecordSet`,
`FormMain`, `Form_BHKWEing`, `WizardParent`, `WErzeugerModel`, `WizardCtrl`) sind
unberührt.

---

## Etappe 4 (14.08.2026) — Betriebstemperaturen je Puffer, Niedertemperatur zugelassen

Nutzeranforderung vom 14.08.2026: Vor- und Rücklauftemperatur werden **je Puffer**
vorgebbar, richten sich als Vorbelegung an den **Systemvorgaben des Projekts** aus und
lassen ausdrücklich auch **niedrige** Temperaturen zu (Niedertemperatursysteme,
z. B. 35/28).

### 1. Die führende Ablage und die Rückfallkette

Konzept 5.1 sagt: „Die Betriebsparameter wandern von der Zuordnung an den **Speicher
selbst** — ein Puffer hat genau einen Betriebszustand, unabhängig davon, wie viele
Anlagen ihn laden." Seit dieser Etappe ist das kein Plan mehr, sondern der Lesepfad:

| Stufe | Quelle | Bedingung |
|---|---|---|
| **1** | `Tab_Pufferspeicher.Vorlauf` / `.Ruecklauf` | **beide** gesetzt und > 0 |
| **2** | `Z_ProjektPufferSp.Vorlauf` / `.Ruecklauf` der WP-Zuordnung | sonst |
| **3** | Engine-Vorgabe `ΔT = 10 K` in `SimulationPufferspeicher.Init` | wenn auch dort nichts Brauchbares steht (`ΔT ≤ 0`) |

**Warum „beide und > 0"?** Eine halbe Angabe ergäbe keine auswertbare Spreizung und
würde den Rückfall nur verdecken — der Speicher sähe gepflegt aus und rechnete doch mit
der Engine-Vorgabe. `PufferSpCtrl.TemperaturenLesen` liefert deshalb nur bei einem
vollständigen Paar `true`; alles andere heißt „nimm den Rückfallweg", nicht „Fehler".
Nachgewiesen in Abschnitt 6, Lauf 3c.

**Regressionsneutralität — die Begründung.** Migrationsregel **R1** (Etappe 2) schreibt
die Werte der **ersten WP-Zuordnung nach `Prioritaet`** an genau den Puffer, den diese
Zuordnung referenziert. Das ist dieselbe Zeile, aus der `SimulationControl` sie heute
liest (`break` nach dem ersten WP-Treffer). Auf einer migrierten Datenbank liefert
Stufe 1 damit **dieselben Zahlen** wie bisher Stufe 2 — der Vorrang ist wertgleich, nicht
nur toleranzgleich. An der Arbeitskopie belegt: Puffer **1008007** trägt `Vorlauf = 35`,
`Ruecklauf = 45`, die Zuordnungen 10058/10072 tragen 35/45. Nachgewiesen in Abschnitt 6,
Läufe 1 und 3b.

Geschrieben wird die Puffer-Zeile beim Speichern der Konfiguration
(`Form_Simulation_Config.btn_Speichern_Click`): nach einem erfolgreichen
`ctrlpsp.Insert()` zusätzlich `UPDATE Tab_Pufferspeicher SET Vorlauf, Ruecklauf` auf
`ctrlpsp.ID_Pufferspeicher` — nur wenn beide Werte > 0 sind. `Insert()` hat die ID
unmittelbar davor aufgelöst (und die Projektkopie bei Bedarf über `CopyFromStamm`
angelegt), sie zeigt also sicher auf `Tab_Pufferspeicher`. Die Zuordnung wird weiterhin
mitgeschrieben — sie ist ab jetzt die **Kopie**, nicht das Original. Der frische
B0-1/B0-11-Block ist dabei unangetastet geblieben; ergänzt sind zwei `TryParse`-Zeilen
und ein `else`-Zweig.

**Der zweite Leser: `WaermequelleClass.Quelltemperatur`, Fall `Pufferspeicher`.**
Konzept 5.4 kündigt diese Umstellung ausdrücklich an. Die Kette hat dort eine Stufe mehr,
weil es um den **Quell**speicher geht:

1. `WQ_ID_Puffer` der Anlage → dessen `Vorlauf`/`Ruecklauf` (Migrationsregel R3 löst
   diese Referenz aus dem Bezeichner auf),
2. der Puffer der WP-Zuordnung → dessen `Vorlauf`/`Ruecklauf`,
3. Altdaten: `Z_ProjektPufferSp.Vorlauf`/`.Ruecklauf` selbst — wie bisher.

Ergebnis ist unverändert die mittlere Temperatur `(V + R) / 2`. Stufe 2 ist wegen R1
wertgleich zu Stufe 3; Stufe 1 greift in der Arbeitskopie nur bei Projekt 1021
(Anlage 10361 → Puffer 1018014), das kein Referenzprojekt ist und dessen Puffer kein
Temperaturpaar trägt — der Fall fällt also weiter auf Stufe 3 durch. Deshalb bleibt die
Regression wertgleich.

Neu in `WaermequelleClass`: `SkalarStill()` — die zusätzliche Abfrage der **Stufe 2**
läuft **ohne Dialog**. Eine nicht migrierte Datenbank liefert dort `null` statt einer
MessageBox mitten im Engine-Lauf; genau dafür ist der Rückfallweg da (Konzept 13.4).

> **Präzisierung (Review-Nacharbeit).** Der Satz „läuft ohne Dialog" gilt nur für die
> **neu hinzugekommene** Abfrage. **Stufe 1** liest `WQ_ID_Puffer` über das bestehende
> `WaermequelleClass.WertLesen`, und das geht über `DataRepository.ExecuteScalar` —
> also über den Weg, der im Fehlerfall eine MessageBox zeigen kann.
>
> Das ist bewusst so geblieben und macht den Engine-Lauf **nicht schlechter**: derselbe
> `WertLesen`-Pfad wird im `Pufferspeicher`-Zweig ohnehin schon zweimal vorher benutzt
> (`WQ_Temp`, davor `WQ_Typ`/`WQ_Unbegrenzt`). Stufe 1 fügt also kein neues Dialogrisiko
> hinzu, sondern erbt das vorhandene. Die **Umstellung des gesamten `WertLesen` auf einen
> stillen Weg** ist eine eigene Änderung mit breitem Aufrufkreis und gehört nicht in
> Paket 1 — sie ist als offener Punkt vermerkt.

### 2. Systemvorgaben des Projekts

Neu in `PufferSpCtrl` (beide `static`, beide **still** — nur `Console.WriteLine`):

| Methode | Inhalt |
|---|---|
| `SystemVorlauf(idProjekt)` | **kleinster** `Vorlauf` über die Wärmeerzeuger-Anlagen des Projekts |
| `SystemRuecklauf(idProjekt)` | **größter** `[Rücklauf]` über dieselben Anlagen |

Anlagenkreis: `Tab_Energieanlagen` mit `ID_Type IN (1, 2, 10, 11)` — Wärmepumpe,
Solarthermie, Heizkessel, BHKW. Das ist die **konservative Auslegung für einen
gemeinsamen Speicher** (Konzept 13.7): der Speicher muss mit dem Erzeuger auskommen, der
am wenigsten Vorlauf liefert und am meisten Rücklauf zurückgibt. Rückgabe `int?`;
`null`, wenn keine Anlage einen gepflegten Wert trägt — dann wird **keine Zahl
erfunden**.

Drei Punkte, die bei der Umsetzung zwingend waren:

- **`[Rücklauf]` trägt den Umlaut** (an der Datenbank verifiziert, Konzept 13.7 /
  Befund B0-4) — anders als `Z_ProjektPufferSp.Ruecklauf` und
  `Tab_Pufferspeicher.Ruecklauf`.
- **Direktes, parametrisiertes SQL** statt der gespeicherten Access-Abfragen
  `Abfrage_Erzeuger_Vorlauftemperaturen` / `…Ruecklauftemperaturen`: deren Definitionen
  enden auf ein hartkodiertes `HAVING ID_Projekt = 8` und liefern für **jedes** Projekt
  0 Zeilen. Muster wie seit B0-9 in `Form_KonfigPufferspeicher`.
- **`AND Vorlauf > 0` bzw. `AND [Rücklauf] > 0`.** Das ist **keine
  Temperatur-Untergrenze**, sondern der Test auf „gepflegt": die Spalten tragen in
  Access den Spalten-Default 0 und sind nie `NULL`. Ohne den Filter zöge eine einzige
  unvollständig erfasste Anlage die Systemvorgabe auf 0. Belegt am Projekt 1008: Anlage
  **10134** („ecoTEC plus VCI 20/26CS/1-5", `ID_Type = 10`) trägt 0/0, die beiden
  Wärmepumpen 10132/10133 tragen 35/25 — mit Filter ergibt sich **35/25**, ohne Filter
  **0/0**.

Gemessen auf der migrierten Arbeitskopie (Werkzeug `Probe`, Abschnitt 6):

| Projekt | SystemVorlauf | SystemRücklauf | Bemerkung |
|---|---|---|---|
| 1007 | 35 | 25 | Niedertemperatur |
| 1008 | 35 | 25 | Niedertemperatur |
| 1010 | 55 | *(null)* | kein gepflegter Rücklauf |
| 1011 | 35 | *(null)* | dito |
| 1017 | 55 | 45 | brauchbares Paar |
| 1018 | *(null)* | *(null)* | keine gepflegten Temperaturen |
| 1023 | 45 | 60 | **vertauscht** (Bestandsdatenfehler) |
| 1024 | 45 | 60 | dito |

Die Struktur (SQL-Text + Parameterliste) steht in `ProjektPuffer`, die Ausführung in
`PufferSpCtrl` bzw. in `SchemaMigration` auf deren eigener stiller Verbindung — dasselbe
Muster wie bei den übrigen Bausteinen aus Etappe 3. Die Migration darf keine zweite
Verbindung auf eine Datei öffnen, die sie gerade umbaut.

**`ProjektPuffer.IstTemperaturpaar(v, r)`** entscheidet einheitlich, ob ein Paar als
Betriebsvorgabe taugt: beide vorhanden, `r > 0`, `v > r`. Der Test auf `v > r` ist nicht
Kosmetik — die Projekte 1023/1024 liefern als Systemvorgabe **45/60 °C**. Ein solches
Paar an den Speicher zu schreiben wäre schlechter als gar nichts: es sähe gepflegt aus
und ergäbe über `ΔT ≤ 0` doch nur den stillen Rückfall. **Eine Untergrenze gibt es
nicht** — 35/28 und tiefer sind gültige Paare.

### 3. Vorbelegung des BHKW-Pendelspeichers (R6)

`SchemaMigration` R6 **und** `PufferSpCtrl.SetPendelspeicherVolumenLiter` schreiben beim
**Anlegen** des Puffers `BHKW-Pendelspeicher` die Systemvorgaben mit — sonst bleiben
beide Spalten `NULL`. `ProjektPuffer.SQL_PUFFER_INSERT` führt dafür zwei Spalten mehr;
die zwei neuen Parameter sind optional, der Aufrufkreis ist unverändert.

Bewusst **keine** eingebaute Vorbelegung „55/35" o. ä.: bei einem Niedertemperatursystem
wäre jede erfundene Zahl falsch. Die Temperaturen kommen aus den Erzeugern selbst oder
gar nicht.

**Heute ergebnisneutral.** `SimulationControl.Simulation_BHKW_Ctrl` rechnet die Kapazität
des Pendelspeichers weiterhin als `Liter · 20 / 860`, also mit **fest 20 K**; die
Temperaturen am Speicher liest dort niemand. Sie sind die Vorbereitung für **Paket 6** —
erst dort zieht die Engine die Kapazität aus `SimulationPufferspeicher`, und **dann
bestimmt die hier abgelegte Spreizung die Kapazität**. Ein Projekt mit 55/45 rechnet
danach mit 10 K statt 20 K, also mit der halben Pendelspeicher-Kapazität. Das ist die
beabsichtigte Wirkung; sie gehört in die Abnahme von Paket 6, nicht hierher.

Neues Zählwerk: `SchemaMigration.DatenPendelspeicherTemperaturen` (davon mit
Systemtemperaturen vorbelegt), im Migrationslauf-Protokoll ausgewiesen.

### 4. Aufgehobene Grenzen und enge Vorannahmen

Systematisch gesucht wurde nach Untergrenzen, Klemmungen und festen Vorbelegungen für
Vor-/Rücklauf im Simulations- und Pufferbereich. **Kernbefund: es gibt keine einzige
Stelle, die einen Vorlauf ≥ 50/55 °C oder einen Rücklauf ≥ 35 °C erzwingt.** Kein Modell
trägt eine hartkodierte „55/35"-Vorbelegung — alle VL/RL-Defaults sind 0. Die
tatsächlichen Hindernisse waren andere:

Die Zeilennummern in den beiden folgenden Tabellen sind auf den Stand **nach der
Review-Nacharbeit (14.08.2026)** nachgezogen; sie sind eine Momentaufnahme, der
Member-Name daneben ist der belastbare Verweis.

| Fundstelle | Befund | Behandlung |
|---|---|---|
| `Form_KonfigPufferspeicher.btn_OK_Click` (`:42-47`) | `Int32.Parse` auf dem Eingabefeld; leer → **stillschweigend 0** (ergab `ΔT ≤ 0` und damit den verdeckten 10-K-Rückfall), „35,5" → unbehandelte `FormatException` | **aufgehoben**: `ProjektPuffer.TemperaturenPruefen`, Meldung + `DialogResult.None` |
| `Form_Simulation_Config.btn_Speichern_Click` (`:1834-1835`) | `Int32.Parse` **nach** `ctrlpsp.Delete()` — eine leere Zelle riss das Speichern mit `FormatException` ab, nachdem alle Zuordnungen bereits gelöscht waren (Konzept 4.6) | **aufgehoben**: `Int32.TryParse`, Unlesbares → 0 |
| `Wizard_WPItem.RUECKLAUF_VORSCHLAEGE` (verwendet `:41` und `:67`) | Rücklauf-Vorschlagsliste **25/30/35/40/45** — für 35/28 gab es keinen Eintrag. Über `Tab_Energieanlagen.[Rücklauf]` steuert das direkt die Systemvorgabe | **aufgehoben**: Liste auf `20, 22, 25, 28, 30, 32, 35, 40, 45` erweitert und unten feiner gestuft (eine Stelle statt zwei) |
| `Wizard_WPItem.btn_Beenden_Click` (`:161-171`) | `Int32.Parse` auf einer **frei beschreibbaren** ComboBox; `<` statt `<=` ließ `Vorlauf == Rücklauf` durch (Spreizung 0 → stiller Rückfall); keine Obergrenze | **aufgehoben**: dieselbe `TemperaturenPruefen` |
| `Form_KonfigPufferspeicher`, Vorbelegungs-SQL (`:84`, `:93`) | Vorbelegung ohne `> 0`-Filter — eine ungepflegte Anlage zog sie auf 0 | **ergänzt**: `AND e.Vorlauf > 0` / `AND e.[Rücklauf] > 0` |
| `Form_Simulation_Config.ListView_MouseDoubleClick`, Zelleditor (`:1437-1447`) | Übernahme **ohne jede Prüfung** — ein vertauschtes Paar (35/45) landete unbemerkt im Datenbestand | **aufgehoben in der Nacharbeit** (B4-2, siehe unten): `TemperaturPaarPruefen` → `ProjektPuffer.TemperaturenPruefen` |

Geprüft und **bewusst unverändert** gelassen:

| Fundstelle | Befund | Warum unverändert |
|---|---|---|
| `SimulationPufferspeicher.Init` (`:71`) | `if (deltaT <= 0) deltaT = 10;` | Das **ist** Stufe 3 der Rückfallkette. Greift bei 35/28 (`ΔT = 7`) nicht. |
| `SimulationControl.Simulation_BHKW_Ctrl` (`:383`) | `kapazitaetPendelspeicher = Liter · 20 / 860` — feste **20 K**, VL/RL gehen nicht ein | Ändern würde die Referenzergebnisse verschieben. Gehört zu **Paket 6** (siehe 3.). |
| `SimulationRunner.cs:155`, `Form_Simulation_Detail.cs:1293` | `Kapazitaet_Pufferspeicher = Volumen · 1,16` — implizit **ΔT = 1 K** | Reine Ergebniskennzahl, in den Referenzläufen enthalten (`Waermepumpe.Kapazitaet_Pufferspeicher`). Ergebniswirksam, deshalb nicht in dieser Etappe. |
| `WaermequelleClass.Quellspeicher` (`:498-499`, `:508`) | Quellspeicher-Spreizung Default 5 K, Klemmung nur bei `≤ 0`, `Math.Round` auf ganze K | Keine Untergrenze; 7 K läuft durch. Die Rundung ist eine Auflösungsfrage (`int`-Schema). |
| `Form_Simulation_Config`, Zelleditor `KeyPress` (`:1442-1447`) | lässt **nur Ziffern** zu | 35/28 geht; betroffen sind nur Nachkommastellen und negative Werte. |
| `SimulationWaermepumpe` (`:178-190`) | Anlagen-Vorlauf muss **exakt** einer `Tab_Kenndaten`-Stützstelle entsprechen, sonst Abbruch | Kennliniensache, keine Temperaturgrenze. Für 35 °C muss eine 35-°C-Kennlinie gepflegt sein. |

**Bekannte Restbeschränkung: Ganzzahligkeit.** `Tab_Pufferspeicher.Vorlauf`/`.Ruecklauf`
und `Z_ProjektPufferSp.Vorlauf`/`.Ruecklauf` sind `LONG`, die Modelle führen `int`,
der Zelleditor lässt kein Komma zu. **28,5 °C ist nirgends abbildbar** — das ist eine
Auflösungs-, keine Bereichsgrenze und war nicht Teil dieser Etappe.

### 5. Die einheitliche Validierung

`ProjektPuffer.TemperaturenPruefen(vorlaufText, ruecklaufText, out v, out r, out fehler)`
— eine Stelle, zwei Aufrufer (`Form_KonfigPufferspeicher.btn_OK`,
`Wizard_WPItem.btn_Beenden`). Regeln:

| Regel | Meldung (deutsch) |
|---|---|
| beide Felder sind ganze Zahlen (`int.TryParse`, invariant) | „Bitte eine Vor-/Rücklauftemperatur als ganze Zahl eingeben (°C)." |
| `Rücklauf > 0` | „Die Rücklauftemperatur muss größer als 0 °C sein." |
| `Vorlauf > Rücklauf` | „Die Vorlauftemperatur muss über der Rücklauftemperatur liegen." + die eingegebenen Werte |
| `Vorlauf ≤ 110` | „Die Vorlauftemperatur darf höchstens 110 °C betragen." |

**Darüber hinaus keine Untergrenze.** Die 110 °C sind ein Tippfehlerschutz (Siedepunkt
bei Umgebungsdruck), keine fachliche Auslegungsgrenze. Der Aufrufer zeigt den Text und
lässt den Dialog offen (`DialogResult.None`).

### 6. Verifikation

Basis: eigene Kopien in `C:\Waermeplan\Etappe4_Test` — **außerhalb des Repos**, Modus
`projekt` statt `lauf` (der legt `Referenzlaeufe\Arbeitskopie` neu an und wird parallel
von einer anderen Sitzung benutzt). Die produktive
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde zu keinem Zeitpunkt geöffnet oder
beschrieben.

| # | Lauf | Ergebnis |
|---|---|---|
| 0 | Build `WP-Plan.sln` Debug/x86 über MSBuild 2022 | **0 Fehler** (nur Bestandswarnungen). `Referenzlauf.csproj` ebenfalls 0 Fehler. |
| 1 | **Migration** auf frischer Kopie der Arbeitskopie | `SchemaVersion` 0 → **5**, Schema-Nachweis **0 Abweichungen**. Zählwerk **unverändert** gegenüber Etappe 2/3: 3 Puffer mit Verwendung, 6 Anlagen auf Puffer, 37 auf Heizkreis, 1 Quell-Puffer, 17 Anlagenzeilen, 0 Pendelspeicher, 10 Hinweise. |
| 2 | **Regression**: alle **8** Referenzprojekte auf dieser migrierten Kopie, `vergleich` gegen `Referenzlaeufe/2026-08-14_B0` | **GESAMT: PASS** — 8/8, **2 033 047 Werte** innerhalb der Toleranz. Die Begründung aus Abschnitt 1 geht damit auf. |
| 3a | **Führende Ablage**, Projekt 1008: Puffer **1008007** direkt in `Tab_Pufferspeicher` auf **35/28** gesetzt | `Q_max` **6,96 → 4,872 kWh** = 600 l · 1,16 · **7 K** / 1000 — die Engine folgt dem Puffer, nicht mehr der Z-Zeile (35/45 → `ΔT ≤ 0` → Vorgabe 10 K → 6,96). Vergleich gegen B0: **FAIL mit 30 472 Abweichungen** — genau die gewollte Wirkung. |
| 3b | dieselbe Kopie, Puffer-Werte auf **NULL** | `Q_max` zurück auf **6,96 kWh**, Vergleich gegen B0 **PASS (227 832 Werte)** — der Z-Rückfall greift und ist wertgleich. |
| 3c | dieselbe Kopie, **halbes** Paar (`Vorlauf = 35`, `Ruecklauf = NULL`) | `Q_max` **6,96 kWh**, Vergleich **PASS** — ein unvollständiges Paar löst den Rückfall aus, statt eine sinnlose Spreizung zu erzeugen. |
| 4 | **Validierung** (Wegwerfwerkzeug `Probe` außerhalb des Repos, Reflection auf die `internal`-Klassen) | 16 Fälle, **16/16 wie erwartet** — Einzelheiten unten. |
| 5 | **R6-Vorbelegung** an eigens präpariertem Bestand (`Pendelspeicher` 1017 = 0,8 m³, 1018 = 1,5 m³, 1024 = 2,0 m³) | 3 Pendelspeicher angelegt, davon **1 von 3** mit Systemtemperaturen vorbelegt (`DatenPendelspeicherTemperaturen = 1`). Die anderen zwei bleiben absichtlich leer: 1018 hat gar keine gepflegten Erzeugertemperaturen, 1024 ein vertauschtes Paar (45/60). Einzelheiten unten. |
| 6 | **No-op**: zweiter Migrationslauf auf derselben Kopie (`--nokopie`) | Alle fünf Schritte „bereits erledigt", `SchemaVersion` bleibt **5**, `MigrationOk = true`, Schema-Nachweis erneut **0 Abweichungen**, 0 neue Pendelspeicher. |

**Zahlen zu Lauf 3** (Projekt 1008, `aggregate.csv`):

| Größe | B0-Referenz (= Z-Werte 35/45) | Puffer auf 35/28 |
|---|---|---|
| `Puffer.Q_max` [kWh] | 6,96 | **4,872** |
| `Puffer.Ladung_gesamt` [kWh] | 22 260,33 | 18 432,84 |
| `Puffer.Entladung_gesamt` [kWh] | 21 837,93 | 17 982,33 |
| `Puffer.Verluste_gesamt` [kWh] | 415,64 | 445,73 |
| `Vektor.puffer_soc.Summe` | 32 645,92 | 24 372,62 |
| `Sim.Restwaerme` [MWh] | 17,5589 | 17,7764 |

Nach dem Nullen (3b) stehen wieder exakt die Werte der linken Spalte.

**Zahlen zu Lauf 4** (Auszug, alle Fälle mit erwartetem Ergebnis bestanden):

| Eingabe | Ergebnis |
|---|---|
| `35` / `28` | **angenommen** (V = 35, R = 28) — der geforderte Niedertemperaturfall |
| `30` / `25`, `25` / `20` | angenommen — noch tiefer, ebenfalls gültig |
| `70` / `50`, `110` / `40` | angenommen |
| `28` / `35` | abgelehnt: „Die Vorlauftemperatur muss über der Rücklauftemperatur liegen." |
| `` / ``, `` / `28`, `35` / `` | abgelehnt, **kein Absturz**: „Bitte eine … als ganze Zahl eingeben (°C)." |
| `abc` / `28`, `35,5` / `28` | abgelehnt, kein Absturz |
| `35` / `35` | abgelehnt (Spreizung 0) |
| `35` / `0`, `35` / `-5` | abgelehnt: „Die Rücklauftemperatur muss größer als 0 °C sein." |
| `111` / `40` | abgelehnt: „… höchstens 110 °C" |
| `" 35 "` / `" 28 "` | angenommen (Leerzeichen werden getrimmt) |

Zusätzlich am Datenbankpfad geprüft: `TemperaturenLesen(1008007)` → `false` bei leeren
Spalten, `true` mit 35/28 nach `SetTemperaturen`; `SetTemperaturen(id, 0, 28)` → `false`
und **kein** Schreibzugriff; `TemperaturenLesen(0)` und `TemperaturenLesen(99999999)` →
`false` ohne Ausnahme.

**Zahlen zu Lauf 5** (R6-Vorbelegung, drei Fälle mit Absicht verschieden):

| Projekt | Systemvorgabe | Ergebnis am neuen Puffer |
|---|---|---|
| 1017 | 55 / 45 | Puffer 1054164, 800 l, **`Vorlauf = 55`, `Ruecklauf = 45`** |
| 1018 | *(null)* / *(null)* | Puffer 1054165, 1500 l, **beide leer** — Protokoll: „keine brauchbaren Systemvorgaben (Vorlauf -, Rücklauf -)" |
| 1024 | 45 / 60 (**vertauscht**) | Puffer 1054166, 2000 l, **beide leer** — Protokoll: „keine brauchbaren Systemvorgaben (Vorlauf 45, Rücklauf 60)" |

Die übrigen Zählwerte dieses Laufs sind unverändert gegenüber Etappe 3: 3 Puffer mit
Verwendung, 10 Anlagen auf Puffer, 33 auf Heizkreis, 1 Quell-Puffer, 20 Anlagenzeilen,
3 Pendelspeicher, 10 Hinweise, **0 Abweichungen** im Schema-Nachweis.

### 7. Geänderte Dateien (Etappe 4)

| Datei | Änderung |
|---|---|
| `Allgemein/Update/ProjektPuffer.cs` | `TYP_WP`/`TYP_SOLARTHERMIE`/`TYP_KESSEL` + `WAERMEERZEUGER_TYPEN`; `SQL_SYSTEM_VORLAUF`/`SQL_SYSTEM_RUECKLAUF` + `SystemTemperaturParameter`; `IstTemperaturpaar`; `SQL_PUFFER_TEMPERATUREN(_UPDATE)`; `SQL_PUFFER_INSERT`/`PufferParameter` um Vorlauf/Ruecklauf erweitert (optionale Parameter); `MAX_VORLAUF_C`, `TemperaturenPruefen`; `using System.Globalization` |
| `Controller/PufferSpCtrl.cs` | `SystemVorlauf`/`SystemRuecklauf` (+ privater `SystemTemperatur`), `TemperaturenLesen`, `SetTemperaturen`; `SetPendelspeicherVolumenLiter` gibt die Systemvorgaben an den INSERT weiter |
| `Allgemein/Update/SchemaMigration.cs` | R6 belegt Vorlauf/Ruecklauf vor; Helfer `SystemTemperatur(Lauf, …)`; Zählwerk `DatenPendelspeicherTemperaturen` (+ Rücksetzung + Zusammenfassungszeile) |
| `Allgemein/Simulation/SimulationControl.cs` | `puffer_wp.Init` bevorzugt die Puffer-Zeile, sonst die Zuordnung |
| `Allgemein/Simulation/WaermequelleClass.cs` | Quell-Puffertemperatur in drei Stufen; neue Helfer `SkalarStill`, `ZahlOderNull`. **Kodierung UTF-8 ohne BOM beibehalten.** |
| `Views/Simulation/Form_Simulation_Config.cs` | `TryParse` statt `Int32.Parse`; Puffer-Zeile nach erfolgreichem `Insert()` mitschreiben |
| `Views/Simulation/Form_KonfigPufferspeicher.cs` | `btn_OK` über `TemperaturenPruefen` (`DialogResult.None`); `> 0`-Filter in der Vorbelegung |
| `Views/Wizard/Wizard_WPItem.cs` | Rücklauf-Vorschlagsliste erweitert (eine Stelle); `btn_Beenden` über `TemperaturenPruefen` |
| `../Referenzlauf/Migrationslauf.cs` | R6-Temperaturzähler im Lauf-Protokoll |
| `Allgemein/Simulation/Paket1_SchemaMigration_Protokoll.md` | dieser Abschnitt |

Kein Designer- und kein `.resx`-Eintrag von Hand geändert. Kodierungen aller berührten
Dateien geprüft: BOM-Zustand unverändert (`WaermequelleClass.cs` weiterhin **ohne** BOM,
alle übrigen mit), Zeilenenden je Datei einheitlich (`Form_Simulation_Config.cs`,
`SchemaMigration.cs`, `Wizard_WPItem.cs`, `Migrationslauf.cs` weiterhin LF, die übrigen
CRLF), keine Ersatzzeichen (`U+FFFD`). Die Dateien der Parallelarbeit
(`Form_Heizkessel*`, `SimulationSPK`, `RecordSet`, `FormMain`, `Form_BHKWEing`,
`WizardParent`, `WErzeugerModel`, `WizardCtrl`) sind unberührt — `WErzeugerModel` wurde
nur gelesen.

---

## Nacharbeit zum Review (14.08.2026)

Nacharbeit der konsolidierten Befunde aus beiden Review-Teilen am uncommitteten Paket 1.
Zwölf Punkte, in der Reihenfolge ihrer Wirkung: erst was die Migration ganz verhindern
konnte, dann was still falsche Daten schrieb, zuletzt die Dokumentation. **Nichts
committet.**

### 1. Bootstrap der Statuszeile (Fix 1)

**Befund.** Ist `Tab_Applikation` leer, scheiterte die Migration **am Bootstrap** — also
bevor auch nur ein Schritt lief. Beide Versuche der bisherigen Fassung konnten auf diesem
Schema gar nicht gelingen; an der Datenbank nachgewiesen:

| Anweisung | Antwort der Datenbank |
|---|---|
| `INSERT INTO [Tab_Applikation] (ID, [SchemaVersion]) VALUES (1, 0)` | *„Sie müssen einen Wert in das Feld 'Tab_Applikation.Projektname' eingeben."* |
| `INSERT INTO [Tab_Applikation] ([SchemaVersion]) VALUES (0)` | *„Sie müssen einen Wert in das Feld 'Tab_Applikation.ID' eingeben."* |

Der Grund steht im Schema (`FillSchema` auf `Tab_Applikation`, verifiziert 14.08.2026):

| Spalte | AutoWert | NULL erlaubt |
|---|---|---|
| `ID` | **nein** | **nein** |
| `Projektname` | nein | **nein** |
| `ID_Projekt`, `Beschreibung`, `Icon`, `Version_Major`, `Version_Minor` | nein | ja |

`ID` ist kein AutoWert — der „Rückfallweg für den AutoWert-Fall" konnte deshalb nie
greifen. Und `Projektname` ist ein Pflichtfeld ohne Spalten-Default, was der erste
Versuch nicht bediente. Erschwerend: der Rückfallweg setzte vorher `l.LetzterFehler = null`
und **löschte damit die aussagekräftige Meldung**. Im Bericht landete „… Feld
'Tab_Applikation.ID' …" — ein Hinweis auf die ID, obwohl der Projektname fehlte.

**Umsetzung** (`SchemaMigration.StatuszeileAnlegen`, neu):

- ID selbst vergeben nach dem `GetMaxID + 1`-Muster. `MAX(ID)` liefert auf der leeren
  Tabelle NULL, `Zahl()` macht daraus 0, also die **1** — Nz-sicher, ohne die
  Access-Funktion `Nz` zu brauchen (die kennt der OLE-DB-Provider außerhalb von Access
  nicht).
- `Projektname = ''` mitschreiben.
- Drei Stufen statt zwei: `(ID, Projektname, SchemaVersion)`, dann `(ID, SchemaVersion)`
  für Schemata ohne `Projektname`, dann `(SchemaVersion)` für einen echten AutoWert.
- **Die Meldung des ERSTEN Versuchs wird gemerkt und am Ende wieder eingesetzt**; der
  Fehler der letzten Stufe steht als Detailnotiz daneben.
- Die Kopfmeldung nennt jetzt den Pflichtfeld-Fall als dritten möglichen Grund neben
  Schreibschutz und Fremdsperre.

**Nachweis.** Kopie mit `DELETE FROM Tab_Applikation`, danach Migration: **Bootstrap OK,
alle fünf Schritte OK, `SchemaVersion` 0 → 5, Schema-Nachweis 0 Abweichungen.** Die
angelegte Zeile: `ID = 1`, `Projektname = ''`, `SchemaVersion = 5`.

### 2. Die Blockade wird im Detailfenster sichtbar (Fix 2, Befund A2)

**Befund.** `SimulationControl.Do_Simulation` kehrt bei gesperrter Migration früh zurück
— dialogfrei, wie es sich für die Engine gehört. `Form_Simulation_Detail` merkte davon
nichts: es rief anschließend `Endergebniss_Simulation()` und `FuelleUebersicht()` auf
**leeren Objekten** auf. Der Anwender sah ein vollständig aussehendes Ergebnis aus
Nullwerten — und konnte es über „Ergebnis speichern" in die Datenbank schreiben.

**Umsetzung.** Neue Methode `SimulationBlockiert()`:

- prüft `SchemaMigration.SimulationGesperrt(out grund)`,
- zeigt die Meldung **genau einmal**,
- deaktiviert `btn_ErgebnisSpeichern` — kein Ergebnis entsteht, also darf auch keines
  gespeichert werden.

Aufgerufen an zwei Stellen: in `Form_Simulation_Detail_Load` **vor** dem automatischen
Lauf (mit `return`, deshalb keine zweite Meldung) und am Anfang von
`btn_Simulation_Click` für den manuellen Druck. Die Prüfung im `Load` steht bewusst vor
`ctrl.ReadSingle(...)`: bei nicht lesbarer `Tab_Einstellungen` liefe der automatische
Lauf sonst gar nicht erst an, und die Meldung bliebe aus.

`FormMain` ist **nicht** angefasst (Parallelarbeit).

### 3. Nur die wirksame Zuordnung schreibt an den Puffer (Fix 3, Befund B4-1)

**Befund.** Der Schreibpfad aus Etappe 4 schrieb `Tab_Pufferspeicher.Vorlauf/Ruecklauf`
nach **jedem** erfolgreichen `ctrlpsp.Insert()` — also auch aus BHKW-, Heizkessel-,
Solarthermie- und Gesamtsystem-Zeilen und aus jeder weiteren Wärmepumpen-Zeile. Die
zuletzt gespeicherte Zeile gewann.

Das hebelt zwei Entscheidungen aus:

- **R2 der Migration** („wirkungslose Altzuordnungen bleiben wirkungslos"): eine
  BHKW-Zuordnung, die die Engine nie gelesen hat, wäre über die führende Ablage plötzlich
  ergebniswirksam geworden.
- **Den Vorrang der führenden Ablage selbst** (Etappe 4, Abschnitt 1): am Speicher stünde
  ein Paar, das mit der von der Engine gelesenen Zeile nichts zu tun hat.

**Umsetzung.** `btn_Speichern_Click` schreibt nur noch aus **einer** Zeile: der ersten
`Wärmepumpe`-Zuordnung. Das ist deterministisch dieselbe, die die Engine auswertet:
`SimulationControl` liest `ORDER BY Prioritaet`, überspringt Nicht-WP-Zeilen mit
`continue` und bricht nach dem ersten Treffer mit `break` ab; die Priorität vergibt die
Speicherschleife fortlaufend in Listenreihenfolge (`prioritaet++`), also gewinnt die erste
WP-Zeile der Liste — und die bekommt zugleich die kleinste `ID`, womit auch der
Gleichstandsfall von R1 (`ORDER BY Prioritaet, ID`) dieselbe Zeile wählt. Umgesetzt über
das Merkerfeld `pufferZeileGeschrieben`; das Erzeuger-Literal steht jetzt einmal in
`ProjektPuffer.ERZEUGER_WAERMEPUMPE` und wird von Engine, Migration und Dialog geteilt.

Der Kommentar an der Stelle nennt die Begründung ausdrücklich, damit die
R2-Entscheidung nicht versehentlich wieder aufgehoben wird.

### 4. Der Bearbeitungspfad validiert (Fix 4, Befund B4-2)

**Befund.** Der ListView-Zelleditor übernahm den Text ungeprüft
(`item.SubItems[i].Text = textBox.Text`). Ein vertauschtes Paar wie 35/45 landete
unbemerkt im Datenbestand — und ab Etappe 4 über den Schreibpfad an der führenden Ablage.
Zusätzlich prüfte `PufferSpCtrl.SetTemperaturen` nur auf `> 0` und hätte 35/45 klaglos
geschrieben.

**Umsetzung.**

- Zelleditor: neue Prüfung `TemperaturPaarPruefen(vorlauf, ruecklauf, out fehler)`, die auf
  `ProjektPuffer.TemperaturenPruefen` aufsetzt — dieselbe Stelle wie
  `Form_KonfigPufferspeicher` und `Wizard_WPItem`. Geprüft wird das **Paar**, nicht die
  einzelne Zelle: die Gegenzelle steht bereits im ListView. Bei Verstoß Meldung und
  Rücksetzen auf den letzten gültigen Wert.
- Zwei Zustände gelten ausdrücklich als in Ordnung, obwohl `TemperaturenPruefen` sie
  ablehnen würde: **beide Zellen leer/0** (das ist die Rücknahme, Fix 5) und **genau eine
  Zelle gefüllt** (der unvermeidliche Zwischenstand — wer die erste von zwei Zellen füllt,
  darf nicht mit einer Meldung unterbrochen werden; ein halbes Paar wird ohnehin nirgends
  an den Puffer geschrieben).
- `PufferSpCtrl.SetTemperaturen` prüft jetzt `ProjektPuffer.IstTemperaturpaar` statt
  `> 0`. Ohne Untergrenze: 35/28 läuft unverändert durch.

### 5. Rücknahme einer Vorgabe (Fix 5, Befund B4-3)

**Befund.** Leerte der Anwender die Temperaturzellen, blieb am Puffer der alte Wert
stehen. Weil die Puffer-Zeile die **führende** Ablage ist, verdeckte er die Zuordnung
dauerhaft — die Rücknahme war aus der Oberfläche heraus nicht möglich.

**Umsetzung.** Neu: `PufferSpCtrl.TemperaturenLoeschen(idPuffer)` setzt beide Spalten auf
`NULL`. Der Schreibpfad ruft sie für die erste WP-Zeile (Regel aus Fix 3), wenn beide
Werte 0 bzw. leer sind. Bewusst getrennt von `SetTemperaturen`: dort ist „unbrauchbares
Paar" ein Grund, **nichts** zu tun; hier ist das Leeren die Absicht. Ein halb gefülltes
Paar lässt den Bestand unverändert stehen — weder Scheinvorgabe noch Datenverlust.

Danach greift wieder die dokumentierte Rückfallkette: Puffer → Zuordnung →
Engine-Vorgabe 10 K.

### 6. R1 und R6 prüfen nach derselben Regel (Fix 6)

**Befund.** R6 prüft die Systemvorgaben mit `ProjektPuffer.IstTemperaturpaar` und lässt
die Spalten sonst leer. **R1 tat das nicht** — es schrieb die Werte der Zuordnung
NULL-tolerant, aber ungeprüft an den Puffer. Im Bestand belegt: Projekt 1008, Zuordnungen
10058/10072 tragen `Vorlauf = 35`, `Ruecklauf = 45` — **vertauscht**. Der Puffer 1008007
bekam damit ein Paar mit `ΔT ≤ 0`: gepflegt aussehend, rechnerisch wertlos, und es
verdeckte die Zuordnung, die Stufe 2 der Rückfallkette ist.

**Umsetzung.** R1 schreibt das Paar nur bei `IstTemperaturpaar`, sonst `NULL` **plus
Protokollhinweis** mit den tatsächlichen Werten — gleiches Prinzip und gleiche Form wie
R6. `Verwendung` und die Schwellen wandern unverändert weiter.

**Nachweis am Bestand** (Erstlauf auf frischer Kopie der Arbeitskopie):

```
HINWEIS  Projekt 1008 R1: Zuordnung 10058 trägt kein brauchbares Temperaturpaar
         (Vorlauf 35, Rücklauf 45) - Vorlauf/Ruecklauf am Puffer 1008007 bleiben leer,
         die Engine fällt geordnet auf Zuordnung bzw. Vorgabe zurück.
```

| Puffer | Projekt | Verwendung | Vorlauf | Ruecklauf |
|---|---|---|---|---|
| 1008007 | 1008 | Heizung | *(leer)* | *(leer)* | 
| 1018009 | 1019 | Heizung | 65 | 45 |
| 1018023 | 1023 | Heizung | 65 | 45 |

**Ergebnisneutral.** Vorher lieferte Stufe 1 das Paar 35/45 → `ΔT ≤ 0` → Engine-Vorgabe
10 K. Jetzt ist Stufe 1 leer → Stufe 2 liefert dieselben 35/45 → derselbe Rückfall auf
10 K. Die Regression bestätigt es (8/8 PASS).

### 7. Schritt 4: Waisen vor den Beziehungen (Fix 7)

**Befund.** `PufferWaisenEntfernen` lief **nach** den vier `ADD CONSTRAINT`. Zeigt noch
eine Anlage auf eine verwaiste Puffer-Zeile, lehnt die restriktive Beziehung das `DELETE`
ab — `NonQuery` liefert −1, `PufferWaisenEntfernen` gibt `false` zurück, **Schritt 4
scheitert**, und die Migration bleibt auf Stand 3 stehen. Ausgerechnet an dem Bestand, den
der Schritt bereinigen soll.

Nachgestellt auf einer Kopie mit der alten Reihenfolge:

```
DELETE FEHLER -> Der Datensatz kann nicht gelöscht oder geändert werden,
                 da die Tabelle 'Tab_Energieanlagen' in Beziehung stehende
                 Datensätze enthält.
```

**Umsetzung.** Zweiteilig, beides zwingend:

1. `PufferWaisenEntfernen` läuft jetzt als Schritt **4c**, also vor den vier
   Beziehungen (4d) und vor der B0-6b-Beziehung (4e).
2. Es löst zuvor selbst die Verweise auf genau die Zeilen, die es löschen wird —
   `ID_PUFFER`, `WS_ID_Puffer`, `WS_ID_Puffer2`, `WQ_ID_Puffer`. Ohne das zeigten nach dem
   `DELETE` Anlagen ins Leere und das `ADD CONSTRAINT` kippte mit **Jet-Fehler 3379**
   („Existing data violates referential integrity rules") — an einer Datenlage, die der
   Schritt selbst erst erzeugt hätte.

**Nachweis.** Kopie präpariert (Anlage 10132 → `WS_ID_Puffer = 1015007`, Anlage 10133 →
`WQ_ID_Puffer = 1018010`; beides Waisen des gelöschten Projekts 1015), dann Migration:

```
- WS_ID_Puffer: 1 Verweise auf verwaiste Puffer-Zeilen geleert
- WQ_ID_Puffer: 1 Verweise auf verwaiste Puffer-Zeilen geleert
- Tab_Pufferspeicher: 4 verwaiste Projektkopien entfernt
- Beziehung … WS_ID_Puffer …: angelegt   (alle vier + B0-6b)
Ergebnis: ERFOLG, SchemaVersion 0 → 5, Abweichungen im Schema-Nachweis: 0
```

### 8. R4 repariert leeres `ID_PUFFER` (Fix 8)

**Befund.** Schritt 4 zieht ungültige `ID_PUFFER`-Werte auf `NULL`. Bei einer
Puffer-Anlagenzeile (`ID_Type = 12`) trifft das eine Zeile, die
`FormMain.SetPufferSpControl` mit einem **harten Cast** liest:

```csharp
ctrl.ReadAll("ID=" + (int)rs.Read("ID_PUFFER"));   // FormMain.cs:1116
```

`(int)` auf `DBNull` ist eine `InvalidCastException` — die Projektansicht bricht ab. Die
Migration erzeugte also erst die Datenlage, an der ein Bestandsleser scheitert.

**Umsetzung.** R4 legt nicht mehr nur fehlende Anlagenzeilen an, sondern repariert
**bestehende** mit leerem `ID_PUFFER`: gesetzt wird die kleinste ID des gleichnamigen
Projekt-Puffers — dieselbe Auswahl, mit der eine neue Zeile verknüpft würde. Neues
Zählwerk `SchemaMigration.DatenAnlagenzeilenRepariert`, im Bericht und im
Migrationslauf-Protokoll ausgewiesen.

**Nachweis.** Kopie präpariert (`ID_PUFFER` der Typ-12-Zeile 11206 in Projekt 1024 auf
NULL), dann Migration:

```
- Projekt 1024 R4: 1 vorhandene Anlagenzeile(n) für Puffer 'Vitocell 140-E 600 Ltr'
                   auf ID_PUFFER = 1036083 gesetzt (war leer)
- R4: 17 Anlagenzeilen (ID_Type = 12) nachgetragen, 1 vorhandene mit ID_PUFFER repariert
```

Der wiederhergestellte Wert ist exakt der ursprüngliche (1036083).

**`FormMain` bleibt unangefasst** (Parallelarbeit). Der fehlende defensive Read dort ist
damit **nicht behoben**, nur entschärft: die Migration erzeugt keine solche Zeile mehr,
aber eine von Hand in Access geleerte Zelle reißt die Ansicht weiterhin ab. Siehe
[Offene Punkte](#offene-punkte).

### 9. Projektlöschung löst die Puffer-Referenzen zentral (Fix 9)

**Befund.** Die B0-6b-Kaskade (`Tab_Projekt.ID → Tab_Pufferspeicher.ID_Projekt`,
`ON DELETE CASCADE`) wird **blockiert**, solange noch eine Anlage auf einen Projekt-Puffer
zeigt: die vier Anlagen-Referenzen sind bewusst restriktiv. Nachgestellt:

```
DELETE FEHLER -> Weitergabe der Operation nicht möglich. Da verwandte (verknüpfte)
                 Datensätze in Tabelle 'Tab_Energieanlagen' vorhanden sind, würden die
                 Regeln der referenziellen Integrität verletzt.
```

Die beiden Löschpfade entfernen die Energieanlagen zwar vorher
(`MenueCtrl.ProjektDelete` und `VariantenCtrl.LoescheVariante`, beide über
`WErzeugerCtrl.Delete`) — aber damit hängt die Kaskade an der **Aufrufreihenfolge**. Ein
dritter Aufrufer, eine umgestellte Reihenfolge oder ein Teilfehler beim Anlagenlöschen,
und das Projekt lässt sich nicht mehr entfernen.

**Umsetzung.** `PufferSpCtrl.ReferenzenLoesenFuerProjekt(idProjekt)` (neu, öffentlich) und
ein Aufruf in **`ProjektCtrl.Delete(string)`** — der einen zentralen Stelle, durch die
beide Wege laufen. Die Aufrufer bleiben unverändert.

**Nachweis** (Werkzeug `Probe`, siehe Abschnitt 13): Wegwerf-Projekt mit Puffer und einer
Anlage, die ihn über `WS_ID_Puffer` referenziert, dann `ProjektCtrl.Delete` **ohne**
vorheriges Löschen der Anlagen → Projekt weg, Puffer-Projektkopie über die Kaskade weg,
Referenz gelöst. Ohne den Fix scheitert derselbe Ablauf mit der Meldung oben.

### 10. `numericUpDown_Volumen` klemmt beim Laden (Fix 10)

`NumericUpDown.Value` wirft eine `ArgumentOutOfRangeException`, sobald der Wert außerhalb
von `Minimum`/`Maximum` liegt. `Gesamtvolumen` kommt aus der Datenbank und kann jede Zahl
tragen (Altbestand, Import, Tippfehler in Access). Der gelesene Wert wird deshalb mit
`Math.Max`/`Math.Min` in den Wertebereich gezogen — ein unmögliches Volumen darf das
Formular nicht am Öffnen hindern.

### 11. Tote Löschpfade entschärft (Fix 11)

`PufferSpCtrl.Delete(string)` löschte ohne vorheriges `ReferenzenLoesen` und wäre an der
restriktiven Beziehung gescheitert. Der Aufrufkreis ist heute leer
(`Form_PufferSp_Admin` und `Form_PufferSp` arbeiten auf den `_STAMM`-Tabellen) — die
Methode bleibt trotzdem stehen und wird mitgepflegt, damit ein späterer Aufruf nicht in
die Beziehung läuft. `DeleteFromProjekt` hatte den Vorlauf bereits. Beides ist im
Methodenkommentar festgehalten.

### 12. Sechs Dokumentationslücken (Befund B7)

| # | Lücke | Behandelt |
|---|---|---|
| 1 | **Wirkung des Schreibpfads** war nur als „nach erfolgreichem `Insert()`" beschrieben — welche Zeile das ist und was das für die anderen bedeutet, stand nirgends | Abschnitt 3 dieser Nacharbeit; zusätzlich der Kommentar im Code |
| 2 | **R1 und R6 prüften verschieden**, ohne dass die Abweichung benannt war | Abschnitt 6; beide prüfen jetzt über `IstTemperaturpaar` |
| 3 | **Nebenwirkung von R4 auf `ProjektWaisenEntfernen`** war nicht erwähnt | siehe unten |
| 4 | **„Temperaturen vorhanden" vs. „Puffer vorhanden"** an der führenden Ablage | siehe unten |
| 5 | **`WertLesen`-Dialogpfad in Stufe 1** — die Aussage „ohne Dialog" galt nur für Stufe 2 | Präzisierung in Etappe 4, Abschnitt 1 |
| 6 | **zwei Begriffe für dieselbe Sache** („Systemvorgabe" / „Systemtemperaturen") | siehe unten |

**Zu 3 — R4 und `ProjektWaisenEntfernen`.** `PufferSpCtrl.ProjektWaisenEntfernen` löscht
Projektkopien, zu denen **keine** Puffer-Anlagenzeile gleichen Bezeichners mehr existiert
(B0-6a). Weil R4 für jeden Bezeichner eine Anlagenzeile sicherstellt, ist nach der
Migration **keine** Projektkopie mehr in diesem Sinne verwaist — das Aufräumen läuft
danach ins Leere, bis der Anwender selbst eine Puffer-Anlage löscht. Das ist gewollt (die
Migration darf keine Anwenderdaten entfernen), aber man muss es wissen, wenn man sich
fragt, warum die Zeilenzahl von `Tab_Pufferspeicher` nach der Migration nicht mehr
schrumpft. Im Bestand: 118 Projekt-Puffer, 22 Anlagenzeilen — die Differenz sind
gleichnamige Kopien, die sich eine Anlagenzeile teilen.

**Zu 4 — was `false` von `TemperaturenLesen` bedeutet.** `PufferSpCtrl.TemperaturenLesen`
liefert nur `true`, wenn die Zeile existiert **und** beide Spalten gesetzt und > 0 sind.
`false` heißt deshalb **„am Puffer steht nichts Brauchbares — nimm den Rückfallweg"** und
gerade **nicht** „diesen Puffer gibt es nicht". Beide Fälle sind für den Leser identisch
und sollen es sein: Stufe 1 ist eine Aussage über die **Temperaturen**, nicht über die
**Existenz** des Speichers. Wer die Existenz braucht (etwa zum Anlegen), fragt separat —
so tut es `PendelspeicherId`.

**Zu 6 — ein Begriff, eine Bedeutung.** Das Protokoll benutzte „Systemvorgabe(n)",
„Systemtemperaturen" und „Systemvorbelegung" nebeneinander. Gemeint ist durchgehend
dasselbe:

> **Systemvorgabe eines Projekts** = das Paar aus `PufferSpCtrl.SystemVorlauf`
> (**kleinster** `Vorlauf`) und `PufferSpCtrl.SystemRuecklauf` (**größter**
> `[Rücklauf]`) über die Wärmeerzeuger-Anlagen `ID_Type IN (1, 2, 10, 11)` mit
> `> 0`-Filter.

„Systemtemperaturen" und „Systemvorbelegung" bezeichnen dieselben zwei Zahlen, sobald sie
an einen neuen Puffer geschrieben wurden. Davon zu unterscheiden ist die
**Betriebsvorgabe eines Speichers** — das Paar, das tatsächlich in
`Tab_Pufferspeicher.Vorlauf/.Ruecklauf` steht. Eine Systemvorgabe wird nur dann zur
Betriebsvorgabe, wenn `IstTemperaturpaar` zutrifft.

### 13. Verifikation der Nacharbeit

Basis: eigene Kopien in `C:\Waermeplan\Review_Nacharbeit` — **außerhalb des Repos**,
Modus `projekt` statt `lauf` (der legt `Referenzlaeufe\Arbeitskopie` neu an und wird
parallel von einer anderen Sitzung benutzt). Die produktive
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde zu keinem Zeitpunkt geöffnet oder
beschrieben.

| # | Lauf | Ergebnis |
|---|---|---|
| 0 | Build `WP-Plan.sln` Debug/x86 über MSBuild 2022 | **0 Fehler** (nur die bekannten Bestandswarnungen). `Referenzlauf.csproj` ebenfalls 0 Fehler. |
| 1 | **Erstlauf** auf frischer Kopie der Arbeitskopie | `SchemaVersion` 0 → **5**, alle fünf Schritte OK, **Schema-Nachweis 0 Abweichungen** |
| 2 | **No-op** (`--nokopie`, dieselbe Kopie) | Alle fünf Schritte „bereits erledigt", `SchemaVersion` bleibt **5**, `MigrationOk = true`, erneut **0 Abweichungen** |
| 3 | **Leere `Tab_Applikation`** (Fix 1) | Bootstrap OK, voller Durchlauf 0 → **5**, 0 Abweichungen. Gegenprobe: beide alten INSERT-Varianten scheitern (Meldungen oben). |
| 4 | **Schreibschutz** (`--schreibschutz`) | Bericht beginnt mit dem DB-Pfad, dann die erweiterte Kopfmeldung + *„Cannot modify the design of table 'Tab_Applikation'. It is in a read-only database."* — **Marker nicht angehoben (0), `MigrationOk = false`, kein Absturz** |
| 5 | **Referenzierte Waise** (Fix 7) | ERFOLG, 2 Verweise geleert, 4 Waisen entfernt, alle Beziehungen angelegt, 0 Abweichungen. Gegenprobe mit der alten Reihenfolge: `DELETE` blockiert. |
| 6 | **Leeres `ID_PUFFER`** (Fix 8) | 1 Anlagenzeile repariert (auf den ursprünglichen Wert 1036083), 0 Abweichungen |
| 7 | **Regression**: alle **8** Referenzprojekte auf der migrierten Kopie, `vergleich` gegen `Referenzlaeufe/2026-08-14_B0` | **GESAMT: PASS** — 8/8, **2 033 047 Werte** innerhalb der Toleranz |
| 8 | **Schreibpfad + Rücknahme** (Werkzeug `Probe`) | **32 von 32** Prüfungen wie erwartet — Einzelheiten unten |
| 9 | **Simulationswirkung der Rücknahme**, Projekt 1008 | Puffer auf 35/28 → Vergleich gegen B0 **FAIL, 30 480 Abweichungen** (die gewollte Wirkung). Danach auf NULL → **PASS, 227 832 Werte** — der Rückfall greift und ist wertgleich zum Bestand. |

**Zählwerk-Änderungen gegenüber Etappe 4** (gleiche Arbeitskopie, Erstlauf):

| Größe | Etappe 4 | Nacharbeit | Grund |
|---|---|---|---|
| Puffer mit Verwendung (R1) | 3 | 3 | unverändert |
| Anlagen auf Puffer | 6 | 6 | unverändert |
| Anlagen auf Heizkreis (R5) | 37 | 37 | unverändert |
| Quell-Puffer (R3) | 1 | 1 | unverändert |
| Anlagenzeilen nachgetragen (R4) | 17 | 17 | unverändert |
| **Anlagenzeilen mit `ID_PUFFER` repariert (R4)** | — | **0** | neues Zählwerk; im Echtbestand gibt es keine solche Zeile (der eine ungültige Wert wird schon in Schritt 4 gemappt). Nachgewiesen an präpariertem Bestand: 1. |
| Pendelspeicher (R6) | 0 | 0 | unverändert |
| **Hinweise** | 10 | **11** | **+1**: der neue R1-Hinweis zum vertauschten Paar 35/45 in Projekt 1008 |
| verwaiste Projektkopien entfernt (Schritt 4) | 4 | 4 | unverändert; zusätzlich werden jetzt die Verweise darauf gelöst (im Echtbestand 0) |

**Zahlen zu Lauf 8** (Werkzeug `Probe`, Wegwerf-Konsolenprojekt außerhalb des Repos, ruft
die `internal`-Klassen per Reflection auf einer eigenen Kopie auf — dasselbe Muster wie in
den Etappen 3 und 4):

| Gruppe | Prüfungen |
|---|---|
| `ProjektPuffer.IstTemperaturpaar` | 35/28 ✓, 70/50 ✓ angenommen; 35/45, 35/35, 35/0, –/28 abgelehnt |
| `Form_Simulation_Config.TemperaturPaarPruefen` | `35/28`, `''/''`, `0/0`, `35/''`, `''/28` durchgelassen; `35/45`, `35/35`, `111/40` mit der jeweils passenden Meldung abgelehnt |
| `PufferSpCtrl` | `SetTemperaturen(35,28)` → true und gelesen 35/28; `(35,45)` → **false, Bestand unverändert**; `(35,0)` → false; `TemperaturenLoeschen` → NULL; ID 0 → false |
| **Schreibpfad `btn_Speichern_Click`, Projekt 1008** | Reihenfolge BHKW 70/50, Kessel 80/60, **WP 35/28**, WP 55/40 → am Puffer steht **35/28**; BHKW- und Kessel-Zeile **wirkungslos**; die zweite WP-Zeile **wirkungslos** (ihr Puffer bleibt leer) |
| **Rücknahme** | erste WP-Zeile auf `0/0` → Puffer wieder **leer**; danach halbes Paar `35/0` → Bestand **35/28 bleibt stehen** |
| **Projektlöschung** | `ProjektCtrl.Delete` ohne vorheriges Anlagenlöschen → Projekt weg, Puffer-Projektkopie über die Kaskade weg, Referenz gelöst |

Aufgerufen wurde dabei die **echte** `Form_Simulation_Config.btn_Speichern_Click` (Instanz
über Reflection, `_zuordnungen` gesetzt), nicht eine nachgebaute Logik.

### 14. Geänderte Dateien (Nacharbeit)

| Datei | Änderung |
|---|---|
| `Allgemein/Update/SchemaMigration.cs` | `StatuszeileAnlegen` (Fix 1) + erweiterte Kopfmeldung; R1 über `IstTemperaturpaar` + Hinweis (Fix 6); Schritt 4 umsortiert und `PufferWaisenEntfernen` löst Referenzen (Fix 7); R4 repariert `ID_PUFFER` + Zählwerk `DatenAnlagenzeilenRepariert` (Fix 8); Helfer `ZahlOderNull`; `ERZEUGER_WAERMEPUMPE` als Alias auf `ProjektPuffer` |
| `Allgemein/Update/ProjektPuffer.cs` | neue Konstante `ERZEUGER_WAERMEPUMPE` |
| `Controller/PufferSpCtrl.cs` | `SetTemperaturen` prüft `IstTemperaturpaar` (Fix 4); `TemperaturenLoeschen` neu (Fix 5); `ReferenzenLoesenFuerProjekt` neu (Fix 9); `Delete(string)` löst Referenzen (Fix 11) |
| `Controller/ProjektCtrl.cs` | `Delete(string)` löst die Puffer-Referenzen des Projekts (Fix 9), Helfer `PufferReferenzenLoesen` |
| `Views/Simulation/Form_Simulation_Config.cs` | Schreibpfad nur aus der ersten WP-Zeile + Rücknahme (Fix 3, 5); Zelleditor-Validierung `TemperaturPaarPruefen`/`IstLeerwert` (Fix 4); `using System.Globalization` |
| `Views/Simulation/Form_Simulation_Detail.cs` | `SimulationBlockiert()` + Aufrufe in `Load` und `btn_Simulation_Click` (Fix 2); Klemmung von `numericUpDown_Volumen.Value` (Fix 10) |
| `../Referenzlauf/Migrationslauf.cs` | neues Zählwerk im Lauf-Protokoll |
| `Allgemein/Simulation/Paket1_SchemaMigration_Protokoll.md` | dieser Abschnitt + die Korrekturen in den Etappen 1–4 |

**Nicht angefasst** (Parallelarbeit, ausdrücklich): `Views/Hauptformular/FormMain.cs`,
`Form_Heizkessel*`, `SimulationSPK`, `RecordSet`, `Form_BHKWEing`, `WizardParent`,
`WErzeugerModel`. `FormMain` und `MenueCtrl`/`VariantenCtrl` wurden nur **gelesen**.

Kodierungen aller berührten Dateien geprüft: BOM-Zustand unverändert, Zeilenenden je Datei
unverändert (Tabelle in Etappe 1, Abschnitt 9), keine Ersatzzeichen (`U+FFFD`). Kein
Designer- und kein `.resx`-Eintrag von Hand geändert. **Nichts committet.**

---

## Offene Punkte

### ~~Vorlauf/Rücklauf der migrierten Puffer~~ → in Etappe 4 entschärft

**Ursprünglicher Punkt (Etappe 2).** Konzept 5.5 nennt für R6 nur Bezeichner,
Verwendung und Volumen; die Migration setzte deshalb **kein** `Vorlauf`/`Ruecklauf` am
`BHKW-Pendelspeicher`. Dieselbe Lücke hatten alle Projekt-Puffer, die nicht über R1
versorgt wurden: nach der Migration trugen **115 von 118** Projekt-Puffern kein
Temperaturpaar. Relevant wird das, sobald die Engine die Kapazität aus
`SimulationPufferspeicher.Init` zieht (`Q_max = Volumen · 1,16 · (Vorlauf − Rücklauf) /
1000`, ohne Temperaturen **ΔT = 10 K**), während die heutige Pendelspeicher-Formel exakt
**ΔT = 20 K** entspricht.

**Durch Etappe 4 entschärft — in zwei Hälften:**

- **Neue Puffer bekommen Systemtemperaturen.** R6 und
  `SetPendelspeicherVolumenLiter` belegen `Vorlauf`/`Ruecklauf` beim Anlegen aus den
  Systemvorgaben des Projekts vor (kleinster Vorlauf / größter Rücklauf über die
  Wärmeerzeuger). Die frühere Empfehlung „Vorbelegung mit 20 K Spreizung, z. B.
  70/50 °C" ist damit **verworfen**: eine feste Zahl wäre bei einem
  Niedertemperatursystem falsch, und die Nutzeranforderung vom 14.08.2026 verlangt
  ausdrücklich, dass auch 35/28 durchgeht.
- **Bestand ohne Temperaturen fällt weiter zurück** — geordnet und nachgewiesen, statt
  stillschweigend: Puffer → Zuordnung → Engine-Vorgabe 10 K (Etappe 4, Abschnitt 1).
  Ein Rückfall ist damit kein Zufall mehr, sondern die dritte Stufe einer dokumentierten
  Kette.

**Was offen bleibt, gehört nach Paket 6:** `SimulationControl.Simulation_BHKW_Ctrl`
rechnet weiterhin `Liter · 20 / 860`, liest die Temperaturen am Speicher also gar nicht.
Erst wenn die Kapazität dort aus `SimulationPufferspeicher` kommt, **bestimmt die
abgelegte Spreizung die Kapazität** — ein Pendelspeicher mit 55/45 rechnet dann mit
10 K statt 20 K, also mit der halben Kapazität. Das ist die beabsichtigte Wirkung und
gehört in die Abnahme von Paket 6.

### ~~Ablösung von `Tab_Einstellungen.Pendelspeicher`~~ → in Etappe 3 erledigt

Der Parameter ist durch den Projekt-Puffer `BHKW-Pendelspeicher` (in **Litern**)
abgelöst; Engine und Oberfläche lesen ihn seit **Etappe 3** nicht mehr. Die Spalte
bleibt physisch bestehen und wird von `KonfigurationCtrl` nur noch mitgeschleppt.

### Nicht abgedeckte Anlagentypen (`REF_*`)

R1 erfasst `ID_Type = 1`, R5 die Typen 1, 2, 10, 11 — so steht es in Konzept 5.5. Die
Referenz-Varianten `REF_KESSEL_TYP = 5`, `REF_SP_TYP = 6`, `REF_WP_TYP = 7`,
`REF_SOLAR_TYP = 8`, `REF_PV_TYP = 9` aus `WizardItemClass` bleiben damit ohne `WS_Ziel`.
In der Arbeitskopie existiert keine einzige Zeile dieser Typen (`ID_Type` kommt nur mit
1, 2, 3, 4, 10, 11, 12 vor), die Lücke ist heute also gegenstandslos. Sollte Paket 2 die
Referenzvarianten als Erzeuger behandeln, ist die Typliste dort nachzuziehen.

### Dedup-Aufhebung aus Konzept 5.2 → Paket 2

`PufferSpCtrl.GetProjektId(Bezeichner, ID_Projekt)` erlaubt heute nur **eine** Zeile je
(Bezeichner, Projekt); Konzept 5.2 will diese Prüfung aufheben, damit mehrere baugleiche
Puffer je Projekt möglich werden.

**In Etappe 1 ausdrücklich nicht umgesetzt.** `Z_ProjektPufferSpCtrl.Insert` ruft
`CopyFromStamm` implizit auf, und `Form_Simulation_Config.btn_Speichern_Click` schreibt
die Zuordnungen bei jedem Speichern neu. Ohne Dedup entstünde damit **bei jedem
Speichern ein weiterer Duplikat-Puffer** — die 122 Zeilen der Arbeitskopie mit ihren
vielen gleichnamigen Kopien zeigen, wohin das führt. Die Aufhebung gehört zusammen mit
der neuen Puffer-Verwaltung in Paket 2, die `CopyFromStamm` nur noch explizit aufruft.

### `ErgebnisCtrl.StellePufferTabelleSicher()` → Paket 7

Konzept 6.6 verlangt neben der Migration eine **Rückfallebene** in `ErgebnisCtrl`:
`StellePufferTabelleSicher()` soll `Tab_ErgebnisPufferspeicher` samt Index und
FK-Constraint per `CREATE TABLE` nachziehen, falls die Migration auf einer Datenbank noch
nicht gelaufen ist — nach dem `Ddl()`-Vorbild aus `WirtschaftlichkeitCtrl`, ergänzt um ein
defensives explizites `DELETE` in `Save`, falls die Constraint fehlt.

**Paket 1 legt die Tabelle an (Schritt 3), die Rückfallebene ist aber nicht Teil davon.**
Sie hat auch keinen Zweck, solange niemand in die Tabelle schreibt: `ErgebnisCtrl` kennt
sie noch gar nicht, `ErgebnisModel` führt keine
`List<ErgebnisPufferspeicherModel>`, und Puffer-Ergebnisse entstehen erst, wenn die Engine
sie liefert. Der Punkt gehört deshalb zusammen mit `TAB_PUFFER`, dem Save/Load-Block und
`ErgebnisModel` in **Paket 7 (Ergebnisspeicherung)** — dort ist er in einem Zug zu
erledigen und dort auch prüfbar.

### Defensiver Read in `FormMain.SetPufferSpControl`

`FormMain.cs:1116` liest `(int)rs.Read("ID_PUFFER")` mit hartem Cast; bei `NULL` ist das
eine `InvalidCastException` und die Projektansicht bricht ab. Fix 8 der Nacharbeit
entschärft das von der Datenseite (R4 füllt leere Werte), **behebt es aber nicht**: eine
von Hand in Access geleerte Zelle oder ein künftiger Löschpfad reißt die Ansicht
weiterhin ab.

`FormMain.cs` ist Parallelarbeit und wurde deshalb **nicht** angefasst. **Adressiert an
die FormMain-Sitzung:** den Read defensiv machen, etwa

```csharp
object roh = rs.Read("ID_PUFFER");
int idPuffer = (roh == null || roh == DBNull.Value) ? 0 : Convert.ToInt32(roh);
if (idPuffer <= 0) continue;          // Zeile ohne Speicher überspringen
ctrl.ReadAll("ID=" + idPuffer);
```

Die bestehende `if (ctrl.rows > 0)`-Prüfung darunter fängt den Rest bereits ab.

### `WaermequelleClass.WertLesen` zeigt Dialoge

`WertLesen` geht über `DataRepository.ExecuteScalar` und kann im Fehlerfall eine
MessageBox mitten im Engine-Lauf zeigen (Konzept 13.4 verlangt Dialogfreiheit). Das ist
Altbestand mit breitem Aufrufkreis; Etappe 4 hat nur die **neu** hinzugekommene Abfrage
über `SkalarStill` geführt. Die Umstellung des gesamten `WertLesen` ist eine eigene
Änderung und gehört in das Paket, das den Engine-Pfad ohnehin anfasst (Paket 2).

### Kleineres

- `ProjektDuplizierenCtrl` wurde mit den neuen Beziehungen **nicht** durchgetestet. Die
  topologische Sortierung arbeitet über `_echteFks` und bekommt jetzt die zusätzliche
  Kante `Tab_Energieanlagen → Tab_Pufferspeicher → Tab_Projekt` (zyklenfrei), sollte
  also greifen. Ein Variantentest gehört in die Abnahme von Paket 2.
- `Referenzlaeufe/2026-08-14_Paket3_Review/` wurde **nicht** gelöscht, obwohl der neuere
  Nachweis `2026-08-14_Paket1_Migration/` ihn ablöst: der Ordner ist als Beleg in
  `Paket3_Erdreichmodell_Protokoll.md` verlinkt und gehört zu paralleler Arbeit.
  `2026-08-14_B0` bleibt als Referenz ohnehin bestehen.
- ADR-Aufgabe 7 (Konzept 5.6/6.6 durch Verweis auf den ADR ersetzen, O6 als erledigt
  markieren, Spaltenzahl korrigieren) ist bereits im Konzept nachgezogen und war nicht
  Teil dieser Etappe.
- **Beobachtung während Etappe 2:** die Referenzlauf-Suite wurde parallel von einer
  anderen Arbeitssitzung benutzt (`Referenzlaeufe\Arbeitskopie\Kenndaten.accdb` und
  `2026-08-14_B0\lauf_protokoll.md` um 12:00 neu geschrieben, Projekte 1008 und 1011 in
  `2026-08-14_B0` neu gerechnet). Die CSV-Dateien sind dabei **byte-identisch** geblieben
  (`git status` meldet in `2026-08-14_B0` nur die Protokolldatei als geändert), die
  Vergleichsbasis ist also intakt. `lauf` und `migration` teilen sich denselben
  Arbeitskopie-Ordner — zwei gleichzeitige Läufe würden sich gegenseitig die Datenbank
  unter den Füßen wegziehen. Deshalb lief die Regression dieser Etappe über den Modus
  `projekt` auf einer eigenen Kopie außerhalb des Repos.
