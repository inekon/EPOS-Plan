# Konzept DA — Datenaustausch der Simulationsdaten: Import und Export gbXML/IFC auf alles Simulationsrelevante

**Stand 12.10.2026 · Konzept zur Entscheidung, nichts gebaut.** Gelesen auf `96171bb` (Zweig `ios_migration_september`,
Basis R52, Schema 221). Auftrag der Sitzung „Gebäudesimulation“. Entscheide **E135** (DIN/TS-18599-10-Profile mit Werten) und
**E136** (Datenaustausch auf alles Simulationsrelevante) in der
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md). Setzt das
[Konzept Datenaustausch gbXML/IFC](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (G4c, G7a–G7f) fort und nimmt Welle MA-8
des [Entwurfs Geführter Modellaufbau](2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md) in sich auf.

**Wortlaut des Anwenders (12.10.2026, drei Vorgaben):**

> „Erweitere EPOS-Plan für den Import z. B. gbXML mit Lasten und Zeitplänen (jeweils Import und Export).
> Erweitere generell den Export und Import auf alles, was für die Simulation relevant ist.
> DIN/TS 18599-10: Profile sind nicht lizenzpflichtig. Aufnehmen und bei Import prüfen und zuordnen.“

**Ergänzung des Anwenders (12.10.2026):** Fenster, transparente Bauteile und solare Einstrahlung werden je Format ausdrücklich
geführt — auch die Größen, die der Kern heute als Konstante rechnet (Abschnitt 2, Gruppe E).

Dieses Papier nennt **keine Normwerte**: keine Zahl aus einer Tabelle der DIN/TS 18599-10, keine Kennwerte eines Produkts. Die
Werte der DIN-Profile kommen vom Anwender (Abschnitt 5). Element- und Eigenschaftsnamen der Formate stammen aus Code und Befund R.
Was davon noch an der Schemakopie gbXML 6.01 oder an der IFC4-Spezifikation zu prüfen ist, steht mit „(prüfen)“ dabei.

## 0 Das Ergebnis in Punkten

Fragen F1–F8 am 13.10.2026 nach Empfehlung entschieden (E138).

- **Der Bestand trägt Geometrie, Bauteile und Schichten in beiden Formaten fast vollständig.** Er trägt aber **keine Zeitpläne**
  im Standardformat. gbXML verliert die Konditionierungskalender ganz (benannter Verlust „Kalender“). IFC trägt sie nur in den
  eigenen Sätzen `EPOS_Kalender_*`, die allein EPOS-Plan liest. Lasten liest gbXML heute nur als Gebäudemittel, nicht je Zone.
- **Inventar: 71 simulationsrelevante Daten** in sieben Gruppen (Standort und Gebäude, Zone, Konditionierung, Bauteil, Aufbau und
  Baustoff, Fenster und Strahlung, Kopplung und Erdreich). Davon trägt heute:

  | | gelesen und geschrieben | nur gelesen | nur geschrieben | nur `EPOS_*` | fehlt |
  |---|---|---|---|---|---|
  | **gbXML** | 27 | 3 | 0 | — | 41 |
  | **IFC** | 21 | 0 | 6 | 14 | 30 |

  Die Zählung steht in Abschnitt 3.4. Drei der fehlenden Daten rechnet der Kern als Konstante (F8–F10).
- **Die drei größten Lücken je Format:**
  - **gbXML:** (1) alle **zehn** Kalenderdaten (Gruppe C: fünf Größen, Feiertage, Perioden, Zeilenbild, Wochen) fehlen — die
    `Schedule`-Kette wird weder gelesen noch geschrieben; (2) **elf** Zonendaten fehlen (Nacht-, Wochenend- und Feriensollwert,
    Höchsttemperatur, Höchstleistungen, Nutzerluftwechsel, Außenluft je Person, Wärme je Person, Strahlungsanteil, Vorheizzeit,
    Übergabe Heizen und Kühlen); Belegung, Geräte und Licht kommen nur als Gebäudemittel an; (3) **acht** der zehn Daten der
    Fenster- und Strahlungsgruppe (Rahmenanteil, Verschattung, Lichttransmission, winkelabhängiges g nur bei 0°, Emissionsgrad der
    Verglasung, Absorptions- und Emissionsgrad opak, Albedo).
  - **IFC:** (1) **sechs** Lasten- und Belegungsdaten fehlen in den Standardsätzen (`Pset_SpaceOccupancyRequirements`,
    `Pset_SpaceThermalLoad` werden weder gelesen noch geschrieben); (2) von den **zehn** Kalenderdaten stehen sieben nur in
    `EPOS_*` und sind für Fremdwerkzeuge unsichtbar, drei fehlen ganz (Zeilenbild, benannte Wochen, Lichtplan); (3) **acht** Daten der Fenster- und Strahlungsgruppe fehlen (wie gbXML, `Pset_DoorWindowShadingType`
    und die optischen Materialsätze bleiben ungenutzt).
- **Zielbild Import:** gbXML-`Schedule` → Konditionierungskalender (Woche mit 168 Zellen, Perioden). Fraktionspläne mal Nennwert
  ergeben die Kalender Geräte und Personen. Lasten und Sollwerte stehen je Zone statt als Gebäudemittel. IFC-Standardsätze werden
  ebenso gelesen. Was nicht abbildbar ist, wird **benannt verworfen**. Je Zone wird das **DIN-Profil geprüft und zugeordnet**
  (E135), in der Rangfolge DIN-Nummer → Raumtyp der Quelle → Gebäudeart → EPOS-Muster. Das Ergebnis zeigt eine Ampel der
  Modellprüfung (MA-1).
- **Zielbild Export:** Alle ergänzten Daten gehen in beide Formate. gbXML schreibt `Schedule` aus den Kalendern (Perioden als
  `WeekSchedule`-Folge, Ferien als eigene Woche) und die Lasten je Zone. IFC schreibt die Standardsätze und behält `EPOS_*` als
  Rückfall. **Round-Trip-Anspruch:** Export → Import im eigenen Format verliert nichts, gehalten von einem Rundlauftest je Format.
- **Formatwahl bleibt:** IFC für die Rückgabe ans CAD-Programm (G7d), gbXML für Simulationswerkzeuge. Nach der Erweiterung
  **verliert gbXML weniger** an ein Fremdwerkzeug: `Schedule`, `OAFlowPerPerson` und `PeopleHeatGain` sind dort Standardelemente,
  die Simulationswerkzeuge lesen. IFC trägt dieselben Daten nur in Sätzen, die diese Werkzeuge selten auswerten.
- **E135, DIN-Profile mit Werten:** Die Kategorie DIN des Katalogs der Nutzungsprofile (45 Profile, heute ohne Werte) wird mit
  Werten gesät und ausgeliefert, **sobald der Anwender die Werte liefert**. Die Sitzung schreibt keinen Wert aus dem Gedächtnis.
  Dafür ist ein Schemaschritt nötig: angemeldet wird die nächste freie Nummer, heute **222**, vergeben ist sie damit nicht.
  „Normwerte einlesen…“ (NP7) bleibt als Abgleichweg.
- **Wellen DA-1 bis DA-9, rund 17,5–24,5 PT.** gbXML Lasten und Zeitpläne kommen zuerst (DA-1 Import, DA-2 Export), dann IFC
  (DA-3), dann die Restdaten (DA-4) und die Profilprüfung (DA-5). Die DIN-Saat (DA-6) hängt an der Lieferung der Werte. DA-8
  (Absorptions- und Emissionsgrad je Bauteil) ist optional. Rechenweg und Basis bleiben unberührt. Nur DA-8 mit gefülltem Feld an
  einem Referenzprojekt würde die Basis berühren.

## 1 Bestand

### 1.1 Papiere und Stufen

| Quelle | Was gilt |
|---|---|
| [Konzept Datenaustausch](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) Kap. 2 | gemeinsames Zuordnungsgerüst beider Formate (Herkunft je Feld, `Tab_Importquelle`, `Tab_Importzuordnung`, ein Zuordnungsdialog) |
| dort 3.4, 3.7 | gbXML-Import G4c: `Schedule`-Kette, `Results`, `AirLoop` **nicht gelesen**; Baualtersklasse, ψ·L und Schichtrichtung fehlen im Schema; `spaceTypeEnum` ohne Wohnwert |
| dort 5.2–5.6 | gbXML-Export G7a (Daten) und G7b (Quadergeometrie), Version **6.01** (`versionEnum` endet dort), `Results` je Zone |
| dort 6.3–6.7 | IFC-Export G7c (semantisch), G7d (Anreicherung der Originaldatei), G7e (schematische Körper); G7f Raumkörper-Leser |
| [ADR-004](../ADR-004_gbXML_LINQ_to_XML.md) | gbXML-Leseweg LINQ to XML mit handgeschriebenem Modell, tolerant; XSD-Prüfung nur für den Export |
| [Befund R](2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md) 1.6, 1.8 | `Construction` und `Material` tragen `Absorptance`, `Roughness`, `Emittance`; `WindowType` trägt `U-value`, `SolarHeatGainCoeff` (mit `solarIncidentAngle`), `Transmittance`, `Reflectance`, `Emittance`, `ShadingCoeff`, dazu `Glaze`, `Gap`, `Frame`, `Blind`; `Schedule` → `YearSchedule` → `WeekSchedule` → `Day` → `DaySchedule` → `ScheduleValue`, Verweise `scheduleIdRef`, `lightScheduleIdRef`, `equipmentScheduleIdRef`, `peopleScheduleIdRef` am `Space`, `heatSchedIdRef`, `coolSchedIdRef`, `airChangesSchedIdRef` an der `Zone` |
| Statuszeilen G4–G7 | G4, G4b, G5, G6a–G6d, G7 (G7a–G7f) umgesetzt; Mehrzonenimport aus IFC und gbXML, Exporte beider Formate |
| [Entwurf MA](2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md) 1.3, 1.4 | Befund zum Export (Tabelle 1.4), Profilregel 2.4, F8, MA-8 |

### 1.2 Code

| Ort | Inhalt |
|---|---|
| `EPOS.Kern/Allgemein/Import/Gbxml/` | `GbxmlLeser` (Campus/Location, Building, Space, Zone, Construction/Layer/Material, WindowType), `GbxmlVokabular` (Enumerationen, Flächenarten), `GbxmlEinheiten`, `GbxmlKoerper`, `GbxmlAbbild` |
| `EPOS.Kern/Allgemein/Import/Ifc/` | `IfcAbbildBauer` (Räume, Zonen aus `IfcSpatialZone` THERMAL, Bauteile, `IfcMaterialLayerSet`, Psets, `EPOS_Zone`/`EPOS_Kalender_*` in `KonditionierungLesen`), `IfcEigenschaften`, `IfcLeser` |
| `EPOS.Kern/Allgemein/Import/Gebaeude/` | formatfreies Abbild (`GebaeudeAbbild`), Aggregation auf Gebäude und Zonen, Zonenplan, Raumnutzungsvorbelegung, Baujahrregel, Baustoffabgleich |
| `EPOS.Kern/Allgemein/Import/Sqproj/` | Leser der HottCAD-Projektdatei; `Din18599Nutzung` (Profilnummer → Nutzung) |
| `EPOS.Kern/Allgemein/Export/Gbxml/` | `GbxmlSchreiber` (Campus/Location mit PLZ und `CADModelAzimuth`, Building, Space, Surface/Opening, Construction/Layer/Material, WindowType, Zone, `Results`, DocumentHistory) |
| `EPOS.Kern/Allgemein/Export/Ifc/` | `IfcSchreiber`, `IfcAnreicherung` (G7d), `IfcKonditionierungssatz` (`EPOS_Zone` und `EPOS_Kalender_<Größe>` mit Woche und Periodentext), `IfcErgebnisse` (`EPOS_Ergebnis`) |
| `EPOS.Kern/Allgemein/Export/Gebaeude/GebaeudeExportVerluste.cs` | Einstufung je Feld für den gbXML-Rundlauf: **Zone 39 Felder** (6 rundläufig, 26 benannter Verlust, 7 nicht Gegenstand), **Bauteil 20** (9 / 5 / 6), **Gebäude 113** (7 / 76 / 30), dazu der Verlust „Kalender“ |

### 1.3 Was der Bestand liest und schreibt (Kurzfassung)

| Datenart | gbXML lesen | gbXML schreiben | IFC lesen | IFC schreiben |
|---|---|---|---|---|
| Standort | `Latitude`, `Longitude` (ins Abbild, nicht in die Klimaregion), `CADModelAzimuth` (gemeldet, nicht angewandt) | `Location` nur mit PLZ, `CADModelAzimuth` | `TrueNorth`, `RefElevation` (Geometrie) | `RefLatitude`, `RefLongitude`, `TrueNorth` |
| Gebäude | `buildingType` | `buildingType` | `Pset_BuildingCommon.YearOfConstruction` | dasselbe und `EPOS_Gebaeude` (Gebäudeart, Baualtersklasse) |
| Zone/Raum | `Area`, `Volume`, `conditionType`, `spaceType` (Hinweis), `AirChangesPerHour`, `PeopleNumber`, `LightPowerPerArea`, `EquipPowerPerArea`, `DesignHeatT`, `DesignCoolT` | dieselben | `RoomType`, `HeatingType`, `InsideTemperature`, `Pset_SpaceThermalRequirements` (Heizgrenzen), `Pset_SpaceCommon`, `EPOS_Zone`, `EPOS_Kalender_*` | `Pset_SpaceThermalRequirements` (`SpaceTemperature`, `SpaceTemperatureSummerMax`, `DiscontinuedHeating`, `NaturalVentilationRate`), `EPOS_Zone`, `EPOS_Kalender_*` |
| Zeitpläne | — | — (Verlust „Kalender“) | nur `EPOS_Kalender_*` | nur `EPOS_Kalender_*` |
| Bauteile | `surfaceType`, `AdjacentSpaceId`, `Tilt`, `Azimuth`, Fläche, `U-value` | dieselben | Bauteilklassen, `IsExternal`, Mengen, `ThermalTransmittance`, Raumgrenzen | `Pset_*Common`, `EPOS_Bauteil` (Randbedingung, `WaermebrueckeUA`) |
| Schichten | `Thickness`, `Conductivity`, `Density`, `SpecificHeat`, `R-value` | dieselben | `IfcMaterialLayerSet`, `LayerThickness`, `Pset_MaterialThermal`, `Pset_MaterialCommon.MassDensity` | dieselben, `EPOS_Baustoff` |
| Fenster | `WindowType`/`Opening`: `U-value`, `SolarHeatGainCoeff` (Wert bei 0°) | dieselben | `ThermalTransmittance`, `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` | `Pset_WindowCommon.ThermalTransmittance`, `SolarHeatGainTransmittance` |
| Ergebnisse | — | `Results` je Zone | — | `EPOS_Ergebnis`, `EPOS_Rechenlauf` |

**Nicht gelesen** in IFC: `Pset_SpaceOccupancyRequirements`, `Pset_SpaceThermalLoad` (`AirExchangeRate` ausdrücklich nicht
benutzt), `Pset_SpaceThermalDesign`, `Pset_SpaceLightingRequirements`, `RoomVentilationType`, `RatedMinAirExchangeRate`,
`IfcZone` (3.5 Nr. 8), `RefLatitude`. **Nicht gelesen** in gbXML: `Schedule`-Kette, `OAFlowPerPerson`, `PeopleHeatGain`,
`Location/Elevation`, optische Kennwerte (`Absorptance`, `Emittance`, `Transmittance`, `Reflectance`), `Frame`, `Blind`.

## 2 Inventar der simulationsrelevanten Daten

Gegenstand sind die Eingänge der Gebäudesimulation (VDI 6007, Zonenschleife, Bauteilweg) und der Konditionierung. Spalten aus
dem Schema der Testdatenbank (`PRAGMA table_info`, Schema 221). **Pflicht** heißt: Ohne das Datum rechnet der Lauf nicht, oder es
gibt keine Vorgabe. **Optional** heißt: Der Lauf erbt oder nimmt eine Vorgabe. **Konstante** heißt: Der Kern rechnet einen
festen Wert ohne Feld.

**A Standort und Gebäude**

| Nr | Datum | EPOS (Tabelle.Spalte) | Pflicht |
|---|---|---|---|
| A1 | Breite, Länge | `Tab_Klimaregion.Latitude`, `.Longitude` über `Tab_Projekt.ID_Klimaregion` | Pflicht (Sonnenstand) |
| A2 | Klimaregion, Klimazone | `Tab_Projekt.ID_Klimaregion`, `Tab_Klimaregion.Klimazone_DIN4710` | Pflicht |
| A3 | Höhe über NN | kein Feld (steckt im Klimadatensatz) | — |
| A4 | Nordrichtung | `Tab_Importquelle.Nordwinkel_Grad`, `Nordwinkel_Herkunft` | Pflicht beim Import |
| A5 | Baujahr, Baualtersklasse | `Tab_Gebaeude.Baujahr`, `.Baualtersklasse` | optional (steuert Vorgaben) |
| A6 | Gebäudeart, Wohn/Nichtwohn | `Tab_Gebaeude.Gebaeudeart`, `.Wohngebaeude_Nicht_Wohngebaeude` | optional (Profilregel) |
| A7 | Bauweise, Masseanteil | `Tab_Gebaeude.Bauweise`, `.Masseanteil_Aussen`, `.Innenflaechenfaktor` | Pflicht im Klassenweg |
| A8 | Wochenendtage | `Tab_Gebaeude.Wochenendtage` | optional (Vorgabe Sa + So) |
| A9 | Feiertagsland | `Tab_Gebaeude.Feiertagsland` | optional (Vorgabe bundeseinheitlich) |
| A10 | Ferien | `Tab_Gebaeude.Ferienbeginn_1…4`, `Ferienende_1…4`, `Ferien`; Perioden Art `FERIEN` | optional |

**B Zone**

| Nr | Datum | EPOS | Pflicht |
|---|---|---|---|
| B1 | Bezeichner | `Tab_Zone.Bezeichner` | Pflicht |
| B2 | Nutzfläche | `Tab_Zone.Nutzflaeche` | Pflicht |
| B3 | Volumen, Raumhöhe | `Tab_Zone.Volumen`, `.Raumhoehe` | Pflicht |
| B4 | beheizt | `Tab_Zone.IstBeheizt` | Pflicht |
| B5 | Nutzungsprofil | `Tab_Zone.Nutzungsprofil` (Textkopie), Katalog `Tab_Raumnutzungsprofil` | optional |
| B6 | Heizsollwert Tag | `Tab_Zone.Raumsolltemperatur_Tag` | Pflicht (erbt vom Gebäude) |
| B7 | Heizsollwert Nacht | `Tab_Zone.Raumsolltemperatur_Nachtabsenkung` | optional |
| B8 | Sollwert Wochenende, Ferien | `.Raumsolltemperatur_Wochenende`, `.Raumsolltemperatur_Ferien` | optional |
| B9 | Kühlsollwert Tag, Nacht | `.Kuehl_Sollwert`, `.Kuehl_Sollwert_Nacht`, `.Kuehlung_Aktiv` | optional |
| B10 | Höchsttemperatur | `.Maximaleraumtemperatur` | optional |
| B11 | Heiz- und Kühlleistung höchstens | `.Heizleistung_Max`, `.Kuehlleistung_Max` | optional |
| B12 | Infiltrationsluftwechsel | `.Luftwechsel_Infiltration` | Pflicht (erbt) |
| B13 | Nutzerluftwechsel | `.Luftwechsel_Nutzer` | optional |
| B14 | Außenluft je Person und je Fläche | Profil `Aussenluft`, `Aussenluft_Einheit`, `Aussenluft_Person` | optional |
| B15 | Belegung | `.Bewohner`; Profil `Personen_Flaeche` | optional |
| B16 | Wärmeabgabe je Person | Profil `Personen_Waerme`; Kalender `PERSONEN.Nennwert` | optional |
| B17 | Geräteleistung | `.Interne_Waermegewinne` (Summe); Profil `Geraete_Leistung`; Kalender `GERAETE.Nennwert` | optional |
| B18 | Beleuchtungsleistung | Profil `Beleuchtung_Leistung`, `Beleuchtung_Anteil` (an der Zone in `Interne_Waermegewinne`) | optional |
| B19 | Strahlungsanteil der Heizung | `.Heizung_Strahlungsanteil` | optional |
| B20 | Vorheizzeit | `.Vorheizzeit_H` | optional (erbt) |
| B21 | Übergabe Heizen je Zone | `.Uebergabe_Art`, `_Exponent`, `_Leistung_Nenn`, `.Auslegung_Vorlauf`, `_Ruecklauf`, `_Raumtemperatur`, `.Regler_Proportionalband` | optional (nur Anlagenkopplung) |
| B22 | Übergabe Kühlen je Zone | `.Kuehl_Uebergabe_Art`, `_Exponent`, `_Leistung_Nenn` | optional |

**C Konditionierung** (`Tab_Konditionierungskalender`, `-periode`, `-woche`, `-vorgabe`; je Gebäude oder Zone)

| Nr | Datum | EPOS | Pflicht |
|---|---|---|---|
| C1 | Kalender Heizsollwert | `Groesse = HEIZSOLL`: `Wert`/`Aus`/`Woche` (168 Zellen), Perioden | optional (sonst Matrix) |
| C2 | Kalender Kühlsollwert | `KUEHLSOLL` | optional |
| C3 | Kalender Lüftung | `LUEFTUNG` | optional |
| C4 | Kalender Geräte | `GERAETE` mit `Nennwert` | optional |
| C5 | Kalender Personen | `PERSONEN` mit `Nennwert` | optional |
| C6 | Feiertage | Perioden Art `FEIERTAG`, `Feiertagsregel` (17 Regeln) | optional |
| C7 | Betriebspause, Heiz- und Kühlperiode | Perioden Art `BETRIEBSPAUSE`, `ZEITRAUM` | optional |
| C8 | Zeilenbild (Tag, Nacht, Wochenende, Ferien, bedingt) | `Tab_Konditionierungsvorgabe` (`Zeile`, `Von`, `Bis`, `Bedingt_K`) | optional |
| C9 | benannte Wochen | `Tab_Konditionierungswoche` (`Name`, `Groesse`, `Woche`) | optional |
| C10 | Beleuchtungszeitplan | kein eigener Kalender (im Profil `Beleuchtung_Anteil`) | — |

**D Bauteil** (`Tab_Bauteil`)

| Nr | Datum | EPOS | Pflicht |
|---|---|---|---|
| D1 | Bauteilart | `Bauteilart` | Pflicht |
| D2 | Fläche | `Flaeche` | Pflicht |
| D3 | Neigung | `Neigung` | Pflicht (Vorgabe je Art) |
| D4 | Azimut | `Azimut` | Pflicht |
| D5 | Randbedingung | `Randbedingung` | Pflicht |
| D6 | Nachbarzone, Trennfläche | `ID_Nachbarzone`, `Trennflaeche_Zuordnung` | optional |
| D7 | U-Wert | `U_Wert` | Pflicht ohne Schichten |
| D8 | ψ·L | `Psi_L` | optional |

**E Aufbau und Baustoff** (`Tab_Bauteilaufbau`, `Tab_Bauteilschicht`, `Tab_Baustoff`)

| Nr | Datum | EPOS | Pflicht |
|---|---|---|---|
| E1 | Schichtfolge, Dicke | `Tab_Bauteilschicht.Reihenfolge`, `.Dicke`, `Bauteil.ID_Aufbau` | Pflicht im Bauteilweg |
| E2 | λ | `.Lambda` (`Lambda_Bemessung`) | Pflicht |
| E3 | ρ | `.Rho` | Pflicht |
| E4 | c | `.cp` | Pflicht |
| E5 | Luftschicht, R der Schicht | `.IstLuftschicht`, `.R_Quelle` | optional |
| E6 | inhomogene Abschnitte | `.ID_Abschnitt`, `EN6946_Balkenbreite`, `_Gefachbreite`, `_Versatz`, `_Drehung` | optional |
| E7 | Übergangswiderstände | `Tab_Bauteilaufbau.Rsi`, `.Rse` | optional |

**F Fenster, transparente Bauteile und solare Einstrahlung**

Einstrahlung je Fläche nach Hay-Davies mit dem Sonnenstand aus Länge und Breite der Klimaregion und einer Albedo
(`GebaeudeKlimaweg.cs`). Vorgaben in `GebaeudeFestwerte.cs`: `VORGABE_RAHMENANTEIL`, `VORGABE_VERSCHATTUNGSFAKTOR` (Vorgabe,
wenn das Feld leer ist), `ABSORPTIONSGRAD_OPAK`, `EMISSIONSGRAD_AUSSEN` (feste Konstanten ohne Feld).

| Nr | Datum | EPOS | Pflicht |
|---|---|---|---|
| F1 | U_w (U_g, U_f) | `Tab_Bauteil.U_Wert`; `Tab_Bauteilaufbau.Ug`, `.Uf`, `.U_Quelle` | Pflicht |
| F2 | g-Wert | `Tab_Bauteil.g_Wert`, `Tab_Bauteilaufbau.g_Wert` | Pflicht (erbt vom Gebäude) |
| F3 | Rahmenanteil | `Tab_Bauteil.Rahmenanteil`, `Tab_Bauteilaufbau.Rahmenanteil`, `.Rahmenbreite` | optional (Vorgabe) |
| F4 | Verschattung, Sonnenschutz | `Tab_Bauteil.Verschattungsfaktor`, `Tab_Bauteilaufbau.Verschattungsfaktor` | optional (Vorgabe) |
| F5 | Lichttransmission | kein Feld, nicht gerechnet | — |
| F6 | winkelabhängiger g-Wert (VDI 6007 Blatt 2) | kein Feld, nicht gerechnet | — |
| F7 | Emissionsgrad der Verglasung | kein Feld, nicht gerechnet | — |
| F8 | Absorptionsgrad opaker Außenflächen | **Konstante** `ABSORPTIONSGRAD_OPAK` | — |
| F9 | langwelliger Emissionsgrad außen | **Konstante** `EMISSIONSGRAD_AUSSEN` | — |
| F10 | Albedo der Umgebung | Konstante im Klimaweg | — |

**G Kopplung und Erdreich**

| Nr | Datum | EPOS | Pflicht |
|---|---|---|---|
| G1 | Luftströme zwischen Zonen | `Tab_Zonenluftstrom` (`ID_ZoneA`, `ID_ZoneB`, `Volumenstrom`) | optional |
| G2 | Bodenplatte und Randbedingung | `Tab_Gebaeude.Grundflaeche`, `.Grundflaeche_Randbedingung`; Bauteile mit Randbedingung Erdreich | Pflicht im Klassenweg |
| G3 | Umfang P nach DIN EN ISO 13370 | `Tab_Gebaeude.Abmessung_Anschluß_Außenwand_Kellerdecke` | optional |
| G4 | wirksamer U-Wert Erdreich, Kellertemperatur | `.Erdreich_U_Wirksam`, `.Kellertemperatur` | optional |

**Nicht Gegenstand:** Anlagentechnik (Erzeuger, Speicher, Kreise, Heizkurve am Gebäude) und Bedarf (Brauchwasser, Prozesswärme,
Stromverbraucher), siehe Abschnitt 8.

Zählung: A 10, B 22, C 10, D 8, E 7, F 10, G 4 — zusammen **71 Daten**. Ohne eigenes Feld sind A3, C10, F5, F6 und F7; B14, B16
und B18 stehen nur im Profil bzw. an der Zone als Summe. Sie bleiben im Inventar, weil Fremdformate sie tragen und ein Rundlauf
sie nicht verlieren soll.

## 3 Abbildung je Format

Kürzel je Format und Richtung: **✓** Standardelement, gelesen und geschrieben · **L** nur gelesen · **S** nur geschrieben ·
**E** nur im eigenen Satz `EPOS_*`, gelesen und geschrieben · **–** fehlt.

### 3.1 gbXML 6.01

| Nr | Element | heute | Lücke und Ziel |
|---|---|---|---|
| A1 | `Campus/Location/Latitude`, `Longitude` | L | schreiben aus der Klimaregion; beim Import Vorschlag der nächsten Klimaregion (kein Klimaimport, E80) |
| A2 | `Location/Name`, `ZipcodeOrPostalCode` | – | Name der Klimaregion schreiben, beim Import nur Hinweis |
| A3 | `Location/Elevation` | – | durchreichen; kein Feld (Klimadatensatz) |
| A4 | `Location/CADModelAzimuth` | ✓ | — |
| A5 | — | – | kein Element (Konzept 3.7); Rückfall siehe 3.3 |
| A6 | `Building/@buildingType` | ✓ | — |
| A7 | — | – | Rückfall 3.3 |
| A8 | `WeekSchedule/Day/@dayType` | – | aus der Verteilung der Tagestypen ableiten (Import), schreiben (Export) |
| A9 | — (`dayType` Holiday ohne Regel) | – | Rückfall 3.3; die Feiertage selbst siehe C6 |
| A10 | `YearSchedule/BeginDate`, `EndDate` | – | Ferien als eigene `WeekSchedule` im Zeitraum |
| B1–B4 | `Zone/Name`, `Space/Area`, `Volume`, `@conditionType` | ✓ | — |
| B5 | `Space/@spaceType` | L | Raumtyp der Quelle für die Profilregel (DA-5); Export der nächstliegenden Enumeration (prüfen) |
| B6 | `Zone/DesignHeatT` | ✓ | — |
| B7, B8 | `heatSchedIdRef` → `Schedule` Typ `Temp` | – | über C1 |
| B9 | `DesignCoolT`, `coolSchedIdRef` | ✓ (Tag) | Nacht über C2 |
| B10, B11, B19 | — | – | Rückfall 3.3 |
| B12 | `Space/AirChangesPerHour` | ✓ | — |
| B13 | `Zone/AirChangesPerHour` + `airChangesSchedIdRef` | – | lesen und schreiben, Zeitplan über C3 |
| B14 | `Zone/OAFlowPerPerson`, `OAFlowPerArea` (prüfen) | – | lesen und schreiben |
| B15 | `Space/PeopleNumber` (drei Einheiten) | ✓ | heute nur als Gebäudemittel übernommen; Ziel je Zone |
| B16 | `Space/PeopleHeatGain` (`heatGainType`, prüfen) | – | lesen und schreiben |
| B17 | `Space/EquipPowerPerArea` | ✓ | heute Gebäudemittel; Ziel je Zone, Zeitplan `equipmentScheduleIdRef` |
| B18 | `Space/LightPowerPerArea` | ✓ | heute Gebäudemittel; Ziel je Zone, `lightScheduleIdRef` |
| B20–B22 | — | – | Rückfall 3.3 |
| C1–C5 | `Schedule` (`Temp`, `Fraction`, `OnOff`) über die Verweise an `Space` und `Zone` | – | **Kern der Welle DA-1/DA-2**, Abbildung 4.1 und 4.3 |
| C6 | `Day/@dayType` Holiday + `YearSchedule` je Feiertag | – | Feiertage als Tageszeiträume; die Regel (beweglich) geht verloren, das Datum im Gemeinjahr bleibt |
| C7 | `YearSchedule`-Folge | – | Perioden als Wochenfolge |
| C8, C9 | — | – | aus der Woche abgeleitet; das Zeilenbild selbst nur im Rückfall |
| D1, D2, D3, D4, D5 | `Surface/@surfaceType`, `RectangularGeometry`, `Tilt`, `Azimuth`, `AdjacentSpaceId` | ✓ | — |
| D6 | `AdjacentSpaceId` × 2 | ✓ | `Trennflaeche_Zuordnung` nur im Rückfall |
| D7 | `Construction/U-value` | ✓ | — |
| D8 | — | – | Rückfall 3.3 |
| E1–E4 | `Construction/LayerId`, `Layer/MaterialId`, `Material/Thickness`, `Conductivity`, `Density`, `SpecificHeat` | ✓ | — |
| E5 | `Material/R-value` | ✓ | — |
| E6 | — | – | Rückfall; im Export die homogenisierte Ersatzschicht (benannt) |
| E7 | `Layer/InsideAirFilmResistance`, `HOutside` (prüfen) | – | lesen und schreiben |
| F1 | `WindowType/U-value`, `Opening/U-value` | ✓ | — |
| F2 | `SolarHeatGainCoeff` | ✓ | — |
| F3 | `WindowType/Frame` (Detailobjekt, prüfen) | – | Anteil aus `Frame` oder Rückfall |
| F4 | `ShadingCoeff`, `Blind` (prüfen) | – | Verschattungsfaktor schreiben und lesen |
| F5 | `Transmittance` (`surfaceType` sichtbar, prüfen) | – | durchreichen (F8 der Fragen) |
| F6 | `SolarHeatGainCoeff@solarIncidentAngle` | L (nur 0°) | übrige Winkel benannt verwerfen, bis ein Feld besteht |
| F7 | `WindowType/Emittance` | – | durchreichen |
| F8 | `Construction/Absorptance`, `Material/Absorptance` | – | Export schreibt die Konstante, Import meldet Abweichung (F8 der Fragen) |
| F9 | `Construction/Emittance` | – | wie F8 |
| F10 | — | – | nicht Gegenstand eines Bauteils |
| G1 | — (`AirLoop` ist Anlagentechnik) | – | Rückfall 3.3 |
| G2 | `surfaceType` `UndergroundSlab`, `SlabOnGrade` | ✓ | — |
| G3, G4 | — | – | Rückfall 3.3 |

### 3.2 IFC4

| Nr | Element | heute | Lücke und Ziel |
|---|---|---|---|
| A1 | `IfcSite.RefLatitude`, `RefLongitude` | S | lesen wie gbXML A1 |
| A2 | — | – | `EPOS_Gebaeude` |
| A3 | `IfcSite.RefElevation` | – (nur für die Geometrie gelesen) | durchreichen; kein Feld |
| A4 | `IfcGeometricRepresentationContext.TrueNorth` | ✓ | — |
| A5 | `Pset_BuildingCommon.YearOfConstruction`; `EPOS_Gebaeude.Baualtersklasse` | ✓ | — |
| A6 | `EPOS_Gebaeude.Gebaeudeart`; `Pset_BuildingCommon.OccupancyType` (prüfen) | S | lesen; Standard schreiben |
| A7 | — | – | `EPOS_Gebaeude` |
| A8, A9 | — | – | `EPOS_Gebaeude` (`Wochenendtage`, `Feiertagsland`) |
| A10 | `EPOS_Kalender_*` Periode `FERIEN` | E | Gebäudeferien zusätzlich in `EPOS_Gebaeude` |
| B1–B4 | `IfcSpace`/`IfcSpatialZone` Name, Mengen, `EPOS_Zone.IstBeheizt` | ✓ | — |
| B5 | `RoomType`, `Pset_SpaceCommon.Category`, `ObjectType` (gelesen); `EPOS_Zone.Nutzung` | E | DIN-Nummer schreiben und lesen (DA-5) |
| B6 | `Pset_SpaceThermalRequirements.SpaceTemperature…`; `EPOS_Zone.Heizsollwert_Tag` | ✓ | — |
| B7 | `EPOS_Zone.Heizsollwert_Nacht`; `DiscontinuedHeating` | E | — |
| B8 | — | – | über C1 im Kalender, sonst `EPOS_Zone` |
| B9 | `SpaceTemperatureSummerMax`; `EPOS_Zone.Kuehlsollwert` | E (Standard nur S) | Standard lesen |
| B10 | `Pset_SpaceThermalRequirements.SpaceTemperatureMax` (prüfen) | – | lesen und schreiben |
| B11 | `Pset_SpaceThermalDesign` (`TotalHeatLoss`, prüfen) | – | schreiben als Auslegung, lesen als Vorschlag |
| B12 | `EPOS_Zone.LuftwechselInfiltration` | S | lesen; Standard `Pset_SpaceThermalLoad.InfiltrationSensible` passt nicht (Leistung, keine Rate) |
| B13 | `NaturalVentilationRate`; `EPOS_Zone.Luftwechsel_Nutzer`; `Pset_SpaceThermalLoad.AirExchangeRate` | E (Standard nur S) | Standard lesen; `RatedMinAirExchangeRate` (herstellereigen) als Vorschlag mit Herkunftsmarke |
| B14 | `Pset_SpaceThermalRequirements.MechanicalVentilationRate` (prüfen) | – | lesen und schreiben, sonst `EPOS_Zone` |
| B15 | `Pset_SpaceOccupancyRequirements.OccupancyNumber`, `AreaPerOccupant`; `EPOS_Zone.Personen` | S (nur EPOS) | Standard lesen und schreiben |
| B16 | `Pset_SpaceThermalLoad.People`; `EPOS_Kalender_PERSONEN.Nennwert_W` | E | Standard lesen und schreiben |
| B17 | `Pset_SpaceThermalLoad.EquipmentSensible`; `EPOS_Kalender_GERAETE.Nennwert_W` | E | Standard lesen und schreiben |
| B18 | `Pset_SpaceThermalLoad.Lighting`; `Pset_SpaceLightingRequirements` | – | lesen und schreiben |
| B19–B22 | — | – | `EPOS_Zone` |
| C1–C5 | `EPOS_Kalender_<Größe>` (Woche 168 Zellen, Periodentext) | E | bleibt; Standard nur nach F6 der Fragen (`IfcTimeSeries`, `IfcWorkCalendar`) |
| C6, C7 | Periodentext in `EPOS_Kalender_*` | E | — |
| C8, C9 | — | – | `EPOS_Kalender_*` um Zeilenbild und Wochennamen erweitern |
| D1–D5 | Bauteilklassen, Mengen, Platzierung, `IsExternal`, Raumgrenzen, `EPOS_Bauteil.Randbedingung` | ✓ | — |
| D6 | `IfcRelSpaceBoundary` | ✓ | `Trennflaeche_Zuordnung` in `EPOS_Bauteil` |
| D7 | `Pset_*Common.ThermalTransmittance` | ✓ | — |
| D8 | `EPOS_Bauteil.WaermebrueckeUA` | S | lesen |
| E1–E4 | `IfcMaterialLayerSet(Usage)`, `LayerThickness`, `Pset_MaterialThermal`, `Pset_MaterialCommon.MassDensity` | ✓ | — |
| E5 | `EPOS_Baustoff` (`Waermedurchlasswiderstand`) | S | lesen |
| E6 | `IfcMaterialConstituentSet` (prüfen) | – | Rückfall `EPOS_Baustoff`; im Export die Ersatzschicht |
| E7 | — | – | `EPOS_Bauteil` |
| F1 | `Pset_WindowCommon.ThermalTransmittance` | ✓ | — |
| F2 | `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` | ✓ | — |
| F3 | `Pset_WindowCommon.GlazingAreaFraction` (= 1 − Rahmenanteil, prüfen) | – | lesen und schreiben |
| F4 | `Pset_DoorWindowShadingType` (Verschattungskoeffizienten), `Pset_WindowCommon.HasSunProtection` (prüfen) | – | lesen und schreiben |
| F5 | `Pset_DoorWindowGlazingType.VisibleLightTransmittance` | – | durchreichen |
| F6 | — | – | nicht abbildbar |
| F7 | `Pset_MaterialOptical` (Emissionsgrade, prüfen) | – | durchreichen |
| F8, F9 | `Pset_MaterialOptical` (solare Reflexion, langwellige Emission, prüfen) | – | wie gbXML F8 |
| F10 | — | – | nicht Gegenstand |
| G1 | — | – | `EPOS_Zone` (Partner und Volumenstrom) |
| G2 | `Pset_SlabCommon.IsExternal`, Lage, Raumgrenze zum Erdreich | ✓ | — |
| G3, G4 | — | – | `EPOS_Gebaeude` |

### 3.3 Was ohne Standardelement geht

- **IFC:** Eigene Eigenschaftssätze sind erlaubt. `EPOS_Zone`, `EPOS_Bauteil`, `EPOS_Baustoff`, `EPOS_Gebaeude` und
  `EPOS_Kalender_*` bestehen schon. Neue Felder wachsen dort. Ein Standardsatz hat Vorrang. `EPOS_*` trägt nur, was kein
  Standardsatz trägt, und zusätzlich die Zeitpläne (Konzept Datenaustausch 6.4 und 6.5).
- **gbXML:** Ob das Schema 6.01 einen Erweiterungspunkt hat (`xs:any`, `anyAttribute` oder ein freies Element), ist **an der
  Schemakopie zu prüfen** (erster Schritt von DA-1). Die Kopie liegt nicht im Repositorium (Konzept Datenaustausch 8.2) und wird
  aus dem Testbaum geladen. Die drei Wege in dieser Folge:
  1. ein gültiger Erweiterungspunkt mit eigenem Namensraum `urn:epos-plan:gbxml:1`, wenn das Schema einen hat;
  2. sonst Konvention in `DocumentHistory` oder `Description` eines Objekts: ein Block `EPOS:` mit Schlüssel=Wert. Er bleibt
     schemagültig und wird von Fremdwerkzeugen übergangen;
  3. sonst Verlust, benannt in `GebaeudeExportVerluste`.

  Die Exportprüfung gegen die XSD (Befund R 4.4) muss grün bleiben. `CADObjectId`/`Name` werden **nicht** als
  Datenträger missbraucht.

### 3.4 Zählung und Formatwahl

Je Inventardatum (71) gilt heute:

| | ✓ | nur L | nur S | E | – |
|---|---|---|---|---|---|
| gbXML | 27 | 3 (A1, B5, F6) | 0 | — | 41 |
| IFC | 21 | 0 | 6 (A1, A6, B12, B15, D8, E5) | 14 (A10, B5, B7, B9, B13, B16, B17, C1–C7) | 30 |

Fehlend je Gruppe — gbXML: A 7, B 11, C 10, D 1, E 2, F 7, G 3. IFC: A 5, B 9, C 3, D 0, E 2, F 8, G 3.

**Nach dem Zielbild** (Abschnitt 4) tragen beide Formate alle 71 Daten im eigenen Rundlauf. **Ohne Standardelement**, also
nur über den Rückfall 3.3 und damit für ein Fremdwerkzeug unsichtbar, bleiben in **gbXML 16** Daten (A5, A7, A9, B10, B11,
B19–B22, C8, C9, D8, E6, G1, G3, G4) und in **IFC 26** (A2, A7–A10, B12, B19–B22, C1–C9 nach F6 (a), D8, E5–E7, G1, G3, G4).
gbXML verliert damit an ein Simulationswerkzeug weniger. Für die Rückgabe an das CAD-Programm bleibt IFC das bessere Format,
weil G7d die Originaldatei anreichert und Geometrie und Kennungen unverändert lässt.

## 4 Zielbild

### 4.1 Import gbXML: Lasten und Zeitpläne

**Lasten und Sollwerte je Zone statt als Gebäudemittel.** Heute fasst `GebaeudeAggregation` `PeopleNumber`, `LightPowerPerArea`
und `EquipPowerPerArea` zum Gebäude zusammen und setzt innere Gewinne auf die Vorgabe E43. Das Ziel:

| gbXML | EPOS-Zone (Mehrzonenweg) | Regel |
|---|---|---|
| `DesignHeatT`, `DesignCoolT` | `Raumsolltemperatur_Tag`, `Kuehl_Sollwert` | wie heute |
| `Space/AirChangesPerHour` | `Luftwechsel_Infiltration` | flächengewichtet je Zone |
| `Zone/AirChangesPerHour`, `OAFlowPerPerson`, `OAFlowPerArea` | `Luftwechsel_Nutzer` (umgerechnet über Fläche, Höhe und Belegung, wie NP7 „Generator“) | Nennwert; Zeitplan C3 |
| `PeopleNumber`, `PeopleHeatGain` | `Bewohner`; Kalender `PERSONEN.Nennwert` | Nennwert × Fraktionsplan |
| `EquipPowerPerArea`, `LightPowerPerArea` | Kalender `GERAETE.Nennwert` (Geräte und Licht addiert, Licht mit eigenem Plan gewichtet) | Nennwert × Fraktionsplan |

**`Schedule` → Konditionierungskalender.**

1. Je `DaySchedule` 24 `ScheduleValue` → ein Tagesbild. Abweichende Anzahl → benannt verworfen.
2. Je `WeekSchedule` die `Day`-Einträge nach `dayType` (prüfen: `Weekday`, `Weekend`, `Mon`…`Sun`, `Holiday`, `All`) → eine
   Woche mit 168 Zellen, Montag 0 Uhr zuerst. `Holiday` wird zur Tagesart der Feiertagsperiode.
3. Je `YearSchedule` mit `BeginDate`/`EndDate` → Woche des Kalenders (der längste Zeitraum) oder eine Periode `ZEITRAUM` mit
   eigener Woche. Daten ohne Jahr ins Gemeinjahr. Ein Zeitraum mit `0`/`OnOff = 0` über alle Stunden wird `BETRIEBSPAUSE`
   (Heizperiode).
4. Typ `Temp` → `HEIZSOLL`/`KUEHLSOLL` (°C). Typ `Fraction` → `GERAETE`/`PERSONEN`/`LUEFTUNG` als Anteil 0…1 mit dem Nennwert
   aus 4.1. Typ `OnOff` → Anteil 0/1 bzw. `aus`.
5. **Nicht abbildbar, benannt verworfen** mit Protokollzeile: mehr Zeiträume, als der Kalender Perioden trägt (Rang ≤ 999);
   Pläne mit Zeitschritt ≠ 1 h; Typen außerhalb der Liste (`ResetTemp`, `ActivityLevel`); Sollwertpläne an Größen, die EPOS
   nicht führt (Feuchte); Werte außerhalb der Prüfklauseln der Kalendertabellen.
6. Bei Konflikt mit dem Profil gilt die Rangfolge des Bestands (GD-5: Ganglinie vor Datei vor Profil vor Erbe), Frage F2.

### 4.2 Import IFC

Gelesen werden die Standardsätze `Pset_SpaceOccupancyRequirements`, `Pset_SpaceThermalLoad`, `Pset_SpaceThermalRequirements`
vollständig (Sommer, Maximum, Lüftung), `Pset_SpaceLightingRequirements`, die Fenstersätze
(`Pset_WindowCommon.GlazingAreaFraction`, `Pset_DoorWindowShadingType`) und `IfcSite.RefLatitude`/`RefLongitude`. Danach folgen
`EPOS_*` als Rückfall. Herstellereigene Eigenschaften (`RoomVentilationType`, `RatedMinAirExchangeRate`) werden nur nach dem
Eigenschaftsnamen gelesen, unabhängig vom Satznamen. Sie werden nur als **Vorschlag mit Herkunftsmarke** übernommen, nie als
Normwert (Entwurf MA, Abgrenzung).

### 4.3 Prüfung und Zuordnung des DIN-Profils je Zone (E135)

Je Zone in dieser Rangfolge:

1. **DIN-Nummer:** Projektdatei `PdProfileUsage` (Bestand `Din18599Nutzung`), IFC `EPOS_Zone` oder eine Eigenschaft mit
   DIN-Nummer (neu), gbXML-Rückfall (3.3) über die Zuordnung `DIN_NUMMER`;
2. **Raumtyp der Quelle:** IFC `RoomType` → Nutzungsklasse (`RAUMTYP_KLASSE`), gbXML `spaceType` → Nutzungsklasse (neue Tafel,
   Zuordnungsart z. B. `GBXML_RAUMTYP`, ohne Schema: die Spalte `Art` ist Text), `HOTTCAD_RAUMTYP`;
3. **Gebäudeart** (`Zonenvorbesetzung.KENNUNG_JE_GEBAEUDEART`);
4. **EPOS-Muster** als letzter Rückfall.

**Prüfen** heißt: Das gefundene DIN-Profil trägt Werte (E135; vor der Saat nur nach NP7). Die Werte der Datei (Sollwert, Lasten,
Luftwechsel) werden je Größe gegen das Profil gestellt, und eine Abweichung über eine Schwelle wird als Hinweis genannt. Die
Schwelle ist eine Festlegung der Welle und kein Normwert. Die Ampel steht in Prüfgruppe 3 der Modellprüfung (MA-1): grün mit
DIN-Profil und Werten, gelb mit Muster oder Profil ohne Werte, rot ohne Profil an einer beheizten Zone. Die Zuordnung bleibt ein
Vorschlag mit Rücknahme („Alle Vorschläge übernehmen“).

### 4.4 Export

- **gbXML:** `Schedule` aus den Kalendern. Je Kalender gibt es eine `Schedule` mit `YearSchedule`-Folge: Die Grundwoche läuft
  über das ganze Jahr. Jede Periode `ZEITRAUM`/`BETRIEBSPAUSE` wird ein eigener `YearSchedule` mit `BeginDate`/`EndDate` (Jahr
  der Preisreihe oder ein festes Bezugsjahr, Gemeinjahr). Ferien werden eine eigene Woche, Feiertage Tage mit `dayType`
  Holiday. Lasten gehen je Zone als Nennwert mit Fraktionsplan, Luftwechsel und Außenluft je Zone. Die Rückfalldaten (3.3)
  werden geschrieben, wenn der Weg 1 oder 2 trägt. Die Verlustliste wird nachgeführt.
- **IFC:** Die Standardsätze (4.2) werden geschrieben. `EPOS_Zone`, `EPOS_Gebaeude` und `EPOS_Kalender_*` werden um die
  Rückfalldaten erweitert. G7d ersetzt eine vorhandene Eigenschaft und fügt keine zweite hinzu (Konzept Datenaustausch 6.6).
- **Round-Trip-Anspruch:** Export → Import im eigenen Format ohne Verlust über alle 71 Inventardaten. Ausgenommen ist, was
  `GebaeudeExportVerluste` als „nicht Gegenstand“ führt. Der Test liest die Tafel und das Inventar als Daten. Eine Wache hält
  beide deckungsgleich: Jedes neue Feld an `ZoneModel`, `BauteilModel` und `ProjektGebaeudeModel` ist eingestuft (wie heute
  per Reflexion).

## 5 DIN/TS-18599-10-Profile mit Werten (E135)

| Punkt | Festlegung |
|---|---|
| Gegenstand | die Kategorie „DIN/TS 18599-10“ (`Tab_Raumnutzungskatalog.Art = 'DIN_V_18599_10'`, ReadOnly): 45 Profile in `Tab_Raumnutzungsprofil` (43 Nutzungen der Tabelle 6 mit Nummer und zwei Wohnprofile ohne Nummer, Schritte 189/190/219), heute ohne Kennwert |
| Fassung | **DIN/TS 18599-10:2025-10**, wie Kategorie, `Quellenhinweis` und Zählung der Zuordnung `DIN_NUMMER` (E96). Der Auftrag nannte 2018-09; die Kategorie führt 2025-10 (Konzept Nutzungsprofile 5.2). Rückfrage F1 |
| Spalten | die 25 Kennwerte des Profils (`Nutzung_Von`, `Nutzung_Bis`, `Betrieb_Von`, `Betrieb_Bis`, `Nutzungstage_Woche`, `Nutzungstage_Jahr`, `Feiertage_Wie_Sonntag`, `Heiz_Soll`, `Heiz_Soll_Ausserhalb`, `Heiz_Aus_Ausserhalb`, `Kuehl_Soll`, `Kuehl_Soll_Ausserhalb`, `Kuehl_Aus_Ausserhalb`, `Aussenluft`, `Aussenluft_Einheit`, `Aussenluft_Ausserhalb`, `Personen_Flaeche`, `Personen_Waerme`, `Personen_Anteil`, `Personen_Anteil_Ausserhalb`, `Geraete_Leistung`, `Geraete_Anteil`, `Geraete_Anteil_Ausserhalb`, `Beleuchtung_Leistung`, `Beleuchtung_Anteil`) und `Aussenluft_Person` (Schritt 220); Zeilenbild und Stundenprofile nur, wenn die Lieferung sie trägt |
| Quelle | **der Anwender liefert** die Tabelle: CSV im Format des Konzepts Nutzungsprofile 6.4 (die Vorlage nach Weg 3 der NP7 erzeugt Kopfzeile, Nummern und Namen) oder einen Projektdatei-Export. Die Sitzung schreibt **keinen** Wert aus dem Gedächtnis; liegt keine Lieferung vor, ruht die Welle |
| Weg in die Auslieferung | eine Saatklasse `RaumnutzungDinSaat` mit den gelieferten Werten als Konstanten und ein Schemaschritt `RaumnutzungDinWerteSchema` (UPDATE der 45 Profile über Kategorie und Nummer bzw. Name, nur wo kein Wert steht; wiederholbar; ergebnisneutral). **Nummer: nächste freie, heute 222** — in der Statuszeile „Schemaschritt angemeldet" erst bei Baubeginn anmelden, nicht hier vergeben; Kette `BauteilkatalogSchema.SCHRITT + 1` |
| Herkunft | ausgelieferte Werte erhalten eine eigene Marke. Der CHECK von `Werte_Herkunft` lässt heute nur `NULL` oder `'EINGELESEN'` zu. Für eine dritte Marke (`'AUSGELIEFERT'`) ist ein **Tabellenneubau** nötig. Die Alternative ohne Neubau: `NULL` = ausgeliefert, die Fassung steht im `Quellenhinweis` der Kategorie. Festlegung in DA-6 nach Messung des Aufwands |
| Auslieferungsvorlage | Schritt 3d nimmt heute eingelesene Werte heraus. Die Prüfung „kein Kennwert in einer Normkategorie“ (NP-F21) wird umgekehrt: Die 45 DIN-Profile tragen die gesäten Werte. Eingelesene Werte des Anwenders werden weiter entfernt, gesäte bleiben. `Werkzeuge/Auslieferungsvorlage` und `AuslieferungsvorlagenWacheTests` werden nachgezogen |
| Katalogpaket | der Katalog der Nutzungsprofile reist heute nicht im Paket (Art Katalog, Schritt 220). Bleibt so: die Werte kommen mit dem Schemaschritt |
| NP7 | „Normwerte einlesen…“ bleibt als **Abgleichweg**: Vorschau der Abweichungen gegen die gesäten Werte, „überschreiben“ nur auf Zuruf, Rücknahme stellt die **gesäten** Werte wieder her (nicht leer) |
| Import | Prüfung und Zuordnung nach 4.3 |
| Wachen | Zählung der DIN-Profile mit Werten gegen eine Konstante der Saat; keine Hersteller- und Produktdaten (`WikiProduktdatenWacheTests` unberührt); Kennzeichnung der Fassung in `Quellenhinweis` und Saatkopf; Testdatenbank trägt die Saat über `Werkzeuge/Testdatenbankschema` |
| Basis | kein Referenzprojekt nutzt ein Nutzungsprofil (`Tab_Zone.Nutzungsprofil` überall leer, Testdatenbank 12.10.2026). Kein Rechenweg liest eine Kategorie; die Saat ist ergebnisneutral, der Referenzlauf bleibt byte-gleich. Einfrieren erst, wenn ein Referenzprojekt ein DIN-Profil zugewiesen bekommt |

## 6 Fragen an den Anwender — entschieden (E138, 13.10.2026)

1. **F1 Quelle und Fassung der DIN-Werte:** (a) CSV im NP7-Format, ausgefüllt aus der Normausgabe des Anwenders, Fassung
   DIN/TS 18599-10:2025-10; (b) Export einer HottCAD-Projektdatei, deren Profile die Werte tragen; (c) beides, CSV als Saat und
   Projektdatei als Gegenprobe. **Empfehlung: (c)**. Die CSV ist vollständig und prüfbar, die Gegenprobe findet Übertragungsfehler.
   Die Fassung bleibt 2025-10, weil Kategorie und Nummern so gesät sind. **Entschieden: (c)**
2. **F2 Zeitplan der Datei gegen Profil:** (a) Datei vor Profil; das Profil füllt nur fehlende Größen, Abweichungen als Hinweis;
   (b) Profil vor Datei. **Empfehlung: (a)**. Es entspricht der Rangfolge des Bestands (GD-5), und die Planung des Fachplaners
   bleibt erhalten. **Entschieden: (a)**
3. **F3 Nicht abbildbare Zeitpläne:** (a) auf Woche und Perioden verdichten, den Rest benannt verwerfen und im Protokoll nennen;
   (b) die ganze Datei ablehnen. **Empfehlung: (a)**. Ein Import mit benannten Verlusten ist schneller am Ziel (Leitziel MA). **Entschieden: (a)**
4. **F4 Umfang und Reihenfolge der ersten Welle:** (a) DA-1 und DA-2 (gbXML Import und Export) direkt nach MA-1, vor MA-2 bis
   MA-7; (b) erst alle MA-Wellen, dann DA; (c) nur DA-1. **Empfehlung: (a)**. Der Anwender hat gbXML mit Lasten und Zeitplänen
   ausdrücklich zuerst genannt, und MA-1 liefert die Ampel, in die DA-5 schreibt. **Entschieden: (a)**
5. **F5 Round-Trip-Test als Pflicht:** (a) jede DA-Welle mit Rundlauftest im eigenen Format über das Inventar und einer Wache
   „jedes Inventardatum eingestuft“; (b) nur Stichproben. **Empfehlung: (a)**. Nur so bleibt der Anspruch „ohne Verlust“
   prüfbar. **Entschieden: (a)**
6. **F6 IFC-Zeitprofile:** (a) Standardsätze für Nennwerte, Zeitpläne weiter in `EPOS_Kalender_*`; (b) zusätzlich
   `IfcRegularTimeSeries` bzw. `IfcWorkCalendar`/`IfcRecurrencePattern` für die Kalender. **Empfehlung: (a)**. Zielwerkzeuge
   lesen die Zeitreihen kaum. (b) erst, wenn ein Werkzeug sie nachweislich liest. **Entschieden: (a)**
7. **F7 Export der Ergebnisse:** (a) wie heute (`Results` je Zone in gbXML, `EPOS_Ergebnis` in IFC), der Import liest keine
   Ergebnisse; (b) erweitern um Monatswerte und Heizlast je Zone in beiden Formaten. **Empfehlung: (a)**. Die Ergebnisse
   gehören nicht zum Modell, und Konzept Datenaustausch 5.6 nennt `Results` „praktisch fragwürdig“. **Entschieden: (a)**
8. **F8 Absorptions- und Emissionsgrad, Fensterdetails:** (a) als optionale Felder je Aufbau mit Schemaschritt und Anschluss im
   Rechenweg (DA-8), Vorgabe = heutige Konstanten; (b) nur durchreichen: Der Export schreibt die Konstanten und die Fensterwerte,
   der Import liest sie und meldet Abweichungen, ohne Feld; (c) weglassen. **Empfehlung: (b) jetzt, (a) als eigene Entscheidung**.
   Fremdwerkzeuge rechnen dann mit den Werten von EPOS statt mit eigenen Vorgaben. (a) ändert den Rechenweg und braucht eine
   Prüfung gegen die Basis. **Entschieden: (b) jetzt, (a) als eigene Entscheidung**

## 7 Wellenplan

| Welle | Inhalt | Aufwand | Abnahme |
|---|---|---|---|
| **DA-1** gbXML-Import Lasten und Zeitpläne | Schemakopie auf Erweiterungspunkt und optische Elemente prüfen (3.3); `Schedule`-Kette lesen → Kalender (4.1); Lasten und Luftwechsel je Zone statt Gebäudemittel; `OAFlowPerPerson`, `PeopleHeatGain`; benannte Verwürfe; Fenster F3/F4 lesen | 3–4 PT | Kern-Tests an synthetischen Proben (runde Phantasiewerte) je Plantyp und Verwurf; Importproben unverändert bis auf die neuen Felder; Referenzlauf byte-gleich |
| **DA-2** gbXML-Export Lasten und Zeitpläne | Kalender → `Schedule` (4.4), Lasten je Zone, Rückfallweg 3.3, Konstanten F8/F9 nach F8 der Fragen; `GebaeudeExportVerluste` nachgeführt | 2,5–3,5 PT | Schemaprobe gegen die Kopie 6.01 grün; Rundlauf gbXML über das Inventar (F5); Verlustliste per Reflexion vollständig |
| **DA-3** IFC Standardsätze lesen und schreiben | 4.2; `EPOS_Zone`/`EPOS_Gebaeude` um Rückfalldaten; G7d ersetzt statt doppelt; Lesen von `RefLatitude`, `WaermebrueckeUA`, `Waermedurchlasswiderstand`, `LuftwechselInfiltration` | 2,5–3,5 PT | Rundlauf IFC (Schreiber und Anreicherung); xBIM-Protokoll ohne Verlust; Importproben |
| **DA-4** Restdaten beider Formate | `Tab_Zonenluftstrom`, Vorheizzeit, Übergabe je Zone, Höchstleistungen, Erdreich (G3, G4), Zeilenbild und Wochennamen | 2–3 PT | Rundlauf beider Formate um die Felder erweitert |
| **DA-5** Profilprüfung beim Import (E135) | Rangfolge 4.3, gbXML-Raumtyptafel, DIN-Nummer in IFC, Ampel in Prüfgruppe 3 der Modellprüfung | 1,5–2 PT | Kern-Tests je Rangstufe und Ampelfarbe; nach MA-1 |
| **DA-6** DIN-Saat (E135) | Saatklasse und Schemaschritt (Nummer bei Baubeginn anmelden, heute 222), Herkunftsmarke, Auslieferungsvorlage 3d, NP7 als Abgleich, Wachen (5) | 2–3 PT | Saat zählt 45 Profile mit Werten; SQL-Dialekt-Prüfer; Testdatenbankschema; Auslieferungsvorlage-Tests; Referenzlauf byte-gleich. **Hängt an der Lieferung der Werte (F1)** |
| **DA-7** Round-Trip-Wache | Inventar als Tafel im Testprojekt, Wache „jedes Feld eingestuft“, gemeinsamer Rundlauf beider Formate | 1,5–2 PT | Wache rot bei neuem, nicht eingestuftem Feld (Gegenprobe) |
| **DA-8** Absorptions- und Emissionsgrad je Bauteil (optional, nur bei F8 (a)) | **Schemabedarf:** zwei nullbare Spalten (Absorptionsgrad, Emissionsgrad, `CHECK` 0…1) an `Tab_Bauteilaufbau` und `Tab_Bauteilaufbau_STAMM`, ggf. an `Tab_Bauteil`; Rechenweg liest das Feld statt der Konstanten, leer = Konstante. Nummer nicht vergeben | 2–3 PT | Referenzlauf byte-gleich, solange kein Referenzprojekt ein Feld füllt; sonst neue Basis mit Einfrierregel |
| **DA-9** Wiki und Logbuch | Seiten „Gebäudeimport“, „Gebäudeexport“, „Nutzungsprofile“; Logbuch-Entwurf | 0,5 PT | Gegenlesemuster der Wurzel-`CLAUDE.md` ohne Treffer |

**Summe rund 17,5–24,5 PT**, ohne DA-8 15,5–21,5 PT. MA-8 des Entwurfs Geführter Modellaufbau (3–4 PT) geht in DA-2 und DA-3 auf.
Der Entwurf MA zählt damit 15,5–22,5 PT ohne MA-8.

**Reihenfolge:** MA-1 → DA-1 → DA-2 → DA-3. DA-4 und DA-7 können danach parallel laufen (verschiedene Dateien). DA-5 kommt nach
MA-1, DA-6 nach der Lieferung der Werte, DA-8 nur nach F8 (a), DA-9 zuletzt. Jede Welle läuft in einem eigenen Worktree, baut den
Kern-Filter und testet mit `--filter`. Das Gate fährt die Orchestrierung nach dem Merge. Die Windows-Schale wird kompiliert
geprüft, wenn eine Hülle des Import- oder Exportdialogs berührt wird.

**Rechenweg, Basis, Schema:** DA-1 bis DA-5 und DA-7 schreiben Daten wie der Import heute. Bestehende Gebäude und
Referenzprojekte werden nie beschrieben, und der Referenzlauf bleibt byte-gleich zu R52. Einen Schemaschritt brauchen nur DA-6
(Saat) und DA-8 (Felder). Beide melden ihre Nummer vor dem Bau an.

## 8 Abgrenzung

- **Keine Anlagentechnik** im Austausch (`AirLoop`, `HydronicLoop`, IFC-Verteilungselemente, Heizkurve, Erzeuger, Speicher);
  ebenso kein Bedarf (Brauchwasser, Prozesswärme, Stromverbraucher).
- **Kein Klimaimport** (E80): Breite und Länge der Datei ergeben höchstens einen Vorschlag der Klimaregion.
- **Keine Normwerte aus dem Gedächtnis** und keine Normtabelle außer den DIN/TS-18599-10-Profilen nach E135, und auch diese nur
  aus der Lieferung des Anwenders. U-Werte, Aufbauten und Baujahrestafeln aus Normen bleiben draußen (E131 im Übrigen).
- **Keine Hersteller- und Produktdaten**, keine Anwenderdateien und keine Werte daraus im Repositorium. Proben tragen runde
  Phantasiewerte.
- **Kein IFC2x3- oder IFC4.3-Schreibpfad** (E3), kein Geometriekernel, keine Geometriebearbeitung.
- **Keine Änderung am Rechenweg** außer optional DA-8 nach eigener Entscheidung.
- **Die HottCAD-Projektdatei** bleibt reiner Leseweg (Bestand `Sqproj`). Sie liefert die DIN-Nummer und Profile; ein Export in
  dieses Format ist nicht Gegenstand.
