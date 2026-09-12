# 03 · Kosten der Photovoltaik

**Dialog:** `Form_KostenKomponente` in derselben Form wie beim BHKW, mit PV-eigener Anordnung
(Anwenderauftrag 02.09.2026) · **Mockup:** `../Mockups/Dialog_Formel_Zahlenprobe.html#pvkosten` ·
**Norm:** DIN EN 17463, 6.3.3 (Degradation) und 6.4 (Endzahlungen statt Restwert) · VDI 2067 ·
**Code:** `EUR_PRO_KWP`, `BaugroesseSumme`, Ersatz-/Restwertlogik im `KapitalwertRechner` ·
**Konzept:** § 2.1, § 3.1, § 3.2, § 2.11.2 (V-G2, V-G4)

## Was PV anders macht — und was der Dialog deshalb anders anordnet

| PV-Eigenheit | Darstellung |
|---|---|
| Die Menge ist die Spitzenleistung in kWp und muss aus Modulanzahl × Modulleistung hergeleitet werden (Befund I-1) | Herleitungszeile „750 Module × 400 Wp" unter der Menge — beide Größen sichtbar |
| Der Wechselrichter lebt 12 Jahre, die Module 25 — Ersatz im Jahr 12 ist der Regelfall | Spalte **Nutzungsdauer** im Investitionsraster; Gruppe **Ersatz und Restwert** mit Barwerten |
| Die Module haben am Ende von T = 20 a noch Restdauer | Restwert je Position, Abweichung V-G4 deklariert |
| PV hat keine Endenergie — Hilfsenergie kann nicht prozentual bemessen werden | Hilfsenergie nur als **Jahresbetrag**, keine Pflichtzeile, kein Schloss |
| Der Ertrag altert | Gruppe **Ertrag und Degradation** mit Quellenfeld (neu, V-G2) |
| Die branchenübliche Kennzahl ist €/kWp | Summenzeile nennt „spezifisch 640,50 €/kWp" |
| Zuschuss und EEG-Vergütung schließen sich in der Regel aus | Infozeile statt Zuschusszeile |

## Was der Dialog zeigt — Reiter Investition

| Position | Kostenart | Bemessung | Satz | Menge | Betrag | Nutzungsdauer | Runde |
|---|---|---|---|---|---|---|---|
| PV-Module (Hauptposition) | ANSCHAFFUNG | € / kWp | 320,00 | 300,00 kWp — 750 Module × 400 Wp | 96.000,00 | 25 a | 1 |
| Wechselrichter | ANSCHAFFUNG | € / kWp | 80,00 | 300,00 kWp | 24.000,00 | 12 a — Ersatz im Jahr 12 | 1 |
| Unterkonstruktion und Montage | ANSCHAFFUNG | € / kWp | 150,00 | 300,00 kWp | 45.000,00 | 25 a | 1 |
| Elektroinstallation, Netzanschluss, Messkonzept | ANSCHAFFUNG | Betrag | — | — | 18.000,00 | 25 a | 1 |
| Planung und Genehmigung | ANSCHAFFUNG | % der Investition | 5,00 | 183.000,00 € — Stufe: Anlage PV-Feld | 9.150,00 | — (= Zeitraum) | 3 |
| **Summe** | | Investition 192.150,00 € · kein Zuschuss · spezifisch 640,50 €/kWp | | | **192.150,00** | | I₀ |

**Warnband (Befund I-1):** „`Tab_Energieanlagen.PV_Leistung` heißt andernorts ausdrücklich
Modulanzahl. Der Dialog zeigt deshalb beide Größen — 750 × 400 Wp = 300,00 kWp — damit ein
€/kWp-Satz nie mit der Modulzahl multipliziert wird. Ohne diese Herleitung stünde hier 240.000 €
statt 96.000 € (Faktor 2,5)."

**Gruppe Ersatz und Restwert** (i = 3,0 %, T = 20 a):

| Position | n | Ersatzbeschaffung | Restwert Jahr 20 | Herleitung |
|---|---|---|---|---|
| PV-Module | 25 a | — | 19.200,00 | 96.000 × 5 / 25 · Restdauer 5 a |
| Wechselrichter | 12 a | Jahr 12 · 24.000,00 € brutto, nicht indexiert | 8.000,00 | 24.000 × 4 / 12 · Alter 8 a nach Ersatz |
| Unterkonstruktion und Montage | 25 a | — | 9.000,00 | 45.000 × 5 / 25 |
| Elektroinstallation | 25 a | — | 3.600,00 | 18.000 × 5 / 25 |
| Planung und Genehmigung | = T | — | — | ohne Nutzungsdauer: kein Ersatz, kein Restwert |
| **Summe** | | **24.000,00 € · Barwert 16.833** | **39.800,00 € · Barwert 22.036** | |

**Infozeile:** Der Restwert ist eine dokumentierte Abweichung von DIN EN 17463, 6.4 (V-G4). Bei PV
wiegt sie: Der Barwert des Restwerts (22.036 €) übersteigt den des Wechselrichtertauschs (16.833 €).
Der Bericht deklariert das als Modellannahme.

## Was der Dialog zeigt — Reiter Betrieb

| Position | Bemessung | Satz | Herleitung | Betrag |
|---|---|---|---|---|
| Wartung und Reinigung — Pflicht · üblich 10–15 €/kWp·a | € / kWp · a | 12,00 | × 300,00 kWp · PV-Feld | 3.600,00 🔒 |
| Instandhaltung — Pflicht · üblich 0,5–1,0 % | % der Investition | 0,50 | × 192.150,00 € · Investition PV-Feld | 960,75 🔒 |
| Versicherung (Allgefahren) | % der Investition | 0,25 | × 192.150,00 € | 480,38 🗑 |
| Monitoring und Direktvermarkter-Grundgebühr | Jahresbetrag | — | | 600,00 🗑 |
| Hilfsenergie — Standby Wechselrichter, Monitoring · keine Pflicht bei PV | Jahresbetrag | — | | 90,00 🗑 |
| **Betriebskosten PV-Feld** | | | brutto 6.820,04 €/a | **5.731,13** |

**Gruppe Ertrag und Degradation (neu, V-G2):** Ertrag Jahr 1 285,0 MWh/a (Simulationslauf) ·
Degradation 0,50 %/a (0 = keine, Vorgabe) · Quelle „Herstellerdatenblatt, lineare
Leistungsgarantie" · Herleitung: Jahr 20 = 285,0 × 0,995^19 = **259,1 MWh**, Ertragsverlust über
20 Jahre 4,7 % — wirkt auf Einspeisung, Eigenverbrauch und damit auf die Vergütungsreihe.

**Vorschaustreifen:** Betriebskosten p. a. 5.731,13 € · Ersatz Wechselrichter Jahr 12 24.000 € ·
Restwert Jahr 20 39.800 € · spezifische Investition 640,50 €/kWp.

Der dritte Reiter „Ertrag / Bonus" öffnet den Vergütungsdialog aus `06` — kein zweiter Rechenweg.

## Berechnungsgrundlage

```
Investition — Mengenkette € / kWp
  Menge [kWp] = Modulanzahl × Modulleistung [Wp] / 1000
  Betrag      = Menge × Satz
  Befund I-1: die heutige Quelle Tab_Energieanlagen.PV_Leistung ist andernorts die
  Modulanzahl — die Herleitung muss beide Größen zeigen, nie nur eine.
Runde 3 wie beim BHKW: Basis = alle Zeilen der Anlage ohne Zuschuss (siehe 01)

Ersatz und Restwert (Konzept § 3.1, aus der Nutzungsdauer n je Position)
  n = Nutzungsdauer, falls ≥ 1 ; sonst n = T  (dann kein Ersatz, kein Restwert)
  Ersatz:   t_j = round(start + k·n)  für k = 1, 2, …  solange 1 ≤ t_j < T
            → Wechselrichter n = 12, T = 20: t = 12 ; Module n = 25: kein Ersatz
  Restwert: Alter = T − letzte Beschaffung ;  Restdauer = n − Alter
            RW_T = Betrag × Restdauer / n     (nur bei Restdauer > 0, linear)
  Barwert:  Ersatz_t / (1 + i)^t ;  RW_T / (1 + i)^T   — Ersatz NICHT indexiert
  Ersatz und Restwert rechnen mit dem BRUTTObetrag (vor Zuschuss)

Betrieb — nur Gruppen A und C, keine Endenergie-Arten
  Wartung        Betrag = kWp × Satz [€/kWp·a]
  Prozent        Betrag = Investition(Anlage) × Satz / 100
  Hilfsenergie   JAHRESBETRAG — der EndenergieAufloeser liefert für PV null

Degradation (neu, V-G2)   Menge_t = Menge_1 × (1 − d)^(t−1)     Vorgabe d = 0 — ergebnisneutral
```

## Berechnungserläuterung am Beispielprojekt

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| R1 Menge | 750 × 400 / 1000 | 300,00 kWp | beide Größen sichtbar (I-1) |
| R1 Module | 300,00 × 320,00 | 96.000,00 € | Hauptposition |
| R1 Wechselrichter | 300,00 × 80,00 | 24.000,00 € | n = 12 a |
| R1 Unterkonstruktion | 300,00 × 150,00 | 45.000,00 € | |
| R1 Elektro | Betrag, fest | 18.000,00 € | |
| **Basis für Runde 3** | 96.000 + 24.000 + 45.000 + 18.000 | **183.000,00 €** | alle Zeilen der Anlage |
| R3 Planung 5 % | 183.000,00 × 5 / 100 | 9.150,00 € | Stufe „Anlage" |
| **I₀** | 183.000,00 + 9.150,00 | **192.150,00 €** | 640,50 €/kWp; kein Zuschuss |
| Ersatz Wechselrichter | t = round(0 + 1 × 12) = 12 | 24.000,00 € im Jahr 12 | brutto, nicht indexiert |
| — Barwert | 24.000 ÷ 1,03^12 = 24.000 ÷ 1,4258 | − 16.833 € | Kapitalwertwirkung |
| Restwert Module | Alter 20, Restdauer 5: 96.000 × 5 / 25 | 19.200,00 € | |
| Restwert Wechselrichter | Alter 20 − 12 = 8, Restdauer 4: 24.000 × 4 / 12 | 8.000,00 € | |
| Restwert Unterkonstruktion + Elektro | (45.000 + 18.000) × 5 / 25 | 12.600,00 € | |
| **Restwert gesamt** | 19.200 + 8.000 + 12.600 | **39.800,00 €** | Jahr 20 |
| — Barwert | 39.800 ÷ 1,03^20 = 39.800 ÷ 1,8061 | + 22.036 € | deklarierte Modellannahme (V-G4) |
| Betrieb Wartung | 300 × 12,00 | 3.600,00 €/a | Gruppe C |
| Betrieb Instandhaltung | 192.150 × 0,50 / 100 | 960,75 €/a | Gruppe B |
| Betrieb Versicherung | 192.150 × 0,25 / 100 | 480,38 €/a | Gruppe B |
| Betrieb Monitoring + Hilfsenergie | 600 + 90 | 690,00 €/a | Gruppe A |
| **Betriebskosten Jahr 1** | 3.600 + 960,75 + 480,38 + 690 | **5.731,13 €/a** | brutto × 1,19 = 6.820,04 |
| Degradation Jahr 20 | 285,0 × (1 − 0,005)^19 = 285,0 × 0,9092 | 259,1 MWh | −4,7 % |

## Befunde und offene Punkte

| Nr. | Befund | Behandlung im Entwurf |
|---|---|---|
| ⚠ I-1 | `EUR_PRO_KWP` summiert `PV_Leistung` — andernorts die Modulanzahl; ein €/kWp-Satz würde mit der Modulzahl multipliziert (Faktor ≈ 2,5 bei 400-Wp-Modulen) | Herleitungszeile mit beiden Größen; in der Umsetzung Mengenquelle auf kWp festlegen |
| I-4 | `BaugroesseSumme` entdoppelt nicht — bei PV gewollt (Modulanzahl × Leistung) | — |
| V-G2 | Degradation je Position mit Quellenangabe fehlt | neues optionales Attribut, Vorgabe 0 %/a |
| V-G4 | Restwert statt Endzahlung — Abweichung von 6.4 | Restwert bleibt, wird deklariert; Rückbau/Entsorgung als Position mit StartJahr = T abbildbar |
| — | Kumulierung Zuschuss / EEG | Infozeile; ZUSCHUSS-Zeile bleibt erfassbar |
