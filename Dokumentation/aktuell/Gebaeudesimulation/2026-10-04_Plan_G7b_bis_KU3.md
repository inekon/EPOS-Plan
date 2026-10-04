# Plan der Wellen G7b bis KU3 — Sitzung Gebäudesimulation

**Stand 04.10.2026, nach den Entscheiden E67, E68 (Kältespeicher ja) und E69 (Gegenüber des IFC-Exports: Revit, Archicad, HiCAD; Zweck Round-Trip).** Dieses Papier plant die Stufen, die nach E67 vor KU3
liegen, und KU3 selbst: Reihenfolge, Zuschnitt in Wellen und Agentenaufträge, Abnahme, Rückfragen an
den Stufengrenzen und der geschätzte Verbrauch. Es ist ein Arbeitsplan, kein Konzept: Fachliche
Festlegungen stehen in den Konzepten, auf die jede Zeile verweist. Erledigte Wellen wandern mit
Statuszeile und Protokoll aus diesem Plan heraus; ist KU3 abgenommen, geht das Papier nach
`ueberholt/`.

Quellen: [Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) Kapitel 5, 6, 9,
10 und 14; [Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 4.6, 5.3, 5.4, 9.2 und
11; [Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.7 und 7;
[Statusdatei](../Status_Gebaeudesimulation_VDI6007.md) Abschnitte 1 und 2.


## 1 Reihenfolge und Maßstab

| Nr. | Welle | Inhalt in einem Satz | PT laut Konzept | Verbrauch (Schätzung) | Rückfrage vorher |
|---|---|---|---|---|---|
| 1 | **G7b** (umgesetzt #709) | gbXML Stufe 2 und 3D-Körperansicht | 7–12 (+4–7 Ansicht) | 4–5 Punkte | keine (E66) |
| 2 | **G7c** (umgesetzt #710) | IFC-Export S1, semantisch | 12–20 | 7–9 Punkte | keine (D6: semantisch zuerst) |
| 3 | **G7d** (umgesetzt #711) | Round-Trip-Anreicherung fremder IFC4-Dateien — die tragende Stufe nach E69 | 6–11 | 4–5 Punkte | keine (E69) |
| 4 | **G7e** (umgesetzt #712) | Schematische Körper im IFC | 8–15 | 5–7 Punkte | keine (E69: Empfänger sind Betrachter); Prüfbilder aus Revit, Archicad, HiCAD liegen beim Anwender |
| 5 | **KU3** | Kältemaschine mit Rückkühlung, freie Kühlung über die Quelle, Kühlung je Zone, Export, Kältespeicher (E68: ja) | 20–31 | 15–19 Punkte | Katalogsaat der Kältemaschinen |
| — | **G7f** — nach Zuruf | Raumkörper aus der IFC-Datei in der Körperansicht (E71; [Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 15): Kern-Leser ohne Geometriekern, Umschalter „Dateikörper \| Exportmodell“; Vorschlag nach KU3-4b, vor AK2 | 4–6 | 4–5 Punkte | Reihenfolge und Dreiecksgrenze (Vorschlag 300 000 je Gebäude) |

Maßstab: EV1 (3–4 PT) kostete rund 3 Punkte des Wochenlimits bei rund 230 Werkzeugaufrufen. Ein
Personentag des Konzepts entspricht damit grob 0,8–1 Punkt. Die Spanne G7b bis KU3 liegt bei
**32–46 Punkten**, verteilt auf drei bis vier Wochenlimits. Jede Welle wird vor dem Start mit ihrer
Schätzung genannt; der Anwender nennt das verbleibende Limit.

Arbeitsweise je Welle unverändert: Worktree je Agent (`opus-umsetzung` für Code und Tests,
`sonnet-mechanik` für Papiere, Wiki und Statuszeilen, `haiku-pruefung` für Zählungen), Agenten
committen und pushen nicht, höchstens rund 150 Werkzeugaufrufe je Auftrag; danach Merge → Gate →
Statuszeile und Protokoll → Push → CI-Vermerk. Kein macOS-, iOS- oder Setup-Lauf ohne Freigabe.


## 2 G7b — gbXML Stufe 2 und Körperansicht

Umgesetzt (#709).

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Kern | Opus, `g7b-kern` | Aneinanderlegen von Zonen mit gemeinsamer Trennfläche, M13-Paare an den Kanten, `PlanarGeometry/PolyLoop`, `ShellGeometry`, `Results`, Kennzeichnung „schematisch“ in der Datei, Freigabe des gbXML-Exports | Proben 2, 3, 12, 25, 27; Rundlauf |
| Ansicht | Opus, `g7b-ansicht` | three.js 0.186.1 (MIT) lokal unter `EPOS.UI/wwwroot/three/`, Reiter „Körper“, Kennzeichen „schematisch“ am Bild, Herkunft je Zone, Lizenzblock mit Wache, Hilfeanker des Exportdialogs | Probe 26 (bunit), Schale 0 Fehler, Lizenzwache |
| Papiere | Sonnet | Wiki „Gebäudeimport“ (Abschnitt „Gebäudedaten ausgeben“ aus Protokoll G7a Abschnitt 8 und Körperansicht), Datenaustauschkonzept 5.5 und 14, Mehrzonenkonzept 6.7, Protokoll, Statuszeile, Logbuch-Satz | Wachen grün, Gegenlese 0 Treffer |

Offen beim Anwender nach G7b: Sichtprobe der Körperansicht unter Windows; Probe 20 (macOS-Lauf) nur
auf Zuruf; Logbuch-Version.


## 3 G7c — IFC-Export S1

Umgesetzt (#710).

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Kern 1 | Opus, `g7c-kern` | `IfcSchreiber` mit `MemoryModel`: Einheiten von Hand (METRE … RADIAN, KILOWATTHOUR), `IfcSite`, `IfcBuilding`, `IfcSpace` mit Psets und Qtos, `IfcZone` nur bei mehreren Zonen, Bauteile, Raumgrenzen 2. Stufe ohne Geometrie, Materialschichten, `EPOS_*`-Sätze, Produktausweis E10, deterministische `GlobalId`, Validator, Formatwahl im Profil | Proben 10, 11, 12, 22, 23; Rundlauf über `IfcLeser` |
| Kern 2 | Opus | `EPOS_Ergebnis` vollständig (Heizwärmebedarf, Heizlast, Kältebedarf und Kältelast nach Kühlkonzept 9.2 mit explizitem `Unit` und dem Hinweis „sensibel, ohne Entfeuchtung“), Beipackzettel als Text je Export (Ressourcen beider Sprachen), IDS-Datei `EPOS_Export.ids` mit der Exportzusage, Wache IDS ↔ geschriebene Sätze | Probe 23 erweitert; IDS-Wache |
| Schalen und Oberfläche | Opus | Formatwahl „gbXML / IFC“ im Exportdialog, Hülle, Dateiendung und Teilen auf iOS, IDS in der Auslieferung: Windows `{app}\Vorlage\EPOS_Export.ids` (`Setup/EPOS-Plan.iss`), iOS als Bündelressource mit `MitSystemOeffnen` auf Anforderung; Lizenzhinweise unverändert (xBIM steht) | bunit des Dialogs, Schale 0 Fehler, `AuslieferungsvorlagenWacheTests` |
| Papiere | Sonnet | Wiki „Gebäudeimport“ (IFC-Export), Datenaustauschkonzept 6.3–6.5 und 10, Softwarearchitektur, Protokoll, Statuszeile, Logbuch-Satz | Wachen grün |

Probe 15 (bSI-Validierungsdienst) und Probe 16 (Betrachter-Prüfmatrix) laufen von Hand beim
Anwender; die Sitzung liefert dafür eine Beispieldatei aus dem Referenzprojekt 1052 (drei Zonen) im
Scratchpad und bittet um das Protokoll. Erst Probe 16 trägt die Aussage in Konzept 6.1.


## 4 G7d — Round-Trip-Anreicherung

Umgesetzt (#711; Protokoll `../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-04_G7d_IFC_Round-Trip.md`).

Vorbedingungen: G7c abgenommen. Das Gegenüber ist mit E69 benannt: Revit, Archicad und HiCAD; der
Zweck ist die angereicherte Rückgabe der Architektendatei — G7d ist damit die tragende Stufe des
IFC-Exports. Quelldateien müssen in IFC4 vorliegen (Revit und Archicad schreiben IFC4; eine Datei in
IFC2x3 oder IFC4.3 wird benannt abgelehnt, mit Angebot einer eigenen Datei nach G7c).

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Kern | Opus | Wiederfinden über `Tab_Importquelle.Hash` nach erneuter Dateiwahl, Sperren: Schemastand ≠ IFC4, Entitätenverlust im Leseprotokoll (beide Kanäle), Hash ungleich — je benannte Verweigerung mit Angebot einer eigenen Datei nach G7c; Ergänzen statt Doppeln (`Pset_WallCommon.ThermalTransmittance` ersetzen), neue Entitäten mit eigener `GlobalId`, `FILE_DESCRIPTION`-Vermerk und eigene `IfcApplication`, `OriginatingSystem` bleibt | Probe 13 (drei Sperrfälle), Wächter `SkipTypes` nie gesetzt, Rundlauf an einer gesäten Testdatei (8.3: selbst erzeugt, kein Download) |
| Oberfläche | Opus | Exportdialog: Wahl „eigene Datei / Originaldatei anreichern“, Hinweis mit Bestätigung (D11), immer neuer Dateiname, Beipackzettel | bunit, Schale 0 Fehler |
| Papiere | Sonnet | Wiki, Konzept 6.6, Protokoll, Statuszeile, Logbuch | Wachen grün |


## 5 G7e — schematische Körper im IFC

Umgesetzt (#712; Protokoll `../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-04_G7e_IFC_Koerper.md`). Der nächste Schritt ist KU3-1 (Schema und Katalog der Kältemaschine, Abschnitt 6).

Vorbedingungen: G7b und G7d abgenommen. Nach E69 sind die Empfänger Betrachter (Revit, Archicad,
HiCAD); ein eigener Export ohne Architektendatei bliebe dort leer, deshalb wird G7e gebaut.

| Teil | Agent | Inhalt | Abnahme |
|---|---|---|---|
| Kern | Opus | Aus dem Zonengeometrie-Modell je Zone `IfcRectangleProfileDef` → `IfcExtrudedAreaSolid` → `IfcShapeRepresentation` → `IfcProductDefinitionShape`; Platte je Bauteil, Fenster als kleinere Platte vor der Wand; Placement-Kette Project → Site → Building → Element, Azimut nur als Drehung des Placements, `TrueNorth` Vorgabe; Kennzeichnung im Projektnamen, `FILE_DESCRIPTION`, je Element im `Description` und als `IfcAnnotation` | Validator grün trotz Placement-Pflicht, Probe 27 (Körper gegen das Modell), Determinismus |
| Papiere | Sonnet | Wiki, Konzept 6.7 und 14, Protokoll, Statuszeile, Logbuch | Wachen grün |

Prüfbilder aus mindestens zwei Betrachtern (Probe 16) liefert der Anwender; die Sitzung legt die
Beispieldatei bereit.


## 6 KU3 — das Umfeld der Kälte

Vorbedingungen: KU2 im Feld (liegt beim Anwender), G6 für die Zonen (erfüllt), G7 für den Export
(mit G7c). Der Kühlsollwert Nacht ist nicht mehr Teil von KU3: Er wurde mit KP1 als Matrixzelle
Kühlen/Nacht gebaut (Kühlkonzept, Kopfvermerk).

**Rückfragen vor dem Start:**

- **K7 Kältespeicher: entschieden (E68, 04.10.2026): ja** — mit der Kältemaschine in Welle KU3-5
  (3–5 PT: Pufferverwendung `VERWENDUNG_KAELTE`, Klassensatz, Warnkriterien, `Entladung_Kuehlung`
  aus `KU-S4` füllt sich).
- **Katalogsaat:** Die Auslieferung braucht Kältemaschinen im Stammkatalog. Neutral benannte
  Beispielgeräte mit runden Werten (etwa „Kältemaschine 50 kW luftgekühlt“) legt die Sitzung an;
  Herstellerdaten pflegt der Anwender später über den Katalogdialog.

**Wellen von KU3** (jede mit eigenem Gate; ergebnisneutral, bis die Welle 4 die Basis wechselt):

| Welle | Inhalt | Schema | PT | Abnahme |
|---|---|---|---|---|
| **KU3-1 Schema und Katalog** (umgesetzt, #713) | `Tab_Kaeltemaschine(_STAMM)` (Nennkälteleistung, Nenn-EER, Kältemittel als Text, Rückkühlart, Mindestteillast), `Tab_Kenndaten_Kaeltemaschine(_STAMM)` als Kennlinie über Rückkühl- und Kaltwassertemperatur, `Z_*`-Zuordnung, Anlagenart, Katalogregister, Projektkopien, Auslieferungsvorlage, Katalogdialog, Katalogfilterprofil, KI-Dialogkatalogeintrag | ein Schemaschritt (Nummer vor dem Bau anmelden) | 4–6 | Schemawache, Katalogrundlauf, Auslieferungsvorlage grün, Referenzlauf byte-gleich |
| **KU3-2 Rechenweg** (umgesetzt, #714) | Rechenklasse unter `EPOS.Kern/Allgemein/Simulation/`, Teillastkennlinie, Rückkühlmodell (Trocken-/Nasskühler, Hilfsstrom), Einordnung in `Kaeltekaskade` und `DeckungKanalKaelte`, freie Kühlung über die Quelle als Betriebsfall des Erzeugers mit Grenze der Quellentemperatur (K8), Kältestrom in die Strombilanz, Komponentenkennung in `Tab_KostenKomponente`, Endenergiezeile, Emissionen | keiner | 5–7 | Rechenproben ohne Datenbank (Kühlkonzept 10.2), Datenbankfälle (10.3), Referenzlauf byte-gleich, weil kein Referenzprojekt eine Kältemaschine führt |
| **KU3-3 Kühlung je Zone** (umgesetzt, #715) | `Tab_Zone.Kuehl_*` werden gelesen (NULL = Wert des Gebäudes), Kühlbedarf je Zone mit eigener Grenze `Kuehlleistung_Max`, gleichzeitiges Heizen und Kühlen ausgewiesen, nicht saldiert (F-K15, K6), Zonenzeilen im Bedarfsdialog, Ergebnisreihen je Zone, Export der Kälteseite in IFC `EPOS_Ergebnis` (`Kaeltebedarf`, `Kaeltelast`) und gbXML `Results` `CoolingLoad` | keiner | 3–5 | `ZonenReferenzprojektWacheTests`, Rundlauf der Exporte, Referenzlauf: 1052 steht nicht in der CI-Auswahl, 1017 und 1047 (Kühlung, Einzone) bleiben byte-gleich |
| **KU3-4a Anlage, Rechenweg, Bericht** (umgesetzt, #716) | Typ 13, Anlagenzeile mit Anzahl, Rechenweg je Anlagenzeile, Bericht (Erzeugerabschnitt, Kennzahlen F-K11), Wirtschaftlichkeit (Investition, Nutzungsdauer), Ergebnis je Maschine | Schemaschritt 183: Typeintrag, Verweis, Kostenkomponente, Ergebnistabelle | 2–3 | Referenzlauf byte-gleich |
| **KU3-4c Erzeugerdialog** (umgesetzt, #717) | Erzeugerdialog der Kältemaschine als Razor-Komponente über `KaeltemaschineAnlageCtrl` | keiner | 2–3 | Dialogtests, Sichtabnahme beim Anwender |
| **KU3-4d Kältestromabrechnung und Stempeltrigger** (umgesetzt, #721) | Kühlträger mit eigenem Zähler in `Kaeltestromabrechnung` für die Kältemaschine (Anteile, Grund- und Leistungspreis, Emissionen je Zähler, Szenario-Mengen); Kostenstempel-Trigger an `Tab_Energieanlagen` um `Kaeltemaschine_Anzahl` erweitern; Berichtsabschnitt und Füllstandsganglinie des Kältespeichers (aus KU3-5); getrennte Projektkopien bei zwei Anlagenzeilen desselben Katalogsatzes | ein Schemaschritt (DROP/CREATE des Triggers) | 2–3 | Datenbankfälle, Referenzlauf byte-gleich |
| **KU3-4b Referenzprojekt, Basis** (erst mit Testdatenbank 181) | neues Referenzprojekt mit Kältemaschine als Kopie von 1017, Einfrierregel „gesäte Kältemaschinendaten“, Basiswechsel R36 (R-Nummer vor dem Bau anmelden), Wiki und Logbuch | keiner | 1–2 | Vergleich der 18 Projekte plus das neue, Basis neu eingefroren |
| **KU3-5 Kältespeicher** (E68; umgesetzt, #718) | `VERWENDUNG_KAELTE` am Puffer, Lade- und Entladeweg im Kältekreis, `Entladung_Kuehlung`, Warnkriterien, Dialogtext „wird gerechnet“ | ein Schemaschritt, falls eine Spalte nötig | 3–5 | Proben, Referenzlauf, Basis nur, wenn das Referenzprojekt den Speicher bekommt |

Verbrauch KU3: rund 15–19 Punkte (mit Kältespeicher, E68); je Welle zwei bis drei Opus-Aufträge
und ein Sonnet-Auftrag.


## 7 Was zwischen den Wellen beim Anwender liegt

| Wann | Was |
|---|---|
| nach G7b (erledigt bis auf Sichtprobe, #709) | Sichtprobe Körperansicht unter Windows; Logbuch-Version; Wiki-Upload gebündelt |
| vor G7d | erledigt mit E69 (Revit, Archicad, HiCAD; Round-Trip) |
| nach G7d | Probe 16 mit einer angereicherten Architektendatei (Datei wählen, anreichern, in Revit, Archicad und HiCAD öffnen), Probe 15 (bSI) der angereicherten Datei, Sichtabnahme des Exportdialogs; Logbuch-Version |
| nach G7c | Probe 15 (bSI-Validierungsdienst) und Probe 16 (Betrachter-Prüfmatrix in Revit, Archicad und HiCAD) mit der bereitgestellten Datei |
| vor G7e | erledigt mit E69 |
| nach G7e | Probe 16 mit Prüfbildern aus Revit, Archicad und HiCAD der gesendeten Datei, Probe 15 (bSI), Sichtabnahme; Logbuch-Version |
| vor KU3 | Freigabe der neutralen Katalogsaat (K7 ist mit E68 entschieden: ja) |
| nach KU3-1 (#713) | Testdatenbank 181 pushen, danach 182 aus dem Bundle der Orchestrierung; Sichtabnahme der Kältemaschinen-Verwaltung unter Windows; iOS-Bau nur auf Zuruf |
| nach KU3-2 (#714) | Testdatenbank 181 pushen; Entscheid, ob die freie Kühlung über die Wärmequelle der Wärmepumpe (Sole) mit KU3-5 gebaut wird; iOS-Bau nur auf Zuruf |
| nach KU3-5 (#718) | Sichtabnahme des Speicherdialogs (Nutzung Kälte); Entscheid zur freien Kühlung über die Sole (mit KU3-5 nicht gebaut) |
| nach KU3-3 (#715) | Sichtabnahme Zonendialog (Kühlgruppe) und Bedarfsdialog unter Windows; Testdatenbank 181 pushen (KU3-4b wartet darauf); iOS-Bau nur auf Zuruf |
| nach KU3-4c (#717) | Sichtabnahme des Dialogs „Kältemaschinen“ unter Windows; **Q27 entschieden (E74, 04.10.2026):** keine Startseitenkachel, kein Assistentenschritt — der Knopf „Kältemaschinen…“ im Reiter „Energieerzeuger“ bleibt der Einstieg |
| nach KU3-4a (#716) | Bundle mit Testdatenbank 182/183 einspielen und pushen (LFS; KU3-4b wartet darauf); Sichtabnahme Kostenvorlagen und Bericht; iOS-Bau nur auf Zuruf |
| laufend | AK1z (eigene Sitzung, Bundle-Weg für Testdatenbank 181 und Basis R35); die Reihenfolge nach KU3: AK2, AK3, GA (E67) |
