# Protokoll R — Prüfpunkte im Körpervergleich, Dach erbt U-Wert und Aufbau (08.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#828**. Commits: `29b4b9b7f`, `3fa2350d7`, `0b99c7dda`, Merge `046d05996`.
**Anlass:** Prüfpunkte #808 (a) und #801 (c) aus den Nach-Blöcken der Statusdatei.

## 1 Anlass

#808 (a): In einer Anwenderdatei (A) wichen drei Böden/Decken um ein Vielfaches vom Körper ab. #801 (c): Der Codeweg „Dach mit Mengensatz und Körper nur in den Platten" war nie geprüft. Kein Rechenweg, kein Schemaschritt, Basis unverändert.

## 2 Gebaut

- `29b4b9b7f` (#808 a): Das CAD-Programm benennt den Körper einer Geschossplatte nach dem Geschosskürzel, die Teile ohne Darstellung je Raum aber nach einem anderen Kürzel. Die Gruppierung über den Namensstamm fand sie nicht, der Körper der ganzen Platte wurde nur gegen den eigenen Mengensatz verglichen; die Warnung war falsch. Mit den Teilen deckt der Mengensatz den Körper auf 0,01 % bzw. 0,2 %. Neue Methode `FremdnamigeTeile`: Hat ein Körper mit Mengensatz keine gleichnamigen Teile und ist er um mehr als 2 % größer, wird er gegen die passende fremdnamige Teilgruppe verglichen. Bedingungen: selbes Gebäude, selbe Klasse und Bauteilart, Stamm ohne eigenen Körper, nicht vergeben, zusammen auf 2 % deckend, eindeutig. Geändert ist nur der Vergleich, die Rechnung nahm schon die richtigen Mengensätze.
- #801 (c), keine Codeänderung: Neue Probe mit Satteldach (Mengensatz am Dach, zwei Platten mit Körper). Fläche aus dem Mengensatz, Neigung 36,87°, je Dachfläche eine Zeile mit Azimut 180°/0°, Körpervergleich ohne Abweichung, Flächen je Raum aus den Körpern.
- `3fa2350d7` (Nebenbefund 1): Ein zerlegtes Dach ohne eigenen U-Wert erbt den nach Fläche gewichteten U-Wert seiner Platten (nur wenn alle einen tragen); ohne eigenen Aufbau erbt es den Aufbau, wenn alle Platten denselben tragen. Infos `DACH_UWERT_PLATTEN`, `DACH_UWERT_SPANNE` (Spanne > 10 %), `DACH_UWERT_EIGEN`, `DACH_AUFBAU_PLATTEN`. Vorher lehnte der Vorschlag ein solches Dach mit `UWERT_FEHLT` ab; in den sechs Anwenderdateien entsteht keine solche Meldung.
- `0b99c7dda` (Nebenbefund 2): In Anwenderdatei B legte das CAD-Programm fünf Teilstücke einer Bodenplatte als andere IFC-Klasse ab; die Warnung `KOERPER_ABWEICHUNG_TEILE` (215,7 %) war falsch. Neue Methode `FremdklassigeTeile`: Bleiben die gleichnamigen Teile um mehr als 2 % unter dem Körper, zählen gleichnamige Teile anderer Klassen dazu (ohne Darstellung, ihr Stamm ohne eigenen Körper, nicht vergeben, zusammen höchstens 2 % über dem Körper). Die Restabweichung 3,3 % steht nur in der Sammelmeldung.

## 3 Prüfung

- Tests: `IfcKoerperPruefpunkteTests` (8) mit Proben in `IfcProbenErzeuger.Pruefpunkte.cs`.
- Unverändert: die Sollwerte der Anwenderdateien, die Referenzprojekte.

## 4 Gate 828

auf `046d05996` (79 min): KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 843, EPOS.Kern 12 383 mit 7 übersprungen, 0 rot; Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 26/26 gegen `2026-10-08_R44_Kuehlkurve` PASS (8 932 241 Werte), 841/841 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 527 Texte, 0 Fundstellen; Windows-Schale 0 Fehler; Auslieferungsvorlage-Tests 61/61.

## 5 Offen

Keiner.
