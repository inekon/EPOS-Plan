# Befund P — Mehrzonenmodell: Zonen, Flächen und Materialdaten aus IFC (15.09.2026)

**Protokoll.** Befund P eines Erkundungs-Agenten (Modell Opus, nur lesend) im Auftrag des Konzepts [`../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md), Sitzung vom 15.09.2026.

---

## 0. Das Ergebnis in neun Sätzen

1. **Die Zonentopologie steht nicht in der Datei, sie ist zu erschließen:** In keiner der vier
   heruntergeladenen und ausgezählten Beispieldateien (FZK-Haus, FZK-Haus mit Space Boundaries,
   Institute mit Space Boundaries, DigitalHub) steht **eine einzige `IfcZone`** — die
   Zonenbildung muss EPOS-Plan aus Raumnamen, Klassifikation, Geschoss und Raumgrenzen
   vorschlagen und der Anwender sie bestätigen.
2. **Die Flächenrechnung ohne Geometriekernel ist belegt, nicht geschätzt:** Von 81 + 206 + 2 582
   Raumgrenzen dieser Dateien lieferten **100 %** über `IfcConnectionSurfaceGeometry` →
   `IfcCurveBoundedPlane` → `IfcPolyline` einen Polygonflächeninhalt (Trapezformel); kein
   einziger Fall brauchte eine Kurvenauswertung (§ 2.3, Messung).
3. **Die Nachbarzone kommt aus `CorrespondingBoundary`** — dort, wo echte
   `IfcRelSpaceBoundary2ndLevel`-Entitäten stehen: DigitalHub 1 724 von 1 724 Grenzen des Typs
   2a, Institute 1 472 von 1 472, FZK-mit-SB 124 von 124 — **lückenlos gefüllt** (§ 2.2).
4. **Der Archicad-Fall hat die Eigenschaft gar nicht:** Das FZK-Haus schreibt 81 Grenzen als
   Basisklasse `IFCRELSPACEBOUNDARY` mit `Name='2ndLevel'`, `Description='2a'` — diese Klasse
   **führt kein Attribut `CorrespondingBoundary`**; die Paarbildung ist dort zu rekonstruieren,
   und das Kennzeichen „2a" ist zudem pauschal auch auf die 41 Außengrenzen geschrieben (§ 2.2).
5. **Die Randbedingung je Fläche liefert IFC direkt und gut:** `InternalOrExternalBoundary`
   trennt INTERNAL / EXTERNAL / **EXTERNAL_EARTH** — im DigitalHub 1 538 / 967 / 76 Grenzen, im
   FZK-mit-SB 316 / 812 / 230 m²; das ist genau die Dreiteilung, die `Grundflaeche_Randbedingung`
   des Einzonenmodells (Konzept 6.1) je Bauteil braucht (§ 2.4).
6. **Die Stoffwerte sind der schwarze Fleck:** In allen vier Dateien trägt **kein einziger
   opaker Baustoff** brauchbare λ, ρ, c — entweder fehlt `Pset_MaterialThermal` ganz
   (DigitalHub: 0 Vorkommen bei 20 Schichtsätzen) oder es steht mit Nullen gefüllt da
   (FZK-Haus und Institute: je vier Sätze, alle Werte `0.`); der einzige gefüllte Stoff der
   Stichprobe ist „Aluminium 131198" mit 160 / 880 / 2800 (§ 3.4).
7. **Daraus folgt die Pflichtstufe des Mehrzonenmodells:** Geometrie und Topologie aus IFC,
   **Stoffwerte aus einem EPOS-Baustoffkatalog über Namensabgleich**, mit Herkunftskennzeichen
   je Zeile und Anwenderzuordnung für jeden Rest (§ 3.6).
8. **`Pset_MaterialThermal` kennt keine Temperaturableitung der Wärmeleitfähigkeit:** Die im
   Auftrag genannte `ThermalConductivityTemperatureDerivative` steht nicht in der
   buildingSMART-Definition (dort nur `SpecificHeatCapacity`, `BoilingPoint`, `FreezingPoint`,
   `ThermalConductivity`), sondern in einer fremden Ontologie; sie ist nicht zu lesen (§ 3.2).
9. **Der Prüfstand ist vorhanden und lizenzrein:** `FM_ARC_DigitalHub_with_SB_v1.ifc`
   (17,6 MB, 59 Räume, 2 582 Grenzen 2. Ebene) steht heute unter **MIT** — die in Befund C
   vermerkte fehlende Lizenzdatei ist vorhanden (§ 5.3); die E3D-Fassungen von FZK-Haus und
   Institute sind die kleineren Prüfstände mit echten 2nd-Level-Entitäten.

**Abgrenzung.** Dieser Befund beschreibt die **Stufe nach G4a**: das Einzonenmodell (G0–G2,
Konzept 4 und 6) bleibt unverändert, der Einzonen-Import ist in Befund N entworfen. Hier steht
nur, was über Befund N hinausgeht: mehrere Zonen, Bauteile je Zone, Nachbarschaft, Schichten.
Alles, was Befund N schon festlegt — Ablauf `Lesen`/`Uebernehmen`, Meldungsschlüssel
`IMP_IFC_PROT_*`, Einheitenauswertung, Azimutkette, Größenlimit, Lizenzlage xBIM,
iOS-Trimming —, gilt unverändert weiter und wird nicht wiederholt.

**Quellenlage.** Die Lexikonseiten von `standards.buildingsmart.org` antworten dem verwendeten
Abrufwerkzeug mit HTTP 403; die Schemaangaben unten stammen deshalb aus der Spiegelung der
IFC-4.3-Dokumentation unter `bimant.com` (jeweilige URL bei der Aussage) und wurden, wo
möglich, gegen die xBIM-Quelltexte auf GitHub und gegen die gemessenen Dateien gehalten. Die
Zählungen entstanden am 15.09.2026 durch Herunterladen der Dateien in den Sitzungs-Scratchpad
und Auszählen mit `grep` bzw. einer Polygonrechnung in Perl; **im Repositorium liegt davon
nichts**.

---

## 1. Zonierung — vom Raum zur thermischen Zone

### 1.1 Was `IfcSpace` trägt

Attribute nach Spezifikation ([IfcSpace, IFC 4.3](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcproductextension/lexical/ifcspace.htm)):
„A space represents an area or volume bounded actually or theoretically."

| Attribut | Typ | Pflicht | was in der Praxis darinsteht (gemessen) |
|---|---|---|---|
| `Name` | `IfcLabel` | optional | FZK-Haus: die Raumnummer `'1'`…`'7'`; DigitalHub: `'1-5'` (Geschoss-Raum) |
| `Description` | `IfcText` | optional | in allen vier Dateien `$` |
| `ObjectType` | `IfcLabel` | optional | in allen vier Dateien `$` |
| `LongName` | `IfcLabel` | optional | **hier steht die Nutzung:** `'Schlafzimmer'`, `'Bad'`, `'Buero'`, `'Wohnen'`, `'Flur'`, `'Küche'`, `'Galerie'`; DigitalHub `'Gruppenbüro 2'`, `'Open Workspace EG'` |
| `CompositionType` | `IfcElementCompositionEnum` | optional | `.ELEMENT.` |
| `PredefinedType` | `IfcSpaceTypeEnum` | optional | FZK-Haus `$`; DigitalHub und die SB-Fassungen `.INTERNAL.` |
| `ElevationWithFlooring` | `IfcLengthMeasure` | optional | DigitalHub `-0.`; FZK-Haus `$` |
| `BoundedBy` (invers) | `SET OF IfcRelSpaceBoundary` | — | der Zugang zu allen Grenzflächen des Raumes |

**Die Leseregel für den Zonennamen lautet daher: `LongName`, sonst `Name`, sonst `ObjectType`,
sonst `GlobalId`** — nicht `Name` zuerst, denn `Name` ist in beiden Praxisdateien eine Nummer.

### 1.2 `Pset_SpaceCommon` — was die Norm verspricht und was ankommt

Nach Spezifikation ([Pset_SpaceCommon](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcproductextension/pset/pset_spacecommon.htm)):
`Reference` (`IfcIdentifier`), `IsExternal` (`IfcBoolean`), `GrossPlannedArea` und
`NetPlannedArea` (`IfcAreaMeasure`), `PubliclyAccessible`, `HandicapAccessible` (`IfcBoolean`).

**Gemessen im FZK-Haus** trägt jeder der sieben Räume ein `Pset_SpaceCommon` mit genau drei
Eigenschaften: `HandicapAccessible` (`IFCBOOLEAN(.T.)`), `NaturalVentilation`
(`IFCBOOLEAN(.T.)`) und `Category` (`IFCLABEL('Allgemeines')`). Zwei Befunde daraus:

- **`IsExternal` fehlt** — die Eigenschaft, an der die Norm die Außenlage eines Raumes
  festmacht, ist in der einzigen lizenzfreien Referenzdatei nicht vorhanden. Eine Beheizungs-
  regel, die daran hängt, läuft im Feld leer.
- **Autorensysteme legen eigene Eigenschaften in das Norm-Pset**: `NaturalVentilation` und
  `Category` gehören nicht zu `Pset_SpaceCommon`. Der Leser darf ein Pset also **nicht** über
  die erwartete Eigenschaftsliste erkennen, sondern nur über den Pset-Namen, und muss
  unbekannte Eigenschaften folgenlos übergehen.
- Zugleich ist `Category='Allgemeines'` genau die Archicad-Raumkategorie, die dieselbe Datei
  ein zweites Mal als Klassifikation führt (§ 1.4) — ein Zonenhinweis, der doppelt vorliegt.

### 1.3 `Pset_SpaceOccupancyRequirements` — Nutzung, wo vorhanden

Eigenschaften nach Spezifikation ([Pset_SpaceOccupancyRequirements](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcproductextension/pset/pset_spaceoccupancyrequirements.htm)):
`OccupancyType` (`IfcLabel`, „Usage type … defined according to the national code"),
`OccupancyNumber`, `OccupancyNumberPeak` (`IfcCountMeasure`), `OccupancyTimePerDay`
(`IfcTimeMeasure`), `AreaPerOccupant` (`IfcAreaMeasure`), `MinimumHeadroom`
(`IfcLengthMeasure`), `IsOutlookDesirable` (`IfcBoolean`).

**Gemessen:** DigitalHub führt das Pset 64-mal (bei 59 Räumen — also auch an Raumtypen);
FZK-Haus, FZK-mit-SB und Institute führen es **gar nicht**. Folge für EPOS-Plan:
`OccupancyType` ist ein **Vorschlag für die Nutzungsgruppe** einer Zone und
`OccupancyNumber`/`AreaPerOccupant` ein **Vorschlag für die inneren Gewinne** — beides zur
Anzeige, nie zur stillen Übernahme, denn die Belegung ist Planungsvorgabe des Architekten, nicht
Betriebsgröße. Dieselbe Zurückhaltung gilt schon im Einzonenweg (Befund N, 4.3, Zeile
`Waermegewinne`).

### 1.4 Klassifikation — `IfcRelAssociatesClassification` → `IfcClassificationReference`

`IfcClassificationReference` ist „a reference into a classification system or source for a
specific classification key (or notation)"
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcexternalreferenceresource/lexical/ifcclassificationreference.htm));
Attribute `Location` (URI), `Identification` (der Schlüssel im Quellsystem), `Name` (lesbare
Bezeichnung), `ReferencedSource` (`IfcClassification` oder eine übergeordnete Referenz),
`Description`, `Sort`. Die Bindung an Objekte läuft über `IfcRelAssociatesClassification`
(`RelatedObjects` → `RelatingClassification`); invers steht sie am Referenzobjekt als
`ClassificationRefForObjects`.

**Gemessen im FZK-Haus:** genau eine Klassifikationsbindung —
`IFCRELASSOCIATESCLASSIFICATION(…,'AC Zone Category',$,(#20909,#21283,#21640,#33774,#34191,#34763,#76214),#21169)`
über **alle sieben Räume**, und die Referenz lautet
`IFCCLASSIFICATIONREFERENCE($,'000','Allgemeines',$,$,$)`. Das ist der Weg, auf dem eine
DIN-277-Nutzungsart, ein Raumbuchschlüssel oder — wie hier — die Raumkategorie des
Autorensystems ankommt: **`Identification` ist der Schlüssel („000", bei DIN 277 etwa „2.1"),
`Name` der Klartext.**

**Regel für EPOS-Plan:** Der Leser sammelt je Raum alle Klassifikationsreferenzen als Paar
(`ReferencedSource.Name` bzw. `Location`, `Identification`, `Name`) ein und bietet **jede
vorgefundene Klassifikationsquelle als eigenen Gruppierungsschlüssel** im Zonendialog an. Er
deutet keinen Schlüssel um: dass „2.1" nach DIN 277 Wohnen bedeutet, ist eine Zuordnung, die der
Anwender einmal trifft und die — wenn sie sich lohnt — in einer Zuordnungstabelle
Klassifikationsschlüssel → EPOS-Nutzungsgruppe abgelegt wird, nicht im Leser fest verdrahtet.
Im Bestand gibt es keine solche Tabelle; sie wäre eine neue Stammtabelle nach dem Muster des
Bauteilkatalogs (Konzept 6.3).

### 1.5 `IfcZone` — da, wenn da

„A zone is a group of spaces, partial spaces or other zones. Zone structures may not be
hierarchical (in contrary to the spatial structure of a project)"
([IfcZone](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcproductextension/lexical/ifczone.htm)).
In `RelatedObjects` sind laut Regel WR1 nur `IfcSpace`, `IfcZone` und `IfcSpatialZone` erlaubt;
„one individual IfcSpace may be associated with zero, one, or several IfcZone's". Die Bindung
läuft über `IfcRelAssignsToGroup`, am Zonenobjekt invers `IsGroupedBy`. `LongName` trägt den
Langnamen der Zone.

Zwei Fallen, beide schon im Konzept benannt (7.2, 7.5 Nr. 9) und hier bestätigt:

- **Schachtelung entschachteln.** Eine Zone darf Zonen enthalten; wer die Räume nicht über den
  transitiven Abschluss sammelt und dabei Mehrfachtreffer entfernt, zählt Flächen doppelt.
- **Mehrfachzuordnung auflösen.** Ein Raum kann in mehreren Zonen liegen (Brandabschnitt,
  Lüftungszone, Mietbereich). Eine **thermische** Zone verlangt eine eindeutige Zuordnung:
  liegt ein Raum in mehreren Zonen, gehört er im Vorschlag in **keine** und wandert in den
  Dialog mit der Auswahlliste seiner Zonen.

**Gemessen:** `IFCZONE` kommt in FZK-Haus, FZK-mit-SB, Institute und DigitalHub **null-mal** vor.
Der Zonenpfad über `IfcZone` ist also der sauberste, aber in der Praxis der seltenste — die
Zonierung steht und fällt mit den Vorschlagsregeln.

### 1.6 Geschoss und Gebäude

`IfcBuildingStorey.Elevation` ist die einzige Größe, die der Leser aus der Höhenkette nimmt, und
nur **relativ** (Sortierung, Vorzeichen) — die drei optionalen Bezüge `IfcSite.RefElevation`,
`IfcBuilding.ElevationOfRefHeight`, `IfcBuildingStorey.Elevation` sind sonst nicht in Deckung zu
bringen (Befund N, 4.4 Nr. 2).

**Gemessen:**

| Datei | Geschosse (Name, `Elevation`) |
|---|---|
| FZK-Haus | `'Erdgeschoss'` 0. / `'Dachgeschoss'` 2.7 |
| DigitalHub | `'B01_OKRD'` −3.37 / `'E00_OKRD'` −0.15 / `'E01_OKRD'` 3.75 |

Der DigitalHub zeigt die Falle unmittelbar: Das **Erdgeschoss** liegt bei −0,15 m. Eine Regel
„`Elevation < 0` heißt Untergeschoss" verwirft hier das Erdgeschoss. Die belastbare Regel ist
**relativ**: Untergeschoss ist jedes Geschoss **unter dem niedrigsten Geschoss, dessen Räume
Grenzen mit `InternalOrExternalBoundary = EXTERNAL` tragen** — ersatzweise: unter dem Geschoss
mit der kleinsten `Elevation` ≥ −0,5 m. Beides ist eine Heuristik; sie erzeugt einen
**Vorschlag**, den der Dialog zeigt.

`IfcBuilding` kann mehrfach je Datei vorkommen; in allen vier gemessenen Dateien steht genau
eines. Die Regel aus Befund N (4.4 Nr. 1) bleibt: **ein EPOS-Gebäude je `IfcBuilding`, eines je
Lauf, Auswahl in einer Klappliste.** Im Mehrzonenmodell kommt hinzu, dass die Zonen an dieses
eine Gebäude hängen; Räume ohne Gebäudezuordnung (über `IfcRelAggregates` bzw.
`IfcRelContainedInSpatialStructure` nicht erreichbar) kommen in einen Sammelposten mit Warnung
und werden **nicht** stillschweigend dem einzigen Gebäude zugeschlagen.

### 1.7 Die Vorschlagsregeln für thermische Zonen

Der Leser bildet **einen Vorschlag** und legt die Regel offen, nach der er ihn gebildet hat. Die
Regeln stehen in fester Rangfolge; die erste, die für die Datei trägt, gewinnt, und der Anwender
kann im Dialog auf jede andere umschalten:

| Rang | Regel | Voraussetzung | Ergebnisname | Bewertung an den Messdateien |
|---|---|---|---|---|
| Z1 | **nach `IfcZone`** | mindestens eine Zone, Räume eindeutig zugeordnet | `IfcZone.LongName` bzw. `.Name` | trägt in **keiner** der vier Dateien |
| Z2 | **nach Klassifikation** | alle Räume tragen dieselbe Klassifikationsquelle | `Identification` + `Name` | FZK-Haus: eine Klasse „000 Allgemeines" für alle sieben Räume → eine Zone |
| Z3 | **nach Nutzung** | `LongName`/`ObjectType` trifft ein Nutzungsmuster | Nutzungsgruppe | FZK-Haus: Schlafen/Bad/Büro/Wohnen/Flur/Küche/Galerie → 4–5 Gruppen |
| Z4 | **nach Geschoss** | Geschosse vorhanden | `IfcBuildingStorey.Name` | FZK-Haus 2, DigitalHub 3 Zonen — der robusteste Vorschlag |
| Z5 | **eine Zone je Gebäude** | immer | Gebäudename | der **Einzonen-Rückfall**: liefert genau das Modell aus Befund N |

**Vorbelegung: Z4 (Geschoss), sofern mehr als ein Geschoss Räume trägt, sonst Z5.** Begründung:
Z4 ist die einzige Regel, die in allen vier Messdateien ein Ergebnis liefert, sie ist dem
Anwender sofort verständlich, und sie trifft die Zonengliederung, die der Rechenkern braucht
(unterschiedliche Randbedingungen an Boden und Dach) besser als eine Nutzungsgliederung
innerhalb eines Geschosses.

**Beheizt oder unbeheizt** — dieselbe Reihenfolge, jede Stufe mit Beleg im Dialog:

| Rang | Merkmal | Aussage | Belegtext im Dialog |
|---|---|---|---|
| B1 | `IfcSpace.PredefinedType = EXTERNAL` | nicht beheizt, gehört nicht ins Modell | „IfcSpace.PredefinedType = EXTERNAL" |
| B2 | `Pset_SpaceCommon.IsExternal = TRUE` | wie B1 | Pset und Eigenschaft |
| B3 | `Pset_SpaceThermalRequirements` vorhanden mit `SpaceTemperatureWinterMin` > 12 °C | beheizt | Pset und Wert |
| B4 | Nutzungsmuster im Namen: `Keller\|Garage\|Carport\|Dachboden\|Speicher\|Abstellraum\|Technik\|Schacht\|Aufzug\|Treppenhaus` bzw. `Basement\|Garage\|Attic\|Shaft\|Plant\|Storage` | nicht beheizt | der getroffene Name |
| B5 | Raum liegt in einem Untergeschoss nach § 1.6 **und** trägt keine Grenze `EXTERNAL` | nicht beheizt | Geschoss und Grenzbilanz |
| B6 | sonst | beheizt | „Vorgabe" |

`Pset_SpaceThermalRequirements` ist in IFC 4.3 entfallen (Konzept 7.2) — B3 greift nur für
IFC2x3 und IFC4 und ist in den Messdateien nie belegt; es bleibt trotzdem, weil es die einzige
Stelle ist, an der die Datei die Beheizung ausdrücklich behauptet.

**Mindestgröße.** Eine Zone unter **2 m² Grundfläche oder unter 2 % der Gebäudegrundfläche**
(der größere der beiden Werte entscheidet) wird nicht eigenständig geführt, sondern dem
**Nachbarn mit der größten gemeinsamen Grenzfläche** zugeschlagen — die Nachbarschaft liefert
§ 2.2. Gibt es keinen solchen Nachbarn (freistehender Schacht ohne 2a-Grenze), bleibt die Zone
stehen und der Dialog warnt. Der Grund ist rechnerisch: Das 7R2C-Netz je Zone (Konzept 4.2)
führt je Zone zwei Kapazitäten und einen Luftknoten; eine 1,5-m²-Abstellkammer als eigene Zone
kostet Rechenzeit und bringt keine Aussage, verzerrt aber die Kopplung.

### 1.8 Was der Anwender im Zonendialog ändern kann

Der Dialog ist ein Zwei-Flächen-Dialog nach dem Muster des Konfliktdialogs des Katalogimports
(`EPOS.UI/Dialoge/Import/ImportKonflikteDialog.razor`); die Steuerwerte sind **Werte, nie
Anzeigetexte** — die Regel steht wörtlich in `EPOS.Kern/Allgemein/Katalog/ImportKonfliktModell.cs:41-46`
(„Kein Anzeigetext darf Steuerwert sein"), belegt durch `AktionText` in `:47-58`, das nur noch
beschriftet.

| Bedienung | Wirkung | Was dabei nachgeführt wird |
|---|---|---|
| Regel wählen (Z1…Z5) | der ganze Vorschlag wird neu gebildet | alle Handeingriffe werden verworfen, nach Rückfrage |
| Zone umbenennen | Name der Zone | — |
| Räume zusammenlegen | markierte Räume in eine Zone | Innengrenzen zwischen ihnen entfallen (§ 2.5) |
| Zone trennen | ein Raum verlässt seine Zone | die vormals internen Flächen werden zu Trennflächen zwischen zwei Zonen |
| Haken „beheizt" | Raum zählt zur beheizten Fläche | die angrenzenden Flächen wechseln ihre Randbedingung (§ 2.4) |
| „alles in eine Zone" | der Einzonen-Rückfall | die Rechnung entspricht genau Befund N |

**Die Anzeige führt drei Spalten, die nicht verhandelbar sind:** Regel (woher der Vorschlag
kommt), Beleg (Entität und Pset/Klassifikation) und Herkunft (`IFC` / `Vorgabe` / `Manuell`) —
dieselbe Dreiheit wie im Einzonendialog (Befund N, 4.5) und aus demselben Grund: Eine Zahl, die
der Anwender später in einem Bericht sieht, muss rückverfolgbar sein.

Nach jeder Änderung rechnet der Dialog die Bilanz neu und zeigt sie als Kopfzeile: Zahl der
Zonen, beheizte Fläche, beheiztes Volumen, Summe Außenflächen, Summe Trennflächen. Eine
Zonenbildung, die die beheizte Fläche gegenüber der Summe der Raumflächen um mehr als 5 %
verändert, ist ein Hinweis auf doppelt gezählte Räume und wird benannt.

---

## 2. Bauteile und Flächen je Zone

### 2.1 Was eine Raumgrenze trägt

`IfcRelSpaceBoundary` ([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcproductextension/lexical/ifcrelspaceboundary.htm)):

| Attribut | Typ | Pflicht |
|---|---|---|
| `RelatingSpace` | `IfcSpaceBoundarySelect` | ja |
| `RelatedBuildingElement` | `IfcElement` | **ja ab IFC4** |
| `ConnectionGeometry` | `IfcConnectionGeometry` | optional |
| `PhysicalOrVirtualBoundary` | `IfcPhysicalOrVirtualEnum` | ja |
| `InternalOrExternalBoundary` | `IfcInternalOrExternalEnum` | ja |

Die Norm trennt die beiden Ebenen deutlich: „1st level space boundary: defined as boundaries of
the space, not taking into account any change in building element or spaces on the other side";
„2nd level space boundary: defined as boundary taking any change in building element or spaces
on the other side into account"; und für die thermische Sicht: „In a thermal view, the
decomposition of the space boundary depends on the material of the providing building element
and the adjacent spaces behind. This is a 2nd level space boundary."

`IfcRelSpaceBoundary1stLevel` ergänzt `ParentBoundary` („Reference to the host, or parent, space
boundary within which this inner boundary is defined") und invers `InnerBoundaries` („Inner
boundaries are defined by the space boundaries of openings, doors and windows")
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcproductextension/lexical/ifcrelspaceboundary1stlevel.htm)).
**Der entscheidende Satz für die Flächenbilanz steht dort wörtlich:** „The space boundary of the
parent is not cut by the inner boundary — both overlap."

`IfcRelSpaceBoundary2ndLevel` ergänzt `CorrespondingBoundary : IfcRelSpaceBoundary2ndLevel`
(optional, `[0:1]`) mit dem inversen `Corresponds : SET [0:1] OF IfcRelSpaceBoundary2ndLevel`;
die Typunterscheidung steht in der Definition: „Type 2a that occurs when there is a space on the
opposite side of the building element providing the space boundary. Type 2b occurs if there is a
building element on the opposite side"
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcproductextension/lexical/ifcrelspaceboundary2ndlevel.htm)).

### 2.2 Die Nachbarzone — gemessen

| Datei | Grenzen gesamt | Basisklasse | 1stLevel | 2ndLevel | davon 2a | davon 2b | 2a mit `CorrespondingBoundary` |
|---|---|---|---|---|---|---|---|
| `AC20-FZK-Haus.ifc` | 81 | **81** | 0 | 0 | (81 als Text „2a") | 0 | **nicht möglich** |
| `AC20-FZK-Haus_with_SB_IBPSA_P1.ifc` | 337 | 0 | 131 | 206 | 124 | 82 | **124 von 124** |
| `AC20-Institute-Var-2_with_SB.ifc` | 3 344 | 0 | 1 322 | 2 022 | 1 472 | 550 | **1 472 von 1 472** |
| `FM_ARC_DigitalHub_with_SB_v1.ifc` | 4 142 | 0 | 1 560 | 2 582 | 1 724 | 858 | **1 724 von 1 724** |

**Drei Schlüsse:**

1. **Wo echte 2nd-Level-Entitäten stehen, ist `CorrespondingBoundary` lückenlos gefüllt.** Die
   Nachbarzone ist dann ein Zeigerzugriff: `b.CorrespondingBoundary.RelatingSpace` → Raum →
   Zone. Der in Befund C zitierte Erfahrungssatz der RWTH-Studie („but missed information on the
   corresponding boundaries in adjacent thermal zones") gilt für die **Rohexporte** der
   Autorensysteme, nicht für die angereicherten Dateien — und genau deshalb ist die
   Rekonstruktion (Punkt 3) Pflichtteil, nicht Kür.
2. **Die Archicad-Basisklasse kann es nicht.** `IFCRELSPACEBOUNDARY` hat neun Attribute
   (gemessenes Beispiel:
   `IFCRELSPACEBOUNDARY('0F8DHwVIWaA92A8pankadM',#12,'2ndLevel','2a',#20909,#15042,#76510,.PHYSICAL.,.INTERNAL.)`);
   ein zehntes und elftes Attribut — `ParentBoundary`, `CorrespondingBoundary` — gibt es dort
   nicht. Die Erkennung „2nd Level" über `Name`/`Description` (Befund N, 4.4 Nr. 4) bleibt
   richtig, **liefert aber nur das Kennzeichen, nicht die Verknüpfung.**
3. **Das Kennzeichen „2a" ist in der Archicad-Datei wertlos.** Alle 81 Grenzen tragen
   `Name='2ndLevel'`, `Description='2a'` — auch die 36 physischen und 5 virtuellen **Außen**grenzen
   (`.EXTERNAL.`), die nach Definition gar kein Gegenstück haben können. Der Leser darf den Typ
   also nicht aus dem Text übernehmen, sondern muss ihn aus `InternalOrExternalBoundary` und dem
   gefundenen Gegenstück **ableiten**.

**Rekonstruktionsregel, wenn `CorrespondingBoundary` fehlt** (Archicad-Fall und IFC2x3):

1. Kandidatenmenge: alle Grenzen mit demselben `RelatedBuildingElement`, anderem
   `RelatingSpace`, `PhysicalOrVirtualBoundary = PHYSICAL`.
2. Eindeutig genau dann, wenn genau ein Kandidat bleibt. **Gemessen im FZK-Haus:** von den
   Bauteilen mit Grenzen haben 24 genau eine Grenze (Außenbauteil), 11 genau zwei (eindeutiges
   Paar), 2 drei, 2 vier, 1 sechs und **1 Bauteil 15 Grenzen** — bei diesen sechs Bauteilen ist
   die Zuordnung über das Bauteil allein nicht entscheidbar.
3. Bleibt mehr als ein Kandidat, entscheidet die Geometrie: gleicher Flächeninhalt innerhalb
   1 % **und** Abstand der Flächenschwerpunkte kleiner als die Bauteildicke. Das ist mit den
   Polygonen aus § 2.3 rechenbar, sobald beide Flächen in dasselbe Bezugssystem gebracht sind —
   und genau dieser Schritt braucht die Placement-Kette, nicht aber einen Geometriekernel.
4. Bleibt es mehrdeutig, ist die Fläche **keine Trennfläche**, sondern grenzt gegen „unbekannt";
   der Dialog zeigt sie unter „Flächen ohne Gegenstück" (§ 6.3).

### 2.3 Flächeninhalt ohne Geometriekernel — gemessen, nicht geschätzt

`ConnectionGeometry` ist ein `IfcConnectionSurfaceGeometry` mit den Attributen
`SurfaceOnRelatingElement` (Pflicht) und `SurfaceOnRelatedElement` (optional), beide „given in
the LCS of the relating element" bzw. „of the related element"
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcgeometricconstraintresource/lexical/ifcconnectionsurfacegeometry.htm)).
Die Fläche selbst ist in der Praxis ein `IfcCurveBoundedPlane`: „A parametric planar surface with
curved boundaries defined by one or more boundary curves", Attribute `BasisSurface` (`IfcPlane`),
`OuterBoundary` (`IfcCurve`), `InnerBoundaries` (`SET OF IfcCurve`, optional, „shall not intersect
each other or the outer boundary"); die Randkurven „shall be defined using the u, and v values
provided by parameterization of the BasisSurface as their x, and y coordinate values"
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcgeometryresource/lexical/ifccurveboundedplane.htm)).

**Messung (Perl, Trapezformel über die Randpunkte, 15.09.2026):**

| Datei | Grenzen mit `ConnectionGeometry` | davon `IfcConnectionSurfaceGeometry` | davon `IfcCurveBoundedPlane` | **Fläche berechenbar** | Σ Fläche |
|---|---|---|---|---|---|
| FZK-Haus | 81 | 81 | 81 | **81 (100 %)** | 778,2 m² |
| FZK-mit-SB | 418 | 418 | 418 | **206 von 206 (2. Ebene)** | 1 358,1 m² |
| Institute-mit-SB | 4 344 | 4 344 | 4 344 | — | — |
| DigitalHub | 5 266 | 5 266 | 5 266 | **2 582 von 2 582 (2. Ebene)** | 22 862,1 m² |

`IfcSurfaceOfLinearExtrusion` — die gekrümmte Grenzfläche, die ohne Kernel **nicht** zu
integrieren wäre — kommt in FZK-Haus, FZK-mit-SB und Institute **null-mal** vor, im DigitalHub
**zweimal** von 5 266. `IfcFaceSurface` (der zweite Zweig von `IfcSurfaceOrFaceSurface`) kommt
nirgends vor. **Damit ist die Kernaussage belegt: Der Flächeninhalt einer Raumgrenze ist ohne
Geometriekernel gewinnbar; die Ausnahme liegt im Promillebereich und ist benannt abzulehnen**
(Meldung, Fläche bleibt leer, Anwender trägt ein).

Drei Umsetzungsfallen, alle gemessen:

- **Die Randkurve ist nicht immer eine `IfcPolyline`.** Das FZK-Haus wickelt sie in eine
  `IfcCompositeCurve` → `IfcCompositeCurveSegment` → `IfcPolyline`
  (`#76508= IFCCURVEBOUNDEDPLANE(#76490,#76505,())`, `#76505= IFCCOMPOSITECURVE((#76503),.F.)`,
  `#76501= IFCPOLYLINE(...)`); der DigitalHub schreibt die `IfcPolyline` unmittelbar
  (`#512=IFCPOLYLINE((#10,#506,#508,#510,#10))`). Der Leser muss **beide** Wege können.
  `IfcIndexedPolyCurve` kommt in keiner der vier Dateien vor, ist aber ab IFC4 zulässig und
  gehört in den Leser.
- **Die Punkte sind mal 2D, mal 3D.** Die Spezifikation verlangt die Parameterdarstellung
  (2D, u/v); der DigitalHub hält sich daran (`#10=IFCCARTESIANPOINT((0.,0.))`), das FZK-Haus
  nicht (`#76491= IFCCARTESIANPOINT((0.,2.5,0.))`, drei Koordinaten). Die Trapezformel über die
  ersten beiden Koordinaten trägt in beiden Fällen, **solange die dritte konstant ist** — was
  bei einer ebenen Berandung so ist und vom Leser geprüft werden sollte (Streuung der dritten
  Koordinate > 1 mm → Meldung).
- **Der geschlossene Ring.** Der DigitalHub wiederholt den ersten Punkt am Ende
  (`(#10,…,#10)`), das FZK-Haus nicht. Die Trapezformel über den zyklischen Index verträgt
  beides; eine Implementierung, die den letzten Punkt roh anhängt, bekommt eine Dreiecksfläche
  zu viel.

### 2.4 Randbedingung, Orientierung, Neigung

**Randbedingung** kommt unmittelbar aus `InternalOrExternalBoundary`. Die Enumeration trägt mehr
als die zwei Werte, die man erwartet — **gemessen im DigitalHub**: `.INTERNAL.` 1 538,
`.EXTERNAL.` 967, **`.EXTERNAL_EARTH.` 76**, `.NOTDEFINED.` 2. Im FZK-mit-SB entfallen
230,2 m² von 1 358,1 m² auf `EXTERNAL_EARTH`. Daraus die Abbildung:

| `InternalOrExternalBoundary` | EPOS-Randbedingung | Anmerkung |
|---|---|---|
| `EXTERNAL` | Außenluft | mit Orientierung und Neigung |
| `EXTERNAL_EARTH` | **Erdreich** | genau der Fall, den `Grundflaeche_Randbedingung = ERDREICH` (Konzept 6.1) beschreibt; hier je Bauteil statt je Gebäude |
| `INTERNAL` mit Gegenstück in derselben Zone | entfällt (innere Masse) | zählt in die Innenbauteilfläche A_IW |
| `INTERNAL` mit Gegenstück in anderer beheizter Zone | Trennfläche Zone↔Zone | die Kopplung des Mehrzonennetzes |
| `INTERNAL` mit Gegenstück in unbeheizter Zone | unbeheizter Nachbar | Reduktionsfaktor wie „Keller" (Konzept 4.4) |
| `INTERNAL` ohne Gegenstück | unbekannt | Dialog, Vorgabe „unbeheizter Nachbar" |
| `EXTERNAL_WATER`, `EXTERNAL_FIRE`, `NOTDEFINED` | benannt abgelehnt | Meldung, Anwender entscheidet |

**Orientierung und Neigung** gewinnt der Leser aus der Normalen der Grenzfläche, nicht mehr aus
der Wandachse: `IfcCurveBoundedPlane.BasisSurface` ist eine `IfcPlane` mit
`IfcAxis2Placement3D`; deren `Axis` ist die Flächennormale im lokalen System. Über die
Placement-Kette `IfcProduct.ObjectPlacement` → `IfcLocalPlacement.RelativePlacement` →
`PlacementRelTo` aufwärts bis zum Weltsystem wird sie gedreht, zuletzt um `TrueNorth` aus
`IfcGeometricRepresentationContext` — **außer** wenn der Kontext eine `IfcMapConversion` über
`HasCoordinateOperation` trägt, dann ist `TrueNorth` laut Spezifikation nur informativ (Konzept
7.2, Befund N 4.3). Daraus:

- **Azimut** = Winkel der in die xy-Ebene projizierten Normalen, 0° = Nord, im Uhrzeigersinn.
- **Neigung** = Winkel zwischen Normale und z-Achse: 90° senkrechte Wand, 0° waagerechte Decke,
  180° Boden. Das Vorzeichen der Normalen entscheidet über oben/unten und ist die Probe darauf,
  ob die Kette richtig gedreht wurde: Eine Bodenplatte, deren Normale nach oben zeigt, ist ein
  Vorzeichenfehler, kein Dach.
- Die Gegenprobe ist billig und gehört in den Leser: **Σ Flächen nach unten ≈ Σ Flächen nach
  oben** je Zone bei geschlossener Hülle.

Das FZK-Haus trägt seine Grenzflächen mit dreidimensionalen Randpunkten (§ 2.3) — dort ist die
Normale auch direkt aus drei nicht kollinearen Randpunkten zu gewinnen; die `IfcPlane` bleibt
aber der Normweg, weil sie in beiden Schreibweisen vorhanden ist.

### 2.5 Bauteilart aus `RelatedBuildingElement`

Die Zuordnung ist ein Typschalter auf dem Bauteil, ergänzt um `PredefinedType` bei Platten:

| IFC-Typ | EPOS-Bauteilart | Anmerkung |
|---|---|---|
| `IfcWall`, `IfcWallStandardCase` | Außenwand oder Innenwand | entscheidet die Randbedingung, **nicht** `Pset_WallCommon.IsExternal` |
| `IfcSlab` `PredefinedType = ROOF`, `IfcRoof` | Dach | |
| `IfcSlab` `PredefinedType = BASESLAB` | Bodenplatte | bei `EXTERNAL_EARTH` zusätzlich bestätigt |
| `IfcSlab` `PredefinedType = FLOOR` | Decke / Boden zwischen Zonen | Neigung entscheidet über oben/unten |
| `IfcWindow` | Fenster | |
| `IfcDoor` | Tür | außen oder innen nach Randbedingung |
| `IfcCurtainWall`, `IfcPlate` | Sonstiges (Pfosten-Riegel) | Rahmen/Glas über `IfcMaterialConstituentSet` (§ 3.5) |
| `IfcColumn`, `IfcBeam`, `IfcMember` | Sonstiges | im DigitalHub 150 + 42 Grenzen mit zusammen 198,6 m² — **nicht** zur Außenwand zählen |
| `IfcVirtualElement` bzw. `PhysicalOrVirtualBoundary = VIRTUAL` | **keine Fläche** | offene Verbindung; im Einzonenmodell bedeutungslos, im Mehrzonenmodell ein Grund, zwei Räume **zusammenzulegen** |

**Gemessen, Grenzflächen je Bauteiltyp:**

| | FZK-Haus (n / m²) | DigitalHub (n / m²) |
|---|---|---|
| `IfcSlab` | 23 / 395,7 | 557 / 7 819,4 |
| `IfcWall(StandardCase)` | 28 / 271,2 | 1 546 / 10 680,7 |
| `IfcRoof` | — | 55 / 3 103,1 |
| `IfcWindow` | 11 / 23,2 | 94 / 716,8 |
| `IfcDoor` | 8 / 17,5 | 136 / 307,5 |
| `IfcVirtualElement` / ohne auflösbares Bauteil | 6 / 36,6 und 5 / 34,1 | — |
| `IfcColumn` / `IfcBeam` / `IfcCurtainWall` | — | 150 / 91,0, 42 / 107,6, 2 / 36,0 |

Der Posten „5 Grenzen ohne auflösbares `RelatedBuildingElement`" im FZK-Haus ist bemerkenswert,
weil das Attribut ab IFC4 Pflicht ist — ein Beleg dafür, dass der Leser **jede** Pflichtangabe
gegen `null` prüfen muss, statt sich auf das Schema zu verlassen.

### 2.6 Fenster, Türen und die Doppelzählung

Die Norm ist eindeutig und die Folge unbequem: „The space boundary of the parent is not cut by
the inner boundary — both overlap." Die Wandgrenze enthält die Fensteröffnung; die Fenstergrenze
liegt zusätzlich darauf. Wer beide addiert, zählt die Öffnung zweimal.

**Gemessen** — wie oft die Grenzebene ihre Öffnungen stattdessen **geometrisch** ausschneidet
(`IfcCurveBoundedPlane` mit gefüllten `InnerBoundaries`), bezogen auf die Grenzen der 2. Ebene:
FZK-Haus **0 von 81**, FZK-mit-SB **1 von 206**, DigitalHub **116 von 2 582**. Über alle
`IfcCurveBoundedPlane` des DigitalHub (auch die der 1. Ebene) sind es 4 326 von 5 266 — die
1st-Level-Flächen tragen die Löcher, die 2nd-Level-Flächen fast nie.

**Daraus die Regel, die je Fläche entscheidet, nicht je Datei:**

```
A_netto(Wandgrenze) = A_polygon(OuterBoundary)
                    − Σ A_polygon(InnerBoundaries der Ebene)          // schon ausgeschnitten: 0
                    − Σ A(Kindgrenzen über InnerBoundaries/ParentBoundary)
```

Die zweite Zeile ist immer 0, wenn die dritte greift, und umgekehrt — beide Wege ziehen dieselbe
Öffnungsfläche genau einmal ab, weil die Ebene mit Loch keine Kindgrenze auf demselben Loch
trägt. Der Leser rechnet beide und **warnt, wenn beide gleichzeitig größer null sind**; das wäre
der Fall, in dem doppelt abgezogen würde.

Fehlt die Eltern-Kind-Beziehung ganz (Archicad-Basisklasse: kein `ParentBoundary`), bleibt der
Weg über die Geometrie: Eine Fenstergrenze liegt in der Ebene ihrer Wandgrenze und ihr Polygon
liegt innerhalb von deren Polygon. Der billige Ersatz — von der Wandfläche **je Zone** die Summe
der Fenster- und Türgrenzen **derselben Zone** abzuziehen — trägt, solange Fenster nur in
Außenwänden sitzen, und ist für das FZK-Haus (11 Fenster, 8 Türen) nachrechenbar.

**Bruttomaß.** Die Bemaßungsregel des Konzepts (N1.9, als Arbeitsannahme geführt) verlangt
Außenmaße. Raumgrenzen sind **Raumseitenflächen** — sie liegen an der Innenoberfläche des
Bauteils und sind damit systematisch kleiner als das Bruttomaß. Der Unterschied wächst mit der
Zahl der Innenecken und ist bei einem Reihenhaus mit vielen kleinen Räumen erheblich. Zwei
Wege stehen offen:

1. **Raumseitenmaß durchhalten** und die Wärmedurchgangsrechnung darauf beziehen (das tun
   Zonenmodelle üblicherweise, und die Norm baut die Grenzen genau dafür).
2. Auf Bruttomaß umrechnen — das braucht Bauteildicken und die Gehrung an jeder Ecke, also
   Geometrie.

**Empfehlung: Weg 1, und im Dialog benennen.** Begründung: Das Mehrzonenmodell rechnet mit
Flächen, die es aus derselben Quelle bekommt wie die Topologie; eine halbe Umrechnung erzeugt
eine Hülle, die weder brutto noch netto ist. Die Abweichung gegen die Einzonenrechnung ist damit
erklärbar und im Abnahmevergleich zu messen — sie gehört in die Abnahme (§ 6.4).

### 2.7 Rückfall ohne Raumgrenzen

Liefert die Datei keine Raumgrenzen (Allplan 2023 exportiert keine, Konzept 7.3; Duplex
Apartment nur 1. Ebene, Konzept 7.8), fällt die Zonentopologie weg. Dann gilt:

1. Bauteile über `IfcRelContainedInSpatialStructure` dem **Geschoss** zuordnen (das ist die
   einzige räumliche Zuordnung, die jede Datei trägt).
2. Zonen nach Regel **Z4 (je Geschoss)** oder **Z5 (ein Gebäude, eine Zone)** bilden — Z5 ist
   genau der Einzonenweg aus Befund N und damit immer verfügbar.
3. Flächen je Zone aus den **Quantity-Sets** der Bauteile (Befund N, 4.3), Außen/Innen aus
   `Pset_*Common.IsExternal` mit dem dort beschriebenen Rückfall.
4. **Nachbarschaft zwischen Zonen gibt es in diesem Fall nicht.** Zwei Geschosszonen ohne
   Grenzen sind thermisch entkoppelt; das ist falsch. Der Leser bietet daher bei fehlenden
   Grenzen **Z5 als Vorgabe** an und Z4 nur, wenn der Anwender die Trenndecke selbst einträgt.
   Eine stillschweigend entkoppelte Mehrzonenrechnung wäre schlechter als die Einzonenrechnung.
5. Der Dialog sagt das mit einem Satz und schaltet die Zonenwahl auf Z5; die Meldung dazu ist
   neu (`IMP_IFC_PROT_KEINE_GRENZEN`).

---

## 3. Materialdaten

### 3.1 Schichtaufbau: `IfcMaterialLayerSetUsage` → `IfcMaterialLayerSet` → `IfcMaterialLayer`

Die Kette hängt am Bauteil über `IfcRelAssociatesMaterial` (§ 4). `IfcMaterialLayerSet` trägt
`MaterialLayers : LIST [1:?] OF IfcMaterialLayer`, `LayerSetName`, `Description` und das
abgeleitete `TotalThickness`; die Schichten „are stacked with no gap", und „Gaps within a
material layer set are expressed as layers by themselves" — eine Luftschicht ist eine Schicht
mit gesetztem `IsVentilated`
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcmaterialresource/lexical/ifcmateriallayerset.htm)).

`IfcMaterialLayer` hat `Material`, `LayerThickness` (`IfcNonNegativeLengthMeasure`),
`IsVentilated` (`IfcLogical`, also **dreiwertig**: `.T.`/`.F.`/`.U.`), `Name`, `Description`,
`Category`, `Priority`. **Gemessen:**

| Datei | Schichtsätze | Schichten | Beispiel |
|---|---|---|---|
| FZK-Haus | 4 | 4 (je einer) | `IFCMATERIALLAYER(#15046,0.24,.U.,$,$,$,$)` — Dicke 0,24 m, `IsVentilated` unbekannt, ohne Name |
| FZK-mit-SB | 4 | 4 | dito |
| Institute-mit-SB | 4 | 4 | dito |
| DigitalHub | 20 | 38 | `IFCMATERIALLAYER(#25056,0.25,$,'Ortbeton - bewehrt',$,'Generisch',$)` — mit `Name` und `Category` |

Zwei Befunde: Die Archicad-Dateien führen **einschichtige** Sätze („Leichtbeton … 0.24",
„Stahlbeton 65690 0.2") — der Schichtaufbau ist dort also gar nicht modelliert, sondern nur die
Gesamtdicke mit einem Sammelnamen. Der DigitalHub führt echte Sätze
(`'Geschossdecke:STB 250'`, `'Basiswand:STB 300'`), aber mit im Mittel **1,9 Schichten** — auch
das ist die Konstruktion des Architekten, nicht die des Bauphysikers.

### 3.2 Stoffwerte: `Pset_MaterialThermal` und `Pset_MaterialCommon`

`Pset_MaterialThermal` trägt nach Spezifikation genau vier Eigenschaften:
`SpecificHeatCapacity` (`IfcSpecificHeatCapacityMeasure`), `BoilingPoint` und `FreezingPoint`
(`IfcThermodynamicTemperatureMeasure`), `ThermalConductivity` (`IfcThermalConductivityMeasure`)
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcmaterialresource/pset/pset_materialthermal.htm)).
`Pset_MaterialCommon` trägt `MolecularWeight`, `Porosity` (`IfcNormalisedRatioMeasure`),
`MassDensity` (`IfcMassDensityMeasure`)
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcmaterialresource/pset/pset_materialcommon.htm)).

**`ThermalConductivityTemperatureDerivative` gibt es dort nicht.** Die Eigenschaft stammt aus der
Ontologie „Digital Construction — Materials" (digitalconstruction.github.io/Materials), die
IFC-Begriffe nachbildet und erweitert; sie ist **kein** buildingSMART-Pset-Inhalt und in keiner
der vier Messdateien vorhanden. Der Leser liest sie nicht. (Der Merkposten gehört korrigiert,
wo er im Auftragstext steht.)

Für das RC-Ersatzmodell nach VDI 6007-1 werden je Schicht d, λ, ρ, c gebraucht (Konzept 4.3,
Bauteilweg G3). IFC liefert also **genau die richtigen vier Größen** — wenn sie dastehen.

### 3.3 Reihenfolge und Richtung: welche Schicht ist innen?

Die Frage entscheidet über die thermische Masse des Modells, denn nur die raumseitigen Schichten
zählen in C₁ (Konzept 4.3). `IfcMaterialLayerSetUsage` beantwortet sie mit fünf Attributen —
`ForLayerSet`, `LayerSetDirection` (`IfcLayerSetDirectionEnum`: AXIS1/AXIS2/AXIS3),
`DirectionSense` (`IfcDirectionSenseEnum`: POSITIVE/NEGATIVE), `OffsetFromReferenceLine`
(`IfcLengthMeasure`), `ReferenceExtent` (optional)
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcmaterialresource/lexical/ifcmateriallayersetusage.htm)):

> „‚Positive' means that the consecutive layers … are placed face-by-face in the direction of the
> positive axis as established by LayerSetDirection: for AXIS2 it would be in +y, for AXIS3 it
> would be +z."

und für den Bezug:

> „A positive value means, that the MlsBase is placed on the positive side of the reference line
> or plane … A negative value means that the MlsBase is placed on the negative side."

mit dem ausdrücklichen Hinweis, dass beide unabhängig sind:

> „The positive or negative sign in the offset only affects the MlsBase placement, it does not
> have any effect on the application of DirectionSense … also DirectionSense does not change the
> MlsBase placement."

**Die Leseregel daraus:**

1. Die Liste `MaterialLayers` läuft **von der MlsBase in Richtung `DirectionSense`** entlang der
   Achse `LayerSetDirection` (bei Wänden AXIS2 = lokale y-Richtung, bei Platten AXIS3 = z).
2. Die MlsBase liegt bei `OffsetFromReferenceLine` von der Bezugslinie (der Wandachse bzw. der
   Bezugsebene der Platte).
3. **Welche Seite raumseitig ist, sagt der Schichtsatz nicht** — das sagt erst die Raumgrenze:
   Die Normale der Grenzfläche zeigt in den Raum. Ist ihre Projektion auf die Achse
   `LayerSetDirection` **gleichgerichtet** mit `DirectionSense`, steht die **letzte** Schicht der
   Liste raumseitig, sonst die **erste**.
4. Fehlt das `…Usage` und hängt der Satz unmittelbar am Bauteil (zulässig, häufig bei Platten),
   gilt die Vorgabe der Norm: erste Schicht außen bzw. unten. Der Leser markiert diesen Fall als
   Annahme und zeigt ihn im Dialog.
5. Im **Mehrzonenmodell** ist die Frage zweiseitig: Eine Trennwand hat für jede der beiden Zonen
   eine andere Innenseite; die Schichtfolge ist dieselbe Liste, einmal vorwärts, einmal rückwärts
   gelesen. Genau dafür ist `CorrespondingBoundary` gut — die zweite Grenze liefert die zweite
   Normale, und die Probe ist, dass beide Normalen entgegengesetzt sind.

Punkt 3 ist der Teil, den ein Einzonenleser nicht braucht und den das Mehrzonenmodell verlangt;
er ist ohne Geometriekernel rechenbar, aber er verlangt, dass die Placement-Kette stimmt.

### 3.4 Wie oft die Stoffwerte gefüllt sind — gemessen

| Datei | Schichtsätze | `Pset_MaterialThermal` | λ, ρ, c brauchbar |
|---|---|---|---|
| `AC20-FZK-Haus.ifc` | 4 | 4 × über `IfcMaterialProperties` | **0** — alle vier Sätze tragen `IFCTHERMALCONDUCTIVITYMEASURE(0.)`, `IFCSPECIFICHEATCAPACITYMEASURE(0.)`, `IFCMASSDENSITYMEASURE(0.)` |
| `AC20-FZK-Haus_with_SB…` | 4 | 4 | **0**, dieselben Nullwerte |
| `AC20-Institute-Var-2_with_SB.ifc` | 4 | 4 | **1** — nur `'Aluminium 131198'` mit 160 / 880 / 2 800; `'Stahlbeton'`, `'Kalksandstein …'`, `'Luftschicht'` alle 0 |
| `FM_ARC_DigitalHub_with_SB_v1.ifc` | 20 | **0 Vorkommen** | **0** — die 632 `IfcMaterialConstituent` und 38 Schichten tragen Namen, aber kein einziges thermisches Pset |

**Das ist der wichtigste Einzelbefund dieses Papiers.** Die Literatur sagt es abstrakt („das
Architekturmodell ist in der Regel für eine thermische Betrachtung nicht geeignet", Konzept 7.3);
die Messung sagt es genau: In der lizenzfreien Referenzdatei stehen die Psets da, **sind aber
mit Nullen gefüllt** — das ist schlimmer als ihr Fehlen, weil ein naiver Leser λ = 0 übernimmt
und damit einen unendlichen Wärmewiderstand rechnet.

**Zwei Pflichtregeln folgen daraus:**

1. **Ein Stoffwert ≤ 0 ist kein Wert.** λ, ρ, c werden nur übernommen, wenn sie in einem
   Plausibilitätsband liegen: λ in [0,005; 500] W/(mK), ρ in [5; 8 000] kg/m³, c in
   [100; 5 000] J/(kgK). Alles andere gilt als „nicht geliefert" und läuft in den Rückfall.
2. **Der U-Wert aus `Pset_*Common.ThermalTransmittance` ist die belastbarere Quelle als die
   Schichten.** Gemessen: FZK-Haus 33 Werte zwischen 0,3 und 2,0 W/(m²K) (2 × 0,3, 9 × 0,4,
   1 × 0,5, 13 × 1,4, 5 × 1,5, 3 × 2,0) — plausibel; DigitalHub 359 Werte. Das Institute trägt
   **null** U-Werte. Wo beides fehlt, greift die Vorgabe je Baualtersklasse (Befund N, 4.3).

### 3.5 Fenster: Rahmen und Glas

Für Fenster und Türen ist der Schichtsatz das falsche Werkzeug; die Norm sieht
`IfcMaterialConstituentSet` vor — „a collection of individual material constituents, each
assigning a material to a part of an element", mit dem ausdrücklichen Beispiel „The different
materials of a window construction shall be provided for the window lining and the window
glazing", also je ein `IfcMaterialConstituent` mit `Name = 'Lining'` bzw. `'Glazing'`
([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcmaterialresource/lexical/ifcmaterialconstituentset.htm)).
`IfcMaterialProfileSet` (Profilquerschnitte, etwa Pfosten und Riegel) spielt für die thermische
Rechnung keine Rolle und bleibt ungelesen.

**Gemessen:** Der DigitalHub führt 632 `IfcMaterialConstituent` in Sets zu je zwei Bestandteilen
— aber die Namen lauten `'Mauerwerk - Naturstein'` und `'Ortbeton - bewehrt Verputzt'` mit
`Category = 'Materialien'`, also **Wandschalen, nicht Rahmen und Glas**. Die
Norm-Bestandteilsnamen „Lining"/„Glazing" kommen nicht vor. Das Constituent-Set ist damit in der
Praxis ein zweiter Weg, mehrschalige Bauteile zu beschreiben, und **kein** verlässlicher Weg zu
Rahmen- und Glasanteil.

Die brauchbaren Fensterkennwerte kommen deshalb aus den Psets:

| Größe | Quelle | gemessen |
|---|---|---|
| U_w (ganzes Fenster) | `Pset_WindowCommon.ThermalTransmittance` (`IfcThermalTransmittanceMeasure`) | FZK-Haus und DigitalHub gefüllt |
| g-Wert | `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` — „Ratio of incident solar radiation contributing to solar gains … Also called g-value (g = Te + qi)" ([Spezifikation](http://www.bimant.com/ifc/IFC4_3/RC1/HTML/schema/ifcsharedbldgelements/pset/pset_doorwindowglazingtype.htm)) | DigitalHub: 85 Vorkommen, davon **79 mit Wert 0.**, 2 × 0,41, 4 × 0,62 → **7 % brauchbar**; FZK-Haus: **0 Vorkommen** |
| Rahmenanteil | `Pset_WindowCommon.GlazingAreaFraction` (`IfcPositiveRatioMeasure`, „Rapport de la surface de vitrage à la surface totale de l'ouverture") → F_F = GlazingAreaFraction | in **keiner** der vier Dateien vorhanden |
| U_g (nur Glas) | `Pset_DoorWindowGlazingType.ThermalTransmittanceSummer/Winter` | nicht gemessen; als Ersatz für U_w ungeeignet (ohne Rahmen) |
| Scheibenzahl, Gasfüllung | `GlassLayers` (`IfcCountMeasure`), `FillGas` (`IfcLabel`, „informational only") | nur als Beleg im Dialog |
| Infiltration | `Pset_WindowCommon.Infiltration` (`IfcVolumetricFlowRateMeasure`, m³/h bei 50 Pa) | nicht als Luftwechsel übernehmen — das ist eine Bauteilgröße, keine Zonengröße |

**Regel:** g-Wert und Rahmenanteil bleiben Vorgaben (Konzept 4.4: F_F = 1 − `Rahmenanteil`,
Vorgabe 0,3), solange nicht ein Wert im Band (g in [0,1; 0,9], Rahmenanteil in [0,05; 0,6])
gefunden wird. Ein g-Wert 0 ist wie λ = 0 kein Wert.

### 3.6 Rückfall: Namensabgleich gegen einen EPOS-Baustoffkatalog

Da die Stoffwerte praktisch nie kommen, ist der **Namensabgleich der Regelweg, nicht der
Notweg** — genau so machen es die etablierten Werkzeuge (IDA ICE liest nur Konstruktionsnamen
und lässt den Anwender zuordnen, Befund C § 2).

Der Abgleich läuft gegen `Tab_Baustoff_STAMM` aus Stufe G3 (Konzept 6.3: `Name`, `Lambda`,
`Rho`, `cp`, `Quelle`, `ReadOnly`; gesät aus DIN 4108-4 / DIN EN ISO 10456). **Die Tabelle gibt
es heute nicht** — eine Suche über `*.cs` und `*.sql` findet `Tab_Bauteil`, `Tab_Bauteilschicht`
und `Tab_Baustoff_STAMM` nirgends im Repositorium; der Mehrzonenimport setzt G3 also voraus.

Die Namen, mit denen der Abgleich zu tun bekommt (alle gemessen):

```
Leichtbeton 102890359     Stahlbeton 65690       Solid 397409098     Holz
Kalksandstein 2816491304  Luftschicht            Aluminium 131198    Leer
Ortbeton - bewehrt        Ortbeton - bewehrt Verputzt                Fußbodenaufbau
Mauerwerk - Naturstein    Radial Gradient Fill 1515460218
```

Daraus die Abgleichkette, Stufe für Stufe, jede mit eigenem Herkunftskennzeichen:

| Stufe | Regel | Beispiel |
|---|---|---|
| N1 | **Zahlenschwänze abschneiden.** Archicad und Revit hängen eine interne Kennung an; `\s+\d{5,}$` entfernen | `Leichtbeton 102890359` → `Leichtbeton` |
| N2 | **Normalisieren:** Kleinschreibung, Umlaute auflösen (ä→ae), Bindestriche und Mehrfachleerzeichen zu einem Leerzeichen, führende/abschließende Zusätze wie `Verputzt`, `bewehrt`, `generisch` als eigene Marke abtrennen | `Ortbeton - bewehrt Verputzt` → `ortbeton` + Marken |
| N3 | **Genauer Treffer** gegen `Tab_Baustoff_STAMM.Name` (normalisiert) | `stahlbeton` → Stahlbeton |
| N4 | **Synonymtabelle**, zweisprachig: `Ortbeton`→Stahlbeton, `Leichtbeton`→Leichtbeton, `Kalksandstein`→KS, `reinforced concrete`→Stahlbeton, `brick`→Vollziegel, `insulation`/`Dämmung`→Mineralwolle, `air (gap\|layer)`/`Luftschicht`→ruhende Luftschicht | `Mauerwerk - Naturstein` → Naturstein |
| N5 | **Teilwort mit eindeutigem Treffer** — nur, wenn genau ein Katalogeintrag als Wortanfang enthalten ist | `Fußbodenaufbau` → kein eindeutiger Treffer |
| N6 | **Sonderfälle ohne Stoff:** `Luftschicht`, `Leer`, `Solid …`, `Radial Gradient Fill …` (das ist eine Schraffur, kein Stoff) | Luftschicht → Ersatzwiderstand nach DIN EN ISO 6946; „Leer"/Schraffur → Schicht verwerfen, Meldung |
| N7 | **Anwenderzuordnung** — der Rest, in einer eigenen Liste des Dialogs, mit Merkfunktion: eine einmal getroffene Zuordnung IFC-Name → Baustoff wird projektweit (später katalogweit) behalten | alles übrige |

**Die Sprache ist kein Nebenschauplatz.** Die gemessenen Namen sind deutsch, weil die Dateien aus
dem deutschen Sprachraum stammen; ein Revit-Export aus einer englischen Vorlage liefert
„Concrete, Cast-in-Place" und „Air". Die Synonymtabelle ist deshalb **zweisprachig** anzulegen
und gehört — wie alle Texte — in die beiden `.resx` **nicht** hinein: Sie ist eine
**Datentabelle** (Steuerwert), kein Anzeigetext; die Regel dazu steht in
`EPOS.Kern/Allgemein/Katalog/ImportKonfliktModell.cs:41-46`.

Für die Anzeige entsteht je Schicht eine Zeile mit `Herkunft` ∈ {`IFC` (Stoffwerte aus der
Datei), `Katalog` (über Namensabgleich, mit der getroffenen Stufe als Beleg), `Manuell`
(Anwender), `Vorgabe`} — dieselbe Aufzählung wie `IfcHerkunft` in Befund N (4.2), um `Katalog`
erweitert.

### 3.7 Was ohne Stoffwerte trotzdem geht

Fehlen die Schichten ganz oder tragen sie keine Stoffe, bleibt das Bauteil **masselos mit
U-Wert** — das ist der Einzonen-Klassenweg (Konzept 4.3), je Bauteil statt je Gebäude:

- U aus `Pset_*Common.ThermalTransmittance`, sonst Vorgabe je Baualtersklasse;
- die thermische Masse der Zone aus `Bauweise` (der Gesamtkapazität je m² Wohnfläche) wie heute,
  mit der Einrastung über `Gebaeudebauweise.BauartAusBauweise`
  (`EPOS.Kern/Allgemein/Gebaeudebauweise.cs:42-48`: < 30 leicht, > 75 sehr schwer, sonst
  schwer, bezogen auf Wh/(m²K)) und den Stufenwerten in `:63` (20 / 50 / 100 × Wohnfläche).

**Damit gilt im Mehrzonenmodell dieselbe Zweiteilung wie im Einzonenmodell** — Bauteilweg, wo
Schichten mit Stoffwerten vorliegen, Klassenweg sonst —, nur je Zone entschieden. Eine Zone, die
ihre Masse aus `Bauweise` bezieht, und eine Nachbarzone, die sie aus Schichten rechnet, sind
zulässig; der Dialog zeigt je Zone, welcher Weg gilt.

---

## 4. xBIM-API für diese Zugriffe

Alle Namen sind Schnittstellen aus `Xbim.Ifc4.Interfaces` und werden von `Xbim.Ifc2x3` und
`Xbim.Ifc4x3` mitbedient (je ein Ordner `Interfaces/IFC4`); die Fundstellen sind die Quelltexte
in `github.com/xBimTeam/XbimEssentials`, abgerufen am 15.09.2026. Befund N führt in seinem
Anhang 6 die Namen des Einzonenwegs; hier stehen nur die zusätzlichen.

| Zweck | geprüfte Signatur | Fundstelle |
|---|---|---|
| Grenzen eines Raumes | `IIfcSpace.BoundedBy : IEnumerable<IIfcRelSpaceBoundary>` | `Xbim.Ifc4/ProductExtension/IfcSpace.cs` (Befund N, Anhang 6) |
| 2. Ebene | `public partial interface @IIfcRelSpaceBoundary2ndLevel : IIfcRelSpaceBoundary1stLevel` | `Xbim.Ifc4/ProductExtension/IfcRelSpaceBoundary2ndLevel.cs` |
| Gegenstück | `IIfcRelSpaceBoundary2ndLevel @CorrespondingBoundary { get; set; }` | dito |
| Gegenstück invers | `IEnumerable<IIfcRelSpaceBoundary2ndLevel> @Corresponds { get; }` | dito |
| Eltern-Grenze | `IIfcRelSpaceBoundary1stLevel.ParentBoundary`, invers `InnerBoundaries` | `Xbim.Ifc4/ProductExtension/IfcRelSpaceBoundary1stLevel.cs` |
| Material am Bauteil | `IEnumerable<IIfcRelAssociates> @HasAssociations { get; }` auf `IIfcObjectDefinition`, gefiltert auf `IIfcRelAssociatesMaterial` | `Xbim.Ifc4/Kernel/IfcObjectDefinition.cs` |
| das Material selbst | `IIfcMaterialSelect @RelatingMaterial { get; set; }` | `Xbim.Ifc4/ProductExtension/IfcRelAssociatesMaterial.cs` |
| Schichtnutzung | `IIfcMaterialLayerSet ForLayerSet`, `IfcLayerSetDirectionEnum LayerSetDirection`, `IfcDirectionSenseEnum DirectionSense`, `IfcLengthMeasure OffsetFromReferenceLine`, `IfcPositiveLengthMeasure? ReferenceExtent` | `Xbim.Ifc4/MaterialResource/IfcMaterialLayerSetUsage.cs` |
| Schichten | `IIfcMaterialLayerSet.MaterialLayers`, `IIfcMaterialLayer.Material/.LayerThickness/.IsVentilated/.Category` | `…/IfcMaterialLayerSet.cs`, `…/IfcMaterialLayer.cs` (Befund N, Anhang 6) |
| **Stoffwerte** | `IEnumerable<IIfcMaterialProperties> @HasProperties { get; }` auf **`IIfcMaterialDefinition`** (nicht auf `IIfcMaterial` — das trägt nur `Name`, `Description`, `Category`, `HasRepresentation`, `IsRelatedWith`, `RelatesTo`) | `Xbim.Ifc4/MaterialResource/IfcMaterialDefinition.cs`, `…/IfcMaterial.cs` |
| ein Stoffwertsatz | `public partial interface @IIfcMaterialProperties : IIfcExtendedProperties` mit `IIfcMaterialDefinition Material { get; set; }`; `Name` und `Properties` erbt es von `IIfcExtendedProperties` | `Xbim.Ifc4/MaterialResource/IfcMaterialProperties.cs` |
| Bestandteile (Fenster) | `IIfcMaterialConstituentSet.MaterialConstituents`, `IIfcMaterialConstituent.Name/.Material/.Category` | `Xbim.Ifc4/MaterialResource/` |
| Klassifikation | `IIfcRelAssociatesClassification.RelatingClassification`, `IIfcClassificationReference.Identification/.Name/.ReferencedSource` | `Xbim.Ifc4/ExternalReferenceResource/` |
| Zonen | `IIfcZone`, Räume über `IIfcGroup.IsGroupedBy` → `IIfcRelAssignsToGroup.RelatedObjects` | `Xbim.Ifc4/ProductExtension/IfcZone.cs`, `…/Kernel/IfcGroup.cs` |
| Grenzflächen-Geometrie | `IIfcConnectionSurfaceGeometry.SurfaceOnRelatingElement/.SurfaceOnRelatedElement`, `IIfcCurveBoundedPlane.BasisSurface/.OuterBoundary/.InnerBoundaries`, `IIfcPlane.Position` | `Xbim.Ifc4/GeometricConstraintResource/`, `…/GeometryResource/` |

**Die eine Falle, die man sonst zweimal baut:** Stoffwerte hängen **nicht** über
`IsDefinedBy`/`IIfcPropertySet` am Material — das ist der Weg für Objekte. Am Material hängen
sie über `HasProperties` als `IfcMaterialProperties`, das in IFC4 seine Norm-Zugehörigkeit im
Attribut `Name` trägt. **Gemessen im FZK-Haus:**
`#15055= IFCMATERIALPROPERTIES('Pset_MaterialThermal',$,(#15058,#15059),#15046);` — Name,
Description, Properties, Material. Wer dort nach einem `IfcPropertySet` sucht, findet nie etwas.

**Was ohne Geometriekernel geht** (mit dem Stand dieses Befunds):

| geht | geht nicht |
|---|---|
| Flächeninhalt jeder ebenen Grenzfläche (Trapezformel, § 2.3: 100 % der gemessenen Fälle) | `IfcSurfaceOfLinearExtrusion` und jede gekrümmte Grenzfläche (2 von 5 266 im DigitalHub) |
| Normale, Azimut, Neigung aus `IfcPlane` + Placement-Kette + `TrueNorth` | Prüfung, ob zwei Flächen wirklich deckungsgleich sind (nur näherungsweise über Inhalt und Schwerpunkt) |
| Schwerpunkt einer Grenzfläche (Polygonschwerpunkt) | Verschneidung, Öffnungsabzug ohne `InnerBoundaries` |
| Schichten, Stoffwerte, Psets, Quantities, Klassifikation, Zonen | Rekonstruktion fehlender Raumgrenzen aus Bauteilkörpern (das ist die Aufgabe von IFC2SB/bim2sim und bleibt Stufe G5) |
| Volumen als Σ `NetVolume` der Räume | Volumen aus der Hülle |

**Einheiten** wie in Befund N (4.3): Faktor aus `IIfcProject.UnitsInContext`. **Gemessen** tragen
alle vier Dateien `IFCSIUNIT(*,.LENGTHUNIT.,$,.METRE.)`, `.AREAUNIT. .SQUARE_METRE.`,
`.VOLUMEUNIT. .CUBIC_METRE.` ohne Prefix — der einfachste Fall; die Auswertung bleibt trotzdem
Pflicht, weil ein Millimetermodell die Flächen um 10⁶ verschiebt und das an keiner Zahl auffällt.

**Ein Punkt bleibt offen und gehört früh geprüft:** IFC2x3 kennt `Pset_MaterialThermal` nicht,
sondern `IfcThermalMaterialProperties` und `IfcGeneralMaterialProperties` als eigene Entitäten.
Ob die IFC4-Schnittstellenschicht von xBIM diese für ein 2x3-Modell auf `HasProperties`
abbildet, ist an einer 2x3-Datei zu messen (Duplex Apartment) — nicht aus der Dokumentation
abzuleiten.

---

## 5. Testdateien als Prüfstand für den Mehrzonen-Import

Alle Zahlen dieses Abschnitts wurden am 15.09.2026 durch Herunterladen und Auszählen der Dateien
gewonnen.

### 5.1 Übersicht

| Datei | Größe | Quelle | Räume | Grenzen (1./2. Ebene) | 2a mit Gegenstück | U-Werte | g-Werte | Schichten | λ/ρ/c brauchbar |
|---|---|---|---|---|---|---|---|---|---|
| `AC20-FZK-Haus.ifc` | 2,5 MB | ifcwiki.org (KIT/IAI) | 7 | 0 / 0 (81 Basisklasse) | **—** | 33 | 0 | 4 Sätze, 4 Schichten | **0** |
| `AC20-FZK-Haus_with_SB_IBPSA_P1.ifc` | 2,9 MB | gitlab.e3d.rwth-aachen.de, `ifc2idfvalidation` | 4 | 131 / 206 | **124/124** | (aus FZK übernommen) | 0 | 4 / 4 | **0** |
| `AC20-Institute-Var-2_with_SB.ifc` | 13,4 MB | dito | 78 | 1 322 / 2 022 | **1 472/1 472** | **0** | 0 | 4 / 4 | 1 (Aluminium) |
| `FM_ARC_DigitalHub_with_SB_v1.ifc` | 17,6 MB | github.com/RWTH-E3D/DigitalHub, `Version_1` | 59 | 1 560 / 2 582 | **1 724/1 724** | 359 | 85 (6 gefüllt) | 20 / 38 | **0** |

Die Zahl 7 statt der in Befund C und Konzept 7.8 genannten 8 Räume des FZK-Hauses beruht auf der
Zählung der Entität `IFCSPACE(`; das achte Vorkommen der Zeichenfolge ist `IFCSPACETYPE(`. Die
Angabe „8 Räume" ist entsprechend zu korrigieren.

### 5.2 Was jede Datei prüft

| Datei | wofür sie der Prüfstand ist |
|---|---|
| `AC20-FZK-Haus.ifc` | **der schwere Fall:** Basisklasse statt 2ndLevel, kein `CorrespondingBoundary`, `IsExternal` fehlt, Stoffwerte auf null, `IfcCompositeCurve` statt `IfcPolyline`, 3D-Randpunkte, eigene Eigenschaften im Norm-Pset, eine Klassifikation über alle Räume. Wer hier durchkommt, kommt überall durch |
| `AC20-FZK-Haus_with_SB_IBPSA_P1.ifc` | **der Sollfall im Kleinen:** dieselbe Geometrie mit echten 1st/2nd-Level-Entitäten, 124 gefüllten Gegenstücken und `EXTERNAL_EARTH`; zugleich der Beleg, dass Zonen **Zusammenfassungen** von Räumen sind — aus den 7 Räumen sind 4 Zonen geworden, die erste trägt im `LongName` vier alte GUIDs (`oldSpaceGuids_0Lt8gR…_17JZcM…_2dQFgg…_3$f2p7…`) |
| `AC20-Institute-Var-2_with_SB.ifc` | **die Größe:** 78 Zonen, 2 022 Grenzen der 2. Ebene, 1 472 Paare — der Lauf- und Speichertest, zugleich der Fall **ohne jeden U-Wert**, also reine Vorgabenrechnung |
| `FM_ARC_DigitalHub_with_SB_v1.ifc` | **der Revit-Fall:** echte Schichtsätze mit Namen und Kategorien, 359 U-Werte, `Pset_SpaceOccupancyRequirements`, drei Geschosse mit negativer Kellerhöhe und dem Erdgeschoss bei −0,15 m, `EXTERNAL_EARTH`, Bauteiltypen bis `IfcCurtainWall`, und **kein einziges thermisches Material-Pset** |

### 5.3 Lizenzlage

- **KIT/IAI-Dateien** (FZK-Haus, Smiley-West, Institute): Nutzung uneingeschränkt mit
  Namensnennung im vorgegebenen Wortlaut — wie in Befund C und Konzept 7.8 vermerkt.
- **`RWTH-E3D/DigitalHub`**: Das Repositorium führt heute eine `LICENSE` mit dem vollen
  MIT-Text, „Copyright (c) 2020 RWTH Aachen University — E3D Institute of Energy Efficiency and
  Sustainable Building"; die GitHub-API meldet `spdx_id: MIT`. **Die in Befund C vermerkte
  fehlende Lizenzdatei ist damit erledigt**; MIT erlaubt die Aufnahme in
  `Referenzlaeufe/Importproben/` samt Weitergabe, verlangt aber die Mitlieferung von
  Lizenztext und Vermerk — dieselbe Auflage wie bei den KIT-Dateien und zu erledigen an
  derselben Stelle (Quellenvermerk und Lizenzhinweise, Befund N 2.2 und 5.1).
- **`gitlab.e3d.rwth-aachen.de/e3d-software-tools/ifc2idfvalidation`** (die beiden
  `…_with_SB`-Fassungen): Lizenz **nicht geprüft**. Vor der Aufnahme ins Repositorium zu klären;
  bis dahin nur zum Messen in einem Arbeitsordner außerhalb des Repositoriums — so wie in dieser
  Sitzung geschehen.

### 5.4 Was davon ins Repositorium gehört

**Vorschlag: zwei Dateien, zusammen rund 20 MB, als gewöhnliche Blobs** (nicht LFS — der Ordner
`Referenzlaeufe/Importproben/` ist LFS-frei, Befund N 4.6):

1. `AC20-FZK-Haus.ifc` (2,5 MB) — bereits für G4a vorgesehen (Konzept 7.6 Nr. 6), deckt hier
   zusätzlich den Archicad-Rekonstruktionsfall ab.
2. `FM_ARC_DigitalHub_with_SB_v1.ifc` (17,6 MB) — MIT, deckt Zonen, Paare, Schichten,
   `EXTERNAL_EARTH`, drei Geschosse und den Revit-Weg ab.

Die 13,4-MB-Institute-Datei ist der Lauftest; sie lohnt nur, wenn die Lizenzfrage geklärt ist,
und ist sonst durch den DigitalHub ersetzbar. **17,6 MB in einem Repositorium sind eine
Entscheidung des Anwenders** — die Alternative ist ein Test, der die Datei zur Laufzeit holt und
ohne sie schweigt (das Muster der Testdatenbank, `EPOS.Kern/CLAUDE.md` „Tests mit Datenbank").

---

## 6. Der Abbildungsplan IFC → Zonenmodell

### 6.1 Die drei Tabellen

**Zone** — je thermischer Zone eine Zeile:

| Feld | Quelle | Rückfall | Herkunft |
|---|---|---|---|
| `Bezeichnung` | `IfcZone.LongName`, sonst Klassifikation, sonst Nutzung, sonst `IfcBuildingStorey.Name` | „Zone 1" | IFC / Vorgabe |
| `Regel` | Z1…Z5 (§ 1.7) | Z5 | — |
| `Grundflaeche` | Σ `Qto_SpaceBaseQuantities.NetFloorArea` der Räume | `GrossFloorArea`; sonst leer, **Pflichtfeld** | IFC / leer |
| `Raumhoehe` | flächengewichtetes Mittel `Height` | `NetVolume / NetFloorArea`; sonst 2,5 m | IFC / Vorgabe |
| `Volumen` | Σ `NetVolume` | Grundfläche × Raumhöhe | IFC / Vorgabe |
| `IstBeheizt` | Regel B1…B6 (§ 1.7) | beheizt | IFC / Vorgabe |
| `Geschoss` | `IfcBuildingStorey.Name` + `Elevation` (relativ) | — | IFC |
| `Raeume` | Liste der `IfcSpace.GlobalId` | — | IFC |
| `Nutzung` | `Pset_SpaceOccupancyRequirements.OccupancyType` bzw. Klassifikation | Wohngebäude | IFC / Vorgabe |

**Bauteil je Zone** — je Grenzfläche (bzw. je zusammengefasster Gruppe) eine Zeile:

| Feld | Quelle | Rückfall | Herkunft |
|---|---|---|---|
| `Bauteilart` | Typ von `RelatedBuildingElement` + `PredefinedType` (§ 2.5) | Sonstiges | IFC |
| `Flaeche` | Polygonfläche der Grenze, abzüglich Öffnungen (§ 2.6) | `Qto_*BaseQuantities` des Bauteils | IFC / leer |
| `Azimut` | Normale der `IfcPlane` über Placement-Kette und `TrueNorth` | Sammelposten, Warnung | IFC / leer |
| `Neigung` | Winkel Normale/z | 90° bei Wand, 0° bei Decke | IFC / Vorgabe |
| `Randbedingung` | `InternalOrExternalBoundary` (§ 2.4) | Außenluft | IFC / Vorgabe |
| `NachbarZone` | `CorrespondingBoundary.RelatingSpace` → Zone; sonst Rekonstruktion (§ 2.2) | leer | IFC / abgeleitet / leer |
| `U_Wert` | `Pset_*Common.ThermalTransmittance` | aus Schichten; sonst Vorgabe je Baualtersklasse | IFC / abgeleitet / Vorgabe |
| `g_Wert`, `Rahmenanteil` | `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance`, `Pset_WindowCommon.GlazingAreaFraction` | Vorgabe 0,6 bzw. 0,3 | IFC / Vorgabe |
| `Aufbau` | Verweis auf den Schichtsatz | leer → masselos (§ 3.7) | IFC / leer |
| `Beleg` | Entität, Pset/Qto, Grenz-GUID | — | — |

**Aufbau und Schicht** — je `IfcMaterialLayerSet` eine Zeile, je Schicht eine:

| Feld | Quelle | Rückfall | Herkunft |
|---|---|---|---|
| `Bezeichnung` | `IfcMaterialLayerSet.LayerSetName` | Bauteilname | IFC / Vorgabe |
| `Reihenfolge` (innen → außen) | `MaterialLayers` + `DirectionSense`/`OffsetFromReferenceLine` + Normale der Raumgrenze (§ 3.3) | Liste wie geschrieben, Annahme benannt | IFC / Annahme |
| `Dicke` | `IfcMaterialLayer.LayerThickness` | — (ohne Dicke keine Schicht) | IFC |
| `Baustoff` | `IfcMaterial.Name` | — | IFC |
| `Lambda`, `Rho`, `cp` | `Pset_MaterialThermal` / `Pset_MaterialCommon` über `HasProperties`, **nur im Plausibilitätsband** (§ 3.4) | Namensabgleich N1…N7 (§ 3.6) | IFC / Katalog / Manuell |
| `IstLuftschicht` | `IsVentilated` bzw. Name | — | IFC / abgeleitet |

### 6.2 Die Fehlerbilder

| Bild | Erkennung | Wirkung | Meldung (neu, Muster wie Befund N 4.5) |
|---|---|---|---|
| **keine Raumgrenzen** | `IfcSpace.BoundedBy` überall leer | Zonenregel auf Z5, Flächen aus Quantities | `IMP_IFC_PROT_KEINE_GRENZEN` (Warnung) |
| **nur 1. Ebene** | keine 2ndLevel-Entität und kein `'2nd'` in Name/Description | Grenzen unbrauchbar für Nachbarschaft; Z5 vorgeben | `IMP_IFC_PROT_NUR_1STLEVEL` (Warnung) |
| **Fläche ohne Gegenstück** | `INTERNAL`, `CorrespondingBoundary` leer, Rekonstruktion mehrdeutig | Randbedingung „unbeheizter Nachbar", Zeile im Dialog rot | `IMP_IFC_PROT_OHNE_GEGENSTUECK` (Warnung, mit Anzahl und Fläche) |
| **Bilanzlücke** | Σ Trennflächen Zone A→B ≠ Σ Zone B→A (> 2 %) | beide Zahlen zeigen, größere nehmen | `IMP_IFC_PROT_TRENNFLAECHE_UNGLEICH` (Warnung) |
| **Überlappung** | Ebene mit Innenrändern **und** Kindgrenzen auf derselben Fläche | nur einmal abziehen | `IMP_IFC_PROT_UEBERLAPPUNG` (Warnung) |
| **Öffnung größer als Wand** | A_netto < 0 | A = 0, Zeile rot | `IMP_IFC_PROT_NETTOFLAECHE_NEGATIV` (Fehler der Zeile) |
| **Stoffwert null oder außerhalb** | λ ≤ 0, ρ ≤ 0, c ≤ 0 oder außerhalb des Bandes | Rückfall Namensabgleich | `IMP_IFC_PROT_STOFFWERT_UNGUELTIG` (Warnung, mit Stoffname) |
| **Baustoff unbekannt** | Namensabgleich ohne Treffer | Anwenderzuordnung, Zeile gelb | `IMP_IFC_PROT_BAUSTOFF_UNBEKANNT` (Warnung) |
| **Zone ohne Hülle** | Zone ohne einzige Grenze `EXTERNAL`/`EXTERNAL_EARTH` | Innenzone — zulässig, aber benannt | `IMP_IFC_PROT_ZONE_OHNE_AUSSEN` (Info) |
| **Zone zu klein** | Fläche < max(2 m², 2 %) | Vorschlag: zusammenlegen | `IMP_IFC_PROT_ZONE_ZU_KLEIN` (Info) |
| **Raum in mehreren Zonen** | Mehrfachzuordnung über `IfcZone` | Raum bleibt unzugeordnet | `IMP_IFC_PROT_RAUM_MEHRFACH` (Warnung) |
| **gekrümmte Grenzfläche** | `IfcSurfaceOfLinearExtrusion` o. ä. | Fläche leer, Zeile rot | `IMP_IFC_PROT_FLAECHE_UNBEKANNT` (Warnung) |

Alle Meldungen sind `PruefMeldung` mit Schlüsseln in **beiden** `.resx`, danach
`Werkzeuge/ResourceDesigner` (Regel in `EPOS.Kern/CLAUDE.md`, Abschnitt „Regeln für Änderungen
hier"); das Protokoll ist die Meldungsliste in Reihenfolge und wird nicht geschrieben (Befund N,
4.5).

### 6.3 Wie der Zuordnungsdialog sie zeigt

Ein Dialog, drei Abschnitte, ein OK. Der Aufbau folgt dem Konfliktdialog
(`EPOS.UI/Dialoge/Import/ImportKonflikteDialog.razor`), die Listen der virtualisierten
`Katalogliste` (Regel und Prüfstand: `Proben/Rasterprobe`, Wurzel-`CLAUDE.md`), weil die
Institute-Datei 78 Zonen und über 2 000 Flächen liefert.

1. **Kopf** — Datei, Schema, Gebäudewahl, Zonenregel (Z1…Z5), Bilanz (Zonen, beheizte Fläche,
   Volumen, Σ Außenfläche, Σ Trennfläche) und das Warnbanner mit der schwersten Meldungsstufe.
2. **Zonen** — je Zone eine Zeile: Name, Regel, Räume, Fläche, Volumen, Haken „beheizt",
   Knöpfe „zusammenlegen" / „trennen". Aufgeklappt darunter die Raumliste mit Name, `LongName`,
   Geschoss, Fläche, Beheizungsregel (B1…B6) und Beleg.
3. **Flächen je Zone** — Bauteilart, Fläche, Azimut, Neigung, Randbedingung, Nachbarzone,
   U-Wert, Aufbau, Herkunft, Beleg. Filter: „nur Fehler", „nur ohne Gegenstück", „nur ohne
   U-Wert", „nur ohne Stoffwerte".
4. **Baustoffe** — die Namensliste aus der Datei gegen den EPOS-Katalog: IFC-Name,
   Abgleichstufe (N1…N7), zugeordneter Baustoff (Klappliste), λ/ρ/c, Herkunft. **Diese Liste
   ist die Arbeit des Anwenders**, und sie ist kurz: gemessen 6 bis 13 Namen je Datei.

**Vier Regeln, die nicht verhandelbar sind:**

- **Nichts wird ohne OK geschrieben** (Hausmuster OK/Abbrechen, Konzept 7.6 Nr. 5).
- **Jede Zahl trägt ihre Herkunft und ihren Beleg** — eine Zelle ohne Beleg ist eine Vorgabe.
- **Kein Anzeigetext ist Steuerwert** (`ImportKonfliktModell.cs:41-46`).
- **Die Plausibilitätsprüfungen laufen vor dem Schreiben**, nicht danach (Konzept 4.8); dazu
  gehört im Mehrzonenmodell neu: geschlossene Hülle je Zone, Trennflächenbilanz, Summe der
  Zonenflächen gegen die Summe der Raumflächen.

### 6.4 Abnahme

Der Mehrzonenimport ist nachgewiesen, wenn

1. für alle vier Messdateien die in § 5.1 genannten Zahlen **wiedergefunden** werden (Zonen,
   Grenzen, Paare, U-Werte, Schichten) — das sind reine Zählproben ohne Datenbank;
2. die Polygonflächen der § 2.3-Messung auf 0,1 m² getroffen werden (die Summen 778,2 / 1 358,1 /
   22 862,1 m² sind die Sollwerte dieser Sitzung);
3. das FZK-Haus **mit** und **ohne** Space-Boundary-Anreicherung auf dieselbe beheizte Fläche
   kommt (die vier Zonen der angereicherten Fassung fassen dieselben sieben Räume zusammen);
4. die Einzonenrechnung desselben Gebäudes (Z5) die Zahlen aus Befund N reproduziert — **das ist
   die eigentliche Probe**: Das Mehrzonenmodell muss den Einzonenfall als Sonderfall enthalten;
5. der Abstand zwischen Raumseitenmaß und Bruttomaß (§ 2.6) für das FZK-Haus beziffert ist —
   diese Zahl ist heute nicht bekannt und entscheidet, ob Weg 1 tragbar ist.

---

## 7. Offene Fragen für den Anwender

1. **Zonenregel als Vorgabe.** Vorschlag Z4 (je Geschoss), Rückfall Z5 (§ 1.7). Oder soll ein
   Import stets Z5 vorschlagen und die Mehrzonigkeit ausdrücklich verlangt werden?
2. **Raumseitenmaß oder Bruttomaß** (§ 2.6). Vorschlag: Raumseitenmaß durchhalten und benennen.
   Das weicht von der Bemaßungsregel des Einzonenmodells ab und ist ein Entscheid.
3. **Mindestgröße einer Zone.** Vorschlag max(2 m², 2 %). Oder soll jeder Raum eine Zone werden
   dürfen (78 Zonen im Institute-Fall)?
4. **Der Baustoffkatalog.** Die Stufe G3 (`Tab_Baustoff_STAMM`, `Tab_Bauteil`,
   `Tab_Bauteilschicht`) existiert im Repositorium nicht; der Mehrzonenimport setzt sie voraus.
   Wird G3 vorgezogen, oder importiert das Mehrzonenmodell zunächst **ohne** Schichten (nur
   U-Werte und Flächen, Masse aus `Bauweise`)?
5. **Die Synonymtabelle** (§ 3.6) ist eine neue Datentabelle, zweisprachig, mit Merkfunktion je
   Projekt. Gehört sie in die Auslieferung (`_STAMM`) oder bleibt sie eine reine Projektgröße?
6. **Der DigitalHub im Repositorium** — 17,6 MB als gewöhnlicher Blob (MIT, § 5.3), oder Test,
   der die Datei zur Laufzeit sucht und ohne sie schweigt?
7. **Die E3D-Dateien** (`…_with_SB`) sind der einzige Weg zu echten 2nd-Level-Entitäten im
   kleinen Format; ihre Lizenz ist ungeklärt (§ 5.3). Nachfragen oder darauf verzichten?
8. **Wie weit soll die Rekonstruktion gehen?** Der Archicad-Fall (§ 2.2) braucht die
   geometrische Paarbildung. Sie ist rechenbar, kostet aber Aufwand und Laufzeit. Alternative:
   den Archicad-Fall auf **eine Zone** beschränken (Z5) und Mehrzonigkeit nur anbieten, wo echte
   2nd-Level-Entitäten mit `CorrespondingBoundary` vorliegen — das wäre die ehrlichere, magere
   erste Stufe.

---

## 8. Anhang: Messprotokoll

**Dateien, heruntergeladen am 15.09.2026 in den Sitzungs-Scratchpad** (nicht im Repositorium):

| Datei | Bezugsort | Bytes |
|---|---|---|
| `AC20-FZK-Haus.ifc` | `https://www.ifcwiki.org/images/e/e3/AC20-FZK-Haus.ifc` | 2 570 803 |
| `AC20-FZK-Haus_with_SB_IBPSA_P1.ifc` | `https://gitlab.e3d.rwth-aachen.de/e3d-software-tools/ifc2idfvalidation/-/raw/main/src/ifc_files/…` | 2 857 082 |
| `AC20-Institute-Var-2_with_SB.ifc` | dito | 13 369 807 |
| `FM_ARC_DigitalHub_with_SB_v1.ifc` | `https://raw.githubusercontent.com/RWTH-E3D/DigitalHub/master/Version_1/…` | 17 621 384 |

**Verfahren:** Entitätszählung mit `grep -o -F` auf die Typnamen; Attributzerlegung, Auflösung
der Verweise und Polygonflächen mit einem Perl-Einwegskript (Trapezformel über die ersten beiden
Koordinaten der Randpunkte, `IfcCompositeCurve` aufgelöst). Die Zählungen sind **Textzählungen**,
keine Auswertung über eine IFC-Bibliothek; sie sind für Entitätsmengen exakt und für
Attributwerte so genau, wie die Schreibweise der Datei einheitlich ist. Wer sie
weiterverwendet, misst sie mit xBIM nach — das ist zugleich die erste Probe des Lesers.

**Spezifikationsquellen** (Abruf 15.09.2026): die Lexikonseiten der IFC-4.3-Dokumentation über
die Spiegelung `bimant.com` (Einzelverweise an den Aussagen), die Attributdefinition von
`IfcRelSpaceBoundary` zusätzlich über dieselbe Spiegelung, und die xBIM-Quelltexte über
`raw.githubusercontent.com/xBimTeam/XbimEssentials/master/Xbim.Ifc4/…`. Die
Originaladressen unter `standards.buildingsmart.org` antworteten dem Abrufwerkzeug mit HTTP 403;
inhaltliche Abweichungen zwischen Spiegel und Original sind nicht auszuschließen und vor der
Umsetzung an der Originalseite zu prüfen.
