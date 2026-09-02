# S7 — Verhaltensbeweis: Referenzläufe auf beiden Backends

**Datum:** 02.09.2026 · **Repo:** `WP-Plan`, Branch `sqlite`
**Referenz:** `Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`, Abschnitt 7
**Vorstand:** S0–S6 fertig (App baut gegen SQLite, Schemapflege startet auf SQLite, Proben 15/15)
**Nicht Gegenstand:** Bedienbeweis (7.3), Frontend-Beweis (7.4), Verteilung (S8)

**Ergebnis in einem Satz:** Das Datenpaar steht (Migration 114/114, alle Prüfsummen gleich,
Exit 0) und die **A-Seite (Access) rechnet alle 10 Referenzprojekte fehlerfrei** — die
**B-Seite (SQLite) bricht in 10 von 10 Projekten ab**, an **zwei Codestellen-Klassen**, die
der Dialekt-Sweep S5 nicht erfasst hat. Ein Wertevergleich A↔B ist deshalb noch nicht
möglich; die Frage „Double → REAL bitgleich?" ist **unbeantwortet, nicht negativ
beantwortet**.

> **NACHTRAG 02.09.2026, 14:00 — der Satz oben gilt nur noch für den ersten Durchgang.**
> B1 und B2 sind behoben, der B-Lauf ist gegen dasselbe Datenpaar wiederholt worden:
> **10 von 10 Projekten, 234 CSV, und der Vergleich gegen den unveränderten A-Lauf ist
> byte-identisch — 0 Abweichungen ohne jede Toleranz.** Die Frage „Double → REAL
> bitgleich?" ist damit **beantwortet: ja.** Alles dazu in **Abschnitt 10**; die
> Abschnitte 1 bis 9 bleiben als Befundlage des ersten Durchgangs unverändert stehen.

---

## 1. Datenpaar — ein Quellstand, zwei Dateien

Wächter vor der Kopie: **keine `Kenndaten.laccdb`** in `C:\ProgramData\EPOS_PLAN\`, kein
laufender Anwendungsprozess. Die produktive Datenbank wurde ausschließlich **gelesen**.

| | Wert |
|---|---|
| Quelle | `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` |
| Größe / Stand | 151 949 312 Bytes · 01.09.2026 15:16:48 · Schemastand **61** (= Zielstand) |
| SHA-256 Quelle | `271279C0A6112D4667DEDD1CA8C6A1C58A2713C30F8CF95F58DDDAA957939FAA` |
| Eingefrorene Kopie | `<Scratch>\s7\Kenndaten_S7.accdb` — **byte- und hashgleich** |
| SHA-256 **nach** dem A-Lauf | `271279C0…939FAA` — **unverändert**, also keine Datendrift zwischen den beiden Läufen |

**Migration** (`EposSqliteMigrator.exe --quelle … --ziel … --bericht …`, 13:30:43):

| Kennzahl | Wert |
|---|---|
| Tabellen migriert | **114 / 114** |
| Zeilen gesamt (Ziel) | 1 392 013 |
| Datenbeweis | **114/114 gleiche Zeilenzahl und gleiche Prüfsumme** |
| `integrity_check` | ok |
| `foreign_key_check` | keine Verletzung |
| Case-Drift (D5) | 0 Befunde in 31 Textschlüsseln |
| nicht migriert | 0 Quelltabellen |
| Dauer / Zielgröße | 13,28 s · 64,6 MB |
| **Exit-Code** | **0** |
| Werkzeugversion | `1.0.0+cb6d5003a7259b27a80bf8ed8286220c121bfa1f` |
| Bericht | `<Scratch>\s7\S7_Migrationsbericht.md` (228 Zeilen, Tabelle je Tabelle) |

Damit ist der **Strukturbeweis (7.1)** für dieses Paar erbracht.

---

## 2. Bauweise der beiden Seiten

Beide Seiten benutzen **denselben Suite-Quellstand**: `git diff 7d41833 cb6d500 --
WindowsFormsApplication1/ Referenzlauf/` ist **leer**, und die zehn Dateien in
`Referenzlauf/` waren vor dem Umbau in Worktree und Hauptbaum **SHA-256-gleich**. Der
einzige inhaltliche Unterschied zwischen A und B ist damit die Zugriffsschicht (plus die
unter 2.3 beschriebene Suite-Erweiterung).

### 2.1 A-Seite — Access, eingefrorener Stand

| | Wert |
|---|---|
| Baum | Wegwerf-Worktree `C:\Users\DirkEngelmann\s7a`, **detached**, `git -c core.longpaths=true worktree add` |
| Commit (`git rev-parse HEAD`) | **`7d418333c5bb99dc825fffb049b5bcc1a064db36`** (= `main`) |
| Arbeitsbaum | sauber (0 Zeilen `git status --porcelain`) |
| Zugriffsschicht | `DataRepository.DB_DATEINAME = "Kenndaten.accdb"`, `Provider=Microsoft.ACE.OLEDB.12.0` |
| Bau | VS-2022-MSBuild 17.14.51, `-restore -p:Configuration=Debug -p:Platform=x64 -p:OutputPath=C:\Users\DirkEngelmann\s7run\A\bin\` |
| Ergebnis | **0 Fehler**, exakt die 5 bekannten Bestandswarnungen (2× CS0109, 2× CS0108, 1× CS1998) |
| Worktree | nach dem Lauf mit `git worktree remove --force` + `prune` **entfernt** (Sync-Automatik) |

> **Nicht benutzt:** die vorhandene EXE unter `Referenzlauf\bin\x64\Debug\` (29.08.2026
> 01:45). Sie ist **älter als der Access-Freeze-Stand** — zwischen dem 29.08. und
> `7d41833` sind u. a. `2ab47b1` (BK3), `ac296e3` (Stromsteuer aus dem Gesetzeskatalog),
> `109e71f` (BK2) und `e02aab3` (B3b, KWKG-Nettostrom) auf `main` gelandet. Ein Lauf
> daraus hätte **Codedrift als Backenddrift** ausgewiesen. Deshalb der Worktree-Bau.

### 2.2 B-Seite — SQLite, aktueller Baum

| | Wert |
|---|---|
| Baum | `C:\Users\DirkEngelmann\Documents\WP-Plan` (Branch `sqlite`) |
| Commit | **`cb6d5003a7259b27a80bf8ed8286220c121bfa1f`** |
| Arbeitsbaum | **87 geänderte Einträge** — die S4/S5/S6-Umstellung ist noch **uncommittet** |
| Zugriffsschicht | `DataRepository.DB_DATEINAME = "Kenndaten.sqlite"`, `Microsoft.Data.Sqlite` |
| Bau | wie A, `-p:OutputPath=C:\Users\DirkEngelmann\s7run\B\bin\` |
| Ergebnis | **0 Fehler**, dieselben 5 Bestandswarnungen |

`Referenzlauf\bin\x64\Debug` und `WindowsFormsApplication1\bin\x64\Debug` wurden **nicht
angefasst** (Ausgabe beider Bauten außerhalb des Repos).

### 2.3 Suite-Erweiterung (die einzige Codeänderung dieses Pakets)

Zwei Dateien, beide in `Referenzlauf/`, beide **BOM-utf8 / durchgängig CRLF vorher wie
nachher** (Kodierungsprobe vor und nach dem Umbau):

**`Referenzlauf/DbUmgebung.cs`**

- `DB_DATEINAME_SQLITE = "Kenndaten.sqlite"` neben dem bestehenden `DB_DATEINAME`.
- `IstSqlite(pfad)` — Endungsprobe.
- `ArbeitskopieDatei(ordner)` — liegt dort eine `Kenndaten.sqlite`, gilt der SQLite-Zweig,
  sonst der Access-Zweig. Die Probe läuft über das **Dateisystem**, weil der Kindprozess
  (Modus `projekt`) nur den *Ordner* übergeben bekommt; ein statisches Kennzeichen stünde
  dort wieder auf seinem Anfangswert.
- `ProduktivQuelleFinden(log, vorgabe)` — Überladung für `--quelle`; ohne Vorgabe sucht
  sie in `%ProgramData%\EPOS_PLAN` erst `Kenndaten.sqlite`, dann `Kenndaten.accdb`, dann
  den Fallback.
- `ArbeitskopieAnlegen` — Wächter je Zweig: `.laccdb` wie bisher, für SQLite eine Warnung
  bei `-wal`/`-shm` **neben der Quelle**. Im Zielordner werden liegengebliebene
  `-wal`/`-shm` **gelöscht**, bevor kopiert wird: ein altes WAL würde beim ersten Öffnen
  in die frische Kopie eingespielt, die dann weder Quellstand noch gültiger Stand wäre.
- `AufArbeitskopieUmschaltenUndPruefen` — SQLite-Zweig setzt
  `DataRepository.PfadUeberschreibung` (Haken aus S4a), der Access-Zweig behält die
  bisherige `Settings.DBPath`-Umbiegung. Die harte Nachprüfung über
  `DataRepository.GetDBPath()` bleibt; die Sperrliste der verbotenen Ziele umfasst jetzt
  auch die beiden `.sqlite`-Ablagen.

**`Referenzlauf/Program.cs`** — Schalter `--quelle <db>` für `lauf` und `liste`
(durchgereicht an `ProduktivQuelleFinden`), Hilfetext, und die Protokollzeile
„Arbeitskopie (beschrieben)" nennt jetzt `ArbeitskopieDatei(ordner)` statt fest die
`.accdb`.

Am Anwendungscode (`WindowsFormsApplication1/`) wurde **nichts** geändert.

---

## 3. Projektmenge — 10 statt 13

Die Bestandsbasis `2026-08-30_B3-Kaskade` (wie schon `2026-08-29_Booster`) führt 13
Projekte. **Auf diesem Rechner (26 Projekte in `Tab_Projekt`) existieren davon nur zehn.**

| | IDs |
|---|---|
| gefahren (A **und** B, feste Liste `--projekte`) | 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039 |
| in der Basis, hier **nicht vorhanden** | **1040, 1041, 1042** |

Das deckt sich mit dem Konzeptvermerk („dieser Rechner: 26 Projekte") und mit dem
Laufprotokoll der Basis, das sie ausdrücklich vom **Zweitstand** herleitet. Beide Seiten
bekamen exakt dieselbe Liste — die Auswahl ist damit kein Freiheitsgrad des Vergleichs.

---

## 4. A-Lauf (Access) — vollständig

`Referenzlauf.exe lauf --ziel <Scratch>\s7\A_access --projekte <10 IDs> --timeout 900`

- Quelle: `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` (nur gelesen), Arbeitskopie
  `C:\Users\DirkEngelmann\s7run\A\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`, DB-Pfad
  der App vor jedem Kindprozess hart verifiziert.
- Schemapflege der Arbeitskopie: Stand vorher **61**, Zielstand **61** — alle 29 Schritte
  „bereits erledigt", ein **No-op**.
- **Erfolgreich 10 von 10**, Gesamtdauer **00:00:16**, **0 Warnungen, 0 Fehler**, **kein
  einziger automatisch geschlossener Dialog**.
- `pruefen`: **10/10 plausibel**, keine NaN/Inf. Zwei Bestandshinweise
  („Gewerk aktiviert, aber kein Modul zugeordnet": 1007 Solarthermie, 1039 Wärmepumpe).

| Projekt | Name | CSV | Vektoren | Werte |
|---|---|---:|---:|---:|
| 1007 | Laurentiuskirche | 29 | 28 | 324 120 |
| 1008 | Heinestr 15 | 21 | 20 | 227 760 |
| 1011 | test1 | 29 | 28 | 324 120 |
| 1017 | WP_PV-Speicher | 21 | 20 | 254 040 |
| 1018 | BHKW Test München | 22 | 21 | 236 520 |
| 1021 | TestSpeichernUnter | 21 | 20 | 227 760 |
| 1023 | Wöhler - Test1 | 25 | 24 | 262 800 |
| 1024 | Wöhler - Test2 | 26 | 25 | 271 560 |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | 22 | 21 | 236 520 |
| 1039 | Wärmepumpe WG - BHKW | 18 | 17 | 201 480 |
| **Summe** | | **234** | **224** | **2 566 680** |

---

## 5. B-Lauf (SQLite) — 0 von 10

`Referenzlauf.exe lauf --quelle <Scratch>\s7\Kenndaten_S7.sqlite --ziel <Scratch>\s7\B_sqlite
--projekte <10 IDs> --timeout 900`

Was **funktioniert** hat:

- Arbeitskopie `…\s7run\B\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite` (64 MB) angelegt,
  DB-Pfad über `PfadUeberschreibung` gesetzt und **hart verifiziert** — vor dem Hauptlauf
  und erneut in jedem der 10 Kindprozesse.
- **Schemapflege auf SQLite: Stand vorher 61 → nachher 61, ERFOLG** (bestätigt S6).
- Projektlandschaft gelesen: **26 Projekte in `Tab_Projekt`**, Auswahl und Ausstattung
  **identisch zur A-Seite** (Modultexte, Puffer-/Quellspeicherzuordnung, Gewerkeliste) —
  die lesende Zugriffsschicht trägt also über weite Strecken.

Was **abgebrochen** ist — alle zehn Projekte, Kindprozess-Exit **3**, Gesamtdauer 6 s,
`Erfolgreich: 0 von 10`, Exit-Code des Laufs **1**. Die halbfertigen Ausgabeordner hat die
Suite planmäßig gelöscht; in `B_sqlite` liegt nur `lauf_protokoll.md`.

| Projekt | `Tab_DBTagV.ID` (B1) | `Tab_Stromganglinie.ID` (B1) | `Tab_Waermebedarf.ID` (B1) | `DELETE`-Syntax (B2) | Abbruchgrund |
|---|---:|---:|---:|---:|---|
| 1007 | 1 | 0 | 0 | 3 | Ergebnis nicht speicherbar |
| 1008 | 1 | 1 | 0 | 0 | Stromganglinie 1008016 hat 0 Werte |
| 1011 | 1 | 1 | 0 | 0 | Stromganglinie 1008027 hat 0 Werte |
| 1017 | 1 | 0 | 0 | 3 | Ergebnis nicht speicherbar |
| 1018 | 1 | 0 | 0 | 3 | Ergebnis nicht speicherbar |
| 1021 | 1 | 0 | 0 | 3 | Ergebnis nicht speicherbar |
| 1023 | 1 | 0 | 0 | 3 | Ergebnis nicht speicherbar |
| 1024 | 1 | 0 | 0 | 3 | Ergebnis nicht speicherbar |
| 1030 | 0 | 1 | 1 | 0 | Stromganglinie 1008032 hat 0 Werte |
| 1039 | 1 | 0 | 0 | 3 | Ergebnis nicht speicherbar |
| **Summe** | **9** | **3** | **1** | **21** | |

Insgesamt **34 SQLite-Fehler** in vier Meldungsformen; alle vier sind auf **zwei Ursachen**
zurückführbar (Abschnitt 7).

---

## 6. Vergleich A ↔ B

| Prüfung | Ergebnis |
|---|---|
| Suite-`vergleich` `A_access` gegen `B_sqlite` | **GESAMT: FAIL** — 10× „im Vergleichslauf nicht vorhanden"; Exit 1 |
| Exaktvergleich ohne Toleranz (`exaktvergleich.py`, SHA-256 je Datei, danach zeilenweise) | 234 Dateien A, **0 Dateien B**, 0 gemeinsam → **nicht bitgleich, weil nichts zu vergleichen ist** |
| `aggregate.csv` byte-/inhaltsgleich | 10 auf der A-Seite (2 077–5 424 Bytes), **0 auf der B-Seite** |

**Einordnung:** Das ist **kein Wertebefund**. Es gibt auf der SQLite-Seite keinen einzigen
Ergebnisvektor, weil jeder Lauf vor dem Export abbricht. Die Aussage aus Rev. 2
(„Double → REAL ist bitgleich") ist damit **weder bestätigt noch widerlegt**. Der
Wertevergleich ist nach Behebung von B1 und B2 zu wiederholen — mit **demselben Datenpaar**
(die eingefrorene `.accdb` und die daraus migrierte `.sqlite` liegen dafür bereit) und
gegen den **bereits vorhandenen A-Lauf**, damit die A-Seite nicht neu erzeugt werden muss.

### Kontrolllauf: A-Seite gegen die eingefrorene Basis vom 30.08.

`vergleich Referenzlaeufe\2026-08-30_B3-Kaskade S:\s7\A_access` (Toleranzvergleich):

| Projekt | Ergebnis |
|---|---|
| 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024 | **PASS** (8 Projekte, zusammen ≈ 2,13 Mio. Werte, **0 Abweichungen**) |
| 1030 | FAIL, 55 338 Abweichungen |
| 1039 | FAIL, 155 052 Abweichungen |
| 1040, 1041, 1042 | FAIL — im Bestand dieses Rechners nicht vorhanden |

Die beiden FAIL-Projekte sind **Datendrift, keine Codedrift** — belegt durch die
Schlüssel, an denen sie hängen:

- **1030:** `aggregate.csv [BHKWModul[1].Modul]` `EC-POWER XRGI 9` → `Agenitor 306
  (250 kW el.) Gas` — der Anwender hat das zweite Kaskadenmodul gewechselt; die
  Zahlenabweichungen sind Folge.
- **1039:** `aggregate.csv [Ergebnis.Bezeichner]` `Simulation Mehrgebäude` → `Simulation
  Wärmepumpe WG - BHKW`, `Sim.PufferWP_vorhanden` `True` → `False`, sämtliche
  `Puffer.*`-Schlüssel fehlen — das Projekt ist inhaltlich ein anderes.

**Wert dieses Kontrolllaufs:** Der frisch aus `7d41833` gebaute Access-Stand reproduziert
die am 30.08. aus `bad41f8` eingefrorene Basis auf **8 von 8 vergleichbaren Projekten
ohne eine einzige Abweichung**. Die A-Seite dieses Pakets ist damit als Bezugsgröße
belastbar.

---

## 7. Befunde

### B1 — Sichten: qualifizierte Spaltennamen laufen in SQLite ins Leere · **blockierend**

Vier SQL-Stellen adressieren Spalten einer **Sicht** über den Namen der zugrunde
liegenden **Tabelle**. Jet löst das auf, SQLite nicht: eine Sicht hat nur ihre eigenen
Ausgabespalten.

| Datei : Zeile | SQL (gekürzt) |
|---|---|
| `Allgemein/Simulation/SimulationWaermebedarf.cs:602` | `select * from Abfrage_Tagverteilung where Bezeichner='…' and Tab_DBTagV.ID=…` |
| `Allgemein/Simulation/SimulationWaermebedarf.cs:305` | `select * from Abfrage_ProjektGebaeudeGanglinie where Tab_Waermebedarf.ID=… order by Tab_WaermebedarfDaten.ID` |
| `Allgemein/Simulation/SimulationStrombedarf.cs:121` | `select * from Abfrage_ProjektStromGanglinie where Tab_Stromganglinie.ID=… order by Tab_StromganglinieDaten.ID` |
| `Allgemein/StromTestClass.cs:48` | `select * from Abfrage_ProjektStromGanglinie where Tab_Stromganglinie.ID=… order by ID` |

Meldung zur Laufzeit: `SQLite Error 1: 'no such column: Tab_DBTagV.ID'` (bzw.
`Tab_Stromganglinie.ID`, `Tab_Waermebedarf.ID`).

**Zweite Hälfte des Befunds — Doppelnamen.** Drei der 14 Sichten selektieren die
`ID`-Spalte **beider** verbundenen Tabellen (`002_views.sql`). SQLite entdoppelt das
selbsttätig:

| Sicht | Spaltennamen laut `PRAGMA table_info` |
|---|---|
| `Abfrage_ProjektGebaeudeGanglinie` | `ID`, **`ID:1`**, `Wert` |
| `Abfrage_ProjektStromGanglinie` | `ID`, **`ID:1`**, `Wert`, `Zeitinterval` |
| `Abfrage_Tagverteilung` | `ID`, `Bezeichner`, **`ID:1`**, `Verteilung` |

Die übrigen 11 Sichten sind frei von Doppelnamen.

**Messung an der migrierten Datenbank:**
`select * from Abfrage_Tagverteilung where Tab_DBTagV.ID=10614` → Fehler;
`select * from Abfrage_Tagverteilung where ID=10614` → **120 Zeilen**. Die Daten sind also
da, nur nicht unter dem qualifizierten Namen erreichbar.

**Wirkung, wenn ungefixt:** Zwei verschiedene Schadensbilder. Die Tagesverteilung fällt
**still** aus (nur eine Warnung „Zum Tagesverteilungstyp … sind keine Daten hinterlegt.
Die Bedarfsrechnung wurde an dieser Stelle abgebrochen"), die Stromganglinie **laut**
(„hat 0 Werte … bitte neu einlesen", Abbruch). Die stille Variante ist die gefährlichere:
ohne Vergleichslauf sieht das Ergebnis vollständig aus.

**Richtung für den Fix** (nicht ausgeführt — S7 ändert keinen Anwendungscode): entweder
die vier SQL-Stellen auf die Sichtspalten umstellen (die `ORDER BY`-Hälften brauchen dann
den entdoppelten Namen `"ID:1"`), oder — sauberer — die drei Sichtdefinitionen mit
sprechenden Aliassen versehen (z. B. `Tab_DBTagV.ID AS ID_TagV`,
`Tab_DBTagVDaten.ID AS ID_Daten`) und die Aufrufer darauf ziehen. Die zweite Variante
beseitigt zugleich den Doppelnamen und ist ein Schemaschritt, kein Streufix.

### B2 — `DELETE <Spalte> FROM …`: Access-Idiom im Ergebnis-Speichern · **blockierend**

`WindowsFormsApplication1/Controller/ErgebnisCtrl.cs:150`

```csharp
v.Ausfuehren("DELETE ID_Projekt FROM " + TAB_KOPF + " WHERE ID_Projekt = ?", p.ToArray());
```

`DELETE <feld> FROM <tabelle>` ist Jet-Syntax; SQLite meldet
`SQLite Error 1: 'near "ID_Projekt": syntax error'`. Der Aufruf steht **innerhalb der
Transaktion** von `ErgebnisCtrl.Save`, die daraufhin zurückgerollt wird → `Save` liefert
`-1` → `Ergebnisexport` bricht ab → **0 CSV**.

Bemerkenswert: **dieselbe Datei benutzt zwei Zeilen weiter oben die richtige Form**
(`ErgebnisCtrl.cs:60`: `DELETE FROM … WHERE ID_Projekt = ?`), und in zwei anderen Dateien
steht das Idiom nur noch als Kommentar über der bereits erfolgten Korrektur
(`KenndatenCtrl.cs:69`, `WErzeugerCtrl.cs:103`). **Zeile 150 ist die letzte lebende
Fundstelle im gesamten `WindowsFormsApplication1/`** (Regex
`DELETE\s+[A-Za-z_]\w*\s+FROM`, case-insensitiv, repoweit) — der S5-Sweep hatte dieses
Muster nicht auf seiner Liste.

**Wirkung:** 21 Fehlschläge in 7 Projekten; jedes gerechnete Ergebnis geht verloren.

### B3 — Reihenfolge der Befunde

B1 schlägt **vor** B2 zu: in den sieben Projekten mit B2-Abbruch ist die Wärmebedarfs­rechnung
bereits durch B1 unvollständig. **Nach dem Fix von B2 allein wären die Ergebnisse
zwar exportierbar, aber falsch.** Beide Befunde müssen zusammen behoben sein, bevor der
Wertevergleich Aussagekraft hat.

### B4 — Referenzmenge geschrumpft · Hinweis

1040/1041/1042 fehlen im Bestand dieses Rechners. Die Basis `2026-08-30_B3-Kaskade` ist
damit nur noch auf 10 ihrer 13 Projekte anwendbar. Kein Umstellungsbefund; für S7 sauber
umschifft (beide Seiten dieselbe Liste), aber bei der nächsten Basis zu entscheiden.

### B5 — Sichten ohne Aufrufer · geprüft, kein Befund

Die im Code auftauchenden Namen `Abfrage_ProjektKostenInvestBetrieb`,
`Abfrage_Neues_Kosten_Model`, `Abfrage_KostenKomponenten`, `Abfrage_Heizkessel_Kosten` und
`Abfrage_Erzeuger_Vorlauftemperaturen` stehen **ausschließlich in Kommentaren und
XML-Dokumentation** (überwiegend `SchemaMigration.cs`), nicht in ausgeführtem SQL. Dass sie
unter den 14 migrierten Sichten fehlen, ist folgerichtig.

---

## 8. Abnahme

| Kriterium | Stand |
|---|---|
| Migrationslauf Exit 0 mit 114/114 | **erfüllt** (alle Prüfsummen gleich, integrity_check ok) |
| A-Lauf vollständig | **erfüllt** (10/10, 234 CSV, 0 Fehler, 10/10 plausibel) |
| B-Lauf vollständig | **nicht erfüllt** — 0/10, Abbruch an B1 und B2 |
| Vergleich A↔B dokumentiert (Ziel 0 Abweichungen) | **nicht entscheidbar** — keine B-Vektoren; Vergleich und Exaktvergleich sind gefahren und protokolliert |
| Protokoll im Repo | **erfüllt** (diese Datei) |

**S7 ist damit nicht abgeschlossen.** Offen: B1 und B2 beheben, dann den B-Lauf gegen
dasselbe Datenpaar wiederholen und gegen den vorhandenen A-Lauf vergleichen. Erst danach
sind 7.3 (Bedienbeweis) und 7.4 (Frontend-Beweis) sinnvoll.

> **Diese Abnahmetabelle ist der Stand des ersten Durchgangs.** Die beiden „nicht
> erfüllt"-Zeilen sind mit der Nacharbeit erledigt — die gültige Abnahme steht in
> **Abschnitt 10.9**.

---

## 9. Pfade

Die Ergebnisordner bleiben bewusst **außerhalb des Repos** (234 CSV ≈ 2,6 Mio. Werte, dazu
zwei Datenbanken).

| Was | Wo |
|---|---|
| Eingefrorene Access-Kopie | `<Scratch>\s7\Kenndaten_S7.accdb` |
| Migrierte SQLite-Datei | `<Scratch>\s7\Kenndaten_S7.sqlite` |
| Migrationsbericht | `<Scratch>\s7\S7_Migrationsbericht.md` |
| A-Lauf (Access) | `<Scratch>\s7\A_access\` (10 Projektordner + `lauf_protokoll.md`) |
| B-Lauf (SQLite) | `<Scratch>\s7\B_sqlite\` (nur `lauf_protokoll.md`) |
| Exaktvergleich-Skript | `<Scratch>\s7\exaktvergleich.py` |
| A-Build (lauffähig, Access) | `C:\Users\DirkEngelmann\s7run\A\bin\Referenzlauf.exe` |
| B-Build (lauffähig, SQLite) | `C:\Users\DirkEngelmann\s7run\B\bin\Referenzlauf.exe` |
| Arbeitskopien der Läufe | `C:\Users\DirkEngelmann\s7run\{A,B}\Referenzlaeufe\Arbeitskopie\` |

`<Scratch>` = `C:\Users\DirkEngelmann\AppData\Local\Temp\claude\C--Users-DirkEngelmann-Documents-Tools-epos-downloads-EPOS-Plan-Tools-Aktionsplan\462d1d2b-408a-4539-ae35-f477e5650afd\scratchpad`
(im Lauf über ein `subst`-Laufwerk `S:` angesprochen — das verschwindet beim Abmelden).

In `C:\Users\DirkEngelmann\s7run\{A,B}\` liegt je eine **leere Datei `WP-Plan.sln`** als
Wurzelmarke: `Program.ProjektWurzelFinden` sucht aufwärts danach und legt darunter
`Referenzlaeufe\Arbeitskopie` an. Ohne die Marke wäre die Arbeitskopie im Fallback
`C:\Waermeplan\WP_Plan` gelandet.

---

# 10. Nacharbeit 02.09.2026 — B1 und B2 behoben, B-Lauf wiederholt

**Ergebnis in einem Satz:** Beide Blocker sind behoben, das Datenpaar ist mit der
kurierten Sichtdefinition neu erzeugt (`Kenndaten_S7_v2.sqlite`, wieder 114/114 und
Exit 0), der B-Lauf rechnet **10 von 10 Projekten** und liefert 234 CSV — und der
Vergleich gegen den **unverändert stehen gebliebenen A-Lauf** ergibt **ohne jede
Toleranz 0 Abweichungen: alle 234 Dateien sind byte-identisch.**

Die A-Seite ist **nicht neu erzeugt** worden. `<Scratch>\s7\A_access` trägt weiterhin die
Zeitstempel vom 13:35 des ersten Durchgangs; die eingefrorene `.accdb` hat vor und nach
der zweiten Migration denselben SHA-256 `271279C0…939FAA`.

---

## 10.1 Fix B2 — `DELETE <Spalte> FROM` (eine Zeile)

`WindowsFormsApplication1/Controller/ErgebnisCtrl.cs`, vormals Zeile 150:

```csharp
// vorher
v.Ausfuehren("DELETE ID_Projekt FROM " + TAB_KOPF + " WHERE ID_Projekt = ?", p.ToArray());
// nachher
v.Ausfuehren("DELETE FROM "            + TAB_KOPF + " WHERE ID_Projekt = ?", p.ToArray());
```

Muster wie in derselben Datei bei `Delete(int)` (Zeile 60). Darüber steht jetzt ein
Kommentarblock, der den Befund benennt, damit die Zeile nicht wieder „aufgeräumt" wird.
Durch den Kommentar rutscht der Aufruf auf **Zeile 156**.

## 10.2 Fix B1 — kurierte Sichten und vier Aufrufstellen

**Schritt 1: die drei Sichten mit Doppel-`ID`.** Die Vermutung aus dem Auftrag hat sich
bestätigt — es sind genau die drei Simulations-Zulieferer. Gemessen wurde nicht geraten:
`PRAGMA table_info` über alle 14 Sichten der migrierten Datei, vorher wie nachher.

`sql/schema/002_views.sql`, **vorher → nachher** (nur die zweite `ID` bekommt einen Namen):

```sql
-- vorher
CREATE VIEW [Abfrage_ProjektGebaeudeGanglinie] AS
SELECT Tab_Waermebedarf.ID, Tab_WaermebedarfDaten.ID, Tab_WaermebedarfDaten.Wert
FROM Tab_Waermebedarf INNER JOIN Tab_WaermebedarfDaten ON Tab_Waermebedarf.ID = Tab_WaermebedarfDaten.ID_Ganglinie;
-- nachher
CREATE VIEW [Abfrage_ProjektGebaeudeGanglinie] AS
SELECT Tab_Waermebedarf.ID, Tab_WaermebedarfDaten.ID AS ID_Daten, Tab_WaermebedarfDaten.Wert
FROM Tab_Waermebedarf INNER JOIN Tab_WaermebedarfDaten ON Tab_Waermebedarf.ID = Tab_WaermebedarfDaten.ID_Ganglinie;

-- vorher
CREATE VIEW [Abfrage_ProjektStromGanglinie] AS
SELECT Tab_Stromganglinie.ID, Tab_StromganglinieDaten.ID, Tab_StromganglinieDaten.Wert, Tab_Stromganglinie.Zeitinterval
FROM Tab_Stromganglinie INNER JOIN Tab_StromganglinieDaten ON Tab_Stromganglinie.ID = Tab_StromganglinieDaten.ID_Ganglinie;
-- nachher
CREATE VIEW [Abfrage_ProjektStromGanglinie] AS
SELECT Tab_Stromganglinie.ID, Tab_StromganglinieDaten.ID AS ID_Daten, Tab_StromganglinieDaten.Wert, Tab_Stromganglinie.Zeitinterval
FROM Tab_Stromganglinie INNER JOIN Tab_StromganglinieDaten ON Tab_Stromganglinie.ID = Tab_StromganglinieDaten.ID_Ganglinie;

-- vorher
CREATE VIEW [Abfrage_Tagverteilung] AS
SELECT Tab_DBTagV.ID, Tab_DBTagV.Bezeichner, Tab_DBTagVDaten.ID, Tab_DBTagVDaten.Verteilung
FROM Tab_DBTagV INNER JOIN Tab_DBTagVDaten ON Tab_DBTagV.ID = Tab_DBTagVDaten.ID_TagV
ORDER BY Tab_DBTagVDaten.ID;
-- nachher
CREATE VIEW [Abfrage_Tagverteilung] AS
SELECT Tab_DBTagV.ID, Tab_DBTagV.Bezeichner, Tab_DBTagVDaten.ID AS ID_Daten, Tab_DBTagVDaten.Verteilung
FROM Tab_DBTagV INNER JOIN Tab_DBTagVDaten ON Tab_DBTagV.ID = Tab_DBTagVDaten.ID_TagV
ORDER BY Tab_DBTagVDaten.ID;
```

**Was sich bewusst NICHT geändert hat:** Spaltenzahl, Spaltenreihenfolge, Join, `ORDER BY`
im Rumpf — und **jeder bereits benutzbare Spaltenname** (`ID`, `Bezeichner`, `Wert`,
`Verteilung`, `Zeitinterval`). Benannt wird ausschließlich die Spalte, die vorher gar
keinen brauchbaren Namen hatte (`ID:1`). Die Kuration kann deshalb keinen Konsumenten
brechen, der vorher funktioniert hat.

**Nachmessung an `Kenndaten_S7_v2.sqlite`:**

| Sicht | vorher (`Kenndaten_S7.sqlite`) | nachher (`_v2`) |
|---|---|---|
| `Abfrage_ProjektGebaeudeGanglinie` | `ID`, **`ID:1`**, `Wert` | `ID`, **`ID_Daten`**, `Wert` |
| `Abfrage_ProjektStromGanglinie` | `ID`, **`ID:1`**, `Wert`, `Zeitinterval` | `ID`, **`ID_Daten`**, `Wert`, `Zeitinterval` |
| `Abfrage_Tagverteilung` | `ID`, `Bezeichner`, **`ID:1`**, `Verteilung` | `ID`, `Bezeichner`, **`ID_Daten`**, `Verteilung` |

Über alle **14 Sichten**: **0 Spaltennamen mit Doppelpunkt** (vorher 3).

**Schritt 2: die vier Aufrufstellen.**

| Datei : Zeile (neu) | vorher | nachher |
|---|---|---|
| `Allgemein/Simulation/SimulationWaermebedarf.cs:310` | `… where Tab_Waermebedarf.ID=<id> order by Tab_WaermebedarfDaten.ID` | `… where ID=<id> order by ID_Daten` |
| `Allgemein/Simulation/SimulationWaermebedarf.cs:612` | `… where Bezeichner='<typ>' and Tab_DBTagV.ID=<id>` | `… where Bezeichner='<typ>' and ID=<id>` |
| `Allgemein/Simulation/SimulationStrombedarf.cs:125` | `… where Tab_Stromganglinie.ID=<id> order by Tab_StromganglinieDaten.ID` | `… where ID=<id> order by ID_Daten` |
| `Allgemein/StromTestClass.cs:53` | `… where Tab_Stromganglinie.ID=<id> order by ID` | `… where ID=<id> order by ID_Daten` |

Die Zeilennummern verschieben sich, weil über jeder Stelle ein Kommentarblock steht, der
den Befund festhält (sonst liest die Stelle sich später wie ein grundloser Umbau).

**Zwei Feinheiten, die eine Messung wert waren:**

1. **`Abfrage_Tagverteilung` bekommt am Aufrufer bewusst KEIN `ORDER BY`.** Die
   Sortierung steht im Rumpf der Sicht. Ob SQLite sie durch ein äußeres `WHERE`
   durchträgt, ist keine Glaubensfrage: gemessen an der migrierten Datei liefert
   `select * from Abfrage_Tagverteilung where ID=10614` **120 Zeilen, `ID_Daten`
   aufsteigend**. Der Aufrufer bleibt damit so knapp wie vorher.
2. **`StromTestClass.cs` sortierte vorher wirkungslos.** `order by ID` bezog sich auf die
   Ganglinien-`ID`, die das `WHERE` ohnehin auf einen Wert festnagelt — eine Sortierung
   ohne Sortierwirkung. Sie geht jetzt auf `ID_Daten` und ist damit dieselbe wie im
   Simulationszweig. `StromTestClass.MyTestLastgang` hängt einzig an
   `Views/Form_StromTest.cs` (Testdialog) und liegt **nicht** im Rechenpfad des
   Referenzlaufs — die Änderung kann das Vergleichsergebnis nicht beeinflusst haben.

**Schritt 3: der Generator zieht nach.** `sql/tools/Erzeuge-Schema.ps1` übernahm 12 der 14
Sichten **wörtlich aus den Access-QueryDefs** — eine Neugenerierung hätte die Kuration
still zurückgedreht. Die drei Texte stehen jetzt in `$VIEWS_UEBERSETZT`, nach demselben
Muster wie die beiden IIf-Übersetzungen, mit Begründung (B1, S7) und Aufruferliste im
Kommentar; dazu je ein Eintrag in `$VIEW_KOMMENTAR` und der erweiterte Kopftext, damit
eine Neugenerierung **dieselbe** Datei erzeugt. Der Kopfhinweis in `002_views.sql` steht
im Skript und in der Datei wortgleich.

**Und das ist nicht behauptet, sondern nachgefahren:** der Generator lief gegen die
eingefrorene Kopie (`-Quelle <Scratch>\s7\Kenndaten_S7.accdb -Ausgabe
<Scratch>\s7\schema_probe`, strikt lesend, SchemaVersion 61, 114 Tabellen, 14 Views) —
das erzeugte `002_views.sql` ist gegenüber der Repo-Datei **byte-identisch**: 9 937 Bytes,
ohne BOM, rein LF, 0 Diff-Zeilen. Eine künftige Neugenerierung dreht die Kuration also
nicht zurück. (Die übrigen fünf Erzeugnisse des Laufs liegen zur Einsicht daneben und
sind nicht ins Repo übernommen worden.)

## 10.3 Konsumenten-Analyse der kurierten Sichten

Vor der Umbenennung geprüft: **Wer liest diese drei Sichten sonst noch, und unter welchen
Spaltennamen?**

| Prüfung | Ergebnis |
|---|---|
| Repoweite Suche nach den drei Sichtnamen | **4 ausführende SQL-Stellen** — exakt die vier oben. Alle übrigen Treffer stehen in Markdown-Dokumentation (`Konzept_*`, `Grundlagen_4_*`, `WPPlan_Code_Befunde.md`, `K1_Aufraeumung_Protokoll.md`). |
| Systematische Gegenprobe | Regex über **alle** C#-Literale mit `from Abfrage_*` **und** einem `Tab_*.`-Qualifizierer → dieselben 4, kein fünfter Fall. |
| Gelesene Spalten der Konsumenten | ausschließlich `rs.Read("Wert")`, `rs.Read("Verteilung")`, `rs.Read("Zeitinterval")` — **namensbasiert** (`RecordSet.Read(string)` → `DataRow[name]`), und **keiner** liest eine `ID`-Spalte aus. Die `ID`-Spalten dienen nur `WHERE`/`ORDER BY`. |
| Probensuite `Proben/ZugriffsschichtProben` | nennt **keinen** `Abfrage_*`-Namen — sie berührt die kurierten Sichten nicht. |
| Migrator | erzeugt die Sichten aus der eingebetteten `002_views.sql` und **liest** aus keiner. |
| Schemapflege (`SchemaMigration.cs`, tabu) | legt Sichten nur in **nummerierten Schritten** an; keine unbedingte Auffrischung. Auf Schemastand 61 → 61 ist das ein No-op, die Kuration wird von der Schemapflege **nicht** überschrieben. |

**Damit ist die Kuration risikofrei:** kein bestehender Konsument spricht einen der
umbenannten Namen an, weil `ID:1` gar nicht ansprechbar war.

> **Ein bewusst offener Punkt:** Die Access-Seite (`SchemaMigration.cs`, tabu für dieses
> Paket) führt die drei Abfragen weiterhin mit Doppel-`ID`. Das ist folgenlos, solange
> die Anwendung auf SQLite läuft — die vier Aufrufstellen sind jetzt aber
> **SQLite-spezifisch** und liefen gegen eine Access-Datenbank nicht mehr. Für S8 ist
> das kein Thema (der Access-Zweig wird nicht mehr gerechnet); es gehört trotzdem
> notiert, damit niemand den Anwendungscode versehentlich wieder gegen ACE fährt.

## 10.4 Das Testpaar v2

Weil `002_views.sql` als **EmbeddedResource** in `EposSqliteMigrator.Kern` steckt, genügt
ein erneuter Migratorlauf nicht — das Werkzeug musste **neu gebaut** werden, sonst hätte
es die alte Sichtdefinition aus der Assembly gespielt. Nach dem Bau gegengemessen: die
eingebettete Ressource enthält **7×** `ID_Daten` und die drei kurierten `CREATE VIEW`.

| Kennzahl | erster Durchgang | **v2** |
|---|---|---|
| Tabellen migriert | 114 / 114 | **114 / 114** |
| Zeilen gesamt (Ziel) | 1 392 013 | **1 392 013** |
| Datenbeweis | alle Prüfsummen gleich | **alle Prüfsummen gleich** |
| `integrity_check` | ok | **ok** |
| `foreign_key_check` | keine Verletzung | **keine Verletzung** |
| Case-Drift (D5) | 0 / 31 | **0 / 31** |
| nicht migriert | 0 | **0** |
| Dauer / Zielgröße | 13,28 s · 64,6 MB | **12,56 s · 64,6 MB** |
| **Exit-Code** | 0 | **0** |
| Sichten / davon mit Doppelnamen | 14 / **3** | 14 / **0** |
| Bericht | `S7_Migrationsbericht.md` | `S7_Migrationsbericht_v2.md` |

Die Quelle wurde wieder **nur lesend** angefasst (`Mode=Read`); SHA-256 vor und nach dem
Lauf identisch.

> **Zur Werkzeugversion:** Der Bericht weist `1.0.0+cb6d5003…` aus — denselben Commit wie
> im ersten Durchgang. Das ist korrekt und trotzdem erklärungsbedürftig: die
> Sicht-Änderung ist **uncommittet** (wie der gesamte S4/S5/S6-Umbau), der Versionsstempel
> nennt den letzten Commit. Dass der Bau die Kuration wirklich trägt, ist deshalb nicht
> über die Versionszeile belegt, sondern über die Ressourcenmessung oben und über
> `PRAGMA table_info` an der erzeugten Datei.

## 10.5 Regressionsnetz vor dem Lauf

| Schritt | Ergebnis |
|---|---|
| Bau `Proben/ZugriffsschichtProben` (VS-2022-MSBuild, `/t:Restore`, Debug x64, `OutputPath` in den Scratchpad) | **0 Fehler**, **exakt 5 Bestandswarnungen** (2× CS0108, 2× CS0109, 1× CS1998) |
| Probenlauf gegen `<Scratch>\s3\Kenndaten_Rechner1.sqlite` | **15 / 15 bestanden**, Exit 0 |
| Bau der Referenzlauf-Suite (B2) | **0 Fehler**, dieselben 5 Bestandswarnungen |

**Zur Rückfrage aus dem Auftrag, ob die Proben gegen eine v2-Kopie laufen müssen:**
nein. Die Probensuite nennt **keinen einzigen `Abfrage_*`-Namen** (geprüft), berührt also
keine der kurierten Sichten. Die alte Sichtdefinition in der s3-Datenbank ist für die 15
Fälle ohne Bedeutung; der Lauf gegen die dokumentierte Regressionsbasis ist damit der
aussagekräftigere. Fall 5 („Namensdubletten aus Joins entdoppelt, `ID`/`ID1`") prüft die
**Entdopplung in der Zugriffsschicht**, nicht die Sichten — sie bleibt unberührt, weil in
den kurierten Sichten gar keine Dublette mehr entsteht.

`bin\x64\Debug` wurde in keinem der drei Bauten beschrieben; alle Ausgaben liegen
außerhalb des Repos.

## 10.6 B-Lauf v2 — 10 von 10

`Referenzlauf.exe lauf --quelle <Scratch>\s7\Kenndaten_S7_v2.sqlite --ziel <Scratch>\s7\B_sqlite_v2 --projekte <dieselben 10 IDs> --timeout 900`

- Arbeitskopie `…\s7run\B2\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite`, DB-Pfad vor
  dem Hauptlauf und in **jedem** der 10 Kindprozesse hart verifiziert.
- Schemapflege auf SQLite: Stand **61 → 61**, No-op.
- **Erfolgreich 10 von 10**, Gesamtdauer **00:00:06**, **0 Fehler**, **Exit 0**.
- `pruefen`: **10/10 plausibel**, keine NaN/Inf, dieselben zwei Bestandshinweise wie auf
  der A-Seite (1007 Solarthermie, 1039 Wärmepumpe — Gewerk aktiv ohne Modul).

| Projekt | Name | CSV | Vektoren | Werte | Status |
|---|---|---:|---:|---:|---|
| 1007 | Laurentiuskirche | 29 | 28 | 324 120 | OK |
| 1008 | Heinestr 15 | 21 | 20 | 227 760 | OK |
| 1011 | test1 | 29 | 28 | 324 120 | OK |
| 1017 | WP_PV-Speicher | 21 | 20 | 254 040 | OK |
| 1018 | BHKW Test München | 22 | 21 | 236 520 | OK |
| 1021 | TestSpeichernUnter | 21 | 20 | 227 760 | OK |
| 1023 | Wöhler - Test1 | 25 | 24 | 262 800 | OK |
| 1024 | Wöhler - Test2 | 26 | 25 | 271 560 | OK |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | 22 | 21 | 236 520 | OK |
| 1039 | Wärmepumpe WG - BHKW | 18 | 17 | 201 480 | OK |
| **Summe** | | **234** | **224** | **2 566 680** | **10/10** |

**Zeile für Zeile dieselben Zahlen wie die A-Tabelle in Abschnitt 4.**

**Zu den Warnungszählern.** Der A-Lauf meldet im Kopf 15 Warnungen, der B-Lauf 17 — der
Unterschied ist **kein Rechenunterschied**:

- Die **fachlichen Meldungen sind identisch**: 15 „Simulation Warnung" und 18 „Simulation
  Hinweis" auf beiden Seiten, nach Sortierung **zeichengleich**, kein einziger Unterschied.
- Die 2 zusätzlichen sind Wächtermeldungen der Suite: neben der v2-Quelldatei lagen ein
  `-wal` und ein `-shm`, weil dieses Protokoll die Datei vorher **lesend** geöffnet hatte
  (Spaltenmessung). Das `-wal` war **0 Bytes** — vollständig eingecheckpointet, es ging
  nichts verloren. Die Wächter melden die **Anwesenheit** der Dateien, nicht ungesicherten
  Inhalt, und der byte-gleiche Vergleich unten belegt es zusätzlich.
- Der Nebenbefund aus dem ersten Durchgang („A: 0 Warnungen") war eine Fehllesung der
  Konsolenausgabe; das A-Protokoll selbst weist **15** aus. Abschnitt 4 ist an dieser
  einen Zahl ungenau — die Laufbilanz „10/10, 0 Fehler" stimmt.

## 10.7 Vergleich A ↔ B_v2

Verglichen wurde der **frisch gerechnete B_v2-Lauf** gegen den **unveränderten A-Lauf vom
13:35** — die A-Seite ist nicht neu erzeugt worden.

**a) Suite-`vergleich`** (mit Toleranz: relativ 1e-4 ab Betrag 1, sonst absolut 0,01):

| Projekt | Dateien | verglichene Werte | Ergebnis |
|---|---:|---:|---|
| 1007 | 29 | 324 219 | **PASS** |
| 1008 | 21 | 227 861 | **PASS** |
| 1011 | 29 | 324 241 | **PASS** |
| 1017 | 21 | 254 154 | **PASS** |
| 1018 | 22 | 236 661 | **PASS** |
| 1021 | 21 | 227 854 | **PASS** |
| 1023 | 25 | 262 936 | **PASS** |
| 1024 | 26 | 271 717 | **PASS** |
| 1030 | 22 | 236 670 | **PASS** |
| 1039 | 18 | 201 540 | **PASS** |
| **GESAMT** | **234** | **2 567 853** | **PASS**, Exit 0 |

**b) Exaktvergleich ohne jede Toleranz** (`exaktvergleich.py`: SHA-256 je Datei, jede
Ungleichheit danach zeilenweise aufgelöst):

| Projekt | Dateien | Zeilen | byte-gleiche Dateien | abweichende Dateien | abweichende Zeilen |
|---|---:|---:|---:|---:|---:|
| Projekt_1007 | 29 | 324 277 | **29** | 0 | 0 |
| Projekt_1008 | 21 | 227 903 | **21** | 0 | 0 |
| Projekt_1011 | 29 | 324 299 | **29** | 0 | 0 |
| Projekt_1017 | 21 | 254 196 | **21** | 0 | 0 |
| Projekt_1018 | 22 | 236 705 | **22** | 0 | 0 |
| Projekt_1021 | 21 | 227 896 | **21** | 0 | 0 |
| Projekt_1023 | 25 | 262 986 | **25** | 0 | 0 |
| Projekt_1024 | 26 | 271 769 | **26** | 0 | 0 |
| Projekt_1030 | 22 | 236 714 | **22** | 0 | 0 |
| Projekt_1039 | 18 | 201 576 | **18** | 0 | 0 |
| **GESAMT** | **234** | **2 568 321** | **234** | **0** | **0** |

Beidseitig 234 Dateien, 234 gemeinsam, **keine Datei nur auf einer Seite**.
`ERGEBNIS: BITGLEICH — alle Dateien byte-identisch.`

**Rest-Abweichungen: keine.** Es gibt nichts zu tolerieren und nichts nachzubessern.
Das schließt die `aggregate.csv` der zehn Projekte ein — sie sind Teil der 234.

**Was damit bewiesen ist.** Die offene Frage aus Rev. 2 — „ist **Double → REAL**
bitgleich?" — ist **positiv beantwortet**: über 2,56 Mio. Ergebniswerte in 234 Vektoren,
gerechnet aus demselben Datenbestand einmal über ACE/Jet und einmal über
Microsoft.Data.Sqlite, stimmt **jedes einzelne Zeichen jeder Ausgabezeile** überein. Das
ist die stärkste Form des Verhaltensbeweises, die 7.2 verlangen kann.

**Was damit NICHT bewiesen ist** (unverändert offen): 7.3 Bedienbeweis, 7.4
Frontend-Beweis, S8 Verteilung — und die drei Projekte 1040–1042 der Basis
`2026-08-30_B3-Kaskade`, die auf diesem Rechner nicht existieren (Befund B4).

## 10.8 Prüfrezepte erweitert

`sql/MIGRATION_Pruefrezepte.md` hat zwei neue Rezepte (11 und 12) plus eine
Endstand-Tabelle 13; `sql/tools/sql_dialekt_inventur.py` zwei neue Abschnitte:

| Rezept | Abschnitt | Muster | Soll | Ist |
|---|---|---|---:|---:|
| `DELETE <Spalte> FROM` | `k)` | `\bDELETE\s+(?!FROM\b)[A-Za-z_]\w*\s+FROM\b` über SQL-Literale | 0 | **0** |
| qualifizierte Sichtspalten | `l)` | Literal enthält `FROM Abfrage_*` **und** `Tab_*.`/`Z_*.` | 0 | **0** |

Beide sind **gegen den Vorzustand gegengeprüft** worden, damit die 0 nicht bloß eine
kaputte Regex ist: Rezept `k)` fängt `DELETE ID_Projekt FROM …` und lässt sowohl
`DELETE FROM …` als auch `DELETE FROM … WHERE … NOT IN (SELECT …)` in Ruhe (das
`(?!FROM\b)` ist der springende Punkt); Rezept `l)` fängt **alle vier** alten
Aufrufstellen und keine der vier neuen.

**Der Merksatz, der in den Rezepten steht:** ein Dialekt-Sweep über den Quelltext ersetzt
den Referenzlauf nicht. Beide Befunde sind dem Sweep entgangen und erst im Lauf
aufgeschlagen — B2, weil es den **Schreibpfad** braucht (eine Leseprobe löst es nie aus),
B1 in seiner gefährlichen Hälfte, weil die Tagesverteilung **still** ausfällt: nur eine
Warnung, das Ergebnis sieht vollständig aus. Ohne den Wertevergleich gegen Access wäre
das durchgerutscht.

## 10.9 Abnahme S7 — **bestanden**

| Kriterium | Sollwert | Istwert | Stand |
|---|---|---|---|
| Migrationslauf Exit 0 mit 114/114 | 114/114, alle Prüfsummen gleich | **114/114**, alle gleich, `integrity_check` ok, 0 FK-Verletzungen, Exit **0** | **erfüllt** |
| A-Lauf vollständig | 10/10 | **10/10**, 234 CSV, 0 Fehler, 10/10 plausibel (unverändert vom ersten Durchgang) | **erfüllt** |
| B-Lauf vollständig | 10/10 | **10/10**, 234 CSV, 0 Fehler, 10/10 plausibel, Exit **0** | **erfüllt** |
| Vergleich A↔B, Ziel 0 Abweichungen | 0 | **0** — 234/234 Dateien byte-identisch, 2 568 321 Zeilen, 0 abweichende Zeilen | **erfüllt** |
| Sichten ohne Doppelnamen | 0 von 14 | **0 von 14** | **erfüllt** |
| Probensuite | 15/15 | **15/15** | **erfüllt** |
| Bau ohne neue Warnungen | 5 Bestandswarnungen | **5**, 0 Fehler (beide Bauten) | **erfüllt** |
| Prüfrezepte für beide Befunde | vorhanden, Soll 0 | **2 neue Rezepte, je Ist 0**, gegengeprüft | **erfüllt** |
| Protokoll im Repo | ja | diese Datei, Abschnitt 10 | **erfüllt** |

**S7 (7.1 Strukturbeweis und 7.2 Verhaltensbeweis) ist bestanden.** Offen bleiben
planmäßig **7.3** (Bedienbeweis) und **7.4** (Frontend-Beweis), danach **S8** (Verteilung).

## 10.10 Pfade der Nacharbeit

| Was | Wo |
|---|---|
| Migrierte SQLite-Datei **v2** | `<Scratch>\s7\Kenndaten_S7_v2.sqlite` (64,6 MB) |
| Migrationsbericht v2 | `<Scratch>\s7\S7_Migrationsbericht_v2.md` |
| B-Lauf v2 (SQLite) | `<Scratch>\s7\B_sqlite_v2\` (10 Projektordner, 234 CSV, `lauf_protokoll.md`) |
| Dialekt-Inventur nach dem Fix | `<Scratch>\s7\befund_dialekt_nach_S7.txt` |
| B2-Build (Referenzlauf-Suite, SQLite) | `C:\Users\DirkEngelmann\s7run\B2\bin\Referenzlauf.exe` |
| Proben-Build | `<Scratch>\s7fix_bin\ZugriffsschichtProben.exe` |
| Arbeitskopie des B2-Laufs | `C:\Users\DirkEngelmann\s7run\B2\Referenzlaeufe\Arbeitskopie\` |
| A-Lauf (Access) — **unangetastet** | `<Scratch>\s7\A_access\` (Zeitstempel 13:35) |

Der erste B-Build (`…\s7run\B\bin\`) und sein Ergebnisordner `<Scratch>\s7\B_sqlite\`
bleiben als Beleg des Erstbefunds stehen; die Wiederholung liegt bewusst daneben.
In `C:\Users\DirkEngelmann\s7run\B2\` liegt dieselbe leere `WP-Plan.sln` als Wurzelmarke
wie in `A` und `B`.

## 10.11 Geänderte Dateien

| Datei | Änderung | Kodierung vorher = nachher |
|---|---|---|
| `WindowsFormsApplication1/Controller/ErgebnisCtrl.cs` | B2-Fix + Begründungskommentar | BOM-utf8, rein CRLF, 159 Nicht-ASCII-Bytes |
| `WindowsFormsApplication1/Allgemein/Simulation/SimulationWaermebedarf.cs` | 2 Aufrufstellen + Kommentare | BOM-utf8, rein CRLF, 424 |
| `WindowsFormsApplication1/Allgemein/Simulation/SimulationStrombedarf.cs` | 1 Aufrufstelle + Kommentar | BOM-utf8, rein CRLF, 103 |
| `WindowsFormsApplication1/Allgemein/StromTestClass.cs` | 1 Aufrufstelle + Kommentar | BOM-utf8, rein CRLF, 25 |
| `sql/schema/002_views.sql` | 3 Sichten kuriert + Kopfhinweis | ohne BOM, utf-8, rein LF |
| `sql/tools/Erzeuge-Schema.ps1` | 3 feste Überschreibungen + Kommentare + Kopftext | ohne BOM, utf-8, rein LF |
| `sql/tools/sql_dialekt_inventur.py` | Abschnitte `k)` und `l)` | ohne BOM, utf-8 (rein ASCII), rein LF |
| `sql/MIGRATION_Pruefrezepte.md` | Rezepte 11–13 | ohne BOM, utf-8, rein LF |
| `sql/S7_Protokoll_2026-09-02.md` | dieser Abschnitt 10 + zwei Verweise | ohne BOM, utf-8, rein LF |

**Kodierungsfalle (Rezept 9) eingehalten:** Vor der ersten Änderung wurde jede Datei
gemessen (BOM-Probe, `utf-8`-strict-Probe, CRLF-/LF-Zählung, Nicht-ASCII-Zahl).
**Keine der neun Dateien war cp1252** — die vier Simulations-/Controller-Dateien sind
BOM-utf8 mit durchgängigem CRLF, die `sql/`-Dateien BOM-los utf-8 mit LF. Nach den
Änderungen wurde dieselbe Messung wiederholt: **Kodierung, Zeilenendenart und
Nicht-ASCII-Zahl je Datei unverändert**, Umlaut-Stichprobe in Bestandskommentaren
(„künftiger", „Löschen", „Wärmebedarf") intakt. Das Edit-Werkzeug kam damit nur auf
Dateien zum Einsatz, für die es unbedenklich ist.

Nicht angefasst: `SchemaMigration.cs`, `SchemaKatalog.cs`, `bin\x64\Debug`, die
Live-Datenbanken in `C:\ProgramData\EPOS_PLAN\`, die A-Seite. Es wurde **kein
git-Kommando** ausgeführt.

## 10.12 Bestätigungslauf nach S8 — erneut bitgleich

**Anlass:** Der als eigener Auftrag ausgekoppelte Fix der Befunde B1/B2 wurde erst
**nach** Abschluss der Nacharbeit (10.1–10.11) und nach dem Arbeitspaket **S8**
gestartet. Beide Fixes standen zu diesem Zeitpunkt bereits im Arbeitsbaum — an allen
sechs Stellen nachgeprüft (drei kurierte Sichten in `002_views.sql`, vier Aufrufstellen,
`ErgebnisCtrl.cs:156`). Der Nachweislauf 10.6/10.7 lief allerdings **vor** dem
S8-Abschluss (Erststart-Migration, `Program.cs`-Verdrahtung, DBName-Werksvorgabe).
Dieser Durchgang wiederholt den Beweis deshalb am Baum **mit** S8 — er bindet also
zusätzlich fest, dass S8 die Rechenpfade nicht verändert hat.

| Schritt | Ergebnis |
|---|---|
| Bau (VS-2022-MSBuild, `-restore`, Debug x64, `OutputPath=C:\Users\DirkEngelmann\s7run\B3\bin\`) | **0 Fehler**, exakt die 5 Bestandswarnungen; `EposSqliteMigrator.Kern` wird seit S8 als ProjectReference mitgebaut |
| Lauf `lauf --quelle <Scratch>\s7\Kenndaten_S7_v2.sqlite --ziel <Scratch>\s7\B_sqlite2` (dieselben 10 IDs, Timeout 900 s) | **10/10**, 234 CSV, 00:00:07, **Warnungen 15 / Fehler 0** (zählergleich mit A — diesmal ohne die zwei WAL/SHM-Wächtermeldungen aus 10.6), Exit **0** |
| Suite-`vergleich` gegen `A_access` | **GESAMT PASS**, 2 567 853 Werte innerhalb der Toleranz, Exit 0 |
| `exaktvergleich.py` (SHA-256, ohne Toleranz) | **234/234 byte-identisch**, 2 568 321 Zeilen, 0 abweichende Dateien/Zeilen — `ERGEBNIS: BITGLEICH`, Exit 0 |

Der Bau liegt bewusst in `C:\Users\DirkEngelmann\s7run\B3\` (leere `WP-Plan.sln` als
Wurzelmarke, Arbeitskopie darunter): `B\` bleibt Beleg des Erstbefunds, `B2\` Beleg der
Nacharbeit — der Auftragstext nannte noch `B\bin` als Bauziel und `Kenndaten_S7.sqlite`
als Quelle, beides Stand **vor** der Nacharbeit. Die v1-Datei trägt als Beleg weiterhin
die alten Sichtdefinitionen (Doppel-`ID` in der Datenbank eingebacken, Schemapflege
61 → 61 zieht Sichten nicht nach); der Lauf muss darum gegen `Kenndaten_S7_v2.sqlite`
gehen. Die A-Seite blieb unangetastet (Zeitstempel 13:35).

**In diesem Durchgang wurde keine Quellcode-Datei geändert** (nur dieser
Protokollabschnitt) und kein git-Kommando ausgeführt. Die Abnahme aus 10.9 gilt
unverändert.
