# SQ — Projektdatei-Import (`.sqproj`) bleibt stehen (11.10.2026)

Anwenderbefund unter Windows im Entwicklermodus: „der sqproj-Datei import scheint nicht mehr zu funktionieren (bleibt
stehen beim import)“. Auftrag: Ursache finden, belegen, klein beheben.

## Weg

Dateiwahl (`GebaeudeImportHuelle.DateiWaehlenAsync`, Windows über `Blazornachlauf.Nachgelagert`) → Lesen im Arbeitsfaden
(`LesenAsync` bzw. `ProjektdateiLesenAsync`, `Kulturweitergabe.Starten`, Kern `GebaeudeImportAblauf`, `SqprojLeser` auf
einer Arbeitskopie, nur lesend) → Zuordnen **synchron im Oberflächenfaden** (`NachDemLesen` → `ZonenDerProjektdatei` →
Schritt `PROJEKTDATEI` → `ProjektdateiUebernehmen` zweimal: Probe auf der Kopie, dann am Plan) → Prüfen → OK
(`Uebernehmen` baut nur die Vorbelegung) → vorbelegter Katalogeditor → Speichern des Katalogsatzes → neue Projektzeile →
Speichern der Gebäudeliste.

## Befund

- Kein synchron blockierender Aufruf (`.Result`, `.Wait()`, `GetResult()`) in Dialog, Hülle und Kernweg; jede
  `_laeuft`/`_schreibt`-Marke wird im `finally` zurückgesetzt.
- Verlauf seit 08.10.: Der Kernweg (`Import/Sqproj`, `Import/Gebaeude`, `Import/Ifc`) ist seit 09.10. unverändert.
  `8ec5af9b7` setzt nur das Kennzeichen `ProjektdateiFreigegeben` (= `Entwicklermodus.Aktiv`) und blendet Knöpfe, Quellen
  und Filter aus; im Entwicklermodus ist der Weg unverändert. `4d367fd88` löscht nichts im Importweg. `096272d49` ändert
  nur Papiere und Lizenzhinweise. `595f89b29` fängt das Bereitstellen der Importgaben (`Katalogladung.Satz`), ohne den
  Ablauf zu ändern. Die Schemaschritte 213–217 legen an `Tab_Zone` nur `Vorheizzeit_H` (nullbar, CHECK 1–47) an; der
  Import setzt die Spalte nicht.
- Nachstellen auf Linux: Kern-Tests `Sqproj*|Projektdatei*|GebaeudeImport*` 434/434, UI-Tests `GebaeudeImport*|Projektdatei*`
  123/123 grün, kein Hänger (Blame-Grenze 120 s). Neuer Durchlauf `SqprojGebaeudedialogDurchlaufTests` (Projekt 1007,
  Testdatenbank auf Schema 217, synthetisches Sporthaus, jede Stufe mit 60-s-Grenze): Importweg 66 ms, Lesen 179 ms,
  Zuordnen 556 ms, Zuordnen mit Projektdatei 60 ms, Prüfen 144 ms, Übernehmen 72 ms, Editor 46 ms, Katalogsatz speichern
  66 ms, Aufnehmen 11 ms, Gebäudeliste speichern 122 ms — grün.
- Im Repositorium liegt keine echte Projektdatei (`Quellen/*.sqproj` steht in `.gitignore`).

## Ergebnis

Ursache auf Linux **nicht nachstellbar**; keine Behebung gebaut. Wahrscheinlichste Ursachen, in dieser Reihenfolge:

1. **Ausnahme im Ereignisweg der WebView** (echte Datei oder Bestandsdatenbank des Anwenders): Eine Ausnahme aus `Zuordnen`,
   `Pruefen` oder dem Schreibweg verlässt den Blazor-Ereignisrückruf; je nach Ort zeigt die Fehlerschranke ihren Ersatz,
   oder die Oberfläche steht ohne Absturz. Beleg liefert `Logs\Ausnahmen.txt` neben der Datenbank.
2. **Rechenlast im Oberflächenfaden**: `Zuordnen` mit dem Schritt `PROJEKTDATEI` läuft synchron (Körperbildner,
   Kerbschnitt, Teilflächen); bei einer großen Datei steht die WebView ohne Fortschrittsanzeige, bis die Zonierung fertig ist.
   Abhilfe wäre ein asynchroner Zuordnungsweg mit Ladezustand — eine Änderung an Dialog und Hülle über die Grenze dieses
   Auftrags.

Vom Anwender gebraucht: `Logs\Ausnahmen.txt` neben der Datenbank, der Schritt, an dem es steht (nach der Dateiwahl mit
Fortschrittsbalken, nach dem Lesen ohne Balken, nach OK), ob die Oberfläche noch auf Klicks reagiert, Größe der Datei und
nach Möglichkeit die Datei selbst unter `Quellen/` (nicht versioniert) für `SqprojQuelldateienDiagnoseTests`.
