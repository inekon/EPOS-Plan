# Auftrag Papiere E21 — Pflegewelle Nr. 23 (erledigt) und Nr. 24/Datenlücken (benannt), Statusnummer NACHTRAG-E21-NR, kein Schemaschritt (25.09.2026)

Merge NACHTRAG-E21-MERGE (pm ab origin; e21 ab cbed6dba). Gate/CI: Platzhalter NACHTRAG-E21-GATE / NACHTRAG-E21-CI. Muster:
`E19_Papiere_498_Auftrag_2026-09-25.md`, Statuszeile #498 und Nach #498. Statusnummer beim Merge gemessen (Startnachricht).

## Wortlaut des Agentenauftrags (model: sonnet)

Papierpflege zur Statuszeile NACHTRAG-E21-NR (E21 — Pflegewelle: § 6.3 Nr. 23 resx-Sammelnachtrag als erledigt festgestellt, zwei verwaiste
Schlüssel gestrichen, drei Rückfalltexte angeglichen, Kommentare korrigiert; Nr. 24 und die Datenlücken 1018/1024/1023/1030/1026 gemessen und
benannt, keine Datenpflege) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge,
kein Stash. ARBEITSORT: Worktree `.claude/worktrees/papiereE21` (Zweig `papiereE21` ab NACHTRAG-E21-MERGE); von der Repowurzel
`C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiereE21`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`. Formregeln: UTF-8 ohne BOM, CRLF, byte-erhaltend (Bytes und CR=LF prüfen); Python
`"C:\Program Files\Python312\python.exe"` binär, nie `sed -i`; deutsche Anführungszeichen im Python-Quelltext als \u201e/\u201c. Kein Mockup,
kein Wiki, kein Logbuch (nichts sichtbar).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e21_berichte.md` (Phase 0 mit Befundtafel und Fragen Q1…Q9, Phase 1 Commits und
Testzahlen laut Startnachricht), `E21_Auftrag_2026-09-25.md`. Ganz lesen.

AUFGABEN: (1) Statusdatei `Dokumentation/aktuell/Status_iOS_Migration.md`: Zeile NACHTRAG-E21-NR (Anlass: Anwender 25.09.2026 „sonst nach
Empfehlung") nach der letzten Zeile vor `---`, kurz (Pflegewelle); Block Nach NACHTRAG-E21-NR mit (a) Fragen E21‑Q1…Q9 (entschieden 25.09.2026
nach Empfehlung a), (b) Befundtafel der Datenlücken in Kurzform (je Projekt ein Satz: Lücke, Grund, warum nicht gepflegt, Kandidat für
spätere Neueinfrierung ja/nein), (c) Nachweis (Referenzlauf byte-gleich, Testdatenbank unverändert), (d) Ressourcen (zwei Streichungen,
Designer-Stand), (e) nächste Schritte (Kandidaten 1018-Träger/1023 bei der nächsten Neueinfrierung nach R15; PV-Projekt mit vollständigen
Preisen als eigene Welle), (f) Nachweis Gate NACHTRAG-E21-GATE, CI NACHTRAG-E21-CI. (2) Konzept
`Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` § 6.3: Nr. 23 erledigt (Einzeiler),
Nr. 24 neuer Wortlaut „gemessen 25.09.2026, benannt: …" (Kurztafel oder Halbsätze, Kandidaten für die nächste Neueinfrierung), Kopfzeile
Codestand. (3) Register: Familie R‑E21 Q1…Q9 (entschieden 25.09.2026 nach Empfehlung a). (4) Entscheidwege-Protokoll: Abschnitt § 8.x.
(5) Kurzes Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E21_Pflege_Ressourcen_Testdaten_Protokoll.md` (Muster E19, kürzer), Index +1.
(6) Analysepapier § 5 Zeile E21 (Pflege, kein Schema). (7) **Zusatz CI #498:** in `Status_iOS_Migration.md` Block Nach #498 (h) und im Protokoll `E19_Unternehmensart_ohne_BHKW_Protokoll.md` die Stelle „steht aus (Beobachtung nach dem Push)“ ersetzen durch „Kern `main` 36103496631 und Windows `main` 36103496647 auf `cbed6dba` grün; der Kern-Lauf 36103490160 auf dem Arbeitszweig vom Nachfolger (#497 `99815b47`) abgebrochen“. (8) Bytes, `git diff --stat`, Bericht ohne Dateiabzüge, verbliebene Platzhalter.
