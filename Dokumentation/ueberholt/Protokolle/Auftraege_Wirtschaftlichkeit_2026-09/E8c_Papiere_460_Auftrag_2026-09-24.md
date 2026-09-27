# Auftrag Papiere #460 — E8c: Bemessungstexte aller Bemessungsarten (E8b‑Q2), Gliederungsprobe nur Jahr‑1-Positionen (E8b‑Q3), U42-Kommentar (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen und CI-Nachweise nennt die Startnachricht (Platzhalter NACHTRAG-460-MERGE / NACHTRAG-460-GATE / NACHTRAG-460-CI).
Muster: `E8b_Papiere_455_Auftrag_2026-09-23.md`, Statuszeile #455 und Block Nach #455. Vor dem Schreiben `grep -n "#45[0-9]\|#46[0-9]"` in
der Statusdatei (#456–#459 Dialog Design sind gepusht, #453 Zapfprofil Z3 folgt; Referenzbasis R13_Kuehlung; Schemastand 114 durch
Kühlung KU2, 115 Zapfprofil T2, E9a nimmt 116–118).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #460 (Mini-Welle E8c nach E8b: die zwei kleinen Aufträge aus E8b‑Q2 und E8b‑Q3 plus der veraltete
U42-Kommentar) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test. ARBEITSORT: Der Worktree
`.claude/worktrees/papiere460` (Zweig `papiere460` ab NACHTRAG-460-MERGE) existiert; von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus
`cd .claude/worktrees/papiere460`, nie im Hauptbaum arbeiten. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #455 (UTF-8 ohne BOM, CRLF,
byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version 1.2.0.4, Wiki-Stichwort `bericht`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e8c_berichte.md` (Phase‑1- und Phase‑2-Bericht: Commits, Bau je Punkt,
Zellvergleich, Schlüssel, Abweichungen, zwei Fragen, erledigt-Gründe, Logbuchsätze, Testzahlen, Referenzlauf), `e8b_entscheide.md` und
`e8_fakten.md` (Register R‑E8b, Konzept § 2.11.6, Mockup U42/U43). Alles ganz lesen. Zweig `e8c` von fbe93de6: E8c/1 e91617da
(`WirtschaftlichkeitZeilen.BemessungText` liest den `BemessungKatalog` — Texte `BM_*` für alle 18 Steuerwerte de/en, BHKW und
Pufferspeicher mit eigener Beschriftung „je Liter"; Wort- und Tabellenbericht geben die Komponente mit; „fester Betrag" nur noch bei BETRAG
und bei leerem/unbekanntem Steuerwert; neu `BetriebskostenCtrl.Bemessungsfaktor`, den Herleitung und Formelmappe Stufe 3 gemeinsam
fragen; Test `BemessungstexteAlleArtenTests` je Art plus Wächter über alle `DbWerte.BEMESSUNG_*`; Zellvergleich 16 Prüfgruppen, geändert
nur 54 Zellen der Spalte „Bemessung", Beträge/Mengen/Sätze/Formeln unverändert), E8c/2 7c901689 (Gliederungsprobe der Betriebskosten
vergleicht nur Positionen des ersten Jahres — Lesart b; `KostenPositionNachweis.StartJahr`; gemeinsame Probe für Wort- und
Tabellenbericht, Toleranz 0,50 €; Herleitungsspalte „ab Jahr X", Hinweistext erklärt es; Test `BetriebskostenStartjahrGliederungTests`;
hybtest und hybbk warnen nicht mehr, hybluecke warnt mit 1.800 gegen 2.300 €), E8c/3 b7dd5e1b (nur der U42-Kommentar in
`WirtschaftlichkeitSeite.razor`); weitere Commits (Nachzug auf e513f05e = Dialog Design #458/#459) laut Startnachricht. Schlüssel:
fünf `BEMESSUNG_*` entfallen (de/en), `WIRT_BK_AB_JAHR` neu, `WIRT_BK_ABWEICHUNG` („Positionen des ersten Jahres") und `WIRT_BK_HINWEIS`
geändert; Abweichungen: keine neuen Schlüssel für Punkt 1 (Katalogtexte), sichtbar „je Betriebsstunde" → „je Stunde", „% of fuel cost" →
„% of fuel costs", „fester Jahresbetrag"; fester Jahresbetrag mit Menge und Satz bekommt keine Herleitung mehr (kommt in der
Testdatenbank nicht vor); Nachweisumschlag Fassung 8 → 9 (Anker `ErgebnisansichtTests` angepasst; ältere Läufe lesen sich als „ab Jahr
1"). Fragen an den Anwender: E8c‑Q1 „je Stunde" bleibt (Katalogtext wie im Kostendialog; a, Empfehlung) oder `BM_STUNDE` → „je
Betriebsstunde" (b); E8c‑Q2 Befund „je kWh Kapazität"/„je m² Kollektorfläche" heißen in der Herleitung an Betriebszeilen „kWh/a"/„m²/a"
— beide Arten im Betriebsraster nicht wählbar, nur Fremd-/Altdaten; lassen (a, Empfehlung) oder umbenennen (b). Logbuchsätze: die zwei aus
dem Bericht. Abnahme am Gerät A‑E8c‑1: Wort- und Tabellenbericht öffnen — Betriebskostentabelle nennt je Position die Bemessungsart,
Positionen mit späterem Startjahr tragen „ab Jahr …", hybtest-Projekt ohne Hinweis „Gliederung unvollständig", ein Projekt mit echter
Lücke weiterhin mit Hinweis.

NEBENBEFUNDE (in den Block Nach #460 unter (d) und, wo genannt, in die Papiere): (1) Linux-Messlatte der ChartProben im Repo trägt die
14 neuen Bilder aus #454 (Brücke, Zahlungsstrom) nicht — Windows-Messlatte lokal 146 Zeilen; Nachtrag beim nächsten Linux-Lauf; (2)
Rechenweg 08, Gap-Kurztafel V‑G6/8/9/11 steht auf dem Stand vor #434 — nachziehen; (3) der Nachweisumschlag im Mockup nennt Fassung 3,
heute 9 — im Mockup und in der Wiki-Quelle berichtigen; (4) CI-Nachweise: fbe93de6 Kern main 35923242428 und Windows main 35923242569
grün, Kern ios abgebrochen (überholt durch c65aefe4, Lauf 35923955544 grün); 46023235 Kern ios 35932714594, Kern main 35932719275,
Windows main NACHTRAG-460-CI; (5) Schemaschritt-Vergabe 24.09.2026: 114 Kühlung KU2 (KU‑S3, Commit 24074b3a), 115 Zapfprofil T2, E9a
116–118 — die Papiere vom 23.09. (Statusdatei Nach #455 (g), Konzept, Analysepapier § 6) nennen noch „114 Zapfprofil, 115 Dialog
Design" → berichtigen.

AUFGABEN: (1) Statusdatei: Zeile #460 (Anlass: Anwender „Fragen aus E8b: Empfehlung" → Q2/Q3 als eigene Aufträge; Mini-Welle E8c) nach
der letzten Zeile vor `---` und Block Nach #460 als neuester Block: (a) Fragen E8c‑Q1/Q2 mit Lesarten und Empfehlung (offen beim
Anwender), (b) Abnahme A‑E8c‑1, (c) Zellvergleich, Umschlag Fassung 9, Nachweis „keine Rechenwirkung" (Anker, Referenzlauf 13/13), (d)
Befunde und Nebenbefunde (1)–(5), (e) Papiernachzug, (f) Logbuch, (g) nächste Etappe: E9a läuft (Schemaschritte 116–118, Kern liest die
Paare, Statusnummer #461), dann E9b (Dialoge, Hinweistext entfällt, #462), (h) Nachweis: Gate NACHTRAG-460-GATE, Push nach Regel. (2)
Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E8c_Bemessungstexte_Startjahr_Protokoll.md` (Muster E8b, kürzer), Index
Reporting zählen (+1). (3) Register: R‑E8b Q2 und Q3 „erledigt #460 (E8c)", neue Familie R‑E8c mit Q1/Q2 (offen, Empfehlung a);
Konzept: § 2.11.6 (Stufe 3 Menge × Satz über `Bemessungsfaktor`, Bemessungstexte aus dem Katalog), Abschnitt zur Betriebskosten-Gliederung
(Regel „Probe nur Positionen des ersten Jahres, ‚ab Jahr X' in der Herleitung"), § 7 Zeile E8c „gebaut #460", Kopfzeile Codestand;
Analysepapier § 5 „E8c #460", § 6 Schemaschritt-Vergabe berichtigen (Nebenbefund 5); Entscheidwege-Protokoll. (4) Mockup: U42-Kommentar
erledigt, Zone „Bericht und Ausgabe" (Betriebskostentabelle mit Bemessungsart und „ab Jahr", Umschlag Fassung 9), Ressourcentafel
(−5 `BEMESSUNG_*`, +`WIRT_BK_AB_JAHR`, 2 geändert; Stand 8.613), Stand-Absatz. (5) Logbuch (#460, zwei Sätze, Stichwort `bericht`),
Wiki-Quelle Berichte (Betriebskostentabelle: Bemessungsart, „ab Jahr", Hinweis nur bei echter Lücke; Tabu-Regex 0). (6) Bytes, `<tr`,
`git diff --stat`, Bericht ohne Dateiabzüge.
