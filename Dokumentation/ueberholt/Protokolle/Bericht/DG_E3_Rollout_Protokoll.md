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
