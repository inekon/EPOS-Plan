# SvgProbe — ein Jahresgang als SVG aus C#, gemessen im echten Browser

**Zweck.** Der Prüfstand zum Konzept
[`Konzept_Diagramme_Interaktiv_EPOS-Plan.md`](../../Dokumentation/aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md)
(DG-1): Kann der Kern statt eines PNG ein SVG liefern, das der Browser selbst hält — und was
kostet das bei 8 760 Stützstellen je Reihe? Gemessen werden Größe, Erzeugungszeit und
Determinismus in der Konsole, Renderzeit, viewBox-Zoom und Trefferprüfung in Chromium.

Die Probe ist **kein Umbau des Bestands** und **keine Bibliothek**: `SvgZeichner.cs` ist ein
Zeichenmodell von 250 Zeilen ohne Abhängigkeit, mit den Maßen von
`ChartRenderer.Jahresgang` (1 304 × 440, Zeichenfläche 1 154 × 240). Der Vergleich gegen das
PNG des Bestands läuft im Wirt über denselben `ChartRenderer.Jahresgang` mit denselben Reihen.

> Wie die Rasterprobe gehört sie **nicht zur Anwendung**: keine Projektmappe, keine CI, kein
> Werkzeug der Auslieferung. Bildschirmfotos und SVG-Dateien bleiben draußen.

## Aufruf

```bash
export DOTNET_ROOT=/root/.dotnet; export PATH=/root/.dotnet:$PATH

# 1. Konsole: Größe, gzip, Erzeugungszeit, byte-gleich (Rückgabe 0/1); --ziel schreibt die .svg
dotnet run --project Proben/SvgProbe -c Release -- --ziel /tmp/svgprobe

# 2. Der Wirt der Rasterprobe (er referenziert SvgProbe und stellt /svgprobe), Port 5371
dotnet build Proben/Rasterprobe/Wirt/Rasterprobe.Wirt.csproj -c Release
ASPNETCORE_URLS=http://127.0.0.1:5371 \
  dotnet Proben/Rasterprobe/Wirt/bin/Release/net10.0/Rasterprobe.Wirt.dll & echo $! > /tmp/wirt.pid

# 3. Die Messung (Chromium headless; playwright liegt global)
cd Proben/SvgProbe
node svgprobe.mjs --url http://127.0.0.1:5371 --fotos /tmp/svgfotos

kill $(cat /tmp/wirt.pid)          # den Wirt per PID beenden, nie pkill
```

Rückgabe `0` = die Sollwerte der gebündelten Fälle erfüllt, `1` = verfehlt, `2` = Aufruf-
oder Verbindungsfehler. Schalter: `--url`, `--fotos <ordner>`, `--nur <präfix>`;
`SVGPROBE_JSON=1` hängt alle Rohwerte als JSON an.

Die Seite nimmt ihre Gaben aus der Adresse: `/svgprobe?variante=roh|jedernte|gebuendelt|png&reihen=1…6`;
`/svgprobe/svg` und `/svgprobe/png` liefern dasselbe Bild als nackte Antwort für die netzfreie
Messung in der Seite.

## Die drei Pfadarten

| Variante | Punkte je Reihe | Was |
|---|---|---|
| `roh` | 8 760 | jede Stunde ein Punkt |
| `jedernte` | 1 252 | jeder n-te Wert, n = Stützstellen / Spalten = 7 — so zeichnet `ChartRenderer.Jahresgang` heute ins PNG; Spitzen zwischen zwei Stützstellen gehen verloren |
| `gebuendelt` | 2 308 | je Bildpunktspalte Minimum und Maximum in Indexreihenfolge; bei 1:1 vom rohen Bild nicht zu unterscheiden, keine Spitze geht verloren |

Die Zeichenfläche ist ein **inneres `<svg>` in Datenkoordinaten** (x = Stunde, y = 0…1 000)
mit eigener `viewBox` und `preserveAspectRatio="none"`; Zoom und Verschieben sind eine
Attributänderung, die Strichstärke bleibt über `vector-effect="non-scaling-stroke"`.

## Was gemessen wird

| | Größe | Sollwert (nur `gebuendelt`) |
|---|---|---|
| (a) | Erzeugung im Kern (Stopwatch, warm) und Zeit bis das Diagramm im Baum steht (Blazor Server, nur Anhalt) | — |
| (b) | Einsetzen des Markups netzfrei, 7×: synchron (Parsen, Stil, Layout), bis zum Bildaufbau, Hauptfaden je Vorgang (CDP `Performance.getMetrics`) | synchron ≤ 100 ms |
| (c) | 30 Schritte viewBox-Zoom auf der Zeitachse (Faktor 0,93, zusammen 8,8-fach) und 30 Schritte Verschieben: synchron, bis zum Bildaufbau, Hauptfaden je Schritt | bis Bildaufbau Median ≤ 20 ms (kein ausgelassener Rahmen bei 60 Hz), synchron ≤ 5 ms |
| (d) | 50 × `elementFromPoint` entlang Reihe 1; 10 000 Umrechnungen Zeiger → Stunde | — |
| (e) | Beschriftung ist Text (`innerText` findet „Reihe 1") | ja |
| (f) | Knoten im SVG, Zeichen in den Pfaden, JS-Heap | — |
| (g) | Fotos 1:1 und gezoomt | — |

## Ergebnis vom 20.09.2026 (DG-1)

Chromium headless (Playwright 1.56.1, 60 Hz), Linux, Release; Konsole und Browser.

| Variante | Reihen | Größe | gzip | Erzeugung | Einsetzen sync | Hauptfaden je Einsetzen | Zoomschritt sync / bis Bildaufbau (Median, Max) | Hauptfaden je Schritt |
|---|---|---|---|---|---|---|---|---|
| gebündelt | 3 | 75,7 KB | 33,3 KB | 2,5–2,8 ms | 1,5 ms | 2,6 ms | 0,1 / 16,7 ms (16,8) | 0,43 ms |
| gebündelt | 6 | 148,6 KB | 65,1 KB | 6,3–7,5 ms | 2,4 ms | 3,7 ms | 0,1 / 16,7 ms (16,8) | 0,49 ms |
| roh | 3 | 277,5 KB | 116,7 KB | 3,9–8,6 ms | 3,8 ms | 5,0 ms | 0,1 / 16,7 ms (16,9) | 0,55 ms |
| roh | 6 | 552,2 KB | 234,0 KB | 18–22 ms | 6,4 ms | 7,8 ms | 0,1 / 16,7 ms (50 — ein ausgelassener Rahmen in 60) | 1,38 ms |
| jeder n-te | 3 | 42,6 KB | 18,3 KB | 0,8–1,5 ms | 1,3 ms | 2,4 ms | 0 / 16,6 ms (16,9) | 0,40 ms |
| PNG (Bestand) | 3 | 131,6 KB | — | 54 ms | 0,4 ms | 1,1 ms | kein Zoom ohne Rundlauf | — |
| PNG (Bestand) | 6 | 196,8 KB | — | 55 ms | 0,4 ms | 1,1 ms | kein Zoom ohne Rundlauf | — |

Dazu: jede Variante zweimal byte-gleich; 56 bzw. 65 Knoten im SVG (24/27 `<text>`), JS-Heap
3,1–3,2 MB (PNG-Seite 4,3–4,7 MB); Text ist Text im SVG, nicht im PNG; `elementFromPoint`
trifft die 2 px dünne Linie nur in 1–4 von 50 Fällen (3,6–19 ms für 50 Aufrufe) — die
Zeigerzeile rechnet deshalb x → Stunde über die viewBox (10 000 Umrechnungen 1–2 ms) und
liest den Wert aus der Reihe. Rückgabe 0, alle 7 Fälle gemessen.

**Foto „1zu1"**: Titel, Legende mit drei Farbfeldern, gepunktetes Raster, Achsen 0…200 kW
und 0…12 Monate, drei Jahresgänge mit Tagesband und einzelnen Spitzen. **Foto „gezoomt"**:
rund 41 Tage, sauberer Tagesgang, die Spitzen als Nadeln, Linien scharf bei 2 px; die
Achsenbeschriftung wird in der Probe nicht nachgeführt (im Produkt zeichnet Blazor die Ticks
nach).

### Was nur am Gerät prüfbar bleibt

Die Probe läuft in Chromium; der Anwender in WebView2 (Windows, 125 % DPI) und WKWebView
(iPad, Kneifgeste). Beides ersetzt keine Abnahme am Gerät; die Achsen-Nachführung beim
Zoom und die Zeigerzeile sind Sache des Bausteins `DiagrammSvg` (Etappe E2) und hier nicht
gemessen.
