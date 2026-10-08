# Abstimmung G5 — Auftrag an die Sitzung IFC / Gebäudeimport, Anforderungen der Sitzung Gebäudesimulation

Stufe **G5** (Geometrieableitung aus IFC-Körpern) ist nach **E101** (Anwender, 07.10.2026) nicht mehr „nur bei Bedarf“. Der
Geometrieteil gehört in den Auftrag der **Sitzung IFC / Gebäudeimport**: Sie baut G5, weil die Dateien des Imports, der
Körper-Leser und die Bauteilstufen bei ihr liegen. Die **Sitzung Gebäudesimulation** baut an G5 nichts; sie nennt hier, was der
Rechenweg von G5 braucht, und nimmt das Ergebnis am Rechenweg ab. Damit überschneiden sich die Wellen beider Sitzungen nicht. Die
Sitzung IFC trägt ihre Antwort in die Spalte „IFC-Sitzung“ ein und pusht sie.

## 1 Was G5 umfasst

Gebaut sind bereits der Raumkörper-Leser ohne Geometriekern (G7f, #727: Extrusion, Tessellation, BRep, `IfcMappedItem`,
Placement-Kette), die Bauteilkörper als Anzeige (HC-1) und der gespeicherte Grundriss je Raum aus dem Dateikörper (HC-5, #784).
Gerechnet wird mit Körpern heute nur bei den Trennflächen zwischen Räumen einer Datei ohne Raumgrenzen. Flächen, Dicke und
Schichten der Bauteile kommen aus Mengensätzen, Raumgrenzen und Katalog ([ADR-003](../ADR-003_IFC_xBIM_ohne_Geometriekernel.md),
[Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 15.1).

G5 ergänzt drei Teile:

| Teil | Inhalt |
|---|---|
| G5-1 | **Bauteilkörper als Rechengröße:** Fläche und Orientierung von `IfcWall`, `IfcSlab`, `IfcRoof` (und `IfcCovering`, soweit Hülle) aus Extrusion, BRep und `IfcMappedItem` samt Placement-Kette — als Quelle, wenn die Datei für das Bauteil keine Mengensätze oder Raumgrenzen liefert |
| G5-2 | **Abzug der Öffnungen:** Fläche der Fenster und Türen aus `IfcOpeningElement`, `IfcWindow`, `IfcDoor` und ihr Abzug von der Wand- bzw. Dachfläche, wo die Mengensätze die Nettofläche nicht nennen |
| G5-3 | **Rückfall für Dateien ohne Mengensätze und Raumgrenzen:** Bauteilflächen je Raum und Zone aus den Körpern statt des Rückfalls „schematisch“; Herkunft je Fläche sichtbar (Mengensatz / Raumgrenze / Körper / schematisch) |

Nicht Teil von G5: Reparatur offener oder überlappender Netze, Dachschrägen im Export, Anlagentechnik, der gbXML-Weg
(`Space/ShellGeometry`, eigener Zuruf).

## 2 Anforderungen des Rechenwegs (Sitzung Gebäudesimulation)

Der Rechenweg nach VDI 6007 (Bauteilweg, Zonenmodell, Erdreich nach DIN EN ISO 13370) liest je Zone Bauteile aus `Tab_Bauteil`.
Was G5 dort ablegt, muss diese Größen tragen:

| Nr. | Größe | Anforderung |
|---|---|---|
| A1 | Fläche | Nettofläche des Bauteils je Zone in m² (opake Fläche ohne Öffnungen); Öffnungen als eigene Bauteile (Fenster/Tür) mit ihrer Fläche, wie es der Bauteilweg heute aus Mengensätzen erhält |
| A2 | Orientierung | Azimut (0° = Nord, im Uhrzeigersinn, nach `TrueNorth` bzw. `IfcMapConversion`) und Neigung (0° = waagrecht nach oben, 90° = senkrecht) je Bauteilfläche; bei gegliederten Wänden je ebener Teilfläche oder flächengewichtet zusammengefasst, benannt |
| A3 | Randbedingung | Außenluft, Erdreich, Nachbarzone (mit Trennflächenzuordnung), unbeheizt — wie der heutige Importweg (Raumgrenzen, R0–R7) |
| A4 | Herkunft | je Fläche sichtbar und gespeichert, damit Prüfregeln und Bericht sie nennen können |
| A5 | Abweichung | wo Mengensätze vorliegen, bleiben sie Quelle; der Körperwert wird nur verglichen und bei Abweichung über einer Schwelle gemeldet (Vorschlag: 2 %, wie die Gegenprobe der Trennflächen) |
| A6 | Abnahme am Rechenweg | Referenzlauf aller 21 Projekte gegen die geltende Basis byte-gleich (G5 berührt keine Referenzprojekte); an den Importproben mit Mengensätzen Körperfläche gegen Mengensatz innerhalb A5; an einer Probe ohne Mengensätze Heizwärme gegenüber dem Rückfall „schematisch“ benannt |

## 3 Dateien und Zuständigkeit

| Bereich | Dateien (Stand 07.10.2026) | Gebäudesimulation | IFC-Sitzung |
|---|---|---|---|
| Körper-Leser und IFC-Abbild | `EPOS.Kern/Allgemein/Import/Ifc/` (`IfcRaumkoerper.cs`, `IfcRaumgrundriss.cs`, `IfcGrenzgeometrie.cs`, `IfcPlatzierung.cs`, `IfcAbbildBauer.cs`, `IfcGebaeudeAbbild.cs`), `EPOS.Kern/Allgemein/Simulation/Gebaeude/Dateikoerper.cs` | keine Änderung | **ändert** (G5-1, G5-2): neu `IfcBauteilkoerper.cs` und `IfcOeffnungen.cs`; `IfcRaumkoerper.cs`, `IfcPlatzierung.cs`, `IfcAbbildBauer.cs`, `IfcGebaeudeAbbild.cs` erweitert; `Dateikoerper.cs` nur, falls die Bauteilkörper es brauchen |
| Importablauf, Bauteilvorschlag, Aufbauten | `EPOS.Kern/Allgemein/Import/Gebaeude/` (`GebaeudeImportAblauf.cs`, `GebaeudeBauteilvorschlag.cs`, `Bauteilzuordnung.cs`, `Ersatzaufbau.cs`, `GebaeudeZuordnungsModell.cs`, `GebaeudeNeulesen.cs`), `EPOS.Kern/Allgemein/Import/Sqproj/` | keine Änderung | **ändert** (G5-3): `GebaeudeImportAblauf.cs`, `GebaeudeBauteilvorschlag.cs`, `GebaeudeZuordnungsModell.cs`, `Koerpernachbarschaft.cs`, neu `Koerperflaechen.cs`; `Bauteilzuordnung.cs`, `Ersatzaufbau.cs`, `GebaeudeNeulesen.cs` und `Sqproj/` bleiben unverändert |
| Rechenweg | `EPOS.Kern/Allgemein/Simulation/Gebaeude/` außer `Dateikoerper.cs` (Bauteilweg, Zonenmodell, Erdreich) | ändert nur bei Bedarf aus A1–A4, nach Absprache | keine Änderung; unsere Dateien dort (`Koerpergrundriss.cs`, `Zonenkoerper.cs`, `Zonengeometrie*.cs` aus G7f/HC-5) nur, wenn G5-3 sie braucht, dann mit Anmeldung hier |
| Export | `EPOS.Kern/Allgemein/Export/Ifc/*`, Exportmodell | keine Änderung | keine Änderung (die Herkunft der Fläche geht nicht in den Export) |
| Oberfläche | Gebäudeansicht, Zuordnungsdialog, Bauteilsteckbrief | keine Änderung | **ändert** (G5-3): Herkunft je Fläche im Zuordnungsdialog und im Bauteilsteckbrief (`EPOS.UI/Bausteine/Bauteilsteckbrief.razor`, `EPOS.UI.Daten/Bedarf/GebaeudeAufbauHuelle.cs`, Gebäudeimport-Hülle), Ressourcen in beiden Sprachen |
| Schema | Schritte nach Anmeldung in der Kopfzeile der Statusdatei | — | ein Schritt `FlaechenherkunftSchema` (`Tab_Bauteil.Flaechenherkunft`), Anmeldung in der Kopfzeile vor G5-0 |
| Testdatenbank, Importproben | `Referenzlaeufe/Kenndaten_Test.sqlite`, `Referenzlaeufe/Importproben/` | Referenzprojekte bleiben unberührt | Testdatenbank nur über den Schemaschritt hochgezogen (`Werkzeuge/Testdatenbankschema`), Referenzprojekte unberührt; neue Proben unter `Referenzlaeufe/Importproben/` mit Zeile in `LIESMICH_Importproben.md` |

## 4 Fragen an die Sitzung IFC

| Nr. | Frage | IFC-Sitzung |
|---|---|---|
| F1 | Übernehmt ihr G5 in euren Auftrag, und wann (nach welchen laufenden Wellen — offen sind laut Statusdatei u. a. Konzept 5.3 des HottCAD-Verbunds, Codes für Putz, Folie und Luftschicht, Richtung bei Wänden gegen Erdreich)? | **Ja.** Start nach dem Push von #795 (zehn Windows-Tests, Gate läuft); die Wiki- und Logbuchpflege BA-5 läuft nebenher. Die offenen Punkte aus BA-4b (Codes für Putz, Folie und Luftschicht, `GUID` in allen HottCAD-Exporten) und Konzept 5.3 des HottCAD-Verbunds warten auf Anwenderdaten und folgen nach G5; die Richtung bei Wänden gegen Erdreich nehmen wir in G5-3 mit, weil A3 sie braucht. |
| F2 | Wellenzuschnitt und Aufwand für G5-1 bis G5-3; braucht G5 einen Schemaschritt (Herkunft je Fläche, A4)? | Vier Aufträge in zwei Wellen. **G5-0:** Schemaschritt `Tab_Bauteil.Flaechenherkunft` (TEXT, `CHECK IN ('MENGENSATZ','RAUMGRENZE','KOERPER','SCHEMATISCH')`, NULL = Bestand) und Proben. **G5-1:** Bauteilkörper über den G7f-Leser (Extrusion, BRep, `IfcMappedItem`, Placement-Kette) → Fläche und Orientierung je ebener Teilfläche, Gegenprobe gegen den Mengensatz (A5). **G5-2:** Öffnungen über `IfcRelVoidsElement`/`IfcRelFillsElement` (Fläche aus dem Körper, sonst `OverallWidth × OverallHeight`), Abzug am Wirt, Fenster und Tür als eigene Bauteile mit der Orientierung des Wirts. **G5-3:** Flächen je Raum und Zone aus Raumkörper ↔ Bauteilkörper (Muster `Koerpernachbarschaft`, G7f-4), Randbedingung, Herkunft in Dialog und Steckbrief. Aufwand: je Auftrag ein Opus-Agent, G5-3 zwei (Kern, Oberfläche); Welle 1 = G5-0 bis G5-2, Welle 2 = G5-3, je ein Gate. **Schemaschritt ja:** die vorhandene Spalte `Tab_Bauteil.Herkunft` nennt die Quelle (IFC, Katalog, Vorgabe …), nicht den Weg der Fläche; Anmeldung vor G5-0 (derzeit ist 196 frei). |
| F3 | Sind die Anforderungen A1–A6 erfüllbar, oder braucht es Änderungen am Rechenweg (dann Absprache nach Abschnitt 3)? | **Erfüllbar ohne Änderung am Rechenweg.** A1: Nettofläche in `Tab_Bauteil.Flaeche`, Öffnungen als eigene Zeilen wie heute. A2: `Tab_Bauteil.Azimut` führt schon 0° = Nord im Uhrzeigersinn, `TrueNorth` und `IfcMapConversion` wie im Raumweg (Probe `ifc4_mapconversion.ifc`); gegliederte Wände je ebener Teilfläche eine Zeile, Teilflächen unter 5° Richtungsunterschied flächengewichtet zusammengefasst und im Protokoll benannt. A3: Randbedingung und Trennflächenzuordnung wie R0–R7. A4: neue Spalte (F2); der Rechenweg liest sie nicht, Prüfregeln und Bericht können sie lesen. A5: Mengensatz bleibt Quelle, Abweichung über 2 % als Meldung wie die Gegenprobe der Trennflächen. A6: Referenzlauf 21/21 byte-gleich, weil kein Referenzprojekt importiert wird; Heizwärme der Probe ohne Mengensätze gegen den Rückfall „schematisch“ im Protokoll. Ergibt sich doch eine Frage an den Rechenweg, melden wir sie hier an. |
| F4 | Welche Importproben ohne Mengensätze oder Raumgrenzen gibt es oder werden beschafft? | Vorhanden (selbst erzeugt): `ifc4_ohne_mengen.ifc` (Raum und Außenwände ohne Mengensatz), `ifc2x3_referenzen.ifc` (ohne Raumgrenzen, mit Raumbezügen), `ifc4_koerper_nachbarn.ifc` (Raumkörper ohne Raumgrenzen), `ifc4_koerper_bauteile.ifc` (Bauteilkörper der Anzeige), `ifc4_rueckfaelle.ifc` (Platten ohne Mengen). Neu mit G5 (selbst erzeugt, eigenes Werk): Wand als Extrusion, BRep und `IfcMappedItem` hinter einer Placement-Kette ohne Mengensatz; Wand mit `IfcOpeningElement`, Fenster und Tür; Kleinhaus ohne Mengensätze und Raumgrenzen (zwei Geschosse, unbeheizter Keller, geneigtes Dach) mit Gegenprobe desselben Hauses mit Mengensätzen. Echte CAD-Exporte ohne Mengensätze erbitten wir beim Anwender, nur zur lokalen Diagnose, nie im Repositorium. |

## 5 Regeln

- Die Sitzung Gebäudesimulation ändert nichts unter `EPOS.Kern/Allgemein/Import/Ifc/` und `EPOS.Kern/Allgemein/Import/Gebaeude/`.
- Braucht G5 eine Änderung am Rechenweg, meldet die Sitzung IFC sie hier an; die Sitzung Gebäudesimulation baut sie oder gibt sie frei.
- Beide Sitzungen melden Schemaschritte in der Kopfzeile der Statusdatei an und prüfen Status- und Entscheidnummern vor jedem Push
  gegen origin.

## 6 Stand

| Teil | Stand |
|---|---|
| G5-1 Bauteilkörper | gebaut (#801) |
| G5-2 Öffnungen | gebaut (#802) |
| Abnahme am Rechenweg (Gebäudesimulation) | G5-1 und G5-2: Referenzlauf 22/22 gegen R40 byte-gleich nach dem Merge (#804); G5-3: **abgenommen mit Befunden** — Referenzlauf 24/24 gegen R43, A5 an sechs Proben ohne stille Abweichung, Heizwärme der Kleinhausprobe auf dem Körperweg gleich dem Mengensatzweg (Rückfall ohne Raumzuordnung +30 %); offen Befund 4.1 — A2 an Dateien mit Mengensätzen und Raumgrenzen ohne Orientierung nicht erfüllt (11 Bauteile ohne Azimut, Dachneigung 0° statt aus dem Körper, B3 greift nicht; der Zonenvorschlag lehnt ab), Nachabnahme nach der Behebung ([Protokoll G5-A](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-08_G5-A_Abnahme_Rechenweg.md)) |
| G5-0 Schemaschritt 197 (`Tab_Bauteil.Flaechenherkunft`) | gebaut (#805, Schritt 197) |
| G5-Nachbesserung (Körpervergleich) | gebaut (#808) |
| G5-N Nordrichtung (Schritt 199) | gebaut (#813) |
| G5-3 mit Farbmodus „Befund“, G5-3d | gebaut (#814) |

A1 bis A5 halten aus Sicht von G5-1 und G5-2; der Rechenweg ist unverändert, der Referenzlauf der CI-Projekte gegen R40 ohne Abweichung.

## 7 Nordrichtung abfragen und nachträglich ändern (G5-N)

Anwenderauftrag vom 07.10.2026: Nennt die Datei keine Nordrichtung, fragt der Import sie ab; ohne Eingabe gilt eine Annahme
mit Bemerkung; die Ausrichtung lässt sich nachträglich ändern. Befund an drei HottCAD-Dateien: keine trägt `TrueNorth`, IFC2X3
kennt keine `IfcMapConversion`, und die Projektdatei (`BmElement.Orientation`) liegt im selben ungedrehten System wie die IFC
(19 von 22 vergleichbaren Wänden Differenz 0°) — ein Nordwinkel lässt sich aus ihr nicht ableiten, er muss vom Anwender kommen.

| Nr. | Regel |
|---|---|
| N1 | **Eingabe:** Wohin zeigt die Planoberseite (+y der Datei)? Winkel in Grad, 0° = Nord, im Uhrzeigersinn, mit Schnellwahl N, NO, O, SO, S, SW, W, NW und einer Nordpfeil-Vorschau am Grundriss. Intern gilt die vorhandene Beziehung wahrer Azimut = Modellazimut − Nordwinkel (`Tab_Importquelle.Nordwinkel_Grad`, Schritt 191); die Eingabe „Planoberseite zeigt nach α“ entspricht Nordwinkel = (360° − α) mod 360°. |
| N2 | **Wann gefragt wird:** Im Zuordnungsdialog, wenn die Datei keine Nordrichtung nennt (IFC: `TrueNorth`/`IfcMapConversion`, gbXML: `CADModelAzimuth`). Nennt die Datei eine, steht sie als Vorgabe im Feld und lässt sich überschreiben. |
| N3 | **Ohne Eingabe:** Annahme Planoberseite = Nord (Nordwinkel 0°); die Meldung sagt, dass es eine Annahme ist und wo sie sich ändern lässt. |
| N4 | **Wirkung:** Azimut aller Bauteile mit Azimut (Wände, Fenster, Türen, geneigte Dächer), die Raumgrundrisse und die Gebäudeansicht folgen dem Nordwinkel; Neigungen bleiben. |
| N5 | **Nachträglich:** Im Gebäudedialog steht bei Gebäuden mit Importquelle das Feld „Ausrichtung“ mit derselben Eingabe. Eine Änderung dreht nach Rückfrage alle Bauteile des Gebäudes um den Unterschied (auch von Hand angelegte — das Gebäude dreht sich als Ganzes) und speichert den neuen Winkel an der Quelle; „Datei erneut lesen“ übernimmt den gespeicherten Winkel. |
| N6 | **Herkunft:** neue Spalte `Tab_Importquelle.Nordwinkel_Herkunft` (`DATEI`, `EINGABE`, `ANNAHME`), Schemaschritt 199; Gebäudedialog und Bericht nennen sie. |
| N7 | **Rechenweg:** unverändert; er liest den Azimut aus `Tab_Bauteil`. Eine Drehung ändert die Ergebnisse (Solargewinne, Fensterflächen je Himmelsrichtung) — gewollt. Keine Einfrierregel berührt: kein Referenzprojekt hat eine Importquelle. |

## 8 G5-3 mit Farbmodus „Befund“ (Zuschnitt)

**Stand: gebaut (#814).**

Anwenderauftrag vom 07.10.2026: Die Gebäudeansicht zeigt Bauteile, die für die Rechnung unvollständig oder fehlerhaft sind;
Bauteile mit Nettofläche 0 entstehen gar nicht.

| Nr. | Regel |
|---|---|
| B1 | **Farbmodus „Befund“** als vierter Knopf neben Zonen, Randbedingung und Aufbau: **rot** — Bauteil ohne U-Wert oder ohne die Eigenschaften, die die Rechnung braucht (kein U aus Datei, Projektdatei, Katalog oder Ersatzaufbau; Fläche fehlt); **orange** — Bauteil mit unlesbarem Körper (Darstellungsart nicht lesbar, offene oder entartete Schale, nur teilweise gelesen; die Fläche kommt dann aus dem Mengensatz oder fehlt); **grau** — ohne Befund. Legende mit Anzahl und Fläche je Stufe, Schalter je Stufe, Klick öffnet den Steckbrief mit der Meldung des Bauteils. |
| B2 | **Nettofläche 0:** Bleibt nach dem Abzug der Öffnungen keine Fläche, legt der Import das Bauteil nicht an; das Protokoll nennt die entfallenen Bauteile in einer Zeile (Anzahl, Namen). Die Warnung „Nettofläche null“ entfällt damit. |
| B3 | **G5-3 Kern:** Flächen je Raum und Zone aus Raumkörper ↔ Bauteilkörper (Muster `Koerpernachbarschaft`), gegliederte Wände je Teilfläche als eigene Zeile, Löcher nach Lage statt nach Flächenanteil, Fenster und Türen mit eigener Raumzuordnung, Richtung bei Wänden gegen Erdreich, Dachneigung aus dem Körper bei Dach mit Mengensatz. |
| B4 | **G5-3 Oberfläche:** Herkunft der Fläche (`Tab_Bauteil.Flaechenherkunft`) im Zuordnungsdialog und im Bauteilsteckbrief; Farbmodus „Befund“ (B1). |

