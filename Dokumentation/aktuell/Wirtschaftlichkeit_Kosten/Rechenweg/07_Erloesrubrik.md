# 07 · Erlösrubrik

**Ort:** Ergebnisreiter, Bericht, Vorschau des BHKW-Dialogs (Konzept § 2.6, Entscheidung D-2) ·
**Mockup:** `../../Mockups/Dialog_Formel_Zahlenprobe.html#erloese` · **Code:** `StromMatrix`
(Differenzmethode), `WirtschaftlichkeitZeilen` · **Konzept:** § 2.6, § 3.6 (Vermiedene
Stromkosten), § 3.8

Alle Erlöse an einem Ort — mit einer Trennung, die keine Kosmetik ist: **Block B darf nicht addiert
werden.** Wer vermiedene Stromkosten neben den Reststrombetrag stellt und beides summiert, zählt
dieselbe Ersparnis zweimal.

## Was die Rubrik zeigt — Jahr 1 (2026), netto

**Block A — zahlungswirksam, geht in den Kapitalwert**

| Position | Rechtsgrundlage | Menge | Satz | €/a | Laufzeit |
|---|---|---|---|---|---|
| KWK-Bonus Einspeisung | § 7 Abs. 1 KWKG | 495,0 MWh | 5,5667 ct | 16.533,1 | Vbh-Kontingent · Deckel |
| KWK-Bonus Eigenstrom | § 7 Abs. 2 KWKG | 1.068,2 MWh | 2,4167 ct | 15.489,1 | zusätzlich Tatbestand § 6 Abs. 3 |
| Energiesteuer BHKW-Brennstoff | § 53a Abs. 5 EnergieStG | 4.797,2 MWh (H_s) | 4,42 €/MWh | 21.203,4 | dauerhaft · jährlicher Antrag |
| Stromsteuer-Entlastung Netzbezug | § 9b StromStG | 250,0 MWh | 20,00 €/MWh | 4.750,0 | nur produzierendes Gewerbe |
| Einspeiseerlös Strom | Tarif / Projektwert | 495,0 MWh | 5,00 ct | 24.750,0 | nominal konstant |
| PV-Vergütung | § 21 · § 51 EEG | 159,6 von 199,5 MWh | Reihe (`06`) | 9.001,4 | 20 a + IBN-Monate |
| **Summe Block A** | | | | **91.727,0** | |

Nicht im Beispiel, aber Teil der Rubrik: KWKG-Pauschale § 9 (≤ 2 kW_el, einmalig, schließt A1/A2
aus) · Energiesteuer Kesselbrennstoff § 54 (nur produzierendes Gewerbe, Sockel 250 €/a) · Restwert
(DIN EN 17463, Ende des Betrachtungszeitraums).

**Block B — Ausweis, nicht addieren**

| Position | Rechtsgrundlage | Menge | Satz | €/a | Warum kein Zahlungsstrom |
|---|---|---|---|---|---|
| Stromsteuer-Befreiung Eigenverbrauch (Block B, sofern nicht ausdrücklich ERLOES gewählt) | § 9 Abs. 1 Nr. 3 StromStG | 1.155,0 MWh | 20,50 €/MWh | 23.677,5 | es entsteht gar keine Stromsteuer — der Vorteil steckt in der kleineren Bezugsrechnung |
| Vermiedene Stromkosten — Arbeit | Differenzmethode | 1.179,7 MWh | 28,80 ct | 339.753,6 | steckt im Reststrombetrag, der in den Kapitalwert geht (E5, fünffach belegt) |
| abzüglich entgangener § 9b-Entlastung | § 9b StromStG | 1.179,7 MWh | 20,00 €/MWh | − 23.594,0 | bei produzierendem Gewerbe |
| **vermiedene Kosten effektiv** | | | | **316.159,6** | |
| Vermiedene Stromkosten — Leistung | Differenzmethode | — | — | − 4.180,0 | regelmäßig **negativ** — Kernaussage, kein Fehler |
| PV: vermiedener Bezug, Kappungs- und Ausfallmengen | | 85,5 MWh | | 24.624,0 | dito bzw. Mengenausweis |

Kennzeichnung im Dialog: Vermerk `[Ausweis]` je Zeile, Summenzeile nur über Block A. In der
**Differenzsicht** gegen ein Vergleichsprojekt (§ 2.9) sind vermiedene Bezüge reguläre
Differenz-Cashflows — die Referenzkosten laufen dort als Gegenposition; die Block-B-Kennzeichnung
gilt für die **Absolutsicht**.

**Drei Tiefen, drei Fragen.** Die Summe je Block und Komponente sagt, wie viel zusammenkommt; die
Zeile nennt Menge, Satz und Herkunft in **einer** Spalte „Satz · Herkunft" (etwa „495,0 MWh × 5,5667 ct
· Abs. 1 · Vorschlag, Katalog 2026"); Wahl und Herleitung stehen in der Überlagerung „Sätze und
Herkunft" des BHKW-Dialogs (`05`). Ein von Hand gesetzter Satz steht in der Spalte als „eigener Wert
6,00 — Vorschlag 5,5667". Block B zeigt je Komponente **eine** Zeile „Vermiedene Stromkosten wirksam"
und trägt die Kette brutto − entgangene § 9b-Entlastung in der Herkunftsspalte; die Tabelle oben ist
die ausgeschriebene Fassung derselben Zeilen.

## Satz, Menge und Handeingabe je Zeile

Je Zeile des Blocks A: woher der Satz kommt, welche Menge er trifft und was eine Handeingabe oder eine
spätere Katalogänderung bewirkt — gemessen am Rechenkern (`KwkgSatzRechner`, `WirtschaftlichkeitCtrl`,
`SteuerGutschriftRechner`, `GesetzKatalog`).

| Zeile | Satz aus | Menge | Handeingabe · Katalogänderung |
|---|---|---|---|
| KWK-Zuschlag Einspeisung | `Tab_Energieanlagen.KWKG_Satz_Einspeisung` — das Feld **ist** der Satz (leer/0 = kein Zuschlag, kein Rückfall); der Vorschlag 5,5667 ct/kWh kommt aus `KwkgSatzRechner.Vorschlag` (Katalog `KWKG_ZUSCHLAG_EINSPEISUNG_*`, Stichtagsjahr = Inbetriebnahmejahr der Anlage, marginale Tranchen) und wird nur beim Übernehmen geschrieben | KWK-Einspeisung nach `HilfsstromRechner.NettoSplit` — der Hilfsstrom mindert zuerst den Eigenverbrauch, im Beispiel bleibt sie bei 495,0 MWh — × Deckelanteil des Jahres | Ein eigener Wert gilt dauerhaft und ungeprüft gegen die Staffel; eine Katalogänderung ändert nur den Vorschlag, nie den gespeicherten Satz. Der Modulnachweis führt Satz und Vorschlagsherleitung nebeneinander (`SatzAusAnlage`); ein Vergleich beider ist noch nicht gezeichnet (U23) |
| KWK-Zuschlag Eigenstrom | `KWKG_Satz_Eigen` ebenso; Vorschlag 2,4167 ct/kWh nach dem Tatbestand `KWKG_Eigenstromfall` (`KWKG_ZUSCHLAG_EIGEN_N2_*`) | KWK-Eigenverbrauch **netto** (brutto 1.155,0 − Hilfsstrom 86,8 = 1.068,2 MWh) × Deckelanteil | wie Einspeisung; zusätzlich: Tatbestand `KEINER` ⇒ 0 mit Meldung, leerer Tatbestand ⇒ Satz bleibt, Meldung „ungeprüft"; Nr. 1 über 100 kW ⇒ Vorschlag 0 |
| Energiesteuer-Gutschrift Brennstoff | **nur Katalog**, jahresscharf: `ENERGIEST_53A5_ERDGAS` 4,42 €/MWh ab 2024 (§ 53a Abs. 5); die Wahl (`Energiesteuer_Wahl`, Anlage ?? Projekt) bestimmt den Schlüssel | gesamter BHKW-Brennstoff der Anlage, **brennwertbezogen**: 4.342,1 MWh (H_i) × eff_hs/eff_hi = 4.797,2 MWh (H_s); kein Kesselbrennstoff (Kessel mit § 53/53a ⇒ 0 mit Begründung); Nutzungsgrad 83 % ≥ 70 % | Keinen Satz von Hand; wer den Satz ändern will, pflegt die Katalogzeile (global, mit Herkunft im Ergebnis: `STEUER_HERKUNFT_FORMAT`). Eine neue Katalogzeile mit `JahrVon` wirkt beim nächsten Lauf im betreffenden Jahr von selbst |
| Stromsteuer-Entlastung Netzbezug | nur Katalog: `STROMST_ENTLASTUNG_9B` 20,00 €/MWh ab 2026, Sockel `STROMST_SOCKELBETRAG_9B` 250 €/a | Netzbezug des Laufs (Restbezug **mit** Anlagen) | kein Satz von Hand; Bedingung Unternehmensart (Projekt); Katalogänderung wirkt jahresscharf |
| Stromsteuer-Befreiung Eigenverbrauch (Block B) | nur Katalog: `STROMST_REGELSATZ` 20,50 €/MWh ab 2026 | KWK-Eigenverbrauch **brutto** aus der Strommatrix × Anteil der bestandenen Anlagen | kein Satz von Hand; Hocheffizienz, räumlicher Zusammenhang, Modus (Projekt); ohne Stundenreihen 0 mit Begründung |
| Einspeiseerlös Strom | reiner Projektwert `Einspeiseverguetung_KWK` 0,0500 €/kWh (Zonen-/Rollentarif kann ihn ersetzen); kein Katalog, kein Vorschlag | KWK-Einspeisung der Strommatrix (`KwkEinspeisungGesamtMWh`) | Handeingabe ist der einzige Weg; leer oder 0 ⇒ keine KWK-Einspeiseerlöse, ohne Meldung; nominal konstant |
| Energiesteuer-Entlastung Heizstoff (Kessel) | nur Katalog: `ENERGIEST_54_ERDGAS` 1,38 €/MWh ab 2024, Sockel 250 €/a einmal je Lauf | Kesselbrennstoff brennwertbezogen 2.272,3 MWh (H_s) | kein Satz von Hand; Bedingung Unternehmensart |
| Vermiedene Netzentgelte (§ 18 StromNEV) | — | — | keine Zeile: der Rechenkern führt sie nicht, und für eine Anlage mit Inbetriebnahme 2026 gibt es sie nicht mehr (Auslaufregel des EnWG); die Rubrik ist hier vollständig |

Der § 53a-Satz 4,42 €/MWh ist der **Erstattungsbetrag** (80 % des vollen Satzes 5,50 €/MWh), nicht die
Reststeuer — Grundlagen § 3.3 und `GesetzKatalog`; Erdgas wird steuerlich brennwertbezogen bemessen
(Grundlagen § 3.5). Der Faktor 1,1048 ist die auf vier Stellen gerundete Schreibweise von 11,6 ÷ 10,5;
der Rechenkern rechnet mit dem ungerundeten Quotienten (Unterschied 0,5 €/a im Beispiel).

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

Die vermiedene Menge ist die **physisch** vermiedene — Bedarf ohne Anlage minus Restbezug der Strommatrix
(1.429,7 − 250,0 = 1.179,7 MWh, davon 85,5 MWh Photovoltaik) —, nicht die brutto bemessene § 9-Menge und nicht
die KWKG-Menge (`Beispielprojekt.md` § 3). Der Hilfsstrom berührt sie nicht: Die Strommatrix ist die Brutto-Welt,
das Netting wirkt allein auf den Zuschlag (`WirtschaftlichkeitCtrl`, Kommentar zu `BaueKwkgReihe`).

Im Kapitalwert ist das bereits korrekt: Die § 9b-Reihe rechnet auf den kleineren Netzbezug und
fällt dadurch automatisch geringer aus. Falsch war bisher nur der **Ausweis** — er zeigte den Vorteil
um 2,00 ct/kWh zu hoch. Die Rubrik zeigt deshalb beide Zeilen: „vermiedene Kosten brutto" und
darunter „abzüglich entgangener § 9b-Entlastung", mit dem effektiven Betrag als Ergebnis.

## Befunde und offene Punkte

| Nr. | Punkt | Behandlung |
|---|---|---|
| ✔ B-1 | § 9 Abs. 1 Nr. 3 als Erlösreihe gebucht; im Bestand buchte kein Lauf die Reihe — nirgends wirksam | umgesetzt mit B6: Ausweis (Block B), Erlösreihe nur bei ausdrücklicher Wahl ERLOES |
| — | Gliederung der Rubrik nach Komponente (Anwenderdurchsicht 18.09.2026) | A/B bleibt die äußere Ordnung, die Komponente gliedert innen, Positionen ohne Anlagenbezug im Block „projektweit" — Konzept § 2.13, Mockup `../../Mockups/Dialog_Formel_Zahlenprobe.html#erloese` |
| E5 | Doppelzählung vermiedener Kosten | Block B nie addieren; Summenzeile nur Block A |
| D-2 | eigene Rubrik in zwei Blöcken | entschieden 30.08.2026 |
| — | Leistungsanteil der vermiedenen Kosten negativ | als Kernaussage ausweisen, nicht unterdrücken |
| — | Die Vorschau des BHKW-Dialogs führte eine Summe „zahlungswirksam" (80.934,2 €), die weder der Variante 1 (84.435,6 €, § 9b auf 335,5 MWh) noch der Variante 3 (91.727,0 €, mit Photovoltaik) entsprach | Vorschau zeigt den Block Blockheizkraftwerk (77.975,6 €) und die projektweite § 9b-Zeile des Laufs „Beide Anlagen" getrennt — Mockup Abschnitt 5, `05` |
| — | Spalte „Satz · Herkunft" je Zeile, Vermerk „eigener Wert — Vorschlag" | Mockup Abschnitt 7; Umsetzungsstand U23 (Vergleich Satz gegen Vorschlag im Nachweis) |
| ✔ S-1 | Hilfsstrom-Netting des Beispiels gegen die Kernregel „Eigen zuerst" — Zuschlag- und Einspeisezeile betroffen | erledigt (U24): Zuschlag 32.022,2 €, Einspeiseerlös 24.750,0 €, Block A der Variante 3 91.727,0 €/a; Einzelheiten in `05` |
