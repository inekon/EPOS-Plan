# Protokoll AH-V3b — AK3-Profilweg, Auskunftswege, Ankunftsbezug als interne Wahl

**11.10.2026 · Sitzung Gebäudesimulation · Welle V3, zweiter Teil, der Aufheizoptimierung Fassung 2.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitte 2.4, 2.6, 2.7
(AK1/AK2/AK3), 5.1/5.2; aufgesetzt auf [AH-V1](2026-10-10_AH_V1_Deckelreihe.md), [AH-V2](2026-10-10_AH_V2_Vorlauf_Option1.md)
und [AH-V3a](2026-10-10_AH_V3a_Vorausschau_Option2.md). Nur Rechenkern: kein Schema, keine Oberfläche.

## Was gebaut ist

| Baustein | Inhalt |
|---|---|
| AK3-Profilweg | Der Rückfall „AK3 → Sollwertrampe“ entfällt samt `Vorheizrueckfall.Ak3` und Schlüssel `SIMENG_VORH_RUECKFALL_AK3` (de/en). Vorlauf, Vorausschau und Plan rechnen auf dem Profilweg, der im AK3-Weg Pass 1 ist (dieselbe Rückstufe, über die die Sollwertrampe ihren Plan bildet); Sollwert- und Deckelreihe stehen über `Aufheizoptimierung.PlanSetzen` im Eingang, der Plan an der Zone — die Fabrik des Kreises baut auf demselben Eingang (Einzone `AufheizplanSetzen`, Mehrzonen dieselben `ZonenEingang`). `Zonenrechnung.Abschluss` (Abschluss des Kreises) weist das Vorheizen am Kreisergebnis nach und setzt die Gebäudewerte; `Ak3Nachfuehren` nennt danach die Hinweise. Pass 1 schweigt für Gebäude, die in den Kreis gehen; endet der Kreis nicht über das Jahr, nennen die Hinweise die Werte von Pass 1 |
| Heizkreis-Schalter aus | geprüft, unverändert: ein Schalter je Projekt (`Vorheizvorgabe.HeizkreisEinbeziehen`), ausgenommen wird ein Gebäude mit wirksamem Heizkreis (Einzone am Eingang, Mehrzonen über `Waermeuebergabe.KopplungWirksamFuer` am Gebäude) — auch auf Stufe AK3 (Test) |
| Eine Weiche für Lauf und Auskunft | `Vdi6007Rechenweg.AufheizplanEinzone` (Schranke der Verfügbarkeit, Rückfall, Option 1/2 oder Sollwertrampe) ruft der Lauf und die Aufheizauskunft `SimulationWaermebedarf.AufheizbemessungEinesGebaeudes`; Mehrzonen: `VorgabeZonen` (Rückfall) in `RechnenMehrzonen` und `Vdi6007Rechenweg.ZonenBauen`, Vorlauf und Pläne am Ende von `Zonenrechnung.ZonenBauen` (samt Schranke der Verfügbarkeit, vorher in `Rechnen`) |
| `Model/Vorheizvorgabe.cs` | interne Wahl ohne Schema: `Vorheizankunftsbezug` (a) `Sollwert` / (b) `Uebergabe`, `Ankunftsbezug` und `UnerreichbareImMaximum` als `internal init`; Vorgaben `ANKUNFTSBEZUG_VORGABE` = (a), `UNERREICHBARE_IM_MAXIMUM_VORGABE` = ja — das Verhalten vor V3b |
| `Gebaeude/Vorheizankunft.cs` | θ_stat (Herleitung unten) und der Bezug min(θ_T, θ_stat) — nur an Zonen mit wirksamer Wärmeübergabe; Vorausschau (je Sprung einmal, ab den Vorlaufmassen) und Nachweis (ab den Laufmassen) nehmen dieselbe Regel |
| `Vorheizplanung` | `Vorheizzeit`: ohne „im Maximum“ das Maximum der erreichbaren Sprünge (`Vorheizanalyse.BedarfMaxErreichbarH`), unerreichbare werden gezählt und gemeldet wie bisher; `Vorheizplan.Ankunftsbezug` |
| Testnaht | `SimulationWaermebedarf.AufheizvorgabeTestnaht` (statisch, `Func<int, Aufheizvorgabe>`): Proben und Messungen über den ganzen Projektlauf samt Kreis, solange die Spalten fehlen (V4) |
| Tests | `VorheizAk3Tests` (7 Fälle), `VorheizAnkunftsbezugTests` (5 Fälle + Messung); Rückfalltests in `VorheizVorgabeTests`/`VorheizBerechnetTests` auf den Heizkreis umgestellt |

## Herleitung des Bezugs (b)

Zwischen zwei Stunden trägt das 2K-Modell nur die Massentemperaturen x = (θ_m,AW, θ_m,IW); eine Stunde mit festem Rand r ist
die Abbildung x ↦ M_r(x) (`Zonenmodell2K.Schritt` mit Sollwert θ_T, der Grenze der Stunde max(P_K, Φ_ref), Außenluft, Sonne,
Gewinnen, Lüftung, Erdreich, Nachbarluft und Heizkurvenvorlauf samt P-Regler der Übergabe zur Kalenderstunde h_s). Der
durchgewärmte Bau ist ihr Fixpunkt x* = M_r(x*) — die Massen ändern sich über die Stunde nicht, Übergabe und Verluste halten
sich die Waage —, gelöst mit Newton auf F(x) = M_r(x) − x (Jacobi-Matrix 2 × 2 aus Differenzenquotienten, Schritt 10⁻³ K,
Abbruch |F| < 10⁻⁹ K; ohne Konvergenz die einfache Iteration). θ_stat ist die Raumluft am Beginn der Stunde in x*
(`LuftAmBeginn`, dieselbe Regel wie die Ankunft); begrenzt nichts, ist θ_stat = θ_T (geregelter Fall), begrenzt die Übergabe am
Heizkurvenvorlauf, liegt θ_stat darunter. Ankunft (b): δθ = max(0, min(θ_T, θ_stat) − θ_air(Beginn h_s)) ≤ ε. Probe: Der
Fixpunkt lässt die Massen in einer Stunde stehen (< 10⁻⁶ K) und trifft die einfache Iteration über 40 000 Stunden auf 10⁻⁴ K.

## Entscheide der Umsetzung

1. **AK3 ohne eigenen Weg:** Der Profilweg ist der Pass 1 des Kreises; Vorlauf und Vorausschau sehen also die Rückstufe, nicht die
   Anlagengrenzen des Kreises. Was der Kreis nicht liefert, misst der Nachweis am Kreisergebnis (Tage ohne Ankunft,
   Abweichungen von der Vorausschau) — kein eigener Hinweis für AK3 nötig; die Mehrwärme des Kreisnachweises steht gegen den
   Vorlauf des Profilwegs.
2. **(b) nur mit Heizkreis:** Eine Zone ohne wirksame Übergabe misst immer gegen θ_T − ε (bitgleich zu (a), Test); die Unterschreitung
   im Block bleibt gegen θ_T gemessen.
3. **Nachbarn:** Die Vorausschau rechnet θ_stat mit der Nachbarluft des Vorlaufs, der Nachweis mit der des Laufs (je die Bahn,
   auf der er die Ankunft prüft).
4. **„Ohne im Maximum“:** `BedarfMaxH` der Gebäudewerte bleibt das Maximum aller Sprünge (Hinweis „nötig wären …“); t_V ist
   mindestens 1 h.

## Nachweise

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- `VorheizAk3Tests` 7/7: Plan im Stepper (Sollwert- und Deckelreihe bitgleich), Kreis ohne Eingriff = Profilweg bitgleich samt
  Nachweis, Kreis mit Eingriff (300 W in den Fenstern) — mehr Tage ohne Ankunft, Spitze = Spitze des Kreises (Option 1 und 2);
  Projekt 1058 (AK3) rechnet Option 1 und 2 im Kreis ohne Rückfallhinweis, Gebäudewerte am Kreisergebnis; Auskunftswege
  (1051 Einzone, 1054 Mehrzonen, Option 1 und 2) liefern bitgleich denselben Plan wie der Lauf, die Aufheizauskunft ohne Befund.
- `VorheizAnkunftsbezugTests` 5/5: θ_stat Fixpunkt und Gegenprobe, an knapper Übergabe (Nennleistung 0,4·Φ_HL) θ_stat < θ_T;
  (b) mindert die unerreichbaren Sprünge; (b) ohne Heizkreis bitgleich (a); ohne „im Maximum“ t_V = Maximum der erreichbaren;
  Vorgaben = Verhalten vor V3b (bitgleich).
- Betroffene Klassen (Filter `Vorheiz`, `Aufheiz`, `Zonen`, `Anlagenkopplung`, `Ak3`, `GebaeudeModell`, `GebaeudeStepper`,
  `Stundenrand`, `Kappungsanteil`, `SimulationWaermebedarf`, `GebaeudeBedarf`, `Zonenmodell`, Ressourcen-, Designer-, Text-,
  Kodierungs-, Double-, Link- und Modulwachen): 1 907 Fälle, 1 906 grün, 1 übersprungen (Bestand), 0 rot; Link- und
  Ordnungswache nach dem Protokoll 24/24.
- Referenzlauf aller 29 Projekte gegen `2026-10-10_R51_FreieKuehlung`: 29 von 29, `GESAMT: PASS` (10 036 768 Werte), 943 CSV
  byte-gleich, 0 abweichend.

## Messung (Entscheidungsgrundlage)

Option 2, ε 1 K, Geltung Gebäude, die übrigen Felder der Projektvorgabe (eingeschaltet), ganzer Projektlauf über die Testnaht
(1058: Nachweis am Kreis). Mehrwärme gegen den Lauf mit der gespeicherten Vorgabe = Basis R51 (1054 und 1051 mit Sollwertrampe,
1047, 1056, 1058 ohne Aufheizoptimierung). Bedarf Median / Max über alle Sprünge (unerreichbare mit min(D, 47)); „Tage ohne
Ankunft“ im Lauf gegen den jeweiligen Bezug; Unterschreitung im Block gegen θ_T; Spitze der Gebäudeheizlast.

| Projekt | Bezug | Unerreichbare im Max. | t_V [h] | Bedarf Median / Max [h] | Tage unerreichbar | Tage ohne Ankunft | Unterschreitung max [K] | Nächte ohne Absenkung | Spitze [kW] | Mehrwärme gegen Basis |
|---|---|---|---|---|---|---|---|---|---|---|
| 1054 (AK1, 3 Zonen) | (a) | mit | 47 | 2 / 47 | 182 | 179 | 3,01 | 567 | 23,4 | +6 148 kWh (+12,9 %) |
| 1054 | (a) | ohne | 7 | 2 / 47 | 182 | 180 | 4,33 | 0 | 24,1 | +3 369 kWh (+7,1 %) |
| 1054 | (b) | mit | 18 | 1 / 18 | 1 | 0 | 3,53 | 565 | 23,6 | +5 015 kWh (+10,5 %) |
| 1054 | (b) | ohne | 18 | 1 / 18 | 1 | 0 | 3,53 | 565 | 23,6 | +5 015 kWh (+10,5 %) |
| 1056 (AK1, Fahrplan) | (a) | mit / ohne | 8 | 8 / 8 | 221 | 220 | 6,00 | 359 | 47,0 | +2 146 kWh (+3,3 %) |
| 1056 | (b) | mit / ohne | 8 | 8 / 8 | 189 | 188 | 6,00 | 359 | 47,0 | +2 146 kWh (+3,3 %) |
| 1047 (AK1, Einzone) | (a) | mit / ohne | 8 | 5 / 8 | 9 | 5 | 1,45 | 359 | 40,7 | +4 580 kWh (+6,6 %) |
| 1047 | (b) | mit / ohne | 2 | 1 / 2 | 0 | 0 | 2,31 | 0 | 41,9 | +983 kWh (+1,4 %) |
| 1058 (AK3) | (a) | mit / ohne | 8 | 5 / 8 | 9 | 1 | 1,31 | 359 | 40,5 | +4 490 kWh (+6,4 %) |
| 1058 | (b) | mit / ohne | 2 | 1 / 2 | 0 | 0 | 2,19 | 0 | 41,9 | +1 094 kWh (+1,6 %) |
| 1051 (ohne Heizkreis) | (a) = (b) | mit / ohne | 5 | 1 / 5 | 0 | 0 | 0,82 | 0 | 28,3 | +1 760 kWh (+7,3 %) |

Rechenzeit je Projektlauf 0,3–3,0 s. Lesart:

- **1054:** (a) mit — Durchheizen (t_V 47 h, 567 Nächte über drei Zonen) ohne Nutzen, 179 Tage ohne Ankunft; (a) ohne — t_V 7 h,
  die Tage ohne Ankunft bleiben (180), die Unterschreitung wächst auf 4,3 K. (b) — die Übergabe hält am warmen Bau θ_T nicht;
  gegen ihre stationäre Luft kommt fast jeder Sprung an (1 unerreichbar, 0 Tage ohne Ankunft); t_V = 18 h (ein Sprung mit Bedarf
  18 h, Median 1 h) hebt die Werktagsabsenkung (D = 13 h) auf. „Im Maximum“ ist unter (b) gleichgültig.
- **1047/1058:** Der Kalender hat kurze Absenkungen (längstes Fenster 8 h); unter (a) sind 9 Sprünge unerreichbar, sie ziehen t_V
  auf 8 h = jede Nacht ohne Absenkung (359) — mit wie ohne „im Maximum“, weil auch die erreichbaren bis 8 h brauchen. (b) setzt
  t_V = 2 h, keine Nacht ohne Absenkung, Mehrwärme 1,4–1,6 % statt 6,4–6,6 %; die Unterschreitung gegen θ_T (2,2–2,3 K) ist die der
  Übergabe, nicht des Vorheizens. 1058 im Kreis wie 1047 im Profilweg (dieselbe Hülle).
- **1056:** Die Nachtsperre der Wärmepumpe (0–6 Uhr) und `Vorlauf_Max` 50 °C begrenzen im Fenster selbst; (b) misst nur die
  Übergabe zur Stunde h_s und ändert t_V nicht (8 h), mindert die unerreichbaren Tage von 221 auf 189. Hier hilft keine der
  Wahlen — das Fenster liegt in der Sperre.
- **1051:** ohne Heizkreis — beide Wahlen ohne Wirkung (bitgleich).

## Offen

1. **Entscheid des Anwenders:** Ankunftsbezug (a)/(b) und „unerreichbare Sprünge im Maximum“ — umgestellt wird je eine Vorgabe in
   `Vorheizvorgabe` (`ANKUNFTSBEZUG_VORGABE`, `UNERREICHBARE_IM_MAXIMUM_VORGABE`). Unter (b) bleibt an 1054 ein einzelner Sprung
   mit 18 h Bedarf, der t_V für das Jahr setzt (F13 „fest“); ein Quantil statt des Maximums wäre eine weitere Wahl.
2. **Fahrplan (1056):** Vorheizfenster in einer Sperrzeit können nicht ankommen; Vorschlag für V4/V5: Sperrstunden in der
   Vorausschau benennen („Fenster in der Sperrzeit“) oder das Fenster vor die Sperre legen.
3. **AK3-Feldläufe (Erdsonde):** Die Hinweise des Kreises erscheinen einmal je Lauf (`HinweisEinmal`) — mit mehreren Feldläufen
   die des ersten.
4. **V4:** Spalten für `Vorheizvorgabe` samt der beiden Wahlen, falls sie wählbar bleiben sollen, und die Ergebnisgrößen;
   die Testnaht `AufheizvorgabeTestnaht` bleibt bis dahin der Weg der Messung.
5. Die Messung bleibt als `VorheizAnkunftsbezugTests.Messung_Ankunftsbezug_und_Maximum` (nur mit `EPOS_MESSUNG=1`).
