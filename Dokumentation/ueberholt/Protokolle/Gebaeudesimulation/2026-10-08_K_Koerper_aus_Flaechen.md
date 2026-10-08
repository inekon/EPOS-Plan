# Protokoll Welle K — Gebäudeviewer für Projektdatei und gbXML wie für IFC, Körper aus Flächen (08.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#816**. Commits: Konzept `8c55441c1`; K1 `074b87ef1`/`acd41b25d`; K2 `af10909ff` bis `ff567aace`; K3 `87c31c08c`/`9885682c3`; K4 `f6ab3b289`/`fd9decea9`. Integration `integration-g5pk` `796afb72c`, im Hauptbaum `08ea6386e`.
**Entscheid:** Anwenderwunsch „gleicher Viewer mit gleichem Umfang“; Konzept Datenaustausch Kapitel 17.

## 1 Anlass

Projektdatei und gbXML tragen keine Körper wie IFC. Der Viewer soll dennoch alle Farbmodi und die Auswahl bieten; die Körper werden dazu aus den Flächen gebildet. Kein Schemaschritt, kein Rechenweg.

## 2 Gebaut

### Körperbildner (`Polygonnetz.cs`, `Koerperbildner.cs`)

- Ohrenschnitt formatfrei; IFC-Körper bitgleich (SHA-256 über 11 Proben).
- Raumprisma mit Höhen je Punkt; Hülle mit T-Teilung und Ausrichtung über Kantenpaare; Extrusion mit Loch und Laibung.
- `Dateikoerper.Quelle` (`Datei`/`AusFlaechen`) und Quellfläche je Dreieck.

### Projektdatei (`SqprojGeometrie.cs`)

- Raumkörper aus dem Raum-XML; `GeoDesc` mit Schleifenwahl; Bauteilkörper mit gemessener Bezugsebene.
- Dicke: gezeichnete Dicke, wenn nur sie passt, sonst Schichtsumme, sonst Vorgabe. Grundriss `Boden`/`Raumpolygon`.
- Anwenderdatei: Raumkörper 100 % geschlossen; Raumvolumen gegen IFC im Median 0,00 % und in Summe +1,9 %; Bauteilkörper 99,3 %; `Bezugsebene_angenommen` 5,9 %.

### gbXML (`GbxmlKoerper.cs`)

- Raumkörper aus `ClosedShell`, sonst aus den Flächen je Raum; Schichtdicken aus der Konstruktion.
- Rechenzeit bei 100 Räumen höchstens rund 50 ms. Proben `gbxml_g5_closedshell.xml` und `gbxml_g5_flaechen.xml`.

### Anbindung

- Flächenklassifikation formatfrei mit erster Regel `QUELLFLAECHE`.
- Sperren: Für aus Flächen gebildete Körper gibt es keinen Körpervergleich, keine Flächenherkunft `Koerper`, keine Körperpaare und keine Bauteilflächen je Raum aus Körpern.
- `Koerperherkunft.Abgeleitet` mit Kennzeichen „aus Flächen gebildet“; Steckbriefzeile „Körper“; der Klick auf ein Dreieck öffnet das Bauteil seiner Quellfläche.
- Abgeleitete Körper gehen nicht in den Export.

## 3 Prüfung

Bitgleichheit der IFC-Körper (11 Proben), Zahlen der Anwenderdatei siehe oben; die Klassifikation hängt bei gbXML und Projektdatei Info-Meldungen mit IFC-Präfix an (`IMP_IFC_PROT_FLAECHE_OHNE_BAUTEIL`).

## 4 Gate 814

GATEZAHLEN

## 5 Offen

- **Sichtabnahme unter Windows:** Farbmodi in 3D mit echter Projektdatei und gbXML; Wortlaut von Kennzeichen und Zeile „Körper“; Rückfall auf das Prisma.
- IFC-Präfix der Info-Meldungen bei gbXML und Projektdatei.
- Logbuch-Eintrag (Version beim Anwender erfragen) und Wiki-Upload gebündelt.
