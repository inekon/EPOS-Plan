# K1 · Aufräumung der Kosten-/Energieträgerstruktur — HF1, Code-Seite

**Stand: 19.08.2026.** Umsetzung der Etappe **K1** aus
[`Konzept_Kosten_Energietraeger_EPOS-Plan.md`](../../../Konzept_Kosten_Energietraeger_EPOS-Plan.md)
(§ 3 HF1, Code-Aufräumliste § 3.4, Etappenplan § 10). Ausgangsstand `807324d`.

**Ergebnis in drei Sätzen.** Zwei verwaiste Dateien sind entfernt, und die schreibende
Anlage samt Vorbefüllung der Alttabelle `Tab_KWKG_Staffel` ist aus
`WirtschaftlichkeitCtrl.StelleTabellenSicher()` heraus — eine Datenbank bekommt die
Tabelle ab jetzt weder neu angelegt noch nachgesät. Die beiden
`Tab_Brennstoff_Projekt`-Abschnitte des Handskripts `migration.manuell.sql` sind durch
Hinweiskommentare ersetzt, und die stillgelegte Kostenkategorie 3 ist an ihren zwei
verbliebenen Stellen als solche gekennzeichnet. **Keine einzige Tabelle wurde
gedroppt** — das ist Migrationsschritt **M-E** in Etappe **K6**.

---

## 1 Was geändert wurde

| # | Gegenstand | Datei : Zeile (vorher → nachher) |
|---|---|---|
| 1 | `IniFileParser` gelöscht — verwaister Rest der `UpdateDB.ini`-Ära | `Allgemein/Import/IniFileParser.cs` (55 Zeilen) → entfernt (`git rm`) |
| 2 | Nicht kompilierte Altfassung gelöscht | `Controller/ProjektDuplizierenCtrl_bak2` (25.266 Byte, ohne `.cs`-Endung) → entfernt (`git rm`) |
| 3 | Konstante des Alttabellennamens entfernt | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:60` (`public const string TAB_KWKG_STAFFEL = "Tab_KWKG_Staffel";`) → ersatzlos |
| 4 | DDL + Saat der Staffel aus `StelleTabellenSicher()` entfernt | `WirtschaftlichkeitCtrl.cs:225-260` (zwei `try/catch`-Blöcke: `CREATE TABLE`, `SELECT COUNT(*)`-Prüfung, Saat 2020:5000 … 2030:2500) → ersatzlos; die Nachbarblöcke `TAB_TARIF` (jetzt `:207-223`) und `TAB_MATRIX` (jetzt `:224-238`) sind unberührt |
| 5 | Doku-Absatz an `LadeKwkgStaffel` richtiggestellt | `WirtschaftlichkeitCtrl.cs:1633-1638` → `:1596-1601`. Aus „bleibt unangetastet stehen — sie wird nur nicht mehr gelesen (Konzept L2)" wurde „wird seit Etappe K1 (19.08.2026) auch nicht mehr ANGELEGT … der endgültige `DROP TABLE` folgt in Migrationsschritt M-E (Etappe K6)". Der `<see cref="TAB_KWKG_STAFFEL"/>`-Verweis musste mit — er hätte sonst ins Leere gezeigt. |
| 6 | Kategorie 3 als stillgelegt gekennzeichnet | `Views/Kosten/Form_Kosten.cs:22` (neue Kommentarzeile) über `KATEGORIE_ENERGIE` (`:23`). Die **Konstante bleibt**: `SchemaMigration.cs:1366` (Schritt 19b) referenziert sie historisch, und Migrationsschritte werden nie rückwirkend geändert. |
| 7 | `Tab_Brennstoff_Projekt`-Löschziel gestrichen | `migration.manuell.sql:239` (`DELETE FROM [Tab_Brennstoff_Projekt] …`) → `:239-241`, dreizeiliger Hinweis im Kommentarstil der Datei |
| 8 | `Tab_Brennstoff_Projekt`-Import gestrichen | `migration.manuell.sql:488-490` (`INSERT … SELECT … FROM [{{QUELLE}}].[Tab_Brennstoff_Projekt]`) → `:490-492`, Hinweis; die Abschnittsüberschrift `-- B.11 Uebrige Projekttabellen.` bleibt stehen |
| 9 | Kategorie-3-Import mit Umbau-Hinweis versehen | `migration.manuell.sql:492-494` → fünfzeiliger Kommentar `:494-498` vor dem **unveränderten** `INSERT INTO [Tab_ProjektWerte]` (`:499-501`) |
| 10 | Dieses Protokoll | `Allgemein/Reporting/K1_Aufraeumung_Protokoll.md` (neu) |

**Drei geänderte, zwei gelöschte und eine neue Datei.** Kein Datenbankobjekt wurde
angefasst; das Repo enthält aus K1 keinen einzigen Drop.

---

## 2 Belege der Nichtverwendung

Repoweite Suche, ausgeschlossen `mit_Puffer_KI_Lösungsversuch\`, `Tempkib2\`,
`WindowsFormsApplication1 - Kopie\`, `.claude\worktrees\`, `*.bak`, `bin`/`obj`.

| Gesucht | Treffer im aktiven Code | Bewertung |
|---|---|---|
| `IniFileParser` | **kein Verwender.** Nur `WindowsFormsApplication1.csproj.netfx-backup:280` (Sicherung der alten Projektdatei, nicht im Build) und die Aufzählung in `WindowsFormsApplication1/CLAUDE.md:41`. Der Aufrufweg `ParseIniFile`/`UpdateTablesStructure` existiert allein in der ausgeschlossenen Kopie `mit_Puffer_KI_Lösungsversuch\…\Allgemein/DbClass.cs:136-191`. | löschbar — die aktive `.csproj` ist SDK-getrieben und globbt `**/*.cs`, ein eigener `Compile Include` bestand nicht |
| `ProjektDuplizierenCtrl_bak2` | git-getrackt, ohne `.cs`-Endung und damit nie kompiliert; kein Verweis irgendwo | löschbar |
| `Tab_KWKG_Staffel` / `TAB_KWKG_STAFFEL` | **lesend: nirgends.** Der Lookup läuft seit Etappe E1 über `Tab_Gesetzesparameter` (`WirtschaftlichkeitCtrl.LadeKwkgStaffel` → `GesetzKatalog.Reihe(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL)`). Schreibend war die Tabelle nur in `StelleTabellenSicher()` — genau der jetzt entfernte Block. | write-only bestätigt |
| → `GesetzKatalog.cs:238, 358, 566` | **nur Kommentare**, die auf die Ablösung verweisen („Rückfallebene wie bei Tab_KWKG_Staffel", „löst Tab_KWKG_Staffel ab") | stehen gelassen |
| → `DbWerte.cs:970` | **nur Kommentar** („Loest die Tabelle Tab_KWKG_Staffel ab (Etappe E1, Schritt 4)") | stehen gelassen |
| → `WirtschaftlichkeitDaten.cs:36` | **nur Kommentar** | stehen gelassen |
| → `Reporting/W4_*.md`, `Konzept_Wirtschaftlichkeit.md`, `UMSETZUNGSSTAND.md`, `PRUEFBERICHT_Rechenkern.md`, `Konzept_BHKW_Kosten_Erloese.md`, `Konzept_Berichtserstellung_EPOS-Plan.md`, `Bericht/LIESMICH_Phase1.md` | Historie der Etappe W4/E1 | **bewusst unverändert** — Protokolle werden nicht rückwirkend umgeschrieben |
| `Tab_Brennstoff_Projekt` | **kein C#-Zugriff.** Vor K1 nur `migration.manuell.sql:239, 488-490`; sonst ausschließlich Konzept- und Bestandsaufnahme-Dokumente | Skriptabschnitte streichbar |

---

## 3 Encoding-Befund je angefasster Datei

Der Baum führt 93 von 372 `.cs`-Dateien als BOM-loses cp1252; ein Werkzeug ohne
Kodierungsangabe zerstört dort die Umlaute. Deshalb wurde jede Datei **vor** dem
Schreiben gemessen und danach per Byte-Diff gegengeprüft.

| Datei | Befund | Bearbeitungsweg |
|---|---|---|
| `Allgemein/Import/IniFileParser.cs` | UTF-8 **mit** BOM | gelöscht — Kodierung ohne Belang |
| `Controller/ProjektDuplizierenCtrl_bak2` | **cp1252 ohne BOM** (erstes hohes Byte `0xFC` an Position 2419) | gelöscht — nie mit einem Textwerkzeug geöffnet |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | UTF-8 **ohne** BOM, CRLF, 162.574 Byte | byte-erhaltend per Python (`read bytes → decode utf-8 → Zeilenschnitt → encode utf-8 → write bytes`), Roundtrip vorher als bytegleich verifiziert. Ergebnis 160.325 Byte, weiterhin **ohne** BOM, 2.810 CRLF, **0** nackte LF |
| `Views/Kosten/Form_Kosten.cs` | UTF-8 **mit** BOM, CRLF, 79.396 Byte | byte-erhaltend per Python, BOM ausdrücklich wieder vorangestellt. Ergebnis 79.557 Byte (+161 = die eine Kommentarzeile), 1.605 CRLF, 0 nackte LF |
| `migration.manuell.sql` | UTF-8 **mit** BOM, CRLF, 66.195 Byte | byte-erhaltend per Python. Ergebnis 66.527 Byte, 524 CRLF, 0 nackte LF |
| `Allgemein/Reporting/K1_Aufraeumung_Protokoll.md` | neu | UTF-8 **ohne** BOM, CRLF — nach `.editorconfig` `[*.md]` |

**Byte-Diff-Gegenprobe.** Der Vergleich Vorher/Nachher zeigt für
`WirtschaftlichkeitCtrl.cs` genau drei Hunks (Zeile 60, Block 225-260, Absatz
1633-1638), für `Form_Kosten.cs` genau eine eingefügte Zeile und für
`migration.manuell.sql` genau zwei Hunks. Keine unbeabsichtigte Byteänderung, kein
verschobener Umlaut, keine neue oder verlorene BOM.

---

## 4 Build — Baseline gegen Ende

`dotnet build` kann das Hauptprojekt in diesem Baum **grundsätzlich nicht** bauen:
`WindowsFormsApplication1.csproj:131-148` führt zwei `COMReference`-Einträge
(Excel-Interop, VBIDE), und die .NET-Core-Fassung von MSBuild bricht daran mit
`MSB4803` ab. Das gilt vor wie nach K1 und ist keine Folge dieser Etappe — auch der
in `WindowsFormsApplication1/CLAUDE.md:13` genannte Aufruf trifft deshalb nur die
Nebenprojekte. Gebaut wurde mit der .NET-Framework-Fassung:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    WindowsFormsApplication1\WindowsFormsApplication1.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x86
```

| Lauf | Ergebnis | Fehler | Warnungen |
|---|---|---|---|
| **Baseline** (`807324d`, vor K1) | erfolgreich | **0** | **6** |
| **Ende** (nach K1) | erfolgreich | **0** | **6** |

Die sechs Warnungen sind zeilengleich dieselben und stammen sämtlich aus unberührten
Dateien: `Model/WErzeugerModel.cs(6,20)` CS0108,
`Controller/KlimaregionStammCtrl.cs(22,24)` und `(23,48)` CS0109,
`Controller/StromverbraucherStammCtrl.cs(25,44)` CS0108, `MDIMainForm.cs(348,28)`
CS1998 sowie `(359,17)` CS4014. **Keine neue Warnung aus einer K1-Datei**, keine
verschwundene.

**Konfliktmarker-Sweep.** 875 Dateien (`*.cs`, `*.md`, `*.resx`, ohne die
ausgeschlossenen Bäume) auf `<<<<<<<` geprüft: **kein echter Treffer**. Die beiden
Fundstellen in `Konzept_Kosten_Energietraeger_EPOS-Plan.md:268` und `:283` sind der
in Backticks gesetzte Prosa-Verweis auf eben diesen Sweep.

---

## 5 Was K1 ausdrücklich NICHT tut

K1 ist reine Code- und Dokumentaufräumung. **Kein `DROP TABLE`, kein
`DROP CONSTRAINT`, keine gelöschte Access-Abfrage, keine gelöschte Beziehung.**
Sämtliche Datenbankeingriffe der Löschlisten § 3.2 und § 3.3 gehören in
**Migrationsschritt M-E, Etappe K6** — zuletzt in der Reihenfolge
M-A → M-B → M-C → M-D → M-E, und erst nachdem die Checkliste in Abschnitt 6
abgearbeitet ist.

Nach K1 gilt für `Tab_KWKG_Staffel`: in einer Bestandsdatenbank **steht sie weiter mit
ihren acht Zeilen**, wird aber weder gelesen noch neu angelegt noch nachgesät. In
einer frisch angelegten Datenbank entsteht sie gar nicht mehr. Beides ist ohne
Rechenwirkung — der Jahresdeckel kommt seit Etappe E1 aus `Tab_Gesetzesparameter`.

Ebenfalls nicht in K1, so vom Konzept vorgesehen: der Umbau des Summen-Labels
„PROJEKT GESAMT (Energiekosten)" von der toten Kategorie-3-Summe auf den
`KostenEmissionRechner` ist **HF4/Etappe K4**; die Löschung der Kategorie-3-Altzeilen
in Bestandsdatenbanken (Entscheidung E3) folgt erst danach.

---

## 6 Manuelle Access-Checkliste (Konzept Anhang B) — vor/mit K6

Diese Schritte kann kein Code erledigen: gespeicherte Abfragen und Beziehungen liegen
in `Kenndaten.accdb`, ihr SQL ist aus dem Repo nicht lesbar. Abzuarbeiten in Access an
der Produktiv-Datenbank, **bevor** M-E die Tabellen dropt. Vorher prüfen, ob
`Kenndaten.laccdb` existiert (dann ist die DB geöffnet), und eine datierte Kopie
anlegen.

### 6.1 Objektabhängigkeiten je Lösch-Abfrage

- [ ] **Objektabhängigkeiten aktivieren**: Access → Name-AutoKorrektur-Info
      einschalten, damit der Abhängigkeitsbereich überhaupt rechnet.
- [ ] Für **jede** Abfrage aus Konzept § 3.3 prüfen, ob eine andere Abfrage sie
      referenziert. Besonders die zwei bekannten Ketten:
  - [ ] `Abfrage_MaxMin_Vorlauf` ↔ `Abfrage_Max_Vorlauf` / `Abfrage_Min_Vorlauf`
  - [ ] `Abfrage_Kuehlung_MaxLast` ↔ `Abfrage_KenndatenKuehlung_Max`
- [ ] Die übrigen Kandidaten durchgehen: `Abfrage_KostenKomponenten` (abgelöst durch
      `Form_Kosten.LiesKomponentenSummen`), `Abfrage_ProjektKostenEnergie`,
      `Abfrage_ProjektKostenKomponenten`, `Abfrage_Kosten_WP` / `_Heizkessel` /
      `_BHKW` / `_Photovoltaik` / `_Solarthermie` / `_Pufferspeicher` /
      `_Stromspeicher`, `Abfrage_Heizkessel_Kosten`,
      `Abfrage_Erzeuger_Vorlauftemperaturen`,
      `Abfrage_Erzeuger_Ruecklauftemperaturen`, `Abfrage_SST`.
- [ ] **`Abfrage_ProjektKostenInvestBetrieb` löschen** — beschlossen als Entscheidung
      **E4** (kein C#-Aufrufer). `KostenPositionCtrl.GruppeSichern` selbst **bleibt**
      (Katalogpflege weiter sinnvoll); anzupassen ist nur der Kommentar dort
      (`KostenPositionCtrl.cs:198-217`).
- [ ] **Nicht anfassen** (aktiv im Betrieb): `Abfrage_Energietraeger_Effektiv`,
      `Abfrage_Kostenfaktoren`, `Abfrage_Gebaeudearten` / `-typen`,
      `Abfrage_Projektgebaeude`, `Abfrage_ProjektGebaeudeGanglinie`,
      `Abfrage_ProjektStromGanglinie`, `Abfrage_Tagverteilung`,
      `Abfrage_Monatsstrom`, `Abfrage_Monatswaerme_Prozesse` / `_Brauchwasser`.

### 6.2 Die `energy_unit`-Join-Abfrage identifizieren

- [ ] Die Abfrage finden, die `energy_carrier` + `energy_group` + `energy_conversion`
      + `pricing_model` + **`energy_unit` vierfach** (Aliasse `a_unit`, `a_unit_1`,
      `a_unit_2`, `a_unit_3`) joint. **Kandidat: `Abfrage_Neues_Kosten_Model`** — vor
      dem Löschen in der SQL-Ansicht gegenprüfen, nicht nach dem Namen gehen.
- [ ] Diese Abfrage löschen, **bevor** `energy_unit` und `energy_group` gedroppt
      werden — sonst bleibt eine kaputte Abfrage in der Datenbank zurück.

### 6.3 Beziehungen löschen

- [ ] `Tab_ProjektTab_Brennstoff_Projekt`
- [ ] `Tab_Brennstoff_StammTab_Brennstoff_Projekt`

Im Regelfall erledigt das der Migrationsschritt per `ALTER TABLE … DROP CONSTRAINT`.
Weichen die Namen in der Produktiv-Datenbank ab, im Beziehungsfenster von Hand lösen
und den abweichenden Namen hier notieren: ________________________

### 6.4 Sichtprüfung der mehrdeutigen Objekte

Namen aus dem DB-Katalog, möglicherweise Extraktionsartefakte; **0 C#-Treffer**. Nur
löschen, wenn sie tatsächlich existieren **und** leer sind.

- [ ] `Tab_KostenKategorien` (Plural — nicht mit `Tab_KostenKategorie` verwechseln)
- [ ] `Tab_ErgebnisKomponente`
- [ ] `Tab_ErgebnisMonat`
- [ ] `Tab_Gebaeude1`

### 6.5 Abschluss

- [ ] **Komprimieren und Reparieren** ausführen.
- [ ] Kopie nach `Referenzlaeufe\Arbeitskopie\` aktualisieren.
- [ ] Referenzläufe erneut rechnen und gegen `Referenzlaeufe\2026-08-19_B5`
      vergleichen.
- [ ] Ausführung in der Produktiv-DB dokumentieren (Konzept § 9 Punkt 3).

---

## 7 Offene Punkte

1. **Die Abnahme von K1 ist noch unvollständig.** Das Konzept fordert „Build grün;
   Referenzläufe B5 byte-identisch; Duplizieren-Smoke". Der Build ist grün und
   baselinegleich; **Referenzlauf-Vergleich und Duplizieren-Smoke stehen aus** — sie
   waren nicht Teil des Arbeitsauftrags. Fachlich ist keine Abweichung zu erwarten:
   die Engine ist unberührt, entfernt wurde ausschließlich ein schreibender DDL-Pfad
   auf eine Tabelle, die niemand liest.
2. **`WirtschaftlichkeitCtrl.cs:1000`** beschreibt die Vbh-Staffel weiterhin als
   „Katalog Tab_KWKG_Staffel". Der Satz ist seit E1 sachlich überholt (Quelle ist
   `Tab_Gesetzesparameter`), lag aber außerhalb des K1-Auftrags, der nur den Absatz
   `:1633-1638` benannte. Ein Einzeiler für K6.
3. **`WindowsFormsApplication1/CLAUDE.md:41`** führt `IniFileParser` noch in der
   Aufzählung des `Import/`-Ordners. Die Datei gehörte nicht zum Commit-Umfang von K1
   und wurde deshalb nicht angefasst.
4. **`migration.manuell.sql:23`** nennt `Brennstoff_Projekt` in der Erläuterung des
   Kopie-ID-Schemas („Zuordnungs-IDs …: +10000"). Ebenfalls stehen gelassen — der
   Auftrag benannte nur die zwei Abschnitte und den Kategorie-3-Import.
5. **Der Kommentar in `KostenPositionCtrl.GruppeSichern` (`:198-217`)** ist nach
   Entscheidung E4 anzupassen, sobald `Abfrage_ProjektKostenInvestBetrieb`
   tatsächlich gelöscht ist (Abschnitt 6.1). Nicht Teil von K1.
6. **Die Ausschlussliste `ProjektDuplizierenCtrl.cs:47-48`** führt `energy_unit` und
   `Tab_KostenKategorie`. Die Einträge werden erst mit den Drops in K6
   gegenstandslos und bleiben bis dahin stehen.
