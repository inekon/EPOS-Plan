# Auftrag Papiere #439 — E7b Q11: HT/NT gestrichen, Leistungspreis-Staffel verlegt, Tarifdialog reduziert (gesichert 23.09.2026)

Merge-SHA, Gate-Zahlen und die Anwenderentscheide zu E7b‑Q1…Q4 nennt die Startnachricht (Platzhalter NACHTRAG-439-MERGE /
NACHTRAG-439-GATE / NACHTRAG-439-ENTSCHEIDE). Muster: `E7a_Papiere_437_Auftrag_2026-09-23.md`, Statuszeile #437 und Block Nach #437.
Statusnummer #438 gehört der Zapfprofil-Sitzung (Z0, Schemaschritt 102) — vor dem Schreiben `grep -n "#438\|#439"` in der Statusdatei.

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #439 (Etappe E7, Teil b — Q11) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test.
ARBEITSORT: Der Worktree `.claude/worktrees/papiere439` (Zweig `papiere439` ab dem Merge NACHTRAG-439-MERGE) existiert; von der
Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere439`, nie im Hauptbaum arbeiten. Commits sofort mit
`git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; kein Push, kein Merge, kein Stash.
Formregeln wie #437 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` = `</tr>`; Wiki-Tabu-Regex aus `CLAUDE.md`, 0 Treffer).

FAKTEN E7b (aus den Agentenberichten, Faktendatei: `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e7b_berichte.md`):
Zweig `e7b` von c4ef252d: E7b/1 `1b3797a3` Strommatrix ohne Tarifzonen (nur Jahressummen und Lastbilder, vom Tarif nur die
Winterspanne; `LadeTarif`/`SpeichereTarif` ohne die 13 Zonenspalten; ein aktiver Zonensatz rechnet nicht mehr und bekommt den
Hinweis `WIRT_HINWEIS_ZEITZONENTARIF`; je Projekt eine Jahreszeile in `Tab_ErgebnisStromMatrix`, Spalte `Zone` = „Jahr"; Wort- und
Excel-Tafel nachgezogen); E7b/2 `23fd7c7e` Schemaschritt **103** (DDL: drei DOUBLE-Spalten `Leistungspreis_Staffelgrenze`,
`Leistungspreis_Staffel1`, `Leistungspreis_Staffel2` an `energy_project_settings`; DML in einer Transaktion, wiederholbar: Staffel
eines aktiven Zonensatzes an den Stromträger jeder Version der Gruppe, nur in leere Spalten; aktive Zonensätze `Aktiv = 0`;
Zonenzeilen der Matrix zu einer Jahreszeile zusammengefasst; `SchemaStand.Zielversion` = 103; Migrationstest); E7b/3 `4d0004a1`
Staffel rechnet im Kern (`KostenEmissionRechner`, Speicherauslegung, Variantenkopie; Bemessung an der Viertelstundenspitze; eine
gepflegte Staffel hat Vorrang vor Leistungspreis und Saisonreihe des Stromträgers; Quelle „Staffel des Stromträgers"); E7b/4
`d263b1cc` Kostenverwaltung pflegt die Staffel (Gruppe „Leistungspreis-Staffel" an der Energieträger-Karte, Schreibweg
`EnergietraegerPreisCtrl`, Hülle, KI-Felder); E7b/5 `beeb3f47` Tarifdialog kennt nur noch das Rollenmodell (Zonen, HT-Fenster,
Modellwahl, Staffel, Sichten „Strombezug"/„Komplett" entfallen; Knopf „Strombezug…" auf der Wirtschaftlichkeitsseite und im
BHKW-Dialog entfällt; Sprünge „BHKW-Tarif…"/„Tarif…" bleiben; kein Menüpunkt existierte); E7b/6 `0780c8a9`
`StromMatrixOhneZonenTests`, Anker-Kommentare alt = neu; E7b/7 `9a89aa28` Kommentar; `a9be7581` Merge 435b9810; E7b/8 `e06d7eae`
zwei Testerwartungen berichtigt (Zeitstempel kulturunabhängig, Staffel-Beschriftung); weitere Commits laut Startnachricht
(Nachzug z0/102). Schlüssel: 21 neu (`ETV_STAFFEL_*` 6, `KI_DLG_ET_STAFFEL_*_ERL` 3, `OPT_QUELLE_STAFFEL*` 4,
`WIRT_MATRIX_TITEL/HERKUNFT/ZEITRAUM/JAHR/STUNDENSPITZE/STUNDENLAST`, `WIRT_HINWEIS_ZEITZONENTARIF`, `WIRT_TARIF_NACHWEIS_ZONEN`),
5 geändert (`WIRT_MATRIX_BEDARF_HINWEIS`, `TARIF_G_ZEITZONEN` → „Winterspanne …", `KI_DLG_TAR_WINTERVON_ERL`, `KI_DLG_TAR_WINTERBIS_ERL`,
`KDLG_ERTRAG_FK7`), 38 gestrichen (`TARIF_*` 22, `KI_DLG_TAR_*_ERL` 8, `OPT_QUELLE_TARIF*` 4, `WIRT_BTN_STROM_TARIF`,
`BHW_BTN_STROMBEZUG`, Waisen `TARIF_BTN_SPEICHERN`, `KDLG_LP_STROM_TARIF`, `KDLG_LP_STROM_TARIF_BTN`). A/B: Ankerweg aller dreizehn
Basisprojekte bitgleich (1023 −639.584,90 €, 1024 −2.896.359,13 €, 1030 −21.895.377,28 €, 99,00 €/a, Kaskade 13.000,00 €; kein
Tarifsatz, keine Staffel); frischer Lauf: Kapitalwert 1024 −2.796.650,73 € gleich, 1030 −31.141.242,71 € mit 1·10⁻⁸ € Differenz
(Matrixsumme in einem Durchlauf statt vier Teilsummen; relative Abweichung ≤ 2·10⁻¹³); gespeicherte Matrix 4 Zonenzeilen → 1
Jahreszeile, geladene Werte ±0,001 MWh durch einfache statt vierfache Rundung (1040 PV 2,272 → 2,273; 1018 −14,733 → −14,732;
1017 Bedarf 672,001 → 672,000 MWh); Altbestand 1018/1031 gleiche Summe (−14,471 / −14,723 MWh). Probe 1030 mit gebautem Zonensatz
(Staffel 1.500 kW / 60 / 90 €): Energiekosten 1.832.155,35 → 1.760.606,20 €/a (Arbeits- und Grundpreis des Stromträgers 1.091.845 €
statt Zonenpreise 1.163.394,15 €; Staffel 135.990 € gleich, jetzt Leistungsanteil des Stromträgers), KWK-Einspeiseerlös 28,56 → 0
(Zonen-Einspeisepreise entfallen, KWK-Satz in 1030 nicht gepflegt), Stromkosten Tarif 1.299.384,15 € → leer, Kapitalwert
−34.819.801,17 → −33.551.896,03 € (+1.267.905,14 €), Leistungspreisquelle 90 €/(kW·a) „Tarifstruktur" → „Staffel des Stromträgers";
1024 im Rollenmodell unverändert (Kapitalwert −2.136.393,15 €, vermiedene Kosten −12.941,70 €; Staffel des Rollensatzes wird nicht
übernommen); nicht migrierte Kopie (101): Hinweis „Zeitzonentarif (HT/NT) entfällt …", Rechnung mit Stromträger ohne Staffel
(Energiekosten 1.624.616,20 €, Kapitalwert −31.141.242,71 €). Live-DB nur gelesen: Stand 100, keine Tarifsätze, Projekt 1062 vier
Zonenzeilen → eine Zeile 3,709 MWh. Tests: gefiltert Kern 130/130, UI 387/387; voller Lauf KiKern 524, SpeicherEngine 386,
SpeicherPlanung 27+1, EPOS.UI 5.228, EPOS.Kern 4.630/4.631 (rot nur Schemastand-Wache bei Repo-DB 101; mit 103-Kopie
Auslieferungsvorlage 19/19 und Wache 1/1 grün), Formularkarte 124/124; Referenzlauf 13/13 PASS, 3.882.737 Werte, byte-gleich gegen
R11 (Basis bleibt); Designer 7.582 unverändert; SQL-Prüfer 1.579 Texte, 0 Fundstellen, Selbsttest 35; Schale 0 Fehler.
Nebenbefunde: `Tab_ProjektTarif` behält die 13 Zonenspalten (Kern liest sie nicht mehr; Kandidat für Aufräum-DDL);
`Tab_ErgebnisStromMatrix.Zone` nur noch „Jahr"; Altmatrix 1018/1031 mit negativem Netzbezug als gespiegelte KWK-Einspeisung
(Bestand vom 21.08., nicht E7b). Wiki-Verweise auf den Dialog: Wirtschaftlichkeit Z. 12 (Fußleiste „Strombezug…"), Z. 93
(Absatz strombezug), Z. 75 (gespeicherte Läufe: „aktive Tarifstruktur" = Rollentarif), Z. 57 (Energiekosten: Staffel des
Stromträgers ergänzen); Kosten Z. 95 (preiswirkung: Staffel-Gruppe); Hilfe-Assistent Z. 72 bleibt gültig; `help_mapping.txt`
Z. 265 `Form_Tarifstruktur` → Anker `#strombezug` (nur Befund). Anwenderfragen E7b‑Q1 (Dialog: a ganz entfernen / b Rollenmodell
bleibt / c später verlegen; Empfehlung b), E7b‑Q2 (Viertelstundenspitze; bestätigen), E7b‑Q3 (Vorrang der Staffel; bestätigen),
E7b‑Q4 (alte Ergebnisse: a lassen / b löschen / c Hinweis; Empfehlung a) — **Anwenderentscheide 23.09.2026:** E7b‑Q1 = b;
E7b‑Q2 bestätigt („Die Viertelstundenspitze wird abgerechnet und ist Maßstab für die Leistungsberechnung"); E7b‑Q3 bestätigt
(„Eine gepflegte Staffel ersetzt beides, sie addiert sich nicht. Entweder Leistungspreis gesetzt oder eine Reihe, keine Addition");
E7b‑Q4 abweichend von der Empfehlung: „alte Tarife verwerfen, nicht mehr relevant" — Schritt 103 löscht nach der Übernahme der
Staffel die Zonensätze aus `Tab_ProjektTarif` und die mit ihnen gerechneten Ergebnisse (Commit E7b/9 laut Startnachricht). Alle vier
als Familie R‑E7b ins Register (entschieden, mit Wortlaut). Logbuchsätze (Version 1.2.0.4, `Wiki_Update_2026-09-26.md`): „Der Zeitzonentarif (Hoch- und Niedertarif,
Winter und Sommer) ist nicht mehr vorhanden; der Strombezug wird mit den Preisen des Stromträgers aus der Kostenverwaltung
bewertet." „Die zweistufige Leistungspreis-Staffel wird in der Kostenverwaltung beim Stromträger des Projekts gepflegt und an der
Viertelstundenspitze des Netzbezugs bemessen." „Der Knopf ‚Strombezug…' auf der Wirtschaftlichkeitsseite und im Dialog
BHKW-Wirtschaftlichkeit ist nicht mehr vorhanden." Abnahme am Gerät A‑E7b‑1: Kostenverwaltung Stromträger mit Staffel (drei Felder),
Wirtschaftlichkeit ohne Knopf „Strombezug…", Matrixtafel ohne Zonen (Wort und Excel), BHKW-Dialog ohne Sprung „Strombezug…",
Rollentarif-Sprung weiterhin erreichbar, Hinweis bei nicht migriertem Zonensatz.

AUFGABEN: (1) Statusdatei: Zeile #439 (Anlass: Anwender „Pushen und Weiter", Etappe E7 Teil b, Q11) nach der letzten Zeile vor
`---` (hinter #438 der Zapfprofil-Sitzung) und Block Nach #439 VOR dem jüngsten Nach-Block: (a) Anwenderfragen E7b‑Q1…Q4 mit
Lesarten, Empfehlung und Entscheidstand, (b) Abnahme am Gerät, (c) A/B-Tafeln (Anker, frischer Lauf, gespeicherte Matrix, Probe
1030 mit Zonensatz, 1024 Rollenmodell, nicht migrierte Kopie), (d) Befunde (Zonenspalten `Tab_ProjektTarif`, Altmatrix 1018/1031,
Live-DB 1062, `help_mapping.txt`), (e) Papiernachzug, (f) Logbuch, (g) nächste Etappe E7c (alle E7-Fragen entschieden; K‑1 Schritt
104), (h) Nachweis: Gate NACHTRAG-439-GATE, Push auf Zuruf. (2) Protokoll
`Dokumentation/ueberholt/Protokolle/Reporting/E7b_Zeitzonentarif_Staffel_Protokoll.md` (Muster E7a), Index Reporting 117 → 118.
(3) Register: Q11 „gebaut #439" mit erledigt-Grund (Zeitzonentarif entfällt: Matrix ohne Zonen, Schritt 103 schaltet Zonensätze ab,
Hinweis bei nicht migriertem Satz; zweistufige Staffel am Stromträger der Kostenverwaltung, Viertelstundenspitze; Tarifdialog auf
das Rollenmodell reduziert, Einstieg „Strombezug…" entfällt), Familie R‑E7b mit E7b‑Q1…Q4; Konzept: § 3.5 Tarifmodus (kein
Zeitzonentarif; Rollenmodell bleibt), Kostenverwaltung/Energiepreisstruktur (Staffel, Vorrang, Viertelstundenspitze), § 2.x
Strommatrix (eine Jahreszeile), § 6.1 Kurztafel E7b, § 6.3 Q11 erledigt-Einzeiler, Kopfzeile Codestand und Schemastand 103
(nächster freier Schritt 104 = K‑1), § 7 Tafel, Schemaschritt-Tafel (103 = E7b); Rechenwege 04 (Bezugspreis ohne Zonen, Staffel),
07 (Rollentarif unverändert); Analysepapier § 5 E7 „Teil b umgesetzt #439", § 6 Schrittvergabe (103 = E7b, 104 = K‑1);
Szenarienkonzept unverändert. (4) Mockup: Schlüsseltafeln (neue/gestrichene Schlüssel der Kategorien Tarif/Kosten/Matrix),
Anhang: U-Zeilen zu Tarifstruktur/Strombezug/Staffel (U-Nummern per `grep -n "Tarifstruktur\|Staffel\|Strombezug"` finden) auf
„erledigt #439" bzw. Vermerk; Stand-Absatz. (5) Logbuch (#439, drei Sätze), Wiki-Quellen `Projekte/Wiki/Programm Dokumentation -
Wirtschaftlichkeit.wiki` (Z. 12, 57, 75, 93) und `… - Kosten.wiki` (Z. 95) auf den Ist-Zustand (Tabu-Regex 0). (7) **Umnummerierung der Schemaschritte** (Kollision 23.09.2026, siehe Statuszeile #438): Die Kette lautet jetzt 101 Gebäudespalten
(Gebäudesimulation, origin), **102 KWKG-Anlagenart (E7a, vorher 101)**, 103 Tww (Zapfprofilgenerator, vorher 102), **104
Leistungspreis-Staffel (E7b, vorher 103)**, **105 K‑1 (E7c1, vorher 104)**. Alle Papiere unter `Dokumentation/aktuell/`, die den
E7a-Schritt als 101, E7b als 103 oder K‑1 als 104 nennen (`grep -rn "Schritt 101\|Schemaschritt 101\|Nummer 101\|SCHRITT_101\|
Schritt 103\|Nummer 103\|Schritt 104\|Nummer 104"` über Konzept, Register, Analysepapier § 6, Rechenwege 04/05/07, Mockup U1/U32/
Anhang, Szenarienkonzept), auf die neuen Nummern bringen; Kopfzeile des Konzepts: Schemastand 104, nächster freier Schritt 105
(K‑1) bzw. 106; das E7a-Protokoll unter `ueberholt/` bleibt Geschichte und bekommt nur einen Nachsatz „Schritt 101 wurde am
23.09.2026 zu 102 umnummeriert (#438)". Statuszeile #437 hat die Zapfprofil-Sitzung schon berichtigt — prüfen, nicht doppelt
ändern. (6) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.
