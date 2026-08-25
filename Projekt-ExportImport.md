# Projekt Export / Import (.wpx) — Kontext für Claude Code

Feature der WinForms-Anwendung (INEKON / WP-Plan, .NET 8, Access via OLE DB / ACE,
PlatformTarget x86, RootNamespace `WindowsFormsApplication1`). Exportiert **ein Projekt**
in eine portable Datei und importiert es wieder — auch in eine **andere, artgleiche**
Access-Datenbank.

> Diese Datei ist eine Handover-Notiz: Sie hält den Zweck, den Ablauf und vor allem die
> ACE-Fallen fest, die bei der Umsetzung teuer waren. Wer hier etwas ändert, sollte den
> Abschnitt „ACE-Lektionen" gelesen haben.

---

## 1. Umfang & Dateien

| Datei | Rolle |
|---|---|
| `ProjektExportImportCtrl.cs` | Kernlogik: Export in `.wpx`, Import (auch in fremde DB). |
| `Form_ProjektExportImport.cs` | Dialog (Tab „Exportieren" / „Importieren"), reiner Code, kein `.resx`. |
| `ProjektDuplizierenCtrl.cs` | Liefert den generischen Kopierplan (`ErmittlePlan`) und die FK-Zielauflösung (`ErmittleZieltabelle`). |
| `ProjektCtrl.cs` | Projektauswahlliste im Export-Tab. |
| `DataRepository.cs` | Zentrale DB-Zugriffe (ACE). **Achtung:** meldet Fehler per MessageBox (`FehlerMelden`). |

**Voraussetzung:** In `ProjektDuplizierenCtrl` müssen `class Spec`, `ErmittlePlan(conn,trans)`
und `ErmittleZieltabelle(tab,col,pk)` **internal** (statt private) sein, damit
`ProjektExportImportCtrl` sie wiederverwenden kann. Einstieg z. B.:
`public static int ZeigeExportImportDialog(IWin32Window owner = null)`.

---

## 2. Dateiformat `.wpx`

ZIP (`System.IO.Compression`) mit JSON (`System.Text.Json`):

```
manifest.json          Format/Version, sourceProject, schemaVersion, Listen: tables, catalogs, fill
data/<Tabelle>.json    Projekt-Tabellenzeilen (gefiltert auf das Projekt)
catalogs/<Tab>.json    Referenzierte Zeilen konfigurierter Natural-Key-Kataloge
fill/<Tab>.json        Referenzierte Zeilen sonstiger (globaler) Kataloge, per Original-ID aufzufüllen
```

`FORMAT_VER` = Version des Datei-Layouts (bei inkompatiblen Änderungen hochzählen).
`SCHEMA_VER` = Stempel des Access-Datenmodells (aktuell fest `29`; siehe „Offene Punkte").

---

## 3. Ablauf

**Export:** Plan aus `ProjektDuplizierenCtrl.ErmittlePlan` → je Projekt-Tabelle Zeilen lesen
und nach `data/` schreiben → referenzierte Kataloge einsammeln (konfiguriert → `catalogs/`,
sonstige über echte FKs → `fill/`) → `manifest.json`. Läuft in einer Transaktion mit
abschließendem **Rollback** (Export ist read-only).

**Import (`Importieren`)** in dieser Reihenfolge:
1. Echte Access-Beziehungen (`_fks`) lesen — **auf frischer Verbindung VOR** der Transaktion.
2. Katalog­auflösung per natürlichem Schlüssel → `katMap` (alt_ID → neu_ID).
3. „fill": fehlende globale Katalogzeilen unter ihrer **Original-ID** auffüllen.
4. Offsets je Projekt-Tabelle bestimmen (`MAX(Ziel.pk) − MIN(Quelle.pk) + 1`).
5. Zeilen einfügen in FK-Reihenfolge, Spalten-Schnittmenge, alles umschlüsseln
   (PK/`ID_Projekt` + Offset, Katalog-FKs über `katMap`).
6. **AutoWert-Reseed** (`ReseedAutoWerte`) nach dem Commit.

---

## 4. ACE-Lektionen (die eigentlichen Stolpersteine)

Diese Punkte haben in der Praxis wiederholt Fehler erzeugt; jeder ist im Code gelöst:

- **Kein 64-Bit-Integer in Access.** Ein `Int64`-Parameter an eine „Long Integer"-Spalte
  scheitert mit *„data value could not be converted …"*. → `Passe`/`AlsDbWert` wandeln
  `Int64` → `Int32` (bzw. `Double`, falls zu groß).
- **`DateTime` braucht `OleDbType.Date`.** Ohne expliziten Typ bindet ADO.NET `DBTimeStamp`;
  Access erwartet „Date" → Fehler **3464** („Datentypenkonflikt in Kriterienausdruck")
  trotz passender .NET-Typen. → `MacheParam` setzt den `OleDbType` je Zielspaltentyp.
- **Leere / verwaiste FK-Werte → NULL.** Ein leerer Text-FK (z. B. `Gruppe = ""`) verletzt
  die referenzielle Integrität, da kein Elterndatensatz `""` hat. Wert ohne passenden
  Elterndatensatz wird auf `NULL` gesetzt (leerer Verweis = kein Verweis). Nicht-leere
  fehlende Namen werden per „Anlernen" (`StelleTextKatalogSicher`) im Zielkatalog angelegt.
- **Expliziter AutoWert-PK: Parameter vs. Literal.** ACE akzeptiert das Einfügen eines
  expliziten AutoWert-PK als **Literal** (wie die Duplizierung via `INSERT … SELECT`),
  lehnt ihn über einen gebundenen **Parameter** aber ab. → `FuehreInsertAus` ist
  **zweistufig**: erst mit Parametern (sicher für Text/Memo/Datum, z. B. `Tab_Projekt`),
  bei Fehler Fallback mit **literalen** Werten (`AlsSqlLiteral`; Datum im US-Format
  `#MM/dd/yyyy HH:mm:ss#`, Text mit `'`-Escaping).
- **AutoWert-Zähler nachziehen (Fehler 3022).** Nach dem Einfügen expliziter IDs führt ACE
  den Zähler NICHT nach → nächster regulärer Insert der App bekäme eine schon vergebene ID.
  → `ReseedAutoWerte` setzt den Zähler auf `MAX+1` (`ALTER TABLE … COUNTER(max+1,1)`),
  **nur für echte AutoWert-Spalten** (via ADOX spät gebunden erkannt; ohne ADOX passiert
  nichts). Beziehungsgebundene Eltern-Spalten (z. B. `Tab_Projekt.ID`) lehnt `ALTER` ab —
  das wird still übergangen; Restrisiko dort löst „Komprimieren und Reparieren".
- **Schema-Rowset & zweite Verbindung.** `GetOleDbSchemaTable(Foreign_Keys)` **vor** der
  Transaktion auf eigener Verbindung lesen. Während einer offenen Import-Transaktion KEINE
  zweite Verbindung zur selben Datei öffnen (Lock/Hänger). FK-Existenz nur gegen einen
  **einmal geladenen, gecachten** Elternschlüssel-Satz prüfen.
- **`DataRepository` zeigt MessageBoxen.** `ExecuteSQL`/`ExecuteScalar` melden Fehler per
  Dialog. Für erwartbar scheiternde Aufrufe (Reseed-`ALTER`) daher **eigene Verbindung**
  nutzen, damit Fehler still im `catch` landen.

---

## 5. Katalog- & Fremdschlüssel-Konzepte

- `KATALOG_SPALTE_ZU_TABELLE` — welche FK-Spalte auf welchen **nicht kopierten** Katalog
  zeigt (fest verdrahtet). Größtenteils zur Laufzeit aus `Foreign_Keys` ableitbar.
- `KATALOG_NATURALKEY` — der fachlich eindeutige Schlüssel je Katalog (z. B.
  `energy_carrier → name`), über den Katalogzeilen in einer fremden DB wiedergefunden werden.
- **fill** (`FuelleKatalog`) — referenzierte Zeilen eines globalen Katalogs unter ihrer
  Original-ID auffüllen, falls im Ziel fehlend (keine Umschlüsselung).
- **Anlernen** (`StelleTextKatalogSicher`) — fehlenden Namen im Zielkatalog per
  „insert if not exists" anlegen (wie die vorhandene Lern-Logik der App).

---

## 6. Verwandtschaft mit dem Migrations-Tool (AccessMigration, eigenständig)

Beide bewegen Access-Daten mit AutoWert-Surrogatschlüsseln zwischen DBs, ohne FKs zu
zerreißen — das Tool für die **ganze DB** (alt → neue Versions-Vorlage, Kataloge inklusive),
diese Klasse für **ein Projekt** (Kataloge werden nicht kopiert, sondern per Name gefunden).

| Konzept | Hier | Migrations-Tool |
|---|---|---|
| Natürlicher Schlüssel statt Autowert-ID | `KATALOG_NATURALKEY` (fest) | `matchColumns` in `migration.config.json` (+ Auto-Ableitung aus Unique-Indizes) |
| FK-Umschlüsselung alt_ID → neu_ID | über natürlichen Schlüssel des Elterns | dito, immer aktiv |
| Original-IDs behalten | „fill" (`FuelleKatalog`) | `preserveIdTables` |
| Wertkonvertierung auf Zielspaltentyp | `Passe()` | `ValueCoercion.Coerce()` |
| Fehlerhafte Zeilen überspringen | Selbstheilung (`NulleVerwaisteFks` + Retry) | `skipRowsOnError` |
| AutoWert-Zähler nachziehen (3022) | `ReseedAutoWerte()` | `AutoNumberReseeder` |
| Umbenennungen (Tabelle/Feld) | **noch nicht** | `tableRenames` / `columnRenames` |

Das Tool leitet natürliche Schlüssel selbst aus den **Unique-Indizes** des Schemas ab; seine
JSON listet nur **Ausnahmen** (Junction-Tabellen über FK-Paar; `Tab_Typ_Energieanlagen`
bewusst über `ID`). Das bestätigt: `KATALOG_SPALTE_ZU_TABELLE` und der Großteil von
`KATALOG_NATURALKEY` sind zur Laufzeit ableitbar; in eine JSON gehörten nur echte Sonderfälle.

---

## 7. Offene Punkte / TODO

- **Umbenennungen** (Tabelle/Feld) über Software-Updates werden noch nicht behandelt:
  Der Import matcht über den Namen → umbenannte Felder gehen still verloren. Lösung:
  Alias-Map (`tableRenames`/`columnRenames`) beim Spalten-Mapping anwenden.
- **Neue Pflichtfelder (NOT NULL ohne Default)** lassen einen alten Export scheitern.
  Team-Konvention: neue Spalten nullable / mit Default migrieren.
- **Skip-/Drift-Report:** sichtbar machen, welche Tabellen/Spalten beim Import übersprungen
  wurden (statt stillem Datenverlust).
- **`SCHEMA_VER`** ist fest `29`; besser dynamisch aus der DB lesen und bei Abweichung nur
  **warnen** (Import ist drift-tolerant). Format-Schranke: neuere `formatVersion` ablehnen.
- **Verschachtelte Cross-DB-Auflösung:** `energy_conversion` nutzt in seinem natürlichen
  Schlüssel `id_brennstoff` (selbst eine ID) → für fremde DBs müsste zuerst der Brennstoff
  über seinen Namen aufgelöst werden. Innerhalb derselben DB unkritisch.
- **Vereinheitlichung:** fest verdrahtete Listen künftig durch dieselbe
  `migration.config.json` ersetzen (`matchColumns`, `tableRenames`, `columnRenames`) plus
  Laufzeit-Ableitung aus Schema/Unique-Indizes; die Listen bleiben als Fallback.

---

## 8. Konventionen für Änderungen

- Dateien als **UTF-8 mit BOM + CRLF** speichern (Umlaute, z. B. `ID_Energieträger`).
- Nach Änderungen Klammer-Balance prüfen.
- Neue Import-Schritte immer **transaktional** halten; teure Prüfungen nur im Fehlerfall.
