# Darstellung der Reiter „Detaillierte Simulation"

Stand 09.09.2026 (W11b‑B‑23). Dieses Blatt ist das **Muster**, dem jeder Reiter von
`EPOS.UI/Seiten/Simulation/SimulationErgebnisSeite.razor` folgt — und dem ein neuer Reiter
folgen muß. Es beschreibt nur die **Darstellung**; welche Zahl woher kommt, sagt das
Fachkonzept, und die Begründungen der einzelnen Schritte stehen in
`WindowsFormsApplication1/Allgemein/Reporting/iU9_W11b_Blazor_Port_Protokoll.md`
(Abschnitte W11b‑B‑10 bis W11b‑B‑23).

## 1. Die Reihenfolge im Reiter

Von oben nach unten, immer:

1. **Leerhinweis**, wenn der Lauf die Komponente nicht führt — `<p class="epos-simerg-hinweis">`
   mit einem Text, der die **fehlende Komponente nennt**
   (`SIM_MSG_KEINE_DATEN_<KOMPONENTE>`). Dann steht sonst nichts da.
2. **Kennzahlenblöcke** (Abschnitt 2).
3. **Tabellen** über die volle Breite (Abschnitt 4).
4. **Diagramme**, jedes mit derselben Steuerzeile darüber (Abschnitt 5).
5. **Export- und Sprungknöpfe** am Blockende: `<button class="epos-simerg-knopf">`.

## 2. Kennzahlen: Hauptgruppen nebeneinander

* Jede fachliche Gruppe ist eine **Hauptgruppe** und trägt einen **dunklen Balken**
  (`<Gruppenkopf Titel="…" />`). Kein `h3`, keine namenlose Liste.
* **Erste Rasterzeile: `Wärme` | `Strom`** — dieselbe Ordnung wie die zwei Ringe des
  Übersichtsreiters, links Wärme, rechts Strom. Führt ein Reiter keine Wärme, stehen dort
  seine zwei fachlichen Hauptgruppen (Photovoltaik: `Erzeugung` | `Bedarf und Deckung`).
* **Zweite Rasterzeile: alles Weitere** — `Auslegung`, `Betrieb`, `Brennstoffverbrauch …`,
  `Einstrahlung`. Jede Zeile ist ein eigenes `<div class="epos-simerg-spalten">`; nur so ist
  die zweite Zeile auch auf einem breiten Schirm eine zweite.
* Steht eine Gruppe **allein** in ihrer Rasterzeile, trägt ihre `section` zusätzlich
  `epos-simerg-kennzahlenzeile` (`grid-column: 1 / -1`). Sonst deckelt die 320‑px‑Spalte des
  `auto-fit`-Rasters die Liste, und die Einheitenspalte fällt in einen Rollbalken.
* Ein **Unterabschnitt** (`<h3 class="epos-untergruppe">`) steht nur INNERHALB einer
  Hauptgruppe, wenn deren Liste sich noch einmal gliedert — heute genau einmal:
  „Wärmebedarf je Bedarfsart" in der Wärmegruppe des Bedarfsreiters.
* Die Gruppentitel kommen aus **`SIMERG_GRP_*`** und tragen **keinen Doppelpunkt**.
* Bleibt eine Gruppe nach ihrer Präsenzregel **leer**, steht statt der Liste ein
  `<Warnbanner>` — kein Balken ohne Inhalt.

## 3. Die Kennzahlenliste selbst

```razor
<dl class="epos-simerg-werte">
    @Wertzeile(Resource.SIMERG_LBL_…, Zahl(…), "MWh/a")
    @Wertzeile(Resource.SIMERG_LBL_REST…, Zahl(…), "MWh/a", betont: true)
</dl>
```

* Drei Spalten: **Beschriftung · Zahl · Einheit**. Die Einheit steht **nur** in der
  Einheitsspalte (`dd.epos-simerg-einheit`), niemals im Beschriftungstext.
* **Beschriftungen enden auf einen Doppelpunkt** (`SIMERG_LBL_*`). Ausgenommen sind
  Beschriftungen, die aus dem Lauf kommen (Namen von Bedarfsarten, Brennstoffen, Modulen).
* **Zahlen:** `N2` (zwei Nachkommastellen, Tausendertrennung) für Energien, Leistungen,
  Temperaturen und Quoten. `N0` für Stunden und Zyklen. Kein `F2`. Immer
  `CultureInfo.CurrentCulture`. Ersatzzeichen bei unbekanntem Wert wörtlich wie das Vorbild
  (`-`, `—`, `0` oder leer).
* Die **Rest- bzw. Summenzeile schließt ihre Gruppe** ab: `betont: true` setzt
  `epos-simerg-abschluss` an `dt` und beide `dd` (fett, Linie darüber). Genau eine je Gruppe.
* Ohne `betont` trägt die Zeile **kein** Klassenattribut — `Zeilenklasse` gibt `null`
  zurück, nicht `""`.
* Die Reihenfolge innerhalb einer Gruppe folgt dem Rechenweg:
  **Bedarf → Erzeugung → Überschuß → Rest → Deckung.**

## 4. Tabellen

* `<table class="epos-raster">`, außerhalb des Spaltenrasters und damit über die volle Breite.
* Zahlenzellen: `<td class="epos-simerg-zahl">` (rechtsbündig, tabellarische Ziffern).
  Textzellen bekommen **kein** Klassenattribut, auch kein leeres.
* Die Kennzahlentabelle des Stromspeichers ist die Ausnahme: `epos-simerg-kennzahlen` mit
  `tr.epos-simerg-gruppenkopf` / `tr.epos-simerg-untergruppe` / `tr.epos-simerg-summe` —
  eine Tabelle kann ihre Gruppen nicht als Balken tragen.

## 5. Diagramme

Über **jedem** Bild dieselbe Steuerung, in dieser Reihenfolge:

1. `<div class="epos-simerg-schalter">` mit **„sortiert"** — die Darstellungsart
   (Ganglinie ↔ Dauerlinie). Nur, wo das Bild sie kennt. Ein Reiter mit zwei Bildern
   derselben Art führt **einen** Schalter ganz oben (Bedarfsreiter, W11b‑B‑16).
2. `<div class="epos-simerg-schalter">` mit **je Reihe einem Schalter** — beschriftet mit
   **derselben Ressource wie die Legende** des Bildes, in der Reihenfolge des Bildes,
   vorbelegt wie das Vorbild (in der Regel alle an).
3. `<ChartBild Png="…" Alt="…" />`. Der Baustein bringt die **Zoomleiste** (`×1`, `1:1`)
   selbst mit; ein `<img>` an `ChartBild` vorbei wäre ein Bild ohne Zoom.
   **Jede Jahresganglinie** trägt dazu `BereichGewaehlt` und `Zurueckgesetzt` — dann
   erscheint zusätzlich der Knopf „Bereich" (Datenzoom, siehe § 5.1).
4. Steht das Bild in einem Spaltenraster, trägt seine `section`
   `epos-simerg-diagrammzeile` (volle Zeile, gedeckelt auf 75 % bzw. das Familienmaß).

**Die Reihenwahl geht als sprachneutrale Liste in `Bildauftrag.Reihen`**
(`"WAERMEBEDARF"`, `"WAERMEPRODUKTION"`, …). Dabei gilt:

| `Reihen` | Bedeutung |
|---|---|
| `null` | keine Angabe → **alle** Reihen (so rufen Bilder ohne Auswahl) |
| leere Liste | der Anwender hat alles abgewählt → **keine** Reihe; der Renderer zeichnet seinen Leerhinweis |

Die Hülle wertet das mit `Alle(a)` / `Gewaehlt(a, alle, "…")` aus
(`WindowsFormsApplication1/Views/Simulation/SimulationErgebnisHuelle.Bilder.cs`) — kein
`wahl.Count == 0`-Rückfall.

### 5.1 Datenzoom — an JEDER Jahresganglinie (W11b‑B‑24)

Ein aufgezogenes Rechteck lässt den **Kern das Bild mit diesem Zeitausschnitt neu
zeichnen**; „1:1" holt das ganze Jahr zurück. Der Bildzoom des Bausteins (Rad, Kneifgeste,
Doppelklick) bleibt daneben bestehen — er vergrößert nur Bildpunkte und ist für 8 760
Stützstellen auf 1 100 Bildpunkten zu grob.

Der Weg ist überall derselbe und in **drei** Schritten fertig:

| Schicht | Was zu tun ist |
|---|---|
| Reiter | ein `Diagrammbereich?`-Feld **je Bild**, `BereichGewaehlt="@(b => _bereich = b)"` und `Zurueckgesetzt="@(() => _bereich = null)"` am `ChartBild`, der Bereich als **sechster** Wert im `Bildauftrag` |
| Hülle | `Fenster(a, laenge)` → `ChartRenderer.FensterAusBild(…)`, weitergereicht an die Zeichenmethode; `laenge` = 8 760 Stunden bzw. 35 040 Viertelstunden |
| Kern | die Zeichenmethode schneidet **zuerst** zu (`Zugeschnitten`) und beschriftet die x-Achse mit `XAchseFenster` |

**Wer ihn bekommt** — jedes Bild mit **Zeitachse**: Bedarf (Wärme, Strom), Wärmegang,
Stromgang, Wärmepumpe (Wärmelast, Stromverbrauch, Speichertemperaturen), Heizkessel,
Solarthermie, BHKW, Photovoltaik, Ladezustand des Stromspeichers.

**Wer ihn nicht bekommt** — jedes Bild **ohne** Zeitachse: die Streuwolke
„Leistung über Außentemperatur" (x = Temperatur), die Monatssäulen der Autarkie, Kuchen
und Ringe. Dort gibt es keinen Rückruf und deshalb auch keinen Knopf „Bereich".

Zwei Regeln, die man leicht verliert:

* **Der Ausschnitt gilt in beiden Zweigen.** Auch die Dauerlinie („sortiert") wird aus dem
  zugeschnittenen Ausschnitt gebildet; der Umschalter verwirft den Bereich **nicht**.
* **Jedes Bild führt seinen eigenen Ausschnitt** — auch die drei Bilder eines Reiters.
  Der Zwischenspeicherschlüssel trennt sie schon (`Bildauftrag.Schluessel` führt den
  Bereich mit).

Der **senkrechte** Anteil (`Achsenfenster.YAnteil`) wirkt nur dort, wo die Null unten
bleibt und die Skala eine Leistungsskala ist — also im `ErzeugerStapel` und im
`Jahresverlauf`. `GanglinieNormiert` (0…100 % der Jahresspitze) und `Temperaturverlauf`
(Achse ohne Nullpunkt) übergehen ihn.

## 6. Hinweise, Warnungen, Kacheln

* Leiser Hinweis: `<p class="epos-simerg-hinweis">`. Warnung mit Rolle: `<Warnbanner Stufe="…">`.
* Zustandszeile über einem Block: `<p class="epos-simerg-status">`, im Fehlerfall zusätzlich
  `epos-simerg-warn`.
* Kennzahlkacheln stehen im `<Kachelraster>`; ihre Zahlen folgen derselben Regel wie die
  Listen (`N2`, `N0`).

## 7. Was ausdrücklich NICHT vereinheitlicht ist

* **Datenzoom** („Bereich"): an **jedem** Bild mit Zeitachse, aber an keinem ohne — die
  Streuwolke, die Monatssäulen, Kuchen und Ringe bleiben beim Bildzoom (§ 5.1). Die
  Zoomleiste selbst hat jedes Bild.
* **Zahlenformate in Tabellen** (`F1` für Jahresnutzungsgrad, Vollzyklen, Füllstand; `F4`
  für den Wechselrichter-Nutzungsgrad): Das sind Genauigkeiten des Vorbilds, keine
  Schreibweisen.
* **Gruppenbalken über Tabellen**: nur die Kesseltabelle trägt einen; die übrigen
  Modultabellen nennen sich in ihrer ersten Spaltenüberschrift.
