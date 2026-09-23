# 04 · Energiekosten

**Dialog:** `EnergietraegerDialog` (`EPOS.UI/Dialoge/Kosten/`) mit der Trägerkarte `EnergietraegerEinstellungen`,
darin `BrennstoffBestandteile` bzw. `StrompreisDetails`; Hülle `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs` ·
**Mockup:** `../../Mockups/Dialog_Formel_Zahlenprobe.html#energie` · **Recht:** BEHG / EBeV 2030,
EU-ETS 2, GEG Anlage 4 und 9 · **Code:** `StromMatrix`, `KostenEmissionRechner`, `LeistungspreisStaffel`,
`energy_carrier` · `energy_price` · `energy_project_settings` · **Konzept:** § 2.5, § 3.5, § 3.11

## Was der Dialog zeigt

Links die Trägerliste (Filterfeld, Gruppenköpfe, im Projektkontext „Aus Katalog übernehmen…" und
„Entfernen"), rechts die Trägerkarte in **vier Blöcken** ohne Reiter: A und B nebeneinander, darunter C
und D über die volle Breite, dann die Preishistorie. Der Kopf nennt den Träger („Energieträger — Erdgas"),
die Zeile darunter Projekt und Preisstellung („… · Preise netto").

**Block A · Preis und Heizwert:** Arbeitspreis 0,7560 €/m³ · Grundpreis 180 €/a · Leistungspreis
0,00 €/(kW·a) samt Modus · H_i 10,50 · H_s 11,60 kWh/m³ · Effektivzeile „effektiv: 1 m³ = 10,50 kWh (H_i) /
11,60 kWh (H_s)" · Herleitung „→ 0,0720 €/kWh · Umrechnungsfaktor H_s/H_i = 1,1048" · Formelzeile
„Formel: 0,76 €/m³ ÷ 10,50 kWh/m³ = 0,0720 €/kWh". Darunter die saisonalen Sätze (nur bei Trägern mit
Leistungspreis), die Katalogübernahme (nur im Projektkontext) und die Hinweise zu fehlenden oder
geliehenen Werten. **Beim Stromträger im Projektkontext** steht im selben Block die Gruppe
„Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)": Staffelgrenze [kW], Preis bis zur Grenze und Preis
über der Grenze [€/(kW·a)], darunter die Erklärzeile; ein leeres Feld heißt „nicht gepflegt", der Katalog
führt keine Staffel (Konzept § 2.5).

**Block B · Preisbestandteile — Transparenz, ohne Preiswirkung** (beim Stromträger an derselben
Stelle „Strompreis Details"). Er ist die Grundlage der Kohärenzprüfung gegen die
Steuerentlastungen (siehe `05`). Jeder Bestandteil trägt einen Wert und einen Aktiv-Schalter; nur
eingeschaltete zählen, ein leeres Feld heißt „kein Anteil". Einen Modus gibt es nicht.

**Die Werte stehen in der Abrechnungseinheit** (€/m³, €/l, €/t) — gerechnet, gespeichert und
geprüft wird in ct/kWh, umgerechnet genau einmal an der Anzeigekante über H_i. Ohne Heizwert
bleibt ct/kWh, und eine leise Zeile nennt den Grund.

| Bestandteil | €/m³ | Herleitung |
|---|---|---|
| Energiesteuer | 0,0638 | 5,50 €/MWh (H_s) |
| CO₂-Bestandteil (BEHG) | 0,1371 | 65 €/t × 2,109 kg/m³ (200,9 g/kWh × 10,5 kWh/m³ ÷ 1000) |
| Netz- und Messentgelt | 0,1180 | — |
| Beschaffung und Vertrieb | 0,4371 | Rest, als Vorschlag am Feld |
| **Summe** | **0,7560** | deckungsgleich mit dem Arbeitspreis ✓ |

Die Summenzeile **prüft**: Weicht die Summe der aktiven Anteile um mehr als 0,0001 €/kWh vom
Arbeitspreis ab, steht sie auf „≠" und nennt den Abstand. Darunter der nicht aufgeschlüsselte
Rest („Nicht aufgeschlüsselter Rest: 0 ct/kWh"), negativ in Warnfarbe.

Zwei Knöpfe: **„Schnellwahl aus Katalog…"** öffnet eine Überlagerung mit den Katalogsätzen — § 2,
§ 53a und § 54 EnergieStG sowie BEHG, je mit Herkunft, Jahr, Katalogwert (€/MWh bzw. €/t) und dem
Satz in der Abrechnungseinheit; die Auswahl trägt in das zugehörige Feld ein. **„In Arbeitspreis
übernehmen"** MELDET nur, dass die Summe in das Arbeitspreisfeld soll; eingetragen wird sie dort,
gespeichert mit „Speichern".

**Block C · Emissionen — Anzeige folgt der Bilanzierungsvorgabe des Projekts** (Entscheidungen
D-1, E-1, ET-D-2): oben die Klappliste **Bilanzierungsmethode** („CO₂ direkt — reale Bilanz,
heizwertbezogen" bzw. „CO₂-Äquivalent (GWP₁₀₀)"), im Katalogkontext nur lesend. Darunter die
Arten **dieses** Trägers mit Art · Wert · Einheit · Quelle · „Katalog…", die Summenzeile und der
Knopf „Emissionsarten & Katalog verwalten…". **Kein Primärenergiefaktor, keine Trägerübersicht.**

| Art | Wert | Einheit | Quelle |
|---|---|---|---|
| Kohlendioxid | 200,9 | g/kWh | EBeV 2030 Anlage 2 (H_i) |

Die Fußnote: Angezeigt wird nur die im Projekt gewählte Größe — CO₂ *oder* CO₂-Äquivalent; SO₂
und NO_x werden weiterhin geführt, aber nicht in dieser Tabelle gezeigt. Der **Nachweissatz** nach
GEG Anlage 9 (Strom 560, ab 2027: 100 g/kWh) ist ein anderer Satz für einen anderen Zweck und
belegt im Code nie dieselbe Variable (Konzept § 3.11).

**Block D · Einheiten und Umrechnung** — aufklappbar, Vorgabe zu. Er führt die Klappliste
**Preisbasis** mit genau zwei Einträgen (Abrechnungseinheit und kWh, Faktor = Heizwert), die
Basiseinheit und den Regelblock samt „Regel hinzufügen" und Verstoßbanner. Die Zeile darunter sagt,
wozu er da ist: *Diese Regeln prüfen die Einheitenkette; gerechnet wird mit Heizwert und Brennwert.*

**Preishistorie:** Datumsfeld „Gültig ab" und Knopf „💾 Speichern" für die ganze Karte; je Preisstand eine
Zeile mit Gültig ab · Heizwert [kWh/m³] · Basis Einheit · Arbeitspreis · Grundpreis [€/a] · Leistungspreis und
dem Knopf „Löschen" mit Rückfrage. Ein neuer Stand entsteht beim Speichern, nicht beim Tippen.

Einen Block „Aufschläge Netzbezug Strom" gibt es nicht — er ist durch die **Zerlegung** ersetzt:
Die Anteile stecken im Arbeitspreis, sie kommen nicht auf ihn (Konzept § 3.5).

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

Leistungspreis BRENNSTOFF — die einzige η-Division der Kette
  Basis = vorgehaltene Anschlussleistung aus den Gerätedaten
  kw = BHKW: (P_el + P_therm) / η_gesamt      Kessel: P_therm / η
       (η > 1,5 gilt als Prozentangabe ÷ 100 ; außerhalb (0;1,5] wird die Anlage übersprungen)
  Saisonreihe (12 Monatssätze) vor konstantem Satz
  Modus JAHR: Satz × kw          Modus MONAT: Satz × kw × 12
  kw ≤ 0 ⇒ kein Leistungspreis

Netzbezug Strom
  StromkostenNetz = Stromrestbedarf × 1000 × Preis + Grundpreis + Leistungsanteil
  Leistungsanteil — Basis ist die gemessene BEZUGSSPITZE, nicht die Anlagenleistung
    Spitze = Maximum der VIERTELSTUNDENreihe des Netzbezugs (dieselbe Reihe, die der
             Speicher kappt) ; das Stundenmittel (StromMatrix.MaxBezugKW) glättet sie
             und bemisst allein die Leistungspreismodelle des Rollentarifs
    Staffel vor Saisonreihe vor konstantem Satz — genau einer rechnet, nie eine Summe
    Staffel (gepflegt, sobald ein Preis > 0; nur am Stromträger des Projekts):
             min(S, G) × P₁ + max(0, S − G) × P₂   S = Jahresspitze, G = max(0, Grenze),
             P₁, P₂ in €/(kW·a), gleich welcher Modus am Träger steht
    Modus JAHR: Satz × Jahresspitze      Modus MONAT: Σ₁₂ (Monatsspitze × Satz)
    Saisonreihe: Σ₁₂ (Monatssatz × Monatsspitze)
    Satz 0 / nicht gepflegt ⇒ kein Anteil ; keine Zeitreihen ⇒ kein Anteil, Träger wird benannt
  Rollentarif: der Reststrombetrag ersetzt den Flat-Anteil GANZ (samt Leistungsanteil und Staffel) ;
               einen Zeitzonentarif (HT/NT) gibt es nicht, die Strommatrix führt keine Tarifzonen
  Kein Aufschlag: Die Preisanteile („Strompreis Details") ZERLEGEN den Arbeitspreis,
                  sie kommen nicht auf ihn — es gibt genau eine Preiswahrheit, und das
                  ist der Arbeitspreis der Trägerkarte
  Wer im Reststrombedarf steckt: Wärmepumpe, Heizstab, Stromspeicher — und der
                  ELEKTROKESSEL. Seine Nutzwärme bucht die Simulation auf den Stromzähler
                  (Nutzungsgrad 1) ; seine Kessel-Modulzeile führt deshalb Verbrauch = 0
                  und trägt NICHTS zu verbrauchJeTraeger bei. Sein Strom wird genau hier
                  bepreist — einmal, im Netzbezug. Die Kostenseite ZEIGT die Menge
                  zusätzlich als Endenergie des Kessels (Rechenweg 02), ohne sie ein
                  zweites Mal zu buchen.

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
  Abrechnungsmenge gehört der KATALOGWERT 181,4 (GESETZ_EF_BILANZ_EBEV_ERDGAS_HO) —
  keine Umrechnung des Beispiels, sonst rund 10 % zu viel CO2
  Bilanz und BEHG: heizwertbezogener Faktor × heizwertbezogene Menge (Tafel unten)
  CO₂-Grenzwert § 9 Abs. 1 Nr. 3 StromStG: BRENNWERTBEZOGEN — Erdgas mit dem Katalogwert
  181,4, sonst Hi-Faktor × H_i/H_s des Trägers, ohne Brennwert der Hi-Faktor (`05`)
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
| **BEHG Jahr 1 (2026)** | 872,329 × 65,00 €/t | **56.701,38 €/a** | eigene Reihe, folgt dem Preispfad je Kalenderjahr. Mit der auf 872,3 t gerundeten Menge ergäbe sich 56.699,50 € — hier wird **ungerundet** gerechnet |

Die Preisbestandteile sind Ausweis: Der CO₂-Bestandteil im Gaspreis (0,1371 €/m³ × 413.533 m³ =
56.695 €) und die BEHG-Reihe (56.701,38 €) beschreiben denselben Betrag — einmal als Teil des
Arbeitspreises, einmal als eigene Reihe. **Im Kapitalwert darf nur einer von beiden stehen.** Heute
rechnet EPOS-Plan die BEHG-Reihe separat; dann muss der Arbeitspreis ohne CO₂-Bestandteil gepflegt
sein — die Kohärenzprüfung (`05`) soll das anzeigen.

## Befunde und offene Punkte

| Nr. | Befund | Behandlung |
|---|---|---|
| ✔ N3 | Ungepflegte Anteilsspalten lasen sich als Vorschlagswerte, nicht als 0 (+32 % Energiekosten) | erledigt: Die Anteile zerlegen den Arbeitspreis; ein ungepflegter Anteil ist inaktiv und trägt 0 bei, der Vorschlag steht nur im Feld und wirkt erst mit dem Haken bzw. dem Knopf am Feld (Beschaffung als Rest, Stromsteuer § 3 / § 9b). Entscheid 18.09.2026 zu N-3 nach Empfehlung; offen bleibt allein ein Sammelknopf „Vorschlagswerte übernehmen" — Bequemlichkeit, kein Fehler |
| ✔ R5 | CO₂ doppelt: Preisbestandteil und BEHG-Reihe | **umgesetzt #405**: `KohaerenzPruefung.Co2DoppelansatzBehg` meldet den Fall als **WARNUNG mit dem doppelt gebuchten Jahresbetrag** (`KohaerenzCo2Tests`, 11 Fälle). Der Rechenweg bleibt, wie er ist — die Zeile ist Ausweis, nicht Korrektur (Entscheid Q3, Weg a) |
| ✔ R6 | Die Kohärenzzeilen erreichten nur die Seite; der Strommix-Rückfall war ein Laufhinweis ohne seinen Wert | **umgesetzt #405**: Die Zeilen stehen im **einen** Zeilenkatalog (`WirtschaftlichkeitZeilen`) und damit in Rubrik, Wort- und Excelbericht; der Rückfall nennt seine 435 g CO₂/kWh |
| ✔ R11 | Hi/Ho am CO₂-Grenzwert: Der Katalog führt Erdgas heiz- **und** brennwertbezogen (200,9 / 181,4 g/kWh), gelesen wurde der Schlüssel der Anlage; ist der Grenzwert 270 g/kWh des § 2 StromStG brennwertbezogen, zählt ein heizwertbezogener Zähler rund 10 % zu hoch | **umgesetzt #437** (E7 Teil a; Entscheid 22.09.2026, Anwender: „es gilt immer der Brennwert"): `SteuerGutschriftRechner.Co2JeEnergieertrag` nimmt zu Erdgas den Katalogwert 181,4 g/kWh (H_s), zu den übrigen Trägern den heizwertbezogenen Wert × H_i/H_s des Trägers, ohne gepflegten Brennwert den Hi-Faktor mit Begründung; die Herleitung nennt den Wert je Anlage. Die Pinnung in `KleinkorrekturenE2Tests` steht auf dem Brennwert (Grenzfall 0,00 → 8.200,00 €/a); das Beispiel in `05` liegt bei 218,6 statt 242,1 g/kWh. Bilanz und BEHG bleiben heizwertbezogen |
| ✔ Q11 | Der Zeitzonentarif (Winter/Sommer × HT/NT) bepreiste den Netzbezug an vier Tarifzonen der Strommatrix; die zweistufige Leistungspreis-Staffel stand im Tarifsatz, rechnete nur dort und war allein über die Sicht „Strombezug" pflegbar | **umgesetzt #439** (E7 Teil b; Anwender 22.09.2026 „kein HT/NT", der Rest nach Empfehlung; die Fragen E7b‑Q1 bis E7b‑Q4 entschieden 23.09.2026): Die Strommatrix führt keine Tarifzonen (je Projekt eine Jahreszeile), den Netzbezug bepreist der Stromträger; die Staffel steht beim Stromträger, geht Leistungspreis und Saisonreihe vor (keine Addition) und bemisst sich an der Viertelstundenspitze — Probe 1030 mit 1.500 kW / 60 / 90 €/(kW·a) bei 2.011 kW: 1.500 × 60 + 511 × 90 = 135.990 €/a; Schemaschritt 104 übernimmt die Staffel der Zonensätze, löscht die Sätze und verwirft ihre gespeicherten Läufe. Das Beispiel dieses Papiers bleibt, wie es ist (kein Leistungspreis, kein Tarif) |
| D-1 / E-1 | Emissionsspalte: eine Größe, Tooltip benennt Äquivalent/Vorkette | entschieden 30.08.2026 |
| § 3.11 | Nachweis- und Bilanzsatz strikt trennen; Stichtag 01.01.2027 (GModG) mit Methodenwechsel für KWK | Katalog mit Gültig-ab-Datum, beide Sätze parallel |
| § 3.11 | CO₂-Preispfad ab 2028 ist Prognose | editierbare Stützstellenreihe mit Status GESICHERT / VORLÄUFIG / PROGNOSE |
