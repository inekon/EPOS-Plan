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
| **iF10** | `IDatenzugriff` mit providerneutralem `DbParam` (Weg b) — oder ~2.300 `OleDbParameter`-Aufrufe maschinell ersetzen (Weg a)? | **Weg (b)**; Weg (a) bleibt spätere Aufräumoption | iU6 | offen | | |
| **iF11** | Mac-Hardware sofort beschaffen — oder Spike auf `macos-latest`-CI-Runner? | **CI-Runner** für den Spike, Mac erst mit iU10 | iU3 | offen | | |
| **iF12** | Vertriebsweg der Auslieferung (Custom Apps / Unlisted / App Store) und Behandlung des Lizenzverkaufs gegenüber Apples Kaufregeln | **Custom Apps** über Apple Business Manager prüfen; Klärung **vor** iU13, nicht im Review | vor iU13 | offen | | |
| **iF13** | Root-Namespace `WindowsFormsApplication1` beim Kern-Umzug mit umbenennen? | **nein** — eigener mechanischer Schritt danach | iU4 | offen | | |
| **iF14** | Anonymisierte `Kenndaten_Test.sqlite` mit den 13 Referenzprojekten versionieren? | **ja** — sonst ist die Kern-CI nur ein Kompilierungstest (iR6) | iU3 (Baustein iE6) | offen | | |
| **iF15** | Wie ist „wertgleich" zwischen x64 und ARM64 definiert? | bestehende Toleranz (rel. 1e-4 / abs. 0,01) für den Plattformvergleich; **Byte-Gleichheit** bleibt Maßstab für Windows-interne Umbauten | vor iU3 | offen | | |
| **iF16** | Chart-Weg in Blazor Hybrid: ScottPlot als Bild, JS-Bibliothek oder natives Steuerelement? | **ScottPlot als Bild** — ein Stack für Bericht und Bildschirm | iU7 | offen | | |
| **iF17** | iU1 (Fundament, .NET 10, CI, COM-Entfernung) **unabhängig vom iOS-Beschluss** beauftragen? | **ja** — Support-Frist 10.11.2026, einzige Antwort auf iR9 | iU1 | **beschieden** | **ja — iU1 läuft seit 02.09.2026 auf Branch `ios_migration`** | 02.09.2026 |
| **iF18** | Welche VS-2026-Edition? (VS 2022 kann `net10.0` nicht targeten) | **Community 2026**, sofern INEKON unter den Enterprise-Schwellen bleibt; sonst Professional | vor iU1 | **beschieden** | **Community 2026 — installiert unter `C:\Program Files\Microsoft Visual Studio\18\Community`** | 02.09.2026 |

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
