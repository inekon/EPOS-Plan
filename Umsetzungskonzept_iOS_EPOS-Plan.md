# Umsetzungskonzept: EPOS-Plan auf iOS

**Rev. 2.2 — 03.09.2026 — Stand nach der Kette iU4…iU8**

Basis: Branch `sqlite`, Stand `6486c36` (02.09.2026) — **die Access-Ablösung ist vollzogen**.
Rev. 1 war gegen `main` (`7d41833`) vermessen, als die Datenschicht noch Access trug; die daraus
folgenden Änderungen sind in § 1.4 und § 5 eingearbeitet.
Referenzbasis der Nachweise: `Referenzlaeufe\2026-08-30_B3-Kaskade` (13 Projekte, 332 CSV, Schemastand 61).
Sämtliche Zahlen dieses Dokuments sind am 02.09.2026 gegen den Arbeitsbaum nachgemessen; Abweichungen
zu den Vorgängerdokumenten sind in § 1.5 einzeln ausgewiesen.

> **Verhältnis der Dokumente**
> [`Konzept_iOS-Portierung_EPOS-Plan.md`](Konzept_iOS-Portierung_EPOS-Plan.md) (Rev. 1, 30.08.2026)
> beantwortet **was und warum** — Leitentscheidungen iL1–iL8, Modell C, Migrationsregeln M1–M10,
> Arbeitsblöcke A–E. Es bleibt gültig und wird hier **nicht wiederholt**, sondern zitiert.
> [`Konzept_DB-Migration_SQLite_EPOS-Plan.md`](Konzept_DB-Migration_SQLite_EPOS-Plan.md) (Rev. 2,
> 31.08.2026) beantwortet die **Datenschicht** — Etappen S0–S8, Entscheidungen D1–D8, Risiken R1–R4.
> Dessen Bauanleitung ist `Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md` (auf Branch
> `sqlite`) — **sie ist mit `6486c36` vom 02.09.2026 vollständig umgesetzt** (S4a–S8, Referenzlauf
> 10/10 Projekte und 234/234 CSV bitgleich, Proben 16/16). Beides wird hier nur eingebunden.
> **Dieses Dokument beantwortet: wie, in welcher Reihenfolge, womit gebaut und woran nachgewiesen.**
> Sein Schwerpunkt liegt auf dem neuen Auftragsbestandteil — der **Anpassung der
> Entwicklungsumgebung** (§ 3).

**Lesehinweis zu den Kürzeln.** Vorhandene Familien werden nie neu nummeriert. Neu in diesem Dokument
sind **iU** (Arbeitspakete), **iE** (Bausteine der Entwicklungsumgebung), **iZ** (Meilensteine),
**iT** (Nachweise), **iR** (Risiken) und **iF10 ff.** (neue Entscheidungsfragen in der Familie des
Grundlagenkonzepts). Fremde Etappenkürzel werden immer qualifiziert zitiert: „Grundlagen-S1",
„SQLite-S4". Das vollständige Glossar steht in § 9.2.

---

## 1 Ergebnis der Umsetzungsprüfung

### 1.0 Was Rev. 2 gegenüber Rev. 1 ändert

Zwischen beiden Fassungen liegen Stunden, aber ein großer Schritt: Die Access-Ablösung ist am
02.09.2026 mit `6486c36` **vollzogen**.

| | Rev. 1 (Vormittag) | Rev. 2 (nach `6486c36`) |
|---|---|---|
| Datenschicht | Konzept + Bauanleitung, kein Anwendungscode | **fertig** — S4a–S8, A/B-Vergleich 10/10 Projekte, 234/234 CSV bitgleich |
| Etappe „Datenschicht" | eigene Etappe vor der Kern-Herauslösung | **entfällt ersatzlos** — die iOS-Kette beginnt direkt nach dem Go/No-Go-Gate |
| iR1 (Stichtag kommt nicht) | größtes Terminrisiko | **erledigt** |
| iU6 | großes Paket mit Transaktionen, Fremdverbindungen, Schemaauskunft | **kleines Paket** — das Meiste ist gebaut |
| § 1.4 | Konflikt **vorhergesagt** | Konflikt **eingetreten und gemessen** — mit kleiner, klarer Restaufgabe |

**Was sich nicht geändert hat und weiterhin die erste Hürde ist:** die COM-Sperre aus § 1.1. Der
Commit hat die Projektdatei nur um `Microsoft.Data.Sqlite` ergänzt; die beiden `COMReference` stehen
unverändert, das Hauptprojekt baut weiterhin nur im Visual-Studio-MSBuild, und `Referenzlauf` ist
weiterhin `net8.0-windows` mit `UseWindowsForms` und Projektreferenz auf die WinForms-App. Ebenso
unverändert: die Support-Frist zum 10.11.2026.

**Rev. 2.2 — Stand nach der Kette iU4…iU8 (03.09.2026).** Zwischen Rev. 2.1 und dieser Fassung
liegt ein einziger Tag und die gesamte Etappe „Gesundung" samt UI-Fundament: **iU4** hat den Kern
physisch herausgelöst, **iU5** ihm die Umgebung abgenommen und in einem zweiten Umzug 74 weitere
Dateien nachgeholt, **iU6** den Datenzugriff von OleDb befreit, **iU7** Renderer und Berichtsausgabe
plattformfrei gemacht und **iU8** mit dem ersten Blazor-Dialog den Modell-C-Stichtag iZ5 gesetzt.
`EPOS.Kern` ist von 91 verlinkten auf **268 physische** `.cs`-Dateien gewachsen, `CA1416` steht bei
**0**, die Lösung baut mit **34 Warnungen**, der Kernfilter meldet **886 Tests**, und der
Referenzlauf 1030/1007/1017 ist nach **jeder einzelnen Tranche** byte-gleich zur Basis
`2026-08-30_B3-Kaskade` geblieben. **Sämtliche Nachweise sind auf Linux geführt**; alles, was ein
Windows braucht — der Vollreferenzlauf 332/332, der Bildvergleich, die Bedienprobe, das Setup —
steht in den beiden Nachweislisten (§ 6). Zwei Planungsposten sind dabei ersatzlos entfallen
(`EPOS.Daten`, die ClosedXML-Schriftübersteuerung), einer ist neu entstanden (die DPI-Insel).
Was Rev. 2.2 im Einzelnen nachführt, steht in § 1.6.

### 1.1 Die eine Aussage, die alles andere ordnet

Das Grundlagenkonzept nennt als Voraussetzung „Mac für Build und Signierung". Das ist richtig, aber
es ist nicht die erste Hürde. Die erste Hürde steht in der Projektdatei:

```xml
<COMReference Include="Microsoft.Office.Interop.Excel"> … </COMReference>
<COMReference Include="VBIDE"> … </COMReference>
```

`WindowsFormsApplication1.csproj` — und die dokumentierte Folge in
`WindowsFormsApplication1/CLAUDE.md`: **„`dotnet build` scheitert an den COM-Referenzen (MSB4803) —
bauen nur über das MSBuild von Visual Studio."**

Daraus folgt dreierlei, und es gilt heute für **jede** Zeile Code des Hauptprojekts:

| Folge | Bedeutung |
|---|---|
| Auf einem Mac ist das Projekt **nicht baubar** | `ResolveComReference` existiert nur im vollen MSBuild aus Visual Studio. Kein Mac, kein Linux, kein Container |
| In jeder CI ist es **nicht baubar** | ein `windows-latest`-Runner mit installiertem VS könnte es, ein Standard-`dotnet build`-Schritt nicht |
| Das Referenzlauf-Werkzeug erbt die Sperre | `Referenzlauf.csproj` bindet die komplette WinForms-App per `ProjectReference` ein und ist deshalb selbst `net8.0-windows` + `UseWindowsForms=true` |

**Der Nachweis, auf dem die ganze Portierung ruht — „der Kern rechnet auf dem iPad wertgleich" — ist
mit dem heutigen Zuschnitt technisch nicht führbar.** Nicht, weil der Rechenkern plattformgebunden
wäre (er ist es nicht: `Allgemein/BhkwPlan.cs`, 410 Zeilen, null Windows-APIs), sondern weil er in
einem Projekt liegt, das man außerhalb von Visual Studio nicht übersetzen kann.

Deshalb steht in diesem Umsetzungskonzept die Entwicklungsumgebung **vor** allem anderen, und deshalb
ist der Machbarkeits-Spike (Grundlagen-S0) **nicht** das erste Paket, sondern das dritte.

### 1.2 Was bereits steht — und was bei null steht

| Baustein des Grundlagenkonzepts | Stand 02.09.2026 | Beleg |
|---|---|---|
| Muster für eine UI- und DB-freie Kernbibliothek | **zweimal erprobt** — `SpeicherEngine` (30 `.cs`) und `KiKern` (22 `.cs`), beide `net8.0`, AnyCPU, **null Paket- und null Projektreferenzen**, je mit eigenem Testprojekt | beide `.csproj` grenzen im Kopfkommentar ausdrücklich ab: „darf weder `WPPlan.Core` noch `DataRepository` noch `System.Windows.Forms` sehen" |
| Regressionsnetz | **vorhanden, scharf und bereits SQLite-fähig** — Referenzlauf-Suite, 6 Modi, Kindprozess je Projekt, Toleranz rel. 1e-4 / abs. 0,01; mit `6486c36` auf beide Backends erweitert und für den A/B-Beweis der Umstellung eingesetzt | `Referenzlaeufe/LIESMICH.md`, `Referenzlauf/DbUmgebung.cs` |
| Chart-Stack SkiaSharp/ScottPlot | **Pakete vorhanden, praktisch ungenutzt** — `SkiaSharp 3.119.0`, `ScottPlot.WinForms 5.1.57` im `.csproj`; `ScottPlot` erscheint in **1** Datei, `SkiaSharp` in **0** | einziger Konsument: `Form_SpeicherOptimierung` |
| Berichtskette portabel | **bereits gegeben** — `ClosedXML 0.105.1` (ab 0.97 ohne `System.Drawing.Common`), `DocumentFormat.OpenXml 3.5.1`, `BouncyCastle 2.7.0`, `MathNet.Numerics 5.0.0` | `.csproj` |
| Datenschicht SQLite | **ausgeführt.** Branch `sqlite`, Commit `6486c36` (02.09.2026): S4a–S8 vollständig — `DataRepository`/`RecordSet` auf `Microsoft.Data.Sqlite`, `DbVorgang`, Schema-Auskunft per `PRAGMA`, Dialekt-Sweep, Schemapflege-Gabelung (`SCHRITTE_SQLITE`), Erststart-Migration, `EposSqliteMigrator` als eigenes Projekt, Zielschema mit 114 `STRICT`-Tabellen / 14 Sichten / 179 Indizes. Nachweis: A/B-Vergleich **10/10 Projekte, 234/234 CSV bitgleich**, Proben 16/16 | `BETRIEB_SQLITE.md`, `sql/`-Protokolle S1–S8 |
| `EPOS.Kern` / `EPOS.UI` | **existiert nicht** — kein Projekt, kein Verzeichnis. `WPPlan.Core` ist nur ein Namespace *innerhalb* der WinForms-App | — |
| CI | **existiert nicht.** `.github/` enthält genau eine Datei, `copilot-instructions.md`, und deren Inhalt ist themenfremder Vorlagenrest (vier Zeilen generischer Azure-Regeln) | kein `workflows/`, kein Azure Pipelines, kein Jenkinsfile |
| Zentrale Buildsteuerung | **existiert nicht** — kein `Directory.Build.props`, kein `Directory.Packages.props`, kein `NuGet.config`, kein `global.json`. Jedes Projekt trägt seine Versionen selbst, die SDK-Version ist nirgends festgeschrieben | `find` bis Tiefe 3, kein Treffer |
| Mac, Xcode, MAUI-Workloads, Apple-Konto | **nirgends erwähnt**, in keinem Dokument, keinem Skript, keiner Projektdatei | — |

### 1.3 Zwei Fristen, die von außen gesetzt sind

Beide waren am 30.08.2026 nicht im Blick und ändern die Reihenfolge:

**(1) .NET 8 und .NET 9 erreichen am 10. November 2026 das Support-Ende.** Microsoft hat die
STS-Laufzeit auf 24 Monate verlängert, wodurch beide Versionen **denselben** Endtermin tragen. Danach
gibt es keine Sicherheitsaktualisierungen mehr. Der Bestand steht heute auf:

| Projekt | heute | nach dem 10.11.2026 |
|---|---|---|
| `WindowsFormsApplication1`, `Referenzlauf` | `net8.0-windows` | ohne Support |
| `SpeicherEngine`, `KiKern` | `net8.0` | ohne Support |
| `SpeicherEngine.Tests`, `KiKern.Tests` | `net9.0` | ohne Support |
| `CSExeCOMServer` | .NET Framework 4.0 | (Framework-Lebenszyklus, hier ohne Belang — totes Altgut) |
| `EposSqliteMigrator.Kern` und `EposSqliteMigrator` (Konsole) | `net8.0` | ohne Support |
| `ZugriffsschichtProben` | `net8.0-windows` | ohne Support |

Dazu kommt seit `6486c36` **`Microsoft.Data.Sqlite 8.0.11`** — eine 8.x-Fassung, die beim Sprung
auf .NET 10 auf 10.x mitzuziehen ist.

Nächstes LTS ist **.NET 10** (Support bis November 2028); .NET 9 ist kein Zwischenziel, weil es
zeitgleich ausläuft. Der Sprung auf .NET 10 ist damit **ohnehin fällig, unabhängig von iOS** — und er
ist zugleich die Voraussetzung für die aktuelle MAUI-Fassung. Beides in einem Zug zu erledigen ist der
einzige Weg, der die Arbeit nicht zweimal macht. Dass die App bereits heute
`Microsoft.Extensions.Http` und `.Logging` in **Version 10.0.3** zieht, zeigt, dass der Bestand dem
Sprung nicht im Weg steht.

**(2) Die Apple-Toolchain ist versionsstarr gekoppelt.** Eine gegebene .NET-für-iOS-Fassung verlangt
**genau eine** Xcode-Hauptversion; eine neuere Xcode-Installation lässt den Build mit einer
ausdrücklichen Fehlermeldung scheitern, bis die passende Workload-Aktualisierung erscheint. MAUI 10
setzt zudem **Visual Studio 2026** voraus — Visual Studio 2022 trägt nur bis MAUI 9. Das ist keine
einmalige Einrichtung, sondern eine **laufende Pflegeverpflichtung** (→ iR2).

### 1.4 Die Datenschicht ist umgestellt — und der iOS-Blocker ist damit real

Rev. 1 hat an dieser Stelle einen Konflikt vorhergesagt. Er ist mit `6486c36` **eingetreten**, und
zwar genau so, wie beschrieben. Das ist kein Vorwurf an die Umsetzung: Für Windows war die
Entscheidung richtig, sie ist gemessen begründet und hat die Umstellung überhaupt erst in
21–30 PT möglich gemacht. Für iOS ist sie eine Wand.

**Gemessen am Stand `6486c36`:**

| Befund | Messung | Folge für iOS |
|---|---|---|
| `System.Data.OleDb 8.0.1` steht **weiterhin** im `.csproj`, neben `Microsoft.Data.Sqlite 8.0.11` | beide `PackageReference` vorhanden | Das Paket ist **Windows-only** — auf iOS nicht verfügbar |
| Alle fünf Ausführungsmethoden führen **weiterhin** `params OleDbParameter[]` | `GetDataTable` (Z. 512), `ExecuteSQL` (545), `ExecuteNonQuery` (564), `ExecuteInsertAndGetId` (584), `ExecuteScalar` (612) | Diese Signaturen können in `EPOS.Kern` nicht existieren |
| `RecordSet.DBCommand` ist **weiterhin** `public OleDbCommand` — und wird aktiv genutzt (`new OleDbCommand()` im Konstruktor, Parameter-Extraktion nach `OleDbParameter[]`) | `RecordSet.cs:49`, dort ausdrücklich: „Die oeffentliche Flaeche bleibt Zeichen fuer Zeichen" | 47 echte Nutzer hängen daran |
| Dateien mit `OleDb`-Bezug | **170** (vor der Umstellung 164) | die Fläche ist nicht kleiner geworden |

**Was die Umstellung für iOS trotzdem gebracht hat** — und das ist erheblich:

| Gewinn | Bedeutung |
|---|---|
| `DbVorgang` (`Allgemein/DbVorgang.cs`, 157 Z.) ersetzt das `(OleDbConnection, OleDbTransaction)`-Tupel | Der providerneutrale Transaktionstyp, den iOS braucht, **existiert bereits** |
| Schema-Auskunft per `PRAGMA` statt `GetOleDbSchemaTable`: `TabelleVorhanden`, `SpalteVorhanden`, `SpaltenVonTabelle`, `IndexListe`, `FremdschluesselListe` | sauber gekapselt und plattformfrei |
| ADOX, `@@IDENTITY`, `SELECT TOP`, Jet-Casts, `CommandBuilder` sind ersetzt | der gesamte Access-Dialekt ist aus dem Weg |
| ACE/OLE-DB als **Datenbanktreiber** ist abgelöst; `Microsoft.Data.Sqlite` bringt die native Bibliothek mit | kein ACE-Redist mehr, keine Bitness-Falle |
| Referenzlauf-Suite auf beide Backends erweitert, A/B-Beweis 234/234 CSV bitgleich geführt | Das Abnahmeinstrument trägt den Backendwechsel nachweislich |

**Die verbleibende Aufgabe ist damit klein und klar umrissen:** `System.Data.OleDb` wird nicht mehr
als *Treiber* gebraucht, sondern nur noch als *Datenträgertyp* für Parameter. Was fehlt, ist der
Austausch dieses Typs — nicht mehr die Umstellung einer Datenbank.

| Weg | Aufwand | Bewertung |
|---|---|---|
| **(a)** `OleDbParameter` überall durch `DbParam` ersetzen | ~2.300 Konstruktoraufrufe in 160 Dateien; weitgehend maschinell, da die Aufrufe uniform sind | ein großer, mechanischer Einzeleingriff — jetzt billiger als vor der Umstellung, weil `DataRepository` innen bereits sauber ist |
| **(b)** `IDatenzugriff` mit `DbParam`; `DataRepository` behält seine OleDb-Fläche als Windows-Adapter | gering — nur die neue Schnittstelle; Altaufrufe wandern mit ihren Masken in iU9 | **Empfehlung, unverändert gegenüber Rev. 1** |

**Empfehlung (→ iF10): Weg (b).** `EPOS.Kern` definiert `IDatenzugriff` mit einem eigenen schlanken
`DbParam` (Name, Wert, optionaler Typ). `DataRepository` bleibt exakt in seiner heutigen Fassung und
wird zusätzlich als **Windows-Adapter** dahintergehängt — die `?`→`@pN`-Übersetzung
(`UebersetzeParameterzeichen`, Z. 270) und `NormalisiereWert` (Z. 312) sind ohnehin schon
providerneutral geschrieben. Die ~2.300 Altaufrufe bleiben unangetastet und wandern erst, wenn ihre
Maske ohnehin nach `EPOS.UI` umgebaut wird (iU9, Strangler-Muster M1).

**Verbindlich festzulegen ist dazu:** Neuer Datenzugriffscode geht ab dem Stichtag (iZ5)
ausschließlich über `IDatenzugriff`/`DbParam`, nie mehr über `DataRepository` mit `OleDbParameter` —
sonst wächst der Altbestand weiter, während er abgebaut werden soll.

### 1.5 Korrekturen an den Vorgängerdokumenten

Nachgemessen am 02.09.2026. Die Abweichungen ändern an keiner Leitentscheidung etwas, aber sie
verschieben Aufwände — und einige Zahlen wandern seit dem 19.08. unkorrigiert von Dokument zu
Dokument.

**Gegenüber dem Grundlagenkonzept (§ 2, § 6.1):**

| Größe | dort | gemessen 02.09. | Einordnung |
|---|---|---|---|
| `Kenndaten.accdb` | 145 MB | **151,9 MB** | wächst weiter |
| gespeicherte Access-Abfragen | 20 | **17** in der DB, **14** im Code referenziert, davon **11** fachlich benötigt | die drei übrigen (`Abfrage_Kuehlung_MaxLast`, `Abfrage_SST`, `Abfrage_KenndatenKuehlung_Max`) erscheinen **ausschließlich** in `SchemaMigration.cs` als Namenskonstanten und Reparatur-DDL — kein einziger lesender Anwendungszugriff. Mit dem Einfrieren des Access-Zweigs verlieren sie ihren letzten Verwender |
| Dateien mit DB-Zugriff | 179 | **160** `DataRepository` · **60** `RecordSet` (grep) bzw. **47** echte Nutzer · **35** eigene `OleDbConnection` (36 mit `RecordSet`, 67 Stellen) | die pauschale Zahl verdeckte drei verschieden schwere Baustellen; die engeren Zahlen stammen aus der SQLite-Bauanleitung |
| Jet-Dialektstellen | 17 Dateien | **`IIf` 14 + `UCase` 3, sämtlich in `SchemaMigration.cs`**, dazu `IIf` in 2 Views | entfällt mit dem Einfrieren des Access-Zweigs fast vollständig. **Real umzustellen sind stattdessen: 42 × `SELECT TOP`, 20 × `@@IDENTITY`, 42 Boolean-Literale** |
| nicht UTF-8 kodierte `.cs` | 93 | **68** (64 × cp1252, 4 × unknown-8bit), alle in `WindowsFormsApplication1/` | die 93 stammen vermutlich aus einer Zählung, die an Dateinamen mit Leerzeichen zerbrach |
| `Form_Simulation_Detail.cs` | 6.200 Zeilen | **7.773** (mit Designer 10.855) | seither um ein Viertel gewachsen — das größte Einzelstück wird teurer, nicht billiger |
| Registry-Zugriffe | 3 Dateien | **9** | hinzugekommen: KI-Modul (`KiEinwilligung`, `KiChatService`), CSV-Export, Variantentest, Lizenzdialog |
| DPAPI | 2 Dateien | **2**, aber andere | nicht mehr nur die Lizenz — `KiChatService` legt den API-Schlüssel ebenso ab |
| `MessageBox.Show` / `ShowDialog` / `DialogResult` | 99 / 74 / 131 | **127 / 115 / 149** projektweit (99 / 94 / 131 nur unter `Views/`) | das Grundlagenkonzept zählte den View-Anteil; für die Dienst-Shims (A3/M4) gelten die Projektsummen |
| Chart- und Grid-Masken | 16 / 16 | **18 Chart-Masken (32 Steuerelemente)** und **19 Grid-Masken (22 Steuerelemente)** im Build | mit iU0 durch Einzeldurchsicht geklärt (Entscheidungsregister § 3). Die in Rev. 2 genannten **9** bzw. **7** waren ein Grep-Artefakt: das Muster traf nur `*.Designer.cs` und übersah die Schreibweise `*.designer.cs`. Die Konzeptzahl 16/16 war damit nahezu richtig |

**Unverändert bestätigt:** 569 `.cs` im Hauptprojekt · 204 View-Dateien (118 mit Designer, 42 rein
programmatisch) · 61 `RecordSet`-Dateien (60 nach engerer Zählung) · 40 Dateien an den
`Program.*`-Statics · 44 Dateien an `Program.Zahl*` · 39 Dateien `DataVisualization` · 2 Dateien
Excel-COM · `BhkwPlan.cs` 410 Zeilen · `SchemaMigration.cs` 13.589 Zeilen, 61 Schritte, `ZIEL_VERSION`
= 61.

**Präzisierung zu iL2.** Das Grundlagenkonzept formuliert: „Windows implementiert sie mit ACE/OLE DB
wie heute, iOS mit SQLite." Diese Zweigleisigkeit ist durch iF9 und das SQLite-Konzept **überholt**:
Es wird **keinen** Parallelbetrieb geben. Das SQLite-Konzept stellt in § 9 ausdrücklich fest, dass der
Providerbruch die Weiche `Access | SQLite` im selben Build verhindert — anders als beim verworfenen
SQL-Server-Weg. Damit trägt `IDatenzugriff` **nur einen** Dialekt, und M3 („übergangsweise zwei
Dialekte") entfällt ersatzlos. Das ist eine Vereinfachung, keine Erschwernis.

### 1.6 Revalidierung

Dieses Kapitel wird bei jeder Revision neu ausgeführt: Zählungen aus § 1.5 nachmessen, Fristen aus
§ 1.3 prüfen, Paketstände gegen die Build-Matrix (§ 3.6) halten. Stand Rev. 2: Messdatum
02.09.2026, Branch `sqlite` @ `6486c36`. Die Datenschichtangaben der Rev. 1 sind überholt und in
§ 1.4 ersetzt.

**Rev. 2.1 — Revalidierung nach iU0 und iU1**, Branch `ios_migration` @ `0ddc417` (02.09.2026).
Nachgeführt sind: die Chart-/Grid-Zeile in § 1.5 (a), die Build-Matrix in § 3.6 (b), der
Umsetzungsstand von iU1 samt zweier Befunde, die diese Planung nicht kannte (c), die Bausteine
iE1–iE4 in § 3.10 (d), der Stichtag .NET 10 in § 5.3 (e) und der Meilenstein iZ1 in § 4.1 (f).
Alles Übrige der Rev. 2 bleibt unverändert gültig. **Sämtliche Nachweise sind hier — auf
Linux — geführt; die Windows-Abnahme steht aus** und ist je Commit abhakbar in
[`Umsetzung_iU0_iU1_Nachweise.md`](Umsetzung_iU0_iU1_Nachweise.md) aufgelistet.

**Rev. 2.2 — Revalidierung nach iU4, iU5, iU6, iU7 und iU8**, Branch `ios_migration` @ `f95fc34`
(03.09.2026). Nachgeführt sind: das Zielbild der Solution in § 2.1 samt Statusspalte (a), die
Build-Matrix in § 3.6 (b), die fünf Statusblöcke in § 4 und die neue Gesamtübersicht an seinem
Anfang (c), die Meilensteine iZ4 und iZ5 in § 4.1 (d), die Etappenübersicht in § 5.1 (e), die
Nachweistabelle „hier geführt / auf Windows offen" in § 6 (f) und die Risikoliste in § 7 —
vier entschärfte, vier neue (g). Alles Übrige der Rev. 2/2.1 bleibt unverändert gültig.
**Sämtliche Nachweise dieser Kette sind auf Linux geführt**; die Windows-Abnahme steht aus und
ist je Commit abhakbar in [`Umsetzung_iU0_iU1_Nachweise.md`](Umsetzung_iU0_iU1_Nachweise.md)
und [`Umsetzung_iU8_Nachweise.md`](Umsetzung_iU8_Nachweise.md).

---

## 2 Zielarchitektur in der Umsetzungssicht

Das Architekturbild (Modell C: ein Kern, eine UI-Bibliothek, zwei Hüllen) steht im Grundlagenkonzept
§ 6a.2 und wird hier nicht wiederholt. Was hier steht, ist seine **Projektfassung**: welche
`.csproj` entstehen, was sie referenzieren dürfen und woran der Bruch der Regel auffällt.

### 2.1 Zielbild der Solution

| Projekt | Art | TargetFramework(s) | Plattform | darf referenzieren | Status |
|---|---|---|---|---|---|
| `EPOS.Kern` | Klassenbibliothek | `net10.0` | AnyCPU | **nichts** aus dem Bestand; nur plattformfreie Pakete | **vorhanden, gewachsen** — iU3 91 verlinkte, iU4-5 **168 physisch verschobene**, nach dem zweiten Umzug (iU5-U) und `ChartRenderer` (iU7-5) **268 `.cs`**. **CA1416 = 0**, **kein `System.Data.OleDb`** (iU6), 0 Fehler / **3 Warnungen** |
| ~~`EPOS.Daten`~~ | — | — | — | — | **entfällt (iU6)** — `IDatenzugriff` und `SqliteDatenzugriff` liegen in `EPOS.Kern/Allgemein/`; ein eigenes Projekt hätte den Kern von seiner Zugriffsschicht getrennt, ohne dass ein zweiter Anbieter in Sicht ist |
| `EPOS.UI` | Razor-Klassenbibliothek | `net10.0` | AnyCPU | `EPOS.Kern` | **vorhanden** (iU8-2) — `EnableWindowsTargeting=false` wie im Kern; 7 Bausteine, 8 Standardfelder, 1 Dialog |
| `EPOS.Kern.Tests` | xUnit | `net10.0` | AnyCPU | `EPOS.Kern` | **vorhanden** (iU4-6) — **35 Tests** (9 iU4-6, 9 iU6-T4, 14 `DiensteTests` iU5, 3 Renderer iU7-8) |
| `EPOS.UI.Tests` | xUnit + bunit 2.9.0 | `net10.0` | AnyCPU | `EPOS.UI` | **vorhanden** (iU8-5/5b/5c) — **64 Tests**, UI-Kultur auf `de-DE` gepinnt |
| `EPOS.Referenzlauf` | Konsole | `net10.0` | AnyCPU | `EPOS.Kern` | **vorhanden** (iU3) — ersetzt `Referenzlauf` für den Kernbeweis; fährt in der CI 1030, 1007, 1017 |
| `SpeicherEngine`, `KiKern` | Klassenbibliothek | `net10.0` | AnyCPU | nichts | ✔ **angehoben** (iU1, `a81fc1b`); `KiKern` ist seit iU5-U2 `ProjectReference` des Kerns |
| `SpeicherEngine.Tests`, `KiKern.Tests` | xUnit | `net10.0` | AnyCPU | ihre Engine | ✔ **angehoben** (iU1) — 337 bzw. 450 Tests |
| `WindowsFormsApplication1` | WinExe | `net10.0-windows` | x64 | alles Obige (COM ist mit iU1-P1.1 entfallen) | **bleibt** — schrumpft über iU9; `ProjectReference` auf `EPOS.Kern` **und** `EPOS.UI`, SDK seit iU8-6 `Microsoft.NET.Sdk.Razor`. Von 585 `.cs` sind **356** übrig; unter `Allgemein/` und `Controller/` noch **62** von 133 |
| `Referenzlauf` | Konsole | `net10.0-windows` | x64 | WinForms-App | **bleibt**, bis iU9 abgeschlossen ist |
| `EPOS.iOS` | MAUI-App (Blazor Hybrid, `Microsoft.NET.Sdk.Razor`) | `net10.0-ios`, `SupportedOSPlatformVersion` 17.0 | ARM64 (`iossimulator-arm64`, `ios-arm64`) | `EPOS.Kern`, `EPOS.UI` | **angelegt** (iU10-3…7) — 19 `.cs` (davon 12 Dienstadapter), **eigene `EPOS.iOS.sln`**, nicht in `WP-Plan.sln` und nicht im Filter (sonst NETSDK1147 auf ubuntu/windows). Simulator-Nachweis über CI-Job `ios.yml` **geführt** (Lauf 33748736894, 03.09.2026: Projekt 1030 **byte-gleich**), per Hand auszulösen |
| `EposSqliteMigrator.Kern` | Klassenbibliothek | `net10.0` | AnyCPU | — | **vorhanden** (seit `6486c36`); bleibt Windows-Werkzeug (liest `.accdb` über OleDb), nicht Teil des iOS-Pfads |
| `CSExeCOMServer` | — | — | — | — | ~~stilllegen (iU0)~~ — **erledigt** (`c3a8233`), aus dem Repo entfernt |
| `Werkzeuge/Formularkarte` (+ `.Tests`) | Konsole + xUnit | `net10.0` | AnyCPU | Roslyn | **neu** (iU8-12) — **eigene `.sln`**, seit dem Schritt „Formularkarte-Tests" in `kern.yml` auf `ubuntu-latest` mitgeprüft. **101 Tests, alle grün** seit iU8-12e (`4aa6b15`): die mit iZ5 gelöschte Maske liegt als eingefrorenes **Prüfmuster** unter `Formularkarte.Tests/Pruefmuster/Kosten/`, der Stapellauf hängt seit iU9-1 an der lebenden **und erreichbaren** `Form_KostenKomponente` |
| `Proben/ChartProben` | Konsole | `net10.0` | AnyCPU | `EPOS.Kern` | **neu** (iU7-3/iU7-6) — eigene `.sln`, `EnableWindowsTargeting=false`; zeichnet 9 Bilder und prüft Maße, Farben, Determinismus. Läuft in `kern.yml` auf ubuntu und macos |

Die beiden vorhandenen Rechenbibliotheken sind das Vorbild: `EPOS.Kern` ist dasselbe Muster, nur
größer. Der Zuschnitt ist damit im Haus zweimal erprobt und nicht neu zu erfinden.

### 2.2 Die Abhängigkeitsregel und ihre Absicherung

**Regel:** `EPOS.Kern` sieht weder `System.Windows.Forms` noch `System.Data.OleDb` noch
`System.Drawing.Common` noch `Microsoft.Win32.Registry` noch `System.Management` noch
`Microsoft.Office.Interop.*`. Das ist wörtlich die Abgrenzung, die `SpeicherEngine.csproj` und
`KiKern.csproj` im Kopfkommentar bereits tragen — nur wird sie dort von Hand eingehalten.

**Absicherung im Build (iE5):** Bei 569 umziehenden Dateien trägt kein Kommentar. Die Regel wird
maschinell durchgesetzt, und zwar zweifach:

1. `EPOS.Kern.csproj` setzt `<EnableWindowsTargeting>false</EnableWindowsTargeting>` und
   `net10.0` ohne `-windows`-Suffix. Jede Windows-only-API bricht dann **den Build**, nicht erst die
   Laufzeit auf dem iPad.
2. Zusätzlich baut die CI `EPOS.Kern` auf einem **macOS-Runner**. Was dort übersetzt, ist portabel;
   was nicht, fällt am Tag seiner Entstehung auf und nicht drei Monate später im Simulator.

Punkt 2 ist der eigentliche Wert der Mac-CI. Er wirkt ab iU4 und damit lange, bevor es eine iOS-App
gibt.

### 2.3 Dienst- und Adapterschnittstellen

Die Namen stammen aus Grundlagen § 6a.2 und A2/A3 und werden hier nur **verortet** — Aufgabe und
Begründung stehen dort.

| Schnittstelle | liegt in | Windows-Fassung | iOS-Fassung | Fundstellenmenge |
|---|---|---|---|---|
| `IDatenzugriff` | `EPOS.Kern` | SQLite (`SqliteDatenzugriff`, iU6-T4) | dieselbe Klasse | 160 + 60 + 35 Dateien |
| `IDialogDienst` | `EPOS.Kern` | `MessageBox`/`ShowDialog` | Blazor-Overlay | 127 / 115 / 149 |
| `IDateiDienst` | `EPOS.Kern` | Dateidialog, Explorer | Document-Picker, Share-Sheet | Importe, Berichtsausgabe |
| `ILizenzAblage` | `EPOS.Kern` | DPAPI | iOS-Keychain | 2 Dateien |
| `IGeraeteId` | `EPOS.Kern` | Registry-`MachineGuid` | `identifierForVendor` | 1 Datei |
| `IEinstellungen` | `EPOS.Kern` | Registry `HKCU\Software\wp-plan` | `Preferences` | 9 Dateien |
| `IPfade` | `EPOS.Kern` | `%APPDATA%`, `C:\ProgramData\EPOS_PLAN` | App-Sandbox | 12 Dateien |
| `IDrucken` / `ITeilen` | `EPOS.Kern` | Windows-Druck | AirPrint, Share-Sheet | Berichtsausgabe |
| `INavigation`, `IProjektKontext`, `ISprache` | `EPOS.Kern` | aus `Program.*` | Blazor-Navigation | 40 Dateien |

**Kein Adapter ist optional.** Fehlt einer, wandert die Plattformbindung als `#if`-Zweig in den Kern
zurück — das ist genau die Doppelpflege, die Modell C abschafft.

### 2.4 Die drei Hüllen

```
                        EPOS.Kern  (Rechenkern · Simulation · Wirtschaftlichkeit · DbWerte
                                    IDatenzugriff + SqliteDatenzugriff · Dienstschnittstellen
                                    ChartRenderer · Berichtsausgabe)
                             ▲                         ▲
              ┌──────────────┘                         └──────────────┐
              │                                                       │
      EPOS.Referenzlauf                                       EPOS.UI (Blazor)
      Konsole, headless                                        ▲            ▲
      Windows + macOS + CI                                     │            │
                                                    Windows-Hülle      iOS-Hülle
                                                    WinForms +         MAUI + BlazorWebView
                                                    BlazorWebView      Navigation nach iL5
                                                    (Altdialoge        iOS-Adapter
                                                     laufen weiter)
```

Die **dritte Hülle ist neu** gegenüber dem Grundlagenkonzept und trägt den ganzen Beweis: ein
headless-Konsolenrunner ohne WinForms-Referenz, der auf Windows **und** macOS **und** in der CI
läuft. Ohne ihn gibt es keinen plattformübergreifenden Wertgleichheitsnachweis (→ iE8, iU4).

### 2.5 Rückbau

Was im Zuge der Umstellung **stirbt** statt mitzuwandern — die Umwandlung ist der Moment dafür
(Grundlagen K6, M1, M4):

| Gegenstand | Menge | Paket |
|---|---|---|
| `CSExeCOMServer` | 1 Projekt, .NET Framework 4.0 | iU0 |
| dokumentierte tote Enden (`FormMain`-Altzweig, `Form_Wirtschaftlichkeit`-Hülle, `Form_AlsVariante`, „- Kopie"-Dateien) | ~10–15 Views | **iU9-W0 erledigt** (iF29): 25 Dateien gelöscht; `FormMain` und `Form_AlsVariante` bleiben nach Anwenderentscheid |
| ~~`DataRepository.ProviderVorhanden()`~~ | ersetzt durch `DatenbankVorhanden()` | ✔ erledigt (`6486c36`) |
| ~~Access-Zweig der `SchemaMigration`~~ | eingefroren, SQLite-Zweig `SCHRITTE_SQLITE` daneben | ✔ erledigt (`6486c36`) |
| ~~3 Views ohne Anwendungszugriff, 5 Phantom-Abfragen~~ | im Zielschema nicht mehr enthalten (14 Sichten) | ✔ erledigt (`6486c36`) |
| `WindowsFormsApplication1.csproj.netfx-backup` | 90 KB Altdatei | iU0 |
| `.github/copilot-instructions.md` (themenfremder Azure-Vorlagenrest) | 4 Zeilen | iU1 |

---

## 3 Entwicklungsumgebung und Build-Infrastruktur

*Dies ist das Kapitel zum ausdrücklichen Auftragsbestandteil „die Entwicklungsumgebung soll angepasst
werden". Es ist kein Anhang zur Portierung, sondern ihre Vorbedingung: Solange das Hauptprojekt nur
im Visual-Studio-MSBuild baut (§ 1.1), ist weder ein Mac-Build noch eine CI noch der
Wertgleichheitsnachweis möglich.*

### 3.1 Der Umbau in einem Satz

Aus **einer** Umgebung (ein Windows-Rechner, Visual Studio, Doppelklick auf `GitHub_Sync.bat`,
manueller Testlauf) werden **drei**: ein Windows-Arbeitsplatz, ein Mac-Arbeitsplatz und eine CI, die
beide bedient — verbunden durch die eine Regel, dass alles außer den beiden Hüllen mit `dotnet build`
baut.

### 3.2 Windows-Arbeitsplatz

| Baustein | heute | künftig | Anmerkung |
|---|---|---|---|
| Visual Studio | 2022 (17.14) | **2026 — zwingend** | VS 2022 kann das .NET-10-SDK zwar laden (ab 17.14), aber **nur .NET 9 und niedriger targeten**. Für `net10.0-windows` führt kein Weg an VS 2026 vorbei. **Kostenfolge:** eine VS-2022-Pro-Standalone-Lizenz deckt VS 2026 nicht ab — es ist eine neue Lizenz nötig (→ iF18) |
| Workloads | .NET-Desktop | + **.NET MAUI**, + ASP.NET (für Blazor-Werkzeuge) | Der Windows-Rechner baut die MAUI-**Windows**-Ziele und redigiert Blazor-Komponenten; iOS-Ziele nicht |
| .NET SDK | 9.0.315 (unfestgeschrieben) | **10.0.x, festgeschrieben in `global.json`** | Heute ist die SDK-Version nirgends fixiert — auf einem zweiten Rechner baut also potenziell etwas anderes |
| WebView2-Laufzeit | nicht gefordert | **Voraussetzung** (auf Windows 11 vorhanden, im Installer prüfen) | trägt `BlazorWebView` in der WinForms-Hülle (M9) |
| Access Database Engine | x64-Redist erforderlich | **entfällt — seit `6486c36` erledigt** | `Microsoft.Data.Sqlite` bringt die native Bibliothek mit. Damit ist auch der offene Punkt (d) der x64-Umstellung (Beschaffung `AccessDatabaseEngine_X64.exe`) gegenstandslos |
| SQLite-Werkzeug | — | **SQLiteStudio/Letos 4.0.3** oder DBeaver | Ersatz für den Access-Direktzugriff (M3a). Der dokumentierte Rückschritt ist konkret: SQLiteStudio 3.4 hat **keinen QBE-Abfrageentwurf** und **kein ER-Diagramm**; Letos 4.0.3 bringt einen ERD-Editor mit |

#### 3.2.1 Umstieg auf Visual Studio 2026

Der Wechsel ist unkritisch, weil er kein Wechsel sein muss: **VS 2026 installiert sich neben
VS 2022**, in eigenem Verzeichnis, beide laufen parallel. Das ist der empfohlene Weg — VS 2022
bleibt als Rückfallebene stehen, bis der .NET-10-Sprung (iU1) durch die Referenzläufe abgenommen ist.

| Schritt | Anmerkung |
|---|---|
| 1. VS 2026 herunterladen und **parallel** installieren | keine Deinstallation von VS 2022 nötig |
| 2. Im Installer die vorhandene VS-2022-Installation übernehmen lassen | Der Installer erkennt sie und baut Workloads, Toolsets, SDKs, Erweiterungen und Einstellungen nach. Rechne mit 30–90 Minuten |
| 3. Workloads prüfen: **.NET-Desktop** (vorhanden) + **.NET MAUI** (neu, für iU2 ff.) + ASP.NET (Blazor-Werkzeuge, für iU8) | MAUI kann auch später nachinstalliert werden |
| 4. Erweiterungen kontrollieren — für dieses Projekt vor allem **ResXManager** (`ResXManager.config.xml` liegt im Repo) | VS 2026 ist erstmals **rückwärtskompatibel** zu VS-2022-Erweiterungen; die meisten laufen unverändert |
| 5. Buildpfade nachziehen | Alt: `C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe` · **Neu: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`** — VS 2026 nutzt die interne Hauptversion `18` als Verzeichnisnamen und liegt unter `Program Files`, nicht `(x86)`. Betroffen: `Setup/build-setup.ps1` (sucht unter `%ProgramFiles%\Microsoft Visual Studio\2022\<Edition>`), `Referenzlaeufe/LIESMICH.md`, `WindowsFormsApplication1/CLAUDE.md`, `Referenzlauf.csproj`. Mit iE3 (COM-Referenzen raus) entfällt die Bindung an das VS-MSBuild ohnehin — bis dahin sind die Pfade zu pflegen |
| 6. Erst danach Schritt 6 aus iU1 (Hauptprojekt auf `net10.0-windows`) | vorher lässt sich `net10.0` gar nicht targeten |

**Zur Lizenz — Community 2026 ist der vorgesehene Weg.** Der heutige Buildpfad zeigt auf
`2022\Community`, es läuft also bereits die kostenfreie Edition. **VS 2026 gibt es ebenfalls als
Community**, mit gegenüber 2022 unveränderten Bedingungen. Der Versionssprung ändert an der
Rechtslage damit nichts; er ist nur der Anlass, die Einordnung einmal festzuhalten:

| Edition | Bedingung |
|---|---|
| **Community 2026** | In einer Organisation bis zu **5 Nutzer**, sofern es **keine „Enterprise"-Organisation** ist. Als Enterprise gilt: **mehr als 250 PCs/Nutzer oder mehr als 1 Mio. USD Jahresumsatz**. Unterhalb dieser Schwellen ist auch kommerzielle Entwicklung gedeckt |
| **Professional 2026** | nötig, sobald eine Schwelle überschritten wird oder mehr als 5 Entwickler arbeiten. Eine gekaufte **VS-2022-Dauerlizenz gilt nicht** für 2026 — dann Abonnement oder neue Standalone-Lizenz |

Bleibt INEKON unter den Schwellen, ist der Umstieg **kostenneutral** (→ iF18).

**Ablauf für Community 2026 im Einzelnen:**

| # | Schritt | Anmerkung |
|---|---|---|
| 1 | Auf `visualstudio.microsoft.com/downloads` **Community 2026** wählen, Web-Installer herunterladen | kostenfrei, kein Produktschlüssel |
| 2 | Installer starten — er erkennt die vorhandene VS-2022-Installation und bietet an, **Workloads und Einstellungen zu übernehmen** | annehmen; spart die manuelle Auswahl |
| 3 | **Nur `.NET-Desktopentwicklung`** auswählen — die beiden anderen Workloads erst, wenn ihre Etappe ansteht (Tabelle unten) | jede Workload kostet mehrere GB; nichts auf Vorrat installieren |
| 4 | Installieren — VS 2022 bleibt **unangetastet** daneben stehen | Dauer 30–90 Minuten je nach Workloads |
| 5 | Mit einem Microsoft-Konto anmelden | Community verlangt die Anmeldung nach 30 Tagen |
| 6 | **ResXManager** aus dem Marketplace nachinstallieren, falls Schritt 2 ihn nicht übernommen hat | VS 2026 ist rückwärtskompatibel zu VS-2022-Erweiterungen |
| 7 | Erste Gegenprobe **ohne** Frameworkwechsel: Solution in VS 2026 öffnen, `Debug|x64` bauen, Referenzlauf fahren | **332/332 byte-gleich** — beweist, dass allein der IDE-Wechsel nichts bewegt |
| 8 | MSBuild-Pfade nachziehen (Schritt 5 der Tabelle oben) | die 2026er-Installation liegt in einem anderen Verzeichnis |
| 9 | Erst danach iU1 Schritt 6: Projekte auf `net10.0` | vorher lässt sich `net10.0` nicht targeten |

Schritt 7 ist der eigentliche Wert dieser Reihenfolge: **IDE-Wechsel und Frameworkwechsel werden
getrennt nachgewiesen.** Bewegt sich danach ein Ergebnis, ist klar, welcher der beiden Schritte es
war.

**Die Workloads — und wann sie wirklich gebraucht werden:**

| Workload in der Oberfläche | Bezeichner | wofür | ab wann |
|---|---|---|---|
| **.NET-Desktopentwicklung** | `Microsoft.VisualStudio.Workload.ManagedDesktop` | WinForms — die Anwendung selbst | **sofort**, Pflicht für iU1 |
| .NET Multi-Platform App UI-Entwicklung | `Microsoft.VisualStudio.Workload.NetCrossPlat` | MAUI-Hülle | **iU10** — und nur zusammen mit dem Mac |
| ASP.NET und Webentwicklung | `Microsoft.VisualStudio.Workload.NetWeb` | Razor-Editor und IntelliSense für `.razor` | **iU8** — reiner Editorkomfort; eine Razor-Klassenbibliothek baut das SDK auch ohne |

**Nur die erste jetzt installieren.** Die beiden anderen sind mehrere GB groß und werden Monate
später gebraucht; sie lassen sich mit demselben Befehl jederzeit nachrüsten. Wer sie auf Vorrat
installiert, pflegt sie ohne Nutzen mit.

Wer die Auswahl reproduzierbar halten will (zweiter Arbeitsplatz, Neuaufsetzen), nimmt statt der
Oberfläche den Installer auf der Kommandozeile. **PowerShell als Administrator** — passend zur
übrigen Werkzeugkette des Hauses (`build-setup.ps1`, Referenzlauf-Anleitung):

```powershell
# 1. Ist-Stand: welche Installationen gibt es?
& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" `
    -all -prerelease -format value -property installationPath

# 2a. VS 2026 ist bereits installiert -> Workloads ergaenzen
#     ACHTUNG: Das Verzeichnis heisst 18 (interne Hauptversion), nicht 2026,
#     und liegt unter Program Files - nicht (x86) wie noch bei VS 2022.
#     Der Installer selbst bleibt unter Program Files (x86).
# --passive verlangt Administratorrechte VON ANFANG AN, sonst Exit Code 5007.
# Start-Process -Verb RunAs loest die UAC-Abfrage aus:
Start-Process -Verb RunAs `
    -FilePath "C:\Program Files (x86)\Microsoft Visual Studio\Installer\setup.exe" `
    -ArgumentList 'modify', `
        '--installPath','C:\Program Files\Microsoft Visual Studio\18\Community', `
        '--add','Microsoft.VisualStudio.Workload.ManagedDesktop', `
        '--includeRecommended','--passive','--norestart'

# Spaeter, wenn iU8 bzw. iU10 anstehen: derselbe Aufruf mit
#   '--add','Microsoft.VisualStudio.Workload.NetWeb'        (iU8)
#   '--add','Microsoft.VisualStudio.Workload.NetCrossPlat'  (iU10)

# 2b. VS 2026 fehlt noch -> Bootstrapper von visualstudio.microsoft.com/downloads,
#     ohne modify und ohne installPath (Datei muss vorher heruntergeladen sein)
Start-Process -Verb RunAs -FilePath "$HOME\Downloads\vs_community.exe" `
    -ArgumentList '--add','Microsoft.VisualStudio.Workload.ManagedDesktop', `
        '--includeRecommended','--passive','--norestart'

# 3. Kontrolle
dotnet --list-sdks      # 10.0.x muss erscheinen
dotnet workload list    # die MAUI-Workloads erscheinen
```

**Der Call-Operator `&` und der Backtick als Zeilenfortsetzung sind in PowerShell zwingend** — ein
Pfad in Anführungszeichen ohne `&` gilt dort als Zeichenkette, und `^` ist die
CMD-Fortsetzung und führt zu `Unerwartetes Token`.

### 3.3 Mac-Arbeitsplatz

Ohne ihn geht ab iU3 (Spike) nichts — das ist keine Frage der Bequemlichkeit, sondern der
Apple-Toolchain: Signierung, Simulator und `.ipa`-Erzeugung laufen ausschließlich auf macOS.

| Baustein | Anforderung | Anmerkung |
|---|---|---|
| Hardware | Mac mit Apple Silicon | zugleich das ARM64-Vergleichsziel für iT3 |
| macOS | die von der eingesetzten Xcode-Fassung geforderte Version | Xcode zieht die macOS-Mindestversion nach |
| Xcode | **genau die Version, die die installierte .NET-für-iOS-Workload verlangt** | Ein Mehr an Aktualität ist ein Fehler: Eine neuere Xcode-Installation lässt den Build ausdrücklich scheitern („requires Xcode X, the current version is Y"), bis die passende Workload nachgezogen ist. **Xcode-Aktualisierungen daher nie automatisch** (→ iR2) |
| .NET | SDK 10 + `dotnet workload install maui-ios` | dazu die Xcode Command Line Tools |
| Simulator | iPad-Simulator | genügt für iU3 und den größten Teil von iU10 |
| Test-iPads | mindestens zwei, verschiedene Größen | für M2 (Bedienbarkeit mit dem Finger) und den Feldtest |

**Pair-to-Mac oder direkt auf dem Mac?** Visual Studio auf Windows kann über einen erreichbaren Mac
für iOS bauen. Für die tägliche Arbeit an der iOS-Hülle empfehle ich dennoch, **direkt auf dem Mac**
zu entwickeln: Der Pair-to-Mac-Weg fügt eine Fehlerquelle hinzu, ohne eine zu beseitigen, und die
Mac-Hardware wird ohnehin gebraucht. Der Windows-Rechner bleibt der Ort für Kern, Datenschicht,
Windows-Hülle und die Blazor-Komponenten — also für den weitaus größten Teil der Arbeit.

**Kann der Mac-Kauf für den Spike aufgeschoben werden?** Ja, mit Einschränkung. Ein
`macos-latest`-Runner in der CI kann `EPOS.Kern` bauen und den headless-Referenzlauf ausführen — das
ist der eigentliche Inhalt von iU3. Was er nicht kann: interaktives Arbeiten im Simulator und
Signierung ohne eingerichtete Zertifikate. **Empfehlung:** iU3 auf einem Cloud-Runner fahren, den Mac
erst mit iU10 beschaffen (→ iF11). Das verschiebt eine vierstellige Investition hinter das Go/No-Go-Gate.

### 3.4 Apple-Konto, Signierung, Vertriebsweg

| Gegenstand | Festlegung | Anmerkung |
|---|---|---|
| Programm | **Apple Developer Program**, 99 €/Jahr | Das *Enterprise* Program ist faktisch kein Weg mehr: Apple genehmigt seit 2022 kaum noch neue Konten und verweist auf Apple Business Manager |
| Bundle-ID | z. B. `de.inekon.eposplan` | einmalig, danach unveränderlich |
| Zertifikate | Distribution-Zertifikat + Provisioning-Profile | im CI-Lauf in einen temporären Schlüsselbund importiert, nie im Repo |
| App Store Connect | API-Schlüssel für den automatisierten Upload | als CI-Secret |
| Windows-Signatur | `build-setup.ps1` hat `-Sign`/`-Thumbprint` **vorbereitet, aber nicht aktiv** | die iOS-Signierung ist der Anlass, beides gemeinsam scharf zu schalten |

**Zum Vertriebsweg (Präzisierung zu iF5).** Das Grundlagenkonzept empfiehlt „zunächst TestFlight".
Das ist als *Testweg* richtig und als *Auslieferungsweg* nicht tragfähig: **TestFlight-Builds
verfallen nach 90 Tagen.** Eine Software mit Jahreslizenz kann darüber nicht betrieben werden. Die
realistischen Wege für eine Fachanwendung an bekannte Firmenkunden:

| Weg | Eignung | Grenze |
|---|---|---|
| **TestFlight** | Feldtest, Vorabversionen | 90 Tage je Build; kein Dauerbetrieb |
| **Custom Apps** über Apple Business Manager | **der passende Weg für Firmenkunden** | Kunde braucht ABM-Konto |
| **Unlisted App** (nicht gelistet, Zugang per Link) | Auslieferung ohne öffentliche Sichtbarkeit | Apple-Review erforderlich |
| Öffentlicher App Store | breite Verfügbarkeit | Review, volle Regeln |
| Ad Hoc | Kleinsttest | max. 100 Geräte |

**Offene Frage mit Geldwirkung:** Die Lizenz wird heute über `epos-plan.de` (WordPress/WooCommerce,
Ed25519-Token) verkauft — außerhalb Apples. Ob Apple bei einer B2B-Fachanwendung mit Firmenlizenz auf
In-App-Kauf besteht, entscheidet über 15–30 % Provision auf jede Lizenz. Das ist **vor** iU13 zu
klären und nicht im Review (→ iF12). Der Custom-Apps-Weg entschärft die Frage, weil dort kein
Verkauf über den Store stattfindet.

### 3.5 Solution-Umbau

Die Reihenfolge ist nicht beliebig — jeder Schritt macht den nächsten prüfbar.

| # | Schritt | Ergebnis |
|---|---|---|
| 1 | `Directory.Build.props` + `Directory.Packages.props` + `global.json` anlegen | eine Stelle für SDK-, Sprach- und Paketversionen statt sieben |
| 2 | Alle Projekte auf **`net10.0`** anheben (Hüllen: `net10.0-windows`) | Support bis 11/2028; MAUI-Voraussetzung; Testprojekte nicht mehr eine Version voraus |
| 3 | `SpeicherEngine`, `KiKern` und beide Testprojekte in die CI nehmen | erste grüne Pipeline — sie bauen heute schon mit `dotnet` |
| 4 | `Referenzlauf` und `CSExeCOMServer` in der `.sln` bereinigen | `Referenzlauf` aufnehmen (er gehört dazu), `CSExeCOMServer` entfernen |
| 5 | **COM-Referenzen isolieren:** Excel-Interop-Nutzung (`ToolsClass`, `GanglinienDatei`) auf ClosedXML umstellen, `VBIDE` entfernen | **danach baut das Hauptprojekt mit `dotnet build`** — die Sperre aus § 1.1 fällt |
| 6 | Kodierung normalisieren: 68 Nicht-UTF-8-Dateien auf UTF-8 mit BOM (M10) | in **einem** Commit, ohne Inhaltsänderung, damit der Diff lesbar bleibt |
| 7 | `EPOS.Kern` anlegen und befüllen (iU4) | Kern baut auf Windows **und** macOS |

**Schritt 5 ist der Angelpunkt und zugleich billig.** Es geht um **zwei Dateien**
(`Allgemein/ToolsClass.cs`, `Allgemein/Import/GanglinienDatei.cs`), und das Zielpaket `ClosedXML
0.105.1` ist bereits im Projekt. Der Gewinn ist unverhältnismäßig groß: `dotnet build` funktioniert,
CI wird möglich, `build-setup.ps1` verliert seine MSB4803-Begründung, und die Excel-Importe laufen
ohne installiertes Office. Grundlagen-D2 hatte das als Portierungsarbeit eingeplant — es ist in
Wahrheit die **Eintrittskarte** und gehört deshalb nach vorne (iU1 statt iU7).

**Auf ClosedXML unter Nicht-Windows achten:** Ab 0.97 ist `System.Drawing.Common` entfallen,
Schriftmaße kommen aus `SixLabors.Fonts`. Auf Plattformen ohne die erwarteten Systemschriften ist die
Standardschrift explizit zu setzen (`LoadOptions.DefaultGraphicsEngine = new
DefaultGraphicEngine("…")`), sonst schlägt das Öffnen fehl. `SixLabors.Fonts` ist im Projekt bewusst
auf **1.0.1 gepinnt** (ab 2.x gilt die Six-Labors-Split-Lizenz) — dieser Pin bleibt.

**Namespace-Frage:** Der Root-Namespace ist `WindowsFormsApplication1`, der Ausgabename seit
29.08.2026 `EPOS_Plan`. Eine Umbenennung des Namespace beim Umzug wäre naheliegend, würde aber jeden
Umzugs-Commit unlesbar machen. **Empfehlung:** Der Umzug nach `EPOS.Kern` erfolgt **ohne**
Namespace-Änderung; die Umbenennung ist ein eigener, rein mechanischer Schritt danach (→ iF13).

### 3.6 Build-Matrix

Das Zielbild — und zugleich die Abnahmecheckliste des Kapitels:

| Projekt | VS-MSBuild (Win) | `dotnet` (Win) | `dotnet` (macOS) | Xcode-Kette |
|---|---|---|---|---|
| `EPOS.Kern` — **268 `.cs`** (168 aus iU4-5, +2 aus iU6-T4, +`ChartRenderer` aus iU7-5, +74 aus dem zweiten Umzug iU5-U, +`EnergietraegerVarianteCtrl` aus iU8-8b); `dotnet build` 0 Fehler, **3 Warnungen** (2 CS0108, 1 CA2255) — **CA1416 seit iU6 bei 0**, kein `System.Data.OleDb` mehr | ✅ | ✅ | ✅ | – |
| ~~`EPOS.Daten`~~ — **entfällt (iU6)**, die Zugriffsschicht liegt im Kern | – | – | – | – |
| `EPOS.UI` — Razor-Klassenbibliothek, `EnableWindowsTargeting=false` | ✅ | ✅ | ✅ | – |
| `EPOS.Kern.Tests` — **35 Tests** | ✅ | ✅ **testet** | ✅ **testet** | – |
| `EPOS.UI.Tests` — **64 bunit-Tests**, UI-Kultur `de-DE` gepinnt | ✅ | ✅ **testet** | ✅ **testet** | – |
| `EPOS.Referenzlauf` | ✅ | ✅ **läuft** | ✅ **läuft** | – |
| `Proben/ChartProben` — eigene `.sln`, 9 Bilder | – | ✅ **läuft** | ✅ **läuft** | – |
| `Werkzeuge/Formularkarte` (+ `.Tests`) — eigene `.sln`, **101 Tests** | ✅ | ✅ **testet** | ✅ **testet** | – |
| `SpeicherEngine`, `KiKern` (+ Tests) | ✅ | ✅ **testet** | ✅ **testet** | – |
| `WindowsFormsApplication1` — SDK `Microsoft.NET.Sdk.Razor` (iU8-6) | ✅ | ✅ *(seit `0ddc417`)* | ❌ | – |
| `Referenzlauf` | ✅ | ✅ *(seit `0ddc417`)* | ❌ | – |
| `EPOS.iOS` — 19 `.cs`, eigene `.sln`; Bau **nur** mit Workload-Set `10.0.400.1` und Xcode 26.6 | ❌ | ❌ | ✅ *(Simulator, seit iU10-6 im Job `ios.yml`)* | ✅ **signiert, `.ipa` — offen, iU13** |

Summe der Lösung am 03.09.2026 (`f95fc34`, Linux, SDK 10.0.400): `dotnet build WP-Plan.sln -c
Release -p:Platform=x64 --no-incremental` → **0 Fehler, 34 Warnungen**; `dotnet test
WP-Plan.Kern.slnf -c Release` → **886** (KiKern 450, SpeicherEngine 337, EPOS.UI 64, EPOS.Kern 35).
`WP-Plan.sln` führt **12** Projekte, `WP-Plan.Kern.slnf` **8**.

Die beiden ❌ in der macOS-Spalte sind gewollt und dauerhaft: WinForms läuft dort nicht. Alles
darüber ist der portable Teil — und er umfasst den gesamten Rechenkern.

**Empirisch bestätigt am 02.09.2026 auf einem Linux-Rechner** (Ubuntu 24.04, .NET SDK 10.0.400
über `dotnet-install.sh`, kein Visual Studio):

| Projekt | `dotnet build` | `dotnet test` |
|---|---|---|
| `SpeicherEngine`, `KiKern`, `EposSqliteMigrator.Kern` | ✅ | — |
| `SpeicherEngine.Tests` (`net9.0`, `DOTNET_ROLL_FORWARD=Major`) | ✅ | **337/337** |
| `KiKern.Tests` (`net9.0`, `DOTNET_ROLL_FORWARD=Major`) | ✅ | **450/450** |
| `WindowsFormsApplication1` und `Referenzlauf` (`-p:EnableWindowsTargeting=true`) | ✅ `dotnet build` auf Windows **und** auf Linux/macOS — **0 Fehler, seit `0ddc417`** (übersetzen, nicht ausführen). Vor iU1 stand hier ❌ **genau 2 × MSB4803**, die COM-Referenzen, sonst nichts | — |

Das ist der Beweis für § 1.1 in Zahlen: Der gesamte Bestand außer den zwei `COMReference`-Zeilen
war schon vor iU1 plattformfrei übersetzbar. **Mit iE3 (P1.1) und der vorgezogenen
Kodierungsnormalisierung (P1.12) kompiliert seit `0ddc417` auch das Hauptprojekt auf Linux und
macOS** — `dotnet build WP-Plan.sln -c Release -p:Platform=x64` übersetzt dort alle 7 Projekte
fehlerfrei, `dotnet test WP-Plan.Kern.slnf` meldet 787/787. Ausführen lässt sich die App dort
nicht (WinForms), aber jeder Übersetzungsfehler fällt ohne Windows-Rechner auf. Für die CI
(§ 3.7) hieß das: Der `kern.yml`-Lauf war sofort möglich, nicht erst nach iU4 — er läuft seit
`b4fd34d` grün auf ubuntu und macos.

### 3.7 Continuous Integration

Es gibt heute **keine**. Das ist bei einem Einzelplatz-Windows-Projekt vertretbar; bei zwei
Plattformen ist es der sichere Weg in eine unbemerkte Kerndrift (M8).

**Zuschnitt:** GitHub Actions, zwei Läufe.

| Workflow | Runner | Inhalt | Auslöser |
|---|---|---|---|
| `kern.yml` | `ubuntu-latest` **und** `macos-latest` | `dotnet build` + `dotnet test` über `WP-Plan.Kern.slnf` (Kern, UI, SpeicherEngine, KiKern und ihre Tests); seit iU7-7 die **ChartProben** mit den neun PNG als Artefakt; seit iU8-12 die **Formularkarte-Tests** (nur `ubuntu-latest` — das Werkzeug ist plattformfrei); Kern-Referenzlauf 1030/1007/1017 gegen die eingecheckte Testdatenbank und Vergleich gegen die Referenzbasis | jeder Push, jeder PR |
| `windows.yml` | `windows-latest` | vollständige Solution mit VS-MSBuild (bis Schritt 5 abgeschlossen ist), danach `dotnet`; Referenzlauf-Vergleich gegen die eingefrorene Basis | Push auf Hauptzweige, nächtlich |
| `ios.yml` | **`macos-26`** (nicht `-latest`: das Label wandert) | Workload-Set `10.0.400.1` + `DEVELOPER_DIR=/Applications/Xcode_26.6.app`; `EPOS.iOS` für `iossimulator-arm64` bauen (ohne Signatur), im Simulator starten, Startmarken prüfen (`EPOS.iOS bereit: Projekte=n`, `SQLite …`, `STRICT=114`), Prüflauf 1030 und Toleranzvergleich gegen `2026-08-30_B3-Kaskade`; Artefakte: Startprotokoll, CSV, Bildschirmabzug, `.app` | **nur `workflow_dispatch`** — kein Push-Auslöser (Anwenderregel 03.09.2026: macOS-Läufer nur nach Rückfrage; bis Migrationsende pauschal freigegeben). Simulator-Bau in **Debug** (Release lief 40 min in der Mono-AOT-Übersetzung) |

**Der ungelöste Punkt: die Testdatenbank.** `.gitignore` schließt `*.accdb` aus — „Änderungen an der
Datenbank landen nie in einem Commit". Eine CI ohne Datenbank kann den Kern nicht rechnen lassen.
Nach der SQLite-Umstellung ist der Weg offen: eine **kleine, anonymisierte `Kenndaten_Test.sqlite`
mit den 13 Referenzprojekten** wird versioniert (SQLite ist eine einzelne Datei, diffbar über ihr
Aufbauskript, und `sqlite-probe/EPOS_Beispiel.sqlite` ist der bereits akzeptierte Präzedenzfall).
Ohne sie bleibt die Kern-CI ein Kompilierungstest. **Vor iU3 zu entscheiden** (→ iF14).

**Secrets:** Apple-Zertifikat (Base64), Provisioning-Profil, App-Store-Connect-Schlüssel,
Windows-Signaturzertifikat. Kein Wert im Repo. Zur Kostenordnung: macOS-Runner werden bei privaten
Repositories mit Faktor 10 auf das Minutenkontingent angerechnet — die iOS-Läufe gehören deshalb an
Tags und manuelle Auslöser, nicht an jeden Push.

**Was die CI *nicht* ersetzt:** die manuelle Abnahme der x64-Umstellung, die laut
`Konzept_Umstellung_64Bit_EPOS-Plan.md` § 10 weiterhin aussteht (Funktionsdurchlauf, Start ohne ACE,
Setup-Testmatrix, Beschaffung von `AccessDatabaseEngine_X64.exe`). Diese Punkte laufen unabhängig
weiter — mit der Ausnahme, dass Punkt (d) mit der SQLite-Umstellung **ersatzlos entfällt**, weil kein
ACE-Redist mehr gebraucht wird.

### 3.8 Das Referenzlauf-Werkzeug plattformfähig machen

Die Suite ist das schärfste Werkzeug im Haus — 6 Modi, Kindprozess je Projekt gegen hängende Läufe,
`DialogWaechter` gegen Engine-MessageBoxen, Toleranz rel. 1e-4 / abs. 0,01, Schutzregel „die
produktive `Kenndaten.accdb` wird nie beschrieben" mit Pfadprüfung per Reflection in jedem
Kindprozess. Sie kann in dieser Form nicht mitwandern: `Referenzlauf.csproj` referenziert die
WinForms-App.

**`EPOS.Referenzlauf`** ist deshalb ein Neuschnitt derselben Logik gegen `EPOS.Kern` — ohne
`DialogWaechter` (im Kern gibt es keine MessageBoxen mehr, das ist der Sinn von `IDialogDienst`),
ohne WinForms, mit identischem CSV-Format und identischer Vergleichsregel. Die eingefrorenen Basen
bleiben damit gültig, und der Vergleich läuft plattformübergreifend.

**Die Regel aus `WindowsFormsApplication1/CLAUDE.md` gilt unverändert:** Das Werkzeug ist
Messinstrument und wird nie zusammen mit Engine-Änderungen umgebaut. `EPOS.Referenzlauf` entsteht
daher in iU4 als **eigener Commit vor** dem Kern-Umzug und wird gegen den unveränderten Bestand
kalibriert.

**Die Toleranzfrage auf ARM64.** Der x64-Umstieg hat den Präzedenzfall geliefert: Abweichungen
zwischen x86 und x64 wurden als Instruktionseffekt bestätigt und mit `DOTNET_EnableFMA=0`
nachgewiesen. Zwischen x64 und Apple Silicon ist dasselbe zu erwarten. „Wertgleich" braucht deshalb
für iOS eine ausdrückliche Definition — bitgleich wird es nicht sein (→ iF15). Vorschlag: **dieselbe
Toleranz wie heute** (rel. 1e-4 / abs. 0,01) für den Plattformvergleich, **Byte-Gleichheit** dagegen
weiterhin für den Windows-internen Umzugsnachweis (iT2), wo sich nichts ändern darf.

### 3.9 Werkzeuge und Arbeitsregeln

| Gegenstand | heute | künftig |
|---|---|---|
| Synchronisation | `GitHub_Sync.bat` (add/commit/pull/push, Merge statt Rebase, branchbezogen) | bleibt; **Tags werden nicht übertragen** — `git push --tags` bleibt Handarbeit (offener Punkt (e) der x64-Umstellung: `letzter-x86-stand` hängt weiterhin an einem Rechner) |
| Commit-Gate | keines | CI ist das Gate; kein Merge in den Hauptzweig ohne grünen Kern-Lauf |
| `.editorconfig` | kein globales `charset` (bewusst — eine BOM in den Referenz-CSV zerstört den Byte-Vergleich) | **unverändert lassen.** Die Regel ist load-bearing |
| Formular-Generator (A7) | ~~existiert nicht~~ **`Werkzeuge/Formularkarte`** (iU8-12) | Entwicklerwerkzeug mit **eigener `.sln`**, bewusst weder in `WP-Plan.sln` noch im Kernfilter (die `.csproj` der Anwendung sammelt `**\*.cs` ein — eine `.cs`-Datei unterhalb `WindowsFormsApplication1\` bricht den Build mit CS0102/CS0017). Aufruf `dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`, Stapellauf mit `--alle` |
| Lokalisierung | ResXManager, Drei-Schichten-Regel | **unverändert.** `MyResource.Resource.*` ist eine normale Klasse und läuft in Blazor auf beiden Plattformen; `DbWerte` bleibt eingefroren (M7) |
| Installer | Inno Setup 6.3, `build-setup.ps1` | bleibt für Windows. ~~`EPOS-Plan.iss:29` auf `EPOS_Plan.exe` korrigieren~~ ✔ erledigt (iU1-P1.10, `ce2dc9e`, samt Umstellung auf `dotnet publish`). **Neu seit iU8-10:** die WebView2-Laufzeit ist die **zweite** Voraussetzung neben ACE; der Online-Bootstrapper wird nur mitgenommen, wenn sie fehlt (→ iF20) |

### 3.10 Die Bausteine iE1–iE10

| Nr. | Baustein | Paket | Nachweis |
|---|---|---|---|
| ~~**iE1**~~ | `global.json`, `Directory.Build.props` (`Directory.Packages.props` noch offen) | iU1 | ✔ erledigt (`e0df744`) — SDK auf 10.0.400 gepinnt, `LangVersion` und `EnableWindowsTargeting` zentral |
| ~~**iE2**~~ | Alle Projekte auf .NET 10 | iU1 | ✔ erledigt (`577701c`, `a81fc1b`, `0ddc417`) — 7 Projekte, 0 Fehler; Referenzlauf-PASS steht als Windows-Nachweis noch aus |
| ~~**iE3**~~ | COM-Referenzen entfernen (2 Dateien auf ClosedXML) | iU1 | ✔ erledigt (`d4b72c8`) — **`dotnet build WP-Plan.sln` läuft durch**. `ToolsClass.ReadExcel` hatte keinen Aufrufer — **gelöscht statt portiert**; portiert wurde nur `GanglinienDatei` |
| ~~**iE4**~~ | GitHub Actions: `kern.yml` (ubuntu + macOS), `windows.yml` | iU1 | ✔ erledigt (`b4fd34d`) — `kern.yml` grün auf ubuntu und macos (787 Tests); `windows.yml` wartet auf den ersten Lauf |
| ~~**iE5**~~ | Portabilitätssperre: `net10.0` ohne `-windows`, macOS-Build in der CI | iU4 | ✔ erledigt (`b1a73af`) — `EnableWindowsTargeting=false` in `EPOS.Kern.csproj`, seit iU8-2 ebenso in `EPOS.UI.csproj`; der Plattform-Wächter meldet 0 Treffer |
| ~~**iE6**~~ | Testdatenbank für die CI (13 Referenzprojekte, SQLite) | iU3 | ✔ erledigt (`db66c95`, `e97f694`) — `Referenzlaeufe/Kenndaten_Test.sqlite`; der Kern-Referenzlauf rechnet in der CI 1030, 1007 und 1017 |
| **iE7** | Mac-Arbeitsplatz: Hardware, Xcode, `maui-ios`, Simulator | iU2 | Hallo-Welt-MAUI mit `EPOS.Kern`-Referenz im Simulator |
| ~~**iE8**~~ | `EPOS.Referenzlauf` — headless, plattformfrei | iU4 | ✔ erledigt (`db9f00f`) — derselbe Lauf auf Linux **und** arm64-macOS, Vergleich PASS und byte-gleich (`edefbef`) |
| **iE9** | Apple-Konto, Bundle-ID, Zertifikate, Signierkette in der CI | iU2 / iU13 | signiertes `.ipa` aus der CI |
| ~~**iE10**~~ | SQLite-Werkzeugkette, Betriebsersatz für Access | — | ✔ erledigt (`6486c36`, `BETRIEB_SQLITE.md`) |

**Abnahme des Kapitels (iZ2):** `dotnet build` baut die gesamte Solution auf Windows; `EPOS.Kern`
baut und testet zusätzlich auf macOS in der CI; eine MAUI-Hülle startet mit Kern-Referenz im
iPad-Simulator.

---

## 4 Arbeitspakete iU0–iU13

Größenklassen: **S** ≤ 3 PT · **M** 4–10 PT · **L** 11–25 PT · **XL** > 25 PT (mehrere Wellen).
Die XL-Pakete sind bewusst nicht durchgeschätzt — vor iU3 gibt es dafür keine belastbare Grundlage,
und das Grundlagenkonzept § 6 sagt dazu das Nötige.

### 4.0 Gesamtübersicht — Stand 03.09.2026

Der Stand aller Pakete auf einen Blick. **„Hier erreicht" heißt: auf Linux gebaut, getestet und
gegen die Referenzbasis gefahren** — die Spalte ganz rechts nennt, was dafür ein Windows braucht.

| Paket | Stand | Commits auf `ios_migration` | auf Windows offen |
|---|---|---|---|
| **iU0** Klärung, Sicherung, Rückbau | ✔ erledigt 02.09. | `c3a8233`, `1ab062d` | Anwender trägt Entscheide und Termine ein |
| **iU1** .NET 10, Windows, CI | ✔ erledigt 02.09. | `c3a8233`..`ce2dc9e`, P1.11 `0c83dba` | Referenzlauf **332/332** (iZ1), Proben 16/16, Excel-Import, Setup |
| **iU2** Mac und Apple-Konto | ⏳ nicht begonnen | — | alles (Hardware, Xcode, Konto) |
| **iU3** Machbarkeits-Spike | ✔ **bestanden** 02.09. (iZ3) | `13cedbb`..`db9f00f`, `edefbef`, `e3bd586` | — (auf Linux **und** arm64-macOS byte-gleich) |
| **iU4** `EPOS.Kern` herauslösen | ✔ hier erreicht 03.09. | `4a0a4e2`..`18f515f` | Vollreferenzlauf **332/332** (iZ4), VS 2026 öffnet 12 Projekte |
| **iU5** Statics kappen, Dienste | ✔ hier erreicht 03.09. (iZ5a) | `35be81f`..`c477523`; zweiter Umzug `a546af9`..`a9e5c16`, Doku `f95fc34` | Bedienprobe: Bericht, Katalogimport, Lizenzaktivierung, KI-Chat, 12 Gewerke, Sprachumschaltung |
| **iU6** Datenzugriff plattformfrei | ✔ hier erreicht 03.09. | `22fb7eb`..`300a354` | Erststart-Migration aus `.accdb`, Solar-/Pufferspeicherdialoge, die 36 `RecordSet`-Views |
| **iU7** Charts und Berichte | ✔ hier erreicht 03.09. | `c6b32eb`..`f84932b`, `6604c05`..`0759b37`, `0af6421` | `Referenzlauf.exe bildvergleich` alt/neu (Vorbedingung für iF23) |
| **iU8** `EPOS.UI`, erster Dialog | ✔ **iZ5 hier erreicht** 03.09. | A `8574911`..`8f5a28e`, `45a21dc`, `f5fb05c` · B `4369fdb`..`eafbc1f`, `eff82aa`, `e3d1e5b` · C `479fcf9`..`0af7ca7`, `4aa6b15` | Dialogabnahme (Maus/Finger, de/en, Hochkontrast, 125 %/150 %, Enter/Esc), Setup mit und ohne WebView2, VS-2026-Designer unter dem Razor-SDK |
| **iU9** Masken in Wellen | 🔄 W0 bis W13 umgesetzt, W14a/W14b in Arbeit | `01c9933` | **32** Designer-Masken offen (38 nach W12, 43 nach W11b, 49 nach W10b, 50 nach W10a, 55 nach W9, 63 nach W8, 73 nach W7, 81 nach W6, 88 nach W5, 91 nach W4, 98 nach W3, 102 nach W2, 105 nach W0); Stilllegung nach iF29 abgeschlossen, Sprungbrücke steht, `ChartRenderer` um Kostenprofil, Kennlinien und die drei Bedarfsbilder erweitert, seit W5 die erste **Seite** (`BlazorSeite`, Reiter „Berichte & Kosten"), seit W6–W9 **alle elf Kacheln des Startbilds** (sieben Erzeuger, vier Bedarfe) und **zehn der dreizehn Assistentenseiten** als Razor-Komponenten; `WPCtrl`, `BedarfStammCtrl`, `TypProfilCtrl`, `Ferienzeit`, die Projektlisten der Bedarfsgewerke und das Suchmuster im Kern; seit W10a die sieben Quell-, Senken- und Pufferdialoge der Simulationskonfiguration mit dem Baustein `Bildkarte` und dem zweireihigen `Jahresgang`, seit W10b die **Simulationskonfiguration als Seite** mit Kartenspalten, SVG-Schema (`SchemaModell`/`SchemaLayout` im Kern) und drei Überlagerungsebenen in einer WebView; seit W11a die Ergebnisrechnung der Detailansicht als DTOs im Kern (`SimulationErgebnisCtrl`), der **nebenläufige Simulationslauf** (`SimulationLaufCtrl`, `Do_Simulation` mit Fortschritt und Abbruch), sieben Ergebnisbilder im Renderer (30 Proben) und der Baustein `Fortschritt`; seit W11b die **Ergebnisseite der Simulation** (`SimulationErgebnisSeite`, zehn Blätter, Autarkie, Ganglinien-Navigatoren, Variantenvergleich als Überlagerung) — `Form_Simulation_Detail` mit 7 766 Zeilen ist gelöscht; seit W12 die **AP5-Importkette als ein Kern-Ablauf** (`GanglinienImportAblauf` mit zwölf bitgleichen Proben), `StromganglinieDialog`, `StromganglinieAdminDialog`, `PeakShavingDialog` (nebenläufig) und der gemeinsame `ImportKonflikteDialog`; seit W13 die **Katalog-Importe** — `KatalogImportDialog` mit vier Ausprägungen (`KatalogImportProfil`/`KatalogImportAblauf` im Kern, transaktional), `WaermebedarfAdminDialog`, `PvModulImportDialog` (CEC/PAN), die Mehrfachmarkierung im `Raster` und zwanzig eingefrorene Importproben; die `ImportKonflikteHuelle` und die Sprungbrücke `WaermebedarfExternAdmin` sind gefallen |
| **iU10**–**iU13** | ⏳ nicht begonnen | — | — |

**Die Reihenfolge auf dem Zweig ist nicht die Reihenfolge der Planung.** iU5 bis iU8 sind in
eigenen Worktrees entstanden und per Cherry-Pick übernommen worden; die SHAs sind dabei neu
vergeben worden. Auf `ios_migration` folgen nach `18f515f` (iU4-8): iU8-1…5/8a/5b → iU7-1…4 →
iU6 → iU8-5c, iU8-12 → iU7-5…8 → iU5-T0…T5 → iU8-6…13 → iU5-U1…U5 mit iU7-9. Die in den
Statusblöcken genannten Basis-SHAs sind die **Entwicklungsbasen**, nicht die Elternteile auf dem
Zweig. Für die Nachweise ist das ohne Belang: Jede Tranche ist einzeln gebaut, getestet und
gegen `2026-08-30_B3-Kaskade` gefahren worden.

**Die drei Zahlen, an denen die ganze Kette hängt** (Linux, SDK 10.0.400, Stand `f95fc34`):

| Messung | Wert | Verlauf |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | **0 Fehler, 34 Warnungen** | 123 (iU4/iU5) → 36 (iU6, die 87 CA1416 fallen weg) → 34 (iU8-9, zwei WFO1000 mit dem gelöschten Formular) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **886** | 787 (Stand iU1) + 9 (iU4-6) + 9 (iU6-T4) + 14 (`DiensteTests`, iU5-T0…T5) + 3 (Renderer, iU7-8) + 64 (`EPOS.UI.Tests`, iU8-5…8a) = **886**. Die Zwischensummen der Statusblöcke (796, 805, 810, 872) sind die Messungen der jeweiligen Entwicklungsbasis |
| Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` | **GESAMT PASS, byte-gleich** | nach **jeder** Tranche, 815 043 Werte |

### iU0 — Klärung, Sicherung, Rückbau · S · Windows

**Voraussetzung:** keine. **Zuordnung:** Vorstufe zu allem.

| Inhalt | Detail |
|---|---|
| Entscheidungen bestätigen | iF1 (Spike), iF3 (Blazor Hybrid), iF7 (Generator), **iF8 (Modell C)**, **iF9 (SQLite auf Windows)** — iU4 ff. setzen sie voraus. **Stand 03.09.2026:** iF9, iF14, iF15, iF17, iF18 und iF19 sind beschieden; iF3, iF7, iF8 und iF16 sind mit iU8 **umgesetzt, förmlich aber noch nicht beschieden** (Entscheidungsregister § 1) |
| Neue Entscheidungen einholen | iF10–iF16 (§ 8) |
| Referenzbasis einfrieren | `2026-08-30_B3-Kaskade` als Bezugspunkt aller Umzugsnachweise festschreiben |
| Chart- und Grid-Masken auszählen | ✔ erledigt (`1ab062d`): **18 Chart-Masken (32 Steuerelemente), 19 Grid-Masken (22 Steuerelemente)** im Build — Aufwandsgrundlage für iU9, Einzeldurchsicht im Entscheidungsregister § 3 (§ 1.5) |
| Rückbau | ✔ erledigt (`c3a8233`) — `CSExeCOMServer` und `WindowsFormsApplication1.csproj.netfx-backup` sind aus dem Repo; die Historie bleibt über den Commit `922228a` erreichbar |
| Offene x64-Punkte | (a)–(c) und (e) aus `Konzept_Umstellung_64Bit_EPOS-Plan.md` § 10 terminieren; (d) entfällt mit SQLite |

**Abnahme:** Entscheidungsregister vollständig, keine offene Vorbedingung für iU1.

### iU1 — Entwicklungsumgebung Stufe 1: .NET 10, Windows und CI · M · Windows

> **Umgesetzt 02.09.2026 auf Branch `ios_migration`, Commits `c3a8233`..`0ddc417`,
> P1.8 `dab063a`, P1.10 `ce2dc9e`, P1.11 `0c83dba`; Nachweis hier geführt, CI Kern + Windows grün,
> Nachweis Windows-Ausführung offen (iZ1).**
> Die Abnahmeliste je Commit steht in
> [`Umsetzung_iU0_iU1_Nachweise.md`](Umsetzung_iU0_iU1_Nachweise.md).

**Voraussetzung:** iU0. **Bausteine:** iE1, iE2, iE3, iE4. **Grundlagen:** A1 (teilweise), D2.
**Frist:** vor dem 10.11.2026 (§ 1.3).

**Ausgangslage (gemessen 02.09.2026):** sechs Projekte auf `net8.0`/`net8.0-windows`, zwei
Testprojekte bereits auf `net9.0`, `CSExeCOMServer` auf .NET Framework 4.0. Keine zentrale
Versionssteuerung.

**Schrittfolge — jeder Schritt einzeln nachweisbar:**

| # | Schritt | Nachweis |
|---|---|---|
| 1 | **Visual Studio 2026** installieren (parallel zu 2022 möglich), .NET-10-SDK | `dotnet --list-sdks` zeigt 10.0.x |
| 2 | `global.json` mit gepinnter SDK-Version, `Directory.Build.props` (LangVersion, Nullable, gemeinsame Eigenschaften), `Directory.Packages.props` (zentrale Paketversionen) | zwei Rechner bauen nachweislich dasselbe |
| 3 | **`UseWPF` entfernen** — siehe Befund unten | Build unverändert grün |
| 4 | Bibliotheken zuerst anheben: `SpeicherEngine`, `KiKern`, `EposSqliteMigrator.Kern`, `EposSqliteMigrator` (Konsole) auf `net10.0` | `dotnet build` grün, Tests grün |
| 5 | Testprojekte auf `net10.0` (heute `net9.0`) | `dotnet test` grün |
| 6 | Hauptprojekt, `Referenzlauf` und `ZugriffsschichtProben` auf `net10.0-windows` | Solution baut in VS 2026 |
| 7 | **Pakete nachziehen — siehe eigene Tabelle unten** | `dotnet list package --outdated` sauber, Proben 16/16 |
| 8 | **Referenzlauf** gegen die eingefrorene Basis | **332/332 byte-gleich** — der Frameworkwechsel darf kein Ergebnis bewegen |
| 9 | COM-Referenzen entfernen (iE3, zwei Dateien auf ClosedXML), `VBIDE` löschen | **`dotnet build WP-Plan.sln` läuft durch** |
| 10 | CI aufsetzen: `kern.yml` (ubuntu + macOS), `windows.yml` | erste grüne Läufe |
| 11 | Setup nachziehen: `EPOS-Plan.iss:29` auf `EPOS_Plan.exe`; `build-setup.ps1` von VS-MSBuild auf `dotnet publish` umstellen, sobald 9 erledigt ist | Setup baut wieder |

**Die Pakete, die mit dem Framework mitziehen müssen (Schritt 7):**

| Paket | heute | Ziel | Fundstellen |
|---|---|---|---|
| **`Microsoft.Data.Sqlite`** | **8.0.11** | **10.x** | `WindowsFormsApplication1.csproj:117` **und** `EposSqliteMigrator/Kern/EposSqliteMigrator.Kern.csproj:19` — beide anheben, sonst laufen zwei Fassungen der Datenschicht nebeneinander |
| `System.Data.OleDb` | 8.0.1 | 10.x | Hauptprojekt, Migrator-Kern (nur noch Datenträgertyp bzw. `.accdb`-Lesen) |
| `System.Configuration.ConfigurationManager` | 8.0.1 | 10.x | Hauptprojekt |
| `Microsoft.Extensions.Http` / `.Logging` | bereits 10.0.3 | — | schon auf .NET-10-Stand |
| `SixLabors.Fonts` | 1.0.1 | **bleibt gepinnt** | Lizenzgrund (ab 2.x Six-Labors-Split-Lizenz) |

`SQLitePCLRaw` steht in keiner Projektdatei — es kommt **transitiv** über
`Microsoft.Data.Sqlite` und zieht standardmäßig `bundle_e_sqlite3`. Für Windows ist das richtig;
für iOS ist dort später `bundle_green` zu erzwingen (iU6). Der Sprung auf 10.x ändert daran nichts,
macht die Frage aber sichtbar, weil das Bundle dann ebenfalls eine neue Hauptversion trägt.

**Empfehlung:** Beide `Microsoft.Data.Sqlite`-Fundstellen über `Directory.Packages.props`
(Schritt 2) zentral führen. Dann ist die Version künftig an **einer** Stelle gepflegt — heute steht
sie zweimal im Repo und kann auseinanderlaufen.

**Nachweis für dieses Paket:** Die Datenschicht hat mit `6486c36` eine eigene Probensuite bekommen
(`Proben/ZugriffsschichtProben`, 16 Proben). Sie ist nach dem Versionssprung erneut zu fahren —
zusammen mit dem Referenzlauf ist das der Beleg, dass die neue Sqlite-Fassung sich identisch verhält.

**Der projektspezifische Fallstrick — und er ist entschärfbar:** `.NET 10` macht
`System.Windows.Forms.ContextMenu` und `System.Windows.Controls.ContextMenu` mehrdeutig, was einen
Compilerfehler erzeugt — **aber nur, wenn beide Welten im Projekt aktiv sind**. Genau das ist hier
formal der Fall (`UseWindowsForms=true` **und** `UseWPF=true`). Die Messung zeigt jedoch:

| Prüfung | Ergebnis |
|---|---|
| XAML-Dateien im Projekt | **0** |
| Dateien mit `System.Windows.Media/Controls/Data/Documents/Shapes/Threading` | **0** |
| Alle `ContextMenu`-Fundstellen | Variablennamen vom Typ `ToolStripMenuItem`, kein WPF-Typ |

**`UseWPF=true` ist ein Relikt ohne jede Nutzung.** Es zu entfernen (Schritt 3) beseitigt das
Hauptrisiko des Sprungs, verkleinert die Ausgabe und ist für sich genommen risikoarm — die
Referenzläufe weisen es nach.

**Weitere Punkte, die zu beobachten sind:** WinForms-Obsoletions in .NET 10 erzeugen
Compilerwarnungen mit eigenen Diagnose-IDs (keine Fehler); die `NoWarn`-Liste ist bei der Gelegenheit
zu durchforsten. Die DPI-Vorgabe bleibt unverändert `DpiUnaware` (`app.manifest`) — daran wird
**nicht** gerührt, das wäre ein eigenes Vorhaben mit Layoutwirkung auf 204 Masken.
`SixLabors.Fonts` bleibt auf **1.0.1 gepinnt** (ab 2.x gilt die Six-Labors-Split-Lizenz).

**Zwei Befunde aus der Umsetzung, die diese Planung nicht kannte:**

1. **Die Kodierungsnormalisierung musste vor den Frameworksprung.** M10 (68 nicht-UTF-8-Dateien)
   war für iU4 vorgesehen. Sobald die COM-Referenzen weg sind (P1.1), übersetzt `csc` das
   Hauptprojekt aber auch auf Linux und macOS — und stolpert dort über **14 der 68 Dateien**, die
   seine 1252-Rückfallkodierung nicht abfängt (252 × `CS1056`/`CS1002`). Unter Windows-MSBuild
   tritt das nicht auf. Ohne die Normalisierung wären weder der hiesige Nachweis für Schritt 6 noch
   der `kern.yml`-Lauf für das Hauptprojekt führbar gewesen. **Vorgezogen und als P1.12 erledigt**
   (`3ba7d54`, reine Umkodierung, kein Inhalt); die `.editorconfig` schreibt `utf-8-bom` für `*.cs`
   fest.
2. **`WFO1000` ist in .NET 10 ein Fehler, nicht nur eine Warnung.** Die Planung erwartete unter
   „Weitere Punkte" nur Obsoletions-*Warnungen*. Die WinForms-Analyse zur
   Designer-Serialisierung hat seit .NET 9 die Standardschwere `error` und bricht den Bestandsbau
   an **60 Fundstellen** (Schwerpunkt `Form_Gesetzesparameter`, `Form_Kosten_VarAuswahl`, die
   Karten-Controls des Kostenmoduls). In `0ddc417` per `.editorconfig` auf `warning` **herabgestuft
   und sichtbar gelassen**; die Annotation je Property ist Fachentscheidung und **gehört zu iU9**.

**Abnahme (iZ1):** `dotnet build WP-Plan.sln` und `dotnet test` laufen auf einem Rechner ohne Visual
Studio durch; Referenzlauf **332/332 byte-gleich**.

> **Dieses Paket ist auch ohne iOS-Beschluss vollständig gerechtfertigt.** Es beseitigt die
> Support-Frist, schafft die erste Testautomatik der Projektgeschichte und macht den Setup-Bau
> wieder lauffähig.

### iU2 — Entwicklungsumgebung Stufe 2: Mac und Apple-Konto · S–M · Mac

**Voraussetzung:** iU1. **Bausteine:** iE7, iE9 (Teil).

| Inhalt | Detail |
|---|---|
| Hardware | Mac mit Apple Silicon, zwei Test-iPads — **oder** zunächst nur `macos-latest`-Runner (→ iF11) |
| Toolchain | macOS, Xcode in der von der Workload geforderten Version, Command Line Tools, .NET SDK 10, `dotnet workload install maui-ios` |
| Apple Developer Program | Konto, Bundle-ID `de.inekon.eposplan`, Zertifikate, Provisioning |
| Probelauf | leere MAUI-App mit `ProjectReference` auf `SpeicherEngine` (stellvertretend für `EPOS.Kern`) im iPad-Simulator |

**Abnahme:** Eine MAUI-App mit einer eingebundenen Rechenbibliothek des Hauses startet im Simulator
und liefert ein gerechnetes Ergebnis auf den Bildschirm.

### iU3 — Machbarkeits-Spike · M · Mac/CI

> **Status 02.09.2026 — bestanden.** Umgesetzt als `EPOS.Kern` (91 verlinkte Dateien, `net10.0`
> ohne WinForms) + `EPOS.Referenzlauf`, Commits `13cedbb`…`db9f00f`. Projekt 1030 auf Linux x64
> gegen `2026-08-30_B3-Kaskade`: **PASS, 22 Dateien byte-identisch.** Vorbedingungen, die das
> Konzept nicht kannte: `DbParam` statt `OleDbParameter` (Test B) und der Kern in einer Assembly
> ohne `UseWindowsForms` (Test A) — beides erledigt. Der Spike lief ohne Mac; die ARM64-Frage (iF15)
> hat der `macos-latest`-Schritt der CI beantwortet: **auf Apple Silicon (`macos-26-arm64`) ebenfalls PASS
> und byte-gleich** (Lauf `edefbef`). Einzelheiten: Entscheidungsregister § 2.2/2.3.

**Voraussetzung:** iU1, iU2. **Entspricht Grundlagen-S0.** **Baustein:** iE6.

Das ist der Beweis, für den das ganze Vorhaben vorne klein gehalten wird.

| Inhalt | Detail |
|---|---|
| Datenstand | **liegt vor** — `Kenndaten.sqlite` als Werksvorgabe, `EposSqliteMigrator` für Kundenbestände. Daraus die CI-Testdatenbank mit den 13 Referenzprojekten schneiden |
| Kernauszug | **nur** `BhkwPlan`, `SimulationControl` und deren zwingende Abhängigkeiten — als Wegwerf-Auszug, nicht als `EPOS.Kern`. Ziel ist Erkenntnis, nicht Bestand |
| Rechenlauf | Projekt 1030 headless im iPad-Simulator **und** auf `macos-latest` |
| Vergleich | Ergebnis-CSV gegen `2026-08-30_B3-Kaskade/Projekt_1030` |

**Abnahme (iZ3 — Go/No-Go):** Werte innerhalb der Toleranz aus § 3.8. Bei Abweichungen darüber:
Ursachenanalyse nach dem Muster des x64-Umstiegs (`DOTNET_EnableFMA=0`). Ergibt sich eine nicht
erklärbare Abweichung, ist das der **begründete Abbruch für kleines Geld** — genau der Zweck dieses
Pakets.

### iU4 — `EPOS.Kern` herauslösen · L · Windows

> **Status 03.09.2026 — hier erreicht, Windows-Nachweis offen.** Sieben Commits
> (`4a0a4e2` iU4-1 … `616dff4` iU4-7) auf `origin/ios_migration` = `9fe9c71`.
>
> **Umfang:** `EPOS.Kern` führt **168 Dateien**, die seit iU4-5 physisch unter
> `EPOS.Kern/` liegen (`git mv`, Ordnerstruktur `Allgemein/…`, `Controller/`, `Model/`,
> `MyResource/`, `Properties/` erhalten). Die Anwendung übersetzt sie nicht mehr, sondern
> referenziert das Projekt; sie schrumpft von 585 auf 417 `.cs`. Der Weg dorthin ging
> über iU4-4: Erst wurde der volle Umfang **verlinkt** und übersetzt — der
> Portabilitätsbeweis vor dem Umzug, Wächter `EnableWindowsTargeting=false`.
>
> **Gekappte Kanten (iU4-1 bis iU4-3):** `Sprache` und `ZahlText` lösen die letzten
> `Program`-Statics des Kernpfads ab (`Program` leitet weiter); acht Schema-Konstanten
> ziehen von `SchemaMigration` nach `SchemaStand`; `KomponentenUebernahmeCtrl` ruft
> `AnlagenSql` direkt statt über `WizardCtrl`; sieben `MessageBox.Show` werden zu
> `Meldung.*`; der Geräte-Aufräumlauf läuft über den Haken
> `WErzeugerCtrl.GeraetewaisenAufraeumen`. Die beiden `partial`-Aufteilungen über die
> künftige Assemblygrenze hinweg sind aufgelöst: `WizardItemClass` bekommt mit
> `WizardSeite` einen abgeleiteten Oberflächentyp, die `FillComboBox`-Hälften werden
> Erweiterungsmethoden in `ControllerListen`. Fünf `*Ctrl.WinForms.cs` entfallen
> ersatzlos (0 Aufrufer).
>
> **`InternalsVisibleTo`** auf `EPOS_Plan` und `EPOS.Kern.Tests` — etliche Controller und
> Modelle sind ohne Zugriffsangabe deklariert und damit `internal`; die Alternative wäre
> eine breite Sichtbarkeitsänderung am Bestand ohne fachlichen Grund gewesen.
>
> **Nachweis hier:** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler,
> **123 Warnungen** (EPOS.Kern 89, App 34) — dieselbe Summe wie vor der Etappe.
> `dotnet test WP-Plan.Kern.slnf` → **796** (787 + 9 neue). Referenzlauf **1030, 1007 und
> 1017** gegen `2026-08-30_B3-Kaskade`: **GESAMT PASS, alle drei byte-gleich.**
>
> **Offen nach iU4:** `Program.*`-Statics und `MessageBox` in `Views/` (iU5), `IDatenzugriff`
> (iU6), die AUSGABE-Hälfte des Berichts samt `ChartRenderer` (iU7), `EPOS.UI` (iU8), die
> Maskenwellen mit den 432 `OleDbParameter`-Altaufrufen und WFO1000 (iU9), `IEinstellungen`
> und `ILizenzAblage` (iU11). In der Anwendung bleiben mit Absicht: `SchemaMigration` samt
> Access-Zweig, `SchemaModell`, `GeraeteWaisen`, `ErststartMigration`,
> `AnlagenEindeutigkeit`, `Katalog/`, `Import/` (außer `AnsiEncoding`), `WizardCtrl`, die
> `*KontextMenuCtrl`, `MenueCtrl`, die Stamm-Controller mit `MessageBox`, `KI/`, `Hilfe/`,
> `GrafikTools/`, `Export/`, `Lizenz/` und `WPCtrl`.

**Voraussetzung:** iU3 bestanden. **Entspricht Grundlagen-S1**, Block A1, M10. **Bausteine:** iE5, iE8.

| Inhalt | Detail |
|---|---|
| `EPOS.Referenzlauf` **zuerst** | headless-Runner gegen den unveränderten Bestand kalibrieren — das Messinstrument entsteht vor dem Umbau (§ 3.8) |
| Umzug | **umgesetzt mit 168 Dateien**: Modelle (46), Rechenkern (`BhkwPlan.cs`), Simulation (25 von 26 — ohne `SchemaModell.cs`), Wirtschaftlichkeit (20), `DbWerte.cs`, die DATEN-Hälfte des Berichts (7 statt 18 — die Ausgabe folgt mit iU7), Zugriffsschicht und 50 Controller |
| Kodierung | **entfällt** — mit iU1-P1.12 (`3ba7d54`) bereits erledigt; der Umzug ist deshalb ein reines `git mv` (171 R100-Umbenennungen) |
| Portabilitätssperre | `net10.0` ohne `-windows`, macOS-Build in der CI (iE5) |
| Namespace | **unverändert** lassen (§ 3.5, → iF13) |

**Abnahme (iZ4):** Windows-App baut und rechnet **byte-gleich** — 332/332. Zusätzlich: `EPOS.Kern`
baut und testet auf macOS in der CI. Reine Umbau-Etappe ohne Ergebniswirkung.
**Hier erreicht** (Linux, drei Projekte byte-gleich); der Windows-Durchlauf 332/332 steht aus.

### iU5 — Statics kappen, Dienste einziehen · L · Windows

> **Status 03.09.2026 — Abnahmekriterium erreicht, Windows-Bedienprobe offen.** Sechs
> Commits (`35be81f` iU5-T0 … `9235a92` iU5-T5) auf der Basis `18f515f`.
>
> **Das Abnahmekriterium ist maschinell erfüllt:**
> ```bash
> git grep -nE '\bProgram\.[A-Za-z]' -- 'EPOS.Kern/*.cs' \
>     'WindowsFormsApplication1/Allgemein/*.cs' \
>     'WindowsFormsApplication1/Controller/*.cs' \
>     'WindowsFormsApplication1/Model/*.cs' | grep -vP ':\s*(///|//|\*)'
> # → 0 Treffer  (Basis: 53)
> ```
>
> **Der Halter statt eines Containers.** `EPOS.Kern/Allgemein/Dienste/` führt neun
> Schnittstellen mit Standardfassungen und den statischen Halter `Dienste`; die
> Windows-Fassungen liegen in `WindowsFormsApplication1/Dienste/` und werden in
> `Program.Main` **vor** `DataRepository.DatenbankVorhanden()` eingelegt. Ein
> DI-Container ist im Bestand fremd — acht austauschbare Haken (`Meldung`,
> `KiTexte.Lieferant`, `KiEinwilligung.Nachfragen`, `KiAusfuehrer.*`,
> `AnlagenEindeutigkeit.Frage`, `SimulationControl.Speicherlauf` …) tragen ihn bereits;
> von den 22 rufenden Klassen sind etliche rein statisch. Begründung im
> Entscheidungsregister § 2.6.
>
> **`Meldung` bleibt** und zeigt seit T0 selbst auf `Dienste.Dialog`. `Program.Main`
> belegt deshalb nur noch `Dienste.*`. Beabsichtigter Nebeneffekt: Die Hinweisdialoge
> des Kerns tragen unter Windows wieder das **Informationssymbol**, das sie bis iU3-2
> hatten.
>
> **Tranchen und Zahlen.**
>
> | Tranche | Commit | Dateien | Fundstellen | Warnungen | Tests | Referenzlauf |
> |---|---|---|---|---|---|---|
> | T0 Halter, Schnittstellen, Adapter | `35be81f` | 33 neu + 4 | — | 123 / 89 | 809 | PASS |
> | T1 Dialoge | `4118ed0` | 14 | 33 `MessageBox.Show` | 123 / 89 | 809 | PASS |
> | T2 Pfade und Dateien | `8add154` | 13 | 12 `SpecialFolder`, 2 Dateidialoge, 1 `Process.Start` | 123 / 89 | 809 | PASS |
> | T3 Einstellungen und Lizenz | `d477a77` | 11 | ~30 Registry, 10 DPAPI, 3 `Settings` | 123 / 89 | 809 | PASS |
> | T4 Sprache | `b9fecf0` | 5 | 5 `nLanguage` | 123 / 89 | 809 | PASS |
> | T5 Navigation und Kontext | `9235a92` | 21 | 32 `Set*Control`, 25 `ShowDialog`, 9 `startfrm` | 123 / 89 | 810 | PASS |
>
> Warnungen „App / Kern"; die Summe **123** ist unverändert die Basis von iU4.
> Referenzlauf jeweils **1030, 1007, 1017** gegen `2026-08-30_B3-Kaskade`:
> **GESAMT PASS, 815.043 Werte**, `diff -rq` nur `protokoll.txt` zusätzlich.
>
> **Rückbau (M4).** `System.Management` als Paketreferenz entfernt (vorher bestätigt:
> null Treffer auf `System.Management`, `ManagementObject`, `ManagementClass`, `Win32_*`
> im ganzen Repo — die Geräte-ID lief nie über WMI). `Program.StartLocalWebServer` /
> `StopLocalWebServer` samt fester Pfadangabe `C:\WPFake` gelöscht (34 Zeilen, einziger
> Aufruf seit jeher auskommentiert). Die Dublette
> `RegPfad = @"Software\EPOS_PLAN\Variantentest"` auf eine Konstante gelegt.
>
> **Ausnahmen, die bewusst stehen bleiben.**
>
> | Fundstelle | Warum |
> |---|---|
> | `DataRepository.cs` — `Properties.Settings.DBPath/DBName`, `CommonApplicationData` | tabu in iU5, gehört zu iU6 |
> | `ErststartMigration.cs` — `Properties.Settings.Save()` | Access-Zweig, bleibt mit der Erststart-Migration in der Anwendung |
> | `HelpExtender` in `Hilfe/HelpCatalog.cs` — `new Form_HelpPopup()` | Oberflächenbaustein wie `HilfeAutomatik`/`InfoKnopf`, geht mit iU9 |
> | 12 `*KontextMenuCtrl` — 44 `new Form_X` / `ShowDialog` | **siehe unten** |
> | `EPOS.Kern/Allgemein/Dienste/StandardPfade.cs` — `SpecialFolder` | die Standardfassung von `IPfade` selbst; genau dort gehört es hin |
> | `SimulationControl`/`SimulationKanaele` (`speicherRegistry`), `SchemaMigration` (`KatalogRegistry`) | Wortteil, kein Registry-Zugriff — der Wächterausdruck führt kein `\b` |
>
> **Warum die Bearbeitungsdialoge der Kontextmenüs bleiben.** Sie sind keine
> `Program`-Bindung, sondern eine Maske-zu-Maske-Kopplung: Der Controller füllt
> `frm.list_werzmodel` mit typisierten Modellen, setzt `frm.m_nType`/`m_ID_Projekt`,
> ruft `frm.SetControls(…)`, zeigt und liest die Liste zurück. Ein Schlüssel plus
> `object[]` bildete das nur ab, indem der halbe Controller in `WinFormsNavigation`
> zöge. Diese Klassen sind ohnehin **Oberflächenbausteine** — sie führen `ListView`,
> `ContextMenuStrip` und `MouseEventHandler` und können nie in den plattformfreien Kern;
> sie wandern mit ihren Masken in iU9. `MenueCtrl` dagegen ist vollständig umgestellt und
> braucht kein `using System.Windows.Forms` mehr.
>
> **Windows-Prüfpunkte für die Abnahme am Gerät.** Registry-Werte werden unverändert
> gelesen (`Language`, `GeminiApiKey`, `KiZaehler`, `KiHinweisBestaetigt`,
> `CsvExportPfad`, `LizenzAnker`, `LizenzZugestimmt`); die Lizenz bleibt aktiviert und
> der KI-Schlüssel lesbar (DPAPI-Geltungsbereiche unverändert `LocalMachine` bzw.
> `CurrentUser`); Umschalten de↔en wirkt nach Neustart wie bisher; alle zwölf Gewerke
> öffnen und speichern über das Kontextmenü; die vier Ja/Nein-Rückfragen antworten in
> beide Richtungen richtig — die Projektlöschung mit Fokus auf „Nein"; alle 19
> Stammdaten- und Einlesemasken aus dem Menü; CSV-Export schlägt den zuletzt benutzten
> Ordner vor; Hilfe-Zwischenspeicher unter `%APPDATA%\EPOS-Plan` und Lizenz unter
> `%APPDATA%\wp-plan` bleiben liegen.
>
> **Offen nach iU5:** der Windows-Vollreferenzlauf 332/332, die Eingabehelfer
> `Program.Zahl*`/`Ganzzahl*` mit ihren Masken (iU9) und die genannten Ausnahmen.
>
> **iU5-Abschluss: der zweite Umzug (03.09.2026).** Fünf Commits (`a546af9` iU5-U1 …
> `a9e5c16` iU5-U5) auf `e3d1e5b`, dem letzten Commit von iU8. Nachdem iU5 den Kernkandidaten die Umgebung
> abgenommen hatte, ist die Frage „was kann noch mit?" nicht mehr geschätzt, sondern
> **gemessen worden**: Jede Datei unter `Allgemein/` und `Controller/` wurde nach
> `EPOS.Kern/` verschoben und der Kernbau mit `EnableWindowsTargeting=false` als Wächter
> laufen gelassen; was er ablehnte, ging unverändert zurück. **74 von 136 Dateien sind
> mitgegangen** — der Kern wächst von **194 auf 268** `.cs`-Dateien; unter
> `WindowsFormsApplication1/Allgemein/` und `/Controller/` bleiben **62**. (Die 136 sind 84 + 49
> aus `c477523` plus die drei Hüllendateien, die iU8-6/iU8-7 dazwischen angelegt haben —
> `Blazor/BlazorDialogForm.cs`, `Blazor/BlazorDienste.cs`, `Hilfe/WindowsHilfeDienst.cs`. Der
> zweite Umzug lief auf `e3d1e5b`, also **hinter** dem ganzen iU8-Strang B.)
>
> | Tranche | Commit | Verschoben | Zurück (Grund) |
> |---|---|---|---|
> | iU5-U1 | `a546af9` | **20** — `Lizenz/` (4), `Export/` (1), `Import/` (12), `Katalog/` (3) | keine |
> | iU5-U2 | `5cb807c` | **11** von 25 aus `KI/` — Wissen, Semantik, Texte, Schutzstufen, Einwilligung | **14**: `KiDialogZugriff`, `KiAusfuehrer`, `HilfeKontext`, `KiAufrufKnopf` (lebende `Control`/`Form`); `KiAktionenDialog` (+ `HelpEntry`); `KiAktionenProjekt`/`-Schreiben` (`OleDbException`); `KiChatService`, `KiAktionenSitzung`, `KiAktionen` (+ `KiHilfe`), `-Energie`, `-Uebernahme`, `-Wirtschaft`, `KiAktionenLastgang` (`GanglinienEintrag`) |
> | iU5-U3 | `82807f4` | **9** — die Bericht-AUSGABE: Word, Excel, `Bausteine/` (4), `IBerichtsBaustein`, `BerichtsKonfiguration`, `ZeitreihenExtraktor` | **2**: `ChartRendererGdi` (GDI+, von vornherein ausgenommen), `BerichtsDatenSammler` (`EnergieMengen` aus `Views/Varianten/`) |
> | iU5-U4 | `c67fe36` | **29** von 47 Controllern — sieben Stamm-, vierzehn Projekt-, fünf Zuordnungs-Controller, `BerichtCtrl`, `SpotpreisImportCtrl`, `SpotpreisLeser` | **18**: die 12 `*KontextMenuCtrl`, `WPCtrl` (+ `.WinForms.cs`, `partial`), `KlimaregionStammCtrl` (`ComboBox`/`ListBox`), `WizardCtrl`/`MenueCtrl` (`WizardParent`), `EnergietraegerKatalogCtrl` (`EnergyCarrier`), `PeakShavingCtrl` (`OleDbException`), `ProjektExportImportCtrl` (`SchemaMigration`) |
> | iU5-U5 | `a9e5c16` | **5** — `ToolsClass`, `FileDlgClass`, `Hilfe/DokuUebersetzung`, `Update/AnlagenEindeutigkeit`, `chart_test` | **3**: `StromTestClass` (`WPCtrl`), `IAssistentRahmen` (`WizardSeite`), `Simulation/SchemaModell` (`Form_Waermesenke`) |
>
> **Ein einziger inhaltlicher Eingriff.** `Bausteine/BausteineStandard.cs` las die
> Programmfassung über `System.Windows.Forms.Application.ProductVersion`; an ihre Stelle tritt
> `DeckblattBaustein.ProduktFassung()` mit derselben Reihenfolge (informelle Fassung des
> Einstiegs-Assemblies → `FileVersionInfo.ProductVersion` → `"1.0.0.0"`). Weil die Anwendung
> `GenerateAssemblyInfo=false` setzt und nur `AssemblyFileVersion 1.1.0.0` deklariert, zeigt das
> Deckblatt unter Windows unverändert `1.1.0.0`. Dazu **17 tote `using`-Zeilen** entfernt
> (`System.Windows.Forms` 15×, `Microsoft.Win32` 2×) — in keiner dieser Dateien wurde ein Typ
> aus dem Namensraum benutzt; ohne `EnableWindowsTargeting` fiel das nie auf.
>
> **Neu im Kern**: `BouncyCastle.Cryptography`, `ClosedXML`, `DocumentFormat.OpenXml`,
> `SixLabors.Fonts`, `Microsoft.ML.OnnxRuntime`, `Microsoft.ML.Tokenizers` und eine
> `ProjectReference` auf `KiKern` — alle plattformfrei. `Mscc.GenerativeAI` und
> `Microsoft.Extensions.Http/Logging` wurden **nicht** gebraucht: Die Namen stehen im Bestand
> nur in Kommentaren von `KiChatService`, und der bleibt in der Anwendung.
>
> **Die Schnittkante, die dabei sichtbar wurde:** Was der KI-Assistent **weiß**, ist Kern; was
> er **bedient**, hängt an lebenden WinForms-Controls und bleibt bei der Oberfläche, bis iU8 die
> Masken ablöst. Dasselbe Muster bei den Controllern: Rechnen und Speichern gehen mit, Listen-
> und Kontextmenü-Bedienung bleibt.
>
> **Nachweis je Tranche:** Kern 0 Fehler (Warnungen 2 → 3, die dritte ist CS0108 aus
> `StromverbraucherStammCtrl` und mitgewandert), Lösung x64 0 Fehler / **34 Warnungen**
> unverändert, **886 Tests** grün, Referenzlauf 1030/1007/1017 GESAMT PASS mit leerem
> `diff -rq`, ChartProben 9 Bilder ohne Verstoß, `ZugriffsschichtProben` und
> `Referenzlauf` (x64) 0 Fehler, beide Wächter 0 Treffer.
>
> *(Die Commit-Bodys der fünf Tranchen nennen 36 Warnungen — sie sind im Worktree ohne
> iU8 gemessen worden. Auf `ios_migration` liegt der zweite Umzug hinter `92380ea`, das
> zwei WFO1000 mit `Form_Kosten_Auswahl` gelöscht hat; nachgemessen sind es dort **34**.)*
>
> **Offen nach dem zweiten Umzug:** die Windows-Bedienprobe (Berichte, Katalogimport,
> Lizenzaktivierung, KI-Chat) und die 59 Dateien, die erst mit `Views/` bzw. mit dem
> Access-Zweig gehen können.

**Voraussetzung:** iU4. **Block A2, A3, A4, M4.**

| Inhalt | Menge |
|---|---|
| `Program.*`-Statics → `INavigation`, `IProjektKontext`, `ISprache`, `IPfade` | **40 Dateien** (21 an den vier Kernnamen, 40 einschließlich Pfade/Sprache) |
| `MessageBox.Show` / `ShowDialog` / `DialogResult` → `IDialogDienst` | **127 / 115 / 149** Dateien (Projektsummen, nicht die View-Zahlen des Grundlagenkonzepts) |
| Registry → `IEinstellungen` | **9 Dateien** (nicht 3) |
| DPAPI → `ILizenzAblage` | 2 Dateien — Lizenz **und** KI-Schlüssel |
| `Program.Zahl*`/`Ganzzahl*` → Eingabekomponenten | **44 Dateien**; Vorkommen: `ZahlPruefen` 121, `ZahlFaerben` 120, `ZahlParsen` 55, dazu `Ganzzahl*` 102 |

**Abnahme:** Kein View-fremder Code greift auf `Program.*`; Referenzläufe unverändert PASS.
**Hier erreicht** (Linux, drei Projekte byte-gleich, Wächter 0 Treffer); die Bedienprobe auf
Windows und der Vollreferenzlauf 332/332 stehen aus.

### iU6 — Datenzugriff plattformfrei · S–M · Windows

> **Status 03.09.2026 — hier erreicht, Windows-Nachweis offen.** Sechs Commits
> (`22fb7eb` iU6-T1 … `2387abf` iU6-T5) auf `origin/ios_migration` = `18f515f`.
>
> **Das Ergebnis in einem Satz: `EPOS.Kern` nennt `System.Data.OleDb` nicht mehr — weder
> im Quelltext noch als `PackageReference` —, und `CA1416` ist von 87 auf 0 gefallen.**
> Der Datenzugriff liegt hinter `IDatenzugriff`; `DataRepository` bleibt die Fassade
> davor und ist für die rund 160 Aufruferdateien unverändert.
>
> | Tranche | Commit | Inhalt | CA1416 |
> |---|---|---|---|
> | iU6-T1 | `22fb7eb` | `RecordSet.DBCommand` **ersatzlos gestrichen** (iR8) | 87 → 78 |
> | iU6-T2 | `582844c` | toter OleDb-Code in `SolarkollektorenCtrl`, `PufferSpCtrl`; Access-Zweig aus `ApplikationCtrl` in die Anwendung | 78 → **0** |
> | iU6-T3a | `35de91d` | Masken-Sweep: `OleDbParameter` → `DbParam` in 46 Views | 0 |
> | iU6-T3b | `fe28cb2` | Brücke aus dem Kern; `System.Data.OleDb` aus `EPOS.Kern.csproj` | 0 |
> | iU6-T4 | `7780df6` | `IDatenzugriff` + `SqliteDatenzugriff`; `DataRepository` wird Fassade | 0 |
> | iU6-T5 | `2387abf` | `bundle_green` für iOS vorbereitet (greift erst mit Multi-Targeting) | 0 |
>
> **iR8 war eine Streichung, kein Umbau.** Die Vermessung fand repositoryweit **null**
> Zugriffe auf `RecordSet.DBCommand` außerhalb von `RecordSet.cs`. Das Kommando wurde
> seit iU3 nur noch lazy im Getter angelegt und blieb damit immer `null`; `MerkeSql()`
> schrieb in ein Objekt, das es nie gab, `Parameter()` lieferte ausnahmslos `null`. Ein
> Ersatztyp wäre eine Fassade für null Nutzer gewesen — und hätte den falschen Eindruck
> erweckt, `RecordSet` trage Parameter. Es gibt deshalb **keinen `DbBefehl`**.
>
> **Dasselbe Bild in zwei Controllern.** `SolarkollektorenCtrl.Update()`,
> `PufferSpCtrl.Delete(string)` und `PufferSpCtrl.Update()` füllten ein `DBCommand`, das
> nie eine Verbindung bekam, und riefen darauf `ExecuteNonQuery()` — auf Windows also
> eine `InvalidOperationException` im `catch` und ein stilles `return false`. Alle drei
> hatten **0 Aufrufer** (erschöpfende Instanzlisten in den Commit-Bodys); geschrieben
> wird über die OleDb-freien `*StammCtrl`. Zusammen waren das 71 der 87 Warnungen.
>
> **`EPOS.Daten` entsteht nicht.** Die Planung sah dafür ein eigenes Projekt vor. Der
> Vertrag ist ein Interface und eine Klasse — ein drittes Projekt hätte den Kern von
> seiner eigenen Zugriffsschicht getrennt, ohne dass ein zweiter Anbieter in Sicht wäre
> (§ 1.5, Präzisierung zu iL2: es gibt **einen** Dialekt). `IDatenzugriff` und
> `SqliteDatenzugriff` liegen daher in `EPOS.Kern/Allgemein/`.
>
> **Der Masken-Sweep kam vor die Streichung.** Umgekehrt wäre der Zwischenstand nicht
> übersetzbar gewesen: Die Views hängen an genau dem impliziten Operator und an
> `DbParam.Von()`, die T3b entfernt. Das Skript hat 434 `OleDbParameter`-Vorkommen
> ersetzt, 54 `DbParam.Von(…)`-Klammern aufgelöst, 39 `OleDbType` auf `DbParamTyp`
> gehoben, 36 Objektinitialisierer `{ Value = }` auf `{ Wert = }` gezogen und 38
> `using System.Data.OleDb;` entfernt — 46 Dateien, keine von Hand.
>
> **Was Windows-seitig offen ist.** Der Referenzlauf deckt den Rechenpfad ab, nicht die
> Bedienung. Offen sind deshalb: die **Erststart-Migration aus einem `.accdb`-Bestand**
> (einziger verbliebener Nutzer der Brücke — `SchemaMigration` und `GeraeteWaisen` binden
> über `DbParamOleDb.Nach`, den Schemamarker liest und schreibt `SchemaVersionAccess`);
> die **Solar- und Pufferspeicher-Dialoge**, die sich „unverändert" verhalten müssen; die
> **36 Views mit `RecordSet`** (FormMain 13, Form_Start 10, Form_PV 6, Form_Gebäude 6,
> Form_WP 4, dazu `Form_DBBHKW.cs:436/450` mit den `DbVorgang`-Überladungen); und die
> Sweep-Dateien mit den meisten Stellen — `Form_Kosten.cs` (83), `ucFuelSettings.cs`
> (80), `Form_BHKWEing.cs` (50), `Form_Heizkessel.cs` (46),
> `Form_Heizkessel_einlesen.cs` (20). Die Liste ist je Commit abhakbar in
> [`Umsetzung_iU0_iU1_Nachweise.md`](Umsetzung_iU0_iU1_Nachweise.md).
>
> **Nachweis hier:** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler,
> **36 Warnungen** (vorher 123; die 87 CA1416 sind weg). `EPOS.Kern` allein: 0 Fehler,
> **2 Warnungen** (CA2255, CS0108 — beide aus dem Bestand). `dotnet test
> WP-Plan.Kern.slnf` → **805** (796 + 9 neue). Referenzlauf **1030, 1007, 1017** gegen
> `2026-08-30_B3-Kaskade`: **GESAMT PASS (815.043 Werte), alle drei byte-gleich** — nach
> **jeder** Tranche. `Proben/ZugriffsschichtProben` übersetzt fehlerfrei.
> `dotnet list EPOS.Kern package | grep -c OleDb` → **0**.

**Voraussetzung:** iU4. **Block B1.** Der Umfang ist gegenüber Rev. 1 stark geschrumpft, weil
`6486c36` das Meiste bereits erledigt hat.

**Bereits erledigt (nicht Gegenstand dieses Pakets):** SQLite-Umstellung von `DataRepository` und
`RecordSet`, die 36 Eigenverbindungs- und die Transaktionsdateien, `DbVorgang`, Schema-Auskunft per
`PRAGMA`, ADOX-/`@@IDENTITY`-/`TOP`-Ersatz, Dialekt-Sweep, Schemapflege-Gabelung, Erststart-Migration.

| Inhalt | Detail |
|---|---|
| `IDatenzugriff` in `EPOS.Kern` mit eigenem `DbParam` | Weg (b) aus § 1.4 (→ iF10). `DataRepository` bleibt unverändert und wird als **Windows-Adapter** dahintergehängt; `UebersetzeParameterzeichen` und `NormalisiereWert` sind bereits providerneutral |
| ~~**`RecordSet.DBCommand`**~~ | **ersatzlos gestrichen** (iU6-T1, `22fb7eb`). Die Vermessung fand repositoryweit **0** externe Nutzer: Das Kommando entstand seit iU3 nur lazy im Getter und blieb immer `null`; die „47 Nutzer" hingen an `Open`, `Next`, `Read`, `Close`. Kein Ersatztyp, kein `DbBefehl` |
| iOS-Laufzeit | `Microsoft.Data.Sqlite` zieht standardmäßig `SQLitePCLRaw.bundle_e_sqlite3`, das auf iOS an der AOT-Regel gegen dynamisches Laden scheitert. **Auf iOS `SQLitePCLRaw.bundle_green` setzen** (nutzt dort die System-SQLite) |
| Paketstand | `Microsoft.Data.Sqlite 8.0.11` beim .NET-10-Sprung auf 10.x mitziehen (iU1) |
| Seed und Pfade | Werksvorgabe `Kenndaten.sqlite` liegt bereits vor (S8). Für iOS: als App-Beilage mitgeben, beim Erststart in den beschreibbaren Bereich kopieren (`IPfade`) — die vorhandene `ErststartMigration` ist die Vorlage |

**Abnahme:** `EPOS.Kern` enthält keinen Verweis auf `System.Data.OleDb` (`EPOS.Daten` entsteht
nicht, s. o.); Referenzlauf über `IDatenzugriff` wertgleich.
**Hier erreicht** (CA1416 87 → 0, `dotnet list EPOS.Kern package | grep -c OleDb` = 0, drei Projekte
byte-gleich); die Windows-Punkte stehen im Statusblock.

### iU7 — Charts und Berichte plattformfrei · M · Windows

> **Status 03.09.2026 — Renderer und Ausgabe sind im Kern.** Fünf
> weitere Commits (`6604c05` iU7-5 … `0af6421` iU7-9) auf der Basis `300a354`, aufbauend auf
> iU7-1…iU7-4 (`c6b32eb`…`f84932b`). Die Ausgabe selbst ist im zweiten Umzug gewandert
> (iU5-U3, `82807f4`); iU7-9 hat den letzten Rest — die Systemdialoge der Berichtsansicht —
> auf `Dienste.Datei` gelegt.
>
> | Tranche | Commit | Inhalt |
> |---|---|---|
> | iU7-5 | `6604c05` | **`ChartRenderer.cs` von `WindowsFormsApplication1` nach `EPOS.Kern/Allgemein/Bericht/`** — verschoben, nicht verlinkt. `SkiaSharp` im `EPOS.Kern.csproj`, die nativen Bibliotheken **bedingt über `IsOSPlatform`** (Linux, macOS, Win32). Namespace bleibt `WindowsFormsApplication1`, alle Aufrufer der Anwendung und `Referenzlauf/Bildvergleich.cs` übersetzen unverändert |
> | iU7-6 | `6737dd4` | **`Proben/ChartProben` hängt am Kern** statt an Ersatzklassen: `ProjectReference` auf `EPOS.Kern` statt `Compile Include`; `ZeitreihenSatzStub.cs` und `BerichtTexteStub.cs` gelöscht. Die Probe misst jetzt die echten `ZeitreihenSatz`/`VerlaufSerie`/`BerichtTexte` |
> | iU7-7 | `dc97916` | **ChartProben in `kern.yml`** — nach dem Test-Schritt, auf ubuntu **und** macos; die neun PNG als Artefakt `chartproben-<os>` (14 Tage) |
> | iU7-8 | `0759b37` | **Drei Renderer-Tests in `EPOS.Kern.Tests`**: `TagesMittel`/`MonatsSummenMWh` exakt, `Kuchen` liefert PNG in 960×600, `BalkenHorizontal` zweimal byte-gleich. **869 → 872 Tests** |
> | iU7-9 | `0af6421` | **Berichtsausgabe über `Dienste.Datei`**: `Views/Bericht/UcBericht.cs` (Ordnerwahl, Speicherziel, zweimal Öffnen) und `Views/Varianten/Form_Variantentest.cs` (Speicherziel, Öffnen) rufen `OrdnerWaehlen`, `DateiSpeichern` und `MitSystemOeffnen` statt `FolderBrowserDialog`, `SaveFileDialog` und `Process.Start`. `WordBerichtGenerator.FindeVorlage()` brauchte nichts — sie sucht schon über `AppDomain.CurrentDomain.BaseDirectory`. Eine bewusste Abweichung: die Dialoge bekommen kein Besitzerfenster mehr, weil `IDateiDienst` keines kennt |
>
> **Der Renderer war die Eintrittskarte, der Rest kam mit iU5-U3 und iU7-9.** Im Kern liegt
> jetzt die **Zeichnung** *und* die **Ausgabe** — `WordBerichtGenerator`,
> `ExcelBerichtGenerator`, `Bausteine/`, `BerichtsKonfiguration`, `ZeitreihenExtraktor`,
> `IBerichtsBaustein` (verschoben im zweiten Umzug, siehe iU5-Statusblock). In der Anwendung
> geblieben ist nur `BerichtsDatenSammler`, weil er `EnergieMengen` aus `Views/Varianten/` ruft;
> `ChartRendererGdi` (Gegenpart des Windows-Bildvergleichs aus iU7-1) ist mit iF23 am
> 03.09.2026 gelöscht.
>
> **Damit steht die Vorlage für iF16.** Der Kern liefert PNG-Bytes, die Oberfläche zeigt
> sie an — genau der Weg, den `EPOS.UI/Standards/ChartBild` (iU8-4) schon annimmt. Ein
> Chart-Stack für Bericht *und* Bildschirm, ohne SkiaSharp-Komponente in der WebView
> (iR3).
>
> **Nachweis:** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler,
> **36 Warnungen** (unverändert); `EPOS.Kern` allein 0 Fehler, **2 Warnungen** (nach
> iU5-U4 drei — die dritte ist mit `StromverbraucherStammCtrl` mitgewandert).
> `dotnet test WP-Plan.Kern.slnf -c Release` → **872** (869 + 3).
> `dotnet run --project Proben/ChartProben -c Release` → *9 Bilder geprueft, 0
> Verstoesse*; alle neun PNG **byte-gleich** zum Stand vor dem Umzug (Schrift auf dem
> Prüfsystem: Liberation Sans). Referenzlauf **1030, 1007, 1017** gegen
> `2026-08-30_B3-Kaskade`: **GESAMT PASS** (815 043 Werte), `diff -rq` meldet für alle
> drei Projekte **keinen** Unterschied.

**Voraussetzung:** iU4. **Block D1, D2, M5.** Läuft **parallel** zu iU5/iU6 — ohne UI testbar.

| Inhalt | Detail |
|---|---|
| `ChartRenderer` | `Allgemein/Bericht/ChartRenderer.cs`, **821 Zeilen**, `System.Drawing` + `Drawing2D` + `Imaging` → SkiaSharp. Der einzige echte GDI+-Blocker der Berichtskette |
| übriges GDI+ | 26 Dateien nutzen `Graphics`; die 256 `System.Drawing`-Treffer sind weit überwiegend Typnutzung (`Color`, `Font`, `Point`) in Designer-Dateien und damit unkritisch |
| Berichtsausgabe | Word/Excel über `IDateiDienst`/`ITeilen` — ✔ mit iU5-U3 und iU7-9 erledigt. ~~ClosedXML-Standardschrift für Nicht-Windows setzen~~ **nicht nötig**: ClosedXML 0.105.1 bringt Carlito eingebettet mit; `GrafikModulSicherstellen()` übersteuert nur, wenn eine Messprobe fehlschlägt (iU7-4, Entscheidungsregister § 2.7) |

**Das Chart-Problem, das das Grundlagenkonzept nicht kennt.** iL4 (Blazor Hybrid) und iL6
(ScottPlot 5/SkiaSharp) vertragen sich nicht unmittelbar: **SkiaSharp-Blazor-Komponenten
funktionieren in einem Blazor-Hybrid-Wirt nicht** — die WebView ist ein eigener Prozess, das
Zeichnen findet im .NET-Prozess statt. Drei Wege:

| Weg | Bewertung |
|---|---|
| **Der .NET-Prozess rendert ein PNG, die Blazor-Seite zeigt es als Bild** | **Empfehlung — und mit iU7/iU8 gebaut.** Gerendert wird nicht mit ScottPlot, sondern mit dem Kern-Renderer `ChartRenderer` (roher SkiaSharp seit iU7-2); die Anzeige übernimmt `EPOS.UI/Standards/ChartBild`. Ein Chart-Stack für Bericht *und* Bildschirm, identische Optik auf beiden Plattformen, kein zusätzliches Paket. Preis: keine Interaktion im Chart (Zoom, Tooltip) ohne Zusatzarbeit. **ScottPlot bleibt für die interaktiven Bildschirmmasken** — heute genau eine, `Form_SpeicherOptimierung` (→ iF22) |
| JavaScript-Chartbibliothek in der Blazor-Schicht | volle Interaktion, aber **zwei** Chart-Stacks (einer für den Bericht, einer für den Bildschirm) — genau das, was M5 abschafft |
| Chart außerhalb der WebView als natives MAUI-Steuerelement | bricht das Modell C — die Komponente wäre je Hülle verschieden |

Zu klären in iU7, nicht erst in iU9 (→ iF16), denn davon hängt ab, ob die 39
`DataVisualization`-Stellen ein oder zwei Ziele haben. **Beantwortet:** ein Ziel — das Bild aus dem
Kern-Renderer; ScottPlot bleibt nur dort, wo im Chart wirklich bedient wird (iF22).

**Abnahme:** Berichtsbilder aus dem neuen Renderer sind gegen die alten sichtgeprüft; Berichtsdatei
zeilengleich. **Hier erreicht, soweit ohne Windows möglich** (ChartProben 9/9, drei Renderer-Tests,
`kern.yml` auf ubuntu und macos). Der Bildvergleich alt/neu lief nur unter Windows und wurde
nie gefahren: Der Anwender hat am 03.09.2026 die Löschung von `ChartRendererGdi.cs` und des
Modus `bildvergleich` ohne ihn angeordnet (→ iF23); der Nachweis der Bildgleichheit ist seither
der Sichtvergleich der Berichte am Gerät.

### iU8 — `EPOS.UI` und der erste Blazor-Dialog unter Windows · L · Windows

> **Status 03.09.2026 — iZ5 hier erreicht, Windows-Abnahme offen.** Drei Stränge, **neunzehn
> Commits**: Strang A (8) auf der Basis `18f515f`, Strang B (7) auf `c477523`, Strang C (4) auf
> `f5fb05c`. **Ein vollständiger Dialog von EPOS-Plan lebt in plattformfreiem Code**:
> `Form_Kosten` öffnet „Energieträger anlegen" als Razor-Komponente; die WinForms-Fassung ist
> gelöscht.
>
> **Strang A — `EPOS.UI`, die Bibliothek**
>
> | Tranche | Commit | Inhalt |
> |---|---|---|
> | iU8-1 | `8574911` | Paketgruppe „Blazor Hybrid (iU8)" in `Directory.Packages.props`: Components.Web/QuickGrid 10.0.11, WebView.WindowsForms 10.0.100, bunit 2.9.0, CodeAnalysis.CSharp 5.9.0 |
> | iU8-2 | `a1b4df6` | `EPOS.UI` als Razor-Klassenbibliothek, `net10.0`, `EnableWindowsTargeting=false` — derselbe Wächter wie im Kern |
> | iU8-3 | `bbb7d42` | Thema aus `KartenStil.cs` als CSS-Variablen; die sieben Bausteine (Gruppenkopf, Warnbanner, SpeichernLeiste, InfoKnopf, Kachel, Herleitungs- und Kohärenzzeile) |
> | iU8-4 | `f690466` | Standardfelder: Zahl, Ganzzahl, Text, Auswahl, Datum, Schalter, Raster (QuickGrid), ChartBild |
> | iU8-5 / 5b / 5c | `cace2db`, `45a21dc`, `f5fb05c` | `EPOS.UI.Tests` mit bunit; Aufnahme in `WP-Plan.sln` und `WP-Plan.Kern.slnf`; UI-Kultur der Tests auf `de-DE` gepinnt |
> | iU8-8a | `8f5a28e` | `EnergietraegerVarianteDialog.razor` — der erste Dialog als Komponente, datenbankfrei |
>
> **Strang B — die Windows-Hülle und der Stichtag**
>
> | Tranche | Commit | Inhalt |
> |---|---|---|
> | iU8-6 | `4369fdb` | **`WindowsFormsApplication1.csproj` auf `Microsoft.NET.Sdk.Razor`**, Projektreferenz auf `EPOS.UI`, `wwwroot/index.html`, `Allgemein/Blazor/BlazorDialogForm.cs` + `BlazorDienste.cs` |
> | iU8-7 | `b12e910` | Hilfe-Brücke: `HelpExtender.ZielFuer(schluessel)` und `Allgemein/Hilfe/WindowsHilfeDienst.cs` |
> | iU8-8b | `1e2a44c` | Sieben Ressourcenschlüssel (`KAUSW_*`, `ALLG_BTN_*`) und `EPOS.Kern/Controller/EnergietraegerVarianteCtrl.cs` |
> | iU8-9 | `92380ea` | **iZ5** — `Form_Kosten` öffnet die Komponente; `Form_Kosten_Auswahl.cs/.Designer.cs/.resx` **gelöscht** (M1) |
> | iU8-10 | `eafbc1f` | WebView2 als zweite Setup-Voraussetzung (`.iss`, `build-setup.ps1`, Setup-Konzept 5.5) |
>
> Dazu in Strang B `eff82aa` (iU8-13, Doku und Windows-Nachweisliste) und `e3d1e5b` (iU8-10b,
> `.gitignore` für den WebView2-Bootstrapper in der Repowurzel).
>
> **Strang C — der Formular-Generator** (`479fcf9` iU8-12a … `0af7ca7` iU8-12d, dazu
> `4aa6b15` iU8-12e): `Werkzeuge/Formularkarte`, Roslyn-Leser, `resx`-Leser mit
> Label-Zeilenregel, Razor-Skelett, Stapellauf über alle Designer-Dateien; mit iU8-12e das
> **Prüfmuster** statt der lebenden Maske.
>
> **Der Razor-SDK ist keine Kosmetik, sondern die einzige Möglichkeit.** Die Gegenprobe mit
> dem einfachen `Microsoft.NET.Sdk` übersetzt fehlerfrei, liefert im
> Veröffentlichungsordner aber **kein `wwwroot`** — weder `index.html` noch `_content` noch
> `_framework/blazor.webview.js`. Der Dialog bliebe beim Anwender leer. Die Umstellung
> kostet **keine** neue Warnung (Codes vor und nach identisch).
>
> **Drei Korrekturen an diesem Konzept**, gemessen statt geschätzt:
>
> | Stelle | Bisher | Befund 03.09.2026 |
> |---|---|---|
> | Zahl der Designer-Dateien (iU8, Formular-Generator) | „**118**" bzw. 79/74/21 aus der Vorvermessung | **123 Dateien, 120 Masken, 63 davon lokalisiert** — die Vorvermessung suchte nur `*.Designer.cs`; der Bestand schreibt auch `*.designer.cs` (`Form_BHKWEing.designer.cs`). Nach dem Löschen von `Form_Kosten_Auswahl` sind es 122/119 |
> | Name der Scoped-CSS-Datei | `EPOS.UI.styles.css` erwartet | **`EPOS_Plan.styles.css`** — das Bündel folgt dem **Host**-Assembly, nicht der Bibliothek, und liegt in `wwwroot\`, nicht neben der EXE |
> | „ClosedXML-Standardschrift für Nicht-Windows setzen" (iU7-Tabelle) | als offene Aufgabe geführt | **nicht nötig.** iU7-4 (`f84932b`) hat nachgemessen: ClosedXML 0.105.1 bringt Carlito eingebettet mit; eine erzwungene Systemschrift machte die Spaltenbreiten schlechter. Gesetzt wird nur noch, wenn eine Messprobe fehlschlägt |
>
> **Das Raster „Label x28 / Control x270" gibt es nicht.** `Point(28,` und `Point(270,`
> kommen in je einer Datei vor. Tragfähig ist die Zeilenregel: das nächste Label **links in
> derselben Zeile** (|Δy| ≤ 8 px) — sie trägt den Stapellauf über alle Masken (iU8-12b).
>
> **Nachweis (Linux, SDK 10.0.400):** `dotnet build WP-Plan.sln -c Release -p:Platform=x64
> --no-incremental` → 0 Fehler, **34 Warnungen** (vorher 36; die beiden entfallenen WFO1000
> gehören dem gelöschten Formular), **keine neuen Warnungscodes**.
> `dotnet test WP-Plan.Kern.slnf -c Release` → **886** (KiKern 450, SpeicherEngine 337,
> EPOS.UI 64, EPOS.Kern 35). Referenzlauf **1030, 1007, 1017** gegen
> `2026-08-30_B3-Kaskade`: **GESAMT PASS** (815 043 Werte), `diff -rq` nur `protokoll.txt`.
> `dotnet run --project Proben/ChartProben -c Release` grün. `dotnet publish -r win-x64
> --self-contained` enthält `wwwroot/index.html`, `wwwroot/EPOS_Plan.styles.css`,
> `wwwroot/_content/EPOS.UI/{epos-ui.css,help_icon.png}`,
> `wwwroot/_content/…QuickGrid/QuickGrid.razor.js`, `wwwroot/_framework/blazor.webview.js`,
> `EPOS.UI.dll`, `Microsoft.Web.WebView2.{Core,WinForms}.dll` und
> `runtimes/win-x64/native/WebView2Loader.dll`.
>
> **Was noch offen ist.**
>
> 1. **Die Windows-Abnahme von iZ5** — Maus *und* Finger (M2), deutsch *und* englisch,
>    Hochkontrast, 125 %/150 % DPI (greift die DPI-Insel?), Enter/Esc, Infoknopf,
>    WebView2-Profilordner, Setup in der Windows-Sandbox ohne WebView2, VS-2026-Designer
>    unter dem Razor-SDK. Die Punkte stehen einzeln in
>    [`Umsetzung_iU8_Nachweise.md`](Umsetzung_iU8_Nachweise.md).
> 2. ~~**Der Stapellauf des Generators liest den gelöschten Dialog.**~~ **Erledigt mit
>    iU8-12e (`4aa6b15`).** 22 der 100 Tests hingen an `Form_Kosten_Auswahl.Designer.cs` bzw.
>    an `new Form_Kosten_Auswahl` in `Form_Kosten.cs` und scheiterten seit iU8-9. Gelöst
>    wurde das nicht durch eine andere Probemaske allein, sondern durch die Trennung von
>    Werkzeugprüfung und Bestandsprüfung: Der letzte Stand der gelöschten Maske liegt
>    **eingefroren** unter `Formularkarte.Tests/Pruefmuster/Kosten/` (Designer, `.cs`, `.resx`
>    und der Aufrufer-Auszug aus `Form_Kosten.cs`, wortgleich aus `92380ea^`), wird **nie
>    übersetzt** und vom Stapellauf **übergangen** wie `bin` und `obj`; die `StapelTests`
>    hängen jetzt an der lebenden `Form_Kosten_VarAuswahl`. **101 Tests, alle grün.**
>    Nachgemessen nach iZ5: **122 Designer-Dateien, 119 Masken** im ganzen Repo, davon
>    **117 unter `Views/`**.
> 3. **WebView2-Verteilung online oder offline** — Bootstrapper (heute), Standalone-Installer
>    oder Fixed Version. Anwenderentscheidung, offen als S10 im Setup-Konzept.
>
> **iU9-1 (vorgezogen) — der Öffner des ersten Dialogs war unerreichbar.** Die Windows-Abnahme
> vom 03.09.2026 hat gezeigt, dass iU8-9 die falsche Maske umgestellt hat: `Form_Kosten` ist seit
> **KD6a kein Einstieg mehr** (`UcBkKosten.btnVerwaltung_Click` öffnet `Form_KostenKomponente`,
> `Form_Start.cs:2175` entfernt `btn_Kosten` per `EntferneAltknopf`). Der erste Blazor-Dialog war
> damit in der Oberfläche nicht zu erreichen — nicht falsch gebaut, nur an der toten Maske
> angeschlossen. Dieselbe Funktion lebte in der zeichengleichen Schwester
> `Views/Kosten/Form_Kosten_VarAuswahl` mit zwei erreichbaren Aufrufern:
> `Form_Heizkessel.CreateNewEnergyCarrier` (Knopf „◀", `btn_Kessel_Hinzu`) und dem Gegenstück in
> `Form_BHKWEing` (`btn_Hinzu`). Beide öffnen seit **iU9-1** dieselbe Razor-Komponente über
> `BlazorDialogForm`; die Schwester ist gelöscht (M1), damit gibt es die drei Abfragen des
> Dialogs nur noch einmal — in `EnergietraegerVarianteCtrl`. `Form_Kosten.CreateNewEnergyCarrier`
> bleibt unverändert stehen (die Maske ist tot, aber nicht gelöscht — das entscheidet der
> Anwender) und trägt den Befund als Kommentar. **Nachweis:** Build 0 Fehler / **30** Warnungen
> (34 minus die vier WFO1000 der gelöschten Maske), **928** Tests, Formularkarte **101/101**,
> Referenzlauf 1030/1007/1017 **GESAMT PASS**. **Die Lehre** steht im Entscheidungsregister
> § 2.8: Die Wahl der ersten Maske muss die **Erreichbarkeit des Öffners** prüfen, nicht nur
> Größe und Feldzahl.

**Voraussetzung:** iU5, iU7. **Block A5, A6, A7; M1, M2, M6, M9.** **Das ist der Modell-C-Stichtag.**

| Inhalt | Detail |
|---|---|
| `EPOS.UI` als Razor-Klassenbibliothek | Bausteinsatz nach A5: SpeichernLeiste, InfoKnopf (an `help_mapping`), Kachel, EinstiegsKarte, Gruppenkopf, Herleitungszeile, Kohärenzzeile, Warnbanner, Farb-/Typografiethema — ~10–12 Bausteine |
| `BlazorWebView` in der WinForms-App | `Microsoft.AspNetCore.Components.WebView.WindowsForms` (für .NET 10 verfügbar und gepflegt); WebView2-Laufzeit als Voraussetzung |
| Standards **vor** der ersten Maske | Raster (QuickGrid-Wrapper), Charts (Ergebnis aus iU7), Datums-/Auswahlfelder — M6: ein nachträglicher Rasterwechsel hieße 36 Masken zweimal bauen |
| Formular-Generator (A7) | ✔ **gebaut** als `Werkzeuge/Formularkarte` (iU8-12). Roslyn über die Designer-Dateien des Bestands — **123/120/63** vor dem Stichtag, **122/119** nach iZ5 und **121 Designer-Dateien / 118 Masken** nach iU9-1 (116 unter `Views/`), nicht 118 und nicht 79/74/21: Feldkarte (Name, Typ, Beschriftung über die **Zeilenregel** „nächstes Label links in derselben Zeile, \|Δy\| ≤ 8 px" — das Raster Label x28/Control x270 gibt es nicht —, Wertebereiche, ComboBox-Einträge, Tab-Reihenfolge, `resx`-Schlüssel beider Sprachen) + Razor-Sektionsskelette, dazu ein Stapellauf `--alle` |
| Erster Dialog | ✔ `Form_Kosten_Auswahl` („Energieträger anlegen") → `EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor`; Datenbankseite in `EPOS.Kern/Controller/EnergietraegerVarianteCtrl.cs`, Texte in `MyResource.Resource.*` — der Dialog spricht damit erstmals auch Englisch |

**Abnahme (iZ5):** Ein Blazor-Dialog läuft im Produktivbetrieb der Windows-App, mit Maus **und**
Finger abgenommen (M2), WinForms-Fassung im selben Schritt stillgelegt (M1).
**Hier erreicht, soweit ohne Windows möglich** — die WinForms-Fassung ist gelöscht, 64 bunit-Tests
grün, der Veröffentlichungsordner trägt `wwwroot` vollständig. Die Abnahme mit Maus und Finger
steht aus und ist die eigentliche Aufgabe von
[`Umsetzung_iU8_Nachweise.md`](Umsetzung_iU8_Nachweise.md).

> **Ab hier gilt die Arbeitsregel:** Jeder **neue** und jeder ohnehin **anzufassende** Dialog wird als
> Blazor-Komponente gebaut — nie mehr doppelt. Alt-Dialoge, die niemand anfasst, bleiben WinForms,
> bis ihre Reihe kommt.

### iU9 — Masken-Umwandlung in Wellen · XL · Windows

**Voraussetzung:** iU8. **Block C (K1–K6).** Läuft ab hier **dauerhaft parallel** zu allem Weiteren.

| Klasse | Menge | Weg |
|---|---|---|
| K1 Formularmasse mit Designer | ~90 | Generator-Skelett + Handlayout nach iL5; Feldkarte als Abnahmecheckliste |
| K2 Grid-Masken | 7 Designer-Instanzen / 36 Dateien mit Typnutzung — **in iU0 zu klären** | QuickGrid-Wrapper, Spalten je Maske |
| K3 Chart-Masken | 9 / 18 — **in iU0 zu klären** | Ergebnis aus iU7 |
| K4 programmatische Views | **42** | ohne Generator; für die jüngsten liegen maschinelle Feldkarten vor |
| K5 Sonderstücke | 6–8 | `Form_Start` (2.470 Z.), `MDIMainForm`, `WizardParent`, Navigatoren — und **`Form_Simulation_Detail`: 7.773 Zeilen, 11 Reiter**, wird zerlegt statt konvertiert |
| K6 stilllegen | ~10–15 | tote Enden begraben statt mitschleppen — **mit iU9-W0 abgetragen** (iF29): neun Masken gelöscht, `nein`/`verwaist` stehen auf 0 |

**Reihenfolge:** nach Anfasswahrscheinlichkeit — zuerst Wirtschaftlichkeit und Kosten (die aktiven
Baustellen; `Views/Kosten` allein sind 48 Dateien), zuletzt die ruhenden Admin- und Katalogdialoge.

**Abnahme je Welle:** Feldkartenabgleich vollständig, beide Sprachen, Maus und Finger.

> **Statusblock iU9 — Welle 13 umgesetzt (04.09.2026, Basis `08c489a` nach W12, zusammengeführt mit `4101740`)**
>
> **Sechs Masken — 2 396 Zeilen `.cs`, 2 621 Designer, 32 `MessageBox` — sind drei Razor-Komponenten:**
> `KatalogImportDialog` (`EPOS.UI/Dialoge/Import/`) mit **vier Ausprägungen** für die VDI-3805-Blätter Heizkessel,
> Pufferspeicher, Solarkollektoren und Wärmepumpen (`KatalogImportProfil` als Satz je Katalog, `KatalogImportAblauf`
> als EIN Kern-Ablauf Lesen → Vorprüfen → Konfliktdialog → Ausführen, transaktional), `WaermebedarfAdminDialog`
> (Zwilling der Stromganglinien-Verwaltung aus W12, `GanglinienTextDatei.Lies(pfad, mitKopfzeile)` bereits mit dem
> Kopfzeilenschalter für W14b) und `PvModulImportDialog` (CEC-Katalog und `.pan`-Dateien, erstmals lokalisiert). Die
> eine Bausteinlücke — **Mehrfachmarkierung im `Raster`** — ist gebaut. Mit der Welle fallen die
> `ImportKonflikteHuelle` aus W12 (alle Aufrufer sind Razor) und die Sprungbrücke `WaermebedarfExternAdmin`
> (`WaermebedarfExternDialog` zeigt die Verwaltung als Überlagerung). **Alle sechs Masken sind im selben Commit wie
> ihr Nachfolger gelöscht** (Regel M1, ohne Nachzügler). Acht Sachcommits und ein Merge (`0711916` … `a59cbd5`),
> auf `ios_migration` als `01c9933`.
>
> **Der Nachweis der Welle sind die Importproben.** Für die fünf Parser, `DublettenPruefung` und `VdiAuswahlFilter`
> gab es keinen Test (W13‑B1); **zwanzig Probendateien** unter `Referenzlaeufe/Importproben/` (188 KB, CP1252 und
> CRLF per `.gitattributes` eingefroren) mit aus dem Bestand eingefrorenen Erwartungswerten entstanden **vor** der
> ersten Maske — Vaillant/Buderus-Heizkessel mit Wirkungsgrad-Rückfall, Pufferspeicher mit dem fehlenden zehnten Block
> (B23, wörtlich behalten), Solar mit allen vier Bauarten, Hoval-Wärmepumpen mit Voll-/Teillast-Trennung, 50
> CEC-Module, vier `.pan`, 8 760 Wärmebedarfswerte mit drei Gegenproben. 27 Abweichungen (A‑1…A‑27) mit je einem
> Windows-Abnahmepunkt, u. a.: Solar bekommt Dublettenprüfung und Konfliktdialog, alle vier Importe schreiben
> transaktional, die Übernahme liest aus den Detailfeldern, Wärmepumpen-Ordner `VDI_Waermepumpe` mit Rückfall.
> Drei neue Befunde: B56 (Wärmebedarf-Beschriftung nannte Komma, der Parser liest invariant), B57 (Trina-PAN ohne
> `Bifacial`-Schlüssel), B58 (`CEC Modules.csv` ist eine Semikolon-Fassung — unlesbar für den Dienst, die Probe stammt
> aus `_UTC`).
>
> **Nachweise** (auf dem gemergten Stand `01c9933`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **2 972** grün (2 807 nach W12), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **123** grün · Stapellauf **32** Masken (38 − 6) · SQL-Prüfer 1 241 Texte, 0 Fundstellen ·
> ChartProben 30 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte).
>
> **Protokoll** mit Feldkartenabgleich je Ausprägung, den Importproben, 27 Abweichungen und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W13_Blazor_Port_Protokoll.md`. **Anwenderfragen:** W13‑O‑2 (PAN
> ohne Temperaturkoeffizienten, B44), W13‑O‑3 (zwei Leistungsbegriffe, B40), W13‑O‑4 (fehlende Nachlaufblöcke, B23),
> W13‑O‑5 (Kühlleistung an Zuheizung gekoppelt, B32), W13‑O‑6 (zwei PV-Menüpunkte, eine Maske). **Windows-Abnahme
> steht aus** (§ 10 des Protokolls, zwölf Punkte). Der vierzehnte iOS-Lauf folgt auf diesem Stand.

> **Statusblock iU9 — Welle 12 umgesetzt (04.09.2026, Basis `73a4338` nach W11b, zusammengeführt mit `fe22915`)**
>
> **Sechs Masken — 2 134 Zeilen `.cs`, 1 409 Designer, 10 `MessageBox` + 13 indirekte — sind sechs
> Razor-Komponenten:** `GanglinieProtokollDialog`, `GanglinieImportOptionenDialog`,
> `ImportKonflikteDialog`, `StromganglinieAdminDialog`, `StromganglinieDialog` und
> `PeakShavingDialog`. **Der rote Faden ist die AP5-Importkette**, die zweimal wörtlich im Bestand
> stand (mit Ablage in der Stammdatenverwaltung, ohne in der Lastspitzenkappung) und jetzt EIN
> Kern-Ablauf `GanglinienImportAblauf` mit drei Rückrufen ist; die drei Zwischenmasken erscheinen
> als `Ueberlagerung` desselben Fensters, jeder Rückruf wartet auf eine `TaskCompletionSource`.
> **`ImportKonflikteDialog` ist Blatt vor Host MIT Hülle** (Entscheid § 8.3 der Vermessung): Vier
> seiner fünf Aufrufer bleiben bis W13 WinForms, und die `Sprungbruecke` kann keine Nutzlast
> zurückgeben — die Hülle kostet 80 Zeilen und lebt eine Welle. **Acht Dateien in den Kern:**
> `GanglinienImportAblauf`, `GanglinienOptionenModell`, `GanglinienProtokollText`,
> `ImportKonfliktModell`, `PeakShavingCtrl` (Umzug), `PeakShavingKennzahlenBlock`,
> `PeakShavingEingaben`, `PeakShavingBild`. **Bilanz 80 Dateien, +9 742 / −5 422 Zeilen** (ohne die
> 3,4 MB Probendateien). Sechzehn Sachcommits und ein Merge (`72dd8ba` … `34e2095`), auf `ios_migration` als `08c489a`.
>
> **Der Nachweis der Welle ist der bitgleiche Ganglinien-Import.** Dafür gab es KEINEN Test
> (Befund W12‑B14); die zwölf Proben — Trennzeichen `;`/`,`/Tab/einspaltig × Dezimaltrenner ×
> Kopfzeile × 8 760/35 040/525 600 × Schaltjahr × beide Sommerzeitfälle × `.xlsx` — entstehen
> deshalb ZUERST, mit aus dem Bestand eingefrorenen Erwartungswerten. Sie laufen danach durch den
> neuen Kern-Ablauf und liefern dieselben Zahlen. **Befund W12‑B27 dabei gefunden und behoben:**
> Der Excel-Zweig war überhaupt nicht benutzbar (drei Leseschleifen liefen um eine Zeile über das
> Feld hinaus, jeder `.xlsx`-Import endete in `IMPORT_PROT_LESEFEHLER`) — damit ist der offene
> Nachweispunkt `Umsetzung_iU0_iU1_Nachweise.md:136` erklärt und abgehakt.
>
> **Zwei Entscheidungen:** (1) **Kein neuer Renderer** für das Vorher/Nachher-Bild —
> `ChartRenderer.ErzeugerStapel` trägt seit W11a eine Sekundärachse und rechnet die
> Jahresstundenmarken über die Reihenlänge um; die ChartProben bleiben bei 30. (2) **Der Anker des
> Erreichbarkeitstests hängt jetzt an `Form_AdminSettings`** (`MDIMainForm → MenuItem_Einstellungen`):
> Von den zwölf Masken mit einem Pfad ab `Form_Start` fällt keine erst in W13/W14 (Befund W12‑B26),
> der Test kann seine Form „über die Startseite" nicht behalten. **Der Rechenlauf der
> Lastspitzenkappung läuft nebenher** (`Task.Run` + `Fortschritt`, Befund W12‑B22) — die dritte
> nebenläufige Rechnung der Anwendung.
>
> **Nachweise** (auf dem gemergten Stand, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **2 807** grün (2 614 nach W11b), **identisch unter
> `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün · Stapellauf **38** Masken (43 − 5),
> 37 erreichbar, 0 × „nein" · SQL-Prüfer 1 231 Texte, 0 Fundstellen · ChartProben 30 Bilder,
> 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) ·
> **Import-Proben byte-gleich**.
>
> **Protokoll** mit Feldkartenabgleich, 19 Abweichungen (A‑1…A‑19), den wörtlich übernommenen
> Befunden und fünf offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W12_Blazor_Port_Protokoll.md`.
> **Anwenderfrage W12‑O‑1:** Befund B5 — derselbe Katalogeintrag lässt sich einem Projekt beliebig
> oft zuordnen (heute wie früher). Soll das so bleiben? **Windows-Abnahme steht aus**: Verwaltung
> mit Einlesen (CSV `;`/`,`, `.xlsx`) samt Optionen/Protokoll/Konflikt, Startbild → Strom-Messdaten
> mit ◀/▶ und „Bearbeiten…", Lastspitzenkappung mit Balken, minimaler Schwelle, drei Reitern und
> CSV — auch ohne geöffnetes Projekt —, die vier W13-Importmasken über die Konflikthülle, de/en,
> 125 %, Esc je Ebene.

> **Statusblock iU9 — Welle 11b umgesetzt (04.09.2026, Basis `81a04ec` nach W11a, zusammengeführt mit `604d1f6`)**
>
> Der zweite Lauf der Welle 11: **`Form_Simulation_Detail` (7 766 Zeilen + 3 082 Designer), `DashboardForm`,
> `NavigatorUebersicht`, `NavigatorStrom`, `NavigatorWaerme` und `Form_SpeicherVariantenVergleich` → eine
> Razor-Seite `SimulationErgebnisSeite`** (`EPOS.UI/Seiten/Simulation/`) mit **zehn** Blättern (R3 „Simulation“
> war nur der Behälter der Menüliste, A‑1), dem Ergebnis-Blatt mit Autarkie-Analyse, den Ganglinien-Navigatoren
> Wärme/Strom und dem Variantenvergleich als Überlagerung; `TabNavigationManager`, `TabListMapper`,
> `DonutChartDrawer`/`Kacheln` gelöscht (`ChartManager` bleibt für Klimadaten und Peak-Shaving, A‑12).
> **Hosting-Entscheid R‑W11‑1:** Seite mit `SeitenZustand` (iOS erreicht sie über `AppWurzel`), auf Windows
> bis W16 in der modalen Dialoghülle, weil die Bedarfsobjekte der Startmaske gehören. Die Hülle fährt
> `SimulationLaufCtrl.Laufen` in `Task.Run` mit `Fortschritt` und Abbrechen; der Automatikstart beim Öffnen
> bleibt, Endlage Übersicht. **Sprungbrücke `SpeicherOptimierung`** (bleibt WinForms, iF22) mit Rückgabe
> `AuslegungUebernommen`; `SimulationKonfigSeite` (W10b) als Überlagerung — `SeitenZustand` wird **nicht**
> doppelt gebraucht. **Bilanz 78 Dateien, +11 159 / −27 103 Zeilen.** Vierzehn Sachcommits und ein Merge
> (`5ac1703` … `2c47cf0`), auf `ios_migration` als `73a4338`.
>
> **Der Ertrag ist eine WebView für das ganze Simulationsergebnis** — drei Navigationen und ~130
> Laufzeit-Steuerelemente sind ein `Reiter`; die 17 Zeichenflächen laufen über die sieben W11a-Bilder.
> **Anwenderentscheid W11a‑O‑1 umgesetzt (A‑19):** „Wärme gesamt“ ist die Summe der **Deckung** je Erzeuger,
> die Restwärme ist **eine** Zahl (`sim.Restwaerme`) und kann rechnerisch nicht negativ werden — 1030
> 6 137,56 − 6 137,56 = 0,00, 1007 6,04, 1017 0,00; Bedarf − Deckung trifft die Bilanzgröße in allen drei
> Projekten. Zehn Befunde in W11b behoben (u. a. zwei Fülllogiken für `chart2`, Heizstab in beiden
> Zweigen derselbe Anteil, Stromgang mit Sortiertumschalter, BHKW-Strom eigene Farbe, die elf Reitertitel
> erstmals englisch), 19 entfallen mit den Masken. Offen: W11b‑O‑1 (14 stille `Console.WriteLine`),
> O‑2 (17 Flächen ohne Foto des Bestands — Sichtabnahme am Gerät), O‑3 (erstes Blatt des Ergebnis-Reiters
> doppelt zur Übersicht?), O‑4 (`Form_SpeicherOptimierung` modal über der WebView), O‑5 (`IosProjektQuelle`
> liefert den Satz noch nicht).
>
> **Nachweise** (auf dem gemergten Stand `73a4338`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **2 614** grün (2 502 nach W11a), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **123** grün · Stapellauf **43** Masken (49 − 6), 42 erreichbar, 0 × „nein“ · SQL-Prüfer
> 1 233 Texte, 0 Fundstellen · ChartProben 30 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS,
> byte-gleich** (815 043 Werte).
>
> **Protokoll** mit Feldkartenabgleich je Reiter, 19 Abweichungen (A‑1…A‑19), 17 Windows-Abnahmewegen und
> sechs offenen Punkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W11b_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Automatikstart mit Balken und bedienbarem Fenster, Abbrechen, Endlage
> Übersicht, die 17 Flächen gegen ein Foto des Bestands, Konfiguration als Überlagerung mit Rücksprung,
> Variantenvergleich mit echtem Fortschritt, Optimierung modal über der WebView, die sechs CSV-Exporte, de/en,
> 125 %. Der zwölfte iOS-Lauf (33832613617) auf diesem Stand ist grün.

> **Statusblock iU9 — Welle 11a umgesetzt (04.09.2026, Basis `427fd59` nach W10a, zusammengeführt mit `a398c9a` nach W10b)**
>
> Die Welle 11 des Wellenplans (Simulationsergebnis: `Form_Simulation_Detail` mit elf Reitern,
> Dashboard, drei Navigatoren, Variantenvergleich — 11 031 Zeilen, 17 Zeichenflächen) läuft in zwei
> Läufen. **W11a** verlegt alles, was ohne Oberfläche geht, in den Kern und hängt die WinForms-Masken
> schon daran — **ohne eine Maske zu löschen**; W11b baut danach die Ergebnisseite in einem Schritt.
> Vermessung `iU9_W11_Vermessung.md` (1 734 Zeilen, 50 Befunde), Arbeitsanweisung
> `iU9_W11a_Arbeitsanweisung.md`. Acht Sachcommits und zwei Merges:
>
> | Commit | Inhalt |
> |---|---|
> | `d0b64b9` `fd1e750` | **W11a.1/2** `ErgebnisPraesenz` (public) und `Ganglinie` (Dauerlinie) im Kern; **elf** inline-SQL-Stellen der Welle als Controller-Methoden (`KonfigurationCtrl.LiesProjekt`, `HeizkesselStammCtrl.BrennstoffartenJeProjekt`, `WErzeugerCtrl.AnlagenJeTyp`, `StromspeicherStammCtrl.KapazitaetJeProjekt` …) |
> | `a2665db` `5ebecdf` | **W11a.3/5** `SimulationErgebnisCtrl` — die Reiterzahlen als **sieben DTOs**, die vier Eigenanteil-Rechnungen mit dem `SimulationRunner` geteilt (**eine Wahrheit**), `SpeicherKennzahlenBlock` (39 Zeilen); `SpeicherAnzeigeCtrl` (vier Kopien der Anzeigetexte → eine), CO₂-Faktoren in `EmissionsVorgaben`, Speicherkapazität über Controller |
> | `88fceb5` | **W11a.4** `SimulationLaufCtrl` (Vorprüfen, Bedarf, Bestücken, Laufen, Abbruchgrund, Speichern); `SimulationControl.Do_Simulation` mit `IProgress<LaufFortschritt>` (fünf Phasen) und `CancellationToken` — **die Detailansicht rechnet nebenläufig**, mit Balken und Abbrechen, statt das Fenster einzufrieren (W11‑B48); kein Lesevorgang musste vorgezogen werden (R‑W10a‑2 gilt) |
> | `52f76ae` `35a8d76` | **W11a.6/7** sieben Ergebnisbilder im `ChartRenderer` — `GanglinieNormiert`, `ErzeugerStapel` (mit zweiter Achse; trägt sechs der siebzehn Flächen), `Streuwolke`, `Ring`, `MonatsStapel`, `Temperaturverlauf` — **16 → 30 Proben**; Baustein **`Fortschritt`** |
> | `b8dfd01` `9f00c91` `c3c75c5` `8c9ecbe` | Merge W6–W10a-Nachweise; Protokoll und drei CLAUDE.md; Merge W10b (sieben Konflikte, u. a. beide Wellen hatten `LiesProjekt` — eine Fassung); Merge auf `ios_migration` |
>
> **Der Ertrag ist der Zahlenabzug.** 95 Kennzahlen je Projekt vor und nach dem Umbau verglichen:
> **92 unverändert**, drei geändert und begründet — die Restwärme rechnet jetzt wie der
> `SimulationRunner` (BHKW mitgezählt: Projekt 1030 Gesamtwärme 5 403 → 6 139 MWh, Restwärme
> 734 → −1,76 MWh; W11‑B35), der PV-Deckungsgrad ohne Strombedarf ist 0 statt `NaN` (B22).
> **Entscheid für den Anwender (W11a‑O‑1):** Restwärme auf ≥ 0 klemmen? Dazu W11a‑O‑2 (CO₂-Faktoren
> 0,42/0,20 wörtlich, `EmissionsVorgaben` hatte kein Gegenstück), O‑3 (Zusammenführung der vier
> Berichtsbilder mit den neuen), O‑5 (`KonfigurationCtrl` liest zwei Modelle — Netzverluste faktisch 0 %,
> Referenzstand wörtlich). Nebenbefund behoben: `TestDatenbank` kopierte 77 MB je Testfall.
>
> **Nachweise** (auf dem gemergten Stand `8c9ecbe`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **2 502** grün (2 379 nach W10b), **identisch unter
> `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün · Stapellauf **49** Masken (unverändert, keine
> Maske gelöscht) · SQL-Prüfer 1 233 Texte, 0 Fundstellen · **ChartProben 30 Bilder**, 0 Verstöße ·
> Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) — nach jedem Teilschritt geprüft.
>
> **Protokoll**: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W11a_Kern_Protokoll.md` (Zahlenabzug
> je DTO, Fadenprüfung, 30 Proben, § 11 Merge-Nachtrag). **Windows-Abnahme steht aus** (acht Punkte):
> nebenläufiger Start mit Balken, Abbrechen, Reiterlage nach dem Automatikstart, die drei geänderten
> Zahlen, 39 Kennzahlzeilen mit Ampelfarben, Variantenvergleich/Optimierung, Autarkie-Kachel, beide Sprachen.

> **Statusblock iU9 — Welle 10b umgesetzt (04.09.2026, Basis `427fd59` nach W10a, zusammengeführt mit `cd849f8`)**
>
> Der zweite Lauf der Welle 10: **`Form_Simulation_Config` mit ihren vier Teildateien
> (4 558 Zeilen), den drei Steuerelement-Klassen `ErzeugerKarte`/`SpeicherKarte`/`SchemaAnsicht`
> (2 121 Zeilen) und dem Zeichenmodell → eine Razor-Seite `SimulationKonfigSeite`** mit den
> Bausteinen **`Schema`** (das Hydraulikbild als SVG), **`ErzeugerKachel`** und **`SpeicherKachel`**;
> `SchemaModell` unverändert in den Kern, dazu `SchemaLayout` (die Anordnung headless prüfbar) und
> `Kaskade` (Platzlogik der `Tool_1…6`). Die sieben W10a-Dialoge hängen als Überlagerungen an der
> Seite; ihre Hüllen verlieren den Fensterweg. **Hosting-Entscheid R‑W10b‑1:** die Komponente ist
> eine Seite (`SeitenZustand`, Eintrag für `AppWurzel`), auf Windows bis W16 in der modalen
> Dialoghülle, weil beide Aufrufer (Startbild, Detailansicht) die modale Rückkehr brauchen.
> Arbeitsanweisung `iU9_W10b_Arbeitsanweisung.md`. Acht Sachcommits und ein Merge:
>
> | Commit | Inhalt |
> |---|---|
> | `2e75393` `93bd88f` | **W10b.0a/b** `SchemaModell` in den Kern, `SchemaLayout` neu; fünf inline-SQL in vier Controller, `Kaskade` und Quellenwahl im Kern, `KonfigurationCtrl.LiesProjekt` |
> | `cac6eb4` `4caee3f` | **W10b.0c/d** Baustein `Schema` (Knoten, Bézier-Kanten, Kaskadenband, Legende, Klick/Doppelklick, Tastatur), Bausteine `ErzeugerKachel`/`SpeicherKachel` (Chips mit sechs Stilen und sechs Zielen, Schwellenband als Inline-SVG) |
> | `dd132ff` | **W10b.1** die Seite: zwei Kartenspalten, Umschalter Liste/Schema mit erhaltener Auswahl, zwei eigene Überlagerungsebenen (Betriebsmodus, WP-Priorität, Quellenwahl, Wärmesenke, Pufferverwaltung; Quelle Pufferspeicher, Quellprofil, Erdreich), Fußzeile mit Sofortschaltern — vier Teildateien und drei Controls gelöscht |
> | `d75908c` `6bea64e` `a91ba2a` | **W10b.2–4** Befund W10b‑B42 (`DatenzugriffTests` ohne Sammlungsmarke riss DB-Tests mit), Formularkarte, Protokoll, vier CLAUDE.md, iOS-Einstieg (`IProjektQuelle.SimulationKonfigGaben` mit Standardumsetzung) |
>
> **Der Ertrag ist eine WebView für die ganze Konfiguration.** Drei Navigationen und ~9 000 Zeilen
> WinForms sind eine Seite mit **einem** `Neuladen()` (statt neun Auffrischungsstellen, W10‑B40);
> die Kette Seite → Quelle/Senke → Pufferverwaltung → Klimazonenkarte läuft in **einem** Fenster,
> Esc schließt je Ebene; `SeitenZustand` wird genau einmal gebraucht. Alle Befunde W10‑B33…B40
> erledigt, dazu W10b‑B41 (`listErzeuger` ohne Leser) und B42. Ein Entscheid für den Anwender:
> soll „Speichern“ erst nach einer Änderung aktiv werden (W10b‑O‑3)? Das Schema ist ohne
> Bildvergleich portiert (W10b‑O‑1) — Sichtabnahme gegen ein Foto des Bestands.
>
> **Nachweise** (auf dem gemergten Stand `a91ba2a`, Linux): Build → 0 Fehler, **12** Warnungen
> (17 nach W10a; fünf WFO1000 der gelöschten Karten) · `dotnet test WP-Plan.Kern.slnf` → **2 379**
> grün (2 284 nach W10a), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün ·
> Stapellauf **49** Masken (50 − 1), 48 erreichbar, 0 × „nein“ · SQL-Prüfer 1 239 Texte,
> 0 Fundstellen · ChartProben 16 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS,
> byte-gleich** (815 043 Werte) · `dotnet publish` mit `wwwroot` samt neuer CSS.
>
> **Protokoll** mit 13 Abweichungen (A‑1…A‑13), 16 Windows-Abnahmewegen und sieben offenen
> Punkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W10b_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: eine WebView für alles, Chipfolge je Anlagentyp, Schema gegen das
> Foto des Bestands, synchrone Auswahl in beiden Ansichten, drei Überlagerungsebenen mit Esc je
> Ebene, Rücksprung aus `Form_Simulation_Detail`, de/en. Der elfte iOS-Lauf (33826084944) auf
> diesem Stand ist grün.

> **Statusblock iU9 — Welle 10a umgesetzt (03.09.2026, Basis `04fc474` nach W9, zusammengeführt mit `b6a72b0`)**
>
> Die Welle 10 des Wellenplans (Simulationskonfiguration, 12 361 Zeilen) ist die größte des Pakets
> und läuft deshalb in zwei Läufen. **W10a** portiert die **sieben Dialoge**, die aus
> `Form_Simulation_Config` heraus geöffnet werden: Betriebsmodus, Klimazonenkarte, Quelle Erdreich,
> Pufferverwaltung, Quelle Pufferspeicher, Quellprofil und Wärmesenke → **sieben Razor-Komponenten**
> in `EPOS.UI/Dialoge/Simulation/`, jede WinForms-Fassung gelöscht (Regel M1), 7 803 Zeilen
> Oberflächencode, 30 `MessageBox`. `Form_Simulation_Config` bleibt bis W10b WinForms und ruft die
> Dialoge über Hüllen. Arbeitsanweisung `iU9_W10a_Arbeitsanweisung.md`, Vermessung
> `iU9_W10_Vermessung.md` (1 887 Zeilen, 40 Befunde). Fünfzehn Sachcommits und ein Merge:
>
> | Commit | Inhalt |
> |---|---|
> | `352f349` `cbfccb1` `7aae643` | **W10a.0a–c** `SenkeAnzeige`/`IstPufferZiel` in den Kern (sonst bräche der Bau beim Löschen der Senkenmaske); Kapazitätsformel, Katalog-SQL, Sondenmeter, Ergebniszuordnung und Profilparser im Kern; Sprungziel `PufferSpAdminNurLesen` |
> | `ef513e6` `53240c4` `6fe8656` | **W10a.0d–f** `ChartRenderer.Jahresgang` (zweireihig, Monatsachse) mit Probe; Baustein **`Bildkarte`** (PNG mit SVG-Klickflächen) samt `KlimazonenPfade` (15 Zonen, zur Bauzeit erzeugt) und dem Kartenbild unter `EPOS.UI/wwwroot/bilder/`; `WertAbfrage` |
> | `18ac6e1` `b34a6d3` `033d0b9` | **W10a.1–3** `BetriebsmodusDialog`, `KlimazonenkarteDialog` (Überlagerung), `QuelleErdreichDialog` (Kollektor/Sonde, VDI-4640-Prüfung, **asynchroner** Simulationslauf aus dem Dialog) |
> | `781e463` `a6d15e5` `82aad99` `97ff674` | **W10a.4–7** `PufferSpProjektDialog` (Klassen-Set, Schwellen, Schichtung, Ladereihenfolge — das Blatt aller drei Absprünge), `QuellePufferspeicherDialog`, `QuellprofilDialog` (virtualisiertes 8 760-Zeilen-Raster), `WaermesenkeDialog` (Senkenliste mit Rang, Parallelverbund, Ladeverhalten) |
> | `630a56b` `e69df40` `427fd59` | **W10a.8–11** Ressourcen, Formularkarte, Protokoll, drei CLAUDE.md, Nachweise auf dem gemergten Stand |
>
> **Der Ertrag ist die Klickkarte, die zum ersten Mal funktioniert.** Die WinForms-Klimazonenkarte
> konnte ihre ausgelieferte SVG **nie** lesen (W10a‑B41: der Parser erwartete den Pfadbefehl getrennt
> von der ersten Koordinate, `float.Parse("M315.30")` warf, ein leerer `catch` verschluckte es) — die
> Maske zeigte immer nur ihre Ladefehlerzeile. Die Blazor-Fassung stellt die Zonenwahl per Klick her.
> Zwei Proben haben die Bauweise bestimmt: `SimulationRunner.Simuliere` läuft in `Task.Run` fehlerfrei
> gegen die Testdatenbank (R‑W10a‑2, deshalb rechnet der Erdreichdialog asynchron mit Wartezustand),
> und das Kartenbild misst 1,29 MiB (R‑W10a‑3, nicht verkleinert). 18 Befunde behoben, 8 wörtlich
> übernommen und als Entscheid für den Anwender notiert (W10a‑O‑1…O‑7), dazu ein nicht
> reproduzierter Einzelausfall der Testsuite unter `en_US` (W10a‑O‑8, Frist der `WaitForAssertion`).
>
> **Nachweise** (auf dem gemergten Stand `427fd59`, Linux): Build → 0 Fehler, **17** Warnungen
> (20 nach W9; drei WFO1000 gingen mit den Designern) · `dotnet test WP-Plan.Kern.slnf` → **2 284**
> grün (2 066 nach W9), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün ·
> Stapellauf **50** Masken (55 − 5; Quellprofil und Wärmesenke hatten keinen Designer), 49 erreichbar,
> 0 × „nein“ · SQL-Prüfer 1 240 Texte, 0 Fundstellen · **ChartProben 16 Bilder**, 0 Verstöße ·
> Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · `dotnet publish` mit `wwwroot`
> samt Kartenbild.
>
> **Protokoll** mit Feldkartenabgleich (7 Masken), 17 Abweichungen (A‑1…A‑17), 20 Windows-Abnahmewegen
> und acht offenen Punkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W10a_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: das Kartenbild in der veröffentlichten `wwwroot`, der asynchrone
> Simulationslauf bei einem großen Projekt, die Pufferverwaltung ohne Abbrechen, der Kesselzweig, der
> die WP-Vorgaben unberührt lässt, die englische Oberfläche mit unübersetzten Steuerwerten.

> **Statusblock iU9 — Welle 9 umgesetzt (03.09.2026, Basis `8995d3e` nach W8, zusammengeführt mit `1cf3dbf`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W9, Arbeitsanweisung
> `iU9_W9_Arbeitsanweisung.md` (Scratchpad der Sitzung): **acht Masken der Bedarfskacheln des
> Startbilds → fünf Razor-Komponenten** in `EPOS.UI/Dialoge/Bedarf/` — Gebäudekatalog auf zwei
> Reitern (`Form_Gebaeude1` + `Form_Gebaeude2`), Gebäudeverwaltung mit Admin-Modus, Wohnflächendialog,
> externer Wärmebedarf mit Kanalwahl und **ein** Dialog `BedarfsProfileDialog` für die Drillinge
> Prozesswärme, Stromverbraucher und Brauchwasser; jede WinForms-Fassung gelöscht (Regel M1),
> 3 289 Zeilen Oberflächencode, 42 `MessageBox`. Mit den Assistentenseiten 2 bis 5 laufen **zehn der
> dreizehn Assistentenseiten** als Razor-Komponenten; alle elf Kacheln des Startbilds sind Blazor.
> Fünfzehn Sachcommits und ein Merge:
>
> | Commit | Inhalt |
> |---|---|
> | `b24e5da` | **W9.0a** Assistentenseiten mit beliebigem Listentyp (`IAssistentListenSeite<T>`, vier Listentypen statt nur `WErzeugerModel`) |
> | `53aa3f1` `af4dba5` `fd97501` `01701c6` `04ce2ba` | **W9.0b–f** Katalogfilter, Baualtersklassen und Bauart-Ableitung der Gebäudemasken im Kern; **`Ferienzeit`** (Jahrestag ↔ Tag/Monat, vier Prüfregeln); die fünf Projektlisten der Bedarfsgewerke als Controller-Methoden statt inline-JOINs in Startbild und Kontextmenüs; **ein** Suchmuster im Haus (Wildcard-Filter aus W7 verallgemeinert); Sprungziel `WaermebedarfExternAdmin` |
> | `e384853` `7f59398` `f960151` | **W9.3/W9.1/W9.2** `GebaeudeWohnflaecheDialog`, `GebaeudeKatalogDialog` (zwei Reiter, drei Modi, 78 Felder), `GebaeudeDialog` (Host mit Wohn-/Nichtwohn-Umschalter, vier Filterzweigen, Wildcard-Suche, Admin-Modus, Assistentenseite 2) |
> | `ae1c097` `3c89b6c` `59d984d` | **W9.4/W9.5** `WaermebedarfExternDialog` (Kanal je Zuordnung, Assistentenseite 3), `BedarfsProfileDialog` (Ausprägung `BedarfsArt`, Simulation in der Hülle, W8-Blätter als Überlagerungen, Assistentenseiten 4 und 5), Brauchwasser-Überlagerung im Gebäudekatalog |
> | `ecbbdc0` `6c174e3` `d04a056` | **W9.6–W9.8** 207 Textschlüssel de/en, Formularkarte-Tests (Anker auf `Form_Stromganglinie` und `Form_PufferSp_Bearbeiten` umgehängt), Protokoll, drei CLAUDE.md, STAND.md |
> | `04fc474` | Merge `origin/ios_migration` (Statusblock W8, de-DE-Festlegung der Kurvennamen-Tests) |
>
> **Der Ertrag sind die Projektlisten im Kern und die generische Assistentenseite.** Fünf
> inline-JOINs, die in Startbild, Kontextmenü und Gebäudekatalog je dreimal wortgleich standen,
> sind fünf Controller-Methoden; die Assistentenschnittstelle aus W6 trägt jetzt jeden Listentyp.
> **Vier Befunde behoben:** die Checkbox „Dezentrale Warmwasserbereitung“ wurde gezeigt und nie
> gespeichert (W9‑B3, stiller Datenverlust, A‑2); zwei ungesicherte `Double.Parse` (B4); in
> englischer Oberfläche lief der Verwendungsfilter ins Leere, weil der Steuerwert übersetzt wurde
> (B8); „Überschreiben“ traf nach Umbenennen keine Zeile (B9). **Drei wörtlich übernommen, Entscheid
> beim Anwender** (W9‑O‑1…O‑3): der Filterzweig ohne Verwendung, die Bauweise am Index der
> Gebäudeart-Liste, kWh gegen MWh in derselben Meldung. Dazu W9‑O‑4 (darf „Überschreiben“
> umbenennen?), W9‑O‑5 (Admin-Modus des Katalogeditors hat keinen Aufrufer) und W9‑O‑7
> (Speicherbedarf von zehn WebViews im Assistenten).
>
> **Windows-Läufer seit W8 mit en-US:** Drei Kern-Tests der Welle 8 verglichen deutsche
> Ressourcentexte und fielen auf dem Windows-Läufer (Lauf 33801244655); seit `1cf3dbf` legt jeder
> Texttest die Oberflächensprache fest, und die Wellen laufen zusätzlich unter `en_US` durch.
>
> **Nachweise** (auf dem gemergten Stand `04fc474`, Linux): Build → 0 Fehler, **20**
> Warnungen · `dotnet test WP-Plan.Kern.slnf` → **2 066** grün (1 906 nach W8; +98 bunit,
> +62 Kern), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün · Stapellauf
> **55** Masken (63 − 8), 54 erreichbar, 0 × „nein“ · SQL-Prüfer 1 241 Texte, 0 Fundstellen ·
> ChartProben 15 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich**
> (815 043 Werte) · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (8 Masken, Drillinge je Ausprägung), 15 Abweichungen
> (A‑1…A‑15), Windows-Abnahmeliste mit 14 Aufrufwegen und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W9_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: die vier Filterkombinationen und die Wildcard-Suche, „Ändern“
> schreibt vier Werte zurück, der Katalogeditor über zwei Reiter mit Ferienregeln, „Brauchwasser…“
> als Überlagerung, Gebäudeverwaltung ohne Projektteil, Kanalwahl beim externen Wärmebedarf, die
> drei Bedarfsblätter mit Simulation → Ergebnis → DB ändern/neu → Typ ändern, **Assistent Seiten 2–5
> mit Speichermessung** (zehn WebViews), de/en, 125 %, Finger/Maus, Esc/Tab.

> **Statusblock iU9 — Welle 8 umgesetzt (03.09.2026, Basis `e5114e1` nach W7, zusammengeführt mit `e74136e`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeilen W8a/W8b, Arbeitsanweisung
> `iU9_W8_Arbeitsanweisung.md` (Scratchpad der Sitzung): **zehn Masken der Bedarfstypen → vier
> Razor-Komponenten** in `EPOS.UI/Dialoge/Bedarf/` — die drei Ergebnismasken, die drei
> Stammkopfmasken und die drei Typprofilmasken der Drillinge Prozesswärme, Stromverbraucher und
> Brauchwasser werden **je eine** Komponente mit der Ausprägung `BedarfsArt`, dazu der Gebäudetyp;
> jede WinForms-Fassung gelöscht (Regel M1), 41 `MessageBox`. Die vier Komponenten sind die
> Blätter, die Welle 9 (Bedarfsmasken vom Startbild) als Überlagerungen einhängt. Elf Sachcommits
> und ein Merge:
>
> | Commit | Inhalt |
> |---|---|
> | `e9d7ad6` `fec0a20` `b1d8a4b` | **W8.0a/b/d** `ProzesswaermeStammCtrl` auf den Schnitt seiner Zwillinge; `BedarfsArt`, `BedarfStammCtrl` und `TypProfilCtrl` im Kern (eine Datenseite für drei Kataloge); `TagVCtrl` trägt die Gebäudetyp-Verwaltung |
> | `c046c07` | **W8.0c** drei Bedarfsbilder im `ChartRenderer` (**Monatssäulen**, **Stundenprofil**, **Jahresverlauf**) mit drei Proben |
> | `34e69ff` `1e9c8fc` `6b65f2e` `2119e18` | **W8.1–W8.4** `BedarfErgebnisDialog` (eingefrorenes Rechenobjekt als DTO), `TypStammDialog`, `TypProfilDialog` (Tag kopieren/einfügen wirkt jetzt, Befund B1), `GebaeudetypDialog` — zehn Masken gelöscht |
> | `cbb358e` `04dd413` `51c806d` | **W8.5–W8.7** 143 Textschlüssel de/en, Formularkarte-Tests, Protokoll, drei CLAUDE.md, STAND.md |
> | `8995d3e` | Merge `origin/ios_migration` (Statusblock W7) |
>
> **Der Ertrag ist die eine Datenseite für drei Kataloge.** Drei Zwillingsdialoge je Blatt mit je
> eigenem Aufbaucode sind eine Komponente mit Ausprägung, die Schreibwege laufen in **einer**
> Transaktion (A‑9), die drei Charts sind drei Renderer-Bilder mit Proben. Zwei Befunde des
> Bestands sind behoben (Tag kopieren/einfügen ohne Wirkung, „Novmember“), einer bleibt als
> **Frage an den Anwender**: `Form_Brauchwasser_Admin` öffnet die **Prozess**-Ansicht des
> Ergebnisdialogs (W8‑O‑3, wörtlich übernommen), und im Brauchwasser-Ergebnis steht ein Teiler
> 1000, den die beiden Zwillinge nicht haben — **eine der Anzeigen ist um den Faktor 1000
> daneben** (W8‑O‑5).
>
> **Nachweise** (auf dem gemergten Stand `8995d3e`, Linux): Build → 0 Fehler, **20**
> Warnungen · `dotnet test WP-Plan.Kern.slnf` → **1 906** grün (1 820 nach W7; +66 bunit,
> +20 Kern) · Formularkarte **123** grün · Stapellauf **63** Masken (73 − 10), 61 erreichbar,
> 0 × „nein“ · SQL-Prüfer 1 254 Texte, 0 Fundstellen · **ChartProben 15 Bilder**, 0 Verstöße
> · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · `dotnet publish` mit
> vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich je Ausprägung, 14 Abweichungen (A‑1…A‑14),
> Windows-Abnahmeliste mit 13 Punkten und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W8_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Startreiter und Sicht des Brauchwasser-Ergebnisdialogs,
> „Überschreiben“ nach „Speichern unter“, Tagwechsel im Typprofil verwirft nicht übernommene
> Eingaben, stiller Kurvenübertrag im Gebäudetyp, fünf bzw. acht Kurvennamen nach Kurvenzahl,
> de/en, 125 %, Finger/Maus, Esc/Enter je Dialog.

> **Statusblock iU9 — Welle 7 umgesetzt (03.09.2026, Basis `198506f` nach W6, zusammengeführt mit `98ebe81`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W7, Arbeitsanweisung
> `iU9_W7_Arbeitsanweisung.md` (Scratchpad der Sitzung): **acht Masken der Gewerke
> Wärmepumpe (5) und Solarthermie (3) → acht Razor-Komponenten** in
> `EPOS.UI/Dialoge/Waermepumpe/` und `EPOS.UI/Dialoge/Solarthermie/`, jede WinForms-Fassung
> gelöscht (Regel M1) — 3 065 Zeilen Oberflächencode, 43 `MessageBox`. Zwei davon sind die
> Assistentenseiten 7 und 8; damit laufen **sechs der dreizehn Assistentenseiten** als
> Razor-Komponenten. Sechzehn Sachcommits:
>
> | Commit | Inhalt |
> |---|---|
> | `2cd898a` | **W7.0a** `WPCtrl` (Projektgeräte `Tab_WP`) aus der Anwendung in den Kern; `FillListBox` entfällt |
> | `0872196` `7fc9419` `0d1e6e4` `7da4f33` `808837b` | **W7.0b–f** Katalogzeile und Katalogfilter im Kern (die Filterlogik des Wärmepumpen-Katalogs, testbar ohne Oberfläche), **`ChartRenderer.Kennlinien`** mit zwei Proben (COP und Leistung über der Außentemperatur, eine Reihe je Vorlauf), `KenndatenCtrl.Abgleichen` (Kennlinien-Rückschreiben in **einer** Transaktion statt RowState-Schleife), sieben Datenwege in Kern-Controller, Sprungziel `SolarganglinieAdmin` |
> | `29a1bf3` `555e770` | **W7.1/W7.2** die Blätter `WaermepumpenKatalogDialog` (Filterleiste mit Wildcard-Suche) und `KennlinienEditorDialog` (Stützstellen je Vorlauf) — beide nur als Überlagerung |
> | `5e71c49` | **W7.6** `SolarkollektorKatalogDialog` |
> | `b30f6bd` `b98cf35` `a371328` | **W7.3–W7.5** `WaermepumpeStammDialog` (zwei Kennlinienbilder, Wärme/Kühlung), `WaermepumpeAnlageDialog` (47 Felder, Bivalenzlogik, Kostenzeile) und `WaermepumpenDialog` (Host mit **vier Ebenen** Überlagerung: Verwaltung → Anlage → Stammdialog → Kennlinien-Editor) |
> | `0ad0a59` `3655bce` | **W7.7/W7.8** `SolarkollektorenDialog` (Assistentenseite 8) und `SolarganglinieDialog` |
> | `35188f7` `0077533` `e5114e1` | **W7.9–W7.11** 157 Textschlüssel de/en, Formularkarte-Tests (Prüfmuster `Wizard_WPItem`, Sprungtabellen-Test auf `Form_AdminStromspeicher`), Protokoll, drei CLAUDE.md, STAND.md |
>
> **Der Ertrag ist die Kennlinie im Kern.** Vier WinForms-Charts mit je eigenem
> Aufbaucode sind **eine** Renderer-Methode mit zwei Proben; Wärme und Kühlung
> (`Tab_Kenndaten_STAMM`, `Tab_Kenndaten_Kuehlung_STAMM` mit `MAX(Last)`) laufen über
> dieselbe Datenseite. Der Projektgeräte-Controller `WPCtrl` liegt jetzt im Kern — bis W7 der
> letzte Erzeuger-Controller in der Anwendung.
>
> **Ein echter Befund (W7‑O‑4, behoben):** „Bearbeiten" im Kontextmenü der WP-Liste schrieb
> `Regelung = Leistungsstufen`, und `Leistungsstufen` wird im ganzen Bestand nie gesetzt —
> jedes Bearbeiten aus dem Kontextmenü **löschte die Leistungsstufen** des Geräts. Dazu
> zwei Entscheide für den Anwender: die Baujahrliste (2024 doppelt, 2022 fehlte; mit A‑15
> lückenlos) und die nie greifende Vorlauf-/Rücklaufprüfung der Solarkollektoren (W7‑O‑5).
>
> **Nachweise** (auf dem gemergten Stand `e5114e1`, Linux): Build → 0 Fehler, **20**
> Warnungen · `dotnet test WP-Plan.Kern.slnf` → **1 820** grün (1 636 nach W6; +155 bunit,
> +29 Kern) · Formularkarte **123** grün · Stapellauf **73** Masken (81 − 8), 71 erreichbar,
> 0 × „nein" · SQL-Prüfer 1 272 Texte, 0 Fundstellen · **ChartProben 12 Bilder**, 0 Verstöße
> · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · `dotnet publish` mit
> vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (8 Masken), 30 Abweichungen (A‑1…A‑30),
> Windows-Abnahmeliste mit 13 Punkten und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W7_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: die vier Überlagerungsebenen mit Esc und Fokusfalle je
> Ebene, die Kennlinienbilder gegen die alten Charts (der Bildvergleich ist mit iF23 gelöscht),
> Wärme/Kühlung, „Kosten bearbeiten…" als zweites Fenster, Assistentenseiten 7 und 8 (jetzt
> sechs WebViews im Assistenten), beide Solarthermie-Zweige, W7‑O‑4 auf einer Anlage mit
> gepflegter Regelung.

> **Statusblock iU9 — Welle 6 umgesetzt (03.09.2026, Basis `740c73e`, zusammengeführt mit W5 `ddaea70` und iF22–iF28 `f7fefdf`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W6, Arbeitsanweisung
> `iU9_W6_Arbeitsanweisung.md` (Scratchpad der Sitzung): **sieben Masken der
> Erzeugerkacheln → sieben Razor-Komponenten** in `EPOS.UI/Dialoge/Erzeuger/`, jede
> WinForms-Fassung gelöscht (Regel M1) — 4 202 Zeilen Oberflächencode, 55 `MessageBox`.
> **Vier davon sind zugleich Assistentenseiten** (PV, Stromspeicher, Heizkessel, BHKW),
> die ersten Razor-Komponenten im Assistentenrahmen. Vierzehn Sachcommits:
>
> | Commit | Inhalt |
> |---|---|
> | `c825649` | **W6.0a/b** `EnergietraegerVarianteCtrl.Anlegen` (die 185 Zeilen Trägeranlage aus Heizkessel und BHKW, zweimal wortgleich, jetzt **einmal** im Kern, eine Transaktion), `VariantenDerGruppe`, `TraegerUmhaengen` |
> | `68f3634` | **W6.0c** Katalogfilter und Detailblöcke der fünf Projektdialoge in die Stamm-Controller (Heizkessel, BHKW, Photovoltaik, Pufferspeicher); `PufferSpFilter` aus der App in den Kern |
> | `9991259` | **W6.0d** vier Sprungziele (`HeizkesselAdmin`, `StromspeicherAdmin`, `PvAdmin`, `PufferSpAdmin`) — die Katalogverwaltungen bleiben WinForms bis W14a |
> | `ca73a39` | **W6.0f** `KostenKnoepfeLeiste.razor` — der KD6-Kostenblock als Razor-Teilstück; die Ziele sind selbst Blazor-Hüllen und öffnen als **zweites Fenster** (A‑1, wie W4‑O3) |
> | `8fc101e` | **W6.0e** `BlazorAssistentSeite<T>` + `IAssistentErzeugerSeite`: eine randlose, `TopLevel=false`-taugliche Hüllenform mit einer verzögert gebauten WebView; `WizardParent` bedient die vier Seiten über **einen** Zweig statt vier |
> | `bd9151f` `dd11c2b` | **W6.1/W6.2** die Katalogeditoren `HeizkesselKatalogDialog` (42 Felder, 3 Speicherwege) und `BhkwKatalogDialog` (58 Felder, abgeleitete Investition, Katalogsatz-Rückfrage) |
> | `448d4c5` `1bb2c19` `ef28099` | **W6.3/W6.4** die Hosts `HeizkesselDialog` und `BhkwDialog` — Trägerwahl, Katalogeditor und Namensdialog als Überlagerungen im selben Fenster; `ErzeugerAuswahlDaten.cs` als gemeinsame Datenform (`Schluessel` ≠ `GeraetId`: zwei gleiche Kessel teilen eine Projektkopie) |
> | `329a1be` `fa670fc` `6e2a2f5` | **W6.5–W6.7** `PhotovoltaikDialog`, `StromspeicherDialog`, `PufferspeicherDialog` (Eindeutigkeitsrückfrage als `Rueckfrage`-Baustein statt `Dienste.Dialog`) |
> | `18a3eb9` | **W6.8/W6.10** Ressourcen-Sammelnachtrag, Protokoll, drei CLAUDE.md, STAND.md |
>
> **Der Ertrag ist die Assistentenseite.** Bis W5 saß jede Razor-Komponente in einem
> modalen Fenster oder in einer `BlazorSeite` einer bestehenden Maske. Der Assistent
> hält seine 13 Seiten als `Func<Form>` und zeigt sie randlos in seinem Panel — dafür
> brauchte es eine **Form**, die eine WebView trägt und erst beim Anzeigen baut
> (Risiko R5: vier WebViews im Voraus). `AssistentSeiten.ERZEUGER[9..12]` zeigen jetzt
> auf `BlazorAssistentSeite<…>`; Welle 7 hängt Wärmepumpe und Solar auf demselben Weg ein.
>
> **Zwei Befunde für den Anwender** (W6‑O‑1, W6‑O‑2, wörtlich übernommen nach Regel F3):
> die Gruppen→`Brennstoff`-Ketten von Heizkessel und BHKW sind uneinheitlich („Sonstige"
> trifft beim Heizkessel nie, ist auf `23` = Fernwärme abgebildet; Fernwärme und
> Wasserstoff fehlen der Kesselkette), und die Filterstufe „Alle" (`Ptherm Like '%'`)
> lässt Katalogsätze ohne Ptherm herausfallen. Vorschlag: künftig über
> `Tab_Brennstoff_Stamm.ID_Kategorie` filtern, dann gibt es die Ketten nicht mehr.
>
> **Nachweise** (auf dem gemergten Stand `198506f`, Linux): `dotnet build WP-Plan.sln
> -c Release -p:Platform=x64` → 0 Fehler, **20** Warnungen · `dotnet test
> WP-Plan.Kern.slnf` → **1 636** grün (1 485 nach W5; +91 bunit, +27 Kern-Tests für die
> neuen Controller-Methoden gegen `Kenndaten_Test.sqlite`) · Formularkarte **123** grün
> (Anker von Heizkessel/BHKWEing auf Klimadaten, Gebäude und Brauchwasser umgehängt) ·
> Stapellauf **81** Masken (88 − 7), 79 erreichbar, 0 × „nein", 0 × „verwaist" ·
> SQL-Prüfer 1 283 Texte, 0 Fundstellen (Prüfer: lokale Variablen werden nicht mehr gegen
> fremde Konstanten aufgelöst, W6‑O‑4) · ChartProben 10 Bilder, 0 Verstöße ·
> Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` **PASS, byte-gleich**
> (815 043 Werte) · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (7 Masken), 20 Abweichungen (A‑1…A‑20),
> Windows-Abnahmeliste mit zehn Aufrufwegen und sechs offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W6_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: die fünf Startkacheln, das Kontextmenü der
> Übersichtslisten (auch REF-Liste), Assistentenseiten 9–12 (Wechsel unter 1 s, kein
> Aufblitzen, Speicher der Browserprozesse), Heizkessel-/BHKW-Admin → Bearbeiten/Neu,
> die Sprungbrücke in die vier Katalogverwaltungen (W2‑7) und die Kostenleiste als
> zweites Fenster.

> **Statusblock iU9 — Welle 5 umgesetzt (03.09.2026, Basis `740c73e`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W5: **sechs Masken →
> sechs Razor-Komponenten**, jede WinForms-Fassung gelöscht (Regel M1). Es ist die
> **erste Welle mit Seiten statt Dialogen** — der ganze Reiter „Berichte & Kosten"
> der Startmaske ist jetzt Blazor, in **einer** WebView. Zehn Commits:
>
> | Commit | Inhalt |
> |---|---|
> | `d95283c` | **W5.0** Bausteinlücken 9–11: `Allgemein/Blazor/BlazorSeite.cs` (nicht-modale Hülle), `EPOS.UI/Dienste/SeitenZustand.cs` (Projektwechsel ohne Neuaufbau der WebView), `Bausteine/Reiter.razor` + `Reiterblatt.razor`, `Kachelraster.razor`, `Kennzahlkachel.razor`; dazu der Nachzug von **A‑17 (W3)** und **A‑2 (W4)** — Kostenprofil, Kostenverwaltung und Trägerkarte bekommen ihre Reiterform zurück |
> | `a39fe13` | **W5.1** `Form_BkUebernahme` → `Dialoge/Berichte/BkUebernahmeDialog.razor` (ein Dialog, zwei Füllungen — Wertgegenüberstellung oder Klartext) |
> | `cd4213d` | **W5.2** `UcBericht` (508 Z.) → `Seiten/Berichte/BerichtSeite.razor` |
> | `bf38fa6` | **W5.3** `UcWirtschaftlichkeit` (831 Z.) → `Seiten/Berichte/WirtschaftlichkeitSeite.razor`; die **fünf Unterdialoge** stehen jetzt in Überlagerungen desselben Fensters (W4‑O3 erledigt) |
> | `47ea9e3` | **W5.4** `UcBkKosten` (1 311 Z., K4) → `Seiten/Berichte/KostenSeite.razor` |
> | `8ea1e2e` | **W5.5** `UcBkUebersicht` (1 552 Z., K4) → `Seiten/Berichte/UebersichtSeite.razor` |
> | `f59aed1` | **W5.6a** Sieben Hüllen liefern ihren Parametersatz (`Gaben`) — Voraussetzung dafür, dass ein Blazor-**Wirt** seine Unterdialoge ohne zweite WebView zeigt |
> | `ff4e6f7` | **W5.6** `UcBerichteKosten` (810 Z., K4) → `Seiten/Berichte/BerichteKostenSeite.razor`; `Form_Start.tabPage6` trägt eine `BlazorSeite<T>`; sechs Masken gelöscht, fünf Windows-Datenseiten neu |
> | `f5d660f` | **W5.7** Ressourcen-Sammelnachtrag: 34 Schlüssel (`BKS_*`, `WIRT_*`) in `Resource.resx` und `Resource.en-US.resx`; `help_mapping.txt` |
> | `f39b4a3` | **W5.8** Formularkarte: Zähler 91 → 88, achtes Prüfmuster (`UcBericht` — einziger Beleg für die `CheckedListBox`) |
>
> **Die Seiten-Hülle ist der Ertrag.** `BlazorDialogForm<T>` ist ein eigenes modales
> Fenster; eine SEITE sitzt in einer vorhandenen Maske und bleibt. `BlazorSeite<T>`
> ist deshalb ein `UserControl` mit denselben `CreationProperties` — insbesondere
> demselben `UserDataFolder`, also **einem gemeinsamen Browserprozess**. Die vier
> Seiten laufen in **einer** WebView (Risiko **R5**); umgeschaltet wird in der
> Komponente. Der Projektwechsel läuft über `SeitenZustand`: ein Objekt mit
> Änderungsereignis, damit die WebView **nicht** neu gebaut wird.
>
> **DPI bleibt offen (Risiko R4, Entscheid iF21).** Die `DpiInsel` der Dialoghülle
> wirkt nur für einen modalen Lauf mit eigenem Fenster. Eine eingebettete Seite
> sitzt im Fenster der DpiUnaware-`Form_Start` und wird bei 125–200 % bitmapskaliert;
> ein Fenster kann seinen DPI-Kontext nachträglich nicht wechseln. `BlazorSeite`
> versucht es deshalb **gar nicht erst**, dokumentiert den Befund und setzt
> `DefaultBackgroundColor` gegen das weiße Aufblitzen. **Die Schärfe der Seiten ist
> damit ein Abnahmepunkt, keine Zusage** — und der eigentliche Entscheid der Welle
> (W5‑O1).
>
> **H11 entfällt.** Die 110 Zeilen Messcode, mit denen `UcBerichteKosten` den
> Infoknopf jeder eingebetteten Seite von der Kopfzeile abrückte, sind ersatzlos
> weg: Die Kopfzeile trägt den Knopf des Behälters, jede Seite ihren eigenen im
> Fluss ihres Inhalts.
>
> **Kein neuer Kern-Controller.** Alle vier Seiten riefen schon vorher ausschließlich
> Kern-Controller (Hausmuster Ä9); die vier SQL-Anweisungen der Kostenseite sind
> wortgleich in die Windows-Datenseite gewandert.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental`
> → 0 Fehler, **20** Warnungen (Basis 22; WFO1000 16 → 14) · `dotnet test
> WP-Plan.Kern.slnf` → **1 485** grün (1 352 vorher; 133 neue bunit-Tests) ·
> Formularkarte **123** grün · Stapellauf **88** Masken, 0 × „nein", 0 × „verwaist" ·
> SQL-Prüfer 1 301 Texte, 0 Fundstellen · ChartProben 10 Bilder, 0 Verstöße ·
> Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**,
> `diff -rq` ohne Unterschied · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (6 Masken), 18 Abweichungen (A‑1…A‑18),
> Windows-Abnahmeliste mit 25 Punkten und acht offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W5_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.

> **Statusblock iU9 — Welle 4 umgesetzt (03.09.2026, Basis `ae1af82`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W4: **sieben Masken →
> sieben Razor-Komponenten**, jede WinForms-Fassung im selben Schritt gelöscht (Regel M1).
> Es ist die größte Welle bisher — die beiden **Hosts** der Kostenseite fallen mit ihren
> fünf Unterbausteinen auf einmal, zusammen 5 216 Zeilen WinForms. Acht Commits, einer je
> Nummer:
>
> | Commit | Inhalt |
> |---|---|
> | `6c3cbc5` | **W4.0** Bausteinlücken 6–8: `Bausteine/Ueberlagerung.razor` (modaler Bereich IN der Komponente, Fokusfalle ohne JS), `Bausteine/Rueckfrage.razor` (Ja/Nein/Abbrechen), `Bausteine/Zeilenraster.razor` (Spaltenkopf, Zeilen, Abschlusszeile, Summenfuß); dazu der Nachzug von **A‑10 aus Welle 3** — die Untereditoren des Emissionskatalogs stehen jetzt in einer Überlagerung |
> | `3db98e0` | **W4.1** `ucVorlagenZeile` → `Dialoge/Kosten/VorlagenZeile.razor`, `ucErtragBonus` → `Dialoge/Kosten/ErtragBonus.razor` mit `ErtragBonusGaben`; erster Aufrufer der Sprungbrücke aus W2.2 (W2‑O6 erledigt) |
> | `e0b63be` | **W4.2** `Form_KostenKomponente` (918 Z.) → `Dialoge/Kosten/KostenKomponenteDialog.razor` + `KostenKomponenteHuelle`; die **fünf Unterdialoge der Welle 1** stehen jetzt in Überlagerungen desselben Fensters statt in je einer zweiten WebView (R2) |
> | `4527d66` | **W4.3** `ucStromAufschlaege` und `ucBrennstoffBestandteile` → `Dialoge/Kosten/StromAufschlaege.razor` und `BrennstoffBestandteile.razor`; Summen, Restzeilen und Schnellwahlsätze kommen als fertiger Text aus der Hülle |
> | `b43e8fd` | **W4.4** `Form_Energietraeger` (535 Z.) und `ucFuelSettings` (2 103 Z.) → `Dialoge/Kosten/EnergietraegerDialog.razor` + `EnergietraegerEinstellungen.razor` + `EnergietraegerHuelle`; **neu im Kern:** `EnergietraegerPreisCtrl` mit den neun SQL-Anweisungen der Maske; neuer Baustein `Mehrfachauswahl` (Bausteinlücke 11) |
> | `09ecd37` | **W4.5** Ressourcen-Sammelnachtrag: 50 Schlüssel (`KKOMP_*`, `ETV_*`, `KDLG_EM_*`, `KDLG_ANLAGE_*`) in `Resource.resx`, `Resource.en-US.resx` und — von Hand — `Resource.Designer.cs` |
> | `45246be` | **W4.6** Formularkarte-Tests: sechstes und siebtes Prüfmuster (`Form_KostenKomponente`, `ucVorlagenZeile`), Anker auf `Form_Heizkessel` umgehängt, Stapellauf-Zähler 98 → 91 |
> | *dieses Paket* | **W4.7** Protokoll, Statusblock, `CLAUDE.md` ×2 |
>
> **Die Überlagerung ist der eigentliche Gewinn.** Bis Welle 3 wich jeder Blazor-Dialog,
> der einen zweiten braucht, aus — der Kostenfaktor-Katalog legt inline an (W1.5, A‑13),
> der Emissionskatalog zeigt seine Untereditoren als eingerückte Blöcke (W3.3, A‑10).
> Grund war immer Risiko **R2**: ein zweites Fenster hieße eine zweite `BlazorWebView`.
> Seit W4.0 gibt es dafür einen Baustein, und **neun Unterdialoge** stehen im selben
> Fenster wie ihr Wirt: Worst/Best, Zeileneditor, Namensabfrage, Übernahme,
> Kostenfaktor-Katalog, Kostenprofil, Spotpreis-Import, saisonale Sätze und der
> Emissionskatalog. Die sechs Hüllen der Wellen 1 bis 3 liefern dafür statt eines
> Fensters ihren **Parametersatz** (`Gaben`). Auf iOS ist diese Bauform ohnehin die
> einzige (iL5).
>
> **Neun SQL-Anweisungen gehen in den Kern.** `ucFuelSettings` las und schrieb selbst;
> `EPOS.Kern/Controller/EnergietraegerPreisCtrl.cs` trägt sie wortgleich — dieselben
> Spalten, dieselbe Rundung, dieselbe Reihenfolge. Zwei Änderungen an der Bauform: Der
> `dynamic`-Rückgabewert ist ein benannter Typ geworden, und die eine
> `RecordSet`-Abfrage mit Zeichenkettenverkettung hat einen Parameter bekommen — sie ist
> damit erstmals für den SQL-Dialektprüfer sichtbar.
>
> **`Views/Kosten` führt keine Designer-Maske mehr.** Mit den sieben Masken fallen die
> zwei nutzerlos gewordenen Karten-Controls `EinstiegsKarte` und `SectionPanel`; ihre
> Nachfolger heißen `Kachel` und `Gruppenkopf`. Der Stapellauf-Test des Werkzeugs läuft
> deshalb über `Views/Heizkessel`.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` →
> 0 Fehler, **22** Warnungen (Basis 26; WFO1000 20 → 16) · `dotnet test WP-Plan.Kern.slnf` →
> **1 352** grün (1 217 vorher; 135 neue bunit-Tests) · Formularkarte **122** grün, Build
> 0/0 · Stapellauf **91** Masken, 0 × „nein", 0 × „verwaist" · SQL-Prüfer 1 301 Texte,
> 0 Fundstellen · ChartProben 10 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 gegen
> `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**, `diff -rq` ohne Unterschied ·
> `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (7 Masken, alle vollständig, dazu 28
> Laufzeitfelder), 20 Abweichungen (A‑1…A‑20), Windows-Abnahmeliste mit achtzehn
> fachlichen Proben und acht offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W4_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.

> **Statusblock iU9 — Welle 3 umgesetzt (03.09.2026, Basis `95cf8be`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W3: **vier Masken →
> vier Razor-Komponenten**, jede WinForms-Fassung im selben Schritt gelöscht (Regel M1).
> Alle vier hängen am Energieträger — `Form_Energietraeger` öffnet zwei direkt,
> `ucFuelSettings` die anderen beiden. Acht Commits, einer je Nummer:
>
> | Commit | Inhalt |
> |---|---|
> | `afd599d` | **W3.0** Bausteinlücken 4–6: `Standards/Dateiwahl.razor` (Pfad + Knopf, Wähler als Delegat), `Bausteine/Zeilenwahl.razor` (der Wahlknopf, der bisher zweimal wortgleich im Markup stand), `Textfeld` + `Mehrzeilig`/`Zeilen`/`NurLesen`, `Raster` + `Bearbeitbar` |
> | `624ce28` | **W3.1** `Form_LeistungspreisReihe` → `Dialoge/Kosten/LeistungspreisReiheDialog.razor` + Hülle; zwölf Monatssätze im mitwachsenden Gitter, Ebenenregel Projekt-/Stammreihe unverändert |
> | `b2a9511` | **W3.2** `Form_SpotpreisImport` → `Dialoge/Kosten/SpotpreisImportDialog.razor` + Hülle; Dateiwahl über `Dienste.Datei`, Prüfen und Schreiben in `Task.Run`, Protokoll mehrzeilig und festbreit |
> | `15417a8` | **W3.3** `Form_Emissionskatalog` (767 Z., zwei Raster) → `Dialoge/Kosten/EmissionskatalogDialog.razor` + Hülle; die beiden zur Laufzeit gebauten Unterdialoge werden **eingerückte Blöcke** statt zweiter WebViews (R2) |
> | `cb700f0` | **W3.4** `Form_Kostenprofil` (36 Laufzeitfelder + Chart) → `Dialoge/Kosten/KostenprofilDialog.razor` + Hülle; **neu im Kern:** `ChartRenderer.Kostenprofil` samt `C_PROFIL` |
> | `5a25c1d` | **W3.5** Ressourcen-Sammelnachtrag: 67 Schlüssel (`LPR_*`, `SPOT_*`, `EMK_*`, `KPROF_*`) in `Resource.resx`, `Resource.en-US.resx` und — von Hand — `Resource.Designer.cs` |
> | `4ea688c` | **W3.6** Formularkarte-Tests: fünftes Prüfmuster (`Form_Kostenprofil`, neun Testbezüge), Stapellauf-Zähler 102 → 98 |
> | *dieses Paket* | **W3.7** Protokoll, Statusblock, `CLAUDE.md` ×3 |
>
> **Die Renderer-Erweiterung ist der eigentliche Gewinn.** `ChartRenderer.Kostenprofil` ist die
> erste neue Methode seit der SkiaSharp-Portierung (iU7) — der Nachweis, dass der Weg
> „Diagramm im Kern zeichnen, in der Oberfläche nur das PNG zeigen" auch für **Eingabemasken**
> trägt, nicht nur für den Bericht. Bildmaß 1296 × 780 (doppelte Zielauflösung des abgelösten
> WinForms-Chart), Linienfarbe wörtlich übernommen, y-Achse vorzeichenfähig. Die Probe
> `Proben/ChartProben` prüft es als zehntes Bild.
>
> **Bausteinsatz.** Zwei neue Bausteine (`Dateiwahl`, `Zeilenwahl`), drei erweiterte Standards
> (`Textfeld`, `Raster`, dazu `Zahlenfeld` unverändert) und sieben CSS-Klassen. Damit sind die
> Bausteinlücken 4, 5 und 6 des Wellenplans geschlossen; `Dateiwahl` bedient ab Welle 13 die
> sechs Importmasken.
>
> **Kein neuer Controller, keine neue SQL-Zeile.** Alle vier Masken riefen schon vorher
> ausschließlich Kern-Controller (`PreisreiheCtrl`, `SpotpreisImportCtrl`,
> `EmissionskatalogCtrl`/`EmissionenCtrl`, `KostenprofilCtrl`) — Hausmuster Ä9.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` →
> 0 Fehler, **26** Warnungen (Basis 28; WFO1000 22 → 20) · `dotnet test WP-Plan.Kern.slnf` →
> **1 217** grün (1 110 vorher; 105 neue bunit- und 2 neue Kern-Tests) · Formularkarte **121**
> grün, Build 0/0 · Stapellauf **98** Masken, 0 × „nein", 0 × „verwaist" · SQL-Prüfer 1 303
> Texte, 0 Fundstellen · ChartProben **10** Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017
> gegen `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**, `diff -rq` ohne Unterschied ·
> `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (4 Masken, alle vollständig, dazu die 36
> Laufzeitfelder und die beiden Untereditoren), 22 Abweichungen (A‑1…A‑22),
> Windows-Abnahmeliste mit vierzehn fachlichen Proben und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W3_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.

> **Statusblock iU9 — Welle 2 umgesetzt (03.09.2026, Basis `b0d3d86`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W2: **sechs Masken →
> vier Razor-Komponenten** (drei neue, eine erweiterte), jede WinForms-Fassung im selben
> Schritt gelöscht (Regel M1). Acht Commits, einer je Nummer:
>
> | Commit | Inhalt |
> |---|---|
> | `f9b5016` | **W2.1** `Form_StromspeicherItemNeu` (28 Aufrufer), `Form_GebaeudetypNeu` und `Form_AlsVariante` → **eine** erweiterte `Dialoge/Allgemein/NamensDialog.razor`; der Variantenablauf steht als `Views/Varianten/AlsVarianteHuelle.cs` |
> | `41db247` | **W2.2** **Sprungbrücke** — `Dialoge/Allgemein/Sprungziel.cs` (Schlüssel) + `Allgemein/Blazor/Sprungbruecke.cs` (Schlüssel → `Form`, modal aus dem Rückruf). Entscheid zu B5b‑O1 |
> | `938947a` | **W2.3** `Form_Tarifstruktur` (K4, 588 Z.) → `Dialoge/Wirtschaftlichkeit/TarifstrukturDialog.razor` + `TarifstrukturHuelle`; `Zahlenfeld`/`Ganzzahlfeld` bekommen `Aktiv` |
> | `a684fcd` | **W2.4** `Form_PhotovoltaikVerguetung` → `Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor` + `PhotovoltaikVerguetungHuelle` |
> | `8ef5b60` | **W2.5** `Form_WirtschaftlichkeitParameter` (K4, 740 Z.) → `Dialoge/Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog.razor` + Hülle; **Ersteinsatz der Sprungbrücke** (Gesetzeskatalog) |
> | `a2b3bd2` | **W2.6** Ressourcen-Sammelnachtrag: 78 Schlüssel (`NAMD_*`, `TARIF_*`, `PVV_*`, `WPAR_*`) in `Resource.resx`, `Resource.en-US.resx` und — von Hand — `Resource.Designer.cs` |
> | `3fd320e` | **W2.7** Formularkarte-Tests: viertes Prüfmuster (`Form_StromspeicherItemNeu`, sechs Testbezüge), Stapellauf-Zähler 105 → 102 |
> | *dieses Paket* | **W2.8** Protokoll, Statusblock, `CLAUDE.md` |
>
> **Die Sprungbrücke ist der eigentliche Gewinn.** Bis W2 konnte ein Blazor-Dialog nur
> *nachgelagert* weiterführen (schließen → Ziel → wieder öffnen, B5b‑O1). Jetzt zeigt ein
> Delegat mit sprachneutralem Schlüssel ein **WinForms**-Ziel modal über dem Dialog — dieselbe
> verschachtelte Nachrichtenschleife wie ein `OpenFileDialog` im Click. Für Ziele, die selbst
> Blazor-Hüllen sind, bleibt es beim nachgelagerten Sprung (Risiko R2), bis Welle 4 den
> Baustein `Ueberlagerung` bringt. **Ob die Schleife am Gerät trägt, ist Abnahmepunkt W2‑7.**
>
> **Bausteinsatz.** Kein neuer Baustein — `Zahlenfeld` und `Ganzzahlfeld` bekommen nur den
> Parameter `Aktiv` (additiv), dazu die CSS-Klasse `epos-untergruppe`. Der Namensdialog aus
> W1 trägt jetzt fünf Masken statt zwei.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` →
> 0 Fehler, **28** Warnungen (Basis 28, unverändert) · `dotnet test WP-Plan.Kern.slnf` →
> **1 110** grün (1 036 vorher; 74 neue bunit-Tests) · Formularkarte **120** grün, Build 0/0 ·
> Stapellauf **102** Masken, 0 × „nein", 0 × „verwaist" · SQL-Prüfer 1 303 Texte, 0
> Fundstellen · Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**,
> `diff -rq` ohne Unterschied · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (6 Masken, alle vollständig), 18 Abweichungen
> (A‑1…A‑18), Windows-Abnahmeliste mit elf fachlichen Proben und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W2_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.

> **Statusblock iU9 — Welle 1 umgesetzt (03.09.2026, Basis `aef9509`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt D: **sieben Masken der Kostenvorlagen und der
> Wirtschaftlichkeit → sechs Razor-Komponenten**, jede WinForms-Fassung im selben Schritt gelöscht
> (Regel M1). Neun Commits, einer je Nummer:
>
> | Commit | Inhalt |
> |---|---|
> | `9e4fa37` | **W1.0** Baustein `Optionsgruppe` (RadioButton-Gruppe; 45 im Bestand) samt 10 bunit-Tests |
> | `0d92c89` | **W1.1** `Form_VorlagenPosition` → `Dialoge/Kosten/VorlagenPositionDialog.razor` |
> | `e94978a` | **W1.2** `Form_VariantenName` + `Form_KostenItemNeu` → **eine** `Dialoge/Allgemein/NamensDialog.razor` mit dem Windows-Helfer `NamensDialogHuelle` |
> | `f6e9264` | **W1.3** `Form_CaseEingabe` → `Dialoge/Kosten/CaseEingabeDialog.razor` |
> | `584be20` | **W1.4** `Form_VorlagenUebernahme` → `Dialoge/Kosten/VorlagenUebernahmeDialog.razor` + `VorlagenUebernahmeHuelle` |
> | `8c40854` | **W1.5** `Form_KostenAdmin` → `Dialoge/Kosten/KostenfaktorKatalogDialog.razor` + `KostenfaktorKatalogHuelle` + Kern-Controller `KostenfaktorCtrl` |
> | `9a5df28` | **W1.6** `Form_WirtschaftlichkeitVerlauf` → `Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufDialog.razor` + `KapitalwertVerlaufHuelle` |
> | `e6a613e` | **W1.7** Ressourcen-Sammelnachtrag: 43 Schlüssel (`VPOS_*`, `NAMD_*`, `KCASE_*`, `KUEB_*`, `KFAK_*`, `WVERL_*`) in `Resource.resx`, `Resource.en-US.resx` und — von Hand — `Resource.Designer.cs` |
> | `21e399c` | **W1.8** Formularkarte-Tests: Prüfmuster für `Form_CaseEingabe`, Stapellauf-Zähler 118 → 111 |
>
> **Bausteinsatz.** Genau ein neuer Baustein (`Optionsgruppe`, Lücke 1 aus Abschnitt E) und ein
> neuer Dialogtyp (`NamensDialog`, Lücke 2). `Ueberlagerung`, `Rueckfrage` und `Fortschritt` bleiben
> offen — die drei Stellen, an denen sie fehlen, sind im Protokoll als A‑16 und A‑17 benannt und
> laufen bis dahin über die Hülle bzw. über die Statuszeile.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler, **30** Warnungen
> (Basis 34; WFO1000 30 → 24) · `dotnet test WP-Plan.Kern.slnf` → **1 024** grün (929 vorher; 95 neue
> bunit-Tests) · Formularkarte **119** grün, Build 0/0 · Stapellauf **111** Masken · SQL-Prüfer
> 1 333 Texte, 0 Fundstellen · Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade`
> **PASS/PASS/PASS**, `diff -rq` ohne Unterschied · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich, 19 Abweichungen (A‑1…A‑19), Windows-Abnahmeliste und sieben
> offenen Punkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W1_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.

> **Statusblock iU9 — Welle 0 umgesetzt (03.09.2026, Basis `908926a`)**
>
> **Die K6-Liste ist abgetragen.** Der Anwenderentscheid **iF29** (Register § 1) hat entschieden,
> was der Erreichbarkeitsgraph vom Vormittag gemeldet hatte: Vier unerreichbare und eine verwaiste
> Maske werden **nicht** nach Blazor umgestellt, sondern stillgelegt — dazu drei Masken, die nur an
> ihnen hingen, und `Form_KwkgModule`, deren Knopf seit B5b ausgeblendet war und deren Felder
> vollständig im `BhkwWirtschaftlichkeitDialog` stehen. Drei Commits:
>
> | Commit | Inhalt |
> |---|---|
> | `bb0474c` | **W0.1** Was von außen an `Form_Kosten` hing, in den Kern gerettet: `EPOS.Kern/Controller/KostenSummenCtrl.cs` (`KATEGORIE_INVESTITION`/`KATEGORIE_BETRIEB`, `GetAllCarriers`, `LiesKomponentenSummen`, `LiesAnlagenSummen` — Rümpfe wörtlich) und `EPOS.Kern/Model/EnergietraegerModel.cs` (`EnergyCarrier`, `EnergyConversion`); sieben Aufrufer umgestellt |
> | `16b106a` | **W0.2** 25 Dateien / 10 625 Zeilen gelöscht: `Form_Kosten`, `Form_KostenfaktorItem`, `ucKostenItem` (Klasse `ucKostenZeile`), `Form_Betriebskosten`, `Form_Variantentest`, `Form_Wirtschaftlichkeit`, `Form_Bericht`, `Form_Simulation_Kurz`, `Form_Simulation_Detail - Kopie.cs`, `ChartManagerNeu.cs`, `Form_KwkgModule` — samt den zwei Altknöpfen der Startseite (`btn_Kosten`, `btn_Varianten`, jetzt auch aus Designer und `.resx` entfernt), dem Modul-Knopf der Wirtschaftlichkeitsparameter, der `Compile Remove`-Liste der `.csproj`, sieben `HilfeKontext`-Einträgen, neun Zeilen `help_mapping.txt` und 26 Kommentarverweisen |
> | `43452a7` | **W0.3** Formularkarte: drittes — und erstes **stillgelegtes** — Prüfmuster (`Form_KostenfaktorItem`), `Form_Kosten.Auszug.cs` um `AddKostenItem` erweitert, 15 Tests umgehängt bzw. ersetzt, `Erreichbarkeit_2026-09-03.md` neu gezogen |
>
> **Zähler.** Designer-Dateien 114 → **108**, Masken 111 → **105**, lokalisiert 62 → **61**,
> Kartenzeilen 2 322 → **2 231**, Felder ohne Beschriftung 172 → **168**. Erreichbarkeit:
> ja 104 → **103**, **nein 4 → 0**, **verwaist 1 → 0**, unklar 2 → **2**
> (`Form_GebWohnflaeche`, `Form_PufferSp_Bearbeiten` — beide bleiben und werden umgestellt).
>
> **Behalten wie entschieden:** `FormMain`/`Form_StromTest` (der Menüpunkt „Projektdetail" bleibt),
> `Form_GebWohnflaeche`, `Form_PufferSp_Bearbeiten`, `Form_AlsVariante`.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` → 0 Fehler,
> **28** Warnungen (30 vorher; die zwei WFO1000 von `AlsDialog` entfallen mit den Dialoghüllen) ·
> `dotnet test WP-Plan.Kern.slnf` → **1 036** grün (35/450/337/214) · Formularkarte **119** grün,
> Build 0/0 · Stapellauf **105** Masken, 0 nein / 0 verwaist · SQL-Prüfer 1 303 Texte, 0 Fundstellen ·
> Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**, `diff -rq` ohne
> Unterschied · `dotnet publish` mit vollständigem `wwwroot`. **Windows-Abnahme steht aus** — die
> vier Prüfpunkte stehen im Protokoll, Abschnitt W0.

### iU10 — Die iOS-Hülle · L · Mac

**Voraussetzung:** iU6, iU8 (Bausteinsatz steht). **Entspricht dem verschobenen Grundlagen-S3.**

| Inhalt | Detail |
|---|---|
| MAUI-App `EPOS.iOS` mit `BlazorWebView` | Navigation nach iL5: Wizard-Workflow als Navigationsstruktur, kein MDI, keine modalen Ketten |
| iOS-Adapter | Keychain (`ILizenzAblage`), `identifierForVendor` (`IGeraeteId`), Document-Picker/Share-Sheet (`IDateiDienst`, `ITeilen`), `Preferences` (`IEinstellungen`), App-Sandbox (`IPfade`), AirPrint (`IDrucken`) |
| Datenbank auf dem Gerät | Seed-Kopie beim Erststart, ~~`bundle_green`~~ **`bundle_e_sqlite3`** (iF27, siehe Statusblock), Backup über das Share-Sheet |
| Lizenz | `LizenzToken` (Ed25519/BouncyCastle) und `LizenzServerClient` (REST gegen `epos-plan.de`) laufen unverändert — nur Ablage und Geräte-Id sind neu |

**Abnahme (iZ6):** Ein Projekt vollständig auf dem iPad durchgeplant; Ergebnis-CSV wertgleich zur
Windows-Basis; Bericht zeilengleich.

#### Statusblock iU10 — Stand 03.09.2026

**Sieben Schritte umgesetzt, einer offen.** Nachweise im Einzelnen:
[`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md).

| Schritt | Inhalt | Stand |
|---|---|---|
| **iU10-1** | Paketlage berichtigt: `bundle_green` raus (die Fassung 2.1.12 **existiert nicht**), `bundle_e_sqlite3` 2.1.12 rein, MAUI 10.0.100 und `SkiaSharp.NativeAssets.iOS` 3.119.0 dazu; `InternalsVisibleTo EPOS.iOS` | ✅ Linux |
| **iU10-2** | `EPOS.UI/Seiten/` — `AppWurzel` (Zustandsmaschine Liste ↔ Dialog), `Projektliste`, `Seitenschluessel`; die Schnittstellen `IProjektQuelle` und `INavigationsZiel` | ✅ Linux, **+12 bunit-Tests** |
| **iU10-3** | `EPOS.iOS/` angelegt: csproj, eigene `.sln`, `MauiProgram`, `App`, `HauptSeite`, `wwwroot/index.html`, `Platforms/iOS/`, Ressourcen | ✅ Restore-Probe · Bau nur CI |
| **iU10-4** | Datenbankweg: Seed-Kopie beim Erststart, `DataRepository.PfadUeberschreibung`, Gate-Zeilen `SQLite …` / `STRICT=…`, Sicherung über `VACUUM INTO` | ✅ Übersetzungsprobe · Wirkung nur CI |
| **iU10-5** | die neun Umgebungsdienste als `Ios*`-Adapter, dazu `IosHilfeDienst`; Belegung in `MauiProgram` in der Reihenfolge von `Program.Main` | ✅ Attrappenprobe · Wirkung nur CI |
| **iU10-6** | Prüfmodus (`EPOS_PRUEFLAUF`) mit den **verlinkten** Bausteinen `Ergebnisexport`/`Protokoll`; CI-Job `.github/workflows/ios.yml` | ✅ YAML geprüft · Lauf nur CI |
| **iU10-7** | `IosProjektQuelle` — Projektliste, Energieträgerliste und der BHKW-Parametersatz über **dieselben** Kern-Controller wie die Windows-Hülle | ✅ Prüfstand gegen `Kenndaten_Test.sqlite` |
| **iU10-CI** | Achter Lauf `ios.yml` (33748736894): Workload 23 s, Bau 57 s, Simulator, Erststart 73 MB, `SQLite 3.53.3`, `STRICT=114`, `Projekte=23`, Prüfmodus 5 s, **iZ6-Vergleich 1030 PASS und byte-gleich** | ✅ CI macOS, 5 min 44 s · **Neunter Lauf** (33785012663, 03.09.2026, 9 min 52 s) auf `f1d387b` nach W5/W6: grün · **Zehnter Lauf** (33809247370, 03.09.2026, 4 min 53 s) auf `21ab680` nach W7–W9: grün · **Elfter Lauf** (33826084944, 04.09.2026, 8 min 50 s) auf `a398c9a` nach W10a/W10b: grün · **Zwölfter Lauf** (33832613617, 04.09.2026, 9 min 02 s) auf `43fb9c3` nach W11a/W11b: grün · **Dreizehnter Lauf** (33838762108, 04.09.2026, 6 min 30 s) auf `62b3457` nach W12: grün |
| **iU10-9** | der iL5-Wizard in `EPOS.UI/Seiten/` und `IosNavigation` vollständig | **offen** |

**Die drei Entscheidungen, die iU10 getroffen hat** (Langfassung im Entscheidungsregister § 2.9):

1. **MAUI Blazor Hybrid**, nicht ein reines `Microsoft.iOS`-Projekt. Für `net10.0-ios` gibt es
   außerhalb von `Microsoft.AspNetCore.Components.WebView.Maui` **keinen** BlazorWebView-Host.
2. **`bundle_e_sqlite3` auch auf iOS** (iF27). `bundle_green` 2.1.12 gibt es nicht, und die
   System-SQLite des Geräts wäre für die **114 STRICT-Tabellen** der Datenbank nicht steuerbar.
   Statisch gelinkt fährt iOS dieselbe **SQLite 3.53.3** wie Windows, Linux und der macOS-CI.
3. **Mindest-iPadOS 17.0** (iF28) — von `SkiaSharp.NativeAssets.iOS` 3.119 (`net8.0-ios17.0`)
   erzwungen; MAUI 10 selbst käme mit 15.0 aus.

**Was wo bewiesen wird.**

| Ort | Was |
|---|---|
| **Hier (Linux)** | Restore gegen die echte `Directory.Packages.props`; Übersetzung aller plattformfreien Hüllendateien; Übersetzung der *gesamten* Hülle gegen Attrappen der Plattform-API; die Datenseite gegen die echte Testdatenbank; 941 Tests; Referenzlauf 1030 byte-gleich |
| **CI (`ios.yml`, `macos-26`)** | dass die Hülle mit der echten MAUI-API übersetzt und paketiert; dass die App im Simulator startet; die drei Startmarken; die Wertgleichheit der CSV gegen `2026-08-30_B3-Kaskade` |
| **Gerät (iU13)** | AOT statt JIT (`ModuleInitializer`, `ApplicationSettingsBase`-Reflection, `DataTable`), Signierung, Speichergrenzen, Bedienung mit dem Finger, Bundle-Größe im Mobilfunk |

### iU11 — Kataloge, Importe, Feinschliff · M–L · Mac

**Voraussetzung:** iU10. **Grundlagen-S5; D3–D5. Umfang nach iF2.**

VDI 3805 / CEC / PAN / CSV-Importe über den iOS-Dateidialog; KI-Chat und Wiki-Hilfe (REST bleibt,
Schlüsselablage über `ILizenzAblage`, Caches über `IPfade`); Katalogpflege nach Scope-Entscheidung
iF2 — Empfehlung des Grundlagenkonzepts: erste Auslieferung **ohne** Katalog-Admin.

### iU12 — Absicherung und Betrieb · M · beide

**Block E1–E3.** Referenzläufe beider Plattformen als Pflicht-Gate; Abnahme je Maske; Mischphase (M9)
dokumentiert — zwei Optiken in einer App sind gewollt und enden erst mit der letzten Maske;
Installer mit WebView2-Voraussetzung; `BETRIEB_SQLITE.md` liegt bereits vor.

### iU13 — TestFlight und Vertriebsweg · S–M · Mac

**Voraussetzung:** iU11, iU12. **Grundlagen-S6, iF5, iF12.**

Signierkette in der CI scharf; TestFlight-Feldtest (90-Tage-Grenze beachten); Vertriebsweg nach
§ 3.4 entscheiden und einrichten; Provisionsfrage (iF12) **vorher** geklärt.

### 4.1 Meilensteine

| Nr. | Meilenstein | nach | Nachweis |
|---|---|---|---|
| **iZ1** | Solution baut ohne Visual Studio | iU1 | `dotnet build`/`dotnet test` grün; Referenzlauf 332/332 byte-gleich — **hier erreicht 02.09.2026** (`0ddc417`, 7 Projekte 0 Fehler, 787 Tests); **Windows-Nachweis offen** |
| **iZ2** | Entwicklungsumgebung steht | iU2 | Build-Matrix § 3.6 erfüllt; MAUI-Hallo-Welt mit Kernbibliothek im Simulator |
| **iZ3** | **Go/No-Go** | iU3 | **erreicht 02.09.2026** — 1030 auf Linux byte-gleich zur Referenzbasis |
| **iZ4** | Kern herausgelöst | iU4 | Windows byte-gleich; `EPOS.Kern` baut und testet auf macOS — **hier erreicht 03.09.2026: der Kern liegt physisch und plattformfrei** (168 Dateien mit iU4-5, nach dem zweiten Umzug **268**; `EnableWindowsTargeting=false`, CA1416 = 0, macOS-Lauf grün; 1030/1007/1017 byte-gleich). **Auf Windows offen: der Vollreferenzlauf 332/332** |
| **iZ5a** | Statics gekappt | iU5 | Wächter `Program.*` im Kernsatz = 0 Treffer — **hier erreicht 03.09.2026** (`35be81f`…`9235a92`); Referenzlauf 1030/1007/1017 byte-gleich, **Windows-Bedienprobe offen** |
| **iZ5** | Modell-C-Stichtag | iU8 | erster Blazor-Dialog produktiv, Maus und Finger abgenommen — **hier erreicht 03.09.2026: der Blazor-Dialog läuft in der Anwendung, `dotnet publish` liefert `wwwroot` vollständig** (`92380ea`, WinForms-Fassung gelöscht). **Auf Windows offen: die Abnahme am Gerät** (Maus/Finger, de/en, Hochkontrast, DPI, Setup) |
| **iZ6** | iPad rechnet ein Projekt vollständig | iU10 | Ergebnis wertgleich, Bericht zeilengleich |
| **iZ7** | Auslieferungsfähig | iU13 | signiertes `.ipa`, Feldtest bestanden, Vertriebsweg eingerichtet |

### 4.2 Abhängigkeiten

```
iU0 → iU1 → iU2 → iU3 ═══ Gate iZ3 ═══> iU4 ─┬─> iU5 ──> iU8 ──> iU9 (dauerhaft)
                                              ├─> iU6 ──────┐
                                              └─> iU7 ──────┴──> iU10 → iU11 → iU12 → iU13
```

**Parallelisierbar:** iU5, iU6 und iU7 nach iU4; iU9 ab iU8 dauerhaft neben allem Weiteren.

**Nicht parallelisierbar:** iU3 vor iU4 (das Gate hat einen Zweck); iU8 vor iU9 (Standards vor der
ersten Maske, M6).

---

## 5 Etappen und Zuordnung zu den Vorgängerdokumenten

### 5.1 Etappenübersicht

| Etappe | Pakete | Ort | Gate |
|---|---|---|---|
| **0 — Fundament** | iU0, iU1 | Windows | iZ1: Solution baut ohne Visual Studio |
| **1 — Umgebung und Beweis** | iU2, iU3 | Mac / CI | **iZ3: Go/No-Go** |
| ~~**2 — Datenschicht**~~ | **entfällt — mit `6486c36` erledigt** (S4a–S8, 234/234 CSV bitgleich) | — | ✔ |
| **2 — Gesundung** | iU4, iU5, iU6, iU7 | Windows | iZ4: Kern byte-gleich, baut auf macOS — **hier erreicht 03.09.2026** (Kern physisch, plattformfrei, 268 `.cs`, CA1416 = 0); **Windows 332/332 offen** |
| **3 — UI-Fundament** | iU8 | Windows | iZ5: Modell-C-Stichtag — **hier erreicht 03.09.2026** (Blazor-Dialog in der App, Publish mit `wwwroot`); **Windows-Abnahme offen** |
| **4 — Masken** | iU9 (Wellen) | Windows | je Welle Feldkartenabnahme |
| **5 — iPad** | iU10, iU11 | Mac | iZ6: ein Projekt vollständig |
| **6 — Auslieferung** | iU12, iU13 | beide | iZ7 |

**Zur Reihenfolge.** Rev. 1 hatte die Datenschicht bewusst vor die Kern-Herauslösung gezogen, damit
keine Datei zweimal angefasst wird. Diese Frage hat sich erledigt: Die Datenschicht **ist fertig**,
die Reihenfolge des Grundlagenkonzepts (Kern zuerst) gilt wieder unverändert. Der Wegfall einer
ganzen Etappe ist der größte Einzelgewinn gegenüber Rev. 1 — die iOS-Kette beginnt jetzt direkt nach
dem Go/No-Go-Gate.

### 5.2 Zuordnung der Kürzel

| Dieses Dokument | Grundlagenkonzept | SQLite-Konzept |
|---|---|---|
| iU0 | — | S0 (teilweise) |
| iU1 | A1 (teilweise), D2 | — |
| iU2 | „Voraussetzungen" § 5 | — |
| iU3 | **S0** | erledigt — die SQLite-Datenbasis steht bereits zur Verfügung |
| iU4 | **S1**, A1, M10 | — |
| iU5 | A2, A3, A4, M4 | — |
| iU6 | B1 | **S4a–S8 erledigt**; `DbVorgang`, `PRAGMA`-Schemaauskunft und Dialekt-Sweep lagen vor. **Selbst erledigt 03.09.2026** (`22fb7eb`…`2387abf`) |
| iU7 | D1, D2, M5 | — |
| iU8 | A5, A6, A7, M1, M2, M6, M9 | — |
| iU9 | Block C: K1–K6 | — |
| iU10 | **S3** (verschoben), D4 | — |
| iU11 | **S5**, D3, D5, iF2 | — |
| iU12 | **E1–E3** | S8 |
| iU13 | **S6**, iF5 | — |
| entfällt | **M3** (zwei Dialekte) | überholt durch D1/§ 9 — es gibt keinen Parallelbetrieb |
| präzisiert | **iL2** (ACE auf Windows) | überholt durch iF9/D7 |

### 5.3 Stichtage

| Stichtag | Bindung | Stand |
|---|---|---|
| ~~**.NET 10**~~ | 10.11.2026 (Support-Ende 8/9) — von außen gesetzt | **erledigt 02.09.2026** (`0ddc417`), Windows-Abnahme offen |
| **Modell C (M1)** | mit iZ5 — ab dann kein Dialog mehr doppelt | **gesetzt am 03.09.2026** mit `92380ea`. Die Arbeitsregel gilt ab sofort: jeder neue und jeder ohnehin anzufassende Dialog entsteht in `EPOS.UI`, die WinForms-Fassung wird im selben Schritt gelöscht |
| ~~**SQLite auf Windows (M3/iF9)**~~ | **erledigt am 02.09.2026** mit `6486c36` | ✔ Die einzige Terminlücke der Kette ist geschlossen |

---

## 6 Nachweise und Teststrategie

Kein Paket gilt als fertig, weil es gebaut ist. Es gilt als fertig, wenn sein Nachweis vorliegt.

| Nr. | Nachweis | Inhalt | Pakete |
|---|---|---|---|
| **iT1** | **Byte-Gleichheit** | Referenzlauf gegen `2026-08-30_B3-Kaskade`, 332/332 Dateien byte-/MD5-gleich. Der Maßstab für alles, was sich **nicht** ändern darf | iU1, iU4, iU5 |
| **iT2** | **Wertgleichheit in Toleranz** | rel. 1e-4 / abs. 0,01 wie heute; nichtnumerische Werte exakt. Der Maßstab für Plattform- und Backendwechsel | iU3, iU6, iU10 |
| **iT3** | **Plattformvergleich** | derselbe Kern-Referenzlauf auf x64-Windows, x64-Linux und arm64-macOS. **Geführt 02.09.2026 für Projekt 1030: byte-gleich auf allen drei** | iU3, iU4, iU10 |
| **iT4** | **Build-Matrix** | § 3.6 vollständig erfüllt; CI grün auf allen drei Runnern | iU1, iU2, iU4 |
| **iT5** | **Berichtsvergleich** | Word-/Excel-Bericht zeilengleich, Chartbilder sichtgeprüft | iU7, iU10 |
| **iT6** | **Feldkartenabnahme** | je Maske das generierte Inventar vollständig abgehakt — bei 730 Textfeldern ist das vergessene Feld der typische Migrationsfehler | iU8, iU9 |
| **iT7** | **Kulturtest** | `EPOS_REFLAUF_UICULTURE=en-US`: Ergebnisdateien **byte-identisch**. Der maschinelle Nachweis der Drei-Schichten-Regel | jede Etappe |
| **iT8** | **Bedienbarkeit** | jede Komponente mit Maus **und** Finger abgenommen (M2) — sonst entsteht die zweite UI durch die Hintertür | iU8, iU9 |
| **iT9** | **Kodierungsnachweis** | nach iU4 null Nicht-UTF-8-Dateien in den neuen Projekten | iU4 |
| **iT10** | **Datenintegrität** | `PRAGMA foreign_key_check` + `integrity_check`, Zeilenzahlen und Prüfsummen je Tabelle — im `EposSqliteMigrator` bereits umgesetzt | iU6 |

**Was die Nachweise nicht abdecken:** die manuelle Abnahme der x64-Umstellung (§ 3.7) und die
Sichtabnahme der Masken. Beides bleibt Handarbeit.

### 6.1 Was hier geführt wurde — und was auf Windows offen ist

Stand 03.09.2026. **Alles in der linken Spalte ist auf Linux gefahren** (SDK 10.0.400, kein Visual
Studio, reduzierte Testdatenbank). Die rechte Spalte braucht ein Windows und ist die eigentliche
Abnahmeliste des Anwenders; sie ist je Commit abhakbar in den beiden Nachweislisten.

| Gegenstand | hier geführt | auf Windows offen |
|---|---|---|
| **Bau** | `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler, **34 Warnungen**; `EPOS.Kern` allein 0 / 3 | VS 2026 öffnet die Projektmappe unter dem Razor-SDK; der **WinForms-Designer** öffnet ein Formular |
| **Tests** | `dotnet test WP-Plan.Kern.slnf` → **886** (450 + 337 + 64 + 35) | dieselben 886 im `windows.yml`-Lauf |
| **Rechenweg** | Referenzlauf **1030, 1007, 1017** gegen `2026-08-30_B3-Kaskade`: GESAMT PASS, byte-gleich nach **jeder** Tranche | **Vollreferenzlauf 332/332** über alle 13 Projekte (iT1, iZ1 **und** iZ4) |
| **Plattform** | derselbe Kernlauf auf x64-Linux **und** arm64-macOS byte-gleich (iT3) | — |
| **Kultur** | `EPOS_REFLAUF_UICULTURE=en-US` byte-identisch (iT7); `EPOS.UI.Tests` mit `LANG=en_US.UTF-8` 64/64 | Sprachumschaltung de↔en in der laufenden Anwendung |
| **Charts** | `ChartProben` 9 Bilder / 0 Verstöße auf ubuntu und macos; drei Renderer-Tests im Kern | **`Referenzlauf.exe bildvergleich`** alt/neu — der einzige Weg, die GDI+-Ablösung abzunehmen (→ iF23) |
| **Datenzugriff** | CA1416 87 → 0, kein OleDb-Paket im Kern, `ZugriffsschichtProben` übersetzt | Proben **16/16**; **Erststart-Migration aus einem `.accdb`-Bestand**; die Solar- und Pufferspeicherdialoge; die 36 `RecordSet`-Views |
| **Bericht** | Ausgabe und Renderer bauen plattformfrei | Word- **und** Excel-Bericht erzeugen; Deckblattfassung zeigt `1.1.0.0`; Vorlage `Vorlagen\Berichtsvorlage.docx` wird gefunden |
| **Dienste (iU5)** | beide Wächter 0 Treffer | Registry-Werte, DPAPI-Geltungsbereiche, 12 Gewerke, 19 Stammdatenmasken, CSV-Export, Ja/Nein-Rückfragen |
| **Blazor-Dialog (iU8)** | 64 bunit-Tests; Publish enthält `wwwroot` vollständig | **die Abnahme von iZ5**: Maus *und* Finger, de/en, Hochkontrast, 125 %/150 % (iF21), Enter/Esc, Infoknopf, Profilordner, zweites Konto |
| **Setup** | Sichtprüfung der `.iss`-Abschnitte (kein Inno-Compiler auf Linux) | `build-setup.ps1` läuft durch; Sandbox **ohne** WebView2 und **ohne** Internet (iF20) |
| **Werkzeug Formularkarte** | Stapellauf über 123 Designer-Dateien, Skelette übersetzen | — (das Werkzeug ist plattformfrei; offen ist nur die Testreparatur nach dem gelöschten Dialog) |

**Zwei Vorbedingungen, die der Anwender selbst herstellt:** die produktive `Kenndaten.sqlite` bzw.
ein `.accdb`-Bestand für die Migrationsprobe, und `MicrosoftEdgeWebview2Setup.exe` in der
Repowurzel für den Setup-Bau (`.gitignore` schließt die Datei aus, `e3d1e5b`).

---

## 7 Risiken

| Nr. | Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|---|
| ~~**iR1**~~ | ~~Der SQLite-Stichtag kommt nicht~~ | — | **erledigt am 02.09.2026** mit `6486c36`. Das größte Terminrisiko der Rev. 1 ist vom Tisch |
| **iR2** | **Apple-Toolchain-Drift.** .NET-für-iOS und Xcode sind versionsstarr gekoppelt; eine automatische Xcode-Aktualisierung legt den Build lahm | Mac-Arbeitsplatz und iOS-CI stehen | Xcode-Aktualisierungen **nie automatisch**; Version im Team dokumentieren; CI-Runner-Image pinnen |
| ~~**iR3**~~ | ~~**Blazor Hybrid und SkiaSharp** vertragen sich nicht unmittelbar (§ iU7)~~ **entschärft 03.09.2026.** Der Kern rendert PNG-Bytes (`ChartRenderer`, SkiaSharp seit iU7-2), die Blazor-Seite zeigt sie als Bild (`EPOS.UI/Standards/ChartBild`, iU8-4). Keine SkiaSharp-Komponente in der WebView, ein Stack für Bericht und Bildschirm | entfallen | iF16, präzisiert durch iF22: **eine** Bibliothek, zwei Nutzungsarten; ScottPlot bleibt für die eine interaktive Maske |
| **iR4** | **Gleitkomma auf ARM64.** Zwischen x64 und Apple Silicon sind Abweichungen zu erwarten wie damals zwischen x86 und x64 | „wertgleich" wird bestreitbar, das Abnahmeinstrument stumpf | Toleranz vor iU3 definieren (iF15); FMA-Analyse als erprobtes Muster |
| **iR5** | **Parallelentwicklung.** Der Fachausbau läuft weiter; jede Etappe, die während iU4–iU9 in die WinForms-App fließt, ist Arbeit, die später wandern muss | Der Umbau holt den Bestand nie ein | **entschärft:** der Modell-C-Stichtag iZ5 ist am 03.09.2026 gesetzt (`92380ea`); die Arbeitsregel steht in `WindowsFormsApplication1/CLAUDE.md`. Das Risiko besteht fort, solange sie nicht eingehalten wird |
| **iR6** | **Kein Testdatenbestand in der CI.** `.gitignore` schließt `*.accdb` aus; ohne Datenbank ist die Kern-CI nur ein Kompilierungstest | Der Wertgleichheitsnachweis läuft nicht automatisch, sondern nur von Hand | Anonymisierte `Kenndaten_Test.sqlite` versionieren (iE6, iF14) |
| **iR7** | **Provisionsfrage beim Lizenzverkauf.** Ob Apple bei einer B2B-Fachanwendung In-App-Kauf verlangt, entscheidet über 15–30 % je Lizenz | Geschäftsmodell | Vor iU13 klären (iF12); Custom Apps über Apple Business Manager entschärfen die Frage |
| ~~**iR8**~~ | ~~**`RecordSet` bleibt an OleDb gebunden.**~~ **Erledigt 03.09.2026 mit iU6-T1 (`22fb7eb`) — als STREICHUNG, nicht als Umbau.** Befund der Vermessung: repositoryweit **0 externe Nutzer** von `RecordSet.DBCommand`; das Kommando entstand seit iU3 nur lazy im Getter und blieb damit immer `null` — die 47 „Nutzer" hingen an `Open`, `Next`, `Read` und `Close`, nie am Kommando | entfallen | `DBCommand`, `_cmd`, `MerkeSql()` und `Parameter()` ersatzlos gestrichen; **kein Ersatztyp** (Begründung im Kopfkommentar von `RecordSet.cs`). `IDisposable` bleibt |
| **iR9** | **Nur ein Rechner, nur ein Mensch.** Tags, Referenzbasen und die einzige Buildumgebung hängen heute an einem Arbeitsplatz (`letzter-x86-stand` ist bis heute nicht gepusht) | Ausfallrisiko für das gesamte Vorhaben | Die CI ist zugleich die Antwort darauf: Sie macht den Build reproduzierbar und vom Einzelrechner unabhängig |
| **iR10** | **`Form_Simulation_Detail`** wächst schneller als die Umstellung (6.200 → 7.773 Zeilen in vier Monaten) | Das größte Einzelstück wird nie fertig konvertiert | In iU9 nicht konvertieren, sondern zerlegen — und dafür einen eigenen Termin setzen, bevor es weiter wächst |
| **iR11** | **DPI.** Die Anwendung ist `DpiUnaware`; nur die Blazor-Hülle stellt sich für die Dauer des modalen Laufs auf `PER_MONITOR_AWARE_V2` (`DpiInsel`, iU8-6) | Greift die Insel nicht, ist der Dialoginhalt bei 125–200 % bitmapskaliert und sichtbar unschärfer als der Rest — oder Fenstergröße und Elternfenster passen nicht zusammen | Am Gerät prüfen (125 %, 150 %, zweiter Monitor mit anderer Skalierung) und den Befund festhalten; auf Windows vor 10/1803 ist das Bitmapskalieren zulässig. → **iF21** |
| **iR12** | **WebView2 offline.** Das Setup nimmt heute den **Online**-Bootstrapper mit (iU8-10). Ein Kunde ohne Internet bekommt die Laufzeit nicht | Die Installation läuft durch, die Anwendung startet — aber jeder Blazor-Dialog bleibt leer. Mit jeder umgestellten Maske wächst der Schaden | Anwenderentscheid **iF20**: Standalone-Installer (~150 MB) beilegen oder Fixed Version verteilen. Das Setup meldet den Fehlschlag bereits (`WebView2Fehlt`) und bricht nicht ab |
| **iR13** | **VS-2026-Designer unter dem Razor-SDK.** `WindowsFormsApplication1.csproj` steht seit iU8-6 auf `Microsoft.NET.Sdk.Razor`; ob der WinForms-Designer damit umgeht, ist **nicht geprüft** | Fällt der Designer aus, sind die verbleibenden ~120 Masken nur noch von Hand zu pflegen — mitten in iU9 | Erster Punkt der Windows-Abnahme (`Umsetzung_iU8_Nachweise.md`, `4369fdb`). Das offizielle WinForms-Blazor-Template geht denselben Weg; der Rückweg wäre die Auslagerung der Web-Anteile in ein eigenes Hüllenprojekt |
| **iR14** | **Kultur der CI-Läufer.** `macos-latest` und `windows-latest` laufen mit **en-US**-UI-Kultur, `ubuntu-latest` und der Arbeitsplatz zufällig deutsch | Jeder Test, der Anzeigetext vergleicht, ist auf zwei von drei Läufern rot — und der Fehler sieht aus wie ein Fachfehler. Belegt mit `f5fb05c` (2 von 64 rot) | Jeder solche Test setzt seine UI-Kultur selbst (`de-DE` im Konstruktor, Rückstellung in `Dispose`). Gegenprobe hier mit `LANG=en_US.UTF-8`, bevor etwas gepusht wird |

**Was die Kette iU4…iU8 an Risiken geschlossen hat.** Vier Punkte, die in Rev. 2/2.1 noch als
Unwägbarkeit geführt wurden, sind gemessen und erledigt:

| Punkt | Befürchtung | Befund 03.09.2026 |
|---|---|---|
| **`partial` über die Assemblygrenze** | Zwei Klassen (`WizardItemClass`, die `FillComboBox`-Hälften) und später der Access-Zweig von `ApplikationCtrl` waren über `partial` in Kern- und Oberflächenhälfte geteilt; das trägt nur innerhalb **einer** Assembly | **gelöst ohne eine einzige Aufrufstelle:** `WizardSeite` erbt statt zu ergänzen, die `FillComboBox`-Hälften werden Erweiterungsmethoden, die beiden Schemamarker-Methoden sind `static` und stehen jetzt in `SchemaVersionAccess`. Fünf `*Ctrl.WinForms.cs` hatten 0 Aufrufer und sind entfallen. Die Falle bleibt eine **Prüfregel** vor jedem weiteren Umzug (`EPOS.Kern/CLAUDE.md`) |
| **iR8 — `RecordSet` an OleDb gebunden** | 47 Nutzer, ein öffentliches `OleDbCommand` | **ersatzlos gestrichen** (iU6-T1): repositoryweit **0** externe Nutzer; das Kommando entstand nur lazy im Getter und blieb immer `null`. Kein Ersatztyp |
| **ClosedXML-Schrift auf Nicht-Windows** | `AdjustToContents` misst Text mit Calibri; ohne Office keine Schrift, also keine Datei | **nicht real** in ClosedXML 0.105.1: Carlito ist eingebettet. Eine erzwungene Systemschrift hätte die Spaltenbreiten **verschlechtert**. Übersteuert wird nur noch, wenn eine Messprobe fehlschlägt (iU7-4) |
| **WinForms-Blazor-Paketlage** | Gibt es `Microsoft.AspNetCore.Components.WebView.WindowsForms` für .NET 10, und in welcher Zählung? | **ja, 10.0.100** — eigene Zählung neben Components.Web/QuickGrid 10.0.11. Die Gruppe steht in `Directory.Packages.props`; der Bau kostet **keine** neue Warnung. Offen blieb nur, dass der **Razor-SDK zwingend** ist, sonst fehlt `wwwroot` im Publish |

---

## 8 Entscheidungsbedarf

### 8.1 Vorbedingungen aus dem Grundlagenkonzept

Diese Fragen sind dort gestellt und empfohlen, aber noch nicht beschieden. **iU4 und alles Weitere
setzen sie voraus:**

| Nr. | Frage | Empfehlung dort | hier benötigt ab |
|---|---|---|---|
| iF1 | S0-Spike beauftragen? | ja | iU3 |
| iF2 | Voller Funktionsumfang oder erste Auslieferung ohne Katalog-Admin? | ohne Katalog-Admin | iU11 |
| iF3 | Blazor Hybrid oder MAUI-XAML? | Blazor Hybrid | iU8 — **umgesetzt, förmlicher Entscheid offen** |
| iF4 | S1/S2 unabhängig vom iOS-Ziel einplanen? | ja | iU1 |
| iF5 | Vertriebsweg | zunächst TestFlight | **präzisiert in § 3.4** — TestFlight ist kein Auslieferungsweg (90 Tage) |
| iF6 | Windows-Charts ebenfalls auf ScottPlot? | mittelfristig, nicht Teil des Vorhabens | iU7 — **durch iF22 überholt**: der Bildschirm bekommt Bilder aus dem Kern-Renderer, ScottPlot bleibt nur für die eine interaktive Maske |
| iF7 | Formular-Generator als Werkzeug? | ja | iU8 — **umgesetzt, förmlicher Entscheid offen** |
| iF8 | **Modell C beschließen** | ja | **iU8 — umgesetzt (iZ5 am 03.09.2026), förmlicher Entscheid offen** |
| ~~iF9~~ | ~~SQLite auch auf Windows, mit Stichtag~~ | ja | **beschieden und ausgeführt** (02.09.2026) |

### 8.2 Neue Fragen aus dieser Prüfung

| Nr. | Frage | Empfehlung |
|---|---|---|
| **iF10** | **Bekommt `IDatenzugriff` einen providerneutralen Parametertyp** (`DbParam`), während `DataRepository` seine OleDb-Fläche als Windows-Adapter behält — Weg (b) aus § 1.4? Oder werden die ~2.300 `OleDbParameter`-Aufrufe in einem Zug maschinell ersetzt (Weg a)? | **Weg (b) — mit iU6 ausgeführt (03.09.2026).** `IDatenzugriff` und `SqliteDatenzugriff` stehen, `DataRepository` ist die unveränderte Fassade davor. **Weg (a) hat sich dabei nebenbei erledigt:** Der Masken-Sweep iU6-T3a hat die 434 `OleDbParameter`-Stellen der Views maschinell auf `DbParam` gezogen — die OleDb-Fläche ist damit nicht nur umgangen, sondern weg. Übrig ist allein der eingefrorene Access-Zweig der Erststart-Migration |
| **iF11** | Mac-Hardware sofort beschaffen — oder iU3 auf einem `macos-latest`-CI-Runner fahren und den Mac erst mit iU10 kaufen? | **CI-Runner für den Spike.** Verschiebt eine vierstellige Investition hinter das Go/No-Go-Gate, ohne den Beweis zu schwächen |
| **iF12** | Vertriebsweg für die Auslieferung: Custom Apps über Apple Business Manager, Unlisted App oder öffentlicher App Store — und wie wird der Lizenzverkauf gegenüber Apples Kaufregeln behandelt? | **Custom Apps** prüfen: passt zum B2B-Kundenkreis und entschärft die Provisionsfrage. Klärung **vor** iU13, nicht im Review |
| **iF13** | Wird der Root-Namespace `WindowsFormsApplication1` beim Kern-Umzug mit umbenannt? | **Nein** — der Umzug bleibt lesbar; die Umbenennung ist ein eigener mechanischer Schritt danach |
| **iF14** | Wird eine anonymisierte `Kenndaten_Test.sqlite` mit den 13 Referenzprojekten versioniert? | **Ja.** Ohne sie ist die Kern-CI ein Kompilierungstest (iR6). `sqlite-probe/EPOS_Beispiel.sqlite` ist der akzeptierte Präzedenzfall |
| **iF15** | Wie ist „wertgleich" zwischen x64 und ARM64 definiert? | **Bestehende Toleranz** (rel. 1e-4 / abs. 0,01) für den Plattformvergleich; **Byte-Gleichheit** bleibt Maßstab für Windows-interne Umbauten |
| **iF16** | Chart-Weg in Blazor Hybrid: als Bild, JavaScript-Bibliothek oder natives Steuerelement? | **als Bild** — ein Stack für Bericht und Bildschirm; Interaktivität nur dort nachrüsten, wo sie fachlich gebraucht wird. **Umgesetzt mit iU7/iU8**: gerendert wird nicht mit ScottPlot, sondern mit dem Kern-Renderer `ChartRenderer` (SkiaSharp); Anzeige über `EPOS.UI/Standards/ChartBild`. Präzisiert durch **iF22** |
| **iF17** | Wird iU1 (Fundament, .NET 10, CI, COM-Entfernung) **unabhängig vom iOS-Beschluss** beauftragt? | **Ja.** Die Support-Frist läuft am 10.11.2026 ab; das Paket ist auch ohne iOS vollständig gerechtfertigt und die einzige Antwort auf iR9 |
| **iF18** | **Welche VS-2026-Edition?** VS 2022 kann `net10.0` nicht targeten, der Umstieg ist zwingend. Heute läuft **Community 2022** | **Community 2026**, sofern INEKON unter den Enterprise-Schwellen bleibt (≤ 250 PCs/Nutzer **und** ≤ 1 Mio. USD Umsatz) und höchstens 5 Entwickler daran arbeiten — dann kostenneutral. Sonst Professional (Abo oder neue Standalone-Lizenz; die 2022er-Dauerlizenz gilt nicht weiter). Vor iU1 einordnen |
| **iF19** | Schrift der Berichts-Charts nach der SkiaSharp-Portierung: mitgelieferte Schrift oder Systemschrift? | **Systemschrift, flexibel** — beschieden 02.09.2026. Umgesetzt mit einer Zwischenstufe, die die Vorgabe nicht kannte: Calibri → **Carlito, Liberation Sans, DejaVu Sans** → Helvetica/Arial → Systemschrift. Ohne sie liefert SkiaSharp unter Linux eine **Serifen**schrift |
| **iF20** | **WebView2-Verteilung:** Online-Bootstrapper (heute), Standalone-Installer (~150 MB) oder Fixed Version? | **Bootstrapper** — entschieden 03.09.2026; der Standalone-Installer kommt erst dazu, wenn ein Kunde ohne Internet installiert (S10 geschlossen) |
| **iF21** | **DPI:** bleibt die Blazor-Hülle eine DPI-Insel in einer `DpiUnaware`-Anwendung? | **Insel jetzt, Anwendung DPI-fähig mit W16** — entschieden 03.09.2026 (Empfehlung angenommen). Die `DpiInsel` (iU8-6) deckt die modalen Dialoge; eingebettete Seiten bleiben bis W16 bitmapskaliert (W5‑O1). Der Windows-Befund bei 125 % und 150 % steht aus |
| **iF22** | **Wie viele Chart-Stacks trägt das Haus?** | **eine Bibliothek (SkiaSharp), zwei Nutzungsarten.** Bericht und Blazor bekommen ein Bild aus dem Kern-Renderer; die interaktiven Bildschirmmasken bleiben bei ScottPlot — heute genau **eine**, `Form_SpeicherOptimierung`. ScottPlot 5 rendert selbst über SkiaSharp |
| **iF23** | **Was geschieht mit `ChartRendererGdi.cs`?** | **ersatzlos gelöscht am 03.09.2026** auf Anweisung des Anwenders — samt `Referenzlauf/Bildvergleich.cs` und dem Modus `bildvergleich`, ohne vorherigen Windows-Bildvergleich. Wächter sind die Renderer-Tests im Kern und `ChartProben` |

---

## 9 Anhang

### 9.1 Fundstellen

| Thema | Fundstelle |
|---|---|
| COM-Sperre, Buildregel | `WindowsFormsApplication1/WindowsFormsApplication1.csproj` (COMReference); `WindowsFormsApplication1/CLAUDE.md` |
| Muster für den Kernschnitt | `SpeicherEngine/SpeicherEngine.csproj`, `KiKern/KiKern.csproj` (Kopfkommentare) |
| Rechenkern | `EPOS.Kern/Allgemein/BhkwPlan.cs` (410 Z., seit iU4-5 dort) |
| Datenzugriff | `EPOS.Kern/Allgemein/DataRepository.cs` (Fassade seit iU6-T4), `IDatenzugriff.cs`, `SqliteDatenzugriff.cs`, `DbParam.cs`, `RecordSet.cs` (ohne `DBCommand`, iU6-T1) |
| Schemapflege | `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` (13.589 Z., 61 Schritte, `ZIEL_VERSION` Z. 112); `SchemaKatalog.cs` (3.461 Z.) |
| Chart-Renderer | `EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs` (SkiaSharp seit iU7-2, im Kern seit iU7-5) — die einzige Fassung; der GDI+-Gegenpart ist mit iF23 gelöscht |
| Excel-COM | `WindowsFormsApplication1/Allgemein/ToolsClass.cs`, `Allgemein/Import/GanglinienDatei.cs` |
| Lizenz | `WindowsFormsApplication1/Allgemein/Lizenz/` (`LizenzToken.cs`, `GeraeteId.cs`, `LizenzServerClient.cs`, `LizenzManager.cs`); `Lizenzserver/` (PHP) |
| Größtes Einzelstück | `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Detail.cs` (7.773 Z.) |
| Oberflächenbibliothek | `EPOS.UI/` (7 Bausteine, 8 Standardfelder, `Dialoge/Kosten/EnergietraegerVarianteDialog.razor`, `wwwroot/epos-ui.css`), Hülle `WindowsFormsApplication1/Allgemein/Blazor/` |
| Formular-Generator | `Werkzeuge/Formularkarte/` (+ `Formularkarte.Tests`), eigene `.sln`, `LIESMICH.md` |
| Referenzlauf | `Referenzlauf/` (10 `.cs`, seit iU7-1 mit `Bildvergleich.cs`; Modi `lauf`, `projekt`, `vergleich`, `pruefen`, `liste`, `migration`, `bildvergleich`), `EPOS.Referenzlauf/` (headless, plattformfrei), `Referenzlaeufe/LIESMICH.md`, Basis `Referenzlaeufe/2026-08-30_B3-Kaskade/`, Testdatenbank `Referenzlaeufe/Kenndaten_Test.sqlite` |
| Freigabekette | `Setup/build-setup.ps1`, `Setup/EPOS-Plan.iss` (Z. 29 `AppExeName`), `Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md` |
| SQLite-Probe | `sqlite-probe/LIESMICH.md`, `sqlite-probe/aufbau.sql`, `sqlite-probe/EPOS_Beispiel.sqlite` |
| SQLite-Umsetzung (Branch `sqlite`, `6486c36`) | `Allgemein/DataRepository.cs` (OleDb-Signaturen Z. 512–612, `UebersetzeParameterzeichen` 270, `NormalisiereWert` 312), `Allgemein/DbVorgang.cs` (157 Z.), `Allgemein/Update/ErststartMigration.cs` (424 Z.), `EposSqliteMigrator/`, `Proben/ZugriffsschichtProben/`, `sql/`, `BETRIEB_SQLITE.md` |
| Kodierungsregel | `.editorconfig` (bewusst ohne globales `charset`) |
| Synchronisation | `GitHub_Sync.bat` |

### 9.2 Kürzelglossar

| Familie | Bedeutung | Herkunft |
|---|---|---|
| **iU0–iU13** | Arbeitspakete | **dieses Dokument**, § 4 |
| **iE1–iE10** | Bausteine der Entwicklungsumgebung | **dieses Dokument**, § 3.10 |
| **iZ1–iZ7** | Meilensteine | **dieses Dokument**, § 4.1 |
| **iT1–iT10** | Nachweise | **dieses Dokument**, § 6 |
| **iR1–iR14** | Risiken | **dieses Dokument**, § 7 |
| iL1–iL8 | Leitentscheidungen der Portierung | `Konzept_iOS-Portierung_EPOS-Plan.md` § 3 |
| iF1–iF9 | Entscheidungsfragen | ebenda § 7 · **iF10–iF18 neu in § 8.2**, **iF19–iF23 aus der Umsetzung** · Stand je Frage im `Entscheidungsregister_iOS_EPOS-Plan.md` |
| M1–M10 | Migrationsregeln Modell C | ebenda § 6a.3 |
| A/B/C/D/E, K1–K6 | Arbeitsblöcke des Vollausbaus | ebenda § 6a.4 |
| Grundlagen-S0–S6 | Etappen der Portierung | ebenda § 5 |
| SQLite-S0–S8 | Etappen der Datenschicht | `Konzept_DB-Migration_SQLite_EPOS-Plan.md` § 12 |
| SQLite-D1–D8, R1–R4 | Entscheidungen und Risiken der Datenschicht | ebenda |
| SQLite-D9, R5, R6 | Fortschreibung aus der Bauanleitung | `Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md` (Branch `sqlite`) |
| P0–P5 | Pakete der 64-Bit-Umstellung | `Konzept_Umstellung_64Bit_EPOS-Plan.md` |

### 9.3 Was dieses Dokument nicht ist

- **Keine Terminzusage.** Vor iZ3 gibt es keine belastbare Aufwandszahl für iU9 ff. — deshalb steht
  das Gate vorn.
- **Kein Ersatz für die Windows-App.** Sie bleibt das Hauptwerkzeug; das iPad kommt dazu.
- **Kein Sync-Konzept.** Autonom heißt getrennt: zwei Geräte, zwei Stände, Austausch über eine Datei
  (Grundlagen § 4).
- **Keine Neufassung der Fachlichkeit.** Sie wandert, sie ändert sich nicht — jede Etappe beweist das
  per Wertgleichheit.
