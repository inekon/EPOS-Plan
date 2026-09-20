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
