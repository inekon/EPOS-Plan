# Protokoll G7e — IFC-Export S3 mit schematischen Körpern (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle G7e (E67, E69), ein Opus-Agent, Commits `a503499`, `5bac5f5`, `6af7b83`, Merge `e76ddd2`. Statuszeile #712.
**Entscheid:** keiner neu beim Anwender; zwei Entscheide der Orchestrierung (Abschnitt 4). Kein Schemaschritt, Basis unverändert (Referenzlauf nicht betroffen).

## 1 Auftrag

Die IFC-Datei des Gebäudeexports um schematische Körper aus dem Zonengeometrie-Modell ergänzen (Datenaustauschkonzept 6.7, Stufe S3), nach derselben Regel wie gbXML Stufe 2, deutlich gekennzeichnet als schematisch und kein Aufmaß.

## 2 Vorgehen

Ein Opus-Agent (76 Aufrufe) in `EPOS.Kern/Allgemein/Export/Ifc/` (`IfcKoerper.cs` neu, `IfcSchreiber.cs` ergänzt); ein konfliktfreier Merge.

## 3 Ergebnis

- **Regel:** Körper nur, wenn das Modell schematische Rechtecke liefert; bei abgelehnter Anordnung S1 ohne Körper mit Vermerk, ohne Umriss S1 ohne Vermerk.
- **Kontext:** Darstellungskontext „Model“ (3D, Genauigkeit 1e-5, `TrueNorth` +Y = Nord) mit Unterkontext „Body“, Längeneinheit Meter.
- **Räume:** `IfcRectangleProfileDef` → `IfcExtrudedAreaSolid` (Tiefe Raumhöhe, ab Bodenlage) → `IfcShapeRepresentation` („Body“/„SweptSolid“) → `IfcProductDefinitionShape` am `IfcSpace`.
- **Bauteile:** Wand, Boden, Decke als Platte 0,1 m nach außen (rechteckig `IfcRectangleProfileDef`, sonst `IfcArbitraryClosedProfileDef` mit `IfcPolyline`); Fenster und Türen als Platte 0,05 m, 0,1 m vor der Wand, nichts ausgeschnitten, das geometrielose `IfcOpeningElement` aus G7c bleibt. Zusammengefasste Bauteile und innere Masse ohne Körper; Meldungen `GEXP_PROT_FLAECHE_OHNE_POLYGON`, `GEXP_PROT_OEFFNUNG_BEGRENZT` wie gbXML.
- **Kennzeichnung** an vier Stellen: `IfcProject.Name` „… (schematisch)“ samt Beschreibung, `FILE_DESCRIPTION`, `Description` je Produkt mit Körper, `IfcAnnotation` „EPOS-Plan“ im Gebäude; bei Widerspruch S1 mit `GEXP_IFC_DATEI_ABGELEHNT` und `GEXP_IFC_GEOMETRIE_ABGELEHNT`.
- **Vorschau** trägt dieselben Geometriemeldungen wie gbXML; Umfangstext `GEXP_STUFE_DATEN` und die Beipacktexte nennen S3.
- **Tests** `IfcKoerperTests` (12): Probe 27 (jeder Körper gegen `Zonenkoerper` auf 1e-6), Placement-Kette, Validator grün (Gegenprobe ohne Placement bricht ab), Kennzeichnung de/en, Probe 12 byte-gleich, Rundlauf über `IfcLeser` mit und ohne Körper gleich (nur `IMP_IFC_PROT_KEIN_NORDEN` entfällt in S3), Anreicherung (G7d) bleibt geometriefrei.
- **Beispieldatei** Referenzprojekt 1052 (132 547 Byte): 33 `IfcExtrudedAreaSolid` (3 Räume, 22 Flächen, 8 Fenster), 48 Placements, 1 Annotation.

## 4 Entscheide der Orchestrierung

- **Gleiche Regel wie gbXML, kein Schalter:** Der Dialog bekommt keine Wahl; Körper entstehen, wenn das Modell Rechtecke liefert.
- **Placement im Ursprung statt Azimutdrehung:** Jedes `IfcProduct` trägt einen `IfcLocalPlacement` auf einer gemeinsamen `IfcAxis2Placement3D` im Ursprung ohne Drehung (Kette Site → Building → Produkt); Lage und Richtung stehen allein in der `Position` des Körpers, die Koordinaten des Modells bleiben unverändert. Der eigene Leser liest so dieselben Azimute wie in S1.

## 5 Abweichungen

Konzept 6.7 sah den Azimut als Drehung des Placements vor; ersetzt durch den zweiten Entscheid aus Abschnitt 4, im Konzept unter „So gebaut (G7e)“ vermerkt.

## 6 Abnahme

Kern-Filter Ifc|Gbxml|GebaeudeExport|Resource|EinheitenWache|Ids 499/500 (1 übersprungen), UI GebaeudeExport 53/53. Gate-Zahlen und CI-Kennung trägt die Orchestrierung nach. Bestandsbefund: Der Bau von `EPOS.Kern.Tests` mit `-p:OhneXbim=true` ist rot (207 Fehler in 13 Testdateien mit xBIM-Typen, kein Bezug zu G7e; das Testprojekt hat keine OhneXbim-Bedingung).

## 7 Offen

- Probe 16 mit Prüfbildern aus Revit, Archicad und HiCAD der gesendeten Datei, Probe 15, Sichtabnahme — beim Anwender.
- Logbuch-Version.
- AK1z-Bundle (Testdatenbank 181) pushen.

## 8 Aufwand

Opus 76 Aufrufe; Papiere Sonnet.
