# Auftrag Papiere #461 — E9a: Vollständige Szenarioabdeckung, Teil a (Schemaschritte 116–118, Kern liest die Paare, SzenarioParameterTests, A/B-Nachweis) (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen, CI-Nachweise und Testdatenbank-Stand nennt die Startnachricht (Platzhalter NACHTRAG-461-MERGE / NACHTRAG-461-GATE /
NACHTRAG-461-CI / NACHTRAG-461-TESTDB). Muster: `E8c_Papiere_460_Auftrag_2026-09-24.md`, Statuszeile #460 und Block Nach #460. Vor dem
Schreiben `grep -n "#45[0-9]\|#46[0-9]"` in der Statusdatei (#458 Stufe 2 und #460 sind gepusht, #453 Zapfprofil Z3 mit Schritt 115 laut
Startnachricht; Referenzbasis R13_Kuehlung; Schemastand nach #461 = 118).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #461 (Etappe E9, Teil a — V‑E des konsolidierten Konzepts; die einzige rechenwirksame Etappe neben E7) für
EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test. ARBEITSORT: Der Worktree `.claude/worktrees/papiere461` (Zweig
`papiere461` ab NACHTRAG-461-MERGE) existiert; von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere461`, nie im
Hauptbaum arbeiten. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein
Merge, kein Stash. Formregeln wie #460 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0
Treffer; Logbuch Version 1.2.0.4, Wiki-Stichwort `szenarien` und `wirtschaftlichkeit` — die zwei Logbuchsätze aus dem E9a-Bericht erst mit
E9b veröffentlichen: hier nur als Entwurf im Protokoll, im Logbuch ein Satz zum Kern ohne Dialogversprechen).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e9a_berichte.md` (Phase 1: Commits, Bau je Punkt, Stellen je Größe,
Lücke 115, Testdatenbank, Befunde 1–7, Fragen E9a‑Q1…Q7 mit Lesarten und Empfehlung, erledigt-Gründe, Logbuch-Entwürfe; Phase 2:
Merge, Tests, Referenzlauf, Zellvergleich, A/B-Tafeln mit Erläuterung, Befunde), `e9a_ab_tafel.md`, `e9_fakten.md` (Quellen: Analysepapier
§ 5 Zeile E9 und § 6 Schritte B/C/D, Konzept § 2.11.4 V‑E, § 2.11.5 Tafel, § 2.11.7, Szenarienkonzept § 2/§ 4/§ 11.1, Register A5, V‑4,
V‑G2, V‑G5, Mockup U15/U27, Statusdatei Nach #455 (g), Tabellenspalten). Alles ganz lesen. Zweig `e9` von 46023235: E9a/1 7f481f6a Schritt
116 = B `SCHRITT_116_SZENARIO_RAHMEN` (`Szen_Best/Worst_Zeitraum` ganze Jahre, `Szen_Best/Worst_Menge` %, an `Tab_ProjektWirtschaftlichkeit`,
Doppelpflicht CREATE + `SpalteSicher`), E9a/2 12d3f0a6 Schritt 117 = C `SCHRITT_117_TRAEGERPREIS_SZENARIO` (`custom_price_work/base/power_best/_worst`
an `energy_project_settings`, keine Doppelpflicht — der Kern legt die Tabelle nirgends selbst an; `VariantenCtrl` kopiert die Spalten beim
Anlegen einer Variante), E9a/3 ac7f3b90 Schritt 118 = D `SCHRITT_118_ERLOESSATZ_SZENARIO` (`Einspeiseverguetung(_KWK)_Best/_Worst` an der
Wirtschaftlichkeit, `DvEntgelt/PpaPreis_Best/_Worst` an `Tab_ProjektPhotovoltaik`, Doppelpflicht PPV; nicht: `PpaSpotAufschlag`,
Marktwertfelder = Q1 a), Zielversion 118; E9a/4 399a5a18 Testdatenbank (LFS 8a3bebaf → 64383984, 67.756.032 Byte, Stand 118, 18 leere
Spalten; Endstand laut NACHTRAG-461-TESTDB nach dem Nachzug mit Schritt 115 des Zapfprofils); E9a/5 1626b5ff Daten (`SzenarioSatz` um
`Zeitraum`, `Menge`, `Einspeiseverguetung`, `EinspeiseverguetungKwk`; Trägerkarte sechs, PV-Karte vier Felder; leer oder 0 = wie Erwartet,
gepflegt = |Wert − Erwartet| > 1e−9, keine Vorgaben = Q5 a); E9a/6 d2ef1862 Kern liest die Paare — Zeitraum in
`WirtschaftlichkeitParameter.FuerSzenario` (Horizont, Restwert, Ersatz, PV-, KWKG-, CO₂-Reihen; Verlauf über
`BerechneVerlaufSzenarienJeZeitraum` = Q4 a), Menge in `WirtschaftlichkeitCtrl.Szenariodaten`/`SzenarioMengen.Variante` und
`EndenergieAufloeser` (alle kWh-Mengen, Stundenreihen und Bezugsspitze; Leistungen, Prozente, Gerätedaten, Festbeträge, gesetzliche Sätze
nicht; Deckel nach dem Faktor = Q2 a), Trägerpreise in `TraegerpreisSzenario.Wirksam`, angewandt in `KostenEmissionRechner.LadeTraeger` und
`StromArbeitspreisEurJeKwh` (Preis als Ganzes ersetzt, Staffel/Saisonreihe gilt weiter mit Kohärenzzeile = Q3 a), Einspeisevergütung PV/KWK
in `FuerSzenario`, DV-Entgelt/PPA in `ProjektPhotovoltaikCtrl.FuerSzenario`; Rollenmodell: Rollenpreise bleiben in allen Szenarien
Erwartet, Kohärenzzeile, ebenso flache Einspeisevergütung neben aktivem PV-Vergütungsdialog = Q7 a; E9a/7 a066b2fd Berichte (Nachweiszeile je
Szenario nennt T und Einspeisevergütung immer, Menge und Trägerpreise nur gepflegt; Formelmappe Stufe 0 Parameterblock 15 statt 12 Zeilen
plus je gepflegtem Trägerpreis eine Zeile; Verlauf und Zahlungsgliederung je Szenario über T_s; `WIRT_SZEN_HINWEIS` bleibt bis E9b;
Anhang-E-Checkliste unverändert); E9a/8 0da92072 Tests (22 Fälle `SzenarioParameterTests`: Nullsemantik, 1e−9-Regel, Wirkungsrichtung,
Speicherweg über drei Controller, 1030 Zeitraum = Lauf mit diesem T, Menge ±10 % symmetrisch, Staffel-Kohärenz, Rollenmodell,
Werkzeug-Wache, PV/KWK-Prüfstand; `BerichtBlattstrukturWacheTests` Stufe 0 angepasst); Merge bdd06e6e (origin a1df2dbe, resx 8.770 je
Sprache); 17 Schlüssel `WIRT_SZ_*` (8), `WIRT_FM_PARAM_*` (6), `WIRT_ANN_*` (3). Phase 2: 12.177/0/1 (Kern 5.503, UI 5.737, KiKern 524,
SpeicherEngine 386, SpeicherPlanung 27/1), gefiltert 52/52, Referenzlauf 13/13 gegen R13 (387 CSV byte-gleich), Zellvergleich 15 Gruppen (3
Zeilen eingeschoben, 2 Textzellen je Gruppe, Wertfassung = Formelfassung), A/B-Tafel neun Größen (1030 Zeitraum/Menge/Arbeits-/Grund-/
Leistungspreis/Einspeisevergütung KWK, 1040 mit Ausgangslage Einspeisevergütung PV/DV-Entgelt/PPA; Erwartet bitgleich, Zurücksetzen exakt).
Befunde: Formelmappe Stufe 1/2 rechnet nur Erwartet (T_s im Parameter- und Verlaufsblock); Szenariopreis ohne Erwartet-Preis → E9b kennzeichnet;
Menge × Ertragsänderung multiplizieren sich (+10 % · +10 % = +21 %); 0 im Wirtschaftlichkeits-Speicherweg als 0, gelesen als leer;
Zeitreihen auch bei Szenario-Leistungspreis; „Günstig = länger" bei kostendominierten Projekten nicht günstig; Vorgaben unsortiert
(W5‑B‑9: Günstig unter Erwartet durch niedrigeren Zins); Testdatenbank ohne PV-Projekt mit vollständigen Preisen; Lücke 115: mechanisch
verkraftet (Migration überspringt Schritte ≤ Stand), Regel „lückenlos" → Merge erst nach Z3. Fragen an den Anwender E9a‑Q1…Q7 (offen,
gebaut jeweils a; Q6 Empfehlung a: Risiko V‑G7 und n-jährliche Zeitpunkte V‑G3 nicht Teil von E9). Abnahme am Gerät A‑E9a‑1: ohne Dialog —
SQL-Pflege auf einer Kopie (z. B. `Szen_Worst_Zeitraum` = 15 für 1030) und Ergebnisseite/Wortbericht: Nachweiszeile Ungünstig nennt „T =
15 a", Kapitalwert Ungünstig ändert sich, Erwartet nicht; Formelmappe Parameterblock 15 Zeilen.

NEBENBEFUNDE: (1) Szenarienkonzept-Kopfzeile nennt „Zielversion 113, neue ab 114" → 118 und E9a; (2) Linux-Messlatte der ChartProben 91
Zeilen (Windows 146) — Nachtrag beim nächsten Linux-Lauf; (3) CI-Nachweise: 3986c7ae main Kern 35938295339 / Windows 35938295404 grün, ios
abgebrochen (überholt durch a1df2dbe 35938837705 grün); NACHTRAG-461-CI; (4) Regelverletzung 02:50: 3-s-Testlauf parallel zu einem
z3-testhost (gemeldet).

AUFGABEN: (1) Statusdatei: Zeile #461 (Anlass: Anwender „Fahre fort", Etappe E9 Teil a — V‑E, Schemaschritte 116–118) nach der letzten
Zeile vor `---` und Block Nach #461 als neuester Block: (a) Fragen E9a‑Q1…Q7 mit Lesarten/Empfehlung/Entscheidstand (offen), (b) Abnahme,
(c) A/B-Tafel (gekürzt: je Größe Projekt, Pflege, KW Günstig/Ungünstig vorher → nachher, Erwartet unverändert), Referenzlauf, Zellvergleich,
(d) Befunde und Nebenbefunde, Lücke 115 und Schrittvergabe (114 Kühlung, 115 Zapfprofil Z3, 116–118 E9a), Testdatenbank-Stand, (e)
Papiernachzug, (f) Logbuch, (g) nächste Etappe E9b (Dialoge, ±-Knopf an drei Orten, Zeilen 8/9, Hinweistext entfällt, Ausweis „n von m";
Statusnummer #462; Auftrag `E9b_Auftrag_2026-09-24.md`), (h) Nachweis: Gate NACHTRAG-461-GATE, Push nach Regel. (2) Protokoll
`Dokumentation/ueberholt/Protokolle/Reporting/E9a_Szenarioabdeckung_Kern_Protokoll.md` (Muster E8b: Anlass, Bau je Punkt, Stellen je
Größe, A/B-Tafel vollständig, Befunde, Fragen, Abnahme), Index Reporting zählen (+1). (3) Register: V‑G5 „teilweise gebaut #461 (Kern);
Dialog E9b", V‑4 (Hinweistext bleibt bis E9b), A5 (ohne Degradation gebaut), neue Familie R‑E9a mit Q1…Q7 (offen, Empfehlung a), R‑E9
Vermerk zu V‑G7/V‑G3 (Q6); Konzept: § 2.11.5 Tafel Spalte „Stand" (Trägerpreise/Erlössätze/Rahmen-Zeitraum/Mengen „Kern gebaut #461,
Spalten 116–118"), Regeln ergänzt (Mengenfaktor-Definition, Trägerpreis-Ersatz, Rollenmodell, Zeitraum je Szenario, keine Vorgaben),
§ 2.11.4 V‑E „Teil a gebaut #461", § 2.11.7 (Hinweistext bis E9b), § 6 Schemaschritte 116–118 mit Tabellen und Spalten, § 7 (E9a gebaut,
E9b nächste), Kopfzeile Codestand und Schemastand 118; Szenarienkonzept Kopfzeile und § 11.1 (V‑E Teil a gebaut); Analysepapier § 5 „E9
Teil a #461", § 6 Schritte B/C/D „= 116/117/118 gebaut #461"; Entscheidwege-Protokoll. (4) Mockup: U15 „teilweise #461 (Kern), Dialog
E9b", Zone Szenarien/Annahmentafel (Nachweiszeile je Szenario, Parameterblock 15 Zeilen), Ressourcentafel (+17 Schlüssel, Stand 8.770),
Stand-Absatz. (5) Logbuch (#461, ein Satz zum Kern, Stichwort `szenarien`), Wiki-Quelle Szenarien/Wirtschaftlichkeit (Ist-Zustand: welche
Größen je Szenario der Kern liest, NULL = wie Erwartet, Pflege bis E9b nur über die Datenbank; Tabu-Regex 0). (6) Bytes, `<tr`,
`git diff --stat`, Bericht ohne Dateiabzüge.
