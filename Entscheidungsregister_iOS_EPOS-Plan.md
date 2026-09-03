# Entscheidungsregister: EPOS-Plan auf iOS

**Stand 02.09.2026 — Arbeitsliste zu iU0 (Klärung, Sicherung, Rückbau)**

> **Verhältnis zum Umsetzungskonzept**
> [`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md) (Rev. 2, 02.09.2026)
> begründet: warum eine Frage gestellt wird, was sie kostet, wovon sie abhängt. **Dieses Register
> begründet nichts.** Es führt Buch: welche Frage steht offen, wer hat sie wann wie entschieden,
> welches Paket wartet darauf. Wo Register und Konzept auseinandergehen, gilt das Konzept für die
> Begründung und das Register für den Stand.
>
> Es deckt die vier Punkte ab, die iU0 als Abnahme verlangt: Entscheidungen (§ 1), Referenzbasis
> (§ 2), Auszählung der Chart- und Grid-Masken (§ 3) und die Terminierung der offenen
> x64-Punkte (§ 4). Der Rückbau (`CSExeCOMServer`, `csproj.netfx-backup`) ist mit `c3a8233`
> bereits ausgeführt und hier nicht mehr geführt.

**Pflegehinweis.** Die Spalten *Entscheid* und *Datum* füllt der Anwender aus — niemand sonst. Eine
Zeile gilt erst als beschieden, wenn beide Felder stehen; „Empfehlung laut Konzept" ist keine
Entscheidung. Das Register wird **bei jeder iF-Entscheidung fortgeschrieben** und der Stand im Kopf
mitgezogen. Ergibt eine Entscheidung eine Änderung am Konzept, wird sie dort nachgeführt, nicht hier
ausgebreitet.

---

## 1 Entscheidungsregister iF1–iF18

iF1–iF9 stammen aus [`Konzept_iOS-Portierung_EPOS-Plan.md`](Konzept_iOS-Portierung_EPOS-Plan.md)
§ 7, iF10–iF18 aus dem Umsetzungskonzept § 8.2. Die Spalte **benötigt ab** nennt das Paket, das ohne
die Entscheidung nicht begonnen werden kann.

| Nr. | Frage (Kurzform) | Empfehlung laut Konzept | benötigt ab | Status | Entscheid (Anwender) | Datum |
|---|---|---|---|---|---|---|
| **iF1** | S0-Spike (Kernrechnung im Simulator, Projekt 1030) beauftragen? | ja | iU3 | offen | | |
| **iF2** | Voller Funktionsumfang — oder erste Auslieferung ohne Katalog-Admin? | ohne Katalog-Admin | iU11 | offen | | |
| **iF3** | UI-Technologie: Blazor Hybrid oder MAUI-XAML? | Blazor Hybrid | iU8 | offen | | |
| **iF4** | Kern-Herauslösung unabhängig vom iOS-Ziel einplanen? | ja | iU1 | offen | | |
| **iF5** | Vertriebsweg im Grundsatz | zunächst TestFlight — im Umsetzungskonzept § 3.4 präzisiert: TestFlight ist **kein** Auslieferungsweg (90 Tage) | iU13 | offen — geht sachlich in iF12 auf | | |
| **iF6** | Windows-Charts ebenfalls auf ScottPlot? | mittelfristig vereinheitlichen, **nicht** Teil dieses Vorhabens | iU7 | offen | | |
| **iF7** | Formular-Generator (Feldinventar aus den 118 `Designer.cs`) als Werkzeug? | ja | iU8 | offen | | |
| **iF8** | **Modell C beschließen** (Strangler-Regel M1) | ja | **iU8 — ohne diesen Beschluss ist iU8 gegenstandslos** | offen | | |
| ~~**iF9**~~ | ~~SQLite auch auf Windows, mit Stichtag~~ | ja | — | **beschieden und ausgeführt 02.09.2026 (`6486c36`)** | ja | 02.09.2026 |
| **iF10** | `IDatenzugriff` mit providerneutralem `DbParam` (Weg b) — oder ~2.300 `OleDbParameter`-Aufrufe maschinell ersetzen (Weg a)? | **Weg (b)**; Weg (a) bleibt spätere Aufräumoption | iU6 | **Weg (b) ausgeführt 03.09.2026** (`22fb7eb`…`2387abf`, § 2.5); Weg (a) hat sich mit dem Masken-Sweep iU6-T3a miterledigt. Entscheid des Anwenders steht noch aus | | |
| **iF11** | Mac-Hardware sofort beschaffen — oder Spike auf `macos-latest`-CI-Runner? | **CI-Runner** für den Spike, Mac erst mit iU10 | iU3 | offen | | |
| **iF12** | Vertriebsweg der Auslieferung (Custom Apps / Unlisted / App Store) und Behandlung des Lizenzverkaufs gegenüber Apples Kaufregeln | **Custom Apps** über Apple Business Manager prüfen; Klärung **vor** iU13, nicht im Review | vor iU13 | offen | | |
| **iF13** | Root-Namespace `WindowsFormsApplication1` beim Kern-Umzug mit umbenennen? | **nein** — eigener mechanischer Schritt danach | iU4 | offen | | |
| **iF14** | `Kenndaten_Test.sqlite` mit den 13 Referenzprojekten versionieren? | **ja** — sonst ist die Kern-CI nur ein Kompilierungstest (iR6). Befund 02.09.: siehe § 2.1 | iU3 (Baustein iE6) | **beschieden** | **ja — Anwender bestätigt 02.09.2026: die Datenbank enthält nirgends Kundendaten.** Anonymisierung entfällt; Reduzierung auf die 13 Projekte nur wegen der Dateigröße (GitHub-Grenze 100 MB) | 02.09.2026 |
| **iF15** | Wie ist „wertgleich" zwischen x64 und ARM64 definiert? | bestehende Toleranz (rel. 1e-4 / abs. 0,01) für den Plattformvergleich; **Byte-Gleichheit** bleibt Maßstab für Windows-interne Umbauten | vor iU3 | **beschieden** | Toleranz wie heute (rel. 1e-4 / abs. 0,01) für den Plattformvergleich; Befund 02.09.: 1030 auf x64-Linux **und** arm64-macOS byte-gleich — die Toleranz wird bisher nicht einmal gebraucht | 02.09.2026 |
| **iF16** | Chart-Weg in Blazor Hybrid: ScottPlot als Bild, JS-Bibliothek oder natives Steuerelement? | **ScottPlot als Bild** — ein Stack für Bericht und Bildschirm | iU7 | offen | | |
| **iF17** | iU1 (Fundament, .NET 10, CI, COM-Entfernung) **unabhängig vom iOS-Beschluss** beauftragen? | **ja** — Support-Frist 10.11.2026, einzige Antwort auf iR9 | iU1 | **beschieden** | **ja — iU1 läuft seit 02.09.2026 auf Branch `ios_migration`** | 02.09.2026 |
| **iF18** | Welche VS-2026-Edition? (VS 2022 kann `net10.0` nicht targeten) | **Community 2026**, sofern INEKON unter den Enterprise-Schwellen bleibt; sonst Professional | vor iU1 | **beschieden** | **Community 2026 — installiert unter `C:\Program Files\Microsoft Visual Studio\18\Community`** | 02.09.2026 |
| **iF19** | Schrift der Berichts-Charts nach der SkiaSharp-Portierung (iU7): mitgelieferte Schrift (plattformgleiche Bilder) oder Systemschrift (plattformpassend)? | Konzept offen; Vermessung iU7: `"Calibri"` steht 15× hart im `ChartRenderer`, Legendenumbruch hängt an Textmaßen | iU7 | **beschieden** | **Systemschrift, flexibel** — Rückfallkette Calibri (Windows) → Systemschrift (macOS/iOS) → Sans (Linux) über `SKFontManager`; Layout bleibt metrikgetrieben, Textbreiten dürfen je Plattform abweichen. Folge: Bildvergleich Windows↔Linux ist Struktur-/Histogrammvergleich, kein Pixelvergleich | 02.09.2026 |

**Was zuerst gebraucht wird.** Vor dem Beginn von iU3 müssen **iF1, iF11, iF14 und iF15** stehen —
sie definieren, was der Spike ist und woran er gemessen wird; ohne sie ist das Go/No-Go-Gate iZ3
nicht bewertbar. **iF8** ist die einzige Frage, deren Ausbleiben ein ganzes Paket (iU8) entwertet.
**iF12** hat die längste Vorlaufzeit (Apple-Prozesse) und gehört deshalb nicht ans Ende.

**Nicht auf der Liste.** iL1–iL8 (Leitentscheidungen), M1–M10 (Migrationsregeln) und D1–D8
(Datenschicht) sind beschieden oder erledigt und werden hier nicht erneut aufgerufen. M3
(„übergangsweise zwei Dialekte") ist mit iF9 ersatzlos entfallen.

---

## 2 Referenzbasis

**`Referenzlaeufe/2026-08-30_B3-Kaskade/` ist der verbindliche Bezugspunkt aller iT1-Nachweise
(Byte-Gleichheit) für iU1 bis iU5.** Was sich zwischen einem Paketstart und seiner Abnahme nicht
ändern darf, wird gegen genau diesen Stand gehalten — nicht gegen einen neu erzeugten Lauf.

| Merkmal | Wert |
|---|---|
| Projekte | **13** — 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039, 1040, 1041, 1042 |
| Ergebnisdateien | **332 CSV** |
| Codestand | **`bad41f8`** (Branch `Pufferspeicher`, B3-Serie), gebaut aus einem `git archive HEAD`-Export außerhalb des Repos (`C:\Waermeplan\_basisbuild`, 0 Fehler) |
| Datenquelle | produktive `Kenndaten.accdb`, Zeitstempel **30.08.2026 06:13:43**, **Schemastand 61**, nur gelesen (keine `laccdb`) |
| Selbstvergleich | **13/13 PASS** (3 558 333 Werte), **332/332 byte-/MD5-gleich**, `pruefen` 13/13 plausibel |

**Nicht Teil der Basis:** 1043 und 1044 (weitere Booster-Varianten). Ihre Aufnahme wäre ein eigener,
bewusster Basiswechsel — kein Nebenprodukt eines Paketabschlusses.

**Zur Behandlung durch iU1.** Der Frameworkwechsel auf .NET 10 ist der erste Nachweis gegen diese
Basis (iU1 Schritt 8): **332/332 byte-gleich**. Bewegt er ein Ergebnis, ist das ein Befund, keine
neue Basis.

**Ein Git-Tag auf `bad41f8` ist sinnvoll** — die Basis hängt heute an einer Commit-Kennung in einer
Markdown-Datei, nicht an einer Marke im Repo. Er löst das Problem aber nur halb:
**`GitHub_Sync.bat` überträgt keine Tags**, es schiebt ausschließlich den Zweig `main`. Setzen und
Übertragen der Marke bleiben damit Handarbeit am Windows-Arbeitsplatz — ein `tag -a` auf `bad41f8`
und anschließend ein ausdrückliches `push --tags`.

Das ist derselbe offene Punkt wie **(e)** der x64-Umstellung (§ 4): Der Rückweg-Tag
`letzter-x86-stand` (`3f126f4`) ist bis heute nicht übertragen — **eine Tag-Abfrage gegen `origin`
liefert am 02.09.2026 keinen einzigen Eintrag.** Beide Marken existieren derzeit nur auf einem
Rechner (iR9).

### 2.1 Befund zur CI-Testdatenbank (iF14), 02.09.2026

Zwei Messungen am Bestand, die die Entscheidung vorbereiten:

| Frage | Befund |
|---|---|
| Enthalten die Referenz-CSV personenbezogene Daten? | **Nein.** Die `aggregate.csv` führen ausschließlich technische Bezeichner — Projektname „Simulation Referenz BHKW-Kaskade (Regressionstest)", Gerätenamen („EC-POWER XRGI 9", „Vitocrossal 200 CM2"), Speicherbezeichner — keine Kunden-, Adress- oder Kontaktfelder. Die 45 MB CSV liegen bereits versioniert im Repo |
| Liegt die Datenbank mit den 13 Projekten im Repo? | **Nein.** `Kenndaten.sqlite` existiert nur unter `C:\ProgramData\EPOS_PLAN` auf dem Anwenderrechner; `Setup/Vorlage/` ist gitignored und im Checkout leer. Ohne die Datenbank kann kein Runner (CI, Linux, macOS) ein Projekt rechnen — die Kern-CI bleibt ein Kompilierungstest |

**Folge:** iF14 ist eine Handlung des Anwenders: eine **Kopie** der `Kenndaten.sqlite` auf die 13
Referenzprojekte reduzieren (übrige `Tab_Projekt`-Zeilen samt Kaskaden löschen, `VACUUM`) und als
`Referenzlaeufe/Kenndaten_Test.sqlite` einchecken. **Nachtrag 02.09.2026, Anwender:** Die Datenbank
enthält nirgends Kundendaten — die Prüfung auf Kundenbezug entfällt, die Reduzierung dient allein
der Dateigröße: **Die produktive `Kenndaten.sqlite` hat 148 MB** (Anwender, 02.09.2026) — GitHub
lehnt Einzeldateien über 100 MB ab, die Reduzierung auf die 13 Projekte ist damit Pflicht. Das
Reduzierungsskript liegt unter `sql/tools/`. Erst damit sind der Spike (iU3) und der Referenzlauf in der CI (iE6) möglich. Die
Referenz-CSV selbst sind nach dem Befund oben unkritisch. **Nachtrag: Der Anwender hat die
Reduzierung am 02.09.2026 selbst durchgeführt — Ergebnis 76 MB** (von 148 MB), unter der
GitHub-Grenze; kein LFS nötig. Die Datei ist als `Referenzlaeufe/Kenndaten_Test.sqlite` einzuchecken.

**Ist-Stand der eingecheckten Datei (02.09.2026, `db66c95`):** `Referenzlaeufe/Kenndaten_Test.sqlite`,
73,4 MB, `integrity_check` ok, `foreign_key_check` leer, 114 Tabellen / 14 Sichten, SchemaVersion 61,
Journal-Modus WAL. **Sie enthält 23 Projekte** (19, 1006–1009, 1017–1019, 1023–1032, 1039–1044),
davon **11 der 13 Referenzprojekte — 1011 und 1021 fehlen.** Für den Spike (nur 1030) genügt das;
für den vollen 13/13-Vergleich (iT1/iT3) braucht es eine neue Kopie aus der Produktivdatenbank per
`VACUUM INTO` und `sql/tools/Reduziere-Testdatenbank.sql`. Die überzähligen Projekte wurden bewusst
nicht entfernt (Auswahl des Anwenders); größte Tabelle `Tab_StromganglinieDaten` mit 823.441 Zeilen.

**Echtlauf des Reduzierungsskripts** (02.09.2026, auf einer Kopie dieser Datei, nicht eingecheckt):
`sql/tools/Reduziere-Testdatenbank.sql` per `executescript` — 1,1 s; 23 → **11** Projekte (die
vorhandenen der 13), `Tab_Applikation.ID_Projekt` → 1030, `Tab_StromganglinieDaten` 823.441 → 473.040
Zeilen, **73,4 → 46,2 MB**, `integrity_check` ok, `foreign_key_check` leer. Das Skript ist damit
auf dem echten Bestand belegt; die Probe gegen die Schema-DB hatte den Größengewinn nicht zeigen
können.

### 2.2 Befund zum Machbarkeits-Spike (iU3), 02.09.2026 — zwei Messungen auf Linux

Vor dem Spike wurde geprüft, was der heutige Bestand außerhalb von Windows *zur Laufzeit* tut.
Beide Tests liefen hier, ohne Mac, ohne Datenbank:

| Test | Ergebnis | Folge |
|---|---|---|
| **A** — die fertig gebaute `Referenzlauf.dll` (`net10.0-windows`) auf Linux starten, Modus `vergleich` (braucht keine Datenbank) | **startet nicht:** „Framework `Microsoft.WindowsDesktop.App` 10.0.0 not found". Nicht die Typreferenzen blockieren, sondern die `FrameworkReference`, die `UseWindowsForms=true` in die `runtimeconfig.json` schreibt — dieses Shared Framework existiert nur auf Windows | Der Rechenkern muss in einer Assembly **ohne** `UseWindowsForms` liegen (`EPOS.Kern`, `net10.0`). Ein Runner, der die WinForms-App referenziert, läuft nirgends außer Windows — egal wie wenig WinForms er nutzt |
| **B** — `new OleDbParameter("@p0", 42.5)` in einem `net10.0`-Konsolenprojekt mit `System.Data.OleDb 10.0.11` auf Linux | **wirft `PlatformNotSupportedException`** schon im Konstruktor; ebenso `OleDbConnection` | `OleDbParameter` ist auf Nicht-Windows **kein** Datenträger, sondern eine Wand. Jeder Aufruf von `DataRepository.GetDataTable(sql, params OleDbParameter[])` mit Parametern scheitert. **`DbParam` (Umsetzungskonzept § 1.4, iF10) ist damit Vorbedingung des Spikes, nicht Folgearbeit** |

Dazu die Abhängigkeitsmessung (Agent, 02.09.): Ein headless-Lauf von Projekt 1030 zieht transitiv
**180 Dateien / 132.117 Zeilen** (80 % aller Nicht-View-Zeilen) — wegen eines Abhängigkeitsknäuels
von 64 Dateien über `SimulationControl` → `SchemaMigration`/`PufferSpCtrl`/`WirtschaftlichkeitCtrl`
→ `Program.cs`. Die WinForms-Bindung *im* Rechenkern ist dagegen winzig: vier tote `using`, vier
`Cursor.Current`, ein Formularaufruf (`Warnkriterien.cs:525`), zwei echte `MessageBox` in
`AnlagenEindeutigkeit.cs`, `Form_Kosten.KATEGORIE_*`-Konstanten — alles Ein-Zeilen-Fixes. Für den
Lauf selbst ist der Weg bereits dialogfrei (`EngineModus`, `SimulationProtokoll`,
`PfadUeberschreibung` existieren). `OleDb` ist die einzige Bindung ohne Ein-Zeilen-Ausweg:
**3.787 Vorkommen in 115 Nicht-View-Dateien.**

**Folge für iU3:** Der Spike ist kein Wegwerf-Auszug „für wenige Tage" (Grundlagenkonzept S0),
sondern setzt zwei Umbauten voraus, die das Konzept erst für iU4/iU6 vorsah:
1. `OleDbParameter` → `DbParam` im gesamten Bestand (Weg (a) aus § 1.4 — maschinell, hier
   kompilierprüfbar);
2. Rechenkern-Auszug in eine `net10.0`-Assembly ohne `UseWindowsForms` — mindestens die
   Simulationskette plus `DataRepository`, mit den Ein-Zeilen-Fixes oben.
Beides ist auf Linux nachweisbar, bevor ein Mac beteiligt ist. **iF11 verschiebt sich damit:** Der
Spike läuft zuerst auf Linux (hier), macOS-Runner folgt für die ARM64-Frage (iF15).

### 2.3 Ergebnis des Machbarkeits-Spikes (iU3), 02.09.2026 — **bestanden, byte-gleich**

| | |
|---|---|
| Umsetzung | fünf Commits `13cedbb`…`db9f00f` (Brücken gekappt, WinForms gelöst, `OleDbCommand` aus dem Pfad, Stromspeicher-Haken, Projekte `EPOS.Kern` + `EPOS.Referenzlauf`); Hauptprojekt unverändert baubar, Warnliste byte-gleich zur Basis, 787 Tests |
| `EPOS.Kern` | `net10.0`, `EnableWindowsTargeting=false`, **91 verlinkte Dateien** (90 aus `WindowsFormsApplication1/` + `SchemaTypKatalog.g.cs`; Planung: ≈ 86) — der Compiler hat entschieden. 87 × CA1416 = das Rest-OleDb-Inventar (`SolarkollektorenCtrl` 41, `PufferSpCtrl` 30, `RecordSet` 9, `ApplikationCtrl` 7) für iR8 |
| Lauf | `EPOS.Referenzlauf lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030` auf Linux x64 (Ubuntu 24.04, SDK 10.0.400): 22 CSV, 150 Skalare, eine erwartete Engine-Warnung (Senke Anlage 14921) |
| Vergleich | gegen `2026-08-30_B3-Kaskade/Projekt_1030`: **PASS, 236.670 Werte in Toleranz — und alle 22 Dateien byte-identisch** (`diff -rq` leer), einschließlich `aggregate.csv` |
| Wiederholt | auf dem Merge-Stand mit FX1/FX2 (`db9f00f`): identisches Ergebnis |

**Bedeutung:** Der Rechenkern rechnet außerhalb von Windows nicht nur „wertgleich innerhalb der
Toleranz", sondern bit-identisch zum Windows-Referenzlauf vom 30.08. — keine Plattform-, keine
Datendrift auf x64. Damit ist Gate **iZ3 (Go)** erreicht. Offen bleibt die ARM64-Frage (iF15): Der
CI-Schritt „Kern-Referenzlauf 1030" in `kern.yml` läuft ab jetzt auch auf `macos-latest` — sein
erstes Ergebnis ist der iT3-Nachweis für Apple Silicon.

**Nachtrag, CI-Lauf auf `edefbef` (02.09.2026, kern.yml mit Vergleichsschritt):**

| Runner | Referenzlauf 1030 | Vergleich gegen Basis | Byte-Diff |
|---|---|---|---|
| `ubuntu-latest` (x64) | ✅ | **PASS**, 236.670 Werte | **alle 22 Dateien identisch** |
| `macos-latest` = `macos-26-arm64` (**Apple Silicon**) | ✅ | **PASS**, 236.670 Werte | **alle 22 Dateien identisch** |

Die für ARM64 erwartete Gleitkomma-Drift (iF15, Analogie zum x86→x64-Umstieg mit FMA) tritt auf
diesem Rechenpfad **nicht** auf: Der Kern rechnet auf x64-Windows, x64-Linux und arm64-macOS
bit-identisch. Das ist der iT3-Nachweis (Plattformvergleich) — geführt ohne Mac-Hardware, auf einem
kostenlosen Runner, mit jedem Push wiederholt. Die Ergebnis-CSV liegen je OS als Artefakt am Lauf
(14 Tage).


### 2.4 Befund iU4 (`EPOS.Kern` herauslösen), 03.09.2026 — **hier erreicht**

Sieben Commits `4a0a4e2`…`616dff4` auf der Basis `9fe9c71`.

**Umfangszahlen.** `EPOS.Kern` führt **168 Dateien**, seit iU4-5 physisch unter `EPOS.Kern/`
(vorher 91 verlinkte). Die Planung nannte 165; die Abweichung ist erklärt und klein:

| Posten | Dateien |
|---|---|
| Bestand aus iU3 | 91 |
| `Allgemein/Wirtschaftlichkeit/*` (`HilfsstromRechner` war schon dabei) | +19 |
| Bericht-DATEN (`BerichtTexte`, `BerichtsDaten`, `EmissionsAusweis`, `KostenEmissionRechner`, `ProjektDetails`, `KennzahlenKatalog`, `AbweichungsErmittler`) | +7 |
| `Model/*` (alle 46) | +24 |
| `Allgemein/Simulation/*` ohne `SchemaModell.cs` — die K8-Hälfte `SimulationControl.Stromspeicher.cs` kommt mit | +1 |
| Controller (`StromspeicherSimCtrl` + die 23 geplanten, einschließlich `WErzeugerCtrl.Aufraeumen.cs`) | +24 |
| `Settings.cs` (zweite `partial`-Hälfte von `Properties.Settings`) | +1 |
| **vom Compiler verlangt:** `Allgemein/Sprache.cs`, `Allgemein/ZahlText.cs` (aus iU4-1) | +2 |
| **Summe verlinkt (iU4-4)** | **169** |
| davon Link auf `../sql/schema/SchemaTypKatalog.g.cs` — bleibt Link, zieht nicht um | −1 |
| **Summe verschoben (iU4-5)** | **168** |

Der Compiler hat außer `Sprache.cs` und `ZahlText.cs` **nichts** weiter verlangt. Die Anwendung
schrumpft von 585 auf 417 `.cs`. `git diff -M`: 171 R100-Umbenennungen (168 `.cs` + `Resource.resx`,
`Resource.en-US.resx`, `Settings.settings`) und zwei `.csproj` — kein Quelltext angefasst.

**Die Partial-Lösung.** Zwei Klassen waren über `partial` in eine Kern- und eine
Oberflächenhälfte geteilt. Das trägt nur innerhalb EINER Assembly. Aufgelöst wurde beides ohne
Änderung an einer Aufrufstelle:

* `WizardItemClass` (Typ- und Nummernkatalog) bleibt im Kern und ist nicht mehr `partial`; die
  Oberflächenhälfte wird der **abgeleitete** Typ `WizardSeite : WizardItemClass` in
  `Views/Wizard/`. Alle Nummernkonstanten bleiben unter dem gewohnten Namen erreichbar; die drei
  `List<WizardItemClass>` werden `List<WizardSeite>`.
* Die `FillComboBox`-Hälften der Controller werden **Erweiterungsmethoden** in
  `Views/GemeinsameBausteine/ControllerListen.cs`. `ctrl.FillComboBox(box)` steht unverändert in
  den Masken. Fünf `*Ctrl.WinForms.cs` (BHKWCtrl, KlimaregionCtrl, WPStammCtrl und die
  `FillListBox`-Hälfte von ProjektCtrl) hatten **0 Aufrufer** und entfallen ersatzlos.

`WPCtrl.WinForms.cs` bleibt unangetastet — `WPCtrl` bleibt in der Anwendung.

**`InternalsVisibleTo`.** Etliche Controller und Modelle sind ohne Zugriffsangabe deklariert und
damit `internal` (`ProjektCtrl`, `KlimaregionCtrl`, `WPStammCtrl`, `Properties.Settings`, `Init`
…). Solange alles in einer Assembly lag, fiel das nicht auf. `EPOS.Kern.csproj` gibt die internen
Typen deshalb für `EPOS_Plan` und `EPOS.Kern.Tests` frei. Die Alternative — jeden betroffenen Typ
auf `public` heben — wäre eine breite Sichtbarkeitsänderung am Bestand ohne fachlichen Grund
gewesen.

**Speicherzweig und Haken.** `SimulationControl.Stromspeicher.cs` zieht mit in den Kern; sein
`[ModuleInitializer]` setzt den K8-Haken jetzt beim Laden von `EPOS.Kern` statt beim Laden der
Anwendung — also weiterhin vor jedem Simulationsaufruf. Das erzeugt die **einzige neue Warnung**
der Etappe (CA2255, 1 ×). Damit ein stillgelegter Haken auffiele, rechnet der CI-Referenzlauf seit
iU4-7 **1030, 1007 und 1017**; die beiden letzteren führen aktive Stromspeicher-Varianten. Alle
drei sind hier byte-gleich zur Basis `2026-08-30_B3-Kaskade`.

Umgekehrt läuft der **Geräte-Aufräumlauf** nach dem Löschen eines Projekts jetzt über den Haken
`WErzeugerCtrl.GeraetewaisenAufraeumen` (`GeraeteWaisen` bleibt in der Anwendung). Vorbelegung
`null` = kein Aufräumlauf; das ist zulässig, weil der Lauf ohnehin NACH dem erfolgreichen DELETE
läuft, sein Ergebnis nicht in den Rückgabewert eingeht und der Migrationsschritt nachholt, was er
liegen lässt.

**Die 1011/1021-Lücke.** Die CI deckt jetzt drei der 13 Referenzprojekte ab. `1011` und `1021`
bleiben ungeprüft — sie stehen in `Referenzlaeufe/2026-08-30_B3-Kaskade/` bereit, sind hier aber
nicht mitgelaufen. Wer den Umfang weiter erhöhen will, tut das über dieselbe Liste in
`kern.yml`; die Laufzeit der drei Projekte liegt bei wenigen Sekunden.

**Zahlen des Nachweises.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64`: **0 Fehler, 123
Warnungen** — dieselbe Summe wie vor der Etappe. Die Verteilung verschiebt sich (vorher Kern 88 /
App 35, jetzt Kern 89 / App 34): Die neue CA2255 kommt hinzu, und CS0108 zu
`WErzeugerModel.ID_Projekt` wird nur noch einmal gemeldet, weil die Datei nur noch einmal
übersetzt wird. **CA1416 bleibt bei 87** — die 77 hinzugekommenen Dateien bringen keine einzige
neue OleDb-Kante mit; das Rest-Inventar für iR8 ist unverändert (`SolarkollektorenCtrl` 41,
`PufferSpCtrl` 30, `RecordSet` 9, `ApplikationCtrl` 7). `dotnet test WP-Plan.Kern.slnf`: **796**
(787 + 9 neue in `EPOS.Kern.Tests`).

### 2.6 Befund iU5 (Statics kappen, Dienste einziehen), 03.09.2026 — **hier erreicht**

Sechs Commits `35be81f`…`9235a92` auf der Basis `18f515f`.

#### Die Entscheidung: statischer Halter, kein DI-Container

Neun Umgebungsdienste liegen hinter kleinen deutschen Schnittstellen und werden von der
statischen Klasse `Dienste` gehalten. Das war eine Wahl gegen den ersten Reflex — einen
`ServiceCollection`-Container —, und zwar aus vier nachprüfbaren Gründen:

1. **Es ist das Hausmuster.** `grep -rn "ServiceCollection\|BuildServiceProvider\|AddSingleton\|IServiceProvider"`
   über das ganze Repo: **0 Treffer**. `Microsoft.Extensions.Http`/`.Logging` stehen nur als
   Mindestversionsforderung von `Mscc.GenerativeAI` in der `.csproj`. Dagegen tragen **acht**
   austauschbare Haken den Bestand: `Meldung.Zeigen/Hinweis/Warnung/Warten`,
   `KiTexte.Lieferant`, `KiEinwilligung.Nachfragen`, `KiAusfuehrer.Uhr/Schreibrecht/ModalerDialog`,
   `AnlagenEindeutigkeit.Frage/Hinweis`, `SimulationControl.Speicherlauf`,
   `SimulationRunner.Speicherergebnismodell`, `WErzeugerCtrl.GeraetewaisenAufraeumen`.
2. **Ein Container verlangt Konstruktoren.** Von den 22 Klassen, die `Program.*` riefen, sind
   etliche **rein statisch**: `AnlagenEindeutigkeit`, `BerichtTexte`, `DokuUebersetzung`,
   `WikiWissen`, `SemantikIndex`, `SemantikModell`, `KiChatService`, `KiEinwilligung`,
   `LizenzManager`, `GeraeteId`, `CsvExportClass`. Ihnen eine Instanz zu reichen hieße, sie zu
   Instanzklassen umzubauen — ein größerer Eingriff als die ganze Etappe, mit entsprechendem
   Risiko für die Byte-Gleichheit.
3. **Der Referenzlauf belegt die Haken heute schon ohne Container** und bleibt so.
4. **Testbarkeit ist gegeben:** Feld setzen, Fall fahren, zurücksetzen — genau wie
   `AnlagenEindeutigkeit.Frage` es vorsieht. `EPOS.Kern.Tests/DiensteTests.cs` fährt das für alle
   neun Dienste.

#### Die neun Schnittstellen und wo sie vom Vorschlag abweichen

| Schnittstelle | Abweichung von Vermessung B.2 | Grund |
|---|---|---|
| `IDialogDienst` | `Frage` hat `warnend` und `vorgabeNein` als Vorgabeparameter | Zwei der vier Rückfragen tragen ein Warnsymbol; der Projekt-Löschdialog setzt den Fokus auf „Nein". Beides ist eine Aussage, kein Beiwerk |
| `IPfade` | `Produktdaten` und `BenutzerLokalBasis` zusätzlich | Der Hilfe-Zwischenspeicher liegt unter `%APPDATA%\<Application.ProductName>` = `EPOS-Plan`, **nicht** unter `%APPDATA%\wp-plan`; der CEC-Modulcache unter `LocalApplicationData\CECModuleImporter` **ohne** Anwendungsordner. Ohne beide Wurzeln wären die Pfade nicht zeichengleich zu halten |
| `IPfade` | `Verbinde` **und** `Unterordner` | Ein Teil der Fundstellen legte den Ordner beim Bilden des Pfades an, der andere nicht. Eine einzige Methode erzeugte leere Ordner, wo bisher keine entstanden |
| `IEinstellungen` | `LiesMaschine` zusätzlich | Der maschinenweite KI-Abschalter steht in `HKLM` und wird in **beiden** Registry-Sichten gelesen (WOW6432Node-Umleitung der x86-Zeit) |
| `ILizenzAblage` | Geltungsbereich als **Parameter**; `Loeschen`, `Vorhanden`, `Ablageort` zusätzlich | Die Vermessung ließ „zwei Instanzen oder Parameter" offen; alle drei Zusätze werden von den zwei Aufrufern gebraucht |
| `IProjektKontext` | `Uebernehmen` liefert `bool`; `Vorhanden` zusätzlich | `MenueCtrl.ProjektAktivSetzen` wertet den Erfolg aus. `Vorhanden` unterscheidet „Oberfläche läuft, kein Projekt offen" von „keine Oberfläche" — nur so meldet `KiAktionenProjekt` weiterhin „keins" statt „das zuletzt geöffnete" |

`IDrucken`/`ITeilen` bleiben außen vor — im Kern gibt es dafür null Fundstellen; sie entstehen
mit iU7.

#### Was nicht verhandelbar war

**Die DPAPI-Geltungsbereiche.** `lizenz.dat` und `lizenz-zeit.dat` liegen im **Gerätebereich**
(`DataProtectionScope.LocalMachine`), `ki-schluessel.dat` im **Benutzerbereich**
(`CurrentUser`). Eine mit dem einen Bereich verschlüsselte Datei lässt sich mit dem anderen nicht
entschlüsseln: Ein versehentlicher Wechsel entwertet jede installierte Lizenz. Der Bereich ist
deshalb ein Aufrufparameter mit Kommentar an beiden Fundstellen, keine Voreinstellung des
Adapters.

**Die Pfade Zeichen für Zeichen.** Drei verschiedene `%APPDATA%`-Wurzeln bleiben getrennt (siehe
Tabelle oben), ebenso `LocalApplicationData\WP-Plan` gegen das nackte `LocalApplicationData`.

**Der Registry-Pfad der Sprache.** `Program.Main` und `MDIMainForm` schrieben seit jeher
`@"Software\\wp-plan"` — mit doppeltem Gegenschrägstrich. Die Registry-Klasse von .NET fasst
mehrfache Trennzeichen zusammen, der Wert liegt also im selben Schlüssel wie alle übrigen.
Verlassen wird sich darauf **nicht**: `WindowsSprache` liest und schreibt mit genau derselben
Zeichenkette, mit der der Wert angelegt wurde, `RegistryEinstellungen` mit der einfachen Form der
übrigen Fundstellen. Sonst stünde nach dem Umbau möglicherweise jeder Anwender wieder auf Deutsch.

#### Ein Verhaltensunterschied, bewusst in Kauf genommen

`StandardSprache.Setzen` belegt zusätzlich `CultureInfo.DefaultThreadCurrentUICulture`. Bisher
galt die eingestellte Sprache nur für den Faden, der `Program.Main` ausführt; ein
Hintergrundfaden beantwortete Textabrufe in der Sprache des Betriebssystems. Im Bestand fiel das
nicht auf, weil die Oberfläche einfädig arbeitet. Die **Rechenkultur** (`CurrentCulture`) wird
ausdrücklich nicht angefasst — Drei-Schichten-Regel, Konzept 13.6.

Zwei weitere Nebenwirkungen sind Verbesserungen: `WinFormsNavigation.OeffneGewerk` prüft
`Program.mainfrm` auf `null` (der Bestand lief dort in eine `NullReferenceException`), und
`LizenzManager.AnkerLesen` legt den Registry-Zweig nicht mehr beim **Lesen** an.

#### Zahlen

`dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental`: **0 Fehler, 123
Warnungen** in jeder der sechs Tranchen — dieselbe Summe wie nach iU4, Verteilung App 34 / Kern
89 unverändert. `dotnet test WP-Plan.Kern.slnf`: **810** (796 + 14 neue in `DiensteTests`).
Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` nach **jeder** Tranche: GESAMT PASS,
815.043 Werte, `diff -rq` nur `protokoll.txt` zusätzlich.

Wächter `Program.*` im Kernsatz: **53 → 0**. Wächter Plattformmuster: **206 → 86**. Davon sind 15 Wortteile (`speicherRegistry`,
`KatalogRegistry`) und 16 Kommentarzeilen; von den 55 verbliebenen Codezeilen entfallen **44** auf
die Bearbeitungsdialoge der zwölf Kontextmenüs, 5 auf `StandardPfade` (die Standardfassung von
`IPfade` selbst), 3 auf `DataRepository` (tabu, iU6), 2 auf `ErststartMigration` (Access-Zweig)
und 1 auf `HelpExtender` (Oberflächenbaustein).

---

### 2.5 Befund iU6 (Datenzugriff plattformfrei), 03.09.2026 — **hier erreicht**

Sechs Commits `22fb7eb`…`2387abf` auf der Basis `18f515f`.

**Das Ergebnis.** `EPOS.Kern` nennt `System.Data.OleDb` nicht mehr — weder im Quelltext noch als
`PackageReference`. **CA1416: 87 → 0.** Der Datenzugriff liegt hinter `IDatenzugriff`;
`DataRepository` bleibt die Fassade davor, und keine der rund 160 Aufruferdateien wurde dafür
angefasst.

| Tranche | Commit | Inhalt | CA1416 |
|---|---|---|---|
| iU6-T1 | `22fb7eb` | `RecordSet.DBCommand` ersatzlos gestrichen (iR8) | 87 → 78 |
| iU6-T2 | `582844c` | toter OleDb-Code in zwei Controllern; Access-Zweig aus `ApplikationCtrl` in die Anwendung | 78 → **0** |
| iU6-T3a | `35de91d` | Masken-Sweep `OleDbParameter` → `DbParam`, 46 Views | 0 |
| iU6-T3b | `fe28cb2` | Brücke aus dem Kern; OleDb-Paket aus `EPOS.Kern.csproj` | 0 |
| iU6-T4 | `7780df6` | `IDatenzugriff` + `SqliteDatenzugriff`; `DataRepository` wird Fassade | 0 |
| iU6-T5 | `2387abf` | `bundle_green` für iOS vorbereitet | 0 |

**iR8 war eine Streichung.** Das Konzept hatte zwei Wege offen gelassen: die Eigenschaft auf einen
providerfreien Typ heben oder `RecordSet` mit seinen Masken in iU9 ablösen. Die Vermessung vom
03.09.2026 macht beide gegenstandslos:

```
git grep -n "\.DBCommand" -- '*.cs'
  EPOS.Kern/Allgemein/DbParam.cs:32   (Kommentar)
  EPOS.Kern/Allgemein/DbParam.cs:45   (Kommentar)
```

**Null Codestellen** — repositoryweit, einschließlich Referenzlauf, Proben und `KiKern`. Niemand
setzte `Connection`, `Transaction`, `CommandText` oder `Parameters`. Seit iU3 entstand das
Kommando nur noch lazy **im Getter** und blieb damit immer `null`: `MerkeSql()` schrieb in ein
Objekt, das es nie gab, und `Parameter()` lieferte ausnahmslos `null`. Die „47 Nutzer" aus dem
Konzept hingen an `Open`, `Next`, `Read` und `Close` — nie am Kommando. Ein Ersatztyp wäre eine
Fassade für null Nutzer gewesen; es gibt deshalb **keinen `DbBefehl`**.

**Derselbe Befund in zwei Controllern — 71 der 87 Warnungen.**

| Methode | Aufrufer | Instanzen der Klasse |
|---|---|---|
| `SolarkollektorenCtrl.Update()` | **0** | `SimulationSolarthermie.cs:230`, `WizardCtrl.cs:1006`, `FormMain.cs:1239`, `Form_SolarKollektoren.cs:233` und `:271` |
| `PufferSpCtrl.Delete(string)` | **0** | `PufferSpKontextMenuCtrl.cs:98` und `:142`, `WizardCtrl.cs:968`, `FormMain.cs:1207`, `Form_Start.cs:1807` |
| `PufferSpCtrl.Update()` | **0** | dieselben fünf |

Alle Instanzen lesen, kopieren oder räumen auf. Geschrieben wird über `SolarkollektorenStammCtrl`
und `PufferSpStammCtrl` — beide OleDb-frei. Zur Laufzeit waren die drei Methoden ohnehin
folgenlos: Das lazy angelegte `DBCommand` bekam nie eine Verbindung, `ExecuteNonQuery()` wäre auf
Windows in die `InvalidOperationException` und damit still in `return false` gelaufen.

**Kein `partial` über Assemblygrenzen.** Der Access-Zweig `GetSchemaVersionOleDb` /
`SetSchemaVersionOleDb` sollte laut Plan als `partial`-Hälfte in der Anwendung landen. Das trägt
nicht: `ApplikationCtrl` liegt seit iU4 in `EPOS.Kern`, und eine `partial`-Hälfte lässt sich über
eine Assemblygrenze hinweg nicht beisteuern. Beide Methoden sind ohnehin `static` und berühren
keinen Instanzzustand; sie stehen jetzt wörtlich in der statischen Anwendungsklasse
`WindowsFormsApplication1/Allgemein/Update/SchemaVersionAccess.cs`
(`[SupportedOSPlatform("windows")]`). Aus `ApplikationCtrl` brauchen sie nur die Namenskonstante
`SPALTE_SCHEMAVERSION`.

**Der Masken-Sweep musste vor die Streichung.** Die Views hängen an genau dem impliziten Operator
und an `DbParam.Von()`, die T3b entfernt — in der geplanten Reihenfolge wäre der Zwischenstand
nicht übersetzbar gewesen. Das Skript hat in 46 Dateien ersetzt:

| Regel | Stellen |
|---|---|
| `OleDbParameter` → `DbParam` (davon 34 voll qualifiziert, 365 `new …(`, 39 Array-, 26 Listendeklarationen) | 434 |
| `DbParam.Von(<ausdruck>)` → `<ausdruck>`, mit Klammerbalance über Zeilengrenzen | 54 |
| `OleDbType` → `DbParamTyp` (im Bestand fünf Werte: Boolean 7, Date 6, Double 6, Integer 15, VarWChar 2; drei weitere Treffer in Kommentaren) | 39 |
| Objektinitialisierer `{ Value = }` → `{ Wert = }` | 36 |
| `using System.Data.OleDb;` entfernt | 38 |

Die 36 Objektinitialisierer waren der einzige Fall, den die Vermessung nicht gelistet hatte —
`DbParam` heißt die Eigenschaft `Wert`, nicht `Value`. Verhaltensgleich: Der Setter macht aus
`null` ein `DBNull.Value`, und genau das tat `DataRepository.NormalisiereWert` mit einem
`null`-Value bisher auch. Die dokumentierte Überladungsfalle `new DbParam("x", 0)` wurde vor dem
Lauf erneut geprüft — **0 Stellen**. Von Hand nachgearbeitet wurde nichts.

**Was von OleDb übrig ist.** In der Anwendung: `DbParamOleDb` (die verschobene Brücke),
`SchemaVersionAccess`, `SchemaMigration`, `GeraeteWaisen`, `ErststartMigration` — der eingefrorene
Access-Zweig, der einen `.accdb`-Bestand vor der Erstmigration auf Zielstand 61 hebt. Dazu drei
`catch (OleDbException)` aus der Access-Zeit (`KiAktionenProjekt`, `KiAktionenSchreiben`,
`PeakShavingCtrl`) — kein Parameterweg, deshalb nicht Teil von iU6. `DbParamOleDb.Aus()` und
`.Von()` haben nach dem Sweep **keinen** Nutzer mehr und bleiben nur als Rückfalltür stehen;
getragen wird allein `.Nach()` mit vier Aufrufstellen.

**`EPOS.Daten` entsteht nicht.** Der Vertrag ist ein Interface und eine Klasse. Ein eigenes Projekt
hätte den Kern von seiner Zugriffsschicht getrennt, ohne dass ein zweiter Anbieter in Sicht wäre —
das Umsetzungskonzept hält in § 1.5 ausdrücklich fest, dass es **einen** Dialekt gibt.
`IDatenzugriff.cs` und `SqliteDatenzugriff.cs` liegen in `EPOS.Kern/Allgemein/`.

**Zahlen des Nachweises.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64`: **0 Fehler, 36
Warnungen** (vorher 123 — die 87 CA1416 sind weg). `EPOS.Kern` allein: **0 Fehler, 2 Warnungen**
(CA2255 zum `ModuleInitializer`, CS0108 zu `WErzeugerModel.ID_Projekt` — beide aus dem Bestand).
`dotnet test WP-Plan.Kern.slnf`: **805** (796 + 9 neue). Referenzlauf 1030/1007/1017 gegen
`2026-08-30_B3-Kaskade`: **GESAMT PASS (815.043 Werte), byte-gleich nach jeder Tranche**.
`dotnet list EPOS.Kern package | grep -c OleDb`: **0**.

---

## 3 Chart- und Grid-Masken — Auszählung

**Anlass.** Das Umsetzungskonzept § 1.5 führt die Konzeptzahl **16/16** als *nicht reproduzierbar*
und stellt ihr **9 bzw. 7** Designer-Instanzen sowie **18 bzw. 36** Dateien mit Typnutzung gegenüber.
iU0 verlangt die Einzeldurchsicht, weil diese Zahl der Aufwandstreiber für **iU9 K2 (Charts)** und
**K3 (Tabellen)** ist. Die folgende Auszählung ist am 02.09.2026 gegen den Arbeitsbaum
(`ios_migration` @ `e0df744`) erhoben; Bezugsraum ist ausschließlich `WindowsFormsApplication1/Views/`.

### 3.1 Verwendete Befehle

```bash
# (a) Dateien mit Chart-Typnutzung
grep -rl --include="*.cs" -E "System\.Windows\.Forms\.DataVisualization|Charting\.Chart" \
     WindowsFormsApplication1/Views/

# (b) Designer-Instanzen Chart — beide Schreibweisen der Designer-Dateien
grep -rn --include="*.Designer.cs" --include="*.designer.cs" \
     -c "new System\.Windows\.Forms\.DataVisualization\.Charting\.Chart()" \
     WindowsFormsApplication1/Views/

# (c) Dateien mit DataGridView
grep -rl --include="*.cs" -E "DataGridView" WindowsFormsApplication1/Views/

# (d) Designer-Instanzen DataGridView
grep -rn --include="*.Designer.cs" --include="*.designer.cs" \
     -c "new System\.Windows\.Forms\.DataGridView()" WindowsFormsApplication1/Views/

# programmatisch erzeugte Steuerelemente — auch in Objektinitialisierer-Schreibweise
# ("new DataGridView" ohne Klammern, Werte folgen im Block danach)
grep -rn --include="*.cs" \
     -E "new +(System\.Windows\.Forms\.DataVisualization\.Charting\.)?Chart *($|\(|\{)" \
     WindowsFormsApplication1/Views/

grep -rn --include="*.cs" -E "new +(System\.Windows\.Forms\.)?DataGridView *($|\(|\{)" \
     WindowsFormsApplication1/Views/ | grep -v -i "\.designer\.cs"
```

**Drei Fallen, die die alten Zahlen erklären.**

1. **Groß-/Kleinschreibung der Designer-Dateien.** Das Repo führt beide Schreibweisen —
   `*.Designer.cs` **und** `*.designer.cs`. Ein Muster nur auf `*.Designer.cs` übersieht bei den
   Charts **7**, bei den Grids **8** Dateien. Genau das ergibt die Zahlen **9** und **7** aus § 1.5:
   16 − 7 = 9 und 15 − 8 = 7. Die dortigen Werte sind damit als Artefakt erklärt, nicht als Befund.
2. **Asymmetrische Suchmuster.** Die „18 Chart- und 36 Grid-Dateien" entstehen aus zwei verschiedenen
   Mustern: `Charting.Chart` trifft **18** Dateien, `System.Windows.Forms.DataVisualization` dagegen
   **37**. Für die Grids wurde mit `DataGridView` breit gesucht (**36**). Verglichen wurde Ungleiches.
3. **Objektinitialisierer.** `new DataGridView` und `new Chart` ohne Klammern (Werte folgen in
   `{ … }`) entgehen jedem Muster, das `(` verlangt — betrifft **fünf** Masken.

### 3.2 Chart-Masken

**37** `.cs`-Dateien unter `Views/` nennen einen Chart-Typ (48 einschließlich `.resx`). Nach
Zusammenfassung von `*.cs` und `*.Designer.cs` je Maske bleiben **21 Masken**:

| Maske (unter `WindowsFormsApplication1/Views/`) | Designer-Instanz | programmatisch erzeugt | nur Typnennung |
|---|---|---|---|
| `Brauchwasser/Form_EingBrauchwasserTyp` | ja (1) | nein | — |
| `Brauchwasser/Form_ErgBrauchwasserwaerme` | ja (1) | nein | — |
| `Gebäude/Form_EingGebTyp` | ja (1) | nein | — |
| `Klimadaten/Form_Klimadaten` | **ja (2)** | nein | — |
| `Kosten/Form_Kostenprofil` | ja (1) | nein | — |
| `Prozesswärme/Form_EingProzTyp` | ja (1) | nein | — |
| `Prozesswärme/Form_ErgProzesswaerme` | ja (1) | nein | — |
| `Simulation/DashboardForm` | ja (1) | nein | — |
| `Simulation/Form_QuelleErdreich` | nein | **ja (1)** — `new Chart { … }`, Z. 613 | — |
| `Simulation/Form_Quellprofil` | nein | **ja (1)** — Z. 655 | — |
| `Simulation/Form_Simulation_Detail` | **ja (9)** | **ja (3)** — Z. 609, 2471, 6736 | — |
| `Simulation/Form_Simulation_Kurz` | ja (1) | nein | — · **nicht kompiliert** (`Compile Remove` in der `.csproj`) |
| `Simulation/Form_Simulation_Detail - Kopie` | nein | nein | **ja** · **nicht kompiliert** |
| `Simulation/GanglinienDarstellung` | nein | nein | **ja** — statische Helferklasse (96 Z.), arbeitet auf fremden `Series` |
| `Simulation/NavigatorStrom` | ja (1) | nein | — |
| `Simulation/NavigatorWaerme` | ja (1) | nein | — |
| `Stromspeicher/Form_PeakShaving` | nein | **ja (1)** — `new Chart()`, Z. 179 | — |
| `Stromverbraucher/Form_EingStromTyp` | ja (1) | nein | — |
| `Stromverbraucher/Form_ErgStromverbraucher` | ja (1) | nein | — |
| `Wizard/Wizard_WPItem` | **ja (2)** | nein | — |
| `Wärmepumpe/Form_WP` | **ja (2)** | nein | — |

**Zwischensummen Chart:** 16 Dateien mit Designer-Instanzen (**27** Steuerelemente) · 4 Dateien mit
programmatischer Erzeugung (**6** Steuerelemente) · 2 Masken mit reiner Typnennung.

### 3.3 Grid-Masken

**36** `.cs`-Dateien unter `Views/` nennen `DataGridView` — zusammengefasst **21 Masken**:

| Maske (unter `WindowsFormsApplication1/Views/`) | Designer-Instanz | programmatisch erzeugt | nur Typnennung |
|---|---|---|---|
| `BHKW/Form_BHKWAdmin` | ja (1) | nein | — |
| `BHKW/Form_BHKWEing` | ja (1) | nein | — |
| `BerichteKosten/UcBerichteKosten` | nein | nein | **ja** — Typprüfung `c is DataGridView`, Z. 545 |
| `BerichteKosten/UcBkKosten` | nein | **ja (2)** — Z. 116, 118 | — |
| `BerichteKosten/UcBkUebersicht` | nein | **ja (1)** — Z. 152 | — |
| `Brauchwasser/Form_Brauchwasser` | ja (1) | nein | — |
| `Gebäude/Form_Gebaeude` | ja (1) | nein | — |
| `Import/Form_ImportKonflikte` | nein | **ja (1)** — `new DataGridView { … }`, Z. 100 | — |
| `Kosten/Form_Emissionskatalog` | **ja (2)** | nein | — |
| `Kosten/ucFuelSettings` | ja (1) | **ja (1)** — Z. 604 | — |
| `Photovoltaik/Form_CECImport` | ja (1) | nein | — |
| `Prozesswärme/Form_Prozesswaerme` | ja (1) | nein | — |
| `Simulation/Form_Quellprofil` | nein | **ja (1)** — Z. 597 | — |
| `Simulation/Form_Simulation_Detail` | nein | nein | **ja** — Typprüfung `ctrl is DataGridView dgv`, Z. 5274 |
| `Simulation/NavigatorUebersicht` | ja (1) | nein | — |
| `Solarthermie/Form_SolarKollektoren` | ja (1) | nein | — |
| `Solarthermie/Form_SolarKollektorenAdmin` | ja (1) | nein | — |
| `Stromspeicher/Form_Stromspeicher` | ja (1) | nein | — |
| `Wirtschaftlichkeit/UcWirtschaftlichkeit` | ja (1) | nein | — |
| `Wärmepumpe/Form_WPFilterAuswahl` | ja (1) | nein | — |
| `Wärmepumpe/Kenndaten` | ja (1) | nein | — |

**Zwischensummen Grid:** 15 Dateien mit Designer-Instanzen (**16** Steuerelemente) · 5 Dateien mit
programmatischer Erzeugung (**6** Steuerelemente) · 2 Masken mit reiner Typnennung.

### 3.4 Die belastbaren Zahlen für iU9 K2/K3

„Anzeigen" heißt: Die Maske erzeugt ein eigenes Steuerelement — im Designer oder im Code. Reine
Typnennungen (`using`-Zeile, `is`-Prüfung in einer Schleife, Helferklasse) zählen nicht; sie kosten
in iU9 nichts.

| Größe | Chart | Grid |
|---|---|---|
| Dateien mit Typnutzung (`.cs`) | 37 | 36 |
| Masken mit Typnutzung | 21 | 21 |
| davon **nur Typnennung** | 2 | 2 |
| **Masken, die ein Steuerelement tatsächlich anzeigen** | **19** | **19** |
| davon nicht kompiliert (`Compile Remove`) | 1 (`Form_Simulation_Kurz`) | 0 |
| **im Build wirksam — Aufwandsgröße für iU9** | **18** | **19** |
| Steuerelemente insgesamt (Designer + programmatisch) | 33 | 22 |
| Steuerelemente im Build | **32** | **22** |

**Verschiedene Masken insgesamt: 36.** `Simulation/Form_Quellprofil` führt als einzige Maske beides
(Chart und Grid, beide programmatisch); 18 + 19 − 1 = 36.

**Vier Befunde für die Planung.**

- **Die Konzeptzahl 16/16 war für die Charts nahezu richtig** — sie traf die 16 Dateien mit
  Designer-Instanzen. Sie unterschlug die vier Masken, die ihre Charts im Code aufbauen.
- **`Form_Simulation_Detail` ist der Ausreißer:** 12 der 32 Charts im Build (9 im Designer, 3 im
  Code) stehen in dieser einen Maske. Das bestätigt iR10 — sie ist in iU9 nicht zu konvertieren,
  sondern zu zerlegen.
- **`Form_Simulation_Kurz` ist tot.** `.cs`, `.Designer.cs` und alle drei `.resx` sind per
  `Compile Remove` aus dem Build genommen; der Inhalt ist in `Form_Simulation_Detail` aufgegangen
  (Kommentare dort verweisen darauf). `Allgemein/KI/HilfeKontext.cs` führt den Namen noch als
  Hilfe-Kontext — Karteileiche, in iU0 oder iU9 mit zu räumen. Ebenso ausgeschlossen:
  `Form_Simulation_Detail - Kopie.cs` und `Allgemein/GrafikTools/ChartManagerNeu.cs`.
- **Fünf Masken bauen ihr Steuerelement im Objektinitialisierer auf** (`Form_QuelleErdreich`,
  `Form_Quellprofil`, `Form_ImportKonflikte`, `ucFuelSettings`, `UcBkUebersicht`). Der
  Formular-Generator aus iF7 findet diese Felder **nicht** in den `Designer.cs` — für sie bleibt die
  Feldkartenabnahme (iT6) Handarbeit.

---

## 4 Offene Punkte der x64-Umstellung

P0–P5 aus [`Konzept_Umstellung_64Bit_EPOS-Plan.md`](Konzept_Umstellung_64Bit_EPOS-Plan.md) sind
umgesetzt; was in § 10 dort offen bleibt, lässt sich nicht automatisieren. iU0 verlangt dafür
Termine — die Inhalte stehen fest, die beiden rechten Spalten füllt der Anwender.

| Punkt | Inhalt | Termin | Verantwortlich |
|---|---|---|---|
| **(a)** | **Funktionsdurchlauf an der Oberfläche** (Prüfliste 5–8): sämtliche Importe — VDI 3805 für Kessel, Puffer, Kollektoren und Wärmepumpe, CEC/PAN, CSV, Klimadaten-Excel, Ganglinien-Excel (die beiden Excel-Wege laufen über Out-of-Process-COM und sind der eigentliche Grund, hier genau hinzusehen) · Bericht als Word **und** Excel · ScottPlot-Dialog `Form_SpeicherOptimierung` als einziger Skia-Konsument · Lizenzaktivierung aus der x86-Ära ohne Re-Aktivierung · KI-Abschalter `KiDeaktiviert` **je einmal** unter `HKLM\SOFTWARE\wp-plan` und unter `HKLM\SOFTWARE\WOW6432Node\wp-plan`, beide Sichten müssen wirken (P1.1) · Prüfpunkt 12 (Cue-Banner in den Suchfeldern) | | |
| **(b)** | **Start ohne ACE** (Prüfliste 3): auf einem Rechner **ohne** Access Database Engine starten; die sprechende Meldung aus P1.3 muss erscheinen statt einer nackten `OleDbException` — in **beiden** Sprachen. **Der Sinn hat sich mit SQLite verschoben:** Der Normalbetrieb braucht ACE nicht mehr, die Meldung ist kein Startblocker mehr. Zu prüfen ist jetzt der **Erststart-Migrationspfad für Alt-Bestände** — dass die Anwendung ohne ACE sauber startet und **beim Antreffen einer `.accdb`** verständlich meldet, dass die Übernahme eine Access-Engine verlangt | | |
| **(c)** | **Setup-Testmatrix** (Prüfliste 9–10): drei Umgebungen — sauberes Win11 ohne Office · Rechner mit **64-bit-Office** und Access · Rechner mit **32-bit-Office** (Hinweisdialog vor dem stillen Lauf, danach Erfolg oder sprechende Meldung, **kein** stiller Fehlschlag). Dazu das Update über eine bestehende **32-bit-Installation**: danach genau **ein** Eintrag in „Apps und Features"; die Rückfrage des alten Deinstallierers („Projektdatenbank löschen?") erscheint sichtbar — hier **„Nein"** wählen, die Voreinstellung stimmt bereits. Prüfpunkt 11 (zweites Windows-Konto bei geöffneter DB) hängt mit dran, ist aber bitness-neutral. **Der Redist-Teil der Matrix entfällt mit (d)** | | |
| ~~**(d)**~~ | ~~Redist beschaffen — und vorher die `.gitignore` nachziehen~~ — **entfallen: seit SQLite ist kein ACE-Redist mehr nötig; ACE wird nur noch von der Erststart-Migration für Alt-Bestände gebraucht.** Damit entfallen auch die beiden dort beschriebenen Fallen (ungeschützte Repo-Wurzel, Entfernen der alten 32-bit-Fassung aus der Versionierung) als Vorbedingung für einen Setup-Build | — | — |
| **(e)** | **Marke auf GitHub übertragen.** `GitHub_Sync.bat` schiebt nur den Zweig `main` und überträgt **keine Tags**. Der Rückweg-Tag `letzter-x86-stand` (`3f126f4`) ist deshalb einmal ausdrücklich mit `push --tags` zu übertragen; ohne diesen Schritt bleibt der einzige Rückweg auf den letzten x86-Stand an einen Rechner gebunden (iR9). **Am 02.09.2026 geprüft: eine Tag-Abfrage gegen `origin` liefert keinen einzigen Eintrag.** Bei der Gelegenheit den Referenzbasis-Tag aus § 2 mitsetzen | | |

**Reihenfolge.** (e) ist in Minuten erledigt und beseitigt ein Ausfallrisiko — er gehört vor alles
andere. (a) und (c) verlangen fremde Rechner und Zeit; sie sind die eigentliche Terminfrage. (b) ist
in seiner neuen Fassung Teil des Migrationspfads und sinnvollerweise mit (a) zusammen zu prüfen.
