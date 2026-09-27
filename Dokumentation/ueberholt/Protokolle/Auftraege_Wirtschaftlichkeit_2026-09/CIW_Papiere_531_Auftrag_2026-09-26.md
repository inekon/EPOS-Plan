# Auftrag Papiere #531 — CI-Wächter: Kulturpinnung in den Tests dauerhaft abgesichert (nur Testcode, 26.09.2026)

Stand: pm26 = 6a91cb03 (Merge des Worktrees `ciw` „(#531)" auf pm26 über origin d0e4354c = #528 BV-E3 Berichtsvorlagen, #523 Zapfprofil, #522 Zapfprofil mit Schemaschritt 145 und Testdatenbank LFS cba0aa41 [70.590.464 B], #527 Dialog Design). Nummernkreuzung: die Welle lief als #528 (Auftrag, Commit-Betreffe 3369232e/73a0cacd/a10e9b55), BV-E3 hat #528 zuerst gepusht → #529 (Kommentar-Commit 7b45a6c9); dann belegte die Cloud-Sitzung G7a #529 (origin dc4a4a8f, dort „##529") und Zapfprofil #530 → Statusnummer #531 (Kommentar-Commit e1e321c1 auf pm26); die Kommentare in den Quelldateien tragen #531 (Folgecommit 7b45a6c9), die Commit-Betreffe bleiben — in der Statuszeile vermerken.. Muster: Statuszeilen #515 und #525
samt Nach-Blöcken (CI-Reparaturen Kultur). Zusatz: CI-Vermerk des Pushs 1dc8d434 (#525) nachtragen; Vorschläge aus Nach #515 (b) und Nach #525 (b)
als umgesetzt kennzeichnen; CLAUDE.md „Bauen und prüfen" um runner.json und Standardkultur ergänzen.

## FAKTEN-BAU (aus `Fakten_2026-09-23/ciw_bericht.md`, Abschnitt Phase 1)

Siehe `ciw_bericht.md`, Abschnitt „Phase 1": Commits 3369232e (CIW/1 Wächter A und UI-Tür je Klasse, `KulturwaechterTests.cs` 1.291 Zeilen mit Quelltextleser und 14 Selbstprüfungen), 73a0cacd (CIW/2 `StandardkulturEnUs.cs` in beiden Projekten, Ausnahme und zwei neue Wächterfälle, `EPOS.UI.Tests/StandardkulturTests.cs`), a10e9b55 (CIW/3 `xunit.runner.json` + csproj `None/PreserveNewest`), 7b45a6c9 (Nummer #531 in Kommentaren). Gepinnte Fälle: keine. Gegenproben g1–g4 (Wächter A rot bei entferntem IDisposable; fremde Kultursetzer-Datei: beide Wächter rot; Kulturwächter 19/19; UI-Standardkulturtest 1/1). Läufe: slnf mit Schaltern (00:40–00:47) KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1, UI 6.450 (54 s), Kern 7.628+1 (6:44) alles grün; Kern und UI OHNE Schalter (01:57–02:05) Kern 7.628+1 grün in 6:14, UI 6.450 grün in 49 s (Nachweis runner.json; früher ohne Schalter 14–19 UI-Fälle rot). Restpunkte: keine; die drei übrigen Testprojekte laufen weiter unter der Rechnerkultur (CIW‑Q4 a).

## FAKTEN-PHASE-0 (aus `ciw_bericht.md`, Phase 0, 25./26.09.2026)

- Anlass: drei rote Windows-CI-Läufe an einem Tag durch Kulturabhängigkeit von Tests (#515: `GebaeudeHochrechnungTests` ohne Pinnung + sechs Klassen mit nie
  entsorgter `Kulturvorrichtung`; #525: ein Fall in `PvPreisProjektTests` ohne Pinnung). Der Windows-Läufer läuft unter en-US, der ubuntu-Läufer (kern.yml) unter
  der invarianten Kultur, die die neutralen deutschen Ressourcen liefert und solche Fehler nie sieht; lokal lief alles unter de-DE.
- Wächter A (Regel je Klasse, auch verschachtelte Klassen): Bestand 204 `Kulturvorrichtung`-Felder in 197 Dateien (Kern 194/187, UI 10/10), heute 0 Verstöße;
  Gegenprobe am Stand `3d703eb7^`: die Regel findet alle sechs Leckklassen von #515 (`DwdTryLeserTests`, `EmissionsspalteTests`, `ErloesrubrikTests`,
  `KwkgPauschaleZeileTests`, `KwkgSatzHerkunftTests`, `TryPaketLeserTests`); Falle: in `EmissionsspalteTests` stand `IDisposable` damals nur an der
  verschachtelten Klasse `Sprachumschaltung` — eine Prüfung je Datei hätte sie übersehen.
- Wächter B als Heuristik verworfen (CIW‑Q2 = b): 11.744 Testmethoden, ohne Pinnung Kern 2.607 in 271 Dateien und UI 737 in 65; die Kriterien Umlaut/ganzer
  Ressourcentext/Teilstück markieren 1.510 Methoden in 290 Dateien, Fehlalarme aus Eingabe-Rücklese-Texten, Katalog-/Datenbankinhalt, Assertion-Meldungen,
  Wiki-/Quelltextlesern und SQL; #515 hätte die Heuristik nur zufällig getroffen (kulturabhängig war `Assert.Contains("„Null“", …)` mit deutschen
  Anführungszeichen). Stattdessen en-US als feste Standardkultur beider Testprojekte: exakt statt heuristisch, jeder fehlende Pin wird lokal im Gate rot;
  Preis: ungepinnte Tests laufen lokal nicht mehr unter de-DE (gewollt).
- en-US-Probelauf vor dem Bau (00:06–00:13, Worktree ciw auf 72212716, Schalter gesetzt): Kern 7.619 = 7.616 grün, 1 übersprungen, 2 rot (nur die beiden
  Wächterfälle der vorübergehenden Probedateien), UI 6.449 grün → 0 echte Funde.
- xunit.runner.json: bisher in keinem Testprojekt; xunit 2.9.3; Gate (`gate.sh`), `kern.yml` und `windows.yml` setzen dieselben Schalter weiter (Kommandozeile
  geht der Datei vor); die Datei macht IDE- und Ad-hoc-Aufrufe ohne Schalter reihenfest.
- Entscheide 26.09.2026 00:20: CIW‑Q1 a (je Klasse), Q2 b (Standardkultur en-US), Q3 entfällt, Q4 a (runner.json nur Kern und UI), Q5 a (Schalter bleiben).
- CI-Nachweis für #525 (Push 1dc8d434): alle drei Läufe grün — Windows `main` 36188363256, Kern `main` 36188363189, Kern `ios_migration_september` 36188358761
  (erster vollständig grüner CI-Stand seit #513).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #531 (CI-Wächter: Kulturpinnung in den Tests dauerhaft abgesichert; nur Testcode, kein Produktcode, kein Schemaschritt,
Testdatenbank durch #531 unverändert — Stand nach dem Merge ist die von #522 gelieferte Datenbank cba0aa41 mit Schemastand 145) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge,
kein Stash. ARBEITSORT: Worktree `.claude/worktrees/papiere529` (Zweig `papiere529` ab 6a91cb03); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus
`cd .claude/worktrees/papiere529`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
Formregeln wie #515/#525; Änderungen mit dem Edit-Werkzeug, Zeilenenden erhalten (Statusdatei CRLF, CLAUDE.md prüfen); Datum mit `date` prüfen (26.09.2026, nachts).

FAKTEN: dieser Auftrag (Abschnitte FAKTEN-BAU und FAKTEN-PHASE-0); `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\ciw_bericht.md` (Phase 0 und Phase 1);
`git log --oneline d0e4354c..HEAD` und `git show <sha> --stat` der Commits; Statuszeilen #515 und #525 mit ihren Nach-Blöcken als Muster und Bezug.

AUFGABEN: (1) Statusdatei `Dokumentation/aktuell/Status_iOS_Migration.md`: Zeile #531 nach #528 (BV-E3) vor `---` (Anlass: drei rote Windows-Läufe, Vorschläge aus
Nach #515 (b) und Nach #525 (b); Änderung in drei Teilen: Wächter A je Klasse, Standardkultur en-US per Modulinitialisierer mit Ausnahme und Wächterfall im
Kulturwächter, `xunit.runner.json` in Kern- und UI-Tests; Nachweis: Testzahlen aus FAKTEN-BAU, Nachweislauf ohne Schalter; Stand 6a91cb03 auf pm26 über
origin d0e4354c (#528 BV-E3, #523, #522 mit Schemaschritt 145 und Testdatenbank cba0aa41 — von #531 nicht berührt); **Gate:** Platzhalter NACHTRAG-531-GATE; **CI:** NACHTRAG-531-CI; kein Logbuchsatz und kein Protokoll, Begründung wie #515: reiner
Testcode) und Block Nach #531 vor Nach #528: (a) Anlass und Entscheid gegen die Heuristik (Zahlen aus FAKTEN-PHASE-0), (b) Regeln der beiden Wächter und der
Standardkultur im Wortlaut, mit Fundstellen (Datei:Zeile im Stand 6a91cb03), (c) Folgen für alle Sitzungen: Tests mit deutschen Texten oder Zahlformaten
brauchen die `Kulturvorrichtung` (Feld mit `IDisposable` oder `using var` im Fall), sonst rot wie auf dem Windows-Läufer; Läufe ohne Schalter sind jetzt
reihenfest, die Schalter bleiben in Gate und Workflows, (d) Gegenproben (Wächter A rot bei entferntem `IDisposable`, fremde Kultursetzer-Datei weiter rot),
(e) Restpunkte aus dem Baubericht, (f) Nachweis Gate/CI-Platzhalter. (2) In Nach #515 (b) hinter den Vorschlägen (1) und (2) und in Nach #525 (b) hinter der
Empfehlung je einen additiven Vermerk „→ umgesetzt in #531 (…)" (ein Halbsatz, nichts löschen). (3) In der Statuszeile #525 und in Nach #525 (c) den
CI-Vermerk „steht aus (Beobachtung nach dem Push — der Windows-Lauf auf `main` ist der Nachweis)" durch den Text aus FAKTEN (alle drei Läufe grün mit
Nummern) ersetzen. (4) `CLAUDE.md`, Abschnitt „Bauen und prüfen", Aufzählungspunkt „Testsammlungen laufen **nicht parallel** …": um einen Halbsatz ergänzen,
dass `EPOS.Kern.Tests` und `EPOS.UI.Tests` dieselben Werte seit #531 als `xunit.runner.json` tragen (Läufe ohne Schalter reihenfest, Schalter bleiben), und
einen neuen Punkt direkt darunter: beide Testprojekte laufen seit #531 unter der Standardkultur en-US (wie der Windows-Läufer); Tests mit deutschen
Ressourcentexten oder Zahlformaten pinnen de-DE mit der `Kulturvorrichtung`, sonst sind sie rot — kein Lauf unter de-DE ist mehr ein Nachweis. Kurz, im
Ton der Nachbarpunkte, Bytes/Zeilenenden der Datei erhalten. (5) Nichts sonst (kein Wiki, kein Konzept, kein Protokoll). (6) Bericht: Commit-SHA,
`git diff --stat` gegen 6a91cb03, Zeilennummern, verbliebene Platzhalter, keine Dateiabzüge.
