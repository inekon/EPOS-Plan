# ZU26 — Katalog der Brauchwasser-Nutzungsarten auf iOS (26.09.2026)

Protokoll des Postens **#524**. Auftrag: den Katalogdialog „Brauchwasser-Nutzungsarten" auf iOS
öffnen (Anwenderentscheid ZU26, N19), mit Naht nach dem Muster der Baustoffkataloge, einem Import
ohne Ordnerwahl und einem Nachweis im Prüfmodus der iOS-Schale. Zweig `zx`, Worktree
`.claude/worktrees/zx`, Opus 5.5. Kein Schemaschritt, Testdatenbank unberührt, Windows-Weg
unverändert.

Der Agent des Postens ging nach dem Code durch einen Sitzungsabbruch verloren; ein zweiter Agent hat
den Stand aufgenommen, gegen den Auftrag geprüft, eine Berichtigung ergänzt, die Papiere geschrieben
und das Gate gezogen. Die Festlegungen stehen als **Nachtrag N23** im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); dieses
Protokoll führt die Prüfung, die Abweichungen und die Gates.

---

## 1. Commits

| Commit | Inhalt |
|---|---|
| `bc26a5e2` | Katalog als Ansicht der `AppWurzel` (Naht `IProjektQuelle.NutzungsartKatalogGaben`), Import ohne Ordner (`IDateiDienst.OrdnerwahlMoeglich`, `OrdnerwahlVerfuegbar`, drei Texte), Kern-Probe `TwwKatalogprobe`, Tests |
| `5f952731` | iOS-Schale: `IosProjektQuelle`, `IosDateiDienst`, `Pruefung/Katalogprobe.cs`, Aufruf in `Prueflauf.cs`, sieben `MauiAsset` in `EPOS.iOS.csproj`, `ios.yml` (Schalter und Schritt „Katalogprobe auswerten") |
| `8c7ddc6c` | Merge von `origin` |
| `15ce63aa` | Wiki-Quelle (zwei Sätze), `EPOS.iOS/CLAUDE.md` |
| `a02258c5` | Berichtigung: „Beenden" führt auf dem iPad zur Startansicht (Wiki-Satz, Testkommentar) |
| `6a9cdf24`, `fe80bd80` | Merges von `origin` (CI-Fix #522 samt Papieren; Papiere E47), konfliktfrei |

---

## 2. Prüfung gegen den Auftrag

| Punkt | Befund |
|---|---|
| Naht nach Baustoff-Muster | vollständig: Positivliste `OeffneMaske`, Weiche in `Zeige()`, Render-Zweig, Schließen über `ZurueckZurListe`, benannte Ablehnung `ZPGK_KEINE_ANSICHT` ohne Parametersatz; Standardumsetzung `null` in `IProjektQuelle`. Anders als bei G3 **ohne** Delegat des Hauptfensters — unter Windows fängt `WinFormsNavigation` den Schlüssel ab |
| Einstieg auf iOS | über den Hilfe-Assistenten (`dialog_oeffnen`, vier `KiMaskenziele` auf `Masken.BrauchwasserNutzungsarten`, `IosNavigation` → `OeffneMaske`); die iOS-Schale hat kein Menü. Derselbe Weg wie Baustoffe und Bauteilaufbauten — erreichbar, aber an die Verfügbarkeit des Assistenten gebunden (Folge ZU34) |
| ZIP im Dokumentenwähler | `Dateifilter` bildet `.zip` auf `public.zip-archive` ab (Bestand); der Dialog reicht auf iOS den Filter `(*.zip)` durch, auch der Rückfallfilter von `PaketWaehlen` folgt der Ordnerwahl |
| benannte Ordnerwahl-Sperre | `IDateiDienst.OrdnerwahlMoeglich` (Fähigkeitsfrage, Standard `true`; iOS `false`) → Knopf „ZIP-Paket wählen…", Satz `ZPGK_IMPORT_NUR_ZIP` vor dem Knopf |
| Prüfmodus | `EPOS_PRUEFLAUF_KATALOGIMPORT`, sieben CSV als `MauiAsset` (Präfix `katalogprobe_`, flache Namen), Laufzeit-ZIP, Probe im Kern; Aufruf außerhalb des xBIM-Blocks vor der Fertigmarke; die Zeilen, die `ios.yml` greift, stimmen mit `TwwKatalogprobe` überein (`paket ergebnis=GELESEN`, `lauf=pruefung|import ergebnis=OK`, `vergleich ergebnis=GLEICH`, `abgelehnt bereich=`, `ende ergebnis=OK befunde=0`, `ordnerwahl=NEIN`) |
| KI-Sicht | kein neues Eingabefeld; `KiMaskenabdeckungWacheTests` unverändert grün |
| Windows-Weg | unverändert: Fenster `TwwNutzungsartAdminHuelle`, Knopftext und Filter wie bisher (`OrdnerwahlVerfuegbar` = `true`) |
| Ressourcen | vier Schlüssel in beiden Sprachen, Designer wiederholbar |

**Auf Windows nicht prüfbar:** Die iOS-Schale baut nur auf macOS. Gelesen und gegen die
Schnittstellen gehalten sind `IosProjektQuelle.NutzungsartKatalogGaben` (Signatur wie
`IProjektQuelle`), `IosDateiDienst.OrdnerwahlMoeglich`, `Katalogprobe` (Namensräume `EPOS.iOS` und
`WindowsFormsApplication1`, `System.IO.Compression`, Zugriff auf interne Klassen über die vorhandenen
`InternalsVisibleTo` von `EPOS.Kern` und `EPOS.UI.Daten`), der Aufruf in `Prueflauf.cs` (Variable
`wurzel` im Gültigkeitsbereich) und die sieben `MauiAsset`-Einträge gegen die Dateien des
Probepakets. Übersetzung und Probe beweist erst der iOS-Lauf.

---

## 3. Abweichungen

| Nr. | Abweichung | Grund |
|---|---|---|
| A1 | „Beenden" führt zur Startansicht, nicht in die Ansicht, aus der der Katalog geöffnet wurde | Muster der Kataloge der Gebäudesimulation (kein Rückwegstapel für Kataloge). Der Wiki-Satz des ersten Agenten hatte Letzteres behauptet; berichtigt in `a02258c5` |
| A2 | Probedateien ohne Bauschalter in jedem Paket | rund 3 KB; ein eigener Bauweg wie bei den Importproben kostet mehr als die Dateien. Die Probe läuft trotzdem nur auf Zuruf |
| A3 | Die Probe verändert den Katalog der Simulator-Datenbank | Sie läuft nach dem Rechennachweis; die Datenbank endet mit dem Lauf |
| A4 | Einstieg auf iOS nur über den Hilfe-Assistenten | Die iOS-Schale hat kein Menü (E-1). Ein eigener Einstieg für diesen einen Katalog wäre ein Vorgriff auf iU11 (Folge ZU34) |

---

## 4. Gates

| Gate | Ergebnis |
|---|---|
| Kern-Filter (vor den Merges) | 0 Fehler |
| voller Testlauf (vor den Merges) | 0 Fehler, 15 099 erfolgreich |
| Windows-Schale (vor den Merges) | 0 Fehler |
| iOS-Schale | auf Windows nicht baubar — Nachweis im iOS-Lauf |
| Kern-Filter nach dem Merge `fe80bd80` | 0 Fehler |
| gefilterte Tests (`AppWurzel`, `Katalog`, `Tww`, `Zapfprofil`, `KiMasken`, Dokumentations-, Wiki- und Ordnungswache) | 2 761 erfolgreich |
| voller Testlauf `WP-Plan.Kern.slnf` nach dem Merge `fe80bd80` | 0 Fehler, 15 099 erfolgreich, 2 übersprungen (EPOS.Kern.Tests 7 679, EPOS.UI.Tests 6 458, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27) |
| Wachen auf `bc67ef7c` (danach nur Papiere) | 31 erfolgreich |
| Windows-Schale nach dem Merge (`EnableWindowsTargeting`) | 0 Fehler |
| `designer_neu.py schreiben` | +0, wiederholbar; Datei mit CRLF belassen |
| Wiki-Tabuwörter in den geänderten Wiki-Zeilen | 0 |

---

## 5. Folgen

- **iOS-Lauf** (`ios.yml`, zählt zehnfach) nach Freigabe des Anwenders: Übersetzung der Schale und
  Katalogprobe; bis dahin ist ZU26 „umgesetzt, iOS-Lauf ausstehend".
- **ZU34** (Anwenderentscheid): Einstieg in die Kataloge auf dem iPad, der nicht am Hilfe-Assistenten
  hängt — Empfehlung: mit iU11 für alle Kataloge, nicht einzeln für diesen.
- **iU11**: die übrigen Katalogverwaltungen nach iF2.
- **Wiki**: die zwei Sätze der Bedienseite gehen mit dem nächsten Sammel-Upload; Logbuch-Satz unter
  1.2.0.4 in `Wiki_Update_2026-09-26.md`.
