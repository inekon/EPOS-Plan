# Auftrag Papiere #454 — E8a: ValERI-Ansicht vollständig (Block 2, Block 4, U41/U46/U47/U48/U49) (gesichert 23.09.2026)

Merge-SHA, Gate-Zahlen und Entscheidstand nennt die Startnachricht (Platzhalter NACHTRAG-454-MERGE / NACHTRAG-454-GATE /
NACHTRAG-454-ENTSCHEIDE). Muster: `E7c3_Papiere_452_Auftrag_2026-09-23.md`, Statuszeile #452 und Block Nach #452. Vor dem Schreiben
`grep -n "#45[0-9]"` in der Statusdatei (#450 Dialog Design Stufe 5, #453 Zapfprofil Z3, #456/#457 Dialog Design; Kühlung/Gebäudesimulation
führen eigene Statusdateien; die Referenzbasis ist seit 45bee6df **R13_Kuehlung**).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #454 (Etappe E8, Teil a — V‑C) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test.
ARBEITSORT: Der Worktree `.claude/worktrees/papiere454` (Zweig `papiere454` ab NACHTRAG-454-MERGE) existiert; von der Repowurzel
`C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere454`, nie im Hauptbaum arbeiten. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #452 (UTF-8 ohne BOM, CRLF,
byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version 1.2.0.4, Wiki-Stichwort `bericht`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e8a_berichte.md` (Phase‑1-Bericht mit Commits, Bau je Punkt, Bildprobe,
Abweichungen, Fragen E8a‑Q1…Q4, erledigt-Gründen, Logbuchsätzen; Phase‑2-Zahlen nennt die Startnachricht) und `e8_fakten.md` (Abschnitte
1, 2, 6–10: Registerzeilen E6‑Q1, E5b‑4, Mockup U41/U42/U46/U47/U48/U49, Konzept § 2.11.3/§ 2.11.4 V‑C). Alles ganz lesen. Zweig `e8a` von
591229e1: E8a/1 ddf252bb Block 4 vollständig (E6‑Q1/U49: Spannenbild und Verlauf mit denselben Bausteinen wie unter „Wie sicher ist
das?"), E8a/2 894c970b Block 2 Zahlungsreihen (je Stand und Szenario Jahrestafel der sechs Bestandteile, Netto, Barwert, Summen; Kern-Klasse
`Zahlungsgliederung`; ohne Lauf Hinweiszeile), E8a/3 836cf54c U46 (Gliederung „Woraus entsteht die Zahl?" mit Barwert, Nominalsumme,
Differenz Leitversion − Referenz), E8a/4 6d979cdc U47 (Tafel „Was daraus im Lauf wird": I₀, Ersatzjahre, Restwert nominal je Szenario),
E8a/5 a03b4b5b U48 (Fußzeile „Drei Szenarien gerechnet · Annahmen aus Vorgaben, nichts gepflegt", nach Pflege „gepflegt"), E8a/6 e86394ed
U41 Brückenbild (`ChartRenderer`, Wasserfall 1240 × 610, deterministisch, sieben ChartProben-Bilder, auch im Wortbericht nach den
Verlaufsbildern), E8a/7 e71f2d4a Nachbesserung Hinweis; weitere Commits (Nachzug bba1f1a7 mit R13) laut Startnachricht. 39 neue Schlüssel
(`WIRT_GL_*` 16, `WIRT_ZR_*` 5, `WIRT_LW_*` 5, `WIRT_FUSS_*` 5, `WIRT_BR_*` 8), einer geändert (`WIRT_VALERI_BLOCK_2_HINWEIS`); Designer
8 431 (vor Nachzug). Bildprobe: 153 Prüfungen, 0 Verstöße, 139 Hashes (132 alt gleich, 7 neu `kapitalwert_bruecke*`); Wache der
Zeichenmethoden 32; keine Rechenwirkung, keine Simulationsgröße berührt. Abweichungen (aus dem Bericht): Jahresreihen erst nach einem Lauf
in der Sitzung; Fußzeile U48 als eigene Zeile über dem Knopffuß; Unterzeilen der Gliederung benannt statt nummeriert; Nullwerte als „0";
Brücke mit Euro-Achse; Wortbericht-Stelle vor der Mehrjahrestafel; ChartProben-LIESMICH um „Etappe E8a" ergänzt; Bild nicht im Browser
geprüft (nur bunit). Fragen E8a‑Q1…Q4 — **alle am 23.09.2026 nach Empfehlung entschieden** (Tabelle in `Fakten_2026-09-23\e8a_entscheide.md`; U42 als E8a/8 in
derselben Welle gebaut, Bildnamen laut Startnachricht): Q1 U42 Zahlungsstrombild (a gestapelte Jahresbalken
je Version, Empfehlung / b Netto + kumulierte Linie / c weglassen); Q2 Leitversion (gebaut: größte Kapitalwertdifferenz im Erwartungsfall,
Sicht 2 Stand B; Empfehlung so lassen); Q3 Jahresreihen beim Öffnen (nur mit neuer Nachweisfassung; Empfehlung nicht in E8); Q4 Fußzeile
U48 nach dem Merge mit e8b in die Knopfreihe (Empfehlung ja). Logbuchsätze: die vier aus dem Bericht. Abnahme am Gerät A‑E8a‑1: Block 2
mit Stand-/Szenariowahl, Block 4 mit Spannenbild und Verlauf, Gliederung mit Nominalsumme und Differenzspalte, Brückenbild auf Seite und
im Wortbericht, Tafel „Was daraus im Lauf wird", Fußzeile vor/nach Pflege.

AUFGABEN: (1) Statusdatei: Zeile #454 (Anlass: Anwender „fahre fort", Etappe E8 Teil a — V‑C) nach der letzten Zeile vor `---` und Block
Nach #454 als neuester Block: (a) Fragen E8a‑Q1…Q4 mit Lesarten/Empfehlung/Entscheidstand, (b) Abnahme, (c) Bildprobe und Nachweis „keine
Rechenwirkung" (Anker, Referenzlauf), (d) Befunde (Windows-Messlatte 111 → 139 mit Zapfprofil-Bildern, Jahresreihen nicht gespeichert,
Fußzeilen-Platz), (e) Papiernachzug, (f) Logbuch, (g) nächste Etappe E8b (Formelmappe, U43, Anhang D) dann E9, (h) Nachweis: Gate
NACHTRAG-454-GATE, CI zu 591229e1 (Kern ios 35901717909, Kern main 35901725737, Windows main 35901725467 grün — in Nach #452 (h)
nachtragen), Push nach Regel. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E8a_ValERI_Bloecke_Protokoll.md` (Muster E7c3),
Index Reporting zählen (+1). (3) Register: E6‑Q1 „gebaut #454", E5b‑4 vollständig (U41/U46/U47/U48), Familie R‑E8a mit Q1…Q4; Konzept:
§ 2.11.4 V‑C (fünf Blöcke vollständig, offen nur U42), § 2.11.3 (Hinweis auf die Nummerierung 1–5 des Codes gegen die Tafel — die
Doppelbelegung aus `e8_fakten.md` auflösen: eine Fußnote), § 2.13 (5), § 6.1 Zeile E8a, § 7 (E8b als nächste), Kopfzeile Codestand
(Schemastand 113, Basis R13 laut origin), Anhang; Entscheidwege-Protokoll; Analysepapier § 5 „E8 Teil a umgesetzt #454" und die § 5-Tafelzeile
E8 um E6‑Q1 ergänzen (Unklarheit 1 aus `e8_fakten.md`); Szenarienkonzept § 11 falls Verlauf/Block 4 dort geführt. (4) Mockup: Anhang U41,
U46, U47, U48, U49 „erledigt #454", U42 „erledigt #454" (E8a/8), falls die Startnachricht den Bau bestätigt, sonst offen mit Verweis E8a‑Q1; Ressourcentafel Kategorie 8 um die 39 Schlüssel; Stand-Absatz.
(5) Logbuch (#454, vier Sätze, Stichwort `bericht`), Wiki-Quelle Wirtschaftlichkeit (Ist-Zustand der ValERI-Bewertung; Tabu-Regex 0).
(6) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.
