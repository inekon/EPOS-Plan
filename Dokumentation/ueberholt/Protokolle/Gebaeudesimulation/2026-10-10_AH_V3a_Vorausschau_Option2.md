# Protokoll AH-V3a — Vorausschau, Option 2 „Vorheizzeit berechnen“, Bedarf, Geltung Gebäude

**10.10.2026 · Sitzung Gebäudesimulation · Welle V3, erster Teil, der Aufheizoptimierung Fassung 2.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitte 2.1, 2.3–2.8,
5.2 (E124 F13: nur „fest“); aufgesetzt auf [AH-V1](2026-10-10_AH_V1_Deckelreihe.md) und
[AH-V2](2026-10-10_AH_V2_Vorlauf_Option1.md). Nur Rechenkern: kein Schema, keine Oberfläche, kein AK3-Profilweg (V3b).

## Was gebaut ist

| Baustein | Inhalt |
|---|---|
| `Gebaeude/Vorheizvorausschau.cs` | Vorausschau eines Sprungs: eigenes `Zonenmodell2K` derselben Parameter ab den Vorlaufmassen am Ende von h_s − t − 1, t Stunden mit θ_T (Kühlkappe und max(s, …) wie im Plan) und der Grenze P_V auf den echten Randwerten der Stunden, Ankunft am Beginn von h_s mit derselben Regel wie der Nachweis (`LuftAmBeginn`, Grenze der Stunde max(P_K, Φ_ref), δθ ≤ ε über `Rechenrand`); `Bedarf` (Bisektion über t ∈ {1 … min(D, 47)} mit Wächter t = tMax + 1, höchstens ⌈log2(tMax + 1)⌉ = 6 Vorausschauen), `BedarfLinear` (Probe), Zähler für Vorausschauen und Schritte |
| `Stundenrand.MitVorheizen` | Rand einer Vorausschaustunde: Sollwert und Leistungsgrenze ersetzt, eine kleinere Schranke der Verfügbarkeit (AK2) bleibt; `ThetaSoll` dafür `private init` |
| `Vorheizanalyse`, `Vorheizplanung.Analysieren` | die Größen vor dem Plan je Zone: Sprünge, längstes Fenster min(D, 47) am vorigen Sprung gekürzt, Φ_ref, S_0, Φ_K,max, P_verf, P_K, P_V; mit Option 2 Bedarf und „unerreichbar“ je Sprung |
| `Vorheizplanung.GebaeudedeckelVerteilen` | Geltung Gebäude (2.7): Φ_K,max,Geb = max über die Sprungstunden aller Zonen von Σ_i Φ_ref,i(h), P_K,Geb daraus, Anteile P_K,i nach Φ_HL,i (Auslegungsheizlast E97, `GebaeudeModellEingang.AuslegungsheizlastW` aus 8.4, über `Aufheizzone.AuslegungsheizlastW`), P_V,i = min(P_K,i, P_verf,i); Zone ohne Φ_HL ungedeckelt (P_K = +∞) und gezählt |
| `Vorheizplanung.PlanBilden` | der Plan aus der Analyse mit t_V (Option 1 wie V2, Fenster = min(t_V, längstes Fenster) — dieselbe Zahl wie vorher) |
| Weiche | `Anwendbar` gilt auch für „Berechnet“; der Rückfall „Berechnet → Sollwertrampe“ entfällt samt Schlüssel; AK3 und Heizkreis-Schalter aus bleiben Rückfälle bis V3b |
| Nachweis, Gebäudewerte | je Prüfung Bedarf, „unerreichbar“, Abweichung; Option 1 bestimmt den Bedarf nur an verfehlten Tagen; Gebäude: Bedarf max, Median, Tage unerreichbar, Tage Abweichung, Zonen ohne Φ_HL, Vorausschauen; Geltung Gebäude misst S an der Summe der Zonen an jeder Sprungstunde einer Zone |
| Hinweise | `SIMENG_VORH_BERECHNET`, `_TAGE_BEDARF` („nötig wären bis … h Vorheizen statt … h“), `_UNERREICHBAR`, `_ABWEICHUNG`, `_OHNE_HEIZLAST` (de/en); `_RUECKFALL_AK3` nennt beide Verfahren; `_RUECKFALL_BERECHNET` entfernt |
| `EPOS.Kern.Tests/VorheizBerechnetTests` | 10 Fälle, siehe Nachweise |

## Entscheide der Umsetzung

1. **Vorausschau ohne Lauf-Zustand:** eigenes Modell statt Sichern/Setzen am Zonenlauf — der Lauf, seine Zähler und der Eingang
   bleiben unberührt (Test: Eingang und Lauf bitgleich vor und nach beliebig vielen Vorausschauen).
2. **Zustand außer den Massen:** Sommerlüftungs- und Nachtauskühlregel als eigene Regeln desselben Eingangs, je Vorausschau
   inaktiv begonnen, Vorstunde aus dem Vorlauf; die Stunde h_s ohne Lüftungsregel wie der Nachweis; Nachbarluft auf der
   Vorlaufbahn (2.7). AK1 rechnet über denselben Rand (Übergabe, Heizkurve, P-Regler), P_V als Leistungsgrenze.
3. **Bedarf ab t = 1** (2.1): Ein Sprung, der ohne Fenster ankäme, hat Bedarf 1 h. Übergänge aus „aus“ haben keinen Bedarf.
4. **Option 2:** t_V = max(1, max t_nötig) je Zone; Geltung Gebäude gibt allen Zonen das Maximum, Geltung Zone jeder Zone
   ihres; `Aufheizzeit_Manuell_H` wirkt nur in Option 1. Unerreichbare Sprünge gehen mit min(D, 47) in das Maximum ein
   (Wortlaut 2.6 Schritt 1–2).
5. **Geltung Gebäude erst ab zwei beheizten Zonen** — mit einer Zone ist die Zone das Gebäude (sonst würde ein Einzonenbau
   ohne Φ_HL ungedeckelt). Ohne jede Zone mit Φ_HL bleiben alle ungedeckelt und werden gemeldet.
6. **Abweichung:** Der Lauf verfehlt einen Sprung, dessen Bedarf (Plan bzw. Vorausschau des verfehlten Tags) erreichbar ist und
   ins Fenster des Plans passt.
7. **Option 1 bleibt bitgleich zu V2** (Fenster über dieselbe Kürzung, Deckelsummen nur endlicher Zonenwerte).

## Nachweise

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- `VorheizBerechnetTests` 10/10 und `VorheizVorgabeTests` 16/16 grün: Vorausschau rein (Eingang, Deckelreihe und Lauf
  bitgleich), Bisektion = lineare Suche an jedem Sprung des Probekalenders bei 20 % und 2 % Toleranz (höchstens
  ⌈log2(tMax + 1)⌉ Vorausschauen), „unerreichbar“ bei P_V = ½·Φ_stat und nicht ohne Grenze, Option 2 t_V = Maximum und
  Fenster min(t_V, längstes Fenster) an jedem Sprung, Ankunft überall, Hinweis; Option 1 nennt den Bedarf nur der verfehlten
  Tage; Geltung Gebäude: Anteile nach Φ_HL gegen unabhängig nachgerechnetes Φ_K,max,Geb, t_V,Geb = max, S an der Summe; Zone
  ohne Φ_HL ungedeckelt mit Hinweis; Hinweistexte de-DE (Bedarf, unerreichbar, Abweichung, AK3); Rückfall mit „Berechnet“ =
  Sollwertrampe bitgleich.
- Betroffene Klassen (Filter `Vorheiz`, `Aufheiz`, `Zonen`, `Anlagenkopplung`, `Ak3`, `GebaeudeModell`, `GebaeudeStepper`,
  `Kappungsanteil`, `Stundenrand`, `Zonenmodell`, Ressourcen-, Designer-, Text-, Kodierungs-, Double-, Link- und
  Modulwachen): 1 717 Fälle, 1 716 grün, 1 übersprungen (Bestand), 0 rot.
- Referenzlauf aller 29 Projekte gegen `2026-10-10_R51_FreieKuehlung`: 29 von 29 gerechnet, `GESAMT: PASS` (10 036 768
  Werte), 943 CSV byte-gleich, 0 abweichend — kein Referenzprojekt nutzt die neuen Verfahren.

## Messung an 1051 (Gebäude 10657, Toleranz 20 %, Testdatenbank)

Heizwärme ohne Aufheizen (Vorlauf) 23 676 kWh, mit der Sollwertrampe des Projekts 24 224 kWh (= R51, 24,22 MWh).
Φ_K,max 23,62 kW, P_K = P_V 28,34 kW, S_max,0 24,06 kW in allen Fällen. Monotonie: Bisektion = lineare Suche an allen 148
Sprüngen bei jedem ε, keine Ausnahme.

| Option 2, ε | t_V | t_nötig Werktag (D = 13): Median / Max | nach Wochenende (D = 61) | nach Ferien/Feiertag | unerreichbar | Nächte ohne Absenkung | Tage ohne Ankunft | Spitze | S_max | Mehrwärme gegen Vorlauf | gegen Sollwertrampe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 2 K | 1 h | 1 / 1 | 1 / 1 | 1 / 1 | 0 | 0 | 0 | 28,34 kW | 0,00 kW | +480 kWh (+2,0 %) | −68 kWh (−0,3 %) |
| 1 K | **5 h** | 1 / 4 (110 × 1 h) | 1 / 5 (26 × 1 h) | 1 / 1 | 0 | 0 | 0 (bis 0,82 K) | 28,34 kW | 0,00 kW | +2 308 kWh (+9,7 %) | +1 760 kWh (+7,3 %) |
| 0,5 K | **8 h** | 1 / 6 (103 × 1 h) | 1 / 8 (20 × 1 h) | 1 / 1 | 0 | 1 | 0 (bis 0,04 K) | 28,34 kW | 0,40 kW | +3 270 kWh (+13,8 %) | +2 722 kWh (+11,2 %) |
| 0,1 K | 9 h | — | — | — | 0 | 1 | 0 | 28,34 kW | 0,48 kW | +3 505 kWh (+14,8 %) | +2 957 kWh (+12,2 %) |

| Option 1 | Tage ohne Ankunft | Nächte ohne Absenkung | Mehrwärme gegen Vorlauf | gegen Sollwertrampe |
|---|---|---|---|---|
| t_V = 6 h | 0 | 0 | +2 671 kWh (+11,3 %) | +2 123 kWh (+8,8 %) |
| t_V = 15 h | 0 | 117 | +4 260 kWh (+18,0 %) | +3 712 kWh (+15,3 %) |

**Klärung der Mehrwärme.** Der Entwurf (2.8, Spalte „Mehrverlust ‚fest‘“, ≈ 2,4–2,6 MWh, „+10 %“) rechnet
H_s·(θ_T − θ_Luft)·(t_V − t_nötig): den **Mehrverlust des festen t_V gegenüber dem Vorheizen nach Bedarf** („täglich“), in
Prozent einer Heizwärme von rund 24–25 MWh — nicht gegen einen Lauf ohne Vorheizen. V2 maß gegen den Vorlauf **ohne jedes
Vorheizen**; diese Zahl enthält zusätzlich die Aufheizwärme selbst (das Laden der Massen nach der Absenkung) und die
548 kWh der Bestandsrampe. Gemessenes Gegenstück zum Entwurf: t_V = 15 h fest gegen Option 2 bei ε 1 K (Bedarf im Median
1 h) = 27 935 − 25 984 = **1 952 kWh = 8,1 % von 24,22 MWh** — dieselbe Größenordnung wie die Abschätzung nach oben (+10 %).
Gegen die Sollwertrampe kostet t_V = 15 h +15,3 %, gegen den Vorlauf +18,0 %.

**Klärung der Vorheizzeit.** Gemessen t_nötig werktags ≤ 4 h, nach dem Wochenende ≤ 5 h (ε 1 K) bzw. ≤ 6 / 8 h (ε 0,5 K),
t_V = 5 h (ε 1 K), 8 h (ε 0,5 K), 9 h (ε 0,1 K); der Entwurf schätzte 9 / 15 h. Das ε erklärt den kleineren Teil (5 → 9 h von
1 K auf 0,1 K). Der größere Teil liegt im Schätzer erster Ordnung: Er lädt die ganze Masse C_w·ΔT_m vor der Ankunft (obere
Schranke; V2 maß für das Plateau 0,40–0,51·t₁) und nimmt die kälteste Außenluft ohne Sonne und Gewinne. Im 2K-Modell kommt
die Luft an, sobald die schnelle Mode geladen ist; der Rest des Massendefizits erscheint nach h_s als Last unter dem Deckel
(P_K bzw. Floor), nicht als Luftdefizit — die größte Unterschreitung im Block bleibt 0,82 K < ε. Der Befund aus V2 (mit
t_V = 6 h kein Tag ohne Ankunft) folgt daraus: t_V = 6 h liegt über dem Bedarf jedes Sprungs bei ε = 1 K.

## Rechenzeit (Release, Linux, Projektlauf samt Datenbanklesen)

| Fall | Vorausschauen | Modellstunden der Vorausschau | Rechenzeit | ohne Aufheizen |
|---|---|---|---|---|
| 1051, Option 2, ε 1 K (1 Zone, 148 Sprünge) | 652 | 3 122 (≈ 0,36 Zonenjahre) | 303 ms | 436 ms (erster Lauf, kalt) |
| 1052, Option 2 (3 Zonen) | 2 584 | — | 1 436 ms | 504 ms |
| 1054, Option 2 (3 Zonen, AK1) | 2 330 | — | 823 ms | 332 ms |

Je Zone: Vorlauf 1 Zonenjahr, Vorausschauen unter 0,5 Zonenjahr (die Bisektion prüft meist kurze t), Lauf 1 Zonenjahr —
deutlich unter der Schranke von zehn Zonenjahren je Zone (2.4).

## Befund 1054 (AK1, Zone mit eigener Übergabe)

Option 2 ergibt t_V = 47 h: 182 Tage (ε 1 K) bzw. 93 Tage (ε 2 K) sind „unerreichbar“, die Unterschreitung im Block erreicht
3,0 K, 567 Nächte (über drei Zonen) bleiben ohne Absenkung. Die Luft kommt dort auch ohne Absenkung nicht innerhalb ε an — die
Übergabe am Vorlauf der Heizkurve (Konvektor 70/50 °C, Proportionalband 2 K) begrenzt, nicht P_V; die Annahme aus 2.7, ε decke
den Regelabstand, trägt hier nicht. Nach 2.6 gehen unerreichbare Sprünge mit min(D, 47) in t_V ein; das erzwingt Durchheizen
ohne Nutzen. Offen zur Entscheidung (siehe unten).

## Offen

1. **V3b:** AK3-Profilweg (Vorlauf, Vorausschau und Plan über `AufheizplanSetzen` im Kreis; heute Rückfall), Heizkreis-Schalter
   aus (Rückfall), Auskunftswege `Vdi6007Rechenweg.ZonenBauen` und die Aufheizauskunft in `SimulationWaermebedarf` (rufen die
   neuen Verfahren noch nicht).
2. **Entscheid zu 1054:** Sollen unerreichbare Sprünge in das Maximum von Option 2 eingehen (Wortlaut 2.6), oder nur Sprünge,
   die mit einem Fenster ankommen — und soll die Ankunft eines gekoppelten Gebäudes gegen die Luft gemessen werden, die die
   Übergabe am warmen Bau hält (Regelabstand), statt gegen θ_T − ε?
3. **V4:** Spalten für `Vorheizvorgabe` und die Ergebnisgrößen (t_V als Bemessungswert an Gebäude und Zone, Bedarf max und
   Median, Tage unerreichbar, Abweichungen, Zonen ohne Φ_HL, Pläne je Zone); `KonfigurationCtrl.AufheizvorgabeLesen` füllt
   `Vorheizen`.
4. Die Messung an 1051 bleibt als `VorheizBerechnetTests.Messung_an_Projekt_1051` (nur mit `EPOS_MESSUNG=1`); eine Wache mit
   harten Werten kommt mit V7.
