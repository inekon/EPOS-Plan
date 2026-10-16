# SK-12 — Zusammenführung von Flussmodell und Renderer, Abbildung Bilanz → Bild

Welle SK, Schritt SK-12, 16.10.2026, Zweig `sk12` (von `origin/ios_migration_september`; `sk1` und `sk2` mit `--no-ff`
gemergt, beide ohne Konflikt). Konzept:
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../../aktuell/Dialoge/2026-10-16_Konzept_Energieflussbild_Sankey.md)
(Abschnitte 2, 3, 4.3, 5, 8, 10; SK-Q12). Vorgänger:
[`2026-10-16_SK-1_Flussmodell.md`](2026-10-16_SK-1_Flussmodell.md),
[`2026-10-16_SK-2_Renderer_Energiefluss.md`](2026-10-16_SK-2_Renderer_Energiefluss.md). Ergebnis ist die Grundlage,
auf der SK-3 (Oberfläche) und SK-4 (Bericht) parallel aufsetzen.

## 1. Vereinheitlichung der Namen

| Vorher (sk1 / sk2) | Jetzt | Grund |
|---|---|---|
| `EnergieflussKnotenArt` (sk1) und `EnergieflussKnotenart` (sk2), gleiche Werte | **`EnergieflussKnotenart`**, einmal in `Energiefluss.cs` | Hauskonvention der Kernaufzählungen ist „…art“ als ein Wort (`Bandart`, `Kantenart`, `Anlagenart`, `Bauteilart`); Bilanz, Abbildung und Renderer teilen genau eine Aufzählung, die Werte 0 … 5 bleiben |
| Domänenklasse `EnergieflussModell` (sk1) neben `ChartRenderer.EnergieflussModell(…)` → `Zeichenmodell` (sk2) | Domänenklasse **`EnergieflussBilanz`** | Hauskonvention: `ChartRenderer.*Modell` liefert ein Zeichenmodell. Die Domänenklasse ist die Jahresbilanz der Version (Knoten mit Zufluss, Abfluss, Rest, geschlossen; Bänder; Hinweise), daher „Bilanz“, nicht „Daten“ oder „Graph“ |
| `EnergieflussKnoten.Spalte` als `double` mit Verlustknoten auf Spalte + 0,5 (sk1) gegen `EnergieflussBildKnoten.Spalte` als `int` mit der Spalte des Erzeugers (sk2) | **eine Regel (Konzept 3.1):** Spalte überall ganzzahlig 0 … 6; ein Verlust- oder Füllstandsknoten trägt die Spalte seines Eigentümers; die Lücke (Spalte + ½, unter dem Fuß) ist Zeichenregel des Renderers, kein Datum | Ein halbzahliges Datum wäre eine Zeichenregel im Domänenmodell; der Renderer stapelt die Verlustknoten ohnehin selbst nach der Lage ihres Eigentümers |

Nachgezogen: `Energiefluss.cs`, `EnergieflussBildDaten.cs`, `EnergieflussCtrl.cs`, `EnergieflussTests`,
`EnergieflussReferenzprojektWacheTests`, Konzept (2.3, 3.1, 4.3, 5, 6, 8), Protokolle SK-1 und SK-2 (Vermerk).
`EnergieflussModellTests` (sk2) behält den Namen: Sie prüft `ChartRenderer.EnergieflussModell`.

## 2. Die Abbildung `EnergieflussAbbildung.Bild(EnergieflussBilanz)` → `EnergieflussBildDaten`

Neue Kerndatei `EPOS.Kern/Allgemein/Bericht/EnergieflussAbbildung.cs`. Sie rechnet nichts (jede Menge, jeder Zeigertext
kommt aus der Bilanz) und zeichnet nichts (Lage, Maßstab, Beschriftung bleiben beim Renderer); die Hinweiszeilen bleiben
an der Bilanz. Die Sprache steckt in der Bilanz (`Englisch`), deshalb ohne zweiten Parameter.

- **Knoten:** `Kennung = Id`, `Spalte` (ganzzahlig, auf 0 … 6 begrenzt), `Name`, `Kurzname` (schmale Fassung), `Art`,
  `Zeigertext` wörtlich (leer → `null`, der Renderer bildet dann seinen Vorgabetext), `Rolle` `KNOTEN` bzw. `VERLUST`,
  `Mwh` = Jahressumme für den Bericht: Quellen ihr Abfluss, alle anderen ihr Zufluss (`EnergieflussAbbildung.Jahressumme`).
- **Bänder:** `Von`, `Nach`, `Mwh`, `Zeigertext` wörtlich; **Bandart** `Verlust` bei Form Verlust, `NichtZugeordnet`
  (gestrichelt) bei Form Rest („ungedeckt“, „nicht zugeordnet“), sonst `Normal`.
- **Farbrolle (Konzept 3.4), in dieser Reihenfolge:** Form Verlust → `VERLUST`; Form Rest → `REST`; Form Brennstoff →
  `BRENNSTOFF`; Form Umwelt (Umweltwärme, Umwelt (Kälte), Kondensationswärme) → `UMWELTWAERME`; Band in einen
  Füllstandsknoten → `SPEICHERLADUNG` (gespeicherte Ladung, kein Verlust); **aus einem Bus die Rolle der Verwendung**
  (Wärmepumpe `WAERME_WP`, Heizstab `HEIZSTAB`, Elektrokessel `WAERME_KESSEL`, Kälteerzeuger `KAELTE`, Split
  `KAELTE_DEZENTRAL`, Strombedarf `VERBRAUCH_GESAMT`, Einspeisung PV und BHKW `UEBERSCHUSS`, Speicher
  `SPEICHERLADUNG`, Heizung `HEIZWAERME`, Warmwasser `WARMWASSER`, Prozesswärme `PROZESSWAERME`, Kühlung `KAELTE`);
  **in einen Bus die Rolle der Quelle** (Netzbezug und Kältestrom eigener Zähler `STROM_NETZ`, Photovoltaik `STROM_PV`,
  BHKW `STROM_BHKW` am Strom- bzw. `WAERME_BHKW` am Wärmebus, Heizkessel und Elektrokessel `WAERME_KESSEL`, Wärmepumpe
  `WAERME_WP`, Heizstab `HEIZSTAB`, Solarthermie `WAERME_SOLAR`, Kälteerzeuger `KAELTE`, Split `KAELTE_DEZENTRAL`);
  aus einem Speicher die Verwendung (Kanalrollen, Kühlung `KAELTE`), Stromspeicher → Strombedarf `STROM_SPEICHER`;
  Quelle → Wandler die Quelle; unbekannte Kennung → Rückfall auf die Energieform (`STROM_NETZ`, `HEIZWAERME`,
  `KAELTE`). Gebündelte Knoten (`weitere:split`, `weitere:puffer`) zählen wie ihre Gruppe.
- **Elektrokessel** ist ein Kessel (`WAERME_KESSEL`, der Name unterscheidet); eine eigene Rolle gibt es nicht
  (Konzept 3.4 nennt keine).
- **Spaltentitel** (sieben): „Quellen“, „Wandler“, Name des Strombusses, „Wandler“, die Namen der Busse Wärme und
  Kälte, die im Bild stehen („Wärme · Kälte“, nur „Wärme“ ohne Kälte), „Speicher“, „Bedarf“ — vier neue Ressourcen
  `EF_SP_QUELLEN`, `EF_SP_WANDLER`, `EF_SP_SPEICHER`, `EF_SP_BEDARF` in beiden Sprachen, Designer mit
  `designer_neu.py schreiben` (19 002 Einträge, wiederholbar).

Öffentliche Schnittstelle für SK-3 und SK-4:

```
EnergieflussBilanz   b = EnergieflussCtrl.Lade(idProjekt, istStamm, englisch[, versionsname]);   // oder Energiefluss.Aus(VariantenDaten, englisch)
EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);                                          // b.Leer / b.Grund / b.Hinweise / b.KiText() bleiben an der Bilanz
Zeichenmodell breit  = ChartRenderer.EnergieflussModell(d, ChartRenderer.ENERGIEFLUSS_BREIT);
Zeichenmodell schmal = ChartRenderer.EnergieflussModell(d, ChartRenderer.ENERGIEFLUSS_SCHMAL);
Zeichenmodell bericht = ChartRenderer.EnergieflussModell(d, ChartRenderer.ENERGIEFLUSS_BERICHT, knotenwerte: true);
byte[] png            = ChartRenderer.EnergieflussBild(d, mass, knotenwerte);
```

## 3. Sichtprüfung an 1018, 1046, 1058, 1063

Die Wache `EnergieflussReferenzprojektWacheTests` zeichnet jede Bilanz in breit, schmal und Bericht und legt die PNG
unter `EPOS_ENERGIEFLUSS_PNG_ORDNER` ab (Sichtprüfung durch den Anwender über die Orchestrierung).

- **Richtig:** Spalten und Reihenfolge nach Tabelle 2.2; Rollen wie 3.4 (Netz braun, Photovoltaik grün, BHKW orange,
  Brennstoff beige, Umweltanteile mint, Wärmepumpe blau, Heizstab gelb, Kessel grau, Heizung rot, Warmwasser cyan,
  Strombedarf dunkelgrün, Einspeisung gelb, Kälte blau, Speicherladung orange, Verlust grau); Verlust- und
  Füllstandsknoten in der Lücke unter ihrem Eigentümer, gestapelt; „ungedeckt“ und „nicht zugeordnet“ gestrichelt;
  Bänder schließen lückenlos an; schmal mit Kurznamen und versetzten Spaltentiteln; Bericht mit Jahressumme je Knoten.
- **Behoben:** Innere Knotennamen wurden an der Spaltenbreite gekürzt („Wärmepumpe (Heiz…“, „Heizungspuffer 10…“,
  „Kältemaschine 20…“), im Bericht fiel damit die Jahressumme weg (gegen SK-Q12). Der Renderer lässt eine innere
  Beschriftung jetzt bis `EF_INNEN_BREITE` = 1,5 Spaltenabstände breit (Konzept 3.1); die Überdeckungsprüfung bleibt.
  Zehn der zwölf Energiefluss-Zeilen der `Messlatte_2026-10-16.sha256` (alle breiten und Berichtsproben, die Probe
  nennt „Wärmepumpe (Heizen)“) bewusst ersetzt; die zwei schmalen Zeilen und die 226 älteren Zeilen sind unverändert.
- **Bleibt, weil Konzept:** Ein Band von Spalte 0 nach Spalte 3 (Umweltwärme → Wärmepumpe) oder vom Strombus zur
  Einspeisung läuft hinter den Bussen der Zwischenspalten hindurch (die Knoten werden nach den Bändern gezeichnet);
  ein Knoten unter 12 px trägt keine Beschriftung (Elektrokessel 2,6 MWh in 1058 breit, Stromspeicher 1,2 MWh in
  1046 — die Namen stehen im Zeigertext, 2.5); ein langer Quellenname wird am linken Rand gekürzt
  („Kältestrom (eigene…“ in 1063, 150 px Rand, 3.1).

## 4. Abnahme

- `dotnet build EPOS.Kern.Tests -c Release`: 0 Fehler.
- `dotnet test … --filter "FullyQualifiedName~Energiefluss"`: 51 bestanden (24 Flussbau, 4 Referenzprojekte mit
  Zeichnung, 14 Zeichenmodell, 8 Abbildung, 1 Rollenwache).
- `dotnet test … --filter "FullyQualifiedName~Energiefluss|FullyQualifiedName~ChartRenderer|FullyQualifiedName~Zeichenmodell|FullyQualifiedName~Farb|FullyQualifiedName~Wache"`
  mit `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`: 848 bestanden, 0 fehlgeschlagen (5 min 9 s).
  `DokumentationLinkWacheTests` und `RepositoryOrdnungWacheTests` nach dem Protokoll noch einmal grün.
- `dotnet run --project Proben/ChartProben -c Release`: 269 Bilder, 0 Verstöße; Hashliste 238 Zeilen, jede Zeile der
  Messlatte (nach dem Ersetzen der zehn Energiefluss-Zeilen) unverändert darin.
- Keine SQL-Anweisung neu oder geändert; kein Schemaschritt; Referenzlauf unberührt.
