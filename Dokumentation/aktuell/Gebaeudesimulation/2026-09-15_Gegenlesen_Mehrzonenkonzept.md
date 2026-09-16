# Gegenlesen des Mehrzonenkonzepts (15.09.2026)

**Papier:** [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md)
— gegengelesen in Rev. 1 (1 133 Zeilen), fortgeschrieben zu **Rev. 2** (1 345 Zeilen).

**Drei Leser, drei Blickwinkel.** Jeder las dasselbe Papier vollständig und gegen eine eigene
Messlatte:

| Leser | Blickwinkel | Messlatte |
|---|---|---|
| **Physik** | Rechenweg, Gleichungen, Reduktion, Kopplung, Konvergenz | Richtlinientext VDI 6007 Blatt 1 und VDI 2078 (Gleichung und Seite je Behauptung), Befunde O, I, J |
| **IFC** | Leser, Grenzflächen, Materialien, Zonierung, xBIM-Schnittstelle | IFC4.3-Spezifikation, xBIM-Quelltext, die vier gemessenen Beispieldateien (Befunde C, N, P, R, S) |
| **Entscheide und Hausregeln** | Datenmodell, Migration, Auslieferung, Dialoge, Tests, Repositoriumsregeln | Quelltext und Schema des Bestands, `CLAUDE.md` der Wurzel und der Projekte, Entscheide E1–E10 |

**Zusammenführung.** Die drei Rohbefunde wurden zu einer Liste zusammengeführt: 20 Einträge der
Schwere **hoch** (H1–H20), 23 **mittel** (M1–M23), 15 **niedrig** (N1–N15), dazu Anhang A (Stil und
Form) und Anhang B (zwischen den Lesern geklärt, nicht übernommen). Zwei scheinbare Widersprüche
zwischen den Lesern wurden dabei aufgelöst und zu je einem Eintrag verschmolzen (H3, M13); ein
Befund entfiel ganz (Anhang B, E7).

**Orchestrator-Punkte.** Drei weitere Korrekturen kamen aus der Zusammenschau mit den parallel
entstandenen Papieren: **O1** (Persistenz der Zuordnung Zone ↔ `GlobalId` als Voraussetzung des
IFC-Exports, Befund S), **O2** (Verweis auf das Datenaustauschkonzept und auf ADR-005), **O3**
(Produktausweis im Wortlaut von Entscheid E10, Entscheide E1–E10 statt E1–E8).

---

## Schwere hoch (20)

| Nr. | Stelle | Befund | Erledigung |
|---|---|---|---|
| H1 | 2.3, Kopplungsformel | Gl. (41)/(42) gewichten θ_A,eq über **alle** p Bauteile der AW-Gruppe mit Σ B_v = 1; die Papierform führte θ_ext mit Gewicht 1 und legte die Nachbaranteile obendrauf (Gesamtgewicht 1 + ΣB_zj) | eingearbeitet (Formelblock neu, Begründung ergänzt; nachgezogen in 0.1 Punkt 2 und 3, 2.2, Probe 9, Risiko in 11, M1) |
| H2 | 0.1, 1.1, 2.1, 2.2 Punkt 2 | „bitgleich wie heute" trägt nach G3 nicht: G3 stellt den Fensterpfad auf Gl. (25)–(28) und R_rad auf Gl. (29)/(31) um — beides sitzt im gemeinsamen Löser | eingearbeitet (alle vier Stellen auf „desselben Programmstands (nach G3)"; neue **Probe 12a** in 8.1 und in der Abnahme von G6b) |
| H3 | 2.2 Punkt 3, Probe 4 | Zwei Fälle statt einem: asymmetrisch beaufschlagte Trennwand von jeder Seite **ganz** reduziert (C₁,korr), symmetrisch beaufschlagte bis zur Mittelebene (halbe Masse je Zone); Probe 4 maß einen Strom, den das 2-K-Modell nicht kennt | eingearbeitet (2.2 Punkt 3 neu samt benannter Modellgrenze; Probe 4 auf die Jahresbilanz je Zone umgestellt; Risikozeile in 11 geteilt). Nachbesserung: auch **Probe 3** verlangte noch den nicht ausweisbaren Strom — sie misst jetzt den Trennflächensummanden der Gl. (41)/(42) und die Jahresenergie |
| H4 | 2.4, Abbruchkriterium | In einer ideal geregelten Zone ist θ_air Vorgabe — das Temperaturkriterium meldet Konvergenz im ersten Durchlauf, während die Heizlast noch wandert | eingearbeitet (zusätzlich **0,1 W**; in 0.1 Punkt 3, Tabelle 2.4 und G6b nachgezogen) |
| H5 | 2.4, Konvergenzbegründung | Diagonaldominanz gilt je Betriebsmuster; die Regelungszuordnung kann innerhalb der Iteration umschlagen | eingearbeitet (Muster des ersten Durchlaufs wird gehalten und gezählt; Probe 8 um die Pendelstunde erweitert) |
| H6 | 2.2 Punkt 1 | Die 4-K-Regel bezieht sich nach VDI 2078, 7.2, S. 46 auf **Raumkonditionen**, nicht auf Sollwerte | eingearbeitet (gerechnetes Δϑ aus adiabatem Vorlauf, Nachprüfung nach dem Lauf; nachgezogen in 5.3 und M3). Nachbesserung: der Vorlauf steht jetzt auch in den Rechenzeitangaben — Tabelle 2.4, 2.9 und Risiko in 11 |
| H7 | 3.2, Probe 12 | Aufgeklebte Innendämmung erfüllt (10a)/(10b) in der Regel nicht; die Kriterien greifen bei raumseitig abgedeckten Speichermassen | eingearbeitet (Kasten in 3.2 neu gefasst, Probe 12 auf Vorsatzschale mit Luftschicht umgestellt) |
| H8 | 2.2 Punkt 4 | Der U-Wert der Trennfläche war nicht festgelegt; die Wahl entscheidet B_NR | eingearbeitet (raumseitig 1/(α_kon,i + α_str), nachbarseitig 1/α_kon,A;NR; Testbeispiel 10 entscheidet zahlenmäßig) |
| H9 | 6.3, Fenster | `GlazingAreaFraction` ist der **Glasflächenanteil**, nicht der Rahmenanteil — als Rahmenanteil gelesen kehrt sich der Wert um | eingearbeitet (Rahmenanteil = 1 − `GlazingAreaFraction`, Band [0,4; 0,95] für F_F) |
| H10 | 6.3, Schichtsatz | Der `IfcMaterialLayerSet` kann am Element**typ** hängen — der Regelfall in Revit- und Archicad-Exporten | eingearbeitet (Typweg über `IsTypedBy` → `RelatingType` → `HasAssociations` als Pflichtweg, ohne Seitenzuordnung) |
| H11 | 6.3, fehlendes `…Usage` | Eine „Normvorgabe (erste Schicht außen)" gibt es nicht; der direkt zugeordnete Satz liefert keine Lage | eingearbeitet (als EPOS-Annahme mit 50 % Irrtumswahrscheinlichkeit benannt, im Dialog umschaltbar) |
| H12 | 6.2, Orientierung und Paarbildung | `SurfaceOnRelatingElement` liegt im LCS des **Raumes**, nicht des Bauteils; der Schwerpunktvergleich misst ohne gemeinsames System nichts | eingearbeitet (Placement-Kette des `RelatingSpace`; Paarbildung über Weltkoordinaten) |
| H13 | 6.2, Gegenprobe | „Σ oben ≈ Σ unten" ist gegen eine globale Spiegelung blind | eingearbeitet (zweiteilig: Σ A_v·n_v ≈ 0 und Verankerung an `BASESLAB`/`EXTERNAL_EARTH`; in 5.3 nachgezogen) |
| H14 | 6.2, Nachbarzone | `CorrespondingBoundary.RelatingSpace` ist ein `IfcSpaceBoundarySelect` — auch `IfcExternalSpatialElement` | eingearbeitet (nur der Raumfall führt auf eine Zone; Außenraumobjekt auf `AUSSENLUFT`/`ERDREICH`) |
| H15 | 6.1, Zeile Z1 | `IfcSpatialZone` mit `PredefinedType = THERMAL` fehlt und wurde nie gemessen (`IFCZONE` ist kein Teilwort von `IFCSPATIALZONE`) | eingearbeitet (Z1 zweistufig, Messung vor G6c nachzuholen; in 0.1 Punkt 4, 6.1 und G6c nachgezogen) |
| H16 | 6.4 gegen 6.6 | 78 „Zonen" sind 78 **Räume**; zugleich lehnte 6.6 bei N > 50 hart ab — der Dialog wurde mit einer Datei begründet, die derselbe Leser abweist | eingearbeitet (Räume statt Zonen; Fehlerbild auf **Warnung mit Rückfrage**; Obergrenze als offene Frage in M12, nachgezogen in 2.9 und 5.3) |
| H17 | 6.5, IFC2x3 | Am xBIM-Quelltext entschieden: `HasProperties` trägt in 2x3 `Name == null`; ein Filter auf `Pset_MaterialThermal` fände nie etwas | eingearbeitet (Sammeln nach Eigenschaftsnamen; offen bleibt nur, ob reale 2x3-Dateien die Werte füllen) |
| H18 | 6.2, Öffnungsabzug | Beide Abzugsterme können gleichzeitig null sein — dann zählt das Fenster zweimal; der behauptete Ausschluss ist unbelegt | eingearbeitet (geometrischer Rückfall, neues Fehlerbild `IMP_IFC_PROT_OEFFNUNG_OHNE_ABZUG`, Messauftrag `ParentBoundary` vor G6c) |
| H19 | Kopf / `Dokumentation/LIESMICH.md` | Das Papier hat keine Indexzeile; Fall 2 der `DokumentationLinkWacheTests` schlägt fehl | erledigt — `Dokumentation/LIESMICH.md` führt jetzt **zwei** Zeilen: eine für das Papier und eine für dieses Protokoll, das ebenfalls keine hatte; die zweite kam mit der Nachbesserung |
| H20 | 3.5, 4.2 | `Tab_Bauteil`, `Tab_Bauteilschicht` und `Tab_Baustoff_STAMM` wurden zweimal und verschieden definiert (G3 und G6a) | eingearbeitet (sie entstehen **einmal mit G3** in der hier vorgeschlagenen Form; 4.4 und G6a nachgezogen). Nachbesserung: Kapitel 6.3 des Grundkonzepts trägt die Form jetzt selbst (`Bezeichner`, `ID_Aufbau`, Angleichungsabsatz) |

**Die beiden Indexzeilen stehen jetzt in `Dokumentation/LIESMICH.md`** — eine für
`aktuell/Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md` im Block der Konzeptpapiere, eine für dieses
Protokoll im Block `Gebaeudesimulation/`. Die zweite fehlte bis zur unabhängigen Prüfung
unbemerkt: Fall 2 der `DokumentationLinkWacheTests` fiel für **zwei** Dateien, nicht für eine.
Beide sind dreispaltig nach dem Muster der Nachbarzeilen gesetzt, mit dem Datum `2026-09-15`. (Hier bewusst ohne Abzug der Zeilen: Die Wache prüft jeden Verweis auch
innerhalb von Codeblöcken, und der Pfad `aktuell/…` löst von diesem Ordner aus nicht auf.)

---

## Schwere mittel (23) und Orchestrator-Punkte

| Nr. | Stelle | Befund | Erledigung |
|---|---|---|---|
| M1 | Probe 2 | „bitgleich" ist für zwei Halbzonen gegen die ungeteilte Zone numerisch nicht erreichbar | eingearbeitet (paarweise bitgleich; Gebäudesumme mit 1e‑9 relativ / 1e‑6 K) |
| M2 | 2.8, Skalierung | Falsche Fundstelle (`SimulationWaermebedarf.cs:435` ist ein Kopfkommentar) und unstimmiger Zonenausweis | eingearbeitet (`BhkwPlan.cs:435`, Argumente `:392` — selbst nachgelesen; Zonenlasten tragen denselben Faktor; Hinweis, dass E8 dieselbe Fundstelle berichtigen muss). Nachbesserung: E8 im Grundkonzept berichtigt (`BhkwPlan.cs:435`, Argumente `:392`; Rückrechnung `SimulationWaermebedarf.cs:613-656`) |
| M3 | 2.8, `Waermelast_Max` | Das Feld gibt es in `SimulationWaermebedarf` nicht | eingearbeitet (`Waermebedarf_Max` `:401`, abgeleitet in `SimulationRunner.cs:358` — beides selbst nachgelesen) |
| M4 | 2.10 | Testbeispiel 10 belegt den Kopplungs**pfad**, nicht die Rückkopplung (Keller mit vorgegebener Temperatur) | eingearbeitet (2.10 neu gefasst, Probe 1 um den zweiten Lauf mit frei schwingendem Keller erweitert) |
| M5 | 2.2 / 3.4 | Die Bemaßungsregel der AW-Gruppe fehlte | eingearbeitet (neuer Absatz „Flächen" in 2.2, mit Bezug auf Gl. (27), (29)/(31), (42)) |
| M6 | 2.6 | „kein Strahlungsweg über Zonengrenzen" ist zu absolut — Q̇_str,A,NR in Gl. (40) existiert | eingearbeitet (EPOS setzt ihn zu null und benennt das; Risikozeile in 11 nachgezogen) |
| M7 | 3.5 gegen 4.2 | Die `Herkunft`-Werte standen in drei Schreibweisen da | eingearbeitet (durchgängig `IFC`/`KATALOG`/`MANUELL`/`VORGABE` mit `CHECK`) |
| M8 | 3.5, Zeile N3 | `Tab_Baustoff_STAMM.Name` gegen hausübliches `.Bezeichner` | eingearbeitet (N3 auf `Bezeichner`, Angleichung von Konzept 6.3 benannt; `Bezeichnung` in 4.2 mitgezogen). Nachbesserung: Konzept 6.3 selbst angeglichen |
| M9 | 4.2, `Tab_Zonenluftstrom` | `CHECK (ID_ZoneA < ID_ZoneB)` normiert nur die Richtung | eingearbeitet (zusätzlich `CREATE UNIQUE INDEX IF NOT EXISTS`, Muster `SpeicherAuslegungCtrl.cs:37-38` — selbst nachgelesen) |
| M10 | 4.2, `Tab_Bauteilschicht_STAMM` | Eine Auslieferungszeile kann nicht auf die Projektkopie zeigen (`Tab_Baustoff.ID_Projekt NOT NULL`) | eingearbeitet (gleiche Spalte, je Seite eigenes `REFERENCES`, Umsetzung über die Id-Abbildung) |
| M11 | 4.4, Schritt S-D | `sql/tools/Reduziere-Testdatenbank.sql` fehlte; drei Tabellen brauchen den zweistufigen Unterausdruck | eingearbeitet (sechs neue Projekttabellen in S-D benannt) |
| M12 | 3.5, Auflagenkasten | Die ReadOnly-Auflage gilt seit #160‑E‑1a nur bei `--kataloge readonly`; der Wächter meldet nur den Fall „von Zeilen auf null" | eingearbeitet (Kasten neu; zusätzlich `Tab_Bauteilschicht_STAMM` ohne Spalte `ReadOnly` — `Vorlagenbau.cs:106-112`, `:175`, `Argumente.cs:72` selbst nachgelesen) |
| M13 | 8.2, Lizenz | Die Lizenzaussage widersprach sich selbst: MIT gilt für das GitHub-Repositorium, die verwendete `…_with_SB`-Fassung stammt aus dem E3D-GitLab | eingearbeitet (Beleg mit Repositorium, Commit und Abrufdatum verlangt; Zahlenabweichung zu Konzept 7.8 benannt) |
| M14 | 10, M10 | Eine 17,6-MB-IFC-Datei fällt nicht unter LFS (`.gitattributes` führt nur die Testdatenbank und die VDI-3805-Archive) | eingearbeitet (LFS-Zeile und Vermerk in `Referenzlaeufe/LIESMICH.md` als Auflage — `.gitattributes:64`, `:65-67`, `:87` selbst nachgelesen) |
| M15 | 2.6 | Vier der neun Felder gibt es im Bestand nicht; sie entstehen erst mit Schemaschritt 77/78 | eingearbeitet |
| M16 | 0.1 Punkt 6, 5.1 | Zwei neue Kataloge (`BAUSTOFF` **und** `AUFBAU`), aber ein Eintrag und ein Menüpunkt | eingearbeitet (21. und 22. Eintrag, zwei Ausprägungen, zwei Menüpunkte; G6a nachgezogen) |
| M17 | 6.2, billiger Ersatz | Der Ersatz zog Innentüren von der Außenwandfläche ab | eingearbeitet (getrennt nach Randbedingung, Zahl und Fläche im Dialog) |
| M18 | 6.1, Regel B3 | Der Wintersollwert wurde ohne Einheitenauflösung verglichen — in Kelvin ist jede Raumtemperatur > 12 | eingearbeitet (`THERMODYNAMICTEMPERATUREUNIT` aus `UnitsInContext`; ohne Einheit greift B4) |
| M19 | 6.6, 6.2 | `IfcSurfaceOfLinearExtrusion` ist nicht notwendig gekrümmt | eingearbeitet (eben bei `IfcPolyline`/`IfcLine`: Profillänge × `Depth`; Fehlerbild in „nicht auswertbare Grenzfläche" umbenannt) |
| M20 | 6.2, Flächeninhalt | „Trapezformel über die ersten beiden Koordinaten" scheitert an senkrechten Wänden mit konstanter x-Koordinate | eingearbeitet (Projektion auf die Achsen der `IfcPlane.Position`) |
| M21 | 6.2, Abnahme-Sollwerte | Die drei Summen sind Bruttosummen aus einer Textauszählung, keine Hüllfläche | eingearbeitet (als Reproduktionsprobe geführt; Probe 14 nachgezogen) |
| M22 | 6.2, Bauteilart | `IfcCurtainWall` in der Restkategorie verliert die solaren Gewinne; die 198,6 m² sind `IfcColumn` + `IfcBeam` | eingearbeitet (neue Bauteilart `VORHANGFASSADE` mit U- und g-Wert, in 4.2 in die `CHECK`-Liste aufgenommen) |
| M23 | 6.2, `VIRTUAL` | Die virtuelle Grenze ist die einzige belegte Luftverbindung — sie zu verwerfen widerspricht 2.7 | eingearbeitet (Vorschlag als Zonen-Luftaustausch; Zusammenlegen ist Dialogvorschlag, keine Leserregel) |
| **O1** | Kapitel 6 (G6c) | Der Import muss die Zuordnung Zone ↔ `IfcSpace.GlobalId`, Gebäude ↔ `IfcBuilding.GlobalId` sowie Name, SHA-256 und Zeitpunkt der Quelldatei persistieren | eingearbeitet (neuer Absatz im Kopf von Kapitel 6, mit Verweis auf Befund S und das Datenaustauschkonzept; in G6c nachgezogen) |
| **O2** | 1.2 / 1.3 | Auch der gbXML-Import liefert Zonen und speist dasselbe Zonenmodell; die Kopplungsentscheidung steht als ADR-005 | eingearbeitet (Absatz in 1.2 mit Verweis auf das Datenaustauschkonzept, Absatz in 1.3 mit Verweis auf ADR-005; zusätzlich in 0.1 Punkt 3 und M1) |
| **O3** | Kapitel 7 (und 0.1, 11) | Produktausweis im Wortlaut von E10; „validiert an den zwölf Testbeispielen" war falsch | eingearbeitet (Kapitel 7 trägt den Wortlaut, 0.1 und 11 verweisen darauf). „E1–E8" kam im Papier nicht vor — der Kopf nennt bereits E1–E10 |

---

## Schwere niedrig (15) und Anhang A

| Nr. | Stelle | Befund | Erledigung |
|---|---|---|---|
| N1 | 2.10 | Falsche Seitenangabe für Tabelle A5.3 | eingearbeitet (S. 48/49) |
| N2 | 2.7 | Der Nachbarzonenstrom in θ_Lue ist eine EPOS-Erweiterung | eingearbeitet (als EPOS-Regel in Anlehnung an Gl. (75) gekennzeichnet) |
| N3 | 2.9 | Rechenzeit ohne zweiten Vorlauf und Umschaltsuche | eingearbeitet (0,8–1,7 s; Tabelle 2.4 und Risiko in 11 nachgezogen). Nachbesserung: der adiabate Vorlauf der 4-K-Zuordnung (H6) kommt als eigener Posten dazu — zusammen rund 1,1–2,0 s |
| N4 | 3.1 | Reihenfolge (20)/(21) gegen (22) vertauscht | eingearbeitet |
| N5 | 0.2 | Zitat unvollständig — 5.3 **verlangt** die Aufteilung großer Räume | eingearbeitet |
| N6 | 4.4, Falle (1) | Die Sicht trägt 58 Spalten in Zeile 90, nicht 56 in Zeile 89 | eingearbeitet (`002_views.sql:89-91` selbst nachgezählt) |
| N7 | 4.4, S-E gegen Kapitel 9 | Derselbe Schritt stand in zwei Stufen | eingearbeitet (S-E gehört mit G1; Absatz unter der Tabelle nachgezogen) |
| N8 | 4.4, Falle (2) | `ID_Bauteil` entsteht nirgends | eingearbeitet |
| N9 | 7, Wiki | `Tab_Baustoff` darf nicht in die Gerätekatalogliste des Wächters | eingearbeitet (`WikiProduktdatenWacheTests.cs:93-94` selbst nachgelesen) |
| N10 | 8.1 Probe 7, 2.9 | `ParallelitaetWacheTests` vergleicht keine Reihen | eingearbeitet |
| N11 | 4.1 / 1.2 / 4.3 | `LesenJeProjekt` und `LesenJeAnlage` vertauscht; Schemaprobe `:102` statt `:101` | eingearbeitet (beide Zeilen selbst nachgelesen) |
| N12 | 4.2, Bauteiltabelle | Die Azimut-Vorgabe nannte Neigungswerte — „NULL = 0°" hieße für jede Wand Nord | eingearbeitet (zwei Zeilen, Azimut-NULL nur bei Neigung 0°/180°) |
| N13 | 4.4 | ADR-001 Option C verlangt nummerierte Schritte mit Konstante | eingearbeitet |
| N14 | 6.3 | `HasProperties` ist geerbt, also ohne Umwandlung abrufbar | eingearbeitet |
| N15 | 6.3 | `IfcMaterialLayerWithOffsets` fehlte | eingearbeitet (mittlere Dicke, Fall im Dialog benannt) |
| A1 | Kopf und Aufbau | entsprechen den Schwesterpapieren | nichts zu ändern |
| A2 | Kodierung | UTF-8 ohne BOM, überlange Zeilen sind Byte-Effekte aus θ, λ, ρ | nichts zu ändern (Zeilenenden auf CRLF gebracht, siehe unten) |
| A3 | Rev.- und Datumsvermerk | unter `Dokumentation/aktuell/` zulässig | nichts zu ändern (Rev.-2-Block ergänzt) |
| A4 | Bezeichner gegen Name gegen Bezeichnung; Persistenzwerte in Großbuchstaben | durchzuziehen | eingearbeitet (mit M7 und M8) |
| A5 | Zitierform: je Behauptung eine Seite und eine Gleichungsnummer | teils gebündelt | eingearbeitet, soweit von N1 und den Physikeinträgen berührt; die übrigen Bündelungen **offen (Form)** |
| A6 | „bitgleich" in drei Bedeutungen | nur dort verwenden, wo derselbe Code denselben Weg geht | eingearbeitet (mit H2 und M1; **acht** verbliebene Vorkommen geprüft — sieben war falsch gezählt; das Kriterium von Probe 1 nennt mit der Nachbesserung ausdrücklich, warum es dort trägt) |

---

## Anhang B — nicht übernommen (zwischen den Lesern geklärt)

| Befund | Warum nicht übernommen |
|---|---|
| „Das Papier verletzt E7, weil es das Mehrzonenmodell auch ohne IFC-Import anbietet" (Leser Repositorium, dort „hoch") | E7 verlangt im Wortlaut die **Dialoge für die Zoneneingabe**, und die Anwendervorgabe vom 15.09.2026 fragt ausdrücklich danach; die Handeingabe ist beauftragt, nicht zusätzlich. Von dem Befund bleibt die Reihenfolge G6b vor G6c — und die begründet Kapitel 9 selbst |
| Physik-Befund 6 gegen Repository-Befund 3 („nichts halbieren" richtig oder falsch) | Beide haben für je einen Fall recht; zusammengeführt in **H3** |
| Physik-Befund 20 gegen Repository-Befund 6/7 (Fundstellen in 2.8) | Die Repository-Fassung ist am Quelltext die genauere; übernommen als **M2** und **M3** |
| Lizenz DigitalHub (Repositorium 15 gegen IFC 20) | MIT für das GitHub-Repositorium belegt, die verwendete `…_with_SB`-Fassung stammt aus dem E3D-GitLab; daraus wurde **ein** Befund (M13) statt zweier gegenläufiger |

---

## Zählung

| Schwere | Einträge | eingearbeitet | abgelehnt | offen |
|---|---|---|---|---|
| hoch (H1–H20) | 20 | 20 | 0 | 0 |
| mittel (M1–M23) | 23 | 23 | 0 | 0 |
| niedrig (N1–N15) | 15 | 15 | 0 | 0 |
| Orchestrator (O1, O2 mittel; O3 niedrig) | 3 | 3 | 0 | 0 |
| Anhang A (A1–A6) | 6 | 3 (A4–A6, A5 teilweise) | 0 | 1 (A5, Form) — A1–A3: nichts zu ändern |
| Anhang B | 4 | — | — | nicht übernommen (zwischen den Lesern geklärt) |

**Summe:** 58 Einträge der Liste (20 hoch, 23 mittel, 15 niedrig) und 3 Orchestrator-Punkte;
**alle 58 und alle 3 eingearbeitet** (H19 mit der Nachbesserung), 0 abgelehnt; aus Anhang A sind
3 eingearbeitet und **1 offen (Form: gebündelte Seitenangaben, A5)**, 4 nicht übernommen
(Anhang B). Die frühere Summenzeile (60 Einträge, 60 eingearbeitet) widersprach der Tabelle
darüber und ist hiermit berichtigt.

## Drei Stellen, an denen vom Wortlaut der Liste abgewichen wurde

1. **H1, Hilfsgröße θ̄_A,eq,ext.** Die Liste schreibt `θ̄_A,eq,ext,z = Σ θ_A,eq,v · B_v` und
   multipliziert sie zugleich mit (1 − Σ B_zj). Beides zusammen würde das Außenteil doppelt
   gewichten. Im Papier steht die Hilfsgröße deshalb **normiert**
   (geteilt durch Σ B_v der Außenbauteile); die Gesamtform ist damit genau Gl. (41) mit Σ B_v = 1 —
   die Aussage der Liste, nicht ihre Schreibweise.
2. **H16 / M12.** Die Liste stellt das Fehlerbild auf „Warnung" um und schiebt die Obergrenze als
   offene Frage neben M12. Damit standen 2.9 und 5.3 (harte Ablehnung) dagegen; beide sind
   mitgezogen: Der **Import** warnt und schlägt das Zusammenlegen vor, die **Rechnung** lehnt
   darüber benannt ab, und die Zahl 50 selbst bleibt offen, bis Probe 6 gemessen hat.
3. **M13, Raumzahl.** Der Nebenbefund aus H16 („Konzept 7.8 nennt 83 Räume") hat sich beim
   Nachlesen **nicht bestätigt**: `Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md:1229` führt
   dieselben 59 Räume. In den Vermerk gehört damit nur die abweichende U-Wert-Zahl (719 gegen 359)
   und die abweichende Quelle.

## Nicht selbst nachgeprüfte Belege

`Datei:Zeile`-Angaben aus dem Bestand sind vor der Übernahme selbst nachgelesen worden. **Nicht**
nachprüfbar waren die Angaben zu xBIM (`Xbim.Ifc2x3/Interfaces/IFC4/IfcMaterial.cs:103`,
`…/IfcMaterialProperties.cs`, `Xbim.Ifc4/ProductExtension/IfcRelSpaceBoundary.cs:30`) und zu den
buildingSMART-Property-Set-Definitionen (`reference_schemas/psd/Pset_WindowCommon.xml`): Beide
liegen nicht im Repositorium. Sie stehen im Papier mit der Begründung des Gegenlesers und sind
beim ersten Lauf des Lesers gegen das Paket zu bestätigen (H9, H10, H14, H17, N14, N15).

## Form

Beide Dateien sind **UTF-8 ohne BOM** mit **CRLF** (`.editorconfig`, `[*.md]`). Alle zwölf
repo-relativen Verweisziele des Papiers lösen auf — einschließlich dieses Protokolls und des
parallel entstandenen Datenaustauschkonzepts.
Die beiden Indexzeilen in `Dokumentation/LIESMICH.md` sind mit der Nachbesserung
nachgetragen (H19); die Statuszeile pflegt der Orchestrator. Nach der Nachbesserung erneut
gemessen: beide Dateien weiter UTF-8 ohne BOM und durchgängig CRLF, ebenso das mit
berührte Grundkonzept.

---

## Nachbesserung nach der unabhängigen Prüfung (15.09.2026)

Eine vierte, unabhängige Prüfung hielt Papier und Protokoll noch einmal gegen die Korrekturliste.
Sie bestätigte die Einarbeitung von H1–H18, H20, M1–M23, N1–N15 und O1–O3 und fand sechs Lücken;
alle sechs sind behoben:

| Lücke | Was geändert wurde |
|---|---|
| **H19 — zwei statt einer Indexzeile** | `Dokumentation/LIESMICH.md` führt jetzt eine Zeile für das Papier und eine für dieses Protokoll; ohne die zweite wäre Fall 2 der `DokumentationLinkWacheTests` weiter gefallen |
| **Probe 3 gegen Probe 4 (H3)** | Probe 3 verlangte unverändert einen Wärmestrom über die Trennfläche, den das 2-K-Modell nach Gl. (28), S. 17 nicht ausweist. Sie misst jetzt den auf die Trennfläche entfallenden Summanden B_NR·θ_NR,eq der Gl. (41)/(42) gegen B_NR·θ_air der eigenen Zone (< 0,01 K je Stunde) und die Jahresenergie (0,1 %) |
| **Adiabater Vorlauf in den Rechenzeitangaben (H6 gegen N3)** | Der Vorlauf mit adiabaten Trennflächen, aus dem die 4-K-Zuordnung fällt, fehlte in 2.9, in Tabelle 2.4 und in der Risikozeile. Er steht jetzt in allen dreien als eigener Posten: ein ungekoppelter Durchlauf je Zone, rund 0,25 s bei 50 Zonen, für jeden Kopplungsweg gleich — zusammen rund 1,1–2,0 s |
| **Aufwände in Kapitel 9 nicht nachgezogen** | Die Summe 34–52 PT stimmt, war nach Rev. 2 aber nicht mehr belegt. Kapitel 9 sagt jetzt, welche Arbeit G6a an G3 abgibt (H20) und welche zu G6b und G6c hinzukommt (H2, H6, H10, H15, H18, M22, O1), und dass sich die Verschiebungen in der Größenordnung aufheben; 0.1 Punkt 6 verweist darauf |
| **Nachbarpapier nicht nachgezogen (H20, M8, M2)** | `Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md` führt in 6.3 jetzt `Bezeichner` statt `Name` und `ID_Aufbau` statt `ID_Bauteil` samt Angleichungsabsatz; Entscheid E8 nennt die richtige Fundstelle `BhkwPlan.cs:435` (Argumente `:392`) und für die Rückrechnung `SimulationWaermebedarf.cs:613-656` — beide selbst nachgelesen |
| **Buchhaltung dieses Protokolls** | Zählungstabelle und Summenzeile widersprachen sich (60 gegen 67/63); beide sind berichtigt, O1–O3 stehen als eigene Zeile, und A6 zählt jetzt die tatsächlichen **acht** Vorkommen von „bitgleich" statt sieben. Das Kriterium von Probe 1 sagt zusätzlich, warum „bitgleich" dort trägt (ideal gehaltene Kellerzone) |

Aus der Liste ist damit nichts mehr offen; offen bleibt allein A5 (gebündelte Seitenangaben, Form).
