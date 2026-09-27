# Auftrag CI-Wächter — Kulturpinnung in Tests dauerhaft absichern (Vorschläge aus #515 und #525; 25.09.2026, nur Testcode, kein Schema)

Anlass: Drei rote Windows-CI-Läufe an einem Tag durch Kulturabhängigkeit von Tests (#515: GebaeudeHochrechnungTests ohne Pinnung, sechs Klassen
mit nie entsorgter Kulturvorrichtung; #525: ein Fall in PvPreisProjektTests ohne Pinnung, obwohl die Klasse sonst pinnt). Statuszeile #515 Nach (b)
empfahl (1) einen Wächter in `KulturwaechterTests`, der verlangt, dass jede Klasse mit einer `Kulturvorrichtung` als Feld `IDisposable` ist
(im Bau von #515 erprobt und grün, nicht committet), und (2) eine `xunit.runner.json` mit `parallelizeTestCollections: false` für EPOS.UI.Tests
(drei Durchgänge 6.378/6.378 grün ohne Schalter). Statuszeile #525 Nach (b) ergänzt (3) einen Wächter, der in Testklassen mit deutschen
Ressourcentexten jeden einzelnen Fall prüft. Statusnummer #527 vorgesehen (#526 Dialog Design, #522–#524 Zapfprofil; beim Push messen).
Worktree `.claude/worktrees/ciw` ab origin 1dc8d434.

## Wortlaut des Agentenauftrags (model: opus)

CI-Wächter für die Kulturpinnung in den Tests von EPOS-Plan. Antworten auf Deutsch. ARBEITSORT: Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\ciw`
(Zweig `ciw`, HEAD 1dc8d434); von der Repowurzel aus `cd .claude/worktrees/ciw`, nie im Hauptbaum, kein Zweigwechsel, kein Push, kein Merge, kein
Stash; kein Produktcode, kein Schema, Testdatenbank unverändert. `dotnet` unter `C:\Program Files\dotnet`. **Testhost-Regel:** vor jedem `dotnet test`
in eigenem Aufruf `tasklist | grep -i testhost`, bei fremdem Prozess 60 s warten. Tests immer mit `-- xUnit.ParallelizeTestCollections=false
xUnit.MaxParallelThreads=2`. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Datum mit `date` prüfen.

LESEN: `EPOS.Kern.Tests/KulturwaechterTests.cs` (bestehender Wächter und seine Probe-Datei-Meldung), `Kulturvorrichtung` (Definition), die Fälle #515
(`git show 3d703eb7 --stat`, Statuszeile #515 und Nach #515 in `Dokumentation/aktuell/Status_iOS_Migration.md`) und #525 (`git show d39847b6`),
`CLAUDE.md` Abschnitt „Bauen und prüfen" (xUnit-Schalter), `EPOS.UI.Tests/*.csproj` und `EPOS.Kern.Tests/*.csproj` (gibt es schon eine
xunit.runner.json? Wie werden Inhaltsdateien kopiert?), `.github/workflows/kern.yml` und `windows.yml` (Testaufrufe).

PHASE 0 (Befund, dann „Bau freigegeben"): (1) Wächter A: Klassen mit `Kulturvorrichtung`-Feld ohne `IDisposable`/`Dispose` — Bestand messen (Treffer
heute 0 erwartet nach #515), Regel formulieren. (2) Wächter B: Heuristik für „deutsche Ressourcentexte im Test ohne Pinnung": Vorschlag — je
[Fact]/[Theory]-Methode: enthält der Methodenrumpf ein Zeichenketten-Literal, das (a) einen Umlaut/ß oder (b) einen Wert aus `Resource.resx`
(neutral = deutsch) wortgleich enthält, dann muss entweder die Klasse eine Kulturvorrichtung im Konstruktor/Feld haben (mit IDisposable) oder die
Methode selbst `new Kulturvorrichtung()` anlegen; Bestand messen (wie viele Methoden treffen heute?), Fehlalarme prüfen (Literale, die keine
Ressourcentexte sind, z. B. Dateinamen, Kommentare) und eine Ausnahmeliste vorschlagen; Aufwand des Nachziehens der Treffer. (3) xunit.runner.json für
EPOS.UI.Tests und EPOS.Kern.Tests (`parallelizeTestCollections: false`, `maxParallelThreads: 2`) — Wirkung auf Laufzeit lokal und CI (kern.yml/windows.yml
setzen die Schalter ohnehin; Datei nimmt die Abhängigkeit von der Kommandozeile), Kopie ins Ausgabeverzeichnis (`CopyToOutputDirectory`). (4) Fragen
CIW‑Q1… mit Empfehlung; Aufwand.

PHASE 1 (nach Freigabe): CIW/1 Wächter A in KulturwaechterTests + Gegenprobe; CIW/2 Wächter B + Ausnahmeliste + Nachziehen aller Treffer (Pinnung
ergänzen, nie Assertions abschwächen); CIW/3 xunit.runner.json in beiden Testprojekten; CIW/4 voller Lauf von WP-Plan.Kern.slnf mit Schaltern UND ein
zweiter Lauf von EPOS.UI.Tests und EPOS.Kern.Tests OHNE Schalter (Nachweis der runner.json), dazu ein Lauf der Kern-Tests unter simulierter en-US-Kultur
(vorübergehender Modulinitialisierer wie in #515, nicht committen — Ergebnis: nur der erwartete Wächterfund der Probe-Datei). Papiere/Wiki nicht
ändern. Bericht: Commits, Regeln der Wächter, Treffer vorher/nachher je Datei, Testzahlen aller Läufe, Aufwand, Abnahme, Restpunkte.

## Entscheide 26.09.2026, 00:20 (nach Empfehlung des Phase-0-Berichts `Fakten_2026-09-23/ciw_bericht.md`)

- **en-US-Probelauf der Hauptsitzung (00:06–00:13, Worktree ciw auf 72212716 mit den beiden Probedateien, Schalter gesetzt):** Kern 7.619 Fälle = 7.616 grün, 1 übersprungen, 2 rot — genau die erwarteten Wächterfälle `KulturwaechterTests.Jeder_DefaultThreadCurrentCulture_Setzer_hat_eine_Rueckstellung` (Kern-Probe) und `Jede_Kulturzuweisung_in_EPOS_UI_Tests_hat_eine_Rueckstellung_oder_nutzt_die_Vorrichtung` (UI-Probe); UI 6.449 grün. **0 echte Funde heute.** Logs/TRX: Scratchpad `ciw/enus_*`.
- **CIW‑Q1 = a** (Regel A je Klasse, verschachtelte Klassen einzeln; Falle `EmissionsspalteTests.Sprachumschaltung`).
- **CIW‑Q2 = b** (en-US als feste Standardkultur von EPOS.Kern.Tests und EPOS.UI.Tests per Modulinitialisierer; Ausnahme im Kulturwächter nur für diese zwei Dateien; keine Heuristik, kein Nachziehen). Begründung: exakt statt heuristisch (Heuristik 1.510 markierte Methoden bei 0 echten Funden), jeder fehlende Pin wird lokal im Gate rot statt erst auf dem Windows-Läufer; Preis (ungepinnte Tests laufen lokal nicht mehr unter de-DE) ist gewollt.
- **CIW‑Q3 entfällt** (nur bei Q2 a/d).
- **CIW‑Q4 = a** (xunit.runner.json nur Kern und UI).
- **CIW‑Q5 = a** (Kommandozeilenschalter bleiben; CLAUDE.md-Halbsatz macht der Papieragent).
- **Restpunkt zweite Tür des UI-Wächters (je Datei):** auf „je Klasse" umstellen, wenn es im Zuge von CIW/1 klein bleibt; sonst als Restpunkt im Bericht.
- Statusnummer **#528** (beim Push messen). Bau freigegeben 00:20; Agent a2d713d4430f3791d.

- **01:55: Nummernkreuzung — BV-E3 (Anwender-Sitzung) hat #528 gepusht (origin d0e4354c) → Welle CI-Wächter = #529**, nächste freie #530; Quellkommentare im Worktree ciw auf #529 umgestellt (Commit 7b45a6c9), Commit-Betreffe bleiben „(#528 CIW/x)".

- **06:50: Zweite Nummernkreuzung — Cloud-Sitzung G7a hat #529 in der Statusdatei belegt (origin dc4a4a8f, als „##529" geschrieben), Zapfprofil hält #530 → Welle CI-Wächter = #531**, nächste freie #532. Kommentare auf pm26 mit weiterem Commit auf #531 umgestellt; Papierauftrag umbenannt `CIW_Papiere_531_Auftrag_2026-09-26.md` (darin steht #529 nur noch für G7a). Schemaschritte 146 (Namensabgleich), 147 (G6b), 148 (E47) vergeben, Testdatenbank auf origin b02fa02e (70.676.480 B, Schemastand 148); nächster freier Schemaschritt 149.
