# Protokoll G5-Nachbesserung — Körpervergleich an echten HottCAD-Dateien (07.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#808**. Commits: `2b0dc69be` (Ohrenschnitt), `cb251e48c` (Bezugsgrößen), `bc247d1fb` (Meldungen je Bauteilart, Aussparung, Körperrest).
**Entscheid:** E101; Abstimmung [G5 IFC](../../../aktuell/Gebaeudesimulation/2026-10-07_Abstimmung_G5_IFC.md); Vorgänger [G5-1 und G5-2](2026-10-07_G5-1_G5-2_Bauteilkoerper_Oeffnungen.md).

## 1 Anlass

Der Anwender importierte unter Windows zwei echte HottCAD-Dateien. Der Import meldete Dachflächen aus dem Körper mit 129,8 % bzw. 3646,9 % Abweichung gegen den Mengensatz, dazu 119 bzw. 219 Warnungen je Datei.

## 2 Befund

1. **Ohrenschnitt bei Löchern mit gleicher größter x-Koordinate** (`EPOS.Kern/Allgemein/Import/Ifc/IfcRaumkoerper.cs`, `Bruecke`): Die Brücke eines weiteren Lochs endete an einem Punkt, an dem schon eine Brücke hing, und wurde am ersten Vorkommen eingefügt. Das Vieleck verdrehte sich, Dreiecke wurden doppelt gezählt (Oberlichtreihen, Fensterbänder als `IfcFaceBound`). Betrifft auch Raumkörper.
2. **Ungleiche Bezugsgrößen:** HottCAD gliedert ein Bauteil in ein Element mit Körper und gleichnamige Teile ohne Darstellung (Name gleich oder mit „-n“). Der Körper trägt das ganze Bauteil, der Mengensatz nur einen Teil. Verglichen wurde Körper gegen Netto.
3. **Warnungsflut:** je Bauteil eine Warnung, auch bei kleinen Abweichungen.
4. **Nordrichtung:** Keine der Dateien trägt TrueNorth; die Projektdatei (`BmElement.Orientation`) liegt im selben ungedrehten System (19 von 22 Wänden Differenz 0°). Ein Nordwinkel ist nicht ableitbar und muss vom Anwender kommen (eigener Teil G5-N).

## 3 Gebaut

- **`2b0dc69be`:** Prüfung `ImKeil` in `Bruecke`; die Brücke wird am richtigen Vorkommen eingefügt.
- **`cb251e48c`:** Summe der Körper gegen Summe aller Mengensätze des gegliederten Bauteils; Vergleich gegen Brutto und Netto, die kleinere Abweichung gilt. Probe `ifc4_g5_flachdach_teile.ifc`.
- **`bc247d1fb`:**
  - Info je Bauteilart (`IfcAbbildBauer.Koerpergruppe`: Außenwand, Innenwand, Dach, Boden und Decke, sonstige; Schlüssel `IMP_IFC_PROT_KOERPER_ABWEICHUNGEN_*`): Anzahl, Median, größte Abweichung mit Name.
  - Einzelwarnung erst ab 25 % (`IfcBauteilkoerper.EINZELWARNUNG_GRENZE = 0.25`).
  - Teile-Info nur je Bauteilart (`KOERPER_TEILE_*`); Schlüssel `IMP_IFC_PROT_KOERPER_TEILE` entfernt.
  - Ausgesparte Öffnungen (`IfcOeffnungen.Ausgespart`, `AUSSPARUNG_TOLERANZ_M = 0.01`, `AUSSPARUNG_ABSTAND_M = 0.5`): Brutto = Körper + ausgesparte Öffnungen, jede Öffnung genau einmal abgezogen, Info `OEFFNUNG_AUSGESPART`.
  - Körper ohne Mengensatz mit Teilen: Die Teile behalten ihre Mengensätze, das Körperelement bekommt nur den Rest max(0, Körper − Σ Brutto der Teile); Rest bis 2 % wird 0, Info `KOERPER_REST`.

## 4 Proben

- `ifc4_g5_flachdach_teile.ifc`: Dach in Körperelement und Teile.
- `ifc4_g5_aussparung.ifc`: Körper spart die Öffnung aus; Abzug nur einmal.
- `ifc4_g5_abweichungen.ifc`: Zusammenfassung je Bauteilart und Einzelwarnung ab 25 %.
- Geänderte Erwartungen nur bei Meldungen: Wandhaus-Gegenprobe (4,8 %) jetzt in der Zusammenfassung, Flachdach (2,4 %) in der Dach-Zusammenfassung.
- Anwenderdateien (lokal geprüft, nicht im Repository): Warnungen 129 auf 13 bzw. 231 auf 40; alle Bauteile mit Mengensatz flächengleich (285/285 bzw. 470/470).

## 5 Prüfung

Abnahmefilter 895 bestanden, 4 übersprungen; UI-Tests 136/136; Kern und Windows-Schale 0 Fehler; Referenzlauf der acht CI-Projekte gegen `2026-10-07_R40_Erdreichquellen` PASS. Rechenweg unverändert.

## 6 Gate 808

Hauptbaum `a9e2e120c`, 88 min: 20 723 Tests, 20 717 grün, 6 übersprungen, 0 rot (KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 775, EPOS.Kern 11 974 mit 5 übersprungen); Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 22/22 gegen `2026-10-07_R40_Erdreichquellen` PASS, 677/677 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 498 Texte, 0 Fundstellen; Windows-Schale 0 Fehler; UI-Tests Gebäudeimport/Ansicht/Aufbau 136/136.

## 7 Offen

- In einer der Dateien weichen drei Böden/Decken um ein Vielfaches ab (Median 318 %): Der Körper deckt die ganze Geschossplatte, der Mengensatz nur den Teil an der Hülle. Es gilt der Mengensatz; Prüfung in G5-3.
- Nordrichtung beim Anwender abfragen (G5-N).
- G5-3.
- Logbuch-Eintrag und Wiki-Upload gebündelt.
