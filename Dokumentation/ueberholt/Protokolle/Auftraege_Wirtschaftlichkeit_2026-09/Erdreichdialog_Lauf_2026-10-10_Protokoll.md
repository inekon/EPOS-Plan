# Protokoll: Dialog Wärmequelle Erdreich — Lauf mit angezeigten Eingaben, Kennwerte (Statuszeile #898, 10.10.2026)

## Auftrag

Anwendermeldung, Referenzprojekt AK3-K (Projekt 1059): Dialog Simulation, Simulation Konfiguration, Wärmepumpe Quelle
Erdsonde (Pille „Quelle: Erdsonde 8×90 m"). Die Ergebnisse der Simulation sollen sofort angezeigt werden (auch die
Grafik), nicht erst nach OK und erneutem Öffnen. Mittlere und niedrigste Temperatur sollen als Charakterisierung der
Ergebnisse angegeben werden.

## Ursache

Nicht die Anzeige: bunit am Dialog, an der Seite `SimulationKonfigSeite` und an der Hülle gegen die Testdatenbank
(Projekt 1059, Anlage 23908) zeigt das Laufergebnis mit 8 760 Werten sofort. Der Knopf „Simulation" rechnete mit den
gespeicherten Quelldaten (`QuelleErdreichHuelle.cs:170-187` rief nur `SimulationRunner().Simuliere(idProjekt, …)`, der
Dialog `Simulieren(Daten.IdProjekt)`); die Eingaben des Dialogs erreichten die Datenbank erst beim OK. Der Dialog führte
das als offenen Punkt W10-B10 mit dem Hinweis `SIMQ_ERDREICH_SIM_NUR_GESPEICHERT`, die Wiki-Quelle sagte es ausdrücklich.

## Berichtigung je Commit

- `12dfc999c`: Kern `EPOS.Kern/Allgemein/Simulation/ErdreichLaufvorgabe.cs` legt die Eingaben einer Anlage für die Dauer
  eines Laufs über die gespeicherten Werte (`AsyncLocal`, nur lesend; Lesestellen `WaermequelleClass.WertLesen`/
  `WertLesenStill`, `ErdreichAuswertung.KlimazoneDesProjekts`). Die Hülle rechnet `Simulieren` mit dem Eingabesatz unter
  der Vorgabe, eine Abbildung Quelle/Sondenfeld für Lauf und OK-Weg. Der Dialog prüft vor dem Lauf dieselben acht Regeln
  wie OK, geschrieben wird nur beim OK; der Hinweis „nur gespeichert" entfällt (beide Sprachen). Kennwerte im Kern:
  `ErdreichTemperatur.LaufKennwerte` (Jahresmittel, Tiefst- und Höchstwert mit Zeitpunkt, Raster 8 760 h, Gemeinjahr);
  die Zeile `kennwerte-lauf` ersetzt die Monats-Min/Max-Zeile; Ressourcen `SIMQ_ERDREICH_LAUF_KENNWERTE_ZEILE`,
  `SIMQ_ERDREICH_LAUF_ZEITPUNKT`. Stunden unter einer Grenze sind bewusst nicht aufgenommen (einzige geführte Grenze ist
  die Frostregel, sie steht schon in der Auslegungsprüfung).
- `d9cde0ddb`: Abbrechen/✕/Esc verwirft einen Lauf, dessen Eingabesatz vom gespeicherten abweicht (der Stand vor dem
  ersten Lauf des Dialogs wird zurückgelegt; `ErdreichAuswertung.StandDesProjekts`/`StandZuruecklegen`, Hülle
  `ErdreichLaufsitzung`, Rückruf `QuelleErdreichAbgebrochen`); OK behält den Lauf. Der Quelltyp läuft über
  `ErdreichLaufvorgabe.Quelltyp` an allen zeilenweisen Lesestellen von `WQ_Typ` (SimulationControl, WaermesenkeClass,
  Warnkriterien zweimal, Hydraulikbild).
- `d0ef3a156`: Merge origin (53 Commits, darunter CSV-3 „CSV…" an jeder Zeitreihe); Konflikte nur in den beiden `.resx`
  (beide Blöcke übernommen), Designer neu erzeugt; der CSV-Export des Erdreichbilds trägt nach dem Lauf die gerechnete
  Reihe (Test).
- `bfc291f88`: Merge origin (6 Commits, ohne Überschneidung).
- Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Wärmequelle Erdreich.wiki`: drei Absätze an den neuen Stand
  angepasst; Upload ausstehend (nächster Sammel-Upload).
- Kein Schemaschritt; Rechenweg unverändert.

## Tests

Neu: `EPOS.Kern.Tests/QuelleErdreichLaufHuelleTests` (rot ohne Laufvorgabe, grün mit; Abbrechen; Quelltyp),
`EPOS.Kern.Tests/ErdreichLaufkennwerteTests`, `EPOS.UI.Tests/Dialoge/QuelleErdreichDialogTests` (+3, +1 CSV),
`EPOS.UI.Tests/Seiten/SimulationKonfigSeiteTests` (Abbrechen/✕).

## Gate

- Auf `d0ef3a156`: `WP-Plan.Kern.slnf` Release 0 Fehler; KiKern 549/549, SpeicherEngine 397/397, SpeicherPlanung 27/28
  (1 übersprungen), EPOS.UI 8 185/8 185, EPOS.Kern 12 960/12 968 (7 übersprungen, 1 rot:
  `ZapfprofilHuelleKatalogdialogTests.Der_Parametersatz_trifft_die_Parameter_der_Komponente` — fremd, kam mit CSV-3
  `bc553f9df`, auf origin mit `6c6377540` berichtigt). Referenzlauf der 11 CI-Projekte gegen
  `2026-10-10_R50_Wochentagsraster`: 11/11 PASS (auch vor dem Merge 11/11).
- Nach Merge `bfc291f88`: Bau 0 Fehler, EPOS.UI 8 186/8 186, EPOS.Kern gefiltert (Zapfprofil, Erdreich, Erdsonde,
  QuelleErdreich, Warnkrit, Waermesenke, Hydraulik, Ressourcen, Wiki, Doku, Csv, Parallelität) 602/602.
- Statusnummer #898 (#897 war auf origin vergeben).

## Offen

- Windows-Sichtprüfung in `EPOS_Plan.exe`: Erdsonde ändern, Simulation — Kurve, Kennwerte und Prüfung sofort;
  Abbrechen und Wiederöffnen — kein gerechneter Lauf mehr.
- Wiki-Upload der Seite Wärmequelle Erdreich im nächsten Sammel-Upload; Logbuch-Satz steht in Abschnitt 2 von
  `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`, Version beim Anwender offen.
