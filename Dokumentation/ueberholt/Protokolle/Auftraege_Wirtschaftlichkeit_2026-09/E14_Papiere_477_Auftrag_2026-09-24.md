# Auftrag Papiere #477 — E14: Formelmappe je Szenario (Stufen 1 und 2 für Günstig und Ungünstig) (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen und CI-Nachweise nennt die Startnachricht (Platzhalter NACHTRAG-477-MERGE / NACHTRAG-477-GATE / NACHTRAG-477-CI).
Muster: `E13_Papiere_474_Auftrag_2026-09-24.md`, Statuszeile #474 und Block Nach #474. Vor dem Schreiben `grep -n "#47[0-9]"` in der
Statusdatei (#476 Dialog Design, AK1-Zeilen des Anwenders; Schemastand 124; Basis R14_Kaelteerzeuger).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #477 (E14 — die Excel-Formelmappe rechnet alle drei Szenarien formelbasiert) für EPOS-Plan. Antworten auf
Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel. ARBEITSORT: Worktree `.claude/worktrees/papiere477` (Zweig `papiere477`
ab NACHTRAG-477-MERGE); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere477`, nie im Hauptbaum. Commits sofort
mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie
#474 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex 0 Treffer; Logbuch Version 1.2.0.4, Wiki `bericht`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e14_berichte.md` (Phase 1: Commits, Bau, Schlüssel, Abweichungen 1–5, Fragen
E14‑Q1…Q3, erledigt-Gründe, Logbuchsatz, Abnahme A‑E14‑1), Phase 2 laut Startnachricht (Merge f6727fdd mit 61efa054, Tests 12.967/0/1,
Referenzlauf 13/13 R14 394 CSV byte-gleich, Zellvergleich 16 Prüfgruppen inkl. neuer Gruppe hybzeit mit Formelzahlen vorher → nachher —
synth 256 → 895, 1019/1023/1024 320 → 1.039, 1030 125 → 373, prep1030 134 → 382, hyb1040 427 → 1.535, hyb1042 429 → 1.541, hybbk 449 →
1.597, hybleer 429 → 1.541, hybtest 448 → 1.596, hybarten 465 → 1.617, hybluecke 448 → 1.596, hybzeit 429 → 1.766; 1018/1031 10, 1026/…
29, 1046 0 unverändert — Excel 16 abweichend 0, ClosedXML abweichend 0 (Fehlerwerte nur NPV/IRR wie bekannt), OpenXML 0 Fehler; je Gruppe
genau drei gewollte Textzellen geändert: Parameterblock Zeile 5 „Betrachtungszeitraum T_s je Szenario [a]", Hinweis Zeile 15, Checkliste
Punkt 11; Wortbericht-Gliederung unverändert; Befund 3 Zinsfuß-Text: keine Prüfgruppe betroffen), `e8b_berichte.md` und `e8_fakten.md`
(§ 2.11.6 Stufenplan, „was dauerhaft Werte bleibt", E8b‑Q1), `e9a_berichte.md` (Befund 1). Alles ganz lesen. Regelverstoß des Bauagenten
(Phase 2): ein voller Lauf startete um 18:39:23 für Sekunden parallel zu z4-Testprozessen und wurde abgebrochen — als Befund in Nach #477
(d) nennen (Zapfprofil informiert).

AUFGABEN: (1) Statusdatei: Zeile #477 (Anlass: Anwender 24.09.2026 „Formelmappe und Befund aus E9a: Auftrag") nach der letzten Zeile
vor `---` und Block Nach #477 als neuester Block: (a) Fragen E14‑Q1…Q3 (offen, gebaut a; Q2 ersetzt E8b‑Q1 a), (b) Abnahme A‑E14‑1, (c)
Nachweis (kein Kernwert ändert sich: Anker, Referenzlauf; Zellvergleich-Tafel mit Formelzahlen), (d) Befunde (Zinsfuß-Text auch bei
Erwartet; Designer-Lücke `ZPG_EINGABE_STOCHASTIK_EINHEITSTAGE` fremd; Menge × Preis nicht zerlegt, Stufe 3 bleibt Erwartet; Testregel-
Verstoß), (e) Papiernachzug, (f) Logbuch, (g) nächste Schritte (E15/E17 laufen, E16 nach E15; Wiki-Upload 26.09.), (h) Nachweis: Gate
NACHTRAG-477-GATE, CI NACHTRAG-477-CI. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E14_Formelmappe_je_Szenario_Protokoll.md`
(Muster E8b/E13), Index Reporting +1. (3) Register: E8b‑Q1 „abgelöst durch E14‑Q2 a (#477)", neue Familie R‑E14 mit Q1…Q3 (offen, Empfehlung
a), E9a-Befund 1 erledigt; Konzept § 2.11.6 (Stufenplan: alle drei Szenarien formelbasiert; „was dauerhaft Werte bleibt" ohne die
Kennzahlen Günstig/Ungünstig; Regel EPOS trägt Werte ein, Excel rechnet neu bleibt), § 2.11.4 V‑D „ergänzt #477", § 7/Anhang, Kopfzeile
Codestand; Analysepapier § 5 (E8 Teil b ergänzt #477); Entscheidwege-Protokoll. (4) Mockup: U43 Punkt 11, Zone „Bericht und Ausgabe"
(Formelmappe je Szenario), Ressourcentafel (+5, 3 gefasst), Stand-Absatz. (5) Logbuch (#477, ein Satz), Wiki-Quelle Wirtschaftlichkeit
(Abschnitt Formelmappe: drei Szenarien, Schutzformeln, Zinsfuß-Text), `Wiki_Update_2026-09-26.md`. (6) Bytes, `<tr`, `git diff --stat`,
Bericht ohne Dateiabzüge.
