# Auftrag Papiere #440 — E7c1: K‑1 Fall 2, Förderende 2030, Anlagenart-Kohärenz (gesichert 23.09.2026)

Merge-SHA, Gate-Zahlen und die Anwenderentscheide zu E7c1‑Q1…Q8 nennt die Startnachricht (Platzhalter NACHTRAG-440-MERGE /
NACHTRAG-440-GATE / NACHTRAG-440-ENTSCHEIDE). Muster: `E7b_Papiere_439_Auftrag_2026-09-23.md`, Statuszeile #439 und Block Nach #439.
Vor dem Schreiben `grep -n "#440\|#441"` in der Statusdatei (Nachbarsitzungen vergeben Nummern parallel).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #440 (Etappe E7, Teil c1 — K‑1, A20, Nr. 30) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein
Build, kein Test. ARBEITSORT: Der Worktree `.claude/worktrees/papiere440` (Zweig `papiere440` ab NACHTRAG-440-MERGE) existiert; von der
Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere440`, nie im Hauptbaum arbeiten. Commits sofort mit
`git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie
#439 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer; Logbuch Version 1.2.0.4).

FAKTEN E7c1 (Faktendatei mit den Berichten des Bau-Agenten: `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e7c1_berichte.md`;
Grundlage der Etappe: `scratchpad\e7c_fakten.md` Abschnitte 1–3): Zweig `e7c1` von e06d7eae: E7c1/1 `e531f0bf` Schemaschritt **105**
(`KWKG_Abwaermeabfuhr` Ganzzahl 0/1 CHECK Vorgabe 0, `KWKG_Stromkennzahl` REAL nullbar, STRICT bleibt; Zielversion 105;
Migrationstest); E7c1/2 `152827a3` K‑1 Fall 2 (`KwkStromRechner`, eingebaut in `ReiheJeAnlage` und den Ersatzweg: mit Kennzeichen
KWK-Strom = min(Netto, Nutzwärme × σ); Nutzwärme = Wärme des Moduls minus Anteil am Wärmeüberschuss, Überschuss nur nach P_el; σ =
gepflegt, sonst P_el ÷ P_th der Gerätezeile; ohne bestimmbares σ Zuschlag 0 mit Herleitungszeile und Kohärenzzeile „Stromkennzahl
fehlt"; Kürzung zuerst an der Einspeisung; Ersatzweg nach P_el mit Zeile „Ersatzweg"; Herleitung je Anlage nennt Fall, σ mit
Herkunft, Nutzwärme, KWK-Strom, Kürzung; 7 neue, leer erlaubte Nachweisfelder, Nachweisfassung bleibt 7); E7c1/3 `e8a6a773` A20
(Konstante `KWKG_REALISIERUNG_JAHRE` gestrichen; Katalogschlüssel `KWKG_INBETRIEBNAHME_FRISTENDE` = 31.12.2030 mit Herkunft,
Katalog-Generation 8 mit Nachsaat; nullbar gelesen, ohne Katalogwert Herleitungszeile „ungeprüft" statt stiller Vorgabe;
Inbetriebnahme nach dem Fristende: kein Zuschlag mit Hinweis; Reihe läuft bis Kontingentende — Test: Inbetriebnahme 01.10.2026 →
Jahr 12 = 2037; `WIRT_KWKG_ANLAGE_FRIST` nennt Fristende und Herkunft); E7c1/4 `d17be688` Nr. 30 (Kohärenzzeile „Anlagenart fehlt",
Schwere Hinweis, nur wenn das Kontingent aus der Anlagenart abzuleiten ist, Regel- und Ersatzweg); E7c1/5 `f1014e66` Überlagerung
„Sätze und Herkunft" (Gruppe 1b des BHKW-Dialogs zeigt eine KWK-Strom-Zeile und den Knopf „Sätze und Herkunft…"; Überlagerung mit
Wahl Fall 1/Fall 2, Zeile „Stromkennzahl σ" mit Vorschlag P_el ÷ P_th, Herkunft, eigenem Wert, „Vorschlag übernehmen", Spalte
„gilt", Knöpfe Abbrechen/Übernehmen; Schreiben beim OK über `KwkgAnlagenCtrl.Speichere`; Mockup U22 nur mit den zwei Feldern);
E7c1/6 `45553be3` Ankertests alt = neu; E7c1/7 `8ca5bcbe` Kommentare; weitere Commits laut Startnachricht (Nachzug 954d4dcc,
Testdatenbank 105 + 1030-UPDATE). Schlüssel: 43 neu (11 K‑1 `WIRT_KWKG_SIGMA_*`, `WIRT_KWKG_FALL2_*`, `KOH_KWKG_STROMKENNZAHL_FEHLT`;
2 A20 `WIRT_KWKG_NACH_FRISTENDE`, `WIRT_KWKG_FRISTENDE_FEHLT`; 2 Nr. 30 `WIRT_KWKG_KONTINGENT_ANLAGE_OHNE_ART`,
`KOH_KWKG_ANLAGENART_FEHLT`; 28 `BHW_*` für die Überlagerung), 1 geändert (`WIRT_KWKG_ANLAGE_FRIST`), Katalogschlüssel
`KWKG_INBETRIEBNAHME_FRISTENDE`; Mockup nennt `WIRT_KWKG_MENGE_FALL2`, gebaut ist `WIRT_KWKG_FALL2_ANLAGE`. Testdaten 1030: Anlagen
14920 und 14921 bekommen `KWKG_Anlagenart = 'NEUANLAGE'` (nur die Neuanlage erreicht 30.000 Vbh ohne Kostenanteil), Anker bleiben
(Kontingent gepflegt). A/B: alle dreizehn Basisprojekte 0 Abweichungen (kein Projekt trägt das Kennzeichen; 8.011 Werte der
KWKG-Reihe gleich); 1024 −2.896.359,13 € / 188.167,18 €/a / 99,00 €/a gleich; 1030 Anker −21.895.377,28 € / KWKG Jahr 1 7.315,96 €
gleich (frisch −31.141.242,71 € / 7.322,63 €; Inbetriebnahme 2027 vor beiden Fristen, Kontingent gepflegt); 1042 Kaskade 13.000,00 €.
Proben 1030 (Anker-Weg, Kapitalwert / KWKG Jahr 1): σ gepflegt 0,5 → −21.904.948,06 / 6.137,94 (605,52 MWh × 0,5 = 302,76 MWh,
Kürzung 71,02 MWh); σ berechnet 50 ÷ 81 → −21.895.377,57 / 7.315,92 (Kürzung 0,002 MWh aus Rundung); σ berechnet + 100 MWh
Überschuss → −21.902.427,26 / 6.448,21 (Nutzwärme 520,77, KWK-Strom 321,47 MWh); ohne σ (P_th leer) → −21.945.748,56 / 1.116,03
(Zuschlag der Anlage 0, Zeile „Stromkennzahl fehlt"); Ersatzweg σ 0,5 → −21.902.856,75 / 6.395,35 (Kürzung 54,40 MWh); Einspeisung
zuerst (künstlicher Bedarf) 10.184,46 → 7.828,43 (Einspeisung 146,55 → 75,53 MWh, Eigenverbrauch 227,23 MWh unverändert); Stichtag
2025 mit Inbetriebnahme 06/2030 −21.954.815,75 / 0 → −21.896.087,48 / 5.899,97 (alte Regel > 4 Jahre nach Stichtag, neu vor
31.12.2030); Inbetriebnahme 31.12.2030 → 5.899,97, 03/2031 → 0; 03/2031 ohne Stichtag 5.899,97 → 0 (Fristende gilt auch ohne
Stichtag); Anlagenart NEUANLAGE → Anker gleich (1.948/1.948 Werte); ohne Kontingent und ohne Anlagenart 1.116,03 gleich, dazu die
neue Zeile. Nebenbefunde: Katalogzeilen `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB` werden gesät, aber nicht mehr
gelesen; KI-Feldkatalog kennt die zwei Felder nicht; Word/Excel-Modultafeln ohne Spalten für Fall 2; Rest der Überlagerung (Anlagenart,
Tatbestand, Satztafel, Energie-/Stromsteuer, „Wirkung Jahr 1", „Wahl und Herkunft…") offen → E7c2. Fragen E7c1‑Q1…Q8 (Lesarten und
Empfehlung; Entscheidstand laut NACHTRAG-440-ENTSCHEIDE): Q1 Kürzungen unter 0,01 MWh (a ohne Toleranz, gebaut / b als 0); Q2
Kontingentverbrauch in Fall 2 (a Vbh nach Bruttostrom, gebaut / b Vbh aus KWK-Strom; gegen die Vbh-Definition prüfen); Q3 Fristende
ohne Stichtag (a ja, gebaut / b nur mit Stichtag); Q4 Fristende fehlt im Katalog (a Zuschlag mit Zeile „ungeprüft", gebaut / b kein
Zuschlag); Q5 Schwere der Kohärenzzeilen (a Hinweis, gebaut / b Warnung); Q6 Nachweisfassung 7 bleibt; Q7 Rest der Überlagerung,
KI-Feldkatalog, Berichtsspalten → E7c2; Q8 Katalogzeilen ohne Leser (spätere Generation). Logbuchsätze: „Im BHKW-Dialog lassen sich
unter ‚Sätze und Herkunft…' je Anlage die Vorrichtung zur Abwärmeabfuhr und die Stromkennzahl pflegen; der KWKG-Zuschlag rechnet
dann mit Nutzwärme × Stromkennzahl." „Der KWKG-Zuschlag gilt für Anlagen mit Inbetriebnahme bis zum 31.12.2030 und läuft bis zum
Ende des Vollbenutzungsstunden-Kontingents." Abnahme am Gerät A‑E7c1‑1: BHKW-Dialog Gruppe 1b mit KWK-Strom-Zeile und Knopf,
Überlagerung mit Fall-Wahl und σ-Zeile, Herleitung je Anlage im Ausweis (Fall 2 mit gepflegtem und berechnetem σ), Kohärenzzeilen
„Stromkennzahl fehlt" und „Anlagenart fehlt", Hinweis bei Inbetriebnahme nach 2030.

AUFGABEN: (1) Statusdatei: Zeile #440 (Anlass: Anwenderentscheide E7‑Q1…Q3 vom 23.09.2026, Etappe E7 Teil c1) nach der letzten Zeile
vor `---` und Block Nach #440 VOR dem jüngsten Nach-Block: (a) Fragen E7c1‑Q1…Q8 mit Lesarten, Empfehlung und Entscheidstand,
(b) Abnahme am Gerät, (c) A/B-Tafeln (Basisprojekte, Proben 1030), (d) Befunde (Katalogzeilen ohne Leser, KI-Feldkatalog,
Berichtsspalten, Mockup-Schlüsselname), (e) Papiernachzug, (f) Logbuch, (g) nächste Etappe E7c2, (h) Nachweis: Gate NACHTRAG-440-GATE,
Push auf Zuruf. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E7c1_KWKG_Fall2_Foerderende_Protokoll.md` (Muster E7b),
Index Reporting 118 → 119. (3) Register: A2*, EZ‑5 (K‑1), A20 (Teil Förderende gebaut; Mindestabstand und ETS 2 offen), Nr. 30
(erledigt), E7‑Q1/E7‑Q2/E7‑Q3 „umgesetzt #440", Familie R‑E7c1 mit Q1…Q8; Konzept: § 3.6 (Fall 2 als Regel mit den fünf
Teilantworten und der σ-Auflage, Fristende 2030 als Katalogdatum, Kohärenzzeile Anlagenart), § 3.9 (zwei neue Kohärenzzeilen), § 6.1
Kurztafel E7c1, § 6.3 Nr. 30 erledigt-Einzeiler, Befund K‑1 erledigt, Kopfzeile Codestand und Schemastand 105 (nächster freier
Schritt 106), § 6 Schrittvergabe (105 = K‑1), § 7; Rechenweg 05 (Fall 2 mit Beispiel, Fristende, A20-Zeile „gebaut #440"),
Prüfkette-Absatz; Analysepapier § 5 E7 „Teil c1 umgesetzt #440", § 6 Schritt A = 105 gebaut; Grundlagenpapier KWKG nur, wenn es
die Vier-Jahres-Frist als Programmregel nennt. (4) Mockup: Schlüsseltafel Kategorie 5 um die neuen Schlüssel (`WIRT_KWKG_MENGE_FALL2`
→ `WIRT_KWKG_FALL2_ANLAGE`), Anhang U1 „erledigt #440" (Chip und Stand), U22 „teilweise #440 (zwei Felder), Rest E7c2" mit Grund,
Stand-Absatz. (5) Logbuch (#440, zwei Sätze), Wiki-Quelle Wirtschaftlichkeit (Ist-Zustand: Abwärmeabfuhr/Stromkennzahl im
BHKW-Dialog, Fristende 2030; Tabu-Regex 0). (7) Nachzug aus #439: Kopf des Szenarienkonzepts (`Konzept_Szenarien_…`, nennt noch Zielversion 100) und des datierten
Prüfpapiers der Mockups (`2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md`) auf Codestand/Schemastand 105 bringen; LIESMICH-Nachtrag
`Referenzlaeufe/LIESMICH.md` „Schemastand 105 (Auftrag #440, Etappe E7c1), die Basis bleibt" im Muster der Nachträge 102/104:
Schritt 105 (DDL zwei Spalten, Katalog-Nachsaat Generation 8 = eine Zeile `KWKG_INBETRIEBNAHME_FRISTENDE`), dazu das
Testdaten-UPDATE (Anlagen 14920/14921 des Projekts 1030: `KWKG_Anlagenart` NULL → 'NEUANLAGE', Kontingent 30.000 h bleibt, kein
Anker bewegt sich, keine Einfrierregel berührt, Referenzlauf 13/13 byte-gleich), Commit E7c1/9 ccf9f22f, LFS-SHA 66aa52b0…, Größe
unverändert 67 727 360 Byte. (6) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.
