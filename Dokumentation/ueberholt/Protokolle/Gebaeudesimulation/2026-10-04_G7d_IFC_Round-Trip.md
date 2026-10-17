# Protokoll G7d — Round-Trip-Anreicherung fremder IFC4-Dateien (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle G7d (E67, E69), Teilzweige als Worktrees `g7d-kern` und `g7d-ui`, Merges `6ddd52c` (Kern) und `46f5603` (Oberfläche). Statuszeile #711.
**Entscheid:** keiner neu beim Anwender; drei Entscheide der Orchestrierung (Abschnitt 4). Kein Schemaschritt, Basis unverändert (Referenzlauf nicht betroffen).

## 1 Auftrag

Die vom Architekten gelieferte IFC-Datei angereichert zurückgeben (E69, Datenaustauschkonzept 6.6): erneute Dateiwahl mit Hash-Abgleich, Schema- und Protokollsperre, Ergänzen statt Doppeln, neuer Dateiname, Kennung in `FILE_DESCRIPTION`, eigene `IfcApplication`, Beipackzettel D11.

## 2 Vorgehen

Zwei Opus-Agenten nacheinander (64 Aufrufe Kern, 43 Aufrufe Oberfläche); zwei konfliktfreie Merges.

## 3 Ergebnis je Teil

**Kern** (`EPOS.Kern/Allgemein/Export/Ifc/IfcAnreicherung.cs`, Commit `4686d2b`):

- **Sperren (Probe 13):** SHA-256 der erneut gewählten Datei ungleich `Tab_Importquelle.Hash`; `Schemastand` ungleich IFC4; `FehlendeEntitaeten` größer 0; dazu erneutes Laden mit `MemoryModel` und Prüfung beider Verlustkanäle; Kopf mit anderem Schema; nur STEP (ifcXML und ifcZIP benannt verweigert, `NUR_STEP`). Jede Verweigerung bietet eine eigene Datei nach G7c an.
- **Ergänzen statt Doppeln:** `Pset_<Klasse>Common` (Wall, Slab, Roof, Window, Door, CurtainWall, Covering, Plate), `Pset_DoorWindowGlazingType`, `Pset_SpaceThermalRequirements`, `Pset_BuildingCommon`. Fachwerte ersetzen vorhandene Eigenschaften, Nebenwerte werden nur ergänzt, fehlende Sätze angelegt, geteilte Sätze je Objekt abgespalten. `Qto_*` nur wenn fehlend und eindeutig zugeordnet, in den Projekteinheiten der Datei.
- **Eigene Sätze:** `EPOS_Bauteil`, `EPOS_Zone`, `EPOS_Gebaeude`, `EPOS_Rechenlauf`, `EPOS_Ergebnis` immer als eigene Sätze, frühere EPOS-Sätze werden ersetzt. `IfcMaterialLayerSet` nur ohne Materialzuordnung an Objekt und Typ, sonst `EPOS_Bauteil.Materialherkunft`.
- **Kennungen und Kopf:** vorhandene `GlobalId` und Entitätsnummern bleiben; neue `IfcRoot` deterministisch aus Quell-GlobalId und Rolle über `IfcExportKennung`. `FILE_DESCRIPTION` mit Vermerk „angereichert durch EPOS-Plan <Fassung> am <Zeit>“; `FILE_NAME` samt `OriginatingSystem`/`PreprocessorVersion` und `FILE_SCHEMA` bleiben; eigene `IfcApplication` mit eigener `IfcOwnerHistory`. Geometrie unverändert (Entitäten byte-gleich).
- **Einheiten, Ergebnisse:** Temperaturen, Heizlast, Winkel und Energie mit ausdrücklicher Einheit; Raumergebnis nur an einem Raum, der als einziger seiner Zone zugeordnet ist, sonst `ERGEBNIS_GETEILT` und nur am Gebäude.
- **Schnittstelle** in `GebaeudeExportAblauf`: `Dateivorschlag` (`haus.ifc` zu `haus_EPOS.ifc`), `AnreicherungsQuelle`, `AnreicherungVorschau` (Sperren oder genau eine Warnung Beipackzettel D11), `Anreichern` mit `GebaeudeAnreicherungBilanz` (Geschrieben, Verweigert, Grund, Objekte, Ergänzt, Ersetzt, Übersprungen, Meldungen, Bytes). Meldungsschlüssel `GEXP_PROT_ANR_*` (19 Ressourcen).
- **Wächter** in `IfcImportTests` jetzt auch über `Export/Ifc/*.cs` (nie `ignoreTypes`/`SkipTypes`, `LoadStep21` mit `null`). Tests `IfcAnreicherungTests` (22; eigene Testdatei „Fremdhaus“, IFC4 in mm mit „Fremd-CAD“-Geschichte): Probe 13, geteilter Satz, zweiter Durchlauf doppelt nichts, Material nur ohne Zuordnung, Kennungen, Validator, Rundlauf über `IfcLeser` mit U-Werten 0,28/0,30/0,25, Geometrieentitäten gleich.

**Oberfläche** (Commits `c19c8e8`, `28d45e5`, `531c3cf`, `7f89374`):

- Im Exportdialog bei Format IFC die Wahl „Eigene Datei schreiben“ / „Originaldatei anreichern“ (nur mit IFC-Importquelle des Gebäudes), Knopf „Originaldatei wählen…“ über `Dienste.Datei.DateiOeffnenAsync`, Anzeige von Dateiname, Importquelle und Vorschaumeldungen.
- Bei Verweigerung Fehlerbanner mit Grund und Knopf „Stattdessen eigene Datei schreiben“; bei Freigabe eigene Bestätigung des Beipackzettels („Ich gebe eine fremde Datei verändert weiter“).
- Speichern reichert zuerst in den Speicher an, dann Dateiwahl mit Vorschlag `_EPOS.ifc` (nie der Originalname); iOS teilt. Erfolgsmeldung mit Objekte/Ergänzt/Ersetzt/Übersprungen.
- Hülle `GebaeudeExportHuelle` mit Gaben `AnreicherungMoeglich`, `OriginalWaehlen`, `Anreichern`; Datenbankzugriffe über `GebaeudeImportCtrl` (`LesenQuellen`, `LesenZuordnungen`). 18 Ressourcen `GEXP_ANR_*`; `KiMaskenabdeckungWacheTests` auf 5 Eingabestellen.
- Tests: UI 167/167 (24 neu), `GebaeudeExportFormatHuelleTests` 20/20 (10 neu, darunter ein Durchlauf ohne Stub: `fremdhaus_EPOS.ifc` im STEP-Format, Original byte-gleich). Windows-Schale 0 Fehler.

## 4 Entscheide der Orchestrierung

- **Nur neue Verstöße brechen ab:** Der Validator läuft vor und nach dem Eingriff; Altlasten der Datei werden als Info `VERSTOESSE_VORHER` gemeldet, nur ein Verstoß, den der Eingriff erzeugt, bricht ab.
- **Nur STEP:** ifcXML und ifcZIP werden benannt verweigert (`NUR_STEP`) mit dem Angebot einer eigenen Datei.
- **Geteilte Zone:** Das Raumergebnis steht nur an einem Raum, der als einziger seiner Zone zugeordnet ist; sonst `ERGEBNIS_GETEILT` und das Ergebnis nur am Gebäude.

## 5 Abweichungen

Keine vom Konzept 6.6 außer den Entscheiden aus Abschnitt 4; sie sind im Konzept unter „So gebaut (G7d)“ vermerkt.

## 6 Abnahme

Kern-Filter und Tests liefen grün; die Zahlen des Gates trägt die Orchestrierung in der Statuszeile nach. Die Testdatenbank auf origin steht noch auf Schema 180 (Bundle AK1z beim Anwender), deshalb sind sechs Datenbank-Wachen (Schemastand, Projekte 1052 und 1054) bis dahin rot — nicht Teil dieser Welle. CI: Kern-Lauf nach dem Push nachzutragen.

## 7 Offen

- Probe 16 mit einer angereicherten Architektendatei (Revit, Archicad, HiCAD), Probe 15 (bSI) der angereicherten Datei, Sichtabnahme des Dialogs — beim Anwender.
- iOS-Dateiwähler mit IFC-Filter auf einem Gerät.
- Keine Größengrenze der Originaldatei.
- Logbuch-Version.

## 8 Aufwand

Opus 64 + 43 Aufrufe; Papiere Sonnet.
