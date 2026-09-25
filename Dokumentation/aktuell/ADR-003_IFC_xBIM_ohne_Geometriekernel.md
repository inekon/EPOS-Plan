# ADR-003: IFC-Anbindung über xBIM als unverändertes NuGet-Paket — `MemoryModel` ohne Esent und ohne Geometriekernel

**Status:** Angenommen (15.09.2026, Anwenderentscheide E3 und E9; Paketzuschnitt nach Befund N und Gegenlesen)
**Datum:** 15.09.2026
**Entscheider:** Anwender (Projektverantwortung EPOS-Plan)
**Betrifft:** [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Kapitel 7, Nachtrag N1.7, N1.13 und N1.16), [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Kapitel 3), [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (Kapitel 6, 6.7),
[`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (IFC-Export, Nachtrag 1 — Gebäudebetrachter)
**Berührte Bereiche:** `EPOS.Kern/Allgemein/Import/`, `EPOS.Kern/Allgemein/Export/`, `Directory.Packages.props`, `Setup/`

---

## Kontext

Der Anwender will Gebäudedaten aus IFC-Dateien importieren und — nach Entscheid E9 — auch
als IFC exportieren. Der Rechenkern ist plattformfrei (`net10.0`, Windows und iOS) und
kennt keine native Abhängigkeit. Für das Gebäudemodell werden keine Körper gebraucht,
sondern Kennwerte: U-Werte (`Pset_WallCommon.ThermalTransmittance`), Bruttoflächen
(`Qto_WallBaseQuantities.GrossSideArea`, `Qto_SpaceBaseQuantities`), Raumgrenzen
(`IfcRelSpaceBoundary2ndLevel`), Schichtaufbauten (`IfcMaterialLayerSet`,
`Pset_MaterialThermal`), Lage und Nordrichtung (`IfcSite.RefLatitude`, `TrueNorth`,
Placement-Kette) — alles ohne Geometriekernel erreichbar (Befunde C, N, P).

Die Recherche zu den Bibliotheken (Befund C, N, S; Gegenlesen IFC) ergab:

| Befund | Folge |
|---|---|
| xBIM Essentials 6.1.605 zielt auf `net10.0` und steht unter **CDDL-1.0** (Datei-Copyleft: Quelltextverfügbarkeit der Bibliothek, Vermerke, Lizenztext an den Empfänger) | nutzbar, Auflagen sind erfüllbar, solange das Paket **unverändert** bleibt |
| Das Metapaket `Xbim.Essentials` zieht `Xbim.Ifc` und damit `Xbim.IO.Esent` (ManagedEsent, nur Windows) nach | scheidet für den Kern aus |
| `Xbim.IO.MemoryModel` bringt `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4`, `Xbim.Ifc4x3` mit; ein Leser bedient alle drei Schemata über `Xbim.Ifc4.Interfaces.IIfc*` | genügt für Import und Export |
| `Xbim.Geometry` ist Windows-only und zieht OpenCASCADE unter LGPL nach | scheidet aus |
| `IfcStore` (GlobalId- und OwnerHistory-Pflege, `AddSite`/`AddBuilding`) liegt in `Xbim.Ifc` | der Export erzeugt GlobalId und OwnerHistory selbst |
| Pset-/Quantity-Helfer hängen an der Klasse `Xbim.Ifc4.Kernel.IfcObject`, nicht an den Schnittstellen; sie lesen keine Typ-Eigenschaften | Zugriff wird selbst geschrieben, Vorkommnis vor Typ |
| Geometrieloses IFC4 ist schemakonform (`ObjectPlacement` und `Representation` sind optional); Raumgrenzen dürfen ohne `ConnectionGeometry` stehen | Export ohne Körper ist zulässig |
| Kein Modellbestand im Wohngebäudebereich; Betrachter öffnen geometrielose Dateien voraussichtlich, zeigen aber nichts an — nicht gemessen, Prüfmatrix offen | Import ist ein Komfortweg für Neubau und Nichtwohnbau, der Export ein Datenblatt |

### Kräfte, die die Entscheidung formen

1. **Plattformfreiheit:** iOS erträgt weder Esent noch native Kernel; Trimming trifft
   Reflexion (`ExpressMetaData` über `GetTypes()`).
2. **Lizenz:** CDDL erlaubt die Nutzung im proprietären Produkt, verlangt aber Vermerke,
   Lizenztext und einen Quellenverweis an den Empfänger; jede eigene Änderung an
   CDDL-Dateien fiele selbst unter die CDDL.
3. **Aufwand und Wartung:** eine Bibliothek weniger ist eine Lizenzseite, ein Paketstand
   und ein Sicherheitsrisiko weniger.
4. **Speicher:** `MemoryModel` hält das Modell im Arbeitsspeicher (10 bis 20 MB je MB
   STEP-Text); ein iPad braucht ein benanntes Größenlimit.
5. **Paketgröße:** `Xbim.IO.MemoryModel` bringt mit `Xbim.Ifc2x3`, `Xbim.Ifc4` und
   `Xbim.Ifc4x3` drei erzeugte Schema-Assemblies mit; sie wachsen in das iOS-App-Paket
   hinein, und Trimming greift bei reflexiv erreichten Typen nur begrenzt. Gemessen
   (Aufgabe 7): +36,1 MB im entpackten, +7,8 MB im gezippten Gerätepaket.
6. **Verwechslungsgefahr beim Export:** Eine Datei mit erfundener Anordnung ist
   gefährlicher als eine ohne Körper.

## Entscheidung

1. **xBIM bleibt ein unverändertes NuGet-Paket** (E3). In den Kern kommt **allein
   `Xbim.IO.MemoryModel`** (mit `Xbim.Common` und den drei Schema-Paketen);
   `Xbim.Essentials`, `Xbim.Ifc`, `Xbim.IO.Esent` und `Xbim.Geometry` werden nicht
   referenziert. Ein Fork ist ausgeschlossen.
2. **Kein Geometriekernel.** Flächen kommen aus den Mengensätzen, die Orientierung aus der
   Placement-Kette samt `TrueNorth` bzw. `IfcMapConversion`; die Ableitung aus Körpern
   (`IfcExtrudedAreaSolid`, Boolesche Operationen) bleibt der Stufe G5 „nur bei Bedarf".
3. **Der Import folgt dem Hausmuster Ablauf/Profil/Satz** unter
   `EPOS.Kern/Allgemein/Import/` (Befund N): Lesen, Vorprüfen, Ausführen; Herkunft je Feld
   (Import / Vorgabe / manuell); Zuordnungsdialog als Razor-Komponente; Größenlimit als
   benannte Ablehnung, bei `.ifczip` gegen die entpackte Größe.
4. **Die Zuordnung wird persistiert:** EPOS-Gebäude bzw. Zone ↔ `GlobalId` der Quelle
   sowie Name, Hash und Zeitpunkt der Datei — Voraussetzung für die Rückgabe angereicherter
   Dateien (Befund S).
5. **Der Export schreibt IFC4 (ADD2 TC1) über `MemoryModel` und `SaveAsStep21(Stream)`:**
   deterministische GlobalIds aus EPOS-Schlüsseln, eigene OwnerHistory, Längeneinheit Meter,
   eigener Eigenschaftssatz `EPOS_Ergebnis` ohne `Pset_`-Präfix, Energie in kWh mit
   ausdrücklicher Einheit, Prüfung mit `ExpressValidation.Validator` vor dem Speichern,
   Stufen S1 (semantisch) → S2 (Round-Trip, nur bei Importnutzung) → S3 (schematische
   Körper mit Kennzeichnung).
6. **Auslieferung:** Lizenzhinweisseite im Installationspaket für xBIM und alle übrigen
   Fremdanteile (Frage U10 des Umsetzungskonzepts), CDDL-Text und Quellenverweis.

## Betrachtete Optionen

### Option A: `Xbim.Essentials` vollständig mit `IfcStore`

| Dimension | Bewertung |
|---|---|
| Komplexität | niedrig — dokumentierter Weg, Helfer für GUID, OwnerHistory, Psets |
| Plattform | **Windows-only** (Esent) |
| Lizenz | CDDL, erfüllbar |

**Dagegen:** verletzt die Plattformfreiheit des Kerns; iOS bekäme keinen Import.

### Option B: `Xbim.IO.MemoryModel` allein, Zugriff und GUIDs selbst geschrieben *(gewählt)*

| Dimension | Bewertung |
|---|---|
| Komplexität | mittel — Pset-/Quantity-Zugriff, GlobalId, OwnerHistory, Einheiten selbst |
| Plattform | Windows und iOS; Trimming zu messen |
| Lizenz | CDDL, unverändertes Paket |
| Aufwand | Import G4a 13–21 PT, Export S1 12–20 PT |

**Dafür:** kleinster Paketzuschnitt, der alle drei Schemata liest und IFC4 schreibt.
**Dagegen:** einige Bequemlichkeiten von `IfcStore` sind nachzubauen.

### Option C: GeometryGymIFC (MIT)

| Dimension | Bewertung |
|---|---|
| Komplexität | mittel |
| Lizenz | MIT, keine Auflagen |
| Verbreitung und Pflege | kleiner als xBIM; weniger Dokumentation |

**Bewertung:** bleibt der **benannte Ausweg**, falls xBIM ausfällt oder die CDDL-Auflagen
nicht mehr tragbar sind (Statusdatei, Zeile Q9).

### Option D: IfcOpenShell oder ein anderer nativer Kernel

| Dimension | Bewertung |
|---|---|
| Plattform | native Bibliothek, iOS nicht erreichbar |
| Lizenz | LGPL |
| Nutzen | Geometrie, die das Gebäudemodell nicht braucht |

**Dagegen:** bezahlt mit Plattform und Lizenz für etwas, das nicht gebraucht wird.

### Option E: Eigener STEP-Leser für die benötigten Entitäten

| Dimension | Bewertung |
|---|---|
| Komplexität | hoch — STEP-Physical-File, Schema-Abhängigkeiten, drei Schemafassungen |
| Lizenz | keine Fremdanteile |
| Aufwand | Vielfaches von B, ohne Export-Schreibweg |

**Dagegen:** ein eigener Parser für ein Format, das eine gepflegte Bibliothek bereits liest.

## Abwägung

A ist der bequemste, B der kleinste und C der lizenzfreieste Weg. A scheitert an iOS, C an
Verbreitung und Dokumentation; E ist unverhältnismäßig, D unnötig. B kostet Eigenbau an
genau den Stellen, die `IfcStore` bequem macht — GlobalId, OwnerHistory, Pset-Zugriff mit
Typ-Rückfall, Einheiten je Größenart —, gewinnt dafür aber beide Plattformen mit einem
Paket, das unverändert bleibt und damit die CDDL-Auflagen auf Vermerke, Lizenztext und
Quellenverweis begrenzt.

## Konsequenzen

- **Einfacher wird:** eine Paketzeile in `Directory.Packages.props`, ein Leser für drei
  Schemata, Import und Export mit derselben Bibliothek.
- **Schwerer wird:** Pset- und Quantity-Zugriff mit Vorkommnis-vor-Typ, Flächeneinheit je
  Größenart (nie aus der Längeneinheit quadriert), Außennormale nur über die Raumgrenze
  bestimmbar (sonst Azimut um 180° unbestimmt und Meldung), `Ifc4x1` benannt abgelehnt.
- **Auslieferung:** Lizenzhinweisseite ist Vorbedingung; ohne sie ist der Import nicht
  auslieferbar.
- **Später zu prüfen:** Trimming-Verhalten eines iOS-Gerätebaus (Simulatorbauten trimmen
  nicht); Größenlimit auf dem iPad messen; **Paketgröße der iOS-App messen** (mit und ohne die
  drei Schema-Assemblies, `ios-arm64`, getrimmt — Kraft 5); Betrachter-Prüfmatrix für
  geometrielose Dateien (rund 1 PT); vertragliche Zulässigkeit der Rückgabe veränderter
  Fremddateien vor S2.
- **Gebäudebetrachter (E11):** kein IFC-Betrachter mit Geometriekernel; die Ansicht zeigt die
  Raumgrenzen als **2D-Grundriss** je Geschoss (SVG in einer Razor-Komponente) und **schematische
  Körper** aus dem Zonengeometrie-Modell (three.js unter MIT, lokal ausgeliefert) —
  [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), Nachtrag 1. **web-ifc**
  (MPL-2.0, auf iOS speicherkritisch) und **xBIM Geometry** (nur Windows, OCCT unter LGPL) bleiben
  benannte, **nicht geplante** Optionen; „Datei extern öffnen" bleibt über `Dienste.Datei` zulässig.
- **Ausweg bleibt benannt:** GeometryGymIFC (MIT).

## Aufgaben

> **Stand 25.09.2026 (Umsetzung G4, [Protokoll G4](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-24_G4_Importe.md)):**
> Aufgaben 1 bis 3 sind umgesetzt — das Gerüst heißt formatfrei `GebaeudeImportAblauf`/`-Profil`/`-Satz`
> (`EPOS.Kern/Allgemein/Import/Gebaeude/`), der Leser `IfcLeser`, die Zuordnung liegt in Schemaschritt 138.
> Aufgabe 5: Der eine iOS-Lauf (E38) hat den Import unter Trimming und AOT im Simulator nachgewiesen
> (xBIM-Metadaten vollständig, 0 IL-Warnungen, kein Deskriptor nötig) und die iOS-Grenzen gemessen
> (Protokoll G4, Abschnitt 10). Aufgabe 7: Der Gerätebau `ios-arm64` lief im ersten Lauf wegen der
> Simulatorkennung in `RuntimeIdentifiers` der iOS-Schale als Simulatorbau und brach am Linken der
> Geräte-SQLite ab — nicht an xBIM; nach der Korrektur baut er vollständig (Plattform iOS, AOT,
> 0 IL-Warnungen), und der Gerätejob von `ios.yml` misst ihn mit und ohne die Paketzeile
> (`-p:OhneXbim=true`; Lauf 36116562830, Protokoll G4, Abschnitt 11). Offen ist allein die Bewertung
> des Zuwachses (Aufgabe 7).

1. [x] `Xbim.IO.MemoryModel` in `Directory.Packages.props` aufnehmen; Lizenzhinweisseite im
       Setup (Frage U10).
2. [x] `IfcImportAblauf`/`IfcImportProfil`/`IfcImportSatz` nach Befund N mit den
       Korrekturen des Gegenlesens (Typ-Eigenschaften, Einheiten, Außennormale, Schemafall
       `Ifc4x1`, `.ifczip`-Größe).
3. [x] Zuordnungstabelle und Herkunftsspalten (Datenaustauschkonzept, Kapitel 7).
4. [ ] Export S1 nach Befund S; S2 und S3 erst nach den Fragen des Datenaustauschkonzepts zum
       **Empfänger** (**D6**, 11.1) und zur **vertraglichen Zulässigkeit der Rückgabe** (**D11**,
       11.1); die **Zurückstellung von S2** steht als **D7** in 11.2 — dort beantwortet das Papier
       selbst, der Anwender kann widersprechen.
5. [x] iOS-Nachweis (Gerätebau, Trimming, Größenlimit) nach Rückfrage.
6. [ ] **Zonengeometrie-Modell und Gebäudeansicht nach E11:** Modell im Kern und 2D-Grundriss im
       Zuordnungsdialog mit G6c, schematische Körper mit G7b; three.js lokal unter
       `EPOS.UI/wwwroot` und auf die Lizenzhinweisseite (Frage U10).
7. [x] **Paketgröße der iOS-App messen** (Kraft 5): je ein Bau `ios-arm64`, getrimmt, mit und
       ohne die Paketzeile `Xbim.IO.MemoryModel`; den Zuwachs durch die drei Schema-Assemblies
       hier eintragen. Fällt er erheblich aus, den Import auf der iOS-Schale benannt ablehnen
       oder die Schemata einzeln referenzieren.
       **Gemessen** (`ios.yml` Lauf 36116562830, Job `geraetebau`: Release, AOT ohne LLVM,
       `TrimMode` partial, ohne Signatur, Seed ist die Testdatenbank; MB zu 1 048 576 Byte):

       | Größe | mit xBIM | ohne xBIM | Zuwachs |
       |---|---|---|---|
       | App-Paket entpackt (Summe der Dateien) | 290,7 MB | 254,6 MB | **+36,1 MB (+14,2 %)** |
       | davon Programm (AOT-Code, statisch gelinkt) | 143,5 MB | 120,7 MB | +22,8 MB |
       | davon xBIM-Dateien (IL-freie DLLs und `aotdata`) | 10,4 MB | — | +10,4 MB |
       | davon übrige Dateien | | | +2,8 MB |
       | App-Paket gezippt (Näherung der Ladegröße) | 84,8 MB | 77,0 MB | **+7,8 MB (+10,1 %)** |
       | Seed-Datenbank (in beiden gleich) | 64,8 MB | 64,8 MB | 0 |

       **Die drei Schema-Assemblies** (`Xbim.Ifc2x3`, `Xbim.Ifc4`, `Xbim.Ifc4x3`) tragen 9,5 der
       10,4 MB xBIM-Dateien und 62,6 der 66,4 MB AOT-Objekte von xBIM vor dem Linken (94 %);
       anteilig gerechnet sind das rund 31 MB des entpackten Zuwachses. `Xbim.Common` und
       `Xbim.IO.MemoryModel` zusammen rund 5 MB.
       **Bewertung offen beim Anwender.** Empfehlung: nicht erheblich — der IFC-Import bleibt
       auf iOS. Der Zuwachs liegt unter einem Achtel des entpackten Pakets und bei rund einem
       Zehntel des gezippten. Ein einzelnes Schema zu streichen, spart je Schema rund ein
       Drittel des Anteils, würde aber ein Schema benannt ablehnen, das unter Windows gelesen wird.
