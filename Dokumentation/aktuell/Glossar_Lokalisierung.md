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

Für **neue** Schlüssel gilt ausschließlich die Tabellenspalte „EN" der Kapitel 1–9.
