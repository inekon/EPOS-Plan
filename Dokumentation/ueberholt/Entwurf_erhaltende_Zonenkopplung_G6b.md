# Entwurf: Erhaltende Zonenkopplung (G6b) — Analyse zur Entscheidung

**Art:** Entwurf, reine Analyse, keine Umsetzung. **Entschieden am 26.09.2026 (K1–K3, Abschnitt 7):**
V0 — der Rechenweg bleibt, Probe 4 misst die Erhaltung; die Messung von 4 (d) verlangt keine
erhaltende Kopplung. Er folgt dem
[Mehrzonenkonzept](../aktuell/Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (2.2 Punkt 3, 2.3) und
[ADR-005](../aktuell/ADR-005_Zonenkopplung_Mehrzonenmodell.md). Bis zur Entscheidung war er ein eigenes Papier;
entschieden mit **E49** (Konzept N1.55) liegt er unter `ueberholt/`.
**Anlass:** Anwenderentscheid vom 26.09.2026, Nr. 1, zum Befund der Welle W4, Probe 4 (Bilanz −6,7 %).
**Stand der Rechnung:** G6b W4c (`a3448e7cb`), Laufgrenze 1.

## 1. Ergebnis vorab

1. **Der Befund der Welle W4 zu Probe 4 wird berichtigt.** Die −6,7 % messen nicht, was der Heizlast
   fehlt. Sie messen die *Zuordnung* des Nachbarglieds am Massenknoten der Außengruppe,
   g_Rest·B_NR·(θ̄_m,AW − θ̄_air,NR). Diese Größe ist kein physikalischer Strom durch die Trennwand:
   θ_m,AW ist die gemeinsame Masse aller Bauteile der Außengruppe, und die Außenwände prägen sie.
2. **Stationär ist die Kopplung erhaltend,** bis auf den Faktor k der Näherung in Gl. (27)/(28), siehe
   Abschnitt 2. Belegt ist das durch die Messung in Abschnitt 2: Zwei gleiche Zonen bei 20/20 °C haben
   keinen Strom über die Trennwand. Trotzdem zeigt die Zuordnung −5,07 MWh, und die AW-Rechnung braucht
   **mehr** Heizwärme als die IW-Rechnung, nicht weniger.
3. **Was dynamisch bleibt:** Die Speichermasse der Trennwand zählt doppelt (MZ 2.2 Punkt 3). Die
   Nachbarluft wirkt außerdem über die Zeitkonstante der zusammengefassten Außengruppe. Wie groß das
   ist, misst bisher keine Probe.
4. **Empfehlung:** Entscheid 1 neu fassen. Zuerst ändert sich allein die Probe 4 (V0, Abschnitt 3):
   Die stationäre Erhaltung wird nachgewiesen und die dynamische Abweichung gegen eine
   wandaufgelöste Referenz gemessen. Eine erhaltende Kopplung wird erst gebaut, wenn diese Messung sie
   verlangt, und dann als eigene Trennflächengruppe (V4). Die Varianten, die nur umbuchen (V1, V3, V5),
   werden nicht empfohlen.

## 2. Wo der Strom „fehlt"

**Je Zone z** strömt aus der Masse der Außengruppe zum Knoten θ_eq:
F_z = g_Rest·(θ_m − θ_eq) = Σ_v g_Rest·B_v·(θ_m − θ_v), mit Σ B_v = 1 (Gl. (41)/(42)). Der Anteil
v = Nachbar j geht an einen Randknoten θ_air,j. Zone j nimmt ihn nicht auf. Sie rechnet ihre
gespiegelte Kopie der Wand gegen θ_air,z. So stehen die Gleichungen, und das ist VDI 6007-1 Gl. (40):
Der Nachbarraum ist eine Randbedingung.

**Stationär** gilt F_z = k_z·ΣUA_z·(θ_air,z − θ_eq,z). Der Faktor ist k_z = (1/ΣUA)/R_Kette. Die Kette
von der Luft zu θ_eq verläuft über die Oberfläche, R₁ und R_Rest. Gl. (28) setzt dabei
R_α,i = 1/(g_konv,AW + g_str) an, den Strahlungspartner also auf Lufttemperatur. Stationär liegt er
dazwischen (g_konv,IW mit g_str in Reihe). Deshalb ist k ≈ 1, aber nicht genau 1. Für das Zonenpaar
bleibt k_z·UA·(θ_z − θ_j) + k_j·UA·(θ_j − θ_z) = (k_z − k_j)·UA·Δθ. Die Kopplung ist also bis auf den
Unterschied der k erhaltend. Bei gleichen Zonen ist sie genau erhaltend. U·A der Gegenseite ist
gleich, denn die Schichtfolge wird umgekehrt und die Übergänge werden getauscht.

**Gemessen** an zwei gleichen Hälften des Probegebäudes (je 100,5 m², 30 m² Trennwand,
Heizwärme in MWh/a):

| Sollwerte | Klima | Trennwand AW | Trennwand IW | AW − IW | Zuordnung Nachbarglied |
|---|---|---|---|---|---|
| 20/20 °C | Jahresgang | 74,663 | 72,945 | +2,36 % | −5,07 |
| 22/20 °C | Jahresgang | 81,461 | 79,555 | +2,40 % | −5,49 |
| 20/20 °C | konstant 0 °C, ohne Sonne | 154,724 | 151,949 | +1,83 % | −9,81 |
| 22/20 °C | konstant 0 °C, ohne Sonne | 162,923 | 160,006 | +1,82 % | −10,33 |

- **Bei 20/20 °C** fließt physikalisch nichts über die Trennwand. Die Zuordnung meldet trotzdem
  −5,07 MWh bzw. −9,81 MWh, und die AW-Rechnung liegt höher, nicht tiefer. Die Zuordnung ist also
  kein Energiefehler der Heizlast.
- **Zuwachs von 20 auf 22 °C in Zone 1, stationär:** Der Zuwachs beträgt +8,199 MWh (AW) bzw.
  +8,057 MWh (IW). Das Verhältnis ist 1,0176. Das Verhältnis der Grundlasten ist 1,0183, die beiden
  weichen um 0,06 % voneinander ab. Der Austausch über die Trennwand kostet das Gebäude also nichts
  außer dem Faktor k. Das Gebäude ist stationär erhaltend.
- **Der Abstand AW − IW** (+1,8 % stationär, +2,4 % im Jahresgang) ist der Befund von Probe 3. Er
  kommt von der Näherung in Gl. (27)/(28), nicht von der Kopplung (Band 3 %, Entscheid Nr. 2).

## 3. Formulierungen

| Variante | Kern | Erhaltend | Stationär richtig | Probe 1 Lauf 1 (TB10) bitgleich | Speicher | Aufwand |
|---|---|---|---|---|---|---|
| **V0** wie jetzt, Probe 4 umgestellt | keine Änderung am Rechenweg | stationär bis k | ja | ja | doppelt | 0,5 PT |
| **V1** Gutschrift des Nachbarglieds | F_zj als Quelle an Luft oder AW-Oberfläche von j | per Buchung | nein (Transfer doppelt) | nein | doppelt | 1,5 PT |
| **V2** symmetrisch halbiert | je Seite halbe Wand in der AW-Gruppe, gegenseitige Gutschrift | per Buchung | ja | nein (TB10 neu messen) | einfach | 2–3 PT |
| **V3** Korrektur auf den Luft-Luft-Strom | Quelle K_z so, dass der Nettotausch UA·(θ_z − θ_j) ist | ja | ja | nur ohne vorgegebenen Nachbarn | doppelt | 1,5–2 PT |
| **V4** eigene Trennflächengruppe | dritter Massenzweig je Zone für die Trennwände, halbiert, gegenseitige Gutschrift | ja, physikalisch | ja | nein (TB10 neu messen) | einfach | 6–10 PT |
| **V5** einseitige Führung | führende Zone rechnet nach Gl. (40), Gegenseite IW plus Gutschrift | per Buchung | nur mit falscher Zuordnung | ja, wenn der Raum führt | einfach | 2–3 PT |

**Vor- und Nachteile:**
- **V1** bucht die Zuordnung um, die θ_m kalt macht, und verdoppelt den stationären Transfer. Nicht empfohlen.
- **V2** ist erhaltend und stationär richtig. Die Wand ist in jeder Zone aber halbiert, damit ist
  Testbeispiel 10 nicht mehr die Rechnung der Richtlinie. Der Messentscheid A7 und Probe 11 wären neu
  zu führen.
- **V3** erzwingt den Luft-Luft-Strom über eine Quelle in der Größe der Zuordnung, also einige kW.
  Sie verzerrt die Dynamik des validierten Einzonenmodells. Nicht empfohlen.
- **V4** löst die Ursache: Die Trennwand bekommt ihre eigene Masse, ihr Strom ist ein echter Strom.
  Dafür ist es eine zweite Physik gegen MZ 2.1. Der geschlossene 2×2-Weg (Sylvester) entfällt, und
  der Löser braucht 3×3 oder ein numerisches Matrixexponential. Die Iteration bleibt Gauß-Seidel.
- **V5** hält Testbeispiel 10 für die führende Seite. Die Gutschrift muss dann aber die Zuordnung sein
  (sonst nicht erhaltend), mit falschem Vorzeichen, sobald die Außenwände die Masse prägen. Außerdem
  hängt das Ergebnis davon ab, welche Seite führt. Nicht empfohlen.

**Bitgleichheit:** Alle Varianten wirken nur im Mehrzonenweg und nur an Trennflächen der Außengruppe.
Unberührt bleiben der Einzonenweg (N = 1, Netz W0, Probe 10, Referenzlauf 14/14), Trennflächen der
Innengruppe (Probe 2) und der Fixpunkt (Probe 5a). Von einem vorgegebenen Nachbarn (Probe 1 Lauf 1)
bucht keine Variante etwas; V2 und V4 ändern trotzdem die Wand der Wohnzone.

## 4. Proben

- **Probe 4, neu:**
  - (a) Luftströme stündlich Σ = 0 (steht).
  - (b) Bilanz je Zone < 0,1 % (steht, gemessen 7e‑8).
  - (c) **Stationäre Erhaltung:** Klima konstant, Zuwachs der Gebäudeheizwärme bei einer
    Sollwertänderung, AW gegen IW im Verhältnis der Grundlasten < 0,1 %. Gemessen sind 0,06 %.
  - (d) **Dynamische Erhaltung gegen eine wandaufgelöste Referenz:** Die Trennwand ist ein eigener
    Knoten, einmal gespeichert, im 5×5-System exakt diskretisiert. Das ist eine Erweiterung der
    Probe 5b. Gemessen wird die Abweichung der Jahresheizwärme; das Kriterium folgt der Messung.
- Die Zuordnung des Nachbarglieds wird nur ausgewiesen, nicht gehalten.
- Bei V2 und V4 kommen neu hinzu: Testbeispiel 10 mit der Trennfläche (A7, Probe 11), Proben 3, 5b,
  6, 7 und die Messprobe; bei V4 zusätzlich die Normfälle der Trennflächengruppe.

## 5. Aufwand

- **V0 mit Probe 4 (c) und (d):** 1,5–2 PT, davon die wandaufgelöste Referenz 1–1,5 PT.
- **V4 zusätzlich:** 6–10 PT, dazu die Neumessung von A7 und Probe 11 und ein Papier zur zweiten Physik.

## 6. Anwenderfragen

| # | Frage | Optionen | Empfehlung |
|---|---|---|---|
| **K1** | Gilt Entscheid Nr. 1 („erhaltende Kopplung") nach der Berichtigung weiter? | (a) V0: Probe 4 umstellen, Freischaltung (W5) ohne Umbau; (b) erhaltende Kopplung vor W5 | **(a)**. Stationär erhaltend ist nachgewiesen; die −6,7 % sind eine Zuordnung. |
| **K2** | Wie wird die dynamische Abweichung beurteilt (Probe 4 (d))? | (a) messen und benennen, Kriterium danach; (b) festes Kriterium vorab, z. B. 1 % der Jahresheizwärme | **(a)**, wie A8 |
| **K3** | Wenn Probe 4 (d) eine erhaltende Kopplung verlangt: welche Variante? | V2, V4, V5 (V1 und V3 fallen weg) | **V4**, als eigene Stufe nach G6b mit eigenem Einfrierweg für TB10 |

## 7. Entscheide und Messung

**Anwenderentscheide vom 26.09.2026** (sie ersetzen Entscheid Nr. 1 zum Befund der Welle W4):

- **K1 = V0.** Der Rechenweg bleibt unverändert, die Freischaltung (W5) folgt ohne Umbau.
- **K2.** Probe 4 (d) wird gemessen, das Kriterium danach benannt, wie bei A8.
- **K3.** Verlangt die Messung eine erhaltende Kopplung, kommt V4 als eigene Stufe nach G6b.

**Messung (G6b W4d)** an zwei Hälften des Probegebäudes, 30 m² Trennwand der Außengruppe, beide
Zonen durchgehend geheizt (ohne Sonne, Strahlungsanteil der Heizung 0, Sollwerte 22 und 20 °C):

| Probe | Fall | Ergebnis | Kriterium |
|---|---|---|---|
| 4 (c) stationäre Erhaltung | Klima konstant 0 °C; Kopplung gegen „jede Zone sieht den Nachbarn auf der eigenen Temperatur" | Austausch je Zone ±0,963 MWh/a; Rest im Gebäude 5·10⁻¹⁶ relativ | < 0,1 % |
| 4 (d) gegen die wandaufgelöste Referenz | stationär (Klima −5 °C konstant) | Lauf 180,837, Referenz 180,758 MWh/a: +0,044 % | < 0,1 % |
| 4 (d) | Jahresgang −15 K | 188,411 gegen 188,329 MWh/a: +0,043 %; Anteil der Dynamik −7·10⁻⁶ | < 0,1 %; Dynamik < 0,01 % |
| 4 (d) | Jahresgang −15 K, Nachtabsenkung 18/17 °C | 179,805 gegen 179,726 MWh/a: +0,044 %; Anteil der Dynamik 3·10⁻⁶ | wie oben |

Die Referenz führt die Trennwand als eigenen Massenknoten, mit ihrer ganzen Masse einmal. Je Seite
hat sie den halben Wandwiderstand und den Übergang 1/(α_kon + α_str) zur Raumluft. Die Zonen haben
dort keine Trennwand, und das 5×5-System ist je Stunde exakt diskretisiert. Die Stundenabweichung
der Gebäudeheizlast liegt bei höchstens 9 W stationär, 57 W im Jahresgang und 256 W mit
Nachtabsenkung. Das ist die Phasenlage der doppelt geführten Speichermasse. Über das Jahr gleicht
sie sich auf 10⁻⁵ aus.

**Folge nach K3:** Eine erhaltende Kopplung ist nicht verlangt, V4 entfällt. Die Zuordnung des
Nachbarglieds (−6,7 %) führt Probe 4 nur noch als benannte Information.
