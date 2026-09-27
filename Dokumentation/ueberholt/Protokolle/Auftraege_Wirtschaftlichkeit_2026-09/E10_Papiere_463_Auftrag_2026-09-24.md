# Auftrag Papiere #463 — E10: Nutzungsdauer S3 (Sätze je Technik, Schemaschritt 120), Speicherflotte an der Nutzungsdauertabelle, Kennzeichnung A8 (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen, CI-Nachweise, Testdatenbank-Stand und Referenzbasis nennt die Startnachricht (Platzhalter NACHTRAG-463-MERGE /
NACHTRAG-463-GATE / NACHTRAG-463-CI / NACHTRAG-463-TESTDB / NACHTRAG-463-BASIS). Muster: `E9b_Papiere_462_Auftrag_2026-09-24.md`,
Statuszeile #462 und Block Nach #462. Vor dem Schreiben `grep -n "#46[0-9]"` in der Statusdatei (#464 Zapfprofil Z4, #465/#466 Dialog
Design laufen parallel; Schemastand nach #463 = 120 (E10), Z4 nimmt 121).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #463 (Etappe E10 des Analysepapiers: Nutzungsdauer Stufe S3 und Speicherflotte; E11 entfällt, danach
E12 Wiki-Runden) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test. ARBEITSORT: Der Worktree
`.claude/worktrees/papiere463` (Zweig `papiere463` ab NACHTRAG-463-MERGE) existiert; von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus
`cd .claude/worktrees/papiere463`, nie im Hauptbaum arbeiten, keinen Zweig wechseln. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #462 (UTF-8 ohne BOM, CRLF,
byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version 1.2.0.4, Wiki-Stichwörter
`nutzungsdauern` und `kosten`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e10_berichte.md` (Phase 1: Commits, Saat-Tafel, Stellen, Schlüssel, Tests,
Vorab-Zahlen 1018/1030/1026/1046, Abweichungen 1–5, Fragen E10‑Q1…Q7, erledigt-Gründe, Logbuchsätze; Phase 2: Umbau E10/9, Tests, A/B-Tafeln,
Referenzlauf, LIESMICH-Text) und `e10_fakten.md` (Quellen mit Zeilen: Analysepapier § 5 E10, A7/A8, § 6; Nutzungsdauer-Konzept § 2.2/§ 3/
§ 4/§ 5/„Offen aus S2"; Register R‑A A7/A8, R‑ND; Konzept § 2.13 (3), § 6.3 Nr. 9h, Item 19; LIESMICH; Rechenweg 08; Tabellenspalten).
Alles ganz lesen. Zweig `e10` von 48d8836d: E10/1 62795858 (Dialog „Nutzungsdauern (AfA)" zeigt Instandsetzung und Wartung, KI-Sicht,
Maskenwache 8 → 12), E10/2 bdd4aba9 (**Schemaschritt 120** sät die Sätze — Mitte des Empfehlungsbereichs der Betriebsvorlagen, nur
Standardzeilen, nur Instandsetzung: Heizkessel 2,0 %, BHKW-Modul 6,0 %, Wärmezentrale 2,0 %, Stromeinspeisung 2,0 %, Bauliche Anlagen
1,25 %; Wärmepumpe/PV/Solarthermie/Speicher leer, Wartung überall leer; Migration, Werkzeug, Nachziehliste, Testdatenbank 6259b348 →
52c4729d, Zielversion 120 — Begründung: iOS migriert nicht, nimmt die Seed-Kopie, deshalb ein Schritt statt Nachsaat beim Start), E10/3
056ccd0d (Vorbelegung: Vorlagenübernahme, Knopf „Sätze vorbelegen…" auf der Betriebsseite, Herkunftszeile unter dem Satzfeld), E10/4
71489e2d (Rechenweg mit implizitem Tabellensatz — **in E10/9 zurückgebaut**, siehe Phase 2), E10/5 b5f9f249 (neuer Kesselkatalogeintrag
in %/a ohne Betrag übernimmt den Wartungssatz; BHKW bleibt; Asymmetrie Item 19 dokumentiert), E10/6 375dc0fa (Speicherflotte: Restwert
je Einheit linear aus der Nutzungsdauer, ohne eigenes Intervall 10 a aus der Tabelle, `RestwertEuro` der Einheit Altfeld — Editor und
KI-Sicht gekennzeichnet; 1046: Restwert 800 → 7.000 € nominal, Kapitalwert +3.432,79 €), E10/7 9d8999f3 (A8: Kessel- und BHKW-Nutzungsdauer
„Nutzungsdauer (Gerätedaten)" mit Tooltip, KI-Vermerk, Vermerk in der Parameterverwendung; Halbsatz „Speichervariante liest 20/21" nicht
gebaut), E10/8 511a6647 (Tests: `NutzungsdauerS3Tests`, `SpeicherFlottenNutzungsdauerTests`, `NutzungsdauerKennzeichnungTests`,
Ergänzungen; `FlottenWirtschaftlichkeitTests` −390 statt −350), E10/9 und Phase‑2-Commits laut Startnachricht. Schlüssel: 24 neu
(`ND_SP_*`, `ND_SAETZE_*`, `KI_DLG_NUD_*`, `ND_SATZ_*`, `ND_SATZART_*`, `FLOTTE_ED_*`, `KBROW_ND_GERAETEDATEN_HINWEIS`), 6 geändert; Stand je
Sprache laut Startnachricht. **Befund 1030 und Umbau (zentral für die Papiere):** Der implizite Tabellensatz zur Rechenzeit hätte bei
1030 doppelt gezählt (H4a-Rückfall auf die Komponente bei Anlage 14921 ohne eigene Investition, dazu Sammelposten „BHKW" 18.000 €/a und
„Heizkessel" 2.000 €/a neben Einzelpositionen) und gerechnete Wirtschaftlichkeiten ohne Zutun geändert (Anwenderentscheid ND‑Q4) —
deshalb wirken die Tabellensätze nur über die explizite Vorbelegung (Vorlagenübernahme, Knopf), der Rechenweg liest den gepflegten Satz
wie bisher; Anker unverändert. Fragen an den Anwender: E10‑Q1 (a gebaut: Sätze über explizite Vorbelegung — Empfehlung; b impliziter
Rückfall zur Rechenzeit, verworfen mit 1030-Zahlen), E10‑Q2 (a gebaut), E10‑Q3 (a gebaut, 1046 +3.432,79 €; b ±0), E10‑Q4 (a gebaut), E10‑Q5
(Basis laut Startnachricht: R13 byte-gleich mit LIESMICH-Nachtrag oder R14), E10‑Q6 (a gebaut), E10‑Q7 Saatwert (a Mitte gebaut; b/c/d).
Datenbefund 1030 (Sammelposten neben Einzelpositionen) als Hinweis für den Anwender. Abnahme am Gerät A‑E10‑1: Dialog „Nutzungsdauern
(AfA)" mit den zwei Satzspalten; Kostenverwaltung › Betrieb: Knopf „Sätze vorbelegen…" auf einer Projektkopie, Herkunftszeile, Kapitalwert
vorher/nachher; Flotten-Editor: Ersatzintervall-Vorgabe und Altfeld-Kennzeichen, 1046 Restwert; Kataloge: „Nutzungsdauer (Gerätedaten)".
Logbuch: die vier Sätze aus dem Bericht (ggf. nach dem Umbau angepasst: „auf Knopfdruck vorbelegt", kein „rechnen mit dem Satz der
Tabelle"). CI #462 (48d8836d): ios Kern 35956782471, main Kern 35956786642, main Windows 35956786551 grün — Nachtrag in Nach #462 (h).

NEBENBEFUNDE: (1) Nutzungsdauer-Konzept-Kopfzeile nennt Codestand 41764ab0/Zielversion 113 → aktualisieren, § 3 S3 „umgesetzt #463", § 6
Zeile S3, Konzept wandert laut Statusdatei Nach #264 nach `ueberholt/`, sobald S3 abgeschlossen ist — prüfen, ob das jetzt gilt (dann
verschieben und Verweise nachziehen, Link-Wache); (2) `Referenzlaeufe/LIESMICH.md` laut Startnachricht (Nachtrag 116–120, Einfrierregel);
(3) Wiki Kosten: Trägerkarte ±-Knöpfe (aus #462) und Nutzungsdauern-Dialog Sätze; (4) Analysepapier § 6: (H) bleibt optional/nicht gebaut,
Schritt 120 = E10 (Nachsaat), Z4 = 121; (5) Befund P5 Rest (aus #462) unverändert offen.

AUFGABEN: (1) Statusdatei: Zeile #463 (Anlass: Anwender „Fahre fort", Etappe E10 — ND‑S3 und Speicherflotte; A7/A8) nach der letzten
Zeile vor `---` und Block Nach #463 als neuester Block: (a) Fragen E10‑Q1…Q7 mit Lesarten/Empfehlung (offen), (b) Abnahme A‑E10‑1, (c)
A/B-Tafeln (gekürzt), Referenzlauf/Basis, Anker unverändert, (d) Befunde (1030 Doppelzählung/Sammelposten, Umbau E10/9, Saat-Tafel,
Nebenbefunde), (e) Papiernachzug, (f) Logbuch, (g) nächste Etappe: E12 Wiki-Runden (Sonnet; Sammel-Upload 28.09.2026) — E11 entfällt; damit
ist der Etappenplan E0–E12 des Analysepapiers bis auf E12 abgearbeitet; offen beim Anwender die Fragen aus E7c3, E8c, E9a, E9b, E10 und
die Abnahmen, (h) Nachweis: Gate NACHTRAG-463-GATE, Push nach Regel. (2) Protokoll
`Dokumentation/ueberholt/Protokolle/Reporting/E10_Nutzungsdauer_S3_Speicherflotte_Protokoll.md` (Muster E9b), Index Reporting +1. (3)
Register: A7 „gebaut #463", A8 „gekennzeichnet #463 (Halbsatz Speichervariante offen)", ND‑Q6/ND‑Q7 umgesetzt #463, Nr. 9h erledigt, neue
Familie R‑E10 mit Q1…Q7 (offen, Empfehlung), Nr. 20 (A/B E10); Konzept: § 2.13 (3) (U39 erledigt), § 6.3 Nr. 9h und Item 19 (dokumentiert),
§ 6/§ 7 (E10 gebaut #463, E12 nächste), Kopfzeile Codestand/Schemastand 120; Nutzungsdauer-Konzept (Nebenbefund 1); Analysepapier § 5 „E10
umgesetzt #463", § 4 A7/A8 Stand, § 6 (Nebenbefund 4), § 2.4 Nebenkonzepte Nutzungsdauer; Entscheidwege-Protokoll. (4) Mockup: U39
„erledigt #463", Zone Kosten/Nutzungsdauer (Sätze, Knopf, Herkunft), Ressourcentafel (+24, 6 geändert), Stand-Absatz. (5) Logbuch (#463,
vier Sätze), Wiki-Quellen Kosten/Nutzungsdauern (Nebenbefund 3; Tabu-Regex 0). (6) Bytes, `<tr`, `git diff --stat`, Bericht ohne
Dateiabzüge.
