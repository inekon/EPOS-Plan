# Umsetzungskonzept: EPOS-Plan auf iOS

**Rev. 1 — 02.09.2026 — zur Abnahme durch Philipp**

Basis: Repo-Stand `7d41833` (Synchronisation 01.09.2026), Branch `main`.
Referenzbasis der Nachweise: `Referenzlaeufe\2026-08-30_B3-Kaskade` (13 Projekte, 332 CSV, Schemastand 61).
Sämtliche Zahlen dieses Dokuments sind am 02.09.2026 gegen den Arbeitsbaum nachgemessen; Abweichungen
zu den Vorgängerdokumenten sind in § 1.5 einzeln ausgewiesen.

> **Verhältnis der Dokumente**
> [`Konzept_iOS-Portierung_EPOS-Plan.md`](Konzept_iOS-Portierung_EPOS-Plan.md) (Rev. 1, 30.08.2026)
> beantwortet **was und warum** — Leitentscheidungen iL1–iL8, Modell C, Migrationsregeln M1–M10,
> Arbeitsblöcke A–E. Es bleibt gültig und wird hier **nicht wiederholt**, sondern zitiert.
> [`Konzept_DB-Migration_SQLite_EPOS-Plan.md`](Konzept_DB-Migration_SQLite_EPOS-Plan.md) (Rev. 2,
> 31.08.2026) beantwortet die **Datenschicht** — Etappen S0–S8, Entscheidungen D1–D8, Risiken R1–R4.
> Dessen Bauanleitung ist `Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md` (Rev. 1,
> 01.09.2026, **auf Branch `sqlite`**) mit den Entscheidungen D9, den Risiken R5/R6 und den
> Arbeitspaketen zu S2–S8. Beides wird hier nur eingebunden.
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
| Regressionsnetz | **vorhanden und scharf** — Referenzlauf-Suite, 6 Modi, Kindprozess je Projekt, Toleranz rel. 1e-4 / abs. 0,01, aktuelle Basis 13/13 PASS über 3.558.333 Werte | `Referenzlaeufe/LIESMICH.md` |
| Chart-Stack SkiaSharp/ScottPlot | **Pakete vorhanden, praktisch ungenutzt** — `SkiaSharp 3.119.0`, `ScottPlot.WinForms 5.1.57` im `.csproj`; `ScottPlot` erscheint in **1** Datei, `SkiaSharp` in **0** | einziger Konsument: `Form_SpeicherOptimierung` |
| Berichtskette portabel | **bereits gegeben** — `ClosedXML 0.105.1` (ab 0.97 ohne `System.Drawing.Common`), `DocumentFormat.OpenXml 3.5.1`, `BouncyCastle 2.7.0`, `MathNet.Numerics 5.0.0` | `.csproj` |
| Datenschicht SQLite | **angelaufen, aber noch kein Anwendungscode.** Auf `main` kein einziger SQLite-Bezug in `.cs`/`.csproj`. Auf **Branch `sqlite`** (Stand 01.09.2026 16:29) liegen: das Implementierungskonzept (644 Z.), `sql/S0_Protokoll_Rechner1_2026-09-01.md` und `sql/tools/Erzeuge-Schema.ps1` (874 Z., Schemagenerator für S2). S0 protokolliert, S2 im Bau, **S4–S8 offen** | SQLite-Konzept § 12; Implementierungskonzept § 9 |
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

### 1.4 Der Konflikt zwischen den beiden Vorhaben

Er ist der wichtigste Einzelbefund dieser Prüfung — und er ist auf der Datenschichtseite gerade
**frisch bestätigt und ausdrücklich verschärft** worden.

Das SQLite-Konzept legt in § 7.4 fest: **`System.Data.OleDb` 8.0.1 bleibt im Projekt**, weil
`OleDbParameter` „ein reiner Datenträger aus einem NuGet-Paket ist und sich ohne jede
OLE-DB-Verbindung konstruieren lässt". Das Implementierungskonzept (01.09.2026) formuliert es
schärfer: *„`OleDbParameter` bleibt als reiner Datenträger an allen ~2.300 Stellen stehen (2.265
unqualifizierte + 28 vollqualifizierte Konstruktoren + 54 Array-Allokationen — an ihnen wird
**nichts** geändert); übersetzt wird **innen** in `DataRepository`."*

**Für Windows ist das die richtige Entscheidung** — sie ist gemessen begründet und hält den
S4-Aufwand klein. **Für iOS ist sie eine Sackgasse:** `System.Data.OleDb` ist ein
Windows-only-Paket. Eine Signatur, die `OleDbParameter` führt, kann in `EPOS.Kern` nicht existieren.

Die Menge ist dabei größer, als eine bloße Signaturänderung vermuten lässt: Es geht nicht um sechs
Methodenköpfe, sondern um **~2.300 Konstruktoraufrufe** in 160 Dateien.

| Weg | Kosten jetzt | Kosten später | Bewertung |
|---|---|---|---|
| **(a)** `DbParam` sofort in S4 — alle ~2.300 Stellen umschreiben | hoch, aber weitgehend maschinell (die Aufrufe sind uniform) | 0 | erkauft Portabilität mit einem großen Einzeleingriff mitten in S4 |
| **(b)** wie geplant, iOS-Datenschicht später separat | 0 | dieselben ~2.300 Stellen, dann ohne S4-Rückenwind | verschiebt die Arbeit, ohne sie zu verkleinern |
| **(c)** **`IDatenzugriff` von Anfang an mit `DbParam`; `DataRepository` behält seine OleDb-Fassade als Windows-Adapter** | gering — nur die neue Schnittstelle | verteilt: jede Aufrufstelle wandert mit ihrer Maske in iU9 | **Empfehlung** |

**Empfehlung (→ iF10): Weg (c).** `EPOS.Kern` definiert `IDatenzugriff` mit einem eigenen schlanken
`DbParam` (Name, Wert, optionaler Typ). `DataRepository` bleibt exakt so, wie das
Implementierungskonzept es baut — mit `OleDbParameter` und der `?`→`@pN`-Übersetzung — und wird
zusätzlich als **Windows-Adapter** hinter `IDatenzugriff` gehängt. Die ~2.300 Altaufrufe bleiben
unangetastet und wandern erst dann auf `DbParam`, wenn ihre Maske ohnehin nach `EPOS.UI` umgebaut
wird (iU9, Strangler-Muster M1). Damit kostet die Portabilität **jetzt fast nichts**, S4 bleibt
unverändert wie geplant, und trotzdem trägt der Kern von Anfang an eine iOS-fähige Schnittstelle.

Was dafür **verbindlich festzulegen ist**: Neuer Datenzugriffscode geht ab dem Stichtag (iZ5)
ausschließlich über `IDatenzugriff`/`DbParam`, nie mehr über `DataRepository` mit `OleDbParameter` —
sonst wächst der Altbestand weiter, während er abgebaut werden soll.

**Zweiter, kleinerer Befund derselben Art:** `RecordSet.cs:9` führt eine **öffentliche, setzbare
Property vom konkreten Typ `OleDbCommand`**:

```csharp
public OleDbCommand DBCommand { get; set; }
```

Der Kommentar nennt den Grund: „Auf `OleDbCommand` umgestellt, damit Zuweisungen aus dem UI-Code
(z. B. `transaction`) ohne Cast funktionieren." UI-Code weist von außen Verbindung und Transaktion
zu. Eine Fassade kann diese Property nicht kappen, ohne die zuweisenden Aufrufer anzufassen. Sie
steht **weder im SQLite- noch im iOS-Grundlagenkonzept** und ist ein zusätzlicher Posten zur
S4-Schätzung (→ iU6, → iF10).

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
| Chart- und Grid-Masken | 16 / 16 | nicht reproduzierbar: **9** bzw. **7** Designer-Instanzen, **18** bzw. **36** Dateien mit Typnutzung | vor iU9 durch Einzeldurchsicht zu klären (→ iU0) |

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
§ 1.3 prüfen, Paketstände gegen die Build-Matrix (§ 3.6) halten. Stand Rev. 1: siehe oben,
Messdatum 02.09.2026, Repo-Stand `7d41833`.

---

## 2 Zielarchitektur in der Umsetzungssicht

Das Architekturbild (Modell C: ein Kern, eine UI-Bibliothek, zwei Hüllen) steht im Grundlagenkonzept
§ 6a.2 und wird hier nicht wiederholt. Was hier steht, ist seine **Projektfassung**: welche
`.csproj` entstehen, was sie referenzieren dürfen und woran der Bruch der Regel auffällt.

### 2.1 Zielbild der Solution

| Projekt | Art | TargetFramework(s) | Plattform | darf referenzieren | Status |
|---|---|---|---|---|---|
| `EPOS.Kern` | Klassenbibliothek | `net10.0` | AnyCPU | **nichts** aus dem Bestand; nur plattformfreie Pakete | **neu** (iU4) |
| `EPOS.Daten` | Klassenbibliothek | `net10.0` | AnyCPU | `EPOS.Kern` | **neu** (iU6) |
| `EPOS.UI` | Razor-Klassenbibliothek | `net10.0` | AnyCPU | `EPOS.Kern` | **neu** (iU8) |
| `EPOS.Kern.Tests` | xUnit | `net10.0` | AnyCPU | `EPOS.Kern`, `EPOS.Daten` | **neu** (iU4) |
| `EPOS.Referenzlauf` | Konsole | `net10.0` | AnyCPU | `EPOS.Kern`, `EPOS.Daten` | **neu** (iU4) — ersetzt `Referenzlauf` für den Kernbeweis |
| `SpeicherEngine`, `KiKern` | Klassenbibliothek | `net10.0` | AnyCPU | nichts | **anheben** (iU1) |
| `SpeicherEngine.Tests`, `KiKern.Tests` | xUnit | `net10.0` | AnyCPU | ihre Engine | **anheben** (iU1) |
| `WindowsFormsApplication1` | WinExe | `net10.0-windows` | x64 | alles Obige + COM | **bleibt** — schrumpft über iU9 |
| `Referenzlauf` | Konsole | `net10.0-windows` | x64 | WinForms-App | **bleibt**, bis iU9 abgeschlossen ist |
| `EPOS.iOS` | MAUI-App | `net10.0-ios` | ARM64 | `EPOS.Kern`, `EPOS.Daten`, `EPOS.UI` | **neu** (iU10) |
| `CSExeCOMServer` | — | — | — | — | **stilllegen** (iU0) — .NET Framework 4.0, fachlich totes Altgut, alle Konsumenten auskommentiert |

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
| `IDatenzugriff` | `EPOS.Kern` | SQLite (`EPOS.Daten`) | SQLite (`EPOS.Daten`) | 160 + 60 + 35 Dateien |
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
                                    IDatenzugriff · Dienstschnittstellen)
                             ▲            ▲            ▲
              ┌──────────────┘            │            └──────────────┐
              │                           │                           │
      EPOS.Referenzlauf            EPOS.UI (Blazor)            EPOS.Daten (SQLite)
      Konsole, headless             ▲            ▲
      Windows + macOS + CI          │            │
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
| dokumentierte tote Enden (`FormMain`-Altzweig, `Form_Wirtschaftlichkeit`-Hülle, `Form_AlsVariante`, „- Kopie"-Dateien) | ~10–15 Views | iU9 |
| `DataRepository.ProviderVorhanden()` | 1 Methode, 1 Aufruf | SQLite-S4 |
| Access-Zweig der `SchemaMigration` (61 Schritte) | eingefroren, nicht portiert | SQLite-S6 |
| `Abfrage_Kuehlung_MaxLast`, `Abfrage_SST`, `Abfrage_KenndatenKuehlung_Max` | 3 Views ohne Anwendungszugriff | SQLite-S1 (bestätigen), S2 (weglassen) |
| 5 Phantom-Abfragen im Code (`Abfrage_KostenKomponenten`, `…_ProjektKostenInvestBetrieb`, `…_Erzeuger_Vorlauftemperaturen`, `…_Heizkessel_Kosten`, `…_Neues_Kosten_Model`) | nur Kommentare/Aufräumlisten, kein ausführbares SQL | SQLite-S1 — bereits geprüft, keiner ist Migrationsgegenstand |
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
| Visual Studio | 2022 (17.14) | **2026** | MAUI 10 wird von VS 2022 nicht mehr getragen. Der Wechsel ist unabhängig von iOS fällig, sobald .NET 10 kommt |
| Workloads | .NET-Desktop | + **.NET MAUI**, + ASP.NET (für Blazor-Werkzeuge) | Der Windows-Rechner baut die MAUI-**Windows**-Ziele und redigiert Blazor-Komponenten; iOS-Ziele nicht |
| .NET SDK | 9.0.315 (unfestgeschrieben) | **10.0.x, festgeschrieben in `global.json`** | Heute ist die SDK-Version nirgends fixiert — auf einem zweiten Rechner baut also potenziell etwas anderes |
| WebView2-Laufzeit | nicht gefordert | **Voraussetzung** (auf Windows 11 vorhanden, im Installer prüfen) | trägt `BlazorWebView` in der WinForms-Hülle (M9) |
| Access Database Engine | x64-Redist erforderlich | **entfällt mit SQLite-S8** | `Microsoft.Data.Sqlite` bringt die native Bibliothek mit — die Bitness-Falle und das Installer-Prerequisite verschwinden ersatzlos |
| SQLite-Werkzeug | — | **SQLiteStudio/Letos 4.0.3** oder DBeaver | Ersatz für den Access-Direktzugriff (M3a). Der dokumentierte Rückschritt ist konkret: SQLiteStudio 3.4 hat **keinen QBE-Abfrageentwurf** und **kein ER-Diagramm**; Letos 4.0.3 bringt einen ERD-Editor mit |

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
| `EPOS.Kern` | ✅ | ✅ | ✅ | – |
| `EPOS.Daten` | ✅ | ✅ | ✅ | – |
| `EPOS.UI` | ✅ | ✅ | ✅ | – |
| `EPOS.Kern.Tests` | ✅ | ✅ **testet** | ✅ **testet** | – |
| `EPOS.Referenzlauf` | ✅ | ✅ **läuft** | ✅ **läuft** | – |
| `SpeicherEngine`, `KiKern` (+ Tests) | ✅ | ✅ **testet** | ✅ **testet** | – |
| `WindowsFormsApplication1` | ✅ | ✅ *(nach Schritt 5)* | ❌ | – |
| `Referenzlauf` | ✅ | ✅ *(nach Schritt 5)* | ❌ | – |
| `EPOS.iOS` | ❌ | ❌ | ✅ | ✅ **signiert, `.ipa`** |

Die beiden ❌ in der macOS-Spalte sind gewollt und dauerhaft: WinForms läuft dort nicht. Alles
darüber ist der portable Teil — und er umfasst den gesamten Rechenkern.

### 3.7 Continuous Integration

Es gibt heute **keine**. Das ist bei einem Einzelplatz-Windows-Projekt vertretbar; bei zwei
Plattformen ist es der sichere Weg in eine unbemerkte Kerndrift (M8).

**Zuschnitt:** GitHub Actions, zwei Läufe.

| Workflow | Runner | Inhalt | Auslöser |
|---|---|---|---|
| `kern.yml` | `ubuntu-latest` **und** `macos-latest` | `dotnet build` + `dotnet test` für Kern, Daten, UI, SpeicherEngine, KiKern; Kern-Referenzlauf gegen die eingecheckte Testdatenbank | jeder Push, jeder PR |
| `windows.yml` | `windows-latest` | vollständige Solution mit VS-MSBuild (bis Schritt 5 abgeschlossen ist), danach `dotnet`; Referenzlauf-Vergleich gegen die eingefrorene Basis | Push auf Hauptzweige, nächtlich |
| `ios.yml` | `macos-latest` | `EPOS.iOS` bauen, signieren, `.ipa`, TestFlight-Upload | Tag / manuell (ab iU13) |

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
| Formular-Generator (A7) | existiert nicht | Entwicklerwerkzeug in `dev\`, nicht in der Solution (die `.csproj` sammelt `**\*.cs` ein — eine `.cs`-Datei unterhalb `WindowsFormsApplication1\` bricht den Build mit CS0102/CS0017) |
| Lokalisierung | ResXManager, Drei-Schichten-Regel | **unverändert.** `MyResource.Resource.*` ist eine normale Klasse und läuft in Blazor auf beiden Plattformen; `DbWerte` bleibt eingefroren (M7) |
| Installer | Inno Setup 6.3, `build-setup.ps1` | bleibt für Windows. **Zu korrigieren:** `EPOS-Plan.iss:29` steht auf `AppExeName "WindowsFormsApplication1.exe"`, das Projekt liefert seit 29.08.2026 `EPOS_Plan.exe` — der Setup-Bau bricht in dieser Kombination ab |

### 3.10 Die Bausteine iE1–iE10

| Nr. | Baustein | Paket | Nachweis |
|---|---|---|---|
| **iE1** | `global.json`, `Directory.Build.props`, `Directory.Packages.props` | iU1 | zwei Rechner bauen nachweislich dasselbe |
| **iE2** | Alle Projekte auf .NET 10 | iU1 | Solution baut, Referenzläufe unverändert PASS |
| **iE3** | COM-Referenzen entfernen (2 Dateien auf ClosedXML) | iU1 | **`dotnet build WP-Plan.sln` läuft durch** |
| **iE4** | GitHub Actions: `kern.yml` (ubuntu + macOS), `windows.yml` | iU1 | erste grüne Läufe |
| **iE5** | Portabilitätssperre: `net10.0` ohne `-windows`, macOS-Build in der CI | iU4 | eine Windows-API im Kern bricht den Build |
| **iE6** | Testdatenbank für die CI (13 Referenzprojekte, SQLite) | iU3 | Kern-Referenzlauf läuft in der CI |
| **iE7** | Mac-Arbeitsplatz: Hardware, Xcode, `maui-ios`, Simulator | iU2 | Hallo-Welt-MAUI mit `EPOS.Kern`-Referenz im Simulator |
| **iE8** | `EPOS.Referenzlauf` — headless, plattformfrei | iU4 | derselbe Lauf auf Windows und macOS, Vergleich PASS |
| **iE9** | Apple-Konto, Bundle-ID, Zertifikate, Signierkette in der CI | iU2 / iU13 | signiertes `.ipa` aus der CI |
| **iE10** | SQLite-Werkzeugkette (Letos/DBeaver), Betriebsersatz für Access | SQLite-S8 | Backup, VACUUM, Sichtprüfung ohne Access möglich |

**Abnahme des Kapitels (iZ2):** `dotnet build` baut die gesamte Solution auf Windows; `EPOS.Kern`
baut und testet zusätzlich auf macOS in der CI; eine MAUI-Hülle startet mit Kern-Referenz im
iPad-Simulator.

---

## 4 Arbeitspakete iU0–iU13

Größenklassen: **S** ≤ 3 PT · **M** 4–10 PT · **L** 11–25 PT · **XL** > 25 PT (mehrere Wellen).
Die XL-Pakete sind bewusst nicht durchgeschätzt — vor iU3 gibt es dafür keine belastbare Grundlage,
und das Grundlagenkonzept § 6 sagt dazu das Nötige.

### iU0 — Klärung, Sicherung, Rückbau · S · Windows

**Voraussetzung:** keine. **Zuordnung:** Vorstufe zu allem.

| Inhalt | Detail |
|---|---|
| Entscheidungen bestätigen | iF1 (Spike), iF3 (Blazor Hybrid), iF7 (Generator), **iF8 (Modell C)**, **iF9 (SQLite auf Windows)** — iU4 ff. setzen sie voraus |
| Neue Entscheidungen einholen | iF10–iF16 (§ 8) |
| Referenzbasis einfrieren | `2026-08-30_B3-Kaskade` als Bezugspunkt aller Umzugsnachweise festschreiben |
| Chart- und Grid-Masken auszählen | Einzeldurchsicht der 18 Chart- und 36 Grid-Dateien; die Konzeptzahl 16/16 ist nicht reproduzierbar (§ 1.5) und Aufwandstreiber für iU9 |
| Rückbau | `CSExeCOMServer` aus dem Repo, `WindowsFormsApplication1.csproj.netfx-backup` entfernen |
| Offene x64-Punkte | (a)–(c) und (e) aus `Konzept_Umstellung_64Bit_EPOS-Plan.md` § 10 terminieren; (d) entfällt mit SQLite |

**Abnahme:** Entscheidungsregister vollständig, keine offene Vorbedingung für iU1.

### iU1 — Entwicklungsumgebung Stufe 1: Windows und CI · M · Windows

**Voraussetzung:** iU0. **Bausteine:** iE1, iE2, iE3, iE4. **Grundlagen:** A1 (teilweise), D2.

| Inhalt | Detail |
|---|---|
| Zentrale Buildsteuerung | `global.json` (SDK 10 gepinnt), `Directory.Build.props` (Sprachversion, Nullable, gemeinsame Eigenschaften), `Directory.Packages.props` (zentrale Paketversionen) |
| .NET 10 | alle sieben Projekte anheben; Testprojekte von `net9.0` auf `net10.0` — sie liegen heute **vor** dem Hauptprojekt, was auf Dauer nicht tragfähig ist |
| **COM-Referenzen entfernen** | `Allgemein/ToolsClass.cs` und `Allgemein/Import/GanglinienDatei.cs` von Excel-Interop auf ClosedXML; `VBIDE`-Referenz löschen; `NoWarn`-Liste um `MSB3568`/`NU1701` bereinigen, soweit dadurch gegenstandslos |
| Solution bereinigen | `Referenzlauf` aufnehmen, `CSExeCOMServer` entfernen |
| CI aufsetzen | `kern.yml` (ubuntu + macOS) und `windows.yml`; `.github/copilot-instructions.md` durch etwas Projektbezogenes ersetzen oder löschen |
| Setup nachziehen | `EPOS-Plan.iss:29` auf `EPOS_Plan.exe`; `build-setup.ps1` von VS-MSBuild auf `dotnet publish` umstellen, sobald die COM-Referenzen weg sind |

**Abnahme (iZ1):** `dotnet build WP-Plan.sln` und `dotnet test` laufen auf einem Rechner ohne Visual
Studio durch. Referenzlauf gegen `2026-08-30_B3-Kaskade`: **13/13 PASS, 332/332 byte-gleich** — der
Frameworkwechsel darf kein einziges Ergebnis bewegen.

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

**Voraussetzung:** iU1, iU2, SQLite-S0–S3. **Entspricht Grundlagen-S0.** **Baustein:** iE6.

Das ist der Beweis, für den das ganze Vorhaben vorne klein gehalten wird.

| Inhalt | Detail |
|---|---|
| Datenstand | `EposSqliteMigrator` (SQLite-S3) erzeugt aus der Produktivkopie eine `.sqlite`; daraus die CI-Testdatenbank mit den 13 Referenzprojekten |
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
| Umzug | Modelle (45 Dateien), Rechenkern (`BhkwPlan.cs`, 410 Z.), Simulation (25 Dateien in `Allgemein/Simulation`), Wirtschaftlichkeit (20), `DbWerte.cs` (2.562 Z.), Berichtslogik (18) |
| Kodierung | die **68** Nicht-UTF-8-Dateien beim Umzug einmalig auf UTF-8 mit BOM (M10) — eigener Commit, keine Inhaltsänderung |
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

### iU6 — Datenschicht plattformfrei anschließen · M · Windows

**Voraussetzung:** iU4, **SQLite-S4a–S4e und S7**. **Block B1**, ergänzt um die Befunde aus § 1.4.

**Dieses Paket ist bewusst klein, weil die SQLite-Bauanleitung das meiste bereits erledigt.** Was
dort schon geplant ist, wird hier **nicht** wiederholt:

| erledigt durch | Inhalt |
|---|---|
| **S4a** | `DataRepository`-Übersetzer (`?`→`@pN`), Verbindungsaufbau mit `PRAGMA foreign_keys = ON` je Verbindung — ohne diese Zeile sind alle 90 Fremdschlüssel wirkungslos |
| **S4b** | die 36 Eigenverbindungs-Dateien, `StilleDb` und zwei private Klone, `RecordSet` **innen** |
| **S4c** | ~24 `GetOleDbSchemaTable`-Stellen, ADOX-Reseed, 3 `CommandBuilder` |
| **S4d** | Selbstheilungs-DDL: 29 Stellen in 16 Dateien |
| **S4e** | Transaktionen auf `DbVorgang` — 18 + 13 Dateien. **Der Typ, den auch iOS braucht, entsteht also ohnehin** |

Für iOS bleibt danach übrig:

| Inhalt | Detail |
|---|---|
| `IDatenzugriff` in `EPOS.Kern` mit eigenem `DbParam` | `DataRepository` bleibt in seiner S4-Fassung und wird als **Windows-Adapter** dahintergehängt (Weg (c), § 1.4, → iF10). Die ~2.300 Altaufrufe bleiben unberührt und wandern mit ihren Masken in iU9 |
| **`RecordSet.DBCommand`** | Die Bauanleitung sagt ausdrücklich: „wird innen umgestellt, **die öffentliche Fläche bleibt**". Die Fläche ist `public OleDbCommand DBCommand { get; set; }` (`RecordSet.cs:9`), der UI-Code weist Verbindung und Transaktion von außen zu. Für Windows unschädlich, **für iOS ein Blocker** — `System.Data.OleDb` fehlt dort. Betrifft **47 echte Nutzer**; entweder die Property auf einen eigenen Typ heben oder `RecordSet` in iU9 mit seinen Masken ablösen |
| iOS-Laufzeit | `Microsoft.Data.Sqlite` zieht standardmäßig `SQLitePCLRaw.bundle_e_sqlite3`, das auf iOS an der AOT-Regel gegen dynamisches Laden scheitert. **Auf iOS `SQLitePCLRaw.bundle_green` setzen** (nutzt dort die System-SQLite) |
| Seed und Pfade | Katalog-`.sqlite` als App-Beilage, beim Erststart in den beschreibbaren Bereich kopieren (`IPfade`) |

**Abnahme:** Testprojekte rechnen auf SQLite wertgleich; `EPOS.Kern` und `EPOS.Daten` enthalten
keinen Verweis auf `System.Data.OleDb`.

### iU7 — Charts und Berichte plattformfrei · M · Windows

**Voraussetzung:** iU4. **Block D1, D2, M5.** Läuft **parallel** zu iU5/iU6 — ohne UI testbar.

| Inhalt | Detail |
|---|---|
| `ChartRenderer` | `Allgemein/Bericht/ChartRenderer.cs`, **821 Zeilen**, `System.Drawing` + `Drawing2D` + `Imaging` → SkiaSharp. Der einzige echte GDI+-Blocker der Berichtskette |
| übriges GDI+ | 26 Dateien nutzen `Graphics`; die 256 `System.Drawing`-Treffer sind weit überwiegend Typnutzung (`Color`, `Font`, `Point`) in Designer-Dateien und damit unkritisch |
| Berichtsausgabe | Word/Excel über `IDateiDienst`/`ITeilen`; ClosedXML-Standardschrift für Nicht-Windows setzen (§ 3.5) |

**Das Chart-Problem, das das Grundlagenkonzept nicht kennt.** iL4 (Blazor Hybrid) und iL6
(ScottPlot 5/SkiaSharp) vertragen sich nicht unmittelbar: **SkiaSharp-Blazor-Komponenten
funktionieren in einem Blazor-Hybrid-Wirt nicht** — die WebView ist ein eigener Prozess, das
Zeichnen findet im .NET-Prozess statt. Drei Wege:

| Weg | Bewertung |
|---|---|
| **ScottPlot rendert im .NET-Prozess ein PNG, die Blazor-Seite zeigt es als Bild** | **Empfehlung.** Ein Chart-Stack für Bericht *und* Bildschirm, identische Optik auf beiden Plattformen, kein zusätzliches Paket. Preis: keine Interaktion im Chart (Zoom, Tooltip) ohne Zusatzarbeit |
| JavaScript-Chartbibliothek in der Blazor-Schicht | volle Interaktion, aber **zwei** Chart-Stacks (einer für den Bericht, einer für den Bildschirm) — genau das, was M5 abschafft |
| Chart außerhalb der WebView als natives MAUI-Steuerelement | bricht das Modell C — die Komponente wäre je Hülle verschieden |

Zu klären in iU7, nicht erst in iU9 (→ iF16), denn davon hängt ab, ob die 39
`DataVisualization`-Stellen ein oder zwei Ziele haben.

**Abnahme:** Berichtsbilder aus dem neuen Renderer sind gegen die alten sichtgeprüft; Berichtsdatei
zeilengleich.

### iU8 — `EPOS.UI` und der erste Blazor-Dialog unter Windows · L · Windows

**Voraussetzung:** iU5, iU7. **Block A5, A6, A7; M1, M2, M6, M9.** **Das ist der Modell-C-Stichtag.**

| Inhalt | Detail |
|---|---|
| `EPOS.UI` als Razor-Klassenbibliothek | Bausteinsatz nach A5: SpeichernLeiste, InfoKnopf (an `help_mapping`), Kachel, EinstiegsKarte, Gruppenkopf, Herleitungszeile, Kohärenzzeile, Warnbanner, Farb-/Typografiethema — ~10–12 Bausteine |
| `BlazorWebView` in der WinForms-App | `Microsoft.AspNetCore.Components.WebView.WindowsForms` (für .NET 10 verfügbar und gepflegt); WebView2-Laufzeit als Voraussetzung |
| Standards **vor** der ersten Maske | Raster (QuickGrid-Wrapper), Charts (Ergebnis aus iU7), Datums-/Auswahlfelder — M6: ein nachträglicher Rasterwechsel hieße 36 Masken zweimal bauen |
| Formular-Generator (A7) | Roslyn über die **118** `Designer.cs`: Feldkarte (Name, Typ, Beschriftung über das Raster Label x28/Control x270, Wertebereiche, ComboBox-Einträge, Tab-Reihenfolge, `resx`-Schlüssel beider Sprachen) + Razor-Sektionsskelette |
| Erster Dialog | ein aktiver, ohnehin anzufassender Dialog aus dem Kosten- oder Wirtschaftlichkeitsbereich |

**Abnahme (iZ5):** Ein Blazor-Dialog läuft im Produktivbetrieb der Windows-App, mit Maus **und**
Finger abgenommen (M2), WinForms-Fassung im selben Schritt stillgelegt (M1).

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
| K6 stilllegen | ~10–15 | tote Enden begraben statt mitschleppen |

**Reihenfolge:** nach Anfasswahrscheinlichkeit — zuerst Wirtschaftlichkeit und Kosten (die aktiven
Baustellen; `Views/Kosten` allein sind 48 Dateien), zuletzt die ruhenden Admin- und Katalogdialoge.

**Abnahme je Welle:** Feldkartenabgleich vollständig, beide Sprachen, Maus und Finger.

### iU10 — Die iOS-Hülle · L · Mac

**Voraussetzung:** iU6, iU8 (Bausteinsatz steht). **Entspricht dem verschobenen Grundlagen-S3.**

| Inhalt | Detail |
|---|---|
| MAUI-App `EPOS.iOS` mit `BlazorWebView` | Navigation nach iL5: Wizard-Workflow als Navigationsstruktur, kein MDI, keine modalen Ketten |
| iOS-Adapter | Keychain (`ILizenzAblage`), `identifierForVendor` (`IGeraeteId`), Document-Picker/Share-Sheet (`IDateiDienst`, `ITeilen`), `Preferences` (`IEinstellungen`), App-Sandbox (`IPfade`), AirPrint (`IDrucken`) |
| Datenbank auf dem Gerät | Seed-Kopie beim Erststart, `bundle_green`, Backup über das Share-Sheet |
| Lizenz | `LizenzToken` (Ed25519/BouncyCastle) und `LizenzServerClient` (REST gegen `epos-plan.de`) laufen unverändert — nur Ablage und Geräte-Id sind neu |

**Abnahme (iZ6):** Ein Projekt vollständig auf dem iPad durchgeplant; Ergebnis-CSV wertgleich zur
Windows-Basis; Bericht zeilengleich.

### iU11 — Kataloge, Importe, Feinschliff · M–L · Mac

**Voraussetzung:** iU10. **Grundlagen-S5; D3–D5. Umfang nach iF2.**

VDI 3805 / CEC / PAN / CSV-Importe über den iOS-Dateidialog; KI-Chat und Wiki-Hilfe (REST bleibt,
Schlüsselablage über `ILizenzAblage`, Caches über `IPfade`); Katalogpflege nach Scope-Entscheidung
iF2 — Empfehlung des Grundlagenkonzepts: erste Auslieferung **ohne** Katalog-Admin.

### iU12 — Absicherung und Betrieb · M · beide

**Block E1–E3.** Referenzläufe beider Plattformen als Pflicht-Gate; Abnahme je Maske; Mischphase (M9)
dokumentiert — zwei Optiken in einer App sind gewollt und enden erst mit der letzten Maske;
Installer mit WebView2-Voraussetzung; `BETRIEB_SQLITE.md` (SQLite-S8).

### iU13 — TestFlight und Vertriebsweg · S–M · Mac

**Voraussetzung:** iU11, iU12. **Grundlagen-S6, iF5, iF12.**

Signierkette in der CI scharf; TestFlight-Feldtest (90-Tage-Grenze beachten); Vertriebsweg nach
§ 3.4 entscheiden und einrichten; Provisionsfrage (iF12) **vorher** geklärt.

### 4.1 Meilensteine

| Nr. | Meilenstein | nach | Nachweis |
|---|---|---|---|
| **iZ1** | Solution baut ohne Visual Studio | iU1 | `dotnet build`/`dotnet test` grün; Referenzlauf 332/332 byte-gleich |
| **iZ2** | Entwicklungsumgebung steht | iU2 | Build-Matrix § 3.6 erfüllt; MAUI-Hallo-Welt mit Kernbibliothek im Simulator |
| **iZ3** | **Go/No-Go** | iU3 | Projekt 1030 im Simulator wertgleich zur Referenzbasis |
| **iZ4** | Kern herausgelöst | iU4 | Windows byte-gleich; `EPOS.Kern` baut und testet auf macOS |
| **iZ5** | Modell-C-Stichtag | iU8 | erster Blazor-Dialog produktiv, Maus und Finger abgenommen |
| **iZ6** | iPad rechnet ein Projekt vollständig | iU10 | Ergebnis wertgleich, Bericht zeilengleich |
| **iZ7** | Auslieferungsfähig | iU13 | signiertes `.ipa`, Feldtest bestanden, Vertriebsweg eingerichtet |

### 4.2 Abhängigkeiten

```
iU0 → iU1 → iU2 → iU3 ═══ Gate iZ3 ═══> iU4 ─┬─> iU5 ──> iU8 ──> iU9 (dauerhaft)
                    ▲                         ├─> iU6 ──────┐
        SQLite-S0–S3┘                         └─> iU7 ──────┤
                    SQLite-S4–S7 ─────────────────> iU6     └──> iU10 → iU11 → iU12 → iU13
```

**Parallelisierbar:** iU5, iU6 und iU7 nach iU4; iU9 ab iU8 dauerhaft neben allem Weiteren; die
SQLite-Etappen S0–S3 vollständig neben iU0–iU2, weil sie die Anwendung nicht anfassen.

**Nicht parallelisierbar:** iU3 vor iU4 (das Gate hat einen Zweck); iU8 vor iU9 (Standards vor der
ersten Maske, M6); SQLite-S4–S7 vor iU6.

---

## 5 Etappen und Zuordnung zu den Vorgängerdokumenten

### 5.1 Etappenübersicht

| Etappe | Pakete | Ort | Gate |
|---|---|---|---|
| **0 — Fundament** | iU0, iU1 · parallel SQLite-S0–S3 | Windows | iZ1: Solution baut ohne Visual Studio |
| **1 — Umgebung und Beweis** | iU2, iU3 | Mac / CI | **iZ3: Go/No-Go** |
| **2 — Datenschicht** | SQLite-S4a–S4e, S5–S7 (≈ 21–30 PT gesamt ab S0) | Windows | SQLite-Abnahmeprotokoll (S7) |
| **3 — Gesundung** | iU4, iU5, iU6, iU7 | Windows | iZ4: Kern byte-gleich, baut auf macOS |
| **4 — UI-Fundament** | iU8 | Windows | iZ5: Modell-C-Stichtag |
| **5 — Masken** | iU9 (Wellen) | Windows | je Welle Feldkartenabnahme |
| **6 — iPad** | iU10, iU11 | Mac | iZ6: ein Projekt vollständig |
| **7 — Auslieferung** | iU12, iU13 | beide | iZ7 |

**Zur Reihenfolge Datenschicht vor Kern (Etappe 2 vor 3).** Das Grundlagenkonzept ordnet
S1 (Kern) vor S2 (Datenschicht). Dieses Dokument **kehrt das um**, aus einem Grund: Wandert der Kern
zuerst, tragen die umgezogenen Dateien noch die OLE-DB-Signaturen, und SQLite-S4 fasst dieselben
Dateien ein zweites Mal an. Andersherum wird jede Datei genau einmal berührt. Der Preis ist, dass der
sichtbare iOS-Fortschritt später beginnt — der Beweis (iU3) liegt zu diesem Zeitpunkt aber schon vor
(→ iF10 hängt mit dieser Umkehrung zusammen).

### 5.2 Zuordnung der Kürzel

| Dieses Dokument | Grundlagenkonzept | SQLite-Konzept |
|---|---|---|
| iU0 | — | S0 (teilweise) |
| iU1 | A1 (teilweise), D2 | — |
| iU2 | „Voraussetzungen" § 5 | — |
| iU3 | **S0** | S1–S3 als Zulieferung |
| iU4 | **S1**, A1, M10 | — |
| iU5 | A2, A3, A4, M4 | — |
| iU6 | **S2** (Rest), B1 | **S4a–S4e, S7** als Voraussetzung; `DbVorgang` entsteht bereits in S4e |
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
| **.NET 10** | 10.11.2026 (Support-Ende 8/9) — von außen gesetzt | **fix**, Paket iU1 |
| **Modell C (M1)** | mit iZ5 — ab dann kein Dialog mehr doppelt | folgt aus iU8 |
| **SQLite auf Windows (M3/iF9)** | SQLite-D7, Stand 01.09.2026: BHKW-Wirtschaftlichkeit ist bereits Schritt 60/61 erledigt, **offen nur noch der Einheitenbruch (Schritt 62)**; S0–S3 laufen sofort | **kein kalendarisches Datum**, aber die Bedingung ist fast erfüllt. Ob Schritt 62 als letzter Access- oder erster SQLite-Schritt kommt, wird laut Implementierungskonzept bei S4-Start entschieden — beides trägt |

---

## 6 Nachweise und Teststrategie

Kein Paket gilt als fertig, weil es gebaut ist. Es gilt als fertig, wenn sein Nachweis vorliegt.

| Nr. | Nachweis | Inhalt | Pakete |
|---|---|---|---|
| **iT1** | **Byte-Gleichheit** | Referenzlauf gegen `2026-08-30_B3-Kaskade`, 332/332 Dateien byte-/MD5-gleich. Der Maßstab für alles, was sich **nicht** ändern darf | iU1, iU4, iU5 |
| **iT2** | **Wertgleichheit in Toleranz** | rel. 1e-4 / abs. 0,01 wie heute; nichtnumerische Werte exakt. Der Maßstab für Plattform- und Backendwechsel | iU3, iU6, iU10 |
| **iT3** | **Plattformvergleich** | derselbe Kern-Referenzlauf auf x64-Windows und ARM64-macOS/iOS; Abweichungen nach dem FMA-Muster des x64-Umstiegs erklärt | iU3, iU4, iU10 |
| **iT4** | **Build-Matrix** | § 3.6 vollständig erfüllt; CI grün auf allen drei Runnern | iU1, iU2, iU4 |
| **iT5** | **Berichtsvergleich** | Word-/Excel-Bericht zeilengleich, Chartbilder sichtgeprüft | iU7, iU10 |
| **iT6** | **Feldkartenabnahme** | je Maske das generierte Inventar vollständig abgehakt — bei 730 Textfeldern ist das vergessene Feld der typische Migrationsfehler | iU8, iU9 |
| **iT7** | **Kulturtest** | `EPOS_REFLAUF_UICULTURE=en-US`: Ergebnisdateien **byte-identisch**. Der maschinelle Nachweis der Drei-Schichten-Regel | jede Etappe |
| **iT8** | **Bedienbarkeit** | jede Komponente mit Maus **und** Finger abgenommen (M2) — sonst entsteht die zweite UI durch die Hintertür | iU8, iU9 |
| **iT9** | **Kodierungsnachweis** | nach iU4 null Nicht-UTF-8-Dateien in den neuen Projekten | iU4 |
| **iT10** | **Datenintegrität** | `PRAGMA foreign_key_check` + `integrity_check`, Zeilenzahlen und Prüfsummen je Tabelle | SQLite-S3, iU6 |

**Was die Nachweise nicht abdecken:** die manuelle Abnahme der x64-Umstellung (§ 3.7) und die
Sichtabnahme der Masken. Beides bleibt Handarbeit.

---

## 7 Risiken

| Nr. | Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|---|
| **iR1** | **Der SQLite-Stichtag kommt nicht.** D7 bindet ihn an „Schema-Beruhigung"; nach dem Stand vom 01.09.2026 fehlt nur noch Schritt 62 (Einheitenbruch) — bei über zwanzig Fachetappen im Jahr kann jederzeit ein neuer Schritt nachrücken | Etappe 2 blockiert, damit iU6, iU10 und das ganze iPad-Ziel | **Kalendarischen Termin setzen**, sobald Schritt 62 beschieden ist (iF10). SQLite-S0–S3 laufen ohnehin risikofrei vorab und sind bereits angelaufen |
| **iR2** | **Apple-Toolchain-Drift.** .NET-für-iOS und Xcode sind versionsstarr gekoppelt; eine automatische Xcode-Aktualisierung legt den Build lahm | Mac-Arbeitsplatz und iOS-CI stehen | Xcode-Aktualisierungen **nie automatisch**; Version im Team dokumentieren; CI-Runner-Image pinnen |
| **iR3** | **Blazor Hybrid und SkiaSharp** vertragen sich nicht unmittelbar (§ iU7) | zwei Chart-Stacks statt einem — genau das, was M5 verhindern soll | Frühentscheidung in iU7 (iF16), nicht erst bei der ersten Chart-Maske |
| **iR4** | **Gleitkomma auf ARM64.** Zwischen x64 und Apple Silicon sind Abweichungen zu erwarten wie damals zwischen x86 und x64 | „wertgleich" wird bestreitbar, das Abnahmeinstrument stumpf | Toleranz vor iU3 definieren (iF15); FMA-Analyse als erprobtes Muster |
| **iR5** | **Parallelentwicklung.** Der Fachausbau läuft weiter; jede Etappe, die während iU4–iU9 in die WinForms-App fließt, ist Arbeit, die später wandern muss | Der Umbau holt den Bestand nie ein | Modell-C-Stichtag (iZ5) so früh wie möglich; ab dann fließt Neues in `EPOS.UI` statt in WinForms |
| **iR6** | **Kein Testdatenbestand in der CI.** `.gitignore` schließt `*.accdb` aus; ohne Datenbank ist die Kern-CI nur ein Kompilierungstest | Der Wertgleichheitsnachweis läuft nicht automatisch, sondern nur von Hand | Anonymisierte `Kenndaten_Test.sqlite` versionieren (iE6, iF14) |
| **iR7** | **Provisionsfrage beim Lizenzverkauf.** Ob Apple bei einer B2B-Fachanwendung In-App-Kauf verlangt, entscheidet über 15–30 % je Lizenz | Geschäftsmodell | Vor iU13 klären (iF12); Custom Apps über Apple Business Manager entschärfen die Frage |
| **iR8** | **`RecordSet` bleibt an OleDb gebunden.** Die Bauanleitung stellt in S4b nur das Innere um — „die öffentliche Fläche bleibt", und die ist `public OleDbCommand DBCommand { get; set; }`. Dazu string-konkateniertes SQL, das die zentrale Parameterübersetzung umgeht | Für Windows folgenlos, **für iOS ein harter Blocker** bei 47 echten Nutzern | In iU6 als eigener Posten führen: Property auf einen eigenen Typ heben **oder** `RecordSet` in iU9 mit seinen Masken ablösen |
| **iR9** | **Nur ein Rechner, nur ein Mensch.** Tags, Referenzbasen und die einzige Buildumgebung hängen heute an einem Arbeitsplatz (`letzter-x86-stand` ist bis heute nicht gepusht) | Ausfallrisiko für das gesamte Vorhaben | Die CI ist zugleich die Antwort darauf: Sie macht den Build reproduzierbar und vom Einzelrechner unabhängig |
| **iR10** | **`Form_Simulation_Detail`** wächst schneller als die Umstellung (6.200 → 7.773 Zeilen in vier Monaten) | Das größte Einzelstück wird nie fertig konvertiert | In iU9 nicht konvertieren, sondern zerlegen — und dafür einen eigenen Termin setzen, bevor es weiter wächst |

---

## 8 Entscheidungsbedarf

### 8.1 Vorbedingungen aus dem Grundlagenkonzept

Diese Fragen sind dort gestellt und empfohlen, aber noch nicht beschieden. **iU4 und alles Weitere
setzen sie voraus:**

| Nr. | Frage | Empfehlung dort | hier benötigt ab |
|---|---|---|---|
| iF1 | S0-Spike beauftragen? | ja | iU3 |
| iF2 | Voller Funktionsumfang oder erste Auslieferung ohne Katalog-Admin? | ohne Katalog-Admin | iU11 |
| iF3 | Blazor Hybrid oder MAUI-XAML? | Blazor Hybrid | iU8 |
| iF4 | S1/S2 unabhängig vom iOS-Ziel einplanen? | ja | iU1 |
| iF5 | Vertriebsweg | zunächst TestFlight | **präzisiert in § 3.4** — TestFlight ist kein Auslieferungsweg (90 Tage) |
| iF6 | Windows-Charts ebenfalls auf ScottPlot? | mittelfristig, nicht Teil des Vorhabens | iU7 |
| iF7 | Formular-Generator als Werkzeug? | ja | iU8 |
| iF8 | **Modell C beschließen** | ja | **iU8 — ohne diesen Beschluss ist iU8 gegenstandslos** |
| iF9 | **SQLite auch auf Windows, mit Stichtag** | ja, nach S2 | **Etappe 2 — siehe iF10** |

### 8.2 Neue Fragen aus dieser Prüfung

| Nr. | Frage | Empfehlung |
|---|---|---|
| **iF10** | **Bekommt `IDatenzugriff` einen providerneutralen Parametertyp** (`DbParam`), während `DataRepository` seine OleDb-Fassade als Windows-Adapter behält — Weg (c) aus § 1.4? Und: **bekommt der SQLite-Stichtag ein Datum**, sobald Schritt 62 entschieden ist? | **Ja zu beidem.** Weg (c) kostet in S4 fast nichts, lässt die ~2.300 Altaufrufe unangetastet und macht den Kern trotzdem iOS-fähig; ohne ihn ist die iOS-Datenschicht ein eigenes Paket. Der Stichtag ist nach dem Stand vom 01.09. greifbar — nur Schritt 62 steht noch aus (iR1) |
| **iF11** | Mac-Hardware sofort beschaffen — oder iU3 auf einem `macos-latest`-CI-Runner fahren und den Mac erst mit iU10 kaufen? | **CI-Runner für den Spike.** Verschiebt eine vierstellige Investition hinter das Go/No-Go-Gate, ohne den Beweis zu schwächen |
| **iF12** | Vertriebsweg für die Auslieferung: Custom Apps über Apple Business Manager, Unlisted App oder öffentlicher App Store — und wie wird der Lizenzverkauf gegenüber Apples Kaufregeln behandelt? | **Custom Apps** prüfen: passt zum B2B-Kundenkreis und entschärft die Provisionsfrage. Klärung **vor** iU13, nicht im Review |
| **iF13** | Wird der Root-Namespace `WindowsFormsApplication1` beim Kern-Umzug mit umbenannt? | **Nein** — der Umzug bleibt lesbar; die Umbenennung ist ein eigener mechanischer Schritt danach |
| **iF14** | Wird eine anonymisierte `Kenndaten_Test.sqlite` mit den 13 Referenzprojekten versioniert? | **Ja.** Ohne sie ist die Kern-CI ein Kompilierungstest (iR6). `sqlite-probe/EPOS_Beispiel.sqlite` ist der akzeptierte Präzedenzfall |
| **iF15** | Wie ist „wertgleich" zwischen x64 und ARM64 definiert? | **Bestehende Toleranz** (rel. 1e-4 / abs. 0,01) für den Plattformvergleich; **Byte-Gleichheit** bleibt Maßstab für Windows-interne Umbauten |
| **iF16** | Chart-Weg in Blazor Hybrid: ScottPlot als Bild, JavaScript-Bibliothek oder natives Steuerelement? | **ScottPlot als Bild** — ein Stack für Bericht und Bildschirm; Interaktivität nur dort nachrüsten, wo sie fachlich gebraucht wird |
| **iF17** | Wird iU1 (Fundament, .NET 10, CI, COM-Entfernung) **unabhängig vom iOS-Beschluss** beauftragt? | **Ja.** Die Support-Frist läuft am 10.11.2026 ab; das Paket ist auch ohne iOS vollständig gerechtfertigt und die einzige Antwort auf iR9 |

---

## 9 Anhang

### 9.1 Fundstellen

| Thema | Fundstelle |
|---|---|
| COM-Sperre, Buildregel | `WindowsFormsApplication1/WindowsFormsApplication1.csproj` (COMReference); `WindowsFormsApplication1/CLAUDE.md` |
| Muster für den Kernschnitt | `SpeicherEngine/SpeicherEngine.csproj`, `KiKern/KiKern.csproj` (Kopfkommentare) |
| Rechenkern | `WindowsFormsApplication1/Allgemein/BhkwPlan.cs` (410 Z.) |
| Datenzugriff | `WindowsFormsApplication1/Allgemein/DataRepository.cs` (436 Z., 17 öffentliche Mitglieder); `Allgemein/RecordSet.cs` (153 Z., `DBCommand` in Z. 9) |
| Schemapflege | `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` (13.589 Z., 61 Schritte, `ZIEL_VERSION` Z. 112); `SchemaKatalog.cs` (3.461 Z.) |
| Chart-Blocker | `WindowsFormsApplication1/Allgemein/Bericht/ChartRenderer.cs` (821 Z.) |
| Excel-COM | `WindowsFormsApplication1/Allgemein/ToolsClass.cs`, `Allgemein/Import/GanglinienDatei.cs` |
| Lizenz | `WindowsFormsApplication1/Allgemein/Lizenz/` (`LizenzToken.cs`, `GeraeteId.cs`, `LizenzServerClient.cs`, `LizenzManager.cs`); `Lizenzserver/` (PHP) |
| Größtes Einzelstück | `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Detail.cs` (7.773 Z.) |
| Referenzlauf | `Referenzlauf/` (9 `.cs`), `Referenzlaeufe/LIESMICH.md`, Basis `Referenzlaeufe/2026-08-30_B3-Kaskade/` |
| Freigabekette | `Setup/build-setup.ps1`, `Setup/EPOS-Plan.iss` (Z. 29 `AppExeName`), `Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md` |
| SQLite-Probe | `sqlite-probe/LIESMICH.md`, `sqlite-probe/aufbau.sql`, `sqlite-probe/EPOS_Beispiel.sqlite` |
| SQLite-Umsetzung (Branch `sqlite`) | `Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md` (644 Z.), `sql/tools/Erzeuge-Schema.ps1` (874 Z.), `sql/S0_Protokoll_Rechner1_2026-09-01.md` |
| Kodierungsregel | `.editorconfig` (bewusst ohne globales `charset`) |
| Synchronisation | `GitHub_Sync.bat` |

### 9.2 Kürzelglossar

| Familie | Bedeutung | Herkunft |
|---|---|---|
| **iU0–iU13** | Arbeitspakete | **dieses Dokument**, § 4 |
| **iE1–iE10** | Bausteine der Entwicklungsumgebung | **dieses Dokument**, § 3.10 |
| **iZ1–iZ7** | Meilensteine | **dieses Dokument**, § 4.1 |
| **iT1–iT10** | Nachweise | **dieses Dokument**, § 6 |
| **iR1–iR10** | Risiken | **dieses Dokument**, § 7 |
| iL1–iL8 | Leitentscheidungen der Portierung | `Konzept_iOS-Portierung_EPOS-Plan.md` § 3 |
| iF1–iF9 | Entscheidungsfragen | ebenda § 7 · **iF10–iF17 neu in § 8.2** |
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
