# KI‑F2 — Simulationskonfiguration für den Hilfe-Assistenten (Protokoll, 20.09.2026)

Statuszeile #419 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Stufe S4, KI‑D‑Q5); Vorgängerwelle [`KIF1_Erzeugermasken_Protokoll.md`](KIF1_Erzeugermasken_Protokoll.md).
Zweig `ki-f2`, Commits `cd27d02d`, `bc6fb749`, `d4a86d4e`, `17cd8bd7`, `c17ee042`.

## Die Masken

| Katalogschlüssel | Dialog | Sichtklasse | Felder | Knöpfe | Haken |
|---|---|---|---|---|---|
| `Form_PufferSp_Projekt` | Pufferspeicher im Projekt | `PufferSpProjektKiSicht` | 16 | Übernehmen, OK, Abbrechen | Auffrischen, Speichern („Anlegen"/„Übernehmen", schließt nicht) |
| `Form_QuelleErdreich` | Erdreichquelle | `QuelleErdreichKiSicht` | 7 | OK, Abbrechen | Auffrischen |
| `Form_QuellePufferspeicher` | Pufferspeicherquelle | `QuellePufferspeicherKiSicht` | 8 | OK, Abbrechen | Auffrischen |
| `Form_Quellprofil` | Quellprofil | `QuellprofilKiSicht` | 3 | OK, Abbrechen | Auffrischen |
| `Form_Waermesenke` | Wärmesenke | `WaermesenkeKiSicht` | 8 (gewählte Zeile) | OK, Abbrechen | Auffrischen |
| `KomponentenKonfiguration` | Komponentenkonfiguration | `KomponentenKonfigurationKiSicht` | 10 | OK, Abbrechen | Auffrischen, Schreibschutz |

Navigationsziel aller sechs: `Masken.Simulation` — die Dialoge gehen aus Schritt ① der Ansicht
Simulation auf und brauchen eine gewählte Komponente; `dialog_oeffnen` öffnet die Ansicht, der
Anwender wählt die Komponente.

## Sichtklassen statt Datenobjekte

Die Daten-Records dieser Dialoge (`QuelleErdreichDaten`, `QuellePufferspeicherDaten`,
`QuellprofilDaten`, `WaermesenkeDaten`) sind unveränderlich: Der Dialog liest sie beim Aufbau in
seine Eingabefelder und erzeugt beim OK einen neuen Satz; `PufferSpProjektDialog` bekommt nur zwei
Ids und baut seine Eingaben erst beim „Übernehmen". Ein daran angemeldeter Katalog hätte dem
Modell den Stand von vorhin gezeigt und in ein Objekt geschrieben, das niemand liest. Je Maske
legt sich deshalb eine Sichtklasse (Muster `SimulationKiSicht`) über die lebenden Eingabefelder;
Setzen nimmt den Weg der Tastatur mit Nachziehen von Kennwertzeile, Vorschau, Prüfung und
Vorbelegungen. Die sechs Masken stehen in `OhneMarkupprobe` mit Begründung; ihr Ersatz ist je ein
bunit-Zeuge in der Testklasse des Dialogs, der die gezeichnete Maske an der Brücke liest und
setzt. `Speichern` gibt es nur beim Pufferspeicher im Projekt; die übrigen schreiben im OK-Weg,
der die Maske schließt — das löst der Assistent nicht aus, `dialog_speichern` lehnt benannt ab.
Die Komponentenkonfiguration legt zwei Arbeitskopien zusammen (projektweite Parameter und
Wärmepumpenanlage); die sieben Wärmepumpen-Konfigurationsfelder stehen damit auch unter
`Form_WP_Anlage`, mit denselben Texten.

## Ansicht „Simulation"

`SimulationKiSicht` führt 38 statt 17 Felder, setzbar 24 statt 4: der Lesepunkt „davor" aus
Schritt ① und die Einstellwerte des Reiters „Stromspeicher" (Ladezustand min/max, Ladeleistung,
Kapazität, Ladeschwelle, Betriebsart, Berechnungsart, Peak-Ziel samt adaptiv, Kompatibilität,
Laden aus PV/BHKW, Netzentladung, BHKW stromgeführt als nur lesbar, Kapitalzins, Nutzungsdauer,
Leistungspreis, Netzladeaufschlag, Preisquelle, Aufschlag). Jedes Feld schreibt sofort über den
Schreibdienst des Reiters; ein Steuerwert, den die Klappliste nicht führt, wird abgewiesen.

## Bewusst nicht deklariert

Verweise in kontextabhängige Listen (Bodentyp, gewählter Puffer, gewähltes Profil, gewählter
Speicher, Parallelverbund, Katalogsatz, Preisreihe, Energieträger), Mehrfachwahlen und
Zahlenfolgen ohne Zeilentyp (Nutzung, Parallelverbund, die Monats- und Stundenwerte des
Quellprofils), die drei Entnahmehöhen des Pufferdialogs ohne Beschriftungsressource (die Maske
zeigt dort den deutschen Vorgabetext auch auf Englisch — Befund für eine spätere Welle), sowie
Löschknöpfe, „Simulation starten" (rechnender Weg), Knöpfe, die nur den Stand oder die Liste
wechseln, und der Kartenknopf ohne Beschriftung. Die Auswahlfelder holt die Welle KI‑F1b nach
(Entscheid KI‑D‑Q6: jedes Eingabefeld ist setzbar).

## Zahlen und Abnahme

72 neue Ressourcenschlüssel je Sprache, sieben neue Einheitenzeichen, 16 neue Tests; Katalog 19
Masken. Worktree: Kern-Filter 0 Fehler, 10 082 Tests grün (1 übersprungen). Gate im Hauptbaum:
siehe Statuszeile #419.
