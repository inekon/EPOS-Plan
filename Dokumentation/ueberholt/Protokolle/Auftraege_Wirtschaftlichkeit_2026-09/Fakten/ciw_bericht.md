# CI-Wächter Kulturpinnung — Phase-0-Bericht (Worktree `ciw`, Zweig `ciw`, HEAD 72212716)

Stand 26.09.2026 00:10 (`date`: Beginn 25.09.2026 23:44). Keine Commits, kein Produktcode, keine Änderung im Worktree außer den beiden
unversionierten Probedateien `EPOS.Kern.Tests/ZZ_KulturprobeEnUs.cs` und `EPOS.UI.Tests/ZZ_KulturprobeEnUs.cs`. Sie bleiben liegen, bis die
Hauptsitzung sie entfernt. Die Messung lief als statischer Leser (`mess.cs`, Ergebnisse `heute2.A.tsv`/`heute2.B.tsv`, dazu die historischen Stände
`hist_Gebaeude.*` = `3d703eb7^` und `hist_PvPreis.*` = `d39847b6^`) im Scratchpad
`C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\ciw\`.

## 1. Wächter A — Kulturvorrichtung als Feld ⇒ IDisposable

**Regel (Vorschlag, im Wortlaut):** Jede Klasse in `EPOS.Kern.Tests` und `EPOS.UI.Tests`, die ein Feld vom Typ `Kulturvorrichtung` hält, führt in
ihrer **eigenen** Basisliste `IDisposable` (oder `IAsyncDisposable`, oder erbt von `BunitContext`/`EposBunitContext`) und ruft in ihrem Rumpf
`<feld>.Dispose()` (auch `?.Dispose()`) auf. Jedes `new Kulturvorrichtung(…)` außerhalb einer Feldzuweisung steht in einer `using`-Deklaration
oder `using`-Anweisung. Ausgenommen sind die beiden `Kulturvorrichtung.cs` und `KulturwaechterTests.cs` wie bisher
(`KulturwaechterTests.cs:370–374`, `:389–393`).

**Bestand heute:** 204 Vorrichtungsfelder in 197 Dateien (Kern 194 in 187, UI 10 in 10), **0 Verstöße in 0 Dateien**. Ungebundene Vorrichtungen:
0 (245 × `using var … new Kulturvorrichtung`, 42 × `using (…)`, die übrigen sind Feldinitialisierer oder Zuweisungen im Konstruktor).

**Gegenprobe am Stand vor #515 (`3d703eb7^`):** Alle sechs Leckklassen halten `private readonly Kulturvorrichtung _kultur = new …()`
(`DwdTryLeserTests.cs:26`, `EmissionsspalteTests.cs:25`, `ErloesrubrikTests.cs:28`, `KwkgPauschaleZeileTests.cs:33`, `KwkgSatzHerkunftTests.cs:34`,
`TryPaketLeserTests.cs:28`). Keine davon führt `IDisposable` in der eigenen Basisliste, keine ruft `_kultur.Dispose()`. Die Regel findet also
**6 von 6**. Falle: `EmissionsspalteTests.cs` enthielt damals das Wort `IDisposable`, aber nur an der verschachtelten Klasse `Sprachumschaltung`
(Zeile 247). Eine **dateibezogene** Prüfung hätte diese Klasse übersehen. Die Regel muss deshalb klassenbezogen prüfen (→ CIW‑Q1).
`hist_Gebaeude`: `GebaeudeHochrechnungTests` hatte gar keine Vorrichtung. Das ist kein Fall für A, sondern für B. Die zweite Klasse der Datei,
`GebaeudeHochrechnungDatenbankTests` (`hist_Gebaeude.cs:333–344`), war schon korrekt. A beseitigt also die Reihenfolge-Abhängigkeit, die #515
verdeckt hat. Den fehlenden Pin selbst findet A nicht.

## 2. Wächter B — deutsche Ressourcentexte ⇒ Kulturpinnung

**Heuristik, wie gemessen.** Geprüft wird jede `[Fact]`/`[Theory]`-Methode. Mitgezählt werden die Literale im Rumpf und in den vorausgehenden
`[InlineData]`-Attributen. Kommentare zählen nicht, interpolierte Zeichenketten werden in ihre festen Teilstücke zerlegt. Grundlage sind die
Ressourcen `EPOS.Kern/MyResource/Resource.resx`: 11.818 Texte, davon 11.440 mit abweichendem en-US-Text.
- **U (umlaut):** Das Literal enthält ä ö ü Ä Ö Ü ß.
- **G (resxGanz):** Das Literal enthält wortgleich einen deutschen Ressourcenwert, der vom en-US-Wert abweicht (≥ 6 Zeichen, ≥ 3 Buchstaben am
  Stück; 9.887 solche Werte).
- **F (resxFrag):** Das Literal (≥ 4 Zeichen, ≥ 3 Buchstaben) steht als Teilstück in einem deutschen Ressourcenwert und in keinem en-US-Wert.
  Ohne „technische" Literale: SQL-Schlüsselwort, Bezeichner mit `_`, Präfix `Tab_`/`ID_`/`Z_`, Dateiendung. Dieser Filter senkt F nur von 641 auf
  639 Methoden.
- **Deckung:** *Feld* (Vorrichtungsfeld, entsorgt), *Erbe* (`EposBunitContext`, auch über Zwischenbasen wie `KiDialogwegBasis`), *Methode*
  (`new Kulturvorrichtung` im Rumpf), *EigenUI* (die Klasse setzt `CurrentUICulture` selbst, Rückstellung vom Kulturwächter geprüft),
  *nurCulture-Methode* (setzt nur `CurrentCulture`, die Ressourcen bleiben ungepinnt → zählt als ungedeckt), *keine*.

**Bestand heute:** 11.744 Methoden (Kern 5.970, UI 5.774).
- Kern: Feld 2.165, EigenUI 972, Methode 226, keine 2.600, nurCulture-Methode 7.
- UI: Erbe 4.729, Feld 307, EigenUI 1, keine 737.
- Ungedeckt: **Kern 2.607 Methoden in 271 Dateien, UI 737 in 65**.

Treffer unter den ungedeckten Methoden (Methoden/Dateien):

| Kriterium | Kern | UI | zusammen |
|---|---|---|---|
| U | 277/118 | 103/34 | 380/152 |
| G | 979/221 | 269/55 | 1.248/276 |
| F | 468/153 | 171/47 | 639/200 |
| U∪F | 643/182 | 234/52 | **877/234** |
| U∪G∪F | 1.191/232 | 319/58 | 1.510/290 |
| U∩G∩F | 84/54 | 36/20 | 120/74 |

**Gegenprobe historisch:**
- **#525** (`hist_PvPreis.cs`, `Szenario_C_…` ab Zeile 386): U=1, G=4, F=1. Alle drei Kriterien finden den Fall (`"Einspeisevergütung PV"`,
  Zeile 400). In derselben Datei flaggen U∪F aber 5 von 9 ungepinnten Methoden, rot war nur diese eine. Präzision **1/5**.
- **#515** (`hist_Gebaeude.cs`, `Eine_Zone_ohne_Bezugsflaeche_…` ab Zeile 205): U=0, G=0, F=2. Gefunden nur über F und **nur zufällig**: Die
  Treffer sind die Eingabenamen `"Null"`/`"Eigene"` (Zeile 210/217). Kulturabhängig ist dagegen `Assert.Contains("„Null“", …)` (Zeile 213), also
  die deutschen Anführungszeichen der Meldungsvorlage. Die im Auftrag vorgeschlagene Kriterienkombination (a) Umlaut / (b) Ganzwert hätte #515
  **nicht** gefunden. Die Zeichenklasse muss „ “ ‚ ‘ einschließen. In dieser Datei flaggen U∪F 3 von 6 ungepinnten Methoden, rot war eine.
  Präzision **1/3**. Dieselbe Klasse pinnte schon drei andere Fälle je Methode (Zeilen 250, 269, 310): dasselbe Muster wie #525. Die Prüfung je
  Methode ist also richtig.
- „ “ ‚ ‘ in Literalen (Grobzählung je Datei): Kern 185 Dateien, davon 55 ohne jede Pinnung in der Datei; UI 182, davon 40. Das ist eine
  Obergrenze, weil Teilklassen-Dateien mitzählen.

**Fehlalarme mit Beispielen (Fundstelle = Attributzeile):**
1. Rundreise: Der Test gibt deutschen Text hinein und liest ihn zurück. Beispiele: `DateiwahlTests.cs:22` „Datei wählen …", `RueckfrageTests.cs:37`
   „Nein", `KachelTests.cs:10` „Kostenprofil", `ChartRendererTests.cs:164` Reihentitel „Kostenprofil"/„Monat".
2. Katalog- und Datenbankinhalt: `WaermepumpenKatalogTests.cs:52` „Luft-Wasser"/„Außen" als Filterwerte.
3. Meldungstexte von Assertions: `hist_PvPreis.cs:275` „PV-Überschuss …", `DatenbanksicherungTests.cs:70` „Erwartet eine bestehende '-wal' …".
4. Wiki-, Dokumentations- und Quelltextleser: `BerechnungsHilfeTests.cs:764` „== Rechenweg ==", „! Größe !! Wert"; dazu die …WacheTests-Klassen.
5. SQL und Bezeichner (vor allem G: 272 × `SELECT`, 99 × `UPDATE`, 41 × `INSERT`): `BedarfProfilTests.cs:51` „Tab_Stromverbrauchertyp_STAMM".

Heute gibt es voraussichtlich **0 echte Treffer**: Windows-CI zu #525 war laut Übergabe in allen drei Läufen grün, und der en-US-Lauf aus #515 zeigte
nur den Kulturwächter-Fund. Alle 877 U∪F-Treffer sind also Fehlalarme oder latent.

**Ausnahmeliste (nur falls Heuristik, CIW‑Q2 a/d):**
(1) technische Literale wie im F-Filter;
(2) Meldungsargument von `Assert.True/False` und das letzte `string`-Argument von Assertions;
(3) Rundreise: dasselbe Literal steht im Fall mindestens zweimal;
(4) Dateiliste der Quelltext-, Wiki- und Dokumentationsleser (…WacheTests, `BerechnungsHilfeTests`, Lokalisierungswachen), je Eintrag mit Begründung
als Konstante in `KulturwaechterTests`.
Die Wirkung von (2) und (3) ist nicht gemessen.

**Aufwand Nachziehen (Variante Heuristik):** U∪F bedeutet 234 Dateien, U∪G∪F 290 Dateien. Je Klasse: Feld + `IDisposable` + `Dispose`, in UI der
Wechsel auf `EposBunitContext`. Das geht skriptgestützt, mit Sonderfällen: Klassen mit eigenem `Dispose`, Fixtures, Teilklassen. Schätzung 4–6 h
einschließlich Läufen. Die Pinnung ist schadlos, weil dieselben Tests im lokalen Gate unter de-DE grün sind.

## 3. `xunit.runner.json`

- Keines der fünf Testprojekte hat eine (0 Funde außerhalb `bin/obj/.claude`). xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Test.Sdk 17.14.1
  (`Directory.Packages.props:101–103`). Das v2-Schema kennt `parallelizeTestCollections` und `maxParallelThreads`.
- `EPOS.Kern.Tests.csproj:28–75` und `EPOS.UI.Tests.csproj:21–35` haben keine Content-/None-Einträge. Das SDK nimmt `.json` von selbst als `None`
  auf, also braucht es `<None Update="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />` (Update, nicht Include).
- `RepositoryOrdnungWacheTests.cs:103–192` prüft Sicherungskopien, Datenbankdateien, Arbeitsordner, die sqlite-Weißliste, LFS-Regeln und
  Normzahlen. Für `.json` gibt es keine Regel. `git check-ignore` meldet beide Pfade als nicht ignoriert. 27 `.json` sind schon versioniert, 4 davon
  unter `EPOS.Kern.Tests/Proben`. `.editorconfig:56–59` verlangt UTF-8 (ohne BOM) und 2 Leerzeichen Einzug.
- Testaufrufe: `kern.yml:135` (ubuntu, invariant, `--no-build`), `windows.yml:159` (windows-latest, en-US) und `gate.sh:25` setzen alle
  `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`. `gate.sh:29` (Wachen) läuft ohne Schalter.
- **Wirkung:** Die RunSettings der Kommandozeile gehen der Datei vor. Bei gleichen Werten ändern sich Gate und CI nicht (GATE520: UI 6.396 in 49 s,
  Kern 7.496 in 6 m 22 s). Seriell werden nur Aufrufe ohne Schalter: IDE, Ad-hoc-`dotnet test`, Gate-Schritt 4. Dieser Modus ist für UI heute
  ohnehin unbrauchbar (cirep #515: 19/14/19 rot von 6.378; mit runner.json dreimal 6.378/6.378 grün). Die Laufzeit parallel/seriell ist nicht
  gemessen (Laufverbot), das folgt in CIW/4.

## 4. Fragen

- **CIW‑Q1 Regel A:** (a) klassenbezogen wie in 1, mit Dispose-Ruf und `using`-Pflicht; (b) dateibezogen („Datei nennt IDisposable").
  **Empfehlung a.** b hätte `EmissionsspalteTests` vor #515 übersehen.
- **CIW‑Q2 Wächter B:**
  - (a) statische Heuristik wie in 2, mit Nachziehen;
  - (b) **en-US als feste Standardkultur beider Testprojekte** per Modulinitialisierer (die #515-Probe als Dauerform), dazu die Datei als zweite
    erlaubte Ausnahme im Kulturwächter und eine Gegenprobe; kein Nachziehen;
  - (c) de-DE als feste Standardkultur: CI wird wie das lokale Gate, fehlende Pins bleiben aber unsichtbar, und die en-US-Abdeckung des
    Windows-Läufers entfällt;
  - (d) a + b.

  **Empfehlung b.** Jeder ungepinnte kulturabhängige Fall wird dann auf jedem Rechner rot, schon im lokalen Gate und nicht erst auf dem
  Windows-Läufer. Das ist exakt statt heuristisch (historische Präzision 1/3 und 1/5, heute voraussichtlich 0 echte unter 877 Treffern), ohne
  Massenänderung. Zwingend en-US, nicht invariant: Invariant liefert die neutralen, deutschen Ressourcen und fände nichts. Deshalb ist `kern.yml`
  auf ubuntu heute blind. Preis: Ungepinnte Tests laufen lokal nicht mehr unter de-DE, sondern wie heute schon auf Windows-CI unter en-US.
- **CIW‑Q3 (nur bei Q2 a/d):** (a) U erweitert um „ “ ‚ ‘, dazu F, Ausnahmeliste (1)–(4), Nachziehen je Klasse; (b) wie im Auftrag
  (Umlaut/Ganzwert), je Methode. **Empfehlung a** (b verfehlt #515).
- **CIW‑Q4 Umfang runner.json:** (a) `EPOS.Kern.Tests` + `EPOS.UI.Tests` wie im Auftrag; (b) zusätzlich KiKern-, SpeicherEngine- und
  SpeicherPlanung.Tests; (c) nur UI. **Empfehlung a.** Die drei anderen pinnen keine Kultur und sind klein.
- **CIW‑Q5 Kommandozeilenschalter:** (a) bleiben in `gate.sh`, `kern.yml`, `windows.yml` und `CLAUDE.md:101`; die runner.json ist die Rückfallebene,
  und `CLAUDE.md:115–116` bekommt später in der Papierpflege einen Halbsatz; (b) entfernen. **Empfehlung a.**

Restpunkt ohne Frage: Die zweite Tür des UI-Wächters ist dateibezogen (`KulturwaechterTests.cs:152–153`, `Contains("Kulturvorrichtung")`). Heute
nennt keine UI-Datei die Vorrichtung nur im Kommentar (0 von 213), es besteht also kein Handlungsbedarf.

## 5. en-US-Probelauf

**Ergebnis (Lauf der Hauptsitzung, 26.09.2026 00:06–00:13, Worktree `ciw` auf 72212716 mit beiden Probedateien, Schalter gesetzt, kein fremder
testhost):** **0 echte Funde.**
- `EPOS.Kern.Tests`: 7.619 Fälle; 7.616 grün, 1 übersprungen, 2 rot, Dauer 6 m 1 s. Rot sind genau die erwarteten Wächterfälle
  `KulturwaechterTests.Jeder_DefaultThreadCurrentCulture_Setzer_hat_eine_Rueckstellung` (Kern-Probe) und
  `KulturwaechterTests.Jede_Kulturzuweisung_in_EPOS_UI_Tests_hat_eine_Rueckstellung_oder_nutzt_die_Vorrichtung` (UI-Probe).
- `EPOS.UI.Tests`: 6.449 grün, 0 rot, Dauer 46 s.

Logs und TRX liegen im Scratchpad unter `ciw/enus_EPOS.Kern.Tests.log|.trx` und `ciw/enus_EPOS.UI.Tests.log|.trx`. Mein abgebrochener Kern-Lauf zählt
nicht.

Vorfall: Meine Warteschleife startete am 25.09. um 23:59:06, zeitgleich mit dem fremden Gate in `zk` (23:59:09). Der zweite Anlauf (`lauf.sh`,
Warteschleife ab 00:02:48) ist gestoppt, der verwaiste bash-Prozess 12060 beendet. Er hat keinen Test gestartet. Der `zk`-Lauf lief bis zu etwa 2 min
parallel zu unserem. Ist er rot, lohnt eine Wiederholung.

## 6. Aufwand Phase 1

| Schritt | Inhalt | Aufwand |
|---|---|---|
| CIW/1 | Wächter A: Klassenleser, Regel, Gegenproben (Feld ohne IDisposable; IDisposable nur an verschachtelter Klasse; korrekt; ungebundenes `new`), Bestandsprobe | 1–1,5 h |
| CIW/2 (b) | Modulinitialisierer in beiden Projekten, Kulturwächter-Ausnahme, Gegenprobe (ungepinnt ⇒ en-US, Ressource liefert Englisch) | 1–1,5 h |
| CIW/2 (a) | Heuristik, Ausnahmeliste, Nachziehen von 234 Dateien | 4–6 h |
| CIW/3 | runner.json in beiden Projekten plus `None Update` | 0,5 h |
| CIW/4 | slnf mit Schaltern (~8 min), Kern+UI ohne Schalter (~8 min), en-US-Lauf (bei b = Normalfall), einschließlich Wartezeiten | 0,5–1 h |

Summe bei Q2 b rund 3,5–4,5 h, bei Q2 a rund 7–9 h. Statusnummer: Der Auftrag nennt #527, die Übergabe führt #527 als Dialog Design und den
CI-Wächter als #528. Beim Push messen.

Ich warte auf „Bau freigegeben" mit den Entscheiden.

## Phase 1

**Statusnummer: #529.** Die Berichtsvorlagen-Sitzung hat #528 gepusht. Die Commit-Betreffs dieser Welle nennen „#528“ und bleiben so stehen; die
Statuszeile vermerkt das. Die Kommentare mit „#528“ in `StandardkulturEnUs.cs`, `KulturwaechterTests.cs` und den csproj stellt die Hauptsitzung um.
Zeilenangaben beziehen sich auf den Stand a10e9b55.

Stand 26.09.2026 ~01:45, Worktree `ciw`, Zweig `ciw`, HEAD a10e9b55 (Basis 72212716). Kein Push, kein Merge, kein Stash. Der Arbeitsbaum ist sauber,
alle Probedateien sind gelöscht. Nach der Anweisung der Hauptsitzung habe ich meinen Lauf 2 (ohne Schalter) abgebrochen, bevor er startete. Ein
Log gibt es nicht; der wartende bash-Prozess ist beendet.

### 1. Commits

| SHA | Schritt | Dateien (Zeilen) |
|---|---|---|
| 3369232e | CIW/1 Wächter A, Tür je Klasse | `EPOS.Kern.Tests/KulturwaechterTests.cs` (+833/−65) |
| 73a0cacd | CIW/2 Standardkultur en-US | `EPOS.Kern.Tests/KulturwaechterTests.cs` (+101/−4), `EPOS.Kern.Tests/StandardkulturEnUs.cs` (neu, 50), `EPOS.UI.Tests/StandardkulturEnUs.cs` (neu, 38), `EPOS.UI.Tests/StandardkulturTests.cs` (neu, 34) |
| a10e9b55 | CIW/3 xunit.runner.json | `EPOS.Kern.Tests/xunit.runner.json` (neu, 5), `EPOS.UI.Tests/xunit.runner.json` (neu, 5), `EPOS.Kern.Tests/EPOS.Kern.Tests.csproj` (+7), `EPOS.UI.Tests/EPOS.UI.Tests.csproj` (+8) |

Die `.cs` sind UTF-8 mit BOM (`QuelltextKodierungWacheTests`), die `.json` UTF-8 ohne BOM. Trailer: `Co-Authored-By: Claude Opus 5.5`. Nur Testcode,
kein Produktcode, kein Schema.

### 2. Regeln im Wortlaut (`EPOS.Kern.Tests/KulturwaechterTests.cs`, a10e9b55)

- **Wächter A** `Jede_Kulturvorrichtung_wird_entsorgt` (:231, Kern `VorrichtungsFunde` :257) prüft je Klasse, verschachtelte einzeln, über alle `.cs`
  von EPOS.Kern.Tests und EPOS.UI.Tests:
  - Eine Klasse mit einem Feld vom Typ `Kulturvorrichtung` führt `IDisposable` oder `IAsyncDisposable` in der **eigenen** Basisliste oder erbt von
    `BunitContext`/`EposBunitContext` (:308).
  - Ihr Rumpf ruft `feld.Dispose()` bzw. `feld?.Dispose()` auf.
  - Jedes andere `new Kulturvorrichtung(` steht in einer Anweisung, die mit `using`/`await using` beginnt, oder ist die Zuweisung an ein
    Vorrichtungsfeld der Klasse (Konstruktorweg, :344).
  - Jede lokale Deklaration `Kulturvorrichtung x = …` steht in einem `using` (:317).
  - Meldung: `Datei:Zeile Klasse (Feld …): Grund`.
- **Tür je Klasse** (UI-Wächter `Jede_Kulturzuweisung_in_EPOS_UI_Tests_hat_eine_Rueckstellung_oder_nutzt_die_Vorrichtung` :170, Kern
  `FehlendeRueckstellungenUi` :200):
  - Eine Zuweisung an eines der sechs UI-Ziele ist befreit, wenn ihre Klasse oder eine umschließende von `EposBunitContext` erbt (auch über eine
    Zwischenbasis wie `KiDialogwegBasis`) oder ein Vorrichtungsfeld hält.
  - Teilklassen werden über Dateien zusammengeführt (`Klassenbestand` :853).
  - Sonst ist wie bisher eine Rückstellung in `Dispose()`/`finally` derselben Datei nötig.
  - Zuweisungen in Kommentaren und Zeichenketten zählen nicht.
- **Ausnahme Standardkultur** (:92, :1245): genau `EPOS.Kern.Tests/StandardkulturEnUs.cs` und `EPOS.UI.Tests/StandardkulturEnUs.cs`, nach
  repo-relativem Pfad, in beiden Dateilisten. Jeder andere Setzer bleibt verboten.
- **`Die_Standardkultur_en_US_steht_in_beiden_Testprojekten`** (:643): Beide Dateien bestehen und tragen `[ModuleInitializer]`. Im Code, nicht im
  Kommentar, setzen sie alle vier Werte: `CultureInfo.DefaultThreadCurrent(UI)Culture` und `Thread.CurrentThread.Current(UI)Culture`. Sie enthalten
  `"en-US"` und keinen anderen Kulturnamen.
- **`Ohne_Pinnung_gilt_en_US_und_die_Vorrichtung_stellt_darauf_zurueck`** (:671, Kern) und `EPOS.UI.Tests/StandardkulturTests.cs:19` (UI) prüfen je
  Assembly zur Laufzeit:
  - Ohne Pinnung gilt en-US, und `Resource.CHART_ACHSE_JAHRESSTUNDEN` = „Hours of the year [h]“.
  - Mit `using (new Kulturvorrichtung())` gilt de-DE und „Jahresstunden [h]“.
  - Danach ist wieder en-US gesetzt.
- **Standardkultur-Dateien:** `EPOS.Kern.Tests/StandardkulturEnUs.cs:39`, `EPOS.UI.Tests/StandardkulturEnUs.cs:28`. `[ModuleInitializer]` setzt die
  vier Werte auf die Konstante `Kultur = "en-US"`; CA2255 ist örtlich mit Begründung unterdrückt.
- **runner.json:** `parallelizeTestCollections: false`, `maxParallelThreads: 2`. Kopiert über `EPOS.Kern.Tests.csproj:59` und
  `EPOS.UI.Tests.csproj:42` (`None Update … PreserveNewest`). Nach dem Bau liegt sie bytegleich in `bin/Release/net10.0` beider Projekte.

### 3. Gegenproben

**Dauerhaft als Selbsttests im Wächter** (13, alle grün):
- **Leser:** `Der_Leser_erkennt_beide_Schreibweisen` :373, `Ein_Setzer_ausserhalb_von_Dispose_und_finally_zaehlt_nicht` :387,
  `Der_UI_Leser_erkennt_alle_sechs_Ziele` :436.
- **Tür:**
  - `Eine_Klasse_die_die_Vorrichtung_nutzt_braucht_keine_eigene_Rueckstellung` :454 (Erbe, Feld, Zwischenbasis mit verschachtelter Hilfsklasse)
  - `Ohne_Vorrichtung_und_ohne_Rueckstellung_faellt_eine_Datei_auf` :474
  - `Die_Tuer_des_UI_Waechters_gilt_je_Klasse_nicht_je_Datei` :489 (Setzerklasse neben bunit-Klasse; Kommentar öffnet die Tür nicht; Setzer nur in
    Kommentar oder Zeichenkette zählt nicht)
- **Wächter A:**
  - `Ein_Vorrichtungsfeld_ohne_IDisposable_faellt_auf` :535
  - `IDisposable_an_einer_verschachtelten_Klasse_zaehlt_nicht_fuer_die_aeussere` :547 (Falle `EmissionsspalteTests` vor #515)
  - `Ein_Vorrichtungsfeld_ohne_Dispose_Ruf_faellt_auf` :560
  - `Richtig_entsorgte_und_gebundene_Vorrichtungen_bleiben_still` :573
  - `Eine_Vorrichtung_ohne_using_faellt_auf` :590 (`var`, Zieltyp-Deklaration, Fabrik; genaue Meldungen mit Zeile)
- **Bestandsproben:** `Der_Waechter_sieht_den_Bestand` :418, `Der_Waechter_sieht_auch_den_UI_Bestand` :510 (Standardkultur jeweils nicht im Bestand),
  `Der_Waechter_A_sieht_die_Vorrichtungsfelder_beider_Projekte` :610 (> 150 Klassen, u. a. EmissionsspalteTests, GebaeudeHochrechnungTests,
  FenstermassTests, EposBunitContext).

**Einmalig im Bau, nicht committet:**
- **(a)** `IDisposable` von `DwdTryLeserTests` entfernt. Ergebnis rot, genau eine Meldung: „EPOS.Kern.Tests/DwdTryLeserTests.cs:26 DwdTryLeserTests
  (Feld _kultur): ohne IDisposable/IAsyncDisposable in der eigenen Basisliste“. Danach zurückgenommen.
- **(b)** `ZZ_KulturprobeFremd.cs` (Modulinitialisierer ohne Rückstellung) in beiden Projekten angelegt. Genau die zwei Kulturwächter-Fälle wurden
  rot, gemeldet nur die ZZ-Dateien, nicht die Standardkultur-Dateien (17/19). Danach gelöscht, wieder 19/19 grün.
- **(c)** Der Bestand war ohne jede Änderung an anderen Dateien grün (CIW/1: 17/17; CIW/2: 19/19 und StandardkulturTests 1/1).

### 4. Umfang von `KulturwaechterTests.cs` (1.291 Zeilen, vorher 426; +931/−66)

| Abschnitt (Zeilen) | Umfang | Art |
|---|---|---|
| Klassendoku, Konstanten (1–117) | ~117 (+70) | Beschreibung des Regelwerks; drei neue Absätze |
| Kern-Wächter (118–157) | 40 | unverändert |
| UI-Wächter und Tür je Klasse (158–217) | 60 (+15) | Regel |
| Wächter A samt Regexen (218–364) | 147 | Regel, neu |
| Selbsttests Kern (365–427) | 63 | unverändert |
| Selbsttests UI (428–528) | 100 (+40) | Selbsttests |
| Selbsttests A, Bestandsprobe (529–631) | 103 | Selbsttests, neu |
| Standardkultur-Wächter und Laufzeitprobe (632–690) | 59 | Regel und Probe, neu |
| Regionen des Kern-Wächters (691–742) | 52 | unverändert |
| **Quelltextleser (743–1209)** | **467** | **Hilfscode, neu** |
| Dateien, Arbeitsbaum (1210–1291) | 82 (+25) | Hilfscode |

246 Zeilen sind XML-Doku (vorher 116). Regel und neue Selbsttests machen rund 460 Zeilen aus. Der größte Posten ist der Quelltextleser:
- Maskierer für Kommentare, Zeichenliterale, wortgetreue, interpolierte und Roh-Zeichenketten sowie Präprozessorzeilen: ~185.
- Klassenleser (Basisliste, Rumpf, Verschachtelung): ~95.
- `Quelle` (Klammertiefe, Zeilen, Felder): ~110.
- `Klassenbestand`/`ErsteBasis` (Teilklassen, Basiskette): ~78.

Die Regeln brauchen „je Klasse“, „Feld auf Klassenebene“, „Anweisung beginnt mit using“ und „nicht im Kommentar“. Ein reiner Textgrep wie beim
alten Wächter hätte die Lücke der Falle `EmissionsspalteTests` und Fehlalarme aus Kommentaren.

**Straffen, ohne die Regeln zu schwächen:**
- **(a) Den Leser in eine eigene Datei** `EPOS.Kern.Tests/Quelltextleser.cs` ziehen. Die Gesamtzeilen bleiben, der Wächter selbst schrumpft auf rund
  820 Zeilen, und andere Quelltext-Wachen können den Leser nutzen. Klein, empfohlen.
- **(b) Den Leser durch Roslyn ersetzen.** `Microsoft.CodeAnalysis.CSharp` steht in `Directory.Packages.props`, ist aber in EPOS.Kern.Tests nicht
  referenziert. Das spart rund 350 Zeilen und arbeitet exakt über den Syntaxbaum. Es kostet eine neue Paketreferenz und die Messung der Laufzeit
  (~825 Dateien). Mittel; Anwenderentscheid.
- **(c)** Die Geschichte in der Klassendoku (#166/#167/#168) könnte ins Protokoll wandern, rund 40 Zeilen. Geringer Nutzen.

Die Selbsttests würde ich nicht kürzen: Jeder belegt eine Kante der Regeln.

### 5. Gepinnte Fälle nach der Standardkultur

**Keine.** Mein voller Lauf 1 auf a10e9b55 (mit Standardkultur en-US, siehe 6) war vollständig grün. Kein Fall wurde gepinnt, keine Assertion
geändert. Das deckt sich mit dem en-US-Probelauf der Hauptsitzung.

### 6. CIW/4 Nachweise

**Lauf 1 (von mir, 26.09.2026 00:41:06–00:47:56):** `WP-Plan.Kern.slnf -c Release --no-build` mit Schaltern auf a10e9b55, nach der Testhost-Regel
gestartet, 410 s Wanduhr.

| Projekt | Ergebnis | Dauer |
|---|---|---|
| KiKern.Tests | 549/549 grün | 330 ms |
| SpeicherEngine.Tests | 386/386 grün | 1 s |
| SpeicherPlanung.Tests | 27 grün, 1 übersprungen (von 28) | 4 s |
| EPOS.UI.Tests | 6.450/6.450 grün (darin neu StandardkulturTests) | 54 s |
| EPOS.Kern.Tests | 7.628 grün, 1 übersprungen (von 7.629; darin +10 neue Kulturwächter-Fälle) | 6 m 44 s |

- Alle 19 Kulturwächter-Fälle waren grün, namentlich:
  - Jeder_DefaultThreadCurrentCulture_Setzer_hat_eine_Rueckstellung
  - Jede_Kulturzuweisung_in_EPOS_UI_Tests_hat_eine_Rueckstellung_oder_nutzt_die_Vorrichtung
  - Jede_Kulturvorrichtung_wird_entsorgt
  - Die_Standardkultur_en_US_steht_in_beiden_Testprojekten
  - Ohne_Pinnung_gilt_en_US_und_die_Vorrichtung_stellt_darauf_zurueck
  - die 14 Selbsttests und Bestandsproben aus Abschnitt 3
- Ebenfalls grün: UI `StandardkulturTests.Ohne_Pinnung_gilt_en_US_und_die_Vorrichtung_stellt_darauf_zurueck`.
- Log und TRX liegen im Scratchpad unter `ciw/l1.log` und `ciw/lauf1/*.trx`.

**Lauf 2 ohne Schalter (EPOS.Kern.Tests, EPOS.UI.Tests) und Laufzeiten gegenüber Lauf 1:** Lauf 2 (Hauptsitzung, 26.09.2026 01:57:40–02:04:51, auf 7b45a6c9 nach Release-Build 0 Fehler, ohne fremden testhost): EPOS.Kern.Tests 7.628 grün, 1 übersprungen (6 min 14 s, gegenüber 6 min 44 s in Lauf 1); EPOS.UI.Tests 6.450/6.450 grün (49 s, gegenüber 54 s). Ohne Schalter also nicht langsamer und ohne die früheren 14–19 roten KiMaskenbrücke-Fälle (Befund B aus #515). Logs `ciw/l2_EPOS.Kern.Tests.log`, `ciw/l2_EPOS.UI.Tests.log`.

### 7. Aufwand, Abnahme, Restpunkte

**Aufwand Phase 1:** rund 1 h 45 min für Bau und Selbsttests, dazu Wartezeiten auf fremde testhosts (zv, zx, zf) und zwei Sitzungsabbrüche.

**Abnahmevorschlag:**
- (1) Lauf 2 (02:05) ist grün ohne Schalter, UI ohne die früheren 14–19 KiMaskenbrücke-Fälle.
- (2) Gate auf dem Merge: Kern und UI mit den neuen Zahlen, Bild-Hashes und Wachen unverändert.
- (3) Eine der Gegenproben (a) oder (b) aus Abschnitt 3 einmal nachstellen.
- (4) Nach dem Push zeigen Windows-CI auf `main` und ubuntu dieselben Zahlen; ubuntu läuft jetzt ebenfalls unter en-US.

**Restpunkte:**
- **Standardkultur und runner.json** gibt es nur in EPOS.Kern.Tests und EPOS.UI.Tests, nicht in KiKern.Tests, SpeicherEngine.Tests,
  SpeicherPlanung.Tests (Entscheid CIW‑Q4 a). Diese drei pinnen keine Kultur. Prüfen sie künftig Ressourcentexte, brauchen sie dieselbe Datei.
- **`CLAUDE.md`** („Bauen und prüfen“, Satz zu den xUnit-Schaltern): Der Halbsatz zu runner.json und Standardkultur en-US kommt vom Papieragenten.
- **Umstellung der Kommentare** von #528 auf #529 übernimmt die Hauptsitzung.
- **Straffung** nach Abschnitt 4 (a) oder (b); Anwenderentscheid.
- **Grenzen von Wächter A:**
  - Eine Vorrichtung als Eigenschaft oder aus einer Fabrikmethode meldet er streng als „ohne using“.
  - Ein Feld in einer Teildatei mit Konstruktorzuweisung in einer anderen Teildatei ebenso.
  - Beides kommt heute nicht vor.
- **Vorfall Parallellauf:** Der erste Probelauf der Nacht (25.09. 23:59) überlappte mit dem Gate in `zk`. Seither laufen alle meine Läufe über
  `testlauf.sh` (Testhost-Regel mit Zufallswartezeit).

## Nachtrag der Hauptsitzung (02:20)

- Lauf 2 ohne Schalter: siehe Abschnitt 6 (eingesetzt). Gegenprobe-Protokolle des Agenten liegen als `ciw/g1.log` (Wächter A rot, 1/1), `g2.log` (fremde Kultursetzer-Datei: 2 rot / 17 grün), `g3.log` (19/19 grün), `g4.log` (UI-Standardkulturtest 1/1) im Scratchpad.
- Commit 7b45a6c9 (Hauptsitzung): Statusnummer #529 statt #528 an 20 Stellen in sechs Dateien (Kommentare); Commit-Betreffe der drei Baucommits bleiben „(#528 CIW/x)".
- Merge auf pm26 = 6a91cb03 „Merge ciw … (#529)" über origin 8c0fa271 (#522–#524 Zapfprofil mit Schemaschritt 145 und Testdatenbank cba0aa41, #527 Dialog Design, #528 BV‑E3); Gate #529 gestartet 02:07.
- Restpunkt Straffung (Abschnitt 4 a/b) bleibt Anwenderentscheid, nicht Teil dieser Welle.
