# Konzeptqualität und Entscheidungsregister — Wirtschaftlichkeit EPOS-Plan

Prüfstand 19.09.2026 · Zweig `ios_migration_september` · `SchemaStand.Zielversion` = 94 · nur gelesen, nichts geändert.
Führendes Papier: `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` (2 427 Zeilen, im Folgenden **KK**).
Vorarbeit heute: Befundpapier `2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md` (§ 4 Q1–Q25) und Protokoll `ueberholt/Protokolle/Reporting/Pruefung_Mockups_2026-09-19/02_Konsistenz_Papiere.md` — dort Belegtes wird hier nur mit `02/x‑n` zitiert, nicht wiederholt.

## 0 Ergebnis in fünf Sätzen

1. Das Konzept ist fachlich die beste Quelle, die das Vorhaben hat — aber es ist **keine Umsetzungsvorlage**, sondern ein gewachsenes Entscheidungsprotokoll: der Geltungsblock (Z. 25–29 „ausdrücklich nicht implementiert") steht im offenen Widerspruch zu § 6.1, das sechzehn abgeschlossene Etappen führt, und § 7 schlägt mit B5 eine Etappe vor, die seit dem 15.09.2026 als `BhkwWirtschaftlichkeitDialog.razor` (67 kB) im Code liegt.
2. Die Quellenangaben des Kopfes greifen ins Leere: `Codestand 922228a` ist eine Kennung vor dem Umschreiben vom 12.09.2026 (heute `e1c4275e`, im Zweig enthalten), und die beiden Quelldokumente der Quelltabelle — Formelkarte `rechenwege_formelkarte.md` und Feldkarte `b5_feldkarte.md` — **haben das Repositorium nie erreicht**; sie lagen im Sitzungs-Scratchpad `…\665cd065-…\scratchpad\`, das heute keine `.md` mehr enthält.
3. Das Entscheidungsregister umfasst **151 Kennungen aus neun Quellen**, davon sind **41 sachlich offen** und **34 als „offen" geführt, obwohl erledigt oder überholt**; die fünf angefragten Namenskollisionen sind alle bestätigt, zwei davon (K‑1/K1, B‑1/B‑1) benennt das Konzept selbst in Z. 886–889, drei (U‑1/U1, V‑1/V‑1, K8/K‑8) nicht.
4. Umsetzungsblockierend sind nur wenige Entscheide, und sie hängen zusammen: **Q1** (Ablösung des zweiten Mockups) blockiert jede Mockup-gestützte Etappe, **BK1‑3/B7‑3** (KWKG-Pauschale Jahr 0) blockiert Erlösrubrik und Ergebnisansicht zugleich, **Q2/U22** blockiert den BHKW-Dialog, **ND‑S3** blockiert die Betriebskostensätze, **V‑E/V‑4** ist die einzige Etappe mit Rechenwirkung — alles Übrige ist Papierpflege.
5. Empfehlung: das Papier vor der ersten Codeetappe in **drei Teile schneiden** — gültiger Stand (§ 1–§ 3 plus eine kurze Etappentafel), Entscheidungsregister als eigene Tabelle mit Kennungsraum, Geschichte in ein Protokoll unter `ueberholt/Protokolle/Reporting/` — und die Etappenkürzel der drei Papiere (B‑Reihe, W5‑B‑Reihe, V‑Reihe, S‑Reihe, #3xx, Un) über eine Übersetzungstafel zusammenführen; ohne das kostet jede Etappe ihre erste Stunde mit der Frage, was noch gilt.

---

# 1 Entscheidungsregister — konsolidiert und entdoppelt

Legende Stand: **offen** · **entschieden am …** · **erledigt mit #…** · **überholt** (die Frage hat keinen Gegenstand mehr).
Legende Blockade: Etappe / Schemaschritt / Dialog, die ohne den Entscheid nicht beginnen kann; „—" = keine.

## 1.1 KK § 5 — Feld- und Dialogentscheide K1–K11 (Z. 2118–2132)

| Kennung | Frage | Stand | Quelle | Blockiert | Empfehlung |
|---|---|---|---|---|---|
| K1 | Eigenes Feld „Deckung je Modul"? | entschieden (kein Feld, bilanziell) | KK Z. 2122 | — | — |
| K2 | Hilfsenergie-Basis: Weg B fest, A/C nur in der Position? | entschieden; **fachlich überholt** durch #365/#366 (Basis = Endenergiebedarf, Schemaschritt 94) | KK Z. 2123 | — | Zeile auf den Stand nach #365 ziehen |
| K3 | Modusfeld § 9 Nr. 3 | erledigt mit #328 (B6), Schemaschritt 88 | KK Z. 2124 | — | — |
| K4 | Tabellenspalte „Brennstoff" ohne Leseweg | offen (als B5-Teilaufgabe geführt; B5 ist gebaut → **Stand unklar**) | KK Z. 2125, § 7 Z. 2409 | B5-Nachlese | am `BhkwWirtschaftlichkeitDialog` nachmessen, dann streichen oder als Restpunkt führen |
| K5 | Jahresnutzungsgrad bleibt Projektgröße | entschieden | KK Z. 2126 | — | — |
| K6 | WP-Hilfsenergie: Spalte formal für alle, Leser nur BHKW/Kessel | entschieden | KK Z. 2127 | — | — |
| K7 | Schreibweg der drei B3a-Anlagenspalten (8 → 11 Spalten) | **erledigt** (`KwkgAnlagenCtrl.Speichere`, 02/c‑6), im Papier weiter offen | KK Z. 2128, Z. 2246 | — | streichen |
| K8 | Achter Fußleistenknopf | entschieden 18.09.2026 (Umschalter, = V‑1) | KK Z. 2129 | Ergebnisansicht § 2.13 | — |
| K9 | § 6.1 zählt „9 Felder", real 11 | offen (Konzeptkorrektur, nie ausgeführt) | KK Z. 2130 | — | beim Neuschnitt erledigen |
| K10 | Hilfsenergie-Bemessung doppelt (Seed gegen Altkatalog) | erledigt | KK Z. 2131 | — | — |
| K11 | `Views\Wirtschaftlichkeit` unlokalisiert | erledigt mit #328 (B6), Wache steht | KK Z. 2132 | — | — |

## 1.2 KK § 5 — Darstellung, Energieträger, Einheiten (Z. 2136–2190)

| Kennung | Frage | Stand | Quelle | Blockiert | Empfehlung |
|---|---|---|---|---|---|
| D‑1 | Emissionsspalte: eine Spalte nach Modus? | entschieden 30.08.2026; umgesetzt mit B7 (#329) | KK Z. 2138 | — | — |
| E‑1 | Modus `CO2E` ohne gepflegte Einzelarten | entschieden 30.08.2026 | KK Z. 2139 | — | — |
| D‑2 | Erlösdarstellung zwei Blöcke, B nicht addieren | entschieden; umgesetzt mit B7 | KK Z. 2140 | — | — |
| D‑3 | Referenz der Differenzrechnung | entschieden 31.08.2026; **erledigt mit #358** (Schemaschritt 92) | KK Z. 2141 | — | — |
| ET‑D‑1 | Einheit der Preisbestandteile | entschieden 18.09.2026 (a) | KK Z. 2149 | Trägerkarte | — |
| ET‑D‑2 | Inhalt des Emissionsblocks der Trägerkarte | entschieden 18.09.2026 (a) | KK Z. 2150 | Trägerkarte | — |
| ET‑D‑3 | Angebot der Preisbasis | entschieden 18.09.2026 (a) | KK Z. 2151 | Trägerkarte | — |
| UR‑1 | Preisbasis rechnete mit Umrechnungsfaktor statt Heizwert | behoben mit ET‑D; **Restschritt 2 offen** („Nach #341": `ID_Umrechnung` ist Altlast) | KK Z. 2152, Status Z. 400 | Trägerkarte, kleiner Schemaschritt | Restschritt als eigene Kennung `UR‑1b` führen |
| E1 | Energieträger des Elektroheizkessels | entschieden 18.09.2026; erledigt mit #349/#353 | KK Z. 2153 | — | Zeile nach `ueberholt/` |
| U‑1 | Einheitenbruch Gase „m³" → „Nm³" | entschieden 30.08.2026 Weg (a); **Umsetzung nicht freigegeben**; Quellenangabe überholt (§ 2, Punkt 2.8) | KK Z. 2178–2190 | eigener Schemaschritt (heute 95) | Freigabe erbitten oder Punkt nach `ueberholt/` |
| U‑1‑R | Fünf Randfragen: Waise `energy_project_settings` Z. 10076/Projekt 1039, kg-/rm-Abrechnung Brennstoffe 4/5/12, Brennstoff 24 „Sonstige", Fremdkörper Regel 67, Prüfschritt `EnergieEinheitenPruefung` | **offen** | KK Z. 2185–2188 | Pufferspeicher-Strang | als fünf Zeilen in das neue Register, nicht als Fließtext |

## 1.3 KK § 5 R‑U1…R‑U5 ≡ Grundlagen § 6 — rechtliche Unsicherheiten

Diese fünf Zeilen stehen **zweimal wortgleich**: `KK` Z. 2197–2201 und `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` Z. 421–436. Es sind **keine Anwenderentscheide**, sondern Rechtsfragen; sie gehören nicht in dasselbe Register wie K1–K11.

| Kennung | Punkt | Stand | Quelle | Blockiert |
|---|---|---|---|---|
| R‑U1 ≡ Grdl § 6 Nr. 1 | § 53 neben § 53a | offen (Hauptzollamt); im Modell als Auswahl entschärft | KK Z. 2197 / Grdl Z. 421 | produktiver Einsatz |
| R‑U2 ≡ Grdl § 6 Nr. 2 ≡ Grdl § 10 Nr. 5 | § 53a Abs. 3, 4,96 €/MWh | offen (Volltext) — **dreifach geführt** | KK Z. 2198 / Grdl Z. 428, 747 | — |
| R‑U3 ≡ Grdl § 6 Nr. 3 | Ausschluss fossiler flüssiger Brennstoffe | offen (Sekundärquelle); Prüfkette gebaut | KK Z. 2199 / Grdl Z. 430 | — |
| R‑U4 ≡ Grdl § 6 Nr. 4 | EuGH 09.07.2026 Beihilfe | offen (kein Primärbeleg) | KK Z. 2200 / Grdl Z. 433 | — |
| R‑U5 ≡ Grdl § 6 Nr. 5 | Nachfolgeregelung nach 2030 | offen; als Datumsparameter modelliert | KK Z. 2201 / Grdl Z. 435 | — |
| Grdl § 10 Nr. 1–4 | 45‑€‑Mechanismus ETS 2 · § 10 Abs. 3 BEHG · Projektionsbericht 2026 · Enddatum Versteigerung 2026 | offen, **im KK nirgends geführt** | Grdl Z. 736–746 | CO₂-Preispfad § 3.11 |

## 1.4 KK § 2.11.4 — ValERI V‑1…V‑4 und Etappen V‑A…V‑E (Z. 676–691)

| Kennung | Frage | Stand | Quelle | Blockiert | Empfehlung |
|---|---|---|---|---|---|
| V‑1 | Fünf Blöcke aufklappbar oder Umschalter? | entschieden 18.09.2026 (Umschalter) = K8 | KK Z. 688 | Ergebnisansicht | Zeile mit K8 zusammenlegen |
| V‑2 | XLSX: nur ValERI-Blatt oder ganzer Bericht? | **gekippt 18.09.2026** durch V‑G10 | KK Z. 689 | V‑D | als „überholt" kennzeichnen |
| V‑3 | IZF/Amortisation auf den Kacheln? | entschieden (behalten mit Label) | KK Z. 690 | V‑A | — |
| V‑4 | Szenario-Parametersätze sofort oder danach? | entschieden 18.09.2026 (danach, mit Hinweistext § 2.11.7) | KK Z. 691 | V‑E | — |
| V‑A | Ausweis, Deklarationszeilen, Steigungsspalte | **offen, nicht gebaut** | KK Z. 680 | — | erste ValERI-Codeetappe |
| V‑B | Referenzwahl | erledigt mit #358 | KK Z. 681 | — | — |
| V‑C | ValERI-Ansicht, fünf Blöcke + Cashflow | **offen** | KK Z. 682 | Ergebnisansicht § 2.13 | — |
| V‑D | XLSX-Formelbericht + Anhang-D-Gegenprobe | **offen**, Umfang seit V‑G10 größer; Stufenplan in § 2.11.6 | KK Z. 683, 735–796 | nach V‑C | Stufe 0 (Parameterblock) einzeln beauftragen |
| V‑E | Vollständige Szenarioabdeckung | **offen**, einzige Etappe mit Rechenwirkung | KK Z. 684 | nach V‑D | A/B-Nachweis vorsehen |
| V‑G1…V‑G11 | siehe § 1.6 (G‑Reihe des Szenarienkonzepts) — dieselben Kennungen ohne und mit `V‑`-Vorsatz | uneinheitlich | KK § 2.11.2 Z. 640 ff. ↔ Szen § 7.3 | — | **eine** Schreibweise wählen |

## 1.5 KK § 6.3 — offene Punkte (Z. 2242–2352)

**B5-Kernaufgaben (Z. 2246–2248)**

| Nr. | Frage | Stand | Blockiert | Empfehlung |
|---|---|---|---|---|
| 1 | Schreibweg 8 → 11 Spalten (K7) | **erledigt** (02/c‑6) | — | streichen |
| 2 | Live-Frisch-Anzeige der Bezugsgröße | **erledigt** mit U31/#347 und #364 (02/c‑6) | — | streichen |
| 3 | Erste Kostenposition mit Anlagenbezug | erledigt mit B5/#286 ff. | — | streichen |

**Nach B6 (Z. 2252–2257), Nr. 4–9** — alle sechs durchgestrichen, sachlich erledigt. Die Nummer **9 ist doppelt vergeben** (Z. 2257 „9." Altkatalog-Bemessung und Z. 2261 „9a."-Reihe); Zählfehler, kein Sachfehler.

**Nach B7 / BK1 / Anwenderdurchsicht (Z. 2261–2328)**

| Nr. | Frage | Stand | Blockiert | Empfehlung |
|---|---|---|---|---|
| 9a (B7‑1) | A4 und A5 stehen in einer Zeile (`SteuerErgebnis.EnergiesteuerEur` summiert § 53/53a und § 54) | **offen** | Erlösrubrik-Ausbau | zwei Rückgabegrößen; mit 9d bündeln |
| 9b (B7‑2) | Nachweise nicht persistiert | erledigt mit #331 (B7P) | — | — |
| 9c (B7‑3) ≡ 9g (BK1‑3) | KWKG-Pauschale (§ 9) hat keine Rubrikzeile / Jahr‑0‑Ausweis | **offen, Anwenderentscheid** — die **einzige Doppelzeile, die zwei Etappen zugleich blockiert** | Erlösrubrik **und** Ergebnisansicht | zu **einer** Kennung zusammenziehen; U17/#346 hat das Bild schon |
| 9d (B7‑4) | Grund einer Nullzeile nicht durchgereicht | **offen** | Erlösrubrik | mit 9a bündeln (beides `SteuerErgebnis`) |
| 9e (BK1‑1) | Sechs Projektspalten ungelesen | erledigt mit #335 (BK1a, Schritt 90) | — | — |
| 9f (BK1‑2) | Ersatzweg rechnet mit Projektsätzen | erledigt mit #335 | — | — |
| 9h | Nutzungsdauer/Ersatz/Restwert — „fünf fehlende Stücke" | **drei offen** (U39), zwei erledigt mit #357 (02/c‑5) | Ergebnisansicht, ND‑S3 | auf U39 verengen |
| 9i | Erlösrubrik nach Komponente innen | **offen** = Q15 = Mockup U6 | Erlösrubrik | Voraussetzung klären: bleibt der Leistungsanteil projektweit? |
| 9j | Verlauf mit drei Szenarien | **offen** = Ergebnisansicht (5) | Ergebnisansicht | mit K8/V‑1 zusammen beauftragen |
| 9k | Persistenz der Nachweise je Anlage | erledigt mit #331 (B7P) = 9b | — | Zeile streichen |
| 9l (BK1‑4) | `KWKG_Kostenanteil` Projekt ohne Leser | erledigt mit #336 (BK1b, Schritt 91) | — | — |
| 9m (BK1‑Q2) | Vbh-Kontingent leer → 0 h statt 30 000 h | abgenommen 18.09.2026 | — | in ein Protokoll | 

*Die Reihe springt 9a…9g, 9l, 9m, dann 9h…9k — die Buchstaben sind nicht sortiert.*

**Fachlich und technisch (Z. 2332–2342)**

| Nr. | Frage | Stand | Beleg / Blockiert |
|---|---|---|---|
| 10 | Bezugsgrößen der übrigen KD1-Bemessungsarten (H1‑1b) | **offen** | Kostendialog |
| 11 | Nachzieh-Migration für Bestandsprojekte | offen, Option | — |
| 12 ≡ Befund B‑5 | `InvestSummeFuer` auf die Kaskadensumme umbauen | **offen** — `BetriebskostenCtrl.InvestSummeFuer` summiert weiter `EingegebenerWert` (`EPOS.Kern/Controller/BetriebskostenCtrl.cs:36`) | B8; **Rechenwirkung** |
| 13 | Pufferkapazität bleibt null | bewusste Grenze | — |
| 14 ≡ Befund S‑6 | Reduzierter Stromsteuersatz „bleibt Konstante bis zur Katalog-Nachpflege" | **überholt**: `STROMST_REDUZIERT_SATZ` ist gesät (`GesetzKatalog.cs:1156`), die Konstante ist ausdrücklich nur noch Rückfallebene und wertgleich (`StrompreisZerlegungModel.cs:86–88`) | Zeile umformulieren |
| 15 | Bilanzjahr und Unternehmensart wirken erst beim nächsten Öffnen | offen, klein | Dialog |
| 16 | Rückweg „Parameterdialog zeigt den erfassten Preisanteil" | offen | Parameterdialog |
| 17 | Kohärenzzeilen nicht persistiert | erledigt mit B7P; Fall 4 ohne Katalogsatz bleibt still | — |
| 18 (HB1‑O1) | Engine-Sortierung `ORDER BY Prioritaet` | **vermutlich überholt** — `SimulationControl.cs:1512, 3372, 4117` sortieren `ORDER BY Prioritaet, ID`; nicht gegengeprüft, ob dies die gemeinte Stelle ist | nachmessen, dann streichen |
| 19 | Asymmetrie „Wartung BHKW" ↔ „Vollwartung / Wartung Kessel" | offen | Betriebskostenraster |

**Nachweis und Betrieb (Z. 2346–2352)**

| Nr. | Frage | Stand | Blockiert |
|---|---|---|---|
| 20 ≡ B9 ≡ A8 | Zahlenprobe gegen die Altanwendung — „blockiert, wartet auf Zulieferung der BHKW-Plan-Excel" | **Vorbedingung entfallen**: Die Excel liegt auf dem Netzlaufwerk vor (Inventarisierung läuft bei einem anderen Agenten); `Dokumentation/ueberholt/Protokolle/Reporting/Analyse_Altanwendung_BHKW-Plan.md` existiert (20 734 B) | B9 kann geplant werden |
| 21 | Basiswechsel der Referenzläufe; 1030 neu verankern | **teilweise erledigt** mit #333 (neue Basis `2026-09-18_R9_Kesselbrennstoff`, KK Z. 2238); die 1030-Anker fehlen weiter (KK Z. 2240) | B8 |
| 22 | Sichtabnahmen B2, BK1, B4 | offen (Windows) | — |
| 23 | resx-Sammelnachtrag B3a, B3b, B4, F-Serie | offen | — |
| 24 | Datenpflege 1018 / 1024 | offen | Referenzlauf |

## 1.6 Szenarienkonzept § 7.3 (G1–G11) und § 11

| Kennung | Lücke | Stand (Entscheid 09.09.2026) | Quelle | Blockiert |
|---|---|---|---|---|
| G1 | Endjahr je Position | entschieden: **nicht umsetzen**, offenlegen (§ 9.4) | Szen Z. 269 | — |
| G2 | Preisänderung je Kostenart | umgesetzt W5‑B‑12 (nur dritter Satz p_I) | Szen Z. 270 | — |
| G3 | Degradation je Faktor | entschieden: nicht umsetzen — **aber KK § 2.11.4 Z. 684 führt Degradation (V‑G2) als Teil von V‑E**: Widerspruch, s. § 2 Punkt 2.11 | Szen Z. 271 ↔ KK Z. 684 | V‑E |
| G4 | Preisindizierung der Ersatzbeschaffung | umgesetzt W5‑B‑12, Migrationsschritt 72 | Szen Z. 272 | — |
| G5 | Startjahr der Energiekosten | entschieden: nicht umsetzen | Szen Z. 273 | — |
| G6 | Nicht monetisierbare Wirkungen | umgesetzt W5‑B‑12 | Szen Z. 274 | — |
| G7 | Betrachtungszeitraum aus der Nutzungsdauer | umgesetzt W5‑B‑11 | Szen Z. 275 | — |
| G8 | Bandbreite im Bericht | umgesetzt W5‑B‑11; bleibt **Wertfassung** bis V‑D | Szen Z. 276, 568 | V‑D |
| G9 | Entscheidungsempfehlung als Text | umgesetzt W5‑B‑11 | Szen Z. 277 | — |
| G10 | Herleitung Eigennutzung/Einspeisung im Bericht | umgesetzt W5‑B‑11 — **nicht zu verwechseln mit V‑G10** (Formelbericht, Entscheid 18.09.2026) | Szen Z. 278 ↔ KK Z. 735 | — |
| G11 | Investitionsgekoppelte Betriebskosten im Szenario | umgesetzt W5‑B‑11 | Szen Z. 279 | — |
| § 11 (1) | V‑4 Hinweistext | entschieden 18.09.2026, **nicht umgesetzt** | Szen Z. 554–561 | Seite |
| § 11 (2) | K‑8/V‑1 Umschalter | entschieden, **nicht umgesetzt** | Szen Z. 562–566 | Ergebnisansicht |
| § 11 (3) | V‑G10 ganzer Bericht formelbasiert | entschieden, **nicht umgesetzt** | Szen Z. 567–572 | V‑D |

## 1.7 Nutzungsdauer-Konzept § 4 (ND‑Q1…Q8) und § 6

| Kennung | Frage | Stand | Quelle | Blockiert |
|---|---|---|---|---|
| ND‑Q1…ND‑Q7 | Quelle, Schlüssel, Ort, Bestand, Auslieferungszeilen, Spalten, Gerätekataloge | **alle entschieden 14.09.2026 nach Empfehlung** | ND Z. 237–249 | — |
| ND‑Q8 | Startwerte der Tabelle 2.6 | entschieden 14.09.2026: als „Richtwert" ausliefern, editierbar; **Normabgleich bleibt beim Anwender** | ND Z. 237–239, 250 | Saatwerte |
| ND‑A (S1) | Tabelle und Verwaltung | umgesetzt | ND Z. 225, 261 | — |
| ND‑B (S2) | Vorbelegung, Knopf, Positionsart, Tafel | umgesetzt mit #357 | ND Z. 226, 262 | — |
| ND‑S3 | Instandsetzung/Wartung, Gerätekataloge | **offen — eigener Anwenderentscheid**; danach Betriebskostensätze je Technik statt Konstanten, **neue Referenzbasis nötig** | ND Z. 227, 263 | Betriebskosten, Referenzbasis |
| ND‑R1 | Hülle der Kostenverwaltung bleibt in der Windows-Schale (1 107 Zeilen) | **offen** = Q14 = „Nach #357 (a)" | ND Z. 229–231 | iOS |
| ND‑R2 | U39: Entkopplung Ersatz/Restwert, geräteeigene Dauerspalten, Speicherflotte | **offen** = KK 9h | ND Z. 231–233 | Ergebnisansicht |

## 1.8 Statusdatei „Nach #340" bis „Nach #369" — Punkte mit Anwenderentscheid

Nur die Punkte, die ausdrücklich einen Anwenderentscheid verlangen, und nur soweit sie die Wirtschaftlichkeit betreffen.

| Kennung | Frage | Stand | Quelle | Blockiert |
|---|---|---|---|---|
| N341‑1 | `UR‑1` Schritt 2: `ID_Umrechnung` als Altlast ablösen | offen | Status Z. 400 | Trägerkarte, Schemaschritt |
| N342‑1 | U26-Folge: Bericht und Herleitung zeigen weiter zwei Nachkommastellen | offen | Status Z. 402 | Bericht |
| N343‑1 | U28–U31 im Mockup-Anhang | erledigt mit #345/#347 | Status Z. 404 | — |
| N344‑1 ≡ Q1 | `Ergebnis_Bandbreite_Herkunft.html` abnehmen oder ablösen | **offen — blockiert die Mockup-Abnahme insgesamt** | Status Z. 406, 02/e‑8 | jede Mockup-gestützte Etappe |
| N345‑1 | U30 (Gruppe Ersatz/Restwert) wartet auf ND‑S3 | offen | Status Z. 407 | ND‑S3 |
| N345‑2 ≡ Q23 | Logbuchsatz und Versionsnummer | offen | Status Z. 407, 425 | Release |
| N346‑1 | Ergebnisansicht § 2.13 (Umschalter, Verlauf) nicht umgesetzt | offen | Status Z. 408 | Ergebnisansicht |
| N347‑1 | Elektrokessel-Ausnahme als Sonderzeile? | **offener Anwenderentscheid** | Status Z. 409 | Endenergie-Auflöser |
| N348‑1 | Abschnitt 8 mit altem PV-Barwert | erledigt mit #351 | Status Z. 410 | — |
| N349‑1 | Eigener Heizstromtarif je elektrischem Verbraucher | **entschieden 18.09.2026: nein** (#353) | Status Z. 411 | — |
| N352‑1 | Mockup Abschnitt 7: Spalte „Satz · Herkunft" | erledigt mit #356 (02/c‑10) | Status Z. 414 | — |
| N353‑1 | Rangfolge im Wiki beschreiben | offen, klein | Status Z. 415 | Wiki |
| N354‑1 | Vergleichssicht setzt Referenzparameter voraus | erledigt mit #358 | Status Z. 416 | — |
| N355‑1 | Vergütung je Variante umsetzen | erledigt mit #359 | Status Z. 417 | — |
| N356‑1/2 | Ressourcenschlüssel des Mockups | erledigt mit #362 | Status Z. 418 | — |
| N357‑1 ≡ Q14 | Hülle der Kostenverwaltung nach `EPOS.UI.Daten` | **offen** | Status Z. 419 | iOS |
| N357‑2 ≡ U39 ≡ 9h | Entkopplung Ersatz/Restwert u. a. | offen | Status Z. 419 | Ergebnisansicht |
| N358‑1 | Verlaufs- und Differenzkurven der Sicht 2 im Bericht | offen (eigene Welle) | Status Z. 420 | Bericht |
| N358‑2 | Sitzungswahl der Sicht 2 nicht persistiert | Entwurfsentscheid | Status Z. 420 | — |
| N360‑1 | Übernehmende Variante ohne eigene Zeile hinterlässt keine Spur | **offen** | Status Z. 422 | Projekttransfer |
| N361‑1 | Rasterfußzeile bei hoher Schrift abgeschnitten | offen, klein | Status Z. 423 | Kostenraster |
| N362‑1 | Tafelzeilen „geplant · Un" mit der Umsetzung nachziehen | offen (Daueraufgabe) | Status Z. 424 | jede Etappe |
| N363‑1 | Katalogmodus schaltet nach OK auf die Zielkategorie | Entwurfsentscheid | Status Z. 425 | — |
| N364‑1 | Bezugsgrößen | erledigt mit #365 (Schemaschritt 94) | Status Z. 426 | — |
| N364‑2 | Dubletten in der Anwenderdatenbank („später prüfen") | **offen** | Status Z. 426 | Datenpflege |
| N366‑1 | Vorlagenhinweis vergleicht nur die Bemessung | offen, eigener Auftrag | Status Z. 428 | Kostendialog |
| N369‑1 | Q1–Q25 des Befundpapiers | **offen — 25 Entscheide auf einmal** | Status Z. 431 | Papierpflege P1–P9 |

## 1.9 Befundpapier § 4 — Q1–Q25

Vollständig in `2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md` Z. 332–358; alle **25 offen**. Für das Register zählt die Zuordnung zu Blockaden und die Entdopplung:

| Q | Deckt sich mit | Blockiert | Kritikalität |
|---|---|---|---|
| Q1 | N344‑1, 02/e‑1…e‑8 | **alle Mockup-gestützten Etappen** | **hoch** |
| Q2 | 02/d‑2, Mockup U22 | BHKW-Dialog (B5-Nachlese) | **hoch** |
| Q3, Q4, Q5, Q6, Q17 | Beispielprojekt/Rechenwege | Zahlenprobe, B9 | mittel |
| Q7 | Formelzeile Trägerkarte `N4` | Trägerkarte (Code) | mittel |
| Q8, Q9, Q13 | Hausstil § 2.7 | **alle neuen Dialoge** | **hoch** |
| Q10, Q11, Q12 | Kataloge, Tarifstruktur, PV-Sprung | Dialoge | gering |
| Q14 | ND‑R1, N357‑1 | iOS | mittel |
| Q15 | 9i, Mockup U6 | Erlösrubrik | **hoch** |
| Q16 | Zellensemantik | Ergebnisansicht, Bericht | mittel |
| Q18, Q19, Q20 | Seite und Parameterdialog | Ergebnisansicht | gering |
| Q21 | Umzüge nach `ueberholt/` | Ordnung | mittel |
| Q22 | 02/g‑5, 02/g‑6 | Mockup offline | gering |
| Q23 | N345‑2 | Release | gering |
| Q24 | Gestaltungsfamilien | nur für bleibende Mockups | gering |
| Q25 | Kopfzeile Codestand — s. § 2 Punkt 2.2 | Kopfzeile | mittel |

## 1.10 Doppelte Kennungen und Namenskollisionen

Alle fünf angefragten Kollisionen sind bestätigt. **Zwei benennt das Konzept selbst** (Z. 886–889 „Namensvorsicht"), **drei nicht**.

| Kollision | Bedeutung A (Beleg) | Bedeutung B (Beleg) | im Papier benannt? |
|---|---|---|---|
| **K‑1 ↔ K1** | Befund: zweiter Fall § 2 Nr. 16 KWKG, Abwärmeabfuhr (KK Z. 2111, § 3.6) | Entscheidung: Feld „Deckung je Modul" (KK Z. 2122) | **ja**, Z. 886–887 |
| **B‑1 ↔ B‑1** | Stromsteuer-Erlösreihe § 9 Nr. 3, erledigt mit B6 (KK Z. 1962, Rechenweg 05/07) | Kessel-Modulspalte `Verbrauch`, erledigt mit #331 (KK Z. 2089) | **ja**, Z. 887–889 |
| **U‑1 ↔ U1** | Einheitenbruch Gase „m³/Nm³", Schemaschritt (KK Z. 2178) | Mockup-Anhang U1 = Abwärmeabfuhr/Stromkennzahl σ, also Befund K‑1 (`Mockups/Dialog_Formel_Zahlenprobe.html` Z. 2708–2709, 4357) | **nein** |
| **V‑1 ↔ V‑1** | ValERI-Entscheidungsfrage „Umschalter", entschieden (KK Z. 688) | Befund: EV-Rundung, EvMix unrundet gegen gerundeten Erlös (KK Z. 2113) | **nein** |
| **K8 ↔ K‑8** | Entscheidung „achter Knopf" (KK Z. 2129, 2425) | dieselbe Sache, aber mit Bindestrich geschrieben (KK Z. 469, 1011; Status #332) — verletzt die eigene Regel „Befund mit Bindestrich" | **nein**, und **regelwidrig** |

Weitere Kennungsprobleme, ungefragt gefunden:

| Problem | Beleg |
|---|---|
| `G10` (Szenarienkonzept: Herleitung Eigen/Einspeisung, umgesetzt) gegen `V‑G10` (KK: ganzer Bericht formelbasiert, Entscheid 18.09.2026) — die `V‑`-Vorsätze werden uneinheitlich gesetzt | Szen Z. 278 ↔ KK Z. 735 |
| `D‑1` (Emissionsspalte, KK Z. 2138) gegen `d‑1` (Befund des Prüfprotokolls) | KK Z. 2138 ↔ 02/d‑1 |
| Nummer **9 doppelt** in § 6.3 (Z. 2257 und Z. 2261 ff.) | KK Z. 2257, 2261 |
| Buchstabenreihe 9a…9g, **9l, 9m**, dann 9h…9k — unsortiert | KK Z. 2288–2328 |
| Statusnummern **#302, #304, #328, #331 je doppelt vergeben** (zwei Rechner) — jede Zeile trägt den Zusatz, aber ein Verweis „#331" allein ist mehrdeutig | Status Z. 224, 228, 249 ff. |

---

# 2 Innere Widersprüche und überholte Aussagen

Über `02/b, c, d, f` hinaus. Jede Zeile mit Zeilennummer und Berichtigungsvorschlag.

## 2.1 Geltungsblock gegen § 6.1 — der schwerste Widerspruch

| Ort | Aussage | Gegenbeleg | Vorschlag |
|---|---|---|---|
| KK Z. 25–29 | „**Arbeitsregel (Anwender, 30.08.2026): erst das Konzept, keine Umsetzung.** Sämtliche hier beschriebenen Vorhaben — die Erlösrubrik (§ 2.6), die Emissionsspalte (§ 2.5), die Befunde aus § 4 — sind **zur Abnahme gedacht und ausdrücklich nicht implementiert**." | § 2.5 Überschrift Z. 300 „— **umgesetzt**"; § 2.6 Überschrift Z. 349 „— **umgesetzt**"; § 6.1 Z. 2223 führt B7 als abgeschlossene Etappe mit PASS-Referenzlauf; § 4 markiert B‑1 (Z. 2089), N1 (Z. 2101) und N3 (Z. 2103) mit ✔ „erledigt" | Absatz ersetzen: „Stand der Umsetzung: § 6.1. Was hier als Soll steht, ist gebaut, sofern § 6.1 die Etappe führt." Die Arbeitsregel selbst gehört als Datumseintrag ins Protokoll |
| KK Z. 16–23 | „Seit der Konsolidierung vom 02.09.2026 ist es die führende Fassung … Die drei Quellen bleiben als Historie stehen" | zwei der drei liegen inzwischen in `ueberholt/`, die dritte in `aktuell/` — s. § 4 | Satz um den heutigen Ablageort ergänzen |

## 2.2 Kopfzeile „Codestand 922228a"

- `922228a` existiert im heutigen Zweig **nicht** (`git cat-file -t 922228a` → „Not a valid object name"): Die Geschichte wurde am 12.09.2026 umgeschrieben.
- Übersetzung über `Dokumentation/ueberholt/Geschichte/commit-map_2026-09-12.txt`: `922228a0…` → **`e1c4275e`** („Synchronisation vom 02.09.2026 18:45"), und dieser Commit **existiert und ist Vorfahr von HEAD**. Die Kennung ist also nicht falsch, nur nicht mehr auflösbar.
- Dasselbe gilt für `b2ad3e3` in der Quelltabelle (Z. 33) → **`2cfb871d`** („F2/B4: resx-Sammelnachtrag", 30.08.2026), ebenfalls Vorfahr von HEAD.
- **Vorschlag** (deckt sich mit Q25): Codestand aus der Kopfzeile streichen. Wenn er bleiben soll, dann als übersetzte Kennung `e1c4275e` mit dem Zusatz „vor dem Umschreiben `922228a`" — sonst steht in einem gültigen Papier eine Kennung, die kein Werkzeug mehr auflöst.

## 2.3 Quelltabelle Z. 31–38 — zwei Quellen existieren nicht

| Zeile | Quelle | Befund |
|---|---|---|
| Z. 33 | Formelkarte `rechenwege_formelkarte.md` (liefert „§ 3 vollständig, § 4") | **Nicht im Repositorium** — weder in `Dokumentation/`, noch sonst im Arbeitsbaum, noch in der Git-Geschichte (`git log --all -- '*formelkarte*'` → leer). Der einzige Fundort ist `Dokumentation/ueberholt/KOORDINATION_Parallelsession_2026-08-30.md:34`: die Datei lag im Sitzungs-Scratchpad `…\C--Waermeplan-WP-Plan-WindowsFormsApplication1\665cd065-…\scratchpad\`. Dieses Verzeichnis existiert noch, enthält aber **keine `.md` mehr** |
| Z. 34 | Feldkarte `b5_feldkarte.md` (liefert „§ 2 vollständig, § 5") | Dasselbe (`KOORDINATION…:36`, `…:120`, `B5b_Blazor_Port_Protokoll.md:145` „Soll = `b5_feldkarte.md` § 1") |

**Folge:** § 2, § 3, § 4 und § 5 — der fachliche Kern des Papiers — nennen als Herleitung zwei unauffindbare Dateien. Sämtliche Befunde aus § 4 („Aus der Abnahmeliste der Formelkarte", Z. 2072) sind damit **nicht mehr gegen ihre Quelle prüfbar**. `Werkzeuge/Formularkarte/FeldkarteSchreiber.cs` ist ein Werkzeug mit ähnlichem Namen, **nicht** diese Datei.
**Vorschlag:** Quelltabelle auf die drei Repo-Papiere und die Protokolle kürzen; die zwei Kartenzeilen mit dem Vermerk „nur noch als Herkunftsangabe, Datei nicht erhalten" führen oder streichen. Wo § 4 noch gilt, gehört der Beleg an die Codestelle, nicht an die Karte.

## 2.4 „Begleitende Artifacts" Z. 40–52 — sechs Links, fünf abgelöst

Die Prüfung hat nur die Stände verglichen (02/g‑6); offen bleibt die **Ablösefrage**:

| Artifact | Inhalt | durch welche Repo-Datei abgelöst |
|---|---|---|
| `e928091e…` B5-Dialogmockup | § 2 | `Mockups/Dialog_Formel_Zahlenprobe.html` Kat. 5 **und** der gebaute `BhkwWirtschaftlichkeitDialog.razor` — **doppelt abgelöst** |
| `588f6e21…` Rechenwege | § 3, § 4 | `Wirtschaftlichkeit_Kosten/Rechenweg/01…08` (acht Papiere) |
| `d924b2ec…` Erlösrubrik BHKW | § 2.6 | `Rechenweg/07_Erloesrubrik.md` + B7 gebaut |
| `236c8a8a…` Pflichtpositionen | § 3.4, H1 | § 2.8 („Entwurf B ist als § 2.8 übernommen" — steht dort selbst) |
| `f8968739…` ValERI Höfingen | § 2.9–2.11 | `Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md` (Höfingen-Gegenprobe) |
| `739d3cca…` Dialog/Formel/Zahlenprobe | § 2.12 | `Mockups/Dialog_Formel_Zahlenprobe.html` — **der einzige, der noch gebraucht wird**, und er trägt den Stand vor #351 |

**Vorschlag:** Abschnitt auf **eine** Zeile eindampfen (739d3cca, mit dem Vermerk „Repo-Datei führt"), die übrigen fünf in das Protokoll. Die Einleitung „maßgeblich ist dieses Dokument" wird damit gegenstandslos.

## 2.5 § 2.2 „neu (BW9)" (Z. 119)

- Die Überschrift nennt den Dialog **neu** und die Leitentscheidung **BW9**. `BW9` stammt aus `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` (Quelltabelle Z. 35 „Leitentscheidungen BW1–BW10") — einem Papier, das heute in `ueberholt/` liegt. Das Kürzel wird im ganzen KK sonst nicht aufgelöst.
- Der Dialog ist gebaut: `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor` (67 110 B, zuletzt 18.09.2026), Statuszeile **#286** (15.09.2026) behandelt ihn bereits als Bestand, #330/#342/#352 haben ihn weiter geändert.
- **Vorschlag:** „neu (BW9)" streichen; Überschrift auf `BhkwWirtschaftlichkeitDialog` (mit `Form_BhkwWirtschaftlichkeit` als Altname) und Gruppenzahl nach 02/d‑1. Wenn BW-Kürzel bleiben sollen, gehört eine Auflösungstafel ins Papier — sonst ist die Leitentscheidung nur in `ueberholt/` nachlesbar, was CLAUDE.md Z. 241–242 („`ueberholt/` nie Regelquelle") widerspricht.

## 2.6 § 6.3 Nummernraum (Z. 2242–2352)

Bereits in § 1.5 im Einzelnen. Zusammengefasst: von **37 Punkten** sind nach Statusdatei und Code **19 erledigt oder überholt**, davon **9 nicht als erledigt gekennzeichnet** (B5‑1, B5‑2, B5‑3, K7, 9k als Dublette von 9b, Nr. 14, Nr. 17-Rest, Nr. 18, Nr. 20-Vorbedingung, Nr. 21-Teil). Die Zählung springt (9 doppelt; 9a…9g, 9l, 9m, 9h…9k).
**Vorschlag:** Erledigte Punkte in das Protokoll, offene neu und lückenlos durchnummerieren; die Durchstreichungen (Z. 2252–2257, 2264, 2288, 2294, 2302, 2326, 2339) sind Geschichte, nicht Stand.

## 2.7 § 6.4 Fallstricke (Z. 2354–2364) — fünf von sieben gegenstandslos

| Fallstrick | Befund heute | Vorschlag |
|---|---|---|
| ACE: `UPDATE … WHERE x IN (SELECT …)` trifft still 0 Zeilen | Datenhaltung ist SQLite (`CLAUDE.md` Z. 66 „Datenhaltung: SQLite"); `Microsoft.ACE.OLEDB` kommt im ganzen Baum nur noch in `Referenzlauf/Migrationslauf.cs` und `Referenzlauf/Referenzlauf.csproj` vor | auf „gilt nur für den Migrationslauf" einschränken |
| ACE: falscher Spaltenname meldet fehlenden Parameter | ebenso | ebenso |
| Zwei gemischte ACE-Verbindungen | ebenso | ebenso |
| `SetzeBetrag` ist ein Upsert | weiterhin gültig | behalten |
| Visual Studio regeneriert `Resource.Designer.cs` | weiterhin gültig | behalten |
| „Keine `.cs` unterhalb von `WindowsFormsApplication1\` (CS0017); Harnesse nach `dev\`" | **`WindowsFormsApplication1\dev\` existiert nicht** (Ordnerliste: Allgemein, Controller, Dienste, Properties, Resources, Views, wwwroot, bin, obj) | streichen oder auf den heutigen Ort ziehen |
| „Build nur über das MSBuild von Visual Studio, x64 — `dotnet build` scheitert an COM" | **überholt**: `global.json` fordert SDK 10.0.400; `CLAUDE.md` Z. 99–100, 112 nennt ausdrücklich `dotnet build WP-Plan.sln -c Debug -p:Platform=x64` als den Weg | streichen |

## 2.8 § 6.5 Doppelte Wahrheiten (Z. 2366–2401)

| Zeile | Aussage | Befund | Vorschlag |
|---|---|---|---|
| Z. 2394 | Stromsteuersatz an zwei Orten, „gekoppelt ist nichts" | gilt weiter; der **reduzierte** Satz ist inzwischen gesät (`GesetzKatalog.cs:1156`) und die Konstante ausdrücklich als Rückfall deklariert (`StrompreisZerlegungModel.cs:84–88`) — § 6.3 Nr. 14 und Befund S‑6 sagen das Gegenteil | Zeilen zusammenführen |
| Z. 2398 | „die gespeicherte Access-Abfrage kennt die neuen Spalten nicht" | Access ist abgelöst; `Umsetzungskonzept_iOS_EPOS-Plan.md:208` zählt die gespeicherten Abfragen als Altbestand des eingefrorenen Access-Zweigs | als Geschichte kennzeichnen oder streichen |
| Z. 2399 | „Komponenten-IDs hart verdrahtet gegen dynamisch gelesen — `Form_Kosten` gegen `UcBkKosten`" | **beide Klassen sind im Produktcode nicht mehr vorhanden**: `Form_Kosten*` existiert nur als Prüfmuster unter `Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/`; `UcBkKosten` hat im gesamten Baum **null** Treffer in `.cs`/`.razor` (nur Erwähnungen in `Entscheidungsregister_iOS_EPOS-Plan.md` und `Lokalisierung_Katalog.md`) | am Razor-Bestand neu messen; bis dahin als „Stand vor dem Razor-Port" kennzeichnen |
| Z. 2400 | „Vorrang Projekt vor Katalog in drei Implementierungen … eine Access-Abfrage" | dritter Ort entfällt mit Access | auf zwei Orte kürzen |

## 2.9 § 7 Reihenfolge B5–B9 (Z. 2405–2417)

| Zeile | Etappe | Befund | Vorschlag |
|---|---|---|---|
| Z. 2409 | **B5** als *vorgeschlagene* Etappe | **gebaut** — `BhkwWirtschaftlichkeitDialog.razor` seit spätestens #286 (15.09.2026); #330, #342, #352 haben daran weitergearbeitet (02/d‑1 nennt dasselbe aus der Mockup-Sicht) | Zeile nach § 6.1 verschieben |
| Z. 2410 | **B6** „**umgesetzt**" | richtig (#328, Schemaschritt 88) — **aber B6 fehlt in § 6.1**, s. § 5 | Zeile nach § 6.1 |
| Z. 2411 | **B7** „umgesetzt" | richtig (#329); steht bereits in § 6.1 | Zeile nach § 6.1, Dublette auflösen |
| Z. 2412 | **B8** „Befunde abarbeiten: I‑1, I‑3, **B‑1/N1**, **N3**, **V‑3**, S‑2" | **drei von sechs sind laut § 4 desselben Papiers erledigt**: B‑1 ✔ (Z. 2089, #331), N1 ✔ (Z. 2101), N3 ✔ (Z. 2103); V‑3 ist laut Z. 2112 teils behoben (KWKG-Pauschale hat eine Spalte, die PV-Reihe nicht). Offen bleiben **I‑1, I‑3, S‑2** und der PV-Teil von V‑3 | B8 auf vier Punkte kürzen; Befund B‑5/§ 6.3 Nr. 12 aufnehmen, der Rechenwirkung hat |
| Z. 2413 | **B9** „sobald die Excel vorliegt" | **Vorbedingung entfallen** — die Excel der Altanwendung liegt auf dem Netzlaufwerk vor und wird von einem anderen Agenten inventarisiert; `ueberholt/Protokolle/Reporting/Analyse_Altanwendung_BHKW-Plan.md` existiert | B9 planbar machen; Prüfumfang gegen die § 5-Tafel der Grundlagen festlegen (s. § 4) |
| Z. 2415–2417 | „B5 bleibt ergebnisneutral … Die erste gewollte Ergebnisänderung kommt mit B6" | Als Reihenfolgebegründung überholt: B5, B6, B7, BK1, BK1a, BK1b, VG, VV und die Hilfsstrom-Umstellung sind gelaufen | Absatz ersetzen durch die Reihenfolge der *heute offenen* Etappen (V‑A…V‑E, U39, Erlösrubrik-Ausbau, ND‑S3, B8, B9) |
| Z. 2419–2427 | „Voraussetzungen vor der Umsetzung — **zwei** Entscheidungen … beide sind inzwischen gefallen" | richtig, aber gegenstandslos: der Abschnitt hat nach dem Fall der Entscheide keine Funktion mehr, und die **wirklichen** heutigen Voraussetzungen (Q1, Q2, Q8/Q9, 9c/9g) stehen nirgends | Abschnitt durch die heutigen Voraussetzungen ersetzen |

## 2.10 § 5 U‑1: „liegt nur auf Zweig `claude/lucid-cori-a9a425`" (Z. 2189–2190)

Drei Aussagen, alle überholt:

| Aussage | Befund |
|---|---|
| „liegt derzeit **nur** auf Zweig `claude/lucid-cori-a9a425`" | **Der Zweig existiert nicht mehr** — `git branch -a` führt: `ios_migration_september`, `main`, `origin/{Pufferspeicher, b5b_lokal, claude/epos-plan-ios-concept-glvd1x, claude/happy-boyd-02b0a2, ios_migration, ios_migration_september, kostenformulare, lokal_dirk, main, pv-ertragsmodell-rechner2, sicherung-lokal, sqlite, version_august_2026}`; kein `lucid` |
| „noch nicht gemergt" | **doch**: `Dokumentation/ueberholt/Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md` liegt im Arbeitsbaum, verschoben mit `f8a88904d` (12.09.2026, „#241 alle Markdown-Dokumente nach Dokumentation/aktu…"); die genannten Commits sind übersetzbar und Vorfahren von HEAD: `37bd068` → `49a37beb` („Konzept Einheitenbruch Energietraeger: drei Loesungswege", 30.08.2026), `8e34222` → `8d78218d` („Einheitenbruch-Konzept: Anwenderentscheidung 30.08.2026") |
| implizit: das Papier ist Arbeitsgrundlage | es liegt in **`ueberholt/`** und ist nach `CLAUDE.md` Z. 241–242 „nie Regelquelle". Das KK stützt eine **offene** Entscheidung (U‑1 plus fünf Randfragen) auf ein Geschichtspapier — ein echter Regelbruch |
| Verwechslungsgefahr | `Dokumentation/aktuell/Konzept_Einheiten_EPOS-Plan.md` **existiert** (Indexzeile `LIESMICH.md:53`, „Energieeinheiten: Inventar, kWh gegen MWh, Stufenplan", 2026‑09‑07) — ein **anderes** Papier. Beide werden im Sprachgebrauch „Einheitenkonzept" genannt |

**Vorschlag:** Zweig- und Commitangabe streichen; auf `ueberholt/Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md` verweisen und ausdrücklich sagen, dass die fünf Randfragen aus dem Geschichtspapier **hierher übernommen** sind (dann ist das KK die Regelquelle). Ist U‑1 nicht mehr gewollt, wandert der ganze Block in das Protokoll.

## 2.11 Weitere innere Widersprüche

| Ort | Widerspruch | Vorschlag |
|---|---|---|
| KK Z. 684 ↔ Szen Z. 271 | V‑E enthält laut KK „**Degradation (V‑G2)**"; das Szenarienkonzept hat `G2` = Preisänderung je Kostenart (umgesetzt) und `G3` = Degradation (**entschieden: nicht umsetzen**). Entweder ist die Nummer falsch oder der Entscheid vom 09.09.2026 ist gekippt | Nummer und Entscheidstand klären; dies ist der einzige gefundene **fachliche** Widerspruch zwischen KK und Nebenkonzept |
| KK Z. 2124 (K3) ↔ Z. 2180–2183 (U‑1-Folge) | Z. 2182 sagt, die Nennung „Schritt 62" für M‑3/§ 9 Nr. 3 sei „auf **63** nachzuziehen"; tatsächlich ist B6 mit **Schemaschritt 88** gebaut (Z. 2124, Z. 2410). Der Nachzieh-Auftrag ist gegenstandslos | Absatz streichen (ergänzt 02/b‑11) |
| KK Z. 2123 (K2) ↔ § 2.2 / #365 | K2 sagt „Weg B (% des **Bedarfs**)" und „Wege A und C nur in der Kostenposition"; seit #365/#366 ist die Bemessung „% des **Endenergiebedarfs** der Anlage" mit Schemaschritt 94 und eigenem Trägerpreis je Anlage | K2 auf den Stand nach #366 ziehen |
| KK Z. 2130 (K9) | K9 („§ 6.1 zählt 9 Felder, real 11") verweist auf einen Satz, der in § 6.1 heute **nicht mehr steht** (die Tabelle Z. 2209–2228 nennt keine Feldzahl) | K9 streichen |
| KK Z. 2072 | „Aus der Abnahmeliste der Formelkarte" | Quelle nicht auffindbar (§ 2.3) |
| KK § 2.13 Z. 898 ↔ Z. 899–1017 | „die **fünf** Punkte" gegen sechs geführte Punkte — 02/d‑19 nennt das; **zusätzlich** trägt derselbe Abschnitt die Überschrift „Anwenderdurchsicht 18.09.2026", also ein Datum im gültigen Teil | Punkt (6) als Verweis, Datum ins Protokoll |

## 2.12 Übersetzungstafel der Etappenkürzel

Fünf Kürzelräume laufen nebeneinander; keiner ist irgendwo aufgelöst. Vorschlag für eine Tafel im neuen Papier:

| Konsolidiertes Konzept | Szenarienkonzept | Nutzungsdauer | Statuszeile | Gegenstand |
|---|---|---|---|---|
| W4 E1–E8, L12/L13 | — | — | vor #300 | Gesetzeskatalog, Tarif-Rollenmodell |
| K1–K6 · KD1–KD6 · P1–P6 · H1–H4b, H21 | — | — | vor #300 | Alttabellen, Kostendialoge, PV, Pflichtpositionen |
| B1 · B2 · B3a · B3b · B4 | — | — | vor #300 | Zahlenprobe, Schema 60/61, Hilfsstrom, Stromsteuer |
| **B5** | — | — | **#286 ff.** | `BhkwWirtschaftlichkeitDialog` |
| **B6** | — | — | **#328** (anderer Rechner) | § 9 Nr. 3 als Ausweis, Schemaschritt 88 |
| **B7** | — | — | **#329** | Erlösrubrik, Energiekosten je Anlage, Emissionsspalte |
| **B7P** | — | — | **#331** (anderer Rechner) | Nachweisumschlag `Nachweis_Json` |
| — (Befund B‑1) | — | — | **#331** (dieser Rechner) | Kessel-Modulspalte `Verbrauch`, neue Basis **#333** |
| **BK1** | — | — | **#330** | KWK-Zuschlag an der Anlage, Schemaschritt 89 |
| **BK1a** | — | — | **#335** | Schritt 90, virtuelle Gesamtanlage |
| **BK1b** | — | — | **#336** | Schritt 91, `KWKG_Kostenanteil` entfällt |
| **VG** (§ 2.9, § 2.15) | — | — | **#358** | Vergleichsprojekt, Schritt 92 |
| **VV** (§ 2.16) — *fehlt in § 6.1* | — | — | **#359** | Vergütung je Variante, Schritt 93 |
| *(namenlos)* — *fehlt in § 6.1* | — | — | **#365/#366** | Hilfsenergie am Endenergiebedarf, Schritt 94 |
| **B8** | — | — | offen | Befunde I‑1, I‑3, S‑2, V‑3-Rest |
| **B9** ≡ A8 | — | — | offen | Zahlenprobe gegen die Altanwendung |
| **V‑A…V‑E** (§ 2.11.4) | W5‑B‑9…W5‑B‑12 (§ 2, § 7, § 9, § 10) | — | **keine Statuszeile in #300–#369** | ValERI: W5‑B‑9/10/11/12 sind gebaut (Migrationsschritte 71, 72), V‑A/V‑C/V‑D/V‑E offen |
| § 2.13 Punkte (1)–(6) | — | — | #332, #346, #354 | Ergebnisansicht |
| § 6.3 Nr. 9h | — | **S2-Rest / U39** | **#357** | Nutzungsdauer, Ersatz, Restwert |
| — | — | **S1 · S2 · S3** | S1 vor #300, S2 = #357, S3 offen | AfA-Tabelle |
| Mockup-Anhang **U1…U40** | — | — | #342 ff. | Umsetzungsstand je Bildstelle; U1 = Befund K‑1 |

---

# 3 Eignung als Umsetzungsvorlage

## 3.1 Was ein Umsetzer braucht — und ob das Papier es liefert

Gemessen am ganzen Papier (2 427 Zeilen): **33** Nennungen „Schemaschritt", **37** verschiedene Ressourcenschlüssel, **11** Testklassennamen, **9** Nennungen „Referenzlauf", **12** „Abnahme", **2** Nennungen „Wiki" (beide in § 2.16, Z. 1345–1346).

| Kriterium | Stand | Beleg |
|---|---|---|
| **Abnahmekriterien** je Etappe | *liefert teilweise* — für die **gelaufenen** Etappen als Ergebniswirkung in § 6.1 (z. B. „332/332 CSV byte-gleich", „90 Dateien SHA256-gleich"); für die **offenen** Etappen nur bei V‑E („A/B-Nachweis") und § 2.11.6 („die Mappe muss vor und nach der Stufe dieselben Werte zeigen") | KK Z. 2217, 2222, 684, 758 |
| **Rechenwirkung** | *liefert* — die durchgängigste Stärke des Papiers; jede Etappentabelle hat die Spalte | KK Z. 2209, 2407, 678 |
| **Schemaschrittnummer** | *liefert, aber falsch* — drei überholte Nennungen (02/b‑1, b‑2, b‑11) und der gegenstandslose Nachzieh-Auftrag „auf 63" (§ 2.11) | KK Z. 3, 1851, 2182 |
| **Ressourcenschlüssel** | *liefert teilweise* — 37 Schlüssel, aber ungleich verteilt: § 2.2/§ 2.13 nennen fast keine, die Tafel steht im **Mockup**, nicht im Konzept | `Mockups/Dialog_Formel_Zahlenprobe.html` Ressourcentafeln |
| **Testklassen** | *liefert teilweise* — 11 Namen, alle für **gelaufene** Etappen (`ErloesrubrikTests`, `VergleichssichtTests`, `ReferenzprojektTests`, …); für offene Etappen kein einziger Vorschlag | KK, gemessen |
| **Wiki-Seite** | **fehlt** — zwei Nennungen im ganzen Papier, beide zu § 2.16. Die Regel aus `CLAUDE.md` Z. 251 ff. verlangt für jede sichtbare Änderung eine Wiki-Pflege; das Konzept nennt sie je Etappe nicht | KK Z. 1345–1346 |
| **Reihenfolge** | *liefert, aber überholt* — § 7 beschreibt die Reihenfolge von Anfang September (§ 2.9) | KK Z. 2405–2417 |
| **Größe/Aufwand** | **fehlt** — keine einzige Aufwandsangabe je Etappe; nur das Szenarienkonzept hat eine Spalte „Aufwand" (§ 7.3) | Szen Z. 267 |

## 3.2 Je Etappe

| Etappe (heute offen) | Abnahme | Rechenwirkung | Schemaschritt | Ressourcen | Tests | Wiki | Reihenfolge | Größe | Gesamt |
|---|---|---|---|---|---|---|---|---|---|
| **§ 6.3 Nr. 9a/9d** (Steuerzeilen trennen, Nullzeilen-Grund) | fehlt | fehlt | — | fehlt | fehlt | fehlt | fehlt | fehlt | **fehlt** |
| **§ 6.3 Nr. 9c/9g** (KWKG-Pauschale Jahr 0) | fehlt | teilweise | — | Mockup U17 | fehlt | fehlt | fehlt | fehlt | **liefert teilweise** |
| **§ 6.3 Nr. 9i** (Erlösrubrik nach Komponente, U6/Q15) | fehlt | liefert | — | Mockup | fehlt | fehlt | teilweise | fehlt | **liefert teilweise** |
| **§ 6.3 Nr. 9j** (Verlauf, drei Szenarien) | fehlt | liefert | — | teilweise | fehlt | fehlt | teilweise | teilweise (§ 2.13) | **liefert teilweise** |
| **§ 6.3 Nr. 9h / U39** (Nutzungsdauer-Rest) | fehlt | liefert | fehlt | fehlt | fehlt | fehlt | fehlt | fehlt | **fehlt** |
| **§ 6.3 Nr. 10–19** (fachlich/technisch) | fehlt | teilweise | — | fehlt | fehlt | fehlt | fehlt | fehlt | **fehlt** |
| **§ 6.3 Nr. 20–24** (Nachweis/Betrieb) | teilweise | — | — | — | fehlt | fehlt | fehlt | fehlt | **liefert teilweise** |
| **V‑A** (Ausweis, Deklaration, Steigung) | fehlt | liefert („keine") | — | fehlt | fehlt | fehlt | liefert | fehlt | **liefert teilweise** |
| **V‑C** (ValERI-Ansicht) | fehlt | liefert | — | fehlt | fehlt | fehlt | liefert | fehlt | **liefert teilweise** |
| **V‑D** (XLSX-Formelbericht) | **liefert** (§ 2.11.6 Stufenplan mit Gegenprobe, Grenze gemessen) | liefert | — | fehlt | fehlt | fehlt | liefert | teilweise | **liefert** |
| **V‑E** (Szenarioabdeckung) | liefert (A/B, NULL = wie Erwartet) | liefert | fehlt | fehlt | fehlt | fehlt | liefert | fehlt | **liefert teilweise** |
| **ND‑S3** (Instandsetzung/Wartung) | liefert (Referenzlauf mit neuer Basis) | liefert | teilweise | fehlt | teilweise | **liefert** (ND § 3 nennt die Wiki-Seite) | liefert | fehlt | **liefert teilweise** |
| **Ergebnisansicht § 2.13** | fehlt | teilweise | — | teilweise | fehlt | fehlt | fehlt | teilweise | **liefert teilweise** |
| **Hausstil § 2.7** | fehlt | liefert („keine") | — | **fehlt** (Farbwerte als Literale, keine Token-Namen) | fehlt | fehlt | fehlt | fehlt | **fehlt** |

**Muster:** Das Papier ist stark bei *Rechenwirkung* und *Reihenfolge*, schwach bis blind bei *Tests*, *Wiki* und *Größe* — genau den drei Angaben, die ein Umsetzer für die Planung einer Welle braucht. Einzige durchgehend brauchbare Etappenbeschreibung ist **§ 2.11.6** (V‑D): gemessene Grenze, Stufenplan, Gegenprobe je Stufe, Liste dessen, was dauerhaft Wert bleibt. Das ist das Muster, an dem sich die übrigen Etappen ausrichten sollten.

## 3.3 Struktur — gültiger Stand gegen Geschichte

`CLAUDE.md` Z. 248–250 formuliert die Regel für sich selbst: „**Diese Datei beschreibt nur den gültigen Stand.** Keine Datums-, Entscheid- oder Protokollvermerke ('seit …', 'vorher …', Auftrags- und Wellenkürzel); was sich geändert hat, steht in der Statusdatei, im Protokoll und in `ueberholt/`." Sinngemäß gilt das für jedes Papier in `aktuell/` (Z. 241: „`aktuell/` ist die Arbeitsgrundlage").

Gemessen am KK:

| Geschichtsmerkmal | Fundstellen (Auswahl) | Gehört nach |
|---|---|---|
| Entscheiddatum in der **Überschrift** | § 2.5 Z. 300 „(Auftrag 30.08.2026) — umgesetzt", § 2.6 Z. 349, § 2.8 Z. 474 „(übernommen 31.08.2026)", § 2.9 Z. 534, § 2.10 Z. 588, § 2.11 Z. 609, § 2.11.5 Z. 693, § 2.11.6 Z. 735, § 2.11.7 Z. 796, § 2.12 Z. 823, § 2.13 Z. 891, § 2.14 Z. 1018, § 2.15 Z. 1044, § 2.16 Z. 1197 — **14 von 16 Unterabschnitten des § 2** | Protokoll |
| „— umgesetzt" in der Überschrift | § 2.5, § 2.6, § 2.9, § 2.15, § 2.16 | § 6.1 bzw. ganz weg |
| Durchgestrichene Punkte | § 6.3 Z. 2252–2257, 2264, 2288, 2294, 2302, 2326, 2339 (7 Blöcke, 20 Zeilen) | Protokoll |
| Auftrags-/Wellenkürzel im Fließtext | BW9, BK‑E‑1, B7‑E‑1, BK1‑Q1/Q2, K‑WZ‑1, VG‑Q1…Q7, VV‑Q1…Q7, ET‑D, FK10, FX5‑a, L4 | Protokoll bzw. Register |
| „Anwenderentscheid vom …", „nach Empfehlung" | durchgehend in § 5, § 6.3, § 2.11.4 | Register (Spalte „Stand") |
| Arbeitsregel vom 30.08.2026 | Z. 25–29 | Protokoll |
| Artifact-Links | Z. 40–52 | Protokoll (bis auf einen) |
| Codestand | Z. 3 | streichen (Q25) |

**Bewertung:** Das KK verletzt die Regel in ihrer Substanz — es ist zu etwa gleichen Teilen Stand und Geschichte. Das ist kein Formalfehler: Die Prüfung heute hat **18 Stellen** gefunden, an denen die Vermischung zu einer falschen Aussage geführt hat (02/b 6 · 02/c 6 · 02/d 22 · 02/f 4 · dieses Papier § 2: 10 weitere) — jedes Mal, weil eine Geschichtszeile wie eine Regelzeile gelesen wird.

## 3.4 Gliederungsvorschlag (drei Papiere statt einem)

**A — `aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan.md`** (gültiger Stand, ohne Geschichte; geschätzt 900–1 100 Zeilen)

| § | Inhalt | woher |
|---|---|---|
| Kopf | Stand (Datum) · Zielversion · nächster freier Schemaschritt · „führende Fassung, Nebenkonzepte: …". **Kein Codestand** | Q25, 02/b‑1, 02/f‑1 |
| 1 | Landkarte: was mit Kapitalwert wirkt, die vier Datenwelten | heutige § 1 unverändert |
| 2 | Dialograum: je Dialog **ein** Abschnitt ohne Datum, mit gebautem Razor-Namen und Altnamen als Paar | heutige § 2.1–2.8, 2.14; 02/d‑22 |
| 3 | Rechenwege | heutige § 3 unverändert (die stärkste Passage) |
| 4 | ValERI und Szenarien: Kernaussage, Gap-Tafel, Blöcke, Formelbericht-Stufenplan, Hinweistext | heutige § 2.9–2.11 |
| 5 | Ergebnisansicht und Vergleichssicht | heutige § 2.13, 2.15, 2.16 |
| 6 | Hausstil (nach Q8/Q9 mit Token-Namen statt Farbliteralen) | heutige § 2.7 |
| 7 | Umsetzungsstand: **eine** Tafel „Etappe · Gegenstand · Schemaschritt · Statuszeile" und **eine** Tafel „offen, in dieser Reihenfolge" | heutige § 6.1 + § 7, entdoppelt |
| 8 | Bekannte Grenzen und Vereinfachungen (was bewusst nicht gebaut wird) | heutige § 6.3 Nr. 13, § 6.5, Szen § 7.3 G1/G3/G5 |
| Anhang | Übersetzungstafel der Kürzel (§ 2.12 dieses Berichts) | neu |

**B — `aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit.md`** (neu; Muster: das vorhandene `aktuell/Entscheidungsregister_iOS_EPOS-Plan.md`)
Eine Tabelle, Spalten *Kennung · Frage · Stand · Quelle · blockiert · Empfehlung*, geführt über alle fünf Kennungsräume, mit einem **Kennungsvergabeblock** oben („Befunde mit Bindestrich, Entscheidungen ohne; `V‑G` nur für die Gap-Reihe; Mockup-Punkte immer `Un`"). Erledigtes wandert ins Protokoll statt durchgestrichen zu werden. Damit entfallen aus A: heutige § 5, § 6.3, § 2.11.4-Entscheidtabelle.

**C — `ueberholt/Protokolle/Reporting/Wirtschaftlichkeit_Entscheidwege_2026-08_bis_09.md`** (neu)
Die Geschichte: Konsolidierung vom 02.09.2026 samt Quelltabelle und Artifact-Links, Arbeitsregel vom 30.08.2026, alle Entscheiddaten und Auftragskürzel, die 20 durchgestrichenen Punkte, die abgelösten Fallstricke (§ 6.4), die aufgelösten doppelten Wahrheiten (§ 6.5 Kopfblock).

Der Umzug ist **keine Umsetzung**, sondern Papierpflege — nach `CLAUDE.md` Z. 243–244 mit `git mv` und Indexzeile im selben Schritt.

---

# 4 Die drei Quellen des Geltungsblocks

| Quelle | Ablage heute | Indexzeile | Widerspruch zum KK |
|---|---|---|---|
| `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` | **`ueberholt/`** | **ja** — `Dokumentation/LIESMICH.md:173`, Datum 2026‑09‑07, Vermerk „eine der drei Quellen der…" | keiner gefunden (Etappenkonzept, Stand B4) — **aber** das KK stützt § 2.2 auf dessen Leitentscheidung `BW9`, die damit nur in `ueberholt/` nachlesbar ist (§ 2.5) |
| `KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` | **`ueberholt/`** | **ja** — `LIESMICH.md:168`, 2026‑08‑29, „Quelldokument; be…" | keiner gefunden; das KK zitiert daraus § 9 (doppelte Wahrheiten, Z. 2390) und die Festlegungen L/KL/E/FK |
| `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` | **`aktuell/`** | **ja** — `LIESMICH.md:50`, Datum **2026‑08‑30** (die Datei trägt Rechtsstand 18./19.08.2026) | **einer, ungelöst** — s. unten |

**Ablageproblem:** Die drei Quellen liegen in zwei verschiedenen Rubriken. Das Grundlagenpapier ist zu Recht in `aktuell/` (es ist Faktenbasis, kein umgesetztes Konzept), aber sein Kopf Z. 6–8 verweist auf `Konzept_BHKW_Kosten_Erloese.md` unter `ueberholt/Protokolle/Reporting/` — ein Papier aus der Geschichte als benannter Hauptverwender. Dazu die zwei toten Verweise aus `Wirtschaftlichkeit_Kosten/LIESMICH.md` (02/g‑1).

**Stichprobe Grundlagen § 5 (Werte der Altanwendung, Z. 397–415):** Die Tafel ist **die Prüfliste für B9/A8** und wird vom KK **nirgends genannt**. Sie führt neun Abweichungen der Altanwendung, darunter drei, die eine Zahlenprobe sofort auseinanderlaufen lassen: Öl 61,35 € je 1 000 l statt je MWh (**Faktor ≈ 10**), Flüssiggas „nicht zuordenbar", Stromsteuer −0,50 €/MWh als Restbelastung statt 20,00 €/MWh Erstattung. Dazu drei Lücken der Altanwendung (Jahresdeckel § 8 Abs. 4, Stromsteuerbefreiung § 9 Abs. 1 Nr. 3, Wegfall des Kumulierungsverbots). **Empfehlung:** Bevor B9 beginnt, wird diese Tafel zur Abnahmeliste von A8 erklärt und im KK § 7 verlinkt — sonst wird die Zahlenprobe als „Fehler in EPOS-Plan" gelesen, wo sie Fehler der Altanwendung findet.

**Stichprobe Grundlagen § 6 gegen KK § 5 R‑U:** **Wortgleich, keine Abweichung** — beide führen dieselben fünf Punkte in derselben Reihenfolge mit denselben Bewertungen (Grdl Z. 421–436 ↔ KK Z. 2197–2201). Der Unterschied ist nur die Spalte „Stand im Konzept" im KK. **Das ist eine gepflegte Doppelung, kein Widerspruch** — aber eine Novelle muss beide Orte treffen, genau wie die in § 6.5 benannten Doppelungen. **Ungelöst** ist dagegen: **Grundlagen § 10 Nr. 1–4** (45‑€‑Mechanismus ETS 2, § 10 Abs. 3 BEHG, Projektionsbericht 2026, Enddatum der Versteigerungsphase) stehen im KK **an keiner Stelle**, obwohl § 3.11 den CO₂-Preispfad auf genau diesen Größen aufbaut. Das ist die einzige inhaltliche Lücke zwischen Quelle und konsolidiertem Papier, die ich gefunden habe.

---

# 5 Statuszeilen-Abgleich #300–#369

**56 der 66 Zeilen** im Bereich #300–#369 betreffen Wirtschaftlichkeit oder Kosten. Nicht betroffen: #300, #303, #305, #306, #307, #309, #320, #323, #324, #327, #367, #368 (Assistent, Senken, Speicherflotte, Klimadaten).

Geführt im KK sind davon **acht** (§ 6.1: BK1 #330, BK1a #335, BK1b #336, VG #358, B7P #331; § 5/§ 7: B6 #328, B7 #329; § 4: B‑1 #331). **Alle übrigen fehlen.** 02/c‑13 hat zwei genannt (VV, Hilfsstrom); hier die vollständige Liste der Lücken:

| # | Kürzel | Halbsatz | in § 6.1/§ 6.3 geführt? |
|---|---|---|---|
| #301 | — | Kosten-Seite nach Variantenwechsel, fünf Nebenbefunde | nein |
| #302 (Kosten) | — | aufruferlose Betriebskosten-Methoden entfernt | nein |
| #304 (Energieträger) | — | Zuordnung je Anlage zum Energieträger | nein |
| #310 | — | entfernter Energieträger bleibt in der Liste | nein |
| #311 | ET‑E‑3 | Auswahlliste auf die möglichen Träger verengt | nein |
| #312 | — | **Energiekosten unvollständig — nur Arbeitspreis** | nein — **Rechenwirkung** |
| #313 | — | Stromträger-Bereich einklappbar, ermittelter Wert | nein |
| #314 | — | Einspeisevergütung v_pv/v_bhkw auf der Trägerseite | nein — berührt § 6.5 „vier Orte" |
| #315 / #317 | — | Kostenleiste an Kessel-, BHKW- und WP-Dialog | nein |
| #316 | — | Aufräumen nach der Strompreis-Welle | nein |
| #318 | — | `Anteil_Modus` (Brennstoff) abgeschafft | nein |
| #319 / #322 | US‑E‑1 | UMLAGEN-Katalogzeilen, Saatdubletten | nein — berührt § 6.3 Nr. 14 |
| #321 | LS‑E‑… | Leistungspreis Stamm ↔ Variante, Lastspitze | nein — **Rechenwirkung** |
| #325 / #328 (Seite) | — | Parameter-Knopf, nicht monetärer Text doppelt | nein |
| **#328** | **B6** | § 9 Nr. 3 als Ausweis, Schemaschritt 88 | **fehlt in § 6.1** (nur § 7 und § 5 K3) |
| #332 | — | Anwenderdurchsicht, sieben Entscheide (K‑1, K‑3, B‑1, N‑3, V‑4, K‑8, V‑G10) | nein — nur einzelne Entscheide im § 5 |
| #333 | — | neue Referenzbasis nach B‑1 | teilweise (§ 6.2 Z. 2238 nennt die Basis, nicht die Zeile) |
| #334 | — | zwei Rechner parallel am Konzept | nein |
| #337–#340 | — | konsolidiertes Mockup, Jahresdeckel, Energiesteuergutschrift, Vergütungsdialoge | nein |
| #341 | ET‑D | Energieträgerdialog nach Mockup | nein — obwohl § 5 ET‑D‑1…3 die Entscheide führt |
| #342, #345, #347, #348, #350, #351, #352, #356, #362 | U23/U26/U28–U31/U33/U36/U38 | Mockup- und Dialogpflege | nein |
| #343 | U24 | Hilfsstrom-Anteil vom Endenergiebedarf der Komponente | nein |
| #344 | — | Mockup-Ordner zusammengelegt | nein |
| #346 | U17/U18 | **KWKG-Pauschale als Zeile im Jahr 0** | nein — obwohl § 6.3 9c/9g genau das fragt |
| #349 / #353 | — | **Elektrokessel: Strom als Energieträger, Rangfolge** | nein — obwohl § 5 E1 den Entscheid führt |
| #354 | — | Vergleichssicht (Mockup) | teilweise (§ 2.15) |
| #355 | — | Vergütung je Variante (Mockup) | teilweise (§ 2.16) |
| **#357** | **ND‑S2** | Nutzungsdauern Stufe S2, U8, U30 | **nein** — und § 2.13/§ 6.3 9h behaupten weiter das Gegenteil (02/c‑3, c‑4, c‑5) |
| **#359** | **VV** | Vergütung je Variante, **Schemaschritt 93** | **nein** (02/c‑13) |
| #360 | — | Projekttransfer einer Variante mit „übernehmen" | nein |
| **#361** | — | Windows-CI rot seit #255, Fußzeilen-Test der **Rasterkarte** | **nein** — Kostenraster-Bezug |
| **#363** | — | **Übernahme aus der Kostenverwaltung (Katalogblock)** | **nein** — und das Mockup beschreibt noch die alte Überlagerung (02/d‑16) |
| **#364** | — | **Hilfsenergiekosten werden nicht berechnet; Bezugsgrößen** | **nein** |
| **#365/#366** | — | **Hilfsenergie am Endenergiebedarf, Schemaschritt 94; Preis des eigenen Stromträgers je Anlage** | **nein** (02/c‑13) — und § 5 K2 sagt weiter „% des Bedarfs" |
| #369 | — | Prüfung der Mockups | nein (das Befundpapier trägt sich selbst) |

**KL-Klimadaten (#367, #368)** betreffen die Wirtschaftlichkeit **nicht** und gehören zu Recht nicht in § 6.1 — die Anfrage bestätigt sich.

**Summe:** § 6.1 führt 19 Etappenzeilen; es fehlen mindestens **acht** Zeilen mit Schema- oder Rechenwirkung: **B5, B6, VV (#359), Hilfsstrom (#365/#366), Nutzungsdauer S2 (#357), B‑1/#331 mit neuer Basis #333, Übernahme #363, Bezugsgrößen #364**.

---

# 6 Empfehlung in zehn Sätzen

1. **Umsetzungsreif ist das Konzept nicht** — fachlich trägt es, aber als Vorlage ist es unbrauchbar, solange der Geltungsblock sagt „nichts ist implementiert", § 6.1 sechzehn gelaufene Etappen führt und § 7 eine gebaute Etappe (B5) als nächsten Schritt vorschlägt.
2. **Vor der ersten Codeetappe** sind vier Dinge am Papier zu tun, und nur diese vier: Geltungsblock und Kopfzeile berichtigen (§ 2.1, § 2.2, Q25), § 6.1 um die acht fehlenden Etappenzeilen ergänzen (§ 5), § 6.3 von den neun falsch als offen geführten Punkten befreien (§ 1.5) und die Quelltabelle von den zwei nicht existierenden Karten trennen (§ 2.3).
3. **Kritisch, weil sie mehrere Etappen zugleich blockieren, sind fünf Entscheide:** Q1 (Ablösung des zweiten Mockups — hängt an jeder Etappe, die sich auf ein Bild beruft), Q8/Q9 (Fußleisten- und Kopfbandregel — hängt an jedem neuen Dialog), 9c ≡ 9g ≡ U17 (KWKG-Pauschale im Jahr 0 — hängt an Erlösrubrik und Ergebnisansicht), Q15 ≡ 9i (Erlösrubrik nach Komponente innen — ohne sie ist die Zahlenprobe im Programm nicht nachvollziehbar) und ND‑S3 (ohne ihn bleiben die Betriebskostensätze Konstanten und der Nutzungsdauer-Strang halb fertig).
4. **Der teuerste Einzelentscheid ist V‑G10** (ganzer Bericht formelbasiert): Er kippt V‑2, vergrößert V‑D erheblich und ist der einzige, für den das Papier bereits eine belastbare Etappenbeschreibung hat (§ 2.11.6) — er sollte deshalb als Muster für alle übrigen Etappenbeschreibungen dienen.
5. **Ein fachlicher Widerspruch ist ungelöst** und gehört vor jede ValERI-Etappe geklärt: KK § 2.11.4 rechnet Degradation in V‑E ein, das Szenarienkonzept hat sie am 09.09.2026 als G3 ausdrücklich abgelehnt (§ 2.11).
6. **Eine Lücke zwischen Quelle und konsolidiertem Papier** ist zu schließen: Grundlagen § 10 Nr. 1–4 (ETS‑2‑Mechanismus, § 10 Abs. 3 BEHG, Projektionsbericht, Enddatum Versteigerung) tragen den CO₂-Preispfad des § 3.11 und stehen im KK nirgends.
7. **B9/A8 ist nicht mehr blockiert** — die Excel der Altanwendung liegt vor; vor dem Start wird die Tafel Grundlagen § 5 zur Abnahmeliste erklärt, sonst liest man Fehler der Altanwendung als Fehler von EPOS-Plan.
8. **Die Reihenfolge der Papierpflege** ist: (P1) Kopf, Geltungsblock, Quelltabelle, Artifacts — eine Stunde, kein Entscheid nötig; (P2) § 6.1 und § 6.3 gegen Statusdatei und Code abgleichen; (P3) Kennungsraum bereinigen und die Übersetzungstafel anlegen (§ 1.10, § 2.12); (P4) Entscheidungsregister als eigenes Papier herauslösen; (P5) Geschichte nach `ueberholt/Protokolle/` auslagern; (P6) erst dann Q1–Q25 einholen, weil ein Viertel der Fragen sich durch P1–P5 erledigt oder anders stellt.
9. **Der Schnitt in drei Papiere** (§ 3.4) ist die eigentliche Arbeit: Ein Papier, das zu gleichen Teilen Stand und Geschichte trägt, produziert die Art Fehler, die heute achtzehnmal gefunden wurde — jedes Mal, weil eine Geschichtszeile wie eine Regelzeile gelesen wurde.
10. **Vor der Umsetzung fehlen dem Papier durchgängig drei Angaben** — Testklasse, Wiki-Seite und Größenordnung je Etappe; sie sind billig nachzutragen, und ohne sie kann keine Welle geplant werden.

---

## Nicht geprüft

- **§ 1 bis § 3 des Konzepts inhaltlich** (Landkarte, Dialograum im Detail, Rechenwege, 1 400 Zeilen) — nur auf Struktur, Kennungen und Kopfangaben gelesen; die Formeln selbst hat heute `01_Nachrechnung.md` geprüft, nicht dieser Durchgang.
- **Die sechs Mockups** — Gegenstand von `00_Sichtpruefung.md`, `03_Mockup_Code.md`, `04_Einheitlichkeit.md`, `06_Struktur_Darstellung.md`; hier nur punktuell als Gegenbeleg gelesen (U1, U40, Ressourcentafeln).
- **Die acht Rechenweg-Papiere** `Rechenweg/01…08` und `Beispielprojekt.md` — nur über die Struktur-Liste von `Wirtschaftlichkeit_Kosten/LIESMICH.md` erfasst.
- **Wiki-Seiten** (`wiki.epos-plan.de`) — Gegenstand von `05_Wiki.md`; hier nur die Nennungen *im Konzept* gezählt.
- **Die Excel der Altanwendung** `BHKW-WP-PLAN.XLSM` auf dem Netzlaufwerk — ausdrücklich einem anderen Agenten zugewiesen; `Analyse_Altanwendung_BHKW-Plan.md` wurde nur auf Existenz geprüft, nicht gelesen.
- **Die drei Quellpapiere vollständig** — nur die angefragten Stichproben (Grundlagen § 5, § 6, § 10, Kopf; die beiden `ueberholt/`-Papiere nur auf Ablage und Indexzeile).
- **§ 6.3 Nr. 10, 11, 13, 15, 16, 19, 22, 23, 24** — als Stand aus dem Papier übernommen, nicht am Code gegengemessen.
- **§ 6.3 Nr. 18 (HB1‑O1)** — `ORDER BY Prioritaet, ID` ist in `SimulationControl.cs` an drei Stellen vorhanden, aber ich habe nicht geprüft, ob das die im Befund gemeinte Engine-Stelle ist; als „vermutlich überholt" geführt.
- **Kein Build, kein Test, kein Referenzlauf** — reine Lesearbeit; alle Codeaussagen stammen aus `grep`/`find`, nicht aus einem Lauf.
- **Die Statuszeilen #1–#299** — außerhalb des Auftragsbereichs; W5‑B‑9…12 und die Etappen vor B5 sind deshalb nur aus den Papieren belegt, nicht aus Statuszeilen.
