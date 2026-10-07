# Protokoll G5-1 und G5-2 — Bauteilkörper als Rechengröße und Abzug der Öffnungen beim IFC-Import (07.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeilen **#800** (G5-1) und **#801** (G5-2). Commits G5-1: `b6d6f9e6`, `0b04240a`, `a4313ace`, zusammengeführt in `a7a7f5c16`; G5-2: `2e2067d7b`, `e97a886b5`, zusammengeführt in `519e1db54`.
**Entscheid:** E101; Abstimmung [G5 IFC](../../../aktuell/Gebaeudesimulation/2026-10-07_Abstimmung_G5_IFC.md), Anforderungen A1 bis A5.

## 1 Auftrag

G5-1: Liefert die IFC-Datei für ein Bauteil keine Mengen, ermittelt der Import Fläche, Neigung und Ausrichtung aus dem Bauteilkörper. G5-2: Öffnungen (Fenster, Türen, Durchbrüche) werden von der Wand- bzw. Dachfläche abgezogen, wo die Datei keine Nettofläche nennt. Kein Rechenweg, kein SQL.

## 2 Gebaut

**G5-1**

- **`IfcBauteilkoerper`** wertet das Dreiecksnetz aus `IfcRaumkoerper.Lesen` aus (gleiche Darstellungsarten, Placement-Kette, mm nach m). Ebene Flächen: Richtung bis 1e-6, Abstand höchstens 1e-4 m. Stirnflächen werden verworfen (Dicke = kleinster Abstand gegenläufiger Ebenen; Stirn, wenn Fläche/Diagonale höchstens 1,5 Dicken).
- **Wand:** senkrechte Flächen (|n_z| < 0,5); die Außenseite zeigt vom Gebäudeschwerpunkt weg (Mittel der Raumkörper, sonst des Bauteilkörpers, sonst Normale der größten Fläche mit Vermerk); maßgeblich ist die größere Seite. **Platte und Dach:** Ober- bzw. Unterseite (n_z > 0,17), Neigung aus der Normalen, Azimut bei geneigter Fläche.
- **Gliederung:** Teilflächen mit weniger als 5° Richtungsunterschied werden flächengewichtet zusammengefasst (Vermerk), ab 5° getrennt geführt. Azimut über TrueNorth bzw. `IfcMapConversion` (`IfcPlatzierung.Azimut`).
- **`Flaechenherkunft`** (`Mengensatz`, `Raumgrenze`, `Koerper`, `Schematisch`, `FlaechenherkunftWerte`), getragen von `AbbildBauteil`, `Zonenflaeche` und `GebaeudeBauteilzeile`; gespeichert erst mit Schemaschritt 197.
- **`IfcAbbildBauer`, Rangfolge A5:** Der Mengensatz bleibt Quelle. Ohne Mengensatz trägt der Körper die Bruttofläche mit Herkunft `Koerper`; mit Mengensatz nur Vergleich, über 2 % Warnung `IMP_IFC_PROT_KOERPER_ABWEICHUNG`. Ein Dach ohne eigene Darstellung bringt die Körper seiner Platten mit. `IfcCovering` liest der Import nicht als Hüllbauteil.
- Fünf Ressourcenschlüssel in beiden Sprachen: `IMP_IFC_PROT_FLAECHE_KOERPER`, `_KOERPER_ABWEICHUNG`, `_KOERPER_GEGLIEDERT`, `_KOERPER_ZUSAMMENGEFASST`, `_KOERPER_AUSSENSEITE`.

**G5-2**

- **Ausgangslage:** Der Import las Fenster und Türen schon als Öffnungen ihres Wirts (`IfcRelVoidsElement`, `IfcRelFillsElement`, Dachfenster über `IfcRelAggregates`); der Abzug lief im nachgelagerten Weg (`GebaeudeHuelleneinordnung`, `GebaeudeBauteilvorschlag.Netto`). Es fehlten Öffnungen ohne Füllung, die Fläche aus dem Körper und eine Nettofläche im Abbild.
- **`IfcOeffnungen`:** Profilfläche in der Ebene des Wirts, Tiefe längs der Normalen, Nischenprüfung mit 1 mm Toleranz, Netto = max(0, Brutto − Abzug).
- **Rangfolge der Öffnungsfläche:** Mengensatz Fenster/Tür, `Qto_OpeningElementBaseQuantities`, `OverallWidth × OverallHeight`, Flächenname aus beliebigem Satz, Körper der Öffnung in der Ebene des Wirts, Körper des Fensters bzw. der Tür. Herkunft `Mengensatz` für die ersten vier, `Koerper` für die Körperwege (Meldung `OEFFNUNG_KOERPER`).
- **Loch** (Öffnung ohne Füllung, durchdringt den Wirt): `AbbildBauteil.LochflaecheM2`, abgezogen, Meldung `OEFFNUNG_LOCH`. **Nische** (`RECESS` oder Tiefe kleiner als die Dicke des Wirts): nicht abgezogen, Meldung `OEFFNUNG_NISCHE`. Ohne Fläche `OEFFNUNG_UNBEMESSEN`.
- **Nettofläche am Wirt**, wo die Datei keine nennt: Brutto (Mengensatz oder Körper) − Öffnungen; höchstens 0 ergibt 0 mit Warnung `OEFFNUNG_NETTO_NULL` (im nachgelagerten Weg Info `NETTOFLAECHE_RUECKFALL` statt Fehler `NETTOFLAECHE_NEGATIV`). Ein Nettomengensatz bleibt unverändert. Kein Doppeln (`_gefuellt`).
- Fenster und Türen übernehmen Neigung und Azimut des Wirts (Dachfenster 36,87°/180°).

## 3 Proben

Erzeuger `EPOS.Kern.Tests/IfcProbenErzeuger.Bauteilkoerper.cs` und `IfcProbenErzeuger.Oeffnungen.cs`.

- `ifc4_g5_wand_extrusion.ifc`, `ifc4_g5_wand_brep_mapped.ifc`: Wandfläche aus Extrusion bzw. Brep mit Abbildung.
- `ifc4_g5_mengen_gegenprobe.ifc`: Nordwand mit 26,25 statt 25 m² im Körper, Meldung mit 4,8 % Abweichung.
- `ifc4_ohne_mengen.ifc`: 90 m² Außenwand aus dem Körper statt der Meldung `KEINE_MENGEN` (Test umbenannt in `Ohne_Mengen_kommt_die_Flaeche_aus_dem_Bauteilkoerper`); `GebaeudeBauteilvorschlagTests` hält `KEINE_AUSSENBAUTEILE` über geleerte Flächen derselben Probe.
- `ifc4_g5_oeffnungen.ifc` (ohne Mengensätze): Süd 22,0, West 18,0, Nord 24,0 mit Loch 1,0, Dach 98,8 m², Gaube Netto 0. `ifc4_g5_oeffnungen_mengen.ifc` ist die Gegenprobe mit Mengensätzen.

## 4 Prüfung

- G5-1: `IfcBauteilkoerperTests` 13 grün, 1 übersprungen (Erzeuger). Referenzlauf der acht CI-Projekte gegen R39 byte-gleich; nach dem Merge die betroffenen Kern-Klassen 982/985 (3 übersprungen).
- G5-2: `IfcOeffnungenTests` 15 grün, 1 übersprungen; voller Filterlauf 997 bestanden, 4 übersprungen; UI-Tests 136/136; Windows-Schale 0 Fehler; Referenzlauf der acht CI-Projekte gegen `2026-10-07_R40_Erdreichquellen` ohne Abweichung. Keine geänderten Erwartungen.

## 5 Gate 800

⟨GATE800⟩

## 6 Offen

- **G5-0:** Schemaschritt 197 `FlaechenherkunftSchema` (`Tab_Bauteil.Flaechenherkunft`) wartet auf Schritt 196 der Sitzung Wirtschaftlichkeit.
- **G5-3:** gegliederte Wände je Teilfläche als eigene Zeile (Teile liegen in `AbbildBauteil.Koerperflaeche.Teile`); Flächen je Raum und Zone aus Raum- und Bauteilkörper; Löcher nach Lage statt nach Flächenanteil verteilen; Fenster und Türen mit eigener Raumzuordnung; Herkunft in Zuordnungsdialog und Bauteilsteckbrief; Richtung bei Wänden gegen Erdreich; Dachneigung bei Dach mit Mengensatz aus dem Körper.
- Ungeprüfter Codeweg: Dach mit Mengensatz und Körper nur in den Platten.
- Sichtabnahme unter Windows mit einer Probe.
- Logbuch-Version und Wiki-Upload gebündelt.
