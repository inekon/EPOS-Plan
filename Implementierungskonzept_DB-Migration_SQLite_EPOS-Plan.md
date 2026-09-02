# Implementierungskonzept: Datenhaltung Access → SQLite — EPOS-Plan

**Rev. 1** · 01.09.2026 · Grundlage:
[`Konzept_DB-Migration_SQLite_EPOS-Plan.md`](Konzept_DB-Migration_SQLite_EPOS-Plan.md) (Rev. 2, 31.08.2026)

Rev. 2 hat die Plattform entschieden und den Etappenplan S0–S8 aufgestellt. Dieses Dokument tut
zwei Dinge: Es **prüft jede Code-Behauptung von Rev. 2 gegen den heutigen Stand** (Branch
`sqlite` @ `7d41833`, 01.09.2026 — sechs unabhängige Messläufe: Zugriffsschicht,
Literal-Inventur, Schemapflege, D3/R1-Feinmessung, Umfeld/DAO-Spotcheck, Typ-Rückweg), und es
macht aus dem Etappenplan eine **Bauanleitung**: Arbeitspakete mit vollständigen Dateilisten,
Code-Mustern und Abnahmekriterien. Der Grundsatz aus Rev. 2 gilt unverändert:
**verhaltensgleich umziehen, per Referenzlauf beweisen, erst dann verbessern.**

Die Messmethodik folgt der korrigierten Rezeptur aus Rev. 2 Abschnitt 3 (String-Literale
extrahieren, darin suchen): 569 `.cs`-Dateien, 39.179 extrahierte Literale; die Messskripte
werden in S5 als `sql/tools/` eingecheckt und sind damit die wiederholbaren Prüfrezepte.

---

## 1. Prüfergebnis: Rev. 2 gegen den Code

### 1.1 Bestätigt (tragende Aussagen halten)

| Behauptung Rev. 2 | Messwert 01.09. | Status |
|---|---|---|
| 6 Ausführungsmethoden in `DataRepository`, eine Verbindungsstring-Quelle `GetConnectionString()` ([DataRepository.cs:161](WindowsFormsApplication1/Allgemein/DataRepository.cs)) | bestätigt; auch alle Eigenverbindungen ziehen diesen String | ✔ |
| 36 Dateien mit eigener `OleDbConnection` (Schwerpunkte ErgebnisCtrl 9, PufferSpCtrl 8, WaermequelleClass 5) | **36 Dateien / 67 Stellen**, Schwerpunkte exakt | ✔ |
| **Kein `?` in einem SQL-Textliteral** (Träger der ?→@pN-Umschreibung) | bestätigt — inkl. Kreuzfragment-Prüfung über Verkettungsgrenzen (293 offene Hochkommata, 0 Risikopaare) | ✔ |
| `@@IDENTITY` 20 Stellen / 16 Dateien | zeichengenau — davon **12 ausführbare Statements in 11 Dateien**, Rest Kommentare | ✔ |
| `SELECT TOP n` 42 | zeichengenau — davon **37 ausführbar, alle `TOP 1`**, 5 Doku-Kommentare | ✔ |
| Access-Funktionen sonst 0; `#…#`, `&`, DISTINCTROW/TRANSFORM/PARAMETERS = 0 | alle 0 bestätigt; `UCase` 3 und `Trim` 15 nur in `SchemaMigration.cs` | ✔ |
| 14 von 17 Abfragen im Code referenziert, `*_Vorlauf`-Trio ungenutzt | Rohzahlen zeichengenau bestätigt | ✔ |
| `SchemaMigration.cs` 13.589 / `SchemaKatalog.cs` 3.461 Zeilen, `ZIEL_VERSION = 61` | exakt | ✔ |
| `IIf` 14 auf 13 Zeilen in `SchemaMigration.cs` | exakt (aber siehe 1.2: 3 weitere im Laufzeitcode) | ✔/Δ |
| Live-DB 151,9 MB; Relations 90; QueryDefs 17 | 151,95 MB; 90; 17 (DAO-Spotcheck, strikt lesend) | ✔ |
| R4: `AccessMigration.sln` nicht auffindbar → S3 = Neubau | endgültig bestätigt: `C:\Users\DirkEngelmann\DB_Migration` existiert nicht, keine Treffer unter Documents/source/Desktop/Downloads | ✔ |

### 1.2 Korrekturen an Rev. 2

| Aussage Rev. 2 | Befund 01.09.2026 |
|---|---|
| „`BeginTransaction`: 29 Dateien" | **18 Dateien / 24 Aufrufe** über `DataRepository.BeginTransaction` — **plus 13 Dateien / 16 Stellen mit eigenem `conn.BeginTransaction()`**. Transaktionsführend sind zusammen **31 Dateien**; die „29" war vermutlich diese Summe auf anderem Stand |
| „`RecordSet`: 61 Nutzer" | 61 ist die grep-Zahl inkl. Kommentaren; **47 Dateien** nutzen die Klasse wirklich |
| „`LIKE`: 4 (+2 RowFilter)" | **20 SQL-Stellen + 2 RowFilter.** Die Filterfragmente der Katalogdialoge (BHKW, Heizkessel, PV, Pufferspeicher) gehen per Verkettung wirklich an die DB. Fast alle sind `Like '%'`-Allesfilter; Bewertung in Abschnitt 6 |
| „`IIf` ausschließlich in `SchemaMigration.cs`" | **3 Stellen im Laufzeitcode:** [Ladeordnung.cs:77](WindowsFormsApplication1/Allgemein/Simulation/Ladeordnung.cs) (ORDER BY Anlagenprio), [EmissionskatalogCtrl.cs:266](WindowsFormsApplication1/Controller/EmissionskatalogCtrl.cs) (`ORDER BY IIF(ist_aktiv,0,1)`), [ProjektDuplizierenCtrl.cs:728](WindowsFormsApplication1/Controller/ProjektDuplizierenCtrl.cs) (Offset-Remapping). SQLite kennt `iif()` seit 3.32 — voraussichtlich lauffähig, wird in S5 durch Ausführung bewiesen |
| „Boolean-Literale `= TRUE/FALSE`: 42" | **44 Zeilen / 51 Vorkommen** (kein einziges `<> TRUE/FALSE`); 20 Zeilen davon im Laufzeitcode. Sonderschreibweisen, die ein naives Muster verfehlt: `= true` (klein, SchemaMigration:10033), `=false` (ohne Leerzeichen, Form_KostenfaktorItem.cs:29) |
| „`GetMaxID` = `MAX(ID)+1`" | Die Methode liefert **nur MAX**; das `+1` steht bei den Aufrufern — **40 Stellen in 27 Dateien**. Eine Stelle nutzt GetMaxID **ohne** `+1` ([ProjektPhotovoltaikCtrl.cs:211](WindowsFormsApplication1/Controller/ProjektPhotovoltaikCtrl.cs)) — in S5 gesondert sichten |
| DDL-Inventur „17 CREATE / 35 ALTER (alle ADD Spalte) / 13 INDEX / 5 DROP" | Rohzeilen inkl. Kommentaren. In Literalen: 16 CREATE TABLE, **25 ALTER TABLE — davon 14 × `ADD CONSTRAINT … FOREIGN KEY` und 1 × `DROP CONSTRAINT`**, 17 CREATE INDEX, 1 DROP-TABLE-Schleife. Folge für die Schemapflege: Abschnitt 5 |
| „nur die beiden Views zu übersetzen" | Die Schemapflege schreibt **5** Abfragen (zusätzlich `Abfrage_SST`, `Abfrage_Kuehlung_MaxLast`, `Abfrage_KenndatenKuehlung_Max`) und löscht 5. Und: **`Abfrage_Kostenfaktoren` ist in Access eine PROCEDURE, keine View** — ACE erlaubt kein `ORDER BY` in `CREATE VIEW`. SQLite erlaubt es; dort wird sie eine normale View (3.2). Die Zeilenangaben in Rev. 2 Abschnitt 3.3 sind zudem vertauscht (Kostenfaktoren ≈ 5513, Energieträger ≈ 6344) |
| D7: „BHKW-Wirtschaftlichkeit ab Schritt 63 offen" | **Bereits gelandet** als Schritte 60/61 (`SCHRITT_60_BRENNSTOFF_BESTANDTEILE`, `SCHRITT_61_STEUER_JE_ANLAGE`). Offen ist nur noch der Einheitenbruch (künftig 62). Die Schema-Beruhigung ist näher als gedacht |
| „90 Beziehungen, 61 Update-/79 Delete-Kaskaden" | Unter den 90 DAO-Relations sind **2 Access-Systembeziehungen** (`MSysNavPaneGroup*`). Echte Fremdschlüssel zwischen echten Tabellen: **88** (59 Update-, 77 Delete-Kaskaden) — S2-Messung 01.09. |
| implizit in 5.2/5.3: „Autowert = PK" | **50 Autowert-Spalten sind in Access nicht alleiniger PK**: 49 Verbund-PKs, die den Autowert enthalten (z. B. `Tab_Energieanlagen (ID, ID_Projekt)`, `Tab_Heizkessel (ID, ID_Projekt, Bezeichner)`), dazu `Tab_Projekt` mit PK auf `Projektname` (`ID` nur UNIQUE). Auflösung: **D10** |
| „verwaiste ODBC-Verknüpfungen entfernt, 118 → 114" | Gilt für den **Messbestand von Rev. 2 (Rechner 2)**. Die Live-DB **dieses** Rechners hat die 4 Verknüpfungen (`ar_internal_metadata`, `products`, `schema_migrations`, `sqlite_sequence` → DSN `testsqlite2`) **noch**, dazu andere Zeilenzahlen (26 statt 30 Projekte, 648.241 statt 823.441 Ganglinienzeilen). Siehe 1.3 |
| „Referenzläufe unter `..\Referenzlaeufe\`" | liegen **im Repo**: `Referenzlaeufe\` (11 Läufe, Basis `2026-08-29_Booster` mit 13 Projekten; jüngster `2026-08-30_B3-Kaskade`) |
| „276 SQL-Zeilen mit Parametern" → Rev. 2: „974" | Auch 974 ist zu niedrig: je nach Definition **1.013–1.563 Zeilen** mit `?` im Literal. Für den Umbau egal (alles läuft zentral durch), für die Aufwandsschätzung nicht |

Randkorrektur: Von den fünf „Phantomnamen" steht keiner in einem ausführbaren Literal — alle
Vorkommen sind Kommentare (davon drei Namen gar nicht in `SchemaMigration.cs`, sondern in
KostenPositionCtrl/UcBkKosten/Form_Kosten/ProjektPuffer). Die S1-Frage aus Rev. 2 ist damit
beantwortet: **kein Laufzeitzugriff, kein Migrationsgegenstand.**

### 1.3 Neufunde — was Rev. 2 nicht kennt

Die folgenden sieben Befunde verändern den Zuschnitt von S4/S5; sie sind in die Arbeitspakete
(Abschnitt 9) eingearbeitet.

**N1 — Der Typ-Rückweg.** ACE liefert `Int32`, `Boolean`, `DateTime` in DataTable/Scalar.
Microsoft.Data.Sqlite liefert stattdessen `Int64`, `Int64 (0/1)` und `String` (ISO-Text).
Jeder harte Cast (`(int)row["ID"]`, `(bool)row["user_edited"]`) bricht zur Laufzeit;
`Convert.To…` läuft weiter — und Typ-Dispatch-Zweige laufen **still** ins Leere.
Gegenmaßnahme: zentrale Wiederherstellung des Typ-Rückwegs im neuen `GetDataTable` plus eng
begrenzter Sweep — Messwerte und Bauentscheidung in 2.4.

**N2 — `GetOleDbSchemaTable`: ~24 Stellen in 15 Dateien** (zweite Zählung: 29 — Abgleich im
Prüfrezept S4c), Schwerpunkt [ErgebnisCtrl.cs](WindowsFormsApplication1/Controller/ErgebnisCtrl.cs)
(6, darunter Rowsets `Indexes` und `Foreign_Keys`), ProjektDuplizierenCtrl (3),
WirtschaftlichkeitCtrl (2), ProjektExportImportCtrl (2). Kein Gegenstück in
Microsoft.Data.Sqlite → Ersatz über `PRAGMA table_info` / `index_list` / `foreign_key_list` /
`sqlite_master` (Abschnitt 2.7).

**N3 — Selbstheilungs-DDL außerhalb der Schemapflege: 29 Absetzstellen in 16 Dateien**
(Muster „Spalte fehlt → `ALTER TABLE … ADD`", dazu CREATE TABLE für Ergebnis-/Katalogtabellen
in WirtschaftlichkeitCtrl (5), ErgebnisCtrl (6 inkl. `CREATE INDEX idx_ErgPuffer`), GesetzKatalog,
EmissionsBilanzRechner, BerichtCtrl, VariantenCtrl u. a.). Diese Stellen laufen **zur Laufzeit**
im SQLite-Build und wandern auf die neuen Helfer (S4d) — sie sind vom Einfrieren des
Access-Zweigs **nicht** erfasst.

**N4 — ADOX-Reseed beim Projektimport.** [ProjektExportImportCtrl.cs:673–693](WindowsFormsApplication1/Controller/ProjektExportImportCtrl.cs):
Autowert-Erkennung per ADOX-COM plus `ALTER TABLE … ALTER COLUMN … COUNTER(max+1,1)` (einziges
`ALTER COLUMN` im Repo, reine ACE-Syntax). Unter SQLite: `sqlite_sequence` direkt setzen
(Abschnitt 2.9). ADOX-Verweis entfällt.

**N5 — `OleDbCommandBuilder`** an 3 Stellen (KlimadatenCtrl.cs:138 ×2, Form_WP.cs:507) — ohne
Gegenstück; Umbau auf ausdrückliche Kommandos. Ebenso ohne Gegenstück: `adapter.FillSchema`
(Kern von `TabellenSchema`, Abschnitt 5).

**N6 — Die Zwei-Bestände-Lage.** Rev. 2 wurde auf **Rechner 2** erarbeitet (Belege: Pfade
`C:\Waermeplan\WP_Plan\dev\…` im mitgesyncten `sqlite-probe\`, dortige Linkbereinigung, 30
Projekte). Auf **diesem** Rechner: 4 ODBC-Verknüpfungen noch vorhanden, 26 Projekte, eigene
Zeilenzahlen, `ProgramData\EPOS_PLAN\DB-Backup\` leer (die Sicherungen liegen im Repo unter
`DB-Backup\`), die in Rev. 2 genannte Sicherung `Kenndaten_vor-Linkbereinigung_2026-08-31.accdb`
existiert hier nicht. Folgen: S0 wird **je Rechner** fällig; Sollzahlen sind bestandsspezifisch —
der Migrator misst sie selbst aus der Quelle (Abschnitt 4); die Zahlen aus Rev. 2 Abschnitt 2
beschreiben den Bestand von Rechner 2.

**N7 — Settings-Falle.** `DBName` ist **User-Scope** (Default `Kenndaten.accdb`, `DBPath` leer).
Eine Bestandsinstallation behält ihren gespeicherten Wert — der neue Default `Kenndaten.sqlite`
greift dort nicht von allein. Erststart-Fixup nötig (Abschnitt 8).

Dazu zwei **Altfehler**, die die Messung nebenbei fand (unabhängig von der Migration falsch):
`BrauchwasserCtrl.cs:87` und `HeizkesselCtrl.cs:99` holen `SELECT @@IDENTITY` über
`GetDataTable` auf einer **neuen** Verbindung — schon unter ACE unzuverlässig, unter SQLite
(`last_insert_rowid()` ist strikt verbindungslokal) sicher falsch. Beide werden in S5 auf
`ExecuteInsertAndGetId` umgestellt.

### 1.4 Entschiedene S1-Fragen

| Frage (Rev. 2) | Entscheid | Beweis |
|---|---|---|
| Bindet Microsoft.Data.Sqlite positionelle `?`? | **Nein — Umschreibung ?→@pN ist Pflicht** (und ohnehin der robuste Weg) | Dokumentationslage; Umschreibung lexikalisch sicher dank „kein ? in SQL-Literalen" |
| **D3** Boolean `NOT NULL DEFAULT 0`? | **Ja (Variante a).** | Alle 97 Spalten sind `YESNO` — der Typ trägt in Access physisch kein NULL (der Code stützt sich an 6 Stellen ausdrücklich darauf, nachgewiesen in der Schritt-19-Verifikation). Von 261 DBNull-Schreibzuweisungen zielt **keine** auf Boolean; alle `OleDbType.Boolean`-Parameter sind literal oder nicht-nullbar; `bool?` heißt an den 3 relevanten Stellen „ableiten", nicht „NULL schreiben" (PufferSpCtrl.KlassenSetBestimmen löst vor dem Schreiben auf). Die 3 `IS NULL`-Zweige auf `user_edited` (SchemaMigration 4870/4880/5012) sind reine Absicherung — `= FALSE` und `IS NULL` führen dort zum selben Ergebnis |
| **D5** Kollation | **BINARY.** Zusätzlich fährt der Migrator einen **Case-Drift-Messlauf** je Bestand (Abschnitt 4, Schritt 7) — ist der leer, ist BINARY beweisbar folgenlos | Vergleichswerte stammen durchgehend aus DB-Rückleseschleifen (belegt an allen 15 `GetIdByName`-Aufrufern und ~30 verketteten WHERE-Stellen; Rücklesepunkt `ProjektCtrl.cs:213`). Gewollte Toleranz liegt im C#-Code (`OrdinalIgnoreCase`, `DublettenPruefung` faltet sogar über den vollen Zeichensatz — mehr als NOCASE je könnte). Kein Eindeutigkeitsindex bricht (BINARY lockert nur). Restrisiken: 4 Katalog-Dublettenwächter und `energy_carrier.code`-Prüfung werden case-sensitiv; Namens-Altpfade (`WQ_Puffer`, `Gebaeudename→Bezeichner`) sind bereits durch FKs abgelöst. Keine Blocker |
| 5 unbekannte `Abfrage_*`-Namen | **kein Migrationsgegenstand** — nur Kommentare, 0 ausführbare Literale | Literal-Inventur |
| NULL-Schreibpfade Boolean | keine | siehe D3 |

Offen aus S1 bleibt allein die Sichtung der **Nachschlagefelder** (reine Anzeigeeigenschaft im
Access-Designer, vermutlich folgenlos) — erledigt der Schema-Generator nebenbei, weil DAO die
Lookup-Eigenschaften mit ausliest.

---

## 2. Zugriffsschicht — Bauplan zu S4

Leitidee aus Rev. 2 bestätigt und verschärft: `OleDbParameter` bleibt als reiner Datenträger an
allen ~2.300 Stellen stehen (2.265 unqualifizierte + 28 vollqualifizierte Konstruktoren + 54
Array-Allokationen — an ihnen wird **nichts** geändert); übersetzt wird **innen** in
`DataRepository`. OleDb bindet rein nach Position, die Parameternamen im Bestand sind beliebig
(`"?"`, `"@p"`, `"@id"`) — der Übersetzer wertet Namen **nicht** aus, sondern nummeriert strikt
nach Reihenfolge.

### 2.1 Verbindung und PRAGMAs

```csharp
private static SqliteConnection OeffneVerbindung()
{
    var conn = new SqliteConnection($"Data Source={GetDBPath()}");
    conn.Open();
    using (var cmd = conn.CreateCommand())
    {
        // foreign_keys und busy_timeout gelten JE VERBINDUNG — ohne die erste Zeile
        // sind alle 90 Fremdschluessel wirkungslose Dekoration (Rev. 2, 5.3).
        cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
        cmd.ExecuteNonQuery();
    }
    return conn;
}
```

`journal_mode=WAL` ist dateipersistent und wird einmalig vom Migrator gesetzt. Das
Verbindungspooling von Microsoft.Data.Sqlite bleibt an; die PRAGMAs je `Open()` sind billig und
idempotent. Vorbereitete Statements cachet Microsoft.Data.Sqlite je Verbindung selbst — die
Einfügeschleifen der Ganglinien bleiben ohne weiteres Zutun schnell.

**Befund aus S3 (02.09.):** Microsoft.Data.Sqlite schaltet Fremdschlüssel beim Öffnen **von sich
aus EIN** — anders als SQLite nativ (Standard AUS). Die explizite `PRAGMA foreign_keys = ON`-Zeile
bleibt trotzdem stehen (dokumentiert die Absicht und schützt gegen Verhaltensänderungen der
Bibliothek); wer FKs bewusst AUS braucht (nur der Migrator beim Laden), muss `Foreign Keys=false`
in den Verbindungsstring schreiben **und** per `PRAGMA foreign_keys`-Rückfrage verifizieren — so
implementiert im EposSqliteMigrator.

### 2.2 Die ?→@pN-Übersetzung

```csharp
// Ueberspringt '…'-Textliterale (inkl. ''-Escape) und [eckige Bezeichner].
// Die Messung fand kein ? in einem SQL-Literal (inkl. Kreuzfragment-Pruefung) —
// der Schutz kostet trotzdem nichts und sichert kuenftiges SQL ab.
internal static string UebersetzeParameterzeichen(string sql)
{
    var sb = new StringBuilder(sql.Length + 16);
    int n = 0;
    for (int i = 0; i < sql.Length; i++)
    {
        char c = sql[i];
        if (c == '\'')
        {
            sb.Append(c);
            for (i++; i < sql.Length; i++)
            {
                sb.Append(sql[i]);
                if (sql[i] == '\'')
                {
                    if (i + 1 < sql.Length && sql[i + 1] == '\'') { sb.Append(sql[++i]); continue; }
                    break;
                }
            }
            continue;
        }
        if (c == '[')
        {
            sb.Append(c);
            for (i++; i < sql.Length && sql[i] != ']'; i++) sb.Append(sql[i]);
            if (i < sql.Length) sb.Append(']');
            continue;
        }
        if (c == '?') { sb.Append("@p").Append(n++); continue; }
        sb.Append(c);
    }
    return sb.ToString();
}
```

### 2.3 Wertenormalisierung beim Schreiben

```csharp
private static SqliteParameter[] UebersetzeParameter(OleDbParameter[] quelle)
{
    if (quelle == null || quelle.Length == 0) return Array.Empty<SqliteParameter>();
    var ziel = new SqliteParameter[quelle.Length];
    for (int i = 0; i < quelle.Length; i++)
        ziel[i] = new SqliteParameter("@p" + i, NormalisiereWert(quelle[i].Value));
    return ziel;
}

private static object NormalisiereWert(object w)
{
    if (w == null || w == DBNull.Value) return DBNull.Value;
    if (w is bool b) return b ? 1 : 0;                       // Boolean -> INTEGER 0/1
    if (w is DateTime d)                                     // Datum -> ISO-8601-Text
        return d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    if (w is Guid g) return g.ToString();                    // Altstellen, TEXT
    if (w is decimal m) return (double)m;                    // Ziel ist immer REAL
    return w;
}
```

Das Datumsformat ist bewusst ohne Sekundenbruchteile und **identisch mit dem des Migrators** —
sonst stünden im selben Feld zwei Schreibweisen. Die exotischen `OleDbType`-Reste (Guid 2,
Decimal 2, VarBinary 1, BigInt 1, Variant 3, Empty 1) werden in S4a einzeln gesichtet.

### 2.4 GetDataTable ohne Adapter — und der Typ-Rückweg (N1)

Microsoft.Data.Sqlite bringt keinen `DataAdapter` mit; `GetDataTable` lädt künftig über den
Reader und begradigt dabei den Typ-Rückweg zentral:

```csharp
// Kern des neuen GetDataTable: Spaltentypen aus dem Reader uebernehmen,
// dabei long -> int (alle Ganzzahlspalten stammen aus Access Long/Integer = Int32);
// Namensdubletten aus Joins wie der alte Adapter entdoppeln (Name, Name1, ...).
// Werte je Zeile mit Convert in den Zieltyp heben.
```

Die Messung (675 DataRow-Konsumstellen in 162 Dateien) zeigt: Der Bestand konsumiert fast
durchgängig über `Convert.To*` (1.365 Aufrufe, 654 davon DB-nah) und ist gegen den Rückweg
weitgehend immun. **Harte Casts brechen nur an 4 Stellen** — 3 × `(int)`, 1 × `(bool)`, alle in
[GebäudeKontextMenuCtrl.cs:103–110](WindowsFormsApplication1/Controller/GebäudeKontextMenuCtrl.cs)
und [Kenndaten.cs:107](WindowsFormsApplication1/Views/Wärmepumpe/Kenndaten.cs).
`(DateTime)`-/`(decimal)`-Casts und `.Field<T>` kommen **gar nicht** vor; alle 17
Datums-Lesestellen laufen über `Convert.ToDateTime` bzw. DBNull-Test; die 7
`RowFilter`/`Select`-Ausdrücke enthalten keinen Datums- oder Boolean-Vergleich.

**Die eigentliche Bruchklasse ist still: Typ-Dispatch.** 69 Stellen verzweigen auf dem
CLR-Laufzeittyp; **46 Zweige wären nach der Umstellung tot — ohne Exception.** Drei Cluster:
(a) `DataColumn.DataType → OleDbType`-Mapper (ProjektExportImportCtrl.cs:993–998/1193–1198,
KomponentenUebernahmeCtrl.cs:1229–1234, MerkmalUebernahmeCtrl.cs:253–258,
AnlagenEindeutigkeit.cs:586–591) — wählten künftig still den falschen Parametertyp;
(b) Exportliteral-Erzeugung ProjektExportImportCtrl.cs:1048–1051 — `bool` fiele in den
Zahlzweig, `DateTime` in den Textzweig; **betrifft den Projekttransfer-Strang** (Abstimmung
nötig: nach dem Umstieg ist der Ziel-Dialekt des Exports selbst SQLite);
(c) `DublettenPruefung.Kanonisch()` (:433–439) — kanonische Schlüssel änderten sich lautlos.

**Bauentscheidung (D9): den Typ-Rückweg zentral wiederherstellen, statt 675 Stellen
anzufassen.** Der neue Lader stellt die heutigen CLR-Typen wieder her:

- **INTEGER → Int32** typgetrieben (alle Ganzzahlspalten stammen aus Access Long/Integer);
- **Boolean- und Datumsspalten über einen generierten Typkatalog** (`SchemaTypKatalog`, in S2
  aus `sql/schema` erzeugt: die 97 Boolean- und 20 Datumsspaltennamen → `Boolean`/`DateTime`) —
  der deklarierte SQLite-Typ (INTEGER/TEXT) verrät sie nicht;
- Namensdubletten aus Joins wie der alte Adapter entdoppeln (`Name`, `Name1`, …).

Das räumt in einem Griff die 4 Cast-Brüche, die 46 toten Dispatch-Zweige, die 280
`.ToString()`-Formatwechsel und die `DataType`-Mapper ab. Spalten-Aliasse (`AS x`) entziehen
sich dem Katalog — Restabdeckung liefern der begrenzte Sweep (Abschnitt 6) und die
Referenzläufe. Voraussetzung des zentralen Griffs ist der Trichter: erst wenn S4b die
Eigenverbindungen zurückgeführt hat, läuft der Konsum durch diese eine Stelle.

### 2.5 ExecuteInsertAndGetId

`SELECT @@IDENTITY` → `SELECT last_insert_rowid()` als zweites Kommando auf **derselben offenen
Verbindung** (wie heute, [DataRepository.cs:306](WindowsFormsApplication1/Allgemein/DataRepository.cs)).
Signatur bleibt ohne `params` (einzige der sechs Methoden — 7 Aufrufstellen verlassen sich darauf).

### 2.6 DbVorgang statt Verbindungs-Tupel

`BeginTransaction()` leckt heute `(OleDbConnection, OleDbTransaction)` an 18 Dateien; 13 weitere
führen Transaktionen auf selbst geöffneten Verbindungen. Beide Gruppen (31 Dateien) wandern auf:

```csharp
public sealed class DbVorgang : IDisposable
{
    public int Ausfuehren(string sql, params OleDbParameter[] p);   // ExecuteNonQuery
    public object Skalar(string sql, params OleDbParameter[] p);    // ExecuteScalar
    public int EinfuegenUndId(string sql, OleDbParameter[] p);      // + last_insert_rowid()
    public void Commit();
    public void Rollback();
    public void Dispose();   // ohne Commit -> Rollback; raeumt Transaktion UND Verbindung ab
}
```

Damit verschwinden nebenbei die uneinheitlichen Aufräummuster (die Mehrzahl der Stellen entsorgt
die Transaktion heute nie; vollständig macht es nur `ProjektDuplizierenCtrl.cs:326–331`).
Die drei Tupel-Zugriffe über `Item1/Item2` (ProjektExportImportCtrl 129/377,
ProjektDuplizierenCtrl 237) werden dabei mit umgestellt.

### 2.7 Schema-Auskunft (Ersatz für GetOleDbSchemaTable, N2)

```csharp
public static bool TabelleVorhanden(string name);                  // sqlite_master
public static bool SpalteVorhanden(string tabelle, string spalte); // PRAGMA table_info
public static List<string> SpaltenVonTabelle(string tabelle);      // PRAGMA table_info
public static DataTable IndexListe(string tabelle);                // PRAGMA index_list/-info
public static DataTable FremdschluesselListe(string tabelle);      // PRAGMA foreign_key_list
```

Die letzten beiden decken die `Indexes`-/`Foreign_Keys`-Rowsets in `ErgebnisCtrl` ab.

### 2.8 Startprüfung

`ProviderVorhanden()` (einziger Aufruf [Program.cs:96](WindowsFormsApplication1/Program.cs))
entfällt — es gibt keinen registrierungspflichtigen Provider mehr. An seine Stelle tritt
„Datenbankdatei vorhanden/öffenbar", sonst verlöre der Start seine einzige frühe Diagnose vor
`SchemaMigration.Ausfuehren` (Program.cs:134).

### 2.9 Sonderfälle

- **ADOX-Reseed** (N4): `UPDATE sqlite_sequence SET seq = (SELECT MAX(ID) FROM [tab]) WHERE
  name = 'tab'` (INSERT, falls Zeile fehlt — sie entsteht erst mit dem ersten AUTOINCREMENT-Insert).
- **OleDbCommandBuilder** (N5): drei Stellen auf ausdrückliche Kommandos umbauen.
- **StilleDb + 2 private Klone** (`PufferSpCtrl.StillScalar`, `WaermequelleClass.SkalarStill`):
  auf die neuen DataRepository-Methoden zurückführen — Fläche verkleinern, wie Rev. 2 für die
  Eigenverbindungen fordert.
- **RecordSet.cs** (47 echte Nutzer): baut 2 eigene Verbindungen aus `GetConnectionString()` —
  wird innen umgestellt, die öffentliche Fläche bleibt.

---

## 3. Zielschema — Bauplan zu S2

### 3.1 Schema-Generator (einmalig, intern)

`sql/tools/Erzeuge-Schema.ps1` liest die Quelle über **DAO 120** (nur DAO zeigt Autowert,
Kaskadenattribute und Memo zuverlässig — die OLE-DB-Sicht hat nachweislich Memo verschluckt,
Rev. 2 Abschnitt 2.2):

- Typabbildung nach Rev. 2 Abschnitt 5.1; `STRICT`; Autowert-Erkennung über `dbAutoIncrField`,
  **nie** über den Spaltennamen (`StammID`, `ID_Klimadaten`, `ID_Klimaregion`!)
- `DEFAULT`-Literale über Abbildungstabelle (`0` → 0, `No`/`False` → 0, `""`, `"%"`, `50`;
  `Null` → kein DEFAULT)
- Boolean: `INTEGER NOT NULL DEFAULT 0 CHECK (x IN (0,1))` (D3-a; der CHECK lässt eine
  vergessene −1→1-Wandlung sofort auffliegen — in `sqlite-probe\` Übung 2 vorgeführt)
- Textlängen < 255: `CHECK (length(x) <= n)` (D2, 90 Spalten); die 171 × `TEXT(255)` ohne CHECK
- Relationen als `FOREIGN KEY` mit `ON UPDATE/DELETE CASCADE` nach DAO-Attributen —
  **88 echte** (2 der 90 DAO-Relations sind `MSysNavPaneGroup*`-Systembeziehungen und entfallen)
- Indizes: Entdoppelung nach **Spaltenliste** (identische Spaltenfolge = ein Index, UNIQUE
  gewinnt; zusätzlich PK-deckungsgleiche überspringen), Rest als `CREATE [UNIQUE] INDEX` unter
  Originalnamen; bei globaler Namenskollision `tabelle_name` (SQLite-Indexnamen sind DB-weit)
- PK-Politik **D10 (AutowertBevorzugen):** der Autowert wird alleiniger
  `INTEGER PRIMARY KEY AUTOINCREMENT`; übrige Alt-PK-Spalten behalten `NOT NULL`
- 3 PK-Ergänzungen: `Tab_BHKW`, `Tab_DBTagVDaten_STAMM`, `Tab_Stromverbrauchertyp_STAMM`
  (vorhandene Autowert-`ID` wird PK)
- Nachschlagefelder: DAO-Lookup-Eigenschaften mit ausgeben → Rest-S1-Sichtung nebenbei erledigt
- zusätzlich generiert: **`SchemaTypKatalog`** (C#-Datei — die Namen der 97 Boolean- und 20
  Datumsspalten, dedupliziert 61/11; typmehrdeutige Namen wie `Heizstab` bewusst ausgenommen)
  für die zentrale Typangleichung in `GetDataTable` (2.4)

Ausgabe `sql/schema/001_grundschema.sql` · `002_views.sql` · `003_indizes_fk.sql` — generiert,
von Hand kuratiert, ab S4-Beginn eingefroren. Nach dem Einheitenbruch (Schritt 62) ist der
Generator billig neu ausführbar; **Quelle der Generierung ist der Bestand von Rechner 2 oder
dieser Rechner NACH S0** (identischer Schemastand 61 vorausgesetzt — der Generator prüft das).

### 3.2 Die 14 Views

12 QueryDefs übernehmen ihren Access-SQL-Text unverändert (Klammer-Joins und `[eckige Klammern]`
sind SQLite-gültig — in `sqlite-probe\` Übung 6 vorgeführt); je View Abnahme durch Ausführung.
Die beiden `IIf`-Abfragen werden übersetzt — Originaltexte wörtlich aus der Schemapflege erhoben:

```sql
-- 002_views.sql — Abfrage_Energietraeger_Effektiv (Original: CREATE VIEW, SchemaMigration.cs:6344-6351)
CREATE VIEW [Abfrage_Energietraeger_Effektiv] AS
SELECT s.ID_Projekt, s.[ID_Energieträger] AS carrier_id,
       ec.code, ec.name, ec.billing_unit,
       CASE WHEN s.custom_hi IS NULL OR s.custom_hi = 0
            THEN ec.hi_kwh_per_unit ELSE s.custom_hi END AS eff_hi,
       CASE WHEN s.custom_hs IS NULL OR s.custom_hs = 0
            THEN ec.hs_kwh_per_unit ELSE s.custom_hs END AS eff_hs
FROM energy_project_settings AS s
     INNER JOIN energy_carrier AS ec ON s.[ID_Energieträger] = ec.id;
```

```sql
-- 002_views.sql — Abfrage_Kostenfaktoren
-- Original: CREATE PROCEDURE (ACE laesst kein ORDER BY in Views zu; SQLite schon,
-- der Umweg entfaellt). Der IIf-Ausdruck steht im ORDER BY ausgeschrieben — unter ACE
-- noetig (Alias gilt dort als ungebundener Parameter), unter SQLite beibehalten,
-- damit der Text 1:1 dem Original entspricht.
CREATE VIEW [Abfrage_Kostenfaktoren] AS
SELECT w.ID, w.ProjektID, w.StammID, w.KategorieID,
       CASE w.KategorieID WHEN 1 THEN 'Investitionskosten'
                          WHEN 2 THEN 'Betriebskosten'
                          WHEN 3 THEN 'Energiekosten' ELSE '' END AS KategorieName,
       k.Komponente, f.Bezeichnung, w.Gruppe, w.EingegebenerWert, w.WorstCase,
       w.BestCase, w.Nutzungsdauer, w.WorstCase_Nutzungsdauer,
       w.BestCase_Nutzungsdauer, w.Einheit, f.IsMainComponent
FROM (Tab_ProjektWerte AS w
      INNER JOIN Tab_Kostenfaktor AS f ON w.StammID = f.StammID)
     INNER JOIN Tab_KostenKomponente AS k ON w.KomponentenID = k.ID
ORDER BY f.IsMainComponent,
         CASE w.KategorieID WHEN 1 THEN 'Investitionskosten'
                            WHEN 2 THEN 'Betriebskosten'
                            WHEN 3 THEN 'Energiekosten' ELSE '' END,
         k.Komponente, w.Gruppe, f.Bezeichnung;
```

### 3.3 S2-Abnahme

1. Leere DB baut fehlerfrei aus den drei Skripten; `PRAGMA integrity_check` sauber.
2. Alle 14 Views per `SELECT` ausführbar.
3. Strukturzählung gegen das DAO-Inventar (Tabellen/Spalten/Indizes/FKs).
4. **DBeaver-ER-Diagramm** aus den 90 FKs deckungsgleich mit dem Access-Beziehungsfenster —
   das ist zugleich der Beweis für Ziel 1 (Rev. 2, Abschnitt 10).
5. `sqlite-probe`-Übungen 1–3 (STRICT, Boolean-CHECK, FK-PRAGMA) laufen gegen das echte Schema.

---

## 4. EposSqliteMigrator — Bauplan zu S3 (Neubau, R4 bestätigt)

Aufteilung: **`EposSqliteMigrator.Kern`** (Klassenbibliothek ohne App-Abhängigkeit; Schema-Skripte
als eingebettete Ressourcen → Einzel-EXE bleibt verteilbar) + **`EposSqliteMigrator`** (Konsole).
Eigene Solution im Repo — der Hauptbuild bleibt unberührt. Die App referenziert den Kern für den
Erststart-Assistenten (Abschnitt 8).

| Schritt | Aktion | Absicherung |
|---|---|---|
| 1 | `.laccdb` vorhanden → Abbruch mit Klartext | keine offene Sitzung |
| 2 | `Tab_Applikation.SchemaVersion` lesen; ≠ 61 → Abbruch „zuerst letzte Access-Fassung von EPOS-Plan starten" | die App hebt per eingefrorenem Access-Zweig; der Migrator hebt **nicht** selbst |
| 3 | Ziel aus eingebetteten `sql/schema/*.sql`; `foreign_keys=OFF`, `journal_mode=MEMORY`, `synchronous=OFF` für den Lauf | Schema aus dem Repo, nie zur Laufzeit erzeugt |
| 4 | Tabellenliste = **Whitelist aus dem Zielschema**, nicht `TableDefs` der Quelle. Quelltabellen außerhalb (ODBC-Verknüpfungen, `MSys*`, Access-Artefakte wie „Einfügefehler") → namentlich „nicht migriert" im Bericht; fehlende Schematabelle in der Quelle → Abbruch | löst die `sqlite_sequence`-Kollision (Rev. 2, 2.4) strukturell — auch für Bestände, deren Verknüpfungen niemand vorher bereinigt hat (dieser Rechner!) |
| 5 | Je Tabelle: `SELECT *` per Reader → vorbereitetes INSERT, eine Transaktion je Tabelle. Wandlung **typgetrieben**: Boolean → 0/1 (−1→1, NULL→0), DateTime → ISO-8601. Danach `sqlite_sequence` je Autowert-Tabelle auf MAX(ID) | 1,57 Mio. Zeilen in Sekunden; der Boolean-CHECK im Schema fängt jede vergessene Wandlung |
| 6 | `PRAGMA foreign_key_check` + `PRAGMA integrity_check`; `orphanPolicy` = `Abbruch` \| `AlsProtokollAussetzen` | Klarstellung gegenüber Rev. 2: „aussetzen" = Verletzung bleibt in der Datei und steht **namentlich im Bericht**; der Constraint wird nicht entfernt. Die bekannten Waisen `Tab_Energieanlagen → Tab_Heizkessel` sind **Textverknüpfungen**, keine der 90 FK-Beziehungen — `foreign_key_check` sieht sie nicht, sie wandern verhaltensgleich mit (Phase 2) |
| 7 | **Beweis:** Zeilenzahl je Tabelle Quelle=Ziel (Sollzahlen aus der Quelle gemessen, nicht aus Rev. 2); Inhaltsprüfsumme über sortierte, kanonisch formatierte Zeilen (Double im „R"-Format, Boolean 0/1, Datum ISO — identische Kanonisierung beidseitig); Stichproben `Tab_Projekt`, `Tab_Kenndaten`, `Tab_Energieanlagen`. Dazu der **Case-Drift-Messlauf** (D5): je Textschlüssel `GROUP BY lower(spalte) HAVING COUNT(DISTINCT spalte) > 1` über die Katalog-Namensspalten (aus `KatalogRegistry.Alle`), `Tab_Projekt.Projektname`, `emissionsart.kuerzel`, `energy_carrier.code` und das Paar `Tab_Pufferspeicher.Bezeichner`/`Tab_Energieanlagen.Bezeichner` — leerer Befund = BINARY beweisbar folgenlos für diesen Bestand | Bericht als Datei `Migrationsbericht_<quelle>_<zeit>.md`, Exitcode |
| 8 | Abschluss: `journal_mode=WAL`, `synchronous=NORMAL`; bei Fehler Zieldatei löschen | die `.accdb` wird nie verändert — sie **ist** das Rollback |

CLI: `EposSqliteMigrator --quelle <pfad.accdb> --ziel <pfad.sqlite> [--orphanPolicy …]
[--bericht <pfad>]`. S0–S3 bleiben damit, wie in Rev. 2 festgehalten, **ohne jede Änderung an
der Anwendung risikofrei vorziehbar**.

---

## 5. Schemapflege — Bauplan zu S6

Gemessen (statt vermutet) sieht der Mechanismus so aus: Start `Program.cs:134` →
`SchemaMigration.Ausfuehren` → `SchritteAbarbeiten` → Marker `Tab_Applikation.SchemaVersion`
(gelesen über eine eigene stille Verbindung, `ApplikationCtrl.cs:88–141`) → Schleife über
`SCHRITTE` mit „Nr ≤ Version → bereits erledigt", Marker-Anhebung **einzeln nach Erfolg**,
Abbruch beim ersten Fehler. Zweites Tor: `SimulationGesperrt` an 4 Stellen. Dieses Gerüst bleibt
**unverändert** — nur die DDL-Sprache und die Proben wechseln:

1. **Access-Zweig einfrieren** (Schritte 1–61). Er bleibt im Code und hat genau noch eine
   Aufgabe: Kundenbestände vor der Erstmigration über OleDb auf Stand 61 heben (Abschnitt 4,
   Schritt 2; Abschnitt 8). Die 14 `IIf`/3 `UCase`/15 `Trim` darin bleiben unangetastet.
2. **Erststart einer neuen DB:** nicht Schritte 1–61 nachspielen, sondern Grundschema aus
   `sql/schema/*.sql` einspielen und `SchemaVersion := 61` setzen.
3. **Neue Schritte (ab 62) in SQLite-DDL** im selben Marker-/Idempotenzmuster, aber mit
   **Vorabproben statt Fehlertext-Deutung**: Der heutige `Ddl`-Helfer wertet
   „bereits vorhanden" über **deutsche ACE-Meldungstexte** aus (`IstBereitsVorhanden`,
   SchemaMigration.cs:13498 — `OleDbException.Errors` ist unter .NET 8 leer, die Jet-Codes
   laufen ins Leere). Das trägt unter SQLite nicht (englische Texte, andere Codes) und war
   schon immer der zerbrechlichste Punkt. Neu: `CREATE TABLE/INDEX IF NOT EXISTS`;
   `ADD COLUMN` nach `PRAGMA table_info`-Probe (SQLite kennt kein `ADD COLUMN IF NOT EXISTS`).
4. **Proben-Ersatz formgleich:** `TabellenSchema`/`SpalteVorhanden` (heute
   `SELECT TOP 1` + `adapter.FillSchema`) → `PRAGMA table_info`; `AbfrageVorhanden`/
   `SchemaZeilen` (heute OleDb-Rowsets `Tables`/`Procedures`) → `sqlite_master WHERE name = ?`
   mit `type`-Unterscheidung. `MSysObjects` wird nirgends verwendet — bestätigt.
5. **Grenze des Musters, ehrlich benannt:** Die Messung zeigt 14 × `ADD CONSTRAINT … FOREIGN KEY`
   und 1 × `DROP CONSTRAINT` in der Historie — **so etwas kann SQLite nicht per `ALTER TABLE`**.
   Die vorhandenen FKs stecken künftig im Grundschema; braucht ein **künftiger** Schritt einen
   neuen FK oder eine Spaltenänderung, gilt das dokumentierte Tabellenneubau-Rezept
   (12 Schritte nach SQLite-Handbuch) als Helfer im SQLite-Zweig. Für `ADD COLUMN`-Schritte —
   der Regelfall — bleibt alles einfach.
6. **Selbstheilungs-DDL der Controller (N3, 29 Stellen / 16 Dateien)** wandert auf dieselben
   Helfer (`SpalteVorhanden` + `Ddl`) — Arbeitspaket S4d, nicht Teil des Einfrierens!

---

## 6. Dialekt-Sweep — Bauplan zu S5 (mit gemessenen Listen)

| Baustelle | Umfang gemessen | Maßnahme |
|---|---|---|
| `SELECT TOP 1` | **37 ausführbare Stellen** (alle `TOP 1`; Liste im Prüfrezept — Schwerpunkte ErgebnisCtrl 7, WirtschaftlichkeitCtrl 4, SchemaMigration 3*, PufferSpCtrl 3, WaermequelleClass 3) | → `LIMIT 1`. *Die SchemaMigration-Stellen nur, soweit sie im lebenden Code liegen (TabellenSchema wird ohnehin ersetzt, Abschnitt 5.4) |
| `SELECT @@IDENTITY` | **12 echte Stellen / 11 Dateien** | zentral in `ExecuteInsertAndGetId`; die übrigen auf `DbVorgang.EinfuegenUndId` bzw. `ExecuteInsertAndGetId`. Die 2 Altfehler (BrauchwasserCtrl:87, HeizkesselCtrl:99 — frische Verbindung!) dabei beheben |
| Boolean-Literale `= TRUE/FALSE` | 44 Zeilen, davon **20 im Laufzeitcode** | SQLite kennt `TRUE`/`FALSE` als 1/0 → laufen nach der −1→1-Wandlung. Je Stelle im Rezept abhaken (Vorsicht Schreibvarianten `= true`, `=false`) |
| `IIf` im Laufzeitcode | **3** (Ladeordnung:77, EmissionskatalogCtrl:266, ProjektDuplizierenCtrl:728) | SQLite `iif()` ≥ 3.32 vorhanden → voraussichtlich unverändert; durch Ausführung beweisen. EmissionskatalogCtrl:266 ist ohnehin ein Access-Boolean-Workaround (−1) — unter 0/1 nachprüfen |
| ` LIKE ` | **20 SQL-Stellen** (+2 RowFilter, DB-unberührt) | 17 sind `Like '%'`-Allesfilter, 3 ASCII-Muster (SchemaMigration) — SQLite-`LIKE` ist per Vorgabe ASCII-case-insensitiv und kollationsunabhängig → verhaltensgleich; je Stelle abhaken. `PufferSpFilter.cs:47` behandelt NULL bereits ausdrücklich |
| Typ-Rückweg (N1) | **4 Cast-Stellen** (GebäudeKontextMenuCtrl 103/105/110, Kenndaten 107) + **3 Dispatch-Cluster** (OleDbType-Mapper in 4 Dateien; Exportliterale ProjektExportImportCtrl 1048–1051; DublettenPruefung.Kanonisch 433–439) | Casts auf `Convert.To…`; die Dispatch-Cluster brauchen ohnehin die Hand, weil `OleDbType` als Konzept entfällt; alles Übrige erledigt die zentrale Typangleichung (2.4) |
| `GetMaxID`-Bestand | 40 Stellen / 27 Dateien (`+1` beim Aufrufer) | läuft unverändert (`SELECT MAX(…)` ist SQLite-gültig); `AUTOINCREMENT` sichert die Monotonie zusätzlich ab. Nur ProjektPhotovoltaikCtrl:211 (ohne `+1`) gesondert sichten |

Abarbeitung als Checkliste mit Vollständigkeitsnachweis in `sql/MIGRATION_Pruefrezepte.md`;
die Messskripte dieser Verifikation (Literal-Extraktion) werden nach `sql/tools/` übernommen —
sie sind die Rezepte, mit denen jeder Sweep seine Vollständigkeit beweist.

---

## 7. Validierung — Bauplan zu S7

1. **Struktur- und Datenbeweis** liefert der Migrationsbericht (Abschnitt 4, Schritte 6–7).
2. **Verhaltensbeweis:** Referenzläufe auf beiden Backends mit der vorhandenen Suite
   [`Referenzlauf\`](Referenzlauf/) (Konsole, kopiert die DB in eine Arbeitskopie und biegt den
   App-Pfad um). Nötige kleine Erweiterung: `DbUmgebung` kennt bisher nur fest
   `Kenndaten.accdb` (+ `.laccdb`-Logik, 2 eigene `OleDbCommand`, 1 `SELECT TOP`) → Endung
   `.sqlite` beibringen. **Frisches Referenzpaar unmittelbar vor der Umstellung erzeugen** —
   die Basis `2026-08-29_Booster` (13 Projekte) ist wegen Datendrift seit 29.08. nur Zusatz;
   vorab prüfen, ob alle 13 Referenzprojekte im jeweiligen Bestand noch existieren (dieser
   Rechner: 26 Projekte). Double → `REAL` ist bitgleich; **jede Abweichung ist ein Befund.**
3. **Bedienbeweis:** Projekt öffnen/duplizieren/rechnen/Bericht; ausdrücklich die 37
   `LIMIT`-Stellen, 12 Identity-Stellen, die Boolean-Vergleiche, alle 14 Views, der
   Projektimport (Reseed!) und die Katalogdialog-Filter (LIKE).
4. **Frontend-Beweis:** Access + sqliteodbc (Datenblatt auf drei Tabellen mit Boolean-Spalten,
   eine QBE-Abfrage über zwei verknüpfte Tabellen) — Treiber-Bitness passend zu Office x64.
5. **Kein Parallelbetrieb als Netz** (Providerbruch, Rev. 2 Abschnitt 9): Der Vergleich läuft
   Access-Build (Freeze-Stand) gegen SQLite-Build auf demselben Bestand.

---

## 8. Verteilung und Erststart — Bauplan zu S8

| Thema | Lösung |
|---|---|
| Pakete | `Microsoft.Data.Sqlite` (zieht `SQLitePCLRaw` mit nativer x64-Bibliothek); `System.Data.OleDb` **bleibt** (Parameterträger + eingefrorener Access-Zweig für die Alt-Hebung) |
| Erststart | `Kenndaten.sqlite` fehlt & `Kenndaten.accdb` vorhanden → Assistent: Access-Zweig hebt auf 61 → `EposSqliteMigrator.Kern` migriert mit Fortschritt und Bericht → Alt-DB wird `Kenndaten.vor-sqlite.accdb` |
| Settings-Fixup (N7) | gespeicherter User-Scope-Wert `DBName = *.accdb` wird beim Erststart einmalig auf `Kenndaten.sqlite` umgestellt und gespeichert; neuer Default ebenso |
| Sicherung | Dateikopie bei geschlossener App genügt (WAL wird beim letzten Schließen eingecheckpointet); im Betrieb `VACUUM INTO`. Das `DB-Backup/`-Verfahren trägt unverändert — künftig ohne 90-MB-`.accdb`-Stände im Repo |
| Zwei Windows-Konten | WAL + `busy_timeout`; die `icacls`-Zeile aus [`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md) bleibt nötig |
| Doku | `BETRIEB_SQLITE.md` (Sicherung, Werkzeuge, WAL-Dateien `-wal`/`-shm`, Wiederherstellung) |

---

## 9. Arbeitspakete, Reihenfolge, Aufwand

| Paket | Inhalt (Dateilisten aus der Messung) | Beweis | PT |
|---|---|---|---|
| **S0** | **Je Rechner:** auf diesem Rechner Linkbereinigung der 4 ODBC-Verknüpfungen (DAO, vorher größengleiche Sicherungskopie — auf Rechner 2 bereits erledigt); Backup-Ablage klären (`ProgramData\…\DB-Backup\` ist leer, Bestände liegen im Repo); Freeze-Punkt dokumentieren | Nachweis wie Rev. 2 Abschnitt 2.4 (118→114, Beziehungen 90 unverändert, Stichproben) | 0,5 |
| **S1** | Rest: Nachschlagefelder (erledigt der Generator mit) | `sql/S1_Feinmessung.md` verweist auf dieses Dokument | 0,5 |
| **S2** | Generator + drei Schemadateien + 14 Views + `SchemaTypKatalog` + Kuration | Abnahme 3.3 (u. a. DBeaver-ER = Beziehungsfenster) | 2–3 |
| **S3** | Migrator Kern+Konsole, Whitelist, Prüfsummen, Case-Drift-Messlauf, Bericht | Echtläufe auf **beiden** Beständen (Rechner 1 + 2): 114 Tabellen, 0 Differenzen | 3–5 |
| **S4a** | DataRepository-Übersetzer: 2.1–2.5, 2.8; Program.cs-Startprüfung | Build grün; Unit-Proben für ?→@pN und Normalisierung | 1,5–2 |
| **S4b** | 36 Eigenverbindungs-Dateien (Liste unten) + StilleDb + 2 Klone + RecordSet innen | 0 × `new OleDbConnection` außerhalb DataRepository | 2–3 |
| **S4c** | Schema-Auskunft (2.7) + Umbau der ~24 `GetOleDbSchemaTable`-Stellen + ADOX-Reseed + 3 CommandBuilder | Rezept: 0 Treffer `GetOleDbSchemaTable`/`ADOX`/`CommandBuilder` | 1,5–2 |
| **S4d** | Selbstheilungs-DDL: 29 Stellen / 16 Dateien auf `SpalteVorhanden`+`Ddl`-Helfer | Rezept: 0 DDL-Literale außerhalb der Helfer | 1–1,5 |
| **S4e** | Transaktionen: 18 + 13 Dateien auf `DbVorgang` | Rezept: 0 × `BeginTransaction()` außerhalb DataRepository | 1–1,5 |
| **S5** | Sweep nach Abschnitt 6 inkl. Typ-Rückweg-Sweep und 2 Identity-Altfehlern | `sql/MIGRATION_Pruefrezepte.md` vollständig abgehakt | 2–3 |
| **S6** | Schemapflege nach Abschnitt 5 (Freeze, Erststart-Grundschema, SQLite-`Ddl`, Probenersatz) | ein neuer Testschritt 62 läuft idempotent durch | 2–3 |
| **S7** | Validierung nach Abschnitt 7 (inkl. Referenzlauf-Suite-Erweiterung) | Abnahmeprotokoll | 2–3 |
| **S8** | Erststart-Assistent, Settings-Fixup, Umbenennung, `BETRIEB_SQLITE.md` | Testinstallation auf sauberer VM; Bestandsinstallation mit gespeicherten Settings | 1–2 |
| | | **Summe** | **≈ 21–30** |

**Reihenfolge:** S0–S3 sofort und app-frei (die `.accdb` wird nie schreibend angefasst); S4a → S4b/c/d/e
(parallelisierbar, S4a ist die Voraussetzung); dann S5 → S6 → S7 → S8. **Cutover je Rechner
separat** — jeder Bestand bekommt seinen eigenen Migrationslauf und Bericht.

### Dateiliste S4b (36 Dateien, Vorkommen eigener Verbindungen)

ErgebnisCtrl 9 · PufferSpCtrl 8 · WaermequelleClass 5 · StilleDb 3 · Form_EingProzTyp 3 ·
RecordSet 2 · GeraeteWaisen 2 · WirtschaftlichkeitCtrl 2 · ApplikationCtrl 2 ·
EnergieEinheitenPruefung 2 · ProjektExportImportCtrl 2 · StromganglinieDatenCtrl 2 ·
Form_DBBHKW 2 · je 1: SchemaMigration, EmissionsBilanzRechner, GesetzKatalog, BHKWCtrl,
BHKWStammCtrl, BerichtCtrl, BrennstoffBestandteilCtrl, HeizkesselStammCtrl, KostenPositionCtrl,
QuellprofilCtrl, SolardatenCtrl, SolarganglinieDatenCtrl, StromAufschlagCtrl,
StromspeicherVarianteCtrl, VariantenCtrl, WPCtrl, WPStammCtrl, Z_ProjektGebGanglinieCtrl,
Form_BHKWEing, Form_Heizkessel, Form_Heizkessel_einlesen, Form_KostenfaktorItem, Form_WP.

### Dateiliste S4e (Transaktionen)

Über `DataRepository.BeginTransaction` (18): StromganglinieStammCtrl (3 ×), ErgebnisCtrl (2),
ProjektExportImportCtrl (2), SolarganglinieStammCtrl (2), WaermebedarfStammCtrl (2),
BrauchwasserStammCtrl, GebaeudeStammCtrl, KlimaregionStammCtrl, KomponentenUebernahmeCtrl,
PreisreiheCtrl, ProjektDuplizierenCtrl, ProzesswaermeStammCtrl, StromverbraucherStammCtrl,
WPCtrl, WPStammCtrl, Z_AnlageSenkeCtrl, Form_Klimadaten, Form_Kosten.
Eigene `conn.BeginTransaction()` (13): WirtschaftlichkeitCtrl, QuellprofilCtrl, SolardatenCtrl,
SolarganglinieDatenCtrl, StromganglinieDatenCtrl (2 Stellen), WPCtrl, WPStammCtrl, Form_BHKWEing,
Form_DBBHKW (2), Form_Heizkessel, Form_Heizkessel_einlesen, Form_EingProzTyp (3).

---

## 10. Entscheidungen (Fortschreibung der D-Liste aus Rev. 2)

| Nr. | Frage | Stand |
|---|---|---|
| D1 | Zielplattform | SQLite (entschieden 31.08.) |
| **D2** | Textlängen-CHECKs | **übernommen:** ja für die 90 Spalten < 255, nein für 171 × TEXT(255) |
| **D3** | Boolean | **entschieden 01.09.: `INTEGER NOT NULL DEFAULT 0 CHECK (x IN (0,1))`** — Beweis in 1.4 |
| D4 | U+FFFD-Altlast | unverändert: wandert mit, Heilung separat |
| **D5** | Kollation | **entschieden 01.09.: BINARY** + Case-Drift-Messlauf je Bestand im Migrator (leerer Befund = beweisbar folgenlos). Keine eigene Kollation → Ziel 1 (Werkzeugfreiheit) bleibt unbeschädigt |
| **D6** | Abfragen | 14 Views bestätigt (zeichengenau); `Abfrage_Kostenfaktoren` wird von PROCEDURE zu echter View |
| **D7** | Zeitpunkt | **aktualisiert:** BHKW-Wirtschaftlichkeit ist bereits Schritt 60/61; offen nur der Einheitenbruch (62). S0–S3 sofort; ob 62 vor dem Cutover als letzter Access-Schritt oder danach als erster SQLite-Schritt kommt, wird bei S4-Start entschieden — beides trägt (Generator ist wiederholbar) |
| D8 | verwaiste Verknüpfungen | auf Rechner 2 erledigt; **auf diesem Rechner offen → S0**; strukturell entschärft durch die Whitelist des Migrators |
| **D9 (neu)** | Typ-Rückweg | **entschieden 01.09.: zentrale Wiederherstellung** in `GetDataTable` (INTEGER→Int32 typgetrieben; Boolean/Datum über generierten `SchemaTypKatalog`) + begrenzter Sweep (4 Cast-Stellen, 3 Dispatch-Cluster). Beleg und Begründung in 2.4 — ein reiner Sweep müsste 675 Stellen anfassen und fände die 46 stillen Dispatch-Brüche nicht |
| **D10 (neu, aus S2)** | Verbund-PKs mit Autowert | **AutowertBevorzugen** (entschieden 01.09.): Bei den 50 Tabellen, deren Access-PK den Autowert nur enthält bzw. daneben liegt, wird der Autowert alleiniger `INTEGER PRIMARY KEY AUTOINCREMENT`; die übrigen Alt-PK-Spalten behalten `NOT NULL`. Verlustfrei: 49 Verbund-PKs sind durch den Autowert impliziert; bei `Tab_Projekt` sichert der bestehende UNIQUE-Index auf `Projektname` die Eindeutigkeit (in 003 nachgewiesen). Der Wegfall der case-insensitiven Text-PK-Schärfe geht in dieselbe permissive Richtung wie D5 |

---

## 11. Risiken (Fortschreibung)

- **R1 Kollation — entschärft:** BINARY belegt tragfähig (1.4); Restrisiko sind die 4
  Katalog-Dublettenwächter und die `energy_carrier.code`-Prüfung, die case-sensitiv werden
  (permissive Richtung, kein Datenverlust), sowie Case-Drift im Altbestand — den misst der
  Migrator je Bestand. Namens-Altpfade (`WQ_Puffer`, `Gebaeudename`) sind durch FKs abgelöst.
- **R2 kein Netz-Mehrplatz:** unverändert bewusst in Kauf genommen.
- **R3 unentdeckter Dialekt:** bleibt das größte Restrisiko (kein Parallelbetrieb). Gegenmittel
  unverändert: Literal-Rezepte (jetzt als Skripte im Repo), Referenzläufe, `FehlerMelden` mit
  `KurzSql`-Klartext (vorhanden, DataRepository.cs:231).
- **R4 Migratorbasis:** erledigt — Neubau bestätigt; die Referenzlauf-Suite existiert als
  Abnahmegerüst.
- **R5 (neu) Zwei-Bestände-Lage:** Rechner 1 und 2 tragen verschiedene Datenstände; Rev.-2-Zahlen
  gelten nur für Rechner 2. Gegenmittel: bestandsspezifische Sollzahlen im Migrator, S0 je
  Rechner, Cutover je Rechner mit eigenem Bericht.
- **R6 (neu) Fehlertext-Idempotenz:** Die deutsche ACE-Meldungstext-Deutung der Schemapflege
  trägt unter SQLite nicht — im SQLite-Zweig konstruktiv ersetzt durch Vorabproben/IF NOT EXISTS
  (Abschnitt 5.3). Im eingefrorenen Access-Zweig bleibt sie und bleibt dort funktionsfähig.

---

## 12. Arbeitsregeln für die Umsetzung

1. **cp1252-Falle:** 68 der 569 `.cs`-Dateien sind BOM-loses cp1252 (Umlaute!) —
   `SchemaMigration.cs` ist dagegen bewusst UTF-8. Vor jedem Edit Kodierung prüfen, sonst
   werden Umlaute zerstört.
2. **Build:** ausschließlich VS-2022-MSBuild, `/p:Platform=x64`; laufende App **und** offenes
   Visual Studio sperren den Ausgabeordner (MSB3027 nennt die Sperrer). Die EPOS-Plan-Instanz
   lief zuletzt seit 31.08. durch — vor Buildläufen schließen.
3. **Sync-Automatik:** committet und pusht periodisch alles im Repo — keine halbfertigen
   Stände im Arbeitsbaum liegen lassen; Arbeitspakete in einem Zug fertigstellen. Branch für
   diesen Strang: `sqlite` (existiert, Stand = `main`).
4. **ACE-Parameter-Falle** (für die Übergangszeit, solange der Access-Zweig läuft): Parameter +
   Unterabfrage bindet still falsch; Fehler nur am deutschen Text erkennbar.

---

*Messprotokoll: Branch `sqlite` @ `7d41833` (01.09.2026). Sechs Messläufe — Zugriffsschicht
(36/67 Verbindungen, 18+13 Transaktionsdateien, 12 echte @@IDENTITY, 40 GetMaxID-Stellen, 24
GetOleDbSchemaTable, ADOX/CommandBuilder-Funde); Literal-Inventur (569 Dateien, 39.179 Literale,
0 × `?` in SQL-Literalen inkl. Kreuzfragment-Prüfung, 37 TOP, 44 Boolean-Zeilen, 20+2 LIKE,
17 IIf, Abfragen-Tabelle zeichengenau); Schemapflege (Zeilen exakt, ZIEL_VERSION 61,
View-Inventar 5/5, beide View-SQLs wörtlich, Idempotenzmuster, 29 DDL-Stellen außerhalb);
D3/R1-Feinmessung (261 DBNull-Zuweisungen klassifiziert, 15 GetIdByName-Aufrufer, 234
Textschlüssel-WHEREs, Kollationsurteil); Umfeld (DAO-Spotcheck strikt lesend: 118 TableDefs,
davon 4 verknüpft, Relations 90, QueryDefs 17; R4-Suche; Referenzläufe; csproj/Settings);
Typ-Rückweg (675 DataRow-Konsumstellen in 162 Dateien, 4 brechende Casts, 69
Typ-Dispatch-Stellen mit 46 toten Zweigen, 0 × `Field<T>`, 0 × `(DateTime)`-Cast, 17 saubere
Datums-Lesestellen). Die Live-DB wurde ausschließlich lesend geöffnet; am Repo wurde nichts
verändert.*
