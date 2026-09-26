# Umsetzungskonzept: EPOS-Plan auf iOS

**Rev. 2.2 — 03.09.2026 — Stand nach der Kette iU4…iU8**

Basis: Branch `sqlite`, Stand `6486c36` (02.09.2026) — **die Access-Ablösung ist vollzogen**.
Rev. 1 war gegen `main` (`7d41833`) vermessen, als die Datenschicht noch Access trug; die daraus
folgenden Änderungen sind in § 1.4 und § 5 eingearbeitet.
Referenzbasis der Nachweise: `Referenzlaeufe\2026-08-30_B3-Kaskade` (13 Projekte, 332 CSV, Schemastand 61).
Sämtliche Zahlen dieses Dokuments sind am 02.09.2026 gegen den Arbeitsbaum nachgemessen; Abweichungen
zu den Vorgängerdokumenten sind in § 1.5 einzeln ausgewiesen.

> **Verhältnis der Dokumente**
> [`Konzept_iOS-Portierung_EPOS-Plan.md`](../ueberholt/Konzept_iOS-Portierung_EPOS-Plan.md) (Rev. 1, 30.08.2026)
> beantwortet **was und warum** — Leitentscheidungen iL1–iL8, Modell C, Migrationsregeln M1–M10,
> Arbeitsblöcke A–E. Es bleibt gültig und wird hier **nicht wiederholt**, sondern zitiert.
> [`Konzept_DB-Migration_SQLite_EPOS-Plan.md`](../ueberholt/Konzept_DB-Migration_SQLite_EPOS-Plan.md) (Rev. 2,
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

**Wo der Stand steht.** Dieses Kapitel hält den Befund der Umsetzungsprüfung vom 02./03.09.2026
fest — also die Ausgangslage, gegen die geplant wurde. Was seither wann mit welchem Ergebnis
umgesetzt wurde, steht in [`Status_iOS_Migration.md`](Status_iOS_Migration.md); die vollständigen
Statusblöcke bis zum 12.09.2026 stehen in
[`../ueberholt/Protokolle/Statusbloecke/Umsetzungskonzept_iOS_Statusbloecke_bis_2026-09-12.md`](../ueberholt/Protokolle/Statusbloecke/Umsetzungskonzept_iOS_Statusbloecke_bis_2026-09-12.md).

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
| `EposSqliteMigrator.Kern` und `EposSqliteMigrator` (Konsole) | `net8.0` | ohne Support — Werkzeug am 13.09.2026 aus dem Repository entfernt, Access-Übernahme am 24.09.2026 eingestellt |
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
[`Umsetzung_iU0_iU1_Nachweise.md`](../ueberholt/Umsetzung_iU0_iU1_Nachweise.md) aufgelistet.

**Rev. 2.2 — Revalidierung nach iU4, iU5, iU6, iU7 und iU8**, Branch `ios_migration` @ `f95fc34`
(03.09.2026). Nachgeführt sind: das Zielbild der Solution in § 2.1 samt Statusspalte (a), die
Build-Matrix in § 3.6 (b), der Stand der fünf Pakete (er steht heute in
[`Status_iOS_Migration.md`](Status_iOS_Migration.md)) und die neue Gesamtübersicht an seinem
Anfang (c), die Meilensteine iZ4 und iZ5 in § 4.1 (d), die Etappenübersicht in § 5.1 (e), die
Nachweistabelle „hier geführt / auf Windows offen" in § 6 (f) und die Risikoliste in § 7 —
vier entschärfte, vier neue (g). Alles Übrige der Rev. 2/2.1 bleibt unverändert gültig.
**Sämtliche Nachweise dieser Kette sind auf Linux geführt**; die Windows-Abnahme steht aus und
ist je Commit abhakbar in [`Umsetzung_iU0_iU1_Nachweise.md`](../ueberholt/Umsetzung_iU0_iU1_Nachweise.md)
und [`Umsetzung_iU8_Nachweise.md`](../ueberholt/Umsetzung_iU8_Nachweise.md).

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
| `EposSqliteMigrator.Kern` | Klassenbibliothek | `net10.0` | AnyCPU | — | **entfernt** (13.09.2026, Sync-Commit `43aaf985`; Access-Übernahme am 24.09.2026 eingestellt, BETRIEB_SQLITE.md 1.1/7) — war Windows-Werkzeug (las `.accdb` über OleDb), nie Teil des iOS-Pfads |
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
| Designer-Datei der Ressourcen | Visual Studio erzeugte `Resource.Designer.cs` über den Code-Generator der `.resx` | **`Werkzeuge/ResourceDesigner/designer_neu.py` ist seit 06.09.2026 der einzige Generator** (`abbb057`); der VS-Generator ist im `.csproj` abgeklemmt (`0785680`) — die vom Studio neu geschriebene Datei blockierte beim Anwender als „lokal geändert" jeden `git pull` |
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

**Der Stand der Umsetzung steht nicht in diesem Dokument.** Was wann mit welchem Ergebnis umgesetzt
wurde, führt [`Status_iOS_Migration.md`](Status_iOS_Migration.md) je Paket, je Welle und je Auftrag
in einer Zeile; die vollständigen Statusblöcke bis zum 12.09.2026 stehen in
[`../ueberholt/Protokolle/Statusbloecke/Umsetzungskonzept_iOS_Statusbloecke_bis_2026-09-12.md`](../ueberholt/Protokolle/Statusbloecke/Umsetzungskonzept_iOS_Statusbloecke_bis_2026-09-12.md).
Dieser Abschnitt sagt nur noch, **was** die Pakete sind und in welcher Ordnung sie stehen.

### 4.0 Gesamtübersicht

| Paket | Gegenstand | Größe | Ort | Voraussetzung |
|---|---|---|---|---|
| **iU0** | Klärung, Sicherung, Rückbau | S | Windows | — (Vorstufe zu allem) |
| **iU1** | Entwicklungsumgebung Stufe 1: .NET 10, Windows und CI | M | Windows | iU0 |
| **iU2** | Entwicklungsumgebung Stufe 2: Mac und Apple-Konto | S–M | Mac | iU1 |
| **iU3** | Machbarkeits-Spike (Gate iZ3) | M | Mac/CI | iU1, iU2 |
| **iU4** | `EPOS.Kern` herauslösen | L | Windows | iU3 bestanden |
| **iU5** | Statics kappen, Dienste einziehen | L | Windows | iU4 |
| **iU6** | Datenzugriff plattformfrei | S–M | Windows | iU4 |
| **iU7** | Charts und Berichte plattformfrei | M | Windows | iU4 |
| **iU8** | `EPOS.UI` und der erste Blazor-Dialog (Modell-C-Stichtag) | L | Windows | iU5, iU7 |
| **iU9** | Masken-Umwandlung in Wellen W0–W16c | XL | Windows | iU8 |
| **iU10** | Die iOS-Hülle `EPOS.iOS` | L | Mac | iU6, iU8 |
| **iU11** | Kataloge, Importe, Feinschliff | M–L | Mac | iU10 |
| **iU12** | Absicherung und Betrieb | M | beide | — |
| **iU13** | TestFlight und Vertriebsweg | S–M | Mac | iU11, iU12 |

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

**Voraussetzung:** iU1, iU2. **Entspricht Grundlagen-S0.** **Baustein:** iE6.

Das ist der Beweis, für den das ganze Vorhaben vorne klein gehalten wird.

| Inhalt | Detail |
|---|---|
| Datenstand | **liegt vor** — `Kenndaten.sqlite` als Werksvorgabe; Kundenbestände aus Access gibt es nicht (Übernahme am 24.09.2026 eingestellt). Daraus die CI-Testdatenbank mit den 13 Referenzprojekten schneiden |
| Kernauszug | **nur** `BhkwPlan`, `SimulationControl` und deren zwingende Abhängigkeiten — als Wegwerf-Auszug, nicht als `EPOS.Kern`. Ziel ist Erkenntnis, nicht Bestand |
| Rechenlauf | Projekt 1030 headless im iPad-Simulator **und** auf `macos-latest` |
| Vergleich | Ergebnis-CSV gegen `2026-08-30_B3-Kaskade/Projekt_1030` |

**Abnahme (iZ3 — Go/No-Go):** Werte innerhalb der Toleranz aus § 3.8. Bei Abweichungen darüber:
Ursachenanalyse nach dem Muster des x64-Umstiegs (`DOTNET_EnableFMA=0`). Ergibt sich eine nicht
erklärbare Abweichung, ist das der **begründete Abbruch für kleines Geld** — genau der Zweck dieses
Pakets.

### iU4 — `EPOS.Kern` herauslösen · L · Windows

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

### iU5 — Statics kappen, Dienste einziehen · L · Windows

**Voraussetzung:** iU4. **Block A2, A3, A4, M4.**

| Inhalt | Menge |
|---|---|
| `Program.*`-Statics → `INavigation`, `IProjektKontext`, `ISprache`, `IPfade` | **40 Dateien** (21 an den vier Kernnamen, 40 einschließlich Pfade/Sprache) |
| `MessageBox.Show` / `ShowDialog` / `DialogResult` → `IDialogDienst` | **127 / 115 / 149** Dateien (Projektsummen, nicht die View-Zahlen des Grundlagenkonzepts) |
| Registry → `IEinstellungen` | **9 Dateien** (nicht 3) |
| DPAPI → `ILizenzAblage` | 2 Dateien — Lizenz **und** KI-Schlüssel |
| `Program.Zahl*`/`Ganzzahl*` → Eingabekomponenten | **44 Dateien**; Vorkommen: `ZahlPruefen` 121, `ZahlFaerben` 120, `ZahlParsen` 55, dazu `Ganzzahl*` 102 |

**Abnahme:** Kein View-fremder Code greift auf `Program.*`; Referenzläufe unverändert PASS.

### iU6 — Datenzugriff plattformfrei · S–M · Windows

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

### iU7 — Charts und Berichte plattformfrei · M · Windows

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
zeilengleich. Der Bildvergleich alt/neu lief nur unter Windows und wurde
nie gefahren: Der Anwender hat am 03.09.2026 die Löschung von `ChartRendererGdi.cs` und des
Modus `bildvergleich` ohne ihn angeordnet (→ iF23); der Nachweis der Bildgleichheit ist seither
der Sichtvergleich der Berichte am Gerät.

### iU8 — `EPOS.UI` und der erste Blazor-Dialog unter Windows · L · Windows

**Voraussetzung:** iU5, iU7. **Block A5, A6, A7; M1, M2, M6, M9.** **Das ist der Modell-C-Stichtag.**

| Inhalt | Detail |
|---|---|
| `EPOS.UI` als Razor-Klassenbibliothek | Bausteinsatz nach A5: SpeichernLeiste, InfoKnopf (an `help_mapping`), Kachel, EinstiegsKarte, Gruppenkopf, Herleitungszeile, Kohärenzzeile, Warnbanner, Farb-/Typografiethema — ~10–12 Bausteine |
| `BlazorWebView` in der WinForms-App | `Microsoft.AspNetCore.Components.WebView.WindowsForms` (für .NET 10 verfügbar und gepflegt); WebView2-Laufzeit als Voraussetzung |
| Standards **vor** der ersten Maske | Raster (QuickGrid-Wrapper), Charts (Ergebnis aus iU7), Datums-/Auswahlfelder — M6: ein nachträglicher Rasterwechsel hieße 36 Masken zweimal bauen |
| Formular-Generator (A7) | ✔ **gebaut** als `Werkzeuge/Formularkarte` (iU8-12). Roslyn über die Designer-Dateien des Bestands — **123/120/63** vor dem Stichtag, **122/119** nach iZ5 und **121 Designer-Dateien / 118 Masken** nach iU9-1 (116 unter `Views/`), nicht 118 und nicht 79/74/21: Feldkarte (Name, Typ, Beschriftung über die **Zeilenregel** „nächstes Label links in derselben Zeile, \|Δy\| ≤ 8 px" — das Raster Label x28/Control x270 gibt es nicht —, Wertebereiche, ComboBox-Einträge, Tab-Reihenfolge, `resx`-Schlüssel beider Sprachen) + Razor-Sektionsskelette, dazu ein Stapellauf `--alle` |
| Erster Dialog | ✔ `Form_Kosten_Auswahl` („Energieträger anlegen") → `EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor`; Datenbankseite in `EPOS.Kern/Controller/EnergietraegerVarianteCtrl.cs`, Texte in `MyResource.Resource.*` — der Dialog spricht damit erstmals auch Englisch |

**Abnahme (iZ5):** Ein Blazor-Dialog läuft im Produktivbetrieb der Windows-App, mit Maus **und**
Finger abgenommen (M2), WinForms-Fassung im selben Schritt stillgelegt (M1). Die Abnahme mit Maus
und Finger ist je Commit abhakbar in
[`Umsetzung_iU8_Nachweise.md`](../ueberholt/Umsetzung_iU8_Nachweise.md).

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

**Stand der Wellen:** [`Status_iOS_Migration.md`](Status_iOS_Migration.md) führt W0 bis W16c je in
einer Zeile; das ausführliche Protokoll je Welle liegt unter
[`../ueberholt/Protokolle/Reporting/`](../ueberholt/Protokolle/Reporting), die vollständigen
Statusblöcke unter
[`../ueberholt/Protokolle/Statusbloecke/`](../ueberholt/Protokolle/Statusbloecke).

### iU10 — Die iOS-Hülle · L · Mac

**Voraussetzung:** iU6, iU8 (Bausteinsatz steht). **Entspricht dem verschobenen Grundlagen-S3.**

| Inhalt | Detail |
|---|---|
| MAUI-App `EPOS.iOS` mit `BlazorWebView` | Navigation nach iL5: Wizard-Workflow als Navigationsstruktur, kein MDI, keine modalen Ketten |
| iOS-Adapter | Keychain (`ILizenzAblage`), `identifierForVendor` (`IGeraeteId`), Document-Picker/Share-Sheet (`IDateiDienst`, `ITeilen`), `Preferences` (`IEinstellungen`), App-Sandbox (`IPfade`), AirPrint (`IDrucken`) |
| Datenbank auf dem Gerät | Seed-Kopie beim Erststart, ~~`bundle_green`~~ **`bundle_e_sqlite3`** (iF27, Begründung unten), Backup über das Share-Sheet |
| Lizenz | `LizenzToken` (Ed25519/BouncyCastle) und `LizenzServerClient` (REST gegen `epos-plan.de`) laufen unverändert — nur Ablage und Geräte-Id sind neu |

**Abnahme (iZ6):** Ein Projekt vollständig auf dem iPad durchgeplant; Ergebnis-CSV wertgleich zur
Windows-Basis; Bericht zeilengleich.

**Nachweise:** [`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md) führt die Schritte
iU10-1 bis iU10-9 im Einzelnen; ihr Stand steht in
[`Status_iOS_Migration.md`](Status_iOS_Migration.md).

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

**Nachtrag (26.09.2026, #524):** Ein Katalog ist vor iU11 auf iOS gezogen — der Katalog der
Brauchwasser-Nutzungsarten (Zapfprofilgenerator, Entscheid ZU26; Umsetzungskonzept Zapfprofilgenerator
N23). Er ist eine freie Ansicht der `AppWurzel` über `IProjektQuelle.NutzungsartKatalogGaben`, geöffnet
über den Hilfe-Assistenten (auf iOS gibt es kein Menü); sein Import nimmt dort allein ein ZIP-Archiv
(`IDateiDienst.OrdnerwahlMoeglich`), und der Prüfmodus weist ihn mit `EPOS_PRUEFLAUF_KATALOGIMPORT` nach.
Für iF2 ändert das nichts: Die übrigen Katalogverwaltungen bleiben auf iOS benannt geschlossen
(KI-D-Q10), ihr Umfang bleibt Sache von iU11. Offen für iU11: ein Einstieg in die Kataloge, der nicht
am Hilfe-Assistenten hängt.

**Nachtrag (26.09.2026, #540):** Der Einstieg steht — der Knopf „Kataloge…“ im Seitenkopf der
Projektliste, nur ohne Menüband (keine Kopfleiste). Seine Einträge sind die Punkte der
`Menuetabelle` mit dem Kennzeichen `Katalog`, deren Ziel die Wurzel führt (`AppWurzel.FuehrtZiel`,
zugleich die Positivliste von `OeffneMaske`): heute Baustoffe, Bauteilaufbauten und
Brauchwasser-Nutzungsarten. Ein weiterer Katalog aus iF2/KI-D-Q10 erscheint dort, sobald die Wurzel
seinen Schlüssel führt (Umsetzungskonzept Zapfprofilgenerator, Nachtrag N29).

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
| **iT10** | **Datenintegrität** | `PRAGMA foreign_key_check` + `integrity_check`, Zeilenzahlen und Prüfsummen je Tabelle — war im `EposSqliteMigrator` umgesetzt (Werkzeug am 13.09.2026 entfernt) | iU6 |

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
| **`partial` über die Assemblygrenze** | Zwei Klassen (`WizardItemClass`, die `FillComboBox`-Hälften) und später der Access-Zweig von `ApplikationCtrl` waren über `partial` in Kern- und Oberflächenhälfte geteilt; das trägt nur innerhalb **einer** Assembly | **gelöst ohne eine einzige Aufrufstelle:** `WizardSeite` erbt statt zu ergänzen, die `FillComboBox`-Hälften werden Erweiterungsmethoden, die beiden Schemamarker-Methoden sind `static` und kamen in eine eigene statische Anwendungsklasse (mit dem Access-Zweig inzwischen entfallen). Fünf `*Ctrl.WinForms.cs` hatten 0 Aufrufer und sind entfallen. Die Falle bleibt eine **Prüfregel** vor jedem weiteren Umzug (`EPOS.Kern/CLAUDE.md`) |
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
| **iF30** | **Lesemodus-Durchsetzung:** `LizenzManager.DarfSchreiben()` hat bis heute genau einen Leser (`KiAusfuehrer.Schreibrecht`, W15c‑B7); weder Simulation noch Projektanlage noch ein Speicherweg fragt den Lizenzzustand. Wann und wo wird der Lesemodus durchgesetzt? | **Nach W16** — angelegt 04.09.2026 mit W15c (E‑9): dann sind alle Speicherwege Razor und ihre Zahl steht fest; dazu die Warnstufen 30/14/7 Tage vor Ablauf (Lizenzkonzept § 6). Bis dahin ist der Zustand **sichtbar** (sechs Zustände, drei Stufen, Detailzeile) und **prüfbar** (19 Kern-Fälle `LizenzZustandTests`), aber nicht durchgesetzt. **Entschieden 04.09.2026 (Empfehlung angenommen): streng — alle Schreibwege und der Simulationslauf werden über die eine Schreibnaht im Kern gesperrt, Ansehen und Berichte bleiben frei, Banner in der `AppWurzel`, Warnstufen 30/14/7 Tage vor Ablauf; Ausnahmen Erststart-Migration, Lizenzaktivierung, Einstellungen; eigene kleine Welle nach der Windows-Abnahme** **Erledigt 06.09.2026 (`8daf051`):** durchgesetzt an der einen Schreibnaht `SqliteDatenzugriff.ErzeugeKommando` (`Schreibnaht.Pruefe`), Lesen frei, eigene `LesemodusException` statt SQLite-Fehler; fünf benannte Ausnahmen (Schema- und Erststart-Migration, Schemamarker, Programmzustand, Sicherung — Lizenzaktivierung und Einstellungen berühren die Datenbank nicht), Simulationssperre vor dem Start, vier Werkzeug-Freigaben, Banner in der `AppWurzel` mit den Warnstufen 30/14/7; Referenzlauf byte-gleich. Offene Punkte iF30‑O‑1…5 im Protokoll **Entschieden 06.09.2026 (O‑1…5):** O‑2 einmal je Kalendertag gebaut (`a3bd169`), O‑1/O‑3/O‑4 bestätigt, O‑5 geschlossen (Access-DB nicht mehr relevant, kein Rückbau) |

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
