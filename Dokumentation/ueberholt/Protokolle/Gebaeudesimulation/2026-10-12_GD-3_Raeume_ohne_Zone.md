# Protokoll GD-3 — Räume ohne DIN-Zone

Welle GD-3 aus dem [Entwurf Gebäudedialog](../../../aktuell/Gebaeudesimulation/2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md)
(Abschnitte 1.2, 2.3, Frage F3, Wellenplan Zeile GD-3). Grundlage ist Entscheid E130 des Anwenders vom 12.10.2026 nach
Empfehlung: Räume ohne DIN-Zone werden automatisch platziert und lassen sich im Zonenbaum zurücknehmen.

## Befund

Der Schritt „Zonen der Projektdatei“ hebt die Zonierung der Regel auf (`Zonenplan.ZonierungAufheben`). Danach hängt
`SqprojZonen.Uebernehmen` nur die Räume ein, die in einer Zone der gewählten Zonierung stehen. Ein Raum ohne Zone blieb in
`Zonenplan.NichtZugeordnet`. Das galt auch für einen unbeheizten Raum in einer beheizten Zone, denn `Zuordnen` lehnt ihn ab.
`Zonenplan.Abschlusspruefung` meldete ihn als Fehler. Damit waren der Bauteilvorschlag (`Moeglich = false`) und das OK gesperrt,
auch bei abgeschaltetem Schalter. Gespeichert wurde erst nach einem Handschritt.

## Regel

Neuer Planschritt `Zonenplan.RestPlatzieren`. `GebaeudeImportAblauf.ProjektdateiUebernehmen` ruft ihn nach der Übernahme auf,
sofern mindestens eine Zone übernommen ist. `SqprojZonen.Uebernehmen` selbst bleibt unverändert. Die Räume werden in dieser
festen Folge platziert, in Dateireihenfolge der Räume und Planreihenfolge der Zonen:

1. **unbeheizt** (wirksamer Haken) → gemeinsame Zone „unbeheizt“. Eine schon platzierte Zone gleichen Namens wird weiter
   belegt, sonst wird sie angelegt.
2. **beheizt mit Nachbarraum in einer beheizten Zone** → die Nachbarzone mit der größten gemeinsamen Fläche. Die Fläche ist die
   Summe der Netto-, sonst Bruttoflächen der Bauteile, die beide Räume als Nachbarn führen (`AbbildBauteil.Nachbarn`). Bei
   Gleichstand gilt die erste Zone im Plan. Der Schritt läuft wiederholt, bis kein Raum mehr eine Nachbarzone findet. So
   wachsen Raumketten mit.
3. **sonst** → die Restzone des Geschosses („Rest OG“, ohne Geschoss „Rest“), ohne Profil.

Jeder platzierte Raum trägt eine `Restplatzierung` mit Grund, Zielzone und gemeinsamer Fläche. Zonen, die der Schritt anlegt,
tragen `Planzone.AusPlatzierung`. Die Zusammenfassung erscheint als Info `…PLAN_REST_PLATZIERT` mit Gesamtzahl und Zahl je
Grund.

`PlatzierungZuruecknehmen(raum)` stellt einen Raum zurück auf „ohne Zone“. Wird dadurch eine Restzone leer, entfällt sie. Ein
Raum, der nicht automatisch platziert wurde, wird benannt abgelehnt. Eine Zuordnung von Hand löscht die Herkunft, ebenso
„Zonierung aufheben“ und die Neubildung nach einer Regel.

Oberfläche: Die Hülle gibt je Raum den Grund als Text und Schlüssel weiter (`data-platziert`). Der Plan liefert Zahl und Meldung
„wie viele wohin“. Der neue Planschritt heißt `RUECKNAHME`. Der Zonenbaum zeigt die Marke „automatisch platziert“ mit dem Grund
im Tooltip und je Raum den Knopf „Zurücknehmen“. Die Meldung steht unter dem Zonenbaum und am Schalter „als Zonen mit
Bauteilen“. Die Sperre am OK bleibt nur, wenn der Anwender eine Platzierung zurücknimmt und den Raum offen lässt.

Kein Schemaschritt, kein Rechenweg berührt.

## Dateien

- Kern: `EPOS.Kern/Allgemein/Import/Gebaeude/Zonenplan.cs`, `GebaeudeImportAblauf.cs`, beide `.resx` mit 12 neuen Schlüsseln
  und `Resource.Designer.cs`.
- Hülle und Oberfläche: `EPOS.UI.Daten/Bedarf/GebaeudeImportZonen.cs`, `GebaeudeImportHuelle.cs`,
  `EPOS.UI/Dialoge/Import/GebaeudeImportDaten.cs`, `GebaeudeImportDialog.razor`, `EPOS.UI/wwwroot/epos-ui.css`.
- Tests: neu `EPOS.Kern.Tests/ZonenplanRestPlatzierenTests.cs` (4 Fälle: drei Regeln samt Kette und Gleichstand,
  Determinismus, Rücknahme, Herkunft bei Handzuordnung und Aufheben). Umgestellt
  `GebaeudedialogBefundProbeTests` (der Weg „nur Projektdatei“ platziert, OK und Bauteilvorschlag sind frei, drei Zonen werden
  in die Testdatenbank geschrieben, Rücknahme sperrt wieder) und `GebaeudeImportNurProjektdateiDialogTests` (bunit: Meldung
  zweifach, Marke, Rücknahme mit Rückkehr der Sperre, englische Texte).

## Testzahlen

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale mit `EnableWindowsTargeting=true`: 0 Fehler.
- `EPOS.Kern.Tests`, Filter Zonenplan, Sqproj, Gebaeudedialog, GebaeudeImport, Zonenbaum, Ressource/Resource, KiMask:
  608 bestanden, 0 rot.
- `EPOS.UI.Tests`, Filter GebaeudeImport, Zonenbaum, Gebaeudedialog, Stilblatt, KiMask, Ressource/Resource, Wache: 470
  bestanden, 0 rot.
- Messung an der Anwenderdatei (nur Zählungen, Weg „nur Projektdatei“, Haken der Datei): 56 Räume. DIN-Zonen: 12 Zonen,
  0 Räume platziert, 0 offen, Bauteilvorschlag möglich. Simulationszonen: 13 Zonen, 3 Räume platziert (alle unbeheizt, eine
  Restzone), 0 offen, Bauteilvorschlag möglich.

## Für die Windows-Sichtabnahme

1. „nur Projektdatei“ mit einer Datei, deren Zonen einen Raum auslassen: Die Liste „Nicht zugeordnete Räume“ ist leer, der
   Schalter „als Zonen mit Bauteilen“ steht an, und darunter steht die Meldung mit Zahl und Ziel.
2. Den Zonenbaum an der Restzone aufklappen: Die Marke „automatisch platziert“ ist leise und kursiv, der Tooltip nennt den
   Grund, und der Knopf „Zurücknehmen“ ist mindestens 44 px hoch.
3. „Zurücknehmen“: Der Raum steht unter „Nicht zugeordnete Räume“, die leere Restzone verschwindet, OK nennt die Sperre, und
   „Rest nach Regel zuordnen“ löst sie.
4. Wechsel DIN ↔ Simulationszonen: Die Platzierung wird neu gebildet, und die Meldung folgt.
