# E7a — Rechenwirksame Lücken, Teil a: CO₂-Grenzwert brennwertbezogen, Schemaschritt 101, vermiedene Menge ohne jede Eigenerzeugung (Protokoll, 23.09.2026)

Statuszeile #437 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E7 (Teil a) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E7, § 6); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 6.3 Nr. 29, 30 und 32 mit § 3.6 und § 3.8; die Entscheide im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
unter R‑NR — Nr. 29 „es gilt immer der Brennwert", Nr. 30 (ein DML-Schritt setzt die leere Zeichenkette auf NULL,
der Dialog zeigt „bitte wählen") und Nr. 32 ≡ U6‑Q1 „ohne jede Eigenerzeugung", alle vom 22.09.2026 —, die Fragen
dieser Etappe unter R‑E7; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 7 (Aufteilung
der vermiedenen Kosten 293.245,6 + 22.914,0 = 316.159,6 €/a) und Kategorie 5. Anwender 23.09.2026: „Pushen und
Weiter". Zweig `e7` von `b4d468a4`, zwei Phasen; Phase 1: `57ba5097` (E7/1), `e4ce4f35` (E7/2), `d1cef396` (E7/3),
`212cb5b5` (E7/4), `8c834746` (E7/5); Phase 2: `8fce3cb2` (E7/7), `61ebd027` (E7/8), `1d98c8b1` (E7/6, Testdatenbank,
vom Orchestrator committet) und `ff8a17f9` (E7/9). Merge `befec9dc` in `ios_migration_september` (Basis `c4a6ca0a`;
24 Dateien, +1 494/−120). Opus 5.5 im Worktree `.claude/worktrees/e7`.

## Befund vor der Welle

- **Nr. 29 (Befund R11):** `SteuerGutschriftRechner.Co2JeEnergieertrag` las den CO₂-Schlüssel der Anlage
  (`EF_BILANZ_EBEV_*`, heizwertbezogen; Erdgas 200,9 g/kWh) und hielt ihn gegen den Grenzwert 270 g/kWh des § 2
  StromStG. Die brennwertbezogene Katalogzeile `EF_BILANZ_EBEV_ERDGAS_HO` (181,4 g/kWh) und die Umrechnung
  `EF_BILANZ_EBEV_UMRECHNUNG_HO` hatten keinen Leser. `KleinkorrekturenE2Tests` pinnte das Verhalten: Im Grenzfall
  (Energieertrag 72 % des Brennstoffs, heizwertbezogen 279,0 g/kWh) bekam eine Erdgasanlage 0,00 €/a.
- **Nr. 30:** Sieben Energieanlagen der Testdatenbank tragen `KWKG_Anlagenart = ''`. Der BHKW-Dialog nannte den
  Leereintrag „(nicht erfasst — gilt als Neuanlage)" — das stimmte nicht: Ohne Anlagenart leitet § 8 KWKG kein
  Kontingent ab (`KwkgKontingentRechner`, Grund „ohne Art").
- **Nr. 32 (Befund aus E4/3, Frage U6‑Q1):** `StromMatrix.Baue` bildete „Bedarf ohne Anlage" als Strombedarf
  abzüglich PV-Eigennutzung. Die vermiedene Menge war damit allein der KWK-Eigenverbrauch, der Verteilschlüssel der
  Erlösrubrik brachte nur das Blockheizkraftwerk ein — netto aus dem Modulnachweis, nach Hilfsstrom —, und der
  vermiedene Bezug der Photovoltaik stand als eigene Ausweiszeile „PV: vermiedener Bezug" zum Flat-Preis im Block
  Photovoltaik. Das Mockup rechnet „ohne jede Eigenerzeugung" (1.179,7 = 1.094,2 + 85,5 MWh).
- **K‑1 und A20** gehörten zum Auftrag der Etappe: K‑1 mit der Messung nach A2 vorab (liegt die Nutzwärme je Modul
  vor?), A20 mit dem Förderende 2030.

## Gebaut — Phase 1 (E7/1 bis E7/5)

- **Nr. 29 — der CO₂-Grenzwert brennwertbezogen (E7/1).** `SteuerGutschriftRechner.Co2JeEnergieertrag` liefert den
  Wert samt Herleitung (`Co2Energieertrag`, Bezug `Co2Bezug`). Führt der Katalog zum Schlüssel der Anlage eine
  brennwertbezogene Zeile — heute allein Erdgas: `EF_BILANZ_EBEV_ERDGAS_HO` 181,4 g/kWh statt `_HI` 200,9 g/kWh
  (`Co2SchluesselBrennwert`) —, nimmt der Zähler sie; sonst rechnet er den heizwertbezogenen Katalogwert über H_i/H_s
  des Trägers um (Projektwert vor Katalogwert, dieselbe Umrechnung wie die Brennwertmenge der Energiesteuer, kein
  pauschaler Faktor). Ohne gepflegten Brennwert bleibt der Hi-Faktor — die konservative Richtung, die Befreiung kann
  so höchstens zu Unrecht entfallen, nie zu Unrecht gewährt werden —, und eine Begründung nennt die Anlage
  (`STEUER_STROMST_CO2_HEIZWERT`). Der Brennstoff im Zähler bleibt die heizwertbezogene Menge des Rechenkerns; allein
  der Faktor wechselt die Bezugsgröße. Die Begründung über dem Grenzwert (`STEUER_STROMST_CO2`, jetzt mit
  „brennwertbezogen") nennt den Wert je Anlage; bei gewährter Befreiung steht die Herleitung in der Herkunft
  (`STEUER_STROMST_CO2_HERLEITUNG` mit `_FAKTOR_HO`, `_FAKTOR_UMGERECHNET` oder `_FAKTOR_HI` je Anlage, etwa
  „BHKW (300 kW): 218,6 g/kWh (EBeV 181,4 g/kWh, brennwertbezogen)"). Bilanz und BEHG lesen diesen Wert nicht.
  Tests: `Co2GrenzwertBrennwertTests` (neu); die Pinnung R11 in `KleinkorrekturenE2Tests` auf den Brennwert gestellt
  (Grenzfall 0,00 → 8.200,00 €/a); `SteuerGutschriftRechnerTests` mit dem Beispielwert 218,55 statt 242,05 g/kWh.
- **Nr. 30 — Schemaschritt 101 (E7/2).** `KwkgAnlagenartLeer` (Kern) ist die eine Quelle für Migration, Werkzeug und
  Nachweis: Sie trifft genau die leere Zeichenkette in `Tab_Energieanlagen.KWKG_Anlagenart`, ist wiederholbar und
  protokolliert die betroffenen Zeilen. `SchemaMigration` führt `SCHRITT_101_KWKG_ANLAGENART_LEER` nach Schritt 100,
  `SchemaStand.Zielversion` 100 → 101; `TestDatenbank.SchemaNachziehen` und `Werkzeuge/Testdatenbankschema` ziehen
  den Schritt nach. Reines DML, ergebnisneutral: In der Testdatenbank werden sieben Zellen NULL — Anlage 12310
  (Projekt 1032, Wärmepumpe), 14819 (1043, Kessel), 14842, 14843, 14844 (1043, Puffer), 14851, 14852 (1043,
  Wärmepumpen); kein BHKW, kein Referenzprojekt. `KWKG_Eigenstromfall = ''` derselben Zeilen bleibt stehen, weil der
  Entscheid nur die Anlagenart nennt. Test `KwkgAnlagenartLeerTests` (Zielstand, die sieben Anlagen, Anweisung,
  Wiederholbarkeit).
- **Nr. 30 — „(bitte wählen)" (E7/3).** `BHW_W_ART_LEER` de „(bitte wählen)", en „(please select)"; derselbe
  Rückfalltext in `BhkwWahlen.Anlagenart`; bunit-Test der Anlagenliste nachgezogen. Keine Rechenwirkung.
- **Nr. 32 — die vermiedene Menge ohne jede Eigenerzeugung (E7/4).** `StromMatrix`: `Zone.BedarfMWh` und das
  Lastbild `LastBedarf` nehmen den Strombedarf der Stunde **vor** Abzug der PV-Eigennutzung; neu sind
  `Zone.PvEigenMWh` und `PvEigenGesamtMWh` — die PV-Eigennutzung, soweit sie Bedarf deckt. Der KWK-Eigenanteil bleibt
  `min(BHKW, Bedarf nach PV)`, der KWK-Split unverändert. `VermiedenMengeMWh` ist damit Bedarf ohne jede
  Eigenerzeugung minus Restbezug; sie führt KWK- und PV-Eigenverbrauch, und die § 9b-Korrektur greift auf beide.
  `WirtschaftlichkeitCtrl.VermiedenAufteilung` bringt Blockheizkraftwerk und Photovoltaik in den Schlüssel ein (eine
  interne Überladung über Modulnachweis und Matrix macht den Kern für den Nachweis erreichbar).
  `WIRT_MATRIX_BEDARF_HINWEIS` de/en: „ohne jede Eigenerzeugung (vor Abzug der PV-Eigennutzung)". Test
  `VermiedeneMengeOhneEigenerzeugungTests` mit dem Anker über den Kernweg (Matrix, Tarifrechner, Verteilschlüssel):
  293.245,6 + 22.914,0 = 316.159,6 €/a (vorher 293.245,6, allein das Blockheizkraftwerk); Lastbild des vollen
  Bedarfs; min-Regel unverändert.
- **Anker (E7/5).** `WirtschaftlichkeitAnkerTests` trägt im Klassenkommentar den A/B-Befund: alt = neu für 1024
  (−2.896.359,13 €), 1030 (−21.895.377,28 €), die Betriebskosten 99,00 €/a und die Kaskade 13.000,00 €, je mit Grund.

## Phase 2 (E7/6 bis E7/9)

Mit der Testfreigabe hat der Orchestrator der Welle vier der sieben Fragen des Zwischenberichts beantwortet (Tafel
„Fragen aus der Etappe"); die drei übrigen sind Anwenderfragen und bleiben zurückgestellt — gebaut ist davon nichts.

- **E7/7 — beide Verteilschlüssel brutto aus der Strommatrix** (Frage 5, nach dem Mockup, Kategorie 7: „der
  Hilfsstrom berührt diese Menge nicht"). `VermiedenAufteilung` nimmt das Blockheizkraftwerk mit
  `StromMatrix.KwkEigenGesamtMWh` (min-Regel) und die Photovoltaik mit `StromMatrix.PvEigenGesamtMWh`; der
  Modulnachweis liefert nur noch den Anlagennamen, wenn genau ein Modul Eigenverbrauch trägt. Ohne Speicher ist die
  Aufteilung damit exakt. Vorher kam der Schlüssel des Blockheizkraftwerks netto aus dem Modulnachweis (nach
  Hilfsstrom) — allein gleichgültig, neben der Photovoltaik nicht mehr. `WIRT_ERL_B1_NAEHERUNG` de „Näherung: verteilt
  nach dem Eigenverbrauch je Anlage", en „approximation: allocated by self-consumption per unit" (vorher „… nach dem
  Netto-Stromanteil je Anlage"). Test mit Hilfsstrom in `VermiedeneMengeOhneEigenerzeugungTests`, Herleitungstext in
  `ErloesrubrikTests`.
- **E7/8 — im Rollentarif ersetzt der PV-Anteil die Zeile „PV: vermiedener Bezug"** (Frage 6; das Mockup zeigt nur
  den Anteil). `WirtschaftlichkeitZeilen.PvVermiedenFlat`: Der Betrag zum Flat-Preis steht nur, wo die Aufteilung der
  vermiedenen Kosten keinen PV-Anteil trägt; im Flat-Tarif bleibt die Zeile. `PvVermiedenerBezug` wird weiter
  gerechnet und gespeichert. Test in `ErloesrubrikTests` (vorher standen beide untereinander: 22.914,0 wirksam und
  24.624,0 zum Flat-Preis).
- **E7/6 — Testdatenbank auf Schemastand 101** (Frage 1). Mit `Werkzeuge/Testdatenbankschema` nachgezogen: genau die
  sieben Zellen und der Schemastand in `Tab_Applikation` (Zellvergleich aller 119 Tabellen, Integritätsprüfung ok,
  Größe 67 624 960 Byte unverändert); als LFS-Zeiger committet (SHA-256 `ec2820fc…`, vorher `d2415fcd…`). Keine
  Einfrierregel berührt. Den Commit hat der Orchestrator gemacht: Die Berechtigungsprüfung verweigerte dem Agenten
  das Schreiben der Repo-Datenbank („Modify Shared Resources"); einen anderen Weg hat er nicht versucht.
- **E7/9 — Nachtrag in `Referenzlaeufe/LIESMICH.md`** zur Basis `2026-09-22_R11_Bestandsbefunde`: Schritt 101 ist
  reines DML, keine Einfrierregel ist berührt, der Referenzlauf 13/13 byte-gleich (357/357 CSV auf Schemastand 100 wie
  101) — die Basis bleibt.

## Fragen aus der Etappe

Der Zwischenbericht nannte sieben Fragen. Drei sind Fragen an den Anwender und im Entscheidungsregister als **R‑E7**
geführt; vier hat der Orchestrator am 23.09.2026 mit der Testfreigabe beantwortet — (5) und (6) nach dem abgenommenen
Mockup (EZ‑9), (1) und (7) als Folge der Entscheide Nr. 30 und Nr. 29.

| Frage | Stand |
|---|---|
| (1) Mit Zielversion 101 werden die Schemastand-Wache und die Auslieferungsvorlage-Tests rot, solange die Repo-Testdatenbank auf 100 steht. Nachziehen, mit LFS committen, Nachtrag im LIESMICH? | ja — gebaut mit E7/6 und E7/9 |
| (2) **E7‑Q1** Nr. 30, Kern-Regel und Kohärenzzeile „Anlagenart fehlt": Wörtlich (Lesart a) verliert jedes BHKW ohne Anlagenart den Zuschlag — das trifft jedes BHKW der Testdatenbank, darunter 1030 mit gepflegtem Kontingent 30.000 h und Sätzen 8/4 ct: KWKG-Erlös Jahr 1 7.315,96 € → 0, Kapitalwert −21.895.377,28 → −21.954.815,75 € (−59.438,48 €), der Anker 1030 bewegt sich; das Konzept nennt als betroffen nur 1032 und 1043. Lesart b: nur wo die Anlagenart gebraucht wird, also wenn das Kontingent nach § 8 abzuleiten ist — so rechnet der Kern schon (0 mit Grund), neu käme allein die Kohärenzzeile | offen — Empfehlung: Lesart b, dazu die Anlagenart des 1030-BHKW in der Testdatenbank pflegen |
| (3) **E7‑Q2** K‑1: Gemessen nach A2 liegt die Wärmeproduktion je Modul vor, der Wärmeüberschuss nur als Projektsumme; in allen BHKW-Basisprojekten ist er 0 — es greift die Aufteilung nach P_el. Offen sind fünf Teilfragen: (1) die ganze Nutzwärme nach P_el aufteilen oder nur den Überschuss (die Wärme bleibt je Modul)? (2) Kennzeichen gesetzt, σ leer: der Vorschlag P_el/P_th (Mockup „leer = 0,845") oder kein Zuschlag, weil das Feld der Wert ist? (3) `min(Netto, Nutzwärme × σ)` an Stelle von `stromNettoJeAnlage` ändert nur den Anteil, bei einer einzigen Anlage bliebe der Zuschlag gleich — Eigen- und Einspeisemenge proportional kürzen oder zuerst die Einspeisung? (4) Wie rechnet Fall 2 auf dem Ersatzweg, wenn sich Anlagen und Module nicht zuordnen lassen? (5) Ort im Dialog: Gruppe 1b (Analysepapier) oder die Überlagerung „Sätze und Herkunft" (Mockup)? | offen — Empfehlung: (1) nur den Überschuss nach P_el aufteilen, (2) der Vorschlag P_el/P_th, (3) zuerst die Einspeisung kürzen, (4) der Ersatzweg nach P_el, (5) die Überlagerung „Sätze und Herkunft"; Schemaschritt **103** |
| (4) **E7‑Q3** A20, Förderende 2030: Lesart a (Analysepapier und Auftrag) — kein Zuschlag in Kalenderjahren nach 2030; das widerspricht den Grundlagen („Eine zeitliche Höchstdauer in Jahren gibt es nicht") und Rechenweg 05, dessen Beispielreihe bis 2037 zahlt. Lesart b (Grundlagen) — 2030 als Katalogdatum für das Ende der Frist zur Inbetriebnahme an Stelle der festen `KWKG_REALISIERUNG_JAHRE = 4` | offen — Empfehlung: Lesart b |
| (5) Der Schlüssel des Blockheizkraftwerks ist netto (Modulnachweis), der der Photovoltaik brutto aus der Matrix — beide brutto? | ja — gebaut mit E7/7; `WIRT_ERL_B1_NAEHERUNG` nachgezogen |
| (6) Im Rollentarif stehen der PV-Anteil und die Zeile „PV: vermiedener Bezug" (Flat-Preis) untereinander — soll die Zeile dort entfallen? | ja, im Rollentarif — gebaut mit E7/8; im Flat-Tarif bleibt sie |
| (7) Umrechnung Hi → Ho über H_i/H_s des Trägers, weil der Katalogfaktor `EF_BILANZ_EBEV_UMRECHNUNG_HO` (3,2508) nur für Erdgas gilt; ohne gepflegten Brennwert der Hi-Faktor? | bestätigt — gebaut mit E7/1; `EF_BILANZ_EBEV_UMRECHNUNG_HO` bleibt ohne Leser |

## A/B-Nachweis

Gemessen mit einem eigenen Messprogramm des Agenten an Kopien der Testdatenbank, je Stand auf zwei Wegen: dem
gebuchten Ergebnis (wie die Ankertests) und einem frischen Lauf mit Stundenreihen.

**Die dreizehn Basisprojekte, Wirtschaftlichkeit: alle Größen vorher = nachher**, auf beiden Wegen und in allen
Szenarien — Kapitalwert 1024 −2.896.359,13 €, 1030 −21.895.377,28 €, 1023 −639.584,90 €; die übrigen zehn führen
keinen Kapitalwert. Gründe: Nr. 29 — kein Projekt erreicht die CO₂-Prüfung, weil Hocheffizienz und räumlicher
Zusammenhang überall 0 sind; Nr. 30 — Schritt 101 trifft nur Anlagen ohne BHKW, und kein Rechenweg unterscheidet
`''` von NULL; Nr. 32 — `Tab_ProjektTarif` ist leer, kein Projekt rechnet im Rollentarif, `VermiedenArbeit`,
`VermiedenLeistung`, `VermiedenGesamt` und `VermiedenEntlastung9b` bleiben überall 0 → 0.

**Strommatrix (frischer Lauf) — nur die Projekte mit Photovoltaik ändern sich:**

| Projekt | Bedarf ohne Anlage [MWh] | PV-Eigennutzung [MWh] | Spitze des Bedarfs [kW] |
|---|---|---|---|
| 1007, 1046 | 19,10 → 24,00 | 4,90 | 6,60 → 6,62 |
| 1040 | 5,40 → 8,00 | 2,60 | 3,72 (gleich) |
| 1045 | 5,94 → 8,00 | 2,07 | 3,7235 → 3,7215 |

Betroffen sind die gespeicherte Spalte `Tab_ErgebnisStromMatrix.Bedarf` und die Matrixtafel in Wort- und
Excelbericht. Bei 1045 sinkt die Spitze leicht, weil die Nachtaufnahme des Wechselrichters (negative PV-Eigennutzung,
9,3 kWh/a) nicht mehr im Bedarf steckt. Alle übrigen Projekte bleiben unverändert; der Referenzlauf exportiert die
Matrix nicht.

**Vorher/Nachher-Fälle je Änderung:**

| Punkt | Fall | vorher | nachher | Grund |
|---|---|---|---|---|
| Nr. 29 | Probe: 1024 mit bestätigter Hocheffizienz und räumlichem Zusammenhang (an einer Kopie gesetzt) | 278,6 g/kWh heizwertbezogen, über 270 — Befreiung 0 € | 262,2 g/kWh (EBeV 286,9 g/kWh heizwertbezogen × H_i/H_s 0,9412) — Befreiung 1.680,07 €/a | Ho statt Hi; Kapitalwert gleich, weil Modus Ausweis |
| Nr. 29 | Probe: 1018 und 1030 | Befreiung wie gebucht | Befreiung unverändert, neue Herleitungszeile (196,9 / 189,1 / 211,3 g/kWh) | lagen schon unter 270 |
| Nr. 29 | Grenzfall R11, Erdgas (Energieertrag 72 % des Brennstoffs) | 279,0 g/kWh — 0,00 €/a | 251,9 g/kWh — 8.200,00 €/a (400 MWh × 20,50 €/MWh) | Ho-Faktor statt Hi-Faktor |
| Nr. 29 | Grenzfall Heizöl (Test) | 0,00 €/a | 8.200,00 €/a | Umrechnung über H_i/H_s |
| Nr. 29 | Beispielprojekt (Rechenweg 05) | 242,1 g/kWh | 218,6 g/kWh | Befreiung unverändert 23.677,5 €/a |
| Nr. 30 | Schritt 101 | — | alle Werte gleich | kein Rechenweg unterscheidet `''` von NULL |
| Nr. 32 | Mockup-Beispiel über den Kernweg (Tafel unten) | 293.245,6 €/a, allein das Blockheizkraftwerk | 293.245,6 + 22.914,0 = 316.159,6 €/a | der PV-Eigenverbrauch wandert in die Menge |

**Mockup-Beispiel über den Kernweg** (mit 10 €/kW·Monat, damit der Leistungsanteil sichtbar wird); der
KWK-Eigenverbrauch bleibt 1.094,2 MWh:

| Größe | vorher | nachher |
|---|---|---|
| vermiedene Menge | 1.094,2 MWh | 1.179,7 MWh |
| Arbeitsanteil | 315.129,6 € | 339.753,6 € |
| § 9b-Korrektur | 21.884,0 € | 23.594,0 € |
| Leistungsanteil | 14.989,0 € | 16.160,3 € |
| Aufteilung | allein das Blockheizkraftwerk, 293.245,6 € | Blockheizkraftwerk 293.245,6 + Photovoltaik 22.914,0 = 316.159,6 € |

**Brutto- gegen Netto-Schlüssel, mit Hilfsstrom (Kern-Test):** vorher netto 1.000,0 : 85,5 → Blockheizkraftwerk
1.086,8 MWh, Photovoltaik 92,9 MWh; jetzt brutto 1.094,2 : 85,5 → genau 1.094,2 und 85,5 MWh (Summe gleich).

## Zahlen und Abnahme

- **Im Worktree `e7`** (Endstand `ff8a17f9`, nach dem Commit der Testdatenbank, nacheinander, ohne anderen
  Testprozess): `TestdatenbankSchemastandWacheTests` 1/1 (vorher rot); `Auslieferungsvorlage.Tests` 19/19 (vorher
  7/19 — alle roten aus „Schemastand 100 (erwartet 101)"); voller Lauf `WP-Plan.Kern.slnf` 0 Fehler, 10 778
  bestanden, 1 übersprungen (EPOS.Kern 4 611, EPOS.UI 5 230, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und
  1 übersprungen — der Überspringer bestand schon vorher); Formularkarte 124/124; Referenzlauf gegen
  `2026-09-22_R11_Bestandsbefunde` 13/13 PASS, 357/357 CSV byte-gleich, auf Schemastand 100 wie 101 gerechnet — die
  Basis bleibt R11; Designer unverändert, 7 599 Einträge; SQL-Prüfer 1 567 Texte, 0 Fundstellen, Selbsttest 35/35;
  Windows-Schale 0 Fehler, 12 Bestandswarnungen. Kein Nachziehlauf der Bestandsergebnisse (Nr. 31).
- **Gate #437** auf `befec9dc` (05:06–05:10): Kern-Filter (Release) 0 Fehler, ChartProben 108 Bilder gleich der
  Windows-Messlatte, Tests 0 Fehler, 10 778 bestanden, 1 übersprungen (EPOS.Kern 4 611, EPOS.UI 5 230, KiKern 524,
  SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen), Dokumentationswachen 24/24; Windows-Schale (Worktree
  `e7`, `ff8a17f9`) 0 Fehler, 12 Bestandswarnungen.
- **Ressourcen:** fünf neue Schlüssel de/en — `STEUER_STROMST_CO2_FAKTOR_HO`, `STEUER_STROMST_CO2_FAKTOR_UMGERECHNET`,
  `STEUER_STROMST_CO2_FAKTOR_HI`, `STEUER_STROMST_CO2_HERLEITUNG`, `STEUER_STROMST_CO2_HEIZWERT`; vier geändert —
  `STEUER_STROMST_CO2` (nennt „brennwertbezogen"), `BHW_W_ART_LEER` („(bitte wählen)" statt „(nicht erfasst — gilt
  als Neuanlage)"), `WIRT_MATRIX_BEDARF_HINWEIS` („ohne jede Eigenerzeugung (vor Abzug der PV-Eigennutzung)"),
  `WIRT_ERL_B1_NAEHERUNG` („verteilt nach dem Eigenverbrauch je Anlage"). Designer 7 599 Einträge.
- **Schemaschritt 101** (`SCHRITT_101_KWKG_ANLAGENART_LEER`), `SchemaStand.Zielversion` = 101. Der nächste freie
  Schritt ist **103**: 102 führt die Sitzung des Zapfprofilgenerators (Zweig `z0`) für ihre Tabellen — sie hatte
  zuerst ebenfalls 101 vergeben und nach dem Merge von e7 umnummeriert.

## Abnahme am Gerät (A‑E7a‑1, Windows und iPad)

(1) Ein Projekt mit BHKW, bei dem Hocheffizienz und räumlicher Zusammenhang bestätigt sind: Die
Stromsteuer-Befreiung nennt in Herkunft bzw. Begründung den CO₂-Wert je Anlage mit dem Faktor, aus dem er entstand
(„… g/kWh (EBeV 181,4 g/kWh, brennwertbezogen)"); Vorschau und Kohärenzprüfung des BHKW-Dialogs zeigen denselben
Lauf. (2) Im BHKW-Dialog heißt der Leereintrag der Anlagenart „(bitte wählen)". (3) Ein Projekt im Rollentarif mit
BHKW und Photovoltaik: Block B der Erlösrubrik schließt je Anlage mit den wirksam vermiedenen Kosten, die Herleitung
nennt „Näherung: verteilt nach dem Eigenverbrauch je Anlage", und eine zweite Zeile „PV: vermiedener Bezug" steht
nicht darin; im Flat-Tarif steht diese Zeile weiter. (4) Die Strommatrix-Tafel in Wort- und Excelbericht nennt den
Bedarf „ohne jede Eigenerzeugung (vor Abzug der PV-Eigennutzung)"; in einem Projekt mit Photovoltaik liegt „Bedarf
ohne Anlage" um die PV-Eigennutzung höher. (5) Englisch.

## Befunde nebenbei

- **Schrittvergabe 101/102:** Die Sitzung des Zapfprofilgenerators (Zweig `z0`) hatte 101 ebenfalls vergeben; nach
  dem Merge von e7 führt sie ihren Schritt als **102** und migriert die Testdatenbank auf dem Stand nach e7. K‑1
  (Schritt A) bekommt den nächsten freien, **103**. Konzept § 3.6 hatte 101 für K‑1 genannt; die Vergabe an Nr. 30
  folgte dem Auftrag der Welle.
- **Wärmepumpenstrom:** Der Strombedarf der Simulation enthält den Strom der Wärmepumpen nicht (1039: Bedarf
  60,0 MWh, Netzbezug 119,2 MWh). Im Rollentarif würde die vermiedene Menge dort negativ. Nicht Teil von E7a.
- **`KWKG_Eigenstromfall = ''`** bleibt an denselben sieben Zeilen stehen (der Entscheid nennt nur die Anlagenart).
- **Kommentar:** `SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART` (`EPOS.Kern/Allgemein/Update/SchemaKatalog.cs`) trägt noch
  „ohne Rechenwirkung" — veraltet seit BK1.
- **`EF_BILANZ_EBEV_UMRECHNUNG_HO`** (3,2508) bleibt ohne Leser (Frage 7).
- **Testdatenbank im Auftrag:** Der Agent durfte die Repo-Testdatenbank nicht schreiben; ein künftiger Auftrag mit
  Schemaschritt sieht den Commit der Datenbank beim Orchestrator vor.

## Offen

- **Drei Anwenderfragen** (Register R‑E7): E7‑Q1 (Nr. 30, Kern-Regel und Kohärenzzeile; Empfehlung Lesart b und die
  Anlagenart des 1030-BHKW pflegen), E7‑Q2 (K‑1, fünf Teilfragen; Schemaschritt 103), E7‑Q3 (A20; Empfehlung Lesart
  b). Gebaut wird danach mit E7c.
- **Befunde:** Wärmepumpenstrom im Strombedarf der Simulation, `KWKG_Eigenstromfall = ''`, der veraltete Kommentar
  an `SPALTE_EA_KWKG_ANLAGENART`, `EF_BILANZ_EBEV_UMRECHNUNG_HO` ohne Leser.
- **Abnahme am Gerät** A‑E7a‑1 (fünf Punkte oben).
- **Nächste Etappen:** E7b — Q11 (Zeitzonentarif HT/NT streichen, Weg 3 aus Nach #291 mit der Messung
  `Messung_Pflegewege_Tarifstruktur_Strom.md`; die Leistungspreis-Staffel in die Kostenverwaltung neben die
  Energiepreisstruktur, Weg 2; Tarifstrukturdialog und Menüpunkt abkündigen; A/B, gegebenenfalls neue Basis); E7c
  nach den Entscheiden — K‑1 (Schritt A = 103), Nr. 30 Kern-Regel, A20, S‑2 (A3), V‑2/V‑1 (A4), die Schritte E, F, G,
  B‑4 Rest, B‑6 und der Kapitalwert 1024 (−676.036,81 € gegen den früheren Konzeptwert).
- **Papiere mit der Statuszeile:** Register (R‑NR Nr. 29 und 32 gebaut, Nr. 30 teilweise, R‑E7 neu, U6‑Q1, A2, A12,
  A20, EZ‑5), Konzept (Kopf mit Schemastand 101, § 2.2, § 2.6, § 2.13 (4), § 3.5 bis § 3.8, § 6.1, § 6.2, § 6.3
  Nr. 29/30/32, § 7, Anhang), Protokoll der Entscheidwege (§ 8.3, § 8.4), Analysepapier (Kopf, § 5, § 6), Rechenwege
  04, 05 und 07, Mockup (Schlüsseltafeln der Kategorien 5 und 7, CO₂-Beispiel 218,6 g/kWh, U6, Schrittnummer in U1
  und U32), Logbuch-Sätze und Wiki-Quelle der Seite Wirtschaftlichkeit.

**Nachsatz:** Schritt 101 wurde am 23.09.2026 zu 102 umnummeriert (#438); die hier für K‑1 genannte 103 ist damit
105 (#439).
