# Protokoll — Konstruktorzeilen in der Datenbank, redundanter Index weg (ZU25)

**Gegenstand.** Der Anwenderentscheid ZU25: die Zeilen des Bedarfstag-Konstruktors gehören in die
Datenbank, und der redundante Index auf `Tab_TwwMessreihe.ID_Projekt` gehört weg — **ein**
Schemaschritt für beides. Dazu die zwei liegen gebliebenen Pflegereste aus N19: der fehlende
Archivfall des Katalogimports und die Namenstafel der Größen der Karte „Herkunft".

**Rahmen.** Worktree `zk`, Zweig `zk` von `4fdda0c2` (= `origin/ios_migration_september`), eine
Sitzung mit `model: opus`. **Schemaschritt 145** (T5 „Konstruktor"); Zielversion vorher 144
(Nachtzeit je Gebäude, E43). Referenzbasis `2026-09-25_R19_BhkwNetzbezug`, CI-Projekte 1030, 1007,
1017, 1045, 1046, 1047.

Die ausführliche Fassung mit Regeln, Abweichungen und Folgen steht als **Nachtrag N21** im
[Umsetzungskonzept Zapfprofilgenerator](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md);
dieses Protokoll hält den Ablauf und die Nachweise.

## 1 Der Anwenderentscheid

| Nr. | Frage | Entscheid (N19) | Umsetzung |
|---|---|---|---|
| ZU25 | Konstruktorzeilen des Bedarfstags (N13 (p)) und der redundante Index (N15 (a)): je ein Schemaschritt oder einer für beide? | **ein** Schemaschritt für beide, nach der Sichtabnahme Z1–Z5 | Schemaschritt **145** `SCHRITT_145_ZAPFPROFIL_KONSTRUKTOR`: `Tab_TwwKonstruktorzeile` **und** `DROP INDEX` im selben Schritt |

## 2 Ablauf

1. **Schemaschritt 145** (`edcfb4a1`): `TwwSchema.SCHRITT_T5_KONSTRUKTOR` (=
   `NachtzeitSchema.SCHRITT` + 1), `SQL_CREATE_KONSTRUKTORZEILE`, `AnweisungenT5Konstruktor`,
   `AufraeumenT5Index`, `IndexT4MessreiheProjekt`; `SchemaStand.Zielversion` symbolisch darauf;
   `SchemaMigration.Schritt_145_ZapfprofilKonstruktor` (nur `SqliteDdl`, wiederholbar);
   `Werkzeuge/Testdatenbankschema` mit eigenem Block hinter der Nachtzeit; Projektkopie und Transfer
   (`FK_MAP`, `KINDER`), Auslieferungsvorlage (leeren, Bericht, Prüfposten „5c"); Testdatenbank neu
   migriert; `Referenzlaeufe/LIESMICH.md` (Schemastand und Nachtrag).
2. **Speichern und Laden** (`28791aa4`): `ZapfprofilCtrl.Speichern` schreibt die Zeilen ersetzend am
   Auslegungssatz, `Lies` und das neue `Konstruktorzeilen(int)` geben sie zurück; die Hülle führt sie
   über `ZapfprofilAuslegungEingabeDaten.Konstruktorzeilen` an den Konstruktor — mit Entwurf und
   ohne; Razor-Startzeilen und `KonstruktorFertig` nachgezogen; Wiki-Quelle der Seite
   „Brauchwasser-Zapfprofil".
3. **Pflegerest Archivfall** (`b3ad534e`): ein Fall in `TwwKatalogimportTests`, der beide Grenzen von
   `PaketLesen` als Parameter an einem Archiv mit zwei Einträgen je 60 Byte messt. Kein Produktcode.
4. **Pflegerest Namenstafel** (`bbc965d3`): zwölf Größen der Auslegung als Konstanten in `ZapfFeld`
   (damit zweiundvierzig), Namenstafel Größe → `ZPG_GROESSE_<KONSTANTE>` über Reflexion,
   `ZapfprofilHuelle.Groessenname` mit benanntem Rückfall, 42 Schlüssel in beiden Sprachen, Designer
   neu; Wache `ZapfprofilGroessennamenWacheTests`.
5. **Papiere** (`1fe767db`): Kapitel 9 (ZU25 umgesetzt), Nachtrag N21, dieses Protokoll,
   Statuszeile #522, Dokumentations-Index, Wiki-Logbuchsatz, Übergabe Abschnitt 15.
6. **Merge von `origin`** (`57859187`, Stand `72212716` — die Posten #525 und #526): zwei Konflikte,
   beide in Papieren. Statusdatei — die fortgeschriebene Zeile #521 von `origin`, dahinter #522, dann
   #525 und #526; die Reihenfolge von `origin` bleibt unangetastet. Dokumentations-Index —
   Protokollzahl Zapfprofilgenerator 12 (diese Welle), Gebäudesimulation 10 (`origin`, G6a). Die
   beiden `.resx` führten sich selbst zusammen (beide Seiten hängen am Ende an), der Designer ist neu
   erzeugt und wiederholbar. Die Testdatenbank kommt unverändert von hier (LFS `cba0aa41`,
   Schemastand 145) — `origin` hat sie nicht angefasst; `Zielversion` bleibt
   `TwwSchema.SCHRITT_T5_KONSTRUKTOR`, die Schrittliste zählt 85 Schritte. Auf `origin` sind #522,
   #523 und #524 frei — die Nummer bleibt.
7. **Zweiter Merge von `origin`** (`983b3aee`, Stand `17728b80` — der Posten #527: Klimaregion im
   Projektassistenten, Klassenhinweis des Gebäudeimports): **ohne Konflikt**; Zielversion,
   Statuszeile #522 und Testdatenbank unberührt, Designer unverändert. Build, Windows-Schale,
   gefilterte Tests (0 Fehler, 1 765 erfolgreich) und Referenzlauf (6/6 PASS) danach wiederholt.

## 3 Gates

| Gate | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler (vor und nach dem Merge) |
| gefilterte Tests (Schema, Migration, Tww, Zapfprofil, Konstruktor, Katalogimport, Auslieferung, Vorlage, KiMasken, Hüllen, Doku- und Wiki-Wachen, Repositoryordnung, Größennamen, Zonen) | 0 Fehler, 1 762 vor dem Merge, **1 943** danach |
| `dotnet test WP-Plan.Kern.slnf -c Release` (voller Lauf, xUnit seriell) | 0 Fehler, **15 042 erfolgreich**, 2 übersprungen (EPOS.Kern.Tests 7 630, EPOS.UI.Tests 6 450, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27); vor dem Merge 14 990 |
| Windows-Schale `-p:EnableWindowsTargeting=true` | 0 Fehler (vor und nach dem Merge) |
| `Werkzeuge/SqlDialektPruefer` gegen `Kenndaten_Test.sqlite` | 1 946 SQL-Texte, **0 Fundstellen** (374 dynamisch) |
| `Werkzeuge/Auslieferungsvorlage.Tests` | 0 Fehler, 36 erfolgreich (vor und nach dem Merge) |
| Referenzlauf 1030, 1007, 1017, 1045, 1046, 1047 gegen `2026-09-25_R19_BhkwNetzbezug` | **6/6 PASS** (198 CSV, 2 208 587 Werte) — vor und nach dem Merge; nach dem Merge nötig, weil er die Gebäudeseite berührt (G6a) |
| `ResourceDesigner` (nur prüfen) | unverändert, wiederholbar |
| Arbeitsbaum | sauber, keine Konfliktmarker |

## 4 Die Testdatenbank

Aus der origin-Fassung (LFS `b68638da…`, Schemastand 144) mit
`dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`
migriert. Befund:

- **Schemastand 145**, STRICT-Tabellen 144 → **145**, `integrity_check` ok, `foreign_key_check` leer;
- **Zellvergleich aller Tabellen gegen die Vorfassung: genau zwei Unterschiede** — der Marker
  `Tab_Applikation.SchemaVersion` 144 → 145 und die neue, **leere** `Tab_TwwKonstruktorzeile`. Kein
  `CREATE`-Text einer bestehenden Tabelle, Sicht oder Trigger geändert, **keine Zeile entfernt,
  hinzugefügt oder geändert**; alle 27 Projekte samt dem Prüfprojekt ohne Referenzrolle stehen Zelle
  für Zelle;
- **Indizes:** `Tab_TwwMessreihe_ID_Projekt` fällt weg, der UNIQUE-Index der neuen Tabelle kommt
  hinzu; `Tab_TwwMessreihe` behält ihren UNIQUE-Index über (ID_Projekt, Bezeichnung, Zeilenindex);
- **zweiter Lauf** des Werkzeugs: 0 Tabellen, 0 Spalten (wiederholbar);
- Größe 70 590 464 Byte (`VACUUM` des Werkzeugs), LFS-SHA-256 `cba0aa41…`, mit aktivem LFS-Filter
  committet, keine `-shm`/`-wal`-Dateien.

**Keine Einfrierregel ist berührt** — reines DDL, kein Rechenweg liest eine Konstruktorzeile, und ein
Index ändert kein Ergebnis, nur den Weg dorthin.

## 5 Abweichungen von der Vorgabe des Auftrags

| Vorgabe | Umgesetzt | Grund |
|---|---|---|
| Spalten `Minute_Beginn` (0–1439), `Dauer_min`, `Energie_Kwh`/`Volumen_l` | `Beginn_h`, `Ende_h`, `Regel`, `Anzahl`, `Volumen_l`, `Zapftemperatur_C`, `Verbraucher` | Das ist, **was der Konstruktor führt**. Die Ereignisform steht schon an `Tab_TwwBedarfstagEreignis_STAMM`; aus Minuten und Energien liesse sich weder Regel noch Anzahl noch Zapftemperatur zurückrechnen — der Konstruktor wäre nicht wieder bedienbar |
| „Index auf dem Verweis" | kein eigener Index | Der UNIQUE-Index über (`ID_TwwProjekt`, `Reihenfolge`) trägt die Spalte an führender Stelle; ein zweiter wäre genau die Redundanz, die derselbe Schritt bei `Tab_TwwMessreihe` wegnimmt |
| „Neuanlage-DDL ohne den Index" | Die Anweisungen von T4 bleiben unverändert; T5 wirft den Index mit `DROP INDEX IF EXISTS` weg | Eine ausgeführte Schrittnummer wird nicht umgeschrieben (ADR-001). Der Endstand ist derselbe: Wer T4 noch vor sich hat, legt den Index an und verliert ihn im nächsten Schritt |

## 6 Offen nach diesem Schritt

- **Sichtabnahme unter Windows:** Konstruktor füllen, OK, Dialog schließen, wieder öffnen — die
  Zeilen stehen; Karte „Herkunft" mit übersetzten Größen.
- **Bezugsart und Bezugsmenge** eines gespeicherten Konstruktortags in den wieder geöffneten
  Konstruktor laden (heute beginnt er dort „ohne Bezug"); sie stehen an der Katalogzeile des Tags.
- **Wiki-Upload** des Absatzes zum Konstruktor im Sammel-Upload; Logbuch-Satz mit Version beim
  Anwender.
