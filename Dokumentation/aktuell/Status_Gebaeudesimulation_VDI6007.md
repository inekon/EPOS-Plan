# Status der Gebäudesimulation VDI 6007 (EPOS-Plan)

**Stand 16.09.2026.** Diese Datei beantwortet eine einzige Frage: **was ist wann mit welchem
Ergebnis entschieden und umgesetzt worden — und was ist offen.** Das Konzept (Befunde,
Rechenweg, Datenmodell, IFC, Stufen, Fragen) steht in
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md);
die Befunde der Prüfagenten und das Gegenlesen liegen als Protokolle unter
[`Gebaeudesimulation/`](Gebaeudesimulation/).

**Die Fortschreibungsregel.** Je Entscheid und je Stufe eine Zeile hier. Der ausführliche
Block (Befund, Umsetzung, Nachweis, Zahlen) kommt als Nachtrag in das Konzept oder als
Protokoll unter `Gebaeudesimulation/` und wird von hier aus
verwiesen. Keine Commit-Kennungen in dieser Datei (Regel aus
[`Status_iOS_Migration.md`](Status_iOS_Migration.md)).

**Lesart der Stände.** „**umgesetzt**" heißt: gebaut, getestet und gegen die Referenzbasis
gefahren. „**teilweise**" heißt: Arbeit getan, Abnahme steht aus. „**offen**" heißt: nicht
begonnen.

---

## 1 Entscheide des Anwenders

| Nr. | Frage (Konzept, Kapitel 13) | Entscheid | Datum | Stand |
|---|---|---|---|---|
| Q1 | Modellwahl je Gebäude, Vorgabe Tagesbilanz oder Stundenwerte? | **Endgültig entschieden (E1): Stundenmodell VDI 6007 für alle Gebäude, auch bestehende**; Tagesbilanz nur als ausdrücklich wählbare Ausnahme; Basis wird mit G1 vollständig neu eingefroren, der Bestandsweg bleibt über ausdrückliche Wahl regressionsgeprüft (Konzept N1.1) | 15.09.2026 | entschieden |
| Q2 | Quelle der Normreferenzwerte und Klimadaten | Blatt 1–3, VDI 6020:2022 und VDI 2078:2015 liegen vor (Befunde I und K). Blatt-1-Referenzwerte aus den gedruckten Tabellen A1.3–A12.3, Band ± 0,1 K / ± 1 W; Normzahlen nicht ausliefern. **E5: keine Datenträger, keine DWD-TRY — Klimabasis sind die vorhandenen PVGIS-TMY-Daten**; Strahlungsweg über VDI 2078 Testbeispiel 7.1 und internen Modellvergleich Blatt 3 gegen Hay-Davies auf TMY (Konzept N1.2, N1.9, N1.10) | 15.09.2026 | entschieden |
| Q14 | Neues Referenzprojekt mit VDI 6007? | mit E1 erledigt: alle dreizehn Referenzprojekte rechnen ab G1 stündlich und werden neu eingefroren | 15.09.2026 | entschieden |
| Q16 | Bestandsgewichte im Stundenmodell streichen? | **entschieden (E2): ja**; der Gebäudedialog zeigt je Bauteil U, A und U·A ohne verdeckte Faktoren, gemeinsame Zielstruktur für VDI-Weg und IFC-Import (Konzept N1.6) | 15.09.2026 | entschieden |
| Q9 | IFC-Bibliothek | **entschieden (E3, nach Empfehlung): xBIM Essentials als unverändertes NuGet-Paket unter CDDL-1.0**, Lizenztext und Quellenverweis im Installationspaket; Ausweg GeometryGymIFC (MIT) (Konzept N1.7) | 15.09.2026 | entschieden |
| Q22, Q23 | Korrektur 10576, Instanzzustand, vierte Einfrierregel | **entschieden (E4, nach Empfehlung): ja, im eigenen Einfrierschritt GB vor G1** (Konzept N1.8) | 15.09.2026 | entschieden |
| E7 | Einzonen- oder Mehrzonenmodell? | **Einzonenmodell zuerst (G0–G2, analog Bestand); Mehrzonenmodell über den IFC-Import als spätere Stufe mit eigenem Konzept** `Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md` (Konzept N1.12) | 15.09.2026 | entschieden |
| E8 | Skalierung des Bestands (Hochrechnung auf andere Fläche/Volumen, Verbrauchs-Rückrechnung) | **bleibt im Stundenmodell erhalten** (Konzept 4.7, N1.12); entfällt erst für Gebäude mit echter Hülle aus G3/G4 | 15.09.2026 | entschieden |
| E9 | Austauschformate | **Import und Export von gbXML und IFC**; gbXML-Import wird Pflicht (G4), Exporte als Stufe G7; eigenes Konzept `Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md` nach dem Mehrzonenmodell (Konzept N1.13) | 15.09.2026 | entschieden, Konzept in Arbeit |
| E10 | Prüfregel der Normtestfälle | **Druckrundung als Toleranz**: Band ± 0,15 K bzw. ± 1,5 W um Programm 1…2; Fälle 6, 9, 10 bestehen damit, offen bleibt Fall 11 (Konzept N1.15) | 15.09.2026 | entschieden |
| E11 | IFC-Betrachter in der App? | **entschieden: ein Gebäudebetrachter, zwei Ansichten, ein Zonengeometrie-Modell** — 2D-Grundriss je Geschoss aus den Raumgrenzen (SVG in einer Razor-Komponente, mit G6c) und schematische Körper aus EPOS-Daten (Quader je Zone, Platte je Bauteil, three.js lokal, mit G7b), zusammen gebaut, 10–17 PT; ein vollwertiger 3D-IFC-Betrachter und die xBIM Geometry Engine sind benannt abgelehnt (Konzept N1.16, Datenaustauschkonzept Kapitel 14, Mehrzonenkonzept 6.7) | 15.09.2026 | entschieden |
| Lizenz VDI 6020 | Nutzung der vorliegenden VDI 6020:2022 | **entschieden (E6): nur zu Forschungszwecken** — nichts daraus in Code, Tests, Wiki, Bericht oder Auslieferung; Produktregeln stützen sich auf VDI 6007 Blatt 1–3 und VDI 2078 (Konzept N1.11) | 15.09.2026 | entschieden |
| Q3–Q8, Q10–Q13, Q15, Q17–Q21 | übrige Fragen | — | — | offen |

## 2 Stufen

| Stufe | Inhalt in einem Satz | Stand | Nachweis |
|---|---|---|---|
| G0 | Löser 2-K-Modell mit den zwölf Normtestfällen im Kern | offen — Prototyp außerhalb des Repositoriums gegen das Normband mit Druckrundung (E10, Befund J): **35 von 36 Prüfungen bestanden, 11 von 12 Fälle vollständig im Band**; offen allein Fall 11 mit 3,4 W in zwei Umschaltstunden (3,9 W gegen das Band ohne Druckrundung; Kühldecke als eigener Knoten ist die wahrscheinlichste, nicht bewiesene Ursache); dazu α_kon je Bauteil, Vorzeichen, Normzahlen als nicht ausgeliefertes Prüfmittel (Konzept N1.2, N1.3, N1.14, N1.15) | Befunde E, I, J |
| G1 | Anbindung als Vorgabemodell, Schemaschritt 77, Sicht und Namensleser, Dialoggruppe, Hülle; **zusammen mit G2 auszuliefern**; Basis vollständig neu eingefroren | offen | **Vor dem Einfrieren ausweisen:** der Abzug R_si/A in R_Rest,AW hebt den Jahresbedarf um rund 12 % gegenüber der Prototyp-Konvention, die Vergleichszahlen in Konzept 5.5 entstanden ohne ihn ([Rechenschritte 9.6](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) |
| GB | Bestandsbefunde **vor G1**: Warnungen statt stiller NaN, Instanzzustand statt `_prevRoomTemp`, Korrektur Gebäude 10576, vierte Einfrierregel — eigener Einfrierschritt auf dem Bestandsweg | offen | Befunde D und G |
| G2 | Ergebnisdarstellung, Sommerlüftungsregel, Infiltration/Nutzerlüftung, Wiki-Seite — **mit G1 auszuliefern** (E1) | offen | — |
| G3 | Bauteilkatalog mit Schichtaufbau und Normnachweis der Reduktion | offen | — |
| G4 | IFC-Import Stufe 1 mit xBIM; der **Gebäudebetrachter (E11)** hängt nicht hier, sondern an G6c (Zonengeometrie-Modell und 2D-Grundriss) und G7b (schematische Körper) | offen | Protokoll Befund C |
| G5 | Geometrieableitung, gbXML | offen, nur bei Bedarf | — |

## 3 Papiere und Architekturentscheide

| Papier | Inhalt in einem Satz | Stand |
|---|---|---|
| [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) | Physik, Rechenweg, Stufen G0–G5, Fragen Q1–Q23, Nachtrag 1 mit E1–E11 (N1.1–N1.16) | Rev. 1 mit Nachtrag 1, gegengelesen; Nachzüge aus Rechenschritten (4.3, 5.5, N1.3, N1.15) und Querabgleich vom 16.09.2026 |
| [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) | Einbindung in den Kern, Gebäudedialog (U·A je Bauteil), IFC-Import G4a, gbXML-Import G4c, Exporte G7, Fragen U1–U16 | **Rev. 2** — 62 Korrekturen eingearbeitet, unabhängig geprüft; Protokoll [`Gegenlesen_Umsetzungskonzept`](Gebaeudesimulation/2026-09-15_Gegenlesen_Umsetzungskonzept.md) |
| [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) | Zonenkopplung, Bauteile und Stoffwerte, Datenmodell Zone → Bauteil → Aufbau → Schicht → Baustoff, Zoneneingabe, IFC-Import in Zonen, Stufen G6a–G6d, Fragen M1–M14 | **Rev. 2** — 58 Korrekturen (20 hoch / 23 mittel / 15 niedrig), E11 (6.7, G6c), Querabgleich (FK_MAP, Quellkennung, Herkunft je Aufbau/Baustoff, D16); Protokoll [`Gegenlesen_Mehrzonenkonzept`](Gebaeudesimulation/2026-09-15_Gegenlesen_Mehrzonenkonzept.md) |
| [`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) | gemeinsames Zuordnungsgerüst (`EPOS.Kern/Allgemein/Import/Gebaeude/`), gbXML-Import G4c, Exporte gbXML/IFC G7a–G7e, Persistenz der Zuordnung (`Tab_Importquelle`, `Tab_Importzuordnung`), Lizenz, 27 Proben, Fragen D1–D16 | **Rev. 2** — 64 Befunde eingearbeitet; Protokoll [`Gegenlesen_Datenaustausch`](Gebaeudesimulation/2026-09-15_Gegenlesen_Datenaustausch.md) |
| [`Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) | Rechenbuch: Schritte A–G, Eingaben und Datenquellen, Zahlenweg an Projekt 1045, Prüfband E10, 15 benannte Abweichungen | **fertig** — zwei Lesungen (9 hoch / 25 mittel / 6 niedrig) eingearbeitet, Protokoll als Kapitel 13 im Papier |
| [`ADR-002`](ADR-002_Stundenmodell_VDI6007_Einbindung.md) | Stundenmodell als Vorgabemodell, eine Naht, Basis neu einfrieren (E1, E2, E4, E8, E10) | angenommen |
| [`ADR-003`](ADR-003_IFC_xBIM_ohne_Geometriekernel.md) | xBIM als unverändertes NuGet-Paket, allein `Xbim.IO.MemoryModel`, kein Geometriekernel (E3, E9) | angenommen |
| [`ADR-004`](ADR-004_gbXML_LINQ_to_XML.md) | gbXML mit LINQ to XML statt XmlSerializer, Version 6.01 | vorgeschlagen — Entscheid offen |
| [`ADR-005`](ADR-005_Zonenkopplung_Mehrzonenmodell.md) | Zonenkopplung über Nachbarraum-Randbedingung, Gauß-Seidel je Stunde (M1) | vorgeschlagen — Entscheid offen |
| [`Systementwurf_Gebaeudesimulation_EPOS-Plan.md`](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) | Anforderungen, Komponentenbild, Datenfluss, Verträge, Speicherung, Fehlerbehandlung, Leistung, Determinismus, Abwägungen, Wiedervorlage | **Rev. 2** — 60 Befunde eingearbeitet, zweite unabhängige Prüfung; Protokoll [`Gegenlesen_Systementwurf`](Gebaeudesimulation/2026-09-15_Gegenlesen_Systementwurf.md) |
| [`Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md`](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) | Software, Datenmodell (W1–W21 aufgelöst), Dialogführung, Integration, Umsetzungsreihenfolge, Architekturfragen A1 ff. | **Rev. 2** — 60 Befunde eingearbeitet, zweite unabhängige Prüfung; Protokoll [`Gegenlesen_Softwarearchitektur`](Gebaeudesimulation/2026-09-15_Gegenlesen_Softwarearchitektur.md) |
| Bestandsbefunde T (Software), U (Dialogführung), V (Datenmodell) | Grundlage der Architekturpapiere, unter `Gebaeudesimulation/` | fertig |
| Word-Dokument „Architektur, Design und Rechenweg" (Auftrag 15.09.2026) | Kurzfassung aller Papiere mit 14 Bildern, aus einer Markdown-Quelle gebaut (Werkzeuge außerhalb des Repositoriums) | gebaut und validiert; Ablage unter `Gebaeudesimulation/` |

## 4 Prüfwerkzeuge außerhalb des Repositoriums

Unter `C:\Users\Dirk\AppData\Local\Temp\epos-spike\` liegen die Werkzeuge der Prüfung vom
15.09.2026: `DbProbe` (liest die Testdatenbank nur lesend), `Prototyp` (7R2C-Löser, Aufruf
`dotnet run -c Release -- --all`, Schalter `--tc11-window`, `--diag`), `EposLauf` (Adapter
mit den Gebäudedaten der Referenzprojekte), `daten` (JSON-Abzüge), `referenz` (AixLib-
Modelica-Dateien, überarbeitete BSD-Lizenz), `vdi` (Textfassungen der Richtlinie). Sie sind
die Vorlage für G0 und die unabhängige Zweitimplementierung für die Abnahme von G1 — keine
Auslieferung, kein Teil des Repositoriums. Werkzeuglage des Rechners:
[`Werkzeuglage_Rechner_EPOS-Plan.md`](Werkzeuglage_Rechner_EPOS-Plan.md).
