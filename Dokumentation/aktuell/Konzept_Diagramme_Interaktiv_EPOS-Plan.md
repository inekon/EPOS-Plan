# Konzept: Diagramme direkt in der Oberfläche statt als Bild (DG-1)

Stand 20.09.2026 — entschieden (DG-Q1…Q7, Abschnitt 8); **E0 umgesetzt** (Statuszeile #399),
**E1 umgesetzt** (#400–#402), **E2 umgesetzt** (#404), **E3 umgesetzt** (#406–#411, #413): jedes
Diagramm der Oberfläche ist ein `Zeichenmodell` im Baustein `DiagrammSvg`, `ChartBild` und der
Bildzoom per CSS-Transform sind entfallen. **E4** ist zur Hälfte umgesetzt — der Wortbericht
bettet jedes Diagramm als SVG mit PNG-Rückfall ein (DG‑E3‑8, #407/#410); offen sind die
gebündelten PNG-Linien und der gezoomte Ausschnitt im Bericht; offen bleibt außerdem die
Abnahme am Gerät **A-DG-1**. Prüfstände: [`Proben/SvgProbe/`](../../Proben/SvgProbe/LIESMICH.md), die Seite
`/diagrammsvg` im Rasterprobe-Wirt ([`Proben/Rasterprobe/`](../../Proben/Rasterprobe/LIESMICH.md)) und die
Hash-Messlatte in [`Proben/ChartProben/`](../../Proben/ChartProben/LIESMICH.md).

## 1 Die Frage

> „Die Diagramme sind bislang über Image. Gibt es eine sinnvolle Möglichkeit, die Charts
> direkt zu erstellen, ohne Image? Prüfe und schlage vor." (Anwender, 20.09.2026)

Gemeint ist: Das Diagramm soll in der Oberfläche als **Zeichnung** stehen, die der Browser
selbst hält — zoombar, verschiebbar, mit Werten am Mauszeiger und schaltbarer Legende —,
nicht als fertiges Pixelbild, das der Kern für jede Bedienung neu malt.

## 2 Ausgangslage, am Code gemessen

### 2.1 Der Renderer

`EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs`, **4 366 Zeilen**, SkiaSharp, ohne
Windows-API. Er liefert **PNG-Bytes**; dieselben Bytes gehen in den Bericht
(`WordBerichtGenerator.Bild(byte[] png, …)` → `ImagePart` PNG; dazu `ExcelBerichtGenerator`,
`PeakShavingBild`, `SpeicherBetriebsbild`, `BerichtsDaten`, `ZeitreihenExtraktor`) und auf den
Bildschirm.

| Größe | Wert |
|---|---|
| öffentliche Zeichenmethoden (`byte[]`) | **26**, zusammen 2 467 Zeilen (Kuchen, BalkenHorizontal, JahresverlaufWaerme, DauerlinieWaerme, StrombilanzMonate, Speicherverlauf, Speichertemperaturen, KapitalwertVerlauf, Kostenprofil, Jahresgang, Kennlinien, MonatsSaeulen, Stundenprofil, Jahresverlauf, GanglinieNormiert, ErzeugerStapel, Streuwolke, Ring ×2, MonatsStapel, Temperaturverlauf, Speicherbetrieb, Optimierungsraster, Schnittkurve, Stueckzahlkurve, Jahresprojektion) |
| private Helfer | **62**, zusammen 1 664 Zeilen; fünf davon liefern selbst `byte[]` (`StapelDiagramm`, `LinienDiagramm`, `MonatsBalken`, `Verlaufsbild`, `Png`) und tragen sechs der 26 Methoden |
| gemeinsam genutzte Helfer | `Start`/`Titel`/`Schrift`/`Text`/`Png` in 20 von 26; `Strich` 14, `Legende` 12, `Fuellung` 11, `TextHoehe` 11, `Leerhinweis` 9, `Linienzug` 8, `Ausschnitt` 6, `ZeichneLinie` 4, `Zugeschnitten` 4, `XAchseFenster` 3, `YRaster` 3 |
| Skalenrechnung | **fünf verschiedene**: `RundeStufe` (4 Methoden), `Nice` (3), `BedarfsSkala` (2), `Stufe` (1) und dreimal dieselbe „schöne Stufen"-Schleife im Rumpf (`Jahresgang`, `KapitalwertVerlauf`, `Kostenprofil`) |
| unmittelbare Skia-Aufrufe | `DrawLine` 69, `DrawRect` 21, `DrawCircle` 6, `DrawPath` 3, `DrawOval` 2, `DrawText` 1 (nur über `Text`); `MeasureText` 46; `CreateDash` 13 (gepunktete Raster); `ClipRect` 2; `Save`/`Restore` 1 — **kein** Shader, Blend-Modus, Bitmap, Drehung, Verlauf |
| Zeitausschnitt (`Achsenfenster`) | fünf Methoden (`Jahresverlauf`, `GanglinieNormiert`, `ErzeugerStapel`, `Temperaturverlauf`, `Speicherbetrieb`); `FensterAusBild` an 6 Stellen der Hüllen; KL-8 ergänzt `Jahresgang` (läuft) |
| Aufrufer | 36 Aufrufstellen in 6 Hüllen von `EPOS.UI.Daten` und 9 Hüllen der Windows-Schale (`Views/…/*Huelle.cs`) |
| Nachweis | `Proben/ChartProben`: **69 Bilder**, geprüft Maße, Farben, Determinismus (zweimal Rendern byte-gleich), heute 0 Verstöße; `EPOS.Kern.Tests/ChartRendererTests` |

Das Bild entsteht also aus einer **kleinen Primitivmenge** (Linie, Rechteck, Kreis/Ellipse,
Pfad, Text, Strichmuster, Zuschnitt) — genau die Menge, die SVG 1.1 ohne Umweg kennt. Was
Skia-spezifisch ist, sind zwei Dinge: die **Textvermessung** (`MeasureText`, 46 Stellen: die
Layouts sind metrikgetrieben) und die **Schriftkette** (`Calibri → Carlito → Liberation
Sans → …`). Beides bleibt beim Zeichenmodell im Kern; das SVG bekommt die gemessenen
Positionen und setzt Text zusätzlich mit `text-anchor`, damit eine abweichende Browserschrift
nichts verschiebt, was rechts- oder mittig ausgerichtet ist.

### 2.2 Die Oberfläche

| Baustein | Umfang | Rolle |
|---|---|---|
| `EPOS.UI/Standards/ChartBild.razor` | 100 Zeilen | einziger Weg für ein Renderer-Bild: `<img src="data:image/png;base64,…">` im Baustein `Diagramm`; 29 Razor-Dateien nutzen ihn |
| `EPOS.UI/Bausteine/Diagramm.razor` | 233 Zeilen | Rahmen mit **Bildzoom** (CSS-Transform im Browser) und **Datenzoom** (Rechteck → `Diagrammbereich` → Hülle → `Achsenfenster` → Kern zeichnet ein neues PNG) |
| `EPOS.UI/wwwroot/epos-diagramm.js` | 331 Zeilen | Rad, Kneifgeste, Ziehen, Doppelklick, Tasten, Rechteck; je Rahmen ein Zustand |
| `EPOS.UI/wwwroot/epos-verlauf.js` | 36 Zeilen | Bildlauf des Gesprächsverlaufs |
| `IJSRuntime` | 3 Komponenten (`Diagramm`, `Gespraechsverlauf`, `LizenzDialog`) | |
| Tests | `DiagrammTests` 18, `ChartBildTests` 12 (bunit) | |

**Jede Bedienung, die das Bild ändert, ist ein Rundlauf:** Rechteck aufziehen → Interop →
Hülle → Kern zeichnet (`Jahresgang` mit drei Reihen: **54 ms** auf dem Linux-Prüfrechner,
Release, warm) → PNG 132 KB → Base64 175 KB im Zeichenlauf → `<img>` dekodiert. Gemessen von
der Adresse bis zum Bild im Baum: rund 200 ms (Blazor Server, nur Anhalt). Der Bildzoom
vergrößert Bildpunkte; ab ×2 ist eine Jahresganglinie unscharf. Werte am Mauszeiger und eine
schaltbare Legende gibt es heute nicht (die Reihenwahl ist ein Schalterblock über dem Bild,
jeder Schalter ein Rundlauf).

### 2.3 Die Schalen

Windows: WebView2 (Chromium). iOS: WKWebView (Safari). Beide **ohne Netz zur Laufzeit**:
Jede Bibliothek müsste in `wwwroot` liegen und über `_content/EPOS.UI/…` kommen. Keine der
beiden `index.html` setzt eine Content-Security-Policy; Skripte laden die Komponenten über
`import()` aus dem Modul, keine Wirtsseite braucht eine `<script>`-Zeile. Der Bericht
entsteht im Kern **ohne Browser** (Word/PDF, Referenzlauf, CI) — er braucht weiterhin PNG.

### 2.4 Laufende Aufträge (Kontext, nicht bewertet)

**KL-8** gibt `Jahresgang` ein `Achsenfenster` (Klimadialog). **DL-3** erstellt das Inventar
aller Diagrammstellen (Datenzoom ja/nein). Beide vervollständigen den Datenzoom der Option A.

## 3 Die Optionen

### Option A — Bestand: PNG aus dem Kern, Interaktion über Rundlauf

**Aufbau.** Wie heute. KL-8/DL-3 bringen den Datenzoom an jede Zeitachse; Werte am Mauszeiger
und Legende ein/aus wären je ein weiterer Rundlauf (Zeiger → Interop → Hülle liest Reihe →
Text) bzw. ein Neuzeichnen.

**Berührungspunkte.** Keine neuen. **Kosten.** S je Bild für den Datenzoom (läuft).
**Was bleibt.** Ein Bild aus Pixeln: unscharf ab ×2 und auf 125-%-Bildschirmen, kein Text
für Suche und Sprachausgabe, jede Bedienung 100–250 ms, Legende und Zeigerwerte nur mit
weiteren Rundläufen. Ein Rundlauf je Radrast ist ausgeschlossen — genau deshalb liegt der
Bildzoom heute im Browser.

### Option B — SVG aus C#, plattformfrei, ohne Bibliothek

**Aufbau.** Der Kern liefert statt Pixeln ein **Zeichenmodell**: eine Liste von Primitiven
(Linie, Rechteck, Kreis, Pfad, Text, Gruppe mit Zuschnitt, Strichmuster) in Bildkoordinaten,
dazu die Zeichenfläche mit ihrem Datenfenster (x = Stunde, y = Wert). **Eine Layoutlogik**
(Skalen, Achsen, Legende, Titel, Leerhinweis, Zuschnitt), **zwei Ausgaben**: `SkiaMaler`
malt das Modell ins PNG (Bericht, Bestand), `SvgSchreiber` schreibt es als SVG-Text
(Bildschirm). Die Zeichenfläche steht im SVG als **inneres `<svg>` in Datenkoordinaten** mit
eigener `viewBox` und `preserveAspectRatio="none"`; Zoom und Verschieben sind dann **eine
Attributänderung** ohne Neuzeichnen und ohne Rundlauf, die Strichstärke bleibt über
`vector-effect="non-scaling-stroke"` stehen. Ein Baustein `DiagrammSvg` zeichnet das Modell
als Razor-Elemente (nicht als `MarkupString`: Blazor tauscht dann bei einer Legendenwahl nur
das eine Attribut, nicht 150 KB Markup). Das JS-Modul `epos-diagramm.js` bekommt neben dem
CSS-Transform einen **viewBox-Modus** (Rad, Kneifgeste, Ziehen, Rechteck, Tasten — dieselben
Handler, andere Wirkung); Werte am Mauszeiger: das Modul meldet die **Stunde** (x → viewBox →
Index, 10 000 Umrechnungen 1–2 ms), die Komponente liest die Werte aus dem Modell und
zeichnet die Zeigerzeile — ein Interop je Bildaufbau, kein Kernaufruf. Legende ein/aus ist
ein Attribut am Pfad.

**Berührungspunkte.** `ChartRenderer` (Zerlegung, siehe 2.1), `ChartBild`/`Diagramm`/
`epos-diagramm.js` (Erweiterung), 29 Aufrufstellen (je eine Zeile: Modell statt `byte[]`),
Hüllen (`Regionsansicht` und `Bildauftrag` führen das Modell), `ChartProben` (bleibt für PNG,
bekommt die SVG-Gegenprobe), bunit.

**Kosten.** L insgesamt, in Etappen S–M (Abschnitt 7). Die Zerlegung ist Handarbeit an 26
Methoden mit rund 100 unmittelbaren `Draw*`-Aufrufen und 46 Textvermessungen; die fünf
Skalenrechnungen werden zu einer. Sie muss **byte-gleich** bleiben — das ist der Preis und
zugleich die Abnahme.

**Geprüft am Prüfstand** (Abschnitt 5): 8 760 Stützstellen je Reihe sind für den Browser
kein Thema; das SVG ist **kleiner als das PNG**; Text bleibt Text; Zoom in einem Bildaufbau.

### Option C — JS-Diagrammbibliothek, lokal gebündelt

Bewertung aus Wissen und Lizenztext, **ohne Download** (Auftragsregel); die Größen sind
Erfahrungswerte der gebündelten Fassungen und vor einer Wahl nachzumessen.

| Kandidat | Lizenz | Bündel | Zeichnung | 8 760 Punkte × 6 Reihen | Stapel / Kuchen / Raster (Heatmap) | Zoom, Zeiger, Legende | Barrierefreiheit |
|---|---|---|---|---|---|---|---|
| **uPlot** | MIT | ≈ 50 KB | Canvas | sehr schnell (dafür gebaut) | Stapel über Bänder und eigene Pfade, **kein** Kuchen, kein Raster | Zoom (Ziehen) und Zeiger eingebaut, Legende einfach; Touch rudimentär | Canvas: kein Text |
| **Chart.js** | MIT | ≈ 200 KB (+ Zoom-Zusatz ≈ 30 KB, Touch über Hammer.js) | Canvas | brauchbar mit Dezimierung, sonst träge | Stapel, Kuchen, Balken ja; Raster nur als Zusatz | Zoom nur über Zusatzmodul | Canvas: kein Text |
| **Apache ECharts** | Apache-2.0 | ≈ 1 MB (Teilbündel 400–600 KB) | Canvas oder SVG | gut („large"-Modus) | alles, auch Heatmap | vollständig, Touch gut | Canvas/SVG: teils |
| **Plotly.js** | MIT | ≈ 3,5 MB (Grundbündel ≈ 1 MB) | SVG (WebGL-Spuren) | träge im SVG-Modus | alles | vollständig | teils |

**Aufbau.** Bibliothek in `EPOS.UI/wwwroot/`, je Diagramm ein JS-Aufruf mit den Reihen als
JSON (8 760 × 6 Werte ≈ 400 KB je Bild über die Interop-Brücke), Hausfarben und Achsen als
Optionen je Bibliothek-API, eine Anpassungsschicht je Diagrammart.

**Das Problem, das keine Bibliothek löst: der Bericht.** Word/PDF entstehen im Kern ohne
Browser (auch im Referenzlauf und in der CI); ein PNG-Export aus dem Browser scheidet dort
aus, und auf dem iPad gibt es keinen headless Browser. Also **zwei Wahrheiten** — Skia im
Bericht, Bibliothek am Bildschirm — mit je eigener Skala, Legende, Beschriftung und Farbe;
jede Änderung zweimal, jede Abweichung ein Befund („der Bericht zeigt etwas anderes als der
Bildschirm"). Dazu: Pflege der Bibliotheksfassung, Sicherheits- und Lizenzprüfung je Update,
Bündelgröße im App-Paket, ein Interop-Protokoll je Diagrammart, Determinismus nur über den
Browser prüfbar (Playwright statt ChartProben). **Kosten.** M für die Einführung (uPlot)
bis L (ECharts), **plus dauerhaft** die zweite Wahrheit.

### Option D — Mischformen

**D1: B nur für Zeitreihen, Balken/Kuchen/Kennlinien weiter PNG.** Das ist kein eigener Weg,
sondern **der Zwischenstand von B während des Rollouts** (Etappe E3): Zeitreihen zuerst,
weil dort Zoom und Zeigerwerte etwas bringen; Kuchen und Ring haben nichts zu zoomen (heute
schon `OhneZoom`) und gewinnen durch SVG nur Schärfe und Text. D1 als Endzustand hieße: zwei
Bildwege in der Oberfläche auf Dauer (`ChartBild` mit PNG **und** `DiagrammSvg`), was
tragbar ist, aber nichts spart — das Modell ist für Kuchen und Balken trivial (wenige
Knoten).

**D2: B nur für die Oberfläche, Bericht unverändert Skia ohne gemeinsames Modell.** Das
spart Etappe E1 (die byte-gleiche Zerlegung) und kostet dafür **eine zweite Layoutlogik**:
26 Diagrammarten × Skala, Achsen, Legende, Leerhinweis, Zuschnitt — rund 2 500 Zeilen
Layout ein zweites Mal, diesmal in C# für SVG. Jede Änderung am Bericht wandert von Hand in
die Oberfläche und umgekehrt; ChartProben deckt nur die eine Hälfte. Das ist dieselbe zweite
Wahrheit wie bei C, nur im eigenen Haus. **Nicht empfohlen.**

## 4 Bewertung

| Kriterium | A Bestand | B SVG aus C# | C JS-Bibliothek | D1 / D2 |
|---|---|---|---|---|
| **Eine Wahrheit** Bericht/Bildschirm | ja (dasselbe PNG) | **ja** (ein Modell, zwei Ausgaben) | **nein** (Skia vs. Bibliothek) | D1 ja (Zwischenstand von B) / D2 **nein** |
| **Plattformfreiheit** (Kern ohne UI, iOS ohne Netz) | ja | **ja** (Text aus dem Kern, kein Paket, kein JS-Fremdcode) | ja mit Bündelung; Lizenz- und Sicherheitspflege | wie B / wie B |
| **Bedienung** Zoom/Pan/Zeiger/Legende, Tastatur, Touch | Datenzoom als Rundlauf (100–250 ms), Bildzoom unscharf; Zeiger und Legende fehlen | **alles ohne Rundlauf**; Handler des Hauses (Rad, Kneifen, Ziehen, Tasten) bleiben; Zeiger über Stunde → Modell | sofort und vollständig (ECharts), einfach (uPlot); Touch je Bibliothek | wie B für Zeitreihen |
| **Leistung** 8 760 × 6 | Kern 54 ms je Bild, 197 KB PNG | gebündelt **149 KB**, Einsetzen 2,4 ms, Zoomschritt 0,1 ms sync, 16,7 ms bis Bildaufbau (60 Hz gehalten); roh 552 KB / 6,4 ms / ein ausgelassener Rahmen in 60 | Canvas schnell; Datenübergabe ≈ 400 KB JSON je Bild | wie B |
| **Determinismus, Prüfbarkeit** | ChartProben (69 Bilder), bunit auf `<img>` | **SVG ist Text**: byte-gleich prüfbar wie heute, Struktur mit bunit zählbar (Pfade, Texte, Attribute), PNG-Seite bleibt ChartProben | nur im Browser (Playwright) | wie B / nur die Oberflächenhälfte |
| **Aufwand** | S (läuft) | **L**, in Etappen S–M; Zerlegung von 26 Methoden byte-gleich | M–L Einführung **+ Dauerkosten** der zweiten Wahrheit | D1 = Teil von B; D2 M sofort, L über die Zeit |
| **Risiko** Bericht/Referenzbasis | keins | Bericht: E1 byte-gleich (Hash-Messlatte), Referenzbasis unberührt (Bilder sind keine Referenzwerte) | Bericht unberührt, aber Bildschirm weicht ab | D2: Drift zwischen Bericht und Bildschirm |
| **Wartung** | eine Stelle | eine Stelle (Modell), zwei dünne Schreiber; JS-Modul + ~150 Zeilen | Fremdfassungen, Anpassungsschicht je Art, zwei Stellen | zwei Stellen |
| **Barrierefreiheit, Schärfe** | Pixel; `alt` als einziger Text | Text bleibt Text (Suche, Sprachausgabe), verlustfrei skaliert, 125 % DPI scharf | Canvas: nichts; SVG-Modus: teils | wie B |

## 5 Prüfstand: Zahlen

`Proben/SvgProbe` (Konsole, ohne Abhängigkeit) und die Seite `/svgprobe` im Rasterprobe-Wirt,
gemessen mit `svgprobe.mjs` in Chromium headless (Playwright 1.56.1, 60 Hz), Linux, 20.09.2026.
Ein Jahresgang 1 304 × 440 wie `ChartRenderer.Jahresgang` (Zeichenfläche 1 154 × 240,
Legende, fünf Rasterstufen, Monatsachse), synthetische Reihen mit Tages- und Jahresgang und
einzelnen Spitzen **zwischen** den Stützstellen der Schrittweite 7.

Drei Pfadarten: **roh** (jede Stunde ein Punkt), **jeder n-te** (Schrittweite 7 — so
zeichnet der Renderer heute ins PNG; Spitzen gehen verloren), **gebündelt** (je
Bildpunktspalte Minimum und Maximum in Indexreihenfolge; bei 1:1 vom rohen Bild nicht zu
unterscheiden, keine Spitze geht verloren).

| Variante | Reihen | Punkte je Reihe | Größe | gzip | Erzeugung im Kern | Einsetzen (sync) | Hauptfaden je Einsetzen | Zoomschritt sync / bis Bildaufbau (Median, Max) | Hauptfaden je Zoomschritt |
|---|---|---|---|---|---|---|---|---|---|
| gebündelt | 3 | 2 308 | **75,7 KB** | 33,3 KB | 2,5–2,8 ms | 1,5 ms | 2,6 ms | 0,1 / 16,7 ms (16,8) | 0,43 ms |
| gebündelt | 6 | 2 308 | **148,6 KB** | 65,1 KB | 6,3–7,5 ms | 2,4 ms | 3,7 ms | 0,1 / 16,7 ms (16,8) | 0,49 ms |
| roh | 3 | 8 760 | 277,5 KB | 116,7 KB | 3,9–8,6 ms | 3,8 ms | 5,0 ms | 0,1 / 16,7 ms (16,9) | 0,55 ms |
| roh | 6 | 8 760 | 552,2 KB | 234,0 KB | 18–22 ms | 6,4 ms | 7,8 ms | 0,1 / 16,7 ms (**50**, ein ausgelassener Rahmen in 60) | 1,38 ms |
| jeder n-te | 3 | 1 252 | 42,6 KB | 18,3 KB | 0,8–1,5 ms | 1,3 ms | 2,4 ms | 0 / 16,6 ms (16,9) | 0,40 ms |
| **PNG (Bestand)** | 3 | — | **131,6 KB** | — | **54 ms** | 0,4 ms (Dekodieren) | 1,1 ms | kein Zoom ohne Rundlauf | — |
| **PNG (Bestand)** | 6 | — | **196,8 KB** | — | 55 ms | 0,4 ms | 1,1 ms | kein Zoom ohne Rundlauf | — |

Weitere Befunde:

- **Determinismus:** jede Variante zweimal byte-gleich (Konsole, 9 Fälle).
- **Baum:** 56 Knoten (3 Reihen) bzw. 65 (6 Reihen) im SVG, davon 24/27 `<text>`; JS-Heap
  3,1–3,2 MB (PNG-Seite 4,3–4,7 MB). Das SVG kostet den Baum nichts Nennenswertes — die Last
  liegt in den `d`-Attributen (74 KB bis 561 KB Zeichen), nicht in Knoten.
- **Text ist Text:** `innerText` findet „Reihe 1" im SVG, nicht im PNG.
- **Zoom:** 30 Schritte auf der Zeitachse (Faktor 0,93, zusammen 8,8-fach) und 30 Schritte
  Verschieben: **eine Attributänderung, 0,1 ms synchron, jeder Schritt in einem Bildaufbau
  (16,7 ms)**; der Hauptfaden arbeitet 0,4–1,4 ms je Schritt. Das Foto „gezoomt" zeigt rund
  41 Tage mit sauberem Tagesgang und den drei Spitzen, Linien scharf bei 2 px.
- **Werte am Mauszeiger:** Eine 2 px dünne Linie trifft `elementFromPoint` nur in 1–4 von 50
  Fällen — die Zeigerzeile darf sich nicht auf den Treffer verlassen, sondern rechnet
  **x → Stunde** über die viewBox (10 000 Umrechnungen 1–2 ms) und liest den Wert aus der
  Reihe. Genau so ist Option B beschrieben.
- **Blazor bis Diagramm im Baum:** 68–121 ms für SVG, 195–225 ms für PNG (Blazor Server über
  SignalR, nur Anhalt; in der WebView läuft derselbe Zeichenlauf über den Prozess).
- **Grenzen der Messung:** Chromium headless, nicht WebView2 und nicht WKWebView; die
  Achsenbeschriftung wird beim Zoom der Probe nicht nachgeführt (im Produkt zeichnet Blazor
  die Ticks nach, wenige Knoten); Rasterung auf den Rasterfäden ist in den Hauptfaden-Zahlen
  nicht enthalten, die gehaltenen 16,7 ms sind der Beleg, dass sie in den Rahmen passt.

**Folgerung für die Pfadart:** Bis 8 760 Stützstellen kann der Pfad **roh** gehen (der Zoom
zeigt dann ohne Nachladen die echte Stunde); ab Viertelstundenreihen (35 040) oder mehr als
drei Reihen ist **gebündelt** die Vorgabe — halb so groß wie das PNG, kein Verlust bei 1:1,
und ein Zoom über etwa das Vierfache lädt den Ausschnitt als rohen Pfad nach (ein Rundlauf
pro Zoomstufe, nicht pro Radrast).

## 6 Empfehlung

**Option B**, in Etappen, mit D1 als bewusstem Zwischenstand: ein Zeichenmodell im Kern,
PNG für den Bericht und SVG für den Bildschirm aus derselben Layoutlogik. Begründung:

1. Sie erfüllt als einzige das erste Kriterium — **Bericht und Bildschirm zeigen dieselbe
   Wahrheit** — und das ohne Fremdcode, ohne Netz, ohne Paket, auf beiden Schalen.
2. Der Prüfstand belegt, dass der Browser mit 8 760 Stützstellen je Reihe umgeht: das SVG ist
   **kleiner als das PNG**, Zoom und Verschieben laufen ohne Rundlauf im Bildaufbau, Text
   bleibt Text.
3. Der Renderer ist dafür **geeigneter, als seine Größe vermuten lässt**: eine kleine
   Primitivmenge, 62 Helfer, die schon heute die meisten Skia-Aufrufe kapseln, kein Shader
   und kein Bitmap-Trick. Die Arbeit ist die byte-gleiche Zerlegung, nicht eine neue Grafik.
4. Option C kauft Bedienung sofort und zahlt sie dauerhaft mit der zweiten Wahrheit; D2
   ebenso, nur im Haus.

Der heutige Baustein `Diagramm` bleibt bis zum Rollout gültig: **Bildzoom** und **Datenzoom
über Rundlauf** gelten für jedes Bild, das noch PNG ist; KL-8 ist zu Ende geführt, DL-3 mit
dem Entscheid DG-Q6 verworfen. Ihr
Kernanteil (`Achsenfenster`, `Zugeschnitten`, `XAchseFenster`) wird vom Zeichenmodell
**übernommen** — der Ausschnitt ist der Zustand des Zooms, den auch der Bericht drucken kann.
Ihr Oberflächenanteil (das `Diagrammbereich`-Feld je Reiter, der sechste Wert im
`Bildauftrag`, der Rundlauf) wird je umgestellter Diagrammart **abgelöst**: Das ist die
Doppelarbeit, rund drei Zeilen je Bildstelle, und sie ist klein gegen den Nutzen, den
Datenzoom bis dahin überall zu haben.

## 7 Etappen

| Etappe | Inhalt | Abnahme | Aufwand |
|---|---|---|---|
| **E0 Vorbereitung** — *umgesetzt (#399)* | KL-8 gemergt (DL-3 verworfen, DG-Q6). `ChartProben` hat die Schalter `--ablage <ordner>` und `--hashes <datei>`; die Liste [`Proben/ChartProben/Messlatte_2026-09-26.sha256`](../../Proben/ChartProben/LIESMICH.md) nennt **183 Bilder** (auch die der Gegen- und Versatzproben, die im Bestand nie geschrieben werden) und ist die **Messlatte**; ihre 91 Hashes aus E0, die Messlatte von E1, stehen unverändert darin. Sie gilt für die **Vorgabe-Palette**, die die Probe ausdrücklich setzt; die Gegenprobe der getauschten Palette (Abschnitt 9) steht deshalb nicht darin. Keine Bilder im Repository | ChartProben grün (72 Proben, 0 Verstöße), Hashliste liegt vor, zweiter Lauf byte-gleich | S |
| **E1 Zeichenmodell hinter dem `ChartRenderer`, ohne Bildänderung** — *umgesetzt (#400–#402): E1a Modell, Maler, Farbrollen, Skala und die 15 Zeitreihenbilder; E1b Säulen, Stapel, Kuchen, Ring, Balken; E1c Kennlinien, Streuwolke, Schnitt- und Stückzahlkurve, Optimierungsraster. **Alle 26 Methoden füllen ein Modell**; im Renderer steht kein Zeichenaufruf einer Grafikbibliothek mehr (Wächter `ZeichenmodellWacheTests`), die Übergangswege sind entfernt. **E2 ist frei.*** | `Allgemein/Bericht/Zeichnung/`: `Zeichenmodell` (Primitive: Linie, Rechteck, Kreis, Ellipse, Pfad, Text, Gruppe mit Zuschnitt, Strichmuster; Zeichenfläche mit Datenfenster) und `SkiaMaler`. Die 62 Helfer werden auf das Modell umgestellt (`Text`, `Strich`, `Fuellung`, `Linienzug`, `Vieleck`, `Kreissegment`, `Legende`, `YRaster`, `XAchse`, `XAchseFenster`, …), die fünf Skalenrechnungen zu **vier benannten Varianten** einer `Skala` (`Rund`, `Nice`, `Stufe`, `Bedarf` — nicht vereinheitlicht, weil jede Vereinfachung ein Bild verschöbe); die 26 Methoden füllen ein Modell und geben weiterhin `byte[]` zurück (`Png(modell)`). **Farben stehen im Befehl als `Farbrolle` plus Abwandlung, nicht als Zahl** (DG-Q7): aufgelöst wird beim Malen gegen `Farbpalette.Aktuell`, die Vorgabepalette trägt die Hausfarben Wert für Wert, und eine Rückwärtssuche gibt einer von außen durchgereichten Hausfarbe ihre Rolle zurück. Drei Aufträge: Zeitreihen (10 Methoden mit `Achsenfenster`, Jahresgang, Kostenprofil, Stundenprofil, Kapitalwert, Projektion), Säulen/Stapel/Kuchen/Ring/Balken (8), Kennlinien/Streuwolke/Schnitt/Stückzahl/Optimierungsraster (5) plus Bericht-Sonderfälle | **alle 91 Hashes gleich** (Text-Diff gegen die Messlatte leer), ChartProben grün, `ChartRendererTests` grün, Referenzlauf unnötig (kein Rechenweg) | **L** (3 × M) |
| **E2 SVG-Ausgabe und Baustein für EINE Art** — *umgesetzt (#404): Der Klimadialog zeigt seine zwei Jahresgänge als SVG; Zoom, Verschieben, Rechteck, Werte am Mauszeiger, Legendenschalter und Farbwahl laufen im Browser, ohne einen einzigen Rundlauf in den Kern, und kein PNG hat sich um ein Byte geändert. Der Rundlauf-Datenzoom (KL-8) ist an dieser Stelle ersatzlos entfallen.* | `SvgSchreiber` (Modell → Text; inneres `<svg>` in Datenkoordinaten, `text-anchor`, `vector-effect`), `ChartRenderer.JahresgangModell` (das Modell statt `byte[]`), Baustein `EPOS.UI/Bausteine/DiagrammSvg.razor` (Razor-Elemente aus dem Modell, Legende schaltbar, Zeigerzeile, Bereich → `Achsenfenster` **ohne** Kernaufruf), `epos-diagramm.js` mit viewBox-Modus (dieselben Handler, zusätzlich Nachladen ab dem Vierfachen). Erste Stelle: **Klimadialog** (`KlimadatenHuelle`, `Regionsansicht` führt zwei Modelle; die PNG-Fassung bleibt für den Bericht). `ChartProben` bekommt die SVG-Gegenprobe (byte-gleich, Knotenzahl) | bunit: Pfade, Texte, Legendenschalter, Zeigerzeile, Fall ohne Gaben; SvgProbe-Sollwerte; **Abnahme am Gerät** A-DG-1 (Windows 125 % DPI und iPad: Zoom, Kneifen, Zeigerzeile, Text scharf) | **M** |
| **E3 Rollout je Diagrammart** — *umgesetzt (#406–#411, #413): alle 26 Diagrammarten haben ein Modell (Kern (a) #406, (b) #408, (c) #409, die vier Berichtsbilder (d) #407), alle 41 Bildstellen der Oberfläche stehen im Baustein `DiagrammSvg` (Oberfläche (a) #411, (b)+(c) #413); `ChartBild`, `Diagramm`, `Diagrammbereich` und der CSS-Transform-Modus sind entfernt. Entscheide DG‑E3‑1 bis DG‑E3‑13 im Protokoll `DG_E3_Rollout_Protokoll.md`.* | in dieser Reihenfolge: (a) Zeitreihen der Ergebnisreiter (`Jahresverlauf`, `GanglinieNormiert`, `ErzeugerStapel`, `Temperaturverlauf`, `Speicherbetrieb`, `Kostenprofil`, `Stundenprofil`) — ersetzt den Rundlauf-Datenzoom dort; (b) `KapitalwertVerlauf`, `Jahresprojektion`, `Kennlinien`, `Streuwolke`, `Schnittkurve`, `Stueckzahlkurve`; (c) `MonatsSaeulen`, `MonatsStapel`, `StrombilanzMonate`, `BalkenHorizontal`, `Optimierungsraster`; (d) `Kuchen`, `Ring` (ohne Interaktion, nur Schärfe und Text). Je Auftrag eine Gruppe; `ChartBild` bleibt, bis die letzte PNG-Stelle umgestellt ist, dann entfällt der Bildzoom per CSS-Transform | je Gruppe bunit, ChartProben (PNG unverändert), Gerät | 4 × S–M |
| **E4 Bericht** — *zur Hälfte umgesetzt (#407, #410): Der Wortbericht bettet jedes Diagramm als SVG mit PNG-Rückfall ein (DG‑E3‑8, revidiert DG‑Q3); der Excelbericht bettet keine Grafik ein, einen eigenen PDF-Weg gibt es nicht — ein PDF entsteht aus Word und erbt dessen Bild.* | Zwei Wahlpunkte offen: die PNG-Linien auf **gebündelt** umstellen (heute jeder n-te; das ändert die Bilder bewusst, neue Hash-Messlatte mit dem Datum des Tages, ChartProben bleibt grün, weil sie Struktur prüft); im Bericht den **gezoomten Ausschnitt** drucken, wenn der Anwender ihn gesetzt hat (Achsenfenster ins Modell, Bildauftrag des Berichts) | Hash-Messlatte neu, Sichtprüfung eines Berichts | S |
| **E5 Farbrollen Teil 3** — *umgesetzt (#418)*: jede Reihe trägt eine Farbrolle (54 Rollen in sechs Gruppen), die Simulationsreiter zeigen die Hausfarben des Berichts, jeder Legendeneintrag hat einen Wähler; Wache gegen feste Farbwerte in Hüllen, Controllern und Schalen-Views | Messlatte unverändert (91/91), Prüfseite ohne Reihe ohne Wähler, DS‑13 | S |

Gesamt: **L** — rund neun Aufträge, keiner größer als M, jeder für sich abnehmbar; der
Bericht ändert sich bis E4 nicht um ein Byte. Ein iOS-Lauf ist nur für E2 begründet (Abnahme
der Bedienung am Gerät; die Schale selbst bleibt unberührt — er ist deshalb eine
Anwenderentscheidung, keine Pflicht).

## 8 Entscheide des Anwenders

| Nr. | Frage | Empfehlung | Entscheid |
|---|---|---|---|
| **DG-Q1** | Grundsatz: Option B (Zeichenmodell, SVG) angehen — oder beim Bestand A bleiben und nur den Datenzoom (KL-8/DL-3) vollenden? | **B.** A bleibt ein Pixelbild mit Rundlauf; C und D2 kaufen Bedienung mit einer zweiten Wahrheit | **B — Zeichenmodell, SVG aus C#** (20.09.2026) |
| **DG-Q2** | Reichweite: alle 26 Diagrammarten (B) oder nur die Zeitreihen (D1 als Endzustand)? | **Alle, in der Reihenfolge von E3**; Kuchen und Ring zuletzt und ohne Bedienung. Ein dauerhafter zweiter Bildweg spart nichts | **alle 26 Diagrammarten, in der Reihenfolge von E3** (20.09.2026) |
| **DG-Q3** | Bericht: PNG aus dem Modell (E4) — oder SVG auch in Word (OpenXML kennt SVG mit PNG-Rückfall seit Word 2016)? | **PNG bleibt.** SVG in Word ist eine spätere Option ohne Nutzen für den Ausdruck; der PDF-Weg braucht ohnehin das Bild | **Bericht bleibt PNG aus dem Modell** (20.09.2026) — **revidiert 20.09.2026 (DG‑E3‑8, Anwender: „alle Grafiken, soweit möglich, auf SVG“):** Der Wortbericht bettet SVG mit PNG-Rückfall ein (Word ab 2016 zeigt das SVG, ältere Leser das PNG); Excel und der Ausdruck aus Word behalten das PNG |
| **DG-Q4** | Interaktion: viewBox-Modus im vorhandenen JS-Modul (Rad, Kneifen, Ziehen, Rechteck, Zeigerstunde) und Blazor nur für Legende, Zeigerzeile und Bereichsmeldung — oder alles über Interop in Blazor? | **JS-Modul für alles, was je Bildaufbau anfällt** (dieselbe Regel, mit der der Bildzoom heute im Browser liegt); Blazor für alles, was Zustand ist | **JS-Modul für Interaktion je Bildaufbau, Blazor für Zustand** (20.09.2026) |
| **DG-Q5** | Pfadart: roh bis 8 760 Stützstellen, gebündelt darüber und ab vier Reihen, Nachladen ab dem Vierfachen — oder immer gebündelt (kleiner, Nachladen früher)? | **Regel wie in Abschnitt 5**; sie steht als eine Konstante im Modell und lässt sich am Gerät nachziehen | **Pfadregel als eine Konstante im Modell** (20.09.2026) |
| **DG-Q6** | Zeitpunkt: E0/E1 sofort nach dem Merge von KL-8 und DL-3 beginnen — oder E1 aufschieben, bis DL-3 den Datenzoom überall über den Rundlauf ausgerollt hat? | **Nach dem Merge beginnen**, DL-3 nicht über sein Inventar hinaus in den Rundlauf investieren: Jede weitere Rundlauf-Verdrahtung ist Doppelarbeit gegen E3 | **Beginn sofort nach dem Merge von KL-8**; **DL-3 verworfen** — der Rundlauf-Datenzoom der vier weiteren Stellen kommt mit E3 (20.09.2026) |
| **DG-Q7** | Farben: die Palette fest im Renderer — oder je Reihe und Größe eine benannte Farbrolle, die anwendungsweit tauschbar ist? | **Farbrollen.** Der Befehl trägt eine Rolle, nicht eine Zahl; die Palette löst sie beim Malen auf. Ein Tausch erreicht damit alle 26 Bilder und den Bericht, ohne dass eine Zeichenmethode angefasst wird | **benannte Farbrollen, anwendungsweit tauschbare Palette; der Bericht folgt** (20.09.2026) |
| **DG-Q8** | Legende: Bleibt eine Reihe mit fest gerechneter Farbe ohne Wähler („Sonstiges" der Prüfseite) — oder bekommt jede Reihe einen Wähler? | Jede Reihe bekommt einen Wähler; dafür trägt jede Reihe eine Farbrolle (vorhandene Rollen für geteilte Größen, neue Rollen mit den bisherigen Farben als Vorgabe für Heizstab, Warmwasser, Speicherladung, Überschuss, Erzeugung gesamt, Speicherfüllstand, BHKW‑Strom); die Simulationsreiter zeigen dann die Hausfarben des Berichts (§ 9, Teil 3) | **jede Reihe wird änderbar** (Anwenderwunsch 20.09.2026), **umgesetzt #418 (DG‑E5)**: 54 Rollen, Hülle, Kern-Bilder und Schalen-Hüllen auf Rollen, Wache gegen feste Farbwerte; die Farbänderung der Reiter war erläutert und angenommen |

Kein Wiki-Logbuch-Satz für E1 selbst: Der Umbau hinter dem Renderer ist unsichtbar. Der Satz
zur **Farbwahl** steht unter Abschnitt 9, der zur ersten umgestellten Maske entsteht mit E2
(Klimadialog).

## 9 Farbrollen

Aus DG-Q7 wird eine Bedienung: **Die Farbe einer Größe ist anwendungsweit einstellbar.**

**Rolle.** Ein Zeichenbefehl trägt keine Farbzahl, sondern einen `Farbton` — eine
`Farbrolle` (`WAERME_WP`, `STROM_PV`, `SPEICHER_3`, `RASTER`, …) und wahlweise eine Abwandlung
(andere Deckung, oder eine im Layout gerechnete Farbe mit ihrer Herkunftsrolle). Vierundfünfzig
Rollen in sechs Gruppen: Allgemein, Erzeuger und Bedarf, Varianten, Speicher, Profile und
Temperaturen, Rasterkarte.

**Palette.** `Farbpalette.Vorgabe` trägt die Hausfarben Wert für Wert — sie ist zugleich die
Messlatte der ChartProben, die ausdrücklich mit ihr rechnen. `Farbpalette.Aktuell` ist die
Palette, gegen die gemalt wird; `Farbpalette.Zuruecksetzen()` stellt die Vorgabe wieder her.
**Aufgelöst wird beim MALEN**, deshalb folgt jedes der 26 Bilder und der Bericht (Word und
PDF malen über denselben `SkiaMaler`), ohne dass eine Zeichenmethode angefasst wird.

**Einstellung.** Ein Schlüssel `DiagrammFarben` unter den Anwendungseinstellungen führt die
geänderten Rollen als kompakten Text `ROLLE=#RRGGBB;…`; **nur die Abweichungen** stehen darin,
leer heißt Hausfarben — eine später geänderte Hausfarbe erreicht damit jeden, der sie nicht
selbst gesetzt hat. Die **Deckung bleibt die der Hausfarbe**: Der Anwender wählt den Farbton,
die Durchsichtigkeit gehört zum Bildaufbau. Ein nicht lesbarer Eintrag wird **benannt
verworfen**, nie still. `Diagrammfarben.Uebernehmen()` speist die Palette — beim Programmstart
und nach dem Speichern der Einstellungen, ohne Neustart.

**Bedienung, Teil 1 (umgesetzt).** Administration › Einstellungen, Rubrik **Diagramme**: je
Rolle ein `Farbfeld` (Systemwähler, beschreibbares Hexfeld, Muster der Hausfarbe daneben), in
den sechs Gruppen; „Hausfarben" setzt alle zurück und speichert nicht, übernommen wird mit
„OK".

**Bedienung, Teil 2 (umgesetzt, DG-E2-1).** Die Legende eines SVG-Diagramms ist Bedienfläche:
Ein Klick auf den **Namen** einer Reihe blendet sie aus und wieder ein (der Eintrag bleibt
lesbar, nur gedämpft), ein Klick auf das **Farbfeld** daneben öffnet den Farbwähler unmittelbar
am Bild — Systemwähler, Hexfeld, Hausfarbenmuster und „Hausfarbe". Der Eintrag ist
fokussierbar; Eingabe und Leertaste schalten, Umschalt + Eingabe öffnet den Wähler. Eine Reihe
mit im Layout gerechneter Farbe führt ihre Herkunftsrolle, und ihr Farbfeld öffnet den Wähler
dieser Rolle. Ein Klick auf ein anderes Farbfeld
wechselt den Wähler mit einem Klick, ein zweiter Klick auf dasselbe schließt ihn; ein Klick ins
Bild oder daneben schließt ihn ebenso. Dasselbe Bild in neuer Instanz — ein Zeichenlauf des
Wirtes nach einer Farbwahl oder ein neuer Rechenlauf — behält Ausschnitt, ausgeblendete Reihen
und den offenen Wähler; erst ein anderes Bild setzt sie zurück.

**Bedienung, Teil 3 (umgesetzt #418, Anwenderentscheid DG‑Q8).** Jede Reihe trägt eine
Farbrolle, und jeder Legendeneintrag hat einen Wähler. Eine `Reihe` des Renderers nennt ihren
`Farbton` statt einer Farbzahl; die Simulationshülle, die Kern-Bilder (Lastspitzenkappung,
Speicherbetrieb, Flottenanzeige) und die Hüllen der Schale übergeben Rollen — wo es die Größe
schon gibt (Heizkessel, BHKW, Solar, Photovoltaik, Wärmepumpe, Bedarf, Rest, Netz) die vorhandene,
sonst eine der vierzehn neuen Rollen mit ihrer bisherigen Farbe als Vorgabe (Heizstab, Heizwärme,
Warmwasser, Prozesswärme, BHKW‑Strom, Überschuss, Erzeugung gesamt, Verbrauch gesamt,
Speicherladung, Speicherfüllstand, Strom aus Speicher, Netz ohne und mit Speicher, Sonnenwinkel).
Die Simulationsreiter zeigen damit dieselben Hausfarben wie der Bericht. Die Wache
`DiagrammfarbenWacheTests` hält Hüllen, Controller und Schalen-Views frei von festen Farbwerten;
die Prüfseite zeigt keine Reihe ohne Wähler. Feste Werte bleiben nur den Ringsegmenten der
Übersicht (keine Reihen, keine SVG-Legende).
Geschrieben wird über
`Diagrammfarben.Setze`/`Zuruecksetzen` in denselben Einstellungsschlüssel wie in der
Administration — **nur die Abweichungen**, und `Uebernehmen()` speist die Palette sofort. Die
PNG-Legende bleibt ein Bildausschnitt ohne Trefferfläche; die Bedienung gilt deshalb je Bild ab
dem Tag, an dem es auf SVG steht (E3).

**Wiki-Logbuch (Version 1.2.0.3):** „Die Farben der Diagramme lassen sich in den Einstellungen
je Größe ändern; der Bericht nimmt dieselben Farben." — dazu, mit E2: „Die beiden Diagramme der
Klimadaten lassen sich auf der Zeitachse vergrößern und verschieben; unter dem Bild stehen die
Werte am Mauszeiger, und über die Legende lassen sich Kurven ausblenden und ihre Farben ändern."
