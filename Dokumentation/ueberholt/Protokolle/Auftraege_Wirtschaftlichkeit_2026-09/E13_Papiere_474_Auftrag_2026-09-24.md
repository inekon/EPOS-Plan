# Auftrag Papiere #474 — E13: Checkliste Punkt 9 (E9b‑Q5 b), Fehlergründe in der Oberfläche (E7c3‑Q6 a), A8-Halbsatz, zwei Hilfe-Anker; Nutzungsdauer-Konzept nach `ueberholt/` (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen und CI-Nachweise nennt die Startnachricht (Platzhalter NACHTRAG-474-MERGE / NACHTRAG-474-GATE / NACHTRAG-474-CI).
Muster: `E10_Papiere_463_Auftrag_2026-09-24.md`, Statuszeile #470 und Block Nach #470. Vor dem Schreiben `grep -n "#47[0-9]"` in der
Statusdatei (#471 Anwender, #472/#473 Dialog Design gepusht, #475 Dialog Design läuft; Schemastand 123 durch Anlagenkopplung AK1 des
Anwenders; Referenzbasis `2026-09-24_R14_Kaelteerzeuger`).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #474 (kleine Bauwelle E13 nach den Anwenderentscheiden vom 24.09.2026) für EPOS-Plan. Antworten auf
Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel. ARBEITSORT: Worktree `.claude/worktrees/papiere474` (Zweig `papiere474` ab
NACHTRAG-474-MERGE); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere474`, nie im Hauptbaum. Commits sofort mit
`git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie
#470 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version 1.2.0.4,
Wiki-Stichwörter `bericht`, `wirtschaftlichkeit`, `kosten`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e13_berichte.md` (Phase 1: Commits, Bau je Punkt, Schlüssel, Abweichungen,
Befund iOS-Weg, erledigt-Gründe, Logbuchsätze, Abnahme A‑E13‑1; Phase 2 laut Startnachricht), `E13_Auftrag_2026-09-24.md`, Register
R‑E9b (Q5), R‑E7c3 (Q6), R‑A (A8), `e12_fakten.md`/`e12_berichte`-Angaben zu den vier Anker-Kandidaten (Nach #470 (d)). Alles ganz lesen.

AUFGABEN: (1) Statusdatei: Zeile #474 (Anlass: Anwender-Freigabe 24.09.2026 „kleine Bauwelle für E9b‑Q5 (b) und E7c3‑Q6 (a)", erweitert
um A8-Halbsatz und Anker) nach der letzten Zeile vor `---` und Block Nach #474 als neuester Block: (a) keine offenen Fragen (alle
Entscheide gefallen), (b) Abnahme A‑E13‑1 (fünf Schritte aus dem Bericht), (c) Nachweis „keine Rechenwirkung" (Anker, Referenzlauf 13/13
gegen R14), Maskenwache, (d) Befunde (iOS-Weg über die Projektliste reicht die Gründe nicht durch — eigener Auftrag; zwei Anker bleiben
Seitenebene mit Begründung; Rücksetzen der Vorsorgewarnung), (e) Papiernachzug inkl. Verschiebung des Nutzungsdauer-Konzepts, (f) Logbuch,
(g) nächste Schritte: Wiki-Sammel-Upload 26.09.2026 (Version 1.2.0.4, freigegeben), Datenpflege 1030/1026 offen, iOS-Weg BHKW-Gründe,
Linux-Messlatte, (h) Nachweis: Gate NACHTRAG-474-GATE; CI-Nachträge: #470 → 4c3792e0 ios Kern 35983410846, main Kern 35983493108,
main Windows 35983493112 grün (in Nach #470 (h)); NACHTRAG-474-CI. (2) Protokoll
`Dokumentation/ueberholt/Protokolle/Reporting/E13_Checkliste_Fehlergruende_Protokoll.md` (Muster E10, kurz), Index Reporting +1.
(3) Register: E9b‑Q5 „gebaut b #474", E7c3‑Q6 „gebaut a #474", A8 „Halbsatz erledigt #474 (nur Vorgabe neuer Einträge)", ggf. V‑G12
Checkliste Punkt 9; Konzept § 2.11.2 (V‑G12), § 2.11.x Checkliste (Punkt 9 Regel erfüllt/teilweise/offen), § 7/Anhang (E13 gebaut #474;
alle Fragen entschieden), Kopfzeile Codestand; Analysepapier § 5 (E7c3‑Q6 und E9b‑Q5 gebaut #474), § 3.1 B‑6 (Gründe in der Oberfläche);
Entscheidwege-Protokoll. (4) **Nutzungsdauer-Konzept** `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md`
nach `Dokumentation/ueberholt/` verschieben (Regel Statusdatei Nach #264: „Konzept wandert nach ueberholt/, sobald S3 abgeschlossen ist";
E10-Entscheide sind gefallen): `git mv`, Kopfzeile „überholt seit #474, S1–S3 gebaut (#269, #357, #463), A8-Halbsatz #474", alle Verweise
nachziehen (laut Papieragent #463: sechs Verweise in Papieren und zwei Pfade in Code-Kommentaren — per `grep -rn "Konzept_Nutzungsdauer_AfA"
--include=*.md --include=*.cs --include=*.html .` finden; Code-Kommentare nur den Pfad ändern), `Dokumentation/LIESMICH.md`-Index anpassen.
Link-Wache muss grün bleiben (nur prüfen per grep, kein Build). (5) Mockup: U43 (Punkt 9 „erfüllt" #474), U10 (Statuszeile nennt Lade-,
Speicher- und Vorsorgegründe #474 — Inhalt von U10 gegenprüfen), Ressourcentafel (+5, 2 gefasst), Stand-Absatz. (6) Logbuch (#474, die zwei
Sätze plus der optionale `kosten`-Satz), Wiki-Quellen Wirtschaftlichkeit (Statuszeile/Warnband) und Kosten (Speichervariante nimmt die
Nutzungsdauer der Tabelle; Zeileneditor-Hilfe), `Wiki_Update_2026-09-26.md` ergänzen (Seitenliste/Sätze); Tabu-Regex 0. (7) Bytes, `<tr`,
`git diff --stat`, Bericht ohne Dateiabzüge.
