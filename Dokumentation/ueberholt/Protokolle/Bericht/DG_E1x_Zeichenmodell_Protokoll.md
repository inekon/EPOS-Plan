# DG-E1x — das Zeichenmodell hinter dem `ChartRenderer` (Protokoll)

Etappe E1 des Konzepts
[`Konzept_Diagramme_Interaktiv_EPOS-Plan.md`](../../../aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md)
in drei Aufträgen: **E1a** Modell, Maler, Farbrollen, Skala und die Zeitreihenbilder;
**E1b** Säulen, Stapel, Kuchen, Ring, Balken; **E1c** Kennlinien, Streuwolke, Schnitt- und
Stückzahlkurve, Optimierungsraster — und danach nur noch der Modellweg.

Der gültige Stand steht in der Statusdatei und im Konzept; hier steht, wie es geworden ist.

---

## E1a — Modell, Maler, Farbrollen, Skala, 15 Zeitreihenbilder (Statusnummer #400)

### Die Aufgabe

Zwischen dem Layout des Renderers und der Ausgabe eine **Befehlsliste** einziehen, ohne dass
sich ein Bild um ein Byte ändert. Die Messlatte dafür lag aus E0 bereit:
`Proben/ChartProben/Messlatte_2026-09-20.sha256`, 91 Hashes aus 71 Proben. Sie ist
unverrückbar — jede Abweichung wird am Bild gesucht, nie durch Anpassen der Liste behoben.

### Was entstanden ist

`EPOS.Kern/Allgemein/Bericht/Zeichnung/` mit vier Dateien:

| Datei | Inhalt |
|---|---|
| `Zeichenmodell.cs` | Fläche (Breite, Höhe, Hintergrund) und Befehle in Zeichenreihenfolge: `Linie`, `Rechteck`, `Kreis`, `Ellipse`, `Kreissegment`, `Pfad`, `Text`, `Gruppe` mit Zuschnitt; dazu `Stift`, `Fuellung`, `Schrift`, `Strichmuster`, die Ziele `IZeichenziel`/`Befehlssammler` und die `Wertliste` |
| `Farbpalette.cs` | `Farbe`, `Farbrolle`, `Farbton`, `Farbpalette` — die Farbschicht |
| `SkiaMaler.cs` | `SkiaBruecke` (die einzigen Skia-Umrechnungen), `Schriftkette`, `Schriftmass`, `SkiaMaler` und `SkiaZiel` |
| `Skala.cs` | `Rund`, `Nice`, `Stufe`, `Bedarf` |

**Das Modell kennt keine Skia-Typen.** `Farbe`, `Punkt` und `Rahmen` sind eigene Werttypen.
Das ist keine Formsache: Der SVG-Schreiber der Etappe E2 soll dasselbe Modell lesen, ohne
SkiaSharp zu brauchen. Alles Skia-Nahe — Umrechnung, Schriftkette, Maler — steht deshalb in
einer eigenen Datei.

**Die Textvermessung bleibt eine Kern-Funktion.** 46 Stellen des Bestands setzen einen Text
rechtsbündig oder mittig, indem sie ihn vorher messen; der Befehl trägt danach eine fertige
Koordinate. Damit beide Seiten zusammenbleiben, liefert `ChartRenderer.Schrift(...)` jetzt ein
**`Schriftmass`**: `MeasureText` für das Layout, `Satz` für den Befehl. Jede vorhandene
Schreibweise (`using (var f = Schrift(15f))`, `f.MeasureText(lab)`, `TextHoehe(f)`) blieb
dadurch unverändert.

### Der Farbentscheid (DG-Q7)

Anwenderentscheid vom 20.09.2026: „Die Farben der Diagramme sollen jeweils änderbar sein."
Im Befehl steht deshalb **kein Farbwert**, sondern ein `Farbton`:

* nur **Rolle** → die Palette entscheidet;
* Rolle + **Deckung** → die Palettenfarbe mit anderer Deckung (der Fall `WithAlpha` der Flächen);
* Rolle + **feste Farbe** → eine im Layout gerechnete Farbe (Verlauf, Abstufung, Mischung), die
  ihre Herkunftsrolle mitführt.

Aufgelöst wird **beim Malen**, gegen `Farbpalette.Aktuell`; `Farbpalette.Vorgabe` trägt die
Hausfarben Wert für Wert und ist damit zugleich die Messlatte.

**Der Kunstgriff, der das billig macht,** ist die Rückwärtssuche `Farbpalette.Ton(farbe)`: Eine
Hausfarbe, die als Wert hereinkommt — aus `C_WP` und Geschwistern, aus `Reihe.Farbe` einer Hülle
—, bekommt ihre Rolle zurück (erst die genaue Farbe samt Deckung, dann dieselbe Farbe mit anderer
Deckung, zuletzt der Rückfall ohne Rolle). So erreicht ein späterer Palettentausch auch die
Bilder, deren Farben von außen hereingereicht werden, **ohne dass eine der 26 Zeichenmethoden
angefasst wird** — und ohne dass die 36 Aufrufstellen der Hüllen sich ändern.

Die 38 Rollen: `HINTERGRUND`, `TEXT`, `ACHSE`, `RASTER`, `RAHMEN`, `LEGENDENRAHMEN`;
`WAERME_WP`, `WAERME_BHKW`, `WAERME_KESSEL`, `WAERME_SOLAR`, `STROM_PV`, `STROM_NETZ`, `REST`,
`BEDARF`, `STAMM`; `SERIE_1`…`SERIE_8`; `SPEICHER_1`…`SPEICHER_6`; `KOSTENPROFIL`,
`PROFILFLAECHE`, `PROFILLINIE`, `QUELLTEMPERATUR`, `AUSSENTEMPERATUR`, `ERSATZJAHR`;
`RASTER_SCHLECHT`, `RASTER_MITTE`, `RASTER_GUT`, `RASTER_LOCH`, `FEINRASTER`.

**Zwei Farben tragen zwei Rollen.** `SERIE_1`…`SERIE_4` sind wertgleich mit den vier
Erzeugerfarben, `ACHSE` wertgleich mit der Beschriftungsfarbe. Die Rückwärtssuche entscheidet
sich für die zuerst eingetragene Rolle; weil der Wert derselbe ist, ändert das kein Bild. Wer
später eine der beiden Rollen einzeln tauschen will, trennt die Werte — dann wird die Suche
eindeutig.

### Der Übergang: ein Rumpf, zwei Ziele

E1a stellt die Helfer um, E1b und E1c die restlichen Methoden. Damit beides nebeneinander läuft,
hat jeder gemeinsame Helfer **einen Rumpf auf `IZeichenziel` und eine dünne
`SKCanvas`-Überladung**, die in ein `SkiaZiel` wickelt; dieses Ziel malt jeden Befehl sofort auf
die Leinwand. Eine noch nicht umgestellte Methode bekommt damit Bildpunkt für Bildpunkt dasselbe
Bild wie zuvor — und es gibt trotzdem nur eine Fassung des Rumpfs. Am Ende von E1c entfallen die
Überladungen, `SkiaZiel` und die beiden Paint-Fabriken `Strich`/`Fuellung`.

Umgestellt sind 19 Helfer, dazu ein neuer: `Leerhinweis`, `Titel`, `PanelRahmen`, `Text`,
`ProzentRaster`, `YRaster`, `BedarfsRaster`, `AchsenRaster`, `XAchse`, `XAchsentitel`,
`XAchseFenster`, `Legende`, `Linienzug`, `Vieleck`, `Kreissegment`, `ZeichneLinie`,
`ZeichneFlaeche`, `StapelZeichnen`, `VerlaufLinie` — und `Achsenkreuz`, das fünf gleichlautende
Blöcke der beiden Achsenlinien ablöst. `Start` und `Png(SKSurface)` bekommen mit `Modell(b, h)`
und `SkiaMaler.Png(modell)` ihre Gegenstücke.

Die fünf Skalenrechnungen sind in `Skala` zusammengezogen, aber **als vier benannte Varianten**
(`Rund`, `Nice`, `Stufe`, `Bedarf`) und nicht vereinheitlicht: Sie liefern nicht dasselbe, und
jede Vereinfachung verschöbe ein Bild. Die dreimal gleichlautende „schöne Stufen"-Schleife im
Rumpf von `Jahresgang`, `KapitalwertVerlauf` und `Kostenprofil` ist die Variante `Stufe`.

### Die 15 Zeichenmethoden

`JahresverlaufWaerme`, `DauerlinieWaerme`, `StrombilanzMonate` (über ihre Kernzeichner
`StapelDiagramm`, `LinienDiagramm`, `MonatsBalken`), `Speicherverlauf`, `Speichertemperaturen`,
`Jahresverlauf`, `GanglinieNormiert`, `ErzeugerStapel`, `Temperaturverlauf` und
`Speicherbetrieb` (beide über `Verlaufsbild`), `Jahresgang`, `Kostenprofil`, `Stundenprofil`,
`KapitalwertVerlauf`, `Jahresprojektion`. Das sind **14 Leinwandblöcke für 15 öffentliche
Methoden**.

Der Zeitausschnitt blieb unberührt: `Achsenfenster`, `FensterAusBild`, `Zugeschnitten` und
`XAchseFenster` rechnen wie zuvor, nur ins Modell.

### Was an der Byte-Gleichheit schwierig war

1. **Die Paint-Belegung je Befehl.** Der Maler musste die Belegung der beiden Fabriken
   wörtlich nachbilden: `Stroke`/`Fill`, Strichstärke, `IsAntialias = true`, und die
   Abweichungen vom Skia-Standard **nur dort, wo der Bestand sie setzte** — `StrokeJoin.Round`
   an den Linienzügen, `CreateDash` an den dreizehn gestrichelten Stellen. Deshalb tragen
   `Strichkappe` und `Strichverbindung` die Skia-Vorgaben als Nullwert, und der Maler setzt sie
   nur, wenn sie davon abweichen.
2. **Wertgleichheit der Befehle.** Ein Record mit einem nackten Array vergliche die Referenz;
   zwei gleich gefüllte Pfade wären dann verschieden, und der Determinismustest des Modells
   liefe ins Leere. Punktfolgen und Gruppeninhalte stehen deshalb in einer `Wertliste`, die
   ihre Elemente vergleicht. Aus demselben Grund ist das Strichmuster ein Record
   (`Strich`, `Lücke`, `Versatz`) und kein `float[]` — alle dreizehn Muster des Bestands sind
   zweiteilig mit Versatz 0.
3. **Die Namensgleichheit von Typ und Methode.** `Text`, `Schrift`, `Fuellung`, `Linie` und
   `Gruppe` heißen im Modell wie Methoden des Renderers. Innerhalb von `ChartRenderer` gewinnt
   die Methode, der Typ wird deshalb qualifiziert (`Zeichnung.Stift`, `Zeichnung.Fuellung`);
   die Modellwerte entstehen über die Fabriken `Stift(...)` und `Flaeche(...)`, damit die
   Aufrufstellen ihre Form behalten.
4. **Die Reihenfolge bei Rechteck und Pfad.** Wo der Bestand erst füllte und dann umrandete
   (Legendenfelder), tut der Maler dasselbe: Füllung vor Rand, im selben Befehl. Zwei getrennte
   Befehle blieben nötig, wo zwei verschiedene Stifte übereinanderliegen.
5. **Ein Werkzeugfehler, der auffiel, bevor er schadete.** Das Skript, das einen Leinwandblock
   in ein Modell hebt, suchte die Methode zunächst über ihren Namen — und traf den ersten
   AUFRUF statt der Deklaration; gehoben wurde dann die übernächste Methode. Es sucht seither
   die Deklaration (`static` in derselben Zeile, keine Kommentarzeile) und nimmt den Namen des
   Modells als Argument, weil in `Speicherverlauf` und `Speichertemperaturen` der Buchstabe `z`
   schon dem `ZeitreihenSatz` gehört.

**Kein Bild ist gewandert.** Der Messlatte-Diff war nach jedem der vier Umbau-Commits leer; es
gab keinen Fall, in dem ein Bild zu untersuchen gewesen wäre.

### Nachweis

| Prüfung | Ergebnis |
|---|---|
| Messlatte `Messlatte_2026-09-20.sha256` | 91 von 91 Hashes gleich, Text-Diff leer — nach jedem Umbau-Commit |
| `Proben/ChartProben` | 71 Bilder, 0 Verstöße |
| `WP-Plan.Kern.slnf` Bau und volle Suite mit den CI-Schaltern | grün (EPOS.Kern 3 953, EPOS.UI 4 859, SpeicherEngine 378, SpeicherPlanung 27, KiKern 499) |
| `ChartRendererTests` | 15 grün, beide Kulturen |
| neu `ZeichenmodellTests` | 14 — Reihenfolge, Gruppe und Zuschnitt, Wertgleichheit, Determinismus, Rollen und Palettentausch |
| neu `SkiaMalerTests` | 17 — je Primitive PNG byte-gleich gegen ein von Hand mit Skia gemaltes Gegenstück, dazu Zuschnitt, Zeichenreihenfolge und Palettentausch |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

### Offen nach E1a

* **E1b** (8 Methoden) und **E1c** (5 Methoden); danach entfallen die `SKCanvas`-Überladungen,
  `SkiaZiel`, `Start`, `Png(SKSurface)` und die beiden Paint-Fabriken.
* Elf Methoden rufen Skia noch unmittelbar (`DrawLine` 23, `DrawRect` 13, `DrawCircle` 6,
  `DrawPath` 3, `DrawOval` 2) — alle in den Bildern von E1b und E1c.
* Die gerechneten Farben der Rasterkarte (`Farbstufe`, `Mischung`, `Rasterfarbe`) bekommen
  ihren Rollenbezug erst mit E1c; bis dahin kommen sie als Wert ohne Rolle ins Modell.
* `Punktmarken`, `Schraffur`, `Farbskala` und `Umbruchtext` sind Helfer der Bilder von E1c und
  stehen noch auf `SKCanvas`.

---

## E1b — Säulen, Stapel, Kuchen, Ring, Balken (Statusnummer #401)

### Die Aufgabe

Die fünf Zeichenmethoden, die noch auf einer Leinwand malten und nicht zu den Kurvenbildern
von E1c gehören, auf das Modell heben — byte-gleich gegen dieselbe unverrückbare Messlatte.
Dazu der Nachtrag aus dem Farbentscheid: **Wo eine Methode ihre Farbe SELBST wählt, nennt sie
die Rolle ausdrücklich**, statt einen Hausfarbenwert durch die Rückwärtssuche zu schicken.

### Die fünf Zeichenmethoden

`Kuchen`, `BalkenHorizontal`, `MonatsSaeulen`, `Ring` (beide Überladungen über den einen
langen Rumpf) und `MonatsStapel`. Die drei Kernzeichner der Gruppe — `StapelDiagramm`,
`LinienDiagramm`, `MonatsBalken` — standen schon auf dem Modell, weil die Zeitreihenbilder von
E1a durch sie hindurchgehen; hier kam nichts an ihnen dazu.

Damit stehen **23 von 26 Methoden** auf dem Modell. Unmittelbar auf eine Leinwand malen nur
noch `Kennlinien`, `Streuwolke`, `Schnittkurve`, `Stueckzahlkurve` und `Optimierungsraster` —
die fünf Bilder von E1c. Die Zahl der unmittelbaren Skia-Zeichenaufrufe fiel von 47 auf 38
(`DrawOval` 2 → 1, `DrawRect` 13 → 7, `DrawCircle` 6 → 5, `DrawLine` 23 → 22, `DrawPath`
unverändert 3; der Rest liegt in den `SKCanvas`-Fassungen von `Kreissegment`, `Linienzug` und
`Vieleck`, die E1c mit sich nimmt).

### Die Rolle im Befehl statt des Werts

Drei Schreibweisen kamen dafür hinzu — `Farbton.Aus(rolle)` im Modell, dazu die Überladungen
`Flaeche(Farbrolle)`, `Stift(Farbrolle, …)` und `Text(…, Farbrolle, …)` im Renderer, neben den
bestehenden Fassungen für Farben von außen.

**Die Trennlinie ist die Herkunft der Farbe, nicht ihr Wert.** Ausdrücklich genannt werden
HINTERGRUND (der weiße Trennring des Kuchens, das Innenloch des Rings), LEGENDENRAHMEN, TEXT,
ACHSE, STAMM und WAERME\_WP (die beiden Balkenfarben). Die Rückwärtssuche behalten
`Segment.Farbe`, `Ringsegment.Farbe`, `Reihe.Farbe` und die Sichtfarbe der Monatssäulen — sie
kommen aus den Hüllen herein, und ihre Rolle steht dort nicht.

Das verschiebt kein Bild, weil die Vorgabepalette dieselben Werte trägt; der neue Testfall
`AusdrueckenDerRolleIstWertgleichZurRueckwaertssuche` hält beides fest — den gleichen Farbwert
für JEDE Rolle und den gleichen Ton überall dort, wo die Rückwärtssuche eindeutig ist.

### Was an der Byte-Gleichheit schwierig war

**Nichts, was ein Bild gekostet hätte** — der Messlatte-Diff war nach jedem der vier
Umbau-Commits leer, kein Bild war zu untersuchen. Vier Stellen verdienten trotzdem Sorgfalt:

1. **Füllung vor Rand, zwei Aufrufe im Bestand.** Kuchenlegende, Balken und die Legendenfelder
   setzten `DrawRect` zweimal ab — erst mit dem Füll-, dann mit dem Strich-Paint. Im Modell
   stehen dafür zwei `Rechteck`-Befehle mit je einer Seite und nicht ein Befehl mit beidem:
   Ein Befehl malte dasselbe, aber der Bestand hält die Reihenfolge über zwei Befehle, und die
   Gleichheit soll am Aufrufbild ablesbar bleiben.
2. **`DrawOval` als Rand ohne Füllung.** Der weiße Trennring des Kuchens ist eine `Ellipse`
   mit Stift und ohne Füllung; der Maler zeichnet dann nur den Rand, wie `DrawOval` mit einem
   Stroke-Paint.
3. **Der Vollkreis des Rings.** `Kreissegment` trägt den Sonderfall ab 360° (Skia zieht sonst
   nichts) im Modell und im Maler — beide Seiten hatten ihn schon aus E1a, der Ring hängt sich
   nur daran.
4. **Die wertgleichen Rollen.** `ACHSE` und `WAERME_WP` teilen ihren Wert mit keiner anderen
   Rolle, `SERIE_1`/`SERIE_3` sehr wohl. Deshalb prüft der neue Testfall die Gleichheit des
   TONS nur dort, wo die Rückwärtssuche eindeutig ist, und die des WERTS überall — sonst wäre
   er eine Behauptung über die Eintragsreihenfolge der Palette.

### Nachweis

| Prüfung | Ergebnis |
|---|---|
| Messlatte `Messlatte_2026-09-20.sha256` | 91 von 91 Hashes gleich, Text-Diff leer — nach jedem Umbau-Commit |
| `Proben/ChartProben` | 71 Bilder, 0 Verstöße |
| `WP-Plan.Kern.slnf` Bau und volle Suite mit den CI-Schaltern | grün |
| `ChartRendererTests` | 15 grün, beide Kulturen |
| `ZeichenmodellTests` | 15 (14 + 1 neu: ausdrückliche Rolle gegen Rückwärtssuche) |
| `SkiaMalerTests` | 17 grün |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

### Offen nach E1b

* **E1c** — `Kennlinien`, `Streuwolke`, `Schnittkurve`, `Stueckzahlkurve`,
  `Optimierungsraster`; danach entfallen `SkiaZiel`, die sechzehn `SKCanvas`-Überladungen,
  `Start`, `Png(SKSurface)`, die `SKCanvas`-Fassungen von `Kreissegment`, `Linienzug` und
  `Vieleck` sowie die beiden Paint-Fabriken `Strich` und `Fuellung`.
* Die gerechneten Farben der Rasterkarte (`Farbstufe`, `Mischung`, `Rasterfarbe`) und die
  Helfer `Punktmarken`, `Schraffur`, `Farbskala`, `Umbruchtext` bleiben E1c.
* Die Helfer der schon umgestellten Bilder nennen ihre Farben weiter als Wert
  (`Titel` STAMM, `YRaster`/`XAchse` RASTER und ACHSE, `Legende` LEGENDENRAHMEN). Das ist
  wertgleich und kostet kein Bild; ausdrücklich genannt werden sie, wenn E1c die letzten
  `SKCanvas`-Überladungen entfernt und jeder Helfer ohnehin angefasst wird.

---

## E1c — Kennlinien, Streuwolke, Schnitt-, Stückzahlkurve, Raster; Abschluss E1 (Statusnummer #402)

### Die Aufgabe

Die letzten fünf Zeichenmethoden auf das Modell — und danach den Übergang abräumen: kein
`SkiaZiel`, keine `SKCanvas`-Überladung, keine Paint-Fabrik. Kein Bild darf wandern.

### Die fünf Zeichenmethoden

| Methode | Was dazukam |
|---|---|
| `Kennlinien` | Punktmarken (Kreis bzw. Kreuz) als `Kreis`- und `Linie`-Befehle; die gestrichelte Nulllinie als `Strichmuster(6, 2)`; die acht Variantenfarben über die neue Rollenliste `Serienrolle` (SERIE_1…8) statt über `C_SERIEN` |
| `Streuwolke` | die Punktwolke als `Kreis`-Befehle mit EINER Füllung je Reihe; die Reihenfarbe kommt von außen und behält die Rückwärtssuche |
| `Schnittkurve` | Kurve, Grob- und Feinpunkte, die rote Optimumsmarke; die x- und y-Skalen wie gehabt über `Skala.Rund` |
| `Stueckzahlkurve` | Säulen mit Rand, Schraffur der unzulässigen Stückzahlen, die offene Optimumsmarke |
| `Optimierungsraster` | Zellen, Netz, Marke, Farbskala und die zwei Fußzeilen; die Zellfarbe kommt jetzt als `Farbton` aus `Rasterfarbe` |

Damit stehen **26 von 26 Zeichenmethoden** auf dem Modell.

### Die Schraffur wird eine Gruppe

`Schraffur` war die einzige Stelle des Renderers mit `Save`/`ClipRect`/`Restore`. Sie ist
jetzt eine `Gruppe` mit Zuschnittrechteck — derselbe Befehl, den `ErzeugerStapel` schon für
seine Fenster nutzt. Der Maler setzt ihn wieder auf `Save`/`ClipRect`/`Restore` um; die
Striche laufen also weiterhin nicht in die Nachbarzelle, und das PNG ist byte-gleich. Der
Gewinn steht in E2: Ein SVG-Weg schreibt daraus ein `<clipPath>`, und niemand muss die Regel
ein zweites Mal formulieren.

### Die gerechneten Farben der Rasterkarte

Der offene Punkt aus E1a. `Rasterfarbe` und `Farbstufe` gaben eine nackte `SKColor` zurück —
und damit hätte ein Palettentausch die Rasterkarte nicht erreicht, obwohl sie ihre drei
Eckfarben aus der Palette bezieht. Jetzt liefern beide einen `Farbton`:

* die Enden der Dreifarbskala und das Loch als **reine Rolle** (`RASTER_SCHLECHT`,
  `RASTER_GUT`, `RASTER_LOCH`);
* jeder Zwischenton als **gerechnete Farbe mit Herkunftsrolle** — die untere der beiden
  gemischten Rollen, damit am Befehl ablesbar bleibt, woraus er entstand;
* `Mischung` mischt seither **Rollenfarben aus `Farbpalette.Aktuell`** statt zweier
  Konstanten. Gegen die Vorgabepalette sind das Bit für Bit dieselben Werte, gegen eine
  getauschte Palette wandert die ganze Skala mit.

### Alle Helfer nennen ihre Rolle

Der Nachtrag aus E1b. 17 × `Stift(Gainsboro)` → `RASTER`, 5 × `Stift(DimGray)` → `ACHSE`,
`Silver` → `RAHMEN`, `Gray` → `LEGENDENRAHMEN`, 45 × die Textfarbe `DimGray` → `ACHSE`, die
Legendenschrift `Black` → `TEXT`, der Titel `C_STAMM` → `STAMM`. `Text` und `Stift` tragen
jetzt einen `Farbton`; die Fassungen mit `Farbrolle` und mit `SKColor` sind dünne
Überladungen darauf. Die Farbe der ZWEITEN Achse ist der Fall, an dem beide Wege
aufeinandertreffen: Sie ist die Reihenfarbe, wenn es genau eine Reihe gibt, sonst die
Achsenfarbe — also `y2G[0].Farbe.Ton()` oder `Farbton.Aus(Farbrolle.ACHSE)`.

### Was der Abschluss entfernt hat

`SkiaZiel`; die 17 `SKCanvas`-Überladungen der gemeinsamen Helfer; `Start`,
`Png(SKSurface)`; die Paint-Fabriken `Strich` und `Fuellung`; die Leinwandfassungen von
`Kreissegment`, `Linienzug` und `Vieleck`; die unbenutzte Hülle `Schriftart`. Aus 91 privaten
Helfern sind 71 geworden.

**Was mit Absicht BLEIBT:** `SKColor`, `SKRect` und `SKPoint`. Sie zeichnen nichts — sie sind
Farbe, Rechteck und Punkt als Werttypen und stehen in der öffentlichen Fläche (`C_WP` …,
`Reihe.Farbe`, `Segment.Farbe`) und im Layout jeder Methode. Ihr Ersatz durch `Farbe`,
`Rahmen` und `Punkt` berührt jede Hülle und jeden Aufrufer und gehört deshalb in eine eigene
Etappe, nicht in einen Auftrag, dessen Messlatte „kein Bild ändert sich" heißt. Die
Textvermessung bleibt Kern-Funktion, steht aber in `Zeichnung/SkiaMaler.cs`
(`Schriftkette`, `Schriftmass`) und nicht mehr im Renderer.

### Der Wächter

`EPOS.Kern.Tests/ZeichenmodellWacheTests` — vier Fälle: kein Skia-Bezeichner außer den drei
erlaubten Werttypen, kein `Draw…`/`ClipRect`/`Canvas`/`PathEffect`, 26 Bildmethoden und
mindestens ebenso viele Übergaben an `SkiaMaler.Png`, und kein `SkiaZiel` mehr im Kern.

**Warum ein Wächter und nicht nur ein Aufräumen.** Die Umstellung ändert kein Bild — genau
deshalb fällt ein Rückfall nicht auf: Ein neuer Zeichenaufruf auf einer Leinwand liefert
dasselbe PNG wie der Modellweg und bliebe in der Hash-Messlatte unsichtbar. Erst der SVG-Weg
stieße darauf, und dann wäre die Ursache Monate alt.

### Was an der Byte-Gleichheit schwierig war

* **Die gestrichelten Linien.** Drei Stellen setzten den Strichel als `SKPathEffect` NACH dem
  Anlegen des Paints (`stift.PathEffect = …`). Im Modell ist das Muster Teil des `Stift`, und
  der Maler hängt den Effekt in derselben Reihenfolge an — `CreateDash({6,2}, 0)` bzw.
  `{8,5}`. Byte-gleich, aber nur, weil der Maler `StrokeCap` und `StrokeJoin` unverändert
  lässt, solange sie auf der Vorgabe stehen.
* **Die Reihenfolge Füllung/Rand.** Die Säulen der Stückzahlkurve wurden erst gefüllt, dann
  umrandet, dann schraffiert. Im Modell sind Füllung und Rand EIN `Rechteck`-Befehl — der
  Maler malt in derselben Reihenfolge, und die Schraffur folgt als eigener Befehl. Ein
  zusammengefasster Befehl mit vertauschter Reihenfolge hätte am Antialiasing des Randes
  sichtbar gelegen.
* **Die Punkte der Schnittkurve.** Grob- und Feinpunkte standen im Bestand in EINER Schleife
  mit zwei vorbereiteten Pinseln; wer sie in zwei Schleifen trennt, ändert die
  Zeichenreihenfolge und damit die Überdeckung dort, wo zwei Punkte aufeinanderliegen. Die
  eine Schleife ist geblieben, nur die Pinsel sind zu zwei Füllungen geworden.
* **Die Bildhöhe des Optimierungsrasters.** Sie wird aus dem umgebrochenen Fußblock
  gerechnet, BEVOR die Zeichenfläche entsteht. Das Modell entsteht deshalb an genau
  derselben Stelle wie vorher die Fläche — eine Zeile früher, und ein Bild mit Zusatzhinweis
  wäre zu niedrig geworden.
* **Die Vollkreis-Falle.** Der Kommentar dazu stand am Leinwand-`Kreissegment`, das entfällt;
  er ist an den Modellbefehl gewandert, wo die Regel jetzt entschieden wird
  (`SkiaMaler`, `Winkel >= 360°` → Ellipse).

### Nachweis

| Prüfung | Ergebnis |
|---|---|
| Messlatte `Messlatte_2026-09-20.sha256` | 91 von 91 Hashes gleich, Text-Diff leer — nach jedem der fünf Umbau-Commits |
| `Proben/ChartProben` | 71 Bilder, 0 Verstöße |
| `WP-Plan.Kern.slnf` Bau und volle Suite mit den CI-Schaltern | grün |
| `ChartRendererTests` | 15 grün, beide Kulturen |
| `ZeichenmodellTests` / `SkiaMalerTests` | 15 / 17 grün |
| `ZeichenmodellWacheTests` | 4 neu, grün |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler |
| Referenzlauf | nicht nötig — kein Rechenweg berührt |

### Offen nach E1c

* **E2 ist frei:** `SvgSchreiber` aus demselben Modell, `DiagrammSvg.razor`, erste Stelle
  Klimadialog.
* `SKColor`, `SKRect` und `SKPoint` stehen weiter in der öffentlichen Fläche des Renderers
  und in den Hüllen — eigene Etappe (siehe oben).
* Die schwarze Optimumsmarke (Rasterkarte, Stückzahlkurve) und der weiße Zellen- und
  Säulenrand nennen die WERTGLEICHEN Rollen `TEXT` und `HINTERGRUND`. Das ist heute richtig —
  der Rand IST der Bildgrund —, aber ein Palettentausch von `TEXT` zöge die Marke mit. Ob
  beide eigene Rollen bekommen, entscheidet der Auftrag, der die Palette zur
  Anwendungseinstellung macht (DF-1); `Farbpalette.cs` ist in E1c bewusst nicht angefasst
  worden, damit dessen Merge ein reiner Anhängekonflikt bleibt.
* `Segment` und `Ringsegment` tragen ihre Farbe weiter als `SKColor`; die Legende bildet sie
  über die Rückwärtssuche ab. Bei wertgleichen Rollen (Orange ist SERIE_1 und die
  BHKW-Farbe) trifft sie die zuerst eingetragene — für das Bild gleichgültig, für einen
  späteren Palettentausch nicht. Ein `Farbton` im Segment gehört in denselben Auftrag wie
  die Einstellungsmaske.
