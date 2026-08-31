# Konzept: Datenhaltung von Access nach SQLite — Umsetzungskonzept

**Rev. 2 — Umsetzungskonzept (Plattform entschieden)** · 31.08.2026

Rev. 1 (31.08.2026 vormittags) war eine ergebnisoffene Plattformprüfung mit **D1 offen**.
**D1 ist entschieden: Es wird keinen Mehrbenutzerbetrieb geben.** Damit entfällt das einzige
Kriterium, das für SQL Server sprach, und dieses Dokument wechselt von der Prüfung zur Umsetzung.
Rev. 1 ist vollständig in Rev. 2 aufgegangen; die Plattformbegründung steht verkürzt in
Abschnitt 1.2, das Schwesterkonzept
[`Konzept_DB-Migration_SQL_EPOS-Plan.md`](Konzept_DB-Migration_SQL_EPOS-Plan.md) (Ziel SQL Server)
bleibt als Vergleichsgrundlage bestehen, wird aber **nicht** weiterverfolgt.

Grundlage: vollständige Neumessung der Live-Datenbank
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` (151,9 MB, Stand 31.08.2026) über ACE-OLE-DB **und**
DAO 120, sowie Code-Inventur `WindowsFormsApplication1`, beides vom 31.08.2026.
Die Datenbank wurde ausschließlich **lesend** geöffnet; es wurde nichts verändert und keine Zeile
Code angefasst.

---

## 1. Auftrag, Entscheidung, Abgrenzung

### 1.1 Ziele (unverändert)

1. **Visuelles Arbeiten bleibt möglich** — Tabellen ansehen, Daten pflegen, Abfragen
   zusammenklicken, Beziehungen sehen.
2. **Einfaches, übersichtliches, strukturiertes Datendesign.**
3. **Klare und zuverlässige Überführung**, wiederholbar auch bei Kundenbeständen.

### 1.2 Warum SQLite (Kurzbegründung, entschieden)

| Punkt | Befund |
|---|---|
| Mehrbenutzer | **entfällt per Entscheidung** — der einzige Vorteil von SQL Server ist damit gegenstandslos |
| Installer | **keine Voraussetzung** — kein MSI, kein Dienst, kein LocalDB je Benutzer |
| Bitness | **entfällt vollständig** — `SQLitePCLRaw` bringt die native Bibliothek mit; die ACE-Falle der x64-Umstellung wiederholt sich nie wieder |
| Typtreue | gleichwertig über **`STRICT`-Tabellen** (seit SQLite 3.37) |
| Dateimodell | bleibt wie heute — Sicherung = Dateikopie, `DB-Backup/`-Verfahren trägt unverändert |
| Kosten/Lizenz | 0 €, Public Domain |

Die aktenkundige Mehrbenutzer-Not aus
[`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md) war **zwei Windows-Konten
am selben Rechner** und laut jenem Dokument ausdrücklich „**nicht Access, sondern NTFS**". Dieser
Fall bleibt mit SQLite gelöst (WAL + `busy_timeout`); die `icacls`-Zeile bleibt nötig und war nie
ein Datenbankproblem.

**Bewusst aufgegeben:** Netzwerk-Mehrplatzbetrieb ist mit SQLite nicht erreichbar (WAL funktioniert
über SMB nicht). Das ist der Preis der Entscheidung und hier festgehalten, damit er später nicht
als Überraschung auftaucht.

### 1.3 Ausdrücklich NICHT Gegenstand (Phase 2)

Umbenennungen (Umlaute, Namenskonventionen) · Ablösung der Textverknüpfungen
(`Bezeichner`/`Projektname` → ID-FKs) · `GetMaxID` → Identity-Rückgabe · Redesign der
Ganglinientabellen · Heilung der U+FFFD-Altlast.

**Grundsatz: Plattformwechsel und Redesign niemals im selben Schritt.** Erst verhaltensgleich
umziehen und per Referenzlauf beweisen, dann verbessern.

---

## 2. Messgrundlage — vollständiges Inventar (31.08.2026)

### 2.1 Datenbank

| Kennwert | Wert |
|---|---|
| Dateigröße | 151,9 MB |
| **Echte Tabellen** | **114** |
| Verknüpfte Tabellen (ODBC, verwaist) | **0** — waren 4, am 31.08.2026 entfernt, siehe 2.4 |
| Spalten (echte Tabellen) | 2.479 |
| Datentypen | **5** |
| **Autowert-Spalten (COUNTER)** | **80** |
| **Memo-Spalten (Langer Text)** | **14** |
| Indizes | **444**, davon eindeutig **232** |
| **Beziehungen** | **90**, davon **0 nicht erzwungen** |
| — mit Update-Kaskade | 61 |
| — mit Delete-Kaskade | 79 |
| Tabellen ohne PK | 3 (`Tab_BHKW`, `Tab_DBTagVDaten_STAMM`, `Tab_Stromverbrauchertyp_STAMM`) |
| Spalten mit Standardwert | 260 — **ausnahmslos Literale**, kein einziger Ausdruck |
| **Gültigkeitsregeln** | **0** (weder Tabelle noch Feld) |
| Gespeicherte Abfragen | **17**, davon **14 im Code referenziert** |
| Spaltennamen mit Umlaut/ß | 20 · Tabellennamen: 0 |
| Zeilen gesamt | ≈ 1,57 Mio. |

**Typverteilung:**

| Access | Spalten | Anteil | SQLite (`STRICT`) |
|---|---|---|---|
| Double | 1.730 | 69,8 % | `REAL` |
| Long/Integer | 371 | 15,0 % | `INTEGER` |
| Text | 261 | 10,5 % | `TEXT` |
| Boolean | 97 | 3,9 % | `INTEGER` |
| Datum | 20 | 0,8 % | `TEXT` (ISO-8601) |

Keine Anlagen-, Mehrwert-, OLE-, Hyperlink- oder Currency-Felder.

**Textlängen** (261 Spalten): 171 × `TEXT(255)`, 27 × `TEXT(50)`, 15 × `TEXT(20)`,
11 × `TEXT(30)`, Rest ≤ 10 Spalten je Länge. Die 14 Memo-Spalten kommen hinzu.

**Standardwerte** (260): `0` (219 ×), `No` (35 ×), `""` (2 ×), `False`, `"%"`, `50`, `Null`
(je 1 ×). **Nichts davon ist ein Ausdruck** — es gibt kein `Now()`, `Date()` oder `=…` in einem
Standardwert.

**Größte Tabellen:** `Tab_StromganglinieDaten` 823.441 · `Tab_Solar_STAMM` 280.320 ·
`Tab_Solar` 254.040 · `Tab_WaermebedarfDaten` 96.360 · `Tab_StromganglinieDaten_STAMM` 78.840 ·
`Tab_WaermebedarfDaten_STAMM` 35.040.

### 2.2 Die 14 Memo-Spalten

`Berichtskonfiguration.KonfigJson` · `energy_price.notes` ·
`Tab_Energieanlagen.WQ_Wochenwerte` · `Tab_ErgebnisWirtschaftlichkeit.Fehlgrund` ·
`Tab_ErgebnisWirtschaftlichkeit.HinweisText` · `Tab_ErgebnisWirtschaftlichkeit.SteuerHerkunft` ·
`Tab_Kostenprofil.Wochenwerte` · `Tab_KostenVorlage.Bemerkung` (+ 6 in den verwaisten
Verknüpfungen aus 2.4, die entfallen).

In SQLite ist das **unauffällig**: `TEXT` hat keine Längengrenze, Memo und Text fallen auf denselben
Typ zusammen. **Korrektur gegenüber Rev. 1**, wo „keine Memo-Felder" stand — die OLE-DB-Typmessung
meldet Memo als `adWChar` und verschluckt den Unterschied; DAO zeigt ihn.

### 2.3 Anwendung

| Kennwert | Wert |
|---|---|
| Zentraler Zugriff | `DataRepository.GetConnectionString()` — **eine** Stelle ([DataRepository.cs:161](WindowsFormsApplication1/Allgemein/DataRepository.cs)) |
| Ausführungsmethoden | 6 (`GetDataTable`, `ExecuteSQL`, `ExecuteNonQuery`, `ExecuteInsertAndGetId`, `ExecuteScalar`, `BeginTransaction`) |
| SQL-Zeilen mit `?` | 974 |
| `OleDbParameter`-Objekte | 2.270 |
| **Dateien mit eigener `OleDbConnection`** | **36** |
| `BeginTransaction`-Dateien | 29 |
| `RecordSet`-Nutzer | 61 |
| `SELECT TOP n` | 42 |
| `@@IDENTITY` | 20 Stellen / 16 Dateien |
| Boolean-Literale `= TRUE/FALSE` im SQL | 42 |
| `SchemaMigration.cs` / `SchemaKatalog.cs` | 13.589 / 3.461 Zeilen, `ZIEL_VERSION = 61` |
| DDL in der Schemapflege | 17 `CREATE TABLE`, 35 `ALTER TABLE` (**alle `ADD <Spalte>`**), 13 `CREATE INDEX`, 5 `DROP TABLE` |

### 2.4 Fund: vier verwaiste ODBC-Verknüpfungen — **bereinigt am 31.08.2026**

In der `.accdb` standen vier **verknüpfte** (keine echten) Tabellen:

```
ar_internal_metadata · products · schema_migrations · sqlite_sequence
  → ODBC;DSN=testsqlite2;Database=C:\Ruby33-x64\bin\store\storage\development.sqlite3;…
```

Der DSN `testsqlite2` löst nicht mehr auf („ODBC-Verbindung zu 'testsqlite2' fehlgeschlagen"), die
Tabellen sind unlesbar. Es sind Reste eines früheren Rails-Experiments.

Drei Konsequenzen:

1. **Sie werden nicht migriert** — sie tragen keine Daten und gehören nicht zum Modell.
   Der Migrator muss verknüpfte Tabellen (`TableDef.Connect <> ""`) **überspringen**.
2. **`sqlite_sequence` wäre ein harter Stopper.** In SQLite ist jeder Name mit Präfix `sqlite_`
   für interne Zwecke reserviert; `CREATE TABLE sqlite_sequence` scheitert. Zudem legt SQLite
   diese Tabelle bei `AUTOINCREMENT` **selbst** an. Ein Migrator, der stumpf über `TableDefs`
   läuft, bricht hier ab. Der Ausschluss aus Punkt 1 löst das mit.
3. **Nebenbei ein Beleg für Ziel 1:** Der Weg „SQLite über ODBC in Access einbinden" wurde in
   genau dieser Datei schon einmal benutzt. Der Verbindungsstring trägt die Signatur des
   quelloffenen sqliteodbc-Treibers (`ILike`, `NoWCHAR`, `StepAPI`, `FKSupport`).

**Erledigt am 31.08.2026.** Die vier Verknüpfungen wurden über DAO aus der Produktivdatenbank
entfernt, nachdem belegt war, dass nichts von ihnen abhängt: **0** Referenzen im C#-Code (der
einzige `products`-Treffer ist ein englischer UI-Text), **0** Nennungen in den 17 Abfragen,
**0** beteiligte Beziehungen; der DSN `testsqlite2` ist in keiner Registry-Hive registriert und
die Zieldatei existiert nicht. Vorher wurde `Kenndaten_vor-Linkbereinigung_2026-08-31.accdb`
angelegt (größengleich zur Quelle).

Nachweis: TableDefs ohne `MSys*` **118 → 114**, verknüpfte Tabellen **4 → 0**, Beziehungen
**90 → 90** unverändert, Stichprobenzeilenzahlen (`Tab_Projekt` 30, `Tab_Energieanlagen` 147,
`Tab_Heizkessel` 25, `Z_AnlageSenke` 97, `Tab_StromganglinieDaten` 823.441, `energy_carrier` 27)
exakt gleich. **DAO- und OLE-DB-Sicht melden seither übereinstimmend 114** — die Zähldifferenz aus
Abschnitt 2.1 ist damit aufgelöst und der Abnahmebeweis in S7 braucht keine Fußnote mehr.

Die Skip-Regel im Migrator (S3, Schritt 3) bleibt trotzdem bestehen — als Sicherung für
Kundenbestände, die eigene Verknüpfungen enthalten können.

---

## 3. Access-Funktionen und -Eigenheiten — vollständige Erhebung

Dies ist der Abschnitt, an dem die Messung vom 19.08. nachweislich danebenlag („Access-Dialekt im
SQL: **praktisch keiner** … `IIf`/`Nz`/`Format` = 0 Treffer"). Ursache: Dieser Code setzt SQL
**mehrzeilig per String-Verkettung** zusammen. Ein Suchmuster, das Funktionsname und
SQL-Schlüsselwort in *derselben Zeile* verlangt, findet solche Stellen systematisch nicht.

**Korrekte Rezeptur:** erst alle String-Literale extrahieren, dann *in deren Inhalt* nach
Funktionsnamen suchen. Das schließt C#-Methodenaufrufe (`.Last()`, `Format(...)`) sauber aus und
erfasst mehrzeiliges SQL vollständig.

### 3.1 Befund im C#-Code

| Funktion | Treffer | Fundort | SQLite | Maßnahme |
|---|---|---|---|---|
| `IIf(...)` | **14** | ausschließlich `SchemaMigration.cs` (13 Zeilen) | `iif()` ab 3.32 vorhanden | siehe 3.3 |
| `UCase(...)` | **3** | `SchemaMigration.cs` 7146/7153/7154 | → `upper()` | umschreiben |
| `Trim(...)` | 15 | verteilt | **vorhanden** | keine Änderung |
| `Nz`, `Format`, `Switch`, `Choose`, `Val`, `Str`, `CStr`/`CInt`/`CDbl`/`CDate`, `DLookup`/`DCount`/`DSum`/`DMax`/`DMin`/`DAvg`, `DateAdd`/`DateDiff`/`DatePart`/`DateSerial`, `InStr`/`InStrRev`/`StrComp`, `IsNumeric`/`IsDate`, `Environ`, `CurrentUser`, `Eval`, `Partition`, `First`/`Last` (als Aggregat) | **0** | — | — | nichts zu tun |
| `#…#`-Datumsliterale | **0** | — | — | nichts zu tun |
| `&`-Stringverkettung im SQL | **0** | — | (`\|\|`) | nichts zu tun |
| `DISTINCTROW`, `TRANSFORM`, `PARAMETERS` | **0** | — | — | nichts zu tun |

Die Treffer auf `Last(` im Rohscan sind **Fehltreffer**: deutscher Fachbegriff („die Laststufe",
„Durchsatz zur Last") und C#-LINQ `.Last()`. Kein Access-Aggregat.

### 3.2 Befund in den 17 gespeicherten Abfragen

| Abfrage | Dialekt | im Code genutzt |
|---|---|---|
| `Abfrage_Energietraeger_Effektiv` | **`IIf`**, `Is Null` | ✔ 30 × |
| `Abfrage_Kostenfaktoren` | **`IIf`**, Klammer-Join | ✔ 27 × |
| `Abfrage_MaxMin_Vorlauf` | Klammer-Join | ✖ |
| `Abfrage_Kuehlung_MaxLast` | sauber | ✔ 7 × |
| `Abfrage_Gebaeudearten` | sauber | ✔ 4 × |
| `Abfrage_SST` | sauber | ✔ 3 × |
| `Abfrage_KenndatenKuehlung_Max` | sauber | ✔ 3 × |
| `Abfrage_ProjektStromGanglinie` | sauber | ✔ 2 × |
| `Abfrage_Gebaeudetypen`, `Abfrage_Monatsstrom`, `Abfrage_Monatswaerme_Brauchwasser`, `Abfrage_Monatswaerme_Prozesse`, `Abfrage_Projektgebaeude`, `Abfrage_ProjektGebaeudeGanglinie`, `Abfrage_Tagverteilung` | sauber | ✔ je 1 × |
| `Abfrage_Max_Vorlauf`, `Abfrage_Min_Vorlauf` | sauber | ✖ |

**Wesentliche Korrektur zu D6 des Schwesterkonzepts:** Dort heißt es „vom Code genutzte Abfragen:
**nur 3**". Gemessen sind es **14 von 17**. Es werden also **14 Views** gebraucht, nicht 3.
Nur `Abfrage_Max_Vorlauf`, `Abfrage_MaxMin_Vorlauf` und `Abfrage_Min_Vorlauf` entfallen.

Fünf weitere `Abfrage_*`-Namen stehen im Code (`Abfrage_Heizkessel_Kosten`,
`Abfrage_KostenKomponenten`, `Abfrage_Neues_Kosten_Model`, `Abfrage_ProjektKostenInvestBetrieb`,
`Abfrage_Erzeuger_Vorlauftemperaturen`), existieren aber **nicht** in der Datenbank — sie stammen
aus Migrationsschritten (Anlegen/Verwerfen) und sind kein Migrationsgegenstand. **In S1 je Name
bestätigen**, bevor etwas entfällt.

### 3.3 Übersetzung der beiden `IIf`-Views

Beide sind `CREATE VIEW`-Definitionen aus `SchemaMigration.cs` (Zeilen 5514–5516 bzw. 6347–6348)
und werden gebraucht. Empfehlung: **nicht** auf SQLites `iif()` setzen, sondern auf `CASE WHEN` —
das ist normgerecht, in jedem Werkzeug lesbar und macht die Views versionierbar:

```sql
-- Abfrage_Energietraeger_Effektiv
CREATE VIEW Abfrage_Energietraeger_Effektiv AS
SELECT s.ID_Projekt, s."ID_Energieträger" AS carrier_id, ec.code, ec.name, ec.billing_unit,
       CASE WHEN s.custom_hi IS NULL OR s.custom_hi = 0
            THEN ec.hi_kwh_per_unit ELSE s.custom_hi END AS eff_hi,
       CASE WHEN s.custom_hs IS NULL OR s.custom_hs = 0
            THEN ec.hs_kwh_per_unit ELSE s.custom_hs END AS eff_hs
FROM energy_project_settings AS s
JOIN energy_carrier AS ec ON s."ID_Energieträger" = ec.id;
```

Die verschachtelten `IIf` in `Abfrage_Kostenfaktoren` werden analog zu einem dreistufigen
`CASE WHEN … END` — inhaltlich unverändert, auch in der `ORDER BY`-Wiederholung.

Die **Klammer-Joins** (`FROM a JOIN (b JOIN c ON …) ON …`) sind SQLite-gültig: Die Grammatik
erlaubt eine geklammerte `join-clause` als `table-or-subquery`. In S2 je View durch Ausführung
bestätigen.

### 3.4 Access-Eigenheiten außerhalb von SQL

| Eigenheit | Befund | Folge |
|---|---|---|
| Formulare, Berichte, Makros, VBA | **keine** — reine Datendatei | entfällt |
| Gültigkeitsregeln (Validation Rules) | **0** | nichts zu portieren |
| Standardwerte als Ausdruck | **0** von 260 | alle als SQLite-`DEFAULT` übernehmbar |
| Nachschlagefelder (Lookup) | in S1 zu prüfen — reine Anzeigeeigenschaft, ohne Datenwirkung | vermutlich entfällt |
| Eckige Klammern `[Feld]` | überall | **SQLite akzeptiert sie** (ausdrücklich zur Access-Kompatibilität) — keine Änderung |
| Access-Boolean `-1` | 97 Spalten | Wandlung `-1 → 1` im Migrator |
| Access-Datum (Double-Serial) | 20 Spalten | Wandlung nach ISO-8601-Text im Migrator |

**Ergebnis:** Der Access-Dialekt dieses Bestands beschränkt sich auf **`IIf` (14) und `UCase` (3),
sämtlich in `SchemaMigration.cs`**, plus `IIf` in zwei Views. Alles Übrige ist bereits ANSI-nah.
Da der Access-Zweig der Schemapflege eingefroren wird (Abschnitt 8), sind **nur die beiden Views
tatsächlich zu übersetzen**.

---

## 4. Zielbild

```
   Kenndaten.sqlite   (eine Datei am heutigen Ort — kein Dienst, keine Voraussetzung)
        │
        ├── EPOS-Plan (App)
        │     DataRepository  →  Microsoft.Data.Sqlite
        │       · ?  →  @pN   (zentral, in einer Methode)
        │       · PRAGMA foreign_keys=ON · journal_mode=WAL · busy_timeout=5000
        │
        └── Visuelle Werkzeuge (nur intern, nicht ausgeliefert)
              · SQLiteStudio          — Entwurf, Query-Builder, Formularansicht
              · DBeaver Community     — ER-Diagramm aus den 90 Fremdschlüsseln
              · Access + sqliteodbc   — Datenblatt und QBE im gewohnten Ablauf

   Schema-Wahrheit im Repo:  sql/schema/*.sql   (versioniert, diffbar, reviewfähig)
```

Der Kern von Ziel 2 ist der letzte Punkt: Die Struktur wird vom Binärblob zum **Codeartefakt**.

---

## 5. Zielschema (S2)

### 5.1 Typabbildung

| Access | Spalten | SQLite (`STRICT`) | Anmerkung |
|---|---|---|---|
| Double | 1.730 | `REAL` | bitgleich (IEEE 754) — Voraussetzung für den Referenzlaufbeweis |
| Long, **Autowert** | 371 (davon **80** Autowert) | `INTEGER`, Autowert → `INTEGER PRIMARY KEY AUTOINCREMENT` | siehe 5.2 |
| Text | 261 | `TEXT` | Länge als `CHECK(length(x) <= n)`, siehe 5.4 |
| Memo | 14 | `TEXT` | keine Sonderbehandlung |
| Boolean | 97 | `INTEGER … CHECK (x IN (0,1))` | `-1 → 1` im Migrator; NOT-NULL siehe 5.5 |
| Datum | 20 | `TEXT` `'YYYY-MM-DD HH:MM:SS'` | ISO-8601: sortierbar, in jedem Werkzeug lesbar, `date()`/`strftime()` rechnen damit |

### 5.2 Autowert

80 Spalten. Muster:

```sql
CREATE TABLE Tab_Energieanlagen (
    ID             INTEGER PRIMARY KEY AUTOINCREMENT,
    Bezeichner     TEXT,
    WQ_Unbegrenzt  INTEGER CHECK (WQ_Unbegrenzt IN (0,1)),
    "Rücklauf"     REAL
) STRICT;
```

**`AUTOINCREMENT` bewusst gesetzt.** Ohne das Schlüsselwort ist die Spalte ein `rowid`-Alias und
SQLite vergibt gelöschte IDs erneut. Da die Anwendung IDs teilweise selbst über `MAX(ID)+1`
vergibt (`GetMaxID`), ist die monoton steigende Variante die sichere. Preis: SQLite legt intern
`sqlite_sequence` an — siehe die Namenskollision in 2.4.

Zwei Besonderheiten aus der Messung: `Tab_Kostenfaktor.StammID` ist Autowert (heißt nur nicht `ID`),
und `Tab_Klimadaten_STAMM.ID_Klimadaten` / `Tab_Klimaregion_STAMM.ID_Klimaregion` ebenso. Der
Generator darf sich **nicht** auf den Namen `ID` verlassen.

### 5.3 Schlüssel, Beziehungen, Indizes

- **90 Beziehungen als echte `FOREIGN KEY`-Constraints.** Alle 90 sind in Access **erzwungen**
  (0 × `dbRelationDontEnforce`) — sie werden also 1:1 scharf übernommen, inklusive
  **61 × `ON UPDATE CASCADE`** und **79 × `ON DELETE CASCADE`**.
- **`PRAGMA foreign_keys = ON` ist Pflicht und wird je Verbindung gesetzt.** SQLite hat
  Fremdschlüssel standardmäßig **aus**. Ohne diese Zeile sind alle 90 Constraints wirkungslose
  Dekoration. Einzige richtige Stelle: das Öffnen der Verbindung in `DataRepository`.
- **444 Indizes** (232 eindeutig) werden übernommen. Die impliziten PK-/Unique-Indizes erzeugt
  SQLite selbst; der Generator muss sie **entdoppeln**, sonst entstehen 232 Dubletten.
- **3 fehlende PKs** ergänzen: `Tab_BHKW`, `Tab_DBTagVDaten_STAMM`,
  `Tab_Stromverbrauchertyp_STAMM`. Alle drei haben bereits eine Autowert-`ID` — sie wird PK,
  ohne Datenänderung.

### 5.4 Textlängen

SQLite kennt keine Längenbegrenzung. Zwei Wege:

- **(a) Länge als `CHECK(length(x) <= n)` mitführen** — hält Ziel 2 („strukturiert") ein und
  fängt Überlängen dort, wo Access sie heute fängt.
- **(b) Weglassen** — schlanker, aber ein heute abgewiesener Wert liefe künftig durch.

**Empfehlung: (a)**, aber nur für die deklarierten Längen unter 255 (90 Spalten). Für die 171
`TEXT(255)`-Spalten ist die Grenze erkennbar eine Access-Vorgabe und keine fachliche Regel —
dort ohne `CHECK`.

### 5.5 Boolean und NULL — zu entscheiden (D3)

Gemessen: **97 Boolean-Spalten, davon 0 als `Required` markiert.** Zugleich prüft der Code an
mehreren Stellen ausdrücklich auf NULL, z. B.
`AND ([user_edited] = FALSE OR [user_edited] IS NULL)`.

| Weg | Wirkung | Risiko |
|---|---|---|
| **(a) `INTEGER NOT NULL DEFAULT 0`** + NULL→0 im Migrator | 42 `= TRUE/FALSE`-Vergleiche werden deterministisch; Access-ODBC-Frontend meldet keine „Schreibkonflikte" | ein Codepfad, der bewusst NULL schreibt, bricht |
| (b) `INTEGER` nullable + `CHECK (x IN (0,1))` | verhaltensgleich zu heute | die `IS NULL`-Zweige bleiben nötig, Dreiwertigkeit bleibt |

**Empfehlung: (a)** — mit der Auflage, dass S1 belegt, dass kein Pfad NULL absichtlich schreibt.
`CHECK` lässt NULL ohnehin passieren, deckt den Fall also nicht ab.

### 5.6 Ergebnisartefakt

`sql/schema/001_grundschema.sql` · `002_views.sql` (14 Views) · `003_indizes_fk.sql` —
**generiert aus der Messung, dann von Hand kuratiert und eingefroren.** Ab Cutover ist dieser
Satz die einzige Quelle der Struktur.

---

## 6. Datenüberführung (S3)

Werkzeug `EposSqliteMigrator`, eigenständig und verteilbar — die Überführung ist **kein
Einmalereignis**: Jeder Kundenbestand ist eine eigene `.accdb`.

**Hinweis zur Aufwandsbasis:** Das im Schwesterkonzept als Grundlage genannte
`AccessMigration.sln` (dort D3) ist auf diesem Rechner **unter keinem Benutzerprofil auffindbar**.
Bis das geklärt ist, gilt S3 als Neubau, nicht als Erweiterung.

| Schritt | Aktion | Absicherung |
|---|---|---|
| 1 | `Kenndaten.laccdb` vorhanden? → Abbruch mit Klartext | keine offene Access-Sitzung |
| 2 | Quelle per App-`SchemaMigration` auf den Freeze-Stand heben | einheitlicher Ausgangspunkt für alle Bestände |
| 3 | **Verknüpfte Tabellen überspringen** (`Connect <> ""`), Systemtabellen (`MSys*`) überspringen | verhindert den `sqlite_sequence`-Abbruch (2.4) |
| 4 | Ziel aus `sql/schema/*.sql` anlegen, `foreign_keys = OFF` | Schema aus dem Repo, nie zur Laufzeit erzeugt |
| 5 | Daten je Tabelle in FK-Reihenfolge; **Boolean −1 → 1** (und NULL → 0 bei D3-a), **Datum → ISO-8601**; eine Transaktion je Tabelle, ein vorbereitetes `INSERT` je Tabelle | 1,57 Mio. Zeilen in Sekunden |
| 6 | `PRAGMA foreign_key_check` · `PRAGMA integrity_check` | **Integritätsbeweis** — jede Waise fällt hier auf |
| 7 | Zeilenzahl Quelle = Ziel je Tabelle; Prüfsumme über sortierten Inhalt; Stichproben `Tab_Projekt`, `Tab_Kenndaten`, `Tab_Energieanlagen` | Bericht als Datei |
| 8 | Bei Fehler: Zieldatei verwerfen | **Die `.accdb` wird nie verändert — sie IST das Rollback** |

**Altlasten nicht stillschweigend reparieren.** Bekannt sind Waisen `Tab_Energieanlagen` →
`Tab_Heizkessel`. Schalter `orphanPolicy` = `Abbruch` | `AlsProtokollAussetzen`; ausgesetzte
Fremdschlüssel stehen **namentlich im Bericht**, nie stumm.

---

## 7. Anwendungsumstellung (S4/S5)

### 7.1 Der Providerbruch und seine Entschärfung

Für SQLite gibt es **keinen OLE-DB-Provider**. Der bei SQL Server tragende Trick („nur den Provider
tauschen, alle `?`-Stellen bleiben") funktioniert hier nicht. Betroffen wären 974 SQL-Zeilen und
2.270 Parameterobjekte.

**Die Entschärfung macht daraus rund 40 Baustellen.** `OleDbParameter` ist ein reiner Datenträger
aus einem NuGet-Paket und lässt sich ohne jede OLE-DB-Verbindung konstruieren. Die sechs Methoden
von `DataRepository` behalten daher ihre Signatur `params OleDbParameter[]`, und übersetzt wird
**innen**:

```
SQL    :  … WHERE a = ? AND b = ?   →   … WHERE a = @p0 AND b = @p1
Werte  :  OleDbParameter[i].Value   →   SqliteParameter("@p" + i, …)
```

Zwei Messfakten machen das gefahrlos:

- **Kein einziges `?` steht innerhalb eines SQL-String-Literals** (über den gesamten Bestand
  geprüft) — der klassische Fallstrick dieser Technik existiert hier nicht.
- **SQLite akzeptiert `[eckige Klammern]`** als Bezeichnerbegrenzer. Der gesamte Bestand
  `[Wirkungsgrad_Öl]`, `[Bezeichner]`, `[user_edited]` läuft unverändert; die 20 Umlautspalten
  sind unkritisch, SQLite-Bezeichner sind UTF-8.

Ob `Microsoft.Data.Sqlite` positionelle `?` direkt binden kann, ist in S1 zu prüfen; die
Umschreibung trägt **unabhängig davon** und ist damit der robustere Weg.

### 7.2 Die drei echten Baustellen

| Baustelle | Umfang | Vorgehen |
|---|---|---|
| **(a) `DataRepository` als Übersetzer** | 1 Datei | `?`→`@pN`, `SqliteConnection`, PRAGMAs beim Öffnen. Danach laufen alle 974 SQL-Zeilen unverändert |
| **(b) Eigene Verbindungen** | **36 Dateien** | Schwerpunkte `ErgebnisCtrl` (9), `PufferSpCtrl` (8), `WaermequelleClass` (5), `Form_EingProzTyp` (3), `StilleDb` (3). Wo möglich **auf `DataRepository` zurückführen** statt nur den Typ tauschen — das verkleinert die Fläche dauerhaft |
| **(c) Transaktionen** | **29 Dateien** | `BeginTransaction()` gibt heute `(OleDbConnection, OleDbTransaction)` zurück und leckt den konkreten Typ. Rückgabe auf ein eigenes `DbVorgang`-Objekt umstellen |

`RecordSet.cs` (61 Nutzer) baut nur zwei eigene Verbindungen und zieht denselben
Verbindungsstring — es wandert mit (a) und (b) mit.

### 7.3 Dialekt-Sweep (S5)

| Baustelle | Umfang (gemessen) | Maßnahme |
|---|---|---|
| `SELECT TOP n` | **42** | → `LIMIT n`. **Muss** umgeschrieben werden — SQLite kennt `TOP` nicht |
| `SELECT @@IDENTITY` | **20 / 16 Dateien** | → `last_insert_rowid()`; in `ExecuteInsertAndGetId` zentral, Rest einzeln |
| Boolean-Literale `= TRUE/FALSE` | **42** | SQLite kennt `TRUE`/`FALSE` seit 3.23 als 1/0 → laufen **nach** der −1→1-Wandlung unverändert. **Prüfen, nicht blind übernehmen** |
| `IIf` in zwei Views | 2 | → `CASE WHEN` (Abschnitt 3.3) |
| `IIf`/`UCase` in `SchemaMigration.cs` | 14 / 3 | **entfällt** — Access-Zweig wird eingefroren (Abschnitt 8) |
| `LIKE` | 4 (+2 reine `DataView.RowFilter`) | ANSI `%`/`_` in beiden — voraussichtlich 0 Änderungen |
| `[eckige Klammern]`, Klammer-Joins | überall | keine Änderung; Views durch Ausführung bestätigen |
| übriger Access-Dialekt | **0** | nichts zu tun |

Abarbeitung als **Checkliste mit Vollständigkeitsnachweis** in `sql/MIGRATION_Pruefrezepte.md`,
analog zu
[`Lokalisierung_Pruefung.md`](WindowsFormsApplication1/Allgemein/Simulation/Lokalisierung_Pruefung.md).
**Die Rezepte müssen die Literal-Extraktion aus Abschnitt 3 verwenden**, nicht die zeilenweise
Suche — sonst wiederholt sich der Fehlbefund vom 19.08.

### 7.4 Pakete und Einstellungen

- **Neu:** `Microsoft.Data.Sqlite` (zieht `SQLitePCLRaw` mit der nativen x64-Bibliothek).
- **Bleibt vorerst:** `System.Data.OleDb` 8.0.1 — `OleDbParameter` wird weiter als Datenträger
  benutzt (7.1). Rückbau erst, wenn die Signaturen in Phase 2 wechseln.
- `Properties.Settings.DBName`: `Kenndaten.accdb` → `Kenndaten.sqlite`. `DBPath` bleibt.
- `DataRepository.ProviderVorhanden()` (die ACE-Startprüfung aus der x64-Umstellung) wird
  **überflüssig und entfällt** — es gibt keinen registrierungspflichtigen Provider mehr.

---

## 8. Schemapflege (S6)

1. **Access-Zweig einfrieren.** Letzte Aufgabe der 61 vorhandenen Schritte: Kundenbestände vor der
   Erstmigration auf den Freeze-Stand heben (S3, Schritt 2). Sie werden **nicht** portiert — das
   Grundschema aus S2 ist bereits ihr Endstand. Damit entfallen auch die 14 `IIf` und 3 `UCase`.
2. **Neue Schritte nur noch in SQLite-DDL**, gleiches Marker- und Idempotenzmuster.
   `PRAGMA table_info(...)` bzw. `sqlite_master` statt ACE-Schema-Probing.
3. **Das Muster trägt unverändert.** Gemessen sind alle 35 `ALTER TABLE` der Schemapflege
   `ADD <Spalte>` — genau das, was SQLite kann. Der zwölfschrittige Tabellenneubau, sonst SQLites
   wundester Punkt, wird hier **nicht gebraucht**.
4. `ZIEL_VERSION` läuft weiter (heute 61). Die Regel „erst Schritt, dann Zielzahl" bleibt.

---

## 9. Validierung (S7)

1. **Struktureller Beweis:** `PRAGMA foreign_key_check` und `PRAGMA integrity_check` sauber;
   Abgleich Tabellen-/Spalten-/Indexzahl gegen das Inventar aus Abschnitt 2.
2. **Datenbeweis:** Zeilenzahlen und Inhalts-Prüfsummen je Tabelle, Bericht archiviert.
3. **Verhaltensbeweis (entscheidend):** Die Referenzläufe (`..\Referenzlaeufe\`, Basis
   `2026-08-29_Booster`) auf **beiden** Backends. Double → `REAL` ist bitgleich; **jede Abweichung
   ist ein echter Befund, keine Toleranzfrage.**
4. **Bedienbeweis:** Projekt öffnen / duplizieren / rechnen / Bericht; ausdrücklich die 42
   `LIMIT`-Stellen, die 42 Boolean-Vergleiche, die 20 Identity-Stellen und **alle 14 Views**.
5. **Frontend-Beweis:** Access-ODBC-Frontend — Datenblatt-Bearbeitung auf drei Tabellen
   (mit Boolean-Spalten!) und eine QBE-Abfrage über zwei verknüpfte Tabellen.

**Kein Parallelbetrieb als Netz.** Anders als bei SQL Server verhindert der Providerbruch die
Weiche `Access | SQLite` im selben Build. Das erhöht das Gewicht von Punkt 3 spürbar — die
Referenzläufe sind hier die Hauptabsicherung, nicht eine Zusatzprüfung.

---

## 10. Verteilung und Werkzeuge (S8)

| Thema | Lösung |
|---|---|
| Installer | **keine Voraussetzung** — `Microsoft.Data.Sqlite` + `SQLitePCLRaw` liegen als normale Abhängigkeit im Ausgabeordner |
| Erststart | Datei aus `sql/schema/*.sql` anlegen, danach Erstmigration mit Fortschritt und Bericht |
| Alt-DB | bleibt liegen, umbenannt in `Kenndaten.vor-sqlite.accdb` = Rückfallebene und Beleg |
| Sicherung | Dateikopie; `VACUUM INTO 'ziel.sqlite'` erzeugt eine konsistente Kopie auch im Betrieb |
| Zwei Windows-Konten | WAL + `busy_timeout`; die `icacls`-Zeile aus `BETRIEB_Mehrbenutzer_Datenbank.md` bleibt nötig |
| Katalog-Updates (`_STAMM`) | unverändert nach der `ReadOnly`-Regel, künftig als SQLite-Upserts |

### 10.1 Visuelle Werkzeuge

| Werkzeug | Lizenz | Datenblatt | QBE | Beziehungen | Entwurf | Rolle |
|---|---|---|---|---|---|---|
| **SQLiteStudio** | GPL, portabel | ✔ + Formularansicht | ✔ Builder | ⚪ | ✔ | **Arbeitsgerät** — einziges mit Unterstützung eigener Kollationen (R1) |
| **DBeaver Community** | Apache 2.0 | ✔ stark | ✖ | ✔ **ER aus FKs** | ✔ | **Gesamtmodell** |
| **Access + sqliteodbc** | Access-Lizenz + freier Treiber | ✔ gewohnt | ✔ **voll** | ✔ Fenster | ✖ | **gewohnter Ablauf** — Treiber-Bitness muss zu Office passen |
| DB Browser for SQLite | frei | ✔ | ✖ | ✖ | ✔ | Alltag, schnell |
| HeidiSQL | GPL, Windows-nativ | ✔ | ✖ | ⚪ | ✔ | eine EXE |

**Empfehlung: SQLiteStudio + DBeaver Community**, Access-Frontend zusätzlich, wenn der
QBE-Entwurf gebraucht wird. Alle drei nur intern, **nicht** ausgeliefert.

Das ist zugleich das stärkste Argument, die 90 Fremdschlüssel in S2 wirklich zu deklarieren:
**Erst dadurch entsteht das ER-Diagramm, das Access heute im Beziehungsfenster zeigt** — Ziel 1
hängt an einer Schemaentscheidung, nicht am Werkzeug.

---

## 11. Risiken

### R1 — Kollation und Umlaute (größtes projektspezifisches Risiko)

Gemessen: Textschlüssel enthalten überwiegend Umlaute.

| Tabelle.Spalte (PK) | mit Nicht-ASCII | Beispiel |
|---|---|---|
| `Tab_Projekt.Projektname` | **14 von 30** | `Beispiel WP WG mit Erdwärme` |
| `Tab_Heizkessel.Bezeichner` | **10 von 25** | `Vitocrossal 200 CM2 raumluftabh<U+FFFD>ngig` |
| `Tab_Typ_Energieanlagen.Bezeichner` | 1 von 7 | `Wärmepumpe` |

Access vergleicht case-insensitiv über den vollen Zeichensatz. SQLites `NOCASE` faltet
**ausschließlich ASCII A–Z** — `'Erdwärme'` und `'ERDWÄRME'` gelten als verschieden.
Betroffen: `GetIdByName` (`WHERE {nameField} = ?`), die `LIKE`-Stellen, jeder Vergleich auf
`Bezeichner`/`Projektname`, und die Eindeutigkeit der Text-PKs.

**Vorgehen:** S1 klärt zuerst, **ob** sich der Code darauf verlässt — Werte werden überwiegend
exakt so zurückgegeben, wie sie gelesen wurden; dann ist `BINARY` unkritisch und sogar sauberer.
Erst wenn ein echter Bedarf belegt ist, eine eigene Kollation
(`SqliteConnection.CreateCollation`). **Preis:** Sie muss auf *jeder* Verbindung registriert sein;
ein Werkzeug, das sie nicht kennt, scheitert auf betroffenen Spalten und Indizes mit
„no such collation sequence" — das **beschädigt Ziel 1**. Von den empfohlenen Werkzeugen
unterstützt nur SQLiteStudio eigene Kollationen.

**Nebenbefund:** Die aus [`KONTEXT_Importkodierung_ANSI.md`](KONTEXT_Importkodierung_ANSI.md)
bekannte U+FFFD-Beschädigung ist in der Messung sichtbar und wandert **unverändert mit**
(Entscheidung D4).

### R2 — Kein Netzwerk-Mehrplatzbetrieb

Bewusst in Kauf genommen (1.2). Sollte die Anforderung je entstehen, ist ein zweiter
Plattformwechsel nötig — der Umbau aus 7.2 macht ihn allerdings **billiger als heute**, weil die
Weiche danach an genau einer Stelle liegt.

### R3 — Unentdeckter Dialekt in selten laufendem SQL

≈ 2.000 Statements; die Messung deckt Muster, nicht jede Zeile — und Abschnitt 3 zeigt, dass eine
schlecht gebaute Messung **systematisch** danebenliegen kann. Gegenmaßnahmen: die korrigierte
Rezeptur aus 3, die Referenzläufe aus 9, und `FehlerMelden` zeigt fehlschlagendes SQL im Klartext
(`KurzSql`). Ohne Parallelbetrieb (9) ist dies das Risiko mit dem größten Restgewicht.

### R4 — Migrationswerkzeug ohne belegte Basis

`AccessMigration.sln` ist nicht auffindbar (Abschnitt 6). Bis zur Klärung ist S3 als Neubau
kalkuliert; findet sich die Codebasis, sinkt S3 um geschätzt 1–2 PT.

---

## 12. Etappenplan

| Etappe | Inhalt | Beweis | PT |
|---|---|---|---|
| **S0** | Freeze; ~~verwaiste ODBC-Verknüpfungen entfernen~~ (erledigt 31.08.); R4 klären | Freeze-Punkt dokumentiert | 0,5 |
| **S1** | Feinmessung: Kollationsbedarf (R1), NULL-Schreibpfade (D3), Nachschlagefelder, die 5 unbekannten `Abfrage_*`-Namen | `sql/S1_Feinmessung.md` | 1 |
| **S2** | Zielschema: 114 Tabellen `STRICT`, 80 Autowerte, 90 FKs mit Kaskaden, 444 Indizes entdoppelt, 3 PKs ergänzt, **14 Views** | leere DB baut fehlerfrei | 2–3 |
| **S3** | `EposSqliteMigrator`: Verknüpfungen überspringen, −1→1, ISO-Datum, `foreign_key_check`, Prüfsummen, Bericht | Echtlauf: 114 Tabellen, 0 Differenzen | 3–5 |
| **S4** | Zugriffsschicht: `DataRepository`-Übersetzer, 36 Eigenverbindungen, 29 Transaktionsstellen | Build grün, App läuft auf SQLite | 5–8 |
| **S5** | Dialekt-Sweep: 42 `LIMIT`, 20 Identity, 42 Boolean, 2 Views | Prüfrezepte abgehakt | 2–3 |
| **S6** | Schemapflege: Access-Zweig einfrieren, SQLite-Zweig aufsetzen | ein neuer Schritt läuft durch | 2–3 |
| **S7** | Referenzläufe auf beiden Backends, Smoke, Frontend-Beweis | Abnahmeprotokoll | 2–3 |
| **S8** | Verteilung, Werkzeuge, `BETRIEB_SQLITE.md` | Testinstallation auf sauberer VM | 1–2 |
| | | **Summe** | **≈ 19–28 PT** |

**S0–S3 sind ohne jede Änderung an der Anwendung risikofrei vorziehbar** — die `.accdb` wird nie
schreibend angefasst. Echter Umstellungsaufwand beginnt mit S4.

---

## 13. Offene Entscheidungen

| Nr. | Frage | Empfehlung |
|---|---|---|
| ~~D1~~ | ~~Zielplattform~~ | **entschieden 31.08.2026: SQLite** (kein Mehrbenutzerbetrieb) |
| **D2** | Textlängen als `CHECK` mitführen? | **Ja für die 90 Spalten < 255**, nein für die 171 `TEXT(255)` (5.4) |
| **D3** | Boolean `NOT NULL DEFAULT 0` oder nullable? | **NOT NULL DEFAULT 0**, wenn S1 keine absichtlichen NULL-Schreibpfade findet (5.5) |
| **D4** | U+FFFD-Altlast | **mitwandern lassen**, separat heilen — sonst vermischen sich Plattformwechsel und Datenkorrektur |
| **D5** | Kollation | **`BINARY`** als Vorgabe; eigene Kollation nur bei belegtem Bedarf (R1) |
| **D6** | Abfragen | **14 Views** übernehmen, 3 entfallen (`Max_Vorlauf`, `Min_Vorlauf`, `MaxMin_Vorlauf`) — **nicht** 3 wie im Schwesterkonzept |
| **D7** | Zeitpunkt | **nach Schema-Beruhigung.** Offen: Einheitenbruch (Schritt 62), BHKW-Wirtschaftlichkeit (ab 63). S0–S3 dürfen früher laufen |
| ~~D8~~ | ~~Verwaiste ODBC-Verknüpfungen~~ | **erledigt 31.08.2026** — entfernt, Nachweis in 2.4 |

---

## 14. Korrekturen gegenüber den Vorgängerdokumenten

Festgehalten, damit die Fehlbefunde nicht ein drittes Mal übernommen werden.

| Aussage | Quelle | Befund 31.08.2026 |
|---|---|---|
| „`IIf`/`Nz`/`Format` = **0 Treffer**" | Schwesterkonzept 2.2 | **14 × `IIf`, 3 × `UCase`** im Code, `IIf` zusätzlich in 2 Views. Ursache: zeilenweise Suche findet mehrzeilig verkettetes SQL nicht |
| „Vom Code genutzte Abfragen: **nur 3**" | Schwesterkonzept 2.1, D6 | **14 von 17** werden referenziert |
| „**keine** Access-Spezialtypen … keine Memo" | Schwesterkonzept 2.1, Rev. 1 | **14 Memo-Spalten** — OLE DB meldet Memo als Text, DAO zeigt den Unterschied |
| „Boolean-Literale `= True/False`: **5 Stellen**" | Schwesterkonzept 7.2 | **42** |
| „**276** SQL-Zeilen mit Parametern" | Schwesterkonzept 2.2 | **974** Zeilen / **2.270** Parameterobjekte |
| „`SchemaMigration.cs` **4.793 Zeilen**" | Schwesterkonzept 2.2 | **13.589** |
| „**110** Tabellen" | Schwesterkonzept 2.1 | **114** echte + 4 verwaiste Verknüpfungen |
| „**83** FK-Zuordnungen" | Schwesterkonzept 2.1 | **90 Beziehungen**, alle erzwungen, 61 Update-/79 Delete-Kaskaden |
| „`AccessMigration.sln` als Basis vorhanden" | Schwesterkonzept 2.3, D3 | **nicht auffindbar** (R4) |
| „nur 2 COUNTER-Spalten" | Rev. 1, S1-Annahme | **80 Autowert-Spalten** |
| „SQLite: schwache Typisierung" | Schwesterkonzept D1 | überholt seit SQLite 3.37 — **`STRICT`-Tabellen** |

---

*Messprotokoll dieser Rev.: Schemascan über ACE-OLE-DB und DAO 120 (114 echte Tabellen, 4 verwaiste
ODBC-Verknüpfungen, 2.479 Spalten, 5 Typen, 80 Autowerte, 14 Memo, 444 Indizes/232 unique,
90 Beziehungen mit Kaskadenattributen, 260 Standardwerte, 0 Gültigkeitsregeln, 17 QueryDefs mit
Volltext, Nicht-ASCII in Textschlüsseln) sowie Code-Inventur über Extraktion sämtlicher
String-Literale (Access-Funktionsinventar, 974 `?`-Zeilen, 2.270 `OleDbParameter`, 42 `TOP`,
20 `@@IDENTITY`, 42 Boolean-Literale, 36 Eigenverbindungen, 19 `Abfrage_*`-Referenzen) — alles
vom 31.08.2026. Sämtliche **Messungen** liefen ausschließlich lesend (`Mode=Read` bzw. DAO
`ReadOnly=True`). Der einzige schreibende Zugriff war die Linkbereinigung nach Abschnitt 2.4
(31.08.2026, gesicherte Kopie vorher, Nachweis dort); die Zahlen dieses Abschnitts 2 sind, wo nicht
anders vermerkt, der Stand **vor** dieser Bereinigung.*
