# EB-5 — Gate, Papiere, Push der Welle EB „Ergebnisreihen“

Sitzung Dialoge und Korrekturen (Übersicht Berichte & Kosten), 17.10.2026, Zweig `eb2`. Vorgänger:
[EB-1](2026-10-17_EB-1_Ergebnisreihen_Kern.md), [EB-2](2026-10-17_EB-2_Ergebnisreihen_Oberflaeche.md),
[EB-4](2026-10-17_EB-4_Standby_und_R58.md). Kurzkonzept (umgesetzt, nach `ueberholt/` verschoben):
[`2026-10-17_Konzept_Ergebnisreihen_Einspeisung_Standby_Abregelung.md`](../../2026-10-17_Konzept_Ergebnisreihen_Einspeisung_Standby_Abregelung.md).

## Merge mit origin

Zweimal: zuerst `592ea4515` (Satzzeile, Testschutz der Oberflächentests; Fast-Forward ohne Konflikt), danach
`e77ce63e4` (Wiki-Quellen BK-5/BK-6/DA-6, Statuskopf mit Schemaschritten 240 bis 242, Übergabepapiere). Seit `93eb06f0b`
hat origin weder `Referenzlaeufe/` noch `EPOS.Kern/Allgemein/Update/` geändert. Einziger Konflikt: das Übergabepapier
`Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md` (Tabellenkopf); die Fassung von origin gilt,
Referenzbasis (R59) und Schemastand der Testdatenbank (239) sind darin nachgeführt.

## Gate (Stand `3330961e2`, danach nur Papiere gemergt)

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler (180 Warnungen, Bestand); Windows-Schale auf Linux
  (`-p:EnableWindowsTargeting=true`): 0 Fehler (21 Warnungen).
- `Werkzeuge/Gate/gate_rest_linux.sh`: `EPOS.UI.Tests` 9 720, `KiKern.Tests` 553, `SpeicherEngine.Tests` 397,
  `SpeicherPlanung.Tests` 27 (1 übersprungen), Dokumentationswachen 35, Designer-Prüfung 19 162 Einträge wiederholbar,
  SqlDialektPruefer 3 058 Texte ohne Fundstelle, Werkzeugtests Formularkarte 124, Auslieferungsvorlage 63,
  Gebäudevergleich 24, Zapfprofilvalidierung 39; keine Konfliktmarker, kein BOM in Markdown.
- Referenzlauf gegen R59: 38 von 38, GESAMT PASS (13 025 990 Werte), CSV byte-gleich 1 233 von 1 233.
- `EPOS.Kern.Tests` vollständig: 14 784 bestanden, 7 übersprungen, 0 Fehler (1 h 32 min).
- ChartProben: 277 Bilder, 0 Verstöße. Rasterprobe: `kopfleistenprobe.mjs` und `diagrammprobe.mjs` (Fälle `fluss`,
  `hervorhebung`, `wahl`) erfüllt, Gegenproben rot wie verlangt.

## Logbuch-Entwurf (Version beim Anwender erfragen)

- Im Simulationsergebnis lassen sich im Blatt Stromproduktion der eingespeiste BHKW-Strom und die PV-Abregelung als Linien zuschalten.
- Das Betriebsbild des Stromspeichers zeigt auf Wunsch den Standby-Verbrauch des Speichersystems.
- Die Übersicht „Ergebnisse – Energie“ und die Kennzahlentafel des Berichts führen Einspeisung BHKW, Abregelung PV und Standby Speichersystem.
- Das Energieflussbild weist Einspeisung, Standby und Abregelung als eigene Flüsse aus statt als „nicht zugeordnet“.
- Die Speicher-CSV enthält die Spalte „Standby Speichersystem [kWh]“.

## Offen

- Die Vorbelegung der Speicherflotte liest eine aktivierte Projektflotte ohne Stand `@Aktuell` nicht (Projekt 1046:
  Standby über die Oberfläche 0, im Referenzlauf 438 kWh/a); Befund im EB-4-Protokoll.
- Sichtprüfung unter Windows; Wiki-Upload mit dem Sammel-Upload.
