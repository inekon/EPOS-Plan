# 07 · Erlösrubrik

**Ort:** Ergebnisreiter, Bericht, Vorschau des BHKW-Dialogs (Konzept § 2.6, Entscheidung D-2) ·
**Mockup:** `../Mockups/Dialog_Formel_Zahlenprobe.html#erloese` · **Code:** `StromMatrix`
(Differenzmethode), `WirtschaftlichkeitZeilen` · **Konzept:** § 2.6, § 3.6 (Vermiedene
Stromkosten), § 3.8

Alle Erlöse an einem Ort — mit einer Trennung, die keine Kosmetik ist: **Block B darf nicht addiert
werden.** Wer vermiedene Stromkosten neben den Reststrombetrag stellt und beides summiert, zählt
dieselbe Ersparnis zweimal.

## Was die Rubrik zeigt — Jahr 1 (2026), netto

**Block A — zahlungswirksam, geht in den Kapitalwert**

| Position | Rechtsgrundlage | Menge | Satz | €/a | Laufzeit |
|---|---|---|---|---|---|
| KWK-Bonus Einspeisung | § 7 Abs. 1 KWKG | 469,0 MWh | 5,5667 ct | 15.664,7 | Vbh-Kontingent · Deckel |
| KWK-Bonus Eigenstrom | § 7 Abs. 2 KWKG | 1.094,2 MWh | 2,4167 ct | 15.866,1 | zusätzlich Tatbestand § 6 Abs. 3 |
| Energiesteuer BHKW-Brennstoff | § 53a Abs. 5 EnergieStG | 4.797,2 MWh (H_s) | 4,42 €/MWh | 21.203,4 | dauerhaft · jährlicher Antrag |
| Stromsteuer-Entlastung Netzbezug | § 9b StromStG | 250,0 MWh | 20,00 €/MWh | 4.750,0 | nur produzierendes Gewerbe |
| Einspeiseerlös Strom | Tarif / Projektwert | 469,0 MWh | 5,00 ct | 23.450,0 | nominal konstant |
| PV-Vergütung | § 21 · § 51 EEG | 199,5 MWh | Reihe | 10.396,3 | 20 a + IBN-Monate |
| **Summe Block A** | | | | **91.330,5** | |

Nicht im Beispiel, aber Teil der Rubrik: KWKG-Pauschale § 9 (≤ 2 kW_el, einmalig, schließt A1/A2
aus) · Energiesteuer Kesselbrennstoff § 54 (nur produzierendes Gewerbe, Sockel 250 €/a) · Restwert
(DIN EN 17463, Ende des Betrachtungszeitraums).

**Block B — Ausweis, nicht addieren**

| Position | Rechtsgrundlage | Menge | Satz | €/a | Warum kein Zahlungsstrom |
|---|---|---|---|---|---|
| Stromsteuer-Befreiung Eigenverbrauch (wandert mit B6 aus Block A hierher) | § 9 Abs. 1 Nr. 3 StromStG | 1.155,0 MWh | 20,50 €/MWh | 23.677,5 | es entsteht gar keine Stromsteuer — der Vorteil steckt in der kleineren Bezugsrechnung |
| Vermiedene Stromkosten — Arbeit | Differenzmethode | 1.179,7 MWh | 28,80 ct | 339.753,6 | steckt im Reststrombetrag, der in den Kapitalwert geht (E5, fünffach belegt) |
| abzüglich entgangener § 9b-Entlastung | § 9b StromStG | 1.179,7 MWh | 20,00 €/MWh | − 23.594,0 | bei produzierendem Gewerbe |
| **vermiedene Kosten effektiv** | | | | **316.159,6** | |
| Vermiedene Stromkosten — Leistung | Differenzmethode | — | — | − 4.180,0 | regelmäßig **negativ** — Kernaussage, kein Fehler |
| PV: vermiedener Bezug, Kappungs- und Ausfallmengen | | 85,5 MWh | | 24.624,0 | dito bzw. Mengenausweis |

Kennzeichnung im Dialog: Vermerk `[Ausweis]` je Zeile, Summenzeile nur über Block A. In der
**Differenzsicht** gegen ein Vergleichsprojekt (§ 2.9) sind vermiedene Bezüge reguläre
Differenz-Cashflows — die Referenzkosten laufen dort als Gegenposition; die Block-B-Kennzeichnung
gilt für die **Absolutsicht**.

## Berechnungsgrundlage

```
Vermiedene Stromkosten — Differenzmethode
  Bezug     = Rollenkosten(Bezugstarif,    Bedarf OHNE Anlage)
  Reststrom = Rollenkosten(Reststromtarif, Restbezug MIT Anlage)
  Vermieden = Bezug − Reststrom            je Arbeit / Leistung / Gesamt
  In den Kapitalwert geht der Reststrombetrag. Die Differenz zusätzlich zu buchen wäre
  Doppelzählung — fünffach belegt (E5).

Korrektur um die entgangene Entlastung (Klarstellung 1, Konzept § 2.6)
  Vermieden_effektiv = Vermieden_brutto − Entlastungssatz(§ 9b) × vermiedene Menge
                       nur produzierendes Gewerbe / Land- und Forstwirtschaft

Welche Vorschrift hängt an der Unternehmensart (Klarstellung 2)
  § 53, § 53a Abs. 5 EnergieStG (BHKW-Brennstoff)     NEIN
  § 54 EnergieStG (Heizstoffe, Kessel)                 JA
  § 9b StromStG (Netzbezug)                            JA
  Im Code prüft ProduzierendesGewerbe genau zwei Stellen: § 54 und § 9b.
```

## Berechnungserläuterung am Beispielprojekt

Die Differenzmethode rechnet beide Seiten mit demselben Arbeitspreis — und der enthält die
Stromsteuer mit 20,50 €/MWh. Ein Unternehmen des produzierenden Gewerbes bekommt davon nach § 9b
20,00 €/MWh zurück. Tatsächlich vermieden werden also nur **0,50 €/MWh** Stromsteuer, nicht 20,50.

| Größe | Menge | Satz | Betrag | Wirkung |
|---|---|---|---|---|
| Strombedarf ohne Anlage | 1.429,7 MWh | 28,80 ct | 411.753,6 €/a | hypothetisch |
| Restbezug mit Anlage | 250,0 MWh | 28,80 ct | 72.000,0 €/a | **Kapitalwert** (Energiekosten, `04`) |
| **Vermieden brutto** | 1.179,7 MWh | — | **339.753,6 €/a** | Ausweis |
| entgangene § 9b-Entlastung | 1.179,7 MWh | 20,00 €/MWh | − 23.594,0 €/a | im Ausweis heute nicht abgezogen |
| **Vermieden effektiv** | — | — | **316.159,6 €/a** | Ausweis |

Die vermiedene Menge ist die **physisch** vermiedene — netto Eigenverbrauch BHKW (1.094,2) plus
PV-Eigenverbrauch (85,5) —, nicht die brutto bemessene § 9-Menge (`Beispielprojekt.md` § 3).

Im Kapitalwert ist das bereits korrekt: Die § 9b-Reihe rechnet auf den kleineren Netzbezug und
fällt dadurch automatisch geringer aus. Falsch war bisher nur der **Ausweis** — er zeigte den Vorteil
um 2,00 ct/kWh zu hoch. Die Rubrik zeigt deshalb beide Zeilen: „vermiedene Kosten brutto" und
darunter „abzüglich entgangener § 9b-Entlastung", mit dem effektiven Betrag als Ergebnis.

## Befunde und offene Punkte

| Nr. | Punkt | Behandlung |
|---|---|---|
| B-1 | § 9 Abs. 1 Nr. 3 heute Erlösreihe; im Bestand bucht kein Lauf die Reihe — nirgends wirksam | Umstellung auf Ausweis mit B6 (Block B) |
| E5 | Doppelzählung vermiedener Kosten | Block B nie addieren; Summenzeile nur Block A |
| D-2 | eigene Rubrik in zwei Blöcken | entschieden 30.08.2026 |
| — | Leistungsanteil der vermiedenen Kosten negativ | als Kernaussage ausweisen, nicht unterdrücken |
