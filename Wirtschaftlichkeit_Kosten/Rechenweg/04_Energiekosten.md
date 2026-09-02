# 04 · Energiekosten

**Dialog:** Energieträger-Dialog mit `ucBrennstoffBestandteile` (B2) und `ucStromAufschlaege` ·
**Mockup:** `../Mockups/Dialog_Formel_Zahlenprobe.html#energie` · **Recht:** BEHG / EBeV 2030,
EU-ETS 2, GEG Anlage 4 und 9 · **Code:** `StromMatrix`, `KostenEmissionRechner`,
`energy_carrier` · `energy_price` · `energy_project_settings` · **Konzept:** § 2.5, § 3.5, § 3.11

## Was der Dialog zeigt

**Gruppe Preis und Heizwert:** Arbeitspreis 0,7560 €/m³ · Grundpreis 180 €/a · Leistungspreis
0,00 · H_i 10,50 · H_s 11,60 kWh/m³ · Herleitung „→ 0,0720 €/kWh · Umrechnungsfaktor H_s/H_i =
1,1048".

**Gruppe Preisbestandteile** — Transparenz ohne Preiswirkung, die Grundlage der Kohärenzprüfung
gegen die Steuerentlastungen (siehe `05`):

| Bestandteil | €/m³ | Herleitung |
|---|---|---|
| Energiesteuer | 0,0638 | 5,50 €/MWh (H_s) × 11,6 kWh/m³ |
| CO₂-Bestandteil (BEHG) | 0,1371 | 65 €/t × 2,109 kg CO₂/m³ (200,9 g/kWh × 10,5 kWh/m³) |
| Netz- und Messentgelt | 0,1180 | Netzbetreiber |
| Beschaffung und Vertrieb | 0,4371 | Rest |
| **Summe** | **0,7560** | deckungsgleich mit dem Arbeitspreis ✓ |

Knöpfe „Schnellwahl aus Katalog…" und „In Arbeitspreis übernehmen" — der zweite schreibt nur auf
Knopfdruck.

**Gruppe Emissionen** — Anzeige folgt der Bilanzierungsvorgabe des Projekts (Entscheidungen D-1,
E-1): **eine** Spalte, CO₂ *oder* CO₂-Äquivalent; SO₂ und NO_x werden geführt, aber hier nicht
gezeigt. Der Tooltip benennt an jedem Wert, ob eine Vorkette enthalten ist.

| Träger | CO₂ direkt, heizwertbezogen | Primärenergiefaktor | Quelle |
|---|---|---|---|
| Erdgas | 200,9 g/kWh | 1,10 | EBeV 2030 Anlage 2 (H_i) · GEG Anlage 4 |
| Heizöl EL | 266,4 g/kWh | 1,10 | EBeV 2030 Anlage 2 |
| Strommix Netzbezug | 435,0 g/kWh | 1,80 | BAFA EEW 3.4 — Rückfall, reale Bilanz |

Der **Nachweissatz** nach GEG Anlage 9 (Strom 560, ab 2027: 100 g/kWh) ist ein anderer Satz für
einen anderen Zweck und belegt im Code nie dieselbe Variable (Konzept § 3.11).

**Gruppe Aufschläge Netzbezug Strom:** Schalter „Aufschläge anwenden" (Vorgabe aus) · Herleitung
„Netzentgelt 6,440 + Umlagen 2,946 + Stromsteuer 2,050 + Konzession 0,110 + Vertrieb 0,200 =
11,746 ct/kWh" · **Warnband N3:** „Leere Spalten lesen sich als Vorschlagswerte, nicht als Null. Ein
ungepflegter Stromträger liefert deshalb die vollen 11,746 ct/kWh. Gemessen an Projekt 1030:
+360.603 €/a (+32 %), Kapitalwert −29,8 %."

## Berechnungsgrundlage

```
Mengen
  verbrauchJeTraeger[carrier] += Verbrauch [MWh/a] je BHKW- und Kesselmodul
  Menge ≤ 0 wird verworfen ; carrier ≤ 0 bei Menge > 0 ⇒ kostenVollstaendig = false

Arbeitspreis
  [A] eff_hi > 0   Menge [Einheit/a] = MWh × 1000 / eff_hi ;  Arbeit = Menge × PreisArbeit [€/Einheit]
  [B] sonst        Arbeit = MWh × 1000 × PreisArbeit [€/kWh]
  eff_hi ist der Heizwert je Abrechnungseinheit, kein Wirkungsgrad — in der Kostenkette
  wird nie durch η geteilt; der Verbrauch ist bereits Endenergie.
  Vorrang nur für Werte > 0: custom_price_work → price_work → null ⇒ Energiekosten = null

Grundpreis   einmal p. a. je Träger ; custom_price_base gilt auch bei 0 (nur NULL fällt durch) ;
             nur addiert, wenn ein Arbeitspreis existiert

Leistungspreis — die einzige η-Division der Kette
  kw = BHKW: (P_el + P_therm) / η_gesamt      Kessel: P_therm / η
       (η > 1,5 gilt als Prozentangabe ÷ 100 ; außerhalb (0;1,5] wird die Anlage übersprungen)
  Saisonreihe (12 Monatssätze) vor konstantem Satz
  Modus JAHR: Satz × kw          Modus MONAT: Satz × kw × 12
  Stromträger ausgenommen ; kw ≤ 0 ⇒ kein Leistungspreis

Netzbezug Strom
  StromkostenNetz = Stromrestbedarf × 1000 × Preis + Grundpreis
  Tarifmodus: Zonen- oder Rollenbetrag ersetzt den Flat-Anteil ; danach IMMER + AufschlagBetrag
  AufschlagBetrag = NetzbezugMWh × 1000 × WirksamCtKwh / 100

CO₂ / BEHG als eigene Reihe
  behgBasisT [t/a] = CO2Brennstoff + (ohne Nachhaltigkeitsnachweis) BiogenBehgMenge × BehgOhneNachweis / 1000
  BEHG_t [€]       = behgBasisT × CO2-Preis(Kalenderjahr)
  Preis: Override CO2_Preis > 0 (dann mit p_E fortgeschrieben), sonst Katalogpfad
         2021–25: 25/30/30/45/55 · 2026: 65 (realisiert) · 2027: Korridor 55–65 (vorläufig)
         ab 2028: 80 als Prognose — EU-ETS 2 startet 2028, nicht 2027
  Bedeutungsumkehr seit K6: 0 heißt „Pfad", nicht mehr „aus"

Emissionsfaktor-Kette (eine für alle Rechner)   PROJEKT → KATALOG → STAMM → CARRIER → null
  CO₂ in g/kWh, SO₂/NO_x in mg/kWh ; Strommix-Rückfall 435 g/kWh bei fehlendem Stromträger (mit Hinweis)
  Hi/Ho-Falle: Erdgas 200,9 g/kWh gilt heizwertbezogen ; auf die brennwertbezogene
  Abrechnungsmenge gehört 181,4 — sonst rund 10 % zu viel CO₂
```

## Berechnungserläuterung am Beispielprojekt

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| 1 Gasmenge | 4.342,1 MWh × 1000 ÷ 10,5 | 413.533 m³/a | Abrechnungseinheit des Trägers |
| 2 Arbeitspreis | 413.533 × 0,7560 | 312.631,2 €/a | identisch zur kWh-Rechnung der Betriebsseite (`02`) |
| 3 Grundpreis | einmal p. a. | 180,00 €/a | nur, wenn ein Arbeitspreis existiert |
| 4 Netzbezug Strom | 250,0 MWh × 1000 × 0,2880 | 72.000,00 €/a | Aufschläge aus |
| — wären Aufschläge an | 250.000 kWh × 11,746 / 100 | + 29.365 €/a | Befund N3 |
| **Energiekosten Jahr 1** | 312.631,2 + 180,00 + 72.000,00 | **384.811,20 €/a** | steigt mit p_E ab Jahr 2 |
| 5 CO₂-Menge | 4.342,1 MWh × 0,2009 t/MWh | 872,3 t/a | EBeV-Faktor Erdgas (H_i) |
| **BEHG Jahr 1 (2026)** | 872,3 × 65,00 €/t | **56.699,50 €/a** | eigene Reihe, folgt dem Preispfad je Kalenderjahr |

Die Preisbestandteile sind Ausweis: Der CO₂-Bestandteil im Gaspreis (0,1371 €/m³ × 413.533 m³ =
56.695 €) und die BEHG-Reihe (56.699,50 €) beschreiben denselben Betrag — einmal als Teil des
Arbeitspreises, einmal als eigene Reihe. **Im Kapitalwert darf nur einer von beiden stehen.** Heute
rechnet EPOS-Plan die BEHG-Reihe separat; dann muss der Arbeitspreis ohne CO₂-Bestandteil gepflegt
sein — die Kohärenzprüfung (`05`) soll das anzeigen.

## Befunde und offene Punkte

| Nr. | Befund | Behandlung |
|---|---|---|
| ⚠ N3 | Ungepflegte Aufschlagsspalten lesen sich als Vorschlagswerte, nicht als 0 (+32 % Energiekosten) | Empfehlung: als 0 lesen, Vorschlag nur auf Knopfdruck übernehmen |
| — | CO₂ doppelt: Preisbestandteil und BEHG-Reihe | Kohärenzzeile „CO₂ im Arbeitspreis und BEHG-Reihe gleichzeitig aktiv" |
| D-1 / E-1 | Emissionsspalte: eine Größe, Tooltip benennt Äquivalent/Vorkette | entschieden 30.08.2026 |
| § 3.11 | Nachweis- und Bilanzsatz strikt trennen; Stichtag 01.01.2027 (GModG) mit Methodenwechsel für KWK | Katalog mit Gültig-ab-Datum, beide Sätze parallel |
| § 3.11 | CO₂-Preispfad ab 2028 ist Prognose | editierbare Stützstellenreihe mit Status GESICHERT / VORLÄUFIG / PROGNOSE |
