# Konzept: HottCAD-Verbund — IFC zuerst, Projektdatei ergänzt, Hülle nach Randbedingung sichtbar (EPOS-Plan)

> **Entscheidstand:** Entwurf vom 05.10.2026 zum Anwenderauftrag vom selben Tag. Offen sind
> die Fragen F1 bis F5 in Kapitel 7. Umsetzung in den Wellen HC-1 bis HC-6 (Kapitel 6);
> HC-5 und HC-6 nur nach Entscheid.

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
Herkunft je Feld, Plattformweg), 14 (Körperansicht) und 15 (Raumkörper, G7f),
[ADR-003](../ADR-003_IFC_xBIM_ohne_Geometriekernel.md) (xBIM ohne Geometriekernel), die Entscheide
E72, E73 und E79 der [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md), der
[Plan G7b bis KU3](2026-10-04_Plan_G7b_bis_KU3.md) als Muster des Wellenzuschnitts. Code:
`EPOS.Kern/Allgemein/Import/Ifc/`, `EPOS.Kern/Allgemein/Import/Gebaeude/`,
`EPOS.UI/Bausteine/GebaeudeAnsicht.razor`, `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs`.


## 0. Das Ergebnis in sechs Punkten

1. **Ziel 2 ist weitgehend erreicht und wird vollendet.** Der Kern liest die HottCAD-IFC schon
   bis zu Raumkörpern, Raumbezügen, Beheizung, Solltemperatur, Raumtyp, Bauteil-U-Werten und
   Aufbauten. Es fehlen drei Dinge, die die Datei hergibt: die **zweiseitige Randbedingung**
   je Bauteil (`HSETU_Bauteilreferenzen.ElementReferences[0|1].AdjacentType`), die
   **Körper der Hüllbauteile** (`IfcShellBasedSurfaceModel` an Außenwänden, Dächern,
   Fenstern, Bodenplatten) und die **Fensterwerte** Rahmenanteil und g-Wert, soweit vorhanden.
2. **Ziel 4 kommt ohne Projektdatei aus.** Die Randbedingungscodes `btaOutside`, `btaGround`,
   `btaHeated`, `btaUnHeated`, `btaCellarCeiling`, `btaUppermostStorey` stehen an jedem
   Bauteil der IFC und ergeben die gewünschten Gruppen unmittelbar. Darstellen lässt sich jede
   Gruppe über die **Flächen der Raumkörper**: Eine Fläche mit Gegenfläche eines anderen Raums
   ist eine Trennfläche (Gruppe nach Beheizung des Nachbarn), eine Fläche ohne Gegenfläche ist
   Hülle (Gruppe nach dem Bauteil, das der Raumbezug nennt). Das verlängert G7f-4
   (Körpernachbarschaft) um die Hüllseite; Hüllbauteile mit eigenem Körper werden zusätzlich
   als Körper gezeichnet.
3. **Der Viewer existiert und wird um einen Farbmodus erweitert.** `GebaeudeAnsicht.razor`
   zeichnet Grundriss (SVG) und Körper (three.js, lokal, MIT) im Importdialog. Neu ist der
   Umschalter **„Zonen | Randbedingung“** mit Legende der Gruppen, in 2D als gefärbte
   Raumkanten und Bodenflächen, in 3D als gefärbte Dreiecke der Raumkörper und Bauteilkörper.
4. **Ziel 3 wird eine Ergänzungsquelle, keine zweite Importart.** Die IFC bleibt Primärdatei.
   Dazu kann der Anwender die zugehörige `.sqproj` wählen; sie liefert eine neue Zonierungsregel
   **Z7 „nach Projektdatei“** (Simulationszonen `ZoneType` 6, wahlweise DIN-18599-Zonen 5 oder
   Wohneinheiten 2) über `BmZoneReference`. Der Abgleich der Räume läuft über **`GId` =
   dekodierte IFC-`GlobalId`**, ohne Namensabgleich. Der Leser nutzt `Microsoft.Data.Sqlite`,
   das der Kern schon hat, öffnet nur lesend, prüft `XmTables.Version` und meldet jede
   Abweichung benannt (Räume ohne Treffer, Zonen ohne Räume).
5. **Nutzungsprofile, Ganglinien, Kalender und Klima aus der `.sqproj`** sind lesbar und
   wertvoll, aber eine eigene, spätere Welle (E79: „DIN-V-18599-Profile als spätere Welle“).
   Sie brauchen die Abbildung auf die Konditionierungsvorlagen und Zonenkalender des Kerns und
   damit einen eigenen Entscheid.
6. **Aufwand:** HC-1 bis HC-4 zusammen 7 bis 11 PT in vier Wellen mit je einer Abnahme; HC-5
   (Profile) 3 bis 4 PT und HC-6 (Geometrie dauerhaft speichern, Viewer außerhalb des Imports)
   3 bis 5 PT nur nach Entscheid. Zwei Rückfragen an Stufengrenzen (F1 vor HC-3, F3 vor HC-6).
   Kein neues Paket, kein Geometriekernel, kein web-ifc.


## 1. Einordnung

### 1.1 Was die drei Ziele vom Bestand verlangen

| Ziel | Heute | Lücke |
|---|---|---|
| (2) IFC ausreizen | `IfcAbbildBauer` liest Struktur, Räume, Raumkörper (G7f-1), Raumbezüge, Beheizung und Sollwert (`HSETU_RaumAllgemein`), Raumtyp (`mrt…`), Bauteile mit U-Wert, einseitige Randbedingung (`HSETU_BauteilAllgemein.AdjacentType`), Aufbauten | zweiseitige Randbedingung, Bauteilkörper, Fensterwerte (`HSETU_EcoCad.FractionOfFrame`, `Pset_DoorWindowGlazingType`) |
| (3) Raum → Zone aus `.sqproj` | Zonierung nur aus IFC (Z1–Z6, Umhängen von Hand); E79 beschließt den Zonenbaum ZB-1/ZB-2 | Leser für `BmZoneReference`, Regel Z7, Abgleich über `GId`, Persistenz der zweiten Quelle |
| (4) Geometrie 2D/3D mit Gruppen | Grundriss und Körperansicht im Importdialog, Zonenfarben, Trennflächen aus Raumkörpern (G7f-4) | Klassifikation **aller** Flächen nach Randbedingung, Farbmodus, Legende, Bauteilkörper |

### 1.2 Was dieses Papier an den Nachbarpapieren ändert

- **Mehrzonenkonzept 6.1:** eine siebte Zonierungsregel Z7 „nach Projektdatei“, Rang hinter Z1
  (Zonen der IFC selbst) und vor Z2; wählbar nur, wenn eine `.sqproj` geladen ist und mindestens
  ein Raum trifft.
- **Datenaustauschkonzept 2.3:** eine Importquelle kann eine **Ergänzungsdatei** tragen
  (Format `SQPROJ`), mit eigenem Dateinamen und SHA-256; die Paarung Raum ↔ Quellobjekt
  bleibt eine Zeile je Zuordnung.
- **Datenaustauschkonzept 15.1:** die Körperansicht zeigt weiterhin keine Dicken und keine
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
| Zonierung | `Gebaeude/GebaeudeZonierung.cs` | Z1–Z6 und X1–X4, M7 Vorgabe, M8 Mindestgröße, Umhängen von Hand, Pflegegrenze 50 Zonen |
| Herkunft | `Gebaeude/Importherkunft.cs`, `GebaeudeFeldzeile.cs` | je Feld Herkunft und Beleg; persistiert `Tab_Zone.Herkunft`, `Tab_Bauteil.Herkunft`, `Tab_Importquelle`, `Tab_Importzuordnung` |
| Ansicht | `EPOS.UI/Bausteine/GebaeudeAnsicht.razor`, `GebaeudeAnsichtZeichnung.cs`, `wwwroot/epos-gebaeude-koerper.js`, `wwwroot/three/` | Reiter „Grundriss \| Körper“, Zonenfarben, „Dateikörper \| Exportmodell“, Klick meldet Raum und Zone; three.js 0.186.1 lokal mit Lizenzwache |
| Datenseite | `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs`, `GebaeudeImportAnsicht.cs`, `GebaeudeImportZonen.cs` | Dateiwahl über `Dienste.Datei`, Größengrenze, Lesen im Arbeitsfaden, DTO der Ansicht |
| Dialog | `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor` | Regelliste, Zonenliste, Bauteile, Baustoffabgleich, eingebettete Ansicht |
| Proben | `Referenzlaeufe/Importproben/`, `EPOS.Kern.Tests/IfcProbenErzeuger*.cs`, `Quellen/*.ifc` | über 30 synthetische IFC-Proben, sechs HottCAD-Anwenderdateien |

Nicht vorhanden: ein `.sqproj`-Leser, der Zonenbaum ZB-1/ZB-2, eine Klassifikation der
Hüllflächen, Bauteilkörper, eine Persistenz von Räumen, Geschossen oder Geometrie.


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

### 3.3 Fensterwerte

`HSETU_EcoCad.FractionOfFrame` steht an allen Fenstern, `Pset_DoorWindowGlazingType` wird
schon gelesen, ein g-Wert fehlt in den HottCAD-Dateien. Neu: `FractionOfFrame` als Beleg für
`Tab_Bauteil.Rahmenanteil` mit Herkunft `Ifc`; der g-Wert bleibt Vorgabe mit Beleg „nicht in
der Datei“. Kein weiterer Aufwand.

### 3.4 Was die IFC nicht hergibt (bleibt wie heute)

Zonen (nur Z4/Z6-Herleitung), Nutzungsprofile, Tagesganglinien, Kalender, Klimareihen,
Schichtaufbauten der Innenbauteile. Das ist der Gegenstand von Kapitel 5.


## 4. Ziel 4 — Flächenklassifikation und Darstellung nach Randbedingung

### 4.1 Die Gruppen

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

### 4.4 Grenzen

Keine Wanddicken, keine Öffnungen als Ausschnitt, keine Verschattungskörper
(`IfcBuildingElementProxy` ohne Körper), keine Dachfenster ohne Körper. Ein Gebäude über der
Dreiecksgrenze fällt wie heute benannt auf Prismen zurück. Die Gruppen sind Anzeige und
Gegenprobe, **keine Rechengröße**: Die Simulation rechnet weiter mit den Bauteilen und
Randbedingungen aus Mengensatz und Zuordnung.


## 5. Ziel 3 — die Projektdatei als Ergänzungsquelle

### 5.1 Grundsatz

Die IFC bleibt die Importdatei. Die `.sqproj` ist eine **Ergänzungsdatei**, die der Anwender
im Importdialog zusätzlich wählt („Projektdatei hinzuladen“). Sie wird nur gelesen, nie
geschrieben, und liefert in der ersten Stufe genau eines: die Zonierung. Ohne Ergänzungsdatei
läuft der Import wie heute.

### 5.2 Leser

- **Öffnen:** `Microsoft.Data.Sqlite` (im Kern vorhanden), Verbindung `Mode=ReadOnly`, kein
  Journal, kein Schreiben; die Datei wird vor dem Öffnen auf die SQLite-Signatur und die
  Größengrenze geprüft (Windows 250 MB, iOS 100 MB, F4). Auf iOS liegt die gewählte Datei als
  Kopie im Sandkasten (`Dienste.Datei.DateiOeffnenAsync` mit Filter `*.sqproj`, UTI
  `public.data`); der Leser liest nur die benötigten Tabellen.
- **Fassungsprüfung:** `XmTables` muss vorhanden sein und die Tabellen `BmRoom`, `BmZone`,
  `BmZoneReference` in Fassung 15.1 bis 17.x führen; `PrProject.ProgName` wird protokolliert.
  Unbekannte Fassung → Meldung `IMP_SQPROJ_PROT_FASSUNG`, Lesen wird trotzdem versucht.
- **Gelesen werden:** `BmRoom` (`UUID`, `GId`, `ShortDesc`, `FloorUUID`, `HeatingType`,
  `InsideTemperature`), `BmZone` (`UUID`, `ZoneType`, `ShortDesc`, `ProfileGroupUUID`,
  `HeatingType`), `BmZoneReference` (`UUID` = Zone, `ReferenceToUUID` = Raum), zur Benennung
  der Zone optional `PdProfileReference` → `PdProfile` → `PdProfileUsage.ProfileUsageType`
  (DIN-V-18599-Profilnummer). Nichts weiter.
- **Abgleich:** Für jeden Raum des IFC-Abbilds wird die `GlobalId` in die GUID dekodiert
  (Base64-Zeichensatz `0–9 A–Z a–z _ $`, 22 Zeichen → 128 Bit) und gegen `BmRoom.GId`
  gesucht (Klammern und Groß-/Kleinschreibung normiert). Ergebnis je Raum: getroffen oder
  nicht. Bilanz: getroffene Räume, nicht getroffene Räume, Zonen ohne getroffenen Raum.
  Unter 100 % Treffer wird der Rest nach der gewählten Rückfallregel (Z4 oder Z6) zugeordnet
  und als `IMP_SQPROJ_PROT_RAUM_OHNE_TREFFER` je Raum gemeldet.
- **Zonierungsregel Z7 „nach Projektdatei“:** eine EPOS-Zone je `BmZone` des gewählten
  `ZoneType` (Vorgabe 6 Simulationszone, wählbar 5 DIN-18599-Zone, 2 Wohneinheit; F1), Name
  aus `ShortDesc` der Zone oder, wenn leer, aus der Profilnummer („Zone 3 – Profil 2“);
  Beheizung der Zone aus der Mehrheit ihrer Räume (B-Regeln des Mehrzonenkonzepts bleiben).
  M8 (Mindestgröße) gilt auch hier; M12 (Pflegegrenze 50 Zonen) ebenso.
- **SQL-Texte:** Die Abfragen stehen in einer Klasse `SqprojLeser` mit `?`-Parametern. Der
  `SqlDialektPruefer` hält Bestandstexte gegen die Testdatenbank; die `.sqproj`-Abfragen
  gehören nicht dorthin und werden in der Prüferkonfiguration als eigener Satz gegen eine
  **synthetische Probe** geprüft (5.5), nicht gegen `Kenndaten_Test.sqlite`.

### 5.3 Herkunft und Persistenz

- `Tab_Importquelle` bekommt eine zweite Zeile je Import mit `Format = 'SQPROJ'`, Dateiname
  (nie Pfad), SHA-256, Größe, `Schemastand` (= höchste `XmTables.Version`),
  `Programmfassung` (= `PrProject.ProgVersion`), `Zonenregel = 'Z7'` und einem Verweis auf die
  Primärquelle (`ID_Primaerquelle`). Das `CHECK` der Spalte `Format` kennt heute nur `IFC`
  und `GBXML` → **ein Schemaschritt** (Nummer bei Baubeginn anmelden, Kette über `+ 1`).
- `Tab_Importzuordnung` bekommt je Raum ↔ `BmRoom.UUID` und je Zone ↔ `BmZone.UUID` eine
  Zeile mit `Quelltyp` `SQPROJ_RAUM` bzw. `SQPROJ_ZONE`.
- Herkunft je Feld: Die Zonenbildung trägt `Importherkunft.Ifc` mit Beleg „Zonierung aus
  Projektdatei (Z7)“; ein neuer Enum-Wert ist nicht nötig, weil kein Fachwert aus der
  `.sqproj` in ein Rechenfeld fließt. Erst HC-5 (Profile) bräuchte den Wert `Sqproj` in
  `Importherkunft` und `WERTE_HERKUNFT`, dann mit eigenem Schemaschritt.

### 5.4 Verhältnis zum Zonenbaum (E79, ZB-1/ZB-2)

Z7 ist der **Startvorschlag**, den der Zonenbaum anzeigt; der Anwender kann danach wie
beschlossen Räume umhängen, Zonen anlegen und löschen. „Nicht zugeordnete Räume“ sind die
Räume ohne Treffer. ZB-1 muss daher die Zonierung als änderbare Liste halten, aus der Z7 nur
den Anfangszustand liefert; ZB-2 zeigt die Quelle („aus Projektdatei“, „nach Regel“, „von
Hand“) je Zone in der Baumzeile. HC-3 und HC-4 setzen auf ZB-1 auf; läuft ZB-1 noch, bauen
sie gegen die heutige `Raumumhaengung` und werden beim Merge angepasst.

### 5.5 Proben und Tests ohne Anwenderdaten

Die `.sqproj`-Dateien tragen Adressen, Kontakte und Herstellerdaten und bleiben außerhalb des
Repositoriums (CLAUDE.md, Wiki-Regel sinngemäß). Getestet wird mit:

- einem **`SqprojProbenErzeuger`** in `EPOS.Kern.Tests`, der mit `Microsoft.Data.Sqlite` eine
  Minimaldatei schreibt (`XmTables` mit Fassungen, `PrProject`, `BmBuilding`, `BmFloor`,
  `BmRoom`, `BmZone`, `BmZoneReference`, optional `PdProfile*`), passend zu einer vorhandenen
  IFC-Probe (`ifc4_z6_cad.ifc`) über identische GUIDs; Byte-Gleichheit als Messlatte wie bei
  den IFC-Proben, Ablage unter `Referenzlaeufe/Importproben/sqproj_*.sqproj`;
- Probenfälle: voller Treffer, Teiltreffer mit Rückfall, Zone ohne Räume, unbekannte Fassung,
  falsche Datei (kein SQLite), `ZoneType` 5 und 2 statt 6, leerer `ShortDesc`;
- einer **Diagnose** wie `IfcQuelldateienDiagnoseTests`, die gegen `C:\Temp\IFC_Ganglinie_Beispiele`
  läuft, wenn der Ordner existiert, und sonst übersprungen wird (nur Zählungen im Protokoll,
  keine Werte).

### 5.6 Später: Profile, Kalender, Klima (HC-5, nach Entscheid)

Die `.sqproj` liefert je Simulationszone zehn Tagesganglinien (Heizen, Kühlen, Personen,
Geräte, Beleuchtung, Lüftung, Elektro, Trinkwasser, Feuchte, Sonnenschutz), die
DIN-V-18599-Profilnummer und einen Klimasatz mit neun Stundenreihen. Die Übernahme braucht
drei Abbildungen, die ein eigener Entscheid festlegt: Profilnummer → Konditionierungsvorlage
(Wohnen, Büro, Schule, keine) oder neue Vorlage je Profil; Heiz-/Kühlganglinie →
Zonenkalender mit Tag-/Nachtsollwert (`Raumsolltemperatur_Tag`, `_Nachtabsenkung`); Klimasatz
→ Klimatabelle des Projekts (nur mit Herkunftsvermerk, Einfrierregeln beachten, kein
Referenzprojekt). Aufwand 3 bis 4 PT; Nutzen: die Nutzung muss nicht nachgetippt werden.


## 6. Wellenzuschnitt

### 6.1 Reihenfolge und Maßstab

| Nr. | Welle | Inhalt in einem Satz | PT | Schema | Rückfrage vorher |
|---|---|---|---|---|---|
| 1 | **HC-1** Kern: IFC vollenden und Flächen klassifizieren | zweiseitige Randbedingung, Bauteilkörper, Fensterwerte; Flächengruppen je Raumkörper mit Bilanz | 2–3 | nein | keine |
| 2 | **HC-2** Ansicht: Farbmodus Randbedingung | Umschalter, Legende, 2D-Kanten und Schraffur, 3D-Dreiecksfarben und Bauteilkörper | 1–2 | nein | keine (Farbtoken nach Hausblatt) |
| 3 | **HC-3** Kern: `.sqproj`-Leser und Z7 | Leser, Fassungsprüfung, GId-Abgleich, Bilanz, Regel Z7, Protokollmeldungen, Probenerzeuger | 2–3 | nein | **F1** (ZoneType-Vorgabe), F4 (Grenzen) |
| 4 | **HC-4** Dialog und Persistenz | „Projektdatei hinzuladen“, Bilanzanzeige, Z7 in der Regelliste, Quelle und Paarungen speichern | 2–3 | **ja** (Format `SQPROJ`, `ID_Primaerquelle`) | keine |
| 5 | **HC-5** Profile, Kalender, Klima aus der Projektdatei | nach eigenem Entscheid (5.6) | 3–4 | ja | **F2** |
| 6 | **HC-6** Geometrie dauerhaft, Viewer außerhalb des Imports | Raumkörper und Flächengruppen je Gebäude speichern, Ansicht im Gebäude- und Zonendialog | 3–5 | ja | **F3** |

Maßstab wie im Plan G7b bis KU3: 1 PT ≈ 0,8–1 Punkt des Wochenkontingents; HC-1 bis HC-4
zusammen rund 7–10 Punkte. Jede Welle endet mit Build, Tests mit `--filter`, Proben, einem
Commit auf dem Arbeitszweig und dem Gate nach dem Merge; Statuszeile und Protokoll wie
gewohnt. Kein iOS-Lauf ohne Rückfrage; HC-2 und HC-4 brauchen die Sichtabnahme auf Windows,
die iPad-Sichtprobe hängt an der offenen Probe 31 aus G7f-2.

### 6.2 HC-1 — Kern: IFC vollenden und Flächen klassifizieren

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Randbedingung | Opus | `IfcAbbildBauer.Bauteil()`: beide `ElementReferences[i].AdjacentType` und `Orientation` lesen; `ANGRENZUNG_ABBILDUNG` um `btaCellarCeiling`, `btaUppermostStorey`, `btaNone`; `AbbildBauteil.RandbedingungSeiteA/B`, wirksame Randbedingung | `IfcCadBauteileTests` erweitert; Probe `ifc4_z6_cad.ifc` um zweiseitige Sätze ergänzt; `IfcQuelldateienDiagnoseTests` zählt Gruppen an den sechs Dateien unter `Quellen/` |
| Bauteilkörper | Opus | `IfcRaumkoerper` für `IfcWall`, `IfcSlab`, `IfcRoof`, `IfcWindow`, `IfcDoor`; `AbbildBauteil.Koerper`; Dreiecksgrenze gemeinsam mit Räumen | neue Probe `ifc4_koerper_bauteile.ifc`; `IfcRaumkoerperTests` um Bauteile |
| Fensterwerte | Opus | `FractionOfFrame` → Rahmenanteil mit Herkunft `Ifc` | `IfcImportWelle2Tests` |
| Flächengruppen | Opus | Klasse `Flaechenklassifikation` in `Import/Gebaeude/`: gepaart/ungepaart aus `Koerpernachbarschaft`, Bauteilzuordnung über Raumbezug und Normale, Rückfälle, Bilanz je Gruppe, Gegenprobe 5 % | `FlaechenklassifikationTests` an `ifc4_koerper_nachbarn*.ifc` und der neuen Probe; Gegenprobe an den sechs Anwenderdateien als Diagnose |

### 6.3 HC-2 — Ansicht: Farbmodus Randbedingung

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| DTO und Hülle | Opus | `GebaeudeAnsichtDaten`: Gruppenindex je Dreieck, Gruppenliste, Bauteilkörper; `GebaeudeImportAnsicht.cs` übersetzt | `GebaeudeImportAnsichtDateikoerperTests` erweitert |
| Grundriss | Opus | Kantenfarben, Schraffur, Legende; `GebaeudeAnsichtZeichnung.cs` | bunit `GebaeudeAnsichtTests`; Rasterprobe nicht nötig (kein `Raster`) |
| Körper | Opus | `epos-gebaeude-koerper.js`: Farbe je Dreieck (`BufferGeometry` mit Farbattribut), Transparenz R0, Bauteilnetze schaltbar, Klick meldet Gruppe | Probe 26/30 erweitert; Lizenzwache unverändert (kein neuer Fremdanteil) |
| Texte | Sonnet | Gruppen- und Legendentexte in beiden Sprachen, `ResourceDesigner` ziehen | `ResourceDesigner`-Prüfung grün |

### 6.4 HC-3 — Kern: `.sqproj`-Leser und Z7

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Leser | Opus | `Import/Sqproj/SqprojLeser.cs`, `SqprojErgaenzung.cs` (Ergebnis: Zonen, Raumzuordnung, Bilanz, Protokoll); ReadOnly, Signatur- und Größenprüfung, Fassungsprüfung | `SqprojLeserTests` an synthetischen Proben |
| GId-Abgleich | Opus | `IfcGlobalIdKodierung` (22 Zeichen ↔ GUID), Normierung, Treffer je Raum | Hin- und Rückkodierung an bekannten Paaren; 20/20 am Wohngebäude in der Diagnose |
| Regel Z7 | Opus | `GebaeudeZonierung`: Regel Z7, Wählbarkeit, Vorgabe `ZoneType` 6, Name, Beheizung, M8/M12 | `GebaeudeZonierungTests`, `ZonierungZ7Tests` |
| Proben | Opus | `SqprojProbenErzeuger`, Proben unter `Referenzlaeufe/Importproben/`, LIESMICH fortgeschrieben; Diagnose gegen `C:\Temp\…` wenn vorhanden | Byte-Gleichheit der Proben; `SqlDialektPruefer` mit eigenem Satz für die `.sqproj`-Abfragen |

### 6.5 HC-4 — Dialog und Persistenz

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Schema | Opus | Schemaschritt: `Tab_Importquelle.Format` um `SQPROJ`, Spalte `ID_Primaerquelle`; `Quelltyp`-Werte | Migrationstest, `SqlDialektPruefer`, Testdatenbank unverändert (kein Referenzprojekt importiert) |
| Hülle | Opus | `GebaeudeImportHuelle`: zweite Dateiwahl, Größengrenze je Plattform, Lesen im Arbeitsfaden, Bilanz-DTO; iOS-Filter `.sqproj` in `Dateifilter.cs` | `GebaeudeImportZonenHuelleTests`; Windows-Schale auf Linux kompiliert |
| Dialog | Opus | Knopf „Projektdatei hinzuladen“, Bilanzzeile, Z7 in der Regelliste mit Quelle, Hinweis bei Teiltreffern | bunit `GebaeudeImportZonenDialogTests` |
| Controller | Opus | `GebaeudeImportCtrl`: zweite Quelle und Paarungen schreiben, `SchonImportiert` kennt die Ergänzung | `GebaeudeImportCtrlTests` |
| Papiere | Sonnet | Statuszeile, Protokoll, Mehrzonenkonzept 6.1 (Z7), Datenaustauschkonzept 2.3, Wiki-Quelle des Importdialogs, Logbuch-Entwurf | Link-Wache grün |

### 6.6 HC-5 und HC-6

Beide erst nach Entscheid (F2, F3). HC-6 bräuchte eine Tabelle für Räume mit Körper je
Gebäude (Netz als BLOB, Gruppenindex, Herkunft) und eine Ladefunktion der Ansicht außerhalb
des Imports; Alternative ohne Schema: Die Ansicht im Gebäudedialog liest die gespeicherte
Importquelle (Dateiname, Hash) und bietet „Datei erneut lesen“ an, wenn die Datei am
gewählten Ort liegt. Die Alternative kostet 1 PT statt 3–5 und wird für F3 empfohlen.


## 7. Fragen mit Empfehlung

| Nr. | Frage | Empfehlung |
|---|---|---|
| F1 | Welche Zonierung der Projektdatei ist die Vorgabe für Z7: Simulationszonen (`ZoneType` 6), DIN-18599-Zonen (5) oder Wohneinheiten (2)? | **6**, weil sie die Zonen der Stundensimulation sind und in allen gerechneten Dateien alle Räume abdecken; 5 und 2 wählbar in der Regelliste („Z7: Simulationszonen \| DIN-Zonen \| Einheiten“) |
| F2 | Sollen Profile, Kalender und Klima aus der Projektdatei übernommen werden (HC-5), und wie werden die Profilnummern auf die Konditionierungsvorlagen abgebildet? | nach HC-4 entscheiden; Vorschlag: Heiz-/Kühlsollwerte je Zone aus den Ganglinien direkt (Tag/Nacht aus `OperatingModeType`), Nutzungsklasse aus der Profilnummer, Klima nicht übernehmen (Projektklima bleibt) |
| F3 | Soll die Geometrie dauerhaft gespeichert werden, damit der Viewer außerhalb des Imports läuft (HC-6), oder genügt „Datei erneut lesen“ im Gebäudedialog? | **„Datei erneut lesen“** (1 PT); Persistenz erst, wenn Berichte oder iOS ohne Datei die Ansicht brauchen |
| F4 | Größengrenzen der `.sqproj`: Windows 250 MB, iOS 100 MB? | ja; die größte Anwenderdatei hat 211 MB, davon über 120 MB Bilder, die nicht gelesen werden; iOS misst wie bei IFC (G4-8) |
| F5 | Gruppenliste R0–R7 nach 4.1 so übernehmen? | ja; Fenster und Türen als eigene Gruppe R7, damit die Hüllbilanz vollständig ist |


## 8. Risiken

- **GId-Abgleich schlägt fehl**, wenn die IFC aus einem anderen Speicherstand exportiert wurde
  als die Projektdatei (Räume gelöscht, neu angelegt). Gegenmaßnahme: Bilanz mit Rückfall auf
  Z4/Z6 für die Resträume, Hinweis mit Datum beider Dateien (`FILE_NAME` und
  `PrJournalEntry.StampEdit`).
- **Raumseitige Stücke ohne Körper** lassen Hüllflächen ohne passendes Bauteil zurück, wenn
  Orientierung und Lage nicht eindeutig sind (schiefe Wände, Erker). Gegenmaßnahme: Rückfall
  nach Normale mit Vermerk, Gegenprobe gegen die Mengensätze.
- **Dreiecksgrenze** mit Bauteilkörpern früher erreicht: Bauteilkörper werden erst
  geladen, wenn Räume unter zwei Dritteln der Grenze bleiben; sonst Hinweis und nur Räume.
- **iPad:** Transparenz und Farbattribute in WebGL sind unkritisch, die Sichtprobe 31 steht
  aber noch aus; HC-2 erhöht die Dringlichkeit, nicht den Umfang.
- **Schemawechsel von HottCAD:** `XmTables.Version` wandert mit jeder Fassung; der Leser
  prüft die Fassung der drei gelesenen Tabellen und meldet, bricht aber nicht ab.
- **Prüferkonflikt:** `.sqproj`-SQL darf nie gegen `Kenndaten_Test.sqlite` geprüft werden
  (fremdes Schema); die Konfiguration des `SqlDialektPruefer` muss das vor HC-3 können.


## 9. Abgrenzung

Kein web-ifc, kein IFC-Vollbetrachter, keine xBIM Geometry Engine, kein neues NuGet-Paket
(ADR-003 und Datenaustauschkonzept 15.6 bleiben). Kein Schreiben der `.sqproj`, kein Lesen
der Binärströme (`WDIN18599DataModel`, `.BDExit`, Grafikmodell), keine Übernahme von
Hersteller-, Adress- oder Kontaktdaten aus der Projektdatei, keine Anwenderdateien im
Repositorium. Die Gruppenfarben sind Anzeige und Gegenprobe, keine Rechengröße; die
Einfrierregeln sind nicht berührt, weil kein Referenzprojekt importiert wird.


## 10. Regeln, die gelten

Fachänderung nur im Kern; Dialoge als Razor mit Hülle in `EPOS.UI.Daten`, keine Datenbank in
der Oberfläche; Texte in beiden Sprachen mit `ResourceDesigner`; Dateiwahl allein über
`Dienste.Datei`; was iOS nicht kann, wird benannt abgelehnt; SQL mit `?`-Parametern; neue
Schemaschritte über `SchemaMigration` mit Anmeldung der Nummer vor dem Bau; Agentenaufträge mit
`model: opus` für Umsetzung, `model: sonnet` für Texte, `AGENT_LAEUFT` im Hauptbaum; Merge →
Gate → Statuszeile und Protokoll → Push → Nachweis; iOS-Lauf nur nach Rückfrage.
