# 05 · Vergütungen BHKW

**Dialog:** `Form_BhkwWirtschaftlichkeit` — neu, BW9 (Konzept § 2.2) · **Mockup:**
`../Mockups/Dialog_Formel_Zahlenprobe.html#bhkw` · **Recht:** § 2 Nr. 16 und Nr. 20, § 6 Abs. 3,
§ 7, § 8 KWKG 2025 · §§ 53, 53a, 54 EnergieStG · § 9 Abs. 1 Nr. 3, § 9b StromStG · **Code:**
`KwkgAnlagenCtrl`, `WirtschaftlichkeitCtrl` (Erlösreihen `KWKG_ZUSCHLAG`, `KWKG_PAUSCHALE`,
`ENERGIESTEUER_GUTSCHRIFT`, `STROMSTEUER_BEFREIUNG`, `STROMSTEUER_ENTLASTUNG`) · **Konzept:** § 2.2,
§ 3.6, § 3.7, § 3.8, § 3.9

Die dichteste Kategorie: vier Rechtsgrundlagen, drei verschiedene Strommengen und eine Staffel, die
marginal rechnet statt klassenweise. Das Formular zeigt je Satz eine Zeile mit seiner Herkunft; Wahl
und Herleitung stehen in einer Überlagerung, die mit einem Knopf übernimmt. Drei Tiefen, drei Fragen:
Vorschau (wie viel?), Satzzeile (woher?), Überlagerung (wie entstanden?).

## Was der Dialog zeigt

**Gruppe Anlagen** — Tabelle je BHKW-Modul (Anlage · P_el · Brennstoff · Stichtag · Inbetriebnahme
· Anlagenart · Eigenstrom § 6 Abs. 3) mit Aufklappzeile: je Größe **eine Satzzeile** „gilt · Herkunft"
(Satz Einspeisung 5,5667 ct/kWh — Vorschlag, Katalog KWKG 2025, § 7 Abs. 1, Stichtagsjahr 2026 · Satz
Eigenstrom 2,4167 — § 7 Abs. 2 mit § 6 Abs. 3 Nr. 2 · Vbh-Kontingent 30.000 h — § 8 Abs. 1 · Jahresdeckel
3.300 h/a (2026) — Staffel § 8 Abs. 4 · KWK-Strom Fall 1 · Energiesteuer § 53a Abs. 5 — Projektvorgabe),
das Feld Hilfsenergieanteil [%] (Vorschlag BHKW 2–4 %, wirkt nur auf die KWKG-Nettostrommenge), das
Warnband zum Jahresdeckel und der Knopf **„Sätze und Herkunft…"**, der die Überlagerung öffnet. Ein von
Hand gesetzter Satz steht in seiner Satzzeile als „eigener Wert 6,00 ct/kWh — Vorschlag 5,5667" und
bleibt stehen, bis er in der Überlagerung neu gesetzt wird. Klapplisten und Knöpfe „Vorschlag übernehmen"
am Feld gibt es im Formular nicht.

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

**Überlagerung „Sätze und Herkunft — BHKW 1"** — die Knöpfe „Sätze und Herkunft…" (Gruppe Anlagen) und
„Wahl und Herkunft…" (Gruppe Steuern) öffnen sie über dem Formular; nach dem Hausmuster trägt sie Titel
und Kreuz, der Inhalt hat keinen zweiten Kopf. Drei Gruppen, **ein** Knopf „Übernehmen":

1. **KWK-Zuschlag — diese Anlage.** Anlagenart (§ 8) mit der Kontingentstufe je Option (neu → 30.000 ·
   modernisiert → 15.000 ab 25 % / 30.000 ab 50 % · nachgerüstet → 10.000 / 15.000 / 30.000) und dem
   Feld Anteil Neuherstellungskosten; Eigenstrom nach § 6 Abs. 3 mit dem Satz je Option (keiner → 0 ·
   Nr. 1 → nicht möglich bei 300 kW · Nr. 2 → 2,4167 · Nr. 3 → 3,9683 ct/kWh); KWK-Strom Fall 1
   (Nettostromerzeugung) oder Fall 2 (Vorrichtung zur Abwärmeabfuhr, Stromkennzahl σ, leer = 0,845 aus
   P_el ÷ P_th). Darunter die Tafel **Größe · Vorschlag · Herkunft · eigener Wert · gilt**:

   ```
   Satz Einspeisung  5,5667 ct/kWh  KWKG_ZUSCHLAG_EINSPEISUNG_* · ab 2020 · § 7 Abs. 1:
                                    50 kW × 8,00 + 50 × 6,00 + 150 × 5,00 + 50 × 4,40 = 1.670 ÷ 300
   Satz Eigenstrom   2,4167 ct/kWh  KWKG_ZUSCHLAG_EIGEN_N2_* · ab 2020 · § 7 Abs. 2:
                                    50 × 4,00 + 50 × 3,00 + 150 × 2,00 + 50 × 1,50 = 725 ÷ 300
   Vbh-Kontingent    30.000 h       KWKG_VBH_NEUANLAGE · ab 2020 · § 8 Abs. 1
   Jahresdeckel      Staffel        KWKG_VBH_JAHRESDECKEL · je Kalenderjahr · § 8 Abs. 4
   ```

   Wirkung Jahr 1 (2026): 469,0 MWh × 5,5667 ct × 0,600 + 1.094,2 MWh × 2,4167 ct × 0,600 =
   **31.530,8 €**. Leer heißt: Der Vorschlag gilt und wird beim Übernehmen in die Anlage geschrieben;
   ein eigener Wert gilt dauerhaft — auch wenn der Katalog später einen anderen Vorschlag liefert —,
   und die Satzzeile im Formular zeigt dann beide Werte.
2. **Energiesteuer.** Projektvorgabe für alle Anlagen oder nur diese Anlage; Entlastung keine → 0 ·
   § 53 (Formular 1131) → 5,50 €/MWh, 26.384,3 €/a · § 53a Abs. 5 (Formular 1135) → 4,42 €/MWh,
   21.203,4 €/a · § 54 (Formular 1450) → 1,38 €/MWh − 250 €, 6.370,1 €/a, nur produzierendes Gewerbe;
   Brennstoffaufteilung nur bei § 53 (voller Brennstoff, § 53 Abs. 2 — oder energetisch × 0,458 →
   12.082 €/a, bewusste Untergrenze). Herkunftszeile `ENERGIEST_53A5_ERDGAS = 4,42 €/MWh, gültig ab 2024
   (GESICHERT) — EnergieStG § 53a Abs. 5`; Menge 4.342,1 MWh (H_i) × 11,6 ÷ 10,5 = 4.797,2 MWh (H_s),
   nur der Brennstoff dieser Anlage. Kein Satz von Hand: Der Katalog liefert ihn jahresscharf.
3. **Stromsteuer — Projekt.** Unternehmensart mit Wirkung je Option (kein produzierendes Gewerbe →
   § 9b und § 54 entfallen · produzierendes Gewerbe → § 9b 20,00 €/MWh · Land- und Forstwirtschaft →
   ebenso), Hocheffizienz, räumlicher Zusammenhang ≤ 4,5 km, Modus § 9 Abs. 1 Nr. 3 Ausweis/Erlös.
   Herkunft `STROMST_ENTLASTUNG_9B = 20,00 €/MWh, ab 2026`, `STROMST_SOCKELBETRAG_9B 250 €/a`,
   `STROMST_REGELSATZ = 20,50 €/MWh, ab 2026`; Wirkung 4.750,0 € (Netzbezug 250,0 MWh, Lauf „Beide
   Anlagen") und 23.677,5 € (Ausweis).

Projektweit bleiben in Gruppe 2: Einspeisevergütung KWK-Strom 0,0500 €/kWh · Abschlag Negativstunden
[%] · Pauschale § 9 · Stichtag § 6 · Förderbeginn (Jahr 1 aller Reihen = 2026). **Warnband** in der
Aufklappzeile: „Die Anlage läuft 5.500 h/a, vergütet werden 2026 aber nur 3.300 h — 60 % der Erzeugung.
Weil der Deckel jährlich fällt, reicht das Kontingent über 12 Kalenderjahre."

**Gruppe Steuern des Projekts** — eine Tafel Vorschrift · Satz · Menge · Jahr 1 · Herkunft und
Bedingungen mit drei Zeilen und der Knopf „Wahl und Herkunft…" (dieselbe Überlagerung):

| Vorschrift | Satz | Menge | Jahr 1 (2026) | Herkunft · Bedingungen |
|---|---|---|---|---|
| Energiesteuer § 53a Abs. 5 | 4,42 €/MWh | 4.797,2 MWh (H_s) | 21.203,4 € | Katalog Erdgas ab 2024, Formular 1135 · Nutzungsgrad 83 % ≥ 70 % ✓ · Brennwertmenge 4.342,1 × 11,6 ÷ 10,5 · nur Brennstoff des BHKW |
| Stromsteuer-Entlastung § 9b | 20,00 €/MWh | 250,0 MWh Netzbezug | 4.750,0 € | Katalog ab 2026, Sockel 250 €/a · produzierendes Gewerbe ✓ · hängt am Restbezug, nicht an der Anlage |
| Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 | 20,50 €/MWh | 1.155,0 MWh brutto | 23.677,5 € · Ausweis | Regelsatz ab 2026 · hocheffizient ✓ · ≤ 4,5 km ✓ · P_el ≤ 2 MW ✓ · CO₂ 242,1 < 270 g/kWh ✓ · Modus Ausweis |

Dazu das Feld Jahresnutzungsgrad 83,0 % (Projektgröße, Schwelle 70 % — K5). Die Steuersätze kommen
jahresscharf aus dem Katalog; einen Satz von Hand gibt es nicht. Die Wahl der Entlastung, die
Unternehmensart und der Modus § 9 Abs. 1 Nr. 3 (Erlös/Ausweis, Vorgabe Ausweis — Spalte
`Stromst_Befreiung_Modus`, Schemaschritt 88) stehen in der Überlagerung. Kohärenzzeile in Firebrick, wenn
der erfasste Brennstoffpreis die Energiesteuer nicht ausweist; die Unternehmensart spielt bei §§ 53 und
53a Abs. 5 keine Rolle, sie wirkt nur auf § 54 (Kessel) und § 9b (Netzbezug).

**Gruppe Kohärenzprüfung** — ohne Rechenwirkung:

| Prüfung | Befund |
|---|---|
| Energiesteuer im Gaspreis | 0,0638 €/m³ ausgewiesen · § 53a gewählt ✓ |
| Satz gegen Katalog | 5,50 €/MWh × 11,6 kWh/m³ ÷ 1000 = 0,0638 €/m³ · deckungsgleich ✓ |
| § 9b bei produzierendem Gewerbe | gewählt ✓ |
| Doppelpflege Hilfsenergie | Anlagenanteil 2,0 % *und* Kostenposition aktiv — Warnung |

**Vorschau Jahr 1 (2026)** — die Erlösrubrik des zuletzt gebuchten Laufs, gezeigt wird der Block
Blockheizkraftwerk samt der projektweiten Zeile; die vollständige Rubrik mit Photovoltaik und Block B
steht in `07`:

| Position | Menge × Satz | €/a |
|---|---|---|
| Zuschlag Kraft-Wärme-Kopplung (§ 7) | 52.551,3 € × 0,600 | 31.530,8 |
| davon Einspeisung | 469,0 MWh × 5,5667 ct | 15.664,7 |
| davon Eigenstrom | 1.094,2 MWh × 2,4167 ct | 15.866,1 |
| Energiesteuer-Gutschrift Brennstoff (§ 53a Abs. 5) | 4.797,2 MWh × 4,42 € | 21.203,4 |
| Einspeiseerlös Strom | 469,0 MWh × 5,00 ct | 23.450,0 |
| **Summe Blockheizkraftwerk** | | **76.184,2** |
| projektweit: Stromsteuer-Entlastung Netzbezug (§ 9b) | 250,0 MWh, Lauf „Beide Anlagen" | 4.750,0 |
| Stromsteuer-Befreiung Eigenverbrauch (§ 9 Abs. 1 Nr. 3) — Ausweis, in keiner Summe | 1.155,0 MWh | 23.677,5 |

Die § 9b-Zeile hängt am Restbezug des Laufs: allein mit dem Blockheizkraftwerk (335,5 MWh) sind es
6.460,0 €/a, mit beiden Anlagen (250,0 MWh) 4.750,0 €/a.

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
| § 9 Abs. 1 Nr. 3 | 1.155,0 MWh (brutto) × 20,50 | 23.677,5 €/a | **Ausweis** (Vorgabe, Schemaschritt 88); Erlösreihe nur bei ausdrücklicher Wahl ERLOES, mit Kohärenzwarnung |
| § 9b | max(0, 250,0 × 20,00 − 250) | 4.750,0 €/a | Netzbezug, produzierendes Gewerbe |

## Befunde und offene Punkte

| Nr. | Befund | Behandlung |
|---|---|---|
| ⚠ **K-1** | **Der zweite Fall des § 2 Nr. 16 fehlt:** bei Anlagen mit Vorrichtung zur Abwärmeabfuhr (Notkühler) ist KWK-Strom = Nutzwärme × Stromkennzahl, nicht die Nettostromerzeugung; EPOS-Plan führt weder Kennzeichen noch Stromkennzahl und rechnet immer Fall 1 — Zuschlag für Notkühler-Anlagen **zu hoch** | **entschieden 18.09.2026 nach Empfehlung**: Kennzeichen und Stromkennzahl je Anlage (Schemaschritt 90, Vorschlag der Stromkennzahl aus P_el/P_th am Feld), Fall 2 in der Mengenbildung je Anlage; kein Referenzprojekt betroffen — Konzept § 3.6 |
| ✔ B-1 | § 9 Abs. 1 Nr. 3 als Erlösreihe gebucht — es entsteht aber gar keine Stromsteuer; gemessen 1.510,84 €/a auf beiden Pfaden (Projekt 1024) | umgesetzt mit B6: Ausweis (`Stromst_Befreiung_Modus`, Vorgabe AUSWEIS, Schemaschritt 88); der Messwert zu 1024 ist am heutigen Stand der Testdatenbank nicht nachstellbar (Hocheffizienznachweis 0) |
| ✔ K3 | Modusfeld § 9 Nr. 3 | erledigt mit B6: Schemaschritt 88, Feld offen, Vorgabe AUSWEIS |
| K4 | Tabellenspalte „Brennstoff" ohne Leseweg | kleiner Leser `CarrierId` → Name in B5 |
| K7 | Schreibweg der drei B5-Spalten fehlt (`KwkgAnlagenCtrl.Speichere` = 8 Spalten) | auf 11 Spalten erweitern — B5-Kernaufgabe |
| R-U1 | § 53 neben § 53a — Entweder-oder | als Auswahl modelliert, mit dem Hauptzollamt zu klären |
| R-U3 | Ausschluss fossiler flüssiger Brennstoffe (nur Sekundärquelle) | als Prüfkette „Heizöl-Neuanlage ab 2025" umgesetzt |
| ⚠ **S-1** | **Hilfsstrom-Netting des Beispiels widerspricht dem Rechenkern:** Die Mengentafel teilt die Nettostromerzeugung 1.563,2 MWh im Verhältnis 70/30 (1.094,2 / 469,0 MWh, `Beispielprojekt.md` § 3); `HilfsstromRechner.NettoSplit` und die Formelkarte ziehen den Hilfsstrom **zuerst vom Eigenverbrauch** ab (Eigen' = 1.155,0 − 86,8 = 1.068,2 · Einsp' = 495,0 MWh — „Physik, keine Konvention"). Wirkung: Zuschlag Jahr 1 32.022,2 statt 31.530,8 €, Einspeiseerlös 24.750,0 statt 23.450,0 € (der Rechenkern bewertet die Einspeisung der Strommatrix, `KwkEinspeisungGesamtMWh`) | **Entscheid ausstehend** (Mockup, Anhang Umsetzungsstand U24): Folgt das Beispiel der Kernregel, sind die Zahlenproben der Abschnitte 5, 7 und 8 und `Beispielprojekt.md` neu zu rechnen; Katalog und Grundlagenpapier sagen zur Reihenfolge nichts, deshalb hier nicht geändert |
| U22 | Überlagerung „Sätze und Herkunft" statt Klapplisten und Knöpfen „Vorschlag übernehmen" am Feld (Anwenderwunsch 18.09.2026) | Mockup Abschnitt 5; Konzept § 2.2 („Der Vorschlag steht am Feld, nicht als Sammelknopf") ist damit überholt und nachzuziehen |
