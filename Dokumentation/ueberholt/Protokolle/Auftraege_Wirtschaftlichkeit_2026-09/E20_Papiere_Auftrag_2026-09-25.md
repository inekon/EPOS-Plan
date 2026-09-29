# Auftrag Papiere E20 — Nr. 10: Wärmepumpe Investitionskosten „je kW elektrisch" aus der Kennlinie, Statusnummer NACHTRAG-E20-NR, kein Schemaschritt (25.09.2026)

Merge NACHTRAG-E20-MERGE (pm ab origin; e20 ab cbed6dba). Gate/CI: Platzhalter NACHTRAG-E20-GATE / NACHTRAG-E20-CI. Muster:
`E19_Papiere_498_Auftrag_2026-09-25.md`, Statuszeile #498 und Nach #498. Statusnummer beim Merge gemessen (Startnachricht).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile NACHTRAG-E20-NR (E20 — Bezugsgröße „je kW elektrisch" für Investitionskosten der Wärmepumpe: P_el = Ptherm ÷ COP
am Normpunkt der Kennlinie (A2/W35, B0/W35, W10/W35, bei W35 interpoliert), gerechneter Zweig in der Bemessungs-Landkarte mit Schalter
`investition`, Betriebskosten unverändert; Anwenderentscheid 25.09.2026 „Wärmepumpe beides nur bei Investitionskosten nach kW elektrisch und
kW thermisch"; kein Schemaschritt) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein
Merge, kein Stash. ARBEITSORT: Worktree `.claude/worktrees/papiereE20` (Zweig `papiereE20` ab NACHTRAG-E20-MERGE); von der Repowurzel
`C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiereE20`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Formregeln wie #498 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`;
Wiki-Tabu-Regex 0 Treffer; Logbuch Version 1.2.0.4, Wiki `kosten`). Python `"C:\Program Files\Python312\python.exe"` binär, nie `sed -i`;
deutsche Anführungszeichen im Python-Quelltext als \u201e/\u201c.

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e20_berichte.md` (Phase 0 Messung, Fundstellen, Fragen E20‑Q1…Q8 mit Entscheid
[Q6 Anwenderfrage offen]; Phase 1 laut Startnachricht: Commits, Schlüssel, Stellen, Testzahlen, A/B 1024), `E20_Auftrag_2026-09-25.md`;
Konzept § 3.2 Tafel der Bezugsgrößen (Z. ~1788–1797) und § 6.3 Nr. 10; Wiki-Quelle Kosten (Tafel :30–46, Absatz :58); Protokolle
`H4a_Bezugsgroessen_Protokoll.md`, `H4b_Investitionsraster_Protokoll.md`. Alles ganz lesen.

AUFGABEN: (1) Statusdatei: Zeile NACHTRAG-E20-NR nach der letzten Zeile vor `---` und Block Nach NACHTRAG-E20-NR: (a) Fragen E20‑Q1…Q8
(Q1–Q5, Q7, Q8 entschieden 25.09.2026 nach Empfehlung a; **Q6 offen beim Anwender**: „je kWh elektrisch" im Betriebsraster der WP lassen (a)
oder entfernen (b)), (b) Abnahme A‑E20‑1, (c) Nachweis (Anker/Referenzlauf byte-gleich; A/B 1024 Satz 1.000 €/kW → 4.000 €, Herleitung
„11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW"), (d) Befunde (Tab_WP ohne P_el-Spalte, `Nennleistung` als Zähler unbrauchbar mit Beispielen, zwei
Katalogtypen ohne Normpunkt → GERAET, Kühlbetrieb unter dem Heiz-Normpunkt), (e) Papiernachzug, (f) Logbuch, (g) nächste Schritte (Q6-Entscheid;
Wiki-Upload), (h) Gate NACHTRAG-E20-GATE, CI NACHTRAG-E20-CI. (2) Protokoll
`Dokumentation/ueberholt/Protokolle/Reporting/E20_Waermepumpe_kW_elektrisch_Protokoll.md` (Muster E19), Index +1. (3) Konzept: § 3.2 Tafel
neue Zeile WP × `EUR_PRO_KW_ELEKTRISCH` = Σ (Ptherm ÷ COP am Normpunkt) × Satz, Quelle `Tab_Kenndaten`, nur Kategorie 1, mit Fußnote zur
Normpunktregel; § 6.3 Nr. 10 neuer Wortlaut (Anwenderregel 25.09.2026 umgesetzt; Rest: Q6); Kopfzeile Codestand; Register: Familie R‑E20
Q1…Q8 (Q6 offen, Empfehlung a), R‑Rest Zeile Nr. 10 nachziehen; Entscheidwege § 8.x; Analysepapier § 5 Zeile E20. (4) Mockup: Zone
Zeileneditor/Bemessung (Auswahl der WP um „je kW elektrisch (nur Investition)" mit Herleitung), Ressourcentafel, Stand-Absatz. (5) Logbuch
(ein Satz), Wiki-Quelle Kosten: Tafel und Absatz um die WP-Zeile „je kW elektrisch (nur Investition), aus der Kennlinie am Normpunkt"
ergänzen (ohne Tabuwörter), Wiki_Update. (6) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge, verbliebene Platzhalter.
