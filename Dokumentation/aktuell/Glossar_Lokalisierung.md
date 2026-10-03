# Glossar Lokalisierung DE → EN (Simulationsbereich)

**Paket 9 „Lokalisierung", Teilpaket L0.3.** Verbindliche Übersetzungen der Fachbegriffe des
Simulationsbereichs. Grundlage für alle englischen Ressourcenwerte in
`MyResource/Resource.en-US.resx` und in den Formular-Satelliten `*.en-US.resx`.

**Regel:** Ein deutscher Fachbegriff hat genau **eine** englische Entsprechung. Wer eine neue
Zeichenkette übersetzt, verwendet die Spalte „EN" unverändert — auch wenn eine andere Formulierung
im Einzelfall eleganter klänge. Konsistenz geht vor Eleganz.

**Terminologiequellen** in dieser Reihenfolge: EN 12831 / EN 15316 / EN 14511 (Heizungsanlagen,
Wärmepumpen), EN 12977 / EN 15316-4-3 (Solarthermie, Speicher), VDI 4640 (Erdwärme),
VDI 4655 (Lastprofile), EN 50524 / EN 61724 (Photovoltaik). Wo keine Norm greift, gilt der im
angelsächsischen Anlagenbau übliche Begriff.

---

## 1. Anlagentechnik — Erzeuger und Systeme

| DE | EN | Anmerkung |
|---|---|---|
| Wärmeerzeuger | heat generator | Oberbegriff; EN 15316. **Nicht** „heat producer" |
| Wärmepumpe | heat pump | EN 14511 |
| Heizkessel | boiler | EN 15316-4-1. **Nicht** „heating kettle" |
| Spitzenlastkessel | peak-load boiler | Kürzel SPK |
| BHKW / Blockheizkraftwerk | CHP unit | combined heat and power; im Fließtext „CHP unit", als Kürzel „CHP" |
| Solarthermie | solar thermal | EN 12977 |
| Solarkollektor | solar collector | |
| Photovoltaik | photovoltaics | Adjektiv „photovoltaic"; Kürzel PV bleibt PV |
| PV-Modul | PV module | |
| Stromspeicher | electricity storage | Kürzel SSP. Bestandsübersetzung der Registerkarte (`KONFIG_STROMSPEICHER`); „battery storage" wäre fachlich präziser, wird aber der Konsistenz mit der ausgelieferten Oberfläche wegen nicht geändert |
| Pufferspeicher | buffer storage | EN 12977; **nicht** „buffer tank" (das ist der Behälter, nicht die Funktion) |
| Warmwasserspeicher | DHW storage | domestic hot water |
| Gesamtsystem | overall system | |
| Kaskade | cascade | mehrere Erzeuger in Staffelung |
| Erzeugerkaskade | generator cascade | |

## 2. Wärmebedarf und Verbrauch

| DE | EN | Anmerkung |
|---|---|---|
| Wärmebedarf | heat demand | EN 12831 |
| Heizwärmebedarf | space heating demand | |
| Brauchwasser / Trinkwarmwasser (TWW) | domestic hot water (DHW) | VDI 6002; **nicht** „service water" |
| Prozesswärme | process heat | |
| Strombedarf | electricity demand | **nicht** „power demand" (power = Leistung) |
| Heizlast | heat load | EN 12831; Leistungsgröße in kW |
| Grundlast | base load | |
| Spitzenlast | peak load | |
| Gleichzeitigkeit | simultaneity | |
| Nutzenergie | useful energy | |
| Endenergie | final energy | |
| Primärenergie | primary energy | |

## 3. Quellen und Senken

| DE | EN | Anmerkung |
|---|---|---|
| Wärmequelle | heat source | EN 14511 |
| Wärmesenke | heat sink | |
| Quelle | source | Kurzform, nur wo der Kontext eindeutig ist |
| Senke | sink | dito |
| Außenluft / Aussenluft | outdoor air | EN 14511 (A = air); **nicht** „ambient air" (das ist Umgebungsluft) |
| Abluft | exhaust air | |
| Erdreich | ground | VDI 4640; **nicht** „soil" (bodenkundlicher Begriff) |
| Erdsonde | borehole heat exchanger | VDI 4640; Kürzel BHE |
| Erdkollektor | horizontal ground collector | |
| Grundwasser | groundwater | EN 14511 (W = water) |
| Abwärme | waste heat | |
| Sole | brine | EN 14511 (B = brine) |
| Sole-Wasser | brine-to-water | Bauart nach EN 14511, Schreibweise mit Bindestrichen |
| Luft-Wasser | air-to-water | |
| Wasser-Wasser | water-to-water | |
| Quellprofil | source profile | zeitlicher Verlauf der Quelltemperatur |
| Quelltemperatur | source temperature | |
| Erdreichmodell | ground model | |
| Ungestörte Erdreichtemperatur | undisturbed ground temperature | VDI 4640 |
| Entzugsleistung | extraction rate | VDI 4640, in W/m |
| Regeneration | regeneration | Wiederaufwärmung des Erdreichs |

## 4. Hydraulik und Temperaturen

| DE | EN | Anmerkung |
|---|---|---|
| Vorlauf | flow | Kurzform |
| Vorlauftemperatur | flow temperature | EN 12831; **nicht** „supply temperature" |
| Rücklauf | return | |
| Rücklauftemperatur | return temperature | |
| Spreizung | temperature spread | ΔT zwischen Vor- und Rücklauf |
| Temperaturdifferenz | temperature difference | |
| Massenstrom | mass flow rate | |
| Volumenstrom | volume flow rate | |
| Schichtung | stratification | Temperaturschichtung im Speicher |
| Ladung / Beladung | charging | Speicher |
| Entladung | discharging | |
| Ladeprioriät / Ladepriorität | charging priority | Reihenfolge der Speicherbeladung |
| Ladeordnung | charging order | |
| Zwischenkreis | intermediate circuit | |
| Heizkreis | heating circuit | |
| Sollwert | setpoint | |
| Grenztemperatur | limit temperature | |
| Obergrenze | upper limit | |
| Untergrenze | lower limit | |

## 5. Speicher

| DE | EN | Anmerkung |
|---|---|---|
| Speichervolumen | storage volume | in Litern |
| Nennvolumen | nominal volume | |
| Bereitschaftsverluste | standby losses | EN 12977; **nicht** „standby heat loss" im Plural-Kontext |
| Wärmeverluste | heat losses | |
| Speicherverluste | storage losses | |
| Höhe | height | |
| Durchmesser | diameter | |
| Dämmstärke | insulation thickness | |
| Wärmeleitfähigkeit | thermal conductivity | in W/(m·K) |
| Wärmedurchgangskoeffizient | heat transfer coefficient | U-Wert |
| Schichtenmodell | stratified model | |
| Speicherzone | storage layer | |

## 6. Betrieb, Kennzahlen und Ergebnisse

| DE | EN | Anmerkung |
|---|---|---|
| Deckungsgrad | coverage | Anteil am Wärmebedarf; auch „solar coverage" bei Solarthermie |
| Deckungsanteil | share of coverage | |
| Vollbenutzungsstunden | full-load hours | **nicht** „operating hours" (das sind Betriebsstunden) |
| Betriebsstunden | operating hours | |
| Laufzeit | runtime | |
| Starts / Taktungen | starts | Anzahl der Einschaltvorgänge |
| Jahresarbeitszahl (JAZ) | seasonal performance factor (SPF) | EN 15316-4-2 |
| Leistungszahl (COP) | coefficient of performance (COP) | EN 14511; COP bleibt COP |
| Kälteverhältnis (EER) | energy efficiency ratio (EER) | EN 14511; Kälteleistung durch elektrische Leistungsaufnahme im Kühlbetrieb, auch „Kälteleistungszahl"; im Schema die Spalte `COP` der Kühlkennlinie (Hinweis unter der Tabelle); EER bleibt EER |
| Kälteleistung | cooling capacity | EN 14511; die Spalte `Pkuehl` der Kühlkennlinie |
| Kühlbetrieb | cooling mode | Betriebsart einer reversiblen Wärmepumpe; Heizbetrieb = heating mode |
| Tagesbetriebsart | daily operating mode | Umschaltregel der reversiblen Wärmepumpe (K8a): je Tag Heizen oder Kühlen |
| Kühltag / Heiztag | cooling day / heating day | Tag, dessen Kältebedarf den Heizbedarf übersteigt bzw. nicht |
| Kältekreis | cooling circuit | Senke der Kälteseite (Persistenzwert `Kaeltekreis`) |
| Kältestrom | cooling electricity | Strom des Kühlbetriebs einschließlich Hilfsstrom |
| Hilfsstromanteil | auxiliary power share | Pumpen und Ventilatoren des Kältekreises, Anteil an der Verdichterarbeit |
| Jahresarbeitszahl Kälte (EER-Jahreswert) | seasonal EER | Kälte durch Kältestrom über das Jahr |
| Wirkungsgrad | efficiency | |
| Nutzungsgrad | utilisation ratio | über einen Zeitraum, im Unterschied zum momentanen Wirkungsgrad |
| Auslastung | utilisation | |
| Teillast | part load | |
| Nennleistung | rated output | EN 14511; **nicht** „nominal power" |
| Thermische Leistung | thermal output | |
| Elektrische Leistung | electrical output | |
| Erzeugte Wärme | heat generated | |
| Eingespeiste Energie | energy fed in | |
| Eigenverbrauch | self-consumption | EN 61724 |
| Autarkiegrad | self-sufficiency ratio | |
| Netzbezug | grid supply | |
| Netzeinspeisung | grid feed-in | |
| Überschuss | surplus | PV-Überschuss = PV surplus |
| Bilanz | balance | |
| Emissionen | emissions | |

**Die Spalte `COP` der Kühlkennlinie führt das Kälteverhältnis (EER)** — Prüfung K22 vom
23.09.2026 (Entscheid E27). Die Spalte in `Tab_Kenndaten_Kuehlung` und
`Tab_Kenndaten_Kuehlung_STAMM` wird nicht umbenannt, aber in Kern, Dialog und Bericht als **EER**
geführt und beschriftet — der eine Fall, in dem Spaltenname und Anzeigename bewusst
auseinandergehen ([Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 5.1,
Festlegung 4). Geprüft wurde an drei Stellen:

- **Importweg** (VDI 3805 Blatt 22, `WaermepumpenImport.KennlinienZu`): Ein Kennlinienkopf
  `710.09` mit Betriebsart 2 liefert je Wertzeile `710.91` die Temperatur (Feld 2), die
  Kälteleistung (Feld 3), die elektrische Leistungsaufnahme (Feld 4) und die Kennzahl (Feld 5); der
  Import schreibt Feld 5 in `COP` und Feld 3 in `Pkuehl`. In den Herstellerdateien unter
  `VDI-3805-Daten/WP-Daten/` ist Feld 5 in 14 067 von 14 198 Wertzeilen dieser Blöcke (99,1 %) auf
  10 % genau der Quotient **Kälteleistung durch Leistungsaufnahme**; in keiner Datei führt es das
  Wärmeverhältnis (Kälteleistung plus Leistungsaufnahme, geteilt durch die Leistungsaufnahme). Der
  Rest sind Rundungen bei kleiner Leistungsaufnahme und Einzelfehler. Die Richtlinie selbst liegt
  nicht im Repositorium; die Feldbedeutung ist aus den Daten erschlossen.
- **Testdatenbank:** Die sieben Katalogsätze mit Kühlkennlinie (174 Zeilen, jede Stützstelle
  zweimal gespeichert) liegen in Kaltwasserlage (Vorlauf 7 und 18 °C); die Kennzahl ist bei 18 °C
  größer als bei 7 °C und stimmt mit den Quellzeilen überein (Kälteleistung durch
  Leistungsaufnahme). Ein Satz trägt auf der Temperaturachse die Kaltwassertemperatur statt der
  Außentemperatur.
- **Befund am Importweg:** Nicht jeder Block mit Betriebsart 2 beschreibt einen Kühlbetrieb. 826
  von 2 641 solchen Blöcken der Herstellerdateien liegen in **Heizlage** (Vorlauf 35 bis 75 °C bei
  Quellentemperaturen bis 25 °C): Sie führen die Kälteleistung am Verdampfer **im Heizbetrieb**;
  ihre Kennzahl ist ebenfalls Kälteleistung durch Leistungsaufnahme, aber des Heizbetriebs — kein
  EER. Einzelne Datensätze vertauschen die Achsen. Der Import übernimmt heute jeden Block in die
  Kühltabelle; nach E27 (K22, Option (a)) wird eine solche Lage beim Import **benannt abgelehnt**,
  nie still als EER gelesen — eine Aufgabe der Stufe KU2.

## 7. Zeit, Profile und Daten

| DE | EN | Anmerkung |
|---|---|---|
| Ganglinie | load profile | **nicht** „curve"; bei Wetterdaten „time series" |
| Lastprofil | load profile | VDI 4655 |
| Jahresdauerlinie | load duration curve | sortierte Jahresganglinie |
| Stundenwerte | hourly values | 8760 Werte |
| Tageswerte | daily values | 365 Werte |
| Monatswerte | monthly values | 12 Werte |
| Wochenprofil | weekly profile | 168 Werte |
| Zeitschritt | time step | |
| Klimadaten | climate data | |
| Klimaregion | climate region | |
| Testreferenzjahr (TRY) | test reference year (TRY) | |
| Außentemperatur | outdoor temperature | |
| Globalstrahlung | global irradiation | EN 61724; Energie in kWh/m² |
| Einstrahlung | irradiance | Leistung in W/m² — Unterschied zu „irradiation" beachten |
| Heizgradtage | heating degree days | |
| Simulation | simulation | |
| Simulationslauf | simulation run | |
| Auswertung | evaluation | |
| Übersicht | overview | |
| Detail | detail | |

## 8. Oberfläche und Dialoge

| DE | EN | Anmerkung |
|---|---|---|
| Auswählen | Select | Schaltfläche/Label |
| Bearbeiten | Edit | |
| Einlesen / Importieren | Import | |
| Speichern | Save | |
| Übernehmen | Apply | |
| Abbrechen | Cancel | |
| Schließen | Close | |
| Löschen | Delete | |
| Neu | New | |
| Hinzufügen | Add | |
| Zuordnen | Assign | |
| Zuordnung | assignment | |
| Konfiguration | configuration | |
| Einstellungen | Settings | |
| Berechnen | Calculate | |
| Starten | Start | |
| Hinweis | Note | MessageBox-Titel |
| Warnung | Warning | |
| Fehler | Error | |
| Frage | Question | |
| Bitte auswählen | Please select | |
| Wirklich löschen? | Really delete? | |
| Keine Daten vorhanden | No data available | |
| Erfolgreich | Successful | |
| Ungültiger Wert | Invalid value | |
| Pflichtfeld | Mandatory field | |

## 9. Engine- und Protokollmeldungen

| DE | EN | Anmerkung |
|---|---|---|
| Rückfall / Rückfallwert | fallback | „ΔT-Rückfall" → „ΔT fallback" |
| Vorgabewert / Standardwert | default value | |
| Extrapolation | extrapolation | |
| Kennfeld | performance map | Herstellerkennfeld der Wärmepumpe |
| Stützstelle | data point | |
| Interpolation | interpolation | |
| Gültigkeitsbereich | valid range | |
| außerhalb des Kennfelds | outside the performance map | |
| nicht plausibel | not plausible | |
| Plausibilitätsprüfung | plausibility check | |
| abgebrochen | aborted | |
| übersprungen | skipped | |
| Berechnung fehlgeschlagen | calculation failed | |
| unvollständige Daten | incomplete data | |
| Protokoll | log | Engine-Protokollkanal aus Paket 8 |
| Meldung | message | |

---

## 10. Nicht übersetzen — Persistenzwerte

Die folgenden Zeichenketten sind **DB-Werte** und bleiben nach der Drei-Schichten-Regel
(Konzept 13.6) **immer deutsch**. Sie stehen in `Allgemein/DbWerte.cs` und dürfen niemals durch
einen Ressourcenverweis ersetzt werden:

`"Wärmepumpe"`, `"Heizkessel"`, `"Solarthermie"`, `"BHKW"`, `"Photovoltaik"`, `"Stromspeicher"`,
`"Beides"`, `"Heizung"`, `"Brauchwasser"`, `"Prozesswärme"`, `"Aussenluft"`, `"Erdreich"`,
`"Grundwasser"`, `"Abwärme"`, `"Pufferspeicher"`, `"PufferHeizung"`, `"PufferBrauchwasser"`,
`"Luft-Wasser"`, `"Sole-Wasser"`, `"Wasser-Wasser"` und die übrigen in `DbWerte.cs` geführten Werte.

Derselbe Wortlaut kann an anderer Stelle **Anzeigetext** sein — dort wird er sehr wohl übersetzt.
Maßgeblich ist nicht das Wort, sondern die Verwendung: geht der String in die Datenbank oder in
einen Vergleich gegen die Datenbank, bleibt er deutsch; geht er auf den Bildschirm, kommt er aus
der Ressource.

## 11. Schreibweisen

- Englische Beschriftungen in **Sentence case**: „Heat demand", nicht „Heat Demand".
  Ausnahme: feststehende Kürzel (COP, SPF, CHP, PV, DHW, TRY, BHE).
- Einheiten sind sprachneutral und bleiben unverändert: `kWh`, `kW`, `°C`, `m³`, `W/(m·K)`, `%`.
- Dezimaltrennzeichen wird in diesem Paket **nicht** umgestellt (`CurrentCulture` bleibt unangetastet,
  siehe Konzept 13.6 „Nicht Teil dieses Pakets").
- Tausendertrennung ebenfalls unverändert.
- Doppelpunkt am Ende von Feldbeschriftungen wird aus dem Deutschen übernommen:
  „Vorlauftemperatur:" → „Flow temperature:".

## 12. Bestandsübersetzungen mit Vorrang

Die sieben `KONFIG_*`-Schlüssel sind seit Längerem ausgeliefert. Ihre englischen Werte gelten
unverändert weiter, auch wo dieses Glossar eine andere Formulierung nennt — eine Umbenennung
der Registerkarten wäre eine sichtbare Änderung ohne fachlichen Gewinn:

| Schlüssel | DE | EN (Bestand) | Abweichung vom Glossar |
|---|---|---|---|
| `KONFIG_BHKW` | BHKW | CHP | Glossar: „CHP unit" — als Registerkartenbeschriftung ist die Kurzform richtig |
| `KONFIG_HEIZKESSEL` | Heizkessel | Boiler | — |
| `KONFIG_SOLARTHERMIE` | Solarthermie | Solar thermal energy | Glossar: „solar thermal" |
| `KONFIG_WAERMEPUMPE` | Wärmepumpe | Heat pump | — |
| `KONFIG_PHOTOVOLTAIK` | Photovoltaik | Photovoltaics | — |
| `KONFIG_STROMSPEICHER` | Stromspeicher | Electricity storage | Glossar folgt dem Bestand |
| `KONFIG_GESAMTSYSTEM` | Gesamtsystem | Overall system | — |

Für **neue** Schlüssel gilt ausschließlich die Tabellenspalte „EN" der Kapitel 1–9 und 13.

## 13. Gebäudehülle und Gebäudemodell

Die Begriffe des Gebäudedialogs in VDI-6007-Struktur und des Stundenmodells (Gebäudesimulation,
Stufe G1). Sie stehen **vor** den englischen Ressourcenwerten fest (Entscheid E28 zu U4), damit
derselbe Begriff nicht zwei Übersetzungen bekommt. Terminologiequellen: VDI 6007, EN 12831,
EN ISO 13790, EN ISO 10211 / 14683, EN 410.

| DE | EN | Anmerkung |
|---|---|---|
| Gebäudehülle | building envelope | Gruppe „Hülle" → „Envelope" |
| Bauteil | building component | Spaltenkopf der U·A-Tabelle |
| Außenwand | external wall | Bestand (`GEBK_LBL_U_AUSSENWAND`) |
| Dach | roof | |
| Bodenplatte | ground floor slab | Zeile der Grundfläche |
| Keller (unbeheizt) | unheated basement | Randbedingung der Bodenplatte |
| Randbedingung | boundary condition | Außenluft, Erdreich, Keller (§ 3 für Außenluft und Erdreich) |
| U-Wert | U-value | Kurzform von „heat transfer coefficient" (§ 5) in Spalten und Meldungen |
| Wärmebrücke | thermal bridge | EN ISO 10211 |
| Wärmebrückenverlustkoeffizient (ψ) | linear thermal transmittance | EN ISO 14683 |
| Wärmeleitwert (H) | heat loss coefficient | H_T, H_ve, H_ges in W/K; **nicht** „heat transfer coefficient" — das ist der U-Wert (§ 5) |
| Transmission / Transmissionswärmeverlust | transmission / transmission heat loss | H_T; EN 12831 |
| Lüftung / Lüftungswärmeverlust | ventilation / ventilation heat loss | H_ve; EN 12831 |
| H_ges gesamt | H_total | Formelzeichen bleibt, Index übersetzt |
| Luftwechselrate | air exchange rate | Bestand (`GEBK_LBL_LUFTWECHSEL`) |
| Nutzfläche | usable area | beheizte Netto-Grundfläche, Bezugsfläche des Stundenmodells (E13, E19) |
| Raumhöhe | room height | Bestand |
| Fensterfläche | window area | je Orientierung: „window area east" usw. |
| Fensterdurchlaßgrad (g-Wert) | window transmittance | Bestand (`GEBK_LBL_FENSTERDURCHLASS`); fachlich total solar energy transmittance (EN 410) |
| Rahmenanteil | frame fraction | EN ISO 13790 |
| Verschattungsfaktor | shading factor | EN ISO 13790 |
| Masseanteil außen | external mass fraction | Modellparameter VDI 6007 |
| Innenflächenfaktor | internal area factor | Modellparameter VDI 6007 |
| Strahlungsanteil (der Heizung) | radiative fraction (of heating) | |
| Heizleistungsgrenze | heating power limit | |
| Bauart | construction type | Bestand: „Light construction" usw. |
| Baualtersklasse | building age class | Bauzeitraum A … M (E47); Bestand `GIMP_FELD_BAUALTERSKLASSE`, Beschriftung `GEBK_LBL_BAUALTERSKLASSE`, Listenspalte `KFLT_SP_BAUALTERSKLASSE` |
| bis 1859 / 1860 bis 1918 / … / ab 2021 | up to 1859 / 1860 to 1918 / … / from 2021 | Texte der Klassen `GEB_BAK_A` … `GEB_BAK_M`: „X bis Y" → „X to Y" |
| Einteilung nach der Deutschen Wohngebäudetypologie des IWU | classification according to the German residential building typology of IWU | Quellenzeile `GEB_BAK_QUELLE`; Eigennamen und Jahreszahlen bleiben |
| Baujahr | year of construction | die Jahreszahl; ist sie gesetzt, folgt die Baualtersklasse ihr (`GEBK_LBL_BAUJAHR`); nicht „construction period" |
| Energiestandard | energy standard | freiwilliges Feld neben der Klasse (`GEBK_LBL_ENERGIESTANDARD`, `GEBA_LBL_ENERGIESTANDARD`); gespeichert der Code |
| Effizienzhaus / Effizienzgebäude | Efficiency House / Efficiency Building | BEG-Förderstufen (`GEB_ES_EH*`); die Zahl bleibt |
| Passivhaus bzw. EnerPHit | Passive House or EnerPHit | `GEB_ES_PASSIVHAUS` |
| Niedrigenergiehaus | low-energy house | `GEB_ES_NIEDRIGENERGIE` |
| Nullemissionsgebäude | zero-emission building | `GEB_ES_NULLEMISSION` |
| teilsaniert / saniert | partially refurbished / refurbished | `GEB_ES_TEILSANIERT`, `GEB_ES_SANIERT`; „GModG" bleibt als Abkürzung |
| wie Baualtersklasse (unsaniert) | as building age class (unrefurbished) | der leere Eintrag `GEB_ES_KEINER` |
| Bauweise | thermal mass | Wh/K bzw. Wh/(m²K); EN ISO 13790 |
| Raumtemperatur / Raumlufttemperatur | indoor temperature / indoor air temperature | |
| Nachtabsenkung von / bis | night setback from / until | Beginn und Ende der Nachtzeit je Gebäude, volle Stunde 0 … 23 (`GEBK_LBL_NACHT_BEGINN`, `GEBK_LBL_NACHT_ENDE`); beide leer = 22 bis 6 Uhr |
| operative Temperatur | operative temperature | VDI 6007, EN ISO 7726 |
| Kühlbedarf | cooling demand | |
| Rechenweg | calculation method | Schalter „Rechenweg" (E20) |
| Rechenmodell | calculation model | |
| Tagesbilanz | daily balance | der Altweg |
| Bestandsweg | legacy method | „Tagesbilanz (Bestandsweg)" → „Daily balance (legacy method)" |
| Vorgabe (eines leeren Felds) | default value | § 8; Platzhalter „Vorgabe 0,3" → „Default value 0.3" |
| Skalierungsfaktor | scaling factor | Hochrechnung nach E8 |
| Infiltration | infiltration | Luftwechsel durch Undichtheiten (Stufe G2) |
| Nutzerlüftung | occupant ventilation | Luftwechsel durch Fensterlüftung (Stufe G2) |
| Sommerlüftung | summer ventilation | erhöhter Luftwechsel an warmen Tagen (Stufe G2) |
| Heizsollwert | heating setpoint | untere Kante des Sollwertbands im Bild „Raumtemperatur" |
| obere Raumtemperatur | maximum indoor temperature | Bestandsfeld `Maximaleraumtemperatur`; obere Kante des Sollwertbands |
| Überhitzungsstunden | overheating hours | Stunden der Nutzungszeit über der oberen Raumtemperatur |
| Vergleich der Rechenwege | comparison of calculation methods | Tabelle im Bedarfsdialog, bis Stufe GA |
| Baustoff | building material | Katalog „Baustoffe" (Stufe G3) |
| herstellerneutral | manufacturer-neutral | Baustoff ohne Hersteller (Norm- oder Richtwert) |
| Bauteilaufbau | construction build-up | Katalog „Bauteilaufbauten"; Schichten innen → außen |
| Bauteilart | component type | Persistenzwerte `AUSSENWAND` … bleiben (§ 10) |
| Innenwand / Decke / Tür / Fenster | internal wall / ceiling / door / window | Bauteilarten |
| Vorhangfassade | curtain wall | Bauteilart mit U- und g-Wert |
| Schicht | layer | Zeile des Schichtenrasters |
| Rohdichte (ρ) | density | kg/m³ |
| spezifische Wärmekapazität (c_p) | specific heat capacity | J/(kg·K), nicht kJ |
| Wärmedurchlasswiderstand (R) | thermal resistance | m²K/W; DIN EN ISO 6946 |
| Wärmeübergangswiderstand innen/außen (R_si/R_se) | internal/external surface resistance | DIN EN ISO 6946, Tabelle 7 |
| ruhende Luftschicht | unventilated air layer | DIN EN ISO 6946, Tabelle 8 |
| flächenbezogene Wärmekapazität | areal heat capacity | Σ ρ·c_p·d in kJ/(m²K); Spalte „Kapazität" → „Heat capacity" |
| Bezugsperiode (T_BT) | reference period | VDI 6007 Blatt 1, Gl. (10a)–(10d) |
| Bauteilweg | component method | Rechnung aus Schichten nach VDI 6007 Blatt 1 |
| Zone | zone | Zone eines Gebäudes (Tab_Zone); in G3 höchstens eine |
| Klassenweg | class method | Rechnung aus den U-Wert-Gruppen und der Bauweise |
| Flächenschlüssel | area key | Anteil der Zonenfläche an der Nutzfläche des Gebäudes |
| Hochrechnung (auf die Projektfläche) | scaling up (to the project area) | Übernahme einer Zone mit dem Skalierungsfaktor |
| Projektkopie | project copy | die Zeile eines Gebäudes im Projekt (Tab_Gebaeude) |
| Hülle und Zonen | envelope and zones | Knopf des Gebäudedialogs |
| Katalogsatz | catalogue record | ein Satz des Gebäudekatalogs |

### Konditionierungsprofile

Die Begriffe des Reiters „Konditionierung" im Gebäude-Katalogeditor: Vorgabe-Matrix, Kalender, Vorlagen und
Aufheizoptimierung (Konzept [Konditionierungsprofile](Konzept_Konditionierungsprofile_EPOS-Plan.md) 7.7, Entwurf
[KP2](../ueberholt/2026-09-29_Entwurf_KP2.md) 5; Ressourcen `KOND_LBL_*`, `KOND_BTN_*`, `KOND_TXT_*`). Sie stehen
**vor** den englischen Ressourcenwerten fest (Entscheid E28 zu U4, wie oben). Was in dieser Tafel schon steht, gilt
unverändert weiter: Infiltration, Nutzerlüftung, Sommerlüftung, Heizsollwert, obere Raumtemperatur, Überhitzungsstunden,
Nachtabsenkung von / bis, Vorgabe, Zone, Katalogsatz, Tagesbilanz und Rechenweg. Die Vorlagennamen („Wohnen", „Büro",
„Schule") sind Daten und bleiben deutsch (§ 10); übersetzt wird ihre **Nutzung** als Anzeigewert.

| DE | EN | Anmerkung |
|---|---|---|
| Konditionierung | conditioning | Oberbegriff für Sollwerte, Lüftung und innere Gewinne einer Zone; Reiter „Konditionierung" → „Conditioning" (`KOND_LBL_REITER`) |
| Vorgabe-Matrix | defaults matrix | die Tabelle des Reiters (fünf Größen × Nennwert, Tag, Nacht, Wochenende, Ferien, Saison), aus der der Generator die Kalender macht; Kurzform „Matrix" → „matrix" |
| Größe (der Konditionierung) | quantity | eine der fünf Größen Heizen, Kühlen, Lüftung, Geräte, Personen; je Größe ein Kalender und eine Vorlagenliste; die Kennwörter `HEIZSOLL` … `PERSONEN` sind Persistenzwerte (§ 10) |
| Heizen | heating | Größe: Heizsollwert in °C, auch „aus"; Spalte „Heizen °C" (`KOND_LBL_SPALTE_HEIZEN`); Heizsollwert → heating setpoint (oben) |
| Kühlen | cooling | Größe: Kühlsollwert in °C, auch „aus"; wirkt nur mit Kühlbetrieb im Projekt (§ 6); Spalte „Kühlen °C" (`KOND_LBL_SPALTE_KUEHLEN`) |
| Kühlsollwert | cooling setpoint | wie Heizsollwert (oben, § 4); die Bestandstexte `GEBK_LBL_KUEHL_SOLLWERT` und `GEBK_ZEILE_KUEHLUNG_*` schreiben noch „set point" |
| Lüftung | ventilation | Größe: Nutzerlüftung in 1/h, die Infiltration bleibt konstant darunter (Begriffe oben); Spalte „Lüftung 1/h" (`KOND_LBL_SPALTE_LUEFTUNG`) |
| Geräte | equipment | Größe: Geräte und Anlage einschließlich Beleuchtung, als Anteil 0 … 100 % eines Nennwerts in W; Spalte „Geräte W bzw. %" → „Equipment W or %" |
| Personen | people | Größe: Anwesenheit der Personen als Anteil eines Nennwerts (Zahl × 70 W, sensible Wärme); Spalte „Personen W bzw. %" → „People W or %" |
| Anwesenheit | occupancy | Anteil 0 … 100 % im Kalender der Größe Personen; Zeitstruktur „wie Anwesenheit" → „as occupancy" |
| Nennwert | nominal value | Zeile der Matrix: der Bezugswert in W (Geräte, Personen) bzw. die Infiltration in 1/h, auf den sich die Anteile beziehen; **nicht** „Nennleistung" (→ rated output, § 6) |
| Interne Wärmegewinne | internal heat gains | Summe aus Geräten und Personen; Feld `Interne_Waermegewinne`, Bestand `GEBK_LBL_WAERMEGEWINNE` |
| Jahresmittel | annual mean | Mittel über die 8760 Stunden; Herleitungszeile der Gewinne unter der Matrix (`KOND_TXT_JAHRESMITTEL`) |
| Maximalraumtemperatur | maximum indoor temperature | Zusatzzeile neben der Matrix: die Grenze der Überhitzungsstunden (Feld `Maximaleraumtemperatur`); dieselbe Größe wie „obere Raumtemperatur" (oben), Bestand `GEBK_LBL_MAXTEMPERATUR` schreibt „Maximum room temperature" |
| Kalender (abgeleitet / angelegt) | calendar (derived / created) | Stundenreihe (8760 Werte) einer Größe je Zone; abgeleitet = der Lauf erzeugt ihn aus der Matrix, angelegt = Zeilen in der Datenbank, die Matrix ist dann nur Vorgabe |
| Kalenderkarte | calendar card | einklappbare Karte je Größe unter der Matrix mit einer Zustandszeile: „aus der Matrix" → „from the matrix", „aus Vorlage Büro" → „from template Büro", „angelegt, 3 eigene Perioden" → „created, 3 own periods" |
| Grundangabe | base value | Ebene 1 des Kalenders: ein Wert oder „aus" |
| Standardwoche | standard week | Ebene 2 des Kalenders: 168 Zellen von Montag 00:00 bis Sonntag 23:00; „Wochenprofil" (§ 7) bleibt der allgemeine Begriff |
| Wochenraster | weekly grid | der Baustein zur Standardwoche: 7 × 24 Zellen mit dem Zellzustand „aus" |
| Periode | period | Ebene 3 des Kalenders: Beginn und Ende (Jahrestage im Gemeinjahr) oder eine Feiertagsregel, mit Rang und Art; **nicht** „Bezugsperiode" (§ 13 oben) |
| Rang | rank | 1 … 999, eindeutig je Kalender; die ranghöchste Periode eines Tages gilt |
| Art (einer Periode) | type | Zeitraum, Ferien, Feiertag, Betriebspause → date range, holidays, public holiday, operating break; die Persistenzwerte `ZEITRAUM`, `FERIEN`, `FEIERTAG`, `BETRIEBSPAUSE` bleiben deutsch (§ 10) |
| Betriebspause | operating break | Art einer Periode: „aus" außerhalb der Saison; der Generator bildet Heiz- und Kühlperiode darauf ab |
| eigene Periode | own period | von Hand angelegt, nicht Teil des Matrixbereichs; im Kartenzustand „angelegt, 3 eigene Perioden" |
| Ausnahmetag | exception day | ein einzelner Tag mit eigener Angabe, angelegt als Periode über einen Tag; bleibt wie die eigenen Perioden beim erneuten Anwenden der Matrix |
| Matrixbereich | matrix area | Standardwoche, Ferien- und Saisonperioden eines Kalenders — das ersetzt „Matrix erneut anwenden" |
| Heizperiode | heating period | Saison der Heizspalte: Start und Ende als Datum im Gemeinjahr; innerhalb wird geheizt, außerhalb steht die Größe auf „aus"; leer = ganzjährig |
| Kühlperiode | cooling period | dasselbe für die Kühlspalte |
| Saison | season | Zeile der Matrix für Heiz- bzw. Kühlperiode (Start, Ende); leer = ganzjährig → „year-round" |
| Nachtauskühlung | night purge ventilation | erhöhter Luftwechsel im Nachtfenster der Lüftungsspalte, nur bedingt wirksam (wie die Sommerlüftung: Raumluft über der Schwelle, Außenluft mindestens ΔT kühler); Kernmeldungen `SIMENG_KOND_NACHTKUEHL_*` schreiben noch „night cooling" |
| Nachtauskühlstunden | night purge ventilation hours | Kennzahl `Nachtauskuehlstunden_H` in Bedarfsdialog, Ergebnis und Bericht |
| Sommerlüftungsstunden | summer ventilation hours | Kennzahl `Sommerlueftungsstunden_H`, neben den Nachtauskühlstunden |
| Nachtfenster | night window | die Nachtzeit einer Spalte (von / bis, volle Stunde 0 … 23), leer = das der Heizspalte; bei Heizen „Nachtabsenkung von / bis" (oben) |
| Aufheizzeit | preheat time | Dauer der Aufheizrampe vor einem Sprung des Heizsollwerts nach oben (Stufe KP3) |
| Aufheizleistung | preheat power | die Leistung, mit der die Aufheizung bemessen wird (Stufe KP3) |
| Aufheizreserve | preheat reserve | der Zuschlag auf die stationäre Last bei der Bemessung der Aufheizleistung (Projekteinstellung, Stufe KP3); Feld „Aufheizreserve ρ" → „Preheat reserve ρ" in %, 1 … 100, Vorgabe 20 %, gespeichert als Anteil |
| Aufheizoptimierung | preheat optimisation | Projekteinstellung neben Kühlbetrieb und Anlagenkopplung: Schalter, Bemessung, Art täglich oder fest (Stufe KP3); Schreibweise „optimisation" wie „utilisation" (§ 6); Schalter „Aufheizoptimierung rechnen" → „Calculate preheat optimisation" (`SIMKONF_AUFH_*`) |
| Bemessung (der Aufheizzeit) | design basis | woran die längste Aufheizzeit bemessen wird: „kälteste Stunde" → „coldest hour" (Vorgabe), „kälteste Stunde − ΔT_K" → „coldest hour − ΔT_K"; die Persistenzwerte `STUNDE`, `STUNDE_ABZUG` bleiben (§ 10); Bemessungsfall → design case |
| Abzug ΔT_K | deduction ΔT_K | um wie viel K die Bemessung (b) kälter rechnet als die kälteste Stunde (0 … 10 K, Vorgabe 2 K) |
| Art der Aufheizzeit | preheat time mode | „täglich" → „daily" (Vorgabe: jeder Tag nach seiner Außentemperatur), „fest" → „fixed" (jeder Tag so lange wie der Bemessungsfall); die Persistenzwerte `TAEGLICH`, `FEST` bleiben (§ 10) |
| Aufschlag (der Aufheizzeit) | surcharge | Projektvorgabe in Stunden und in Prozent, um die jede ermittelte Rampe länger wird, es gilt der größere Wert (Stufe KP3); Felder „Aufschlag (h)" → „Surcharge (h)", „Aufschlag (%)" → „Surcharge (%)" (`SIMKONF_AUFH_AUFSCHLAG_*`) |
| Aufheizzeit manuell | manual preheat time | je Gebäude eingegebene Aufheizzeit in h, dritte Art neben „täglich" und „fest" → „manual"; Persistenzwert `MANUELL` bleibt (§ 10) |
| Vorschlag | suggestion | Wert neben einem Feld, den der Anwender übernehmen kann, hier die bemessene Aufheizzeit und die Spanne nach Bauart; „Übernehmen" → „Apply" (§ 8); nicht „proposal" |
| Herleitungszeile | derivation line | leise Zeile unter einem Feld oder Abschnitt, die sagt, woraus ein Wert folgt — bei der Aufheizoptimierung je Gebäude „t_auf,max … bei … · P_auf … · C_w …"; das Wort selbst steht in keiner Beschriftung |
| Ferien | holidays | Zeile der Matrix und Art der Periode; Bestand „Holiday setpoint (all day)" (`GEBK_LBL_SOLL_FERIEN`); **nicht** „vacation" |
| Ferienzeitraum | holiday period | datierter Zeitraum (`Ferienbeginn_1` … `Ferienende_4`) des Gebäudes, gilt für alle Spalten; Datum im Gemeinjahr; Bestand „Holiday start" / „Holiday end" |
| Feiertag | public holiday | wie § 14 (Tagtyp: Sonn-/Feiertag); die neun bundeseinheitlichen Feiertage stehen als Regel und wirken „wie Sonntag" → „as Sunday"; Länderfeiertage sind gewöhnliche Perioden |
| Bezugsjahr | reference year | das Jahr, dessen Wochentage und Feiertage Vorschau, Teppichbild und Lauf verwenden; nicht „Bezugsperiode" (§ 13 oben) |
| Gemeinjahr | common year | Jahr mit 365 Tagen ohne 29.02.; Kalender und Ferienzeiträume rechnen darin; „TT.MM." → „DD.MM." |
| Zeitfenster | time window | Werkzeug der Karte: Tage, von, bis, Wert — der Weg zur Standardwoche ohne Zellenarbeit |
| Zeitstruktur übernehmen | apply time structure | Werkzeug der Karte für Kühlen, Lüftung und Geräte: „wie Heizung" → „as heating" oder „wie Anwesenheit" → „as occupancy"; Übernehmen → Apply (§ 8) |
| Teppichbild | carpet plot | Jahresbild der Vorschau: Tage × Stunden, Farbe = Wert, „aus" als eigene Fläche; die Unterschrift nennt das Bezugsjahr |
| aus (Zellzustand) | off | Zustand einer Zelle bei Heizen und Kühlen und in der Standardwoche jeder Größe (Lüftung 0 1/h, Anteile 0 %); das Kennwort „aus" in gespeicherten Wochen (`KOND_WOCHE_AUS`) bleibt deutsch (§ 10) |
| Kalender anlegen | create calendar | Knopf der Matrix: schreibt die Kalender aus der Matrix; „Verwerfen" → „Discard" löscht sie und kehrt zur Matrix zurück |
| Matrix erneut anwenden | reapply matrix | ersetzt nur den Matrixbereich, eigene Perioden und Ausnahmetage bleiben; die Rückfrage nennt beides |
| Zurücknehmen | undo | nimmt den letzten Schritt des Arbeitsstands zurück |
| Vorlage | template | vorbefüllter Kalender einer Größe, ausgeliefert oder eigen; Auswahlliste je Kalenderkarte, Zeile „Vorlage" der Matrix |
| Vorlage übernehmen | apply template | wirkt auf Gebäude, Zone oder Katalogbau und nur auf eine Größe (Übernehmen → Apply, § 8); Nennwert und Saison des Ziels bleiben |
| Vorlage in allen Größen übernehmen | apply template to all quantities | Abkürzung in der Kopfzelle der Zeile „Vorlage" (E57): die Liste „alle Größen" → „all quantities" führt jeden Namen aus mindestens einer Liste; eine Rückfrage für alle Größen, eine Größe ohne gleichnamige Vorlage bleibt; kein Satzbegriff |
| Als Vorlage speichern | save as template | Knopf der Karte: Name, Beschreibung, Nutzung; ohne Nennwert und Saison; wird sofort gespeichert |
| Vorlagen verwalten | manage templates | Blatt im Katalogeditor, aus jeder Kalenderkarte; fünf Listen mit Umschalter der Größe |
| Nutzung (einer Vorlage) | type of use | Wohnen, Büro, Schule, Sonstige → residential, office, school, other; Anzeigewerte, die Persistenzwerte `WOHNEN`, `BUERO`, `SCHULE`, `SONSTIGE` bleiben deutsch (§ 10); vgl. Nutzungsart (§ 14) |
| ausgeliefert / eigen | delivered / own | Vorlage oder Katalogsatz der Auslieferung (`ReadOnly`) bzw. vom Anwender angelegt |
| gesperrt (Schloss) | locked | Sätze und Vorlagen der Auslieferung; „nur lesbar" → „read-only"; der Lesemodus nennt den Grund |
| Speichern unter | save as | legt eine bearbeitbare Kopie unter neuem Namen an; Bestand `GEBK_HINWEIS_SPEICHERN_UNTER` |
| Duplizieren | duplicate | Kopie einer ausgelieferten Vorlage oder eines Katalogsatzes; Kernmeldungen „Duplicate…" |
| Umbenennen | rename | eigene Vorlagen in der Verwaltung |
| Kopieren nach … | copy to … | Handlung der Vorlagenverwaltung: eine Vorlage als eigene Vorlage einer anderen Größe — Heizen → Kühlen, Geräte ↔ Personen |
| Komfortsollwert | comfort setpoint | der Kühlsollwert, den „Kopieren nach …" von Heizen nach Kühlen in jede Zelle setzt, deren Heizsollwert den Tagwert der Vorlage erreicht (Vorgabe 26 °C) |
| Absenksollwert | setback setpoint | der Kühlsollwert, den „Kopieren nach …" von Heizen nach Kühlen in die Absenkzeiten setzt – jede Zelle mit Heizsollwert unter dem Tagwert (Vorgabe 28 °C, wählbar „aus“; nie unter dem Komfortsollwert) |
| Tagwert | day value | der Heizsollwert der Zeile „Tag“ einer Vorlage, ohne Tagzeile ihr höchster Heizsollwert – die Schwelle der Absenkzeit bei „Kopieren nach …" |
| Katalogbau | catalogue building | ein Gebäude des Gebäudekatalogs, Träger von Matrix und Kalendern; „Katalogsatz" (oben) ist der allgemeine Begriff |
| Aus dem Katalog erneut übernehmen | reapply from the catalogue | Knopf im Reiterkopf (nur im Projekt): ersetzt die Gebäudeebene samt Bestandszellen, die Zonen bleiben |
| erben / vom Gebäude | inherit / from the building | Zone: eine leere Zelle erbt den Gebäudewert, Platzhalter „Vorgabe …" → „Default value …" (Vorgabe, oben); Kartenzustand „vom Gebäude" → „from the building" |
| Vom Gebäude übernehmen und anpassen | copy from building and adjust | Zone: legt eine eigene Kopie des Gebäudekalenders an |
| Kalender im Einzelnen | calendar details | Knopf der Kalenderkarte: klappt Grundangabe, Wochenraster, Zeitfenster, Periodenliste, Werkzeuge und Teppichbild auf |
| In den Kalender übernehmen | apply to calendar | Knopf der Gruppe „Wärmeübergabe": das Sollwert-Zeitprogramm wird die Standardwoche des Heizkalenders (Übernehmen → Apply, § 8) |

Abweichende Bestandstexte: Die Kernmeldungen `SIMENG_KOND_*` (Stufe KP1) schreiben an einigen Stellen „heating season",
„night cooling", „input matrix" und „set point", der Gebäudedialog (`GEBK_*`) „set point" und „Maximum room temperature".
Für neue Schlüssel gilt diese Tafel (§ 12).

## 14. Trinkwarmwasser und Zapfprofil

Die Begriffe des Zapfprofilgenerators (Dialog „Brauchwasser-Zapfprofil", Ressourcen `ZPG_*`).
Sie stehen vor den englischen Ressourcenwerten fest (Umsetzungskonzept Zapfprofilgenerator 5.3).
Terminologiequellen: EN 12831-3, EN 15316-3, EN 806.

| DE | EN | Anmerkung |
|---|---|---|
| Zapfprofil | draw-off profile | Profil der Warmwasserentnahme; im Titel „domestic hot water draw-off profile" |
| Zapfprofilgenerator | draw-off profile generator | |
| Zapfung | draw-off | die entnommene Warmwasserenergie |
| Zapfstelle | tap | Bilanzgrenze „an der Zapfstelle" → „at the tap" |
| Zirkulation | circulation | Zirkulationsleitung: circulation pipe |
| Teilreihe | partial series | „Zirkulation als eigene Teilreihe" |
| Nutzungsart | type of use | Katalogeintrag des Generators |
| Nutzungszone | usage zone | |
| Bezugsgröße | reference quantity | Menge in der Bezugsart der Nutzungsart |
| Bedarfsniveau | demand level | niedrig/mittel/hoch → low/medium/high |
| Tagesgang | daily profile | Stundenanteile eines Tages |
| Tagesgangsatz | daily profile set | vier Tagesgänge je Tagtyp |
| Tagtyp | day type | Werktag, Samstag, Sonn-/Feiertag, Ruhetag → working day, Saturday, Sunday/public holiday, rest day |
| Jahresgang | annual profile | zwölf Monatswerte |
| Bilanzgrenze | balance boundary | |
| Auslegung | design | „Auslegung…" → „Design…" |
| Summenlinie | cumulative curve | Summenlinienverfahren der Speicherauslegung |
| Bedarfstag | design day | Zapftag der Auslegung (Konstruktor, Referenztag, Normtag); **nicht** „demand day" |
| Wertepaarkurve | pairs curve | Speichervolumen über Erzeugerleistung, Erweiterung des Nachweisverfahrens |
| Verfahrensvergleich | method comparison | Speicherauslegung nach mehreren Verfahren, Regel „größtes Volumen" |
| Konstruktor | constructor | „Bedarfstag konstruieren…" → „Construct design day…" |
| Bedarfskennzahl | demand index | Kennzahl der Wohnungstabelle |
| Nenninhalt | nominal capacity | Speicher; **nicht** „nominal volume" (das ist Nennvolumen) |
| Katalogversion | catalogue version | Schreibweise „catalogue" wie im Bestand |
