# Importproben — Herkunft und Lizenzstand

Die Dateien dieses Ordners sind die Proben der Importleser in `EPOS.Kern.Tests`. Sie liegen als
gewöhnliche Blobs (kein LFS) und sind in `.gitattributes` mit `-text` von der
Zeilenenden-Normierung ausgenommen: Kodierung und Zeilenenden bleiben byteweise, wie sie sind
(Windows-1252 bei den VDI-3805-Ausschnitten, UTF-16LE bei `gbxml_haus_utf16.xml`).

Eine Zeile je Probe. „Eigenes Werk" heißt: im Projekt selbst geschrieben oder erzeugt, ohne
Fremddaten. Wo die Herkunftspapiere keine Lizenz nennen, steht „nicht belegt".

## Gebäudeimport gbXML (Stufe G4c)

Selbst erzeugt am 24.09.2026 nach Datenaustauschkonzept 8.3 (keine gbxml.org-Beispieldateien, keine
Werkzeugexporte): neutrale Bezeichner, runde Werte, keine Normzahlen und keine Herstellerdaten.
Die Fuß- und die UTF-16-Datei entstanden aus derselben Gebäudedefinition wie die SI-Datei.

| Datei | Zweck | Herkunft | Lizenzstand |
|---|---|---|---|
| `gbxml_haus_si.xml` | Probenhaus in Meter/Celsius, UTF-8: drei beheizte Räume über einem unbeheizten Keller, Außenwände in vier Himmelsrichtungen mit 23 m² Fenstern und einer Außentür, Flachdach, Kellerdecke, Innenwand, Geschossdecken; unsymmetrische Außenwand mit Innendämmung, Ostwand des Obergeschosses nur als `PolyLoop`, ein Fenstertyp mit zwei g-Werten (0° und 60°) | selbst erzeugt | eigenes Werk |
| `gbxml_haus_fuss.xml` | dasselbe Gebäude in Feet/SquareFeet/CubicFeet/°F und BTU-Einheiten, exakt umgerechnet; ein Heizsollwert mit lokalem `unit="C"`, ein Raum mit Fläche je Person (Probe 6) | selbst erzeugt | eigenes Werk |
| `gbxml_haus_utf16.xml` | die SI-Datei als UTF-16LE mit BOM (Probe 5) | selbst erzeugt | eigenes Werk |
| `gbxml_ohne_konstruktionen.xml` | ohne `Construction`, `Material` und `WindowType` — U-Werte aus der Baualtersklasse (Probe 7) | selbst erzeugt | eigenes Werk |
| `gbxml_rwert_schicht.xml` | ein Aufbau mit einer Schicht nur mit R-Wert — masselos (Probe 8) | selbst erzeugt | eigenes Werk |
| `gbxml_nachbar_leer.xml` | ein `spaceIdRef` ohne Ziel — gilt als unbeheizt | selbst erzeugt | eigenes Werk |
| `gbxml_ohne_nachbar.xml` | eine Außenwand ohne `AdjacentSpaceId` und eine Verschattungsfläche | selbst erzeugt | eigenes Werk |
| `gbxml_norddrehung.xml` | `CADModelAzimuth` 60° — gemeldet, nicht aufaddiert (Probe 21) | selbst erzeugt | eigenes Werk |
| `gbxml_kennung_lang.xml` | eine Flächenkennung mit 80 Zeichen (Probe 24) | selbst erzeugt | eigenes Werk |
| `gbxml_zwei_gebaeude.xml` | zwei Gebäude in einer Datei, eines je Lauf (U13) | selbst erzeugt | eigenes Werk |
| `gbxml_nettoflaeche_negativ.xml` | ein Fenster größer als seine Wand — Nettofläche 0 (U14) | selbst erzeugt | eigenes Werk |
| `gbxml_innenflaechen_teilweise.xml` | drei beheizte Räume (60 m², 3 m hoch) mit vollständigen Außenaufbauten, zwei massiven Innenwänden und einer Ständerwand nur mit R-Wert — die Innenflächen sind nicht vollständig (Bauteilvorschlag G4b, innere Masse nach Datenlage) | selbst erzeugt | eigenes Werk |
| `gbxml_zonen_viele.xml` | 60 beheizte Räume zu je 10 m² auf zwei Geschossen, je eine Außenwand und Bodenplatte bzw. Dach, keine Innenflächen — je Raum eine Zone (X3) ergibt 60 Zonen über der Obergrenze 50: Warnung mit dem Vorschlag der Geschossregel X2 (Zonenimport G6c, M12) | selbst erzeugt | eigenes Werk |

## Gebäudeimport IFC (Stufe G4a)

Selbst erzeugt am 25.09.2026 mit xBIM (`MemoryModel` im Schreibmodus) durch die Testhilfe
`EPOS.Kern.Tests/IfcProbenErzeuger.cs` — deterministisch (feste `GlobalId`s, fester Zeitstempel,
feste Kopfangaben, CRLF); `IfcProbenTests` hält fest, dass eine erneute Erzeugung byte-gleich wäre
(beim `.ifczip` der entpackte Inhalt). Neutrale Bezeichner, runde Werte, keine Normzahlen und keine
Herstellerdaten; nichts aus dem Netz. Längen in Millimetern, Flächen in m² ohne Prefix. Die
KIT-Probe `AC20-FZK-Haus.ifc` ist nicht aufgenommen (entscheidet der Anwender).

| Datei | Zweck | Herkunft | Lizenzstand |
|---|---|---|---|
| `ifc4_haus.ifc` | Probenhaus IFC4: ein Gebäude, zwei beheizte Vollgeschosse über einem Kellergeschoss mit unbeheiztem „Keller"; vier beheizte Räume (130 m², Höhen 2,6/2,4 m), Außenwände in vier Richtungen mit `BaseQuantities` bzw. `Qto_WallBaseQuantities` (eine nur mit Länge × Höhe), U-Wert am Wandtyp und zwei Vorkommniswerte, fünf Fenster mit `Pset_DoorWindowGlazingType` (eines nur mit Breite × Höhe), Haustür über `OverallWidth`/`OverallHeight`, Flachdach, Kellerdecke, Geschossdecke, Bodenplatte, Innenwand; 28 Raumgrenzen als Basisklasse mit `Name='2ndLevel'`/`Description='2a'`; TrueNorth [−2, 1, 0]; `Pset_BuildingCommon.YearOfConstruction = 'ca. 1965'`; Sollwert als Bereich mit `SetPointValue` | selbst erzeugt | eigenes Werk |
| `ifc2x3_haus.ifc` | dasselbe Haus in IFC2X3 (Sollwert als `SpaceTemperatureMin`, soweit abbildbar) — dieselben Zahlen | selbst erzeugt | eigenes Werk |
| `ifc4_haus.ifczip` | `ifc4_haus.ifc` im ZIP-Behälter — Größengrenze gegen die entpackte Größe | selbst erzeugt | eigenes Werk |
| `ifc4x1_kopf.ifc` | Kopf mit dem nicht angenommenen Schema IFC4X1 | selbst erzeugt | eigenes Werk |
| `ifc4_zwei_gebaeude.ifc` | zwei Gebäude, eines je Lauf (U13) | selbst erzeugt | eigenes Werk |
| `ifc4_ohne_mengen.ifc` | Raum und Außenwände ohne jeden Mengensatz — Meldung statt Geometrieableitung | selbst erzeugt | eigenes Werk |
| `ifc4_mapconversion.ifc` | Kontext mit TrueNorth UND `IfcMapConversion` (90°) — die Umrechnung gilt, TrueNorth wird nicht addiert | selbst erzeugt | eigenes Werk |
| `ifc4_schichten.ifc` | Schichtenhaus: ein beheizter Raum, vier Außenwände, Dach- und Bodenplatte ohne U-Werte, aber mit `IfcMaterialLayerSetUsage` und Stoffwerten in `Pset_MaterialThermal`/`Pset_MaterialCommon`; zwei Wände zählen innen zuerst gegen die Achse (`NEGATIVE`), zwei außen zuerst längs der Achse (`POSITIVE`) — Bauart aus den raumseitigen Schichten und Schichtfolge aus der Nutzung | selbst erzeugt | eigenes Werk |
| `ifc4_schichten_nullwerte.ifc` | dasselbe Haus, die Dämmung mit ρ = 0 und c = 0 — Stoffwerte ≤ 0 als Fehlstelle | selbst erzeugt | eigenes Werk |
| `ifc2x3_schichten.ifc` | dasselbe Haus in IFC2X3 mit `IfcThermalMaterialProperties`, `IfcGeneralMaterialProperties` und einem `IfcExtendedMaterialProperties` — die Stoffwerte werden benannt nicht gelesen | selbst erzeugt | eigenes Werk |
| `ifc4_rueckfaelle.ifc` | zwei Geschosse, Räume ohne Höhe und Volumen, Dach- und Bodenplatte ohne Mengen, keine Tür, Obergeschoss mit `GrossFloorArea` — die Vorgabe-Rückfälle für Raumhöhe, Dach-, Grund- und sonstige Fläche | selbst erzeugt | eigenes Werk |
| `ifc4_vorhangfassade.ifc` | Fassadenhaus: ein beheizter Raum (80 m²), zwei Vorhangfassaden (`IfcCurtainWall`, Süd 25 m² mit U-Wert 1,3, West 20 m² ohne U-Wert), Ost- und Nordwand mit U-Wert am Wandtyp, ein Fenster, Dach- und Bodenplatte — Vorhangfassaden im Bauteilvorschlag transparent, in den Summenfeldern unter „Sonstige Flächen" | selbst erzeugt | eigenes Werk |
| `ifc4_haus_materialnamen.ifc` | das Probenhaus (`ifc4_haus.ifc`) mit Schichtsätzen an Außenwänden, Dach, Keller- und Geschossdecke und Innenwand: Materialnamen, wie Autorensysteme sie schreiben (angehängte Kennungen, Marken wie „bewehrt"/„Verputzt", Namen deutscher und englischer Vorlagen, `Air`, eine Schraffur als Schicht, ein Sammelname ohne Treffer), die Stoffwerte in `Pset_MaterialThermal`/`Pset_MaterialCommon` voller Nullen — die Probe des Namensabgleichs N1…N7 im Bauteilvorschlag | selbst erzeugt | eigenes Werk |
| `ifc4_zonen.ifc` | Zonenhaus: Keller, Erd- und Obergeschoss mit sechs Räumen, Raumgrenzen der 2. Ebene, Polygone an einer Fassade über zwei Geschosse und an der Geschossdecke, ein Gegenstück der Datei, eine Grenze ohne Gegenstück, geschachtelte und mehrfache `IfcZone`, Klassifikation, ein Raum unter der Mindestgröße, Beheizungsregeln B3 und B5 — die Probe der Zonierung Z1, Z2, Z4 und des Vorschlags mehrerer Zonen (G6c) | selbst erzeugt | eigenes Werk |
| `ifc4_verlust.ifc` | von Hand geschriebene Kleinstdatei (< 5 KB), absichtlich beschädigt: ein unbekannter Entitätstyp und ein Verweis ins Leere — beide Verlustkanäle | von Hand geschrieben | eigenes Werk |

## Katalog-, Geräte-, Ganglinien- und Klimaimporte

| Datei | Zweck | Herkunft | Lizenzstand |
|---|---|---|---|
| `cec_module_50.csv` | 50 PV-Module mit Kopf-, Einheiten- und `[0]`-Zeile (Modulimport) | Ausschnitt aus `VDI-3805-Daten/PV/CEC Modules_UTC.csv` (iU9-W13) | CEC-Verwaltungsangabe, NREL-SAM-Fassung unter BSD-3-Clause (`VDI-3805-Daten/PV/LIESMICH_CEC_Inverters.md`) |
| `cec_module_gegenprobe.csv` | Kommentarzeile, Leerzeile, Komma im Anführungsfeld | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `cec_wechselrichter_21.csv` | 21 Wechselrichter der CEC-Liste (Wechselrichterimport, Konzept Wechselrichter S1.5) | Ausschnitt aus `VDI-3805-Daten/PV/CEC Inverters.csv` | wie `cec_module_50.csv` |
| `dwd_try_synthetisch_72h.dat` | 72 Stunden im DWD-TRY-Format, ausdrücklich keine amtlichen Daten (Konzept Klimadatenquellen) | selbst erzeugt, synthetisch | eigenes Werk |
| `ganglinie_mit_kopfzeile.txt` | Ganglinie mit Kopfzeile für den Kopfzeilenschalter (iU9-W14b) | Projektbestand (iU9-W13) | nicht belegt |
| `heizkessel_buderus.vdi` | drei Sätze mit Emissionswerten und Öl-Brennstoffindex (VDI 3805 Blatt 3) | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand unter `VDI-3805-Daten/`, nicht belegt |
| `heizkessel_vaillant.vdi` | fünf Sätze, Wirkungsgrad in Spalte 26 und Rückfall auf 710.01 Spalte 6 | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `ond_muster_10000tl_3profile.ond` | Wechselrichter mit drei ProfilPIO-Fassungen | selbst erzeugt, synthetisch (Konzept Wechselrichter, Anhang A) | eigenes Werk |
| `ond_muster_2500tl.ond` | Wechselrichter mit den Zahlen des Anhangs A | selbst erzeugt, synthetisch (Konzept Wechselrichter, Anhang A) | eigenes Werk |
| `pan_jinko_jkm260p.pan` | Moduldatei im PAN-Format | Kopie der PAN-Datei des Bestands unter `VDI-3805-Daten/PV/` (iU9-W13) | Herstellerdatei, nicht belegt |
| `pan_lg_320n1k.pan` | Moduldatei im PAN-Format | Kopie der PAN-Datei des Bestands unter `VDI-3805-Daten/PV/` (iU9-W13) | Herstellerdatei, nicht belegt |
| `pan_panasonic_vbhn325.pan` | Moduldatei im PAN-Format | Kopie der PAN-Datei des Bestands unter `VDI-3805-Daten/PV/` (iU9-W13) | Herstellerdatei, nicht belegt |
| `pan_trina_tsm650.pan` | Moduldatei im PAN-Format, bifazial ohne `Bifacial`-Schlüssel | Kopie der PAN-Datei des Bestands unter `VDI-3805-Daten/PV/` (iU9-W13) | Herstellerdatei, nicht belegt |
| `pufferspeicher_vaillant.vdi` | Trinkwasserabschnitt und zehn Blöcke, neun Sätze (VDI 3805) | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `pufferspeicher_weishaupt.vdi` | Solarspeicher neben Pufferspeicher | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `pvgis_tmy_stuttgart_72h.json` | 72 Stunden in PVGIS-Form, deterministisch gerechnet — kein mitgeschnittener PVGIS-Lauf (iU9-W14c) | selbst erzeugt, synthetisch | eigenes Werk |
| `solarganglinie_8760.txt` | Kopfzeile und 8 760 Werte (Ganglinienimport, iU9-W14b) | Projektbestand | nicht belegt |
| `solarkollektoren_gegenprobe.vdi` | alle vier Bauarten und der Bezugsflächen-Rückfall | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `solarkollektoren_vaillant.vdi` | Flach-, Röhrenkollektor und leere Bruttofläche | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `stromspeicher_bslib_7.csv` | die vollständige `bslib_database.csv`, unverändert (Konzept Stromspeicherimport) | bslib, HTW Berlin / FZ Jülich, DOI 10.5281/zenodo.6514527 | CC BY 4.0 (Datenbank) |
| `stromspeicher_cec_ess_23.csv` | echte Zeilen der CEC Energy Storage System List, unverändert: Titel-, Stand- und Kopfzeilen, 23 Geräte (Konzept Stromspeicherimport) | California Energy Commission | Public Records Act, Namensnennung, kommerzielle Nutzung eingeschränkt (Konzept Stromspeicherimport 1.4) |
| `waermebedarf_8760.txt` | 8 760 Stundenwerte, ein Wert je Zeile (Ganglinienimport, iU9-W13) | Projektbestand | nicht belegt |
| `waermebedarf_gegenprobe_komma.txt` | Dezimalkomma | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `waermebedarf_gegenprobe_leerzeile.txt` | Leerzeile in der Reihe | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `waermebedarf_gegenprobe_semikolon.txt` | Semikolon als Trenner | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `waermepumpen_gegenprobe_aufstellung.vdi` | Aufstellungsindex 7 außerhalb der Tabelle | selbst erzeugt, Gegenprobe (iU9-W13) | eigenes Werk |
| `waermepumpen_hoval.vdi` | drei Sätze, Voll- und Teillast getrennt (VDI 3805 Blatt 22) | Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
| `waermepumpen_hoval_ohne_abschluss.vdi` | drei Blöcke ohne Abschluss, ein Satz | gekürzter Ausschnitt aus dem Herstellerkatalog unter `VDI-3805-Daten/` (iU9-W13) | wie der Katalogbestand, nicht belegt |
