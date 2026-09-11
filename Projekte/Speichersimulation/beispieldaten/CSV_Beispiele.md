# Synthetische CSV-Beispiele für EPOS Plan

Diese Dateien enthalten künstliche Testdaten, keine Messwerte und keine historischen Marktprognosen.

- `Jahresdaten_synthetisch_2026.csv`: vollständiges Kalenderjahr mit 8.760 Stunden; Bruttolast, verfügbare PV und effektiver Bezugspreis, PV-Vergütung. Der Import erzeugt 35.040 Viertelstunden.
- `Prognosen_synthetisch_2026.csv`: 365 künstliche Prognose-Snapshots mit jeweils bis zu 48 Stunden. Last und PV weichen bewusst von den Jahresdaten ab. Die Bekanntheitszeitpunkte sind Teil des synthetischen Beispiels.

Format: UTF-8 mit BOM, Semikolon, Dezimalkomma, Zeitstempel in UTC, Intervallanfang, Leistungswerte in kW und Preise in €/kWh. Die Kalendergrenzen entsprechen dem vollständigen Jahr in Europe/Berlin. Im Import die vorbelegten Spalten und Einheiten anhand der Vorschau prüfen.

Für eine Bewertung nur dieses Jahres Projektlaufzeit 1 wählen. Für eine längere gleichbleibende Projektion das Referenzjahr wiederholen. Die Prognose-Datei dient dem getrennten Test der planenden Strategien; sie ist kein Nachweis realer Vorhersagegüte.
