# Konzept: HottCAD-Verbund — IFC zuerst, Projektdatei ergänzt, Hülle nach Randbedingung sichtbar (EPOS-Plan)

> **Entscheidstand:** Rev. 2 vom 05.10.2026. Ziel 3 (Raum → Zone aus der Projektdatei) ist mit
> den Wellen SQ-1 bis SQ-3 (#731, E80, Datenaustauschkonzept Nachtrag 3) bereits gebaut; dieses
> Papier beschreibt dazu den Nachzug nach **E87** (Wahl der Zonierung, Vorgabe
> DIN-V-18599-Zone, Größengrenze). **E87** entscheidet die Fragen F1 bis F5 (Kapitel 7); es ist
> nichts mehr offen. Umsetzung in den Wellen HC-1 bis HC-4 (Kapitel 6); **alle vier Wellen sind gebaut (HC-3 #736, HC-1 #740, HC-2 #746, HC-4 #754), offen bleibt nur 5.3; HC-5 (Grundriss je Raum aus dem Dateikörper, Kapitel 11) ist gebaut (#784).**

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

Bauteilaufbauten aus der Projektdatei (`TcBuildingElementDimension` mit Schichten), Zuordnungsstufen je
Bauteil und Farbmodus „Aufbau“: [Konzept Bauteilaufbau beim Gebäudeimport](2026-10-06_Konzept_Bauteilaufbau_Import.md).


## 6. Wellenzuschnitt

### 6.1 Reihenfolge und Maßstab

| Nr. | Welle | Inhalt in einem Satz | PT | Schema | Rückfrage vorher |
|---|---|---|---|---|---|
| 1 | **HC-3** Nachzug E87 an SQ | Wahl „DIN-Zonen \| Simulationszonen“ mit Vorgabe 5, Protokoll, Quelle im Zonenplan, Größengrenze 250/100 MB, Nachtrag 3 berichtigt | 0,5–1 | nein | keine |
| 2 | **HC-1** Kern: IFC vollenden und Flächen klassifizieren | zweiseitige Randbedingung, Bauteilkörper, Rahmenanteil; Flächengruppen R0–R7 je Raumkörper mit Bilanz — **HC-1 umgesetzt (#740)**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-05_HC-1_IFC_Randbedingung_Bauteilkoerper_Flaechen.md) | 2–3 | nein | keine |
| 3 | **HC-2** Ansicht: Farbmodus Randbedingung | Umschalter, Legende, 2D-Kanten und Schraffur, 3D-Dreiecksfarben und Bauteilkörper — **HC-2 umgesetzt (#746)**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_HC-2_Farbmodus_Randbedingung.md) | 1–2 | nein | keine |
| 4 | **HC-4** „Datei erneut lesen“ | Ansicht im Gebäudedialog aus der gespeicherten Importquelle, Hashprüfung, beide Farbmodi — **HC-4 umgesetzt (#754)**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_HC-4_Datei_erneut_lesen.md) | 1 | nein | keine |

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

**Gebauter Stand (#754):** Knopf, Dateiwahl, Prüfung, Ansicht in beiden Farbmodi und Hinweis bei abweichendem Hash sind umgesetzt, die Ansicht als eigene Überlagerung statt eingebettet; die vier Abweichungen vom Entwurf (Überlagerung nach Hausregel DL-2, kein Knopf im Assistenten, Dateiname im Titel des Wählers statt wörtlich vorbelegt, Dateidatum der Projektdatei nicht gezeigt) und die fehlende Laufanzeige beim ersten Lesen stehen im [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_HC-4_Datei_erneut_lesen.md).


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


## 11. Weg 1 — Grundriss je Raum aus dem Dateikörper (HC-5, gebaut #784)

> **Gebaut (#784) nach Freigabe der Fragen F6 bis F11 (E94); Abweichungen im [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_HC-5_Grundriss_aus_Dateikoerper.md).** E87 F3 gilt für die volle Geometrie weiter.

### 11.1 Anlass und Ziel

Eine HottCAD-IFC ohne Raumgrenzen (`IfcRelSpaceBoundary`) zeigt im Modus „Dateikörper“ das Gebäude richtig, im Modus
„Exportmodell“ aber jeden Raum als Rechteck aus Fläche und Seitenverhältnis, je Geschoss gereiht (Kennzeichen
„schematisch“). Ursache: `GebaeudeGrundriss.Bilden` findet keine Boden- oder Deckengrenze und fällt je Raum auf
`Zonengeometrie.Rechteck` zurück; `Zonenkoerper` baut nur über diesem Rechteck ein Prisma. Der Export
(`GbxmlSchreiber.Raumgeometrie`, von `IfcSchreiber` und `IfcKoerper` mitgenutzt) liest das Gebäude aus der Datenbank
(`GebaeudeExportAblauf`, ein `AbbildRaum` je Zone), in der keine Geometrie steht — er ist für jedes importierte
Gebäude schematisch.

**Ziel (Weg 1):** Beim Import wird aus jedem Raumkörper sein **Grundriss** abgeleitet und mit **Höhenlage** und
**Höhe** gespeichert. Exportmodell, IFC-Export und gbXML-Export zeigen dann je Raum ein Prisma über dem echten
Grundriss am richtigen Ort. Was ein Prisma nicht abbildet — Dachschrägen, Höhenversprünge, Galerien —, geht
benannt verloren.

**Abgrenzung.** E87 F3 wird **nur für den Grundriss** gelockert: Ringe, Boden, Höhe, Herleitung und Vermerke je
Raum, wenige Kilobyte je Gebäude. Die Dreiecksnetze bleiben ungespeichert; die Ansicht „Dateikörper“ außerhalb des
Imports bleibt beim Weg von HC-4 („Datei erneut lesen“). **Nicht beauftragt:** Weg 2 — beim Export die Importdatei
erneut lesen (Prüfsumme wie HC-4) und die Netze als `IfcTriangulatedFaceSet` schreiben — und Weg 3 — die Netze
speichern. Weg 2 bleibt ein möglicher späterer Zusatz für Dachschrägen im IFC-Export (F11); Weg 1 bleibt dann der
Rückfall, wenn die Datei nicht mehr vorliegt. Kein neues Paket, kein Geometriekern (ADR-003,
Datenaustauschkonzept 15.6).

### 11.2 Ableitung ohne Geometriekern

Eingang ist der `Dateikoerper` eines Raums (`AbbildRaum.Koerper`, `Raumumriss.Koerper`): Punkte in Weltkoordinaten
des Modellsystems [m], gerundet auf 1e‑6, Dreiecke mit Normale je Dreieck. Neue Klasse **`Koerpergrundriss`** in
`EPOS.Kern/Allgemein/Simulation/Gebaeude/` neben `Dateikoerper`; ohne Datenbank, ohne Oberfläche, wirft nicht. Der
Name `IfcRaumgrundriss` ist belegt (Profilring senkrechter Extrusionen, 11.5).

**Stufe 1 — Boden (`KoerperBoden`).**

1. **Orientierung prüfen:** Bei geschlossener Schale (kein Vermerk `Offen`) entscheidet das Vorzeichen des
   Volumens (Summe über die Dreiecke, Divergenzsatz), ob die Normalen nach außen zeigen; ist es negativ, gelten alle
   Normalen umgekehrt. Bei offener Schale gilt die Normale nicht als verlässlich: Bodendreiecke sind dann die
   waagerechten Dreiecke (|n_z| ≥ cos 10°) im untersten Band (höchstens 0,05 m über dem tiefsten Punkt).
2. **Bodendreiecke:** alle Dreiecke mit Außennormale nach unten innerhalb 10° (n_z ≤ −cos 10°), **auf jeder Höhe** —
   so trägt ein Raum mit Stufe seinen ganzen Fußabdruck. Liegen sie mehr als 0,05 m auseinander, Vermerk `Stufen`.
3. **Projektion:** x und y jedes Eckpunkts auf ganze Millimeter gerundet; Dreiecke, deren projizierte Fläche unter
   1e‑6 m² fällt, entfallen.
4. **T-Stöße auflösen:** Jede projizierte Kante wird an jedem projizierten Eckpunkt geteilt, der auf ihr liegt
   (Abstand ≤ 1 mm, dieselbe Regel wie der Ring der Datei in `Zonenkoerper`, Datenaustauschkonzept 5.5 Nr. 4).
5. **Kanten heben sich auf:** Je Teilkante zählt die Richtung; eine Kante, die gegenläufig gleich oft vorkommt, ist
   innen und fällt weg. Gleichläufige Doppel (überlappende Dreiecke) bleiben einmal, Vermerk `Ueberlappung`.
6. **Ringe bilden:** Die Randkanten werden zu geschlossenen Zügen verkettet; an einem Punkt mit mehr als zwei
   Randkanten wird die Kante mit der kleinsten Linksdrehung genommen, Start am kleinsten x, dann y — so ist das
   Ergebnis deterministisch (15.6 Nr. 5 des Datenaustauschkonzepts). Gegen den Uhrzeigersinn ist Außenring, im
   Uhrzeigersinn Loch; jedes Loch hängt am Außenring, der es enthält (Punkt-im-Polygon). Mehrere Außenringe je Raum
   sind zulässig.
7. **Vereinfachen:** Kollineare Punkte entfallen, wenn ihr Abstand zur Sehne der Nachbarn unter 1 mm liegt (Bögen mit
   32 Sehnen je Vollkreis bleiben ab rund 0,2 m Radius erhalten); Ringe unter **0,01 m²** entfallen mit Vermerk
   `Splitter`.

**Stufe 2 — Decke (`KoerperDecke`):** dieselbe Folge mit den Dreiecken nach oben, wenn Stufe 1 keinen Ring ergibt
(etwa ein Körper ohne Boden). **Stufe 3 — Hülle (`KoerperHuelle`):** die konvexe Hülle aller projizierten Punkte
(monotone Kette, deterministisch), wenn auch Stufe 2 nichts ergibt (verdrehte Netze, nur senkrechte Flächen);
Vermerk `Konvex`, Einbuchtungen gehen verloren. **Stufe 4 — wie heute:** ohne Dateikörper oder mit leerer Hülle das
Rechteck (`Wandflaechen`, `Seitenverhaeltnis`, `Quadrat`), Herkunft `Schematisch`.

**Neue Werte der vorhandenen Aufzählungen** (`Zonengeometrie.cs`; der Beleg heißt dort `Umrissherleitung`):
`Geometrieherkunft.Dateikoerper = 2` („Gestalt und Lage aus dem Raumkörper der Datei, als Prisma“) und
`Umrissherleitung.KoerperBoden = 6`, `KoerperDecke = 7`, `KoerperHuelle = 8`. Sprachneutrale Schlüssel, nie
Anzeigetext (Datenaustauschkonzept 14.4 Nr. 4).

**Maße je Raum.**

| Größe | Regel |
|---|---|
| Lage | Weltkoordinaten des Modellsystems wie Körper und Raumgrenzen (Datenaustauschkonzept 15.2, Zeile „Nordrichtung“); der Nordwinkel dreht nur die Azimute der Kanten, nie die Punkte |
| Höhenlage `Boden_m` | tiefster Punkt der Bodendreiecke, ohne Stufe 1 der tiefste Punkt des Körpers |
| Höhe `Hoehe_m` | Spanne des Körpers (höchster minus tiefster Punkt). Übersteigt sie V/A des Mengensatzes um mehr als 10 %, Vermerk `Dachschraege` (F9) |
| Geschoss | das Geschoss des Raums aus der Datei (`AbbildRaum.GeschossKennung`, Name und Lage); weicht `Boden_m` mehr als 0,5 m von der Geschosslage ab, Vermerk `Geschosslage` |
| Abgleich | Ringfläche (Außenringe minus Löcher) gegen `AbbildRaum.FlaecheM2` des Mengensatzes; über **10 %** Abweichung (F8) Vermerk `Flaeche` und eine Zeile im Importprotokoll (`IMP_GEB_PROT_GRUNDRISS_FLAECHE`, I). Gespeichert wird trotzdem; die Fläche der Rechnung bleibt die des Mengensatzes |
| Grenze | die Dreiecksgrenze von G7f-1 gilt schon beim Lesen; über ihr gibt es keinen Körper und damit Stufe 4 |

**Wo die Ableitung läuft:** in `GebaeudeGrundriss.Eingang` — ein Raum ohne Boden- und Deckengrenze, aber mit Körper,
bekommt den abgeleiteten Grundriss als `Umrissraum.Grundriss`; `Zonengeometrie.Bilden` nimmt ihn vor dem Rechteck.
So zeigt schon das Exportmodell des Importdialogs und von HC-4 den echten Grundriss, und gespeichert wird genau
dieses Ergebnis. Ein Raum mit Raumgrenzen bleibt bei ihnen (E73).

**Proben unter `Referenzlaeufe/Importproben/`** (Räume mit Körper, ohne Raumgrenzen):

| Probe | Erwartung |
|---|---|
| `ifc2x3_koerper_brep.ifc`, `ifc4_koerper_vieleckssatz.ifc`, `ifc4_koerper_dreiecksnetz.ifc` | trägt: `KoerperBoden`, ein Ring, Fläche gleich der Bodenfläche |
| `ifc4_koerper_extrusion_polygon.ifc` | trägt; Gegenprobe gegen `AbbildRaum.GrundrissM` (Profilring) auf 1 mm |
| `ifc4_koerper_extrusion_loch.ifc` | trägt: Außenring 6 m × 4 m mit einem Loch 2 m × 2 m, Fläche 20 m² |
| `ifc4_koerper_extrusion_bogen.ifc` | trägt: der Sehnenzug bleibt nach dem Vereinfachen erhalten |
| `ifc4_koerper_abgebildet.ifc`, `ifc4_koerper_platzierung.ifc` | trägt: Maßstab bzw. Placement-Kette (Drehung 90°, Geschoss 3 m höher, mm) — Lage und `Boden_m` richtig |
| `ifc4_koerper_beschnitt.ifc` | trägt mit Grenze: Grundriss richtig, Höhe aus dem ersten Operanden zu groß, Vermerk `Dachschraege` |
| `ifc4_koerper_offen.ifc` | trägt: Quader ohne Decke, Boden aus dem untersten Band |
| `ifc4_koerper_nachbarn.ifc`, `ifc4_koerper_bauteile.ifc` | tragen: mehrere Räume, zwei Geschosse; Probe für Kanten und Export (11.4) |
| `ifc4_koerper_nachbarn_grenzen.ifc` | Gegenprobe: Raumgrenzen gehen vor, kein `KoerperBoden` |
| `ifc4_koerper_advancedbrep.ifc`, `ifc4_z6_cad.ifc`, `ifc4_z6_sollwerte.ifc` | tragen nicht (kein Körper): Stufe 4 wie heute |

Neu, selbst erzeugt nach 8.3 des Datenaustauschkonzepts: `ifc4_koerper_grundriss_stufe.ifc` (L-Form mit Stufe 0,3 m,
Vermerk `Stufen`, Abweichung zum Mengensatz über 10 %) und `ifc4_koerper_grundriss_ohne_boden.ifc` (Schale ohne
Boden, `KoerperDecke`). Die sechs HottCAD-Dateien unter `Quellen/` laufen als Diagnose (`IfcQuelldateienDiagnoseTests`,
ohne Datei übersprungen): Herleitung je Raum, Zahl der Vermerke, Flächenabweichung je Gebäude.

### 11.3 Speicherung

**Neue Tabelle `Tab_Raumgrundriss`** — Kindliste der Importquelle, `STRICT`, `AUTOINCREMENT`, Bauform wie
`ImportzuordnungSchema`: eine DDL-Klasse `RaumgrundrissSchema` als einzige Quelle für Migration,
`Werkzeuge/Testdatenbankschema`, Testvorrichtung und Nachweis (ADR-001 Option C).

| Spalte | Typ | Regel |
|---|---|---|
| `ID` | INTEGER | Primärschlüssel, `AUTOINCREMENT` |
| `ID_Importquelle` | INTEGER NOT NULL | → `Tab_Importquelle.ID`, `ON DELETE CASCADE` |
| `ID_Zone` | INTEGER NULL | → `Tab_Zone.ID`, `ON DELETE SET NULL`; die Zone, der der Raum beim Import zugeordnet wurde; NULL = Raum ohne Zone (etwa unbeheizt unter Z5) |
| `Quellkennung` | TEXT NOT NULL | `GlobalId` des `IfcSpace` bzw. `id` des gbXML-`Space`, gekürzt nach `Quellkennung.Kuerzen`; `CHECK (length BETWEEN 1 AND 64)`; Wiedererkennung, keine Beziehung |
| `Raumname` | TEXT NULL | `CHECK (length <= 200)` |
| `Geschoss` | TEXT NULL | Name des Geschosses, `CHECK (length <= 200)` |
| `Geschoss_Lage_m` | REAL NULL | Höhenlage des Geschosses der Datei |
| `Boden_m` | REAL NOT NULL | Höhenlage des Bodens im Modellsystem |
| `Hoehe_m` | REAL NOT NULL | `CHECK (Hoehe_m > 0)` |
| `Ringe` | TEXT NOT NULL | Textform (unten); `CHECK (length BETWEEN 11 AND 65536 AND Ringe NOT GLOB '*[^0-9,;\|-]*')` |
| `Ringflaeche_m2` | REAL NOT NULL | `CHECK (Ringflaeche_m2 > 0)` |
| `Abweichung` | REAL NULL | (Ringfläche − Raumfläche) / Raumfläche; NULL ohne Raumfläche |
| `Herleitung` | TEXT NOT NULL | `CHECK (Herleitung IN ('KoerperBoden','KoerperDecke','KoerperHuelle','Boden','Decke'))` — die letzten beiden nur nach F6 |
| `Vermerke` | TEXT NULL | sprachneutrale Schlüssel mit Komma (`Stufen`, `Ueberlappung`, `Splitter`, `Konvex`, `Dachschraege`, `Geschosslage`, `Flaeche`); `CHECK (length <= 200)` |

Dazu `UNIQUE (ID_Importquelle, Quellkennung)` und der Index `idx_Raumgrundriss_Zone` über `ID_Zone` (Weg der
Löschregel). Kein `ID_Projekt` (W16 wie die Herkunftsablage), keine Boolean-Spalte, keine Beziehung über Text.

**Textform statt BLOB.** `Ringe` = Ringe getrennt durch `|`, Punkte durch `;`, je Punkt `x,y` in **ganzen
Millimetern, absolut im Modellsystem**, invariant, ohne Schlusspunkt; Außenring gegen den Uhrzeigersinn, seine Löcher
im Uhrzeigersinn dahinter. Gründe: lesbar mit jedem SQLite-Werkzeug, deterministisch und vergleichbar, ohne
Fassungskopf; ein BLOB spart rund 40 % von wenigen Kilobyte. Absolut statt relativ zu einem Bezugspunkt, weil ganze
Millimeter auch georeferenzierte Lagen (10⁶ m, zehn Stellen) exakt tragen und keine Spalte für den Bezugspunkt
nötig ist; den Bezugspunkt gegen den `float`-Verlust bildet wie heute erst die Ansicht (Datenaustauschkonzept 15.4).

**Größe.** Ein Raum mit acht Ecken: rund 110 Byte Ringe, mit Name, Kennung und Zahlen rund 300 Byte; ein Raum mit
Bögen (40 Punkte) rund 600 Byte. Das Gebäude mit 49 Räumen: **rund 15 KB**, bei sehr runden Grundrissen bis rund
30 KB.

**Schemaschritt.** Die nächste freie Nummer zum Zeitpunkt des Baus (Stand 06.10.2026: 190 angemeldet, 191 frei),
**vor dem Bau in der Zeile „Schemaschritt angemeldet“ im Kopf der Statusdatei anzumelden und sofort zu pushen**;
die Kette hängt über `+ 1` an der Vorgängerklasse. Ohne DML, ohne Saat; die Testdatenbank bekommt die leere Tabelle.

**Verhalten.**

| Fall | Regel |
|---|---|
| Import | `GebaeudeImportHerkunft` trägt die Grundrisse je Raum; `GebaeudeImportCtrl.SchreibeHerkunft` schreibt sie nach der Quelle **im selben Vorgang**, die Zone aus der Paarung Raum → Zone des Vorschlags (`GebaeudeZonenCtrl.VorschlagSchreiben`), nicht über Text |
| erneuter Import | Jeder Import legt eine neue Projektkopie mit eigener Quelle an; die Grundrisse hängen an ihr. Gelesen werden die Zeilen der jüngsten Quelle des Gebäudes (größte `ID`) |
| Gebäude löschen | Kaskade Gebäude → Importquelle → Grundriss |
| Zone löschen | `ID_Zone` wird NULL; der Raum bleibt für die Ansicht, der Export übergeht ihn |
| Projekt duplizieren | `ProjektDuplizierenCtrl`: Eintrag zweistufig wie `Tab_Importzuordnung` (`ID_Importquelle IN (SELECT ID FROM Tab_Importquelle WHERE ID_Gebaeude IN (…))`), `ID_Zone` über `FK_OVERRIDE` auf die Zonenkopie |
| Auslieferungsvorlage | `Prueflauf.ImportablageLeer` prüft die dritte Tabelle mit: leer, sonst Befund (Prüfung, kein stilles Leeren) |
| gbXML-Gebäude | Umriss aus dem `PolyLoop` der Boden- oder Deckenflächen; speichern mit Herleitung `Boden`/`Decke` **nach F6** |
| IFC mit Raumgrenzen | Umriss aus den Raumgrenzen; speichern wie gbXML **nach F6** |
| Katalog (`_STAMM`) | unberührt — die Herkunft hängt nur an Projektkopien |

### 11.4 Lesen und Wirkung

- **Export.** `GebaeudeExportAblauf` lädt die Grundrisse des Gebäudes (`RaumgrundrissCtrl.Lesen(idGebaeude)`) und
  hängt sie je Zone an deren `AbbildRaum` (eine Zone = die Grundrisse ihrer Räume, Mehrfachpolygon ohne Vereinigung,
  Datenaustauschkonzept 14.1). `GebaeudeGrundriss.Eingang` reicht sie als `Umrissraum.Grundriss` weiter;
  `Zonengeometrie.Bilden` nimmt sie **vor** `Rechteck`, Herkunft `Dateikoerper`. Trägt eine Zone nur für einen Teil
  ihrer Räume Grundrisse, steht der Rest als Rechteck daneben (Reihung wie heute neben Umrissen aus Raumgrenzen).
- **Körper.** `Zonenkoerper.Bilden` baut für `Dateikoerper` je Ring ein Prisma von `Boden_m` um `Hoehe_m` (heute nur
  über dem Rechteck); `Raumkoerper` trägt dazu Boden und Höhe je Ring statt je Raum. Die Wände der Zone
  (`Tab_Bauteil`) kommen auf die Kanten: Eine Kante mit gegenläufiger Kante eines anderen Raums (Abstand bis 0,8 m wie
  `Koerpernachbarschaft`, mit Überlappung) ist innen, wenn der Raum zur selben Zone gehört — dort keine Platte —,
  sonst Trennkante zu dessen Zone; alle übrigen sind Außenkanten. Jede Wandzeile verteilt sich über die Kanten ihrer
  Art (außen bzw. Trennkante zur Nachbarzone) und ihres Sektors nach Kantenlänge; die Plattenhöhe ist Fläche durch
  Kantenlänge, höchstens die Raumhöhe. Eine Zeile ohne passende Kante steht benannt in `OhneKante`.
- **IFC-Export** (`IfcKoerper`): je Ring ein `IfcExtrudedAreaSolid` über `IfcArbitraryClosedProfileDef` (mit Löchern
  `IfcArbitraryProfileDefWithVoids`) auf der Höhe `Boden_m`, alle Ringe einer Zone als Träger derselben Darstellung
  „Body“ ihres `IfcSpace`. Schräge Dächer werden nicht geschrieben.
- **gbXML-Export** (`GbxmlSchreiber`): `Space/ShellGeometry` aus allen Prismen der Zone, `Space/PlanarGeometry` aus dem
  größten Außenring (das Schema kennt dort nur einen `PolyLoop`), `Surface/PlanarGeometry` aus den Platten. Eine
  Trennfläche zwischen zwei Zonen liegt auf der Mittellinie ihrer beiden Gegenkanten.
- **Ansicht.** Exportmodell im Importdialog, in HC-4 und im Export zeigen dasselbe (Probe 27). Die Kennzeichenzeile
  des Exportmodells nennt „n aus Dateikörper“; `Koerperherkunft` bekommt den Wert `Grundriss`, Legende „aus
  Dateikörper (Grundriss)“, Texte in beiden `.resx`. **„schematisch“ entfällt für diese Räume**; die Exportmeldung
  `GEOMETRIE_SCHEMATISCH` zählt nur noch Rechtecke, eine neue Info nennt die Räume aus dem Grundriss.
- **Was nicht abgebildet wird** und wie es heißt: Dachschrägen und Giebel (Prisma bis zum First, Vermerk
  `Dachschraege`), Höhenversprünge im Boden (Prisma ab dem tiefsten Boden, `Stufen`), Galerien und Lufträume (ein
  Prisma über dem Fußabdruck), Wanddicken und Öffnungen als Ausschnitt (wie 4.4). Die Vermerke stehen in der
  Raumliste der Ansicht und je Zone in `Space/Description` bzw. `IfcSpace.Description`.

### 11.5 Rechnung und Basis

**Die Geometrie geht nicht in die Rechnung ein.** Geprüft:

- `Zonengeometrie`, `Zonenkoerper`, `Raumumriss` und `Dateikoerper` werden außer in ihren eigenen Dateien nur
  gelesen von `GebaeudeGrundriss` (Import), `GebaeudeNeulesen` (HC-4), `GbxmlSchreiber.Raumgeometrie`, `IfcSchreiber`,
  `IfcKoerper` (Export), `GebaeudeImportAnsicht`, `GebaeudeImportHuelle` (`EPOS.UI.Daten`) und den DTO in `EPOS.UI`.
  Kein Rechenweg — Gebäudemodell, Mehrzonenlauf, Solar, Verschattung, Wirtschaftlichkeit — und kein Referenzlauf
  verweist auf sie; die Simulation liest `Tab_Zone` und `Tab_Bauteil`.
- Der Dateikörper wirkt an **einer** Stelle auf geschriebene Werte: `Koerpernachbarschaft.Paare` →
  `GebaeudeBauteilvorschlag` (Trennflächen aus Körpern, G7f-4) beim Import. HC-5 ändert daran nichts.
- Der vorhandene Profilring `AbbildRaum.GrundrissM` (`IfcRaumgrundriss`) speist rechenwirksam
  `IfcAbbildBauer.FlaecheAusGrundriss` (Raumfläche ohne Mengensatz) und `GrundrissTrenndecken`. **Der neue Grundriss
  speist beides nicht** — er steht in eigenen Feldern; sonst änderten sich Zonenflächen und Trenndecken der Importe
  ohne Raumgrenzen. Ob er später den Profilring ersetzt, ist eine eigene Frage außerhalb von HC-5.
- `Tab_Raumgrundriss` lesen allein der Export und die Ansicht.

**Einfrierregeln: nicht berührt (nein).** Kein Referenzprojekt ist importiert; die Testdatenbank bekommt nur die
leere Tabelle. Erwartet ist der Referenzlauf **byte-gleich** gegen `2026-10-07_R42_Vorlaufinterpolation_AK3`.

### 11.6 Bestand

Gebäude, die vor HC-5 importiert wurden, haben keinen Grundriss und exportieren wie heute schematisch; die Ansicht
von HC-4 zeigt ihr Exportmodell nach dem Bau trotzdem richtig, weil sie aus der Datei ableitet. **Nachtragen (F7):**
In der Überlagerung „Importdatei erneut lesen“ von HC-4 erscheint nach passender Prüfsumme der Knopf „Grundriss
übernehmen“; nach Rückfrage schreibt er die Zeilen zur vorhandenen Quelle, die Zone über die vorhandenen Paarungen
in `Tab_Importzuordnung` (deren `ID_Zone`). Damit schreibt HC-4 erstmals — ausschließlich in `Tab_Raumgrundriss`.
Bei abweichender Prüfsumme kein Knopf.

### 11.7 Plattformen

Windows und iOS gleich: Ableitung, Speicherung und Export liegen im Kern; die Textform ist invariant. Die Größe ist
auf beiden Plattformen unkritisch (Kilobyte je Gebäude); die Dreiecksgrenze aus Datenaustauschkonzept 15.4 gilt
unverändert. Kein iOS-Lauf nötig (keine `Dienste.*`-Schnittstelle, kein Prüfmodus berührt).

### 11.8 Wellenzuschnitt HC-5

HC-5 ist gebaut (#784), Abweichungen vom Zuschnitt im [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_HC-5_Grundriss_aus_Dateikoerper.md).

| Teil | Agent | Inhalt | Abnahme | PT |
|---|---|---|---|---|
| Ableitung | Opus | `Koerpergrundriss` (Stufen 1–4, Vermerke, Abgleich), neue Werte der Aufzählungen, `Umrissraum.Grundriss`, `GebaeudeGrundriss.Eingang`, `Zonengeometrie.Bilden` | `KoerpergrundrissTests` (Kern ohne Datenbank) an den Proben aus 11.2 samt zwei neuen; Probe 25 (Determinismus) erweitert; Diagnose an `Quellen/` | 0,5 |
| Schema und Ablage | Opus | `RaumgrundrissSchema` (Schritt vorher angemeldet), Migration, `Testdatenbankschema`, `SchemaStand`; Schreiben in `SchreibeHerkunft`, `RaumgrundrissCtrl.Lesen`; Duplizieren; `Prueflauf` | `RaumgrundrissCtrlTests` (Kern mit Datenbank: schreiben, jüngste Quelle lesen, Kaskade beim Gebäude, NULL beim Löschen der Zone, Duplizieren), Duplizier- und Herkunftstests erweitert, `SqlDialektPruefer` ohne Fundstelle | 0,5 |
| Export | Opus | `GebaeudeExportAblauf` lädt, `Zonenkoerper` Prisma je Ring und Kantenzuordnung, `IfcKoerper` Profil mit Löchern, `GbxmlSchreiber` Shell und Planar | Rundlauf IFC (Export, dann liest `IfcRaumkoerper` das Prisma zurück; Probe 28 erweitert), gbXML-`PolyLoop` gegen das Modell (Probe 27 erweitert), an `ifc4_koerper_nachbarn.ifc` | 0,6 |
| Ansicht und Texte | Opus, Sonnet | `Koerperherkunft.Grundriss`, Kennzeichenzeile, Legende, Exportinfo; Texte beider Sprachen, `ResourceDesigner` | bunit `GebaeudeAnsichtTests` (Proben 26 und 30 erweitert), Designerprüfung grün | 0,2 |
| Papiere | Sonnet | Datenaustauschkonzept 14.1, 15.1 und 15.3 („Die Exporte bleiben unverändert“) und dieses Kapitel als gebaut, Statuszeile, Protokoll, Wiki-Quellen „Gebäudeimport“ und „Gebäudeexport“, Logbuch-Entwurf | Link-Wache grün | 0,2 |
| | | | **zusammen** | **2,0** |

Mit F7 (Nachtragen) kommen rund 0,3 PT dazu (`GebaeudeNeulesen`, Hülle, Knopf; `GebaeudeHuelleTests`, bunit
`GebaeudeDialogImportTests`; Windows-Schale auf Linux kompilieren). Jeder Teil baut den Kern-Filter, läuft seine
Tests mit `--filter` und committet sofort; das Gate fährt die Orchestrierung einmal nach dem Merge, mit Referenzlauf
(byte-gleich erwartet). Sichtabnahme unter Windows (Exportmodell der HottCAD-Datei mit 49 Räumen, IFC-Export in
einem fremden Betrachter); kein iOS-Lauf.

### 11.9 Fragen an den Anwender — entschieden (E94, 06.10.2026)

Alle Empfehlungen sind übernommen; die Spalte „Empfehlung“ ist damit der Entscheid.

| Nr. | Frage | Empfehlung |
|---|---|---|
| F6 | Den Umriss auch für Gebäude aus gbXML und aus IFC mit Raumgrenzen speichern? | **ja** — ihr Export ist heute ebenso schematisch; Herleitung `Boden`/`Decke`, kaum Mehraufwand |
| F7 | Bestand über „Datei erneut lesen“ nachtragen (HC-4 schreibt dann den Grundriss)? | **ja**, als ausdrücklicher Knopf mit Rückfrage, nur bei passender Prüfsumme (+0,3 PT) |
| F8 | Schwelle der Flächenabweichung zwischen Ring und Mengensatz? | **10 %** als Hinweis, wie die Rechteckprobe (Datenaustauschkonzept 14.1 Nr. 2); gespeichert wird trotzdem |
| F9 | Höhe des Prismas: Spanne des Körpers oder V/A des Mengensatzes? | **Spanne**, damit das Prisma den Körper umschließt; Vermerk `Dachschraege` ab 10 % über V/A |
| F10 | Export je Zone (die Prismen ihrer Räume in einem Raum der Datei) oder je importiertem Raum? | **je Zone** — das Datenmodell führt Bauteile je Zone; je Raum erst auf Zuruf |
| F11 | Weg 2 (Netze beim Export aus der erneut gelesenen Datei) später als Zusatz? | **zurückstellen**, bis ein Abnehmer Dachschrägen im IFC braucht; Weg 1 bleibt der Rückfall ohne Datei |

### 11.10 Risiken

- **Fremde Netze:** falsch orientierte, offene oder überlappende Schalen — Volumenvorzeichen, unteres Band und
  Vermerke fangen sie; nichts wird repariert (Datenaustauschkonzept 15.6 Nr. 2).
- **Kantenzuordnung:** Wanddicken trennen Nachbarräume um bis zu 0,8 m; eine Gegenkante kann fehlen oder falsch
  greifen. Gegenmaßnahme: benannt in `OhneKante`, Gegenprobe der Plattenflächen gegen `Tab_Bauteil`.
- **gbXML-Abnehmer** erwarten kantenschlüssige Flächen (1-mm-Regel, IES VE); echte Grundrisse mit Wanddicke sind es
  zwischen Zonen nicht. Mittellinie der Trennfläche; Sichtproben 2 und 16 mit dem Bau wiederholen.
- **Prismenhöhe** bei Dächern ohne Beschnitt zu groß; benannt, nicht gerechnet.
- **Schemaschritt-Kollision** mit parallelen Sitzungen: Anmeldung vor dem Bau, Kette über `+ 1`.
- **Bestand** bleibt ohne F7 schematisch; das Exportmodell von HC-4 weicht dann vom Export ab — benannt in der
  Exportmeldung, bis nachgetragen ist.
