# Auftrag Papiere — Anwenderentscheide 24.09.2026 „E14: Empfehlung / E15: Empfehlung / E17: Empfehlung"

Anwender am 24.09.2026 (abends, ca. 21:00): „E14: Empfehlung / E15: Empfehlung / E17: Empfehlung" — alle Fragen der Familien R‑E14 (Q1…Q3),
R‑E15 (Q1…Q4) und R‑E17 (Q1…Q4) sind nach Empfehlung entschieden (jeweils Lesart a, alle bereits gebaut). Kein Code, keine Welle, keine
neue Statusnummer (Nachtrag). Worktree `.claude/worktrees/entscheide2` (Zweig `entscheide2509` ab origin f7489905; enthält 87dd9fd4 = #479).
Muster: `Entscheide_2026-09-24_Nachtrag_Auftrag.md` und Commit c3da0f44 (Merge 811d24c6).

## Wortlaut des Agentenauftrags (model: sonnet)

Papierpflege für EPOS-Plan: Nachtrag der Anwenderentscheide vom 24.09.2026 („E14: Empfehlung / E15: Empfehlung / E17: Empfehlung").
Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel. ARBEITSORT: Worktree
`C:\Waermeplan\EPOS-Plan\.claude\worktrees\entscheide2` (Zweig `entscheide2509`, HEAD f7489905); von der Repowurzel aus
`cd .claude/worktrees/entscheide2`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln: UTF-8 ohne BOM, CRLF,
byte-erhaltend (vorher/nachher Bytes und CR=LF prüfen). Python: `"C:\Program Files\Python312\python.exe"`.

MUSTER: Commit c3da0f44 („Papiere: E7c3-Q1 bis Q8, … entschieden") — `git show c3da0f44 --stat` und die Form der Zeilen im Register
(Spalte Entscheid „entschieden 24.09.2026, nach Empfehlung: a"), in den Statusdatei-Blöcken Nach #4xx (a) („Entscheid 24.09.2026: nach
Empfehlung.") und im Konzept § 7 (Offen-Absatz). Alle elf Fragen sind mit Lesart a gebaut; kein Bau offen.

AUFGABEN: (1) Register `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`: in den
Familien R‑E14 (Q1…Q3), R‑E15 (Q1…Q4) und R‑E17 (Q1…Q4) die Spalte Entscheid von „offen — Empfehlung: a" auf „entschieden 24.09.2026,
nach Empfehlung: a"; Familienüberschriften/-einleitungen und die Familienübersicht am Kopf entsprechend; bei E15‑Q4 den Vermerk lassen,
dass Lesart c (R_loss als Prozent der Differenzreihe wie Norm Anhang F Tabelle F.2) als spätere Erweiterung offen bleibt, aber nicht
beauftragt ist. Kopfzeile Datum. (2) Statusdatei `Dokumentation/aktuell/Status_iOS_Migration.md`: in den Blöcken Nach #477, Nach #478 und
Nach #479 jeweils beim Punkt (a) der Fragen einmal je Block „Entscheid 24.09.2026: nach Empfehlung (a)." ergänzen und in (g) „nächste
Schritte" die Entscheide als erledigt kennzeichnen; die Statuszeilen selbst unverändert; kein Block löschen, nichts umsortieren.
(3) Konzept `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` (gleicher Ordner wie das Register): Offen-Absatz in § 7 / Anhang-
Etappentafel („offen … E14/E15/E17" → „entschieden 24.09.2026, nach Empfehlung"), Kopfzeile Datum; § 2.11.2 falls dort ein Entscheid-
Vermerk zu V‑G7/V‑G11 steht. (4) Analysepapier (Datei per `git grep -l "E17" Dokumentation/aktuell | grep -i analyse`): § 5 Zeilen E14,
E15, E17 Entscheidstand. (5) Protokolle `Dokumentation/ueberholt/Protokolle/Reporting/E14_Formelmappe_je_Szenario_Protokoll.md`,
`E15_Risikomodul_Protokoll.md`, `E17_Nicht_monetaere_Wirkungen_Protokoll.md`: kurzer Nachsatz „Entscheide 24.09.2026: alle nach
Empfehlung (a)" am Ende. (6) Ein Commit (oder wenige), `git diff --stat`, Bericht ohne Dateiabzüge: geänderte Dateien mit Zeilen,
Bytes/CRLF-Prüfung, was nicht gefunden wurde.
