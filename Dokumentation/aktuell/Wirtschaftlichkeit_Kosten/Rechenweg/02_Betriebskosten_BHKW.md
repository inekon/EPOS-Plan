# 02 · Betriebskosten BHKW

**Dialog:** `KostenKomponenteDialog`, Optionsgruppe „Betriebskosten" — Entwurf B (Konzept § 2.8) · **Mockup:**
`../../Mockups/Dialog_Formel_Zahlenprobe.html#betrieb` · **Norm:** VDI 2067 · **Code:**
`BetriebskostenCtrl.Betrag`, `EndenergieAufloeser`, `DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN` /
`_ENDENERGIEBEDARF` · **Konzept:** § 2.8, § 3.4 · **umgesetzt:** Etappe H1 (Pflichtpositionen,
Schemaschritt 59)

## Was der Dialog zeigt

Dasselbe Fenster wie bei den Investitionskosten (`01`), die Optionsgruppe steht auf „Betriebskosten":
Betragsspalte „Betrag netto [€/a]", Spalte Nutzungsdauer leer, Bemessungen des Betriebsrasters (fester
Jahresbetrag · % der Investition · % der Endenergiekosten · % des Endenergiebedarfs · je kWh thermisch · je kWh
elektrisch; „je kWh" und „je Stunde" nur, wo eine Bestandszeile sie trägt). Die drei Pflichtzeilen nach VDI 2067
(Wartung BHKW, Instandhaltung BHKW, Hilfsenergiekosten — Schemaschritt 59) tragen an Stelle des Papierkorbs ein
Schloss mit dem Werkzeugtipp „Pflichtposition, kann nicht gelöscht werden"; jeder Löschversuch — über das
Schloss, die Tastatur oder den Zeileneditor — antwortet „… ist eine Pflichtposition dieser Komponente und kann
nicht gelöscht werden. Zum Deaktivieren den Satz bzw. Betrag auf 0 setzen." Der Empfehlungsbereich steht als
leise Zeile unter dem Satzfeld („Empfehlung 1 bis 2 %") und zusätzlich in dessen Werkzeugtipp
(„Empfehlung: 1 – 2 %"), die Bezugsgröße im Werkzeugtipp des Betragsfeldes („1,50 % von 240.772,40 €"). Unter dem
Betrag jeder gerechneten Zeile steht Bezugsgröße und Herkunft als leise Zeile („× 1.650.000,00 kWh · Lauf");
eine Runde nennt sie hier nicht, weil die Betriebsseite keine Kaskade kennt. Absolute
Positionen spiegeln den Satz im Betrag (🔗).

Über dem Raster steht, aus welchem Simulationslauf die Mengen stammen („Mengen stammen aus dem Simulationslauf
vom 30.08.2026 06:11") — ohne gespeichertes Ergebnis der Grund „kein Simulationslauf". Unter dem Raster nennt
die Gruppe **Endenergie je Komponente** je Anlage mit Endenergie den Jahresbedarf [kWh/a], die Arbeitskosten
[€/a] und die Herkunft der Menge; sie ist die Bezugsgröße der Bemessungen „% des Endenergiebedarfs" und „% der
Endenergiekosten". Fehlt der Arbeitspreis eines beteiligten Trägers, steht in der Kostenspalte ein
Gedankenstrich, nie eine 0. Anlagen ohne Endenergie (Photovoltaik, Solarthermie, Speicher) bekommen keine Zeile
— dort ist nur der feste Jahresbetrag zulässig.

**Der Elektrokessel in dieser Gruppe.** Ein Heizkessel, dessen Gerät den Brennstoff „Elektrische Energie“
führt, bezieht Strom, nicht Brennstoff. Seine Zeile nennt deshalb seinen **Stromeinsatz** — die Nutzwärme des
Laufs, denn der Rechenkern führt ihn mit Nutzungsgrad 1 —, bewertet ihn mit dem Arbeitspreis des Stromträgers
und nennt als Herkunft „Strom · Netzbezug (im Reststrombedarf des Projekts bepreist)“. Die Menge ist damit
sichtbar, **bezahlt wird sie genau einmal**: im Netzbezug des Projekts; seine Brennstoffspalte bleibt bei 0.
Führt ein Projekt Brennstoff- und Elektrokessel, stehen sie als zwei Zeilen — zwei Energieformen mit zwei
Preisen ergeben keine gemeinsame Bezugsgröße.

| Position | Bemessung | Satz | Bezugsgröße (Werkzeugtipp) | Betrag |
|---|---|---|---|---|
| Wartung BHKW — Pflicht · Empfehlung 0,02–0,04 €/kWh | je kWh elektrisch | 0,0280 €/kWh | 1.650.000,00 kWh · BHKW 1 | 46.200,00 |
| Instandhaltung BHKW — Pflicht · Empfehlung 1,00–2,00 % | % der Investition | 1,50 % | 240.772,40 € · Investition BHKW 1 | 3.611,59 |
| Hilfsenergiekosten — Pflicht · Empfehlung 2,00–4,00 % | % der Endenergiekosten | 2,00 % | 312.631,20 € Endenergiekosten · BHKW 1 → 21.710 kWh Strom | 6.252,62 |
| Versicherung | fester Jahresbetrag | 1.100,00 €/a | — (Satz = Betrag) | 1.100,00 |
| **Summe Betriebskosten netto** | | | brutto 68.025,41 €/a | **57.164,21** |

**Doppelpflege-Warnung** über dem Raster (Text `KOH_HILFSENERGIE_DOPPELT`, wortgleich aus der Kohärenzprüfung —
sie erscheint nur, wenn die Anlage einen Hilfsenergieanteil > 0 **und** eine aktive Hilfsenergie-Kostenposition führt):
„Hilfsenergie doppelt gepflegt (Menge an der Anlage und Kostenposition Hilfsenergiekosten): BHKW 1 führt einen
Hilfsenergieanteil von 2,00 % und zugleich eine aktive Hilfsenergie-Kostenposition. Die Mengenangabe mindert den
KWK-Zuschlag, die Kostenposition belastet die Betriebskosten — verrechnet wird nichts."

**Ohne Simulationslauf** tragen die mengenbasierten Zeilen ein ⚠ im Betrag, und unter dem Raster steht
„‚Wartung BHKW': kein Simulationslauf · ‚Hilfsenergiekosten': kein Simulationslauf"; investitionsbasierte Sätze
rechnen sofort.

## Berechnungsgrundlage

```
Sperre zuerst: fehlt Menge ODER Satz  ⇒  Betrag = 0   (nicht der gespeicherte Wert)

  A  absolut   BETRAG, JAHRESBETRAG   Betrag = eingegebener Wert
  B  Prozent   PROZENT_*              Betrag = Menge × Satz / 100
  C  Produkt   EUR_PRO_*              Betrag = Menge × Satz

Vorrang der Bezugsmenge (frisch vor Konserve, H2-1)
  1. Szenariowert gepflegt → keine Ableitung
  2. BETRAG / leer → gespeicherter Wert
  3. Endenergie-Arten (PROZENT_ENDENERGIEKOSTEN / _BEDARF): Menge IMMER frisch aus dem
     jüngsten Lauf (höchste Tab_Ergebnis.ID); Auflöser null ⇒ Betrag 0 — die Konserve greift nie
  4. Rückfall-ermittelbare Arten (9 Stück): frisch versuchen, Konserve nur bei null
  5. Übrige Arten (EUR_PRO_H, EUR_PRO_KWH, PROZENT_BRENNSTOFF-/STROMKOSTEN): nur Konserve (B-4)

Endenergie je Komponente (EndenergieAufloeser)
  BHKW, Kessel   Bedarf = Σ Verbrauch × 1000          Kosten = Bedarf × Arbeitspreis(CarrierId)
  Elektrokessel  Bedarf = Σ (Waerme_Gas + Waerme_Oel) × 1000    Kosten = Bedarf × Strompreis
  Wärmepumpe     Bedarf = Σ (Stromverbrauch + Heizstab) × 1000   Kosten = Bedarf × Strompreis
  PV · Solarthermie · Speicher    null — nur Jahresbetrag zulässig
  Arbeitspreis = PreisArbeit / EffHi   (ohne Grund- und Leistungspreis)

Vorrangregel Prozent vor Absolut (KL4): gepflegter Satz schlägt Absolutbetrag;
  das unterlegene Feld wird GESPERRT, nicht geleert.
Erlöse: IstErloes && wert > 0 → wert = −wert  (an drei Stellen identisch geklemmt)
```

**Hilfsenergie-Definition (29.08.2026):** immer Strom, bemessen an der **Endenergie der Anlage** —
Weg A: % der Endenergiekosten (BHKW, Kessel: Brennstoff × Trägerpreis; Wärmepumpe: Strom ×
Bezugspreis) · Weg B: % des Endenergiebedarfs (kWh) · Weg C: fester Jahresbetrag. Solarthermie,
Puffer-, Stromspeicher und PV: **nur absolut**. Weg B braucht keine zweite Formel — der Auflöser
übergibt den bewerteten Bedarf; die Sätze von A und B sind nicht austauschbar (Faktor ≈ 3,4, das
Preisverhältnis Strom zu Brennstoff).

**Basis „% der Investition" auf der Betriebsseite** (`InvestSummeFuer`): `SUM(EingegebenerWert)`
Kategorie 1 ohne Zuschuss, stufig Anlage → Komponente → Projekt, **vor** Zuschussabzug —
abgeleitete Beträge fehlen dort (Befund B-5). Im Mockup ist der Kaskadenbetrag 240.772,40 € gezeigt,
wie er nach Behebung von B-5 anzusetzen wäre.

## Berechnungserläuterung am Beispielprojekt

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| Wartung | 1.650.000 kWh × 0,0280 €/kWh | 46.200,00 €/a | Gruppe C, Menge aus dem Lauf |
| Instandhaltung | 240.772,40 × 1,50 / 100 | 3.611,59 €/a | Gruppe B, Basis Investition der Anlage |
| 1 Endenergiemenge | 4.342,1 MWh × 1000 | 4.342.100 kWh | Brennstoff des BHKW aus dem jüngsten Lauf |
| 2 Arbeitspreis | 0,7560 €/m³ ÷ 10,5 kWh/m³ | 0,0720 €/kWh | Heizwert als Umrechnung, keine η-Division |
| **Endenergiekosten** | 4.342.100 × 0,0720 | **312.631,20 €/a** | Bezugsgröße der Prozentzeile |
| 3 Hilfsenergie 2 % | 312.631,20 × 2,00 / 100 | 6.252,62 €/a | Weg A |
| 4 Rückrechnung Strom | 6.252,62 € ÷ 0,288 €/kWh | 21.710 kWh/a | Plausibilität, ohne Rechenwirkung |
| Versicherung | Jahresbetrag | 1.100,00 €/a | Gruppe A |
| **Betriebskosten Jahr 1** | 46.200,00 + 3.611,59 + 6.252,62 + 1.100,00 | **57.164,21 €/a** | steigt mit p_B ab Jahr 2; brutto × 1,19 = 68.025,41 |

Der Hilfsenergie-Satz von 2 % am **Brennstoff** entspricht 21.710 kWh Strom = 1,3 % der
Bruttostromerzeugung. Ein Wärmepumpen-Satz von 2 % würde direkt an Stromkosten bemessen — deshalb
sind die Prozentwerte verschiedener Anlagen nicht vergleichbar.

## Befunde und offene Punkte

| Nr. | Befund | Behandlung im Entwurf |
|---|---|---|
| ⚠ B-1 | **Kessel-Endenergie ist strukturell 0** — der Rechenkern setzt `Verbrauch` nie; Endenergie-Positionen am Kessel liefern 0 € | Herleitungszeile zeigt „× 0 kWh" und macht den Befund sichtbar; Behebung: Verbrauch aus dem Lauf nachziehen |
| B-3 | „Jüngster Lauf" ist die höchste ID, nicht der Zeitstempel | Banner nennt Datum und Uhrzeit des Laufs |
| B-5 | `InvestSummeFuer` summiert `EingegebenerWert` — abgeleitete Beträge fehlen | Mockup zeigt den Kaskadenbetrag; Umsetzung muss auf den Kaskadenbetrag umstellen |
| B-6 | Fehler werden geschluckt (`catch {}` ⇒ still 0) | Strich statt 0, Warnzeile |
| B-7 | `MengenEinheit` beschriftet die neuen Arten mit „€" | Herleitungszeile nennt kWh bzw. € ausdrücklich |
| K10 | Hilfsenergie-Bemessung doppelt: Seed gegen Altkatalog | in B5/B6 nachziehen |
