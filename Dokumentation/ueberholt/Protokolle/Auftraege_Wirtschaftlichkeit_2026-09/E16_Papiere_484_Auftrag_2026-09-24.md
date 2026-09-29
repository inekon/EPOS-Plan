# Auftrag Papiere #484 — E16: Wiederholperiode je Kostenposition (V‑G3 „alle n Jahre"), Schemaschritt 129 (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen und CI-Nachweise nennt die Startnachricht (NACHTRAG-484-MERGE = ae7b0ed0 auf pm18 ab c778ab12, e16 = 7c8f4fc4; Schritt 129; -GATE / -CI
nach dem Gate GATE484.log und der CI). Muster: `E17_Papiere_479_Auftrag_2026-09-24.md`, Statuszeile #479 und Block Nach #479.
Vor dem Schreiben `grep -n "#48[0-9]"` in der Statusdatei (#488 Auslieferungsvorlage-Testwert, #487 Dialog Design Projekt-Gebäudeliste
[reserviert], #486 Zapfprofil Z4b; Entscheide E14/E15/E17 am 24.09.2026 nachgetragen [132d37da, Datum berichtigt c778ab12]; AK1 W3 [fremde Sitzung, nicht erreichbar] hat 21:22 Schemaschritt 128 gepusht [01408e8b, ErgebnisGebaeudeSchema, Testdatenbank 81209c50] ohne Statuszeile, E16 daher 129 [in Phase 1 vorläufig 128]; Basis R14_Kaelteerzeuger).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #484 (E16 — Wiederholperiode je Betriebskostenposition nach DIN EN 17463 6.3.1 „alle n Jahre"; damit ist
V‑G3 als letzte Lücke der VALERI-Gap-Tafel geschlossen) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein
Zweigwechsel. ARBEITSORT: Worktree `.claude/worktrees/papiere484` (Zweig `papiere484` ab NACHTRAG-484-MERGE); von der Repowurzel
`C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere484`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #479 (UTF-8 ohne BOM, CRLF,
byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex 0 Treffer; Logbuch Version 1.2.0.4, Wiki `kosten`). Python:
`"C:\Program Files\Python312\python.exe"`, Dateien binär lesen/schreiben, nie `sed -i`.

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e16_berichte.md` (Phase 1: Commits, Schemaschritt, Schlüssel, Code-Stellen,
Fragen E16‑Q1…Q4, Abweichungen 1–9, erledigt-Gründe, Logbuchsatz, Abnahme A‑E16‑1; Phase 2 laut Startnachricht: Nachzug, Testdatenbank,
Testzahlen, A/B-Tafel 1030 Zeile 101600098, Referenzlauf, SQL-Prüfer, Designer), `e17_berichte.md`/`e15_berichte.md` (Form), Konzept
§ 2.11.2 (V‑G3), § 2.13 (3), Rechenweg `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md`,
Register R‑V (V‑G3). Alles ganz lesen.

AUFGABEN: (1) Statusdatei: Zeile #484 (Anlass: Anwender 24.09.2026 „V‑G3 n‑jährliche Zeitpunkte: Ausbau der Bemessung an den
Kostenpositionen (eine Wiederholperiode je Position plus Rechenweg und Ausweis), ebenfalls mit Schemaspalte") nach der letzten Zeile vor
`---` und Block Nach #484: (a) Fragen E16‑Q1…Q4 (offen, gebaut a), (b) Abnahme A‑E16‑1, (c) Nachweis (ohne Pflege bitgleich: Anker,
Referenzlauf; A/B-Tafel), (d) Befunde (Abweichungen 1–9, Umschlag 10 → 11, Maskenwache 8 → 9, Schrittnummer-Messung), (e) Papiernachzug,
(f) Logbuch, (g) nächste Schritte (VALERI-Gap-Tafel geschlossen; Wiki-Upload 26.09.; Abnahmen A‑E13…A‑E17), (h) Nachweis: Gate
NACHTRAG-484-GATE, CI NACHTRAG-484-CI. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E16_Wiederholperiode_Protokoll.md`
(Muster E17), Index +1. (3) Register: V‑G3 „gebaut #484 (Schritt NACHTRAG-484-SCHRITT)", Familie R‑E16 Q1…Q4 (offen, Empfehlung a);
Konzept § 2.11.2 V‑G3 teilweise → umgesetzt (alle vier Zeitpunktarten), § 2.11.4 V‑E (keine Lücke offen), § 2.13 (3) Startjahr = Beginn
der Folge, § 6 Schemaschritt mit Spalten, § 7/Anhang, Kopfzeile Codestand/Schemastand; Rechenweg 08 (Zahlungsjahre s, s+n, … ≤ T, Regel
`KapitalwertRechner.ZahltImJahr`); Analysepapier § 5/§ 6 (Schritt = E16); Entscheidwege-Protokoll. (4) `Referenzlaeufe/LIESMICH.md`:
Nachtrag Schemaschritt (zwei Spalten, LFS-SHA, ergebnisneutral; „heute Schemastand"). (5) Mockup: Zeileneditor-Zone (Feld „Zahlung alle n
Jahre" mit Herleitung), Betriebskostentabelle „alle n Jahre ab Jahr X", Formelmappe (Hilfsspalte), Ressourcentafel (+8, Designer 10.006),
Stand-Absatz. (6) Logbuch (#484, ein Satz, Wiki `kosten`), Wiki-Quelle Kosten: Zeileneditor-Abschnitt (Feld, Herleitung, Wirkung auf die
Wirtschaftlichkeit, Vorlagenübernahme) und Wiki-Quelle Wirtschaftlichkeit (Betriebskostentabelle „alle n Jahre"), Wiki_Update. (7) Bytes,
`<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.
