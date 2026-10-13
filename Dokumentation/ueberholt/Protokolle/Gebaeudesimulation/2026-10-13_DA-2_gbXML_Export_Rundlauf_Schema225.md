# Protokoll DA-2 — gbXML-Export von Lasten und Zeitplänen, Rundlauf, Schemaschritt Lastfelder der Zone

Welle DA-2 aus dem [Konzept Datenaustausch der Simulationsdaten](../../../aktuell/Gebaeudesimulation/2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)
(Abschnitte 2, 3.1, 4.4, 7). Grundlage ist Entscheid E138 (Anwender 13.10.2026): F5 Rundlauftest je DA-Welle mit Wache „jedes
Inventardatum eingestuft“; F7 Ergebnisse im Export wie heute; F8 (b) Absorptions- und Emissionsgrad nur durchreichen. Vorgänger:
[Protokoll DA-1](2026-10-13_DA-1_gbXML_Import_Lasten_Zeitplaene.md).

## Schemaschritt `ZonenLastfelderSchema`

| Spalte | Tabellen | Typ | Inventar | Bedeutung |
|---|---|---|---|---|
| `Aussenluft_Person` | `Tab_Zone`, `Tab_Zone_STAMM` | `REAL`, nullbar, `CHECK (… IS NULL OR … >= 0)` | B14 | Außenluft je Person [m³/(h·Person)] |
| `Licht_W_m2` | dieselben | ebenso | B18 | Beleuchtungsleistung je Fläche [W/m²], ein Teil der inneren Lasten |
| `Personen_Waerme_W` | dieselben | ebenso | B16 | Wärmeabgabe je Person [W] |

- **Kein `Licht_Anteil`:** Der Bestand hält die inneren Lasten als Summe (`Interne_Waermegewinne` bzw. Nennwert des Kalenders
  `GERAETE`). Summe und Lichtleistung je Fläche geben die Geräte ohne Verlust zurück (Geräte = Summe − Licht × Fläche). Ein
  Anteil wäre ein zweiter Träger derselben Größe.
- **Rechenweg unberührt:** Der Lauf liest keine der drei Spalten, er liest weiter Luftwechsel, `Interne_Waermegewinne` und die
  Nennwerte der Kalender. Keine Referenzzone trägt einen Wert.
- **Kette:** `SCHRITT = BauteilkatalogHaustechnikSchema.SCHRITT + 1`. Im Worktree liegen die angemeldeten Schritte 223 (Sitzung
  Zapfprofilgenerator) und 224 (`KaeltebedarfSplitAbrechnungSchema`, Sitzung Anlagendialog) nicht vor, deshalb trägt der Schritt
  hier die Nummer 223. **Beim Zusammenführen hängt er an die Klasse des Schritts 224 und trägt die angemeldete Nummer 225.**
  Zusammenführung auf den Stand mit K6: angemeldet als 225, hängt vorläufig an 223 (`SCHRITT = KaeltebedarfSplitAbrechnungSchema.SCHRITT + 1`, Nummer 224); beim Push wird an 224 `TwwZonenferienSchema` (Zapfprofil) umgehängt.
  `SchemaStand.Zielversion` = `ZonenLastfelderSchema.SCHRITT`.
- **Eingehängt** wie Schritt 222: `SchemaMigration` der Windows-Schale (`SCHRITT_ZONEN_LASTFELDER`, `Schritt_ZonenLastfelder`),
  `Werkzeuge/Testdatenbankschema`, Testvorrichtung `EPOS.Kern.Tests/TestDatenbank.cs`, `Paketanhebung` (Art `Ddl`), Zielversion.
  Wiederholbar, zweiter Lauf ohne Handgriff.
- **Testdatenbank** mit `Werkzeuge/Testdatenbankschema` migriert (6 Spalten angelegt, Schemastand 223), mit aktivem LFS-Filter
  committet: LFS-Zeiger `03ba8220…` (96 604 160 Byte) → `cc9adf2c…` (96 608 256 Byte); der Commit trägt nur den Zeiger.
- **Lesen und Schreiben:** `ZoneModel` trägt die drei Felder, `GebaeudeZonenCtrl` liest und schreibt sie als Zusatzspalten hinter
  der Probe `GebaeudeZonenanschluss.LastfelderVorhanden` (ohne Schritt bleiben sie außen vor). Katalogkopie und Projektpaket kopieren
  die Spalten schemagetrieben. `GebaeudeExportVerluste` stuft alle drei als rundläufig ein.
- **Schemawachen:** `BauteilkatalogHaustechnikSchemaTests` prüft das Ziel als „mindestens“; neu `ZonenLastfelderSchemaTests` (5 Tests).

## Export (`GbxmlSchreiber`, neu `GbxmlZeitplanschreiber`)

| Inventar | gbXML-Element | Quelle in EPOS | Regel |
|---|---|---|---|
| C1–C5 | `Schedule` (`type` `Temp` bzw. `Fraction`) je Kalender der Zone, Kennung `<Zonenkennung>-<Größe>` | Kalender der Zone (Zone vor Gebäude) | stabil, zweimal geschrieben byte-gleich |
| C7, A10 | `YearSchedule` mit `Name`, `BeginDate`, `EndDate` (Bezugsjahr 2001, Gemeinjahr), `WeekScheduleId` | Grundwoche 1–365, danach je Periode `ZEITRAUM`/`FERIEN`/`BETRIEBSPAUSE` aufsteigend nach Rang | der spätere Abschnitt gilt wie der ranghöhere |
| A8 (nur im Kalender) | `WeekSchedule/Day/@dayType` | Woche der Angabe | gleiche Tage Mo–Fr → `Weekday`, gleiches Wochenende → `Weekend`, sonst Einzeltage |
| C6 | `Day/@dayType="Holiday"` in der Grundwoche | Angabe der ranghöchsten Feiertagsregel | Tagesbild Montag der Angabe |
| — | `DaySchedule/ScheduleValue` (24 Werte) | Stundenwerte | gleiche Tagesbilder eines Plans teilen einen Tagesplan; „aus“: Heizen −50 °C, Kühlen 99 °C, Anteile 0 |
| B15, B16 | `Space/PeopleNumber`, `PeopleHeatGain` (`unit="W"`, `heatGainType="Sensible"`), `peopleScheduleIdRef` | Nennwert `PERSONEN`, `Personen_Waerme_W` | Personen = Nennwert ÷ Wärme (ohne Feld 70 W); ohne Kalender der Bestand |
| B17, B18 | `EquipPowerPerArea`, `LightPowerPerArea`, `equipmentScheduleIdRef`, `lightScheduleIdRef` | Nennwert `GERAETE`, `Licht_W_m2` | Geräte = Nennwert ÷ Fläche − Licht; Licht trägt den Geräteplan |
| B14 | `Space/OAFlowPerPerson` (`CubicMPerHr`), `OAFlowPerArea` (`CubicMPerHrSquareM`) | `Aussenluft_Person` | ohne Umrechnung |
| B13, C3 | `Zone/AirChangesPerHour`, `airChangesSchedIdRef` | Kalender `LUEFTUNG` | Nennwert = Höchstwert des Kalenders (sonst `Luftwechsel_Nutzer`), Plan in Anteilen |
| B6, B9, C1, C2 | `DesignHeatT`, `DesignCoolT` (Bestand), `heatSchedIdRef`, `coolSchedIdRef` | Kalender `HEIZSOLL`, `KUEHLSOLL` | — |
| F8, F9 | `Construction/Absorptance` `type="ExtSolar"` (0,6) und `type="ExtIR"` (0,9) | `GebaeudeFestwerte.ABSORPTIONSGRAD_OPAK`, `EMISSIONSGRAD_AUSSEN` | E138 F8 (b): der Import liest beide und meldet eine Abweichung über 0,005 (`IMP_GBXML_PROT_ABSORPTION_ABWEICHUNG`), ohne Feld |
| Ergebnisse | `Results` | wie bisher (F7) | unverändert |

**Zählung:** 21 Elemente und Attribute neu geschrieben (6 an `Space`, 4 an `Zone`, 10 der Zeitplankette, `Absorptance` in zwei
Arten); PeopleNumber und EquipPowerPerArea folgen dem Kalender, wo die Zone einen trägt. **Ohne Element** blieben F3
(Rahmenanteil), F4 (Verschattung), F7 (Emissionsgrad der Verglasung) und F10 (Albedo): Ohne Schemakopie ist kein Element
belegt (`WindowType/Frame`, `ShadingCoeff`, `Emittance` sind ungeprüft). Fensterwerte U und g schreibt der Export wie bisher.

## Rundlauf (E138 F5)

`EPOS.Kern.Tests/GbxmlExportRundlaufTests` (3 Tests): Eine Zone (100 m², 250 m³) mit fünf Kalendern — Heizen mit Werktag-,
Wochenend- und Nachtwerten, Messe (Bezug „wie Samstag“), Sommerferien und neun Feiertagen; Kühlen mit „aus“ außerhalb der
Nutzung und Hitzeperiode; Lüftung 2 bzw. 0,5 1/h; Geräte 800 W mit Umbau und Betriebsurlaub; Personen 420 W mit Feiertagen —
dazu Licht 3 W/m², Wärme 80 W je Person und Außenluft 30 m³/(h·Person). Export, Rücklesen mit dem eigenen Leser, Bildung über
den Importweg der DA-1 und Vergleich:

- **verglichen:** je Kalender die 8760-Reihe (5 × 8760 = 43 800 Stundenwerte), der Nennwert (5), die Perioden ohne Feiertage mit
  Bezeichner, Beginn, Ende und Art (5 Perioden, 20 Felder), die Feiertagsregeln (18), der Nennluftwechsel (1) und die drei
  Lastfelder — zusammen **43 847 Felder, 0 Abweichungen**.
- Die Datei ist zweimal geschrieben byte-gleich (stabile Kennungen).
- Der Import schweigt bei den Konstanten und meldet einen geänderten Absorptionsgrad.

**Grenzen des Rundlaufs** (benannt, im Test nicht als Verlust geführt): der Rang einer Periode wird neu vergeben (Reihenfolge
bleibt); „wie Wochentag X“ kommt als Woche zurück; die Art `BETRIEBSPAUSE` eines Temperaturkalenders kommt als `ZEITRAUM` mit
„aus“ zurück; eine Periode über den Jahreswechsel kommt als zwei Perioden zurück; sich überlappende Perioden werden an den
Grenzen geteilt; die Feiertage kommen als die neun Regeln des Kerns mit einem gemeinsamen Tagesbild zurück (Konzept 3.1 C6);
benannte Wochen (C9) und das Zeilenbild (C8) haben kein Element.

## Inventar-Einstufung (`DatenaustauschInventar`, Wache `DatenaustauschInventarWacheTests`, 2 Tests)

Die Tafel führt die 71 Daten des Konzepts mit Kennung A1 … G4 und der Einstufung für gbXML. Die Wache hält die Zählung je
Gruppe (A 10, B 22, C 10, D 8, E 7, F 10, G 4), lückenlose Kennungen und die Deckung mit dem Code: Jedes Element eines Datums
„beide Richtungen“ steht in der exportierten Rundlaufdatei und im Leser.

| Einstufung gbXML | Zahl | Daten |
|---|---|---|
| beide Richtungen | 42 | A4, A6, A10, B1–B4, B6–B9, B12–B18, C1–C7, D1–D7, E1–E5, F1, F2, F8, F9, G2 |
| nur gelesen | 3 | A1, B5, F6 |
| nur geschrieben | 0 | — |
| ohne Element | 26 | A2, A3, A5, A7, A8, A9, B10, B11, B19–B22, C8, C9, C10, D8, E6, E7, F3, F4, F5, F7, F10, G1, G3, G4 |

A8 ist „ohne Element“ als Feld: Die Tagtypen tragen das Wochenende nur im Kalender. DA-3 (IFC) und DA-4 (Rest) setzen die Spalte
IFC an und ziehen Daten aus „ohne Element“ in den Rückfall nach Konzept 3.3.

## Import (Ergänzung zu DA-1)

- `GbxmlKonditionierung.Lastfelder` setzt die drei Spalten zusätzlich zu Kalendern und Nennwerten: Licht flächengewichtet
  (Σ Licht × Fläche ÷ Fläche der Zone), Wärme und Außenluft je Person personengewichtet; latente Wärme bleibt außen vor.
  Mehrzonenweg und Einzonenweg (beheizte Räume) rufen sie.
- Der Hinweis `IMP_GBXML_PROT_AUSSENLUFT_PERSON` nennt, dass der Wert je Person zusätzlich an der Zone steht.

## Annahmen zum Schema 6.01

Die Schemakopie liegt nicht im Repositorium (Protokoll DA-1). Ohne Kopie angenommen sind: die Reihenfolge der Wurzelelemente
(`Schedule`, `WeekSchedule`, `DaySchedule` vor `Zone`); `type` an `Schedule`, `WeekSchedule` und `DaySchedule`; die Einheiten `W`,
`CubicMPerHr`, `CubicMPerHrSquareM`; `heatGainType="Sensible"`; `Absorptance` an `Construction` mit `type` `ExtSolar`/`ExtIR`
und `unit="Fraction"` und ihre Stellung nach `U-value`; die Stellung von `PeopleHeatGain` nach `PeopleNumber` und von `OAFlowPerPerson`
und `OAFlowPerArea` nach `EquipPowerPerArea`; der Attributname `airChangesSchedIdRef`. Die Schemaprobe gegen die Kopie (Probe 3)
ist mit der Kopie lokal zu fahren.

## Dateien

- neu: `EPOS.Kern/Allgemein/Update/ZonenLastfelderSchema.cs`, `EPOS.Kern/Allgemein/Export/Gbxml/GbxmlZeitplanschreiber.cs`,
  `EPOS.Kern/Allgemein/Export/Gebaeude/DatenaustauschInventar.cs`, `EPOS.Kern.Tests/ZonenLastfelderSchemaTests.cs`,
  `EPOS.Kern.Tests/GbxmlExportRundlaufTests.cs`, `EPOS.Kern.Tests/DatenaustauschInventarWacheTests.cs`
- geändert: `SchemaStand.cs`, `Paketanhebung.cs`, `SchemaMigration.cs` (Schale), `Werkzeuge/Testdatenbankschema/Program.cs`,
  `EPOS.Kern.Tests/TestDatenbank.cs`, `BauteilkatalogHaustechnikSchemaTests.cs`, `ZoneModel.cs`, `GebaeudeZonenCtrl.cs`,
  `GebaeudeZonenanschluss.cs`, `GebaeudeExportVerluste.cs`, `GebaeudeExportAblauf.cs`, `GbxmlSchreiber.cs`, `GbxmlVokabular.cs`,
  `GbxmlLeser.cs`, `GbxmlKonditionierung.cs`, `GebaeudeBauteilvorschlag.cs`, beide `Resource*.resx`, `Resource.Designer.cs`,
  Testdatenbank (LFS)
- Ressourcen: 1 neuer Schlüssel je Sprache (`IMP_GBXML_PROT_ABSORPTION_ABWEICHUNG`), 1 Text je Sprache geändert
  (`IMP_GBXML_PROT_AUSSENLUFT_PERSON`)

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale (`EnableWindowsTargeting=true`, Debug x64): 0 Fehler.
  `Werkzeuge/Testdatenbankschema` (Release): 0 Fehler.
- `dotnet test EPOS.Kern.Tests --filter "Gbxml|Schema|Datenaustausch|Zonenplan|ReferenzprojektWache"`: 1062 grün, 0 rot. Neu sind
  10 Tests (`ZonenLastfelderSchemaTests` 5, `GbxmlExportRundlaufTests` 3, `DatenaustauschInventarWacheTests` 2). Nachgezogen:
  `GebaeudeG3SchemaTests` (Spaltenfolge der Zone), `BauteilkatalogHaustechnikSchemaTests` (Ziel „mindestens“). Zusätzlich
  `GebaeudeExport|GebaeudeImport|Vorheiz`: grün.
- SQL-Dialekt-Prüfer: 2906 SQL-Texte, 0 Fundstellen. Designer: zweiter Lauf +0 (wiederholbar).
- Referenzlauf der zwölf Projekte der CI-Auswahl gegen `2026-10-11_R52_Vorheizen`: GESAMT PASS (4 452 743 Werte), 12 von 12
  byte-gleich.

## Offen

- **Nummer 225 beim Zusammenführen** (siehe Kette).
- **Schemakopie 6.01:** Annahmen oben prüfen; Fensterelemente für F3, F4, F7 nach der Kopie schreiben und lesen.
- **Mehrere Räume je Zone:** Der Export schreibt je Zone einen Raum. Ein Rücklesen mehrerer Räume verdichtet nach DA-1.
- **Rückfall 3.3** (`EPOS:`-Block in `Description`) für die 26 Daten „ohne Element“ ist nicht gebaut (DA-4).
- **Anwenderprobe unter Windows:** Export einer Zone mit Kalendern und Wiedereinlesen im Importdialog.

## Logbuch-Vorschlag

Der gbXML-Export schreibt die Kalender der Zonen als Zeitpläne sowie Personen, Geräte, Licht, Außenluft und Luftwechsel je Zone;
ein Wiedereinlesen gibt sie ohne Verlust zurück.
