# SK-2 — Bereitschaftsverlust nur in betriebsbereiten Stunden, Basis R23 (#568)

Stand: 26.09.2026 · Zweig `ios_migration_september` · Commits `576c98689` Betriebsbereitschaft,
`75d0d634e` Elektrokessel und Laufprotokoll, `941dfca27` Kessel-Reiter, `bd820bdb0` Referenzbasis
R23 einfrieren/R22 archivieren, `32261a850` Papiere; Merge `4f6f5c8f5`. Kein Schemaschritt.
Anwenderentscheid 26.09.2026 zum Anlass. Statuszeile und offene Punkte: „#568“ und „Nach #568“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).

## 1 Anlass

SK-1 (#559, [`SK1_Kessel_Bereitschaft_kW_Protokoll.md`](SK1_Kessel_Bereitschaft_kW_Protokoll.md))
hatte den Bereitschaftsverlust von einem Prozentwert der Nennleistung auf eine Leistung in kW
umgestellt, aber weiter mit **jeder** Stillstandsstunde des Jahres gerechnet. Die Vorgabe
`Tab_Einstellungen.Kessel_Betriebsbereitschaft` [h/a] blieb ungelesen. Ein Kessel mit wenigen
Laufstunden (etwa 1047, solar entlastet) trug den Bereitschaftsverlust über nahezu alle 8 760
Stunden des Jahres, auch in Monaten ohne jeden Wärmebedarf.

## 2 Behebung

- **`SimulationSPK.IstBetriebsbereit`**: Ein stillstehender Kessel ist betriebsbereit, wenn der
  Tag ein Heiztag ist (Raumwärmebedarf des Projekts vor der Kaskade > 0) oder er in den 24
  Stunden davor gelaufen ist (Nachlauf); nur dann trägt die Stillstandsstunde den
  Bereitschaftsverlust. Eine gepflegte Vorgabe `Kessel_Betriebsbereitschaft` deckelt Lauf- plus
  Bereitschaftsstunden und meldet den Überhang als Hinweis.
- **Laufprotokoll**: je Kessel Laufstunden, Starts, Bereitschaftsstunden und Bereitschaftsverlust
  (kWh); die Basis führt sie neu als `Kessel[i].Laufstunden`, `.Starts`, `.Bereitschaftsstunden`,
  `.BereitschaftKwh` (60 Skalare mehr in `aggregate.csv`).
- **Elektrokessel ohne Doppelemission**: Sein Strom steht im Reststrombedarf und damit im
  Netzbezug, den Emissionsbilanz und Kosten schon bewerten; `Em.Kessel.*` zählte ihn mit dem
  Stromfaktor ein zweites Mal. Jetzt ist `Em.Kessel.*` beim Elektrokessel 0
  (`EPOS.Kern.Tests/KesselBereitschaftTests`).
- **Kessel-Reiter** (`941dfca27`): Tafel Wärme mit „Restwärmebedarf nach Kessel“ und „davon aus
  Puffer (andere Erzeuger)“; Rasterzeile „Betrieb“ (Betriebsstunden, Starts, Bereitschaftsstunden,
  Verlust kWh/a); Kesselbild als Flächenstapel des Stufeneingangs
  (`SimulationErgebnisCtrl.KesselbildReihen`); Beschriftung „Maximale Brennstoffleistung Gas
  (Hu)“ mit Kurztext „max. Wärmelast“; Hinweis, wenn der Wirkungsgrad noch der Platzhalter 1,0 ist.

## 3 Gegenprobe gegen R22 (15 Projekte)

A/B: **0/15 PASS** allein wegen der 60 neuen Skalare; **445/460 CSV byte-gleich**, alle
Zeitreihen unverändert — verschieden sind nur die fünfzehn `aggregate.csv`.

| Projekt | Bereitschaftsstunden R22 → R23 | Kessel-Verbrauch [MWh] | Jahresnutzungsgrad [%] | Kessel-CO₂ [t] |
|---|---|---|---|---|
| 1007, 1046 (Gas, 0,05 kW) | 6 963 → 5 931 | 10,68 → 10,63 | 84,74 → 85,16 | 2,563 → 2,550 |
| 1008 (Gas, 0,05 kW) | 6 292 → 5 260 | 23,85 → 23,80 | 86,44 → 86,63 | 5,723 → 5,711 |
| 1017 (Elektrokessel) | 5 742 → 4 534 | — | 97,92 → 98,24 | 11,51 → 0 |
| 1024 (Elektrokessel, 0 kW) | 3 839 → 2 712 | — | — | 28,40 → 0 |
| 1047 (Elektrokessel) | 8 694 → 7 401 | — | 66,61 → 70,05 | 0,84 → 0 |

Mit dem CO₂ wandern SO₂, NOx und Staub des Kessels (`Em.Kessel.*`). Bei 1023 (0,03 kW) bleiben die
Bereitschaftsstunden 3 702, bei 1018, 1030, 1039, 1040, 1041, 1042, 1045 und 1049 ist die
Bereitschaftsleistung 0 — dort wandert keine Zahl außer den neuen Einträgen. Die
Netzbezugs-Emissionen stehen in keiner Referenz-CSV; die Wirtschaftlichkeit führte den
Elektrokessel schon vorher ohne Brennstoffverbrauch. Kein Fehlschlag, keine Ablehnung: 15/15
Projekte gerechnet; der Einfrierlauf ist mit dem A/B-Lauf 460/460 CSV byte-gleich (Determinismus).

## 4 Neue Basis R23

`2026-09-26_R23_KesselBereitschaft` — fünfzehn Projekte (1007, 1008, 1017, 1018, 1023, 1024, 1030,
1039, 1040, 1041, 1042, 1045, 1046, 1047, 1049), 460 CSV, 2 685 Skalare, gerechnet gegen
`Kenndaten_Test.sqlite` (Schemastand 150, LFS-SHA-256 `09b6c52350ede9b0bc6e1081452d0ebce060fcd7adad4ed7bd0f6205635e7111`).
R22 (`2026-09-26_R22_Solarthermie`, 460 CSV, 2 625 Skalare) archiviert unter
[`Dokumentation/ueberholt/Referenzbasen/2026-09-26_R22_Solarthermie/`](../../Referenzbasen/2026-09-26_R22_Solarthermie/protokoll.txt).
Herleitung, Aufbau und Wegweiser: [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md).

```bash
dotnet build EPOS.Referenzlauf/EPOS.Referenzlauf.csproj -c Release
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf \
  --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
  --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047,1049 \
  --ziel Referenzlaeufe/2026-09-26_R23_KesselBereitschaft
```

## 5 Papiere

Konzept Simulationsablauf § 12 neu (Betriebsbereitschaft, Bereitschaftsverlust, Kesselbild).

## 6 Offene Punkte

Siehe „Nach #568“ in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md):

1. Der Bereitschaftsverlust des Elektrokessels wirkt bisher nur im Nutzungsgrad, ohne eigene
   Emission — Anwenderentscheid, ob der Netzbezug ihn zusätzlich ausweisen soll.
2. „Maximale Brennstoffleistung Gas (Hu)“ zeigt bei mehreren Gaskesseln die Summe der
   Einzelmaxima — Anwenderentscheid, ob das der gewollten Kennzahl entspricht.
3. Der CSV-Export des Kessel-Reiters führt weiter die alten Spalten
   `Kesselleistung_stuendlich`/`Restwaerme`, nicht die neuen Größen.
4. Das Heiztag-Kriterium der Betriebsbereitschaft ist bei den VDI-6007-Gebäuden fast ganzjährig
   erfüllt (Raumwärmebedarf > 0 an fast jedem Tag) — zu prüfen, ob das die gewollte Wirkung ist.
