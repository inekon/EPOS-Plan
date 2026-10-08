# Recherche: Grenzen der Wärmeübergabe und hydraulische Restriktionen von Wärmepumpen

Stand 07.10.2026 · Grundlage für ein EPOS-Plan-Konzept (Stundensimulation) · Normen, Richtlinien, Verbands- und Planungsliteratur; Herstellerunterlagen nur für herstellerübergreifende Regeln, keine Typen. Kennzeichen: **[Qn]** Quelle (Liste am Ende), **(sek.)** Sekundärquelle, **(Abl.)** eigene Rechnung mit den angegebenen Formeln, **(n. e.)** Normtext nicht eingesehen.

## 0 Kernaussagen

- Ein Heizkörper liefert bei 55/45/20 °C rund 51 % seiner Normleistung (75/65/20 °C), bei 50/40 °C rund 40 % (n = 1,3). Ein auf 70/55 °C ausgelegter Bestandsheizkörper deckt damit 64 % bzw. 50 % seiner Auslegungslast (Abl., Abschn. 1).
- Der COP sinkt je Kelvin Vorlauf um rund 1,5–2,5 %, die Heizleistung bleibt nahezu gleich (0 bis −0,3 %/K). Begrenzend ist die Einsatzgrenze (Höchstvorlauf), nicht ein stetiger Leistungsabfall ([Q13] Abl., [Q3]).
- Spreizung: Fläche 5 K, Heizkörper 5–7 K, höchstens 10 K. Der Mindestvolumenstrom ist herstellerspezifisch; aus σ_N/σ_max ergeben sich rund 50–70 % des Auslegungsvolumenstroms ([Q8], Abl.).
- Die Vorgaben 55 °C (R410A/R32) und 70 °C (R290) sind belegt. R744 gehört nicht in die 70-°C-Gruppe: Grenze ist der Rücklauf (≤ 30–35 °C), nicht der Vorlauf ([Q16], [Q18], [Q19], [Q20]).
- Ein Bivalenzpunkt von −5 °C deckt nach DIN V 4701-10/VDI 4650 rund 98 % (parallel) bzw. 91 % (alternativ) der Jahresheizarbeit ([Q23], [Q9]).
- Seit 29.07.2026 sind die 65-%-Pflicht und § 71h GEG aufgehoben (GModG). Eine WP-Hybridheizung erfüllt die Bio-Treppe bei WP-Leistung (Teillastpunkt A) ≥ 30 % (parallel/teilparallel) bzw. ≥ 40 % (alternativ) der Spitzenlastleistung; die BEG verlangt weiter 65 % EE ([Q27]–[Q32]).
- Bei Trennspeicher oder Weiche bestimmen beide Volumenströme Vor- und Rücklauf: Ein zu großer Verbraucherstrom zwingt die WP über die Heizkurve ([Q7], [Q3]).

## 1 Heizflächen

**Norm.** DIN EN 442-1/-2:2015-03 (EN 442:2014): Normwärmeleistung bei 75/65/20 °C, logarithmische Übertemperatur ΔT_ln,N = 49,8 K; früher galt 90/70/20 °C, alte Herstellerangaben sind umzurechnen [Q1]. Kennlinie Φ = K_M · ΔT_ln^n [Q1], [Q4].

| Bauart | Exponent n | Quelle |
|---|---|---|
| Plattenheizkörper | 1,20–1,30 | [Q1], [Q2], [Q3] |
| Glieder-/Röhrenradiatoren (Guss, Stahl) | ≈ 1,30 (glatte Rohre, Rippenrohre 1,25) | [Q1], [Q3] |
| Konvektoren (auch Unterflur) | 1,25–1,45 | [Q1], [Q2] |
| Gebläsekonvektoren (Prüfnorm DIN EN 16430) | ≈ 1,1–1,2 bei fester Lüfterstufe; bei temperaturgeführter Drehzahl stark abweichend | [Q2], [Q6] |
| Fußboden-/Flächenheizung | ≈ 1,1 (EN-1264-Kennlinie linear in Δθ_H, Basiskennlinie 1,1) | [Q1], [Q3], [Q5] |

n hängt von Anschlussart, Heizmittelstrom, Bauhöhe und Reihenzahl ab; praktisch darf es konstant gesetzt werden [Q1]. Für Plattenheizkörper mit Konvektionsblechen (Typ 22) weicht der EN-442-Ansatz über den üblichen Massenstrombereich um bis zu ≈ 10 % ab; ohne Bleche passt er, außer bei sehr kleinen Massenströmen [Q4].

**Umrechnung.** Φ/Φ_N = (ΔT_ln / 49,8 K)^n mit ΔT_ln = (θ_V − θ_R) / ln[(θ_V − θ_i)/(θ_R − θ_i)]. Die arithmetische Übertemperatur ist nur zulässig, solange c = (θ_R − θ_i)/(θ_V − θ_i) ≥ 0,7 [Q1]. Fehler der arithmetischen Übertemperatur (Abl.): c = 0,70 → +1,1 %; c = 0,60 (45/35) → +2,2 %; c = 0,53 (35/28) → +3,3 % (≈ +4 % Leistung bei n = 1,3). Für Auslegungspaar und Spreizung gibt es keine verbindliche Norm [Q1].

**Auslegungspaare im Bestand** [Q1]: 90/70 °C (etwa 1930 bis Mitte der 1970er), 70/55 °C (Niedertemperaturtechnik), 55/45 °C (Brennwerttechnik); 75/65 °C ist der Normpunkt. Fußbodenheizung 35/30 bzw. 35/28 °C (σ = 5 K im Auslegungsraum, übrige Räume größer) [Q5].

| Paar (θ_i = 20 °C) | 90/70 | 75/65 | 70/55 | 55/50 | 55/45 | 50/40 | 45/35 | 35/28 |
|---|---|---|---|---|---|---|---|---|
| ΔT_ln [K] | 59,4 | 49,8 | 42,1 | 32,4 | 29,7 | 24,7 | 19,6 | 11,1 |
| Φ/Φ_N, n = 1,25 | 1,25 | 1,00 | 0,81 | 0,59 | 0,52 | 0,42 | 0,31 | 0,15 |
| Φ/Φ_N, n = 1,30 | 1,26 | 1,00 | 0,80 | 0,57 | 0,51 | 0,40 | 0,30 | 0,14 |
| Φ/Φ_N, n = 1,40 | 1,28 | 1,00 | 0,79 | 0,55 | 0,49 | 0,37 | 0,27 | 0,12 |

**Rechenbeispiel Restleistung (Abl., n = 1,3).** Ein Heizkörper, der die Raumlast Q_D beim Auslegungspaar D gerade deckt, hat Φ_N = Q_D / (Φ_D/Φ_N). Anteil von Q_D bei abgesenktem Vorlauf; „konst. ṁ" = Auslegungsmassenstrom bleibt, Rücklauf stellt sich ein:

| Auslegung D | Φ_N/Q_D | 55/45 | 55/50 | 50/40 | VL 55, konst. ṁ | VL 50, konst. ṁ |
|---|---|---|---|---|---|---|
| 90/70 | 0,80 | 41 % | 46 % | 32 % | 42 % (RL 46,6 °C) | 35 % (RL 43,0 °C) |
| 75/65 | 1,00 | 51 % | 57 % | 40 % | 56 % (RL 49,4 °C) | 46 % (RL 45,4 °C) |
| 70/55 | 1,25 | 64 % | 71 % | 50 % | 64 % (RL 45,4 °C) | 53 % (RL 42,0 °C) |

Lesart: Für volle Deckung bei 55/45 °C braucht ein Raum Φ_N ≈ 1,96 · Q_D (50/40: 2,5; 45/35: 3,4; 35/28: 7,0). Bestandsheizkörper sind oft größer: Bei 70/55-Auslegung (1975) lag Φ_N ≈ 1,2 · Q_D, nach Modernisierung im Mittel ≈ 1,55 · Q_D, raumweise stark streuend [Q1]. Im Feld genügten auch in unsanierten Häusern mit alten Heizkörpern rund 55 °C [Q16]; der BWP empfiehlt Heizflächen, für die 60 °C ausreichen [Q9].

**Flächenheizung (DIN EN 1264).** Höchste Oberflächentemperatur: Aufenthaltszone 29 °C, Randzone (≤ 1 m breit) 35 °C, Bad θ_i + 9 K = 33 °C. Basiskennlinie q = 8,92 · (θ_F,m − θ_i)^1,1 W/m² → 100 W/m² bei 9 K, 175 W/m² bei 15 K; mehr leistet kein System [Q5]. Systemkennlinie q = K_H · Δθ_H (K_H aus Verlegeabstand, Estrich, Belag; Prüfstelle) bis zur Grenzkurve. Auslegung: Raum mit der höchsten Wärmestromdichte, σ = 5 K, R_λ,B = 0,10 m²K/W (Bad 0), Belag höchstens 0,15 m²K/W; übrige Räume mit größerer Spreizung; Vorlauf höchstens 55 °C [Q5]. Oberfläche typisch nur ≈ 2 K über Raumluft, daher starker Selbstregeleffekt [Q3].

## 2 Wärmepumpe, Verflüssigerseite

**Prüfbedingungen DIN EN 14511-2.** EN 14511-2:2022 kennt für Wasser vier Stufen (low, intermediate, medium, high) [Q14]:

| Stufe | Rücklauf/Vorlauf | Spreizung | Beleg |
|---|---|---|---|
| Niedertemperatur (W35) | 30/35 °C | 5 K | [Q11], [Q12], [Q13] |
| Zwischenstufe (W45) | 40/45 °C | 5 K | [Q10], [Q13] |
| Mitteltemperatur (W55) | 47/55 °C | 8 K | [Q11], [Q12], [Q13] |
| Hochtemperatur (W65) | 55/65 °C | 10 K | Stufe W65 [Q10], [Q14]; Paar n. e. |

Die Benennung schwankt: [Q10] nennt W45 „medium", W55 „high", W65 „very high"; Ökodesign und EN 14825 nennen 35 °C Nieder- und 55 °C Mitteltemperaturanwendung [Q9], [Q15]. Bei Geräten mit festem Volumenstrom wird dieser im Normpunkt (30/35 bzw. 47/55) eingestellt und für die Teillastprüfungen beibehalten; die Prüfstelle stellt je Stufe einen eigenen Volumenstrom ein [Q11], [Q12], [Q13]. Der COP hängt an der mittleren Verflüssigungstemperatur, deshalb legt die Norm Eintrittstemperatur und Volumenstrom mit fest [Q10].

**DIN EN 14825 (SCOP, Teillast; aktuell EN 14825:2022).** Mittleres Klima: T_design = −10 °C; Prüfpunkte A −7 °C (Teillast 88 %), B +2 °C (54 %), C +7 °C (35 %), D +12 °C (15 %), Teillast = (T_j − 16)/(T_design − 16). Vorlauf bei gleitender Austrittstemperatur (Design/A/B/C/D): Niedertemperatur 35/34/30/27/24 °C, Mitteltemperatur 55/52/42/36/30 °C [Q13], [Q15]. Der Hersteller erklärt T_biv (mittleres Klima ≤ +2 °C, n. e.) und TOL; unter T_biv rechnet der SCOP die Zusatzheizung mit COP 1 [Q15].

**Auslegungsspreizung und Grenzen.**
- Heizseite 5–7 K bei Auslegungsleistung, statische Heizflächen bis höchstens 10 K; Trinkwarmwasser höchstens 10 K; Quellenseite 3–5 K [Q8]. Flächenheizung 5 K im Auslegungsraum [Q5]. Verflüssiger: Spreizung 5 K (Auslegung), Grädigkeit 3–5 K [Q3].
- Volumenstrom je kW Wasser (ρ = 1 kg/l, c = 4,18 kJ/(kg·K)): 3 K 287 l/h, 5 K 172 l/h, 7 K 123 l/h, 8 K 108 l/h, 10 K 86 l/h [Q8].
- Mindestvolumenstrom: stets nach Herstellerangabe [Q8], [Q6]. Wirkmechanismen (Fachwissen, Werte gerätespezifisch): zu wenig Durchfluss führt zu Hochdruckabschaltung; die Abtauung durch Kreislaufumkehr entzieht dem Heizwasser Energie (Frostgefahr am Plattenübertrager bei zu wenig Volumen oder Durchfluss); zu wenig Volumen lässt die WP takten. Mindestvolumen ohne Puffer 3–5 l/kW, bei L/W zusätzlich Abtauenergie beachten [Q8]; VDI 4645: ≥ 3 l/kW nicht absperrbar, sonst Puffer, für die Abtauung 20 l/kW (sek. [Q37]). Im Bestand ist hydraulisch zu entkoppeln, damit der Mindestdurchsatz in allen Betriebssituationen gesichert ist; die Regelung schützt die WP vor zu hohem Rücklauf [Q7].
- Relativer Mindestvolumenstrom (Abl.): bei voller Leistung V̇_min/V̇_N = σ_N/σ_max, also 5 K/10 K = 50 %, 7 K/10 K = 70 %. Spreizungen unter ≈ 3 K kosten Pumpenenergie und Strömungsgeschwindigkeit; eine Normgrenze gibt es nicht.

**Höchstvorlauf je Kältemittel** (kritische Temperatur nach [Q18], R1234ze(E) nach [Q21]):

| Kältemittel | T_krit | typ. Höchstvorlauf | Anmerkung |
|---|---|---|---|
| R410A | 71 °C | 55–60 °C | Standard-WP erreichen 55–60 °C „ohne Probleme" [Q16]; Zwischeneinspritzung erweitert die Einsatzgrenzen [Q18] |
| R32 | 78 °C | 55–65 °C | Bandbreite aus Marktübersichten (sek.) |
| R290 | 97 °C | 65–75 °C | „Hochtemperatur-WP" 65–70 °C, Propan-Geräte bis 75 °C [Q16] |
| R744 (CO₂) | 31 °C | 80–90 °C (transkritisch) | Effizienz hängt am Rücklauf, Faustregel < 35 °C [Q19]; große Spreizung nötig, für Raumheizung „nur schwer", v. a. TWW und > 50 kW [Q18]; COP 2,8 bei 35/55 °C, +30 K Vorlauf nur 2,74 [Q20] |
| R1234ze(E) | 109 °C | 70–85 (90) °C | Großwärmepumpen und Kälte mit Abwärmenutzung, max. ≈ 85 °C [Q19] |

Der Höchstvorlauf sinkt bei tiefer Quellentemperatur (Einsatzgrenze, gerätespezifisches Kennfeld) [Q7], [Q9]. VDI 4650 Blatt 1:2024-02 hat Korrekturfaktoren bis 60 °C Vorlauf [Q22]; für die BEG ist der BWP-JAZ-Rechner bis 60 °C zulässig, monovalente Anlagen über 55 °C dürfen als bivalent-alternativ mit Heizstab abgebildet werden [Q32].

**COP und Leistung über dem Vorlauf** — eigene Auswertung der WPZ-Prüfresultate Luft/Wasser [Q13] (Mediane über alle Geräte mit beiden Messpunkten, ohne Gerätenamen; „je K" = (1 − Verhältnis)/ΔK, bezogen auf W35):

| Vergleich | n | COP-Verhältnis (Spanne) | COP je K | Heizleistung |
|---|---|---|---|---|
| A7/W47-55 zu A7/W30-35 (EN 14511:2011/13) | 24 | 0,64 (0,51–0,78) | −1,8 % | 0,99 (geregelt 0,99, fest 1,00) |
| A7/W40-45 zu A7/W30-35 | 25 | 0,79 (0,73–0,88) | −2,1 % | – |
| A−7/W55 zu A−7/W35 | 17 | 0,71 (0,45–0,75) | −1,5 % | 0,99 |
| A7/W55 zu A7/W35 (EN 14511:2004/07) | 29 | 0,65 (0,43–0,73) | −1,8 % | Festdrehzahl 0,94 (n = 22) |

Gegenproben: Temperaturhub 35 → 55 K senkt den COP von 4,4 auf 3,0 (−1,6 %/K) [Q3]; Carnot-Grenze −1,3 bis −1,5 %/K (Abl.); BWP-Faustregel „1 K weniger Vorlauf = 2,5–3 % weniger Strom" (sek.). Festdrehzahl-Geräte verlieren ≈ 0,3 %/K Heizleistung, geregelte halten sie bis zur Einsatzgrenze [Q13].

**Spreizung und COP.** Bei unterkritischen Kältemitteln zählt die mittlere Verflüssigungstemperatur [Q10]: Bei festem Vorlauf senkt eine größere Spreizung nur den Rücklauf (etwas mehr Unterkühlung, kleiner Gewinn). Bei fester mittlerer Heizflächentemperatur hebt jede zusätzliche 2 K Spreizung den Vorlauf um 1 K, also ≈ −2 % COP (Abl.). Eine WPZ-Untersuchung fand für R407C 10 K am günstigsten und unter 5 K deutliche Einbußen (sek. [Q38]). Für R744 gilt das Gegenteil: große Spreizung, tiefer Rücklauf [Q18], [Q19].

## 3 Hydraulische Einbindung

| Einbindung | Massenströme | Folge für das Modell | Quelle |
|---|---|---|---|
| direkt, ohne Puffer | ṁ_WP = ṁ_HK, σ_WP = σ_HK | Mindestdurchfluss über dauernd offene Teilfläche oder Überströmventil; Mindestvolumen 3–5 l/kW; nur Fläche, Sole/Wasser oder geregelte L/W | [Q8], [Q6] |
| Reihenpuffer (im Rücklauf) | gekoppelt (Differenzdruck-Überströmventil) | Puffer liefert Volumen für Laufzeit und Abtauung, glättet Temperaturen | [Q8], [Q6] |
| Parallelpuffer, Trennspeicher, hydraulische Weiche | entkoppelt | Mischung an der Trennstelle (s. u.) | [Q7], [Q3], [Q8] |

Nach BDH sollen WP- und Verbraucherkreis bei Vollöffnung gleiche Volumenströme führen. Ist der Verbraucherstrom größer, mischt sich Rücklauf in den Vorlauf und die WP muss stets über der Heizkurve fahren; ist der WP-Strom größer, wird der Speicher dauernd geladen und die WP-Leistung steht nicht voll zur Verfügung [Q7]. Das Schweizer Handbuch fordert ṁ_Erzeuger > ṁ_Verbraucher [Q3]. Mischungsbilanz (Abl.): Für ṁ_HK > ṁ_WP gilt θ_V,HK = [ṁ_WP·θ_V,WP + (ṁ_HK − ṁ_WP)·θ_R,HK]/ṁ_HK und θ_R,WP = θ_R,HK; für ṁ_WP > ṁ_HK gilt θ_V,HK = θ_V,WP und θ_R,WP = [ṁ_HK·θ_R,HK + (ṁ_WP − ṁ_HK)·θ_V,WP]/ṁ_WP.

Puffergröße: Laufzeitoptimierung 20–25 l/kW, Sperrzeitüberbrückung 30–40 l/kW [Q8] (VDI 4645: Abtauung 20 l/kW, Sperrzeit 30–40 l/kW je Stunde, sek. [Q37]). Nach § 14a EnWG werden Abschaltzeiten bei der Dimensionierung nicht mehr angesetzt [Q9]. Regelwerk: VDI 4645:2018-03 gilt, Entwurf VDI 4645 vom März 2026 (Einspruch bis 31.05.2026) [Q36]; VDI 4650 Blatt 1:2024-02 [Q6], [Q22]; DIN EN 15450:2007-12 gilt, Entwurf E DIN EN 15450:2026-02 mit neuem Anhang zu Deckungsgraden aus Wetterdaten [Q25].

## 4 Bivalenz

**Begriffe.** Zwei Schaltpunkte: oberhalb des Bivalenzpunkts arbeitet nur die WP, unterhalb des Abschaltpunkts keine WP [Q7]; gleichlautend [Q18], [Q26]; Betriebsweisen auch in DIN EN 15450 (n. e.) [Q25].

| Betriebsweise | über Bivalenzpunkt | zwischen Bivalenz- und Abschaltpunkt | unter Abschaltpunkt |
|---|---|---|---|
| monovalent | WP | WP | WP |
| monoenergetisch | WP | WP + Heizstab | nur Heizstab, sobald die WP-Einsatzgrenze unterschritten ist [Q18] |
| bivalent-parallel | WP | WP + Kessel | WP + Kessel (kein Abschaltpunkt) |
| bivalent-teilparallel | WP | WP + Kessel | nur Kessel |
| bivalent-alternativ | WP | entfällt (beide Punkte fallen zusammen) | nur Kessel |

Zweiter Erzeuger: bei alternativ/teilparallel für die volle Heizlast, bei parallel so, dass WP + Kessel die Heizlast decken [Q8]. Der Kessel wird erst zugeschaltet, wenn zugleich θ_a unter der Bivalenz- bzw. Abschalttemperatur liegt, die WP mit Höchstleistung läuft oder gesperrt ist und die Sollwerte dauerhaft unterschritten werden [Q7].

**Deckungsanteil (Tabelle aus DIN V 4701-10, Rechengrundlage der VDI 4650)** [Q23], [Q24]; Leistungsanteil = WP-Leistung bei Norm-Außentemperatur / Norm-Heizlast:

| Bivalenzpunkt [°C] | −10 | −7 | −5 | −3 | −2 | 0 | +2 | +5 |
|---|---|---|---|---|---|---|---|---|
| Leistungsanteil | 0,77 | 0,65 | 0,58 | 0,50 | 0,46 | 0,38 | 0,31 | 0,19 |
| Deckung parallel | 1,00 | 0,99 | 0,98 | 0,96 | 0,95 | 0,90 | 0,83 | 0,61 |
| Deckung alternativ | 0,96 | 0,94 | 0,91 | 0,83 | 0,78 | 0,64 | 0,46 | 0,19 |

[Q24] bestätigt die Zeile −10 °C („VDI 4650, Tabelle 8", Fassung 2009) und ordnet 0,96 dem teilparallelen **oder** alternativen Betrieb zu.

- BWP (monoenergetisch, geregelte L/W-WP): Leistungsanteil 50–80 % bei Norm-Außentemperatur (70–90 % bezogen auf −7 °C), Taktpunkt möglichst ≤ +5 °C; schon 50–80 % ergeben Deckungsanteile > 95 % [Q9]. Beispiel Potsdam (θ_NA −12 °C, 10 kW, 55 °C, Heizgrenze 15 °C): Leistungsanteil 42/65/88 % → Bivalenzpunkt +0,5/−5/−10 °C, Deckung 92,5/98,8/99,0 %, JAZ bei 55 °C 3,1/3,5/3,3 [Q9].
- Übliche Bivalenzpunkte L/W: monoenergetisch −5 bis −8 °C, bivalent −2 bis −4 °C (sek. [Q23]). Widerspruch: eine Hersteller-Planungsanleitung nennt für bivalent-parallel 50–70 % WP-Leistung bei nur 85–92 % Jahresanteil [Q39], deutlich unter der DIN-Tabelle (0,96–0,99).
- Norm-Außentemperatur: DIN/TS 12831-1:2020-04 postleitzahlgenau; Spanne der BWP-Klimakarte −18,8 bis −4,2 °C [Q9]. EN 14825: T_biv im mittleren Klima ≤ +2 °C (n. e.) [Q15].

**Recht und Förderung (Stand 07.10.2026).**
- Das Gebäudemodernisierungsgesetz (GModG, BGBl. 2026 I Nr. 226, gestaffelt in Kraft ab 29.07.2026) löst das GEG ab; die 65-%-Pflicht und § 71h GEG sind aufgehoben [Q29], [Q30]. Neue Gas-, Öl- und Flüssiggasheizungen im Bestand unterliegen der Bio-Treppe: 10 % (2029), 15 % (2030), 30 % (2035), 60 % (2040) [Q28], [Q41].
- § 43 GModG (Abs. 5 in [Q27]): Eine WP-Hybridheizung erfüllt die Pflicht, wenn die WP-Leistung bei Teillastpunkt A (DIN EN 14825) ≥ 30 % (bivalent-parallel/-teilparallel) bzw. ≥ 40 % (bivalent-alternativ) der Leistung des Spitzenlasterzeugers beträgt; für andere Betriebsweisen gilt die Nachweispflicht ab 01.01.2029; bei ≥ 6 WE oder Nichtwohngebäuden und Anrechnung > 30 % ist ein Nachweis erst nach 31.12.2039 nötig [Q27], [Q28].
- Bis 28.07.2026 galt § 71h GEG 2024: Vorrang der WP, ≥ 30 %/40 % der Heizlast oder ersatzweise Teillastpunkt A gegen die Spitzenlastleistung [Q30].
- BEG EM: Nach der Maßnahme müssen die versorgten Einheiten zu ≥ 65 % erneuerbar beheizt werden (TMA 3.1, KfW-Infoblatt 11.0 vom 16.09.2026) [Q31]. Für Hybride gilt das als erfüllt bei gemeinsamer, fernansprechbarer Steuerung und WP-Leistung (Teillastpunkt A, −7 °C) ≥ 30 % (parallel/teilparallel) bzw. ≥ 40 % (alternativ) der Gesamtleistung oder der Norm-Heizlast inkl. TWW; Gas- oder Öl-Spitzenlast nur als Brennwertkessel; bilanziert wird nach DIN V 18599 über Energiemengen ([Q32], Stand 01.06.2026, verweist noch auf § 71h GEG). Hybrid-Kompaktgeräte: 65 % der Ausgaben förderfähig; ein fossiler Spitzenlastkessel schließt den Klimageschwindigkeits-Bonus aus [Q31]. JAZ nach VDI 4650 Blatt 1 (aktuelle Fassung) [Q32], Mindestwert 3,0 (sek. [Q40]); neue, einkommensabhängige Fördersätze seit 21.07.2026 [Q41].

**Reihenschaltung WP → Kessel (Vorwärmbetrieb).** Anerkannte Grundschaltung: Die WP fährt Grundlast, der Kessel wird als Spitzenlasterzeuger „in den Vorlauf des Heizverteilsystems" eingebunden und hebt über einen Bivalenzmischer bei Bedarf die Temperatur; Kessel mit kleinem Wasserinhalt über hydraulische Weiche [Q7]. Die serielle Einbindung (Grundlast wärmt den Rücklauf vor, Spitzenlastkessel hebt auf den Vorlaufsollwert) gilt als robust; Nachteil ≈ 1 % mehr Brennstoff wegen höherem Kesselrücklauf [Q33]. Modellfolge (Abl.): Die WP liefert auch dann noch Teilwärme, wenn sie den Vorlaufsollwert nicht mehr erreicht (Vorwärmung bis zu ihrem Höchstvorlauf), bis ihr Rücklauf die zulässige Grenze erreicht; die Schaltung wirkt damit eher parallel als teilparallel.

## 5 Heizkurve

- Parameter: Steilheit (Neigung) = Vorlaufänderung je K Außentemperatur, Niveau = Parallelverschiebung; Faustformel Neigung = (θ_V − θ_i)/(θ_i − θ_a); typisch Heizkörper ≈ 1,4, Fußbodenheizung ≈ 0,6 [Q35]. Bei θ_i = 20 °C und θ_NA = −12 °C ergibt sich (Abl.): Auslegungsvorlauf 35 °C → 0,47; 45 °C → 0,78; 55 °C → 1,09; 70 °C → 1,56.
- Herleitung aus Auslegungspunkt und Exponent (2. Heizkörpergleichung [Q1], konstanter Massenstrom, Last linear in θ_a, Abl.): φ = (θ_i − θ_a)/(θ_i − θ_a,N); θ_V = θ_i + Δθ_m,N · φ^(1/n) + σ_N · φ/2; θ_R = θ_V − σ_N · φ. Die Krümmung folgt aus 1/n; exakt mit ΔT_ln iterativ (hier < 0,1 K Unterschied). Mit Heizgrenze: φ = (θ_HG − θ_a)/(θ_HG − θ_a,N).

| θ_a [°C] | −12 | −5 | 0 | +5 | +10 | +15 |
|---|---|---|---|---|---|---|
| φ (Bezug θ_i = 20 °C) | 1,00 | 0,78 | 0,63 | 0,47 | 0,31 | 0,16 |
| θ_V/θ_R (55/45, n = 1,3) | 55,0/45,0 | 48,7/40,9 | 44,0/37,8 | 39,1/34,4 | 33,8/30,7 | 28,0/26,4 |
| θ_V mit Heizgrenze 15 °C | 55,0 | 47,5 | 41,9 | 35,8 | 29,1 | aus |

- Heizgrenze: Deutschland HG 20/15 (DIN 4108-6, VDI 2067); gebäudeabhängig vor 1977 15–17 °C, 1977–1995 14–16 °C, nach 1995 12–15 °C, Niedrigenergiehaus 11,5–14 °C, Passivhaus 9,5–11 °C [Q34].
- Feldmessung: Die JAZ korreliert nur mit der Heizkreistemperatur; nicht die Temperatur im Normauslegungspunkt entscheidet, sondern die Heizkurve [Q17]; der Großteil der Jahresheizarbeit fällt bei mäßigen Außentemperaturen an [Q16].

## 6 Vorgabewerte für EPOS-Plan (Empfehlung)

Feldnamen nach `CLAUDE.md`, soweit vorhanden; „neu" = noch kein Feld.

| Größe | Vorgabe | Bereich | Quelle / Begründung | Feld |
|---|---|---|---|---|
| n Plattenheizkörper | 1,30 | 1,20–1,35 | [Q1], [Q2], [Q3] | `Uebergabe_Exponent` |
| n Glieder-/Röhrenradiator | 1,30 | 1,25–1,35 | [Q1], [Q3] | `Uebergabe_Exponent` |
| n Konvektor | 1,40 | 1,25–1,50 | [Q1], [Q2] | `Uebergabe_Exponent` |
| n Gebläsekonvektor (feste Stufe) | 1,10 | 1,0–1,2 | [Q2], Abl. | `Uebergabe_Exponent` |
| n Fußboden-/Flächenheizung | 1,10 | 1,0–1,1 | [Q1], [Q3], [Q5] | `Uebergabe_Exponent` |
| Auslegungsspreizung Fläche | 5 K | 3–7 K | [Q5], [Q8] | `Auslegung_Vorlauf`/`_Ruecklauf` |
| Auslegungsspreizung Heizkörper (WP) | 7 K | 5–10 K | [Q8] | dto. |
| Bestandsauslegung Heizkörper | 70/55 °C | 90/70 … 55/45 °C | [Q1] | dto. |
| Spreizung Verflüssiger, Auslegung | 5 K | 3–8 K | [Q3], [Q8], [Q11] | neu |
| Höchstspreizung WP | 10 K | 8–10 K (R744 ≥ 20 K) | [Q8], EN 14511 55/65; R744: [Q18], [Q20] (Abl.) | neu |
| Mindestspreizung WP | 3 K | 2–5 K | Tabelle [Q8], Abl. (Pumpenaufwand) | neu |
| Mindestvolumenstrom / Auslegungsvolumenstrom | 60 % | 40–100 %; Herstellerwert hat Vorrang | Abl. σ_N/σ_max; [Q6], [Q8] | neu |
| Mindestanlagenvolumen, direkt | 5 l/kW | 3–5 l/kW | [Q8], [Q37] | neu |
| Puffer Laufzeit/Abtauung | 20 l/kW | 20–25 l/kW | [Q8], [Q37] | neu |
| Höchstvorlauf R410A/R32 (L/W, Bestand) | 55 °C – **bestätigt** | 55–60 °C (R32 bis 65 °C) | [Q16], [Q9], [Q22], [Q32] | `Vorlauf_Max` |
| Höchstvorlauf R290 | 70 °C – **bestätigt** | 65–75 °C | [Q16] | `Vorlauf_Max` |
| Höchstvorlauf R744 | 80 °C **und** Rücklauf ≤ 35 °C – **korrigiert** | VL 65–90 °C, RL 25–35 °C | [Q18], [Q19], [Q20] | `Vorlauf_Max`, Rücklaufgrenze neu |
| Höchstvorlauf R1234ze(E), Großanlage | 80 °C | 70–85 (90) °C | [Q19], [Q21] | `Vorlauf_Max` |
| COP-Änderung je K Vorlauf (ohne Kennfeld) | −2,0 %/K | −1,5 … −2,5 %/K | [Q13] Abl., [Q3] | neu |
| Heizleistung je K Vorlauf | 0 %/K geregelt, −0,3 %/K fest | 0 … −0,5 %/K | [Q13] Abl. | neu |
| Bivalenzpunkt parallel/monoenergetisch (L/W) | −5 °C | −10 … +2 °C | [Q9], [Q23] | neu |
| Abschaltpunkt teilparallel/alternativ | aus Heizkurve und `Vorlauf_Max` rechnen; ersatzweise −3 °C | −5 … +5 °C | [Q7], [Q23] | neu |
| Leistungsanteil WP an Norm-Heizlast (monoenergetisch) | 65 % | 50–80 % (bei −7 °C: 70–90 %) | [Q9] | Prüfregel |
| Hybrid-Mindestanteil (GModG/BEG) | 30 % parallel, 40 % alternativ | fest | [Q27], [Q32] | Prüfregel |
| Heizgrenze | 15 °C | 10–17 °C | [Q34] | Heizkurve |
| Norm-Außentemperatur | nach PLZ | −18,8 … −4,2 °C | DIN/TS 12831-1, [Q9] | Klima |

Nicht am Primärtext geprüft: Paar 55/65 °C (EN 14511-2), T_biv ≤ +2 °C (EN 14825), VDI-4645-Werte, BEG-Mindest-JAZ 3,0, Bandbreite R32 — vor Übernahme in Hilfetexte am Norm- bzw. Richtlinientext gegenlesen.

## Quellen (alle abgerufen am 07.10.2026)

- [Q1] Wolff, D.; Jagnow, K.: Heizflächenauslegung bei Heizkörperheizungen, Überarbeitung Recknagel/Sprenger/Schramek (Ausgabe 2007). http://www.bosy-online.de/hydraulischer_abgleich/heizflaechenauslegung_recknagel.pdf
- [Q2] Paschotta, R.: Heizkörperexponent. RP-Energie-Lexikon, Stand 04.05.2025. https://www.energie-lexikon.info/heizkoerperexponent.html
- [Q3] Dott, R. u. a.: Wärmepumpen – Planung, Optimierung, Betrieb, Wartung. 5. Aufl., BFE/EnergieSchweiz, Faktor Verlag, Zürich 2018. https://www.fws.ch/wp-content/uploads/2018/12/Buch_WP_Web_2018.pdf
- [Q4] Gritzki, R. u. a.: Can we still trust in EN 442? Part 2. REHVA Journal 04/2021. https://www.rehva.eu/rehva-journal/chapter/can-we-still-trust-in-en-442-new-operating-definitions-for-radiators-part-2-model-based-analysis-and-results
- [Q5] Danfoss: Handbuch Projektierung von Fußbodenheizungen – Planungsgrundlagen DIN-Normen (DIN EN 1264), 12/2008. https://assets.danfoss.com/documents/latest/100638/AG000086464394de-000101.pdf
- [Q6] BDH: Informationsblatt Nr. 37 Teil 1 – Hydraulische Wärmeübergabesysteme mit Wärmepumpe, Stand 10/2025. https://www.bdh-industrie.de/fileadmin/user_upload/Downloads/Infoblaetter/Infoblatt_Nr_37-1_Juli_2025_Hydraulische_Waermeuebergabe_mit_Waermepumpe-Grundlagen.pdf
- [Q7] BDH: Informationsblatt Nr. 57 – Bivalente Wärmepumpen-Systeme, März 2019. https://www.bdh-industrie.de/fileadmin/user_upload/Downloads/Infoblaetter/Infoblatt_Nr_57_Maerz_2019_Bivalente_Waermepumpensysteme.pdf
- [Q8] BWP: Leitfaden Hydraulik, Stand Juli 2016. https://www.waermepumpe.de/uploads/media/BWP_LF_Hydraulik_final_web.pdf
- [Q9] BWP: Leitfaden Wärmepumpendimensionierung, Impressum „Stand 09-2026". https://www.waermepumpe.de/fileadmin/user_upload/waermepumpe/07_Publikationen/BWP_LF_WPDimensionierung.pdf
- [Q10] Klein, B.: The basis for comparison – The determination of performance rates for heat pumps. REHVA Journal, Oktober 2012, S. 15–18. https://www.rehva.eu/fileadmin/hvac-dictio/05-2012/p15-18_klein.pdf
- [Q11] EHPA: Testing Regulation – Testing of Air/Water Heat Pumps, Version 2.4a, 07.06.2021. https://www.ehpa.org/wp-content/uploads/2022/07/EHPA_TestReg_AW_HP_V2.4a_20210607_.pdf
- [Q12] Eschmann, M. (NTB Buchs): Qualitätsüberwachung von Kleinwärmepumpen 2018, BFE, 06.12.2018. https://pubdb.bfe.admin.ch/de/publication/download/10077
- [Q13] WPZ Buchs (OST): Prüfresultate Luft/Wasser-Wärmepumpen nach EN 14511 und EN 14825, Stand 29.05.2026; eigene Medianauswertung. https://www.ost.ch/fileadmin/dateiliste/3_forschung_dienstleistung/institute/ies/wpz/luft-wasser-waermepumpen/pruefresultate_lw_29052026.pdf
- [Q14] EN 14511-2:2022, Leseprobe (SIST EN 14511-2:2022). https://cdn.standards.iteh.ai/samples/70159/6e04504dca5f4a59a4b19731abd322bf/SIST-EN-14511-2-2022.pdf
- [Q15] Rasmussen, P. (Danish Technological Institute): Calculation of SCOP for heat pumps according to EN 14825, 31.12.2011. https://www.chiltrix.com/documents/App-B-Teknical-SCOP-NEF%20-EN14825.pdf
- [Q16] Miara, M. (Fraunhofer ISE): Are heat pumps able to deliver sufficiently high temperatures in the heating circuit? innovation4e, 17.02.2021. https://blog.innovation4e.de/en/2021/02/17/are-heat-pumps-able-to-deliver-sufficiently-high-temperatures-in-the-heating-circuit/
- [Q17] Günther, D. u. a. (Fraunhofer ISE): Effizienzanalyse von Wärmepumpen im EFH-Bestand (WPsmart im Bestand), Chillventa eSpecial 15.09.2020. https://www.ise.fraunhofer.de/content/dam/ise/de/documents/publications/conference-paper/091_Fraunhofer-ISE_Chillventa-2020_1015_EFFIZIENZANALYSE%20VON%20W%C3%84RMEPUMPEN%20IM%20BESTAND.pdf
- [Q18] UBA: Hauswärmepumpen mit natürlichen Kältemitteln – Zwischenbericht, Texte 82/2022 (Tab. 20, Abschn. 3.1.3). https://www.umweltbundesamt.de/system/files/medien/479/publikationen/texte_82-2022_hauswaermepumpen_mit_natuerlichen_kaeltemitteln.pdf
- [Q19] EnergieSchweiz/BFE: Kältemittel-Fibel Klimakälte, Ausgabe 09/2025. https://pubdb.bfe.admin.ch/de/publication/download/8710
- [Q20] Keller, L.: Großwärmepumpen mit R744. KKA 03/2024. https://www.kka-online.info/artikel/grosswaermepumpen-mit-r744-4108042.html
- [Q21] CoolProp: R1234ze(E) – Fluid Properties. https://coolprop.org/fluid_properties/fluids/R1234ze(E).html
- [Q22] SI-Informationen: VDI 4650 – Aktualisierte Berechnung der JAZ (VDI 4650 Blatt 1:2024-02), 14.03.2024. https://www.si-shk.de/vdi-4650-aktualisierte-berechnung-der-jaz-212649/
- [Q23] energie-experten.org: Bivalenzpunkt (Tabelle nach DIN V 4701-10/VDI 4650), Stand 01.07.2026 (sek.). https://www.energie-experten.org/heizung/waermepumpe/planung/bivalenzpunkt
- [Q24] Hönig, C. (WPsoft): Wärmepumpen-Förderung – Wie man die Jahresarbeitszahl erhöht. TGA Fachplaner 11/2009. https://www.wp-opt.de/tga-2009-11.pdf
- [Q25] DIN Media: E DIN EN 15450:2026-02, Planung von Heizungsanlagen mit Wärmepumpen. https://www.dinmedia.de/de/norm-entwurf/din-en-15450/398823989
- [Q26] dena: Praxisleitfaden für Wärmepumpen in Mehrfamilienhäusern, 03/2024. https://www.gebaeudeforum.de/fileadmin/gebaeudeforum/Downloads/Leitfaden-Handbuch/Leitfaden_Waermepumpen-in-Mehrfamilienhaeusern.pdf
- [Q27] § 43 GModG. https://www.gesetze-im-internet.de/geg/__43.html
- [Q28] WERK.E: Bio-Treppe Alternativen nach § 43 GModG, 08/2026 (sek.). https://werk-e.de/bio-treppe-alternativen/ · ENERGIE-FACHBERATER: GModG – diese Heizungen sind erlaubt, 29.07.2026 (sek.). https://www.energie-fachberater.de/beratung-foerdermittel/gesetzliche-vorgaben/gebaeudemodernisierungsgesetz-gmodg/gmodg-diese-heizungen-sind-erlaubt.php
- [Q29] Ingenieurkammer-Bau: Gebäudemodernisierungsgesetz im Bundesgesetzblatt (BGBl. 2026 I Nr. 226). https://ingkh.de/ingkh/aktuelles/news/GModG-Bundesgesetzblatt.php
- [Q30] buzer.de: § 71h GEG (aufgehoben zum 29.07.2026; Fassung 2024). https://www.buzer.de/71h_GEG.htm
- [Q31] KfW: BEG – Infoblatt zu den förderfähigen Maßnahmen und Leistungen (Sanieren), Version 11.0, in Kraft 16.09.2026. https://www.kfw.de/PDF/Download-Center/F%C3%B6rderprogramme-(Inlandsf%C3%B6rderung)/PDF-Dokumente/6000004863_Infoblatt_BEG_F%C3%B6rderf%C3%A4hige_Ma%C3%9Fnahmen.pdf
- [Q32] KfW: BEG – Liste der technischen FAQ, Einzelmaßnahmen, Version 7.0, 01.06.2026 (TFAQ 8.14, 8.18). https://www.kfw.de/PDF/Download-Center/F%C3%B6rderprogramme-(Inlandsf%C3%B6rderung)/PDF-Dokumente/6000004864_BEG_TFAQ_EM.pdf
- [Q33] Lotz, D.: Parallele oder serielle Einbindung? IKZ-Haustechnik, 22.12.2016. https://www.ikz.de/detail/news/detail/parallele-oder-serielle-einbindung/
- [Q34] Wikipedia: Heizgrenze. https://de.wikipedia.org/wiki/Heizgrenze
- [Q35] BBS Oldenburg (Lernmaterial Anlagenmechaniker): Heizkurve. https://bbs-old.de/anlagenmechaniker/online/heizkurve2/index.html
- [Q36] baulinks.de: Richtlinie VDI 4645 E – Heizungsanlagen mit elektrisch betriebenen Wärmepumpen, 2026. https://www.baulinks.de/webplugin/2026/0661.php4
- [Q37] energie-experten.org: Pufferspeicher für Wärmepumpen (Werte nach VDI 4645), Stand 01.07.2026 (sek.). https://www.energie-experten.org/heizung/waermepumpe/waermepumpenheizung/pufferspeicher
- [Q38] effiziente-waermepumpe.ch: Temperaturspreizung und COP (WPZ-Untersuchung), 30.11.2009 (sek.). https://www.effiziente-waermepumpe.ch/2009/11/30/temperaturspreizung-und-cop/
- [Q39] Viessmann: Planungsanleitung Grundlagen für Wärmepumpen, 5/2015 (nur herstellerübergreifende Aussagen). https://www.meinhausshop.de/media/pdf/meinhausshop-grundlagen-waermepumpen-de.pdf
- [Q40] autarc: Welche Wärmepumpen bekommen ab 2026 noch Förderung? 05.01.2026 (sek.). https://www.autarc.energy/wissen/schallanforderungen-warmepumpe-forderung
- [Q41] GÖRG: Gebäudemodernisierungsgesetz (GModG) schafft GEG ab, 17.07.2026. https://www.goerg.de/de/aktuelles/veroeffentlichungen/17-07-2026/gebaeudemodernisierungsgesetz-gmodg-schafft-geg-ab-was-sich-aendert-und-was-bleibt
