# KL-5 — Katalograhmen mit Katalogliste: Eingabeblock und Knöpfe überlagerten die Liste

Protokoll zum Auftrag **KL-5** (19.09.2026). Der gültige Stand steht in
[`Proben/Rasterprobe/LIESMICH.md`](../../../../Proben/Rasterprobe/LIESMICH.md) und in den
Kommentaren an `.epos-katalog-paar` in `EPOS.UI/wwwroot/epos-ui.css`; hier steht, was war
und wie es gemessen wurde.

## 1 Der Anwenderbefund

Zwei Bildschirmbilder der Windows-Abnahme:

* **Klimadaten** (Fenster 1 180 × 780, nach einem erfolgreichen TRY-Regionalimport, 35
  Regionen in der Liste): „Diagramm, Button und Liste sind überlagert. Button ‚Daten
  einlesen‘ steht vor Elementen.“ Die Regionsliste wurde ab etwa der vierten Zeile von der
  Reiterleiste „Temperatur | Sonnenwinkel“ und vom Diagrammkasten „Jahrestemperatur
  Verlauf“ übermalt; der Kasten reichte über die Fußleiste, von „Löschen“ war nur
  „…öschen“ zu lesen, „Beenden“ halb verdeckt. Vor dem Import — Platzhalter statt Bildern
  — war das Bild in Ordnung.
* **Stromverbraucher Verwaltung** (Hülle 780 × 640, vom Anwender auf rund 1 600 × 1 000
  gezogen): Die Eingabefelder und die Knopfleiste zeichneten mitten über die Listenzeilen,
  Abbrechen/OK über die letzten Zeilen und den waagerechten Rollbalken.

Zwei Masken, ein Muster: `Katalograhmen` + `Katalogliste`. Es tragen es **vierzehn
Menüpunkte in sieben Komponenten** — `KlimadatenDialog`, `BedarfAdminDialog`
(Brauchwasser, Prozesswärme, Stromverbraucher), `WaermebedarfAdminDialog`,
`SolarganglinieAdminDialog`, `KatalogBrowserDialog` (Heizkessel, BHKW, Solarkollektoren,
Pufferspeicher), `WaermepumpeStammDialog`, `ModulKatalogDialog` (PV-Module,
Wechselrichter, Stromspeicher).

## 2 Wie gemessen wurde

bunit hat kein Layout: Eine Überlagerung ist eine Aussage über **Rechtecke** und war mit
dem vorhandenen Prüfnetz grundsätzlich nicht zu sehen — deshalb stand der Befund trotz
grüner Tests im Bild des Anwenders. Der Prüfstand `Proben/Rasterprobe` bekam deshalb eine
zweite Probe:

* **Probeseite** `Proben/Rasterprobe/Wirt/Seiten/Katalogprobe.razor` (`/katalogprobe`):
  stellt **den ganzen Dialog** mit synthetischen Daten, wahlweise eine der sieben Masken,
  mit 35 Zeilen. Das Diagramm kommt aus demselben `ChartRenderer.Jahresgang` (1 304 × 440 px),
  den die Windows-Hülle ruft — die Maße entscheiden hier, ein kleineres Ersatzbild hätte
  den Befund verfehlt.
* **Messprogramm** `Proben/Rasterprobe/katalogprobe.mjs`: misst im Chromium die Kästen von
  Listenblock, Listenhülle, Eingabeblock, Reiterleiste, Diagramm und Fußleiste samt jedem
  Knopf darin und meldet jede Überschneidung als Verstoß. Gemessen wird der **sichtbare**
  Kasten (jedes Rechteck mit denen aller rollenden Vorfahren geschnitten) — mit dem bloßen
  `getBoundingClientRect` gälte sonst jeder gerollte Dialog als Verstoß. Dazu
  `elementFromPoint` auf vier Ecken und der Mitte jedes Fußleistenknopfes: Wer liegt dort
  zuoberst?
* Zwölf Fälle: Klimadaten, Stromverbraucher und PV-Module je bei **1 180 × 780** und
  **1 600 × 1 000**, Klimadaten zusätzlich ohne Bilder, die übrigen vier Masken je einmal,
  und eine **Gegenprobe** mit dem Maß von vor KL-5 (`--vorher`).

## 3 Die Ursache, in Zahlen

Fall K1 (Klimadaten, 1 180 × 780, 35 Regionen, Bilder befüllt), Stand **vor** der Behebung:

| Größe | gemessen |
|---|---|
| Dialoghöhe (`height: 100dvh`) / Inhalt | 780 px / **1 475 px** |
| Rahmen `.epos-katalog-paar` | auf **601,8 px** gestaucht, Inhalt 1 377 px |
| `grid-template-rows` (gerechnet) | **295,906 px \| 295,906 px** |
| Listenblock | Reihe 295,9 px, Hülle 457,6 px → **296 284 px²** über den Feldern darunter |
| Eingabeblock | Reihe 295,9 px, Inhalt **1 071 px** → malt bis y = 876,8 |
| Fußleiste | y = 720 … 764 → **49 829 px²** vom Diagramm überdeckt |
| „Daten einlesen“, „Löschen“, „Beenden“ | `elementFromPoint` liefert `img.epos-chartbild` |

Die Kette:

1. `.epos-katalog-fuellend` gab dem Rahmen `flex: 1 1 auto` **und** `min-height: 0`. Er
   durfte also unter seinen Inhalt schrumpfen — und tat es, sobald der Dialog
   (`height: 100dvh`) kürzer war als sein Inhalt.
2. Der Rahmen ist ein **Raster** mit zwei `auto`-Reihen. Weil seine zwei Kinder
   (`.epos-katalog-liste`, `.epos-katalog-eingabe`) ihrerseits `min-height: 0` trugen, war
   die **Mindestgröße einer Reihe null**: Chromium verteilte die gestauchte Höhe zu
   **gleichen Teilen** auf beide Reihen, statt jeder ihren Inhalt zu lassen.
3. Die zweite Reihe war damit 775 px zu kurz. Ihr Inhalt zeichnete darüber hinaus — quer
   über die Liste und über die Fußleiste. „Daten einlesen“ hat **keine** eigene Lage: Der
   Eindruck, er stünde „vor Elementen“, kam allein aus dieser Stauchung.

In den Masken ohne Diagramm wirkt derselbe Mechanismus über die **Listenhülle**: Sie bleibt
in ihrer Höchsthöhe von 457,6 px stehen und ragt aus dem gestauchten Listenblock heraus —
gemessen mit `--vorher`: Stromverbraucher 186 698 px², PV-Module 280 105 px², BHKW-Katalog
280 105 px², Solarganglinie 174 076 px², Wärmebedarf 145 847 px² über die Felder darunter.
Das ist das zweite Anwenderbild.

## 4 Die Behebung

Zwei Zeilen in `EPOS.UI/wwwroot/epos-ui.css`, beide am gemeinsamen Ort und keine je Dialog:

```css
.epos-katalog-paar.epos-katalog-fuellend { overflow: auto; }
.epos-katalog-paar > .epos-katalog-liste,
.epos-katalog-paar > .epos-katalog-eingabe { min-height: auto; }
```

* `min-height: auto` gibt den zwei Kindern ihre selbsttätige Mindestgröße zurück; damit ist
  die Mindestgröße einer `auto`-Reihe wieder die Höhe ihres Inhalts, und es gibt nichts mehr
  gleichmäßig zu verteilen.
* `overflow: auto` lässt den Rahmen **in sich** rollen, statt die Maske länger zu machen.
  Ohne das wäre die Fußleiste zwar nicht mehr überdeckt, stünde aber rund 850 px unter dem
  Fensterrand — „Beenden“ wäre so unerreichbar wie vorher, nur anders. Kopf,
  Fortschrittsbalken und Fußleiste bleiben nun stehen, wo sie sind.

Keine Positionierung, keine feste Pixelhöhe. Die zwei Masken, die `epos-katalog-fuellend`
auf eine bloße Liste setzen (`GesetzeskatalogDialog`, `WaermepumpenKatalogDialog`), haben
keine zweite Reihe und bleiben unberührt; die gemeinsame Klasse `.epos-katalog-fuellend`
selbst wurde nicht angefasst.

## 5 Messung nachher

| Größe | K1 (1 180 × 780) | K2 (1 600 × 1 000) |
|---|---|---|
| `grid-template-rows` | **564,594 px \| 1 070,8 px** | 564,594 px \| 1 101,84 px |
| Listenhülle | **457,6 px** = Höchstmaß, sieben Zeilen | 457,6 px |
| Rahmen sichtbar / Inhalt | 601,8 px / 1 645 px (rollt in sich) | 821,8 px / 1 676 px |
| Fußleiste | y = 720 … 764, alle drei Knöpfe **frei** | y = 940 … 984, **frei** |
| Rollhöhe des Dialogs | **780 px** = Fensterhöhe | 1 000 px |

`katalogprobe.mjs`: **12 von 12 Fällen ohne Überlagerung**, die Gegenprobe `G1` zeigt den
Befund wie erwartet. `rasterprobe.mjs`: **9 von 9** wie zuvor.

## 6 Prüfnetz

* `Proben/Rasterprobe/katalogprobe.mjs` — die Maßprüfung (zwölf Fälle, Gegenprobe
  `--vorher`), beschrieben in `Proben/Rasterprobe/LIESMICH.md`.
* `EPOS.UI.Tests/Bausteine/KatalograhmenTests` — zwei neue Fälle: die zwei Regeln stehen im
  Stilblatt, und die gemeinsame Füllklasse bleibt unberührt.
* `EPOS.UI.Tests/Dialoge/KlimadatenDialogTests` — zwei neue Fälle: Liste in ihrer Hülle im
  Listenblock, Reiter im Eingabeblock, Fußleiste außerhalb des Rahmens; „Daten einlesen“
  ohne eigene Lage (`position`/`z-index`).

## 7 Offen

* **Nur unter Windows prüfbar:** Die Probe läuft unter Chromium, der Anwender unter
  WebView2 mit 125 % Windows-DPI (`zoomFactor`, nicht `deviceScaleFactor`). Die Abnahme am
  Gerät steht aus — Abnahmepunkt **A-KL5-1**.
* Der Rahmen rollt jetzt in sich. Bei sehr kleinen Fenstern stehen damit zwei Rollbereiche
  ineinander (Rahmen und Listenhülle); gemessen stört das nicht, ist aber eine Stelle, die
  eine Anwenderrückmeldung verdient.
