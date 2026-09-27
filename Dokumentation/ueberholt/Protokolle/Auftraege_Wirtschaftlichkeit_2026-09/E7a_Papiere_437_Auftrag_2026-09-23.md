# Auftrag Papiere #437 — E7a rechenwirksame Lücken, Teil a (gesichert 23.09.2026)

Merge-SHA und Gate-Zahlen nennt die Startnachricht (Platzhalter NACHTRAG-437-MERGE / NACHTRAG-437-GATE). Muster: Auftrag #434/#436
(`E5_Papiere_434_Auftrag_2026-09-22.md`, `E6_Papiere_436_Auftrag_2026-09-23.md`), Statuszeilen #434–#436 und Nach-Blöcke.

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #437 (Etappe E7, Teil a) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test.
ARBEITSORT: von der Repowurzel `C:\Waermeplan\EPOS-Plan`: `git worktree add .claude/worktrees/papiere437 -b papiere437 ios_migration_september`
(nach dem Merge von e7), `cd .claude/worktrees/papiere437`; Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`;
kein Push, kein Merge. Formregeln wie #436 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Tabu-Regex für Wiki).

FAKTEN E7a (aus den Agentenberichten): Zweig `e7` von b4d468a4: E7/1 `57ba5097` Nr. 29 — `SteuerGutschriftRechner.Co2JeEnergieertrag`
prüft den CO₂-Grenzwert 270 g/kWh brennwertbezogen (Erdgas Ho-Faktor 181,4 g/kWh aus dem Katalog, andere Brennstoffe Umrechnung
über H_i/H_s des Trägers, ohne gepflegten Brennwert bleibt der Hi-Faktor mit Begründung; Herleitungszeile je Anlage; Probe 1024 mit
Hocheffizienz/räumlichem Zusammenhang: 278,6 → 262,2 g/kWh, Befreiung 0 → 1.680,07 €/a im Ausweis); E7/2 `e4ce4f35` Schemaschritt 101
(DML: leere `KWKG_Anlagenart` → NULL, Zielversion 101; sieben Anlagen 12310, 14819, 14842, 14843, 14844, 14851, 14852 in 1032/1043,
keine ein BHKW); E7/3 `d1cef396` Dialog „(bitte wählen)" (`BHW_W_ART_LEER`); E7/4 `212cb5b5` Nr. 32 — `StromMatrix` Bedarf und Lastbild
vor Abzug der PV-Eigennutzung (`PvEigenGesamtMWh`), KWK-Eigenanteil unverändert min(BHKW, Bedarf nach PV), Anker 293.245,6 + 22.914,0 =
316.159,6 €/a über den Kernweg; E7/5 `8c834746` Anker-Kommentare; E7/6 `1d98c8b1` Testdatenbank auf Schemastand 101 (LFS); E7/7
`8fce3cb2` beide Verteilschlüssel brutto (`WIRT_ERL_B1_NAEHERUNG` „verteilt nach dem Eigenverbrauch je Anlage"); E7/8 `61ebd027` im
Rollentarif ersetzt der PV-Anteil die Zeile „PV: vermiedener Bezug" (Flat bleibt); E7/9 LIESMICH-Nachtrag Referenzläufe (Schemastand
101). Ressourcen: neu `STEUER_STROMST_CO2_FAKTOR_HO`, `_FAKTOR_UMGERECHNET`, `_FAKTOR_HI`, `_HERLEITUNG`, `_HEIZWERT`; geändert
`STEUER_STROMST_CO2`, `BHW_W_ART_LEER`, `WIRT_MATRIX_BEDARF_HINWEIS`, `WIRT_ERL_B1_NAEHERUNG`. A/B: alle dreizehn Basisprojekte
wirtschaftlich unverändert (kein Rollentarif im Bestand, `Tab_ProjektTarif` leer; CO₂-Prüfung greift nirgends, weil Hocheffizienz und
räumlicher Zusammenhang überall 0); Strommatrix-Bedarf (gespeicherte Spalte `Tab_ErgebnisStromMatrix.Bedarf`, Matrixtafel in Wort- und
Excelbericht) ändert sich in 1007/1046 (19,10 → 24,00 MWh, Spitze 6,60 → 6,62 kW), 1040 (5,40 → 8,00), 1045 (5,94 → 8,00, Spitze
3,7235 → 3,7215 kW: Nachtaufnahme des Wechselrichters 9,3 kWh/a); Referenzlauf 13/13 gegen R11, 357 CSV byte-gleich (Export liest die
Matrix nicht) — Basis bleibt R11. Kein Nachziehlauf (Nr. 31). **Zurückgestellt (Anwender gefragt, Register):** Nr. 30 Kern-Regel und
Kohärenzzeile (Lesart a wörtlich / b nur bei abgeleitetem Kontingent; Empfehlung b, dazu Anlagenart des 1030-BHKW pflegen), K‑1
(fünf Teilfragen; Schemaschritt jetzt 103, weil 102 an die Zapfprofil-Sitzung Z0 ging), A20 (Lesart a kein Zuschlag nach 2030 / b 2030
= Ende der Inbetriebnahmefrist; Empfehlung b). Nebenbefunde: Strombedarf der Simulation enthält den Wärmepumpenstrom nicht (1039:
Bedarf 60,0, Netzbezug 119,2 MWh — im Rollentarif würde die vermiedene Menge negativ); `KWKG_Eigenstromfall = ''` bleibt stehen;
Code-Kommentar an `SPALTE_EA_KWKG_ANLAGENART` („ohne Rechenwirkung") veraltet; `EBEV_UMRECHNUNG_HO` ohne Leser. Logbuchsätze:
CO₂-Grenzwert brennwertbezogen mit Herleitung je Anlage; vermiedene Stromkosten im Rollentarif ohne jede Eigenerzeugung mit Anteil je
Anlage. Abnahme am Gerät A‑E7a‑1: Kohärenz/Herleitung der Stromsteuerbefreiung (Anlage mit Hocheffizienz und räumlichem Zusammenhang),
BHKW-Dialog Anlagenart „(bitte wählen)", Rollentarif-Projekt: Erlösrubrik Block B mit BHKW- und PV-Anteil ohne doppelte PV-Zeile,
Strommatrix-Tafel „ohne jede Eigenerzeugung".

AUFGABEN: (1) Statusdatei: Zeile #437 (Anlass: Anwender „Pushen und Weiter", Etappe E7 Teil a) und Block Nach #437 VOR Nach #436:
(a) die drei offenen Anwenderfragen mit Lesarten und Empfehlung, (b) Abnahme am Gerät, (c) A/B-Tafeln (Basisprojekte, Matrix,
Mockup-Beispiel), (d) Befunde (Schrittvergabe 101/102 mit Z0, Wärmepumpenstrom, Eigenstromfall, Kommentar, UMRECHNUNG_HO),
(e) Papiernachzug (Rechenwege 04 R11, 05 CO₂ 242,1 → 218,6 g/kWh und Ho-Faktor, 07 und Konzept § 3.6 „ohne jede Eigenerzeugung"),
(f) Logbuch (#437), (g) nächste Etappe E7b/E7c (nach Entscheiden; Q11 HT/NT), (h) Nachweis: Push auf Zuruf; dazu in Nach #436 (h) die
CI-Läufe zu b4d468a4 (Kern main 35808256505 grün, Windows 35808256402 grün, Kern Arbeitszweig 35808255630 überholt; Kern zu 57b15a7c
35807420180 grün, zu 4929d432 35808545906 grün). (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E7a_Rechenwirksame_Luecken_Protokoll.md`
(Muster E6), Index Reporting 116 → 117. (3) Register: R‑NR Nr. 29 gebaut #437, Nr. 32 gebaut #437, Nr. 30 teilweise (offen Kern-Regel,
Frage), K‑1 und A20 mit den Lesarten als offene Fragen (Familie E7‑Q1…Q3 anlegen); Konzept: § 3.5/3.7 Regel brennwertbezogen,
§ 3.6/§ 2.6 „ohne jede Eigenerzeugung" (Nr. 32 als Regel), § 6.1 Kurztafel E7a, § 6.3 Nr. 29/32 erledigt-Einzeiler, Nr. 30 teilweise,
Kopfzeile Codestand und Schemastand 101 (nächster freier Schritt 103, 102 bei Z0), Anhang § 7 Tafel; Rechenwege 04/05/07 nachziehen
(Zahlen aus den Fakten); Analysepapier § 5 E7 „Teil a umgesetzt #437", § 6 Schemaschritte (101 Nr. 30 statt K‑1, K‑1 = 103, Z0 = 102);
Szenarienkonzept unverändert. (4) Mockup: Schlüsseltafel Kategorie 5/7 um die neuen STEUER_STROMST_CO2_*-Schlüssel und den geänderten
Näherungstext; Anhang: keine U-Zeile betroffen außer Vermerk bei U6 (Verteilschlüssel brutto, #437). (5) Logbuch (#437), Wiki-Quelle
Wirtschaftlichkeit: Satz zur brennwertbezogenen CO₂-Prüfung und zur vermiedenen Menge (Ist-Zustand, Tabu-Regex 0). (6) Bytes, `<tr`,
`git diff --stat`, Bericht ohne Dateiabzüge.
