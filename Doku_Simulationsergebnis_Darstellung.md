# Darstellung der Reiter „Detaillierte Simulation"

Stand 10.09.2026 (W11b‑B‑29). Dieses Blatt ist das **Muster**, dem jeder Reiter von
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
2. **Parameterblock**, wenn der Reiter einen führt (bislang nur „Stromspeicher", § 1.1).
3. **Kennzahlenblöcke** (Abschnitt 2).
4. **Tabellen** über die volle Breite (Abschnitt 4).
5. **Diagramme**, jedes mit derselben Steuerzeile darüber (Abschnitt 5).
6. **Export- und Sprungknöpfe** am Blockende: `<button class="epos-simerg-knopf">`.

### 1.1 Parameter auf einem Ergebnisreiter (W11b‑B‑28, W11b‑B‑29)

**Wortlaut des Anwenders (10.09.2026):** „bringe den Tab Parameter → Stromspeicher aus
Dialog ‚Detaillierte Simulation‘ in den Tab ‚Stromspeicher‘. Die Felder mit Parametern
sollen änderbar sein (und die Möglichkeit die geänderten Parameter zu Speichern)."

**Und sein Entscheid dazu (10.09.2026, nach der ersten Fassung):** „Sofort schreiben, wie
überall sonst im Programm."

* Der Block steht **unter der Kopfzeile und über den Kacheln**, und zwar **auch ohne
  Lauf**: Gerade vor dem ersten Lauf will der Anwender die Betriebsführung einstellen.
* Er nimmt dieselben Bausteine wie ein Parameterblatt: `epos-simerg-felder` mit
  `<Formularraster Einspaltig="true">`, `<Gruppenkopf>` je Abschnitt, Hinweisabsätze
  darunter.
* **Jedes Feld schreibt sofort** — wie überall sonst (§ 7). Die in W11b‑B‑28 benannte
  Ausnahme („er puffert und schreibt auf Knopfdruck") ist **zurückgenommen**: Der Puffer
  starb beim Reiterwechsel (`Reiterblatt` zeichnet nur das aktive Blatt), ein
  fehlgeschlagenes Schreiben meldete sich als Erfolg, und den Knopf am Blockende fand der
  Anwender nicht.
* **Am KOPF des Blocks**, vor den Feldern und in dieser Reihenfolge: der Zustand
  („Aktive Variante: …", Warnfarbe ohne Variante), die **Rückmeldung des letzten
  Schreibvorgangs** (Warnfarbe nur bei Fehlschlag), dann eine
  `<div class="epos-simerg-knopfzeile">` mit den Sprungknöpfen, die zu den Parametern
  gehören („Nach Auslegung optimieren"). Speichern- und Verwerfen-Knöpfe gibt es nicht.
* **Geprüft wird vor dem Schreiben**, je Feld die Regel, die zu ihm gehört. Ein Verstoß
  weist ab, und der Wert bleibt im Feld stehen: Das Zahlenfeld meldet jede Taste, und ein
  Zwischenzustand ist keine Fehleingabe.
* **Ohne Schreibdienst sind die Felder gesperrt.** Ein Eingabefeld, das nirgendwo ankommt,
  ist eine Attrappe.
* Ein **Gerätedatum** ist nur dann ein Eingabefeld, wenn es **eindeutig einem Gerät
  gehört**; sonst bleibt es gesperrt und trägt den Hinweis, woher es kommt.

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
Solarthermie, BHKW, Photovoltaik, Lastgang und Speicherbetrieb des Stromspeichers
(bis W11b‑B‑26 dessen blosser Ladezustand, siehe § 5.3).

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

### 5.2 Der Optimierungsdialog folgt denselben Regeln (W11b‑B‑25)

Die Auslegungsoptimierung (`EPOS.UI/Dialoge/Strom/SpeicherOptimierungDialog.razor`) ist kein
Reiter der Ergebnisseite, übernimmt seit dem Befund W11b‑B‑25 (Windows-Abnahme 09.09.2026:
„Grafik zu groß, Dialog übersichtlicher, Lastgang und Speicherung in einer Grafik") aber
dessen Sprache:

* **Kennzahlen** als `dl.epos-simerg-werte` je Gruppe, nebeneinander im
  `.epos-simerg-spalten` — nicht mehr als eine lange Tabelle. Eine Gruppe ohne Zeilen
  entfällt ganz (die Gruppe „Lastspitzenkappung" gibt es nur bei dieser Berechnungsart).
* **Schalter über dem Bild**, in der Reihenfolge aus § 5: erst die Darstellungsart
  („Ganzes Jahr" statt der Woche um die Jahresspitze), dann je Reihe ein Schalter, dann
  das `ChartBild` mit `BereichGewaehlt`/`Zurueckgesetzt` (Datenzoom, § 5.1).
* **Reihenwahl** nach derselben Regel: `null` = alle, leere Liste = keine.
* **Zwei Ausnahmen**, beide im Stilblatt und je begründet: Rasterkarte und Schnittkurve
  stehen NEBENEINANDER (`.epos-speicheropt-diagramme`) statt jede in einer eigenen
  `epos-simerg-diagrammzeile` — die Kurve ist der Schnitt durch die Karte, und
  untereinander sah der Anwender nie beide zugleich; und die Anzeigehöhe ist auf 320 px
  gedeckelt. Gedeckelt ist die ANZEIGE, nicht das PNG: Der Zeichner liefert weiter
  860 × 560 und 720 × 460, sonst wäre auch das Gezoomte grob.

### 5.2a Die Flottenansicht folgt seit #184 denselben Regeln (SD‑Q6, SD‑Q7)

Das Ergebnis der Speicherflotte (`EPOS.UI/Dialoge/Strom/SpeicherFlottenErgebnisAnsicht.razor`)
war bis zum 11.09.2026 der EINE Ort, der von diesem Blatt abwich: ein Bild ohne Reihenwahl,
ohne Dauerlinie und ohne Datenzoom, der Ausschnitt fest auf sieben Tage ab dem 1. Januar
(Befund 1.4 des Konzepts „Stromspeicher-Dialoge"). Mit Auftrag **#184** gilt hier § 1 bis § 6
unverändert: Kennzahlkacheln oben, Tabellen über die volle Breite, über JEDEM Bild dieselbe
Steuerzeile — „sortiert", je Reihe ein Schalter mit derselben Ressource wie die Legende, das
`ChartBild` mit `BereichGewaehlt`/`Zurueckgesetzt`. Der Ladezustand steht als **zweite Achse**
im Netzbild (§ 5.3), je Einheit wählbar; das zweite Bild mit den Energiegrenzen bleibt
daneben wählbar, weil dort das SoC-Band eine eigene Skala braucht.

**Zwei Dinge kommen über dieses Blatt hinaus**, beide aus Anwenderentscheiden vom 11.09.2026:
eine **Zeitraumwahl Jahr / Woche / Tag mit Navigator** (SD‑Q7, Muster W8‑E‑2) zusätzlich zum
Datenzoom — eine Jahresganglinie im Viertelstundenraster zeigt sonst keinen einzigen
Ladezyklus —, und eine **Δ-Spalte** in der Vergleichstabelle (SD‑Q6), deren Vorzeichen der
Kern bewertet (`FlottenVergleichszeile.NegativIstBesser`): Bei Kosten, Netzbezug und
Bezugsspitze ist weniger besser, bei der Einspeisung mehr. Die Farbe steht dabei neben dem
Vorzeichen und nicht an seiner Stelle.

### 5.3 Zwei Achsen in EINEM Bild (W11b‑B‑26)

Zwei Größen gehören in dasselbe Bild, wenn der Anwender sie **zusammen** lesen muss —
und auf **zwei Achsen**, wenn sie verschiedene Einheiten haben. Beides ist eine
Fachaussage, keine Geschmacksfrage:

| Fall | Achse |
|---|---|
| mehrere Größen **derselben** Einheit (vier Leistungen in kW) | **EINE** Achse. Eine zweite behauptete eine zweite Einheit, wo keine ist — und die Frage „um wie viel senkt der Speicher die Spitze" ist nur über einer gemeinsamen Skala zu beantworten |
| eine Größe **anderer** Einheit daneben (Ladezustand in kWh) | **zweite** Achse rechts. Auf der kW-Skala lägen bei 400 kWh Inhalt und 40 kW Bezug die Leistungen platt auf der Nulllinie |

Die zweite Achse fängt **unten bei null** an, trägt Zahlen und Beschriftung in der
**Farbe ihrer Reihe** und beschriftet die **Einheit**; ihre Null muss deshalb nicht auf
der Null der linken liegen, die vorzeichenfähig sein kann. Der Reihenname in der
**Legende nennt die Einheit ebenfalls** („Ladezustand [kWh]") — in einer Legende, in der
drei Nachbarn in kW stehen, stellt die Achsenbeschriftung allein die Zuordnung nicht her.

Zwei Renderer können das: `ChartRenderer.ErzeugerStapel` (B3, Parameter `zweiteAchse` /
`y2Titel` — linke Achse ab null) und `ChartRenderer.Speicherbetrieb` (B10, seit
W11b‑B‑26 — linke Achse **vorzeichenfähig**, für Reihen um die Nulllinie). Beide sortieren
im Zweig „sortiert" **jede Reihe für sich**, die der zweiten Achse eingeschlossen.

Wo das Bild eines Reiters eine zweite Größe aufnimmt, **entfällt deren eigenes Bild**:
Der Stromspeicher-Reiter zeigte bis dahin den Ladezustand allein; jetzt steht er in der
Grafik daneben und nicht mehr zweimal auf demselben Reiter.

## 6. Hinweise, Warnungen, Kacheln

* Leiser Hinweis: `<p class="epos-simerg-hinweis">`. Warnung mit Rolle: `<Warnbanner Stufe="…">`.
* Zustandszeile über einem Block: `<p class="epos-simerg-status">`, im Fehlerfall zusätzlich
  `epos-simerg-warn`.
* Kennzahlkacheln stehen im `<Kachelraster>`; ihre Zahlen folgen derselben Regel wie die
  Listen (`N2`, `N0`).

## 7. Was ausdrücklich NICHT vereinheitlicht ist

* **Jedes Feld schreibt sofort** — auf dem Reiter „Parameter" (wörtlich wie der
  Vorläufer: `SpeichereKonfigurationsAenderung`, `SpeichereVariantenAenderung`) UND im
  Speicherparameterblock des Ergebnisreiters (§ 1.1). Das ist seit W11b‑B‑29 **keine
  Regel mit Ausnahme mehr**, sondern schlicht die Regel: W11b‑B‑28 hatte den Block
  gepuffert, weil der Anwender „die Möglichkeit … zu Speichern" verlangt hatte — und
  bekam dafür Eingaben, die beim Reiterwechsel verschwanden. **Wer einen Puffer bauen
  will, hat es hier schon einmal schiefgehen sehen** und braucht einen Grund, der den
  Reiterwechsel und das Schließen der Seite übersteht.
* **Datenzoom** („Bereich"): an **jedem** Bild mit Zeitachse, aber an keinem ohne — die
  Streuwolke, die Monatssäulen, Kuchen und Ringe bleiben beim Bildzoom (§ 5.1). Die
  Zoomleiste selbst hat jedes Bild.
* **Zahlenformate in Tabellen** (`F1` für Jahresnutzungsgrad, Vollzyklen, Füllstand; `F4`
  für den Wechselrichter-Nutzungsgrad): Das sind Genauigkeiten des Vorbilds, keine
  Schreibweisen.
* **Gruppenbalken über Tabellen**: nur die Kesseltabelle trägt einen; die übrigen
  Modultabellen nennen sich in ihrer ersten Spaltenüberschrift.

## Speicherauslegung mit Kosten und Zeitreihen – 11.09.2026

Die Auslegungsoptimierung der aktiven Speicheranlage bietet jetzt kWh- oder kW-Suchbereiche, feste oder variable C-Raten, unabhängige Investitions- und Betriebskostenquellen sowie Last, PV und effektive Bezugspreise aus EPOS oder getrennten CSV-Dateien. Gespeicherte Profile enthalten auch die importierten Zeitreihen. Änderungen kennzeichnen alte Ergebnisse; die Übernahme eines veralteten Bestpunkts bleibt gesperrt.

Bedienung, Einheiten und Grenzen: [Doku_Speicherauslegung_Kosten_Zeitreihen.md](Doku_Speicherauslegung_Kosten_Zeitreihen.md). Nachweis: [Doku_Speicherauslegung_Pruefnachweis.md](Doku_Speicherauslegung_Pruefnachweis.md).
