# EB-2 — Ergebnisreihen in der Oberfläche: Stromgang, Speicherbild, Übersicht, Katalogfassung 26

Sitzung Dialoge und Korrekturen, 17.10.2026, Zweig `eb2` auf `eb1` (`def464959`). Konzept:
[`2026-10-17_Konzept_Ergebnisreihen_Einspeisung_Standby_Abregelung.md`](../../2026-10-17_Konzept_Ergebnisreihen_Einspeisung_Standby_Abregelung.md),
Abschnitt 5; Anwenderentscheide 17.10.2026 (1) Einspeisung BHKW als eigene Reihe (Name nach EB-Q4 „Stromeinspeisung BHKW“), (2) Standby des
Stromspeichers, (3) Abregelung in den Zeitreihen-Grafiken zuschaltbar, Vorgabe aus; EB-Q2 und EB-Q3 nach Empfehlung.
Nicht gepusht.

## Befund gegen den Bestand

- Die Ergebnistabellen der Reiter führten die drei Größen schon: BHKW „Stromeinspeisung“ [MWh/a] (E29), PV „Abregelung“
  samt Anteil (PV3, nur mit Wert), Stromspeicher „Eigenverbrauch Speichersystem“ und „davon aus dem Netz“ [kWh/a] (SP1,
  nur mit Standby). Keine Änderung nötig; `epos-simerg-*`-Regeln nicht berührt, `tabellenprobe.mjs` deshalb nicht gezogen.
- Der BHKW-Reiter bot die Reihe „Stromeinspeisung“ im Strombild schon wählbar an. Sie heißt nicht „eingespeister Strom“:
  Dieselbe Ressource beschriftet das BHKW-Strombild des Berichts — ein Umbenennen änderte Bericht und Bild. Offen für die
  Orchestrierung, ob umbenannt werden soll.
- Die Bericht-Bausteine führen keine Einspeisezeile aus der Strommatrix außer der Strommatrix-Tafel der Wirtschaftlichkeit
  (bleibt, sie ist die Matrix). Naheliegend fehlte die Zeile in der Kennzahlentafel; sie kommt über den Kennzahlenkatalog.

## Umsetzung

| Größe | Tabelle (Reiter) | Grafik | Übersicht „Ergebnisse – Energie“ | KI / Bericht |
|---|---|---|---|---|
| Einspeisung BHKW | Bestand (BHKW-Reiter) | Stromgang: „Stromeinspeisung BHKW“ (EB-Q4), `BHKW_EINSPEISUNG`, gestrichelt in BHKW-Farbe, ab Werk aus; BHKW-Reiter: Bestand | `energie.bhkw_einspeisung` [MWh/a, N1], Gruppe Strom; Ergebniszeile, Rückfall Strommatrix, sonst „—“ (EB-Q3) | Tabelle der KI-Sicht der Übersicht; Kennzahlentafel des Berichts |
| Abregelung PV | Bestand (PV-Reiter) | Stromgang: „PV-Abregelung“, `PV_ABREGELUNG`, nur mit Abregelung wählbar, ab Werk aus; PV-Reiter: Bestand (Schicht) | `energie.pv_abregelung` [MWh/a, N1], nur > 0 | wie oben |
| Standby Speichersystem | Bestand (Stromspeicher-Reiter, kWh/a) | Betriebsbild Stromspeicher: „Standby Speichersystem“, nur mit Standby, ab Werk aus | `energie.speicher_standby` [kWh/a, N0], nur > 0 | wie oben |

- Kern: `SimulationErgebnisCtrl.Netzreihen` um `BhkwEinspeisungKw` (Flotte: `BhkwNetzeinspeisungKw`, sonst
  `BhkwEinspeisung_viertelstuendlich`) und `PvAbregelungKw` (Flotte: `PvAbregelungKw`, sonst `PvEinspeisungAufteilen`)
  ergänzt — keine neue Rechnung, die Summen treffen `BhkwEinspeisungMwh` und `PvAbregelungMwh`.
  `ZeitreihenExtraktor.SpeichersystemEigenverbrauchKw` intern statt privat (eine Reihe für Satz und Bild).
- Die Einspeisung BHKW ist ein Anteil der Netzeinspeisung, keine zweite Einspeisung: Legende „(BHKW-Anteil)“, Strichart
  gestrichelt; die Abregelung steckt nicht in der Netzeinspeisung.
- Hülle: `REIHE_BHKW_EINSPEISUNG`, `REIHE_PV_ABREGELUNG` in Bild, Reihenliste und `CsvStromgang`;
  `REIHE_SPEICHER_STANDBY` im Betriebsbild, `SpeicherErgebnisDaten.MitStandby`. Reiter: der Standby läuft in der
  vorhandenen Schalterschleife mit eigener Zuwahl (`_zugeschaltet`), die übrigen Reihen behalten die Abwahl —
  `KiMaskenabdeckungWacheTests` (StromspeicherReiter 2, StromgangReiter 1) unverändert.
- EB-Q2: Der Kennzahlenblock des Stromspeichers zeigt alle Energiezeilen in kWh/a; die Standby-Zeile folgt ihren Nachbarn
  (Anzeigeeinheit des Blocks), die Übersicht ebenfalls kWh/a — „genau einmal umgerechnet“ heißt hier: gar nicht.
- EB-Q3: `EnergievergleichCtrl.BhkwEinspeisungRueckfall` liest bei `StromeinspeisungMwh` = NULL die KWK-Einspeisung der
  gespeicherten Strommatrix in `VariantenDaten.BhkwEinspeisungStrommatrixMwh`; ohne Matrix bleibt die Kennzahl leer („—“).
- Katalogfassung 26 (`FASSUNG_ERGEBNISREIHEN`): 3 Kennzahlen × 15 erzeugte Einträge, Messlatte
  `Vorlagenfeldkatalog_v26.txt`; zehn Berichtsvorlagen mit `Werkzeuge/Berichtsvorlage alle` neu gebaut (OpenXmlValidator
  0 Fehler). Der Bericht ändert sich damit um die neuen Zeilen der Kennzahlentafel (BHKW-Projekte: Einspeisung BHKW).
- Ressourcen `CHART_LEGENDE_BHKW_EINSPEISUNG`, `CHART_LEGENDE_SPEICHER_STANDBY` (de/en), Designer neu geschrieben.
- CSV: Stromgang folgt der Wahl; die Reiter-CSV des Stromspeichers hängt mit Standby die Spalte „Standby Speichersystem
  [kWh]“ als letzte an (EB-Q5), die bisherigen Spalten bleiben unverändert.
- Wiki-Quellen: Simulation (Blatt Stromproduktion), Stromspeicher (Betriebsbild), Varianten (Gruppe Strom); BHKW und
  Photovoltaik unverändert (die Reiter änderten sich nicht). Gegenlesemuster 0 Treffer.

## Abnahme

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale (`EnableWindowsTargeting=true`, Release): 0 Fehler.
- `EPOS.UI.Tests` vollständig: 9 693/9 693 grün.
- `EPOS.Kern.Tests` gefiltert (`Ergebnis|Strom|BHKW|Bhkw|PV|Photovoltaik|Speicher|Uebersicht|Energievergleich|Kennzahl|Csv|Ki|Wache`):
  3 444/3 444 grün.
- ChartProben: 276 Bilder, 0 Verstöße; Messlatte `Messlatte_2026-10-16.sha256` 248/248 Zeilen unverändert in der Hashliste.
- Neue Tests: `ErgebnisreihenOberflaecheTests` (7: Stromgang ab Werk aus und CSV, Reihen = Jahreswerte, Abregelung mit
  Grenze, Gruppe Strom und Fassung 26, Rückfall Einspeisung, Abregelung/Standby nur > 0, Ladeweg der Übersicht mit
  Altergebnis), `StromspeicherReiterTests.Der_Standby_ist_zuschaltbar_und_ab_Werk_aus`. Angepasst:
  `ZuschaltbareReihenStromTests` (Netzreihen nicht mehr zuletzt), `VorlagenfeldkatalogWacheTests` (26),
  `BerichtEnergieflussTests` (Word-Fassung ≥ 25).

## Logbuch-Entwurf (Version beim Anwender erfragen)

- Im Simulationsergebnis lassen sich im Blatt Stromproduktion der eingespeiste BHKW-Strom und die PV-Abregelung als Linien zuschalten.
- Das Betriebsbild des Stromspeichers zeigt auf Wunsch den Standby-Verbrauch des Speichersystems.
- Die Übersicht „Ergebnisse – Energie“ und die Kennzahlentafel des Berichts führen Einspeisung BHKW, Abregelung PV und Standby Speichersystem.

## Offen

- Sichtprüfung unter Windows; Wiki-Upload mit dem Sammel-Upload.
- Erledigt mit dem Nachzug (Anwenderentscheide 17.10.2026, EB-Q4/EB-Q5): Name überall „Stromeinspeisung“, im Blatt
  Stromproduktion „Stromeinspeisung BHKW“ (BHKW-Reiter und Bericht unverändert); Reiter-CSV des Stromspeichers mit
  Standby als letzter Spalte, Test `Csv_Stromspeicher_haengt_den_Standby_als_letzte_Spalte_an`.
- Befund beim Nachzug: Der Lauf der Oberfläche (`SimulationErgebnisHuelle`, `SpeicherflotteAusLaufVorbelegen`) rechnet
  den Speicher über die vorbelegte Flotte; dort ist der Standby 0 (1007 mit 20 W Standby: Lauf der Oberfläche 0 kWh/a,
  `SimulationRunner` 175,2 kWh/a; 1046: 0 gegen 438 kWh/a). Standby-Reihe im Betriebsbild, CSV-Spalte und Kennzahl
  erscheinen deshalb nach einem Lauf aus der Oberfläche nicht. Der Referenzlauf rechnet über `SimulationRunner` und ist
  nicht betroffen. Eigener Auftrag (Kern, Flottenvorbelegung).
