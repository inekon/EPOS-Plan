# DG-E3 — Der Rollout je Diagrammart (Protokoll)

Etappe E3 des Konzepts
[`Konzept_Diagramme_Interaktiv_EPOS-Plan.md`](../../../aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md):
Jede Diagrammart bekommt ihr öffentliches Zeichenmodell und ihren SVG-Baustein, Gruppe für
Gruppe. Dieses Papier sammelt die Abschnitte der vier Gruppen; der gültige Stand steht in der
Statusdatei und im Konzept, hier steht, wie es geworden ist.

Vorgänger: [`DG_E2_SvgBaustein_Protokoll.md`](DG_E2_SvgBaustein_Protokoll.md) — dort stehen die
Schnittstelle des Modells, die Entscheide DG-E2-1…4 und die Messlatte, an der jede Etappe
gemessen wird.

---

## Gruppe (a) — Kern: sieben Zeitreihen-Modelle

### Die Aufgabe

Die sieben Zeitreihen-Diagramme der Simulations-Ergebnisreiter — `Kostenprofil`,
`Stundenprofil`, `Jahresverlauf`, `GanglinieNormiert`, `ErzeugerStapel`, `Temperaturverlauf`,
`Speicherbetrieb` — bekommen ein **öffentliches Zeichenmodell** für die SVG-Ausgabe, so wie
`Jahresgang` es mit E2 bekommen hat. Damit tragen sie dieselbe Wahrheit wie der Bericht und
lösen den Rundlauf-Datenzoom an diesen Stellen ab.

Drei davon können, was der Jahresgang nicht konnte, und deshalb ist die Etappe mehr als eine
Wiederholung: Der **Erzeugerstapel** führt gestapelte Flächen UND eine zweite y-Achse, das
**Stundenprofil** eine Fläche mit Randlinie, und `Kostenprofil` wie `Stundenprofil` zählen auf
ihrer x-Achse keine Jahresstunde, sondern den Index der Reihe.

**Die unverrückbare Bedingung blieb: kein PNG darf sich ändern.** Gemessen an der
Windows-Messlatte dieses Rechners (91 Hashes aus E0/E1), nach jedem Commit.

### Was entstanden ist

| Datei | Was dazugekommen ist |
|---|---|
| `EPOS.Kern/Allgemein/Bericht/Zeichnung/Zeichenmodell.cs` | `Reihenart`; `Datenreihe` um `Fenster`, `Art`, `Unten` und `Randton` erweitert (`Gleicht` nachgezogen); `Pfadregel.GebuendelteKante` |
| `EPOS.Kern/Allgemein/Bericht/Zeichnung/SvgSchreiber.cs` | die neue Senkrechte, die Flächen, der **öffentliche** `Reihenpfad` samt Fenster-Überladung |
| `EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs` | sieben `…Modell`-Methoden; `VerlaufsbildModell` als gemeinsamer Rumpf von Temperaturverlauf und Speicherbetrieb; `Reihenfenster`; die drei Rasterhelfer in `…OhneKreuz` aufgeteilt |
| `Proben/ChartProben/Program.cs`, `LIESMICH.md` | acht SVG-Gegenproben und der Schalter `--svg-alle <ordner>` |
| `EPOS.UI/Bausteine/DiagrammSvg.razor` | `data-voll` und die Zeigerlinie auf `Flaeche.Bild.Hoehe` nachgezogen (siehe unten) |
| `EPOS.Kern.Tests/` | `SvgSchreiberTests` +6, `ZeichenmodellTests` +2, `PfadregelTests` +4, `ChartRendererTests` +9; zwei E2-Fälle auf die neue Senkrechte umgestellt |

### Die Entscheide

**DG-E3-1 — Senkrechte in Bildpunkten, jede Reihe mit eigenem Fenster.** Das innere
`<svg class="epos-flaeche">` trägt in x weiterhin Stunden, in y aber **Bildpunkte der
Zeichenfläche**: `viewBox = "XVon 0 Breite Bild.Hoehe"`. Jede `Datenreihe` bringt ihr eigenes
`Datenfenster` mit — den x-Bereich und den y-Bereich **ihrer** Achse —, und der Schreiber
rechnet `y = Bild.Hoehe − (Wert − YVon) / (YBis − YVon) · Bild.Hoehe`.
`Zeichenflaeche.Daten` bleibt das Fenster der linken Achse und die Vorgabe für Reihen ohne
eigenes.

*Warum.* Mit der E2-Regel (`y' = YBis − y`, die viewBox in Werteinheiten) kann ein Bild genau
EINE y-Achse tragen. Der Erzeugerstapel hat zwei: Links stehen Leistungen in kW, rechts der
Speicherinhalt in kWh, und ihre Nullpunkte liegen nicht übereinander. Ein zweites inneres
`<svg>` daneben wäre eine zweite `viewBox`, die bei jedem Zoom mitgeführt werden müsste —
zwei Stellen für eine Geste, und der Zoom ist im Konzept ausdrücklich „eine
Attributänderung". Die Bildpunkt-Senkrechte löst beides: Ein Zoom auf der Zeitachse ändert
weiterhin nur `x` und `width`, und jede Reihe bringt ihre Skala selbst mit.

Die x-Seite gewinnt dasselbe: Reihen, die das PNG über einen anderen x-Bereich zeichnet — die
beiden **nebeneinander** gestellten Stapelgruppen, das um ein Fach versetzte Stundenprofil,
eine kürzere Reihe neben einer längeren —, stehen im SVG an derselben Stelle wie im Bild,
ohne Sonderweg im Schreiber.

**DG-E3-2 — Flächen.** `Datenreihe` bekommt `Art` (`Linie` | `Flaeche`) und ein optionales
`double[] Unten`, die untere Grenze einer Stapelschicht (`null` = Achsennull, in das Fenster
geklemmt). Eine Fläche wird im SVG ein geschlossener `<path>` — Oberkante vorwärts,
Unterkante rückwärts, `Z` — mit `fill` in der Reihenfarbe samt der Deckung ihres Farbtons und
ohne Strich, es sei denn, das PNG zieht eine Randlinie. Gebündelt wird eine Fläche über
`Pfadregel.GebuendelteKante`: je Bildpunktspalte der Höchstwert der Oberkante und der
Kleinstwert der Unterkante — die **konservative Hülle**, nie kleiner als die rohe Fläche und
bei 1:1 von ihr nicht zu unterscheiden. Linien bündeln wie bisher (Minimum UND Maximum je
Spalte). Deterministisch, kulturfrei.

*Warum ein Punkt je Spalte und nicht zwei.* Eine Fläche ist ein geschlossener Zug; zwei
Punkte je Spalte ergäben an der Oberkante ein Sägeblatt, das Fläche wegnimmt, die im Bild
steht.

**DG-E3-3 — Nachladen ohne Kern.** `SvgSchreiber.Reihenpfad` ist **öffentlich** und hat eine
Überladung mit Fenster (`von`, `bis` in der Einheit der x-Achse, `roh` erzwingbar). Der
Baustein rechnet damit beim Zoom über das Vierfache den sichtbaren Ausschnitt roh aus den
Datenreihen des Modells nach — kein Rundlauf, das Modell trägt die Werte ohnehin. Damit ist
DG-E2-4 eingelöst, und zwar genau dann, wenn der erste gebündelte Pfad entsteht.

Der Fensterpfad einer rohen Reihe ist **der Ausschnitt des Vollpfads**: dieselben Punkte,
dieselben Zahlen (`SvgSchreiberTests.DerFensterpfadIstDerAusschnittDesVollpfads`).

### Was x je Bild bedeutet

Das Modell nennt die **Einheit** der x-Achse nicht — der Baustein liest sie aus seinem eigenen
Parameter. Was x **zählt**, steht dagegen fest:

| Bild | x im Datenfenster | Anmerkung |
|---|---|---|
| `Jahresgang` (E2) | Jahresstunde 0 … n−1 | im Fenster dessen Grenzen |
| `Kostenprofil` | **Index der Reihe** 0 … n−1 | Das Bild legt das Profil über seine EIGENE Länge auf die Monatsachse 0…12; ein Profil muss keine 8 760 Werte führen |
| `Stundenprofil` | **Index der Reihe**; Fläche 0 … n, Reihe **1 … n** | Wert `i` steht am rechten Rand seines Fachs (Stunde n meint das Intervall (n−1, n]) — genau das drückt das eigene Fenster der Reihe aus |
| `Jahresverlauf` | Jahresstunde 0 … n−1 | im Fenster dessen Grenzen |
| `GanglinieNormiert` | Stützstelle 0 … n−1 | Stunden oder Viertelstunden, je nach Reihenlänge; in der **Dauerlinie** zählt x den Rang, nicht die Stunde |
| `ErzeugerStapel` | Stützstelle 0 … n−1 | nebeneinander gestellte Gruppen sitzen über ihr eigenes Fenster in ihrer Bildhälfte |
| `Temperaturverlauf` | Stützstelle 0 … n−1 | |
| `Speicherbetrieb` | Stützstelle 0 … n−1 | in der Dauerlinie der Rang |

Die y-Seite: `Zeichenflaeche.Daten` ist immer die **linke** Achse. `GanglinieNormiert` führt
dort Prozent (0…100,2), `Temperaturverlauf` und `Speicherbetrieb` eine vorzeichenfähige Achse
**ohne Nullpunkt**, die übrigen eine Skala ab null (das Kostenprofil ab der negativen Stufe,
wenn die Reihe unter null läuft). Reihen der zweiten Achse tragen ihr eigenes Fenster von null
bis zum geglätteten Höchstwert.

### Vier Stellen, an denen die Umsetzung über den Auftrag hinausgeht

1. **`Datenreihe` bekommt ein viertes Feld: `Randton`.** Der Entscheid nennt `Art` und
   `Unten`. Das Stundenprofil zieht aber eine Randlinie in einer **anderen** Farbe als die
   Füllung (`PROFILLINIE` über `PROFILFLAECHE`) — „dann wie dort" lässt sich ohne eine zweite
   Farbe nicht sagen. Die Alternative wären zwei `Datenreihe` für eine gezeichnete Reihe
   gewesen: dieselbe Reihe zweimal in `Modell.Reihen`, zweimal in der Zeigerzeile des
   Bausteins, zweimal in jeder Zählung. Ein optionales Feld ist die kleinere Änderung.
   `Staerke` ist dann die Breite des Randes; ohne `Randton` bekommt die Fläche keinen Strich —
   so zeichnet der Stapel.
2. **Die Fläche des Stundenprofils beginnt im SVG bei x = 1, nicht am linken Achsenrand.**
   Der PNG-Zug führt vor dem ersten Wert noch einen Zwickel über das erste Fach
   (`(rc.Left, rc.Bottom)` → `(rc.Left, punkte[0].Y)`), weil er aus Bildpunkten gebaut ist.
   Im SVG entsteht die Fläche aus den DATEN, und der erste Wert sitzt bei x = 1. Der
   Unterschied ist ein Streifen von 1/168 der Breite (rund 6 Bildpunkte von 1 044) an der
   linken Kante; im Bildvergleich ist er nicht auszumachen. Ihn zu schließen hieße, dem
   Schreiber eine Sonderregel „wenn die Reihe rechts der Fläche beginnt, ziehe die
   Unterkante bis zum Flächenrand" zu geben — eine Regel für genau ein Bild.
3. **Drei Rasterhelfer sind aufgeteilt** (`BedarfsRaster`, `YRaster`, `ProzentRaster` →
   `…OhneKreuz` plus `Achsenkreuz`). Der Grund steht schon in E2: Das Achsenkreuz gehört
   weder zu `xachse` noch zu `yachse` — blendet die Oberfläche beim Zoom die Teilung einer
   Achse aus, müssen die beiden Achsenlinien stehen bleiben. Die Befehlsreihenfolge ändert
   sich dabei nicht, das PNG also auch nicht.
4. **Eine Stelle der OBERFLÄCHE rechnete die viewBox nach — und musste mit.** Der Baustein
   `DiagrammSvg` schreibt die vollen Grenzen als `data-voll` an das innere `<svg>` (E2,
   Abweichung 1 des UI-Teils) und zieht die Zeigerlinie über dessen Höhe; beides rechnete er
   selbst aus dem Datenfenster (`YBis − YVon`). Mit DG-E3-1 wich `data-voll` von der `viewBox`
   des Schreibers ab, und das JS-Modul hätte beim Zoom gegen falsche Grenzen geklemmt. Beide
   Stellen lesen jetzt `Flaeche.Bild.Hoehe`. **Gefunden hat es der bunit-Fall
   `DS1_Der_Baum_traegt_Flaeche_und_je_Reihe_einen_Pfad`**, der `viewBox` und `data-voll`
   gegeneinander hält — der Fall selbst blieb unverändert, geändert hat sich der Baustein.
   Das ist die einzige Zeile der Oberfläche, die dieser Auftrag angefasst hat; der übrige
   UI-Teil der Gruppe (a) steht noch aus.

### Was an der Byte-Gleichheit schwierig war

1. **Die Reihenfolge der Befehle ist die Byte-Gleichheit.** `Markiert` sammelt und setzt die
   Marke neu; solange kein Block verschoben wird, bleibt das Bild wörtlich dasselbe. Die
   Arbeit bestand deshalb darin, die Klammern GENAU um die vorhandenen Blöcke zu legen — auch
   dort, wo ein `using (var f = Schrift(15f))` zwei Texte für zwei verschiedene Achsen
   enthält (Stundenprofil: erst `xTitel`, dann `yTitel`; sie bekommen zwei Klammern in
   derselben Reihenfolge).
2. **Die Zeichenfläche muss VOR dem ersten Reihenbefehl stehen.** Der Schreiber setzt das
   innere `<svg>` an die Stelle des ERSTEN Befehls mit der Marke `reihe:…` und übergeht jeden
   weiteren. Steht die Fläche später im Rumpf, zeichnet das SVG die Reihen trotzdem — aber
   das Modell wäre ein anderes, und die Reihenfolge von Raster, Reihen und zweiter Achse
   stimmte nicht mehr mit dem PNG überein. In allen sieben Methoden steht `z.Flaeche = …`
   deshalb unmittelbar vor dem Zeichnen der ersten Reihe.
3. **Die Deckung der Stapelflächen gehört zur Farbe der Datenreihe.**
   `StapelZeichnen` malt mit `r.Farbe.WithAlpha(alpha)` — 210 im Regelfall, 150 für die
   überlagerte Säulengruppe. Die `Datenreihe` nimmt denselben Ausdruck; die Rückwärtssuche
   der Palette gibt der Farbe ihre Rolle zurück und trägt die Deckung als Abwandlung. Ein
   `Ton()` auf der undurchsichtigen Farbe hätte im SVG eine andere Fläche ergeben als im PNG.
4. **`Temperaturverlauf` und `Speicherbetrieb` teilen sich einen Rumpf.** Aus `Verlaufsbild`
   (`byte[]`) wurde `VerlaufsbildModell` (`Zeichenmodell`); beide öffentlichen Methoden gehen
   über `SkiaMaler.Png(…Modell(…))`. Der Wächter `ZeichenmodellWacheTests` zählt weiterhin 26
   `public static byte[]` und verlangt mindestens ebenso viele Übergaben an `SkiaMaler.Png` —
   die Aufteilung nimmt sechs Übergaben aus Leerfällen heraus und legt sieben Ausdrucksformen
   dazu; die Zahl bleibt weit über 26.

**Kein Bild ist gewandert.** Der Messlatte-Diff war nach jedem Schritt am Renderer leer; es
gab keinen Fall, in dem ein Bild zu untersuchen gewesen wäre.

### Die Schnittstelle, die der UI-Teil benutzt

| Was | Signatur |
|---|---|
| Reihe | `sealed record Datenreihe(string Name, Farbton Ton, float Staerke, Strichmuster Muster, double[] Werte, Datenfenster Fenster = null, Reihenart Art = Reihenart.Linie, double[] Unten = null, Farbton Randton = null)` |
| Reihenart | `enum Reihenart { Linie = 0, Flaeche = 1 }` |
| Pfad einer Reihe | `string SvgSchreiber.Reihenpfad(Datenreihe, Zeichenflaeche, bool roh)` |
| … für einen Ausschnitt | `string SvgSchreiber.Reihenpfad(Datenreihe, Zeichenflaeche, double von, double bis, bool roh)` |
| Flächenbündelung | `IReadOnlyList<Punkt> Pfadregel.GebuendelteKante(double[] werte, int spalten, bool hoechstwert)` |
| Kostenprofil | `Zeichenmodell ChartRenderer.KostenprofilModell(string titel, double[] stundenwerte, string einheit, string achseMonat)` |
| Stundenprofil | `Zeichenmodell ChartRenderer.StundenprofilModell(string titel, double[] werte, int intervall, string xTitel, string yTitel)` |
| Jahresverlauf | `Zeichenmodell ChartRenderer.JahresverlaufModell(string titel, double[] stundenwerte, string yTitel, SKColor farbe, Achsenfenster fenster = null)` |
| Normierte Ganglinie | `Zeichenmodell ChartRenderer.GanglinieNormiertModell(string titel, IReadOnlyList<Reihe> reihen, string yTitel, Achse achse, bool sortiert, Achsenfenster fenster = null)` |
| Erzeugerstapel | `Zeichenmodell ChartRenderer.ErzeugerStapelModell(string titel, IReadOnlyList<Reihe> stapel, IReadOnlyList<Reihe> linien, Reihe kontur, string yTitel, Achse achse, bool sortiert, IReadOnlyList<Reihe> zweiteAchsen = null, string y2Titel = null, Achsenfenster fenster = null)` |
| Temperaturverlauf | `Zeichenmodell ChartRenderer.TemperaturverlaufModell(string titel, IReadOnlyList<Reihe> reihen, bool minAuto, Achsenfenster fenster = null)` |
| Speicherbetrieb | `Zeichenmodell ChartRenderer.SpeicherbetriebModell(string titel, IReadOnlyList<Reihe> reihen, string yTitel = null, Reihe ladezustand = null, string y2Titel = null, bool sortiert = false, Achsenfenster fenster = null)` |

Die Griffe im Markup bleiben die der Etappe E2 (`class="epos-flaeche"`, `class="epos-reihe"`,
`data-reihe`, `data-marke`); neu ist die Marke **`yachse2`** für Rasterlinie, Beschriftung und
Titel der rechten Achse — die Oberfläche blendet sie zusammen mit ihrer Reihe aus, wenn der
Anwender die Reihe über die Legende abwählt.

### Nachweis

| Prüfung | Ergebnis |
|---|---|
| Windows-Messlatte des Rechners (91 Hashes) | 91 von 91 gleich, Text-Diff leer — nach jedem Schritt am Renderer |
| `Proben/ChartProben` | **84 Bilder geprüft, 0 Verstöße** (76 wie bisher, dazu acht SVG-Gegenproben); 91 Hashes geschrieben, die Messlatte bleibt bei 91 Zeilen |
| `WP-Plan.Kern.slnf` Bau und volle Suite mit den CI-Schaltern | grün |
| `SvgSchreiberTests` | 29 (23 aus E2, zwei davon auf DG-E3-1 umgestellt, 6 neu) |
| `PfadregelTests` | 10 (6 aus E2, 4 neu) |
| `ZeichenmodellTests` | 20 (18 aus E2, 2 neu) |
| `ChartRendererTests` | 30 (21 aus E2, 9 neu) |
| `ZeichenmodellWacheTests`, `SkiaMalerTests`, `FarbpaletteTests`, `ErgebnisbilderTests` | grün, die Wächter unverändert |
| bunit `DiagrammSvgTests`, `KlimadatenDialogTests`, `DiagrammTests`, `ChartBildTests` | 106 grün — **kein Fall geändert**; der eine, der `viewBox` und `data-voll` gegeneinander hält, hat den Nachzug im Baustein erzwungen (Abweichung 4) |
| Sichtprüfung `--svg-alle` gegen `--ablage` (Edge kopflos) | `erzeugerstapel_waerme`, `erzeugerstapel_zwei_speicher`, `kostenprofil`, `stundenprofil_woche`: gleiche Struktur, gleiche Farben, gleiche Achsen und Legenden. Der sichtbare Unterschied ist der gewollte: Das SVG zeigt den vollen Tagesgang (roh bzw. konservative Hülle), das PNG das auf jeden n-ten Wert ausgedünnte Band (DG-E2-2) |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

### Offen nach dem Kernteil der Gruppe (a)

* **Der UI-Teil der Gruppe:** die sieben Bilder der Ergebnisreiter auf `DiagrammSvg`
  umstellen, das **Nachladen ab dem Vierfachen** über die Fenster-Überladung von
  `Reihenpfad` einbauen (bis hierher gibt es die Schnittstelle, keinen Nutzer), die Marke
  `yachse2` beim Ausblenden einer Reihe der zweiten Achse mitschalten und den
  Rundlauf-Datenzoom an diesen Stellen entfernen.
* **Die Zeigerzeile liest `Modell.Reihen`.** Sie muss dafür das **eigene Fenster** der Reihe
  lesen statt `Flaeche.Daten` — sonst liest sie beim Stundenprofil und bei nebeneinander
  gestellten Stapelgruppen den falschen Index, und bei einer Reihe der zweiten Achse den
  richtigen Wert mit der falschen Einheit.
* **Doppelte Leerzeichen im Text gehen im Browser verloren.** Der Bildtitel des Kostenprofils
  heißt „Kostenprofil  [ct/kWh]" mit zwei Leerzeichen; das PNG setzt beide, das SVG zeigt
  eines (XML-Leerraum). Wenn es auffallen soll, ist `xml:space="preserve"` am `<text>` die
  Stelle — sie gehört zum Baustein, nicht zum Schreiber.
* **Die Gruppen (d), (b) und (c)** stehen unten; der Oberflächen-Teil folgt. `ChartBild`
  bleibt, bis die letzte PNG-Stelle umgestellt ist.

---

## Gruppe (d) — Berichtsbilder und SVG im Wortbericht

### Die Aufgabe

Zwei Dinge in einem Auftrag. Erstens: Die vier **reinen Berichtsbilder** —
`JahresverlaufWaerme` (Tagesmittel, Stapelflächen, Bedarfslinie), `DauerlinieWaerme`,
`Speicherverlauf` (drei Wochenfelder) und `Speichertemperaturen` (drei Felder) — bekommen
ihr Zeichenmodell. Zweitens: Der **Wortbericht bettet Diagramme als SVG mit PNG-Rückfall**
ein, überall dort, wo die Renderer-Methode schon ein Modell hat.

Sie stehen zusammen, weil sie einander bedingen: Ohne Modell kein SVG, und ohne Ziel im
Bericht wäre das Modell dieser vier Bilder ohne Nutzer — sie kommen auf keinem
Ergebnisreiter vor.

**Die unverrückbare Bedingung blieb: kein PNG darf sich ändern.** Gemessen an der
Windows-Messlatte dieses Rechners (91 Hashes aus E0/E1), nach jedem Schritt.

### Die Entscheide

**DG-E3-7 — die vier sind reine PIXELBILDER.** Ihr Modell trägt `Flaeche = null` und
**keine** `Datenreihe`. Der `SvgSchreiber` schreibt damit jeden Befehl so, wie der
`SkiaMaler` ihn malt: die Reihen als Pixelpfad, jeden Text als `<text>`. Die Marken stehen
trotzdem — `titel`, `xachse`, `yachse`, `reihe:<Name>`, `legende:<Name>`.

*Warum.* Der Bericht ist **keine Bedienfläche**: Es gibt dort kein Zoomen, kein Abwählen
über die Legende, keine Zeigerzeile. Was das SVG im Bericht bringt, ist Schärfe (Vektor
statt 96-dpi-Raster) und Text, der Text bleibt — durchsuchbar, kopierbar, in der
Bildschirmlupe scharf. Eine Zeichenfläche in Datenkoordinaten brächte nichts davon und
kostete bei zweien der vier eine Unmöglichkeit: `Speicherverlauf` und
`Speichertemperaturen` zeichnen drei Wochenfelder nebeneinander, das wäre nicht **eine**
Zeichenfläche, sondern drei. Und der Weg über `Datenreihe` würde die Bilder sogar
verändern — er zeichnet roh bzw. als konservative Hülle, das PNG jeden n-ten Wert
(DG-E2-2); im Bericht sollen SVG und PNG-Rückfall dasselbe zeigen.

Die Marke `leerhinweis` kommt in diesen vier nicht vor: Sie geben `null` zurück, wenn der
Lauf die Reihen nicht führt, und der Bericht lässt die Stelle samt Beschriftung aus, statt
eine leere Fläche zu setzen. Kein Bild, kein Modell.

**DG-E3-8 — SVG im Wortbericht (revidiert DG-Q3; Anwenderentscheid 20.09.2026 „alle
Grafiken, soweit möglich").** `WordKontext` bekommt `Bild(Zeichenmodell, Breite, Höhe)`.
Es legt **zwei** Teile ab:

* das PNG aus `SkiaMaler.Png(modell)` als gewöhnlichen `a:blip` — den Rückfall;
* den SVG-Text aus `SvgSchreiber.Text(modell)` als zweiten `ImagePart` mit dem Inhaltstyp
  `image/svg+xml` (UTF-8 **ohne** Vorzeichenfolge), verknüpft über
  `DocumentFormat.OpenXml.Office2019.Drawing.SVG.SVGBlip` in der Erweiterungsliste des
  Blips, URI `{96DAC541-7B7A-43D3-8B79-37D633B846F1}`.

Maße und Lage sind dieselben wie beim reinen PNG. Word ab 2016 zeigt und druckt das SVG,
jeder ältere Leser und jeder Konverter, der die Erweiterung nicht kennt, das PNG. Der
`byte[]`-Weg bleibt unverändert daneben stehen; beide teilen sich einen Rumpf
(`BildTeile`), damit die Drawing-Struktur nur an einer Stelle steht.
* **Die Gruppen (b), (c) und (d)** des Rollouts stehen noch aus; `ChartBild` bleibt, bis die
  letzte PNG-Stelle umgestellt ist.

---

## Gruppe (b) — Kern: sechs Bilder ohne Zeitachse

### Die Aufgabe

Die sechs Bilder, die keine Jahresstunden zählen — `KapitalwertVerlauf`,
`Jahresprojektion`, `Kennlinien`, `Streuwolke`, `Schnittkurve`, `Stueckzahlkurve` —
bekommen ein **öffentliches Zeichenmodell** für die SVG-Ausgabe.

Sie sind nicht die Wiederholung der Gruppe (a), sondern ihr Gegenstück: Dort zählt x
immer die Zeit, hier zählt x ein Projektjahr, eine Außentemperatur, eine Kapazität,
eine Stückzahl oder eine Stützstelle. Drei der sechs zeichnen überhaupt keine
Ganglinie, sondern Säulen und Punktmarken. Daraus folgen die drei Entscheide dieser
Gruppe: Die Fläche muss sagen, **was** x zählt (DG-E3-4), eine Reihe muss ihre
**x-Stelle je Wert** mitbringen können (DG-E3-5), und ein Bild ohne sinnvollen Zoom
bekommt **gar keine Zeichenfläche** (DG-E3-7).

**Die unverrückbare Bedingung blieb: kein PNG darf sich ändern.** Gemessen an der
Windows-Messlatte dieses Rechners (91 Hashes aus E0/E1), nach jedem Commit.

### Was entstanden ist

| Datei | Was dazugekommen ist |
|---|---|
| `EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs` | `JahresverlaufWaermeModell`, `DauerlinieWaermeModell`, `SpeicherverlaufModell`, `SpeichertemperaturenModell`; `StapelDiagramm`/`LinienDiagramm` → `…Modell`; Marken in `AchsenRaster` und `PanelRahmen` |
| `EPOS.Kern/Allgemein/Bericht/WordBerichtGenerator.cs` | `Bild(Zeichenmodell, …)`, der gemeinsame Rumpf `BildTeile`, die Konstante `SVG_EXT_URI`; **Befund:** die Rahmenreihenfolge jeder Tabelle richtiggestellt |
| `Bausteine/BausteineVergleich.cs`, `BausteineProjekt.cs` | die vier Bildstellen auf den Modellweg; `Sicher` führt jetzt Modelle, `SicherPng` bleibt der Strombilanz |
| `Proben/ChartProben/Program.GruppeD.cs` (neu), `Program.cs`, `ChartProben.csproj`, `LIESMICH.md` | vier SVG-Gegenproben (`svgd_*`), eine Registrierungszeile, die Quelle im Projekt, der Abschnitt im LIESMICH |
| `EPOS.Kern.Tests/ChartRendererGruppeDTests.cs` (neu) | 28 Fälle: PNG aus demselben Modell, Determinismus, reines Pixel-SVG, Text bleibt Text, die Marken, „kein Bild, kein Modell" |
| `EPOS.Kern.Tests/WordBerichtSvgWacheTests.cs` (neu) | 8 Fälle: beide Teile je Bild, der `byte[]`-Weg unverändert, `null` schreibt nichts, `OpenXmlValidator` ohne Fehler, kein verwaister Teil und kein Verweis ins Leere |

`Zeichenmodell.cs` und `SvgSchreiber.cs` sind **nicht** angefasst — die Nachbargruppen (b)
und (c) erweitern sie.

### Drei Stellen, die Aufmerksamkeit brauchten

1. **Die Marken gehören an die gemeinsamen Helfer, nicht an die vier Methoden.**
   `AchsenRaster` setzt y-Raster, y-Beschriftung, x-Raster, x-Beschriftung und das
   Achsenkreuz in **einem** Rumpf; die Klammern liegen deshalb dort: der y-Teil unter
   `yachse`, der x-Teil unter `xachse`, das `Achsenkreuz` unter keiner von beiden (dieselbe
   Begründung wie in Gruppe (a): Wer eine Achsenteilung ausblendet, will die Achsenlinien
   behalten). Ein Nebennutzer bekommt die Marken mit: `MonatsBalken` (Strombilanz, Gruppe
   b/c). Das ist folgenlos — der `SkiaMaler` übergeht Marken, das PNG bleibt byte-gleich —
   und erspart der Nachbargruppe die halbe Arbeit.
2. **Der Rahmen eines Wochenfeldes ist sein Achsenkreuz.** `PanelRahmen` setzt das Rechteck
   ohne Marke und die Feldüberschrift („Winterwoche (Jan)") unter `xachse`: Sie sagt, welche
   Woche das Feld zeigt, und ist damit die Beschriftung seiner Zeitachse. Jede Reihe wird in
   diesen Bildern **dreimal** gezeichnet — einmal je Feld — und trägt jedes Mal dieselbe
   Marke `reihe:<Name>`; eine Legendenwahl träfe damit alle drei Felder zugleich.
3. **Eine Schleifenvariable in einer Marken-Klammer braucht eine eigene Kopie.** `Markiert`
   ruft die Klammer sofort auf, aber die Stapelschleife von `StapelDiagrammModell` schreibt
   `unten = oben` **nach** dem Zeichnen weiter. Ober- und Unterkante gehen deshalb als
   eigene Namen in die Klammer; sonst zeichnete jede Schicht die Kanten der nächsten.

### Der Befund nebenbei: der Wortbericht war nicht schemagültig

Die geforderte Validierung mit dem `OpenXmlValidator` meldete am ersten Lauf **acht
Fehler** — einen je Tabelle, alle gleich: „unexpected child element `w:left`" in
`w:tblBorders`. Ursache ist nicht das SVG, sondern die **Reihenfolge**: `CT_TblBorders`
führt `top, left, bottom, right, insideH, insideV`, `NeueTabelle` setzte links und rechts
aber hinter unten. Der Fehler stand in **jeder** Office-Fassung von 2007 bis 2021 an, also
seit die Methode existiert; Word ist nachsichtig und zeichnet den Rahmen trotzdem, ein
strengerer Leser hätte die Datei zurückgewiesen. Mit dem Tausch validiert der Bericht in
allen sechs Fassungen fehlerfrei. Am Bild ändert er nichts — es sind dieselben sechs Rahmen
in denselben Farben.

### Welche Bildstellen des Wortberichts jetzt SVG tragen

| Bildstelle | Baustein | Weg |
|---|---|---|
| Wärmeerzeugung im Jahresverlauf | Ergebnisse (je Variante) | **Modell → SVG + PNG** |
| Jahresdauerlinie Wärme | Ergebnisse (je Variante) | **Modell → SVG + PNG** |
| Speicherverlauf | Ergebnisse (je Variante) | **Modell → SVG + PNG** |
| Speichertemperaturen | Projektbeschreibung (Stamm) | **Modell → SVG + PNG** |
| Strombilanz im Monatsverlauf | Ergebnisse (je Variante) | offen — `byte[]`, kein Modell |
| Kapitalwertverlauf (zwei Bilder) | Wirtschaftlichkeit | offen — `byte[]`, kein Modell |
| Kuchen „Wärmedeckung"/„Stromdeckung" | Vergleich (je Variante) | offen — `byte[]`, kein Modell |
| Balken je Schlüsselkennzahl | Vergleich | offen — `byte[]`, kein Modell |

Die offenen Arten gehören zu den Gruppen (b) und (c); sie gehen mit deren Merge auf den
Modellweg, indem an der Aufrufstelle `…Modell(…)` statt `byte[]` geholt wird — an
`WordKontext.Bild` ist dafür nichts mehr zu tun.

**`Jahresgang` (E2) kommt im Wortbericht nicht vor**, ebenso wenig
`PeakShavingBild.Lastgang` und `SpeicherBetriebsbild.Zeichnen`: Beide liefern `byte[]` für
die **Oberfläche** (Reiter und Dialoge), nicht für den Bericht. Sie bleiben, wo sie sind;
für sie ist an dieser Stelle nichts offen.

### Wie das PDF entsteht

**Gar nicht — das Programm hat keinen PDF-Weg.** `BerichtCtrl` kennt genau zwei Ausgaben:
`ErzeugeWord` (`.docx`, OpenXML-SDK) und `ErzeugeExcel` (`.xlsx`, ClosedXML). Im ganzen
Repositorium steht keine PDF-Bibliothek, kein Word-Interop, kein `ExportAsFixedFormat`;
„PDF" kommt nur als Dateifilter der Lizenzvereinbarung vor. Ein PDF entsteht also **aus dem
Wortbericht heraus**, im Word des Anwenders („Speichern unter" oder „Drucken → Microsoft
Print to PDF"), und erbt dabei genau das Bild, das Word anzeigt: das SVG in Word 2016 und
neuer, das PNG in jeder älteren Fassung. Die Vorgabe „der PDF-Weg bleibt PNG" ist damit von
selbst erfüllt und zugleich besser, als sie klingt.

**Der Excelbericht bleibt PNG** — genauer: **ohne Bild**. `ExcelBerichtGenerator` bettet
überhaupt keine Grafik ein; er führt die Zahlen, aus denen die Bilder entstehen. Es gibt
dort also nichts umzustellen.
| `EPOS.Kern/Allgemein/Bericht/Zeichnung/Zeichenmodell.cs` | `Achsenart`; `Zeichenflaeche` um `X` und `XEinheit` erweitert; `Reihenart.Punkte`; `Datenreihe` um `XWerte` und `Einheit` erweitert (`Gleicht` nachgezogen) |
| `EPOS.Kern/Allgemein/Bericht/Zeichnung/SvgSchreiber.cs` | die Punktwolke (`Punktwolke`, `stroke-linecap="round"`), die x-Stelle je Wert (`XStelle`) in Linie, Fläche und Fensterschnitt |
| `EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs` | sechs `…Modell`-Methoden; `Achsenteilung`; `Jahresreihe` als Rumpf der Projektionsreihen; die sieben Flächen der Gruppe (a) mit ihrer Achsenart nachgezogen |
| `Proben/ChartProben/Program.GruppeB.cs` (neu), `Program.cs`, `ChartProben.csproj` | acht Gegenproben und die sechs SVG-Dateien für `--svg-alle`; eine Registrierungszeile und eine `Compile`-Zeile |
| `EPOS.Kern.Tests/` | `ChartRendererGruppeBTests` (neu, 15), `SvgSchreiberTests` +3, `ZeichenmodellTests` +2 |

### Die Entscheide

**DG-E3-4 — Die Fläche nennt, was x ZÄHLT.** `Zeichenflaeche` bekommt
`Achsenart X` (`Stunden` | `Index` | `Wert`) und `string XEinheit` („°C", „kWh", „a");
`Datenreihe` bekommt `string Einheit` für die Zeigerzeile („kW", „€"). Die neue
Funktion `ChartRenderer.Achsenteilung(Zeichenflaeche f, double von, double bis)`
liefert daraus die Marken eines Ausschnitts: für `Stunden` über
`Jahresstundenteilung`, für `Wert` mit denselben „schönen" Stufen wie das Bild
(`Skala.Rund`), für `Index` ganzzahlig.

*Warum.* Mit E2/E3-1 weiß die Oberfläche, WO eine Stunde steht, aber nicht, ob x
überhaupt eine Stunde ist. Beim Zoom blendet sie die Marke `xachse` aus und zeichnet
die Teilung nach (DG-E2-3) — bisher konnte sie das nur über
`Jahresstundenteilung`, und die ergibt auf einer Temperaturachse Unsinn. Die
Achsenart steht an der FLÄCHE und nicht am Bild, weil sie zum Datenfenster gehört;
die Einheit der WERTE steht an der REIHE, weil eine Reihe der zweiten Achse eine
andere führt als die der linken.

*Die Vorgabe ist `Stunden` ohne Einheit* — damit bleibt jede Fläche der früheren
Etappen wörtlich, was sie war. Nachgezogen sind die sieben Flächen der Gruppe (a):
`Kostenprofil` und `Stundenprofil` zählen einen **Index** (sie legen ihre Reihe über
deren eigene Länge auf eine feste Achse), die übrigen fünf **Stunden**. Kein Bild hat
sich dabei um ein Byte geändert — der Maler übergeht die Zeichenfläche ganz.

**DG-E3-5 — Punktwolke und x-Stelle je Wert.** `Reihenart.Punkte` mit
`double[] XWerte`: Die Streuwolke führt x = Außentemperatur je Stunde, y = Leistung.
Im SVG wird daraus **ein** `<path>` je Reihe aus Segmenten `M x,y h 0` — eine Strecke
der Länge null, die erst `stroke-linecap="round"` zum Punkt macht —, mit
`stroke-width` = Punktdurchmesser des PNG (Radius 2,5 → 5) und
`vector-effect="non-scaling-stroke"`. **Gebündelt wird nicht:** Eine Bündelung je
Bildpunktspalte nähme genau die Verdichtung weg, die die Aussage der Wolke ist, und
8 760 Punkte sind so oder so EIN Knoten.

*Über den Auftrag hinaus:* `XWerte` gilt nicht nur für `Punkte`. Die **Schnittkurve**
ist eine LINIE mit ungleichmäßigen Stützstellen — die zweite Suchphase schiebt
Feinpunkte zwischen die Grobpunkte (Auftrag #224). Ohne eigene x-Stelle säße im SVG
jeder Feinpunkt an der falschen Kapazität. `XStelle` im Schreiber liefert deshalb für
jede Reihenart entweder den eigenen Wert oder, wie bisher, die gleichmäßige Teilung
des Fensters; auch der Fensterschnitt sucht dann nach der x-STELLE statt nach dem
Index. Für jede Reihe der Gruppe (a) (`XWerte == null`) ändert sich dabei kein
Zeichen — der Nachweis ist der unveränderte Fall
`DerFensterpfadIstDerAusschnittDesVollpfads`.

**DG-E3-7 — Reine Pixelbilder.** `Kennlinien`, `Jahresprojektion` und
`Stueckzahlkurve` haben `Flaeche = null`: kein inneres `<svg>`, nur Marken. Ihre
x-Achse zählt Stützstellen, Projektjahre und ganze Geräte — dazwischen liegt nichts,
worauf ein Zoom zeigen könnte. Der Grund ist aber nicht nur fachlich: Der Schreiber
ersetzt den ERSTEN Befehl mit einer `reihe:…`-Marke durch das innere `<svg>` und
übergeht jeden weiteren (DG-E2-2). Ein inneres svg nähme diesen drei Bildern also
ihre Säulen, ihre Schraffur und ihre Punktmarken und ersetzte sie durch Pfade.

`KapitalwertVerlauf` (x = Projektjahr), `Streuwolke` (x = Außentemperatur) und
`Schnittkurve` (x = Größenachse der Rastersuche) bekommen das innere svg samt
Datenreihen.

**Reihen als `Datenreihe` nur dort, wo eine Zeigerzeile Sinn ergibt.**
`Jahresprojektion` führt je Jahr Netto-Cashflow, kumulierten Stand und jede weitere
gewählte Linie; `Kennlinien` je Vorlaufstufe ihre Stützstellen. Beide tragen das
Projektjahr bzw. die Außentemperatur als `XWerte` — ohne sie läse die Zeigerzeile den
Index statt des Jahres. Die `Stueckzahlkurve` führt **keine** Reihe: An einer Säule
sagt eine Zeigerzeile nicht mehr, als die Säule zeigt.

### Was x je Bild bedeutet

| Bild | x | Achsenart | `XEinheit` | Fläche |
|---|---|---|---|---|
| `KapitalwertVerlauf` | **Projektjahr** 0 … N | `Wert` | `a` | ja |
| `Streuwolke` | **Außentemperatur** der Stunde | `Wert` | `°C` | ja |
| `Schnittkurve` | die **Größenachse der Rastersuche** (Kapazität, je Modus auch Leistung oder C-Rate) | `Wert` | — | ja |
| `Kennlinien` | **Außentemperatur** der Stützstelle | — | — | nein |
| `Jahresprojektion` | **Projektjahr** 1 … n | — | — | nein |
| `Stueckzahlkurve` | **Stückzahl** (ganze Geräte) | — | — | nein |

Die y-Seite: Jede Reihe nennt ihre Einheit. `KapitalwertVerlauf` und
`Jahresprojektion` führen „€" fest — dort steht die Einheit im Bild selbst („… [€]",
Hausregel „alles in EINER Einheit, deshalb EINE Achse"). `Streuwolke`, `Schnittkurve`
und `Kennlinien` nehmen den **Titel ihrer Wertachse**, den der Aufrufer setzt
(„Leistung [kW]", „ΔJ [€/a]", „COP"): Bei diesen Bildern wechselt die Größe je
Aufruf, und was der Aufrufer an die Achse schreibt, ist genau das, was die
Zeigerzeile zeigen soll.

Die Marken je Bild: `titel`, `xachse`, `yachse`, `reihe:<Name>` (auch an den Säulen,
der Schraffur und den Punktmarken), `legende:<Name>` (setzt `Legende` seit E2 selbst),
`nulllinie`, `leerhinweis` und `marke` — letztere für den roten Optimum-Kreis der
Schnittkurve, das schwarze Quadrat der Stückzahlkurve und das Ersatzjahr-Band der
Jahresprojektion. Das **Achsenkreuz bleibt markenlos**, wie seit E2.

### Vier Stellen, an denen die Umsetzung über den Auftrag hinausgeht

1. **`XWerte` trägt auch eine LINIE**, nicht nur eine Punktwolke (siehe DG-E3-5). Die
   Alternative wäre gewesen, die Schnittkurve im SVG gleichmäßig zu verteilen — sie
   läge dann sichtbar neben dem PNG, sobald ein Feinraster im Spiel ist.
2. **Die `Schnittkurve` nennt KEINE x-Einheit.** Ihre x-Größe wechselt mit dem
   Suchmodus (`Flottenachsengroesse`: Kapazität in kWh, Leistung in kW, C-Rate), und
   sie steht im Achsentitel, den der Aufrufer setzt (`Achsentext`). Eine feste
   Einheit im Modell wäre für zwei von drei Suchmodi falsch; ein neuer Parameter
   gehört zur Oberflächen-Etappe, wenn die Zeigerzeile ihn braucht.
3. **Die Stützpunkt-Kreise der Schnittkurve fallen im SVG weg.** Sie tragen die Marke
   ihrer Reihe, damit die Legendenwahl Kurve und Punkte zusammen schaltet — und
   werden damit vom inneren svg verschluckt (DG-E2-2). Die Kurve selbst steht dort als
   Pfad; die Punkte kann die Oberfläche als zweite Reihe mit `Reihenart.Punkte`
   nachziehen, wenn sie sie sehen will. Die drei Bilder ohne Fläche haben das Problem
   nicht — bei ihnen steht jeder Reihenbefehl als Pixel-Element im Baum.
4. **`ChartProben` bekommt eine zweite Art von Gegenprobe.** Die `SvgModellprobe` der
   Gruppe (a) verlangt eine Zeichenfläche und mindestens eine Datenreihe; für ein
   reines Pixelbild ist genau das der Fehlerfall. `SvgPixelModellprobe` prüft deshalb
   das Gegenteil: kein inneres svg, die Reihenbefehle als gewöhnliche Elemente mit
   ihrer Marke, Titel und beide Achsen markiert, und jede Datenreihe mit ihren
   x-Stellen. Die Liste `GRUPPE_B_MIT_FLAECHE` **ist** die Prüfung zu DG-E3-7.

### Was an der Byte-Gleichheit schwierig war

Wenig — die Lehren der Gruppe (a) trugen: Die Klammern der Marken liegen GENAU um die
vorhandenen Blöcke, die Zeichenfläche steht VOR dem ersten Reihenbefehl, und ein
`using (var f = Schrift(15f))` mit zwei Texten für zwei Achsen bekommt zwei Klammern
in derselben Reihenfolge (`Kennlinien`, `Schnittkurve`, `Stueckzahlkurve`,
`Jahresprojektion` und `Streuwolke` setzen alle erst den x-, dann den y-Titel).

Zwei Stellen waren eigen:

* **`Streuwolke` und `Stueckzahlkurve` klammern MITTEN in einen Rumpf.** Die Streuwolke
  ruft `YRaster`, das Raster und Achsenkreuz zusammen absetzt; markiert werden darf nur
  das Raster. Sie ruft deshalb jetzt `YRasterOhneKreuz` und `Achsenkreuz` einzeln —
  dieselbe Aufteilung, die die Gruppe (a) für `BedarfsRaster` und `ProzentRaster`
  gemacht hat, und dieselbe Befehlsreihenfolge.
* **Der Wächter `ZeichenmodellWacheTests` zählt mit.** Die sechs Methoden hatten je
  ein bis zwei frühe `return SkiaMaler.Png(z)` in ihren Leerfällen; nach dem Umbau hat
  jede genau eine Übergabe. Die Zahl fiel von 36 auf 29 — die Forderung „mindestens
  so viele Übergaben wie die 26 Bildmethoden" ist weiter erfüllt, und die 26 bleiben
  26.

### Die Schnittstelle, die der UI-Teil benutzt

| Was | Signatur | Zeile |
|---|---|---|
| Achsenart | `enum Achsenart { Stunden = 0, Index = 1, Wert = 2 }` | `Zeichenmodell.cs`:162 |
| Zeichenfläche | `sealed record Zeichenflaeche(Rahmen Bild, Datenfenster Daten, Achsenart X = Achsenart.Stunden, string XEinheit = null)` | `Zeichenmodell.cs`:207 |
| Reihenart | `enum Reihenart { Linie = 0, Flaeche = 1, Punkte = 2 }` | `Zeichenmodell.cs`:214 |
| Reihe | `sealed record Datenreihe(string Name, Farbton Ton, float Staerke, Strichmuster Muster, double[] Werte, Datenfenster Fenster = null, Reihenart Art = Reihenart.Linie, double[] Unten = null, Farbton Randton = null, double[] XWerte = null, string Einheit = null)` | `Zeichenmodell.cs`:297 |
| Teilung der x-Achse | `IReadOnlyList<(double Wert, string Text)> ChartRenderer.Achsenteilung(Zeichenflaeche flaeche, double von, double bis)` | `ChartRenderer.cs`:4900 |
| Kapitalwert-Verlauf | `Zeichenmodell ChartRenderer.KapitalwertVerlaufModell(string titel, List<Reihe> reihen, string fussnote)` | `ChartRenderer.cs`:603 |
| Kennlinien | `Zeichenmodell ChartRenderer.KennlinienModell(string titel, string yTitel, string xTitel, IReadOnlyList<KennlinienReihe> reihen, Kennlinienmarke marke)` | `ChartRenderer.cs`:1226 |
| Streuwolke | `Zeichenmodell ChartRenderer.StreuwolkeModell(string titel, string xTitel, string yTitel, IReadOnlyList<Punktreihe> reihen)` | `ChartRenderer.cs`:2430 |
| Schnittkurve | `Zeichenmodell ChartRenderer.SchnittkurveModell(string titel, string xTitel, string yTitel, IReadOnlyList<double> kapazitaetenKwh, IReadOnlyList<double> werte, double optimumKwh, double optimumEur, IReadOnlyList<bool> feinpunkte = null)` | `ChartRenderer.cs`:3548 |
| Stückzahlkurve | `Zeichenmodell ChartRenderer.StueckzahlkurveModell(string titel, string xTitel, string yTitel, IReadOnlyList<int> stueckzahlen, IReadOnlyList<double> werte, int besteStelle, IReadOnlyList<bool> unzulaessig = null)` | `ChartRenderer.cs`:3741 |
| Jahresprojektion | `Zeichenmodell ChartRenderer.JahresprojektionModell(string titel, IReadOnlyList<int> jahre, Reihe netto, Reihe kumuliert, IReadOnlyList<int> ersatzjahre, IReadOnlyList<Reihe> weitere = null, string yTitel = null, string xTitel = null)` | `ChartRenderer.cs`:3946 |

Der Pfad einer Reihe bleibt, was E3 aus ihm gemacht hat
(`SvgSchreiber.Reihenpfad`:493 und :514) — er kennt jetzt zusätzlich die Punktwolke
und die eigene x-Stelle. Die Griffe im Markup sind unverändert
(`class="epos-flaeche"`, `class="epos-reihe"`, `data-reihe`, `data-marke`); ein
Punktpfad trägt zusätzlich `stroke-linecap="round"`.

### Nachweis

| Prüfung | Ergebnis |
|---|---|
| Windows-Messlatte des Rechners (91 Hashes) | 91 von 91 gleich, Text-Diff leer — vor dem ersten Schritt und nach jedem Commit |
| `Proben/ChartProben` | **88 Bilder geprüft, 0 Verstöße** (84 wie bisher, dazu vier SVG-Gegenproben); 91 Hashes geschrieben, die Messlatte bleibt bei 91 Zeilen |
| `WP-Plan.Kern.slnf` Bau Release | 0 Fehler |
| Volle Suite mit den CI-Schaltern | **9 946 Fälle, 0 Fehler** (Kern 4 124, UI 4 918, KiKern 499, SpeicherEngine 378, SpeicherPlanung 27) |
| `ChartRendererGruppeDTests` | 28 grün |
| `WordBerichtSvgWacheTests` | 8 grün |
| `BerichtBlattstrukturWacheTests`, `ZeichenmodellWacheTests`, `ChartRendererTests` | grün, unverändert |
| Wortbericht aus Referenzprojekt **1030** der Testdatenbank | erzeugt über einen `dotnet`-Dateiskriptlauf (`SimulationRunner.Simuliere(1030)` → `ZeitreihenExtraktor.AusLauf` → `WordBerichtGenerator.Erzeuge`, alle Bausteine an): 227 092 Byte, 17 Teile — **7 PNG und 4 SVG**; die vier SVG sind genau die vier Bilder der Gruppe (d), die drei übrigen PNG sind Strombilanz und die zwei Kuchen |
| `OpenXmlValidator` über diesen Bericht | **0 Fehler in allen sechs Fassungen** (Office2007 … Office2021) — vor der Rahmenkorrektur acht |
| Sichtprüfung `--svg-alle` gegen `--ablage` (Edge kopflos) | alle vier deckungsgleich: gleiche Struktur, Farben, Achsen, Felder und Legenden. Anders als in Gruppe (a) gibt es hier **keinen** gewollten Unterschied im Linienzug — die Pixelbilder zeichnen dieselben Befehle, nur als Vektor |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

### Offen nach Gruppe (d)

* **Die Bildarten ohne Modell** (Strombilanz, Kapitalwertverlauf, Kuchen, Balken) waren
  offen, bis die Gruppen (b) und (c) ihre `…Modell`-Methoden mitbrachten. **Erledigt
  (DG-E3d2):** Die sechs Stellen — Strombilanz (`BausteineVergleich`, Ganglinien), Balken je
  Schlüsselkennzahl und die zwei Kuchen „Wärmedeckung"/„Stromdeckung" (derselbe Baustein),
  die zwei Kapitalwertverläufe (`BausteineWirtschaftlichkeit`) — holen jetzt `…Modell(…)`
  und gehen über `WordKontext.Bild(Zeichenmodell, …)`. Die Tabelle „Welche Bildstellen des
  Wortberichts jetzt SVG tragen" weiter oben ist damit überholt: **Es gibt im Wortbericht
  keine Bildstelle mehr ohne SVG.** Der Bericht aus Projekt 1030 legt zu allen sieben Bildern
  beide Teile ab (20 Teile statt 17 — 7 PNG, 7 SVG, 6 Strukturteile), Validator 0 Fehler in
  allen sechs Office-Fassungen. Die Fehlerfang-Klammern sind mitgezogen: `SicherPng` und
  `SicherB` weichen dem `Sicher`, das ein Zeichenmodell führt, und der
  Wirtschaftlichkeits-Baustein bekommt dieselbe Klammer, die er bisher nicht hatte — ein
  Diagrammfehler lässt die Bildstelle aus, statt den Bericht zu reißen.
* **Was PNG bleibt — und warum.** Im Wortbericht nichts: Jedes Diagramm trägt SVG **mit
  PNG-Rückfall**, das PNG steht also weiterhin in jedem `a:blip` und ist das, was jeder Leser
  vor Word 2016 und jeder Konverter zeigt. Rein PNG bleiben drei Dinge, alle außerhalb des
  Wortberichts: die **Oberfläche** (`PeakShavingBild.Lastgang`, `SpeicherBetriebsbild.Zeichnen`
  — sie liefern `byte[]` für Reiter und Dialoge und haben kein Zeichenmodell), der
  **Excelbericht** (`ExcelBerichtGenerator` bettet überhaupt kein Bild ein, er führt die
  Zahlen) und die **ChartProben**, die weiterhin über die `byte[]`-Renderer messen — sie sind
  die Messlatte der Byte-Gleichheit. Der Weg `WordKontext.Bild(byte[] …)` hat damit **keinen
  Leser mehr**; er bleibt trotzdem stehen, weil ein Fremdbild oder eines der beiden
  Oberflächenbilder ihn braucht, sobald es in den Bericht soll.
* **Doppelte Leerzeichen im Titel gehen im Browser verloren** — derselbe offene Punkt wie in
  Gruppe (a), hier sichtbar an „Wärmeerzeugung im Jahresverlauf (Tagesmittel)  [kW]" und
  „Jahresdauerlinie Wärme  [kW]". Im PNG stehen zwei Leerzeichen, im SVG eines. Die Stelle
  ist `xml:space="preserve"` am `<text>`; sie gehört zum Schreiber bzw. Baustein, nicht zu
  diesen Bildern.
* **Ungeprüft bleibt, wie Word das SVG tatsächlich zeigt** — auf diesem Rechner ist kein
  Word installiert. Nachgewiesen sind die Struktur (Validator, beide Teile, Beziehungen) und
  die Darstellung des SVG im Browser; der Blick in Word steht beim Anwender aus.
* **Die Bildunterschriften bleiben, wie sie sind.** Ein SVG könnte einen Alternativtext
  tragen (`wp:docPr/@descr`) — für Barrierefreiheit wäre das der nächste Schritt, er gehört
  aber nicht in diese Etappe.
| Windows-Messlatte des Rechners (91 Hashes) | 91 von 91 gleich, Text-Diff leer — vor dem Beginn, nach K2 und in der Abnahme |
| `Proben/ChartProben` | **92 Bilder geprüft, 0 Verstöße** (84 wie bisher, dazu acht Gegenproben der Gruppe (b)); 91 Hashes geschrieben |
| `WP-Plan.Kern.slnf` Bau Release | 0 Fehler; die vier Warnungen des Kerns sind Bestand (`KlimaregionStammCtrl`, `StromverbraucherStammCtrl`, `WErzeugerModel`) |
| volle Suite mit den CI-Schaltern | grün |
| `ChartRendererGruppeBTests` (neu) | 15 |
| `SvgSchreiberTests` / `ZeichenmodellTests` | 32 (29 + 3) / 22 (20 + 2) |
| `ChartRendererTests`, `PfadregelTests` | 30 / 10 — **kein Fall geändert** |
| `ZeichenmodellWacheTests` | grün: 26 `public static byte[]`, 29 Übergaben an `SkiaMaler.Png` |
| Sichtprüfung `--svg-alle` gegen `--ablage` (Edge kopflos) | `streuwolke_drei_reihen`, `kapitalwert_absolut`, `jahresprojektion`, `kennlinien_cop`: gleiche Struktur, gleiche Farben, gleiche Achsen, Legenden, Säulen, Ersatzjahr-Band und Punktmarken. Die Punkte der Streuwolke sind bei sechsfacher Vergrößerung runde Scheiben wie im PNG — die runde Strichkappe trägt |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

### Offen nach dem Kernteil der Gruppe (b)

* **Der UI-Teil:** die sechs Bilder auf `DiagrammSvg` umstellen, die Zeigerzeile aus
  `Modell.Reihen` samt `Einheit` und `XWerte` speisen und die Achsenteilung eines
  Ausschnitts über `ChartRenderer.Achsenteilung` statt über `Jahresstundenteilung`
  holen. Bis hierher gibt es die Schnittstelle, keinen Nutzer.
* **Die Stützpunkte der Schnittkurve im SVG** (Abweichung 3) — eine zweite Reihe mit
  `Reihenart.Punkte`, sobald die Oberfläche sie zeigen soll.
* **Die x-Einheit der Schnittkurve** (Abweichung 2): Sie kommt als Parameter, wenn die
  Zeigerzeile sie braucht; der Aufrufer kennt sie (`Flottenachsengroesse`).
* **`Zeichenbefehl.Wert` / `data-wert`** ist Gruppe (c); dieser Auftrag hat den
  Basis-Record und die generische Attributausgabe des Schreibers bewusst nicht
  angefasst, damit die Zweige zusammengehen.
* **Die Gruppen (c) und (d)** des Rollouts stehen noch aus.

---

## Gruppe (c) — Kern: sieben Bilder ohne Zeitachse

### Die Aufgabe

Die sieben Diagrammarten **ohne** Zeitachsen-Zoom — `Kuchen`, `BalkenHorizontal`,
`StrombilanzMonate`, `MonatsSaeulen`, `MonatsStapel`, `Ring` (zwei Überladungen) und
`Optimierungsraster` — bekommen ihr öffentliches Zeichenmodell für die SVG-Ausgabe. Sie
haben keine Zeitachse: zwölf starre Monatsfächer, eine Handvoll Varianten, ein Kuchen, ein
Ring und ein Raster aus Kapazität × C-Rate. Es gibt hier also nichts zu zoomen — und damit
verschiebt sich die Frage: Nicht das innere `<svg>` macht diese Bilder interaktiv, sondern
**der Wert am Element**.

**Die unverrückbare Bedingung blieb: kein PNG darf sich ändern.** Gemessen an der
Windows-Messlatte dieses Rechners (91 Hashes aus E0/E1), nach jedem Commit.

### Was entstanden ist

| Datei | Was dazugekommen ist |
|---|---|
| `EPOS.Kern/Allgemein/Bericht/Zeichnung/Zeichenmodell.cs` | `Zeichenbefehl.Wert` (Z. 113) und die Klammer `Markiert(marke, wert, inhalt)` (Z. 589) |
| `EPOS.Kern/Allgemein/Bericht/Zeichnung/SvgSchreiber.cs` | `data-wert` an derselben Stelle wie `data-marke` (Z. 414) |
| `EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs` | sieben `…Modell`-Methoden, `MonatsBalkenModell` als Rumpf der Strombilanz, die Wertformatierung (`WERT_TRENNER`, `Elementwert`, `Anteilwert`, `Achsenwert`, `Zellwert`, `Achseneinheit`), `AchsenRasterOhneKreuz` |
| `Proben/ChartProben/Program.GruppeC.cs` (neu), `Program.cs`, `ChartProben.csproj`, `LIESMICH.md` | zehn SVG-Gegenproben in einer partiellen Klasse, eine Registrierungszeile, die Compile-Zeile, das Papier nachgezogen |
| `EPOS.Kern.Tests/ChartRendererGruppeCTests.cs` (neu) | zwölf Fälle |
| `EPOS.Kern.Tests/` | `ZeichenmodellTests` +2 (20 → 22), `SvgSchreiberTests` +2 (29 → 31) |

### Die Entscheide

**DG-E3-6 — Der Wert am Element.** Der Basis-Record `Zeichenbefehl` bekommt neben `Marke`
ein optionales `string Wert { get; init; }` — den **fertig formatierten Text**, den die
Oberfläche beim Zeigen auf das Element anzeigt („Jan · Wärmepumpe: 12,5 MWh",
„Kapazität 220 kWh · Entladeleistung 100 kW: 4.000 €", „Wärmepumpe: 48,0 %"). Der
`SvgSchreiber` schreibt ihn als `data-wert` an derselben Stelle wie `data-marke`; der
`SkiaMaler` übergeht ihn wie die Marke, und das PNG bleibt byte-gleich.

*Warum fertiger Text und keine Zahl.* Formatiert wird dort, wo auch die Beschriftung des
Bildes entsteht — in der `DE`-Kultur des Renderers und mit denselben Nachkommastellen.
Eine nackte Zahl im Modell müsste die Oberfläche ein zweites Mal formatieren; Bild und
Zeigetext gingen dann auseinander, und die Einheit wüsste die Oberfläche ohnehin nicht.

**Die Nachkommastellen sind die der EIGENEN Achse des Bildes.** Das ist die Lesart von
„Formatierung wie die Beschriftung des PNG", und sie ist je Bild eine andere:

| Bild | Format der Zahl | woher |
|---|---|---|
| `MonatsSaeulen` | `N0` / `N1` / `N2` | `Skala.Bedarf` — dieselbe Wahl, die die y-Beschriftung trifft |
| `MonatsStapel` | `N0` ab 10, sonst `N1` | die Regel des `YRaster` |
| `MonatsBalken` (Strombilanz) | `N0` | die Regel des `AchsenRaster` |
| `BalkenHorizontal` | `N0` | das Bild schreibt die Zahl selbst hinter den Balken |
| `Kuchen`, `Ring` | `N1`, Anteil in Prozent | die Prozentzahl der Legende bzw. die Mittelzahl |
| `Optimierungsraster` | `0.#` senkrecht, `0.##` waagerecht, `N0` für den Zellwert | die beiden Achsenbeschriftungen und die Farbskala |

**DG-E3-7 — Reine Pixelbilder.** Alle sieben haben `Flaeche = null` (kein inneres `<svg>`,
kein Zoom) und **keine `Datenreihe`** — die Werte stehen am Element. Säulen,
Stapelschichten, Balkenzeilen, Rasterzellen, Ring- und Kuchensegmente sind Pixel-Elemente
mit der Marke `reihe:<Name>` (dem Legendenschalter; beim Kuchen `reihe:<Segmentname>`) und
ihrem `Wert`. Die Legende markiert der Helfer `Legende` bereits (`legende:<Name>`); dazu
kommen `titel`, `xachse`/`yachse`, die Farbskala des Rasters als `skala`, die Bestmarke als
`marke` und der Leerhinweis als `leerhinweis`.

**Achsenkreuz und Netzlinien bleiben markenlos.** Dieselbe Regel wie in E2 und in
Gruppe (a): Die beiden Achsenlinien — und beim Raster die Netzlinien zwischen den Zellen —
müssen stehen bleiben, wenn die Oberfläche eine Achsenteilung ausblendet oder eine Reihe
abwählt. Dafür ist `AchsenRaster` wie schon seine drei Nachbarn in `…OhneKreuz` plus
`Achsenkreuz` aufgeteilt; die Befehlsreihenfolge ändert sich dabei nicht.

### Fünf Stellen, an denen die Umsetzung eine eigene Entscheidung gebraucht hat

1. **Der Kuchen markiert seine Legende selbst.** Der Entscheid sagt, die Legende markiere
   der Helfer `Legende`. Der Kuchen baut seine aber selbst, weil der Eintrag die
   Prozentzahl trägt („Solarthermie   12,0 %"). Ohne Marke wäre er der einzige der sieben,
   dessen Legende nichts schaltet — deshalb steht `legende:<Segmentname>` dort von Hand,
   mit demselben Schlüssel wie am Segment. Die Befehle bleiben unverändert.
2. **Der Bedarfszug des Monatsbalkens trägt nur seinen Namen als Wert.** Er ist im PNG EIN
   Streckenzug über zwölf Monate und zeigt keine einzelne Zahl; ihn in zwölf Elemente zu
   zerlegen hieße, das Bild zu ändern. Die Regel daraus: Ein Element, das genau eine Zahl
   zeigt, nennt sie; eines, das eine ganze Reihe zeigt, nennt seinen Namen. So trägt jedes
   `reihe:`-Element einen `data-wert`, und die Gegenprobe kann das ohne Ausnahmeliste
   prüfen.
3. **Die Rasterzelle heißt `reihe:<Skalentitel>`.** Ein Raster hat keine Legende und keine
   Reihen — es zeigt EINE Größe, und die benennt die Farbskala rechts („ΔJ [€/a]"). Sie
   trägt deshalb die Marke `skala` und ist damit die Legende dieses Bildes; alle 60 Zellen
   tragen denselben Reihenschlüssel, und ihren Ort nennt jede in ihrem Wert.
4. **`Achsenwert` zerlegt die Achsenbeschriftung.** „Kapazität [kWh]" wird zu „Kapazität
   220 kWh" — die Einheit wandert hinter die Zahl, wo sie hingehört. Was hinter der Klammer
   noch folgt, fällt weg: Die C-Raten-Achse trägt die Erläuterung „(Leistung = Kapazität ×
   C-Rate)", und die ist ein Satz über die ACHSE, nicht über die Zelle.
5. **Loch und Sperre stehen im Wert.** Ein nicht gerechneter Kandidat (#226) nennt statt
   der Zahl „nicht gerechnet", eine gesperrte Zelle hängt „(unzulässig)" an (#193). Beides
   sagt das Bild schon — das eine durch die Lochfarbe, das andere durch die Schraffur —,
   und der Zeigetext sagt es in Worten. Die Schraffur gehört dabei zur Zelle: Sie steht in
   derselben Klammer und trägt denselben Wert.

### Was an der Byte-Gleichheit zu beachten war

1. **Eine Klammer um einen Schleifenrumpf braucht feste Kopien.** `Markiert` nimmt einen
   Lambda-Ausdruck; eine Laufvariable, die der Rumpf danach weiterzählt (`unten` im
   Stapel, `start` im Kuchen und im Ring), muss vorher in eine eigene Größe kopiert
   werden. Sonst zeichnete der Stapel alle Schichten auf dieselbe Höhe — ein Fehler, den
   der Bildvergleich sofort meldet, und genau deshalb steht er hier.
2. **Die Marke gehört um den BLOCK, nicht um den einzelnen Befehl.** Eine Balkenzeile sind
   vier Befehle (Beschriftung, Balken, Rahmen, Zahl), eine gesperrte Rasterzelle zwei
   (Fläche und Schraffurgruppe). Sie stehen zusammen in einer Klammer und tragen denselben
   Wert; die Reihenfolge bleibt wörtlich die des Bestands.
3. **`Markiert` mit Wert überschreibt nichts.** Marke und Wert werden je für sich nur
   gesetzt, wo noch keiner steht — so kann eine äußere Klammer keine feinere Angabe
   verdrängen.
4. **Die `byte[]`-Methoden bleiben 26.** Der Wächter `ZeichenmodellWacheTests` zählt sie;
   jede geht jetzt über `SkiaMaler.Png(…Modell(…))`. `StrombilanzMonate` behält dabei ihr
   `null`: `StrombilanzMonateModell` liefert in denselben Fällen kein Modell, und die
   Bildmethode gibt `null` weiter, statt den Maler mit `null` zu rufen.

**Kein Bild ist gewandert.** Der Messlatte-Diff war nach jedem der vier Commits leer.

### Die Schnittstelle, die der UI-Teil benutzt

| Was | Signatur (`ChartRenderer.cs`, Zeile) |
|---|---|
| Wert am Befehl | `string Zeichenbefehl.Wert { get; init; }` — `Zeichenmodell.cs` Z. 113 |
| Klammer mit Wert | `void Zeichenhilfe.Markiert(this IZeichenziel z, string marke, string wert, Action<IZeichenziel> inhalt)` — `Zeichenmodell.cs` Z. 589 |
| Kuchen | `Zeichenmodell KuchenModell(string titel, List<Segment> segmente)` — Z. 151 |
| Balken | `Zeichenmodell BalkenHorizontalModell(string titel, string einheit, List<Balken> balken)` — Z. 217 |
| Strombilanz | `Zeichenmodell StrombilanzMonateModell(ZeitreihenSatz z)` — Z. 309 (`null` ohne Daten) |
| Monatssäulen | `Zeichenmodell MonatsSaeulenModell(string titel, double[] werte, SKColor farbe, string einheit, IReadOnlyList<string> monatsnamen = null)` — Z. 1490 |
| Ring | `Zeichenmodell RingModell(string titel, IReadOnlyList<Ringsegment> segmente, double mitteWert, string mitteEinheit)` — Z. 2540 |
| Ring (lang) | `Zeichenmodell RingModell(…, string mitteUnterzeile, bool mitLegende)` — Z. 2587 |
| Monatsstapel | `Zeichenmodell MonatsStapelModell(string titel, string einheit, IReadOnlyList<Reihe> reihen)` — Z. 2678 |
| Rasterkarte | `Zeichenmodell OptimierungsrasterModell(string titel, string xTitel, string yTitel, string skalaTitel, IReadOnlyList<double> cRaten, IReadOnlyList<double> kapazitaetenKwh, double[][] werte, int besteZeile, int besteSpalte, bool[][] unzulaessig = null, string fusszeile = null, string fusszeileZusatz = null)` — Z. 3228 |

Die Griffe im Markup: `data-marke` wie bisher, daneben **`data-wert`** mit dem fertigen
Text. Ein `<svg class="epos-flaeche">` und ein `path.epos-reihe` gibt es bei diesen sieben
Bildern **nicht** — wer hier einen Zoomgriff anbietet, bietet ihn ins Leere.

### Nachweis

| Prüfung | Ergebnis |
|---|---|
| Windows-Messlatte des Rechners (91 Hashes) | 91 von 91 gleich, Text-Diff leer — nach jedem der vier Commits |
| `Proben/ChartProben` | **94 Bilder geprüft, 0 Verstöße** (84 wie bisher, dazu zehn Gegenproben der Gruppe (c)); 91 Hashes geschrieben |
| `WP-Plan.Kern.slnf` Bau Release | 0 Fehler |
| volle Suite mit den CI-Schaltern | grün (`EPOS.Kern.Tests` 4104, `EPOS.UI.Tests` 4918, `SpeicherEngine` 378, `SpeicherPlanung` 27, `KiKern` 499) |
| `ChartRendererGruppeCTests` | 12 neu |
| `ZeichenmodellTests` / `SvgSchreiberTests` | 22 (20 + 2) / 31 (29 + 2) |
| `ZeichenmodellWacheTests`, `ErgebnisbilderTests`, `ChartRendererTests` | grün, unverändert |
| Sichtprüfung `--svg-alle` gegen `--ablage` (Edge kopflos) | `kuchen`, `ring_waermedeckung`, `monatsstapel_drei_reihen`, `optimierungsraster`: gleiche Struktur, gleiche Farben, gleiche Achsen und Legenden — bei diesen Bildern sogar Bildpunkt für Bildpunkt, weil sie keine gebündelten Reihen führen. Der einzige sichtbare Unterschied ist der aus Gruppe (a) bekannte: Doppelte Leerzeichen im Text („Deckung  [kWh]", „Solarthermie   12,0 %") zeigt der Browser als eines |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

### Offen nach dem Kernteil der Gruppe (c)

* **Der UI-Teil der Gruppe:** die sieben Bilder auf `DiagrammSvg` umstellen und den
  `data-wert` beim Zeigen anzeigen (Zeigezeile oder Kurzhinweis am Element) — bis hierher
  gibt es den Wert, aber keinen Nutzer. Dabei ist zu entscheiden, ob `SvgKnoten` eine
  bequeme Eigenschaft `Wert` neben `Marke` bekommt; das Attribut selbst steht im Baum.
* **Die Legendenschalter dieser sieben.** `reihe:<Name>` und `legende:<Name>` tragen
  denselben Schlüssel; die Oberfläche kann sie paarweise schalten. Beim Raster ist der
  Schlüssel der Skalentitel, und die Farbskala (`skala`) gehört zu ihm.
* **Doppelte Leerzeichen im Text** — derselbe offene Punkt wie in Gruppe (a):
  `xml:space="preserve"` am `<text>` wäre die Stelle, und sie gehört zum Baustein.
* **Die Gruppen (b) und (d)** des Rollouts; `ChartBild` bleibt, bis die letzte PNG-Stelle
  umgestellt ist.

---

## Gruppe (a) — Oberfläche

### Die Aufgabe

Die zwölf Zeitreihen-Bilder, deren Zeichenmodelle der Kernteil der Gruppe (a) gebaut hat, in
die Oberfläche bringen: Die Hüllen führen das Modell statt der Bytes, die Reiter und Dialoge
zeigen `DiagrammSvg` statt `ChartBild`, und der Rundlauf-Datenzoom entfällt je umgestellter
Stelle. Dazu die vier Punkte, die der Kernteil unter „Offen für den UI-Teil" hinterlassen
hat: die Zeigerzeile mit dem eigenen Fenster jeder Reihe, die Marke `yachse2`, das Nachladen
ab dem Vierfachen und `xml:space="preserve"`.

`ChartBild` bleibt für die Bilder derselben Seite, die noch kein Modell haben — die Gruppen
(b) und (c) bringen sie nach.

### Der Entscheid

**DG-E3-9 — Der Rundlauf-Datenzoom entfällt je umgestellter Stelle.** Mit dem Bild wandert
sein Zoom: Wo eine Stelle ihr `Zeichenmodell` an `DiagrammSvg` gibt, verliert sie im selben
Schritt den sechsten Wert des Bildauftrags (`Diagrammbereich Bereich`), die
`Diagrammbereich`-Felder und -Rückrufe ihres Reiters (`BereichGewaehlt`, `Zurueckgesetzt`,
die Prüfhilfe `Bereich`) und den Umrechner der Hülle (`ChartRenderer.FensterAusBild` über
`Bildausschnitt`). Der Zeitausschnitt ist dann die `viewBox` der Zeichenfläche — eine
Attributänderung, kein zweiter Renderlauf.

*Warum je Stelle und nicht in einem Zug.* Der Rundlauf ist die einzige Zoomart, die ein PNG
kennt; eine Seite, die beide Bildarten trägt (die Wärmepumpe: drei Modelle und die
Streuwolke), braucht ihn für die verbliebene weiter. Der Entscheid löst ihn deshalb dort ab,
wo sein Gegenstand verschwindet, und lässt ihn stehen, wo noch ein Pixelbild hängt. Das ist
genau das, was Konzept § 6 als „Doppelarbeit, rund drei Zeilen je Bildstelle" angekündigt
hat.

*Was dabei NICHT fällt:* der Kernanteil (`Achsenfenster`, `Zugeschnitten`, `XAchseFenster`).
Er ist der Zustand des Zooms, den auch der Bericht druckt, und die `…Modell`-Methoden nehmen
ihn weiterhin entgegen — die Ganglinienquelle des Bedarfsdialogs schneidet damit ihre Woche
und ihren Tag zu, und der Zoom im Bild bewegt sich INNERHALB dieses Ausschnitts.

### Was der Baustein dazubekommen hat

| Punkt | Wie er gelöst ist |
|---|---|
| **Zeigerzeile je Reihe** | `Reihenindex(reihe, x)` rechnet über das EIGENE Fenster der Reihe: Index = (x − `Fenster.XVon`) / Schrittweite, mit Schrittweite = (`XBis` − `XVon`) / (n − 1) — dieselbe Rechnung wie in `SvgSchreiber.Reihenpfad`. Liegt x außerhalb des Fensters, steht die Reihe nicht in der Zeile; sie ist dort nicht gezeichnet |
| **Einheit je Reihe** | `Einheit` gilt für die linke Achse, `EinheitRechts` für die zweite. Welche Reihe rechts steht, sagt ihr Fenster: eine andere y-Spanne als die Zeichenfläche (`AufRechterAchse`) |
| **`yachse2` fällt mit seinen Reihen** | `RechteAchseLeer` — es gibt Reihen der rechten Achse UND jede ist abgewählt; dann bekommt jeder Knoten mit der Marke `yachse2` `display="none"` |
| **Nachladen ab dem Vierfachen** | `FensterGemeldet` rechnet Faktor = volle Breite / (bis − von). Ab 4 bekommt jede Reihe, für die `Pfadregel.Roh(n, reihen)` falsch ist, ihren Ausschnitt roh: `SvgSchreiber.Reihenpfad(reihe, flaeche, von, bis, roh: true)`. Das Ergebnis steht in `_ausschnitt` und ERSETZT beim Zeichnen das `d` des Pfades — der Baum bleibt derselbe, Blazor tauscht ein Attribut. Unter dem Vierfachen und in der Vollansicht wird `_ausschnitt` geleert, und der Vollpfad des Schreibers steht wieder |
| **`xml:space="preserve"`** | an jedem `<text>`, das der Baustein zeichnet |
| **Flächen schalten wie Linien** | ohne Zutun: Der Schreiber gibt einer Fläche denselben `data-reihe`-Griff wie einer Linie, und `display="none"` wirkt auf beide. Der bunit-Fall hält es fest |
| **Ticks ohne Kalender** | die neue `Achsenart` (Jahresstunde, Index, Stützstelle, Rang). Nur `Jahresstunde` nimmt `ChartRenderer.Jahresstundenteilung`; die übrigen bekommen eine ganzzahlige Teilung mit höchstens sieben Marken auf einer runden Schrittweite. Sie entscheidet außerdem, ob die Zeigerzeile „4.000 h" oder „4.000" schreibt |

### Die umgestellten Stellen

**Simulations-Ergebnisreiter.** `SimulationErgebnisHuelle.Bilder.cs` liefert jetzt
`Modell(Bildauftrag)` neben `Bild(Bildauftrag)`; die Seite hält beide in eigenen
Zwischenspeichern.

| Bild | Modell | Reiter, Kennung |
|---|---|---|
| Bedarf Wärme | `GanglinieNormiertModell` | `BedarfReiter`, `simerg-bedarf-waerme` |
| Bedarf Strom | `GanglinieNormiertModell` | `BedarfReiter`, `simerg-bedarf-strom` |
| WP Produktion | `ErzeugerStapelModell` | `WaermepumpeReiter`, `simerg-wp-produktion` |
| WP Stromverbrauch | `JahresverlaufModell` | `WaermepumpeReiter`, `simerg-wp-strom` |
| Speichertemperaturen | `TemperaturverlaufModell` | `WaermepumpeReiter`, `simerg-wp-temperaturen` |
| Heizkessel | `ErzeugerStapelModell` | `HeizkesselReiter`, `simerg-heizkessel` |
| Solarthermie | `ErzeugerStapelModell` | `SolarthermieReiter`, `simerg-solarthermie` |
| BHKW | `ErzeugerStapelModell` | `BhkwReiter`, `simerg-bhkw` |
| Photovoltaik | `ErzeugerStapelModell`, zweite Achse | `PhotovoltaikReiter`, `simerg-photovoltaik` |
| Speicherbetrieb | `SpeicherbetriebModell`, zweite Achse | `StromspeicherReiter`, `simerg-speicherbetrieb` |
| Wärmegang | `ErzeugerStapelModell`, zweite Achse | `WaermegangReiter`, `simerg-waermegang` |
| Stromgang | `ErzeugerStapelModell` | `StromgangReiter`, `simerg-stromgang` |

**Bedarf, Quellen, Kosten und die Bedarfsstammdialoge:**

| Stelle | Modell | Kennung |
|---|---|---|
| `BedarfErgebnisHuelle` — Jahresverlauf Brauchwasser | `JahresverlaufModell` | `bedarf-jahresverlauf` |
| `BedarfErgebnisHuelle` — Ganglinienquelle Woche/Tag | `JahresverlaufModell` mit `Achsenfenster` | `bedarf-gang-<Stufe>-<Nummer>` |
| `QuellprofilHuelle` | `JahresverlaufModell` | `quellprofil` |
| `KostenprofilHuelle` | `KostenprofilModell` | `kostenprofil` |
| `TypStammHuelle`, Wochen-Stundenprofil | `StundenprofilModell` | `typprofil` |
| `GebaeudetypHuelle`, Tages-Stundenprofil | `StundenprofilModell` | `gebaeudetyp` |
| `GebaeudeHuelle` | `GanglinieNormiertModell` | `gebaeude-bedarf` |
| `WaermebedarfExternHuelle`, `StromganglinieHuelle` | `GanglinieNormiertModell` | `ganglinie-<Schlüssel>` |

**Was PNG bleibt — und warum:**

| Stelle | Grund |
|---|---|
| Streuwolke „Leistung über Außentemperatur" | keine Zeitachse: x ist die Außentemperatur; ihr Modell entsteht in einer späteren Gruppe |
| Ring „Wärmedeckung", Ring „Stromdeckung" | Kreissegmente, kein x |
| Monatssäulen der Autarkie-Analyse | zwölf Monate, kein Zeitraster zum Zoomen |
| Monatssäulen des Bedarfsergebnisses | dasselbe; sie stehen auf demselben Blatt wie der umgestellte Jahresverlauf |
| Flottenansicht der Stromspeicher-Auslegung | gehört zur Gruppe (d). Sie hängt ihren Ausschnitt seither selbst an den Zwischenspeicherschlüssel, statt ihn durch einen `Bildauftrag` zu schleifen, der ihn nicht mehr führt |

### Drei Stellen, an denen die Umsetzung über den Auftrag hinausgeht

1. **Ein gemeinsamer Rumpf `Farbwahlwirt` statt zwanzigmal derselben zwanzig Zeilen.** Der
   Auftrag nennt „`FarbeSetzen`/`FarbeZuruecksetzen` über die Hülle … wie im Klimadialog".
   Der Klimadialog ist EINE Stelle; hier sind es zehn Reiter und acht Dialoge, und der Weg
   ist überall derselbe: zwei Parameter, zwei Rückrufe, ein `StateHasChanged`. Sie stehen
   deshalb einmal in `EPOS.UI/Bausteine/Farbwahlwirt.cs`, und ein Wirt schreibt
   `@inherits Farbwahlwirt`. Der Klimadialog bleibt unverändert — sein Weg ist derselbe, nur
   ausgeschrieben.
2. **Die Modelle werden zwischengespeichert wie zuvor die Bilder — aus einem zweiten
   Grund.** Beim PNG war der Zwischenspeicher eine Ersparnis. Beim Modell ist er Bedingung:
   `DiagrammSvg` baut seinen Knotenbaum nur neu, wenn die REFERENZ des Modells wechselt, und
   ein Modell, das je Zeichenlauf neu entstünde, verwürfe mit dem Baum auch Zoom,
   Zeigerstelle und abgewählte Reihen. Die Ergebnisseite führt dafür `_modelle` neben
   `_bilder`, `GanglinienGrafik` und `GebaeudeBedarfDialog` je einen Eintrag je
   Schalterstellung.
3. **Die Kennung trägt, was das Bild unterscheidet.** Zwei `DiagrammSvg` auf einem Blatt
   dürfen nicht dieselbe `clipPath`-Kennung bekommen. Wo ein Wirt sein Bild wechselt, ohne
   die Komponente zu tauschen — der Navigator der Ganglinienstufen, der Schalter
   „sortiert" —, wandert das Unterscheidende in die Kennung: `bedarf-gang-<Stufe>-<Nummer>`,
   `ganglinie-<Schlüssel>`.

### Die Prüfseite

`Proben/Rasterprobe/Wirt/Seiten/DiagrammSvgProbe.razor` (`/diagrammsvg`) kennt neben dem
Jahresgang der Etappe E2 drei weitere Bilder:

| Adresse | Was sie zeigt |
|---|---|
| `?bild=erzeugerstapel` | zwei gestapelte Flächen, eine Linie darüber, eine Reihe auf der zweiten Achse in kWh — alle vier über **35 040 Viertelstunden**, also gebündelt: Erst hier gibt es etwas nachzuladen |
| `?bild=stundenprofil` | eine Fläche mit Randlinie über 168 Wochenstunden; `Achsenart.Index`, ganzzahlige Teilung im Ausschnitt |
| `?bild=speicherbetrieb` | drei Leistungen um die Nulllinie und der Ladezustand rechts — zum Prüfen, dass `yachse2` mit ihm fällt |

Der Stand über dem Bild trägt `data-nachgeladen` (wie viele Pfade der Baustein gerade roh
nachgerechnet hat) und `data-achse2aus`.

### Offen nach dem UI-Teil der Gruppe (a)

* **Die Gerätenachweise (A-DG-1)** stehen weiter aus — Windows bei 125 % DPI und das iPad.
  Neu dazugekommen sind der Griff auf den Legendeneintrag einer FLÄCHE und die Zeigerzeile
  mit zwei Einheiten.
* **Die Gruppen (b), (c) und (d)** des Rollouts. `ChartBild` bleibt, bis die letzte
  PNG-Stelle umgestellt ist; die verbliebenen Stellen stehen oben.
* **Für die umgestellten DIALOGE gibt es keine Wiki-Seiten.** Die Rubrik „Programm
  Dokumentation" führt dreizehn Seiten; „Bedarfsergebnis", „Quellprofil", „Bedarfstyp",
  „Gebäudetyp", „Wärmebedarf extern", „Stromganglinie" und „Gebäude" kommen dort nicht vor.
  Die neue Bedienung dieser Dialoge ist damit unbeschrieben — entweder eine eigene Seite
  oder ein Verweis auf den Abschnitt „Die Diagramme bedienen" der Seite
  „Simulationsergebnisse".
* **Die Stromspeicher-AUSLEGUNG beschreibt noch den Datenzoom durch Ziehen im Bild**
  (`Programm Dokumentation - Stromspeicher.wiki`). Das trifft zu, solange ihre Bilder PNG
  sind; mit der Gruppe (d) muss der Absatz mit.
* **Zwei Wünsche an den Kern** stehen im Bericht des Auftrags; sie brauchen keine Änderung
  an `Zeichenmodell.cs`, `SvgSchreiber.cs` oder `ChartRenderer.cs`, sondern nur eine
  Zusage, dass die Reihen der zweiten Achse ihr y-Fenster behalten und die
  `…Modell`-Methoden ihr `Achsenfenster` weiterhin annehmen.

---

## Gruppen (b) und (c) — Oberfläche, Abschluss E3

### Die Aufgabe

Die einundzwanzig verbliebenen `ChartBild`-Stellen auf `DiagrammSvg` umstellen — und damit
den Umstieg der Oberfläche von PNG auf SVG abschließen. Danach gibt es in `EPOS.UI` kein
Renderer-Pixelbild mehr; `ChartBild`, `Diagramm`, `Diagrammbereich`, der CSS-Transform-Modus
des JS-Moduls und die zwei Prüfklassen dazu entfallen im selben Auftrag.

Die Gruppe unterscheidet sich von (a) in einem Punkt, und der prägt alles Weitere: **Zwölf
der einundzwanzig Bilder haben keine Zeichenfläche.** Eine Säule, eine Rasterzelle, ein
Ringsegment führen keine Datenreihe, aus der sich ein Wert lesen ließe — und zwischen zwei
Monaten, zwei ganzen Geräten oder zwei Ringsegmenten liegt nichts, worauf ein Zoom zeigen
könnte. Der Kern hat diesen Bildern mit Gruppe (c) statt dessen den **Wert am Element**
mitgegeben (`data-wert`); die Oberfläche musste ihn nur noch zeigen.

### Die Entscheide

**DG-E3-10 — Wert am Zeiger.** Bei Bildern ohne Zeichenfläche (Säulen, Stapel, Balken,
Raster, Ring, Kuchen, Kennlinien, Jahresprojektion, Stückzahl) zeigt die Zeigerzeile den
`data-wert` des Elements, auf das der Zeiger zeigt (`@onpointerenter`/`@onpointerleave` je
markiertem Element; Berührung: Antippen zeigt, erneutes Antippen daneben löscht). Kein Zoom,
keine Leiste, keine JS-Bindung bei diesen Bildern (`Flaeche == null` ⇒ `OhneZoom`).

**DG-E3-11 — Achsenart aus dem Modell.** Der Baustein liest `Zeichenflaeche.X` und
`XEinheit` aus dem Kern; der Oberflächen-Parameter `Achsenart` aus Gruppe (a) entfällt (die
Aufrufstellen der Gruppe (a) nachziehen). Ticks bei `Achsenart.Wert` über
`ChartRenderer.Achsenteilung(flaeche, von, bis)`; die Zeigerzeile nimmt bei Reihen mit
`XWerte` den nächstliegenden Punkt zur Zeigerstelle, sonst den Index wie bisher; die Einheit
je Reihe aus `Datenreihe.Einheit`, der Rückfall aus dem Parameter.

**DG-E3-12 — Achsenseite.** `Datenreihe` bekommt `Achsenseite` (`Links`/`Rechts`, Vorgabe
Links); der Kern setzt sie in `ErzeugerStapelModell` und `SpeicherbetriebModell` für die
rechte Achse; der Baustein liest sie statt aus der y-Spanne zu raten.

**DG-E3-13 — Abschluss: kein PNG mehr in der Oberfläche.** Nach der Umstellung der 21 Stellen
gibt es keine `ChartBild`-Stelle mehr. Dann entfallen `EPOS.UI/Standards/ChartBild.razor`,
`EPOS.UI/Bausteine/Diagramm.razor`, `Diagrammbereich.cs` (falls ohne Leser), der
CSS-Transform-Modus des JS-Moduls samt Stilregeln, `ChartBildTests` und `DiagrammTests` — im
selben Auftrag, mit Beleg (`grep`), dass kein Leser bleibt (auch `Proben/Rasterprobe/Wirt`,
`EPOS.iOS`, `WindowsFormsApplication1` prüfen). Der `byte[]`-Weg des Renderers bleibt für den
Bericht. `EPOS.UI/CLAUDE.md` beschreibt danach den gültigen Stand: jedes Diagramm der
Oberfläche ist ein `Zeichenmodell` im Baustein `DiagrammSvg`; keine Geschichte.

Dazu gelten unverändert DG-E2-1, DG-E2-3 und DG-E3-1 bis DG-E3-9.

### Die Kern-Nachzüge

Der Kern brachte aus den Gruppen (b) und (c) die Modelle der Bildarten mit, nicht aber die
Modelle der **Oberflächenbilder** und der **Flottenansichten** — sie stehen nicht im
`ChartRenderer`, sondern in eigenen Klassen. Fünf Nachzüge in einem Commit, kein Bild
geändert:

| Was | Wo |
|---|---|
| `SpeicherBetriebsbild.Modell(…)` und `PeakShavingBild.Modell(…)` als Zwillinge von `Zeichnen`/`Lastgang`; der PNG-Weg malt deren Ergebnis | `EPOS.Kern/Allgemein/Bericht/` |
| je `byte[]`-Bildmethode ein Zwilling: `RasterModell`, `SchnittmodellBeiSpalte`/`…BeiZeile`, `AusschnittModell`, `StueckzahlModell`, `StueckzahlrasterModell`, `Modelle` (Netz/SoC), `JahresprojektionsModell`; die `byte[]`-Methoden gehen über den gemeinsamen Helfer `Gemalt` | `EPOS.Kern/Controller/SpeicherFlottenAnzeigeCtrl*.cs` |
| `Zeichenbefehl.Wert` an den Datenelementen der Pixelbilder der Gruppe (b): Punktmarken der Kennlinien, Säulen und Ersatzjahr-Marken der Jahresprojektion, Säulen und Bestmarke der Stückzahlkurve — Format wie in Gruppe (c) (`Achsenwert`, `Elementwert`, `Achseneinheit`) | `ChartRenderer.cs` |
| `Datenreihe.Achsenseite` samt Setzen in `ErzeugerStapelModell` und `VerlaufsbildModell`; `SvgKnoten.Wert` neben `Marke` | `Zeichenmodell.cs`, `SvgSchreiber.cs`, `ChartRenderer.cs` |
| die Stützpunkte der Schnittkurve als eigene `Punkte`-Reihen — grob und fein getrennt | `ChartRenderer.cs` |

**Zwei Stellen gingen über den Auftrag hinaus.**

1. **Die Stützpunkte der Schnittkurve brauchen ZWEI Reihen, nicht eine.** Das PNG zeichnet
   Grobpunkte in `STAMM` mit Radius 3,5 und Feinpunkte der zweiten Suchphase in `FEINRASTER`
   mit Radius 2,5. Eine `Datenreihe` trägt genau EINE Farbe und EINE Strichbreite;
   zusammengelegt sähe das SVG anders aus als das Bild. Eine leere Art bekommt keine Reihe —
   so bleibt die Reihenzahl bei einer reinen Grobsuche bei zwei, und die `Pfadregel` bündelt
   die Kurve nicht plötzlich anders.
2. **In der Dauerlinie zählt x den RANG, nicht die Stunde.** `ErzeugerStapelModell`,
   `VerlaufsbildModell` und `GanglinieNormiertModell` setzten `Achsenart.Stunden`
   unabhängig vom Schalter „sortiert". Solange die Oberfläche ihre Achsenart selbst mitgab,
   fiel das nicht auf; mit DG-E3-11 liest sie die des Modells, und eine Jahresstundenteilung
   mit Monatsnamen über einer Rangachse wäre schlicht falsch. Die drei Methoden sagen jetzt
   `Zeitachsenart(sortiert)`. **Kein PNG ändert sich davon** — der Maler übergeht die
   Zeichenfläche ganz.

### Was der Baustein dazubekommen hat

| Punkt | Wie er gelöst ist |
|---|---|
| **Wert am Zeiger** | Der Baustein zählt beim Bauen des Baumes einmal, ob überhaupt ein Knoten einen `Wert` trägt (`_baumHatWerte`). Trägt einer und hat das Bild keine Zeichenfläche, bekommt jeder Knoten mit Wert `onpointerenter` (setzen), `onpointerleave` (löschen — **nur bei der Maus**) und `onpointerdown` (setzen). Die Fläche selbst bekommt `onpointerdown`, das löscht — aber nur, wenn nicht gerade ein Element gemeldet hat. Ein Druck steigt vom Element zur Fläche auf; damit gilt bei Berührung genau die Regel des Entscheids: Antippen zeigt, Antippen daneben löscht |
| **Achsenart aus dem Modell** | `XAchsenart` liest `Modell.Flaeche.X`; `XMass` nimmt `Flaeche.XEinheit`, sonst den Parameter, sonst „h" auf einer Stundenachse. Die Ticks des Ausschnitts kommen aus `ChartRenderer.Achsenteilung` — die eine Funktion, die Stunden kalendarisch, einen Index ganzzahlig und einen Wert mit „schönen" Stufen teilt. Der Parameter `Achsenart` und die hauseigene `Ganzzahlteilung` des Bausteins sind entfallen |
| **Nächstliegender Punkt statt Index** | `Reihenindex` sucht bei einer Reihe mit `XWerte` den kleinsten Abstand statt über die Schrittweite zu rechnen. Die Schnittkurve mischt Grob- und Feinpunkte, die Streuwolke führt die Außentemperatur je Stunde — dort gibt es keinen gleichmäßigen Schritt |
| **Einheit je Reihe** | `Reiheneinheit` nimmt `Datenreihe.Einheit`, sonst `Einheit` bzw. `EinheitRechts` |
| **Achsenseite** | `AufRechterAchse` liest `Datenreihe.Achsenseite` statt die y-Spanne zu vergleichen |
| **Eine Reihe steht einmal in der Zeile** | Die Schnittkurve führt Kurve und Stützpunkte unter DEMSELBEN Namen, damit die Legende beides zusammen schaltet. Die Zeigerzeile nennt jeden Namen deshalb nur einmal |
| **Eine Punktwolke wird nie nachgeladen** | Ihr Vollpfad trägt schon jeden Punkt (DG-E3-5); ein Fensterpfad wäre genau sein Ausschnitt |
| **Die Zeigerstelle ist eine Gleitkommazahl** | Das JS-Modul meldete ganze Stunden. Auf einer C-Raten-Achse von 0,1 bis 2,0 fiele eine ganzzahlige Stelle mit dem ganzen Bild zusammen. Gerundet wird jetzt auf ein Tausendstel der SICHTBAREN Breite — feiner als ein Bildpunkt und zugleich **weniger** Meldungen als zuvor (8 760 ganze Stunden auf rund 1 000 Bildpunkte waren acht Meldungen je Bildpunkt) |
| **`WertAmZeigerGeaendert`** | Ein Rückruf für den Wirt, der den Wert woanders braucht. Das Bild selbst zeigt ihn ohne ihn; ein Zeichenlauf der Komponente frischt aber die Seite DARÜBER nicht auf, und genau die misst der Prüfstand |
| **Ring und Kuchen** | `LegendeSchaltbar="false"` an der Aufrufstelle — ein Kreis, dem ein Segment fehlt, ist kein Kreis mehr. Der Wert am Zeiger und die Farbwahl über das Farbfeld bleiben |

### Was entfallen ist (DG-E3-13)

| Was | Warum es gehen konnte |
|---|---|
| `EPOS.UI/Standards/ChartBild.razor` | keine Aufrufstelle mehr |
| `EPOS.UI/Bausteine/Diagramm.razor` | einziger Leser war `ChartBild` |
| `EPOS.UI/Bausteine/Diagrammbereich.cs` | einziger Leser war `Diagramm` samt den Rundlauf-Rückrufen der umgestellten Stellen |
| der CSS-Transform-Modus des JS-Moduls (`binden`-Option `modus`, `male`, `meldeBereich`, `setzeStufe`, `zoomeUm`, der Zustand `inhalt`/`vx`/`vy`/`viewbox`) | er bediente nur `Diagramm` |
| die Stilregeln `.epos-diagramm`, `.epos-diagramm--rund` (samt den zwei Folgeregeln), `.epos-diagramm-flaeche`, `…:focus-visible`, `.epos-diagramm-inhalt`, `.epos-chartbild` | dieselbe Ursache |
| `EPOS.UI.Tests/Standards/ChartBildTests.cs`, `EPOS.UI.Tests/Bausteine/DiagrammTests.cs` | sie prüften genau diese zwei Bausteine |

**Was BLEIBT und warum.** Die Leiste, der Stufentext, die Knöpfe, das Gummiband und der
Platzhalter tragen dieselben Klassennamen (`.epos-diagramm-leiste`, `-stufe`, `-knopf`,
`-gummi`, `--zieht`, `--bereich`, `.epos-chartbild-platzhalter`) — `DiagrammSvg` benutzt sie
weiter. Der `byte[]`-Weg des Renderers bleibt vollständig stehen: Er ist der Weg des
**Berichts** und die Messlatte der ChartProben.

### Die Prüfseite

`Proben/Rasterprobe/Wirt/Seiten/DiagrammSvgProbe.razor` (`/diagrammsvg`) kennt neben den vier
Bildern der Etappen E2/E3a fünf weitere, alle aus synthetischen Reihen ohne Zufall:

| Adresse | Was sie zeigt |
|---|---|
| `?bild=raster` | eine Rasterkarte Kapazität × C-Rate mit **einem Loch** und **zwei gesperrten Zellen** — beide Sonderfälle des Werttextes („nicht gerechnet", „(unzulässig)") sind damit messbar |
| `?bild=ring` | ein Ring mit vier Segmenten; **Legende nicht schaltbar**, Werte am Zeiger ja |
| `?bild=kennlinien` | drei Vorlaufstufen mit Punktmarken — jede Marke nennt Außentemperatur und Wert |
| `?bild=streuwolke` | 8 760 Punkte über der Außentemperatur; hier lässt sich prüfen, dass die Zeigerzeile den **nächstliegenden** Punkt nimmt statt eines Index |
| `?bild=kapitalwert` | zwei Verläufe über dem Projektjahr — das einzige der fünf **mit** Zeichenfläche (`Achsenart.Wert`, Einheit „a") und damit mit Zoom |

Der Stand über dem Bild (`#diagrammsvg-stand`) trägt neben `data-nachgeladen` und
`data-achse2aus` jetzt **`data-wert-am-zeiger`**.

### Der Befund am Anfang: der zusammengeführte Stand baute nicht

Vor dem ersten Schritt war `EPOS.UI` **rot**: 28 Fehler CS0104, „`Achsenart` ist ein
mehrdeutiger Verweis". Die Oberfläche der Gruppe (a) hatte eine eigene Aufzählung
`EPOS.UI.Bausteine.Achsenart` mitgebracht, der Kern der Gruppe (b) eine gleichnamige
`WindowsFormsApplication1.Zeichnung.Achsenart` — und die Oberfläche zieht beide Namensräume.
Zwei Zweige, jeder für sich grün, zusammen rot; der Kern-Filter der CI sieht `EPOS.UI` nicht
und meldete es deshalb nicht.

**DG-E3-11 löst es an der Wurzel**: Die Oberflächen-Aufzählung entfällt, es bleibt die des
Kerns. Ein Alias hätte den Bau geradegerückt und den doppelten Begriff stehen lassen.

### Offen nach dem Abschluss der Etappe E3

* **Die Gerätenachweise (A-DG-1)** stehen weiter aus — Windows bei 125 % DPI und das iPad.
  Neu dazugekommen ist der Wert am Zeiger bei BERÜHRUNG: Antippen und Antippen daneben sind
  im Prüfstand gemessen, nicht am Gerät.
* **Doppelte Leerzeichen im Text** sind erledigt: Der Baustein setzt `xml:space="preserve"`
  an jedem `<text>` (Gruppe (a), UI-Teil).
* **Für mehrere umgestellte DIALOGE gibt es keine Wiki-Seiten.** Die Rubrik „Programm
  Dokumentation" führt dreizehn Seiten; „Wärmepumpe", „Bedarfsergebnis", „Quellprofil",
  „Bedarfstyp", „Gebäudetyp", „Wärmebedarf extern", „Stromganglinie", „Gebäude",
  „Erdreichquelle" und „Lastspitzenkappung" kommen dort nicht vor. Die neue Bedienung dieser
  Dialoge ist damit unbeschrieben — entweder eine eigene Seite oder ein Verweis auf den
  Abschnitt „Die Diagramme bedienen" der Seite „Simulationsergebnisse".

### Die Wiki-Seiten

| Seite | Was geändert wurde |
|---|---|
| `Programm Dokumentation - Simulationsergebnisse.wiki` | Der Absatz „Drei Bilder dieser Auswertung sind Pixelbilder" ist ersetzt: Ringe, Monatssäulen und Monatsstapel zeigen den **Wert des Elements**, auf das der Anwender zeigt (samt Berührung), die Legende eines Rings ist nicht schaltbar; die Streuwolke zoomt über der **Außentemperatur** |
| `Programm Dokumentation - Stromspeicher.wiki` | Der **Datenzoom durch Ziehen im Bild** ist aus der Steuerzeile der Ergebnisdiagramme heraus und als Zoom IM Bild beschrieben (mit Verweis auf „Die Diagramme bedienen"); die Zeitraumwahl bleibt daneben stehen. Die Jahresprojektion und die drei Auslegungsbilder ohne Achse (Raster, Stückzahlkurve, Stückzahlraster) zeigen ihren Wert am Zeiger — samt „(unzulässig)" und „nicht gerechnet"; Schnittkurven und Ausschnitt zoomen über ihrer Größenachse |
| `Programm Dokumentation - Wirtschaftlichkeit.wiki` | Der Abschnitt **Verlauf…** beschreibt die zwei Bilder samt Zoom, Zeigerzeile mit dem Projektjahr, Legendenwahl und Farbfeld |

**Fehlende Seiten** — nicht angelegt, nur gemeldet: „Wärmepumpe" (die zwei Kennlinien der
Anlagen- und der Stammmaske), „Bedarfsergebnis" (Monatssäulen und Jahresverlauf),
„Erdreichquelle" und „Lastspitzenkappung". Die Rubrik „Programm Dokumentation" führt sie
nicht.

### Nachweis

| Prüfung | Ergebnis |
|---|---|
| Windows-Messlatte des Rechners (91 Hashes) | 91 von 91 gleich, Text-Diff leer — vor dem ersten Schritt, nach dem Kern-Commit und in der Abnahme |
| `Proben/ChartProben` | 106 Bilder geprüft, 0 Verstöße |
| `WP-Plan.Kern.slnf` Bau Release | 0 Fehler |
| `WP-Plan.sln` Debug x64 (im Worktree) | 0 Fehler |
| `Proben/Rasterprobe/Wirt` | 0 Fehler |
| volle Suite mit den CI-Schaltern | 0 Fehler |
| `ChartRendererNachzuegeTests` (neu) | 14 Fälle: die zwei Oberflächenbilder malen ihr eigenes Modell, die Achsenseite steht an der Reihe, jede Punktmarke/Säule/Marke nennt ihren Wert, die Stützpunkte der Schnittkurve stehen als zwei Punktreihen, die Dauerlinie zählt einen Index — und **kein Bild ändert sich dabei** |
| `grep -rn '<ChartBild\|<Diagramm ' EPOS.UI Proben --include=*.razor` | ohne Treffer |
| Doku-, Repo- und Wiki-Wachen | grün |
| Tabu-Regex über die geänderten Wiki-Seiten | keine Treffer in den neuen Zeilen |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

### Die einundzwanzig umgestellten Stellen

| Stelle | Bild | Modell | Kennung | Fläche |
|---|---|---|---|---|
| `Dialoge/Bedarf/BedarfErgebnisDialog` | Monatssäulen | `MonatsSaeulenModell` | `bedarf-monate` | nein |
| `Dialoge/Simulation/QuelleErdreichDialog` | Jahresgang | `JahresgangModell` | `quelle-erdreich` | ja |
| `Dialoge/Strom/PeakShavingDialog` | Lastgang | `PeakShavingBild.Modell` | `peakshaving-lastgang` | ja |
| `Dialoge/Strom/SpeicherFlottenErgebnisAnsicht` | Jahresprojektion | `JahresprojektionsModell` | `flotte-projektion` | nein |
| … | Netzbild | `Modelle(…).Netz` | `Netzkennung` (Zeitraum, Einheit, Sortierung) | ja |
| … | Ladezustand | `Modelle(…).Soc` | `Sockennung` | ja |
| `Dialoge/Strom/SpeicherFlottenGroessenAnsicht` | Stückzahlkurve | `StueckzahlModell` | `flotte-stueckzahl` | nein |
| … | Stückzahlraster | `StueckzahlrasterModell` | `flotte-stueckraster` | nein |
| … | Rasterkarte | `RasterModell` | `flotte-raster-<Einheit>` | nein |
| … | Ausschnitt | `AusschnittModell` | `flotte-ausschnitt-<Einheit>` | ja |
| … | Spaltenschnitt | `SchnittmodellBeiSpalte` | `flotte-schnitt-spalte-<Einheit>-<Spalte>` | ja |
| … | Zeilenschnitt | `SchnittmodellBeiZeile` | `flotte-schnitt-zeile-<Einheit>-<Zeile>` | ja |
| `Dialoge/Waermepumpe/WaermepumpeAnlageDialog` | Kennlinie COP | `KennlinienModell` | `wp-kennlinie-cop` | nein |
| … | Kennlinie Leistung | `KennlinienModell` | `wp-kennlinie-leistung` | nein |
| `Dialoge/Waermepumpe/WaermepumpeStammDialog` | Kennlinie COP | `KennlinienModell` | `wp-kennlinie-cop` | nein |
| … | Kennlinie Leistung | `KennlinienModell` | `wp-kennlinie-leistung` | nein |
| `Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufDialog` | Differenz | `KapitalwertVerlaufModell` | `kapitalwert-differenz` | ja |
| … | absolut | `KapitalwertVerlaufModell` | `kapitalwert-absolut` | ja |
| `Seiten/Simulation/ErgebnisReiter` | Monatsstapel | `MonatsStapelModell` | `simerg-monate` | nein |
| `Seiten/Simulation/UebersichtReiter` | Ring Wärme/Strom | `RingModell` | `simerg-ring-waerme` / `-strom` | nein |
| `Seiten/Simulation/WaermepumpeReiter` | Streuwolke | `StreuwolkeModell` | `simerg-wp-streuwolke` | ja |

**Neun der einundzwanzig haben eine Zeichenfläche** und damit Zoom und die Zeigerzeile aus
den Reihen; die übrigen zwölf zeigen den Wert am Element.
