# Konzept: HottCAD-Verbund — IFC zuerst, Projektdatei ergänzt, Hülle nach Randbedingung sichtbar (EPOS-Plan)

> **Entscheidstand:** Rev. 2 vom 05.10.2026. Ziel 3 (Raum → Zone aus der Projektdatei) ist mit
> den Wellen SQ-1 bis SQ-3 (#731, E80, Datenaustauschkonzept Nachtrag 3) bereits gebaut; dieses
> Papier beschreibt dazu den Nachzug nach **E87** (Wahl der Zonierung, Vorgabe
> DIN-V-18599-Zone, Größengrenze). **E87** entscheidet die Fragen F1 bis F5 (Kapitel 7); es ist
> nichts mehr offen. Umsetzung in den Wellen HC-1 bis HC-4 (Kapitel 6).

Der Anwender hat am 05.10.2026 drei Ziele genannt: **(2)** alle sinnvollen Informationen aus
der IFC-Datei nutzen, nicht aus der `.sqproj`; **(3)** was in der IFC fehlt und wesentlich ist,
aus der `.sqproj` nehmen, vor allem die Zuordnung Raum → Zone; **(4)** die Geometrie aus der
IFC darstellen (2D und 3D) und dabei die thermisch relevanten Bauteile sichtbar in Gruppen
einteilen: beheizt gegen unbeheizt, beheizt gegen außen, Boden gegen unbeheizt oder außen,
Boden gegen Erdreich.

**Grundlagen:** [Befund HottCAD-Projektdatei](2026-10-05_Befund_HottCAD_Projektdatei.md)
(Struktur der `.sqproj`, Codetabellen, Schlüssel `GId`), [Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md)
Kapitel 6 (Zonierungsregeln Z1–Z6, 6.5 Rückfälle, 6.7 Grundrissansicht),
[Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) Kapitel 2 (Zuordnungsgerüst,
Herkunft je Feld, Plattformweg), 14 (Körperansicht), 15 (Raumkörper, G7f) und **16 (Nachtrag 3:
Projektdatei, Stufe SQ)**, [ADR-003](../ADR-003_IFC_xBIM_ohne_Geometriekernel.md) (xBIM ohne
Geometriekernel), die Entscheide E72, E73, E79, E80, E81 und E87 der
[Statusdatei](../Status_Gebaeudesimulation_VDI6007.md), der [Plan G7b bis KU3](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-04_Plan_G7b_bis_KU3.md)
als Muster des Wellenzuschnitts. Code: `EPOS.Kern/Allgemein/Import/Ifc/`,
`EPOS.Kern/Allgemein/Import/Sqproj/`, `EPOS.Kern/Allgemein/Import/Gebaeude/`,
`EPOS.UI/Bausteine/GebaeudeAnsicht.razor`, `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs`.


## 0. Das Ergebnis in sechs Punkten

1. **Ziel 2 ist weitgehend erreicht und wird vollendet.** Der Kern liest die HottCAD-IFC schon
   bis zu Raumkörpern, Raumbezügen, Beheizung, Solltemperatur, Raumtyp, Bauteil-U-Werten und
   Aufbauten. Es fehlen drei Dinge, die die Datei hergibt: die **zweiseitige Randbedingung**
   je Bauteil (`HSETU_Bauteilreferenzen.ElementReferences[0|1].AdjacentType`), die
   **Körper der Hüllbauteile** (`IfcShellBasedSurfaceModel` an Außenwänden, Dächern,
   Fenstern, Bodenplatten) und der **Rahmenanteil** der Fenster.
2. **Ziel 4 kommt ohne Projektdatei aus.** Die Randbedingungscodes `btaOutside`, `btaGround`,
   `btaHeated`, `btaUnHeated`, `btaCellarCeiling`, `btaUppermostStorey` stehen an jedem
   Bauteil der IFC und ergeben die gewünschten Gruppen unmittelbar. Darstellen lässt sich jede
   Gruppe über die **Flächen der Raumkörper**: Eine Fläche mit Gegenfläche eines anderen Raums
   ist eine Trennfläche (Gruppe nach Beheizung des Nachbarn), eine Fläche ohne Gegenfläche ist
   Hülle (Gruppe nach dem Bauteil, das der Raumbezug nennt). Das verlängert G7f-4
   (Körpernachbarschaft) um die Hüllseite; Hüllbauteile mit eigenem Körper werden zusätzlich
   als Körper gezeichnet. Die Gruppenliste R0–R7 (4.1) ist mit E87 festgelegt.
3. **Der Viewer existiert und wird um einen Farbmodus erweitert.** `GebaeudeAnsicht.razor`
   zeichnet Grundriss (SVG) und Körper (three.js, lokal, MIT) im Importdialog. Neu ist der
   Umschalter **„Zonen | Randbedingung“** mit Legende der Gruppen, in 2D als gefärbte
   Raumkanten und Bodenflächen, in 3D als gefärbte Dreiecke der Raumkörper und Bauteilkörper.
   Außerhalb des Imports erscheint die Ansicht im Gebäudedialog über **„Datei erneut lesen“**
   (E87, kein Schema, keine Persistenz der Geometrie).
4. **Ziel 3 ist gebaut (SQ-1 bis SQ-3, #731).** Der Importdialog lädt die `.sqproj` zur IFC
   dazu; der Kern liest nur lesend, gleicht Räume über `GId` ↔ `GlobalId` und dann über den
   Raumnamen je Geschoss ab, übernimmt die Zonen als Zonenplan und bildet Nutzung, Heiz- und
   Kühlsollwerte, Lüftung, Geräte und Personen je Zone als Konditionierung — **soweit die
   Projektdatei sie trägt** (E87, F2). **Nachzug nach E87:** Trägt die Projektdatei beide
   Zonierungen, bietet der Dialog die Wahl „DIN-V-18599-Zonen | Simulationszonen“ an;
   **Vorgabe ist die DIN-V-18599-Zone (`ZoneType` 5)**, weil sie in jeder Projektdatei
   vorhanden ist. Bisher galt bei Räumen in mehreren Zonen die Simulationszone.
5. **Kein Klimaimport, keine zweite Importart.** E80 hat entschieden: Die Hülle kommt weiter
   aus dem IFC-Export, das Projektklima bleibt. Die Größengrenze der Projektdatei wird
   eigenständig: **250 MB Windows, 100 MB iOS** (E87, F4), weil der Leser nur kleine Tabellen
   liest und die Größe aus eingebetteten Bildern stammt.
6. **Aufwand:** HC-1 und HC-2 zusammen 3 bis 5 PT, HC-3 (Nachzug E87 an SQ) 0,5 bis 1 PT,
   HC-4 („Datei erneut lesen“) 1 PT. Keine Rückfrage mehr vor einer Welle. Kein neues Paket,
   kein Geometriekernel, kein web-ifc, kein Schemaschritt.


## 1. Einordnung

### 1.1 Was die drei Ziele vom Bestand verlangen

| Ziel | Heute (Stand #731) | Lücke |
|---|---|---|
| (2) IFC ausreizen | `IfcAbbildBauer` liest Struktur, Räume, Raumkörper (G7f-1), Raumbezüge, Beheizung und Sollwert (`HSETU_RaumAllgemein`), Raumtyp (`mrt…`), Bauteile mit U-Wert, einseitige Randbedingung (`HSETU_BauteilAllgemein.AdjacentType`), Aufbauten | zweiseitige Randbedingung, Bauteilkörper, Rahmenanteil (`HSETU_EcoCad.FractionOfFrame`) |
| (3) Raum → Zone aus `.sqproj` | **gebaut:** `Import/Sqproj/` (Leser, Abbild, Raumabgleich, Zonen, Konditionierung, Protokoll), Knopf „Projektdatei dazuladen“, Zonenbaum (ZB-1, E79, E81), Proben 33–38 | Wahl der Zonierung mit Vorgabe DIN-Zone, eigene Größengrenze (E87); Diagnose an echten Dateien steht beim Anwender |
| (4) Geometrie 2D/3D mit Gruppen | Grundriss und Körperansicht im Importdialog, Zonenfarben, Trennflächen aus Raumkörpern (G7f-4) | Klassifikation **aller** Flächen nach Randbedingung, Farbmodus, Legende, Bauteilkörper, Ansicht außerhalb des Imports |

### 1.2 Was dieses Papier an den Nachbarpapieren ändert

- **Datenaustauschkonzept 16.3 (Nachtrag 3):** Der Satz „Liegt ein Raum in mehreren Zonen,
  gilt die Simulationszone“ wird durch E87 ersetzt: Der Anwender wählt die Zonierung, Vorgabe
  ist `ZoneType` 5. Die Größengrenze der Projektdatei löst sich von der IFC-Grenze. Nachzug
  mit HC-3.
- **Datenaustauschkonzept 15.1:** Die Körperansicht zeigt weiterhin keine Dicken und keine
  Öffnungen als Ausschnitt; sie zeigt aber **Hüllbauteile mit eigenem Körper** als Flächennetz
  und färbt Raumflächen nach Randbedingung. Die Datei bleibt Quelle, nichts wird repariert.
- **E73 (Rangfolge Raumgrenzen vor Körpern vor Raumbezügen)** gilt unverändert; die
  Klassifikation der Hüllflächen nimmt denselben Rang.


## 2. Was heute schon steht

| Baustein | Datei | Leistung |
|---|---|---|
| IFC-Leser | `EPOS.Kern/Allgemein/Import/Ifc/IfcLeser.cs`, `IfcAbbildBauer.cs` | STEP, ifcXML, ifczip; IFC2X3/4/4X3; Struktur, Räume, Zonen, Raumgrenzen, Raumbezüge, Bauteile, Aufbauten, Stoffwerte; `HSETU_*` für Beheizung, Sollwert, Raumtyp, Randbedingung (einseitig), Verkleidung |
| Raumkörper | `Ifc/IfcRaumkoerper.cs` | Dreiecksnetz aus `IfcFacetedBrep`, `IfcShellBasedSurfaceModel`, `IfcExtrudedAreaSolid`, Dreiecks- und Vieleckssätze, `IfcMappedItem`; Vermerke (Bogen, Loch, Uneben, Offen) |
| Nachbarschaft | `Gebaeude/Koerpernachbarschaft.cs` | Trennflächen aus gegenläufigen gemeinsamen Flächen zweier Raumkörper (E73, G7f-4) |
| Zonierung | `Gebaeude/GebaeudeZonierung.cs`, `Gebaeude/Zonenplan.cs` | Z1–Z6 und X1–X4, M7 Vorgabe, M8 Mindestgröße, Zonenplan mit Umhängen, Anlegen, Löschen (ZB-1) |
| Projektdatei | `EPOS.Kern/Allgemein/Import/Sqproj/` (`SqprojLeser`, `SqprojAbbild`, `SqprojRaumabgleich`, `SqprojZonen`, `SqprojKonditionierung`, `SqprojProfil`, `SqprojProtokoll`, `SqprojStand`) | `.sqproj` nur lesend (`Mode=ReadOnly`, `query_only`), Fassungsprüfung, Räume, Zonen (`ZoneType` 5 und 6, Zonierung wählbar mit Vorgabe DIN-V-18599-Zone, eigene Größengrenze 250/100 MB), Profile, Ganglinien, Kalender; Abgleich `GId` ↔ `GlobalId`, dann Name je Geschoss; Konditionierung je Zone |
| Herkunft | `Gebaeude/Importherkunft.cs`, `GebaeudeFeldzeile.cs` | je Feld Herkunft und Beleg; persistiert `Tab_Zone.Herkunft`, `Tab_Bauteil.Herkunft`, `Tab_Importquelle`, `Tab_Importzuordnung` |
| Ansicht | `EPOS.UI/Bausteine/GebaeudeAnsicht.razor`, `GebaeudeAnsichtZeichnung.cs`, `wwwroot/epos-gebaeude-koerper.js`, `wwwroot/three/` | Reiter „Grundriss \| Körper“, Zonenfarben, „Dateikörper \| Exportmodell“, Klick meldet Raum und Zone; three.js 0.186.1 lokal mit Lizenzwache |
| Datenseite | `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs`, `GebaeudeImportAnsicht.cs`, `GebaeudeImportZonen.cs` | Dateiwahl über `Dienste.Datei`, Größengrenze, Lesen im Arbeitsfaden, Projektdatei dazuladen und entfernen, DTO der Ansicht |
| Dialog | `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor` | Regelliste, Zonenbaum mit „aus Projektdatei“, nicht zugeordnete Räume, Bauteile, Baustoffabgleich, eingebettete Ansicht |
| Proben | `Referenzlaeufe/Importproben/`, `EPOS.Kern.Tests/IfcProbenErzeuger*.cs`, `SqprojProbenErzeuger*.cs`, `Quellen/*.ifc` | über 30 synthetische IFC-Proben, synthetische `.sqproj`-Proben (33–38), sechs HottCAD-Anwenderdateien; `SqprojQuelldateienDiagnoseTests` gegen `Quellen/<name>.sqproj`, wenn vorhanden |

Nicht vorhanden: eine Klassifikation der Hüllflächen, Bauteilkörper, ein Farbmodus nach
Randbedingung, eine Wahl der Zonierung der Projektdatei, eine Ansicht außerhalb des Imports.


## 3. Ziel 2 — die IFC vollständig ausreizen

### 3.1 Zweiseitige Randbedingung

`HSETU_Bauteilreferenzen` trägt an jedem Bauteil `ElementReferences[0].AdjacentType` und, bei
Innenbauteilen, `ElementReferences[1].AdjacentType`, dazu je Seite `Orientation (°)` und
`ElementAssignmentDirectionType`. Heute liest `IfcAbbildBauer.Bauteil()` nur
`HSETU_BauteilAllgemein.AdjacentType` über `ANGRENZUNG_ABBILDUNG`. Neu:

- beide Seiten lesen und am `AbbildBauteil` als `RandbedingungSeiteA/B` halten;
- die Abbildung der `bta…`-Codes um `btaCellarCeiling`, `btaUppermostStorey` und `btaNone`
  ergänzen (heute unbekannt → Rückfall), mit `Randbedingung.Unbeheizt` als Ziel für die ersten
  beiden und einem Vermerk „Dachraum“ bzw. „Keller“ als Beleg;
- die **wirksame Randbedingung** eines Bauteils ist die nicht beheizte Seite; bei
  `btaHeated` auf beiden Seiten ist das Bauteil innen und thermisch neutral.

Gemessen an zwei Dateien (Wohngebäude, Sportheim): alle 440 bzw. 211 Bauteile tragen den
Satz; `btaUnHeated` kommt an Wänden beidseitig vor (eine Seite beheizt, eine unbeheizt), an
Decken stehen `btaCellarCeiling` und `btaUppermostStorey` jeweils der beheizten Seite gegenüber.

### 3.2 Körper der Hüllbauteile

Hüllbauteile haben in der HottCAD-IFC eine Darstellung `Body`/`SurfaceModel`
(`IfcShellBasedSurfaceModel` aus `IfcPolyLoop`); Innenbauteile und raumseitige Stücke sind
geometrielos. Am Wohngebäude: 75 von 117 Wänden, 10 von 46 Platten, 7 von 17 Dächern, 31 von
32 Fenstern mit Körper; am Sportheim 92 von 205 Wänden, 78 von 80 Fenstern.

`IfcRaumkoerper` liest `IfcShellBasedSurfaceModel` bereits für Räume. Neu ist nur der Aufruf
für `IfcWall`, `IfcSlab`, `IfcRoof`, `IfcWindow`, `IfcDoor` mit demselben Ohrenschnitt und
denselben Vermerken; das Ergebnis hängt als `Koerper` am `AbbildBauteil`. Grenzen bleiben
die von G7f-1: `DREIECKSGRENZE` 300 000 je Gebäude, kein Beschnitt, kein Boolesches Ergebnis
außer dem ersten Operanden. Die Körper dienen allein der Anzeige; Flächen für die Rechnung
kommen weiterhin aus den Mengensätzen (ADR-003).

### 3.3 Rahmenanteil

`HSETU_EcoCad.FractionOfFrame` steht an allen Fenstern; `Pset_DoorWindowGlazingType` wird
schon gelesen, ein g-Wert fehlt in den HottCAD-Dateien. Neu: `FractionOfFrame` als Beleg für
`Tab_Bauteil.Rahmenanteil` mit Herkunft `Ifc`; der g-Wert bleibt Vorgabe mit Beleg „nicht in
der Datei“. Kein weiterer Aufwand. Ein Wert über 1 gilt als Prozent (der CAD-Export schreibt 30),
ein Wert bis 1 als Anteil.

### 3.4 Was die IFC nicht hergibt

Zonen (nur Z4/Z6-Herleitung), Nutzungsprofile, Tagesganglinien, Kalender, Klimareihen,
Schichtaufbauten der Innenbauteile. Zonen, Profile, Ganglinien und Kalender liefert die
Projektdatei (Kapitel 5); Klima wird nach E80 nicht importiert; Innenaufbauten bleiben U-Wert
mit Vorgabe-Aufbau.


## 4. Ziel 4 — Flächenklassifikation und Darstellung nach Randbedingung

### 4.1 Die Gruppen (E87, F5)

Die Gruppen folgen der Frage des Anwenders und schließen die Dachseite, die Fenster und die
neutralen Innenflächen ein, damit jede Fläche genau eine Farbe hat:

| Nr. | Gruppe | Bauteile | IFC-Code der nicht beheizten Seite | Farbe (Token) |
|---|---|---|---|---|
| R1 | Wand beheizt gegen außen | `IfcWall` senkrecht | `btaOutside` | rot |
| R2 | Wand beheizt gegen unbeheizt | `IfcWall` | `btaUnHeated` | orange |
| R3 | Wand oder Boden gegen Erdreich | `IfcWall`, `IfcSlab` | `btaGround` | braun |
| R4 | Boden gegen unbeheizt oder außen | `IfcSlab` unten | `btaUnHeated`, `btaCellarCeiling`, `btaOutside` | gelb |
| R5 | Decke oder Dach gegen außen | `IfcRoof`, `IfcSlab` oben | `btaOutside` | blau |
| R6 | Decke gegen unbeheizt (Dachraum) | `IfcSlab` oben | `btaUnHeated`, `btaUppermostStorey` | hellblau |
| R7 | Fenster und Türen nach außen oder gegen unbeheizt | `IfcWindow`, `IfcDoor` | `btaOutside`, `btaUnHeated` | türkis |
| R0 | innen, thermisch neutral | alle | `btaHeated` beidseitig | grau, in 3D halbtransparent |

Die Zuordnung Boden/Decke entscheidet die **Flächennormale** der Raumkörperfläche (nach
unten = Boden, nach oben = Decke), nicht der IFC-Typ; so landet die Trenndecke zwischen
beheiztem Raum und unbeheiztem Keller beim oberen Raum in R4 und beim Keller in R0. Die
Farbtoken kommen aus dem Hausblatt (bestehende zehn Zonenfarben werden nicht angetastet,
die Gruppen bekommen acht eigene Token mit Dunkelmodus-Fassung).

### 4.2 Die Regel: jede Fläche eines Raumkörpers bekommt eine Gruppe

Ausgangspunkt ist das Dreiecksnetz je Raum aus G7f-1 und die Flächenpaarung aus G7f-4.

1. **Trennfläche (gepaart):** Die Fläche hat eine gegenläufige gemeinsame Fläche eines
   anderen Raums (`Koerpernachbarschaft`). Gruppe aus der Beheizung des Nachbarraums
   (`HSETU_RaumAllgemein.HeatingType`): beheizt → R0; unbeheizt → R2 (senkrecht), R4 (nach
   unten), R6 (nach oben). Aus Sicht des unbeheizten Raums ist dieselbe Fläche R0.
2. **Hüllfläche (ungepaart):** Die Fläche hat keinen Partner. Gruppe aus dem **Bauteil des
   Raumbezugs**, das zur Fläche passt — gleicher Raum (`IfcRelReferencedInSpatialStructure`),
   gleiche Orientierung (`ElementReferences[i].Orientation` gegen die Flächennormale, Toleranz
   15°), gleiche Lage (Wand, Boden, Decke). Dessen Randbedingung (3.1) gibt R1, R3, R4, R5
   oder R6. Fenster und Türen (R7) liegen als eigene Körper vor der Wand oder, ohne Körper, als
   Flächenanteil in der Legende.
3. **Rückfälle, benannt:** Hüllfläche ohne passendes Bauteil → Gruppe aus der Normale allein
   (senkrecht R1, unten R3 wenn unterstes Geschoss sonst R4, oben R5) mit Vermerk
   `FLAECHE_OHNE_BAUTEIL`; Raum ohne Körper → kein Eintrag, der Raum erscheint wie heute als
   Prisma „schematisch“ ohne Gruppenfarbe; Fläche mit Partner aber unbekannter Beheizung des
   Nachbarn → R2 mit Vermerk.

Rangfolge wie E73: liegen `IfcRelSpaceBoundary` vor (andere Autorensysteme), bestimmen sie
Partner und Randbedingung vor den Körpern; die Raumbezüge kommen zuletzt. Ergebnis ist je Raum
eine Liste `Flaechengruppe(Dreiecksindizes, Gruppe, Bauteilkennung, Beleg)` im Abbild, dazu
eine **Bilanz je Gruppe in m²** (Summe der Dreiecksflächen), die der Dialog neben der Legende
zeigt und die als Gegenprobe gegen die Bauteilflächen des Mengensatzes dient (Abweichung über
5 % wird als Hinweis gemeldet, nicht als Fehler).

### 4.3 Darstellung

- **2D (Grundriss, SVG):** Jede Kante des Raumpolygons bekommt die Gruppenfarbe ihrer
  senkrechten Flächen (R0 bis R3, R7 als Marke auf der Kante); die Bodenfläche des Raums wird
  im Modus „Randbedingung“ schraffiert nach R3/R4 oder bleibt weiß (R0). Decken sind im
  Grundriss nicht darstellbar und stehen nur in der Legende und Bilanz.
- **3D (Körper, three.js):** Die Dreiecke jedes Raumkörpers tragen die Gruppenfarbe; R0 ist
  halbtransparent, damit die Hülle von außen und von innen lesbar bleibt. Hüllbauteile mit
  eigenem Körper (3.2) werden zusätzlich als Netz in ihrer Gruppenfarbe gezeichnet und per
  Legende ein- und ausschaltbar. Klick auf ein Dreieck meldet Raum, Gruppe und Bauteil an
  Blazor zurück (vorhandener Rückkanal, erweitert um die Gruppe).
- **Umschalter „Zonen | Randbedingung“** im Reiterkopf der `GebaeudeAnsicht`; Legende mit
  Gruppen, Flächensumme und Schaltern je Gruppe; Zustand lebt im Dialog, nichts wird
  gespeichert.
- **DTO:** `GebaeudeAnsichtDaten` bekommt je Raum ein `Koerperfeld` mit Gruppenindex je
  Dreieck (ein Byte je Dreieck, angehängt an das vorhandene Bytefeld) und eine Gruppenliste;
  die Bauteilkörper kommen als eigene `Koerperfeld`-Einträge mit Gruppe und Bauteilkennung.
- **Außerhalb des Imports (E87, F3):** Der Gebäudedialog zeigt für ein importiertes Gebäude
  den Knopf „Datei erneut lesen“. Er liest die in `Tab_Importquelle` gespeicherte Datei (Name
  und SHA-256) über `Dienste.Datei` erneut, prüft den Hash, meldet eine Abweichung benannt
  und zeigt dann dieselbe `GebaeudeAnsicht` mit beiden Farbmodi, ohne etwas zu schreiben. Die
  Geometrie wird nicht persistiert.

### 4.4 Grenzen

Keine Wanddicken, keine Öffnungen als Ausschnitt, keine Verschattungskörper
(`IfcBuildingElementProxy` ohne Körper), keine Dachfenster ohne Körper. Ein Gebäude über der
Dreiecksgrenze fällt wie heute benannt auf Prismen zurück. Die Gruppen sind Anzeige und
Gegenprobe, **keine Rechengröße**: Die Simulation rechnet weiter mit den Bauteilen und
Randbedingungen aus Mengensatz und Zuordnung.


## 5. Ziel 3 — die Projektdatei als Ergänzungsquelle (gebaut, ein Nachzug)

### 5.1 Was SQ-1 bis SQ-3 liefern (#731, E80)

Die IFC bleibt die Importdatei. Im Zuordnungsdialog steht der Knopf „Projektdatei dazuladen
(.sqproj)“, aktiv nur bei einer IFC aus dem passenden CAD-Programm. Der Kern öffnet die Datei
nur lesend (`Mode=ReadOnly`, ohne Pool, `query_only`), prüft Fassung und Tabellen, liest
`BmBuilding → BmFloor → BmRoom`, `BmZone` mit `BmZoneReference`, die Profile mit
DIN-V-18599-Nummer, die 24-Stunden-Ganglinien und die Kalenderabschnitte. Räume werden zuerst
über `GId` ↔ `GlobalId` (GUID umkodiert in die 22-stellige Form), dann über den Raumnamen je
Geschoss abgeglichen; was nicht trifft, bleibt benannt unzugeordnet. Zonen vom `ZoneType` 5
und 6 werden Zonen des Zonenplans (ZB-1) mit Nutzung aus der Profilnummer; die Ganglinien
ergeben die Konditionierung je Zone (Heiz- und Kühlsollwert Tag/Nacht, Lüftung, Geräte,
Personen). **E87 (F2) bestätigt: übernehmen, soweit vorhanden** — eine Projektdatei ohne
Zeitprofile (so das Wohngebäude EH55, das nur `ZoneType` 5 und Nutzungsprofile trägt) liefert
Zonen und Nutzung, die Konditionierung bleibt dann bei den Vorlagen. Kein Schemaschritt, kein
neues Paket, keine Anwenderdatei im Repositorium; Proben 33 bis 38 grün. Einzelheiten:
Datenaustauschkonzept Kapitel 16.

### 5.2 Nachzug nach E87: Wahl der Zonierung, Vorgabe DIN-V-18599-Zone, Größengrenze

Gebauter Stand (**E87, F1**): Die Zonierung ist ein Parameter, die Räume gehören der wirksamen Zonierung.

- Trägt die Projektdatei **beide** Zonierungen (`ZoneType` 5 und 6 mit Räumen), bietet der
  Dialog die Wahl **„DIN-V-18599-Zonen | Simulationszonen“** an.
- **Vorgabe ist `ZoneType` 5** (DIN-V-18599-Zone), weil sie in jeder Projektdatei vorhanden
  ist; die Simulationszonen fehlen etwa im Wohngebäude EH55.
- Trägt die Datei nur eine der beiden Zonierungen, wird sie ohne Wahl genommen; `ZoneType` 2,
  7, 10, 0 und 8 bleiben gezählt und übersprungen (Nachtrag 3, 16.3).
- Die Wahl steht im Dialog neben dem Knopf „Projektdatei dazuladen“, wird im Protokoll
  vermerkt (`IMP_SQ_PROT_ZONIERUNG`) und beim gespeicherten Zonenplan als Quelle
  „aus Projektdatei (DIN-Zonen)“ bzw. „(Simulationszonen)“ geführt. Ein Wechsel der Wahl
  baut den Zonenplan neu auf; Handänderungen gehen dabei nach Rückfrage verloren.
- Die Konditionierung je Zone folgt der gewählten Zonierung: bei DIN-Zonen aus dem
  Nutzungsprofil der Zone und, wo vorhanden, aus der Profilgruppe der Simulationszone, mit
  der die DIN-Zone die meisten Räume teilt (Regel aus 16.3 bleibt).
- **Größengrenze (E87, F4):** Die Projektdatei hat in `SqprojProfil` eine eigene Grenze
  von **250 MB unter Windows und 100 MB auf iOS**, losgelöst von der IFC-Grenze (50/20 MB).
  Der Leser liest nur die Profil-, Zonen- und Raumtabellen; die Größe der Anwenderdateien
  (bis 211 MB) stammt aus eingebetteten Bildern, die nie gelesen werden. Die iOS-Grenze ist gesetzt; ihre Messung wie bei IFC (G4-8) steht aus, bis dahin lehnt iOS über 100 MB benannt ab.

Gebaut (#736): `SqprojZonen`, `SqprojStand`, `SqprojProfil`, `GebaeudeImportHuelle`, Dialog, Texte, Proben 33, 36 und 37 erweitert, Nachtrag 3 berichtigt.

### 5.3 Offen aus SQ, beim Anwender oder nach Zuruf

Diagnose an echten Projektdateien (`Quellen/<name>.sqproj` neben `<name>.ifc`, nur lokal),
Nachzug der drei Annahmen (Tagesart, Personenzahl, Geräteleistung), gespeicherter Zonenplan
als Startplan, Ziehen mit der Maus, Windows-Sichtabnahme, iOS-Lauf für den Dateifilter
`.sqproj`. Nichts davon ist Voraussetzung für HC-1 bis HC-4.


## 6. Wellenzuschnitt

### 6.1 Reihenfolge und Maßstab

| Nr. | Welle | Inhalt in einem Satz | PT | Schema | Rückfrage vorher |
|---|---|---|---|---|---|
| 1 | **HC-3** Nachzug E87 an SQ | Wahl „DIN-Zonen \| Simulationszonen“ mit Vorgabe 5, Protokoll, Quelle im Zonenplan, Größengrenze 250/100 MB, Nachtrag 3 berichtigt | 0,5–1 | nein | keine |
| 2 | **HC-1** Kern: IFC vollenden und Flächen klassifizieren | zweiseitige Randbedingung, Bauteilkörper, Rahmenanteil; Flächengruppen R0–R7 je Raumkörper mit Bilanz — **HC-1 umgesetzt (#740)**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-05_HC-1_IFC_Randbedingung_Bauteilkoerper_Flaechen.md) | 2–3 | nein | keine |
| 3 | **HC-2** Ansicht: Farbmodus Randbedingung | Umschalter, Legende, 2D-Kanten und Schraffur, 3D-Dreiecksfarben und Bauteilkörper — **HC-2 umgesetzt (#746)**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_HC-2_Farbmodus_Randbedingung.md) | 1–2 | nein | keine |
| 4 | **HC-4** „Datei erneut lesen“ | Ansicht im Gebäudedialog aus der gespeicherten Importquelle, Hashprüfung, beide Farbmodi | 1 | nein | keine |

HC-3 geht vor, weil der Anwender die Projektdateien jetzt importiert und die Vorgabe
DIN-Zone sofort wirken soll. Maßstab wie im Plan G7b bis KU3: 1 PT ≈ 0,8–1 Punkt des
Wochenkontingents; alle vier Wellen zusammen rund 4–6 Punkte. Jede Welle endet mit Build,
Tests mit `--filter`, Proben, einem Commit auf dem Arbeitszweig und dem Gate nach dem Merge;
Statuszeile und Protokoll wie gewohnt. Kein iOS-Lauf ohne Rückfrage; HC-2 und HC-4 brauchen
die Sichtabnahme auf Windows, die iPad-Sichtprobe hängt an der offenen Probe 31 aus G7f-2.

### 6.2 HC-3 — Nachzug E87 an SQ

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Kern | Opus | `SqprojZonen`: Zonierung als Parameter (5 Vorgabe, 6 wählbar), Verfügbarkeit beider Zonierungen im `SqprojStand`, Protokollsatz `IMP_SQ_PROT_ZONIERUNG`; `SqprojProfil`: Grenze 250/100 MB | `SqprojZonenTests` (Probe 36) mit beiden Zonierungen und mit nur einer; `SqprojLeserTests` (Probe 33) Grenze; gebaut (#736) |
| Hülle und Dialog | Opus | Wahlfeld neben „Projektdatei dazuladen“, nur sichtbar bei beiden Zonierungen; Quelle im Zonenbaum; Rückfrage beim Wechsel; iOS-Grenze in `GrenzeFuerPlattform` | `SqprojHuelleTests`, bunit `GebaeudeImportProjektdateiDialogTests` (Probe 37); Windows-Schale auf Linux kompiliert; gebaut (#736) |
| Papiere | Sonnet | Nachtrag 3 (16.2, 16.3, 16.4) berichtigt, Statuszeile, Protokoll, Wiki-Quelle des Importdialogs, Logbuch-Entwurf | Link-Wache grün; gebaut (#736) |

### 6.3 HC-1 — Kern: IFC vollenden und Flächen klassifizieren

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Randbedingung | Opus | `IfcAbbildBauer.Bauteil()`: beide `ElementReferences[i].AdjacentType` und `Orientation` lesen; `ANGRENZUNG_ABBILDUNG` um `btaCellarCeiling`, `btaUppermostStorey`, `btaNone`; `AbbildBauteil.RandbedingungSeiteA/B`, wirksame Randbedingung | `IfcCadBauteileTests` erweitert; Probe `ifc4_z6_cad.ifc` um zweiseitige Sätze ergänzt; `IfcQuelldateienDiagnoseTests` zählt Gruppen an den sechs Dateien unter `Quellen/` |
| Bauteilkörper | Opus | `IfcRaumkoerper` für `IfcWall`, `IfcSlab`, `IfcRoof`, `IfcWindow`, `IfcDoor`; `AbbildBauteil.Koerper`; Dreiecksgrenze gemeinsam mit Räumen | neue Probe `ifc4_koerper_bauteile.ifc`; `IfcRaumkoerperTests` um Bauteile |
| Rahmenanteil | Opus | `FractionOfFrame` → Rahmenanteil mit Herkunft `Ifc` | `IfcImportWelle2Tests` |
| Flächengruppen | Opus | Klasse `Flaechenklassifikation` in `Import/Gebaeude/`: gepaart/ungepaart aus `Koerpernachbarschaft`, Bauteilzuordnung über Raumbezug und Normale, Rückfälle, Bilanz je Gruppe, Gegenprobe 5 % | `FlaechenklassifikationTests` an `ifc4_koerper_nachbarn*.ifc` und der neuen Probe; Gegenprobe an den sechs Anwenderdateien als Diagnose |

### 6.4 HC-2 — Ansicht: Farbmodus Randbedingung

**Gebauter Stand (#746):** Umschalter, Legende, Grundrisskanten und Bodenschraffur, Dreiecksfarben je Gruppe in Geometriegruppen mit eigenem Material, R0 halbtransparent, Bauteilkörper in Gruppenfarbe und Klick sind umgesetzt; die Abweichungen vom Entwurf unten (Farbtafel als Konstante, Geometriegruppen statt Farbattribut, schematische Umrisse, neutrale Prismen) stehen im [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_HC-2_Farbmodus_Randbedingung.md).

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| DTO und Hülle | Opus | `GebaeudeAnsichtDaten`: Gruppenindex je Dreieck, Gruppenliste, Bauteilkörper; `GebaeudeImportAnsicht.cs` übersetzt | `GebaeudeImportAnsichtDateikoerperTests` erweitert |
| Grundriss | Opus | Kantenfarben, Schraffur, Legende; `GebaeudeAnsichtZeichnung.cs` | bunit `GebaeudeAnsichtTests`; Rasterprobe nicht nötig (kein `Raster`) |
| Körper | Opus | `epos-gebaeude-koerper.js`: Farbe je Dreieck (`BufferGeometry` mit Farbattribut), Transparenz R0, Bauteilnetze schaltbar, Klick meldet Gruppe | Probe 26/30 erweitert; Lizenzwache unverändert (kein neuer Fremdanteil) |
| Texte | Sonnet | Gruppen- und Legendentexte in beiden Sprachen, `ResourceDesigner` ziehen | `ResourceDesigner`-Prüfung grün |

### 6.5 HC-4 — „Datei erneut lesen“ im Gebäudedialog

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Kern und Hülle | Opus | `GebaeudeImportCtrl.LesenQuellen` liefert Dateiname und Hash; `GebaeudeHuelle`: Datei über `Dienste.Datei` wählen (Vorbelegung Dateiname), Hash prüfen, Abbild lesen, `GebaeudeAnsichtDaten` bauen; nichts schreiben | `GebaeudeHuelleTests` mit passendem und abweichendem Hash |
| Dialog | Opus | Knopf „Datei erneut lesen“ im Gebäudedialog (nur bei importiertem Gebäude), eingebettete `GebaeudeAnsicht` mit beiden Farbmodi, Hinweis bei abweichendem Hash | bunit `GebaeudeDialogImportTests` erweitert |
| Papiere | Sonnet | Datenaustauschkonzept 14 (Ort der Ansicht), Statuszeile, Protokoll, Wiki-Quelle des Gebäudedialogs | Link-Wache grün |


## 7. Fragen — alle entschieden (E87, 05.10.2026)

| Nr. | Frage | Entscheid |
|---|---|---|
| F1 | Welche Zonierung der Projektdatei ist die Vorgabe? | Wahl zwischen `ZoneType` 5 (DIN-V-18599-Zonen) und 6 (Simulationszonen), wenn beide vorhanden; **Vorgabe 5**, weil immer vorhanden. Wohneinheiten (2) nicht wählbar. Nachzug HC-3 |
| F2 | Profile, Kalender, Klima aus der Projektdatei? | **übernehmen, falls vorhanden** (Profile, Ganglinien, Kalender; wie E80 gebaut); kein Klimaimport (E80) |
| F3 | Geometrie dauerhaft speichern oder „Datei erneut lesen“? | **„Datei erneut lesen“** im Gebäudedialog (HC-4, 1 PT, kein Schema); Persistenz bleibt aus |
| F4 | Größengrenze der `.sqproj`? | **250 MB Windows, 100 MB iOS**, eigenständig von der IFC-Grenze; iOS misst wie G4-8 |
| F5 | Gruppenliste R0–R7 nach 4.1? | **ja**, mit Fenstern und Türen als eigener Gruppe R7 |


## 8. Risiken

- **GId-Abgleich schlägt fehl**, wenn die IFC aus einem anderen Speicherstand exportiert wurde
  als die Projektdatei (Räume gelöscht, neu angelegt). SQ fällt auf den Raumnamen je Geschoss
  zurück und meldet Resträume; ergänzend sollte der Dialog das Datum beider Dateien zeigen
  (`FILE_NAME` und `PrJournalEntry.StampEdit`).
- **Raumseitige Stücke ohne Körper** lassen Hüllflächen ohne passendes Bauteil zurück, wenn
  Orientierung und Lage nicht eindeutig sind (schiefe Wände, Erker). Gegenmaßnahme: Rückfall
  nach Normale mit Vermerk, Gegenprobe gegen die Mengensätze.
- **Dreiecksgrenze** mit Bauteilkörpern früher erreicht: Bauteilkörper werden erst
  geladen, wenn Räume unter zwei Dritteln der Grenze bleiben; sonst Hinweis und nur Räume.
- **iPad:** Transparenz und Farbattribute in WebGL sind unkritisch, die Sichtprobe 31 steht
  aber noch aus; HC-2 erhöht die Dringlichkeit, nicht den Umfang. Die 100-MB-Grenze der
  Projektdatei ist auf iOS bis zur Messung eine Annahme.
- **Zonierungswechsel nach Handarbeit:** Ein Wechsel „DIN-Zonen ↔ Simulationszonen“ baut den
  Zonenplan neu auf; ohne Rückfrage gingen Umhängungen verloren (E81 lässt leere Zonen ohnehin
  ungespeichert).
- **„Datei erneut lesen“ ohne Datei:** Liegt die Datei nicht mehr am Ort oder ist der Hash
  anders, gibt es keine Ansicht; der Dialog sagt das und bietet die Dateiwahl an. Das ist der
  bewusst gewählte Preis dafür, keine Geometrie zu speichern (F3).
- **Schemawechsel von HottCAD:** `XmTables.Version` wandert mit jeder Fassung; der Leser
  prüft und meldet, bricht aber nicht ab.


## 9. Abgrenzung

Kein web-ifc, kein IFC-Vollbetrachter, keine xBIM Geometry Engine, kein neues NuGet-Paket
(ADR-003 und Datenaustauschkonzept 15.6 bleiben). Kein Schreiben der `.sqproj`, kein Lesen
der Binärströme (`WDIN18599DataModel`, `.BDExit`, Grafikmodell), keine Übernahme von
Hersteller-, Adress- oder Kontaktdaten aus der Projektdatei, keine Anwenderdateien im
Repositorium, kein Klimaimport (E80), keine Persistenz der Geometrie (E87). Die Gruppenfarben
sind Anzeige und Gegenprobe, keine Rechengröße; die Einfrierregeln sind nicht berührt, weil
kein Referenzprojekt importiert wird.


## 10. Regeln, die gelten

Fachänderung nur im Kern; Dialoge als Razor mit Hülle in `EPOS.UI.Daten`, keine Datenbank in
der Oberfläche; Texte in beiden Sprachen mit `ResourceDesigner`; Dateiwahl allein über
`Dienste.Datei`; was iOS nicht kann, wird benannt abgelehnt; SQL mit `?`-Parametern;
Agentenaufträge mit `model: opus` für Umsetzung, `model: sonnet` für Texte, `AGENT_LAEUFT` im
Hauptbaum; Merge → Gate → Statuszeile und Protokoll → Push → Nachweis; iOS-Lauf nur nach
Rückfrage.
