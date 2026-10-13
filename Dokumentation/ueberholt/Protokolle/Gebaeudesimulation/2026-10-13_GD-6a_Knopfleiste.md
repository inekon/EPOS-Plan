# Protokoll GD-6a — Knopfleiste des Gebäudedialogs

Welle GD-6a nach den Anwenderwünschen vom 12.10.2026
([Übergabepapier](../../../aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md), Abschnitt 4
Punkt 2): „Simulation nur von ‚Im Projekt‘ – alle Gebäude oder Auswahl (auswählbar machen)“ und „Baustoff-Zuordnungen…“ aus
der Leiste des Projektsatzes an Import und Editor.

## Befund

- **Simulation:** Der Knopf „Simulation…“ (`BedarfGaben`, `BeiSimulation`) stand nur in der Satz-Überlagerung
  (`epos-gebaeude-satzknoepfe`), die „⤢ Vergrößern“ am Projektsatz öffnet. Die Hülle `GebaeudeHuelle` reicht `BedarfGaben`
  immer durch; in der Windows-Schale fehlt nichts, der Knopf war nur versteckt.
- **Export, Neulesen, Ausrichtung:** Sie sind ebenso nur in der Satz-Überlagerung erreichbar: „Exportieren (gbXML)…“ und
  „Datei erneut lesen“ in `epos-gebaeude-satzknoepfe`, „Ausrichtung ändern…“ im Abschnitt Ausrichtung des Satzes. Alle drei
  wirken auf genau die gewählte Zeile und tragen eigene Sperren (Zeile ohne Projektkopie, ungespeicherte Änderungen,
  Importquelle). In die Kopfleiste gingen sie nur mit Umbau (Einzelwahl aus Kästchen, Sperrlogik je Ziel) — deshalb nur
  Befund, nicht verschoben.
- **Mehrfachwahl:** Das Kästchen einer Projektzeile setzte über `BeiWahlGeaendert` den Satz auf diese Zeile und nahm damit
  einen gewählten Katalogsatz weg.
- **Baustoff-Zuordnungen:** Der Knopf stand in der Katalog-Fußleiste rechts (`KatalogFussRechts`) neben „Importieren…“; der
  Parametersatz `BaustoffzuordnungenGaben` kommt aus der Hülle (`BaustoffzuordnungenHuelle.Gaben(projektId)`).

## Ergebnis

- **Kopfknopf:** „Simulation…“ steht in der Kopfleiste des Abschnitts IM PROJEKT neben „In die Datenbank übernehmen…“ und
  „Aus dem Projekt entfernen“: ohne Kästchen „Simulation… (alle n Gebäude)“, mit Kästchen „Simulation… (m gewählt)“; ohne
  Projektzeile weich gesperrt mit Grund. Nicht im Assistenten, nicht ohne Delegat; die Katalogverwaltung
  (`GebaeudeAdminDialog`) kennt ihn nicht. Der Knopf am Satz bleibt für das gewählte Gebäude.
- **Semantik mehrerer Gebäude:** Ein Ziel öffnet den Wärmebedarf wie bisher. Mehrere rechnen nacheinander je Zeile über
  denselben Weg `BedarfGaben` (samt stillem Speichern einer Importzeile außerhalb des Assistenten) und stehen in einer neuen
  leichten Überlagerung `GebaeudeSimulationsliste`: je Gebäude Name, Heizwärme, Höchstlast, Befund (der benannte Grund ohne
  Zahl), bei mehr als einem gerechneten die Summe der Heizwärme; „Bedarf…“ springt in den Einzelbedarf
  (`GebaeudeBedarfDialog`), die Liste bleibt darunter stehen. Begründung: `GebaeudeBedarfDialog` trägt genau ein Gebäude, ein
  Umbau auf mehrere hätte Bilder, Zonen und Kennzahlen vervielfacht; die Liste braucht keine Datenbank (Zahlen aus `Daten`
  des Parametersatzes). Rechenweg unberührt.
- **Mehrfachwahl:** Das Kästchen einer Projektzeile lässt einen gewählten Katalogsatz stehen; ohne Katalogsatz wählt es wie
  bisher die Zeile.
- **Baustoff-Zuordnungen:** Aus dem Gebäudedialog genommen (Knopf, Überlagerung, vier Textparameter). Der Dialog reicht
  `BaustoffzuordnungenGaben` an den Gebäudeimport (Knopf unter dem Kopf der Liste „Bauteilaufbauten“) und an den
  Gebäudeeditor (Reiter „Zonen“, unter den Zonen mit Bauteilen und Aufbauten) durch, beide als Überlagerung mit eigener
  Rückmeldung; im Assistenten reicht er nichts durch. Die Naht der Hülle bleibt unverändert, kein Plattformcode.
- **Ressourcen:** 13 Schlüssel `GEBSIM_*` in beiden Sprachen, Designer neu (wiederholbar).

## Dateien

- `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`, neu `GebaeudeSimulationsliste.razor` und `GebaeudeSimulationseintrag.cs`
- `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor`, `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor`
- `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`
- Tests: neu `EPOS.UI.Tests/Dialoge/GebaeudeDialogTests.Knopfleiste.cs` (partial), dazu `GebaeudeImportDialogTests`,
  `GebaeudeKuehluebergabeTests`, `BaustoffzuordnungenDialogTests`

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Windows-Schale mit `EnableWindowsTargeting=true`: 0 Fehler.
- `EPOS.UI.Tests` mit Filter `Gebaeude|Baustoffzuordnungen`: 881 bestanden, 0 rot (11 neue Fälle, zwei alte des
  Gebäudedialogs zu einem zusammengelegt).
- `designer_neu.py`: unverändert, wiederholbar.

## Offen

- Export, Neulesen und Ausrichtung in die Kopfleiste oder an die Detailzeile (Befund oben) — Anwenderentscheid.
- Doppelter Knopf „Simulation…“ (Kopfleiste und Satz-Überlagerung): ob der am Satz entfällt — Anwenderentscheid.
- Nach dem Entfernen einer Zuordnung im laufenden Import gilt der gemerkte Stand des Laufs weiter; erst ein neuer Lauf liest
  ihn neu.
- Hilfeseite „Gebäude“ und Logbuch-Entwurf (Kopfknopf, Liste, neuer Ort der Baustoff-Zuordnungen) mit der Wiki-Welle.
- Windows-Sichtabnahme der Kopfleiste (Breite mit drei Knöpfen und Zähltext).
