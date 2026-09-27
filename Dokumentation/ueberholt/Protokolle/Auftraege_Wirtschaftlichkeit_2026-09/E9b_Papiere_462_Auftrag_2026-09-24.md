# Auftrag Papiere #462 — E9b: Szenariopflege in den Dialogen (±-Knopf, Szenariotafel Zeilen 8/9), Hinweistext entfällt, Ausweis „n von m Parametern szenariert" — E9 abgeschlossen (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen und CI-Nachweise nennt die Startnachricht (Platzhalter NACHTRAG-462-MERGE / NACHTRAG-462-GATE / NACHTRAG-462-CI).
Muster: `E9a_Papiere_461_Auftrag_2026-09-24.md`, Statuszeile #461 und Block Nach #461. Vor dem Schreiben `grep -n "#45[0-9]\|#46[0-9]"` in
der Statusdatei (#458 Stufe 3a/3b von Dialog Design stehen vor #461; Referenzbasis R13_Kuehlung; Schemastand 118, kein neuer Schritt).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #462 (Etappe E9, Teil b — damit ist V‑E und E9 abgeschlossen) für EPOS-Plan. Antworten auf Deutsch. Nur
Papiere, kein Build, kein Test. ARBEITSORT: Der Worktree `.claude/worktrees/papiere462` (Zweig `papiere462` ab NACHTRAG-462-MERGE)
existiert; von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere462`, nie im Hauptbaum arbeiten. Commits sofort
mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie
#461 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version
1.2.0.4, Wiki-Stichwörter `szenarien` und `wirtschaftlichkeit`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e9b_berichte.md` (Phase 1: Commits, Bau je Punkt, Schlüssel, Maskenwache,
Formularkarte, Abweichungen 1–6, Fragen E9b‑Q1…Q5, erledigt-Gründe, Abnahme A‑E9‑1 in acht Schritten, fünf Logbuchsätze — darunter die
zwei aus E9a zurückgestellten —, Hinweise für Papiere und Merge; Phase 2: Merge, Tests, Referenzlauf), `e9a_berichte.md` (Kernseite,
Befunde), `e9_fakten.md`. Alles ganz lesen. Zweig `e9b` von 0d296ca0: E9b/1 8e3e9357 (Szenariotafel Zeilen 8 Betrachtungszeitraum —
Ganzzahlfeld 1–50, Erwartet-Spalte T — und 9 Mengenänderung — Erwartet „0 %"; „Vorgaben" leert alle 18 Felder, die Einspeisevergütungen
bleiben; KI-Sicht +4 Felder `best_/worst_zeitraum`, `best_/worst_menge`; Maskenwache Parameterdialog 26 → 30 im Block „ETAPPE E9b"),
E9b/2 6a7f738e (±-Knopf an den Trägerpreisen: `CaseEingabeDialog` als allgemeiner Baustein mit gemeinsamem Parametersatz, neuer
Knopf-Baustein `SzenarioKnopf`; Kostenposition unverändert, `CaseEingabeDialogTests` unverändert grün; Trägerkarte im Projekt je Preis
Arbeit/Grund/Leistung, Kennzeichen ● bei gepflegtem Paar, Umschalten €/kWh zieht die Szenariowerte mit; Kohärenzzeile „ohne Wirkung" bei
Staffel/Saisonreihe; ⚠ ohne Erwartet-Preis, gespeichert wird trotzdem; im Katalog keine Knöpfe; Hilfekennung
`Form_WirtschaftlichkeitSzenariowerte.btn_Help` → `Wirtschaftlichkeit#szenariowerte`; kein neues Prüfmuster — Prüfmuster sind eingefrorene
WinForms-Vorgänger, Ersatz: Test „jeder Schlüssel des Parametersatzes trifft einen [Parameter]"), E9b/3 5841cd7c (± Einspeisevergütung PV im
Parameterdialog, ± Einspeisevergütung KWK im Dialog „BHKW-Wirtschaftlichkeit" — dort wird der Erwartet-Wert seit #325 gepflegt, geschrieben
mit OK des BHKW-Dialogs —, ± DV-Entgelt nur bei Marktprämie, ± PPA-Preis nur bei sonstiger Direktvermarktung im PV-Dialog; Kohärenzzeile
bei aktivem Rollenmodell/PV-Vergütungsdialog), E9b/4 8ed0f1a5 (`WIRT_SZEN_HINWEIS` entfernt aus Ressourcen, Seite, Wort- und Excelbericht;
Ausweis „n von m Parametern szenariert: …" unter der Annahmentafel, in Block 4, in Wort- und Excelbericht und in Punkt 9 der
Anhang-E-Checkliste; Zählregel im Kern `SzenarioAbdeckung`: m = 7 Tafelgrößen + Zeitraum + Menge + Einspeisevergütung PV und KWK + DV-Entgelt
und PPA-Preis je Vergütungszeile mit PV-Anlage + je Träger mit Verbrauch Arbeits- und Grundpreis, Leistungspreis beim Stromträger immer,
sonst nur wo gepflegt; n = gepflegt nach der 1e−9-Regel — die Vorgaben der sieben Tafelgrößen zählen nicht; U10-Kommentar nachgezogen),
E9b/5 a4bcb41e (CSS-Klasse `epos-szenarioleiste` statt `epos-leiste` — Fußleistenwache und BHKW-Tests; Assistent lehnt Nutzungsdauer,
Startjahr, Zuschuss im Szenariopaar mit Grund `KI_DLG_CSE_NUR_KOSTEN` ab), E9b/6 6d0fef5f (rund 70 Tests: bUnit `SzenarioKnopfTests` 7,
`CaseEingabeSzenariopaarTests` 15, `EnergietraegerSzenarioTests` 10, Ergänzungen Parameter-/BHKW-/PV-Dialog; Kern `SzenarioAbdeckungTests`
15 mit „Hinweistext weg, Ausweis da" und Zählung an 1030, `EnergietraegerSzenarioHuelleTests` 6 in Nm³ und kWh); Merge und Phase‑2-Zahlen
laut Startnachricht. Schlüssel: 38 neu (`WPAR_SZ_ZEITRAUM`, `WPAR_SZ_MENGE`, `KI_DLG_WPA_SZ_*`, `KI_DLG_CSE_*`, 15 × `SZP_*` plus
`SZP_PV_INAKTIV`, `SZP_PV_DV_OHNE_WIRKUNG`, `SZP_PV_PPA_OHNE_WIRKUNG`, `ETV_SZ_TITEL/_HINWEIS/_OHNE_ERWARTET/_SPEICHERFEHLER`,
`WIRT_ANN_DV_ENTGELT`, `WIRT_ANN_PPA_PREIS`, `WIRT_SZ_ABDECKUNG`, `WIRT_SZ_ABDECKUNG_LISTE`, `WIRT_AE_9_ABDECKUNG`), 1 entfallen
(`WIRT_SZEN_HINWEIS`), 2 geändert (`WPAR_SZ_HINWEIS`, `WIRT_AE_9_TEILWEISE`); Stand je Sprache laut Startnachricht. Abweichung 6: die
„Rahmen-Gruppe" (Konzept § 2.11.5) wird über die Szenariotafel gepflegt (Zeilen 1–4 und 8), nicht über einen eigenen ±-Knopf — Konzept
nachziehen. Fragen an den Anwender: E9b‑Q1 Bauform (a verallgemeinerter CaseEingabeDialog — gebaut, Empfehlung; b eigene Dialoge); E9b‑Q2
Zählregel m (a wie oben — gebaut, Empfehlung; b nur projektweite Größen m = 11; Lesarten: n zählt nur gepflegte Werte, „0 von m" ohne
Pflege obwohl die Vorgaben der Tafel die Szenarien verschieben; Einspeisevergütung KWK zählt auch ohne BHKW); E9b‑Q3 Ort des Ausweises (a
unter der Annahmentafel, Block 4, Berichte, Checkliste Punkt 9 — gebaut, Empfehlung; b Kopf der Ergebnisseite); E9b‑Q4 Szenariopreis ohne
Erwartet-Preis (a Warnzeichen, Kohärenzzeile, Banner, speichern — gebaut, Empfehlung; b verweigern; Grundpreis und Erlössätze warnen nie, 0
gültig); E9b‑Q5 Checkliste Punkt 9 (a „teilweise" — gebaut; b „erfüllt", sobald beide Szenarien gerechnet — Empfehlung; c „erfüllt" nur mit
mindestens einer Pflege). Abnahme am Gerät A‑E9‑1: die acht Schritte aus dem Bericht wörtlich übernehmen. Logbuch: die fünf Sätze aus dem
Bericht (zwei `szenarien` aus E9a, zwei `szenarien` E9b, ein `wirtschaftlichkeit` E9b).

NEBENBEFUNDE: (1) CI-Nachweise #461 (0d296ca0): ios Kern 35945134055, main Kern 35945142392, main Windows 35945142208 — alle grün; als
Nachtrag in Nach #461 (h); dazu NACHTRAG-462-CI; (2) Wiki `Programm Dokumentation - Wirtschaftlichkeit.wiki` veraltet: Z. 50 („Ein
Eingabefeld dafür führt keiner der Dialoge"), Anker `szenariohinweis` (entfallener Hinweistext — Anker mit Verweis auf den Ausweis
behalten oder umbenennen, Tabu-Regex beachten), Beschreibung des Parameterdialogs (sieben Zeilen → neun), Trägerkarte/PV-/BHKW-Dialog
mit ±-Knöpfen; (3) Linux-Messlatte der ChartProben 91 Zeilen (Windows 146) — Nachtrag beim nächsten Linux-Lauf; (4) Testdatenbank ohne
PV-Projekt mit vollständigen Preisen (Datenaufgabe, aus E9a).

AUFGABEN: (1) Statusdatei: Zeile #462 (Anlass: Anwender „Fahre fort", Etappe E9 Teil b — V‑E; E9 abgeschlossen) nach der letzten Zeile
vor `---` und Block Nach #462 als neuester Block: (a) Fragen E9b‑Q1…Q5 mit Lesarten/Empfehlung (offen beim Anwender; E9a‑Q1…Q7 weiter
offen), (b) Abnahme A‑E9‑1 (acht Schritte), (c) Nachweis „keine Rechenwirkung ohne Pflege" (Anker, Referenzlauf 13/13), Maskenwache-Stand,
(d) Befunde (Abweichungen 1–6, Zählregel-Lesarten, Nebenbefunde 1–4), (e) Papiernachzug, (f) Logbuch, (g) nächste Etappe: E10 laut
Analysepapier § 5 (Inhalt dort nachlesen und mit Statusnummer #463 und Schemaschritt-Vergabe ab 119 nennen; prüfen, was E10/E11/E12 sind —
E11 entfällt laut Konzept-Anhang), (h) Nachweis: Gate NACHTRAG-462-GATE, Push nach Regel. (2) Protokoll
`Dokumentation/ueberholt/Protokolle/Reporting/E9b_Szenarioabdeckung_Dialoge_Protokoll.md` (Muster E9a), Index Reporting zählen (+1). (3)
Register: V‑G5 „gebaut #461/#462", V‑4 erledigt (Hinweistext entfallen #462), U10/U15 erledigt, Familie R‑E9b mit Q1…Q5 (offen, Empfehlung
a/a/a/a/b), Q18/A-Zeilen prüfen; Konzept: § 2.11.5 (Tafel Spalte „Stand" alle Zeilen gebaut #461/#462; Regel „Pflege": Szenariotafel für
Rahmen und Zeitraum/Menge, ±-Knopf an Trägerpreisen und Erlössätzen — Einspeisevergütung KWK im BHKW-Dialog; Ausweis-Regel mit Zählregel
und Lesart), § 2.11.7 (Hinweistext entfallen #462, Absatz als Rückschau), § 2.11.4 V‑E „gebaut #461/#462", § 6/§ 7 (E9 abgeschlossen, E10
nächste), Kopfzeile Codestand; Szenarienkonzept § 4 (Dialog: neun Zeilen, Vorgaben 18 Felder, ±-Knöpfe), § 11.1 (V‑E gebaut);
Analysepapier § 5 „E9 umgesetzt #461/#462 — E9 abgeschlossen", § 3 Befund P5 erledigt; Entscheidwege-Protokoll. (4) Mockup: U15 „erledigt
#462", U10 „entfallen #462", Zone Szenarien/Annahmentafel (Ausweis „n von m", Zeilen 8/9, ±-Knöpfe an drei Orten), Ressourcentafel (+38,
−1, 2 geändert; Stand laut Startnachricht), Stand-Absatz. (5) Logbuch (#462, fünf Sätze), Wiki-Quelle Wirtschaftlichkeit (Nebenbefund 2
vollständig; Tabu-Regex 0). (6) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.
