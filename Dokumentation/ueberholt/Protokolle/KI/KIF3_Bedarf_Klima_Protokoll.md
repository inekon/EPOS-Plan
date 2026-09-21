# KI‑F3 — Bedarf und Klima für den Hilfe-Assistenten (Protokoll, 21.09.2026)

Statuszeile #421 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Stufe S4, KI‑D‑Q5 und KI‑D‑Q6); Vorgänger [`KIF1_Erzeugermasken_Protokoll.md`](KIF1_Erzeugermasken_Protokoll.md),
[`KIF2_Simulationskonfiguration_Protokoll.md`](KIF2_Simulationskonfiguration_Protokoll.md),
[`KIF1b_Wahlfelder_Protokoll.md`](KIF1b_Wahlfelder_Protokoll.md).
Zweig `ki-f3`, Commits `5f42a228`, `370cbb21`, `1ff0bc7c`, `8b3a0c8b`; Merge `2c00805c`.

## Die Masken

| Katalogschlüssel | Dialog | Bindung | Felder (nur lesbar / Wahl) | Knöpfe | Ziel |
|---|---|---|---|---|---|
| `Form_Gebaeude` | Gebäude (Projekt und Verwaltung) | `GebaeudeKiSicht` | 10 (6 / 3) | OK, Abbrechen | `Masken.GebaeudeAdmin` |
| `Form_GebWohnflaeche` | Wohnfläche | `GebaeudeWohnflaecheKiSicht` | 9 (5 / 1) | OK, Abbrechen | Startseite |
| `Form_Gebaeude1` | Gebäudekatalog | `GebaeudeKatalogKiSicht` | 37 (1 / 5) | Werte übernehmen, Überschreiben, Speichern, Beenden | `Masken.GebaeudeAdmin` |
| `Form_Gebaeude_Bedarf` | Gebäudebedarf | `GebaeudeBedarfKiSicht` | 6 (4 / 1) | OK | Startseite |
| `Form_EingGebTyp` | Gebäudetyp | `GebaeudetypKiSicht` | 3 (1 / 2) | Speichern, Beenden | `Masken.GebaeudetypenAdmin` |
| `Form_EingStromTyp` | Typprofil | `TypProfilKiSicht` | 3 (0 / 2) | Speichern, Beenden | `Masken.StromverbraucherAdmin` |
| `Form_EingDBStromverbraucher` | Typstamm | `TypStammDaten` (Daten-Objekt) | 3 (1 / 1) | Überschreiben, Speichern, Beenden | `Masken.StromverbraucherAdmin` |
| `Form_Prozesswaerme` | Bedarfsprofile | `BedarfsProfileKiSicht` | 8 (6 / 1) | Übernehmen, OK, Abbrechen | Startseite |
| `Form_Prozesswaerme_Admin`, `Form_Stromverbraucher_Admin`, `Form_Brauchwasser_Admin` | Bedarfsverwaltung (drei Ausprägungen) | `BedarfAdminKiSicht` | je 5 (4 / 1) | Beenden | je `Masken.*Admin` |
| `Form_ErgStromverbraucher` | Bedarfsergebnis | `BedarfErgebnisKiSicht` | 4 (0 / 3) | OK | `Masken.Simulation` |
| `Form_Waermebedarf` | Wärmebedarf extern | `WaermebedarfExternZeile` (Daten-Objekt) | 2 (1 / 1) | OK, Abbrechen | Startseite |
| `Form_Solarganglinie` | Solarganglinie | `SolarganglinieKiSicht` | 3 (2 / 1) | OK, Abbrechen | Startseite |
| `Form_Klimadaten` | Klimadaten | `KlimadatenKiSicht` | 9 (2 / 3) | Beenden | `KiMaskenziele.KLIMADATEN` |

Katalog 19 → 34 Masken, 218 → 330 Felder, davon 27 neue Wahl-Felder. Haken: Auffrischen
überall; Prüfen und Speichern bei Gebäudekatalog und Typstamm; Schreibschutz und Speichern bei
Gebäudetyp und Typprofil; Speichern („Übernehmen") bei den Bedarfsprofilen.

## Drei Entscheidungen

`Form_Gebaeude` ist eine Maske: `Masken.GebaeudeAdmin` öffnet dieselbe Razor-Komponente in der
Betriebsart Verwaltung, der Modus steht als nur lesbares Feld `verwaltung` — zwei Einträge hätten
zwei Wahrheiten über eine gezeichnete Maske geführt. Die Bedarfsverwaltung trägt drei
Katalogschlüssel bei einem Feldsatz: Jede Ausprägung hat ihren Menüweg und lässt sich einzeln
öffnen; die Ausprägung steht zusätzlich als Feld `bedarfsart`. `WaermebedarfAdminDialog` und
`SolarganglinieAdminDialog` bleiben draußen (Suche, Auswahl, Importpfad aus dem Dateidialog —
keine Einstellwerte, Ausnahme nach KI‑D‑Q5); `StromganglinieAdminDialog` trägt dagegen die
Klappliste „Zeitintervall" und gehört in die Welle Strom (offen, Statusdatei Nach #421).

## Sichtklassen

Dreizehn Masken binden über eine Sichtklasse (in `OhneMarkupprobe` mit Begründung, Ersatz je ein
bunit-Zeuge): Filter- und Anzeigeschalter liegen in privaten Feldern (Gebäude, Gebäudebedarf,
Bedarfsergebnis, Bedarfsprofile, Klimadaten), der Ergebnisrecord entsteht erst beim OK
(Wohnfläche), der Name ist eine Wahl, die einen anderen Satz lädt (Gebäudetyp, Typprofil). Der
Gebäudekatalog legt zwei Stände zusammen (erstes Reiterblatt am Satz, zweites im eigenen
Arbeitsstand); sein Speicherweg nimmt „Werte übernehmen" mit. Typstamm und Wärmebedarf extern
melden ihr veränderliches Daten-Objekt an und tragen die Markup-Probe.

## Bewusst nicht deklariert

Raster mit eigenem Editor (24 Stundenwerte je Tageskurve, 7 × 24 Wochenwerte, 12 Monatswerte des
Bedarfskopfes, Katalog- und Zuordnungslisten samt Filtern); Zahlenfolgen ohne Zeilentyp (16
Ferienzahlen des Gebäudekatalogs, 12 Monatssummen des Gebäudebedarfs, Kennzahlen und Monatswerte
der Ergebnisanzeige); Ladevorgänge (CSV-Import der Ganglinien, „Daten einlesen" der Klimadaten als
rechnender Weg fürs Aktionsregister); die gerechnete Bauweise; Reiter und Löschknöpfe. Die zwei
Dateipfade der Klimadaten sind nur lesbar — gesetzt werden sie über den Dateidialog der Plattform.

## Zahlen und Abnahme

122 neue Ressourcenschlüssel je Sprache, drei Einheitenzeichen (`W`, `W/(m²·K)`, `1/h`), 26 neue
Tests (45 Läufe). Worktree: Kern-Filter 0 Fehler, 10 203 Tests grün (1 übersprungen). Gate im
Hauptbaum auf `2c00805c`: siehe Statuszeile #421.
