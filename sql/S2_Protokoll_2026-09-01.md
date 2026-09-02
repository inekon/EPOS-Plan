# S2-Abnahmeprotokoll — Zielschema SQLite

**01.09.2026** · Arbeitspaket **S2** aus
[`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](../Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md)
(Abschnitt 3) · Quelle: `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` (Rechner 1, **nach S0**,
151.949.312 Bytes, Stand 01.09.2026 15:16:48) · Zugriff **strikt lesend** über
`DAO.DBEngine.120` (`OpenDatabase(pfad, exklusiv=false, readonly=true)`).
An Datenbank und Anwendung wurde **nichts** geändert; Länge und Zeitstempel der Quelle sind vor
und nach dem Lauf gleich (der Generator prüft das selbst und verwirft den Lauf sonst).

**Gesamturteil: Abnahme bestanden** — 25 von 25 Prüfungen ohne Abweichung. Es bleiben
**9 Kurationspunkte**, davon einer (**K1**) mit echtem Entscheidungsbedarf vor S3/S4.

## 1. Vorabprüfung

| Prüfung | Soll | Ist |
|---|---|---|
| `Tab_Applikation.SchemaVersion` (DISTINCT) | 61 | **61** (genau ein Wert) |
| `TableDefs` ohne `MSys*` | 114 | **114** |
| davon verknüpft (`Connect <> ""`) | 0 | **0** |
| `Relations.Count` | 90 | **90** |

## 2. Soll/Ist — Struktur

„Soll" ist die DAO-Messung vom 31.08.2026 auf Rechner 2 (Konzept Rev. 2, Abschnitte 2 und 5).

| Kennwert | Soll | Ist | Befund |
|---|---:|---:|---|
| Tabellen | 114 | **114** | ✔ |
| Spalten gesamt | 2.479 | **2.479** | ✔ |
| Autowertspalten (`dbAutoIncrField`) | 80 | **80** | ✔ |
| `INTEGER PRIMARY KEY AUTOINCREMENT` erzeugt | 80 | **80** | ✔ (siehe K1) |
| Double → `REAL` | 1.730 | **1.730** | ✔ |
| Long → `INTEGER` | 371 | **371** | ✔ |
| Boolean → `INTEGER … CHECK` | 97 | **97** | ✔ |
| Datum → `TEXT` | 20 | **20** | ✔ |
| Text → `TEXT` | 261 | **253** | ▲ A1 |
| davon `Size < 255` mit `CHECK(length…)` | 90 | **82** | ▲ A1 |
| davon `Size = 255` ohne CHECK | 171 | **171** | ✔ |
| Memo → `TEXT` | 14 | **8** | ▲ A1 |
| unbekannte DAO-Typcodes | 0 | **0** | ✔ (nur 1, 4, 7, 8, 10, 12) |
| `Field.Required` | — | **114** | — |
| Beziehungen gesamt | 90 | **90** | ✔ |
| davon Systembeziehungen (`MSys*`) | 0 | **2** | ▲ A2 |
| **erzeugte `FOREIGN KEY`-Klauseln** | 90 | **88** | ▲ A2 |
| `dbRelationDontEnforce` | 0 | **0** | ✔ |
| `ON UPDATE CASCADE` | 61 | **59** | ▲ A2 |
| `ON DELETE CASCADE` | 79 | **77** | ▲ A2 |
| Tabellen ohne Primärindex („PK ergänzt") | 3 | **3** | ✔ |
| Autowert nicht alleiniger PK | 0 | **50** | ▲ **A3 / K1** |
| Indizes in der Quelle | 444 | **441** | ▲ A1 |
| Indizes nach Entdoppelung → 003 | 232 | **260** | ▲ A4 |
| Indexnamen-Kollisionen (umbenannt) | — | **100** | K5 |
| gespeicherte Abfragen | 17 | **17** | ✔ |
| Views in 002 | 14 | **14** | ✔ |
| `PARAMETERS`/`TRANSFORM`/`DISTINCTROW` | 0 | **0** | ✔ |
| Nachschlagefelder (`DisplayControl` 110/111) | ? | **0** | ✔ ([S1-Restbericht](S1_Feinmessung.md)) |

DEFAULT-Rohwerte — **exakt wie erwartet**, keine Ausdrücke (`(`, `)`, `=`) vorhanden:
`0` × 219 · `No` × 35 · `""` × 2 · `False` × 1 · `"%"` × 1 · `50` × 1 · `Null` × 1.
Verteilung: 4 × `0` und 35 × `No` und 1 × `False` auf Boolean (dort ignoriert, es gilt
`DEFAULT 0`), 95 × `0` auf Long, 120 × `0` und 1 × `50` auf Double, `""` × 2 und `"%"` auf Text,
`Null` × 1 (`Tab_Projekt.ID_Klimaregion`, ergibt kein `DEFAULT`).

### Abweichungen im Klartext

**A1 — Memo 8 statt 14, Text 253 statt 261, Indizes 441 statt 444: Folge von S0, kein Fehler.**
Konzept Rev. 2, Abschnitt 2.2 nennt es selbst: von den 14 Memo-Spalten lagen „**+ 6 in den
verwaisten Verknüpfungen aus 2.4, die entfallen**". Die acht verbliebenen sind genau die dort
namentlich aufgeführten (`Berichtskonfiguration.KonfigJson`, `energy_price.notes`,
`Tab_Energieanlagen.WQ_Wochenwerte`, `Tab_ErgebnisWirtschaftlichkeit.Fehlgrund` /
`.HinweisText` / `.SteuerHerkunft`, `Tab_Kostenprofil.Wochenwerte`,
`Tab_KostenVorlage.Bemerkung`). Dieselben vier ODBC-Verknüpfungen erklären die 8 fehlenden
Textspalten (alle mit `Size < 255`, daher genau die Lücke 90 → 82) und die 3 fehlenden Indizes.
Die Gesamtspaltenzahl 2.479 stimmt exakt — die Erwartungswerte der Typtabelle waren an dieser
einen Stelle noch der Stand **vor** S0. → **Konzept nachziehen (K8), keine Nacharbeit am Schema.**

**A2 — 88 statt 90 Fremdschlüssel: zwei Access-Systembeziehungen.** `Relations.Count` ist 90,
aber zwei davon verbinden Systemtabellen:
`MSysNavPaneGroupCategoriesMSysNavPaneGroups` und `MSysNavPaneGroupsMSysNavPaneGroupToObjects`
(beide Attribut 4352 = `UpdateCascade|DeleteCascade`). Sie gehören zum Navigationsbereich des
Access-Frontends, nicht zum Datenmodell, und werden übersprungen. Das erklärt auch 61 → 59 und
79 → 77 exakt. **Fachlich bleiben es 88 echte Beziehungen, alle erzwungen.**

**A3 — 50 Autowertspalten sind in Access nicht alleiniger Primärschlüssel.** Siehe **K1**.

**A4 — 260 statt 232 Indizes.** Die 232 aus dem Konzept entstehen bei Entdoppelung nach
**Spaltenliste**; die für S2 vorgegebene Regel entdoppelt nur gegen den Primärschlüssel. Beide
Zahlen sind nachgerechnet (siehe **K3**).

## 3. Abbildungsregeln — wie umgesetzt

- **Typen** `1→INTEGER` (Boolean, immer `NOT NULL DEFAULT 0 CHECK (x IN (0,1))`),
  `4→INTEGER`, `7→REAL`, `8→TEXT`, `10→TEXT` (+ `CHECK (length(x) <= n)` bei `Size < 255`),
  `12→TEXT` ohne CHECK. Jede Tabelle endet auf `) STRICT;`.
- **NOT NULL** aus `Field.Required` **und** aus Boolean **und** aus der Mitgliedschaft im
  Access-Primärindex (siehe K6).
- **Autowert** ausschließlich über `Attributes -band 16`, nie über den Spaltennamen — die drei
  Sonderfälle `Tab_Kostenfaktor.StammID`, `Tab_Klimadaten_STAMM.ID_Klimadaten`,
  `Tab_Klimaregion_STAMM.ID_Klimaregion` sind korrekt erkannt.
- **Fremdschlüssel** stehen in der `CREATE TABLE` der **Kindtabelle** in `001`, nicht in `003` —
  SQLite kann sie nicht nachrüsten. Der Kopfkommentar von `003` sagt das ausdrücklich (K7).
- **Bezeichner** durchgängig `"…"`; die 20 Spalten mit Umlaut (z. B. `Rücklauf`,
  `k_Wert_Außenwand`, `WBVK_Anschluß_Fenster_Wand`, `ID_Energieträger`) laufen fehlerfrei.
- **Determinismus:** Tabellen alphabetisch **ordinal**, Spalten in Original-Ordinalreihenfolge,
  Indizes und FK je Tabelle ordinal nach Namen. Der Zeitstempel im Dateikopf ist der
  **Änderungsstand der Quelle**, nicht die Laufzeit — zwei aufeinanderfolgende Läufe erzeugen
  **byte-identische** Dateien (per SHA-256 geprüft).

### „PK ergänzt" (erwartet und bestätigt)

| Tabelle | Spalte | vorher |
|---|---|---|
| `Tab_BHKW` | `ID` | kein Primärindex, Autowert + UNIQUE-Index `ID` |
| `Tab_DBTagVDaten_STAMM` | `ID` | kein Primärindex, Autowert |
| `Tab_Stromverbrauchertyp_STAMM` | `ID` | kein Primärindex, Autowert |

Nach dem Lauf hat **jede** der 114 Tabellen einen Primärschlüssel.

## 4. Abnahmeproben

`python sql/tools/baue_leere_db.py` — Python 3.14.5, **sqlite3 3.50.4** (≥ 3.37 für `STRICT`).
Leere Datenbank aus `001` → `002` → `003`, 1.708.032 Bytes.

```
  [ok] SQLite-Version >= 3.37 (STRICT)                Soll: >= 3.37   Ist: 3.50.4
  [ok] 001_grundschema.sql laeuft fehlerfrei
  [ok] 002_views.sql laeuft fehlerfrei
  [ok] 003_indizes_fk.sql laeuft fehlerfrei
  [ok] PRAGMA integrity_check                         Soll: ok        Ist: ok
  [ok] PRAGMA foreign_key_check                       Soll: leer      Ist: 0 Zeile(n)
  [ok] Tabellen (ohne sqlite_%)                       Soll: 114       Ist: 114
  [ok] Views                                          Soll: 14        Ist: 14
  [ok] Indizes (explizit angelegt)                    Soll: 260       Ist: 260
  [ok] CREATE INDEX-Anweisungen in 003                Soll: 260       Ist: 260
  [ok] Spalten gesamt                                 Soll: 2479      Ist: 2479
  [ok] Tabellen mit abweichender Spaltenzahl          Soll: 0         Ist: 0
  [ok] FOREIGN-KEY-Klauseln gesamt                    Soll: 88        Ist: 88
  [ok] davon ON UPDATE CASCADE                        Soll: 59        Ist: 59
  [ok] davon ON DELETE CASCADE                        Soll: 77        Ist: 77
  [ok] INTEGER PRIMARY KEY AUTOINCREMENT              Soll: 80        Ist: 80
  [ok] Tabellen mit STRICT                            Soll: 114       Ist: 114
  [ok] Tabellen ohne Primaerschluessel                Soll: 0         Ist: 0
  [ok] FK-Elternspalten ohne PK/UNIQUE-Deckung        Soll: 0         Ist: 0
  [ok] Views per SELECT ... LIMIT 0 ausfuehrbar       Soll: 14        Ist: 14
  [ok] STRICT: TEXT in REAL-Spalte (emissionsart.co2_aequivalent)
       -> abgewiesen: cannot store TEXT value in REAL column emissionsart.co2_aequivalent
  [ok] Boolean-CHECK: -1 in emissionsart.ist_pflicht
       -> abgewiesen: CHECK constraint failed: ist_pflicht
  [ok] FK: unbekannter Elternwert in emissionswert.emissionsart_id -> emissionsart
       -> abgewiesen: FOREIGN KEY constraint failed
  [ok] Gegenprobe: derselbe INSERT mit foreign_keys=OFF -> durchgelassen
  [ok] DEFAULT unter STRICT (energy_carrier.price_work) -> eingefuegt, price_work = 0.0

  Pruefungen: 25, davon Abweichungen: 0 -> BESTANDEN
```

Bemerkenswert:

- Die **Gegenprobe** belegt schwarz auf weiß, was Konzept 5.3 fordert: ohne
  `PRAGMA foreign_keys = ON` läuft derselbe verletzende `INSERT` durch. Das PRAGMA je
  Verbindung ist in S4 nicht optional.
- Die **DEFAULT-Probe** zeigt, dass `DEFAULT 0` auf einer `REAL`-Spalte unter `STRICT` sauber
  zu `0.0` wird (`STRICT` erlaubt die verlustfreie Wandlung INTEGER → REAL).
- Zusatzprüfung über die Vorgabe hinaus: **jede FK-Elternspalte** ist durch Primärschlüssel oder
  UNIQUE-Index gedeckt. `PRAGMA foreign_key_check` schweigt auf leeren Tabellen dazu; ein
  ungedeckter Elternschlüssel fiele sonst erst in S3 beim ersten Datensatz als
  „foreign key mismatch" auf.
- Alle 14 Views sind ausführbar, **inklusive** der drei mit doppelten Ergebnisspaltennamen
  (`Abfrage_ProjektGebaeudeGanglinie`, `Abfrage_ProjektStromGanglinie`,
  `Abfrage_Tagverteilung` wählen je zweimal `ID`) und der Abhängigkeit
  `Abfrage_KenndatenKuehlung_Max` → `Abfrage_Kuehlung_MaxLast`.

## 5. Typkatalog

| | Spalten | eindeutige Namen | ausgeschlossen |
|---|---:|---:|---|
| `BoolSpalten` | 97 | **61** | `Heizstab` (K4) |
| `DatumSpalten` | 20 | **11** | — |

Die Namen wiederholen sich über Tabellen hinweg (97 Boolean-Spalten tragen 62 verschiedene
Namen, 20 Datumsspalten nur 11). `typkatalog.json` enthält zusätzlich die exakte
Tabelle→Spalten-Zuordnung; Datumsnamen sind: `Aenderungsdatum`, `Erstelldatum`, `GeaendertAm`,
`Inbetriebnahme`, `KWKG_Inbetriebnahme`, `KWKG_Stichtag`, `Tarif_GueltigAb`, `Zeitstempel`,
`gueltig_ab`, `valid_from`, `valid_to`.

## 6. Kurationspunkte

### K1 — 50 Autowertspalten sind in Access nicht alleiniger Primärschlüssel · **Entscheidung nötig**

Das ist der einzige Punkt, der vor S3/S4 abgenommen werden muss. Weder Konzept noch
S2-Auftrag kannten ihn; erwartet waren 0 solche Fälle.

Gemessen (per DAO **und** unabhängig per ADOX gegengeprüft, also kein Messartefakt):

- **49 Tabellen** haben einen **mehrspaltigen** Access-Primärschlüssel, der die Autowertspalte
  als erste Spalte enthält — z. B. `Tab_Energieanlagen` mit `PrimaryKey = (ID, ID_Projekt)`,
  `Tab_Heizkessel` mit `(ID, ID_Projekt, Bezeichner)`. (35 × zweispaltig, 14 × dreispaltig.)
- **1 Tabelle**, `Tab_Projekt`, hat den Primärschlüssel auf `Projektname` (TEXT), während die
  Autowertspalte `ID` nur einen UNIQUE-Index trägt.

SQLite verlangt für `AUTOINCREMENT` zwingend `INTEGER PRIMARY KEY` als **alleinigen**
Schlüssel. Die wörtliche Auftragsregel („harter Fehler zur Kuration") hätte den Lauf für 50 von
114 Tabellen abgebrochen; zugleich verlangt derselbe Auftrag 80 `AUTOINCREMENT`-Spalten, und
Konzept 5.2 führt als Muster ausgerechnet `Tab_Energieanlagen` mit
`ID INTEGER PRIMARY KEY AUTOINCREMENT` und **ohne** Verbund-PK vor. Das Konzept hat sich also
implizit bereits für die hier gewählte Auflösung entschieden.

**Umgesetzt (Vorgabe `-AutowertPK AutowertBevorzugen`):** die Autowertspalte wird alleiniger
`INTEGER PRIMARY KEY AUTOINCREMENT`; **alle** Spalten des ursprünglichen Access-PK behalten
`NOT NULL`. Das ist **verlustfrei**, nicht bloß pragmatisch:

- Für die 49 Verbund-PKs ist die Eindeutigkeit des alten Tupels durch die Eindeutigkeit der
  Autowertspalte **logisch impliziert** — eine zusätzliche UNIQUE-Regel wäre reiner Ballast.
  Die neue Regel ist strenger als die alte, nie schwächer; jeder heute gültige Bestand bleibt
  gültig.
- Für `Tab_Projekt` ist die Eindeutigkeit von `Projektname` durch den bereits vorhandenen
  UNIQUE-Index `Projektname` gesichert (steht in `003`). Alle Beziehungen auf `Tab_Projekt`
  zeigen ohnehin auf `ID`, nicht auf `Projektname` — an den Kaskaden ändert sich nichts.
- Der Generator prüft das selbst: er legte **0** Ersatz-UNIQUE-Indizes an, weil alle alten
  PK-Tupel bereits gedeckt oder impliziert sind (`ZusatzUnique` in `inventar.json` ist leer).

**Alternativen im Generator hinterlegt**, falls die Abnahme anders entscheidet:
`-AutowertPK Originaltreu` übernimmt den Access-PK wörtlich und degradiert die 50
Autowertspalten zu `INTEGER NOT NULL` **ohne** `AUTOINCREMENT` — dann müsste die Anwendung alle
IDs selbst vergeben, und `ExecuteInsertAndGetId` (Implementierungskonzept 2.5) wäre für die
halbe Datenbank wertlos. `-AutowertPK Streng` bricht bei jedem Konflikt ab.

Vollständige Liste der 50 Tabellen in
[`schema/inventar.json`](schema/inventar.json) unter `PkGewechselt` (je Tabelle mit Access-PK,
effektivem PK und dem Vermerk, ob die alte Eindeutigkeit impliziert ist).

### K2 — Zwei Systembeziehungen: 88 statt 90 Fremdschlüssel

Siehe A2. Zahlen in Konzept Rev. 2 (5.3) und Implementierungskonzept (2.1, 3.1) sollten von
90/61/79 auf **88/59/77** korrigiert werden.

### K3 — 81 spaltenlisten-gleiche Indexdubletten

Access legt zu jeder Beziehung einen Stützindex an, obwohl oft schon ein gleichspaltiger Index
existiert (z. B. `Tab_StromganglinieDaten`: `ID_GanglinieDaten` und
`Tab_StromganglinieTab_StromganglinieDaten`, beide auf `(ID_Ganglinie)`). Nach der
S2-Vorgabe („auch `.Foreign`-Stützindizes übernehmen") stehen alle 260 in `003`.
Mit `-IndexEntdoppelung Spaltenliste` blieben **179**. Kein UNIQUE-Flag ist innerhalb einer
Dublettengruppe uneinheitlich, die Entdoppelung wäre also folgenlos für die Semantik — sie
spart aber Schreiblast auf den Großtabellen (`Tab_StromganglinieDaten`: 648.241 Zeilen).
Die Variante ist mitgeprüft: `-IndexEntdoppelung Spaltenliste` besteht dieselbe Abnahme
**25 von 25** und ergibt eine leere Datenbank von 1.359.872 statt 1.708.032 Bytes.
**Empfehlung: vor S3 auf `Spaltenliste` umstellen.** Liste in `inventar.json` unter
`SpaltenlistenDubletten`.

### K4 — `Heizstab` ist typmehrdeutig und fehlt daher im Typkatalog

`Tab_Energieanlagen.Heizstab` ist Boolean, `Tab_ErgebnisWaermepumpeModul.Heizstab` ist Double.
Ein namensbasierter Katalog kann beide nicht unterscheiden, deshalb ist der Name in
`BoolSpalten` **nicht** enthalten. Für S4 heißt das: die Boolean-Angleichung in `GetDataTable`
greift für `Tab_Energieanlagen.Heizstab` nicht automatisch. Zu prüfen, ob eine Lesestelle das
braucht — sonst genügt der Eintrag in `typkatalog.json` als Nachweis. Datumsspalten sind
eindeutig, dort gibt es den Fall nicht.

### K5 — 100 Indexnamen mussten umbenannt werden

Access-Indexnamen sind nur je Tabelle eindeutig, SQLite-Objektnamen global (und
groß-/kleinschreibungsunabhängig). Betroffen vor allem `ID_Projekt` (35 ×), `Bezeichner` (18 ×),
`Typname` und `ID_GanglinieDaten` (je 6 ×), `ID_WP` (5 ×) sowie das Paar `code`/`Code`, das sich
nur in der Schreibweise unterschied. Regel: **alle** Träger eines mehrfach vergebenen Namens
werden zu `Tabelle_Indexname` (deterministisch, nicht „der erste gewinnt"). Der Generator
verweigert den Lauf, falls danach noch ein Name doppelt wäre oder einem Tabellennamen gliche.
Vollständige Zuordnung in `inventar.json` unter `IndexKollisionen`, Originalname zusätzlich je
Index unter `Tabellen.<name>.Indizes[].AccessName`.

### K6 — `NOT NULL` auch aus der Access-PK-Mitgliedschaft

Die Auftragsregel leitet `NOT NULL` allein aus `Field.Required` ab. Access verbietet NULL aber
auch in **jeder** PK-Spalte, unabhängig von `Required`. Beim PK-Wechsel aus K1 wäre diese Regel
sonst stillschweigend verlorengegangen. Der Generator setzt `NOT NULL` deshalb zusätzlich für
Access-PK-Mitglieder. Betroffen sind über `Required` hinaus genau **zwei** Spalten:
`Z_ProjektWaermebedarf.ID_Projekt` und `Z_ProjektWaermebedarf.ID_Ganglinie` (beide Teil des
Access-PK `(ID_Z, ID_Projekt, ID_Ganglinie)`). Wirkt zugleich gegen die bekannte
SQLite-Eigenheit, dass eine `PRIMARY KEY`-Spalte außer `INTEGER PRIMARY KEY` sonst NULL
zuließe — betrifft `pricing_model.code` und die beiden Verbund-PKs ohne Autowert
(`Tab_Applikation`, `Tab_Brauchwassertyp`).

### K7 — Dateiname `003_indizes_fk.sql` führt in die Irre

Die Datei enthält **nur** Indizes; die Fremdschlüssel stehen zwingend in `001`. Name aus dem
Konzept beibehalten, Kopfkommentar der Datei stellt es klar. Optional in S6 umbenennen.

### K8 — Konzeptzahlen auf den Stand nach S0 ziehen

Memo 14 → **8**, Text 261 → **253**, Text mit CHECK 90 → **82**, Indizes 444 → **441**,
Beziehungen 90 → **88** (siehe A1, A2).

### K9 — Ablageort von `SchemaTypKatalog.g.cs`

Liegt vorerst in `sql/schema/`. In S4 nach
`WindowsFormsApplication1/Allgemein/` übernehmen und in die `.csproj` aufnehmen
(Namensraum ist bereits `WindowsFormsApplication1.Allgemein`).

## 7. Erzeugte Dateien

| Datei | Größe | Inhalt |
|---|---:|---|
| [`sql/tools/Erzeuge-Schema.ps1`](tools/Erzeuge-Schema.ps1) | 41.836 B | Generator (PowerShell 7, DAO 120) |
| [`sql/tools/baue_leere_db.py`](tools/baue_leere_db.py) | 13.971 B | Aufbau der leeren DB + 25 Abnahmeproben |
| [`sql/schema/001_grundschema.sql`](schema/001_grundschema.sql) | 80.834 B | 114 × `CREATE TABLE … STRICT` inkl. PK und 88 FK |
| [`sql/schema/002_views.sql`](schema/002_views.sql) | 8.491 B | 14 Views (2 übersetzt, 12 unverändert) |
| [`sql/schema/003_indizes_fk.sql`](schema/003_indizes_fk.sql) | 25.664 B | 260 × `CREATE [UNIQUE] INDEX` |
| [`sql/schema/SchemaTypKatalog.g.cs`](schema/SchemaTypKatalog.g.cs) | 3.420 B | 61 Bool- + 11 Datumsnamen für S4 |
| [`sql/schema/typkatalog.json`](schema/typkatalog.json) | 7.744 B | dasselbe mit Tabelle→Spalten-Zuordnung |
| [`sql/schema/inventar.json`](schema/inventar.json) | 981.103 B | vollständiges Strukturinventar + Zählungen |
| [`sql/S1_Feinmessung.md`](S1_Feinmessung.md) | 2.795 B | Rest-S1: Nachschlagefelder |

Alle Dateien UTF-8 **ohne** BOM mit LF-Zeilenenden. Wiederholungsaufruf:

```powershell
pwsh -File sql\tools\Erzeuge-Schema.ps1
python sql\tools\baue_leere_db.py
```

## 8. Was S2 noch offen lässt

- **K1 abnehmen** (PK-Politik) — blockiert S3/S4.
- **DBeaver-ER-Diagramm** gegen das Access-Beziehungsfenster (Implementierungskonzept 3.3,
  Punkt 4): manuelle Sichtprüfung, hier nicht durchführbar.
- Handkuratierung und **Einfrieren** des Satzes vor S4-Beginn (Konzept 5.6).

---

## Nachtrag 02.09.2026 — K1 und K3 entschieden, Basis neu erzeugt

- **K1 abgenommen:** Politik `AutowertBevorzugen` bestätigt und als **D10** ins
  [`Implementierungskonzept`](../Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md)
  (Abschnitte 1.2, 3.1, 10) übernommen. Stichprobe geprüft: `Tab_Projekt` erhält
  `ID INTEGER PRIMARY KEY AUTOINCREMENT`, `Projektname TEXT NOT NULL` **plus**
  `CREATE UNIQUE INDEX "Projektname"` in 003 — keine Eindeutigkeit geht verloren.
- **K3 umgesetzt:** Satz mit `-IndexEntdoppelung Spaltenliste` neu erzeugt — **179 Indizes**
  (81 spaltenlisten-gleiche Dubletten entfallen, 96 Namenskollisionen deterministisch
  umbenannt). Das ist ab jetzt die eingecheckte Basis.
- **Abnahme wiederholt:** `baue_leere_db.py` gegen den neuen Satz — **25/25 bestanden**
  (Zählungen 114/2.479/88 FK/59/77 Kaskaden/80 AUTOINCREMENT/179 Indizes; STRICT-, Boolean-,
  FK- und FK-OFF-Gegenprobe unverändert).
- **K2/K8** (88 echte FKs, MSys-Systembeziehungen; Konzeptzahlen) sind im
  Implementierungskonzept Abschnitt 1.2 festgehalten.
