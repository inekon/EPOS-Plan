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
| (l) | nur Fälle mit `frei: true` (ein `Raster` ohne eigene Höchsthöhe, `Begrenzt="false"`): Abstandshalter, Sichtbarkeitsmelder, gezeichnete Zeilen, Höhe jeder Zeile, ob die Hülle selbst senkrecht rollt, welcher Vorfahr wirklich rollt, ob Seite oder Überlagerung quer rollen | 0 Abstandshalter, 0 Melder, alle Zeilen gleich hoch, Hülle rollt nicht senkrecht, Seite und Überlagerung quer 0 px (quer rollt allein die Hülle; mit `querMax` auch sie höchstens so weit) |

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
| T1 / T2 | 6 654 Nutzungsarten im **Katalog der Brauchwasser-Nutzungsarten** (Seite `/katalogprobe?maske=tww`), 1 088 × 624 und 400 × 624 — dieselben Sollwerte wie J / K (Rollbehälter = Hülle, Zeile 46 px, Tastatur) |
| T3 | derselbe Katalog mit 40 Sätzen (unter der Schwelle, das Maß eines ausgelieferten Katalogs), 1 088 × 624 |
| GD1 / GD2 | 6 654 Gebäude in der Katalogliste des **Gebäude-Projektdialogs** „Eingabe der Gebäudedaten" (Seite `/katalogprobe?maske=projekt-gebaeude`; Projektliste oben, Übernahmeleiste, Katalog darunter), 1 088 × 624 und 400 × 624 — gemessen nur in der Katalogliste (`bereich: '.epos-katalogliste'`, die Projektliste darüber trägt ebenfalls eine Hülle); Sollwerte: Rollbehälter = Hülle, Zeile **53 px** (Projektdialog: die Wahlspalte bleibt), keine Tastaturprobe |
| GD3 | derselbe Dialog mit 269 Gebäuden (das Maß der Testdatenbank, weiter virtualisiert), 1 088 × 624 |
| Z1 / Z2 | die **Wohnungstabelle** der Stufe Erweitert im Zapfprofil-Dialog (Seite `maske=wohnungen`; die Probe klickt die Stufe „Erweitert"), 12 Wohnungstypen, 1 088 × 624 und 400 × 624 — Sollwerte (l) |
| Z3 / Z4 / Z5 | das **Raster der Zapfkategorien** für sich (Seite `maske=kategorien`), 10 Kategorien, bearbeitbar 1 088 × 624 und 400 × 624, lesend (`art=lesen`) 1 088 × 624 — Sollwerte (l) |
| Z6 / Z7 | dasselbe Raster **in seiner Überlagerung** „Kategorien…" des Katalogdialogs (Seite `maske=tww`), 1 088 × 624 (dazu `querMax` 1 px) und 400 × 624 — Sollwerte (l) |
| Z8 | **Gegenprobe** zu Z7: die versteckte Feldbeschriftung ohne positionierten Vorfahren (`position: static`). Sie MUSS die Sollwerte verfehlen |
| GI / GJ | 2 400 Zeilen in der Liste **„Flächen je Zone“ des Gebäudeimports** (Stufe G6c; Seite `/gebaeudeimport?datei=ifc4_zonen.ifc&flaechen=2400`, die Probe klickt „Datei wählen…“, die Flächen des Zonenhauses reihum vervielfacht), 1 088 × 624 und 400 × 624 — gemessen nur in der Flächenliste (`bereich: '.epos-gebimport-flaechen'`); Sollwerte: Rollbehälter = Hülle, Zeile **46 px** (die Zeile ist die Wahl) |

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
Seit Stufe 1 der Neuordnung dazu `maske=stromganglinie|projekt-heizkessel` (seit Stufe G3,
Welle K auch `projekt-gebaeude`, der Gebäudedialog des Projekts — `gebaeude` ist die Verwaltung), `art=` (Ausprägung
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
| K4 | der **Katalograhmen mit Eingabeblock** (Seite `maske=rahmen`: Liste, darunter zwei Bilder in Reitern und acht Felder, darunter die Fußleiste) — die Anordnung, die seit Stufe 4 keine Verwaltung mehr trägt, der Baustein aber weiterführt |
| G1 | **Gegenprobe**: derselbe Rahmen (`maske=rahmen`) mit dem Maß von vor KL-5 samt dem Raster von damals. Sie MUSS den Befund zeigen |
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

**Alternative ohne Verzeichnisverbindung.** Statt der Verzeichnisverbindung genügt eine
vorübergehende Hülle `node_modules/playwright` neben den `.mjs`-Skripten (Kopie oder
Verzeichnisverbindung auf denselben Ordner `.playwright/package`); `node.exe` aus dem
NuGet-Cache findet die Bibliothek dann über die gewöhnliche Modulsuche, ohne `NODE_PATH`. Die
Hülle bleibt außerhalb des Repositoriums (`.gitignore`) und wird nach der Messung wieder
gelöscht; der Wirt-Port ist frei wählbar (`--url`, siehe oben).

**Aufruf mit dem installierten Edge (`rasterprobe.mjs`).** Ohne Playwright-Chromium genügt das
npm-Paket `playwright-core` (ohne eigenen Browser, rund 9 MB entpackt) in einem Ordner außerhalb des
Repositoriums; `--kanal msedge` (oder `chrome`) startet den installierten Browser headless — Edge
rechnet mit derselben Engine wie WebView2 in der Anwendung:

```bash
npm install playwright-core@1.58.0 --prefix <ordner>
NODE_PATH=<ordner>/node_modules node rasterprobe.mjs --url http://127.0.0.1:5299 --kanal msedge
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

## Neuordnung der Administrationsdialoge, Stufe 3 (V3, V6, V8, V12)

Das **Stammblatt steht neben der Liste** (`Katalograhmen` mit `Blatt`, ab 900 px Rahmenbreite
rechts, `clamp(340px, 36 %, 440px)`; schmal als Blatt über der Liste), darüber die
**Auswahlleiste**; die Liste hat eine **Kästchenspalte** (Mehrfachwahl). Die Fälle N01 … N08 und
N10 … N12 (die Verwaltungen mit Stammblatt, Menge `STUFE3` in `katalogprobe.mjs`) messen dazu
(Funktion `stufe3probe`, Sollwerte in `pruefe`):

| | Größe | Sollwert |
|---|---|---|
| (u1) | breit: Stammblatt rechts der Liste, 340 … 440 px breit, Auswahlleiste über ihm | ja |
| (u2) | breit: ganze Zeilen der Liste unter dem Kopf (ab 8 Sätzen im Katalog) | ≥ 8 |
| (u3) | schmal: Stammblatt beim Öffnen verborgen, Auswahlleiste über der Liste | ja |
| (u4) | drei Kästchen gesetzt: die Leiste nennt „3 gewählt", die Liste springt nicht | ja |
| (u5) | breit, „Vergleichen": Vergleichstabelle im Stammblatt, quer 0 px, nicht über dessen Rand | ja |
| (u6) | schmal, „Stammblatt ›": das Blatt an der Stelle der Liste, Auswahlleiste bleibt; „‹ Liste" zurück | ja |
| (u7) | die Seite rollt nie quer | 0 px |

**Ergebnis vom 23.09.2026** (Chromium headless, Playwright 1.58.0; „vorher" = Stand vor Stufe 3,
derselbe Wirt aus `HEAD` gebaut):

| | vorher (Stufe 2) | nachher (Stufe 3) |
|---|---|---|
| Listenhülle 1 088 × 624 (N01, N05, N10, N12) | 209 px innen (drei Zeilen) | **424 px** innen, **8 ganze Zeilen** (N03: alle 7 Sätze) |
| Listenhülle 400 × 624 (N01 / N10) | 118 / 110 px innen | **230 / 286 px** innen (3 / 5 ganze Zeilen) |
| Stammblatt 1 088 × 624 | — (Eingabeblock darunter, 160 px) | 380 × 370 px rechts, Auswahlleiste 380 × 94 px darüber |
| Querüberlauf 1 088 / 400 px | 0 px | 0 px; Vergleichstabelle quer 0 px |
| Kästchen gesetzt | — | Liste springt nicht (auch schmal: feste Ordnung der Leiste) |
| Nachbar Heizkessel-Projektdialog | P2a quer 0 px, P2b quer 91 px | unverändert (P2a 0 px, P2b 91 px) |
| Rückgabe | Katalogprobe 0 (45), Rasterprobe 0 (13) | **Katalogprobe 0 (45), Rasterprobe 0 (13)** |

## Neuordnung der Administrationsdialoge, Stufe 4 (V9, V14)

**Klimadaten und die drei Zeitreihen** (Wärmebedarf extern, Solarthermieganglinie,
Stromganglinie) tragen das Stammblatt wie die Gerätekataloge — Gruppe „Jahresverlauf" bzw.
„Ganglinie" mit dem Bild des Kern-Renderers (`ChartRenderer.JahresverlaufModell`, 8 760
Stundenwerte, im Blatt auf dessen Breite gezogen; „groß…" öffnet es breit) und Gruppe
„Herkunft" als Text. Das **Einlesen ist eine Überlagerung** mit Titel und Kreuz hinter
„Import…" in der Fußleiste (`button.epos-importknopf`). Die Fälle N09, N13, N14, N15 kommen
dazu in die Menge `STUFE3` (Messung (u1) … (u7) wie oben) und bilden die Menge `STUFE4`
(Funktion `stufe4probe`, Sollwerte in `pruefe`):

| | Größe | Sollwert |
|---|---|---|
| (v1) | das Bild der Gruppe steht ganz im Stammblatt (rechts nicht über dessen Inhalt), das SVG nicht breiter als seine Fläche | ja |
| (v2) | der Inhalt des Stammblatts rollt nicht quer; schmal gemessen im geöffneten Blatt | 0 px |
| (v3) | „Import…" öffnet **eine** Überlagerung mit Titel und genau einem Kreuz, ganz im Fenster, mit den Feldern des Einlesens (`.epos-einlesen`) | ja |
| (v4) | die Überlagerung rollt nicht quer, die Seite auch nicht | 0 px |
| (v5) | das Kreuz schließt sie wieder | ja |

Die Gegenprobe G1 hat mit Stufe 4 ihren Gegenstand verloren (der Klimadialog steht nicht mehr
„Liste oben, Eingabeblock darunter"); sie misst jetzt die Probeseite `maske=rahmen`, die diese
Anordnung des `Katalograhmen` (Schlitz `Eingabe`) im Bau nachstellt, K4 dieselbe Seite mit der
Behebung von KL-5.

**Ergebnis vom 23.09.2026** (Chromium headless 1208, Playwright 1.61.0 über eine Hülle mit
`executablePath`, weil der zu 1.61 gehörige Chromium auf dem Rechner fehlte; die Messung hängt
nicht an der Fassung):

| | 1 088 × 624 (`a`) | 400 × 624 (`b`) |
|---|---|---|
| Klimadaten N09: Listenhülle | 424 px innen, 8 ganze Zeilen | 286 px innen, 5 ganze Zeilen |
| N09: Stammblatt / Bild „Jahresverlauf" | 380 × 370 px rechts / 358 × 122 px (SVG 356 × 120) | 368 × 364 px / 346 × 118 px |
| N09: Überlagerung „Klimadaten einlesen" | 900 × 562 px, 1 Kreuz, quer 0 | 368 × 562 px, 1 Kreuz, quer 0 |
| Zeitreihen N13 / N14 / N15: Bild „Ganglinie" | 358 × 199 px (SVG 356 × 197) | 346 × 193 px (SVG 344 × 191) |
| N13 Überlagerung „Datei einlesen" | 900 × 292 px | 368 × 397 px |
| N14 / N15 Überlagerung (Titel der Ganglinie) | 900 × 272 px | 368 × 373 / 368 × 295 px |
| Stammblatt quer, Seite quer, Überlagerung quer | 0 px | 0 px |
| Vergleich (N09, drei gewählt) | Tabelle 358 × 535 px im Blatt, quer 0 | — |
| K4 Rahmen mit Eingabeblock (1 180 × 780) | Listenhülle 372 px, Eingabeblock 222 px darunter, frei | — |
| G1 Gegenprobe | Befund wie erwartet: Listenhülle über Eingabeblock, 116 471 px² Überschneidung | — |
| Rückgabe | **Katalogprobe 0 (46 Fälle), Rasterprobe 0 (13 Fälle)** | |

## Neuordnung der Administrationsdialoge, Stufe 5 (V16)

Die drei **Sonderlisten** stehen im Gerüst der Verwaltungen: **Gebäude** (`GebaeudeAdminDialog`,
Katalogliste mit Profil `FuerGebaeude` statt eigener Tabelle und vier Vorfiltern), **Gebäudetypen**
(`GebaeudetypDialog`, Typliste als Katalogliste, Gruppe „Tagesprofil" mit Klappliste „Kurve",
„Stundenwerte…" als Überlagerung) und **Lastspitzenkappung** (`PeakShavingDialog`, Liste der
Lastgänge, Parameter und Ergebnis im Stammblatt, „Lastgang aus Datei…" als Überlagerung). Die
Wirt-Seite kennt dafür die Masken `gebaeude`, `gebaeudetyp` und `peak`. Die Fälle N16 … N18 (Menge
`STUFE5` in `katalogprobe.mjs`, je 1 088 × 624 und 400 × 624) laufen durch Stufe 1 bis 3 wie die
übrigen Verwaltungen — die Lastspitzenkappung ohne Schloss und ohne Kästchen, ihre Auswahlleiste steht
nur schmal — und bekommen dazu die Funktion `stufe5probe` (Sollwerte in `pruefe`):

| | Größe | Sollwert |
|---|---|---|
| (w1) | das Stammblatt steht — breit rechts der Liste, schmal nach „Stammblatt ›" an ihrer Stelle | ja |
| (w2) | der Inhalt des Stammblatts rollt nicht quer, die Seite auch nicht | 0 px |
| (w3) | der Knopf des Falls öffnet **eine** Überlagerung mit Titel und genau einem Kreuz, ganz im Fenster, ohne Querrollen: „Gebäudetypen…" (N16, die eingebettete Typenverwaltung), „Stundenwerte…" (N17, 24 Felder), „Lastgang aus Datei…" (N18, die Felder des Einlesens) | ja |
| (w4) | das Kreuz schließt sie | ja |
| (w5) | N18: „Berechnen" füllt das Ergebnis im Blatt; die Kennzahltabelle steht ganz im Blatt, nichts rollt quer | ja |

Die Prüfung (u1) der Stufe 3 („Auswahlleiste über dem Stammblatt") übergeht eine Auswahlleiste ohne
Fläche — die der Lastspitzenkappung steht breit gar nicht. Die Tabellen einer Stammblattgruppe
brechen ihre Texte um (`.epos-stammblattgruppe .epos-raster-huelle > table.epos-raster`): ohne diese
Regel war die Kennzahltabelle 443 px breit in 356 px Gruppe.

**Ergebnis vom 23.09.2026** (Chromium headless 1208, Playwright 1.58.0 über das NuGet-Paket, Wirt auf
Port 5317):

| | 1 088 × 624 (`a`) | 400 × 624 (`b`) |
|---|---|---|
| Gebäude N16 (277 Sätze): Listenhülle | 426 px, 8 ganze Zeilen, quer 0 | 288 px, 5 ganze Zeilen, quer 0 |
| N16: Stammblatt | 380 × 370 px rechts, Inhalt quer 0 | 368 × 364 px als Blatt, quer 0 |
| N16: Überlagerung „Gebäudetypen…" | 900 × 562 px, 1 Kreuz, eingebettete Verwaltung, quer 0 | 368 × 562 px, 1 Kreuz, quer 0 |
| Gebäudetypen N17 (12 Sätze): Listenhülle | 426 px, 8 ganze Zeilen | 232 px, 3 ganze Zeilen |
| N17: Überlagerung „Stundenwerte…" | 900 × 507 px, 24 Felder, 1 Kreuz, quer 0 | 368 × 562 px, 24 Felder, 1 Kreuz, quer 0 |
| Lastspitzenkappung N18 (5 Lastgänge): Listenhülle | 426 px, 5 Zeilen | 281 px, 4 ganze Zeilen |
| N18: Stammblatt / nach „Berechnen" | 380 × 464 px (ohne Auswahlleiste), Kennzahltabelle 356 px breit, 21 Zeilen, quer 0 | 368 × 363 px, Tabelle 344 px breit, quer 0 |
| N18: Überlagerung „Lastgang aus Datei…" | 900 × 224 px, 1 Kreuz, quer 0 | 368 × 242 px, 1 Kreuz, quer 0 |
| Zeilenmaß, Schloss, Fußleiste | 46 px; Schloss in N16/N17; Fußleiste frei | 46 px; Fußleiste zweizeilig, frei |
| Rückgabe | **Katalogprobe 0 (52 Fälle), Rasterprobe 0 (13 Fälle)** | |

### „Import…" als Zweitweg in den Gerätekatalogen (Konzept 7.1 d)

Die Gerätekataloge A1 bis A3 tragen „Import…" in der Fußleiste (ohne BHKW, für das es keinen
Herstellerimport gibt). Er öffnet den vorhandenen Import — `KatalogImportDialog` (Heizkessel,
Solarkollektoren, Pufferspeicher, Stromspeicher, Wärmepumpe) bzw. `ModulImportDialog` (PV-Module,
Wechselrichter) — über den Baustein `ImportUeberlagerung` als Überlagerung **ohne eigenen Kopf**: Titel
und Kreuz trägt der Importdialog selbst, weil er sein Kreuz während eines Laufs wegnimmt und Esc dann
als „Lauf abbrechen" deutet. Die Wirt-Seite reicht den Import in den Masken `browser`, `modul` und
`waermepumpe` herein; damit tragen N01 und N03 bis N08 ihre volle Fußleiste. Die Fälle N19 … N22
(Menge `IMPORT`, je 1 088 × 624 und 400 × 624) laufen wie N01/N05/N07/N08 durch Stufe 1 bis 3 und
dazu durch `stufe5probe` mit dem Öffner `button.epos-importknopf`. Neu in `pruefe`: (w6) in der
Überlagerung steht genau ein Importdialog, (w7) die Überlagerung hat keinen eigenen Kopf; der Titel
wird bei ihr aus dem eingebetteten Dialog gelesen, geschlossen wird über dessen Kreuz.

**Ergebnis vom 23.09.2026:**

| | 1 088 × 624 (`a`) | 400 × 624 (`b`) |
|---|---|---|
| Fußleiste mit „Import…" | eine Zeile, alle Knöpfe frei („Import…" 88 × 44 px bei x 884) | zweizeilig, alle Knöpfe frei |
| N19 Heizkessel: Überlagerung | 1 044,5 × 586,5 px, „Heizkessel Einlesen", 1 Kreuz, quer 0 | 384 × 586,5 px, 1 Kreuz, quer 0 |
| N20 PV-Module: Überlagerung | 1 044,5 × 586,5 px, „Photovoltaik Module Import", 1 Kreuz, quer 0 | 384 × 586,5 px, 1 Kreuz, quer 0 |
| N21 Stromspeicher: Überlagerung | 1 044,5 × 586,5 px, „Stromspeicher Einlesen", 1 Kreuz, quer 0 | 384 × 586,5 px, 1 Kreuz, quer 0 |
| N22 Wärmepumpe: Überlagerung | 1 044,5 × 586,5 px, „Wärmepumpen Einlesen", 1 Kreuz, quer 0 | 384 × 586,5 px, 1 Kreuz, quer 0 |
| Kreuz des Imports | schließt die Überlagerung | schließt die Überlagerung |
| Rückgabe | **Katalogprobe 0 (60 Fälle), Rasterprobe 0 (13 Fälle)** | |

### „Schloss aufheben…" / „Schloss setzen…" in der Auswahlleiste (Entscheid AD-Q15)

Alle zehn Verwaltungen tragen eine vierte Handlung zwischen „Duplizieren…" und „Löschen"; die
Wirt-Seite reicht dafür in jeder Maske einen `Schlossweg` herein (`ProbeSchloss`). Die Beschriftung
wechselt mit der Auswahl; der Knopf hält über die `Breitenvorlage` die Breite der längeren. Gemessen
gegen denselben Wirt ohne den Weg (vorher = drei Handlungen), Chromium headless 1208, Playwright 1.58.0
über das NuGet-Paket, Wirt auf Port 5359:

| | 1 088 × 624 (`a`) | 400 × 624 (`b`) |
|---|---|---|
| Liste springt beim Setzen der Kästchen (u4) | nein | nein |
| Auswahlleiste Fokuszeile / „3 gewählt" | 94 / 94 px wie vorher (A1 bis A4, A6 bis A10); Bedarfsprofile mit langem Löschtext 118 / 94 px | 150 / 150 px (vorher 100 / 100; Klimadaten 100 / 100 wie vorher) |
| Listenhülle | unverändert (426 px, 8 ganze Zeilen) | 50 px weniger: N01 181,8 px (2 Zeilen, vorher 231,8), N10 237,8 px (4, vorher 287,8) |
| Rückgabe | **Katalogprobe 0 (60 Fälle), Rasterprobe 0 (13 Fälle)** | |

Damit breit zwei Zeilen reichen, ist das erste Wort der Leiste höchstens `10rem` breit (voller Name im
Kurztext und im Kopf des Stammblatts), und der leise Hinweis „Kästchen: mehrere wählen" kürzt sich in
seiner Zeile, statt eine dritte zu öffnen. Schmal brechen vier Handlungen in 368 px zwangsläufig in zwei
Zeilen um — die Liste verliert dort eine Zeile; offen im Konzept Administrationsdialoge 7.1 (f).

## Zapfprofilgenerator, Stufe Z4 — Katalog der Nutzungsarten, Wohnungstabelle, Zapfkategorien

Der Katalogdialog „Brauchwasser-Nutzungsarten" (`TwwNutzungsartAdminDialog`) trägt eine neue
`Katalogliste`; die Wohnungstabelle des Zapfprofil-Dialogs und das Raster der Zapfkategorien sind
bearbeitbare `Raster` ohne eigene Höchsthöhe. Die Wirt-Seite kennt dafür die Masken `tww`
(mit „Kategorien…" als Überlagerung), `wohnungen` und `kategorien` (`art=lesen`); die Rasterprobe
misst sie in den Fällen T1 … T3 und Z1 … Z8 (Sollwerte (l) für die Raster ohne Höchsthöhe, `bereich`
grenzt die Messung auf ein Raster der Seite ein, `klick` ist der Schritt vor der Messung), die
Katalogprobe den ganzen Dialog im Fall N23 (`a` 1 088 × 624, `b` 400 × 624; Stufe 1 bis 3 und
`stufe4probe`: Bild der Gruppe „Tagesgang" im Stammblatt, „Import…" öffnet die Überlagerung des
Katalogimports).

**Befund und Behebung:**

- **Katalogliste quer (s3).** Ohne Rang rollte die Liste bei 1 088 px um 58 px, bei 400 px um 350 px
  quer. Das Profil `FuerTwwNutzungsart` gibt jetzt Ränge: Nutzungsart und Bezugsart immer, Kalender und
  Katalogversion bei Platz, Herkunft und Status weichen als erste (das Stammblatt nennt beide).
- **Überlagerung „Zapfkategorien" rollte mit.** Die versteckte Feldbeschriftung der Zeilentabellen
  (`position: absolute`) hatte keinen positionierten Vorfahren; ihr Bezugskasten war die Überlagerung
  (`position: fixed`), und die Zellen jenseits des rechten Hüllenrandes ließen die Überlagerung um
  135 px (1 088 px) bzw. 795 px (400 px) quer rollen — Rollbereich im Rollbereich. Behoben mit
  `position: relative` an `.epos-feld` in den drei Zeilentabellen des Zapfprofils (Gegenprobe Z8).
- **Raster zu breit.** Zahlenfelder in Vorgabebreite (20 Zeichen, rund 165 px) und einzeilige
  Spaltenköpfe machten das Kategorien-Raster 1 548 px breit; bei 1 088 px lagen Streuung, Kappung,
  Herkunft und „Entfernen" hinter dem rechten Rand (560 px quer in der Überlagerung). Zahlenfelder
  jetzt 5,5em, der Name 9em, die Pfeile der Reihenfolge ein Touchziel breit, Spaltenköpfe und die
  Herkunft brechen um.

**Ergebnis vom 24.09.2026** (Chromium headless 1208, Playwright 1.58.0 über das NuGet-Paket, Wirt auf
Port 5361):

| | 1 088 × 624 | 400 × 624 |
|---|---|---|
| T1 / T2: Katalog, 6 654 Sätze virtualisiert | Rollbehälter = Hülle (424 px), Zeile 46 px, 0 Platzhalter, echte Zeilen nach dem Rollen in 122 ms, 3 + 4 Melder, Ende → Zeile 6 653 im Bild, Pos1 → 0, quer 0 | Hülle 260 px, sonst wie links |
| T3: Katalog, 40 Sätze | nicht virtualisiert, Zeile 46 px, 0 Abstandshalter, 0 Melder, quer 0 | — |
| N23: Katalogdialog | Liste 659,8 px mit vier von sechs Spalten (Herkunft und Status weichen), quer 0 (vorher 58); 8 ganze Zeilen; Stammblatt 380 × 370 px rechts, Bild 358 px breit darin; „Import…": Überlagerung 900 × 345 px, 1 Kreuz, quer 0; Fußleiste frei | Nutzungsart und Bezugsart, quer 0 (vorher 350); Stammblatt als Blatt; Überlagerung 368 × 490 px, quer 0 |
| Z1 / Z2: Wohnungstabelle, 12 Zeilen | Zeile 45 px, 0 Abstandshalter, 0 Melder, Hülle rollt nicht senkrecht; quer 102 px in 498 px Eingabeblock (vorher 348) | quer 254 px (vorher 500); Seite quer 0 (vorher 179) |
| Z3 / Z4: Kategorien-Raster, 10 Zeilen | Zeile 45 px, quer 0 (vorher 514), Seite quer 0 (vorher 89) | Hülle quer 638 px, Seite quer 0 (vorher 777) |
| Z5: Kategorien lesend | Zeile 27,2 px, quer 0 (vorher 94) | — |
| Z6 / Z7: Kategorien in der Überlagerung | Hülle quer 0 (vorher 560), Überlagerung quer 0 (vorher 135) | Hülle quer 656 px, Überlagerung quer 0 (vorher 795) |
| Z8: Gegenprobe | — | Überlagerung quer 333 px: verfehlt wie erwartet |
| Rückgabe | **Rasterprobe 0 (24 Fälle), Katalogprobe 0 (62 Fälle)** | |

Offen: Die Wohnungstabelle rollt im Eingabeblock des Zapfprofil-Dialogs bei 1 088 px noch 102 px quer
(„Entfernen" teils hinter dem Rand) — fünf Spalten mit Auswahlfeld und Knopf passen nicht in 498 px.

## Gebäudesimulation G3, Welle K — die Katalogseite des Gebäudedialogs (GD1–GD3)

Die Katalogseite des Gebäudedialogs ist die virtualisierte `Katalogliste` (Wahlspalte, 53 px,
Filterstand aus dem Kern); Maske `projekt-gebaeude` des Wirts (`maske=gebaeude` bleibt die
Gebäudeverwaltung, Fall N16 der Katalogprobe). Die Fälle messen unter `.epos-katalogliste`, weil die
Projektliste darüber ebenfalls eine `.epos-raster-huelle` trägt.

**Gemessen am 25.09.2026 im integrierten Browser der App** (Playwright fehlt auf dem Arbeitsrechner;
kein Download, Anwenderentscheid): dieselben Größen im Seitenkontext abgelesen, die Sichtbarkeitsmelder
über die Änderungen der Abstandshalter gezählt. Ein ausgeblendetes Fenster zeichnet nicht — dann
stehen keine Zeilen und die Messung ist wertlos (`document.visibilityState` prüfen).

| Fall | Zeilenhöhe / Maß | Rollbehälter | neue Zeilen nach dem Rollen | Platzhalter | Abstandshalter-Änderungen in 3 s |
|---|---|---|---|---|---|
| GD1 (6 654, 1 088 × 624) | 53 / 53 | Hülle | 151 ms | 0 | 4 |
| GD2 (6 654, 400 × 624) | 53 / 53 | Hülle (420 px, 9 Zeilen) | 183 ms | 0 | 4 |
| GD3 (269, 1 088 × 624) | 53 / 53 | Hülle | im Rollen | 0 | 4 |
| J (Vergleich, 46 px) | 46 / 46 | Hülle | 173 ms | 0 | 4 |
| J, Gegenprobe (Zeilen 45,3/45,7 px) | — | — | — | 0 | **200** |

Im integrierten Browser schlug die Gegenprobe an der Gebäudeliste mit verkleinerten (52,5, 50,
30 px) oder wechselnden (50/56 px) Zeilen nur schwach aus (0 bis 12 Änderungen); die Zählung über die
Abstandshalter ist dort kein scharfer Nachweis. Der Skriptlauf zählt die Sichtbarkeitsmelder selbst
und ist scharf:

**Skriptlauf vom 25.09.2026** (`node rasterprobe.mjs --kanal msedge`, Edge headless über
`playwright-core` 1.58.0, Wirt Release auf Port 5299): **alle 27 Fälle erfüllen die Sollwerte**. GD1
bis GD3 im Einzelnen: Zeilenhöhe 53 / Maß 53, Rollbehälter die Hülle (418 px innen), 0
`loading`-Umschaltungen, nach dem Rollen um 2 000 px 16 echte Zeilen nach 138 bis 148 ms und 0
Platzhalter, Sichtbarkeitsmelder 3 beim Aufbau und 4 in den 3 s nach dem Rollen. **Gegenprobe**
(`--entpinnt --nur GD`, gezeichnete Zeile 52,5 px gegen das Maß 53): 3 von 3 Fällen rot — 408 bis
410 Sichtbarkeitsmeldungen in 3 s, 16 Platzhalter drei Sekunden nach dem Rollen.

## Gebäudeimport-Sichtprobe (Stufe G4) — Seite `/gebaeudeimport`

**Zweck.** Den Gebäudeimport so sehen, wie die Anwendung ihn zeigt, **ohne** die Datenbank des
Anwenders zu öffnen: der echte `GebaeudeImportDialog` mit der echten `GebaeudeImportHuelle` (ohne
Projekt), die Proben aus `Referenzlaeufe/Importproben/`. Ersetzt ist allein das Fenster der
Dateiwahl — der `Probenwaehler` (`Dienste.Datei` des Wirtes) liefert den Pfad der Probe; Dateiart,
Größengrenze samt benannter Ablehnung, Lesen, Zuordnen und Prüfen sind der Weg der Anwendung.
Der Wirt referenziert dafür `EPOS.UI.Daten` (das ihm seine internen Hüllen freigibt) und richtet
die Zugriffsschicht auf einen Ordner, den es nicht gibt (`DataRepository.PfadUeberschreibung` in
`Program.cs`): Ein Datenbankzugriff scheiterte laut, statt `%ProgramData%\EPOS_PLAN` zu öffnen.
Dass die Hülle ohne Projekt keine Datenbank fragt, hält
`GebaeudeImportHuelleTests.Ohne_Projekt_fragt_die_Huelle_keine_Datenbank`.

```bash
# Windows, Git-Bash (Wurzel des Arbeitsbaums)
dotnet build Proben/Rasterprobe/Wirt/Rasterprobe.Wirt.csproj -c Release
ASPNETCORE_URLS=http://127.0.0.1:5299 \
  dotnet Proben/Rasterprobe/Wirt/bin/Release/net10.0/Rasterprobe.Wirt.dll
# PowerShell: $env:ASPNETCORE_URLS='http://127.0.0.1:5299'; dotnet Proben/Rasterprobe/Wirt/bin/Release/net10.0/Rasterprobe.Wirt.dll
```

| Adresse | Was sie zeigt |
|---|---|
| `/gebaeudeimport` | der Zuordnungsdialog mit `gbxml_haus_si.xml`; „Datei wählen…" liest die Probe, OK übernimmt. Darunter die Tabelle **Übernahme**: Name, Baualtersklasse, jede Zeile mit Haken (Wert, Einheit, Herkunft) und die Abbildung auf den Gebäudeeditor (`Vorbelegung` = `NachKatalogdaten` samt Herleitungszeile) — Nutzfläche, Raumhöhe, Fläche je Nutzer, Flächen und U-Werte je Gruppe, Fenster N/O/S/W und Ost + West, g, ψ und Anschlusslängen, Luftwechsel, Sollwert, Baujahr |
| `/gebaeudeimport?datei=<name>` | dasselbe mit einer anderen Datei des Ordners (die Seite führt die Gebäudeproben als Verweise); eine fremde Art zeigt die benannte Ablehnung |
| `/gebaeudeimport?datei=ifc4_haus_materialnamen.ifc` | der Abschnitt **Baustoffe** unter „Bauteile (echte Hülle)": zwanzig Materialnamen mit Stufe, Baustoff, Stoffwerten und Herkunft der Werte, „16 von 20 zugeordnet, 1 ohne Treffer", „Fußbodenaufbau" gelb. Der Namensabgleich rechnet ohne Projekt gegen die Auslieferungssaat im Speicher (Katalog und Synonyme), ohne Datenbank; eine Zuordnung über die Klappliste bildet den Vorschlag neu (Kopfzeile der Bauteile: 6 → 7 Aufbauten). Die Übernahme nennt die Baustoffzuordnungen des Ergebnisses; gemerkt wird nichts. Wächter: `GebaeudeImportBaustoffeHuelleTests.Ohne_Projekt_bildet_die_Huelle_den_Abschnitt_ohne_Datenbank` |
| `…&ios=1` | die Größengrenze von iOS („gbXML 25 MB · IFC 20 MB") statt Windows |
| `/gebaeudeimport?datei=ifc4_zonen.ifc` | **mehrere Zonen** (Stufe G6c): Zonenregel im Kopf (vorbelegt je Geschoss), Bilanz, Zonen mit aufklappbaren Räumen, Flächen je Zone als virtualisierte Katalogliste mit drei Filtern; mit `datei=gbxml_zonen_viele.xml` und der Regel X3 die Obergrenze samt Knopf für die gröbere Regel |
| `…&flaechen=<n>` | die Liste „Flächen je Zone“ bekommt n Zeilen — die der Probe reihum wiederholt (Fälle GI und GJ der Rasterprobe) |
| `…&kultur=en-US` bzw. `de-DE` | Kultur und Sprache (Ressourcentexte, Zahlen der Übernahme); der Seitenabruf setzt dafür das Kulturkeks, das die Schaltung (`/_blazor`) liest — ohne `kultur=` gilt die Kultur des Prozesses |
| `…&dialog=gebaeude` | der Gebäudedialog des Projekts (wie `katalogprobe?maske=projekt-gebaeude`, zwölf synthetische Katalogsätze) **mit** „Importieren (gbXML, IFC)…"; der Zuordnungsdialog steht in seiner Überlagerung, nach OK öffnet der **vorbelegte Katalogeditor** im Modus Neu, nach dessen OK steht das Gebäude in der Projektliste samt Meldung. Der Editor bekommt den Parametersatz der Anwendung (`GebaeudeKatalogHuelle.Gaben`); nur seine Wege zur Datenbank weichen der Seite: Typen, Arten, Katalognamen und die Namensprüfung der Übernahme gegen den synthetischen Katalog, „Lies" findet nichts, die hergeleiteten Vorgaben der Wärmeübergabe fehlen, „Speichern" schreibt nichts. Die übrigen Beschriftungen des Gebäudedialogs bleiben seine deutschen Vorgaben |

Geschrieben wird nirgends; der Wirt bleibt außerhalb jeder Projektmappe und CI. Schalter für
Probe, Kultur, Grenze und Fall stehen oben auf der Seite (sie laden neu, damit die Schaltung die
Kultur wechselt).

**Selbstprüfung vom 25.09.2026** (integrierter Browser, Wirt auf Port 5299): Lesen, Zuordnen, OK
und Übernahme in beiden Fällen, `kultur=en-US` in Dialog und Übernahme, `ios=1` in der
Grenzzeile; im Protokoll des Wirtes kein Datenbankzugriff. **Befund:** Der vorbelegte Editor hielt
sein OK an. „Die Luftwechselrate muss größer als 0 sein" galt für jede Probe: Die Abbildung kannte die
Pflichtangabe `Luftwechselrate` nicht (ein gelesener Luftwechsel geht nach D12 auf die Infiltration),
sie blieb die 0 eines neuen Gebäudes. „Die Fläche je Nutzer muss größer als 0 sein" folgt nicht der
Baualtersklasse, sondern der Datei: Das Probenhaus nennt 5 Personen (24 m² je Nutzer), der IFC-Leser und
`gbxml_ohne_konstruktionen.xml` nennen keine, und ein Keller, der in der Raumliste als beheizt gilt,
macht die Angabe unvollständig. **Behebung:** Luftwechselrate (0,7 1/h), Fläche je Nutzer (35 m²) und
innere Gewinne (0 W) tragen, wo die Datei sie nicht liefert, eine ausgewiesene Vorgabe; die
Herleitungszeile des Editors nennt jede übernommene Vorgabe. Wächter: `GebaeudeImportEditorabschlussTests`
und der bunit-Fall `GebaeudeDialogImportTests.Mit_der_echten_Huelle_schliesst_der_vorbelegte_Editor_mit_OK`.

## Markenprobe (Berichtsvorlagen BV-E6) — Seite `/vorlagenfeldprobe`

Die Platzhaltermarke (`Vorlagenfeldknopf`) samt Umschalter und leiser Zeile in zehn Varianten
(Kachel, Diagramm, Tabelle, Feld, Text, rechter Rand) mit echten Katalogschlüsseln je Art und
Kontext; die Seite nimmt `?stellung=aus|marken|schluessel`. Der Wirt trägt dafür den Zustand
`Vorlagenfeldansicht` als Singleton und hängt `VorlagenfeldanzeigeHuelle` als Quelle des Halters
ein — ohne Zwischenablage, damit der Weg „Text markiert" gemessen wird.

```bash
node vorlagenfeldprobe.mjs --url http://127.0.0.1:5299 --fotos /tmp/vfpfotos [--breite 1280]
```

Gemessen je Stellung bei 1 280 × 900: Stilblatt geladen; drei Stellungen des Umschalters je
≥ 44 × 44, genau eine an; „Aus" ohne Marke und ohne Zeile; sonst zehn Marken je ≥ 44 × 44, keine
überdeckt den Titel ihres Elements oder eine andere Marke, keine ragt aus dem Fenster, im
Schlüsselmodus zeigt jeder Chip seinen Schlüssel und die Zeile „10 Platzhalter"; die Seite rollt
nicht quer. In „Marken" öffnet Überfahren die Aufklappung innerhalb des Fensters (Varianten 1, 6,
10), ein Klick heftet an, „Kopieren" ohne Zwischenablage zeigt `{{projekt.kunde}}` markiert, Esc
löst. Dazu die Seite `/vorlagenfeldwirte`: die echten Bausteine `Kennzahlkachel`, `DiagrammSvg` und
`Vergleichstabelle` je ohne und mit Vorlagenfeld nebeneinander. In „Aus" liegen Titel, Wert, Leiste,
Bild und Tabelle beider Seiten auf denselben Höhen (keine Layoutverschiebung); in allen drei Stellungen
sind die Zeilen der Vergleichstabelle mit Marke so hoch wie ohne und wie in „Aus"; die Marke ist
≥ 44 × 44 und überdeckt weder den Kacheltitel noch einen Knopf der Zoomleiste. Rückgabe `0` = kein
Verstoß.
