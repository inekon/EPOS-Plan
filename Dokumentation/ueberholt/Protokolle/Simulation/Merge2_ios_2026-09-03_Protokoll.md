# Merge 2 `origin/ios_migration` → `ios_migration` (03.09.2026, nachmittags)

Zweite Zusammenführung desselben Strangs. Merge 1 hatte den **Umzug** nach `EPOS.Kern` /
`EPOS.UI` geholt; Merge 2 holt, was seither dazukam: **38 Remote-Commits** — iU9 (Blazor-
Dialoge, `Form_Kosten_VarAuswahl` gelöscht), iU10 (iOS-Hülle `EPOS.iOS` als **eigene**
Projektmappe), das **SQL-Dialekt-Audit** (11 Umschreibungen in 7 Dateien) und die
Wirtschaftlichkeitspakete FX2–FX5/B5.

* **Merge-Base:** `430a864` (der Remote-Elter von Merge 1 — unser Strang ist seither nicht
  neu verzweigt)
* **Lokal vorher:** `884ce7a` (16 Commits: Paket A/B, Projektdialoge, FS1, Merge-1-Nachweis)
* **Remote:** `71cde0c`
* **Merge-Commit:** `c2c64cb` — „Merge 2 origin/ios_migration (iU9/iU10, SQL-Dialekt-Audit):
  Paket A/B, FS1-N1 zusammengeführt"
* **Sicherungsreferenz:** Branch `sicherung/vor-merge2-2026-09-03` auf `884ce7a`
* **Nachweisanker:** Branch `merge2/ios-2026-09-03`
* Kein Push.

Der Arbeitsbaum war vor dem Merge sauber; es gab nichts vorab festzuhalten (anders als bei
Merge 1, wo `b9c566f` nötig war).

---

## 1. Berührungsfläche

Unsere 16 Commits ändern 65 Dateien (ohne `Referenzlaeufe/`), die 38 Remote-Commits 111.
**Acht Dateien liegen in beiden Mengen:**

| Datei | Auflösung |
|---|---|
| `EPOS.Kern/Allgemein/DbWerte.cs` | automatisch (getrennte Konstantenblöcke) |
| `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | automatisch (unsere PV-Andockung / Remotes FX2–FX5) |
| `EPOS.Kern/Controller/KostenProjektPositionenCtrl.cs` | **KONFLIKT** — siehe 2. |
| `EPOS.Kern/Controller/PhotovoltaikCtrl.cs` | automatisch (Remote änderte nur einen Kommentar) |
| `EPOS.Kern/Controller/SolardatenCtrl.cs` | automatisch (unser Ortszeit-Lesepfad / Remotes Spaltennamen im Schreibpfad) |
| `EPOS.Kern/MyResource/Resource.resx` | automatisch, Vereinigung geprüft |
| `EPOS.Kern/MyResource/Resource.en-US.resx` | automatisch, Vereinigung geprüft |
| `WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs` | automatisch (unser `Form_PVModell` bleibt, Remotes `Form_Kosten_VarAuswahl` fällt weg) |

**Keine Umbenennungen** diesmal — der Umzug war mit Merge 1 abgeschlossen. Die iOS-Hülle
`EPOS.iOS` steht in einer **eigenen** Projektmappe; `WP-Plan.sln` führt unverändert
dieselben zwölf Projekte und nennt „iOS" an keiner Stelle. Es war also nichts an der
Windows-Projektmappe anzupassen, und MAUI-Workloads waren nicht nötig.

### 1.1 Was automatisch zusammenging — und wie es nachgewiesen ist

Für jede der sieben automatisch aufgelösten Dateien wurde gegengeprüft, dass **beide**
Seiten wirklich angekommen sind: jede in `430a864..884ce7a` bzw. `430a864..origin/ios_migration`
**hinzugefügte** Zeile steht im Merge, jede **gelöschte** ist verschwunden.

| Datei | unsere +/− | Remote +/− | fehlend / zurückgeblieben |
|---|---|---|---|
| `DbWerte.cs` | 47 / 0 | 42 / 6 | 0 / 0 |
| `WirtschaftlichkeitCtrl.cs` | 14 / 1 | 351 / 23 | 0 / 0 |
| `PhotovoltaikCtrl.cs` | 22 / 7 | 7 / 0 | 0 / 0 |
| `SolardatenCtrl.cs` | 113 / 0 | 14 / 3 | 0 / 0 |
| `HilfeKontext.cs` | 3 / 0 | 0 / 1 | 0 / 0 |
| `KostenProjektPositionenCtrl.cs` | 25 / 5 | 17 / 5 | 0 / 0 |

> Drei Treffer bei `PhotovoltaikCtrl.cs` („`Laenge = ?,`", „`Breite = ?,`", „`WHERE`") sind
> Scheinbefunde des zeilenweisen Vergleichs: Wir haben an diesen Zeilen nur nachlaufende
> Leerzeichen entfernt, der Text selbst steht unverändert an seiner Stelle.

### 1.2 Ressourcen — die Vereinigung ist gezählt, nicht geglaubt

Die Merge-1-Stolperstelle (fehlendes `</data>` an der Naht) konnte diesmal nicht auftreten,
weil Git beide Blöcke ohne Konflikt zusammenlegte. Trotzdem geprüft, in **beiden** Sprachen:

* **XML wohlgeformt** (`[xml]`-Ladeprobe): ja, je **2 901** `data`-Elemente.
* **Schlüsselmengen:** Basis 2 742 + unsere 58 + Remotes 105 = **2 905** Vorkommen; keiner
  fehlt, keiner ist doppelt, keiner ist erfunden (Differenz 2 905 ↔ 2 901 sind vier
  `<data name=`-Zeichenfolgen **innerhalb** von Werten — sie stand schon vor dem Merge so).
* **de und en führen dieselbe Schlüsselmenge** (Differenzmenge leer).

---

## 2. Der Doppel-Fix: `KostenProjektPositionenCtrl.AnkerNachziehen`

Der einzige Konflikt, ein Hunk. **Beide Seiten haben denselben Fehler unabhängig gefunden
und behoben** — den Access-Verbund `UPDATE … INNER JOIN … SET`, den SQLite mit
„near INNER: syntax error" abweist:

* **Wir**, `5fa2b18` (FS1 N-1, Cutover-Befund 02.09.): je Komponente ein Fehlerdialog beim
  **Speichern der Erzeugerliste**; Nachweis 28 → 0 Fehler
  (`FS1_N1_AnkerNachziehen_Protokoll.md`).
* **Remote**, `dd4113f` (SQL-Dialekt-Audit): derselbe Fehler beim **Anlegen eines
  Heizkessels**.

Beide Fassungen ersetzen den Verbund durch dasselbe Mittel: ein **korreliertes UPDATE** mit
**EXISTS-Schutz**. Das erzeugte `SET`-SQL ist auf beiden Seiten **zeichengleich**; der
einzige Unterschied liegt in der Auswahlliste der EXISTS-Unterabfrage (`SELECT 1` bei uns,
`SELECT a.[<Spalte>]` bei Remote) — semantisch gleichwertig, denn `EXISTS` fragt die
Existenz der Zeile ab, nicht den Wert.

**Übernommen wurde unsere Fassung.** Begründung — sie ist die fachlich vollständigere:

1. **`SELECT 1` in EXISTS** sagt, was gemeint ist. Bei `SELECT a.[<Spalte>]` muss der Leser
   erst selbst herleiten, dass ein **NULL** in der Geräte­spalte den Schutz *nicht*
   aushebelt. Die Wirkung ist dieselbe, die Aussage ist klarer.
2. **Der Kommentar trägt die Herleitung**: dass `a.ID` Schlüssel ist und die Unterabfrage
   darum höchstens eine Zeile liefert; dass Positionen ohne (gültige) Anlage dieses
   Projekts unberührt bleiben — genau die Eigenschaft, die das Duplizierer-Szenario
   „verstellter Anker" abdeckt (Ä24: der Duplizierer versetzt `ID_Anlage`, den
   komponentenabhängigen Anker kann er nicht kennen).
3. **Der Hinweis auf Schritt 47** steht nur bei uns: `SchemaMigration` erledigt dasselbe
   einmalig für den Bestand und **behält im eingefrorenen Access-Zweig seine JOIN-Form**.
   Ohne diesen Satz lädt die Stelle beim nächsten Audit dazu ein, auch dort umzuschreiben.
4. Die benannte Zwischengröße `komponentenId` statt zweier `Convert.ToInt32(r["ID"])`.

**Remotes Beitrag geht nicht verloren:** Sein zweiter Fundort ist als Kommentarzeile
übernommen — „schon das ANLEGEN eines Heizkessels löste ihn aus". Zwei unabhängige
Reproduktionen desselben Befunds sind mehr wert als eine.

Die Datei ist UTF-8 **mit** BOM und CRLF; aufgelöst wurde byte-erhaltend (Perl, `:raw`),
nachgemessen: BOM erhalten, 0 reine LF, 231 Nicht-ASCII-Bytes unverändert. Beide Fassungen
(Konflikt und Auflösung) liegen unter `P:\merge2\aufloesungen\`.

## 2.1 Was das SQL-Dialekt-Audit sonst angefasst hat

Elf Stellen in sieben Dateien, davon eine in unserer Schnittmenge (`SolardatenCtrl`, dort
aber im **Schreib**pfad `Insert`/`WriteDataTable`: `Außen_Temp` → `Temperatur`,
`ID_Klimaregion`/`Name` → `ID`/`Bezeichner`). Unser Paket-A-Lesepfad `ReadOrtszeit` liegt an
anderer Stelle derselben Datei und wurde nicht berührt. Remote hat ausdrücklich **nicht**
angefasst, was schon SQLite-gültig ist, und die drei `UPDATE`-JOINs in `SchemaMigration.cs`
(Access-Zweig) stehen gelassen — das passt zu unserer Entscheidung unter 2.

**Migrationsstand:** Remote steht weiterhin auf `ZIEL_VERSION = 61`; unser 62/63 kollidiert
mit nichts. Nach dem Merge: `ZIEL_VERSION = 63`, `FREEZE_VERSION_ACCESS = 61` (von der
Migrationsprobe gegengeprüft).

**Gelöschte Maske:** Remote hat `Form_Kosten_VarAuswahl` entfernt. Im ganzen Baum verweist
nichts mehr darauf außer fünf **Kommentaren**, die die Ablösung erklären — kein
Kompilierbezug.

---

## 3. Nachweis

### 3.1 Build — dreimal, mit demselben Werkzeug

MSBuild aus VS 18 Community, `-restore /p:Platform=x64`, `WP-Plan.sln` (zwölf Projekte).

| Stand | Ergebnis |
|---|---|
| Merge, Worktree `P:\wt2` | **0 Fehler** |
| Merge, `git archive HEAD` → `P:\merge2\src` | **0 Fehler** |
| `origin/ios_migration` pur, `git archive` → `P:\merge2\theirs` | **0 Fehler** |
| Hauptbaum nach der Übernahme (`bin\x64\Debug`) | **0 Fehler** |

**Warnungsprofil in allen vier Läufen identisch:** WFO1000 24, NU1510 4, CS0109 2, CS0108 2,
WFO0003 1, CA2255 1. Schärfer noch: Die **29 datei- und zeilengenauen** Warnungen von
Merge und reinem `origin/ios_migration` sind Zeile für Zeile dieselben — **aus unseren
Dateien kommt keine einzige neue Warnung.**

> Gegenüber Merge 1 fällt WFO1000 von 28 auf 24. Das ist Remotes Löschung von
> `Form_Kosten_VarAuswahl` und Verwandtem, nicht unsere Seite: Merge und THEIRS zeigen
> denselben Wert.

### 3.2 Referenzlauf — der Dreifach-Nachweis

Werkzeug `Referenzlauf/`, 14 Projekte
(1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1026, 1028, 1029, 1030, 1039, 1043),
Quelle `P:\pa0\Quelle\Kenndaten.sqlite` (MD5 `47bcefaca0f18d2180ba37786c6cb6b3`, gemessen),
je **frische** Arbeitskopie; die Arbeitskopie des Merge-Laufs migriert 61 → 63, die des
THEIRS-Laufs bleibt auf 61.

| Vergleich | Ergebnis |
|---|---|
| **MERGE (M2) gegen M1** (`2026-09-03_M2_nach-Merge2` ↔ `2026-09-03_M1_nach-Merge`) | **355/355 bitgleich** (MD5), 0 ungleich, keine Datei nur auf einer Seite |
| **THEIRS (`71cde0c`) gegen THEIRS (`430a864`)** (`P:\merge2\theirs_lauf` ↔ `P:\merge\theirs_lauf`) | **355/355 bitgleich** |
| Toleranzvergleich M1 → M2 | **14/14 PASS**, 3 882 476 Werte |
| `pruefen` auf M2 | **plausibel**, dieselben drei Bestandshinweise |

**Einordnungstabelle (Datei | M1=MERGE? | THEIRS abweichend? | Einordnung): leer.**

Genau dafür war der zweite Vergleich gedacht: Das SQL-Dialekt-Audit hat SQL **umgeschrieben**,
und trotzdem rechnet der reine Remote-Stand `71cde0c` byte-für-byte dasselbe wie `430a864`.
Die umgeschriebenen Stellen liegen sämtlich auf Pfaden, die der Referenzlauf nicht betritt
(Schreibwege ohne Aufrufer, Katalogpflege, Preisreihen-Rückfallebene). Und weil zugleich
MERGE = M1 exakt gilt, hat auch die Zusammenführung nichts verschoben. **Beide Achsen sind
exakt** — es gibt keine Abweichung, die einzuordnen wäre.

Der Lauf selbst: 14 von 14 erfolgreich, 26 Warnungen, 0 Fehler — dieselben Zahlen wie M1.

### 3.3 Harness Paket A und B (gegen den Merge-Build)

| Probe | Ergebnis |
|---|---|
| `rein` — Zeitbasis (PA1) | **18 PASS, 0 FAIL** |
| `rein` — Modell (PB1): Huld, Hay-Davies, Kennlinie, Clipping, Degradation, Technologie | **58 PASS, 0 FAIL** |
| `zeitbasis` an der DB-Kopie (14 Klimaregionen) | **115 PASS, 0 FAIL** |
| `migration` 61 → 62 → 63 auf frischer Kopie, Zweitlauf idempotent, kein DML | **24 PASS, 0 FAIL** |
| INEKON „Schulung 01", Prüfstand `kd1runner` Modus `pv6` | **28 PASS, 0 FAIL** (I3 −0,76 %, I4 −0,47 %) |

Zusammen **243 PASS, 0 FAIL** — Probe für Probe dieselben Zahlen wie bei Merge 1, samt der
INEKON-Abstände auf zwei Nachkommastellen.

Die Harness-Vorlagen mussten diesmal **gar nicht** angepasst werden; es genügte, die drei
Merge-1-Anpassungen (Referenz auf `EPOS.Kern.dll`, `DataRepository.PfadUeberschreibung`,
`SolardatenCtrl` über `typeof(DataRepository).Assembly`) mitzunehmen und den Pfad
`P:\merge\src` auf `P:\merge2\src` zu ziehen.

> **Fallstrick beim Nachbauen — bitte lesen.** Der Modus `pv6` **migriert nicht**; er setzt
> nur `DataRepository.PfadUeberschreibung`. Auf einer Kopie im Schemastand **61** meldet er
> „no such column: Degradation" und liefert **24 PASS, 4 FAIL** (P0, P2–P4) — vier
> Folgefehler eines einzigen fehlgeschlagenen `Speichern`, **kein** Befund am Code. Der Lauf
> braucht eine Kopie im Stand **63**; hier wurde die migrierte Kopie der Migrationsprobe
> verwendet. Der erste Versuch dieses Merges lief genau in diese Falle und ist nur deshalb
> hier vermerkt, damit es dem Nächsten nicht wieder passiert.

Die produktive Datenbank `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` blieb unberührt
(Zeitstempel **02.09.2026 22:07:36**, vorher wie nachher gemessen).

### 3.4 Übernahme in den Hauptbaum

Der Hauptbaum stand unverändert auf `884ce7a` und war sauber; die Übernahme lief deshalb als
**`git merge --ff-only merge2/ios-2026-09-03`** — ein reines Vorspulen, kein zweiter
Merge-Commit. Der Worktree `P:\wt2` ist entfernt, der Branch bleibt als Anker.
`core.longpaths` war nur für `git worktree add` auf dem subst-Pfad gesetzt und ist wieder
entfernt.

---

## 4. Offene Punkte

* **Sichtabnahme am Programm** steht aus. Neu hinzugekommen und noch nie am laufenden
  Programm gesehen: die Blazor-Dialoge aus iU9 (Heizkessel und BHKW öffnen jetzt den
  Dialog „Energieträger-Variante"; `Form_Kosten_VarAuswahl` ist gelöscht), der Dialog
  BHKW-Wirtschaftlichkeit (B5) und die FX2–FX5-Änderungen an der Wirtschaftlichkeit.
  **Aus Merge 1 unverändert offen:** Löschdialog über `Dienste.Navigation` →
  `Projektloeschwahl`, die PV-Masken `Form_PV`, `Form_PVModell`, `Form_AdminPV`.
* **Vom Remote selbst als offen gekennzeichnet** (Commit `26c4d10`, „Nachweis Windows"):
  Kosten → Position mit **neuer** Gruppe (die Gruppe muss danach in der Auswahlliste
  stehen); Preisreihen anlegen/einlesen/löschen auf einer DB **ohne** `Tab_PreisreiheDaten`
  (danach darf keine Wertzeile bleiben); Klimadaten → Region löschen; Wärmepumpe-Test →
  Knopf „Kühlung". Diese vier Pfade betritt der Referenzlauf nicht — sie sind der Grund,
  warum er trotz umgeschriebenem SQL bitgleich bleibt, und zugleich der Grund, warum sie
  von Hand geprüft werden müssen.
* **Nebenbefund am Prüfwerkzeug, nicht am Programm:** `kd1runner` **ohne** Modusargument
  (der Migrationsweg) stirbt seit dem Umzug an einer `NullReferenceException` — er sucht
  `WindowsFormsApplication1.Properties.Settings` per Reflexion in der Assembly von
  `SchemaMigration`, die Einstellungen sind aber mit dem Kern gewandert. Der Modus `pv6`
  ist davon nicht betroffen (er nimmt `PfadUeberschreibung`). Reparatur auf Zuruf: dieselbe
  Zeile wie in `Program.Vorbereiten`.
* **Kein Push.** `sicherung/vor-merge2-2026-09-03` und `merge2/ios-2026-09-03` sind lokale
  Anker und können nach der Abnahme entfallen. Die Merge-1-Anker
  (`sicherung/vor-merge-2026-09-03`, `merge/ios-2026-09-03`) stehen ebenfalls noch.
* Schemastand bleibt **63**; `.wpx`-Pakete mit Stand 62 werden abgewiesen — systemimmanent.
* `sql/pv_katalog/` (Reparaturskript Isc-Signatur) ist weiterhin **nur mitgeführt**, nicht
  ausgeführt; die Freigabe steht aus.
* Aus dem Bestand unverändert offen: Bestätigung der Degradation „vermiedener Bezug",
  Katalogpflege Technologie/T_NOCT, E3 zurückgestellt.
