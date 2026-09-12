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
| Hilfsenergieanteil BHKW | 2,0 % des Brennstoffs | A | `Hilfsenergie_Anteil` (neu B5) |
| Energiesteuerwahl · Aufteilung | § 53a Abs. 5 · voller Brennstoff | A | `Energiesteuer_Wahl` · `Aufteilung_Methode` |
| Photovoltaik | 300 kWp = 750 Module × 400 Wp | A | `Tab_Energieanlagen.PV_Leistung` ⚠ I-1 |
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
| Hilfsstrom BHKW | 2,0 % × 4.342,1 | 86,8 MWh/a |
| Nettostromerzeugung | 1.650,0 − 86,8 | **1.563,2 MWh/a** |
| PV-Ertrag Jahr 1 | 300 kWp × 950 kWh/kWp | **285,0 MWh/a** |
| PV-Degradation | 0,5 %/a → Jahr 20: 285,0 × 0,995^19 | 259,1 MWh |

## 3 Strombilanz

Die Aufteilung Eigenverbrauch / Einspeisung liefert im echten Lauf die Stundensimulation; hier ist
sie als Annahme gesetzt (BHKW 70 / 30 %, PV 30 / 70 %), **auf brutto und netto getrennt angewandt**,
weil verschiedene Vorschriften verschiedene Mengen verlangen (siehe `Rechenweg/05`, Mengentafel).

| Menge | MWh/a | verwendet von |
|---|---|---|
| BHKW Eigenverbrauch **brutto** (70 % × 1.650,0) | 1.155,0 | § 9 Abs. 1 Nr. 3 StromStG, CO₂-Grenzwert |
| BHKW Einspeisung brutto (30 %) | 495,0 | — |
| BHKW Eigenverbrauch **netto** (70 % × 1.563,2) | 1.094,2 | KWKG § 7 Abs. 2, Differenzmethode |
| BHKW Einspeisung netto (30 %) | 469,0 | KWKG § 7 Abs. 1, Einspeiseerlös |
| PV Eigenverbrauch (30 % × 285,0) | 85,5 | vermiedener Bezug (Ausweis) |
| PV Einspeisung (70 %) | 199,5 | EEG-Vergütung |
| Reststrombezug Netz | 250,0 | Energiekosten, § 9b |
| Strombedarf ohne Anlagen (250,0 + 1.094,2 + 85,5) | 1.429,7 | Differenzmethode „Bezug ohne Anlage" |
| physisch vermiedener Bezug (1.094,2 + 85,5) | 1.179,7 | vermiedene Stromkosten |

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
| Kaskadenprobe Projekt 1042 | A 16.993,60 · B 849,68 · C 3.084,33 → Delta +20.927,61 € | `Rechenweg/01` — die Investitionszahlen des BHKW sind diesem Beleg nachgebildet |
| Mischsatz 300 kW | 5,5667 ct/kWh | `Rechenweg/05` |
| AW 300 kWp | 6,04 ct/kWh; Degression 8,60 → 8,10 trifft 16/16 BNetzA-Werte | `Rechenweg/06` |
| Aufschlagsbefund N3, Projekt 1030 | +360.603 €/a (+32 %), Kapitalwert −29,8 % | `Rechenweg/04` |
| § 9 Nr. 3 Doppelzählung, Projekt 1024 | 1.510,84 €/a auf beiden Pfaden | `Rechenweg/05`, `07` |
| Höfingen `Tab_kurz_KWKG2020` | Mehrinvestition 55.745 · NPV 65.259 € · IZF 20,4 % · 4,33 a | `Rechenweg/08` |
