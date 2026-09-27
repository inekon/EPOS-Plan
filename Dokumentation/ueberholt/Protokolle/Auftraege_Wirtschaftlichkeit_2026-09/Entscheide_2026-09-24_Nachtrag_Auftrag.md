# Auftrag Papiere — Anwenderentscheide 24.09.2026 „Offene Entscheide: Empfehlung" (E7c3, E8c, E9a, E9b, E10, E12)

Anwender am 24.09.2026, 16:30: „Offene Entscheide: Empfehlung" — alle offenen Fragen der Familien R‑E7c3 (Q1…Q8), R‑E8c (Q1/Q2), R‑E9a
(Q1…Q7), R‑E9b (Q1…Q5), R‑E10 (Q1…Q4, Q6, Q7; Q5 erledigt) und E12‑Q1…Q4 sind nach Empfehlung entschieden. Kein Code, keine Welle, keine
neue Statusnummer (Nachtrag). Worktree `.claude/worktrees/entscheide` (Zweig `entscheide2409` ab origin 65cfa844).

## Wortlaut des Agentenauftrags (model: sonnet)

Papierpflege für EPOS-Plan: Nachtrag der Anwenderentscheide vom 24.09.2026 („Offene Entscheide: Empfehlung"). Antworten auf Deutsch. Nur
Papiere, kein Build, kein Test, kein Zweigwechsel. ARBEITSORT: Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\entscheide` (Zweig
`entscheide2409`, HEAD 65cfa844); von der Repowurzel aus `cd .claude/worktrees/entscheide`, nie im Hauptbaum. Commits sofort mit
`git add <pfad>`, Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln: UTF-8
ohne BOM, CRLF, byte-erhaltend (vorher/nachher Bytes und CR=LF prüfen); Mockup `<tr` = `</tr>`.

MUSTER: der Nachtrag der E8b-Entscheide vom 23.09.2026 — Commit d978d5c8 („Papiere: E8b-Q1 bis Q6 entschieden") und die Zeilen im Register
R‑E8b (Spalte Entscheid „entschieden 23.09.2026, nach Empfehlung: a"), im Statusdatei-Block Nach #455 (a) („Entscheid 23.09.2026: nach
Empfehlung.") und im Konzept § 7 (Offen-Absatz). `git show d978d5c8` zeigt die Form.

AUFGABEN: (1) Register `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`: in den
Familien R‑E7c3 (Q1…Q8), R‑E8c (Q1/Q2), R‑E9a (Q1…Q7), R‑E9b (Q1…Q5), R‑E10 (Q1…Q4, Q6, Q7) die Spalte Entscheid von „offen — Empfehlung: x"
auf „entschieden 24.09.2026, nach Empfehlung: x" (x = die jeweils empfohlene Lesart; bei E9b‑Q5 = b, bei E7c3‑Q6 = a mit Zusatz „Bau in
einer Folgewelle") — die Familienüberschriften/-einleitungen und die Familienübersicht am Kopf (Zeile ~54 ff.) entsprechend („entschieden
24.09.2026, nach Empfehlung"); E12‑Q1…Q4 (falls im Register, sonst nur Statusdatei): Q1 a = 26.09.2026, Q2 a, Q3 a (1.2.0.4), Q4 a.
Zwei Fragen verlangen noch Bau, das im Entscheid vermerken: E9b‑Q5 b (Checkliste Punkt 9 „erfüllt, sobald beide Szenarien gerechnet" —
gebaut ist a) und E7c3‑Q6 a (Ladefehler/Speicherfehler/Vorsorgewarnung in der Oberfläche) — beide „Bau offen, eigener kleiner Auftrag".
(2) Statusdatei `Dokumentation/aktuell/Status_iOS_Migration.md`: in den Blöcken Nach #452, Nach #460, Nach #461, Nach #462, Nach #463 und
Nach #470 jeweils beim Punkt (a) der Fragen hinter jeder Frage bzw. einmal je Block „Entscheid 24.09.2026: nach Empfehlung" ergänzen (bei
E9b‑Q5 „: b", bei E7c3‑Q6 „: a, Bau offen"), die Statuszeilen selbst unverändert; kein Block löschen. (3) Konzept
`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`: Offen-Absatz in § 7 / Anhang-Etappentafel („offen die … Fragen aus E7c3/E8c/E9a/
E9b/E10" → „entschieden 24.09.2026, nach Empfehlung; Bau offen: E9b‑Q5 b, E7c3‑Q6 a"), Kopfzeile Datum. (4) Analysepapier § 5 (Zeilen E7,
E9, E10, E12: Entscheidstand) und `Wiki_Update_2026-09-26.md` (Termin 26.09.2026 bestätigt, Version 1.2.0.4 bestätigt — Upload nach
Freigabe). (5) Protokoll: kurzer Nachsatz „Entscheide 24.09.2026: alle nach Empfehlung" am Ende der Protokolle E7c3, E8c, E9a, E9b, E10
unter `Dokumentation/ueberholt/Protokolle/Reporting/` (Dateinamen per `ls | grep -i "E7c3\|E8c\|E9a\|E9b\|E10"`). (6) Ein Commit
(oder wenige), `git diff --stat`, Bericht ohne Dateiabzüge: geänderte Dateien mit Zeilen, Bytes/CRLF-Prüfung, was nicht gefunden wurde.
