# Beispielprojekt „Musterprojekt Gewerbepark"

Die eine Zahlenquelle für alle Rechenwege in diesem Ordner. Jede Größe hier ist entweder eine
**Annahme** (A), eine **Ableitung** (→) aus anderen Größen dieser Datei oder ein **Beleg** (B) aus
dem Bestand von EPOS-Plan. Wer eine Zahl in einem Rechenweg nicht nachvollziehen kann, findet ihre
Wurzel hier.

## 1 Anlagen

| Größe | Wert | Art | Quelle im Datenmodell |
|---|---|---|---|
| BHKW elektrisch / thermisch | 300 / 355 kW | A | `Tab_BHKW.Pel` · `Ptherm` |
| Wirkungsgrade el / th / gesamt | 38 / 45 / 83 % | A | Katalog |
| Betriebsstunden | 5.500 h/a | A | Simulationslauf |
| Energieträger BHKW | Erdgas, H_i 10,5 · H_s 11,6 kWh/m³ | A | `energy_carrier` |
| KWKG-Anlagenart · Stichtag · Inbetriebnahme | neu (§ 8 Abs. 1) · 14.03.2026 · 01.10.2026 | A | `Tab_Energieanlagen.KWKG_*` |
| Eigenstrom-Tatbestand § 6 Abs. 3 | Nr. 2 Kundenanlage | A | `KWKG_Eigenstromfall` |
| Hilfsenergieanteil BHKW | 2,0 % des Brennstoffs | A | `Hilfsenergie_Anteil` |
| Vorrichtung zur Abwärmeabfuhr · Stromkennzahl σ | nicht gesetzt · 0,845 (= 300 / 355) | A · → | Anlagenangaben KWKG — im Beispiel greift § 2 Nr. 16 Fall 1 |
| Gas-Brennwertkessel (Stammprojekt und Spitzenlast) | Nutzungsgrad 95 % | A | `Tab_Heizkessel` |
| Energiesteuerwahl · Aufteilung | § 53a Abs. 5 · voller Brennstoff | A | `Energiesteuer_Wahl` · `Aufteilung_Methode` |
| Photovoltaik | 300 kWp = 750 Module × 400 Wp | A | `Tab_Energieanlagen.PV_Leistung` — die Spalte trägt andernorts die Modulanzahl, die Herleitung zeigt deshalb beide Größen |
| PV-Inbetriebnahme · Einspeiseart | 01.08.2026 · Überschusseinspeisung | A | PV-Dialog |
| PV-Vermarktung | Direktvermarktung mit Marktprämie, DV-Entgelt 0,40 ct/kWh | A | PV-Dialog |
| Unternehmensart | produzierendes Gewerbe | A | Projektangabe — wirkt auf § 54 und § 9b |

## 2 Abgeleitete Energiemengen

| Größe | Rechnung | Wert |
|---|---|---|
| Brennstoffeinsatz BHKW | 1.650,0 ÷ 0,38 | **4.342,1 MWh/a** (H_i) |
| Brennstoff brennwertbezogen | 4.342,1 × 11,6 / 10,5 = × 1,1048 | 4.797,2 MWh/a (H_s) |
| Gasmenge in Abrechnungseinheit | 4.342,1 × 1000 ÷ 10,5 | 413.533 m³/a |
| Stromerzeugung brutto | 300 kW × 5.500 h | **1.650,0 MWh/a** |
| Nutzwärme | 4.342,1 × 0,45 | 1.953,9 MWh/a |
| Brennstoff Kessel (Stammprojekt, dieselbe Nutzwärme) | 1.953,9 ÷ 0,95 | **2.056,7 MWh/a** (H_i) |
| Brennstoff Kessel brennwertbezogen | 2.056,7 × 1,1048 | 2.272,3 MWh/a (H_s) |
| Hilfsstrom BHKW | 2,0 % × 4.342,1 | 86,8 MWh/a |
| Nettostromerzeugung | 1.650,0 − 86,8 | **1.563,2 MWh/a** |
| PV-Ertrag Jahr 1 | 300 kWp × 950 kWh/kWp | **285,0 MWh/a** |
| PV-Degradation | 0,5 %/a → Jahr 20: 285,0 × 0,995^19 | 259,1 MWh |

## 3 Strombilanz

Die Aufteilung Eigenverbrauch / Einspeisung liefert im echten Lauf die Stundensimulation; hier ist
sie als Annahme gesetzt (BHKW 70 / 30 %, PV 30 / 70 %) und gilt für die **Bruttoerzeugung** — die
Strommatrix des Rechenkerns ist die Brutto-Welt (`StromMatrix.KwkEigenGesamtMWh`,
`KwkEinspeisungGesamtMWh`). Der Hilfsstrom mindert allein die KWKG-Zuschlagsmengen, und zwar
**zuerst den Eigenverbrauch**; die Einspeisung erst, wenn der Eigenverbrauch aufgezehrt ist
(`HilfsstromRechner.NettoSplit`: Hilfsstrom verlässt die Kundenanlage nie). Welche Vorschrift mit
welcher Menge rechnet, steht in `Rechenweg/05`, Mengentafel.

| Menge | MWh/a | verwendet von |
|---|---|---|
| BHKW Eigenverbrauch **brutto** (70 % × 1.650,0) | 1.155,0 | § 9 Abs. 1 Nr. 3 StromStG, CO₂-Grenzwert |
| BHKW Einspeisung brutto (30 %) | 495,0 | Einspeiseerlös (`KwkEinspeisungGesamtMWh`) |
| BHKW Eigenverbrauch **KWKG** (1.155,0 − 86,8 Hilfsstrom) | 1.068,2 | KWKG § 7 Abs. 2 |
| BHKW Einspeisung **KWKG** (495,0, der Hilfsstrom ist vom Eigenverbrauch gedeckt) | 495,0 | KWKG § 7 Abs. 1 |
| PV Eigenverbrauch (30 % × 285,0) | 85,5 | vermiedener Bezug (Ausweis) |
| PV Einspeisung (70 %) | 199,5 | EEG-Vergütung — davon 20 % abgeregelt (§ 51), vergütet 159,6 |
| Reststrombezug Netz (Lauf „Beide Anlagen") | 250,0 | Energiekosten, § 9b |
| Strombedarf ohne Anlagen (Bedarfsreihe der Strommatrix) | 1.429,7 | Differenzmethode „Bezug ohne Anlage" |
| physisch vermiedener Bezug (1.429,7 − 250,0) | 1.179,7 | vermiedene Stromkosten |

## 4 Preise und Sätze

| Größe | Wert | Art |
|---|---|---|
| Erdgas Arbeitspreis | 0,7560 €/m³ = 0,0720 €/kWh (H_i) | A |
| — davon Energiesteuer | 0,0638 €/m³ (5,50 €/MWh H_s × 11,6 kWh/m³) | → |
| — davon CO₂ (BEHG) | 0,1371 €/m³ (65 €/t × 200,9 g/kWh × 10,5 kWh/m³ = 2,109 kg/m³) | → |
| — davon Netz- und Messentgelt | 0,1180 €/m³ | A |
| — davon Beschaffung und Vertrieb | 0,4371 €/m³ | → Rest |
| Erdgas Grundpreis | 180 €/a | A |
| Strom Arbeitspreis (Bezug und Reststrom, gleicher Tarif) | 28,80 ct/kWh | A |
| Strom-Aufschläge (Schalter aus) | 6,440 + 2,946 + 2,050 + 0,110 + 0,200 = 11,746 ct/kWh | B |
| Einspeisevergütung KWK (`Einspeiseverguetung_KWK`) | 5,0 ct/kWh | A |
| Jahresmarktwert Solar | 4,50 ct/kWh | A |
| CO₂-Preis 2026 | 65 €/t | B (Grundlagen § 8.1) |
| Emissionsfaktor Erdgas (EBeV 2030, H_i) | 200,9 g CO₂/kWh | B |
| Umsatzsteuer (nur Bruttoanzeige) | 19 % | Katalog |
| Kalkulationszins · Betrachtungszeitraum | 3,0 % · 20 a | Vorgabe `Tab_ProjektWirtschaftlichkeit` |
| Preissteigerung Energie · Betrieb | 0,0 · 0,0 %/a | Vorgabe |
| Preissteigerung Investition p_I (Ersatz und Restwert) | **0,0 %/a** | Vorgabe — bei 0 bleiben Ersatz- und Restwertbeträge nicht indiziert |
| Jahr 1 | **Kalenderjahr der Inbetriebnahme**, ersatzweise des Förderbeginns — hier **2026** | Konvention; volle Rechenjahre, keine Teiljahre |

## 4a Investition, Betriebskosten, Ersatz und Restwert

Alle Beträge netto, Kalkulationszins 3,0 %, Betrachtungszeitraum 20 a, p_I = 0 %/a.

| Komponente / Position | Rechnung | Betrag | Nutzungsdauer |
|---|---|---|---|
| BHKW-Modul | 300,00 kW × 653,60 €/kW | 196.080,00 € | 15 a |
| Montage und Inbetriebnahme | 5 % der Erzeugerkosten (196.080,00) | 9.804,00 € | 15 a |
| Hydraulik und Einbindung | Betrag | 13.000,00 € | 20 a |
| Planung und Genehmigung | 10 % von 218.884,00 | 21.888,40 € | — |
| **Investition BHKW brutto** | | **240.772,40 €** | |
| Zuschuss | Betrag | − 6.000,00 € | |
| **I₀ BHKW** | | **234.772,40 €** | |
| PV-Module | 300,00 kWp × 320,00 €/kWp | 96.000,00 € | 25 a |
| Wechselrichter | 300,00 kWp × 80,00 €/kWp | 24.000,00 € | 12 a |
| Unterkonstruktion und Montage | 300,00 kWp × 150,00 €/kWp | 45.000,00 € | 25 a |
| Elektroinstallation, Netzanschluss | Betrag | 18.000,00 € | 25 a |
| Planung und Genehmigung | 5 % von 183.000,00 | 9.150,00 € | — |
| **I₀ Photovoltaik** | kein Zuschuss · 640,50 €/kWp | **192.150,00 €** | |

| Betriebskosten Jahr 1 | Zusammensetzung | Betrag |
|---|---|---|
| Blockheizkraftwerk | 46.200,00 Wartung + 3.611,59 Instandhaltung (1,50 % von 240.772,40) + 6.252,62 Hilfsenergie + 1.100,00 Versicherung | **57.164,21 €/a** |
| Photovoltaik | 3.600,00 + 960,75 + 480,38 + 600,00 + 90,00 | **5.731,13 €/a** |
| Gas-Brennwertkessel | Wartung als Jahresbetrag | **2.400,00 €/a** |

| Ersatz und Restwert | Rechnung | Wert |
|---|---|---|
| Ersatz BHKW-Modul und Montage, Jahr 15 | 205.884,00 € ÷ 1,03^15 | Barwert 132.149 € |
| Restwert BHKW im Jahr 20 | 205.884,00 × 10 / 15 = 137.256,00 ; ÷ 1,03^20 | Barwert 75.995 € |
| Ersatz Wechselrichter, Jahr 12 | 24.000,00 € ÷ 1,03^12 | Barwert 16.833 € |
| Restwert Photovoltaik im Jahr 20 | 19.200 + 8.000 + 9.000 + 3.600 = 39.800,00 ; ÷ 1,03^20 | Barwert 22.036 € |

Von neun Investitionspositionen tragen sieben eine Nutzungsdauer; die beiden Planungspositionen
tragen keine und laufen still bis zum Ende des Betrachtungszeitraums.

## 4b Energiekosten und Erlöse der vier Versionen

| Version | I₀ | Betriebskosten | Energiekosten | Erlöse (Block A) |
|---|---|---|---|---|
| Stammprojekt — Weiterbetrieb | — | 2.400 | 560.016 | 31.230 |
| Variante 1 — Blockheizkraftwerk | 234.772 | 59.564 | 409.435 | 84.436 |
| Variante 2 — Photovoltaik | 192.150 | 8.131 | 535.392 | 38.521 |
| Variante 3 — beide Anlagen | 426.922 | 65.295 | 384.811 | 91.727 |

Energiekosten: Brennstoff × Arbeitspreis + Grundpreis 180 €/a + Netzbezug × 28,80 ct/kWh.
Kesselseite des Stammprojekts: 2.056,7 MWh × 0,0720 €/kWh = 148.082 €/a; Energiesteuer-Entlastung
§ 54: 2.272,3 MWh (H_s) × 1,38 €/MWh − 250 € = 2.885,7 €/a.
PV-Vergütung Jahr 1: 9.001,44 € aus `PvErloesRechner` (Stufe 1, ohne Stundenreihe) — 159,6 von 199,5 MWh vergütet
(§ 51, 20 % abgeregelt), Spoterlös 7.182,00 + Marktprämie 2.457,84 − DV-Entgelt 638,40; § 51a im Jahr 20 1.095,52 €;
Reihe nominal 150.118 €, Barwert 113.800 € (`Rechenweg/06`).

## 4c Wirtschaftlichkeit über 20 Jahre

Kapitalwertrechnung nach `KapitalwertRechner` mit den Reihen aus 4a und 4b (Rechenweg/08): i = 3,0 %,
T = 20 a, p_E = p_B = p_I = 0, Referenz das Stammprojekt. Kapitalwertdifferenz = Nettobarwert der Version
− Nettobarwert des Stammprojekts; Annuität mit a(3 %, 20 a) = 0,06722; Amortisation ohne, interner
Zinsfuß mit Restwert.

| Kennzahl (Szenario Erwartet) | Stammprojekt | Variante 1 — BHKW | Variante 2 — PV | Variante 3 — beide |
|---|---|---|---|---|
| Nettobarwert absolut | −7.902.712 € | −6.242.507 € | −7.720.222 € | −6.060.017 € |
| Kapitalwertdifferenz zum Stamm | — | +1.660.205 € | +182.491 € | +1.842.695 € |
| Annuität der Differenz | — | 111.592 €/a | 12.266 €/a | 123.858 €/a |
| Dynamische Amortisation | — | 1,68 a | 8,64 a | 2,64 a |
| Interner Zinsfuß | — | 61,2 % | 11,5 % | 39,2 % |
| Wärmegestehungskosten | 27,19 ct/kWh | 21,47 ct/kWh | 26,56 ct/kWh | 20,85 ct/kWh |
| Erlöse, Barwert (nominal) | 464.618 (624.592) | 1.025.946 (1.339.380) | 552.977 (740.510) | 1.114.304 (1.455.297) |

Szenarien (Vorgaben `Tab_ProjektWirtschaftlichkeit`, nichts gepflegt): Ungünstig i 4,0 %, p_E/p_B/p_I
+1 %/a, Investition +10 %, Erträge −10 %, Nutzungsdauer −2 a · Günstig spiegelbildlich (i 2,0 %,
−1 %/a, −10 %, +10 %, +2 a).

| Kapitalwertdifferenz zum Stamm [€] | Ungünstig | Erwartet | Günstig |
|---|---|---|---|
| Variante 1 — BHKW | 1.506.740 | 1.660.205 | 1.811.714 |
| Variante 2 — PV | 129.296 | 182.491 | 236.921 |
| Variante 3 — beide | 1.636.035 | 1.842.695 | 2.048.635 |

## 5 Gesetzliche Sätze (Katalog `Tab_Gesetzesparameter`, Stand 2026)

| Vorschrift | Satz |
|---|---|
| KWKG § 7 Abs. 1 Einspeisung, Staffel | 8,00 / 6,00 / 5,00 / 4,40 / 3,40 ct/kWh (bis 50 / 100 / 250 kW / 2 MW / darüber) |
| KWKG § 7 Abs. 2 Eigenstrom, Nr. 2 Kundenanlage | 4,00 / 3,00 / 2,00 / 1,50 / 1,00 ct/kWh |
| KWKG § 8 Abs. 1 Kontingent neu | 30.000 Vbh |
| KWKG § 8 Abs. 4 Jahresdeckel | 2026: 3.300 · 2027: 3.100 · 2028: 2.900 · 2029: 2.700 · ab 2030: 2.500 h/a |
| EnergieStG § 2 Abs. 3 Erdgas Regelsatz | 5,50 €/MWh (H_s) |
| EnergieStG § 53a Abs. 5 Erdgas Teilsatz | 4,42 €/MWh · Nutzungsgradschwelle 70 % |
| EnergieStG § 54 Erdgas | 1,38 €/MWh, Sockel 250 €/a |
| StromStG Regelsatz | 20,50 €/MWh |
| StromStG § 9b Entlastung | 20,00 €/MWh, Sockel 250 €/a |
| StromStG § 9 Abs. 1 Nr. 3 Bedingungen | ≤ 2 MW · hocheffizient · 4,5 km · CO₂ < 270 g/kWh Energieertrag |
| EEG anzulegender Wert 300 kWp, Stichtag 08/2026 | 6,04 ct/kWh (B, marginal gemischt) |

## 6 Belegzahlen des Bestands (nicht Teil des Beispielprojekts)

| Beleg | Wert | Verwendung |
|---|---|---|
| Kaskadenprobe Projekt 1042 (Baugröße 26,00 kW) | A 16.993,60 · B 849,68 · C 3.084,33 → Delta +20.927,61 € | `Rechenweg/01` — belegt die Kaskadenformel; die **Sätze** des Beispiels sind ihr nachgebildet, die Menge ist 300 kW |
| Mischsatz 300 kW | 5,5667 ct/kWh | `Rechenweg/05` |
| AW 300 kWp | 6,04 ct/kWh (10 × 8,10 + 30 × 7,06 + 260 × 5,84) ÷ 300; Degression 8,60 → 8,10 trifft 16/16 BNetzA-Werte | `Rechenweg/06` |
| Aufschlagsmessung Projekt 1030 | +360.603 €/a (+32 %), Kapitalwert −29,8 % | `Rechenweg/04` |
| § 9 Nr. 3 Doppelzählung, Projekt 1024 | 1.510,84 €/a auf beiden Pfaden | `Rechenweg/05`, `07` |
| Mischsatz Eigenstrom 300 kW | 2,4167 ct/kWh (Tatbestand Nr. 2) | `Rechenweg/05` |
| Beispielprojekt B `Tab_kurz_KWKG2020` | Mehrinvestition 55.745 · NPV 65.259 € · IZF 20,4 % · 4,33 a | `Rechenweg/08` |
