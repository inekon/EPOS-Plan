# Protokoll G7b — gbXML Stufe 2 und 3D-Körperansicht (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle G7b (E66), Teilzweige als Worktrees, Merges `a9dbba2`, `e3c3d24`, `7bf23eb`, `39b2cff`. Statuszeile #709.
**Entscheid:** keiner neu. Kein Schemaschritt, Basis R34 unverändert (Referenzlauf nicht betroffen).

## 1 Auftrag

Schematische Zonengeometrie und Raumkörper aufbauen, gbXML Stufe 2 (Geometrie und Ergebnisse) schreiben, die 3D-Körperansicht mit three.js in den Gebäudebetrachter einbauen und den gbXML-Export damit ausliefern (F1, D2).

## 2 Vorgehen

Drei Opus-Agenten nacheinander in getrennten Dateien (70, 59 und 39 Aufrufe: Kern und Schreiber, Ansicht und Hülle, Papiere und Wachen) und ein Sonnet-Agent (8 Aufrufe: Dateilisten, Zählungen). Vier konfliktfreie Merges.

## 3 Ergebnis je Teil

- **Kern** (`f5707f9`, `6676a07`, `c1e5759`): Anordnung der Zonen (`Zonengeometrie.Anordnung.cs`) und Raumkörper (`Zonenkoerper.cs`); Nachbarpaare aus dem Gegenstück der Datei oder aus Bauteilen mit genau zwei Seiten.
- **gbXML Stufe 2** (`3761f37`, `90507bc`): `PolyLoop` an Flächen und Öffnungen, `ClosedShell` je Raum, `Results` je Zone; Kennzeichnung „schematisch“ an drei Stellen; Freigabe `GbxmlExportFreigegeben` dauerhaft an.
- **Ansicht** (`af8afac`, `43710b6`, `8c58e81`): three.js 0.186.1 lokal, Modul `epos-gebaeude-koerper.js`, Reiter „Körper“, DTO und Hülle `GebaeudeImportAnsicht`.
- **Lizenz und Papiere** (`1e25f51`, `536604d`, `5ece04e`, `e26b7a0`, `d41864f`): Lizenzhinweise Abschnitt 7 mit Wache, Hilfeanker, Wiki Gebäudeimport, Datenaustausch- und Mehrzonenkonzept, Logbuch-Satz.

## 4 Entscheide der Orchestrierung

- **Keine Drehung:** Der Azimut bleibt physikalisch; ein Paar mit der Trennwand auf derselben Himmelsseite bleibt getrennt (Hinweis `ZGEO_NICHT_ANGELEGT`), bei Überlappung lehnt `ZGEO_ANORDNUNG_ABGELEHNT` benannt ab.
- **Widerspruch → Stufe 1:** Lässt sich die Geometrie nicht widerspruchsfrei bilden, schreibt der Export Stufe 1 derselben Datei mit Vermerk, kein Einzelzonenmodell.
- **Zonenhöhe als Rückfall:** Höhe aus dem Raum, sonst aus der Zone, sonst kein Körper; die Legende nennt „Höhe als Vorgabe“.

## 5 Abweichungen

Nicht gebaut: Dachschrägen und `SurfaceReferenceLocation`.

## 6 Abnahme

Tests: `ZonengeometrieAnlegenTests` (5, Probe 25), `GbxmlStufe2Tests` (9, darunter Schemaprüfung gegen die XSD, Rundlauf, Azimut gegen PolyLoop), `GebaeudeImportAnsichtKoerperTests` (3), `GebaeudeAnsichtTests` GA3 (7), Lizenzwache; Sichtprobe mit Chromium. Gate: siehe Statuszeile #709; CI: Kern-Lauf nach dem Push nachzutragen.

## 7 Offen

- Beim Anwender: Sichtabnahme der Körperansicht unter Windows (WebView2) und iOS, Logbuch-Version, Wiki-Upload.
- Probe 20 (macOS) nur auf Zuruf.

## 8 Aufwand

Opus 70 + 59 + 39 Aufrufe, Sonnet 8 Aufrufe.
