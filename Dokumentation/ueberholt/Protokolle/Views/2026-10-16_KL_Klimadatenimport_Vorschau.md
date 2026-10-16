# KL — Klimadatenimport: Vorschau, Schlussleiste, Vorschaukarte

Sitzung Dialoge und Korrekturen (Übersicht Berichte & Kosten), 16.10.2026, Zweig `klima`. Konzept:
[`2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md`](../../2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md);
Mockup: [`Klimadatenimport_Mockup.html`](../../Mockups/Klimadatenimport_Mockup.html).

## Befund (KL-A)

Anwenderbefund 16.10.2026: Stuttgart-Vaihingen landet in Region 14 Stötten (57 km, 284 m Höhenunterschied). Die Regionswahl nimmt die nächste von 15 Repräsentanzstationen des Pakets (Haversine) und rechnet damit richtig; sie trifft weder die DWD-Klimaregion noch den ortsgenauen Datensatz. Es fehlten Trefferanzeige und Koordinatenfelder, die Schlussleiste der Import-Überlagerung war abgeschnitten. Entscheide KD-Q1 bis KD-Q9 am 16.10.2026 nach Empfehlung.

## Stand

| Welle | Statuszeile | Inhalt | Nachweis |
|---|---|---|---|
| KL-A | #1121 | Befund und Konzept Klimadatenimport, Mockup, Entscheide KD-Q1–KD-Q9 | Dokumentation |
| KD-1 | #1122 | Schlussleiste der sechs Einlesen-Überlagerungen haftet; Fenstermaß Klimadaten über `Fenstermass.MitUeberlagerung` | `FensterrahmenTests`, `FenstermassTests`, `fensterprobe.mjs` (Fall `klimaimport`) |
| KD-2 | #1123 | Vorschau liest den Klimadatensatz im Kern (alle drei Quellen); Import übernimmt den Vorschausatz ohne zweiten Abruf | Kern-Tests, Referenzlauf der 18 CI-Projekte |
| KD-3 | #1124 | Vorschaukarte im Importdialog; Fenstermaß 1000 px | bUnit, `fensterprobe.mjs` |

## Gate

Referenzlauf 37/37 PASS gegen R55 (12 736 541 Werte in Toleranz), CSV byte-gleich 1 204/1 205; byte-verschieden nur
`Projekt_1072/aggregate.csv` (zwei Kälteschlüssel einer anderen Sitzung, R56). EPOS.UI.Tests 9 606/0, KiKern 553/0,
SpeicherEngine 397/0, SpeicherPlanung 27/0 (1 übersprungen), gefilterte Kern-Tests 1 143/0, Kern-Wächter 35/0,
SQL-Prüfer 0 Fundstellen, Designer-Prüfung wiederholbar, Windows-Schale und Kern-Mappe 0 Fehler, Werkzeugtests grün.

## Offen

KD-4 bis KD-7 des Konzepts (Rohdatentabelle, CSV-Export); Wiki-Upload der Quellen mit dem Sammel-Upload, Version beim Anwender; Sichtprüfung unter Windows.
