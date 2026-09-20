# ChartProben — der plattformfreie Nachweis des Diagramm-Renderers

**Zweck.** `EPOS.Kern/Allgemein/Bericht/ChartRenderer` zeichnet jedes Diagrammbild des
Berichts und der Oberfläche. Die Probe hält ihn auf einem nackten Linux-Abbild fest:
Sie zeichnet **jedes** Bild aus synthetischen Reihen (Sinus und Rampe, fest verdrahtet —
keine Datenbank, kein Zufall, keine Datei) und prüft je Bild PNG-Signatur, Bildmaße,
Farbvielfalt, jede erwartete Palettenfarbe und den **Determinismus** (zweimal zeichnen
liefert byte-gleiche Dateien). Rot wird sie, sobald der Renderer eine Windows-API braucht
oder sich ein Bild ändert.

Neben den Maßproben stehen **Gegenproben**: zwei Zeichenwege, die sich unterscheiden
*müssen* (`… (wirkt)`) — ein Fensterparameter, eine Strichart oder ein Legendenname, den der
Renderer stillschweigend überginge, bestünde jede Maß- und Farbprüfung und täte trotzdem
nichts. Dazu kommt die Versatzprobe der Legendenzeilen (`… (Versatz … px)`).

Die Probe läuft in `kern.yml` bei jedem Push.

---

## Aufruf

```bash
dotnet run --project Proben/ChartProben -c Release
```

| Schalter | Wirkung |
|---|---|
| `--ziel <ordner>` | Zielordner der Maßproben-Bilder. Vorgabe: `artifacts/chartproben` (steht in `.gitignore`) |
| `--ablage <ordner>` | legt **jedes** gezeichnete Bild als PNG in diesem Ordner ab — auch die beiden Bilder je Gegenprobe (`…_a.png` / `…_b.png`) und die beiden der Versatzprobe (`…_wenige.png` / `…_viele.png`). Dateiname = Probenname |
| `--hashes <datei>` | schreibt die **Hash-Messlatte**: je Bild eine Zeile aus SHA-256, zwei Leerzeichen und `<name>.png`, nach Name geordnet, mit LF und ohne BOM — das Format von `sha256sum`, also mit `sha256sum -c` im Ablageordner nachrechenbar |
| `--svg <datei>` | schreibt den Jahresgang der Klimadaten **einmal** als SVG-Text (UTF-8 ohne BOM, LF) und nennt Größe und Knotenzahl — zum Ansehen im Browser und als Nachweis der SVG-Gegenprobe |
| `--svg-alle <ordner>` | schreibt **jedes** Modell mit SVG-Gegenprobe als eigene `.svg` in diesen Ordner (Dateiname = Bildname ohne `svg_`) — für die Sichtprüfung gegen das Skia-Bild aus `--ablage`. Kein Teil der Prüfung: Es entsteht kein PNG und keine Hashzeile |

Ohne `--ablage` und ohne `--hashes` verhält sich die Probe unverändert.

```bash
# Messlatte erzeugen und gegen den Bestand halten
dotnet run --project Proben/ChartProben -c Release -- \
    --ablage /tmp/chartbilder --hashes /tmp/neu.sha256
diff Proben/ChartProben/Messlatte_2026-09-20.sha256 /tmp/neu.sha256
```

---

## Die Hash-Messlatte

`Messlatte_2026-09-20.sha256` ist der eingefrorene Stand **aller** Probebilder. Sie ist die
Abnahme des Umbaus auf das **Zeichenmodell**
([Konzept DG-1](../../Dokumentation/aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md),
Etappe E1): Dort wird der Renderer hinter einem Modell aus Primitiven zerlegt, **ohne dass
sich ein Bild ändern darf**. Der Nachweis ist kein Urteil, sondern ein leerer Text-Diff
gegen diese Datei.

- **Warum Hashes und keine Bilder.** Bilder gehören nicht ins Repository
  (`EPOS.Kern.Tests/RepositoryOrdnungWacheTests`); die Hashliste ist eine kleine Textdatei
  und sagt dasselbe.
- **Warum alle Bilder und nicht nur die 51 Maßproben.** Was die Messlatte nicht nennt, kann
  sich beim Umbau unbemerkt ändern. Deshalb stehen auch die Bilder der Gegen- und
  Versatzproben darin, die im Bestand nur miteinander verglichen und nie geschrieben werden.
- **Umfang.** 72 Proben (51 Maßproben, 20 Gegenproben, 1 Versatzprobe) ergeben **91 Bilder**
  und ebenso viele Zeilen. Die sechzehn SVG-Gegenproben zeichnen kein PNG und stehen deshalb
  nicht darin — die Probenzahl steigt, die Messlatte bleibt bei 91.
- **Die Messlatte gilt für die Vorgabe-Palette.** Die Farben der Diagramme sind eine
  Anwendungseinstellung (Rubrik „Diagramme"); die Probe setzt deshalb zu Beginn ausdrücklich
  `Farbpalette.Vorgabe`, damit die Hashliste unabhängig von einer Anwendereinstellung bleibt.
  Die Gegenprobe `palette_abweichend_wirkt` zeichnet dasselbe Bild ein zweites Mal mit einer
  getauschten Rolle; ihre zwei Bilder stehen **nicht** in der Ablage und nicht in der
  Messlatte — sonst hinge die eingefrorene Liste an einer Einstellung.
- **Wann sie neu eingefroren wird.** Nur, wenn ein Bild sich **bewusst** ändern soll — die
  Etappe E4 des Konzepts nennt den Fall (Linien gebündelt statt jeder n-te). Dann entsteht
  eine neue Datei mit dem Datum des Tages, und die alte wird im selben Schritt entfernt.
- Die Datei steht als `text eol=lf` in `.gitattributes`: Ein Auschecken mit `autocrlf`
  machte sonst CRLF daraus, und der Vergleich schlüge in jeder Zeile fehl.
- **Die Messlatte ist plattformgebunden.** Sie ist auf dem Linux-Läufer der CI eingefroren.
  Der Maler holt seine Schrift über `SKFontManager.Default`, also aus den Systemschriften der
  Plattform; auf Windows weichen deshalb **alle 91 Hashes** ab, obwohl die Probe dort dieselben
  72 Bilder mit 0 Verstößen meldet (gemessen 20.09.2026). Der Text-Diff gegen die Messlatte gilt
  auf dem Linux-Läufer; auf Windows zählt das strukturelle Ergebnis der Probe.
- **Bildgleichheit auf Windows nachweisen.** Wer dort prüfen will, ob ein Umbau ein Bild verändert
  hat, baut den Vergleichsstand in einem Worktree (`git worktree add --detach <ordner> <basis>`),
  lässt die Probe dort und am HEAD mit `--ablage` und `--hashes` laufen und vergleicht die beiden
  Windows-Hashlisten miteinander — gleicher Rechner, gleiche Schriften, die Listen müssen gleich
  sein. So ist der Umbau auf das Zeichenmodell auch auf Windows abgenommen (Basis E0 gegen den
  Abschluss von E1 mit DF‑1: 91 von 91 gleich; E2 gegen denselben Stand: wieder 91 von 91).

---

## Die SVG-Gegenprobe

Seit der Etappe E2 des
[Diagrammkonzepts](../../Dokumentation/aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md) gibt
es zu jedem Modell einen **zweiten Ausgabeweg**: `SvgSchreiber` schreibt dasselbe
`Zeichenmodell`, das der `SkiaMaler` ins PNG malt, als SVG-Text für den Bildschirm. Sechzehn
Proben halten ihn fest — sie zeichnen **kein PNG**, legen nichts ab und stehen **nicht** in
der Messlatte:

| Probe | Aussage |
|---|---|
| `svg_jahresgang_byte_gleich` | zweimal geschrieben *und* zweimal erzeugt ergibt denselben Text, Byte für Byte — kein Zufall, keine Zeitangabe, keine Kulturabhängigkeit |
| `svg_jahresgang_struktur` | genau ein `<path class="epos-reihe">` je Reihe, jeder mit `vector-effect` und `data-marke`; das innere `<svg>` mit `preserveAspectRatio="none"`; mindestens so viele `<text>` wie das Modell Beschriftungen führt |
| `svg_palette_wirkt` | die Farbrollen werden **beim Schreiben** aufgelöst: mit getauschter Palette ändert sich die Strichfarbe der Reihe, mit der Vorgabe nicht |
| `svg_jahresgang_fenster` | mit `Achsenfenster` steht die `viewBox` des inneren svg auf den **Fensterstunden** — die Schnittstelle, über die die Oberfläche zoomt |

Die Etappe E3 legt acht weitere dazu — je ein Modell der Gruppe (a), der `ErzeugerStapel`
zweimal (mit einer und mit zwei Reihen auf der zweiten Achse). Alle acht prüfen dasselbe:
Determinismus, ein `path.epos-reihe` je `Datenreihe`, Flächen mit `fill` und geschlossenem
Zug, Linien mit `fill="none"`, die **viewBox-Höhe in Bildpunkten** (Entscheid DG-E3-1) und
die zweite Achse (`data-marke="yachse2"`) genau dann, wenn das Modell sie führt:

| Probe | Bild |
|---|---|
| `svg_kostenprofil` | `KostenprofilModell` — x ist der Index der Reihe |
| `svg_stundenprofil_woche` | `StundenprofilModell` — die eine Fläche mit ihrer Randlinie |
| `svg_jahresverlauf_bedarf` | `JahresverlaufModell` |
| `svg_ganglinie_normiert` | `GanglinieNormiertModell` — vier Reihen in Prozent |
| `svg_erzeugerstapel_waerme` | `ErzeugerStapelModell` — fünf Stapelflächen, Kontur, zweite Achse |
| `svg_erzeugerstapel_zwei_speicher` | dasselbe mit **zwei** Reihen auf der zweiten Achse |
| `svg_temperaturverlauf` | `TemperaturverlaufModell` — Achse ohne Nullpunkt |
| `svg_speicherbetrieb` | `SpeicherbetriebModell` — vier Leistungen links, der Ladezustand rechts |

Warum sie nötig sind: Ein Schreiber, der die Reihen wegließe, die Palette nicht läse oder das
Datenfenster überginge, bestünde jede Maß-, Farb- und Determinismusprüfung des PNG-Wegs — das
PNG entsteht ja weiterhin aus dem Maler.

### Die vier reinen Pixelbilder der Gruppe (d)

Die vier **Berichtsbilder** sind der Gegenfall: Nach Entscheid DG-E3-7 tragen sie **keine**
Zeichenfläche und **keine** `Datenreihe` — im Bericht gibt es keine Bedienung, der Gewinn des
SVG ist Schärfe und durchsuchbarer Text. Ihre Gegenprobe (`SvgPixelprobe` in
`Program.GruppeD.cs`) prüft deshalb das Gegenteil: kein inneres `<svg class="epos-flaeche">`,
kein `path.epos-reihe`, dafür **jeder** Textbefehl des Modells als `<text>`, die Marken
`titel`, `xachse`, `yachse`, `reihe:…`, `legende:…` im Baum — und zu jeder gezeichneten Reihe
genau ein Legendeneintrag.

| Probe | Bild |
|---|---|
| `svgd_jahresverlauf_waerme` | `JahresverlaufWaermeModell` — Tagesmittel, Stapelflächen, Bedarfslinie |
| `svgd_dauerlinie_waerme` | `DauerlinieWaermeModell` — x zählt den Rang |
| `svgd_speicherverlauf` | `SpeicherverlaufModell` — drei Wochenfelder in einem Bild |
| `svgd_speichertemperaturen` | `SpeichertemperaturenModell` — drei Felder, Achse ohne Nullpunkt |

**Sichtprüfung.** `--svg-alle <ordner>` schreibt alle dreizehn Modelle (die acht der
Gruppe (a), den Jahresgang und die vier der Gruppe (d)) als Dateien; ein kopfloser Browser
macht daraus ein Bild, das sich neben das Skia-PNG aus `--ablage` legen lässt. Erwartet wird
**dieselbe Struktur**, nicht dasselbe Pixel: Das SVG der Gruppe (a) zeichnet die Reihe roh
bzw. als konservative Hülle, das PNG jeden n-ten Wert — im SVG steht deshalb der volle
Tagesgang, wo das PNG ein ausgedünntes Band zeigt (Entscheid DG-E2-2). Bei den vier
Pixelbildern der Gruppe (d) entfällt dieser Unterschied: Sie zeichnen dieselben Befehle,
nur als Vektor.
