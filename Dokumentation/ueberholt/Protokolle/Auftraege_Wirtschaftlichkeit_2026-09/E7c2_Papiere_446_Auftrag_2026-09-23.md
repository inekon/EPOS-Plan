# Auftrag Papiere #446 — E7c2: Schritte E/F/G, S‑2, V‑1/V‑2, B‑4, E7c1-Reste (gesichert 23.09.2026)

Merge-SHA, Gate-Zahlen nennt die Startnachricht (Platzhalter NACHTRAG-446-MERGE / NACHTRAG-446-GATE). Muster:
`E7c1_Papiere_440_Auftrag_2026-09-23.md`, Statuszeile #440 und Block Nach #440. Vor dem Schreiben `grep -n "#446\|#447"` in der Statusdatei
(Nachbarsitzungen vergeben Nummern parallel; #443 Zapfprofil, #444/#445 Dialog Design sind vergeben).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #446 (Etappe E7, Teil c2) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test.
ARBEITSORT: Der Worktree `.claude/worktrees/papiere446` (Zweig `papiere446` ab NACHTRAG-446-MERGE) existiert; von der Repowurzel
`C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere446`, nie im Hauptbaum arbeiten. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #440 (UTF-8 ohne BOM, CRLF,
byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version 1.2.0.4).

FAKTEN: vollständig in `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e7c2_zwischenbericht.md` (Punkte 1–5 mit A/B-Tafeln),
`e7c2_berichte.md` (Phase‑1-Bericht Punkte 6–9, Nachtrag E7c2/10, Schlüssel, Abweichungen, Fragen, erledigt-Gründe, Logbuchsätze) und
`e7c2_entscheide.md` (Anwenderentscheide E7c2‑Q1…Q8 vom 23.09.2026: alle nach Empfehlung außer Q4 Brennstoff 24 „Sonstige" kWh statt m³,
gebaut als E7c2/10); Phase‑2-Zahlen (Nachzug, Tests, Referenzlauf gegen R12, Designer, SQL-Prüfer) nennt die Startnachricht. Alle drei
Dateien ganz lesen; Grundlage der Etappe: `Fakten_2026-09-23\e7c_fakten.md` Abschnitte 4–6 (Registerzeilen A3, A4, A6, A9, ET‑D‑3, U‑1,
Konzept § 4/§ 6/§ 6.3 Nr. 9h, Mockup U32/U39). Zweig `e7c2` von origin 4971556a: E7c2/1 e924834d Schritt E (108: `ErsatzFuehren`,
`RestwertAnsetzen` je Position, nullbar, NULL = wie bisher; Positions- und Vorlagendialog), E7c2/2 66620b70 Schritt F (109: `Preisbasis`
als eigener Kartenzustand, DML aus `ID_Umrechnung`: 5 Zeilen kWh, 23 Abrechnungseinheit; Rückfall −1 entfällt), E7c2/3 8854ba56 Schritt G
(110: fünf Gase 1, 2, 3, 14, 25 „m³" → „Nm³" in Einheit/Preiseinheit + eine Preiszeile 1039; erweitert durch E7c2/10 af450ed6: Brennstoff 24
„Sonstige" → kWh/€/kWh, kein Nutzer in Test- und Anwenderdatenbank, fremde Träger/Preise bleiben und werden im Migrationsprotokoll
gezählt), E7c2/4 657abb4a S‑2 (Mischlage § 53/§ 53a neben § 54 gesperrt, § 54-Betrag 0, Kohärenzzeile WARNUNG
`KOH_FALL5_MISCHLAGE_SPERRE`, alter Hinweis `KOH_FALL5_MISCHLAGE` gestrichen; § 53-Seite zählt nur Anlagen mit Stromerzeugung), E7c2/5
4354b009 B‑4 Rest (`PROZENT_BRENNSTOFFKOSTEN`/`PROZENT_STROMKOSTEN` frisch aus dem jüngsten Lauf, Bezugsgröße Arbeitskosten = Menge ×
Arbeitspreis), E7c2/6 2f0fc21f V‑1/V‑2 (EV-Mix ungerundet, Erlös auf Cent; § 51a bei fester Vergütung mit der Einspeisevergütung, in
der Direktvermarktung mit dem anzulegenden Wert; `PvErloesRechnerEegTests` neu gepinnt), E7c2/7 a339a633 E7c1‑Q2 b (Vbh in Fall 2 aus dem
KWK-Strom ÷ P_el, auch Ersatzweg), E7c2/8 3b6f54fe E7c1‑Q1 (Rundungsgrund bei Kürzungen < 0,01 MWh), E7c2/9a dcc875ac (KI-Feldkatalog
`anlage_abwaermeabfuhr`/`anlage_stromkennzahl`; Word/Excel-Modultafel bei Fall 2 fünf Spalten mehr), E7c2/9b ed4b3395 (Rest der
Überlagerung „Sätze und Herkunft" U22: Anlagenart, Tatbestand, Satztafel, „Wirkung Jahr 1", Energie- und Stromsteuer, Knopf „Wahl und
Herkunft…"; Kern `KwkgJahresbetrag`), E7c2/9c bd866b4c Ankertests, E7c2/10 af450ed6; Zielversion 110; weitere Commits (Nachzug
13fff671 mit Schritt 106) laut Startnachricht. A/B: 13 Basisprojekte nach jedem Punkt 9.195/9.195 Werte gleich (Anker 1024
−2.896.359,13 €, 1030 −21.895.377,28 €); Proben laut Berichten (E an 1024, F, G, S‑2 an 1030: § 54 7.987,41 → 0, KW −20.388.846,98 →
−20.507.679,50, U7-Probe 24.088,43 → 21.202,71; B‑4 an 1030; V‑1/V‑2 an 1040/1045/1046 und Rechner 100 kWp; Q2 b an 1030 σ 0,5: Vbh
7.475,69 → 6.055,2 h/a, KWKG Jahr 1 6.137,94 → 7.316,03 €, ohne Deckel Jahr 5 1.057,55 → 12.458,43 €). Schlüssel: 72 + 8 ERK_* + 6
weitere neu, 1 gestrichen (Liste in den Berichten). Fragen E7c2‑Q1…Q8 entschieden (Tabelle in `e7c2_entscheide.md`); für E7c3
zurückgestellt: Q5 b (`VpvCtKwh` ungerundet), Q8 b (Energiesteuer-Vorschau je Wahl), B‑6 Robustheit, Kapitalwert 1024, Rest von § 6.3
Nr. 9h (geräteeigene Dauerspalten, Speicherflotte), E7c1‑Q8 Katalogzeilen, Hi = Hs = 1,0 für Brennstoff 24. Logbuchsätze: die neun aus
dem Phase‑1-Bericht (Satz 3 ergänzt um „Sonstige in kWh"). Abnahme am Gerät A‑E7c2‑1: Positionsdialog mit Ersatz/Restwert-Feldern,
Energieträger-Karte Preisbasis bleibt nach Wiederöffnen, Brennstoffkatalog Nm³/kWh, Kohärenz-Warnung bei Mischlage, Prozentzeilen
der Betriebskosten, PV-Vergütungsdialog § 51a, BHKW-Dialog Überlagerung vollständig, Word-Tafel mit 16 Spalten bei Fall 2 (Umbruch
prüfen).

AUFGABEN: (1) Statusdatei: Zeile #446 (Anlass: Anwender „fahre fort"/„fahre fort auf diesem account", Etappe E7 Teil c2) nach der
letzten Zeile vor `---` (hinter #445) und Block Nach #446 VOR dem jüngsten Nach-Block: (a) Entscheide E7c2‑Q1…Q8 mit Lesarten
(entschieden 23.09.2026), (b) Abnahme am Gerät, (c) A/B-Tafeln, (d) Befunde (Hi = Hs = 0 bei Brennstoff 24, Word-Tafel 16 Spalten,
„leer = Vorschlag"-Regel der Überlagerung, Lücke 106/107 bis zum Nachzug, danach 108–110), (e) Papiernachzug, (f) Logbuch, (g) nächste Etappe E7c3 dann E8,
(h) Nachweis: Gate NACHTRAG-446-GATE, CI-Nachweise zu ac6e65ee (Kern ios 35850016737, Kern main 35850035400, Windows main 35850035254
grün — auch in Nach #439/#440 (h) nachtragen), Push nach Regel ohne Rückfrage. (2) Protokoll
`Dokumentation/ueberholt/Protokolle/Reporting/E7c2_Schritte_EFG_S2_V_B4_Protokoll.md` (Muster E7c1), Index Reporting 119 → 120
(vorher zählen — Nachbarn haben eigene Rubriken). (3) Register: A3, A4, A6, A9, U‑1, ET‑D‑3 gebaut #446 mit erledigt-Grund; R‑E7c1 Q1/Q2/Q7
umgesetzt #446; neue Familie R‑E7c2 mit Q1…Q8 (Wortlaut der Entscheide, Q4 abweichend); Konzept: § 2.5 (Preisbasis-Spalte), § 2.13 (3)
(Ersatz/Restwert je Position), § 3.5/§ 3.6 (V‑1/V‑2, Q2 b, Rundungshinweis), § 3.9 (Sperre S‑2 als Warnung), § 4 (S‑2, V‑1/V‑2, B‑4 erledigt),
§ 5 (U‑1 Nm³/kWh), § 6 Schrittvergabe (106 Dialog Design, 107 Gebäudesimulation E30, 108 E, 109 F, 110 G; nächster freier 111), § 6.1 Zeile E7c2, § 6.3 Nr. 9h
teilweise, § 7 und Anhang (E7c3-Umfang), Kopfzeile Codestand/Schemastand 110; Entscheidwege-Protokoll (Wortlaut der erledigten Punkte);
Analysepapier § 5 „Teil c2 umgesetzt #446", § 6 Tafel (E/F/G gebaut mit Nummern); Rechenwege 04 (Preisbasis, Nm³/kWh, B‑4), 05 (Q2 b,
Rundungshinweis, S‑2), 06/PV (V‑1/V‑2), 07 falls berührt; Szenarienkonzept-Kopf. (4) Mockup: Schlüsseltafeln (neue/gestrichene Schlüssel
je Kategorie), Anhang U22 „erledigt #446", U32 „erledigt #446", U39 „teilweise #446" mit Grund, Stand-Absatz. (5) Logbuch (#446, neun
Sätze), Wiki-Quellen Wirtschaftlichkeit/Kosten (Ist-Zustand; Tabu-Regex 0). (6) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.
