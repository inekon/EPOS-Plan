# Gegenlesen des Umsetzungskonzepts Gebäudesimulation VDI 6007 (15.09.2026)

**Papier:** [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
— gegengelesen als Rev. 1 (1 341 Zeilen), nach Einarbeitung **Rev. 2** (1 546 Zeilen). Eine
unabhängige Nachprüfung der Einarbeitung hat sieben kleine Unstimmigkeiten gefunden; sie sind
nachgebessert (Rev. 2, 1 547 Zeilen) und stehen unten unter **Nachlese**.

Drei Leser haben das Papier unabhängig voneinander geprüft, jeder aus einem eigenen Blickwinkel:

| Leser | Blickwinkel | Fragte |
|---|---|---|
| 1 | **Repositorium** | Stimmt jede Datei-, Zeilen- und Namensangabe gegen den Arbeitsbaum? Greifen die genannten Wächter wirklich so? |
| 2 | **Entscheide und Hausregeln** | Trägt jede Aussage die Entscheide E1–E10 und die Regeln der `CLAUDE.md`-Dateien — Einheiten, Schreibwege, Hilfe, Wiki, Einfrieren? |
| 3 | **IFC-Technik** | Hält der Importentwurf gegen Schema, xBIM-API und die Praxis der Autorensysteme? |

Die drei Rohbefunde wurden zu **einer** Korrekturliste zusammengeführt (58 Einträge: 15 hoch,
23 mittel, 18 niedrig; dazu 2 Stil-Einträge). Vier Widersprüche zwischen den Lesern wurden dabei
am Quelltext geklärt: die Bauweise-Stufen (Hinweg `BauweiseAusBauart` 20/50/100 gegen Rückweg
`BauartAusBauweise` 30/75), die Schwere der Indexzeile, die Spaltenzahl des Schemaschritts und die
Zeilenangaben zu `SolardatenCtrl`. Doppelbefunde wurden entdoppelt (H5, M4, N3, N4; M21 ging in den
Ersatztext von H12, N18 wurde aus H2 abgetrennt).

**Der Orchestrator hat vier eigene Korrekturen beigesteuert** (O1–O4), verbindlich wie die Einträge
der Schwere hoch: die Folgen des Entscheids **E9** für die Stufenordnung (gbXML-Import ist Pflicht
in G4, die Exporte sind G7), der Produktausweis nach **E10** (Druckrundung), die vollständige
Entscheidliste E1–E10 und die Auflage aus Befund S, die Zuordnung des Imports zu persistieren.

---

## Die Einträge

| Nr. | Schwere | Stelle | Befund | Erledigung |
|---|---|---|---|---|
| H1 | hoch | 1.4, 1.9, Kap. 4 | Der Wächtereintrag ohne Unterordner macht `EinheitenWacheTests` rot: `Simulationsdateien()` prüft `Simulation` + Listeneintrag mit `File.Exists` | eingearbeitet — Eintrag lautet nun `Gebaeude/GebaeudeModellErgebnis.cs`, an allen drei Stellen |
| H2 | hoch | 3.4, Zeile `Bauweise` | Papier verwechselt Hin- und Rückweg (Schwellen 30/75 statt Stufen 20/50/100), und `BauweiseNachfuehren()` überschreibt jeden freien Wert | eingearbeitet — Schwellen richtiggestellt, Editorfalle benannt, freier Zahlenweg als Punkt in M-e (2.11) |
| H3 | hoch | 2.5 | Die Kernprobe gegen `SpezWaermeverlusteC` ist so nicht führbar (Lüftungsanteil, Faktor 100, zwei Bezugsflächen) | eingearbeitet — Probe auf Transmission + Wärmebrücken eingegrenzt, Faktor und Bezugsfläche benannt |
| H4 | hoch | Kap. 0 Punkt 1, 1.1, 1.5 | Die Verzweigung an `:581` allein trennt die Modelle nicht — `Bewohner_und_Flaeche_berechnen` ruft `:647` selbst | eingearbeitet — zweite Verzweigungsstelle an drei Stellen benannt, Datenbankfall als Gegenprobe |
| H5 | hoch | Kap. 4, Schlussabsatz | Indexzeilen, Statuszeile je Stufe und Wächtereintrag sind drei Handgriffe, nicht zwei | eingearbeitet, **abweichend**: Die Indexzeile für dieses Papier steht inzwischen (`Dokumentation/LIESMICH.md:55`, die Befunde A–S auf `:56-74`), die Wache ist deswegen nicht mehr rot — der Absatz nennt die Regel statt eines heutigen Fehlers |
| H6 | hoch | 1.4, 1.8 | `KuehlenergieKwh`/`JahresheizwaermeKwh` reißen den Einheitenwächter (Regel 2: Jahressummen, die den Kern verlassen, führen MWh) | eingearbeitet — Kennzahlen auf `…Mwh`, Umrechnung im Kern festgeschrieben, Skalarliste des Exports nachgezogen |
| H7 | hoch | Kap. 4, Zeile G1+G2 | Der von N1.9 zur G1-Pflicht erklärte Strahlungsnachweis (VDI 2078 Testbeispiel 7.1/7.2) fehlt in der Abnahme | eingearbeitet |
| H8 | hoch | Kap. 4, Zeilen M3 und M4 | `SqlDialektPruefer` fehlt in beiden Abnahmen, obwohl M3 den ersten `DROP`/`CREATE VIEW` und M4 neue Spaltenlisten bringt | eingearbeitet |
| H9 | hoch | Kap. 4, Zeile G1+G2 | Der Rückweg-Test schriebe `Gebaeude_Modell` in die eingefrorene Testdatenbank | eingearbeitet — Lauf auf gitignorierter Arbeitskopie, eigener Modus des Referenzlaufs |
| H10 | hoch | 3.1 | Der genannte `Speichern`-Delegat trifft `Tab_Gebaeude_STAMM`, nicht die Projektzeile | eingearbeitet — eigener Delegat `UebernehmenInsProjekt`, der Katalogweg als zweiter, ausdrücklich zu wählender Weg |
| H11 | hoch | 3.4 | Mengensätze heißen als Instanz `BaseQuantities`; `Qto_…` ist der Name der Vorlage | eingearbeitet — Vorspann vor die Abbildungstabelle, Messung an der Importprobe |
| H12 | hoch | 3.4 | `RelatingPropertyDefinition` ist ein SELECT, und Eigenschaften am **Typ** werden nicht gelesen | eingearbeitet (trägt M21 mit) |
| H13 | hoch | 3.4 | Die Außennormale ist aus dem Placement nicht zu gewinnen — senkrecht zur Wandachse liegen zwei Richtungen | eingearbeitet — Seite über `IIfcRelSpaceBoundary.RelatingSpace`, sonst `IMP_IFC_PROT_SEITE_UNBESTIMMT` |
| H14 | hoch | 3.3 | Plattformfreie Hülle ohne plattformfreien Empfänger: der Schreibweg liegt heute in der Schale | eingearbeitet, **abweichend**: kein eigener Teil „G4-0". Der Umzug ist bereits **M-k** des Dialogumbaus (2.8, 2.11) und damit mit G1+G2 erledigt, bevor G4a beginnt; ein zweiter Teil hätte dieselbe Arbeit ein zweites Mal veranschlagt. Die Vorbedingung und die benannte Ablehnung auf iOS stehen in 3.3 und 3.8 |
| H15 | hoch | 3.4, Einheiten | Der Längenfaktor darf für `AREAUNIT`/`VOLUMEUNIT` nicht quadriert werden | eingearbeitet — eigener Prefix gilt, Ableitung nur bei ganz fehlendem Typ, mit Meldung |
| M1 | mittel | 1.5 | Falscher Beleg für die Skalierung E8: `:435` ist der Rückgabewert von `TaeglHeizlastWG`, keine Nachmultiplikation | eingearbeitet — samt der Folge, dass das Stundenmodell die Verhältnisrechnung selbst führen muss |
| M2 | mittel | 1.9 | `CREATE VIEW` ohne `DROP` scheitert auf der Testkopie und verschwindet im `catch` | eingearbeitet — Zweierfolge `SQL_VIEW_DROP` + `SQL_VIEW_NEU` |
| M3 | mittel | 3.4 | Zwei Azimutkonventionen im selben Papier (IFC gegen Süd-Konvention des Kerns) | eingearbeitet — Umrechnung `az_EPOS = az_IFC − 180` samt Unit-Test |
| M4 | mittel | 1.4, 2.7 | `GebaeudeBedarfCtrl` hat keine Naht für das erzwungene Modell | eingearbeitet — vierter Parameter `modellErzwungen`, sechs neue Kennzahlen, beides in 1.4 und im Merge G1+G2 |
| M5 | mittel | 1.2 | Der Vergleich in 8.2 zeigt Modell- **und** Himmelsmodellwechsel in einer Zahl | eingearbeitet — Aufteilung wird in G1 einmal gemessen und im Protokoll ausgewiesen |
| M6 | mittel | 1.6, 1.10 | `002_views.sql` hat zwei Leser (`baue_leere_db.py`, `Reduziere-Testdatenbank.probe.py`) | eingearbeitet an beiden Stellen |
| M7 | mittel | 1.2 | Der von E5/N1.10 verlangte Modellvergleich Aydinli/Krochmann gegen Hay-Davies fehlt | eingearbeitet — als dritter G1-Nachweis |
| M8 | mittel | 1.4 | Prüfmodus nach Konzept 10.4 (2) ist nirgends vorgesehen | eingearbeitet — `Pruefmodus`-Schalter am `GebaeudeModellEingang` |
| M9 | mittel | 1.6, 2.5 | Keine Spalte für die Kellertemperatur (N1.3 ersetzt den Faktor 0,5 durch θ_NR,eq) | eingearbeitet — Spalte in Schritt 77, Feld neben der Randbedingung, Aufnahme in die vierte Einfrierregel und ins DTO. **Nachgebessert:** die Aufwandszeile **M-b** in 2.11 war als einzige Stelle bei „11 Felder" stehen geblieben und nennt jetzt 12 |
| M10 | mittel | Kap. 4, Zeile M3 | Umlautregel für den Namensleser ungenannt (acht der 58 Spalten tragen `ß`/`ä`) | eingearbeitet |
| M11 | mittel | 1.8 | Vierte Einfrierregel zu eng gefasst | eingearbeitet — Aufzählung der geschützten Spalten samt der G1/G2-Zugänge |
| M12 | mittel | Kap. 4, Zeile G1+G2 | Logbuch-Eintrag und Versionsnummer fehlen in der Abnahme, obwohl sich Anwenderzahlen ändern | eingearbeitet |
| M13 | mittel | 2.7 | `BildauftragRaumtemperatur` ohne Datenzoom; § 5.1 verlangt ihn an jeder Jahresganglinie | eingearbeitet — eigenes `ChartBild` mit eigenem Ausschnitt |
| M14 | mittel | 2.4, 3.3 | Hilfeschlüssel und KI-Maskenanmeldung fehlen | eingearbeitet — je ein Absatz |
| M15 | mittel | 3.4 | Bemaßungsregel ohne den Vorbehalt aus E6 (sie stammt aus VDI 6020) | eingearbeitet — als Arbeitsannahme ohne Normzitat gekennzeichnet |
| M16 | mittel | 3.4 | Bei `IfcMapConversion` fällt die Drehung ersatzlos weg | eingearbeitet — `atan2(XAxisOrdinate, XAxisAbscissa)`, Kontext `ContextType = 'Model'` |
| M17 | mittel | 3.2 | `MemoryModel.OpenRead(pfad, melder)` gibt es so nicht | eingearbeitet — `ReportProgressDelegate`, Senke über `XbimServices` |
| M18 | mittel | 3.2 | Schemafall `Ifc4x1` fehlt in der Ablehnliste | eingearbeitet — samt der abschließenden Aufzählung der drei angenommenen Schemata |
| M19 | mittel | 3.5, Nr. 4 | `InternalOrExternalBoundary` bleibt ungenutzt | eingearbeitet — Vorrang vor der Zählregel, `EXTERNAL_EARTH` setzt zugleich die Randbedingung |
| M20 | mittel | 3.4 | `IIfcRoof` führt `Qto_RoofBaseQuantities`, nicht die Slab-Mengen | eingearbeitet — samt Doppelzählschutz bei `IIfcRelAggregates` |
| M21 | mittel | 3.4 | `RelatingPropertyDefinition` ist ein SELECT | eingearbeitet — im Ersatztext von H12 |
| M22 | mittel | 3.6, 3.8, U16 | Die Trimming-Warnung trifft den falschen Bau: die CI baut für den Simulator, dort wird nie getrimmt | eingearbeitet — nichts wird voreingestellt, G4-8 misst einen Gerätebau; U16 umformuliert |
| M23 | mittel | 3.2, Zeile 2 | Das Größenlimit misst bei `.ifczip` die falsche Zahl | eingearbeitet — entpackte Größe aus dem Zip-Verzeichnis |
| N1 | niedrig | 1.2, 1.10 | Falscher Beleg für den Vorgabeboden (`:185` ist Ton trocken) | eingearbeitet — `:47` (`BODENTYP_DEFAULT`), Katalogzeile `:188` |
| N2 | niedrig | Kap. 4, Summen | Die Obergrenzen der Aufwandssummen sind je 0,5 PT zu klein | eingearbeitet — 30,5 / 42,5 / 63,5 PT. **Nachgebessert:** dieselbe Rechenart traf auch die **Untergrenze von G4a** — G4-1…G4-8 ergeben 13,5 PT, nicht 13; 3.8, die G4a-Zeile in Kapitel 4 und die Gesamtsumme („bis G4a **42–63,5 PT**") sind nachgezogen |
| N3 | niedrig | 3.7 | Zahlen des Probenordners (27 Dateien, rund 300 KB) | eingearbeitet, am Arbeitsbaum nachgezählt |
| N4 | niedrig | 1.6 | 22 gegen 28 `SchemaSpalte`-Einträge | eingearbeitet, **abweichend**: durch die neue Spalte `Kellertemperatur` (M9) sind es **24** Einträge (zwölf Spalten je zweimal), nach der Verschmelzung U5 **30** (fünfzehn Spalten). Die Zahl 28 der Liste kannte M9 noch nicht |
| N5 | niedrig | 1.6 | Beleg für „kein DDL-DEFAULT auf einem Fachwert": `SchemaKatalog.cs:40-44` begründet nur die FK-Spalten | eingearbeitet — Beleg gestrichen |
| N6 | niedrig | 3.6 | Beleg `KeineDateiwahl.cs:14` ist die Klassendeklaration | eingearbeitet — `:17-20` |
| N7 | niedrig | 2.4 | Rückfall `BauweiseAusBauart` liefert ein absolutes 50 | eingearbeitet — Fußnote, Bauart als Pflichtfeld |
| N8 | niedrig | 1.7 | `SolardatenCtrl.Insert`/`WriteDataTable` sind toter Bestand mit falschen Zeilenangaben | eingearbeitet — richtige Zeilen, kein Aufrufer, M4 löscht sie |
| N9 | niedrig | Kap. 0, 1.6 | Zehn nullbare Spalten, nicht elf — `Aussenbauteile_Strahlung` ist `NOT NULL DEFAULT 0` | eingearbeitet, **abweichend**: mit `Kellertemperatur` (M9) sind es **elf nullbare von zwölf** |
| N10 | niedrig | 1.8 | Die drei neuen CSV-Reihen führen Watt gegen Einheitenregel 1 | eingearbeitet — `KuehlbedarfKwh`, Export in kWh, Watt-Form bleibt intern |
| N11 | niedrig | Kap. 4, Zeile G3 | Menüzeile für den Baustoffdialog fehlt | eingearbeitet |
| N12 | niedrig | 3.6 | `.ifcxml` und `.ifczip` haben passende Typkennungen; nur `.ifc` hat keine | eingearbeitet |
| N13 | niedrig | 3.4 | Nur `IIfcPropertySingleValue` zu lesen verfehlt Eigenschaften | eingearbeitet — `IIfcPropertyBoundedValue`, sonst benannt übergangen |
| N14 | niedrig | 3.4 | `ObjectPlacement` ist nicht immer ein `IIfcLocalPlacement` | eingearbeitet — `IMP_IFC_PROT_PLATZIERUNGSART` |
| N15 | niedrig | (Konzept 7.4) | CDDL: § 3.3 in Verbindung mit § 3.2, nicht § 3.4 | **abgelehnt: nicht einschlägig.** Der Satz mit „§ 3.4" steht im Konzept, nicht in diesem Papier; dieses Papier zitiert allein § 3.1 (Quellenverweis an den Empfänger), und das ist richtig. Die Korrektur gehört ins Konzept |
| N16 | niedrig | 3.4 | Die Wohnfläche mischt zwei Messregeln (netto/brutto) | eingearbeitet — `IMP_IFC_PROT_FLAECHENART_GEMISCHT` |
| N17 | niedrig | 3.1 | „kein neuer Maskenschlüssel" widerspricht dem `HilfeSchluessel` in 3.3 | eingearbeitet |
| N18 | niedrig | 3.4, Spalte „Rückfall" | `Bauweise`-Rückfall darf kein absoluter Wert sein | eingearbeitet |
| A1 | Stil | 1.5 | Schreibfehler `Zonemodell2K` | eingearbeitet — `Zonenmodell2K`; keine weitere Fundstelle im Papier |
| A2 | Stil | 1.10 | Die Abweichung von N1.6 (drei Gruppen statt einer) fehlt in der Korrekturtabelle | eingearbeitet — neue Zeile |
| O1 | hoch | Kap. 0 Punkt 6, Kap. 4, 3.8 | Die Stufentabelle stellt den gbXML-Import als Kür in G5; E9 macht ihn zur Pflicht in G4, die Exporte werden G7 | eingearbeitet — neue Zeile **G4c** (14–22 PT, Befund R), neue Zeile **G7**, G5 auf die Geometrieableitung eingegrenzt, Reihenfolgehinweis auf die offene Frage **D1** ohne Entscheid |
| O2 | hoch | Kap. 6 | Produktaussage „validiert an den zwölf Testbeispielen" entspricht nicht E10 | eingearbeitet — Wortlaut nach Konzept N1.15 samt dem Wechsel auf „zwölf von zwölf", sobald G0 den Fall 11 löst |
| O3 | mittel | Kopf, Kap. 5 | „durch E1–E8 entschieden" ist unvollständig | eingearbeitet — E1–E10 im Kopf und in Kapitel 5; weitere vollständige Aufzählungen gibt es nicht |
| O4 | mittel | 3.2 | Der Import muss die Zuordnung EPOS-Gebäude ↔ `IfcBuilding.GlobalId` samt Name, SHA-256 und Zeitpunkt der Quelldatei persistieren (Befund S, Abschnitt 6) | eingearbeitet — eigener Absatz nach der Ablauftabelle, Verweis auf Befund S und das Datenaustauschkonzept; kein Datenmodell hier ausgeführt |

---

## Folgeänderungen, die kein Eintrag verlangt hat

Sie ergeben sich zwingend aus den eingearbeiteten Einträgen und stehen deshalb hier, nicht in der
Tabelle:

1. **Prüfband der Normtestfälle** (aus O2/E10): 1.3, 1.9 und die G0-Zeile in Kapitel 4 führten noch
   „± 0,1 K / ± 1 W". E10 setzt das Band auf **± 0,15 K / ± 1,5 W** (Toleranz plus halbe
   Druckstelle); entsprechend lautet die G0-Abnahme jetzt „elf der zwölf Normtestfälle".
2. **Spaltenzahlen** (aus M9): `Kellertemperatur` zieht 1.6 (24 bzw. 30 `SchemaSpalte`-Einträge),
   1.7 und U5 (15 Spalten je Tabelle), die Sichtdefinition, das DTO in 2.4 (zwölf Felder), die
   Aufwandszeile M-b in 2.11 (12 Felder) und die vierte Einfrierregel in 1.8 nach.
3. **Nullbarkeit** (aus N9/M9): Kapitel 0 Punkt 2 und 1.6 sprechen jetzt von „elf nullbaren der
   zwölf neuen Spalten".
4. **Befundtabelle im Kopf**: Befund R und Befund S sind aufgenommen, weil O1 und O4 sich auf sie
   stützen.
5. **Abgrenzung** (Kapitel 6): eigener Punkt für den Datenaustausch nach E9 — dieses Papier trägt
   allein den IFC-Import.
6. **Schritt 14 der Ablauftabelle** (aus H10/O4) nennt jetzt `UebernehmenInsProjekt` und das
   Ablegen der Herkunftsdaten statt des Katalog-`Speichern`.
7. **Meldungsschlüssel** (aus H13, N14, N13, N16): vier neue `IMP_IFC_PROT_*` — aus dreizehn werden
   **siebzehn**; die Zahl steht in 3.2.

## Was offen ist — Papierführung, beim Orchestrator

Zwei Dinge liegen außerhalb dieses Auftrags und sind **noch nicht erledigt**:
[`Dokumentation/LIESMICH.md`](../../LIESMICH.md) und die Statusdatei pflegt der Orchestrator.

1. **`DokumentationLinkWacheTests.Der_Index_nennt_jedes_Papier` ist heute rot.** Der Test prüft
   jede Datei unter `aktuell/` (`EPOS.Kern.Tests/DokumentationLinkWacheTests.cs:169-190`); es
   fehlen die Indexzeilen für **dieses Protokoll** und für
   `Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`. Jenes Papier ist inzwischen abgelegt, der
   Verweis des Umsetzungskonzepts darauf greift also — offen sind allein die zwei Indexzeilen.
2. **Die Statusdatei** [`Status_Gebaeudesimulation_VDI6007.md`](../Status_Gebaeudesimulation_VDI6007.md)
   führt noch keine Zeile zu Rev. 2 des Umsetzungskonzepts. E9 und E10 stehen dort als entschieden
   und decken sich wörtlich mit dem Kopf des Papiers.

---

## Zählung

| Schwere | Einträge | eingearbeitet | abgelehnt | offen (Form) |
|---|---|---|---|---|
| hoch (H1–H15) | 15 | 15 | 0 | 0 |
| mittel (M1–M23) | 23 | 23 | 0 | 0 |
| niedrig (N1–N18) | 18 | 17 | 1 (N15) | 0 |
| Orchestrator (O1–O4) | 4 | 4 | 0 | 0 |
| Stil (A1, A2) | 2 | 2 | 0 | 0 |
| **Summe** | **62** | **61** | **1** | **0** |

Die Summe zählt die Stil-Einträge mit; sie sind Einträge wie die anderen, nur ohne Schwere.

Drei Einträge sind **abweichend von ihrem NEU-Text** eingearbeitet (H5, H14, N4/N9); die Gründe
stehen in der Tabelle. Ein Eintrag ist abgelehnt, weil er ein anderes Papier betrifft (N15).
Kein Eintrag blieb als „offen (Form)" liegen.

---

## Nachlese — unabhängige Nachprüfung der Einarbeitung

Eine unabhängige Prüfung hat alle 62 Einträge im Papier nachgewiesen: keiner fehlt, keiner ist
sinnentstellt. Sie hat sieben kleine Unstimmigkeiten gefunden — fünf sind im Papier behoben, zwei
liegen beim Orchestrator.

| Nr. | Stelle | Unstimmigkeit | Erledigung |
|---|---|---|---|
| 1 | 2.11, Zeile M-b | „11 Felder in `GebaeudeKatalogDaten`", während 2.4 nach M9 auf zwölf gezogen war — die eine Stelle, die stehen blieb | behoben: **12 Felder**, mit Verweis auf 2.4 (siehe M9) |
| 2 | 2.4, DTO | „neun `double?` (die sieben Modellparameter, die Kellertemperatur und die Heizleistungsgrenze)" geht nicht auf: die Gruppe „Rechenmodell" führt fünf Zahlenparameter plus Heizleistungsgrenze | behoben: die neun sind nun einzeln benannt — fünf Zahlenparameter, Heizleistungsgrenze, `Kellertemperatur`, `FensterflaecheOst`, `FensterflaecheWest`. Die **Zahl zwölf war richtig**, nur die Aufschlüsselung nicht. „Sieben Modellparameter und ein Schalter" in 2.1 zählt die acht Zeilen der Gruppe „Rechenmodell" und bleibt unverändert; das DTO zählt anders, weil Ost und West dazukommen und die Klappliste ein `string?` ist |
| 3 | Kap. 4, Papierführung | Die Zeilenspanne `:50-70` für die Indexzeilen trifft nicht | behoben: dieses Papier steht auf `Dokumentation/LIESMICH.md:55`, die Befunde A–S auf `:56-74`; die H5-Zeile oben nannte `:51` und ist nachgezogen |
| 4 | 3.8 und Kap. 4, G4a | Die Teilsummen G4-1…G4-8 ergeben 13,5–21 PT, beide Tabellen nannten 13–21 | behoben: **13,5–21 PT** an beiden Stellen, Gesamtsumme „bis G4a **42–63,5 PT**" (dieselbe Rechenart wie N2) |
| 5 | Zählung dieses Protokolls | Die Summenzeile stand über der Stil-Zeile und las sich, als summierten allein die vier Zeilen darüber auf 62 | behoben: die Stil-Zeile steht jetzt vor der Summe, dazu ein Satz, was die Summe zählt |
| 6 | `Dokumentation/LIESMICH.md` | Diesem Protokoll fehlt die Indexzeile (dem inzwischen abgelegten Datenaustauschkonzept ebenso), `Der_Index_nennt_jedes_Papier` ist deshalb rot | **offen, Orchestrator** — siehe „Was offen ist" |
| 7 | `Status_Gebaeudesimulation_VDI6007.md` | Keine Zeile zu Rev. 2 des Umsetzungskonzepts | **offen, Orchestrator** — siehe „Was offen ist" |

Nicht berührt hat die Nachbesserung den Inhalt der 62 Einträge: keine der Korrekturen wird dadurch
unwirksam, und keine Zahl des Rechenwegs ändert sich.
