# Gegenlesen des Datenaustauschkonzepts (15.09.2026)

**Papier:** [`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md)
— gegengelesen in Rev. 1 (1 187 Zeilen), fortgeschrieben zu **Rev. 2** (1 423 Zeilen).

**Drei Leser, drei Messlatten.** Jeder las dasselbe Papier vollständig gegen eine eigene Messlatte:

| Leser | Blickwinkel | Messlatte |
|---|---|---|
| **repo** | Quelltext, Schema, Werkzeuge, Fundstellen | der Bestand selbst — `EPOS.Kern`, `EPOS.iOS`, `sql/`, `Referenzlaeufe/`, `.gitattributes`, die drei Nachbarpapiere |
| **formate** | gbXML-Schema, IFC4, Zielwerkzeuge, Export- und Round-Trip-Physik | Befund R und Befund S Satz für Satz, dazu Schema- und Spezifikationslage |
| **regeln** | Entscheide E1–E10, Hausregeln, Datenmodell, Migration, Auslieferung, Form | `CLAUDE.md` der Wurzel, `.editorconfig`, ADR-001, Mehrzonenkonzept, Umsetzungskonzept |

**Umfang der Liste:** 64 Befunde — 13 **hoch**, 35 **mittel**, 16 **niedrig**. Zwölf davon sind
Doppelnennungen zweier Leser zu derselben Stelle (1/49, 2/47, 3/46, 4/50, 6/54, 7/55, 8/56, 12/57,
15/63, 20/64, 25/52, 36/58); sie wurden zu je einer Einarbeitung verschmolzen.

**Zählung:** **63 eingearbeitet**, **0 abgelehnt**, **1 gegenstandslos** (Nr. 59 — das Papier war
bereits durchgängig CRLF). Offen im Papier: nichts. Drei Punkte liegen **außerhalb** dieses Auftrags
und gehen an den Orchestrator (Abschnitt 4).

---

## 1. Entscheidungen, wo die Leser auseinanderliefen oder der Befund nicht trug

Alle zehn Punkte sind am Quelltext bzw. an den Befunden nachgemessen worden, nicht abgewogen.

| # | Streitpunkt | Entscheidung | Grund |
|---|---|---|---|
| 1 | **Nr. 20 (hoch, formate) gegen Nr. 64 (niedrig, regeln):** Round-Trip einer IFC2x3-Datei — Widerspruch zu 6.2 auflösen oder nur querverweisen? | **Nr. 20 umgesetzt**, Nr. 64 zusätzlich: G7d gilt nur für Quelldateien im Schema **IFC4**, alles andere wird benannt abgelehnt; 6.2 bekommt den Querverweis auf 6.6 | Das Anreichern legt **neue** Entitäten an, und Schreiben verlangt instanziierbare Schematypen (Befund S, 4.3/4.5); `IfcRelSpaceBoundary2ndLevel` gibt es in 2x3 gar nicht. Ein zweiter Schreibpfad bräuchte `Xbim.Ifc2x3` und damit eine Erweiterung von **E3** |
| 2 | **Nr. 15 gegen Nr. 63:** Zeilenbereich des Begründungsblocks in `IDateiDienst.cs` (`:73-101` gegen `:72-101`) | **`:73-101`** | Selbst gemessen: `:72` ist leer, der Rahmen `// ===` steht auf `:73`, die Überschrift „Die WARTBAREN Zwillinge" auf `:74`, der letzte Textsatz auf `:101`; `:102` ist wieder leer, `:103` beginnt der XML-Kommentar von `DateiOeffnenAsync` (`:107`) |
| 3 | **Nr. 15 gegen Nr. 63:** Fundstelle für `KeineDateiwahl` (`:17-20`/`:23-26` gegen `:17-25`) | **`:17-20` (Öffnen) und `:23-26` (Speichern)** | Selbst gemessen: `DateiOeffnen` `:17-20` mit `return ""` auf `:19`, `DateiSpeichern` `:23-26` mit `:25`, `OrdnerWaehlen` `:29-32` mit `:31`. `:17-25` schlüge zwei Methoden in einen Bereich |
| 4 | **Nr. 3 gegen Nr. 46:** Reihenfolge der `CHECK`-Werte für `Herkunft` | **`CHECK (Herkunft IN ('GBXML','IFC','KATALOG','MANUELL','VORGABE'))`** | Das Mehrzonenkonzept führt die vier Bestandswerte **alphabetisch** (`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md:653`); `GBXML` alphabetisch einzuordnen hält die Konvention, statt eine zweite aufzumachen |
| 5 | **Nr. 25 gegen Nr. 52:** Persistenz als eigene Zeile „G4-P" führen oder in G4c einrechnen? | **In G4c einrechnen** (17–28 PT), dazu **G7d −2–3 PT** und **G7a +1–2 PT**, und eine Fußnote zur Herkunft jeder Zahl | Kapitel 10 Nr. 4 sagt bereits, die Persistenz gehöre in die **erste gebaute Importstufe, egal welche**. Eine eigene Zeile legte sie auf G4c fest und widerspräche der eigenen Reihenfolgeregel |
| 6 | **Nr. 12 und Nr. 57:** Indexzeile in `Dokumentation/LIESMICH.md` fehlt | **Teilweise gegenstandslos:** die Zeile für dieses Papier **steht bereits** (`Dokumentation/LIESMICH.md:56`). Umgesetzt wurde nur die Umformulierung von Probe 18 auf die Wache als Dauerprüfung | Selbst nachgezählt. Dem **Mehrzonenkonzept** fehlt die Zeile weiterhin — Abschnitt 4, Nr. 1 |
| 7 | **Nr. 59:** Datei sei mit LF geschrieben | **Gegenstandslos** | Nachgemessen: 1 187 von 1 187 Zeilen mit CRLF, kein BOM. Der Befund traf auf einen älteren Stand zu. Rev. 2 ist ebenfalls durchgängig CRLF (1 423/1 423) |
| 8 | **Nr. 9:** Fundstellen der Kaskadenfalle (`AnlageStrangCtrl.cs:33-43`, `WizardCtrl.cs:110-111`) | **`AnlageStrangCtrl.cs:17` und `:35-45`; `WizardCtrl.cs:109-111`**, dazu `:1094` (Sichern) und `:1187` (Wiederherstellen) | Selbst gemessen: der `<para>` der Falle N3.3 beginnt `:34`, die Überschrift steht `:35`, der letzte Satz `:45`. `StraengeSichern` wird `:111` gerufen, der ST1-Kommentar steht `:109-110` |
| 9 | **Nr. 14:** „18 vorhandene Proben" in `Referenzlaeufe/Importproben/` | **Zahl weggelassen**, die Zusage aufgenommen | Nachgezählt: **27** Proben. Die tragende Aussage ist ohnehin `.gitattributes:87` (`Referenzlaeufe/Importproben/** -text`), und die steht jetzt im Papier |
| 10 | **Nr. 18:** Befund R widerspricht sich selbst — „Pflichtbestandteil des **Modells**" (`:251`) gegen „Pflichtbestandteil des **Schemas**" (`:695`) | **Der Messwert schlägt die Empfehlung:** das Schema erzwingt an `Surface` nur `id`, `surfaceType`, `constructionIdRef` (Befund R, 1.3, mit `ValidationType.Schema` nachgemessen; Gegenprobe: eine `Surface` ganz ohne Geometrie validiert) | Das Papier sagt jetzt beides: schemaseitig **nicht** erzwungen, in allen vier ausgezählten Dateien **vorhanden**. Damit trägt das Argument für D1 weiter, ohne eine falsche Schemaaussage |

---

## 2. Die Befunde im Einzelnen

Schwere wie in der Korrekturliste. „Erledigung" nennt die Stelle in **Rev. 2**.

| Nr. | Schwere | Leser | Stelle | Befund (Kurzform) | Erledigung |
|---|---|---|---|---|---|
| 1 | hoch | repo | 2.5 | `.xml` falle auf `public.data` zurück, keine registrierte UTI für beide Formate — beides falsch | **eingearbeitet** (2.5): nachzutragen sind allein `.ifcxml` → `public.xml` und `.ifczip` → `public.zip-archive`; für `.ifc` bleibt `public.data` (`Dateifilter.cs:22/:29/:32/:37-40`), für gbXML ist nichts zu tun. Mit Nr. 49 verschmolzen |
| 2 | hoch | repo | 3.4 | Zielspalte `Bezeichnung` statt `Bezeichner` | **eingearbeitet** (3.4, Zeile `Surface`). Mit Nr. 47 verschmolzen |
| 3 | hoch | repo | 1.4, 7.3, 2.1, 2.2 | `KATALOG` fällt aus dem Wertebereich von `Herkunft` | **eingearbeitet**: 1.4 Nr. 2, 7.3, `Importherkunft` in 2.1, neue Zeile `Katalog` in 2.2. Mit Nr. 46 verschmolzen |
| 4 | hoch | repo | 7.4 | `ProjektDuplizierenCtrl.ID_MAP` gibt es nicht | **eingearbeitet** (7.4): `FK_MAP` (`:85`), `FK_OVERRIDE` (`:140`), `KINDER` (`:152`), Begründung `:113-118`; dazu der ausdrückliche Satz, dass es keine `ID_MAP` gibt. Mit Nr. 50 verschmolzen |
| 5 | mittel | repo | 7.5 | „vier Einfrierregeln", „fünfte" — heute sind es drei | **eingearbeitet** (7.5, Punkt 3): drei heute (`Referenzlaeufe/LIESMICH.md:57/:79/:100`), vier nach GB (E4), fünf nach G6 |
| 6 | mittel | repo | 3.7, 3.4 | „Normvorgabe" statt Hausannahme; Schichtfolge wird nicht umgekehrt | **eingearbeitet** (3.7 und 3.4): benannte EPOS-Annahme mit 50 % Irrtum je Bauteil; `Reihenfolge` zählt innen → außen, die gbXML-Folge wird beim Schreiben umgekehrt. Probe 1 erweitert. Mit Nr. 54 verschmolzen |
| 7 | mittel | repo | 7.4 | U5 als erledigt dargestellt; „nächste freie Nummer nach 78" widerspricht der eigenen Aufzählung | **eingearbeitet** (7.4): 77 für G1, U5 **offen**, sonst 78; S-F hinter den Mehrzonenschritten. Mit Nr. 55 verschmolzen |
| 8 | mittel | repo | 7.4 | `sql/tools/Reduziere-Testdatenbank.sql` fehlt in der Registerpflege | **eingearbeitet** (7.4): zwei `DELETE`-Zeilen nach dem Muster `:368` (Elternkette `:132`), Eltern zuerst, dazu die Gegenprobe. Mit Nr. 56 verschmolzen |
| 9 | mittel | repo | 7, 2.3, Probe 14 | Kaskadenfalle der Bauform B nicht behandelt; „geordnete" Kindliste trifft nicht zu | **eingearbeitet**: neuer Absatz „Die Kaskadenfalle" in Kapitel 7, „geordnete" gestrichen, Verweis in 2.3, Probe 14 um das Speichern erweitert |
| 10 | mittel | repo | 1.4, 2.1 | Klassenumbenennungen gegenüber Umsetzungskonzept 3.3 stehen nirgends als Präzisierung | **eingearbeitet**: neue Zeile 6 in 1.4, Begründung in 2.1 |
| 11 | mittel | repo | 6.2, Probe 20 | `TrimmerRootDescriptor` als vorhanden vorausgesetzt | **eingearbeitet** (6.2 und Probe 20): heute keiner gesetzt, G4-8 misst einen Gerätebau, Frage U16 |
| 12 | mittel | repo | Probe 18 | Indexzeile fehlt, Wache wäre rot | **eingearbeitet, Teil gegenstandslos**: die Zeile steht bereits (`Dokumentation/LIESMICH.md:56`); Probe 18 nennt nur noch die Wache. Mit Nr. 57 verschmolzen. Siehe Abschnitt 1, Nr. 6 |
| 13 | mittel | formate/repo | 5.2 | Export schreibt `AirChangesPerHour` ohne Quellspalte | **eingearbeitet** (5.2): `← Luftwechsel_Infiltration`, `Luftwechsel_Nutzer` benannt weggelassen; Probe 1 um den Luftwechsel erweitert |
| 14 | niedrig | repo | 8.3 | `.gitattributes`-Zusage für `Importproben/` fehlt | **eingearbeitet** (8.3): `.gitattributes:87` benannt, „an `.gitattributes` ist nichts zu tun" |
| 15 | niedrig | repo | 2.5 | Zwei ungenaue Zeilenangaben | **eingearbeitet** (2.5): `:73-101` und `:17-20`/`:23-26`. Mit Nr. 63 verschmolzen; Entscheidung in Abschnitt 1, Nr. 2 und 3 |
| 16 | niedrig | repo | 2.1 | Fundstelle für `PruefStufe`/`PruefMeldung` fehlt | **eingearbeitet** (2.1): `SpeicherEngine/GanglinienPruefung.cs:55` und `:74` |
| 17 | niedrig | repo | 2.4 | Zuordnungsdialog liest sich als Bestand, existiert aber nicht | **eingearbeitet** (2.4): „der mit G4a entstehende … Vorschlag Umsetzungskonzept 3.3", samt `IfcZuordnungDaten.cs` und `IfcImportHuelle.cs` |
| 18 | hoch | formate | 0.1, 1.3, 10, D1 | „Pflichtbestandteil des Schemas" ist falsch | **eingearbeitet** an allen vier Stellen: schemaseitig nicht erzwungen, in allen vier Messdateien vorhanden. Entscheidung in Abschnitt 1, Nr. 10 |
| 19 | hoch | formate | 5.5, Probe 2 | Abstandsregel macht die Trennfläche im Mehrzonenfall unmöglich | **eingearbeitet** (5.5, Punkt 3): Zonen mit gemeinsamer Trennfläche werden aneinandergelegt; ist keine Anordnung ableitbar, wird G7b für das Mehrzonenmodell benannt abgelehnt. Probe 2 erweitert |
| 20 | hoch | formate | 6.6 gegen 6.2 | Round-Trip verspricht IFC2x3-Rückgabe, 6.2 verbietet das Schreiben | **eingearbeitet** (6.6, 6.2, 7.1, Probe 13, Kapitel 12): G7d nur für IFC4, alles andere benannt abgelehnt; `Schemastand` ist das Sperrkriterium. Entscheidung in Abschnitt 1, Nr. 1 |
| 21 | mittel | formate | 6.1, 0.1 | „Das 3D-Fenster bleibt leer" steht als Tatsache | **eingearbeitet** (6.1, 0.1, Probe 16): Vorbehalt „nach der Quellenlage zu erwarten", Probe 16 trägt die Aussage |
| 22 | mittel | formate | 1.2, 10/D1 | Archicad als belegte Quelle geführt | **eingearbeitet** (1.2 und Kapitel 10, Reihenfolge Nr. 1): aussichtsreichster, aber **ungemessener** Kandidat; Belegslücke Vectorworks |
| 23 | mittel | formate | 6.4, 6.3 | kWh ohne Entität; `IfcUnitAssignment` unvollständig | **eingearbeitet** (6.4 und 6.3): `IfcConversionBasedUnit` `KILOWATTHOUR` mit `ConversionFactor` gegen JOULE; vollständige Einheitenzuweisung; neue Probe 23 |
| 24 | mittel | formate | 6.4, Probe 12 | Schlüsselpfad deckt nur Sachobjekte ab | **eingearbeitet** (6.4): Rollenglied `#<Rolle>`, Hinweis auf die fehlende BCL-Fabrik für RFC 4122 Version 5; Probe 11 auf jede `IfcRoot`-Instanz erweitert |
| 25 | mittel | formate | 10 | Aufwände aus den Befunden übernommen, Umfang aber verschoben | **eingearbeitet** (10): G4c 17–28, G7a 9–14, G7d 6–11, Summen 45–77 und 62–105, dazu die Herkunftsfußnote. Mit Nr. 52 verschmolzen; Entscheidung in Abschnitt 1, Nr. 5 |
| 26 | mittel | formate | 5.6, Probe 3 | `Results` schreiben und fehlerfrei validieren ist nicht abgesichert | **eingearbeitet** (5.6 und Probe 3): zulässige `unit`-Werte je `resultsType` vor dem Bau festlegen; sonst entfällt der Block benannt |
| 27 | mittel | formate | 3.2, 3.4 | `CADModelAzimuth`-Aufaddieren steht als Regel, ist aber unbelegt | **eingearbeitet** (3.2, 3.4, 3.8): als Annahme gekennzeichnet, bis zur Gegenmessung Warnung statt stiller Rechnung; neue Probe 21 und Fehlerbild `IMP_GBXML_PROT_NORDDREHUNG` |
| 28 | mittel | formate | 3.6 | „Die IDREF-Integrität prüft das Schema selbst" — beim Import prüft niemand | **eingearbeitet** (3.6): der Leser löst selbst auf und meldet `IMP_GBXML_PROT_VERWEIS_LEER`; neues Fehlerbild in 3.8 |
| 29 | mittel | formate | 5.2 | Entfällt `Location`, entfallen auch Koordinaten und `CADModelAzimuth` | **eingearbeitet** (5.2): Folge benannt, Hinweis im Exportdialog, Vermerk in `Campus/Description`; Optionalität von `Location` als Messauftrag, sonst PLZ als Pflichteingabe |
| 30 | mittel | formate | 5.5 | Brutto- oder Nettowandfläche für die Grundrissableitung? | **eingearbeitet** (5.5 Punkt 2, dazu 3.4 und 3.6): **Bruttoflächen**, und der Bruttowert wird beim Import mitgeführt |
| 31 | mittel | formate | 6.3, 8.4 | Logisch-Kennzeichnung der Raumgrenzen fehlt | **eingearbeitet** (6.3 und 8.4): `EPOS_Bauteil.Raumgrenze = 'logisch'` und Satz im Beipackzettel |
| 32 | mittel | formate | 6.6, Kapitel 4, 7.1 | Sperre zählt nur `FailedEntity` | **eingearbeitet** (Kapitel 4 Nr. 3, 6.6 Nr. 2, 7.1, Probe 13): Summe beider Verlustkanäle, `ignoreTypes` nie gesetzt (Wächtertest) |
| 33 | mittel | formate | 6.6 gegen 2.3 | Der Hash beschafft die Originaldatei nicht | **eingearbeitet** (6.6 Nr. 1): der Anwender wählt die Datei erneut, EPOS vergleicht den SHA-256; Probe 13 erweitert |
| 34 | mittel | formate | 0.2 | „ohne eine einzige typisierte Eigenschaft" übertreibt | **eingearbeitet** (0.2): `object[] Items` mit `ItemsElementName` statt typisierter Eigenschaften. Mit Nr. 61 verschmolzen |
| 35 | mittel | formate | 1.3 | „Schema seit 2020 unverändert" vermengt zwei Daten | **eingearbeitet** (1.3): 8.01 ist byteweise 7.04, Beispieldateien von 2020, Werkzeugliste ungepflegt |
| 36 | mittel | formate | 8.2, D3 | „gegenstandslos" gilt nur für die Auslieferung, nicht für die Ablage im Repositorium | **eingearbeitet** (8.2 und D3): Unterfrage ausgeschrieben, zwei Wege benannt, LIESMICH-Zeile am Ablageort. Mit Nr. 58 verschmolzen |
| 37 | mittel | formate | 5.3, D10 | „genauso ehrlich wie die Quadergeometrie" überzieht | **eingearbeitet** (5.3 und D10): trifft U-Wert und Gesamtkapazität, **nicht** die Lage der Masse; Satz gehört wörtlich in `Construction/Description` |
| 38 | mittel | formate | 3.5, 3.8 | `Surface` ohne `AdjacentSpaceId` verschwindet still | **eingearbeitet** (3.5 und 3.8): Zeile auf `Shade` eingeschränkt, neuer Fall `IMP_GBXML_PROT_OHNE_NACHBAR` |
| 39 | niedrig | formate | 3.4 | Pfad `Construction/@constructionIdRef` falsch | **eingearbeitet** (3.4): `Surface/@constructionIdRef` |
| 40 | niedrig | formate | 0.1 | „von keinem Simulationswerkzeug" ist absoluter als der Beleg | **eingearbeitet** (0.1): geprüfte **dynamische** Werkzeuge namentlich; für Solar-Computer und EVEBI weder belegt noch widerlegt |
| 41 | niedrig | formate | 3.8 | Begründung der Versionsmeldung trägt nicht | **eingearbeitet** (3.8): `0.37` ist ein **gültiger** Wert; der Versionswert steuert nichts am Lesen |
| 42 | niedrig | formate | 7.2, 7.3 | Keine Regel für Kennungen über 64 Zeichen | **eingearbeitet** (5.4): kürzen auf 56 Zeichen plus acht Zeichen SHA-256-Präfix, Hinweis im Protokoll; Fehlerbild in 3.8, neue Probe 24 |
| 43 | niedrig | formate | 6.4, Probe 20 | IDS nur über einen Windows-Setup-Pfad verortet | **eingearbeitet** (6.4): Windows über `Source:`-Zeile (`Setup/EPOS-Plan.iss:330-331`), iOS als Bündelressource, Weitergabe nur über `MitSystemOeffnen` |
| 44 | niedrig | formate | 6.4, Probe 10 | `ValidationFlags` nicht genannt | **eingearbeitet** (6.4): Attributprüfung ausdrücklich eingeschaltet, Probe 10 weist es nach |
| 45 | niedrig | formate | 6.3/6.5 | Part21-Kodierung deutscher Klartexte ungeprüft | **eingearbeitet** (6.5): `\X2\…\X0\`-Escape benannt, neue Probe 22 (zeichenweiser Vergleich, bSI-Dienst) |
| 46 | hoch | regeln | 1.4, 7.3, D9 | `KATALOG` verschwindet aus dem `CHECK` | mit Nr. 3 verschmolzen — **eingearbeitet**; Reihenfolge der Werte siehe Abschnitt 1, Nr. 4 |
| 47 | hoch | regeln | 3.4 | `Bezeichnung` statt `Bezeichner` | mit Nr. 2 verschmolzen — **eingearbeitet** |
| 48 | hoch | regeln | 7.2 | Keine Spalte für die Paarung EPOS-Gebäude ↔ `IfcBuilding` | **eingearbeitet** (7.2): fünfte Spalte `ID_Gebaeude`, `CHECK` auf fünf Summanden, `Quelltyp` um `IfcBuilding`/`Building` erweitert, Begründung ergänzt; Probe 14 erweitert |
| 49 | hoch | regeln | 2.5 | `.xml` ist bereits auf `public.xml` abgebildet | mit Nr. 1 verschmolzen — **eingearbeitet** |
| 50 | hoch | regeln | 7.4 | `ID_MAP` existiert nicht | mit Nr. 4 verschmolzen — **eingearbeitet**; Mitziehen in den Nachbarpapieren siehe Abschnitt 4, Nr. 2 |
| 51 | hoch | regeln | 3.3 gegen 10 und 1.4 | X1…X4 stehen zugleich in G4c und in G6c; E7 wird still auf gbXML erweitert | **eingearbeitet**: **X4 in G4c, X1…X3 in G6c** (3.3, 2.4, Kapitel 10, Vorbedingungen), neue Zeile 7 in 1.4 und neue Frage **D16** |
| 52 | mittel | regeln | 10, 0.6 | G4c-Aufwand enthält die Persistenz nicht | mit Nr. 25 verschmolzen — **eingearbeitet**; Entscheidung in Abschnitt 1, Nr. 5 |
| 53 | mittel | regeln | 0.3 | „in keinem der drei Nachbarpapiere enthalten" trifft nicht zu | **eingearbeitet** (0.3): Anforderung steht im Mehrzonenkonzept, neu sind hier die Tabellen |
| 54 | mittel | regeln | 3.4, 3.7, 5.2, Probe 1 | Schichtfolge wird nicht umgekehrt; Export nennt die Schreibrichtung nicht | mit Nr. 6 verschmolzen — **eingearbeitet**, dazu die Schreibrichtung in 5.2 und das gesäte Testgebäude mit Innendämmung in Probe 1 |
| 55 | mittel | regeln | 7.4 | „S-A bis S-E" ist falsch, S-E gehört zu G1 | mit Nr. 7 verschmolzen — **eingearbeitet**: S-A bis S-D, S-E mit G1, Nummernfolge unberührt |
| 56 | mittel | regeln | 7.4 | Reduzierskript und Auslieferungsvorlage fehlen | mit Nr. 8 verschmolzen — **eingearbeitet**; Prüfregel „beide Tabellen leer" in der Auslieferungsvorlage, Probe 18 erweitert |
| 57 | mittel | regeln | Probe 18, Index | Indexzeile fehlt | mit Nr. 12 verschmolzen — **eingearbeitet, Teil gegenstandslos** |
| 58 | mittel | regeln | 8.2 gegen 8.3, D3 | Ungleichbehandlung XSD / Beispieldateien unbegründet | mit Nr. 36 verschmolzen — **eingearbeitet** |
| 59 | niedrig | regeln | ganze Datei | Datei mit LF geschrieben | **gegenstandslos** — nachgemessen 1 187/1 187 CRLF, kein BOM. Siehe Abschnitt 1, Nr. 7 |
| 60 | niedrig | regeln | Probe 3, 8.2 | Widerspruch 8.01-Kopie gegen `version="6.01"` | **eingearbeitet** (8.2 und Probe 3): „ihr `versionEnum` endet bei 6.01, weil 8.01 byteweise 7.04 ist" an beiden Stellen |
| 61 | niedrig | regeln | 0.2 | Werkzeugbefunde ohne Entscheidungswert auf der Entscheidungsseite | mit Nr. 34 verschmolzen — **eingearbeitet**: Compilerdiagnosen gestrichen, Verweis auf ADR-004 |
| 62 | niedrig | regeln | Kapitel 11 | Fünfzehn Fragen ohne Rang und Zeitpunkt | **eingearbeitet**: Kapitel 11 in **11.1 „Jetzt zu entscheiden"** (D1, D2, D4, D5, D6, D11, D16 mit Spalte „Wann spätestens") und **11.2 „Technische Festlegungen zur Kenntnis"** (D3, D7–D10, D12–D15) geteilt; Einleitungssatz nennt die drei tragenden Antworten |
| 63 | niedrig | regeln | 2.1, 2.5 | Drei ungenaue Fundstellen | mit Nr. 15 verschmolzen — **eingearbeitet**; `CsvExportClass.cs` trägt jetzt den Projektpfad |
| 64 | niedrig | regeln | 6.2 gegen 6.6 | Liest sich als Widerspruch | mit Nr. 20 verschmolzen — **eingearbeitet**: 6.2 verweist auf 6.6, und 6.6 begrenzt den Round-Trip auf IFC4 |

---

## 3. Was sich inhaltlich am Papier geändert hat

Neben den Richtigstellungen tragen fünf Änderungen echte Folgen für die Umsetzung:

1. **Der Round-Trip G7d wird auf IFC4 begrenzt** (Nr. 20). Eine 2x3- oder 4.3-Quelle wird benannt
   abgelehnt; `Tab_Importquelle.Schemastand` ist das Kriterium. Wer den zweiten Schreibpfad will,
   braucht eine Erweiterung von **E3**.
2. **Die Schichtfolge wird beim Schreiben umgekehrt** (Nr. 6/54). Ohne die Umkehr säße die
   Speichermasse auf der falschen Seite des Aufbaus, und Probe 1 fände es nicht, weil Export und
   Import symmetrisch falsch wären. Das gesäte Testgebäude trägt deshalb jetzt eine Innendämmung.
3. **Die Zonenregeln werden auf zwei Stufen verteilt** (Nr. 51): X4 in G4c, X1…X3 in G6c. Damit
   erweitert sich **E7** auf ein zweites Format — deshalb die neue Frage **D16** und die neue
   Zeile 7 in 1.4.
4. **Die Persistenz kostet Geld, und das steht jetzt drin** (Nr. 25/52): G4c 17–28 PT statt 14–22,
   G7d 6–11 statt 8–14, G7a 9–14 statt 8–12; Summen 45–77 (G7) und 62–105 (gesamt), mit einer
   Fußnote, welche Zahl aus welchem Befund stammt.
5. **Die Kaskadenfalle** (Nr. 9) und die **fünfte Spalte in `Tab_Importzuordnung`** (Nr. 48) sind
   beide Datenmodellfragen, die vor dem Schemaschritt S-F zu entscheiden sind — die erste, weil
   `ON DELETE CASCADE` sonst bei **jedem** Speichern über den Wizard-Weg zuschlägt; die zweite, weil
   der Round-Trip die Gebäudeentität sonst nicht wiederfindet.

Dazu vier neue Proben (21 Azimutdrehung gbXML, 22 Umlautprobe IFC, 23 Einheitenzuweisung IFC,
24 überlange Kennung) und fünf neue Fehlerbilder in 3.8.

---

## 4. Außerhalb dieses Auftrags — an den Orchestrator

Diese drei Punkte liegen in Dateien, die dieser Auftrag nicht anfassen durfte:

1. **Indexzeile für das Mehrzonenkonzept.** `Dokumentation/LIESMICH.md` führt
   `Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md` bereits (`:56`), **nicht** aber
   `Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`. `DokumentationLinkWacheTests` ist rot, solange die
   Zeile fehlt.
2. **`ID_MAP` → `FK_MAP` in den Nachbarpapieren.** Derselbe nicht existierende Bezeichner steht in
   `Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md` (Schritt S-D) und in Befund Q, Abschnitt 6. Richtig
   sind `FK_MAP` (`EPOS.Kern/Controller/ProjektDuplizierenCtrl.cs:85`), `FK_OVERRIDE` (`:140`) und
   `KINDER` (`:152`).
3. **Aufwandszahlen nachziehen.** `Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`
   (Zeilen um `:1419` und `:1452`) und das Mehrzonenkonzept führen für den gbXML-Import weiterhin
   14–22 PT; nach der Vorziehung der Persistenz sind es 17–28 PT. Ebenso ist der Zuwachs von **G6c**
   um X1…X3 im Mehrzonenkonzept (Kapitel 9, heute 10–16 PT) zu benennen.

---

## 5. Form

Beide Dateien sind **UTF-8 ohne BOM** mit **CRLF** in jeder Zeile, mit ausschließlich
repo-relativen Verweisen. Alle dreizehn relativen Verweise des Papiers wurden nach dem Umbau einzeln
gegen den Arbeitsbaum geprüft und lösen auf. Normzahlen der VDI 6007 stehen nicht im Papier; aus
VDI 6020:2022 ist nach **E6** nichts herangezogen. Kein Commit, kein Push, kein Build; die
Testdatenbank wurde nicht angefasst.
