# DG‑E5 — Jede Reihe eine Farbrolle (Protokoll, 20.09.2026)

Statuszeile #418 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_Diagramme_Interaktiv_EPOS-Plan.md`](../../../aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md)
§ 8 (DG‑Q8) und § 9 (Bedienung, Teil 3). Zweig `dg-e5-farbrollen`, Commits `d827f99c`, `d3c60d73`,
`c6b7b66d`, `0dac95b6`; Merge `609c82e7`.

## Anlass und Entscheid

Nach dem Nachzug #414 blieb die Meldung „der Klick auf das Farbfeld öffnet nicht immer den
Farbwähler" in den Simulationsreitern bestehen: Die Simulationshülle gab ihren Reihen sechzehn
feste `SKColor`-Konstanten aus dem WinForms-Vorbild (Rot, Blau, Gelb, Grün, Braun, Orange …). Eine
Rolle bekam eine Reihe nur, wenn ihre Farbe eine Hausfarbe der Palette war — keine der sechzehn
war eine; zwei trafen zufällig fremde Rollen (Blau die Profillinie, Sattelbraun Speicher 4). Der
Anwender entschied (DG‑Q8): Jede Reihe bekommt einen Wähler, auch die Prüfreihe „Sonstiges"; die
sichtbare Farbänderung der Reiter auf die Hausfarben des Berichts wurde erläutert und angenommen.

## Umsetzung

**Kern.** `ChartRenderer.Reihe` und die Punktreihe tragen einen `Farbton` (Rolle, wahlweise mit
gerechneter Farbe oder Deckung); die `Farbe` für die PNG-Wege entsteht aus der Palette. Die
Modellmethoden lesen den Ton der Reihe statt ihn aus der Farbe zurückzusuchen. Vierzehn neue
Rollen mit ihrer bisherigen Farbe als Vorgabe — Erzeuger und Bedarf: HEIZSTAB, HEIZWAERME,
WARMWASSER, PROZESSWAERME, STROM_BHKW, UEBERSCHUSS, ERZEUGUNG_GESAMT, VERBRAUCH_GESAMT; Speicher:
SPEICHERLADUNG, SPEICHERFUELLSTAND, STROM_SPEICHER, NETZ_OHNE_SPEICHER, NETZ_MIT_SPEICHER;
Profile und Temperaturen: SONNENWINKEL. Drei Vorgaben weichen von der bisherigen Farbe ab, weil
die Palette keine doppelten Farbwerte verträgt: STROM_BHKW `#A0522D` (Sattelbraun ist Speicher 4),
UEBERSCHUSS `#FFD54F` (Gelb war doppelt mit Heizstab), VERBRAUCH_GESAMT `#2E8B57` (Grün war doppelt
mit der Summe der Wärmeerzeugung). Die Palette führt 54 Rollen in sechs Gruppen; Administration ›
Einstellungen › Diagramme zeigt sie über `Diagrammfarben.Gruppen`.

**Hülle und übrige Stellen.** `SimulationErgebnisHuelle.Bilder.cs` nennt je Verwendungsstelle
die Rolle: Bedarfe (Wärme, Strom, Lastgang) → BEDARF, die drei Bedarfskanäle → HEIZWAERME /
WARMWASSER / PROZESSWAERME, Heizkessel → WAERME_KESSEL, BHKW-Wärme → WAERME_BHKW, Solar →
WAERME_SOLAR, Photovoltaik → STROM_PV, Wärmepumpe → WAERME_WP, Heizstab → HEIZSTAB, Restwärme und
Autarkielücke → REST, Speicherladung → SPEICHERLADUNG, Speicherfüllstand und Ladezustand →
SPEICHERFUELLSTAND, Eigenverbrauch aus Speicher → STROM_SPEICHER, Summe Wärmeerzeugung →
ERZEUGUNG_GESAMT, Summe Stromverbrauch → VERBRAUCH_GESAMT, BHKW-Strom → STROM_BHKW,
Speichertemperaturen → SPEICHER_1…6, Quelltemperatur → QUELLTEMPERATUR. Ebenso Bedarfsergebnis,
Quellprofil, Erdreichquelle, Lastspitzenkappung (NETZ_OHNE_SPEICHER, NETZ_MIT_SPEICHER,
SPEICHERFUELLSTAND), Speicherbetriebsbild, Flottenanzeige (Ladezustand) und die Windows-Hüllen
Klimadaten (AUSSENTEMPERATUR, SONNENWINKEL), Gebäude, Stromganglinie und Wärmebedarf extern
(BEDARF).

**Sichtbare Farbänderungen in den Reitern.** Bedarfs- und Lastganglinien werden dunkelgrau
(`#333333`), die Wärmepumpe blau (`#4172C4`), der Heizkessel grau (`#808080`), Solarthermie gelb
(`#FFC000`), BHKW-Wärme orange (`#ED7D31`), Photovoltaik grün (`#70AD47`), Restwärme hellgrau
(`#BFBFBF`); die Speichertemperaturen tragen die Speicherrollen. Unverändert bleiben Heizstab,
Warmwasserbedarf, Speicherladung, Speicherfüllstand, Summe Wärmeerzeugung, Lastspitzenkappung,
Sonnenwinkel und die Speicherreihen des Wärmegangs. Wer die alten Farben will, setzt sie in den
Einstellungen oder am Bild.

**Baustein und Prüfseite.** `DiagrammSvg.RolleDerReihe` öffnet den Wähler auch für eine im
Layout gerechnete Farbe mit Herkunftsrolle; nur eine unbenannte Rolle bleibt ohne (DS‑13). Die
dritte Reihe „Sonstiges" der Prüfseite trägt eine gerechnete Farbe mit Herkunftsrolle und öffnet
deren Wähler.

**Wache.** `DiagrammfarbenWacheTests` (EPOS.Kern.Tests): kein `SKColors.` und kein `new SKColor(`
in den Hüllen, Controllern, Windows-Views sowie in Lastspitzenkappungs- und Speicherbetriebsbild;
die Ausnahmen (Renderer, SkiaMaler, Palette, PNG-Wege) sind benannt.

## Offen

Die Ringfarben der Übersicht bleiben feste Werte (`SKColor.Parse`): Ringsegmente sind keine
Reihen, die Ringe entstehen ohne SVG-Legende, und der Rest-Grauton hängt am Stilblatt. Die
Flottenanzeige baut ihre übrigen Reihen aus den Hausfarben `ChartRenderer.C_*`; sie bekommen über
die Rückwärtssuche eine Rolle, bei den Serienfarben aber WAERME_BHKW/STROM_PV/WAERME_WP/STROM_NETZ
statt SERIE_1…4 — eine ausdrückliche Nennung wäre eine Verhaltensänderung und blieb aus.

## Abnahme

Worktree: Kern-Filter 0 Fehler, 10 052 Tests grün (1 übersprungen), ChartProben 106 Bilder /
0 Verstöße mit 91/91 byte-gleichen Hashes zur Basis (dreimal geprüft), Windows-Schale 0 Fehler,
Wirt 0 Fehler. Gate im Hauptbaum auf `609c82e7`: siehe Statuszeile #418.
