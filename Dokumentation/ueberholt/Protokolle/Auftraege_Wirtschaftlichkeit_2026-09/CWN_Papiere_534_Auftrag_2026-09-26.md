# Auftrag Papiere #534 — Nachlese CI-Wächter: Quelltextleser in eigener Datei, Standardkultur en-US in allen fünf Testprojekten (nur Testcode, 26.09.2026)

Stand: pm26 = Merge cwn (Teil 1 db0db625, Teil 2 f3cbda17) über origin 64b18fbb (#533 Zapfprofil ZU21, #530 R20, #532 BV‑E4, G6b W4), danach Kommentar-Commit
„#534 statt #533". Fakten: `Fakten_2026-09-23/cwn_bericht.md` (Teil 1 und Teil 2), `Entscheide_2026-09-26_Restpunkte.md` (Anwenderentscheide 08:25).
Nummernkreuzung: Welle lief als #533 (Commit-Betreffe „CWN #533: …"), Zapfprofil pushte #533 zuerst → Statusnummer #534; Kommentare tragen #534.

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #534 für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge, kein Stash.
ARBEITSORT: Worktree `.claude/worktrees/papiere533` (Zweig `papiere533`, Stand f3cbda17 = Merge cwn Teil 2 auf pm26); von der Repowurzel `C:\Waermeplan\EPOS-Plan`
aus `cd .claude/worktrees/papiere533`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
Formregeln wie #531/#525; Edit-Werkzeug, Zeilenenden erhalten (Statusdatei CRLF, CLAUDE.md prüfen); Datum mit `date` prüfen.

FAKTEN: dieser Auftrag; `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\cwn_bericht.md` (Commits 6e01aed3 Teil 1: Quelltextleser.cs 511 Zeilen, KulturwaechterTests
1.291 → 810 Zeilen, 19/19 grün, Gegenprobe wortgleich; 64f470cc Teil 2: StandardkulturEnUs.cs + xunit.runner.json + csproj-Eintrag in KiKern.Tests, SpeicherEngine.Tests,
SpeicherPlanung.Tests, Kulturwächter-Fall umbenannt `Die_Standardkultur_en_US_steht_in_allen_Testprojekten`, Setzer-Wächter über alle fünf Projekte, Bestand fremder Setzer 0,
keine Pinnung nötig, Zahlen KiKern 549 / SpeicherEngine 386 / SpeicherPlanung 27+1 mit und ohne Schalter gleich, Gegenproben rot wie erwartet); Statuszeile #531 und Nach #531
als Bezug; `Entscheide_2026-09-26_Restpunkte.md` (Nr. 22 „ok", Straffung a, Standardkultur jetzt vorsorgen; E28/E29/Nr. 21 laufen getrennt — hier nur nennen).

AUFGABEN: (1) Statusdatei `Dokumentation/aktuell/Status_iOS_Migration.md`: Zeile #534 nach #533 vor `---` (Anlass: Anwenderentscheide 26.09.2026 zu den Restpunkten
aus #531; Änderung Teil 1 und Teil 2 mit Zahlen; Nummernkreuzung; Stand auf pm26 über origin 64b18fbb; **Gate:** NACHTRAG-534-GATE; **CI:** NACHTRAG-534-CI; kein Logbuchsatz,
kein Protokoll wie #531) und Block Nach #534 vor Nach #533 (falls es einen gibt, sonst vor Nach #532): (a) was verschoben wurde und warum (Empfehlung a aus #531), (b) die
fünf Testprojekte unter en-US, Regel für Nachbarn, (c) Restpunkte aus dem Bericht (Leser auf Kulturvorrichtung zugeschnitten; keine Laufzeitprobe in den kleinen Projekten;
UI-Wächter nicht ausgedehnt; Roslyn-Variante b nicht gewählt), (d) Nachweis-Platzhalter. (2) Nach #531 (e): die Restpunkte „Straffung des Quelltextlesers" und „Standardkultur
nur Kern und UI" additiv mit „→ erledigt in #534" versehen; den Fallnamen `Die_Standardkultur_en_US_steht_in_beiden_Testprojekten` in Nach #531 (b) um „(seit #534
`…_in_allen_Testprojekten`)" ergänzen. (3) `CLAUDE.md`, „Bauen und prüfen": die beiden Punkte aus #531 so fassen, dass runner.json und Standardkultur en-US für alle fünf
Testprojekte gelten (seit #534); Bytes/Zeilenenden erhalten. (4) Konzept `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`
§ 6.3 Nr. 22 (Sichtabnahmen B2, BK1, B4) als erledigt kennzeichnen (Durchstreichung wie die Nachbarn, „— abgenommen vom Anwender 26.09.2026 (→ Register R‑Rest)") und im
Entscheidungsregister `Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`, Familie R‑Rest, eine Zeile für Nr. 22 (Entscheid „ok", 26.09.2026) ergänzen; Nr. 21 dort als
„Sichtprüfung läuft (P1030)" vermerken, ohne den Konzeptpunkt zu schließen. (5) Nichts sonst. (6) Bericht: Commit-SHAs, `git diff --stat` gegen f3cbda17, Zeilennummern,
verbliebene Platzhalter, keine Dateiabzüge.
