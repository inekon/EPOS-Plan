# Auftrag Papiere #455 — E8b: Formelmappe Stufen 0–3, Anhang-E-Checkliste (U43), Anhang-D-Gegenprobe (gesichert 23.09.2026)

Merge-SHA, Gate-Zahlen und Entscheidstand nennt die Startnachricht (Platzhalter NACHTRAG-455-MERGE / NACHTRAG-455-GATE /
NACHTRAG-455-ENTSCHEIDE). Muster: `E8a_Papiere_454_Auftrag_2026-09-23.md`, Statuszeile #454 und Block Nach #454. Vor dem Schreiben
`grep -n "#45[0-9]"` in der Statusdatei (#450 Dialog Design Stufe 5, #453 Zapfprofil Z3, #456–#459 Dialog Design; Referenzbasis R13_Kuehlung).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #455 (Etappe E8, Teil b — V‑D; damit E8 abgeschlossen) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere,
kein Build, kein Test. ARBEITSORT: Der Worktree `.claude/worktrees/papiere455` (Zweig `papiere455` ab NACHTRAG-455-MERGE) existiert; von
der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere455`, nie im Hauptbaum arbeiten. Commits sofort mit
`git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #454
(UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version 1.2.0.4,
Wiki-Stichwort `bericht`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e8b_berichte.md` (Phase‑1-Bericht: Commits, Stufen mit Zellvergleich,
ClosedXML-Befund und Entscheid, Anhang-D-Ergebnis, U43, Abweichungen, Fragen E8b‑Q1…Q6, erledigt-Gründe, Logbuchsätze; Phase‑2-Zahlen
nennt die Startnachricht) und `e8_fakten.md` (Abschnitte 1, 3, 4, 5: Konzept § 2.11.6 Stufenplan, V‑G10/V‑G12, Register Q18, Mockup U43,
Anhang D). Alles ganz lesen. Zweig `e8b` von 591229e1: E8b/0 4b06bce8 (Blattstruktur-Wache über Excel- und Word-Generator ergänzt,
ClosedXML-Befund als Wache), E8b/1 5b6a0125 Stufe 0 (Parameterblock je Szenario mit i, T, p_E, p_B, p_I, 15 benannte Bereiche), E8b/2
8cac394b Stufe 1 (Mehrjahrestabelle in Formeln: Energie/CO₂ als Fortschreibung, Betrieb in zwei Termen über Hilfsspalten, Netto
Zeilensumme, Barwert, Kumuliert; bis 272 Formeln), E8b/3 238511ed Stufe 2 (NBW über `NPV` + Jahr 0 + Restwert-Barwert, Annuität `PMT`,
Differenzreihe Variante − Referenz mit Restwert-Nominaldifferenz, `IRR` nur bei einem Vorzeichenwechsel, Amortisation über Hilfsspalte,
Text statt Zellfehler), E8b/4 a8b05f9c Stufe 3 (bemessene Position Menge × Satz, Summe als Spaltensumme, Δ%-Block als Zellbezug), E8b/6
826b2d4d Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne` (wahrscheinlichster Fall 64.479,51 € bei Soll 64.480 €, Worst −202.801,57 €
bei −202.802 €, Best 598.319,65 € bei 598.320 €; Jahreswerte D.5 stimmen; sechs D.6-Zeilen ±0,5 € mit Steigungen 472 / −57 / 1.121 /
−889 / −900 / −354; ausgenommen: zwei D.6-Zeilen mit Pumpenwerten, „Gasverbrauch BHKW", Tippfehler D.7 348.583 statt 349.583 kWh/a;
Rechenweg unverändert), E8b/5 4408523a + 3fa1ec6c U43 (Checkliste 15 Punkte in fünf Gruppen im Kern `AnhangECheckliste`, Stand
erfüllt/teilweise/offen aus dem Lauf; Wortbericht Abschlussseite nach dem Anhang; Excel letztes Blatt „Checkliste Anhang E" mit
Notenspalte 1–5; Knopf-Baustein `AnhangEChecklisteKnopf` im Fuß des Bewertungsblocks); ClosedXML-Befund: 0.105.1 speichert Formeln ohne
Ergebniswert und kennt NPV/PMT/IRR nicht → EPOS rechnet jede Formel in C# nach, trägt den Kernwert als Ergebnis ein, Excel rechnet beim
Öffnen neu (Excel 16 rechnet jede Formelzelle auf den gespeicherten Wert; LibreOffice/openpyxl nicht installiert, XML wie `data_only`
gelesen: Formel und Wert überall vorhanden); Zellvergleich 13 Prüfgruppen Wertfassung = Formelfassung (Formeln je Mappe z. B. 1024 320,
hyb1042 429, hybbk 449, prep1030 134; 1046 ohne Wirtschaftlichkeit 0); Ressourcen `WIRT_FM_*` 20 und `WIRT_AE_*` 83; `Zahlungsbild` drei
reine Ausweisfelder; Wache legt drei Zeilen in `Tab_ProjektWerte` der Arbeitskopie an; weitere Commits (Merge e8a, Nachzug, Fußzeile U48 in
die Knopfreihe = E8a‑Q4) laut Startnachricht. Fragen E8b‑Q1…Q6 (Entscheidstand laut NACHTRAG-455-ENTSCHEIDE): Q1 Kennzahlen
Günstig/Ungünstig bleiben Werte (a, Empfehlung / b zwei weitere Tabellen je Stand); Q2 `WirtschaftlichkeitZeilen.BemessungText` kennt nur 4
von 17 Bemessungsarten („fester Betrag" neben Menge × Satz) — eigener kleiner Auftrag (Empfehlung); Q3 Warnung „Gliederung unvollständig"
bei Startjahr-Positionen (a stehen lassen / b nur Positionen des Jahres 1 vergleichen oder Startjahr nennen — Empfehlung b als eigener
Auftrag); Q4 Knopf ruft Kernfunktion direkt (a so lassen, Empfehlung / b über die Hülle); Q5 Merge-Stellen mit e8a (erledigt beim
Nachzug); Q6 Referenzbasis (Phase 2 gegen R13 nach dem Nachzug). Logbuchsätze: die fünf aus dem Bericht (Stufen 0–3, U43). Abnahme am
Gerät A‑E8b‑1: Excel-Bericht öffnen — Parameterblock, Formeln in der Mehrjahrestabelle, Kennzahlen als Formeln, Betriebskosten Menge ×
Satz, Blatt „Checkliste Anhang E"; Wortbericht Abschlussseite; Knopf „Anhang-E-Checkliste…" auf der Ergebnisseite; Vergleich der Werte mit
der Wertfassung (Excel rechnet beim Öffnen neu).

AUFGABEN: (1) Statusdatei: Zeile #455 (Anlass: Anwender „fahre fort", Etappe E8 Teil b — V‑D; E8 abgeschlossen) nach der letzten Zeile
vor `---` und Block Nach #455 als neuester Block: (a) Fragen E8b‑Q1…Q6 mit Lesarten/Empfehlung/Entscheidstand, (b) Abnahme, (c)
Zellvergleich, Anhang-D-Zahlen, Nachweis „keine Rechenwirkung", (d) Befunde (ClosedXML-Befund und Entscheid, D.6/D.7-Fehler der Norm,
`BemessungText` 4 von 17, Warnung bei Startjahr-Positionen, Knopf-Regel), (e) Papiernachzug, (f) Logbuch, (g) nächste Etappe E9 (Analysepapier
§ 5: Schemaschritte B/C/D Szenariorahmen, Trägerpreise, Erlössätze best/worst — Nummern ab 116 laut Vergabe; vorher prüfen), (h)
Nachweis: Gate NACHTRAG-455-GATE, Push nach Regel. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E8b_Formelmappe_AnhangE_D_Protokoll.md`
(Muster E8a), Index Reporting zählen (+1). (3) Register: Q18 (U43 gebaut #455), R‑V V‑G10/V‑G12 gebaut, Familie R‑E8b mit Q1…Q6, A7
(Anhang-D-Gegenprobe erledigt); Konzept: § 2.11.6 (Stufen 0–3 gebaut, ClosedXML-Befund als Regel „EPOS trägt Werte ein, Excel rechnet
neu", was Werte bleibt unverändert), § 2.11.4 V‑D gebaut, Gap-Tafel V‑G10/V‑G12 erledigt, Anhang D Gegenprobe (Zahlen, Ausnahmen), § 6.1
Zeile E8b, § 7 (E8 abgeschlossen, E9 nächste), Kopfzeile Codestand; Entscheidwege-Protokoll; Analysepapier § 5 „E8 umgesetzt #454/#455 —
E8 abgeschlossen", § 3.3 A7 erledigt, § 3.5 Testlücke der Generatoren geschlossen (Blattstruktur-Wache); Protokoll `05/§ 3.3` bzw. das
Reporting-Protokoll zu ClosedXML mit Verweis auf den Befund. (4) Mockup: Anhang U43 „erledigt #455", Zone „Bericht und Ausgabe"
(Excel-Formelmappe „gebaut #455, Stufen 0–3", „Was dauerhaft Werte bleibt" unverändert), Ressourcentafel Kategorie 8/9 um die 103
Schlüssel, Stand-Absatz. (5) Logbuch (#455, fünf Sätze, Stichwort `bericht`), Wiki-Quelle Wirtschaftlichkeit/Berichte (Ist-Zustand der
Formelmappe und der Checkliste; Tabu-Regex 0). (6) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.
