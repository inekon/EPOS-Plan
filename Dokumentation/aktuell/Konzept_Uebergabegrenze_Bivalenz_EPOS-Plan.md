# Konzept — Übergabegrenze und Bivalenz der Wärmepumpe

**Auftrag des Anwenders vom 07.10.2026** („Übergabe als begrenzender Faktor der Wärmepumpe") ·
**Stand 07.10.2026 — Entwurf, nicht entschieden** · **Fassung 2, 08.10.2026: Rücklaufgrenze verallgemeinert, andere
Erzeuger; Entscheide U‑1, U‑2, U‑4 des Umsetzungskonzepts vom 08.10.2026 eingearbeitet** · Codestand `da03e5333` (Zweig `ios_migration_september`,
Schemastand 201, Referenzbasis `2026-10-07_R43_Kaelteseite_AK3K`) · Mockup
`Mockups/Waermepumpe_Bivalenz_Uebergabe.html` · Fragen **UB‑Q1 bis UB‑Q11** (Abschnitt 10).
**Umsetzung:** [`Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md`](Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md).

Ziel: Eine Wärmepumpe liefert in EPOS-Plan heute so viel, wie ihr Kennfeld an der gewählten Stützstelle hergibt —
auch in Stunden, in denen der Heizkreis einen Vorlauf über ihrem Höchstvorlauf verlangt. Das Papier legt fest, wie
**die Heizfläche beim Höchstvorlauf der Wärmepumpe** als zweite Grenze neben das Kennfeld tritt, wie daraus die
Betriebsbereiche einer bivalenten Anlage und ihre beiden Bivalenzpunkte folgen, welche Spreizungsgrenzen der
Wärmepumpe dabei gelten, mit welchen Vorgabewerten der Anwender startet und wie er sie eingibt.

---

## 1 Anlass und Ziel

**Die Überlegung des Anwenders (sinngemäß).** Heizkörper und Flächen eines Bestandsgebäudes sind für den Kessel
ausgelegt: Bei der tiefsten Außentemperatur genügt der höchste Vorlauf des Kessels (etwa 75 °C), der Massenstrom ist
eingestellt, der Rücklauf folgt der Last. Kommt an dieselbe Übergabe eine Wärmepumpe mit einem Höchstvorlauf von
etwa 55 °C, reicht die Leistung der Heizflächen bei diesem Vorlauf nicht mehr aus — gleich, wie groß die Wärmepumpe
ist. Der Rücklauf kann dabei nicht beliebig sinken, die Spreizung ist begrenzt. Zwischen einem ersten und einem
zweiten Bivalenzpunkt stützt der Kessel, darunter läuft der Kessel allein. Die Übergabe soll deshalb als
begrenzender Faktor der Wärmepumpe in die Rechnung eingehen.

**Zahlenbeispiel (nachgerechnet).** Heizkörper mit Auslegung 75/60/20 °C, Exponent n = 1,3, Nennleistung 10 kW,
konstanter Massenstrom aus dem Auslegungspunkt (W_H = 10 kW / 15 K = 0,667 kW/K, rund 574 l/h). Bei einem Vorlauf
von 55 °C stellt sich das Gleichgewicht aus Wasserseite und Heizflächengleichung ein bei

| Mittelwertbildung | Leistung | Rücklauf | Spreizung |
|---|---|---|---|
| arithmetisch (Festlegung AK1, Konzept Anlagenkopplung 3.1) | **5,68 kW** (56,8 %) | **46,48 °C** | 8,52 K |
| logarithmisch (EN 442) | 5,69 kW (56,9 %) | 46,46 °C | 8,54 K |

Die Annahme des Anwenders (≈ 5,7 kW, Rücklauf ≈ 46,5 °C) stimmt auf 0,1 K; der Unterschied der beiden Mittelwerte
liegt unter 0,02 kW. Weitere Vorläufe bei demselben Heizkörper: 50 °C → 4,68 kW (Rücklauf 42,98 °C), 60 °C →
6,71 kW (49,93 °C), 70 °C → 8,88 kW (56,68 °C). Daraus folgt der Kern dieses Papiers: **Eine Wärmepumpe mit
9 kW Kennfeldleistung bei 55 °C kann an diesem Heizkörper nie mehr als 5,68 kW abgeben, solange die Raumtemperatur
20 °C betragen soll.** Nach dem Kennfeld allein läge der Bivalenzpunkt des Beispielgebäudes (10 kW bei −12 °C) bei
−3,8 °C; die Übergabe verschiebt ihn auf **+1,8 °C** (Abschnitt 4.4).

## 2 Fachgrundlagen

Belege aus der Recherche vom 07.10.2026 (Quellen in Abschnitt 11, Nummern [Qn]). Kennzeichen: **(n. e.)** Normtext
nicht eingesehen, **(sek.)** Sekundärquelle, **(Abl.)** eigene Rechnung. **Werte mit (n. e.) oder (sek.) werden vor
der Übernahme in Hilfetexte und Vorgaben am Norm- oder Richtlinientext gegengelesen.**

**2.1 Heizflächengleichung (DIN EN 442).** Normwärmeleistung bei 75/65/20 °C, logarithmische Übertemperatur
ΔT_ln,N = 49,8 K; Kennlinie Φ = K_M · ΔT_ln^n [Q1], [Q4]. Exponenten: Plattenheizkörper 1,20–1,30, Glieder- und
Röhrenradiatoren ≈ 1,30, Konvektoren 1,25–1,45, Gebläsekonvektoren bei fester Stufe ≈ 1,1–1,2, Flächenheizung ≈ 1,1
[Q1]–[Q3], [Q5], [Q6]. Die arithmetische Übertemperatur ist zulässig, solange (θ_R − θ_i)/(θ_V − θ_i) ≥ 0,7 [Q1];
darunter überschätzt sie die Leistung um 1 bis 4 % (Abl.). Für Typ‑22-Platten mit Konvektionsblechen weicht der
Ansatz um bis zu ≈ 10 % ab [Q4]. Bestandspaare: 90/70 °C (bis Mitte der 1970er), 70/55 °C, 55/45 °C [Q1].
Restleistung bei abgesenktem Vorlauf und konstantem Massenstrom (Abl., n = 1,3): Auslegung 70/55 → bei 55 °C
64 % (Rücklauf 45,4 °C), Auslegung 90/70 → 42 % (Rücklauf 46,6 °C).

**2.2 Flächenheizung (DIN EN 1264).** Höchste Oberflächentemperatur 29 °C in der Aufenthaltszone, 35 °C in der
Randzone, 33 °C im Bad; Basiskennlinie q = 8,92 · (θ_F,m − θ_i)^1,1 W/m², rund 100 W/m² bei 9 K Übertemperatur
[Q5]. Auslegung mit σ = 5 K im Raum mit der höchsten Wärmestromdichte, Vorlauf höchstens 55 °C [Q5]. Eine
Flächenheizung ist damit **in der Regel nicht durch den Höchstvorlauf einer Wärmepumpe begrenzt** — ihre Grenze ist
die Oberflächentemperatur, und die liegt unter jedem Wärmepumpen-Höchstvorlauf. Das Modell muss sie deshalb nicht
gesondert abbilden; die Übergabegleichung mit n = 1,1 genügt.

**2.3 Heizkurve aus Auslegungspunkt und Exponent.** Bei konstantem Massenstrom und lastlinearer Heizlast gilt
θ_V = θ_i + Δθ_m,N · φ^(1/n) + σ_N · φ/2 und θ_R = θ_V − σ_N · φ mit φ = (θ_i − θ_a)/(θ_i − θ_a,N) [Q1] (Abl.);
das ist die Form, die EPOS-Plan seit AK1 rechnet (Konzept Anlagenkopplung 3.4). Steilheit typisch 1,4 bei
Heizkörpern, 0,6 bei Fußbodenheizung [Q35]; Heizgrenze 15 °C, gebäudeabhängig 10–17 °C [Q34]. Die
Jahresarbeitszahl hängt an der Heizkurve, nicht an der Temperatur des Normauslegungspunkts [Q16], [Q17].

**2.4 Verflüssigerseite.** Prüfpaare nach DIN EN 14511-2: W35 30/35 °C (5 K), W45 40/45 °C (5 K), W55 47/55 °C (8 K),
W65 55/65 °C (10 K; Paar n. e.) [Q10]–[Q14]. Auslegungsspreizung 5–7 K, statische Heizflächen höchstens 10 K;
Volumenstrom je kW: 172 l/h bei 5 K, 86 l/h bei 10 K [Q8]. Der **Mindestvolumenstrom** ist herstellerspezifisch
[Q6], [Q8]; aus dem Verhältnis Auslegungs- zu Höchstspreizung folgen 50–70 % des Auslegungsvolumenstroms (Abl.).
Zu wenig Durchfluss führt zur Hochdruckabschaltung, zu wenig Volumen zum Takten; Mindestvolumen ohne Puffer
3–5 l/kW [Q8], nach VDI 4645 ≥ 3 l/kW (sek. [Q37]). **Höchstvorlauf je Kältemittelklasse:** R410A 55–60 °C,
R32 55–65 °C (sek.), R290 65–75 °C, R1234ze(E) 70–85 °C in Großanlagen [Q16], [Q18], [Q19], [Q21]. **R744 ist ein
Sonderfall:** Vorlauf 80–90 °C sind möglich, begrenzend ist der **Rücklauf** (Faustregel < 35 °C) [Q18]–[Q20]; Rücklaufgrenzen aller Wärmepumpen in 2.11.
Der Höchstvorlauf sinkt bei tiefer Quellentemperatur (Einsatzgrenze im Kennfeld) [Q7], [Q9].

**2.5 COP und Leistung über dem Vorlauf.** Eigene Medianauswertung der WPZ-Prüfresultate [Q13]: COP −1,5 bis
−2,1 % je Kelvin Vorlauf, Heizleistung nahezu unverändert (geregelte Geräte 0,99 zwischen W35 und W55,
Festdrehzahl ≈ −0,3 %/K). **Begrenzend ist also die Einsatzgrenze, nicht ein stetiger Leistungsabfall** — genau das
macht die Übergabegrenze beim Höchstvorlauf zur maßgebenden Größe.

**2.6 Bivalenzbegriffe.** Oberhalb des Bivalenzpunkts arbeitet nur die Wärmepumpe, unterhalb des Abschaltpunkts
keine [Q7], [Q18], [Q26]; Betriebsweisen auch in DIN EN 15450 (n. e.) [Q25]:

| Betriebsweise | über Bivalenzpunkt | zwischen den Punkten | unter Abschaltpunkt |
|---|---|---|---|
| monoenergetisch | WP | WP + Heizstab | Heizstab, sobald die Einsatzgrenze unterschritten ist |
| bivalent-parallel | WP | WP + Kessel | WP + Kessel |
| bivalent-teilparallel | WP | WP + Kessel | nur Kessel |
| bivalent-alternativ | WP | entfällt (Punkte fallen zusammen) | nur Kessel |

Der Kessel wird zugeschaltet, wenn die Wärmepumpe mit Höchstleistung läuft oder gesperrt ist und die Sollwerte
dauerhaft unterschritten werden [Q7].

**2.7 Deckungsanteile.** Tabelle nach DIN V 4701-10, Rechengrundlage der VDI 4650 [Q23] (sek.), [Q24]:
Bivalenzpunkt −5 °C → Deckung 98 % parallel, 91 % alternativ; 0 °C → 90 %/64 %; +2 °C → 83 %/46 %. Für das
Zahlenbeispiel heißt das (Abl.): Die Verschiebung des Bivalenzpunkts von −3,8 °C auf +1,8 °C senkt den
Deckungsanteil im alternativen Betrieb von rund 85 % auf rund 50 %. **Die Übergabegrenze ist damit keine
Randkorrektur.** Ein Hersteller nennt für bivalent-parallel nur 85–92 % Jahresanteil [Q39] — die Tabelle ist eine
Orientierung, die Stundenrechnung ersetzt sie.

**2.8 Hydraulische Einbindung.** Direkt ohne Puffer: ṁ_WP = ṁ_HK, Spreizung der Wärmepumpe gleich der des Heizkreises.
Reihenpuffer: gekoppelt. Parallelpuffer, Trennspeicher, hydraulische Weiche: entkoppelt, Mischung an der
Trennstelle [Q3], [Q7], [Q8]. Mischungsbilanz (Abl.):

```
ṁ_HK > ṁ_WP:  θ_V,HK = [ṁ_WP·θ_V,WP + (ṁ_HK − ṁ_WP)·θ_R,HK] / ṁ_HK ,   θ_R,WP = θ_R,HK
ṁ_WP ≥ ṁ_HK:  θ_V,HK = θ_V,WP ,   θ_R,WP = [ṁ_HK·θ_R,HK + (ṁ_WP − ṁ_HK)·θ_V,WP] / ṁ_WP
```

Ist der Verbraucherstrom größer, fährt die Wärmepumpe stets über der Heizkurve [Q7]; gefordert wird
ṁ_Erzeuger > ṁ_Verbraucher [Q3]. Beide Zweige sind energieerhaltend und am Umschaltpunkt ṁ_WP = ṁ_HK stetig;
kompakt gilt σ_WP·ṁ_WP = σ_HK·ṁ_HK (Abl.; ideale Mischung, kein Speicherinhalt, eingeprägte Ströme, stationär in
der Stunde). Mit r = ṁ_HK/ṁ_WP hebt r > 1 den nötigen Vorlauf der Wärmepumpe um (r − 1)·σ_HK (trifft θ_WP,max und
σ_max), r < 1 ihren Rücklauf um (1 − r)·σ_HK (trifft σ_min und die Rücklaufgrenze 2.11).

**Puffer.** Die Näherung „Mischung an der Entladeseite nicht gerechnet" (4.4) ist vertretbar, wenn ṁ_WP ≥ ṁ_HK oder
der Puffer geschichtet rechnet; sonst unterschätzt sie den Vorlauf der Wärmepumpe um bis zu (r − 1)·σ_HK, und die
Weichenbilanz wird als Schranke ausgewiesen. Bei R744 kommt der Rücklauf der Wärmepumpe aus der untersten
Pufferzone (wie der Kollektoreintritt der Solarthermie heute). Größenordnung (Abl., Heizkörper n = 1,3, Auslegung
65/55 °C bei −12 °C, θ_WP,max = 55 °C): Puffermischung verschiebt den Bivalenzpunkt einer unterkritischen
Wärmepumpe von −3,3 °C um +1,1 K (r = 1,2) bzw. +2,7 K (r = 1,5); der Deckungsanteil nach 2.7 sinkt parallel um 1
bzw. 5, alternativ um 5 bzw. 17 Prozentpunkte [Q23]. R744 an Heizkörpern 55/45 °C mit Rücklaufgrenze 35 °C:
Abschaltgrenze ohne Mischung bei +4 °C, mit 1,43-fachem Strom der Wärmepumpe bei +6 °C, Deckungsanteil alternativ
von ≈ 28 % auf unter 19 %. Eine belastbare JAZ-Differenz zwischen Parallel- und Reihenpuffer fand sich in
Feldstudien und Leitfäden nicht [Q55], [Q56].

**2.9 Reihenschaltung Wärmepumpe → Kessel (Vorwärmbetrieb).** Anerkannte Grundschaltung: Die Wärmepumpe fährt
Grundlast, der Kessel hebt im Vorlauf auf den Sollwert [Q7]; die serielle Einbindung gilt als robust, Nachteil rund
1 % Mehrverbrauch wegen höheren Kesselrücklaufs [Q33]. Die Wärmepumpe liefert dann auch Teilwärme, wenn sie den
Sollvorlauf nicht mehr erreicht — bis der Rücklauf ihre Spreizungsgrenze erreicht. Die Schaltung wirkt damit eher
parallel als teilparallel (Abl.). Rechenweg der Reihe (Abl.):

```
θ_V,WP = min( θ_WP,max ; θ_V,HK ; θ_R,sys + Φ_WP / (ṁ_WP · c_p) )
Φ_WP   = min( Φ_KF(θ_V,WP) ; ṁ_WP · c_p · (θ_WP,max − θ_R,sys) ) ,   Φ_WP = 0, wenn < Φ_min
Betrieb nur, wenn θ_R,sys < θ_WP,max − σ_min ;   R744 zusätzlich θ_R,sys ≤ θ_R,grenz (4.4)
Φ_K    = ṁ_sys · c_p · (θ_V,HK − θ_V,WP) ,       Rücklauf des Kessels = θ_V,WP
```

Der Kessel sieht als Rücklauf den **Vorlauf der Wärmepumpe**, nicht den Heizkreisrücklauf; das mindert seinen
Brennwertnutzen (2.12) und verlangt eine eigene Rücklaufstufe der Kesselkennlinie (4.4). Bei großem Systemstrom
(Kesselauslegung 15–20 K) unterschreitet σ_WP oft σ_min — dann Teilstrom oder Bypass um die Wärmepumpe (Abl.).

**2.10 Recht und Förderung (Stand 07.10.2026).** Das Gebäudemodernisierungsgesetz (GModG, in Kraft ab 29.07.2026)
löst das GEG ab; § 71h GEG und die 65‑%‑Pflicht sind aufgehoben [Q29], [Q30]. **§ 43 GModG:** Eine
Wärmepumpen-Hybridheizung erfüllt die Pflicht, wenn die Wärmepumpenleistung im Teillastpunkt A (DIN EN 14825,
−7 °C) mindestens **30 %** (parallel, teilparallel) bzw. **40 %** (alternativ) der Leistung des Spitzenlasterzeugers
beträgt [Q27], [Q28] (sek.). Die **BEG** verlangt weiter 65 % erneuerbare Wärme; Hybride gelten mit denselben
30/40‑%‑Anteilen als erfüllt, bilanziert nach DIN V 18599 [Q31], [Q32]. EPOS-Plan weist diese Anteile aus,
prüft aber keine Förderfähigkeit.

**2.11 Rücklaufgrenzen der Wärmepumpe.** Fünf Arten, getrennt nach Ursache:

| Art | Gilt für | Wirkt über | Wert | Folge im Rechenweg (4.4) |
|---|---|---|---|---|
| (i) physikalisch | R744 (transkritisch) | Gaskühleraustritt ≈ Wassereintritt + Grädigkeit | Planungsgrenze 35 °C [Q19]; Geräte bis 63–65 °C Eintritt [Q45], [Q46] | stetige Abwertung bis zur harten Grenze |
| (ii) abgeleitet | alle unterkritischen Kältemittel | Verflüssigungstemperatur am Vorlauf | θ_R,grenz = θ_WP,max − σ_min | aus vorhandenen Feldern, kein Eintrag nötig |
| (iii) Hersteller, Regler | Datenblatt- oder Reglerwert | Hochdruckwächter, Rücklaufsoll-Maximum | Datenblattwerte 50–65 °C | `Ruecklauf_Max`, wirkt als min(Feld, (ii)) |
| (iv) Anfahrgrenze unten | Luft/Wasser, Kaltstart | Mindestverflüssigungsdruck | unter ≈ 15 °C Rücklauf, Ende ≈ 23 °C [Q48] | Prüfhinweis, kein Feld |
| (v) Gas-Absorption | NH₃/H₂O | Absorber und Kondensator | ≈ θ_WP,max − 10 K [Q50] | Randnotiz |

(i) **R744.** Über dem kritischen Punkt (31 °C) gibt CO₂ Wärme gleitend ab; die Temperatur vor der Drossel folgt
dem Wassereintritt, und nahe der pseudokritischen Temperatur hebt jedes Kelvin Rücklauf die Enthalpie vor der
Drossel, den Drosselverlust und den optimalen Hochdruck [Q18], [Q42], [Q43] (sek.). Gemessen sinkt der COP um rund
1,8 %/K zwischen 28 und 35 °C Rücklauf [Q42], ein Fachaufsatz nennt 2–3 %/K [Q43] (sek.); der Vorlauf wirkt kaum (≈
−0,07 %/K, Abl. aus [Q20]). Die Geräte schalten bei 35 °C nicht ab: Leistung und COP sinken ab ≈ 29 °C Eintritt
stetig, zulässig sind 63–65 °C [Q45], [Q46]. 35 °C ist eine Effizienz- und Planungsgrenze.
Trinkwarmwasser-Kennwerte gelten bei 9–17 °C Eintritt [Q44] und überschätzen den COP in der Raumheizung um 15–30 %
(Abl.) — ein R744-Kennfeld ist nur mit seinem Bezugsrücklauf verwendbar.

(ii) **Unterkritisch.** Die Einsatzgrenzen der Verdichter sind in Verdampfungs- und Verflüssigungstemperatur
gegeben [Q49]; die Verflüssigungstemperatur folgt dem Vorlauf (θ_c ≈ θ_V + 3–5 K [Q3]). Der Rücklauf wirkt nur über
θ_V = θ_R + σ: Über θ_WP,max − σ_min kann die Wärmepumpe nicht mehr mit σ_min heizen. Für sich wirkt er allein über
die Unterkühlung, +0,3 … +1 %/K COP je K tieferem Rücklauf bei festem Vorlauf (Abl.) — zu klein für eine eigene
Kennlinie.

(iii) **Datenblatt und Regler.** Von sechzehn untersuchten Produktlinien nennen neun eine ausdrückliche höchste
Rücklauf- bzw. Eintrittstemperatur, fünf davon außerhalb R744: zwei R290-Geräte (65 °C; Abschaltschwelle ab Werk 50
°C, einstellbar bis 55 °C) [Q52], [Q53], zwei R32-Geräte (59 °C; ein R32-Gerät nennt 55 °C) [Q54], [Q51] und eine
Gas-Absorptionswärmepumpe [Q50]. Begründet wird das mit dem Schutz vor dem Ansprechen des Hochdruckwächters bzw.
der Verflüssigungsdruckgrenze [Q53], [Q50]. Der Abstand Höchstvorlauf − Höchstrücklauf beträgt 5–10 K bei
ΔT-geregelten Geräten, 20–25 K bei R290-Hochtemperaturgeräten mit fester Schwelle, 25–30 K bei R744.
Rücklaufgeregelte Geräte bilden die Heizkurve auf den Rücklauf ab; ihr „Rücklauf max." ist der Höchstvorlauf
abzüglich der Auslegungsspreizung, einstellbar 25–70 °C [Q47], [Q48] — eine Regelungsgrenze, die enger sein kann
als (ii).

(iv) **Anfahrgrenze.** Luft/Wasser-Geräte holen bei Außentemperatur unter 10 °C und Rücklauf unter 15 °C den
zweiten Erzeuger hinzu, bis der Rücklauf 23 °C erreicht [Q48]. In beheizten Gebäuden liegt der Rücklauf über der
Raumtemperatur, nach Sperrzeiten kühlt das Systemwasser höchstens gegen sie ab (Abl.) — vernachlässigbar,
Prüfhinweis nur bei Sollwerten unter 15 °C (UB‑Q10). (v) **Gas-Absorption:** Eintritt höchstens 50 °C im
Dauerbetrieb, 55 °C absolut [Q50]; nur von Belang, falls eine Gas-Wärmepumpe als Typ geführt wird.

**2.12 Rücklaufgrenzen anderer Wärmeerzeuger.**

| Erzeuger | Art der Grenze | typischer Wert | Wirkung | heute im Kern | Folge für das Konzept |
|---|---|---|---|---|---|
| Brennwertkessel Gas/Öl | stetig, oben | Taupunkt Erdgas ≈ 57 °C, Öl ≈ 47 °C [Q57]; Nutzungsgrad Erdgas ≈ 102 % (Hi) bei 20 °C → ≈ 92 % bei 60 °C Rücklauf [Q58] | kein Schaden, der Kessel läuft ohne Brennwertnutzen weiter [Q59] | `Kennlinie_Brennwert` über `Kesselkennlinie.Ruecklauf` (Heizkreis → Speicher → Paar → Rückfall) | Rücklaufstufe „Vorwärmer" (4.4, UB‑Q11) |
| NT-/Konstanttemperaturkessel | hart, unten (Taupunktkorrosion, zeitverzögert) | Mindestrücklauf 55–65 °C [Q59], [Q60] | Versottung über Monate | keine Grenze; Rücklaufanhebung vorausgesetzt, Energie unverändert | Prüfhinweis, kein Feld (UB‑Q10) |
| Biomassekessel | hart, unten (Pflichtbauteil Rücklaufanhebung) | Pellet ≈ 55 °C, Scheitholz 60–65 °C; Norm ≥ 55 °C [Q61], [Q62] | Versottung, Teer, Gewährleistung | wie NT-Kessel | Prüfhinweis, kein Feld (UB‑Q10) |
| BHKW (Verbrennungsmotor) | hart, oben (Motorkühlkreis) | ≈ 70 °C; Mikro-BHKW einstellbar bis 73 °C [Q63], [Q64] | Abschaltung | kein Temperaturbezug | `Ruecklauf_Max` am BHKW, Vorgabe 70 °C, Grund `RUECKLAUF_MAX` (UB‑Q9, UB‑E3) |
| Brennstoffzelle PEM | hart, oben | ≈ 50 °C [Q65] | kein Betrieb darüber | kein eigener Typ | Datenblattwert in dasselbe Feld, falls als BHKW geführt |
| Solarthermie | stetig (Kollektorgleichung) | Flachkollektor ≈ 0,5–0,7 Prozentpunkte je K (Abl. aus [Q67]) | Ertrag sinkt | Kollektoreintritt aus der untersten Pufferzone der Vorstunde | kein Feld |
| Fernwärme | vertraglich, oben | meist 50 °C [Q66] | Preiszuschlag | kein Erzeugertyp | Randnotiz |
| Heizstab | keine (Temperaturbegrenzer am Vorlauf) | — | — | an der Wärmepumpe | keine |
| Puffer | stetig (Schichtung) | — | vermischte Schichten mindern die nutzbare Spreizung | Paar nur für die Kapazität, `T_unten` bei Schichtung | Randnotiz; Herkunft des R744-Rücklaufs (2.8) |

Eine harte Höchstgrenze mit Stundenwirkung hat neben der Wärmepumpe nur das BHKW — derselbe Mechanismus wie
`Ruecklauf_Max`. Die Mindestgrenzen der NT- und Festbrennstoffkessel sichert in jeder Anlage eine Rücklaufanhebung;
die Stundenbilanz ändert sie nicht (Abl.). Brennwert und Solarthermie wirken stetig und sind im Kern abgebildet;
offen ist allein der richtige Kesselrücklauf im Vorwärmbetrieb.

## 3 Ausgangslage am Code

**3.1 Was AK1 rechnet** (Konzept Anlagenkopplung 3.1, 3.2, 10.2; Schritt H). Übergabegleichung mit arithmetischem
Mittel und konstantem Massenstrom `W_H = Phi_N / (theta_V,N − theta_R,N)` aus dem Auslegungspunkt; Lösung je
Stunde als skalares Nullstellenproblem (Newton, höchstens 8 Schritte). Eingaben am Gebäude (`Tab_Gebaeude`:
`Heizkreis_Aktiv`, Übergabeart, Exponent, Nennleistung, Auslegungspunkt, Heizkurve, `Regler_Proportionalband`)
und je Zone (`Tab_Zone`: `Uebergabe_Art`, `Uebergabe_Exponent`, `Uebergabe_Leistung_Nenn`, `Auslegung_Vorlauf`,
`Auslegung_Ruecklauf`, `Auslegung_Raumtemperatur`, `Regler_Proportionalband`). Schritt H1 kappt den Sollvorlauf am
Vorlaufangebot der Anlage und bucht den Grund `VORLAUF_ANLAGE`; der Rücklauf mehrerer Zonen wird
massenstromgewichtet zusammengeführt. Das Vorlaufangebot ist **das höchste `Vorlauf_Max` der verfügbaren Erzeuger**
(Rückfall `Vorlauf`; `Anlagenfahrplan.VorlaufAngebotC`, `EPOS.Kern/Allgemein/Simulation/Anlagenfahrplan.cs`).

**3.2 Was die Wärmepumpe tut.** `SimulationWaermepumpe.StuetzstelleWaehlen` wählt die Kennfeldstützstelle
(`Tab_Kenndaten`: `Vorlauf`, `Temperatur`, `COP`, `Ptherm`) am **gerechneten Vorlauf**; seit AK3‑I wird zwischen
zwei Stützstellen linear interpoliert, über der obersten gehalten. `Vorlauf_Max` an `Tab_Energieanlagen` wirkt
als **Angebotsdeckel** des Projektvorlaufs, nicht als Grenze der Wärmepumpe selbst. Bivalenz hängt allein an der
Außentemperatur: `Bivalenter_Betrieb`, `Betriebsart` (`DbWerte.WP_BETRIEBSART_PARALLEL`/`_TEILPARALLEL`/
`_ALTERNATIV`) und `Abschaltpunkt` (`SimulationWaermepumpe.cs`, Betriebsartverzweigung: teilparallel aus bei
`Temperatur ≤ Abschaltpunkt`, alternativ über `AlternativAus`, parallel unverändert). `Heizstab` liefert
elektrische Nachheizung je Baustein; `Mindestleistung_kW` und `Taktverlustfaktor_Cd` an `Tab_WP` rechnen das Takten.
Die Kaskade (`Tab_Einstellungen.Tool_1` bis `Tool_4`, Konzept Simulationsablauf 13) ordnet die Direktdeckung:
Steht die Wärmepumpe vorn, deckt sie bis zur Kennfeldleistung, der Kessel den Rest.

**3.3 Was fehlt.**

| Lücke | Folge heute |
|---|---|
| Übergabegrenze beim Höchstvorlauf als Kriterium | Bietet ein Kessel 75 °C an, rechnet die Wärmepumpe an der obersten Stützstelle mit voller Kennfeldleistung — sie liefert Wärme bei einem Vorlauf, den sie nicht erreicht |
| Spreizung, Mindestvolumenstrom der Wärmepumpe | kein Feld; nur `WQ_Spreizung` auf der Quellseite |
| Einbindung (direkt, Puffer, Weiche) | nur Senken und Pufferzuordnung (`Z_AnlageSenke`, `ID_PUFFER`), keine Mischungsbilanz |
| Vorwärmbetrieb (Reihe) | nicht vorhanden; der Kessel ergänzt nur parallel am selben Vorlauf |
| Rücklaufgrenze (alle Wärmepumpen) | nicht vorhanden; der Rücklauf erreicht die Wärmepumpe nicht — `SimulationWaermepumpe` erhält nur `Heizkreisvorlauf`, obwohl `Anlagenkopplung.Kreis` Vor- und Rücklauf liefert |
| Kesselrücklauf im Vorwärmbetrieb | `Kesselkennlinie.Ruecklauf` kennt nur Heizkreis → Speicher → Paar → Rückfall; ein vorgewärmter Rücklauf fehlt |
| Rücklaufgrenze BHKW | kein Temperaturbezug in `SimulationBHKW` |
| berechnete Bivalenzpunkte, Bivalenzdiagramm, Herleitungszeile | `Bivalenzpunkt = -100` ist ein unbelegtes Feld; kein Bild im `ChartRenderer` |

**3.4 Abgrenzung zu AK3.** AK3 schließt den Kreis zwischen Gebäude und Erzeugern; der Vorlauf bleibt dort eine
**Vorgabe** (Heizkurve, `Vorlauf_Max`, H2-Raumeinfluss), die Angebotsfunktion `Angebot(h, V)` summiert die
Kapazitäten der verfügbaren Erzeuger beim Vorlauf V ([Entwurf AK3](Gebaeudesimulation/2026-10-07_Entwurf_AK3.md),
Festlegungen 5–8). AK3‑I (E102) interpoliert das Kennfeld über den Vorlauf. **Dieses Papier ändert keine der beiden
Festlegungen;** es ersetzt in der Kapazität der Wärmepumpe „Kennfeld bei V" durch „Kennfeld bei min(V, Höchstvorlauf),
begrenzt durch Übergabe und Hydraulik" (4.3–4.5). Ohne Kopplung (Übergabeart `IDEAL` oder Kopplungsstufe aus) gibt
es keine Übergabedaten; dann rechnet die Wärmepumpe wie heute, und die Herleitungszeile sagt das.

## 4 Zielbild und Rechenweg je Stunde

### 4.1 Größen

```
W_H        Σ ṁ·c_p der Heizkreise (Gebäude bzw. Zonen) aus dem Auslegungspunkt   [kW/K]
θ_V,soll   Sollvorlauf der Stunde (Heizkurve, H2), vor der Kappung durch H1       [°C]
θ_R        gerechneter Rücklauf der Stunde, massenstromgewichtet (Schritt H)     [°C]
θ_WP,max   Höchstvorlauf der Wärmepumpe = Vorlauf_Max (Rückfall Vorlauf)          [°C]
σ_A, σ_max, σ_min   Auslegungs-, Höchst-, Mindestspreizung der Wärmepumpe         [K]
ṁ_WP·c_p   Wasserstrom der Wärmepumpe = Φ_WP,N / σ_A                              [kW/K]
Φ_KF(θ)    Kennfeldleistung bei Vorlauf θ und Quelltemperatur der Stunde (AK3‑I)  [kW]
```

### 4.2 Schritt UB‑a — Kalibrierung aus dem Auslegungspunkt

Je Zone (bzw. Gebäude ohne Zonen) einmal je Lauf, unverändert aus AK1:

```
W_H,z    = Φ_N,z / (θ_V,N,z − θ_R,N,z)
Δθ_m,N,z = (θ_V,N,z + θ_R,N,z)/2 − θ_i,N,z
```

### 4.3 Schritt UB‑b — Übergabegrenze beim Höchstvorlauf

Je Zone das Gleichgewicht Wasserseite = Heizflächengleichung bei Auslegungsmassenstrom und Vorlauf θ_WP,max, mit
derselben Lösungsroutine wie H2 (eindeutig, weil f streng monoton fällt):

```
f(Φ) = Φ_N,z · ((θ_WP,max − Φ/(2·W_H,z) − θ_i) / Δθ_m,N,z)^n_z − Φ = 0
Φ_UE,max,z = Nullstelle,   θ_R,UE,z = θ_WP,max − Φ_UE,max,z / W_H,z,   Δθ_UE,z = θ_WP,max − θ_R,UE,z
Φ_UE,max   = Σ_z Φ_UE,max,z      (je Gebäude, dann je Projekt)
```

θ_i ist in der Stundenrechnung die Raumtemperatur der Stunde; für die Herleitungszeile und das Bivalenzdiagramm der
Auslegungsraumtemperatur. Grenzfälle: θ_WP,max ≤ θ_i → Φ_UE,max = 0; θ_WP,max ≥ θ_V,N → keine Begrenzung durch die
Übergabe (Flächenheizung, großzügige Heizkörper). Für das Zahlenbeispiel: 5,68 kW, Rücklauf 46,48 °C, 8,52 K.

### 4.4 Schritt UB‑c — Grenzen der Wärmepumpe (ΔT-Restriktionen)

```
Φ_WP,grenz(h) = min( Φ_KF(min(θ_V,soll, θ_WP,max)) ,  Φ_Hydraulik(h) )
```

| Einbindung | Φ_Hydraulik | Regel |
|---|---|---|
| **direkt** | W_H · min(θ_V − θ_R, σ_max) | Die Wärmepumpe sieht den Heizkreisstrom. Ist die Heizkreisspreizung größer als σ_max, erreicht sie den Vorlauf nicht; ihre Leistung ist W_H · σ_max (Grund `SPREIZUNG_MAX`). Ist sie kleiner als σ_min, unterschreitet der Durchfluss den Mindestvolumenstrom nicht, aber die Wärmepumpe moduliert unter ihre Mindestleistung: **die Stunde taktet** — Energie wie heute, Taktverlust über `Mindestleistung_kW` und `Taktverlustfaktor_Cd`, gezählt als Stunde „Spreizung unterschritten" |
| **Puffer** (Reihen- oder Parallelpuffer) | ṁ_WP·c_p · σ_max an der Ladeseite | Die Ladeseite rechnet das Puffermodell des Bestands; der Puffer kann nie wärmer sein als θ_WP,max. Die Mischungsbilanz der Entladeseite wird **benannt nicht gerechnet** (Näherung: Entnahme mit `TNutz` des Kanals, wie AK3 Festlegung 6); vertretbar bei ṁ_WP ≥ ṁ_HK oder geschichtetem Puffer, sonst Weichenbilanz als Schranke (2.8); R744 liest den Rücklauf aus der untersten Pufferzone |
| **Weiche** | ṁ_WP·c_p · (θ_V,WP − θ_R,HK) bei ṁ_WP < ṁ_HK | Mischungsbilanz 2.8: Ist der Heizkreisstrom größer, sinkt der Heizkreisvorlauf unter θ_WP,max; die Übergabegrenze 4.3 wird dann am gemischten Vorlauf gelöst (dieselbe Nullstellenroutine mit einer zweiten Gleichung, eindeutig). Bei ṁ_WP ≥ ṁ_HK wie direkt, ohne σ-Grenze aus dem Heizkreis |

**Mindestvolumenstrom.** Er wird als Anteil des Auslegungsvolumenstroms geführt (Vorgabe 60 %, belegt 56–84 % des
Nennstroms [Q50], [Q51]). Zum kleinsten Volumenstrom gehört die größte Spreizung, ṁ ≥ ṁ_min ⇔ σ ≤ Φ/(ṁ_min·c_p); er
wirkt deshalb als **Höchst**spreizung der Stunde:

```
σ_max,eff(h) = min( σ_max , Φ_WP(h) / (ṁ_WP,min · c_p) )
```

Die Mindestspreizung σ_min bindet bei kleinster Leistung oder beim Bypass mit ṁ_min. Direkte Einbindung mit
Überströmventil: ṁ_WP = max(ṁ_HK, ṁ_min), gerechnet mit dem Zweig ṁ_WP ≥ ṁ_HK der Mischungsbilanz 2.8 — der
Rücklauf der Wärmepumpe steigt durch die Beimischung, die Rücklaufgrenze wird an ihm geprüft; σ_WP ≥ σ_min verlangt
Φ_HK ≥ ṁ_min·c_p·σ_min, sonst taktet die Stunde (ṁ_min = 0,6·ṁ_N, σ_A = 5 K, σ_min = 3 K: Φ_HK ≥ 0,36·Φ_N, Abl.).
Wer nur einen der Werte Mindestvolumenstrom und σ_max kennt, gibt diesen ein; der andere folgt aus ṁ_min/ṁ_N =
σ_A/σ_max (Herleitungszeile). Gelten beide, gilt die strengere.

**Rücklaufgrenze (alle Wärmepumpen).** Feld `Ruecklauf_Max` an jeder Wärmepumpe (nullbar). θ_R,WP ist der Rücklauf
zur Wärmepumpe: direkt der Heizkreisrücklauf, an Weiche oder Überströmventil der gemischte nach 2.8, am Puffer bei
R744 die unterste Pufferzone.

```
θ_R,grenz = min( Ruecklauf_Max , θ_WP,max − σ_min )       leeres Feld: nur der zweite Term
θ_R,WP(h) > θ_R,grenz   →   Φ_WP = 0                       Grund RUECKLAUF_MAX
R744:  f(h) = 1 − k · (θ_R,WP − θ_R,bez)   für θ_R,bez < θ_R,WP ≤ Ruecklauf_Max, sonst f = 1
       Φ_WP = Φ_KF · f ,   COP = COP_KF · f ,   Stromaufnahme unverändert
```

Die Stromaufnahme bleibt, weil die Verdichterarbeit beim transkritischen Prozess bei festem Hochdruck nahezu gleich
bleibt (Abl.). Vorgaben für R744: k = 2,5 %/K (belegt 1,8–3 %/K [Q42], [Q43]), θ_R,bez = 30 °C (Bezugsrücklauf des
Kennfelds, EN-14511-Paar 30/35 °C [Q14]), `Ruecklauf_Max` = 40 °C; mit k = 0 bleibt die harte Grenze 35 °C wählbar
(UB‑Q3). Neue nullbare Felder `Ruecklauf_Bezug` und `Ruecklauf_Abwertung_ProzentJeK` (leer = keine Abwertung). Die
Unterkühlungswirkung unterkritischer Geräte (2.11) bleibt ungerechnet. **Übergabe des Rücklaufs:**
`SimulationWaermepumpe` bekommt neben `Heizkreisvorlauf` den `Heizkreisruecklauf` aus `Anlagenkopplung.Kreis`
(UB‑E3); ohne Kopplung gibt es keinen Rücklauf, die Grenze ruht, und die Herleitungszeile sagt das.

**Rücklaufstufe „Vorwärmer" der Kesselkennlinie.** In Stunden mit Vorwärmbetrieb und laufender Wärmepumpe (B3) ist
der Kesselrücklauf θ_V,WP (2.9). `Kesselkennlinie.Ruecklauf` erhält dafür die Stufe „Vorwärmer" vor „Heizkreis"
(Kette Vorwärmer → Heizkreis → Speicher → Paar → Rückfall); sonst rechnet der Brennwertkessel im Vorwärmbetrieb mit
zu viel Brennwertnutzen — bei Erdgas ≈ 99–100 % bei 30–35 °C statt ≈ 93–95 % bei 45–55 °C Rücklauf (Abl. aus [Q58])
(UB‑E2, UB‑Q11). **BHKW (Option, UB‑Q9):** `Ruecklauf_Max` auch am BHKW; liegt der Rücklauf zum BHKW (Heizkreis
bzw. unterste Pufferzone) darüber, liefert es in der Stunde nichts (Grund `RUECKLAUF_MAX`) — anlagenbedingte
Abschaltgrenze, getrennt vom Auslegungspaar `Vorlauf`/`Ruecklauf`; der Auslegungsrücklauf liegt darunter
(Prüfregel mit Hinweis, Umsetzungskonzept U‑3).

### 4.5 Schritt UB‑d — Betriebsbereiche und Bivalenzpunkte

Je Stunde ein Bereich, in dieser Reihenfolge geprüft (Bezeichner der Ergebnisse in Klammern):

```
B0  Wärmepumpe nicht verfügbar (Sperrzeit, Zeitprogramm, Abschaltpunkt, Rücklaufgrenze)   → NUR_KESSEL
B1  θ_V,soll ≤ θ_WP,max  und  Φ_Bedarf ≤ Φ_WP,grenz                                         → WP_ALLEIN
B2  θ_V,soll ≤ θ_WP,max  und  Φ_Bedarf > Φ_WP,grenz                                         → PARALLEL
        Wärmepumpe Φ_WP,grenz, Kessel den Rest am selben Vorlauf (heutiges Verhalten)
B3  θ_V,soll > θ_WP,max  und  Vorwaermbetrieb = 1                                          → VORWAERMUNG
        Φ_WP = min( W_H·(θ_WP,max − θ_R) , Φ_KF(θ_WP,max) , Φ_Hydraulik )
        Φ_WP = 0, wenn θ_WP,max − θ_R < σ_min                       (Grund SPREIZUNG_MIN)
        Anteil a = (θ_WP,max − θ_R) / (θ_V,soll − θ_R),  Kessel hebt auf θ_V,soll
B4  θ_V,soll > θ_WP,max  und  Vorwaermbetrieb = 0                                          → NUR_KESSEL
        die Wärmepumpe kann zu einem Vorlauf über ihrem Höchstvorlauf nichts beitragen
```

**Ohne Kessel** (die Wärmepumpe ist der einzige Wärmeerzeuger): Das Vorlaufangebot ist θ_WP,max, H1 kappt, die
Übergabe liefert höchstens Φ_UE,max — der Fehlbetrag ist Komfortverlust und Restbedarf wie in AK2
(`Komfort_Unterschreitungsstunden`, `Komfort_Kelvinstunden`). Das rechnet AK1/AK2 heute schon; neu ist nur, dass
Bereich und Grund ausgewiesen werden. **Mit Heizstab** wirkt der Heizstab in B3 wie der Kessel in Reihe (er sitzt
im Vorlauf der Wärmepumpe); seine Leistung begrenzt die Anhebung, den Rest trägt Komfortverlust.

**Bivalenzpunkte (berechnet, für Herleitungszeile, Diagramm und Bericht).** Aus der Heizkurve (2.3), der lastlinearen
Heizlast Φ(θ_a) = Φ_N · φ(θ_a) und der Kennfeldgeraden bei θ_WP,max über der Quelltemperatur:

```
erster Bivalenzpunkt   θ_biv,1 = höchste θ_a mit  Φ(θ_a) > min(Φ_UE,max , Φ_KF(θ_WP,max, θ_a))
                                 (gleichwertig: θ_V,soll(θ_a) = θ_WP,max, wenn die Übergabe begrenzt)
zweiter Bivalenzpunkt  θ_biv,2 = Vorwärmbetrieb: höchste θ_a mit θ_R,soll(θ_a) ≥ θ_WP,max − σ_min
                                 ohne Vorwärmbetrieb: θ_biv,2 = θ_biv,1
                                 höchstens der eingegebene Abschaltpunkt (teilparallel, alternativ)
```

Zahlenbeispiel (Gebäude 10 kW bei −12 °C, Heizkörper 75/60, θ_WP,max = 55 °C, Kennfeld 7 kW bei −7 °C bis 9 kW
bei +7 °C, σ_min = 3 K): nach Kennfeld allein −3,8 °C; **θ_biv,1 = +1,8 °C** (Übergabe begrenzt);
**θ_biv,2 = −3,6 °C** im Vorwärmbetrieb (−6,6 °C bei σ_min = 0). Wärmepumpe im Vorwärmbetrieb: 0 °C → 4,40 kW
von 6,25 kW, −2 °C → 3,03 kW von 6,88 kW; ohne Vorwärmbetrieb unter +1,8 °C nichts.

**Wechselwirkung mit „Bivalenter Betrieb" und `Abschaltpunkt` (Vorschlag, UB‑Q4).** Der eingegebene Abschaltpunkt
bleibt als **Deckel**: Unter ihm ist die Wärmepumpe bei teilparallel und alternativ aus (B0), wie heute. Die
berechneten Punkte wirken zusätzlich über B3/B4 — es gilt jeweils, was die Wärmepumpe früher abschaltet. Die
Betriebsart steuert die Bereiche so:

| Betriebsart | B2 (Kessel ergänzt) | B3/B4 (über Höchstvorlauf) | alternativ-Sonderregel |
|---|---|---|---|
| parallel | ja | B3, wenn Vorwärmbetrieb, sonst B4 | — |
| teilparallel | ja | wie parallel; unter Abschaltpunkt B0 | — |
| alternativ | nein: Reicht Φ_WP,grenz nicht, deckt der Kessel allein | B4 (Vorwärmbetrieb nicht wählbar) | Umschaltung je Stunde statt an der Außentemperatur, Abschaltpunkt bleibt Deckel |

Die Herleitungszeile nennt beide Werte: „Abschaltpunkt eingegeben −10 °C; aus der Übergabe berechnet −3,6 °C —
maßgebend −3,6 °C."

**Einbau.** Profilweg (AK1/AK2): in der Kaskadenstunde ersetzt `Φ_WP,grenz` bzw. der Bereichswert die
Kennfeldkapazität der Wärmepumpe; θ_V,soll, θ_R und W_H kommen aus dem Projektheizkreis (`HeizkreisProjekt`).
AK3: in `Angebot(h, V)` (Festlegung 5) dieselbe Regel; θ_R hängt dort vom Durchlauf ab — die Abhängigkeit ist
monoton und wird von den Abbruchschwellen des Kreises mitgeführt (benannt im Prüforakel). Neue
Verfügbarkeitsgründe: `UEBERGABE_HOECHSTVORLAUF`, `SPREIZUNG_MAX`, `SPREIZUNG_MIN`, `RUECKLAUF_MAX`. Eine
Wärmepumpe ohne die neuen Felder und mit `Vorwaermbetrieb = 0` verhält sich in B1/B2 wie heute; **neu wirkt B4**
(UB‑Q1). Wirksam wird die Übergabegrenze nur bei gesetztem `Einbindung`; leer bleibt der heutige Weg
(Umsetzungskonzept U‑1, entschieden 08.10.2026).

## 5 Datenmodell

**5.1 Neue Spalten (Vorschlag).** Gerätegrenzen gehören zum Gerät, Schaltung und Einbindung zur Anlage:

| Spalte | Tabelle | Typ | Vorgabe bei NULL | Bereich | Begründung |
|---|---|---|---|---|---|
| `Spreizung_Auslegung_K` | `Tab_WP`, `Tab_WP_STAMM` | REAL | 5 K | 3–8 | Gerätewert (EN 14511-Paar); bestimmt ṁ_WP |
| `Spreizung_Max_K` | dto. | REAL | 10 K (R744: 30 K) | 5–40 | Herstellergrenze [Q8] |
| `Spreizung_Min_K` | dto. | REAL | 3 K | 0–8 | Pumpenaufwand, Takten [Q8] (Abl.) |
| `Mindestvolumenstrom_Prozent` | dto. | REAL | 60 % | 20–100 | σ_N/σ_max (Abl.); Herstellerwert hat Vorrang |
| `Ruecklauf_Max` | dto. | REAL | leer = abgeleitet θ_WP,max − σ_min | 20–70 °C | Datenblatt, Regler [Q51]–[Q54]; R744 [Q42]–[Q46] |
| `Ruecklauf_Bezug` | dto. | REAL | leer = 30 °C, wenn abgewertet wird | 20–40 °C | Bezugsrücklauf des Kennfelds (R744) |
| `Ruecklauf_Abwertung_ProzentJeK` | dto. | REAL | leer = keine Abwertung | 0–5 | R744 [Q42], [Q43] |
| `Kaeltemittel` | dto. | TEXT ohne CHECK-Liste (Werteliste im Kern als Klappliste) | leer = allgemeine (unterkritische) Vorgaben | `R410A`, `R32`, `R290`, `R744`, `R134a`, `R1234ze(E)`, `R407C`, `R454C`, `R455A`, `R1233zd(E)`, `SONSTIGES` | wählt die Vorgaben je Kältemittelklasse (6.3); Umsetzungskonzept U‑2 |
| `Ruecklauf_Max` | `Tab_BHKW` (Option UB‑Q9) | REAL | leer = keine Grenze; Neuanlage und Schnellwahl 70 °C | 40–90 °C | Motorkühlkreis [Q63], [Q64]; anlagenbedingte Abschaltgrenze, getrennt vom Auslegungspaar `Vorlauf`/`Ruecklauf`, das darunter liegt (Prüfregel mit Hinweis, Umsetzungskonzept U‑3) |
| `Einbindung` | `Tab_Energieanlagen` | TEXT `CHECK (Einbindung IN ('DIREKT','PUFFER','WEICHE'))` | `PUFFER`, wenn der Anlage ein Heizungspuffer zugeordnet ist, sonst `DIREKT` | — | Anlagenschaltung |
| `Vorwaermbetrieb` | `Tab_Energieanlagen` | INTEGER `CHECK (Vorwaermbetrieb IN (0,1))` | 0 | 0/1 | Anlagenschaltung (Reihe WP → Kessel) |

`Vorlauf_Max` an `Tab_Energieanlagen` **genügt als Höchstvorlauf** — eine eigene Spalte
`Bivalenz_Abschalt_Vorlauf` brächte eine zweite Wahrheit. Die Vorgabe bei NULL bleibt `Vorlauf` (AK2); neu ist allein
die Schnellwahl nach Kältemittelklasse im Dialog (Abschnitt 6). Die Klasse steht im Gerätefeld `Kaeltemittel`
(Umsetzungskonzept U‑2, entschieden 08.10.2026): Die Schnellwahl speichert sie und füllt nur leere Felder mit den
Vorgaben der Klasse; gefüllte Felder bleiben.

**Katalog oder Projekt (UB‑Q2).** Empfehlung: Gerätegrenzen am Katalog (`Tab_WP` und `Tab_WP_STAMM`, mit den
Projektkopien des Katalogs wie `Mindestleistung_kW`), weil sie zum Gerät gehören und mit VDI-3805-Daten kommen
können; leere Felder rechnen mit der Normvorgabe der Tafel und sagen das in der Herleitungszeile. Die Alternative —
alles an `Tab_Energieanlagen` — wäre schneller gebaut, ließe aber jede Anlage dieselbe Gerätegrenze neu eingeben.
Die Übergabespalten an `Tab_Gebaeude` und `Tab_Zone` bleiben unverändert.

**Ergebnisspalten** an `Tab_ErgebnisWaermepumpeModul` (und summiert an `Tab_ErgebnisWaermepumpe`):
`Bereich_WpAllein_h`, `Bereich_Parallel_h`, `Bereich_Vorwaermung_h`, `Bereich_NurKessel_h` (INTEGER), die
zugehörigen Wärmemengen `…_MWh` der Wärmepumpe (REAL), `Spreizung_Unterschritten_h`, `Ruecklauf_Ueberschritten_h`,
`Bivalenzpunkt_1`, `Bivalenzpunkt_2`, `Uebergabe_Max_kW` (REAL, berechnet nach 4.3/4.5); an den Kesselergebnissen die
Stufenstunden der Kesselkennlinie (`RuecklaufStufenstunden`) um die Stufe „Vorwärmer" — ausgewiesen im
Protokollhinweis `SIMENG_KESSEL_BRENNWERT_BETRIEB`, keine Ergebnisspalte am Kessel (Umsetzungskonzept U‑4).

**5.2 Schemaschritt.** Ein Schritt für Eingabe- und Ergebnisspalten, **Nummer erst bei der Umsetzung anmelden**
(Zeile „Schemaschritt angemeldet" der [Statusdatei](Status_iOS_Migration.md); heute ist 200 die nächste freie
Nummer, 199 ist vergeben). Spalten nullbar, Boolean mit `CHECK (… IN (0,1))`, Tabellen bleiben `STRICT`; der
`SqlDialektPruefer` läuft nach jeder neuen Anweisung. Der Katalogabgleich und das Projektpaket führen die acht
Gerätespalten wie die übrigen `Tab_WP`-Spalten.

**5.3 Einfrierregel und Basis.** Der Rechenweg ändert Ergebnisse überall dort, wo θ_V,soll > θ_WP,max und ein Kessel
das Vorlaufangebot hebt (B4 statt voller Kennfeldleistung) — zu prüfen an **1047, 1054, 1056 und 1058**; ohne
Kopplung bleibt jedes Projekt byte-gleich. Die Wirksamkeit hängt an `Einbindung` (Umsetzungskonzept U‑1, entschieden
08.10.2026: Opt-in): Bei leerem Feld bleibt der heutige Weg, 1047, 1054, 1056 und 1058 ändern ihr Ergebnis nicht,
und die Basis wächst allein um das neue Referenzprojekt. Neue Basis im selben Schritt, begründet in
[`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md). Neues Referenzprojekt als **Kopie von 1056**
(nächste freie Projektnummer, heute 1060): Gebäude mit Heizkörpern 75/60 °C, Wärmepumpe mit `Vorlauf_Max` 55 °C
und `Vorwaermbetrieb` 1, Kessel in Reihe, Nachtsperre und Zeitprogramme von 1056 zurückgesetzt, damit die Wirkung
allein der Übergabegrenze gehört (UB‑Q7). Neue Einfrierregel in [`CLAUDE.md`](../../CLAUDE.md): gesäte
Spreizungs- und Einbindungsdaten eines Referenzprojekts (die acht Gerätespalten seiner Wärmepumpe, `Einbindung`,
`Vorwaermbetrieb`, `Vorlauf_Max`) und das Anlegen oder Entfernen dieses Referenzprojekts.

## 6 Bedienung und Eingabe

**6.1 Ort.** Konfiguration der Wärmepumpe (`EPOS.UI/Dialoge/Waermepumpe/WaermepumpeKonfiguration.razor`), neue
`Formulargruppe` **„Bivalenz und Übergabe"** an Stelle der heutigen Gruppe „Betrieb" (Schalter bivalenter Betrieb,
Betriebsart, Bivalenztemperatur ziehen mit um). Der Höchstvorlauf bleibt in der Gruppe „Betriebszeiten" (AK2) und
bekommt dort die Schnellwahl; die Gruppe „Bivalenz und Übergabe" zeigt ihn als Lesewert mit Verweis. Die
Gerätegrenzen (Spreizungen, Mindestvolumenstrom, Rücklaufgrenze) stehen zusätzlich im Stammblatt des Katalogs
(`WaermepumpeStammFelder.razor`); in der Konfiguration erscheinen sie als Lesewerte mit Herkunft „Katalog" bzw.
„Vorgabe", änderbar nur im Katalog (UB‑Q2).

**6.2 Felder der Gruppe „Bivalenz und Übergabe".** Mockup: `Mockups/Waermepumpe_Bivalenz_Uebergabe.html`.

| Feld | Baustein | Vorgabe | Sichtbar | Herleitungszeile / Sperre |
|---|---|---|---|---|
| Bivalenter Betrieb | `Schalter` | aus | immer | — |
| Betriebsart | `Auswahlfeld` | teilparallel | bivalent | „Vorwärmbetrieb ist bei alternativ nicht wählbar" |
| Bivalenztemperatur (Abschaltpunkt) | `Zahlenfeld` °C | leer | teilparallel, alternativ | „eingegeben … — berechnet … — maßgebend …" |
| Einbindung | `Auswahlfeld` direkt / Puffer / Weiche | aus der Pufferzuordnung | bivalent oder Kopplung aktiv | „Puffer: Entladeseite als Näherung" |
| Vorwärmbetrieb (Kessel in Reihe) | `Schalter` | aus | parallel, teilparallel | „Die Wärmepumpe wärmt den Rücklauf bis zum Höchstvorlauf vor, der Kessel hebt auf den Sollvorlauf" |
| Höchstvorlauf | Lesewert | `Vorlauf_Max`, sonst `Vorlauf` | immer | Klappliste „Kältemittel" und Schnellwahl in „Betriebszeiten" (speichert die Klasse in `Kaeltemittel`, füllt nur leere Felder): R410A/R32 55 °C · R290 70 °C · R744 80 °C, Rücklauf ≤ 40 °C (Abwertung ab 30 °C) · R1234ze(E) 80 °C |
| Spreizung Auslegung / max. / min., Mindestvolumenstrom | Lesewerte | Katalog, sonst Tafel 6.3 | Kopplung aktiv | Herkunft je Wert |
| Höchster Rücklauf | Lesewert | `Ruecklauf_Max`, sonst abgeleitet | Kopplung aktiv | Herkunft „Katalog" bzw. „abgeleitet: 52 °C = 55 − 3"; bei R744 „Bezugsrücklauf 30 °C · Abwertung 2,5 %/K · Grenze 40 °C" |

**Herleitungszeile der Gruppe** (aus den vorhandenen Gebäudedaten gerechnet, `Herleitungszeile`-Baustein):
„Übergabe bei Höchstvorlauf 55 °C: **5,7 kW von 10,0 kW** Heizlast (57 %), Rücklauf 46,5 °C, Spreizung 8,5 K ·
erster Bivalenzpunkt **+1,8 °C** (nach Kennfeld allein −3,8 °C) · zweiter Bivalenzpunkt **−3,6 °C** (Vorwärmbetrieb) ·
Wärmepumpe bei −7 °C 70 % der Kesselleistung (§ 43 GModG: mindestens 30 %)." Ohne Kopplung: „Die Übergabe ist nicht
beschrieben (Kopplung aus) — die Wärmepumpe rechnet ohne Übergabegrenze." Ein kleines Bivalenzdiagramm (dasselbe
Zeichenmodell wie 7.2) klappt unter der Zeile auf.

**Weiche Sperren** (`Warnbanner`, Stufe Warnung, speichern bleibt möglich): σ_min ≥ σ_max; Höchstvorlauf unter dem
Auslegungsvorlauf der Flächenheizung; Rücklaufgrenze unter dem Auslegungsrücklauf aller Zonen („Die Wärmepumpe
liefert bei diesem Rücklauf nie"); Anteil nach § 43 GModG unterschritten (Hinweis, keine Sperre);
Vorwärmbetrieb ohne Kessel oder Heizstab in der Kaskade.

**6.3 Vorgabewerte (Tafel für leere Felder und Schnellwahl, aus Recherche Abschnitt 6).**

| Größe | Vorgabe | Bereich | Quelle |
|---|---|---|---|
| Exponent Platten-, Glieder-, Röhrenheizkörper | 1,30 | 1,20–1,35 | [Q1]–[Q3] |
| Exponent Konvektor / Gebläsekonvektor / Fläche | 1,40 / 1,10 / 1,10 | 1,25–1,50 / 1,0–1,2 / 1,0–1,1 | [Q1], [Q2], [Q5] |
| Auslegungsspreizung Heizkreis Fläche / Heizkörper an WP | 5 K / 7 K | 3–7 / 5–10 K | [Q5], [Q8] |
| Spreizung Verflüssiger, Auslegung | 5 K | 3–8 K | [Q3], [Q8], [Q11] |
| Höchstspreizung | 10 K; R744 30 K (Herstellerwert hat Vorrang) | 8–10 K | [Q8]; R744 [Q42], [Q44] (Abl.: bei R744 begrenzt der Rücklauf, nicht die Spreizung) |
| Mindestspreizung | 3 K | 2–5 K | [Q8] (Abl.) |
| Mindestvolumenstrom | 60 % | 40–100 % (belegt 56–84 %) | (Abl.), [Q6], [Q8], [Q50], [Q51] |
| Höchstvorlauf R410A/R32 · R290 · R744 · R1234ze(E) | 55 · 70 · 80 · 80 °C | 55–65 · 65–75 · 65–90 · 70–85 °C | [Q16], [Q18]–[Q21]; R32-Band (sek.) |
| Rücklaufgrenze R744 | 40 °C (mit Abwertung) · 35 °C (harte Grenze ohne Abwertung) | 30–50 °C | [Q19], [Q42], [Q45], [Q46] |
| Bezugsrücklauf R744 | 30 °C | 25–35 °C | [Q14], [Q42] |
| Abwertung R744 | 2,5 %/K | 1,8–3 %/K | [Q42], [Q43] (sek.) |
| Rücklaufgrenze unterkritisch | leer (abgeleitet θ_WP,max − σ_min) | Datenblattwerte 50–65 °C | [Q49], [Q51]–[Q54] |
| Rücklaufgrenze BHKW (Option) | 70 °C | 60–73 °C | [Q63], [Q64] |
| Vorgaben je Kältemittelklasse | über `Kaeltemittel` gewählt: Höchstvorlauf, Spreizungen, Rücklaufgrenze, bei R744 Bezugsrücklauf und Abwertung; leer = allgemeine Vorgaben | wie die Zeilen oben | 2.4, 2.11; Umsetzungskonzept U‑2 |
| Bivalenzpunkt parallel/monoenergetisch (Orientierung) | −5 °C | −10 … +2 °C | [Q9], [Q23] (sek.) |
| Hybrid-Mindestanteil § 43 GModG / BEG | 30 % parallel, 40 % alternativ | fest | [Q27], [Q32] |

Die Exponenten und Auslegungspaare sind die AK1-Vorgaben der Gruppe „Wärmeübergabe"; sie werden nicht verändert,
nur in der Herleitungszeile genannt.

**6.4 Was unverändert bleibt.** Gebäude- und Zonendialog (Gruppe „Wärmeübergabe", Konzept Anlagenkopplung 9.1),
Simulationskonfiguration und Kaskade, Kesseldialog. **Hausregeln:** nur Hausbausteine (`Formulargruppe`,
`Zahlenfeld`, `Schalter`, `Auswahlfeld`, `Herleitungszeile`, `Warnbanner`), Zielgröße 44 px, Farben nur über die
Token von `epos-ui.css`, Texte in `MyResource.Resource.*` beider Sprachen mit `designer_neu.py`, Datenbankseite im
Controller des Kerns, die Rechnung der Herleitungszeile in einer Klasse des Kerns (dieselbe wie 4.3/4.5), die Hülle
in `EPOS.UI.Daten`; KI-Sicht (`WaermepumpeAnlageKiSicht.cs`) um die neuen Felder und die Herleitungszeile.

## 7 Ergebnisse und Bericht

**7.1 Ergebnisreiter Wärmepumpe** (`EPOS.UI/Seiten/Simulation/WaermepumpeReiter.razor`): eine Kachelzeile
„Betriebsbereiche" — Stunden und Wärme der Wärmepumpe je Bereich (allein, mit Kessel parallel, Vorwärmung, nur
Kessel), dazu die beiden berechneten Bivalenzpunkte und die Übergabegrenze in kW; die Stunden mit unterschrittener
Spreizung und der Stundenzähler „Rücklaufgrenze" (jede Wärmepumpe, abgeleitete oder eingetragene Grenze) als leise
Zeile, nur wenn > 0. Die Stunden der Rücklaufstufe „Vorwärmer" des Kessels stehen im Protokollhinweis
`SIMENG_KESSEL_BRENNWERT_BETRIEB` (`Ruecklaufstufe.Vorwaermer`); eine Ergebnisspalte am Kessel erst, wenn der
Bericht sie verlangt (Umsetzungskonzept U‑4).

**7.2 Bivalenzdiagramm.** Neue Methode `ChartRenderer.Bivalenzdiagramm` mit `BivalenzdiagrammModell` (Muster
`VorlaufRuecklauf`/`VorlaufRuecklaufModell`): x Außentemperatur, y Leistung; Heizlast (Gerade aus Φ_N und
Auslegungspunkt), Kennfeldleistung bei Höchstvorlauf, Übergabegrenze bei Höchstvorlauf, Leistung der Wärmepumpe
nach Betriebsbereich, senkrechte Marken der beiden Bivalenzpunkte und des eingegebenen Abschaltpunkts, dazu
optional die Stundenpunkte des Laufs (Leistung der Wärmepumpe je Stunde über der Außentemperatur). Farben aus der
Diagrammfarbtafel, keine neue Farbe. **ChartProben:** synthetische Reihe, Maße, Farben, Determinismus, neue
Zeilen in der jüngsten `Messlatte_*.sha256` (Hash-Vergleich auf ubuntu). **Bericht:** Bildschlüssel
`stand.bild.wp_bivalenz` im Vorlagenfeldkatalog, Katalogfassung + 1, Platzhalteranzeige „kein Bivalenzdiagramm —
Kopplung aus" ohne Übergabedaten; die Standardvorlage und der Kurzbericht über `Werkzeuge/Berichtsvorlage` neu
bauen (Wachen `BerichtsvorlageDateiWacheTests`, `AuslieferungsvorlagenWacheTests`).

**7.3 Tafel im Bericht.** „Bivalenz und Übergabe": Höchstvorlauf, Übergabegrenze (kW, % der Heizlast, Rücklauf),
Bivalenzpunkte (berechnet, eingegeben, maßgebend), Stunden und Wärme je Bereich, Anteil nach § 43 GModG.
**Hinweiszeilen** im Bericht und im Reiter: „Stundenmodell: Speichermasse des Estrichs, Ventil- und Reglerdynamik
und die Abtauung sind nicht abgebildet. Herstellerwerte zu Spreizung, Volumenstrom und Einsatzgrenze haben Vorrang
vor den Vorgaben." Prüfhinweise ohne Feld (UB‑Q10): Rücklauf unter 15 °C bei Außentemperatur unter 10 °C
(Anfahrgrenze der Luft/Wasser-Wärmepumpe, der Zusatzheizer deckt); NT- oder Biomassekessel mit Mindestrücklauf
55–65 °C (Rücklaufanhebung vorausgesetzt).

**7.4 KI-Sicht und Export.** Die Ergebnisspalten gehen in die KI-Sicht des Reiters und in den Ergebnisexport
(gleiche Schlüssel wie die Spalten); die Herleitungszeile der Konfiguration in `WaermepumpeAnlageKiSicht`.

## 8 Prüfungen und Nachweis

**8.1 Rechenproben ohne Datenbank** (`EPOS.Kern.Tests`, neue Klasse `UebergabegrenzeTests`):

| Probe | Erwartung |
|---|---|
| Gleichgewicht 75/60/20, n = 1,3, 10 kW, θ_WP,max = 55 °C | 5,680 kW, Rücklauf 46,48 °C (Handrechnung Abschnitt 1, Toleranz 1e‑3) |
| dieselbe Probe bei 50/60/70 °C | 4,681 / 6,714 / 8,877 kW |
| Exponent 1,0 (geschlossen lösbar), 1,1, 1,4 | Vergleich mit geschlossener Form bzw. Bisektion |
| θ_WP,max ≥ Auslegungsvorlauf | keine Begrenzung (Φ_UE,max ≥ Φ_N) |
| θ_WP,max → θ_i | Φ_UE,max → 0, Rücklauf → Raumtemperatur, kein NaN |
| Flächenheizung 35/28, θ_WP,max = 55 °C | keine Begrenzung |
| Höchstspreizung direkt (Heizkreis 90/70 an WP σ_max 10 K) | Φ = W_H · 10 K, Grund `SPREIZUNG_MAX` |
| Vorwärmanteil bei 0 °C im Zahlenbeispiel | 4,40 kW, Anteil 70,4 %; bei θ_R > θ_WP,max − σ_min null |
| Bivalenzpunkte des Zahlenbeispiels | +1,83 °C / −3,55 °C / Kennfeld −3,84 °C |
| Weiche mit ṁ_WP < ṁ_HK | gemischter Vorlauf nach 2.8, Leistung kleiner als direkt |
| alternativ, Last über Φ_WP,grenz | Wärmepumpe 0, Kessel alles |
| Wärmepumpe ohne neue Felder, `Vorwaermbetrieb` 0, θ_V,soll ≤ θ_WP,max | byte-gleich zum Bestand |
| abgeleitete Rücklaufgrenze unterkritisch (θ_WP,max 55 °C, σ_min 3 K, Feld leer) | θ_R,grenz 52 °C; Rücklauf 52,5 °C → 0, Grund `RUECKLAUF_MAX`; Feld 50 °C → Grenze 50 °C |
| R744-Abwertung (k 2,5 %/K, θ_R,bez 30 °C, Grenze 40 °C) | Rücklauf 34 °C → Leistung und COP × 0,90, Strom gleich; 40,1 °C → 0; k = 0 mit 35 °C → harte Grenze |
| Mindestvolumenstrom als Höchstspreizung, Überströmventil (ṁ_min 60 %, σ_A 5 K, σ_min 3 K) | σ_max,eff = min(σ_max, Φ/(ṁ_min·c_p)); Φ_HK < 0,36·Φ_N taktet; Rücklauf der Wärmepumpe gemischt nach 2.8 |
| Rücklaufstufe „Vorwärmer" | Brennwertkessel in B3 rechnet `EtaBrennwert` am Vorlauf der Wärmepumpe; ohne Vorwärmbetrieb Stufenkette unverändert |
| BHKW-Rücklaufgrenze (Option) | Rücklauf 71 °C bei Grenze 70 °C → BHKW 0; Feld leer → byte-gleich |

**8.2 Datenbankfälle und Wachen.** Schemaschritt mit Wiederholprobe; Dialogtests (bunit) für Sichtbarkeit,
Schnellwahl und Herleitungszeile; Wache des Referenzprojekts (`UebergabegrenzeReferenzprojektWacheTests`) hält
Übergabe, `Vorlauf_Max`, `Vorwaermbetrieb` und die Bereichsstunden.

**8.3 Referenzlauf.** Lauf vor und nach dem Rechenweg; die Abweichungen von 1047/1054/1056/1058 werden je Projekt
erklärt (Stunden B4 statt Kennfeldleistung, Stunden der abgeleiteten Rücklaufgrenze, Vorwärmer-Stufe des Kessels). Neue Basis mit dem neuen Referenzprojekt; ob es in die CI-Auswahl
kommt, entscheidet UB‑Q7.

**8.4 Wiki und Logbuch.** Quellen `Projekte/Wiki/Programm Dokumentation - Wärmepumpe.wiki` (Gruppe „Bivalenz und
Übergabe", Bivalenzdiagramm), `Projekte/Wiki/Grundlagen - Wärmepumpe.wiki` (Übergabegrenze, Vorwärmbetrieb,
Spreizungen; die vorhandenen Grafiken `Dateien/Einbindung_Waermepumpe_bivalent.svg` und
`Dateien/Jahresdauerlinie_bivalent.svg` bleiben und bekommen eine Reihenschaltung dazu), die Seiten zur
Anlagenkopplung und zur Wärmeübergabe des Gebäudes (Verweis). Logbuch-Entwurf, je ein Satz: „Die Wärmepumpe rechnet
mit der Leistung der Heizflächen bei ihrem Höchstvorlauf; die beiden Bivalenzpunkte werden berechnet und
angezeigt." · „Neu ist der Vorwärmbetrieb: Wärmepumpe und Kessel in Reihe." · „Neues Berichtsbild
Bivalenzdiagramm." Gegenlesen mit dem Muster aus `CLAUDE.md`; keine Hersteller- oder Produktdaten.

## 9 Etappen

| Etappe | Inhalt | Abnahme | Aufwand |
|---|---|---|---|
| **UB‑E1** Übergabegrenze und Herleitungszeile | Klasse im Kern (4.2, 4.3, Bivalenzpunkte 4.5 statisch), Herleitungszeile und Lesewerte in der Konfiguration, Schnellwahl Höchstvorlauf; **keine Rechenwirkung** | Rechenproben 8.1 (Gleichgewicht, Grenzfälle); Basis byte-gleich; Windows-Schale kompiliert | 3–4 PT |
| **UB‑E2** Betriebsbereiche, Vorwärmbetrieb, Bivalenzpunkte | Schemaschritt (5.1), B0–B4 in Kaskadenstunde und `Angebot(h, V)`, Verfügbarkeitsgründe, Ergebnisspalten, Rücklaufstufe „Vorwärmer" der Kesselkennlinie, Gruppe „Bivalenz und Übergabe", Referenzprojekt, Einfrierregel, neue Basis | Rechenproben (mit Vorwärmer-Stufe), Referenzvergleich mit Erklärung je Projekt, SQL-Dialekt, Wache | 5–7 PT |
| **UB‑E3** ΔT-Restriktionen und Einbindung | Spreizungen, Mindestvolumenstrom als Höchstspreizung, Überströmventil, Rücklaufübergabe an die Wärmepumpe (`Heizkreisruecklauf`), `Ruecklauf_Max` mit abgeleiteter Grenze, `Ruecklauf_Bezug`, `Ruecklauf_Abwertung_ProzentJeK`, BHKW-Option (UB‑Q9), Einbindung direkt/Weiche (Puffer als Näherung), Stammblatt des Katalogs, Katalogabgleich und Projektpaket | Rechenproben Höchst-/Mindestspreizung, Weiche, Überströmventil, abgeleitete Grenze, R744-Abwertung, BHKW; Basis neu, wenn die abgeleitete Grenze in einem gekoppelten Referenzprojekt Stunden zählt oder das Referenzprojekt Werte trägt | 4–6 PT |
| **UB‑E4** Bivalenzdiagramm und Bericht | `ChartRenderer.Bivalenzdiagramm`, ChartProben-Messlatte, Kacheln im Reiter, Berichtsbild und Tafel, Katalogfassung, Vorlagen neu bauen, KI-Sicht, Export | ChartProben grün mit neuer Messlatte, Vorlagenwachen grün | 3–4 PT |
| **UB‑E5** Wiki und Logbuch | Wiki-Quellen, Logbuch-Entwurf, Konzept „wie gebaut", dann nach `ueberholt/` | Link-Wache, Wiki-Gegenlesemuster, `WikiProduktdatenWacheTests` | 1–2 PT |

Summe **16–23 PT**. **Abhängigkeit zu AK3:** UB‑E2 setzt die AK3-Basis (W5, R42) und AK3‑I voraus — die
Kennfeldleistung bei θ_WP,max ist die interpolierte; sie greift in `Angebot(h, V)` ein und sollte deshalb **nach**
AK3‑W6 und der laufenden Kältewelle AK3‑K (Basis R43) gebaut werden, damit keine zwei Basiswechsel ineinanderlaufen.
UB‑E1 hat keine Rechenwirkung und kann sofort beginnen.

## 10 Fragen an den Anwender

| Kennung | Gegenstand | Optionen | Empfehlung | Entscheid |
|---|---|---|---|---|
| **UB‑Q1** | Vorwärmbetrieb | a Reihe WP → Kessel als Schalter je Anlage · b nur parallel/teilparallel (über Höchstvorlauf WP aus) · c zusätzlich Mischung paralleler Erzeuger am gemeinsamen Vorlauf | **a** — anerkannte Grundschaltung [Q7], [Q33], geschlossen rechenbar; c ist hydraulisch unscharf und bleibt benannt abgelehnt | |
| **UB‑Q2** | Ort der Gerätegrenzen | a Katalog `Tab_WP(_STAMM)` mit Normvorgabe bei NULL · b Projekt `Tab_Energieanlagen` | **a** — Gerätewert, einmal gepflegt, VDI‑3805-fähig | |
| **UB‑Q3** | Rücklaufgrenze R744 (neu gefasst in Fassung 2) | a harte Grenze 35 °C · b Abwertung 2,5 %/K ab Bezugsrücklauf 30 °C bis harte Grenze 40 °C · c später | **b** — gemessen −1,8 %/K [Q42], ohne Kennfeld über dem Rücklauf auskommend; a bleibt mit k = 0 wählbar | |
| **UB‑Q4** | vorhandener Abschaltpunkt | a bleibt als Deckel, berechnete Punkte zusätzlich, maßgebend der wärmere · b durch die berechneten Punkte ersetzen · c nur anzeigen | **a** — kein Bestandsprojekt verliert seine Eingabe | |
| **UB‑Q5** | Einbindungsmodell | a direkt und Weiche jetzt, Puffer als benannte Näherung · b nur direkt · c alle drei mit Mischung am Puffer | **a** — die Weiche ist die häufigste Bestandsschaltung; die Puffermischung gehört in die Pufferstufe | |
| **UB‑Q6** | Vorgabewerte | a Tafel 6.3 übernehmen, (n. e.)/(sek.)-Werte vor den Hilfetexten gegenlesen · b nur belegte Werte, übrige leer | **a** | |
| **UB‑Q7** | Bericht und Bild | a Bild, Tafel und Kacheln (UB‑E4) · b nur Tafel und Kacheln · c später | **a** — das Diagramm ist die Aussage, die der Planer dem Kunden zeigt | |
| **UB‑Q8** | Referenzprojekt und Zuständigkeit | a Kopie von 1056 (Fahrplan zurückgesetzt), in die CI-Auswahl, gebaut von der Sitzung Gebäudesimulation im Zug von AK3 nach AK3‑K · b ohne CI · c eigene Sitzung | **a** — dieselbe Naht (`Angebot(h, V)`, Schritt H) wie AK3 | |
| **UB‑Q9** | Rücklaufgrenze am BHKW | a ja, `Ruecklauf_Max` am BHKW, Vorgabe 70 °C, in UB‑E3 · b nein | **a** — harte Abschaltung des Motorkühlkreises [Q63], derselbe Mechanismus wie an der Wärmepumpe | |
| **UB‑Q10** | Mindestrücklauf Biomasse-/NT-Kessel, Anfahrgrenze der Wärmepumpe | a Prüfhinweis ohne Feld · b Feld | **a** — die Rücklaufanhebung sichert ihn in jeder Anlage, die Stundenbilanz ändert sich nicht | |
| **UB‑Q11** | Rücklaufstufe „Vorwärmer" der Kesselkennlinie | a ja, in UB‑E2 · b später | **a** — Pflicht für den Brennwertnutzen im Vorwärmbetrieb | |

Zu UB‑Q2 und UB‑Q6: Die Vorgaben gelten je Kältemittelklasse aus dem Gerätefeld `Kaeltemittel` (Umsetzungskonzept
U‑2, entschieden 08.10.2026); die Fragen selbst bleiben offen.

**Technische Festlegungen zur Kenntnis (Widerspruch möglich):** arithmetisches Mittel wie AK1; konstanter
Massenstrom aus dem Auslegungspunkt; Herleitungszeile und Diagramm mit lastlinearer Heizlast und
Auslegungsraumtemperatur, die Stundenrechnung mit der Raumtemperatur der Stunde; σ_min-Unterschreitung im
Direktbetrieb ist Takten, keine Abschaltung; Heizstab wirkt in B3 wie ein Kessel in Reihe; die Rücklaufgrenze jeder
Wärmepumpe ist min(`Ruecklauf_Max`, θ_WP,max − σ_min).

**Risiken.** Ergebnisänderung bei vier Referenzprojekten (erklärbar, aber Basiswechsel); Rückkopplung von θ_R in
der AK3-Iteration (monoton, Prüforakel erweitern); Bestandsprojekte mit `Vorlauf_Max` als Fahrplankniff (1056)
ändern ihre Lesart nicht, wohl aber ihr Ergebnis, wenn ein Kessel einen höheren Vorlauf anbietet.

## 11 Quellen

[Q1]–[Q41] abgerufen am 07.10.2026, [Q42]–[Q67] (Fassung 2) am 08.10.2026; Kurzfassung der Recherchen.

1. [Q1] Wolff, D.; Jagnow, K.: Heizflächenauslegung bei Heizkörperheizungen, Überarbeitung Recknagel/Sprenger/Schramek (2007). http://www.bosy-online.de/hydraulischer_abgleich/heizflaechenauslegung_recknagel.pdf
2. [Q2] Paschotta, R.: Heizkörperexponent. RP-Energie-Lexikon, 04.05.2025. https://www.energie-lexikon.info/heizkoerperexponent.html
3. [Q3] Dott, R. u. a.: Wärmepumpen – Planung, Optimierung, Betrieb, Wartung. 5. Aufl., BFE/EnergieSchweiz, Zürich 2018. https://www.fws.ch/wp-content/uploads/2018/12/Buch_WP_Web_2018.pdf
4. [Q4] Gritzki, R. u. a.: Can we still trust in EN 442? Part 2. REHVA Journal 04/2021. https://www.rehva.eu/rehva-journal/chapter/can-we-still-trust-in-en-442-new-operating-definitions-for-radiators-part-2-model-based-analysis-and-results
5. [Q5] Handbuch Projektierung von Fußbodenheizungen – Planungsgrundlagen DIN EN 1264, 12/2008. https://assets.danfoss.com/documents/latest/100638/AG000086464394de-000101.pdf
6. [Q6] BDH: Informationsblatt Nr. 37 Teil 1 – Hydraulische Wärmeübergabesysteme mit Wärmepumpe, 10/2025. https://www.bdh-industrie.de/fileadmin/user_upload/Downloads/Infoblaetter/Infoblatt_Nr_37-1_Juli_2025_Hydraulische_Waermeuebergabe_mit_Waermepumpe-Grundlagen.pdf
7. [Q7] BDH: Informationsblatt Nr. 57 – Bivalente Wärmepumpen-Systeme, 03/2019. https://www.bdh-industrie.de/fileadmin/user_upload/Downloads/Infoblaetter/Infoblatt_Nr_57_Maerz_2019_Bivalente_Waermepumpensysteme.pdf
8. [Q8] BWP: Leitfaden Hydraulik, 07/2016. https://www.waermepumpe.de/uploads/media/BWP_LF_Hydraulik_final_web.pdf
9. [Q9] BWP: Leitfaden Wärmepumpendimensionierung, Stand 09-2026. https://www.waermepumpe.de/fileadmin/user_upload/waermepumpe/07_Publikationen/BWP_LF_WPDimensionierung.pdf
10. [Q10] Klein, B.: The basis for comparison – performance rates for heat pumps. REHVA Journal 10/2012, S. 15–18. https://www.rehva.eu/fileadmin/hvac-dictio/05-2012/p15-18_klein.pdf
11. [Q11] EHPA: Testing Regulation – Air/Water Heat Pumps, Version 2.4a, 07.06.2021. https://www.ehpa.org/wp-content/uploads/2022/07/EHPA_TestReg_AW_HP_V2.4a_20210607_.pdf
12. [Q12] Eschmann, M. (NTB Buchs): Qualitätsüberwachung von Kleinwärmepumpen 2018, BFE. https://pubdb.bfe.admin.ch/de/publication/download/10077
13. [Q13] WPZ Buchs (OST): Prüfresultate Luft/Wasser-Wärmepumpen nach EN 14511 und EN 14825, 29.05.2026; eigene Medianauswertung. https://www.ost.ch/fileadmin/dateiliste/3_forschung_dienstleistung/institute/ies/wpz/luft-wasser-waermepumpen/pruefresultate_lw_29052026.pdf
14. [Q14] EN 14511-2:2022, Leseprobe. https://cdn.standards.iteh.ai/samples/70159/6e04504dca5f4a59a4b19731abd322bf/SIST-EN-14511-2-2022.pdf
15. [Q15] Rasmussen, P. (DTI): Calculation of SCOP for heat pumps according to EN 14825, 2011. https://www.chiltrix.com/documents/App-B-Teknical-SCOP-NEF%20-EN14825.pdf
16. [Q16] Miara, M. (Fraunhofer ISE): Are heat pumps able to deliver sufficiently high temperatures in the heating circuit? 17.02.2021. https://blog.innovation4e.de/en/2021/02/17/are-heat-pumps-able-to-deliver-sufficiently-high-temperatures-in-the-heating-circuit/
17. [Q17] Günther, D. u. a. (Fraunhofer ISE): Effizienzanalyse von Wärmepumpen im EFH-Bestand, 2020. https://www.ise.fraunhofer.de/content/dam/ise/de/documents/publications/conference-paper/091_Fraunhofer-ISE_Chillventa-2020_1015_EFFIZIENZANALYSE%20VON%20W%C3%84RMEPUMPEN%20IM%20BESTAND.pdf
18. [Q18] UBA: Hauswärmepumpen mit natürlichen Kältemitteln, Texte 82/2022. https://www.umweltbundesamt.de/system/files/medien/479/publikationen/texte_82-2022_hauswaermepumpen_mit_natuerlichen_kaeltemitteln.pdf
19. [Q19] EnergieSchweiz/BFE: Kältemittel-Fibel Klimakälte, 09/2025. https://pubdb.bfe.admin.ch/de/publication/download/8710
20. [Q20] Keller, L.: Großwärmepumpen mit R744. KKA 03/2024. https://www.kka-online.info/artikel/grosswaermepumpen-mit-r744-4108042.html
21. [Q21] CoolProp: R1234ze(E) – Fluid Properties. https://coolprop.org/fluid_properties/fluids/R1234ze(E).html
22. [Q22] SI-Informationen: VDI 4650 – Aktualisierte Berechnung der JAZ (VDI 4650 Blatt 1:2024-02), 14.03.2024. https://www.si-shk.de/vdi-4650-aktualisierte-berechnung-der-jaz-212649/
23. [Q23] energie-experten.org: Bivalenzpunkt (Tabelle nach DIN V 4701-10/VDI 4650), 01.07.2026 (sek.). https://www.energie-experten.org/heizung/waermepumpe/planung/bivalenzpunkt
24. [Q24] Hönig, C.: Wärmepumpen-Förderung – Wie man die Jahresarbeitszahl erhöht. TGA Fachplaner 11/2009. https://www.wp-opt.de/tga-2009-11.pdf
25. [Q25] DIN Media: E DIN EN 15450:2026-02, Planung von Heizungsanlagen mit Wärmepumpen. https://www.dinmedia.de/de/norm-entwurf/din-en-15450/398823989
26. [Q26] dena: Praxisleitfaden für Wärmepumpen in Mehrfamilienhäusern, 03/2024. https://www.gebaeudeforum.de/fileadmin/gebaeudeforum/Downloads/Leitfaden-Handbuch/Leitfaden_Waermepumpen-in-Mehrfamilienhaeusern.pdf
27. [Q27] § 43 GModG. https://www.gesetze-im-internet.de/geg/__43.html
28. [Q28] WERK.E: Bio-Treppe Alternativen nach § 43 GModG, 08/2026 (sek.). https://werk-e.de/bio-treppe-alternativen/ · ENERGIE-FACHBERATER: GModG – diese Heizungen sind erlaubt, 29.07.2026 (sek.). https://www.energie-fachberater.de/beratung-foerdermittel/gesetzliche-vorgaben/gebaeudemodernisierungsgesetz-gmodg/gmodg-diese-heizungen-sind-erlaubt.php
29. [Q29] Ingenieurkammer-Bau: Gebäudemodernisierungsgesetz im Bundesgesetzblatt (BGBl. 2026 I Nr. 226). https://ingkh.de/ingkh/aktuelles/news/GModG-Bundesgesetzblatt.php
30. [Q30] buzer.de: § 71h GEG (aufgehoben zum 29.07.2026). https://www.buzer.de/71h_GEG.htm
31. [Q31] KfW: BEG – Infoblatt zu den förderfähigen Maßnahmen und Leistungen (Sanieren), Version 11.0, 16.09.2026. https://www.kfw.de/PDF/Download-Center/F%C3%B6rderprogramme-(Inlandsf%C3%B6rderung)/PDF-Dokumente/6000004863_Infoblatt_BEG_F%C3%B6rderf%C3%A4hige_Ma%C3%9Fnahmen.pdf
32. [Q32] KfW: BEG – Liste der technischen FAQ, Einzelmaßnahmen, Version 7.0, 01.06.2026. https://www.kfw.de/PDF/Download-Center/F%C3%B6rderprogramme-(Inlandsf%C3%B6rderung)/PDF-Dokumente/6000004864_BEG_TFAQ_EM.pdf
33. [Q33] Lotz, D.: Parallele oder serielle Einbindung? IKZ-Haustechnik, 22.12.2016. https://www.ikz.de/detail/news/detail/parallele-oder-serielle-einbindung/
34. [Q34] Wikipedia: Heizgrenze. https://de.wikipedia.org/wiki/Heizgrenze
35. [Q35] BBS Oldenburg: Heizkurve (Lernmaterial). https://bbs-old.de/anlagenmechaniker/online/heizkurve2/index.html
36. [Q36] baulinks.de: Richtlinie VDI 4645 E, 2026. https://www.baulinks.de/webplugin/2026/0661.php4
37. [Q37] energie-experten.org: Pufferspeicher für Wärmepumpen (Werte nach VDI 4645), 01.07.2026 (sek.). https://www.energie-experten.org/heizung/waermepumpe/waermepumpenheizung/pufferspeicher
38. [Q38] effiziente-waermepumpe.ch: Temperaturspreizung und COP, 30.11.2009 (sek.). https://www.effiziente-waermepumpe.ch/2009/11/30/temperaturspreizung-und-cop/
39. [Q39] Planungsanleitung Grundlagen für Wärmepumpen, 05/2015 (nur herstellerübergreifende Aussagen). https://www.meinhausshop.de/media/pdf/meinhausshop-grundlagen-waermepumpen-de.pdf
40. [Q40] autarc: Welche Wärmepumpen bekommen ab 2026 noch Förderung? 05.01.2026 (sek.). https://www.autarc.energy/wissen/schallanforderungen-warmepumpe-forderung
41. [Q41] GÖRG: Gebäudemodernisierungsgesetz (GModG) schafft GEG ab, 17.07.2026. https://www.goerg.de/de/aktuelles/veroeffentlichungen/17-07-2026/gebaeudemodernisierungsgesetz-gmodg-schafft-geg-ab-was-sich-aendert-und-was-bleibt
42. [Q42] Stene, J.: Residential CO2 Heat Pump System for Combined Space Heating and Hot Water Heating. Dissertation, NTNU Trondheim, EPT-Report 2004:6, 02/2004. https://www.osti.gov/etdeweb/servlets/purl/20559406
43. [Q43] IIAR / Natural Refrigeration Review: CO2 Heat Pumps: Main Differences from Refrigeration Systems and Real-World Applications (Rangelov, Karampour). Fachaufsatz, 2026 (sek., Artikelseite). https://naturalrefrigerationreview.com/co2-heat-pumps-main-differences-from-refrigeration-systems-and-real-world-applications/
44. [Q44] Mitsubishi Electric: Data Book QAHV-N560YA-HPB(-BS) Hot Water Heat Pump (MEE15K075). Herstellerunterlage, o. J. https://ecoinnovation.lv/wp-content/uploads/2023/02/GAISS_U%CC%84DENS_SILTUMSU%CC%84KNIS_QAHV_DataBook.pdf
45. [Q45] Mitsubishi Electric UK: High temperature CO2 heat pump increases hot water efficiency. Pressemitteilung, 10.09.2020, https://les.mitsubishielectric.co.uk/latest-news/high-temperature-co2-heat-pump-increases-hot-water-efficiency-2 · Eintrittsbereich 5–63 °C nach Produktseite Mitsubishi Electric Australia (sek.). https://www.mitsubishielectric.com.au/product/qahv-n560ya-hpb-hot-water-heat-pump/
46. [Q46] Mayekawa: Unimo-AW – Air Source CO2 Heat Pump. Datenblatt, o. J. https://americas.mayekawa.com/mna/downloads/pdf/Heat%20Pumps/Unimo-AW.pdf
47. [Q47] Dimplex: Wärmepumpenmanager – Bedienungsanleitung für den Installateur, FD 9709. Herstellerunterlage, o. J. https://www.dimplex.eu/sites/g/files/emiian586/files/media_import/medias/docus/7/Dimplex_WPM_Bedienungsanleitung2_fd9709_de.pdf
48. [Q48] alpha innotec: Betriebsanleitung Luxtronik 2.0/2.1 (Fachhandwerker). Herstellerunterlage, o. J. https://www.alpha-innotec.ch/fileadmin/content/downloads/Lux_Fachhandwerker_de.pdf
49. [Q49] Copeland: Copeland scroll compressors for R290 comfort applications (DSC167). Produktbroschüre, o. J. https://media.copeland.com/c35cd3b3-ac35-40ec-9fc6-b2ab00acbba9/DSC167-Comfort-Propane-Scroll-Compressors-EN.pdf
50. [Q50] Robur: Abso Pro design manual, Section B01 – Aerothermic heat pump GAHP A, Rev. B, https://www.robur.com/hubfs/doc/D-FSC003EN_B_22MCLSDC016_ADM_Section_B01_GAHP_A_EN.pdf · GAHP-A Installation, Use and Maintenance Manual. Herstellerunterlagen, o. J. https://www.robur.com/hubfs/doc/D-LBR369_H_24MCLSVI015_GAHP-A_USA_CSA_60Hz_Tinlet_EU.pdf
51. [Q51] Daikin: Installation manual Daikin Altherma low temperature monobloc EBLQ/EDLQ05+07CAV3, 4P403578-1F. Installationsanleitung, 06/2018. https://www.daikin.co.uk/content/dam/document-library/installation-manuals/heat/air-to-water-heat-pump-low-temperature/eblq-cv3/EBLQ05-07CV3-EDLQ05-07CV3_4PEN403578-1F_Installation%20Manual_English.pdf
52. [Q52] Viessmann: Vitocal 250-A PRO AWO-AC-AF 251.A40 – Datenblatt/Planungsanleitung (sek., Händlerspiegelung). https://www.heizprofishop.at/Datenbl%C3%A4tter/Viessmann/250A/250%20pro%20datenblatt.pdf
53. [Q53] Stiebel Eltron: WPMW II / WPMS II – Wärmepumpen-Manager, Datenblatt (Funktion „Rücklauftemperatur-MAX") (sek., Händlerspiegelung). https://www.eibmarkt.com/isroot/eibmarkt/Files/Datenblatt/WPMW%20II.pdf
54. [Q54] Mitsubishi Electric: Ecodan R32 – Single Phase Spec Sheet, 07/2021. https://static1.squarespace.com/static/5894a3b0be659481ff6f152c/t/60f031af7fedc2490c0ccfc1/1626354096029/Mitsubishi+Ecodan+Single+Phase+Spec+Sheet+Jul+2021.pdf
55. [Q55] Russ, C.; Miara, M. u. a. (Fraunhofer ISE): Feldmessung Wärmepumpen im Gebäudebestand – Kurzfassung zum Abschlussbericht, 08/2010. https://wp-monitoring.ise.fraunhofer.de/wp-im-gebaeudebestand/download/WP_im_Gebaeudebestand_Kurzfassung.pdf
56. [Q56] Prinzing, M. u. a. (OST): Vorschlag zur Einbindung leistungsvariabler Wärmepumpen in WPesti. Wegleitung im Auftrag von EnergieSchweiz, 18.12.2020. https://pubdb.bfe.admin.ch/de/publication/download/10357
57. [Q57] Wikipedia: Rauchgaskondensation. https://de.wikipedia.org/wiki/Rauchgaskondensation
58. [Q58] Böhm, G.: Wie effektiv sind Brennwertkessel? SBZ (Tabelle Nutzungsgrad über Rücklauftemperatur, über Suchindex-Auszug). https://www.sbz-online.de/sites/default/files/ulmer/de-sbz/document/file_186069.pdf
59. [Q59] Paschotta, R.: Rücklaufanhebung. RP-Energie-Lexikon. https://www.energie-lexikon.info/ruecklaufanhebung.html
60. [Q60] bosy-online.de: Rücklauftemperaturanhebung für Holz- und Pelletkessel (sek., Suchindex-Auszug). http://www.bosy-online.de/Ruecklauftemperaturanhebung.htm
61. [Q61] Kesselheld: Rücklaufanhebung für Holzkessel – Möglichkeiten und Vorgehen (sek.). https://www.kesselheld.de/ruecklaufanhebung-fuer-holzkessel/
62. [Q62] DIN EN 303-5:2021-11, Heizkessel – Teil 5 (Mindestkesseleintrittstemperatur paraphrasiert nach Sekundärquelle). https://webstore.ansi.org/standards/din/dinen3032021de · SBZ-Monteur: Auslegung eines Pufferspeichers für Festbrennstoffkessel (sek.). https://www.sbz-monteur.de/wie-funktioniert-eigentlich/die-auslegung-eines-pufferspeichers-fuer-festbrennstoffkessel
63. [Q63] Yados: Betriebsanleitung Blockheizkraftwerke (BHKW) mit Otto- und Dieselmotoren, Stand 09/2021. https://yados.de/medias/Betriebsanleitung-Blockheizkraftwerke-09-2021-DE.pdf
64. [Q64] SenerTec: Technisches Datenblatt Dachs 2.9; Bedien- und Einstellanleitung MSR2. Herstellerunterlagen, o. J. https://www.senertec.de/wp-content/uploads/2021/01/8098-501-000-06-Technisches-Datenblatt-Dachs-2_9.pdf
65. [Q65] Viessmann: Datenblatt Vitovalor PT2, Brennstoffzellen-Heizgerät. Herstellerunterlage, o. J. https://community.viessmann.de/viessmann/attachments/viessmann/customers-fuel-cell/1149/1/Datenblatt%20Vitovalor%20PT2.PDF
66. [Q66] AGFW FW 515, nach Technischen Anschlussbedingungen Fernwärme (Beispiel eines Versorgers, 02/2025; paraphrasiert). https://www.estw.de/de/Energie-Wasser/Hausanschluss/Fernwaerme/TAB-FW-515-M-2025-02.pdf
67. [Q67] kollektorleistung.de: Die Kollektorgleichung zur Berechnung der Wirkungsgradkennlinie. https://www.kollektorleistung.de/Kollektorgleichung.html
