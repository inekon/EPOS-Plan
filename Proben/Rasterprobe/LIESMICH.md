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
| W1, C1, P1 | Wärmebedarf, BHKW-Katalog, Wärmepumpen-Stamm (je 1 180 × 780) |
| K4 | der **Katalograhmen mit Eingabeblock** (Seite `maske=rahmen`: Liste, darunter zwei Bilder in Reitern und acht Felder, darunter die Fußleiste) — die Anordnung, die seit Stufe 4 keine Verwaltung mehr trägt, der Baustein aber weiterführt |
| G1 | **Gegenprobe**: derselbe Rahmen (`maske=rahmen`) mit dem Maß von vor KL-5 samt dem Raster von damals. Sie MUSS den Befund zeigen |
| N01a … N15b | **Neuordnung Stufe 1**: jede Verwaltung im Katalograhmen (vier Katalogbrowser, drei Modulkataloge, Wärmepumpe, Klimadaten, drei Bedarfe, zwei Zeitreihen) mit vollen Zeilen, je 1 088 × 624 (`a`) und 400 × 624 (`b`) — Messung und Sollwerte im Abschnitt zu Stufe 1 unten; N14 entfällt (siehe unten) |
| G2 | **Gegenprobe zu Stufe 1**: Heizkessel mit den Regeln von vor Stufe 1. Sie MUSS Rollbereich-in-Rollbereich und Querüberlauf zeigen |
| P2a / P2b | Nachbar: der Heizkessel-**Projektdialog** (erbt die Katalogliste); die Liste darf nicht zusammenfallen, bei 1 088 px nicht quer rollen |

**S1 und N14 (Solarthermieganglinie) sind gestrichen** (Welle N, 09.10.2026): Seit D1 ist die
Solarthermieganglinie keine Verwaltung im `Katalograhmen` mehr — der Menüpunkt öffnet den vereinten
`SolarganglinieDialog` im Katalogbetrieb, ein Fensterdialog mit der Katalogseite
`GanglinieKatalogseite` (Katalogliste und Katalogpflege in einer Leiste), ohne Rahmen, Eingabeblock
und Stammblatt. Beide Fälle warteten auf `.epos-katalog-dialog` und brachen ab (Rückgabe 2); was sie
maßen — Listenhülle gegen Eingabeblock (KL-5), Stammblatt mit Bild „Ganglinie" und Einlese-Überlagerung
(Stufen 3 und 4) —, gibt es an dieser Maske nicht mehr. Die Katalogliste darin ist derselbe Baustein,
den `rasterprobe.mjs` misst; Wärmebedarf (W1, N13) und Stromganglinie (N15) bleiben als Zeitreihen
im Rahmen gemessen.

**Mehrfachwahl folgt der Übernahme (DZ1-N2).** Zwei weitere Fälle `DZ1N2_uebernahme_heizkessel_1280x800` und
`DZ1N2_uebernahme_bhkw_1280x800` öffnen den echten Projektdialog der Fensterprobe mit `?aufnahme=1` (der Wirt nimmt
dann bei „In das Projekt übernehmen“ eine Zeile auf): Kästchen der ersten Projektzeile, einen Katalogsatz ankreuzen,
übernehmen samt Trägerwahl — danach muss genau die neue Projektzeile angekreuzt sein, und „Aus dem Projekt
entfernen“ muss genau sie entfernen, die zuvor angeklickte Zeile bleibt. Ergebnis vom 10.10.2026: beide grün,
der volle Lauf 66 Fälle ohne Verstoß.

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
| `…&dialog=gebaeude` | der Gebäudedialog des Projekts (wie `katalogprobe?maske=projekt-gebaeude`, zwölf synthetische Katalogsätze) **mit** „Importieren (gbXML, IFC)…"; der Zuordnungsdialog steht in seiner Überlagerung, nach OK öffnet der **vorbelegte Katalogeditor** im Modus Neu, nach dessen OK steht das Gebäude in der Projektliste samt Meldung. Der Editor bekommt den Parametersatz der Anwendung (`GebaeudeKatalogHuelle.Gaben`); nur seine Wege zur Datenbank weichen der Seite: Typen, Arten und die Namensprüfung der Übernahme gegen den synthetischen Katalog, die hergeleiteten Vorgaben der Wärmeübergabe fehlen, „Speichern" schreibt nichts. Die übrigen Beschriftungen des Gebäudedialogs bleiben seine deutschen Vorgaben |

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

## Sichtprobe „Berichte & Kosten“ (Konzept Navigation, Variante A) — Seite `/berichtekosten`

Der echte Rahmen `BerichteKostenSeite` als sechstes Reiterblatt einer Startseiten-Leiste — zwei
Reiterebenen übereinander wie in der Anwendung — mit den Statuszeilen aus Mockup A, Stammname,
Platzhalter-Umschalter und Hilfe im Leistenende. Die vier Seiten bekommen synthetische Stände (ohne
Projekt, ohne Datenbank); die Seite nimmt `?seite=UEBERSICHT|KOSTEN|WIRTSCHAFT|BERICHT`, `?ansicht=1`
(als eigene Ansicht mit Rückweg) und `?stamm=<name>`.

```bash
node berichtekostenprobe.mjs --url http://127.0.0.1:5299 --fotos /tmp/bkfotos
```

Gemessen bei 1 280 × 900 und 820 × 1 180: Stilblatt geladen; vier Reiter, jeder ≥ 44 px hoch; über
900 px die lange Statuszeile und das Leistenende in derselben Zeile wie die Reiter, darunter die
Kurzform und das Leistenende in eigener Zeile; die Warnung in `--epos-warn-text`; nichts ragt aus dem
Fenster, die Seite rollt nicht quer. Ein langer Stammname kürzt sich mit Auslassung (voller Name im
`title`), wird es enger, fällt die Beschriftung „Stamm:“ weg. Rückgabe `0` = kein Verstoß.

## Rollprobe „Berichte & Kosten“ — Seite `/berichtekosten`

Dieselbe Seite wie die Sichtprobe, gemessen wird aber der **Reiterwechsel**: Die Probe klickt bei
1 280 × 600 und 820 × 700 nacheinander auf die vier Reiter (hin und zurück) und misst nach jedem
Klick die Rollposition des Fensters und jedes rollenden Vorfahren, ob die Reiterleiste im Fenster
liegt, ob die neue Seite direkt unter ihr beginnt und ob der Fokus auf der Seitenwurzel steht. Jede
der vier Seiten fokussiert beim ersten Zeichnen ihre Wurzel; ohne `preventScroll` rollt Chromium
eine Seite, die nicht ganz ins Fenster passt, ins Bild und schiebt die Leiste hinaus.

```bash
node berichtescrollprobe.mjs --url http://127.0.0.1:5299 [--fotos /tmp/rollfotos]
node berichtescrollprobe.mjs --gegenprobe   # Fokus ohne preventScroll nachgestellt: muss rot sein
```

Rückgabe `0` = kein Verstoß. Gegenprobe rot bei Wirtschaftlichkeit und Bericht (Fenster gerollt um
153 bis 253 px), Übersicht und Kosten passen mit den synthetischen Ständen ganz ins Fenster.

## Fokusprobe — springt ein Dialog beim Öffnen? — Seiten `/fensterprobe`, `/katalogprobe`, `/konditionierungsprobe`, `/gebaeudeimport`

Fast jeder Dialog, jede Überlagerung und jede Seite fokussiert beim ersten Zeichnen ihre Wurzel. Ein
`FocusAsync()` ohne `preventScroll` rollt Chromium so, dass das Element sichtbar wird: Ist der Dialog höher als
sein Rollbehälter oder steht er nicht oben darin, rollt Chromium ihn ins Bild und schiebt Dialogkopf, Reiter oder
Schlussleiste hinaus. Die Probe öffnet 30 Fälle bei 1 280 × 600 und 820 × 700 und misst nach dem Öffnen, bevor
irgendwer rollt, die Rollposition des Fensters und **jedes** Elements, die Lage des obersten Dialogkopfs (in einer
Überlagerung deren Kopf) und des Primärknopfs und das Fokusziel. Verstoß: eine Rollposition ungleich 0 oder ein
Kopf über der Oberkante des Fensters.

| Fälle | Seite |
|---|---|
| `fenster-*` (6) | `/fensterprobe`: Heizkessel, BHKW, Wärmepumpen, Gebäude, Dubletten, Katalog — als Wurzel in `#app` wie im eigenen Fenster |
| `ueberlagerung-gebaeude-simulation` | `/fensterprobe?fall=gebaeude`, Knopf „Simulation...": der Wärmebedarf als Überlagerung |
| `katalog-*` (16) | `/katalogprobe`, alle Masken außer `rahmen`, 40 Zeilen, volle Spalten |
| `konditionierung-*` (6) | `/konditionierungsprobe`: Gebäudekatalog als breite Überlagerung (projekt, gesamt, neu, vorlagen), Bausteine, Verwaltung in der Seite |
| `gebaeudeimport` | `/gebaeudeimport`: Zuordnungsdialog über dem Gebäudedialog |

```bash
node fokusprobe.mjs --url http://127.0.0.1:5299 [--nur <präfix>] [--fotos <ordner außerhalb des Repositorys>]
node fokusprobe.mjs --gegenprobe   # dasselbe Fokusziel ohne preventScroll neu fokussiert: muss rot sein
```

Rückgabe `0` = kein Verstoß (mit `--gegenprobe`: mindestens einer), `1` = sonst, `2` = Aufbaufehler. Die Probe
steht in keiner CI; die Quelltextwache dazu ist `EPOS.UI.Tests/FokusOhneRollenWacheTests` (jeder `FocusAsync(`
unter `EPOS.UI` trägt `preventScroll: true` oder einen Vermerk `Rollen gewollt: <Grund>`).

**Ergebnis vom 06.10.2026** (Wirt Release, Chromium headless, `kultur=de-DE`):

| Fall | Fenster | vorher (Bestand ohne `preventScroll`) | nachher |
|---|---|---|---|
| Wärmepumpen im eigenen Fenster | 1 280 × 600 / 820 × 700 | Fenster gerollt um 1 059 / 1 315 px — der Fokus der eingebetteten Detailansicht zieht das Dokument fast bis ans Ende | 0 |
| Gebäude → „Simulation..." (Überlagerung) | beide | Überlagerung gerollt um 70 px, ihr Kopf bei −23 / −18 px | 0, Kopf bei 47 / 52 px |
| Verwaltung Gebäude in der Seite | beide | Fenster gerollt um 153 px (Probenkopf aus dem Bild) | 0 |
| Gebäudeimport | beide | Fenster gerollt um 6 / 24 px | 0 |
| übrige 26 Fälle | beide | 0 | 0 |

Vorher 10 Verstöße, nachher 0. Gegenprobe rot mit 26 Verstößen: zusätzlich zu den vier Fällen oben die vier
Gebäudekatalog-Überlagerungen der Konditionierungsprobe (70 px, Kopf bei −35 / −32 px) — im Bestand rollte deren
Erstfokus beim Öffnen nicht, ein erneuter Fokus ohne Schutz rollt sie.

## Konditionierungsprobe (Stufe KP2) — Seite `/konditionierungsprobe`

**Zweck.** Die Cloud-Vorabnahme der Oberflächenwellen von KP2 (Entwurf KP2, Abschnitt 7): der echte
`GebaeudeKatalogDialog` in der **breiten** Überlagerung, wie Gebäudedialog und Gebäudeverwaltung ihn
zeigen — ohne Datenbank. Der Parametersatz ist der der Anwendung (`GebaeudeKatalogHuelle.Gaben` in der
Betriebsart Neu, dem Weg der Hülle ohne Datenbankzugriff); darüber legt die Seite je Fall Daten,
Betriebsart, Sperre und Zonenweg. Gebäudetypen und -arten sind feste Listen, „Speichern“ und der
Zonenweg melden Erfolg und schreiben nichts, die hergeleiteten Vorgaben der Wärmeübergabe (Klimareihe
eines Projekts) und der Brauchwasserweg (keine Schale hängt ihn ein) fehlen. Der Reiter „Konditionierung“
bekommt den Weg der Hülle ohne Datenbank (`KonditionierungHuelle.ReinerWeg`): Zellen, Kalender, Rückfragen,
„aufteilen“ und „Zurücknehmen“ rechnen über die reinen Schritte des Kerns; „Aus dem Katalog erneut
übernehmen…“ übernimmt im Fall `projekt` den Stand, wie er ist. Jeder Satz trägt eine leere Konditionierung,
wie ein Satz einer Datenbank mit den Tabellen. Die Vorlagen (Welle U2) kommen aus der Ablage ohne Datenbank
mit den 14 ausgelieferten Vorlagen der Saat (`Konditionierungsvorlagenablage.AusSaat`, je Öffnen neu):
Auswahlliste, Vorschau, „Übernehmen“, „Als Vorlage speichern…“ und die Verwaltung als Blatt wirken wie in der
Anwendung, geschrieben wird nur in die Ablage der Seite.

| Adresse | Fall |
|---|---|
| `/konditionierungsprobe?fall=projekt` | Betriebsart Projekt mit zwei Zonen (Wohnen EG, Büro OG) samt Bauteilen |
| `…?fall=gesamt` | Bearbeiten; die Lüftung als Gesamtangabe — nur `Luftwechselrate` (0,6), Infiltration und Nutzerlüftung leer |
| `…?fall=gesperrt` | Bearbeiten, ein ausgelieferter Satz: `Gesperrt` und `SperrGrund`, wie die Hülle sie für `ReadOnly` setzt |
| `…?fall=neu` | Betriebsart Neu, der leere Satz der Hülle (Vorgabe) |
| `…?fall=ohnetabellen` | Bearbeiten ohne die Tabellen der Konditionierung: der Reiter benannt gesperrt, nur die Bestandszellen |
| `…?fall=vorlagen` | Bearbeiten, der volle Satz mit Kühlung: die Vorlagen je Karte und die Vorlagenverwaltung (Welle U2), die Abkürzung „alle Größen“ in der Zeile „Vorlage“ (Welle U5) |
| `…?fall=bausteine` | die Bausteine der Welle U0b in derselben Überlagerung: `Wochenraster` mit `MitAus` und `Umbrechend` (Sonntag 0–5 Uhr „aus“), zwei `Gemeinjahrdatum` (01.10., 30.04.) |
| `…?fall=karte` | Bearbeiten, die Karte im Einzelnen (Welle U3): „Heizen“ über denselben Weg ohne Datenbank angelegt — Sommerferien aus der Matrix, die neun Feiertage als Regel, das Zeitfenster Mo–Fr 6–8 Uhr 22 °C und eine eigene Periode über den Jahreswechsel („aus“) |
| `…?fall=verwaltung` | die Gebäudeverwaltung (`GebaeudeAdminDialog`, Welle U4) wie in ihrem eigenen Fenster, ohne Überlagerung: drei Katalogbauten (einer ausgeliefert), Stammblatt mit der Gruppe „Konditionierung“ und dem breiten Blatt über demselben Weg ohne Datenbank |
| `…&kultur=de-DE` bzw. `en-US` | Kultur und Sprache wie bei `/gebaeudeimport` |

```bash
node konditionierungsprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--nur <fall>] [--kultur de-DE|en-US]
```

Gemessen je Fall bei 390 × 844, 820 × 1 180, 1 180 × 820 und 1 300 × 900, jeder Reiter angewählt: kein
Querrollen (`scrollWidth ≤ clientWidth`) für Seite, Überlagerung und Reiterblatt; die Überlagerung ganz
im Fenster und `min(96vw, 1400px)` breit; beim Öffnen stehen ihr Titel und ihr Kreuz im Bild;
Bedienziele ≥ 44 × 44 px (ein Kästchen mit seiner Beschriftung, auch die zwei Felder der Hilfepille); im
Reiterblatt überdeckt kein Bedienziel ein anderes und keines ragt heraus; Esc, ✕
und Esc aus einem Feld schließen. Je Fall dazu: zwei Zonen im Reiter „Zonen“ (projekt); Infiltration
und Nutzerlüftung leer, die Herleitungszeile nennt 0,60 1/h aus der Luftwechselrate (gesamt);
Grundzeile mit Schloss, OK weich gesperrt mit dem Grund als `title`, „Speichern unter“ frei, der
OK-Versuch meldet den Grund und schreibt nichts (gesperrt); kein „Speichern unter“ (neu); die Anordnung
des Wochenrasters je Behälterbreite, 168 Zellen ≥ 44 × 44 px, sechs Zellen „aus“, im breitesten
Fenster auch an den Schwellen 1 150, 1 149, 600 und 599 px (bausteine). Im Reiter „Konditionierung“ der
Umbruch am Behälter: ab 900 px fünf Spalten der Matrix und alle Karten, darunter eine Spalte, höchstens eine
Karte und die fünf Reiter je Größe, die Tabelle ohne Querrollen; je Fall „Kalender anlegen“ und
„Zurücknehmen“ neben „Aus dem Katalog erneut übernehmen…“ (projekt), die Rückfrage „aufteilen“ an der
Gesamtangabe, nach „Ja“ Infiltration 0,3, Nutzerlüftung 0,3, Nachtauskühlung 2 und das Feld ΔT (gesamt),
die Werte als Text ohne Feld und ohne Knopf (gesperrt), Karten mit „Kalender anlegen“ (neu), der Grund
statt der Karten (ohnetabellen). Die Vorlagen (Welle U2): in `projekt`, `gesamt`, `neu` und `vorlagen` je Karte
eine Auswahlliste, zusammen die 14 der Ablage, „Büro“ in jeder zuerst; im Fall `vorlagen` an der Karte
„Heizen“ „Übernehmen“ ohne Wahl weich gesperrt, die Wahl „Büro“ mit Schloss, Beschreibung und Vorschau der
Woche, „Übernehmen“ (Herkunft in Karte und Zeile „Vorlage“, die Wahl danach leer), die Rückfrage P12 an den
angelegten Kalender („Nein“ lässt die Herkunft), „Als Vorlage speichern…“ inline (Formular gemessen, die
eigene Vorlage danach zuletzt in der Liste) und die Verwaltung als Blatt (der Editor daneben ausgeblendet,
die Liste der Größe samt der eigenen, Löschen an den ausgelieferten weich gesperrt mit Grund, Vorschau per
Klick, Umschalter „Personen“, Esc führt zurück in den Editor); Karte, Formular und Blatt je auf Bedienziele,
Überdeckung und Querrollen gemessen. Rückgabe `0` = kein Verstoß,
`1` = mindestens einer, `2` = Aufbaufehler. Die Probe steht in keiner CI.

**Ergebnis vom 29.09.2026** (Welle U0b; Wirt Release auf Port 5299, Chromium headless über das globale
Playwright, `kultur=de-DE`): **vorher drei Befunde, nachher kein Verstoß** in 20 Läufen (5 Fälle ×
4 Breiten, Rückgabe 0).

| Befund | vorher | Ursache | Behebung |
|---|---|---|---|
| Reiter 1 rollt bei 390 px quer | 43 px, in allen vier Editorfällen | die Klappliste „Energiestandard“ hielt als Flexkind ihren längsten Eintrag („wie Baualtersklasse (unsaniert)“, 373 px) als Mindestmaß | `.epos-formularraster .epos-feld-zeile > select.epos-eingabe { min-width: 0 }` — Wache `FormularrasterTests` |
| Titel und ✕ stehen beim Öffnen außerhalb des Bilds | Überlagerung um 70 bis 71 px gerollt, in allen 16 Editorläufen | der Editor fokussierte seine hohe Wurzel ohne `preventScroll` | `FocusAsync(preventScroll: true)` |
| 7 × 24 an der Schwelle unter dem Berührungsmaß | Zellen 43,91 px bei 1 150 px Behälter (44,00 erst ab 1 152 px) | Tagesspalte 3rem: 48 + 24 × (44 + 2) = 1 152 px | Tagesspalte 46 px — Wache `StilblattTests` |

Nachher, Überlagerung / Dialog / Reiterblatt in px: 390 → 374 / 340 / 340; 820 → 787 / 753 / 753;
1 180 → 1 133 / 1 099 / 1 099; 1 300 → 1 248 / 1 160 / 1 160 — die Dialogwurzel bleibt in der breiten
Überlagerung auf ihre 1 160 px gedeckelt. Querrollen überall 0, Überdeckungen 0, Bedienziele unter 44 px
0 (je Lauf die zwei Felder der Hilfepille mit 28 × 26 px ausgenommen), beim Öffnen gerollt 0 px; Esc,
✕ und Esc im Feld schließen überall. Wochenraster: Behälter 340 px → 4 × 6 (kleinste Zelle 55,1 px),
753 → 2 × 12 (56,8), 1 099 → 2 × 12 (85,6), 1 160 → 7 × 24 (44,4); an den Schwellen 1 150 → 7 × 24
(44,0), 1 149 → 2 × 12, 600 → 2 × 12 (44,0), 599 → 4 × 6; jede Zelle 44 px hoch, sechs Zellen „aus“.

**Ergebnis vom 30.09.2026** (Welle U1; Wirt Release auf Port 5299, Chromium headless über das globale
Playwright, `kultur=de-DE`): **kein Verstoß** in 24 Läufen (6 Fälle × 4 Breiten, Rückgabe 0). Zwei
Befunde aus den Fotos des ersten Laufs sind behoben: „Zurückneh|men“ brach bei 390 px im Wort, „Verwerfe|n“
in der Karte bei 1 180 px — die Knopfzeilen von Kopf und Karte brechen jetzt zwischen den Knöpfen um, und
ab 900 px stehen die fünf Karten in einer Reihe (`minmax(200px, 1fr)`; Wache `StilblattTests`); die zwei
Felder der Saison nennen im Platzhalter Start und Ende.

| Fenster | Überlagerung / Dialog | Behälter „Konditionierung“ | Matrix | Karten sichtbar |
|---|---|---|---|---|
| 390 × 844 | 374 / 340 px | 340 px | eine Spalte, fünf Reiter je Größe | 1 von 5 |
| 820 × 1 180 | 787 / 753 px | 753 px | eine Spalte, fünf Reiter je Größe | 1 von 5 |
| 1 180 × 820 | 1 133 / 1 099 px | 1 099 px | fünf Spalten, keine Reiter | 5 von 5 |
| 1 300 × 900 | 1 248 / 1 214 px | 1 214 px | fünf Spalten, keine Reiter | 5 von 5 |

Der Editor ist nicht mehr auf 1 160 px gedeckelt (1 300 px: Dialog 1 214 statt 1 160 px). Die Hilfepille
misst 44 × 44 px je Feld; Bedienziele unter 44 px, Überdeckungen, Querrollen und beim Öffnen gerollt: je 0.
Felder der Matrix: 25 mit Weg (neu, gesamt, projekt), 0 und 25 Texte im Lesemodus (gesperrt), 8
Bestandszellen ohne Tabellen (ohne Kühlspalte, die ohne „Gebäude wird gekühlt“ weich gesperrt ist). „Kalender
anlegen“ an „Heizen“ ergibt „angelegt, 0 eigene Perioden“, danach ist „Zurücknehmen“ frei. Das Wochenraster
der Bausteine misst wie im Ergebnis vom 29.09.2026; die Fotos liegen außerhalb des Repositorys.

**Ergebnis vom 30.09.2026** (Welle U2; Wirt Release, Chromium headless über das globale Playwright,
`kultur=de-DE`): **vorher drei Befunde, nachher kein Verstoß** in 28 Läufen (7 Fälle × 4 Breiten, Rückgabe 0).

| Befund | vorher | Ursache | Behebung |
|---|---|---|---|
| Zoomleiste der Vorschau unter dem Berührungsmaß | 71 × 26 und 43 × 26 px an jedem Vorschaubild, in Karte und Verwaltung, alle vier Breiten | `DiagrammSvg` zeichnet seine Zoomleiste; eine Woche in der Karte braucht keinen Zeitbereich | `OhneZoom="true"` an beiden Vorschauen — bunit `KalenderkarteTests`, `KonditionierungVorlagenDialogTests` |
| „Übernehmen“ überdeckt die Auswahlliste | 84 px (1 180) bzw. 61 px (1 300) in allen fünf Karten ab 900 px | die Klappliste hielt als Flexkind ihren längsten Eintrag als Mindestmaß, die Zeile brach nicht um | `.epos-kond-vorlagewahl` bricht um, Feld `flex: 1 1 10rem`, Liste `min-width: 0` — Wache `StilblattTests` |
| Kopf des Blatts ragt heraus | Titel der Verwaltung 57 px aus dem Blatt bei 390 px, die Überlagerung rollt 41 px quer | `.epos-blatt-kopf` brach nicht um: „‹ {Wirtstitel}“ und Titel in einer Zeile | `.epos-blatt-kopf { flex-wrap: wrap }` im Baustein — Wache `StilblattTests` |

| Fenster | Behälter „Konditionierung“ | Karten | Vorschau „Büro“ | Blatt der Verwaltung |
|---|---|---|---|---|
| 390 × 844 | 340 px | 1 von 5 | 312 px | 340 px, quer 0 |
| 820 × 1 180 | 753 px | 1 von 5 | 725 px | 753 px, quer 0 |
| 1 180 × 820 | 1 099 px | 5 von 5, „Übernehmen“ unter der Liste | 184 px | 1 099 px, quer 0 |
| 1 300 × 900 | 1 214 px | 5 von 5, „Übernehmen“ unter der Liste | 207 px | 1 214 px, quer 0 |

Die fünf Listen führen 3/3/2/3/3 Vorlagen, zusammen die 14 der Ablage, „Büro“ überall zuerst. Nach
„Übernehmen“ steht „aus Vorlage Büro“ in der Karte und „Büro“ in der Zeile „Vorlage“ der Matrix, die
Rückfrage P12 nennt die Vorlage und was ersetzt wird. Bedienziele unter 44 px, Überdeckungen, Querrollen und herausragende Ziele: je 0 — auch mit dem offenen
Formular „Als Vorlage speichern…“ und im Blatt der Verwaltung (22 Ziele); die Fotos liegen außerhalb des
Repositorys.

**Welle U4 — Zonendialog als Blatt und Blatt „Konditionierung“ der Verwaltung.** Der Fall `projekt` öffnet
nach „Kalender anlegen“ an „Heizen“ die Zone „Wohnen EG“ („Öffnen…“ im Reiter „Zonen“) als breites Blatt
über dem Editor und misst es wie ein Reiterblatt (Querrollen, Bedienziele ≥ 44 px, Überdeckung, Herausragen),
dazu die Zonenmatrix am Behälter (vier Karten — die Kühlspalte der Zone trägt keine): Die Zone ERBT
(„Heizen · Tag“ leer, Platzhalter „Vorgabe 20“), folgt dem Gebäude (Zustandszeile „vom Gebäude“, die
Heizspalte `epos-kond--ohnewirkung`, Knopf „Vom Gebäude übernehmen und anpassen“), ÜBERSCHREIBT
(„Geräte · Nennwert“: Platzhalter „Vorgabe 240“ — 400 W × 90/150 m² —, dann eigene 300) und hat nach
„übernehmen“ einen eigenen Kalender ohne Spalte ohne Wirkung; Esc führt zurück zum Editor, die Überlagerung
bleibt. Der Fall `verwaltung` misst die Gruppe „Konditionierung“ (fünf Zustandszeilen, Knopf ≥ 44 px, im
schmalen Fenster nach „Stammblatt ›“), das Blatt nach „Konditionierung…“ (breit, Liste und Stammblatt
ausgeblendet, gemessen wie oben, Matrix am Behälter, Felder und „Kalender anlegen“), eine Zelle zählt im Fuß
als Änderung und „Speichern“ wird frei, Esc führt zurück; nach „Verwerfen“ steht der ausgelieferte Satz im
Blatt nur als Text. **Die haftende Fußleiste zählt nicht als Überdeckung:** Die `SpeichernLeiste` des
Zonendialogs haftet im Blatt am unteren Rand (#572-Nachtrag) und liegt mit Absicht über dem Inhalt, der unter
ihr durchrollt; die Probe übergeht Paare, von denen genau eines in einer haftenden Ebene steht (erster Lauf:
2 bis 4 solcher Paare je Breite, alle Fußleiste über Matrixfeldern bzw. Bauteilknöpfen).

**Ergebnis vom 30.09.2026** (Welle U4; Wirt Release auf Port 5299, Chromium headless über das globale
Playwright, `kultur=de-DE`, zusammen mit den Vorlagen der Welle U2): **kein Verstoß** in 32 Läufen (8 Fälle ×
4 Breiten, Rückgabe 0). Das Blatt der Verwaltung führt die Vorlagenlisten der fünf Karten (derselbe Weg der
Hülle) und hat deshalb mehr Ziele als das Zonenblatt.

| Fenster | Zonenblatt | Zonenmatrix | Verwaltungsblatt | Verwaltungsmatrix |
|---|---|---|---|---|
| 390 × 844 | 340 px, 35 Ziele | 320 px: eine Spalte, fünf Reiter, 1 von 4 Karten | 358 px, 36 Ziele | 358 px: eine Spalte, fünf Reiter, 1 von 5 Karten |
| 820 × 1 180 | 753 px, 35 Ziele | 733 px: eine Spalte, fünf Reiter, 1 von 4 Karten | 788 px, 36 Ziele | 788 px: eine Spalte, fünf Reiter, 1 von 5 Karten |
| 1 180 × 820 | 1 099 px, 56 Ziele | 1 079 px: fünf Spalten, 4 von 4 Karten | 1 148 px, 70 Ziele | 1 148 px: fünf Spalten, 5 von 5 Karten |
| 1 300 × 900 | 1 214 px, 56 Ziele | 1 194 px: fünf Spalten, 4 von 4 Karten | 1 268 px, 70 Ziele | 1 268 px: fünf Spalten, 5 von 5 Karten |

Querrollen, Bedienziele unter 44 px, Überdeckungen und Herausragen: je 0; Felder der Matrix 25 (ausgeliefert:
0 Felder, 25 Texte, kein „Kalender anlegen“); „Konditionierung…“ 147 × 44 px. Die Rasterprobe der
Gebäudelisten (`node rasterprobe.mjs --nur GD`) erfüllt GD1 bis GD3 unverändert (Zeilenhöhe 53 / Maß 53,
Rollbehälter die Hülle, 0 Platzhalter nach dem Rollen, Sichtbarkeitsmelder 3 / 4), die Katalogprobe der
Verwaltung (`node katalogprobe.mjs --nur N16`) läuft in beiden Fenstern ohne Überlagerung durch.

**Welle U3 — die Karte im Einzelnen.** Der Fall `karte` wählt den Reiter „Konditionierung“ (das zweite Blatt
der Reiterfolge), klappt an „Heizen“ „Kalender bearbeiten…“ auf (die Einzelheiten stehen unter allen Karten, die Karte bleibt an ihrem Platz) und misst die Karte wie ein Reiterblatt
(Querrollen, Bedienziele ≥ 44 px, Überdeckung, Herausragen), dazu: die Einzelheiten über die ganze Zeile neben der stehenden Karte; das
Wochenraster am Behälter (unter 600 px vier Zeilen je Tag, ab 600 px zwei, ab 1 150 px eine) ohne Querrollen;
die Periodenliste mit elf Zeilen, davon eine des Matrixbereichs, ohne Querrollen und **ohne Wortbruch** (jedes
Wort einer sichtbaren Zelle steht auf einer Zeile, gemessen über `Range.getClientRects`); sieben Tagesknöpfe
am Zeitfenster; den Vermerk des letzten Werkzeugs; das Teppichbild mit 1 bis 2 000 Elementen, das Bezugsjahr
in der Zeile darunter und Zeitraum, Wert und Quelle am Zeiger. Je Breite EIN Foto der aufgeklappten Karte
(`karte_<breite>_einzelheiten.png`), die Fotos je Reiter entfallen in diesem Fall.

**Welle K1b — die Kalenderbedienung.** Derselbe Fall `karte` läuft mit Berührung (`hasTouch`, `pointer: coarse`) und
misst zusätzlich die Kalenderbedienung über den Einzelheiten: 168 Zellen des Wochenprofils und 365 Tage des
Jahresrasters je ≥ 44 px, kein Querrollen der Kalenderbedienung (Wochenprofil, Tabellen und Jahresraster rollen im
eigenen Kasten), das Jahresraster in einem Kasten mit `overflow-x: auto`, und ab 1 300 px alle fünf Karten in einer
Zeile. Ergebnis vom 09.10.2026 bei 390, 820, 1 180 und 1 300 px: kein Verstoß im Fall `karte`; der volle Lauf meldet
acht Verstöße im Zonenblatt (ein Kästchen 217,6 × 15 px), die die Kalenderbedienung nicht berührt.

**Ergebnis vom 30.09.2026** (Welle U3 auf dem Stand mit U4 und der vorgebbaren Reiterfolge; Wirt Release auf
Port 5299, Chromium headless über das globale Playwright, `kultur=de-DE`): **vorher ein Befund, nachher kein
Verstoß** in 36 Läufen (9 Fälle × 4 Breiten, Rückgabe 0). Der Befund stand im Foto bei 390 px; die Probe misst
ihn jetzt (Gegenprobe gegen den alten Stand: 20 Wortbrüche, Rückgabe 1).

| Befund | vorher | Ursache | Behebung |
|---|---|---|---|
| Periodenliste bricht im Wort | 20 Wörter bei 390 px („Karfrei\|tag“, „Weihnachts\|tag“, Datumsangaben) | fünf Spalten neben den 2 × 2 Knöpfen in 314 px Behälter, `overflow-wrap: anywhere` | unter 600 px fallen „Art“ und „Von–Bis“ (der Zeitraum leise unter dem Namen, wo er anders lautet als der Name), Rang 2,25 rem, Wert 4 rem — Wache `StilblattTests`, bunit `PeriodenlisteTests` |

| Fenster | Karte | Wochenraster | Periodenliste | Ziele im Blatt |
|---|---|---|---|---|
| 390 × 844 | 340 px | 314 px → 4 Zeilen je Tag | ohne „Art“ und „Von–Bis“, 0 Wortbrüche | 267 |
| 820 × 1 180 | 753 px | 727 px → 2 Zeilen je Tag | alle Spalten, 0 Wortbrüche | 267 |
| 1 180 × 820 | 1 099 px | 1 073 px → 2 Zeilen je Tag | alle Spalten, 0 Wortbrüche | 313 |
| 1 300 × 900 | 1 214 px | 1 188 px → 1 Zeile je Tag | alle Spalten, 0 Wortbrüche | 313 |

Querrollen, Bedienziele unter 44 px, Überdeckungen und Herausragen: je 0. Das Teppichbild zählt in jeder
Breite 405 Elemente, 354 davon mit Wert; am ersten Feld steht „Mi 01.01., 0–24 Uhr: aus · Betriebsruhe
zwischen den Jahren (Zeitraum)“. Die übrigen acht Fälle messen wie im Ergebnis der Welle U4; der Reiter
„Konditionierung“ steht in der neuen Reiterfolge an zweiter Stelle, Matrix und Karten wie dort. Die Fotos
liegen außerhalb des Repositorys.

**Welle U5 — die Abkürzung (E57).** Der Fall `vorlagen` misst zusätzlich die Liste „alle Größen“ in der
Kopfzelle der Zeile „Vorlage“ (≥ 44 px, Platzhalter „—“, die Namen in der Reihenfolge des Kerns), die eine
Rückfrage mit fünf Größenzeilen und der Vorgabe „Nein“, nach „Nein“ keine Herkunft, nach „Ja“ „Büro“ in allen
fünf Karten und Zellen der Zeile und die Wahl wieder auf „—“; die Texte, an denen sie misst, liest die Probe
je Kultur aus einer Tafel, sie läuft mit `--kultur de-DE` und `--kultur en-US` — am 02.10.2026 je 36 Läufe
(9 Fälle × 4 Breiten) ohne Verstoß, Rückgabe 0, die Liste 102,5 × 44 px in allen vier Fenstern.

---

## Kalenderprobe (Kalenderbedienung Stufe 2) — Seite `/konditionierungsprobe?fall=karte`

**Zweck.** Die Kalenderbedienung (`EPOS.UI/Dialoge/Bedarf/KalenderbedienungAbschnitt.razor`) im echten Browser:
Jahresraster, Maske „gilt für“, benannte Wochen und Schnellfelder ordnen sich über Stilregeln an, die bunit nicht
misst. Die Probe nimmt die Seite der Konditionierungsprobe im Fall `karte` (kein eigener Wirt, keine eigene
Probenseite), wählt den Reiter „Konditionierung“ und klappt die Karte „Heizen“ im Einzelnen auf.

```bash
node kalenderprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--ohne-gegenprobe]
```

| Fenster | Gemessen | Sollwert |
|---|---|---|
| 1 280 × 800 | Jahresraster: Monate, Zellen (Tage und Leerzellen), Zeilen nach Lage, kleinste Tageszelle | 12 × 31 = 372 Zellen, 365 Tage, 12 Zeilen, jede Tageszelle ≥ 20 px |
| 1 280 × 800 | Maske „gilt für“: nach „Kalender anlegen“ an der Lüftung eine neue Zeile „Probezeile“ (10.–14.03., Wirkung „aus“), ihre Maskenknöpfe, ein Klick auf „Lüftung“ | fünf Knöpfe, der Klick schaltet `aria-pressed` um |
| 1 280 × 800 | Block „Benannte Wochen“ (samt „Woche anlegen“), Schnellfelder Wochenende und Feiertagsland | sichtbar, sieben Tagesknöpfe |
| 1 024 × 700 | horizontales Rollen von Seite, Überlagerung (samt rollender Kästen außerhalb der Bedienung), Kalenderbedienung und Rollkasten des Jahresrasters | je 0 px |
| je Fenster | Konsole (`console.error`, Seitenfehler) | fehlerfrei |

**Gegenprobe.** Dieselbe Messung im absichtlich verengten Fenster 360 × 700 mit den Sollwerten des Breitfensters muss
rot werden (dort rollen Überlagerung und Kalenderbedienung quer); bleibt sie grün, ist der Lauf rot. Rückgabe `0` =
alle Sollwerte erfüllt und Gegenprobe rot, `1` = Verstoß, `2` = Aufruf- oder Verbindungsfehler.

## Fensterprobe (Kopf+Fuß fest) — Seite `/fensterprobe`

**Zweck.** Ein Dialog im eigenen Fenster (`BlazorDialogForm`) rollt als **Dokument**. Nach dem
Anwenderentscheid vom 30.09.2026 („Kopf+Fuß fest") haften dort die Kopfzeile und die Schlussleiste am
Fenster, nur der Inhalt dazwischen rollt — `epos-ui.css`, Abschnitt „Dialog im eigenen Fenster", an die
`Fenstermarke` gebunden, die allein die Fensterwurzel von `BlazorDialogForm` in `#app` hinter den Dialog zeichnet. Die Seite stellt echte Dialoge
so in den Browser, wie die WebView2 sie zeigt: `body > div#app > .epos-dialog`, dahinter die Marke — ohne
Datenbank. bunit misst weder Lage noch Rollstand; die Voraussetzungen der Regel hält die Wache
`EPOS.UI.Tests/FensterrahmenTests`.

| Adresse | Fall |
|---|---|
| `/fensterprobe?fall=heizkessel` | `HeizkesselDialog`, zwei Projektzeilen, 40 Katalogzeilen, Kostenknöpfe (eine Knopfzeile mitten im Inhalt) |
| `…?fall=bhkw` | `BhkwDialog`, ebenso |
| `…?fall=waermepumpen` | `WaermepumpenDialog` mit eingebetteter Detailansicht; die Temperaturprüfung schlägt an (Warnband im Fußblock der Detailansicht) |
| `…?fall=gebaeude` | `GebaeudeDialog`; „Simulation…" öffnet den Wärmebedarf als Überlagerung |
| `…?fall=dubletten` | `KatalogDublettenDialog` — das Protokoll steht **unter** der Schlussleiste |
| `…?fall=katalog` | `BedarfAdminDialog` (Katalogdialog, `.epos-katalog-dialog`) |
| `…&marke=0` | ohne Fenstermarke — der Dialog steht wie ohne die Regel |
| `…&zeilen=<n>`, `…&kultur=de-DE\|en-US` | Katalogzeilen; Kultur und Sprache wie bei `/gebaeudeimport` |

```bash
node fensterprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--nur <fall>] [--ohne-gegenprobe]
```

Gemessen bei **1 088 × 624** (Fenstermaß des Anwenders) und **520 × 624** (`Fenstermass.MindestBreite`).
Je Fensterdialog: die Marke steht in `#app` hinter dem Dialog, der Dialog ist höher als das Fenster; bei Rollstand oben,
Mitte und Ende steht der Kopf bei `top 0` und die Schlussleiste mit `bottom` = Fensterhöhe (± 1 px), beide
über die volle Breite der Wurzel; am Ende steht alles, was im Markup vor der Leiste kommt, über ihr; keine
andere Leiste haftet, genau ein haftender Kopf; der Tabulator durch den ganzen Dialog legt kein Feld unter
Kopf oder Fuß und rollt nicht, wenn er in Kopf oder Fuß landet. Wärmepumpen: OK bei Rollstand 0 — das Band
des Wirts steht oben im Bild, das Band der Detailansicht über der Schlussleiste und unverdeckt
(`elementFromPoint`). Dublettenprüfung: oben haftet der Fuß, am Ende steht er ganz im Bild und das Protokoll
frei darunter. Überlagerung (Gebäude → „Simulation…"): ihr Fuß haftet an ihrem Boden, der Kopf des
Unterdialogs haftet nicht, Kopf und Fuß des Fensters liegen unter der Abdunkelung, das Dokument rollt nicht —
und jede Zahl ist dieselbe wie mit `marke=0`. Katalogdialog: Kopf und Fuß statisch, das Dokument rollt nicht,
jede Zahl dieselbe wie mit `marke=0`.

**Gegenprobe** (läuft mit): dieselben Fensterdialoge mit `marke=0` müssen die Haft-Kriterien verfehlen; mit
Marke, aber `scroll-padding: 0` muss der Tabulator Felder unter Kopf oder Fuß legen; das eingebettete
Warnband mit `bottom: 0` muss verdeckt sein; der Fuß der Dublettenprüfung mit negativem Rand muss das
Protokoll überdecken. Rückgabe `0` = kein Verstoß **und** Gegenprobe rot, `1` = Verstoß oder Gegenprobe
grün, `2` = Aufbaufehler. Die Probe steht in keiner CI.

**Ergebnis vom 30.09.2026** (Wirt Release auf Port 5299, Chromium headless über das globale Playwright,
`kultur=de-DE`): **kein Verstoß, Gegenprobe rot** (Rückgabe 0).

| Fall | Fenster | Kopf / Fuß | Dokument / Rollweg | am Ende | Tabulator |
|---|---|---|---|---|---|
| Heizkessel | 1 088 × 624 | 71 / 69 px | 1 437 / 813 px | Inhalt bis 520 px, Leiste 555–624 px | 72 Schritte, 0 verdeckt, 0 Sprünge |
| BHKW | 1 088 × 624 | 71 / 69 px | 1 485 / 861 px | Inhalt bis 520 px, Leiste 555–624 px | 73, 0, 0 |
| Wärmepumpen | 1 088 × 624 | 71 / 69 px | 2 014 / 1 390 px | Inhalt bis 535 px, Leiste 555–624 px | 78, 0, 0 |
| Gebäude | 1 088 × 624 | 71 / 69 px | 1 253 / 629 px | Inhalt bis 520 px, Leiste 555–624 px | 73, 0, 0 |
| Dublettenprüfung | 1 088 × 624 | 71 / 69 px | 781 / 157 px | Leiste 347–416 px, Protokoll 426–608 px | 8, 0, 0 |
| Heizkessel | 520 × 624 | 71 / 69 px | 1 821 / 1 197 px | Inhalt bis 520 px, Leiste 555–624 px | 66, 0, 0 |
| BHKW | 520 × 624 | 71 / 69 px | 1 913 / 1 289 px | Inhalt bis 520 px, Leiste 555–624 px | 69, 0, 0 |
| Wärmepumpen | 520 × 624 | 71 / 69 px | 2 621 / 1 997 px | Inhalt bis 535 px, Leiste 555–624 px | 72, 0, 0 |
| Gebäude | 520 × 624 | 71 / **125** px (zweizeilig) | 1 665 / 1 041 px | Inhalt bis 464 px, Leiste 499–624 px | 65, 0, 0 |
| Dublettenprüfung | 520 × 624 | 71 / 125 px | 883 / 259 px | Leiste 292–417 px, Protokoll 427–608 px | 8, 0, 0 |

Wärmepumpen, Band der Detailansicht: 515–555 px bei 1 088 px (Leiste ab 555), 497–555 px bei 520 px;
das Band des Wirts steht bei Rollstand 0 unter dem Kopf (109–149 px). Überlagerung: 31–593 px hoch, ihr Fuß
523–592 px oben wie unten gerollt (Boden der Überlagerung 592 px), Kopf des Unterdialogs `static`, Fenster-
kopf und -fuß unter der Abdunkelung; mit und ohne Marke dieselben Zahlen. Katalogdialog: Kopf 16–62 px,
Fuß 564–608 px, Dokument 624 px, beide `static`; mit und ohne Marke dieselben Zahlen. Gegenprobe: ohne Marke
12 Haft-Verstöße je Fensterdialog (Dublettenprüfung 7 bzw. 8), ohne `scroll-padding` 5 bis 10 verdeckte
Felder je Dialog, eingebettetes Band mit `bottom: 0` verdeckt, Dublettenfuß mit negativem Rand über dem
Protokoll.

**Grenzen.** Einen **schreibgeschützten** Textbereich rollt Chromium beim Fokus nicht ins Bild (ein
bearbeitbarer rollt) — mit und ohne die Regel; die Probe misst dort nur, ob Kopf oder Fuß verdecken, was von
ihm im Fenster steht (Dublettenprüfung: Protokoll bei 520 px unterhalb des Fensters, Details angeschnitten).
Die Höhen im `scroll-padding` sind gerechnet, nicht gemessen: 71 und 69 px, unter 760 px Fensterbreite eine
Knopfzeile mehr. Die WebView2 selbst misst die Probe nicht — sie ist dasselbe Chromium; die Sichtprobe am
Gerät bleibt ein Abnahmepunkt unter Windows.

## Bannerprobe (Meldung im langen Fensterdialog) — Seite `/fensterprobe?meldung=1`

**Zweck.** Die langen Dialoge zeigen ihre Meldung als `Warnbanner` oben im Inhalt. Im eigenen Fenster haftet
der Kopf, der Inhalt rollt — wer unten arbeitet, sah die Meldung nicht. Ein `Warnbanner`, das unmittelbares
Kind der Dialogwurzel ist, haftet deshalb unter dem Kopf und über der Schlussleiste und trägt dort ein Kreuz zum
Ausblenden (`epos-ui.css`, „Dialog im eigenen Fenster"; Wache `EPOS.UI.Tests/FensterrahmenTests`). Der Schalter
`meldung=1` der Fensterprobenseite lässt die Trägerwahl von Heizkessel und BHKW fehlschlagen und gibt dem
Gebäudedialog „In DB übernehmen" (weich gesperrt, der Versuch meldet den Grund); ohne ihn steht die Seite wie
für die Fensterprobe.

```bash
node bannerprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--nur heizkessel|bhkw|gebaeude] [--ohne-gegenprobe]
```

Gemessen bei **1 088 × 624** und **520 × 624**, je Fall: Katalogsatz wählen (Heizkessel, BHKW), ans Ende rollen,
die Meldung per Skript auslösen (rollt nicht). Dann steht das Banner ganz im Fenster, unter dem Kopf und über der
Schlussleiste, unverdeckt (`elementFromPoint` an vier Ecken); Kopf (`top 0`) und Schlussleiste (`bottom` =
Fensterhöhe) haften; der auslösende Knopf steht, wo er stand (den Zuwachs oben gleicht der Scroll-Anker aus);
das Kreuz ist sichtbar und blendet aus; in Ruhe (Rollstand 0) steht das Banner, wo es ohne die Haftregel stünde.
Ohne Fenstermarke (wie Überlagerung, Blatt, iOS-Seite) ist es `static` und das Kreuz verborgen.

**Gegenprobe** (läuft mit): dieselben Fälle mit `position: static` am Banner müssen das Sichtkriterium verfehlen.
Rückgabe `0` = kein Verstoß **und** Gegenprobe rot, `1` = Verstoß oder Gegenprobe grün, `2` = Aufbaufehler. Die
Probe steht in keiner CI.

**Ergebnis vom 06.10.2026** (Wirt Release, Chromium headless, `kultur=de-DE`): **kein Verstoß, Gegenprobe rot**
(Rückgabe 0). Fensterprobe, Fokusprobe und Rollprobe „Berichte & Kosten" danach unverändert grün, ihre
Gegenproben rot.

| Fall | Fenster | Rollstand vor → nach | Banner (Kopf bis 71 px) | Schlussleiste ab | Gegenprobe `static` |
|---|---|---|---|---|---|
| Heizkessel | 1 088 × 624 | 761 → 813 px | 71–113 px | 555 px | −704…−662 px, 3 Verstöße |
| BHKW | 1 088 × 624 | 761 → 813 px | 71–113 px | 555 px | −704…−662 px, 3 |
| Gebäude | 1 088 × 624 | 629 → 681 px | 71–113 px | 555 px | −572…−530 px, 3 |
| Heizkessel | 520 × 624 | 1 171 → 1 239 px | 71–129 px (zweizeilig) | 555 px | −1 130…−1 071 px, 3 |
| BHKW | 520 × 624 | 1 193 → 1 261 px | 71–129 px | 555 px | −1 152…−1 093 px, 3 |
| Gebäude | 520 × 624 | 1 093 → 1 161 px | 71–129 px | 499 px | −1 052…−993 px, 3 |

In Ruhe steht das Banner bei 109–151 px (520 px: 109–168 px), mit und ohne Haftregel gleich.

**Grenzen.** Stehen zwei Banner zugleich als Kinder der Wurzel, haften sie an derselben Stelle übereinander. Das
Polster oben rechnet eine Bannerzeile; ein mehrzeiliges Banner lässt dem angesprungenen Feld weniger Luft.

## Rollbereichprobe „kein Rollbereich im Rollbereich" — Seiten `/rollbereichprobe`, `/fensterprobe`

**Zweck.** Die Projektdialoge mit Katalogauswahl (Baustein `Zweispaltenauswahl`, Variante V1 „Gerahmt und
gestapelt", [Konzept](../../Dokumentation/aktuell/Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md)
Abschnitt 4 und 8) füllen ihr Fenster; es rollen allein Projektliste, Katalogliste und die aufgeklappte
Detailzeile, keine in der anderen. bunit sieht weder Lage noch Rollbereich — die Probe misst es im echten
Chromium. Die Seite `/rollbereichprobe?fall=…` trägt ohne Datenbank Pufferspeicher, Stromspeicher,
Photovoltaik, Solarkollektoren, Bedarfsprofile, Wärmebedarf extern, Strom- und Solarganglinie und die
Kältemaschinenauswahl (`KaeltemaschineKatalogDialog`); Heizkessel, BHKW, Wärmepumpen und Gebäude nimmt die
Probe von `/fensterprobe`.

```bash
node rollbereichprobe.mjs --url http://127.0.0.1:5299 [--nur <fall>] [--ohne-gegenprobe]
```

**Fälle.** Dreizehn Dialoge × drei Fenster (1 280 × 800, 1 280 × 720, 1 024 × 700) × die Zustände Vorgabe,
Trennlinie oben (Pos1), Trennlinie unten (Ende), Detailzeile auf bei beiden Grenzen, Detailzeile wieder zu,
mit der Maus gezogen, neu geladen (die Höhe kommt über `Dienste.Einstellungen` wieder) und beim Gebäude die
offene Überlagerung „Simulation…". Je Zustand: kein sichtbares Element mit `overflow: auto|scroll` in einem
anderen; Dokument und Dialogkörper rollen nicht; kein Bereich und kein Bereichsinhalt läuft über; die
Katalogliste behält Kopf und zwei Zeilen (unter 600 px Bausteinhöhe Kopf und eine Zeile, Stilblatt
`@container katalogauswahl`); die Kopfleisten brechen nicht um und schneiden keinen Knopf ab; die Konsole
bleibt ohne Fehler. **Gegenprobe** (läuft mit): ein absichtlich rollender Inhalt des Katalogbereichs muss als
verschachtelter Rollbereich, ein rollender Dialogkörper als solcher erkannt werden. Rückgabe `0` = kein
Verstoß und Gegenprobe rot, `1` = Verstoß oder Gegenprobe grün, `2` = Aufbaufehler. Die Probe steht in keiner CI.

**Ganglinie mit verdichtetem Kopf (DZ1-N1).** In den vier Ganglinien-Fällen (Wärmebedarf extern, Strom-, Solar-,
PV-Ganglinie) misst die Probe aufgeklappt zusätzlich: Kopf der Satzfläche (bis zur Oberkante der Kurve, ohne Polster)
ab 1 024 px Breite höchstens 90 px — in 768 × 1 024 brechen die Kennzahlen um (93 px) —, Kurve mindestens
`--epos-kurve-min` (90 px), Satzfläche ohne eigenen Rollbalken, und in 1 280 × 800 und 1 280 × 720 rollt der
Dialogkörper bei Strom-, Solar- und PV-Ganglinie nicht. Beim Wärmebedarf extern gibt das Fenster die Untergrenze
(Kopf + 90 px) nicht her; dort rollt der Dialogkörper nach KB1 (Befund, 40 px in 1 280 × 800, 20 px in 1 280 × 720).
Die 1 280 × 720 erreicht in keinem Fall 180 px Kurve; die Untergrenze 90 px ergibt sich aus der Mindesthöhe des
engsten Ganglinien-Wirts ohne Rollen (Solar, PV: 194 px Satzfläche in 1 280 × 800). **Vierte Gegenprobe:** In der
Solarganglinie bei 1 280 × 800 muss eine auf 60 px gedrückte Kurve („Kurve < Untergrenze“) und eine Kurven-Untergrenze
von 300 px („Dialogkörper rollt“) rot werden. Mit `--fotos <ordner>` legt die Probe je Ganglinien-Fall und Fenster ein
Foto der aufgeklappten Detailzeile ab (`<fall>_<breite>x<hoehe>.png`).

**Ergebnis vom 10.10.2026, DZ1-N1:** 666 Zustände, 0 Verstöße, alle vier Gegenproben rot. Kurve (Detailzeile auf):

| Dialog | 1 280 × 800 | 1 280 × 720 | 1 024 × 768 | 1 024 × 700 | 1 093 × 614 | 768 × 1 024 |
|---|---|---|---|---|---|---|
| Wärmebedarf extern | 93 (rollt) | 94 (rollt) | 122 | 94 (rollt) | 94 (rollt) | 328 |
| Stromganglinie | 139 | 148 | 196 | 128 | 94 (rollt) | 328 |
| Solarthermie-, PV-Ganglinie | 96 | 112 | 160 | 94 (rollt) | 94 (rollt) | 328 |

**Kurve in voller Breite (DZ1-N2).** Die Zeichenfläche nimmt ihre Größe vom Behälter, und das Zeichenmodell entsteht
in genau dieser Größe (`BildauftragMass`, Behältermaß von `DiagrammSvg`). Die Untergrenze der Kurve ist 150 px. Die
Probe misst aufgeklappt zusätzlich: Kurve mindestens 90 % so breit wie die Satzfläche (ohne Polster), das Modell in
Behältergröße (viewBox gegen die Fläche, je höchstens 3 px), und bei Strom-, Solar- und PV-Ganglinie rollt der
Dialogkörper in 1 280 × 800 und 1 280 × 720 nur, solange die Kurve auf ihrer Untergrenze steht (höchstens 5 px
darüber; KB1). Vor dem Messen wartet sie, bis das Modell im gemessenen Maß steht. Je Zustand schreibt sie eine Zeile
„Kurve B × H px, Satzfläche, Modell, Dialog rollt“. **Vierte Gegenprobe** (Solarganglinie, 1 280 × 800): eine auf 60 px
gedrückte Kurve, eine auf 300 px Breite begrenzte Kurve und eine Untergrenze der Satzfläche von 600 px (der
Dialogkörper rollt, obwohl die Kurve über ihrer Untergrenze steht) müssen rot werden.

**Ergebnis vom 10.10.2026, DZ1-N2:** 666 Zustände, 0 Verstöße, alle Gegenproben rot. Kurve Breite × Höhe in px
(Detailzeile auf; Satzfläche jeweils gleich breit wie die Kurve), in Klammern der Überhang des rollenden Dialogkörpers:

| Dialog | 1 280 × 800 | 1 280 × 720 | 1 024 × 768 | 1 024 × 700 | 1 093 × 614 | 768 × 1 024 |
|---|---|---|---|---|---|---|
| Wärmebedarf extern | 1 110 × 153 (100) | 1 118 × 154 (80) | 982 × 154 (32) | 982 × 154 (100) | 1 051 × 154 (186) | 726 × 377 |
| Stromganglinie | 1 110 × 153 (14) | 1 118 × 154 (6) | 982 × 196 | 982 × 154 (26) | 1 051 × 154 (112) | 726 × 451 |
| Solarthermie-, PV-Ganglinie | 1 110 × 153 (57) | 1 118 × 154 (42) | 982 × 160 | 982 × 154 (62) | 1 051 × 154 (148) | 726 × 396 |

**Heizkessel (Stufe 2a).** Zusätzlich je Fenster: Detailzeile auf mit Kosten (die Satzfläche muss die
Kostenknöpfe tragen), Überlagerung „Bearbeiten…" für die Projektkopie und für zwei angekreuzte Katalogsätze
(Blätterleiste oder Hinweiszeile „übersprungen"), danach geschlossen mit Esc. Für den Heizkessel gilt in
jedem Zustand und jedem Fenster Kopf **und zwei** Katalogzeilen; die Zeilenhöhe misst die Probe an der Liste
(Kästchenmodus 46 px, sonst 53 px).

**Ergebnis vom 09.10.2026, Stufe 2a** (Wirt Release, Chromium headless, Playwright aus
`/opt/node-tools/node_modules`): **306 Zustände, 0 Verstöße, Gegenprobe rot** (verschachtelt 1 Paar,
Dialogkörper rollt) — Rückgabe 0. Stufe 1 maß 294 Zustände; dazu kommen zwölf des Heizkessels. Vorgabe der
Trennlinie (Höhe des Bausteins / Projektliste / Katalogliste in px, Kopf der Katalogliste 53 px); die Suche
steht seit Stufe 2 in der Kopfleiste des Katalogs, deshalb gewinnen alle Wirte eine Zeilenhöhe:

| Dialog | 1 280 × 800 | 1 280 × 720 | 1 024 × 700 |
|---|---|---|---|
| Heizkessel (Kontext im Dialogkopf, Zeile 46 px) | 648 / 138 / 259 | 568 / 138 / 179 | 548 / 138 / 159 |
| BHKW, Gebäude, Pufferspeicher, Photovoltaik, Solarkollektoren, Bedarfsprofile, Wärmebedarf extern | 620 / 138 / 226 | 540 / 138 / 146 | 520 / 138 / 126 |
| Wärmepumpen | 620 / 138 / 230 | 540 / 138 / 150 | 520 / 138 / 130 |
| Stromspeicher | 620 / 138 / 270 | 540 / 138 / 190 | 520 / 138 / 170 |
| Stromganglinie | 648 / 138 / 312 | 568 / 138 / 232 | 548 / 138 / 212 |
| Solarganglinie | 648 / 138 / 161 | 568 / 138 / 108 | 548 / 138 / 108 |

Heizkessel bei Trennlinie unten, Detailzeile auf und „Detailzeile auf mit Kosten": Katalogliste 147 px
(Kopf + 2 × 46 px), Projektliste 250 / 170 / 150 bzw. 160 / 120 / 110 px; Überlagerung „Bearbeiten…"
offen: Liste unverändert, drei Rollbereiche (Projekt, Katalog, Überlagerung), keiner im anderen.

**Heizkessel (Stufe 2b).** Nach „Bearbeiten…" öffnet die Probe je Fenster die Rückfrage „In die Datenbank
übernehmen…" (Wirt: drei Zeilen — überschreibbar, gesperrt, Ursprung nicht bekannt, ein langer Name), misst den
Zustand „Rückfrage-Überlagerung offen" und schließt sie mit Esc; rot, wenn die drei Zeilen fehlen oder Esc nicht
schließt. **Ergebnis vom 09.10.2026, Stufe 2b:** **309 Zustände, 0 Verstöße, Gegenprobe rot** (verschachtelt 1 Paar,
Dialogkörper rollt) — Rückgabe 0; Liste unverändert (648 / 138 / 259, 568 / 138 / 179, 548 / 138 / 159), drei
Rollbereiche (Projekt, Katalog, Überlagerung), keiner im anderen — die Rückfrage hat keinen eigenen. Am selben Stand
`fensterprobe.mjs`, `bannerprobe.mjs` und `katalogprobe.mjs` grün; `rasterprobe.mjs` wie vor (Z6 verfehlt, fremd).

Außerhalb des Bausteins meldet die Probe als **Befund, nicht gezählt**: die Kältemaschinenauswahl ist ein
Katalogdialog (`.epos-katalog-dialog` mit `overflow: auto` als Notnagel um Liste und Stammblatt) und kein
Wirt des Bausteins; die Überlagerung „Simulation…" des Gebäudes rollt als Ganzes um zwei Listen. Beide je
drei Fenster.

**Grenzen.** Bei 720 und 700 px Fensterhöhe behält der Heizkessel Kopf und zwei Katalogzeilen; die
Wirte ohne eigenes Zeilenmaß (Stufe 3 und 4: Kontextzeile noch unter dem Kopf) klemmen dort weiter auf
Kopf und eine Zeile (`@container katalogauswahl`). Die Probe misst Chromium, nicht die WebView2 selbst.

Die **Fensterprobe** misst für Heizkessel, BHKW, Wärmepumpen und Gebäude „nichts rollt außer den
Listen und der Detailzeile" (Dokument und Dialogkörper rollen nicht, Kopf und Schlussleiste statisch im
Bild, auch mit aufgeklappter Detailzeile und über den ganzen Tabulatorweg; Gegenprobe: ein rollender
Dialogkörper wird rot); die Haft-Kriterien gelten nur noch für die Dublettenprüfung. Die **Bannerprobe**
misst in diesen drei Fällen, dass das Banner unter dem Kopf im Bild steht, sein Kreuz trägt und das
Dokument nicht rollt.

**Stufe 2a, 09.10.2026:** Der Heizkessel trägt seine Kontextzeile im Dialogkopf; das Banner steht damit
unmittelbar unter einem Kopf, der niedriger ist als die geschätzte Kopfhöhe `--epos-fenster-kopf`, und die
Haftregel schob es in Ruhe um 15 px nach unten (Bannerprobe: „in Ruhe 87 px, ohne Haftregel 72 px"). In der
Katalogauswahl rollt der Dialogkörper nicht; dort haftet das Banner deshalb mit `top: 0`. Danach:
Bannerprobe erfüllt (Gegenprobe rot), Fensterprobe erfüllt (Gegenprobe rot), Katalogprobe 59 Fälle ohne
Überlagerung, Rasterprobe 28 von 29 (Z6 `kategorien_ueberlagerung`, quer 47 px, war vorher schon rot).
**Kompaktstufe und Rollbalken (KB1).** Drei weitere Fenster: 1 024 × 768 und 768 × 1 024 (iPad quer
und hoch), 1 093 × 614 (Laptop bei 125 %). Die Stufe misst die Probe in jedem Fenster nach der
Medienabfrage `(max-width: 1199.98px), (max-height: 799.98px)` — Normalstufe nur in 1 280 × 800, die
Kompaktstufe auch in 1 280 × 720 und 1 024 × 700. Je Zustand zusätzlich: Schrift 13 bzw. 12 px,
Projektzeile 53 bzw. 46 px, Katalogzeile 53/46 bzw. 46/40 px (mit Zeilenmaß), Projektliste mindestens ihre
Untergrenze, alle Knöpfe der Schlussleiste nach dem Rollen des Dialogkörpers sichtbar und treffbar (gerollt
wird nur, wenn der Dialog einen bedienbaren Rollbalken hat). In der Kompaktstufe gilt Kopf und **zwei**
Katalogzeilen auch unter 600 px Bausteinhöhe. Der Dialogkörper darf nur in den drei neuen Fenstern rollen und
nur mit `data-zweispalten-eng` (Fenster unter der Mindesthöhe) — gezählt als Befund; in den drei bisherigen
Fenstern bleibt jeder rollende Dialogkörper ein Verstoß. Mit `--fotos <ordner>` legt die Probe Bilder von
Heizkessel und Gebäude in den drei neuen Fenstern ab. **Zweite Gegenprobe (Rollbalken):** ein 400 px hoher
Klotz im Heizkessel bei 1 093 × 614 muss den Dialog eng schalten, Projektliste (74 px) und Katalogliste
(128 px) auf ihrer Untergrenze lassen und die Schlussleiste erreichbar halten; derselbe Klotz mit
`overflow: hidden` am Dialog muss die Schlussleiste unerreichbar melden.

**Ergebnis vom 10.10.2026, KB1** (Wirt Release, Chromium headless): **618 Zustände, 0 Verstöße**, beide
Gegenproben rot (verschachtelt 1 Paar, Dialogkörper rollt; ohne Rollbalken „Abbrechen, OK“ unerreichbar) —
Rückgabe 0. Gemessen (Bausteinhöhe / Projektliste / Katalogliste in px, Vorgabe der Trennlinie):

| Dialog | 1 280 × 800 | 1 280 × 720 | 1 024 × 700 | 1 024 × 768 | 768 × 1 024 | 1 093 × 614 |
|---|---|---|---|---|---|---|
| Heizkessel (Zeile 53 / 46 bzw. 46 / 40) | 648 / 138 / 259 | 596 / 120 / 253 | 576 / 120 / 233 | 644 / 120 / 301 | 900 / 120 / 557 | 490 / 120 / 147 |
| Gebäude, Wärmepumpen (Kontextzeile) | 620 / 138 / 226 | 571 / 120 / 225 | 551 / 120 / 205 | 619 / 120 / 273 | 875 / 120 / 529 | 465 / 99 / 140 |
| Stromganglinie | 648 / 138 / 312 | 596 / 120 / 299 | 576 / 120 / 279 | 644 / 120 / 347 | 900 / 120 / 603 | 490 / 120 / 193 |

Kompaktstufe: Schrift 12 px, Kartentitel 14 px, Knöpfe und Kopfleisten 37 px, Projektzeile 46 px,
Katalogzeile 46 px (Kästchenmodus 40 px), Untergrenze der Projektliste 74 px, Katalogliste 128 px (Heizkessel)
bzw. 140 px. Detailzeile auf: Projekt- und Katalogliste auf ihrer Untergrenze, die Detailzeile zeigt
mindestens 80 px ihres Inhalts; in 1 093 × 614 reicht das Fenster dafür nicht — dort rollt der Dialogkörper
(23 Zustände, alle „Detailzeile auf“), sonst in keinem Zustand. Im schmalen Bereich (768 px) tritt die
Überschrift der Kopfleiste hinter die Marke zurück, die Pfeile der Knöpfe entfallen, und das Summenfeld des
BHKW behält die Beschriftung neben dem Feld: Die Kopfleisten bleiben einzeilig (37 px).

Dieselbe Ausnahme kennen **Fensterprobe** und **Bannerprobe**: Trägt der Dialog `data-zweispalten-eng`,
darf der Dialogkörper rollen (Befund, nicht gezählt), Kopf und Schlussleiste bleiben `static`, und die
Schlussleiste muss am Ende des Rollwegs im Fenster stehen; die Gegenprobe „rollender Dialogkörper“ der
Fensterprobe sperrt den Schalter und bleibt rot. Am Stand KB1 grün: Fensterprobe (Befund in 1 088 × 624
und 520 × 624 mit aufgeklappter Detailzeile, beim Gebäude in 520 × 624 auch zugeklappt — die zweizeilige
Schlussleiste), Bannerprobe (BHKW und Gebäude in beiden Fenstern), Katalogprobe, Legendenprobe. Die
**Rasterprobe** misst in (c) das gesetzte Zeilenmaß mal `--epos-zeilenskala` der Hülle: GD1 bis GD3
(Gebäude-Projektdialog, 624 px Höhe, also Kompaktstufe) 46,0 px bei `ItemSize` 53, keine Platzhalter,
4 Sichtbarkeitsmelder nach dem Rollen; verfehlt bleibt allein Z6 (fremd).

**Vorrang der Detailzeile (DZ1, Konzept 4.4 und 4.8).** Je Zustand mit aufgeklappter Detailzeile misst die
Probe zusätzlich: Projektliste und Katalogliste höchstens 2 px über ihrer Untergrenze, die Satzfläche reicht
bis an den unteren Rand des Bausteins (Rest höchstens 3 px); trägt sie eine Ganglinie, ist die Zeichenfläche so
hoch wie der Platz, den Kennzahlen, Schalter und Zoomleiste lassen (oder so breit wie die Satzfläche). Die
Tabelle am Ende nennt die Höhe der Satzfläche (und Bild/Platz der Ganglinie). **Dritte Gegenprobe:** Heizkessel
in 1 280 × 800 mit einer auf 100 px begrenzten Satzfläche — sie lässt 66 px frei und muss rot werden.
**Kopfleiste der Projektliste.** Mit aufgeklappter Detailzeile rechnet die Rasterzeile der Projektliste mit einer Kopfleiste in Touchzielhöhe; die Probe meldet jede höhere Kopfleiste (ein Rand am Knopf im Leistenzusatz ließ die Wärmepumpen 6 px überlaufen). Gegenprobe: Wärmepumpen in 1 280 × 800 mit 6 px Rand am Umstellknopf muss rot werden.
**Ergebnis vom 10.10.2026, DZ1, Teil 1:** 618 Zustände, 0 Verstöße, alle drei Gegenproben rot. Satzfläche
aufgeklappt (Trennlinie oben) in px:

| Dialog | 1 280 × 800 | 1 280 × 720 | 1 024 × 700 | 1 024 × 768 | 768 × 1 024 | 1 093 × 614 |
|---|---|---|---|---|---|---|
| Heizkessel (Kosten, Alle Daten) | 165 | 172 | 152 | 220 | 476 | 97 |
| Gebäude | 118 | 132 | 112 | 180 | 436 | 97 |

**Ganglinie in der Detailzeile (DZ1, Teil 2, Konzept 4.9).** Der Wirt trägt die vier Ganglinien-Dialoge mit
synthetischer Reihe (Kennzahlen und ein Zeichenmodell je Schalterstellung): `fall=waermebedarf`, `stromganglinie`,
`solarganglinie` und neu `pvganglinie`. Die Probe wählt dort vor dem Aufklappen die erste Projektzeile und
verlangt in der Satzfläche eine Zeichenfläche der Ganglinie. Deren Untergrenze (Kopf plus 150 px Kurve, DZ1-N2) gehört zur Mindesthöhe:
Reicht das Fenster nicht, rollt der Dialogkörper (Befund, auch in den Fenstern ohne Kompaktstufe), das Bild hat
keinen eigenen Rollbalken. **Ergebnis vom 10.10.2026 (vor DZ1-N1, Kopf unverdichtet, Untergrenze 260 px):** 666 Zustände,
0 Verstöße, drei Gegenproben rot. Satzfläche (Bild/Platz der Zeichenfläche) in px, Trennlinie oben:

| Dialog | 1 280 × 800 | 1 280 × 720 | 1 024 × 700 | 1 024 × 768 | 768 × 1 024 | 1 093 × 614 |
|---|---|---|---|---|---|---|
| Wärmebedarf extern | 276 (139/139) | 277 (148/148) | 277 (148/148) | 277 (148/148) | 436 (307/307) | 277 (148/148) |
| Stromganglinie | 276 (139/139) | 277 (148/148) | 277 (148/148) | 277 (148/148) | 510 (328, volle Breite) | 277 (148/148) |
| Solarthermieganglinie | 276 (136/136) | 277 (144/144) | 277 (144/144) | 277 (144/144) | 502 (213/213) | 277 (144/144) |
| PV-Ganglinie | 276 (136/136) | 277 (144/144) | 277 (144/144) | 277 (144/144) | 502 (144/144) | 277 (144/144) |

Außer in 768 × 1 024 rollt mit aufgeklappter Ganglinie der Dialogkörper (`data-zweispalten-eng`). Die
**Fensterprobe** misst aufgeklappt ebenso Listen auf Untergrenze und den Rest der Satzfläche; ihre Gegenprobe
begrenzt die Satzfläche auf 40 px und muss rot werden.

## Legendenprobe (Ringlegende der Ergebnisübersicht) — Seite `/legendenprobe`

**Zweck.** Die Legende neben den Ringen der Ergebnisübersicht (Wärme, Strom, Kälte) steht in einer
Ringzeile, die ein Container ist (`epos-ui.css`, „Ring und Legende"; Markup einmal in
`UebersichtReiter.Legende`): Ist die Ringzeile schmaler als 520 px, steht die Legende unter dem Ring;
ein Eintrag ist eine umbrechende Flexzeile, deren Zahlenpaar (Menge, Anteil) als Ganzes in die zweite
Zeile rückt, wenn neben dem Namen kein Platz ist; der Name bricht nur an Wortgrenzen. Die Seite stellt
den echten `UebersichtReiter` mit synthetischen Ringdaten für Wärme, Strom und Kälte und langen Namen
wie im Betrieb („Wärmepumpe Luft/Wasser Kaskade 1", „Fernwärmeübergabestation"); die Probe setzt die
Breite des Rahmens `#legendenprobe-rahmen` per Skript. bunit hält die Struktur
(`EPOS.UI.Tests/Seiten/UebersichtReiterTests`, alle drei Ringe dieselben Klassen, die Regel als Regel).

```bash
node legendenprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--ohne-gegenprobe]
```

**Fälle** (Fenster × Rahmen, Höhe 900 px): 1 280 voll, 1 280 mit Rahmen 1 100 px, 1 280 mit Rahmen
760 px (zwei schmale Spalten), 480 voll, 360 voll. Je Ringzeile: unter 520 px steht die Legende unter dem
Ring, sonst daneben; kein Eintrag ist höher als zwei Zeilenhöhen (Zeilenhöhe als Blockkopie in der
Schrift des Namens gemessen, die Summenzeile mit ihrem Rand von 5 px); kein Wort ist gebrochen (Breite
jedes Namenselements ≥ Breite seines längsten Worts, per Range an einer ungebrochenen Kopie gemessen,
und jedes Wort liegt auf einer Zeile, `Range.getClientRects`); das Zahlenpaar steht im Eintrag.

**Gegenprobe** (läuft mit): dieselben Fälle mit der alten Regel (Raster neben dem Ring bis 620 px
Fensterbreite, `overflow-wrap: anywhere` am Namen) müssen Verstöße liefern. Rückgabe `0` = kein Verstoß
**und** Gegenprobe rot, `1` = Verstoß oder Gegenprobe grün, `2` = Aufbaufehler. Die Probe steht in keiner CI.

**Ergebnis vom 09.10.2026** (Wirt Release, Chromium headless, `kultur=de-DE`): **0 Verstöße, Gegenprobe
rot mit 103 Verstößen** (Rückgabe 0).

| Fall | Ringzeilen Wärme / Strom / Kälte | Lage | höchster Eintrag | Gegenprobe |
|---|---|---|---|---|
| 1 280 voll | 630 / 630 / 1 278 px | neben | 2,00 Zeilen | 13 Verstöße |
| 1 280, Rahmen 1 100 | 540 / 540 / 1 098 px | neben | 2,27 Zeilen (Summenzeile mit Rand) | 17 |
| 1 280, Rahmen 760 | 370 / 370 / 758 px | unter, unter, neben | 2,00 Zeilen | 45 |
| 480 voll | 478 / 478 / 478 px | unter | 1,27 Zeilen | 13 |
| 360 voll | 358 / 358 / 358 px | unter | 2,00 Zeilen | 15 |

## Diagrammprobe (Zeigerbalken im Zoom, Auftrag GX) — Seite `/diagrammsvg`

**Wozu.** Der senkrechte Balken an der Mausstelle eines Diagramms (`DiagrammSvg`) soll der Maus im selben Bild
folgen, auch bei ×12. Er gehört dem Modul `epos-diagramm.js`: gesetzt unmittelbar im `pointermove`, die Zeile
darunter aus der Zeigertafel, die der Baustein einmal je Zeichnen mitgibt — kein Rundlauf nach .NET je Bewegung.
Die Stelle rechnet das Modul gegen den **Viewport** der Datenfläche (Bildschirmmatrix des äußeren svg), nicht gegen
`getBoundingClientRect()` des inneren svg — das meldet im Zoom die Hülle der Pfade, ein Vielfaches zu breit.
bunit hat weder Layout noch JavaScript; die Probe misst es im Chromium.

```bash
node diagrammprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--ohne-gegenprobe]
```

**Ablauf und Sollwerte.** Seite laden, per Rad auf ×12 zoomen, warten, bis die Zeile dem Modul gehört
(`.epos-diagramm-zeigerzeile--modul`), dann die Maus in 40 Schritten über die Datenfläche führen. Je Schritt,
unmittelbar nach der Bewegung: Balken sichtbar und **|Balkenmitte − Maus x| ≤ 1 px**; nach dem nächsten Bildaufbau
nennt die Zeile **Stunde und Datum**. Über die ganze Bewegung: die Pfade der Reihen sind **dieselben Elemente** mit
unverändertem `d`, **0 Mutationen** (MutationObserver auf der Datenfläche).

**Gegenprobe** (läuft mit): Die Probe liefert das Modul so aus, dass es die Tafel ablehnt — dann zeichnet der Baustein
Linie und Zeile nach einem Rundlauf selbst, und die Linie steht nicht im Bild der Maus. Sie muss verfehlen.

**Ergebnis vom 10.10.2026** (Linux-Container, Chromium headless, Wirt Blazor Server): Stufe ×12, 41 Schritte,
größter Abstand **0,50 px**, Zeile mit Datum **41/41**, 3 Pfade, **0 Mutationen** — Rückgabe 0. Gegenprobe:
13 von 41 Schritten über 1 px (verfehlt, richtig).

---

## Tabellenprobe (Ergebnistabellen der Simulation, Auftrag TA) — Seite `/tabellenprobe`

**Wozu.** In den Ergebnistabellen der Simulationsreiter standen die Köpfe links über breiten Spalten, die Werte
rechts am Zellenrand — Kopf und Wert lagen nicht übereinander. Die Regel: Eine Zahlenspalte trägt
`.epos-simerg-zahl` an Kopf **und** Zellen (rechtsbündig), eine Textspalte an keinem (linksbündig); die Tabelle
(`table.epos-simerg-tabelle`) nimmt die Breite ihres Inhalts, jede ihre eigene. bunit prüft die Klassen
(`EPOS.UI.Tests/Seiten/Tabellenausrichtung.cs`), nicht die Lage; die Probe misst sie im Chromium. Die Seite stellt
den echten `WaermepumpeReiter` mit einem Modul und zwei Speichern.

```bash
node tabellenprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--ohne-gegenprobe]
```

**Sollwerte.** Fälle 1280 px, 1280 px mit Rahmen 760 px, 480 px. Je Tabelle (Modul, Speicher) und Zahlenspalte:
**|rechter Rand des Kopftexts − rechter Rand des Werts| ≤ 2 px** in jeder Datenzeile; je Textspalte dasselbe für
die linken Ränder. Passt der Inhalt in den Behälter, ist die Tabelle **nicht breiter als ihr Inhalt**
(`width: max-content` + 1 px) — der Flex-Block `.epos-simerg-block` dehnte sie sonst auf volle Breite.
`TABELLENPROBE_JSON=1` hängt die Rohwerte an. Rückgabe `0`/`1`/`2` wie oben.

**Gegenprobe** (läuft mit): dieselbe Seite mit der alten Regel (Zahlenköpfe links, Tabelle 100 % breit) — sie muss
verfehlen.

**Ergebnis vom 10.10.2026** (Linux-Container, Chromium headless, Wirt Blazor Server): 6 + 6 Zahlenspalten, größter
Abstand **0,00 px** in allen drei Fällen, Breiten 948 / 1037 px statt 1280 px — Rückgabe 0. Gegenprobe:
12 Verstöße, größter Abstand 61,84 px (verfehlt, richtig).

---

## Kennzahlenprobe (Kennzahlenzeile der Ergebnisübersicht) — Seite `/legendenprobe`

**Zweck.** Die Kennzahlenzeile der Ergebnisübersicht (Wärme, Strom, Kälte mit zwei Bändern) ist ein
Container (`epos-ui.css`, „Das Kennzahlenband"; Markup einmal in `UebersichtReiter.Kennzahl`): Die Kacheln
stehen in einer umbrechenden Flexzeile, keine wird schmaler als ihr Inhalt (Zahl samt Einheit, längstes
Wort der Beschriftung), eine Kachel ohne Platz rückt in die nächste Zeile; ist das Band schmaler als 480 px,
stehen die Kacheln untereinander, je Kachel Beschriftung links und Zahl rechts. Zahl und Einheit stehen in
einem Element mit `white-space: nowrap`, die Beschriftung bricht nur an Wortgrenzen. Die Probe nutzt die
Seite der Legendenprobe (echter `UebersichtReiter`, Kälte mit Erzeuger) und setzt die Breite des Rahmens
`#legendenprobe-rahmen` per Skript. bunit hält die Struktur (`EPOS.UI.Tests/Seiten/UebersichtReiterTests`,
Zahl und Einheit in einem Element, die Regel als Regel).

```bash
node kennzahlenprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--ohne-gegenprobe]
```

**Fälle** (Fenster 1 280 × 900, Rahmen 1 100, 760 und 600 px). Je Kennzahlenband und Kachel: Beschriftung
und Zahl samt Einheit (per Range gemessen, die Ausdehnung des Textes) liegen in ihrer Kachel; kein
Inhaltsstück schneidet den Kasten einer anderen Kachel, keine zwei Kacheln überschneiden sich
(Bounding-Box-Vergleich); Zahl und Einheit liegen auf einer Zeile; kein Wort der Beschriftung ist gebrochen
(`Range.getClientRects`).

**Gegenprobe** (läuft mit): dieselben Fälle (a) mit der alten Regel (Drittelraster `minmax(0, 1fr)`,
`min-width: 0`) und (b) mit einer erzwungenen Überlappung (die Zahl jeder Folgekachel 80 px nach links)
müssen je Verstöße liefern. Rückgabe `0` = kein Verstoß **und** beide Gegenproben rot, `1` = Verstoß oder
eine Gegenprobe grün, `2` = Aufbaufehler. Die Probe steht in keiner CI.

**Ergebnis vom 10.10.2026** (Wirt Release, Chromium headless, `kultur=de-DE`): **0 Verstöße, Gegenprobe
alte Regel rot mit 16, erzwungene Überlappung rot mit 32 Verstößen** (Rückgabe 0).

| Rahmen | Bänder Wärme / Strom / Kälte / Kälte-Deckung | Anordnung | Gegenprobe alt / erzwungen |
|---|---|---|---|
| 1 100 | 540 / 540 / 1 098 / 1 098 px | nebeneinander | 0 / 16 |
| 760 | 370 / 370 / 758 / 758 px | untereinander, untereinander, nebeneinander, nebeneinander | 6 / 8 |
| 600 | 290 / 290 / 598 / 598 px | untereinander, untereinander, nebeneinander, nebeneinander | 10 / 8 |

## Spaltenwahlprobe (Spaltenwahl und Verwendungsmarke, KS1) — Seite `/fensterprobe`

**Zweck.** Die Katalogliste der Projektdialoge zeigt die Verwendung im Projekt als Marke am Bezeichner,
getönte Zeile und Legende „● im Projekt“ statt als Spalte, und ihre Spalten sind über „Spalten…“ in der
Kopfleiste des Katalogs wählbar (Konzept Projektdialoge mit Katalogauswahl 4.10). Die Probe misst das im
echten Heizkessel- und BHKW-Dialog der Fensterprobe; sie steht als Fälle `KS1_*` am Ende von
`katalogprobe.mjs`.

```bash
node katalogprobe.mjs --url http://127.0.0.1:5299 --nur KS1 [--fotos <ordner außerhalb des Repositorys>]
```

Die Seite nimmt dafür zwei Gaben: `?verwendet=1` — die erste Projektzeile heißt wie die zweite
Katalogzeile, die Liste trägt also Marke und Legende — und `?summe=<text>` für die Summe der
Projekt-Kopfleiste (die Probe setzt „1.240,5“). Gemerkt wird die Wahl in den Einstellungen des Wirts
(`Dienste.Einstellungen`, flüchtig je Prozess): Ein Neuladen der Seite findet sie wieder.

**Fälle** Heizkessel und BHKW in 1 280 × 800 und 768 × 1 024, je:

1. Standardanzeige: keine Spalte „im Projekt verwendet“; die Marke ist sichtbar und hat die Projektfarbe
   (`rgb(29, 158, 117)`), die Zeile trägt `epos-zeile--verwendet`, die Legende ist sichtbar, der Knopf
   „Spalten…“ liegt ganz in der Kopfleiste; die Summe ist nicht gekürzt (`scrollWidth ≤ clientWidth`) und
   die Projekt-Kopfleiste einzeilig.
2. Auswahl offen: ganz im Bild, rollt nicht in sich (kein `overflow: auto|scroll`, keine Überhöhe), das
   Dokument rollt nicht.
3. „Hersteller“ ab, „im Projekt verwendet“ an: die Köpfe folgen.
4. Esc im Kästchen schließt die Auswahl, nicht den Dialog.
5. Neuladen: die Wahl gilt weiter; der Katalogbereich rollt nicht quer (die Liste rollt in ihrem Raster).
6. „Standard“ schließt die Auswahl und setzt zurück, auch nach dem Neuladen.

**Gegenprobe** `KS1_gegenprobe_heizkessel_768x1024` (läuft mit): Die Auswahl bekommt eine Rollhöhe von
60 px, die Marke wird versteckt — der Fall MUSS Verstöße liefern.

**Ergebnis vom 10.10.2026** (Wirt Release, Chromium headless): **4 von 4 Fällen erfüllt, Gegenprobe rot**
(„Verwendungsmarke nicht sichtbar; Auswahl rollt in sich“); der ganze Lauf `katalogprobe.mjs` mit
64 Fällen ohne Überlagerung (Rückgabe 0). Ebenso grün: `rollbereichprobe.mjs`, `fensterprobe.mjs`,
`bannerprobe.mjs` (der BHKW-Fall wählt seine Katalogzeile über `.epos-zeilenzelle--name` — der
Katalog des BHKW-Dialogs ist seit seiner Stufe 3 eine Zeilenwahl ohne Wahlknopf), `legendenprobe.mjs`,
`kennzahlenprobe.mjs`.

**Trefferzahl und Hinweis der Spaltenwahl (DZ1, Konzept 4.10).** Dieselben KS1-Fälle messen zusätzlich: Die
Trefferzahl der Katalog-Kopfleiste kürzt nicht mit Auslassung und ihr Zahlteil steht ganz in ihrem Kasten (das
Hauptwort darf fehlen, die Probe meldet, ob es steht); in der offenen Auswahl steht neben jeder angehakten
Spalte, deren Kopf bei der Breite ausgeblendet ist, der Hinweis „bei dieser Breite ausgeblendet“, und neben
keiner sichtbaren. Die Gegenprobe kneift die Trefferzahl zusätzlich auf 30 px und versteckt den Hinweis.
**Ergebnis vom 10.10.2026:** 4 von 4 Fällen erfüllt, Gegenprobe rot („Trefferzahl abgeschnitten; Hinweis fehlt
bei ausgeblendeter Spalte: Brennstoff, η, Brennwert“). Trefferzahl in allen vier Fällen „40 von 40 Sätzen“ mit
Hauptwort; Hinweis bei 768 × 1 024 am Heizkessel neben Brennstoff, η und Brennwert, am BHKW neben Brennstoff,
P_th, σ, η und Motortyp, bei 1 280 × 800 am BHKW neben Motortyp, am Heizkessel neben keiner Spalte.
