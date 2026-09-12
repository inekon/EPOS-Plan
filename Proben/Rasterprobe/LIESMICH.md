# Rasterprobe — die virtualisierte Katalogliste im echten Browser

**Zweck.** `EPOS.UI/Bausteine/Katalogliste.razor` (über `EPOS.UI/Standards/Raster.razor`,
QuickGrid 10.0.11 mit `Virtualize`) so zeigen, wie der Stromspeicherimport sie zeigt — und
messen, was `bunit` grundsätzlich nicht messen kann: **Layout, Stilkaskade und die
`IntersectionObserver` von `Virtualize`**.

Das ist keine akademische Lücke. Zweimal wurde das gemeldete Flackern der Auswahlliste
ohne Browser bearbeitet (**W13‑B‑6**, 09.09.2026 und **#212**, 11.09.2026), zweimal blieb
es. **#235** (12.09.2026) hat es im Chromium gemessen und in einem Zug gefunden: Die
Rechnung von #212 („44 px Berührungsziel + 2 × 4 px Zellenpolsterung + 1 px Trennlinie =
53 px") las die Polsterung aus dem **Hausstilblatt**, während QuickGrids eigenes
Stilblatt sie überschreibt — die Zeile war **48,2 px** hoch, die **Platzhalterzeile nur
21,9 px**. `bunit` hat keinen Kaskadenrechner; die Wache zu #212 prüfte die falsche
Rechnung und war grün.

> Die Probe gehört **nicht zur Anwendung**: Sie steht in keiner Projektmappe
> (`WP-Plan.sln`, `WP-Plan.Kern.slnf`) und wird von keiner CI gebaut — wie der
> Python-Referenzkern unter `Projekte/Speichersimulation/code/` ist sie Nachweis, kein
> Werkzeug der Auslieferung. Bildschirmfotos und `node_modules` bleiben draußen
> (`.gitignore`).

---

## Aufruf

```bash
export DOTNET_ROOT=/root/.dotnet; export PATH=/root/.dotnet:$PATH

# 1. Der Wirt (Blazor Server, eine Seite, kein Zustand)
dotnet build Proben/Rasterprobe/Wirt/Rasterprobe.Wirt.csproj -c Release
ASPNETCORE_URLS=http://127.0.0.1:5299 \
  dotnet Proben/Rasterprobe/Wirt/bin/Release/net10.0/Rasterprobe.Wirt.dll &

# 2. Die Messung (Chromium headless; playwright und der Browser liegen global)
cd Proben/Rasterprobe
node rasterprobe.mjs --url http://127.0.0.1:5299 --fotos /tmp/rasterfotos
```

Rückgabe `0` = alle Sollwerte erfüllt, `1` = mindestens einer verfehlt, `2` = Aufruf- oder
Verbindungsfehler.

| Schalter | Wirkung |
|---|---|
| `--url <adresse>` | Wurzel des Wirtes (Vorgabe `http://127.0.0.1:5299`) |
| `--fotos <ordner>` | Bildschirmfotos zu t = 0,5 s, 5 s, nach dem Rollen und nach dem Zurückrollen |
| `--nur <präfix>` | nur die Fälle, deren Name so beginnt (z. B. `--nur B`) |
| `--entpinnt` | **nimmt ALLEN Fällen das gesetzte Zeilenmaß wieder weg** — der Stand VOR dem Fix von #235, aus demselben Programm gemessen. Der Lauf muss dann rot sein |
| `RASTERPROBE_JSON=1` | hängt alle Rohwerte als JSON an |

Die Seite des Wirtes nimmt ihre Gaben aus der Adresse:
`/probe?modus=sofort|laden&zeilen=<n>&takt=<n>` — `laden` ahmt „CEC-Liste abrufen" nach
(zehn Fortschrittsmeldungen, dann die volle Liste), `takt` legt nach dem Laden zusätzliche
Zeichenläufe je Sekunde darauf.

---

## Was gemessen wird

| | Größe | Sollwert |
|---|---|---|
| (a) | Umschaltungen der QuickGrid-Klasse `loading` nach dem Laden | 0 in 5 s |
| (b) | Platzhalterzeilen (`td.grid-cell-placeholder`) und echte Zeilen zu t = 0,5 / 1 / 2 / 5 s | 0 Platzhalter, ≥ 1 echte |
| (c) | gemessene Höhe der `tr` gegen `ItemSize` | gleich |
| (d) | Höhen der zwei `Virtualize`-Abstandshalter, Rollhöhe des Behälters | — (Protokoll) |
| (e) | welches Element `Virtualize` als Rollbehälter findet (Nachbau von `findClosestScrollContainer`) | `.epos-raster-huelle--hoch` |
| (f) | Bildschirmfotos | — |
| (g) | nach einem Rollen um 2 000 px: echte Zeilen, Platzhalter nach 3 s Ruhe | echte Zeilen ≤ 300 ms, danach 0 Platzhalter |
| (i) | **Rückmeldungen der Sichtbarkeitsmelder in den 3 s nach dem Rollen** | ≤ 12 |

(i) ist der schärfste Wert: Er zeigt den Fehler unmittelbar. Streiten sich die zwei Melder,
melden sie im Takt des Bildaufbaus.

## Die Fälle

| Fall | Was |
|---|---|
| A | 6 654 Zeilen, sofort da, 1 300 × 900 |
| B | 6 654 Zeilen über den Ladeweg („CEC-Liste abrufen"), 1 300 × 900 |
| C | wie B, Fenster 1 300 × 700 |
| D | wie B, `deviceScaleFactor` 1,25 (der Anwender läuft mit 125 % DPI) |
| E | wie B, dazu 10 zusätzliche Zeichenläufe je Sekunde nach dem Laden |
| F | **119 Zeilen — unter der Virtualisierungsschwelle**, Gegenprobe |
| G | 20 746 Zeilen (die CEC-Modulliste), Ladeweg |
| I | 6 654 Zeilen, danach im Suchfeld gefiltert (→ 444 Zeilen, weiter virtualisiert) |
| H | **Gegenprobe zum Fix**: dieselbe Seite, das gesetzte Zeilenmaß per Stilblatt wieder weggenommen. Sie MUSS die Sollwerte verfehlen — sonst belegt der Fix nichts |

---

## Ergebnis vom 12.09.2026 (Auftrag #235)

Chromium headless (Playwright 1.56.1), Blazor Server, 6 654 synthetische Sätze mit dem
echten Listenprofil des Stromspeicherimports (acht Spalten + Wahlspalte).

### Vorher (`node rasterprobe.mjs --entpinnt`, Rückgabe 1 — 7 von 9 Fällen rot)

| Größe | A / B / C / D / E / G / I |
|---|---|
| Zeilenhöhe gemessen | **47,7 … 48,2 px** gegen `ItemSize` **53** |
| Platzhalterzeile | **21,9 px** |
| Platzhalter nach dem Rollen (3 s Ruhe) | **16**, echte Zeilen **0** — dauerhaft |
| Sichtbarkeitsmelder in 3 s nach dem Rollen | **366 … 374** (≈ 123/s, also einer je Bildaufbau) |
| Abstandshalter dabei | Sprung alle 33 ms zwischen **1 713 / 377 281 px** und **2 284 / 376 710 px** |
| `loading`-Umschaltungen | 0 (die Klasse steht im virtualisierten Zweig nie) |
| Fall F (119 Zeilen) | grün — nicht virtualisiert, also kein Streit |

Das Bild dazu (`A_..._gerollt.png`) ist das Bildschirmfoto des Anwenders: Platzhalterzeilen
mit „…" in jeder Zelle, darüber und darunter leere Rasterfläche, Trefferzeile und
Statuszeile richtig.

### Nachher (`node rasterprobe.mjs`, Rückgabe 0 — 9 von 9 Fällen erfüllt)

| Größe | A / B / C / D / E / G / I |
|---|---|
| Zeilenhöhe gemessen | **53,0 px** = `ItemSize` **53** |
| Platzhalter zu t = 5 s | **0** |
| nach dem Rollen um 2 000 px | echte Zeilen nach **115 … 139 ms**, danach **0** Platzhalter / 16 echte |
| Sichtbarkeitsmelder in 3 s nach dem Rollen | **4** (statt 370) |
| Sichtbarkeitsmelder beim Aufbau | 3 (6 im Filterfall I) |
| `loading`-Umschaltungen nach dem Laden | 0 |
| Fall H (Gegenprobe, Maß wieder weggenommen) | zeigt den Fehler: 16 Platzhalter, **372** Meldungen |

### Die Ursache in zwei Sätzen

`Virtualize` misst nicht, es rechnet: Es teilt die Höhe des Rollbehälters durch `ItemSize`,
setzt danach die Höhen seiner zwei Abstandshalter und lässt je einen Sichtbarkeitsmelder
darauf laufen — weicht das Maß von dem ab, was wirklich im Baum steht, kommen die beiden auf
verschiedene Anfangszeilen und schieben das Fenster endlos gegeneinander (gemessen alle
33 ms). Jeder Sprung stellt QuickGrids Datenanforderung neu an, und die fällt hinter dessen
100‑ms‑Entprellung: Bei 33 ms Takt wird jede abgebrochen, bevor sie fertig wird — die Liste
bleibt in ihren Platzhalterzeilen stehen, und die sind mit 21,9 px so viel kürzer als die
echten, dass sie den Streit selbst am Leben halten.

### Der Fix

Das Maß wird nicht zum dritten Mal geraten, sondern **gesetzt**: Dieselbe Zahl geht als
`ItemSize` an `Virtualize` und als `--epos-rasterzeile` an die Hülle
(`Raster.razor`, `Hoehenstil`), und `epos-ui.css` gibt sie **beiden** Zeilenarten des
virtualisierten Rasters. Wert und Baum können nicht mehr auseinanderlaufen. Die
Virtualisierung bleibt vollständig erhalten; ein Rückfall auf `Pagination` war nicht nötig.

### Was nur unter Windows prüfbar bleibt

Die Probe läuft unter Chromium; der Anwender läuft unter **WebView2** (derselbe
Blink-Unterbau, aber eigene Fassung) und mit **125 % Windows-DPI**, das WebView2 als
`zoomFactor` und nicht als `deviceScaleFactor` weiterreicht. Fall D nähert das an, ersetzt
aber keine Abnahme am Gerät. Ebenso nicht gemessen: der echte `KatalogImportDialog` samt
Dateiwahl und CEC-Netzabruf — die Probe stellt die `Katalogliste` mit dem echten Profil,
nicht den ganzen Wirt.
