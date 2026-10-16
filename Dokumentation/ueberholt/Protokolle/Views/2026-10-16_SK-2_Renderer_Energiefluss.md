# SK-2 — Renderer des Energieflussbilds (Sankey) im Kern

Stand: 16.10.2026, Zweig `sk2` (von `origin/ios_migration_september` samt Zweig `sk`). Grundlage:
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../../aktuell/Dialoge/2026-10-16_Konzept_Energieflussbild_Sankey.md)
Abschnitte 2.2, 2.3, 2.5, 3, 6 und 8 (Zeile SK-2); Anwenderentscheide 16.10.2026: ein Bild mit Strom, Wärme und
Kälte, SK-Q1 bis SK-Q11 nach Empfehlung, SK-Q12 (Bildschirm: Werte nur im Zeigertext, Knoten nur mit Namen;
Bericht: Knoten mit Jahressumme, Bänder ohne Zahlen).

## 1. Ergebnis

| Teil | Inhalt |
|---|---|
| Eingabetyp (Vertrag für SK-3/SK-4) | `EPOS.Kern/Allgemein/Bericht/EnergieflussBildDaten.cs`: `EnergieflussBildDaten` (`Knoten`, `Baender`, `Spaltentitel`), `EnergieflussBildKnoten` (`Kennung`, `Spalte`, `Name`, `Kurzname`, `Mwh`, `Rolle`, `Art`, `Zeigertext`), `EnergieflussBildBand` (`Von`, `Nach`, `Mwh`, `Rolle`, `Art`, `Zeigertext`), Aufzählungen `EnergieflussKnotenart` (Quelle, Wandler, Bus, Speicher, Senke, Verlust) und `EnergieflussBandart` (Normal, Verlust, NichtZugeordnet). Ohne Bezug zum Flussmodell `Energiefluss` (SK-1). |
| Renderer | `ChartRenderer.Energiefluss.cs` (Teildatei): `EnergieflussModell(daten, Bildmass? mass = null, bool knotenwerte = false)` → `Zeichenmodell`, `EnergieflussBild(…)` → PNG. Maße `ENERGIEFLUSS_BREIT` 1180 × 560, `ENERGIEFLUSS_SCHMAL` 680 × 640, `ENERGIEFLUSS_BERICHT` 1240 × 620; Regeln „schmal“ unter 900 px Breite. |
| Farbrollen | `BRENNSTOFF` #8B6F47, `UMWELTWAERME` #7FB8A4, `KAELTE` #2A7FA6, `VERLUST` #B8B8B8 (Gruppe „Erzeuger und Bedarf“), `KNOTEN` #4A5769 (Gruppe „Allgemein“) in `Farbpalette`, `Diagrammfarben` und den Ressourcen `DGF_ROLLE_*` beider Sprachen. Der Einstellungsdialog liest die Gruppen und braucht keine Änderung. 60 Rollen. |
| ChartProben | `Program.Energiefluss.cs`: vier Maßproben, vier Gegenproben, eine SVG-Probe; 269 Bilder, 0 Verstöße; 12 neue Zeilen an `Messlatte_2026-10-16.sha256` angehängt, die 226 bisherigen unverändert. |
| Tests | `EnergieflussModellTests` (14 Fälle), `ChartRendererRollenTests.Energiefluss_traegt_die_Rolle_jedes_Bandes_und_Knotens`, `FarbpaletteTests` auf 60 Rollen. |

## 2. Zeichenregeln

- **Layout:** Spalten 0 … 6 gleichmäßig zwischen Rändern von 150 px (schmal 100 px); Knotenbreite 14 px; Lücke
  zwischen den Knoten einer Spalte 30 px (schmal 26 px) — wie im Mockup, weil die Beschriftung der inneren Knoten
  über dem Knoten Platz braucht (Konzept 3.1 nennt 10 px). Jede Spalte vertikal zentriert, Knoten in
  Eingabereihenfolge. Maßstab aus der höchsten Spaltensumme über die nutzbare Höhe; verlängert die Mindestbandbreite
  eine Spalte über die Höhe, schrumpft der Maßstab in Schritten von 3 %, bis jede Spalte passt.
- **Bänder:** geschlossener Pfad mit 24 Stützstellen je Kante, Oberkante y0 + (y1 − y0)·(3t² − 2t³), Füllung in der
  Rolle mit Deckung 140 (0,55); „nicht zugeordnet“ zusätzlich mit gestricheltem Rand 4/3 px. Mindestbreite 2 px.
  Ausgehende Bänder liegen nach der Lage ihres Ziels (Verluste zuunterst), eingehende nach der ihrer Quelle.
- **Verlustknoten:** in der Lücke rechts neben dem Erzeuger (Spalte + ½), 8 px unter dessen Fuß; mehrere in einer
  Lücke stapeln sich; reicht der Platz nicht, rückt der Knoten an den unteren Rand.
- **Beschriftung (SK-Q12):** Quellen links, Senken rechts, innere Knoten über dem Knoten (unter 12 px Höhe keine,
  Busse immer), Verlustknoten darunter. Bildschirm nur Namen (schmal Kurznamen), keine Zahl im Bild; Bericht
  (`knotenwerte`) an Quellen und Senken die Zeile „x MWh/a“, innen „Name · x“, am Verlust „Verlust x“. Bänder tragen
  nie eine Zahl. Eine Beschriftung, die eine schon gesetzte überdecken würde, entfällt (größere Knoten zuerst).
- **Marken:** Band `fluss:<von>:<nach>`, Knoten `knoten:<Kennung>` (Rechteck samt Beschriftung), Spaltentitel
  `spaltentitel`; Wert ist der Zeigertext der Gabe, sonst „Von → Nach: x MWh/a“ bzw. „Name: x MWh/a“.
- **Wegfall:** Bänder mit nicht endlichem oder nicht positivem Wert, unbekanntem Knoten oder Schleife, Bänder aus
  einem Verlustknoten und Knoten ohne Band. Ohne Band ein leeres Bild.

## 3. Abnahme

- `dotnet build EPOS.Kern.Tests -c Release`: 0 Fehler.
- `dotnet test … --filter "Energiefluss|ChartRenderer|Zeichenmodell|Farb|Wache"`: 812 bestanden, 0 fehlgeschlagen.
- `dotnet run --project Proben/ChartProben -c Release`: 269 Bilder, 0 Verstöße; Hashliste 238 Zeilen, jede der
  226 Zeilen der Messlatte unverändert darin.
- `EPOS.UI.Tests` gefiltert auf Farbe, Einstellungsdialog und Stilblatt: 169 bestanden, 0 fehlgeschlagen (die
  Rollenzahl im Dialog folgt `Diagrammfarben.Rollen`, keine Datei in `EPOS.UI` geändert).
- `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests` nach dem Protokoll: 24 bestanden.

## 4. Für SK-3 und SK-4

- Die Abbildung des Flussmodells (SK-1, seit SK-12 `EnergieflussBilanz`) auf `EnergieflussBildDaten` — Spalten laut
  Konzept 2.2, Rollen laut 3.4, Kurznamen aus den Ressourcen, Verlustknoten je Erzeuger bzw. Speicher mit
  `Art = Verlust` und der Spalte des Erzeugers, Spaltentitel in der Oberflächensprache — hat SK-12 als
  `EnergieflussAbbildung.Bild` in den Kern gelegt (Protokoll
  [`2026-10-16_SK-12_Abbildung_Energiefluss.md`](2026-10-16_SK-12_Abbildung_Energiefluss.md)). SK-3 ruft sie und
  zeichnet zwei Modelle (breit, schmal), `knotenwerte = false`.
- SK-4 ruft `EnergieflussModell(daten, ChartRenderer.ENERGIEFLUSS_BERICHT, knotenwerte: true)`.
