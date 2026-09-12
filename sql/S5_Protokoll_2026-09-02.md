# S5 — Dialekt-Sweep mit Vollständigkeitsnachweis

**Datum:** 02.09.2026 · **Repo:** `WP-Plan`, Branch `sqlite`
**Referenz:** `Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`, Abschnitt 6
**Vorstand:** S4a–e fertig (App baut gegen SQLite, Typ-Rückweg D9 aktiv, Probensuite 12/12)
**Nicht Gegenstand:** SchemaMigration-Start (= S6), `SchemaMigration.cs`, `SchemaKatalog.cs`,
`Referenzlauf/`, `EposSqliteMigrator/`

**Ergebnis in einem Satz:** 26 `SELECT TOP 1` → `LIMIT 1` in 13 Dateien, 4 harte Casts
vereinheitlicht, 1 Dialekt-Dispatch (Exportliterale) von Access auf SQLite umgestellt,
1 Dispatch geprüft und bewusst unverändert gelassen; **58 Ausführungsnachweise gegen die
Probendatenbank, 0 Fehler**; Build 0 Fehler / exakt 5 Bestandswarnungen; Probensuite 12/12.

---

## 1. Kodierungsbefunde (Pflichtprüfung je Datei)

Vor der ersten Änderung wurde jede Zieldatei vermessen (`s5_enc.py`: BOM → normal;
sonst `utf-8`-strict-Probe; nur bei Fehlschlag cp1252 → byte-treu über latin-1).

**Befund: keine einzige der 21 berührten Dateien ist cp1252.** Damit entfiel der
byte-treue Sonderweg vollständig.

| Datei | Kodierung | CRLF |
|---|---|---|
| `Controller/ErgebnisCtrl.cs` | BOM-utf8 | durchgängig |
| `Controller/EmissionenCtrl.cs` | BOM-utf8 | durchgängig |
| `Controller/BetriebskostenCtrl.cs` | utf8 ohne BOM | durchgängig |
| `Allgemein/Bericht/ProjektDetails.cs` | utf8 ohne BOM | durchgängig |
| `Allgemein/Bericht/KostenEmissionRechner.cs` | utf8 ohne BOM | durchgängig |
| `Allgemein/Bericht/BerichtsDatenSammler.cs` | utf8 ohne BOM | durchgängig |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | utf8 ohne BOM | durchgängig |
| `Allgemein/KI/KiSchreibschutz.cs` | BOM-utf8 | durchgängig |
| `Allgemein/KI/Aktionen/KiAktionenSchreiben.cs` | BOM-utf8 | durchgängig |
| `Controller/KomponentenUebernahmeCtrl.cs` | BOM-utf8 | durchgängig |
| `Controller/StromPreisCtrl.cs` | BOM-utf8 | durchgängig |
| `Controller/TechnikPlanwertCtrl.cs` | utf8 ohne BOM | durchgängig |
| `Controller/StromspeicherVarianteCtrl.cs` | BOM-utf8 | durchgängig |
| `Controller/GebäudeKontextMenuCtrl.cs` | BOM-utf8 | durchgängig |
| `Views/Wärmepumpe/Kenndaten.cs` | BOM-utf8 | durchgängig |
| `Controller/ProjektExportImportCtrl.cs` | BOM-utf8 | durchgängig |
| `Allgemein/Katalog/DublettenPruefung.cs` | utf8 ohne BOM (reines ASCII) | durchgängig |
| *nur gelesen:* `ProjektPhotovoltaikCtrl.cs`, `Ladeordnung.cs`, `EmissionskatalogCtrl.cs`, `ProjektDuplizierenCtrl.cs` | BOM-utf8 | — |

### Gegenrechnung auf Byte-Ebene

Der TOP-Umbau lief **nicht** über einen Texteditor, sondern über eine reine
**Byte-Ersetzung** ASCII→ASCII (`s5_top.py`). Jede Datei wurde vor und nach dem Schreiben
gemessen; **alle drei Kennzahlen blieben identisch**:

| Datei | Ersetzungen | Nicht-ASCII-Bytes vorher → nachher | CRLF vorher → nachher |
|---|---|---|---|
| `ErgebnisCtrl.cs` | 7 | 159 → 159 | 1608 → 1608 |
| `EmissionenCtrl.cs` | 1 | 311 → 311 | 722 → 722 |
| `BetriebskostenCtrl.cs` | 1 | 452 → 452 | 735 → 735 |
| `ProjektDetails.cs` | 1 | 98 → 98 | 142 → 142 |
| `KostenEmissionRechner.cs` | 2 | 527 → 527 | 564 → 564 |
| `BerichtsDatenSammler.cs` | 1 | 240 → 240 | 481 → 481 |
| `WirtschaftlichkeitCtrl.cs` | 7 | 4259 → 4259 | 5842 → 5842 |
| `KiSchreibschutz.cs` | 2 | 3 → 3 | 115 → 115 |
| `KiAktionenSchreiben.cs` | 1 | 6 → 6 | 574 → 574 |
| `KomponentenUebernahmeCtrl.cs` | 1 | 509 → 509 | 1243 → 1243 |
| `StromPreisCtrl.cs` | 2 | 11 → 11 | 529 → 529 |
| `TechnikPlanwertCtrl.cs` | 2 | 740 → 740 | 1010 → 1010 |
| `StromspeicherVarianteCtrl.cs` | 3 | 3 → 3 | 393 → 393 |

Zusätzlich wurde eine **Umlaut-Stichprobe** (Zeichenzählung je Umlaut, BOM-Status,
Nicht-ASCII-Byteanzahl) vor und nach dem gesamten Arbeitspaket erhoben — inklusive der
per Editor geänderten Dateien. Der Vergleich ist **zeichengleich** (`diff` leer).

**Warum die Zeilen `WirtschaftlichkeitCtrl.cs:722/730` byte-weise angefasst wurden:**
Sie enthalten `[Wirkungsgrad_Öl]`. Ersetzt wurde deshalb nur das reine ASCII-Präfix
(`"SELECT TOP 1 g.Bezeichner…` → `"SELECT g.Bezeichner…`), das `Ö` blieb unberührt.

---

## 2. Sweep-Gegenstand 1 — `SELECT TOP 1` → `LIMIT 1`

**Messstand vor S5:** 29 ausführbare `SELECT TOP` — davon 3 in `SchemaMigration.cs` (Tabu)
→ **26 umzubauen**.

> **Abweichung zur Auftragsliste (dokumentiert statt stillschweigend übergangen).**
> Der Auftrag nannte „~36" und führte `WaermequelleClass 368/395/918` sowie
> (über Konzept-Abschnitt 6) `PufferSpCtrl` mit 3 Stellen. Beide sind **bereits in S4b
> erledigt** worden („S5 vorgezogen"): `WaermequelleClass.cs:892` trägt schon
> `ORDER BY ID LIMIT 1`, die Schema-Auskunft bei :363 ersetzt das frühere
> `SELECT TOP 1 *` durch `StilleDb.SpaltenNamen`; `PufferSpCtrl` 1407/1592/1693 sind
> Kommentare, die den vollzogenen Umbau festhalten. Auch die Zeilennummern der
> Auftragsliste (z. B. ErgebnisCtrl 746/768/…) stammen aus einer älteren Messung; die
> Datei ist inzwischen 11 Zeilen kürzer. **Maßgeblich sind die per Grep verifizierten
> Ist-Zeilen unten.**

### Die 26 Umbauten

| # | Datei | Zeile | Abfrage (Kurzform) |
|---|---|---|---|
| 1 | `Controller/ErgebnisCtrl.cs` | 735 | `Tab_Ergebnis` … `ORDER BY ID DESC LIMIT 1` |
| 2 | `Controller/ErgebnisCtrl.cs` | 757 | `Tab_ErgebnisEnergiebedarf` … `LIMIT 1` |
| 3 | `Controller/ErgebnisCtrl.cs` | 778 | `Tab_ErgebnisWaermepumpe` … `LIMIT 1` |
| 4 | `Controller/ErgebnisCtrl.cs` | 818 | `Tab_ErgebnisBHKW` … `LIMIT 1` |
| 5 | `Controller/ErgebnisCtrl.cs` | 877 | `Tab_ErgebnisHeizkessel` … `LIMIT 1` |
| 6 | `Controller/ErgebnisCtrl.cs` | 931 | `Tab_ErgebnisSolarthermie` … `LIMIT 1` |
| 7 | `Controller/ErgebnisCtrl.cs` | 964 | `Tab_ErgebnisPhotovoltaik` … `LIMIT 1` |
| 8 | `Controller/EmissionenCtrl.cs` | 408 | `Tab_Applikation` (Berechnungsmodus) `LIMIT 1` |
| 9 | `Controller/BetriebskostenCtrl.cs` | 545 | `Tab_Ergebnis` … `ORDER BY ID DESC LIMIT 1` |
| 10 | `Allgemein/Bericht/ProjektDetails.cs` | 67 | `Tab_Klimaregion` … `LIMIT 1` |
| 11 | `Allgemein/Bericht/KostenEmissionRechner.cs` | 514 | `energy_project_settings ⋈ energy_carrier` … `LIMIT 1` |
| 12 | `Allgemein/Bericht/BerichtsDatenSammler.cs` | 89 | `Tab_Ergebnis` (Zeitstempel) … `LIMIT 1` |
| 13 | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | 722 | Kessel über Anlagenzeilen … `ORDER BY g.Ptherm DESC, g.ID LIMIT 1` |
| 14 | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | 730 | Kessel Rückfall … `ORDER BY Ptherm DESC, ID LIMIT 1` |
| 15 | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | 4570 | Strompreis Projekt/Katalog … `LIMIT 1` |
| 16 | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | 5459 | `Tab_Ergebnis` (ID) … `ORDER BY ID DESC LIMIT 1` |
| 17 | `Allgemein/KI/KiSchreibschutz.cs` | 72 | beliebige Tabelle nach ID … `LIMIT 1` |
| 18 | `Allgemein/KI/KiSchreibschutz.cs` | 109 | beliebige Tabelle (Spaltenprobe) `LIMIT 1` |
| 19 | `Allgemein/KI/Aktionen/KiAktionenSchreiben.cs` | 482 | `Tab_ProjektWerte` … `LIMIT 1` |
| 20 | `Controller/KomponentenUebernahmeCtrl.cs` | 1159 | `Tab_Energieanlagen` … `ORDER BY ID DESC LIMIT 1` |
| 21 | `Controller/StromPreisCtrl.cs` | 378 | `energy_price` … `ORDER BY valid_from DESC LIMIT 1` |
| 22 | `Controller/StromPreisCtrl.cs` | 388 | `energy_price` … `ORDER BY valid_from ASC LIMIT 1` |
| 23 | `Controller/TechnikPlanwertCtrl.cs` | 672 | `Tab_Ergebnis` … `ORDER BY ID DESC LIMIT 1` |
| 24 | `Controller/TechnikPlanwertCtrl.cs` | 885 | `Tab_Ergebnis` … `ORDER BY ID DESC LIMIT 1` |
| 25 | `Controller/StromspeicherVarianteCtrl.cs` | 45 | `Tab_StromspeicherVariante` … `ORDER BY ID LIMIT 1` |
| 26 | `Controller/StromspeicherVarianteCtrl.cs` | 74 | Variante ⋈ Anlage, `Aktiv = TRUE` … `ORDER BY v.ID LIMIT 1` |

**Regel durchgängig eingehalten:** `TOP 1` hinter dem `SELECT` entfernt, `LIMIT 1` ans
**Statement-Ende** hinter ein etwaiges `ORDER BY`. In keinem Fall wurde eine Sortierung
verändert, hinzugefügt oder entfernt.

### Ausführungsnachweis (Teil 1 der Messung)

Alle 26 Abfragen wurden mit plausiblen Parametern **read-only** gegen
`s3/Kenndaten_Rechner1.sqlite` ausgeführt — **26/26 fehlerfrei**.

| Kennung | Stelle | Zeilen | erste Zeile (gekürzt) |
|---|---|---|---|
| E1 | ErgebnisCtrl:735 | 1 | `(207, 1043, 'Simulation Beispiel WP WG 1', '2026-08-15 22:26:38', …)` |
| E2 | ErgebnisCtrl:757 | 1 | `(236, 207, 64.31, 38.6, …)` |
| E3 | ErgebnisCtrl:778 | 1 | `(207, 207, 64.31, 14.2, 50.53, …)` |
| E4 | ErgebnisCtrl:818 | 0 | — (Projekt hat kein BHKW-Ergebnis) |
| E5 | ErgebnisCtrl:877 | 1 | `(66, 207, 14.2, 0.0, 14.2, …)` |
| E6 | ErgebnisCtrl:931 | 0 | — (keine Solarthermie) |
| E7 | ErgebnisCtrl:964 | 0 | — (keine PV) |
| E8 | EmissionenCtrl:408 | 1 | `('CO2',)` |
| E9 | BetriebskostenCtrl:545 | 1 | `(207, '2026-08-15 22:26:38')` |
| E10 | ProjektDetails:67 | 1 | `('Berlin',)` |
| E11 | KostenEmissionRechner:514 | 1 | `(54,)` |
| E12 | BerichtsDatenSammler:89 | 1 | `('2026-08-15 22:26:38',)` |
| E13 | WirtschaftlichkeitCtrl:722 | 1 | `('ecoTEC plus VCI 20/26CS/1-5', 1, 0.876, 0.0)` |
| E14 | WirtschaftlichkeitCtrl:730 | 1 | `('ecoTEC plus VCI 20/26CS/1-5', 1, 0.876, 0.0)` |
| E15 | WirtschaftlichkeitCtrl:4570 | 1 | `(0.38, 0.0)` |
| E16 | WirtschaftlichkeitCtrl:5459 | 1 | `(207,)` |
| E17 | KiSchreibschutz:72 | 1 | `(10354, 1007, 'BYD B-Box HVM 11.0', …)` |
| E18 | KiSchreibschutz:109 | 1 | `(10109, 1012, 'T 800-2', …)` |
| E19 | KiAktionenSchreiben:482 | 1 | `(100701713, 1007, 2, 74, 0.0)` |
| E20 | KomponentenUebernahmeCtrl:1159 | 1 | `(10109,)` |
| E21 | StromPreisCtrl:378 | 1 | `('2026-08-12 00:00:00', 0.15)` |
| E22 | StromPreisCtrl:388 | 1 | `('2026-08-09 20:33:16', 0.0)` |
| E23 | TechnikPlanwertCtrl:672 | 1 | `(207, '2026-08-15 22:26:38')` |
| E24 | TechnikPlanwertCtrl:885 | 1 | `(207, '2026-08-15 22:26:38')` |
| E25 | StromspVarianteCtrl:45 | 1 | `(1, 10354, 'Grünstrom', 1, 1, …)` |
| E26 | StromspVarianteCtrl:74 | 0 | — (Projekt 1043 hat keine aktive Variante) |

Die vier 0-Zeilen-Fälle sind **Datenbefunde, keine Fehler**: Die Abfrage lief, das
Projekt hat schlicht kein Ergebnis dieses Gewerks. Der Code behandelt genau diesen Fall
(`if (dt == null || dt.Rows.Count == 0) …`).

**Wirkungsbeweis `LIMIT 1`:** Dieselbe Abfrage ohne und mit `LIMIT 1` verglichen —
die gelieferte Zeile ist **identisch zur ersten Zeile der Sortierung**. Damit ist die
`TOP 1`-Semantik nachweislich erhalten.

**Schlussrezept:** `SELECT TOP` ausführbar außerhalb `SchemaMigration.cs` = **0**
(dort verbleiben 3: 5664, 13132, 13364 — Arbeitspaket S6).

---

## 3. Sweep-Gegenstand 2 — die 4 harten Casts

| Datei | Zeile (vorher) | vorher | nachher |
|---|---|---|---|
| `Controller/GebäudeKontextMenuCtrl.cs` | 103 | `(int)dr["ID"]` | `Convert.ToInt32(dr["ID"])` |
| `Controller/GebäudeKontextMenuCtrl.cs` | 105 | `(int)dr["ID_ProjektGebaeude"]` | `Convert.ToInt32(dr["ID_ProjektGebaeude"])` |
| `Controller/GebäudeKontextMenuCtrl.cs` | 110 | `(bool)dr["dezWarmwasserbereitung"]` | `Convert.ToBoolean(dr["dezWarmwasserbereitung"])` |
| `Views/Wärmepumpe/Kenndaten.cs` | 107 | `(int)dr[1]` | `Convert.ToInt32(dr[1])` |

**Verhalten identisch.** Durch D9 kommen die Werte bereits als `Int32`/`bool` an — der
harte Cast würde also heute tragen. Die `Convert`-Form ist die robuste Vereinheitlichung:
Sie trägt zusätzlich, falls ein Wert einmal als `Int64` oder als 0/1 durchreicht (z. B. eine
Ausdrucksspalte mit Alias, die der `SchemaTypKatalog` nicht kennt). `using System;` ist in
beiden Dateien vorhanden.

**Nachmessung (Typ-Rückweg-Rezept, Abschnitt A):** harte `(int)`-Casts auf DB-Werte
**0**, harte `(bool)`-Casts **0**.

---

## 4. Sweep-Gegenstand 3a — Exportliteral-Dispatch (`ProjektExportImportCtrl`)

### Befund der Analyse: es ist kein Export, sondern der Import-Fallback

Der Auftrag beschrieb die Stelle als „Exportliteral-Erzeugung für den Projekttransfer".
Die Analyse zeigt ein anderes, für die Migration **kritischeres** Bild:

- Der **Export** schreibt **JSON in ein ZIP** (`RowsToJson` → `WriteEntry(ZipArchive…)`).
  Er erzeugt **kein SQL**.
- `AlsSqlLiteral` hat im ganzen Baum **genau einen Aufrufer**:
  `FuehreInsertAus` (`ProjektExportImportCtrl.cs:1026`) — den **Literal-Fallback des
  IMPORTS**. Der `INSERT` wird zweistufig versucht: (1) mit Parametern, (2) bei Fehlschlag
  mit literalen Werten.
- Der **Einspielweg ist derselbe Methodenrumpf**: `v.Ausfuehren(sql)` auf dem
  `DbVorgang` — also **die SQLite-Zugriffsschicht**, nicht eine externe Datei.

**Damit lag hier ein echter Laufzeitfehler des SQLite-Builds:** `#MM/dd/yyyy HH:mm:ss#`
ist reine Access-Syntax und in SQLite ein **Syntaxfehler**. Der Fallback wäre still
gescheitert (`catch { return ersteAusnahme; }`) und hätte den ersten Fehler gemeldet —
der wahre Grund wäre unsichtbar geblieben.

### Umbau (`ProjektExportImportCtrl.cs:1036 ff.`)

| Typ | vorher (Access/ACE) | nachher (SQLite) |
|---|---|---|
| `bool` | `"True"` / `"False"` | **`"1"` / `"0"`** |
| `DateTime` | `"#" + MM/dd/yyyy HH:mm:ss + "#"` | **`'yyyy-MM-dd HH:mm:ss'`** (InvariantCulture) |

Der Dialektwechsel ist im Quelltext ausdrücklich kommentiert.

**Warum genau diese zwei Formen.** Sie sind exakt das, was der **Parameterweg** derselben
Methode erzeugt — `DataRepository.NormalisiereWert` (Zeilen 312–319) bildet `bool` → `1/0`
und `DateTime` → `"yyyy-MM-dd HH:mm:ss"` ab. Beide Zweige schreiben jetzt dasselbe. Und es
ist die Form, die der Bestand tatsächlich führt: In der Probendatenbank stehen alle
Datumsspalten als TEXT im Muster `2026-08-15 22:26:38` (`Tab_Ergebnis.Zeitstempel`,
`Tab_Projekt.Aenderungsdatum/Erstelldatum`, `energy_price.valid_from`, …), alle
Wahrheitsspalten als 0/1.

> **Anmerkung zum `bool`-Zweig:** `True`/`False` hätte SQLite als Schlüsselwort sogar
> akzeptiert (seit 3.23 = 1/0). `1`/`0` ist trotzdem die richtige Wahl: eindeutig,
> deckungsgleich mit dem Parameterweg und mit der 0/1-Konvention des Bestands.

### Befund zum Einspielweg (nur Analyse, kein Umbau — er liegt im Scope und trägt)

Die Kette wurde durchgeprüft: `ZielTypen` liest die .NET-Zieltypen über
`SELECT * FROM [tab] WHERE 1 = 0`; das liefert auch **ohne Zeilen** korrekte Typen, weil
`LadeTabelle` den **deklarierten** Spaltentyp auswertet (`TypAusDeklaration`) und die
D9-Regeln darauf anwendet. `Passe(v, ziel)` wandelt den JSON-Wert in genau diesen Typ —
erzeugt also echte `bool`- und `DateTime`-Werte, die dann bei `AlsSqlLiteral` ankommen.
**Der Einspielweg funktioniert im SQLite-Build**, nachdem der Dispatch umgestellt ist;
ohne diesen Umbau wäre er für jede Zeile mit Datumsspalte gebrochen.

---

## 5. Sweep-Gegenstand 3b — `DublettenPruefung.Kanonisch` (geprüft, **nicht** geändert)

**Ergebnis: keine Lücke, kein Umbau.** Belegt über Aufrufpfade und Typherkunft.

**Aufrufpfade und woher die Werte stammen:**

| Aufrufstelle | Wertquelle | Typ nach D9 |
|---|---|---|
| `DublettenPruefung.cs:200` (`AbweichendeSpalten`) | `KatalogSatz.Zeile[sp]` aus `GetDataTable` | `bool` / `DateTime` / `int` / `double` / `string` |
| `:239`, `:385`, `:397`, `:320` (Inhalts-/Blockhashes) | `DataRow` aus `GetDataTable("SELECT * FROM [k.Tabelle]")` | dito |
| `:266/:277` (Importkandidat) | `kand.Werte[sp]` aus den Datei-Importern | von der DB **unberührt** |
| `KatalogBereinigung.cs:158` (`ErsteEigeneSpalte`) | zwei `DataRow` aus `GetDataTable` | dito |

**Warum der Dispatch unverändert richtig greift:**

- `bool` → `"1"`/`"0"`: Unter ACE kam eine Yes/No-Spalte als `bool`, unter SQLite liefert
  D9 (`SchemaTypKatalog.BoolSpalten`) wieder `bool`. **Gleiche Eingabe, gleiche Ausgabe.**
- `DateTime` → `"o"`: Unter ACE `DateTime`, unter SQLite über `SchemaTypKatalog.DatumSpalten`
  wieder `DateTime`. Unverändert.
- `double` → `"R"`: ACE Double → `double`, SQLite REAL → `double`. Unverändert.
- `Int32` fällt in den `IFormattable`-Zweig (`f.ToString(null, InvariantCulture)`) → `"42"`.
  Vor **und** nach der Migration derselbe Text.
- **`Int64` kann nicht entkommen:** D9 bildet *jede* INTEGER-Spalte auf `Int32` ab
  (`Spaltenart.GanzzahlInt32`) — auch Ausdrucks- und Aliasspalten, bei denen der
  beobachtete Werttyp entscheidet.
- **`float` kann nicht mehr auftreten** (SQLite REAL → `double`); der Zweig bleibt als
  toter, aber harmloser Pfad stehen.
- **`byte[]`/BLOB gibt es nicht:** Die migrierte Datenbank kennt nur die deklarierten
  Typen INTEGER, TEXT, REAL. Die einzigen zwei Spalten ohne Typangabe gehören zu
  `sqlite_sequence`, einer SQLite-internen Tabelle, die die App nie abfragt.

Da die Vergleiche bei `:277` eine **DB-Seite** gegen eine **Datei-Seite** stellen und die
DB-Seite ihren Typ behält, bleiben auch die Hash- und Dublettenurteile stabil.

---

## 6. Ausführungsnachweise 4a–4c

Messgrundlage: `s3/Kenndaten_Rechner1.sqlite` (read-only; der einzige Schreibfall auf
einer Kopie). SQLite der Messung **3.50.4**; die App bringt Microsoft.Data.Sqlite
**8.0.11** mit (e_sqlite3 ≈ 3.44) — beide weit über der `iif()`-Schwelle 3.32.

### 4a — die 3 Laufzeit-`IIf`: **3/3 lauffähig, Wirkung belegt**

**A1 `Allgemein/Simulation/Ladeordnung.cs:77` (`SqlAnlagenprio`)**
Ausgeführt mit `ANLAGENPRIO_UNGEPFLEGT = 99`:
`IIF(a.Prioritaet IS NULL OR a.Prioritaet = 0, 99, a.Prioritaet)` als Projektion **und**
als `ORDER BY`-Ausdruck. Läuft. **Wirkung geprüft:** Jede Zeile mit `Prioritaet` NULL
oder 0 erhält effektiv 99, jede gepflegte behält ihren Wert; die ungepflegten Anlagen
sortieren damit ans Ende — genau die Absicht der Regel.

**A2 `Controller/EmissionskatalogCtrl.cs:266` (`ORDER BY IIF(ist_aktiv, 0, 1)`)**
Ausgeführt über `emissionswert` (13 Zeilen). **Wirkung geprüft:** `ist_aktiv`-Folge
`[1, 0, 0, 0, …]` — der aktive Wert steht **oben**, die inaktiven folgen.
**Der Access-Workaround ist unter 0/1 nachweislich weiterhin korrekt:** Der Bestand
führt `DISTINCT ist_aktiv = [0, 1]` (keine −1 mehr). Damit würde zwar auch das früher
falsche `ist_aktiv DESC` wieder stimmen — `IIF` bleibt aber die kodierungsunabhängige
und deshalb dauerhaft richtige Form. Kein Handlungsbedarf.

**A3 `Controller/ProjektDuplizierenCtrl.cs:740` (`INSERT … SELECT` mit `IIF`)**
Auf einer **Kopie** ausgeführt, danach zurückgerollt. Bauform exakt wie
`BaueInsertSql`: `IIF([col] > 0, [col] + <offset>, [col])`.
Ergebnis: `Tab_StromspeicherVariante` 13 → 14 Zeilen; Quellwert
`ID_Energieanlage = 10354` erscheint in der Kopie als **910354** (Versatz 900000) —
**Versatzlogik korrekt**, `IIF` im `INSERT … SELECT` lauffähig.

> Die Auftragsliste nannte Zeile 728; das ist der Kopf der Methode `BaueInsertSql`.
> Der `IIF`-Ausdruck selbst steht in **Zeile 740**.

### 4b — Boolean-Literal-SQL: **19 Muster ausgeführt, alle grün**

Kein Umbau — nur Nachweis. Die 20 Laufzeitzeilen (Fundliste in
`MIGRATION_Pruefrezepte.md`, Rezept 3) sind über 19 repräsentative Muster abgedeckt
(`PufferSpCtrl:1034` und `:1035` bilden **eine** Abfrage).

| Kennung | Stelle | Ergebnis |
|---|---|---|
| B1 | Warnkriterien:713 (`WQ_Unbegrenzt = TRUE`) | 1 |
| B2 | EmissionenCtrl:641 (`ist_aktiv = TRUE`) | 81 |
| B3 | EmissionskatalogCtrl:67 (`ausgewaehlt = TRUE OR ist_pflicht = TRUE`) | 3 |
| B4 | EmissionskatalogCtrl:289 (`ist_aktiv = TRUE`) | 27 |
| B5 | EnergieEinheitenPruefung:492 (`energy_conversion.aktiv = TRUE AND factor > 0`) | 65 |
| B6 | EnergieEinheitenPruefung:579 (`energy_carrier.is_active = TRUE`) | 3 (Probe) |
| B7 | EnergieEinheitenPruefung:635 (`ID = ? AND aktiv = TRUE AND factor > 0`) | `('m3',)` |
| B8 | KostenPositionCtrl:170 (`IsMainComponent = True`) | 10 |
| B9 | KostenPositionCtrl:185 (`= False`) | 43 |
| B10 | KostenPositionCtrl:201 (`= False`) | 43 |
| B11 | KostenPositionCtrl:260 (`f.IsMainComponent = True`) | 10 |
| B12 | PufferSpCtrl:1034/1035 (3 × `= FALSE`, Klassen-Set-Leseprobe) | 0 (Abfrage läuft) |
| B13 | StromspeicherVarianteCtrl:76 (`Aktiv = TRUE`) | 7 |
| B14 | StromspeicherVarianteCtrl:234 (`Aktiv = FALSE`) | 6 |
| B15 | StromspeicherVarianteCtrl:239 (`Aktiv = TRUE`) | 7 |
| B16 | Form_KostenAdmin:35 (`= False ORDER BY Bezeichnung`) | 3 (Probe) |
| B17 | Form_KostenAdmin:119 (`= False`) | 43 |
| B18 | Form_KostenfaktorItem:29 (`IsMainComponent=false`, **ohne Leerzeichen**) | 3 (Probe) |
| B19 | ucFuelSettings:888 (`user_edited = TRUE`) | 0 (Abfrage läuft) |

**Deckungsbeweis `TRUE`/`FALSE` ≡ 1/0 gegen den 0/1-Bestand:**

| Tabelle | Spalte | `= TRUE` | `= 1` | `= FALSE` | `= 0` | DISTINCT |
|---|---|---|---|---|---|---|
| `Tab_StromspeicherVariante` | `Aktiv` | 7 | 7 | 6 | 6 | `[0, 1]` |
| `Tab_Kostenfaktor` | `IsMainComponent` | 10 | 10 | 43 | 43 | `[0, 1]` |
| `emissionswert` | `ist_aktiv` | 81 | 81 | 224 | 224 | `[0, 1]` |
| `energy_conversion` | `aktiv` | 65 | 65 | 0 | 0 | `[1]` |
| `Tab_Pufferspeicher` | `Nutzung_Heizung` | 31 | 31 | 2 | 2 | `[0, 1]` |

**Durchgängig gleich** — die Access-Wandlung −1 → 1 ist vollständig, `TRUE`/`FALSE`
selektieren korrekt.

**Wertposition:** `SELECT TRUE, FALSE` → `(1, 0)`, `typeof` je `'integer'`. Ein
`INSERT … VALUES(TRUE)` schreibt also **1** — passend zur Bestandskonvention. Damit sind
auch die 12 Wertpositions-Zeilen (Rezept 3b) abgedeckt.

**Sonderschreibweisen mitgezählt:** `= True`/`= False` (KostenPositionCtrl,
Form_KostenAdmin) und `=false` ohne Leerzeichen (Form_KostenfaktorItem:29) — beide laufen.

### 4c — `LIKE`: **9 Muster ausgeführt, alle grün**

Kein Umbau — nur Nachweis. 22 Laufzeitstellen, davon die geforderten Muster:

| Kennung | Stelle | Ergebnis |
|---|---|---|
| L1 | `PufferSpFilter.cs:47` (`Gesamtvolumen IS NULL OR Gesamtvolumen Like '%'`) | 5 |
| L2 | `PufferSpFilter.cs:136` (`Hersteller Like '%'`) | 5 |
| L3 | `BHKWCtrl.cs:16` (`Ptherm LIKE '%'`) | 7 |
| L4 | `BHKWStammCtrl.cs:24` (`Ptherm LIKE '%'`) | 79 |
| L5 | `Form_Heizkessel.cs:592` (`Ptherm Like '%'`) | 20 |
| L6 | `Form_Heizkessel.cs:611` (`Brennstoff Like '%'`) | 20 |
| L7 | `Form_BHKWEing.cs:179` (`Brennstoff Like '%'`) | 7 |
| L8 | `Form_PV.cs:221` (`Bezeichner Like '%'`) | 6 |
| L9 | `sqlite_master`-Filter (S4-Bestand, `name NOT LIKE 'sqlite_%'`) | 114 |

**Semantik-Nachweis (Allesfilter lässt NULL aus — wie unter ACE):**
`Tab_Pufferspeicher_STAMM.Hersteller` gesamt 5 = 5 Treffer + 0 NULL;
`Tab_Heizkessel.Ptherm` gesamt 20 = 20 + 0 NULL. Summe stimmt in beiden Fällen.

**Bestätigt:** Alle Stellen laufen über die Zugriffsschicht; die zwei Treffer in
`Views/Wärmepumpe/Kenndaten.cs:40/47` sind **`DataTable.RowFilter`** (ADO.NET im
Speicher), berühren die Datenbank nicht und gehören nicht in das DB-Rezept.

### Gesamtbilanz der Ausführung

**58 Nachweise, 0 Fehler** (26 TOP-Abfragen + 3 IIf + 19 Boolean + 9 LIKE + 1 Wertpositionsprobe).

---

## 7. Sweep-Gegenstand 5 — `GetMaxID` ohne `+1` (`ProjektPhotovoltaikCtrl.cs:211`)

**Befund: ABSICHT, kein Fehler. Kein Umbau.**

```
int datenId = DataRepository.GetMaxID("Tab_PreisreiheDaten");   // :211  (ohne +1)
for (int m = 0; m < n; m++)
{
    datenId++;                                                   // :214  <- das +1
    DataRepository.ExecuteSQL("INSERT INTO [Tab_PreisreiheDaten] (ID, …) VALUES (?, ?, ?)", …);
}
```

Das `+1` fehlt nicht — es ist in die **Schleife** gewandert. `datenId++` steht **vor**
dem `INSERT`, die erste vergebene ID ist also `MAX(ID) + 1`, genau wie bei der
Einzelform. Die Stelle vergibt **mehrere** IDs hintereinander; das laufende Inkrement ist
dafür die richtige Bauform. Zum Vergleich steht 12 Zeilen darüber (`:199`) die
Einzelform `GetMaxID(...) + 1` für den einen Kopfsatz.

**Dialektseitig unbedenklich:** `SELECT MAX(ID) FROM t` ist gültiges SQLite;
`GetMaxID` liefert bei leerer Tabelle 0 (→ erste ID 1). `AUTOINCREMENT` sichert die
Monotonie zusätzlich ab.

---

## 8. Rezept-Endstand

Beide Messskripte liegen jetzt dauerhaft unter `sql/tools/` (Kopfkommentare auf Zweck
und Aufruf umgeschrieben, Docstrings auf Roh-Strings umgestellt — beide laufen aus dem
neuen Ort **warnungsfrei**):

- `sql/tools/sql_dialekt_inventur.py`
- `sql/tools/typ_rueckweg_scan.py`

Die Sollwerte, Istwerte und die Fundlisten der bewussten Ausnahmen stehen in
**`sql/MIGRATION_Pruefrezepte.md`**.

| Rezept | Soll | Ist | Status |
|---|---|---|---|
| `SELECT TOP` außerhalb SchemaMigration | 0 | **0** | erfüllt |
| `SELECT TOP` in SchemaMigration (S6) | 3 | **3** | bewusste Ausnahme |
| `@@IDENTITY` ausführbar | 0 | **0** | erfüllt |
| Boolean-Vergleiche Laufzeit | laufen | **20 Zeilen / 19 Muster ausgeführt** | erfüllt |
| Boolean-Wertposition Laufzeit | schreibt 1/0 | **12 Zeilen, `TRUE`→1 bewiesen** | erfüllt |
| `LIKE` Laufzeit | verhaltensgleich | **22 Zeilen / 9 Muster ausgeführt** | erfüllt |
| `IIf` Laufzeit | 3, lauffähig | **3/3 ausgeführt** | erfüllt |
| übrige Access-Funktionen Laufzeit | 0 | **0** | erfüllt |
| harte `(int)`/`(bool)`-Casts | 0 | **0** | erfüllt |
| OleDb **ausführend** außerhalb Ausnahmen | 0 | **0** | erfüllt |

---

## 9. Abnahme

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| Build (`/t:Restore`, dann `Debug`/`x64`, OutputPath ins Scratchpad) | 0 Fehler, exakt 5 Bestandswarnungen | **0 Fehler, 5 Warnungen** |
| Probensuite `Proben/ZugriffsschichtProben` | 12/12 | **12/12** |
| `SELECT TOP` ausführbar außerhalb `SchemaMigration.cs` | 0 | **0** |
| Ausführungsnachweise gegen die Probendatenbank | alle grün | **58/58** |

**Die 5 Bestandswarnungen** (unverändert gegenüber S4, keine neue hinzugekommen):

- `CS0108` — `StromverbraucherStammCtrl.items` verdeckt `StromverbraucherModel.items`
- `CS0108` — `WErzeugerModel.ID_Projekt` verdeckt `WPModel.ID_Projekt`
- `CS0109` — `KlimaregionStammCtrl.items`: `new` nicht erforderlich
- `CS0109` — `KlimaregionStammCtrl.rows`: `new` nicht erforderlich
- `CS1998` — async-Methode ohne `await`

Der Build lief **nicht** nach `WindowsFormsApplication1\bin\x64\Debug`, sondern nach
`…/scratchpad/s5_bin/`.

---

## 10. Auffälligkeiten (alles, was sich nicht 1:1 erhalten ließ)

1. **`ProjektExportImportCtrl.AlsSqlLiteral` war ein echter, latenter Laufzeitfehler.**
   Das ist die einzige Stelle des Sweeps, an der sich das Verhalten **absichtlich ändern
   musste**: `#…#` ist in SQLite ein Syntaxfehler. Der Import-Literal-Fallback wäre für
   jede Zeile mit Datumsspalte gebrochen — und zwar **still**, weil `catch` den zweiten
   Fehler verwirft und die erste Ausnahme meldet. Umbau ist im Quelltext kommentiert.
   Der `bool`-Zweig wurde von `"True"/"False"` auf `1/0` gezogen; beides hätte
   funktioniert, `1/0` ist die zum Parameterweg deckungsgleiche Form.

2. **Der Auftragsumfang bei Gegenstand 1 war zu hoch angesetzt** (~36 statt 26).
   `WaermequelleClass` (3) und `PufferSpCtrl` (3) waren bereits in S4b umgestellt worden;
   die restlichen Treffer der alten Messung sind Kommentare und englischer Berichtstext.
   Auch die genannten Zeilennummern stammten aus einer älteren Messung. **Nichts wurde
   übersehen** — der Endstand ist per Rezept mit 0 belegt.

3. **`ProjektDuplizierenCtrl`: `IIF` steht in Zeile 740, nicht 728.** 728 ist der Kopf
   der `foreach`-Schleife in `BaueInsertSql`.

4. **Zeilennummern der Boolean-Fundliste weichen vom Auftrag ab**
   (`EnergieEinheitenPruefung` 492/579/635 statt 495/582/638; `KostenPositionCtrl`
   170/185/201/260 statt 209/224/240/299). Ursache ist dieselbe: ältere Messung. Die
   **Anzahl** stimmt exakt (20 Laufzeitzeilen).

5. **Eine Kategorie war im Auftrag nicht getrennt geführt:** `TRUE`/`FALSE` in
   **Wertposition** (`INSERT … VALUES(TRUE)`, `SET … = FALSE`) — 12 weitere
   Laufzeitzeilen. Sie sind harmlos (SQLite schreibt 1/0, genau die Bestandskonvention)
   und jetzt im Rezept als eigener Punkt 3b geführt, damit sie bei künftigen Sweeps nicht
   durchrutschen.

6. **Die LIKE-Zahl ist gewachsen, nicht geschrumpft:** 22 statt der im Konzept genannten
   20 Laufzeitstellen. Grund sind **fünf in S4 neu gebaute** `sqlite_master`-Abfragen
   (`name NOT LIKE 'sqlite_%'`) — bereits SQLite-nativ, kein Altbestand.

7. **Fünf `(string)`-Casts bleiben stehen** (`GebäudeKontextMenuCtrl` 109/111/114/115/116).
   Sie waren **nicht** Teil des Auftrags und sind **kein Migrationsrisiko**: Ein
   `(string)`-Cast auf `DBNull` warf unter ACE genauso wie unter SQLite. Ein Umbau wäre
   eine eigenständige Robustheitsverbesserung — bewusst **nicht** in S5 vorgenommen,
   damit der Sweep verhaltensneutral bleibt.

8. **Sechs `public OleDbCommand DBCommand`-Felder bleiben** (`RecordSet` und je ein
   Controller in BHKW, BHKW-Stamm, PV, Pufferspeicher, Solarkollektoren). Sie sind
   **kein Rest, sondern Absicht**: reine Parameterträger, die nie eine Verbindung
   bekommen (`RecordSet.cs:67-69` kopiert `DBCommand.Parameters` in ein
   `OleDbParameter[]` für die Zugriffsschicht). Aufräumen wäre kosmetisch und ist nicht
   Teil von S5.

9. **Vier `SELECT`-Abfragen lieferten 0 Zeilen** (E4, E6, E7, E26). Das sind
   **Datenbefunde**, keine Fehler: Das Probeprojekt hat kein BHKW-, Solar- und
   PV-Ergebnis und keine aktive Stromspeichervariante. Der Code behandelt genau diesen
   Fall.

10. **Zwei Messskript-Kopfzeilen mussten angepasst werden**, damit die Rezepte aus
    `sql/tools/` warnungsfrei laufen: Die Docstrings enthielten einen Backslash vor
    einem Leerzeichen (`WindowsFormsApplication1\ `), was Python als ungültige
    Escape-Sequenz meldet. Beide Docstrings sind jetzt Roh-Strings (`r"""`).
