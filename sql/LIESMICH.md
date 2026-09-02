# sql\ — Schema-Wahrheit und Nachweise der SQLite-Migration

Gehört zu [`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](../Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md)
(dort Abschnitte 3–6) und [`Konzept_DB-Migration_SQLite_EPOS-Plan.md`](../Konzept_DB-Migration_SQLite_EPOS-Plan.md) (Rev. 2).

| Pfad | Inhalt |
|---|---|
| `schema\001_grundschema.sql` | 114 `STRICT`-Tabellen, 88 Fremdschlüssel **inline** (SQLite kann FKs nicht nachrüsten), 80 × `INTEGER PRIMARY KEY AUTOINCREMENT`, Boolean als `INTEGER NOT NULL DEFAULT 0 CHECK (0,1)` (D3), Textlängen-CHECKs < 255 (D2), PK-Politik D10 |
| `schema\002_views.sql` | die 14 vom Code genutzten Abfragen als Views; `Abfrage_Energietraeger_Effektiv` und `Abfrage_Kostenfaktoren` von `IIf`/PROCEDURE nach `CASE WHEN`/View übersetzt |
| `schema\003_indizes_fk.sql` | 179 Indizes (spaltenlisten-entdoppelt; enthält KEINE FKs — Name historisch, Begründung im Kopfkommentar) |
| `schema\SchemaTypKatalog.g.cs` | generiert: Boolean-/Datumsspaltennamen für die zentrale Typangleichung in `DataRepository.GetDataTable` (D9); wird ab S4a per Link ins App-Projekt eingebunden |
| `schema\inventar.json` | vollständiges Strukturinventar (Whitelist + Typquelle des Migrators) |
| `schema\typkatalog.json` | Boolean-/Datumsspalten je Tabelle, exakt |
| `tools\Erzeuge-Schema.ps1` | Generator (DAO 120, strikt lesend, deterministisch — Wiederholungslauf ist byte-identisch); Laufzeit > 5 min |
| `tools\baue_leere_db.py` | Abnahme: baut die leere DB und fährt 25 Proben (STRICT, Boolean-CHECK, FK an/aus, Views, Zählungen) |
| `tools\sql_dialekt_inventur.py` | **Prüfrezept 1** (ab S5): misst Dialektmerkmale (TOP, `@@IDENTITY`, `= TRUE/FALSE`, LIKE, Access-Funktionen, `?`) in den **echten C#-String-Literalen** — Kommentare zählen nicht mit. Rein lesend |
| `tools\typ_rueckweg_scan.py` | **Prüfrezept 2** (ab S5): misst den Typ-Rückweg (harte Casts auf DB-Werte, `Convert.ToXxx`, Reader-Getter) und belegt, dass D9 keine Konsumstelle offen lässt |
| `MIGRATION_Pruefrezepte.md` | **Vollständigkeitsurkunde des Dialekt-Sweeps**: je Rezept Sollwert nach S5, Istwert und die Fundliste der bewussten Ausnahmen (SchemaMigration, GeraeteWaisen) |
| `S0_Protokoll_Rechner1_2026-09-01.md` | Linkbereinigung Rechner 1 (118 → 114, Sicherung, Nachweis) |
| `S1_Feinmessung.md` | Rest-S1: Nachschlagefelder (Befund: 0) |
| `S2_Protokoll_2026-09-01.md` | Schema-Abnahme 25/25 + Nachtrag 02.09. (K1→D10, K3→Spaltenliste) |
| `S5_Protokoll_2026-09-02.md` | Dialekt-Sweep: 26 × `TOP 1`→`LIMIT 1`, 4 Casts, Exportliteral-Dispatch auf SQLite; 58 Ausführungsnachweise gegen die Probendatenbank, 0 Fehler |
| `S3_Migrationsbericht_Rechner1_2026-09-02.md` | Echtlauf des Migrators auf Rechner 1: 114 Tabellen, 1 392 013 Zeilen, alle Prüfsummen gleich, Exit 0 |
| `S7_Protokoll_2026-09-02.md` | Verhaltensbeweis: Referenzläufe Access ↔ SQLite, nach den Fixes B1/B2 **byte-identisch** (10/10 Projekte, 234 CSV) |
| `S8_Protokoll_2026-09-02.md` | Erststart-Assistent, Settings-Fixup, `BETRIEB_SQLITE.md`; Probe **Fall 16** (16/16), beide Bauten 0 Fehler / 5 Bestandswarnungen |

**Regeln:** Ab Cutover ist `schema\*.sql` die einzige Quelle der Struktur (Rev. 2, 5.6) —
Änderungen nur über den Generator bzw. ab Schritt 62 über die SQLite-Schemapflege.
Migrierte `.sqlite`-Bestände gehören **nicht** ins Repo (Größe; die Sync-Automatik würde sie
mitpushen) — Migrationsberichte dagegen schon: sie werden als `S3_Migrationsbericht_*.md`
neben diesen Protokollen abgelegt.
