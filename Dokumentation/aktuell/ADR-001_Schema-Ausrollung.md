# ADR-001: Ausrollung von Schemaänderungen und Datenmigration in EPOS-Plan

**Status:** Angenommen (14.08.2026)
**Datum:** 12.08.2026
**Entscheider:** Dirk (Projektverantwortung EPOS-Plan)
**Betrifft:** `Konzept_Simulation_QuellenSenken.md` Fassung 11, Kapitel 5.6 und 6.6 —
diese beiden Kapitel werden durch diesen ADR ersetzt
**Berührte Pakete:** 1 (Schema + Migration), 7 (Ergebnis + Anzeigen)

---

## Kontext

Das Konzept Quellen/Senken braucht drei Arten von Datenbankänderungen an einer
**bereits beim Anwender im Einsatz befindlichen** `Kenndaten.accdb`:

| Klasse | Umfang | Heute abgedeckt durch |
|---|---|---|
| **(a) Additives DDL** | 22 neue Spalten — 15 in `Tab_Energieanlagen` (5.3), 7 in `Tab_Pufferspeicher` (5.1) | `WaermequelleClass.SchemaSicherstellen()` |
| **(b) Strukturelles DDL** | 1 `CREATE TABLE` (`Tab_ErgebnisPufferspeicher`), 1 Index, 5 Beziehungen: 3 neue FKs plus Nachrüstung `ID_PUFFER` und `Tab_Projekt → Tab_Pufferspeicher` (B0-6) | **nichts** |
| **(c) Einmaliges DML** | Projektdatenmigration `Z_ProjektPufferSp` → neues Modell (5.5), muss je Datenbank **genau einmal** laufen | **nichts** |

Kapitel 5.6 des Konzepts sah für (b) und (c) die Klasse
`Allgemein/Update/UpdateDatabaseFromScript.cs` vor. Diese Klasse ist im aktuellen
Arbeitsstand **gelöscht** — mitsamt dem gesamten Ordner `Allgemein/Update/` (letzter
Stand in Commit `6c2e4c8`, 350 Zeilen). Damit hat Paket 1 derzeit keinen Ausrollpfad,
und Paket 7 kann seine Ergebnistabelle nicht anlegen.

Die im Repo-Wurzelverzeichnis liegenden `migration.manuell.sql` und
`migration.config.json` sind **kein** Ersatz: Sie migrieren Anwenderdaten aus einer
alten Benutzerdatenbank in eine neue Versionsdatenbank (Ordner `DB_Migration` neben
dem Repo) und lösen damit ein anderes Problem — Dateiaustausch statt In-Place-Änderung.
Sie sind für dieses Vorhaben ausdrücklich ausgeschlossen.

### Kräfte, die die Entscheidung formen

1. **Schreibschutz beim Normalanwender.** `C:\ProgramData\EPOS_PLAN` erlaubt normalen
   Benutzern nur das Anlegen neuer Dateien, nicht das Ändern vorhandener. Eine vom
   Installer geschriebene `Kenndaten.accdb` ist schreibgeschützt, bis sie einmal über
   „Komprimieren und reparieren" neu geschrieben wurde. **`ALTER TABLE` kann also
   regulär fehlschlagen** — das ist kein Randfall.
2. **Stilles Scheitern ist der teuerste Ausgang.** `SchemaSicherstellen()` fängt
   Fehler heute mit `catch { Console.WriteLine(…) }` ab. In einer `WinExe` ohne
   Konsole ist das spurlos: `WertLesen` liefert danach `null`, der Anwender sieht
   Defaults statt seiner Eingaben und rechnet mit falschen Annahmen weiter.
3. **Datenmigration ist nicht über Existenzprüfung idempotent.** Ob eine Spalte
   fehlt, lässt sich prüfen. Ob ein Projekt bereits migriert wurde, nicht — ein
   Anwender darf `WS_Ziel` legitim wieder auf `Heizkreis` stellen. Ohne expliziten
   Marker überschreibt eine bei jedem Start laufende Migration Anwenderentscheidungen.
4. **Keine automatisierten Tests.** Jede Verifikation ist ein manueller Lauf unter
   Windows/x86 mit ACE 32-bit. Ein Mechanismus, der schwer zu beobachten ist, ist
   damit auch schwer zu verifizieren.
5. **Kein Versionsmarker vorhanden.** Es gibt keine `SchemaVersion` irgendwo im
   Schema. `Tab_Applikation` ist eine anwendungsweite Einzelzeilen-Statustabelle und
   wäre der natürliche Ort dafür.
6. **Kein Startzeitpunkt für DB-Arbeit.** `Program.cs` hat keine DB-Startsequenz;
   `SchemaSicherstellen()` läuft lazy aus `Form_Simulation_Config` und
   `SimulationControl`. Es gibt heute keine Stelle, an der ein Migrationslauf
   verlässlich genau einmal je Programmstart stattfindet.

---

## Entscheidung

**Eine versionierte, im Programmcode gehaltene Migration mit Schemamarker in
`Tab_Applikation.SchemaVersion`, ausgeführt einmalig beim Programmstart.**

Konkret:

- Neue Klasse `Allgemein/Update/SchemaMigration.cs` mit einem Einstiegspunkt
  `Ausfuehren(out string fehlerbericht)`, aufgerufen aus `Program.Main` **vor** dem
  Öffnen der MDI-Oberfläche.
- Die Migrationsschritte sind nummerierte C#-Methoden (`Schritt_1_SpaltenAnlagen`,
  `Schritt_2_SpaltenPuffer`, `Schritt_3_ErgebnisTabelle`, `Schritt_4_Beziehungen`,
  `Schritt_5_ProjektdatenQuellenSenken`). Jeder Schritt läuft nur, wenn
  `SchemaVersion < n`, und hebt den Marker **erst nach nachgewiesenem Erfolg** an.
- Additives DDL nutzt die vorhandene, bewährte Existenzprüfung aus
  `SchemaSicherstellen()` weiter — die Logik wird verschoben, nicht neu erfunden.
- Strukturelles DDL und die Datenmigration laufen als gewöhnliches SQL über
  `DataRepository` (und damit automatisch über `DataRepository.GetDBPath()`, womit
  der offene Punkt O6 gegenstandslos wird).
- Fehler werden **gesammelt und einmal gemeldet**; schlägt ein Schritt fehl, bleibt
  der Marker stehen und der Simulationsbereich verweigert den Start mit klarer
  Meldung, statt auf halb migriertem Schema zu rechnen.
- `WaermequelleClass.SchemaSicherstellen()` bleibt als Rückfallebene bestehen, ruft
  aber intern denselben Spaltenkatalog auf. Doppelte Wahrheit über die Spaltenliste
  wird vermieden.

---

## Betrachtete Optionen

### Option A: `UpdateDatabaseFromScript` reaktivieren

Datei aus `6c2e4c8` zurückholen, DB-Pfad von der Registry (`HKCU\…\ODBC.INI\TEST` →
`DBQ`) auf `DataRepository.GetDBPath()` umstellen, Aufrufer verdrahten, Skriptdatei
mit `SQL=`-Zeilen ausliefern. Das war die Vorgabe von Konzept 5.6.

| Dimension | Bewertung |
|---|---|
| Komplexität | Mittel — 350 Zeilen bestehender, aber unerprobter Code |
| Aufwand | 1–1,5 PT (Pfad, Aufrufer, Skript, Auslieferung) |
| Deckt (a)/(b)/(c) | ja / ja / **nur ohne Run-once-Garantie** |
| Vertrautheit | gering — die Klasse hatte im geprüften Umfang **nie einen Aufrufer** |

**Pro:** Kann durch `BACKUP_REL:` / `CLEAN_COL:` / `RESTORE_REL:` Beziehungen um
Spaltenänderungen herum sichern und wiederherstellen — genau die Fähigkeit, die
Punkt (b) mit seinen fünf Beziehungen braucht. Beliebiges SQL ohne Neucompilierung
nachschiebbar.

**Contra:** Kein Run-once-Schutz — das Skript läuft bei jedem Aufruf komplett, was
für (a) und (b) idempotent ist, für (c) aber nicht; Anwenderentscheidungen würden bei
jedem Start überschrieben. Die Skriptdatei muss ausgeliefert, gefunden und
versionsgleich zur EXE gehalten werden — eine zweite Auslieferungsspur neben der
Assembly. Sie führt außerdem genau das Muster wieder ein, das mit `migration*.sql`
gerade verworfen wurde: lose SQL-Dateien als Migrationsträger. Und der Code wurde
bewusst entfernt (bestätigt 14.08.2026); ihn zurückzuholen, widerspräche dieser
Entscheidung.

### Option B: Alles in `SchemaSicherstellen()`, ohne Versionsmarker

Den bestehenden Mechanismus um die 22 Spalten erweitern, eine Schwestermethode
`TabelleSicherstellen()` für `CREATE TABLE` ergänzen, die Datenmigration über
heuristische Erkennung („`WS_Ziel` ist überall leer → noch nicht migriert") auslösen.

| Dimension | Bewertung |
|---|---|
| Komplexität | Niedrig |
| Aufwand | 0,5 PT |
| Deckt (a)/(b)/(c) | ja / teilweise / **nein** |
| Vertrautheit | hoch — das Muster läuft seit Langem produktiv |

**Pro:** Kleinster Eingriff, kein neuer Mechanismus, bewährt und idempotent für
additives DDL. Läuft automatisch ohne Verdrahtung im Startpfad.

**Contra:** Die Heuristik für (c) ist nicht tragfähig — ein Anwender, der alle Anlagen
legitim auf `Heizkreis` zurückstellt, löst damit eine erneute Migration aus, die seine
Puffer-Zuordnungen aus `Z_ProjektPufferSp` wiederherstellt. Das ist ein stiller
Datenfehler, der genau die Anwender trifft, die bewusst konfigurieren. Beziehungen
(`ADD CONSTRAINT`) lassen sich zwar absetzen, aber ohne Sicherungs-/Wiederherstellungs-
Logik nicht gefahrlos wiederholen. Zusätzlich läuft der Mechanismus lazy und
prozessweit einmalig (`_schemaGeprueft`) — es gibt keinen definierten Zeitpunkt, an dem
ein Anwender über einen Fehlschlag informiert würde.

### Option C: Versionierte In-Code-Migration mit Schemamarker *(gewählt)*

`Tab_Applikation` erhält `SchemaVersion LONG`. Eine Klasse `SchemaMigration` führt
nummerierte Schritte in fester Reihenfolge aus, jeder genau einmal, mit
Sammelfehlerbericht und Startblockade bei Fehlschlag.

| Dimension | Bewertung |
|---|---|
| Komplexität | Mittel |
| Aufwand | 1,5–2 PT (Mechanismus + 5 Schritte) |
| Deckt (a)/(b)/(c) | ja / ja / **ja** |
| Vertrautheit | mittel — neues Muster, aber Standard und gut lesbar |

**Pro:** Einziger Weg mit echter Run-once-Semantik für die Datenmigration. Ein
Einstiegspunkt, ein Log, eine Meldung — statt heute drei Aufrufstellen, die still
scheitern. Die Migration ist im selben Commit wie der Code, der sie braucht: keine
zweite Auslieferungsspur, keine Versionsdrift zwischen EXE und Skript. Der
Schreibschutzfall wird erkennbar statt stillschweigend, und der Marker verhindert, dass
ein halb migriertes Schema als fertig gilt. Trägt später auch das Feature-Flag
`Kaskade_Zweikanalig` aus Paket 4 und jede weitere Schemaänderung.

**Contra:** Neuer Mechanismus, den es zu pflegen gilt; ein falsch gesetzter Marker ist
schwerer zu korrigieren als ein wiederholbares Skript (Gegenmittel: Marker nur nach
Erfolg setzen, und je Schritt einzeln). Nachträgliche Korrekturen brauchen einen neuen
Build — bei einer Desktop-Anwendung mit ohnehin gekoppelter Auslieferung aber kein
realer Nachteil. Erfordert eine Startsequenz in `Program.cs`, die es heute nicht gibt.

---

## Abwägung

Der Kern ist Anforderungsklasse (c). Für (a) sind alle drei Optionen gleichwertig, für
(b) liegen A und C gleichauf. Nur bei der **einmaligen Datenmigration** trennen sie
sich, und dort ist B durch die untragfähige Heuristik ausgeschieden und A dadurch, dass
ein Skript ohne Zustand nicht wissen kann, ob es schon gelaufen ist.

Zwischen A und C entscheidet die Frage, ob die Migration **Daten** oder **Code** ist.
Bei einer Desktop-Anwendung, deren Datenbank beim Anwender liegt und deren EXE
geschlossen ausgeliefert wird, gibt es keinen Gewinn durch nachschiebbares SQL: Wer
das Skript austauschen kann, kann auch die EXE austauschen. Der vermeintliche
Flexibilitätsvorteil von A ist damit hypothetisch, während sein Nachteil — eine zweite
Auslieferungsspur, die versionsgleich gehalten werden muss — bei jedem Update real
anfällt.

Die Schreibschutzsituation unter `C:\ProgramData\EPOS_PLAN` verstärkt das. Sie macht
Fehlschläge zu einem erwartbaren Normalfall, nicht zu einer Ausnahme. Ein Mechanismus
muss diesen Fall also **erkennen, melden und den Folgebetrieb blockieren** können. Das
leistet nur C, weil nur dort ein Zustand existiert, gegen den sich „nicht fertig"
überhaupt ausdrücken lässt.

Der Aufpreis von C gegenüber A beträgt rund 0,5 PT. Gemessen an Paket 1 (4–6 PT) und
am Gesamtvorhaben (61–78 PT) ist das ein Rundungsfehler gegenüber dem Risiko, das er
abdeckt.

---

## Konsequenzen

**Was leichter wird**

- Die Datenmigration bekommt eine belastbare Einmal-Semantik; die Migrationstabelle
  aus Konzept 5.5 wird direkt umsetzbar.
- Schemafehler werden sichtbar statt still — das behebt zugleich die in Konzept 5.6
  benannte Schwäche „Fehler werden verschluckt", ohne sie separat behandeln zu müssen.
- Paket 7 kann `Tab_ErgebnisPufferspeicher` samt `ON DELETE CASCADE` regulär anlegen;
  die in 6.6 beschriebene Sorge um Waisenzeilen durch fehlende Löschweitergabe entfällt.
- Das Feature-Flag `Kaskade_Zweikanalig` (Paket 4) und die B0-6-Beziehung
  `Tab_Projekt → Tab_Pufferspeicher` bekommen einen definierten Ort.
- Künftige Schemaänderungen haben ein Muster, dem sie folgen können.

**Was schwerer wird**

- `Program.cs` bekommt eine Startsequenz mit DB-Zugriff. Das ist ein neuer
  Fehlerpfad vor dem ersten Fenster und braucht eine eigene, verständliche Meldung.
- Ein fehlgeschlagener Schritt blockiert den Simulationsbereich. Das ist gewollt,
  erzeugt aber Supportfälle, die es vorher (scheinbar) nicht gab — tatsächlich sind
  es dieselben Fälle, nur bisher unsichtbar.
- Die Schemadefinition liegt an zwei Orten, solange `SchemaSicherstellen()` als
  Rückfallebene bestehen bleibt. Gegenmittel: gemeinsamer Spaltenkatalog, eine Quelle.

**Was später erneut zu prüfen ist**

- Ob `SchemaSicherstellen()` nach ein bis zwei Releases ganz entfallen kann, sobald
  die Migration verlässlich beim Start läuft.
- Ob der Schreibschutzfall eine eigene Behandlung braucht (etwa ein geführtes
  „Komprimieren und reparieren"), falls er in der Praxis häufig auftritt.
- ~~Der Grund für die Löschung von `UpdateDatabaseFromScript.cs` ist ungeklärt.~~
  **Geklärt (14.08.2026):** Die Löschung war beabsichtigt. Zusätzlich festgelegt:
  Das externe DB-Migrationswerkzeug (`DB_Migration`) und das dynamisch erzeugte
  Migrationsskript (`migration*.sql`) sind **grundsätzlich nicht** Teil der
  Anwendungsarchitektur — das Skript wird bei jeder Migration neu generiert und
  ändert sich, es taugt weder als Referenz noch als Ausrollpfad. Option A ist
  damit endgültig verworfen.

---

## Aufgaben

1. [x] Klären, warum `Allgemein/Update/` gelöscht wurde — **geklärt 14.08.2026:
       Absicht.** Die Prämisse des ADR ist bestätigt, der ADR ist angenommen.
2. [ ] `Tab_Applikation` um `SchemaVersion LONG` erweitern (Default 0) und in
       `ApplikationCtrl` lesbar/schreibbar machen.
3. [ ] `Allgemein/Update/SchemaMigration.cs` anlegen: Schrittregister, Marker-Handling,
       Sammelfehlerbericht. UTF-8 mit BOM, Namespace `WindowsFormsApplication1`.
4. [ ] Spaltenkatalog aus `WaermequelleClass.SchemaSicherstellen()` herauslösen, damit
       Migration und Rückfallebene dieselbe Liste verwenden.
5. [ ] Schritte 1–5 implementieren (22 Spalten, Ergebnistabelle, 5 Beziehungen,
       Projektdatenmigration nach Konzept 5.5).
6. [ ] Aufruf in `Program.Main` vor dem MDI-Start verdrahten; Blockade des
       Simulationsbereichs bei nicht abgeschlossener Migration.
7. [ ] Konzept `Konzept_Simulation_QuellenSenken.md` nachziehen: Kapitel 5.6 und 6.6
       durch Verweis auf diesen ADR ersetzen, offenen Punkt O6 als erledigt markieren,
       die Spaltenzahl in 5.6 von „13" auf 22 korrigieren.
8. [ ] Abnahmefall ergänzen: Migration auf schreibgeschützter `Kenndaten.accdb` —
       erwartetes Verhalten ist eine verständliche Meldung und ein blockierter
       Simulationsstart, kein stiller Weiterlauf.
