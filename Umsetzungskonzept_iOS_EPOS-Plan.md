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
| **iU7** Charts und Berichte | ✔ hier erreicht 03.09. | `c6b32eb`..`f84932b`, `6604c05`..`0759b37`, `0af6421` | `Referenzlauf.exe bildvergleich` alt/neu (Vorbedingung für iF23)  Zoom seit 05.09.2026 über den Baustein `Diagramm` (Nachtrag im W11b-Block) |
| **iU8** `EPOS.UI`, erster Dialog | ✔ **iZ5 hier erreicht** 03.09. | A `8574911`..`8f5a28e`, `45a21dc`, `f5fb05c` · B `4369fdb`..`eafbc1f`, `eff82aa`, `e3d1e5b` · C `479fcf9`..`0af7ca7`, `4aa6b15` | Dialogabnahme (Maus/Finger, de/en, Hochkontrast, 125 %/150 %, Enter/Esc), Setup mit und ohne WebView2, VS-2026-Designer unter dem Razor-SDK. Anwenderwunsch **iU8‑E‑1** (05.09.2026, `ddf4d00`): Fachdialoge öffnen im Anteil des Arbeitsbereichs (85 % × 90 %, Deckel 92 %; `EPOS.UI/Dienste/Fenstermass.cs`), fünf kleine Masken als `Dialogart.Klein`; Abnahme je 100/125/150 % offen; **iU8‑E‑2** (05.09.2026, `6ab0b9f`): hausweite Formularregel — Baustein `Formularraster`/`Formulargruppe`, Beschriftung neben dem Feld, kurze Zahlenfelder mit Einheit, `auto-fill`-Spalten; acht Dialoge umgestellt, Rest als #91 in drei Paketen — Restumstellung #91 in drei Paketen abgeschlossen (P1 Erzeuger W6/W7, P2 Kosten W1–W5, P3 Bedarf/Simulation/Projekt W8–W16a: 41 weitere Dateien, alle `epos-feldpaar` gefallen; iU8‑O‑1 am 06.09. geschlossen (`e6bc2fd`, `PufferSpProjektDialog` im Raster) `PufferSpProjektDialog`) |
| **iU9** Masken in Wellen | ✔ **W0 bis W16 umgesetzt, M9 abgeschlossen** 04.09. | `ab3aea8` | **1** Designer-Maske offen (`Form_HelpPopup`, bis iU11; 2 nach W16b, 7 nach W16a, 11 nach W15c, 12 nach W15b, 13 nach W15a, 17 nach W14c, 21 nach W14a, 28 nach W14b, 32 nach W13, 38 nach W12, 43 nach W11b, 49 nach W10b, 50 nach W10a, 55 nach W9, 63 nach W8, 73 nach W7, 81 nach W6, 88 nach W5, 91 nach W4, 98 nach W3, 102 nach W2, 105 nach W0); Stilllegung nach iF29 abgeschlossen, Sprungbrücke steht, `ChartRenderer` um Kostenprofil, Kennlinien und die drei Bedarfsbilder erweitert, seit W5 die erste **Seite** (`BlazorSeite`, Reiter „Berichte & Kosten"), seit W6–W9 **alle elf Kacheln des Startbilds** (sieben Erzeuger, vier Bedarfe) und **zehn der dreizehn Assistentenseiten** als Razor-Komponenten; `WPCtrl`, `BedarfStammCtrl`, `TypProfilCtrl`, `Ferienzeit`, die Projektlisten der Bedarfsgewerke und das Suchmuster im Kern; seit W10a die sieben Quell-, Senken- und Pufferdialoge der Simulationskonfiguration mit dem Baustein `Bildkarte` und dem zweireihigen `Jahresgang`, seit W10b die **Simulationskonfiguration als Seite** mit Kartenspalten, SVG-Schema (`SchemaModell`/`SchemaLayout` im Kern) und drei Überlagerungsebenen in einer WebView; seit W11a die Ergebnisrechnung der Detailansicht als DTOs im Kern (`SimulationErgebnisCtrl`), der **nebenläufige Simulationslauf** (`SimulationLaufCtrl`, `Do_Simulation` mit Fortschritt und Abbruch), sieben Ergebnisbilder im Renderer (30 Proben) und der Baustein `Fortschritt`; seit W11b die **Ergebnisseite der Simulation** (`SimulationErgebnisSeite`, zehn Blätter, Autarkie, Ganglinien-Navigatoren, Variantenvergleich als Überlagerung) — `Form_Simulation_Detail` mit 7 766 Zeilen ist gelöscht; seit W12 die **AP5-Importkette als ein Kern-Ablauf** (`GanglinienImportAblauf` mit zwölf bitgleichen Proben), `StromganglinieDialog`, `StromganglinieAdminDialog`, `PeakShavingDialog` (nebenläufig) und der gemeinsame `ImportKonflikteDialog`; seit W13 die **Katalog-Importe** — `KatalogImportDialog` mit vier Ausprägungen (`KatalogImportProfil`/`KatalogImportAblauf` im Kern, transaktional), `WaermebedarfAdminDialog`, `PvModulImportDialog` (CEC/PAN), die Mehrfachmarkierung im `Raster` und zwanzig eingefrorene Importproben; die `ImportKonflikteHuelle` und die Sprungbrücke `WaermebedarfExternAdmin` sind gefallen; seit W14b die **Bedarfs-Admin** — `BedarfAdminDialog` mit drei Ausprägungen über `BedarfsArt` (`BedarfsVorschauCtrl` im Kern) und `SolarganglinieAdminDialog` (Sprungziel → Überlagerung), `ToolsClass` gefallen; dazu der Anwenderentscheid **Energieeinheit MWh/kWh wählbar** (W8‑O‑5/W9‑O‑3); seit W14a die **Erzeuger-Admin** — `KatalogBrowserDialog` mit vier Ausprägungen (`KatalogBrowserProfil`), `PufferSpKatalogDialog` (der vierte Katalogeditor), `ModulKatalogDialog` (PV, Stromspeicher), die Heizkessel-Brennstoffkette im Kern berichtigt, die letzten fünf ablösbaren Sprungziele → Überlagerungen, `SpeichernLeiste`/`KiAufrufKnopf`/`PufferSpFilter` gefallen; **der Erreichbarkeitsbefund steht auf 0 nein / 0 verwaist / 0 unklar**; seit W14c die **Verwaltung** — `GesetzeskatalogDialog` (Zeilendialog als Überlagerung), `KatalogDublettenDialog` mit dem Baustein `Baumansicht`, `EinstellungenDialog` (`EinstellungenCtrl` im Kern), `KlimadatenDialog` (`KlimaregionStammCtrl` und `KlimaImportAblauf` im Kern, zwei Klimabilder im Renderer → 32 Proben); die **letzten zwei ablösbaren Sprungziele** → Überlagerungen, `ChartManager` (die MS-Chart-Bindung) und `RoundedPanel` gefallen, **WFO1000 6 → 0**, Warnungen der Mappe 12 → 6; **Anwenderentscheide W14c E‑3/E‑5/E‑6/E‑7 vom 04.09.2026 umgesetzt** (`a0e6707`: Komponente wieder `KlimadatenDialog`, feste Pfade ohne Ordnerwähler nur lesend, **Altbereinigung der Klimadaten-Waisen als Schema-Schritt 62** — `ZIEL_VERSION` 62, neu `FREEZE_VERSION` 61 —, keine Ortsliste in der Auslieferung); seit W15a das **Projekt** — Baustein `ProjektListe` (vier Projektlisten des Bestands werden eine), `ProjektWahlDialog` (Öffnen und Löschen), `ProjektKopieDialog`, `ProjektTransferDialog` (`ProjektExportImportCtrl` im Kern, `SchemaStand.Zielversion`; **der Projektimport war seit der SQLite-Umstellung kaputt, B55 — von den Proben gefunden und behoben**), `ProjektKopfSeite` (die erste Assistentenseite als Razor, über `BlazorAssistentSeite`); `ProjektAuswahl` (uc) bleibt bis W16; seit W15b **Hilfe und KI** — `KiChatService` (1 751 Z.) im Kern hinter der Naht `IKiAusfuehrung`, die Bausteine `Gespraechsverlauf` (Bausteinlücke 17) und `KiKnopf`, `Warnbanner.Verfaellt`, `TextAnzeige`, `KiHinweisDialog`, `KiEinstellungenDialog`, `KiChatDialog` in vier Kindern (kein Streaming, kein Markdown, Schlüssel nie durchgereicht, Riegel vor dem `Modellkanal`); `Form_HelpPopup` (E‑2, fällt in iU11) und `Form_Hinweis` (E‑1b, fällt mit W16) bleiben bewusst; seit W15c **Lizenz und Erststart** — `LizenzVerwaltungDialog`, `ErststartDialog` (besitzerlose Hülle mit vier Zusätzen an `BlazorDialogForm`) und `LizenzDialog` (drei Reiterblätter, Zustimmungsmodus, Browserdruck), im Kern `LizenzManager.Bewerten`, `LizenzCtrl`, `LizenzTextCtrl`, `ZustimmungCtrl`; **die ersten Lizenztests überhaupt** (+79 Kern-, +67 bunit-Fälle); **E‑8 Weg 2: `Program.Main` prüft die WebView2-Laufzeit und endet mit Meldung, wenn sie fehlt**; iF30 (Lesemodus-Durchsetzung) nach W16; seit W16a **der Assistent** — `KomponentenBestandCtrl` im Kern mit **Nachweis N6** (Bitmaske bitgleich für alle 13 Referenzprojekte), `AssistentCtrl` und `WizardCtrl` im Kern, Baustein `Assistent`, Seite `AssistentSeite` (13 Seiten in Bestandsreihenfolge), `KomponentenauswahlDialog`, `Kachel.Zustand`; `WizardParent`, `Wizard_Komponenten`, `Wizard_Stromlastgang`, `ProjektAuswahl` (uc) und `BlazorAssistentSeite` gefallen (26 Dateien); `Views/Wizard` und `Views/Projekt` führen keine Designer-Maske mehr; seit W16b **die Startseite** — Seite `Startseite` (`EPOS.UI/Seiten/Start/`, Kopfband, sechs Reiter mit 21 Kacheln in fünf Reiterkomponenten, Reiter 6 = `BerichteKostenSeite`), im Kern `ProjektKontextCtrl` (**Nachweis N7**), `StartseiteCtrl` und `BedarfsZustand`, `StartseiteHuelle` im `MDIMainForm_Load`, `Dienste.Projekt` über den Kern; **E‑5** (Simulationskonfiguration als freie Ansicht, Ergebnis als `Ueberlagerung` — **R‑W10b‑1 und R‑W11‑1 eingelöst**), **E‑7** (`FormMain`, `Form_StromTest`, `StromTestClass` und zwölf `*KontextMenuCtrl` ohne Nachfolge gefallen), **E‑9** (`Form_Start`-Designer als Prüfmuster eingefroren); `Form_Start` (+`.bak`), `AktionsKarte`, `Form_Hinweis`, `FormStartProjektKontext` gefallen — **34 Dateien, 13 019 gegen 5 549 Zeilen**; `Program.startfrm` weg; `WindowsFormsApplication1` führt noch `MDIMainForm` und `Form_HelpPopup` und **null Inline-SQL** (B34); seit W16c **das Hauptfenster** — Baustein `Menueband` mit der aus dem Designer erzeugten `Menuetabelle` (54 Punkte, **Nachweis N4**), Seite `Hauptfenster` hinter `HauptfensterHuelle`, `Seitenschluessel` als die eine Schlüsseltabelle beider Plattformen (K7), `AppWurzel` als gemeinsame Wurzel (E‑1); `MDIMainForm` auf die Hülle zurückgebaut (873 → 129 Zeilen, Designer und drei `.resx` als Prüfmuster, E‑9) und in `Hauptfensterrahmen` umbenannt (E‑10), **Per Monitor V2 statt `DpiInsel` (E‑6, iF21)**, Zeugen und Schwellen der Formularkarte auf N1/N2; **die Mischphase (M9) ist zu Ende** — `WindowsFormsApplication1` führt eine Designer-Maske (`Form_HelpPopup`, bis iU11), null Inline-SQL und die `Sprungbruecke` mit einem Zweig (iF22) |
| **iU10** iOS-Hülle `EPOS.iOS` | ✔ hier erreicht 03.09., seither je Welle im CI geprüft | `ios.yml`-Läufe 15–22 grün (außer 18), zuletzt 33898599945 auf `c8fbd77` | Gerätebefunde (iU13), siehe `Umsetzung_iU10_Nachweise.md` |
| **iU11**–**iU13** | ⏳ nicht begonnen | — | — |

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

> **Statusblock iU9 — Teilwelle 16c umgesetzt, Welle 16 und Meilenstein M9 abgeschlossen (04.09.2026, Basis `c8fbd77` nach W16b, zusammengeführt mit `97b048c` nach dem zweiundzwanzigsten iOS-Lauf)**
>
> **Das Hauptfenster ist Razor, und `WindowsFormsApplication1` führt keine Fachmaske mehr — die Mischphase (M9) ist zu
> Ende.** `MDIMainForm.cs` 873 → **129** Zeilen (die Hülle: `Form` + `Application.Run`, eine `BlazorWebView` mit
> `Hauptfenster.razor`, Besitzer der `BlazorDialogForm<T>`, F1, `Application.Restart()` beim Sprachwechsel,
> `LizenzManager.NachpruefungImHintergrund()`), `MDIMainForm.Designer.cs` (493 Z., 45 `ToolStripMenuItem`) und die drei
> `.resx` (4 000 Z.) **vor dem Rückbau** als Prüfmuster `Pruefmuster/Hauptformular/` eingefroren (E‑9), die acht `Init*`
> und 34 `MenuItem_*`-Handler gefallen, `MenueCtrl` 347 → 257 Z. (26 → 6 Methoden), `WinFormsNavigation` 269 → 256,
> `BlazorDialogForm` 386 → 301 (die `DpiInsel` samt zwei `ShowDialog`-Überladungen weg). Neu in `EPOS.UI`: Baustein
> **`Menueband`** (`Menuepunkt`, **`Menuetabelle` per Skript aus dem Designer erzeugt, nicht abgetippt** — R‑W16‑8; **54**
> Punkte und 8 Trenner, nicht 45: die acht `Init*` hängten neun Punkte programmatisch ein, B2; vier `.resx`-Leichen und
> sieben fehlende englische Beschriftungen bereinigt, B3/B4; die toten Handler `MenuItem_PV_Import_PAN`/`MenuItem_PV_Import`
> gefallen, W16‑B24; der KI-Assistent eine Tabellenzeile statt einer Suche über den Anzeigetext, W16‑B23), Seite
> **`Hauptfenster`** (Menüband + Kopfband PRODUKTNAME/GATTUNG/CLAIM/Version + Inhaltsfläche, `Springe(schluessel)` als
> einziger Handler) hinter **`HauptfensterHuelle`**, **`Seitenschluessel` als die eine Schlüsseltabelle beider Plattformen**
> (K7, E‑1/E‑2: 34 Werte, die übernommenen `Masken`-Werte sind Verweise, `INavigation.OeffneMaske` bleibt; `Masken.PvImport`
> fehlte seit W13 im ASCII-Zeugen, B1) und **`AppWurzel` als gemeinsame Wurzel** (eine Wurzel, zwei Schalen: `Kopfleiste` als
> `RenderFragment` trägt unter Windows das Menüband, auf iOS nichts; `Startansicht` ist die Ansicht beim Aufmachen und das
> Ziel des Rückwegs). **E‑6/iF21 eingelöst:** `app.manifest` Per Monitor V2, `Program.Main` `HighDpiMode.PerMonitorV2` — der
> Gerätebefund bei 100/125/150 % steht aus (`Umsetzung_iU9_Nachweise.md` § 12.1). Fensterhilfe im Kopfband
> (`Hauptfenster.btn_Help`, W16b‑O‑4), `HilfeKontext`, `help_mapping.txt`, vier `CLAUDE.md` und
> `Konzept_iOS-Portierung_EPOS-Plan.md` (M9 ✔) nachgezogen. Sieben Sachcommits, Protokoll, Merge und Gate-Nachtrag
> (`915e0a7` … `54b7c96`), auf `ios_migration` als `ab3aea8`; **`WindowsFormsApplication1` führt noch EINE Designer-Maske**
> (`Form_HelpPopup`, bis iU11, W15b‑E‑2), die Hülle `MDIMainForm` ohne Designer, **null Inline-SQL** und die `Sprungbruecke`
> mit einem Zweig (`Form_SpeicherOptimierung`, iF22). `EPOS.iOS` ist unberührt — die drei neuen Schnittstellenglieder
> (`StartseiteGaben`, `BerichteKostenGaben`, `AdresseOeffnen`) haben Standardumsetzungen, die `AppWurzel` sagt es im Banner.
>
> **Neun Angleichungen** (A‑1…A‑9: das Menü klappt beim Klick auf, nicht beim Überfahren; `&&` → `&` in vier Menütexten;
> die nie lesbare Ladeanzeige `label_OnlineDoku` entfällt; der Titel ist von Anfang an „EPOS-Plan", W16‑B22; „Über" über
> `Dienste.Dialog`; Browserstart über `Dienste.Datei.AdresseOeffnen` statt `Process.Start`, B8; die 21 einzeiligen
> `MenueCtrl`-Methoden entfallen; die Fensterhilfe sitzt im Kopfband; sieben stille `Console.WriteLine` entfallen) und die
> Befunde W16c‑B1…B10, darunter: **die N1-Sollwerte „0/0" gehen nicht auf** — sie sind vom Stand vor W15b gerechnet,
> geprüft wird die starke Form „genau eine Maske, und zwar `Form_HelpPopup`" (B7); `Program.cs` brauchte keine
> Bereinigung (B9); `AppWurzel.ZurueckZurListe` räumte `_simErgebnis` nicht ab (B10, behoben). **R‑W16‑10 eingelöst**
> (`Form_HelpPopup` meldet „ja", `MDIMainForm` bleibt als Klasse Wurzel des Graphen, ist aber keine Maske mehr),
> **W16b‑O‑1 erledigt** (der Maskenschlüssel-Zeuge ist über einen Sprungtabellen-Auszug im Prüfmuster zurück).
> **Anwenderentscheide 04.09.2026:** E‑1, E‑2, E‑6, E‑8a, E‑9 bestätigt; W16c‑E‑1 (das Menü klappt beim Klick auf)
> bestätigt; **W16c‑E‑2: Untermenü „Sprache"** und **W16c‑E‑3: Ansichtswechsel** umgesetzt (`a9797d1`: die zwei Sprachpunkte
> sind Untereinträge des Kopfes „Sprache", N4 jetzt 55 Punkte / 8 Trenner / 4 Köpfe, W16c‑O‑3 erledigt; „Varianten und
> Bericht…" wechselt die Ansicht der `AppWurzel` auf `BERICHTE_KOSTEN` wie auf iOS, Windows liefert die
> `BerichteKostenGaben` aus derselben Hülle wie das sechste Reiterblatt, Rückweg über `ZurueckZurListe`; dabei Befund
> W16c‑B11: `IProjektQuelle` fehlte im Windows-Dienstverzeichnis, `KeineProjekte` eingetragen — Abnahmepunkt W16c‑O‑6;
> Gate auf dem gemergten Stand: 0 Fehler / 6 Warnungen, **4 012** Tests auch unter `en_US`, Formularkarte 122, Referenzlauf
> byte-gleich). **E‑10 entschieden 04.09.2026: `MDIMainForm` → `Hauptfensterrahmen`** (umgesetzt `c7f989b`, W16c‑O‑1 erledigt;
> nicht `Hauptfenster`, das ist die Razor-Seite); **W16a‑E‑1/W16b‑O‑5 entschieden 04.09.2026: der Assistent wird in
> iU11 zusammen mit der Transaktion W16a‑O‑1 eine freie Ansicht der `AppWurzel`**, bis dahin modal; **W16b‑E‑1 und
> W16b‑E‑2 bestätigt 04.09.2026**; **iF30 entschieden 04.09.2026** (streng über die Schreibnaht im Kern, eigene Welle
> nach der Windows-Abnahme, siehe Register). **Was iU11 erbt:** `Form_HelpPopup` (fällt mit
> `HelpCatalog`/`HelpExtender`, Ersatz `IHilfeDienst` steht), die `Sprungbruecke` mit einem Zweig, E‑10, W16b‑O‑3 erledigt
> (`bd0592a`, eine Wahrheit im Kern), die drei iOS-Standardumsetzungen, die DPI-Abnahme (W16c‑O‑2),
> `Seitenschluessel` mit 34 Werten in einer Klasse (W16c‑O‑4, Teilung entlang Ansicht/Maske/Weg), keine Menüfreischaltung
> nach Projektzustand wie im Bestand (W16c‑O‑5); die `WFO1000`-Herabstufung kann mit `Form_HelpPopup` entfallen.
>
> **Nachweise** (auf dem gemergten Stand `ab3aea8`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **4 002** grün (3 968 nach W16b; N4 `MenuebandTests` 15 Fälle — 54 Punkte, 8 Trenner,
> 5 Köpfe, jeder Klick ein bekannter `Seitenschluessel`, jede Beschriftung de und en aus `MyResource`, 11 Bilder; dazu
> `HauptfensterTests`, `AppWurzelTests`, `SeitenschluesselTests`), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte
> **122** grün (+1: der zurückgeholte Maskenschlüssel-Zeuge; **N1** `Masken == 1` = `Form_HelpPopup` mit Namen,
> `Erreichbar(Ja) == 1`; **N2** das Prüfmuster liefert weiter Karten, Skelette und Erreichbarkeit) · Stapellauf **1** Maske /
> 2 Designer (**Sollwert exakt getroffen**), 0 lokalisiert, **1 erreichbar / 0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer
> 1 200 Texte, 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich**
> (815 043 Werte; der Startweg Erststart → Lizenz → `NachpruefungImHintergrund` ist inhaltlich unverändert) · beide
> Wächter leer.
>
> **Protokoll** mit der Feldkarte (eine Kartenzeile), der erzeugten Menütabelle (§ 4), den neun Angleichungen, den
> Befunden, den Zeugen der Formularkarte (§ 8), der Löschliste mit `git grep`-Nachweis und der **Vollabnahme N1–N10 in
> sechzehn Punkten**: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W16c_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme 04.09.2026, erster Befund W16c‑B12 (Startabsturz):** `BlazorSeite<T>` gibt jedem Parametersatz
> einen `SeitenZustand` unter dem Schlüssel `Zustand` mit; seit W16c ist die Wurzel `BlazorSeite<Hauptfenster>`, und
> `Hauptfenster.razor` führte den Parameter nicht — Blazor warf beim ersten Rendern, verpackt als
> `TargetInvocationException` bei `Application.Run`. Behoben in `1429712`: `Hauptfenster` und `AppWurzel` nehmen den
> Zustand (die Hüllen der Startseite und der Berichte behalten ihre eigenen), `BlazorSeite` prüft per Reflexion und
> meldet lesbar, `HauptfensterTests` rendern über `AddMultipleAttributes` wie die Hülle (Gegenprobe mit unbekanntem
> Schlüssel rot); die bunit-Tests sahen es nicht, weil sie mit getippten Parametern rendern (B12a, Parallele zu B11).
> Abnahmepunkt 0 „Start" bleibt bis zum Gerät offen (W16c‑O‑7). **Windows-Abnahme
> steht aus** — Erststart mit Lizenz, 54 Menüpunkte in drei Ebenen mit Tastatur und F1, Kopfband, 21 Kacheln, Assistent,
> Simulation, Bericht, Sprachwechsel, **DPI 100/125/150 %** mit `Form_HelpPopup` und `Form_SpeicherOptimierung` als
> Kandidaten für eine echte Abweichung, Monitorwechsel, Setup mit WebView2. Der vierundzwanzigste iOS-Lauf
> (33904433007) auf diesem Stand ist grün — erstmals mit `Hauptfenster`, `Menueband` und der `AppWurzel` als gemeinsamer
> Wurzel (N9; Lauf 23 war eine abgebrochene Dublette).
> **Windows-Abnahme 05.09.2026, W16c‑E‑4 („Sprache sollte oben rechts sein") umgesetzt (`4bcd981`):** der Kopf
> „Sprache" steht am **rechten Rand** des Menübands — `Menuepunkt.RechtsBuendig` aus der erzeugten `Menuetabelle`,
> das `Menueband` hängt `epos-menueband-punkt--rechts` (`margin-left: auto`) an. **Nur die Optik wandert**:
> Markup-Reihenfolge, Tastaturweg (Ende = „Sprache"), Sprachausgabe und N4 bleiben unverändert; fünf bunit-Fälle.
> Der Abnahmepunkt 3a „Wo ‚Sprache' steht" ist neu in der Liste, die Sichtprüfung bleibt beim Anwender.
> **Anwenderwunsch W16c‑E‑5 vom 05.09.2026 (Farbgebung wie vor W16), umgesetzt (`04d5ac6`):** Menüband und
> Kopfband folgen `menuToolbar` und `MDIMainForm.InitMarke` — AliceBlue #f0f8ff, vier Köpfe in 16 px, kühle
> Trennlinie #dee3e8, Produktname 19 px, Gattung und Claim 11 px in #70777e. Fünf Werte, die bisher nur als
> Rückfall in der Regel standen (`--epos-marke`, `--epos-marke-untertitel`, `--epos-marke-trennlinie`,
> `--epos-menue-flaeche`, `--epos-flaeche-hell`), sind jetzt Token in `:root`; die Menühöhe bleibt beim
> Berührungsziel 44 px, die Versionsfarbe des Bestands ist wegen 2,77:1 nicht übernommen. Abnahmepunkt 7a.
> **Windows-Abnahme 05.09.2026 (PDF des Anwenders, S. 3), Befund W16c‑B13 (`78f32a7`):** die verschachtelten
> Untermenüs des Menübands („Administration → Wärmebedarf & Heizung ▸ …") ließen sich nicht aufklappen. Drei
> Ursachen: `@onfocusout` am `<nav>` schloss die Klappe schon beim Zeigerdruck, weil `focusout` auch bei einem
> Fokuswechsel **im Band selbst** feuert und die gedrückte Zeile beim Loslassen aus dem DOM war (kein `click` mehr;
> `FocusEventArgs` kennt kein `relatedTarget`, auf dem iPad setzt eine Berührung gar keinen Fokus); die zweite
> Ebene führte keinen eigenen Offen-Zustand (ein Feld für oben, ein flaches Namens-Set darunter, kein Ausschluss
> unter Geschwistern); und die Tastatur kannte weder → noch ← noch ein Wandern in der offenen Klappe. Behoben: der
> Offen-Zustand ist ein **Pfad über alle drei Ebenen**, eine Schließfläche (`position: fixed; inset: 0`) ersetzt
> `focusout`, `→ ← ↑ ↓` wandern in der Klappe mit rovendem `tabindex`, `OpenRegion` je Kind und Verweisfang je
> Zeile (ein bedingter `AddElementReferenceCapture` brach Blazors Abgleich). 14 hüllengleiche Wachen mit der
> echten `Menuetabelle`, darunter die Gegenprobe zur Ursache; Abnahmepunkte 4a (Maus/Berührung) und 5a
> (Tastatur); drei Hausregeln in `EPOS.UI/CLAUDE.md`.

> **Statusblock iU9 — Teilwelle 16b umgesetzt (04.09.2026, Basis `84d7c16` nach W16a, zusammengeführt mit `d4a7632` nach dem einundzwanzigsten iOS-Lauf)**
>
> **Die Wurzel der Anwendung aus Anwendersicht ist Razor, und der Altzweig ist weg — 34 Dateien, 13 019 gelöschte gegen
> 5 549 neue Zeilen:** `Form_Start` (2 339 Z. `.cs` + 1 864 `.bak`, 1 381 Designer, 4 900 `.resx`) → Seite **`Startseite`**
> (`EPOS.UI/Seiten/Start/`: Kopfband mit Projektauswahl, Statuszeichen und Klimafeld, sechs `Reiter` mit 21 Kacheln in den
> fünf Reiterkomponenten `ProjektReiter`, `WaermebedarfReiter`, `StrombedarfReiter`, `ErzeugerReiter`, `SimulationReiter`,
> Reiter 6 = `BerichteKostenSeite` aus W5; Kachelzustand aus `KomponentenBestandCtrl`, Reitersperre und die drei
> `Form_Hinweis`-Aufrufe über `Warnbanner.Verfaellt`) hinter **`StartseiteHuelle`** (`BlazorSeite<Startseite>` im
> `MDIMainForm_Load`); `FormMain` mit `Form_StromTest`, `StromTestClass` und den zwölf `*KontextMenuCtrl` (E‑7, Altzweig
> K6‑a, 6 682 Z.) **ohne Nachfolge**; `AktionsKarte` → `Kachel`; `Form_Hinweis` → `Warnbanner.Verfaellt`;
> `FormStartProjektKontext` → **`ProjektKontextCtrl`** im Kern (K2, **Nachweis N7 zuerst**: der Wechsel zieht Id, Name und
> Klimazone zugleich nach, ein unbekannter Name lässt den Kontext stehen, `Uebernehmen` schreibt „zuletzt geöffnet",
> `Setzen` nicht), dazu **`StartseiteCtrl`** (K4, die vier SQL mit `DbParam`) und **`BedarfsZustand`** (E‑5: die zwei
> Bedarfsobjekte gehören dem Projekt, nicht mehr einem Fenster). **E‑5 umgesetzt:** die Simulationskonfiguration löst
> die Startseite in derselben WebView ab, das Ergebnis liegt als `Ueberlagerung` darüber, die zwei modalen Hüllen sind
> gefallen — **R‑W10b‑1 und R‑W11‑1 damit eingelöst** (in den Blöcken W10b/W11b nachgetragen). `Dienste.Projekt` läuft
> über `ProjektKontextCtrl`, `Program.startfrm` gibt es nicht mehr, `IProjektQuelle.Startkacheln(int)` (K6) mit
> Standardumsetzung. 78 neue Texte de/en, darunter erstmals englisch die drei Literale aus dem Code (B1).
> `Form_Start.Designer.cs` und drei `.resx` als Prüfmuster `Pruefmuster/Hauptformular/` eingefroren (E‑9), alle elf
> Typzeugen hängen am Prüfmuster. Sieben Sachcommits, Protokoll, Merge und Gate-Nachtrag (`b10cfc1` … `666fe4f`), auf
> `ios_migration` als `ff60252`; **`WindowsFormsApplication1` führt noch zwei Masken** (`MDIMainForm`, `Form_HelpPopup`) **und
> null Inline-SQL** (B34 eingelöst).
>
> **Zehn Angleichungen** (A‑1…A‑10: 13 `Paint`-Handler → CSS, drei Bindemuster → ein `@onclick` je Kachel,
> `UpdateWizardSymbole` → `KomponentenBestandCtrl`, der Hinweis der Reitersperre steht vorher statt nach dem Klickversuch,
> `Form_Hinweis` → Banner 3 s, fünf `MessageBox` des Klimaspeicherwegs → ein Banner, die Statusfarbe der Solar-Radiobuttons
> entfällt, `IProjektKontext.Vorhanden = true` wie auf iOS, „Öffnen…"/„zuletzt geöffnet" setzen das Projekt aktiv statt
> ein Detailformular zu zeigen, gerechnete Rechtsbündigkeit → CSS) und die Befunde W16b‑B1…B8, darunter:
> **`IosProjektKontext` liest die Klimazone anders** (Stammname statt Projektkopie) — der Kern übernimmt den Windows-Weg,
> **W16b‑O‑3 entschieden 04.09.2026 („iOS-Lösung"): die Messung zeigte, dass die iOS-Abfrage den falschen
> Schlüsselraum las — `ID_Klimaregion` ist die Id der Projektkopie, der Stammname war auf iOS immer leer; umgesetzt als
> EINE Wahrheit im Kern (`StartseiteCtrl.ProjektKlimazone` liest die Projektkopie, die Stammabfrage fällt),
> `IosProjektKontext` läuft über `ProjektKontextCtrl`, N7 15 Fälle, `bd0592a`** (B2); `ProjektTransferDialogTests` flatterhaft, nicht von dieser Welle (B7,
> W16b‑O‑2); die Stapellauf-Sollzahl „1/2" der Anweisung ist die von nach W16c, gemessen 2 Masken / 3 Designer (B8).
> **Anwenderfragen:** **E‑7 umgesetzt** — verloren gehen die Gewerksübersicht in Listenform und das Drag & Drop
> zwischen den zwölf Listen; an ihrer Stelle dieselben zwölf Gewerke als Kacheln mit Statuspunkt, jede führt in
> denselben Dialog wie das Kontextmenü; **nicht ersetzt** ist das Verschieben eines Katalogeintrags per Maus; **E‑5
> umgesetzt**; E‑1/E‑2 vorbereitet (K7 ist W16c); E‑9 umgesetzt; **W16a‑E‑1 bleibt offen** (der Assistent bleibt modal,
> an ihm hängt ein Schreibweg — technisch wäre die freie Ansicht jetzt möglich, W16b‑O‑5); **neu W16b‑E‑1** (der Reiter
> „Simulation" springt ohne Klimaregion sichtbar auf Reiter 1 zurück, die Meldung steht als Banner oben statt als
> `MessageBox` — bestätigt 04.09.2026) und **W16b‑E‑2** (der Reiter „Berichte & Kosten" wird von Anfang an gehalten, ein
> Ladevorgang mehr beim ersten Variantenwechsel — bestätigt 04.09.2026). **Was W16c erbt:** `MDIMainForm` nur an vier Stellen angefasst
> (`MDIMainForm_Load`, `MenuItem_Neu`/`_ProjektBearbeiten` → `projektkontext.Setzen`, `_AlsVariante` → `Dienste.Projekt`,
> `_VariantenBericht` → `StartseiteHuelle.Aktuelle`), Menü, `Init*`, Kopfband, F1 und Sprachwechsel unberührt; der
> „ja"-Zeuge steht an `MDIMainForm` und ist beim Rückbau umzuhängen, der Maskenschlüssel-Zeuge ist gestrichen (W16b‑O‑1,
> rückholbar über einen Sprungtabellen-Auszug im Prüfmuster); `Erreichbarkeit.Wurzelmasken` = nur `MDIMainForm`
> (W16‑B3 erledigt); `AppWurzel` unberührt, `Seitenschluessel.STARTSEITE` ist K7; `Form_Start.btn_Help` (die Fensterhilfe)
> wandert ans Hauptfenster (W16b‑O‑4).
>
> **Nachweise** (auf dem gemergten Stand `ff60252`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 968** grün (3 923 nach W16a; N7 `ProjektKontextCtrlTests` 12 Fälle, N3
> `StartseiteTests` 21 Fälle — sechs Reiter, 21 Kacheln 5/4/3/7/2, Kachelzustand aus der Bitmaske, Reitersperre,
> Projektwechsel über `SeitenZustand`, die E‑5-Ansichten), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte
> **121** grün (T1 gestrichen) · Stapellauf **2** Masken / 3 Designer (B8), **2 erreichbar / 0 nein / 0 verwaist /
> 0 unklar** · SQL-Prüfer 1 200 Texte, 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017
> **PASS, byte-gleich** (815 043 Werte) · **Referenzlauf mit Projektwechsel** (§ 16.3, R‑W16‑4): 1030→1007 und 1007→1030
> byte-gleich zur Basis und untereinander, dazu der Kern-Fall 1030→1007→1030 · beide Wächter leer.
>
> **Protokoll** mit dem Feldkartenabgleich der 108 Kartenzeilen, den zehn Angleichungen, den Befunden, der Löschliste mit
> `git grep`-Nachweis und **sechzehn Abnahmepunkten**:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W16b_Blazor_Port_Protokoll.md`. **Windows-Abnahme steht aus**,
> darunter alle 21 Kacheln, der Projektwechsel im Kopfband, die Konfiguration als Ansicht, das Ergebnis als Überlagerung
> und **DPI 100/125/150 %** (die Seite sitzt bis iF21/W16c in der DpiUnaware-`MDIMainForm`, R‑W16‑2 — die Abnahme hält
> fest, wie unscharf es ist). Der zweiundzwanzigste iOS-Lauf (33898599945) auf diesem Stand ist grün — erstmals mit der
> Razor-Startseite und `IProjektQuelle.Startkacheln` im gemeinsamen `EPOS.UI` (N9; iOS setzt die Startkacheln noch nicht
> um, der Zweig in der `AppWurzel` kommt mit K7 in W16c).
> **Windows-Abnahme 05.09.2026 (erstes Bildschirmfoto der gestylten Startseite — „Icons fehlen … Design ähnlich
> WinForms"), W16b‑E‑3 und W16b‑E‑4 umgesetzt (`4bcd981`):** die **21 Kachelbilder und Symbole** von `Form_Start`
> wandern per `git mv` unverändert nach `EPOS.UI/wwwroot/bilder/start` (Zuordnung Kachel → Datei in
> `Seiten/Start/Kachelbilder.cs`, der Ausschnitt 84 × 84 im Stilblatt über `object-fit`, aus `Properties/Resources`
> samt drei Leichen ausgetragen; `PHeizkessel.jpg` lag nur eingebettet in `Form_Start.resx`, Bytes nachgemessen;
> die Konfigurationskachel hatte im Vorläufer kein Bild und bekommt `PSchnellSim.jpg`, das einzige Kachelbild ohne eigene Kachel). Die
> **Gattungszeile** der Startseite steht nur noch **ohne Kopfleiste** (`Startseite.KopfbandZeigen`, von der
> `AppWurzel` nach dem Parametersatz gesetzt: Windows nennt die Gattung schon im Markenkopf, iOS behält die Zeile).
> Die **Anordnung** folgt `Form_Start.Designer.cs` ohne feste Pixelkoordinaten: Kopfband über den zwei Kästen, Klima
> links und Projekt rechts (Seh- und Tabreihenfolge), Statuszeichen vor der Beschriftung, Globus im Klimakasten,
> Schriftgrade des Markenbands, der Reiter und der Überschriften, **drei Kachelspalten** (Mindestbreite 404, das
> Raster läuft schmal nicht mehr über), Sinnbild links vom Titel, Erläuterung darunter, Zurück/Weiter 132 px fett.
> Nicht angeglichen: der Bildknopf „Speichern" bleibt beschriftet, der Infoknopf beim Haus-Token 28 px, keine
> feste Kachelhöhe. 18 neue bunit-Fälle; die Sichtprüfung (Abnahmepunkte 1, 2a, 2b) bleibt beim Anwender.
> **Anwenderwunsch W16b‑E‑5 vom 05.09.2026 („Design und Farbgebung … angelehnt an winforms Version vor‑W16"),
> umgesetzt (`04d5ac6`):** die Startseite trägt wieder die Anmutung von `Form_Start` (erhoben aus Designer,
> drei `.resx` und den zwei `*_Paint` des Standes `84d7c16`) — Reiterleiste auf eigenem Grund mit **gefüllter
> aktiver Zunge** in #005aa0 und weißer Schrift, weißes Reiterblatt mit kühlem Rahmen, die zwei Kopfkästen in
> #b4becd mit 8 px Rundung und 16‑px‑Beschriftungen, jede Erläuterung halbfett in DimGray, der
> Zusammenfassungskasten auf #f9fafc, die Knöpfe in LightGray. Sieben neue Token `--epos-start-*` gelten **nur**
> für `Seiten/Start`, damit der gemeinsame Farbsatz der Dialoge nicht kippt; drei Farben des Vorläufers sind
> wegen des Hauskontrasts 4,5:1 bewusst in derselben Familie ersetzt (weiß auf #6876df bei 16 px, 128,128,255 auf
> #f9fafc, die Versionsfarbe 150,156,162). Nur Stilblatt, kein Markup, keine feste Pixelkoordinate; Wache
> `StartseiteAnmutungTests` (30 Fälle: Token, tragende Regeln, Kontrast nach WCAG 2.1 aus den Stilblattwerten
> nachgerechnet); Abnahmepunkte 2c–2g für die Sichtprüfung. Nicht angeglichen: Statusanstrich der ganzen
> Kachel (A‑1), Kachelbeschriftung mittig (W16b‑E‑3), 29‑px‑Menühöhe und 65‑px‑Kopfkästen (Berührungsziel
> 44 px, M2/iL4), 1,5‑px‑Rahmen.
> **Windows-Abnahme 05.09.2026, Befund W16b‑B‑1 („die Dialoge auf der Startseite sind leer"), `f889e9e`:** Das
> modale Fenster zeigte die Hüllenfläche #F5F4EF, die zweite WebView2 zeichnete nichts. Als Ursache **ausgeschlossen**:
> Parametersatz (61 Hüllenstellen und 13 Assistentenseiten gegen die `[Parameter]` geprüft, 0 Treffer), Ausnahme
> beim ersten Zeichnen (alle 21 Kachelziele rendern ohne Gaben), Stilblatt (die Dialogwurzeln stehen vor Z. 1386 und
> waren nie abgeschaltet), Faden, WebView2-Laufzeit. Übrig bleibt der **Weg selbst**: Seit Startseite und Hauptfenster
> Razor sind (W16b.2/W16c.2), öffnet jeder Kachelklick und Menüpunkt ein modales Fenster mit einer ZWEITEN WebView2
> aus dem `WebMessageReceived`-Rückruf der ersten heraus — genau das, was `Sprungbruecke` seit W2.2 als Risiko R2
> ausschließt; die Belege vom 04.09. (W4‑B‑1, W5‑B‑1) hingen noch an WinForms-Klicks. `Blazorsprung` lässt das
> Ereignis zu Ende laufen und fährt den Sprung eine Nachricht später (`BeginInvoke`, Wiedereintrittssperre) — an den
> **zwei** Verteilern `StartseiteHuelle.Kachelweg` und `HauptfensterHuelle.Weg`, nicht in 21/55 Aufrufern; der
> synchrone Vertrag von `Weg` bleibt. `WebViewWache` hängt sich an `CoreWebView2InitializationCompleted` und
> `BlazorWebViewInitialized` (der WinForms-`BlazorWebView` 10.0.100 führt kein `UnhandledException`) und zeigt nach
> 10 s einen markierbaren Text statt der leeren Fläche; `Parametersatzwache` in beiden Hüllen. Dauerhafte Wachen auf
> Linux: `ParametersatzTests` (liest die Hüllenquellen, Reflexion über `EPOS.UI`), `StartkachelDialogeTests`
> (21 Ziele + Assistent mit Hüllensatz, Gegenprobe), `StartseiteTests` (+2). **Ursache wahrscheinlich, nicht
> bewiesen** — die Abnahme am Gerät (Protokoll W16b § 12.3, A1–A7) entscheidet; bleibt eine Fläche leer, steht nach
> 10 s der Grund darin. **Befund W16b‑B‑2 (Reiter gesperrt):** die Sperre ist vorbildgetreu (`Form_Start_Load`), und
> Projektname im Kopfband und gesperrte Reiter schließen einander aus (beides hängt an `ProjektId()`); Erklärung ist
> die Farbe (**W16b‑B‑2b**: frei #5f5e5a gegen gesperrt #888780, das Vorbild zeichnete frei schwarz) — behoben mit
> W16b‑E‑5 (`1a72cd5`), Wache in `StartseiteTests`. Offene Frage ans Gerät: stand das Banner „Bitte zuerst ein
> Projekt auswählen!" über der Leiste? **Antwort des Anwenders: ja** — Befund W16b‑B‑2 damit geschlossen (kein
> Projekt offen, Sperre und Banner vorbildgetreu).
> **Anwenderwunsch W16b‑E‑6 vom 05.09.2026 („ja, oder anderen Hinweis geben der elegant ist"), umgesetzt
> (`2981c1a`):** Das dauerhafte Warnbanner der Reitersperre ist gefallen. An seine Stelle treten eine **leise
> Einstiegszeile** im Reiter „Projekt" (mit dem ⚠ des Kopfbands, verschwindet mit dem offenen Projekt — Name und ✔
> stehen darüber im Kopfband, wie bei der Gattungszeile W16b‑E‑4), der Grund als **Tooltip** am nun **weich**
> gesperrten Reiterknopf (`Reiterblatt.Sperrgrund` → `aria-disabled` statt `disabled`, weil ein `disabled`-Knopf
> keine Zeigerereignisse annimmt und keinen Tooltip zeigt; neues Ereignis `Reiter.Verweigert`, die Pfeiltasten
> überspringen beide Bauarten) und das bisherige Banner **flüchtig für drei Sekunden nach dem Versuch** — auf eine
> gesperrte Zunge wie über „Weiter ▶", den Weg der Tastatur; das ist `tabControl_Wizard_Selecting` samt der
> Lebensdauer von `Form_Hinweis`, nur ohne Wegklicken. Sperre und Farbgebung (W16b‑E‑5) bleiben. Zwei Texte
> `START_EINSTIEG` und `START_SPERRE_TIPP` in beiden Sprachen, ortsneutral („oben"/„unten") für die Schale ohne
> Kopfleiste. Wachen `ReiterTests` +2, `StartseiteTests` +5; Abnahmepunkte 1/1a–1c/3/14/16 im Protokoll. Der
> Windows-CI-Lauf 128 auf `e65d3a9` fiel an **einem** Test: der Suchhelfer `Stilblock` in `StartseiteTests` las
> das Stilblatt ohne Zeilenenden-Angleichung, und auf dem Windows-Läufer liegt es nach `text=auto` mit CRLF —
> ein zweizeiliger Selektor traf nicht mehr. Angeglichen an `StartseiteAnmutungTests`/`StilblattTests`
> (`\r\n` → `\n`), Gegenprobe mit CRLF-Stilblatt grün; Kern-Lauf 133 war grün.
>
> **Anwenderwunsch W16b‑E‑7 vom 05.09.2026 („Kacheln sollten ähnlich wie zuvor angeordnet sein – sind jetzt zu
> groß"; Bildschirmfoto: zwei Kacheln je Zeile, jede rund die halbe Fensterbreite), umgesetzt in `436dfbc`:** Ursache
> war die **Mindest**breite 404 px an einem Raster mit `1fr`-Spalten — bei 150 % Skalierung misst das Reiterblatt
> eines Full-HD-Schirms rund 1 200 CSS‑px, die drei Kacheln brauchen 1 228; also blieben zwei Spalten, und `1fr`
> verteilte die ganze Breite auf sie, während Sinnbild (84 px) und Titel (16 px) blieben. `Kachelraster` führt
> deshalb eine **`Hoechstbreite`** (0 = dehnend wie bisher): drei feste Spalten von höchstens `--epos-kachel-max`,
> linksbündig, `gap: 6px 8px` (die Fugen des Designers aus `Form_Start.resx`: x = 18/422/834, y = 134/325),
> dazu `grid-auto-rows: minmax(185px, auto)` als Zeilenhöhe des Vorläufers (Kachel 404 × 185); auf schmalem Schirm
> zwei Spalten (< 1 150 px) und eine (< 720 px). Alle fünf Reiter mit ihren 21 Kacheln setzen `Hoechstbreite="404"`,
> die Kennzahlreihen der Kosten- und der Wirtschaftlichkeitsseite dehnen unverändert; Farben, Schriften und die
> sieben `--epos-start-*`-Token aus W16b‑E‑5 sind unberührt. Wachen: `StartseiteTests.Jeder_Reiter_stellt_seine_
> Kacheln_im_Vorbildmass` (alle fünf Reiter) und drei Fälle in `StartseiteAnmutungTests` am Stilblatt; der Fall
> `Das_Kachelraster_nimmt_die_Kachelbreite_des_Vorlaeufers` aus W16b‑E‑3 entfällt, weil er genau die Mindestbreite
> festschrieb, die den Befund verursacht hat. Ein erster Agentenlauf zu diesem Wunsch wurde um 16:45 UTC durch eine
> Unterbrechung abgebrochen; sein Teilstand (`FestesMass`, `auto-fill`, geschrumpfte Seitenränder) ist geprüft und
> verworfen — er hätte beim Anwender weiterhin zwei Spalten ergeben.
>
> **Nachtrag zu W13‑B‑1 (`4fd8cc7`):** § 12 ist um die **Fehlerschranke** ergänzt — die dritte Wache neben
> `Parametersatzwache` (falscher Schlüssel vor dem ersten Zeichnen) und `WebViewWache` (WebView2 kommt nicht hoch):
> `Fehlerschranke.razor` + `Wurzel<T>`, gemountet von `BlazorDialogForm`, `BlazorSeite` und `EPOS.iOS/HauptSeite`;
> der Parametersatz geht unverändert durch, die Wachen prüfen weiter gegen `T`. Regel (c) ist erweitert: aus „kein
> `ShowDialog` aus einem Blazor-Ereignis" wird **„kein modales Systemfenster im WebView-Rückruf"** — mit zwei
> Werkzeugen, `Blazorsprung` ohne Rückgabewert und `Blazornachlauf` mit.

> **Statusblock iU9 — Teilwelle 16a umgesetzt (04.09.2026, Basis `975ead5` = Tag `vor-W16`, zusammengeführt mit `3c7e0d6` nach den W15c-Entscheiden)**
>
> **Der ganze Projektassistent bis auf seine Daten ist verschwunden — vier Masken, 1 694 Zeilen `.cs`, 988 Designer,
> 2 `MessageBox`, 26 Dateien:** `Wizard_Stromlastgang` (keine neue Komponente — die Assistentenseite 6 ist der
> `StromganglinieDialog` aus W12, W12‑O‑3), `Wizard_Komponenten` → `KomponentenauswahlDialog` (13 Kacheln über
> `Kachelraster`, die Rückfrage beim Abwählen einer belegten Komponente wortgleich mit `VorgabeNein`), `WizardParent` →
> Baustein **`Assistent`** (`Seiten` mit Titel/Inhalt/`Aktiv`, `NaechsteAktive(richtung)` statt `Next`/`Back`, „Weiter"
> wird auf der letzten aktiven Seite „Speichern") und Seite **`AssistentSeite`** (13 Seiten als `RenderFragment` in
> Bestandsreihenfolge, linkes Band nur in Betriebsart BEARBEITEN auf Schritt 0 mit der Razor-Projektliste aus W15a) hinter
> `AssistentHuelle` mit Gaben und Delegaten, `ProjektAuswahl` (uc) → der Baustein `ProjektListe` (die iZ5-Ausnahme aus
> W15a ist eingelöst). Dazu gelöscht: `WizardSeite`, `AssistentSeiten`, die zwei `IAssistent*Seite`, `IAssistentRahmen`
> und `BlazorAssistentSeite` (kein WinForms-Rahmen mehr). Im Kern: **`KomponentenBestandCtrl`** (unverändert verschoben,
> **Nachweis N6 zuerst**: `Bitmaske(id)` gegen den eingefrorenen `Form_Start.status`-Wert für **alle 13**
> Referenzprojekte — keine Abweichung, E‑3 damit erzwungen statt behauptet), **`AssistentCtrl`** (die sechs `Load*FromDB`,
> `SpeichernAusfuehren` beide Zweige mit der bitgleichen Reihenfolge der 21 Controlleraufrufe, Seitenschaltung) und
> `WizardCtrl` gleich mit (Befund W16a‑B2: seine einzige WinForms-Kante war ein totes Feld), `Kachel.Zustand`/`Aktiv`
> (B7), `IProjektQuelle.AssistentGaben(betriebsart, id)` mit Standardumsetzung. Sechs Sachcommits, Protokoll, Merge und
> Gate-Nachtrag (`d10b7b9` … `654bd66`), auf `ios_migration` als `81052cc`; `Views/Wizard` und `Views/Projekt` führen keine
> Designer-Maske mehr.
>
> **Acht Angleichungen** (A‑1…A‑8) und die Befunde W16a‑B1…B8, darunter: `WizardCtrl` in den Kern (B2), auch `LoadZGeb`
> ließ sein `RecordSet` offen (B3, behoben), K5 gegenstandslos (B5), **`AktionsKarte` fällt nicht mit W16a** — sechs
> Instanzen in `Form_Start`, sie geht mit W16b (B6). **Anwenderfragen:** E‑3 belegt (N6), **E‑4 halb** — die eine
> Meldung statt 17 stiller `return` ist da (`AssistentErgebnis` nennt den fehlgeschlagenen Schritt, der Assistent bleibt
> stehen), die **Transaktion nicht**: sie verlangte den Umbau aller 23 Schreibmethoden von `WizardCtrl` auf einen
> hereingereichten `DbVorgang`, was R‑W16‑6 ohne Windows-Feldvergleich untersagt — offen; E‑9 für den
> Kleinschreibungs-Zeugen umgesetzt (`Wizard_Komponenten` als Prüfmuster `Pruefmuster/Wizard/`); **neu W16a‑E‑1** (der
> Assistent bleibt unter Windows modal, Begründung wie R‑W10b‑1 — mit W16b/W16c könnte er eine freie Ansicht in derselben
> WebView werden: soll er? — **entschieden 04.09.2026: ja, in iU11 mit der Transaktion W16a‑O‑1**) und **W16a‑E‑2** (der NEU-Zweig schließt bei einem `Add_Projekt`-Fehlschlag nicht mehr
> kommentarlos, die Eingaben bleiben erhalten — bestätigen?). **Was W16b erbt:** der Rückweg der Hülle an
> `Program.startfrm.HinweisProjektGeoeffnet()` wird ein Rückruf an die Razor-Startseite, `IosProjektQuelle` setzt
> `AssistentGaben` noch nicht um, `Form_Start.UpdateWizardSymbole` ist ersatzlos zu löschen (N6 belegt die Gleichheit),
> `AktionsKarte` (3) und `Form_Hinweis` (3) fallen dort.
>
> **Nachweise** (auf dem gemergten Stand `81052cc`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 923** grün (3 833 nach W15c; N6 16 Fälle, N5 `AssistentTests` 28, R‑W16‑6
> `AssistentCtrlTests` 26 — Bearbeiten- und Neu-Lauf lassen Zählstand, Bitmaske, Anlagenbezeichner und Kopffelder gleich),
> **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün (ein Zeuge ins Prüfmuster umgezogen) · Stapellauf
> **7** Masken / 8 Designer (**Sollwert exakt getroffen**), **7 erreichbar / 0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer
> 1 234 Texte, 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich**
> (815 043 Werte) · beide Wächter leer.
>
> **Protokoll** mit den sechzehn geprüften Annahmen, dem Feldkartenabgleich, N5/N6, dem zweiten R‑W16‑6-Nachweis, acht
> Abweichungen, den Befunden, der Löschliste mit `git grep`-Nachweis und **elf Abnahmepunkten**:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W16a_Blazor_Port_Protokoll.md`. **Windows-Abnahme steht aus**,
> darunter der `projekt`-CSV-Vergleich eines neu angelegten und eines bearbeiteten Projekts Feld für Feld gegen den Stand
> `vor-W16` (R‑W16‑6, nur auf Windows). Der einundzwanzigste iOS-Lauf (33890882150) auf diesem Stand ist grün — erstmals
> mit dem Assistenten hinter `AssistentGaben` in der `AppWurzel` (N9; iOS setzt die Gaben noch nicht um, W16a‑O‑4).
> **Windows-Abnahme 05.09.2026 (PDF S. 6), zwei Befunde (`974c198`, Protokoll § 12):** **W16a‑B‑1** — die Weiche
> „Profil/Ganglinie" stand als eigener Kasten unter der Solarthermiekarte des Erzeugerreiters; sie sitzt jetzt in
> deren Rahmen, weil nur noch diese Kachel den Wirt bekommt und der den Kartenrahmen trägt (über das Markup ginge es
> nicht — eine Kachel ist ein `<button>`, der keine Optionsfelder enthalten darf), Klickziel und Tastaturweg
> unverändert. **W16a‑B‑2** — der Parametersatz einer Assistentenseite wird beim Betreten geholt und nicht mehr bei
> jedem Neuzeichnen (Herleitung im W9-Protokoll § 12.1, Befund W9‑B‑1).
>
> **Zum Anwenderwunsch W15a‑E‑1 vom 05.09.2026 (`325a275`), gemeldet an dieser Maske (Assistent, Seite 0, linkes
> Band):** Der Projektname bricht jetzt um, statt abgeschnitten zu werden; Varianten stehen eingerückt unter ihrem
> Stamm und tragen darunter leise „Variante von ‹Stamm›" — eine Artspalte hat in 280 px keinen Platz. `AssistentSeite`
> reicht dafür zwei Texte durch, `AssistentHuelle` füllt sie aus `PRJ_LIST_ART_VARIANTE`/`PRJ_LIST_VARIANTE_VON`.
> Herleitung im W15a-Protokoll § 14, hier § 13, Abnahmepunkt A‑W16a‑E‑1.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `ProjektKopfSeite`: die sechs Felder zweispaltig im Raster
> (`epos-projektkopf-raster` gefallen), Pflichtsternchen, Legende und der Hinweis bei leerem/vergebenem Namen aus der
> Rechner-2-Linie (Merge 5) bleiben zwischen Feldern und Beschreibung — deshalb steht die Beschreibung als breites Feld
> unter dem Raster. Paket P3 gesamt: 14 Dateien, 168 Felder, 43 Raster, kein `Formulargruppe` nötig (jede Gruppe des
> Vorbilds ist schon ein `Gruppenkopf`), 15 `epos-feldpaar` und drei Zweispalter samt Stilblattregeln gefallen, eine
> Zeile CSS neu (Herleitungszeile spannt über alle Spalten); UI 2 595 (+14), Formularkarte 122.

> **Statusblock iU9 — Welle 15c umgesetzt (04.09.2026, Basis `f71853b` nach W15b, zusammengeführt mit `5a73fd6` nach den W15b-Entscheiden)**
>
> **Drei Masken — 1 588 Zeilen `.cs`, 246 Designer, 119 `.resx`, 16 `MessageBox` und drei Meldungen im Startweg — sind
> drei Razor-Komponenten, sechs Kern- und Hüllenvorarbeiten und vier Hüllenwege:** `LizenzVerwaltungDialog`
> (`EPOS.UI/Dialoge/Lizenz/`, Zustand als Zeichenkette, Aktivieren/Lesen/Trial/Freigeben/Auffrischen als Delegaten, das
> Schlüsselfeld nach Erfolg leer — S4; **auch als Überlagerung im Lizenzdialog**, A‑4), `ErststartDialog` (unbestimmter
> `Fortschritt` ohne Abbruch, Protokoll als `Textfeld`, **besitzerlose Hülle** mit den vier neuen Zusätzen an
> `BlazorDialogForm<T>` — `ImTaskbar`, `AufBildschirmMittig`, `SchliessenGesperrt`, `Mindestmass` — und dem Rückkanal
> `LaufAktiv`; für die 40 bestehenden Aufrufer ändert sich nichts) und `LizenzDialog` (drei `Reiterblatt`
> Lizenzvereinbarung / Rechtliche Hinweise / Komponenten, Zustimmungsmodus als Parameter, „Drucken" über den Browserdruck,
> „Speichern unter…" als Text, Verweise nur aus dem eigenen Ressourcentext und nie als `MarkupString`) hinter zwei
> Hüllenwegen (Menü Hilfe und der besitzerlose `ZustimmungSicherstellen`-Weg aus `Program.cs`). **Kein neuer Baustein**
> (B12). Im Kern: **`LizenzManager.Bewerten`** — die reine Zustandsrechnung aus `Pruefe()` herausgezogen, `Pruefe()` bleibt
> Fassade (E‑10, Verhalten unverändert) —, `LizenzCtrl` mit `LizenzGaben`, `EmailGueltig` und `LicDateiLesen` (liest nur,
> prüft nicht — S3), `LizenzTextCtrl` (die Online-Quelle ist **eine Zeile**, heute bitgleich die AGB-Seite — E‑17),
> `ZustimmungCtrl` (`catch → true` wortgleich, E‑15), `StatusText`/`TypText` über neun `MyResource`-Schlüssel;
> `ErststartCtrl` bleibt in der Windows-Anwendung, weil `ErststartMigration` OleDb mitbringt (Wächter). **Der
> Wellennachweis ist eine Erstanlage:** bis hierher prüfte kein einziger Test den 659-Zeilen-Lizenzkern (B1) — jetzt
> **+79 Kern-Fälle** (`LizenzZustandTests` 19: Ränder Kulanz und Karenz ±1 Tag, Uhrtoleranz, Laufzeit sticht Leine,
> Schreibrecht je Zustand — **kein Fall fasst die Ablage an**; `LizenzTokenTests` 10 mit einem im Test erzeugten
> Schlüsselpaar, **kein Server-Token im Repository**; `LizenzTexteTests` 12; `LizenzCtrlTests` 21; `LizenzTextCtrlTests` 17)
> und **+67 bunit-Fälle** (28 / 26 / 13), alle vor der ersten Maske. **E‑8, Weg 2:** `Program.Main` prüft nach der
> Sprachwahl und vor dem ersten besitzerlosen Dialog die WebView2-Laufzeit; fehlt sie, erscheint eine native `MessageBox`
> mit der Bezugsquelle (zweisprachig, Wortlaut nach dem Setup) und das Programm endet — keine WinForms-Rückfallmasken;
> die Zusage in `Umsetzung_iU8_Nachweise.md` („startet, nur der Dialog bleibt leer") ist berichtigt. **E‑7:** die
> 27 Rechtstexte stehen **deutsch in beiden Sprachzweigen** mit dem Zusatz „Binding version in German." (A‑9);
> **maschinell umgezogen und zurückverglichen: 26 von 27 zeichengleich, einer berichtigt** (O‑1 — .NET 10, SQLite und
> WebView2 statt .NET 8 und ACE). Zwölf Sachcommits, Protokoll, Merge und Gate-Nachtrag (`bb805d3` … `7cb03d1`), auf
> `ios_migration` als `2369f52`; 63 Texte des Lizenzdialogs zweisprachig.
>
> **Neun Angleichungen** (A‑1…A‑9): Suchleiste und A+/A− entfallen (die WebView zoomt selbst), keine RTF-Anzeige
> (`.rtf`/`.docx` zeigen denselben Hinweistext), Browserdruck statt `PrintDocument` (der einzige Nutzer des Bestands
> fällt), Verwaltung als Überlagerung, keine geratenen Verweise, Speichern als Text, Online-Fassung wird abgewartet statt
> aus `async void` geschrieben, `Mindestmass` statt einer verdeckten `MinimumSize`, der englische Zusatz einmal statt
> 27-mal. **Befunde** B1…B28 eingetreten — außer B26 (der Typzeuge stand seit W14a/W14c schon auf „Bestand ODER
> Prüfmuster", `GroupBox` liegt im eingefrorenen Muster); neu **B29** (E‑6 gegenstandslos: der „ja"-Zeuge liegt seit
> W14c auf `MDIMainForm`, der Wurzel) und **B30** (der Erststart überschreibt seinen Zustandstext mit der Schlussmeldung —
> bitgleich, mit Zeugen). **Anwenderfragen:** E‑8 Weg 2 (oben), E‑7 (oben — eine andere Entscheidung kostet 27 Werte im
> englischen Zweig), **E‑9 → iF30** (Register: Lesemodus-Durchsetzung nach W16 — `DarfSchreiben()` hat genau einen Leser),
> E‑2 (Druckknopf bleibt), E‑4 (kein iOS-Einstieg in die Lizenzverwaltung, iU11), E‑12 (Suchleiste entfallen), E‑17
> (Vertragsendpunkt `epos/v1/vertrag` später — eine Zeile). **Entschieden am 04.09.2026 (Empfehlungen angenommen):**
> W15c‑O‑1 — der Vertragsendpunkt löst die AGB-Seite ab, sobald der Lizenzserver 1.4.0 im Betrieb ist (eine Zeile und ihr
> Zeuge); W15c‑O‑2 — das `LizenzTexte`-Bündel für die zwei großen Komponenten **ist umgesetzt** (04.09.2026, `2281ece`:
> gemessen 18 bzw. 29 Einzelparameter, nicht 25/20, werden **einer**; `LizenzDialog` 449 → 349 und `LizenzVerwaltungDialog`
> 451 → 347 Zeilen; `LizenzTexte` füllt sich selbst aus `MyResource` in de und en, ein leerer Katalogeintrag bleibt leer (E‑7);
> die 54 Dialogfälle bleiben, ein neuer Fall prüft die Selbstfüllung aller Texte; Regel in `EPOS.UI/CLAUDE.md`: ab etwa
> zehn Anzeigetexten ein Bündel, `*Texte` = Beschriftungen, `*Gaben` = Zustand). **Offen:** W15c‑O‑3
> (Textsuche im Vertragstext), W15c‑O‑4 (Lizenzeinstieg auf iOS, iU11), W15c‑O‑5 (`Form_HelpPopup` fällt weder mit W15c
> noch mit W16).
>
> **Nachweise** (auf dem gemergten Stand `2369f52`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 833** grün (3 687 nach den W15b-Entscheiden), **identisch unter
> `LC_ALL=en_US.UTF-8`** · Formularkarte **124** grün · Stapellauf **11** Masken (12 − 1; zwei der drei Masken waren
> Code-Formen ohne Designer), 14 Designer, **11 erreichbar / 0 nein / 0 verwaist / 0 unklar**, Lokalisierungszähler
> unverändert 7 · SQL-Prüfer 1 235 Texte, 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf
> 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · beide Wächter leer, S1/S2-`git diff` leer.
>
> **Protokoll** mit Feldkartenabgleich (zwei Karten von Hand), den 146 neuen Fällen, neun Abweichungen, den Befunden
> B1…B30, der Sicherheit S1…S4, den vier Hüllenzusätzen und **22 Abnahmepunkten**:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W15c_Blazor_Port_Protokoll.md`. **Windows-Abnahme steht aus**, und
> zwei Punkte sind nur dort führbar: **der Erststart auf einem echten `.accdb`-Bestand** mit Fehlschlag-Variante
> (Rückfallstand `git show 3ae6847:WindowsFormsApplication1/Views/Admin/Form_Erststart.cs`) und **die Windows-Sandbox
> ohne WebView2** (Meldung und Programmende statt leerer Dialoge); dazu Aktivieren/Lesen/Trial/Freigeben, die Zustimmung
> beim Erststart, Drucken, Speichern unter, de/en, 125 %. Der zwanzigste iOS-Lauf (33883210632) auf diesem Stand ist grün.
>
> **Rückweg-Anker für W16 (R‑W16‑12): `975ead5`** — der Statusblock-Commit dieser Welle, der letzte Stand vor W16a. Der
> Git-Tag `vor-W16` ließ sich aus der CI-Umgebung nicht pushen (der Push-Zugang der Umgebung erlaubt nur den Zweig
> `ios_migration`, Tag-Refs werden mit HTTP 403 abgewiesen); **der Anwender hat ihn am 04.09.2026 vom Arbeitsplatz aus
> gesetzt** — `refs/tags/vor-W16` zeigt auf `975ead5`.
>
> **Befund W15c‑B‑1 und Anwenderwunsch W15c‑E‑1 vom 05.09.2026 (Bildschirmfoto Hilfe → Lizenz: „Die Darstellung
> kann verbessert werden. Wozu gibt es ‚Datei wählen'? löschen"), umgesetzt in `ee7214f`:** **W15c‑B‑1** — Jede
> Zeile begann links vom sichtbaren Rand. Ursache war die Wurzelregel selbst: Die drei Wurzeln der Welle 15c
> (`.epos-lizenz`, `.epos-lizverw`, `.epos-erststart`) hängen als einzige nicht unter `.epos-dialog` und trugen
> deshalb weder Seitenrand noch `overflow-x` — das erste Zeichen stand bei x = 0 an der Kante der WebView, und bei
> 125–150 % schnitt sie es weg. Rand und Waagerecht-Sperre stehen jetzt an einer Stelle für alle drei
> (`overflow-x: clip`, nicht `hidden`). **W15c‑E‑1** — Der Vertragstext stand in einem `<textarea>` mit
> Größenanfasser; er ist jetzt eine Leseansicht: Kopf mit dem `InfoKnopf` (er stand hinter „Schließen"), je Karte
> derselbe gerollte Lesebereich mit Absätzen und den „§ …"-Zeilen als Überschriften, leise Fußzeile, rechts die
> Knopfleiste. Am Wortlaut ändert sich nichts, neu ist allein `LizenzDialog.Vertragsabschnitte`. **„Datei wählen…"
> ist gefallen:** `LizenzHuelle.DateiWaehlen` war der Rest der RichTextBox des Vorläufers (Filter `*.rtf;*.docx;*.pdf`)
> und ersetzte den lesbaren Vertragstext durch den Zeiger auf eine Datei, die die WebView seit E‑1 nicht anzeigt;
> der Weg zur `.lic` bleibt allein „Lizenz aktivieren…", eine Vertragsdatei findet `LizenzTextCtrl.DateiSuchen`
> weiterhin selbst. 15 neue bunit-Fälle (UI 2 497). Offen als **W15c‑O‑6**: Die vier KI-Wurzeln der Welle 15b haben
> dieselbe Form (kein Seitenrand, kein `overflow-x`) — sie gehören in die laufende Überarbeitung des
> Hilfe-Assistenten (W15b‑E‑3).

> **Statusblock iU9 — Welle 15b umgesetzt (04.09.2026, Basis `c11f13d` nach W15a, zusammengeführt mit `08cbc2a` nach den W15a-Entscheiden)**
>
> **Vier Masken — 2 243 Zeilen `.cs`, 191 Designer, die eine `MessageBox` der Welle — sind fünf Razor-Komponenten,
> zwei Bausteine, ein Nachtrag und zwei Hüllen:** `TextAnzeige` (`EPOS.UI/Dialoge/Hilfe/`, Überlagerung), `KiHinweisDialog`
> mit `KiHinweisHuelle` (die Einwilligung aus `Program.cs` läuft jetzt **asynchron** über `KiEinwilligung.Nachfragen`, alle
> drei Aufrufer in einem Schritt), `KiEinstellungenDialog` (Schlüssel nur als Vorbelegung, `type="password"`, nie
> durchgereicht — S‑1/S‑2) und **`KiChatDialog` in vier Kindern** (Rahmen, `KiBestaetigungBlock` als Fußbereich des Verlaufs,
> `KiWerkzeugliste` mit der Kulturregel, `KiEingabezeile` mit Enter/Shift+Enter; keine `.razor` über 400 Zeilen), dahinter
> `KiChatHuelle` **nicht-modal mit Besitzer** (E‑6, holt ein offenes Fenster nach vorn). Neu: der Baustein
> **`Gespraechsverlauf`** (Bausteinlücke 17 — zehn Rollen, kein Streaming, kein Markdown, keine Link-Erkennung, Autoscroll nur
> unten, nichts in `localStorage`; 29 Fälle), der Baustein **`KiKnopf`** (der KI-Einstieg aus einer Maske, über
> `Seitenschluessel.KiAssistent` ohne `Masken.*`-Zwilling — E‑10) und `Warnbanner.Verfaellt`. Im Kern: **`KiChatService`
> (1 751 Z.) per `git mv`** — Befund B31: kein reines Verschieben, der Dienst ruft zehnmal `KiAusfuehrer`, deshalb die Naht
> `IKiAusfuehrung`/`KiAusfuehrungsweg` mit stiller Standardfassung (Bauart `Dienste.*`, **der Einwilligungsriegel bleibt
> davor** — S‑4), `Kurzbeschreibung.Umbrechen`, `KiAusfuehrer.AufOberflaeche` statt `Control`-Anker (E‑8), `KiChatKontext`
> (Positivliste der 24 Bereiche, Ermittlung in der Hülle — E‑9), `KiVerlaufstexte` (zwei getrennte Listen Anzeige/Prompt).
> **Zwei Masken bleiben bewusst:** `Form_HelpPopup` (E‑2, ihr Ersatz `IHilfeDienst` steht auf beiden Plattformen; fällt
> mit `HelpCatalog` in iU11) und `Form_Hinweis` (E‑1b, drei Aufrufer in `Form_Start`; fällt mit W16 — der Nachfolger
> `Warnbanner.Verfaellt` ist gebaut und geprüft). 16 Sachcommits, ein Merge und ein Gate-Nachtrag (`ab25d75` … `34047de`),
> auf `ios_migration` als `fa9d17f`; 21 Textschlüssel de/en (419 `KI_*` beidseitig), acht CSS-Variablen.
>
> **Die neun Zeugen entstanden vor den Masken** (T‑1…T‑9, 129 Fälle; **kein Netz in einem einzigen Fall**, Modellaufrufe
> nur über den Prüfkanal `Modellkanal`): Einwilligungsriegel P‑1 (ohne `Nachfragen` kein Modellaufruf, Abschalter,
> Fassung 1 < 2), Werkzeugrunde und die vier Ausgänge der Bestätigung P‑2/P‑3. **Zehn Angleichungen** (A‑1…A‑10): keine
> Maßparameter, Bestätigungsblock unten im Verlauf, keine Positionsrechnung, kein `DetectUrls`, Autoscroll nur unten, die
> eine MessageBox wird ein Warnbanner, die 400‑ms-Sperruhr und der Flackerschutz entfallen. **Befunde** B1…B30 eingetreten,
> vier neu (B31 Naht, B32 20 statt 17 Texte, B33, B34 `Schalter` hält seinen Zustand selbst — `@key` im Chat).
> **Anwenderfragen:** E‑1b und E‑6 wie vorläufig entschieden, E‑8/E‑9/E‑10 umgesetzt; **entschieden am 04.09.2026
> (Empfehlungen angenommen, `13835f2`/`aaaacce`, gemerged als `4775213`):** W15b‑O‑1 — der Schnitt der Naht
> `IKiAusfuehrung` ist bestätigt; die iOS-Hülle nutzt denselben Kern und läuft bis zu ihrer eigenen Fassung (O‑4) auf der
> stillen Standardfassung `KeineAusfuehrung` (fragen und suchen ja, ausführen nein); W15b‑O‑2 — der Tooltip der
> Semantikzeile ist als `title` zurück (wortgleich der alte Schlüssel `KI_SEMANTIK_HERKUNFT`, zwei Zeugen); **offen:** W15b‑O‑3 (`Standards/Schalter`-Rücksetzer, 20 Nutzer, eigene Welle), W15b‑O‑4
> (iOS bedient `KiAssistentGaben` noch nicht, Handprobe in iU11), W15b‑O‑5 (`Form_HelpPopup` ist die einzige Maske, die
> weder mit W15c noch mit W16 fällt).
>
> **Nachweise** (auf dem gemergten Stand `fa9d17f`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 685** grün (3 524 nach den W15a-Entscheiden), **identisch unter
> `LC_ALL=en_US.UTF-8`** · Formularkarte **124** grün · Stapellauf **12** Masken (13 − 1; drei der vier Masken waren
> Code-Formen ohne Designer), 15 Designer, **12 erreichbar / 0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer 1 235 Texte,
> 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) ·
> beide Wächter leer.
>
> **Protokoll** mit Feldkartenabgleich (drei Karten von Hand), den neun Zeugen, zehn Abweichungen, den Befunden B1…B34,
> der Sicherheit S‑1…S‑4 und 17 Abnahmepunkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W15b_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Menü und F1, zweites Öffnen holt nach vorn, eine Frage mit Modell einmal echt, Nur-suchen
> ohne Modell, Werkzeugliste mit Kulturregel, die vier Ausgänge der Bestätigung, Rechtshinweis aus dem Chat und beim
> Erststart, „Modell neu erkennen" über Abbrechen hinweg, Kopieren, de/en, Esc je Ebene; **bekannter Schönheitsfehler
> (Punkt 16):** die DPI-Insel greift nur im modalen Lauf, der nicht-modale Chat ist ab 125 % bitmapskaliert (iF21, W16c).
> Der achtzehnte iOS-Lauf (33876284942) auf diesem Stand war **rot** — CS0103 im iOS-Hilfedienst aus W15b.0g, den nur der
> macOS-Läufer übersetzt; behoben in `f0e23a4`, der neunzehnte Lauf (33878903371) darauf ist grün.
>
> **Windows-Abnahme 05.09.2026 am Hilfe-Assistenten — Befunde W15b‑B‑1, W15b‑B‑2 und Wünsche W15b‑E‑3, W15b‑E‑4
> (Bildschirmfotos „Hilfe-Assistent", „Aktionen von Hand ausführen", HTTP 401), umgesetzt in `28082e8`:** **W15b‑B‑1** —
> „Einstellungen…" öffnete ein leeres Fenster, dann stürzte die Anwendung ab: `KiChatHuelle.Gaben.cs:208` gab
> `Task.FromResult(KiEinstellungenHuelle.Oeffnen(_fenster))` heraus, ein zweites modales `BlazorDialogForm` mit
> einer zweiten WebView2 synchron im `WebMessageReceived`-Rückruf der ersten — dieselbe Lage wie W16b‑B‑1 und
> W13‑B‑1 (Risiko R2); dieselbe Zeile für den Rechtshinweis, dritte Fundstelle `KiHinweisHuelle.Einhaengen`. Die
> Einstellungen und der Hinweis erscheinen seither als **Überlagerung** derselben WebView (Entscheid E‑5; beide Hüllen
> hatten ihren Parametersatz seit W15b.3/.4 dafür getrennt), die Delegatenwege laufen über `Blazornachlauf`; drei
> gleichartige Fundstellen in Bericht (`BerichtSeiteGaben.cs`), Einstellungen (`EinstellungenHuelle.cs`) und
> Ergebnisseite (`SimulationErgebnisHuelle.Wege.cs`) sind mitbehoben, und die neue Wache `HuellenwegTests` hält
> alle 63 Hüllendateien darauf, dass kein modales Systemfenster synchron aus einem Blazor-Ereignis kommt.
> **W15b‑B‑2** — die Frage endete mit dem rohen Anbietertext „HTTP 401 … Expected OAuth 2 access token": Neu
> `KiKern/KiDienstfehler.cs` mit `KiDienstAusnahme` — Anwendersatz in den Verlauf, Rohtext ins Protokoll
> („Protokoll anzeigen"); `SendenAsync` weist eine Anfrage **ohne** Schlüssel ab, bevor sie hinausgeht, und wertet den
> Rückgabewert von `TryAddWithoutValidation` aus. Der **doppelte Block** im Verlauf kam aus der Eingabezeile:
> `@onkeydown:preventDefault` wird beim Zeichnen ausgewertet, der Browser trug den Zeilenumbruch nach, `oninput`
> schrieb die Frage ins geleerte Feld zurück. Die Schlüsselablage selbst (`%APPDATA%\wp-plan\ki-schluessel.dat`,
> DPAPI) ist unverändert; steht künftig „Kein API-Schlüssel hinterlegt", ist der Speicher leer, bei „(401)" nimmt
> der Dienst einen vorhandenen Schlüssel nicht an. **W15b‑E‑3** — `.epos-kichat` hing an einer offenen Höhenkette,
> der Gesprächsverlauf war nicht zu sehen; er füllt jetzt die Höhe, die Begrüßung steht mit einem Satz und einer
> Klappe im Kopf, der Zähler genau einmal, die drei Knöpfe in einer Reihe (`KiChatAnmutungTests`). **W15b‑E‑4** —
> `KiAktion` führt Titel und Beispiel (zweisprachig, alle 24 Aktionen); die Werkzeugliste zeigt Klartext statt
> Bezeichner, gruppiert nach lesend/ändernd, mit Suchfeld, Kennzeichen, Beispiel und beschrifteten Pflichtfeldern,
> der Andockpunkt ist nur noch Kurztext, und das eingebaute Hilfewissen trägt den Abschnitt „Aktionen des
> Assistenten". UI 2 571 (+25), Kern 1 165 (+76), KiKern 469 (+19). Protokoll W15b, 21 Abnahmepunkte.

> **Statusblock iU9 — Welle 15a umgesetzt (04.09.2026, Basis `f7e2758` nach W14c, zusammengeführt mit `8651b0d` nach den W14c-Entscheiden)**
>
> **Sechs Bauteile — 1 254 Zeilen `.cs`, 683 Designer, fünf Formen und ein UserControl — sind ein Baustein, drei Dialoge,
> eine Assistentenseite und vier Hüllen:** der Baustein **`ProjektListe`** (`EPOS.UI/Bausteine/`; der Bestand führte **vier
> Projektlisten nebeneinander**, die fünfte lag fertig als iOS-Seite — „Eine Projektauswahl für alle" aus
> `Konzept_Projektdialoge_Vereinheitlichung.md` ist eingelöst, `Seiten/Projektliste` baut darauf und ihre fünf Tests sind
> unverändert grün), `ProjektWahlDialog` (`EPOS.UI/Dialoge/Projekt/`, Zweck Öffnen oder Löschen — zwei Masken in einer
> Komponente), `ProjektKopieDialog` („Speichern unter", Duplizierlauf mit Fortschritt und **Abbruch über
> `CancellationToken` mit Rollback**), `ProjektTransferDialog` (Export/Import, erstmals englisch) und `ProjektKopfSeite`
> (`EPOS.UI/Seiten/Assistent/`, die erste Assistentenseite als Razor über `BlazorAssistentSeite`, Weg (a) ohne Umbau am
> Rahmen). **`ProjektAuswahl` (uc) bleibt bis W16** — bewusste iZ5-Ausnahme, weil `WizardParent` es hostet; nur die Hüllform
> fällt. Im Kern: **`ProjektExportImportCtrl` (1 278 Z.) per `git mv`** — die einzige Kante war `SchemaMigration.ZIEL_VERSION`,
> jetzt `SchemaStand.Zielversion` (62) —, `ProjektAngaben`, `ProjektCtrl.IdVonName`/`NamenListe`/`LoeschenMitVorarbeiten`/
> `Kopf`, `ProjektDuplizierenCtrl.PruefeNamen`/`VerwaltungsfelderSetzen`, `KlimaregionStammCtrl.IdVonName`/
> `NameZuProjektregion`, `IProjektQuelle.TransferDaten()` mit Standardumsetzung; 83 Textschlüssel de/en. **Die Proben
> entstanden zuerst — und fanden Befund B55: der Projektimport war seit der SQLite-Umstellung kaputt** (benannte
> Platzhalter `@id`/`@k0`/`@c0` im SQL-Text, die Zugriffsschicht bindet nach Position; jeder Import brach mit „Must add values
> for the following parameters"). Vier Stellen auf `?`, P1–P5 danach grün, vor und nach dem Umzug. Elf Sachcommits, ein Merge
> und ein Gate-Nachtrag (`7d8c93a` … `b612775`), auf `ios_migration` als `e759eaf`.
>
> **Zwölf Angleichungen** (A‑1…A‑12): kein Emoji auf „Abbrechen", Duplizieren abbrechbar, Fenster wächst nicht, **die
> Dublettenprüfung wird richtig** (`PruefeNamen` statt Präfixsuche — „Muster" neben „Musterprojekt" wird angenommen),
> Doppelklick markiert nur, Löschdialog mit Esc, Transferdialog übersetzt, Datum folgt der Programmsprache, Sicherung und
> Importbericht über Delegaten (Windows-Vorgabe unverändert), eine Projektliste mit Suche auch in „Löschen"; **A‑12 gilt
> nicht für „Export"** — der Transferdialog behält sein Auswahlfeld (Platz unter der Variantenliste; W15a‑O‑2). **B56 widerlegt
> B25:** „Projekt → Öffnen…" ist im MDI-Menü vorhanden und verdrahtet, E‑6 ist gegenstandslos. **Anwenderfragen
> entschieden:** E‑1 nein, E‑2 ja, E‑3 nur markieren, E‑4 Programmsprache, E‑5 „Löschen" ja / „Export" nein. **Offen:**
> W15a‑O‑1 (P6, Referenzlauf auf ein importiertes Projekt, nicht gelaufen — Ersatz Abnahmepunkt 4). **Entschieden am
> 04.09.2026:** W15a‑O‑2 (Empfehlung angenommen — der Transferdialog behält sein Auswahlfeld, keine volle Projektliste im
> Export) und W15a‑O‑3 („Projektname darf nicht gleich sein, daher löschen. Rückfragen in diesem Fall": Namen sind über den
> eindeutigen Index `Projektname` eindeutig, das Löschen über den Namen bleibt; trifft ein Name mehrere Projekte, fragt das
> Programm mit Vorgabe „Nein" nach statt still beide zu löschen — umgesetzt in `ba806b7`, gemerged als `fe07e82`:
> `LoeschStand.Mehrdeutig` mit Anzahl in `ProjektCtrl.LoeschenMitVorarbeiten`, zweite `Rueckfrage` im `ProjektWahlDialog`,
> sieben Tests, darunter zwei auf einer Arbeitskopie ohne den Index). **W15a‑O‑4 (entschieden 04.09.2026, Empfehlung
> angenommen):** `VariantenCtrl.LoescheVariante` rief `ProjektCtrl.Delete(name)` direkt — jetzt dieselbe Vorprüfung
> (`LoeschBefund` statt `bool`, `Mehrdeutig` mit Anzahl) und dieselbe zweite Rückfrage in der `UebersichtSeite`, sechs
> Tests; umgesetzt in `5104ea3`, gemerged als `1c49f38`.
> **Testanker:** der Maskenschlüssel-Zeuge steht jetzt auf `FormMain`/`Masken.ProjektDetail`, zwei W16-Aufträge (T1, T2)
> stehen in den Tests und im Protokoll.
>
> **Nachweise** (auf dem gemergten Stand `e759eaf`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 511** grün (3 436 nach den W14c-Entscheiden; P1–P5, P7–P9, 14 Fälle `ProjektListe`,
> 45 Fälle der drei Dialoge und der Seite), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **124** grün ·
> Stapellauf **13** Masken (17 − 4; `Form_ProjektExportImport` war eine Code-Form ohne Designer), 14 Designer,
> **13 erreichbar / 0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer 1 234 Texte, 0 Fundstellen · ChartProben 32 Bilder,
> 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · beide Wächter leer.
>
> **Protokoll** mit Feldkartenabgleich je Maske (Transfermaske von Hand), den Proben, zwölf Abweichungen, den Befunden
> B1…B56 und der Windows-Abnahme: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W15a_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Projektwechsel über alle vier Wege mit offenen Blazor-Seiten, Löschen mit Kaskade,
> Speichern unter (Fortschritt, Abbrechen, Dublette „Muster" neben „Musterprojekt"), **Export→Import-Rundreise mit
> Variantenpaket und Sicherung samt Kennzahlenvergleich** (der Import war bis hierher unbenutzbar), Assistent vor und
> zurück, de/en, 125 %. Der siebzehnte iOS-Lauf (33867643966) auf diesem Stand ist grün.
> **Windows-Abnahme 05.09.2026 (PDF S. 4), Befund W15a‑B‑1 (`974c198`, Protokoll § 13):** in „Speichern unter"
> lag die Spalte „Geändert" hinter dem waagerechten Rollbalken — der Umbruch der zwei Spalten kam erst bei 780 px,
> und die Hausregel `white-space: nowrap` trieb die Tabelle über die Spaltenbreite. Der Umbruch kommt jetzt bei
> 1 100 px (Liste über die volle Breite, Formular darunter), Name und Kunde brechen um, das Datum bleibt einzeilig
> mit fester Breite und kulturabhängigem Kurzformat. `ProjektWahlDialog` war nicht betroffen, `ProjektTransferDialog`
> führt keine Projektliste (W15a‑O‑2).
>
> **Anwenderwunsch W15a‑E‑1 vom 05.09.2026 (zwei Bildschirmfotos: „Projekt öffnen: Es sollte wie zuvor kenntlich
> sein, welches Variantenprojekte sind"), umgesetzt in `325a275`:** Als eigene Spalte gab es die Variante im Vorbild
> nie — weder `ProjektAuswahl` (418 Z.) noch `Form_ProjektAuswahl` (99 Z.) führten das Wort; kenntlich war sie **am
> Namen** (`VariantenCtrl.AnlegenAusStamm` bildet „‹Stamm› - ‹Bezeichner›", `Form_Start.FuelleVariantenCombo` zeigte
> genau das, die Ordnung kam aus `VariantenCtrl.LadeGruppe`: Stamm zuerst, dann Varianten `ORDER BY Variantenname`).
> Das trug nicht mehr, weil das Assistentenband 280 px breit ist und ausgerechnet der Teil abgeschnitten wurde, der
> die Variante ausmacht. Jetzt trägt `ProjektKopfZeile` Stamm-Id, Bezeichner und Stammnamen (`IstVariante`) aus
> **einer** Abfrage mit zwei LEFT JOINs in `ProjektCtrl.NamenListe` (fehlt `Tab_Variante`, läuft die alte Abfrage —
> ein LEFT JOIN auf eine fehlende Tabelle hätte die ganze Liste geleert); der Baustein `ProjektListe` gruppiert
> Stamm → Varianten nach Bezeichner wie `LadeGruppe` (auch unter Datumssortierung, Stamm-Ausfall und Ringketten
> abgesichert), zeigt die Spalte „Art" **nur, wenn die Liste eine Variante führt**, sonst die leise Zeile „Variante
> von …" mit Einrückung, und die Suche greift über den Bezeichner. Aufrufer: `ProjektWahlDialog`, `ProjektKopieDialog`,
> `AssistentSeite` und die drei Hüllen, vier neue Textschlüssel de/en; `Startseite.Varianten` war schon gekennzeichnet
> und bleibt, `ProjektTransferDialog` führt keine `ProjektListe`. Dabei ist der Befund **W15a‑B‑1** erst wirklich
> behoben: Die Umbruchregel stand im Blatt und **wirkte nicht** — `.epos-raster td` (0,1,1) schlug
> `.epos-projektliste-name` (0,1,0); sie trägt jetzt den Tabellenselektor davor. Elf neue bunit-Fälle, ein Kern-Fall
> gegen `Tab_Variante`; Protokoll § 14, Abnahmepunkt A‑W15a‑E‑1.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `ProjektTransferDialog` (drei Raster, einspaltig) und `ProjektKopieDialog` (die vier Felder
> rechts neben der Liste, Beschriftung daneben).

> **Statusblock iU9 — Welle 14c umgesetzt (04.09.2026, Basis `4e77221` nach W14a/W14b, zusammengeführt mit `809fe41`)**
>
> **Fünf Masken — 2 198 Zeilen `.cs`, 1 425 Designer, 26 `MessageBox` + 2 indirekte — sind fünf Razor-Komponenten in
> vier Fenstern:** `GesetzeskatalogDialog` mit `GesetzeskatalogZeileDialog` als Überlagerung (`EPOS.UI/Dialoge/Admin/`;
> aus dem Kostendialog und dem Wirtschaftlichkeits-Parameterdialog erscheint der Katalog jetzt IM Dialog, E‑1),
> `KatalogDublettenDialog` über dem neuen Baustein **`Baumansicht`** (der einzige `TreeView` des Bestands; 14 bunit-Fälle:
> Rollen und Ebenen, Dreieck klappt ohne zu wählen, ein `tabindex`, die vier Pfeiltasten, Auswahl überlebt den Neuaufbau),
> `EinstellungenDialog` (`EinstellungenCtrl` im Kern über `Dienste.Pfade`/`Dienste.Einstellungen`, vier Rubriken als
> `Reiter`) und `KlimadatenDialog` (seit E‑3 wieder so benannt; `KlimaregionStammCtrl` in den Kern gezogen, `KlimaImportAblauf` mit Abbruch, die zwei
> Klimabilder im Renderer → **32 Proben**). **Vier der fünf Fachteile lagen schon im Kern** (`GesetzKatalog`,
> `DublettenPruefung`/`KatalogBereinigung`/`KatalogRegistry`, `SolarPVGISCalculator`) — die Vorarbeit war Zuschnitt, kein
> neuer Rechenweg; der Nachweis (`KatalogpflegeTests`, 104 Fälle über eine Arbeitskopie) entstand vor der ersten Maske,
> weil es für acht Kern-Klassen **keinen einzigen Test** gab (B62). Mit der Welle fallen die **letzten zwei ablösbaren
> `Sprungziel`-Zweige** (`Sprungbruecke` führt nur noch `SpeicherOptimierung`, bis W16 — R‑W14c‑11), `ChartManager`
> (560 Z., **die MS-Chart-Bindung endet**), `RoundedPanel` und **alle sechs WFO1000** der Mappe (Warnungen 12 → 6, Rest
> Altbestand). Neun Sachcommits, ein Merge und ein Gate-Nachtrag (`8ee59d7` … `c1b049e`), auf `ios_migration` als
> `f7e2758`; 80 Textschlüssel de/en für zwei nie lokalisierte Masken.
>
> **17 Angleichungen, zwei hingenommene Abweichungen** (A‑1…A‑17): `Rueckfrage.VorgabeNein` für sechs Löschfragen,
> der Klimaimport lässt sich abbrechen, Klimaregion löschen fragt **und räumt die 8 760 + 365 Datenzeilen ab** (der alte
> Weg ließ Waisen), die Dublettenprüfung des Imports fragt die Datenbank statt der Präfixsuche, **ein** PVGIS-Abruf statt
> vier (kein gespeichertes Byte ändert sich), „Standardwerte" setzt den Datenbanknamen ins richtige Feld (B53, der einzige
> Rechenfehler), Dublettenscan im Hintergrund mit Fortschritt; hingenommen: Legende in den Klimabildern, kein Mausrad-Zoom.
> Sechs Befunde wörtlich trotz Befund (B3, B5, B8, B16, B30, B39). **Anwenderfragen (entschieden am 04.09.2026, umgesetzt in
> `e86eff6`/`766f349`/`1fbffd2`/`24c8912`, gemerged als `a0e6707`):** E‑3 (Klimaregion = die deutschen Regionen der
> Klimazonenkarte, Klimadaten = der weltweite TMY-Download: die Komponente heißt wieder `KlimadatenDialog`, der Menütext
> bleibt „Klimadaten"), E‑5 (ohne Ordnerwähler sind die fünf Pfade fest und **nur lesend**, Hinweistext de/en — die
> iOS-Sandbox), E‑6 („Altbereinigung ausführen": **Schema-Schritt 62** räumt Waisen in `Tab_Solar_STAMM` und
> `Tab_Klimadaten_STAMM` ab, `ZIEL_VERSION` 62 und neu `FREEZE_VERSION` 61, weil Freeze- und Zielstand bis dahin dieselbe
> Konstante waren; auf `Kenndaten_Test.sqlite` ein Leerlauf — 32 Regionen × 8 760 und × 365 exakt; Projektpakete mit
> Schemastand 61 werden nach Regel B2 abgewiesen; die Datenblöcke tragen ohnehin `ON DELETE CASCADE`, der Schritt ist ein
> Netz für Altbestände), E‑7 (keine Ortsliste in der Auslieferung; Katalognamen scheiden als Vorschlag aus, weil der
> Ortsname zugleich der Regionsname wird und A‑9 vergebene Namen abweist — Variante (c) bleibt), E‑8 (Hinweis:
> ohne WebView2-Laufzeit bleiben die letzten vier Admin-Masken leer). **Testanker:** `Form_Klimadaten` als Prüfmuster
> `Pruefmuster/Klimadaten/` (fünf Anker und der `Chart`-Typzeuge), drei Anker auf `MDIMainForm` (fällt als letzte, W16);
> Schwellen 20 / 17 / 11 / 17. Abweichung von der Vermessung benannt: der Test zählt 20 Designer-Dateien repoweit
> (18 + 2 generierte des Kerns).
>
> **Nachweise** (auf dem gemergten Stand `f7e2758`, Linux): Build → 0 Fehler, **6** Warnungen (12 nach W14a) ·
> `dotnet test WP-Plan.Kern.slnf` → **3 430** grün (3 227 nach W14a), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **124** grün · Stapellauf **17** Masken (21 − 4; `Form_KatalogDubletten` war eine Code-Form ohne
> Designer), 18 Designer, 17 erreichbar, **0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer 1 232 Texte, 0 Fundstellen ·
> ChartProben **32** Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · beide
> Wächter leer.
>
> **Protokoll** mit Feldkartenabgleich je Maske, der WFO1000-Bilanz, 17 Abweichungen, den Befunden B1…B64, der Zählung
> zu E‑6 und 13 Abnahmepunkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W14c_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Katalog aus beiden Razor-Aufrufern mit Esc-Ebenen, Klimaimport einmal echt gegen PVGIS
> vorher/nachher zahlengleich, Abbrechen, die zwei Bilder gegen den Bestand, Löschen mit Kaskade (`SELECT COUNT(*)`),
> Dublettenscan mit Fortschritt und Baum per Tastatur, Einstellungen speichern/zurücksetzen, KI-Schalter mit
> Maschinenriegel, Reihenfolge des Administrationsmenüs (B63), de/en, 125 %, fehlende Ortsliste. Der sechzehnte
> iOS-Lauf (33861268537) auf diesem Stand ist grün.
>
>
> **Anwenderwunsch W14c‑E‑9 vom 05.09.2026 (Admin-Dialoge an die Bildschirmgröße) mit Befund W14c‑B‑16, umgesetzt in
> `ddf4d00`:** Die drei Hüllklassen `epos-klimaregion-*` des `KlimadatenDialog` standen nie im Stilblatt — die
> „zwei Spalten" lagen deshalb untereinander; ersetzt durch den `Katalograhmen` (Liste links, Diagramme und Import
> rechts). Der `GesetzeskatalogDialog` gibt seiner einen Liste die volle Höhe (`epos-katalog-fuellend`, wie die
> ListView 916×424 in `Form_Gesetzesparameter`), der Zeileneditor bleibt Überlagerung; Dubletten und Einstellungen
> gewinnen allein durch das größere Fenster. Protokoll ergänzt.

> **Statusblock iU9 — Welle 14a umgesetzt (04.09.2026, Basis `01c9933` nach W13, zusammengeführt mit `c9855b1` nach W14b)**
>
> **Sieben Masken — 2 387 Zeilen `.cs`, 2 369 Designer, 39 `MessageBox` + 32 indirekte — sind drei Razor-Komponenten:**
> `KatalogBrowserDialog` (`EPOS.UI/Dialoge/Erzeuger/`) mit **vier Ausprägungen** über `KatalogBrowserProfil` (Heizkessel,
> BHKW, Solarkollektoren, Pufferspeicher mit `NurLesen`) — vier Masken waren Behälter um Editoren, die seit W6/W7 Razor
> sind —, `PufferSpKatalogDialog` (**der fehlende vierte Katalogeditor**, als Überlagerung im Browser) und
> `ModulKatalogDialog` mit zwei Ausprägungen (Stromspeicher als Vorbild, Photovoltaik bekommt dessen gepflegte Bauart).
> Im Kern: `KatalogZeilen`/`KatalogsatzAnzeige`/`SpeichernAus` je Katalog, die Speichertyp-Abbildung, `ModulKatalogProfil`,
> `DbWerte.SP_TYP_LITHIUM_IONEN`, `StromspeicherModel.C_VER_VORGABE` — und **`HeizkesselStammCtrl.Filtern` berichtigt**
> (W14‑B2: der Kern trug die Brennstoffkette der mit W6.3 gelöschten Maske; Fernwärme, Sonstige Energieträger und
> Wasserstoff filterten in **beiden** Heizkesseldialogen nicht — Vorher/Nachher-Zählung je Gruppe im Protokoll). Mit der
> Welle fallen die **letzten fünf ablösbaren `Sprungziel`-Zweige** (ihre Aufrufer sind Razor → Überlagerungen),
> `Views/Pufferspeicher/PufferSpFilter.cs`, `Allgemein/SpeichernLeiste.cs` und `Allgemein/KI/KiAufrufKnopf.cs` (E‑10:
> der KI-Einstieg aus einer Maske kommt mit W15b zurück). **Der Erreichbarkeitsbefund steht erstmals auf 0 nein /
> 0 verwaist / 0 unklar** — `Form_PufferSp_Bearbeiten` und `Form_SolarKollektorenAdmin` sind als Prüfmuster eingefroren
> (der „unklar"-Zeuge und der `DataGridView`-Typzeuge). Elf Sachcommits und ein Merge (`5fdbb4b` … `e5f387c`), auf
> `ios_migration` als `4e77221`. Der Nachweis (50 eingefrorene Fälle `KatalogVerwaltungTests`) entstand vor der ersten
> Maske; 97 Textschlüssel de/en für zwei nie lokalisierte Masken.
>
> **16 Abweichungen** (A‑1…A‑16), u. a.: „OK" liefert OK, ein Löschtext mit Namen, die achte BHKW-Leistungsstufe trifft
> (79 statt 8 Treffer vorher), `Exists`-Vorabtest überall, Löschen mit Rückfrage auch bei PV, der echte Löschgrund statt
> „Projektzuordnung", keine modale Prüfung beim Feldverlassen, der Kontextmenüweg des Stromspeichers öffnet den Katalog,
> Hersteller- und Speicherlisten sortiert. **Anwenderfragen:** E‑2 (Aperturfläche im Feld „Kollektorfläche" — wörtlich),
> E‑9 (je zwei Menüpunkte für Heizkessel und Pufferspeicher — beide behalten), **E‑11 neu** (die Solarkollektoren-Verwaltung
> hat zwei Flächenfelder, „Kollektorfläche" wird nirgends gefüllt — Modulfläche zeigen oder Feld streichen?).
>
> **Nachweise** (auf dem gemergten Stand `4e77221`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 227** grün (3 087 nach W14b), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **124** grün · Stapellauf **21** Masken (28 − 7), 21 erreichbar, **0 nein / 0 verwaist / 0 unklar** ·
> SQL-Prüfer 1 232 Texte, 0 Fundstellen · ChartProben 30 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017
> **PASS, byte-gleich** (815 043 Werte).
>
> **Protokoll** mit Feldkartenabgleich je Ausprägung, der Brennstoffzählung, 16 Abweichungen, den Befunden B1…B79 und
> 13 Abnahmepunkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W14a_Blazor_Port_Protokoll.md`. **Windows-Abnahme
> steht aus**: die Brennstoffkette in beiden Heizkesseldialogen, die achte BHKW-Stufe, Löschrückfrage PV, `NurLesen` des
> Pufferspeicherbrowsers, Kontextmenüweg Stromspeicher, die Überlagerungen statt Sprüngen, de/en, 125 %. Der fünfzehnte
> iOS-Lauf (33852944072) auf diesem Stand ist grün.
> **Zum Anwenderentscheid #76 vom 05.09.2026 geprüft und nicht betroffen (`b6fd863`):** der
> `KatalogBrowserDialog` (vier Ausprägungen) führt eine Liste plus Detailblock, kein Projekt↔DB-Paar; die vier
> Projektdialoge, die er als Sprungziel bedient, sind über Welle 6 auf `Zweispaltenauswahl` umgestellt.
>
>
> **Anwenderwunsch W14a‑E‑6 vom 05.09.2026 (Bildschirmfoto „Administration Solarkollektoren": Fenster klein, Liste
> und Eingabe untereinander mit Seitenrollbalken, Kopfzeile „Name | Name"), umgesetzt in `ddf4d00`:**
> `KatalogBrowserDialog` (vier Ausprägungen) und `ModulKatalogDialog` (zwei) stellen Liste und Eingabe wieder
> nebeneinander wie ihre sechs Vorbilder (`Form_Heizkessel_Admin` 726×383 bis `Form_BHKWAdmin` 856×517) — über den
> neuen Baustein `EPOS.UI/Bausteine/Katalograhmen.razor` (Liste links mit Filter, Detailblock rechts, Umbruch
> untereinander unter 900 CSS‑px = `--epos-zweispalten-umbruch`, die Liste rollt in sich; die Höchsthöhe aus W9‑B‑2
> fällt nur im Rahmen). Die Hülle `BlazorDialogForm` öffnet jeden `Fachdialog` im Anteil des Arbeitsbereichs
> (85 % × 90 %, gedeckelt auf 92 %; Rechnung plattformfrei in `EPOS.UI/Dienste/Fenstermass.cs`, `FenstermassTests`
> 11 Fälle); fünf kleine Masken tragen `Dialogart.Klein` und bleiben, wie sie waren. Die Wahlspalte heißt wieder
> „Wahl" — Ursache waren die zwei Hüllen (`KatalogBrowserHuelle` gab `SpalteName`, `ModulKatalogHuelle`
> `Listenbeschriftung`), beide lesen jetzt `KFAK_SP_WAHL`. Die Katalogwurzel rollt, statt die Schlussleiste
> abzuschneiden. 34 neue bunit-Fälle (`KatalograhmenTests`, `KatalogdialogTests`, `FenstermassTests`); Protokoll
> ergänzt.
>
> **Anwenderwunsch iU8‑E‑2 / W14a‑E‑7 vom 05.09.2026 (Bildschirmfoto „Administration Photovoltaik Module":
> „Verbessere die Darstellung der Dialoge, insbesondere der Parameter auf der rechten Seite: kompakter,
> übersichtlicher"), umgesetzt in `6ab0b9f`:** Im `Katalograhmen` aus W14a‑E‑6 nahm rechts jedes Feld die volle Breite,
> die Beschriftung stand darüber, die Zahlenfelder waren so breit wie die Textfelder und die Einheit stand am rechten
> Rand des Blocks — der Block war doppelt so hoch wie der Dialog und rollte. Gemessen an `Form_AdminPV.resx`
> (607 × 489) tat das Vorbild es anders: Beschriftungsspalte 178 px, Zahlenfeld 62 px, Einheit 4 px dahinter. Die
> Antwort ist eine **hausweite Regel und kein Sonderfall**: der Baustein `EPOS.UI/Bausteine/Formularraster.razor`
> (Beschriftung neben dem Feld in einer 12-rem-Spalte, `repeat(auto-fill, minmax(--epos-formularspalte, 1fr))`
> nach der Breite des Rasters — die rechte Spalte eines Katalograhmens liegt damit genauso richtig wie ein
> freistehender Dialog —, `Einspaltig` als benannter Rückweg) mit `Formulargruppe.razor` als leiser
> Zwischenüberschrift (`display: contents`, damit die Felder direkte Rasterkinder bleiben), dazu zwei Klassen, mit
> denen ein Feld seine Länge selbst meldet (`Zahlenfeld`/`Ganzzahlfeld` kurz mit der Einheit unmittelbar dahinter,
> mehrzeiliges `Textfeld` breit); unter 900 CSS‑px fällt die Beschriftung wieder über das Feld, `--epos-touchziel`
> bleibt die Mindesthöhe, und die Regel greift nur innerhalb von `.epos-formularraster` — ein Dialog hängt seinen
> vorhandenen Feldlauf hinein, mehr nicht. Acht Dialoge tragen die neue Form (`ModulKatalogDialog`,
> `KatalogBrowserDialog` ×4, `PufferSpKatalogDialog`, `BedarfAdminDialog`, `WaermebedarfAdminDialog`, dazu die
> Stichproben `HeizkesselDialog`, `GebaeudeDialog`, `EinstellungenDialog` — je eine Zeile). Die Bestandsaufnahme
> aller 92 Dateien mit 624 Feldbausteinen (41 Klasse A: reines Einhängen; 43 Klasse B: Handarbeit) und der
> Vorschlag für drei Restpakete stehen im Protokoll W14a; die Restumstellung läuft als Aufgabe #91 (Pakete P1
> Erzeuger W6/W7/W14a, P2 Kosten W1–W5, P3 Bedarf/Simulation/Projekt W8–W16a). `FormularrasterTests` 14 Fälle
> (darunter einer, der jede Selektorzeile des Blocks auf `.epos-formularraster` prüft), UI 2 546, Formularkarte
> 122.

> **Statusblock iU9 — Welle 14b umgesetzt (04.09.2026, Basis `01c9933` nach W13, zusammengeführt mit `34cc691`; parallel zu W14a)**
>
> **Vier Masken — 670 Zeilen `.cs`, 937 Designer, 11 `MessageBox` — sind zwei Razor-Komponenten:** `BedarfAdminDialog`
> (`EPOS.UI/Dialoge/Bedarf/`) mit **drei Ausprägungen** über `BedarfsArt` (Brauchwasser, Prozesswärme, Stromverbraucher —
> die drei Drillinge waren bis auf die Bezeichner zeichengleich; fünf ihrer sieben Knöpfe riefen schon die Razor-Dialoge
> aus W8) und `SolarganglinieAdminDialog` (`EPOS.UI/Dialoge/Solarthermie/`, Zwilling der Ganglinien-Verwaltungen aus W12
> und W13, Einlesen über `GanglinienTextDatei.Lies(pfad, mitKopfzeile: true)` mit `Fortschritt`). Im Kern:
> `BedarfStammCtrl.Bezeichner`/`Kopf`/`Loeschen`, **`BedarfsVorschauCtrl`** (die drei Vorrechnungen als ein Weg),
> `SolarganglinieStammCtrl.Exists`/`HatProjektzuordnung` (die Präfixsuche `FindString` war die einzige Dublettenprüfung,
> B70). Mit der Welle fallen `EPOS.Kern/Allgemein/ToolsClass.cs` (letzter Nutzer), das Sprungziel `SolarganglinieAdmin`
> (`SolarganglinieDialog` zeigt die Verwaltung als Überlagerung) und der Kleinschreibungs-Zeuge der Formularkarte wandert
> auf `WizardParent.designer.cs`. **Elf Sachcommits und zwei Merges** (`2a53d36` … `8b855ce`), auf `ios_migration`
> als `c9855b1`. Der Nachweis (27 Kern-Fälle + Probe `solarganglinie_8760.txt`) entstand vor der ersten Maske.
>
> **Sieben Abweichungen** (A‑1…A‑7): Leerprüfung vor dem Löschen auch beim Brauchwasser, ein Löschsatz mit Platzhalter
> statt dreier Schreibweisen, Fehlschlag und ReadOnly-Sperre als Warnbanner, Rückfrage vor dem Löschen der Solarganglinie,
> der Ganglinienordner sichtbar (stand auf `Visible = False`, B79), „OK" liefert OK. **Zwei neue Befunde:** B78 — der
> Knopf „Ergebnisse" stand in allen drei Drillingen im Code, aber in keinem Designer: er war seit jeher tot, die
> Anwenderfragen E‑7/E‑8 der Vermessung sind damit gegenstandslos; B79 (s. o.). **E‑6 (B49, Brauchwasser ohne Teiler)
> ist durch den Anwenderentscheid W8‑O‑5/W9‑O‑3 erledigt** — `Energieeinheit`/`BedarfEinheitWahl` im Kern, MWh als
> Vorgabe, kWh wählbar, konsistent in allen Bedarfsansichten; die Prozesswärme im W9-Weg rechnet ebenfalls über die
> Einheitenklasse (`SimulationWaermebedarf.ProzesssummeUebernehmen`, W9‑O‑3b). Offen: **W14b‑O‑1** (Jahressumme in
> drei Formaten — Anwenderfrage, Empfehlung `F2`), W14b‑O‑2 (`Rechenstand` des W9-Wegs und `BedarfsVorschauCtrl`
> zusammenführen), W14b‑O‑3 (gleichnamige Datei im Ablageordner wird weiterverwendet), **W8‑O‑5b** (Simulation →
> „Wärmebedarf-Details" teilt ein bereits in MWh stehendes Brauchwasser ein zweites Mal — Anwenderentscheid).
>
> **Nachweise** (auf dem gemergten Stand `c9855b1`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 087** grün (3 012 vor dem Merge), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **123** grün · Stapellauf **28** Masken (32 − 4), 27 erreichbar, 0 × „nein", 1 „unklar" (fällt mit
> W14a) · SQL-Prüfer 1 240 Texte, 0 Fundstellen · ChartProben 30 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017
> **PASS, byte-gleich** (815 043 Werte).
>
> **Protokoll** mit Feldkartenabgleich je Ausprägung, sieben Abweichungen, den Befunden B48…B79 und 25 Abnahmepunkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W14b_Blazor_Port_Protokoll.md`. **Windows-Abnahme steht aus**:
> die sieben Knöpfe je Ausprägung, die drei Jahressummen-Formate, Löschen leer/mit Rückfrage/schreibgeschützt, „Grafik"
> als Überlagerung mit Einheitenwahl, Solarganglinie (Ordner, Kopie, Einlesen mit Kopfzeile, Projektzuordnungssperre,
> Überlagerung aus dem Projektdialog), de/en, 125 %. Der fünfzehnte iOS-Lauf folgt nach dem Merge von W14a.
>
>
> **Anwenderwunsch W14b‑E‑9 vom 05.09.2026 (Admin-Dialoge an die Bildschirmgröße), umgesetzt in `ddf4d00`:**
> `SolarganglinieAdminDialog` steht nebeneinander wie `Form_Solarganglinie_Admin` (681×344, Liste links nach
> Hausanordnung); `BedarfAdminDialog` (drei Ausprägungen) und `WaermebedarfAdminDialog` bleiben gestapelt wie ihre
> Vorbilder (`Form_Stromverbraucher_Admin` 542×489, `Form_AdminWaermeeinlesen` 676×433), nehmen aber die Höhe des
> größeren Fensters (`Katalograhmen Gestapelt`). Protokoll ergänzt.

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
> steht aus** (§ 10 des Protokolls, zwölf Punkte). Der vierzehnte iOS-Lauf (33844935661) auf diesem Stand ist grün.
>
> **Windows-Abnahme 05.09.2026, Befund W13‑B‑1 („Admin: vdi3805 Datei import: Absturz bei Datei laden, teilweise
> Absturz auch bei Dateiauswahl-Dialog"), behoben in `4fd8cc7`:** Zwei Ursachen. Der modale Dateiwähler lief
> **synchron im `WebMessageReceived`-Rückruf** derselben WebView2 — `KatalogImportHuelle.DateiWaehlen` gab
> `Task.FromResult(Dienste.Datei.DateiOeffnen(…))` heraus, ein schon erfüllter Task, der `OpenFileDialog` pumpte seine
> Nachrichtenschleife also in der WebView, die gerade zeichnet (wortgleich das Muster von W16b‑B‑1, eine Ebene tiefer;
> elf Hüllen hatten dieselbe Zeile, ob es gutgeht, hing an der Zeitlage — daher das „teilweise"). Und eine Ausnahme
> aus einem Blazor-Ereignis hatte **kein Netz** — der WinForms-`BlazorWebView` 10.0.100 führt kein
> `UnhandledException`. Behebung: `IDateiDienst`/`IDialogDienst` führen wartbare Zwillinge mit Standardfassung
> (`DateiOeffnenAsync`, `DateiSpeichernAsync`, `OrdnerWaehlenAsync`, `MeldungAsync`, `WarnungAsync`, `FrageAsync`);
> die Windows-Fassungen posten sie über `Allgemein/Blazor/Blazornachlauf.cs` — der Bruder von `Blazorsprung` für den
> Fall **mit** Rückgabewert — eine Nachricht später; `Dateiwahl.razor` und `KatalogImportDialog` brauchten keine Zeile,
> sie warteten von jeher. Dazu die **Fehlerschranke** (`EPOS.UI/Bausteine/Fehlerschranke.razor` auf `ErrorBoundaryBase`,
> `Wurzel<T>`), die alle drei Hüllen und die iOS-Seite statt `T` mounten. Der Kern ist als Ursache ausgeschlossen: neun
> neue Fälle fahren alle vier Ausprägungen gegen sechs Bauarten kaputter Dateien, `Lesen` macht daraus eine
> `IMP_KAT_PROT_LESEFEHLER`-Meldung. Auf iOS war derselbe Befund ein anderer Fehler: `IosDateiDienst.AufDemHauptfaden`
> lieferte vom Hauptfaden `default`, der Wähler ging nie auf — mit den `…Async`-Fassungen behoben. Protokoll § 13,
> Abnahmepunkte B1–B7 (Wähler geht auf, kaputte Datei → Warnbanner, Fehlerkasten mit rotem Rand statt Absturz).
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `PvModulImportDialog` (Klasse B, 21 von 28 Feldern in drei Rastern — die drei
> Detailreiter sind Formularblöcke, die zwei Filterleisten `epos-pvimport-filter` über dem Modulgitter nicht;
> `epos-pvimport-details` samt Regel gefallen). Nachtrag im W13-Protokoll, obwohl die Datei in der P3-Pakettabelle
> stand.

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
> **Anwenderentscheid #76 vom 05.09.2026 („#76: Empfehlung"), umgesetzt in `b6fd863`:** `StromganglinieDialog` steht im Baustein `Zweispaltenauswahl`; die zwei
> Glyphen-Parameter samt `STROMGL_BTN_HINZUFUEGEN`/`_ENTFERNEN` sind ohne Nutzer und gefallen.
>
> **Windows-Abnahme 05.09.2026, Befund W12‑B‑1 (Bildschirmfoto „Standard Stromprofil": „Beschriftung der Buttons
> nicht zur Umrandung passen" — die vier Katalogknöpfe unter der Datenbankliste liefen über ihren Rahmen bzw. waren
> abgeschnitten), behoben in `e56ac6d`:** Ursache waren zwei Zeilen im Hausblatt, nicht der Baustein
> `Zweispaltenauswahl`: `.epos-leiste` war ein `display: flex` ohne `flex-wrap`, `.epos-knopf` hatte neben
> `min-width: 88px` die Vorgabe `flex-shrink: 1` — in der halb so breiten rechten Spalte schrumpften die vier Knöpfe
> auf ihre 88 px, während „Stromverbraucher" als unteilbares Wort breiter blieb. Behoben an **einer** Stelle für das
> ganze Haus, ohne einen Dialog anzufassen: `flex-wrap: wrap` an der Leiste, `flex: 0 1 auto` mit `white-space:
> normal` und `overflow-wrap: break-word` am Knopf, `padding: 4px 12px`; **kein** `overflow: hidden`, denn
> Abschneiden wäre derselbe Fehler in still; einzeilige Knöpfe („OK", „Abbrechen", Fußleiste) bleiben in Höhe und
> Breite, wie sie waren. Mit erledigt sind die Katalogleisten aller elf Projekt↔DB-Dialoge (vier Knöpfe bei
> `BedarfsProfileDialog` und `GebaeudeDialog`, drei bei BHKW/Heizkessel/Solarkollektoren, zwei bei
> Pufferspeicher/Photovoltaik/Wärmebedarf extern) und die der Katalogverwaltungen am `Katalograhmen`. Drei neue
> Wachen in `ZweispaltenauswahlTests` (14 → 17: Markup, Bestand, Stilregel), Gegenprobe mit zurückgedrehtem Blatt
> rot. Protokoll W12, Abnahmepunkte A‑W12‑B‑1.
>
> **Anwenderwunsch W12‑E‑1 vom 05.09.2026 (Bildschirmfoto „Stromganglinien": „csv-Datei Stromlastgang importieren
> (mit Info zum Format) fehlt. Ebenfalls fehlt löschen und Speichern unter"), umgesetzt in `43f0581`:** Das Vorbild
> `Form_Stromganglinie` (678 × 345) hatte keinen Import, kein Löschen und kein Speichern unter — der Wunsch ist eine
> echte Erweiterung; „Datei Einlesen…" und „Ganglinie Löschen" lagen eine Maske weiter in `Form_Stromganglinie_Admin`,
> „Speichern unter" gab es im ganzen Bestand nicht (der Eintrag „… - Kopie" der Testdatenbank ist ein zweiter Import
> unter anderem Dateinamen). Der Dialog trägt unter der Katalogliste jetzt **vier** Knöpfe statt einem: „CSV-Datei
> importieren…", „Speichern unter…", „Löschen", „Bearbeiten…". Der Import ist kein zweiter Weg, sondern derselbe: Die
> Kette liegt seit W12.0d im Kern (`GanglinienImportAblauf`), ihre Oberflächenseite steht jetzt im Baustein
> `GanglinienImportLauf.razor` (drei Überlagerungen, `Starten(pfad, raster)`), den auch die Verwaltung
> `StromganglinieAdminDialog` einhängt (422 → 303 Zeilen) — die Überlagerungen gibt es einmal statt zweimal. Der
> Formathinweis nennt sichtbar, was die Kette annimmt (8 760 bzw. 35 040 Werte, vier Feldtrennzeichen oder einspaltig,
> erkannte Kopfzeile, Komma oder Punkt, kW oder kWh je Intervall, Bezeichner = Dateiname ohne Erweiterung) und steht
> als Kurztext am Infoknopf. Löschen prüft zwei Sperren vor der Rückfrage und meldet beide Gründe — Projektzuordnung
> (`StromganglinieStammCtrl.HatProjektzuordnung`, Muster W14b) und `ReadOnly` (Grund als `title` am Knopf).
> „Speichern unter" ist die Kopie unter neuem Namen (`KopiereStamm`: Kopf und Werte in einer Transaktion, `ORDER BY
> ID`, immer `ReadOnly = false`, Vorschlag „‹Name› - Kopie", Dublettenprüfung vor dem Einfügen in Maske und Kern).
> Nebenbefund behoben: `ReadAll` warf `ReadOnly` weg — die Verwaltungshülle fragte je Zeile nach (N+1), der
> Projektdialog konnte einen Auslieferungssatz nicht erkennen. Kern 1 086 (+10 `StromganglinieKatalogTests`), UI 2 509
> (+16), SQL-Prüfer 1 204 / 0. Protokoll W12, zehn Abnahmepunkte.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `PeakShavingDialog` (19 Felder, vier Raster, sieben `epos-feldpaar` gefallen; „minimale
> Schwelle ermitteln" steht neben der Zielschwelle, Ergebnisreiter bleibt Tabelle) und
> `GanglinieImportOptionenDialog` (Klasse B: die acht Formatlisten im Raster, das Vorschaugitter nicht).
> `StromganglinieDialog`/`StromganglinieAdminDialog` unangetastet (W12‑E‑1/E‑2).
>
> **Anwenderwunsch W12‑E‑2 vom 05.09.2026 („stelle die importierte Stromganglinie als Grafik dar (wie bisher, zoombar,
> umschaltbar auf sortiert)"), umgesetzt in `dbdcdf1`:** Ein „bisher" gab es nicht: Weder `Form_Stromganglinie_Admin`
> noch `Form_Stromganglinie` noch `Wizard_Stromlastgang` trug je ein Chart, und das einzige Chart der
> Stromverbrauchermasken (`Form_ErgStromverbraucher`) zeichnete Monatssäulen. Übernommen ist deshalb das einzige
> Vorbild, das der Bestand kennt — das Bild B1 des Bedarfsreiters (`ChartRenderer.GanglinieNormiert`) — in der
> Anordnung des `GebaeudeBedarfDialog` (W9.8). Sobald links oder rechts eine Zeile markiert ist, steht unter den zwei
> Spalten der neue Baustein `GanglinienGrafik`: drei Kennzahlen (Jahresarbeit, Spitze als 100‑%-Linie,
> Vollbenutzungsstunden), der Schalter „sortiert", die Einheitenwahl MWh/kWh (W8‑O‑5) und das Bild im Baustein
> `Diagramm` mit Bild- und Datenzoom. Die Zahlen liefert `StromganglinieAuswertungCtrl` im Kern aus derselben Wertspalte
> wie der Lauf (Katalog und Projektkopie); 35 040 Viertelstundenwerte gehen durch
> `SimulationControl.Viertelstunden_zu_Stundenwerte_Mittelwert` — kein zweiter Rechenweg, kein neues Renderer-Bild.
> Den Platz gibt der Formathinweis her: sichtbar ist eine Zeile (`STROMGL_HINWEIS_FORMAT_KURZ`), der volle Wortlaut
> hängt am Infoknopf. Eingefroren gegen die Testdatenbank: `Lastgang_Strom_NestleLB` 4 790,086 MWh / 2 070,00 kW /
> 2 314,05 h/a; `test` (35 040 → 8 760) 4 788,929 MWh / 1 310,75 kW / 3 653,58 h/a. Tests: `StromganglinieDialogTests`
> 30 → 41, `StromganglinieAuswertungTests` 7 neu; ChartProben und SQL-Prüfer unverändert grün. Zehn Abnahmepunkte
> A‑W12‑E‑2 im W12-Protokoll.

> **Statusblock iU9 — Welle 11b umgesetzt (04.09.2026, Basis `81a04ec` nach W11a, zusammengeführt mit `604d1f6`)**
>
> Der zweite Lauf der Welle 11: **`Form_Simulation_Detail` (7 766 Zeilen + 3 082 Designer), `DashboardForm`,
> `NavigatorUebersicht`, `NavigatorStrom`, `NavigatorWaerme` und `Form_SpeicherVariantenVergleich` → eine
> Razor-Seite `SimulationErgebnisSeite`** (`EPOS.UI/Seiten/Simulation/`) mit **zehn** Blättern (R3 „Simulation“
> war nur der Behälter der Menüliste, A‑1), dem Ergebnis-Blatt mit Autarkie-Analyse, den Ganglinien-Navigatoren
> Wärme/Strom und dem Variantenvergleich als Überlagerung; `TabNavigationManager`, `TabListMapper`,
> `DonutChartDrawer`/`Kacheln` gelöscht (`ChartManager` bleibt für Klimadaten und Peak-Shaving, A‑12).
> **Hosting-Entscheid R‑W11‑1:** Seite mit `SeitenZustand` (iOS erreicht sie über `AppWurzel`), auf Windows
> bis W16 in der modalen Dialoghülle, weil die Bedarfsobjekte der Startmaske gehören (**eingelöst mit W16b, E‑5,
> 04.09.2026:** das Ergebnis ist eine `Ueberlagerung` der Razor-Startseite, die Bedarfsobjekte gehören dem Projekt,
> `BedarfsZustand`; der Automatikstart beim Öffnen bleibt). Die Hülle fährt
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
> **Windows-Abnahme 05.09.2026 („Allgemein bei Charts: das Zoomen funktioniert nicht"; PDF S. 8–9), drei
> Diagrammbefunde behoben (`db35a7b`, Protokoll § 10a):** **A‑1** nimmt den in A‑7 aufgegebenen Zoom zurück (Risiko
> R‑W11‑5 geschlossen): Jedes Renderer-Bild steht im neuen Baustein **`Diagramm`** (`Diagramm.razor` +
> `epos-diagramm.js`, über `ChartBild` für alle **34** Bilder, auch Kuchen, Ringe und Kennlinien) — Mausrad zoomt
> um den Zeiger, Ziehen verschiebt, Doppelklick/Taste 0 zurück, +/−, Pinch, Stufenanzeige „×2,5", Knopf „1:1",
> ganz im Browser ohne Neuzeichnen; **Datenzoom** für die Jahresganglinien (Bedarf, Wärmegang, Stromgang) über
> ein Rechteck (Umschalt+Ziehen oder Knopf „Bereich") → der Kern zeichnet mit einem `Achsenfenster` neu, die
> x-Achse trägt die wirklichen Jahresstunden in runden Schritten; ohne Fenster zeichnet jedes Bild byte-genau
> wie zuvor (iU7-Renderer unverändert PNG, `ChartProben` prüft jetzt 34 Bilder und 2 Gegenproben, die 30
> Bestandsbilder byte-gleich). JS über `import()` wie `epos-verlauf.js`, die `index.html` beider Wirte
> unverändert; WKWebView-Punkte (`wheel`+`ctrlKey`, `gesturestart` unterdrückt, `touch-action: none`) sind
> vorbereitet. **W11b‑B‑2** stellt die Diagramme der Ergebnisseite über die volle Rasterbreite (eine Regel
> `.epos-simerg-diagrammzeile` statt acht Sonderfälle, `min-height` 280 px). **W11b‑B‑3**: die Streuwolke B4
> „Leistung über Außentemperatur" hatte richtige Serien und Achsentitel, aber fünf gleichmäßig verteilte Marken
> (−18,2 … −5,3 … 7,7) — jetzt runde Teilung (1/2/2,5/5 × 10^k), aufgerundete Bereiche, Ränder für Legende und
> Titel. Abnahmepunkte 18–24; Punkt 22 (iPad-Pinch, Seite zoomt nicht mit) bleibt bis zur Geräteprüfung offen.
> Bewusste Vereinfachungen: die Null bleibt unten, B1 nimmt nur den Zeitausschnitt, „1:1" verwirft Bild- und
> Datenzoom, Doppelklick nur den Bildzoom.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `ParameterReiter` der Simulationsseite (Klasse B, 23 Felder, neun Raster, alle einspaltig —
> unter jeder Zahl steht ihre Entsprechung; `Daten.Unterblaetter` und Grafikreiter bleiben).

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
> **Eingelöst mit W16b (E‑5, 04.09.2026):** die Konfiguration löst die Razor-Startseite in derselben WebView ab, die
> modale Hülle ist gefallen; die zwei Bedarfsobjekte gehören dem Projekt (`BedarfsZustand`), nicht mehr einem Fenster.
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
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `QuelleErdreichDialog` („Standort" einspaltig, jeder Hinweis unter seinem Feld; Vorschau
> und Auslegungsprüfung bleiben am Diagramm), `QuellprofilDialog` (die 24 Stundenwerte zu zweit je Zeile; Werteseite
> bleibt Tabelle), `WaermesenkeDialog` (Klasse B: beide Blöcke einspaltig, weil jeder Schalter das Feld unter sich
> freigibt; die Senkenliste bleibt Zeilenraster).
>
> **Anwenderbefund W10b‑B‑1 vom 05.09.2026 („Simulation → Simulation-Konfiguration → Ansicht Schema: Die Pfeile sind
> teilweise zu dick und falsch dargestellt"), behoben in `4e39d4a`:** Drei Sachen, drei Ursachen, keine im Rechenweg.
> Die Farbregel des Stilblatts setzte neben dem Strich auch die **Füllung** und schlug dabei das `fill="none"` am
> Element — der offene Bézierbogen wurde als Fläche ausgemalt (die „riesige blaue Fläche mit gezacktem Rand"); und
> `markerUnits="strokeWidth"` machte aus der 9×7-Spitze bei 1,8 px Strich 16×13 px. Die Füllung gehört jetzt allein
> der Pfeilspitze, die Spitze misst feste 10×8 Nutzereinheiten, die Strichstärke steht gedeckelt im Kern (2 px,
> hervorgehoben 3, Grenzen 2–6) als Attribut am Element — eine Breite aus Leistung oder Volumen gab es nie und wird
> bewusst nicht eingeführt. Die Wegführung in `SchemaLayout` ist von Bézierbögen auf **Spaltenbahnen** umgestellt:
> waagerecht aus dem Kasten, senkrecht in der Gasse, waagerecht in den Zielkasten; jede Leitung mit eigener Senkrechte
> (`GassenBelegen`), übersprungene Spalten auf einer kastenfreien Bahn gequert (`FreieBahn`), mehrere Ansätze an
> derselben Kastenseite über die Kastenhöhe verteilt (`AnkerVerteilen`) — keine Leitung kreuzt mehr einen Kasten, für
> sechs echte Projekte Strecke für Strecke nachgeprüft. Der Widerspruch „Quelle: Puffer 3000Ltr · Kaskade" gegen
> „Keine Kaskade im Projekt" entstand, weil der Satz am leeren Kaskadenband hing und das Band nur bei einem Lader auf
> Rang 1 entstand — Projekt 1042 lädt seinen Quellpuffer auf Rang 2. `SchemaModell.HatKaskade` trägt die Tatsache
> seither selbst, die Kettensuche geht über alle Ränge, es gilt `HatKaskade ⇔ Band`. Nebenbefund: 1030 „B3-Kaskade"
> trägt trotz Namens keinen Quellpuffer; die echten Kaskadenprojekte der Testdatenbank sind 1042, 1043, 1044.
> Nachweise: `SchemaLayoutTests` 19, `SchemaModellTests` neu 10 gegen die Testdatenbank, `SchemaTests` 16; Kern 1 190
> und UI 2 648 grün in beiden Kulturen, Kern-Wächter leer, kein Referenzlauf nötig (das Schema rechnet nichts). Sechs
> Abnahmepunkte im W10b-Protokoll.

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
>
> **Nachzug iU8‑O‑1 (06.09.2026), umgesetzt in `e6bc2fd`:** Der letzte offene Dialog des Anwenderwunsches iU8‑E‑2 steht im
> Raster. Alle drei Parameterblöcke von `PufferSpProjektDialog` (Eigenschaften, Schichtung und Leistungsgrenzen,
> Ladereihenfolge) liegen jetzt im `Formularraster`; sechs `Formulargruppe`n gliedern die 21 Felder — Volumen und
> Verluste, Temperaturen, Schwellen, Leistungsgrenzen und, erst ab zwei Schichten, Schichtmodell und Entnahmehöhen.
> Kein Raster ist einspaltig: Die Felder tragen hier keine Reihenfolge, sie gehören paarweise zusammen
> (Vorlauf/Rücklauf, Ein‑/Abschaltschwelle, Lade‑/Entladeleistung) — das unterscheidet ihn von `WaermesenkeDialog` und
> `QuelleErdreichDialog` aus P3. Die beiden `Zeilenraster` (Bestand, Ladereihenfolge) bleiben draußen; eine Liste ist
> kein Formularblock. Kein Feld verloren, keines umbenannt, keines verschoben (2 Auswahl‑, 1 Text‑,
> 1 Mehrfachauswahl‑, 4 Ganzzahl‑, 13 Zahlenfelder vorher wie nachher); die eine Herleitungszeile weniger ist der Kopf
> der Entnahmehöhen, der jetzt der Titel seiner Gruppe ist. Eine Selektorzeile im Stilblatt, fünf Ressourcenschlüssel
> de/en, `Resource.Designer.cs` mit `Werkzeuge/ResourceDesigner` erzeugt (4 870 → 4 875). `EPOS.UI.Tests`
> 2 679 → 2 683, grün unter de und en. **iU8‑O‑1 ist geschlossen.** Fünf Abnahmepunkte A‑iU8‑O‑1 im W10a-Protokoll.

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
> **Windows-Abnahme 05.09.2026 (PDF des Anwenders, S. 4–5), Befunde W9‑B‑4 (Prozesswärme) und W9‑B‑5
> (Standardlastprofil), behoben in `490c48e`:** „Simulation bringt Ergebnis 0 (monatlicher Verlauf), Grafik
> bleibt leer". **Nicht** die Einheit — der Regressionsverdacht auf W8‑O‑5/W9‑O‑3b ist ausgeschlossen
> (`Energieeinheit.MWh.AusMWh` ist die Identität, kein DTO-Feld vertauscht) — sondern die **Namensauflösung**: die
> Zuordnungen des Projekts tragen die Namen der **Projektkopien** (`Tab_*.Bezeichner`, in der Testdatenbank acht
> mit Zusatz „(P‹Projekt›)"), die Vorschau des Bedarfsprofil-Dialogs schlug sie aber ausschließlich im
> `_STAMM`-Katalog nach (Modus aus `list == null ? Projektrechnung : Katalogvorschau`, so seit V0‑4, kein
> W8/W9-Regress) und übergeht Unbekanntes still — zwölf Nullmonate. `ProfilBedarf.Vorschaumodus(namen, idProjekt)`
> hält die Regel einmal fest: ohne Liste Projektrechnung, mit Liste ohne Projekt Katalogvorschau, mit Liste **und**
> Projekt die neue **Projektvorschau** — Katalog zuerst (jede heute richtige Zahl bleibt zeichengleich), Projektkopie
> als Rückfall mit Kopf und Typprofil. Messung Projekt 1017: Vorschau 0 → 672 000,4 kWh. Zeuge zuerst rot, fünf
> Wachen in `BedarfsProfilVorschauTests`, Referenzlauf byte-gleich (alle elf rechenbaren Basisprojekte). **Offener
> Anwenderentscheid W9‑O‑3c:** eine im Projekt geänderte Kopie wird in der Vorschau weiter mit der Katalogverteilung
> gezeigt (Brauchwasser 1007: Januar 1,900 statt 0,552 MWh bei gleicher Jahressumme); Kopie zuerst brächte Vorschau
> und Lauf zur Deckung, ändert aber angezeigte Zahlen — bewusst nicht nebenbei entschieden.
> **Windows-Abnahme 05.09.2026 (PDF S. 1–2), drei Befunde behoben (`974c198`, Protokoll § 12):** **W9‑B‑1** — das
> im Projekt gespeicherte Gebäude stand unmarkiert in der Liste, weil `AssistentSeite` den Parametersatz der
> stehenden Seite bei **jedem Neuzeichnen** neu zog (die Hüllen bauen dabei eine neue Anzeigeliste — der lebenden
> Komponente wurde die Liste unter den Füßen getauscht) und `GebaeudeDialog` seine Markierung an der
> Objektgleichheit festmachte; der Seiteninhalt wird jetzt beim **Betreten** geholt und gemerkt (wie
> `WizardParent.Next/Back`), die Markierung läuft über die `IdZ` und wird bei einem Listenwechsel nachgezogen.
> **W9‑B‑2** — „Liste zu lang": jede Rasterliste steht seither in einem festen Rahmen mit Rollbalken
> (`--epos-listenhoehe` 22 rem an `.epos-raster-huelle`, stehender Spaltenkopf, Parameter `Begrenzt`, Rückweg
> `--frei`); Anwenderregel, in `EPOS.UI/CLAUDE.md` festgehalten, gilt für Gebäude, Wärmebedarf extern,
> Bedarfsprofile, Gebäudetyp, Stromganglinien, Solarkollektoren, Wärmepumpe, die vier Katalogverwaltungen und die
> Projektdialoge. **W9‑B‑3** — die zwei Richtungsknöpfe des Gebäude-Dialogs tragen Beschriftung und Kurztext in
> beiden Sprachen, das Zeichen zeigt in die Wanderrichtung; das Anordnungsschema selbst ist der Anwenderentscheid
> **#76 („Empfehlung": altes Schema nebeneinander, Umbruch auf schmalem Schirm)** und folgt als eigene Welle, die
> zwei Geschwisterdialoge stehen als W9‑O‑8 offen. 21 bunit-Wachen; Abnahmepunkte A‑W9‑B‑1…B‑3.
> **W9‑O‑3c entschieden (05.09.2026, „W9‑O‑3c: Empfehlung", `ab60806`):** Die Projektvorschau des
> Bedarfsprofil-Dialogs liest die **Projektkopie zuerst** und den `_STAMM`-Katalog nur noch als Rückfall für die
> noch nicht gespeicherte Zeile — Vorschau und Lauf zeigen überall dieselben Zahlen (Brauchwasser 1007: Januar
> 1,900 → **0,552 MWh** bei unveränderter Jahressumme 4 059,7 kWh; Prozesswärme 1041 und Stromverbraucher 1024
> zeichengleich). Projektrechnung und Katalogvorschau unberührt; die eingefrorene Wache „Katalog bleibt erste
> Quelle" ist auf den Entscheid umgestellt, sieben Fälle in `BedarfsProfilVorschauTests`, Referenzlauf über alle
> elf rechenbaren Projekte byte-gleich. Abnahmepunkt A‑W9‑O‑3c.
> **Anwenderentscheid #76 vom 05.09.2026 („#76: Empfehlung"), umgesetzt in `b6fd863`:** `GebaeudeDialog`, `WaermebedarfExternDialog` und
> `BedarfsProfileDialog` stehen wieder **nebeneinander wie im BHKW-PLAN** (Vorbild `Form_Gebaeude` 252/63/436 px:
> Filterblock rechts über der Katalogliste, Detailblock unter dem Paar, Kanalklappliste links bei der Projektzeile)
> und brechen erst unter 900 px untereinander um; die Listen bleiben höhenbegrenzt (W9‑B‑2). Das Pfeilzeichen ist aus
> `GEB_BTN_UEBERNEHMEN`/`_ENTFERNEN` entfernt und hängt jetzt an der Anordnung; „übernehmen" zeigt wie im Vorbild
> zur Projektliste (◀). **W9‑O‑8 damit geschlossen.** Wache `ZweispaltenauswahlTests` (14) mit Medienabfrage gegen
> Token, drei Anordnungsfälle, Selektoren in zwölf Testklassen nachgezogen.
>
> **Anwenderwunsch W9‑E‑2 vom 05.09.2026 (zwei Bildschirmfotos: „der Wärmebedarf vom Gebäude sollte aus diesem
> Dialog (mit Button Simulation) aufgerufen werden können – analog wie aus dem Simulationsbereich", ohne
> Brauchwasser und ohne Gesamt), umgesetzt als **W9.8** in `7811b5d`:** Der Gebäudedialog zeigt über den neuen
> Knopf „Simulation…" (neben „Ändern", frei bei markiertem Projektgebäude, nicht in der Katalogverwaltung) den
> Wärmebedarf **eines** Gebäudes als vierte Überlagerung — Heizung allein, ohne Brauchwasser und ohne Gesamtsumme:
> Wärmebedarf [MWh/kWh wählbar, W8‑O‑5], maximale Wärmelast [kW], Vollbenutzungsstunden, die Jahresganglinie als
> `ChartRenderer.GanglinieNormiert` (dasselbe Bild B1 wie auf der Ergebnisseite, nur mit einer Reihe) im Baustein
> `Diagramm` mit „sortiert", Bild- und Datenzoom, dazu die Monatsübersicht. Ein Vorbild gab es nicht — `Form_Gebaeude`
> trug nur `btn_Aendern`, `Form_Simulation_Kurz` (iF29) rechnete den ganzen Lauf; **neu ist die Auskunft, nicht die
> Rechnung.** Gerechnet wird im Kern (`EPOS.Kern/Controller/GebaeudeBedarfCtrl`) mit **denselben** Methoden wie der
> Lauf: `SimulationWaermebedarf.KlimakalenderLesen` und `…HeizwaermeEinesGebaeudes` sind Anweisung für Anweisung aus
> `Waermebedarf_berechnen` herausgezogen, die Schleife des Laufs ruft sie; Schlüssel ist `Z_ProjektGebaeude.ID`
> (eine neue Abfrage `SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?`), die Jahressumme rechnet wie der
> Lauf in float. Der Referenzlauf bleibt byte-gleich, bei einem Ein-Gebäude-Projekt (1007) ist die Zahl des Dialogs
> bitgleich zu `Waermebedarf_Gebaeude_Gesamt`. Ohne Zahl (ungespeicherte Zeile, Projekt ohne Klimaregion) meldet der
> Dialog. 13 Kern- und 20 bunit-Fälle (eine Wache: kein Brauchwasser, kein „Gesamt"), acht Texte de/en; auf iOS ist
> der Dialog nur als Assistentenseite erreichbar, `BedarfGaben` ist dort mitzudenken, wenn der Assistent in iU11
> verdrahtet wird. Protokoll W9.8.
>
> **Nachtrag zu W9‑B‑4/B‑5 (`99033ce`, W8‑B‑3):** Der heute Vormittag berichtigte Rechenstand war nicht die Ursache
> des Nullwerts im Ergebnisdialog; die Namensauflösung der Projektkopien arbeitet korrekt — der Profilbedarf ging
> erst danach verloren, in der Abschrift der Vorschaurechnung in `BedarfsProfileHuelle`, die mit W8‑B‑3 ersatzlos
> entfällt (`BedarfsVorschauCtrl.ProjektVorschau` im Kern).
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `GebaeudeKatalogDialog` (41 Felder, neun Raster in beiden Reitern; „Wohnfläche" kurz mit
> „m²", die Ferientage als Tag | Monat nebeneinander — acht `epos-feldpaar` gefallen) und `GebaeudeWohnflaecheDialog`
> (zwei Raster).
>
> **Anwenderwunsch W9‑E‑3 vom 05.09.2026 („Gestalte den Dialog bei Wärmebedarf → Daten importieren analog zum Import
> des Strombedarf → Messdaten importieren (mit grafischer Darstellung etc. wie kürzlich vorgenommen)"), umgesetzt in
> `4d64626`:** Der Dialog „Wärmebedarf Extern" folgt seither `StromganglinieDialog` nach W12‑E‑1/W12‑E‑2. Unter der
> Katalogliste stehen vier Knöpfe — „CSV-Datei importieren…", „Speichern unter…", „DB Ganglinie löschen",
> „Einlesen/Bearbeiten.." —, darunter der einzeilige Formathinweis mit dem vollen Wortlaut am Infoknopf
> (`WBX_HINWEIS_FORMAT`/`…_KURZ`, de/en), und unter den zwei Spalten die Grafik der markierten Ganglinie (Kennzahlen,
> „sortiert", Einheitenwahl, Bild B1 mit Bild- und Datenzoom). Der Befund der Umsetzung ist eine Doppelung: Der
> Wärmebedarf führte eine zweite, engere Importkette neben der AP5-Kette des Stroms (eine Textzeile je Wert,
> Dezimaltrenner Punkt, kein Trennzeichen, keine Einheitenwahl, kein Protokoll, nur 8 760 Werte). Sie ist ersatzlos
> entfallen: `GanglinienImportAblauf` bekommt mit `GanglinienZiel` eine Ausprägung als Daten (Muster
> `KatalogImportProfil`), und vier Masken hängen denselben Baustein `GanglinienImportLauf` ein — der Wärmeimport kann
> seither Excel, Kopfzeilen, Trennzeichen, kWh je Intervall und Viertelstundenwerte. Den Rechenweg der Kennzahlen gibt
> es nur einmal: `StromganglinieAuswertungCtrl` ist zu `GanglinienAuswertungCtrl` mit `GanglinienQuelle`
> verallgemeinert, dieselbe Verdichtung wie im Lauf; die Bausteine `GanglinienGrafik` und `GanglinienImportLauf` liegen
> jetzt unter `Bausteine/`. Drei Befunde fielen dabei: Der Dialog kannte das Auslieferungskennzeichen nicht (er holte
> nur eine Namensliste), die ReadOnly-Meldung der Wärmeverwaltung sprach von der „Stromganglinie"
> (`WBAD_MSG_SCHREIBGESCHUETZT`), und Überschreiben wechselte die Kopf-Id (`ErsetzeGanglinie` behält sie). Eine
> Falle, die es beim Strom nicht gibt: `Z_ProjektWaermebedarf.ID_Ganglinie` zeigt auf die Projektkopie, eine eben
> aufgenommene Zeile trägt die Stamm-Id — der Dialog gibt die Id nur bei `IdZ > 0` weiter, sonst Rückfall über den
> Bezeichner. Hausregel aus dem Umzug: Ein Baustein, der Komponenten eines anderen Namensraums zeichnet, braucht das
> `@using` im Kopf — sonst hält der Razor-Übersetzer sie stumm für HTML-Elemente. Der Kanal bleibt, wie er war, und
> steht im `Formularraster`. Eingefroren: `Wärmebedarf_Laurentiuskirche` 65,430 MWh / 47,649 kW / 1 373,16 h/a.
> Nachweise: Kern 1 230 und UI 2 679 grün (auch en‑US), Windows-Bau 0 Fehler, SQL-Prüfer 0 Fundstellen, ChartProben
> 40/0, Kern-Wächter leer; Referenzlauf nicht nötig — gelesen wird nur. Vierzehn Abnahmepunkte A‑W9‑E‑3 im W9-Protokoll.

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
> **Windows-Abnahme 05.09.2026, Befund W8‑B‑1 (`490c48e`):** Ergebnisdialog, Einheitenwahl und ChartRenderer
> sind **entlastet** — die Nullreihe hinter „Prozesswärme/Standardlastprofil bringt 0, Grafik leer" entstand in der
> Vorschau der Welle 9 (W9‑B‑4/B‑5, Namensauflösung der Projektkopien); die leeren Achsen 0–5 sind das korrekte
> Bild einer Nullreihe (`MonatsSaeulen` mit `maxWert = 0`). Renderer unverändert, ChartProben 32/0.
>
> **Anwenderwunsch W8‑E‑1 und Befund W8‑B‑2 vom 05.09.2026 (Bildschirmfotos „Typ in DB ändern…" aus dem Standard-
> Stromprofil, „eigenes Lastprofil" im Assistenten, WinForms-Vorbild „Stromverbrauchertyp Stundenverteilung"),
> umgesetzt in `01dae1c`:** **W8‑E‑1 — Stundenverteilung in der Anordnung des Vorbilds.** Der Anwender wollte den Dialog
> „so wie zuvor"; die Razor-Fassung stapelte Typen, Wochentage und die 24 Stundenwerte untereinander und war dreimal
> so hoch wie `Form_EingStromTyp` (607 × 544). Zurückgeholt ist die Anordnung des Designers — Typliste links,
> Beschreibung rechts, Reiter darunter; im Wochenblatt drei Spalten zu acht Stundenwerten mit der Nummer vor dem
> Feld, rechts die Wochentagsliste mit „Tag kopieren"/„Tag einfügen", unten „Änderungen Übernehmen" samt Diskette,
> und die Fußleiste in der Reihenfolge Speichern unter | Speichern in DB | Löschen | Neu | Schließen. Übernommen ist
> die Anordnung, nicht das Pixelmaß: `--epos-touchziel` gilt weiter (acht statt neun Typzeilen im Rahmen), die
> Dreiteilung macht das Stilblatt (`grid-auto-flow: column`), im Markup laufen die Felder weiter 1…24, damit der
> Tabulatorweg bleibt. **W8‑B‑2 — ein belegter Typname meldet sich, statt zu werfen.** „Neu" mit vorhandenem Namen
> lief bis ins `INSERT` und endete in einem modalen „SQLite Error 19"-Kasten aus einem Blazor-Ereignis heraus
> (`TypProfilCtrl.Anlegen` prüfte nicht, der Wurf lief über `SqliteDatenzugriff` → `DataRepository.FehlerMelden` →
> `Dienste.Dialog`, Regel A‑8). `TypProfilCtrl.TypExists` prüft jetzt vorher, `Neu`/`SpeichernUnter` geben
> `TypAnlageErgebnis` statt `bool`, die Namensabfrage bleibt offen mit dem Warnbanner „Ein Typ mit diesem Namen ist
> schon vorhanden" (`BPRO_MSG_NAME_BELEGT`, de/en). Beide Aufrufwege — Überlagerung und Assistentenseite — zeigen
> dieselbe Komponente aus einem Parametersatz, alle drei Ausprägungen (Strom, Prozesswärme, Brauchwasser). Zwei
> Kern-Wachen, elf bunit-Fälle; Protokoll „Windows-Abnahme 05.09.2026 — Stundenverteilung".
>
> **Anwenderwunsch W8‑E‑2 und Befund W8‑B‑3 vom 05.09.2026 (Bildschirmfoto „monatlicher Verlauf…" aus dem
> Standard-Stromprofil: „max. Strombedarf 3,72 kW, Gesamter Strombedarf 0, Stromganglinie 0, Strombedarf Gebäude
> 0"), umgesetzt in `99033ce`:** Der Ergebnisdialog des Strombedarfs zeigte „Gesamter Strombedarf 0" und
> „Strombedarf Gebäude 0" neben einem gerechneten Spitzenwert (**W8‑B‑3**). Ursache war eine zweite, von Hand
> nachgezogene Fassung der Vorschaurechnung in `BedarfsProfileHuelle.Rechenstand`, aus der die Zeile
> herausgefallen war, die `Strombedarf_Gebaeude_gesamt` belegt — dieselbe Klasse Fehler wie W9‑B‑4/B‑5, nur eine
> Ebene weiter; weder Einheit noch Projektkopie waren beteiligt. Die sechs Zuweisungen stehen jetzt einmal im Kern
> (`SimulationStrombedarf.ProfilbedarfUebernehmen`, Zwilling von `ProzesssummeUebernehmen`), und die Projektvorschau
> ist als `BedarfsVorschauCtrl.ProjektVorschau` aus der Hülle in den Kern gezogen; Katalog- und Projektvorschau
> nehmen dieselbe Fassung, die Hülle hält den Stand nur noch. Zum Wunsch **W8‑E‑2** gliedert `Kennzahlart` das
> Blatt in drei Kategorien: die LEISTUNG („max. Leistung", vormals „max. Strombedarf") in einem eigenen Block und
> außerhalb der Summe, darunter die Posten, am Fuß abgesetzt die Summe; „Strombedarf Gebäude" heißt jetzt
> „Strombedarf aus Profil" und trägt den gerechneten Wert (8 000 kWh/a bei kWh, 8,00 bei MWh). Der Grafikreiter
> bekommt mit `BedarfGangGrafik` die Stufen Jahr | Woche | Tag samt Ringnavigator — ohne neues Renderer-Bild, über
> einen optionalen `Achsenfenster`-Parameter an `ChartRenderer.Jahresverlauf` (`jahresverlauf_bedarf` bleibt
> byte-gleich; ChartProben jetzt 36 Bilder + 4 Gegenproben). Die Wärmeausprägung ist konsistent mitgezogen und sieht
> ihren Grafikreiter unverändert. Kern 1 077, UI 2 491, Referenzlauf byte-gleich. Beim Merge mit W8‑E‑1 (`4724774`)
> fehlten zwei schließende Klammern aus der Konfliktauflösung; dabei fiel eine seit `91bac96` verwaiste
> Konfliktmarke vor dem Schema-Block auf, die als ungültiger Selektor die erste Regel des Blocks verschluckte —
> beide behoben, Klammerbilanz 739/739.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `BedarfsProfileDialog` (Block „Jahresverbrauch": zwei Felder in einer Zeile,
> „Übernehmen" darunter) und `TypStammDialog` (die zwölf Monatswerte in zwei Spalten zu sechs statt zwölf voller
> Zeilen) hängen im Raster; `TypProfilDialog` und `BedarfErgebnisDialog` bleiben, wie sie heute abgenommen wurden.

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
> **Anwenderentscheid #76 vom 05.09.2026 („#76: Empfehlung"), umgesetzt in `b6fd863`:** `SolarkollektorenDialog` und `SolarganglinieDialog` stehen im
> Baustein `Zweispaltenauswahl` (Anordnung unverändert, Klartext-Knöpfe, Umbruch unter 900 px). Die drei
> Wärmepumpenmasken bleiben auf `epos-auswahlpaar`, weil sie keine Projekt↔DB-Auswahl sind — geprüft und im
> Protokoll begründet.
>
>
> **Anwenderwunsch W7‑E‑1 vom 05.09.2026 („Admin-Menüs sind nicht an Größe Bildschirm angepasst"), umgesetzt in
> `ddf4d00`:** Die sechs Fenster der Welle öffnen im Anteil des Arbeitsbereichs (Hüllenregel iU8‑E‑1, 85 % × 90 %,
> gedeckelt auf 92 %); `SolarganglinieAdminDialog` stellt Liste und Eingabe nebeneinander (Baustein `Katalograhmen`).
> Die drei Katalogeditoren bleiben Überlagerung ohne volle Höhe, `WaermepumpenKatalogDialog` bleibt unverändert.
>
> **Formularraster, Paket P1 (iU8‑E‑2, 05.09.2026, `6b2a23f`; Anwenderbeispiel „Verwaltung BHKW"):** Fünf der sechs Masken sind umgestellt (`KennlinienEditorDialog` „Neue Stützstelle",
> `WaermepumpeStammDialog` zehn Felder, `WaermepumpeAnlageDialog` mit Kenndaten/Auslegung/Spitzenlast,
> `SolarkollektorKatalogDialog` 14, `SolarkollektorenDialog` 8); die Wärmepumpen-Anlage nutzt als erster Dialog des
> Hauses den benannten Rückweg `Einspaltig` — ihr Block „Spitzenlast" ist eine Regel, die sich von oben nach unten
> aufblättert. Der `WaermepumpenKatalogDialog` bleibt bewusst außen vor: seine zwölf Felder filtern eine Liste, sie
> beschreiben kein Gerät. `PufferSpProjektDialog` (W10a) ist in keinem Paket umgestellt — offen als iU8‑O‑1.

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
> **Windows-Abnahme 04./05.09.2026, Befund W6‑B‑1 (Hauptfenster als ungestyltes HTML):** Der Regel
> `.epos-mehrzeilig { white-space: pre-line;` fehlte das schließende `}`. Es ging **nicht** in W6.4a
> (`1bb2c19`, dort heil und letzte Regel des Blatts) verloren, sondern im Merge **`7e8e341`** (Welle 5 in
> Welle 6, 03.09.2026): beide Zweige hatten an dasselbe Dateiende angebaut, beim Auflösen blieb die eine
> Zeile liegen. Chromium las die 414 Blöcke dahinter (Reiter, Kachelraster, Zellenaktionen, Startseite,
> Menüband) als **verschachtelte** Regeln unter `.epos-mehrzeilig` — gültiges CSS, keine Meldung; die 155
> Blöcke davor (Dialoge, Knöpfe, Felder, Raster) waren nie betroffen, darum sahen die Dialoge der Wellen 6
> bis 15 in der Abnahme richtig aus und erst das Hauptfenster (W16c) fiel um. Auch der Stilblattteil von
> W5‑B‑1 war bis dahin wirkungslos. Klammer gesetzt (`aa98738`, Bilanz 619/619); Wache
> **`EPOS.UI.Tests/StilblattTests.cs`** (`5c9d95c`): eigener Strukturparser über jedes `.css` unter
> `EPOS.UI/wwwroot` — Klammerbilanz, keine Stilregel in einer Stilregel, kein `&`-Selektor, Zeile und
> Selektor in der Meldung, Gegenprobe mit entfernter Klammer; Bestandsaufnahme ohne weiteren Befund.
> Hausregel in `EPOS.UI/CLAUDE.md`, Herleitung in Protokoll W6 § 12.
> **Anwenderentscheid #76 vom 05.09.2026 („#76: Empfehlung"), umgesetzt in `b6fd863`:** die fünf Erzeugerdialoge (Heizkessel, BHKW,
> Photovoltaik, Pufferspeicher, Stromspeicher) stehen im neuen Baustein **`Zweispaltenauswahl`** — Anordnung
> unverändert nebeneinander wie `Form_Heizkessel` (316/88/313 px), neu sind der Klartext mit Kurztext auf den zwei
> Knöpfen (beide Sprachen, `AUSWAHL_BTN_*`) und der Umbruch untereinander unter 900 px (Token
> `--epos-zweispalten-umbruch`, Glyphen ◀▶/▲▼ je Breite). Protokollabschnitt „Anwenderentscheid #76", Abnahmepunkte
> A‑#76 (breit nebeneinander, schmal untereinander, Listen begrenzt, Knöpfe beschriftet und gesperrt ohne Markierung).
>
> **Formularraster, Paket P1 (iU8‑E‑2, 05.09.2026, `6b2a23f`; Anwenderbeispiel „Verwaltung BHKW"):** Sechs Masken der Welle hängen ihren Parameterblock ins `Formularraster`
> (`HeizkesselKatalogDialog` 21 Felder in vier Gruppen, `BhkwKatalogDialog` 26, `BhkwDialog`, `PhotovoltaikDialog` —
> der gestrichelte Anlagenrahmen bleibt, der Raster steht darin —, `StromspeicherDialog`, `PufferspeicherDialog`,
> dazu `HeizkesselDialog` aus #90 nachgezogen); die handgebauten `epos-feldpaar`-Wirte sind in `Dialoge/Erzeuger/`
> restlos verschwunden. Der Detailblock des BHKW-Projektdialogs — das Beispiel des Anwenders — steht jetzt in drei
> bis fünf Zeilen statt sieben: Name und Hersteller in der Feldspalte, thermische und elektrische Leistung kurz
> nebeneinander, Beschreibung über beide Spalten, darunter Brennstoff und die drei kurzen Felder Grenzleistung,
> Vorlauf, Rücklauf; die Summenzeile unter der linken Liste ist einspaltig kompakt. Eine `Herleitungszeile` im Raster
> spannt seither über alle Spalten, sodass die Kostengruppe des BHKW-Katalogs ein Raster bleibt. Neu hausweit:
> `Textfeld.Kurz` und `ErzeugerDetail.IstZahl` — welches nur lesbare Anzeigefeld kurz ist, entscheidet sich an einer
> Stelle am Wert, nicht an der Beschriftung. Nicht umgestellt: die Spalte „Eigenschaften" der BHKW-Datenbankliste
> (vier Zeilen je Zelle) kommt aus `BhkwHuelle.KatalogZeilen` in der Windows-Hülle — als offener Punkt im W6-Protokoll.
>
> **Zusammenführung Rechner 2 am 05.09.2026 (`12aa3a5`):** Auf dem zweiten Rechner des Anwenders lief seit dem
> 02.09.2026 eine eigene Entwicklungslinie, sechsmal mit `origin/ios_migration` zusammengeführt und jedes Mal mit
> Referenzlauf-Nachweis (M1–M5, 355/355 byte-gleich zur jeweiligen Vorstufe); sie kam als Zweig
> `pv-ertragsmodell-rechner2` (28 Commits auf Basis `ed71d73`, Tip `d331823`) nach GitHub und ist hier mit **einem**
> Konflikt (`epos-ui.css`, beide Seiten hatten Blöcke ans Dateiende gehängt) zusammengeführt. Inhalt: das
> **PV-Ertragsmodell** Paket A (Zeitbasis UTC→Ortszeit der `Tab_Solar`-Leser, Anlagenparameter
> WR-Wirkungsgrad/Systemverluste, Migration 62) und Paket B (Rechenmodell ERWEITERT mit Hay-Davies, Huld,
> WR-Kennlinie, Clipping, Degradation; Modellwahl im PV-Dialog über `PvModellFelder`; Datenmodell PV_Modell/
> Wechselrichter/Technologie, Migration 63 — EINFACH unverändert), die **PV-Katalog-Koeffizienten** (Import CEC/PAN,
> `PvModulPlausibilitaet`, Reparaturskript unter `sql/pv_katalog/`), die **Projektdialoge** (Löschen mit Mehrfachauswahl,
> Öffnen, Neues Projekt) und die **Projektstammdaten** (Datumspflege, Kunde/Bearbeiter, `ProjektKopfSeite`), dazu
> **FS1 N‑1** (AnkerNachziehen unter SQLite: Access-JOIN-UPDATE durch korreliertes UPDATE ersetzt). Konzepte:
> `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`, `Konzept_Projektstammdaten_EPOS-Plan.md`; Protokolle unter
> `WindowsFormsApplication1/Allgemein/Simulation/` (PaketA, PaketB, FS1, Merge 1–5); acht Referenzlauf-Ordner
> `Referenzlaeufe/2026-09-0x_*` (je 356 Dateien) als Nachweise mit `LIESMICH.md`. Gate hier: Bau 0 Fehler, Tests 469 + 337 + 2 634 + 1 168 grün (serialisiert), Formularkarte 122, SQL-Prüfer 0 Fundstellen, ChartProben 36 Bilder grün. Betroffene
> Wellen: W3 (`PhotovoltaikVerguetungDialog`), W6 (`PhotovoltaikDialog`), W13 (`PvModulImportDialog`), W14a
> (`ModulKatalogDialog`), W15a (`ProjektWahlDialog`), W16a (`ProjektKopfSeite`). **Nachzug `a738d08`:** Die
> Rechner-2-Linie brachte die Migrationen 62–64 mit, die Testdatenbank `Referenzlaeufe/Kenndaten_Test.sqlite` stand
> aber auf Schemastand 61 — der SQL-Prüfer meldete deshalb neun Spaltenfehler in PV- und Projektabfragen, dazu eine
> Prüferlücke (dynamischer `SELECT` ohne `FROM` in `WizardCtrl.FachspaltenSelect`). Neues Werkzeug
> `Werkzeuge/Testdatenbankschema` zieht die Testdatenbank idempotent auf den Stand des `SchemaKatalog` (`--trocken`
> zeigt die anstehenden Schritte), Regel „Testdatenbank mitziehen" in `BETRIEB_SQLITE.md` 6.5; Prüfer 0 Fundstellen.
> **Referenzbasis:** Der Referenzlauf weicht seit dieser Zusammenführung für 1030, 1007 und 1017 von
> `2026-08-30_B3-Kaskade` ab — genau die Paket-A-Verschiebung der Solar-Zeitbasis von UTC auf Ortszeit, die auf
> Rechner 2 als PA0→PA1 dieselben Zahlen zeigt (1007 und 1017 hier byte-gleich zu deren PA1/PB1/M5). Ob die Basis
> neu eingefroren wird, war dem Anwender vorgelegt — **Entscheid 05.09.2026: ja.** Neue CI-Basis
> `Referenzlaeufe/2026-09-05_R2_Zeitbasis` aus `EPOS.Referenzlauf` auf Linux: elf Projekte, 282 CSV (1011 und 1021 der
> B3-Basis stehen nicht in `Kenndaten_Test.sqlite`), zweiter Lauf byte-gleich; gegen die Windows-Basis M5 von Rechner 2
> sind sechs der acht gemeinsamen Projekte byte-gleich, 1030 und 1039 tragen die schon zwischen B3 und PA0 bekannte
> Umgebungsdifferenz des zweiten Rechners. `kern.yml`, das Gate und `CLAUDE.md` halten seither gegen R2_Zeitbasis;
> `2026-08-30_B3-Kaskade` bleibt zur Geschichte liegen.
>
> **Anwenderwunsch W6‑E‑1 vom 05.09.2026 („optional sollten beim ausgewählten PV-Modul alle Eigenschaften/Parameter
> angezeigt werden"), umgesetzt in `87191a8`:** Der Block „Modul Eigenschaften:" zeigte vier der neunzehn Spalten von
> `Tab_PV_STAMM`; die übrigen dreizehn standen nur im Katalogdialog — dort, wo man ein Modul ÄNDERT, nicht dort, wo man
> es AUSWÄHLT. Unter dem Block steht jetzt ein Aufklapper „Alle Modulparameter anzeigen" (`PVD_AUFKLAPP_PARAMETER`,
> de/en), zugeklappt als Vorgabe, nur lesend, im `Formularraster` mit Einheit hinter dem kurzen Feld. Es ist ein Knopf
> mit `aria-expanded` und kein `<details>`: Nur so gehört der Offen-Zustand dem Dialog und übersteht den Wechsel des
> gewählten Moduls. Beschriftung und Einheit kommen aus `ModulKatalogProfil` (Ausprägung Photovoltaik) — derselben
> Quelle wie der Katalogdialog —, die zwei Temperaturkoeffizienten aus dem Modulimport (`PVIMP_LBL_ALPHA_ISC`/
> `_BETA_VOC`), die der Katalog nicht führt; neu ist genau EIN Anzeigetext. Gelesen wird im selben Vorgang:
> `PhotovoltaikStammCtrl.Detail` trägt seither alle Spalten (4 → 17), und weil der Dialog ihn bei jeder
> Auswahländerung ruft, zieht der Block von selbst nach. Nicht gepflegt heißt „–", nicht 0 — NULL und die 0 des
> Bestands sind dieselbe Aussage; ein unbekannter Technologiecode bleibt sichtbar. `Textfeld` bekommt dafür `Einheit`
> wie `Zahlenfeld`. Tests: `PvModulparameterTests` 12 neu, `PhotovoltaikDialogTests` 14 → 21, beide Reihen unter de
> und en grün; SQL-Prüfer 0 Fundstellen, Kern-Wächter leer. Beobachtung W6‑O‑5: Die Einheit „[KW]" an Modul- und
> Gesamtleistung war bestandstreu aus `Form_PV` übernommen, sachlich aber Watt (der Katalog nennt denselben Wert
> „Nennleistung (Pmax)" in W, `AnlagenKwp` teilt durch 1000). Zehn Abnahmepunkte A‑W6‑E‑1 im W6-Protokoll.
>
> **Anwenderentscheid W6‑O‑5 vom 05.09.2026 („Gesamtleistung in kW"), umgesetzt in `d534af4`:** Der PV-Projektdialog
> zeigte zwei Leistungen unter der Beschriftung „[KW]", die beide Watt waren — `Tab_PV.Leistung` führt die
> Modulleistung in Watt, und die Gesamtleistung war deren rohes Produkt mit der Modulanzahl; zehn Module ergaben
> „2751,912 KW". Seither heißt das Modulfeld „Modul Leistung [W]" und die Gesamtleistung „Gesamtleistung [kW]" mit drei
> Nachkommastellen („2,752"); der englische Text „Total power [kW]" sagte die Einheit als einziger schon richtig und
> bleibt. Die Wandlung steht als `PhotovoltaikCtrl.GesamtleistungText` im Kern neben `KwpSumme` — eine kWp-Wahrheit,
> ohne Windows nachweisbar — und ist reine Anzeige: Der Rechenweg (`AnlagenKwp`, `KwpSumme`, Simulation,
> Wirtschaftlichkeit) ist unberührt, der Referenzlauf bitgleich. Nachweis: Kern 1 209 → 1 213, UI 2 656 → 2 659, beide
> grün unter de und en; Windows-Bau 0 Fehler; Kern-Wächter leer. Vier Abnahmepunkte A‑W6‑O‑5 im W6-Protokoll.
>
> **Anwenderwunsch W6‑E‑2 vom 06.09.2026 („Wechselrichter – ausgegraut. Import liegt nicht vor, Admin zum
> Anlegen/Bearbeiten liegt nicht vor … Mockup und Konzept vor Umsetzung"), Konzept und Mockup in `8fee437`:** Der Knopf
> trägt genau eine Sperrbedingung (`PvModellFelder.razor`: `disabled`, solange das Modell nicht ERWEITERT ist) und ist
> im Modell EINFACH bestimmungsgemäß gesperrt; EINFACH multipliziert den Ertrag mit dem konstanten Faktor
> `PV_WrWirkungsgrad` (NULL = 0,95, `SimulationPV`) — ohne Clipping, Kennlinie und AC-Nennleistung. Nachgeprüft fehlen
> Wechselrichtertabelle, Katalogeintrag, Verwaltungsausprägung, Import, Strangbegriff und Menüpunkt vollständig; die
> Modulkennwerte für eine Auslegungsprüfung liegen seit W6‑E‑1 ungenutzt im Katalog. Das neue Papier
> `Konzept_Wechselrichter_EPOS-Plan.md` (982 Zeilen) schlägt `Tab_Wechselrichter_STAMM` mit Projektkopie, die
> Strangzuordnung `Z_AnlageStrang` (Migrationsschritte 65/66), eine Kennlinie aus sechs Stützstellen mit
> mitgeschriebenen Sandia-Koeffizienten, den CEC-Wechselrichterimport und den Rechenweg Module → Strang → MPPT → Gerät
> → Clipping mit acht Auslegungsprüfungen vor; ohne Strangzuordnung bleibt der Rechenweg Zeichen für Zeichen
> erhalten, die Basis `2026-09-05_R2_Zeitbasis` also byte-gleich. Vorgeschlagen sind drei Stufen (S1
> Katalog/Verwaltung/Import ohne Rechenwirkung sofort, S2 und S3 zusammen). Das Mockup
> `Mockups/Wechselrichter_Mockup_2026-09-06.html` (1 439 Zeilen, eigenständig, Hausstil) zeigt vier Ansichten:
> PV-Dialog mit dem Abschnitt „Wechselrichter und Stränge" samt Plausibilitätsampel, Verwaltung, Import und den
> Rechenfluss als SVG. **Nichts umgesetzt; zehn Entscheidungsfragen W6‑E‑2‑Q1…Q10 liegen beim Anwender**, darunter
> Kennlinienform (Empfehlung Stützstellen, weil Sandia die DC-Spannung je Stunde bräuchte) und ob der Wechselrichter
> auch in EINFACH wirkt (Empfehlung ja — damit entfällt der ausgegraute Knopf).

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
> **Windows-Abnahme 04.09.2026, erster Befund (Kosten-Seite):** die Aktionsspalte der Anlagentabelle war unsichtbar
> (`display:flex` direkt am `<td>` nahm der Zelle ihre Tabellenrolle, der Spaltenkopf war leer — W5‑B‑1) und der
> Doppelklick auf der losen Position fehlte. **W5‑O3 entschieden: der Doppelklick ist als zweiter Weg zurück, der
> Knopf bleibt**; Aktionsspalte als gewöhnliche Zelle mit beschriftetem Kopf (`5f153f1`, sechs bunit-Fälle, darunter eine
> Wache auf das Stilblatt; Regel in `EPOS.UI/CLAUDE.md`: kein `display:flex` auf `<td>`/`<th>`, Aktionsknöpfe ohne
> Hover sichtbar). Die Sichtprüfung in der WebView2 bleibt beim Anwender.
> **Anwenderwunsch W5‑E‑1 vom 05.09.2026 („Variantenprojekte-Auswahl als Dropdown, damit weniger Platz verwendet
> wird"), umgesetzt in `06332f2`:** Die Variantenwahl der Übersichtsseite ist ein `Auswahlfeld` „Variante:" (Stamm
> zuerst, dann „Bezeichner — Projektname", Id = `Tab_Projekt.ID`) statt einer Tabelle; Bezeichnerfeld und die drei
> Knöpfe stehen mit ihm in einer Zeile (Umbruch auf schmalem Fenster), der Simulationsstand darunter als leise
> `aria-live`-Zeile „Simulation: ‹Datum›" bzw. „noch nicht simuliert" mit dem ⚠ als eigenem Element und dem Grund
> im Kurztext (`BerichtsDatenSammler.ErmittleStatus`: kein Ergebnis oder Ergebnis älter als das Änderungsdatum), und
> die Unterschiedstabelle bekommt die frei gewordene Höhe (`epos-raster-huelle--vergleich`, 35,2 rem; Hausregel
> W9‑B‑2 bleibt). Neu dafür `VarianteZeile.SimZeitpunkt` und `Auswahlfeld.Kurzname`; die Parameter
> `SpalteArt`/`SpalteBezeichner`/`SpalteProjektname` entfallen. Protokoll Abschnitt 13, zehn neue bunit-Fälle.
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** Seiten tragen ihre Felder in Werkzeugzeilen und Tabellenspalten; genau eine Stelle — das
> Pfadfeld des Zielordners der `BerichtSeite` — ist ein Formularfeld und steht einspaltig im Raster. `UebersichtSeite`
> und `WirtschaftlichkeitSeite` bleiben unverändert und sind zugleich die Gegenprobe, dass die Regel nur innerhalb
> `.epos-formularraster` greift. Paket P2: 16 Dateien, UI 2 562 (+16), Formularkarte 122.
>
> **Anwenderbefund W5‑E‑2 vom 05.09.2026 („Gewerk Anlage gibt es nicht. Dort stehen Parameter. Dargestellt werden nur
> die Erzeugerkomponenten, die verwendet werden, keine Parameter"), umgesetzt in `7dcda25`:** Die Gegenüberstellung der
> Seite „Übersicht" lief über `AbweichungsErmittler.Felder` und nahm damit die Blöcke „Anlage" und „Gebäude" mit —
> Konfigurationsblöcke ohne Komponentenbestand; im Projekt des Bildschirmfotos (1042 „Booster-Kette mit
> Kombi-Speicher" mit Variante 1044 „Schichtspeicher") waren das 21 Anlagen- und 4 Gebäudemerkmale über 10
> Komponentenzeilen. Das Vorbild ist nachgesehen: Die gelöschte Maske `UcBkUebersicht` zeigte den Block ebenfalls (der
> Wächter `AnlagenEinheitlich` greift nur bei verschiedenen Anlagengewerken), schon in ihrer ersten Fassung — das
> wirkliche Vorbild ist der Berichtsbaustein `BausteineProjekt`, der seit jeher allein über
> `ProjektDetails.GewerkTabellen` zählt. Die Zeilenbildung zieht deshalb in den Kern: `Allgemein/Bericht/
> KomponentenVergleich.cs` mit dem anzeigefreien `KomponentenVergleichZeile` liefert je verwendetem Erzeugergewerk eine
> Kopfzeile „Anzahl Komponenten" und darunter eine Zeile je Komponente; ein Gewerk mit Stückzahl 0 in allen Versionen
> erscheint gar nicht. `UebersichtSeiteGaben.FuelleVergleich` schrumpft von 88 auf 10 Zeilen und bildet nur noch ab.
> Die Unterschiedsansicht einer Variante bleibt vollständig — dort zeigt eine Zeile eine Änderung und trägt die
> Merkmalsübernahme; `AbweichungsErmittler` ist nicht angefasst, der Referenzlauf unberührt. Nachgezogen ist ein Text
> (`BK_MSG_VERGLEICH_UMFANG`: „Komponentenzeile(n)" statt „Merkmalszeile(n)", de/en). Nachweis:
> `KomponentenVergleichTests` (7 Fälle) und ein bunit-Fall in `UebersichtSeiteTests`; Kern 1 197 und UI 2 649 grün unter
> de und en, SQL-Prüfer 0 Fundstellen, Kern-Wächter leer. **W5‑O‑4 — Anwenderentscheid 05.09.2026: „soll bleiben".**
> Die Unterschiedsansicht einer Variante zeigt weiterhin alle Abweichungen einschließlich der Anlagen- und
> Gebäudeparameter, weil eine Zeile dort eine tatsächliche Änderung ist und die Übernahme (z. B. einer geänderten
> Vorlauftemperatur) daran hängt. Acht Abnahmepunkte A‑W5‑E‑2 im W5-Protokoll.

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
> **Windows-Abnahme 04.09.2026, Anwenderwunsch W4‑E‑1 (Energieträgerverwaltung):** Suche und Filter in der
> Trägerauswahl wie in den Importdialogen — Filterfeld über der Liste mit demselben Kernbaustein `VdiAuswahlFilter`
> (Teilzeichenkette, Groß-/Kleinschreibung egal, mehrere Begriffe UND-verknüpft, Bezeichnung und Gruppe), Gruppenköpfe
> nur über Treffern, der gewählte Träger übersteht den Filterwechsel, Pfeiltasten wandern über die Treffer; der Baustein
> `Zeilenwahl` um `Beschriftung`/`Zusatzklasse` erweitert, die 19 Bestandsaufrufe unverändert (`80e73c7`, neun bunit-Fälle,
> Abnahmeprobe W4‑19).
> **Windows-Abnahme 04.09.2026, Befund W4‑B‑1 (Preisbasis doppelt oder leer):** die Hülle baute die Preisbasen aus der
> Zieleinheit jeder Umrechnungsregel ohne Dublettenprüfung (Nm³→Nm³ und m³→Nm³ ergaben Nm³ doppelt, 8 der 27 Träger),
> ohne Regeln blieb das Feld leer (5 Träger), ohne Treffer fiel die Wahl still auf Index 0 — wortgleich vom Vorläufer
> `ucFuelSettings` übernommen. Der Listenaufbau liegt jetzt datenbankfrei im Kern (`EnergietraegerPreisCtrl.Preisbasen`:
> Abrechnungseinheit zuerst, Zieleinheiten in Regelreihenfolge, jede genau einmal, normalisiert verglichen; die Id
> indiziert die bereinigte Liste); m³ ist bewusst keine Preisbasis (nur Quelle des z-Faktors, L4). 14 Kern-Fälle, ein
> bunit-Fall, Referenzlauf byte-gleich, keine Datenzeile berührt (`cac4a1d`, W4-Protokoll § 9a).
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** `StromAufschlaege`, `EnergietraegerDialog`, `EnergietraegerEinstellungen`, `ErtragBonus`: drei
> handgebaute `epos-feldpaar` entfallen — der Raster misst die eigene Breite und legt unter `--epos-formularspalte`
> selbst auf eine Spalte um. Preisblock (Schalter + Wert + Schnellwahl), Datenraster und das Suchfeld der Trägerliste
> bleiben, wo sie waren; der Übernahmeknopf des Energieträgers steht in einer `epos-leiste`.

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
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** Vier Dialoge (`SpotpreisImportDialog` einspaltig wegen des Pfadfelds, `KostenprofilDialog`,
> `EmissionskatalogDialog`, `GesetzeskatalogZeileDialog`), davon drei mit geteiltem Raster: Der Satz zwischen den Feldern
> behält die volle Zeile, die Beschriftungskante läuft über beide Raster durch. Die Wertetafeln (12 Monats-, 24
> Stundenwerte) und Datenraster bleiben, was sie sind.

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
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** Die drei Wirtschaftlichkeitsmasken (`TarifstrukturDialog`, `PhotovoltaikVerguetungDialog`,
> `WirtschaftlichkeitParameterDialog`) sind der eigentliche Anlass des Pakets: 53 Felder, vierzehn Raster, sechs
> `Formulargruppe`n (erster echter Einsatz; die vier zur Laufzeit gebauten Untergruppen der Tarifstruktur und die Gruppe
> „Bilanz"). Der Block „Rollenmodell" war der höchste des Hauses und halbiert sich, weil alle Stufenfelder kurz sind.
> Beim Merge mit der Rechner-2-Linie blieb das Degradationsfeld (Paket B) im Raster — sein Tooltip-Wirt ist durchsichtig.

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
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** Die drei Kostenvorlagen-Kleindialoge (`CaseEingabeDialog`, `VorlagenPositionDialog`,
> `VorlagenUebernahmeDialog`) und der BHKW-Wirtschaftlichkeitsdialog tragen den Raster; die Vorlagenübernahme ist die
> Stelle, an der `Einspaltig` seinen Namen verdient — eine Kette von Wahlen bleibt eine Kette, die Beschriftung steht
> trotzdem neben dem Feld. `VorlagenZeile` bleibt Bearbeitungszeile in der Tabelle.

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
| **iU10-CI** | Achter Lauf `ios.yml` (33748736894): Workload 23 s, Bau 57 s, Simulator, Erststart 73 MB, `SQLite 3.53.3`, `STRICT=114`, `Projekte=23`, Prüfmodus 5 s, **iZ6-Vergleich 1030 PASS und byte-gleich** | ✅ CI macOS, 5 min 44 s · **Neunter Lauf** (33785012663, 03.09.2026, 9 min 52 s) auf `f1d387b` nach W5/W6: grün · **Zehnter Lauf** (33809247370, 03.09.2026, 4 min 53 s) auf `21ab680` nach W7–W9: grün · **Elfter Lauf** (33826084944, 04.09.2026, 8 min 50 s) auf `a398c9a` nach W10a/W10b: grün · **Zwölfter Lauf** (33832613617, 04.09.2026, 9 min 02 s) auf `43fb9c3` nach W11a/W11b: grün · **Dreizehnter Lauf** (33838762108, 04.09.2026, 6 min 30 s) auf `62b3457` nach W12: grün · **Vierzehnter Lauf** (33844935661, 04.09.2026, 10 min 04 s) auf `29aecbc` nach W13: grün · **Fünfzehnter Lauf** (33852944072, 04.09.2026, 7 min 24 s) auf `ecd6cfe` nach W14a/W14b: grün · **Sechzehnter Lauf** (33861268537, 04.09.2026, 9 min 28 s) auf `0cc1495` nach W14c: grün · **Siebzehnter Lauf** (33867643966, 04.09.2026, 10 min 30 s) auf `c11f13d` nach W15a: grün · **Achtzehnter Lauf** (33876284942, 04.09.2026, 2 min 26 s) auf `f71853b` nach W15b: **rot** (CS0103 `IosHilfeDienst`, nur der macOS-Läufer übersetzt die iOS-Hülle; behoben `f0e23a4`) · **Neunzehnter Lauf** (33878903371, 04.09.2026, 6 min 27 s) auf `f0e23a4`: grün · **Zwanzigster Lauf** (33883210632, 04.09.2026, 10 min 14 s) auf `975ead5` nach W15c: grün · **Einundzwanzigster Lauf** (33890882150, 04.09.2026, 8 min 03 s) auf `84d7c16` nach W16a: grün · **Zweiundzwanzigster Lauf** (33898599945, 04.09.2026, 7 min 56 s) auf `c8fbd77` nach W16b: grün · **Vierundzwanzigster Lauf** (33904433007, 04.09.2026, 8 min 43 s) auf `555ef11` nach W16c: grün (Nr. 23 war eine abgebrochene Dublette) · **Fünfundzwanzigster Lauf** (33913313694, 04.09.2026, 6 min 29 s) auf `853b8c6` nach den W16-Nachträgen (E‑2/E‑3, LizenzTexte, W16b‑O‑3): grün · **Sechsundzwanzigster Lauf** (33975880961, 05.09.2026, 6 min 14 s) auf `7bec4ad` nach den Abnahmebefunden vom 05.09. (Baustein `Diagramm`, `epos-diagramm.js` über `import()`): grün · **Siebenundzwanzigster Lauf** (33982889724, 05.09.2026, 10 min 51 s) auf `c563a40` nach den sechs Nachmittagsbefunden vom 05.09. (W13‑B‑1: `…Async`-Zwillinge in `IDateiDienst`/`IDialogDienst`, Fehlerschranke `Wurzel<T>` in `EPOS.iOS/HauptSeite`; W9.8, W15a‑E‑1, W16b‑E‑7, iU8‑E‑1): grün · **Achtundzwanzigster Lauf** (33992594094, 05.09.2026, 6 min 53 s) auf `6eddd27` nach der Zusammenführung der Rechner-2-Linie: Bau, Start, Prüfmodus grün, **iZ6-Vergleich rot**, weil `ios.yml` noch gegen `2026-08-30_B3-Kaskade` hielt (8 711 Abweichungen = Paket-A-Zeitbasis; Basiswechsel `37dfebb`, Workflow `e3fd980`) · **Neunundzwanzigster Lauf** (33993379551, 05.09.2026, 6 min 32 s) auf `e3fd980` gegen `2026-09-05_R2_Zeitbasis`: grün, PASS und byte-gleich (236 670 Werte, `diff -rq` leer; die Simulation meldet die Paket-A-Zeitbasis „Klimadaten: UTC → MEZ/MESZ, Referenzjahr 2025") |
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
| **iR12** | **WebView2 offline.** Das Setup nimmt heute den **Online**-Bootstrapper mit (iU8-10). Ein Kunde ohne Internet bekommt die Laufzeit nicht | **Seit iU9-W15c ist der Schaden nicht mehr graduell, sondern absolut:** Erststart und Lizenzzustimmung laufen über eine Blazor-Hülle, beide enden bei „false“ — **die Anwendung startet gar nicht mehr** (Befund W15c-B10). Bis dahin blieben nur die Dialoge leer | Anwenderentscheid **iF20**: Standalone-Installer (~150 MB) beilegen oder Fixed Version verteilen. Das Setup meldet den Fehlschlag bereits (`WebView2Fehlt`) und bricht nicht ab. **Seit iU9-W15c.6a prüft `Program.Main` die Laufzeit selbst** (`CoreWebView2Environment.GetAvailableBrowserVersionString()` in `try/catch`) und meldet ihr Fehlen mit der Bezugsquelle, statt den Anwender vor einem leeren Fenster stehen zu lassen — Entscheid W15c-E-8, Weg 2 |
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
| **iF30** | **Lesemodus-Durchsetzung:** `LizenzManager.DarfSchreiben()` hat bis heute genau einen Leser (`KiAusfuehrer.Schreibrecht`, W15c‑B7); weder Simulation noch Projektanlage noch ein Speicherweg fragt den Lizenzzustand. Wann und wo wird der Lesemodus durchgesetzt? | **Nach W16** — angelegt 04.09.2026 mit W15c (E‑9): dann sind alle Speicherwege Razor und ihre Zahl steht fest; dazu die Warnstufen 30/14/7 Tage vor Ablauf (Lizenzkonzept § 6). Bis dahin ist der Zustand **sichtbar** (sechs Zustände, drei Stufen, Detailzeile) und **prüfbar** (19 Kern-Fälle `LizenzZustandTests`), aber nicht durchgesetzt. **Entschieden 04.09.2026 (Empfehlung angenommen): streng — alle Schreibwege und der Simulationslauf werden über die eine Schreibnaht im Kern gesperrt, Ansehen und Berichte bleiben frei, Banner in der `AppWurzel`, Warnstufen 30/14/7 Tage vor Ablauf; Ausnahmen Erststart-Migration, Lizenzaktivierung, Einstellungen; eigene kleine Welle nach der Windows-Abnahme** |

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
