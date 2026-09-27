# Auftrag Papiere #498 — E19: Restpunkte § 6.3 Nr. 15 (überholt) und Nr. 33 (Unternehmensart ohne BHKW im Parameterdialog), kein Schemaschritt (gesichert 25.09.2026)

Merge NACHTRAG-498-MERGE (pm20 ab origin; e19 ab b5a2e389 mit Nachzug a0bbc633 = Z5 #495 Schritt 140). Gate und CI stehen aus: Platzhalter
NACHTRAG-498-GATE / NACHTRAG-498-CI. Muster: `E18_Papiere_492_Auftrag_2026-09-24.md`, Statuszeile #492 und Nach #492. Vor dem Schreiben
`grep -n "#49[0-9]"` in der Statusdatei (#494 Berichtsvorlagen-Sitzung, #495 Z5 Zapfprofil Schritt 140, #496 Dialog Design Katalog Schritt 141,
#497 Dialog Design Assistent; G3/G4/AK1 Cloud-Sitzungen Schritte 132–139; Basis R14_Kaelteerzeuger; Zielversion beim Merge messen).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #498 (E19 — zwei Restpunkte der Wirtschaftlichkeit: Nr. 15 „Bilanzjahr und Unternehmensart wirken erst beim
nächsten Öffnen" als durch die Schalentrennung überholt geschlossen, mit Wache-Test; Nr. 33 „Unternehmensart ohne BHKW nicht pflegbar"
gebaut: Unternehmensart im Parameterdialog Gruppe Strom nur ohne BHKW, darunter die Anzeige des erfassten Stromsteueranteils live, KI-Feld
mit Sperre, Maskenwache 34 → 35; § 9b damit für Projekte ohne BHKW erreichbar; kein Schemaschritt) für EPOS-Plan. Antworten auf Deutsch. Nur
Papiere, kein Build, kein Test, kein Zweigwechsel. ARBEITSORT: Worktree `.claude/worktrees/papiere498` (Zweig `papiere498` ab
NACHTRAG-498-MERGE); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere498`, nie im Hauptbaum. Commits sofort
mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie
#492 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex 0 Treffer; Logbuch Version 1.2.0.4, Wiki
`wirtschaftlichkeit`). Python `"C:\Program Files\Python312\python.exe"`, binär lesen/schreiben, nie `sed -i`; deutsche Anführungszeichen im
Python-Quelltext als \u201e/\u201c.

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e19_berichte.md` (Phase 0 Befunde mit Fundstellen, Fragen E19‑Q1…Q6 mit Entscheid;
Phase 1 Commits, Schlüssel, Stellen, Testzahlen, erledigt-Gründe, Logbuchsatz, Abnahme A‑E19‑1), `E19_Auftrag_2026-09-25.md`, Muster
`e18_berichte.md`; Konzept § 6.3 (Nr. 15, 33), § 2.2 (Parameterdialog Gruppe Strom, BHKW-Dialog Gruppe 4), § 2.4, § 3.8; Protokolle
`B4_Energieintensitaet_Protokoll.md` (Grenze 2), `E18_Restpunkte_Stromsteuer_Protokoll.md` (A‑E18‑1 Vorbehalt 1017). Alles ganz lesen.

AUFGABEN: (1) Statusdatei: Zeile #498 (Anlass: Anwender 25.09.2026 „fahre fort"; Entscheide E19‑Q1…Q6 am 25.09.2026 nach Empfehlung durch den
Orchestrator, Q4 = b) nach der letzten Zeile vor `---` und Block Nach #498: (a) Fragen (entschieden, gebaut), (b) Abnahme A‑E19‑1,
(c) Nachweis (Anker/Referenzlauf bitgleich; § 9b ohne BHKW an 1007-Kopie), (d) Befunde (Nr. 15 überholt seit Schalentrennung; 1017 hat BHKW
→ A‑E18‑1-Vorbehalt gegenstandslos; Bilanzjahr nur mit Brennstofferzeuger = benannte Grenze Q4 b; iOS unberührt), (e) Papiernachzug,
(f) Logbuch, (g) nächste Schritte (Wiki-Upload 26.09.; Abnahmen; Restpunkte Nr. 10/11/13/18), (h) Nachweis: Gate NACHTRAG-498-GATE,
CI NACHTRAG-498-CI. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E19_Unternehmensart_ohne_BHKW_Protokoll.md` (Muster E18),
Index +1. (3) Konzept: § 6.3 Nr. 15 erledigt/überholt (Einzeiler, Grund im Entscheidwege-Protokoll), Nr. 33 erledigt (mit der Grenze
Bilanzjahr), § 2.2 Parameterdialog Gruppe Strom (Unternehmensart nur ohne BHKW, Anzeige), § 3.8 (§ 9b ohne BHKW erreichbar), § 6.5 falls
Pflegestellen-Tafel; Kopfzeile Codestand/Zielversion (beim Merge gemessen, Schritt von Z5/#495, nicht E19); Register Familie R‑E19 Q1…Q6
(entschieden 25.09.2026 nach Empfehlung, Q4 b); Entscheidwege-Protokoll (§ 8.x); Analysepapier § 5 Zeile E19 (kein Schemaschritt).
(4) `Referenzlaeufe/LIESMICH.md`: kein Nachtrag, „heute Schemastand" prüfen. (5) Mockup: Parameterdialog Gruppe Strom (Unternehmensart +
Anzeigezeilen, Sichtbarkeitsregel ohne BHKW), Ressourcentafel (Designer-Stand vom Merge), Stand-Absatz. (6) Logbuch (#498, ein Satz),
Wiki-Quelle Wirtschaftlichkeit: Anker `parameter` (Unternehmensart ohne BHKW, Verweis mit BHKW) und `bhkw-wirtschaftlichkeit` (Halbsatz),
Wiki_Update. (7) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge, verbliebene Platzhalter mit Fundstellen.
