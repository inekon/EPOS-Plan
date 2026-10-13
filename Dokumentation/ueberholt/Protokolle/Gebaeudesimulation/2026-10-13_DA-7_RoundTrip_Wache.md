# Protokoll DA-7 — Round-Trip-Wache: Inventartafel, „jedes Feld eingestuft“, gemeinsamer Rundlauf gbXML/IFC

Welle DA-7 des [Konzepts Datenaustausch](../../../aktuell/Gebaeudesimulation/2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)
(Abschnitt 7; E138 F5). Zweig `da7`, Basis `5012a267f` (DA-2 und DA-3 zusammengeführt, Schemaschritt 225). Nur Tests, Tafel,
Ausnahmen und Protokoll; kein Leser, kein Schreiber, kein Schemaschritt, kein SQL, keine Ressource. Referenzprojekte bleiben
unberührt.

## Befund

- Die Kerntafel `DatenaustauschInventar` (DA-2/DA-3) führt die 71 Daten mit dem Weg je Format und den Elementen, aber nicht die
  EPOS-Felder dahinter. Ob ein neues Feld einer Zone, eines Bauteils oder eines Kalenders eingestuft ist, prüfte bisher nur die
  Verlustliste `GebaeudeExportVerluste` — und nur für drei Modelle, ohne Kalender, Luftströme, Aufbauten und Schichten.
- Die Verlustliste ist hinter DA-2 zurückgeblieben: 40 ihrer Felder widersprechen der Tafel (Abstimmungsliste unten), etwa
  `Luftwechsel_Nutzer`, `Interne_Waermegewinne`, `Bewohner` und der Kühlsollwert der Zone, die gbXML seit DA-2 in beiden Richtungen trägt.
- Der gemeinsame Rundlauf fand im IFC-Weg fünf Abweichungen an der Bauteillage, die der DA-3-Rundlauf nicht verglich (er misst
  U, g, Rahmenanteil, ψ·L und Schichten, nicht Neigung, Azimut und Nachbarn). Darunter ein echter Befund: **die Außenwand Ost
  (Azimut 90°) kommt als 270° zurück**; Nord, Süd und West kommen richtig zurück.
- Der gbXML-Export schreibt Personen und Geräte aus dem Nennwert der Kalender (DA-2: Nennwert = Personen × Wärme je Person bzw.
  (Geräte + Licht) × Fläche). Eine Probe, deren Zonenfelder nicht dazu passen, kommt mit den Kalenderwerten zurück — in EPOS
  passen beide immer zusammen; die gemeinsame Probe hält deshalb 5,25 Personen und 5 W/m² Geräte (420 W, 800 W).

## Tafel (`EPOS.Kern.Tests/Datenaustauschinventar.cs`)

Je Datum: Kennung, EPOS-Felder (`Tabelle.Spalte`, für reine Modellfelder `Modell.Feld`), Welle je Format, Grund, wenn ein Format
das Datum nicht oder nur lesend trägt. Weg und Elemente kommen aus der Kerntafel (keine Doppelführung). Formatklasse: gbXML
Standardelement (beide Richtungen oder nur gelesen) / Erweiterungspunkt (nicht gebaut, 0) / nicht übertragbar; IFC Standardsatz /
`EPOS_*` / nicht übertragbar. „DA-4 (in Arbeit)“ lässt die Wache in jedem Weg durch; die Orchestrierung stellt diese Zeilen beim
Zusammenführen von DA-4 auf die tragende Welle um (und die Zählungen in der Wache).

| Nr | Datum | gbXML | Welle | IFC | Welle | Grund, wo nicht getragen |
|---|---|---|---|---|---|---|
| A1 | Breite, Länge | nur gelesen | Bestand | Standard | DA-3 | gbXML: der Export schreibt die Lage nicht |
| A2 | Klimaregion, Klimazone | nicht übertragbar | offen | nicht übertragbar | offen | `Location/Name` nur Hinweis; `EPOS_Gebaeude` nicht erweitert |
| A3 | Höhe über NN | nicht übertragbar | kein Feld | nicht übertragbar | kein Feld | steckt im Klimadatensatz |
| A4 | Nordrichtung | Standard | Bestand | Standard | Bestand | — |
| A5 | Baujahr, Baualtersklasse | nicht übertragbar | offen | Standard | Bestand | gbXML 6.01 ohne Element, Rückfall 3.3 nicht gebaut |
| A6 | Gebäudeart | Standard | Bestand | `EPOS_*` | DA-3 | — |
| A7 | Bauweise, Masseanteil | nicht übertragbar | offen | nicht übertragbar | offen | Rückfall 3.3 bzw. `EPOS_Gebaeude` nicht gebaut |
| A8 | Wochenendtage | nicht übertragbar | offen | nicht übertragbar | offen | gbXML nur über `dayType` im Kalender |
| A9 | Feiertagsland | nicht übertragbar | offen | nicht übertragbar | offen | `dayType` Holiday ohne Landesregel |
| A10 | Ferien | Standard | DA-2 | `EPOS_*` | Bestand | — |
| B1–B4 | Bezeichner, Fläche, Volumen, beheizt | Standard | Bestand | Standard | Bestand | — |
| B5 | Nutzungsprofil | nur gelesen | Bestand | `EPOS_*` | Bestand | gbXML: keine Enumeration im Export (DA-5) |
| B6 | Heizsollwert Tag | Standard | Bestand | Standard | Bestand | — |
| B7 | Heizsollwert Nacht | Standard | DA-2 | `EPOS_*` | Bestand | — |
| B8 | Sollwert Wochenende, Ferien | Standard | DA-2 | `EPOS_*` | DA-3 | — |
| B9 | Kühlsollwert | Standard | DA-2 | Standard | DA-3 | — |
| B10, B11 | Höchsttemperatur, Höchstleistungen | nicht übertragbar | DA-4 | nicht übertragbar | DA-4 | DA-4 (in Arbeit) |
| B12 | Infiltration | Standard | Bestand | `EPOS_*` | DA-3 | — |
| B13–B18 | Nutzerluftwechsel, Außenluft, Belegung, Wärme je Person, Geräte, Licht | Standard | DA-2 | Standard bzw. `EPOS_*` (B14) | DA-3 | — |
| B19 | Strahlungsanteil der Heizung | nicht übertragbar | offen | nicht übertragbar | offen | Rückfall 3.3 bzw. `EPOS_Zone` nicht gebaut |
| B20–B22 | Vorheizzeit, Übergabe Heizen/Kühlen je Zone | nicht übertragbar | DA-4 | nicht übertragbar | DA-4 | DA-4 (in Arbeit) |
| C1–C7 | Kalender, Feiertage, Perioden | Standard | DA-2 | `EPOS_*` | Bestand | — |
| C8, C9 | Zeilenbild, benannte Wochen | nicht übertragbar | DA-4 | nicht übertragbar | DA-4 | DA-4 (in Arbeit) |
| C10 | Beleuchtungszeitplan | nicht übertragbar | kein Feld | nicht übertragbar | kein Feld | steckt im Profil |
| D1–D7 | Bauteil | Standard | Bestand | Standard | Bestand | — |
| D8 | ψ·L | nicht übertragbar | offen | `EPOS_*` | DA-3 | gbXML: Rückfall 3.3 nicht gebaut |
| E1–E4 | Schichtfolge, λ, ρ, c | Standard | Bestand | Standard | Bestand | — |
| E5 | Luftschicht, R | Standard | Bestand | `EPOS_*` | DA-3 | — |
| E6 | inhomogene Abschnitte | nicht übertragbar | offen | nicht übertragbar | offen | Export als homogenisierte Ersatzschicht |
| E7 | Übergangswiderstände | nicht übertragbar | offen | nicht übertragbar | offen | gbXML an der Schemakopie zu prüfen; `EPOS_Bauteil` nicht erweitert |
| F1, F2 | U_w, g | Standard | Bestand | Standard | Bestand | — |
| F3 | Rahmenanteil | nicht übertragbar | offen | Standard | DA-3 | gbXML: `WindowType/Frame` an der Schemakopie zu prüfen |
| F4 | Verschattung | nicht übertragbar | offen | nicht übertragbar | offen | `ShadingCoeff` bzw. `Pset_DoorWindowShadingType` nicht gebaut |
| F5 | Lichttransmission | nicht übertragbar | kein Feld | nur gelesen | DA-3 | nicht gerechnet, nur durchgereicht |
| F6 | winkelabhängiger g-Wert | nur gelesen | Bestand | nicht übertragbar | kein Feld | nur 0°; kein Feld |
| F7 | Emissionsgrad Verglasung | nicht übertragbar | kein Feld | nicht übertragbar | kein Feld | nicht gerechnet |
| F8, F9 | Absorptions-, Emissionsgrad (Konstanten) | Standard | DA-2 | Standard | DA-3 | — |
| F10 | Albedo | nicht übertragbar | kein Feld | nicht übertragbar | kein Feld | Konstante im Klimaweg |
| G1 | Luftströme zwischen Zonen | nicht übertragbar | DA-4 | nicht übertragbar | DA-4 | DA-4 (in Arbeit) |
| G2 | Bodenplatte, Randbedingung | Standard | Bestand | Standard | Bestand | — |
| G3, G4 | Umfang P, U-Wert Erdreich, Kellertemperatur | nicht übertragbar | DA-4 | nicht übertragbar | DA-4 | DA-4 (in Arbeit) |

Zuordnungen, die das Konzept nicht wörtlich nennt (DA-7): `Tab_Gebaeude.Energiestandard` → A5 (steuert Vorgaben),
`Luftwechselrate` → B12, `Nachtabsenkung_Beginn/Ende` → B7, `Wochenende` → B8, `Sommerlueftung` → C3, `Aussenbauteile_Strahlung` → F8,
die Wärmebrückenfelder des Klassenwegs (`WBVK_*`, `Abmessung_Anschluß_Fenster_Wand`, `_Wand_Dach`) → D8, `Aufheizzeit_Manuell_H` → B20,
`Tab_Konditionierungskalender.Nutzung` → B5, `Tab_Konditionierungsperiode.ID_Woche` → C9.

## Zählung (Formatwahl nach Konzept 3.4)

| | Standard | eigener Weg | nicht übertragbar | davon DA-4 | davon offen | davon kein Feld |
|---|---|---|---|---|---|---|
| gbXML | 45 (42 beide Richtungen, 3 nur gelesen) | 0 (Erweiterungspunkt nicht gebaut) | 26 | 10 | 11 | 5 |
| IFC | 32 (31 beide Richtungen, 1 nur gelesen) | 16 (`EPOS_*`) | 23 | 10 | 8 | 5 |

Welle je Format — gbXML: Bestand 26, DA-2 19, DA-4 10, offen 11, kein Feld 5. IFC: Bestand 31, DA-3 17, DA-4 10, offen 8, kein Feld 5.

**Formatwahl:** Im eigenen Rundlauf trägt IFC 47 Daten (31 + 16), gbXML 42 — jedes Datum, das gbXML trägt, trägt auch IFC. Für ein
Fremdwerkzeug sichtbar sind in gbXML 45, in IFC 32 (die Kalender C1–C7 liegen nach F6 (a) in `EPOS_*`). Damit gilt 3.4 weiter:
gbXML für die Übergabe an ein Simulationswerkzeug, IFC für die Rückgabe an das CAD-Programm. Nach DA-4 wandern höchstens die
zehn DA-4-Daten aus „nicht übertragbar“; offen bleiben in gbXML 11 (A2, A5, A7, A8, A9, B19, D8, E6, E7, F3, F4), in IFC 8
(A2, A7, A8, A9, B19, E6, E7, F4).

## Wache „jedes Feld eingestuft“ und Ausnahmen

Geprüft werden die Spalten von zehn Tabellen über `PRAGMA table_info` der Testdatenbank (`Tab_Zone`, `Tab_Bauteil`, `Tab_Gebaeude`,
`Tab_Zonenluftstrom`, die vier Konditionierungstabellen, `Tab_Bauteilaufbau`, `Tab_Bauteilschicht`) — 294 Spalten, 159 in der Tafel,
135 in Ausnahmen, 0 ohne Einstufung — und per Reflexion die öffentlichen Felder von `ZoneModel`, `BauteilModel`,
`ProjektGebaeudeModel` und `GebaeudeModel` (über die Tabellenspalte oder als `Modell.Feld`). Umgekehrt muss jede Spalte der Tafel und
der Ausnahmen in der Testdatenbank bestehen (auch `Tab_Klimaregion`, `Tab_Projekt`, `Tab_Importquelle`) und jedes Modellfeld im Modell.

| Ausnahme (Grund) | Felder |
|---|---|
| Kennung, Fremdschlüssel oder Rang — die Datei trägt eigene Kennungen, der Import vergibt neue | `ID`, `ID_*`, `Rang` aller Tabellen; `items`, `ID_Gebaeude`, `Bauteile` der Modelle |
| Herkunftsmarke der Übernahme | `Herkunft`, `Quellkennung`, `Flaechenherkunft`, `Quelle`, `Dicke_Quelle` |
| Anzeige, Name oder Freitext | Bauteil `Bezeichner`, `Gebaeudename`, `Typ`, `Beschreibung`, Kalender `Bemerkung`, Aufbau- und Schichttexte |
| Katalogeinordnung und -prüfung | Aufbau `Bauteilart`, `Typaufbau`, `*_Code`, `Simulation_Ungeeignet`, `Gueltigkeit`; Schicht `*_Code` |
| Bauphysik und Ökobilanz ohne Rechenweg der Gebäudesimulation | Feuchte (`Mue_*`, `Feuchtegehalt`, `Kapillaraktiv`, `Dampfdicht`), Mechanik, `Hinterlueftet`, Schicht-`Emissionsgrad`, Einbau-ψ des Fensters, `OI3_*` |
| Summenfeld des Klassenwegs (der Import bildet es aus den Bauteilen) | Fensterflächen je Richtung, `k_Wert_*`, Flächen von Wand, Dach, Fenster, Sonstigem |
| Bedarf (Konzept 8) | `Wohnflaeche_gesamt`, Gebäude-`Bewohner`, `WW_Bedarf`, `spez_Waermeverbrauch`, `Waermebedarf`, Bedarfsfelder des Modells |
| Wahl des Rechenwegs | `Gebaeude_Modell` |
| Anlagentechnik am Gebäude (Konzept 8) | Heizkreis, Übergabe, Auslegung, Heizkurve, Kühlübergabe, Kühlkurve am Gebäude (27 Spalten) |

## Verlustliste ↔ Tafel

Regel: „rundläufig“ heißt in der Tafel gbXML beide Richtungen (oder Ausnahme), „benannter Verlust“ heißt nicht ganz getragen
(oder Ausnahme), „nicht Gegenstand“ heißt nicht in der Tafel. Jedes Feld der Verlustliste ist eingestuft. **40 benannte Abweichungen**
(`DatenaustauschRundlaufWacheTests.Abstimmung`), eine neue oder eine erledigte macht die Wache rot:

- Zone (11): `Luftwechsel_Nutzer`, `Interne_Waermegewinne`, `Bewohner`, `Raumsolltemperatur_Nachtabsenkung`, `_Wochenende`, `_Ferien`,
  `Kuehl_Sollwert`, `Kuehl_Sollwert_Nacht`, `Kuehlung_Aktiv` (Verlust, gbXML trägt sie seit DA-2), `Bezeichner` (Verlust wegen des
  Einzonenwegs X4), `Nutzungsprofil` (nicht Gegenstand, Tafel B5).
- Bauteil (2): `ID_Nachbarzone`, `Trennflaeche_Zuordnung` (Verlust; D6 trägt die Nachbarschaft, der Import macht innere Masse).
- Gebäude (27): Sollwerte Nacht/Wochenende/Ferien, `Nachtabsenkung_Beginn/Ende`, `Wochenende`, `Ferien` und die acht Ferienfelder,
  Kühlsollwerte und `Kuehlung_Aktiv`, `Luftwechsel_Nutzer`, `Interne_Waermegewinne`, `Gebaeudeart`, `Wohngebaeude_Nicht_Wohngebaeude`,
  `Luftwechselrate`, `Sommerlueftung`, `Grundflaeche_Randbedingung`, `Aussenbauteile_Strahlung` (Verlust am Gebäudefeld, das Datum
  trägt gbXML über Zone, Kalender oder Bauteil), `Grundflaeche` (nicht Gegenstand, Tafel G2).

**Ergänzung der Verlustliste (nicht in DA-7 geändert):** an der Zone `Luftwechsel_Nutzer`, `Interne_Waermegewinne`, `Bewohner`,
`Kuehl_Sollwert`, `Kuehl_Sollwert_Nacht`, `Kuehlung_Aktiv` auf „rundläufig“ (DA-2 trägt sie je Zone); am Gebäude `Gebaeudeart` auf
„rundläufig“ (`buildingType`); die Nacht-, Wochenend- und Ferienwerte bleiben Feldverluste, die der Kalender trägt — ihr Text sollte
das nennen. Dazu die doppelte Zeile `["Vorheizzeit_H"]` in `GebaeudeExportVerluste.Zone` (zweimal gesetzt, harmlos) entfernen.

## Gemeinsamer Rundlauf

`DatenaustauschRundlaufWacheTests.Gemeinsamer_Rundlauf_beider_Formate_gleich_wo_die_Tafel_traegt_sonst_verlustbenannt`: das Abbild
des IFC-Rundlaufs (zwei Zonen, fünf Kalender an Zone 1, Lasten beider Zonen, neun Bauteile mit Fenster und Tür, vier Aufbauten
mit Schichten, Lage, Baujahr, Rahmenanteil, Wärmebrücke; Personen und Geräte passend zu den Nennwerten) wird nach gbXML und nach IFC
geschrieben, mit dem eigenen Leser gelesen (gbXML-Kalender über den Importweg der DA-1) und je Inventardatum verglichen. Erwartung
aus der Tafel: „gleich“, wo das Format das Datum in beiden Richtungen trägt; sonst „verlustbenannt“ (abweichend oder ohne Träger).
Ein Verlustdatum, das gleich zurückkommt, macht die Wache ebenfalls rot (Tafel nachführen).

| Format | gleich | abweichend | ohne Träger im Abbild | Felder |
|---|---|---|---|---|
| gbXML | 42 | 5 (A1, A5, B5, D8, F3 — alle „nicht“ bzw. „nur gelesen“) | 24 | 70 300 |
| IFC | 44 | 3 (D3, D4, D6 — nur an den benannten Grenzen) | 24 | 70 300 |

Ohne Träger im Abbild (beide Formate): A2, A3, A7, A8, A9, B10, B11, B19–B22, C8, C9, C10, E6, E7, F4–F7, F10, G1, G3, G4 — kein
getragenes Datum ist darunter. Die Luftströme zwischen den Zonen (G1) haben im Abbild keinen Träger; der Rundlauf nimmt sie auf,
sobald DA-4 sie ins Abbild bringt.

**Benannte Grenzen des IFC-Rundlaufs** (`Rundlaufgrenzen`, neu oder behoben macht rot):

| Datum, Bauteil | Grenze |
|---|---|
| D3, Bauteil 108 | Innendecke zum unbeheizten Raum (Ersatzaufbau): Neigung 180° kommt leer zurück, wirksam gilt die Vorgabe der Bauteilart |
| D4, Bauteil 103 | **Außenwand Ost: Azimut 90° kommt als 270° zurück — Befund, offen** |
| D4, Bauteil 105 | Innenwand zum unbeheizten Raum: Azimut kommt leer zurück |
| D4, Bauteil 109 | Innenwand innerhalb der Zone: Azimut kommt leer zurück |
| D6, Bauteil 109 | Innenwand mit derselben Zone auf beiden Seiten: eine Raumgrenze statt zwei Nachbarn |

## Tests

- Neu `EPOS.Kern.Tests/Datenaustauschinventar.cs` (Tafel und Ausnahmen) und `EPOS.Kern.Tests/DatenaustauschRundlaufWacheTests.cs` mit
  zwei Klassen: `DatenaustauschRundlaufWacheTests` (5 Tests: Tafel mit Welle und Grund, Modellfelder, Gegenprobe Modell und Spalte,
  Verlustliste ↔ Tafel, gemeinsamer Rundlauf) und `DatenaustauschSpaltenWacheTests` (2 Tests, Sammlung „Testdatenbank“: Spalten der
  zehn Tabellen, Gegenprobe gegen die echten Spalten von `Tab_Zone`).
- **Gegenprobe rot → grün:** `Tab_Zone.Vorheizzeit_H` aus der Tafel genommen → 4 von 7 rot (Spaltenwache „Tab_Zone.Vorheizzeit_H“,
  Modellwache und Verlustlistenwache „ZoneModel.Vorheizzeit_H“, Spalten-Gegenprobe); zurückgesetzt → 7 von 7 grün. Die Gegenproben
  im Test selbst (synthetisches Modell `ZoneMitNeuemFeld.Probefeld_Neu`, Spalte `Tab_Zone.Probespalte_DA7`) laufen grün mit, weil sie
  das Rot erwarten.
- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- `dotnet test EPOS.Kern.Tests --filter "Datenaustausch|Rundlauf|Inventar|Gbxml|Ifc|Export|Dokumentation|Repository"`: 866 grün,
  0 rot, 5 übersprungen (Bestand); vor diesem Protokoll gefahren, die Dokumentationswache danach erneut.

## Offen

- **IFC-Azimut der Außenwand Ost** (90° → 270°): Ursache in `IfcSchreiber` bzw. `IfcLeser`/`IfcAbbildBauer` suchen; der DA-3-Rundlauf
  verglich die Bauteillage nicht. Nach der Behebung die Grenze aus `Rundlaufgrenzen` nehmen.
- **Verlustliste nachführen** (Ergänzung oben) und die Abstimmungsliste entsprechend kürzen.
- **Beim Zusammenführen von DA-4:** die DA-4-Zeilen der Tafel (B10, B11, B20–B22, C8, C9, G1, G3, G4) auf die tragende Welle
  setzen, die Zählungen in `Die_Tafel_fuehrt_die_71_Daten_mit_Welle_und_Grund_je_Format` und im Rundlauf nachziehen und für die neuen
  Abbildfelder Vergleicher ergänzen (Luftströme G1 auch in die gemeinsame Probe).
- Die Kerntafel und diese Tafel stehen getrennt (Weg und Elemente im Kern, Felder und Welle im Test); eine neue Spalte braucht einen
  Eintrag hier, ein neuer Weg einen in der Kerntafel.

## Logbuch-Vorschlag

Keiner — die Welle ändert keine sichtbare Funktion (nur Prüfungen im Testprojekt).
