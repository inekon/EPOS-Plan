# Entscheidungsregister: EPOS-Plan auf iOS

**Stand 03.09.2026 — nach der Kette iU4…iU10** (Ausgangspunkt: Arbeitsliste zu iU0 —
Klärung, Sicherung, Rückbau)

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

## 1 Entscheidungsregister iF1–iF28

iF1–iF9 stammen aus [`Konzept_iOS-Portierung_EPOS-Plan.md`](Konzept_iOS-Portierung_EPOS-Plan.md)
§ 7, iF10–iF18 aus dem Umsetzungskonzept § 8.2; iF19–iF23 sind in der Umsetzung entstanden, **iF24–iF28 mit der iOS-Hülle (iU10)**. Die Spalte **benötigt ab** nennt das Paket, das ohne
die Entscheidung nicht begonnen werden kann.

| Nr. | Frage (Kurzform) | Empfehlung laut Konzept | benötigt ab | Status | Entscheid (Anwender) | Datum |
|---|---|---|---|---|---|---|
| **iF1** | S0-Spike (Kernrechnung im Simulator, Projekt 1030) beauftragen? | ja | iU3 | offen | | |
| **iF2** | Voller Funktionsumfang — oder erste Auslieferung ohne Katalog-Admin? | ohne Katalog-Admin | iU11 | offen | | |
| **iF3** | UI-Technologie: Blazor Hybrid oder MAUI-XAML? | Blazor Hybrid | iU8 | **umgesetzt, vorläufig** — durch den Auftrag „fahre fort bis iU8" vom 03.09.2026 gedeckt; förmlicher Entscheid des Anwenders offen — `EPOS.UI` ist eine Razor-Bibliothek, die Hülle `BlazorDialogForm` ein `BlazorWebView` (iU8-2/iU8-6, § 2.8) | | |
| **iF4** | Kern-Herauslösung unabhängig vom iOS-Ziel einplanen? | ja | iU1 | offen | | |
| **iF5** | Vertriebsweg im Grundsatz | zunächst TestFlight — im Umsetzungskonzept § 3.4 präzisiert: TestFlight ist **kein** Auslieferungsweg (90 Tage) | iU13 | offen — geht sachlich in iF12 auf | | |
| **iF6** | Windows-Charts ebenfalls auf ScottPlot? | mittelfristig vereinheitlichen, **nicht** Teil dieses Vorhabens | iU7 | offen | | |
| **iF7** | Formular-Generator (Feldinventar aus den 118 `Designer.cs`) als Werkzeug? | ja | iU8 | **umgesetzt, vorläufig** — durch den Auftrag „fahre fort bis iU8" vom 03.09.2026 gedeckt; förmlicher Entscheid des Anwenders offen — `Werkzeuge/Formularkarte` (iU8-12a…e), Stapellauf über **123** Designer-Dateien / **120** Masken vor dem Stichtag, **122 / 119** danach (§ 2.8) | | |
| **iF8** | **Modell C beschließen** (Strangler-Regel M1) | ja | **iU8 — ohne diesen Beschluss ist iU8 gegenstandslos** | **umgesetzt, vorläufig** — durch den Auftrag „fahre fort bis iU8" vom 03.09.2026 gedeckt; förmlicher Entscheid des Anwenders offen — Stichtag iZ5 mit `92380ea`: `Form_Kosten_Auswahl` gelöscht, der Dialog lebt als Komponente | | |
| ~~**iF9**~~ | ~~SQLite auch auf Windows, mit Stichtag~~ | ja | — | **beschieden und ausgeführt 02.09.2026 (`6486c36`)** | ja | 02.09.2026 |
| **iF10** | `IDatenzugriff` mit providerneutralem `DbParam` (Weg b) — oder ~2.300 `OleDbParameter`-Aufrufe maschinell ersetzen (Weg a)? | **Weg (b)**; Weg (a) bleibt spätere Aufräumoption | iU6 | **Weg (b) ausgeführt 03.09.2026** (`22fb7eb`…`2387abf`, § 2.5); Weg (a) hat sich mit dem Masken-Sweep iU6-T3a miterledigt. Entscheid des Anwenders steht noch aus | | |
| **iF11** | Mac-Hardware sofort beschaffen — oder Spike auf `macos-latest`-CI-Runner? | **CI-Runner** für den Spike, Mac erst mit iU10 | iU3 | offen | | |
| **iF12** | Vertriebsweg der Auslieferung (Custom Apps / Unlisted / App Store) und Behandlung des Lizenzverkaufs gegenüber Apples Kaufregeln | **Custom Apps** über Apple Business Manager prüfen; Klärung **vor** iU13, nicht im Review | vor iU13 | offen | | |
| **iF13** | Root-Namespace `WindowsFormsApplication1` beim Kern-Umzug mit umbenennen? | **nein** — eigener mechanischer Schritt danach | iU4 | offen | | |
| **iF14** | `Kenndaten_Test.sqlite` mit den 13 Referenzprojekten versionieren? | **ja** — sonst ist die Kern-CI nur ein Kompilierungstest (iR6). Befund 02.09.: siehe § 2.1 | iU3 (Baustein iE6) | **beschieden** | **ja — Anwender bestätigt 02.09.2026: die Datenbank enthält nirgends Kundendaten.** Anonymisierung entfällt; Reduzierung auf die 13 Projekte nur wegen der Dateigröße (GitHub-Grenze 100 MB) | 02.09.2026 |
| **iF15** | Wie ist „wertgleich" zwischen x64 und ARM64 definiert? | bestehende Toleranz (rel. 1e-4 / abs. 0,01) für den Plattformvergleich; **Byte-Gleichheit** bleibt Maßstab für Windows-interne Umbauten | vor iU3 | **beschieden** | Toleranz wie heute (rel. 1e-4 / abs. 0,01) für den Plattformvergleich; Befund 02.09.: 1030 auf x64-Linux **und** arm64-macOS byte-gleich — die Toleranz wird bisher nicht einmal gebraucht | 02.09.2026 |
| **iF16** | Chart-Weg in Blazor Hybrid: ScottPlot als Bild, JS-Bibliothek oder natives Steuerelement? | **ScottPlot als Bild** — ein Stack für Bericht und Bildschirm | iU7 | **umgesetzt, vorläufig** — durch den Auftrag „fahre fort bis iU8" vom 03.09.2026 gedeckt; förmlicher Entscheid des Anwenders offen — **Bild aus dem Kern-Renderer**: `ChartRenderer` (SkiaSharp) liefert PNG-Bytes, `EPOS.UI/Standards/ChartBild` zeigt sie an. Durch **iF22** präzisiert | | |
| **iF17** | iU1 (Fundament, .NET 10, CI, COM-Entfernung) **unabhängig vom iOS-Beschluss** beauftragen? | **ja** — Support-Frist 10.11.2026, einzige Antwort auf iR9 | iU1 | **beschieden** | **ja — iU1 läuft seit 02.09.2026 auf Branch `ios_migration`** | 02.09.2026 |
| **iF18** | Welche VS-2026-Edition? (VS 2022 kann `net10.0` nicht targeten) | **Community 2026**, sofern INEKON unter den Enterprise-Schwellen bleibt; sonst Professional | vor iU1 | **beschieden** | **Community 2026 — installiert unter `C:\Program Files\Microsoft Visual Studio\18\Community`** | 02.09.2026 |
| **iF19** | Schrift der Berichts-Charts nach der SkiaSharp-Portierung (iU7): mitgelieferte Schrift (plattformgleiche Bilder) oder Systemschrift (plattformpassend)? | Konzept offen; Vermessung iU7: `"Calibri"` steht 15× hart im `ChartRenderer`, Legendenumbruch hängt an Textmaßen | iU7 | **beschieden** | **Systemschrift, flexibel** — Rückfallkette Calibri (Windows) → Systemschrift (macOS/iOS) → Sans (Linux) über `SKFontManager`; Layout bleibt metrikgetrieben, Textbreiten dürfen je Plattform abweichen. Folge: Bildvergleich Windows↔Linux ist Struktur-/Histogrammvergleich, kein Pixelvergleich | 02.09.2026 |
| **iF20** | **Verteilung der WebView2-Laufzeit.** Online-Bootstrapper (~2 MB, braucht beim Setup Internet), Standalone-Installer (~150 MB, offline) oder Fixed-Version-Verteilung (Laufzeit im Programmordner, Aktualisierung liegt dann bei uns)? | Bootstrapper, solange kein Kunde ohne Internet installiert; sonst den Standalone-Installer beilegen | vor der Auslieferung von iU8 | **offen — Anwenderentscheid.** Umgesetzt ist der **Bootstrapper** (iU8-10, `eafbc1f`): das Setup nimmt ihn nur mit, wenn die Laufzeit fehlt, und läuft weiter, wenn er scheitert. Offen als **S10** in `Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md` § 5.5 | | |
| **iF21** | **DPI.** Die Anwendung ist bewusst `DpiUnaware`; die WebView2 wäre darin bei 125–200 % bitmapskaliert und sichtbar unscharf. Bleibt die Hülle eine **DPI-Insel** (`PER_MONITOR_AWARE_V2` nur für die Dauer des modalen Laufs) — oder wird die Anwendung als Ganzes DPI-fähig? | Insel; die Umstellung der 120 Masken ist ein eigenes Vorhaben | iU8, spätestens iU9 | **hier gebaut** (`DpiInsel` in `Allgemein/Blazor/BlazorDialogForm.cs`, iU8-6). **Windows-Befund offen:** 125 % und 150 % sind noch nicht am Gerät gesehen worden; auf Windows vor 10/1803 greift die Insel nicht. Prüfpunkte in `Umsetzung_iU8_Nachweise.md` | | |
| **iF22** | **Präzisierung zu iF16 — wie viele Chart-Stacks trägt das Haus?** | **eine Bibliothek (SkiaSharp), zwei Nutzungsarten** | iU7/iU8 | **Bericht und Blazor** bekommen ein **Bild aus dem Kern-Renderer** (`EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs`, seit iU7-2 SkiaSharp statt GDI+); die **interaktiven Bildschirmmasken** bleiben bei **ScottPlot** — und das ist heute genau **eine** Maske, `Form_SpeicherOptimierung`. ScottPlot 5 rendert selbst über SkiaSharp: es bleibt bei **einer** Grafikbibliothek, nur bei zwei Nutzungsarten. iF16 ist damit präzisiert, nicht ersetzt | | |
| **iF23** | **Was geschieht mit `ChartRendererGdi.cs`?** Der wortgleiche GDI+-Stand aus iU7-1 ist der Gegenpart des Windows-Bildvergleichs — eine zweite, nicht gepflegte Fassung desselben Renderers | **ersatzlos löschen**, sobald der Bildvergleich abgenommen ist | nach der Windows-Abnahme von iU7 | **wartet auf den Anwender.** Er führt `Referenzlauf.exe bildvergleich --quelle <sqlite> --projekte 1030,1007,1017 --ziel <ordner>` aus; steht in `bildvergleich.md` PASS, wird die Datei gelöscht — sie ist bis dahin die einzige verbliebene GDI+-Stelle der Berichtskette | | |
| **iF24** | **Apple-Developer-Konto** (99 €/Jahr) jetzt beschaffen — oder erst nach einem grünen Simulator-Lauf? | erst **nach** iU10-6; bis dahin kostet der Nachweis nur CI-Minuten. Der Simulator-Job braucht weder Konto noch Zertifikat. Gerätebau und TestFlight sind iU13 | vor iU13 | offen | | |
| **iF25** | **Seed-Datenbank im App-Bundle.** E1 = Testdatenbank (77 MB, nur CI), E2 = Produktivstand (148 MB, ~250–280 MB installiert), E3 = Massendaten beim Erststart nachladen, E4 = vollständiger Download beim Erststart | **E1 für iU10** (per `-p:SeedDb` gesetzt), **E2 für TestFlight** — vorher `VACUUM` der Produktiv-DB messen. E3/E4 nur bei Store-Vorgaben; On-Demand-Resources sind mit .NET für iOS nicht baubar | vor iU13 | offen | | |
| **iF26** | **KI-Semantiksuche auf iOS**: die 46-MB-`onnxruntime.xcframework` statisch mitlinken — oder die Semantiksuche auf iOS abschalten? | in iU10/iU11 **abschalten** — `ExcludeAssets="native;build;buildTransitive"` in `EPOS.iOS.csproj`. `SemantikModell` scheitert damit erst beim Aufruf, nicht beim Start; Chat und Wiki laufen über REST und sind unberührt. Entscheid vor iU13 | vor iU13 | **umgesetzt, vorläufig** (iU10-3) | | |
| **iF27** | **`bundle_green` ist tot** — fährt iOS dieselbe SQLite 3.53.3 wie Windows (`bundle_e_sqlite3`, statisch gelinkt)? | **ja.** Die Fassung 2.1.12 von `bundle_green` existiert nicht (letzte 2.1.11, in 3.0 entfallen), und die System-SQLite des Geräts wäre für die **114 STRICT-Tabellen** nicht steuerbar. Auf iOS liefert `bundle_e_sqlite3` `provider.internal` und eine statisch gelinkte `e_sqlite3.a` — dieselbe Fassung auf allen vier Läufern | iU10 | **umgesetzt** (iU10-1), Bestätigung des Anwenders offen | | |
| **iF28** | **Mindest-iPadOS 17.0** akzeptabel? | **ja** — von `SkiaSharp.NativeAssets.iOS` 3.119 (`net8.0-ios17.0`) erzwungen; MAUI 10 selbst käme mit 15.0 aus. iPadOS 17 läuft auf iPads ab 2018 | iU10 | **umgesetzt, vorläufig** (iU10-3) | | |

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


**Zu den Commit-Angaben in §§ 2.4–2.8.** „Auf der Basis X" nennt die **Entwicklungsbasis** —
iU5 bis iU8 sind in eigenen Worktrees entstanden und per Cherry-Pick nach `ios_migration`
übernommen worden; die SHAs sind dabei neu vergeben worden und hier berichtigt. Auf dem Zweig
stehen die Pakete deshalb in einer anderen Reihenfolge als in der Planung: nach `18f515f`
(iU4-8) folgen iU8-1…5/8a/5b, dann iU7-1…4, dann iU6, dann iU8-5c und iU8-12, dann iU7-5…8,
dann iU5-T0…T5, dann iU8-6…13, zuletzt iU5-U1…U5 mit iU7-9. Die Reihenfolge ist für die
Nachweise ohne Belang — jede Tranche ist für sich gebaut, getestet und gegen die Referenzbasis
gefahren worden —, aber sie erklärt, warum die genannten Basis-SHAs nicht die Elternteile der
Commits auf dem Zweig sind.

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

### 2.7 Befund iU7 (Charts und Berichte plattformfrei), 03.09.2026 — **hier erreicht**

Neun Commits in zwei Wellen: `c6b32eb`…`f84932b` (iU7-1…iU7-4, vor iU6 eingereiht) und
`6604c05`…`0759b37` (iU7-5…iU7-8) sowie `0af6421` (iU7-9, im zweiten Umzug nachgezogen).

**Der Renderer ist 8 Bilderzeuger für 9 Bilder.** `ChartRenderer` hat acht öffentliche
`byte[]`-Methoden — `Kuchen`, `BalkenHorizontal`, `JahresverlaufWaerme`, `DauerlinieWaerme`,
`StrombilanzMonate`, `Speicherverlauf`, `Speichertemperaturen`, `KapitalwertVerlauf`. Die
letzte liefert zwei Bilder (`kapitalwert_differenz` und `kapitalwert_absolut`); daher neun
Bilder in `Proben/ChartProben`. Das ist keine Wortklauberei: Die Probe zählt Bilder, die
Testliste zählt Methoden, und ohne diesen Satz laufen die beiden Zahlen als Widerspruch durch
die Dokumente.

**Keine einzige Aufruferanpassung.** Die Portierung GDI+ → SkiaSharp (`17d2c1a`) hat nur
`ChartRenderer.cs` angefasst. Möglich war das, weil sämtliche Aufrufer — `BausteineVergleich`,
`BausteineProjekt`, `BausteineWirtschaftlichkeit`, `Form_WirtschaftlichkeitVerlauf`,
`ExcelBerichtGenerator` — die Farben ausschließlich als `ChartRenderer.C_*` durchreichen und
nie ein `System.Drawing.Color` selbst bilden. Die Farbfelder und die geschachtelten Typen
`Segment`/`Balken`/`Reihe` tragen seither `SKColor`; die öffentliche Fläche ist unverändert.

**Die Schriftkette hat eine Zwischenstufe bekommen, die die Vorgabe nicht kannte.** iF19
beschied „Calibri → Systemschrift → irgendeine Sans". Der Probelauf unter Linux zeigte, dass
die native SkiaSharp-Fassung ohne fontconfig bei `"Calibri"` nichts liefert und als
Systemschrift **DejaVu *Serif*** zurückgibt — eine Serifenschrift in Achsen und Legenden wäre
gegenüber Calibri ein sichtbarer Rückschritt. Dazwischen stehen deshalb vier serifenlose
Familien: **Carlito, Liberation Sans, DejaVu Sans**, dazu Helvetica/Arial. Ergebnis auf dem
Prüfsystem: **Liberation Sans**. Windows bleibt bei Calibri, macOS/iOS bei Helvetica bzw. SF.
Die Reihenfolge ist dieselbe, die iU7-4 für die Spaltenbreiten des Excel-Berichts benutzt —
Tabelle und Diagramm desselben Berichts sollen nicht in verschiedenen Schriften vermessen
werden.

**Die ClosedXML-Schriftfalle ist in dieser Fassung nicht real.** Das Umsetzungskonzept führte
„ClosedXML-Standardschrift für Nicht-Windows setzen" als offene Aufgabe. Nachgemessen gegen
ClosedXML 0.105.1 (Wegwerf-Harnisch außerhalb des Repos, Linux ohne Calibri und ohne Carlito):
`AdjustToContents` läuft durch, und selbst eine ausdrücklich unsinnige Rückfallschrift fängt
ClosedXML selbst ab — das Paket bringt **Carlito eingebettet** mit („CarlitoBare"), den
metrisch zu Calibri passenden Ersatz. Eine erzwungene Systemschrift hätte die Spaltenbreiten
**schlechter** gemacht (Liberation Sans trägt Arial-Metrik, nicht Calibri-Metrik). Deshalb
entscheidet `GrafikModulSicherstellen()` **dreistufig**, einmal je Prozess:

| Stufe | Bedingung | Folge |
|---|---|---|
| 1 | Calibri oder das metrisch gleiche Carlito ist installiert | `DefaultGraphicEngine(<diese>)`; auf Windows ändert sich nichts |
| 2 | sonst: **Messprobe** — eine Wegwerf-Mappe mit `AdjustToContents` | läuft sie durch, bleibt die Vorgabe von ClosedXML stehen (eingebettetes Carlito) |
| 3 | wirft sie — Abbild ohne Schriften, oder eine künftige ClosedXML-Fassung ohne eingebettete Schrift | erste vorhandene Familie aus Calibri, Carlito, Liberation Sans, DejaVu Sans, Arial; sonst irgendeine installierte |

Das ist die einzige Abweichung von der Vorgabe (sie verlangte den Rückfall bedingungslos) und
sie ist begründet: Stufe 2 verhindert eine Verschlechterung, die niemand bestellt hatte.

**Der Bildvergleich ist ein Modus der Referenzlauf-Suite, kein Skript.** `Referenzlauf.exe
bildvergleich --quelle <sqlite> --projekte 1030,1007,1017 --ziel <ordner>` rechnet je Projekt
frisch, holt den `ZeitreihenSatz` über `ZeitreihenExtraktor.AusLauf` und rendert die neun
Bilder **zweimal** — einmal mit `ChartRendererGdi` (der wortgleichen GDI+-Kopie aus `c6b32eb`),
einmal mit dem neuen `ChartRenderer`. Gemessen werden Bildmaße (Pflicht), der Anteil
abweichender Pixel bei 24/255 Toleranz je Kanal und ein Farbhistogramm über die Palette;
Ergebnis ist eine `bildvergleich.md` mit PASS oder PRUEFEN. Der Pixelvergleich läuft
absichtlich über SkiaSharp und nicht über `System.Drawing` — die Messung soll nicht dieselbe
Bibliothek benutzen, die sie beurteilen soll. **Der Modus läuft nur unter Windows** (die
GDI+-Seite gibt es nur dort); hier wurde er ausschließlich übersetzt. Er ist die Vorbedingung
von **iF23**.

**Zahlen des Nachweises.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64`: 0 Fehler, in
der ersten Welle **123 Warnungen** (unverändert zur Basis `9fe9c71`), nach iU6 **36**.
`dotnet test WP-Plan.Kern.slnf -c Release` nach iU7-8: **872** (869 + 3 Renderer-Tests).
`dotnet run --project Proben/ChartProben -c Release`: *9 Bilder geprueft, 0 Verstoesse*, alle
neun PNG byte-gleich zum Stand vor dem Umzug. Referenzlauf 1030/1007/1017 gegen
`2026-08-30_B3-Kaskade`: **GESAMT PASS**, `diff -rq` ohne Unterschied.

---

### 2.8 Befund iU8 (`EPOS.UI` und der erste Blazor-Dialog), 03.09.2026 — **iZ5 hier erreicht**

Drei Stränge, neunzehn Commits: Strang A `8574911`…`8f5a28e` mit `45a21dc`/`f5fb05c` (Basis
`18f515f`), Strang B `4369fdb`…`eafbc1f` mit `eff82aa`/`e3d1e5b` (Basis `c477523`), Strang C
`479fcf9`…`0af7ca7` und `4aa6b15` (Basis `f5fb05c`).

**Die Paketlage.** Eine eigene Gruppe „Blazor Hybrid (iU8)" in `Directory.Packages.props`:

| Paket | Fassung | Wofür |
|---|---|---|
| `Microsoft.AspNetCore.Components.Web` | 10.0.11 | die Komponenten selbst |
| `Microsoft.AspNetCore.Components.QuickGrid` | 10.0.11 | `EPOS.UI/Standards/Raster` |
| `Microsoft.AspNetCore.Components.WebView.WindowsForms` | **10.0.100** | nur die Windows-Hülle; eigene Zählung, nicht die des .NET-Majors |
| `bunit` | 2.9.0 | `EPOS.UI.Tests` (`BunitContext`, `Render<T>`) |
| `Microsoft.CodeAnalysis.CSharp` | 5.9.0 | Roslyn für `Werkzeuge/Formularkarte` |

**Der Razor-SDK ist keine Kosmetik.** `WindowsFormsApplication1.csproj` steht seit `4369fdb`
auf `Microsoft.NET.Sdk.Razor`. Die Gegenprobe mit dem einfachen `Microsoft.NET.Sdk` übersetzt
fehlerfrei, liefert im Veröffentlichungsordner aber **kein `wwwroot`** — weder `index.html`
noch `_content` noch `_framework/blazor.webview.js`; der Dialog bliebe beim Anwender leer. Die
Umstellung kostet keine neue Warnung (Codes vor und nach identisch). Aus der Anwendung wird
dadurch keine Webanwendung: Sie bleibt `WinExe` und WinForms.

**Der Name der Scoped-CSS-Datei folgt dem Host, nicht der Bibliothek.** Erwartet worden war
`EPOS.UI.styles.css`; erzeugt wird **`EPOS_Plan.styles.css`**, und sie liegt **in `wwwroot\`**,
nicht neben der EXE. Wer sie neben der EXE sucht, hält einen ungestalteten Dialog für einen
Fehler der Bibliothek.

**Der erste Dialog.** `Form_Kosten` → „Energieträger anlegen" öffnet nicht mehr
`Form_Kosten_Auswahl`, sondern `EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor` in
der Hülle `BlazorDialogForm`. Die WinForms-Fassung ist im selben Schritt **gelöscht** (`.cs`,
`.Designer.cs`, `.resx`) — Regel M1, kein Schalter, kein „bis auf Weiteres". Die Datenbankseite
liegt in `EPOS.Kern/Controller/EnergietraegerVarianteCtrl.cs`, die Texte in
`MyResource.Resource.*` (`KAUSW_*`, `ALLG_BTN_*`); der Dialog spricht damit erstmals auch
Englisch. Die Validierungs-MessageBox ist ein Warnbanner **im** Dialog geworden.

**64 bunit-Tests.** `EPOS.UI.Tests` prüft die sieben Bausteine, die acht Standardfelder und den
Dialog — ohne Datenbank, ohne WebView2, ohne Windows. Zusammen mit KiKern (450),
SpeicherEngine (337) und `EPOS.Kern` (35) sind das **886** Tests im Kernfilter.

**Die Formularkarte korrigiert die Vorvermessung.** Der Stapellauf über den ganzen Baum
(`0af7ca7`) findet **123 Designer-Dateien, davon 120 Masken (mit `InitializeComponent`) und 63
lokalisiert (`ApplyResources`)** — nicht 79/74/21. Der Fehler der Vorvermessung war die
Beachtung der Groß-/Kleinschreibung: Der Bestand schreibt beides,
`Form_Kosten_Auswahl.Designer.cs` **und** `Form_BHKWEing.designer.cs`. Die Konzeptzahl 118 war
damit näher an der Wahrheit als die Nachmessung. Gezählt wurden dabei 2 377 Kartenzeilen, 178
Felder ohne Beschriftung und vier selbstgebaute Steuerelemente ohne Zielkomponente
(`AktionsKarte`, `ProjektAuswahl`, `HeaderGradientPanel`, `KlimazonenKarte`).

**Nach dem Stichtag iZ5 sind es 122 Designer-Dateien und 119 Masken**, davon **117 unter
`Views/`** — `Form_Kosten_Auswahl` ist gelöscht. Das war zugleich das Problem des Werkzeugs: 22
seiner 100 Tests lasen genau diese Maske live aus dem Repo und waren seit `92380ea` rot.
**iU8-12e (`4aa6b15`) löst das durch eine Trennung, nicht durch eine andere Probemaske.** Der
letzte Stand der Maske liegt eingefroren unter `Formularkarte.Tests/Pruefmuster/Kosten/`
(Designer, `.cs`, `.resx` und der Aufrufer-Auszug aus `Form_Kosten.cs`, wortgleich aus
`92380ea^`); die Muster werden **nie übersetzt** und vom Stapellauf **übergangen** wie `bin` und
`obj`, damit sie das Vollständigkeitsnetz nicht verfälschen. Die `StapelTests` prüfen weiterhin
den lebenden Bestand — bis iU9-1 an der zeichengleichen Schwester `Form_Kosten_VarAuswahl`,
seit deren Löschung an `Form_KostenKomponente`. **101 Tests, alle grün.** Die Regel dahinter ist allgemein: **Ein Test, der das Werkzeug prüft, gehört
an ein eingefrorenes Muster; ein Test, der den Bestand prüft, an den Bestand.** Mit jeder
weiteren umgestellten Maske wäre sonst dasselbe wieder passiert.

**Das Raster „Label x28 / Control x270" gibt es nicht.** `Point(28,` und `Point(270,` kommen in
je einer Datei vor. Tragfähig ist die Zeilenregel: das nächste Label **links in derselben
Zeile** (|Δy| ≤ 8 px). Sie trägt den Stapellauf über alle Masken.

**Die CI-Läufer rechnen englisch.** Zwei `SpeichernLeiste`-Tests fielen auf `macos-latest` und
auf dem Windows-Runner aus, weil die Leiste dort die englischen Ressourcen zog: Beide Läufer
laufen mit **en-US**-UI-Kultur, Ubuntu und der Entwicklungsarbeitsplatz zufällig deutsch.
Reproduziert mit `LANG=en_US.UTF-8` (2 von 64 rot). Die Testklasse pinnt die UI-Kultur seit
`f5fb05c` im Konstruktor auf `de-DE` und stellt sie in `Dispose` zurück; die Komponente ist
unverändert. **Die Lehre gilt über iU8 hinaus:** Ein Test, der Anzeigetext vergleicht, muss
seine Kultur selbst setzen — sonst prüft er die Sprache des Läufers.

**Zwei Fundstücke für K6 (stilllegen statt mitnehmen).**

| Fund | Befund | Beleg |
|---|---|---|
| ~~`bOhneVariante` in `Form_Kosten_VarAuswahl`~~ — **begraben mit iU9-1** | **totes Feature.** Die Eigenschaft wurde an genau zwei Stellen gesetzt (`Form_Heizkessel.cs:282`, `Form_BHKWEing.cs:513`) — beide Male auf `false`, also auf den Vorgabewert; die Maske selbst hat sie nie gelesen. Mit der Maske ist sie gelöscht | `git grep bOhneVariante` — vor iU9-1 drei Treffer, keiner setzte `true`; heute nur noch das eingefrorene Prüfmuster |
| `SectionPanel` in `Views/Kosten/` | **ohne Nutzer.** `new SectionPanel` kommt im ganzen Repo **null**-mal vor; die Klasse lebt nur noch als Optikvorbild des Blazor-Bausteins `Gruppenkopf` und wird in `EinstiegsKarte` ausdrücklich *nicht* beerbt | `git grep "new SectionPanel"` — 0 Treffer |

Beides gehört in die K6-Liste von iU9 — nicht in die Umstellung. Eine tote Eigenschaft zweimal
zu bauen ist teurer, als sie einmal zu begraben.

**Zahlen des Nachweises (Linux, SDK 10.0.400).** `dotnet build WP-Plan.sln -c Release
-p:Platform=x64 --no-incremental`: **0 Fehler, 34 Warnungen** (vorher 36; die beiden
entfallenen WFO1000 sind `bOhneVariante` und `m_szBVrennstoff` des gelöschten Formulars), keine
neuen Warnungscodes. `EPOS.Kern` allein: 0 Fehler, **3 Warnungen**. `dotnet test
WP-Plan.Kern.slnf -c Release`: **886**. Referenzlauf 1030/1007/1017 gegen
`2026-08-30_B3-Kaskade`: **GESAMT PASS** (815 043 Werte), `diff -rq` nur `protokoll.txt`.
`dotnet publish -r win-x64 --self-contained` enthält `wwwroot/index.html`,
`wwwroot/EPOS_Plan.styles.css`, `wwwroot/_content/EPOS.UI/`, `wwwroot/_framework/blazor.webview.js`,
`EPOS.UI.dll`, `Microsoft.Web.WebView2.{Core,WinForms}.dll` und
`runtimes/win-x64/native/WebView2Loader.dll`.

**Was Windows-seitig offen ist:** die Abnahme von iZ5 selbst — Maus *und* Finger, deutsch *und*
englisch, Hochkontrast, 125 %/150 % (iF21), Enter/Esc, Infoknopf, WebView2-Profilordner, Setup
in der Sandbox ohne WebView2 und ohne Internet (iF20), VS-2026-Designer unter dem Razor-SDK.
Die Punkte stehen einzeln in
[`Umsetzung_iU8_Nachweise.md`](Umsetzung_iU8_Nachweise.md).

**Nachtrag 03.09.2026 — der Öffner des ersten Dialogs war unerreichbar (Befund der
Windows-Abnahme).** `Form_Kosten` ist seit **KD6a kein Einstieg mehr**:
`Views/BerichteKosten/UcBkKosten.btnVerwaltung_Click` öffnet `Form_KostenKomponente`, und
`Views/Hauptformular/Form_Start.cs:2175` entfernt den alten Knopf mit
`EntferneAltknopf(btn_Kosten)`. Der Dialog aus iU8-9 war damit in der Oberfläche gar nicht zu
erreichen; die Abnahmeliste hätte ins Leere geführt. Die gleiche Funktion — Katalog-Energieträger
wählen, Variantennamen vergeben, Projektträger anlegen — lebte in der zeichengleichen Schwester
`Views/Kosten/Form_Kosten_VarAuswahl` mit zwei **erreichbaren** Aufrufern:
`Views/Heizkessel/Form_Heizkessel.cs:251` und `Views/BHKW/Form_BHKWEing.cs:482` (nach der
Umstellung `:304` bzw. `:535`), beide über den Knopf **„◀"** (`btn_Kessel_Hinzu` bzw.
`btn_Hinzu`) hinter der Kachel *Heizkessel* (`Form_Start.cs:624`) / *BHKW*
(`Form_Start.cs:1216`) im Startseiten-Reiter **Energieerzeuger**.

Behoben mit der **vorgezogenen ersten iU9-Welle (`iU9-1`)**: Beide Aufrufer zeigen jetzt
`BlazorDialogForm<EnergietraegerVarianteDialog>` nach demselben Muster wie `Form_Kosten`; die
Schwester ist gelöscht (M1). Damit stehen die drei Abfragen des Dialogs nur noch einmal im
Bestand — der in `EnergietraegerVarianteCtrl` angekündigte „Zweitnutzer" ist eingelöst, statt
zum zweiten toten Zwilling zu werden. Drei Vorabfragen der Aufrufer (`Bezeichner`,
`ID_Kategorie`, `Gruppe`) entfallen ersatzlos: Sie belegten allein `m_szBrennstoff`,
`m_KategorieID` und `m_szKategorie` des alten Dialogs, und **nach** dem Dialog hat keiner der
drei Werte je eine Rolle gespielt. Bewusst in Kauf genommen ist eine Verhaltensabweichung: Die
Auswahlliste ist nicht mehr auf die Kategorie des vorgewählten Brennstoffs eingeengt; der
angelegte Träger bleibt stimmig, weil `group_code`, `pricing_model`, `billing_unit`, Hi, Hs und
die Umrechnung ausnahmslos aus dem **gewählten** Träger abgeleitet werden.

**Die Lehre — sie gilt über iU8 hinaus: Die Wahl der umzustellenden Maske muss die
Erreichbarkeit ihres Öffners prüfen, nicht nur Größe, Feldzahl und Feldtypen.** Ein Dialog, den
niemand aufrufen kann, lässt sich weder abnehmen noch produktiv erproben; er sieht nach
Fortschritt aus und ist keiner. Zwei Prüfungen kosten je eine Minute und hätten den Befund vorweg
geliefert: `git grep -n "new <Maske>"` für die Aufrufer und dann für jeden Aufrufer dieselbe
Frage eine Ebene höher, bis ein Menüpunkt, eine Kachel oder ein Reiter erreicht ist. Als
**Folgepunkt** ist eine Spalte **„Öffner erreichbar"** für `Werkzeuge/Formularkarte` notiert
(`Werkzeuge/Formularkarte/LIESMICH.md`, Abschnitt „Grenzen") — das Werkzeug kennt die Aufrufer
einer Maske bereits (`maske.Aufrufer`), es fehlt allein der Schritt von dort zum Einstieg. **Nicht
mit iU9-1 umgesetzt**, damit die Welle klein bleibt.

### 2.9 Befund iU10 (die iOS-Hülle), 03.09.2026 — **hier erreicht, soweit ohne Mac möglich**

Sieben Commits, `iU10-1`…`iU10-7`, auf `ios_migration`. Die Nachweise im Einzelnen stehen in
[`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md); hier steht, was an Erkenntnis
bleibt.

**Die vorbereitete Paketzeile war doppelt falsch.** `Directory.Packages.props` und
`EPOS.Kern.csproj` trugen seit iU6-T5 eine Vorbereitung für iOS: `SQLitePCLRaw.bundle_green`
2.1.12, bedingt auf `-ios`/`-maccatalyst`. Beim ersten Restore wäre sie zweimal gebrochen:

| Befund | Beleg |
|---|---|
| **Die Fassung 2.1.12 existiert nicht.** `bundle_green` endet bei **2.1.11** und ist in SQLitePCLRaw 3.0 ganz entfallen | Restore-Probe: `NU1102` |
| **Die Begründung war überholt.** Sie lautete „auf iOS verbietet die AOT-Regel das dynamische Laden". `bundle_e_sqlite3` lädt dort aber gar nichts: Für die iOS-Kennungen liefert das Paket `provider.internal` (`DllImport __Internal`) und eine **statisch gelinkte** `e_sqlite3.a` je Gerät und Simulator (`NativeReference Kind=Static ForceLoad`) | Paketinhalt 2.1.12 |

**Das STRICT-Gate ist der eigentliche Grund** (→ iF27). `Referenzlaeufe/Kenndaten_Test.sqlite`
führt **114 von 115 Tabellen als `STRICT`**; das verlangt SQLite ≥ 3.37. Mit `bundle_green` hinge
die Fassung an der System-SQLite des Geräts — iOS 17 = 3.42 (belegt), iOS 18/26 nicht belegbar und
in keinem Fall steuerbar. Mit `bundle_e_sqlite3` steht auf **allen vier Läufern** dieselbe
**SQLite 3.53.3**; auf Linux nachgemessen über den Prüfstand von iU10-7. Die App schreibt die
beiden Zahlen beim Start ins Protokoll (`SQLite 3.53.3`, `STRICT=114`), und der CI-Job prüft sie.

**Runner- und Xcode-Lage — beides muss gemeinsam gepinnt werden.** Das Workload-**Set**
`10.400.1` bringt das iOS-Manifest **26.5.10315** mit, und dessen `WorkloadDependencies.json`
verlangt `"xcode": "[26.6,)"`. Der Job setzt deshalb `DEVELOPER_DIR=/Applications/Xcode_26.6.app`
und läuft auf **`macos-26`** statt auf `macos-latest` — das Label wandert, und ein neuer
Xcode-Standard bräche den Bau ohne eine einzige Codeänderung (iR-a). Das Abbild `macos-26` führt
macOS 26.6.1, Xcode 26.6 als Standard, Simulator-Laufzeiten iOS 26.2/26.4/26.5, iPad-Gerätetypen
und .NET 10.0.400 vorinstalliert — **Workloads sind nicht vorinstalliert** und werden im Job
nachgezogen (~3–6 min).

**Die Paketversionen der Hülle.**

| Paket | Fassung | Warum genau diese |
|---|---|---|
| `Microsoft.AspNetCore.Components.WebView.Maui` | **10.0.100** | letzte stabile Fassung; hängt an `Microsoft.Maui.Controls` **derselben** Fassung |
| `Microsoft.Maui.Controls` | **10.0.100** | das Workload-Set setzt intern 10.0.20 — ohne Angleich über `<MauiVersion>` stünden zwei Stände im Graph (`NU1605`, iR-b) |
| `SkiaSharp.NativeAssets.iOS` | **3.119.0** | dieselbe Zählung wie die drei anderen Nativen; trägt **nur** `net8.0-ios17.0` und erzwingt damit iPadOS 17.0 (iF28) |
| `SQLitePCLRaw.bundle_e_sqlite3` | **2.1.12** | die Fassung, die `Microsoft.Data.Sqlite` 10.0.11 ohnehin transitiv zieht |
| `Microsoft.ML.OnnxRuntime` | 1.22.1, `ExcludeAssets="native;build;buildTransitive"` | die native Hälfte ist eine **46 MB** große `xcframework` mit C++-Linkung (iF26) |

**Drei Abweichungen von der Planvorlage, jede mit Grund.**

1. **`Microsoft.NET.Sdk.Razor` statt `Microsoft.NET.Sdk`.** Nur das Razor-SDK packt die statischen
   Web-Bestände einer Razor-Klassenbibliothek mit ein. Ohne es fehlte `_content/EPOS.UI/epos-ui.css`
   im Anwendungspaket, und die Oberfläche wäre ungestaltet — derselbe Befund wie in iU8-6 (§ 2.8).
   Damit entfällt zugleich die `MauiAsset`-Zeile für `wwwroot`.
2. **`SingleProject=true`.** Ohne die Eigenschaft sucht die iOS-Kette `Info.plist` im Projektstamm
   statt unter `Platforms/iOS/`.
3. **`App.CreateWindow` statt `MainPage`.** `Application.MainPage` ist seit .NET 9 abgekündigt.

**Ein Namensraum für die ganze Hülle.** Die Adapter liegen im Ordner `Dienste/`, aber im
Namensraum `EPOS.iOS` — ein Unter-Namensraum `EPOS.iOS.Dienste` verdeckte den statischen Halter
`WindowsFormsApplication1.Dienste` des Kerns, und `Dienste.Pfade` löste gegen den eigenen Ordner
auf. Die Windows-Hülle hält es genauso (`WindowsFormsApplication1/Dienste/*.cs` liegen alle im
Namensraum `WindowsFormsApplication1`).

**Was iU10 bewusst NICHT tut.** Das **Anlegen** einer Energieträger-Variante schreibt noch nicht:
Der Schreibweg (Katalogsuche, `INSERT` in `energy_carrier`, Preishistorie, Projektzuordnung) steht
bis heute in `Views/Kosten/Form_Kosten.CreateNewEnergyCarrier` und hängt dort am Typ
`EnergyCarrier` und an `EnergietraegerKatalogCtrl` — beides ist mit Absicht in der Anwendung
geblieben. Ihn in der iOS-Hülle nachzubauen wäre genau die Doppelpflege, die Modell C abschafft.
Der Dialog läuft vollständig (echte Trägerliste, Prüfung, Ergebnis samt der sechs abgeleiteten
Werte über `EnergietraegerVarianteCtrl.Ergaenzen`); das Anlegen wartet auf den Umzug des
Schreibwegs in den Kern.

**Die Grenze der Linux-Nachweise — sie ist scharf zu ziehen.** Übersetzt wird hier gegen
**Attrappen** der MAUI-, UIKit- und Foundation-API. Das belegt, dass der eigene Programmtext in
sich stimmt: Namen, Typen, Überladungen, Nullbarkeit. Es belegt **nicht**, dass die echte MAUI-API
so heißt. Der erste Lauf von `ios.yml` ist deshalb kein Formalakt, sondern der eigentliche Beweis.

---

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
