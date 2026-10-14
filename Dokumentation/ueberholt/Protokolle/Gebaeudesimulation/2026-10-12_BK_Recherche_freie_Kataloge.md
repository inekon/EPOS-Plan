# BK Recherche: freie Kataloge von Baustoffen und Bauteilaufbauten

Stand 2026-10-11. Zenodo, oekobaudat.de, masea-ensan.com waren per WebFetch gesperrt (DNS); Belege nur über die Suche. Alles "unklar" ist nicht belegt.

| Quelle | Inhalt | Umfang | Felder | Format | Lizenz / kommerziell | Eignung |
|---|---|---|---|---|---|---|
| ÖKOBAUDAT (BBSR) | Ökobilanz-Datensätze (EPD); teils Rohdichte, λ als Produkt-Zusatzangaben | tausende, Produkte/Hersteller-EPD | Rohdichte, λ teils; c, μ nicht systematisch | ILCD/EPD XML, CSV | Daten "kostenlos und uneingeschränkt" laut Seite; formale Lizenz (CC BY / ND?) nicht belegt: unklar. https://www.oekobaudat.de/anleitungen/datennutzer.html | gering (Herstellerdaten, kein c) |
| MASEA (Fraunhofer IBP) | Baustoffe Altbau, auch historische | n. b. | hygrothermische Kennwerte | Web-Datenbank, Einbindung in WUFI/Delphin | Nutzungsbedingungen nicht gefunden: unklar. https://www.baunetzwissen.de/daemmstoffe/tipps/linkliste/materialdaten-fuer-die-energetische-altbausanierung-152614 | hoch fachlich, rechtlich offen |
| TABULA/EPISCOPE (IWU) | Typgebäude, Bauteile, U-Werte je Baualtersklasse, Länder | Deutschland ca. 40 Typen | U-Werte, Flächen, g; Bauteilbeschreibung | Webtool, Excel | Lizenz nicht belegt; Dänemark-Bericht untersagt kommerzielle Nutzung (nur dieses PDF): unklar. https://www.iwu.de/research/gebaeudebestand/tabula-en/?L=1 | mittel |
| Stein/Loga 2025 Typgebäude-Modell (Zenodo 15488271, v1.1 16905741) | 6 Basis-Typgebäude (3 Baualtersklassen x 2 Größen), Tabellenwerte, Hüllflächenschätzung | 6 Typen, Excel | U-Werte, Flächen, Heizsysteme; Schichtaufbauten nicht belegt, vermutlich nur U-Werte | xlsx | CC BY 4.0 laut Auftrag (Record nicht abrufbar, nicht selbst geprüft). https://doi.org/10.5281/zenodo.16905741 | mittel (nur U-Vorgaben) |
| BMWi/BMI Bekanntmachung Datenaufnahme Wohngebäude (BAnz AT 04.12.2020 B1) | Pauschal-U-Werte alter Bauteile je Baualtersklasse | Tabellen | U-Werte, keine Schichten/Stoffwerte | PDF | Amtliche Bekanntmachung einer Rechtsvorschrift; amtliche Werke § 5 UrhG gemeinfrei (rechtlich bestätigen, ggf. nur der amtliche Text, eingebundene Normverweise nicht). https://www.bundesanzeiger.de/pub/publication/qzQUGd8A3unSCCbVMcf/content/qzQUGd8A3unSCCbVMcf/BAnz%20AT%2004.12.2020%20B1.pdf | mittel (U-Werte), gering (Aufbau) |
| EnergyPlus DataSets (Materials, Constructions, ASHRAE HOF) | IDF-Schnipsel, US-Bauweisen | einige Hundert | λ ρ c, Rauheit, Absorption | IDF | Software unter BSD-artiger Lizenz (https://github.com/NREL/EnergyPlus/blob/develop/LICENSE.txt); Lizenz der Datensätze nicht ausdrücklich, Werte aus ASHRAE Handbook: unklar | gering (US) |
| NREL BCL / OpenStudio Standards | Komponenten, Materialien, Konstruktionen | >30 000 Komponenten/Maßnahmen | λ ρ c, Aufbauten | XML/OSM | Data.gov: CC BY; ODI: CC0, widersprüchlich: unklar. https://catalog.data.gov/dataset/building-component-library | gering (US) |
| Hotmaps Building Stock (Eurac) | EU-Gebäudebestand je Land und Bauperiode | EU28 | U-Werte, Bauweise, Materialien (grob) | CSV, GitLab | CC BY 4.0 laut Fremdtabelle (nicht am Original belegt). https://eurac.edu/en/institutes-centers/institute-for-renewable-energy/tools-services/energy-database/hotmaps | mittel (grob, nur U) |
| EU Building Stock Observatory | Bestandskennzahlen | n. b. | U-Werte | Portal | Lizenz nicht gefunden: unklar | gering |
| baubook (AT) | Baustoffe, Produkte | groß | bauphysikalische Kennwerte | Web, XML | Nein: Datenbankschutz, Übernahme in eigene Datenbank verboten, nur Kooperationsvereinbarung. https://www.baubook.info/de/service/downloads/agb/baubook_agb_de.pdf/@@download/file/baubook_AGB_2022-12_de.pdf | nur per Vereinbarung |
| WUFI-Materialdatenbank (IBP) | Baustoffe | groß | alle hygrothermisch | in Software | Nein (Softwarelizenz/AGB): https://www.irbnet.de/daten/wufi/AGB_WUFI_en.pdf | nur Anwenderimport |
| ubakus, IDA ICE, PHI-Bauteilkatalog | - | - | - | - | Nicht belegt, nicht verwendbar angenommen | - |
| AAU Building Materials Database | Baustoffe Review | n. b. | ρ λ c | Web | Lizenz nicht ermittelbar: unklar. https://www.researchgate.net/publication/357355874_Thermal_Properties_of_Building_Materials_-_Review_and_Database | prüfen |

## Empfehlung
1. Ausgeliefert: Stein/Loga (CC BY 4.0) und die amtliche Bekanntmachung (§ 5 UrhG) nur als U-Wert-Vorgaben je Baualtersklasse. Eine freie Quelle für Stoffwerte (λ ρ c) mit Schichtaufbauten für deutsche Bauweisen ist nicht gefunden. Hotmaps (CC BY) als EU-Ergänzung nur nach Prüfung am Original.
2. Stoffwerte: nur nach Klärung (MASEA, BCL/CC0 als Notbehelf mit US-Bezug). Realistisch: eigene Kuratierung aus Messwerten/Literatur oder Katalogpflege durch den Anwender.
3. Nur Anwenderimport (Werte bleiben in der Anwenderdatenbank): WUFI, baubook, MASEA bis zur Klärung, Hersteller-/ÖKOBAUDAT-Daten, EnergyPlus-IDF.

## Offene Lizenzfragen
- ÖKOBAUDAT: formale Datenlizenz (CC BY? ND?), Nutzung technischer Zusatzangaben in Software (BBSR).
- MASEA: Nutzungsbedingungen, Einbettung in kommerzielle Software (Fraunhofer IBP).
- TABULA/EPISCOPE-Daten: Weiterverwendung (IWU).
- Stein/Loga: Lizenzvermerk am Record prüfen, ob Aufbauten enthalten sind; Namensnennung.
- Bekanntmachung: ob Tabellen eigenständig gemeinfrei sind; Norm-Zitate.
- BCL/EnergyPlus-DataSets: CC0 vs. CC BY; ASHRAE-Herkunft der Werte.
- Hotmaps/EU BSO: Lizenz am Original.
- baubook: Kooperationsvereinbarung für Softwarehersteller (Forschungs-/RPKW-Vereinbarung).

## Links
- https://www.oekobaudat.de/anleitungen/datennutzer.html
- https://www.baunetzwissen.de/daemmstoffe/tipps/linkliste/materialdaten-fuer-die-energetische-altbausanierung-152614
- https://www.iwu.de/research/gebaeudebestand/tabula-en/?L=1
- https://zenodo.org/records/16905675 , https://zenodo.org/records/15488315 , https://zenodo.org/records/15488643
- https://www.iwu.de/forschung/gebaeudebestand/repraesentative-typgebaeude/
- https://umwelt-online.de/recht/energie/envdat.htm
- https://bigladdersoftware.com/epx/docs/9-4/getting-started/data-sets.html
- https://bcl.nrel.gov/about , https://catalog.data.gov/dataset/building-component-library , https://certificates.theodi.org/en/datasets/37420
- https://www.baubook.info/en/baubook/services
- https://www.irbnet.de/daten/wufi/AGB_WUFI_en.pdf
