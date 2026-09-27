# Auftrag Papiere #479 — E17: Nicht monetarisierbare Wirkungen (V‑G11), Tabelle `Tab_ProjektWirkung`, Schemaschritt 127 (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen und CI-Nachweise nennt die Startnachricht (Platzhalter NACHTRAG-479-MERGE / -MERGE2 / -GATE / -GATE2 / -CI).
Muster: `E15_Papiere_478_Auftrag_2026-09-24.md`, Statuszeile #478 und Block Nach #478. Vor dem Schreiben `grep -n "#47[0-9]\|#48[0-9]"`
in der Statusdatei (#481 Katalogimport-Fix einer Anwender-Sitzung, #482 Access-Übernahme eingestellt [dritte Sitzung mig2], #483/#485 Dialog
Design [Schritt 126, Push steht bevor], #484 E16, #486 Zapfprofil Z4b; Schemastand nach #479 = 127; Basis R14_Kaelteerzeuger).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #479 (E17 — nicht monetarisierbare Wirkungen mit Kategorie und Beurteilung nach DIN EN 17463 6.1/8.2; damit
sind V‑G7 und V‑G11 des VALERI-Abgleichs erledigt, offen bleibt V‑G3 = E16) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build,
kein Test, kein Zweigwechsel. ARBEITSORT: Worktree `.claude/worktrees/papiere479` (Zweig `papiere479` ab NACHTRAG-479-MERGE); von der
Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere479`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #478 (UTF-8 ohne BOM, CRLF,
byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex 0 Treffer; Logbuch Version 1.2.0.4, Wiki `wirtschaftlichkeit`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e17_berichte.md` (Phase 1: Commits, Datenmodell, Kern, Dialog, Bericht,
Schlüssel, Abweichungen, Fragen E17‑Q1…Q4, erledigt-Gründe, Logbuchsatz, Abnahme A‑E17‑1 acht Schritte) und Phase 2 laut Startnachricht
(Merge 311780cd mit c99c4c7a — Schema-Dateien 125 vor 127, Deklarationen `Deklarationen(Kurztext(Wirkungen), p)`, resx-Naht ergänzt, Designer
9.998 —, E17/6 Testdatenbank 125 → 127 LFS c99a1eae 67.801.088 Byte [132 Tabellen STRICT, 210 Indizes, 0 Freitexte übernommen], E17/7 Test
Altfeld roh, E17/8 Stilregeln vor das Formularraster und Infoknopf mit Assistent [hebt die Phase‑1-Abweichung `MitAssistent="false"` auf];
gefiltert Kern 27/27 Wirkungen, UI 311/311; voller Lauf Kern 6.072, UI 6.009, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27/1;
Referenzlauf 13/13 R14 394 CSV byte-gleich; SQL 1.811/0), `e15_berichte.md`, Konzept § 2.11.2 V‑G11, Szenarienkonzept § 10/§ 11.1 (W5‑B‑12),
Register R‑V (V‑G11). Alles ganz lesen.

AUFGABEN: (1) Statusdatei: Zeile #479 (Anlass: Anwender 24.09.2026 „V‑G11 … kleiner Dialog-und-Bericht-Auftrag ohne Rechenwirkung") nach der
letzten Zeile vor `---` und Block Nach #479: (a) Fragen E17‑Q1…Q4 (offen, gebaut a), (b) Abnahme A‑E17‑1, (c) Nachweis (keine Rechenwirkung:
Anker, Referenzlauf; Maskenwache-Stand), (d) Befunde (Kern-Datenklasse in UI = Q4; Freitext zählt nicht mehr; KI-Aktion Parameter lesen
meldet Altfeld; Werkzeug baut Sicht 122 bei jedem Schreiblauf neu [Bestand]; Designer-Nachtrag; Lücke 126 bis zum Dialog-Design-Push), (e)
Papiernachzug, (f) Logbuch, (g) nächste Schritte (E16 #484 V‑G3 Schritt nach Regel; Wiki-Upload 26.09.; VALERI-Gap-Tafel: nur V‑G3 offen),
(h) Nachweis: Gate NACHTRAG-479-GATE / -GATE2, CI NACHTRAG-479-CI. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/
E17_Nicht_monetaere_Wirkungen_Protokoll.md` (Muster E15), Index +1. (3) Register: V‑G11 „gebaut #479 (Schritt 127)", Familie R‑E17 Q1…Q4
(offen, Empfehlung a); Konzept § 2.11.2 V‑G11 (Kategorie und Beurteilung gebaut), § 2.11.4 V‑E (nur V‑G3 offen → E16), Checkliste 2b/3b,
§ 6 Schemaschritt 127 (Tabelle, Spalten, Freitext-Übernahme), § 7/Anhang, Kopfzeile Codestand/Schemastand 127; Szenarienkonzept § 10 (Freitext
→ Liste), § 11.1 (W5‑B‑12: V‑G11 vollständig); Analysepapier § 5/§ 6 (Schritt 127 = E17); Entscheidwege-Protokoll. (4) `Referenzlaeufe/
LIESMICH.md`: Nachtrag 127 (Tabelle, LFS c99a1eae, ergebnisneutral; 126 = Dialog Design folgt). (5) Mockup: Zone Bewertungsblock
(Wirkungsliste statt Freitext), Checkliste 2b/3b, Bericht (Tabelle), Ressourcentafel (+40, Stand 9.998), Stand-Absatz. (6) Logbuch (#479, ein
Satz), Wiki-Quelle Wirtschaftlichkeit: Anker `nicht-monetaer` (Liste mit Kategorie, Beschreibung, Dauer, drei Wirkungsgrade, Beurteilung;
Altfeld) und `checkliste` (2b/3b erfüllt/teilweise/offen) nachziehen, Wiki_Update. (7) Bytes, `<tr`, `git diff --stat`, Bericht ohne
Dateiabzüge.
