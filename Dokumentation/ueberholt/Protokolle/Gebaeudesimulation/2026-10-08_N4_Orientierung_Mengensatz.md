# Protokoll N4 — Orientierung bei Mengensatz, Hanglage im Einzonenweg (08.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#823**. Commits: `ca951e7c4`, `a5d2328aa`, `8e69a3945` (Zweig `n4-orientierung` auf `35e0038f2`).
**Anlass:** Befunde der Abnahme G5 am Rechenweg ([Protokoll G5-A](2026-10-08_G5-A_Abnahme_Rechenweg.md)).

## 1 Anlass

Befund 4.1: Bei Dateien mit Mengensätzen und Raumgrenzen ohne Orientierung war A2 nicht erfüllt (Bauteile ohne Azimut, Dachneigung 0°, B3 griff nicht, der Zonenvorschlag lehnte ab). Dazu der offene Punkt aus #822: Hanglage im Einzonenweg nicht geteilt. Kein Schemaschritt, Basis unverändert.

## 2 Gebaut

- Ursache 4.1: `IfcAbbildBauer.Koerperflaechen` lief bei Mengensatz nur in den Flächenvergleich und übersprang die Orientierung; die Normale der Raumgrenzen (`IfcCurveBoundedPlane`) wurde nicht genutzt; die Dachneigung blieb bei der Vorgabe 0°.
- Neue Methode `OrientierungBeiMengensatz`: Die Fläche bleibt die des Mengensatzes. Wo die Datei keine Orientierung nennt, gilt zuerst die flächengewichtete Außennormale der Raumgrenzen (Streuung höchstens 10°), sonst der Bauteilkörper; für Außenwände und das Dach (damit greift B3). Fenster übernehmen die Richtung ihrer Wand. Info `IMP_IFC_PROT_ORIENTIERUNG_ERGAENZT` je Datei.
- Hanglage im Einzonenweg: `Teilflaechen.GliedernAmGelaende` mit `Teilflaeche.Rand` teilt wie im Zonenweg Posten am Erdreich und an der Außenluft; Öffnungen gehen an den Teil ihrer Lage; Raumgrenzen der Datei werden nicht geteilt.

## 3 Prüfung

- `ifc4_g5_kleinhaus_mit_mengen.ifc`: 13 Bauteile aus den Raumgrenzen ergänzt, der Zonenvorschlag läuft durch, alle 19 Zeilen gleich dem Körperweg; Heizwärme 10,090 MWh/a und Heizlast 6,873 kW, gleich dem Körperweg (Abweichung 0 %).
- Hinweis 4.2: Die Abweichung 17,5 % an der Ostwand von `ifc4_g5_mengen_gegenprobe.ifc` ist echt, kein Fehler. Die Wand ist im Grundriss ein L mit einem 2 m langen Flügel nach außen, hinter dem kein Raum liegt; der Körper zählt (7,7 + 2,0) · 2,5 = 24,25 m², raumseitig liegen 20 m² an.
- Tests: `OrientierungMengensatzTests` (3), `HanglageTests` +2.
- Geänderte Erwartung: `IfcKoerperTests.Rundlauf_ueber_den_IfcLeser_gleich_wie_ohne_Koerper` — statt `SEITE_UNBESTIMMT` (6 Wände) die Info `ORIENTIERUNG_ERGAENZT` (8 Bauteile aus dem Körper); übrige Meldungen und Bauteilwerte gleich.
- Unverändert: `G5AbnahmeRechenwegTests` 7/7 grün (ihre Orientierungsergänzung wirkt nur noch an 2 Stellen ohne Einfluss auf das Ergebnis), die Sollwerte der lokalen Anwenderdateien, die Referenzprojekte.

## 4 Gate 823

GATEZAHLEN

## 5 Offen

- Nachabnahme A2 und B3 an `ifc4_g5_kleinhaus_mit_mengen.ifc` durch die Sitzung Gebäudesimulation; dabei die Orientierungsergänzung in `G5AbnahmeRechenwegTests` herausnehmen.
- Fehlt der Wand die Orientierung, nehmen Fenster noch nicht ersatzweise die ihres eigenen Körpers.
