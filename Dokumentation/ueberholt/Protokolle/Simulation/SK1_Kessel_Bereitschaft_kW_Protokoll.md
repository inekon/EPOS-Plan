# SK-1 — Bereitschaftsverlust des Heizkessels als Leistung in kW (#559)

Stand: 26.09.2026 · Zweig `ios_migration_september` · Commit `2b4be6aa9`, Merge `b9eb67755`;
Neueinfrierung gemeinsam mit #560 (Basis `2026-09-26_R22_Solarthermie`, Commit `ce976fac5`,
Merge `206d42563`). Kein Schemaschritt. Anwenderentscheid 26.09.2026 „bestätigt: #559“ (Einheit kW).
Statuszeile und offene Punkte: „#559“ und „Nach #559“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).

## 1 Befund am Projekt 1067

Anlass war die Ertragsanalyse der Solarthermie (#557, Kapitel 10 in
[`SA1_Waermeautarkie_Solarthermie_Protokoll.md`](SA1_Waermeautarkie_Solarthermie_Protokoll.md)):
Der Gasverbrauch blieb bei rund 66–67 MWh, obwohl die Kesselwärme von 55,6 auf 51,5 MWh sank; der
Jahresnutzungsgrad fiel von 0,84 auf 0,78.

| Größe (Projekt 1067) | vor #559 | nach #559 |
|---|---:|---:|
| Gasverbrauch des Kessels | 66,05 MWh | 63,26 MWh |
| Jahresnutzungsgrad des Kessels | 84,2 % | 87,9 % |
| Wärmebedarf ungedeckt nach der Kaskade | 5,45 MWh | 5,45 MWh (jetzt als Warnung gemeldet) |
| Spitzenlast gegen Kesselleistung | 47,6 kW gegen 22 kW | unverändert |

Der Restwärmebedarf des Kessels ist Stufeneingang minus Kesselanteil; er enthält den Solaranteil,
der aus dem Puffer kommt — das ist definitionsgemäß so und kein Fehler. Die 5,45 MWh sind echt
ungedeckt: Die Lastspitze übersteigt die Kesselleistung.

## 2 Ursache

`Tab_Heizkessel.Betriebsbereitschaftverlust` ist eine **Leistung in kW**: Der Import liest sie aus
VDI 3805 Blatt 3, Satz 700, Spalte 28 und weist sie in kW aus; Herstellerrohdaten nennen
0,116–0,210 kW, der Katalog 0,03–0,16 kW, je Baureihe über alle Leistungsgrößen gleich.

Der Lauf las sie als Anteil der Nennleistung (Stand vor `2b4be6aa9`):

- `EPOS.Kern/Allgemein/Simulation/SimulationSPK.cs:242–243` — Übernahme des Katalogwerts, Werte
  über 1 wurden durch 100 geteilt („Prozentwert“);
- `EPOS.Kern/Allgemein/Simulation/SimulationSPK.cs:1295` — Brennstoffeinsatz einer
  Stillstandsstunde = Wert × `Kessel_Leistung_Spk[i]`.

Beispiel: 0,075 kW eines 22-kW-Kessels wurden 1,65 kW je Stillstandsstunde, das Zweiundzwanzigfache.
Am stärksten betroffen sind Kessel mit vielen Stillstandsstunden (Referenz 1007: Nutzungsgrad
50,2 %, 1047: 6,7 %).

## 3 Behebung

- `SimulationSPK.BereitschaftsleistungKw` — der Katalogwert ist die Bereitschaftsleistung in kW,
  ohne Multiplikation und ohne Prozentdeutung; negativ oder nicht gesetzt heißt null. Eine
  Stillstandsstunde verbraucht diesen Wert mal eine Stunde.
- Hinweis `SIMENG_KESSEL_BEREITSCHAFT_HOCH`, wenn der Wert 2 % der Nennleistung übersteigt
  (`BEREITSCHAFT_PLAUSIBEL_ANTEIL`) — gerechnet wird mit ihm, der Hinweis macht einen als Prozent
  gepflegten Eintrag sichtbar.
- Warnung `SIMENG_WAERME_UNTERDECKUNG` (`SimulationControl.WaermeUnterdeckungMelden`), wenn nach der
  ganzen Erzeugerkaskade mindestens 0,1 % des Wärmebedarfs ungedeckt bleiben.
- Einheit kW im Katalogdialog des Heizkessels, in `ParameterVerwendung`, im Erklärtext des
  Assistenten und in der Wiki-Quelle Gerätekataloge.
- Test `EPOS.Kern.Tests/KesselBereitschaftTests` (vorher rot: 1007 mit 18,02 statt 10,68 MWh).

## 4 Gegenprobe gegen R21

Die vierzehn Projekte der Basis R21 mit dem neuen Stand: alle Zeitreihen byte-gleich, 8 von 14
PASS; Abweichungen allein in `aggregate.csv` der Kesselprojekte (SO₂, NOx und Staub proportional
zum CO₂):

| Projekt | Brennstoffverbrauch | Nutzungsgrad Kessel | CO₂ |
|---|---|---|---|
| 1007, 1046 | 18,02 → 10,68 MWh | 50,20 → 84,74 % | 4,33 → 2,56 t |
| 1008 | 30,49 → 23,85 MWh | 67,62 → 86,44 % | 7,32 → 5,72 t |
| 1023 | 93,48 → 91,45 MWh | 85,40 → 87,29 % | 22,44 → 21,95 t |
| 1017 (Elektrokessel) | — | 68,47 → 97,92 % | 16,46 → 11,51 t |
| 1047 | — | 6,71 → 66,61 % | 8,33 → 0,84 t |

Neu eingefroren wurde gemeinsam mit dem Referenzprojekt 1049 (#560) als Basis
`2026-09-26_R22_Solarthermie`; Herleitung in
[`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md).

## 5 Nebenbefunde (offen)

1. `Vorgabe_Betriebsbereitschaft` [h/a] wird im Lauf nie gelesen.
2. Die Emissionen eines Elektrokessels werden aus `Kessel_Verbrauch` gerechnet — mögliche
   Doppelzählung mit dem Netzbezug.
3. Der Kessel-Reiter könnte Restwärme der Stufe, davon aus dem Puffer, Betriebsstunden, Starts und
   Bereitschaftsverlust zeigen (etwa ½ Tag).
4. Keine Teillast- und Brennwertkennlinie: der Wirkungsgrad ist fest (0,881).
