# E20 — Investitionskosten der Wärmepumpe „je kW elektrisch“ aus der Kennlinie am Normpunkt (Protokoll, 25.09.2026)

Statuszeile #502 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Anwender am
25.09.2026 zu Konzept § 6.3 Nr. 10, „KD1-Bemessungsarten: BHKW kosten bemessen nach kWh El., andere nach thermischer
Leistung, Wärmepumpe beides“, präzisiert „‚Wärmepumpe beides‘ nur bei Investitionskosten nach kW elektrisch und kW
thermisch“ (Statusdatei Nach #498 (a), Register R‑Rest). Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.2 (Tafel der Runde 1, Art ↔ Gewerk gekreuzt), § 6.3 Nr. 10;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E20 (neu) und R‑Rest (Nr. 10); Herkunft der Landkarte:
[`H4a_Bezugsgroessen_Protokoll.md`](H4a_Bezugsgroessen_Protokoll.md) (die Regel „genau eine Größe je Gewerk“) und
[`H4b_Investitionsraster_Protokoll.md`](H4b_Investitionsraster_Protokoll.md) (`BaugroesseSumme` mit der
Art↔Gewerk-Kreuzprüfung); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 1 (Zone „Dialog — Investitionskosten“,
Erklärtafel und Absatz „Bewerten“; Ressourcentafel) und Stand-Absatz des Anhangs. Vorgänger:
[`E19_Unternehmensart_ohne_BHKW_Protokoll.md`](E19_Unternehmensart_ohne_BHKW_Protokoll.md). Zweig `e20` von `cbed6dba`
(`origin` nach dem Push #498, Zielversion 141); Opus 5.5 im Worktree `.claude/worktrees/e20`, zwei Phasen: Phase 0 nur
gelesen und gemessen, Phase 1 nach der Freigabe (25.09.2026, 09:15) `13d8a9a4` (E20/1), `e10300d7` (E20/2), `f50ac248`
(E20/3), `a414b768` (E20/4). Merge `49ea20e0` („Merge e20: Waermepumpe Investitionskosten je kW elektrisch aus der
Kennlinie am Normpunkt (#502)“) auf `pm22b` über `pm21b` = `365143e1` (#506, dazu #505 mit Schemaschritt 142),
ohne Konflikt. Basis `2026-09-24_R14_Kaelteerzeuger`. **Kein Schemaschritt, im Bestand keine Rechenwirkung** — Anker
und Referenzlauf byte-gleich; der Schemastand bleibt 142 (#505).

## Befund vor der Welle (Phase 0)

1. **`Tab_WP` hat keine Spalte für die elektrische Leistung.** `Nennleistung` (INTEGER) ist thermisch und wird beim
   VDI-3805-Import abgeschnitten (`KatalogImportSatz.cs:486`); `maxPtherm` steht immer auf 0; `Heizung` ist die
   Leistung des Heizstabs; dazu `Kuehlleistung` und `Kuehlbetrieb`. Der Nenn-COP aus VDI 3805 (Satz 700 Sp. 31,
   `WaermepumpenImport.cs:145`) lebt nur im Einlesedialog und wird nicht gespeichert. Die elektrische Leistung ergibt
   sich deshalb aus der Kennlinie `Tab_Kenndaten` (Vorlauf, Temperatur, COP, Ptherm): P_el = Ptherm ÷ COP — ein dritter
   gerechneter Zweig der Landkarte neben der Photovoltaik (`KwpSumme`) und der Solarthermie (`KollektorfeldKw`), ohne
   Schemaschritt.
2. **Messung.** 29 Anlagenzeilen mit Wärmepumpe (19 Luft/Wasser, 10 Sole/Wasser), alle mit einer Kennlinie bei Vorlauf
   35, kein COP ≤ 0. Katalog `_STAMM`: 49 Geräte (34 L/W, 14 S/W, 1 W/W, 2 ohne Typ); der Normpunkt je Bauart (A2/W35,
   B0/W35, W10/W35) ist bei allen echten Geräten vorhanden; T 800-2 und 352.AHT führen W35 nur bei −5/0/5/10 bzw.
   0/5/10/15 °C (lineare Interpolation); LS 16-B R nur mit COP 0 und test7 ohne Kennlinie ergeben null mit Grund GERAET.
3. **`Nennleistung` als Zähler unbrauchbar.** Das Verhältnis zu Ptherm am Normpunkt streut von 0,26 bis 1,9
   (CS5800i AW 12 M: 12 gegen 4,3 kW; CS3400i AWS 10 E: 3 gegen 3,8 kW; L 28 I-2: 25 gegen 13,4 kW). Das Maximum über
   die Kennlinie liegt bis zum Faktor 2 über dem Normpunkt (T 800-2: 17,06 statt 8,8 kW bei 65 °C) und ist eher eine
   Anschlussleistung.
4. **Kühlbetrieb.** Nur 1017 kühlt; P_el im Kühlbetrieb 2,7 bis 4,2 kW liegt unter dem Heiz-Normpunkt (7,91 kW).
   Beispiele am Heiz-Normpunkt: 1024 CS6800iAW 10, A2/W35 11,6 ÷ 2,90 = 4,00 kW; 1017 WPE-I 59, B0/W35 35,6 ÷ 4,50 =
   7,91 kW.
5. **Wo die Kreuzung Art ↔ Gewerk sitzt.** Landkarte `TechnikPlanwertCtrl.cs` `Geraetespalte` (`EUR_PRO_KW_ELEKTRISCH`
   nur an BHKW-Pel und Stromspeicher-Leistung), `BaugroesseSumme`, `KenntBaugroesse`, `BaugroessenName`,
   `BaugroesseHerleitung`; Grund und Filter `WirtschaftlichkeitCtrl.BasisGrund`, `BasisGrundFuerZeile`, `FrischeBasis`
   (las die `KategorieID` nicht); Kategorie 1 `InvestKaskade.InvestBetrag`; Kategorie 2 `RueckfallMenge`; Dialogfilter
   `KostenVorlagenCtrl.PasstZuGewerk` (ohne Kategorie) und `BemessungKatalog.Auswahl` (kannte `invest`, gab es nicht
   weiter); Dialogzeilen `KostenProjektPositionenCtrl.cs`; Herleitung `KostenHerleitung.HerkunftText`, Ressource
   `KDLG_GR_PEL` vorhanden; die KI-Sicht liest die Bemessungsliste der Hülle (`KiDialoge.cs:6602`); die Berichte zeigen
   nur die Betriebskosten mit Bemessungstext, kein Investitionsraster, Anhang D unberührt; Wache
   `PufferspeicherVolumenbemessungTests.Die_Zuordnung_kommt_aus_EINER_Landkarte`.
6. **Risiko null.** Keine Zeile in `Tab_ProjektWerte` oder den Vorlagen trägt an der Wärmepumpe (KomponentenID 1)
   `EUR_PRO_KW_ELEKTRISCH`; an der Wärmepumpe steht nur `EUR_PRO_KW_HEIZLEISTUNG` (in 1040 und einer Vorlage).

## Gebaut

- **E20/1 — Kern-Lesekette** (`13d8a9a4`): `TechnikPlanwertCtrl.WaermepumpePelKw(projekt, idAnlage)` summiert über die
  Anlagenzeilen Ptherm ÷ COP am Normpunkt der Kennlinie des Projektgeräts bei Vorlauf 35 — Bauart Luft/Wasser A2,
  Sole/Wasser B0, Wasser/Wasser W10 (`DbWerte.WP_BAUART_*`); fehlt die Stützstelle, wird je Größe linear interpoliert,
  nie extrapoliert; COP ≤ 0, Ptherm ≤ 0 oder unbekannte Bauart ergeben null; Heizstab und Kühlkennlinie zählen nicht.
  Neu `IstWpElektrischeLeistung`, `WpNormQuellentemperatur`, `WaermepumpeNormpunkt`, Klasse `WpNormpunkt`;
  `BaugroesseSumme` und `KenntBaugroesse` mit der Überladung `bool investition` (Vorgabe false);
  `BaugroessenName` → `KDLG_GR_PEL` (E20‑Q1 a, Q2 a, Q3 a, Q4 a).
- **E20/2 — Kategoriebindung** (`e10300d7`): `WirtschaftlichkeitCtrl.BasisGrund(bem, komp, elektrokessel, investition)`,
  beide `BasisGrundFuerZeile` mit `investition`, `RueckfallMenge(…, investition = false)`; `FrischeBasis` und
  `MengeAusweisen` lesen die `KategorieID`; `KostenVorlagenCtrl.PasstZuGewerk(persistenz, komp, invest)`,
  `BemessungKatalog.Auswahl` filtert je Raster; `InvestKaskade` fragt mit true, die Betriebsschleifen mit false — an der
  Wärmepumpe antwortet die Betriebsseite weiter GEWERK; `KostenProjektPositionenCtrl` reicht die Kategorie durch
  (E20‑Q5 a, Q7 a, Q8 a).
- **E20/3 — Herleitung und Texte** (`f50ac248`): `WaermepumpePelHerleitung` („11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW“;
  interpoliert mit „≈“; mehrere Wärmepumpen als Summe), Überladung von `BaugroesseHerleitung`.
- **E20/4 — Tests** (`a414b768`): neu `WaermepumpeElektrischeLeistungTests` mit 13 Fällen (Kopien 1024, 1017, 1006,
  1009: A2/W35 4,00 kW; B0/W35 mit Kühlbetrieb 7,91 kW; Interpolation A0/A5 8,75 kW; ohne umschließende Stützstelle
  null mit Grund GERAET; COP 0 und fehlende Bauart null; ohne Raster und im Betrieb null; Kaskade 4.000 € mit Herkunft
  ANLAGE, Betriebszeile 0/GEWERK; Dialogzeilen, Herleitung, Summenform, Name P_el);
  `PufferspeicherVolumenbemessungTests` Kreuztafel je Raster und der Fall „nur je kW elektrisch an der WP unterscheidet
  sich“; `BemessungsauswahlJeGewerkTests` Investitionsliste der Wärmepumpe mit `EUR_PRO_KW_ELEKTRISCH`,
  `PruefeFehlende` je Raster; `BetriebskostenBaugroesseTests` Fall „passt nur im Investitionsraster“.

## Schlüssel

**2 neu** (de/en, hinter `KDLG_HERLEITUNG_SOLAR_KW_FLAECHE`): `KDLG_HERLEITUNG_WP_PEL` „{0} kW ÷ COP {1} ({2}) = {3} kW“
und `KDLG_HERLEITUNG_WP_PEL_SUMME` „Σ P_el am Normpunkt von {0} Wärmepumpen = {1} kW“ (en „Σ P_el at the rating point of
{0} heat pumps = {1} kW“). Wiederverwendet: `KDLG_GR_PEL`, `BM_KW_ELEKTRISCH`, `KDLG_BASIS_GRUND_GERAET`. Je Sprache
auf dem Merge 11.011 → 11.013 Einträge, der Designer 11.008 → 11.010 Eigenschaften, wiederholbar (+0).

## Fragen aus der Welle

Entschieden hat der Orchestrator am 25.09.2026 (09:15) mit der Baufreigabe, nach Empfehlung — alle a; **E20‑Q6 ist
offen beim Anwender**, gebaut ist dort die Empfehlung a (→ Register R‑E20).

| Frage | Lesarten | Entscheid |
|---|---|---|
| **E20‑Q1** Bezugsgröße P_el | (a) Ptherm ÷ COP am Normpunkt je Bauart, bei W35 interpoliert; (b) Maximum über die Kennlinie; (c) Auslegungspunkt; (d) `Nennleistung` ÷ COP | a |
| **E20‑Q2** Normpunkt Luft/Wasser | (a) A2/W35; (b) A7/W35 nach EN 14511 (1024: a 4,00 kW, b 4,23 kW) | a |
| **E20‑Q3** Heizstab | (a) nicht einrechnen; (b) einrechnen | a |
| **E20‑Q4** Kühlbetrieb | (a) nur der Heiz-Normpunkt; (b) die Kühlkennlinie mitbetrachten | a |
| **E20‑Q5** Freischaltung | (a) Schalter `investition` in der Landkarte, die Betriebsseite bleibt GEWERK; (b) überall freischalten, die Betriebsauswahl sperren | a |
| **E20‑Q6** „je kWh elektrisch“ im Betriebsraster der Wärmepumpe (Strommenge aus dem Lauf, Grund LAUF) | (a) in E20 nicht anfassen, als Rest von Nr. 10 benennen; (b) aus der Betriebsauswahl nehmen, Bestandszeilen über `benutzt` schützen | **offen beim Anwender** — Empfehlung a, gebaut a |
| **E20‑Q7** Beschriftung | (a) „je kW elektrisch“ unverändert; (b) eigene Beschriftung an der Wärmepumpe | a |
| **E20‑Q8** Grund bei fehlendem Normpunkt | (a) GERAET wiederverwenden; (b) eigener Grund | a |

## Abweichungen und Befunde

1. **P_el kommt aus der Kennlinie, nicht aus einer Gerätespalte** — `Tab_WP` führt keine; `Nennleistung` ist
   thermisch und als Zähler unbrauchbar (Befund 3 oben).
2. **Zwei Katalogtypen ohne Normpunkt** (LS 16-B R nur mit COP 0, test7 ohne Kennlinie) ergeben null mit Grund
   GERAET — der Dialog nennt „es fehlt das Gerät oder seine Angabe“; kein Referenzprojekt ist betroffen.
3. **Kühlbetrieb unter dem Heiz-Normpunkt** — an 1017 liegt P_el im Kühlbetrieb (2,7 bis 4,2 kW) unter den 7,91 kW
   des Heiz-Normpunkts; die Bezugsgröße bleibt der Heiz-Normpunkt (E20‑Q4 a).
4. **Rest von Nr. 10 ist E20‑Q6** — die Betriebsseite der Wärmepumpe ist unverändert, sie bietet weiter „je kWh
   elektrisch“; ob die Art dort bleibt, entscheidet der Anwender.
5. Kein Bericht zeigt ein Investitionsraster; Anhang D ist unberührt. Die KI-Sicht erbt die Regel je Raster über die
   Bemessungsliste der Hülle.

## Nachweis

- **Im Bestand ohne Rechenwirkung:** keine Zeile an der Wärmepumpe trägt `EUR_PRO_KW_ELEKTRISCH`; Anker unberührt;
  Referenzlauf 13/13 gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 4.207.049 Werte, 394/394 CSV byte-gleich;
  Testdatenbank unverändert.
- **A/B 1024** (Arbeitskopie der Testdatenbank, Anlage 11262, Satz 1.000 €/kW; Position 202000001 Kategorie 1,
  202000002 Kategorie 2):

  | Größe | vorher | nachher |
  |---|---|---|
  | Kaskade Kategorie 1 (Menge/Betrag) | null / 0,00 € | 4,00 kW / 4.000,00 €, Herkunft ANLAGE |
  | Investitionssumme | 12.001,00 € | 16.001,00 € |
  | Dialog Kategorie 1 | Grund GEWERK | Herleitung „11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW“ |
  | Kategorie 2 | 0,00 € / GEWERK | 0,00 € / GEWERK (unverändert) |
  | Auswahl der Wärmepumpe | Investition nein, Betrieb nein | Investition ja, Betrieb nein |

- **Phase 1** (Worktree `e20`): Kern-Filter Release 0 Fehler, Windows-Schale x64 0 Fehler; Designer +0;
  SqlDialektPruefer 1.923/0; gefiltert 181/181 (10 Klassen); voller Lauf 14.042 bestanden / 0 Fehler / 2 übersprungen
  (EPOS.Kern 6.837 und 1 übersprungen, EPOS.UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1
  übersprungen); Testhost-Regel eingehalten.
- **Merge** `49ea20e0` auf `pm22b` über `365143e1` ohne Konflikt; Designer 11.010, wiederholbar (+0).
- **Gate:** Kern-Filter 0 Fehler, ChartProben 161/161 gleich der Windows-Messlatte, voller Lauf 14.274 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern 7.069 und 1 übersprungen, EPOS.UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen), Dokumentationswachen 29/29 (`GATE502.log`, 25.09.2026 11:09–11:20 Uhr, auf `5f55764c` = Merge e20 `49ea20e0` über den Push #506 `0d1cec86`)
- **CI:** Kern `main` 36118667674 und Windows `main` 36118667693 auf `f83ce27d` grün; Kern ubuntu 36118663283 vom Nachfolger (#504) abgebrochen

## Abnahme am Gerät (A‑E20‑1, Windows)

1. **Investition:** Projekt 1024 → Kostenverwaltung, Wärmepumpe, Investitionskosten: „je kW elektrisch“ ist wählbar;
   mit 1.000 €/kW steht 4.000,00 €, die Herleitung nennt „P_el der Anlage · 11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW“.
2. **Betrieb:** Betriebskosten der Wärmepumpe — „je kW elektrisch“ steht nicht in der Auswahl.
3. **Sole/Wasser:** Projekt 1017 (mit Kühlbetrieb) — „35,60 kW ÷ COP 4,50 (B0/W35) = 7,91 kW“.
4. **Englische Oberfläche:** dieselben Zeilen auf Englisch.
5. **Bestand:** nach Speichern und erneutem Öffnen unverändert; der Kapitalwert enthält die 4.000 €.

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026), Stichwort `kosten`: „Die Investitionskosten der
Wärmepumpe lassen sich auch je kW elektrisch bemessen; Bezugsgröße ist die elektrische Leistungsaufnahme am Normpunkt
der Kennlinie.“

## Papiere mit der Statuszeile

Konzept (Kopf mit Codestand `49ea20e0` und Zielversion 142, E20 ohne Schritt; Schrittabsatz; § 3.2 Tafel der Runde 1
mit der Zeile `EUR_PRO_KW_ELEKTRISCH` an der Wärmepumpe, nur Kategorie 1, und der Fußnote zur Normpunktregel; § 6.1
Zeile E20; § 6.3 Nr. 10; § 7; Anhang), Register (Kopf, Familientafel, neue Familie R‑E20, R‑Rest Zeile Nr. 10),
Analysepapier (§ 5 Zeile E20 ohne Schemaschritt), Protokoll der Entscheidwege (Kopf von § 8, § 8.42, § 8.43), Mockup
(Kategorie 1: Erklärtafel der Bemessung an der Wärmepumpe, Absatz „Bewerten“; Ressourcentafel; Stand-Absatz),
Update-Papier und die Wiki-Quelle Kosten (Anker `bemessung`: Tafelzeile, Absatz zur Normpunktregel, Halbsatz zur
Betriebskostenseite), Index (Reporting 135 → 136). `Referenzlaeufe/LIESMICH.md` bleibt ohne Nachtrag (kein
Schemaschritt).

## Offen

- **E20‑Q6** — Anwenderentscheid: „je kWh elektrisch“ im Betriebsraster der Wärmepumpe lassen (a, Empfehlung) oder
  entfernen (b); mit ihm ist § 6.3 Nr. 10 geschlossen.
- **Abnahme am Gerät** A‑E20‑1 (fünf Schritte oben).
- **Gate** und **CI** (Nachweis oben).
- Der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).
