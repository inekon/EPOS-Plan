# Bericht CWN — Nachlese CI-Wächter: Quelltextleser in eigene Datei (#533)

Stand 26.09.2026 08:16, Worktree `.claude/worktrees/cwn`, Zweig `cwn`, Basis ffa196be9. Kein Push, kein Merge, kein Stash.
Nur Testcode. Kein Produktcode, kein Schema, keine Papiere, `CLAUDE.md` unverändert. Arbeitsbaum sauber.

## 1. Commit

| SHA | Betreff | Dateien |
|---|---|---|
| 6e01aed38 | CWN #533: Quelltextleser aus KulturwaechterTests auslagern | `EPOS.Kern.Tests/KulturwaechterTests.cs` (+7/−488), `EPOS.Kern.Tests/Quelltextleser.cs` (neu, 511) |

`git diff --stat` ffa196be9..6e01aed38: 2 Dateien, 518 Einfügungen, 488 Löschungen.

## 2. Zeilen (`wc -l`)

| Datei | vorher | nachher |
|---|---|---|
| `EPOS.Kern.Tests/KulturwaechterTests.cs` | 1.291 | 810 |
| `EPOS.Kern.Tests/Quelltextleser.cs` | – | 511 |

Beide Dateien sind UTF-8 mit BOM und CRLF (`file`, `QuelltextKodierungWacheTests` grün).

## 3. Was verschoben wurde

Neu ist `internal static class Quelltextleser` im Namensraum `EPOS.Kern.Tests`. Der Kopfkommentar nennt den Zweck, die Herkunft (#531) und diese
Nachlese (#533). Die XML-Kommentare der Mitglieder sind wortgleich mitgewandert.

- **Aus dem Abschnitt „Werkzeug — Quelltextleser“ (vorher Zeilen 742–1208), unverändert:**
  - Typen `Feld`, `Klasse`, `Quelle` (mit `Lies`, `AusText`, `Zeile`, `Ort`, `IstCode`, `InnersteKlasse`, `RelativeTiefe`), `Klassenbestand` (mit `NutztVorrichtung`)
  - `ErsteBasis`, `Klassenkopf`, `LiesKlassen`, `UeberspringeLeer`, `Schliessende`
  - Maskierer: `Maskiere`, `Leeren`, `Zeilenende`, `ZeichenEnde`, `ZeichenketteEnde`
- **Aus dem Wächter-A-Abschnitt, weil `Quelle.Baue` sie braucht:**
  - `Vorrichtungsdeklaration` (auch von Wächter A benutzt)
  - `Feldvorlauf` (nur vom Leser benutzt)
  - `Anweisungsvorlauf` (auch von Wächter A benutzt)
  - Die drei stehen unter einer eigenen Abschnittsüberschrift „Felderkennung“.
- **Sichtbarkeit:** Die vier Typen sowie `Vorrichtungsdeklaration` und `Anweisungsvorlauf` wurden von `private` auf `internal` gestellt. Alles
  andere bleibt `private`. Die Mitglieder der Typen waren schon `public`.
- **In `KulturwaechterTests`:**
  - `using static EPOS.Kern.Tests.Quelltextleser;`, damit alle Aufrufe wortgleich bleiben.
  - `using System.Text;` entfällt.
  - Der Verweis in der Klassendoku lautet jetzt `cref="Quelltextleser.Quelle"`.
  - Die Klassendoku hat einen neuen Absatz zur Auslagerung.
- **Unverändert in `KulturwaechterTests`:**
  - alle Regeln und Meldungen, die 19 Fälle
  - die Wächter-A-Regexe `EntsorgbareBasis`, `NeueVorrichtung`, `MitNeuerVorrichtung`, `FeldvorlaufMitTyp`, `UsingVorlauf`, `Zuweisungsvorlauf` und `IstFeldzuweisung`
  - die Regionenhilfen `InRegion`, `RestoreRegionen`, `SchliessendeKlammer`
  - die Datei- und Arbeitsbaumhilfen

## 4. Testläufe

Alle Läufe liefen mit `--filter`, `-c Release --no-build` und den xUnit-Schaltern, nach der Testhost-Regel. Es lief kein fremder testhost.

| Lauf | Stand | Ergebnis |
|---|---|---|
| Release-Build `WP-Plan.Kern.slnf` | ffa196be9 | 0 Fehler (53 Warnungen, Bestand) |
| Kulturwächter vorher | ffa196be9 | 19/19 grün, 4 s |
| Gegenprobe vorher | ffa196be9 ohne `IDisposable` an `DwdTryLeserTests` | Wächter A rot, 1 Meldung (siehe 5) |
| Release-Build `WP-Plan.Kern.slnf` | nach der Auslagerung | 0 Fehler, keine Warnung aus den zwei Dateien |
| Kulturwächter nachher | nach der Auslagerung | 19/19 grün, 4 s. Die Liste der Fall-Namen und Ergebnisse ist gleich wie vorher (`diff` leer) |
| Kodierungs- und Dokumentationswachen | nach der Auslagerung | 37/37 grün: `QuelltextKodierungWacheTests` 6, `DokumentationLinkWacheTests` 9, `RepositoryOrdnungWacheTests` 14, `WikiProduktdatenWacheTests` 8 |
| Gegenprobe nachher | nach der Auslagerung ohne `IDisposable` an `DwdTryLeserTests` | Wächter A rot, dieselbe Meldung |
| Schlusslauf | nach dem Zurücknehmen, neu gebaut | Kulturwächter und `QuelltextKodierungWacheTests` 25/25 grün |

Die 19 Kulturwächter-Fälle, vorher und nachher grün:
- `Jeder_DefaultThreadCurrentCulture_Setzer_hat_eine_Rueckstellung`
- `Jede_Kulturzuweisung_in_EPOS_UI_Tests_hat_eine_Rueckstellung_oder_nutzt_die_Vorrichtung`
- `Jede_Kulturvorrichtung_wird_entsorgt`
- `Die_Standardkultur_en_US_steht_in_beiden_Testprojekten`
- `Ohne_Pinnung_gilt_en_US_und_die_Vorrichtung_stellt_darauf_zurueck`
- `Der_Leser_erkennt_beide_Schreibweisen`
- `Ein_Setzer_ausserhalb_von_Dispose_und_finally_zaehlt_nicht`
- `Der_UI_Leser_erkennt_alle_sechs_Ziele`
- `Der_Waechter_sieht_den_Bestand`
- `Der_Waechter_sieht_auch_den_UI_Bestand`
- `Eine_Klasse_die_die_Vorrichtung_nutzt_braucht_keine_eigene_Rueckstellung`
- `Ohne_Vorrichtung_und_ohne_Rueckstellung_faellt_eine_Datei_auf`
- `Die_Tuer_des_UI_Waechters_gilt_je_Klasse_nicht_je_Datei`
- `Ein_Vorrichtungsfeld_ohne_IDisposable_faellt_auf`
- `IDisposable_an_einer_verschachtelten_Klasse_zaehlt_nicht_fuer_die_aeussere`
- `Ein_Vorrichtungsfeld_ohne_Dispose_Ruf_faellt_auf`
- `Richtig_entsorgte_und_gebundene_Vorrichtungen_bleiben_still`
- `Eine_Vorrichtung_ohne_using_faellt_auf`
- `Der_Waechter_A_sieht_die_Vorrichtungsfelder_beider_Projekte`

Ein voller Kern-Lauf war nicht beauftragt. Ihn fährt das Gate der Hauptsitzung.

## 5. Gegenprobe

Ich habe `IDisposable` an `DwdTryLeserTests` einmal vor und einmal nach der Auslagerung entfernt. Beide Male wurde
`Jede_Kulturvorrichtung_wird_entsorgt` rot. Die Meldung ist wortgleich:

> EPOS.Kern.Tests/DwdTryLeserTests.cs:26 DwdTryLeserTests (Feld _kultur): ohne IDisposable/IAsyncDisposable in der eigenen Basisliste

Beide Änderungen habe ich zurückgenommen, nicht committet. `git status` ist sauber.

## 6. Aufwand

Rund 12 Minuten (08:05–08:17). Davon waren etwa 3 Minuten Bau und Tests. Es gab keine Wartezeit auf fremde testhosts.

## 7. Restpunkte

- **Der Leser ist noch auf die Kulturvorrichtung zugeschnitten.**
  - `Klasse.Vorrichtungsfelder` und `Quelle.Baue` erkennen nur Felder vom Typ `Kulturvorrichtung`, über `Vorrichtungsdeklaration`.
  - `Klassenbestand.NutztVorrichtung` kennt `EposBunitContext`.
  - Eine andere Quelltext-Wache kann `Maskiere`, `Quelle` (Maske, Zeile, Ort, IstCode, Klassen, Tiefe) und den Klassenleser schon nutzen.
  - Wer andere Feldtypen braucht, sollte den Feldtyp als Parameter herausziehen. Das ist ein eigener kleiner Schritt und bewusst nicht Teil
    dieser reinen Verschiebung.
- **Die Kommentar-Nummer #533** ist vorgesehen. Beim Push misst die Hauptsitzung, ob sie frei ist. Sonst stellt sie Betreff, Kopfkommentar
  von `Quelltextleser.cs` und Klassendoku von `KulturwaechterTests` um.
- **Roslyn (Variante b)** bleibt Anwenderentscheid.

## Teil 2: Standardkultur der drei kleinen Testprojekte

Nachtrag der Hauptsitzung (Anwenderentscheid 26.09.2026: „jetzt vorsorgen“). Stand 08:20, Zweig `cwn`, auf 6e01aed38. Kein Push, kein
Merge. Der Arbeitsbaum ist sauber.

### 1. Commit

| SHA | Betreff | Dateien (Zeilen) |
|---|---|---|
| 64f470cc4 | CWN #533: Standardkultur en-US und runner.json in drei Testprojekten | 10 Dateien, +185/−13 (siehe unten) |

- **`StandardkulturEnUs.cs`** (je 38 Zeilen, neu) in `KiKern.Tests`, `SpeicherEngine.Tests` und `SpeicherPlanung.Tests`.
  - Muster wie in `EPOS.Kern.Tests`: `[ModuleInitializer]` setzt `CultureInfo.DefaultThreadCurrent(UI)Culture` und
    `Thread.CurrentThread.Current(UI)Culture` auf `"en-US"`. CA2255 ist örtlich unterdrückt.
  - Namensraum des jeweiligen Projekts.
  - Kopfkommentar gekürzt, mit Verweis auf die Kern-Datei, #531 und #533.
  - UTF-8 mit BOM, CRLF.
- **`xunit.runner.json`** (je 5 Zeilen, neu) in denselben drei Projekten. Sie ist bytegleich zur Datei in `EPOS.Kern.Tests`, UTF-8 ohne BOM.
- **csproj** der drei Projekte (je +7): `<None Update="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />` mit demselben Kommentar wie
  in `EPOS.Kern.Tests.csproj`. Kodierung und Zeilenenden bleiben erhalten.
- **`EPOS.Kern.Tests/KulturwaechterTests.cs`** (+35/−13):
  - `StandardkulturDateien` nennt jetzt fünf Pfade. Das gilt für die Pfad-Ausnahme und für den Standardkultur-Wächter.
  - Der Fall heißt jetzt `Die_Standardkultur_en_US_steht_in_allen_Testprojekten`. Die Meldung lautet „… (Auftrag #531/#533).“
  - Neue Liste `KleineTestprojekte`.
  - Der Setzer-Wächter `Jeder_DefaultThreadCurrentCulture_Setzer_hat_eine_Rueckstellung` suchte bisher nur `EPOS.Kern.Tests` ab. Über
    `Testdateien()` sucht er jetzt auch die drei kleinen Projekte ab.
  - Die Bestandsprobe `Der_Waechter_sieht_den_Bestand` verlangt zusätzlich `KiAbsichtTests.cs`, `FlottenDiagnoseTests.cs` und
    `OrToolsFlottenPlanerTests.cs`.
  - Die Klassendoku hat dazu einen Satz mehr.
  - Unverändert: der UI-Wächter, Wächter A und `Testprojekte`, weil es in den drei Projekten keine Kulturvorrichtung gibt.

### 2. Befund Kultursetzer (vorher, grep `CultureInfo|CurrentCulture|Kulturvorrichtung`)

- **Keine `Kulturvorrichtung`** und **kein prozessweiter Setzer** (`DefaultThreadCurrent(UI)Culture`) in den drei Projekten. Der Bestand für
  den erweiterten Setzer-Wächter war 0.
- **Threadgebundene Pins mit Rückstellung gibt es, und alle stellen sauber zurück:**
  - **KiKern.Tests:**
    - `KiAbsichtTests.cs:258` setzt de-DE, `KiPruefungTests.cs:349` setzt de-DE, `KiProtokollTests.cs:63` setzt en-US. Alle drei stellen in `finally` zurück.
    - Sonst gibt es nur `CultureInfo`-Konstanten als Formatargumente (`De`/`En` in `KiBestaetigung*`, `KiFeldBlock`, `KiZahlenreihe`).
  - **SpeicherEngine.Tests:**
    - Fünf `Flotten*Tests` setzen im Konstruktor `CultureInfo.CurrentCulture` und `Thread.CurrentThread.CurrentCulture` auf de-DE und
      stellen in `Dispose()` zurück.
    - `GanglinienPruefungTests.cs:593/641` stellt in `finally` zurück.
    - Sonst gibt es nur `InvariantCulture`.
  - **SpeicherPlanung.Tests:** keine Treffer.
- **Gepinnte Fälle:** keine. Kein Fall erwartet ungepinnt deutsche Texte oder Zahlformate. Unter en-US sind alle grün, und keine Assertion
  wurde geändert.

### 3. Nachweise

Alle Läufe liefen mit `-c Release --no-build`, nach der Testhost-Regel. Es lief kein fremder testhost.

| Lauf | Ergebnis |
|---|---|
| Release-Build `WP-Plan.Kern.slnf` | 0 Fehler (53 Warnungen, Bestand) |
| `xunit.runner.json` in `bin/Release/net10.0` | liegt in allen drei Projekten, `cmp` bytegleich zur Kern-Datei |
| Vorher (ohne Standardkultur), mit Schaltern | KiKern 549/549, SpeicherEngine 386/386, SpeicherPlanung 27 grün + 1 übersprungen (28) |
| Nachher, **mit** Schaltern | KiKern 549/549, SpeicherEngine 386/386, SpeicherPlanung 27 grün + 1 übersprungen (28) |
| Nachher, **ohne** Schalter | KiKern 549/549, SpeicherEngine 386/386, SpeicherPlanung 27 grün + 1 übersprungen (28) |
| Kulturwächter und `QuelltextKodierungWacheTests` | 25/25 grün |
| Nach dem Commit: Kodierung und die drei Dokumentationswachen | 37/37 grün, jetzt mit den versionierten neuen `.cs` |

Die 19 Kulturwächter-Fälle nach Teil 2, alle grün:
- `Jeder_DefaultThreadCurrentCulture_Setzer_hat_eine_Rueckstellung`
- `Jede_Kulturzuweisung_in_EPOS_UI_Tests_hat_eine_Rueckstellung_oder_nutzt_die_Vorrichtung`
- `Jede_Kulturvorrichtung_wird_entsorgt`
- **`Die_Standardkultur_en_US_steht_in_allen_Testprojekten`** (umbenannt)
- `Ohne_Pinnung_gilt_en_US_und_die_Vorrichtung_stellt_darauf_zurueck`
- `Der_Leser_erkennt_beide_Schreibweisen`
- `Ein_Setzer_ausserhalb_von_Dispose_und_finally_zaehlt_nicht`
- `Der_UI_Leser_erkennt_alle_sechs_Ziele`
- `Der_Waechter_sieht_den_Bestand`
- `Der_Waechter_sieht_auch_den_UI_Bestand`
- `Eine_Klasse_die_die_Vorrichtung_nutzt_braucht_keine_eigene_Rueckstellung`
- `Ohne_Vorrichtung_und_ohne_Rueckstellung_faellt_eine_Datei_auf`
- `Die_Tuer_des_UI_Waechters_gilt_je_Klasse_nicht_je_Datei`
- `Ein_Vorrichtungsfeld_ohne_IDisposable_faellt_auf`
- `IDisposable_an_einer_verschachtelten_Klasse_zaehlt_nicht_fuer_die_aeussere`
- `Ein_Vorrichtungsfeld_ohne_Dispose_Ruf_faellt_auf`
- `Richtig_entsorgte_und_gebundene_Vorrichtungen_bleiben_still`
- `Eine_Vorrichtung_ohne_using_faellt_auf`
- `Der_Waechter_A_sieht_die_Vorrichtungsfelder_beider_Projekte`

### 4. Gegenproben (nicht committet)

- **(a)** `SpeicherEngine.Tests/StandardkulturEnUs.cs` vorübergehend aus dem Arbeitsbaum genommen. `Die_Standardkultur_en_US_steht_in_allen_Testprojekten`
  wurde rot mit „Die Standardkultur fehlt: SpeicherEngine.Tests/StandardkulturEnUs.cs (Auftrag #531/#533).“ Danach zurückgelegt, 19/19 grün.
- **(b)** Zur Ausdehnung des Setzer-Wächters: `KiKern.Tests/ZZ_KulturprobeFremd.cs` mit `CultureInfo.DefaultThreadCurrentCulture = null;` ohne
  Rückstellung angelegt. `Jeder_DefaultThreadCurrentCulture_Setzer_hat_eine_Rueckstellung` wurde rot und meldete genau
  „ZZ_KulturprobeFremd.cs (DefaultThreadCurrentCulture)“. Danach gelöscht.

### 5. Aufwand

Rund 6 Minuten (08:15–08:21), davon etwa 2 Minuten Bau und Tests.

### 6. Restpunkte

- **Alter Fallname in der Statusdatei:** `Dokumentation/aktuell/Status_iOS_Migration.md` nennt in #531 noch
  `Die_Standardkultur_en_US_steht_in_beiden_Testprojekten`. Dort steht auch Restpunkt (3) „Standardkultur und runner.json nur in Kern und UI“.
  Beides ist jetzt überholt; das nimmt der Papieragent auf. Keine Papiere geändert.
- **`CLAUDE.md`** („Bauen und prüfen“) nennt runner.json und en-US nur für `EPOS.Kern.Tests` und `EPOS.UI.Tests`. Das Nachziehen auf alle
  fünf Testprojekte übernimmt der Papieragent (`CLAUDE.md` ist hier unverändert).
- **Keine Laufzeitprobe in den drei kleinen Projekten.** Der Standardkultur-Wächter prüft die Quelle; ein Gegenstück zu
  `Ohne_Pinnung_gilt_en_US_…` (Kern) bzw. `EPOS.UI.Tests/StandardkulturTests` gibt es in den drei kleinen Projekten nicht. Das ist
  vertretbar, weil dort keine Ressourcentexte geprüft werden. Bei Bedarf wäre es ein Einzeiler je Projekt
  (`Assert.Equal("en-US", CultureInfo.CurrentCulture.Name)`).
- **Tür-Regel nicht auf die drei Projekte ausgedehnt:** Die threadgebundenen Setzer der drei Projekte (`CurrentCulture`, sechs Ziele wie UI)
  prüft der UI-Wächter nicht. Heute stellen alle sauber zurück (siehe 2). Eine Ausdehnung war nicht beauftragt.
