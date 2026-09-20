# DG-E2 — SVG-Ausgabe des Zeichenmodells und der Baustein (Protokoll)

Etappe E2 des Konzepts
[`Konzept_Diagramme_Interaktiv_EPOS-Plan.md`](../../../aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md)
in zwei Teilen: **Kern** — Zeichenmodell, `SvgSchreiber`, `JahresgangModell` und die
Gegenprobe; **Oberfläche** — der Baustein `DiagrammSvg`, der viewBox-Modus des JS-Moduls und
die erste umgestellte Maske (Klimadialog). Dieses Papier hält beide Abschnitte; der UI-Teil
ergänzt seinen später.

Der gültige Stand steht in der Statusdatei und im Konzept; hier steht, wie es geworden ist.

---

## Kern — Modell, SvgSchreiber, JahresgangModell, Gegenprobe

### Die Aufgabe

Aus der Befehlsliste, die die Etappe E1 hinter den `ChartRenderer` gezogen hat, eine **zweite
Ausgabe** machen: `SkiaMaler.Png` malt sie für den Bericht, `SvgSchreiber` schreibt sie für den
Bildschirm. Die Zeichenfläche steht im SVG als **inneres `<svg>` in Datenkoordinaten**, damit
Zoom und Verschieben eine Attributänderung bleiben — ohne Neuzeichnen und ohne Rundlauf
(Entscheid DG-Q1, Prüfstand § 5 des Konzepts).

**Die unverrückbare Bedingung: kein PNG darf sich ändern.** Die Messlatte dafür lag aus E0 und
E1 bereit — 91 Hashes aus den Probebildern; jede Abweichung wird am Bild gesucht, nie durch
Anpassen der Liste behoben.

### Was entstanden ist

| Datei | Was dazugekommen ist |
|---|---|
| `EPOS.Kern/Allgemein/Bericht/Zeichnung/Zeichenmodell.cs` | `Zeichenbefehl.Marke`; die Records `Datenfenster`, `Zeichenflaeche`, `Datenreihe`; `Zeichenmodell.Flaeche`, `.Reihen`, `.FuegeReihe`; die statische `Pfadregel`; die Klammer `Zeichenhilfe.Markiert` |
| `EPOS.Kern/Allgemein/Bericht/Zeichnung/SvgSchreiber.cs` | **neu** — `SvgKnoten` (Name, geordnete Attribute, Kinder, Textinhalt, Marke) und `SvgSchreiber.Baum` / `.Text` |
| `EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs` | `JahresgangModell` (der Rumpf von `Jahresgang`), `Jahresstundenteilung`, die Marken im Bild und im Helfer `Legende` |
| `Proben/ChartProben/Program.cs` | vier SVG-Gegenproben und der Schalter `--svg` |
| `EPOS.Kern.Tests/` | **neu** `SvgSchreiberTests` (21) und `PfadregelTests` (8); Ergänzungen in `ChartRendererTests` (5) und `ZeichenmodellTests` (3) |

**Die Marke.** Ein `Zeichenbefehl` darf sagen, wozu er gehört: `titel`, `xachse`, `yachse`,
`reihe:<Name>`, `legende:<Name>`, `nulllinie`, `leerhinweis`; `null` heißt „keine Marke" und
ist die Vorgabe. Der `SkiaMaler` übergeht sie — das PNG bleibt byte-gleich —, der
`SvgSchreiber` gibt sie als `data-marke` weiter, und die Oberfläche schaltet damit Gruppen ein
und aus.

Gesetzt wird sie über `Zeichenhilfe.Markiert(marke, inhalt)`: Der Block schreibt in einen
`Befehlssammler`, und die Klammer setzt die Marke an jedem Befehl, der noch keine hat. Der
Grund ist Tippfehlerfläche: Eine Marke gehört zu einem BLOCK — die x-Achse sind sechs
Rasterlinien, sechs Beschriftungen und ein Titel —, und die Helfer, die sie absetzen, haben
zwölf Nutzer. So steht sie einmal an der Klammer statt an jedem Helferaufruf.

**Die Zeichenfläche.** `Zeichenflaeche(Rahmen Bild, Datenfenster Daten)` trägt das
Pixelrechteck und das, was darin steht: `XVon`/`XBis` als erste und letzte Stützstelle des
Bildes, `YVon`/`YBis` als Grenzen der fertigen Skala. Die Befehlsliste trägt fertige
Bildpunkte — daraus ist nicht mehr abzulesen, welche Stunde an welcher Stelle steht; der
SVG-Weg braucht genau das.

**Die Pfadregel** (DG-Q5, „eine Konstante im Modell"): `ROH_BIS_STUETZSTELLEN = 8760`,
`ROH_BIS_REIHEN = 3`, `Roh(stuetzstellen, reihen)` und `Gebuendelt(werte, spalten)` — je
Bildpunktspalte Minimum und Maximum in Indexreihenfolge, höchstens zwei Punkte je Spalte.

**Der Schreiber.** `SvgSchreiber.Baum(modell, palette, kennung)` baut den Knotenbaum,
`SvgSchreiber.Text(...)` serialisiert ihn. Deterministisch heißt hier: Attribute in der
Reihenfolge, in der sie gebaut werden, Zahlen in `InvariantCulture` (Bildpunkte `0.##`,
Datenwerte `0.###`), LF-Zeilenenden, kein Zufall und keine Zeitangabe. Farben werden **beim
Schreiben** gegen `palette ?? Farbpalette.Aktuell` aufgelöst — genau wie beim Malen; deshalb
tragen Bildschirm und Bericht dieselben Farben, und ein Tausch erreicht beide, ohne dass eine
Zeichenmethode angefasst wird (DG-Q7).

### Die Entscheide

**DG-E2-2 — Reihen im SVG roh, PNG unverändert.** Das innere `<svg>` zeichnet die Reihen aus
`Datenreihe` nach `Pfadregel` (roh bis 8 760 Stützstellen und drei Reihen, sonst gebündelt);
der auf jeden n-ten Wert gekürzte Pixelpfad bleibt allein für das PNG, bis E4 die PNG-Linien
auf gebündelt umstellt und beide Wege zusammenfallen. *Warum:* Der gekürzte Pfad verliert
Spitzen zwischen zwei Stützstellen der Schrittweite — beim Zoom wäre genau das zu sehen, was
nicht da ist. Die Daten liegen ohnehin vor; sie im Modell mitzuführen kostet nichts, und ein
zweiter Pfad ändert am PNG nichts, weil der Maler `Datenreihe` nicht kennt.

**DG-E2-3 — Nur die Reihen liegen in Datenkoordinaten.** Raster, Achsen, Legende und Titel
bleiben Pixel-Elemente; beim Zoom (nur Zeitachse, y bleibt voll) blendet die Oberfläche die
`xachse`-Elemente aus und zeichnet die Ticks aus `Jahresstundenteilung` nach. *Warum:* Nur die
Reihen müssen mitzoomen. Läge die Beschriftung mit in der Datenfläche, skalierte
`preserveAspectRatio="none"` die Schrift in beide Richtungen verschieden — Text würde
verzerrt, und die Rasterlinien wären keine Bildpunkte mehr breit.

**DG-E2-4 — Nachladen ab dem Vierfachen** (gebündelte Pfade) kommt mit E3, wenn erstmals ein
gebündelter Pfad entsteht; in E2 gibt es keinen. *Warum:* Der Jahresgang des Klimadialogs führt
eine Stundenreihe, also geht sein Pfad roh, und ein roher Pfad zeigt bei jedem Zoom die echte
Stunde. Ein Nachladeweg ohne einen einzigen gebündelten Pfad wäre unprüfbarer Vorrat.

### Zwei Stellen, an denen die Umsetzung vom Auftrag abweicht

1. **Der Textbefehl bekommt `dominant-baseline="text-before-edge"`.** Der Auftrag nannte die
   Attributliste ohne dieses Attribut („y unverändert"). Die y-Koordinate geht auch
   unverändert durch — aber sie meint im Modell die **linke OBERE Ecke**, nicht die
   Grundlinie: Der `SkiaMaler` zieht dafür den Aufstieg ab, und die 46 Beschriftungen des
   Bestands rechnen damit (eine y-Beschriftung steht auf `y − Zeilenhöhe/2`). Ohne das
   Attribut säße jeder Text um eine Zeilenhöhe zu hoch, der Bildtitel zur Hälfte außerhalb der
   Fläche. Die Alternative — die Grundlinie hier ausrechnen — bräuchte die Schriftmetrik und
   damit SkiaSharp im Ausgabeweg; genau das soll nicht sein. Es ist dieselbe Aufteilung wie
   bei x: Der Renderer gibt die gerechnete Stelle, und jeder Ausgabeweg verschiebt auf seine
   Art (der Maler über die gemessene Breite, das SVG über `text-anchor`).
2. **`Jahresstundenteilung` und `XAchseFenster` gehen durch einen gemeinsamen Rumpf**
   (`Stundenteilung(double, double)`) statt dass `XAchseFenster` die öffentliche Funktion
   selbst ruft. Grund: Eine **Viertelstundenreihe** zählt Jahresstunden in Vierteln — `h0` und
   `h1` sind dort gebrochen, und die öffentliche Funktion führt `int`. Ein Runden im Bild wäre
   eine stille Verhaltensänderung an einer Stelle, die keine Messlatte abdeckt (kein
   Probebild verbindet Viertelstundenreihe und Fenster). Eine Regel, drei Türen; dass beide
   dasselbe liefern, prüft `Jahresstundenteilung_deckt_sich_mit_der_gezeichneten_Achse` am
   fertigen Modell.

### Was an der Byte-Gleichheit schwierig war

1. **Die Marke gehört zur Gleichheit des Befehls.** `Zeichenbefehl` ist ein Record; eine
   `init`-Eigenschaft auf dem abstrakten Basistyp geht in jedes `Equals` der abgeleiteten
   Records ein. Das ist gewollt — ein Bild, dessen Legende ihre Marke verlöre, ist nicht
   dasselbe Modell —, heißt aber auch: Jede Marke, die irgendwo gesetzt wird, verändert das
   Modell und muss vom Maler übergangen werden. Der `SkiaMaler` liest sie nirgends; die
   Messlatte ist der Nachweis.
2. **`Markiert` sammelt und setzt die Marke neu** (`b with { Marke = … }`) statt sie beim
   Anlegen mitzugeben. Der Sammler behält die Reihenfolge, die Befehle bleiben sonst wörtlich
   dieselben. Wäre statt dessen jeder Helfer um einen Parameter erweitert worden, hätte jede
   der zwölf Legenden-Aufrufstellen angefasst werden müssen.
3. **Das Achsenkreuz bleibt ohne Marke.** Es gehört weder zu `xachse` noch zu `yachse`: Blendet
   die Oberfläche beim Zoom die Teilung der x-Achse aus, müssen die beiden Achsenlinien stehen
   bleiben. Das ist keine Byte-Frage, aber die Stelle, an der die Marken zum ersten Mal eine
   Aussage über das Bild treffen und nicht nur eine Zuordnung.
4. **`Jahresgang` gibt jetzt eine Ausdrucksform zurück** (`=> SkiaMaler.Png(JahresgangModell(…))`).
   Der Wächter `ZeichenmodellWacheTests` zählt `public static byte[]` (26, unverändert) und
   verlangt mindestens ebenso viele Übergaben an `SkiaMaler.Png`; die Aufteilung nimmt eine
   Übergabe aus dem Leerfall heraus, die Zahl bleibt mit 41 weit darüber.
5. **Der Leerfall gibt jetzt das Modell zurück, nicht die Bytes.** Er trägt seither die Marke
   `leerhinweis` und **keine** Zeichenfläche — ein Bild ohne Reihen hat keine.

**Kein Bild ist gewandert.** Der Messlatte-Diff war nach jedem der vier Commits leer; es gab
keinen Fall, in dem ein Bild zu untersuchen gewesen wäre.

### Die Schnittstelle, die der UI-Teil benutzt

| Was | Signatur |
|---|---|
| Knoten | `sealed class SvgKnoten`: `Name`, `IReadOnlyList<KeyValuePair<string,string>> Attribute`, `IReadOnlyList<SvgKnoten> Kinder`, `Inhalt`, `Marke`, `Attribut(name, wert)`, `Fuege(kind)`, `Alle()` |
| Baum und Text | `SvgKnoten SvgSchreiber.Baum(Zeichenmodell, Farbpalette = null, string kennung = "d")`; `string SvgSchreiber.Text(…)` mit denselben Gaben, dazu `Text(SvgKnoten)` |
| Fläche | `sealed record Zeichenflaeche(Rahmen Bild, Datenfenster Daten)`; `sealed record Datenfenster(double XVon, double XBis, double YVon, double YBis)` |
| Reihe | `sealed record Datenreihe(string Name, Farbton Ton, float Staerke, Strichmuster Muster, double[] Werte)` |
| Marke | `string Zeichenbefehl.Marke { get; init; }`; `Zeichenhilfe.Markiert(this IZeichenziel, string, Action<IZeichenziel>)` |
| Modell des Bildes | `Zeichenmodell ChartRenderer.JahresgangModell(string titel, IReadOnlyList<Reihe> reihen, string xTitel, string yTitel, bool minimumNull = false, Achsenfenster fenster = null)` |
| Ticks beim Zoom | `IReadOnlyList<(int Stunde, string Text)> ChartRenderer.Jahresstundenteilung(int von, int bis)` |
| Pfadart | `static class Pfadregel`: `ROH_BIS_STUETZSTELLEN`, `ROH_BIS_REIHEN`, `bool Roh(int, int)`, `IReadOnlyList<Punkt> Gebuendelt(double[], int)` |

Die Griffe im Markup: `class="epos-flaeche"` am inneren `<svg>`, `class="epos-reihe"` und
`data-reihe="<Name>"` an jedem Reihenpfad, `data-marke` an jedem Element, das aus einem
markierten Befehl entstand. **Zoom und Verschieben sind die `viewBox` des inneren `<svg>`** —
eine Attributänderung, sonst nichts.

### Nachweis

| Prüfung | Ergebnis |
|---|---|
| Windows-Messlatte des Rechners (91 Hashes) | 91 von 91 gleich, Text-Diff leer — nach jedem der vier Commits |
| `Proben/ChartProben` | 76 Bilder geprüft, 0 Verstöße (72 wie bisher, dazu die vier SVG-Gegenproben); **91 Hashes geschrieben**, die Messlatte bleibt bei 91 Zeilen |
| `WP-Plan.Kern.slnf` Bau und volle Suite mit den CI-Schaltern | grün |
| neu `SvgSchreiberTests` | 21 |
| neu `PfadregelTests` | 8 |
| `ChartRendererTests`, `ZeichenmodellTests`, `ZeichenmodellWacheTests`, `SkiaMalerTests`, `FarbpaletteTests` | grün, die vier Wächter unverändert |
| SVG des Klimadaten-Jahresgangs (`--svg`) | 108 473 Byte, 49 Knoten — das PNG desselben Bildes misst 41 617 Byte, der Baum bleibt klein, die Last steckt im `d`-Attribut der rohen Reihe (8 760 Stützstellen) |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

Die Sichtprüfung des geschriebenen SVG (Edge, kopflos) zeigt Titel, Legende, Raster, beide
Achsen, die Nulllinie und die rohe Reihe mit ihrem Tagesgang als dichtes Band — genau das, was
der auf jeden siebten Wert gekürzte PNG-Pfad nicht zeigt.

### Offen nach dem Kernteil

* **Der UI-Teil der Etappe:** `EPOS.UI/Bausteine/DiagrammSvg.razor` (Razor-Elemente aus dem
  Baum, Legende schaltbar, Zeigerzeile, Bereich → `Achsenfenster` ohne Kernaufruf),
  `epos-diagramm.js` mit viewBox-Modus, der Klimadialog als erste Stelle, bunit und die Abnahme
  am Gerät (A-DG-1).
* **Nur `Jahresgang` hat sein Modell öffentlich.** Die übrigen 25 Bilder folgen mit E3, je
  Gruppe eine `…Modell`-Methode nach demselben Muster.
* **Die Marken sind auf den Jahresgang beschränkt** — außer `legende:<Name>`, die alle zwölf
  Nutzer des Helfers `Legende` tragen. Jedes weitere Bild setzt seine mit E3.
* **`dominant-baseline`** ist die eine Stelle, an der das SVG die Schriftlage anders löst als
  der Maler. Zeigt die Abnahme am Gerät eine sichtbare Abweichung zum PNG, ist die Alternative
  eine Zeilenhöhe im `Schrift`-Satz — gerechnet im Layout, wo die Metrik ohnehin vorliegt.
* **`Farbpalette.Aktuell` ist prozessweiter Zustand.** Der Schreiber nimmt sie als Vorgabe;
  die Oberfläche sollte ihre Palette ausdrücklich übergeben, sobald zwei Ansichten
  verschiedene Paletten zeigen könnten.
