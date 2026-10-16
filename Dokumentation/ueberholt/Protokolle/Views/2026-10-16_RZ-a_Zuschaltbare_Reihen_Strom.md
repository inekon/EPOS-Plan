# RZ-a — Zuschaltbare Reihen in drei Strombildern

Anwenderentscheid 16.10.2026: „Kandidaten 1–5 als Welle RZ“. Teil a umfasst die Kandidaten 1 bis 3, Teil b
(Gebäude-Außentemperatur, Speicherauswahl und Quellprofil) folgt getrennt. Zweig `rza`, Basis `3bfe1b75b`.

## Regel

Jede neue Reihe ist **ab Werk aus**. Ohne Wahl entsteht derselbe Bildauftrag wie vorher, Bild, CSV und Bericht
bleiben byte-gleich. Kein Rechenweg und kein Schema wurden geändert, auch `ChartRenderer` und `DiagrammSvg` nicht.
Die Hülle liest nur Reihen, die der Lauf schon führt. Der Bericht (`ErgebnisbildPlan`) zeigt die Bilder ohne die
Schalter der Reiter (Muster „Produktion“) und bleibt deshalb unverändert.

## Stand

| Kandidat | Ort des Schalters | Reihenschlüssel | CSV |
|---|---|---|---|
| 1 Stromgang: Netzbezug und Einspeisung | Reiter *Ergebnis*, Blatt *Stromproduktion*, Serienauswahl (hinter allen Erzeugern) | `NETZBEZUG`, `NETZEINSPEISUNG` | `CsvStromgang` nimmt die gewählten Spalten mit |
| 2 Wärmepumpe: WP und Heizstab getrennt | Reiter *Wärmepumpe*, Unterblatt *Strom*, Schalter „Wärmepumpe und Heizstab getrennt“ | `WP_STROM`, `HEIZSTAB_STROM`, `WP_STROM_SUMME` | die Reiter-CSV führt beide Größen schon getrennt; „CSV…“ am Bild folgt dem Modell |
| 3 Peak-Shaving: Lade-/Entladeleistung und Schwelle | Maske *Lastspitzenkappung*, Reiter *Chart*, zwei Schalter unter „Ladezustand“ | `PeakShavingBildwahl` (Leistung, Schwelle) | `PeakShavingHuelle.CsvSpalten`: fünf Spalten, dahinter die gewählten |

- Netzreihen über `SimulationErgebnisCtrl.NetzreihenViertelstunde`. Der Netzbezug ist `Rest_Strombedarf_viertelstuendlich`.
  Die Einspeisung kommt mit Flotte aus der gesamten Netzeinspeisung der Flotte, sonst aus PV-Einspeisung plus
  BHKW-Einspeisung.
- Die Leistung der Lastspitzenkappung ist Energie je Intervall geteilt durch 8 760 h/Länge, die Schwelle ist
  `ErreichteSchwelleKw`.
- KI-Sicht der Lastspitzenkappung um `leistungsreihen` und `schwellenlinie` ergänzt. Der Hilfe-Assistent des
  Wärmepumpenreiters nennt den neuen Schalter, solange das Blatt *Strom* vorn steht.
- 14 Ressourcenschlüssel in beiden Sprachen, Designer neu geschrieben (18 830 Einträge, wiederholbar).

## Abnahme

- `WP-Plan.Kern.slnf` Release: 0 Fehler. Windows-Schale (`EnableWindowsTargeting=true`): 0 Fehler.
  `Rasterprobe.Wirt`: 0 Fehler.
- `EPOS.Kern.Tests`, gefiltert (ZuschaltbareReihen, PeakShaving, Stromgang, Waermepumpe, Bildauftrag, Ergebnisbild,
  Csv, Wache, KiDialog, KiMasken): 1 099/0.
- `EPOS.UI.Tests`, gefiltert (PeakShaving, Stromgang, Waermepumpe, GangUndErgebnis, Csv, Wache, Ki): 1 435/0.
  Eingabebilanz PeakShavingDialog 17 → 19, WaermepumpeReiter 9 → 10, KI-Felder Lastspitzenkappung 22 → 24.
- ChartProben: 257 Bilder, 0 Verstöße. Messlatte `Messlatte_2026-10-14.sha256`: 225/225 Zeilen unverändert in der
  Hashliste.
- Neue Tests: `ZuschaltbareReihenStromTests` (5 Fälle), `PeakShavingBildTests` (+3),
  `WaermepumpeReiterTests` (+1), `PeakShavingDialogTests` (+1).

## Offen

Teil b der Welle RZ. Wiki-Quellen (Simulation, Wärmepumpe, Stromspeicher) gehen mit dem Sammel-Upload hinaus. Den
Logbuch-Entwurf bekommt die Orchestrierung, die Version erfragt sie beim Anwender. Sichtprüfung unter Windows.

## Integration

Zweig `integ-dk`, 16.10.2026, zusammen mit `kd8a`, `mu`, `rza`, `rzb` und `origin` (Schritt 237, Basis R57). `KiMaskenabdeckungWacheTests`: `WaermepumpeReiter` zusammen mit RZ-b 11. EPOS.UI.Tests 9645 grün, `ZuschaltbareReihenStromTests` und `PeakShavingBildTests` grün (Kern-Teilmenge 616). Gate gegen R57: Referenzlauf 38 Projekte ohne Abweichung (13 025 956 Werte, CSV 1233 von 1233 byte-gleich), KiKern 553, SpeicherEngine 397, SpeicherPlanung 27, Dokumentationswachen 35, Werkzeugtests 250, ChartProben 257 Bilder 0 Verstöße (Messlatte unverändert), SQL-Dialektprüfer grün, Windows-Schale 0 Fehler.
