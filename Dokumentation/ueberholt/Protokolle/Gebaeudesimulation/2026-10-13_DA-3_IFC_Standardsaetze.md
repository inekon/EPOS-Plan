# Protokoll DA-3 — IFC-Standard-Eigenschaftssätze lesen und schreiben, Rundlauf, Inventar-Einstufung IFC

Welle DA-3 aus dem [Konzept Datenaustausch der Simulationsdaten](../../../aktuell/Gebaeudesimulation/2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)
(Abschnitte 3.2, 3.3, 4.2, 4.4, 7). Grundlage ist Entscheid E138 (Anwender 13.10.2026): F2 (a) Datei vor Profil; F5 Rundlauftest
mit Wache „jedes Inventardatum eingestuft“; F6 (a) Standardsätze für Nennwerte, Zeitpläne weiter in `EPOS_Kalender_*`; F8 (b)
Absorptions- und Emissionsgrad und Fensterwerte nur durchreichen. Vorgänger: [Protokoll DA-1](2026-10-13_DA-1_gbXML_Import_Lasten_Zeitplaene.md),
[Protokoll DA-2](2026-10-13_DA-2_gbXML_Export_Rundlauf_Schema225.md). **Kein Schemaschritt**: Die Lastfelder aus DA-2 reichen.
Rechenweg und Referenzprojekte sind unberührt.

## Gemeinsame Festlegung (`IfcStandardsaetze`)

Eine Klasse unter `EPOS.Kern/Allgemein/Import/Ifc/` hält Satz- und Eigenschaftsnamen und baut die Raumsätze aus den Feldern von
EPOS — für Schreiber (`IfcSchreiber`), Anreicherung (`IfcAnreicherung`, G7d: ergänzt, eine vorhandene Eigenschaft ersetzt) und Leser
(`IfcAbbildBauer`, neuer Teil `IfcAbbildBauer.Standardsaetze.cs`) dieselben Namen.

## Schreiben (Export und Anreicherung)

| Inventar | IFC-Träger | Quelle in EPOS | Regel |
|---|---|---|---|
| B15 | `Pset_SpaceOccupancyRequirements.OccupancyNumber`, `AreaPerOccupant` | Personen, Fläche je Person | je Raum, auch unbeheizt |
| B16 | `Pset_SpaceThermalLoad.People` [W] | Personenzahl × Wärme je Person | nur sensible Wärme; ohne Personenzahl nur `EPOS_Zone.WaermeJePersonW` |
| B17 | `Pset_SpaceThermalLoad.EquipmentSensible` [W] | Geräte W/m² × Fläche | beheizte Räume |
| B18 | `Pset_SpaceThermalLoad.Lighting` [W], `Pset_SpaceLightingRequirements.ArtificialLighting` | Licht W/m² × Fläche; Licht > 0 | beheizte Räume |
| B6, B9 | `Pset_SpaceThermalDesign.HeatingDryBulb`, `CoolingDryBulb` (Kelvin) | Heiz- und Kühlsollwert | zusätzlich zu `Pset_SpaceThermalRequirements` |
| B14 | `EPOS_Zone.AussenluftJePerson` [m³/(h·Person)] | Außenluft je Person | kein Standardelement je Person (Konzept 3.3) |
| B16 | `EPOS_Zone.WaermeJePersonW` [W] | Wärme je Person | Rückfall, wenn `People` fehlt |
| B1–B4 | `IfcSpatialZone` `THERMAL` je Zone, Name und Beschreibung wie `IfcZone` | Zonen ab zwei | zusätzlich zur `IfcZone`; jeder Raum verweist mit `IfcRelReferencedInSpatialStructure` auf die Zone (Regel WR31 verbietet den Raum als Glied), Platzierung am Gebäude |
| F3 | `Pset_WindowCommon.GlazingAreaFraction` (`IfcNormalisedRatioMeasure`) | 1 − Rahmenanteil | je Fenster mit Rahmenanteil |
| F8 | `Pset_MaterialOptical.SolarReflectanceFront` | 1 − `ABSORPTIONSGRAD_OPAK` (0,4) | je Baustoff, durchgereicht |
| F9 | `Pset_MaterialOptical.ThermalIrEmissivityFront` | `EMISSIONSGRAD_AUSSEN` (0,9) | je Baustoff, durchgereicht |

Die Kalender bleiben in `EPOS_Kalender_*` (F6 a); die Ergebnisse (`EPOS_Ergebnis`) sind unverändert. Die Kennungen sind stabil
(Schlüssel je Raum bzw. Zone und Rolle): die Datei zweimal geschrieben ist byte-gleich. Die Anreicherung schreibt die Raumsätze,
den Glasanteil und die Optik; eine `IfcSpatialZone` legt sie nicht an (sie ändert keine Struktur der Originaldatei).

## Lesen (Import)

| Inventar | gelesen aus | Ziel | Regel |
|---|---|---|---|
| A1 | `IfcSite.RefLatitude`, `RefLongitude` | Breite, Länge | erstes Grundstück mit beiden; Grad, Minuten, Sekunden, Millionstel |
| A6 | `EPOS_Gebaeude.Gebaeudeart` | Gebäudeart | nur, wenn `ObjectType` keine nennt |
| B15 | `OccupancyNumber`, `AreaPerOccupant` | Personen, Fläche je Person | vor `EPOS_Zone.Personen` |
| B16 | `People` ÷ Personenzahl | Wärme je Person | sonst `EPOS_Zone.WaermeJePersonW` |
| B17, B18 | `EquipmentSensible`, `Lighting` ÷ Fläche (nach der Flächenbestimmung) | Geräte, Licht W/m² | sonst `EPOS_Zone.GeraeteWm2`/`LichtWm2`; `ArtificialLighting` falsch ohne Wert → 0 |
| B6, B9 | `SpaceTemperatureSummerMax`, sonst `CoolingDryBulb`; `HeatingDryBulb` als Rückfall | Kühl-, Heizsollwert | Sollwertsatz in IFC4X3 entfallen, dort nicht gelesen |
| B7 | `DiscontinuedHeating` | Nachtabsenkung (Kennzeichen) | der Nachtwert bleibt in `EPOS_Zone` |
| B13 | `NaturalVentilationRate` (+ `MechanicalVentilationRate`) | Nutzerluftwechsel, Luftwechsel der Zone | Summe beider Raten |
| B14 | `EPOS_Zone.AussenluftJePerson`; `Pset_SpaceThermalDesign.VentilationAirFlowrate` [m³/s] | Außenluft je Person bzw. je Fläche | je Fläche nur ohne Wert je Person |
| B12 | `EPOS_Zone.LuftwechselInfiltration` | Infiltrationsluftwechsel | — |
| B1–B4 | `IfcSpatialZone` `THERMAL` | Zonenbildung (Regel Z1) | Vorrang vor `IfcZone` und vor der Zonierung aus dem Raumtyp (Bestand); der Bezug wird jetzt in beiden Richtungen gelesen (Zone → Räume und Raum → Zone) |
| F3 | `Pset_WindowCommon.GlazingAreaFraction` | Rahmenanteil = 1 − Glasanteil, Herkunft IFC | nur ohne Rahmenanteil des CAD-Exports (Bestand) |
| D8 | `EPOS_Bauteil.WaermebrueckeUA` | Wärmebrücke W/K | — |
| E5 | `EPOS_Baustoff.Waermedurchlasswiderstand` | R der Schicht | nicht in IFC2X3 |
| F8, F9 | `Pset_MaterialOptical.SolarReflectanceFront`, `ThermalIrEmissivityFront` | — | Abweichung > 0,005 von den Konstanten → `IMP_IFC_PROT_OPTIK_ABWEICHUNG`, kein Feld |

- **Rangfolge (E138 F2 sinngemäß):** Standardsatz vor `EPOS_*` (Konzept 3.3), Datei vor Profil. Die Lasten gehen als
  `GbxmlRaumlasten` mit Kennzeichen `AusIfc` in den gemeinsamen Weg der DA-1 (`GbxmlKonditionierung.Bilden`, `Lastfelder`,
  `Profilabgleich`); die Konditionierung trägt dann die Herkunft `IfcDatei` mit der Bemerkung „aus IFC-Datei (Standardsätze)“ und
  `Zonenkonditionierung.AusIfcStandard`. Nennwerte ohne Zeitplan behalten den Zeitplan des Profils (`…NENNWERT_OHNE_PLAN`, wie
  DA-1); Abweichungen vom Profil über 5 % meldet `…PROFIL_ABWEICHUNG`. Trägt ein Raum `EPOS_Kalender_*`, gilt weiter die
  Konditionierung aus `EPOS_*` (Bestand); die Lastfelder der Zone kommen in beiden Fällen aus den Standardsätzen.
- **Gemeinsame Meldungen:** Der Weg der DA-1 meldet mit den Schlüsseln `IMP_GBXML_PROT_*`; ihre Texte nennen kein Format.
- **Herstellereigen:** `RatedMinAirExchangeRate` und `RoomVentilationType` werden nach dem Namen in jedem Satz gelesen. Der
  Mindestluftwechsel ist **nur ein genannter Vorschlag** (`IMP_IFC_PROT_HERSTELLER_VORSCHLAG`, eine Meldung je Datei mit Zahl,
  Eigenschaft, Spanne und Räumen) und wird nicht übernommen — es gilt das Nutzungsprofil. So bleiben die Importproben der
  CAD-Dateien unter `Quellen/` unverändert (sie tragen die Eigenschaft in jedem Raum). `RoomVentilationType` wird durchgereicht.
- **Durchgereicht ohne Zielfeld** (gezählt in `IMP_IFC_PROT_STANDARDSAETZE`, eine Meldung je Datei): `OccupancyType`,
  `OccupancyNumberPeak`, `OccupancyTimePerDay`, `EquipmentLatent`, `AirExchangeRate` (beide Sätze), `HeatingDesignAirflow`,
  `CoolingDesignAirflow`, `Illuminance`, `SpaceTemperatureMax`, `SpaceHumidity`, `NaturalVentilation`, `MechanicalVentilation`,
  `AirConditioning`, `RoomVentilationType`, an der Verglasung `ThermalTransmittance`, `ThermalTransmittanceWinter`,
  `VisibleLightTransmittance`.

## Annahmen zu den IFC4-Namen

Die IFC4-Schemakopie (Pset-Tafel) liegt nicht im Repositorium. Angenommen nach der veröffentlichten Tafel IFC4 ADD2 TC1 und
gelesen ohne Groß-/Kleinschreibung:

- `Pset_SpaceOccupancyRequirements`: `OccupancyType`, `OccupancyNumber`, `OccupancyNumberPeak`, `OccupancyTimePerDay`, `AreaPerOccupant`.
- `Pset_SpaceThermalLoad`: `People`, `Lighting`, `EquipmentSensible`, `AirExchangeRate` als Leistung des Raums in W bzw.
  Volumenstrom; `EquipmentLatent` ist vermutlich kein IFC4-Name (nur gezählt).
- `Pset_SpaceThermalDesign`: `HeatingDryBulb`, `CoolingDryBulb`, `VentilationAirFlowrate` (m³/s, SI angenommen),
  `HeatingDesignAirflow`, `CoolingDesignAirflow`, `AirExchangeRate`.
- `Pset_SpaceLightingRequirements`: `ArtificialLighting`, `Illuminance`; `ArtificialLightingPower` u. ä. sind keine IFC4-Namen —
  die Lichtleistung geht über `Pset_SpaceThermalLoad.Lighting`.
- `Pset_SpaceThermalRequirements`: `SpaceTemperatureSummerMax`, `SpaceTemperatureMax`, `SpaceHumidity`, `DiscontinuedHeating`,
  `NaturalVentilation`, `NaturalVentilationRate`, `MechanicalVentilation`, `MechanicalVentilationRate` (1/h), `AirConditioning`.
- `Pset_WindowCommon.GlazingAreaFraction` = Glasanteil (1 − Rahmenanteil); `Pset_DoorWindowGlazingType` führt in IFC4
  `ThermalTransmittanceSummer`/`ThermalTransmittanceWinter` (U der Verglasung, nicht U_w) — daher nur durchgereicht.
- `Pset_MaterialOptical`: `SolarReflectanceFront` (α = 1 − ρ für opake Stoffe), `ThermalIrEmissivityFront`. Damit ist für beide
  Konstanten ein Standardname belegt; ein `EPOS_*`-Satz für sie ist nicht nötig.
- Die Werte werden als Einzelwerte geschrieben (IFC4 sieht teils `IfcPropertyBoundedValue` vor, wie im Bestand bei
  `SpaceTemperature`); der Leser nimmt beide Arten.

## Rundlauf (E138 F5)

`EPOS.Kern.Tests/IfcExportRundlaufTests` (4 Tests): Das Probenabbild des gbXML-Rundlaufs (Zone 1: 100 m², fünf Kalender, Licht
3 W/m², Geräte 2,5 W/m², drei Personen à 80 W, Außenluft 30 m³/(h·Person)) mit Nutzung und Nachtsollwert, dazu Zone 2 (50 m²,
10 m² je Person, Geräte 4 W/m², Licht 0, 100 W je Person, Außenluft 40 m³/(h·Person), Infiltration 0,5 1/h), Lage, Baujahr,
Rahmenanteil 0,3 am Fenster und Wärmebrücke 1,5 W/K an der Südwand. Export über `IfcSchreiber`, Rücklesen mit `IfcLeser`:

- **verglichen:** 5 Kalender × (8760 Stundenwerte + Nennwert) = 43 805, dazu 186 Einzelfelder (Breite, Länge, Gebäudeart, Baujahr;
  je Zone Fläche, Volumen, Heiz-, Kühl- und Nachtsollwert, Infiltration, Nutzerluftwechsel, Personen, Fläche je Person, Geräte,
  Licht, Wärme und Außenluft je Person, Luftwechsel der Zone, Zone, Nutzung und die drei Lastfelder der Zone; je Bauteil U, g,
  Rahmenanteil, Wärmebrücke; je Schicht Dicke, λ, ρ, c, R) — zusammen **43 991 Felder, 0 Abweichungen** (Toleranz 1e‑6).
- Beide Zonen kommen als getrennte Zonen der `IfcSpatialZone` zurück. Die Datei ist zweimal geschrieben byte-gleich.
- Gegenproben: ohne `EPOS_Kalender_*` bildet der Zonenplan die Konditionierung aus den Standardsätzen (Herkunft IFC, Personen
  240 W, Lüftung 2 1/h, Nennwert ohne Plan gemeldet); `RatedMinAirExchangeRate` an Stelle von `NaturalVentilationRate` ergibt
  nur den Vorschlag; eine solare Reflexion 0,2 statt 0,4 ergibt `OPTIK_ABWEICHUNG`, die Konstanten schweigen.

**Grenzen des Rundlaufs** (benannt): Die Schichtfolge kommt außen → innen zurück (Bestand); `IfcZone` und `IfcSpatialZone`
tragen dieselbe Zone, der Leser nimmt die `IfcSpatialZone`, die Zonenkennung ist deren GUID; die Kalender laufen über `EPOS_*`
(für Fremdwerkzeuge unsichtbar, F6 a); B10, B11, B19–B22, A2, A7–A9, C8–C10, E6, E7, F4, F6, F7, F10, G1, G3, G4 haben kein Element
(DA-4).

## Inventar-Einstufung IFC (`DatenaustauschInventar.Ifc`, Wache `DatenaustauschInventarWacheTests`, 3 Tests)

Neue Einstufung `NurEpos` (beide Richtungen nur über einen eigenen Satz). Die Wache prüft je Datum die Deckung mit dem Export der
Rundlaufprobe (Eigenschaftsname bzw. Klasse in der STEP-Datei) und mit den Quellen des IFC-Lesers (Eigenschaftsname, Schnittstelle
oder Attribut), dazu die Zählung und dieselben Kennungen wie die Tafel.

| Einstufung IFC | Zahl | Daten |
|---|---|---|
| beide Richtungen (Standardelement) | 31 | A1, A4, A5, B1–B4, B6, B9, B13, B15–B18, D1–D7, E1–E4, F1–F3, F8, F9, G2 |
| nur `EPOS_*` | 16 | A6, A10, B5, B7, B8, B12, B14, C1–C7, D8, E5 |
| nur gelesen | 1 | F5 (`VisibleLightTransmittance`, durchgereicht) |
| nur geschrieben | 0 | — |
| ohne Element | 23 | A2, A3, A7, A8, A9, B10, B11, B19–B22, C8, C9, C10, E6, E7, F4, F6, F7, F10, G1, G3, G4 |

Je Gruppe (beide / `EPOS_*` / gelesen / ohne): A 3/2/0/5, B 11/5/0/6, C 0/7/0/3, D 7/1/0/0, E 4/1/0/2, F 5/0/1/4, G 1/0/0/3.
**Vergleich mit gbXML (DA-2):** gbXML 42 beide Richtungen, 3 nur gelesen, 26 ohne Element. IFC trägt 31 über Standardelemente
und weitere 16 über `EPOS_*` — zusammen 47 im eigenen Rundlauf, 5 mehr als gbXML (IFC zusätzlich in beiden Richtungen: A1, A5,
B5, D8, F3; gbXML liest A1 und B5 nur); jedes Datum, das gbXML in beiden Richtungen trägt, trägt auch IFC. Ohne Element in IFC,
aber in gbXML gelesen: F6 (der winkelabhängige g-Wert). Für ein Fremdwerkzeug sichtbar
sind in IFC 32 Daten (31 + F5), in gbXML 45 — die Kalender C1–C7 liegen in IFC nach F6 (a) bewusst in `EPOS_*`.

## Dateien

- neu: `EPOS.Kern/Allgemein/Import/Ifc/IfcStandardsaetze.cs`, `EPOS.Kern/Allgemein/Import/Ifc/IfcAbbildBauer.Standardsaetze.cs`,
  `EPOS.Kern.Tests/IfcExportRundlaufTests.cs`
- geändert: `IfcAbbildBauer.cs` (sechs Aufrufe, `IfcSpatialZone` in beiden Richtungen), `IfcSchreiber.cs`, `IfcAnreicherung.cs`,
  `GbxmlKonditionierung.cs`, `GbxmlRaumlasten.cs`, `SqprojKonditionierung.cs`, `GebaeudeBauteilvorschlag.cs`,
  `DatenaustauschInventar.cs`, `DatenaustauschInventarWacheTests.cs`, beide `Resource*.resx`, `Resource.Designer.cs`
- Ressourcen: 4 neue Schlüssel je Sprache (`IMP_IFC_STANDARD_HERKUNFT`, `IMP_IFC_PROT_STANDARDSAETZE`,
  `IMP_IFC_PROT_HERSTELLER_VORSCHLAG`, `IMP_IFC_PROT_OPTIK_ABWEICHUNG`); Designer wiederholbar (zweiter Lauf +0).
- Kein SQL, kein Schemaschritt, keine Hülle der Windows-Schale.

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- `dotnet test EPOS.Kern.Tests --filter "Ifc|Datenaustausch|GebaeudeImport|GebaeudeExport|Zonenplan|Gbxml|Resource"`: 1059 grün,
  0 rot, 5 übersprungen (Bestand). Neu sind 5 Tests (`IfcExportRundlaufTests` 4, `DatenaustauschInventarWacheTests` 1). Die
  Durchgangsproben der CAD-Dateien unter `Quellen/` und die Quelltextwache des Imports (keine schemagebundene xBIM-Klasse) sind grün.

## Offen

- **Schemakopie IFC4 (Pset-Tafel):** die Annahmen oben prüfen, besonders `Pset_MaterialOptical` und die Werttypen.
- **Vorschlag übernehmen:** Der herstellereigene Mindestluftwechsel ist nur gemeldet; eine Übernahme durch den Anwender gehört zur
  Profilprüfung (DA-5) bzw. in den Importdialog.
- **B10, B11** (`SpaceTemperatureMax`, Heiz-/Kühlleistung höchstens) haben kein Feld im Abbild: DA-4 (Höchstleistungen) setzt sie an.
- **Rückfall `EPOS_Gebaeude`** für A2, A7–A9, G3, G4 und **`EPOS_Zone`** für B19–B22, G1: DA-4.
- **Anreicherung und `IfcSpatialZone`:** G7d legt keine Zone an; eine Originaldatei ohne thermische Zonen bleibt so.
- **Anwenderprobe unter Windows:** Export mit zwei Zonen, Öffnen in einem Fremdwerkzeug (Sätze sichtbar), Wiedereinlesen.

## Logbuch-Vorschlag

Der IFC-Export schreibt Personen, Geräte, Licht, Auslegungstemperaturen, Glasanteil der Fenster und thermische Zonen in den
IFC-Standardsätzen, der IFC-Import liest sie wieder ein (Datei vor Nutzungsprofil).
