# ADR-004: gbXML-Leseweg — LINQ to XML mit handgeschriebenem Modell statt `XmlSerializer`

**Status:** Vorgeschlagen (15.09.2026, Empfehlung aus Befund R; Entscheid des Anwenders steht aus)
**Datum:** 15.09.2026
**Entscheider:** Anwender (Projektverantwortung EPOS-Plan)
**Betrifft:** [`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md)
(Kapitel 3 und 5), [`Gebaeudesimulation/2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md`](Gebaeudesimulation/2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md),
Nachtrag N1.13 des [Konzepts](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
**Berührte Bereiche:** `EPOS.Kern/Allgemein/Import/`, `EPOS.Kern/Allgemein/Export/`, Testdaten

---

## Kontext

Entscheid E9 macht den gbXML-Import zur Pflicht der Stufe G4 und sieht den gbXML-Export in
Stufe G7 vor. gbXML ist das Austauschformat der Energiesimulation (OpenStudio/EnergyPlus,
IES VE, DesignBuilder; Autorensysteme Revit, Archicad). Befund R hat Schema, Beispieldateien
und den .NET-Weg selbst nachgemessen:

| Befund | Folge |
|---|---|
| `GreenBuildingXML_Ver8.01.xsd` ist bis auf Versionsattribut, Versionszeile und Änderungsprotokoll gleich `7.04`; der Aufzählungstyp `versionEnum` endet bei `6.01` — eine Datei mit `version="8.01"` ist nach dem eigenen Schema ungültig | EPOS schreibt `6.01` |
| Schema und die vier offiziellen Beispieldateien tragen **keine Lizenz** | XSD nur im Test, nicht ausliefern; Testdateien selbst erzeugen |
| Keine der vier Beispieldateien ist schemagültig (88 bis 2 930 Fehler); ein Architekturexport hat null Konstruktionen; zwei Dateien sind UTF-16LE | Import tolerant, nicht validierend, als Strom öffnen |
| Jede Fläche trägt `RectangularGeometry` (Fläche, Azimut, Neigung) neben `PlanarGeometry` | Import braucht keinen Geometriekernel |
| Zielwerkzeuge lesen nur `PlanarGeometry/PolyLoop` (OpenStudio: 0 Vorkommen von `RectangularGeometry` im Rückübersetzer) | Export ohne synthetische Geometrie hat für Simulationswerkzeuge keinen Wert |
| `xsd.exe` erzeugt 328 Klassen mit `object[] Items` (durchgängig `choice maxOccurs="unbounded"`); `new XmlSerializer(typeof(gbXML))` wirft schon auf Windows mit JIT (`CodeGenError`), unter Trimming/AOT kommen IL3050 und IL2026 | generierter Code ist unbrauchbar |
| Es gibt keine gepflegte .NET-gbXML-Bibliothek (offizielles C#-SDK: letzter Stand 2016, Schema 5.11, keine Lizenz) | kein Paket zu referenzieren |
| `spaceTypeEnum` (126 Werte) kennt keine Wohnnutzung; Baualter und Wärmebrücken fehlen im Schema | `spaceType` weglassen, Vorgaben aus dem Gebäudekatalog |
| Gemessen: 16,3-MB-Datei validierend in 1,66 s, XSD-Compile 4 ms | Streaming nicht nötig |

### Kräfte, die die Entscheidung formen

1. **Plattformfreiheit und Trimming:** der Kern muss auf iOS laufen; Reflexion und
   Code-Erzeugung zur Laufzeit sind dort riskant.
2. **Robustheit gegen die Praxis:** die Dateien der Autorensysteme sind nicht schemagültig
   und lückenhaft; der Leser muss ohne Konstruktionen sinnvoll weiterlaufen.
3. **Umfang:** EPOS braucht rund 25 der 518 Elemente des Schemas.
4. **Wartung:** ein Fremdpaket ohne Pflege ist teurer als eigener Code.

## Entscheidung (vorgeschlagen)

1. **Lesen und Schreiben mit LINQ to XML** (`System.Xml.Linq`) gegen ein
   **handgeschriebenes Modell** der benötigten Elemente (`Campus`, `Building`, `Space`,
   `Zone`, `Surface`, `Opening`, `Construction`, `Layer`, `Material`, `WindowType`,
   `Location`, Einheitenattribute) — reine BCL, keine Code-Erzeugung, keine Reflexion.
2. **Import tolerant:** Datei als Strom öffnen (Kodierung aus der XML-Deklaration),
   Einheiten global und lokal auflösen, `RectangularGeometry` als Flächenquelle,
   Fensterabzug selbst rechnen, Aufbauten je Schicht auf Vollständigkeit prüfen
   (unvollständig → masselos → Warnung und Klassenweg), fehlende Konstruktionen als
   Herkunft „Vorgabe" kennzeichnen.
3. **Export zweistufig:** Stufe 1 schemagültiger Datenexport mit `version="6.01"`,
   `RectangularGeometry`, vollständigen Aufbauten, mindestens vier Flächen je `Campus`,
   ohne `spaceType` und ohne Anlagentechnik; Stufe 2 synthetische Quadergeometrie
   (`PlanarGeometry/PolyLoop`) aus Fläche, Volumen und Bauteilgruppen mit Kennzeichnung
   „schematisch" in Datei und Oberfläche.
4. **Validierung nur im Test** gegen das XSD, das nicht ausgeliefert wird; Rundlauf
   Export → Import als Regressionstest mit selbst erzeugten Dateien.

## Betrachtete Optionen

### Option A: `xsd.exe`-Klassen mit `XmlSerializer`

| Dimension | Bewertung |
|---|---|
| Komplexität | scheinbar niedrig |
| Brauchbarkeit | `object[] Items` statt typisierter Eigenschaften; Serializer-Konstruktor wirft |
| Plattform | IL3050/IL2026 unter Trimming und AOT |

**Dagegen:** gemessen nicht lauffähig, auch nicht auf Windows.

### Option B: LINQ to XML mit handgeschriebenem Modell *(vorgeschlagen)*

| Dimension | Bewertung |
|---|---|
| Komplexität | mittel — rund 25 Elemente, Einheitenlogik, IDREF-Auflösung zweistufig |
| Plattform | reine BCL, iOS-fest |
| Robustheit | tolerant gegen ungültige Dateien |
| Aufwand | Import **17–28 PT** (G4c: 14–22 PT für das Lesen allein, dazu die Persistenz aus Kapitel 7); Export **9–14 PT** (G7a) plus **7–12 PT** für Stufe 2 (G7b, nach der E11-Ersparnis) — [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) Kapitel 10 |

**Dafür:** kleinster, wartbarer, plattformfester Weg. **Dagegen:** eigener Code statt
Schema-Automatik; Schemaänderungen sind von Hand nachzuziehen (praktisch seit 7.04 keine).

### Option C: `XmlReader` als Strom

| Dimension | Bewertung |
|---|---|
| Komplexität | hoch — IDREF-Auflösung braucht mehrere Durchläufe oder Zwischenspeicher |
| Nutzen | Speicher bei sehr großen Dateien |

**Dagegen:** 16 MB laden in unter zwei Sekunden; der Gewinn rechtfertigt die Komplexität nicht.

### Option D: Fremdbibliothek

Es gibt keine gepflegte .NET-Bibliothek mit Lizenz. Entfällt.

## Abwägung

A scheidet gemessen aus, D existiert nicht, C löst ein Problem, das gbXML-Dateien nicht
haben. B ist damit nicht nur die beste, sondern die einzige tragfähige Option; offen bleibt
nur, ob der Export mit Stufe 2 (synthetische Geometrie) überhaupt beauftragt wird — ohne sie
ist er ein Datenblatt in XML, das bilanzierende Werkzeuge lesen, Simulationswerkzeuge nicht.

## Konsequenzen

- **Einfacher wird:** keine Paketabhängigkeit, keine Lizenzseite, ein Lesemodell für Import
  und Export; das Zuordnungsgerüst (Ablauf/Profil/Satz, Herkunft je Feld) wird mit dem
  IFC-Import geteilt.
- **Schwerer wird:** Einheiten (Fuß, Fahrenheit, BTU) und Kodierung sind eigene Testfälle;
  die Vollständigkeit der Aufbauten ist je Aufbau zu prüfen; die Versionsnummer `6.01`
  braucht einen Quelltextkommentar, damit niemand sie „korrigiert".
- **Offen für den Anwender:** Reihenfolge gbXML-Import vor IFC-Import (**D1**; Befund R empfiehlt
  es: kleinere Aufgabe, schafft das Zuordnungsgerüst, erschließt Archicad); Export nur mit
  Stufe 2 (**D2**); Zonenbildung X1…X3 mit G6c (**D16**) — alle drei in Kapitel 11.1 des
  Datenaustauschkonzepts. Der Umgang mit lizenzlosen Test- und Schemadateien ist als **D3** in
  11.2 beantwortet; offen bleibt dort allein die Ablage der XSD-Kopie im Repositorium.

## Aufgaben

1. [ ] Entscheid des Anwenders zu **D1** (Reihenfolge), **D2** (Export nur mit Stufe 2) und
       **D16** (Zonenbildung X1…X3 mit G6c) — Kapitel 11.1 des Datenaustauschkonzepts. **D3**
       (Schema und Testdateien ohne Lizenz) steht in 11.2 zur Kenntnis; zu klären bleibt dort
       allein die Ablage der XSD-Kopie im Repositorium.
2. [ ] `GbxmlImportAblauf`/`-Profil`/`-Satz` nach dem Hausmuster; Lesemodell; Einheiten.
3. [ ] Rundlauf-Testdateien selbst erzeugen; XSD in den Testordner, gitignoriert oder mit
       geklärter Lizenz.
4. [ ] Export Stufe 1, dann Stufe 2 nach Freigabe.
