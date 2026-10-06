# Protokoll HC-5 (+HC-5c) — Grundriss je Raum aus dem Dateikörper (06.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#779**. Opus-Agenten im Worktree: Teil A `4713e25f`, `98874efb`, `4f1eb465`, `b0534049`, `8483d5a7`; Teil B `be0e5204`, `9958601f`, `eaea4a12`, `f440e1f7`, `d4d8761b`; HC-5c `6e8fac2a`, `5c647e6b`, `8bbbeae7`, `d6e65516`, `46e36465`. Merges `f1d248e9`, `e1204e9a`, `154ded7f`, `56fca129` (Schemaschritt 191 an 190 `RaumnutzungDinTsSchema` gehängt, Testdatenbank von origin auf 191 gehoben). Schemaschritt 191, Basis unverändert.
**Entscheid:** E94 (F6–F11); Konzept [HottCAD-Verbund](../../../aktuell/Gebaeudesimulation/2026-10-05_Konzept_HottCAD_Verbund_IFC_Projektdatei_Viewer.md) Kapitel 11; Vorwelle [HC-4](2026-10-06_HC-4_Datei_erneut_lesen.md).

## 1 Auftrag

Der Grundriss je Raum wird aus dem Dateikörper abgeleitet, beim Import gespeichert und von Exportmodell, IFC-Export und gbXML-Export statt des angenommenen Rechtecks benutzt. Die volle Geometrie bleibt ungespeichert (E87 F3).

## 2 Gebaut

- **Kern** `Koerpergrundriss`: Bodendreiecke eines Raumkörpers, Projektion auf ganze Millimeter, Ringe mit Löchern; Rückfälle Decke → konvexe Hülle → Rechteck; Vermerke `Stufen`, `Ueberlappung`, `Splitter`, `Konvex`, `Dachschraege`, `Geschosslage`, `Flaeche`.
- **Schemaschritt 191 `RaumgrundrissSchema`:** `Tab_Raumgrundriss` (STRICT; je Raum Ringe in ganzen Millimetern, Boden, Höhe, Herleitung, Vermerke; Kaskade an der Importquelle, `ID_Zone` SET NULL) und `Tab_Importquelle.Nordwinkel_Grad`. Ablage beim Import (IFC aus dem Körper bzw. echten Raumgrenzen, gbXML aus `PolyLoop`), beim Duplizieren und in der Auslieferungsvorlage.
- **Verbraucher:** die Zonengeometrie nimmt die Grundrisse vor dem Rechteckersatz; Exportmodell, IFC-Export (je Zone ein `IfcSpace` mit `IfcExtrudedAreaSolid`, Löcher über `IfcArbitraryProfileDefWithVoids`) und gbXML-Export (`ClosedShell`) zeigen die echten Grundrisse; „schematisch“ steht nur noch bei Rechtecken.
- **HC-5c:** Nordwinkel aus `TrueNorth`/`IfcMapConversion` bzw. `CADModelAzimuth`; Platten an den Prismenkanten (Innen-, Trenn-, Außenkante; Wände nach Ausrichtung ±15°; Fenster und Türen auf der größten Platte ihrer Wand; Boden und Decke als Streifen); ohne Nordwinkel Modell-Nord mit Vermerk.
- **Oberfläche:** Ansicht „aus Dateikörper (Grundriss)“, Importprotokoll (Herleitungen, Flächenabweichung über 10 %), Knopf „Grundriss übernehmen“ in „Datei erneut lesen“ (F7; nur bei passender Prüfsumme, mit Rückfrage).
- Keine Rechengröße; Referenzlauf byte-gleich.

## 3 Diagnose an den sechs Anwenderdateien

Jeder Raum kommt aus `KoerperBoden` (Verwaltung 113, Produktion 49, Sportheim 56, MFH 1964 30, MFH 1984 29, WG-EH55 20 Räume). Flächenabweichung je Gebäude 0 % (Sportheim −2,5 %). Zugeordnete Wandfläche: WG-EH55 100 %, MFH 1984 98,5 %, MFH 1964 98,3 %, Sportheim 97,8 %, Produktion 96,5 %, Verwaltung 93,7 %. Keine Datei nennt einen Nordwinkel.

## 4 Abweichungen vom Konzept

1. Die Ableitung ist außerhalb von `GebaeudeGrundriss.Eingang` gebaut (`GebaeudeRaumgrundrisse.Bilden`); F6 gilt bei IFC nur mit echten Raumgrenzen.
2. Die Ansicht zeichnet Löcher nicht (der Export schon).
3. gbXML ohne `PlanarGeometry` des größten Rings und ohne Vermerke; `PlanarGeometry` je Fläche nur für die größte Platte.
4. Platten enden an der Prismenhöhe (der Rest bei Außen- gegen Innenmaß wird nicht gezeichnet), Lochkanten tragen keine Wandplatten, eine Trennwand wird keiner bestimmten Nachbarzone zugeordnet.

## 5 Gate 775

Gate 775 (Hauptbaum, `56fca129`, 60 min): 20 181 Tests, 20 177 grün, 4 übersprungen, 0 rot (KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 687, EPOS.Kern 11 520 mit 3 übersprungen); Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 21/21 gegen `2026-10-05_R38_Vorlaufwahl` PASS, 646/646 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 460 Texte, 0 Fundstellen; Auslieferungsvorlage-Tests 60/60

## 6 Offen

- **Sichtabnahme unter Windows:** Exportmodell mit Prismen und Platten, IFC und gbXML in einem fremden Betrachter, „Grundriss übernehmen“ an einem Bestandsgebäude.
- **Logbuch-Entwurf** (Version beim Anwender erfragen): „Exportmodell und IFC-/gbXML-Export zeigen importierte Gebäude mit ihren echten Raumgrundrissen; ‚Datei erneut lesen‘ kann den Grundriss für schon importierte Gebäude übernehmen.“
- **Wiki-Upload** „Gebäudeimport“ und „Gebäude“ gebündelt.
- Konzept 5.3 bleibt offen.
