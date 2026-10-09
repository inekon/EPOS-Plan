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

Die Probe läuft in `kern.yml` bei jedem Push, auf ubuntu samt Vergleich mit der Hash-Messlatte.

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
| `--svg-alle <ordner>` | schreibt **jedes** Modell mit SVG-Gegenprobe als eigene `.svg` in diesen Ordner (Dateiname = Bildname ohne `svg_` bzw. `svg_c_`) — für die Sichtprüfung gegen das Skia-Bild aus `--ablage`; daneben `<name>_druck.svg`, der SVG-Teil des Wortberichts (`SvgSchreiber.Drucktext`, Reihen als Pixelpfade ohne `vector-effect`). Kein Teil der Prüfung: Es entsteht kein PNG und keine Hashzeile |

Ohne `--ablage` und ohne `--hashes` verhält sich die Probe unverändert.

```bash
# Messlatte erzeugen und gegen den Bestand halten
dotnet run --project Proben/ChartProben -c Release -- \
    --ablage /tmp/chartbilder --hashes /tmp/neu.sha256
diff Proben/ChartProben/Messlatte_2026-10-09.sha256 /tmp/neu.sha256
```

---

## Die Hash-Messlatte

`Messlatte_2026-10-09.sha256` ist der eingefrorene Stand **aller** Probebilder. Sie ist die
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
- **Umfang.** Ein Lauf prüft **254 Bilder**; **222** davon zeichnen ein PNG — Maßproben, die
  beiden Bilder jeder Gegenprobe und die der Versatzprobe — und stehen als Zeilen in der Messlatte.
  Die SVG-Gegenproben und die Schriftproben der Bildgröße Stufe 2 (`stufe2_…_schrift`) zeichnen
  kein PNG und stehen deshalb nicht darin. `Messlatte_2026-10-09.sha256` nennt alle 222 Bilder
  aller Abschnitte unten.
- **Die Messlatte gilt für die Vorgabe-Palette.** Die Farben der Diagramme sind eine
  Anwendungseinstellung (Rubrik „Diagramme"); die Probe setzt deshalb zu Beginn ausdrücklich
  `Farbpalette.Vorgabe`, damit die Hashliste unabhängig von einer Anwendereinstellung bleibt.
  Die Gegenprobe `palette_abweichend_wirkt` zeichnet dasselbe Bild ein zweites Mal mit einer
  getauschten Rolle; ihre zwei Bilder stehen **nicht** in der Ablage und nicht in der
  Messlatte — sonst hinge die eingefrorene Liste an einer Einstellung.
- **Wann sie neu eingefroren wird.** Nur, wenn ein Bild sich **bewusst** ändern soll — die
  Etappe E4 des Konzepts nennt den Fall (Linien gebündelt statt jeder n-te), die Stufenregel
  des Stapels (Abschnitt „Gestapelte Jahresganglinien" unten) ist einer. Dann entsteht
  eine neue Datei mit dem Datum des Tages, und die alte wird im selben Schritt entfernt; die
  Commit-Nachricht nennt die geänderten Bilder und den Grund.
- **Wann sie nachgezogen wird.** Kommen Proben hinzu, ohne dass sich ein Bild ändert, zieht der
  nächste Lauf auf dem Linux-Läufer die Liste nach: `--ablage` und `--hashes` ergeben
  `Messlatte_<Datum>.sha256`; jede Zeile der bisherigen Datei muss darin unverändert stehen —
  sonst hat sich ein Bild geändert —, die neuen Zeilen sind die neuen Proben. Die neue Datei
  ersetzt die alte im selben Schritt, die Begründung (wie viele neu, keines geändert) steht in der
  Commit-Nachricht. Wer Proben hinzufügt, nennt sie im Abschnitt seiner Etappe unten.
- Die Datei steht als `text eol=lf` in `.gitattributes`: Ein Auschecken mit `autocrlf`
  machte sonst CRLF daraus, und der Vergleich schlüge in jeder Zeile fehl.
- **Die Messlatte ist plattformgebunden.** Sie ist auf dem Linux-Läufer der CI eingefroren.
  Der Maler holt seine Schrift über `SKFontManager.Default`, also aus den Systemschriften der
  Plattform; auf Windows weichen deshalb **alle Hashes** ab, obwohl die Probe dort dieselben
  Bilder mit 0 Verstößen meldet. Der Text-Diff gegen die Messlatte gilt
  auf dem Linux-Läufer; auf Windows zählt das strukturelle Ergebnis der Probe.
- **Die CI hält sie.** Auf ubuntu läuft die Probe in `kern.yml` mit `--hashes`; der Schritt
  „ChartProben-Messlatte (nur ubuntu)“ prüft, dass **jede** Zeile der jüngsten
  `Messlatte_*.sha256` unverändert in der Hashliste des Laufs steht, und nennt die Zahl der
  unveränderten Zeilen. Eine geänderte oder fehlende Zeile macht ihn rot und nennt die Bilder;
  zusätzliche Zeilen sind neue Proben, er zählt sie und bleibt grün. Die Hashliste liegt auch
  bei Rot als Artefakt `chartproben-hashliste-ubuntu-latest` bei — von dort übernimmt sie ein
  bewusstes Neueinfrieren oder Nachziehen. Ein Schriftwechsel im Läuferabbild macht den Schritt
  ebenfalls rot; er nennt das Abbild im Protokoll. Auf macOS wird nicht verglichen.
- **Bildgleichheit auf Windows nachweisen.** Wer dort prüfen will, ob ein Umbau ein Bild verändert
  hat, baut den Vergleichsstand in einem Worktree (`git worktree add --detach <ordner> <basis>`),
  lässt die Probe dort und am HEAD mit `--ablage` und `--hashes` laufen und vergleicht die beiden
  Windows-Hashlisten miteinander — gleicher Rechner, gleiche Schriften, die Listen müssen gleich
  sein. So ist der Umbau auf das Zeichenmodell auch auf Windows abgenommen (Basis E0 gegen den
  Abschluss von E1 mit DF‑1: 91 von 91 gleich; E2 gegen denselben Stand: wieder 91 von 91).
  Die Etappe E6 der Wirtschaftlichkeit ist auf dieselbe Weise abgenommen: die 91 Bilder vor ihr
  gleich, 17 neu, keines geändert.

---

## Die SVG-Gegenprobe

Seit der Etappe E2 des
[Diagrammkonzepts](../../Dokumentation/aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md) gibt
es zu jedem Modell einen **zweiten Ausgabeweg**: `SvgSchreiber` schreibt dasselbe
`Zeichenmodell`, das der `SkiaMaler` ins PNG malt, als SVG-Text für den Bildschirm.
Achtunddreißig Proben halten ihn fest (die vier der Etappe E6 im Abschnitt „Etappe E6" unten) —
sie zeichnen **kein PNG**, legen nichts ab und stehen **nicht** in der Messlatte:

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

Die **Gruppe (b)** legt acht weitere dazu (`Program.GruppeB.cs`, eine partielle Hälfte
derselben Klasse). Ihre sechs Bilder zählen auf der x-Achse keine Zeit, und drei von ihnen
haben deshalb **gar keine Zeichenfläche** (Entscheid DG-E3-7) — für sie gilt die umgekehrte
Prüfung: kein inneres `<svg>`, die Reihenbefehle als gewöhnliche Pixel-Elemente mit ihrer
Marke, und jede `Datenreihe` mit ihrer x-Stelle je Wert:

| Probe | Bild |
|---|---|
| `svg_kapitalwert_absolut` | `KapitalwertVerlaufModell` — x ist das Projektjahr; eine kürzere Reihe endet früher |
| `svg_streuwolke_drei_reihen` | `StreuwolkeModell` — drei Punktwolken, x ist die Außentemperatur |
| `svg_schnittkurve` | `SchnittkurveModell` — eine Linie mit **ungleichmäßigen** Stützstellen |
| `svg_kennlinien_cop` | `KennlinienModell` — reines Pixelbild samt Punktmarken |
| `svg_jahresprojektion` | `JahresprojektionModell` — Säulen, Linie und Ersatzjahr-Marke |
| `svg_flotte_stueckzahlkurve` | `StueckzahlkurveModell` — Säulen mit Schraffur, ohne Datenreihe |
| `svg_streuwolke_punkte` | die Punktwolke selbst: ein `M x,y h 0` je Wert, runde Strichkappe, Strichbreite = Punktdurchmesser, **keine** Bündelung (DG-E3-5) |
| `svg_achsenteilung` | `ChartRenderer.Achsenteilung` je Achsenart: Stunden, ganzzahliger Index, runde Wertstufen (DG-E3-4) |

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

### Die Gruppe (c): sieben Bilder ohne Zeitachse

Die Modelle der Gruppe (c) stehen in einer eigenen Datei (`Program.GruppeC.cs`); sie bringen
ihre Gaben selbst mit, und `Program.cs` ruft sie mit einer Zeile. Hier ist nicht das innere
`<svg>` der Prüfgegenstand, sondern sein **Fehlen**: Diese Bilder sind reine Pixelbilder
(Entscheid DG-E3-7) — keine Zeichenfläche, keine `Datenreihe`, kein Zoom. Jede Probe prüft
Determinismus, das Fehlen von `epos-flaeche` und `epos-reihe`, die Marke `titel`, dass jedes
Datenelement (`data-marke="reihe:…"`) einen nicht leeren **`data-wert`** trägt (Entscheid
DG-E3-6) und dass jeder Legendeneintrag Elemente schaltet, die es gibt:

| Probe | Bild |
|---|---|
| `svg_c_kuchen` | `KuchenModell` — Segmente und Legende, beide unter demselben Schlüssel |
| `svg_c_balken_horizontal` | `BalkenHorizontalModell` — eine Zeile ist ein Element |
| `svg_c_strombilanz_monate` | `StrombilanzMonateModell` — Stapel, Nebenbalken und der Bedarfszug |
| `svg_c_monatssaeulen` | `MonatsSaeulenModell` — zwölf starre Fächer, eine Reihe |
| `svg_c_monatsstapel_drei_reihen` | `MonatsStapelModell` — drei Schichten je Monat |
| `svg_c_ring_waermedeckung` | `RingModell` — fünf Segmente mit ihrem Anteil |
| `svg_c_optimierungsraster` | `OptimierungsrasterModell` — 60 Zellen, Bestmarke, Farbskala |

Drei weitere prüfen den **Wortlaut** des Werts, den kein Aufbau-Test sieht:
`svg_c_rasterzelle_nennt_beide_achsen` (die Zelle nennt beide Achsen samt Einheit und die
Einheit der Farbskala, und die Bestmarke nennt dieselbe Zelle),
`svg_c_monatswert_nennt_monat_und_einheit` (Monatsname und Einheit, mit den Nachkommastellen
der eigenen y-Achse) und `svg_c_anteil_steht_in_prozent` (Kuchen und Ring zeigen Anteile).

**Sichtprüfung.** `--svg-alle <ordner>` schreibt alle neunundzwanzig Modelle (die acht der
Gruppe (a), die sechs der Gruppe (b), die sieben der Gruppe (c), die vier der Gruppe (d), den
Jahresgang und die drei der Etappe E6) als Dateien; ein kopfloser Browser macht daraus ein
Bild, das sich neben das Skia-PNG aus `--ablage` legen lässt. Erwartet wird **dieselbe Struktur**, nicht dasselbe
Pixel: Das SVG der Gruppen (a) und (b) zeichnet die Reihe roh bzw. als konservative Hülle,
das PNG jeden n-ten Wert — im SVG steht deshalb der volle Tagesgang, wo das PNG ein
ausgedünntes Band zeigt (Entscheid DG-E2-2). Die Pixelbilder der Gruppen (c) und (d) haben
keine Reihen und stehen deshalb Bildpunkt für Bildpunkt wie das PNG; nur die Textbreiten
kommen aus dem Browser statt aus der Schriftkette.

---

## Etappe E6 der Wirtschaftlichkeit: Verlauf mit drei Szenarien und Spannenbild

Zwei Bilder des Abschnitts „Wie sicher ist das?" bringen eigene Probedateien mit; `Program.cs`
ruft jede mit einer Zeile. Ihre Texte sind die deutsche Vorgabe der Textbündel, kein Bild hängt
an der Sprache des Rechners.

| Datei | Bild | Maßproben | Gegenproben | SVG-Proben |
|---|---|---|---|---|
| `Program.Szenarien.cs` | `KapitalwertSzenarien` — der kumulierte Barwert der Differenz zur Referenz je Jahr; Farbe = Stand, Strichart = Szenario (durchgezogen, gestrichelt, **gepunktet**), zweigeteilte Legende, Nulldurchgang je Linie | `kapitalwert_szenarien` (drei Stände, 1240 × 620), `…_eine_variante`, `…_acht_varianten` (eine Legendenzeile mehr: 1240 × 650), `…_neun_varianten` (die benannte Ablehnung) | `strichart_gepunktet_wirkt`, `kapitalwert_szenarien_legende_zweigeteilt_wirkt`, `kapitalwert_szenarien_nulldurchgang_wirkt` | `svg_kapitalwert_szenarien` (Modellprobe), `svg_kapitalwert_szenarien_stricharten` (Strichfolge je Szenario, Wert an jeder Marke) |
| `Program.Spanne.cs` | `KapitalwertSpanne` — die Bandbreite je Version als Balken vom kleinsten bis zum größten Szenariowert, der Erwartungsfall als Punkt, die Referenz als Nulllinie | `kapitalwert_spanne` (die drei Versionen des Mockups, 1240 × 434), `…_unter_referenz` (vier Versionen um die Nulllinie, 1240 × 506), `…_leer` (Leerhinweis, 1240 × 200) | `kapitalwert_spanne_erwartet_wirkt`, `kapitalwert_spanne_spanne_wirkt` | `svg_c_kapitalwert_spanne` (Pixelbildprobe wie Gruppe (c)), `svg_kapitalwert_spanne_nulllinie` (die Nulllinie liegt bei der Referenz: links der Balken darüber, rechts der darunter, mitten in dem, der sie kreuzt) |

Das sind sieben Maßproben und fünf Gegenproben — **17 neue Bilder** — und vier SVG-Proben.
Kein Bild von vorher hat sich geändert: auf Windows am selben Rechner gegen den Stand vor der
Etappe gemessen, 91 von 91 gleich.

Die 17 Bilder stehen in der Messlatte.

---

## Gebäudesimulation (Stufe G2): Raumtemperatur eines Gebäudes

`ChartRenderer.Raumtemperatur` zeichnet den Jahresverlauf der Raumluft- und der operativen
Temperatur mit dem Sollwertband (Heizsollwert nach Fahrplan und obere Raumtemperatur,
gestrichelt) — das zweite Bild des Bedarfsdialogs eines Gebäudes. Zwei Proben halten es:

| Probe | Aussage |
|---|---|
| `raumtemperatur_gebaeude` | Maßprobe 1240 × 560, die Farben der Rollen `SERIE_1` und `SERIE_2`, Determinismus |
| `raumtemperatur_sollband_wirkt` | Gegenprobe: dasselbe Bild mit und ohne Sollwertband muss sich unterscheiden |

Die drei Bilder (eines der Maßprobe, zwei der Gegenprobe) stehen in der Messlatte. Das Bild nutzt den
vorhandenen Verlaufsweg des Temperaturbilds; kein bestehendes Bild ändert sich.

---

## Anlagenkopplung (Stufe AK1): Vorlauf und Rücklauf eines Gebäudes

`ChartRenderer.VorlaufRuecklauf` zeichnet den Jahresverlauf des gefahrenen Vorlaufs und des
Rücklaufs eines gekoppelt gerechneten Gebäudes mit dem Auslegungspunkt (Auslegungsvorlauf und
-rücklauf, gestrichelt) — das Bild „Vorlauf und Rücklauf" des Bedarfsdialogs eines Gebäudes.
Stunden ohne Heizbetrieb sind **Lücken**: Die Reihe trägt dort NaN, und mit dem Schalter
`Reihe.Luecken` bricht die Linie ab, statt die Reihe unbrauchbar zu machen — im PNG ein Linienzug je
Stück, im SVG ein Teilpfad mit eigenem `M` je Stück. Ohne den Schalter bleibt alles, wie es war. Die
Proben stehen in `Program.Anlagenkopplung.cs`:

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `vorlauf_ruecklauf_gebaeude` | 1240 × 560, die Farben der Rollen `SERIE_1` und `SERIE_2`, Determinismus; die Probenreihen heizen bis Stunde 2 999 und ab Stunde 6 500 |
| Gegenprobe | `vorlauf_auslegung_wirkt` | dasselbe Bild mit und ohne Auslegungspunkt muss sich unterscheiden |
| Gegenprobe | `vorlauf_luecke_wirkt` | dieselben Reihen mit gefülltem Sommer zeichnen anders |
| SVG-Probe | `svg_vorlauf_luecken` | jede Reihe mit Lücke zerfällt in zwei Teilpfade (gebündelt wie roh), kein Pfad trägt „NaN", die Linien des Auslegungspunkts bleiben ein Zug, ein Ausschnitt ganz in der Lücke zeichnet nichts |
| SVG-Probe | `svg_ohne_luecke_unveraendert` | ein Zug ohne Lücke bleibt ein Teilpfad mit jeder Stunde |

Kein Bild von vorher hat sich geändert: auf Windows am selben Rechner gegen den Stand vor der Welle
gemessen, alle 151 Hashes des Vorstands gleich, 5 neu (156 Zeilen). Die fünf Bilder (eines der
Maßprobe, vier der Gegenproben) stehen in der Messlatte.

**Die Kälteseite** zeichnet dasselbe Bild mit den Reihen der Kühlübergabe (fester Kaltwasser-Vorlauf,
Rücklauf darüber, Lücken außerhalb der Kühlstunden), ohne neuen Parameter: die Maßprobe
`kuehlvorlauf_ruecklauf_gebaeude` (1240 × 560), die Gegenproben `kuehlvorlauf_auslegung_wirkt` und
`kuehlvorlauf_luecke_wirkt` und die SVG-Probe `svg_kuehlvorlauf_luecken` (ein Teilpfad je
Kühlperiode, kein „NaN"). Ihre fünf Bilder stehen in der Messlatte.

---

## Zapfprofilgenerator (Stufe Z1): die Vorschaubilder des Zapfprofils

`ZapfprofilBilder` (`EPOS.Kern/Allgemein/Bericht/`) zeichnet die drei Vorschaubilder des
Dialogs „Brauchwasser-Zapfprofil": den **Tagesgang** je Tagtyp (Werktag, Samstag, Sonn-/Feiertag)
mit der Zirkulation gestrichelt, das **Wochenprofil** über 168 Stunden und den **Jahresgang** als
Monatsstapel aus Zapfung und Zirkulation. Tagesgang und Wochenprofil gehen durch das neue
`ChartRenderer.StundenprofileModell` — das Stundenprofil mit mehreren Reihen: die erste als Fläche,
jede weitere als Linie in ihrer Strichart, die Legende oben, die y-Achse auf runder Stufe
(`Skala.Stufe`). Der Jahresgang nimmt den vorhandenen `MonatsStapelModell`. Die Proben stehen in
`Program.Zapfprofil.cs`; ihre Texte sind die deutsche Vorgabe (`ZapfprofilBildtexte`).

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `zapfprofil_tagesgang` | drei Tagtypen und Zirkulation, 1244 × 524, die Farben der Rollen `WARMWASSER`, `SERIE_6`, `SERIE_5` |
| Maßprobe | `zapfprofil_wochenprofil` | 168 Wochenstunden, Teilung alle 24 h, 1244 × 524 |
| Maßprobe | `zapfprofil_jahresgang` | zwölf gestapelte Säulen, 978 × 542, Zapfung (`WARMWASSER`) und Zirkulation (`SERIE_7`) |
| Maßprobe | `stundenprofile_leer` | keine gültige Reihe: der Leerhinweis, 1244 × 464 |
| Maßprobe | `stundenprofile_legende_umbruch` | drei lange Reihennamen, die Legende bricht einmal um: 1244 × 554 (die Namen sind so lang, dass die Zeilenzahl auch bei anderen Textbreiten bleibt) |
| Gegenprobe | `stundenprofile_zweite_reihe_wirkt` | mit und ohne Samstag |
| Gegenprobe | `stundenprofile_strichart_wirkt` | die Zirkulation gestrichelt und durchgezogen |
| Gegenprobe | `zapfprofil_jahresgang_zirkulation_wirkt` | der Jahresgang mit und ohne Zirkulationsschicht |
| SVG-Probe | `svg_zapfprofil_tagesgang`, `svg_zapfprofil_wochenprofil` | Modellprobe der Gruppe (a): je Reihe ein `path.epos-reihe`, die erste als geschlossene Fläche |
| SVG-Probe | `svg_c_zapfprofil_jahresgang` | Pixelbildprobe der Gruppe (c): jede Schicht mit ihrem Wert |

Das sind fünf Maßproben und drei Gegenproben — **11 neue Bilder** — und drei SVG-Proben. Kein Bild
von vorher hat sich geändert: auf Windows am selben Rechner gegen den Stand vor der Stufe gemessen,
alle 111 Hashes des Vorstands gleich, 11 neu (122 Zeilen). Die elf Bilder stehen in der Messlatte.

---

## Zapfprofilgenerator (Stufe Z2): die Bilder der Auslegung

`ZapfprofilBilder` zeichnet die drei Bilder der Überlagerung „Auslegung" über das neue
`ChartRenderer.SummenlinieModell` — ein Linienbild über einer x-Größe mit eigener Teilung, linker
vorzeichenfähiger Achse, optionaler zweiter Achse (gemeinsame Skala von null an) und Marken
(Strecke oder Punkt mit Beschriftung): die **Summenlinie** des Bedarfstags (Bedarf und Versorgung
kumuliert über 1 441 Minutenwerte, Speicherinhalt als Strecke, kleinster Abstand als Punkt), die
**Wertepaarkurve** (Speichervolumen über Leistung mit eigenen x-Stellen, gewählter Punkt) und die
**maßgebende Woche** der Stundenbilanz (Zapfung als Fläche, Zirkulation gestrichelt, Ladung; Defizit
und Füllstand rechts, maßgebender Zeitpunkt). Die Proben stehen in `Program.Auslegung.cs`; ihre
Texte sind die deutsche Vorgabe (`ZapfprofilAuslegungBildtexte`).

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `zapfprofil_summenlinie` | 1 441 Minutenwerte, 1244 × 524, Bedarf (`WARMWASSER`) und Versorgung (`SPEICHERLADUNG`) |
| Maßprobe | `zapfprofil_wertepaarkurve` | fünf Wertepaare mit gewähltem Punkt, 1244 × 524, Rolle `STAMM` |
| Maßprobe | `zapfprofil_auslegungswoche` | 168 Wochenstunden mit zweiter Achse, 1244 × 524, `WARMWASSER`, `SPEICHERLADUNG`, `SERIE_4`, `SPEICHERFUELLSTAND` |
| Maßprobe | `summenlinie_leer` | keine gültige Reihe: der Leerhinweis, 1244 × 464 |
| Gegenprobe | `summenlinie_marken_wirken` | dieselbe Summenlinie mit und ohne Marken |
| Gegenprobe | `summenlinie_zweite_achse_wirkt` | dieselbe Woche mit und ohne Defizit und Füllstand |
| Gegenprobe | `summenlinie_xwerte_wirken` | dieselben Werte über ungleichmäßigen x-Stellen und über dem Index |
| SVG-Probe | `svg_zapfprofil_summenlinie`, `svg_zapfprofil_wertepaarkurve`, `svg_zapfprofil_auslegungswoche` | Modellprobe der Gruppe (a): je Reihe ein `path.epos-reihe`, die Fläche geschlossen, die zweite Achse genau dann, wenn das Modell sie führt |

Das sind vier Maßproben und drei Gegenproben — **10 neue Bilder** — und drei SVG-Proben. Kein Bild
von vorher hat sich geändert: auf Windows am selben Rechner gegen den Stand vor der Stufe gemessen,
alle 122 Hashes des Vorstands gleich, 10 neu (132 Zeilen). Die zehn Bilder stehen in der Messlatte.

---

## Zapfprofilgenerator (Stufe Z4): die Dauerlinie der Bilanz

`ZapfprofilBilder.Dauerlinie` zeichnet den Reiter „Dauerlinie" des Zapfprofils über das
`ChartRenderer.SummenlinieModell` der Stufe Z2: die 8 760 Stundenwerte von Zapfung und Zirkulation
absteigend geordnet über dem Rang 1 … 8 760, je Perzentil P50, P90, P95 und P99 eine beschriftete
Strecke an seinem Rang (ganzzahlig ⌈p · 8 760 / 100⌉ wie `Zapfauswertung.Dauerlinie`) und wahlweise
eine waagerechte Vergleichslinie, etwa die Ladeleistung, gestrichelt. Die Probereihe ist die
Probewoche des Wochenprofils samt Zirkulation, über das Jahr wiederholt (`Dauerlinienprobe` in
`Program.Zapfprofil.cs`); die Texte sind die deutsche Vorgabe (`ZapfprofilBildtexte`).

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `zapfprofil_dauerlinie` | 8 760 Ränge mit vier Perzentilmarken und Vergleichslinie, 1244 × 524, Zapfung (`WARMWASSER`) und Vergleich (`SPEICHERLADUNG`) |
| Gegenprobe | `zapfprofil_dauerlinie_marken_wirken` | dieselbe Linie mit und ohne Perzentilmarken |
| Gegenprobe | `zapfprofil_dauerlinie_vergleich_wirkt` | dieselbe Linie mit und ohne Vergleichslinie |
| SVG-Probe | `svg_zapfprofil_dauerlinie` | Modellprobe der Gruppe (a): die Linie als `path.epos-reihe` |

Das sind vier Proben — eine Maßprobe, zwei Gegenproben und eine SVG-Probe — und **5 neue Bilder**.
Kein Bild von vorher hat sich geändert: auf Windows am selben Rechner gegen den Stand vor der Stufe
(`48d8836d`) gemessen, alle 146 Hashes des Vorstands gleich, 5 neu (151 Zeilen; 165 Proben, 0
Verstöße). Die fünf Bilder stehen in der Messlatte.

---

## Etappe E8a der Wirtschaftlichkeit: Brückenbild und Zahlungsstrombild

### Das Brückenbild

`ChartRenderer.KapitalwertBruecke` zeichnet „Von der Investition zur Kapitalwertdifferenz" als
Wasserfall: je Bestandteil (Investition, Betriebskosten, Energiekosten, Erlöse,
Ersatzbeschaffungen, Restwert) eine Säule vom Stand vor bis zum Stand nach dem Schritt — rot,
wenn er die Differenz mindert, grün, wenn er sie mehrt, gestrichelt verbunden —, zuletzt die
Ergebnissäule von null bis zur Summe in der Hausfarbe; ein reines Pixelbild, 1240 × 610. Die
Proben stehen in `Program.Bruecke.cs`; ihre Texte sind die deutsche Vorgabe (`BrueckenTexte`).

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `kapitalwert_bruecke` | die sechs Schritte des Mockups (ΔKW 1.842.695 €), 1240 × 610, die Farben der Rollen `RASTER_GUT`, `RASTER_SCHLECHT`, `STAMM` |
| Maßprobe | `kapitalwert_bruecke_unter_referenz` | eine Brücke, die unter der Referenz endet (ΔKW −410.000 €), dasselbe Maß |
| Maßprobe | `kapitalwert_bruecke_leer` | kein zeichenbarer Schritt: der Leerhinweis, 1240 × 200 |
| Gegenprobe | `kapitalwert_bruecke_schritt_wirkt` | dieselbe Brücke mit anderem Beitrag der Energiekosten |
| Gegenprobe | `kapitalwert_bruecke_reihenfolge_wirkt` | dieselben Schritte in umgekehrter Reihenfolge |
| SVG-Probe | `svg_c_kapitalwert_bruecke` | Pixelbildprobe der Gruppe (c): jede Säule mit ihrem Wert |
| SVG-Probe | `svg_kapitalwert_bruecke_treppe`, `…_treppe_unter_referenz` | die Säulen bilden eine Treppe: jede beginnt auf der Höhe, auf der die vorige endet, die Ergebnissäule reicht von der Nulllinie bis zum letzten Stand |

Das sind drei Maßproben und zwei Gegenproben — **7 neue Bilder** — und drei SVG-Proben.

### Das Zahlungsstrombild

`ChartRenderer.Zahlungsstrom` zeichnet den „Zahlungsstrom je Jahr" EINER Version in einem Szenario
als gestapelte Jahresbalken: je Jahr 0…T die Positionsspalten der Mehrjahrestafel (Investition und
Ersatz, Betrieb, Energie, CO₂-Abgabe, Einspeisung, KWK-Zuschlag, Steuergutschriften, PV-Vergütung,
Pauschale), die Einnahmen von der Nulllinie nach oben, die Ausgaben nach unten, in der Reihenfolge
der Tafel; die Ersatzjahre tragen ein Band hinter ihrem Balken und ein Dreieck am oberen Rand. Eine
Differenz zu einer Referenz zeigt es nicht. Ein reines Pixelbild, 1240 × 620. Die Proben stehen in
`Program.Zahlungsstrom.cs`; ihre Texte sind die deutsche Vorgabe (`ZahlungsstromTexte`), die Beträge
wachsen durch fortgesetzte Multiplikation (auf jedem Rechner bitgleich).

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `zahlungsstrom` | ein BHKW-Stand über 20 Jahre mit allen Spaltenarten und drei Ersatzjahren, 1240 × 620, die Farben `STAMM`, Energie, KWK-Zuschlag und das Dreieck der Ersatzjahre |
| Maßprobe | `zahlungsstrom_ohne_ersatz` | eine PV-Anlage über 10 Jahre ohne Ersatz, dasselbe Maß |
| Maßprobe | `zahlungsstrom_leer` | kein zeichenbarer Betrag: der Leerhinweis, 1240 × 200 |
| Gegenprobe | `zahlungsstrom_ersatzjahr_wirkt` | dieselben Reihen, eines der drei Ersatzjahre fehlt |
| Gegenprobe | `zahlungsstrom_vorzeichen_wirkt` | dieselben Beträge, die CO₂-Abgabe als Einnahme statt als Ausgabe |
| SVG-Probe | `svg_c_zahlungsstrom` | Pixelbildprobe der Gruppe (c): jede Schicht mit ihrem Wert |
| SVG-Probe | `svg_zahlungsstrom_stapel`, `…_stapel_ohne_ersatz` | je Jahr schließen die Einnahmen über, die Ausgaben unter der Nulllinie lückenlos aneinander an, in der Reihenfolge der Tafel; genau die Ersatzjahre tragen eine Marke |

Das sind wieder drei Maßproben und zwei Gegenproben — **7 neue Bilder** — und drei SVG-Proben.

Kein Bild von vorher hat sich geändert: auf Windows am selben Rechner gegen den Stand vor der Etappe
gemessen, alle 132 Hashes des Vorstands gleich, 14 neu (146 Zeilen). Die vierzehn Bilder stehen in
der Messlatte.

---

## Berichtsvorlagen (Etappe BV-E5): die Bildgröße Stufe 2

Ein Bildplatzhalter der Berichtsvorlage zeichnet sein Diagramm im **Zielmaß seines Rahmens**
(`Bildmass`), statt es im festen Maß zu zeichnen und danach zu skalieren
([Konzept Berichtsvorlagen](../../Dokumentation/ueberholt/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md),
Abschnitt 6.5). Die Proben stehen in `Program.Zielgroesse.cs`: die dreizehn Berichtsbilder in zwei
neuen Größen — **halb** (622 × 400 Bildpunkte des Modells, im Bericht die halbe Satzspiegelbreite,
zwei Bilder nebeneinander) und **hoch** (622 × 800). Bilder, deren Höhe den Daten folgt (Balken je
Stand, Spannenbild), nehmen nur die Breite; Brücken-, Spannen- und Szenarienbild zeichnen nicht
schmaler als ihre Mindestbreite (1000, 900 bzw. 760 Bildpunkte), und räumt eine umbrechende Legende
der Zeichenfläche Platz, wächst das Bild in der Höhe.

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `stufe2_<bild>_halb`, `stufe2_<bild>_hoch` | je Bild und Größe Maß, Farben der Rollen und Determinismus: `kuchen`, `jahresverlauf_waerme`, `dauerlinie_waerme`, `strombilanz_monate`, `speicherverlauf`, `speichertemperaturen`, `kapitalwert_absolut`, `kapitalwert_szenarien`, `kapitalwert_bruecke`, `zahlungsstrom` in beiden Größen, `balken_horizontal` und `kapitalwert_spanne` nur halb |
| Schriftprobe | `stufe2_<bild>_<größe>_schrift` | das Bild im Zielmaß ist schmaler als im festen Maß, nicht niedriger als das Zielmaß, und Legende und Achsen tragen **dieselben Schriftgrößen** — neu gezeichnet, nicht verkleinert (der Titel darf einpassen) |

Das sind **22 Maßproben** und 22 Schriftproben; die Maßproben ergeben **22 neue Bilder** in der
Messlatte, die Schriftproben zeichnen kein PNG. Die Bilder im festen Maß bleiben unverändert: Ein
neuer Parameter hat eine Vorgabe, die das Bild byte-gleich lässt (`EPOS.Kern/CLAUDE.md`, „Bericht").

## Gestapelte Jahresganglinien

`ganglinie_normiert_gestapelt` zeichnet die normierte Ganglinie so, wie sie die Bedarfsseite
zeigt: Heizung, Brauchwasser und Prozesswärme als Flächen übereinander
(`Stapelart.Flaeche`), die Summe als schmaler Rand auf der Oberkante des Stapels und der
Jahreshöchstwert der Summe als Bezugswert. Die übrigen Bilder der normierten Ganglinie tragen
keine Flächenreihe.

Jedes gestapelte Bild mit mehr Werten als Bildpunktspalten folgt der **Stufenregel des
Stapels** (`Pfadregel.Stufen`, `Pfadregel.Spitzenstunden`, `Pfadregel.Stundenwerte`,
`Pfadregel.Stufenstuecke`, `Pfadregel.Treppe` in
`EPOS.Kern/Allgemein/Bericht/Zeichnung/Zeichenmodell.cs`): Eine
Stapelschicht und jede Linie, die einen Stapel begleitet (`Datenreihe.Huelle` — Summe, Kontur,
Kanten), zeichnet je Stufe eine waagrechte Kante — im Jahresbild je Tag, solange ein Tag
schmaler als vier Bildpunktspalten ist, sonst je Spalte —, und zwar ALLE mit den Werten
derselben Stunde: der **Spitzenstunde** der Bezugsgröße (`Datenreihe.Bezug`: die Bedarfs- oder
Summenlinie, sonst die Kontur, sonst die Oberkante des Stapels; bei Gleichstand die erste
Stunde). So summieren sich die Schichten je Stufe zur Oberkante, die Dicke jeder Schicht ist
ihr Wert in dieser Stunde, und ein taktender Erzeuger steht nicht als Band auf Nennleistung da.
Nachbarschichten teilen ihre Kante genau, die Schichten decken voll, und PNG und Druck malen sie
von der Achse, die oberste zuerst — keine Lücke, nichts scheint durch, die Summe liegt als Rand
auf der Oberkante. Unter dem Achsentitel steht „je Tag die Stunde der Tagesspitze“
(`CHART_HINWEIS_STUFE_TAG`). Die überlagerte Produktion der Wärmepumpenseite (eine Säulengruppe
über einer Flächengruppe) steht als Kanten über dem Bedarf, nicht halbtransparent. Die
Dauerlinie stapelt nicht.

**Eine Lücke bleibt Lücke.** Ein nicht endlicher Wert (NaN — etwa „aus“ einer Kalenderwoche)
wählt keine Spitzenstunde; eine Stufe ohne endlichen Bezugswert nimmt ihre erste Stunde. Steht
eine Reihe in der Spitzenstunde einer Stufe nicht endlich da — eine Schicht an Ober- oder
Unterkante —, ist die Stufe für sie eine Lücke: Die Treppe bricht dort ab und setzt danach mit
eigenem Teilpfad neu an (`Pfadregel.Stufenstuecke`; im SVG je Stück ein Teilpfad, eine Schicht
mit `Z` geschlossen, im PNG je Stück ein Vieleck oder Linienzug). Die Lücke steht nie als Null
da, keine Nachbarstufe reicht in sie hinein, und die Zeigerzeile nennt die Schicht in dieser
Stufe nicht. Ein Vormittag „aus“ an einem Tag mit der Spitze am Abend ist deshalb keine Lücke,
ein Tag, dem allein die Spitzenstunde fehlt, schon. Die Proben stehen in `PfadregelTests.cs`
(`EineLueckeBleibtLuecke`), `SvgSchreiberTests.cs`
(`EineStapelschichtMitLueckeZeigtDieLueckeAlsLuecke`) und `ChartRendererTests.cs`
(`Eine_Luecke_im_Stapel_bleibt_im_PNG_eine_Luecke`); kein Probebild trägt eine Stapelschicht mit
Lücke, die Messlatte berührt das nicht.

Die Regel ändert genau die zwölf Bilder mit dichtem Stapel: `ganglinie_normiert_gestapelt`,
`erzeugerstapel_waerme`, `erzeugerstapel_strom_viertelstunden`, `erzeugerstapel_zwei_speicher`,
`erzeugerstapel_neun_reihen` samt `…_wenige` und `…_viele`, `erzeugerstapel_fenster` samt
`…_a` und `…_b` sowie `erzeugerstapel_zweite_achse_a` und `…_b` (`erzeugerstapel_fenster` und
`…_fenster_b` zeichnen einen Ausschnitt, der nicht dichter ist als die Spalten — sie ändern sich
nur durch die deckenden Schichten). Byte-gleich bleiben die Dauerlinien
(`erzeugerstapel_kessel_sortiert`, `ganglinie_normiert_sortiert`), die Bilder ohne
Stapelschicht (`erzeugerstapel_solar_zwei_linien`) und der Berichtsstapel `jahresverlauf_waerme`
(Tagesmittel, nicht dichter als die Spalten). Die Proben „Schichten summieren sich in jeder
Stufe zur Oberkante“, „kein taktender Erzeuger als Nennleistungsband“, „keine Löcher“ und „Summe
scheint nicht durch“ stehen in `EPOS.Kern.Tests/ChartRendererTests.cs` und
`PfadregelTests.cs`, die Treppen in `SvgSchreiberTests.cs`.

## Konditionierungsprofile (KP2, Welle K4): Teppichbild und Woche mit Lücke

`ChartRenderer.KalenderteppichModell` (Datei `ChartRenderer.Kalenderteppich.cs`) zeichnet das Teppichbild eines
Kalenders aus `Kalenderteppich` (Kern): x die 365 Tage des Bezugsjahres, y die 24 Stunden, Farbe der Wert. Stunden
gleicher Farbstufe und Folgetage mit denselben Läufen und derselben Quelle werden zu Rechtecken; „aus" ist die eigene
Rolle `RASTER_LOCH` mit einer Schraffur als EIN Streckenzug je Fläche; über 2 000 Elementen zeichnet das Bild benannt
gröber. `KalenderwocheModell` ist die Woche einer Größe mit Einheit; NaN („aus") bricht die Fläche des Stundenprofils
(ohne NaN ist das Stundenprofil bitgleich). Die Proben stehen in `Program.Kalenderteppich.cs`, Bezugsjahr 2025:

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `kalenderteppich_heizen_buero` | Heizen „Büro" mit Neujahr, Herbstferien und Heizperiode; 1244 × 464, `HEIZWAERME` und `RASTER_LOCH` |
| Maßprobe | `kalenderteppich_lueftung_aus` | Lüftung, nachts und am Wochenende „aus"; `SERIE_6` und `RASTER_LOCH` |
| Maßprobe | `kalenderwoche_aus` | die Lüftungswoche mit „aus" als Lücke; `PROFILLINIE` und die Profilfläche |
| Gegenprobe | `kalenderteppich_aus_wirkt` | dieselbe Woche mit 0 statt „aus" zeichnet anders |
| Gegenprobe | `kalenderteppich_ferien_wirkt` | Heizen mit und ohne Herbstferien |
| Gegenprobe | `kalenderwoche_luecke_wirkt` | die Woche mit und ohne Lücke |
| SVG-Probe | `svg_c_kalenderteppich_heizen_buero`, `svg_c_kalenderteppich_lueftung_aus` | Pixelbildprobe der Gruppe (c): jedes Feld mit `data-wert`, Titel, Legende schaltet vorhandene Elemente |
| SVG-Probe | `svg_kalenderteppich_grenze_und_aus` | jedes Teppichbild höchstens 2 000 Elemente (Büro 169, Lüftung 521, Grenzbild 121), das Grenzbild nennt seine Vergröberung; jedes „aus"-Feld `RASTER_LOCH` mit genau einer Schraffur in `RAHMEN`; die Werte am Feld wörtlich |
| SVG-Probe | `svg_kalenderwoche_luecke` | die Fläche der Woche zerfällt in fünf Teilpfade (roh und gebündelt), kein „NaN", die Einheit steht an der Reihe; ohne Lücke ein Zug |

Das sind drei Maßproben und drei Gegenproben — **9 neue Bilder** — und vier SVG-Proben. Die
Teppichbilder ändern kein Bild von vorher. Die Woche ist ein Stundenprofil ohne Stapelschicht
und geht deshalb nicht über die Stufenregel des Stapels; ihre Lücke ist die des Flächenzugs
(`SvgSchreiber.Flaechenzug`). `Messlatte_2026-09-30.sha256` trägt beide Stände — die neun
Teppichbilder und die zwölf Bilder der Stufenregel (Abschnitt oben). Die Windows-Messliste des
Gates (`Werkzeuge/Gate/LIESMICH.md`) ist auf Windows nachzuziehen: diese zwölf Zeilen geändert,
neun neu, alle übrigen gleich.

## Kesselkennlinie (Konzept Kesselkennlinie 5): die kleine Kurve des Kesseleditors

`ChartRenderer.KesselkennlinieModell` (Datei `ChartRenderer.Kesselkennlinie.cs`) zeichnet den Wirkungsgrad über der Last,
720 × 430: x die Laststufe in Prozent der Nennleistung, y der Wirkungsgrad als Faktor, eine Linie samt Punktmarken je
Reihe — mit Brennwertkennlinie eine je Rücklauf (30, 50, 60 °C), ohne sie eine. Ein reines Pixelbild ohne Zeichenfläche wie
die Kennlinien der Wärmepumpe; jede Punktmarke nennt ihren Wert, jede Linie führt eine Datenreihe mit der Last als x-Stelle,
Linien und Legendenfelder tragen die Serienrolle. Die Punkte rechnet der Kern (`Kesselkennlinie.Kurven`) aus fest
verdrahteten, neutralen Katalogwerten (Gas-Brennwertkessel, η₁₀₀ 0,97, η₃₀ 1,07). Die Proben stehen in
`Program.Kesselkennlinie.cs`:

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `kesselkennlinie_brennwert` | drei Rückläufe in `SERIE_1` bis `SERIE_3` |
| Maßprobe | `kesselkennlinie_teillast` | die Teillastkennlinie allein in `SERIE_1` |
| Gegenprobe | `kesselkennlinie_brennwert_wirkt` | der Schalter Brennwertkennlinie kommt im Bild an |
| Gegenprobe | `kesselkennlinie_eta30_wirkt` | ein gepflegtes η₃₀ zeichnet anders als die Normvorgabe |
| SVG-Probe | `svg_kesselkennlinie_brennwert` | Pixelbild der Gruppe (b): keine Zeichenfläche, Reihenbefehle mit Marke, Datenreihen mit x-Stelle |
| SVG-Probe | `svg_kesselkennlinie_werte` | 30 Werte an den Punktmarken, die Prüfpunkte wörtlich („Last 30 % · Rücklauf 30 °C: 1,070“, „Last 100 % · Rücklauf 60 °C: 0,970“), drei Legendeneinträge; ohne Brennwertkennlinie eine Linie |

Das sind zwei Maßproben und zwei Gegenproben — **6 neue Bilder** — und zwei SVG-Proben. Kein Bild von vorher ändert sich:
Die 194 Zeilen der bisherigen Messlatte stehen unverändert in `Messlatte_2026-09-30.sha256`, dazu die sechs neuen. Die
Windows-Messliste des Gates (`Werkzeuge/Gate/LIESMICH.md`) ist auf Windows nachzuziehen: sechs Zeilen neu, alle übrigen
gleich.

## Die Stromlast des BHKW: das zweite Bild des BHKW-Reiters

Unter der Wärmelast zeigt der BHKW-Reiter die „Stromlast Jahresganglinie“ (`Bilder.BhkwStrom`): die
Stromproduktion als Säule, darüber Stromeinspeisung, Reststrombedarf und Strombedarf als Linien, in den
Farbrollen des Stromgangs (`STROM_BHKW`, `UEBERSCHUSS`, `REST`, `BEDARF`). Gezeichnet wird über
`ChartRenderer.ErzeugerStapel` wie die Wärmelast; die synthetischen Reihen stehen zueinander wie im Lauf
(Einspeisung = Produktion über dem Bedarf, Reststrom = Bedarf über der Produktion). Die Proben stehen in
`Program.BhkwStrom.cs`:

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `bhkw_strom_ganglinie` | vier Reihen über den Monaten in den vier Farbrollen |
| Maßprobe | `bhkw_strom_sortiert` | dieselben Reihen als Dauerlinie über den Jahresstunden |
| Gegenprobe | `bhkw_strom_reihenwahl_wirkt` | die Abwahl der Einspeisung kommt im Bild an |
| Gegenprobe | `bhkw_strom_sortiert_wirkt` | „sortiert“ zeichnet anders als die Ganglinie |
| SVG-Probe | `svg_bhkw_strom_ganglinie` | das Modell der Zeichenfläche: Flächen, Linien, Datenreihen |

Das sind zwei Maßproben und zwei Gegenproben — **6 neue Bilder** — und eine SVG-Probe. Kein Bild von vorher ändert
sich: Die 200 Zeilen der bisherigen Messlatte stehen unverändert in `Messlatte_2026-10-03.sha256`, dazu die sechs
neuen. Die Windows-Messliste des Gates (`Werkzeuge/Gate/LIESMICH.md`) ist auf Windows nachzuziehen: sechs Zeilen
neu, alle übrigen gleich.

## Kälteproduktion (Ergebnisreiter „Kälte Produktion Chart“)

`KaelteProduktionBild` (Datei `EPOS.Kern/Allgemein/Bericht/KaelteProduktionBild.cs`) zeichnet die Jahresganglinie der
Kälteerzeugung, 1 240 × 560, über `ChartRenderer.ErzeugerStapelModell`: je Kälteerzeuger seine gedeckte Kälte als Säule
(erster Erzeuger in `WAERME_WP`), darauf die ungedeckte Kälte in `REST`, darüber der Kältebedarf als Linie in `BEDARF`.
Die synthetischen Reihen stehen in `Program.cs` (`KaelteProduktionSatz`): ein Bedarf mit Sommerspitze, die Wärmepumpe
deckt bis 30 kW.

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `kaelte_produktion` | Wärmepumpe, ungedeckte Kälte und Kältebedarf in ihren Rollenfarben |
| Maßprobe | `kaelte_produktion_ohne_erzeuger` | ohne Kälteerzeuger ist der ganze Bedarf ungedeckt: Rest und Bedarfslinie |

Das sind **2 neue Bilder**. Kein Bild von vorher ändert sich: Die 200 Zeilen der bisherigen Messlatte stehen unverändert
in `Messlatte_2026-10-03.sha256`, dazu die zwei neuen. Die Windows-Messliste des Gates (`Werkzeuge/Gate/LIESMICH.md`)
ist auf Windows nachzuziehen: zwei Zeilen neu, alle übrigen gleich.

## Raumtemperatur und Sollwert (Anlagenkopplung AK2, Bedarfsdialog)

`ChartRenderer.Komfortwoche` zeichnet die Woche mit der größten Unterschreitung (`Komfortwoche.GroessteUnterschreitung`),
1 240 × 560: die Raumluft in `SERIE_1`, den Heizsollwert gestrichelt in `SERIE_3` und die gezählten
Unterschreitungsstunden als dritte Reihe in `SERIE_2` — nur in diesen Stunden, sonst Lücke. Die synthetische Woche steht
in `Program.Anlagenkopplung.cs`: Sollwert 21/17 °C, an den ersten drei Morgen 1,5 bis 3 K Unterschreitung.

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `komfortwoche_gebaeude` | Raumluft, Markierung und Sollwert in ihren Rollenfarben |
| Gegenprobe | `komfortwoche_markierung_wirkt` | die Markierung der Unterschreitung kommt im Bild an |
| SVG-Probe | `svg_komfortwoche_markierung` | drei Reihenpfade; die Markierung zerfällt in einen Teilpfad je Morgen, kein „NaN“ |

Das sind eine Maßprobe und eine Gegenprobe — **3 neue Bilder** — und eine SVG-Probe. Kein Bild von vorher ändert sich:
Die 208 Zeilen der bisherigen Messlatte stehen unverändert in `Messlatte_2026-10-05.sha256`, dazu die drei neuen. Die
Windows-Messliste des Gates (`Werkzeuge/Gate/LIESMICH.md`) ist auf Windows nachzuziehen: drei Zeilen neu, alle übrigen
gleich.

## Bivalenzdiagramm der Wärmepumpe (Übergabegrenze UB‑E4)

`ChartRenderer.Bivalenzdiagramm` zeichnet das `BivalenzdiagrammModell` (900 × 540): Leistung über der Außentemperatur mit
den Bereichsflächen „nur Kessel“ (`WAERME_KESSEL`), „Vorwärmung“ (`UEBERSCHUSS`), „parallel“ (`SERIE_3`) und „Wärmepumpe
allein“ (`SERIE_2`), der Heizlast (`BEDARF`), dem Kennfeld bei Höchstvorlauf (gestrichelt, `SERIE_3`), der Übergabegrenze
(gepunktet, `SERIE_8`), der Wärmepumpe als Fläche (`SERIE_2`), wahlweise den Stundenpunkten (`SERIE_5`) und den Marken
θ_biv,1, θ_biv,2, Abschaltpunkt und „nach Kennfeld“. Ohne Übergabedaten steht der Platzhalter „kein Bivalenzdiagramm —
Kopplung aus“. Herleitung und Modell rechnet der Kern aus dem Zahlenbeispiel des Fachkonzepts (Heizkörper 75/60/20 °C,
10 kW; Gebäude 10 kW bei −12 °C; Kennfeld 7 bis 9 kW; σ_min 3 K; teilparallel mit Vorwärmbetrieb, Abschaltpunkt −10 °C);
die Proben stehen in `Program.Bivalenzdiagramm.cs`:

| Art | Probe | Aussage |
|---|---|---|
| Maßprobe | `bivalenzdiagramm` | Heizlast, Kennfeld und Übergabegrenze in ihren Rollenfarben, Determinismus |
| Maßprobe | `bivalenzdiagramm_stunden` | dasselbe mit 400 synthetischen Stundenpunkten in `SERIE_5` |
| Maßprobe | `bivalenzdiagramm_platzhalter` | der Platzhalter ohne Übergabedaten |
| Gegenprobe | `bivalenz_kennfeld_wirkt` | ein anderes Kennfeld (6 statt 7 kW bei −7 °C) zeichnet anders |
| Gegenprobe | `bivalenz_vorwaermung_wirkt` | ohne Vorwärmbetrieb zwei Flächen statt drei |
| Gegenprobe | `bivalenz_stunden_wirkt` | die Stundenpunkte kommen im Bild an |
| Gegenprobe | `bivalenz_platzhalter_wirkt` | der Platzhalter ist ein eigenes Bild |

Das sind drei Maß- und vier Gegenproben — **11 neue Bilder**. Kein Bild von vorher ändert sich: Die 211 Zeilen von
`Messlatte_2026-10-05.sha256` stehen unverändert in `Messlatte_2026-10-09.sha256`, dazu die elf neuen. Die
Windows-Messliste des Gates (`Werkzeuge/Gate/LIESMICH.md`) ist auf Windows nachzuziehen: elf Zeilen neu, alle übrigen
gleich.
