# SK-2d-1 — Energieflussbild: Stufen, Verbinder, Formfilter (Kern)

Welle SK, Schritt SK-2d-1, 16.10.2026, Zweig `sk2d` (von `sk2c`, Kopf `a71d1ec02`). Konzept:
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../../aktuell/Dialoge/2026-10-16_Konzept_Energieflussbild_Sankey.md)
(Abschnitte 3.1, 3.3, 3.5, 8, 10 mit SK-Q13 und SK-Q17, Anhang 12). Vorgänger:
[`2026-10-16_SK-2c_Variante_A.md`](2026-10-16_SK-2c_Variante_A.md). Die Oberfläche (Schalter „Kette | Stufen“,
Filterknöpfe, Hervorhebung im Modul von `DiagrammSvg`, Browserprobe) folgt als SK-2d-2.

**Anlass.** Anwenderentscheide vom 16.10.2026: Variante A „Kette“ für breit und Bericht, die schmale Fassung nach der
Verbinderregel aus Variante B (SK-Q13); Variante B „Vier Stufen Einsatz | Anlagen | Energieform | Nutzen“ zusätzlich
wählbar (SK-Q17); Formfilter Alle | Strom | Wärme | Kälte wie in der Anschauung der Recherche (`Varianten.html`);
Bildschirm ohne Zahlen, Werte im Zeigertext (SK-Q12).

## 1. Umgesetzte Regeln

| Stufe | Regel |
|---|---|
| **Renderer** `ChartRenderer.EnergieflussModell` | **Stufen:** vier feste Spalten nach der Knotenart (Quelle 0, Wandler 1, Bus 2, Senke 3) mit Titeln aus `EnergieflussBildDaten.Stufentitel` (Marke `stufe:<Spalte>`, Kopf 52 px, schmal 46 px); Wegpunkte, Spuren, Relaxation, Tausch, Kästen, Verluste, Legende wie in der Kette. **Verbinderregel** (`Verbinderregel`, `EfBand.Verbinder`): Bus → Wandler — in den Stufen auch jedes Band, das nicht nach rechts liefe — als zwei Stummel unter einer Marke: am Bus mit Pfeilspitze und „→ Gerät“ (Name, schmal Kurzname, Bericht mit Wert), am Gerät mit offenem Kreis (Rolle des Bandes, Füllung `HINTERGRUND`) und der Kurzform des Busses; Länge min(90 px; 30 % der Spaltenlücke); die Stummel fächern zu ihren Beschriftungszeilen auf (von oben nach unten geplant) und stehen in der Ausgangsfolge vor dem ersten Band, das auf Stummellänge tiefer sinkt als sie (Schwelle 28 px = 6 px / Ease 0,216), spätestens vor den Verlusten; Verbinder werden zuletzt gezeichnet (kein Band überdeckt ihre Beschriftung). Ein Verbinder zählt nicht in den längsten Weg zur Senke, zieht keine Knoten zueinander, bekommt keinen Wegpunkt und zählt nicht als Kreuzung (`EnergieflussKreuzungspaare`: eine Marke mit mehreren Pfaden). Die Regel gilt in den Stufen, in der **schmalen Fassung** (680 × 560) und als **Rückfall der Kette** über `EF_MAX_SPALTEN_KETTE` = 6 Spalten. **Verlustziel:** der Abzweig zielt `max(12 px; Senkung / Ease(Abzweiglänge / Lücke))` unter den Fuß — ein Band vor ihm sinkt auf Abzweiglänge höchstens ebenso tief, eines nach ihm tiefer. **Beschriftungsplätze:** Kästen und Balken sind belegt. **Leeres Bild** mit `Hinweis` mittig (Marke `hinweis`). Die `NotSupportedException` für Stufen entfällt. |
| **Abbildung** `EnergieflussAbbildung` | `Bild(bilanz, filter = Alle)`, `Gefiltert(daten, filter, englisch)` (Regel wie `sichtbar` der Anschauung: ein Wandler trägt die Formen seiner Abgabe, jeder andere Knoten die Formen aller seiner Bänder ohne Verluste; es bleibt jedes Band der Form oder eines Knotens, der sie trägt, ein Verlustabzweig mit seinem Eigentümer; Rest entfällt, Knoten ohne Band entfallen, Legende folgt, Stufentitel bleiben, leerer Filter mit `Hinweis` „Kälte: keine Flüsse in dieser Version“); `Stufentitel(englisch)`, `Filtertext`, `Darstellungstext`, `FormDesFilters`, `Form(Band)` (Umkehrung der Rolle); `Umfeld(daten, kennung)` als Referenz der Hervorhebungsregel (bis zum nächsten Bus, ein Bus nur seine Bänder, Verlustabzweige gewöhnliche Bänder). |
| **Bilddaten** `EnergieflussBildDaten` | neu `Stufentitel`, `Filter`, `Hinweis`; Aufzählung `EnergieflussFilter { Alle, Strom, Waerme, Kaelte }`. |
| **Ressourcen** | neu in beiden Sprachen: `EF_ST_EINSATZ`, `EF_ST_ANLAGEN`, `EF_ST_ENERGIEFORM`, `EF_ST_NUTZEN`, `EF_FILTER_ALLE`, `EF_FILTER_LEER`, `EF_DARSTELLUNG_KETTE`, `EF_DARSTELLUNG_STUFEN`; entfernt (ohne Verwendung seit SK-2c): `EF_SP_QUELLEN`, `EF_SP_WANDLER`, `EF_SP_BEDARF`; `Resource.Designer.cs` über `designer_neu.py schreiben`. |

## 2. Schnittstelle für SK-2d-2 und Vertrag der Hervorhebung

Konzept 3.5 (Tabelle) und 3.3 („Umfeld für die Hervorhebung“): {Kette, Stufen} × {breit, schmal} × {Alle, Strom,
Wärme, Kälte} = 16 Zeichenmodelle je Version aus `EnergieflussAbbildung.Bild(bilanz, filter)` und
`ChartRenderer.EnergieflussModell(daten, mass, false, darstellung)`; der Bericht immer Kette, ungefiltert, mit
Knotenwerten. Die Hülle gibt dem Baustein je Version die Kanten (`Marke`, `Von`, `Nach` aus den ungefilterten
Bilddaten) und die Busse (Knoten mit `Art == Bus`) als JSON mit; das JS zerlegt keine Marke (Kennungen tragen selbst
Doppelpunkte) und rechnet das Umfeld nach der Regel von `EnergieflussAbbildung.Umfeld`. Modelle je Version im
vorhandenen Puffer (`ErgebnisFlussStand`) halten — einmal rechnen oder beim ersten Wechsel nachrechnen, nie je Rendern.

## 3. Abweichungen von `Varianten.html`

- **Stummel fächern auf:** Das Ende eines Stummels liegt in der Mitte seiner Beschriftungszeile (die Anschauung senkte
  jeden Stummel fest um 6 px — drei dünne Stummel an einem Bus lagen übereinander).
- **Ausgangsfolge der Stummel:** vor dem ersten Band, das auf Stummellänge tiefer sinkt (die Anschauung stellte Verbinder
  immer zuunterst; bei 1046 kreuzte das Band „nicht zugeordnet“ dann den dicken Heizstab-Stummel).
- **Verlustziel nach der Spaltenlücke** (die Anschauung: fest 12 px; bei vier Spalten kreuzte der Speicherverlust des
  Strombusses das Band „nicht zugeordnet“ der Gaben).
- **Formfilter lässt weg statt abzublenden** (Anwenderauftrag); ein Verbinder im Filter besteht aus beiden Stummeln.
- **Umfeld** nimmt Verlustabzweige jedes durchlaufenen Wandlers mit (die Anschauung nur die des Startknotens).

## 4. Kreuzungszahlen (Paare sich schneidender Bandmittellinien, Verbinder zählen nicht)

| Projekt | Kette breit | Kette schmal (Verbinder) | Bericht | Stufen breit | Stufen schmal |
|---|---|---|---|---|---|
| 1018 | 0 | 0 (keine) | 0 | 0 | 0 |
| 1046 | 0 | 0 (`strom→heizstab`, `strom→wp_heizen`) | 0 | 0 | 0 |
| 1058 | 2 (unvermeidbar, SK-2c) | 0 (`strom→elektrokessel`, `strom→wp_heizen`, `strom→wp_kuehlen`) | 2 | 0 | 0 |
| 1063 | 0 | 0 (`strom→elektrokessel`) | 0 | 0 | 0 |

Formfilter: Strom und Wärme in allen vier belegt; Kälte bei 1018 und 1046 leer und benannt, bei 1058 (4 Bänder) und
1063 (5 Bänder) belegt. Die Gaben der ChartProben: Kette drei unvermeidbare Kreuzungen (unverändert), Stufen null.

## 5. Nachweise

- `dotnet build EPOS.Kern.Tests -c Release`, `dotnet build EPOS.UI.Tests -c Release`: 0 Fehler.
- `EPOS.Kern.Tests` gefiltert `Energiefluss|ChartRenderer|Zeichenmodell|Farb|Wache`: 864 grün (darunter die 66 Fälle
  `Energiefluss*`); `EPOS.UI.Tests` gefiltert `Uebersicht|Stilblatt|KiMasken`: 267 grün (Hülle unverändert).
- `dotnet run --project Proben/ChartProben -c Release`: 276 Bilder, 0 Verstöße; Messlatte: die 226 älteren Zeilen
  unverändert, `energiefluss*` jetzt 22 Zeilen (`energiefluss`, `energiefluss_bericht` und die Gegenproben der Kette
  byte-gleich; `energiefluss_schmal`, `energiefluss_schmal_wirkt_b`, `energiefluss_kaelte_split` ersetzt; zehn neu).
- Sichtprüfung der PNG der vier Referenzprojekte (Stufen breit und schmal, Kette schmal, Filter Strom/Wärme/Kälte in
  Kette und Stufen) über `EPOS_ENERGIEFLUSS_PNG_ORDNER`.

## 6. Offen (SK-2d-2)

Schalter „Kette | Stufen“ (je Benutzer gemerkt, Vorgabe Kette), Filterknöpfe (je Sitzung), Hinweiszeile des leeren
Filters, 16 Modelle je Version in der Hülle, Hervorhebung im Modul von `DiagrammSvg` nach dem Vertrag, Stilblatt,
`diagrammprobe.mjs` Fall `fluss` samt Gegenprobe gegen `Umfeld`, Windows-Schale mit `EnableWindowsTargeting=true`
gebaut. Der Bericht (SK-4) nimmt weiterhin `EnergieflussModell(daten, ENERGIEFLUSS_BERICHT, true)` — Kette, ungefiltert.
