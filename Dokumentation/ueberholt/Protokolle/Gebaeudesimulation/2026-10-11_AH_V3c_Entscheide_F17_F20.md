# Protokoll AH-V3c — Entscheide F17–F20: Quantil, Ankunftsbezug, unerreichbare Sprünge, Sperrzeit

**11.10.2026 · Sitzung Gebäudesimulation · Welle V3, dritter Teil, der Aufheizoptimierung Fassung 2.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitte 2.6, 2.7, 5.3;
aufgesetzt auf [AH-V3a](2026-10-10_AH_V3a_Vorausschau_Option2.md) und
[AH-V3b](2026-10-11_AH_V3b_AK3_Auskunft_Ankunftsbezug.md). Nur Rechenkern und Papiere: kein Schema, keine Oberfläche.

**Anwenderentscheid (E125, 11.10.2026), Wortlaut:** „F17-F20: Empfehlung“ — F17 (b), F18 (b), F19 (b), F20 (a).

## Was gebaut ist

- **F17 (b) — Ankunftsbezug:** `Vorheizvorgabe.ANKUNFTSBEZUG_VORGABE` = `Vorheizankunftsbezug.Uebergabe`; an einer Zone mit
  Heizkreis messen Vorausschau und Nachweis gegen min(θ_T, θ_stat) − ε. Die Wahl bleibt `internal` (Init-Eigenschaft
  `Vorheizvorgabe.Ankunftsbezug`): Tests und Messung rechnen (a) als Gegenprobe.
- **F18 (b) — unerreichbare Sprünge:** Sie gehen nicht in t_V ein, werden gezählt (`Vorheizplan.SpruengeUnerreichbar`) und wie
  bisher gemeldet. Die Wahl `UnerreichbareImMaximum` samt `UNERREICHBARE_IM_MAXIMUM_VORGABE` ist zurückgebaut — nach dem
  Quantil hätte „im Maximum“ keinen Gegenstand mehr. Auch der Median und t_nötig,max laufen nur über die erreichbaren Sprünge.
- **F19 (b) — Quantil:** `Vorheizplanung.Quantil(bedarfe, prozent)` liefert das kleinste t der Liste, das mindestens p % der Werte
  deckt: Rang ⌈p·n/100⌉ (ganzzahlig `(p·n + 99)/100`) der aufsteigend sortierten Bedarfe, ohne Interpolation; unter 20 Sprüngen
  ist das bei 95 % das Maximum, ohne Sprung 0 (t_V dann 1 h). Kern-Konstante `Vorheizplanung.BEDARF_QUANTIL_PROZENT` = 95.
  t_V der Zone = max(1, `Vorheizanalyse.BedarfQuantilH`), mit Geltung Gebäude das Maximum der Zonenquantile (unverändert).
  Neue Größen: `Vorheizplan.BedarfQuantilH`, `SpruengeUeberVorheizzeit`; `Vorheizplan.BedarfMaxH` ist t_nötig,max der
  erreichbaren Sprünge; im Nachweis `TageUeberVorheizzeit` und `UnterschreitungUeberVorheizzeitK`; in den Gebäudewerten
  dieselben als Summe bzw. Vereinigung. Der Hinweis `SIMENG_VORH_BERECHNET` nennt t_V, Anteil, Median, t_nötig,max und die
  Zahl der Sprünge darüber; mit Option 2 nennt `SIMENG_VORH_TAGE_BEDARF` die Tage über t_V samt ihrer Unterschreitung
  („nötig wären bis t_nötig,max h statt t_V h“). Option 1 bleibt im Nachweis, wie sie war.
- **F20 (a) — Sperrzeit:** `Vorheizplanung.FensterstundeGesperrt` liest dieselbe Schranke, die Vorlauf, Vorausschau und Lauf
  tragen (`GebaeudeModellEingang.Verfuegbarkeit`, nur mit wirksamer Kopplung): gesperrt ist eine Fensterstunde mit Grund
  Sperrzeit, Zeitprogramm oder Speicher leer und Schranke ≤ `Rechenrand.ABSOLUT`. Eine Teilsperre mit verfügbarer Wärme zählt
  nicht. Der Nachweis zählt Tage, gesperrte Fensterstunden und die Uhrzeiten (gesperrt und frei); der Hinweis
  `SIMENG_VORH_SPERRZEIT` nennt sie, `SIMENG_VORH_SPERRZEIT_GANZ`, wenn keine Fensterstunde frei ist. Plan, Fenster und Lauf
  bleiben bitgleich (Test).
- Ressourcen beider Sprachen (`SIMENG_VORH_BERECHNET` neu gefasst, `SIMENG_VORH_SPERRZEIT`, `SIMENG_VORH_SPERRZEIT_GANZ`),
  `Resource.Designer.cs` mit `designer_neu.py schreiben` erzeugt.

## Nachweise

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Neue Klasse `VorheizEntscheideTests` 8/8: Quantil an konstruierten Profilen (1…100 → 95, 20 → 19, unter 20 das Maximum,
  ein Wert, leer, Gleichstand 38 × 4 h + 2 × 30 h → 4 h, drei Ausreißer → 30 h), Analyse bemisst nur erreichbare Sprünge,
  Hinweis der Tage über t_V, Uhrzeiten (auch über Mitternacht), Sperrzeit an synthetischer Schranke 0 von 0 bis 6 Uhr
  (gemeldet, Plan und Deckelreihe bitgleich), Fenster ganz in der Sperre, Teilsperre und Zone ohne Heizkreis ohne Meldung,
  Option 1 unter (a) und (b) bitgleich (Sollwertreihe, Heizlast, Raumluft).
- `VorheizAnkunftsbezugTests` und `VorheizBerechnetTests` auf die Entscheide umgestellt (Vorgaben F17–F19, Quantil und
  Abdeckung, Tage ohne Ankunft ⊆ Tage über t_V ∪ unerreichbar, Geltung Gebäude = Maximum der Zonenquantile).
- Betroffene Klassen (Filter `Vorheiz`, `Aufheiz`, `Zonen`, `Anlagenkopplung`, `Ak3`, `GebaeudeModell`, `GebaeudeStepper`,
  `Stundenrand`, `SimulationWaermebedarf`, Ressourcen-, Lokalisierungs-, Textschlüssel-, Kodierungs-, Double-, Link- und
  Ordnungswachen): 1 151 Fälle, 1 150 grün, 1 übersprungen (Bestand), 0 rot; nach dem Text für das Fenster ganz in der Sperre
  `Vorheiz` samt Ressourcen-, Kodierungs-, Link- und Ordnungswachen 167/167.
- Referenzlauf aller 29 Projekte gegen `2026-10-10_R51_FreieKuehlung`: 29 von 29, `GESAMT: PASS` (10 036 768 Werte), 943 CSV
  byte-gleich, 0 abweichend — kein Referenzprojekt rechnet Option 1 oder 2.

## Messung

Option 2 mit den Vorgaben (Bezug (b), Quantil 95 %, unerreichbare nur gezählt), ε 1 K, Geltung Gebäude, die übrigen Felder der
Projektvorgabe (eingeschaltet), ganzer Projektlauf über die Testnaht `SimulationWaermebedarf.AufheizvorgabeTestnaht` (1058:
Nachweis am Kreis); Mehrwärme gegen den Lauf mit der gespeicherten Vorgabe = Basis R51. Messklasse
`VorheizAnkunftsbezugTests.Messung_Entscheide_F17_bis_F20` (nur mit `EPOS_MESSUNG=1`).

| Projekt | t_V (Q95) [h] | t_nötig,max [h] | Median [h] | Sprünge / Tage über t_V | Tage unerreichbar | Tage ohne Ankunft | Unterschreitung max [K] | Nächte ohne Absenkung | Spitze [kW] | Mehrwärme gegen Basis | Sperrzeit |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1054 (AK1, 3 Zonen) | 12 | 18 | 1 | 12 / 12 | 1 | 10 | 3,97 | 365 | 23,8 | +4 637 kWh (+9,7 %) | – |
| 1047 (AK1, Einzone) | 1 | 2 | 1 | 13 / 13 | 0 | 10 | 2,39 | 0 | 42,0 | +484 kWh (+0,7 %) | – |
| 1058 (AK3) | 1 | 2 | 1 | 13 / 13 | 0 | 1 | 2,31 | 0 | 42,0 | +547 kWh (+0,8 %) | – |
| 1056 (AK1, Fahrplan) | 1 | 8 | 1 | 8 / 8 | 189 | 197 | 6,52 | 0 | 47,0 | ±0 kWh | 359 Tage, 359 h, 5–6 Uhr, keine freie Stunde |
| 1051 (ohne Heizkreis) | 2 | 5 | 1 | 5 / 5 | 0 | 5 | 1,54 | 0 | 28,3 | +450 kWh (+1,9 %) | – |

Rechenzeit je Projektlauf 0,4–2,5 s. Lesart gegen die Messung V3b (Bezug (b), Maximum):

- **1054:** Das Quantil setzt t_V von 18 auf 12 h; die Werktagsabsenkung (D = 13 h) bleibt trotzdem aus — 12 h decken mit dem
  vorigen Sprung gekürzt die ganze Nacht (365 Nächte über drei Zonen statt 565), Mehrwärme +9,7 % statt +10,5 %. Zwölf Sprünge
  über t_V (bis 18 h) werden gemeldet, 10 Tage ohne Ankunft, Unterschreitung bis 3,97 K.
- **1047/1058:** t_V 1 h statt 2 h, 13 Sprünge mit 2 h Bedarf werden gemeldet; Mehrwärme +0,7/+0,8 % statt +1,4/+1,6 %. Die
  Unterschreitung gegen θ_T (2,3–2,4 K) ist die der Übergabe.
- **1056:** Ohne die unerreichbaren Sprünge (189 Tage, Sperre und `Vorlauf_Max`) sinkt t_V von 8 auf 1 h; das Fenster 5–6 Uhr
  liegt an jedem Tag mit Sprung ganz in der Nachtsperre (0–6 Uhr) — der neue Hinweis nennt das, die Mehrwärme ist 0. Acht
  erreichbare Sprünge bräuchten bis 8 h.
- **1051:** ohne Heizkreis wirkt nur das Quantil: t_V 2 h statt 5 h, Mehrwärme +1,9 % statt +7,3 %, fünf Sprünge darüber.

## Offen (für V4)

1. **Schema:** die Ergebnisgrößen t_V, t_nötig,max, Median, Sprünge und Tage über t_V, Tage unerreichbar, Tage und Stunden in der
   Sperrzeit je Gebäude und Zone; die Wahl des Ankunftsbezugs bleibt intern (keine Spalte), der Quantilanteil Kern-Konstante.
2. **Testnaht** `AufheizvorgabeTestnaht` bleibt bis V4 der Weg der Messung.
3. **1056:** Mit der Nachtsperre bleibt Option 2 wirkungslos (Fenster in der Sperre); eine Schnellaufheizung oder ein
   verschobenes Fenster sind nicht entschieden und nicht gebaut.
4. **Text „unerreichbar“:** `SIMENG_VORH_UNERREICHBAR` nennt weiter die Nächte ohne Absenkung; sie hängen mit F18 nicht mehr an
   den unerreichbaren Sprüngen — bei V5 (Oberfläche, Texte) neu fassen.
