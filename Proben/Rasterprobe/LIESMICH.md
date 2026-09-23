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
| J / K | 6 654 Stromspeicher **im Katalogdialog** (Seite `/katalogprobe`, Stromspeicher-Verwaltung), 1 088 × 624 und 400 × 624 — die Liste nimmt dort seit Stufe 1 der Neuordnung die Resthöhe; zusätzlich geprüft: Rollbehälter = Hülle, Zeile **46 px** (Stufe 2: die Zeile ist die Wahl) und die Tastatur (k): Ende wählt die letzte Zeile, sie steht gezeichnet im Bild, Pos1 zurück, die Liste behält den Fokus |
| L / M | wie A (freie Liste der Importmaske), 1 088 × 624 und 400 × 624 |

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

---

## Nachtrag vom 12.09.2026 (Auftrag #240) — die Hausregel für die Zellenpolsterung

**Was geändert wurde.** Die Hausregel `.epos-raster th, .epos-raster td { padding: 4px 8px }`
wog (0,1,1) und hat in einem QuickGrid-Raster deshalb **nie** gegolten — `#235` hat das
nebenbei gemessen (1,6 px statt 4 px), aber nicht behoben. `epos-ui.css` führt seither
zusätzlich

```css
table.epos-raster.quickgrid > tbody > tr > td { padding: 4px 8px; }            /* (0,2,4) */
table.epos-raster--bearbeitbar.quickgrid > tbody > tr > td { padding: 0 8px; } /* (0,2,4) */
```

Kein `!important`; die zweite Zeile hält die bearbeitbare Zeile eng, die sonst von der
ersten 4 px zurückbekäme.

**Messung vorher / nachher** (derselbe Wirt, Chromium headless, `node rasterprobe.mjs`):

| Größe | vorher | nachher |
|---|---|---|
| Zeilenhöhe der virtualisierten Fälle A–E, G, I (Maß gesetzt) | 53,0 px | **53,0 px** |
| Zeilenhöhe Fall F (119 Zeilen, **kein** gesetztes Maß) — die NATÜRLICHE Höhe | **48,188 px** | **53,0 px** |
| Fall H (Gegenprobe, Maß weggenommen) | 47,7 … 48,2 px, 366 Melder | **52,5 px, 402 Melder** |
| Rückgabe | 0 (9 von 9) | **0 (9 von 9)** |

**`ItemSize` bleibt 53** — und das ist kein Zufall, sondern der Beleg: Die natürliche Höhe
war 44 + 2 × 1,6 + 1 = 48,2 px und ist jetzt 44 + 2 × 4 + 1 = 53,0 px, also genau das Maß,
das `Raster.ZEILENHOEHE` seit #235 setzt. `height` an einer Tabellenzeile ist ein
Mindestmaß; erreicht werden darf es, unterschritten nicht. Fall F misst das ohne jedes
gesetzte Maß und ist deshalb der eigentliche Nachweis der Änderung.

**Die Gegenprobe H bleibt rot** — sie nimmt der Zeile ihr gesetztes Maß, und die restlichen
0,5 px Unterschied (52,5 gegen 53) reichen den zwei Sichtbarkeitsmeldern aus: 402 Meldungen
in drei Sekunden statt der erlaubten zwölf, 16 Platzhalterzeilen nach dem Rollen. Am
Prüfprogramm war nichts zu ändern.

---

### Was nur unter Windows prüfbar bleibt

Die Probe läuft unter Chromium; der Anwender läuft unter **WebView2** (derselbe
Blink-Unterbau, aber eigene Fassung) und mit **125 % Windows-DPI**, das WebView2 als
`zoomFactor` und nicht als `deviceScaleFactor` weiterreicht. Fall D nähert das an, ersetzt
aber keine Abnahme am Gerät. Ebenso nicht gemessen: der echte `KatalogImportDialog` samt
Dateiwahl und CEC-Netzabruf — die Probe stellt die `Katalogliste` mit dem echten Profil,
nicht den ganzen Wirt.

---

## Die zweite Probe: `katalogprobe.mjs` — der GANZE Katalogdialog (KL-5)

**Zweck.** `rasterprobe.mjs` misst *eine Liste*. `katalogprobe.mjs` misst, **wo die
Kästen liegen**: den ganzen Dialog aus `Katalograhmen` + `Katalogliste` — Listenblock,
Eingabeblock, Reiterleiste, Diagrammkasten, Fußleiste und jeden Knopf darin — und meldet
**jede Überschneidung als Verstoß**. Eine Überlagerung ist eine Aussage über Rechtecke;
bunit hat kein Layout und sieht sie grundsätzlich nicht.

**Der Anlass.** Im Dialog „Klimadaten" (1 180 × 780, nach einem TRY-Regionalimport mit
35 Regionen) malten Reiterleiste und Diagrammkasten über die Listenzeilen, der
Eingabeblock über die Fußleiste: Von „Löschen" war nur „…öschen" zu lesen, „Beenden"
halb verdeckt. Dasselbe Bild kam aus der „Stromverbraucher Verwaltung" — kein Fehler
einer Maske, sondern einer des Musters: **vierzehn Menüpunkte in sieben Komponenten**
hängen daran.

```bash
export DOTNET_ROOT=/root/.dotnet; export PATH=/root/.dotnet:$PATH
dotnet build Proben/Rasterprobe/Wirt/Rasterprobe.Wirt.csproj -c Release
ASPNETCORE_URLS=http://127.0.0.1:5299 \
  dotnet Proben/Rasterprobe/Wirt/bin/Release/net10.0/Rasterprobe.Wirt.dll &

cd Proben/Rasterprobe
node katalogprobe.mjs --url http://127.0.0.1:5299 --fotos /tmp/katalogfotos
```

Rückgabe `0` = keine Überlagerung, `1` = mindestens eine, `2` = Aufruf- oder
Verbindungsfehler. Schalter: `--url`, `--fotos`, `--nur <präfix>` wie oben; `--vorher`
setzt **allen** Fällen das Maß von vor KL-5 zurück (der Rahmen darf unter seinen Inhalt
schrumpfen und rollt nicht in sich) — der Lauf muss dann rot sein.

Die Seite des Wirtes nimmt ihre Gaben aus der Adresse:
`/katalogprobe?maske=klima|bedarf|modul|waermebedarf|solar|browser|waermepumpe&zeilen=<n>&bilder=1|0`.
Seit Stufe 1 der Neuordnung dazu `maske=stromganglinie|projekt-heizkessel`, `art=` (Ausprägung
von `browser`, `modul`, `bedarf`) und `voll=1` (jede Spalte belegt, in den Textlängen der
Testdatenbank — `Zeilenbau.Voll`).
Die Zeilen sind synthetisch, die **Maße** nicht: Das Diagramm kommt aus demselben
`ChartRenderer.Jahresgang` (1 304 × 440 px), den die Windows-Hülle ruft.

### Was gemessen wird

| | Größe | Sollwert |
|---|---|---|
| (a) | Überschneidung Eingabeblock / Reiterleiste / Diagramm **mit der Liste** | 0 px² |
| (b) | Überschneidung **Listenhülle mit dem Eingabeblock** | 0 px² |
| (c) | Überschneidung Eingabeblock / Diagramm / Liste **mit der Fußleiste** | 0 px² |
| (d) | Beginn der Fußleiste gegen das Ende des Rahmens | Fußleiste **unter** dem Rahmen |
| (e) | jeder Knopf der Fußleiste: `elementFromPoint` auf vier Ecken und Mitte | der Knopf selbst |
| (f) | jeder Knopf der Fußleiste: im Fenster und ungeschnitten | ja |
| (g) | Bildschirmfotos | — (Protokoll) |

Gemessen wird der **sichtbare** Kasten, nicht der gerechnete: Jedes Rechteck wird mit
denen aller rollenden Vorfahren geschnitten. `getBoundingClientRect` allein meldete sonst
jeden gerollten Dialog als Verstoß.

### Die Fälle

| Fall | Was |
|---|---|
| K1 / K2 | Klimadaten, 35 Regionen, 1 180 × 780 und 1 600 × 1 000 |
| K3 | Klimadaten **vor** dem Import (Platzhalter statt Bildern) |
| B1 / B2 | Stromverbraucher Verwaltung, dieselben zwei Größen |
| M1 / M2 | Photovoltaik-Module, dieselben zwei Größen |
| W1, S1, C1, P1 | Wärmebedarf, Solarganglinie, BHKW-Katalog, Wärmepumpen-Stamm (je 1 180 × 780) |
| G1 | **Gegenprobe**: Klimadaten mit dem Maß von vor KL-5 (seit Stufe 1 samt dem Raster von damals). Sie MUSS den Befund zeigen |
| N01a … N15b | **Neuordnung Stufe 1**: jede Verwaltung im Katalograhmen (vier Katalogbrowser, drei Modulkataloge, Wärmepumpe, Klimadaten, drei Bedarfe, drei Zeitreihen) mit vollen Zeilen, je 1 088 × 624 (`a`) und 400 × 624 (`b`) — Messung und Sollwerte im Abschnitt zu Stufe 1 unten |
| G2 | **Gegenprobe zu Stufe 1**: Heizkessel mit den Regeln von vor Stufe 1. Sie MUSS Rollbereich-in-Rollbereich und Querüberlauf zeigen |
| P2a / P2b | Nachbar: der Heizkessel-**Projektdialog** (erbt die Katalogliste); die Liste darf nicht zusammenfallen, bei 1 088 px nicht quer rollen |

### Ergebnis vom 19.09.2026 (Auftrag KL-5)

**Vorher** (`node katalogprobe.mjs --vorher`, Rückgabe 1 — 8 von 11 Fällen rot):

| Größe | K1 (Klimadaten, 1 180 × 780) |
|---|---|
| Dialoghöhe / Inhalt | 780 px / **1 475 px** |
| Rahmen | auf **601,8 px** gestaucht, Inhalt 1 377 px |
| Rasterreihen | **295,906 px \| 295,906 px** — gleich groß, nicht nach Inhalt |
| Listenblock | Reihe 295,9 px, Hülle 457,6 px → **296 284 px²** über den Feldern darunter |
| Eingabeblock | Reihe 295,9 px, Inhalt **1 071 px** → malt bis y = 876,8 |
| Fußleiste | y = 720 … 764 → **49 829 px²** vom Diagramm überdeckt |
| „Daten einlesen", „Löschen", „Beenden" | von `img.epos-chartbild` verdeckt |

Derselbe Mechanismus in den anderen Masken, dort allein über die **Listenhülle**: Sie
bleibt in ihrer Höchsthöhe stehen und ragt aus dem gestauchten Listenblock heraus —
B1 186 698 px², M1 280 105 px², C1 280 105 px², S1 174 076 px², W1 145 847 px² über die
Felder darunter. Das ist das zweite Anwenderbild („Stromverbraucher Verwaltung").

**Nachher** (`node katalogprobe.mjs`, Rückgabe 0 — 12 von 12 Fällen erfüllt):

| Größe | K1 | K2 |
|---|---|---|
| Rasterreihen | **564,594 px \| 1 070,8 px** (nach Inhalt) | 564,594 \| 1 101,84 |
| Listenhülle | **457,6 px** = Höchstmaß, sieben Zeilen | 457,6 px |
| Rahmen | 601,8 px sichtbar, rollt in sich (1 645 px) | 821,8 px von 1 676 px |
| Fußleiste | y = 720 … 764, **frei** | y = 940 … 984, **frei** |
| Dialog-Rollhöhe | **780 px** = Fensterhöhe (die Maske rollt nicht mehr) | 1 000 px |

### Die Ursache in zwei Sätzen

`.epos-katalog-fuellend` gab dem Rahmen `flex: 1 1 auto` **und** `min-height: 0`, er durfte
also unter seinen Inhalt schrumpfen; weil seine zwei Kinder ihrerseits `min-height: 0`
trugen, war die Mindestgröße einer `auto`-Rasterreihe null, und Chromium verteilte die
gestauchte Höhe zu **gleichen Teilen** auf beide Reihen statt nach Inhalt. Die zweite
Reihe war damit 775 px zu kurz, und ihr Inhalt zeichnete einfach darüber hinaus — quer
über die Liste und über die Fußleiste.

### Der Fix

Zwei Zeilen in `epos-ui.css`, beide am gemeinsamen Ort und keine je Dialog: Die zwei
Kinder des Rasterpaares bekommen ihre selbsttätige Mindestgröße zurück
(`min-height: auto`), damit jede Reihe ihre Inhaltshöhe behält; und der Rahmen rollt in
sich (`overflow: auto`), statt die Maske länger zu machen — sonst stünde „Beenden" 850 px
unter dem Fensterrand und wäre so unerreichbar wie vorher. Keine Positionierung, keine
feste Pixelhöhe. Die zwei Masken, die `epos-katalog-fuellend` auf eine bloße Liste setzen
(`GesetzeskatalogDialog`, `WaermepumpenKatalogDialog`), haben keine zweite Reihe und
bleiben unberührt; die neun Fälle von `rasterprobe.mjs` bleiben grün.

### Der Wirt trägt auch die SVG-Probe (DG-1)

Die Seite `/svgprobe` und die Antworten `/svgprobe/svg` und `/svgprobe/png` gehören zur
[`SvgProbe`](../SvgProbe/LIESMICH.md) (Konzept Diagramme direkt in der Oberfläche); der Wirt
referenziert dafür `Proben/SvgProbe/SvgProbe.csproj`. Gemessen wird sie mit
`Proben/SvgProbe/svgprobe.mjs`, üblich auf Port 5371.

### Nachtrag (DL-2b) — ein Vorfahr ist keine Überdeckung

Prüfung (e) setzt ihre Punkte **2 px** von den vier Ecken des Knopfes. Der Knopf des
Hauses trägt `border-radius: 6px` (`--epos-ecke`), und Chromium schnappt seinen Kasten
für die Trefferprüfung auf ganze Bildpunkte: Je nach Bruchteil der Knopfbreite liegt der
Eckpunkt damit um Bruchteile eines Bildpunktes **neben** der runden Ecke. Gemessen an der
Fußleiste der Wärmepumpen-Verwaltung (Fall P1): rechter Rand 262,484 px, geschnappt 262,
Abstand des Punktes zum Bogenmittelpunkt 6,009 statt höchstens 6 — `elementFromPoint`
meldete dort die **Leiste**, also den Vorfahren des Knopfes.

Ein Vorfahr kann sein eigenes Kind nicht überdecken; er zeichnet darunter. Der Punkt liegt
schlicht außerhalb der runden Ecke, und das ist kein Befund. `katalogprobe.mjs` lässt
deshalb auch einen Treffer gelten, der den Knopf **enthält** (`oben.contains(b)`). Eine
echte Überdeckung kommt immer aus einem anderen Zweig des Baumes; die Gegenprobe G1 meldet
ihre drei verdeckten Knöpfe unverändert über `img.epos-chartbild`.

---

## Neuordnung der Administrationsdialoge, Stufe 1 (V1, V2, V7)

Konzept `Dokumentation/aktuell/Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`,
Abschnitt 7: Abnahme je Stufe mit den Fällen **1 088 × 624** (das Fenstermaß des Anwenders,
85 % × 90 % von 1 920 × 1 040 bei 150 %) und **400 × 624**.

**Aufruf unter Windows ohne npm.** Das NuGet-Paket `Microsoft.Playwright` bringt Node und die
Bibliothek mit; Fassung 1.58.0 passt zum Browser `chromium-1208` unter
`%LOCALAPPDATA%\ms-playwright`. Die Probe findet die Bibliothek über `NODE_PATH`, wenn dort ein
Ordner `playwright` liegt (z. B. eine Verzeichnisverbindung auf
`%USERPROFILE%\.nuget\packages\microsoft.playwright\1.58.0\.playwright\package`):

```bash
NODE_PATH=<ordner mit playwright> \
  ~/.nuget/packages/microsoft.playwright/1.58.0/.playwright/node/win32_x64/node.exe \
  katalogprobe.mjs --url http://127.0.0.1:5299 --nur N
```

**Was die Fälle N zusätzlich messen** (Funktion `STUFE1`, Sollwerte in `pruefe`):

| | Größe | Sollwert |
|---|---|---|
| (s1) | jedes Element, das WIRKLICH rollt (overflow auto/scroll und mehr Inhalt als Platz), und ob eines im anderen liegt | keines im anderen |
| (s2) | rollt die Maske (`.epos-katalog-dialog`)? | nein |
| (s3) | Querüberlauf der Listenhülle, und welche Spalte jenseits ihrer Innenbreite liegt | ≤ 1 px |
| (s4) | Höhe der ersten Datenzeile | 53 px (`ItemSize`) |
| (s5) | ein gekürzter Bezeichner trägt seinen vollen Namen im Kurztext | ja |

Die KL-5-Prüfungen (Überlagerung, Fußleiste im Fenster, Knöpfe frei) laufen in jedem Fall mit.

**Ergebnis vom 23.09.2026** (Chromium headless, Playwright 1.58.0):

| | vorher (Bestand) | nachher |
|---|---|---|
| Rollbereiche ineinander | 27 von 28 Fällen: die Liste im rollenden Rahmen (Wärmepumpe dazu das Reiterblatt) | 0 von 30 |
| Heizkessel 1 088 × 624 | Rahmen 474 px sichtbar von 1 261, Liste 367 von 456 px sichtbar, Eingabeblock 0 px sichtbar; Tabelle 1 288 px in 1 054 px Hülle (Bezeichner 487 px, Hersteller 251 px; P_th, η, Brennwert jenseits) | Liste 209 px und Eingabeblock 160 px je für sich rollend, alle sieben Spalten in 1 054 px, 0 px quer |
| größter Querüberlauf 1 088 × 624 | Wärmepumpe 1 269 px | 0 px (drei Spalten mit Rang weichen) |
| größter Querüberlauf 400 × 624 | Wärmepumpe 1 957 px | 0 px (Bezeichner und Hauptkennwert bleiben) |
| Fußleiste | im Fenster, frei | im Fenster, frei (400 px: zweizeilig) |
| Zeilenhöhe | 53 px | 53 px |
| virtualisiert im Dialog (J, K) | — | Rollbehälter = Hülle (209 / 118 px), Zeile 53 px, 4 Melder nach dem Rollen |
| Rückgabe | Katalogprobe 1, Rasterprobe — | Katalogprobe 0 (45 Fälle), Rasterprobe 0 (13 Fälle) |

## Neuordnung der Administrationsdialoge, Stufe 2 (V4, V10, V11)

In den Verwaltungen ist die **Zeile die Wahl** (`Katalogliste.ZeileIstWahl`): keine Wahlspalte,
jede Zelle eine Klickfläche von 45 px, mit der Trennlinie **46 px** Zeilenmaß — dieselbe Zahl als
`ItemSize` und `--epos-rasterzeile`. Die Liste ist ein Tabulatorhalt; `epos-katalogliste.js` hält
Pfeil hoch/runter, Pos1 und Ende vom Rollen ab (nur wenn die Liste selbst den Fokus hat) und rollt
die gewählte Zeile ins Bild. Ein Auslieferungssatz trägt das Schloss hinter dem Namen.

**Was die Fälle N zusätzlich messen** (Funktionen `STUFE2` und `tastenprobe`):

| | Größe | Sollwert |
|---|---|---|
| (t1) | Höhe jeder gezeichneten Datenzeile (bis zwölf) | 46 px — keine bricht um |
| (t2) | Wahlspalte (`th.epos-spalte-wahl`, `.epos-anlagenwahl`) | keine |
| (t3) | Tabulatorhalt an der Hülle, genau eine Zeile `epos-zeile--gewaehlt` | ja |
| (t4) | linker Balken der Fokuszeile an ihrer ersten SICHTBAREN Zelle und nur dort | ja — auch wenn die erste Spalte weicht (Wärmepumpe, 400 px: `epos-balken-ab-N`) |
| (t5) | Schloss des Auslieferungssatzes ganz in seiner Zelle, sichtbar | ja, 16 × 16 px |
| (t6) | Tastatur: Fokus auf die Liste, Ende / Pos1 / zweimal Pfeil runter | richtige Zeile gewählt, ganz im sichtbaren Teil unter dem Kopf, Fokus bleibt, Pos1 rollt auf 0 |

Die Probe wählt die erste Zeile über die Klickfläche des **Namens** (`.epos-zeilenzelle--name`):
Die erste Zelle kann in einer schmalen Liste weichen.

**Ergebnis vom 23.09.2026** (Chromium headless, Playwright 1.58.0):

| | Stufe 1 | Stufe 2 |
|---|---|---|
| Zeilenhöhe der Verwaltungen (N01 … N15, beide Größen) | 53 px | **46 px** in jeder Zeile, keine Umbrüche |
| Querüberlauf 1 088 / 400 px | 0 px | 0 px |
| Rollbereiche ineinander | 0 | 0 |
| Fußleiste (mit „Duplizieren…" und „Beenden") | im Fenster, frei | im Fenster, frei |
| Schloss | — | 16 × 16 px, ganz in der Namenszelle |
| Tastatur (30 Fälle N) | — | Ende / Pos1 / Pfeil runter wählen richtig, Zeile im Bild, Fokus bleibt |
| virtualisiert im Dialog (J, K) | Zeile 53 px | Zeile **46 px**, Platzhalter 46 px, 4 Melder nach dem Rollen; Ende → Zeile 6 653 gezeichnet im Bild, 4 Melder in 3 s, Pos1 → Rollstand 0 |
| Nachbar Heizkessel-Projektdialog (P2a/P2b) | Wahlspalte, 53 px | unverändert: Wahlspalte, quer 0 px bei 1 088 |
| Rückgabe | Katalogprobe 0 (45), Rasterprobe 0 (13) | **Katalogprobe 0 (45), Rasterprobe 0 (13)** |
