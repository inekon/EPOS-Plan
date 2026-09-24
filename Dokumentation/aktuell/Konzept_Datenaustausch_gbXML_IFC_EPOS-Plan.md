# Konzept: Datenaustausch gbXML und IFC — Import und Export (EPOS-Plan)

**Rev. 2 — 17.09.2026 — Prüfung 17.09.2026, E26 eingearbeitet**

> **Was Rev. 2 ändert:** 7.4 nennt keine festen Schrittnummern mehr — die Gebäudespalten-Schritte
> tragen die Papiernamen **M3** und **M4**, der Zielstand wird an `SchemaStand.Zielversion`
> abgelesen; sonst bleibt der Stand der Rev. 1.
>
> **Nachzug 22.09.2026 — E27 (22.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.32):** Der Anwender hat alle acht
> D-Fragen des Registers entschieden, sämtlich nach der Empfehlung dieses Papiers: **D1** gbXML-Import
> vor IFC-Import (G4c vor G4a); **D2** gbXML-Export nur mit Stufe 2 (G7a und G7b zusammen);
> **D4** kWh mit ausdrücklicher Einheit; **D5** deterministische Kennungen in beiden Formaten;
> **D6** die semantische Stufe G7c zuerst — das Gegenüber des IFC-Exports benennt der Anwender vor
> der Stufe, die über die semantische hinausgeht; **D11** die Rückgabe angereicherter fremder
> IFC-Dateien ist zulässig, mit Kennung in der Datei und Beipackzettel; **D16** die gbXML-Zonenbildung
> erweitert E7 auf ein zweites Format; **D17** (der Rest aus D3) die gbXML-XSD liegt **außerhalb**
> des Repositoriums, der Validierungstest wird benannt übersprungen, wenn sie fehlt. Mit E27 ist
> auch **U10** entschieden (Lizenzhinweisseite mit der ersten IFC-Stufe). Die Fließtexte in 0, 1.3,
> 1.4, 3.3, 5.1, 6.1, 6.4, 6.6, 8.1, 8.2, 9, 10, 11 und 12 tragen den entschiedenen Stand.
>
> **Nachzug 24.09.2026 — E38 (24.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.43):** Der Anwender hat
> die drei Importfragen **U13**, **U14** und **U15** des Umsetzungskonzepts nach Empfehlung
> entschieden; sie gelten für beide Importwege, weil gbXML (G4c) und IFC (G4a) über das gemeinsame
> Zuordnungsgerüst dieselben Zielfelder füllen (2.1): **U13** eines je Lauf (Klappliste), **U14** die
> Wandfläche um Fenster und Außentüren vermindert, **U15** ψ als Vorgabe je Baualtersklasse,
> Anschlusslängen leer, beide mit Herkunftsmarke. Zugleich ist die Stufe G4 beauftragt — zuerst G4c,
> dann G4a; G4b erst nach G3 und nachdem G4a im Feld war. Für G4 gibt es genau einen iOS-Lauf, bei
> der Abnahme von G4a und nur nach ausdrücklicher Rückfrage; G4c wird ohne iOS-Lauf abgenommen. 3.4,
> 3.6, 3.7 und 9 tragen den Vermerk.

Auftrag (Anwender, 15.09.2026, im Wortlaut):

> „Der Import und Export von gbXML und IFC sollen möglich sein."

Das ist **Entscheid E9** ([`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Nachtrag 1, N1.13): EPOS-Plan tauscht Gebäudedaten in beide Richtungen und in beiden Formaten;
der gbXML-Import wird Pflicht in G4, die beiden Exporte sind Stufe G7.

**Dieses Papier ist das dritte der Gebäudesimulation.** Es steht neben

| Papier | Was dort steht — und hier nicht wiederholt wird |
|---|---|
| [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) | Physik, Rechenweg, Stufen G0–G5, die Entscheide E1–E11 |
| [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) | Einbindung in den Kern, Gebäudedialog, **IFC-Gebäudeimport G4a** (Kapitel 3), Stufen und Abnahme (Kapitel 4) |
| [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) | Zonenkopplung, **Datenmodell** Zone → Bauteil → Aufbau → Schicht → Baustoff (Kapitel 4), **IFC-Import in Zonen** (Kapitel 6), Stufe G6 |

Die Befunde, auf denen jede Aussage dieses Papiers steht:

| Befund | Gegenstand |
|---|---|
| [`Gebaeudesimulation/2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md`](Gebaeudesimulation/2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md) | gbXML: Schema, Pflichtangaben, Beispieldateien ausgezählt, Zielwerkzeuge, .NET-Umsetzung, Abbildung, Aufwand |
| [`Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md`](Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md) | IFC-Export: geometrieloses IFC4, logische Raumgrenzen, `EPOS_Ergebnis`, GUID und `OwnerHistory`, Round-Trip, Stufen S1–S3 |
| [`Gebaeudesimulation/2026-09-15_Befund_N_IFC-Import_Entwurf.md`](Gebaeudesimulation/2026-09-15_Befund_N_IFC-Import_Entwurf.md) | Importmuster Ablauf/Profil/Satz, Paketwahl, Klassen, Herkunft je Feld, Plattformnaht |
| [`Gebaeudesimulation/2026-09-15_Befund_C_IFC-Recherche.md`](Gebaeudesimulation/2026-09-15_Befund_C_IFC-Recherche.md) | IFC-Schema, Psets/Qtos, gbXML-Einordnung, Bibliotheken, Testdateien und ihre Lizenzen |
| [`Gebaeudesimulation/2026-09-15_Befund_P_IFC_Zonen_Materialien.md`](Gebaeudesimulation/2026-09-15_Befund_P_IFC_Zonen_Materialien.md) | Zonen, Grenzflächen und Stoffwerte an vier IFC-Dateien gemessen |

Die beiden Bauweisen, die dieses Papier voraussetzt, sind als Entscheidungsvermerk festgehalten:
[`ADR-003_IFC_xBIM_ohne_Geometriekernel.md`](ADR-003_IFC_xBIM_ohne_Geometriekernel.md) (xBIM als
unverändertes NuGet-Paket, `MemoryModel` ohne Esent) und
[`ADR-004_gbXML_LINQ_to_XML.md`](ADR-004_gbXML_LINQ_to_XML.md) (LINQ to XML mit handgeschriebenem
Modell; angenommen 16.09.2026, E16). Der Stand der Entscheide steht in [`Status_Gebaeudesimulation_VDI6007.md`](Status_Gebaeudesimulation_VDI6007.md).
Normzahlen der VDI 6007 stehen hier nicht; aus VDI 6020:2022 ist nach **E6** nichts herangezogen.
Jede Aussage über den Quelltext trägt Datei und Zeile. **Dieses Papier entscheidet nichts; es legt
vor** — seine Fragen D1 bis D17 hat der Anwender mit E27 (22.09.2026) entschieden (11).

---

## 0. Das Ergebnis in sechs Punkten

1. **Vier Wege, und sie sind ungleich viel wert.** Der **gbXML-Import** ist die billigste und
   ergiebigste Richtung: 17–28 PT einschließlich Persistenz, keine Fremdbibliothek, kein
   Geometriekernel. Die thermische Topologie trägt in gbXML die `Surface` mit ihren ein bis zwei
   `AdjacentSpaceId`; **das Schema erzwingt sie nicht** — an der `Surface` sind nur `id`,
   `surfaceType` und `constructionIdRef` erzwungen —, aber **alle vier ausgezählten Dateien
   schreiben sie**, anders als die IFC-Raumgrenzen, die eine selten exportierte MVD voraussetzen
   (Befund R, 1.3/1.8). Der **IFC-Import** steht in den Nachbarpapieren (G4a, G6c). Die beiden
   **Exporte** stehen unter je einem Vorbehalt: Ein gbXML ohne `PolyLoop` wird von keinem der
   geprüften **dynamischen** Simulationswerkzeuge (IES VE, OpenStudio/EnergyPlus, DesignBuilder) zu
   einem Modell; für die bilanzierenden Werkzeuge (Solar-Computer, EVEBI) ist es weder belegt noch
   widerlegt (Befund R, 3). Und ein geometrieloses IFC ist für ein Rechenwerkzeug vollständig, für
   den Menschen am Bildschirm leer — **nach der Quellenlage zu erwarten**, systematisch geprüft erst
   mit Probe 16 (Befund S, 1.8).

2. **Für gbXML gibt es keine .NET-Bibliothek — und es braucht auch keine.** Das aus dem Schema
   erzeugte Modell ist unbrauchbar: In seinen 25 094 Zeilen stehen die Kindinhalte durchgängig als
   `object[] Items` mit paralleler `ItemsElementName`-Liste statt als typisierte Eigenschaften — es
   gibt kein `space.Area`. Der Standardserialisierer scheidet schon auf Windows aus. Der Weg ist
   **LINQ to XML mit handgeschriebenem Lesemodell**: reine BCL, kein Paket, keine
   Laufzeitcodeerzeugung, iOS-fest — statt 518 Schemaelementen braucht EPOS rund 25
   (Befund R, 4.1/4.2; [`ADR-004`](ADR-004_gbXML_LINQ_to_XML.md), angenommen 16.09.2026, E16).

3. **Beide Importe tragen EIN Zuordnungsgerüst.** Muster Ablauf/Profil/Satz (Befund N, 1.1),
   Herkunft je Feld, **ein** Zuordnungsdialog, **eine** Persistenz. Die **Anforderung** steht im
   Mehrzonenkonzept (Kapitel 6 — Zuordnung, Dateiname, SHA-256, Zeitpunkt) und verweist für die
   Tabellen hierher; **neu sind hier die Tabellen**: `Tab_Importquelle` (Dateiname, SHA-256, Größe,
   Schema, Zeitpunkt) und `Tab_Importzuordnung` (EPOS-Zeile ↔ `IfcSpace.GlobalId` bzw. gbXML-`id`).
   Ohne sie gibt es keinen Round-Trip (Befund S, 6, Nr. 6) und keine belegbare Herkunft je Feld.

4. **Zwei Festlegungen sind unwiderruflich und gehören vor die erste Zeile Quelltext.**
   `gbXML/@version` muss **`6.01`** heißen — `8.01` ist nach dem eigenen Schema ungültig, weil
   `versionEnum` bei 6.01 endet (Befund R, 1.1). Und die `GlobalId` des IFC-Exports muss
   **deterministisch** aus den EPOS-Schlüsseln entstehen, sonst trägt jeder zweite Export derselben
   Zone eine andere Kennung und entwertet den Modellvergleich beim Empfänger (Befund S, 3.3). Dazu
   die Einheitenfrage: kWh mit explizitem `Unit` statt stillschweigendem Joule (Befund S, 1.6).

5. **Kein Export darf mehr behaupten, als er weiß.** Die synthetische Quadergeometrie des
   gbXML-Exports und die schematischen Körper des IFC-Exports haben die richtige Bilanz und eine
   erfundene Gestalt. Das gehört sichtbar in die Datei (`Campus/Description`, `FILE_DESCRIPTION`,
   Projektname) **und** in die Oberfläche — eine Datei, die aussieht wie ein Gebäude, aber keines
   ist, ist gefährlicher als eine, die offensichtlich leer ist (Befund S, 2.3).

6. **Lizenz: eine Auflage, zwei Verzichte.** xBIM bleibt unverändertes NuGet-Paket unter CDDL-1.0
   (**E3**), und § 3.1 verlangt den Quellenverweis an den Empfänger — die Lizenzhinweisseite im
   Installationspaket ist Vorbedingung der Auslieferung und kommt mit der ersten IFC-Stufe
   (Umsetzungskonzept 3.6, U10, entschieden mit E27). Das
   gbXML-Schema hat **keine Lizenz** und wird nicht ausgeliefert, sondern nur im Test aus einer
   lokalen Kopie benutzt; die vier gbxml.org-Beispieldateien kommen **nicht** ins Repositorium —
   die Prüfdateien erzeugt der eigene Exporteur (Befund R, 1.10/4.4). **Die XSD-Kopie liegt nicht
   im Repositorium** (D3/D17, entschieden mit E27): `.gitignore`, Einrichtungshinweis, und der
   Validierungstest wird benannt übersprungen, wenn sie fehlt (8.2). **Aufwand: G4c 17–28 PT, G7a–G7e 42–72 PT,
   mit der 3D-Ansicht aus E11 (4–7 PT) zusammen 46–79 PT** (Kapitel 10; die Befundzahlen
   enthalten die Persistenz noch nicht, die Summe G7 den Anteil des Gebäudebetrachters aus
   Nachtrag 1).

**Nachtrag 1 (Entscheid E11, 15.09.2026) ändert an diesen sechs Punkten die Herkunft, nicht den
Inhalt:** Die synthetische Geometrie aus Punkt 5 wird nicht mehr im Exporteur gerechnet, sondern
einmal im Kern als **Zonengeometrie-Modell** — dieselbe Quelle, aus der der **Gebäudebetrachter**
seinen 2D-Grundriss (mit G6c) und seine schematischen Körper (mit G7b) zeichnet; G7b und G7e
**schreiben** sie nur noch. Aufwand des Betrachters **10–17 PT**, davon 4–7 PT in G7; die **Summe G7
steht damit bei 46–79 PT** statt 45–77 PT (Kapitel 14 und Kapitel 10).

---

## 1. Einordnung

### 1.1 Was E9 verlangt

E9 nennt den Gegenstand des Austauschs ausdrücklich: im Einzonenmodell die Hülle nach **E2**
(Bauteilgruppen mit U·A, Fenster je Orientierung, Volumen), im Mehrzonenmodell (**E7**) Zonen,
Bauteile je Zone mit Fläche und Orientierung, Aufbauten, Schichten und Baustoffe — samt
Ergebnissen als Eigenschaften. Und er nennt die Grenze: **ein Export ohne Geometriekernel liefert
keine Gebäudegeometrie**, sondern ein semantisches Modell.

Dieses Papier prüft, was daraus in jeder der vier Richtungen tatsächlich wird, und was die
Zielwerkzeuge damit anfangen können.

### 1.2 Die vier Wege

| Weg | Stufe | Gegenüber | Was er leistet | Wo er beschrieben ist |
|---|---|---|---|---|
| **gbXML-Import** | **G4c** (Pflicht) | Archicad, Revit, Vectorworks, DDS-CAD | Zonen, Flächen, Azimut/Neigung, Randbedingung, U-Werte, Schichtaufbauten — ohne Geometriekernel | Kapitel 3 dieses Papiers |
| **IFC-Import** | G4a (Einzone), G6c (Zonen) | dieselben Autorensysteme, Scan-to-BIM | dasselbe, aber mit Geometrieauswertung der Raumgrenzen | Umsetzungskonzept 3, Mehrzonenkonzept 6; **Ergänzungen** in Kapitel 4 |
| **gbXML-Export** | **G7a** (Daten), **G7b** (Quader) | Solar-Computer (GBIS), EVEBI; mit G7b OpenStudio/EnergyPlus, IES VE, DesignBuilder | Bilanzgrößen vollständig; mit G7b ein rechenbares Ersatzmodell | Kapitel 5 |
| **IFC-Export** | **G7c** (S1), **G7d** (Round-Trip), **G7e** (Körper) | Fachplaner, Energieberater, Prüfwerkzeuge, bSI-Validierungsdienst | semantisches Modell mit Ergebnissen; mit G7d die angereicherte Fremddatei | Kapitel 6 |

**Archicad ist der aussichtsreichste, aber ungemessene Quellkandidat.** Keine der vier
ausgezählten Dateien stammt von dort, die Feldzuordnung ist nicht aus der Primärquelle belegbar,
und ein Fehlerbericht zum gbXML-Export ist bis AC28 offen; **vor einer Zusage im Feld ist mit einer
echten Kundendatei zu prüfen** (Befund R, 2.2). Zu Vectorworks fehlt jeder Beleg über
Schichtaufbauten — dort besteht eine echte Belegslücke.

**Die Rollen sind verschieden, und sie sollten nicht vermischt werden.** Die **Importe** füllen ein
EPOS-Gebäude vor; ihr Maßstab ist, wie viele Felder mit Herkunft `Ifc`/`GbXml` statt `Vorgabe`
belegt werden. Die **Exporte** geben ein EPOS-Ergebnis weiter; ihr Maßstab ist, ob das Gegenüber es
lesen und nachvollziehen kann. Ein Export ist **kein** Sicherungsformat und **kein** Projektaustausch
zwischen zwei EPOS-Installationen — dafür gibt es `ProjektExportImportCtrl`.

### 1.3 Was gbXML gegenüber IFC kann, und was nicht

| | gbXML | IFC |
|---|---|---|
| Thermische Topologie | `Surface` mit 1–2 `AdjacentSpaceId`: **in der Praxis durchgängig vorhanden** (alle vier Messdateien), **schemaseitig nicht erzwungen** | optional (`IfcRelSpaceBoundary`), in der Praxis oft fehlend oder nur 1. Ebene |
| Fläche/Azimut/Neigung | fertig in `RectangularGeometry` — **kein Geometriekernel** | aus Polygon und Placement-Kette zu rechnen (Mehrzonenkonzept 6.2) |
| Schichtaufbau mit λ/ρ/c | `Construction`→`Layer`→`Material`, in 11–14 von 16–21 Stoffen gefüllt (Befund R, 2.1) | `IfcMaterialLayerSet`, in **null** von vier Messdateien brauchbar gefüllt (Befund P, 0, Nr. 6) |
| Baualtersklasse | **fehlt vollständig** | `Pset_BuildingCommon.YearOfConstruction` (Text) |
| Normstatus, Verbreitung | kein ISO-Standard; 8.01 ist **byteweise 7.04** (nur Versionsattribut und Protokolltext unterscheiden sich), die Beispieldateien stammen von 2020, die Werkzeugliste ist ungepflegt | ISO 16739; die deutsche Normungsarbeit (VDI 2552 Blatt 11.9) läuft darauf |
| Bibliothek für .NET | **keine** — Eigenimplementierung, rund 25 Elemente | xBIM unter CDDL-1.0 (E3), rund 10 MB Assemblies |
| Lizenz des Schemas | **keine** (dreifach geprüft, Befund R, 1.10) | frei, buildingSMART |

Daraus folgt kein „gbXML statt IFC", sondern eine Reihenfolge: gbXML ist die **kleinere**
Aufgabe, die dasselbe Zuordnungsgerüst schafft, das der IFC-Weg dann mitbenutzt. **Entschieden ist
mit E27: der gbXML-Import kommt vor dem IFC-Import** (**D1**; 10).

### 1.4 Was dieses Papier an den Nachbarpapieren präzisiert

Sieben Stellen, je mit Grund — alles andere gilt unverändert weiter.

| # | Stelle | Präzisierung |
|---|---|---|
| 1 | `IfcHerkunft { Leer, Ifc, Vorgabe, Manuell }` (Umsetzungskonzept 3.3, Befund N 4.2) | heißt **`Importherkunft`** und bekommt den Wert **`GbXml`**. Derselbe Dialog zeigt beide Formate; ein Aufzählungstyp mit `Ifc` im Namen, der eine gbXML-Zahl kennzeichnet, ist eine Unwahrheit (Frage **D8**) |
| 2 | `Tab_Zone.IfcGuid`, `Tab_Bauteil.IfcGuid` mit `CHECK (Herkunft IN ('IFC','KATALOG','MANUELL','VORGABE'))` (Mehrzonenkonzept 4.2) | heißen **`Quellkennung`**; `Herkunft` bekommt **zusätzlich** `'GBXML'`, also `CHECK (Herkunft IN ('GBXML','IFC','KATALOG','MANUELL','VORGABE'))`. **`KATALOG` bleibt** — es trägt die Herkunft „aus dem Bauteil-/Baustoffkatalog kopiert". Die Spalten gibt es noch nicht — die Umbenennung kostet in Schemaschritt S-C nichts (Frage **D9**) |
| 3 | Der Import persistiert die Zuordnung heute nirgends | **Neue Anforderung**: `Tab_Importquelle` und `Tab_Importzuordnung` (Kapitel 7). Befund S, 6, Nr. 6 nennt sie als Voraussetzung des Round-Trips; sie trägt zugleich die Herkunft je Feld über den Lauf hinaus |
| 4 | Konzept 11 führt gbXML unter **G5** mit 10–15 PT, Befund C ordnet es in Stufe 3 ein | Nach **E9** ist der gbXML-Import **Pflicht in G4**. Befund R misst **14–22 PT** für das Lesen allein; mit der Persistenz aus Kapitel 7 werden daraus **17–28 PT** (Kapitel 10) |
| 5 | Der Stufenplan des Umsetzungskonzepts (Kapitel 4) endet bei G5 | **G7** kommt hinzu, mit fünf Teilstufen (Kapitel 10) |
| 6 | `IfcImportSatz.cs` / `IfcZuordnungsModell.cs` in `Import/Ifc/` (Umsetzungskonzept 3.3) | heißen **`GebaeudeImportSatz.cs`** / **`GebaeudeZuordnungsModell.cs`** und liegen in `Import/Gebaeude/`: Satz und Zuordnungsregeln beschreiben das **Ziel**, nicht die Datei, und beide Leser füllen dieselben Zielfelder (2.1) |
| 7 | **E7** nennt für das Mehrzonenmodell den IFC-Import; G6c führt nur `Z1…Z5` (Mehrzonenkonzept 9) | Die Zonenbildung aus gbXML (`X1…X3`) läuft mit **G6c** mit, `X4` (Einzonen-Rückfall) schon in **G4c** (3.3). Das erweitert E7 auf ein zweites Format — **entschieden mit E27 (D16): ja** |

---

## 2. Das gemeinsame Zuordnungsgerüst

### 2.1 Was gemeinsam ist und was je Format bleibt

Das Muster Ablauf / Profil / Satz liegt dreifach im Bestand (Befund N, 1.1) und gilt unverändert.
Neu ist nur die Aufteilung: **Was die Datei beschreibt, bleibt beim Format; was das Ziel beschreibt,
wandert eine Ebene höher.**

```
EPOS.Kern/Allgemein/Import/Gebaeude/          gemeinsam, formatfrei
    Importherkunft.cs        enum { Leer, Ifc, GbXml, Katalog, Vorgabe, Manuell } —
                             Wert, kein Anzeigetext
    GebaeudeQuelle.cs        Format, Dateiname, Hash, Groesse, Schema, Zeitpunkt, Programmfassung
    GebaeudeImportSatz.cs    je ZIELFELD eine Zeile: Zielfeld, Gruppe, Wert?, Einheit,
                             Importherkunft, Beleg, Uebernehmen
    GebaeudeZuordnungsModell.cs  HerkunftText, KopfText, ZeilenText, Pruefe — oberflaechenfrei

EPOS.Kern/Allgemein/Import/Ifc/               Leser IFC (Umsetzungskonzept 3.3, Befund N 4.2)
EPOS.Kern/Allgemein/Import/GbXml/             Leser gbXML (3.1 dieses Papiers)
EPOS.Kern/Allgemein/Export/GbXml/             GbXmlExportAblauf, GbXmlExportProfil
EPOS.Kern/Allgemein/Export/Ifc/               IfcExportAblauf, IfcExportProfil
```

Das Zwischenmodell (`IfcGebaeudeAbbild`, `GbXmlModell`) bleibt **je Format**, weil es die Datei
spiegelt und mit ihr veraltet. Der **Satz** ist gemeinsam, weil er die Zielfelder des Gebäude- bzw.
Zonenmodells spiegelt und beide Leser dieselben füllen. **Deshalb die Umbenennung** gegenüber
Umsetzungskonzept 3.3 (1.4, Nr. 6): `IfcImportSatz.cs` → `GebaeudeImportSatz.cs`,
`IfcZuordnungsModell.cs` → `GebaeudeZuordnungsModell.cs`, beide von `Import/Ifc/` nach
`Import/Gebaeude/`.

Drei Regeln des Bestands gelten wörtlich und werden hier nur benannt, nicht wiederholt: der Ablauf
zeigt nichts an (Zäsur statt Rückruf), ein fehlerhafter Eintrag bricht den Lauf nicht ab, und der
Zustand lebt im Ablauf, nicht in der Komponente (Befund N, 1.1). Dazu die Drei-Schichten-Regel:
„Kein Anzeigetext darf Steuerwert sein" — der Wortlaut steht im Klassenkopf von
`EPOS.Kern/Allgemein/Katalog/ImportKonfliktModell.cs:41-43`, die Klasse selbst auf `:45`, die
Beschriftungsfunktion `AktionText` auf `:48`.

Fehlerbilder sind **Meldungen, keine Ausnahmen**: `PruefStufe`
(`SpeicherEngine/GanglinienPruefung.cs:55`) und `PruefMeldung` (`:74` — Schlüssel und invariant
formatierte Werte, nie Text); die Schlüssel heißen
`IMP_IFC_PROT_*` bzw. **`IMP_GBXML_PROT_*`**. Fortschritt und Bilanz nehmen die vorhandenen
Bausteine `ImportFortschritt` (`EPOS.Kern/Allgemein/Import/KatalogImportAblauf.cs:9`) und
`ImportBilanz` (`:31`).

### 2.2 Herkunft je Feld

Jede Zahl, die in ein EPOS-Feld läuft, trägt zwei Angaben: **woher** (`Importherkunft`) und
**woraus** (`Beleg` — Entität, Eigenschaftssatz, Anzahl der zusammengefassten Bauteile). Die Regel
aus dem Mehrzonenkonzept 6.4 gilt für beide Formate wörtlich: *eine Zelle ohne Beleg ist eine
Vorgabe.*

| Wert | Bedeutung | Beleg |
|---|---|---|
| `Ifc` | aus der IFC-Datei gelesen | `IfcWall #1234 / Qto_WallBaseQuantities.GrossSideArea` |
| `GbXml` | aus der gbXML-Datei gelesen | `Surface aw-nord / RectangularGeometry 12,0 × 2,6 m` |
| `Katalog` | aus dem Bauteil-/Baustoffkatalog übernommen (Weg `CopyFromStamm`) | Katalogname |
| `Vorgabe` | aus der Baualtersklasse vorbelegt | Klassenname |
| `Manuell` | vom Anwender im Zuordnungsdialog geändert | — |
| `Leer` | nichts gefunden, nichts vorbelegt | — |

Die Herkunft wandert **mit der Zeile in die Datenbank** (`Tab_Zone.Herkunft`,
`Tab_Bauteil.Herkunft`, Mehrzonenkonzept 4.2) und bleibt damit auch nach dem Lauf sichtbar. Das ist
die Voraussetzung dafür, dass der Gebäudedialog nach **E2** neben U, A und U·A anzeigen kann, ob
eine Zahl gemessen, gelesen oder geraten ist.

### 2.3 Persistenz der Zuordnung

Die Paarung **EPOS-Zeile ↔ Quellobjekt** überlebt heute den Dialog nicht. Sie muss es, aus drei
Gründen: der Round-Trip (G7d) findet sonst nicht wieder, welcher `IfcSpace` zu welcher Zone gehört;
ein zweiter Import derselben Datei kann sonst nicht erkennen, was er schon einmal zugeordnet hat;
und die Herkunftsangabe „aus Datei X vom Y" ist ohne die Quelldatei nur die halbe Auskunft.

Gespeichert wird **je Lauf eine Quelle** und **je Paarung eine Zeile** (Tabellen in Kapitel 7).
Drei Festlegungen dazu:

- **Nur der Dateiname, nie der Pfad.** Absolute Sandbox-Pfade unter iOS enthalten eine GUID, die
  bei jeder Neuinstallation wechselt (Befund S, 4.5); ein gespeicherter Pfad wäre spätestens nach
  dem nächsten App-Update falsch.
- **SHA-256 des Dateiinhalts**, hexadezimal klein, 64 Zeichen. Er beantwortet die einzige Frage,
  die beim Round-Trip zählt: *Ist das dieselbe Datei?* Ein Zeitstempel beantwortet sie nicht.
- **Die Zuordnung ist Projektware und kaskadiert mit dem Gebäude.** Wird das Gebäude gelöscht,
  verschwindet sie mit — sie beschreibt nichts, was ohne das Gebäude noch Sinn hätte. Die Kaskade
  hat eine Falle, und sie ist benannt: **7 („Die Kaskadenfalle")**.

### 2.4 Ein Zuordnungsdialog für beide Formate

Der **mit G4a entstehende** Zuordnungsdialog `EPOS.UI/Dialoge/Bedarf/IfcZuordnungDialog.razor`
(Vorschlag Umsetzungskonzept 3.3; die Datei gibt es heute nicht, ebenso wenig
`IfcZuordnungDaten.cs` und `EPOS.UI.Daten/Bedarf/IfcImportHuelle.cs`) bekommt den gbXML-Weg
**mit**, statt eine zweite Maske zu erhalten. Der Aufbau bleibt: Kopf mit Datei, Format, Schema,
Gebäudewahl und Bilanz; Zeilenliste mit Gruppe, Feld, gelesenem Wert, Beleg, Vorgabewert, Herkunft
und Haken; Protokoll; OK/Abbrechen. Vier Dinge unterscheiden sich je Format und stehen deshalb als
**Daten** im Profil, nicht im Quelltext des Dialogs: Dateifilter, Größengrenze, Schemaanzeige und
die Liste der Zonierungsregeln (Z1…Z5 für IFC, X1…X4 für gbXML, 3.3 — in G4c steht davon nur
`X4` zur Wahl).

Die Hausregeln gelten unverändert: nichts wird ohne OK geschrieben, Abbrechen liefert `null` und
der Wirt verwirft, der Dialog gibt **alle** Zeilen zurück, auch die unveränderten. Beim
Mehrzonenimport kommen die vier Abschnitte aus Mehrzonenkonzept 6.4 dazu; sie sind formatfrei
formuliert und brauchen keine Ergänzung.

### 2.5 Der Plattformweg

Beide Richtungen laufen über die vorhandene Naht, ohne eine neue Schnittstelle:

| Richtung | Aufruf | Fundstelle |
|---|---|---|
| Datei wählen | `Dienste.Datei.DateiOeffnenAsync(titel, filter, startOrdner)` | `EPOS.Kern/Allgemein/Dienste/IDateiDienst.cs:107` |
| Datei schreiben | `Dienste.Datei.DateiSpeichernAsync(titel, filter, vorschlag)` | `IDateiDienst.cs:115` (synchrone Fassung `:25`) |
| Datei weiterreichen | `Dienste.Datei.MitSystemOeffnen(pfad)` | `IDateiDienst.cs:34` |
| ohne Oberfläche | `KeineDateiwahl` liefert `""`, der Aufrufer tut nichts | `EPOS.Kern/Allgemein/Dienste/KeineDateiwahl.cs:17-20` (Öffnen), `:23-26` (Speichern) |

**Die asynchronen Zwillinge sind Pflicht, nicht Geschmack** (Begründung im Kopf von
`IDateiDienst.cs:73-101`): ein synchroner Wähler pumpt unter Windows eine verschachtelte
Nachrichtenschleife, und auf iOS geht er vom Hauptfaden gar nicht erst auf. Der vorhandene
Exportweg zeigt beide Muster — `EPOS.Kern/Allgemein/Export/CsvExportClass.cs:91` ruft die synchrone Fassung aus einem
Nicht-Blazor-Zusammenhang, `EPOS.UI.Daten/Simulation/SimulationErgebnisHuelle.Wege.cs:598` die
asynchrone aus einer Hülle. **Die neuen Hüllen nehmen die asynchrone.**

Für iOS ist damit **keine neue Plattformarbeit nötig** (Befund S, 4.5): `SaveAsStep21` nimmt einen
`Stream`, `XDocument.Save` ebenfalls, und beide schreiben unmittelbar in den vom Dateidienst
gelieferten Pfad. Nachzutragen sind `[".ifcxml"] = "public.xml"` und
`[".ifczip"] = "public.zip-archive"` in `EPOS.iOS/Dienste/Dateifilter.cs:25-44` — beide Kennungen
sind dort für `.xml` (`:29`) und `.zip` (`:32`) schon geführt (ebenso Umsetzungskonzept 3.6). Für
`.ifc` gibt es **keine** registrierte Typkennung; dort bleibt `public.data` (`:22`) mit einem
Kommentar wie im `.lic`-Fall (`:37-40`). **Für gbXML ist nichts zu tun:** `.xml` ist bereits auf
`public.xml` abgebildet; einen neuen Eintrag braucht es nur, wenn der Wähler zusätzlich auf
`*.gbxml` hören soll.

**Größengrenze für beide Formate.** Für IFC steht sie mit 50 MB Windows / 20 MB iOS im
Umsetzungskonzept (Frage U11). Für gbXML ist sie **neu zu setzen**: die größte gemessene
Beispieldatei hat 16,3 MB und 648 885 Knoten und ist in 1 656 ms validierend durchgelesen
(Befund R, 4.3) — der Zeitbedarf ist unkritisch, der Speicherbedarf eines vollständigen `XDocument`
nicht. **Vorschlag: 25 MB Windows / 10 MB iOS**, benannt abgelehnt statt versucht; die iOS-Zahl ist
zu messen, nicht zu schätzen.

---

## 3. gbXML-Import (Stufe G4c, Pflicht)

### 3.1 Das Lesemodell

```
EPOS.Kern/Allgemein/Import/GbXml/
    GbXmlDatei.cs          Laden als STROM, Namensraum, Wurzelattribute
    GbXmlEinheiten.cs      globale und lokale unit-Attribute -> SI
    GbXmlModell.cs         Campus, Building, Space, Surface, Opening,
                           Construction, Layer, Material, WindowType, Zone, Location
    GbXmlImportAblauf.cs   Lesen / Zuordnen / Pruefen, Muster KatalogImportAblauf
    GbXmlImportProfil.cs   Dateifilter, MaxBytes, Zonenregel, Bänder, Hilfeschlüssel
```

`EPOS.Kern` benutzt bislang **keine** XML-API (Befund R, 4.3); gbXML ist die erste Nutzung.
`System.Xml.Linq` ist Teil der BCL — kein Paketeintrag, keine Laufzeitcodeerzeugung, keine
Trimming-Auflage. Namensraum `http://www.gbxml.org/schema`, `elementFormDefault="unqualified"`.

**Zwei Regeln, die den Leser tragen:**

1. **Die Datei wird als Strom geöffnet, nie als Text gelesen.** Zwei der vier Messdateien sind
   UTF-16LE mit BOM; `XmlReader.Create(Stream)` und `XDocument.Load(string)` kommen damit zurecht,
   `File.ReadAllText` mit erzwungener Kodierung nicht (Befund R, 2.1, Punkt 4). Ein Wächtertest
   über den Quelltext des Lesers hält `File.ReadAllText` und `XDocument.Parse` draußen.
2. **Es wird nicht validiert.** Keine der vier offiziellen Beispieldateien ist schemagültig; die
   Fehlerzahlen reichen von 88 bis 2 930 (Befund R, 2.3). Eine XSD-Prüfung beim Import würde jede
   reale Datei ablehnen. Der Leser entscheidet **je Feld**, ob der Wert brauchbar ist.

Gelesen wird **zweistufig**: erst die Wurzelkataloge (`Construction`, `Layer`, `Material`,
`WindowType`, `Zone`), dann die Flächen — denn `Surface` hängt nicht unter `Space`, sondern unter
`Campus`, und die Zuordnung läuft ausschließlich über `AdjacentSpaceId/@spaceIdRef` (Befund R, 1.2).

### 3.2 Einheiten

Die vier Pflichtattribute an der Wurzel (`lengthUnit`, `areaUnit`, `volumeUnit`, `temperatureUnit`)
gelten global; einzelne Elemente dürfen mit eigenem `unit`-Attribut abweichen. **Jede Größe wird
deshalb zweimal geprüft: lokales `unit` schlägt das globale.** Das ist nicht theoretisch — drei der
vier Messdateien sind in `Feet` geschrieben, zwei in Fahrenheit (Befund R, 2.1).

`unit` ist Pflicht bei `Conductivity`, `Density`, `SpecificHeat`, `U-value`, `R-value`,
`SolarHeatGainCoeff`, `Transmittance`, `PeopleNumber`, `LightPowerPerArea`, `EquipPowerPerArea` —
und **optional** bei `Area`, `Volume`, `Width`, `Height`, `Thickness`, `Temperature`; dort gilt das
globale Attribut (Befund R, 1.3). `Azimuth` und `Tilt` sind einheitenlos in Grad.

Ein unbekannter Aufzählungswert ist eine **Warnung mit Wert `null`**, kein Abbruch und keine
stillschweigende Annahme — `IMP_GBXML_PROT_EINHEIT_UNBEKANNT`.

Die Nordrichtung steckt in `Location/CADModelAzimuth` (optional, Dezimalgrad); fehlt sie, gilt 0
und **der Dialog sagt das** — dieselbe Regel wie `TrueNorth` beim IFC-Weg (Umsetzungskonzept 3.5,
Nr. 6). Breite und Länge sind schlichte Dezimalgrad und damit anders als
`IfcCompoundPlaneAngleMeasure` keine Fehlerquelle.

**Der Umgang mit `CADModelAzimuth` ist eine Annahme, keine belegte Regel.** Befund R (1.5) belegt
nur, dass das Attribut die Drehung des CAD-Modells gegen Nord beschreibt — **weder Vorzeichen noch
Nullbezug noch Drehsinn** sind dort gemessen, und ob gbXMLs Azimutdefinition mit der EPOS-Definition
(0° = Nord, im Uhrzeigersinn) zusammenfällt, steht ebenfalls nicht fest. Ein falsches Vorzeichen
dreht das Gebäude still. **Bis zur Gegenmessung** (eine Datei mit gesetztem `CADModelAzimuth` und
einer Fläche bekannter Lage; das Ergebnis gehört in dieses Papier) wird `CADModelAzimuth ≠ 0` als
**Warnung mit Anzeige im Dialog** behandelt und **nicht still aufaddiert**; die Probe „Azimutdrehung"
steht in Kapitel 9 (Probe 21).

### 3.3 Zonenbildung: Space und Zone auf EPOS-Zonen

`Zone` ist in gbXML **kein** Container für Räume, sondern ein Sollwert- und Anlagenobjekt; die
Verknüpfung läuft umgekehrt über `Space/@zoneIdRef` (Befund R, 1.2). Mehrere `Space` je `Zone` sind
die gbXML-Entsprechung des EPOS-Mehrzonenmodells — aber nur, wenn das Autorensystem sie auch so
benutzt. Die Auszählung sagt, dass es das oft **nicht** tut: der Revit-Export führt 93 `Zone` zu
93 `Space`, die Einfamilienhausdatei 12 zu 12, und nur `Urban_House_MEP` fasst 26 `Space` in **eine**
`Zone` (Befund R, 2.1).

Daraus folgt die Regelkette — dieselbe Bauform wie Z1…Z5 beim IFC-Weg (Mehrzonenkonzept 6.1): der
Leser bildet einen **Vorschlag**, legt die Regel offen und lässt umschalten.

| Rang | Regel | Voraussetzung | Anmerkung |
|---|---|---|---|
| **X1** | nach `Zone` (`Space/@zoneIdRef`) | mindestens eine `Zone`, **und Zahl der `Zone` < Zahl der `Space`** | Der Zusatz ist der Kern: 1:1 ist keine Gliederung, sondern Revits Begleitobjekt |
| **X2** | nach `BuildingStorey` (`Space/@buildingStoreyIdRef`) | Geschosse vorhanden und mehr als eines trägt Räume | die Entsprechung zu Z4, dem robustesten IFC-Vorschlag |
| **X3** | eine Zone je `Space` | immer | nur bei wenigen Räumen sinnvoll |
| **X4** | eine Zone je `Building` | immer | der **Einzonen-Rückfall**, und der Regelfall für G4c ohne G6 |

**Vorbelegung: X1, sonst X2, sonst X4.** Die Mindestgröße max(2 m², 2 % der Gebäudegrundfläche) und
die Obergrenze von 50 Zonen gelten unverändert (Mehrzonenkonzept 6.1, Fragen M8/M12); der
`IMP_GBXML_PROT_ZU_VIELE_ZONEN` ist eine benannte Ablehnung.

**Beheizt oder unbeheizt** liefert gbXML besser als IFC: `Space/@conditionType` kennt `Heated`,
`Cooled`, `HeatedAndCooled`, `Unconditioned`, `Vented`, `NaturallyVentedOnly` (Befund R, 1.7). Regel:
`Unconditioned`, `Vented` und `NaturallyVentedOnly` → `Tab_Zone.IstBeheizt = 0`, alles andere 1;
fehlt das Attribut, entscheidet der Name nach derselben Musterliste wie beim IFC-Weg (B4).

**Ohne G6 gibt es nur eine Zone.** Läuft G4c vor dem Mehrzonenmodell, ist **X4** die einzige
wählbare Regel und der Import schreibt in `Tab_Gebaeude` wie G4a. **X1 bis X3 entstehen mit G6c**
und werden dort zusammen mit Z1…Z5 freigeschaltet; G4c baut allein X4. Dass damit auch der
gbXML-Weg in Zonen führt, erweitert **E7** (dort: Mehrzonenmodell über den IFC-Import) auf ein
zweites Format — das ist mit **E27** entschieden (**D16**: ja; Zeile 7 in 1.4). Der Zuwachs von G6c (Mehrzonenkonzept 9,
heute 16–26 PT für Z1…Z5, B1…B6 und den Grundriss aus
Nachtrag 1) ist dort nachzuziehen.

### 3.4 Die Abbildung gbXML → EPOS

Grundlage ist die Tabelle aus Befund R, 5.1; hier steht sie auf die Tabellen des Mehrzonenkonzepts
(4.2) abgebildet. Spaltennamen sind von dort wörtlich übernommen.

| gbXML | EPOS-Ziel | Regel, Verlust |
|---|---|---|
| `Campus/Building` | `Tab_Gebaeude` | ein EPOS-Gebäude je `Building`; mehrere → Klappliste, **eines je Lauf** (Umsetzungskonzept 3.5, Nr. 1; Frage U13, **mit E38 nach Empfehlung entschieden**) |
| `Building/@buildingType` | — | nur Anzeige; `SingleFamily`/`MultiFamily` bestätigen die Wohnnutzung |
| `Space` (+ `Zone` über `@zoneIdRef`) | `Tab_Zone` (`Bezeichner`, `Rang`) | Aggregation nach 3.3; `Bezeichner` aus `Space/Name`, sonst `@id` |
| `Space/Area`, `/Volume` | `Tab_Zone.Nutzflaeche`, `.Volumen` | `Raumhoehe` = Volumen ÷ Fläche, wenn beides vorliegt; sonst NULL = Wert des Gebäudes. **E13 (16.09.2026):** die Zielgröße heißt durchgängig **Nutzfläche** (beheizte Netto-Grundfläche) — auf Gebäudeebene trägt sie weiter die Spalte `Tab_Gebaeude.Wohnflaeche` (Annahme, Frage Q11a; Konzept N1.17) |
| `Space/@conditionType` | `Tab_Zone.IstBeheizt` | 3.3 |
| `Zone/DesignHeatT`, `/DesignCoolT` | `Tab_Zone.Raumsolltemperatur_Tag`, `.Maximaleraumtemperatur` | fehlt oft; NULL = Wert des Gebäudes, **keine** Zahlenvorgabe im Import |
| `Space/AirChangesPerHour` | `Tab_Zone.Luftwechsel_Infiltration` | **eine** Zahl gegen zwei Spalten — `Luftwechsel_Nutzer` bleibt NULL (Frage **D12**). `InfiltrationFlow` (`Loose`/`Average`/`Tight`) ist für 1/h unbrauchbar |
| `Space/PeopleNumber` | `Tab_Zone.Bewohner` | drei Einheiten möglich (`NumberOfPeople`, `SquareMPerPerson`, `SquareFtPerPerson`) — umrechnen, nicht raten |
| `Space/LightPowerPerArea`, `/EquipPowerPerArea` | `Tab_Zone.Interne_Waermegewinne` | W/m² × Fläche, Einheit Pflicht |
| `Surface` | `Tab_Bauteil` (`Bezeichner`, `Rang`) | Zuordnung zur Zone nur über `AdjacentSpaceId` |
| `Surface/@surfaceType` | `Tab_Bauteil.Bauteilart` | Tabelle 3.5 |
| `RectangularGeometry/Width × Height` | `Tab_Bauteil.Flaeche` | **Bruttomaß** einschließlich der Öffnungen; **Fenster selbst abziehen** (3.6). In `Tab_Bauteil.Flaeche` steht danach die **Nettofläche** — das ist für den Export (5.5) zu beachten |
| `RectangularGeometry/Azimuth`, `/Tilt` | `Tab_Bauteil.Azimut`, `.Neigung` | Grad; `Location/CADModelAzimuth ≠ 0` wird **nicht still aufaddiert**, sondern gemeldet (3.2) |
| Anzahl `AdjacentSpaceId` + `@surfaceType` | `Tab_Bauteil.Randbedingung`, `.ID_Nachbarzone` | Tabelle 3.5 |
| `Construction` | `Tab_Bauteilaufbau` (`Bezeichner`, `Bauteilart`, `Quelle`, `Herkunft`) | je `Construction` ein Aufbau in der **Projektkopie**; `Quelle` = Dateiname |
| `Surface/@constructionIdRef` | `Tab_Bauteil.ID_Aufbau` | **Pflichtattribut** — fehlt es, Warnung und nur U-Wert |
| `Layer` → `Material` | `Tab_Bauteilschicht` (`Reihenfolge`, `Dicke`, `Lambda`, `Rho`, `cp`) | `Reihenfolge` zählt **innen → außen**, ab 1 (Mehrzonenkonzept 4.2); solange die Annahme „erste Schicht außen" gilt (3.7), wird die gbXML-Schichtfolge beim Schreiben **umgekehrt**. Ohne die Umkehr säße die Speichermasse auf der falschen Seite — genau die Größe, auf die es nach VDI 6007 ankommt |
| `Material` | `Tab_Baustoff` über Namensabgleich N1…N7 (Mehrzonenkonzept 3.5/6.3) | die Werte werden **an die Schicht kopiert**, nicht verwiesen |
| `Construction/U-value` | `Tab_Bauteil.U_Wert` | in den Messdateien nur 1 von 8 bzw. 9 von 9 gesetzt — meist aus den Schichten rechnen |
| `Opening` (`@openingType`) | eigene Zeile in `Tab_Bauteil`, `Bauteilart = 'FENSTER'` bzw. `'TUER'` | `FixedWindow`/`OperableWindow`/`FixedSkylight`/`OperableSkylight` → Fenster; `SlidingDoor`/`NonSlidingDoor` → Tür; `Air` → keine Fläche |
| `Opening/RectangularGeometry/Width × Height` | `Tab_Bauteil.Flaeche` | Außenmaß einschließlich Rahmen |
| `WindowType/U-value` **oder** `Opening/U-value` | `Tab_Bauteil.U_Wert` | **beide Orte prüfen**; nur `WPerSquareMeterK` und `BtuPerHourSquareFtF` sind zulässige Einheiten |
| `WindowType/SolarHeatGainCoeff` | `Tab_Bauteil.g_Wert` | bei mehreren mit `solarIncidentAngle` den bei 0° nehmen |
| — | `Tab_Bauteil.Rahmenanteil`, `.Verschattungsfaktor` | gbXML kennt beides nicht → NULL = Vorgabe (0,3 / 0,9) |
| `Location/Latitude`, `/Longitude`, `/Elevation` | — | nur Anzeige; die Klimaregion wählt der Anwender (Umsetzungskonzept 3.1) |
| — | `Tab_Bauteil.Psi_L`, Baualtersklasse | **fehlt in gbXML vollständig** → Vorgabe bzw. Anwenderangabe (3.8) |
| `Surface/@surfaceType="Shade"` | — | benannt übergangen; Verschattung durch Nachbarbebauung ist abgegrenzt |
| `Schedule`-Kette, `Results`, `AirLoop`/`AirSystem`, `Occupants`, `Behaviors` | — | in G4c nicht gelesen |

### 3.5 Bauteilart und Randbedingung

`surfaceTypeEnum` kennt 15 Werte, `Tab_Bauteil.Bauteilart` acht. Die Abbildung ist eindeutig:

| gbXML `surfaceType` | `Bauteilart` | `Randbedingung` |
|---|---|---|
| `ExteriorWall` | `AUSSENWAND` | `AUSSENLUFT` |
| `UndergroundWall` | `AUSSENWAND` | `ERDREICH` |
| `InteriorWall` | `INNENWAND` | aus der Nachbarschaft (unten) |
| `Roof` | `DACH` | `AUSSENLUFT` |
| `Ceiling`, `InteriorFloor` | `DECKE` | aus der Nachbarschaft |
| `UndergroundCeiling` | `DECKE` | `ERDREICH` |
| `SlabOnGrade`, `UndergroundSlab` | `BODENPLATTE` | `ERDREICH` |
| `ExposedFloor`, `RaisedFloor` | `BODENPLATTE` | `AUSSENLUFT`, mit Hinweiszeile |
| `Air` | — | **keine Fläche**, sondern ein Grund, zwei Räume zusammenzulegen (wie `VIRTUAL` beim IFC-Weg) |
| `Shade` | — | übergangen |
| `FreestandingColumn`, `EmbeddedColumn` | `SONSTIGES` | **nicht** zur Außenwand zählen |

Die Randbedingung ergibt sich aus der Zahl der `AdjacentSpaceId` und dem Typ (Befund R, 1.7); die
Regel hielt in allen vier Messdateien:

| `AdjacentSpaceId` | Lage | `Tab_Bauteil.Randbedingung` |
|---|---|---|
| 1, Außentyp | Außenluft | `AUSSENLUFT` |
| 1, Untergrundtyp | Erdreich | `ERDREICH` |
| 2, beide Räume in **derselben** EPOS-Zone | innere Masse | Zeile entfällt, Fläche zählt in A_IW |
| 2, verschiedene Zonen, Nachbarzone beheizt | Trennfläche | `ZONE` mit `ID_Nachbarzone` |
| 2, Nachbarzone unbeheizt | Trennfläche | `ZONE` auf die unbeheizte Zone (Mehrzonenkonzept 2.5) |
| 2, `spaceIdRef` zeigt ins Leere | unbekannt | `UNBEHEIZT`, Zeile rot, `IMP_GBXML_PROT_NACHBAR_UNBEKANNT` |
| 0, `surfaceType='Shade'` | Verschattung | übergangen |
| 0, **anderer** `surfaceType` | unbekannt | Zeile **rot**, Fläche ohne Zone, `IMP_GBXML_PROT_OHNE_NACHBAR`. Das Schema erzwingt `AdjacentSpaceId` nicht (Befund R, 1.3); eine Außenwand ohne Nachbarangabe darf nicht still verschwinden |

`AdjacentSpaceId` kann ein eigenes `surfaceType`-Attribut tragen — die Sicht *dieses* Raumes auf die
Fläche (Boden der einen, Decke der anderen Zone). Der Leser nimmt sie, wenn sie da ist; sie
entscheidet über oben und unten, wo die Neigung es nicht tut. `exposedToSun` (Vorgabe `true`) ist
eine davon unabhängige Angabe und wird nur zur Gegenprobe herangezogen.

**Die Gegenprobe gehört in den Leser**, dieselbe wie beim IFC-Weg (Mehrzonenkonzept 6.2): Σ Flächen
nach unten ≈ Σ Flächen nach oben je Zone, und jede Trennfläche muss von beiden Seiten dieselbe
Größe haben. Weicht sie um mehr als 2 % ab, wird die größere genommen und gemeldet.

### 3.6 Aufbauten, Schichten, Stoffwerte

Die Kette ist flach und über IDREF verkettet: `Surface/@constructionIdRef` → `Construction` →
`LayerId/@layerIdRef` → `Layer` → `MaterialId/@materialIdRef` → `Material`. **Der Leser löst sie
selbst auf** — `constructionIdRef`, `layerIdRef`, `materialIdRef` und `windowTypeIdRef` — und meldet
jeden Verweis ins Leere als Warnung (`IMP_GBXML_PROT_VERWEIS_LEER`), kein Abbruch. Die
IDREF-Integrität prüft das Schema zwar, aber **beim Import findet keine Schemaprüfung statt** (3.1,
Regel 2); es prüft also niemand außer dem Leser.

**Die wichtigste Prüfung ist je Aufbau, nicht je Material.** `Material` kann statt der vier
Stoffwerte auch nur ein `R-value` führen — gemessen bei 7 von 18, 9 von 21 bzw. 2 von 16 Stoffen
(Befund R, 2.1). Eine einzige solche Schicht macht die Wärmespeicherfähigkeit des **ganzen** Aufbaus
unbrauchbar, denn die Masse fehlt dort, wo sie sitzt. Regel:

1. Alle Schichten vollständig (`Thickness`, `Conductivity`, `Density`, `SpecificHeat`) → Aufbau
   wird geschrieben, Herkunft `GbXml`.
2. Mindestens eine Schicht nur mit `R-value` → **`IMP_GBXML_PROT_AUFBAU_MASSELOS` (Warnung)**; der
   U-Wert wird aus der Schichtung gerechnet und geschrieben, die Masse kommt aus `Bauweise` bzw.
   dem Baustoffkatalog, die Zeile ist gelb und trägt Herkunft `Vorgabe` in den Stoffwerten.
3. Gar keine `Construction` → **`IMP_GBXML_PROT_KEINE_KONSTRUKTIONEN` (Warnung)**, alle U-Werte
   aus der Baualtersklasse, Herkunft `Vorgabe`. **Das ist kein Sonderfall:** der reine
   Architekturexport ist ein Viertel der Stichprobe — 1 775 Flächen mit vollständiger Geometrie und
   null Konstruktionen, null Materialien, null Fenstertypen (Befund R, 2.1, Punkt 1).

**Der Namensabgleich ist der Regelweg, nicht der Notweg** — dieselbe Kette N1…N7 mit zweisprachiger
Synonymtabelle wie beim IFC-Import (Mehrzonenkonzept 3.5/6.3). Der Unterschied ist erfreulich: bei
gbXML ist er meist nur die Gegenprobe, weil zwei Drittel der Stoffe ihre Werte mitbringen; bei IFC
ist er die einzige Quelle.

**Plausibilitätsband:** λ, ρ, c ≤ 0 ist **kein Wert**, sondern eine Fehlstelle — das ist die Lehre
aus den mit Nullen gefüllten IFC-Dateien (Befund P, 0, Nr. 6) und gilt hier genauso.

**Fensterabzug.** `RectangularGeometry` liefert die Wandfläche **einschließlich** der Öffnungen; die
`Opening`-Kinder liegen zusätzlich darauf. Regel wie beim IFC-Weg (Umsetzungskonzept 3.5, Nr. 10):
A_Wand = Σ Wandfläche − Σ A_Fenster − Σ A_Außentür derselben Fläche; wird das negativ, A = 0, Zeile
rot, `IMP_GBXML_PROT_NETTOFLAECHE_NEGATIV`. **Der Bruttowert wird mitgeführt** (Wandfläche zuzüglich
ihrer Öffnungen), weil der Export ihn braucht (5.5, Punkt 2). Der Abzug ist die Antwort auf Frage
**U14** des Umsetzungskonzepts, **mit E38 (24.09.2026) nach Empfehlung entschieden** — für beide
Importwege.

### 3.7 Was gbXML nicht sagt

Drei Angaben fehlen im Schema und müssen anders entstehen:

| Fehlend | Warum es fehlt | Wie vorbelegt wird |
|---|---|---|
| **Baualtersklasse / Baujahr** | gbXML hat kein Gegenstück zu `Pset_BuildingCommon.YearOfConstruction` (Befund R, 5.1) | **Anwenderangabe im Zuordnungsdialog**, Klappliste über die 21 Klassen; sie steuert alle U-Wert- und ψ-Vorgaben und ist deshalb das erste Feld des Dialogs |
| **Wärmebrückenzuschlag ψ·L** | kein Ziel im Schema | ψ als Vorgabe je Baualtersklasse, Anschlusslängen **leer** — dieselbe Regel wie beim IFC-Weg (Umsetzungskonzept, Frage U15, **mit E38 nach Empfehlung entschieden**; Herkunftsmarke `Vorgabe` bzw. `Leer`) |
| **Schichtrichtung innen/außen** | `Construction` sagt die Reihenfolge, aber nicht, welches Ende raumseitig ist | **Annahme: erste Schicht außen** — wie die **benannte EPOS-Annahme** beim IFC-Weg ohne `…Usage` (Mehrzonenkonzept 6.3: die Spezifikation gibt dort **keine** Lage an; es ist keine Normvorgabe, sondern eine Hausannahme mit 50 % Irrtumswahrscheinlichkeit je Bauteil). Die Annahme wird **markiert**, ist im Aufbaueditor umkehrbar, und weil `Tab_Bauteilschicht.Reihenfolge` innen → außen zählt, wird die gelesene Folge beim Schreiben **umgekehrt** (3.4) |

Die Nutzungssemantik geht zusätzlich verloren: `spaceTypeEnum` hat 126 Werte, und **kein einziger**
enthält „Residential" (Befund R, 1.9). Für die EPOS-Zielgruppe gibt es also keine passende
Raumnutzung; `@spaceType` wird beim Import nur als Hinweistext angezeigt und nirgends hin abgebildet.

### 3.8 Fehlerbilder

Alle als `PruefMeldung` mit Schlüsseln in **beiden** `.resx`, danach `ResourceDesigner`
(`python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben`).

| Bild | Erkennung | Wirkung | Schlüssel | Stufe |
|---|---|---|---|---|
| Datei zu groß | Größe > `MaxBytes` | Lauf endet | `IMP_GBXML_PROT_ZU_GROSS` | F |
| Lesefehler | `XmlException` gefangen, `ex.Message` als Wert | Lauf endet | `IMP_GBXML_PROT_LESEFEHLER` | F |
| kein `Campus`/`Building` | Wurzel ohne Kinder | Lauf endet | `IMP_GBXML_PROT_KEIN_CAMPUS` | F |
| Versionswert unbekannt | `@version` außerhalb `versionEnum` | **kein Abbruch** — der Versionswert steuert nichts am Lesen. Die vier Messdateien schreiben `0.37`, einen **gültigen** Wert; selbst ein ungültiger (etwa `8.01`, wie ihn neuere Werkzeuge schreiben können) wird nur vermerkt | `IMP_GBXML_PROT_VERSION_UNBEKANNT` | I |
| Einheit unbekannt | `unit` außerhalb des Aufzählungstyps | Wert `null`, Feld leer | `IMP_GBXML_PROT_EINHEIT_UNBEKANNT` | W |
| keine Konstruktionen | null `Construction` | U-Werte aus der Klasse | `IMP_GBXML_PROT_KEINE_KONSTRUKTIONEN` | W |
| `constructionIdRef` fehlt | Pflichtattribut nicht gesetzt (1 775 bzw. 54 Fälle in den Messdateien) | nur U-Wert, kein Aufbau | `IMP_GBXML_PROT_OHNE_AUFBAU` | W |
| Aufbau masselos | eine Schicht nur mit `R-value` | 3.6, Punkt 2 | `IMP_GBXML_PROT_AUFBAU_MASSELOS` | W |
| Nachbar unbekannt | `spaceIdRef` ohne Ziel | `UNBEHEIZT`, Zeile rot | `IMP_GBXML_PROT_NACHBAR_UNBEKANNT` | W |
| ohne Nachbarangabe | null `AdjacentSpaceId` bei anderem `surfaceType` als `Shade` | Fläche ohne Zone, Zeile rot | `IMP_GBXML_PROT_OHNE_NACHBAR` | W |
| Verweis ins Leere | `constructionIdRef`/`layerIdRef`/`materialIdRef`/`windowTypeIdRef` ohne Ziel | Kette bricht an dieser Stelle ab, Rest wird gelesen | `IMP_GBXML_PROT_VERWEIS_LEER` | W |
| Kennung zu lang | gbXML-`id` über 64 Zeichen | gekürzt und um acht Zeichen SHA-256-Präfix ergänzt (7.2) | `IMP_GBXML_PROT_KENNUNG_GEKUERZT` | I |
| Nordangabe gesetzt | `CADModelAzimuth ≠ 0` | **nicht** aufaddiert; der Dialog zeigt den Wert (3.2) | `IMP_GBXML_PROT_NORDDREHUNG` | W |
| Geometrie fehlt | weder `RectangularGeometry` noch auswertbare `PlanarGeometry` | Fläche leer, Zeile rot | `IMP_GBXML_PROT_GEOMETRIE_FEHLT` | W |
| Trennflächenbilanz | A→B ≠ B→A um mehr als 2 % | beide zeigen, größere nehmen | `IMP_GBXML_PROT_TRENNFLAECHE_UNGLEICH` | W |
| Nettofläche negativ | A_Wand < 0 nach Fensterabzug | A = 0, Zeile rot | `IMP_GBXML_PROT_NETTOFLAECHE_NEGATIV` | F |
| zu viele Zonen | N > 50 | benannte Ablehnung | `IMP_GBXML_PROT_ZU_VIELE_ZONEN` | F |
| kein Norden | `CADModelAzimuth` fehlt | Annahme 0°, **der Dialog sagt es** | `IMP_GBXML_PROT_KEIN_NORDEN` | W |

---

## 4. IFC-Import — was der Datenaustausch ergänzt

Der IFC-Import steht vollständig im Umsetzungskonzept (Kapitel 3, Stufe G4a) und im
Mehrzonenkonzept (Kapitel 6, Stufe G6c). Hier stehen **vier Ergänzungen**, die erst der
Datenaustausch braucht — sonst nichts.

| # | Ergänzung | Grund | Stufe |
|---|---|---|---|
| **1** | **Persistenz der Zuordnung.** Nach dem OK schreibt der Ablauf eine Zeile in `Tab_Importquelle` und je zugeordnetem Objekt eine in `Tab_Importzuordnung` (Kapitel 7) | Voraussetzung für den Round-Trip G7d (Befund S, 6, Nr. 6) und für die Auskunft „aus welcher Datei stammt diese Zahl" | **G4a** — nicht später, sonst fehlt die Zuordnung für alle vorher importierten Gebäude |
| **2** | **`Importherkunft` statt `IfcHerkunft`**, Wert `Ifc` unverändert, Wert `GbXml` neu; `Tab_*.Herkunft` bekommt `'GBXML'` in den Wertebereich, `IfcGuid` heißt `Quellkennung` | ein Dialog, ein Wertebereich, zwei Formate (1.4) | **G4a** bzw. Schemaschritt S-C |
| **3** | **Protokollprüfung auf Entitätenverlust.** Der `ILoggerFactory`, den `MemoryModel` ohnehin entgegennimmt, liefert den Mitschnitt; der Ablauf zählt **beide** Verlustkanäle — `FailedEntity`-Einträge **und** Warnungen `Entity #… is referenced but could not be instantiated` — und legt die **Summe** in `Tab_Importquelle.FehlendeEntitaeten` ab. Dazu ein Wächtertest: `ignoreTypes`/`SkipTypes` wird beim Import **nie** gesetzt | Die Rückgabe einer Fremddatei (G7d) ist nur zulässig, wenn beim Lesen **keine** Entität verlorenging (Befund S, 3.2). Wer nur `FailedEntity` zählt, gibt eine Datei trotz grüner Sperre beschädigt zurück | **G4a** (zählen), **G7d** (sperren) |
| **4** | **SHA-256 der Eingangsdatei** beim Lesen bilden und mitschreiben | beantwortet beim Round-Trip die einzige Frage, die zählt: *dieselbe Datei?* | **G4a** |

Alles Übrige gilt unverändert: Ablauf `Lesen`/`Uebernehmen`, Schlüssel `IMP_IFC_PROT_*`,
Einheitenauflösung über `IIfcProject.UnitsInContext`, Azimutkette mit `TrueNorth` und
`IfcMapConversion`, Größengrenze, Paketwahl `Xbim.IO.MemoryModel` ohne Esent, iOS-Trimming.

---

## 5. gbXML-Export (Stufe G7a und G7b)

### 5.1 Der Vorbehalt vorweg

**Ohne Stufe 2 hat der Export keinen Wert für Simulationswerkzeuge.** Das ist die zentrale Aussage
von Befund R (6) und trägt die Entscheidung, ob G7 sich lohnt:

- **OpenStudio/EnergyPlus** liest `RectangularGeometry` **gar nicht** — null Vorkommen im
  `ReverseTranslator`, während der `ForwardTranslator` es 18-mal schreibt (Befund R, 1.4).
- **IES VE** verlangt ausdrücklich `PolyLoop`, geschlossene Raumvolumen und Kantenübereinstimmung
  auf 1 mm; `AdjacentSpaceId` kommt in der Pflichtliste nicht einmal vor.
- **DesignBuilder** ist die toleranteste Implementierung — aber auch sie *repariert* Polygone, sie
  *erfindet* keine.
- **Solar-Computer (GBIS) und EVEBI** rechnen nach DIN V 18599 bzw. Heizlast und brauchen primär
  Flächen, U-Werte und Orientierungen; hier ist ein geometriearmer Export am ehesten verwertbar —
  **belegt ist das nicht** (die Hersteller dokumentieren den Import aus AutoCAD/Revit, nicht aus
  beliebigen Quellen).

Daraus folgt **D2**, entschieden mit E27: **Der gbXML-Export kommt erst mit Stufe 2** — G7a und
G7b werden zusammen gebaut. G7a allein wäre ein Datenblatt in XML-Form — legitim als Beleg- und
Archivformat und als Grundlage des Rundlauf-Regressionstests, aber keine Interoperabilität.

### 5.2 Stufe G7a — der schemagültige Datenexport

**Die Wurzel.** Sechs Pflichtattribute, und eines davon ist eine Falle:

```
<gbXML version="6.01" temperatureUnit="C" lengthUnit="Meters"
       areaUnit="SquareMeters" volumeUnit="CubicMeters" useSIUnitsForResults="true">
```

`version="8.01"` ist nach dem eigenen Schema **ungültig** — `versionEnum` endet bei `6.01`, der im
Änderungsprotokoll zu 7.03/7.04 angekündigte Nachtrag wurde nie ausgeführt (Befund R, 1.1). **In den
Quelltext gehört ein Kommentar mit dieser Begründung**, sonst „korrigiert" das früher oder später
jemand.

**Pflichtangaben, empirisch bestimmt** (Befund R, 1.3) — was der Exporteur liefern muss:

| Ebene | Pflicht | Wie EPOS es füllt |
|---|---|---|
| `Campus` | `id`; **mindestens 4 `Surface`** | Ein Einzonen-Quader mit sechs Bauteilgruppen erfüllt das; ein reduziertes Modell mit drei Gruppen nicht → Meldung statt schemawidriger Datei |
| `Building` | `id`, `buildingType` | `SingleFamily` bzw. `MultiFamily` aus dem EPOS-Gebäudetyp; bei Nichtwohnnutzung der nächstliegende Wert, benannt |
| `Location` | **`ZipcodeOrPostalCode`** (Koordinaten dagegen optional) | deutsche PLZ aus dem Projekt; **keine erfundene PLZ**. Fehlt sie, entfällt das `Location`-Element — und **mit ihm Koordinaten, Höhe und `CADModelAzimuth`**, also jede Orts- und Nordangabe. Der Exportdialog weist darauf hin, der Verzicht steht in `Campus/Description`. Dass `Location` unter `Campus` selbst optional ist, ist **am XSD nachzumessen** (Befund R, 1.3 belegt nur den Pflichtinhalt *innerhalb* von `Location`); ergibt die Messung das Gegenteil, wird die PLZ **Pflichteingabe des Exportdialogs** |
| `Space` | `id` | 5.4 |
| `Surface` | `id`, `surfaceType`, **`constructionIdRef`** | jede Fläche bekommt einen Aufbau — auch dann, wenn EPOS nur einen U-Wert führt (5.3) |
| `Opening` | `id`, `openingType` | `FixedWindow` bzw. `NonSlidingDoor` |
| `Construction`/`Layer`/`Material`/`WindowType`/`Zone` | `id` | 5.4 |
| Stoffwerte | `unit` bei `Conductivity`, `Density`, `SpecificHeat`, `U-value`, `SolarHeatGainCoeff`, `PeopleNumber`, `LightPowerPerArea`, `EquipPowerPerArea` | `WPerMeterK`, `KgPerCubicM`, `JPerKgK`, `WPerSquareMeterK` — als C#-Konstanten geführt, nicht als Zeichenketten am Schreibort |

**Was geschrieben wird**, mit der Abbildung aus Befund R, 5.2 auf die Tabellen des
Mehrzonenkonzepts:

| EPOS | gbXML |
|---|---|
| `Tab_Gebaeude` | `Campus` + `Building` + `Location` + `Location/CADModelAzimuth = 0` (EPOS-Azimute sind bereits geografisch) |
| `Tab_Zone` | `Space` (`Area`, `Volume`, `AirChangesPerHour` ← **`Luftwechsel_Infiltration`**, `PeopleNumber`, `LightPowerPerArea`, `EquipPowerPerArea`) + `Zone` (`DesignHeatT`, `DesignCoolT`), verbunden über `@zoneIdRef`. `Luftwechsel_Nutzer` hat kein Ziel im Schema und wird **benannt weggelassen** — die Umkehrung von **D12** |
| `Tab_Zone.IstBeheizt` | `Space/@conditionType` = `Heated` bzw. `Unconditioned` |
| — | **`Space/@spaceType` wird weggelassen** — kein Wohnwert im Aufzählungstyp (Befund R, 1.9); die Nutzung drückt sich in den Zahlen aus |
| `Tab_Bauteil` | `Surface` mit `@surfaceType`, `@constructionIdRef`, `RectangularGeometry` (`Width`, `Height`, `Azimuth`, `Tilt`) — Breite aus Fläche ÷ Höhe |
| `Tab_Bauteil.Randbedingung` | `AdjacentSpaceId`: 1× bei `AUSSENLUFT`/`ERDREICH`, 2× bei `ZONE`/`UNBEHEIZT` (Umkehrung der Tabelle in 3.5) |
| `Tab_Bauteil` FENSTER/TUER | `Opening` im Wirtsbauteil + `WindowType` (`U-value`, `SolarHeatGainCoeff`) |
| `Tab_Bauteilaufbau` | `Construction` mit `LayerId`-Kette und zusätzlich `U-value` |
| `Tab_Bauteilschicht` | `Layer` → `Material` (`Thickness`, `Conductivity`, `Density`, `SpecificHeat`) — **Schreibrichtung: `Reihenfolge` 1 (raumseitig) steht als letztes `Layer`**, weil gbXML nach der Annahme aus 3.7 mit der äußeren Schicht beginnt. Export und Import kehren also beide um; Probe 1 prüft das an einem unsymmetrischen Aufbau (Innendämmung) |
| Ergebnisse | `Results` — **erst Stufe 2** (5.5) |
| Anlagentechnik | `AirLoop`, `HydronicLoop`, `AirSystem`, `ZoneHVACEquipment` — **wird nicht geschrieben**; das EPOS-Anlagenmodell passt nicht auf das US-HVAC-Schema |

### 5.3 Die Aufbauten — und der Fall „EPOS hat nur einen U-Wert"

**Wer nur U-Werte je Bauteilgruppe schreibt, erzeugt im Zielwerkzeug ein masseloses Gebäude.** Der
Quelltext von OpenStudio zeigt die Folge unmittelbar: mit `Density`, `Conductivity`, `Thickness`
und `SpecificHeat` entsteht ein `StandardOpaqueMaterial` mit Speichermasse, nur mit `R-value` ein
`MasslessOpaqueMaterial`, ohne alles ein Stummel mit R = 0,001 (Befund R, 1.6). Für eine stationäre
Heizlast ist masselos richtig, für jede dynamische Rechnung — und damit für alles, was VDI 6007
ausmacht — ist es falsch.

Hat EPOS die Schichten (nach G3 bzw. G6), werden sie vollständig geschrieben. Hat es nur den U-Wert
und die `Bauweise`, bleiben zwei Wege (Frage **D10**):

- **Ersatzschichtung, gekennzeichnet (Empfehlung).** Eine Schicht, deren λ, ρ, c und Dicke so
  gewählt sind, dass Wärmedurchlasswiderstand **und** flächenbezogene Wärmekapazität des Aufbaus
  getroffen werden. `Construction/Name` und `Construction/Description` sagen, dass es eine
  Ersatzschichtung ist; `Material/Name` trägt denselben Vermerk. **Der Vorbehalt gehört
  danebengeschrieben:** Die Ersatzschichtung trifft U-Wert und Gesamtwärmekapazität, **nicht die
  Lage der Masse im Aufbau** — und davon hängt das dynamische Verhalten ab (Dämmung innen oder außen
  ergibt bei gleichem U und gleicher Kapazität eine andere Antwort). Das Zielwerkzeug rechnet also
  weiterhin etwas anderes als EPOS: nicht masselos, aber falsch verteilt. Dieser Satz gehört
  **wörtlich** in `Construction/Description` und in die Meldung an den Anwender. Die Ersatzschichtung
  ist ein Vorschlag dieses Papiers; Befund R empfiehlt sie nicht, sondern nur, den Fall zu melden.
- **Nur `U-value`, masselos, mit Meldung.** Ehrlicher im Buchstaben, im Ergebnis irreführender: das
  Zielwerkzeug rechnet klaglos weiter, nur falsch.

In beiden Fällen gilt: Der Fall wird dem Anwender **vor** dem Schreiben gemeldet, nicht danach.

### 5.4 Kennungen

`id` ist in gbXML vom Typ `xsd:ID`; das Schema prüft die Verweisintegrität und meldet
`Reference to undeclared ID` (Befund R, 1.3). Zwei Folgen: Kennungen dürfen **nicht mit einer Ziffer
beginnen**, und sie müssen im Dokument eindeutig sein.

**Regel — deterministisch aus den EPOS-Schlüsseln**, dieselbe Linie wie die IFC-GUIDs (6.4):

```
epos-zone-<Tab_Zone.ID>        epos-bauteil-<Tab_Bauteil.ID>     epos-oeffnung-<Tab_Bauteil.ID>
epos-aufbau-<Tab_Bauteilaufbau.ID>   epos-schicht-<Tab_Bauteilschicht.ID>
epos-stoff-<Tab_Baustoff.ID>   epos-fenstertyp-<Tab_Bauteilaufbau.ID>
```

Damit trägt ein zweiter Export desselben Projekts dieselben Kennungen, und der Rundlauf
Export → Import findet seine Zeilen wieder.

**Fremde Kennungen können länger sein.** Eine gbXML-`id` ist `xsd:ID` (NCName) und in der Länge
unbegrenzt; `Tab_Importzuordnung.Quellkennung` fasst 64 Zeichen (7.2). Regel beim **Import**:
Kennungen über 64 Zeichen werden auf 56 Zeichen gekürzt und um acht Zeichen eines SHA-256-Präfixes
der vollen Kennung ergänzt; der Fall wird als Hinweis protokolliert
(`IMP_GBXML_PROT_KENNUNG_GEKUERZT`). Ein Importabbruch aus einem Formatgrund, den niemand erwartet,
wäre die schlechtere Antwort.

### 5.5 Stufe G7b — synthetische Quadergeometrie

Das ist der einzige Weg, der den Export für die Simulationswerkzeuge brauchbar macht, und er
braucht **keinen Geometriekernel** — nur Rechenarbeit (Befund R, 3). **Die Geometrie stammt aus dem
Zonengeometrie-Modell (Nachtrag 1, Kapitel 14); G7b schreibt sie nur noch.** Die Regeln unten bleiben
die Herleitung — sie steht im Kern, nicht im Exporteur.

**Die Regel je Zone:**

1. **Raumhöhe** h = `Volumen` ÷ `Nutzflaeche`; fehlt eines, h = `Tab_Zone.Raumhoehe` bzw. die des
   Gebäudes.
2. **Grundriss** aus den Bauteilgruppen, nicht geraten: Mit A_NS = Summe der Nord- und
   Südwandflächen und A_OW = Summe der Ost- und Westwandflächen folgt l = A_NS ÷ (2·h) und
   b = A_OW ÷ (2·h). **A_NS und A_OW sind Bruttoflächen** — Wandfläche **zuzüglich** der zugehörigen
   Öffnungen (`Tab_Bauteil.Flaeche` der Wand + Σ Fenster und Türen derselben Wand). In
   `Tab_Bauteil.Flaeche` steht nach dem Import die **Nettofläche** (Fensterabzug, 3.6); mit ihr
   fielen l und b um den Fensteranteil zu klein aus, l·b unterschritte die Nutzfläche systematisch
   und die 10-%-Probe schlüge im Regelfall an. **Probe:** l · b gegen `Nutzflaeche`. Weicht sie um mehr als 10 % ab, wird das
   Rechteck aus `Nutzflaeche` und dem Verhältnis l : b gebildet und die Abweichung gemeldet — der
   Flächeninhalt der Zone ist die härtere Größe. Fehlen Wandflächen ganz, gilt das Quadrat
   (l = b = √A).
3. **Anordnung:** **keine Stapelung** — die Geschosszuordnung ist im Einzonenmodell nicht bekannt
   und im Mehrzonenmodell nur als Reihenfolge (dieselbe Regel wie bei den IFC-Körpern, Befund S,
   2.3). **Aber: Zonen, die eine Trennfläche teilen, werden aneinandergelegt**, so dass die
   gemeinsame Fläche geometrisch dieselbe ist. Eine Trennfläche ist in gbXML **eine** `Surface` mit
   zwei `AdjacentSpaceId` (5.2, Umkehrung von 3.5); sie kann nicht gleichzeitig auf zwei räumlich
   getrennten Quadern liegen, und IES VE verlangt je Raum ein geschlossenes Volumen mit
   Kantenübereinstimmung auf 1 mm (Befund R, 1.4). Nur Zonen **ohne** gemeinsames Bauteil dürfen mit
   sichtbarem Abstand stehen. Ist aus den Bauteilgruppen keine widerspruchsfreie Anordnung
   ableitbar, wird **G7b für das Mehrzonenmodell benannt abgelehnt** (Meldung) und nur das
   Einzonenmodell exportiert — ein Quaderfeld mit doppelt gezählten Trennflächen wäre schlimmer als
   kein Stufe-2-Export.
4. **Flächen:** vier Wände, Boden, Decke; je Fläche vier `CartesianPoint` aus **demselben
   Eckpunktsatz**, damit die 1-mm-Kantenregel von IES VE von selbst erfüllt ist. Umlaufsinn so, dass
   die Normale nach außen zeigt. Die `PolyLoop` steht in `PlanarGeometry`; `RectangularGeometry`
   bleibt **zusätzlich** stehen, weil alle Autorensysteme beides schreiben (Befund R, 2.1, Punkt 3).
5. **Dachschrägen:** Eine geneigte Fläche ersetzt den Rechteckring auf der zugehörigen Seite; ist
   keine Geometrie ableitbar, bleibt das Dach waagerecht und die Neigung steht in
   `RectangularGeometry/Tilt` — mit Vermerk.
6. **Fenster** als Rechteck **in** der Wandfläche, mittig, Randabstand ≥ 0,1 m, Seitenverhältnis der
   Wand. Passt die Fensterfläche nicht hinein, wird sie auf 90 % der Wandfläche begrenzt und
   gemeldet — eine Öffnung größer als ihre Wand ist ein Datenfehler, kein Geometrieproblem.
7. **`SurfaceReferenceLocation`** wird nur gesetzt, wenn die Bemaßung bekannt ist:
   `InteriorSurface` beim Raumseitenmaß aus einem Zonenimport (Mehrzonenkonzept 6.2, Frage M2),
   sonst weggelassen. Der Export behauptet nichts, was er nicht weiß.
8. Zusätzlich darf `Space/ShellGeometry/ClosedShell` geschrieben werden — IES VE nennt es als
   gleichwertige Alternative zu `Surface/PlanarGeometry/PolyLoop`.

**Was dieser Export leistet:** korrekte Flächen, Orientierungen, U-Werte, vollständige
Schichtaufbauten mit λ/ρ/c, Zonensollwerte, Luftwechsel und innere Gewinne — genug für eine
plausible Heizlast und einen plausiblen Jahresbedarf im Zielwerkzeug.

**Was er nicht leistet:** echte Verschattung durch Eigen- und Nachbarbebauung, echte Wärmebrücken,
echte Raumgeometrie, jede Tageslichtrechnung.

**Kennzeichnung — Pflicht, nicht Kür.** Der Vermerk steht an drei Stellen: `Campus/Description`,
`Building/Description` und im Dialog, der den Export auslöst. Wortlaut sinngemäß: *„Ersatzmodell:
Flächen, Orientierungen und Aufbauten sind die des EPOS-Gebäudemodells; die Raumgeometrie ist
schematisch erzeugt und bildet den tatsächlichen Grundriss nicht ab."*

### 5.6 `Results` — technisch möglich, praktisch fragwürdig

gbXML hat Ergebnisfelder: `resultsTypeEnum` enthält `HeatLoad`, `CoolingLoad`, `Energy`, `Power`,
`DryBulbTemperature` — also genau die Kategorien, die EPOS erzeugt (Befund R, 1.8). Aber die
`Results`-Blöcke der Revit-Beispieldateien sind der mit Abstand größte Fehlerherd bei der
Validierung: 463 Meldungen je Attribut in **einer** Datei, weil sie mit längst entfernten Attributen
arbeiten. Ergebnisse über gbXML zurückzuliefern ist ein schlecht gepflegter Pfad.

**Vorschlag:** `Results` gehört in **G7b**, nicht in G7a, und schreibt nur die drei Jahresgrößen je
Zone (`Energy`, `HeatLoad`, `DryBulbTemperature` als Mittel) mit `id`, `unit`, `resourceType` und
`startTime` — ohne `timeIncrement`-Reihen. Der IFC-Export ist für Ergebnisse der bessere Weg
(Kapitel 6).

**`unit` ist an `Results` Pflichtattribut, und genau daran scheiterten die Messdateien:** 151
Meldungen „Einheit `kBtuPerHour` unbekannt" in der einen, 20 Meldungen „Einheit `kW` unbekannt" in
der anderen (Befund R, 2.3). **Vor dem Bau von G7b sind deshalb die zulässigen `unit`-Werte je
`resultsType` aus dem XSD namentlich festzulegen** und in dieses Papier einzutragen; ist der
gewünschte Wert (etwa kWh) dort nicht enthalten, wird in der zulässigen Einheit geschrieben und die
Umrechnung im Text genannt. **Findet sich für eine Ergebnisgröße überhaupt kein zulässiger Wert,
entfällt ihr `Results`-Block benannt** — „`Results` schreiben" und „fehlerfrei validieren" (Probe 3)
sind sonst nicht beide zu haben.

---

## 6. IFC-Export (Stufe G7c bis G7e)

### 6.1 Die Entscheidung ist keine technische

Ein IFC ohne jede Geometrie ist schemakonform und validierungsfähig: `IfcProduct.ObjectPlacement`
und `.Representation` sind beide OPTIONAL, und die einzige Regel dazu läuft nur in eine Richtung —
wer eine Repräsentation gibt, braucht ein Placement, nicht umgekehrt (Befund S, 1.1). Auch
`IfcSpatialStructureElement` fordert nur die Aggregation (WR41), keine Lage.

Aber: Die Datei **öffnet voraussichtlich** in einem Betrachter, und das 3D-Fenster **bleibt leer**.
Für ein Rechenwerkzeug als Gegenüber ist das unproblematisch, für einen Menschen mit Betrachter ist
die Datei praktisch wertlos und erzeugt den Eindruck, der Export sei kaputt. **Das ist nach der
Quellenlage zu erwarten, nicht gemessen:** belegt ist allein die xBIM-Aussage zu ihrem eigenen
geometrielosen Beispiel, dazu der Revit-`DirectShape`-Fall und die BIMvision-Baumansicht; einen
veröffentlichten systematischen Test gibt es nicht (Befund S, 1.8). **Was jeder Betrachter öffnet,
anzeigt und meldet, erhebt Probe 16** (rund 1 PT) — erst danach ist die Aussage tragfähig. **Ohne
benannten Empfänger lässt sich zwischen S1 und S3 nicht wählen.** **Entschieden ist mit E27
(D6): die semantische Stufe S1 (G7c) zuerst.** Das Gegenüber — Werkzeug und Zweck — benennt der
Anwender; fällig ist das vor der Stufe, die über die semantische hinausgeht (10).

### 6.2 Schema, Paket, Grenzen

| Festlegung | Wert | Grund |
|---|---|---|
| Schreibschema | **IFC4 (ADD2 TC1)**, `FILE_SCHEMA (('IFC4'))` | `Pset_SpaceThermalRequirements` existiert dort noch (in 4.3 entfallen), `IfcRelSpaceBoundary2ndLevel` gibt es erst ab IFC4, und IFC4 ist ISO 16739. `XbimSchemaVersion` kennt keinen eigenen Wert für ADD2 TC1 (Befund S, 4.5) |
| **Nicht** geschrieben wird | IFC2x3, IFC4.3 | 2x3 fehlen die 2nd-Level-Raumgrenzen; das Verbreitungsargument greift bei einer Datei ohne MVD nicht. Das gilt für den **eigenen** Export (G7c/G7e) **und** für den Round-Trip: eine Quelldatei in 2x3 oder 4.3 wird dort benannt abgelehnt (6.6) |
| Modell | `new MemoryModel(new EntityFactoryIfc4())`, Speichern über `SaveAsStep21(Stream)` | `IfcStore` lebt in `Xbim.Ifc` und zieht `Xbim.IO.Esent` — ausgeschlossen (Befund N, 2.1; Umsetzungskonzept 3.6) |
| Paket | **keines zusätzlich**: `Xbim.IO.MemoryModel` deckt Lesen und Schreiben | `Directory.Packages.props:15` (`CentralPackageTransitivePinningEnabled` = `false`) lässt die drei Schemapakete transitiv kommen; `Microsoft.Extensions.Logging` steht bereits auf `:30` |
| Trimming | dieselbe Trimming-Lage wie beim Import — **heute ist kein `TrimmerRootDescriptor` gesetzt** | `EPOS.iOS.csproj` setzt weder `TrimMode` noch `PublishTrimmed`, und im Repositorium gibt es keinen `TrimmerRootDescriptor`; **G4-8 misst einen Gerätebau**, und erst wenn dort Typen fehlen, entsteht `EPOS.iOS/Pruefung/XbimRoots.xml` (Umsetzungskonzept 3.6, Frage U16). Entsteht einer, deckt er den Export mit ab, weil dieselben Baugruppen benutzt werden (Befund S, 4.5) |
| MVD | **keine Angabe** im `FILE_DESCRIPTION` | Es passt keine: Reference View verlangt ausdrücklich explizite Geometrie, Energy Analysis View ist Entwurf ohne jede Dokumentation. **Eine falsche MVD-Behauptung wäre schlimmer als keine** (Befund S, 1.7) |

### 6.3 Stufe G7c (S1) — der semantische Export

Grundlage ist die Abbildungstabelle aus Befund S, 5; hier auf die Tabellen des Mehrzonenkonzepts
(4.2) bezogen.

| EPOS | IFC4 | Eigenschaften / Mengen |
|---|---|---|
| Projekt | `IfcProject` + `IfcUnitAssignment` | **Die Zuweisung wird vollständig von Hand gebaut** — `IfcProject.Initialize(ProjectUnits.SIUnitsUK)` stellt `LENGTHUNIT` auf `MILLI.METRE` (Befund S, 4.2). Festgelegt werden `LENGTHUNIT` METRE, `AREAUNIT` SQUARE_METRE, `VOLUMEUNIT` CUBIC_METRE, `ENERGYUNIT` JOULE, `POWERUNIT` WATT, `THERMODYNAMICTEMPERATUREUNIT` KELVIN und `PLANEANGLEUNIT` RADIAN. Das ist der Rückfall, auf den der Empfänger landet, wenn an einer Eigenschaft einmal das `Unit` fehlt (6.4) |
| Klimaort | `IfcSite` | `RefLatitude`/`RefLongitude` als `LIST[3:4] OF INTEGER`; **alle Glieder tragen dasselbe Vorzeichen** — das ist die Prüfsumme (Befund S, 1.2) |
| `Tab_Gebaeude` | `IfcBuilding` | `Pset_BuildingCommon.YearOfConstruction` (Text!) als Bandmitte, `EPOS_Gebaeude.Baualtersklasse` mit dem Klassennamen, `EPOS_Ergebnis` mit den Summen |
| `Tab_Zone` | `IfcSpace` (`PredefinedType = SPACE`) | `Qto_SpaceBaseQuantities`: `NetFloorArea` ← `Nutzflaeche`, `Height` ← `Raumhoehe`, `NetVolume` ← `Volumen`. `Pset_SpaceThermalRequirements`: `SpaceTemperature` ← `Raumsolltemperatur_Tag`, `SpaceTemperatureSummerMax` ← `Maximaleraumtemperatur`, `DiscontinuedHeating` ← Nachtabsenkung gesetzt, `NaturalVentilationRate` ← `Luftwechsel_Nutzer`. `LongName` = Zonenname |
| `Tab_Zone.IstBeheizt = 0` | `IfcSpace` ohne `Pset_SpaceThermalRequirements` | `EPOS_Zone.IstBeheizt = FALSE` |
| mehrere Zonen | zusätzlich `IfcZone` + `IfcRelAssignsToGroup` | im **Einzonenfall weglassen** — eine Gruppe mit einem Element ist Rauschen |
| `Tab_Bauteil` AUSSENWAND/INNENWAND | `IfcWall` | `Pset_WallCommon.ThermalTransmittance` ← `U_Wert`, `.IsExternal`; `Qto_WallBaseQuantities.GrossSideArea` ← `Flaeche` |
| DACH | `IfcSlab` `PredefinedType = ROOF` (bzw. `IfcRoof`) | `Qto_SlabBaseQuantities.GrossArea` |
| BODENPLATTE | `IfcSlab` `BASESLAB` | dito |
| DECKE | `IfcSlab` `FLOOR` | dito |
| FENSTER | `IfcWindow` | `Pset_WindowCommon.ThermalTransmittance`, `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` ← `g_Wert`, `Qto_WindowBaseQuantities.Area` |
| TUER | `IfcDoor` | `Pset_DoorCommon.ThermalTransmittance` |
| `Azimut`, `Neigung` | **kein Standardplatz** | `EPOS_Bauteil.Azimut`/`.Neigung` (`IfcPlaneAngleMeasure`); die Bezugsdefinition (0° = Nord, im Uhrzeigersinn; 0° = waagerecht) **muss** im `Description` der Eigenschaft stehen, sonst ist die Zahl wertlos |
| `Randbedingung`, `ID_Nachbarzone` | `IfcRelSpaceBoundary2ndLevel` **ohne** `ConnectionGeometry` | `AUSSENLUFT` → `EXTERNAL`; `ERDREICH` → **`EXTERNAL_EARTH`**; `ZONE`/`UNBEHEIZT` → `INTERNAL` mit `CorrespondingBoundary`. `PhysicalOrVirtualBoundary = PHYSICAL`, `RelatedBuildingElement` ist ab IFC4 **Pflicht**. Zusätzlich `Name='2ndLevel'`, `Description='2a'`/`'2b'` — die Spezifikation nennt diese Konvention selbst als Unterscheidungsmerkmal. **Die fehlende Anschlussgeometrie wird eigens als logisch gekennzeichnet:** `EPOS_Bauteil.Raumgrenze = 'logisch'` und ein Satz im Beipackzettel („Raumgrenzen ohne `ConnectionGeometry` sind logisch, nicht vermessen"). Die meisten View-Definitionen erwarten dort die 3D-Anschlussfläche; ohne die Kennzeichnung hält der Empfänger sie für vermessen (Befund S, 1.4) |
| `Tab_Bauteilaufbau` | `IfcMaterialLayerSet` + `IfcRelAssociatesMaterial` | **nicht** `IfcMaterialLayerSetUsage` — dessen `OffsetFromReferenceLine` setzt eine Bauteilachse voraus, und ohne Körper gibt es keine (Befund S, 1.5). `LayerSetName` ← `Bezeichner` |
| `Tab_Bauteilschicht` | `IfcMaterialLayer` | `LayerThickness` ← `Dicke`, `IsVentilated` ← `IstLuftschicht`. Die Innen-/Außen-Orientierung geht verloren und steht deshalb in `EPOS_Bauteil.Schichtrichtung` |
| `Tab_Baustoff` | `IfcMaterial` | `Pset_MaterialThermal.ThermalConductivity` ← `Lambda`, `.SpecificHeatCapacity` ← `cp`; `Pset_MaterialCommon.MassDensity` ← `Rho` |
| `Psi_L` | `EPOS_Bauteil.WaermebrueckeUA` (W/K) | kein Standardplatz |
| Struktur | `IfcRelAggregates` (Project→Site→Building→Space), `IfcRelContainedInSpatialStructure` (Bauteile→Building) | ein `IfcBuildingStorey` ist **nicht** erzwungen (WR41 verlangt Aggregation, kein Geschoss) |

**Die Zusammenfassung muss auch so heißen.** Im Einzonenmodell wird jede Bauteilgruppe **ein**
Element: alle Außenwände zusammen ein `IfcWall` mit Summenfläche und flächengewichtetem U-Wert. Das
ist eine ehrliche Aggregation, solange `Name = "Außenwände (zusammengefasst)"` und
`EPOS_Bauteil.IstZusammenfassung = TRUE` mit `AnzahlTeilflaechen` danebenstehen. Wer das weglässt,
liefert ein Modell mit vier Wänden aus, das aussieht wie ein Gebäude mit vier Wänden (Befund S, 5).

### 6.4 `EPOS_Ergebnis`, Einheiten, Kennungen

**Die Namensregel ist scharf.** Eigenschaftssätze, die nicht Teil der Spezifikation sind, dürfen das
Präfix `Pset_` **nicht** tragen; der bSI-Validierungsdienst prüft es aktiv (Regel PSE001). Dasselbe
gilt für `Qto_`. **`EPOS_Ergebnis` ist damit regelkonform** — und durchgängig gilt `EPOS_` für alle
eigenen Sätze: `EPOS_Ergebnis`, `EPOS_Gebaeude`, `EPOS_Zone`, `EPOS_Bauteil`, `EPOS_Rechenlauf`.
**Nie** `Pset_EPOS…` oder `Qto_EPOS…` (Befund S, 1.6).

| Eigenschaft (`IfcPropertySingleValue`) | Typ | Ort |
|---|---|---|
| `Heizwaermebedarf` | `IfcEnergyMeasure` **mit explizitem `Unit`** | `IfcSpace`, `IfcBuilding` |
| `HeizwaermebedarfFlaechenbezogen` | `IfcReal`, Einheit im `Description` | dito |
| `Heizlast` | `IfcPowerMeasure` (W) | dito |
| `RaumtemperaturMittel`, `RaumtemperaturMax` | `IfcThermodynamicTemperatureMeasure` | `IfcSpace` |
| `Rechenmodell` | `IfcLabel` | `EPOS_Rechenlauf` am `IfcBuilding` |
| `Validierung` | `IfcLabel` | dito — **wörtlich** der Produktausweis nach **E10** (6.5) |
| `Programmfassung` | `IfcLabel` | dito |
| `Rechenzeitpunkt` | `IfcDateTime` | dito |
| `Wetterdatensatz` | `IfcLabel` | dito — die PVGIS-TMY-Reihe nach **E5** |

Die letzten fünf sind der eigentliche Wert des Satzes: **ein Ergebnis ohne Angabe, womit es
gerechnet wurde, ist in fremder Hand wertlos.** Mengen gehören nicht hierher, sondern in
`IfcElementQuantity`, dessen `MethodOfMeasurement` die vorgesehene Stelle für „nach VDI 6007" ist.

**Die Einheitenentscheidung ist unwiderruflich** — und mit E27 gefallen (**D4**): kWh mit
ausdrücklicher Einheit. `IfcPropertySingleValue.Unit` ist
OPTIONAL; fehlt es, gilt die globale Einheitenzuweisung des `IfcProject`. Wer `IfcEnergyMeasure`
ohne `Unit` schreibt, behauptet damit **Joule**. Zwei saubere Wege: in Joule schreiben und die
kWh-Entsprechung ins `Description` — oder an **jeder** Energie-Eigenschaft ein explizites `Unit`
mitgeben. **Empfehlung: der zweite**, er kostet eine einmalige Helferfunktion und ist der ehrlichere.
`Heizlast` (W) und Temperaturen sind unkritisch.

**Die Bauform dazu ist zu benennen, sonst bleibt die Empfehlung auf halbem Weg stehen.** kWh ist
keine SI-Einheit und lässt sich nicht als `IfcSIUnit` schreiben; es braucht eine
`IfcConversionBasedUnit` (Name `KILOWATTHOUR`, `UnitType = ENERGYUNIT`) mit
`ConversionFactor = IfcMeasureWithUnit(IfcEnergyMeasure(3.6E6), IfcSIUnit(ENERGYUNIT, JOULE))`.
Sie wird **einmal je Datei** angelegt und an jeder Energie-Eigenschaft als `Unit` gesetzt. Eine Probe
hält die `IfcUnitAssignment` aus 6.3 gegen die geschriebenen Maßtypen.

**GlobalId und OwnerHistory setzt EPOS selbst.** `MemoryModel` vergibt **weder** das eine **noch**
das andere — das tut nur `IfcStore`, und der ist der Esent-Weg. `GlobalId` ist Pflichtattribut; wird
es nicht gesetzt, entsteht `$` an Position 1 und damit eine schemawidrige Datei (Befund S, 3.3).
Verbindliche Auflage: **eine EPOS-eigene Erzeugungsfunktion**, die für jede `IIfcRoot`-Instanz beides
belegt.

**Deterministisch, von Anfang an** (**D5**, entschieden mit E27, für IFC und gbXML): namensbasierte UUID (RFC 4122, Version 5) aus
einem festen EPOS-Namensraum und dem **Schlüsselpfad aus IDs**, nie aus Namen, **mit einem
Rollenglied am Ende**:

```
<ID_Projekt>/<ID_Gebaeude>/<ID_Zone>/<ID_Bauteil>#<Rolle>
Rolle ∈ { Objekt, Pset:EPOS_Bauteil, Pset:EPOS_Zone, Qto, RelProps,
          RelAggregates, RelContained, RelMaterial, SpaceBoundary:2a, … }
```

**Das Rollenglied ist nicht Feinschliff, sondern Bedingung.** `GlobalId` ist Pflicht an **jeder**
`IIfcRoot`-Instanz — also auch an `IfcPropertySet`, `IfcElementQuantity`, `IfcRelDefinesByProperties`,
`IfcRelAggregates`, `IfcRelAssociatesMaterial`, `IfcRelSpaceBoundary2ndLevel`, `IfcProject`,
`IfcSite`. Ohne Ableitung für diese Entitäten erzeugte jeder Lauf neue Zufallskennungen, und Probe 12
(zweimal exportieren ergibt byte-gleiche Dateien) wäre nicht erfüllbar.

Umgewandelt wird über die implizite Umwandlung von `System.Guid` nach `IfcGloballyUniqueId`. **Die
BCL bringt keine Fabrik für namensbasierte UUID mit** — die Ableitung nach RFC 4122 Version 5 ist
selbst zu schreiben und mit festen Prüfwerten in einem Test einzufrieren. Ein zweiter Export
derselben Zone trägt dann dieselbe `GlobalId`; ein dupliziertes Projekt bekommt neue IDs und damit
richtigerweise neue Kennungen. **Nachträglich ist das nicht mehr zu ändern, ohne alte Exporte zu
entwerten.**

**Vor dem Speichern läuft der Validator.** `Xbim.Common.ExpressValidation.Validator` prüft das ganze
Modell gegen die generierten EXPRESS-Regeln; Verstöße werden als `PruefMeldung` gemeldet — dieselbe
Mechanik wie beim Import. **Die Tiefe steuert `ValidationFlags`, und die Attributprüfung muss
eingeschaltet sein** — sonst fällt eine fehlende `GlobalId` gar nicht auf. Probe 10 weist genau das
nach: eine künstlich entfernte `GlobalId` wird tatsächlich gemeldet; andernfalls ist die Prüfung nur
scheinbar da (Befund S, 4.4).

**Statt einer MVD zwei Dinge:** der **bSI-Validierungsdienst** (er prüft ausdrücklich keine
Darstellung, eine geometrielose Datei kann dort vollständig bestehen — das ist der belastbarste
verfügbare Konformitätsnachweis) und eine **mitgelieferte IDS-Datei** mit der Exportzusage. IDS ist
auf alphanumerische Information begrenzt und deckt Geometrie nicht ab — also genau auf EPOS'
Datenumfang zugeschnitten und billiger als eine eigene MVD (Befund S, 1.7). **Vorschlag:** die IDS
reist mit der Auslieferung, nicht als zweite Datei je Export. **Windows:**
`{app}\Vorlage\EPOS_Export.ids` über eine `Source:`-Zeile nach dem Muster von
`Setup/EPOS-Plan.iss:330-331`. **iOS:** dieselbe Datei als Bündelressource neben der
Seed-Datenbank; weitergegeben wird sie nur auf ausdrückliche Anforderung über `MitSystemOeffnen`,
weil jeder Schreibvorgang dort ein eigenes Teilen-Blatt ist. **E9** gilt für beide Schalen, also
auch diese Zusage.

### 6.5 Der Produktausweis in der Datei

`EPOS_Rechenlauf.Validierung` trägt den Wortlaut aus **E10**, unverändert und ohne Umschreibung:

> „Rechenkern nach VDI 6007 Blatt 1; elf der zwölf Testbeispiele im Normband einschließlich
> Druckrundung, Testbeispiel 11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung)"

Der Ausweis steht als `IfcLabel` in der STEP-Datei. **ISO 10303-21 kennt nur einen begrenzten
Zeichenvorrat; Umlaute stehen dort als `\X2\…\X0\`-Folgen.** Das betrifft auch die deutschen
Klartexte aus 6.3 (`Name = "Außenwände (zusammengefasst)"`). Probe 22 schreibt sie, liest sie zurück
und vergleicht zeichenweise — die gbXML-Seite hat mit Probe 5 dieselbe Absicherung.

`Rechenmodell` trägt die Kurzform („VDI 6007 Blatt 1, Zweikapazitätenmodell"). Im Mehrzonenfall gilt
der Ausweis **je Zone** (Mehrzonenkonzept 0, Punkt 2) — das Mehrzonenmodell selbst ist eine
EPOS-Erweiterung, keine Norm, und die Datei darf das Gegenteil nicht nahelegen.

### 6.6 Stufe G7d (S2) — Round-Trip-Anreicherung

Der Fall mit dem besten Verhältnis von Nutzen zu Aufwand: die Fremddatei bringt Geometrie und
Struktur mit, EPOS ergänzt nur, was fehlt — U-Werte, Baustoffe, Ergebnisse. `Part21Writer` schreibt
schlicht alle Instanzen des Modells heraus, und `FILE_SCHEMA` der Eingangsdatei bleibt dabei
unverändert stehen (Befund S, 3.1).

**Genau daraus folgt die erste Grenze: G7d gilt nur für Quelldateien im Schema IFC4.** Wer in ein
2x3-Modell EPOS-Sätze einträgt, muss sie mit den konkreten `Xbim.Ifc2x3`-Typen anlegen — Schreiben
verlangt instanziierbare Schematypen, während Lesen über die `IIfc*`-Schnittstellen läuft —,
`IfcRelSpaceBoundary2ndLevel` gibt es dort gar nicht, und ein `TrimmerRootDescriptor` müsste die
zweite Schemabaugruppe mittragen. Das widerspricht **E3** („im Kern allein `Xbim.IO.MemoryModel` +
`Xbim.Ifc4`"). **Eine Quelldatei in IFC2x3 oder IFC4.3 wird deshalb benannt abgelehnt:** „Rückgabe
nur für IFC4; für diese Datei wird eine eigene Datei nach G7c geschrieben." `Tab_Importquelle.Schemastand`
ist das Sperrkriterium (7.1). Wer den zweiten Schreibpfad doch will, muss ihn gesondert veranschlagen
(Xbim.Ifc2x3-Typen, Trimmer, Ersatz für die 2nd-Level-Raumgrenzen) und **E3 dafür erweitern lassen**.

Sechs Bedingungen:

1. **Nur für eine Datei, die zuvor über den Import gelesen wurde**, wiedergefunden über
   `Tab_Importquelle.Hash` — nicht über den Dateinamen. **Beschafft wird sie nicht von EPOS:**
   gespeichert ist bewusst nur der Dateiname ohne Pfad (2.3), und unter iOS wechselt die
   Sandbox-GUID ohnehin. Der Anwender **wählt die Originaldatei beim Export erneut** über
   `Dienste.Datei.DateiOeffnenAsync`; EPOS bildet den SHA-256 und hält ihn gegen
   `Tab_Importquelle.Hash`. Stimmt er nicht überein, wird die Rückgabe benannt abgelehnt und eine
   eigene Datei nach G7c angeboten.
2. **Das Leseprotokoll muss frei von Entitätenverlust sein** — frei von `FailedEntity`-Einträgen
   **und** von Warnungen `Entity #… is referenced but could not be instantiated`; `ignoreTypes`/
   `SkipTypes` wird beim Import nie gesetzt (Wächtertest). Gibt es Einträge, wird die Rückgabe
   **verweigert** und stattdessen eine eigene Datei nach G7c geschrieben. Das ist der einzige Schutz
   gegen stille Datenvernichtung in einer fremden Datei (Befund S, 3.2).
3. **Vorhandene Sätze werden ergänzt, nicht gedoppelt.** Trägt die Wand schon ein
   `Pset_WallCommon` mit `ThermalTransmittance`, wird die Eigenschaft ersetzt, nicht ein zweiter Satz
   angelegt. Die EPOS-Herkunft bleibt im eigenen `EPOS_Bauteil`-Satz nachvollziehbar.
4. **Immer unter neuem Namen speichern.** Die Eingangsdatei wird **nie** überschrieben — das ist
   die Hausregel „nichts wird ohne OK geschrieben" in ihrer schärfsten Form.
5. **Die Datei behauptet sonst weiter ihre Herkunft.** `OriginatingSystem` und
   `PreprocessorVersion` bleiben stehen; EPOS ergänzt deshalb `FILE_DESCRIPTION` um einen Vermerk und
   legt eine eigene `IfcApplication` an.
6. **Die vertragliche Frage ist entschieden** (**D11**, E27): Die Rückgabe angereicherter
   fremder IFC-Dateien ist **zulässig, mit Kennung in der Datei und Beipackzettel**. Die Begründung
   der Auflagen bleibt: Die Reference View sagt ausdrücklich, dass der Empfänger das Modell nicht
   verändern soll und Änderungswünsche als BCF-Meldung an den Urheber zurückgehen; EPOS'
   Anreicherung ist genau das, was die RV nicht vorsieht. Deshalb trägt die Datei die Kennung
   (`FILE_DESCRIPTION`-Vermerk und eigene `IfcApplication`, Nr. 5; neuer Dateiname, Nr. 4), und der
   Beipackzettel sowie ein Hinweis im Dialog, den der Anwender bestätigt, sagen ihm, dass er eine
   fremde Datei verändert weitergibt.

Vorhandene `GlobalId`-Werte sind gewöhnliche Attribute und werden unverändert zurückgeschrieben —
GUID-Stabilität für Bestandsentitäten ist kostenlos. Nur die **neuen** Entitäten (Eigenschaftssätze,
Beziehungen) brauchen die eigene Erzeugungsfunktion aus 6.4.

### 6.7 Stufe G7e (S3) — schematische Körper

Der Quader über einem Rechteckprofil braucht vier Entitäten und **keine boolesche Operation**:
`IfcRectangleProfileDef` → `IfcExtrudedAreaSolid` → `IfcShapeRepresentation`
(`RepresentationIdentifier = "Body"`, `RepresentationType = "SweptSolid"`) →
`IfcProductDefinitionShape` (Befund S, 2.1).

**Die Geometrie stammt aus dem Zonengeometrie-Modell (Nachtrag 1, Kapitel 14)** — dasselbe Polygon
und dieselbe Höhe, die der Gebäudebetrachter zeigt und die G7b als `PolyLoop` schreibt; G7e schreibt
sie als `IfcExtrudedAreaSolid`.

**Ab dem Augenblick, in dem `Representation` gesetzt ist, greift `PlacementForShapeRepresentation`:
jedes Bauteil braucht dann auch ein `ObjectPlacement`.** Die Kette ist
`IfcProject` → `IfcSite` → `IfcBuilding` → Bauteil, jeweils `IfcLocalPlacement`. Jede vergessene
Zuweisung macht die Datei schemawidrig — der Validator aus 6.4 fängt es ab.

- **`TrueNorth` bleibt auf der Vorgabe** (+Y = Nord); der Azimut wird ausschließlich in der Drehung
  des jeweiligen Placements ausgedrückt. Je weniger Drehungen sich überlagern, desto weniger kann
  schiefgehen.
- **Anordnung** wie in 5.5: je Zone ein Quader, Zonen nebeneinander mit Abstand, keine Stapelung.
- **Fenster als kleinere Platte vor der Wand**, nicht ausgeschnitten — `IfcRelVoidsElement` wäre
  wieder ein Kernelthema.
- **Erst damit** wird `IfcMaterialLayerSetUsage` zulässig, und `IfcRelSpaceBoundary` könnte eine
  echte `ConnectionGeometry` bekommen.

**Das Risiko dieser Stufe ist nicht technisch.** Eine Datei, die *aussieht* wie ein Gebäude, aber
keines ist, lädt zur Verwechslung mit einem Architekturmodell ein, und ihre Maße würden in einem
Aufmaßwerkzeug klaglos abgegriffen. Die Kennzeichnung ist deshalb Abnahmekriterium: im
Projektnamen, in `FILE_DESCRIPTION`, je Element im `Description` und als `IfcAnnotation`. Dazu
Prüfbilder aus mindestens zwei Betrachtern.

**S3 kauft Anschaulichkeit und bezahlt mit Verwechslungsgefahr.** Es lohnt erst, wenn im Feld
tatsächlich jemand die Datei in einem Betrachter erwartet.

---

## 7. Datenmodell-Ergänzungen

Der Vorschlag folgt den Hausmustern aus Befund Q (Kapitel 1) und dem Mehrzonenkonzept (4.1/4.2) und
ist **nicht entschieden**. Bauform: Kindliste an `Tab_Gebaeude`, `STRICT`, `AUTOINCREMENT`,
**Kaskade nur zum Eltern**, `CHECK (length(...))` statt Typlänge, Beziehungen über IDs statt Text.
**Ungeordnet** — beide Tabellen führen kein `Rang`; ihre Reihenfolge trägt keine Aussage.

**Die Kaskadenfalle.** Bauform B hat eine benannte Falle, und sie trifft auch hier: Ihr Speicherweg
ist **Löschen + Neuanlegen je Eltern** (Mehrzonenkonzept 4.1; Vorbild und Warnung im Klassenkopf von
`EPOS.Kern/Controller/AnlageStrangCtrl.cs:17`, `:35-45`), und `Tab_Gebaeude.ID_ProjektGebaeude`
kaskadiert selbst — `sql/schema/001_grundschema.sql:1187` setzt `ON DELETE CASCADE` auf
`Z_ProjektGebaeude`. Ein `ON DELETE CASCADE` von `Tab_Importquelle` auf `Tab_Gebaeude` räumt die
Herkunftsablage deshalb **nicht nur beim Löschen des Gebäudes ab, sondern bei jedem Speichervorgang,
der über den Wizard-Weg läuft**. Die Rettung steht dort, wo das Löschen steht — nicht im
Importcontroller: Sichern vor dem `DELETE`, Wiederherstellen nach dem Neuanlegen, wörtlich nach dem
Muster `WizardCtrl.StraengeSichern` (`EPOS.Kern/Controller/WizardCtrl.cs:109-111`, Sicherung
`:1094`, Wiederherstellung `:1187`). Probe 14 prüft deshalb **beides**: nach dem Löschen des
Gebäudes sind die Tabellen leer, **nach einem gewöhnlichen Speichern stehen sie unverändert**.

### 7.1 `Tab_Importquelle` — eine Zeile je Importlauf

| Spalte | Typ | NULL | Bedeutung |
|---|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | — | |
| `ID_Gebaeude` | INTEGER NOT NULL | — | FK → `Tab_Gebaeude.ID`, `ON DELETE CASCADE` |
| `Format` | TEXT NOT NULL CHECK (IN ('IFC','GBXML')) | — | Persistenzwert, kein Anzeigetext |
| `Dateiname` | TEXT NOT NULL CHECK (length ≤ 260) | — | **nur der Name, nie der Pfad** (2.3) |
| `Hash` | TEXT NOT NULL CHECK (length = 64) | — | SHA-256 des Dateiinhalts, hexadezimal klein |
| `Groesse` | INTEGER NOT NULL | — | Bytes |
| `Schemastand` | TEXT | ja | `IFC4`/`IFC2X3`/`IFC4X3` bzw. der gbXML-`version`-Wert, wie gelesen; **Sperrkriterium des Round-Trips** — nur `IFC4` lässt G7d zu (6.6) |
| `Zeitpunkt` | TEXT NOT NULL | — | ISO-8601, invariant |
| `Programmfassung` | TEXT | ja | EPOS-Fassung des Laufs |
| `Zonenregel` | TEXT | ja | `Z1`…`Z5` bzw. `X1`…`X4` des Laufs |
| `FehlendeEntitaeten` | INTEGER NOT NULL DEFAULT 0 | — | **Summe beider Verlustkanäle**: `FailedEntity`-Einträge **und** Warnungen `Entity #… is referenced but could not be instantiated` (Befund S, 3.2); **> 0 sperrt den Round-Trip** (6.6) |

### 7.2 `Tab_Importzuordnung` — eine Zeile je Paarung

| Spalte | Typ | NULL | Bedeutung |
|---|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | — | |
| `ID_Importquelle` | INTEGER NOT NULL | — | FK → `Tab_Importquelle.ID`, `ON DELETE CASCADE` |
| `ID_Gebaeude` | INTEGER | ja | FK → `Tab_Gebaeude.ID`, **ohne** Kaskade — die Paarung EPOS-Gebäude ↔ `IfcBuilding.GlobalId` bzw. gbXML-`Building/@id` |
| `ID_Zone` | INTEGER | ja | FK → `Tab_Zone.ID`, **ohne** Kaskade |
| `ID_Bauteil` | INTEGER | ja | FK → `Tab_Bauteil.ID`, ohne Kaskade |
| `ID_Aufbau` | INTEGER | ja | FK → `Tab_Bauteilaufbau.ID`, ohne Kaskade |
| `ID_Baustoff` | INTEGER | ja | FK → `Tab_Baustoff.ID`, ohne Kaskade |
| `Quellkennung` | TEXT NOT NULL CHECK (length ≤ 64) | — | `IfcGloballyUniqueId` (Base64, 22 Zeichen) bzw. gbXML-`id` |
| `Quelltyp` | TEXT NOT NULL CHECK (length ≤ 40) | — | `IfcBuilding`, `IfcSpace`, `IfcWall`, `Building`, `Space`, `Surface`, `Construction`, `Material` … |

Dazu die Regel, die aus fünf Fremdschlüsseln einen Zeiger macht:

```sql
CHECK ((ID_Gebaeude IS NOT NULL) + (ID_Zone IS NOT NULL) + (ID_Bauteil IS NOT NULL)
     + (ID_Aufbau IS NOT NULL) + (ID_Baustoff IS NOT NULL) = 1)
```

**Warum `ID_Gebaeude` auch hier steht**, obwohl `Tab_Importquelle` schon eines führt: Dort zeigt es
vom Lauf auf das Ziel, hier trägt es die **Paarung** mit der Quellentität. Das Mehrzonenkonzept
verlangt sie ausdrücklich (Kapitel 6: „die Zuordnung EPOS-Zone ↔ `IfcSpace.GlobalId` und
EPOS-Gebäude ↔ `IfcBuilding.GlobalId` … persistiert"), und dieses Papier schreibt in 6.3/6.4
`EPOS_Gebaeude`, `EPOS_Ergebnis` und `EPOS_Rechenlauf` an den `IfcBuilding` — ohne gespeicherte
Paarung ist die Gebäudeentität beim Round-Trip G7d nicht wiederzufinden.

**Warum fünf Spalten statt eines Paars aus `Zielart` und `ID_Ziel`:** Eine polymorphe Kennung ohne
Fremdschlüssel wäre ein Textverweis in Zahlenform — genau das, was die Hausregel „neue Beziehungen
über IDs" verhindern soll. Fünf NULL-fähige Fremdschlüssel mit einem `CHECK` kosten eine Zeile DDL
und behalten die Verweisintegrität.

**Indizes:** `(ID_Importquelle)` und `(Quellkennung)` — der zweite ist der Weg des Round-Trips vom
`IfcSpace.GlobalId` zurück zur EPOS-Zone.

### 7.3 Herkunftsspalten

Am Bestand des Mehrzonenkonzepts (4.2) ändern sich zwei Dinge, beide in Schemaschritt **S-C**,
solange die Spalten noch nicht existieren (Frage **D9**):

| Vorher (Mehrzonenkonzept 4.2) | Nachher | Grund |
|---|---|---|
| `Tab_Zone.IfcGuid`, `Tab_Bauteil.IfcGuid` TEXT CHECK (length ≤ 22) | **`Quellkennung`** TEXT CHECK (length ≤ 64) | eine gbXML-`id` in einer Spalte namens `IfcGuid` ist eine Unwahrheit; 22 Zeichen reichen für `xsd:ID` nicht |
| `Herkunft` TEXT mit `CHECK (Herkunft IN ('IFC','KATALOG','MANUELL','VORGABE'))` | `Herkunft` TEXT CHECK (`Herkunft IN ('GBXML','IFC','KATALOG','MANUELL','VORGABE')`) | das zweite Format kommt **hinzu**; `KATALOG` bleibt — es trägt die Herkunft „aus dem Bauteil-/Baustoffkatalog kopiert", die auf dem `CopyFromStamm`-Weg entsteht. Ein `CHECK` ohne `KATALOG` lehnte jede aus dem Katalog (G3) übernommene Zeile beim Schreiben ab |

`Tab_Bauteilaufbau.Herkunft` und `Tab_Baustoff.Herkunft` tragen denselben Wertebereich —
einschließlich `KATALOG`, denn genau dort entsteht er (Mehrzonenkonzept 3.5/4.2);
`Tab_Bauteilaufbau.Quelle` nimmt den Dateinamen des Imports. `Tab_Bauteilschicht` braucht keine eigene Herkunft — sie erbt die
des Aufbaus, und ihre Stoffwerte sind ohnehin Kopien zum Zeitpunkt der Zuordnung.

### 7.4 Schemaschritt

**Schrittnummern stehen in keinem Papier.** Der Zielstand wird bei der Beauftragung an
`SchemaStand.Zielversion` abgelesen (`EPOS.Kern/Allgemein/Update/SchemaStand.cs`); Stand 22.09.2026
steht er auf **100**, die nächste freie Nummer ist damit **101** — eine Momentaufnahme, keine
Festlegung. Bis zur Beauftragung
tragen die Schritte nur ihre Papiernamen: **M3** für die Gebäudespalten — die Spalten von G1 und
G2 in **einem** Schritt, so ist es mit E27 entschieden (Umsetzungskonzept 1.7, **U5**) — und
**M4** für die Klimaspalten, der mit Schemaschritt 95 bereits vorweggenommen ist und getrennt
bleibt. Die Nummern vergibt erst die Beauftragung (A11, ebenfalls mit E27 entschieden). Dazu ein eigener Schritt für die
`Tab_Solar`-Klimaspalten und die Mehrzonenschritte **S-A** bis **S-D** sowie **S-G**
(Mehrzonenkonzept 4.4). (**S-E** ist der Umbau von `GebaeudeStammCtrl.CopyFromStamm` und läuft als
eigener Einfrierschritt mit **G1**; er berührt die Nummernfolge nicht.)

Der Schritt dieses Papiers heißt hier **S-F** und bekommt die nächste freie Nummer **hinter den
Mehrzonenschritten**; die Zahl bleibt offen, bis die Reihenfolge der Auslieferung feststeht. Inhalt:
`Tab_Importquelle` und `Tab_Importzuordnung` anlegen, die beiden Indizes setzen. **Kein Datenumbau,
keine Saat.**

Der Weg ist ADR-001 ([`ADR-001_Schema-Ausrollung.md`](ADR-001_Schema-Ausrollung.md), Option C): eine
`ImportzuordnungSchema.cs` im Kern als **eine** Quelle für Migrationsschritt,
`Werkzeuge/Testdatenbankschema`, Kopierweg und Nachweis. Dazu die Registerpflege ohne DDL:

- **`ProjektDuplizierenCtrl.FK_MAP`** (`EPOS.Kern/Controller/ProjektDuplizierenCtrl.cs:85`) für die
  Abbildung FK-Spalte → Zieltabelle, bei Bedarf `FK_OVERRIDE` (`:140`), und **`KINDER`** (`:152`)
  zweistufig `Tab_Importquelle` → `Tab_Importzuordnung`. Alle drei sind `OrdinalIgnoreCase` — ein
  zweiter Eintrag in anderer Schreibweise ist eine `ArgumentException` beim Laden der Klasse
  (Begründung im Quelltext `:113-118`). *Eine Sammlung `ID_MAP` gibt es nicht; wer danach sucht,
  sucht nach einem Bezeichner, den der Bestand nicht führt.*
- **`sql/tools/Reduziere-Testdatenbank.sql`.** Beide Tabellen führen **kein** `ID_Projekt` und hängen
  über `ID_Gebaeude` bzw. `ID_Importquelle` — genau der Fall, für den das Mehrzonenkonzept in S-D
  denselben Eintrag verlangt. Zwei `DELETE`-Zeilen nach dem Muster der `Tab_DBTagV`-Kette (`:368`,
  Elternkette dokumentiert `:132`): `Tab_Importquelle` über
  `ID_Gebaeude NOT IN (SELECT ID FROM Tab_Gebaeude)`, `Tab_Importzuordnung` über
  `ID_Importquelle NOT IN (SELECT ID FROM Tab_Importquelle)`, Eltern zuerst. Dazu die Gegenprobe
  `Reduziere-Testdatenbank.probe.py`. Ohne die Zeilen bleiben beim Reduzieren Waisenzeilen stehen.
- **`Werkzeuge/Auslieferungsvorlage`** bekommt eine Prüfregel: **beide Tabellen sind in der
  Auslieferungsdatenbank leer**. Sonst trüge eine ausgelieferte `Kenndaten.sqlite` Dateinamen und
  SHA-256 fremder Importe mit.

Nach jeder neuen SQL-Anweisung läuft `Werkzeuge/SqlDialektPruefer`.

### 7.5 Einfrierregel-Prüfung

**Der Schemaschritt ist ergebnisneutral, und der Datenaustausch berührt die Referenzbasis nicht.**
Drei Prüfungen, die das tragen:

1. **Kein Rechenweg liest die beiden neuen Tabellen.** Sie sind reine Herkunftsablage; weder
   `SimulationWaermebedarf` noch der Bauteilweg noch der Bericht greift darauf zu. Der Wortlaut für
   „ergebnisneutral, solange kein Leser da ist" steht in `Referenzlaeufe/LIESMICH.md`.
2. **Import und Export laufen nur auf Zuruf des Anwenders.** Kein Referenzprojekt wird importiert,
   kein Lauf exportiert. Der Referenzlauf meldet **`GESAMT: PASS`** unverändert — gegen die aktuelle
   Basis, nicht gegen eine neue.
3. **Keine der heute drei Einfrierregeln ist betroffen** (`Referenzlaeufe/LIESMICH.md:57` —
   Emissionsfaktoren, `:79` — PV-Modulkoeffizienten, `:100` — Flottenparameter des Projekts 1046).
   Der Datenaustausch sät keine Emissionsfaktoren, keine PV-Modulkoeffizienten und keine
   Speicherflotte. Nach dem Einfrierschritt **GB** (**E4**) sind es vier, nach **G6** fünf
   („gesäte Zonendaten", Mehrzonenkonzept 8.3) — auch von denen berührt der Datenaustausch keine.
   Eine eigene Einfrierregel für den Datenaustausch wird nicht gebraucht.

---

## 8. Lizenz und Auslieferung

### 8.1 xBIM: CDDL-1.0, unverändert, mit Quellenverweis

Nach **E3** bleibt xBIM ein **unverändertes NuGet-Paket** unter CDDL-1.0 — nie geforkt, nie
gepatcht; dann gibt es nichts offenzulegen. Die Auflage aus **§ 3.1** ist der dauerhafte
Quellenverweis **an den Empfänger**, und dafür fehlt im Setup heute die Stelle:
`Setup/EPOS-Plan.iss:164-165` setzt `LicenseFile={#SetupDir}Lizenz.rtf`, `:330-331` kopiert dieselbe
Datei nach `{app}` — eine Seite für Fremdbibliotheken gibt es nicht.

**Die Festlegung steht im Umsetzungskonzept (3.6, U10 — mit E27 entschieden: die Seite kommt
mit der ersten IFC-Stufe) und wird hier nur bekräftigt:** eine
`Setup/Vorlage/Lizenzhinweise.txt` mit je Fremdbibliothek Name, Version, Lizenz, Copyright-Vermerk
und Quelltextverweis, eine `Source:`-Zeile nach dem Muster `:330-331`, und als Pflegeweg eine Zeile
je ausgelieferter `PackageVersion` aus `Directory.Packages.props`. **Ohne diese Seite ist der
IFC-Weg nicht auslieferbar** — und das gilt für den Export genauso wie für den Import, weil beide
dasselbe Paket benutzen.

Der gbXML-Weg braucht **kein** Paket und fügt der Seite nichts hinzu. Das ist ein weiteres Argument
für die Reihenfolge aus **D1** (entschieden mit E27): G4c ist ohne die Lizenzarbeit auslieferbar.

### 8.2 Das gbXML-Schema: keine Lizenz, also nicht ausliefern

Dreifach geprüft, dreimal negativ (Befund R, 1.10): kein Lizenzkopf im XSD, `"license": null` in der
GitHub-API des Schema-Repositoriums, keine Nutzungsbedingung auf gbxml.org. Das Schema wird zwar
durchgängig als „open and free" beworben, aber ohne ausdrückliche Lizenz ist die Rechtslage bei
Weitergabe unklar.

**Für EPOS ist das entschärft, weil das XSD nicht gebraucht wird:** Der Exporteur ist
handgeschrieben und führt die Aufzählungswerte als C#-Konstanten. **Festlegung (D3; der Rest D17
entschieden mit E27 (22.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.32)):**

- Das XSD wird **nicht ausgeliefert**. Es dient allein der Validierung des Exports im
  Regressionstest gegen die Schemakopie `GreenBuildingXML_Ver8.01.xsd` — deren `versionEnum` bei
  **6.01** endet, weil 8.01 byteweise 7.04 ist (Befund R, 1.1); `XmlSchemaSet.Compile()` kostet 4 ms.
- Es wird **nie aus dem Netz geladen** — weder im Test noch zur Laufzeit.
- **Für das Installationspaket ist die Frage damit gegenstandslos. Die Ablage der Kopie ist
  entschieden (D17, E27): außerhalb des Repositoriums.** Die Vorsicht, die Befund R (1.10) für die
  Beispieldateien anmeldet („nicht ungeprüft in ein EPOS-Regressionstest-Repositorium übernehmen"),
  gilt dem Buchstaben nach genauso für ein Schema ohne ausdrückliche Lizenz — im Repositorium würde
  die Datei ebenfalls weitergegeben, nur an einen anderen Kreis. Deshalb: ein Eintrag in
  `.gitignore`, ein **Einrichtungshinweis**, wie die Kopie lokal beigestellt wird, und eine
  **LIESMICH-Zeile** mit Herkunft, Abrufdatum und Lizenzstand „keine", wie 8.3 sie für
  `Referenzlaeufe/Importproben/` schon verlangt. Fehlt die Datei, wird der Validierungstest
  **benannt übersprungen** — dasselbe Muster wie für die lokal beigestellten Normzahlen (U8), nie
  ein stilles Grün. Die verworfene Möglichkeit — die Kopie unter `EPOS.Kern.Tests/` mit
  ausgeschriebener Begründung — bleibt hier als Begründung stehen.

### 8.3 Testdateien: selbst erzeugen statt herunterladen

| Quelle | Lizenz | Verwendung |
|---|---|---|
| Die vier gbxml.org-Beispieldateien | **keine** | **nicht** ins Repositorium (Befund R, 1.10) |
| ASHRAE-RP-1810-Testfälle (gbxml.org) | keine ausgewiesenen Bedingungen | nicht aufnehmen |
| Ladybug/Honeybee | AGPL-3.0 | scheidet als Testdatenquelle aus |
| `AC20-FZK-Haus.ifc` (KIT/IAI) | uneingeschränkt, Namensnennung | bleibt die IFC-Importprobe (Umsetzungskonzept 3.7); mit **Q12** entschieden: kommt mit Quellenvermerk ins Repositorium |
| `FM_ARC_DigitalHub_with_SB_v1.ifc` | **MIT** | die Mehrzonen-Importprobe (Mehrzonenkonzept 8.2); als RWTH-/bim2sim-Datei nach **Q12** erst **nach der Lizenzklärung** aufzunehmen — die Lizenzangabe ist vor der Aufnahme zu belegen |

**Die gbXML-Prüfdateien erzeugt EPOS selbst** — das ist der eigentliche Gewinn des Rundlaufs: Der
Exporteur schreibt aus einem gesäten Testgebäude eine Stufe-1- und eine Stufe-2-Datei, und der
Importer liest sie zurück. Dazu kommen von Hand geschriebene Kleinstdateien je Fehlerbild (3.8),
jede unter 5 KB: eine in Fuß/Fahrenheit, eine in UTF-16LE, eine ohne `Construction`, eine mit einer
reinen `R-value`-Schicht, eine mit ins Leere zeigendem `spaceIdRef`. Sie gehören nach
`Referenzlaeufe/Importproben/` (gewöhnliche Blobs, **kein** LFS) und in die dort anzulegende
`LIESMICH_Importproben.md` mit Quellenvermerk „selbst erzeugt". Der Ordner besteht bereits, und die
tragende Zusage steht auch schon: **`.gitattributes` nimmt `Referenzlaeufe/Importproben/**` mit
`-text` von der Zeilenenden-Normierung aus** (`.gitattributes:87`). Genau das ist die Bedingung
dafür, dass die UTF-16LE-Probe (Probe 5) byteweise überlebt — `* text=auto` schriebe sie beim
nächsten Auschecken um. **An `.gitattributes` ist nichts zu tun.**

**Q12 ist entschieden (16.09.2026): Importproben für alle Importwege.** Nicht nur der
IFC-Weg bekommt seine Probe, sondern **jeder** Leser: die KIT-Datei mit Quellenvermerk für IFC,
die selbst erzeugten Rundlaufdateien (Stufe 1 und Stufe 2) und die Kleinstdateien je Fehlerbild
für gbXML — alle unter `Referenzlaeufe/Importproben/` mit ihrer LIESMICH-Zeile (Herkunft,
Abrufdatum bzw. „selbst erzeugt", Lizenzstand). RWTH- und bim2sim-Dateien bleiben bis zur
Lizenzklärung draußen (Konzept N1.17).

### 8.4 Kennzeichnung schematischer Geometrie

Die Kennzeichnungspflicht aus 5.5 und 6.7 ist eine **Auslieferungsauflage**, keine
Geschmacksfrage, und sie steht an drei Stellen:

| Ort | gbXML | IFC |
|---|---|---|
| in der Datei | `Campus/Description`, `Building/Description` | `FILE_DESCRIPTION`, `IfcProject.Name`, `Description` je Element, `IfcAnnotation` |
| logische Raumgrenzen (S1) | — | `EPOS_Bauteil.Raumgrenze = 'logisch'` je `IfcRelSpaceBoundary2ndLevel` **ohne** `ConnectionGeometry`, dazu der Satz im Beipackzettel: „Raumgrenzen ohne Anschlussgeometrie sind logisch, nicht vermessen" (6.3, Befund S, 1.4) |
| im Beipackzettel | Hinweistext im Exportdialog, bestätigungspflichtig | dito, zusätzlich der Satz „Diese Datei enthält Fachdaten ohne Bauteilgeometrie" für S1 |
| in der Oberfläche | Herleitungszeile unter dem Exportknopf | dito |

---

## 9. Tests und Abnahme

| Nr. | Probe | Kriterium |
|---|---|---|
| 1 | **Rundlauf gbXML** — Export Stufe 1 aus einem gesäten Testgebäude, Import derselben Datei | Zonenfläche, Volumen, **Luftwechsel**, jede Bauteilfläche, Azimut, Neigung, U-Wert, Schichtdicke und λ/ρ/c auf 1e‑6 gleich. Das gesäte Gebäude trägt einen **unsymmetrischen Aufbau (Innendämmung)** mit fest erwarteter `Reihenfolge`, damit eine vergessene Umkehr (3.4/5.2) nicht symmetrisch durchrutscht. **Das ist der tragende Regressionstest und zugleich die Quelle der eigenen Prüfdateien** |
| 2 | **Rundlauf gbXML Stufe 2** — dieselbe Datei mit `PolyLoop` | zusätzlich: jede `PolyLoop` ist geschlossen, jede Kante trifft die Kante einer Nachbarfläche auf 1 mm, Σ Flächen = Σ Bauteilflächen, **und eine Trennfläche zwischen zwei Zonen ist von beiden Seiten geometrisch dieselbe** (5.5, Punkt 3). Ist keine widerspruchsfreie Anordnung ableitbar, prüft die Probe die **benannte Ablehnung** |
| 3 | **XSD-Validierung des Exports** | beide Stufen validieren gegen die lokale Schemakopie `GreenBuildingXML_Ver8.01.xsd` **fehlerfrei** — ihr `versionEnum` endet bei 6.01, weil 8.01 byteweise 7.04 ist (Befund R, 1.1). Stufe 2 **einschließlich `Results`**; findet sich für eine Ergebnisgröße kein zulässiger `unit`-Wert, entfällt der `Results`-Block benannt (5.6). Nur im Test, nie in der Auslieferung, nie aus dem Netz; die Kopie liegt außerhalb des Repositoriums, und fehlt sie, wird die Probe **benannt übersprungen** (8.2, D17) |
| 4 | **Import validiert nie** | Wächtertest über den Quelltext: kein `XmlSchemaSet` im Leser; ebenso kein `File.ReadAllText`, kein `XDocument.Parse` (3.1) |
| 5 | **Kodierungsprobe** | die selbst erzeugte UTF-16LE-Datei mit BOM wird gelesen und liefert dieselben Zahlen wie ihre UTF-8-Entsprechung |
| 6 | **Einheitenprobe** | Fuß/Fahrenheit-Datei und Meter/Celsius-Datei liefern dieselben SI-Werte; ein lokales `unit` schlägt das globale; ein unbekanntes `unit` ergibt `null`, nie 0 |
| 7 | **Leere Bauphysik** | Datei ohne `Construction` läuft durch; **jede** U-Wert-Zeile trägt Herkunft `Vorgabe`, keine trägt `GbXml` |
| 8 | **Masseloser Aufbau** | ein Aufbau mit einer reinen `R-value`-Schicht → Warnung, Stoffwerte mit Herkunft `Vorgabe`, U-Wert aus der Schichtung |
| 9 | **Zonenregel X1** | eine Datei mit 12 `Space` und 12 `Zone` fällt auf X2 zurück; eine mit 26 `Space` und 1 `Zone` nimmt X1 (3.3) |
| 10 | **IFC-Export: `ExpressValidation.Validator`** | leere Verstoßliste vor `SaveAsStep21`; eine künstlich entfernte `GlobalId` wird gemeldet |
| 11 | **Determinismus der Kennungen** | zweiter Export desselben Projekts liefert **byte-gleiche** `GlobalId` an **jeder `IfcRoot`-Instanz** — Objekte, Eigenschaftssätze, Mengen, Beziehungen, Raumgrenzen (6.4, Rollenglied) — und dieselben gbXML-`id`; ein dupliziertes Projekt liefert andere. Dazu feste Prüfwerte für die selbstgeschriebene RFC-4122-Version-5-Ableitung |
| 12 | **Determinismus der Datei** | zweimal exportieren ergibt byte-gleiche Dateien **außer** dem Zeitstempel, und der steht an genau einer Stelle |
| 13 | **Round-Trip-Sperre** | drei Fälle, je benannte Verweigerung mit Angebot einer eigenen Datei nach G7c: `Tab_Importquelle.FehlendeEntitaeten > 0` (beide Verlustkanäle, 6.6 Nr. 2); **Hash der erneut gewählten Datei ≠ `Tab_Importquelle.Hash`** (6.6 Nr. 1); **`Schemastand ≠ 'IFC4'`** (6.6, Einleitung). Dazu der Wächtertest, dass der Import nie `ignoreTypes`/`SkipTypes` setzt |
| 14 | **Persistenz** | nach dem Import findet ein Test **Gebäude und Zone** über `Tab_Importzuordnung.Quellkennung` wieder; **nach einem gewöhnlichen Speichern des Gebäudes stehen Quelle und Zuordnung unverändert** (Kaskadenfalle, Kapitel 7); erst nach dem **Löschen** des Gebäudes sind beide Tabellen leer |
| 15 | **bSI-Validierungsdienst** | von Hand, je Stufe einmal (S1 und S3); das Ergebnis wird protokolliert. Der Dienst prüft ausdrücklich **keine** Darstellung — eine geometrielose Datei kann dort vollständig bestehen |
| 16 | **Betrachter-Prüfmatrix** | rund 1 PT: eine geometrielose EPOS-Testdatei durch Archicad, Revit, Solibri, BIMcollab Zoom, FZKViewer, BIMvision schicken und protokollieren, **was öffnet, was anzeigt, was meldet**. Die veröffentlichte Quellenlage reicht dafür nicht (Befund S, 1.8); **erst dieses Ergebnis trägt die Aussage in 6.1** |
| 17 | **Referenzlauf** | **`GESAMT: PASS`** unverändert gegen die aktuelle Basis, vor und nach jeder Teilstufe (7.5) |
| 18 | **Wachen** | `DokumentationLinkWacheTests` als Dauerprüfung über die Indexzeile in [`../LIESMICH.md`](../LIESMICH.md) und jeden relativen Verweis; **`Werkzeuge/Auslieferungsvorlage` grün** (beide Importtabellen leer, 7.4); `EinheitenWacheTests` — jede neue Klasse mit physikalischen Größen kommt **im selben Merge** in die Liste; `RepositoryOrdnungWacheTests`; `WikiProduktdatenWacheTests`, sobald eine Wiki-Quelle entsteht; `Werkzeuge/SqlDialektPruefer` mit dem Schemaschritt |
| 19 | **Dialogtests** (bunit) | Abbrechen liefert `false` und ruft `Uebernehmen` nie; ein abgehakter Haken hält die Zeile aus dem Satz; die Herkunftstexte kommen aus `GebaeudeZuordnungsModell`, nicht aus der Komponente; derselbe Dialog zeigt beide Formate |
| 20 | **iOS** | **Gerätebau** (heute ist kein `TrimmerRootDescriptor` gesetzt, Umsetzungskonzept 3.6 / Frage U16; entsteht mit G4-8 einer, läuft der Bau **mit** ihm), Prüfmodus liest die KIT-Datei und schreibt einen IFC- und einen gbXML-Export; Größengrenze **gemessen**, nicht geschätzt. **Kostet einen macOS-Lauf — beim Anwender zu erfragen** |
| 21 | **Azimutdrehung gbXML** | eine Datei mit gesetztem `CADModelAzimuth` und einer Fläche bekannter Lage (Ost oder Süd): Vorzeichen, Nullbezug und Drehsinn werden gegengemessen und das Ergebnis in 3.2 eingetragen. Bis dahin prüft die Probe, dass EPOS **nicht still aufaddiert**, sondern meldet |
| 22 | **Umlautprobe IFC** | ein Element mit `Außenwände (zusammengefasst)` und der Produktausweis nach E10 werden geschrieben, zurückgelesen und **zeichenweise** verglichen (Part21-Escape `\X2\…\X0\`, 6.5); die Datei wird zusätzlich im bSI-Validierungsdienst geprüft |
| 23 | **Einheitenzuweisung IFC** | die `IfcUnitAssignment` (6.3) wird gegen die tatsächlich geschriebenen Maßtypen gehalten; jede Energie-Eigenschaft trägt die `IfcConversionBasedUnit` `KILOWATTHOUR` als `Unit` (6.4) |
| 24 | **Überlange Kennung** | eine gbXML-Datei mit einer `id` über 64 Zeichen wird gelesen; die Zuordnung steht gekürzt mit SHA-256-Präfix in `Tab_Importzuordnung`, der Hinweis im Protokoll (5.4) |

**Die Proben 25 bis 27** — Determinismus der Geometrie, Komponentenprobe des
Gebäudebetrachters, „Ansicht und Datei zeigen dasselbe" — gehören zu dieser Liste und stehen
in **14.5**; sie werden mit G6c bzw. G7b abgenommen.

**Je Importweg eine Probe im Repositorium** (Entscheid zu **Q12**, 16.09.2026): die Proben 1, 2
und 5 bis 9 laufen gegen Dateien, die nach 8.3 im Repositorium liegen — für gbXML die selbst
erzeugten, für IFC die KIT-Datei mit Quellenvermerk. Ein Importweg ohne eigene Probendatei gilt
als nicht abgenommen; RWTH- und bim2sim-Dateien kommen erst nach der Lizenzklärung hinzu.

**Abnahme je Teilstufe:** Kern-Filter grün, die zugehörigen Proben bestanden, Referenzlauf
unverändert, Windows-Sichtabnahme (Datei wählen bzw. schreiben, Zuordnung prüfen, OK), iOS-Lauf nach
Rückfrage. **Für G4 gilt E38 (24.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
N1.43):** genau ein iOS-Lauf, bei der Abnahme von G4a und ausschließlich nach ausdrücklicher
Rückfrage beim Anwender; G4c wird ohne iOS-Lauf abgenommen.

---

## 10. Stufen und Aufwand

| Stufe | Inhalt | Abnahme | Aufwand |
|---|---|---|---|
| **G4c — gbXML-Import** | Lesemodell (`GbXmlDatei`, `GbXmlEinheiten`, `GbXmlModell`), Einheiten global und lokal, Aggregation auf das Gebäudemodell, Nachbarschaftsauflösung, Fensterabzug, Aufbauprüfung je Aufbau, **Zonenregel X4** (X1…X3 mit G6c, 3.3), Meldungen in beiden `.resx`, Anschluss an den gemeinsamen Zuordnungsdialog — **dazu die Persistenz** (Kapitel 7: Schemaschritt S-F, zwei Tabellen, `ImportzuordnungSchema.cs`, Registerpflege, Reduzierskript, Auslieferungsvorlage, Umbenennungen `IfcGuid` → `Quellkennung` und `IfcHerkunft` → `Importherkunft`) | Proben 1, 5–9, 14, 21, 24; Referenzlauf unverändert; Windows-Sichtabnahme | **17–28 PT** |
| **G7a — gbXML-Export Stufe 1** | `GbXmlExportAblauf`/`-Profil`, Wurzelattribute mit `version="6.01"`, `Campus`/`Building`/`Location`/`Space`/`Zone`/`Surface` mit `RectangularGeometry`/`Opening`, vollständige `Construction`-Kette mit **Schichtumkehr**, **Ersatzschichtung samt Kennzeichnung** (5.3), deterministische Kennungen, XSD-Prüfung im Test | Proben 1, 3, 11, 12 | **9–14 PT** |
| **G7b — gbXML Stufe 2** | synthetische Quadergeometrie, kantenschlüssige `PolyLoop`, Fenster als Rechtecke, `ShellGeometry`, `Results` je Zone, Kennzeichnung in Datei und Oberfläche; **die Geometrie kommt aus dem Zonengeometrie-Modell** (Nachtrag 1) | Probe 2; Sichtprobe in mindestens einem Zielwerkzeug | **7–12 PT** † |
| **G7c — IFC-Export S1** | `IfcExportAblauf`/`-Profil`, vollständige Abbildung aus 6.3, `EPOS_*`-Sätze, vollständige `IfcUnitAssignment` und `IfcConversionBasedUnit` für kWh, eigene `GlobalId`/`OwnerHistory`-Erzeugung mit Rollenglied, Validator mit Attributprüfung, kein MVD-Eintrag, IDS in der Auslieferung (Windows und iOS), Beipackzettel | Proben 10–12, 15, 16, 22, 23; Lizenzhinweisseite vorhanden | **12–20 PT** |
| **G7d — IFC-Export S2 (Round-Trip)** | Wiederfinden über `Tab_Importzuordnung` (die Tabellen stehen schon aus G4), erneute Dateiwahl mit Hash-Abgleich, Schema- und Protokollsperre, Ergänzen statt Doppeln, neuer Name, `FILE_DESCRIPTION` und eigene `IfcApplication` | Probe 13; Kennung in der Datei und Beipackzettel vorhanden (D11, mit E27 entschieden: zulässig mit diesen Auflagen) | **6–11 PT** |
| **G7e — IFC-Export S3 (Körper)** | Quader je Zone, Platte je Bauteil **aus dem Zonengeometrie-Modell** (Nachtrag 1), Placement-Kette, Azimut als Drehung, `TrueNorth` auf der Vorgabe, Kennzeichnung, Prüfbilder | Probe 16 mit Bildern; Validator grün trotz Placement-Pflicht | **8–15 PT** † |
| **Gebäudebetrachter (E11)** | Zonengeometrie-Modell im Kern (Polygon, Höhe, Geschoss, Kantenzuordnung), **2D-Grundriss je Geschoss mit G6c**, **3D-Ansicht mit G7b** — eine Komponente, Umschalter „Grundriss \| Körper", three.js lokal, Kennzeichnung „schematisch" (Kapitel 14) | Proben 25–27; Sichtabnahme Windows und iOS | **10–17 PT**, davon **4–7 PT hier** (3D-Ansicht); Zonengeometrie und 2D-Grundriss (**6–10 PT**) rechnet das [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) unter G6c |
| | **Summe G7** (einschließlich der 3D-Ansicht aus E11) | | **46–79 PT** |
| | **Summe G4c + G7** | | **63–107 PT** |

† **Ersparnis durch E11:** G7b und G7e rechnen die Geometrie nicht mehr selbst, sondern schreiben das
Zonengeometrie-Modell (Nachtrag 1, Kapitel 14) — **zusammen 3–5 PT weniger**: G7b 8–14 → **7–12 PT**,
G7e 10–18 → **8–15 PT**. **Was E11 an den Summen ändert:** −3–5 PT bei G7b/G7e, +4–7 PT für die
3D-Ansicht → **Summe G7 45–77 → 46–79 PT**, **Summe G4c + G7 62–105 → 63–107 PT**. Die übrigen
6–10 PT des Betrachters (Zonengeometrie-Modell und 2D-Grundriss) stehen im Mehrzonenkonzept unter
G6c und sind hier **nicht** mitgezählt.

Aufwände sind Größenordnungen für Entwicklung **und Nachweis**; Agentenarbeit verkürzt die
Kalenderzeit, nicht die Prüfzeit.

**Woher die Zahlen kommen, und was dazugekommen ist.** Befund R (6) veranschlagt **14–22 PT** für
den gbXML-Import **ohne** Persistenz, ohne Schemaschritt und ohne die Umbenennungen; Befund S (6, S2)
veranschlagt die Persistenz der Zuordnung mit **3–5 PT innerhalb** von S2 („die Zuordnung muss dafür
persistiert werden — eine Anforderung an den Import, die heute noch nicht drinsteht"). Dieses Papier
**zieht diese Arbeit nach G4 vor** (Kapitel 4, Nr. 1–4, und Kapitel 7). Deshalb: **G4c +3–6 PT**
(Schemaschritt, zwei Tabellen, Register, Reduzierskript, Auslieferungsvorlage, Umbenennungen,
Probe 14), **G7d −2–3 PT** (die Tabellen stehen dort schon). **G7a +1–2 PT** für die Ersatzschichtung
(5.3) — sie ist ein Vorschlag dieses Papiers und steckte in R's 8–12 PT nicht drin. Alle übrigen
Zahlen sind unverändert aus den Befunden übernommen. Umsetzungskonzept und Mehrzonenkonzept führen
für G4c noch die kleinere Zahl; sie ist dort nachzuziehen.

**Vorbedingungen.**

| Stufe | setzt voraus | Warum |
|---|---|---|
| G4c | **G1 + G2** | sonst importiert man in ein Tagesmodell, das die Daten nicht nutzt — dasselbe Argument, mit dem G4a hinter G2 steht |
| G4c mit Schichten | **G3** (Bauteilkatalog) | ohne `Tab_Bauteilaufbau`/`Tab_Bauteilschicht` gibt es kein Ziel für `Construction`/`Layer`/`Material`; **ohne G3 läuft G4c auf U-Werte und Flächen** und ist damit die kleinere Hälfte |
| G4c mit Zonen | **G6** | ohne `Tab_Zone` bleibt nur X4; X1…X3 laufen mit G6c (3.3, D16) |
| G7a/G7b | **G3** | ein Export ohne Schichten erzeugt im Ziel ein masseloses Gebäude (5.3) |
| G7c | **G4a** (Paket, Lizenzhinweisseite) und **G1/G2** (Ergebnisse) | das Paket ist dasselbe, die Ergebnisse sind der Inhalt |
| G7d | **G7c** und ein **genutzter** IFC-Import | ohne Fremddateien im Feld hat der Weg keinen Gegenstand (D7) |
| G7e | **G7c** und das vom Anwender **benannte Gegenüber** des IFC-Exports (D6, E27) | Körper ohne Semantik sind nichts, und ohne Empfänger ist zwischen S1 und S3 nicht zu wählen (6.1) |

**Reihenfolge und Begründung.**

1. **G4c vor G4a** — **entschieden mit E27 (D1)**; der Anwender hatte keine Präferenz, es gilt
   die Empfehlung dieses Papiers. Die Begründung: gbXML ist die kleinere Aufgabe, braucht kein
   Paket, keine Lizenzarbeit und keinen Geometriekernel-Ersatz; es schafft das Zuordnungsgerüst,
   das der IFC-Weg dann mitbenutzt. Die thermische Topologie kommt zuverlässiger mit — das Schema
   erzwingt sie zwar nicht, aber alle vier ausgezählten Dateien schreiben sie, während die
   IFC-Raumgrenzen eine selten exportierte MVD voraussetzen. Mit Archicad erschließt es den
   Autorensystem-Weg, der die thermischen Daten am vollständigsten liefern **dürfte** (Befund R, 6)
   — belegt ist das nicht: keine der vier Messdateien stammt von dort, die Feldzuordnung ist nicht
   aus der Primärquelle belegbar, und ein Fehlerbericht zum Export ist bis AC28 offen (1.2).
   **Dagegen** sprach die Praxislage: die deutsche Normungsarbeit läuft auf IFC, und was im Feld
   ankommt, weiß der Anwender — er hat keine Präferenz angegeben.
2. **G7a und G7b zusammen oder gar nicht** — **entschieden mit E27 (D2)**: ohne Stufe 2 ist der
   Export kein Simulationsmodell (5.1).
3. **G7c → G7d → G7e**: die semantische Stufe zuerst — **entschieden mit E27 (D6)** —, S3
   zuletzt, weil es den geringsten fachlichen und den höchsten Missverständnisertrag hat (Befund S,
   6). Vor der Stufe, die über die semantische hinausgeht, benennt der Anwender das Gegenüber des
   IFC-Exports.
4. **Die Persistenz (Kapitel 7) gehört in die erste gebaute Importstufe**, egal welche — sie
   nachzurüsten hieße, für alle vorher importierten Gebäude keine Zuordnung zu haben.

---

## 11. Fragen mit Empfehlung

Nur **neue** Fragen; Q1–Q23 des Konzepts, U1–U16 des Umsetzungskonzepts und M1–M14 des
Mehrzonenkonzepts stehen dort.

**Die drei Antworten, die dieses Papier zur Beauftragung brauchte, sind mit E27 (22.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.32)
gegeben:** die Reihenfolge (**D1**: gbXML vor IFC), ob G7 für gbXML lohnt (**D2**: nur mit
Stufe 2) und wie mit dem Gegenüber des IFC-Exports umzugehen ist (**D6**: semantische Stufe
zuerst; das Gegenüber benennt der Anwender vor der Stufe, die über die semantische hinausgeht).
Auch die übrigen Fragen des ersten Blocks — die **unwiderruflichen** technischen Festlegungen —
und der Rest D17 aus D3 sind entschieden, sämtlich nach Empfehlung. Die Fragen und Empfehlungen
bleiben als Begründung stehen; die Spalte „Empfehlung" trägt den Entscheidvermerk. Der zweite
Block ist zur Kenntnis.

### 11.1 Jetzt zu entscheiden

| Nr. | Frage | Empfehlung | Wann spätestens |
|---|---|---|---|
| **D1** | **Reihenfolge: gbXML-Import vor IFC-Import?** | **Entschieden mit E27 (22.09.2026): ja** — der Anwender hat keine Präferenz, es gilt die Empfehlung. Begründung: gbXML ist kleiner (kein Paket, keine CDDL-Auflage, kein Geometriekernel-Ersatz), liefert die thermische Topologie zuverlässiger und schafft dasselbe Zuordnungsgerüst. Die Frage hing allein daran, **welche Dateien im Feld ankommen**; die Reihenfolge des Umsetzungskonzepts (G4a zuerst) ist damit abgelöst | **vor der Beauftragung** von G4c/G4a |
| **D2** | **Lohnt G7 für gbXML nur mit Stufe 2?** | **Entschieden mit E27 (22.09.2026): ja**, der gbXML-Export kommt erst mit der zweiten Stufe. Ohne synthetische Geometrie ist der Export ein Datenblatt in XML-Form — legitim als Beleg-, Archiv- und Rundlaufformat, aber keine Interoperabilität: OpenStudio liest `RectangularGeometry` nicht, IES VE verlangt `PolyLoop`, DesignBuilder repariert Polygone, erfindet aber keine. **Ausnahme:** Ist das benannte Gegenüber Solar-Computer oder EVEBI, genügt G7a — dann ist aber vorher zu **belegen**, dass deren Importe eine geometriearme Datei annehmen; in Befund R ist es nicht belegt | **vor der Beauftragung** von G7 |
| **D4** | **Ergebnisgrößen in kWh mit explizitem `Unit` — oder in Joule mit Hinweis?** | **Entschieden mit E27 (22.09.2026): kWh mit explizitem `Unit`.** Kostet eine einmalige Helferfunktion und ist die ehrlichere Angabe; ohne `Unit` behauptet die Datei Joule. **Unwiderruflich** — eine spätere Umstellung entwertet alte Exporte | **vor der ersten Zeile Quelltext** (G7c) |
| **D5** | **Deterministische Kennungen (IFC-`GlobalId`, gbXML-`id`)?** | **Entschieden mit E27 (22.09.2026): ja, von Anfang an**, in beiden Exportformaten, aus dem Schlüsselpfad der **IDs**, nie aus Namen. Voraussetzung für jeden Modellvergleich beim Empfänger und für den Rundlauf. Nachträglich nicht mehr einzuführen | **vor der ersten Zeile Quelltext** (G7a und G7c) |
| **D6** | **Wer ist das Gegenüber des IFC-Exports — welches Werkzeug, welcher Anwender, welcher Zweck?** | **Entschieden mit E27 (22.09.2026) nach Empfehlung: die semantische Stufe zuerst** — G7c bauen, G7e zurückstellen. **Das Gegenüber (Werkzeug, Zweck) bleibt beim Anwender zu benennen**, fällig vor der Stufe, die über die semantische hinausgeht. Begründung: Ohne benannten Empfänger ist zwischen S1 (Rechenwerkzeug, Betrachter bleibt leer) und S3 (Betrachter zeigt etwas, Verwechslungsgefahr hoch) nicht sinnvoll zu wählen | Benennung des Gegenübers vor **G7e**; G7c geht ohne |
| **D11** | **Vertragliche Zulässigkeit der Rückgabe fremder IFC-Dateien** | **Entschieden mit E27 (22.09.2026): zulässig, mit Kennung in der Datei und Beipackzettel** (6.6). Die Reference View sagt ausdrücklich, dass der Empfänger das Modell nicht verändern soll. Technisch ist der Weg billig; rechtlich ist er der Grund, ihn zu lassen. Mindestens: Beipackzettel, eigene `IfcApplication`, neuer Dateiname, und ein Hinweis im Dialog, den der Anwender bestätigt | vor **G7d** |
| **D16** | **Erweitert die Zonenbildung aus gbXML (X1…X3) den Entscheid E7 auf ein zweites Format?** | **Entschieden mit E27 (22.09.2026): ja.** E7 nennt für das Mehrzonenmodell den IFC-Import; G6c führt heute nur Z1…Z5 (Mehrzonenkonzept 9, 16–26 PT einschließlich Grundrissansicht aus Nachtrag 1). Die gbXML-Regeln X1…X3 sind dieselbe Bauform auf denselben Tabellen und laufen sinnvoll **mit G6c** mit; **X4** (Einzonen-Rückfall) gehört ohnehin in G4c. Ein Nein hätte gbXML einzonig gelassen; der Zuwachs von G6c ist damit gesetzt | **vor der Beauftragung** von G4c |

### 11.2 Technische Festlegungen zur Kenntnis

Diese beantwortet das Papier selbst; sie stehen hier, damit der Anwender widersprechen kann, nicht
damit er entscheiden muss.

| Nr. | Frage | Empfehlung |
|---|---|---|
| **D3** | **XSD und Testdateien ohne Lizenz** | **XSD nicht ausliefern**, nur als lokale Kopie im Test, nie aus dem Netz. **Die vier gbxml.org-Beispieldateien nicht ins Repositorium** — die Prüfdateien erzeugt der eigene Exporteur im Rundlauf, dazu von Hand geschriebene Kleinstdateien je Fehlerbild (8.3). **Für das Installationspaket ist die Rechtslage damit gegenstandslos. Die Ablage der XSD-Kopie ist entschieden (D17, E27 vom 22.09.2026): außerhalb des Repositoriums** — `.gitignore`, Einrichtungshinweis, LIESMICH-Zeile mit Herkunft, Abrufdatum und Lizenzstand „keine"; der Validierungstest wird benannt übersprungen, wenn die Datei fehlt (8.2) |
| **D7** | **S2 (Round-Trip) streichen, wenn der IFC-Import im Feld kaum genutzt wird?** | **Zurückstellen, nicht streichen.** S2 hat das beste Verhältnis von Nutzen zu Aufwand, aber nur, wenn fremde Dateien ankommen — und für den Wohngebäudebestand verneint Befund C das ausdrücklich. **Die Persistenz (Kapitel 7) wird trotzdem in G4 gebaut**, weil sie auch die Herkunft trägt; sie ist nicht der teure Teil |
| **D8** | **`IfcHerkunft` → `Importherkunft` mit dem Wert `GbXml`?** | **Ja, je Format ein eigener Wert** (`Ifc`, `GbXml`), nicht ein gemeinsames `Datei` mit dem Format eine Tabelle weiter. Der Anwender soll in der Zelle sehen, woher die Zahl kommt |
| **D9** | **`Tab_Zone.IfcGuid`/`Tab_Bauteil.IfcGuid` in `Quellkennung` umbenennen, bevor S-C gebaut wird?** | **Ja.** Die Spalten gibt es noch nicht, die Umbenennung kostet nichts, und eine gbXML-`id` in einer Spalte namens `IfcGuid` ist eine Unwahrheit. Zugleich `Herkunft` um `'GBXML'` erweitern und die Länge auf 64 Zeichen setzen |
| **D10** | **Ersatzschichtung beim Export, wenn EPOS nur U-Wert und `Bauweise` führt — oder masselos schreiben und melden?** | **Ersatzschichtung mit Kennzeichnung.** Ein masseloses Gebäude im Zielwerkzeug ist für jede dynamische Rechnung falsch, und das Zielwerkzeug merkt es nicht. **Der Vorbehalt gehört dazu:** Die Ersatzschichtung trifft U-Wert und Gesamtwärmekapazität, **nicht die Lage der Masse im Aufbau**; das dynamische Verhalten im Zielwerkzeug weicht deshalb von der EPOS-Rechnung ab. Das muss wörtlich in `Construction/Description` und in die Meldung an den Anwender — Befund R empfiehlt die Ersatzschichtung nicht, sie ist ein Vorschlag dieses Papiers |
| **D12** | **`AirChangesPerHour` ist eine Zahl, EPOS hat zwei Luftwechselspalten** | **Auf `Luftwechsel_Infiltration` legen, `Luftwechsel_Nutzer` NULL lassen** (= Wert des Gebäudes). Eine Aufteilung wäre eine Annahme, und Annahmen gehören nicht in eine Importzeile mit Herkunft `GbXml` |
| **D13** | **gbXML-Zonenregel X1 nur, wenn Zahl der `Zone` < Zahl der `Space`?** | **Ja.** Revit schreibt je Raum eine `Zone`; ohne den Zusatz entstünden aus einer Bürodatei 93 EPOS-Zonen, und die Mindestgrößenregel müsste sie hinterher wieder zusammenräumen |
| **D14** | **Wo stehen die Exporte in der Oberfläche?** | **Als Überlagerung im Gebäudedialog**, wie der Import — kein neuer Menüpunkt, kein neuer Maskenschlüssel; das Menü ist Daten (`Menuetabelle.cs`), und ein Untermenü mit einem Punkt ist verboten. Zusätzlich ein Einstieg dort, wo die Ergebnisse liegen (Bedarfsdialog), sobald G1/G2 stehen |
| **D15** | **Größengrenze für gbXML** | **25 MB Windows / 10 MB iOS**, benannt abgelehnt statt versucht. Die größte gemessene Datei hat 16,3 MB und 648 885 Knoten; die iOS-Zahl ist **zu messen** |

---

## 12. Risiken

| Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|
| **Falsches Versprechen.** Ein Quader mit richtiger Bilanz und erfundener Gestalt (gbXML G7b, IFC G7e) wird für ein Gebäudemodell gehalten; seine Maße werden in einem Aufmaßwerkzeug klaglos abgegriffen | Der Empfänger rechnet mit erfundener Geometrie weiter | Kennzeichnung an drei Stellen, verbindlich (8.4); Prüfbilder als Abnahmekriterium; nie „Gebäudemodell" in Text oder Dateinamen |
| **Masselose Aufbauten im Ziel.** Führt EPOS nur U-Werte, entsteht im Zielwerkzeug ein Gebäude ohne Speichermasse | Für jede dynamische Rechnung falsch — und das Zielwerkzeug merkt es nicht | Ersatzschichtung (D10) oder benannte Meldung **vor** dem Schreiben; nie stillschweigend |
| **`version="8.01"` wird „korrigiert"** | Die Datei fällt bei jeder XSD-Prüfung durch | Kommentar im Quelltext mit der Begründung aus Befund R, 1.1; Probe 3 fängt es ab |
| **gbXML ist kein gepflegter Standard mehr.** 8.01 ist byteweise 7.04, letzter Push der Beispieldateien 2020, die Werkzeugliste führt abgekündigte Produkte | Der Exporteur veraltet mit dem Schema, ohne dass es auffällt | Aufzählungswerte als C#-Konstanten an **einer** Stelle; keine Abhängigkeit vom Netz; der Rundlauf ist der einzige Maßstab, der immer trägt |
| **Datenlage.** Für den deutschen Wohngebäudebestand gibt es kaum IFC- und kaum gbXML-Dateien | Beide Importe laufen im Feld selten an | **Beide sind Komfort, kein Ersatz für die manuelle Eingabe** — das gehört in die Anwenderführung und in die Aufwandsabwägung, nicht erst in die Enttäuschung |
| **Stiller Entitätenverlust beim Round-Trip.** Was der Leser nicht instanziieren konnte, ist nach dem Zurückschreiben weg | Eine fremde Datei wird beschädigt zurückgegeben | Protokollprüfung auf **beide** Verlustkanäle als **Sperre**, nicht als Warnung; `ignoreTypes` nie setzen (6.6, Probe 13) |
| **Der Round-Trip trifft eine 2x3-Datei.** Die EPOS-Ergänzungen lassen sich nur mit den konkreten Schematypen anlegen; `IfcRelSpaceBoundary2ndLevel` gibt es dort nicht | Entweder eine zweite Schemabaugruppe gegen **E3** — oder ein Weg, der im Feld auf halber Strecke stehen bleibt | G7d auf Quelldateien im Schema **IFC4** begrenzen und alles andere benannt ablehnen; `Tab_Importquelle.Schemastand` ist das Kriterium (6.6, 7.1) |
| **Unwiderrufliche Festlegungen** (Kennungen, Einheiten) werden spät getroffen | Alte Exporte werden entwertet, Modellvergleiche beim Empfänger brechen | D4 und D5 sind **vor** der ersten Zeile Quelltext entschieden (E27, 11.1) — kWh mit ausdrücklicher Einheit, deterministische Kennungen in beiden Formaten |
| **Speicher auf iOS.** `MemoryModel` hält das IFC-Modell, `XDocument` das gbXML-Dokument vollständig im Arbeitsspeicher | Abbruch ohne erklärbares Fehlerbild | Größengrenzen je Plattform, **gemessen** (D15, Frage U11); benannte Ablehnung statt Versuch |
| **CDDL § 3.1 nicht erfüllt** — die Lizenzhinweisseite fehlt | Der IFC-Weg ist **nicht auslieferbar**, Import wie Export | Seite mit G4-1 anlegen, für **alle** Fremdanteile (8.1, U10, entschieden mit E27); nie forken, nie patchen |
| **Vertragliche Frage der Rückgabe** wird erst nach der Umsetzung gestellt | 6–11 PT für einen Weg, den man nicht anbieten darf | D11 ist vor G7d entschieden (E27): zulässig mit Kennung in der Datei und Beipackzettel — die Auflagen sind Abnahmekriterium von G7d (10) |
| **Zwei Formate, ein Dialog** wächst zu einer Maske mit Sonderfällen | Der Dialog wird unwartbar, die Formate laufen auseinander | Was sich unterscheidet, steht als **Daten** im Profil (2.4); ein Wächtertest hält Formatnamen aus der Komponente heraus |

---

## 13. Abgrenzung — was dieses Papier nicht behandelt

- **Die Physik, der Rechenweg und der Gebäudedialog.** Zweikapazitätenmodell, Randbedingungen,
  Vorlauf, Plausibilitätsprüfungen, U·A-Tabelle nach E2 — das steht im
  [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) und im
  [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md).
- **Der IFC-Import selbst.** Ablauf, Klassen, Meldungsschlüssel, Azimutkette, Paketwahl,
  iOS-Trimming, Importprobe: Umsetzungskonzept Kapitel 3 (G4a) und
  [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kapitel 6 (G6c). Hier stehen nur die
  vier Ergänzungen aus Kapitel 4.
- **Das Zonenmodell und sein Datenmodell.** Zonenkopplung, Gruppenbildung AW/IW, Bauteilreduktion,
  Baustoffkatalog, Tabellen `Tab_Zone`/`Tab_Bauteil`/`Tab_Bauteilaufbau`/`Tab_Bauteilschicht`/
  `Tab_Baustoff` — Mehrzonenkonzept Kapitel 2 bis 5. Dieses Papier **benutzt** sie und benennt seine
  Präzisierungen daran in 1.4 (Zeilen 2, 3 und 7).
- **Die Geometrieableitung aus IFC-Körpern** (`IfcExtrudedAreaSolid`, `IfcBooleanClippingResult`,
  `IfcMappedItem`, Placement-Kette) — Stufe **G5**, Begründung im Umsetzungskonzept 3.1.
- **Anlagentechnik in beiden Formaten.** gbXML `AirLoop`/`HydronicLoop`/`AirSystem`/
  `ZoneHVACEquipment` und die IFC-Verteilungselemente werden weder gelesen noch geschrieben; das
  EPOS-Anlagenmodell passt nicht auf das US-HVAC-Schema.
- **Nutzungsprofile und Zeitpläne.** gbXML `Schedule`/`YearSchedule`/`WeekSchedule`, `Occupants`,
  `Behaviors` und die IFC-Zeitreihen bleiben ungelesen; EPOS führt seine Profile selbst.
- **Verschattung durch Nachbarbebauung, Tageslicht, Wärmebrückenrechnung, Feuchte** — wie in
  Konzept 15 abgegrenzt. `surfaceType="Shade"` wird benannt übergangen.
- **BCF und die Erzeugung von IDS als Werkzeug.** EPOS liefert **eine** IDS-Datei mit seiner
  Exportzusage mit; ein IDS-Editor oder ein Rückmeldeweg über BCF ist nicht Gegenstand.
- **Projektaustausch zwischen zwei EPOS-Installationen.** Dafür gibt es
  `ProjektExportImportCtrl`; gbXML und IFC sind Fremdformate, kein Sicherungsformat.
- **Ein vollwertiger IFC-Betrachter im Programm.** web-ifc mit three.js in der WebView (15–25 PT,
  MPL-2.0, auf iOS speicherkritisch) und die native xBIM Geometry Engine (nur Windows, OCCT unter
  LGPL) sind mit **E11 benannt abgelehnt** und bleiben spätere Optionen nur bei Bedarf aus der
  Praxis; gebaut wird der Gebäudebetrachter auf der Exportgeometrie (Nachtrag 1, Kapitel 14).
- **Die Entscheidung, ob und wann G4c und G7 beauftragt werden.** Dieses Papier legt vor.

---

## 14. Nachtrag 1 — Gebäudebetrachter auf der Exportgeometrie (E11, 15.09.2026)

Anwender, 15.09.2026, im Wortlaut:

> „ergänze im Konzept: ifc Viewer — Variante: 2D-Grundriss je Geschoss aus den Raumgrenzen (SVG in
> einer Razor-Komponente) und Schematische Körper aus EPOS-Daten (Quader je Zone, Platte je
> Bauteil) — Zusammen gebaut."

Das ist **Entscheid E11** ([Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Nachtrag
N1.16). Für dieses Papier hat er eine einzige, aber tragende Folge: **Die Geometrie, die G7b und G7e
schreiben, wird nicht mehr im Exporteur gerechnet.** Sie entsteht einmal im Kern und wird von der
Ansicht und von beiden Exporten gelesen. 5.5 und 6.7 bleiben als **Herleitung** gültig; was sich
ändert, ist der Ort, an dem sie steht.

### 14.1 Das Zonengeometrie-Modell — eine Quelle für drei Ausgaben

Im Rechenkern entsteht ein **Zonengeometrie-Modell** (Arbeitsname; den endgültigen Namen setzt das
Architekturpapier — es führt das Modell als `Zonengeometrie` mit `Zonenumriss`,
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 1.3): je Zone ein
**Grundrisspolygon**, eine **Höhe**, ein **Geschoss** und die
**Zuordnung der Bauteile** zu den Polygonkanten bzw. zu Boden und Decke.

**Woher das Polygon kommt.**

1. **Mit IFC** aus den Raumgrenzen (`IfcRelSpaceBoundary`): der Polygonflächeninhalt ist ohne
   Geometriekernel zu rechnen (Befund P; [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md)
   6.2), das Geschoss steht an `IfcBuildingStorey`.
2. **Ohne IFC** als Rechteck aus Zonenfläche und dem Seitenverhältnis der Bauteilgruppen:
   h = V/A, l = A_NS/(2·h), b = A_OW/(2·h) — **dieselbe Herleitung wie 5.5**, samt Bruttoflächenregel,
   10-%-Probe gegen die Nutzfläche und Rückfall auf das Quadrat.
3. **Im Einzonenmodell** (E7) ist das ein Quader je Gebäude.

Die **Anordnung** der Zonen zueinander folgt weiter 5.5, Punkt 3: keine Stapelung, Zonen mit
gemeinsamer Trennfläche aneinandergelegt, sonst Reihung je Geschoss — und genau deshalb ist sie
**erfunden**, solange keine Raumgrenzen vorliegen.

### 14.2 Die beiden Ansichten

| Teil | Technik | Stufe | Aufwand |
|---|---|---|---|
| **Zonengeometrie-Modell** | Kern, plattformfrei: Polygon, Höhe, Geschoss, Zuordnung der Bauteile zu Kanten, Boden und Decke | **G6c** (Mehrzonenkonzept 9) | 3–5 PT |
| **Ansicht 1 — 2D-Grundriss je Geschoss** | **SVG in einer Razor-Komponente, keine Bibliothek**; Räume und Zonen als Polygone, Farbe je Zone, Klick wählt die Zone bzw. ordnet sie zu | **G6c** — im Zuordnungsdialog des IFC-Imports (Mehrzonenkonzept 6.4 und 6.7); der **gbXML-Import speist dieselbe Ansicht** über `Space`/`Zone` (3.3) | 3–5 PT |
| **Ansicht 2 — schematische Körper** | Polygon um die Höhe extrudiert (bei Rechteckgrundriss ein Quader je Zone), **Platte je Bauteil** an Kante, Boden und Decke; **three.js (MIT) lokal** unter `EPOS.UI/wwwroot`, nie vom CDN | **G7b** — Sichtprüfung dessen, was G7b als `PolyLoop` und G7e als `IfcExtrudedAreaSolid` schreibt | 4–7 PT |
| | | **zusammen** | **10–17 PT** |

**Eine Komponente, ein Umschalter** „Grundriss \| Körper" (Arbeitsname `GebaeudeAnsicht.razor`) —
nicht zwei Seiten, die auseinanderlaufen.

### 14.3 Was sich für G7b und G7e ändert

- **G7b (gbXML Stufe 2)** liest Polygone und Höhen aus dem Zonengeometrie-Modell und schreibt sie
  als `PlanarGeometry/PolyLoop` (dazu `ShellGeometry`, 5.5). Die kantenschlüssige Regel (1 mm,
  IES VE) und die benannte Ablehnung bei widersprüchlicher Anordnung bleiben unverändert — sie
  greifen jetzt am Modell, nicht im Exporteur.
- **G7e (IFC-Export S3)** liest dasselbe Modell und schreibt `IfcRectangleProfileDef` →
  `IfcExtrudedAreaSolid` samt Placement-Kette (6.7). **Derselbe Grundriss, dieselbe Höhe, dieselbe
  Anordnung** wie in der 3D-Ansicht und im gbXML; ein Unterschied zwischen beiden Dateien ist damit
  ein Fehler, keine Auslegung.
- **Die Ansicht ist der Prüfstand.** Bisher war die Sichtprobe der Exporte ein fremdes Werkzeug
  (Probe 2, Probe 16). Mit der 3D-Ansicht sieht der Anwender **vor** dem Schreiben, was er
  verschickt.
- **Ersparnis 3–5 PT** (Kapitel 10, Fußnote). Getrennt gebaut wären es 10–20 PT plus eine zweite,
  abweichende Geometrie.

### 14.4 Regeln

1. **Kennzeichnung „schematisch" in der Oberfläche**, nicht nur in der Datei. 8.4 verlangt den
   Vermerk in `Campus/Description`, `Building/Description`, `FILE_DESCRIPTION`, Projektname und
   `IfcAnnotation`; die Ansicht trägt ihn **sichtbar am Bild**. Ein Bild, das aussieht wie ein
   Gebäude, ist dieselbe Verwechslungsgefahr wie eine Datei, die aussieht wie ein Gebäude
   (Kapitel 12, „Falsches Versprechen").
2. **three.js kommt auf die Lizenzhinweisseite** (8.1, Frage U10) — MIT, lokal ausgeliefert, nie vom
   CDN; dieselbe Auflage wie für jeden anderen Fremdanteil.
3. **iOS:** WebGL läuft in der WebView; die Dreieckszahl ist klein (je Zone ein Quader und je Bauteil
   eine Platte), ein Speicherproblem entsteht daraus nicht — anders als bei einem vollwertigen
   IFC-Betrachter (14.6).
4. **Keine Geometrie ohne Herkunft.** Jede Zone weist aus, ob ihr Polygon aus Raumgrenzen oder aus
   der Rechteckherleitung stammt — dieselbe Herkunftsregel wie je Feld (2.2).

### 14.5 Abnahmeproben

| Nr. | Probe | Kriterium |
|---|---|---|
| 25 | **Determinismus der Geometrie** | Dieselbe Eingabe liefert dieselben Polygone (Koordinaten auf 1e‑6) und **byteweise gleiche** Exporte; die Probe läuft über das Modell, nicht über das Bild, und ergänzt die Proben 2 und 12 |
| 26 | **Komponentenprobe** (bunit) | `GebaeudeAnsicht` zeichnet je Zone ein Polygon mit der Zonenfarbe, der Klick meldet die Zone an den Wirt, der Umschalter wechselt die Ansicht, die Kennzeichnung „schematisch" steht im gerenderten Baum; kein Anzeigetext ist Steuerwert |
| 27 | **Ansicht und Datei zeigen dasselbe** | `PolyLoop` (G7b) und `IfcExtrudedAreaSolid` (G7e) werden **gegen das Zonengeometrie-Modell** gehalten, nicht gegeneinander; Abweichung ist ein Fehler |

### 14.6 Was benannt abgelehnt ist

- **Ein vollwertiger 3D-IFC-Betrachter** (web-ifc mit three.js in der WebView): 15–25 PT,
  **MPL-2.0** (Datei-Copyleft, dazu die WASM-Auslieferung), auf iOS speicherkritisch — dasselbe
  Argument wie bei `MemoryModel` (Kapitel 12, „Speicher auf iOS").
- **Die native xBIM Geometry Engine:** nur Windows, OCCT unter LGPL — gegen
  [`ADR-003`](ADR-003_IFC_xBIM_ohne_Geometriekernel.md) und gegen die Plattformfreiheit des Kerns.

Beides bleibt eine **spätere Option nur bei Bedarf aus der Praxis** und ist nicht geplant. **„Datei
extern öffnen"** bleibt als Handgriff über `Dienste.Datei` zulässig — für den Anwender, der eine
fremde IFC-Datei wirklich ansehen will, ist das der ehrlichere Weg als ein halber eigener
Betrachter.
