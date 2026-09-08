# Nachweisliste iU10 — die iOS-Hülle

**Stand 03.09.2026 · Branch `ios_migration` · ab `d4d5e20` (Basis `6f67a32`) —
sieben Schritte iU10-1…iU10-7, dazu die Doku iU10-8**

Paket iU10 des [`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md) (§ 4) ist
umgesetzt, soweit es ohne Mac geht: `EPOS.iOS` steht als MAUI-Blazor-Hybrid-App, die neun
Umgebungsdienste des Kerns haben ihre iOS-Fassungen, die Datenbank kommt beim Erststart in die
Sandbox, und ein Prüfmodus rechnet das Referenzprojekt 1030 auf dem Gerät.

**Diese Liste trennt drei Dinge, und die Trennung ist der Punkt:**

| Spalte | Was sie bedeutet |
|---|---|
| **Linux** | hier geführt, mit Ausgabe im Commit. Abgehakt. |
| **CI** | nur der Job `.github/workflows/ios.yml` auf `macos-26` kann es zeigen. Er läuft **von Hand**. |
| **Gerät** | erst mit einem echten iPad und einem Apple-Konto — Paket iU13. |

> **Der iOS-Bau ist auf Linux und Windows unmöglich.** `dotnet restore` auf ein
> `net10.0-ios`-Ziel endet mit **NETSDK1147** („the following workloads must be installed"), und
> die Workload `maui-ios` gibt es dort nicht — `dotnet workload search` kennt sie auf Linux nicht
> einmal dem Namen nach. Alles unten unter „Linux" ist deshalb bewusst indirekt geführt.

---

## Wie der CI-Job ausgelöst wird

1. Auf GitHub das Repository öffnen → Reiter **Actions**.
2. Links in der Liste der Workflows **iOS** wählen.
3. Rechts **Run workflow** → Branch `ios_migration` → **Run workflow**.

Der Lauf dauert 15–20 Minuten (Workload 3–6, Bau 5–8, Simulator 3–5) und wird bei privaten
Repositories mit **Faktor 10** auf das Minutenkontingent angerechnet. Er läuft zusätzlich von
selbst, wenn ein Push `EPOS.iOS/**`, `.github/workflows/ios.yml` oder `Directory.Packages.props`
berührt — Kern und Oberfläche stehen bewusst **nicht** im Pfadfilter, sie werden bei jedem Push von
`kern.yml` auf ubuntu und macos gebaut, getestet und gegen dieselbe Referenzbasis gerechnet.

Nach dem Lauf liegen zwei Artefakte bereit:

- **`ios-simulator`** — `start.log` (das Startprotokoll der App), `pruefung/` mit den CSV, dem
  `protokoll.txt` und `fertig.txt`, dazu `oberflaeche.png` (Bildschirmabzug des Simulators).
- **`ios-app`** — das gebaute `EPOS.iOS.app`. Hineinsehen lohnt: Größe, mitgelieferte
  `Kenndaten.sqlite`, statisch gelinkte `e_sqlite3`.

---

## Nachweise auf Linux — geführt

### `d4d5e20` — iU10-1: Paketlage

- [x] `dotnet restore WP-Plan.sln` → **0 Fehler**, kein `NU1102`, kein `NU1605`, kein `NU1008`.
- [x] `dotnet build WP-Plan.Kern.slnf -c Release` → **0 Fehler, 3 Warnungen** (unverändert: 2×
      CS0108, 1× CA2255 — alle aus dem Kern, alle vorher schon da).
- [x] `dotnet test WP-Plan.Kern.slnf -c Release` → **929/929** (KiKern 450, SpeicherEngine 337,
      EPOS.UI 107, EPOS.Kern 35).
- [x] `EPOS.Referenzlauf lauf --projekte 1030` gegen `Kenndaten_Test.sqlite`, danach `vergleich`
      gegen `Referenzlaeufe/2026-08-30_B3-Kaskade` → **GESAMT PASS** (22 Dateien, 236 670 Werte),
      `diff -rq` **byte-gleich**. Die Paketkorrektur ändert am Rechenweg nichts — `e_sqlite3` war
      auf Windows und Linux ohnehin schon aktiv.

**Der Befund, der die Zeile ausgelöst hat:** `SQLitePCLRaw.bundle_green` **2.1.12 existiert nicht**
(letzte Fassung 2.1.11, in SQLitePCLRaw 3.0 ganz entfallen). Die seit iU6-T5 vorbereitete Zeile
hätte den ersten iOS-Restore mit `NU1102` gebrochen. Einzelheiten im Entscheidungsregister § 2.9.

### `03e8c0a` — iU10-2: Seiten in `EPOS.UI`

- [x] `dotnet build EPOS.UI/EPOS.UI.csproj -c Release` → **0 Fehler, 0 Warnungen**.
- [x] `dotnet test WP-Plan.Kern.slnf -c Release` → **941/941**, also **+12** gegenüber iU10-1:
      5 Tests `Projektliste` (Zeilenzahl, Spaltenfolge, Leertext, beide Knöpfe mit ihrem
      Maskenschlüssel), 7 Tests `AppWurzel` (Start in der Liste, Klick öffnet den Dialog,
      Abbrechen kehrt zurück **und lädt neu**, OK reicht das Ergebnis an die Hülle, ohne
      BHKW-Daten bleibt die Liste mit Hinweis stehen, mit Daten geht der zweite Dialog auf,
      Anmeldung als `INavigationsZiel`).
- [x] UI-Kultur in beiden Testklassen auf **de-DE** gepinnt und im `Dispose` zurückgestellt — wie
      in `SpeichernLeisteTests`; die CI-Läufer auf macOS und Windows laufen englisch.

### `e1a2220` — iU10-3: die Hülle

- [x] **Restore-Probe.** Eine Kopie der **echten** `EPOS.iOS.csproj` mit `TargetFramework`
      `net10.0` statt `net10.0-ios` und ohne die Workload-Zeilen (`UseMaui`, `SingleProject`,
      `MauiVersion`, `MauiAsset`, `MauiIcon`, `MauiSplashScreen`, `RuntimeIdentifiers`,
      `SupportedOSPlatformVersion`), gegen die **echte** `Directory.Packages.props` →
      **Restore OK**. Der aufgelöste Graph:

  | Paket | Fassung |
  |---|---|
  | `Microsoft.AspNetCore.Components.WebView.Maui` | 10.0.100 |
  | `Microsoft.AspNetCore.Components.WebView` | 10.0.0 |
  | `Microsoft.Data.Sqlite` / `.Core` | 10.0.11 |
  | `SQLitePCLRaw.bundle_e_sqlite3` / `.core` / `.lib.e_sqlite3` / `.provider.e_sqlite3` | **je 2.1.12** |
  | `SkiaSharp` | 3.119.0 |
  | `Microsoft.ML.OnnxRuntime` / `.Managed` | 1.22.1 |

- [x] **`SkiaSharp.NativeAssets.iOS` bleibt in der Probe draußen** — das Paket trägt **nur**
      `net8.0-ios17.0` und meldet im `net10.0`-Stub `NU1202`. Genau das ist der Beleg, dass es die
      Fassung 3.119.0 gibt und dass sie **iPadOS 17.0 erzwingt** (iF28).
- [x] `dotnet sln EPOS.iOS/EPOS.iOS.sln list` → `EPOS.iOS.csproj`.
- [x] `EPOS.iOS.csproj`, `Platforms/iOS/Info.plist`, beide SVG und `wwwroot/index.html` als XML
      wohlgeformt.

### `872b897` — iU10-4: Datenbankweg

- [x] **Übersetzungsprobe.** `Datenbankbereitstellung.cs` in einem `net10.0`-Projekt gegen die
      echten `EPOS.Kern` und `EPOS.UI` → **0 Fehler**, keine eigene Warnung. Die Datei kennt
      bewusst **keine iOS-API**: Den Zugang zum Anwendungspaket bekommt sie als Rückruf
      (`FileSystem.OpenAppPackageFileAsync` reicht ihn in `MauiProgram`), den Ablageort über
      `Dienste.Pfade`.
- [x] Beide SQL-Texte gegen `Referenzlaeufe/Kenndaten_Test.sqlite` gemessen:
      **STRICT-Tabellen 114 von 115**, `Tab_Projekt` **23 Zeilen**. Das sind die Zahlen, die der
      CI-Job im Startprotokoll erwartet.

### `0f7cb21` — iU10-5: die neun Adapter

- [x] **Übersetzungsprobe der plattformfreien Dateien** (`Datenbankbereitstellung`, `Dateifilter`,
      `IosNavigation`, `IosProjektKontext`, `IosHilfeDienst`) → **0 Fehler, 0 eigene Warnungen**.
- [x] **Attrappenprobe.** Alle `.cs`-Dateien der Hülle außer den beiden iOS-Einstiegspunkten
      (`Platforms/iOS/Main.cs`, `AppDelegate.cs`) gegen **Attrappen** der MAUI-, UIKit- und
      Foundation-API → **0 Fehler, 0 Warnungen**. Am Ende von iU10 sind das **17 Dateien**
      (19 minus die beiden Einstiegspunkte) plus die beiden verlinkten Bausteine des
      Referenzlaufs.

> **Was die Attrappenprobe beweist und was nicht.** Sie belegt, dass der eigene Programmtext in
> sich stimmt: Namen, Typen, Überladungen, Nullbarkeit, Sichtbarkeiten. Sie belegt **nicht**, dass
> die echte MAUI-API so heißt — die Attrappen sind nach bestem Wissen der öffentlichen
> MAUI-10-Signaturen geschrieben, also aus derselben Quelle wie der Code selbst. Der erste Lauf
> von `ios.yml` ist deshalb kein Formalakt.

### `556ae7e` — iU10-6: Prüfmodus und CI-Job

- [x] `python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ios.yml'))"` → gültiges
      YAML; Schrittnamen, Pfade und Artefaktnamen sichtgeprüft.
- [x] Die in den Job eingebettete **Simulatorwahl** gegen zwei synthetische `simctl`-Ausgaben
      geprüft: Ein iPad mit iOS 26 wird gefunden; gibt es keines, endet der Schritt mit **Exit 1**
      statt mit leerer UDID.
- [x] Attrappenprobe einschließlich `Pruefung/Prueflauf.cs` und der beiden **verlinkten**
      Bausteine `Referenzlauf/Ergebnisexport.cs` und `Referenzlauf/Protokoll.cs` → **0 Fehler,
      2 Warnungen**. Die zwei stammen aus den verlinkten Dateien (CS8602, CS8604): Sie sind wie
      das Werkzeug, aus dem sie kommen, ohne Nullbarkeitsangaben geschrieben, während die Hülle
      `Nullable=enable` führt. Ein `NoWarn` dagegen gälte für die ganze Hülle und ist deshalb
      **nicht** gesetzt — der Kommentar in der `.csproj` sagt es.

### `6b4ba46` — iU10-7: die Datenseite

- [x] **Prüfstand gegen eine Arbeitskopie von `Kenndaten_Test.sqlite`**, der die echten
      Kern-Controller über `IosProjektQuelle` ruft:

```
SQLite 3.53.3
STRICT=114
Projekte=23
      19  Wöhler WP                          stuttgart
    1006  Stromspeicher mit Wärmepumpe                      WP
    1007  Laurentiuskirche                                  WP+PV+Speicher+Kessel+Puffer
    1008  Heinestr 15                                       WP+Kessel+Puffer
Energietraeger=25
BhkwDaten(1030)=geladen
   StammName=Referenz BHKW-Kaskade (Regressionstest)
   Anlagen=2   HatHeizkessel=True   Katalog/Speichern/ErgebnisseLaden gesetzt
BhkwDaten(0)=null
```

- [x] Die gemeldete **SQLite 3.53.3** ist dieselbe Fassung, die `bundle_e_sqlite3` 2.1.12 auf iOS
      statisch mitlinkt — der Beleg für das STRICT-Gate (iF27).
- [x] `dotnet build`/`dotnet test WP-Plan.Kern.slnf` → **0 Fehler, 0 Warnungen, 941/941**.

---

## Nachweise, die nur die CI führen kann — **geführt, achter Lauf grün**

**Lauf 33748736894 (`ios.yml`, `macos-26`, 03.09.2026, 11:16–11:21 UTC, 5 min 44 s)** auf `7f89425`:

| Schritt | Ergebnis |
|---|---|
| Workload `maui-ios` Set `10.0.400.1` | installiert in 23 s (iOS 26.5.10315, MAUI 10.0.20/10.0.100) |
| Bau `EPOS.iOS` Simulator (**Debug**, JIT, kein Linker) | 57 s |
| Simulator (iPad, iOS 26.x) gestartet, App installiert | ja |
| Erststart | Datenbank aus dem Paket kopiert, 73 MB |
| Startmarken | `SQLite 3.53.3` · `STRICT=114` · `EPOS.iOS bereit: Projekte=23` |
| Prüfmodus Projekt 1030 | Simulation in 5 s, 22 CSV, 150 Skalare, `fertig.txt` |
| **iZ6-Vergleich gegen `2026-08-30_B3-Kaskade`** | **GESAMT: PASS, 236 670 Werte; `diff -rq`: BYTE-GLEICH (iOS-Simulator arm64)** |
| Artefakte | `ios-simulator` (Startprotokoll, CSV, Bildschirmabzug, 1,1 MB), `ios-app` (86 MB) |

Der Weg dorthin, acht Läufe: (1) Workload-Set in CLI-Schreibweise `10.0.400.1`; (2) `Microsoft.Maui.Controls`
ausdrücklich referenzieren; (3) `INavigation` gegen MAUI qualifizieren; (4/5) Release-Bau lief 40 min in
der Mono-AOT-Übersetzung → Simulator-Bau in Debug; (6/7) Startmarken hinter Zeitstempel und `\r` der
pty-Ausgabe. Verbrauch aller Läufe zusammen ≈ 65 macOS-Minuten.

**Damit belegt:** Der Kern rechnet auf iOS byte-gleich zur Windows-Basis (iZ6-Vorstufe im Simulator),
die mitgelieferte SQLite ist auf allen vier Plattformen dieselbe (iF27 bestätigt), der Datenbankweg auf dem
Gerät funktioniert. Offen bleibt der Gerätebau (iU13).

### Ursprüngliche Liste (zur Nachvollziehbarkeit)

Abzuhaken nach dem ersten grünen Lauf von **Actions → iOS → Run workflow**.

- [x] **Die Workload installiert sich** — bestätigt im zweiten Lauf (33734332715, 03.09.2026, 20 s): Set `10.0.400.1`, Manifest iOS 26.5.10315, MAUI 10.0.20; Restore, Kern und `EPOS.UI` bauen für `net10.0-ios`. Der Lauf brach danach in der Hülle ab (CS0234 `Microsoft.Maui`): `Microsoft.Maui.Controls` muss seit .NET 8 ausdrücklich referenziert werden — behoben, dritter Lauf nach Freigabe des Anwenders.
- [x] **Die Hülle übersetzt für `net10.0-ios`** — bestätigt im fünften Lauf (33740727778, 03.09.2026): `EPOS.iOS.dll` nach 47 s. Danach lief der Release-Bau 40 min in der Mono-AOT-Übersetzung (LLVM) und wurde abgebrochen; der Simulator-Bau läuft seither in **Debug** (JIT, kein Linker). Dritter/vierter Lauf davor: `Microsoft.Maui.Controls`-Referenz und `INavigation`-Mehrdeutigkeit behoben.
- [ ] **Die Workload installiert sich.** `dotnet workload install maui-ios --version 10.0.400.1`
      läuft durch, `dotnet workload list` zeigt `maui-ios`.
- [ ] **Xcode passt zur Workload.** Kein Fehler „requires Xcode …"; `xcodebuild -version` meldet
      26.6. (Läuft es hier auf, ist `DEVELOPER_DIR` oder das Runner-Label zu ziehen — iR-a.)
- [ ] **Der Bau geht durch.** `dotnet build … -f net10.0-ios -r iossimulator-arm64` → 0 Fehler.
      Hier fällt auf, wo eine Attrappe von der echten API abweicht.
- [ ] **Das Razor-SDK packt die Web-Bestände mit.** Im `.app` liegen `wwwroot/index.html`,
      `_content/EPOS.UI/epos-ui.css` und `_framework/blazor.webview.js`. Fehlen sie, bleibt die
      Oberfläche ungestaltet oder leer — derselbe Befund wie in iU8-6.
- [ ] **Die App startet im Simulator** und das Fenster zeigt die Projektliste (Bildschirmabzug
      `oberflaeche.png` im Artefakt).
- [ ] **Die drei Startmarken stehen im Protokoll:**
      `EPOS.iOS bereit: Projekte=23`, `SQLite 3.53.3`, `STRICT=114`.
- [ ] **Die Seed-Kopie findet statt.** `start.log` meldet beim ersten Lauf „Erststart — Datenbank
      wird aus dem Anwendungspaket kopiert." und danach die Größe.
- [ ] **`IosPfade` liefert brauchbare Ordner.** `start.log` meldet `Ablage:` und `Dokumente:`
      unterhalb der Sandbox (`…/Library/Application Support/WP-Plan/EPOS_PLAN` bzw.
      `…/Documents`) — **nicht** `~/.config`.
- [ ] **Der Prüflauf endet.** `Documents/pruefung/fertig.txt` entsteht innerhalb von fünf Minuten.
- [ ] **Der iZ6-Vergleich besteht.** `EPOS.Referenzlauf vergleich` meldet **GESAMT: PASS** gegen
      `Referenzlaeufe/2026-09-05_R2_Zeitbasis/Projekt_1030` (Toleranz rel. 1e-4 / abs. 0,01, iF15;
      bis 05.09.2026 gegen `2026-08-30_B3-Kaskade`, Basiswechsel nach Paket A der Rechner-2-Linie).
- [ ] **Der Byte-Diff wird protokolliert** — er ist nur Information: Auf ARM64 sind
      Gleitkommaabweichungen im Rahmen der Toleranz erwartbar.
- [ ] **Die Bundle-Größe steht fest.** `.app`-Artefakt wiegen — Grundlage für die Entscheidung
      iF25 (Seed-Datenbank).

---

**Neunter Lauf 33785012663 (`ios.yml`, `macos-26`, 03.09.2026, 17:31–17:41 UTC, 9 min 52 s)** auf
`f1d387b` — der Stand **nach den Wellen 5 und 6**: grün. Derselbe Weg wie im achten Lauf;
EPOS.UI mit den Berichtsseiten (W5), den sieben Erzeugerdialogen und der Assistentenseite (W6)
sowie der Kern mit den neuen Controller-Methoden bauen für `net10.0-ios`, der Prüfmodus rechnet
Projekt 1030 im Simulator und der iZ6-Vergleich meldet PASS (der Job wäre sonst rot). Die
längere Dauer kommt aus dem gewachsenen Bau (785 statt 528 bunit-Tests werden nicht gebaut,
wohl aber die neuen Razor-Komponenten). Ausgelöst per `workflow_dispatch` unter der pauschalen
Freigabe bis Migrationsende.

**Zehnter Lauf 33809247370 (`ios.yml`, `macos-26`, 03.09.2026, 21:41–21:46 UTC, 4 min 53 s)** auf
`21ab680` — der Stand **nach den Wellen 7, 8 und 9**: grün. Derselbe Weg; EPOS.UI trägt jetzt
alle elf Kacheln des Startbilds, zehn Assistentenseiten und die Bedarfstyp-Dialoge, der Kern die
drei Bedarfsbilder des Renderers, `WPCtrl`, `Ferienzeit` und die Projektlisten der Bedarfsgewerke.
Bau, Simulatorstart, Erststart mit Seed-Kopie, Prüfmodus 1030 und iZ6-Vergleich PASS. Der Lauf
war mit knapp fünf Minuten halb so lang wie der neunte, weil der Läufer den Workload aus dem
Cache zog. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Elfter Lauf 33826084944 (`ios.yml`, `macos-26`, 04.09.2026, 01:31–01:40 UTC, 8 min 50 s)** auf
`a398c9a` — der Stand **nach den Wellen 10a und 10b**: grün. Erstmals mit der Simulationskonfiguration
als Razor-**Seite** (`SimulationKonfigSeite`, Eintrag in `Seitenschluessel`/`AppWurzel`), dem
SVG-Schema, den drei neuen Bausteinen und dem Kartenbild (1,29 MiB) unter `wwwroot/bilder/`; die
`IosProjektQuelle` trägt für `SimulationKonfigGaben` noch die Standardumsetzung, die Seite ist am
Gerät also noch nicht erreichbar (iU11). Bau, Simulatorstart, Erststart mit Seed-Kopie, Prüfmodus
1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis
Migrationsende.

**Zwölfter Lauf 33832613617 (`ios.yml`, `macos-26`, 04.09.2026, 03:16–03:25 UTC, 9 min 02 s)** auf
`43fb9c3` — der Stand **nach den Wellen 11a und 11b**: grün. Erstmals mit der Ergebnisseite der
Simulation als zweiter Razor-**Seite** in `Seitenschluessel`/`AppWurzel` (`SimulationErgebnisSeite`,
zehn Blätter, sieben Renderer-Bilder aus W11a, Baustein `Fortschritt`) und ohne `Form_Simulation_Detail`;
die `IosProjektQuelle` liefert den Parametersatz der Seite noch nicht (W11b‑O‑5, iU11), die Seite ist
am Gerät also noch nicht erreichbar. Bau, Simulatorstart, Erststart mit Seed-Kopie, Prüfmodus 1030
und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis
Migrationsende.

**Dreizehnter Lauf 33838762108 (`ios.yml`, `macos-26`, 04.09.2026, 04:58–05:04 UTC, 6 min 30 s)** auf
`62b3457` — der Stand **nach der Welle 12**: grün. Erstmals mit `GanglinienImportAblauf` und den zwölf
Ganglinien-Proben im Kern, den sechs neuen Razor-Dialogen (Stromganglinie, Ganglinien-Verwaltung,
Importprotokoll, Importoptionen, Konfliktdialog, Lastspitzenkappung) und ohne `Form_Stromganglinie`,
`Form_Stromganglinie_Admin`, `Form_PeakShaving`, `Form_GanglinieProtokoll`, `Form_GanglinieImportOptionen`.
Bau, Simulatorstart, Erststart mit Seed-Kopie, Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per
`workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Vierzehnter Lauf 33844935661 (`ios.yml`, `macos-26`, 04.09.2026, 06:34–06:44 UTC, 10 min 04 s)** auf
`29aecbc` — der Stand **nach der Welle 13**: grün. Erstmals mit `KatalogImportAblauf`/`KatalogImportProfil`
und den zwanzig Importproben im Kern, den drei Import-Komponenten (`KatalogImportDialog` mit vier Ausprägungen,
`WaermebedarfAdminDialog`, `PvModulImportDialog`), der Mehrfachmarkierung im `Raster` und ohne die sechs
Importmasken; die Sprungbrücke `WaermebedarfExternAdmin` ist gefallen. Bau, Simulatorstart, Erststart mit
Seed-Kopie, Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen
Freigabe bis Migrationsende.

**Fünfzehnter Lauf 33852944072 (`ios.yml`, `macos-26`, 04.09.2026, 08:20–08:27 UTC, 7 min 24 s)** auf
`ecd6cfe` — der Stand **nach den Wellen 14a und 14b**: grün. Erstmals mit den Katalogbrowsern
(`KatalogBrowserDialog`, `PufferSpKatalogDialog`, `ModulKatalogDialog`), der Bedarfs-Admin (`BedarfAdminDialog`,
`SolarganglinieAdminDialog`), der Energieeinheiten-Wahl MWh/kWh im Kern, der berichtigten Heizkessel-Brennstoffkette
und ohne elf Admin-Masken, `ToolsClass`, `SpeichernLeiste`, `KiAufrufKnopf`; die Sprungbrücke trägt nur noch die
Zweige `Gesetzesparameter`, `GesetzesparameterCo2` (W14c) und `SpeicherOptimierung` (iF22). Bau, Simulatorstart,
Erststart mit Seed-Kopie, Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der
pauschalen Freigabe bis Migrationsende.

**Sechzehnter Lauf 33861268537 (`ios.yml`, `macos-26`, 04.09.2026, 10:02–10:11 UTC, 9 min 28 s)** auf
`0cc1495` — der Stand **nach der Welle 14c**: grün. Erstmals mit dem Gesetzeskatalog (`GesetzeskatalogDialog` mit
Zeilendialog als Überlagerung), der Katalog-Dublettensuche über dem Baustein `Baumansicht`, dem Einstellungsdialog
(`EinstellungenCtrl` im Kern über `Dienste.Pfade`/`Dienste.Einstellungen`) und den Klimaregionen (`KlimaregionStammCtrl`
und `KlimaImportAblauf` im Kern, zwei Klimabilder im Renderer); ohne `ChartManager` (die MS-Chart-Bindung ist beendet),
ohne `RoundedPanel`, und die Sprungbrücke trägt nur noch den Zweig `SpeicherOptimierung`. Bau, Simulatorstart, Erststart
mit Seed-Kopie, Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe
bis Migrationsende.

**Siebzehnter Lauf 33867643966 (`ios.yml`, `macos-26`, 04.09.2026, 11:22–11:33 UTC, 10 min 30 s)** auf
`c11f13d` — der Stand **nach der Welle 15a und den W14c-Entscheiden**: grün. Erstmals mit dem Baustein
`ProjektListe` unter der iOS-Projektliste (`Seiten/Projektliste` baut darauf, die fünf Fälle unverändert), den
Projektdialogen (`ProjektWahlDialog`, `ProjektKopieDialog`, `ProjektTransferDialog` — `ProjektExportImportCtrl` im Kern,
`IProjektQuelle.TransferDaten()` mit Standardumsetzung, damit `IosProjektQuelle` unverändert bleibt), der Assistentenseite
`ProjektKopfSeite`, dem Schema-Schritt 62 (`SchemaStand.Zielversion` 62 im Kern; die Seed-Kopie trägt keine Waisen) und
den festen Pfaden des Einstellungsdialogs ohne Ordnerwähler (E‑5). Bau, Simulatorstart, Erststart mit Seed-Kopie,
Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis
Migrationsende.

**Achtzehnter Lauf 33876284942 (`ios.yml`, `macos-26`, 04.09.2026, 13:07–13:09 UTC, 2 min 26 s)** auf
`f71853b` — der Stand **nach der Welle 15b**: **rot**, Bau der iOS-Hülle mit CS0103: `IosHilfeDienst.cs(67)` schrieb
`MyResource.Resource.HILFE_IOS_BESCHREIBUNG` (Auflage H‑2 aus W15b.0g — der einzige deutsche Satz der iOS-Hülle wurde in
die Ressourcen gehoben), aber die Ressourcenklasse liegt in `WindowsFormsApplication1.MyResource`, und das `using` auf
`WindowsFormsApplication1` macht den Unter-Namensraum in `EPOS.iOS` nicht sichtbar. Im Kern löst sich derselbe Ausdruck
relativ zum umgebenden Namensraum auf, deshalb fiel es auf Linux und Windows nicht auf — **die iOS-Hülle wird nur vom
macOS-Läufer übersetzt**, das ist der Sinn des Laufs. Behoben mit `f0e23a4` (voll qualifiziert; kein weiterer Treffer in
`EPOS.iOS`).

**Neunzehnter Lauf 33878903371 (`ios.yml`, `macos-26`, 04.09.2026, 13:35–13:41 UTC, 6 min 27 s)** auf `f0e23a4`: grün.
Erstmals mit `KiChatService` im Kern hinter `IKiAusfuehrung` (auf iOS die stille Standardfassung `KeineAusfuehrung`),
dem Baustein `Gespraechsverlauf`, den KI-Dialogen, `Seitenschluessel.KiAssistent` in der `AppWurzel`, dem Tooltip-Schlüssel
und den Rückfragen bei mehrdeutigem Projekt- und Variantennamen (O‑3/O‑4). Bau, Simulatorstart, Erststart mit Seed-Kopie,
Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Zwanzigster Lauf 33883210632 (`ios.yml`, `macos-26`, 04.09.2026, 14:20–14:31 UTC, 10 min 14 s)** auf
`975ead5` — der Stand **nach der Welle 15c** (Rückweg-Anker `vor-W16`): grün. Erstmals mit `LizenzManager.Bewerten`
und den ersten Lizenztests im Kern, `LizenzCtrl`/`LizenzTextCtrl`/`ZustimmungCtrl`, den vier Hüllenzusätzen an
`BlazorDialogForm<T>` und den Lizenz- und Erststartkomponenten in `EPOS.UI`; der WebView2-Riegel aus `Program.Main`
(E‑8) betrifft nur die Windows-Anwendung, die iOS-Hülle startet unverändert über `AppWurzel`. Bau, Simulatorstart,
Erststart mit Seed-Kopie, Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen
Freigabe bis Migrationsende.

**Einundzwanzigster Lauf 33890882150 (`ios.yml`, `macos-26`, 04.09.2026, 15:41–15:49 UTC, 8 min 03 s)** auf
`84d7c16` — der Stand **nach der Teilwelle 16a**: grün. Erstmals mit dem Assistenten hinter
`IProjektQuelle.AssistentGaben` in der `AppWurzel` (N9 — `IosProjektQuelle` setzt die Gaben noch nicht um, der Assistent
ist auf iOS angekündigt, aber nicht bedienbar, W16a‑O‑4), mit `KomponentenBestandCtrl` und `AssistentCtrl` im Kern und
ohne `BlazorAssistentSeite`, `WizardParent` und `ProjektAuswahl`. Bau, Simulatorstart, Erststart mit Seed-Kopie, Prüfmodus
1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Zweiundzwanzigster Lauf 33898599945 (`ios.yml`, `macos-26`, 04.09.2026, 17:04–17:12 UTC, 7 min 56 s)** auf
`c8fbd77` — der Stand **nach der Teilwelle 16b**: grün. Erstmals mit der Razor-Startseite und
`IProjektQuelle.Startkacheln` im gemeinsamen `EPOS.UI` (N9 — `IosProjektQuelle` setzt die Startkacheln noch nicht um, der
Zweig in der `AppWurzel` kommt mit K7 in W16c), mit `ProjektKontextCtrl`, `StartseiteCtrl` und `BedarfsZustand` im Kern
und ohne `Form_Start`, `FormMain` und die zwölf `*KontextMenuCtrl`. Bau, Simulatorstart, Erststart mit Seed-Kopie,
Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis
Migrationsende. Der Lauf Nr. 23 (33898901613) auf demselben Stand war eine versehentliche Dublette (der erste Aufruf
ging vor einem Neustart des Sitzungsprozesses durch, seine Bestätigung ging verloren) und wurde nach 100 Sekunden
abgebrochen; der nächste echte Lauf trägt die Nummer 24.

**Vierundzwanzigster Lauf 33904433007 (`ios.yml`, `macos-26`, 04.09.2026, 18:10–18:18 UTC, 8 min 43 s)** auf
`555ef11` — der Stand **nach der Teilwelle 16c, dem Ende der Mischphase (M9)**: grün. Erstmals mit `Hauptfenster`,
`Menueband` und der `AppWurzel` als gemeinsamer Wurzel beider Plattformen (N9 — die `Kopfleiste` ist auf iOS leer,
`StartseiteGaben`, `BerichteKostenGaben` und `AdresseOeffnen` laufen in die Standardumsetzung, die `AppWurzel` sagt es im
Banner; die Adapter sind iU11), mit `Seitenschluessel` als der einen Schlüsseltabelle (K7) und ohne den Designer der
`MDIMainForm`. `EPOS.iOS` selbst ist in W16c nicht angefasst worden. Bau, Simulatorstart, Erststart mit Seed-Kopie,
Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis
Migrationsende.

**Fünfundzwanzigster Lauf 33913313694 (`ios.yml`, `macos-26`, 04.09.2026, 19:51–19:58 UTC, 6 min 29 s)** auf
`853b8c6` — der Stand **nach den Nachträgen zu Welle 16** (W16c‑E‑2 Untermenü „Sprache", W16c‑E‑3 Ansichtswechsel auf
`BERICHTE_KOSTEN`, W15c‑O‑2 `LizenzTexte`-Bündel, W16b‑O‑3 Klimazone als eine Wahrheit im Kern): grün. Erstmals mit
dem Ansichtswechsel der `AppWurzel` auf die Berichte-Seite über den Menüweg (auf iOS der einzige Weg dorthin, die
`Kopfleiste` bleibt leer) und mit `IosProjektKontext` als dünner Weiterleitung auf `ProjektKontextCtrl` — die
Klimazone kommt jetzt aus dem Kern, die eigene Stammabfrage der Hülle ist gefallen. Bau, Simulatorstart, Erststart mit
Seed-Kopie, Prüfmodus 1030 und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis
Migrationsende.

**Sechsundzwanzigster Lauf 33975880961 (`ios.yml`, `macos-26`, 05.09.2026, 15:47–15:53 UTC, 6 min 14 s)** auf
`7bec4ad` — der Stand **nach den Befunden der Windows-Abnahme vom 05.09.2026** (W16c‑B13 Untermenüs, W9‑B‑1…B‑5,
W15a‑B‑1, W16a‑B‑1/B‑2, W11b‑B‑2/B‑3 und A‑1): grün. Erstmals mit dem Baustein `Diagramm` und dem Modul
`epos-diagramm.js`, das wie `epos-verlauf.js` über `import()` geladen wird — die `index.html` der iOS-Hülle blieb
unverändert; ob Pinch und Ein-Finger-Verschieben in der WKWebView die Seite nicht mitzoomen (`gesturestart`,
`touch-action: none`), bleibt Abnahmepunkt 22 am Gerät. Bau, Simulatorstart, Erststart mit Seed-Kopie, Prüfmodus 1030
und iZ6-Vergleich PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Siebenundzwanzigster Lauf 33982889724 (`ios.yml`, `macos-26`, 05.09.2026, 18:04–18:14 UTC, 10 min 51 s)** auf
`c563a40` — der Stand **nach den sechs Befunden und Wünschen des Nachmittags der Windows-Abnahme vom 05.09.2026**
(W16b‑E‑7 Kachelmaß, W9‑E‑2 Gebäude-Simulation, W15a‑E‑1 Variantenprojekte, W5‑E‑1 Übersicht-Auswahlfeld, iU8‑E‑1
Admin-Dialoge, W13‑B‑1 Importabsturz): grün. Für die iOS-Hülle zählt vor allem W13‑B‑1: `IDateiDienst` und
`IDialogDienst` führen wartbare Zwillinge (`…Async`) mit Standardfassung, `IosDateiDienst.AufDemHauptfaden` lieferte
vom Hauptfaden bisher `default` (der Wähler ging nie auf) und ist über die `…Async`-Fassungen behoben, und
`EPOS.iOS/HauptSeite` mountet `Wurzel<AppWurzel>` — die Fehlerschranke, die eine Komponentenausnahme als Fehlerkasten
zeigt statt den Prozess zu beenden. Bau, Simulatorstart, Erststart mit Seed-Kopie, Prüfmodus 1030 und iZ6-Vergleich
PASS. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Achtundzwanzigster Lauf 33992594094 (`ios.yml`, `macos-26`, 05.09.2026, 21:17–21:24 UTC, 6 min 53 s)** auf
`6eddd27` — der Stand **nach der Zusammenführung der Rechner-2-Linie** (PV-Ertragsmodell Paket A/B, Projektdialoge,
Projektstammdaten, FS1), dem Hilfe-Assistenten (W15b), dem Formularraster P1–P3 (iU8‑E‑2), der Testdatenbank auf
Schemastand 64 und der Stromganglinie als Grafik (W12‑E‑2): Bau, Simulatorstart, Erststart mit Seed-Kopie
(Schemastand 64) und Prüfmodus 1030 grün, **der iZ6-Vergleich rot** — nicht wegen des Rechenwegs, sondern weil
`ios.yml` noch gegen `2026-08-30_B3-Kaskade` hielt: 8 711 Abweichungen in 1030, exakt die Paket-A-Verschiebung der
Solar-Zeitbasis von UTC auf Ortszeit, dieselbe Zahl wie der Linux-Läufer im Gate. Die CI-Basis war am selben Tag mit
Anwenderentscheid auf `2026-09-05_R2_Zeitbasis` gewechselt (`37dfebb`, `kern.yml` und Gate), `ios.yml` fehlte in der
Umstellung — behoben in `e3fd980`.

**Neunundzwanzigster Lauf 33993379551 (`ios.yml`, `macos-26`, 05.09.2026, 21:33–21:40 UTC, 6 min 32 s)** auf
`e3fd980` — derselbe Stand plus Schema-Pfeile (W10b‑B‑1: `SchemaLayout` in Spaltenbahnen, `HatKaskade` über alle
Ränge) und der Workflow-Umstellung: Bau, Simulatorstart, Erststart, Prüfmodus 1030 und **iZ6-Vergleich gegen
`2026-09-05_R2_Zeitbasis` PASS und byte-gleich (236 670 Werte, `diff -rq` leer; die Simulation meldet die Paket-A-Zeitbasis „Klimadaten: UTC → MEZ/MESZ, Referenzjahr 2025")**. Damit ist auch auf dem iOS-Simulator belegt, dass der zusammengeführte
Rechenweg samt Paket A die neue Basis trifft. Beide Läufe ausgelöst per `workflow_dispatch` unter der pauschalen
Freigabe bis Migrationsende.

**Dreißigster Lauf 34017405042 (`ios.yml`, `macos-26`, 06.09.2026, 06:47–06:53 UTC, 5 min 54 s)** auf
`cb8379e` — der Stand **nach der Welle iF30 „Lesemodus streng"**: Die Schreibnaht `Schreibnaht.Pruefe` in
`SqliteDatenzugriff.ErzeugeKommando` sperrt im Lesemodus jede schreibende Anweisung des Kerns, und genau das trifft
den Prüfmodus der iOS-Hülle, der ohne Lizenz rechnet und Ergebnisse schreibt: `EPOS.iOS/Pruefung/Prueflauf.cs` hebt
die Sperre mit einer benannten Zeile `Schreibnaht.WerkzeugFreigabe(…)` nach `KulturSetzen`, die Seed-Kopie und
`VACUUM INTO` in `Datenbankbereitstellung` laufen als Ausnahme „Sicherung", und `IosProjektQuelle.Lizenzlage()`
liefert der `AppWurzel` den Zustand für das Lizenzbanner. Bau, Simulatorstart, Erststart mit Seed-Kopie, Prüfmodus
1030 und iZ6-Vergleich gegen `2026-09-05_R2_Zeitbasis` PASS und byte-gleich (236 670 Werte, `diff -rq` leer; das Vergleichswerkzeug meldet „Schreibnaht: freigegeben für EPOS.Referenzlauf (Rechennachweis ohne Lizenz)"). Dazu im selben Stand: die Referenzbasis R2_Zeitbasis
(Anwenderentscheid 05.09.), `Resource.Designer.cs` per Werkzeug erzeugt, PufferSpProjektDialog im Formularraster,
Wechselrichter-Konzept. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Einunddreißigster Lauf 34086413182 (`ios.yml`, `macos-26`, 07.09.2026, 05:20–05:25 UTC, 5 min 47 s)** auf
`5247e73` — der Stand nach den Wellen des 06.09.2026 (W6‑E‑4 Vor-/Rücklauf aus dem Katalog, H13 Fassung 2 und 3,
W6‑O‑1…O‑9 mit dem Wechselrichterweg S1–S3, dem Importwirt und dem Prüfprojekt 1045, W7‑B‑1/B‑2/E‑2 Wärmepumpe,
W16c‑E‑6 Administration-Menü): Workload, Bau, Simulatorstart, Erststart mit Seed-Kopie (`EPOS.iOS bereit: Projekte=24` —
das zwölfte Prüfprojekt 1045 zählt mit) und `SQLite 3.53.3` grün; **rot allein im STRICT-Gate:** Der Workflow verlangte
wörtlich `STRICT=114`, die Testdatenbank führt seit den Migrationsschritten 65 (Wechselrichterkatalog, zwei Tabellen) und
66 (`Z_AnlageStrang`) aber **117 STRICT-Tabellen von 118** (die 118. ist `sqlite_sequence`). Prüfmodus und iZ6-Vergleich
kamen deshalb nicht an die Reihe — die App selbst hatte keinen Fehler. Behebung im Folgecommit (siehe Lauf 32): Das Gate
leitet die Erwartung aus der Seed-Datenbank ab (`sqlite3 … sql LIKE '%STRICT%'`) und verlangt im Startprotokoll mindestens
diese Zahl — eine kleinere meldet weiterhin eine fremde SQLite-Fassung, eine nach einem Migrationsschritt gestiegene bricht
den Lauf nicht mehr. Der Pfad-Adapter `IosPfade` erbt das neue Mitglied `IPfade.Herstellerdaten` (W6‑O‑9) von
`StandardPfade`; auf iOS findet die Suche keinen Ordner und liefert den leeren Pfad, die Einstellungen fallen auf die Vorgabe
zurück. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Zweiunddreißigster Lauf 34087446473 (`ios.yml`, `macos-26`, 07.09.2026, 05:36–05:46 UTC, 9 min 23 s)** auf
`7885947` — derselbe Programmstand wie Lauf 31, dazu das umgestellte STRICT-Gate: Workload 19 s, Bau 2 min 9 s
(0 Fehler, 12 Warnungen des Bestands), Simulatorstart 1 min 18 s, Erststart mit Seed-Kopie (66 MB), `SQLite 3.53.3`,
**`STRICT=117`** gegen „erwartet mindestens 117 (Seed)", `EPOS.iOS bereit: Projekte=24`, Prüfmodus 1030 in 24 s
(22 CSV, 150 Skalare), iZ6-Vergleich gegen `2026-09-06_R3_Straenge` **PASS (236 670 Werte)** und **BYTE-GLEICH**
(`diff -rq` leer, iOS-Simulator arm64). Damit ist der gesamte Stand des 06.09.2026 — Wechselrichterweg S1–S3 mit
Strangmodell und Clipping, Modul je Strang, Importwirt mit OND, Prüfprojekt 1045 in der Seed-Datenbank, Vor-/Rücklauf-
Vorbelegung, Wärmepumpen-Umbau, Administration-Menü — auf iOS gebaut, gestartet und im Rechenweg byte-gleich nachgewiesen.
Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Dreiunddreißigster Lauf 34094835604 (`ios.yml`, `macos-26`, 07.09.2026, 07:18–07:27 UTC, 8 min 16 s)** auf
`83dec06` — der Gesamtstand des 07.09.2026: W6‑E‑6 (Vorauswahl des Herstellerfilters), W16c‑E‑7 und W16c‑O‑7 (Knoten
„Photovoltaik", „Klimadaten" direkt im Kopf — das Menü ist Daten, die `Menuetabelle` ist auf beiden Plattformen dieselbe),
W13‑E‑2 Stufe 0 und S1 (Stromspeicherimport mit `CecSpeicherDienst` und bslib-Auslieferung), W6‑B‑2 (Raster-Fix beim
Wechsel des Virtualisierungsschalters — trifft jede lange Liste der `AppWurzel`), W6‑E‑5 (Mehrfachwahl und Doppelklick in
allen sechs Importen) und W6‑E‑7 (BHKW-Leistungsuntergrenze ohne Fallback, **Migrationsschritt 67** in der Seed-Datenbank,
Schemastand 67). Workload 29 s, Bau 1 min 35 s (0 Fehler), Simulatorstart 2 min 16 s, Erststart mit Seed-Kopie, Startmarken
(STRICT-Gate aus der Seed-Datenbank, `Projekte=24`) grün, Prüfmodus 1030 in 1 min 37 s, iZ6-Vergleich gegen
`2026-09-06_R3_Straenge` **PASS (236 670 Werte)** und **BYTE-GLEICH** (`diff -rq` leer). Damit rechnet auch der Stand mit
gefallenem BHKW-Fallback auf iOS byte-gleich — der Beleg, dass Schritt 67 (NULL → 30) die Rechnung des Bestands unverändert
lässt. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Vierunddreißigster Lauf 34117430636 (`ios.yml`, `macos-26`, 07.09.2026, 11:35–11:41 UTC, 5 min 13 s)** auf
`c99cdb7` — der zweite Tagesstand des 07.09.2026 und der erste Lauf, der **den Rechenkern in `double`** prüft (W8‑O‑5d:
Stundenreihen, Akkumulatoren, `BhkwPlan`, Datenbankgrenze, `SpeicherEngine`-Naht), dazu W11b‑B‑5 (die letzte
WinForms-Fachmaske ist Razor, `Sprungziel` leer, `Sprungbruecke` gelöscht, **`ScottPlot.WinForms` aus
`Directory.Packages.props`** — der erste Restore der iOS-Hülle ohne dieses Paket), W8‑O‑5c S1 (Einheitenregel, elf
Hüllen-Umrechnungen im Kern), W14a‑E‑8 B1/B3 (Emissionsquelle, BHKW-Investition) und W7‑B‑3 (Kennlinien der
Projekt-Wärmepumpe). Workload 17 s, Bau 57 s (0 Fehler), Simulatorstart 1 min 20 s, Erststart mit Seed-Kopie (66 MB),
Startmarken `SQLite 3.53.3` · `STRICT=117` (Erwartung aus der Seed-Datenbank: 117) · `Projekte=24` grün, Prüfmodus 1030
in 3 s (22 CSV, 150 Skalare), **iZ6-Vergleich gegen die neue Basis `2026-09-07_R4_Double` PASS (236 670 Werte)** und
**BYTE-GLEICH** (`diff -rq` leer, iOS-Simulator arm64). Damit ist die R4-Basis, die auf dem Linux-Läufer aus dem
`double`-Kern eingefroren wurde, auch auf Apple Silicon byte-gleich reproduziert — die Umstellung auf `double` hat die
Plattformgleichheit nicht angetastet. Der `kern.yml`-Lauf 203 zum selben Commit ist grün; Lauf 202 (`f44e89b`, nur
Dokumentation) fiel an `Der_Fortschritt_kommt_gedrosselt_an`, dem Wettlauf des Fortschrittsmelders der
Speicheroptimierung, der in `1fabbd1` behoben ist (Weitergabe unter dem Schloss, zehn Läufe grün). Ausgelöst per
`workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Fünfunddreißigster Lauf 34132905061 (`ios.yml`, `macos-26`, 07.09.2026, 14:25–14:37 UTC, 11 min 14 s)** auf
`556d3f9` — der dritte Tagesstand des 07.09.2026 und der erste Lauf gegen die Basis **R5** (W8‑O‑5d‑Q1/Q2: benannter
Zahlenrand an Speicherhysterese und BHKW-Volllastgrenze, keine `int`-Abschneidung mehr in `BhkwPlan`; Em‑9.8: die zehn
Emissionsskalare `Em.Kessel.*`/`Em.Bhkw.*` im Referenzexport). Workload 27 s, Bau 1 min 48 s (0 Fehler), Simulatorstart
2 min 50 s, Erststart mit Seed-Kopie (66 MB), Startmarken `SQLite 3.53.3` · `STRICT=117` (Erwartung aus der Seed-Datenbank:
117) · `Projekte=24` grün, Prüfmodus 1030 in 22 s — **22 CSV, 160 Skalare** (Lauf 34: 150; die zehn neuen sind die
Emissionsgrößen, wie in Kapitel 11 des Emissionskonzepts vorhergesagt), **iZ6-Vergleich gegen `2026-09-07_R5_Zahlenrand`
PASS (236 680 Werte, zehn mehr als in Lauf 34)** und **BYTE-GLEICH** (`diff -rq` leer, iOS-Simulator arm64). Damit
reproduziert Apple Silicon auch den Zahlenrand (`1e‑9 + 1e‑12 · |Schwelle|`) und die `double`-Rückgaben von `BhkwPlan`
byte-gleich — Projekt 1030 hat keinen Gebäudebedarf, seine Vektoren sind gegenüber R4 unverändert; der Beweis für die
geänderten Bedarfsprojekte bleibt der Linux-Läufer (12/12 byte-gleich beim Einfrieren). Die `kern.yml`-Läufe 204, 205
und 206 (`67cda00`, `f59047f`, `556d3f9`) sind grün. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis
Migrationsende.

**Sechsunddreißigster Lauf 34145451647 (`ios.yml`, `macos-26`, 07.09.2026, 16:55–17:03 UTC, 8 min 01 s)** auf
`17ef1e3` — der vierte Tagesstand des 07.09.2026 und der erste Lauf mit der **Katalogfilter-Stufe S1** (W14a‑E‑10:
Bausteine `Spaltenfilter` und `Katalogliste` im Standard `Raster`, QuickGrid `ColumnOptions`, `Katalograhmen`
untereinander, 49 neue Ressourcenschlüssel — jede lange Liste der `AppWurzel` läuft über dieses Raster) sowie
W8‑O‑5d‑Q3/Q4 (Zahlenrand an allen 17 Betriebsschwellen). Workload 24 s, Bau 1 min 17 s (0 Fehler), Simulatorstart
1 min 35 s, Erststart mit Seed-Kopie (66 MB), Startmarken `SQLite 3.53.3` · `STRICT=117` (Erwartung aus der Seed-Datenbank:
117) · `Projekte=24` grün, Prüfmodus 1030 in 6 s (22 CSV, 160 Skalare), **iZ6-Vergleich gegen `2026-09-07_R5_Zahlenrand`
PASS (236 680 Werte)** und **BYTE-GLEICH** (`diff -rq` leer, iOS-Simulator arm64) — die Zahlenränder der drei
BHKW-Fahrweisen und der Reservemarke ändern auf Apple Silicon ebenso wenig wie auf Linux (12/12 byte-gleich beim
Merge). Die `kern.yml`-Läufe 207 bis 210 (`3d51daf`, `322f614`, `c98790b`, `17ef1e3`) sind grün. Ausgelöst per
`workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

**Siebenunddreißigster Lauf 34152839454 (`ios.yml`, `macos-26`, 07.09.2026, 18:44–18:53 UTC, 8 min 50 s)** auf
`8da1020` — der fünfte Tagesstand des 07.09.2026 und der erste Lauf mit der **Katalogfilter-Stufe S2** (W14a‑E‑10:
die sieben Projektdialoge samt Wärmepumpe im Spaltenmodell, `Zweispaltenauswahl` untereinander, Spalte „im Projekt
verwendet", Filterstand je Katalog über die Sitzung) und dem **Schemaschritt 68** (Q7: `Firma` im Stromspeicherkatalog,
`SchemaStand.Zielversion` 68) — die Seed-Datenbank trägt damit erstmals den Stand 68 in die App. Workload 21 s, Bau
1 min 32 s (0 Fehler), Simulatorstart 2 min 06 s, Erststart mit Seed-Kopie (66 MB), Startmarken `SQLite 3.53.3` ·
`STRICT=117` (Erwartung aus der Seed-Datenbank: 117 — Schritt 68 ergänzt eine Spalte, keine Tabelle) · `Projekte=24`
grün, Prüfmodus 1030 in 6 s (22 CSV, 160 Skalare), **iZ6-Vergleich gegen `2026-09-07_R5_Zahlenrand` PASS (236 680
Werte)** und **BYTE-GLEICH** (`diff -rq` leer, iOS-Simulator arm64). Die `kern.yml`-Läufe 213 bis 215 (`bc9ba07`,
`8da1020`, `833ff69`) sind grün; der Lauf 216 (Ereignis `pull_request` auf `833ff69`) fiel an EINEM Test rot
(`ModulImportDialogTests.Der_Herstellerfilter_zeigt_nur_noch_die_Zeilen_des_Herstellers`, 5 erwartet, 155 gezählt),
den der Push-Lauf 215 auf demselben Commit grün führt — ein Wettlauf im Test, kein Rechenbefund; die Behebung nach
dem Muster W16b‑O‑2 läuft als eigene Aufgabe. Ausgelöst per `workflow_dispatch` unter der pauschalen Freigabe bis
Migrationsende.

**Achtunddreißigster Lauf 34161281262 (`ios.yml`, `macos-26`, 07.09.2026, 20:56–21:06 UTC, 9 min 53 s)** auf
`d4edc85` — der sechste Tagesstand des 07.09.2026 und der erste Lauf mit der Seed-Datenbank auf **Schemastand 69**
(W6‑B‑5: Schritt 69 repariert die PV-Modulkoeffizienten; die Basis heißt seither `2026-09-07_R6_PvKoeffizienten`),
dazu W6‑B‑4 (Strangtabelle ohne Rollen, Strangvorbelegung im Kern, `Auswahlfeld` mit `selected` und `@key` je Option)
und W6‑B‑2‑O‑1 (Wartehelfer der Importtests). Workload 27 s, Bau 1 min 36 s (0 Fehler), Simulatorstart 2 min 28 s,
Erststart mit Seed-Kopie (66 MB), Startmarken `SQLite 3.53.3` · `STRICT=117` (Erwartung aus der Seed-Datenbank: 117 —
Schritt 69 ändert Werte, keine Tabelle) · `Projekte=24` grün, Prüfmodus 1030 in 18 s (22 CSV, 160 Skalare),
**iZ6-Vergleich gegen `2026-09-07_R6_PvKoeffizienten` PASS (236 680 Werte)** und **BYTE-GLEICH** (`diff -rq` leer,
iOS-Simulator arm64). Projekt 1030 ist in R6 byte-gleich zu R5 — es rechnet mit keinem der reparierten Module; den
Nachweis der Reparatur selbst führt Projekt 1007 auf Linux (elf von zwölf Projekten byte-gleich, 1007 in acht Dateien
der PV-Kette, Ursache allein `T_NOCT`, Gegenbeweis im `protokoll.txt` der Basis). Die `kern.yml`-Läufe 223 bis 226
(`2a4ad5e`, `d4edc85`, je Push und Pull-Request) sind grün. Ausgelöst per `workflow_dispatch` unter der pauschalen
Freigabe bis Migrationsende.

**Neununddreißigster Lauf 34164138879 (`ios.yml`, `macos-26`, 07.09.2026, 21:43–21:52 UTC, 9 min 37 s)** auf
`c53b39b` — der siebte Tagesstand des 07.09.2026 und der erste Lauf mit der **Katalogfilter-Stufe S3** (W14a‑E‑10:
Bedarfs- und Zeitreihenkataloge im Spaltenmodell, Jahresarbeit und Spitze aus einer Gruppenabfrage, Vergleich von zwei
bis drei markierten Zeilen im Baustein, beide Importmasken auf der `Katalogliste` — 21 Dialoge in 16 Komponenten auf
EINER Liste, alle in der `AppWurzel` erreichbar) sowie dem Anwender-Merge 7 (`159f3f8`: PV-Reiter der Ergebnisseite,
W11b‑B‑6 bis B‑10 — Erzeugung statt genutztem Anteil, W/m², Flächenschätzung, Diagrammbreite). Workload 24 s, Bau
1 min 10 s (0 Fehler), Simulatorstart 1 min 56 s, Erststart mit Seed-Kopie (66 MB, Schemastand 69), Startmarken
`SQLite 3.53.3` · `STRICT=117` (Erwartung aus der Seed-Datenbank: 117) · `Projekte=24` grün, Prüfmodus 1030 in 22 s
(22 CSV, 160 Skalare), **iZ6-Vergleich gegen `2026-09-07_R6_PvKoeffizienten` PASS (236 680 Werte)** und
**BYTE-GLEICH** (`diff -rq` leer, iOS-Simulator arm64) — die drei Aggregatabfragen der Zeitreihenkataloge und der
Vergleich sind Anzeige, kein Rechenweg. Die `kern.yml`-Läufe 227 bis 234 (`b2ee331`, `ba6f8d5`, `c53b39b`, `8a6ca2b`,
je Push und Pull-Request) sind grün; das Gate auf Linux zählt seit S3 Kern 2 033 und UI 3 239 Fälle. Ausgelöst per
`workflow_dispatch` unter der pauschalen Freigabe bis Migrationsende.

## Nachweise, die nur ein Gerät führen kann — offen (iU13)

Sie brauchen ein Apple-Developer-Konto (iF24), ein Signaturzertifikat und ein iPad.

**iF24 — entschieden am 03.09.2026: Konto beschaffen.** Gemeint ist die **Mitgliedschaft im
Apple Developer Program** (99 US-Dollar/Jahr), nicht die kostenlose Apple ID: Die Apple ID ist
nur das Benutzerkonto, an das die Mitgliedschaft gebunden wird. Ohne Mitgliedschaft signiert
Xcode höchstens für eigene Geräte („Personal Team“, Signatur läuft nach 7 Tagen ab, 3 Apps);
TestFlight, App Store Connect, Bundle-ID-Registrierung, Distribution-Zertifikat und Apple
Business Manager gibt es erst mit dem Programm. Das Enterprise Program (299 US-Dollar) ist für
rein interne Apps und für den Verkauf an Kunden ungeeignet. Die Schritte, die nur der Anwender
gehen kann (die CI übernimmt danach die Signierkette, iE9):

1. Apple ID der Firma mit Zwei-Faktor-Anmeldung anlegen (nicht die private ID eines
   Mitarbeiters — das Konto trägt später die App).
2. Im Apple Developer Program als **Organisation** einschreiben (99 US-Dollar/Jahr). Dafür
   verlangt Apple eine **D-U-N-S-Nummer** der INEKON; Prüfung durch Dun & Bradstreet dauert
   Tage bis Wochen, die Einschreibung selbst nochmals einige Tage. Eine Einzelperson-Einschreibung
   ginge schneller, kann aber später nicht in eine Organisation umgewandelt werden.
3. Nach der Freischaltung in App Store Connect die **Bundle-ID `de.inekon.eposplan`** (aus
   `EPOS.iOS/EPOS.iOS.csproj`) registrieren und einen App-Eintrag anlegen.
4. Ein **Apple-Distribution-Zertifikat** (`.p12` mit Kennwort) und ein
   **Provisioning-Profil** für TestFlight erzeugen.
5. Beides als Repository-Geheimnisse hinterlegen (`IOS_P12_BASE64`, `IOS_P12_PASSWORD`,
   `IOS_PROFILE_BASE64`); `ios.yml` bekommt in iU13 den Signierschritt. Geheimnisse gehören
   nie in einen Commit.
6. Für einen späteren Vertrieb außerhalb des Stores: **Apple Business Manager** prüfen
   (Custom Apps, § 3.4 des Umsetzungskonzepts; entschärft die Provisionsfrage iR7).

**iF25 — entschieden am 03.09.2026: E1 für die CI, E2 für TestFlight.** Die CI baut weiter mit der
Testdatenbank (`-p:SeedDb`, 77 MB). Vor iU13 misst der Anwender unter Windows, wie groß der
Produktivstand als Seed wird — die Produktivdatenbank liegt nicht im Repo:

```
sqlite3 Kenndaten.sqlite "VACUUM INTO 'Kenndaten_seed.sqlite'"
dir Kenndaten_seed.sqlite
```

Die so erzeugte Datei wird in iU13 per `-p:SeedDb=<Pfad>` gebaut. Liegt sie deutlich über
150 MB, ist vor dem Store-Weg zu prüfen, welche Massendatentabellen (Klima- und Solardaten)
sich beim Erststart nachladen lassen (E3). E4 (Volldownload beim Erststart) nur bei Store-Vorgaben.

- [ ] **AOT statt JIT.** Der Simulator führt JIT aus, das Gerät nicht. Zu prüfen sind die
      Stellen, die Reflection oder Startzeitmagie benutzen: der `[ModuleInitializer]` in
      `SimulationControl.Stromspeicher.cs`, `ApplicationSettingsBase` in `Properties/Settings`,
      die 102 Dateien mit `DataTable`, BouncyCastle und die Ressourcensatelliten (iR-e).
      `MtouchLink=SdkOnly` belassen — der Kern darf nicht getrimmt werden.
- [ ] **Signierkette und `.ipa`** (iU13, § 3.4).
- [ ] **Speicher und Laufzeit.** Eine 8760-Stunden-Simulation mit Kaskade auf einem iPad: Dauer,
      Spitzenspeicher, kein Abschuss durch den Jetsam.
- [ ] **Bedienung mit dem Finger.** Berührungsziele ≥ 44 px, Tastatur, Drehen des Geräts, die
      sicheren Abstände unter der Home-Anzeige (`viewport-fit=cover`).
- [ ] **Lizenz.** Das iPad ist am Lizenzserver ein **neues Gerät** (`identifierForVendor`); die
      Zahl der gebundenen Geräte je Lizenz ist zu klären (iF12).
- [ ] **Dateien-App.** Berichte, CSV und die Datenbanksicherung sind unter „Auf meinem iPad →
      EPOS-Plan" sichtbar (`UIFileSharingEnabled`).
- [ ] **Bericht.** `WordBerichtGenerator` findet `Vorlagen/Berichtsvorlage.docx` im Paket (iR-g);
      der Bericht selbst ist iU11.

---

## Nachtrag iU9-W10b (04.09.2026) — die erste FACHSEITE im iOS-Einstieg

Bis hierher kannte `AppWurzel` drei Ansichten: die Projektliste und zwei Dialoge. Mit
**iU9-W10b** kommt die **Simulationskonfiguration** als vierte dazu —
`Seitenschluessel.SimulationKonfiguration` und ein Zweig in `AppWurzel.Zeige`, gebaut nach
dem Muster `BhkwWirtschaftlichkeit`. Sie ist die erste **Fachseite** (kein Dialog), die die
Wurzel zeigt, und damit der erste Beleg dafür, dass eine Seite mit Überlagerungen auf dem
iPad genauso läuft wie unter Windows: Die sieben Dialoge der Welle 10a erscheinen darin als
Überlagerung, ohne zweites Fenster.

- [x] `dotnet build EPOS.UI -c Release` → **0 Fehler, 0 Warnungen**.
- [x] `dotnet test WP-Plan.Kern.slnf -c Release` → **2 379/2 379**, darunter 26 bunit-Fälle
      für die Seite und 7 für `AppWurzel` (unverändert).
- [ ] **`IProjektQuelle` ist gewachsen** — `SimulationKonfigGaben(int idProjekt)` liefert den
      Parametersatz. Die Methode hat eine **Standardumsetzung** (`=> null`), damit
      `EPOS.iOS/Dienste/IosProjektQuelle` durch die Erweiterung **nicht bricht**: `EPOS.iOS`
      steht bewusst weder in `WP-Plan.sln` noch im Solution-Filter, ein Pflichtmitglied hätte
      den iOS-Job stumm gebrochen. **Offen:** Solange `IosProjektQuelle` die Methode nicht
      umsetzt, meldet die Wurzel „Zu diesem Projekt lässt sich die Simulationskonfiguration
      nicht öffnen" — die Seite ist auf dem Gerät also noch nicht erreichbar. Das Nachziehen
      der Quelle (Controller, Delegatensatz, Texte) ist ein eigener Schritt in **iU11**.
- [ ] **iOS-Job einmal laufen lassen** (`Actions → iOS → Run workflow`, bis Migrationsende
      pauschal freigegeben). Er baut `EPOS.iOS` gegen die erweiterte Schnittstelle; die
      Standardumsetzung muss ihn tragen, ohne dass die Hülle angefasst wurde.

---

## Was iU10 bewusst **nicht** tut

- **Kein Wizard.** `AppWurzel` ist eine Zustandsmaschine mit VIER Ansichten (seit iU9-W10b),
  kein Router. Der
  iL5-Wizard (Projekt → Bedarf → Erzeuger → Simulation → Bericht) ist **iU10-9**.
- **Kein Anlegen einer Energieträger-Variante.** Der Schreibweg steht bis heute in
  `Views/Kosten/Form_Kosten.CreateNewEnergyCarrier` und hängt dort am Typ `EnergyCarrier` und an
  `EnergietraegerKatalogCtrl`. Ihn in der iOS-Hülle nachzubauen wäre genau die Doppelpflege, die
  Modell C abschafft; er wandert mit dem Umzug der Kostenmasken in den Kern. Der Dialog läuft
  vollständig, nur das Schreiben unterbleibt.
- **Keine KI-Semantiksuche.** Die native Hälfte von OnnxRuntime bleibt per `ExcludeAssets`
  draußen (iF26); `SemantikModell` scheitert erst beim Aufruf, nicht beim Start. Chat und Wiki
  laufen über REST und sind unberührt.
- **Kein Hilfe-Zwischenspeicher.** `IosHilfeDienst` löst über dieselbe `help_mapping.txt` auf wie
  Windows und öffnet die richtige Wikiseite; Kurztext und Beschreibung aus dem `HelpCatalog`
  fehlen noch (iU11).
- **Kein Geräte-Backup-Ausschluss.** Die Datenbank liegt unter `Application Support` und wird von
  iCloud gesichert. Das ist gewollt: Eine gerechnete Variante ist Arbeit, keine Zwischenablage.

> **Regel (Anwender, 03.09.2026):** Vor jedem Aufruf des macOS-Läufers nachfragen; der Job startet nie von selbst.
