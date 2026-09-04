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
      `Referenzlaeufe/2026-08-30_B3-Kaskade/Projekt_1030` (Toleranz rel. 1e-4 / abs. 0,01, iF15).
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
