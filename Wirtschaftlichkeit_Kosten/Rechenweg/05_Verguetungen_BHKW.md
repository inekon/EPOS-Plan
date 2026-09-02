# 05 · Vergütungen BHKW

**Dialog:** `Form_BhkwWirtschaftlichkeit` — neu, BW9 (Konzept § 2.2) · **Mockup:**
`../Mockups/Dialog_Formel_Zahlenprobe.html#bhkw` · **Recht:** § 2 Nr. 16 und Nr. 20, § 6 Abs. 3,
§ 7, § 8 KWKG 2025 · §§ 53, 53a, 54 EnergieStG · § 9 Abs. 1 Nr. 3, § 9b StromStG · **Code:**
`KwkgAnlagenCtrl`, `WirtschaftlichkeitCtrl` (Erlösreihen `KWKG_ZUSCHLAG`, `KWKG_PAUSCHALE`,
`ENERGIESTEUER_GUTSCHRIFT`, `STROMSTEUER_BEFREIUNG`, `STROMSTEUER_ENTLASTUNG`) · **Konzept:** § 2.2,
§ 3.6, § 3.7, § 3.8, § 3.9

Die dichteste Kategorie: vier Rechtsgrundlagen, drei verschiedene Strommengen und eine Staffel, die
marginal rechnet statt klassenweise. Der Dialog bringt zusammen, was heute auf drei Formulare
verteilt ist, und zeigt zu jedem Satz, woher er kommt.

## Was der Dialog zeigt

**Gruppe Anlagen** — Tabelle je BHKW-Modul (Anlage · P_el · Brennstoff · Stichtag · Inbetriebnahme
· Anlagenart) mit Aufklappzeile: Eigenstrom nach § 6 Abs. 3 (Combo) · Satz Einspeisung · Satz
Eigenstrom (0 = Vorschlag) · Vbh-Kontingent · Vbh-Jahresdeckel (0 = Staffel) · **neu B5:**
Energiesteuerentlastung (Anlage) · Brennstoff auf Strom/Wärme (Anlage) · Hilfsenergieanteil [%]
(Vorschlag BHKW 2–4 %, wirkt nur auf die KWKG-Nettostrommenge).

**Mengentafel** — welche Vorschrift rechnet mit welcher Menge. Das ist die wichtigste neue
Darstellung dieser Kategorie:

| Menge | MWh/a | verwendet von | Grund |
|---|---|---|---|
| Stromerzeugung brutto | 1.650,0 | § 9 Abs. 1 Nr. 3 · CO₂-Grenzwert · Vollbenutzungsstunden | an den Generatorklemmen gemessen |
| − Hilfsstrom (2,0 % × 4.342,1) | − 86,8 | — | Neben- und Hilfsanlagen, § 2 Nr. 20 KWKG |
| **= Nettostromerzeugung** | **1.563,2** | KWKG-Zuschlag § 7 | = KWK-Strom, § 2 Nr. 16 Fall 1 |
| davon Eigenverbrauch (70 %) | 1.094,2 | Zuschlag Abs. 2 | |
| davon Einspeisung (30 %) | 469,0 | Zuschlag Abs. 1 · Einspeiseerlös | |
| Eigenverbrauch brutto (70 %) | 1.155,0 | § 9 Abs. 1 Nr. 3 StromStG | kein Netting — andere Vorschrift |

Das Netting wirkt **ausschließlich** auf die KWKG-Zuschlagsmengen. Stromsteuer, CO₂-Grenzwert und
Vollbenutzungsstunden bleiben brutto — keine Inkonsistenz, sondern Folge davon, dass nur § 7 KWKG
auf „KWK-Strom" im Sinne des § 2 Nr. 16 zahlt.

**Gruppe KWK-Zuschlag (§ 7)** — Herleitungslabel je Satz:

```
Einspeisung 5,5667 ct/kWh — 50 kW × 8,00 + 50 kW × 6,00 + 150 kW × 5,00 + 50 kW × 4,40 = 1.670 ÷ 300 kW
Eigenstrom  2,4167 ct/kWh — Tatbestand Nr. 2: 50 × 4,00 + 50 × 3,00 + 150 × 2,00 + 50 × 1,50 = 725 ÷ 300 kW
```

Knopf „Vorschlag in die Satzfelder übernehmen" (schreibt nur auf Knopfdruck) · Vbh-Kontingent
30.000 h (neu, § 8 Abs. 1) · Jahresdeckel 2026 3.300 h/a (Staffel § 8 Abs. 4) · Abschlag
Negativstunden [%]. **Warnband:** „Die Anlage läuft 5.500 h/a, vergütet werden 2026 aber nur
3.300 h — 60 % der Erzeugung. Das Kontingent reicht dadurch über 12 Kalenderjahre."

**Gruppe Energiesteuer:** Entlastung Projekt (keine · § 53 Formular 1131 · § 53a Abs. 5 Formular
1135 · § 54 Formular 1450) · Brennstoffaufteilung · Jahresnutzungsgrad 83,0 % (Schwelle 70 %) ·
Herleitung „§ 53a Abs. 5 · Erdgas 4,42 €/MWh · 4.342,1 MWh (H_i) × 1,1048 = 4.797,2 MWh (H_s) ×
4,42 = 21.203,4 €/a" · Info „Alternative § 53: voller Satz 5,50 €/MWh → 26.384,3 €/a. Nicht
kumulierbar — je Anlage genau eine Wahl." · Info „Die Unternehmensart spielt hier **keine** Rolle:
§§ 53 und 53a Abs. 5 differenzieren nicht danach. Sie wirkt nur auf § 54 (Kessel) und § 9b
(Netzbezug)." · Kohärenzzeile in Firebrick, wenn der erfasste Brennstoffpreis die Energiesteuer nicht
ausweist.

**Gruppe Stromsteuer:** Unternehmensart (führend) · Hocheffizienz nachgewiesen · räumlicher
Zusammenhang ≤ 4,5 km · Modus § 9 Abs. 1 Nr. 3 (Erlös/Ausweis — **Lücke K3**, Spalte kommt mit B6) ·
Herleitung „P_el 300 kW ≤ 2 MW ✓ · CO₂ 242,1 < 270 g/kWh ✓ · 1.155,0 MWh × 20,50 €/MWh = 23.677,5 €/a"
und „§ 9b: 250,0 MWh × 20,00 − 250 = 4.750,0 €/a" · **Warnband:** „Auf selbst erzeugten und selbst
verbrauchten Strom entsteht gar keine Stromsteuer — der Vorteil steckt bereits in der kleineren
Bezugsrechnung. Deshalb Ausweis statt Erlös (Befund B-1)."

**Gruppe Kohärenzprüfung** — ohne Rechenwirkung:

| Prüfung | Befund |
|---|---|
| Energiesteuer im Gaspreis | 0,0638 €/m³ ausgewiesen · § 53a gewählt ✓ |
| Satz gegen Katalog | 5,50 €/MWh × 11,6 kWh/m³ ÷ 1000 = 0,0638 €/m³ · deckungsgleich ✓ |
| § 9b bei produzierendem Gewerbe | gewählt ✓ |
| Doppelpflege Hilfsenergie | Anlagenanteil 2,0 % *und* Kostenposition aktiv — Warnung |

**Vorschau Jahr 1 (2026)**, live aus dem einen Rechenweg:

| Position | Menge × Satz | €/a |
|---|---|---|
| KWK-Zuschlag Einspeisung (§ 7 Abs. 1) | 469,0 MWh | 15.664,7 |
| KWK-Zuschlag Eigenstrom (§ 7 Abs. 2) | 1.094,2 MWh | 15.866,1 |
| Energiesteuer-Gutschrift (§ 53a Abs. 5) | | 21.203,4 |
| Stromsteuer-Entlastung Netzbezug (§ 9b) | | 4.750,0 |
| Einspeiseerlös Strom | 469,0 MWh × 5,0 ct | 23.450,0 |
| **Summe zahlungswirksam** | | **80.934,2** |
| Stromsteuer-Befreiung Eigenverbrauch (§ 9 Abs. 1 Nr. 3) — Ausweis | 1.155,0 MWh | 23.677,5 |

## Berechnungsgrundlage

```
Mischsatz — marginal über die Leistungsanteile, NICHT klassenweise
  Satz [ct/kWh] = Σ_k Breite_k × Satz_k / P_el
  Breite_k      = min(Obergrenze_k, P_el) − Obergrenze_(k−1)
  Eine Klassenlogik hätte bei 300 kW nur 4,40 ct/kWh ergeben — 21 % zu wenig.
  § 7 Abs. 3a geht Abs. 1 und 2 vor: Neuanlage ≤ 50 kW → 16,00 / 8,00 ct/kWh
  Abs. 2 (Eigenstrom) nur in den drei Tatbeständen des § 6 Abs. 3 ; KEINER ⇒ Satz 0
  Sätze je Anlage: Anlagensatz ?? Projektsatz ; beim Eigenstromsatz verlangt ein gepflegter
  Anlagensatz einen Tatbestand — fehlt er, Satz 0 mit Meldung

Mengenkette (§ 2 Nr. 16 und Nr. 20 KWKG)
  Hilfsstrom(A) = Hilfsenergie_Anteil / 100 × Brennstoff(A) [MWh/a]
  Netto(A)      = max(0, Brutto(A) − Hilfsstrom(A)) ;  Anteil = Netto(A) / Σ Netto
  Eigen/Einsp(A) = Projektmengen_netto × Anteil      (ohne Stundenreihen: alles Eigen)
  Eigen zuerst: Eigen' = max(0, E − H) ;  Einsp' = max(0, F − max(0, H − E))
  Rechtskette: § 7 zahlt auf KWK-Strom → § 2 Nr. 16: bei Anlagen ohne Abwärmeabfuhr ist das
  die Nettostromerzeugung → § 2 Nr. 20: abzüglich Neben- und Hilfsanlagen. Das Netting ist richtig.

Jahresreihe mit Kontingent und Deckel
  Bonus_voll = Eigen × 10 × SatzEigen + Einsp × 10 × SatzEinsp      [€/a bei MWh und ct/kWh]
  je Jahr:  Vergütet  = min(Vbh, Deckel(Kalenderjahr), Restkontingent) × (1 − Abschlag)
            Reihe[t] += Bonus_voll × Vergütet / Vbh
            Rest     −= Vergütet
  Kontingent § 8: Override > 0 gewinnt ; sonst neu 30.000 h · modernisiert ab 50 %/25 % →
  30.000/15.000 · nachgerüstet ab 50/25/10 % → 30.000/15.000/10.000 ; darunter 0 mit Fehlgrund
  Deckelstaffel 5.000 (2021) · 4.000 (2023) · 3.500 (2025) · 3.300 (2026) · 3.100 · 2.900 · 2.700 · 2.500 (ab 2030)
  Prüfkette vorab: Stichtag ≤ 31.12.2026 · Realisierungsfrist 4 Jahre · Ausschreibung > 500 kW ·
  Heizöl-Neuanlage ab 2025
  Pauschale § 9 (≤ 2 kW): 0,04 × 60.000 × P_el, einmalig in Index 0

Energiesteuer, anlagenscharf — je Betrachtungsjahr ein Rechnerlauf, Katalog: jüngste Zeile mit
JahrVon ≤ Jahr ; fehlt der Satz ⇒ 0 € mit Begründung, nie geraten ; Wahl(a) = Anlagenwert ?? Projektwert
  § 53   Gutschrift = Satz_voll(Träger, Jahr) × Menge
         VOLLER_BRENNSTOFF (Vorgabe): Menge = Brennstoff(a) ungeteilt (§ 53 Abs. 2)
         ENERGETISCH: Brennstoff × Strom/(Strom + Wärme) — kein Rechtsverfahren, bewusste Untergrenze
         nur Stromerzeuger ; Kessel mit § 53/53a ⇒ 0 € + Begründung
  § 53a  Gutschrift = Teilsatz × Brennstoff(a), immer Gesamteinsatz
         Nutzungsgradschwelle 70 % (Projektgröße) ; ungepflegt oder unterschritten ⇒ 0 € + Begründung
  § 54   netto = max(0, Σ_{Wahl=54} Teilsatz_54 × Menge(a) − 250 €) ; nur produzierendes Gewerbe
         oder Land-/Forstwirtschaft ; Sockel EINMAL je Lauf
  Einheitenkette:  €/MWh: MWh_Hi × (eff_hs / eff_hi) → Brennwertmenge (Erdgas 11,6/10,5 = 1,1048)
                   €/1.000 l bzw. kg: MWh × 1000 / eff_hi / 1000 — nur mit passender Einheit, keine geratene Dichte
                   €/GJ: MWh × 3,6
  Sätze: Erdgas 5,50 / 4,42 / 1,38 €/MWh · Heizöl EL 61,35 / 40,35 / 15,34 €/1.000 l · Sockel 250 €/a

Stromsteuer
  § 9 Abs. 1 Nr. 3   Betrag = Regelsatz(Jahr) × KwkEigen_BRUTTO [MWh/a] × Anteil
                     Anteil = Σ Strom(a, bestanden) / Σ Strom(a, alle) ; Regelsatz 20,50 €/MWh
                     vier Bedingungen: Hocheffizienz · räumlicher Zusammenhang 4,5 km (Anwenderangaben)
                     P_el ≤ 2 MW je Anlage · CO₂ < 270 g/kWh Energieertrag
                     CO₂-Energieertrag = Faktor_EBeV × Brennstoff / (Strom + Wärme)
                     KwkEigen nur mit Stundenreihen — sonst 0 mit Begründung
  § 9b               Betrag = max(0, 20,00 €/MWh × Netzbezug [MWh/a] − 250 €/a) ; nur produzierendes
                     Gewerbe ; hängt an keiner KWK-Anlage
  Die Mengen beider Vorschriften sind disjunkt (Eigenverbrauch gegen Netzbezug).

Einspeiseerlös   KWK_Einspeisung × 10 × EV_KWK (nur bei gepflegtem Satz) → Zonentarif → Rollentarif ;
                 nominal konstant

Kohärenzprüfung (§ 3.9) — Warnzeilen ohne Rechenwirkung
  2 Entlastung ohne Belastung: Gutschrift gebucht, Preis weist die Steuer nicht aus → Warnung mit Betrag
  3 Belastung ohne Entlastung: Anteil ausgewiesen, keine Wahl bzw. kein § 9b → Hinweis
  4 Satz ≠ Katalogsatz (Toleranz 0,005 ct/kWh) → Hinweis
  Doppelpflege Hilfsenergie: Anlagenanteil > 0 UND aktive Kostenposition derselben Anlage → Warnung
```

## Berechnungserläuterung am Beispielprojekt

### Der Mischsatz — wie ein Steuertarif

| Leistungsanteil | Breite | Satz | Beitrag | Rechtsgrundlage |
|---|---|---|---|---|
| bis 50 kW | 50 kW | 8,00 ct | 400 | § 7 Abs. 1 Nr. 1 |
| > 50 bis 100 kW | 50 kW | 6,00 ct | 300 | Nr. 2 |
| > 100 bis 250 kW | 150 kW | 5,00 ct | 750 | Nr. 3 |
| > 250 kW bis 2 MW | 50 kW | 4,40 ct | 220 | Nr. 4 |
| **Mischsatz Einspeisung** | 300 kW | | **1.670 ÷ 300 = 5,5667 ct/kWh** | belegt am Bestand |

Eigenstrom (Tatbestand Nr. 2 Kundenanlage): 50 × 4,00 + 50 × 3,00 + 150 × 2,00 + 50 × 1,50 = 725 ÷
300 = **2,4167 ct/kWh**. Ohne Tatbestand wäre der Satz 0 und Bonus_voll halbiert.

### Die Jahresreihe

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| 1 Bonus Einspeisung | 469,0 × 10 × 5,5667 | 26.107,8 € | volle Menge, ungedeckelt |
| 2 Bonus Eigenstrom | 1.094,2 × 10 × 2,4167 | 26.443,5 € | nur wegen Tatbestand Nr. 2 |
| **Bonus_voll** | 26.107,8 + 26.443,5 | **52.551,3 €** | Bezugsgröße der Jahresreihe |
| 3 Deckelanteil 2026 | min(5.500 ; 3.300 ; 30.000) ÷ 5.500 | 0,600 | Deckel greift, nicht das Kontingent |
| **Zuschlag Jahr 1** | 52.551,3 × 0,600 | **31.530,8 €** | 15.664,7 Einspeisung + 15.866,1 Eigenstrom |

| t | Jahr | Deckel | Vergütet | Rest danach | Zuschlag |
|---|---|---|---|---|---|
| 1 | 2026 | 3.300 | 3.300 | 26.700 | 31.530,8 |
| 2 | 2027 | 3.100 | 3.100 | 23.600 | 29.620,0 |
| 3 | 2028 | 2.900 | 2.900 | 20.700 | 27.708,9 |
| 4 | 2029 | 2.700 | 2.700 | 18.000 | 25.797,9 |
| 5–11 | 2030–2036 | 2.500 | 2.500 | 15.500 → 500 | 23.887,0 je Jahr |
| 12 | 2037 | 2.500 | **500** (Rest) | 0 | 4.777,4 |
| 13–20 | 2038–2045 | — | 0 | 0 | 0 |
| **Summe** | | | 30.000 | | **286.644 €** = Bonus_voll × 30.000 / 5.500 |

Der volle Jahresbonus wird nie ausgezahlt: Der Jahresdeckel sinkt bis 2030 auf 2.500 h, und das
Gesamtkontingent ist danach erschöpft. Aus einer scheinbaren Dauerförderung wird eine Reihe über
zwölf Jahre mit fallendem Anfang — acht Jahre des Betrachtungszeitraums bleiben ohne Zuschlag. Das
Mockup zeigt die Reihe als Balkendiagramm.

### Energiesteuer

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| Brennwertmenge | 4.342,1 MWh × 11,6 / 10,5 | 4.797,2 MWh (H_s) | Einheitenkette €/MWh |
| § 53a Abs. 5 | 4.797,2 × 4,42 | **21.203,4 €/a** | Nutzungsgrad 83 % ≥ 70 % ✓ |
| Alternative § 53 | 4.797,2 × 5,50 | 26.384,3 €/a | voller Brennstoff, Abs. 2 |
| Alternative § 53 energetisch | × 1.650 / (1.650 + 1.953,9) = × 0,458 | 12.082 €/a | bewusste Untergrenze, kein Rechtsverfahren |

### Stromsteuer

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| CO₂-Energieertrag | 200,9 × 4.342,1 / (1.650,0 + 1.953,9) | 242,1 g/kWh | < 270 ✓ (Heizöl 303 g/kWh würde scheitern) |
| § 9 Abs. 1 Nr. 3 | 1.155,0 MWh (brutto) × 20,50 | 23.677,5 €/a | **Ausweis** nach B6; heute als Erlösreihe gebucht (B-1) |
| § 9b | max(0, 250,0 × 20,00 − 250) | 4.750,0 €/a | Netzbezug, produzierendes Gewerbe |

## Befunde und offene Punkte

| Nr. | Befund | Behandlung |
|---|---|---|
| ⚠ **K-1** | **Der zweite Fall des § 2 Nr. 16 fehlt:** bei Anlagen mit Vorrichtung zur Abwärmeabfuhr (Notkühler) ist KWK-Strom = Nutzwärme × Stromkennzahl, nicht die Nettostromerzeugung; EPOS-Plan führt weder Kennzeichen noch Stromkennzahl und rechnet immer Fall 1 — Zuschlag für Notkühler-Anlagen **zu hoch** | Kennzeichen und Stromkennzahl je Anlage aufnehmen, Fall 2 rechnen — zu entscheiden |
| ⚠ B-1 | § 9 Abs. 1 Nr. 3 als Erlösreihe gebucht — es entsteht aber gar keine Stromsteuer; gemessen 1.510,84 €/a auf beiden Pfaden (Projekt 1024) | Umstellung auf Ausweis (`Stromst_Befreiung_Modus`, Vorgabe AUSWEIS) mit B6 entschieden |
| K3 | Modusfeld § 9 Nr. 3 — Spalte kommt erst mit B6 (Schemaschritt 63) | im Mockup vorhanden, ausgegraut bis B6 |
| K4 | Tabellenspalte „Brennstoff" ohne Leseweg | kleiner Leser `CarrierId` → Name in B5 |
| K7 | Schreibweg der drei B5-Spalten fehlt (`KwkgAnlagenCtrl.Speichere` = 8 Spalten) | auf 11 Spalten erweitern — B5-Kernaufgabe |
| R-U1 | § 53 neben § 53a — Entweder-oder | als Auswahl modelliert, mit dem Hauptzollamt zu klären |
| R-U3 | Ausschluss fossiler flüssiger Brennstoffe (nur Sekundärquelle) | als Prüfkette „Heizöl-Neuanlage ab 2025" umgesetzt |
