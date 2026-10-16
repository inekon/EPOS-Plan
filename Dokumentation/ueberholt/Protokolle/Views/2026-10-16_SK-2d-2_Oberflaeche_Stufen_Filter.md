# SK-2d-2 — Energieflussbild: Darstellung, Formfilter, Hervorhebung (Oberfläche)

Welle SK, Schritt SK-2d-2, 16.10.2026, Zweig `sk2d2` (von `sk2d`, Kopf `42f9438f2`). Konzept:
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../../aktuell/Dialoge/2026-10-16_Konzept_Energieflussbild_Sankey.md)
(Abschnitte 3.3, 3.5, 4.3a, 8). Vorgänger:
[`2026-10-16_SK-2d-1_Stufen_Verbinder_Filter.md`](2026-10-16_SK-2d-1_Stufen_Verbinder_Filter.md) und
[`2026-10-16_SK-3_Oberflaeche_Energiefluss.md`](2026-10-16_SK-3_Oberflaeche_Energiefluss.md).

**Anlass.** Anwenderentscheide vom 16.10.2026: SK-Q17 Schalter „Kette | Stufen“, Vorgabe Kette, Wahl je Benutzer gemerkt,
der Bericht zeigt immer die Kette; Formfilter „Alle | Strom | Wärme | Kälte“ je Sitzung; Hervorhebung beim Überfahren
oder Antippen (Umfeld voll, Rest abgeblendet); Werte nur im Zeigertext (SK-Q12); schmale Fassung unter 900 px aus dem
Stilblatt; Legende im Bild.

## 1. Bedienung

- Kopf der Ansicht „Fluss“: Versionswahl, daneben zwei Knopfgruppen im Muster des Umschalters „Tabelle | Grafik |
  Fluss“ (`role="radiogroup"`, ein Tabulatorhalt, Pfeiltasten, Pos1/Ende; Fokus folgt der Tastenwahl). Beschriftungen
  aus dem Kern (`EnergieflussAbbildung.Darstellungstext`/`Filtertext`), Sprachausgabe der Gruppen neu
  `BK_ERG_FLUSS_DARSTELLUNG` und `BK_ERG_FLUSS_FILTER` in beiden Sprachen; `BK_ERG_FLUSS_ZEIGER` nennt die Hervorhebung.
- Darstellung je Benutzer unter `Uebersicht.Ergebnisse.Fluss.Darstellung` (Hülle `FlussDarstellungLesen/Schreiben`, ein
  unbekannter Wert gilt als Kette); Filter je Sitzung in der Hülle (`s_flussFilter`) wie die Versionswahl.
- Leerer Filter: Hinweis im Bild (Renderer, Marke `hinweis`) und als leise Zeile `.epos-ergebnis-fluss-filterhinweis`
  über dem Bild; die Knöpfe bleiben stehen.
- KI-Ansichtszeile: „Ansicht: Fluss (Energieflussbild der Version …; Darstellung …; Energieform …)“; die Flussliste bleibt
  die der ganzen Version. `KiMaskenabdeckungWacheTests`: Zahl der Eingaben bleibt 1 (Knopfgruppen sind Anzeigezustand),
  Vermerk ergänzt.

## 2. Puffer

`ErgebnisFlussStand.Bild(darstellung, filter)` liefert `ErgebnisFlussBild` (breit, schmal, Hinweis). Kette/„Alle“ sind
`Breit`/`Schmal` des Standes; jede andere der 8 Wahlen baut der Delegat `Bildbau` der Hülle
(`EnergieflussAbbildung.Gefiltert` → `ChartRenderer.EnergieflussModell` breit und schmal) beim ersten Bedarf und hält sie
im Stand — der Stand liegt im Puffer der Hülle je Version, Simulationsstand und Sprache. Die Komponente wählt das Bild nur
beim Laden und bei einem Wechsel, nie je Rendern. bunit: nach Stufen und Stufen/Strom 3 gebaute Bilder, Hin- und
Rückschalten liefert dieselben Referenzen, alle 8 Kombinationen ergeben 8 Bilder (= 16 Modelle), keine Neuladung der Kennzahlen.

## 3. Hervorhebung — wo das JS liegt

Eigenes kleines Modul `EPOS.UI/wwwroot/epos-energiefluss.js`, von der Komponente einmal je `figure` gebunden
(Ereignisdelegation `pointerover`/`pointerdown`, `pointerleave` der Maus hebt auf; ein neues Bildpaar ruft `aufheben`).
**Begründung:** Der Vertrag 3.3 verlangt keine Änderung an `DiagrammSvg`, Schreiber, Maler oder Wache; die Hervorhebung
braucht keinen Zustand des Bausteins und keinen Rundlauf, und `diagrammprobe.mjs --fall zoom` bleibt damit unberührt.
Kanten und Busse stehen als JSON an der `figure` (`data-kanten` = je Band `[Marke, Von, Nach]` zwischen bekannten,
verschiedenen Knoten der ungefilterten Bilddaten; `data-busse`); die Marke wird nie zerlegt. Regel wie
`EnergieflussAbbildung.Umfeld`; hervorgehoben werden die Bandmarken des Umfelds und die Knoten an ihren Enden (Klasse
`epos-fluss-an`), die `figure` trägt `epos-ergebnis-fluss-bild--hervor`, das Stilblatt blendet übrige `fluss:`/`knoten:`-
Elemente auf Deckkraft 0,18 ab; `stufe:`, `legende:` und `hinweis` bleiben unberührt. Tastatur: wie die Zeigerzeile — die
Elemente des Bildes sind nicht fokussierbar.

## 4. Nachweise

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| Windows-Schale `-c Release -p:Platform=x64 -p:EnableWindowsTargeting=true` | 0 Fehler |
| `EPOS.UI.Tests` vollständig | 9 665 bestanden (ein Lauf zeigte 1 Rot in `VorlagenfeldAbdeckungWacheTests`: Ausnahmemuster `Modell="@_stand.` → `Modell="@_bild?.` nachgezogen, Klasse danach grün) |
| `EPOS.Kern.Tests` gefiltert `Energiefluss\|Wache\|Uebersicht\|Ki` | 1 144 bestanden, 0 rot |
| neue bunit-Fälle (`UebersichtErgebnisFlussTests`) | 7: Schalter gemerkt, Filter je Sitzung, leerer Filter mit Hinweis, Puffer ohne Neuberechnung, JSON der Kanten/Busse, Pfeiltasten, KI-Zeile; dazu `StilblattTests.SK2d2_…` (Abblendregel) |
| `diagrammprobe.mjs --fall fluss` | vorher rot (4 von 28 Elementen ohne Zeigertext — die Farbfelder der Legende aus SK-2c); nachher 24 Elemente, 12 Bänder und 12 Knoten, Gegenprobe verfehlt |
| `--fall hervorhebung` | Wandler `wp_heizen` 3/3 Bänder, Bus `strom` 6/6 Bänder, Verbinder `fluss:strom:wp_heizen` 3/3 Pfade; Gegenproben ohne Modul und ohne Abblendregel verfehlt |
| `--fall wahl` | Kette 12 Bänder, Stufen 4 Titel, Strom 6 Bänder, Kälte mit Hinweis im Bild und als Zeile, Pfeil links → Wärme mit Fokus |
| `--fall zoom` | vorher und nachher erfüllt: ×12, 41 Schritte, größter Abstand 0,50 px, 0 Mutationen; Gegenprobe verfehlt |

Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Varianten.wiki` um Kette/Stufen, Filter und Hervorhebung ergänzt;
Gegenlesemuster ohne Treffer. Rasterprobe-LIESMICH um die Fälle `hervorhebung` und `wahl` ergänzt.

## 5. Logbuch-Entwurf (Upload gebündelt, Versionsnummer beim Anwender erfragen)

- Im Abschnitt „Ergebnisse — Energie“ der Übersicht lässt sich das Energieflussbild als Kette oder in vier Stufen zeigen
  und auf Strom, Wärme oder Kälte beschränken.
- Beim Zeigen auf einen Knoten des Energieflussbilds wird sein Umfeld hervorgehoben.

## 6. Offen

- SK-4 (Bericht, Vorlagenfeld `stand.bild.energiefluss`) ersetzt die Ausnahme in `VorlagenfeldAbdeckungWacheTests`.
- SK-5: Statuszeile, Konzept nach `ueberholt/`.
