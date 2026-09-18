# 06 · Vergütungen Photovoltaik

**Dialog:** `PhotovoltaikVerguetungDialog` (`EPOS.UI/Dialoge/Wirtschaftlichkeit/`) — Bestand, zugleich Stilmuster (Konzept § 2.3) ·
**Mockup:** `../Mockups/Dialog_Formel_Zahlenprobe.html#pv` · **Recht:** § 21, § 21c, § 51, § 51a,
§ 100 EEG · **Code:** `PvErloesRechner`, Erlösreihe `PV_VERGUETUNG` · **Konzept:** § 2.3, § 3.6
(Photovoltaik / EEG)

Die PV-Seite ist der Gegenentwurf zum BHKW: keine Staffel über Leistungsanteile im Zuschlag, dafür
ein anzulegender Wert, der degressiv altert, eine Marktprämie als Differenzgröße und zwei
Kürzungstatbestände — negative Preise und die 60-%-Kappung.

## Was der Dialog zeigt

Schalter „Vergütung anwenden" im Kopf, dann sieben Gruppen in der Reihenfolge des Dialogs. Den Satz gibt der
Anwender an **einer** Stelle ein: **AW-Override [ct/kWh] (0 = Katalog)** in der Gruppe „Anzulegender Wert"; leer
oder 0 heißt, der Katalogwert gilt, und die Zeilen darunter sagen, wie er entsteht. Feste EV und Jahresmarktwert
sind Herleitungszeilen, keine Felder.

| Gruppe (Ressourcentext) | Felder im Beispiel |
|---|---|
| **Anlage** | Zeile „Installierte Leistung: rechnerisch 300,0 kWp" · Zahlenfeld Override [kWp] (0 = keiner) 0,00 · Datumsfeld Inbetriebnahme 01.08.2026 · Zahlenfeld Degradation [%/a] 0,50 (wirkt nur in der Erlösreihe) · Optionsgruppe Einspeiseart: **Überschusseinspeisung** / Volleinspeisung · Warnzeilen nur über 1 MW (Ausschreibung, AW-Override nötig) und über 2 MW (Stromsteuer prüfen) |
| **Anzulegender Wert** | Zahlenfeld **AW-Override [ct/kWh] (0 = Katalog)** 0,00 · Zeile „AW_mix: **6,04 ct/kWh**" (mit Override: „AW_mix: 7,00 ct/kWh (Override)") · Herleitung „marginale Klassen, Katalogwerte zum Stichtag 08/2026: 100 kWp × 7,94 + 200 kWp × 5,09 = 1.812 ÷ 300 · Degression 0,99 je Halbjahresstichtag ab 01.02.2024" · Zeile „Feste EV (AW − 0,40): 5,64 ct/kWh" |
| **Vermarktung** | Optionsgruppe: Feste Einspeisevergütung (gesperrt: „Feste EV nur bis 100 kW (§ 21 Abs. 1 Nr. 1).") · **Direktvermarktung mit Marktprämie** · Sonstige Direktvermarktung / PPA · Keine Vergütung (unentgeltlich) (gesperrt ab 200 kW) · Zahlenfeld DV-Entgelt [ct/kWh] 0,40 · Zahlenfelder PPA-Festpreis [ct/kWh] (0 = keiner) und PPA-Aufschlag auf Spot [ct/kWh], gesperrt, solange keine sonstige Direktvermarktung gewählt ist · Hinweiszeile · Jahresmarktwert Solar 2026: 4,50 ct/kWh aus „Marktwerte importieren…" (Anzeige, U27) |
| **Vergütungsausfall (§ 51 / § 51a)** | Klappliste Anwenden: Automatisch · Ganzzahlfeld iMSys-Einbaujahr (0 = keins) 0 · Zahlenfeld Ausfallanteil der Einspeisearbeit [%] 20,0 · Schalter § 51a-Kompensation (Laufzeitverlängerung) ✓ · Statuszeile „greift ab der ersten negativen Viertelstunde." (Anlage ≥ 100 kWp, Inbetriebnahme nach dem 25.02.2025) |
| **Strompreis / Bezugsbewertung** | Schalter „Netzbezug stundenscharf aus Preiszeitreihe bewerten" · Hinweiszeile zur Stromsteuerfreiheit des Eigenverbrauchs (§ 9 StromStG) |
| **60-%-Wirkleistungsbegrenzung (§ 9 Abs. 2 EEG)** | Klappliste Anwenden: **Ja** · Statuszeile „aktiv: Einspeisung auf 60 % der kWp begrenzt (ohne iMSys)." — „Automatisch" begrenzt nur bei fester Einspeisevergütung ohne iMSys, bei Direktvermarktung greift die Kappung nur mit „Ja" · Kappungsgrenze 0,6 × 300 kWp = 180 kW · Verlust = Σ max(0; Einsp_h − 180) = 3.990 kWh/a (1,4 %), messbar nur mit Stundenreihe |
| **Vorschau** | eine Zeile „Einspeisung 199,5 MWh/a · Satz Jahr 1: 6,04 ct/kWh · Erlös Jahr 1: 10.396 €/a · Vergütungsausfall 39.900 kWh (614 €) · § 51a-Gutschrift 1.205 € (Jahr 20)" · Kennzahlzeile (Stromgestehungskosten und Autarkiegrad ohne Lauf als Gedankenstrich, Eigenverbrauchsquote 30,0 %) · Ertragszeile 285,0 / 85,5 / 199,5 MWh/a |

**Vorschau Jahr 1 (2026), aufgeschlüsselt** (Vorschlag U27 — der Dialog zeigt die eine Zeile):

| Position | Herleitung | € |
|---|---|---|
| Spoterlös | 199,5 MWh × 4,50 ct | 8.977,50 |
| Marktprämie | × (6,04 − 4,50) ct | 3.072,30 |
| Prämienausfall § 51 | 20 % der Einspeisung | − 614,46 |
| DV-Entgelt | × 0,40 ct | − 798,00 |
| Kappungsverlust 60 % | 3.990 kWh × 6,04 ct | − 241,00 |
| **Vergütung PV** | | **10.396,34** |
| Vermiedener Netzbezug — Ausweis | 85,5 MWh × 28,80 ct | 24.624,00 |

Knöpfe „Marktwerte importieren…" und „Einspeise-Tarif…" mit Sprunghinweis; Fußleiste: Abbrechen · Übernehmen;
Statuszeile „Reihe über 20 volle Jahre ab dem Kalenderjahr der Inbetriebnahme".

## Berechnungsgrundlage

```
Anzulegender Wert
  Degression   Faktor 0,99^n   (Halbjahresstichtage 1.2. / 1.8. ab 01.02.2024 bis Inbetriebnahme)
  AW_mix     = round( Σ Anteil_k × AW_Klasse_k / Σ Anteil_k , 2 )
               marginale Klassen 10 / 40 / 100 / 400 / 1000 kWp
  EV_fest    = max(0, AW_mix − 0,40)              nur ≤ 100 kW
  Ausfallvergütung = AW × (1 − 20 %)              nur > 100 kW

Marktprämie (Direktvermarktung, § 21 EEG — Pflicht ab 100 kW)
  Erlös = Spot€
        + Arbeit × max(0, AW − Jahresmarktwert) / 100
        − Arbeit × DV-Entgelt / 100

§ 51 — negative Preise, je Jahr (AUTO)
  IBN < 25.02.2025 → nein ; ≥ 100 kWp → ja ; sonst ab dem Jahr nach dem iMSys-Einbau
  Ausfallanteil a = 20 % pauschal   ODER   Σ Einsp(Spot < 0) / Σ Einsp   (stundenscharf)
  Prämienausfall  = Arbeit × a × (AW − Jahresmarktwert) / 100
  Der Spoterlös bleibt — es entfällt nur die Prämie.

§ 51a — Kompensation im letzten Vergütungsjahr
  Gutschrift = Ausfallarbeit(Jahr 1) × 0,5 × AW / 100        Einmalzahlung, auf ihr Jahr abgezinst

60-%-Kappung
  Verlust [kWh] = Σ_h max(0, Einsp_h − 0,6 × kWp)             mit AW bewertet

Die PV-Reihe steigt NICHT mit p_E: Der anzulegende Wert ist gesetzlich fixiert (nominal konstant,
DIN EN 17463, 6.3.2). Der Jahresmarktwert schwankt — er gehört in die Szenarienpflege (Best/Worst),
nicht in eine Preissteigerungsrate. Steigt er über den AW, entfällt die Prämie ganz, der Spoterlös
steigt aber.
```

Belege des Bestands: 8,60 × 0,99^n → 8,10 ct/kWh ab 08/2026 trifft 16 von 16 BNetzA-Werten exakt ·
300 kWp → 6,04 ct/kWh · Marktprämie Jahr 1 = 13.536,00 € und § 51a = 1.812,00 € in einem anderen
Bestandsprojekt (Formelbeleg, nicht das Beispielprojekt).

## Berechnungserläuterung am Beispielprojekt

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| 1 Degression | 0,99 je Halbjahresstichtag ab 01.02.2024 | Katalog | Stichtage 1.2. und 1.8. bis zur Inbetriebnahme |
| 2 Klassenanteile | 100 kWp × 7,94 + 200 kWp × 5,09 | 1.812 ct·kWp | marginale Klassen, Katalogwerte zum Stichtag |
| **AW gemischt** | 1.812 ÷ 300 | **6,04 ct/kWh** | Belegwert des Bestands für 300 kWp |
| 3 Spoterlös | 199.500 kWh × 4,50 ÷ 100 | 8.977,50 € | Jahresmarktwert Solar |
| 4 Marktprämie | 199.500 × (6,04 − 4,50) ÷ 100 | 3.072,30 € | Differenz AW zu Marktwert |
| 5 Ausfall § 51 | 199.500 × 20 % × 1,54 ÷ 100 | − 614,46 € | nur die Prämie entfällt |
| 6 DV-Entgelt | 199.500 × 0,40 ÷ 100 | − 798,00 € | Kosten des Direktvermarkters |
| 7 Kappung 60 % | 3.990 kWh × 6,04 ÷ 100 | − 241,00 € | Mengenverlust, mit AW bewertet |
| **PV-Vergütung Jahr 1** | 8.977,50 + 3.072,30 − 614,46 − 798,00 − 241,00 | **10.396,34 €** | eigene Reihe, 20 Jahre + IBN-Monate |
| § 51a im letzten Jahr | 39.900 kWh × 0,5 × 6,04 ÷ 100 | 1.204,98 € | Einmalzahlung, auf ihr Jahr abgezinst |
| Vermiedener Bezug | 85,5 MWh × 288 €/MWh | 24.624,00 € | **Ausweis** — steckt im Reststrombetrag |

Mit Degradation (`03`, 0,5 %/a) sinkt die Einspeisung bis Jahr 20 auf 199,5 × 0,9092 = 181,4 MWh;
die Reihe folgt der Menge, der AW bleibt.

## Befunde und offene Punkte

| Nr. | Punkt | Behandlung |
|---|---|---|
| — | Feste Vergütung über 100 kW nicht wählbar — Radio gesperrt mit Hinweis | Bestand ✓ |
| — | > 1 MW Ausschreibung: AW-Override nötig; > 2 MW Stromsteuer prüfen | Warnzeile im Bestand ✓ |
| V-G5 | Jahresmarktwert, PPA-/DV-Preise: Best/Worst-Paar je Feld | Szenarioabdeckung (Konzept § 2.11.5), Etappe V-E |
| V-G2 | Degradation wirkt auf die Einspeisemenge der Reihe | Attribut aus `03`, Vorgabe 0 |
| — | Bei Direktvermarktung greift die 60-%-Kappung in Stellung „Automatisch" nicht (`PvErloesRechner`: AUTO = feste EV ohne iMSys); das Beispiel rechnet sie mit der Stellung „Ja" | Mockup Abschnitt 6 zeigt „Ja" mit Statuszeile „aktiv" |
| U27 | Aufgeschlüsselte Vorschau (Spoterlös, Marktprämie, § 51, DV-Entgelt, Kappung, Summe, vermiedener Netzbezug) statt der einen Zeile; Jahresmarktwertzeile in der Gruppe Vermarktung — Marktwert-Override und Marktwertentwicklung haben im Dialog kein Feld | Mockup Abschnitt 6, Anhang Umsetzungsstand |
| — | § 51a-Formel ist eine Näherung (Verlängerung der Vergütungsdauer um die Ausfallstunden) | im Bericht als Näherung deklarieren |
