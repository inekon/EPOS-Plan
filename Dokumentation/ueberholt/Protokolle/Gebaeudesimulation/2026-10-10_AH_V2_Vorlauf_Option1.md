# Protokoll AH-V2 — Vorlauf, Deckel, Vorheizplan Option 1 und Nachweis im Lauf

**10.10.2026 · Sitzung Gebäudesimulation · Welle V2 der Aufheizoptimierung Fassung 2.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitte 2.1–2.5, 2.7,
2.9, 5.1/5.2 (E122, E124) und Wellenplan Zeile V2; aufgesetzt auf
[AH-V1](2026-10-10_AH_V1_Deckelreihe.md). Nur Rechenkern: kein Schema (V4), keine Oberfläche (V5), keine Option 2 (V3).

## Was gebaut ist

| Baustein | Inhalt |
|---|---|
| `Model/Vorheizvorgabe.cs` | Einstellung ohne Schema: `Aufheizverfahren` (Sollwertrampe / Vorgabe / Berechnet), t_V 1–47 h, `Vorheiztoleranzart` (% / kW) und Wert (leer = Kern-Konstante 20 %), ε (leer = 1 K), `Vorheizgeltung`, Heizkreis-Schalter (Vorgabe an); `DeckelW(Φ_K,max)` |
| `Aufheizvorgabe.Vorheizen` | `init`-Eigenschaft, Vorgabe `Vorheizvorgabe.Sollwertrampe`; `Eingeschaltet()` behält sie |
| `Gebaeude/Vorheizplanung.cs` | Weiche (`Anwendbar`, `Rueckfall`, `Wirksam`), Vorlauf und Plan (`AnwendenEinzone`, `AnwendenZonen`, `Planen`), Φ_ref (`PhiRef` = `Aufheizoptimierung.PhiStat`), Blockende, Nachweis (`Nachweisen*`), Gebäudewerte; Datentypen `Vorheizplan`, `Vorheizsprung`, `Vorheizpruefung`, `Vorheiznachweis`, `Vorheizgebaeude` |
| `Zonenmodell2K.LuftAmBeginn(in Stundenrand)` | Raumluft im Augenblick am Beginn einer Stunde aus dem Zustand der Massen: der erste Fall des Lösers, geregelt = Sollwert, sonst Ausgang 2 des Falls (Heizgrenze, Totband, Übergabe mit Leitwert); rein |
| `GebaeudeModellEingang.MassenErfassen`, `Zonenlauf`, `GebaeudeModellErgebnis.MassenEndeAw/Iw` | Massen am Stundenende je Stunde, nur mit Schalter (Vorlauf für V3, Lauf für die Ankunft) |
| `Aufheizplan.Vorheizen`, `GebaeudeModellErgebnis.Vorheizen` | Plan samt Nachweis je Zone; Gebäudewerte am Ergebnis (auch skaliert) — der Ergebnisschreiber (V4) holt sie dort ab |
| `Vdi6007Rechenweg`, `Zonenrechnung`, `ZonenEingang.Bauen` | Einzone: Rückfall prüfen → Vorlauf → Plan → Lauf → Nachweis; Mehrzonen: Bestandsrampe in `Bauen` nur ohne Option 1, Vorlauf und Pläne in `Zonenrechnung.Rechnen` nach der Schranke der Verfügbarkeit (AK2), Nachweis nach dem Lauf; Bestandshinweise W1–W5 entfallen mit Option 1 |
| `HinweisVorheizen`, zehn Schlüssel `SIMENG_VORH_*` | Kennzahlen (t_V, Φ_K,max, P_K, P_V, Spitze, Nächte ohne Absenkung, Mehrwärme), Tage ohne Ankunft mit größter Unterschreitung, Floor-Stunden, Toleranz 0, Zonen ohne Sprung, Übergänge aus „aus“, vier Rückfälle — einmal je Gebäude, Deutsch und Englisch |
| `EPOS.Kern.Tests/VorheizVorgabeTests` | 15 Fälle, siehe Nachweise |

## Entscheide der Umsetzung

1. **Vorgabe bitgleich:** Ohne `Vorheizen` oder mit Verfahren Sollwertrampe läuft der Bestand Zeichen für Zeichen; nur
   Verfahren „Vorgabe“ mit t_V ruft die neue Planung.
2. **Rückfall auf die Sollwertrampe mit Hinweis:** Projekt AK3 (Profilweg kommt mit V3), Gebäude mit wirksamem Heizkreis
   bei Schalter aus (F16; dort rechnet der Bestand „gekoppelt, nicht optimiert“), Verfahren „Vorgabe“ ohne t_V, Verfahren
   „Berechnet“ (V3). AK3 wird am Projekt erkannt (`Ak3Kernstufe.Wirksam`), der Heizkreis am Gebäude.
3. **t_V:** `Aufheizzeit_Manuell_H` des Gebäudes übersteuert die Projektzeit (2.5); Zonen erben sie.
4. **P_verf** ist die heutige Aufheizleistung der Bemessung (`Heizleistung_Max`, sonst (1 + ρ)·Φ_stat); fehlt sie, +∞.
5. **Zwei Anstiege** (16 → 18 → 20 °C): Das Fenster des zweiten Sprungs beginnt frühestens am ersten; die Nacht zählt nur
   ohne diese Kürzung. Überlappen Fenster und Block, gilt die größere Grenze (der Block schützt die Heizlast der Stunde).
6. **Blockende:** die Stunde vor dem nächsten Sprung nach unten (über 0,01 K mit Rechenrand) oder vor „aus“.
7. **Ankunft:** δθ = θ_T − θ_air(Beginn h_s), θ_air aus den Massen am Ende von h_s − 1 und dem Rand von h_s
   (`LuftAmBeginn`; mit Nachbarn deren Stundenmittel von h_s); angekommen, wenn ε ≥ δθ über `Rechenrand.SchwelleErreicht`.
   Unterschreitung: Stundenmittel im Block unter θ_T − ε; Deckelstunden: Kappungsanteil > 0 im Block (jede Stunde einmal).
8. **Geltung Gebäude** rechnet bis V3 je Zone (Zonenanteile über Φ_HL kommen mit V3); die Gebäudewerte sind die
   Vereinigung bzw. Summe der Zonen, die Spitze die der Gebäudeheizlast.
9. **AK2:** Der Vorlauf der Zonenschleife sieht die Schranke der Verfügbarkeit (gesetzt vor dem Vorlauf); im Einzonenweg
   ebenso (gesetzt vor dem Plan).

## Nachweise

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- `VorheizVorgabeTests`: 15/15 grün — Einstellung und Kernkonstanten, Rückfälle, Rückfallhinweis AK3 einmal je Gebäude
  (de/en), Fenster/Sollwert/Deckelreihe/Floor je Stunde (Werktag D = 13, Wochenende D = 61, Ferien D > 47, zwei Anstiege,
  „aus“ ohne Fenster), kW-Toleranz und t_V = 15 ≥ D (Nacht ohne Absenkung), manuelle Aufheizzeit, Nachweisgrößen und die
  Grenze in jeder Stunde, längeres Vorheizen kommt öfter an und kostet mehr, Mehrzonen mit Geltung Zone, AK1 mit Schalter
  an/aus, Probe erste Ordnung (drei Fälle), Sollwertrampe bitgleich über die Fassade (Projekt 1018), Messung 1051.
- Betroffene Klassen (Filter `Vorheiz`, `Aufheiz`, `Zonen`, `Anlagenkopplung`, `Ak3`, `GebaeudeModell`,
  `Kappungsanteil`, Text-, Kodierungs-, Double- und Linkwachen): 1 356 Fälle, 1 355 grün, 1 übersprungen (Messung nur mit `EPOS_MESSUNG=1`), 0 rot.
- Referenzlauf aller 29 Projekte gegen `2026-10-10_R51_FreieKuehlung`: 29 von 29 gerechnet, `GESAMT: PASS` (10 036 768 Werte), 943 CSV byte-gleich, 0 abweichend.

**Probe gegen die erste Ordnung (2.3).** Bau ohne Sonne und Gewinne bei −5 °C, Massen im Gleichgewicht bei 16 °C, Fenster
47 h vor 20 °C unter P_V = Φ_stat + Δ. Die erste Ordnung t₁ = C_w·ΔT/(P_V − Φ_stat) lädt die ganze Masse und ist obere
Schranke; das Plateau des Laufs (Σ Kappungsanteile) liegt bei 0,40–0,51·t₁ (Δ = 1 kW: 12,2 h gegen 27,0 h; 2 kW: 6,9 gegen
13,5 h; 4 kW: 2,7 gegen 6,8 h), weil die schnelle Mode die Luftseite vor der Masse lädt. Gehalten im Band [0,25; 1,0]·t₁.

## Messung an 1051 (Gebäude 10657, Toleranz 20 %, Testdatenbank)

| Größe | t_V = 6 h | t_V = 15 h | Entwurf 2.8 |
|---|---|---|---|
| Φ_K,max | 23,62 kW | 23,62 kW | ≈ 23,5 kW |
| P_K = P_V | 28,34 kW | 28,34 kW | ≈ 28,2 kW |
| Jahresspitze des Laufs | 28,34 kW | 28,34 kW | ≤ P_K |
| S_max,0 (Vorlauf) / S_max (Lauf) | 24,06 / 0,15 kW | 24,06 / 0,62 kW | — |
| Tage ohne Ankunft (ε = 1 K) | 0 | 0 | — |
| größte Unterschreitung im Block | 0,59 K (0 h über ε) | 0,00 K | — |
| Deckelstunden / Floor-Stunden | 3 / 0 | 0 / 0 | — |
| Nächte ohne Absenkung | 0 | 117 | jede Werktagsnacht |
| Übergänge aus „aus“ | 1 | 1 | — |
| Heizwärme Vorlauf | 23 676 kWh | 23 676 kWh | — |
| Mehrwärme | +2 671 kWh (+11,3 %) | +4 260 kWh (+18,0 %) | ≈ +10 % bei 15 h |

Die Mehrwärme ist gegen den Vorlauf **ohne jedes Vorheizen** gemessen (Entwurf 2.5); schon 6 h kosten 11 %, 15 h 18 %. Der
Entwurf schätzt +10 % für 15 h gegen die Absenkung mit Rampe; der Unterschied ist offen (Punkt 3 unten).

## Offen für V3/V4

1. **V3:** Option 2 (Vorausschau ab `Vorheizplan.VorlaufMassenAw/Iw`, Bisektion, Bedarf t_nötig, „unerreichbar“), Geltung
   Gebäude mit Zonenanteilen über Φ_HL, AK3-Profilweg (heute Rückfall), Bedarf t_nötig im Hinweis („nötig wären …“).
2. **V4:** Spalten für `Vorheizvorgabe` und die Ergebnisgrößen (`GebaeudeModellErgebnis.Vorheizen`, `Aufheizplan.Vorheizen`
   je Zone); `KonfigurationCtrl.AufheizvorgabeLesen` füllt `Vorheizen`. Die Auskunftswege (`Vdi6007Rechenweg.ZonenBauen`,
   `SimulationWaermebedarf` Aufheizauskunft) rufen Option 1 noch nicht — mit gesetztem Verfahren hätte ein Mehrzonengebäude
   dort keinen Plan; bis V4 kann kein Datenweg das Verfahren setzen.
3. **Messung 1051:** Mehrwärme bei 15 h 18 % statt ≈ 10 % — gegen welche Bezugsgröße der Entwurf rechnet (Vorlauf ohne
   Vorheizen oder Bestandsrampe), klärt V3 mit der Messung an der Kopie von 1051.
4. Die Ergebniszeile des Bestands (`Aufheizergebnis`) wird mit Option 1 aus dem Plan gefüllt (Fensterlänge als n − 1,
   Zustand BEMESSEN); welche Spalten V4 dafür neu braucht, entscheidet V4.
