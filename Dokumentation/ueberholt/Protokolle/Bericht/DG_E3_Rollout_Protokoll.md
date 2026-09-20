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
