# BK — Feldinventar der Bauphysik-Tabellen des Hottgenroth-Katalogs

Erhoben von der lokalen Sitzung „iOS migration merge conflicts“ am 12.10.2026 an einer angehängten Kopie der
`HsetuAppTimData.mdf` (danach wieder abgehängt), angefordert für den Schemaschritt der Welle BK-2 (E132 F2 „alle Felder
aufnehmen“). **Nur Struktur: keine Werte, keine Katalog-, Hersteller- oder Produktnamen.** Grundlage des Entwurfs:
[Entwurf Bauteilkatalog](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md), dazu der
[Strukturbericht](2026-10-12_BK_Strukturbericht_Hottgenroth.md).

- **Belegung %** = Anteil der Zeilen ohne NULL, ohne Platzhalter −987654321,99/−987654,32, ohne leeren Text, ohne
  Leer-GUID (`AAAAAAAA-AAAA-…`).
- **Codes** = Zahl verschiedener Werte ohne NULL/Platzhalter bei Codespalten.
- Bedeutungen sind aus Spaltennamen und Strukturbericht abgeleitete Vermutungen. Gleichartige Spalten stehen in einer Zeile.
  Die Einzelzeilen-Fassung liegt beim Anwender unter `C:\Temp\Hsetu\Feldinventar.md`.

## TcMaterial (10 719 Zeilen, 101 Spalten)

| Spalte | SQL-Typ | Belegung % | Bedeutung und Einheit (Vermutung) | Codes |
|---|---|---|---|---|
| UId | uniqueidentifier | 100 | interne Kennung (GUID) |  |
| GId | uniqueidentifier | 100 | interne Kennung (GUID) |  |
| OId | int | 100 | Ordnung (intern) | 630 |
| SeqNum | int | 100 | Ordnung (intern) | 2769 |
| SortNum | int | 100 | Ordnung (intern) | 2769 |
| ShortDesc | nvarchar | 80 | Kurztext |  |
| LongDesc | nvarchar | 97 | Langtext |  |
| InfoDesc | nvarchar | 43 | Infotext |  |
| BodyText | nvarchar | 4 | Fließtext |  |
| BodyTextType | int | 2 | Art des Fließtexts (Code) | 1 |
| MatchCode | nvarchar | 2 | Abgleichscode (Text, intern) |  |
| Version | nvarchar | 0 | Versionstext [immer NULL] |  |
| TemperatureMax | float | 8 | Höchsttemperatur (°C, Vermutung) |  |
| Roughness | float | 8 | Rauheit (Einheit offen) |  |
| Class | nvarchar | 0 | Klassenbezeichnung (Text) [NULL oder Platzhalter/leer gemischt] |  |
| ElasticConstant | float | 0 | Elastizitätskonstante (Einheit offen) |  |
| YieldStrength | float | 0 | Streckgrenze (Einheit offen) [NULL oder Platzhalter/leer gemischt] |  |
| PartialSafetyFactor | float | 0 | Teilsicherheitsbeiwert (-) [NULL oder Platzhalter/leer gemischt] |  |
| ProofStrength2Percent | float | 0 | Dehngrenze bei Prozent-Dehnung (Einheit offen) [NULL oder Platzhalter/leer gemischt] |  |
| LinearExtensionCoefficient | float | 8 | Längenausdehnungskoeffizient (Einheit offen) |  |
| HeatConductivity | float | 84 | Wärmeleitfähigkeit λ in W/(m·K) |  |
| Density | float | 77 | Rohdichte ρ in kg/m³ |  |
| HeatCapacity | float | 76 | spezifische Wärmekapazität c; gemischt kJ/(kg·K) und J/(kg·K) |  |
| MaterialType | int | 100 | Baustoffart (Code) | 14 |
| MaterialGroupType | int | 37 | Stoffgruppe (Code, Vermutung DIN 4108-4) | 9 |
| MaterialSurfaceType | int | 100 | Oberflächenart (Code) | 7 |
| MaterialLCAType | int | 0 | Ökobilanzart (Code) | 1 |
| DiffusionResistanceCoefficient | float | 0 | Wasserdampf-Diffusionswiderstandszahl µ (-) |  |
| LowerDiffusionResistanceCoefficient | float | 60 | µ untere Grenze (-) |  |
| UpperDiffusionResistanceCoefficient | float | 59 | µ obere Grenze (-) |  |
| DesignValueThermalConductivity | float | 0 | Bemessungswert λ in W/(m·K) |  |
| LowerDesignValueThermalConductivity | float | 0 | Bemessungswert λ untere Grenze, W/(m·K) [NULL oder Platzhalter/leer gemischt] |  |
| UpperDesignValueThermalConductivity | float | 0 | Bemessungswert λ obere Grenze, W/(m·K) [NULL oder Platzhalter/leer gemischt] |  |
| IsVapourDiffusionTight | bit | 100 | Schalter dampfdicht | 2 |
| IsHeatReflective | bit | 100 | Schalter wärmereflektierend | 2 |
| IsCapillary | bit | 100 | Schalter kapillaraktiv | 2 |
| MoistureContent | float | 3 | Feuchtegehalt (Einheit offen) |  |
| HumiditySupplementConductivity | float | 0 | Feuchtezuschlag zu λ (Einheit offen) [nur Platzhalter] |  |
| WaterSaturation80Percent | float | 0 | Wassergehalt bei hoher Luftfeuchte (Einheit offen) [nur Platzhalter] |  |
| WaterSaturationFree | float | 0 | Wassergehalt freie Sättigung (Einheit offen) [nur Platzhalter] |  |
| MaterialPCMIsDefined | bit | 100 | Schalter Phasenwechselstoff definiert | 2 |
| MaterialPCMT1, MaterialPCMT2 | float | 0 | Phasenwechselstoff Temperatur 1/2 (°C, Vermutung) [nur Platzhalter] |  |
| MaterialPCMHeatCapacity, MaterialPCMThermalConductivity | float | 0 | Phasenwechselstoff, Kennwerte fest (Einheit offen) [nur Platzhalter] |  |
| MaterialPCMHeatCapacityLiquid, MaterialPCMThermalConductivityLiquid | float | 0 | Phasenwechselstoff, Kennwerte flüssig (Einheit offen) [nur Platzhalter] |  |
| MaterialPCMAdmixture | float | 0 | Phasenwechselstoff Beimischung (Einheit offen) [nur Platzhalter] |  |
| MaterialPCMEnthalpyOfFusion | float | 0 | Phasenwechselstoff Schmelzenthalpie (Einheit offen) [nur Platzhalter] |  |
| OI3AP, OI3PEI, OI3GWP | float | 70 | Ökoindex OI3 Versauerung, Primärenergie, Treibhauspotenzial (Einheit offen) |  |
| EPDVersion | nvarchar | 0 | EPD-Version (Text) [NULL oder Platzhalter/leer gemischt] |  |
| EPDPERE … EPDADPF (25 Spalten) | float | 0 | EPD-Umweltindikatoren (Kürzel nach EN 15804, Einheit offen) [NULL oder Platzhalter/leer gemischt] |  |
| CatalogGroupUId | uniqueidentifier | 0 | interne Kennung (GUID) [immer NULL] |  |
| DataSourceUId | uniqueidentifier | 0 | interne Kennung (GUID) [nur Leer-GUID] |  |
| EPDUId | uniqueidentifier | 0 | interne Kennung (GUID) [NULL oder Platzhalter/leer gemischt] |  |
| CatalogStateType | int | 100 | Verwaltungscode (intern) | 4 |
| EditModeType, StateType, VariantType | int | 100 | Verwaltungscodes (intern) | je 1 |
| VariantRoot | nvarchar | 0 | Variantenwurzel (Text, intern) |  |
| GeneratorType | int | 100 | Verwaltungscode (intern) | 3 |
| StampEdit, StampCreate, StampSync | datetime | 100 | Zeitstempel (intern) |  |
| MId | uniqueidentifier | 0 | interne Kennung (GUID) [NULL oder Platzhalter/leer gemischt] |  |
| ProjectUId | uniqueidentifier | 0 | interne Kennung (GUID) [nur Leer-GUID] |  |
| TIMUId | uniqueidentifier | 0 | interne Kennung (GUID) |  |
| SyncType | int | 100 | Synchronisation (intern) | 3 |
| SyncCreate, SyncEdit | uniqueidentifier | 0 | Synchronisation (intern) [NULL oder Platzhalter/leer gemischt] |  |
| SyncVersion, SyncMeta | nvarchar | 100 | Synchronisation (intern) |  |
| SyncStamp | datetime | 100 | Zeitstempel (intern) |  |
| SyncRowVersion | uniqueidentifier | 34 | Synchronisation (intern) |  |
| VAPOURDIFFUSIONCOEFFICIENT | float | 0 | Dampfdiffusionskoeffizient (Einheit offen) [NULL oder Platzhalter/leer gemischt] |  |

## TcBuildingElementDimension (8 427 Zeilen, 157 Spalten)

| Spalte | SQL-Typ | Belegung % | Bedeutung und Einheit (Vermutung) | Codes |
|---|---|---|---|---|
| UId | uniqueidentifier | 100 | interne Kennung (GUID) |  |
| BasisUId | uniqueidentifier | 100 | interne Kennung (GUID), Verweis auf TcBasis (Artikelstamm; dort GroupType = Bauteilart, ManufacturerUId) |  |
| GId | uniqueidentifier | 100 | interne Kennung (GUID) |  |
| OId | int | 100 | Ordnung (intern) | 67 |
| SeqNum | int | 100 | Ordnung (intern) | 1091 |
| SortNum | int | 100 | Ordnung (intern) | 1092 |
| ShortDesc | nvarchar | 90 | Kurztext |  |
| LongDesc | nvarchar | 100 | Langtext |  |
| InfoDesc | nvarchar | 83 | Infotext |  |
| BodyText | nvarchar | 0 | Fließtext [NULL oder Platzhalter/leer gemischt] |  |
| BodyTextType | int | 26 | Art des Fließtexts (Code) | 1 |
| Thickness | float | 83 | Dicke in m |  |
| RawThickness | float | 0 | Dicke in m [nur Platzhalter] |  |
| FinalThickness | float | 0 | Dicke in m |  |
| Width, Length, Height | float | 0 | Breite/Länge/Höhe in m (Vermutung) [nur Platzhalter] |  |
| Area | float | 0 | Fläche in m² (Vermutung) [nur Platzhalter] |  |
| Volume | float | 0 | Volumen in m³ (Vermutung) [nur Platzhalter] |  |
| WValue | float | 0 | W-Wert (Bedeutung offen) [nur Platzhalter] |  |
| UValue | float | 86 | Wärmedurchgangskoeffizient U in W/(m²K) |  |
| UnValue | float | 0 | U-Wert, Nebenvariante (Bedeutung offen) [NULL oder Platzhalter/leer gemischt] |  |
| RValue | float | 1 | Wärmedurchlasswiderstand R in m²K/W |  |
| ElementMeta | nvarchar | 14 | Schlüssel-Wert-Text zum Bauteil (u. a. Rsi/Rse) |  |
| InternalCoefficientOfHeatTransfer | float | 14 | Rsi, innerer Übergangswiderstand in m²K/W (trotz Name) |  |
| ExternalCoefficientOfHeatTransfer | float | 14 | Rse, äußerer Übergangswiderstand in m²K/W (trotz Name) |  |
| IsVentilated | bit | 100 | Schalter hinterlüftet | 2 |
| FractionOfFrame | float | 15 | Rahmenanteil in % |  |
| GValue | float | 26 | Gesamtenergiedurchlassgrad g (0 bis 1) |  |
| ShadingCoefficient | float | 26 | Verschattungsfaktor (-, Vermutung) |  |
| MullionFirst/SecondThickness/Offset, TransomFirst/SecondThickness/Offset (8 Spalten) | float | 0 | Pfosten/Riegel, Dicke bzw. Versatz in m (Vermutung) [nur Platzhalter] |  |
| HorizontalBarNumberOfBars, VerticalBarNumberOfBars | int | 26 | Anzahl Sprossen waagerecht/senkrecht | je 2 |
| Horizontal-/VerticalBarWidth, …Area, …LengthWeight (6 Spalten) | float | 0 | Breite, Fläche, Längengewichtung der Sprossen [NULL oder Platzhalter/leer gemischt] |  |
| GlazingIsoType | int | 26 | Verglasungsart nach ISO (Code) | 1 |
| GlazingUValue | float | 0 | Ug, Verglasung, W/(m²K) [NULL oder Platzhalter/leer gemischt] |  |
| GlazingGValue | float | 0 | g-Wert Verglasung (0 bis 1) [NULL oder Platzhalter/leer gemischt] |  |
| GlazingLength, GlazingWidth, GlazingPerimeter, GlazingArea | float | 0 | Maße der Verglasung (m bzw. m², Vermutung) [nur Platzhalter] |  |
| GlazingOI3IndexActive | bit | 26 | Schalter OI3 aktiv | 1 |
| GlazingOI3AP, GlazingOI3PEI, GlazingOI3GWP, GlazingBasisWeight | float | 0 | Ökoindex OI3 und Gewichtung der Verglasung [NULL oder Platzhalter/leer gemischt] |  |
| EdgeLength, EdgeLengthWeight, EdgePsiValue | float | 0 | Randverbund: Länge, Gewichtung, ψ in W/(m·K) [nur Platzhalter bzw. gemischt] |  |
| FrameWidth | float | 5 | Rahmenbreite in m |  |
| FrameArea, FrameFractionOfFrame | float | 0 | Rahmenfläche in m², Rahmenanteil am Rahmen [nur Platzhalter] |  |
| FrameUValue | float | 5 | Uf, Rahmen, W/(m²K) |  |
| FrameLengthWeight | float | 0 | Gewichtung Rahmenlänge (Vermutung) [NULL oder Platzhalter/leer gemischt] |  |
| FrameOI3IndexActive | bit | 26 | Schalter OI3 aktiv | 1 |
| FrameOI3AP, FrameOI3PEI, FrameOI3GWP | float | 5 | Ökoindex OI3 Rahmen (Einheit offen) |  |
| PsiValue | float | 4 | ψ Wärmebrücke in W/(m·K) |  |
| PsiSystemValue, PsiGeometryValue | float | 1 | ψ-Anteile System und Geometrie, W/(m·K) |  |
| ValidStartDate, ValidEndDate | datetime | 100 | Gültigkeit Beginn/Ende (Datum) |  |
| VDI3805DatabaseName, VDI3805StandardNumber, VDI3805DataSet… (53 Spalten), VDI3805TGANumber | int / nvarchar | 0 | VDI-3805-Satzkennungen und -beschreibungen (Haustechnik) [immer NULL] | 0 |
| DataContentTypeSet | int | 0 | Inhaltsarten (Bitmenge, Code) [immer NULL] | 0 |
| MatchCode | nvarchar | 5 | Abgleichscode (Text, intern) |  |
| CatalogGroupUId, EPDUId | uniqueidentifier | 0 | interne Kennungen (GUID) [immer NULL] |  |
| FrameOI3UId, GlazingOI3UId | uniqueidentifier | 26 | interne Kennungen (GUID), Verweis auf OI3-Zeile |  |
| CatalogStateType | int | 100 | Verwaltungscode (intern) | 2 |
| GeoDesc, GeoType | nvarchar / int | 0 | Geometriebeschreibung und -art [immer NULL] | 0 |
| EditModeType, StateType, VariantType | int | 100 | Verwaltungscodes (intern) | je 1 |
| VariantRoot | nvarchar | 0 | Variantenwurzel (Text, intern) |  |
| GeneratorType | int | 100 | Verwaltungscode (intern) | 2 |
| StampEdit, StampCreate, StampSync | datetime | 100 | Zeitstempel (intern) |  |
| MId, ProjectUId | uniqueidentifier | 0 | interne Kennungen (GUID) [nur Leer-GUID] |  |
| SyncType | int | 100 | Synchronisation (intern) | 2 |
| SyncCreate, SyncEdit | uniqueidentifier | 0 | Synchronisation (intern) [nur Leer-GUID] |  |
| SyncVersion, SyncMeta | nvarchar | 100 | Synchronisation (intern) |  |
| SyncStamp | datetime | 100 | Zeitstempel (intern) |  |
| SyncRowVersion | uniqueidentifier | 27 | Synchronisation (intern) |  |

## Teil 2

Schichten (`TcBuildingElementLayer`), Abschnitte (`TcBuildingElementLayerSection`), Fenster (`TcWindowPanel`) und
`TcMaterialTechData` folgen.
