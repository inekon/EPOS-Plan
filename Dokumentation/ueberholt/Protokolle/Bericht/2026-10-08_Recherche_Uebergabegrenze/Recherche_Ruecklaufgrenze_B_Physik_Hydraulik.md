# Recherche B: Rücklaufgrenze der Wärmepumpe – Physik und Hydraulik

Stand 08.10.2026 · Ergänzung zu `Recherche_WP_DeltaT_Bivalenz.md` (dort [Q1]–[Q41]; hier neu ab [Q60]) · Prüfgegenstand: Konzept „Übergabegrenze und Bivalenz der Wärmepumpe" · Kennzeichen: **[Qn]** Quelle, **(sek.)** Sekundärquelle, Abstract oder Zusammenfassung, **(Abl.)** eigene Rechnung mit den angegebenen Formeln, **(n. e.)** Volltext nicht eingesehen. Herstellernamen stehen nur in dieser Arbeitsdatei; die Vorgaben in Abschnitt 8 sind herstellerneutral begründet.

## 0 Kernaussagen

- Eine **eigenständige physikalische** Rücklaufgrenze hat nur der transkritische Kreis (R744). Der Gaskühler-Wassereintritt bestimmt die CO₂-Temperatur vor der Drossel und damit Kälteleistung, Drosselverlust und optimalen Hochdruck. Gemessen: −1,8 %/K COP zwischen 28 und 35 °C Rücklauf [Q60]; Fachaufsatz: −2 bis −3 %/K, Betrieb unter 35 °C [Q61] (sek.), [Q19].
- R744-Geräte schalten über 35 °C nicht ab: Herstellergrenzen am Eintritt liegen bei 50 °C (Ladeende) bis 63 °C [Q62], [Q64]; Leistung und COP fallen ab ≈ 29 °C Eintritt [Q64]. Für die Stundenrechnung ist **stetige Abwertung bis zu einer harten Grenze** das bessere Modell; am genauesten ist ein R744-Kennfeld über dem Wassereintritt statt über dem Vorlauf.
- **Unterkritische** Kreisläufe (R290, R32, R410A, R134a, HFO, Gemische) haben keine Rücklaufgrenze über „Vorlauf_Max − Mindestspreizung" hinaus. Die Verdichtergrenze liegt an der Verflüssigungstemperatur, also am Vorlauf [Q72]; der Rücklauf wirkt nur über die Unterkühlung: +0,3 … +1 %/K COP je K tieferem Rücklauf bei festem Vorlauf (Abl.).
- „Rücklauf max." rücklaufgeregelter Regler ist eine **Regelungsgrenze**: einzustellen als berechnete maximale Vorlauftemperatur abzüglich Spreizung, Bereich 25–70 °C [Q66], [Q67].
- **Gas-Absorptions-WP (NH₃/H₂O)** nennen eine Höchsteintrittstemperatur: 55 °C Heizen / 60 °C TWW bei 65/70 °C Höchstvorlauf, also Vorlauf_Max − 10 K [Q73].
- **Mindestrücklauf** (Kaltstart, Abtauung): 15 °C, Kaltstart endet bei 23 °C [Q67]; im beheizten Gebäude ohne Bedeutung, Prüfhinweis nur bei Sollwerten unter ≈ 15 °C.
- Die **Mischungsbilanz** des Konzepts ist richtig (energieerhaltend, stetig am Umschaltpunkt). Puffermischung verschiebt bei unterkritischen WP den Bivalenzpunkt um ≈ 1 K (Volumenstromverhältnis 1,2) bis ≈ 3 K (1,5); bei R744 hebt ein Überschussstrom der WP den Rücklauf und verschiebt die Abschaltgrenze um ≈ 2 K (Abl., Abschn. 7.3).

## 1 R744 (transkritisch)

### 1.1 Warum der Rücklauf maßgeblich ist

- CO₂ hat T_krit = 31 °C [Q18]. Über dem kritischen Druck gibt der Kreis Wärme ohne Kondensation ab; die CO₂-Temperatur gleitet im Gaskühler. Vor der Drossel liegt der Zustand (p_hoch, T_GK,aus). Im Gegenstrom gilt `T_GK,aus ≈ θ_R + ΔT_A` mit der Grädigkeit am kalten Ende („temperature approach" [Q60]).
- Nahe der pseudokritischen Temperatur (bei 80–100 bar etwa 35–45 °C, Fachwissen) ist die Wärmekapazität des CO₂ sehr groß. Wenige Kelvin höhere T_GK,aus heben die Enthalpie vor der Drossel stark: kleinere Enthalpiedifferenz im Verdampfer, größerer Drosselverlust, höherer optimaler Hochdruck, mehr Verdichterarbeit. Der Rücklauf ist deshalb die Führungsgröße des Hochdrucks [Q61] (sek.). Bei 35 °C Rücklauf und 3 K Grädigkeit liegt T_GK,aus ≈ 38 °C mitten in diesem Bereich (Abl.).
- Der Vorlauf (Gaskühler-Wasseraustritt) wirkt schwach: 35/55 → 35/85 °C senkt den COP nur von 2,80 auf 2,74 [Q20], also ≈ −0,07 %/K (Abl.).
- Interne Pinch-Punkte bei Aufheizung 25 → 70 °C: ≈ 0/3/7 K bei 85/90/95 bar [Q61] (sek.). Ein tiefer Rücklauf erlaubt einen tieferen Hochdruck ohne Pinch.

### 1.2 Zahlen

| Beleg | Bedingung | Wirkung des Rücklaufs |
|---|---|---|
| Messung Prototyp Sole/Wasser [Q60] | nur Raumheizung, t₀ = −5 °C, 33/28 → 35/30 → 40/35 °C | COP 3,15 → −4 % → −12 %, ≈ −1,8 %/K |
| dto., TWW-Vorwärmgaskühler [Q60] | TWW 60 bzw. 80 °C | −1,1 bzw. −0,8 %/K je K Eintritt |
| Bestandsstudie (Brandes/Kruse 2000 nach [Q60], sek.) | Heizkörper 70/50 → 93/40 °C (Volumenstrom −65 %) | JAZ +15 % trotz 23 K höherem Vorlauf; JAZ bei 70/50 ≈ 2,8, 10–25 % unter damaligen L/W-WP |
| Herstellerdaten TWW-Großgerät [Q63] | A7, Austritt 65 °C, Eintritt 9 → 15 °C | COP 3,65 → 3,44, ≈ −1,0 %/K (Abl.) |
| Herstellerangaben [Q64] | Eintritt über 29 °C | Leistung und COP sinken; zulässig 5–63 °C (sek.) |
| Fachaufsatz 2026 [Q61] (sek.) | Wärmepumpen und Wärmerückgewinnung | −2 bis −3 %/K; Betrieb unter 35 °C; über 37 °C ist zweistufiges NH₃ effizienter |
| Kältemittel-Fibel [Q19] | Faustregel | Rücklauf unter 35 °C |
| Studien China [Q65] (n. e.) | Raumheizung, tiefe Außentemperatur | ab 25 °C starker Abfall, über 40 °C COP unter 1,5 |

Die Empfindlichkeit wächst mit dem Rücklauf: ≈ 1 %/K bei 10–15 °C [Q63], ≈ 1,8 %/K bei 28–35 °C [Q60], 2–3 %/K um 30–40 °C [Q61], weil T_GK,aus in den pseudokritischen Bereich rückt (Abl. aus den Belegen).

Relativer COP gegenüber 30 °C Rücklauf (Abl., linear mit 1,8 bzw. 3 %/K):

| Rücklauf | 30 °C | 35 °C | 40 °C | 45 °C | 50 °C |
|---|---|---|---|---|---|
| COP / COP(30 °C) | 1 | 0,85–0,91 | 0,70–0,82 | 0,55–0,73 | 0,40–0,64 |

Heizleistung: sinkt ab ≈ 29 °C Eintritt [Q64]; eine Zahl je K fand sich in keiner eingesehenen Primärquelle. Eine Übersichtsarbeit nennt für 40 → 80 °C Gaskühler-Austrittstemperatur COP 5,2 → 1,8 und Leistung 27,9 → 11,9 kW ([Q65], n. e.), linear ≈ −1,4 %/K Leistung (Abl.).

### 1.3 Herstellergrenzen

- TWW-Gerät mit Schichtspeicher: startet unter 45 °C Speichertemperatur und lädt, bis der Wassereintritt 50 °C erreicht. Das ist Ladeende, keine Störgrenze [Q62]. Raumheizung nur begrenzt: mit TWW-Mindestbedarf, unter 8000 BTU/h (≈ 2,3 kW), Heizungsrücklauf in den Speicherboden [Q62].
- TWW-Großgerät: Eintritt 5–63 °C, Austritt 55–90 °C; volle Leistung und COP nur bis ≈ 29 °C Eintritt [Q64]; Kennwerte bei 9–17 °C Eintritt [Q63].
- Die harte Herstellergrenze liegt also deutlich über 35 °C; 35 °C ist eine **Effizienz- und Planungsgrenze**.

### 1.4 Modell für die Stunde

Drei Stufen (Abl.):

- **A – Stufe des Konzepts:** Φ = 0 für θ_R > Ruecklauf_Max = 35 °C, sonst Kennfeld. Konservativ bei bivalenten Anlagen (der Kessel übernimmt früher); überschätzt den COP zwischen Bezugsrücklauf des Kennfelds und 35 °C um bis 10–15 %. Bei monovalenter R744-Anlage entsteht über 35 °C ein Komfortdefizit, das die reale Anlage nicht hat. Vertretbar als benannte Näherung, wenn das Kennfeld bei ≈ 30 °C Rücklauf gilt.
- **B – stetige Abwertung bis zur harten Grenze** (empfohlen, solange kein Rücklauf-Kennfeld vorliegt):
  ```
  f_R = 1 − k_R · max(0; θ_R − θ_R,Bezug)      k_R = 0,025 1/K  (0,018 … 0,03)
  g_R = 1 − k_Φ · max(0; θ_R − θ_R,Bezug)      k_Φ ≈ 0,01 1/K   (schwach belegt)
  COP = COP_KF(θ_Q; θ_V) · f_R ,   Φ_max = Φ_KF(θ_Q; θ_V) · g_R ,   Φ = 0 für θ_R > Ruecklauf_Max
  ```
  θ_R,Bezug = Rücklauf, bei dem die Kennfelddaten gemessen sind (bei R744 Pflichtangabe; Vorgabe 30 °C). Harte Grenze dann 40 °C oder Herstellerwert.
- **C – Kennfeld über dem Wassereintritt** (am genauesten): Für R744 trägt das Kennfeld Leistung und COP über θ_R statt über θ_V. Der Vorlauf wirkt nur als Einsatzgrenze (bis 90 °C) und schwach (≈ −0,1 %/K, Abl. aus [Q20]). Die Grenze ist der Kennfeldrand — dieselbe Logik wie Vorlauf_Max bei unterkritischen Geräten.

Warnung: TWW-Datenblätter geben R744-Kennwerte bei 9–17 °C Eintritt an [Q63]. In der Raumheizung mit 30 °C Rücklauf überschätzen sie den COP um ≈ 15–30 % (Abl., 1–2 %/K über 15–20 K). Ohne Bezugsrücklauf ist ein R744-Kennfeld nicht verwendbar.

### 1.5 Spreizung bei R744

Der Prototyp lief mit 5 K Spreizung (33/28 °C) bei COP 3,15 [Q60]: Eine Mindestspreizung ist physikalisch nicht gefordert, gefordert ist der tiefe Rücklauf. Große Spreizungen (≥ 20 K) folgen daraus, sobald der Vorlauf 55 °C oder mehr beträgt. Die Bestandsstudie gewann mit 93/40 statt 70/50 °C (53 statt 20 K) 15 % JAZ (nach [Q60], sek.).

## 2 Unterkritische Kreisläufe

### 2.1 Verflüssiger, Grädigkeit, Unterkühlung

- `θ_c ≈ θ_V + Grädigkeit` (3–5 K [Q3]). Im Gegenstrom tritt das Wasser am Unterkühlungsende ein und durchläuft Kondensation und Enthitzung. Rund 75–80 % der Wärme fallen bei θ_c an (Abl., gerundete Stoffwerte R290/R410A bei t_c = 40 °C); der Pinch liegt am Siede- oder Taupunkt. Daher folgt θ_c dem Vorlauf, und der Rücklauf begrenzt nur die erreichbare Unterkühlung (Flüssigkeit nicht kälter als θ_R + Pinch).
- Wirkung je K Unterkühlung (Abl., t₀ = −5 °C, t_c = 40 °C, gerundete Stoffwerte): `Δq₀/q₀ ≈ c_p,L / (h''(t₀) − h'(t_c))` → R290 ≈ 2,85/262 ≈ 1,1 %/K, R410A ≈ 1,8/152 ≈ 1,2 %/K Kälteleistung; Heizleistung und COP ≈ 0,8–1 %/K. Das ist die ideale Obergrenze, wenn die zusätzliche Unterkühlung tatsächlich genutzt wird.
- Serienregelungen halten 2–5 K Unterkühlung (Bezugspunkt eines R290-Verdichters: 4 K [Q72]). Bei festem Vorlauf senkt ein tieferer Rücklauf den Taupunkt-Pinch um ≈ 0,2 K je K (Enthitzungsanteil) und damit den Verflüssigungsdruck: +0,3 … +1 %/K COP je K tieferem Rücklauf (Abl.). Gegenprobe: R407C am günstigsten bei 10 K, Einbußen unter 5 K (sek. [Q38]).
- Mit gezielter Unterkühlung (Unterkühler, Regelung auf optimale Unterkühlung): COP +7 bis +30 % bei 10 bis 50 K Wassererwärmung; R290 10 → 60 °C: optimale Unterkühlung 42 K, COP 5,35, ≈ +25 % gegenüber ohne [Q68] (sek.). Das betrifft TWW-Geräte mit großer Spreizung, nicht Heizkreise mit 5–10 K.
- Ergebnis: Ein hoher Rücklauf kostet unterkritisch wenig Effizienz; er begrenzt erst, wenn `θ_V = θ_R + σ` die Vorlaufgrenze erreicht.

### 2.2 Verdichter-Einsatzgrenze

- Hüllkurven stehen in Verdampfungs- und Verflüssigungstemperatur (R290-Scroll: Achsen t_c 15–75 °C und t₀ −35 bis +15 °C, drehzahlgeregelt bis 80 °C; Bezugspunkt t₀ −7 °C, t_c 50 °C, Überhitzung 10 K, Unterkühlung 4 K) [Q72]. Daraus folgt `θ_V,max ≈ t_c,max(t₀) − Grädigkeit`; der Rücklauf erscheint nur über `θ_V = θ_R + σ`.
- Die harte Grenze im Betrieb ist der Hochdruckschalter. Ein Regler nennt als Ursachen der Hochdruckabschaltung Heizwasserdurchfluss, Überströmventil und Temperatur [Q67] — also Volumenstrom und Vorlauf, nicht den Rücklauf allein.
- Unten begrenzen Mindestverflüssigungstemperatur und Druckverhältnis (Hüllkurve unten, Achse ab 15 °C [Q72]); das ist eine Anfahrgrenze (Abschnitt 3).

### 2.3 Rücklaufgeregelte Wärmepumpen

- Zwei Reglerfamilien (Dimplex [Q66], Luxtronik [Q67]) bilden die Heizkurve auf den **Rücklauf** ab: Der Regler berechnet aus Heizkurve und Außentemperatur eine Rücklaufsolltemperatur [Q66]; beim anderen beziehen sich die Temperaturwerte auf den Rücklauf, für Vorlaufwerte ist die Spreizung abzuziehen [Q67].
- Begründung des Herstellers [Q66]: lange Laufzeiten mit Erwärmung des ganzen umgewälzten Volumens, Erfassung der Störgrößen, und bei konstantem Rücklauf ergibt eine kleinere Spreizung einen tieferen Vorlauf. Hintergrund (Abl.): Festdrehzahlgeräte mit festem Volumenstrom — der Vorlauf springt beim Start, der Rücklauf ist die träge Zustandsgröße der Anlage.
- Folge für „Rücklauf max.": Einzustellen ist die maximale Rücklaufsolltemperatur, die sich aus der berechneten maximalen Vorlauftemperatur abzüglich der Spreizung ergibt; obere Begrenzung 25–70 °C, untere Heizkurvengrenze 18 °C (L/W) bzw. 15 °C (S/W, W/W), Hysterese 0,5–5 K, Werk 2 K [Q66]. Das ist eine **Regelungsgrenze und zugleich abgeleitet**; physikalisch fügt sie nichts hinzu, sie kann aber enger eingestellt sein als das Kennfeld erlaubt.
- Leistungsgeregelte Geräte regeln meist den Austritt (Zielwert „leaving water temperature" [Q74]).

### 2.4 Enthitzer

- Ein Enthitzer liefert 15–20 % der Heizleistung; das Heißgas liegt 30–40 K über der Verflüssigungstemperatur, deshalb erreicht TWW 60–70 °C, während der Verflüssiger eine Niedertemperaturheizung bedient [Q60].
- Der Enthitzerkreis hat eine eigene Eintrittsgrenze unterhalb der Heißgastemperatur minus Grädigkeit; für den Verflüssiger ist er keine Rücklaufgrenze. In EPOS-Plan: benannte Näherung (nicht gerechnet) oder fester Leistungsanteil.

### 2.5 Zeotrope Gemische

- Gleit (druckabhängig): R407C ≈ 6 K, R454C 7,3–7,8 K, R455A 10,6–11,9 K; im Verflüssiger 6,1–9,9 K für die R404A-Ersatzgemische [Q69] (sek.).
- Lorenz-Prozess: Gleitende Phasenwechsel folgen gleitenden Wassertemperaturen; Bezug ist die thermodynamische Mitteltemperatur, `COP_LZ = T_m / (T_m − T_m,0)` [Q60]. Bei 40 K Temperaturänderung auf Quellen- und Senkenseite bringt das beste Gemisch bis +26 % COP gegenüber reinen Kältemitteln [Q70] (sek.); der Gewinn hängt stark vom Verdichter ab [Q70].
- Folge: weiches Effizienzoptimum bei σ ≈ Gleit (R407C: 10 K günstigst [Q38]). Bei σ < Gleit steigt der Hochdruck — Effizienzverlust, keine Abschaltung. Keine eigene Rücklaufgrenze.

### 2.6 Hochtemperatur-Wärmepumpen

- Über 20 Geräte von 13 Herstellern liefern mindestens 90 °C bei 20 kW bis 20 MW; COP 2,4–5,8 bei 95 bis 40 K Temperaturhub; Kältemittel u. a. R1233zd(E), R1336mzz(Z), R245fa, R600, R601, R718 [Q71] (sek.).
- Einsatzgrenzen werden als Senkenaustritt und Temperaturhub angegeben; der Senkeneintritt erscheint über Unterkühler oder Economiser und den Senkengleit (gerätespezifisch, hier nicht erhoben). Für EPOS-Plan: Kennfeld über dem Hub, Rücklaufgrenze abgeleitet; ein Herstellerwert „max. Senkeneintritt" wird als Feldwert übernommen.

### 2.7 Typologie

| Art | Wirkt über | Unterkritisch | R744 |
|---|---|---|---|
| harte physikalische Grenze | Hochdruck, t_c,max bzw. T_GK,aus | ja, am **Vorlauf** | ja, am **Rücklauf** |
| abgeleitete Grenze | θ_R ≤ θ_V,max(θ_Q) − σ_min,eff | ja, als stetige Leistungsbegrenzung | entfällt |
| Regelungsgrenze | Rücklaufsoll-Maximum, Estrichprogramm | rücklaufgeregelte Geräte | Gerätesteuerung (Ladeende) |
| Anfahrgrenze | Mindestrücklauf 15–18 °C | ja (v. a. L/W) | unkritisch |
| weiche Effizienzgrenze | σ < Gleit, fehlende Unterkühlung | ja, gering | — |

Die abgeleitete Grenze als stetige Leistungsbegrenzung (Abl.):

```
Φ_max(θ_R) = min[ Φ_KF(θ_Q; θ_V,max) ;  ṁ_WP · c_p · (θ_V,max − θ_R) ]
Φ = 0,  wenn  ṁ_WP · c_p · (θ_V,max − θ_R) < Φ_min      (Mindestmodulation bzw. Takten)
θ_R,grenz = θ_V,max − max( σ_min ; Φ_min / (ṁ_WP · c_p) )
```

Festdrehzahl (Φ_min = Φ_N, fester Volumenstrom): θ_R,grenz = θ_V,max − σ_N. Geregelte Geräte liefern zwischen θ_V,max − σ_N und θ_R,grenz einen abnehmenden Teil ihrer Leistung, statt an einer Stelle abzuschalten.

## 3 Mindest-Rücklauftemperatur

| Anlass | Wert | Beleg |
|---|---|---|
| Kaltstart L/W | außen unter 10 °C und Rücklauf unter 15 °C → zweiter Wärmeerzeuger, bis der Rücklauf 15 °C übersteigt; Kaltstart endet bei 23 °C | [Q67] |
| untere Heizkurvengrenze (Rücklaufsoll) | 18 °C L/W, 15 °C S/W und W/W | [Q66] |
| Abtauung, Zusatzheizer | Mindestvolumenstrom muss in allen Zuständen gesichert sein, sonst kein Betrieb | [Q74], [Q8] |
| Estrichtrocknung | Programm mit Höchstrücklauf 25–50 °C, Werk 35 °C | [Q66] |
| Verdichter | Mindestverflüssigungstemperatur bzw. Druckverhältnis (Ölrückführung) | [Q72] (Achse), Fachwissen |
| Gas-Absorption | Eintritt ≥ 30 °C im Dauerbetrieb, kurzzeitig tiefer | [Q73] |

Bedeutung für die Stundenrechnung:

- Im beheizten Gebäude liegt der Heizungsrücklauf über der Raumtemperatur; bei Sollwerten ab 18 °C wird 15 °C nicht unterschritten → vernachlässigbar.
- Wiederaufheizen nach Sperrzeit: Das Systemwasser kühlt in 2–6 h höchstens gegen Raumtemperatur; kein Kaltstart, der tiefe Rücklauf nützt der WP (Abl.).
- Zu prüfen nur bei Absenk- oder Frostschutzsollwerten unter ≈ 15 °C (Ferien, Hallen), bei Erstaufheizung und Estrich (nicht Teil der Jahresrechnung) und bei Puffern, die aus dem Kühlbetrieb kommen. Vorschlag: kein Feld, Prüfhinweis „Rücklauf unter 15 °C bei Außentemperatur unter 10 °C: Zusatzheizer deckt" (Abl.).

## 4 Gas-Absorptionswärmepumpen (NH₃/H₂O) — Randnotiz

- L/W-Gerät, Hochtemperaturausführung [Q73]: Vorlauf max. 65 °C Heizen / 70 °C TWW; Eintritt max. 55 °C Heizen / 60 °C TWW; Eintritt min. 30 °C im Dauerbetrieb; Volumenstrom nominal 2500 l/h, min. 1400 l/h (56 %), max. 4000 l/h; 41,3 kW bei A7W35 → Nennspreizung ≈ 14 K (Abl.).
- GUE: A7W35 164 %, A7W50 152 %, A7W65 124 %, A−7W50 127 %; bei 70 °C Vorlauf Wärmebelastung auf 50 % reduziert [Q73].
- Lesart: Die Eintrittsgrenze liegt genau 10 K unter dem Höchstvorlauf — eine abgeleitete Grenze mit σ_min ≈ 10 K, vom Hersteller als eigener Wert genannt. Hintergrund (Fachwissen): Absorber und Kondensator geben Wärme an das Heizwasser ab, die Absorption braucht eine kühle Lösung.

## 5 Spreizung

| Aussage | Beleg | Bewertung |
|---|---|---|
| Höchstspreizung ≈ 10 K, statische Heizflächen | Auslegung 5–7 K, höchstens 10 K [Q8]; Regler setzt bei Heizkörpern fest 10 K Zielspreizung, bei Fußbodenheizung/Gebläsekonvektor variabel [Q75]; Prüfstufe 55/65 °C [Q14] | bestätigt |
| Mindestspreizung ≈ 3 K | Spreizung einstellbar 3–10 K [Q75] (sek.); Volumenstromtabelle ab 3 K [Q8]; darunter Pumpenaufwand | bestätigt |
| R744 ≥ 20 K | TWW 9 bzw. 17 → 65 °C (48–56 K) [Q63]; Raumheizung 93/40 besser als 70/50 °C (nach [Q60]); Messbetrieb aber auch bei 5 K [Q60] | als Folge der Rücklaufgrenze bestätigt, als Mindestspreizung nicht |
| TWW ≤ 10 K (Ladung über Wärmeübertrager) | [Q8] | bestätigt; Durchlaufladung mit R744 oder R290-Unterkühler 40–55 K [Q63], [Q68] |
| Geräte mit 15 K und mehr | Gas-Absorption ≈ 14 K nominal (Abl. aus [Q73]); R744 stets; R290 mit Unterkühler im Labor 10 → 60 °C [Q68]; Hochtemperatur-WP mit Senkengleit bis 40 K [Q70] | für Serien-L/W mit R290 in der Raumheizung keine herstellerneutrale Angabe gefunden |

## 6 Mindestvolumenstrom

| Beleg | Mindestvolumenstrom | Anteil am Nennvolumenstrom |
|---|---|---|
| Monoblock, Baugrößen 5 und 7 [Q74] | 12 l/min (Abtauung, Zusatzheizer) | 84 % bzw. 60 % bei 5 K (Abl., angenommene Nennleistung 5 bzw. 7 kW) |
| Gas-Absorption [Q73] | 1400 l/h | 56 % |
| Ableitung σ_N/σ_max ([Q8], Vorrecherche) | — | 50–70 % |

Gründe:

- Durchflusswächter: unterhalb des Mindeststroms Fehler, kein Heizbetrieb [Q74].
- Abtauung und Zusatzheizer: der Mindeststrom ist gerade dann gefordert [Q74]; Abtauung durch Kreislaufumkehr entzieht dem Heizwasser Wärme, Frostgefahr am Plattenübertrager [Q8].
- Hochdruckabschaltung bei zu geringem Durchfluss [Q67].
- Regelstabilität: zu große Spreizung lässt den Vorlauf überschwingen, die WP taktet (Fachwissen).

Zusammenhang mit der Spreizung (Abl.) — die Formel aus der Fragestellung braucht eine Klarstellung:

- Bei gegebener Leistung gehört zum **kleinsten** Volumenstrom die **größte** Spreizung: `σ(ṁ_min) = Φ / (ṁ_min · c_p)`. Bedingung σ(ṁ_min) ≤ σ_max → `ṁ_min / ṁ_N = σ_N / σ_max` (Herleitung der 60 %).
- Die Mindestspreizung bindet am **größten** Volumenstrom oder bei der **kleinsten** Leistung: `Φ_min / (ṁ_WP · c_p) ≥ σ_min`.
- Erzwingt ein Überströmventil ṁ_WP = ṁ_min > ṁ_HK, gilt an der WP `σ_WP = Φ_HK / (ṁ_min · c_p)`. σ_WP ≥ σ_min verlangt `Φ_HK ≥ ṁ_min · c_p · σ_min`, sonst Takten. Beispiel: ṁ_min = 0,6 · ṁ_N bei σ_N = 5 K und σ_min = 3 K → Φ_HK ≥ 0,36 · Φ_N.

## 7 Hydraulische Einbindung

### 7.1 Mischungsbilanz (a)

- Beide Zweige sind richtig. Energiebilanz für ṁ_HK > ṁ_WP: `Φ_HK = ṁ_HK·c_p·(θ_V,HK − θ_R,HK) = ṁ_WP·c_p·(θ_V,WP − θ_R,HK) = Φ_WP`. Für ṁ_WP ≥ ṁ_HK: `Φ_WP = ṁ_WP·c_p·(θ_V,WP − θ_R,WP) = ṁ_HK·c_p·(θ_V,WP − θ_R,HK) = Φ_HK`. Bei ṁ_WP = ṁ_HK stimmen beide Zweige überein (stetig); die Vorzeichen stimmen.
- Kompakte Form beider Zweige: `σ_WP · ṁ_WP = σ_HK · ṁ_HK`.
- Für den Rechenweg (Heizkreis gegeben, WP gesucht):
  ```
  r = ṁ_HK / ṁ_WP
  r > 1:  θ_V,WP = θ_V,HK + (r − 1) · σ_HK ,   θ_R,WP = θ_R,HK
  r ≤ 1:  θ_V,WP = θ_V,HK ,                    θ_R,WP = θ_R,HK + (1 − r) · σ_HK
  ```
  r > 1 hebt den nötigen WP-Vorlauf (trifft Vorlauf_Max und σ_max); r < 1 hebt den WP-Rücklauf (trifft σ_min und bei R744 Ruecklauf_Max).
- Annahmen: ideale Weiche (vollständige Mischung des Kurzschlussstroms, keine Schichtung, kein Speicherinhalt, keine Wärmeverluste), gleiche c_p und Dichte, von den Pumpen unabhängig eingeprägte Volumenströme, stationär in der Stunde, Φ_WP = Φ_HK. Bei ΔT-geregelter WP-Pumpe gilt ṁ_WP = Φ / (c_p · σ_soll); der Zweig wechselt dann mit der Last.
- Die direkte Einbindung mit Überströmventil ist kein eigener Fall: ṁ_WP = max(ṁ_HK; ṁ_min), dann Zweig r ≤ 1 (Abl.).

### 7.2 Feldstudien und Leitfäden (b)

- Fraunhofer-ISE-Feldmessung Bestand 2006–2009 [Q76]: Hydraulik und Regelung sind auf die WP abzustimmen; eine im Sommer laufende Heizkreispumpe nach dem Puffer entlädt diesen; ein hydraulischer Abgleich ist nötig, damit die Spreizung im Heizkreis nach dem Puffer eingehalten wird. Keine Zahl zur JAZ-Wirkung des Puffers.
- EnergieSchweiz/OST, Rechenhilfe WPesti [Q77]: bildet die Einbindung über eine Temperaturüberhöhung ab; für leistungsgeregelte WP 0 K, viele Anlagen laufen ab Werk mit 5 K Hysterese; leistungsgeregelte Kleinwärmepumpen erreichen im Feld 10–15 % bessere JAZ als vorhergesagt.
- BDH [Q7]: gleiche Volumenströme anstreben; größerer Verbraucherstrom → WP fährt stets über der Heizkurve; größerer WP-Strom → Speicher dauerhaft geladen, Leistung nicht voll verfügbar. Schweizer Handbuch [Q3]: ṁ_Erzeuger > ṁ_Verbraucher. BWP [Q8]: Reihenpuffer im Rücklauf koppelt die Ströme.
- Quantifizierte JAZ-Differenzen Parallel- gegen Reihenpuffer fanden sich in den eingesehenen Primärquellen nicht ([Q76], [Q77]; die WPZ-Feldmessung und der WP-Monitor wurden nicht vollständig gesichtet). Ratgeberseiten nennen 3–5 K Vorlaufanhebung und −0,3 bis −0,5 JAZ ohne nachvollziehbare Primärquelle; ein Simulationsanbieter spricht qualitativ von COP-Verlusten „oft um mehrere zehn Prozent" [Q78] (sek.). Beides nicht übernommen.
- Größenordnung (Abl. mit 7.1): r = 1,2 bzw. 1,5 bei σ_HK = 10 K → WP-Vorlauf in Auslegung +2 bzw. +5 K → COP −3 bis −10 % in diesen Stunden (−1,5 bis −2 %/K, Vorrecherche [Q13]); über das Jahr weniger, weil σ_HK mit der Last fällt.

### 7.3 Puffernäherung und Bivalenzpunkt (c)

Beispiel (Abl.): Heizkörper n = 1,3, Raum 20 °C, Auslegung bei −12 °C, Lastanteil `f = (20 − θ_a) / 32`, konstanter Heizkreisstrom, `θ_V,HK = 20 + ΔT_m,N · f^(1/1,3) + σ_N·f/2`.

- Unterkritisch, Auslegung 65/55 °C, Vorlauf_Max 55 °C: ohne Mischung Grenze bei f ≈ 0,73 → θ_a ≈ −3,3 °C. Mit r = 1,2 bei ≈ −2,2 °C (+1,1 K), mit r = 1,5 bei ≈ −0,6 °C (+2,7 K).
- Wirkung auf den WP-Anteil nach DIN V 4701-10 / VDI 4650 ([Q23], linear interpoliert): r = 1,2 → parallel −1, alternativ −5 Prozentpunkte; r = 1,5 → parallel −5, alternativ −17 Prozentpunkte.
- R744, Auslegung 55/45 °C, Ruecklauf_Max 35 °C: Grenze ohne Mischung bei f ≈ 0,50 → θ_a ≈ +4 °C. Mit 1,43-fachem WP-Strom (σ_WP,N = 7 K, r = 0,7) steigt der WP-Rücklauf um 3·f K; Grenze bei ≈ +6 °C (+1,9 K). WP-Anteil alternativ von ≈ 28 % auf unter 19 % (Tabellenende +5 °C [Q23]).
- Urteil:
  - **Unterkritisch:** vertretbar, wenn r ≤ 1 (Auslegungsregel [Q3], [Q7]) oder der Puffer geschichtet gerechnet wird — der Pufferkopf bleibt ≈ θ_V,HK, der Bivalenzpunkt unverändert. Bei r > 1 und schlechter Schichtung unterschätzt die Näherung den WP-Vorlauf um bis zu (r − 1)·σ_HK; die Weichenbilanz liefert die Schranke (vollständige Mischung). Spürbar wird das vor allem im alternativen und teilparallelen Betrieb.
  - **R744:** nur vertretbar, wenn der WP-Rücklauf aus der untersten Pufferzone kommt und der Heizkreisrücklauf dort ungemischt eintritt (wie die Arbeitstemperatur des Kollektorfelds aus der untersten Zone); mit einem voll durchmischten Puffer nicht.
  - Unterkritisch mit r < 1 und kleiner Last: σ_WP = r·σ_HK·f fällt unter σ_min (bei r = 0,7 und σ_HK,N = 10 K schon bei f < 0,43); diese Stunden deckt der Puffer durch Laden und Takten — die Puffernäherung bildet das richtig ab, die Weiche ohne Puffer nicht (Abl.).

### 7.4 Reihenschaltung WP → Kessel (d)

```
WP im Systemrücklauf, ṁ_WP = ṁ_sys (oder Teilstrom):
θ_V,WP = min( θ_V,max ; θ_V,HK ; θ_R,sys + Φ_WP / (ṁ_WP · c_p) )
Φ_WP   = min( Φ_KF(θ_Q; θ_V,WP) ; ṁ_WP · c_p · (θ_V,max − θ_R,sys) ) ;  Φ_WP = 0, wenn < Φ_min
Betrieb nur, wenn θ_R,sys ≤ θ_V,max − σ_min,eff ;  R744 zusätzlich θ_R,sys ≤ Ruecklauf_Max
Kessel:  Φ_K = ṁ_sys · c_p · (θ_V,HK − θ_V,WP)
```

- Vorteil: Die WP arbeitet am tieferen Zwischenvorlauf statt am Systemvorlauf und liefert bis zur abgeleiteten Grenze stetig weniger (Abl.).
- Bei großem Systemstrom (Kesselauslegung 15–20 K) ist σ_WP = Φ_WP / (ṁ_sys · c_p) oft kleiner als σ_min → Teilstrom oder Bypass um die WP (Abl.).
- R744 in Reihe vor Heizkörpern: Die Abschaltung folgt der Rücklaufkurve, nicht der Vorlaufkurve; 55/45-°C-Heizkörper erreichen 35 °C Rücklauf schon bei ≈ +4 °C außen (Abl. 7.3).
- Nebenwirkung: Der angehobene Kesselrücklauf mindert die Brennwertnutzung; das gehört in die Kesselkennlinie am Rücklauf.

## 8 Antwort auf die Anwenderfrage

**Ja — aber nicht wie bei CO₂.** Unter den elektrischen Kompressions-WP hat nur der transkritische R744-Kreis eine eigenständige physikalische Rücklaufgrenze. Alle anderen haben eine **abgeleitete** Grenze (Vorlaufgrenze minus Mindestspreizung, besser als stetige Leistungsbegrenzung), rücklaufgeregelte Geräte zusätzlich eine **Regelungsgrenze**, Gas-Absorptions-WP eine **Herstellergrenze** am Eintritt; nach unten gibt es eine **Anfahrgrenze**.

| Gruppe | Art der Rücklaufgrenze | Vorgabe `Ruecklauf_Max` | Regel bei leerem Feld |
|---|---|---|---|
| R744 transkritisch, Raumheizung | harte physikalische Grenze (Gaskühleraustritt), davor stetige COP-Abwertung 2–3 %/K | 35 °C (Stufe A); 40 °C bei Rücklaufkorrektur (Stufe B) | 35 °C; nie aus Vorlauf_Max ableiten; Bezugsrücklauf des Kennfelds Pflicht (Vorgabe 30 °C) |
| R744, TWW-Ladung aus Schichtspeicher | dieselbe, im Speicherbetrieb als Ladeende | Herstellerwert | 35 °C; Ladeende, wenn der Speicherboden Ruecklauf_Max erreicht |
| unterkritisch, reine Kältemittel und Gemische mit kleinem Gleit (R290, R32, R410A, R134a, R1234ze(E), R600a) | abgeleitet: θ_V,max(θ_Q) − σ_min,eff | leer | `θ_R,grenz = Vorlauf_Max(θ_Q) − max(σ_min; Φ_min/(ṁ_WP·c_p))`, σ_min = 3 K; Leistung ≤ ṁ_WP·c_p·(Vorlauf_Max − θ_R) |
| unterkritisch, rücklaufgeregelter Regler | Regelungsgrenze = Vorlauf_Max − σ_Ausl | Reglerwert, wenn bekannt | wie Zeile davor; ein eingetragener Wert wirkt als min(Feld; abgeleitet) |
| zeotrope Gemische mit großem Gleit (R407C, R454C, R455A) | abgeleitet, dazu weiche Effizienzgrenze σ ≈ Gleit | leer | wie unterkritisch; Hinweis σ_Ausl ≥ Gleit |
| Hochtemperatur-WP (R1233zd(E), R1336mzz(Z), R600/R601, R245fa) | abgeleitet; Hubgrenze; ggf. Herstellergrenze Senkeneintritt | Herstellerwert, sonst leer | wie unterkritisch; Kennfeld über dem Hub |
| Enthitzer-Teilkreis | eigene Grenze unter Heißgastemperatur | — | nicht gerechnet (benannte Näherung) |
| Gas-Absorption NH₃/H₂O | Herstellergrenze am Eintritt = Vorlauf_Max − 10 K | Vorlauf_Max − 10 K | Vorlauf_Max − 10 K |
| alle elektrischen WP, untere Grenze | Anfahrgrenze: Mindestrücklauf 15 °C (Kaltstart, Abtauung) | kein Feld | Prüfhinweis nur bei Sollwerten unter 15 °C; Zusatzheizer deckt |

## 9 Prüfergebnis zur Konzeptaussage

| Element | Ergebnis | Formulierungsvorschlag |
|---|---|---|
| Kennfeld Quelle × Vorlauf, Leistung/COP über dem Vorlauf | bestätigt (unterkritisch); zu ergänzen (R744) | „Bei R744 ist die Kennfeldachse der Wassereintritt (Rücklauf). Liegt nur ein Kennfeld über dem Vorlauf vor, trägt es den Bezugsrücklauf; je K darüber sinkt der COP um 2,5 %." |
| Höchstspreizung 10 K | bestätigt; zu ergänzen | „Gilt für unterkritische Geräte; Gas-Absorption ≈ 10–15 K; R744 ohne Höchstspreizung. σ_HK > σ_max bei direkter Einbindung ist ein Auslegungsfehler und wird geprüft, nicht stündlich abgeschaltet." |
| Mindestspreizung 3 K | bestätigt; zu ergänzen | „Unterhalb von Vorlauf_Max − σ_N sinkt die lieferbare Leistung stetig auf ṁ_WP·c_p·(Vorlauf_Max − θ_R); unter Φ_min liefert die WP nichts. Bei zeotropen Gemischen soll σ_Ausl den Gleit nicht unterschreiten (Hinweis)." |
| R744 ≥ 20 K | zu berichtigen | „Für R744 gilt keine Mindestspreizung, sondern die Rücklaufgrenze. Die Spreizung folgt aus Vorlauf und Rücklauf und liegt bei Vorläufen ab 55 °C über 20 K." |
| Mindestvolumenstrom 60 % | bestätigt (Belege 56–84 %); Formel klarstellen | „Zum Mindestvolumenstrom gehört die größte Spreizung σ = Φ/(ṁ_min·c_p) ≤ σ_max. Erzwingt ein Überströmventil ṁ_min, liefert die WP mindestens ṁ_min·c_p·σ_min, sonst taktet sie." |
| Rücklaufgrenze nur für CO₂ | zu berichtigen | „Eine eigene physikalische Rücklaufgrenze hat der transkritische R744-Kreis. Gas-Absorptions-WP haben eine Höchsteintrittstemperatur (Vorgabe Vorlauf_Max − 10 K). Bei unterkritischen WP folgt die Grenze aus Vorlauf_Max und Mindestspreizung; Ruecklauf_Max bleibt leer, ein eingetragener Reglerwert rücklaufgeregelter Geräte wirkt zusätzlich." |
| Ruecklauf_Max 35 °C, darüber liefert die WP nichts | bestätigt als konservative Planungsgrenze; zu ergänzen | „Über Ruecklauf_Max liefert die R744-WP nichts (benannte Näherung: reale Geräte laufen bis 50–63 °C Eintritt mit 2–3 %/K geringerem COP). Zwischen Bezugsrücklauf und Ruecklauf_Max sinkt der COP je K um 2,5 %; mit dieser Korrektur gilt 40 °C." |
| direkt: ṁ_WP = ṁ_HK, σ_WP = σ_HK | bestätigt; zu ergänzen | „Mit Überströmventil gilt ṁ_WP = max(ṁ_HK; ṁ_min); dann rechnet die Mischungsbilanz (Zweig ṁ_WP ≥ ṁ_HK)." |
| hydraulische Weiche mit Mischungsbilanz | bestätigt (Vorzeichen, Fallunterscheidung, Stetigkeit) | Kompakte Form σ_WP·ṁ_WP = σ_HK·ṁ_HK und die Annahmen (ideale Mischung, keine Speicherung, eingeprägte Ströme) ins Konzept aufnehmen; Rechenweg nach 7.1. |
| Puffer als benannte Näherung | mit Bedingung bestätigt | „Die Näherung gilt für unterkritische WP, wenn ṁ_WP ≥ ṁ_HK oder der Puffer geschichtet rechnet. Sonst unterschätzt sie den WP-Vorlauf um bis zu (r − 1)·σ_HK; die Weichenbilanz wird als Schranke ausgewiesen. Bei R744 kommt der WP-Rücklauf aus der untersten Pufferzone." |
| Reihenschaltung WP → Kessel | zu ergänzen, falls nicht enthalten | Formeln aus 7.4: Betrieb bis θ_R,sys ≤ Vorlauf_Max − σ_min,eff, R744 zusätzlich θ_R,sys ≤ Ruecklauf_Max; Kessel hebt nach. |
| Mindestrücklauf | zu ergänzen | „Unter 15 °C Rücklauf bei Außentemperaturen unter 10 °C deckt der Zusatzheizer (Kaltstart). In beheizten Gebäuden tritt das nicht auf; Prüfhinweis bei Sollwerten unter 15 °C." |

Nicht am Primärtext geprüft: Zahlen aus [Q61] (Artikelseite), [Q65] (Suchauszüge), [Q68]–[Q71] (Abstracts), Spreizungsbereich 3–10 K aus [Q75], Hüllkurvengrenzen aus [Q72] (nur Achsen gelesen). Vor Übernahme in Hilfetexte gegenlesen.

## 10 Quellen (abgerufen am 08.10.2026)

Bestehende Quellen [Q3], [Q7], [Q8], [Q13], [Q14], [Q18], [Q19], [Q20], [Q23], [Q38] siehe `Recherche_WP_DeltaT_Bivalenz.md`.

- [Q60] Stene, J.: Residential CO2 Heat Pump System for Combined Space Heating and Hot Water Heating. Dissertation, NTNU Trondheim, Department of Energy and Process Engineering, EPT-Report 2004:6, Februar 2004. https://www.osti.gov/etdeweb/servlets/purl/20559406
- [Q61] IIAR / Natural Refrigeration Review: 2026 Tech Paper „CO2 Heat Pumps: Main Differences from Refrigeration Systems and Real-World Applications" (Rangelov, Karampour), Fachaufsatz, 2026 (sek., Artikelseite). https://naturalrefrigerationreview.com/co2-heat-pumps-main-differences-from-refrigeration-systems-and-real-world-applications/
- [Q62] Sanden: SANCO2 Heat Pump Water Heater – Technical Information, Herstellerunterlage, Oktober 2017. https://static1.squarespace.com/static/5c1a79ca96d455dcbffdc742/t/5c474cee562fa759dd733b04/1548176625850/Sanden_sanc02_technical-info_10-2017_4.pdf
- [Q63] Mitsubishi Electric: Data Book QAHV-N560YA-HPB(-BS) Hot Water Heat Pump (MEE15K075), Herstellerunterlage, o. J. https://ecoinnovation.lv/wp-content/uploads/2023/02/GAISS_U%CC%84DENS_SILTUMSU%CC%84KNIS_QAHV_DataBook.pdf
- [Q64] Mitsubishi Electric UK: High temperature CO2 heat pump increases hot water efficiency, Pressemitteilung, 10.09.2020, https://les.mitsubishielectric.co.uk/latest-news/high-temperature-co2-heat-pump-increases-hot-water-efficiency-2 ; Eintrittsbereich 5–63 °C nach Produktseite Mitsubishi Electric Australia (sek., Suchauszug), https://www.mitsubishielectric.com.au/product/qahv-n560ya-hpb-hot-water-heat-pump/
- [Q65] (n. e., nur Suchauszüge; Zuordnung der Einzelaussagen nicht eindeutig) (a) Application of transcritical CO2 heat pumps to boiler replacement in low impact refurbishment projects, Heliyon, 2024, https://pmc.ncbi.nlm.nih.gov/articles/PMC10945128/ ; (b) Modified transcritical CO2 heat pump system with new water flow configuration for residential space heating, Energy Conversion and Management, 2020/21, https://www.sciencedirect.com/science/article/abs/pii/S0196890420313145 ; (c) A comprehensive review and analysis on CO2 heat pump water heaters, Energy Conversion and Management: X, 2022, https://www.sciencedirect.com/science/article/pii/S2590174522001003 (Abruf gesperrt) ; (d) US-Patent 11,255,579, Control method of transcritical carbon dioxide composite heat pump system, https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/11255579
- [Q66] Dimplex: Wärmepumpenmanager – Bedienungsanleitung für den Installateur, FD 9709 / L23, Bestell-Nr. 452115.66.48, Herstellerunterlage. https://www.dimplex.eu/sites/g/files/emiian586/files/media_import/medias/docus/7/Dimplex_WPM_Bedienungsanleitung2_fd9709_de.pdf
- [Q67] alpha innotec: Betriebsanleitung Luxtronik 2.0/2.1 (Fachhandwerker), Herstellerunterlage, o. J. https://www.alpha-innotec.ch/fileadmin/content/downloads/Lux_Fachhandwerker_de.pdf
- [Q68] Pitarch, M. u. a.: Evaluation of optimal subcooling in subcritical heat pump systems, Int. J. Refrigeration, 2017; dies.: Experimental study of a subcritical heat pump booster for sanitary hot water production using a subcooler … (R290), Int. J. Refrigeration, 2017 (sek., Abstracts). https://www.researchgate.net/publication/315442456_Evaluation_of_optimal_subcooling_in_subcritical_heat_pump_systems ; https://www.researchgate.net/publication/308019460
- [Q69] Mitsubishi Heavy Industries: Technical Review Vol. 56 No. 4, Dezember 2019 (R454C), https://www.mhi.com/group/mth/development/paper/pdf/e564070.pdf ; Fachaufsatz „R-454C, R-459B, R-457A and R-455A as low-GWP replacements of R-404A: Experimental evaluation and optimization", Int. J. Refrigeration, 2019 (Anlage im EPA-Verfahren EPA-HQ-OAR-2021-0643), https://downloads.regulations.gov/EPA-HQ-OAR-2021-0643-0228/attachment_12.pdf (beide sek., Suchauszüge)
- [Q70] Zühlsdorf, B. u. a.: Analysis of temperature glide matching of heat pumps with zeotropic working fluid mixtures for different temperature glides, Energy, 2018, https://www.sciencedirect.com/science/article/abs/pii/S0360544218306522 ; Roskosch, D. u. a.: Beyond Temperature Glide: The Compressor is Key to Realizing Benefits of Zeotropic Mixtures in Heat Pumps, Energy Technology, 2021, https://onlinelibrary.wiley.com/doi/full/10.1002/ente.202000955 (sek.)
- [Q71] Arpagaus, C.; Bless, F.; Uhlmann, M.; Schiffmann, J.; Bertsch, S.: High temperature heat pumps: Market overview, state of the art, research status, refrigerants, and application potentials. Energy 152 (2018) 985–1010 (sek., Abstract). https://ideas.repec.org/a/eee/energy/v152y2018icp985-1010.html
- [Q72] Copeland: Copeland scroll compressors for R290 comfort applications (DSC167), Produktbroschüre, o. J. https://media.copeland.com/c35cd3b3-ac35-40ec-9fc6-b2ab00acbba9/DSC167-Comfort-Propane-Scroll-Compressors-EN.pdf
- [Q73] Robur: Abso Pro design manual, Section B01 – Aerothermic heat pump GAHP A, Rev. B, Herstellerunterlage. https://www.robur.com/hubfs/doc/D-FSC003EN_B_22MCLSDC016_ADM_Section_B01_GAHP_A_EN.pdf
- [Q74] Daikin: Installation manual Daikin Altherma low temperature monobloc EBLQ/EDLQ05+07CAV3, 4P403578-1F, 06/2018. https://www.daikin.co.uk/content/dam/document-library/installation-manuals/heat/air-to-water-heat-pump-low-temperature/eblq-cv3/EBLQ05-07CV3-EDLQ05-07CV3_4PEN403578-1F_Installation%20Manual_English.pdf
- [Q75] Daikin: Daikin Altherma 3 R F, Installationsanleitung, Konfigurationsassistent Hauptzone (S. 26), https://www.manualslib.com/manual/1986467/Daikin-Altherma-3-R-F.html?page=26 ; Spreizungsbereich 3–10 K nach Installer reference guide ERLA/EHFZ (sek., Suchauszug), https://www.daikin.eu/content/dam/document-library/Installer-reference-guide/heat/air-to-water-heat-pump-low-temperature/EHFZ-D3V,ERLA-DV_4PEN596821-1A_Installer%20reference%20guide_English.pdf
- [Q76] Russ, C.; Miara, M.; Platt, M.; Günther, D.; Kramer, T.; Dittmer, H.; Lechner, T.; Kurz, C. (Fraunhofer ISE): Feldmessung Wärmepumpen im Gebäudebestand – Kurzfassung zum Abschlussbericht, Berichtszeitraum 01.12.2006–31.12.2009, August 2010. https://wp-monitoring.ise.fraunhofer.de/wp-im-gebaeudebestand/download/WP_im_Gebaeudebestand_Kurzfassung.pdf
- [Q77] Prinzing, M.; Berthold, M.; Uhlmann, M.; Eschmann, M.; Bertsch, S. (OST): Vorschlag zur Einbindung leistungsvariabler Wärmepumpen in WPesti, Wegleitung im Auftrag von EnergieSchweiz, 18.12.2020. https://pubdb.bfe.admin.ch/de/publication/download/10357
- [Q78] Vela Solaris: Buffer tanks in parallel or in series: How to achieve planning reliability and cost-effectiveness, Fachbeitrag, o. J. (sek., nur qualitativ). https://www.velasolaris.com/en/buffer-tanks-parallel-or-in-series/

Nicht erreichbar oder ohne verwertbaren Text: Volltext [Q65] (Captcha bzw. 403), WPZ-Feldmessung 2015–2018 (keine Pufferauswertung im eingesehenen Aufsatz), Luxtronik-Servicewert „Rückl.-Begr." (Werkseinstellung nicht am Primärtext bestätigt, daher nicht verwendet).
