# N5 — Nachzüge am Gebäudedialog (16.10.2026)

Sitzung „Gebäudesimulation“, Worktree `n5` auf `3a8f1bcb0`, Opus 5.5. Zwei Punkte aus der Nachzugsliste des Übergabepapiers
(Abschnitt 4, Punkt 3 und Punkt 6 „Verbleibend aus N3“). Kein Schemaschritt, kein neues SQL, Rechenweg unberührt, keine
`Dienste.*`-Schnittstelle geändert.

## 1. Wochenvorschau der Konditionierung nach Neulesen

- **Ursache:** `KonditionierungBearbeitung.Vorschauschluessel` setzte den Schlüssel der Kalenderkarte aus der Fassung der
  Konditionierung, der Wahl, Kühlung, Kopplung, Übergabeart und Sollwertprofil zusammen. Das Neulesen aus N3
  (`GebaeudeKatalogDialog.PrueffelderNeulesen`) ersetzt den Satz durch einen frisch gelesenen mit Fassung 0 — der Schlüssel
  blieb gleich, `Kalenderkarte` rechnete nicht neu und zeigte bis zur nächsten Handlung die alte Woche (so im Protokoll MA-1b,
  Abschnitt „Nachzug N3“, als offen vermerkt).
- **Lösung:** Der Schlüssel trägt einen Lesestand: Ein neuer Konditionierungssatz wird an seiner Identität erkannt und zählt
  den Stand hoch. Dazu das Raster des Wegs (`WochentagJan1`, `Bezugsjahr`), damit ein Wechsel der Klimaregion (Punkt 2) die
  Vorschau ebenfalls neu rechnet. `Kalenderschluessel` baut auf demselben Schlüssel auf und folgt mit.
- **Datei:** `EPOS.UI/Dialoge/Bedarf/KonditionierungBearbeitung.cs`.

## 2. „Klimaregion wählen…“ im Gebäudedialog

- **Ausgang:** Der Knopf an Gruppe 4 des Ampelblatts (`Modellpruefungsblatt.Klimaregion`) hatte im Gebäudedialog keinen
  Delegaten und zeigte nur den Hinweis `MPB_HINWEIS_KLIMA`.
- **Lösung ohne Navigation:** Statt eines Sprungs in die Startseite (das hätte eine Naht der Navigation gebraucht) öffnet der
  Gebäudedialog über dem Ampelblatt eine Überlagerung mit derselben `Suchauswahl` und denselben Einträgen wie die Kopfleiste und
  der Projektassistent (`StartseiteCtrl.KlimaregionAuswahlzeilen`, #527), vorbelegt mit der Stammregion des Projekts
  (`StartseiteCtrl.ProjektKlimaregionStammId`). OK schreibt über den Speicherweg der Kopfleiste
  (`StartseiteCtrl.KlimaregionSpeichern` mit Stamm-Id, Kopie der Region in das Projekt) und zieht den Projektkontext über die
  bestehende Methode `IProjektKontext.Uebernehmen` nach (Kopfband, Klimazone) — nur, wenn das Projekt das offene ist. Eine
  unveränderte Wahl schließt nur; ein Grund (Texte der Kopfleiste) bleibt am Feld stehen. Im Assistenten kein Weg.
- **Nachziehen nach der Wahl:** Das Ampelblatt prüft neu, die Kennwerte der Zeile werden aufgefrischt; steht der Editor offen,
  bekommt er den Konditionierungsweg neu aus `ProjektGaben` (Raster der neuen Region: Wochentag des 1. Januar, Feiertagsjahr),
  die übrigen Gaben bleiben dieselben Objekte; die Prüffassung zählt hoch, damit der Editor wie nach einer Übernahme (N3)
  Feiertagsland, Konditionierung und Zonen neu liest — mit ungespeicherten Änderungen daran nicht.
- **Dateien:** `EPOS.UI/Dialoge/Bedarf/GebaeudeKlimaregionWeg.cs` (neu), `GebaeudeDialog.razor` (Parameter
  `KlimaregionGaben`, Überlagerung, Wirt), `EPOS.UI.Daten/Bedarf/GebaeudeHuelle.cs` (`KlimaregionWeg`,
  `KlimaregionSpeichern`, Gabe `KlimaregionGaben`), Ressourcen `MPB_KLIMA_TITEL`, `MPB_KLIMA_TEXT`, `MPB_KLIMA_LABEL`,
  `MPB_KLIMA_GESPEICHERT` in beiden Sprachen, Designer nachgezogen (zweiter Lauf +0). Kein neuer Hilfeknopf, keine neue
  Maske mit Hilfe; die Eingabebilanz der KI-Maskenabdeckung für `GebaeudeDialog` steigt mit Vermerk von 0 auf 1 (die
  Suchauswahl gehört zur Handlung, die mit eigenem OK sofort schreibt).

## Tests

- `EPOS.UI.Tests/Dialoge/GebaeudeKatalogPruefleisteTests.N5_Neulesen_aendert_den_Schluessel_der_Wochenvorschau`
  (Gegenprobe im Fall: Zeichnen ohne Neulesen lässt den Schlüssel stehen; ohne den Lesestand im Schlüssel ist der Fall rot).
- `EPOS.UI.Tests/Dialoge/GebaeudeDialogTests.Klimaregion.cs`, vier Fälle: Wahl speichert und prüft neu; Grund bleibt am
  Feld, unveränderte Wahl schreibt nicht; ohne Delegat der Hinweis; der offene Editor bekommt das Raster der neuen Region und
  einen neuen Vorschauschlüssel.
- `EPOS.Kern.Tests/GebaeudeKlimaregionHuelleTests`, zwei Fälle auf der Kopie der Testdatenbank (Projekt 1030): Liste gleich der
  Kopfleiste, Vorbelegung mit der Stammregion, Speichern schreibt; ohne Region benannter Grund, ohne Projekt kein Weg.
- Ergebnis: Kern-Filter Release 0 Fehler; Windows-Schale (Linux, `EnableWindowsTargeting`) 0 Fehler; UI-Filter (Gebäudedialog,
  Gebäudeeditor, Konditionierung, Kalenderkarte, Modellprüfung, Wachen, KI) 526/526 grün; Kern-Filter
  (`GebaeudeKlimaregionHuelleTests`, `KonditionierungHuelleTests`, `AssistentKlimaregionTests`, `ModellpruefungDatenbankTests`)
  23/23 grün, Kern-Filter Ressourcen/Hilfe/KI-Masken 370/370 grün.

## Sichtabnahme Windows

- A-N5-1: Gebäudedialog → „Modell prüfen…“ → Gruppe 4 „Klimaregion wählen…“: Die Liste zeigt Kurzform und Einträge wie die
  Kopfleiste, vorbelegt mit der Region des Projekts.
- A-N5-2: Andere Region wählen, OK: Meldung „Die Klimaregion „…“ ist für das Projekt gespeichert.“, die Kopfleiste der
  Startseite zeigt die neue Region, das Ampelblatt ist neu geprüft.
- A-N5-3: Editor offen (Leiste im Reiter „Zonen“ → Gruppe 4), Region wechseln: Bildunterschrift und Teppichbild der
  Konditionierung nennen das Raster der neuen Region; die Wochenvorschau steht sofort richtig.
- A-N5-4: Nach einer Übernahme der Gruppe 3 im Ampelblatt zeigt die Wochenvorschau der Konditionierung ohne weitere Handlung
  den neu gelesenen Stand.

## Offen

- Die Herkunftszeile unter der Liste (wie im Projektassistenten) fehlt in der Überlagerung.
- Steht der Editor nicht offen, bleibt nichts nachzuziehen; geöffnete Wärmebedarfsansichten lesen die Region erst beim
  nächsten Öffnen.
