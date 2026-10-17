# IV — Gebäudeimport aus der Projektdatei: drei technische Verfeinerungen der Geometrie

Sitzung „IFC / Gebäudeimport“, 17.10.2026, Zweig `ifc6` (von `ios_migration_september` `26c01427c`). Auftrag: die drei
Punkte „Technisch, ohne Entscheid“ aus Abschnitt 4 des Übergabepapiers
[„IFC / Gebäudeimport“](../../2026-10-08_Uebergabe_IFC_Gebaeudeimport.md), vom Anwender am 17.10.2026 beauftragt. Kein
Schemaschritt, keine SQL-Anweisung, keine Ressource, kein Eingriff in den Rechenweg; Hülle und Ansicht unverändert.

## Messung

Die Diagnose steht als Test `EPOS.Kern.Tests/SqprojVerfeinerungDiagnoseTests` gegen die Anwenderdatei
`Quellen/Sportheim_1970_unsaniert.sqproj` (unversioniert; fehlt sie, schweigt der Fall). Sie gibt nur Anteile und Zählungen
aus und hält die Endwerte als Schranken (Ersatzkennung höchstens 10 %, angenommene Bezugsebene höchstens 3 %, alle
Raumkörper gebildet).

| Größe | vorher | nachher |
|---|---|---|
| Raumkörper gebildet | 56 von 56 | 56 von 56 |
| Raumdreiecke mit Ersatzkennung (Anteil der Dreiecke) | 464 von 1140 (40,7 %) | 120 von 1820 (6,6 %) |
| dieselben nach Fläche | 43,6 % | 5,9 % |
| davon Decke / Mantel / Kante | 210 / 252 / 2 | 0 / 116 / 4 |
| Raumpolygone mit mehr als einer Quellfläche am Boden | 15 von 60 (3 mit zwei Randbedingungen) | jetzt getrennt angekommen |
| Raumpolygone mit mehr als einer Quellfläche an der Decke | 20 von 60 (7 mit zwei Arten und Randbedingungen) | jetzt getrennt angekommen |
| Bauteilkörper mit angenommener Bezugsebene | 26 von 438 (5,9 %): 16 Außenwände, 10 Decken | 7 von 438 (1,6 %): 7 Außenwände |

Die Mehrdreiecke nachher stammen aus der Teilung von Boden und Decke; die Raumvolumen bleiben.

## Ursachen und Lösung

**1. Quellflächen von Boden und Decke.** Die Projektdatei ist so gebaut: 15 Böden und 20 Decken der 60 Raumpolygone liegen
über bzw. unter mehreren eigenen Flächen. `SqprojGeometrie.Deckel` wählte nur die mit der größten Überdeckung. Jetzt liefert
sie alle eigenen, nicht senkrechten Flächen mit mindestens einem Stichpunkt des Polygons in ihrem Grundriss, die tragende
vorn. Der Körperbildner (`Raumpolygon.Bodenteile`/`Deckenteile`, `Grundrissteil`) zerlegt die Deckeldreiecke an den Kanten
der Ringe in konvexe Zellen; jede Zelle trägt die Kennung des ersten Rings, der ihren Schwerpunkt enthält, sonst die der
tragenden Fläche, Höhen baryzentrisch im Ausgangsdreieck. Schließt ein Polygon so nicht (zwei Polygone, Kanten unter der
Toleranz), gilt es ohne Teilung.

**2. Ersatzkennung.** Je Ersatzdreieck die nächste eigene, parallele Bauteilfläche gesucht: Bei 248 der 464 lag die
Bezugsebene genau um die gezeichnete Dicke (`Thickness`) vor dem Raum, die Zuordnung suchte aber nur bis zur Aufbaudicke
plus 2 cm; bei 66 Deckendreiecken lag die Decke bis zu 30 cm hinter der Dicke (Hohlraum). Jetzt suchen Mantel, Boden und
Decke bis zur größeren der beiden Dicken plus Suchweite (30 cm); Flächen bis zur Dicke plus 2 cm gehen vor. Schließt ein
Raumkörper damit nicht, gilt erst der enge Mantel, dann die enge Zuordnung wie zuvor (an der Datei: ein Raum mit engem
Mantel, zwei Polygone ohne Teilung).

**3. Angenommene Bezugsebene.** Bei 21 der 26 traf kein Stichpunkt der Fläche den Raumkörper (der Raum überdeckt die Fläche
nur teilweise oder gar nicht), bei 5 lag der Raum knapp hinter der gezeichneten Dicke. `Messen` nimmt jetzt, wenn kein
Stichpunkt trifft, die nächste parallele Raumfläche, die sich in der Ebene mit der Fläche überlappt (Wände), bzw. die nächste
parallele Ebene des Raums (Decken, Böden, Dächer: Raumhöhe); liegt der Raum hinter der Dicke, aber innerhalb der Suchweite,
reicht der Körper zum Raum hin (Spalt, gezeichnete Dicke). Gesucht wird bis zur größeren Dicke plus Suchweite.

## Tests

- `KoerperbildnerTests`: L-Grundriss mit geteiltem Boden und geteilter Decke (Flächen je Quelle, geschlossen, nach außen),
  geneigte Decke mit Teilfläche in derselben Ebene.
- `SqprojGeometrieTests`: Boden mit zwei Quellflächen; Wand und Decke mit Bezugsebene hinter der Aufbaudicke; Bezugsebene
  aus Spalt, Überlappung und Raumhöhe. Gegenprobe: die drei neuen Fälle sind auf dem Ausgangsstand rot.
- `SqprojVerfeinerungDiagnoseTests`: Diagnose und Schranken an der Anwenderdatei.
- Gezielt grün: Sqproj-, Körper-, Gbxml-, IFC-, Flächenklassifikations- und Ansichtsklassen in `EPOS.Kern.Tests` (897,
  6 Erzeugerfälle übersprungen), `GebaeudeAnsicht*`/`GebaeudeImport*` in `EPOS.UI.Tests` (189). Kern-Filter baut ohne Fehler.
- Referenzlauf 1030, 1051, 1073 gegen `2026-10-17_R58_Kaeltesummen`: PASS. Die Körper der Projektdatei speisen nur Ansicht
  und Flächenklassifikation (Randgruppen, Befund), nicht Flächen oder U-Werte des Imports.

## Grenzen

- 116 Manteldreiecke bleiben mit Ersatzkennung: 52 ohne eigene Fläche im Umkreis von 1 m, 40 mit nur schräg stehenden
  Flächen (Höhensprünge, Bodenplatten, Decken), 13 mit einer parallelen Fläche weiter als Dicke plus 30 cm, 11 im Raum mit
  engem Mantel (dort schloss der Körper nur mit der Zuordnung bis Dicke plus 2 cm); dazu 4 Dreiecke mit der Kantenkennung
  des Körperbildners.
- 7 Außenwände behalten die angenommene Bezugsebene: Keine Raumfläche ihres Raums überlappt sie (Wandteile oberhalb oder
  neben dem Raum, dem die Datei sie zuordnet).
- Die Teilung von Boden und Decke greift nur, wenn das Polygon mit ihr schließt; sonst trägt es eine Quellfläche je Boden und
  Decke. Sichtbare Texte, Ressourcen und die Wiki-Seite „Gebäudeimport“ sind nicht betroffen (der Klick öffnet häufiger den
  Steckbrief, die Bedienung bleibt).
