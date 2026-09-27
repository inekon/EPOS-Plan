# Auftrag Papiere #452 — E7c3: Vbh-Definition, B‑6, Kapitalwert 1024, Q5/Q8, Brennstoff 24, U22 (gesichert 23.09.2026)

Merge-SHA, Gate-Zahlen und Entscheidstand nennt die Startnachricht (Platzhalter NACHTRAG-452-MERGE / NACHTRAG-452-GATE /
NACHTRAG-452-ENTSCHEIDE). Muster: `E7c2_Papiere_446_Auftrag_2026-09-23.md`, Statuszeile #446 und Block Nach #446. Vor dem Schreiben
`grep -n "#452\|#452"` in der Statusdatei (#447–#449 Dialog Design, #450 Dialog Design Stufe 5 reserviert, #451 Zapfprofil Z2; Kühlung/Gebäudesimulation führen eigene Statusdateien).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #452 (Etappe E7, Teil c3 — Reste) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test.
ARBEITSORT: Der Worktree `.claude/worktrees/papiere452` (Zweig `papiere452` ab NACHTRAG-452-MERGE) existiert; von der Repowurzel
`C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere452`, nie im Hauptbaum arbeiten. Commits sofort mit `git add <pfad>`, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #446 (UTF-8 ohne BOM, CRLF,
byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version 1.2.0.4).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e7c3_berichte.md` (Phase‑1-Bericht mit allen Punkten, A/B, Schlüsseln, Fragen,
erledigt-Gründen, Logbuchsätzen; Phase‑2-Zahlen nennt die Startnachricht) und `e7c2_entscheide.md` (Nachtrag 17:10: **Vbh-Definition des
Anwenders** Vbh = W_a ÷ P_Nenn, erzeugte Arbeit brutto ÷ Nennleistung, in Fall 1 und Fall 2 gleich; E7c2/7 zurückgebaut, E7c1‑Q2 b präzisiert,
E7c2‑Q7 erledigt). Alles ganz lesen. Zweig `e7c3` von c0131b2b: E7c3/5 ba9d0b13 Vbh nach Definition (Rückbau E7c2/7; Probe 1030 σ 0,5
zurück auf Vbh 7.475,69 h/a, KWKG Jahr 1 6.137,94 €, Kapitalwert −21.904.948,06 €; Rückfall ohne Modul-Vbh mit Bruttostrom), E7c3/3
3e7bc8b4 Q5 b (`VpvCtKwh` ungerundeter EV-Mix: 100 kWp 6,03 → 6,032 ct/kWh, 750 kWp 5,52 → 5,518933), E7c3/6 92aa069a Brennstoff 24
Hi = Hs = 1,0 (Katalog-Generation 9 als Nachpflege, kein Schemaschritt), E7c3/7 f114499b `KWKG_REALISIERUNGSFRIST`/`KWKG_STICHTAG_DAUERBETRIEB`
Status ABGEKUENDIGT (vierter Status, nichts gelöscht), E7c3/2 46291409 Kapitalwert 1024 nachgerechnet (Abweichung −676.036,81 € gegen
Konzeptwert −2.220.322,32 € = Datenstand: −676.495,37 € aus Schemaschritt 83, der 11,746 ct/kWh Strompreisanteile in den Arbeitspreis
faltete (35,000 → 46,746 ct/kWh), +458,56 € aus der Übernahme Access → SQLite am 02.09.; mit 35 ct rechnet der Kern bitgleich
−2.219.863,76 €; Anker bleibt −2.896.359,13 €), E7c3/1a–1e d5f2d00a, a2863b31, 8820ec1e, 27e4cd7b, 38ca5bc5 B‑6 (100 leere `catch` in
`KohaerenzPruefung`, `EmissionsBilanzRechner`, `Emissionsquelle`, `GesetzKatalog`, `WirtschaftlichkeitCtrl` benannt; strenger Leseweg
`StilleDb.TabelleStreng`/`ScalarStreng` an 26 Lesestellen, weil `DataRepository` nie wirft; Warnzeile „Prüfung/Rechenstufe „X" nicht
ausführbar: <Grund>", Eigenschaften `Lesefehler`, `LetzterFehler`, `Katalogfehler`, `Ladefehler`, `Speicherfehler`, `Vorsorgewarnung`; neun
Fehlerlagen nachgestellt; Rest 29 `catch` in 13 Dateien + 5 stille Lesestellen im Engine-Modus), E7c3/4 18d148a4 Q8 b Energiesteuer-Vorschau
je Wahl im Kern (keine, § 53 voll/energetisch, § 53a, § 54; mitgespeichert; Nachweisfassung 7 → 8; Handprobe Rechenweg 05: § 53 voll
26.383,46 €, § 53 energetisch 12.079,17 €, § 53a 21.202,71 €, § 54 6.369,85 €), E7c3/8 5f7711a7 U22 Anzeigezeilen je Wahl mit Wirkung
(Klapplisten im Formular bleiben), E7c3/10 cabd55a9 Ankertests; Nr. 9h nur gemessen (Tab_BHKW 3/6 gepflegt, Stamm 44/79; Tab_Heizkessel
1/22, Stamm 0/63, beide ohne Leser in der Wirtschaftlichkeit; Tab_StromspeicherVariante 13/13 rechnet in Speicherwirtschaftlichkeit und
Peak-Shaving; Nutzungsdauer-Tabelle BHKW-Modul 15 a, Batterie 10 a; Flotte 1046 zwei Einheiten Ersatzintervall 10 a, Restwert 500/300 €;
Vorschlag für ND‑S3 im Bericht); Nachzug 91315089 (origin 8c94ee3a: Kühlung KU1 Welle 2), E7c3/1f 29fbbc77 (Zeilenumbruch im Fehlergrund), E7c3/1g 77618ce2 (Blattstruktur-Wache ohne Speichern; ihre Prüfgruppe trägt erfundene Projekt-Ids 9001/9002, das Speichern scheiterte schon immer still), E7c3/11 ffff2fe2 (Testdatenbank auf Katalog-Generation 9: fünf Zellen, Brennstoff 24 Hi/Hs 0 → 1, `KWKG_REALISIERUNGSFRIST`/`KWKG_STICHTAG_DAUERBETRIEB` Status GESICHERT → ABGEKUENDIGT, Marker 8 → 9; LFS-SHA db143098…, 67 743 744 Byte; Schemastand 113 unverändert; `Referenzlaeufe/LIESMICH.md` bekommt dazu einen Nachtrag „Katalog-Generation 9 (Auftrag #452), die Basis bleibt“ im Muster der Schritt-Nachträge: keine Einfrierregel berührt, Referenzlauf 13/13 byte-gleich mit beiden Ständen), letzter Nachzug (39c63361 + Zapfprofil Z2) laut Startnachricht. Phase 2: gefiltert Kern 680/681, nach 1f RobustheitB6 17/17, UI 359/359; voller Lauf 0 Fehler (Kern 5.175, UI 5.471, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27+1); Referenzlauf 13/13 PASS gegen R12, 4.250.839 Werte, 399/399 byte-gleich (Repo-DB und Generation-9-Kopie); Designer wiederholbar; SQL-Prüfer 1.714 Texte, 0 Fundstellen; Schale 0 Fehler; resx je 8.091 nach dem Merge (Naht `</data>` von Hand ergänzt, Befund). A/B: 13
Basisprojekte 9.519/9.519 Werte gleich, Anker 1024 −2.896.359,13 €, 1030 −21.895.377,28 €, 99,00 €, 13.000,00 €. Schlüssel: 43 neu
(de/en), geändert `WIRT_KWKG_FALL2_VBH`, `WIRT_KWKG_FALL2_VBH_ERSATZ`, `KI_DLG_BHW_ABWAERME_ERL`, keiner entfallen. Abweichungen: Lesefehler
als Zeile statt Dialog; fehlende Spalte `Hilfsenergie_Anteil` = Lesefehler (nur Datenbanken vor Schritt 61); nicht lesbarer Tarif = nicht
aktiv. Fragen E7c3‑Q1…Q8 (Lesart gebaut = a; Entscheidstand laut NACHTRAG-452-ENTSCHEIDE): Q1 Kapitalwert 1024 Konzepttafel auf den
gemessenen Wert / b Strompreis 35 ct zurück; Q2 Katalog-Generation 9 als Nachpflege / b Schemaschritt 114; Q3 Status ABGEKUENDIGT als
vierter Status / b Vermerk; Q4 strenger Leseweg / b bei DataRepository bleiben; Q5 B‑6-Rest als eigene kleine Etappe / b so lassen; Q6
`Ladefehler`/`Speicherfehler`/`Vorsorgewarnung` in Statuszeile und BHKW-Dialog zeigen (nächste Welle) / b nur im Kern; Q7 U22 Zeilen in
der Überlagerung, Klapplisten bleiben / b Formular-Klapplisten werden Anzeigezeilen; Q8 Vorschau bei Projektvorgabe je Anlage / b
zusätzlich Projektvorschau. Logbuchsätze: „Die Überlagerung ‚Sätze und Herkunft…' nennt für jede Energiesteuerentlastung Satz und Betrag
im ersten Jahr und zeigt die Wirkung jeder Wahl in ihrer Zeile." „Lässt sich eine Rechenstufe der Wirtschaftlichkeit nicht ausführen,
steht der Grund als Warnung an der Ergebniszeile." Der E7c2-Logbuchsatz „Vollbenutzungsstunden aus dem KWK-Strom" (Version 1.2.0.4,
unveröffentlicht) wird gestrichen und durch „Die Vollbenutzungsstunden des KWKG-Kontingents zählen die erzeugte Arbeit des Moduls
geteilt durch die Nennleistung." ersetzt. Abnahme am Gerät A‑E7c3‑1: Überlagerung mit Anzeigezeilen und Energiesteuer-Vorschau,
Warnzeile bei nicht ausführbarer Prüfung (Probe mit umbenannter Tabelle auf Kopie), Herleitung Vbh mit Formel.

AUFGABEN: (1) Statusdatei: Zeile #452 (Anlass: Anwender „fahre fort" und Vbh-Definition 23.09.2026, Etappe E7 Teil c3) nach der letzten
Zeile vor `---` und Block Nach #452 als neuester Block: (a) Vbh-Entscheid und Fragen E7c3‑Q1…Q8 mit Lesarten/Empfehlung/Entscheidstand,
(b) Abnahme, (c) A/B und Kapitalwert-1024-Befund, (d) Befunde (B‑6-Rest 29 + 5, Nr. 9h-Messung, Nachweisfassung 8, `DataRepository`
wirft nie), (e) Papiernachzug inkl. **Berichtigung der Vbh-Aussagen aus #440/#446** (Register E7c1‑Q2 und E7c2‑Q7 präzisiert, Konzept
§ 3.6, Rechenweg 05, Nach #440 (a) und Nach #446 (a) mit Vermerk „präzisiert #452"), (f) Logbuch, (g) nächste Etappe E8 (fünf Blöcke,
Formelmappe 0–3, U43, Anhang D, U41/U46–U48, E6‑Q1), (h) Nachweis: Gate NACHTRAG-452-GATE, CI zu c0131b2b (Kern ios 35876777602, Kern
main 35876797866, Windows main 35876797964 grün — auch in Nach #446 (h) nachtragen), Push nach Regel. (2) Protokoll
`Dokumentation/ueberholt/Protokolle/Reporting/E7c3_Reste_B6_Kapitalwert_Protokoll.md` (Muster E7c2), Index Reporting 120 → 121 (zählen).
(3) Register: E7c1‑Q2 b präzisiert (Vbh-Definition, Wortlaut des Anwenders), E7c1‑Q8 umgesetzt, E7c2‑Q5 b/Q7/Q8 b umgesetzt, Familie
R‑E7c3 mit Q1…Q8; Konzept: § 3.6 (Vbh = W_a ÷ P_Nenn brutto in beiden Fällen; Energiesteuer-Vorschau; Katalog-Generation 9), § 3.9
(Warnzeilen B‑6), § 4 (B‑6 erledigt für fünf Dateien, Rest), § 6.2 (Kapitalwert 1024 mit Befund, Anker bleibt), § 6.3 (Nr. 9h Messung,
Vorschlag ND‑S3), § 6.1 Zeile E7c3, § 7 (E8 als nächste Etappe), Kopfzeile Codestand (Schemastand 113 unverändert); Entscheidwege-Protokoll;
Analysepapier § 5 „Teil c3 umgesetzt #452 — E7 abgeschlossen", § 6 unverändert; Rechenwege 05 (Vbh-Formel, Vorschau je Wahl,
Rückbau der Netto-Zählung), 06/PV (Q5 b), 04 (Brennstoff 24 Hi/Hs); Grundlagenpapier KWKG (Vbh-Definition als Programmregel, falls dort
geführt). (4) Mockup: U22 „erledigt #452" (Anzeigezeilen), U39 weiter teilweise (Nr. 9h → ND‑S3), Ressourcentafel (43 neue Schlüssel),
Stand-Absatz. (5) Logbuch (#452: zwei Sätze neu, E7c2-Satz ersetzt), Wiki-Quelle Wirtschaftlichkeit (Anker `kwk-abwaermeabfuhr` berichtigen:
Vbh nach erzeugter Arbeit; Energiesteuer-Vorschau; Tabu-Regex 0). (6) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.
