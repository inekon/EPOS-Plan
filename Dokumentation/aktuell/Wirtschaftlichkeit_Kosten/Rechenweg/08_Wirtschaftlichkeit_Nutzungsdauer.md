# 08 · Wirtschaftlichkeit über die Nutzungsdauer

**Dialog:** `UcWirtschaftlichkeit`, Umschalter „Kennzahlen / ValERI-Bewertung" (Konzept § 2.10,
Empfehlung V-1) · **Mockup:** `../Mockups/Dialog_Formel_Zahlenprobe.html#valeri` · **Norm:** DIN EN
17463:2021-12 (ValERI) — Abschnitte 6, 7, 8, Anhänge A, C, E · **Code:** `KapitalwertRechner`,
`Tab_ProjektWirtschaftlichkeit` · **Konzept:** § 2.9 (Vergleichsprojekt), § 2.11 (ValERI), § 3.1

Die Norm verlangt weniger eine andere Rechnung als eine andere Darstellung: Der Kapitalwert ist das
**einzige** Entscheidungskriterium, interner Zinsfuß und Amortisation sind ausdrücklich
nachrichtlich. EPOS-Plan rechnet bereits nach dieser Norm (`KapitalwertRechner` trägt sie im Namen);
die Integration ist eine Vervollständigungs- und Darstellungsaufgabe.

## Was die Ansicht zeigt

Zahlen aus der realen Höfingen-Mappe `BHKW_Höfingen_Erneuerung_20kWel.XLS`, Blatt
`Tab_kurz_KWKG2020` — 20-kW-BHKW-Erneuerung gegen benannte Vergleichsheizung.

**Kopfzeile:** Vergleichsprojekt (Combo) „Variante 2 — Gas-Brennwertkessel (Bestand)" — die
Referenz der Differenzrechnung, die Unterlassensalternative der Norm (8.1.2).

| Block | Anlage | Referenz | Differenz | Normbezug |
|---|---|---|---|---|
| Investitionskosten | 78.245 | 22.500 | 55.745 | 6.1 · 7.3 |
| Betriebskosten p. a. | 3.466 | 518 | 2.948 | 6.3.1 |
| Energiekosten p. a. | 13.746 | 11.498 | 2.248 | 6.3.2 |
| Erlöse p. a. | 15.632 | 0 | − 15.632 | 6.1 |
| **Jahresüberschuss der Differenz** | | | **10.436** | 7 |

**Gruppe Kennzahlen:** Kapitalwert **65.259 €** (Entscheidungskriterium) · Interner Zinsfuß 20,4 %
*nachrichtlich (Anhang C)* · Dynamische Amortisation 4,33 a *nachrichtlich (Anhang C)* ·
Kalkulationszins 2,0 % · Nutzungsdauer 13,3 a · **Warnband:** „Der interne Zinsfuß ist bei dieser
Reihe mehrdeutig: Ersatzbeschaffung und KWKG-Auslauf erzeugen mehr als einen Vorzeichenwechsel. Die
Norm verwirft ihn als Entscheidungsgrundlage."

**Gruppe Deklarationen (Abschnitt 7 und 9):** Rechnung **nominal** (Realwerte nach 6.3.2 unzulässig)
· Energie- und Stromsteuerentlastungen **berücksichtigt** · Ertragsteuern **nicht berücksichtigt** ·
keine Abschreibungen als Cashflow · Restwert linear — **dokumentierte Abweichung** von 6.4 ·
Risikozuschlag nicht angesetzt (6.5 optional) · Szenariopflege „12 von 31 Parametern" · Knöpfe
„Anhang-E-Checkliste…", „XLSX mit Formeln exportieren…".

**Szenarien** — alle Parameter gleichzeitig variiert (7.3): Best +118.430 · Erwartet +65.259 ·
Worst −12.870 € (Best/Worst im Mockup Beispielwerte). „Ein negativer Worst Case ist nach 8.1.3 kein
Ausschlusskriterium — er beziffert das Risiko." Fußleiste: „Kapitalwert > 0 ⇒ vorteilhaft (8.1.2)" ·
Sensitivität… · Bericht erzeugen.

## Berechnungsgrundlage

```
KW [€] = − I₀
         + Σ_{t=1..T} ( E_t − A_t ) / (1 + i)^t
         + RW_T / (1 + i)^T
         + Einmalzahlung_t0                       Index 0 wird nicht abgezinst, mindert I₀ nicht

A_t = Betrieb_t × (1 + p_B)^(t−1)
    + Energie_1 × (1 + p_E)^(t−1)
    + CO2_t + Ersatz_t
E_t = Einspeiseerlös_1                            nominal KONSTANT
    + Σ Erlösreihen: KWKG_ZUSCHLAG · KWKG_PAUSCHALE · ENERGIESTEUER_GUTSCHRIFT ·
                     STROMSTEUER_BEFREIUNG · STROMSTEUER_ENTLASTUNG · PV_VERGUETUNG

Rahmen (Tab_ProjektWirtschaftlichkeit, EINE Zeile je Stammprojekt, gültig für die Vergleichsgruppe)
  Zinssatz i 3,0 % · Betrachtungszeitraum T 20 a · Preissteigerung Energie p_E 0,0 ·
  Preissteigerung Betrieb p_B 0,0 · Einspeisevergütung PV · Einspeisevergütung KWK (NULL = aus) ·
  CO2_Preis (0 = Katalogpfad) · ID_Referenzprojekt (D-3)
  Genau ZWEI Preissteigerungsreihen — keine je Träger, keine je Position

Nutzungsdauer, Ersatz, Restwert, Startjahr
  n = Nutzungsdauer, falls ≥ 1 ; sonst n = T   (dann kein Ersatz, kein Restwert)
  start = StartJahr, falls > 1 ; sonst 0 ; start = 0 → Betrag in I₀ ; start ≥ 2 → Zahlung im
  Jahr start, abgezinst, NICHT indexiert ; start > T → keine Zahlung, nur Ausweis
  Ersatz:   t_j = round(start + k·n)  für k = 1,2,… solange 1 ≤ t_j < T
  Restwert: Alter = T − letzte Beschaffung ; Restdauer = n − Alter ; RW_T = Betrag × Restdauer / n

Kennzahlen
  Annuitätenfaktor a(i,n) = i·(1+i)^n / ((1+i)^n − 1) ; a = 1/n bei i ≈ 0
  Kapitalwertdifferenz = KW(Variante) − KW(Referenz)
  Annuität = KW-Differenz × a(i,T)
  Dynamische Amortisation = erstes t mit kumuliertem Barwert ≥ 0, linear interpoliert, OHNE Restwert
  Interner Zinsfuß = Nullstelle KW(r), Bisektion −99 %…1000 %, 200 Schritte — bei > 1 Vorzeichen-
  wechsel mehrdeutig → Warnung (V-G8)
  Wärmegestehungskosten = (−KW × a(i,T)) / (Wärmebedarf [MWh/a] × 1000) [€/kWh]

Szenarien — VALERI-Vorrang
  Szenariowert = Szenariospalte, falls ≠ 0 ; sonst eingegebener Wert
  gepflegt ⇔ |Szenariowert − Erwartet| > 1e−9 → dann wird JEDE Ableitung übersprungen
  Best/Worst: alle gepflegten Best- bzw. Worst-Werte gleichzeitig (7.3) — Investition, Betrieb,
  Energiepreise, Erlössätze, Rahmen (i, T, p_E, p_B), Mengenfaktor (§ 2.11.5) ;
  gesetzliche Sätze werden NICHT szenariert (Rechtsgrößen, Katalogpfad)

Sensitivität (nur „Erwartet", ceteris paribus, 7.2)
  Zins ± 1 %-Punkt · p_E ± 1 %-Punkt · Investition der Variante ± 10 % (Zuschuss nicht mitskaliert) ·
  Energiekosten inkl. CO₂ ± 10 % · „KWKG-Bonus entfällt" ; Ausweis mit Steigung €/% (V-G6)
```

## Berechnungserläuterung an der Höfingen-Mappe

Die Mappe ist die externe Gegenprobe: Ihre Zahlen entstanden ohne EPOS-Plan. Die jahresscharfe
Nachrechnung mit der obigen Formel trifft ihren Kapitalwert von 65.259 € exakt; die Tafel zeigt die
Näherung über eine konstante Reihe, damit der Weg mit dem Taschenrechner nachvollziehbar bleibt.

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| 1 Mehrinvestition | 78.245 − 22.500 | − 55.745 € | Periode 0, nicht abgezinst |
| 2 Mehrbetriebskosten | 3.466 − 518 | − 2.948 €/a | Wartung des Motors gegen den Kessel |
| 3 Mehrbrennstoff | 13.746 − 11.498 | − 2.248 €/a | das BHKW verbrennt mehr, erzeugt aber Strom |
| 4 Erlöse | 15.632 − 0 | + 15.632 €/a | KWKG, Energiesteuer, vermiedener Bezug |
| **Jahresüberschuss** | 15.632 − 2.948 − 2.248 | **10.436 €/a** | konstante Reihe über 13,3 Jahre |
| 5 Barwertfaktor | (1 − 1,02^−13,3) ÷ 0,02 | 11,577 | Rentenbarwert bei i = 2,0 % |
| 6 Näherung | 10.436 × 11,577 − 55.745 | 65.073 € | konstante Reihe, ohne Jahresschärfe |
| **Kapitalwert der Mappe** | jahresscharf, `Tab_kurz_KWKG2020` | **65.259 €** | Abweichung der Näherung 0,3 % — Rundung der Jahreswerte |

Kennzahlen der Mappe: IZF 20,4 % · dynamische Amortisation 4,33 a — beide nachrichtlich.

## Was die Norm zusätzlich verlangt (Gap-Tabelle V-G, Kurzfassung)

| # | Anforderung | Stand | Behandlung |
|---|---|---|---|
| V-G2 | Degradation je Position mit Quelle | fehlt | neues Attribut, Vorgabe 0 (`03`) |
| V-G3 | Zeitpunkt „alle n Jahre" | fehlt | kleiner Ausbau der Bemessung |
| V-G4 | kein Restwertverfahren | Restwert linear | dokumentierte Abweichung, Deklarationszeile |
| V-G5 | Szenarien = alle Parameter gleichzeitig | nur Betragsspalten | **entschieden 31.08.2026:** vollständige Abdeckung (§ 2.11.5), Etappe V-E |
| V-G6 | Sensitivität mit Steigung €/% und Diagramm | 5 Fälle, ohne Steigung | Spalte und Diagramm ergänzen |
| V-G7 | Risiko: Zinszuschlag oder Abzug R_loss × p_loss | fehlt | optionales Modul, Vorgabe aus |
| V-G8 | IZF/Amortisation nur nachrichtlich, Mehrdeutigkeitswarnung | gleichrangig | Label + Warnung |
| V-G9 | Steuerdeklaration Pflicht | fehlt | zweiteilige Deklarationszeile |
| V-G10 | Bericht mit editierbarer XLSX mit Formeln (Anhang A) | Werte-Export | **größte Einzellücke mit hartem Muss** — ValERI-Blatt je Szenario |
| V-G11 | nicht monetarisierbare Wirkungen | fehlt | Freitext + Kategorie + Beurteilung, nie im NPV |
| V-G12 | Anhang-E-Checkliste (15 Punkte) | fehlt | Abschlussseite des Berichts |

Externe Gegenprobe der Etappe: Anhang D der Norm (BHKW 90 kW_th, 18 Jahre, NPV 64.480 €, Worst
−202.802 €, Best +598.320 €) — EPOS muss mit denselben Eingaben dieselben Zahlen treffen. Zwei
Zeilen der Sensitivitätstabelle D.6 tragen im Normtext versehentlich Werte des Pumpenbeispiels —
als Prüfreferenz ungeeignet.

## Offene Entscheidungen

| Nr. | Frage | Empfehlung |
|---|---|---|
| V-1 | Fünf Blöcke als Aufklappabschnitte oder zweite Ansicht mit Umschalter? | Umschalter — die Seite ist voll (K8) |
| V-2 | XLSX-Formelexport nur das ValERI-Blatt oder der ganze Bericht? | nur das ValERI-Blatt |
| V-3 | IZF/Amortisation von den Kacheln nehmen oder mit Label behalten? | behalten mit „nachrichtlich" |
| V-4 | Szenario-Parametersätze vor oder nach V-A–V-D? | danach — einzige Etappe mit Rechenwirkung, eigener A/B-Nachweis |
