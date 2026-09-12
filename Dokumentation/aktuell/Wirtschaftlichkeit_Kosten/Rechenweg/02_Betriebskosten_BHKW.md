# 02 · Betriebskosten BHKW

**Dialog:** `Form_KostenKomponente`, Reiter Betrieb — Entwurf B (Konzept § 2.8) · **Mockup:**
`../Mockups/Dialog_Formel_Zahlenprobe.html#betrieb` · **Norm:** VDI 2067 · **Code:**
`BetriebskostenCtrl.Betrag`, `EndenergieAufloeser`, `DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN` /
`_ENDENERGIEBEDARF` · **Konzept:** § 2.8, § 3.4 · **umgesetzt:** Etappe H1 (Pflichtpositionen,
Schemaschritt 59)

## Was der Dialog zeigt

Banner: „Alle Beträge und Bezugsgrößen sind **netto**. Mengen stammen aus dem Simulationslauf vom
‹Datum, Uhrzeit›." Die drei Pflichtzeilen nach VDI 2067 (Wartung, Instandhaltung, Hilfsenergie)
stehen oben, hinterlegt, mit **Schloss statt Papierkorb** — der Löschversuch bietet „Satz auf 0
setzen" an. Unter der Position steht der Empfehlungsbereich, unter dem Satz die Herleitung
(Menge und Quelle, anlagenscharf), unter dem Betrag „berechnet". Absolute Positionen zeigen ein
gesperrtes Satzfeld.

| Position | Bemessung | Satz | Herleitung | Betrag |
|---|---|---|---|---|
| Wartung BHKW — Pflicht · üblich 2,0–4,0 ct/kWh | € / kWh elektrisch | 0,0280 | × 1.650.000 kWh el · BHKW 1 | 46.200,00 🔒 |
| Instandhaltung — Pflicht · üblich 1,0–2,0 % | % der Investition | 1,50 | × 33.927,61 € · Investition BHKW 1 | 508,91 🔒 |
| Hilfsenergie — Pflicht · üblich 2,0–4,0 % (BHKW) | % der Endenergiekosten | 2,00 | × 312.631,20 € Endenergiekosten · BHKW 1 → 21.710 kWh Strom | 6.252,62 🔒 |
| Versicherung | Jahresbetrag | — | | 1.100,00 🗑 |
| **Betriebskosten BHKW 1** | | | brutto 64.333,22 €/a | **54.061,53** |

**Warnband:** „Die Hilfsenergie ist zugleich als Anlagenanteil im BHKW-Dialog gepflegt (2,0 %).
Doppelpflege — es zählt die Kostenposition, der Anlagenanteil wirkt nur auf die
KWKG-Nettostrommenge."

**Ohne Simulationslauf** zeigen mengenbasierte Zeilen einen Strich statt einer 0 samt Warnzeile
(„Stromproduktion unbekannt — Simulation noch nicht gelaufen"); investitionsbasierte Sätze rechnen
sofort. Fußhinweis: „n von m Pflichtpositionen rechnen noch nicht — Simulation ausführen."

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
abgeleitete Beträge fehlen dort (Befund B-5). Im Mockup ist der Kaskadenbetrag 33.927,61 € gezeigt,
wie er nach Behebung von B-5 anzusetzen wäre.

## Berechnungserläuterung am Beispielprojekt

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| Wartung | 1.650.000 kWh × 0,0280 €/kWh | 46.200,00 €/a | Gruppe C, Menge aus dem Lauf |
| Instandhaltung | 33.927,61 × 1,50 / 100 | 508,91 €/a | Gruppe B, Basis Investition der Anlage |
| 1 Endenergiemenge | 4.342,1 MWh × 1000 | 4.342.100 kWh | Brennstoff des BHKW aus dem jüngsten Lauf |
| 2 Arbeitspreis | 0,7560 €/m³ ÷ 10,5 kWh/m³ | 0,0720 €/kWh | Heizwert als Umrechnung, keine η-Division |
| **Endenergiekosten** | 4.342.100 × 0,0720 | **312.631,20 €/a** | Bezugsgröße der Prozentzeile |
| 3 Hilfsenergie 2 % | 312.631,20 × 2,00 / 100 | 6.252,62 €/a | Weg A |
| 4 Rückrechnung Strom | 6.252,62 € ÷ 0,288 €/kWh | 21.710 kWh/a | Plausibilität, ohne Rechenwirkung |
| Versicherung | Jahresbetrag | 1.100,00 €/a | Gruppe A |
| **Betriebskosten Jahr 1** | 46.200,00 + 508,91 + 6.252,62 + 1.100,00 | **54.061,53 €/a** | steigt mit p_B ab Jahr 2; brutto × 1,19 = 64.333,22 |

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
