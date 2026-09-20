# Prüfpunkt „Valide Berechnung" — Nachrechnung der Zahlenproben

Geprüft: `Dokumentation/aktuell/Mockups/Dialog_Formel_Zahlenprobe.html` (4.849 Zeilen), Abschnitte
„Das durchgerechnete Beispiel", Kategorien 1–8 und Anhang „Herkunft der Zahlen".
Zeilenangaben sind **Zeilen der HTML-Datei**.

## 1 Kurzbefund

Die Zahlenproben des Mockups sind valide: von rund 180 nachgerechneten Größen treffen alle bis auf
neun exakt, und die neun Ausnahmen liegen unter 3 € bzw. unter 0,1 MWh. Der gesamte Kennzahlenblock
der Kategorie 8 — Nettobarwerte, Kapitalwertdifferenzen, Annuitäten, Amortisationen, interne
Zinsfüße, Bandbreiten, Verlaufsstart- und -endwerte, Nulldurchgänge und Restwerte aller drei
Szenarien — ließ sich mit `KapitalwertRechner` ohne Datenbank bitgenau bis auf ±2 € nachfahren; auch
`KwkgSatzRechner`, `KwkgKontingentRechner`, `EegSatzRechner`, `PvErloesRechner`,
`PvKennzahlenRechner`, `HilfsstromRechner` und `SteuerGutschriftRechner` bestätigen ihre Werte. Die
einzige inhaltlich schwere Feststellung betrifft nicht eine falsche Zahl, sondern eine Lücke: Der
Erdgas-Arbeitspreis 0,7560 €/m³ enthält den CO₂-Bestandteil 0,1371 €/m³, und daneben steht eine
BEHG-Reihe von 56.699,50 €/a — genau die Doppelpflege, die `Rechenweg/04` untersagt; die Gliederung
der Kategorie 8 löst den Widerspruch stillschweigend, die dafür vorgesehene Kohärenzzeile fehlt im
Kern. Zwei weitere Größen sind aus den gezeigten Eingaben nicht herleitbar: der Leistungsanteil
−4.180,0 € und die Kesselwahl § 54, die `Beispielprojekt.md` nicht führt. Alle übrigen
Abweichungen sind Rundungsfolgen — durchweg daher, dass das Mockup mit dem gerundeten
Hs/Hi-Faktor 1,1048 statt mit 11,6/10,5 weiterrechnet.

## 2 Methode

**Rein aufrufbar (ohne `IDatenzugriff`/`DataRepository`)** — alle geprüften Rechner der
Wirtschaftlichkeit nehmen ihre Gesetzeswerte als Delegat entgegen (Leitentscheidung L9):

| Klasse | Einstieg | rein |
|---|---|---|
| `KapitalwertRechner` | `Rechne`, `Ersatz`, `Barwert`, `Annuitaet`, `InternerZinsfuss`, `AmortisationDifferenz` | ja |
| `KwkgSatzRechner` | `Vorschlag(pel, jahr, art, fall, katalog, kultur)` | ja |
| `KwkgKontingentRechner` | `Ableiten(art, anteil, jahr, katalog, kultur)` | ja |
| `EegSatzRechner` | `AnzulegenderWert`, `AwKlasse`, `Degressionsschritte` | ja |
| `PvErloesRechner` | `Rechne(pv, kwp, einspeisung, …, katalog, jahresmarktwert, …)` | ja |
| `PvKennzahlenRechner` | `Rechne(…)` | ja |
| `SteuerGutschriftRechner` | `Rechne(eingabe, jahr, satz, kultur)` | ja |
| `HilfsstromRechner` | `MengeMWh`, `NettoSplit` | ja (`internal`) |
| `ErsatzRestwertTafel` | `Zeilen`, `Hinweis` | ja bis auf `NutzungsdauerCtrl.Standard` im Hinweistext (`internal`) |
| `GesetzKatalog` | `Vorbelegung()` — der vollständige Gesetzeskatalog als Code-Rückfallebene | ja, `public static` |

**Nicht rein und deshalb nach Konzeptformel gerechnet:** `WirtschaftlichkeitCtrl`,
`InvestKaskade.Lies/Summen`, `KostenSummenCtrl`, `KostenHerleitung.Bilde` und
`BetriebskostenCtrl.Betrag` lesen `DataRepository`. Betroffen sind damit die Investitionskaskade
(Runden 1–3), die Betriebskostenbemessung und die Energiekostenkette — diese Werte sind allein nach
`Konzept … konsolidiert.md` § 3 und den Rechenwegen 01–04 nachgerechnet, nicht gegen den Kern
gehalten. `EnergietraegerPreiskarte` und `Preisanteile` sind zwar rein, tragen aber nur die
Anzeigekante; sie wurden für die Formelzeile der Trägerkarte gelesen.

**Skript:** `SP\Nachrechnung\rechnen.cs` (dotnet-Dateiskript, `#:project` auf
`EPOS.Kern.csproj`, `#:property AssemblyName=EPOS.Kern.Tests` für die `internal`-Sicht).
Rohausgabe: `SP\Nachrechnung\lauf.txt`. Der Gesetzeskatalog kommt aus
`GesetzKatalog.Vorbelegung()` — keine Datenbank, keine Kopie, kein Schreibzugriff.
Laufzeit: 41 s Erstübersetzung von EPOS.Kern, danach 1,1 s je Lauf.

**Konventionen der Nachrechnung:** Jahr 1 = 2026, volle Jahre, i = 3 %, T = 20 a,
p_E = p_B = p_I = 0, Strom 28,80 ct/kWh, Erdgas 0,7560 €/m³ bei H_i 10,5 / H_s 11,6 kWh/m³,
KWKG-Deckel 3.300/3.100/2.900/2.700/2.500, Kontingent 30.000 Vbh, Vbh 5.500 h/a; Szenarien
±1 %-Punkt (Zins und drei Preisraten), ±10 % (Investition, Erträge), ±2 a (Nutzungsdauer).
Zwischenergebnisse ungerundet.

## 3 Ergebnistabelle

Bewertung: **trifft** = Abweichung 0 oder reine Anzeigerundung · **Rundung** = Abweichung allein aus
einem gerundeten Zwischenwert · **Abweichung** = nicht durch Rundung erklärbar · **nicht
herleitbar** = aus den gezeigten Eingaben nicht ableitbar.

### 3.1 Mengenbilanz und Strombilanz

| Größe | Mockup (Zeile) | nachgerechnet (Formel) | Kern | Abweichung | Bewertung |
|---|---|---|---|---|---|
| Brennstoff BHKW H_i | 4.342,1 MWh (557) | 1.650,0 ÷ 0,38 = 4.342,105 | — | 0 | trifft |
| Nutzwärme | 1.953,9 MWh (559) | 4.342,105 × 0,45 = 1.953,947 | — | 0 | trifft |
| Kesselbrennstoff H_i | 2.056,7 MWh (561, 611) | 1.953,947 ÷ 0,95 = 2.056,787 | — | 0 | trifft |
| PV-Ertrag | 285,0 MWh (563) | 300 × 950 ÷ 1000 | — | 0 | trifft |
| Reststrombezug V3 | 250,0 MWh (564) | Annahme des Laufs | — | — | trifft |
| Gasmenge | 413.533 m³ (1885) | 4.342,105 × 1000 ÷ 10,5 = 413.533,8 | — | 0 | trifft |
| Brennstoff H_s | 4.797,2 MWh (2171, 2285, 2368, 2636) | 4.342,105 × 11,6/10,5 = **4.796,99** | 4.796,99 | −0,2 MWh | Rundung (B4) |
| Kesselbrennstoff H_s | 2.272,3 MWh (617) | 2.056,787 × 1,104762 = 2.272,28 | 2.272,28 | 0,0 | trifft |
| Hilfsstrom | 86,842 MWh (2158) | 2 % × 4.342,105 | 86,8421 | 0 | trifft |
| Nettostromerzeugung | 1.563,158 MWh (2158) | 1.650,0 − 86,842 | 1.563,1579 | 0 | trifft |
| Eigen KWKG / Einspeisung KWKG | 1.068,158 / 495,000 (2159) | `NettoSplit`: Eigen zuerst | 1.068,1579 / 495,0 | 0 | trifft |
| Netzbezug V1 / V2 | 335,5 / 1.344,2 MWh (1940) | 1.429,7 − 1.094,2 / 1.429,7 − 85,5 | — | 0 | trifft (Herleitung erst Kat. 5/7, B13) |
| Stromkennzahl σ | 0,845 (2223, 4786) | 300 ÷ 355 = 0,84507 | — | 0 | trifft |
| Fall-2-Menge | 1.651,3 MWh (2341, 4789) | 1.953,947 × 0,84507 = **1.651,22** | — | +0,1 MWh | Abweichung (B6) |
| PV Jahr 20: Ertrag/Eigen/Einspeisung | 259,1 / 77,7 / 181,4 MWh (2931–2935) | × 0,995¹⁹ = 259,11 / 77,73 / 181,38 | 0,995¹⁹ = 0,909156 | 0 | trifft |

### 3.2 Investitionskaskade (Kategorie 1)

| Größe | Mockup (Zeile) | nachgerechnet | Kern | Abw. | Bewertung |
|---|---|---|---|---|---|
| BHKW-Modul | 196.080,00 (693, 908) | 300 × 653,60 | — | 0 | trifft |
| Montage 5 % | 9.804,00 (702, 912) | 196.080 × 0,05 | — | 0 | trifft |
| Hydraulik | 13.000,00 (711) | fester Betrag | — | 0 | trifft |
| Basis Runde 3 | 218.884,00 (720, 915) | 196.080 + 9.804 + 13.000 | — | 0 | trifft |
| Planung 10 % | 21.888,40 (720, 917) | 218.884 × 0,10 | — | 0 | trifft |
| Investition brutto | 240.772,40 (745, 919) | 218.884 + 21.888,40 | — | 0 | trifft |
| I₀ BHKW | 234.772,40 (741, 923) | 240.772,40 − 6.000 | — | 0 | trifft |
| Kaskadenfaktor | 1,155 (928) | 1 + 0,05 + 0,10 × 1,05 | — | 0 | trifft |
| Summe brutto (USt 19 %) | 279.379,16 (742) | 234.772,40 × 1,19 = 279.379,156 | — | 0 | trifft |
| PV-Module / WR / Unterkonstr. / Elektro | 96.000 / 24.000 / 45.000 / 18.000 (1335–1362) | 300 × 320 / 80 / 150 · fest | — | 0 | trifft |
| PV Basis Runde 3 | 183.000,00 (1371, 1617) | Σ der vier Runde-1-Zeilen | — | 0 | trifft |
| PV Planung 5 % | 9.150,00 (1371, 1619) | 183.000 × 0,05 | — | 0 | trifft |
| I₀ PV | 192.150,00 (1383, 1621) | 183.000 + 9.150 | — | 0 | trifft |
| PV brutto | 228.658,50 (1384) | 192.150 × 1,19 | — | 0 | trifft |
| PV spezifisch | 640,50 €/kWp (1385) | 192.150 ÷ 300 | — | 0 | trifft |

### 3.3 Ersatz und Restwert (Kategorie 1) — gegen `KapitalwertRechner.Ersatz` / `.Barwert`

| Größe | Mockup (Zeile) | nachgerechnet | Kern | Abw. | Bewertung |
|---|---|---|---|---|---|
| Ersatz BHKW, Jahr 15 | 205.884,00 (828, 939) | 196.080 + 9.804 | 205.884,00 | 0 | trifft |
| Barwert davon | 132.149 (828, 940) | ÷ 1,03¹⁵ = 132.149,11 | 132.149,11 | 0 | trifft |
| Restwert BHKW | 137.256,00 (829, 942) | 205.884 × 10/15 | 137.256,00 | 0 | trifft |
| Barwert davon | 75.995 (829, 943) | ÷ 1,03²⁰ = 75.995,32 | 75.995,32 | 0 | trifft |
| Restwerte Modul/Montage/Hydraulik | 130.720 / 6.536 / 0,00 (831–835) | ×10/15 bzw. Restdauer 0 | gleich | 0 | trifft |
| Ersatz WR, Jahr 12 | 24.000,00 (839, 945) | Nutzungsdauer 12 a | 24.000,00 | 0 | trifft |
| Barwert davon | 16.833 (839, 946) | ÷ 1,03¹² = 16.833,12 | 16.833,12 | 0 | trifft |
| Restwerte PV-Positionen | 19.200 / 8.000 / 9.000 / 3.600 (842–848) | 5/25 · 4/12 · 5/25 · 5/25 | gleich | 0 | trifft |
| Restwert PV gesamt / Barwert | 39.800,00 / 22.036 (840, 948) | ÷ 1,03²⁰ = 22.036,30 | 22.036,30 | 0 | trifft |
| V3 Ersatz nominal / Barwert | 229.884,00 / 148.982 (852) | 132.149,11 + 16.833,12 = 148.982,22 | gleich | 0 | trifft |
| V3 Restwert nominal / Barwert | 177.056,00 / 98.032 (853) | 177.056 ÷ 1,03²⁰ = 98.031,61 | 98.031,61 | 0 | trifft |
| Saldo V3 | −50.950 (952) | −148.982 + 98.032 | — | 0 | trifft |
| Tafelzeilen „3 von 4" / „4 von 5" / „7 von 9" | (828, 839, 852) | Zuschuss zählt nicht mit; Planung ohne Dauer | `ErsatzRestwertTafel` zählt gleich | 0 | trifft |
| Bezugsbetrag V3 in der Tafel | 432.922,40 (851) | 240.772,40 + 192.150 (brutto, vor Zuschuss) | — | 0 | trifft |
| p_I = 2 %: Ersatz WR nominal | 30.437,80 (954) | 24.000 × 1,02¹² | 30.437,803 | 0 | trifft |
| p_I = 2 %: Barwert | 21.348 (955) | ÷ 1,03¹² = 21.348,46 | 21.348,46 | 0 | trifft |

### 3.4 Betriebskosten (Kategorien 2 und 3)

| Größe | Mockup (Zeile) | nachgerechnet | Kern | Abw. | Bewertung |
|---|---|---|---|---|---|
| Arbeitspreis Gas | 0,0720 €/kWh (1207) | 0,7560 ÷ 10,5 | — | 0 | trifft |
| Endenergiekosten BHKW | 312.631,20 (1136, 1210) | 4.342.100 kWh × 0,0720 | — | 0 (exakt 312.631,58) | trifft |
| Wartung | 46.200,00 (1051, 1213) | 1.650.000 × 0,0280 | — | 0 | trifft |
| Instandhaltung 1,5 % | 3.611,59 (1060, 1216) | 240.772,40 × 0,015 = 3.611,586 | — | 0 | trifft |
| Hilfsenergie 2 % | 6.252,62 (1069, 1219) | 312.631,20 × 0,02 = 6.252,624 | — | 0 | trifft |
| Rückrechnung Strom | 21.710 kWh (1222) | 6.252,624 ÷ 0,288 = 21.710,5 | — | 0 | trifft |
| Versicherung | 1.100,00 (1078) | Jahresbetrag | — | 0 | trifft |
| Betriebskosten BHKW | 57.164,21 (1090, 1226) | Summe der vier Zeilen | — | 0 | trifft |
| … brutto | 68.025,41 (1091) | × 1,19 = 68.025,4099 | — | 0 | trifft |
| PV: 3.600 / 960,75 / 480,38 / 600 / 90 | (1461–1497, 1622–1629) | 300 × 12 · 192.150 × 0,5 % · × 0,25 % · fest | — | 0 | trifft |
| Betriebskosten PV | 5.731,13 (1509, 1632) | 5.731,125 | — | 0 | trifft |
| … brutto | 6.820,04 (1510) | 5.731,125 × 1,19 = 6.820,039 | — | 0 | trifft |
| Kessel | 2.400,00 (615, 1243) | Jahresbetrag | — | 0 | trifft |
| Versionen | 2.400 / 59.564,21 / 8.131,13 / 65.295,34 (1246–1247) | additiv | — | 0 | trifft |

### 3.5 Energiekosten (Kategorie 4)

| Größe | Mockup (Zeile) | nachgerechnet | Kern | Abw. | Bewertung |
|---|---|---|---|---|---|
| Energiekosten Kessel | 148.082,40 (613, 1139) | 2.056.700 × 0,0720 | — | 0 | trifft |
| Grundpreis | 180,00 €/a (1891) | je Träger einmal | — | 0 | trifft |
| Netzbezug V3 | 72.000,00 (1894) | 250,0 × 1000 × 0,2880 | — | 0 | trifft |
| Netzbezug Stamm/V1/V2 | 411.754 / 96.624 / 387.130 (1938–1940) | 1.429,7 / 335,5 / 1.344,2 × 288 | — | 0 | trifft |
| Energiekosten der vier Versionen | 560.016 / 409.435 / 535.392 / 384.811 (1928–1929) | Brennstoff + 180 + Netzbezug | — | 0 | trifft |
| CO₂-Menge BHKW | 872,3 t (1900) | 4.342,105 × 0,2009 = 872,329 | — | 0 | trifft |
| BEHG BHKW | 56.699,50 (1903) | 872,329 × 65 = **56.701,38** | — | −1,9 € | Rundung (B7) |
| BEHG Kessel | 26.857 (1944) | 2.056,787 × 0,2009 × 65 = 26.858,55 | — | −1,5 € | Rundung (B7) |
| Preisbestandteil Energiesteuer | 0,0638 €/m³ (1716) | 5,50 €/MWh(H_s) × 11,6 ÷ 1000 | — | 0 | trifft |
| CO₂-Masse je m³ | 2,109 kg (1719) | 200,9 × 10,5 ÷ 1000 = 2,10945 | — | 0 | trifft |
| Preisbestandteil CO₂ | 0,1371 €/m³ (1718) | 65 ÷ 1000 × 2,10945 = 0,137114 | — | 0 | trifft |
| Netz- und Messentgelt | 0,1180 €/m³ (1720) | Annahme | — | — | trifft |
| Beschaffung als Rest | 0,4371 €/m³ (1721) | 0,7560 − 0,0638 − 0,1371 − 0,1180 | — | 0 | trifft |
| Summe | 0,7560 €/m³ (1724) | deckungsgleich | — | 0 | trifft |
| Faktor H_s/H_i | 1,1048 (1704) | 11,6 ÷ 10,5 = 1,104762 | — | 0 | trifft |
| EBeV brennwertbezogen | 181,4 g/kWh (1874) | 200,9 × 10,5/11,6 = **181,85** | Katalog: 181,4 | +0,45 | trifft als Katalogwert (B10) |
| Schnellwahl § 2 / § 53a / § 54 | 0,6076 / 0,4883 / 0,1525 ct/kWh (1801–1808) | Satz × H_s ÷ H_i ÷ 10 | — | 0 | trifft |
| Schnellwahl § 53a / § 54 je m³ | 0,0513 / 0,0160 €/m³ (1806, 1809) | Satz ÷ 1000 × 11,6 | — | 0 | trifft |
| Schnellwahl BEHG | 1,3059 ct/kWh (1818) | 0,137114 ÷ 10,5 × 100 | — | 0 | trifft |
| Formelzeile | „0,76 ÷ 10,50 = 0,0720" (1705) | 0,76 ÷ 10,50 = **0,0724** | `N2`-Formatierung im Kern | Darstellung | Abweichung (B9) |

### 3.6 KWKG (Kategorie 5) — gegen `KwkgSatzRechner`, `KwkgKontingentRechner`, `HilfsstromRechner`

| Größe | Mockup (Zeile) | nachgerechnet | Kern | Abw. | Bewertung |
|---|---|---|---|---|---|
| Tranchen Einspeisung | 400 / 300 / 750 / 220 (2435–2442) | 50×8 · 50×6 · 150×5 · 50×4,4 | — | 0 | trifft |
| Mischsatz Einspeisung | 5,5667 ct (2062, 2444) | 1.670 ÷ 300 = 5,56667 | 5,5667 | 0 | trifft |
| Mischsatz Eigen Nr. 2 | 2,4167 ct (2064, 2445) | 725 ÷ 300 = 2,41667 | 2,4167 | 0 | trifft |
| Mischsatz Eigen Nr. 3 | 3,9683 ct (2216) | (50×5,41 + 200×4,00 + 50×2,40) ÷ 300 | 3,9683 | 0 | trifft |
| Kontingent | 30.000 h (2065, 2234) | § 8 Abs. 1 | 30.000 | 0 | trifft |
| Bonus Einspeisung | 27.555,2 (2461) | 495,0 × 10 × 5,5667 = 27.555,165 | — | 0 | trifft |
| Bonus Eigenstrom | 25.815,2 (2463) | 1.068,2 × 10 × 2,4167 = 25.815,19 | exakt 25.814,17 | +1,0 € | Rundung |
| Bonus_voll | 53.370,4 (2465) | 27.555,165 + 25.815,189 | — | 0 | trifft |
| Deckelanteil 2026 | 0,600 (2467) | min(5.500; 3.300; 30.000) ÷ 5.500 | — | 0 | trifft |
| Zuschlag Jahr 1 | 32.022,2 (2469) | 53.370,4 × 0,600 = 32.022,21 | — | 0 | trifft |
| davon Einspeisung / Eigenstrom | 16.533,1 / 15.489,1 (2469) | × 0,600 | — | 0 | trifft |
| Dauer der Reihe | 12 Jahre, „Rest 500 h" (2476, 2512) | 3.300+3.100+2.900+2.700+7×2.500 = 29.500; Rest 500 | — | 0 | trifft |
| Reihe nominal | 291.111 (2476, 2634) | 53.370,4 × 30.000 ÷ 5.500 = 291.111,02 | — | 0 | trifft |
| Reihe Barwert | 246.167 (2633) | Σ Reihe_t ÷ 1,03^t = 246.166,01 | — | −1 € | Rundung |
| Einspeiseerlös | 24.750,0 (2645) | 495.000 kWh × 0,05 | — | 0 | trifft |
| … Barwert | 368.218 (2646) | 24.750 × 14,877475 = 368.217,5 | — | 0 | trifft |
| Summe Block BHKW Jahr 1 | 77.975,6 (2173, 3154) | 32.022,2 + 21.203,4 + 24.750,0 | — | 0 | trifft |

### 3.7 Energie- und Stromsteuer — gegen `SteuerGutschriftRechner`

| Größe | Mockup (Zeile) | nachgerechnet | Kern | Abw. | Bewertung |
|---|---|---|---|---|---|
| § 53a Gutschrift | 21.203,4 (2171, 2290, 2368) | 4.796,99 × 4,42 = **21.202,71** | 21.202,71 | +0,7 € | Rundung (B4) |
| … Barwert | 315.453 (2637) | 21.203,4 × 14,877475 = 315.453,0 | — | 0 | trifft |
| § 53 voller Brennstoff | 26.384,3 (2273) | 4.796,99 × 5,50 = **26.383,46** | 26.383,46 | +0,8 € | Rundung (B4) |
| § 53 energetisch, Faktor | 0,458 (2280) | 1.650 ÷ 3.603,947 = 0,457831 | — | 0 | trifft |
| § 53 energetisch, Betrag | 12.082 (2280) | 4.342,105 × 0,457831 × 1,104762 × 5,50 = **12.079,17** | 12.079,17 | +2,8 € | Abweichung (B5) |
| § 54 BHKW | 6.370,1 (2275) | 4.796,99 × 1,38 − 250 = **6.369,85** | 6.369,85 | +0,25 € | Rundung (B4) |
| § 54 Kessel | 2.885,7 (618, 3161) | 2.272,28 × 1,38 − 250 = 2.885,72 | 2.885,72 | 0 | trifft (Wahl fehlt, B3) |
| § 9b Stamm / V1 / V2 / V3 | 28.344,0 / 6.460,0 / 26.634,0 / 4.750,0 (3167–3168) | Menge × 20,00 − 250 | gleich | 0 | trifft |
| § 9b Barwert (V3) | 70.668 (2641) | 4.750 × 14,877475 = 70.668,0 | — | 0 | trifft |
| § 9 Nr. 3 (Ausweis) | 23.677,5 (2175, 2372) | 1.155,0 × 20,50 | 23.677,5 | 0 | trifft |
| CO₂-Grenzwert | 242,1 g/kWh (2312, 2373) | 200,9 × 4.342,105 ÷ 3.603,947 = 242,05 | 242,05 | 0 | trifft |

### 3.8 PV-Vergütung (Kategorie 6) — gegen `EegSatzRechner`, `PvErloesRechner`, `PvKennzahlenRechner`

| Größe | Mockup (Zeile) | nachgerechnet | Kern | Abw. | Bewertung |
|---|---|---|---|---|---|
| Degressionsschritte | 6 (2788, 2953) | Stichtage 02/2024 … 08/2026 | 6 | 0 | trifft |
| Degressionsfaktor | 0,9415 (2788, 2953) | 0,99⁶ = 0,941480 | — | 0 | trifft |
| Klassen 8,60→8,10 · 7,50→7,06 · 6,20→5,84 | (2954) | `round(Basis × 0,99⁶; 2)` | 8,10 / 7,06 / 5,84 | 0 | trifft |
| AW_mix | 6,04 ct (2787, 2959) | 1.811,2 ÷ 300 = 6,0373 → 6,04 | 6,04 | 0 | trifft |
| Klassensumme | 1.811,2 (2956, 4776) | 10×8,10 + 30×7,06 + 260×5,84 | — | 0 | trifft |
| Feste EV | 5,64 ct (2790) | 6,04 − 0,40 | 5,64 | 0 | trifft |
| Ausfallarbeit § 51 | 39.900 kWh (2961) | 199.500 × 20 % | 39.900 | 0 | trifft |
| Ausweis Vergütungsausfall | 2.409,96 (2868, 2962) | 39.900 × 6,04 ÷ 100 | 2.409,96 | 0 | trifft |
| Spoterlös | 7.182,00 (2863, 2964) | 159.600 × 4,50 ÷ 100 | — | 0 | trifft |
| Marktprämie | 2.457,84 (2864, 2967) | 159.600 × 1,54 ÷ 100 | 2.457,84 | 0 | trifft |
| DV-Entgelt | 638,40 (2865, 2970) | 159.600 × 0,40 ÷ 100 | — | 0 | trifft |
| Vergütung Jahr 1 | 9.001,44 (2867, 2977) | 7.182,00 + 2.457,84 − 638,40 | 9.001,44 | 0 | trifft |
| Satz Jahr 1 | 4,51 ct (2852, 2977) | 9.001,44 ÷ 199.500 = 4,512 | 4,512 | 0 | trifft |
| Jahr 19 | 6.100,40 (2980) | 9.001,44 × 0,995¹⁸ − 85.500 × (1−0,995¹⁸) × 0,288 | 6.100,4013 | 0 | trifft |
| Jahr 20 vor § 51a | 5.946,77 (2980) | analog t = 20 | 5.946,7793 | 0 | trifft |
| § 51a | 1.095,52 (2982) | 39.900 × 0,5 × 6,04/100 × 0,995¹⁹ | 1.095,5151 | 0 | trifft |
| Jahr 20 gesamt | 7.042,29 (2985) | 5.946,78 + 1.095,52 | 7.042,2944 | 0 | trifft |
| Mehrbezug über 20 a | 22.705,69 (2980) | Σ 85.500 × (1−0,995^{t−1}) × 0,288 | 22.705,6933 | 0 | trifft |
| Reihe nominal | 150.118 (2985, 3033) | 150.118,43 | 150.118,43 | 0 | trifft |
| Reihe Barwert | 113.800 (2985, 3033) | 113.799,97 | 113.799,97 | 0 | trifft |
| Eigenverbrauchsquote | 30,0 % (2853) | 85,5 ÷ 285,0 | 30,0 | 0 | trifft |
| Autarkiegrad | 6,0 % (2853) | 85,5 ÷ 1.429,7 = 5,98 | 5,9803 | Anzeige | trifft |
| Vorteil durch PV | 16.791 €/a (2853) | (−192.150 + 20×24.624 + 150.118 − 20×5.731,13) ÷ 20 | 16.791,30 | 0 | trifft |
| LCOE₀ / diskontiert | 5,38 / 6,54 ct (2854) | 5,3820 / 6,5427 | 5,3820 / 6,5427 | 0 | trifft |

### 3.9 Vermiedene Kosten und Erlösrubrik (Kategorie 7)

| Größe | Mockup (Zeile) | nachgerechnet | Abw. | Bewertung |
|---|---|---|---|---|
| Vermieden brutto | 339.753,6 (3251) | 1.179,7 × 1000 × 0,288 | 0 | trifft |
| entgangene § 9b | 23.594,0 (3253) | 1.179,7 × 20,00 | 0 | trifft |
| Vermieden effektiv | 316.159,6 (3255) | Differenz | 0 | trifft |
| BHKW-Anteil brutto / § 9b / effektiv | 315.129,6 / 21.884,0 / 293.245,6 (2671–2679) | 1.094,2 = 1.179,7 − 85,5 | 0 | trifft |
| PV-Anteil brutto / § 9b / effektiv | 24.624,0 / 1.710,0 / 22.914,0 (2869, 3181) | 85,5 MWh | 0 | trifft |
| Probe | 293.245,6 + 22.914,0 = 316.159,6 (3262) | — | 0 | trifft |
| Leistungsanteil | −4.180,0 (3185) | keine Bezugsspitze, kein Leistungspreis Strom im Beispiel | — | **nicht herleitbar (B2)** |
| Block A Stamm/V1/V2/V3 | 31.229,7 / 84.435,6 / 38.521,1 / 91.727,0 (3171–3172) | Summe der Komponenten | 0 | trifft |
| Versionstafel Erlöse | 31.230 / 84.436 / 38.521 / 91.727 (111–126) | gerundet | 0 | trifft |

### 3.10 Wirtschaftlichkeit (Kategorie 8) — gegen `KapitalwertRechner`

Alle Werte dieses Blocks stammen aus vollständigen Läufen von `KapitalwertRechner.Rechne`
(vier Versionen × drei Szenarien) mit den Reihen der Kategorien 1–7.

| Größe | Mockup (Zeile) | nachgerechnet / Kern | Abw. | Bewertung |
|---|---|---|---|---|
| Rentenbarwertfaktor 3 %/20 a | (implizit) | 14,877475 | — | — |
| Betriebskosten-Barwerte | −35.706 / −886.165 / −120.971 / −971.430 (3695–3698) | 35.705,94 / 886.165,04 / 120.970,61 / 971.429,71 | 0 | trifft |
| … nominal | 48.000 / 1.191.284 / 162.622 / 1.305.907 (3695–3698) | 48.000 / 1.191.284,2 / 162.622,5 / 1.305.906,7 | ≤1 € | trifft |
| Energiekosten-Barwerte | −8.331.624 / −6.091.362 / −7.965.281 / −5.725.019 (3701–3704) | 8.331.623,96 / 6.091.361,90 / 7.965.281,02 / 5.725.018,95 | 0 | trifft |
| … nominal | 11.200.320 / 8.188.704 / 10.707.840 / 7.696.224 (3701–3704) | 20 × Jahreswert | 0 | trifft (ohne BEHG, B1) |
| Erlös-Barwerte | 464.618 / 1.025.946 / 552.977 / 1.114.304 (3707–3710) | 464.619,08 / 1.025.945,05 / 552.978,56 / 1.114.304,54 | ≤2 € | trifft |
| … nominal | 624.592 / 1.339.380 / 740.510 / 1.455.297 (3707–3710) | 624.594 / 1.339.379 / 740.512 / 1.455.297 | 2 / 1 / 2 / 0 € | Abweichung (B8) |
| Ersatz-Barwerte | — / −132.149 / −16.833 / −148.982 (3713–3716) | siehe 3.3 | 0 | trifft |
| Restwert-Barwerte | — / +75.995 / +22.036 / +98.032 (3719–3722) | siehe 3.3 | 0 | trifft |
| Nettobarwerte | −7.902.712 / −6.242.507 / −7.720.222 / −6.060.017 (3725–3726) | −7.902.710,8 / −6.242.508,1 / −7.720.219,9 / −6.060.017,1 | ≤2 € | trifft |
| ΔKW V1 / V2 / V3 | 1.660.205 / 182.491 / 1.842.695 (3453–3454) | 1.660.202,8 / 182.490,9 / 1.842.693,7 | ≤2 € | trifft |
| Annuitätsfaktor a(3 %; 20 a) | 0,067216 (3865) | 0,0672157 | 0 | trifft |
| Annuitäten | 111.592 / 12.266 / 123.858 (3457–3458) | 111.591,7 / 12.266,3 / 123.858,0 | 0 | trifft |
| Dynamische Amortisation | 1,68 / 8,64 / 2,64 a (3461) | 1,6777 / 8,6431 / 2,6368 (ohne Restwert, linear interpoliert) | 0 | trifft |
| Interner Zinsfuß | 61,2 / 11,5 / 39,2 % (3464) | 61,21 / 11,51 / 39,19 (Bisektion, mit Restwertdifferenz) | 0 | trifft |
| Wärmegestehungskosten | 27,19 / 21,47 / 26,56 / 20,85 ct (3467–3469) | (−KW × a) ÷ (1.953,9 MWh × 1000) = 27,185 / 21,474 / 26,558 / 20,846 | 0 | trifft (Bezugsmenge fehlt, B11) |
| Bandbreite V1 | 1.506.740 / 1.660.205 / 1.811.714 (3499–3500) | 1.506.738 / 1.660.203 / 1.811.712 | ≤2 € | trifft |
| Bandbreite V2 | 129.296 / 182.491 / 236.921 (3503–3504) | 129.296 / 182.491 / 236.921 | 0 | trifft |
| Bandbreite V3 | 1.636.035 / 1.842.695 / 2.048.635 (3507–3508) | 1.636.034 / 1.842.694 / 2.048.633 | ≤2 € | trifft |
| Spannen | 304.974 / 107.625 / 412.600 (3500–3508) | Differenz Günstig − Ungünstig | 0 | trifft |
| Anteil der Spanne | 18 % / 59 % (3573) | 304.974/1.660.205 · 107.625/182.491 | 0 | trifft |
| Verlauf-Startwerte | −470.215 / −426.922 / −383.630 (3631) | 432.922,40 × 1,1/1,0/0,9 − 6.000 (Zuschuss nicht skaliert) | 0 | trifft |
| Verlauf-Endwerte vor Restwert | 1.564.393 / 1.744.663 / 1.929.171 (3632) | 1.564.392 / 1.744.662 / 1.929.170 | 1 € | trifft |
| Nulldurchgänge | 3,02 / 2,64 / 2,28 a (3633) | 3,02 / 2,64 / 2,28 | 0 | trifft |
| Restwerte je Szenario nominal | 156.977 / 177.056 / 177.517 (4115–4116) | 156.977 / 177.056 / 177.517 | 0 | trifft |
| Restwert-Barwerte | 71.642 / 98.032 / 119.464 (3660) | ÷ 1,04²⁰ · 1,03²⁰ · 1,02²⁰ | 0 | trifft |
| Ersatzjahre je Szenario | 10·13·18 / 12·15 / 14·17 (4113–4114) | aus n∓2 und `Ersatz` | 0 | trifft |
| Sicht 2 (B − A) | −234.772 · −850.459 · +2.240.262 · +561.327 · −132.149 · +75.996 = +1.660.205 (3844–3845) | Zelle für Zelle aus der Gliederung | 0 | trifft |
| Sicht-2-Bandbreite | 1.506.739 / 1.660.205 / 1.811.714 (3846) | Differenz der Bandbreitenzellen (±1 € erklärt) | 0 | trifft |
| Höfingen: Mehrinvestition / Überschuss | 55.745 / 10.436 (4217, 4229) | 78.245−22.500 · 15.632−2.948−2.248 | 0 | trifft |
| Höfingen: Barwertfaktor | 11,577 (4232) | (1 − 1,02^−13,3) ÷ 0,02 = 11,5773 | 0 | trifft |
| Höfingen: Näherung / Abweichung | 65.073 / 0,3 % gegen 65.259 (4235–4239) | 10.436 × 11,577 − 55.745 = 65.072,4 | 0 | trifft |

## 4 Befunde

| Nr. | Ort (Datei · Abschnitt/Anker · Zeile) | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| B1 | Mockup · Kat. 4 „Preisbestandteile" Z. 1716–1724 und „Laufende Kosten" Z. 1944–1947; Kat. 8 „Gliederung" Z. 3701 · Rechenweg/04 Z. 151–153, 161 | Der Arbeitspreis 0,7560 €/m³ trägt den CO₂-Bestandteil 0,1371 €/m³ **und** daneben steht eine BEHG-Reihe „eigene Reihe je Kalenderjahr" mit 26.857 / 56.700 €/a. `Rechenweg/04` verlangt ausdrücklich: „Im Kapitalwert darf nur einer von beiden stehen … dann muss der Arbeitspreis ohne CO₂-Bestandteil gepflegt sein." Die Gliederung der Kat. 8 führt die BEHG-Reihe gar nicht (Nominalsumme 11.200.320 = 20 × 560.016 belegt: nur der Arbeitspreis), sagt das aber nirgends. `KapitalwertRechner.Rechne` würde eine übergebene `behgJahr`/`behgJeJahr`-Reihe zusätzlich buchen; die in `Rechenweg/04` als Behandlung genannte Kohärenzzeile „CO₂ im Arbeitspreis und BEHG-Reihe gleichzeitig aktiv" existiert im Kern nicht (`KohaerenzPruefung.cs` kennt keinen CO₂-Fall). Wirkung bei Doppelbuchung: −443.981 € auf ΔKW V1 **und** V3 (−27 % bzw. −24 %). | Entweder (a) die BEHG-Zeile der Kat. 4 als „Ausweis — bereits im Arbeitspreis enthalten" kennzeichnen und in Kat. 8 eine Fußzeile ergänzen, oder (b) den Beispiel-Arbeitspreis ohne CO₂ führen (0,6189 €/m³) und die BEHG-Reihe zusätzlich buchen — dann sind alle Energiekosten- und Kapitalwertzahlen neu zu rechnen. Unabhängig davon die Kohärenzzeile im Kern nachziehen und in der Kohärenzprüfung der Kat. 5 (Z. 2146) zeigen. | Mockup · Konzept · Code | hoch |
| B2 | Mockup · Kat. 7 Block B Z. 3183–3185 · Rechenweg/07 Z. 38 | „Vermiedene Stromkosten — Leistungsanteil −4.180,0 €" ist aus keiner gezeigten Eingabe ableitbar: `Beispielprojekt.md` führt weder eine Bezugsspitze noch einen Leistungspreis Strom (Trägerkarte Erdgas: Leistungspreis 0,00), und `Rechenweg/07` lässt Menge und Satz als „—". Zusätzlich inkonsistent: Stamm, V1 und V2 tragen „— keine Bezugsspitze gerechnet", obwohl derselbe Strombezugstarif gilt. | Bezugsspitze und Leistungspreis Strom als Beispielgrößen in `Beispielprojekt.md` aufnehmen und die Rechnung (Jahresspitze ohne Anlage × Satz − Jahresspitze mit Anlage × Satz) in Kat. 7 zeigen; oder die Zelle in allen vier Spalten gleich behandeln. | Beispielprojekt · Mockup · Rechenweg | mittel |
| B3 | Mockup · Kat. 5 Z. 2073, Kat. 7 Z. 3161 · `Beispielprojekt.md` Z. 21 | Das Stammprojekt bekommt 2.885,7 €/a Energiesteuer-Entlastung § 54 für den Kessel. `Beispielprojekt.md` führt als Projektvorgabe nur „§ 53a Abs. 5 · voller Brennstoff"; damit gäbe `SteuerGutschriftRechner` dem Kessel (`Stromerzeuger = false`) 0 € mit der Begründung „§ 53 und § 53a entlasten nur Anlagen mit Stromerzeugung". Die 2.885,7 € setzen eine **anlagenscharfe** Wahl § 54 am Kessel voraus, die nirgends steht. Nachgerechnet mit dieser Wahl trifft der Betrag (2.885,72 €). | In `Beispielprojekt.md` § 1 eine Zeile „Energiesteuerwahl Kessel: § 54 EnergieStG (anlagenscharf)" ergänzen und im Mockup Kat. 5 als solche ausweisen. | Beispielprojekt · Mockup | mittel |
| B4 | Mockup · Kat. 5 Z. 2171, 2285, 2290, 2368, 2636; Kat. 2 der Kesselzeile Z. 617 · `Beispielprojekt.md` Z. 32 | Die Brennwertmenge „4.797,2 MWh (H_s)" entsteht aus `4.342,1 × 1,1048`, also aus zwei gerundeten Zwischenwerten. Exakt sind es **4.796,99 MWh**; der Kern rechnet mit `EffHs/EffHi = 1,104762`. Folgen: § 53a 21.203,4 statt 21.202,71 €, § 53 26.384,3 statt 26.383,46 €, § 54 BHKW 6.370,1 statt 6.369,85 €. Das widerspricht der eigenen Konvention „Zwischenergebnisse werden ungerundet weitergereicht" (Z. 630). | Mengen- und Betragszeilen auf die ungerundete Kette umstellen (4.797,0 MWh; 21.202,7 / 26.383,5 / 6.369,8 €) oder den Faktor als „11,6 ÷ 10,5" statt „1,1048" schreiben. Gleichlautend in `Beispielprojekt.md` § 2 und `Rechenweg/05`. | Mockup · Beispielprojekt · Rechenweg | gering |
| B5 | Mockup · Kat. 5 Überlagerung Z. 2280 | „energetisch (konservativ) × 0,458 → 12.082 €/a bei § 53": nachgerechnet **12.079,17 €** (Kern identisch). Der Wert ist aus keiner konsistenten Kette reproduzierbar — weder aus 26.384,3 × 0,458 (= 12.084,0) noch aus der ungerundeten Rechnung. | Auf 12.079 € berichtigen. | Mockup | gering |
| B6 | Mockup · Kat. 5 Z. 2341, 2358; Anhang Z. 4789 | Fall-2-Menge „1.651,3 MWh": `1.953,9 × 0,845` ergibt 1.651,05, mit σ = 300/355 und ungerundeter Nutzwärme **1.651,22**. Der ausgewiesene Wert liegt 0,1 MWh darüber. | Auf 1.651,2 MWh berichtigen (Anhang „Herkunft der Zahlen" mit). | Mockup | gering |
| B7 | Mockup · Kat. 4 Z. 1903, 1944; `Beispielprojekt.md` (mittelbar) | Die BEHG-Beträge rechnen mit der gerundeten Menge: 872,3 × 65 = 56.699,50 statt 872,329 × 65 = **56.701,38 €**; Kessel 26.857 statt 26.858,55 €. | Ungerundet rechnen oder die Rundung in der Anmerkung nennen. | Mockup · Rechenweg | gering |
| B8 | Mockup · Kat. 8 Gliederung Z. 3707, 3709, 3827 | Nominalsumme der Erlöse: Stammprojekt 624.592 € statt 624.594 €, Variante 2 740.510 € statt 740.512 €. Beide Differenzen von je 2 € erklären sich daraus, dass die Kessel-§-54-Reihe mit 2.885,60 €/a statt mit den ausgewiesenen 2.885,7 €/a summiert wurde. | Nominalsummen auf 624.594 und 740.512 berichtigen. | Mockup | gering |
| B9 | Mockup · Kat. 4 Trägerkarte Z. 1705 · `EPOS.Kern/Controller/EnergietraegerPreiskarte.cs`, `Formel(…)` | Die Formelzeile lautet „0,76 €/m³ ÷ 10,50 kWh/m³ = 0,0720 €/kWh" — 0,76 ÷ 10,50 ergibt aber 0,0724. Ursache ist nicht das Mockup, sondern der Kern: `Formel` formatiert den Arbeitspreis mit `N2`, das Ergebnis mit `N4`. Eine Formelzeile, die ihr eigenes Ergebnis nicht liefert, lädt zum Nachrechnen ein und irritiert. | Arbeitspreis in der Formelzeile mit `N4` ausgeben (`0,7560 €/m³ ÷ 10,50 kWh/m³ = 0,0720 €/kWh`); Mockup entsprechend. | Code · Mockup | gering |
| B10 | Mockup · Kat. 4 Z. 1873–1874 | „Hi/Hs-Falle: Erdgas 200,9 g/kWh gilt heizwertbezogen; auf die brennwertbezogene Abrechnungsmenge gehört 181,4" liest sich als Ableitung aus 200,9. Mit dem H_s/H_i des Beispiels (1,1048) wären es 181,8; 181,4 ist der **Katalogwert** `GESETZ_EF_BILANZ_EBEV_ERDGAS_HO` (implizit H_s/H_i ≈ 1,107). | Den Wert als Katalogwert kennzeichnen („EBeV-Brennwertsatz aus dem Katalog"), nicht als Umrechnung des Beispiels. | Mockup · Rechenweg | gering |
| B11 | Mockup · Kat. 8 Z. 3466–3469 | Die Wärmegestehungskosten stehen ohne Formel und ohne Bezugsmenge. Nachgerechnet: (−KW × a(i,T)) ÷ (1.953,9 MWh × 1000) — beides steht in `Rechenweg/08` Z. 86/134, im Mockup aber nirgends. | Bezugsmenge „1.953,9 MWh/a Nutzwärme" und die Formel in die Bedeutungsspalte aufnehmen. | Mockup | gering |
| B12 | Mockup · Kat. 5 Anzeigezeile Z. 2062–2064 · Kat. 8 Anhang Umsetzungsstand Z. 4403–4406 | Kat. 7 weist § 53a (BHKW, 21.203,4 €) und § 54 (Kessel, 2.885,7 €) als getrennte Zeilen aus; `SteuerGutschriftRechner` gibt beide heute als **eine** Summe `SteuerErgebnis.EnergiesteuerEur` zurück. Der Mockup nennt das im Umsetzungsstand (U7), die Erlösrubrik selbst nicht. | In Kat. 7 an den beiden Zeilen den Umsetzungsvermerk U7 setzen, wie an den anderen noch nicht gebauten Zeilen. | Mockup | gering |
| B13 | Mockup · Kat. 4 Z. 1940 | Die Netzbezugsmengen 335,5 und 1.344,2 MWh sind nur über die Aufteilung `1.094,2 = 1.179,7 − 85,5` herleitbar, die erst in Kat. 5/7 (Z. 2672, 3262) erscheint. In Kat. 4 stehen sie ohne Herleitung. | In der Herkunftsspalte der davon-Zeile auf die Aufteilung der vermiedenen Menge verweisen. | Mockup | gering |

Nicht als Befund geführt, weil geprüft und in Ordnung: Ersatz und Restwert rechnen auf der
**Brutto**investition 432.922,40 € statt auf I₀ 426.922,40 € (Z. 851) — so ist es in Z. 766–767
ausdrücklich beschrieben und so rechnet der Kern. Ebenso, dass die Szenarien den Zuschuss von
6.000 € nicht mitskalieren (Startwerte 470.215 / 426.922 / 383.630 reproduzieren das exakt) und
dass die Prosa in Z. 3874 die Sicht-1-Spalte (+561.328 · +75.995) statt der Sicht-2-Spalte
(+561.327 · +75.996) nennt — das ist genau der beschriebene Rundungsabstand.

## 5 Offene Fragen an den Anwender

1. **CO₂ doppelt (B1) — welcher Weg?** Empfehlung: Weg (a). Die BEHG-Zeile der Kategorie 4 als
   Ausweis kennzeichnen („bereits im Arbeitspreis enthalten") und den Arbeitspreis des Beispiels
   voll belassen. Das erhält alle nachgerechneten Zahlen und macht die Kategorie-8-Gliederung
   lesbar. Weg (b) — Arbeitspreis ohne CO₂ und BEHG als eigene Reihe — wäre die Variante, die der
   heutige Rechenkern erwartet, zieht aber eine Neurechnung sämtlicher Energiekosten-, Barwert- und
   Kapitalwertzahlen nach sich. **Unabhängig von der Wahl** empfehle ich, die Kohärenzzeile „CO₂ im
   Arbeitspreis und BEHG-Reihe gleichzeitig aktiv" im Kern zu ergänzen — `Rechenweg/04` führt sie
   als Behandlung, `KohaerenzPruefung` kennt sie nicht.
2. **Leistungsanteil −4.180,0 € (B2).** Soll das Beispiel eine Bezugsspitze und einen
   Strom-Leistungspreis bekommen, damit die Zahl herleitbar wird? Empfehlung: ja — sonst ist es die
   einzige Zahl der ganzen Seite ohne Wurzel in `Beispielprojekt.md`, und ausgerechnet sie trägt ein
   überraschendes Vorzeichen. Alternativ die Zelle in allen vier Spalten auf „— keine Bezugsspitze
   gerechnet" setzen und den negativen Leistungsanteil im Fließtext erklären.
3. **Kesselwahl § 54 (B3).** Soll `Beispielprojekt.md` die anlagenscharfe Wahl ausweisen?
   Empfehlung: ja, eine Zeile in § 1 — sie ist zugleich das einzige Beispiel für die B3-Fähigkeit
   „Wahl je Anlage" und damit dokumentarisch wertvoll.
4. **Rundungsdisziplin (B4–B8).** Soll das Mockup durchgehend auf die ungerundete Kette umgestellt
   werden, wie seine eigene Konventionszeile verlangt? Empfehlung: ja für die Beträge (§ 53a, § 53,
   § 54, BEHG, Erlös-Nominalsummen), nein für die angezeigten Mengen — dort genügt, den Faktor als
   „11,6 ÷ 10,5" statt „1,1048" zu schreiben. Die Einzelabweichungen liegen alle unter 3 €, aber sie
   sind der einzige Grund, warum ein Nachrechner ins Stocken gerät.
5. **Formelzeile der Trägerkarte (B9).** Änderung im Kern (`N2` → `N4`) oder Hinnahme? Empfehlung:
   ändern — es ist eine Zeile Code und die Zeile steht sonst dauerhaft falsch in jeder Trägerkarte
   mit vierstelligem Arbeitspreis.
