# Befund S — IFC-Export aus EPOS-Plan ohne Geometriekernel (15.09.2026)

**Protokoll.** Befund S eines Recherche-Agenten (Modell Opus) im Auftrag des Konzepts [`../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), Sitzung vom 15.09.2026.

Aufbauend auf [`2026-09-15_Befund_C_IFC-Recherche.md`](2026-09-15_Befund_C_IFC-Recherche.md) (Schemainhalte, Bibliothekenvergleich, Lizenzen) und [`2026-09-15_Befund_N_IFC-Import_Entwurf.md`](2026-09-15_Befund_N_IFC-Import_Entwurf.md) (Paketwahl, Trimming, Ablaufmuster). Was dort steht, wird hier nicht wiederholt. Alle Spezifikationsstellen sind gegen **IFC4 ADD2 TC1** geprüft, alle Quelltextstellen gegen den `master`-Zweig von `xBimTeam/XbimEssentials` (Abruf 15.09.2026).

---

**Kurzantwort.** Ein IFC ohne jede Geometrie ist schemakonform, validierungsfähig und für ein semantisches Gegenüber vollständig auswertbar — `IfcProduct.Representation` ist OPTIONAL, `IfcRelSpaceBoundary.ConnectionGeometry` ebenfalls. Es passt aber in **keine** offizielle MVD, und in einem Betrachter bleibt der Bildschirm leer. Die Entscheidung ist daher keine technische, sondern eine über das Gegenüber: für ein Rechenwerkzeug reicht Stufe S1, für den Menschen am Bildschirm braucht es S3. Der größte Einzelnutzen liegt dazwischen, in S2 — die importierte Fremddatei um EPOS-Wissen anreichern und zurückgeben, weil dort die Geometrie schon da ist und nur die Zahlen fehlen. Beim Schreiben mit xBIM gibt es eine Falle, die im Import nicht auftritt: **`MemoryModel` vergibt weder `GlobalId` noch `OwnerHistory`** — das tut nur `IfcStore`, und `IfcStore` ist der Esent-Weg, den Befund N für iOS ausgeschlossen hat.

---

## 1. Was ein IFC ohne Geometrie darf

### 1.1 Die Spezifikation ist eindeutig: Repräsentation ist optional

`IfcProduct` führt sieben direkte Attribute; die beiden geometrischen sind mit `?` (OPTIONAL) ausgewiesen ([IfcProduct, IFC4 ADD2 TC1](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifckernel/lexical/ifcproduct.htm)):

| # | Attribut | Typ | Kardinalität |
|---|---|---|---|
| 6 | `ObjectPlacement` | `IfcObjectPlacement` | `?` |
| 7 | `Representation` | `IfcProductRepresentation` | `?` |

Die einzige Regel dazu läuft nur in eine Richtung — `PlacementForShapeRepresentation`: *„If a Representation is given being an IfcProductDefinitionShape, then also an ObjectPlacement has to be given."* Und im Abschnitt „Concept usage": *„The Product Placement establishes the object coordinate system and is required, if a geometric shape representation is provided for this product."* Umgekehrt gilt nichts: **kein Körper — kein Placement nötig** ([ebd.](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifckernel/lexical/ifcproduct.htm)).

Auch die Raumstruktur fordert keine Lage. `IfcSpatialStructureElement` hat genau eine Regel, WR41: *„All spatial structure elements shall be associated (using the IfcRelAggregates relationship) with another spatial structure element, or with IfcProject."* — eine Aggregationspflicht, keine Geometriepflicht ([IfcSpatialStructureElement](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcspatialstructureelement.htm)). `IfcSpace` trägt nur die beiden Regeln `CorrectPredefinedType` und `CorrectTypeAssigned` ([IfcSpace](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcspace.htm)), `IfcZone` nur WR1: nur `IfcSpace`, `IfcZone`, `IfcSpatialZone` dürfen gruppiert werden ([IfcZone](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifczone.htm)).

Den praktischen Gegenbeweis liefert die xBIM-Dokumentation selbst: ihr Einstiegsbeispiel erzeugt ausdrücklich *„simple IFC model without any geometry"* und druckt die vollständige, gültige Ausgabedatei mit `FILE_SCHEMA (('IFC4'));` ab ([docs.xbim.net, Basic model operations](https://docs.xbim.net/examples/basic-model-operations.html)).

### 1.2 Was EPOS semantisch schreiben kann

Alles Folgende ist ohne einen einzigen Geometrieknoten schreibbar.

| Gegenstand | Entität / Satz | Anmerkung |
|---|---|---|
| Projektrahmen | `IfcProject` mit `UnitsInContext` (`IfcUnitAssignment`) | Ein `IfcGeometricRepresentationContext` ist ohne Repräsentationen nicht nötig; xBIM legt ihn beim Helfer trotzdem an (Abschnitt 4.4). |
| Grundstück | `IfcSite` mit `RefLatitude`, `RefLongitude`, `RefElevation` | Alle drei OPTIONAL ([IfcSite](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcsite.htm)). |
| Gebäude | `IfcBuilding` + `Pset_BuildingCommon` | `YearOfConstruction` ist `IfcLabel`, also Text (Befund C, Abschnitt 1). |
| Geschoss | `IfcBuildingStorey` | Nur wenn EPOS Geschosse führt; sonst weglassen, Räume hängen dann am Gebäude. |
| Zone/Raum | `IfcSpace` + `Qto_SpaceBaseQuantities` + `Pset_SpaceCommon` + `Pset_SpaceThermalRequirements` | Siehe 1.3. |
| Zonengruppe | `IfcZone` + `IfcRelAssignsToGroup` | ([IfcRelAssignsToGroup](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifckernel/lexical/ifcrelassignstogroup.htm)) |
| Bauteil | `IfcWall`/`IfcSlab`/`IfcRoof`/`IfcWindow` + `Pset_*Common` (`ThermalTransmittance`, `IsExternal`) + `Qto_*BaseQuantities` | Befund C, Abschnitt 1. |
| Raumgrenze | `IfcRelSpaceBoundary2ndLevel` **ohne** `ConnectionGeometry` | Siehe 1.4. |
| Aufbau | `IfcMaterialLayerSet` → `IfcMaterialLayer` → `IfcMaterial` | Siehe 1.5. |
| Stoffwerte | `Pset_MaterialThermal`, `Pset_MaterialCommon` | Befund C, Abschnitt 1. |
| Zuordnung Material | `IfcRelAssociatesMaterial` (`RelatingMaterial: IfcMaterialSelect`, Pflicht) | ([IfcRelAssociatesMaterial](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcrelassociatesmaterial.htm)) |
| Struktur | `IfcRelAggregates`, `IfcRelContainedInSpatialStructure` | `RelatingStructure` ist `IfcSpatialElement`: ein Bauteil darf unmittelbar am `IfcBuilding` hängen, ein Geschoss ist nicht erzwungen ([IfcRelContainedInSpatialStructure](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcrelcontainedinspatialstructure.htm)). |
| Ergebnisse | eigener Satz `EPOS_Ergebnis` | Siehe 1.6. |

**Die Ortsangabe ist eine Falle.** `IfcSite.RefLatitude`/`RefLongitude` sind vom Typ `IfcCompoundPlaneAngleMeasure`, und der ist definiert als `LIST [3:4] OF INTEGER` — Grad, Minuten, Sekunden und optional Millionstelsekunden, *„irrespective of unit assignments"*. Entscheidend für den Schreiber: *„All measure components have the same sign (positive or negative)"*, und die Spezifikation liefert den Umrechnungsschnipsel gleich mit (`c[2] := (a - c[1]) * 60` usw.) ([IfcCompoundPlaneAngleMeasure](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcmeasureresource/lexical/ifccompoundplaneanglemeasure.htm)). Wer aus 48,137 Grad `(48, 8, 13)` macht und bei negativer Länge das Vorzeichen nur vorne setzt, schreibt stillschweigend falsch — die Prüfsumme dagegen ist trivial: alle drei bzw. vier Zahlen dasselbe Vorzeichen.

### 1.3 `Pset_SpaceThermalRequirements` — nur bis IFC4, dafür auch an der Zone

Der Satz ist in IFC4 ADD2 TC1 vorhanden, gilt für `IfcSpace`, `IfcSpatialZone` **und `IfcZone`** und ist `PSET_TYPEDRIVENOVERRIDE`. Vollständige Eigenschaftenliste: `SpaceTemperature`, `SpaceTemperatureMax`, `SpaceTemperatureMin`, `SpaceTemperatureSummerMax`, `SpaceTemperatureSummerMin`, `SpaceTemperatureWinterMax`, `SpaceTemperatureWinterMin`, `SpaceHumidity`, `SpaceHumidityMax`, `SpaceHumidityMin`, `SpaceHumiditySummer`, `SpaceHumidityWinter`, `DiscontinuedHeating`, `NaturalVentilation`, `NaturalVentilationRate`, `MechanicalVentilationRate`, `AirConditioning`, `AirConditioningCentral` ([Pset_SpaceThermalRequirements](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/pset/pset_spacethermalrequirements.htm)). Dass er in IFC 4.3 fehlt, hat Befund C festgestellt — für den Export ist das ein Argument, **IFC4 (ADD2 TC1) als Schreibschema zu wählen und nicht 4.3**: die Sollwerte gehen dort sonst nur noch in einem eigenen Satz unter.

Dass `DiscontinuedHeating` (Nachtabsenkung) und `NaturalVentilationRate` mit dabei sind, ist für EPOS ein Glücksfall — das sind genau die Nutzungsgrößen des VDI-6007-Modells.

### 1.4 Raumgrenzen ohne Geometrie: erlaubt, benannt, aber schwächer

`IfcRelSpaceBoundary.ConnectionGeometry` ist `?`. Der erläuternde Text sagt es ausdrücklich zweimal ([IfcRelSpaceBoundary](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcrelspaceboundary.htm)):

> „The attribute ConnectionGeometry may be inserted, in this case it describes the physical space boundary geometically, or it may be omited, in that case it describes a physical space boundary logically."

> „The IfcRelSpaceBoundary may have geometry attached. If geometry is not attached, the relationship between space and building element is handled only on a logical level."

Pflicht sind dagegen `RelatingSpace` (`IfcSpaceBoundarySelect`), `RelatedBuildingElement` (`IfcElement`, **seit IFC4 Pflicht**), `PhysicalOrVirtualBoundary` und `InternalOrExternalBoundary` ([ebd.](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcrelspaceboundary.htm)). `CorrespondingBoundary` am `IfcRelSpaceBoundary2ndLevel` ist `?` und wird über die inverse Beziehung `Corresponds` (`S[0:1]`) gegengelesen ([IfcRelSpaceBoundary2ndLevel](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcrelspaceboundary2ndlevel.htm)).

Die Randbedingung „Erdreich" hat einen eigenen Aufzählungswert: `EXTERNAL_EARTH` — *„The space boundary faces a physical or virtual element where there is earth (or terrain) on the other side."* Daneben stehen `INTERNAL`, `EXTERNAL`, `EXTERNAL_WATER`, `EXTERNAL_FIRE` (letzteres meint, irreführend benannt, ein Nachbargebäude) und `NOTDEFINED` ([IfcInternalOrExternalEnum](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcinternalorexternalenum.htm)). Die fünf EPOS-Randbedingungen bilden sich damit ohne Rest ab.

**Aussagekraft ohne `ConnectionGeometry`.** Was bleibt, ist genau das, was EPOS ohnehin weiß: *welche* Fläche *welchen* Raum gegen *was* begrenzt, und bei 2a *welche* Grenze auf der anderen Seite dieselbe Wand belegt. Was fehlt, ist *wie groß* und *wo* — die Größe liefert stattdessen `Qto_*BaseQuantities` am Bauteil, der Ort gar nicht. Für einen Empfänger, der eine Hüllflächenbilanz rechnet, reicht das. Für einen Empfänger, der daraus ein Strahlungsmodell oder eine Verschattung aufbaut, reicht es nicht — und genau das erwartet die Norm auch: *„In most view definitions the 3D connection surface geometry is required."* ([ebd.](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcrelspaceboundary.htm)) Der bSI-Implementierungsvereinbarung „Space Boundary Addon View" (IFC2x3 TC1, Final) liegt dieselbe Erwartung zugrunde ([MVD-Datenbank](https://github.com/buildingSMART/technical.buildingsmart.org/blob/main/MVD-Database.md), [SB 1.1 PDF](https://standards.buildingsmart.org/MVD/RELEASE/IFC2x3/TC1/SB1_1/IFC%20Space%20Boundary%20Implementation%20Agreement%20Addendum%202010-03-22.pdf)).

**Konsequenz für EPOS:** die logischen Raumgrenzen schreiben — sie kosten fast nichts und sind die einzige Stelle, an der die Topologie des Zonenmodells überhaupt abgebildet wird — aber sie im Beipackzettel und im `Description`-Feld als logisch kennzeichnen, damit niemand sie für vermessene Flächen hält. Die Kennzeichnung nach der Archicad-Praxis (`Name = '2ndLevel'`, `Description = '2a'`/`'2b'`, Befund C, Abschnitt 6) sollte EPOS **zusätzlich** zur richtigen Entität `IfcRelSpaceBoundary2ndLevel` setzen: die Spezifikation nennt diese Namenskonvention selbst als das Unterscheidungsmerkmal, und Leser, die nur auf den Namen prüfen, finden die Grenzen dann auch.

### 1.5 Aufbauten: `IfcMaterialLayerSet`, **nicht** `IfcMaterialLayerSetUsage`

Der Unterschied ist für einen geometrielosen Export entscheidend. `IfcMaterialLayerSetUsage` verortet den Schichtstapel gegenüber der Bauteilachse und setzt dafür Geometrie voraus: *„the OffsetFromReferenceLine shall match the exact positions between the two shape representations […] that is the IfcShapeRepresentation's with RepresentationIdentifier=\"Axis\" and RepresentationIdentifier=\"Body\""* ([IfcMaterialLayerSetUsage](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcmaterialresource/lexical/ifcmateriallayersetusage.htm)). Ohne Körper gibt es keine Achse, gegen die sich der Versatz prüfen ließe.

EPOS verknüpft deshalb das `IfcMaterialLayerSet` **unmittelbar** über `IfcRelAssociatesMaterial` mit dem Bauteil — `RelatingMaterial` ist vom Typ `IfcMaterialSelect`, und `IfcMaterialLayerSet` ist darin enthalten ([IfcRelAssociatesMaterial](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcrelassociatesmaterial.htm)). Reihenfolge und Dicken bleiben erhalten, die Innen-/Außen-Orientierung geht verloren; sie gehört in `LayerSetName` oder in eine Eigenschaft des EPOS-Satzes. In Stufe S3 (schematische Körper) kann `IfcMaterialLayerSetUsage` nachgezogen werden.

### 1.6 Der eigene Eigenschaftssatz — die Namensregel ist scharf

Die Regel steht wörtlich in `IfcPropertySet` ([IfcPropertySet](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifckernel/lexical/ifcpropertyset.htm)):

> „The naming convention \"Pset_Xxx\" applies to all those property sets that are defined as part of this specification and it shall be used as the value of the Name attribute. In addition any user defined property set can be captured. Property sets that are not declared as part of the IFC specification shall have a Name value **not including the \"Pset_\" prefix**."

Dasselbe für Mengen: *„the quantity set definitions that are part of this standard start with the prefix \"Qto_\""* ([IFC4.3, Introduction](https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/content/introduction.htm)). Der bSI-Validierungsdienst prüft das aktiv — Regel PSE001 stellt sicher, *„that when a property set starts with `Pset_` it is indeed one of the standard ones"* ([bSI-Forum: Contradictory rules for Pset prefix usage](https://forums.buildingsmart.org/t/contradictory-rules-for-pset-prefix-usage/5949)).

**Der Vorschlag `EPOS_Ergebnis` ist damit regelkonform.** Empfehlung für die Umsetzung:

- Ein durchgängiges Herstellerpräfix `EPOS_` für alle eigenen Sätze (`EPOS_Ergebnis`, `EPOS_Bauteil`, `EPOS_Rechenlauf`). Die Vorgehensweise ist branchenüblich; Vectorworks etwa schreibt seinen Anwendern `VwPset_` oder `ePset_` vor ([Vectorworks-Hilfe](https://app-help.vectorworks.net/2017/eng/VW2017_Guide/IFC/Using_Custom_IFC_Property_Sets.htm)).
- **Nie** `Pset_EPOS…` oder `Qto_EPOS…`.
- Jede Größe als `IfcPropertySingleValue`. `Name` (`IfcIdentifier`) ist Pflicht, `NominalValue` (`IfcValue`) und `Unit` (`IfcUnit`) sind OPTIONAL; zur Einheit sagt die Spezifikation: *„Unit for the nominal value, if not given, the default value for the measure type (given by the TYPE of nominal value) is used as defined by the global unit assignment at IfcProject."* ([IfcPropertySingleValue](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcpropertyresource/lexical/ifcpropertysinglevalue.htm)).

Daraus folgt eine Entscheidung, die bewusst zu treffen ist: **Heizwärme in kWh ist keine SI-Einheit.** Wer `IfcEnergyMeasure(12500.0)` ohne `Unit` schreibt, behauptet Joule, sobald die globale Einheitenzuweisung `ENERGYUNIT` auf Joule stellt. Zwei saubere Wege: entweder in Joule schreiben und im `Description`-Feld der Eigenschaft die kWh-Entsprechung nennen, oder an *jeder* Energie-Eigenschaft ein explizites `Unit` mitgeben. Der zweite Weg ist der ehrlichere und kostet eine einmalige Helferfunktion. Die Größen `Spitzenlast` (`IfcPowerMeasure`, W) und `Raumtemperatur` (`IfcThermodynamicTemperatureMeasure`) sind unkritisch.

Vorschlag für den Satz an `IfcSpace` bzw. `IfcBuilding`:

| Eigenschaft | Typ | Bemerkung |
|---|---|---|
| `Heizwaermebedarf` | `IfcEnergyMeasure` | mit explizitem `Unit`; Jahreswert |
| `HeizwaermebedarfFlaechenbezogen` | `IfcReal` | kWh/(m²·a), Einheit im `Description` |
| `Heizlast` | `IfcPowerMeasure` | Spitzenlast |
| `RaumtemperaturMittel` / `-Max` | `IfcThermodynamicTemperatureMeasure` | |
| `Rechenmodell` | `IfcLabel` | z. B. „VDI 6007-1, Zweikapazitätenmodell" |
| `Programmfassung` | `IfcLabel` | EPOS-Version |
| `Rechenzeitpunkt` | `IfcDateTime` | |
| `Wetterdatensatz` | `IfcLabel` | Klimazone nach DIN 4710 |

Die letzten vier sind der eigentliche Wert des Satzes: ein Ergebnis ohne Angabe, *womit* es gerechnet wurde, ist in fremder Hand wertlos. Mengen gehören nicht hierher, sondern in `IfcElementQuantity` — dessen `MethodOfMeasurement` (`IfcLabel`, OPTIONAL) ist die vorgesehene Stelle für „nach VDI 6007" ([IfcElementQuantity](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcproductextension/lexical/ifcelementquantity.htm)).

### 1.7 Welche MVD passt — keine, und das ist in Ordnung

Die offizielle MVD-Datenbank von buildingSMART führt für IFC4 ADD2 TC1 genau fünf Einträge ([MVD-Database.md](https://github.com/buildingSMART/technical.buildingsmart.org/blob/main/MVD-Database.md)):

| MVD | Schema | Status | Für EPOS? |
|---|---|---|---|
| Reference View 1.2 | IFC4 ADD2 TC1 | **Final** | nein, verlangt Geometrie |
| Design Transfer View 1.1 | IFC4 ADD2 TC1 | Draft | nein |
| Quantity Takeoff View | IFC4 ADD2 TC1 | Draft | nein, keine Doku verlinkt |
| **Energy Analysis View** | IFC4 ADD2 TC1 | **Draft** | inhaltlich am nächsten, aber ohne jede Dokumentation — der Eintrag verlinkt nur den Kürzel „EV" |
| Product Library View 0.1 | IFC4 ADD2 TC1 | Draft | nein |

**Eine MVD „Property Set Exchange" gibt es nicht** — weder für IFC4 noch für IFC2x3. Der „Basic FM Handover View" existiert, ist aber **IFC2x3 TC1**, nicht IFC4 ([ebd.](https://github.com/buildingSMART/technical.buildingsmart.org/blob/main/MVD-Database.md)); er verlangt außerdem Space Boundaries und Basismengen, also mehr als EPOS liefert.

Die Reference View scheidet aus einem einzigen Satz aus. Ihr Zweckkapitel zählt auf, was ausgetauscht wird: *„physical elements with **explicit geometry**, properties, quantities, material, and classification"* und *„spatial elements (spaces, zones) with **explicit geometry**, properties, quantities, and classification"* ([Reference View 1.2, Kapitel 1.1](https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/views/reference-view/index.htm)). Ohne Körper ist die Datei keine RV-Datei.

**Das ist kein Mangel, solange es benannt wird.** Die xBIM-Dokumentation formuliert die Lage präzise: *„This IFC doesn't define any Model View Definition (MVD) so there are no additional restrictions apart from WHERE rules and required properties."* ([docs.xbim.net](https://docs.xbim.net/examples/basic-model-operations.html)) EPOS schreibt also `FILE_DESCRIPTION` **ohne** `ViewDefinition[…]`-Angabe. Eine falsche MVD-Behauptung wäre schlimmer als keine.

Zwei Dinge treten an die Stelle der MVD:

1. **Der bSI-Validierungsdienst** prüft STEP-Syntax, Schema (einschließlich der EXPRESS-Regeln) und normative Regeln. Er prüft ausdrücklich **keine** Darstellung: *„For multiple reasons, geometric visualisation is not within the scope or the mandate of the Validation Service. Many errors are invisible in a viewer or unrelated to a geometric representation or prevent visualisation altogether."* ([Validation Service](https://github.com/buildingSMART/technical.buildingsmart.org/blob/main/validation-service.md), Dienst unter [validate.buildingsmart.org](https://validate.buildingsmart.org/)). Eine geometrielose EPOS-Datei kann dort vollständig bestehen — das ist der belastbarste verfügbare Konformitätsnachweis und gehört in die Abnahme.
2. **IDS** (Information Delivery Specification) ist das passende Werkzeug für die Anforderungsseite, weil es genau auf EPOS' Datenumfang zugeschnitten ist: IDS ist auf alphanumerische Information begrenzt — Eigenschaften, Mengen, Klassifikationen, Materialien und Beziehungen — und deckt Geometrie nicht ab ([bSI, IDS](https://technical.buildingsmart.org/projects/information-delivery-specification-ids/)). EPOS kann seine Exportzusage als IDS-Datei mitliefern; das ist billiger als eine eigene MVD und wird von Prüfwerkzeugen gelesen.

### 1.8 Öffnet ein Betrachter so eine Datei?

Hier ist die Quellenlage dünn, und der Befund muss ehrlich bleiben: **es gibt keinen veröffentlichten, systematischen Test** geometrieloser IFC-Dateien über Archicad, Revit, Solibri, BIMcollab und die xbim-Betrachter hinweg. Was belegbar ist:

- Die xBIM-Dokumentation sagt zu ihrem eigenen geometrielosen Beispiel schlicht: *„This wall doesn't have any geometry so most of IFC viewers won't show you anything."* ([docs.xbim.net](https://docs.xbim.net/examples/basic-model-operations.html)) Das ist die Kernaussage — die Datei **öffnet**, das 3D-Fenster **bleibt leer**.
- BIMvision führt „some parts may have no geometry" als eine der Ursachen dafür, dass nichts dargestellt wird, und beschreibt dennoch die Navigation über den Objektbaum ([BIMvision Help Center](https://helpcenter.bimvision.eu/question/bimvision-cant-show-geometry-of-a-file/)). Betrachter dieser Bauart (BIMvision, FZKViewer) zeigen Struktur- und Eigenschaftsbaum unabhängig von der Darstellung.
- Für Revit gilt: `IfcSpace` aus einer verknüpften IFC-Datei wird als `DirectShape` mit gemeinsam genutzten Parametern abgebildet, nicht als Revit-Raum ([Autodesk/revit-ifc, Issue 15](https://github.com/Autodesk/revit-ifc/issues/15)). Ein `DirectShape` ohne Körper ist nichts — die Semantik landet also bestenfalls in Parametern, sichtbar wird nichts.
- Archicad kennt umgekehrt selbst die Option, Zonengeometrie beim IFC-Export wegzulassen ([Graphisoft-Hilfe, Model View Definitions](https://help.graphisoft.com/AC/25/INT/_AC25_Help/121_IFC/121_IFC-46.htm)) — der Fall „IfcSpace ohne Körper" ist der Branche also vertraut.

**Bewertung.** Für ein *Rechenwerkzeug* als Gegenüber ist die Sache unproblematisch. Für einen *Menschen mit Betrachter* ist eine geometrielose Datei praktisch wertlos und erzeugt den Eindruck, der Export sei kaputt. Das ist der stärkste Einzelgrund für Stufe S3 — und der Grund, warum der Befund empfiehlt, S1 nie ohne begleitenden Text auszuliefern („Diese Datei enthält Fachdaten ohne Bauteilgeometrie"). **Für die Abnahme ist eine eigene Prüfmatrix zu erheben** (Abschnitt 6, offene Fragen): die veröffentlichte Quellenlage ersetzt sie nicht.

---

## 2. Vereinfachte Körper ohne Kernel

### 2.1 Es ist wirklich nur Schreibarbeit

Der Quader über einem Rechteckprofil braucht vier Entitäten und **keine einzige boolesche Operation**:

`IfcExtrudedAreaSolid` hat die Pflichtattribute `SweptArea` (`IfcProfileDef`), `Position` (`IfcAxis2Placement3D`), `ExtrudedDirection` (`IfcDirection`) und `Depth` (`IfcPositiveLengthMeasure`) ([IfcExtrudedAreaSolid](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcgeometricmodelresource/lexical/ifcextrudedareasolid.htm)). `IfcRectangleProfileDef` braucht `ProfileType` (`AREA`), `XDim`, `YDim`; `Position` (`IfcAxis2Placement2D`) ist OPTIONAL — *„If unspecified, no translation and no rotation is applied."* ([IfcRectangleProfileDef](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcprofileresource/lexical/ifcrectangleprofiledef.htm)).

Das xBIM-Beispiel zeigt die vollständige Kette in etwa fünfzehn Zeilen, samt der beiden Kennzeichnungen, auf die es ankommt — `RepresentationIdentifier = "Body"`, `RepresentationType = "SweptSolid"` ([docs.xbim.net, Proper Wall in 3D](https://docs.xbim.net/examples/proper-wall-in-3d.html)):

```csharp
var rectProf = model.Instances.New<IfcRectangleProfileDef>();
rectProf.ProfileType = IfcProfileTypeEnum.AREA;
rectProf.XDim = width;  rectProf.YDim = length;

var body = model.Instances.New<IfcExtrudedAreaSolid>();
body.Depth = height;  body.SweptArea = rectProf;
body.ExtrudedDirection = model.Instances.New<IfcDirection>();
body.ExtrudedDirection.SetXYZ(0, 0, 1);

var shape = model.Instances.New<IfcShapeRepresentation>();
shape.ContextOfItems = modelContext;
shape.RepresentationType = "SweptSolid";
shape.RepresentationIdentifier = "Body";
shape.Items.Add(body);

var rep = model.Instances.New<IfcProductDefinitionShape>();
rep.Representations.Add(shape);
wall.Representation = rep;
```

Ab dem Augenblick, in dem `Representation` gesetzt ist, greift `PlacementForShapeRepresentation`: **jedes Bauteil braucht dann auch ein `ObjectPlacement`** ([IfcProduct](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifckernel/lexical/ifcproduct.htm)). Die Placement-Kette ist `IfcProject` → `IfcSite` → `IfcBuilding` → (`IfcBuildingStorey`) → Bauteil, jeweils `IfcLocalPlacement` mit `PlacementRelTo` auf die Ebene darüber und `RelativePlacement` als `IfcAxis2Placement3D`.

### 2.2 Achse und Nordrichtung

`TrueNorth` sitzt am `IfcGeometricRepresentationContext`, ist OPTIONAL, eine **zweidimensionale** Richtung in der xy-Ebene, und *„If not present, it defaults to 0. 1., meaning that the positive Y axis of the project coordinate system equals the geographic northing direction."* Steht zusätzlich eine `IfcMapConversion` im Modell, ist `TrueNorth` nur noch informativ ([IfcGeometricRepresentationContext](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcrepresentationresource/lexical/ifcgeometricrepresentationcontext.htm)).

**Empfehlung: `TrueNorth` auf der Vorgabe lassen (+Y = Nord) und den Azimut ausschließlich in der Drehung des jeweiligen Wand-Placements ausdrücken.** Beim Import ist die umgekehrte Rechnung (Placement-Kette mal `TrueNorth`) die bekannte Fehlerquelle (Befund C, Abschnitt 5); beim Export gilt dasselbe in Grün — je weniger Drehungen sich überlagern, desto weniger kann schiefgehen. `RefDirection` der Wand ergibt sich unmittelbar aus dem EPOS-Azimut α (0° = Nord, im Uhrzeigersinn): Wandnormale nach außen zeigend, Wandachse senkrecht dazu. Die Neigung geht in die Extrusionsrichtung; bei 90°-Wänden bleibt sie `(0,0,1)`, bei Dachschrägen ist sie zu kippen — oder das Dach wird als flache Platte mit `IfcSlab`/`PredefinedType = ROOF` geschrieben und die Neigung bleibt eine Eigenschaft.

### 2.3 Was es bringt, was es kostet

**Es bringt:** die Datei öffnet sich sichtbar; ein Bearbeiter erkennt in zwei Sekunden, ob acht Wände statt vier entstanden sind oder ob ein Fenster die Größe der Wand hat. Das ist eine echte Plausibilitätsprüfung, die kein Zahlenausdruck ersetzt. Außerdem wird `IfcMaterialLayerSetUsage` erst damit zulässig (Abschnitt 1.5), und `IfcRelSpaceBoundary` könnte eine echte `ConnectionGeometry` bekommen.

**Es kostet:** die Anordnung ist erfunden. EPOS kennt keine Grundrisse — die Wände stehen nicht an ihrem Ort, sie stehen dort, wo der Exporter sie hinlegt. Eine Datei, die *aussieht* wie ein Gebäude, aber keines ist, ist gefährlicher als eine, die offensichtlich leer ist: sie lädt zur Verwechslung mit einem Architekturmodell ein, und ihre Maße würden in einem Aufmaß-Werkzeug klaglos abgegriffen.

**Vorschlag für die schematische Anordnung**, wenn S3 kommt:

- Je Zone ein Quader aus Fläche und Höhe (Kantenlänge = √Fläche, wenn kein Seitenverhältnis bekannt ist), Zonen nebeneinander in einer Reihe mit sichtbarem Abstand — keine Stapelung, weil Geschosszuordnung nicht bekannt ist.
- Je Bauteil eine Platte aus Bruttofläche × Dicke, am Azimut ausgerichtet, an der zugehörigen Zonenwand angelehnt. Fenster als kleinere Platte **vor** der Wand, nicht ausgeschnitten (kein `IfcRelVoidsElement`, das wäre wieder ein Kernelthema).
- Ein `IfcAnnotation` oder wenigstens ein `IfcLabel` im Projektnamen, das die Datei als schematisch kennzeichnet — und dieselbe Kennzeichnung in `FILE_DESCRIPTION`.

**Bewertung: semantischer Export gegen schematische Körper.**

| | S1 semantisch | S3 schematisch |
|---|---|---|
| Schemakonform | ja | ja |
| bSI-Validierung | besteht | besteht |
| MVD | keine | keine (RV verlangt mehr als Quader) |
| Betrachter | leer | zeigt etwas |
| Verwechslungsgefahr | keine | **hoch** |
| Zusatzaufwand | — | Placement-Kette, Achsen, Anordnungsregel, Prüfbilder |
| Nutzen für Rechenwerkzeuge | voll | unverändert |

Die Rechnung fällt klar aus: **S3 kauft Anschaulichkeit und bezahlt mit Verwechslungsgefahr.** Es lohnt erst, wenn im Feld tatsächlich jemand die Datei in einem Betrachter erwartet — und dann nur mit sichtbarer Kennzeichnung.

---

## 3. Rückgabe angereicherter Dateien (Round-Trip)

Das ist der Fall mit dem besten Verhältnis von Nutzen zu Aufwand: die Fremddatei bringt Geometrie und Struktur mit, EPOS ergänzt nur, was fehlt — U-Werte, Baustoffe, Ergebnisse.

### 3.1 Speichert xBIM vollständig zurück?

**Ja, für alles, was der Leser verstanden hat.** `Part21Writer.Write` läuft schlicht über alle Instanzen des Modells und schreibt jede einzelne heraus ([Part21Writer.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/Step21/Part21Writer.cs)):

```csharp
foreach (var entity in model.Instances)
{
    WriteEntity(entity, output, metadata, map);
    output.WriteLine();
}
```

Der Kopf kommt aus `model.Header`, einschließlich `FILE_NAME` mit `AuthorName`, `Organization`, `PreprocessorVersion` und `OriginatingSystem` sowie `FILE_SCHEMA` ([`WriteHeader`, ebd.](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/Step21/Part21Writer.cs)). **Die Schemafassung bleibt damit von allein erhalten** — eine IFC2x3-Datei wird als IFC2x3 zurückgeschrieben, eine IFC4-Datei als IFC4. Das ist die gewünschte Eigenschaft und erspart jede Konvertierung.

### 3.2 Die eine echte Verlustquelle

Was der Leser **nicht** instanziieren konnte, ist weg. Der Parser protokolliert und geht weiter ([XbimP21Scanner.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/Step21/XbimP21Scanner.cs)):

```csharp
Logger?.LogError(LogEventIds.FailedEntity, e, $"Could not create type {entityTypeName}");
```

Dazu kommen `Logger?.LogWarning("Entity #{0,-5} is referenced but could not be instantiated", …)` und ein ausdrücklicher `SkipTypes`-Mechanismus, den der Aufrufer über `ignoreTypes` setzen kann ([ebd.](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/Step21/XbimP21Scanner.cs)). Betroffen sind herstellereigene Erweiterungen und Entitäten aus einer neueren Schemafassung, als der geladene Entitätenerzeuger kennt.

**Folge für die Umsetzung:** Der Round-Trip ist nur dann zulässig, wenn der Protokollmitschnitt des Lesens **leer von `FailedEntity`-Einträgen** ist. Gibt es welche, muss EPOS die Rückgabe verweigern und stattdessen eine eigene Datei schreiben (S1). Das ist eine Prüfung von zehn Zeilen und der einzige Schutz gegen stille Datenvernichtung in einer fremden Datei. Der `ILoggerFactory`, den `MemoryModel` ohnehin entgegennimmt, liefert den Mitschnitt frei Haus ([MemoryModel.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.IO.MemoryModel/MemoryModel.cs)).

### 3.3 GUID-Stabilität und `OwnerHistory`

Vorhandene `GlobalId`-Werte sind gewöhnliche Attribute und werden unverändert zurückgeschrieben — GUID-Stabilität für Bestandsentitäten ist also kostenlos. Für **neue** Entitäten (die Eigenschaftssätze, die Beziehungen) gilt:

- **Mit `IfcStore`:** `IfcRootInit` setzt automatisch `root.OwnerHistory = OwnerHistoryAddObject; root.GlobalId = Guid.NewGuid().ToPart21();`, und beim Ändern bestehender Objekte wird eine `MODIFIED`-Historie erzeugt, die Erstellungsdatum, Benutzer und Anwendung aus der alten übernimmt ([IfcStore.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Ifc/IfcStore.cs)). Der Quelltext kommentiert das selbstkritisch: *„This is naive. We have a store-wide OwnerHistory for modifications when really we […] OwnerHistory is not really fit for purpose."*
- **Mit `MemoryModel`:** **nichts davon geschieht.** `IfcRootInit` ist eine Methode von `IfcStore`, nicht des Modells. `GlobalId` ist ein Pflichtattribut — wird es nicht gesetzt, entsteht `$` an Position 1 und damit eine schemawidrige Datei.

Da Befund N `IfcStore` für den Kern ausgeschlossen hat (Esent, nicht iOS-tragbar), ist das eine **verbindliche Auflage an die Umsetzung**: eine EPOS-eigene Erzeugungsfunktion, die für jede `IIfcRoot`-Instanz `GlobalId` und `OwnerHistory` setzt. Für `GlobalId` bietet `IfcGloballyUniqueId` die Umwandlung in beide Richtungen und eine implizite Umwandlung aus `System.Guid` ([IfcGloballyUniqueIdPartial.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Ifc4/UtilityResource/IfcGloballyUniqueIdPartial.cs)) — damit lassen sich aus den EPOS-Schlüsseln **deterministische** GUIDs ableiten (namensbasierte UUID), so dass ein zweiter Export derselben Zone dieselbe `GlobalId` trägt. Das ist die Voraussetzung für einen sinnvollen Modellvergleich beim Empfänger und sollte von Anfang an so gebaut werden; nachträglich ist es nicht mehr zu ändern, ohne alte Exporte zu entwerten.

### 3.4 Risiken der Rückgabe

1. **Die Datei behauptet weiter ihre Herkunft.** `OriginatingSystem` und `PreprocessorVersion` bleiben stehen (Abschnitt 3.1). Eine von EPOS veränderte Datei sieht im Kopf aus wie das Original. **Auflage:** EPOS ergänzt `FILE_DESCRIPTION` um einen Vermerk, legt eine eigene `IfcApplication` an und speichert **immer unter neuem Namen** — die Eingangsdatei wird nie überschrieben. Das ist auch die Hausregel „nichts wird ohne OK geschrieben" (Befund N, Abschnitt 1.4).
2. **Urheber- und Lizenzlage des Fremdmodells.** Ein Architekturmodell ist ein Werk; die Reference View benennt die Erwartungshaltung ausdrücklich: *„The source of the BIM information remains with the originator"*, *„The receiver of the IFC4 Reference View is not supposed to modify the model"*, und Änderungswünsche gehören als BCF-Meldung zurück an den Urheber ([Reference View 1.2](https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/views/reference-view/index.htm)). EPOS' Anreicherung ist streng genommen genau das, was die RV nicht vorsieht. **Das ist kein technisches, sondern ein vertragliches Thema** und gehört in den Beipackzettel und in die Anwenderführung: Der Anwender muss wissen, dass er eine fremde Datei verändert weitergibt.
3. **Bestehende Sätze doppeln.** Trägt die Wand schon ein `Pset_WallCommon` mit `ThermalTransmittance`, darf EPOS es nicht ein zweites Mal anlegen. Regel: vorhandenen Satz gleichen Namens suchen, Eigenschaft ersetzen, sonst anlegen. Die EPOS-Herkunft bleibt im eigenen `EPOS_Bauteil`-Satz nachvollziehbar.
4. **Dateigröße und Speicher.** Der Round-Trip hält das ganze Modell im Arbeitsspeicher. Für iOS ist die Größenbegrenzung aus Befund N (Abschnitt 1.7) zu übernehmen und beim Schreiben erneut zu prüfen.

---

## 4. Die xBIM-Schreib-API im Einzelnen

### 4.1 Der dokumentierte Weg (nicht der EPOS-Weg)

Die Dokumentation zeigt ([docs.xbim.net](https://docs.xbim.net/examples/basic-model-operations.html)):

```csharp
var editor = new XbimEditorCredentials { ApplicationDevelopersName = …,
    ApplicationFullName = …, ApplicationIdentifier = …, ApplicationVersion = …,
    EditorsFamilyName = …, EditorsGivenName = …, EditorsOrganisationName = … };

using (var model = IfcStore.Create(editor, IfcSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
{
    using (var txn = model.BeginTransaction("Hello Wall")) { …; txn.Commit(); }
    model.SaveAs("BasicWall.ifc");
}
```

**Zwei Abweichungen zur ausgelieferten Fassung 6.1.605**, gegen den Quelltext geprüft:

- Der Aufzählungstyp heißt heute `XbimSchemaVersion`, nicht `IfcSchemaVersion` — die Dokumentationsseite stammt aus der xBIM-4-Zeit (sie trägt `ApplicationVersion = "4.0"`). Die Signatur lautet `public static IfcStore Create(XbimEditorCredentials editorDetails, XbimSchemaVersion ifcVersion, XbimStoreType storageType)` ([IfcStore.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Ifc/IfcStore.cs)).
- `SaveAs` lautet `public void SaveAs(string fileName, StorageType? format = null, ReportProgressDelegate progDelegate = null)`; der Aufruf `model.SaveAs(pfad, StorageType.Ifc)` ist richtig, und ohne Angabe wird `StorageType.Ifc` als Vorgabe gewählt ([ebd.](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Ifc/IfcStore.cs)).

**`IfcStore` gehört trotzdem nicht in den Kern** — es lebt im Paket `Xbim.Ifc`, und dessen nuspec listet `Xbim.IO.Esent` als Abhängigkeit, die ihrerseits `Microsoft.Database.ManagedEsent` zieht (geprüft an [xbim.ifc.nuspec 6.1.605](https://api.nuget.org/v3-flatcontainer/xbim.ifc/6.1.605/xbim.ifc.nuspec) und [xbim.io.esent.nuspec 6.1.605](https://api.nuget.org/v3-flatcontainer/xbim.io.esent/6.1.605/xbim.io.esent.nuspec)). Das bestätigt die Paketwahl aus Befund N, Abschnitt 2.1 — und gilt für das Schreiben genauso wie für das Lesen.

### 4.2 Der EPOS-Weg: `MemoryModel` unmittelbar

```csharp
using Xbim.Common;                 // ITransaction, ProjectUnits
using Xbim.Common.Step21;          // XbimSchemaVersion, IStepFileHeader
using Xbim.IO.Memory;              // MemoryModel
using Xbim.Ifc4;                   // EntityFactoryIfc4
using Xbim.Ifc4.Kernel;            // IfcProject, IfcPropertySet, IfcRelDefinesByProperties
using Xbim.Ifc4.ProductExtension;  // IfcSite, IfcBuilding, IfcSpace, IfcZone, IfcElementQuantity
using Xbim.Ifc4.SharedBldgElements;// IfcWall, IfcSlab, IfcRoof, IfcWindow
using Xbim.Ifc4.PropertyResource;  // IfcPropertySingleValue
using Xbim.Ifc4.QuantityResource;  // IfcQuantityArea, IfcQuantityVolume
using Xbim.Ifc4.MaterialResource;  // IfcMaterial, IfcMaterialLayer, IfcMaterialLayerSet
using Xbim.Ifc4.MeasureResource;   // IfcLabel, IfcAreaMeasure, IfcThermalTransmittanceMeasure …

using var model = new MemoryModel(new EntityFactoryIfc4());
using (var txn = model.BeginTransaction("EPOS-Export"))
{
    var project = model.Instances.New<IfcProject>(p => p.Name = "…");
    project.Initialize(ProjectUnits.SIUnitsUK);
    // … Entitäten anlegen, GlobalId und OwnerHistory selbst setzen …
    txn.Commit();
}
using var strom = File.Create(pfad);
model.SaveAsStep21(strom);
```

Nachweise der einzelnen Stücke:

- **Erzeuger.** `public sealed class EntityFactoryIfc4 : IEntityFactory` im Namensraum `Xbim.Ifc4` ([EntityFactoryIfc4.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Ifc4/EntityFactoryIfc4.cs)). Die Bauformen von `MemoryModel` sind `MemoryModel(IEntityFactory, ILoggerFactory, int labelFrom)` und `MemoryModel(IEntityFactory, IStepFileHeader, ILoggerFactory)` ([MemoryModel.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.IO.MemoryModel/MemoryModel.cs)).
- **Transaktion.** `public virtual ITransaction BeginTransaction(string name)` an `StepModel`, der Basisklasse von `MemoryModel` ([StepModel.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/Model/StepModel.cs)). Regel aus der Dokumentation: *„Transactions can't be nested so there has always be just one transaction at the time"*, und *„You have to commit transaction explicitly to keep the changes."* ([docs.xbim.net](https://docs.xbim.net/examples/basic-model-operations.html))
- **Speichern.** `public virtual void SaveAsStep21(Stream stream, ReportProgressDelegate progress = null, bool leaveOpen = false)` sowie eine Fassung mit `TextWriter`; daneben `SaveAsStep21Zip` an `MemoryModel` ([StepModel.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/Model/StepModel.cs), [MemoryModel.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.IO.MemoryModel/MemoryModel.cs)). Der Strom ist der richtige Weg für iOS (Abschnitt 4.5).
- **Einheiten und Kontext.** `IfcProject.Initialize(ProjectUnits units)` liegt in **`Xbim.Ifc4`**, nicht in `Xbim.Ifc` — der Kern kann sie also benutzen ([IfcProjectPartial.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Ifc4/Kernel/IfcProjectPartial.cs)). **Achtung:** `ProjectUnits.SIUnitsUK` setzt `LENGTHUNIT` auf `MILLI.METRE` und `MASSUNIT` auf `KILO.GRAM`; die Methode legt außerdem die beiden Kontexte „Building Model" (3D) und „Building Plan View" (2D) an. EPOS rechnet in Metern — die Längeneinheit ist nach `Initialize` zu überschreiben oder die `IfcUnitAssignment` von Hand zu bauen.
- **Was der Kern *nicht* hat:** `IIfcProjectExtensions.AddSite`/`AddBuilding` liegen in `Xbim.Ifc` ([IIfcProjectExtensions.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Ifc/Extensions/IIfcProjectExtensions.cs)) und stehen damit nicht zur Verfügung; die `IfcRelAggregates` schreibt EPOS selbst.

### 4.3 Konkrete Typen zum Schreiben, Schnittstellen zum Lesen

`model.Instances.New<T>()` verlangt einen **instanziierbaren** Typ — die Dokumentation formuliert es als Sprachregel: *„You always have to specify a non-abstract type to create. This is built in xbim in a way where you get a compile time error if you don't."* ([docs.xbim.net](https://docs.xbim.net/examples/basic-model-operations.html)) Die `Xbim.Ifc4.Interfaces.IIfc*`-Schnittstellen sind der schemaübergreifende Lesepfad — *„We have implemented IFC4 interfaces on IFC2x3 entities which means you can query IFC2x3 and IFC4 with a single codebase"* ([ebd.](https://docs.xbim.net/examples/basic-model-operations.html)). **Zum Schreiben also `Xbim.Ifc4.SharedBldgElements.IfcWall`, zum Lesen `IIfcWall`** — dieselbe Zweiteilung, die der Importentwurf schon kennt.

Die Namensräume folgen der Ordnerstruktur des Pakets (geprüft am Dateibaum von [XbimEssentials](https://github.com/xBimTeam/XbimEssentials)): `Kernel` (`IfcProject`, `IfcPropertySet`, `IfcRelDefinesByProperties`, `IfcRelAggregates`), `ProductExtension` (`IfcSite`, `IfcBuilding`, `IfcBuildingStorey`, `IfcSpace`, `IfcZone`, `IfcElementQuantity`, `IfcRelAssociatesMaterial`, `IfcRelSpaceBoundary2ndLevel`), `SharedBldgElements` (`IfcWall`, `IfcSlab`, `IfcRoof`, `IfcWindow`), `PropertyResource`, `QuantityResource`, `MaterialResource`, `MeasureResource`, `ProfileResource`, `GeometricModelResource`, `GeometricConstraintResource`, `RepresentationResource`.

### 4.4 Prüfung vor dem Speichern

`Xbim.Common.ExpressValidation.Validator` liegt in `Xbim.Common` und prüft ein ganzes Modell, eine Entitätenmenge oder eine einzelne Entität gegen die generierten EXPRESS-Regeln; die Tiefe steuert `ValidationFlags` ([Validator.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/ExpressValidation/Validator.cs), [ValidationFlags.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/Enumerations/ValidationFlags.cs)). Für `Xbim.Ifc4` liegen die Regelumsetzungen als eigener Ordner `Validation/` je Entität bei. **Empfehlung:** der Export ruft den Validator vor `SaveAsStep21` und meldet Verstöße als `PruefMeldung` — dieselbe Meldungsmechanik wie beim Import (Befund N, Abschnitt 1.2). Damit fällt eine fehlende `GlobalId` im Haus auf und nicht beim Empfänger.

### 4.5 Schema und Plattform

**Schema: IFC4 (ADD2 TC1).** `XbimSchemaVersion` kennt `Unsupported`, `Ifc4`, `Ifc4x1`, `Ifc2X3`, `Cobie2X4`, `Ifc4x3` ([XbimSchemaVersion.cs](https://github.com/xBimTeam/XbimEssentials/blob/master/Xbim.Common/Step21/XbimSchemaVersion.cs)) — **es gibt keinen eigenen Wert für ADD2 TC1**; IFC4 ADD2 TC1 ist `Ifc4` und schreibt `FILE_SCHEMA (('IFC4'));`. Das ist die richtige Wahl: `Pset_SpaceThermalRequirements` existiert dort noch (Abschnitt 1.3), `IfcRelSpaceBoundary2ndLevel` gibt es erst ab IFC4 (Befund C, Abschnitt 1), und IFC4 ist ISO 16739. **IFC2x3 zu schreiben lohnt nicht:** die 2nd-Level-Raumgrenzen fehlen dort als Entität, und das Hauptargument für 2x3 — die Verbreitung bei Empfängern — greift bei einer Datei, die ohnehin keine MVD erfüllt, nicht.

**iOS.** Der Weg steht bereits: `EPOS.iOS/Dienste/IosDateiDienst.cs` hält fest, dass iOS keinen „Speichern unter"-Dialog kennt; `DateiSpeichern` liefert stattdessen einen Pfad in den Dokumentenordner der Sandbox (über `UIFileSharingEnabled` in der App „Dateien" sichtbar), der Aufrufer schreibt dorthin, und die fertige Datei wird über `MitSystemOeffnen` per Teilen-Blatt weitergereicht. Für den Export ist damit **keine neue Plattformarbeit nötig** — der Ablauf ist derselbe wie beim vorhandenen CSV-Export. Ergänzend: `FileSystem.AppDataDirectory` zeigt unter iOS in das von iTunes/iCloud gesicherte `Library`-Verzeichnis, und absolute Sandbox-Pfade dürfen nie gespeichert werden, weil sie eine bei jeder Neuinstallation wechselnde GUID enthalten ([Microsoft Learn, File system helpers](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-system-helpers?view=net-maui-10.0)). Da `SaveAsStep21` einen `Stream` nimmt, kann EPOS unmittelbar in den vom Dateidienst gelieferten Pfad schreiben, ohne Zwischendatei.

**Trimming.** Derselbe `TrimmerRootDescriptor` deckt Export und Import ab (Befund N, Abschnitt 2.3) — der Export bringt kein zusätzliches Trimming-Risiko mit, weil er dieselben Baugruppen benutzt.

---

## 5. Abbildung EPOS → IFC

| EPOS | IFC-Entität | Eigenschaften / Mengen | Anmerkung |
|---|---|---|---|
| Projekt | `IfcProject` | `UnitsInContext` | Längeneinheit METRE setzen (4.2) |
| Standort/Klimaort | `IfcSite` | `RefLatitude`/`RefLongitude` als `LIST[3:4] OF INTEGER`, `RefElevation` | Vorzeichenregel beachten (1.2) |
| Gebäude | `IfcBuilding` | `Pset_BuildingCommon`: `YearOfConstruction` (Text!) aus Baualtersklasse, `NumberOfStoreys`; `EPOS_Ergebnis` am Gebäude | Baualtersklasse ist ein Band, kein Jahr — Bandmitte schreiben und den Klassennamen zusätzlich in `EPOS_Gebaeude.Baualtersklasse` |
| Zone | `IfcSpace` (`PredefinedType = SPACE`) | `Qto_SpaceBaseQuantities`: `NetFloorArea`, `GrossFloorArea`, `Height`, `NetVolume`, `GrossVolume`; `Pset_SpaceCommon`: `IsExternal=false`, `GrossPlannedArea`; `Pset_SpaceThermalRequirements`: `SpaceTemperature`, `SpaceTemperatureWinterMin`, `SpaceTemperatureSummerMax`; `Pset_SpaceOccupancyRequirements` bei bekannter Belegung | ([Qto/Pset siehe Befund C, Abschnitt 1](2026-09-15_Befund_C_IFC-Recherche.md)) |
| Zone (Gruppierung) | zusätzlich `IfcZone` + `IfcRelAssignsToGroup` | `Pset_SpaceThermalRequirements` ist auch an `IfcZone` zulässig | Nur bei Mehrzonenmodell; bei Einzonenmodell weglassen |
| Nutzungsprofil | `IfcSpace.LongName` + `Pset_SpaceCommon.Category` | Nutzungsbezeichnung im Klartext | Klassifikation über `IfcRelAssociatesClassification`, wenn EPOS eine Nutzungsliste mit Kennungen führt |
| Bauteilgruppe / Bauteil | `IfcWall` (`PredefinedType = SOLIDWALL`/`STANDARD`), `IfcSlab` (`FLOOR`/`BASESLAB`/`ROOF`), `IfcRoof`, `IfcWindow` | `Pset_*Common`: `ThermalTransmittance`, `IsExternal`; `Qto_WallBaseQuantities.GrossSideArea`, `Qto_SlabBaseQuantities.GrossArea`, `Qto_WindowBaseQuantities.Area` | `IfcSlabTypeEnum`: `BASESLAB` = *„floor slab against the ground […] part of the foundation"*, `ROOF` = flaches oder geneigtes Dach ([IfcSlabTypeEnum](https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/schema/ifcsharedbldgelements/lexical/ifcslabtypeenum.htm)) |
| Fenster | `IfcWindow` | zusätzlich `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` (= g-Wert) | Befund C, Abschnitt 1 |
| **Azimut / Neigung** | **kein Standardplatz** | `EPOS_Bauteil`: `Azimut` (`IfcPlaneAngleMeasure`, 0° = Nord, im Uhrzeigersinn), `Neigung` (`IfcPlaneAngleMeasure`, 0° = waagerecht), `Himmelsrichtung` (`IfcLabel`, „SW") | Ohne Geometrie gibt es keine andere Möglichkeit. Die Bezugsdefinition **muss** im `Description` der Eigenschaft stehen, sonst ist die Zahl wertlos. In S3 zusätzlich als Placement-Drehung. |
| Randbedingung / Nachbarzone | `IfcRelSpaceBoundary2ndLevel` | `RelatingSpace`, `RelatedBuildingElement` (Pflicht), `PhysicalOrVirtualBoundary = PHYSICAL`, `InternalOrExternalBoundary`, `CorrespondingBoundary` bei Typ 2a; `Name='2ndLevel'`, `Description='2a'`/`'2b'` | Erdreich → `EXTERNAL_EARTH`; Außenluft → `EXTERNAL`; Nachbarzone → `INTERNAL` mit `CorrespondingBoundary`; unbeheizter Nachbarraum → `INTERNAL`, Gegenzone als `IfcSpace` mit eigenem Sollwert; **kein** `ConnectionGeometry` |
| Aufbau | `IfcMaterialLayerSet` (`MaterialLayers`, `LayerSetName`) + `IfcRelAssociatesMaterial` | Schichtreihenfolge = Reihenfolge der `IfcMaterialLayer` | **nicht** `IfcMaterialLayerSetUsage` (1.5) |
| Schicht | `IfcMaterialLayer` | `LayerThickness`, `Name`, `IsVentilated` bei Luftschichten | |
| Baustoff | `IfcMaterial` | `Pset_MaterialThermal`: `ThermalConductivity` (λ), `SpecificHeatCapacity` (c); `Pset_MaterialCommon`: `MassDensity` (ρ) | Befund C, Abschnitt 1 |
| Ergebnisse je Zone | `EPOS_Ergebnis` an `IfcSpace` | siehe Tabelle in 1.6 | |
| Ergebnisse Gebäude | `EPOS_Ergebnis` an `IfcBuilding` | Summen + Rechenmodell, Version, Zeitpunkt, Wetterdatensatz | |
| Struktur | `IfcRelAggregates` (Project→Site→Building→Space), `IfcRelContainedInSpatialStructure` (Bauteile→Building) | | WR41 verlangt die Aggregation, nicht ein Geschoss (1.1) |

**Einzonenmodell ohne Zonen.** Ein `IfcSpace` je Gebäude, aggregiert unmittelbar unter `IfcBuilding`; **kein** `IfcZone` (eine Gruppe mit einem Element erzeugt nur Rauschen). Jede Bauteilgruppe wird **ein** Element: alle Außenwände zusammen ein `IfcWall` mit `Qto_WallBaseQuantities.GrossSideArea` = Summenfläche und flächengewichtetem U-Wert. Das ist eine ehrliche Aggregation und muss auch so heißen — `Name = "Außenwände (zusammengefasst)"`, und in `EPOS_Bauteil` ein Merkmal `IstZusammenfassung = TRUE` mit `AnzahlTeilflaechen`. Wer das weglässt, liefert ein Modell mit vier Wänden aus, das aussieht wie ein Gebäude mit vier Wänden. Fenster werden je Himmelsrichtung zusammengefasst (die fünf EPOS-Sektoren aus Befund C, Abschnitt 7), weil der Azimut sonst verloren geht.

---

## 6. Empfehlung

### S1 — Semantischer Export (12–20 PT)

`IfcExportAblauf` + `IfcExportProfil` unter `EPOS.Kern/Allgemein/Export/` nach dem Muster aus Befund N, Abschnitt 1.1. Umfang: die vollständige Abbildungstabelle aus Abschnitt 5, ohne jede Geometrie. `MemoryModel` + `EntityFactoryIfc4`, Schema IFC4, Speichern über `SaveAsStep21(Stream)`. Eigene Erzeugungsfunktion für `GlobalId` (deterministisch aus EPOS-Schlüsseln) und `OwnerHistory`. Validator vor dem Speichern. Keine MVD-Angabe im Kopf, dafür ein Beipackzettel und eine mitgelieferte IDS-Datei mit der Exportzusage.

*Aufwand:* Abbildung und Sätze 6–9 PT, GUID/OwnerHistory/Validator 2–3 PT, Ablauf/Dialog/Meldungen 3–5 PT, Tests und Abnahme 1–3 PT.

*Risiken:* Einheitenentscheidung bei kWh (1.6) — früh festlegen, danach nicht mehr änderbar ohne alte Exporte zu entwerten. Deterministische GUIDs ebenso. Enttäuschung beim Anwender, der die Datei in einem Betrachter öffnet — mit Text im Dialog abfangen.

### S2 — Round-Trip-Anreicherung (8–14 PT, setzt S1 und den Import voraus)

Nur verfügbar, wenn die Zieldatei zuvor über den Import gelesen wurde. Originaldatei erneut laden, Protokoll auf `FailedEntity` prüfen (3.2) und bei Treffern die Rückgabe verweigern, Sätze ergänzen statt doppeln, unter neuem Namen speichern, `FILE_DESCRIPTION` und eine eigene `IfcApplication` ergänzen.

*Aufwand:* Wiederfinden der Zuordnung EPOS-Zone ↔ `IfcSpace.GlobalId` 3–5 PT (die Zuordnung aus dem Importdialog muss dafür **persistiert** werden — das ist eine Anforderung an den Import, die heute noch nicht drinsteht), Ergänzungslogik 3–5 PT, Schutzprüfungen und Anwenderführung 2–4 PT.

*Risiken:* die vertragliche Frage aus 3.4 Nr. 2 — vor der Umsetzung zu klären, nicht danach. Stiller Entitätenverlust, wenn die Protokollprüfung vergessen wird.

### S3 — Schematische Körper (10–18 PT, setzt S1 voraus)

Quader je Zone und Platte je Bauteil, Placement-Kette, Azimut als Drehung, `TrueNorth` auf der Vorgabe. Sichtbare Kennzeichnung als schematisch in Projektname, `FILE_DESCRIPTION` und je Element. Prüfbilder als Abnahmekriterium.

*Aufwand:* Geometrieerzeugung und Placement 5–8 PT, Anordnungsregel je Zone 2–4 PT, Kennzeichnung 1 PT, Abnahme über Betrachter 2–5 PT.

*Risiken:* Verwechslung mit einem Architekturmodell (Abschnitt 2.3) — das ist das eigentliche Risiko dieser Stufe und nicht technisch lösbar, nur durch Kennzeichnung zu mildern. Zusätzlich: sobald Körper da sind, greift `PlacementForShapeRepresentation`, und jede vergessene Placement-Zuweisung macht die Datei schemawidrig — der Validator aus 4.4 fängt das ab, wenn er da ist.

### Reihenfolge und offene Fragen

**S1 → S2 → S3.** S3 zuletzt, weil es den geringsten fachlichen und den höchsten Missverständnisertrag hat. S2 lohnt sich nur, wenn im Feld tatsächlich fremde IFC-Dateien ankommen — was Befund C (Abschnitt 6) für den Wohngebäudebestand ausdrücklich verneint. **Wenn die Praxis den Import kaum nutzt, ist S2 zu streichen und S1 der ganze Umfang.**

Zu entscheiden bzw. zu erheben:

1. **Wer ist das Gegenüber?** Ohne einen benannten Empfänger (welches Werkzeug, welcher Anwender, welcher Zweck) lässt sich zwischen S1 und S3 nicht sinnvoll wählen. Diese Frage geht an den Anwender, nicht an die Technik.
2. **Einheit der Ergebnisgrößen** — Joule mit Hinweis oder kWh mit explizitem `Unit` (1.6). Empfehlung: kWh mit explizitem `Unit`.
3. **Deterministische GUIDs ja/nein** (3.3). Empfehlung: ja, von Anfang an.
4. **Betrachter-Prüfmatrix erheben.** Eine geometrielose EPOS-Testdatei durch Archicad, Revit, Solibri, BIMcollab Zoom, FZKViewer, BIMvision und den bSI-Validierungsdienst schicken und protokollieren, was jeweils *öffnet*, was *anzeigt* und was *meldet*. Die veröffentlichte Quellenlage (1.8) reicht dafür nicht; der Test kostet etwa 1 PT und ersetzt die Vermutungen dieses Abschnitts.
5. **Vertragliche Zulässigkeit der Rückgabe fremder Dateien** (3.4 Nr. 2) — vor S2 zu klären.
6. **Persistenz der Import-Zuordnung** — Anforderung an den Import (Befund N), Voraussetzung für S2.
