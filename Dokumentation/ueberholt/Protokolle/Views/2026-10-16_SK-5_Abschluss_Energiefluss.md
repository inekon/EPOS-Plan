# SK-5 — Abschluss der Welle Energieflussbild

Welle SK, Schritt SK-5, 16./17.10.2026, Zweig `sk5` (aus `sk2d` mit SK-1 bis SK-3, SK-2c, SK-2d-1 und `sk4`, dazu
die Berichtsmesslatten 620 × 245). Konzept (nach `ueberholt/` verschoben):
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../2026-10-16_Konzept_Energieflussbild_Sankey.md).
Vorgänger: [`2026-10-16_SK-2d-2_Oberflaeche_Stufen_Filter.md`](2026-10-16_SK-2d-2_Oberflaeche_Stufen_Filter.md),
[`2026-10-16_SK-4_Bericht_Energiefluss.md`](2026-10-16_SK-4_Bericht_Energiefluss.md).
Statuszeilen #1162 bis #1165.

## 1. Zusammenführung

- **`sk2d2` in `sk5`.** Zwei Fachkonflikte:
  - `EPOS.UI/Seiten/Berichte/UebersichtErgebnisFluss.razor`: SK-4 setzte an beiden `DiagrammSvg` die
    Vorlagenfeldmarke `stand.bild.energiefluss` (Stufe „ähnlich“, Hinweis `VF_ORT_ENERGIEFLUSS`), SK-2d-2 band das
    Modell an das gewählte Bildpaar (`@_bild?.Breit`, `@_bild?.Schmal`). Beides behalten.
  - `EPOS.UI.Tests/VorlagenfeldAbdeckungWacheTests.cs`: SK-4 hatte die SK-3-Ausnahme entfernt, SK-2d-2 ihr Muster
    auf `Modell="@_bild?.` umgestellt. Ergebnis: keine Ausnahme, das Bild ist über die Marke gedeckt.
  - `Resource.resx`/`Resource.en-US.resx`, `epos-ui.css`, Konzept, Protokolle, Wiki, Rasterprobe: ohne Konflikt
    zusammengeführt; keine doppelten Schlüssel, Designer unverändert (19 137 Einträge).
- **`origin/ios_migration_september` in `sk5`.** Testdatenbank auf origin unverändert (Schritt 238). Zwei Konflikte:
  `Dokumentation/LIESMICH.md` (das Klimadatenkonzept liegt auf origin schon in `ueberholt/`, die Zeile des
  Energieflussbilds aus `sk5`) und `Proben/Rasterprobe/diagrammprobe.mjs` (Fälle `fluss`, `hervorhebung`, `wahl` aus
  SK-3/SK-2d-2 und die Schriftmessung „Schrift im Satz“ aus #1157 im Fall `zoom` beide behalten; Kopf und Aufruf
  vereinigt). Designer danach 19 139 Einträge, unverändert. Keine Konfliktmarker im Repositorium.

## 2. Gate (einmal nach dem letzten Merge, Kopf `07e837efc`)

| Prüfung | Ergebnis |
|---|---|
| Bau `WP-Plan.Kern.slnf` (Release) | 0 Fehler |
| Windows-Schale auf Linux, Debug und Release (`EnableWindowsTargeting=true`) | je 0 Fehler |
| `EPOS.Kern.Tests` vollständig | 14 734 grün, 7 übersprungen |
| `EPOS.UI.Tests` | 9 692 grün |
| `KiKern.Tests` | 553 grün |
| `SpeicherEngine.Tests` | 397 grün |
| `SpeicherPlanung.Tests` | 27 grün, 1 übersprungen |
| Dokumentationswachen (Link, Repository-Ordnung, Wiki-Produktdaten) | 35 grün |
| Werkzeugtests Formularkarte, Auslieferungsvorlage, Gebäudevergleich, Zapfprofilvalidierung | 124, 63, 24, 39 grün |
| SqlDialektPruefer | 3 055 SQL-Texte, 0 Fundstellen |
| Designer, BOM in Markdown, Konfliktmarker | unverändert, keine, keine |
| Referenzlauf 38 Projekte gegen R57 `2026-10-16_R57_Schnellaufheizung` | GESAMT PASS (13 025 956 Werte), 1 233 von 1 233 CSV byte-gleich |
| ChartProben | 276 Bilder, 0 Verstöße; Hashliste 248 Zeilen, Messlatte `Messlatte_2026-10-16.sha256` 248 von 248 unverändert |
| Rasterprobe `diagrammprobe.mjs` | `fluss` 12 Bänder und 12 Knoten mit Zeigertext, 24 Zeilen Textalternative; `hervorhebung` Wandler 3/3, Bus 6/6, Verbinder 3/3; `wahl` Kette 12, Stufen 4 Titel, Strom 6, Kälte-Hinweis; `zoom` größter Abstand 0,50 px, Schrift im Satz 14 Bilder; alle Gegenproben verfehlt (richtig) |

Der Rechenweg ist unberührt; kein Schemaschritt, keine neue Basis.

## 3. Papiere

- Statuszeilen #1162 (SK-1 Flussmodell), #1163 (SK-2 Zeichnung: SK-2, SK-12, SK-2c Variante A, SK-2d-1), #1164
  (SK-3 Oberfläche: SK-3, SK-2d-2), #1165 (SK-4 Bericht, Katalogfassung 25, und dieser Abschluss).
- Konzept Diagramme Abschnitt 9: Rollen des Energieflussbilds (`STROM`, `WAERME`, `KAELTE`, `BRENNSTOFF`,
  `UMWELTWAERME`, `VERLUST`, `KNOTEN`) und die Rollenzahl 62.
- Konzept Energieflussbild: Abschnitt 13 „Offene Punkte beim Abschluss“ (Variante C „Bilanzbalken“ nach SK-Q16, die
  Ergebnisspalten nach 1.7), dann per `git mv` nach `ueberholt/`; Mockup `Energieflussbild_Mockup.html` nach
  `ueberholt/Mockups/`; Index und Verweise der acht SK-Protokolle nachgezogen. Vormerkung C in der Statusdatei 4.3,
  die Wiki-Seiten in 4.2.

## 4. Logbuch-Entwurf (Versionsnummer beim Anwender erfragen, nicht veröffentlicht)

Zusammengeführt aus SK-3, SK-2d-2 und SK-4 (Konzept Hilfesystem 13.4: ein Satz je sichtbarer Änderung):

- Unter Berichte & Kosten › Übersicht zeigt der Abschnitt „Ergebnisse – Energie“ mit der Darstellung „Fluss“ das
  Energieflussbild einer gewählten Version über Strom, Wärme und Kälte; die Werte erscheinen beim Überfahren.
- Das Energieflussbild lässt sich als Kette oder in vier Stufen zeigen, auf Strom, Wärme oder Kälte beschränken und
  hebt beim Zeigen das Umfeld eines Knotens hervor.
- Der Word-Bericht zeigt je Variante das Energieflussbild unter den Kennzahlen; für eigene Vorlagen gibt es den
  Platzhalter `stand.bild.energiefluss`.

Betroffene Wiki-Quellen: `Projekte/Wiki/Programm Dokumentation - Varianten.wiki`,
`Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki` (Upload ausstehend, Statusdatei 4.2).

## 5. Offen

- Variante C „Bilanzbalken“ je Energieform als vierte Stellung „Bilanz“ bzw. Berichtstafel (SK-Q16), vorgemerkt.
- Die drei Ergebnisspalten nach Konzept 1.7 (BHKW-Einspeisung, Standby des Speichersystems, PV-Abregelung) als
  Schemaschritt mit neuer Basis, vorgemerkt (SK-Q1 b).
