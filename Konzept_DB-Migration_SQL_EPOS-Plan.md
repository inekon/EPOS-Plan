# Konzept: DB-Migration Access → SQL Server für EPOS-Plan

**Rev. 1 — Entwurf zur Abnahme** · 19.08.2026 · Grundlage: Live-Messung `C:\ProgramData\EPOS_Plan\Kenndaten.accdb` (90,4 MB, Stand 19.08.2026 19:38) und Code-Inventur `WindowsFormsApplication1` (Hauptbaum)

---

## 1. Auftrag und Ziele

Migration der Datenhaltung von MS Access (`Kenndaten.accdb`, ACE-OLEDB) nach SQL, mit drei vom Auftraggeber gesetzten Zielen:

1. **Visuelles Arbeiten bleibt möglich** — das gewohnte Access-Design (Tabellen ansehen, Daten pflegen, Abfragen zusammenklicken, Beziehungen sehen) soll auch mit SQL-Backend nutzbar sein.
2. **Einfaches, übersichtliches, strukturiertes Datendesign** — die Struktur soll nach der Migration klarer sein als vorher, nicht komplizierter.
3. **Klare und zuverlässige Überführung** — die Migration selbst muss nachvollziehbar, wiederholbar und geprüft ablaufen (auch bei Kundenbeständen).

**Was die Migration zusätzlich löst** (Nebengewinne, keine Ziele):

- Das Mehrbenutzer-/ACL-Problem (`Kenndaten.laccdb` unter `C:\ProgramData`, siehe `BETRIEB_Mehrbenutzer_Datenbank.md`) **entfällt ersatzlos** — SQL Server regelt Gleichzeitigkeit selbst.
- Kein „Komprimieren und reparieren", kein 2-GB-Dateilimit, keine ACE-Bitness-Falle (x86/x64) mehr.
- Fremdschlüssel und Transaktionen werden serverseitig **erzwungen**, nicht nur deklariert.
- Echte Backups im laufenden Betrieb; Netzwerk-/Mehrplatzfähigkeit ohne Dateifreigabe.

---

## 2. Ausgangslage (gemessen am 19.08.2026)

### 2.1 Datenbank

| Kennwert | Befund |
|---|---|
| Tabellen | **110** (Namensschema `Tab_*`, `Tab_*_STAMM`, `Z_*`, dazu `energy_*`, `pricing_model`, `Berichtskonfiguration`) |
| Spalten | **2.646** |
| Datentypen | **nur 5**: Double (1.669), Integer (340), Text/WChar (228), Boolean (74), Datum (16) |
| Access-Spezialtypen | **keine** — keine Anlagen-, Mehrwert-, OLE-, Hyperlink- oder Currency-Felder |
| Beziehungen | **83 FK-Spaltenzuordnungen** vorhanden |
| Tabellen ohne PK | **3**: `Tab_BHKW`, `Tab_DBTagVDaten_STAMM`, `Tab_Stromverbrauchertyp_STAMM` |
| Gespeicherte Abfragen | 29 SELECT-Abfragen (Views) + 7 Aktions-/Parameterabfragen, darunter Artefakte (`Abfrage1`, `Abfrage2`, `Tab_BHKW_Einfügen_Test`, `Tab_StromganglinieDaten Abfrage`) |
| Vom Code genutzte Abfragen | **nur 3**: `Abfrage_Energietraeger_Effektiv`, `Abfrage_Kostenfaktoren`, `Abfrage_Projektgebaeude` |
| Spaltennamen mit Umlaut/ß | 20 (z. B. `Wirkungsgrad_Öl`, `k_Wert_Außenwand`, `ID_Energieträger`); Tabellennamen sauber |
| Größte Tabellen | `Tab_StromganglinieDaten` 534.361 · `Tab_Solar_STAMM` 280.320 · `Tab_Solar` 148.920 · `Tab_WaermebedarfDaten(_STAMM)` 78.840/35.040 · `Tab_StromganglinieDaten_STAMM` 78.840 · `Tab_Kenndaten` 27.112 — gesamt ≈ **1,25 Mio. Zeilen** |
| Text als Schlüssel | `Tab_Projekt` PK = `Projektname`; viele Komposit-PKs mit `Bezeichner` — die bekannte Textverknüpfungs-Altlast (CLAUDE.md) |

**Bewertung:** Das ist für eine Access→SQL-Migration ein ausgesprochen gutmütiges Schema. Die fünf Typen mappen verlustfrei; die klassischen SSMA-Stolpersteine (Anlagen, Mehrwertfelder, OLE) kommen nicht vor.

### 2.2 Anwendung

| Kennwert | Befund |
|---|---|
| Zentraler Zugriff | `DataRepository.GetConnectionString()` — **ein einziger Umschaltpunkt**; auch `RecordSet` (inzwischen OleDb, nicht mehr ODBC — CLAUDE.md dort veraltet) zieht ihn |
| DB-Pfad | `Properties.Settings` (`DBPath`/`DBName`), Default `%ProgramData%\EPOS_PLAN` |
| Parameterstil | `?` positional (OleDb), **276 SQL-Zeilen mit Parametern** |
| SQL-Volumen | ≈ 672 SELECT- / 153 INSERT- / 165 UPDATE- / 123 DELETE-Zeilen |
| Access-Dialekt im SQL | **praktisch keiner**: `IIf`/`Nz`/`Format`/`Date()`/`Now()`/`TRANSFORM`/`DISTINCTROW` = 0 Treffer; `#…#`-Datumsliterale = 0 (alle `#`-Treffer sind Excel-Zahlenformate); `LIKE` = 4 Stellen; Boolean-Literale `= True/False` = **5 Stellen** (`KostenPositionCtrl.cs`) |
| Identity-Abholung | `SELECT @@IDENTITY` in 15 Dateien; daneben `GetMaxID`-Muster (App vergibt IDs teils selbst per `MAX(ID)`) |
| Transaktionen | `BeginTransaction` in 29 Dateien (OleDbTransaction) |
| App-eigene Schemapflege | `Allgemein/Update/SchemaMigration.cs` (**4.793 Zeilen**, idempotente Schritte mit Markern) + `SchemaKatalog.cs` (1.414 Zeilen); DDL in Access-Dialekt (`LONG`, `TEXT(n)`, `YESNO`): 21 × `CREATE TABLE`, 40 × `ALTER TABLE` |

**Bewertung:** Der Dialekt-Sweep ist klein — die eigentliche Arbeit steckt in der **Schemapflege-Schicht** (DDL-Dialekt) und in der **sauberen Erstüberführung**.

### 2.3 Vorhandene Werkzeuge und laufende Stränge

- `C:\Users\DirkEngelmann\DB_Migration\AccessMigration.sln` — vorhandener **Access→Access-Migrator** (C#/.NET 8): liest Schema/Beziehungen generisch, ordnet Eltern vor Kindern, Backup + Auto-Rollback, Dry-Run, Validierungsbericht. Direkt wiederverwendbare Basis für den Datenlader (Abschnitt 6).
- **K-Strang** (`Konzept_Kosten_Energietraeger_EPOS-Plan.md`, Rev. 2): K1 fertig; K2–K6 ändern das Schema weiter (Löschliste Alttabellen, faktor_name-Migration, neue Kostenprofile). → Wechselwirkung siehe Abschnitt 12/D7.
- Die App migriert Kundenschemata heute selbst beim Start (`SchemaMigration.cs`) — dieser Mechanismus bleibt das Vorbild für künftige SQL-Schemaupdates.

---

## 3. Zielbild

```
                    ┌─────────────────────────────────────────┐
                    │  SQL Server (eine Codebasis, 2 Profile) │
                    │  · Einzelplatz: LocalDB 2022            │
                    │  · Mehrplatz:   SQL Server Express      │
                    │  DB: EPOS_Kenndaten                     │
                    └───────┬───────────────┬─────────────────┘
             TCP/Shared Mem │               │ ODBC Driver 18
                            │               │
        ┌───────────────────┴──┐   ┌────────┴─────────────────────┐
        │ EPOS-Plan (App)      │   │ Visuelle Werkzeuge           │
        │ DataRepository       │   │ · Kenndaten_Design.accdb     │
        │ (OleDb → MSOLEDBSQL, │   │   (Access-Frontend, ver-     │
        │  ?-Parameter bleiben)│   │    knüpfte Tabellen: Daten-  │
        └──────────────────────┘   │    blatt, QBE-Designer)      │
                                   │ · SSMS: Tabellendesigner,    │
   Schema-Wahrheit im Repo:        │   Datenbankdiagramme         │
   sql/schema/*.sql (T-SQL,        └──────────────────────────────┘
   versioniert, Review-fähig)
```

Vier Festlegungen bilden das Zielbild:

1. **Backend = SQL Server** in zwei Profilen mit identischer Engine und identischem Code: **LocalDB** für den Einzelplatz (läuft als Benutzerprozess, kein Dienst, ~53-MB-MSI im Installer), **Express** für Mehrplatz/Server (kostenlos bis 10 GB pro DB). Begründung und Alternativen: Abschnitt 4.
2. **Die App spricht direkt SQL Server** — Umschaltung im zentralen `GetConnectionString()`, OleDb-Stack bleibt (Provider `MSOLEDBSQL19` statt `Microsoft.ACE.OLEDB.12.0`), dadurch bleiben alle 276 `?`-Parameterstellen unverändert gültig.
3. **Visuelles Arbeiten** über zwei sich ergänzende Oberflächen (Abschnitt 3.1): ein Access-Design-Frontend mit verknüpften Tabellen für alles Datennahe, SSMS für alles Strukturelle.
4. **Die Schema-Wahrheit wandert ins Repo**: ein kuratierter Satz T-SQL-Skripte (`sql/schema/…`) ist ab Cutover die einzige Quelle der Struktur — versionierbar, diffbar, reviewbar. Das ist der Kern von Ziel 2 („strukturiertes Datendesign"): Struktur wird zum Codeartefakt statt Binärblob.

### 3.1 Visuelles Arbeiten nach der Migration — was geht wo

| Tätigkeit | Werkzeug | Anmerkung |
|---|---|---|
| Daten ansehen/bearbeiten (Datenblatt) | **Access-Frontend** (`Kenndaten_Design.accdb`) | wie gewohnt; bearbeitbar sind alle Tabellen mit PK (109 von 110 nach S2) |
| Ad-hoc-Abfragen zusammenklicken | **Access-Frontend**, QBE-Designer | voll funktionsfähig über verknüpfte Tabellen; für schwere Auswertungen Pass-Through-Abfragen (T-SQL direkt) |
| Beziehungen **ansehen** | Access-Frontend (Beziehungsfenster) oder SSMS-Diagramm | Access zeigt Joins, verwaltet aber keine SQL-FKs |
| Tabellen **entwerfen/ändern**, Beziehungen **anlegen** | **SSMS** (Tabellendesigner, Datenbankdiagramme) | die Access-Entwurfsansicht ist auf verknüpften Tabellen prinzipbedingt schreibgeschützt |
| Struktur ändern, dauerhaft | **T-SQL-Skript im Repo** + `SchemaMigration` | SSMS-Designer für den Entwurf, das Ergebnis wird als Skript eingecheckt |

Das Access-Frontend ist eine **reine Hülle** (keine lokalen Tabellen, nur ODBC-Links per DSN-less Connect), liegt im Repo bzw. beim Entwickler und wird **nicht an Kunden ausgeliefert** (D5 kann das später ändern). Damit ist Ziel 1 erfüllt, ohne dass Access Teil der Laufzeitarchitektur bleibt.

**Zwei bekannte Access-Frontend-Fallen, die das Schema von vornherein vermeidet (S2):**
- `bit`-Spalten müssen **NOT NULL DEFAULT 0** sein, sonst provoziert Access „Schreibkonflikt"-Meldungen (Access-Ja/Nein kennt kein NULL — die 74 Boolean-Spalten werden entsprechend angelegt).
- Tabellen ohne PK sind im Frontend nur lesbar → die 3 PK-losen Tabellen bekommen einen PK (D4).

---

## 4. Plattformwahl (Entscheidung D1)

| Kriterium | **SQL Server (LocalDB/Express)** — Empfehlung | SQLite | PostgreSQL |
|---|---|---|---|
| Access-Frontend (verknüpfte Tabellen, QBE) | **erstklassig** (ODBC Driver 18, kanonischer Microsoft-Pfad, SSMA verlinkt sogar automatisch) | schwach (ODBC-Treiber Dritter, Typprobleme) | brauchbar (psqlODBC), aber ungewohnt |
| Visueller Struktur-Designer | **SSMS**: Tabellendesigner + Datenbankdiagramme, kostenlos | kein vollwertiger | pgAdmin (ERD-Tool), ok |
| Typtreue zu Access | 1:1 (Tabelle in Abschnitt 5.1) | schwache Typisierung (Type Affinity), Datum als TEXT — Gegenteil von Ziel 2 | gut |
| .NET/OleDb-Kompatibilität | **`?`-Parameter bleiben** (MSOLEDBSQL) | ADO.NET-Provider-Wechsel nötig | Npgsql: `?` nicht unterstützt → Umbau |
| T-SQL-Nähe zum Access-SQL der App | hoch (`TOP n`, `@@IDENTITY`, Klammer-Joins funktionieren) | mittel (`LIMIT` statt `TOP`) | mittel (`LIMIT`, `SERIAL`) |
| Verteilung Einzelplatz | LocalDB: 53-MB-MSI, Benutzerprozess, kein Dienst | perfekt (eine Datei) | schwer (Dienst, Ports) |
| Mehrplatz | Express: bewährt, kostenlos ≤ 10 GB | ungeeignet | gut, aber Betriebsaufwand |
| Windows-/Installer-Integration | **nativ** | gut | mäßig |

**Empfehlung D1: SQL Server** — LocalDB als Einzelplatz-Standard, Express als Mehrplatz-Option; gleiche Engine, gleiches T-SQL, gleicher Code, nur ein anderer Connection-String. SQLite scheidet wegen Ziel 1 (visuelles Access-Arbeiten) und Ziel 2 (strenge Typen) aus; PostgreSQL wegen Verteilungs- und Umbauaufwand ohne Gegenwert in dieser Umgebung.

**Ehrliche Grenze von LocalDB:** LocalDB-Instanzen laufen **pro Windows-Benutzer**. Das Szenario „zwei Windows-Konten am selben Rechner gleichzeitig" (der heutige laccdb-Schmerz) braucht entweder eine **freigegebene LocalDB-Instanz** (`SqlLocalDB share`, machbar, aber hakelig) oder gleich **Express als Dienst** — Letzteres ist die robuste Antwort und wird im Installer als Option „Mehrplatz" angeboten (D5).

---

## 5. Schema-Überführung

### 5.1 Typ-Mapping (vollständig für diese DB)

| Access (gemessen) | Spalten | SQL Server | Anmerkung |
|---|---|---|---|
| Double | 1.669 | `float` | bitgleich (IEEE 754) |
| Integer/Long | 340 | `int` | Autowert → `IDENTITY(1,1)` **nur wo Access COUNTER hat** (Feinmessung S1) |
| Text (WChar) | 228 | `nvarchar(n)` / `nvarchar(max)` | Länge aus Access; „Langer Text" → `nvarchar(max)` |
| Boolean (Ja/Nein) | 74 | `bit NOT NULL DEFAULT 0` | Access-True = −1 → 1 beim Laden wandeln; NOT NULL wegen Access-Frontend (3.1) |
| Datum | 16 | `datetime2(3)` | SSMA-Standard; ODBC Driver 18 + Access ≥ 2016 vertragen das |

**Collation:** `Latin1_General_CI_AS` für die ganze DB — case-insensitive wie Access, damit die Textverknüpfungen (`Bezeichner`, `Typname`, `Projektname`) ihr heutiges Vergleichsverhalten behalten. Wird bei `CREATE DATABASE` festgelegt.

### 5.2 Regeln

1. **Namen 1:1** — alle Tabellen- und Spaltennamen bleiben exakt erhalten, **einschließlich der 20 Umlaut-Spalten** (`[Wirkungsgrad_Öl]` ist in T-SQL uneingeschränkt gültig). Kein Rename im Zug der Migration — jede Umbenennung würde den kompletten SQL-Bestand der App (≈ 1.100 Statements) zum Risiko machen. Aufräumen ist Phase 2 (Abschnitt 5.5).
2. **PK/FK 1:1 + drei gezielte Ergänzungen (D4):** Die 83 FK-Zuordnungen werden als echte Constraints angelegt (gleiche UPDATE/DELETE-Regeln wie in Access). Die 3 PK-losen Tabellen erhalten einen PK (`ID` vorhanden? sonst `IDENTITY`-Zusatzspalte) — nötig fürs bearbeitbare Access-Frontend und für saubere Upserts.
3. **Indizes** wie in Access (Feinmessung S1 liest sie aus), zusätzlich geprüfte Indizes auf `ID_Ganglinie`/`ID_Projekt` der Massentabellen.
4. **Views statt gespeicherter Abfragen:** Nur die **3 nachweislich genutzten** Abfragen werden als T-SQL-Views angelegt (`Abfrage_Energietraeger_Effektiv`, `Abfrage_Kostenfaktoren`, `Abfrage_Projektgebaeude`) — Namen identisch, damit der App-Code (`SELECT … FROM Abfrage_…`) unverändert läuft. Die übrigen 26 + 7 werden **nicht** migriert (deckungsgleich mit der Löschliste des K-Strangs; D6). Die Abfrage-SQL wird beim Konvertieren auf Access-Eigenheiten geprüft (Klammer-Joins sind T-SQL-kompatibel; `IIf` etc. kommen laut Messung nicht vor, wird je View verifiziert).
5. **Identity-Strategie:** Access-COUNTER-Spalten werden `IDENTITY`; die Erstbefüllung läuft mit `SET IDENTITY_INSERT … ON`, damit **alle IDs exakt erhalten bleiben** (kein FK-Remapping nötig — anders als beim Access→Access-Merge geht es hier um eine 1:1-Kopie). Wo die App IDs selbst vergibt (`GetMaxID`-Muster), bleibt das Verhalten unverändert — Hinweis: unter echtem Mehrplatzbetrieb ist `MAX(ID)+1` racebehaftet; Ablösung durch `IDENTITY`/`OUTPUT` ist ein Phase-2-Punkt, kein Migrationsblocker.

### 5.3 Ergebnisartefakt

`sql/schema/001_grundschema.sql` (+ `002_views.sql`, `003_indizes_fk.sql`) im Repo — **generiert aus der Feinmessung, dann von Hand kuratiert und eingefroren**. SSMA dient als Zweitmeinung (Abschnitt 6.4), nicht als Quelle der Wahrheit.

### 5.4 Massendaten

Die 6 Ganglinien-/Klimatabellen (≈ 1,1 Mio. der 1,25 Mio. Zeilen) sind schmale 3–15-Spalten-Tabellen — `SqlBulkCopy` überträgt sie in Sekunden. Keine Sonderbehandlung nötig; Struktur bleibt Phase 1 unangetastet (8760er-Raster ist App-Invariante).

### 5.5 Ausdrücklich NICHT in dieser Migration (Phase 2, separat beauftragen)

- Umbenennungen (Umlaute, Namenskonventionen vereinheitlichen)
- Ablösung der Textverknüpfungen (`Bezeichner`/`Projektname` → ID-FKs)
- `GetMaxID` → `IDENTITY`-Rückgabe
- Datenmodell-Optimierung der Ganglinientabellen

Grundsatz: **Plattformwechsel und Redesign niemals im selben Schritt.** Erst verhaltensgleich umziehen und per Referenzlauf beweisen, dann verbessern.

---

## 6. Datenüberführung — das Migrationswerkzeug

### 6.1 Warum ein eigenes Werkzeug (Entscheidung D3)

Die Überführung ist **kein Einmalereignis**: Jeder Kunde hat eine eigene `Kenndaten.accdb` mit eigenen Projekten. Das Werkzeug muss daher **unbeaufsichtigt, wiederholbar und berichtend** laufen — beim Entwickler wie beim Kunden (Erstmigration im Installer/Erststart). SSMA ist ein interaktives Entwicklerwerkzeug und nicht verteilbar; es taugt als Kontrolle, nicht als Auslieferungsweg.

Die vorhandene `AccessMigration`-Codebasis (DB_Migration) liefert Schema-Reader, Abhängigkeitsordnung, Backup/Rollback, Dry-Run und Berichtswesen — sie wird um ein **SQL-Server-Ziel** erweitert (`EposSqlMigrator`):

### 6.2 Ablauf eines Migrationslaufs

| Schritt | Aktion | Absicherung |
|---|---|---|
| 1 | Quelle prüfen: `Kenndaten.laccdb` vorhanden? → Abbruch mit Klartextmeldung | keine offene Access-Sitzung |
| 2 | Quelle per App-`SchemaMigration` auf letzten Access-Stand heben | einheitlicher Ausgangspunkt für alle Kundenbestände |
| 3 | Ziel-DB anlegen aus `sql/schema/*.sql` (leer, mit FKs **deaktiviert**) | Schema aus dem Repo, nie generiert-on-the-fly |
| 4 | Daten je Tabelle in FK-Reihenfolge: `SqlBulkCopy`, `IDENTITY_INSERT ON`, Boolean −1→1 | Batch-Transaktion je Tabelle |
| 5 | FKs aktivieren (`WITH CHECK`) | **serverseitiger Integritätsbeweis** — jede Waise fällt hier auf |
| 6 | Validierung: Zeilenzahl je Tabelle Quelle=Ziel; je Tabelle Prüfsumme über sortierten Inhalt (`HASHBYTES`-Vergleich gegen quellseitige Berechnung); Stichproben `Tab_Projekt`, `Tab_Kenndaten` | Bericht als Datei (wie heute `migration_log_*.txt`) |
| 7 | Bei Fehler: Ziel-DB verwerfen (Quelle bleibt unangetastet — **die Access-Datei wird nie verändert**, sie IST das Rollback) | risikofrei wiederholbar |

Damit ist Ziel 3 („klare und zuverlässige Überführung") konstruktiv erfüllt: Jeder Lauf endet entweder mit vollständigem Prüfbericht oder folgenlos.

### 6.3 Umgang mit bekannten Datenaltlasten

Der Lauf **repariert nicht stillschweigend**. Verletzt eine Kundendatei beim FK-Aktivieren die Integrität (Erfahrung aus dem Access→Access-Lauf: `Tab_Energieanlagen` → `Tab_Heizkessel`), listet der Bericht die betroffenen Zeilen; per Konfigurationsschalter (`orphanPolicy`: `Abbruch` | `AlsProtokollAussetzen`) kann der betroffene FK ausnahmsweise mit `WITH NOCHECK` angelegt werden — sichtbar im Bericht, nie stumm.

### 6.4 SSMA als Zweitmeinung

Einmalig in S2: SSMA-Lauf gegen dieselbe Quelle, Abgleich des generierten Schemas mit unserem kuratierten DDL (Typen, Indizes, Nullability). Abweichungen werden begründet oder übernommen. Danach hat SSMA keine Rolle mehr.

---

## 7. App-Umstellung

### 7.1 Provider-Weiche (Entscheidung D2)

`DataRepository.GetConnectionString()` erhält eine Konfigurationsweiche (`Properties.Settings.DbModus`: `Access` | `Sql`):

```
Access:  Provider=Microsoft.ACE.OLEDB.12.0;Data Source=<Pfad>\Kenndaten.accdb;
Sql:     Provider=MSOLEDBSQL19;Data Source=(localdb)\MSSQLLocalDB;   (bzw. Server\Instanz)
         Initial Catalog=EPOS_Kenndaten;Integrated Security=SSPI;
         Use Encryption for Data=Optional;
```

**Empfehlung D2: OleDb-Stack behalten, Provider MSOLEDBSQL19** (aktueller, von Microsoft gepflegter OLE-DB-Treiber für SQL Server):

- Alle **276 `?`-Parameterzeilen bleiben unverändert** (OleDb bleibt positional).
- `OleDbTransaction` (29 Dateien), `GetOleDbSchemaTable`-Prüfungen und `RecordSet` funktionieren unverändert.
- `SELECT @@IDENTITY` funktioniert auf SQL Server (15 Dateien unverändert; Umstellung auf `SCOPE_IDENTITY()` ist Phase-2-Kosmetik).
- Die Alternative `Microsoft.Data.SqlClient` ist der modernere Stack, verlangt aber die Umschreibung sämtlicher `?`-Parameter auf `@namen` plus Tausch aller ADO-Typen — **viel Umbau ohne fachlichen Gegenwert**; als spätere Evolution möglich, weil die Weiche den Zugriff ohnehin zentralisiert.

**Parallelbetrieb als Sicherheitsnetz:** Solange die Weiche existiert, kann jede Installation per Einstellung auf Access zurück (mit Datenstand des letzten Migrationslaufs). Der Access-Pfad bleibt bis zum übernächsten Release erhalten (D5), dann Rückbau.

### 7.2 Dialekt-Sweep (klein, gemessen)

| Baustelle | Umfang | Maßnahme |
|---|---|---|
| Boolean-Literale `= True/False` | 5 Stellen (`KostenPositionCtrl.cs`) | `= 1` / `= 0` — funktioniert **auf beiden** Backends (Access akzeptiert 0/−1-Vergleiche; `<> 0` wo Altbestände −1 enthalten) |
| `LIKE`-Muster | 4 Stellen | prüfen: über ACE-OLEDB gilt bereits ANSI-Syntax (`%`/`_`) — vermutlich 0 Änderungen |
| `SELECT TOP n` | 37 Stellen | T-SQL-kompatibel, keine Änderung |
| Klammer-Join-Syntax | überall | T-SQL-kompatibel, keine Änderung |
| Datums-/Funktionsdialekt | 0 Stellen | nichts zu tun (gemessen) |

Der Sweep wird als **Checkliste mit Vollständigkeitsnachweis** abgearbeitet (Grep-Rezepte aus dieser Messung, in `sql/MIGRATION_Pruefrezepte.md` dokumentiert), analog zur Lokalisierungs-Prüfrezeptur.

### 7.3 Schemapflege-Schicht (der eigentliche Brocken)

`SchemaMigration.cs` (4.793 Zeilen) hält Kundenschemata idempotent aktuell — in Access-DDL. Strategie:

1. **Einfrieren:** Der Access-Zweig wird mit dem Cutover-Release eingefroren. Seine letzte Aufgabe: Kundenbestände unmittelbar vor der Erstmigration auf den finalen Access-Stand heben (Schritt 2 in 6.2).
2. **Neue Schritte nur noch T-SQL:** Ab Cutover erhält `SchemaMigration` einen SQL-Server-Zweig (gleiches Marker-/Idempotenz-Muster; `IF COL_LENGTH(…) IS NULL ALTER TABLE …` statt Schema-Probing per ACE). Die vorhandenen Access-Schritte werden **nicht** nach T-SQL portiert — das Grundschema-Skript (5.3) entspricht bereits dem Endstand.
3. Damit bleibt der bewährte Mechanismus „App bringt DB beim Start auf Stand" erhalten — nur die Sprache wechselt.

### 7.4 Lizenz-Lesemodus

Der heutige Gedanke „`Mode=Read` im Connection-String" übersetzt sich sauberer: SQL-Login/Rolle `db_datareader` bzw. `ApplicationIntent=ReadOnly` — wird im Lizenzstrang umgesetzt, hier nur als Anschlusspunkt notiert.

---

## 8. Verteilung und Betrieb

| Thema | Lösung |
|---|---|
| Installer Einzelplatz | SQL Server 2022 **LocalDB**-MSI als Prerequisite (~53 MB); DB-Anlage beim Erststart aus `sql/schema/*.sql`; danach Erstmigration des vorhandenen Kundenbestands (6.2) mit Fortschrittsanzeige und Bericht |
| Installer Mehrplatz | Option „vorhandenen SQL Server verwenden" (Express/Standard): Verbindungsdialog + Rechteprüfung; `icacls`-Post-Install entfällt für SQL, bleibt nur solange der Access-Parallelpfad existiert |
| Backups | App-Menüpunkt „Datenbank sichern" → `BACKUP DATABASE … TO DISK` (LocalDB/Express können das); zusätzlich vor jeder Schemamigration automatisch (heutiges `DB-Backup/`-Muster) |
| Alt-DB nach Migration | `Kenndaten.accdb` bleibt unangetastet liegen (umbenannt in `Kenndaten.vor-sql.accdb`) = Rückfallebene + Beleg |
| Künftige Versionsupdates | nur noch T-SQL-`SchemaMigration` (7.3); der Access→Access-Migrator (DB_Migration) wird für Neuinstallationen obsolet, bleibt aber für Alt-Support archiviert |
| Katalog-Updates (`_STAMM`) | unverändert nach der `ReadOnly`-Regel, künftig als T-SQL-Upserts im Update-Mechanismus |

---

## 9. Validierung und Abnahme

1. **Struktureller Beweis:** FK-Aktivierung `WITH CHECK` (6.2 Schritt 5) + Schemaabgleich gegen SSMA (6.4).
2. **Datenbeweis:** Zeilenzahlen + Inhalts-Prüfsummen je Tabelle, Bericht archiviert.
3. **Verhaltensbeweis (entscheidend):** Die bestehenden Referenzläufe (`Referenzlaeufe\`, B5-Vergleich, 816er-Smoke) werden **auf beiden Backends** gefahren — gleiche Projekte, gleiche Simulationen, Ergebnisse müssen bitidentisch bzw. innerhalb der etablierten Toleranz identisch sein. Double→float ist bitgleich; Abweichungen wären echte Befunde.
4. **Bedienbeweis:** Smoke über die Kern-Workflows (Projekt öffnen/duplizieren/rechnen/Bericht) auf SQL; die 5 Boolean-Stellen und 3 Views ausdrücklich testen.
5. **Frontend-Beweis:** Access-Design-Frontend: Datenblatt-Edit auf 3 Tabellen (mit bit-Spalten!), QBE-Abfrage über 2 verknüpfte Tabellen.

---

## 10. Risiken und Grenzen

| Risiko | Einschätzung | Gegenmaßnahme |
|---|---|---|
| Unentdeckter Access-Dialekt in selten laufendem SQL | mittel — 1.100+ Statements, Messung deckt Muster, nicht jede Zeile | Parallelbetrieb + Referenzläufe + Prüfrezepte; Fehlerpfad `FehlerMelden` zeigt fehlschlagendes SQL im Klartext |
| Kundendaten verletzen FK-Integrität | bekannt aus Juni-Lauf | `orphanPolicy` + Bericht (6.3), niemals stiller Datenverlust |
| LocalDB-Eigenheiten (Benutzerprozess, Instanzstart) | niedrig–mittel | Erststart-Prüfung mit Klartextdiagnose; Mehrplatz → Express |
| `SchemaMigration`-Doppelpflege während Parallelbetrieb | real, aber begrenzt | Schema-Freeze während Cutover-Fenster (D7); neue Schritte erst nach Rückbau des Access-Pfads oder doppelt in beiden Dialekten |
| Treiber-Prerequisites (MSOLEDBSQL19, ODBC 18) | niedrig | Installer-Prerequisites; keine Bitness-Falle mehr (Treiber gibt es sauber in x64) |
| Verwechslung der DB-Kopien im Repo (`Kenndaten-ok.accdb` etc.) | bekannt | Migrationsquelle ist ausschließlich der konfigurierte Laufzeitpfad |

**Grenze:** Access-Formulare/Makros/VBA in der `.accdb` gibt es nicht (reine Datendatei) — geprüft: nur Tabellen + Abfragen. Sollte sich in S1 doch ein VBA-Rest finden, wird er inventarisiert und entfällt ersatzlos.

---

## 11. Etappenplan

| Etappe | Inhalt | Ergebnis / Beweis | Aufwand |
|---|---|---|---|
| **S1 Feinmessung & Freeze** | ADOX-Scan: COUNTER-Spalten, Defaults, Indizes, Memo-Längen, Validierungsregeln; Abgleich mit K-Löschliste; Schema-Freeze-Punkt festlegen | `sql/S1_Feinmessung.md` (Inventar) | 1 PT |
| **S2 Schema-DDL** | kuratiertes T-SQL (Tabellen, PKs inkl. 3 Ergänzungen, FKs, Indizes, 3 Views, Collation); SSMA-Zweitmeinung | `sql/schema/*.sql` im Repo; leere DB baut fehlerfrei | 2–3 PT |
| **S3 Datenlader** | `EposSqlMigrator` auf AccessMigration-Basis: BulkCopy, IDENTITY_INSERT, −1→1, FK-Beweis, Prüfsummen, Bericht, orphanPolicy | Echtlauf gegen Live-Kopie: 110 Tabellen, 0 Differenzen | 3–4 PT |
| **S4 App-Umstellung** | Provider-Weiche, Dialekt-Sweep (5+4 Stellen + Verifikation), T-SQL-Zweig `SchemaMigration`, Erststart-Anlage | Build grün; App läuft komplett auf SQL | 4–6 PT |
| **S5 Design-Frontend** | `Kenndaten_Design.accdb` (ODBC-Links, DSN-less), SSMS-Diagramm „Gesamtmodell" | Frontend-Beweis (9.5) | 0,5–1 PT |
| **S6 Validierung** | Referenzläufe + Smoke auf beiden Backends, Abweichungsanalyse | Abnahmeprotokoll | 2–3 PT |
| **S7 Verteilung** | Installer (LocalDB-Prerequisite, Erstmigration, Backup-Menü), Doku `BETRIEB_SQL.md` | Testinstallation auf sauberer VM | 2–4 PT |
| | | **Summe** | **≈ 15–22 PT** |

S1–S3 sind ohne jede Änderung an der App risikofrei vorziehbar (die Access-Quelle wird nie verändert). Echter Umstellungsaufwand beginnt mit S4.

---

## 12. Entscheidungspunkte (zur Abnahme durch Philipp)

| Nr. | Frage | Empfehlung |
|---|---|---|
| **D1** | Zielplattform | **SQL Server** — LocalDB (Einzelplatz) + Express (Mehrplatz), gleiche Engine/Code |
| **D2** | Datenzugriff der App | **OleDb behalten, Provider MSOLEDBSQL19** (276 `?`-Stellen unverändert); SqlClient allenfalls Phase 2 |
| **D3** | Migrationswerkzeug | **eigener `EposSqlMigrator`** (AccessMigration-Erweiterung, verteilbar, berichtend); SSMA nur als einmalige Zweitmeinung |
| **D4** | Schema-Eingriffe Phase 1 | **strikt 1:1** plus genau drei Ergänzungen: PKs für `Tab_BHKW`, `Tab_DBTagVDaten_STAMM`, `Tab_Stromverbrauchertyp_STAMM`; `bit NOT NULL DEFAULT 0`; sonst nichts (keine Renames, Umlaute bleiben) |
| **D5** | Rollout | erst intern (S1–S6), Kundenrelease mit Parallelbetriebs-Weiche; Access-Rückfallpfad bis übernächstes Release, dann Rückbau; Design-Frontend nur intern |
| **D6** | Gespeicherte Abfragen | nur die 3 genutzten als Views; übrige 33 entfallen (Abgleich mit K-Strang-Löschliste in S1) |
| **D7** | Zeitpunkt | **nach Schema-Beruhigung des K-Strangs** (mind. nach K2/K3, ideal nach K6) bzw. expliziter Schema-Freeze; S1–S3 dürfen früher laufen, S4 erst ab Freeze |

---

## 13. Wechselwirkungen mit laufenden Strängen

- **K-Strang (Kosten/Energieträger):** ändert Schema bis K6 — D7 regelt die Reihenfolge. Die K-Löschliste und D6 (tote Abfragen) sind deckungsgleich zu halten; Löschungen VOR der Migration verkleinern die Migrationsfläche.
- **Stromspeicher (AP1–AP10):** fertig; `Tab_Stromspeicher*`/`Tab_ErgebnisStromspeicher` sind in der Messung enthalten und migrieren mit.
- **KI-Assistent:** greift über dieselbe `DataRepository` zu — profitiert automatisch; keine Sonderbehandlung.
- **Lizenzierung:** Lesemodus-Anschlusspunkt siehe 7.4.
- **Mehrbenutzer-Doku:** `BETRIEB_Mehrbenutzer_Datenbank.md` wird mit S7 durch `BETRIEB_SQL.md` abgelöst (icacls-Workaround entfällt für SQL-Installationen).

---

*Messprotokolle dieser Rev.: Schema-Scan (110 Tabellen, Typverteilung, FKs, PK-Lücken, Views) und Code-Inventur (Dialekt-Trefferzahlen) vom 19.08.2026; Rezepte reproduzierbar, Ablage der Grep-/Scan-Rezepte folgt in S1 als `sql/MIGRATION_Pruefrezepte.md`.*
