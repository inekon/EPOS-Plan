# Modul, Strang und Wechselrichter — Regeln, Meldungen, Auslegung

**Stand 08.09.2026, Rev. 2.** Rev. 1 (ebenfalls 08.09.2026) beantwortete den Anwenderwunsch
„Wie sind die Regeln, damit Module und Wechselrichter zusammenpassen; welche Meldungen kann es
geben; erstelle eine Dokumentation und eine Methode zur Unterstützung der passenden Auswahl".
Rev. 2 ergänzt die **Grundlagen** (Kennwerte, STC, Temperaturgang mit Einheiten), den
**Normbezug**, ein durchgerechnetes **Zahlenbeispiel** und die **offenen Punkte**; sie
berichtigt die Meldungstexte auf den Stand nach Befund W6‑B‑4 und dient zugleich als
Hintergrundtext für die Programmhilfe im Wiki.

Grundlage ist Kapitel 4.2 des `Konzept_Wechselrichter_EPOS-Plan.md`; gerechnet wird in
`EPOS.Kern/Allgemein/Import/StrangPlausibilitaet.cs` (die Prüfung) und
`EPOS.Kern/Allgemein/Import/StrangAuslegung.cs` (die Auslegungshilfe). Der Abgleich dieser
Fassung gegen den Code und gegen die Word-Fassung „Auslegung Und Prüfung Von PV-Anlagen.docx"
steht in `K:\pv2\pv_pruefbericht.md`.

*Gliederung gegenüber Rev. 1: 1 Begriffe (erweitert) · 2 Grundlagen (neu) · 3 Normbezug (neu) ·
4 = alt 2 · 5 Rechenbeispiel (neu) · 6 = alt 3 · 7 = alt 4 · 8 = alt 5 · 9 = alt 6 ·
10 Offene Punkte (neu).*

## 1 Die Begriffe

| Begriff | Bedeutung | Wo es steht |
|---|---|---|
| **Modul** | ein Katalogmodul mit U_oc, U_mpp, I_sc, beta_OC, alpha_SC, P_STC | Administration → Photovoltaik → PV Module |
| **Strang** | n Module **in Reihe** (Spannungen addieren sich), davon p Stränge **parallel** am selben Tracker (Ströme addieren sich) | Strangtabelle im PV-Dialog |
| **MPPT** | ein MPP-Tracker des Wechselrichters; jeder hat sein Spannungsfenster und seine Stromgrenze | Spalte „MPPT" |
| **Gerät** | ein physischer Wechselrichter; Stränge mit derselben **Gerätenummer** hängen am selben Gerät, ein zweites Gerät desselben Typs bekommt Gerät 2 | Spalte „Gerät" |
| **DC/AC** | Σ P_STC der Module am Gerät ÷ AC-Nennleistung des Geräts | Ampel je Gerät |

Zwei Auslegungstemperaturen nach üblicher Praxis: **−10 °C** ist der kalte Fall (höchste
Spannung), **+70 °C Zelltemperatur** der heiße (niedrigste Spannung, höchster Strom). Für die
MPP-Spannung setzt EPOS-Plan `beta_OC` ein; der Katalog führt keinen eigenen Koeffizienten.
Das ist die Näherung im Werkzeugtipp jeder Ampelzeile.

## 2 Grundlagen: Kennwerte, STC und Temperaturgang

### 2.1 Die Kennwerte des Moduls — und ihre Einheiten

Ein Solarmodul hat eine Strom-Spannungs-Kennlinie, die von zwei Randpunkten und einem
Arbeitspunkt beschrieben wird: **Leerlaufspannung** U_oc (kein Strom fließt),
**Kurzschlussstrom** I_sc (keine Spannung liegt an) und der **MPP** — der Punkt, an dem das
Produkt aus Strom und Spannung am größten ist, beschrieben durch U_mpp und I_mpp. Die
Auslegungsprüfung braucht davon vier Werte und die zwei Temperaturkoeffizienten.

**Die Einheiten sind entscheidend und werden erfahrungsgemäß verwechselt:**

| Feld (`PhotovoltaikModel`) | Bedeutung | **Einheit** | Vorzeichen | typisch (kristallines Modul) |
|---|---|---|---|---|
| `m_U_Leerlauf` | U_oc bei STC | V | + | 30…50 V |
| `m_U_Mpp` | U_mpp bei STC | V | + | 25…42 V |
| `m_I_Kurzschluss` | I_sc bei STC | A | + | 9…14 A |
| `m_beta_OC` | Temperaturkoeffizient der **Leerlaufspannung** | **V/K** | **negativ** | −0,10…−0,15 V/K |
| `m_alpha_SC` | Temperaturkoeffizient des **Kurzschlussstroms** | **A/K** | **positiv** | +0,002…+0,006 A/K |
| `m_Leistung` | P_STC des Moduls | **W** (nicht kW) | + | 260…600 W |
| `m_T_NOCT` | Nennbetriebszelltemperatur | °C | + | 42…48 °C |
| `m_Temp_Coeff_Pmax` | gamma_PMP, Koeffizient der **Leistung** | **%/K** | negativ | −0,30…−0,45 %/K |

**Beachten:** Datenblätter geben `beta_OC` und `alpha_SC` häufig **relativ in %/K** an
(typisch −0,25…−0,35 %/K für die Spannung, +0,04…+0,06 %/K für den Strom) oder in **mV/K**
bzw. **mA/K**. EPOS-Plan führt sie **absolut** in V/K und A/K. Ein relativer Wert, ungerechnet
eingetragen, verschiebt P1 bis P4 um Größenordnungen. Der PAN-Import rechnet deshalb
ausdrücklich um (`muISC / 1000` und `muVocSpec / 1000`, `Pan/PanModule.cs`), und
`PvModulPlausibilitaet` prüft die Wertebereiche beim Import und beim Speichern
(`ALPHA_MAX = 0,05 A/K`, `BETA_MIN = −0,5 V/K`). Umrechnung aus einer Prozentangabe:
`beta_OC [V/K] = beta_rel [%/K] · U_oc [V] / 100`, `alpha_SC [A/K] = alpha_rel [%/K] · I_sc [A] / 100`.
Nur `gamma_PMP` steht **relativ** in %/K — es geht nicht in die Auslegungsprüfung ein, sondern
in die Ertragsrechnung.

Vom Gerät (`WechselrichterModel`) braucht die Prüfung: `m_P_AC_Nenn` [**kW**],
`m_P_DC_Max` [**kW**], `m_U_Mpp_Min`/`m_U_Mpp_Max` [V], `m_U_Dc_Max` [V], `m_I_Dc_Max`
[A, **je MPPT**], `m_Anzahl_Mppt` und `m_Straenge_Je_Mppt`. Bei diesen Feldern heißt
**NULL** „keine Grenze, keine Prüfung"; bei den Modulwerten gelten **0 und NULL gleichermaßen**
als „nicht gepflegt".

### 2.2 STC und die beiden Auslegungsfälle

Alle Katalogwerte gelten unter **Standard-Testbedingungen** (STC): 1 000 W/m² Einstrahlung,
**25 °C Zelltemperatur**, Luftmasse AM 1,5. Diese Bedingungen treten im Betrieb praktisch nie
auf — bei 1 000 W/m² ist ein Dachmodul deutlich wärmer als 25 °C. Für die Auslegung zählen
deshalb nicht die Datenblattwerte selbst, sondern die **Hüllkurve** der beiden Extremfälle:

* **Kalter Fall, −10 °C Zelltemperatur.** Kälte hebt die Spannung. Der ungünstigste Zustand
  ist der klare Wintermorgen vor dem Zuschalten des Wechselrichters: volle Leerlaufspannung
  bei tiefer Temperatur. Hier entscheidet sich, ob das Gerät Schaden nimmt (P1) und ob das
  MPP-Fenster oben verlassen wird (P3).
* **Heißer Fall, +70 °C Zelltemperatur.** Wärme senkt die Spannung deutlich und hebt den Strom
  leicht. Hier entscheidet sich, ob der Strang im Sommer aus dem MPP-Fenster fällt (P2) und ob
  der Trackerstrom überschritten wird (P4).

Die beiden Zahlen stehen als `T_KALT = −10.0` und `T_HEISS = 70.0` in
`StrangPlausibilitaet.cs`, die Bezugstemperatur als `T_STC = 25.0`. Sie sind **feste
Planungsannahmen der üblichen Praxis**, nicht standortabhängig gerechnet — siehe Abschnitt 3
und offener Punkt O‑3.

### 2.3 Die Temperaturformeln, die EPOS-Plan verwendet

Beide Formeln sind **linear** um den STC-Punkt herum:

```
Spannung eines Strangs bei T:   U(T) = n · [ U_STC + beta_OC · (T − 25) ]     [V]
Strom eines Strangs bei T:      I(T) =       I_sc  + alpha_SC · (T − 25)      [A]
```

`n` ist die Zahl der Module **in Reihe**. Da `beta_OC` negativ ist, steigt die Spannung im
kalten Fall (T − 25 = −35 K) und fällt im heißen (T − 25 = +45 K); da `alpha_SC` positiv ist,
steigt der Strom im heißen Fall. Der Strom eines Strangs ist von der Modulzahl in Reihe
**unabhängig** — parallele Stränge dagegen addieren ihre Ströme.

Im Code: `StrangPlausibilitaet.SpannungReihe(reihe, uStc, betaOc, temperatur)` und
`StrangPlausibilitaet.StromJeStrang(modul)`. Fehlt einer der beteiligten Werte, liefern beide
`null` — die betroffene Prüfung entfällt dann und färbt gelb.

Die Nennleistung eines Strangs ist `StrangKwp = Modulzahl · m_Leistung / 1000` [kWp], mit
`Modulzahl = n · p`.

### 2.4 Die Näherung für die MPP-Spannung

Der Modulkatalog führt **keinen** eigenen Temperaturkoeffizienten für U_mpp. P2 und P3 setzen
dafür `beta_OC` ein; die Auslegungspraxis tut dasselbe. Der Satz dazu steht im Werkzeugtipp
jeder Ampelzeile (`Befund.NaeherungMpp`).

Zur Richtung der Näherung: Bei kristallinem Silizium ist der **relative**
Temperaturkoeffizient der MPP-Spannung betragsmäßig **größer** als der der Leerlaufspannung.
Mit `beta_OC` gerechnet wird der Spannungsabfall im heißen Fall daher eher **unterschätzt** —
**P2 fällt tendenziell etwas zu günstig aus**, nicht zu streng. Im kalten Fall (P3) wirkt die
Näherung umgekehrt konservativ. Der Fehler liegt bei wenigen Prozent. Wer nahe an der unteren
Fenstergrenze auslegt, sollte deshalb einen Sicherheitsabstand einplanen und nicht auf das
letzte Volt bauen.

## 3 Normbezug — was geprüft wird und was nicht

**Was EPOS-Plan prüft, sind Datenblattgrenzen des gewählten Geräts, keine Normgrenzen.** P1 bis
P8 vergleichen Modulwerte mit den Grenzen genau dieses Wechselrichters. Das Programm ersetzt
damit keine normkonforme Planung und keine Elektrofachkraft.

Der Vollständigkeit halber das Umfeld, in dem diese Prüfungen stehen. Die folgenden Angaben
stammen aus der Word-Fassung „Auslegung Und Prüfung Von PV-Anlagen.docx" (08.09.2026);
**sie sind nicht gegen den Normtext geprüft** und deshalb hier gekennzeichnet:

* **DIN VDE 0100-712** — Errichten von Niederspannungsanlagen, Teil PV-Stromversorgungssysteme:
  Schutz gegen elektrischen Schlag, Brandschutz, Leitungsdimensionierung, DC-seitiger
  Überspannungsschutz. *(nach Docx, nicht geprüft)* **EPOS-Plan bildet davon nichts ab.**
* **Begrenzung der Systemspannung** auf 1 000 V DC im Niederspannungsbereich, 1 500 V DC bei
  Großanlagen. *(nach Docx, nicht geprüft)* **EPOS-Plan prüft keine absolute Spannungsgrenze**
  — P1 misst ausschließlich gegen `U_Dc_Max` des gewählten Geräts.
* **Sicherheitsfaktor 1,25 auf den Kurzschlussstrom** für die Auslegung von Bauteilen und
  Leitungen (Wolkenrand- und Albedoeffekte lassen die Einstrahlung kurzzeitig über 1 000 W/m²
  steigen). *(nach Docx, nicht geprüft)* **P4 enthält diesen Faktor nicht.** P4 rechnet allein
  die thermische Korrektur `alpha_SC · 45 K`; das sind rund **+2 %**, nicht +25 %. Die Docx
  behauptet an dieser Stelle, der Faktor sei „impliziter Bestandteil der thermischen
  Korrektur" — **das trifft nicht zu**. Siehe offener Punkt O‑1.
* **IEC 62548** — Auslegungsanforderungen an PV-Generatoren, unter anderem zur
  Spannungskorrektur über die Temperatur. *(nach Docx, nicht geprüft)* Die Docx liest daraus
  feste Auslegungstemperaturen von −10 °C und +70 °C ab. Nach hiesigem Kenntnisstand verlangt
  die Norm die **am Standort niedrigste zu erwartende Temperatur** und keine feste Zahl; die
  −10 °C sind eine verbreitete Planungsannahme für Deutschland. **Ungeklärt** — siehe O‑3.
* **VDE-AR-N 4105:2026-03** — Erzeugungsanlagen am Niederspannungsnetz; unter anderem
  800 VA AC und 2 000 Wp DC für Steckersolargeräte. *(nach Docx, nicht geprüft)* **EPOS-Plan
  kennt keine Anlagenklassen und keine Anmeldeschwellen.** Anzumerken ist, dass ein solches
  Gerät ein DC/AC-Verhältnis von 2,5 hätte und von P6 gelb angemahnt würde — das Band 1,0…1,5
  ist auf Dachanlagen zugeschnitten, nicht auf gedrosselte Kleinstanlagen.

Kurz: Die acht Regeln schützen den **Wechselrichter** und den **Ertrag**. Anlagenschutz,
Leitungen, Netzanschluss und Anmeldung sind nicht Gegenstand dieses Werkzeugs.

## 4 Die acht Regeln

Ein **fehlender Wert** (0 oder leer) macht die betroffene Prüfung **gelb** und nennt den Wert
(„Werte fehlen: …") — sie schlägt nicht fehl, sie kann nur nichts sagen.

| Nr. | Prüfung | Formel | Farbe bei Verletzung |
|---|---|---|---|
| **P1** | Leerlaufspannung im kalten Fall | n · [U_oc + beta_OC·(−10 − 25)] ≤ U_Dc_Max | **rot** — das Gerät kann bei Frost und Sonne Schaden nehmen |
| **P2** | MPP-Fenster im heißen Fall | n · [U_mpp + beta_OC·(70 − 25)] ≥ U_Mpp_Min | **rot** — der Strang regelt im Sommer ab |
| **P3** | MPP-Fenster im kalten Fall | n · [U_mpp + beta_OC·(−10 − 25)] ≤ U_Mpp_Max | **gelb** — das Gerät regelt an der Grenze |
| **P4** | Eingangsstrom je MPPT | Σ p · [I_sc + alpha_SC·(70 − 25)] ≤ I_Dc_Max | **rot** |
| **P5** | Strangzahl je MPPT | Σ p ≤ Stränge_je_MPPT | **gelb** |
| **P6** | DC/AC-Verhältnis | 1,0 ≤ Σ P_STC ÷ P_AC_Nenn ≤ 1,5 | **gelb**, in beide Richtungen |
| **P7** | DC-Eingangsleistung | Σ P_STC ≤ P_Dc_Max | **gelb** |
| **P8** | Modulsumme | Σ (n · p) = „Anzahl Module" der Anlage | **gelb** |

P1 bis P3 laufen **je Strang**, P4 bis P7 **je Gerät** (P4/P5 je Tracker), P8 je Anlage.
Die Farbe des Abschnitts ist die schlechteste seiner Zeilen.

**Was jede Regel bedeutet und wie man sie einhält:**

| Nr. | Warum sie da ist | Abhilfe bei Verletzung |
|---|---|---|
| **P1** | Die höchste Spannung der Anlage überhaupt. Wird `U_Dc_Max` überschritten, ist die Eingangsstufe des Geräts gefährdet, und die Herstellergarantie erlischt. Die einzige zerstörungsrelevante Prüfung — deshalb rot. | weniger Module in Reihe; oder ein Gerät mit höherer `U_Dc_Max` |
| **P2** | Fällt die Strangspannung im Sommer unter die untere Fenstergrenze, findet der Tracker keinen Arbeitspunkt: Das Gerät drosselt oder speist gar nicht mehr ein — und das genau in den ertragsstärksten Monaten. Rot, weil es den Ertragszweck der Anlage trifft. | mehr Module in Reihe; oder ein Gerät mit niedrigerer `U_Mpp_Min` |
| **P3** | Über der oberen Fenstergrenze regelt das Gerät den Arbeitspunkt künstlich ab. Nichts geht kaputt (`U_Dc_Max` hält, das prüft P1), es kostet nur Ertrag — deshalb gelb. | weniger Module in Reihe |
| **P4** | Der Strom aller parallelen Stränge eines Trackers addiert sich. Über `I_Dc_Max` werden Leiterbahnen und Klemmen thermisch überlastet. Rot. | weniger Stränge parallel am Tracker; Stränge auf mehrere Tracker verteilen (Spalte MPPT); zweites Gerät |
| **P5** | Zahl der Anschlussklemmen je Tracker. Mehr Stränge erfordern eine externe Parallelverschaltung mit eigener Absicherung — eine Bauentscheidung, keine Rechengröße. Gelb. | Stränge auf Tracker verteilen |
| **P6** | Das Inverter Load Ratio. Unter 1,0 ist das Gerät überdimensioniert (bezahlte, ungenutzte AC-Leistung); über 1,5 wird zu viel Mittagsspitze abgeschnitten (Clipping). Gelb in beide Richtungen; das Band ist eine Empfehlung, keine Grenze. | Modulzahl am Gerät ändern; anderes Gerät |
| **P7** | Absolute DC-Anschlussgrenze des Herstellers, meist wegen der Dauerwärme im Gehäuse. Überschreitung kostet oft die Garantie. Gelb, weil elektrisch bereits P1 und P4 greifen. | weniger Module am Gerät; zweites Gerät |
| **P8** | Zusicherung: Die Strangtabelle und der Anlagenwert „Anzahl Module" dürfen nicht auseinanderlaufen. Seit Entscheid Q9 leitet die Maske den Anlagenwert ohnehin aus der Tabelle ab. Gelb. | Strangtabelle oder Anlagenwert angleichen |

## 5 Das Rechenbeispiel

Die Zahlen stammen aus Anhang A des `Konzept_Wechselrichter_EPOS-Plan.md` und stehen Zahl für
Zahl im Prüfstand (`EPOS.Kern.Tests/StrangPlausibilitaetTests.cs`,
`EPOS.Kern.Tests/StrangAuslegungTests.cs`).

**Modul „Ablytek 6MN6A275"** (für das Beispiel angenommene Werte eines 60-zelligen Moduls):

```
P_STC = 275,19 W     U_oc = 38,4 V     U_mpp = 31,4 V     I_sc = 9,34 A
beta_OC = −0,118 V/K                   alpha_SC = +0,0047 A/K
```

**Wechselrichter „Muster 2500TL"**: P_AC_Nenn = 2,50 kW · 1 MPP-Tracker · MPP-Fenster
80…500 V · U_Dc_Max = 600 V · I_Dc_Max = 12,0 A.

**Schritt 1 — die Kennwerte auf die Auslegungstemperaturen bringen (je Modul):**

| Größe | Rechnung | Ergebnis |
|---|---|---|
| U_oc bei −10 °C | 38,4 + (−0,118)·(−10 − 25) = 38,4 + 4,13 | **42,53 V** |
| U_mpp bei +70 °C | 31,4 + (−0,118)·(70 − 25) = 31,4 − 5,31 | **26,09 V** |
| U_mpp bei −10 °C | 31,4 + (−0,118)·(−35) = 31,4 + 4,13 | **35,53 V** |
| I bei +70 °C | 9,34 + 0,0047·45 = 9,34 + 0,2115 | **9,5515 A** |

**Schritt 2 — die Konfiguration prüfen: 10 Module in Reihe, 1 Strang.**

| Prüfung | Rechnung | Ergebnis |
|---|---|---|
| **P1** | 10 · 42,53 = 425,30 V ≤ 600 V | **grün** |
| **P2** | 10 · 26,09 = 260,90 V ≥ 80 V | **grün** |
| **P3** | 10 · 35,53 = 355,30 V ≤ 500 V | **grün** |
| **P4** | 1 · 9,5515 = 9,5515 A ≤ 12,0 A | **grün** |
| **P6** | 10 · 275,19 W = 2,7519 kWp ÷ 2,50 kW = **1,100760** | **grün** (1,0…1,5) |
| **P7** | ohne `P_Dc_Max` keine Grenze | entfällt |
| **P8** | 10 · 1 = 10 = „Anzahl Module" | **grün** |

Die Ampelzeile lautet dann: `Strang 1: 10 Module in Reihe, 1 parallel · U_oc(−10 °C) 425 V ≤ 600 V ·
MPP 261…355 V im Fenster 80…500 V`, und der Gerätechip: `Muster 2500TL (Gerät 1): I 9,55 A ≤ 12,0 A · DC/AC 1,10`.

**Schritt 3 — die drei Gegenproben:**

| Fall | Rechnung | Ergebnis |
|---|---|---|
| 14 Module in Reihe | P1: 14 · 42,53 = 595,42 V ≤ 600 V hält; P3: 14 · 35,53 = 497,42 V ≤ 500 V hält; **P6**: 3,8527 kWp ÷ 2,50 kW = **1,541064** | **gelb** über P6 |
| 15 Module in Reihe | **P1**: 15 · 42,53 = **637,95 V > 600 V** | **rot** über P1 |
| 2 Stränge parallel | **P4**: 2 · 9,5515 = **19,103 A > 12,0 A** | **rot** über P4 |

**Schritt 4 — was die Auslegungshilfe daraus rückwärts rechnet:**

```
Module in Reihe, Untergrenze (P2):   ⌈ 80 V ÷ 26,09 V ⌉ = ⌈3,07⌉ =  4
Module in Reihe, Obergrenze (P1):    ⌊600 V ÷ 42,53 V ⌋ = ⌊14,11⌋ = 14
Module in Reihe, Obergrenze (P3):    ⌊500 V ÷ 35,53 V ⌋ = ⌊14,07⌋ = 14
                        →  „passend wären 4…14 Module in Reihe"

Stränge je Tracker (P4):             ⌊12,0 A ÷ 9,5515 A⌋ = ⌊1,26⌋ = 1
Module je Gerät (P6 unten):          ⌈1,0 · 2500 W ÷ 275,19 W⌉ = ⌈9,08⌉ = 10
Module je Gerät (P6 oben):           ⌊1,5 · 2500 W ÷ 275,19 W⌋ = ⌊13,63⌋ = 13
                        →  „passend wären 10…13 Module je Gerät"
```

Die 15 Module der zweiten Gegenprobe liegen über der genannten Obergrenze 14, die 14 Module der
ersten über der Modulgrenze 13 je Gerät — Prüfung und Hilfe sagen dasselbe, nur aus
entgegengesetzter Richtung. Mit einer DC-Eingangsgrenze von 3,0 kW drückte P7 die Obergrenze
weiter auf ⌊3000 ÷ 275,19⌋ = **10** Module je Gerät.

## 6 Die Meldungen

**Je Strang** (Zeile „Strang n: …" unter der Tabelle, Teile durch „ · " getrennt):

| Text | Bedeutung | Abhilfe |
|---|---|---|
| `10 Module in Reihe, 1 parallel` | die geprüfte Konfiguration | — |
| `U_oc(−10 °C) 537 V ≤ 600 V` | P1 eingehalten | — |
| `U_oc(−10 °C) … V > … V — der Wechselrichter kann bei Frost und Sonne Schaden nehmen` | P1 verletzt | weniger Module in Reihe oder Gerät mit höherer U_Dc_Max |
| `MPP 357…460 V im Fenster 210…500 V` | P2 und P3 eingehalten | — |
| `MPP im Sommer 357 V < 590 V — der Strang regelt ab` | P2 verletzt: im Sommer sinkt die Strangspannung unter das Fenster, das Gerät findet keinen Arbeitspunkt | mehr Module in Reihe oder Gerät mit niedrigerem U_Mpp_Min |
| `MPP im Winter … V > … V — das Gerät regelt an der Grenze` | P3 verletzt | weniger Module in Reihe |
| `Werte fehlen: …` | nicht prüfbar — siehe die Liste unten | Katalogwerte pflegen |
| `passend wären 4…14 Module in Reihe` (auch: `mindestens 4`) | **Auslegungshilfe**, steht hinter einem roten oder gelben Strangbefund | die genannte Reihe wählen |
| `keine Reihe passt zu diesem Gerät, anderes Gerät wählen` | Untergrenze aus P2 liegt über der Obergrenze aus P1/P3 | anderes Gerät |
| `Modulsumme 20 weicht von „Anzahl Module" 30 ab` | P8; die Meldung hängt an der Anlage und steht deshalb am **ersten** Strang | Strangtabelle oder Anlagenwert angleichen |

**Die einzeln benannten fehlenden Angaben** (seit Befund **W6‑B‑4**, 07.09.2026 — vorher nannte
eine Meldung Wertpaare mit „oder", obwohl die Prüfung jeden Wert einzeln kennt). Jede Angabe
erscheint **genau einmal**, auch wenn sie mehrere Prüfungen unbrauchbar macht:

`Leerlaufspannung U_OC des Moduls` · `MPP-Spannung U_MPP des Moduls` ·
`Kurzschlussstrom I_SC des Moduls` · `Temperaturkoeffizient alpha_SC des Moduls` ·
`Temperaturkoeffizient beta_OC des Moduls` · `Module in Reihe` · `der Wechselrichter` ·
`das Modul der Anlage`

Ist ein **Modul**wert darunter, folgt der **Pflegeweg als eigener Satzteil in derselben
Meldung** (nicht im Werkzeugtipp): `Pflege: Administration → Energiesysteme → Photovoltaik →
PV Module → Bearbeiten (Felder alpha_SC, beta_OC, T_NOCT) oder Neuimport aus „CEC Modules.csv"`.

**Je Gerät** (Chip im Kopf „Wechselrichter und Stränge": „Name (Gerät n): …"):

| Text | Bedeutung | Abhilfe |
|---|---|---|
| `I 13,72 A ≤ 30,0 A` | P4 eingehalten (größter Trackerstrom) | — |
| `I … A > … A am MPPT n` | P4 verletzt | weniger Stränge parallel am Tracker, Stränge auf Tracker verteilen (Spalte MPPT), zweites Gerät |
| `3 Stränge am MPPT 1, zulässig sind 2` | P5 verletzt | Stränge auf Tracker verteilen |
| `DC/AC 1,10` | P6 eingehalten | — |
| `DC/AC 0,88 liegt außerhalb 1,0…1,5` | P6 verletzt: das Gerät ist zu groß (< 1,0) oder zu klein (> 1,5) für die Module | Modulzahl am Gerät ändern oder anderes Gerät |
| `10,616 kWp über der DC-Eingangsgrenze 9,00 kW` | P7 verletzt | weniger Module am Gerät, zweites Gerät |
| `Werte fehlen: AC-Nennleistung des Geräts` | P6 nicht prüfbar | Gerätekatalog pflegen |
| `Angabe fehlt: Zahl der MPP-Tracker — gerechnet wird auf einem` | konservative Annahme | Gerätekatalog pflegen |
| `Kein Wechselrichter zugeordnet` | Strangzeile ohne Gerät | Gerät wählen |
| `passend wären 10…13 Module je Gerät` (auch: `mindestens 10`, `höchstens 13`, `kein Modulfeld passt zu diesem Gerät`) | **Auslegungshilfe** hinter P6/P7 | Aufteilung entsprechend |

## 7 So wird die Konfiguration passend

1. **Modul wählen** (Projektzeile). Prüfen, dass der Katalog U_oc, U_mpp, I_sc, beta_OC und
   alpha_SC führt — sonst bleibt alles gelb. Die Einheiten stehen in Abschnitt 2.1.
2. **Gerät wählen** („Wechselrichter aus dem Katalog"). Faustregel: P_AC ≈ P_STC der Module
   ÷ 1,1…1,3; das MPP-Fenster muss zur Modulspannung mal Reihe passen.
3. **Module in Reihe** so, dass die Strangspannung im Sommer über U_Mpp_Min und im Winter
   unter U_Mpp_Max und U_Dc_Max bleibt. Die Ampel nennt den Bereich, sobald sie rot oder
   gelb ist („passend wären …").
4. **Stränge parallel** je Tracker so, dass der Strom unter I_Dc_Max bleibt (bei den heutigen
   Halbzellenmodulen mit 13…14 A meist ein Strang je Tracker).
5. **Geräte zählen**: mehrere Stränge an einem Gerät teilen sich dessen AC-Leistung. Passt
   die Modulzahl nicht ins Band 1,0…1,5, ein **zweites Gerät desselben Typs** anlegen —
   zweite Strangzeile, Spalte „Gerät" = 2. Leer heißt 1.
6. **Tracker**: zwei Stränge an einem Gerät mit zwei MPP-Trackern bekommen MPPT 1 und 2
   (eine gemeinsame Clipping-Grenze, getrennte Stromgrenzen).
7. **Modulsumme** = „Anzahl Module" der Anlage, sonst meldet P8.

### Beispiel aus der Abnahme (Philadelphia Solar 530 W, 20 Module)

* Ein **SMA Sunny Boy 6.0** mit beiden Strängen (Gerät leer = 1): DC/AC 1,77 und 10,6 kWp über
  der DC-Grenze 9,0 kW → **zwei Geräte**: Strang 1 Gerät 1, Strang 2 Gerät 2, je 5,3 kWp,
  DC/AC 0,88 (gelb: leicht unter 1,0 — mit 12 Modulen je Strang wären es 1,06). Diese
  Aufteilung ist **von Hand** entstanden; der Knopf „Auslegung vorschlagen" liefert sie nicht,
  weil das DC/AC-Band für dieses Gerät 12…16 Module je Gerät verlangt und 20 Module sich nicht
  so aufteilen lassen (offener Punkt O‑2).
* Ein **SMA Sunny Highpower PEAK3 SHP100** für 5,3 kWp: DC/AC 0,05, und sein MPP-Fenster
  beginnt bei 590 V — zehn Module liefern im Sommer 357 V: „der Strang regelt ab". Die Hilfe
  sagt dazu „passend wären 17…18 Module in Reihe" (aus 53,7 V und 35,7 V je Modul gerechnet);
  für 20 Module geht das nicht auf, und das DC/AC-Band verlangt mindestens 189 Module
  (⌈100 kW ÷ 530,8 W⌉) — das Gerät ist für Großanlagen, `GeraeteBewerten` reiht es hinter
  allen passenden ein.
* Zehn Module in Reihe am Sunny Boy 6.0 (Fenster 210…500 V, U_dc 600 V): U_oc(−10 °C) 537 V
  ≤ 600 V, MPP 357…460 V im Fenster — **grün**.

## 8 Die Auslegungshilfe (`StrangAuslegung`)

| Methode | Antwort |
|---|---|
| `Reihe(modul, geraet)` | Bereich für „Module in Reihe" (Min aus P2, Max aus P1/P3) |
| `ParallelJeMppt(modul, geraet)` | Stränge je Tracker (P4, P5) |
| `ModuleJeGeraet(modul, geraet)` | Module je Gerät (P6-Band, P7) |
| `Vorschlagen(modul, geraet, anzahlModule)` | eine Aufteilung: Reihe, Stränge je Gerät, Gerätezahl, DC/AC — wenige Geräte zuerst, dann DC/AC nahe 1,25, dann lange Reihen |
| `GeraeteBewerten(modul, anzahlModule, katalog)` | alle Geräte des Katalogs, passende zuerst |
| `Aufteilen(vorschlag, mppts)` | derselbe Vorschlag als **Tabelle**: je Gerät und belegtem Tracker eine Zeile (Gerät, MPPT, Reihe, parallel) |
| `ReiheEmpfehlung`, `GeraetEmpfehlung` | die Sätze der Ampel |

**Der Rechenweg — dieselben Regeln, rückwärts.** Alle Grenzen werden auf **ganze Module**
gerundet, und zwar immer in die sichere Richtung: Untergrenzen aufgerundet (`⌈…⌉`),
Obergrenzen abgerundet (`⌊…⌋`).

```
Reihe.MinMpp = ⌈ U_Mpp_Min ÷ [U_mpp + beta_OC·45] ⌉            (aus P2)
Reihe.MaxUoc = ⌊ U_Dc_Max  ÷ [U_oc  + beta_OC·(−35)] ⌋          (aus P1)
Reihe.MaxMpp = ⌊ U_Mpp_Max ÷ [U_mpp + beta_OC·(−35)] ⌋          (aus P3)
Reihe.Min    = max(1, MinMpp)        Reihe.Max = min(MaxUoc, MaxMpp)

ParallelJeMppt = min( ⌊ I_Dc_Max ÷ [I_sc + alpha_SC·45] ⌋ ,  Straenge_Je_Mppt )   (P4 und P5)

ModuleJeGeraet.Min = ⌈ 1,0 · P_AC_Nenn·1000 ÷ P_STC ⌉                              (P6 unten)
ModuleJeGeraet.Max = min( ⌊ 1,5 · P_AC_Nenn·1000 ÷ P_STC ⌋ , ⌊ P_Dc_Max·1000 ÷ P_STC ⌋ )
                                                                        (P6 oben und P7)
```

Eine Grenze, deren Werte fehlen, entfällt; sind **alle** unprüfbar, rät die Hilfe nicht
(`Reihenbereich.Pruefbar = false`, der Satz bleibt leer). Liegt die Untergrenze über der
Obergrenze, gibt es keine passende Reihe — dann steht „keine Reihe passt zu diesem Gerät".

**Wie `Vorschlagen` wählt.** Es probiert alle Reihenlängen von der längsten zulässigen
abwärts, nimmt nur solche, die die Modulzahl **ohne Rest** teilen, verteilt die Stränge auf
gleich belegte Geräte und behält die Aufteilung mit der besten Bewertung. Die Rangfolge ist
dreistufig: **(1) möglichst wenige Geräte, (2) DC/AC möglichst nahe an 1,25** (`DCAC_MITTE`,
die Mitte des Bandes 1,0…1,5), **(3) möglichst lange Reihe** (höhere Spannung, kleinerer
Strom). `GeraeteBewerten` setzt davor noch die Unterscheidung passend/unpassend und dahinter
den Namen als letzte Ordnung. Alle Stränge sind gleich lang und alle Geräte gleich belegt —
die Aufteilung, die ein Planer von Hand wählen würde.

`Aufteilen` verteilt die Stränge eines Geräts **so gleichmäßig wie möglich** auf seine
Tracker; die vorderen Tracker bekommen den Strang mehr, wenn die Zahl nicht aufgeht. Mehr
Tracker als Stränge lassen die überzähligen unbelegt — ein Gerät mit vier Trackern und einem
Strang bekommt **eine** Zeile, nicht vier.

Die Sätze stehen seit dem 08.09.2026 in der Ampel des PV-Dialogs hinter jedem roten oder
gelben Befund an P1–P3 bzw. P6/P7 („… · passend wären 4…14 Module in Reihe"). Nachweis:
`EPOS.Kern.Tests/StrangAuslegungTests.cs` (Anhang-A-Modul und -Gerät, Zahl für Zahl gegen die
Prüfung) und drei Fälle in `StrangPlausibilitaetTests`.

### In der Oberfläche (seit 08.09.2026, **W6‑B‑8**)

`Vorschlagen`, `Aufteilen` und `GeraeteBewerten` sind im PV-Dialog bedienbar — Abschnitt
„Wechselrichter und Stränge", Weg „mit Wechselrichter":

**1. Die Katalogwahl ist sortiert und beschriftet.** Das Auswahlfeld „Wechselrichter aus dem
Katalog" listet die (nach Hersteller gefilterten) Geräte in der Reihenfolge von
`GeraeteBewerten` — passende zuerst, unter ihnen wenige Geräte vor vielen und DC/AC nahe 1,25
vor entfernterem. Passende Geräte tragen ihre Zahlen im Text:

| Eintrag | Bedeutung |
|---|---|
| `Muster 2500TL — DC/AC 1,10 · 1 Gerät` | passt: ein Gerät, DC/AC 1,10 im Band 1,0…1,5 |
| `Muster 5000TL-2M — DC/AC 1,22 · 2 Geräte` | passt, braucht aber zwei Geräte |
| `Gross 100TL — passt nicht` | keine Aufteilung: Spannungsfenster, Strom oder DC/AC‑Band gehen nicht auf |

Gemessen wird am **Modul der markierten Projektzeile** und an ihrer **Modulzahl** (steht eine
Strangtabelle, gilt deren abgeleitete Summe). Fehlt eines von beiden, bleibt die Liste
alphabetisch und unbeschriftet — ohne Modulfeld gibt es nichts zu bewerten. Die Klapplisten
**je Strangzeile** bleiben in jedem Fall alphabetisch und ohne Zusatz: Dort steht das Gerät
eines einzelnen Strangs.

**2. Der Knopf „Auslegung vorschlagen"** steht neben „Strang anlegen". Er ist frei, sobald ein
Katalogsatz gewählt ist und Modul und Modulzahl feststehen. Sein Klick

* rechnet `Vorschlagen(modul, gerät, modulzahl)` und legt das Ergebnis mit
  `Aufteilen(vorschlag, Anzahl_Mppt)` in Zeilen — je Gerät und belegtem Tracker eine, die
  Stränge so gleichmäßig auf die Tracker verteilt, wie die Zahl es zulässt;
* nimmt den Katalogsatz in das Projekt auf (dieselbe Kopie wie „Strang anlegen");
* **ersetzt** die Strangtabelle durch die Vorgaben (Ränge neu ab 1; Neigung und Azimut bleiben
  leer, also der Anlagenwert) und meldet darunter, was geschehen ist:
  `Vorschlag: 2 Geräte, je 1 Strang mit 10 Modulen in Reihe, DC/AC 1,10 — die Strangtabelle
  wurde ersetzt.`

Gibt es keine Aufteilung, **bleibt die Tabelle stehen**, und der Grund erscheint als Warnung:
`Kein Vorschlag: Die Modulzahl lässt sich nicht in gleich lange Stränge und gleich belegte
Geräte teilen.` (weitere Gründe: „Keine Reihe passt zu diesem Gerät (Spannungsfenster)",
„Spannungswerte des Moduls oder Grenzen des Geräts fehlen", „Modul oder Gerät fehlt",
„Keine Module").

Ersetzt und nicht ergänzt wird, weil ein Vorschlag eine **ganze** Aufteilung des Modulfelds ist
— gleich lange Stränge, gleich belegte Geräte; an bestehende Zeilen angehängt ergäbe er eine
Anlage mit der doppelten Modulzahl.

Gerechnet wird im Kern (`StrangAuslegung`), formatiert in der Windows-Hülle
(`PhotovoltaikHuelle`), gezeigt in `EPOS.UI/Dialoge/Erzeuger/PvStraengeFelder.razor`. Ohne die
beiden Delegaten bleibt alles beim Stand davor — die iOS-Hülle bekommt sie später, ohne dass
die Maske sich ändert. Nachweise: `EPOS.Kern.Tests/StrangAuslegungTests.cs` (`Aufteilen`) und
`EPOS.UI.Tests/Dialoge/PvStraengeFelderTests.cs` (Abschnitt 6).

## 9 Grenzen

Keine Verschattung, keine Kabel- und Anschlussverluste, keine Ost/West-Mischung auf einem
Tracker (die Prüfung rechnet je Tracker die Summe), keine Modul-Mischung in einem Strang, und
die MPP-Spannung mit beta_OC statt eines eigenen Koeffizienten. Für die Simulation gilt
unverändert das Rechenmodell der Anlage (EINFACH oder ERWEITERT); die Ampel ist Prüfung und
Rat, kein Rechenergebnis.

Dazu, ausdrücklich benannt:

* **Kein Sicherheitsfaktor.** P4 rechnet nur die thermische Korrektur (rund +2 %), nicht den in
  der Auslegungspraxis üblichen Faktor 1,25 auf den Kurzschlussstrom (siehe Abschnitt 3 und
  O‑1). Wer nahe an `I_Dc_Max` auslegt, sollte den Abstand selbst wahren.
* **Feste Auslegungstemperaturen.** −10 °C und +70 °C gelten für jedes Projekt, unabhängig von
  Klimaregion, Montageart und `T_NOCT` des Moduls (O‑3).
* **Kein Rückfall ohne Koeffizienten.** Fehlt `beta_OC`, entfällt P1 vollständig — die Ampel
  wird nur gelb. Ein Ersatzweg über einen Sicherheitsfaktor auf U_oc besteht nicht (O‑4).
* **Die Einschaltspannung wird nicht geprüft.** `U_Start` steht im Gerätekatalog, geht aber in
  keine der acht Regeln ein (O‑5).
* **Keine absolute Systemspannung.** Geprüft wird ausschließlich gegen `U_Dc_Max` des Geräts,
  nicht gegen 1 000 V oder 1 500 V.
* **Die Prüfung läuft nur im Dialog.** Ein Projekt mit roter Ampel rechnet in der Simulation
  ohne Protokollhinweis durch (O‑6).
* **Fehlende Modulnennleistung bleibt stumm.** Ohne `m_Leistung` entfallen P6 und P7 ohne
  Meldung und ohne Gelbfärbung — die einzige Lücke in der Regel „ein fehlender Wert macht
  gelb" (O‑7).

## 10 Offene Punkte

Ergebnis des Abgleichs vom 08.09.2026 gegen Code und Word-Fassung
(`K:\pv2\pv_pruefbericht.md`, dort mit Belegstellen). Nichts davon ist umgesetzt.

| Nr. | Punkt | Art |
|---|---|---|
| **O‑1** | Soll P4 einen Sicherheitsfaktor (1,25 · I_sc) bekommen — als zusätzliche gelbe Prüfung oder als Einstellung? P4 rot zu verschärfen würde Bestandsprojekte umfärben. | Entscheid des Anwenders |
| **O‑2** | `Vorschlagen` behandelt das weiche DC/AC-Band als harte Bedingung und scheitert dann mit der Begründung „lässt sich nicht in gleich lange Stränge teilen", die den wahren Grund verschweigt (Beispiel Sunny Boy 6.0 mit 20 Modulen). | Codeänderung vorgeschlagen |
| **O‑3** | Feste −10/+70 °C beibehalten oder an Klimaregion und `T_NOCT` koppeln? Die Zuordnung dieser Zahlen zu IEC 62548 in der Word-Fassung ist ungeklärt. | Entscheid des Anwenders |
| **O‑4** | Rückfall für P1 ohne `beta_OC` über einen Sicherheitsfaktor auf U_oc (1,15 bzw. 1,25)? P1 ist die einzige zerstörungsrelevante Prüfung, und der Katalogbestand ist an dieser Stelle bekanntermaßen lückenhaft. | Entscheid des Anwenders |
| **O‑5** | Einschaltspannung `U_Start` als gelbe Prüfung gegen die Strangspannung im heißen Fall aufnehmen? | Codeänderung vorgeschlagen |
| **O‑6** | Prüfmeldung beim Simulationsstart nachrüsten — oder die entsprechende Aussage in Konzept 4.2 und im Wiki streichen? | Entscheid des Anwenders |
| **O‑7** | Fehlende Modulnennleistung soll gelb machen (heute stumm). | Codeänderung vorgeschlagen |
| **O‑8** | P8 kann über den einzigen Aufrufer nie anschlagen, weil die Maske die abgeleitete Summe als Anlagenwert übergibt (Entscheid Q9). Gegen den gespeicherten Wert prüfen oder P8 als reine Zusicherung führen? | Entscheid des Anwenders |
| **O‑9** | Trägt `I_Dc_Max` im Katalog den Kurzschluss- oder den Nennstrom je Tracker? Davon hängt ab, ob P4 zu streng oder zu lasch ist. | Entscheid des Anwenders |
| **O‑10** | Die Word-Fassung „Auslegung Und Prüfung Von PV-Anlagen.docx" enthält Einheitenfehler (Koeffizienten als %/K), unbelegte Normaussagen, eine erfundene Architekturbeschreibung und ein falsch aufgelöstes Praxisbeispiel; ihre Formeln liegen als Bilder vor und sind weder durchsuchbar noch weiterverwendbar. Berichtigen oder aus dieser Markdown-Fassung neu erzeugen? | Entscheid des Anwenders |
