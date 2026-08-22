# Umstellung auf 64 Bit (x64) — Analyse und Vorgehensweise

Stand: 22.08.2026 — P0 bis P3 abgeschlossen. P1: Commits `b4f5543` + `14777bc`.
P2: Commit `31d5406` (Rückweg-Tag `letzter-x86-stand` = `3f126f4`); Solution baut
und startet unter x64. P3: Referenzvergleich x86 ↔ x64 **GESAMT PASS**
(9 Projekte, 2.427.467 Werte in Toleranz — Beleg in
`Referenzlaeufe/2026-08-22_P3_x64/vergleich_x86_x64.md`); der UI-Funktionsdurchlauf
(Prüfliste 5–8) bleibt manuelle Abnahme. Offen: P4 (in Arbeit), P5.
Betrachtet wurden der gesamte Bestand unter `WindowsFormsApplication1`
(ohne Altkopien und Worktrees), die Solution, alle Nebenwerkzeuge, der Installer unter
`Setup\` sowie die aktuelle Faktenlage zur 64-bit Access Database Engine (Quellen in
Abschnitt 9).

**Aufgabe.** EPOS-Plan wird heute zwingend als 32-bit-Anwendung (x86) gebaut und
ausgeliefert, weil der In-Process-Datenbankprovider `Microsoft.ACE.OLEDB.12.0` zur
Prozess-Bitness passen muss. Die Anwendung soll auf **x64** umgestellt werden.

**Entscheidung (21.08.2026):** Die Umstellung erfolgt **vollständig auf x64, ohne
x86** — kein Doppelgleis, kein x86-Setup, keine x86-Konfigurationen im Bestand
(Abschnitt 5.1). Der Rückweg führt ausschließlich über die Git-Historie
(Tag vor der Umstellung, P2.1). Am 22.08.2026 wurden auch die Umsetzungsdetails
5.2–5.4 wie vorgeschlagen entschieden — **P0 ist abgeschlossen**, die Umsetzung
beginnt mit P1.

Dieses Dokument ist reine Analyse und Planung — es wurde noch keine Zeile Code geändert.

---

## 1. Kurzfassung des Befunds

**Im Quellcode gibt es keinen einzigen x64-Blocker.** Der Rechenkern ist vollständig
verwaltetes C#, es existiert genau ein P/Invoke (gegen `user32.dll`, korrekt typisiert),
kein ActiveX, kein `unsafe`, kein `BinaryFormatter`, keine eigene native DLL. Die beiden
einzigen NuGet-Pakete mit nativen Binaries (SkiaSharp, HarfBuzzSharp) liefern win-x64
mit — die 64-bit-DLLs liegen schon heute ungenutzt im x86-Build-Output. Excel-Interop
läuft out-of-process und ist bitness-tolerant.

**Die Build-Seite ist bereits vorbereitet:** Solution und Haupt-csproj kennen die
Plattform x64 seit jeher (`Platforms x86;x64;AnyCPU` mit bedingtem `PlatformTarget`);
auf dem Entwicklungsrechner wurde laut `Konzept_Etappe3b_Formularsteuerung.md:116`
zeitweise sogar mit `Platform=x64` gearbeitet, und der 64-bit-ACE-Provider ist dort
vorhanden (64-bit-Office C2R).

**Die eigentliche Arbeit liegt in drei Ecken:**

1. **Installer und Verteilung** (`Setup\EPOS-Plan.iss`, `build-setup.ps1`) — komplett
   auf `win-x86` verdrahtet: 32-Bit-Installationsmodus, 32-bit-ACE-Redist, Prüfung in
   der 32-Bit-Registry, `HKLM32`-Schlüssel. Dazu die Übernahme bestehender
   32-bit-Installationen (sonst entstehen zwei Einträge unter „Apps und Features").
2. **Ein Registry-Zugriff mit Verhaltensänderung** — der KI-Abschalter
   (`KiEinwilligung.cs:82`) liest `HKLM\Software\wp-plan` ohne `RegistryView` und würde
   nach der Umstellung in einer anderen Registry-Sicht suchen als bisher.
3. **Verifikation** — Referenzlauf x86 gegen x64 (Gleitkomma-Toleranz), Funktions­durchlauf,
   Setup-Testmatrix.

**Die Datenlage macht die Umstellung risikoarm:** `Kenndaten.accdb` ist dateikompatibel,
es gibt keinerlei Datenmigration, die Geräte-ID der Lizenz bleibt unverändert (liest die
`MachineGuid` bereits heute explizit aus der 64-Bit-Registry-Sicht), und ein Rückweg
bleibt über die Git-Historie möglich (Tag vor der Umstellung, P2.1; die `.accdb` ist in
beide Richtungen dateikompatibel).

**Die Grundsatzentscheidung ist getroffen** (5.1, 21.08.2026): vollständige Umstellung
auf x64, ohne x86. Für Bestandskunden mit 32-bit-Office gibt es damit bewusst kein
x86-Setup mehr als Ausweich — dort bleibt nur der Wechsel auf 64-bit-Office oder der
dokumentierte KB-5004577-Weg (Hinweisdialog im Setup, P4.2). Das ist dieselbe Reibung,
die das heutige x86-Setup bereits in Gegenrichtung hat (sein eigener Fehlertext nennt
„ein installiertes 64-Bit-Microsoft-Office" als häufigste Ursache,
`EPOS-Plan.iss:186-187`). Auch 5.2–5.4 sind entschieden (22.08.2026, Vorschläge
unverändert angenommen) — es gibt keine offenen Entscheidungen mehr.

---

## 2. Ist-Stand: Wo x86 heute festgezurrt ist

### 2.1 Build — vorbereitet, Default ist x86

| Fundstelle | Inhalt |
|---|---|
| `WindowsFormsApplication1\WindowsFormsApplication1.csproj:21-22` | Kommentar „WICHTIG: x86, weil Microsoft.ACE.OLEDB.12.0 bitness-gebunden ist" + `<PlatformTarget>x86</PlatformTarget>` als Default |
| `…csproj:23` | `<Platforms>x86;x64;AnyCPU</Platforms>` — x64 ist angelegt |
| `…csproj:50-55` | bedingte PropertyGroups: `Platform==x64 → PlatformTarget x64`, `AnyCPU → AnyCPU` |
| `WP-Plan.sln:17-22` | Solution-Konfigurationen Debug/Release × **x86/x64** (kein Any CPU auf Solution-Ebene) |
| `WP-Plan.sln:23-63` | nur das Hauptprojekt folgt der Solution-Plattform; SpeicherEngine, KiKern und beide Testprojekte bauen immer als Any CPU — **an der Solution ist nichts zu ändern** |
| `WindowsFormsApplication1\CLAUDE.md:13` | Buildbefehl `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x86` |
| `CLAUDE.md:12` (Wurzel) | „Build zwingend **x86** (ACE OLEDB)" |
| `.claude\settings.local.json:9-11` | hinterlegte MSBuild-/Pfad-Berechtigungen mit `Platform=x86` bzw. `bin\x86\…` |

Nicht gesetzt (und damit unkritisch): `Prefer32Bit`, `RuntimeIdentifier`,
`Directory.Build.props`, CI-Pipelines (`.github\workflows` existiert nicht).

### 2.2 Nebenwerkzeuge — auf x86 fixiert

| Projekt | Fundstelle | Besonderheit |
|---|---|---|
| Referenzlauf | `Referenzlauf\Referenzlauf.csproj:22-24` | `<Platforms>x86</Platforms>` — **nur** x86 definiert; Build nur über VS-MSBuild (MSB4803, `:9-13`) |
| KiHarnisch | `KiHarnisch\KiHarnisch.csproj:31-32` | x86 |
| Zahlen-Harness | `dev\harness_zahlenpruefung\paket1_12formulare\Harness.csproj:6` | x86 |
| CSExeCOMServer | `CSExeCOMServer\CSExeCOMServer.csproj` | Altbestand, nicht mehr referenziert — bleibt unangetastet |

### 2.3 Installer und Verteilung — vollständig 32-bittig

`Setup\EPOS-Plan.iss` (Inno Setup ≥ 6.3):

- `:37-40` — Quelle ist `artifacts\publish\win-x86` (Ergebnis von
  `dotnet publish -r win-x86 --self-contained true`)
- `:97-100` — `ArchitecturesAllowed=x86compatible`, `ArchitecturesInstallIn64BitMode`
  bewusst **nicht** gesetzt → Installation nach `Programme (x86)`
- `:46-47` — beigelegte `AccessDatabaseEngine.exe` = **32-bit** ADE 2016
  (Original liegt in der Repo-Wurzel)
- `:326-329` — `AceVorhanden()` prüft `HKCR32` auf die ProgID `Microsoft.ACE.OLEDB.12.0`
  (nur ProgID, ohne CLSID-/`InprocServer32`-Kette)
- `:274-278` — Redist-Installation per `/quiet`, `:336-340` Nachprüfung
- `:186-187` — Fehlertext: „Häufigste Ursache ist ein installiertes
  64-Bit-Microsoft-Office, das die 32-Bit-Engine blockiert."
- `:259-264` — `HKLM32\SOFTWARE\INEKON\EPOS-Plan` (`InstallDir`, `Version`)

`Setup\build-setup.ps1`:

- `:151-158` — `dotnet publish … -r win-x86 --self-contained true -p:Platform=x86`
- `:61` — `$PublishDir = artifacts\publish\win-x86`
- `:218` — signtool wird gezielt im x86-Zweig des Windows-SDK gesucht

Dazu `Setup\Konzept_Setup_InnoSetup_EPOS-Plan.md` (u. a. `:48`, `:131`, `:473`) mit der
damaligen Festlegung „zwingend x86" — inklusive der inzwischen überholten Antwort auf
die eigene Frage „Wird noch ein 64-Bit-Stand gebraucht? … nein" (`:473`).

### 2.4 Datenzugriff — eine zentrale Stelle, eine Ausnahme, ODBC ist tot

- `Allgemein\DataRepository.cs:165` — **einziger** produktiver ConnectionString-Bau:
  `Provider=Microsoft.ACE.OLEDB.12.0;Data Source={GetDBPath()};` — kein Fallback,
  keine Provider-Erkennung.
- `Views\Kosten\Form_KostenfaktorItem.cs:47-50` — einzige Stelle mit **eigenem**
  Provider-String (nutzt immerhin `GetDBPath()`).
- `app.config:14` und `Properties\Settings.Designer.cs:29` — toter Beispiel-
  ConnectionString mit Uralt-Pfad (`C:\Users\wg008\…`), laut Projekt-CLAUDE.md unbenutzt.
- **ODBC existiert nicht mehr:** `Allgemein\RecordSet.cs` arbeitet längst mit
  `OleDbConnection` (`:9-11`, `:34`, `:59`); die einzige ODBC-Codedatei
  `Controller\WPTestCtrl.cs` ist per `csproj:60` vom Build ausgeschlossen und wäre
  (wegen des entfernten `Program.DBConnection`) nicht einmal mehr übersetzbar. Das Paket
  `System.Data.Odbc` (`csproj:98`) ist eine Karteileiche. **Der ODBC-Absatz in
  `WindowsFormsApplication1\CLAUDE.md` („RecordSet.cs (ODBC …)") ist veraltet.**
- 14 Aufrufe von `GetOleDbSchemaTable` (u. a. `Controller\ErgebnisCtrl.cs`,
  `WirtschaftlichkeitCtrl.cs:434`) — ACE-gebunden, aber API-identisch unter x64.

### 2.5 COM, P/Invoke, native Pakete — unkritisch

- **Excel-Interop** (einzige COM-Nutzung): `Allgemein\ToolsClass.cs:41` (Klimadaten-Import)
  und `Allgemein\Import\GanglinienDatei.cs:936` (Ganglinien-Import), jeweils
  `new Microsoft.Office.Interop.Excel.Application()` → Out-of-Process-COM, die Bitness
  des installierten Excel ist **egal**. `VBIDE` ist nur COM-Referenz ohne Codenutzung
  (`csproj:140-148`). Beide mit `EmbedInteropTypes=True` — keine PIA-Verteilung nötig.
- **P/Invoke:** genau einer — `Allgemein\GrafikTools\TextBoxExtensionsClass.cs:11`
  (`user32.dll`/`SendMessage`, Cue-Banner), Handles korrekt als `IntPtr`.
- **Kein** ActiveX/AxHost/OCX, kein `unsafe`, kein `StructLayout`/`Marshal`-Interop,
  kein `BinaryFormatter`, kein WebBrowser/WebView2, keine XAML-Dateien trotz `UseWPF`.
- **CSExeCOMServer / BHKWPLAN.DLL:** nur auskommentierte bzw. dokumentarische Reste
  (`SimulationStrombedarf.cs:32`, `SimulationWaermebedarf.cs:76`,
  `SimulationWaermepumpe.cs:234`, `BhkwPlan.cs:9`).
- **Native NuGet-Assets:** nur `SkiaSharp.NativeAssets.Win32` 3.119.0 und
  `HarfBuzzSharp.NativeAssets.Win32` 8.3.1.1 — beide bringen win-x86 **und** win-x64
  mit; im heutigen Output liegt `runtimes\win-x64\native\libSkiaSharp.dll` bereits bei.
  Einziger Konsument ist ScottPlot (`Views\Stromspeicher\Form_SpeicherOptimierung.cs`).
  `System.Data.OleDb` selbst ist verwaltet und RID-neutral.

### 2.6 Registry — eine echte Verhaltensänderung, ein bereits gelöster Fall

| Stelle | Verhalten heute (x86) | Verhalten nach x64 |
|---|---|---|
| `Allgemein\KI\KiEinwilligung.cs:82` — liest `HKLM\Software\wp-plan\KiDeaktiviert` **ohne** `RegistryView` | WOW6432Node-Umleitung: gelesen wird real `HKLM\SOFTWARE\WOW6432Node\wp-plan` | gelesen wird `HKLM\SOFTWARE\wp-plan` — ein von Admins unter WOW6432Node gesetzter Abschalter **wirkt nicht mehr** |
| `Allgemein\Lizenz\GeraeteId.cs:54-58` — `MachineGuid` | liest **explizit** `RegistryView.Registry64` (Kommentar `:46-48` erklärt warum) | unverändert → **Geräte-ID stabil, Lizenz-Token bleiben gültig** |
| alle HKCU-Zugriffe (`Software\wp-plan`, `Software\EPOS_PLAN\…`) | keine Umleitung | unverändert |
| Installer `HKLM32\SOFTWARE\INEKON\EPOS-Plan` (`EPOS-Plan.iss:259-264`) | 32-Bit-Sicht | ein 64-Bit-Setup schreibt die 64-Bit-Sicht; Alt-Erkennung muss beide Sichten kennen |

Randnotiz: Der Ressourcentext (`MyResource\Resource.Designer.cs:4166`) nennt den Pfad
wörtlich „HKLM\Software\wp-plan\KiDeaktiviert" — unter x64 stimmt er erstmals wörtlich.

DPAPI ist bitness-neutral: Lizenz-Token (`LizenzManager.cs`, `LocalMachine`-Scope) und
KI-Schlüssel (`KiChatService.cs:1386-1417`, `CurrentUser`, `%APPDATA%\wp-plan`) bleiben
ohne Zutun lesbar.

---

## 3. Faktenlage 64-bit Access Database Engine (Stand 2026)

Kurzfassung der externen Recherche (Quellen: Abschnitt 9), verifiziert per
Registry-Befund auf dem Entwicklungsrechner:

1. **Der ConnectionString kann unverändert bleiben.** Die *Access Database Engine 2016
   Redistributable x64* registriert **beide** ProgIDs — `Microsoft.ACE.OLEDB.12.0`
   (CLSID `{3BE786A0-0366-4F5C-9434-25CF162E475E}`) und `…16.0` — auf dieselbe
   `ACEOLEDB.DLL`; Microsoft empfiehlt selbst weiterhin 12.0. Gleiches gilt für
   64-bit-Office (C2R) mit Access: Der Provider ist dann systemweit registriert, eine
   Redist ist nicht nötig (Ausnahme: Office 2019 Volumenlizenz).
2. **64-bit-Office ist seit 2019 der Installations-Default.** Neue Kundenrechner haben
   also typischerweise 64-bit-Office — genau die Konstellation, in der das **heutige**
   x86-Setup seine 32-bit-Engine nur über den nicht unterstützten `/quiet`-Weg
   daneben bekommt. Die Umstellung auf x64 löst die Reibung für die wachsende Mehrheit
   und verschiebt sie auf die schrumpfende Minderheit mit 32-bit-Office.
3. **Mischbitness bleibt offiziell nicht unterstützt** — 64-bit-Engine neben
   32-bit-Office geht nur über den in KB 5004577 dokumentierten `/quiet`-Workaround
   (dokumentiert, aber nicht „supported"; Office-Reparaturen/-Updates können die
   Registrierung wieder zerstören).
4. **Lebenszyklus:** Der Support der ADE 2016 Redistributable endete am 14.10.2025; sie
   ist weiter herunterladbar und funktionsfähig, Microsofts Nachfolger ist die
   *Microsoft 365 Access Runtime* — die allerdings die Bitness eines vorhandenen
   C2R-Office erzwingt und daher kein Weg an 32-bit-Office vorbei ist.
5. **Robuste Erkennung:** Nicht nur die ProgID prüfen (kann als Leiche ohne Server
   dastehen — auf dem Entwicklungsrechner exakt so vorhanden), sondern die Kette
   ProgID → CLSID → `InprocServer32` → DLL-Datei existiert, in der **64-Bit-Sicht**.
6. **Keine funktionalen Engine-Unterschiede** für reine `.accdb`-Nutzung (Tabellen +
   gespeicherte `Abfrage_*`, kein VBA). Der einzige harte Blocker wäre
   `Microsoft.Jet.OLEDB.4.0` (nur 32-bit) — kommt im Code nicht vor. Das
   `.laccdb`-Locking ist dateibasiert und bitness-übergreifend; die Regeln aus
   `BETRIEB_Mehrbenutzer_Datenbank.md` gelten unverändert.

**Was sich für Anwender ändert:** nichts an den Daten. `Kenndaten.accdb` ist
dateikompatibel, es gibt keine Migration, und selbst ein Rückwechsel auf einen alten
x86-Programmstand (Git-Historie) wäre ohne Datenverlust möglich.

---

## 4. Gleitkomma: x86- und x64-Ergebnisse können minimal abweichen

Der Rechenkern (`Allgemein\BhkwPlan.cs`, `float`-Vektoren mit `double`-Zwischenrechnung)
läuft unter .NET 8 auf beiden Plattformen über RyuJIT/SSE2 — aber der x64-JIT darf
zusätzlich FMA/AVX-Instruktionen einsetzen. Ergebnisse können sich dadurch in den
letzten Mantissenbits unterscheiden; über 8760-Stunden-Summen kann das sichtbar werden
(typisch < 1e-4 relativ, aber nicht bitgenau).

Konsequenz für die Abnahme: Der Vergleich der Referenzläufe x86 ↔ x64 braucht eine
**definierte Toleranz** statt Bitgleichheit. Zum Eingrenzen echter Fehler lässt sich der
x64-Lauf diagnostisch mit `DOTNET_EnableFMA=0` (bzw. `DOTNET_EnableAVX=0`) wiederholen —
verschwindet die Abweichung, ist es der erwartete Instruktions-Effekt, kein Bug. Diese
Schalter sind reine Diagnose, keine Dauerlösung.

---

## 5. Entscheidungen

### 5.1 Nur noch x64 — oder Übergangs-Doppelgleis? *(entschieden: nur x64)*

| Variante | Wirkung | Kosten |
|---|---|---|
| **A — x64 als einziges Release** | ein Build, ein Setup, klare Ansage; Kunden mit 32-bit-Office brauchen den KB-5004577-Weg oder ein Office-Update | Risiko nur bei 32-bit-Office-Bestand |
| **B — Dual-Release x86 + x64** | niemand bleibt zurück; Installer je Bitness | doppelte Publish-/Setup-/Testkette **auf Dauer** |
| **C — AnyCPU** | entscheidet nichts, verlagert das Problem nur zur Laufzeit (läuft auf 64-bit-Windows als x64) | scheidet aus |

**Entschieden (21.08.2026): Variante A in der konsequenten Form — vollständig x64,
ohne x86.** Kein Bitness-Parameter im Build-Skript, kein x86-Setup für Sonderfälle;
die x86-/AnyCPU-Konfigurationen werden aus Solution, Haupt-csproj und Nebenwerkzeugen
entfernt (P2). Als Rückweg genügt die Git-Historie: Vor der Umstellung wird ein Tag
gesetzt (P2.1); von dort ist der letzte x86-Stand jederzeit wieder baubar, und die
`.accdb` ist in beide Richtungen dateikompatibel. Für Kunden mit 32-bit-Office gibt es
bewusst keinen Ausweich-Build — nur den Wechsel auf 64-bit-Office oder den
KB-5004577-Weg (Hinweisdialog im Setup, P4.2).

### 5.2 Umgang mit bestehenden 32-bit-Installationen beim Update *(entschieden)*

Ein x64-Setup installiert nach `Programme` statt `Programme (x86)`, und Inno Setup legt
seinen Uninstall-Eintrag in der 64-Bit-Registry-Sicht an — die vorhandene
32-bit-Installation wird also **nicht** automatisch als dieselbe Anwendung erkannt:
Ergebnis wären zwei Einträge unter „Apps und Features" und zwei Programmordner.

**Entschieden (22.08.2026):** Das x64-Setup erkennt die 32-bit-Installation
(Uninstall-Key derselben AppId in der 32-Bit-Sicht bzw.
`HKLM32\SOFTWARE\INEKON\EPOS-Plan`) und **deinstalliert sie still** vor der eigenen
Installation. Die Nutzdaten sind davon nicht berührt — sie
liegen in `%ProgramData%\EPOS_PLAN` (Datenbank) und `%APPDATA%\wp-plan` (Lizenz,
KI-Schlüssel), beides bitness-neutral; der alte Uninstaller fasst laut
`EPOS-Plan.iss:300-307` nur `{app}` an.

### 5.3 Welche Redist wird beigelegt? *(entschieden: ADE 2016 x64)*

**Entschieden (22.08.2026):** **ADE 2016 Redistributable x64** beilegen (funktioniert, registriert 12.0,
identisches Setup-Muster wie heute) und im Setup-Konzept dokumentieren, dass die
*Microsoft 365 Access Runtime* der designierte Nachfolger ist (EOL der ADE 2016 im
Oktober 2025 ist bekannt und akzeptiert). Die vorhandene 32-bit-`AccessDatabaseEngine.exe`
in der Repo-Wurzel wird durch die x64-Fassung ersetzt — einen x86-Sonderfall gibt es
nach Entscheidung 5.1 nicht mehr; zur Eindeutigkeit heißt die neue Datei
`AccessDatabaseEngine_X64.exe`, die alte wird in P5 entfernt.

### 5.4 Aufräumen im selben Zug? *(entschieden: ja)*

Kleiner, risikoarmer Beifang, der die Umstellung sauberer macht: Paket
`System.Data.Odbc` entfernen, `Controller\WPTestCtrl.cs` löschen (ist ohnehin nicht
übersetzbar), toten `KenndatenConnectionString` aus `app.config`/Settings nehmen,
`Form_KostenfaktorItem` auf `DataRepository.GetConnectionString()` umstellen.
**Entschieden (22.08.2026):** ja — als Teil von P1, getrennt committen.

---

## 6. Vorgehensweise

Sechs Pakete. P1–P3 machen die Anwendung selbst x64-fähig und nachweisbar korrekt;
P4 stellt die Verteilung um; P5 räumt Doku und Reste auf. P0 ist die Entscheidung.

### P0 — Entscheidungen aus Abschnitt 5 *(abgeschlossen 22.08.2026)*

Alle vier Entscheidungen sind getroffen: **5.1** vollständig x64, ohne x86
(21.08.2026); **5.2** stille Deinstallation der 32-bit-Vorinstallation; **5.3**
ADE 2016 Redistributable x64 als beigelegte Voraussetzung; **5.4** Aufräum-Beifang in
P1 (5.2–5.4 am 22.08.2026, Vorschläge unverändert angenommen). Die Umsetzung beginnt
mit P1.

### P1 — Code robust machen *(umgesetzt 22.08.2026, Commits `b4f5543` + `14777bc`)*

Alle vier Punkte sind umgesetzt und mit `Platform=x86` gebaut (VS-MSBuild; `dotnet build`
scheitert seit dem .NET-10-Preview-SDK auch beim Hauptprojekt an MSB4803).
Die HKLM-Doppelsicht liest über die neue Hilfsmethode `LesenMaschine(RegistryView, …)`
in `KiEinwilligung.cs`; die Startprüfung heißt `DataRepository.ProviderVorhanden()`
und hängt in `Program.Main` direkt nach der Sprachwahl. Der Praxistest der
Fehlermeldung auf einem Rechner ohne ACE bleibt Prüfpunkt 3 der Abnahme.

1. **`KiEinwilligung.cs`**: HKLM-Lesung des Abschalters auf **beide** Registry-Sichten
   erweitern (erst 64, dann 32-Bit-Sicht; ein Treffer genügt). Damit wirken vorhandene
   Admin-Einträge aus der x86-Ära weiter, und der Schalter funktioniert in beiden
   App-Bitnessen identisch. (Datei-Kodierung vor dem Edit prüfen — Fallstricke, Abschn. 8.)
2. **`Form_KostenfaktorItem.cs:47-50`**: eigenen Provider-String durch
   `DataRepository.GetConnectionString()` ersetzen → der Provider steht danach nur noch
   an einer Stelle im Code.
3. **Startprüfung ACE-Provider**: beim Programmstart (vor dem ersten DB-Zugriff) eine
   Probe-Verbindung öffnen; schlägt sie mit „Provider nicht registriert"
   (0x80040154) fehl, eine sprechende, zweisprachige Meldung zeigen („Die
   64-Bit-Access-Datenbank-Engine fehlt …", Verweis auf Setup/Redist) statt einer
   nackten `OleDbException`. Texte nach der Drei-Schichten-Regel über
   `MyResource.Resource.*` in beiden Satelliten-`.resx`.
4. Aufräum-Beifang aus 5.4 (entschieden: ja) — eigener Commit.

### P2 — Build und Werkzeuge vollständig auf x64 (Entscheidung 5.1)

1. **Git-Tag setzen** (z. B. `letzter-x86-stand`) — einziger Rückweg und zugleich der
   Referenzpunkt, von dem der x86-Vergleichslauf in P3 gebaut wird.
2. **`WindowsFormsApplication1.csproj`**: `<Platforms>x64</Platforms>`,
   `PlatformTarget` fest auf `x64`; die bedingten PropertyGroups für x64/AnyCPU
   entfallen. Kommentar in `:21` ersetzen („x64 — ACE muss als 64-bit-Engine
   vorliegen").
3. **`WP-Plan.sln`**: Solution-Konfigurationen `Debug|x86`/`Release|x86` samt
   Projekt-Mappings entfernen — übrig bleiben Debug/Release × x64.
4. **Nebenwerkzeuge**: `Referenzlauf.csproj`, `KiHarnisch.csproj` und
   `dev\…\Harness.csproj` auf `x64` umstellen (der x86-Vergleichslauf in P3 wird vom
   getaggten Altstand gebaut, nicht vom neuen).
5. **Buildbefehle/Berechtigungen**: `WindowsFormsApplication1\CLAUDE.md:13` auf
   `-p:Platform=x64` ändern; `.claude\settings.local.json` auf die x64-Varianten der
   MSBuild-/Pfad-Einträge umstellen.
6. Lokal `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64`, Anwendung starten,
   Projekt öffnen — der 64-bit-Provider ist auf dem Entwicklungsrechner vorhanden.

### P3 — Verifikation *(der Kern der Abnahme, Prüfliste in Abschnitt 7)*

1. **Referenzlauf doppelt**: der x86-Lauf wird vom getaggten Altstand (P2.1) gebaut,
   der x64-Lauf vom neuen Stand — das Referenzlauf-Tool jeweils nur mit VS-MSBuild
   (MSB4803), beide Läufe gegen dieselbe DB-Kopie. Vergleich mit definierter Toleranz;
   Abweichungen nach dem Rezept aus Abschnitt 4 einordnen.
2. **Funktionsdurchlauf** über alle Gewerke-Pfade mit externen Berührpunkten:
   sämtliche Importe (VDI 3805 × 4 Gewerke, CEC/PAN, CSV, Klimadaten-Excel,
   Ganglinien-Excel), Bericht Word + Excel, ChartRenderer, ScottPlot-Dialog
   (`Form_SpeicherOptimierung` — einziger Skia-Konsument), Lizenzaktivierung,
   KI-Chat, Simulation, Wirtschaftlichkeit.
3. **DB-Schreibtest** ausschließlich gegen eine datierte Kopie (`DB-Backup/`-Regel;
   vorher `Kenndaten.laccdb` prüfen).

### P4 — Setup und Verteilung auf x64 *(nach P0)*

1. **`build-setup.ps1`**: RID `win-x64`, `-p:Platform=x64`, `$PublishDir =
   artifacts\publish\win-x64`; signtool-Suche auf den x64-Ordner umstellen. Kein
   Bitness-Parameter — es gibt nur noch win-x64 (Entscheidung 5.1).
2. **`EPOS-Plan.iss`**:
   - `ArchitecturesAllowed=x64compatible` + `ArchitecturesInstallIn64BitMode=x64compatible`
     (deckt auch ARM64-Windows mit x64-Emulation ab); `PublishDir` auf win-x64.
   - `AceVorhanden()` ersetzen durch die robuste Kette aus Abschnitt 3.5 in der
     64-Bit-Sicht: ProgID `Microsoft.ACE.OLEDB.12.0` → CLSID → `InprocServer32` →
     Datei existiert.
   - Beilage `AccessDatabaseEngine_X64.exe` (5.3); `/quiet`-Aufruf und Nachprüfung wie
     gehabt. **Neu davor:** Erkennung „32-bit-Office vorhanden?" — dann nicht blind
     installieren, sondern Hinweisdialog (Engine-Installation neben 32-bit-Office ist
     nicht unterstützt; Optionen nennen).
   - Fehlertexte spiegeln (`:186-187`): häufigste Ursache ist jetzt ein
     **32**-Bit-Office, das die **64**-Bit-Engine blockiert.
   - `HKLM32` → `HKLM` für `SOFTWARE\INEKON\EPOS-Plan`; Übernahme-/Deinstallationslogik
     für die 32-bit-Vorinstallation (5.2), inklusive Lesen der alten Schlüssel aus der
     32-Bit-Sicht.
   - Unverändert lassen: `{commonappdata}\EPOS_PLAN`-Anlage samt `users-modify`-Rechten
     und `icacls`-Reparaturlauf (`:285-289`) — das ACL-Thema aus
     `BETRIEB_Mehrbenutzer_Datenbank.md` ist bitness-unabhängig.
3. **Setup-Testmatrix** (Abschnitt 7, Punkte 8–10).

### P5 — Doku und Reste

- `CLAUDE.md` (Wurzel): „Build zwingend x86" ersetzen; `WindowsFormsApplication1\CLAUDE.md`:
  Buildbefehl, DPI-Absatz unberührt, **veralteten ODBC-Absatz zu `RecordSet.cs`
  korrigieren** (ist seit der OleDb-Umstellung falsch, unabhängig von x64).
- `Setup\Konzept_Setup_InnoSetup_EPOS-Plan.md`: x86-Festlegungen und die Antwort auf
  die 64-Bit-Frage (`:473`) nachziehen.
- Auskommentierte `CSExeCOMServer.SimpleObject`-Zeilen in den drei Simulationsklassen
  entfernen (laut Projekt-CLAUDE.md ohnehin freigegeben).
- **Letzte x86-Reste** (Entscheidung 5.1): alte `bin\x86`-/`obj`-Ausgabezweige löschen,
  die 32-bit-`AccessDatabaseEngine.exe` aus Repo-Wurzel und `Setup\Voraussetzungen\`
  entfernen (ersetzt durch die x64-Fassung, 5.3).

---

## 7. Prüfliste für die Abnahme

| # | Prüfung | Erwartung |
|---|---|---|
| 1 | `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64` und Release-Publish `win-x64` | baut ohne neue Warnungen; Output unter `bin\x64\…` bzw. `artifacts\publish\win-x64` |
| 2 | Start auf Rechner mit 64-bit-ACE, Projekt öffnen, Stammdaten lesen/schreiben | identisches Verhalten zum x86-Stand |
| 3 | Start auf Rechner **ohne** ACE | neue sprechende Meldung aus P1.3 statt roher `OleDbException` (beide Sprachen) |
| 4 | Referenzlauf x86 (getaggter Altstand, P2.1) vs. x64 (neuer Stand), gleiche DB-Kopie | Abweichungen innerhalb der definierten Toleranz; Ausreißer per `DOTNET_EnableFMA=0` als Instruktions-Effekt bestätigt |
| 5 | Alle Importe: VDI 3805 (Kessel, Puffer, Kollektoren, WP), CEC/PAN, CSV, Klimadaten-Excel, Ganglinien-Excel | funktionsgleich; Excel-Import auch mit 64-bit-Office (Excel-Bitness darf beliebig sein) |
| 6 | Bericht Word + Excel, Charts, `Form_SpeicherOptimierung` (ScottPlot/Skia) | Rendering fehlerfrei — beweist, dass die win-x64-Skia-Native geladen wird |
| 7 | Lizenz: vorhandene Aktivierung aus der x86-Ära | Token weiterhin gültig (Geräte-ID unverändert, DPAPI lesbar) — **keine Re-Aktivierung** |
| 8 | KI: Chat funktioniert (DPAPI-Schlüssel), Abschalter `KiDeaktiviert` je einmal unter `HKLM\SOFTWARE\wp-plan` **und** `HKLM\SOFTWARE\WOW6432Node\wp-plan` gesetzt | wird in **beiden** Fällen respektiert (P1.1) |
| 9 | Setup-Matrix: (a) sauberes Win11 ohne Office → Redist wird installiert; (b) 64-bit-Office mit Access → Redist wird übersprungen; (c) 32-bit-Office → Hinweisdialog statt Blindinstallation | jeweils lauffähige Installation bzw. verständlicher Abbruch |
| 10 | Update über bestehende 32-bit-Installation | alter Eintrag/Ordner entfernt, genau **ein** Eintrag in „Apps und Features"; `Kenndaten.accdb`, Lizenz und Einstellungen unangetastet |
| 11 | Mehrbenutzer: zweites Windows-Konto bei geöffneter DB | Verhalten wie dokumentiert in `BETRIEB_Mehrbenutzer_Datenbank.md` (bitness-neutral) |
| 12 | Cue-Banner in Suchfeldern sichtbar | der einzige P/Invoke funktioniert unter x64 |
| 13 | Negativprobe „ohne x86": Solution und alle csproj enthalten keine x86-/AnyCPU-Plattform mehr, `-p:Platform=x86` schlägt fehl, in `Setup\` existiert kein `win-x86`-Pfad, keine 32-bit-Redist im Repo | Entscheidung 5.1 vollständig umgesetzt |

**Vor Schritt 2 ff.:** `Kenndaten.laccdb` prüfen und datierte Kopie der `.accdb` nach
`DB-Backup/` — die Prüfungen schreiben in die Datenbank, und `.accdb` ist von
`.gitignore` ausgeschlossen; ein Rückweg über Git existiert nicht.

---

## 8. Fallstricke bei der Umsetzung

- **Kodierung.** 93 der `.cs`-Dateien sind nicht UTF-8; vor jedem Edit in P1
  (insbesondere `KiEinwilligung.cs`) die Kodierung prüfen und byte-sicher patchen,
  sonst zerschießt der Diff die Datei.
- **Nur in `WindowsFormsApplication1` suchen.** Die Altkopien
  (`..\WindowsFormsApplication1 - Kopie`, `..\mit_Puffer_KI_Lösungsversuch`) und die
  Worktrees unter `.claude\worktrees\` enthalten fast identische Dateien — Treffer
  daraus führen zu Änderungen am falschen Code (dort liegt u. a. noch der alte
  ODBC-Stand mit `Program.DBConnection`).
- **Referenzlauf-Tool nur über VS-MSBuild** bauen (`MSB4803` bei `dotnet build`,
  dokumentiert in `Referenzlauf.csproj:9-13` und `Referenzlaeufe\LIESMICH.md:330`).
  Das Hauptprojekt selbst baut und published dagegen mit `dotnet` — so bleibt es.
- **Alte `bin\x86`-Ausgaben** liegen bis P5 noch auf der Platte —
  Verwechslungsgefahr beim manuellen Testen; in P5 werden sie endgültig entfernt.
- **Nicht am selben Tag mischen:** Ein x86-Prozess und ein x64-Prozess können dieselbe
  `.accdb` zwar gefahrlos nacheinander (und per `.laccdb`-Locking auch gleichzeitig)
  öffnen — für Referenzvergleiche trotzdem immer mit einer definierten DB-Kopie je
  Lauf arbeiten, sonst sind Abweichungen nicht zuordenbar.
- **C2R-Nebenwirkung im Feld:** Office-Updates können die ACE-Registrierung auf den
  virtualisierten VFS-Pfad umbiegen oder `OLEDB_SERVICES` verstellen (Pooling aus,
  im Extremfall Abstürze beim Prozessende). Das trifft x86 heute genauso; die
  Startprüfung aus P1.3 macht solche Fälle wenigstens diagnostizierbar. Bei
  Support-Fällen zuerst die Provider-Registrierung prüfen (Kette aus Abschnitt 3.5).
- **`GitHub_Sync.bat` committet alles** (`git add -A`) — die Setup-Beilagen
  (`Setup\Voraussetzungen\*.exe`, `Setup\Vorlage\*.accdb`, `Setup\Ausgabe\`) sind laut
  Befund **nicht** in der `.gitignore`; vor dem ersten x64-Setup-Build die drei
  Einträge ergänzen, sonst landet die Redist-EXE im Repo.

---

## 9. Aufwand

| Paket | Umfang | Aufwand |
|---|---|---|
| P0 Entscheidungen | alle getroffen (5.1: 21.08.2026, 5.2–5.4: 22.08.2026) | erledigt |
| P1 Code robust | 2 Dateien + Startprüfung + 2 `.resx`, optional Aufräumen | 3–5 h |
| P2 Build/Werkzeuge | Git-Tag, 4 csproj + `WP-Plan.sln`, Doku-Befehle | 1–2 h |
| P3 Verifikation | Referenzläufe + Funktionsdurchlauf + DB-Test | 6–10 h |
| P4 Setup | `.iss` + `build-setup.ps1` + Redist + Testmatrix (3 Umgebungen) | 6–10 h |
| P5 Doku/Reste | 2× CLAUDE.md, Setup-Konzept, Kommentarleichen | 1–2 h |

**Gesamt rund 17–29 h**, davon der größte Teil Verifikation und Setup-Testmatrix.
P1+P2 zusammen ergeben bereits eine lauffähige x64-Anwendung für den Eigenbedarf;
auslieferbar ist der Stand erst nach P3+P4.

---

## 10. Externe Quellen (Abschnitt 3)

- Access Database Engine 2016 Redistributable — Microsoft Download Center:
  <https://www.microsoft.com/en-us/download/details.aspx?id=54920>
- KB 5004577 — offizieller `/quiet`-Workaround für Mischbitness:
  <https://learn.microsoft.com/en-us/troubleshoot/power-platform/power-automate/desktop-flows/cannot-connect-access-database-engine-ole-db>
- Microsoft 365 Access Runtime (Bitness-Zwang, Office-2019-VL-Einschränkung):
  <https://support.microsoft.com/en-us/access/download-and-install-microsoft-365-access-runtime>
- Office Deployment Tool — x64 als Default seit Office 2019:
  <https://learn.microsoft.com/en-us/microsoft-365-apps/deploy/office-deployment-tool-configuration-options>
- Jet OLE DB / ODBC nur 32-bit (Blocker-Referenz, hier nicht betroffen):
  <https://learn.microsoft.com/en-us/troubleshoot/microsoft-365-apps/access/jet-odbc-driver-available-32-bit-version>
- ADE 2016 x64: `OLEDB_SERVICES`-Bug und C2R-VFS-Override (Microsoft Q&A):
  <https://learn.microsoft.com/en-us/answers/questions/5043822/microsoft-access-database-engine-2016-redistributa>,
  <https://learn.microsoft.com/en-us/answers/questions/5848144/oledbconnection-using-microsoft-ace-oledb-16-0-cau>

## 11. Verwandte Dokumente

- [`CLAUDE.md`](CLAUDE.md) — Datenhaltung, DB-Backup-Regel, ACL-Thema (in P5 anzupassen)
- [`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md) — Build,
  Architektur, Kodierungs-Fallstrick (in P5 anzupassen, ODBC-Absatz veraltet)
- [`Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md`](Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md)
  — bisheriges Setup-Konzept mit der x86-Festlegung
- [`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md) — ACL/Sperrdatei,
  bitness-unabhängig, Prüfpunkt 11
- `Referenzlaeufe/LIESMICH.md` — Bau und Aufruf des Referenzlauf-Werkzeugs (P3.1)
