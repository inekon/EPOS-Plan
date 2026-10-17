# Protokoll ZB — Zonenbaum: freie Zonen, nicht zugeordnete Räume, Nutzung je Zone (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Wellen ZB-1 (Kern) und ZB-2 (Dialog) nach Entscheid E79, zwei Opus-Agenten nacheinander im Worktree `zonenbaum`: ZB-1 `b0d4449a`, `6a3872d2`, `e5ef7a48`, `31b0fbaa`, `5b1c5b8a`, `e2983252`; ZB-2 `752adcf8`, `493d2dbc`, `5c142f83`, `28599619`, `8a1d216a`, `47afd1a8`, `bafe66ad`; Merge `688cdb7e`. Statuszeile #730.
**Entscheid:** E79 (Anwender, 05.10.2026) mit den drei Empfehlungen; zwei Festlegungen der Orchestrierung (Abschnitt 4). Kein Schemaschritt, Basis unverändert, Referenzlauf nicht betroffen.

## 1 Auftrag

Die Zuordnung Raum–Zone beim Gebäudeimport nach dem Muster des Zonierungsdialogs von HottCAD: Baum Gebäude → Zonen → Räume mit Geschoss, Liste „nicht zugeordnete Räume“, Zone hinzufügen und löschen, Zonierung aufheben, Räume und Geschosse zur gewählten Zone, Nutzung je Zone aus den Konditionierungsvorlagen; Mehrfachwahl plus Knopf, Ziehen später; nicht zugeordnete Räume sperren das OK.

## 2 Vorgehen

ZB-1 (82 Aufrufe) baute den Zonenplan im Kern, ZB-2 (91 Aufrufe) den Dialog samt Hülle; Merge in den Hauptbaum ohne Konflikt, Gate 730.

## 3 Ergebnis

- **Kern** `EPOS.Kern/Allgemein/Import/Gebaeude/Zonenplan.cs`, `EPOS.Kern/Controller/ZonenplanCtrl.cs`: freie Zonen (Schlüssel „Z:n“, Name, Nutzung WOHNEN/BUERO/SCHULE/keine, Herkunft, angelegt), Räume zugeordnet, nicht zugeordnet oder „außerhalb“ (von der Regel bewusst draußen gelassen, sperren das OK nicht); Startplan aus der Regel (Z4, Z6, …), danach nur von Hand: `ZoneAnlegen`, `ZoneLoeschen`, `ZoneUmbenennen`, `NutzungSetzen`, `ZonierungAufheben`, `Zuordnen` (alles oder nichts), `GeschossZuordnen`, `RestNachRegelZuordnen`, `NachRegelNeuBilden`, `BeheizungSetzen`; jede Ablehnung benannt. Zonierung und Bauteilvorschlag aus dem Plan; Mindestgröße M8 nur im Regelvorschlag; Speichern von Name (`Tab_Zone.Bezeichner`), Nutzung (Kalenderkopien der Vorlagen über den KP2-Schritt `Konditionierungsarbeit.VorlageUebernehmen`, unbeheizte Zonen ohne Heiz- und Kühlkalender) und Raumzuordnung (`Tab_Importzuordnung`); erneuter Import derselben Datei findet den Plan wieder (leere Zonen und der Merker „angelegt“ nicht). Abschlussprüfung `ZUORDNUNG_UNVOLLSTAENDIG` sperrt das Speichern. Vorbelegung der Nutzung nur für beheizte Zonen aus Büro → BUERO und Wohnen/Schlafen/Küche → WOHNEN. 15 Ressourcen × IFC/gbXML × de/en.
- **Dialog** (`GebaeudeImportDialog.razor`, Hülle `GebaeudeImportHuelle`, DTOs `GebaeudePlanschritt`, `GebaeudeZonenplanDaten`): Baum mit Auswahlhaken je Zone und Raum (`data-zone`, `data-raum`), Name als Eingabefeld, Nutzung als Klappliste, Haken „beheizt“, Fläche, Raumzahl, Heizsollwert Tag; rechts „Nicht zugeordnete Räume (n)“; Knöpfe Zone hinzufügen, Zone(n) löschen, Zonierung aufheben (Rückfrage), Räume zur ausgewählten Zone, Geschoss zur Zone, Rest nach Regel; Grundriss-Klick als Schritt; Ablehnungen als Banner mit Raumhaken als Ausweg; OK weich gesperrt (`aria-disabled`, Zahl) mit Hinweis über der Schlussleiste; Regelwechsel mit Rückfrage. Die Hülle hält keinen Planzustand: die Schritte reisen in der Anfrage, der Plan wird je Aufruf neu gebildet; ohne Schritt bleibt die Zonierung die der Regel. Die Zonentabelle der Welle D ist durch den Baum ersetzt. 31 Dialogtexte de/en; Wiki-Quelle „Gebäudeimport“ Abschnitt „Zonen zuordnen“.
- **Tests:** `ZonenplanTests` 17, `ZonenplanDatenbankTests` 2, `GebaeudeImportZonenplanHuelleTests` 5, bunit `GebaeudeImportZonenbaumDialogTests` 8 und eine Stilblatt-Wache; Diagnose der sechs HottCAD-Dateien mit Plan je Z6-Vorschlag (keine Datei mit nicht zugeordneten Räumen).

## 4 Entscheide der Orchestrierung

1. Nutzung nur für beheizte Zonen und nur für Büro und Wohnen vorbelegt; Sanitär, Verkehr, Lager, Technik, Sport, Gastronomie allein ergeben „keine“.
2. Ergibt der Plan genau eine Zone, bleibt der Einzonenweg; Name und Nutzung dieser Zone werden nicht übernommen, der Dialog sagt das am Schalter.

## 5 Abweichungen

Dritter Raumzustand „außerhalb“ (ZB-1); Nutzung über den KP2-Arbeitsstand statt `KonditionierungsvorlageCtrl` (Lüftungsvorlage teilt den Gebäudeluftwechsel); ein Raumhaken nach dem ersten Schritt ist selbst ein Schritt ohne Rückfrage; Hinweis zur OK-Sperre über der Schlussleiste, Leiste unverändert.

## 6 Abnahme

Gate 730 im Hauptbaum auf `688cdb7e`: Kern-Filter 0 Fehler, ChartProben 208 gleich, Kern 10 993 grün (3 übersprungen), UI 7 528, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), Dokumentationswachen 35/35, Referenzlauf 20/20 PASS gegen R36, 608/608 byte-gleich, gestörter Lauf PASS. Windows-Schale auf Linux: 0 Fehler.

## 7 Offen

- Ziehen mit der Maus (zweite Stufe); Pfeiltasten im Baum (`role=tree`).
- Gespeicherter Plan derselben Datei wird noch nicht als Startplan geladen (`ZonenplanCtrl.Gespeichert` vorhanden).
- Leere Zonen und der Merker „angelegt“ überleben den erneuten Import nicht; dafür wäre `Tab_Zone.Nutzung`/`Angelegt` (Schemaschritt) nötig — E81 (05.10.2026): kein Schemaschritt, bleibt so.
- DIN-V-18599-Nutzungsprofile aus der `.sqproj` (Befund `aktuell/Gebaeudesimulation/2026-10-05_Befund_HottCAD_Projektdatei.md`) als eigene Welle.
- Windows-Sichtabnahme des Zonenbaums (Anwender); Logbuch-Satz (Version offen): „Der Gebäudeimport zeigt die Zonen als Baum mit einer Liste nicht zugeordneter Räume; Zonen lassen sich anlegen, löschen, benennen und mit einer Nutzung versehen, Räume, Geschosse oder der Rest nach Regel lassen sich zuordnen.“

## 8 Aufwand

Rund 173 Agentenaufrufe, 2 PT.
