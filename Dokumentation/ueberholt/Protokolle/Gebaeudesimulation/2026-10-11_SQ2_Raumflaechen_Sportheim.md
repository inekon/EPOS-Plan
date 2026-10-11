# SQ2 — Raumflächen beim Import aus der Projektdatei (Fehlersuche)

Meldung: Beim Import allein aus der Projektdatei (`.sqproj`) „stimmt die Größe der Räume nicht“; gezeigt wurden
unter anderem EG-R1 47,312 m², EG-R4 102,26 m², EG-R6 35,67 m², EG-R7 55,83 m², EG-R13 114,696 m², Kopf beheizt
718,99 m² und Volumen 2.321,71 m³, dazu die Sperre „3 Räume sind keiner Zone zugeordnet (UG-R10, UG-R17, UG-R18)“.

## Befund

Je Raum (56 Räume) wurde die Fläche aus jeder Quelle der Datei und aus der IFC desselben Exportstands gegen die
Fläche gehalten, die EPOS übernimmt (`BmRoom.Area`, `SqprojLeser`, Raumzeile `FlaecheM2 = Area`):

| Quelle | Räume | Abweichung von `BmRoom.Area` | Räume über 0,1 % |
|---|---|---|---|
| DIN-277-Nettofläche (`BmBuildingSpace.NetAreaValue`) | 56 | −0,04 % … +6,98 % | 1 (EG-R45) |
| Raum-XML `room/@area` | 56 | −0,04 % … 0 % | 0 |
| Raumpolygone (Summe der Teilpolygone) | 56 | −0,04 % … 0 % | 0 |
| Bodenfläche des Raumkörpers | 56 | −0,04 % … +1,23 % | 1 (UG-R2) |
| Bodenhüllflächen (Level 3, Bezug 8, Boden/Bodenplatte, `GrossArea`) | 56 | 0 % … +70,4 % | 55 |
| IFC-Menge über die HottCAD-GUID | 56 | 0 % | 0 |

- Alle Raumflächen der Bildschirmfotos sind exakt `BmRoom.Area` = Raum-XML = Polygon = IFC. Die Kopfwerte stimmen mit
  der Datei überein (Volumen = `BmBuilding.HeatedNetVolumeOfBuilding`, beheizte Fläche = Summe der beheizten Räume);
  der IFC-Weg liefert dieselben Summen.
- Die einzige systematische Abweichung haben die Bodenhüllflächen: Sie sind nach Bauteilmaß (bis Wandachse bzw.
  Außenkante) gebildet und je Raum um 2 % … 70 % größer als die lichte Raumfläche — am stärksten bei kleinen Räumen
  (Flure, WC). Wer die Fläche des Bodenbauteils eines Raums neben die Raumfläche legt, sieht diesen Unterschied; er ist
  normgemäß (Hüllflächen nach Außenmaß) und kein Lesefehler.
- Maßstab, Einheit (Meter), Transformation (`transform_origin`, `insertation_point` überall 0) und Geschosshöhen sind
  unauffällig; vier Räume tragen zwei Teilpolygone, beide werden gelesen.

## Ergebnis

Kein Fehler in der Flächenermittlung nachweisbar: EPOS übernimmt die Fläche, die das CAD-Programm als Raumfläche
führt (lichte Netto-Raumfläche). Keine Codeänderung; der Rechenweg ist nicht berührt, kein Referenzlauf nötig.
Zur Klärung beim Anwender: Gegen welchen Wert wurde verglichen (CAD-Raumstempel, Bauteilliste, Bruttofläche)?

## Die drei nicht zugeordneten UG-Räume

UG-R10, UG-R17 und UG-R18 sind die einzigen Räume, deren Zonen in der Datei sich widersprechen: unbeheizt
(`HeatingType` 2), in der Nutzungszone (Typ 5) „unbeheizt“, in der Simulationszone (Typ 6) aber in einer beheizten
Nutzungszone. Sie haben also eine DIN-Zone; der Befund GD („ohne DIN-Zone“) trifft so nicht zu, die Ursache liegt im
Widerspruch beheizt/Zone. Der Zonenvorschlag des Kerns ordnet unter X1 bis X4 ohne Haken alle 56 Räume zu; die
Sperre entsteht im Dialogweg (Haken bzw. gespeicherter Plan). Übergabe an GD-3.

## Diagnose

`EPOS.Kern.Tests/SqprojQuelldateienDiagnoseTests.Raumflaechen_je_Quelle` gibt die Vergleichstabelle je Raum (nur
Raumkennungen und Prozent), die Spannen und die nicht zugeordneten Räume je Zonenregel aus; ohne Anwenderdatei unter
`Quellen/` endet sie ohne Prüfung.
