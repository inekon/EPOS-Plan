# Referenzlauf-Protokoll

**Zeitpunkt:** 03.09.2026 12:16:57

**Quelle (produktiv, nur gelesen):** `P:\pa0\Quelle\Kenndaten.sqlite`

**Arbeitskopie (beschrieben):** `P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite`

**Zielordner:** `P:\wt4\Referenzlaeufe\2026-09-03_M4_nach-Merge4`

**Gesamtdauer:** 00:00:10  |  **Timeout je Projekt:** 300 s

**Warnungen:** 26  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:00 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:00 | 21 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:00 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:00 | 21 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:00 | 22 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:00 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:00 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:00 | 26 | OK |
| 1026 | Beispiel WP WG 1 | Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | per --projekte vorgegeben | 00:00 | 29 | OK |
| 1028 | Beispiel WP WG mit Erdwärme | Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | per --projekte vorgegeben | 00:00 | 29 | OK |
| 1029 | Beispiel WP WG 1 - Erdwärme | Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | per --projekte vorgegeben | 00:00 | 29 | OK |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:00 | 22 | OK |
| 1039 | Wärmepumpe WG - BHKW | Tools: Wärmepumpe / Anlagen: BHKW | per --projekte vorgegeben | 00:00 | 18 | OK |
| 1043 | Booster-Kette mit Kombi-Speicher | Tools: Wärmepumpe, Heizkessel, Solarthermie, Photovoltaik, Stromspeicher / Anlagen: WP,Kessel,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:00 | 34 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: P:\merge4\src
Zielordner:    P:\wt4\Referenzlaeufe\2026-09-03_M4_nach-Merge4
Timeout je Projekt: 300 s

Quelle vorgegeben (--quelle): P:\pa0\Quelle\Kenndaten.sqlite
WARNUNG: Neben der Quelldatenbank liegt Kenndaten.sqlite-wal. Die Datenbank wurde nicht sauber geschlossen; die Kopie enthaelt nur den eingecheckpointeten Stand.
WARNUNG: Neben der Quelldatenbank liegt Kenndaten.sqlite-shm. Die Datenbank wurde nicht sauber geschlossen; die Kopie enthaelt nur den eingecheckpointeten Stand.
Arbeitskopie angelegt: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite (64 MB)
DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite

Schema-Migration der Arbeitskopie ...
  P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
  Zeitpunkt: 03.09.2026 12:16:47
  Schemastand vorher: 61   (Zielstand 63)
  Schritt 62  PV-Anlagenparameter (PV_WrWirkungsgrad, PV_Systemverluste) an Tab_Energieanlagen anlegen (Paket A, Stufe E1.3): OK
          - Tab_Energieanlagen.PV_WrWirkungsgrad: angelegt
          - Tab_Energieanlagen.PV_Systemverluste: angelegt
          - 62: 2 Spalte(n) (PV_WrWirkungsgrad, PV_Systemverluste) an Tab_Energieanlagen sichergestellt. KEIN DML: beide Spalten bleiben NULL, und NULL heisst 0,95 (Wechselrichter-Wirkungsgrad) bzw. 0 % (Systemverluste) - genau der bisher fest verdrahtete Rechenweg. KEIN Rechenergebnis aendert sich.
  Schritt 63  PV-Modellwahl (PV_Modell, Wechselrichterangaben, Technologie, Degradation) anlegen (Paket B, Stufe E2): OK
          - Tab_Energieanlagen.PV_Modell: angelegt
          - Tab_Energieanlagen.PV_WrNennleistungKw: angelegt
          - Tab_Energieanlagen.PV_WrEta10: angelegt
          - Tab_Energieanlagen.PV_WrEta50: angelegt
          - Tab_Energieanlagen.PV_WrEta100: angelegt
          - Tab_PV.Technologie: angelegt
          - Tab_PV_STAMM.Technologie: angelegt
          - Tab_ProjektPhotovoltaik.Degradation: angelegt
          - 63: 8 Spalte(n) sichergestellt - PV_Modell, PV_WrNennleistungKw, PV_WrEta10, PV_WrEta50, PV_WrEta100 an Tab_Energieanlagen, Technologie an Tab_PV und Tab_PV_STAMM, Degradation an Tab_ProjektPhotovoltaik. KEIN DML: alle acht Spalten bleiben NULL. NULL heisst bei PV_Modell "Modell EINFACH", also der Rechenweg aus Paket A, und bei der Degradation 0 %/a. KEIN Rechenergebnis aendert sich.
  Schemastand nachher: 63   (Zielstand 63)
Migration: ERFOLG (Zielstand 63).

Projektlandschaft wird gelesen ...
26 Projekte in Tab_Projekt.

Gewaehlte Referenzprojekte (14):
  - Projekt 1007 "Laurentiuskirche"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben
  - Projekt 1008 "Heinestr 15"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1011 "test1"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben
  - Projekt 1017 "WP_PV-Speicher"
      Ausstattung: Tools: BHKW, Heizkessel, Stromspeicher | Anlagen: WP,Batterie,Kessel,BHKW
      Grund:       per --projekte vorgegeben
  - Projekt 1018 "BHKW Test München"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1021 "TestSpeichernUnter"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP,Puffer | Quellspeicher(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1023 "Wöhler - Test1"
      Ausstattung: Tools: Wärmepumpe, Heizkessel | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1024 "Wöhler - Test2"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, BHKW | Anlagen: WP,Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1026 "Beispiel WP WG 1"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1028 "Beispiel WP WG mit Erdwärme"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1029 "Beispiel WP WG 1 - Erdwärme"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1030 "Referenz BHKW-Kaskade (Regressionstest)"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1039 "Wärmepumpe WG - BHKW"
      Ausstattung: Tools: Wärmepumpe | Anlagen: BHKW
      Grund:       per --projekte vorgegeben
  - Projekt 1043 "Booster-Kette mit Kombi-Speicher"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, Solarthermie, Photovoltaik, Stromspeicher | Anlagen: WP,Kessel,Puffer | Quellspeicher(WP)
      Grund:       per --projekte vorgegeben

--- Projekt 1007 (Laurentiuskirche) ---
      | [12:16:48] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:48] Simulation startet fuer Projekt 1007 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Ablytek 6MN6A270": T_NOCT ist mit 9.340 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Ablytek 6MN6A275": T_NOCT ist mit 9.420 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [12:16:48] Simulation beendet, Ergebnis-Kopf-ID 208.
      | [12:16:48] Projekt 1007: 29 CSV-Dateien, 99 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:00
--- Projekt 1008 (Heinestr 15) ---
      | [12:16:49] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:49] Simulation startet fuer Projekt 1008 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Anlage „CS7800iLW 16": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS5800i AW 12 M + AW 5 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [12:16:49] Simulation beendet, Ergebnis-Kopf-ID 209.
      | [12:16:49] Projekt 1008: 21 CSV-Dateien, 101 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:00
--- Projekt 1011 (test1) ---
      | [12:16:49] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:49] Simulation startet fuer Projekt 1011 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25 (2)" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T (2)': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [12:16:50] Simulation beendet, Ergebnis-Kopf-ID 210.
      | [12:16:50] Projekt 1011: 29 CSV-Dateien, 121 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:00
--- Projekt 1017 (WP_PV-Speicher) ---
      | [12:16:50] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:50] Simulation startet fuer Projekt 1017 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „WPE-I 59 H 400 Premium": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der BHKW-Anlage „BHKW EW K 10 S [K] Heizol" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „eloBLOCK VE 28" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Zum Stichtag 01.01.2026 gab es noch keine Preisversion - es gilt die aelteste vorhandene.
      | [12:16:51] Simulation beendet, Ergebnis-Kopf-ID 211.
      | [12:16:51] Projekt 1017: 21 CSV-Dateien, 114 Skalare.
Projekt 1017: OK, 21 CSV-Dateien, 00:00
--- Projekt 1018 (BHKW Test München) ---
      | [12:16:51] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:51] Simulation startet fuer Projekt 1018 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Speicher-Registry: Puffer 1054175 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „Vitocrossal 200 CM2 raumluftabh�ngig" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | [12:16:51] Simulation beendet, Ergebnis-Kopf-ID 212.
      | [12:16:51] Projekt 1018: 22 CSV-Dateien, 141 Skalare.
Projekt 1018: OK, 22 CSV-Dateien, 00:00
--- Projekt 1021 (TestSpeichernUnter) ---
      | [12:16:51] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:51] Simulation startet fuer Projekt 1021 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „CS7800iLW 12": Der Speicher „allSTOR exclusiv VPS 800/3-7" ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen. Nach der Startfüllung liefe die Quelle leer.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [12:16:52] Simulation beendet, Ergebnis-Kopf-ID 213.
      | [12:16:52] Projekt 1021: 21 CSV-Dateien, 94 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:00
--- Projekt 1023 (Wöhler - Test1) ---
      | [12:16:52] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:52] Simulation startet fuer Projekt 1023 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „CS7800iLW 12": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Anlage „CS6800iAW MB + AW 10 OR-T": Der Erzeuger-Vorlauf 45 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers „Vitocell 140-E 600 Ltr". Der Erzeuger kann den Speicher nie auf Solltemperatur laden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoVIT VKK 186/5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [12:16:52] Simulation beendet, Ergebnis-Kopf-ID 214.
      | [12:16:52] Projekt 1023: 25 CSV-Dateien, 136 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:00
--- Projekt 1024 (Wöhler - Test2) ---
      | [12:16:52] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:52] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der Speicherstufe. Er rechnet deshalb als Mitglied der Stundenschleife an seiner Kaskadenposition mit (Phase B) - ohne Puffer-Senke als reine Heizkreis-Stufe.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [12:16:53] Simulation beendet, Ergebnis-Kopf-ID 215.
      | [12:16:53] Projekt 1024: 26 CSV-Dateien, 157 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:00
--- Projekt 1026 (Beispiel WP WG 1) ---
      | [12:16:53] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:53] Simulation startet fuer Projekt 1026 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [12:16:53] Simulation beendet, Ergebnis-Kopf-ID 216.
      | [12:16:53] Projekt 1026: 29 CSV-Dateien, 137 Skalare.
Projekt 1026: OK, 29 CSV-Dateien, 00:00
--- Projekt 1028 (Beispiel WP WG mit Erdwärme) ---
      | [12:16:53] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:53] Simulation startet fuer Projekt 1028 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [12:16:54] Simulation beendet, Ergebnis-Kopf-ID 217.
      | [12:16:54] Projekt 1028: 29 CSV-Dateien, 137 Skalare.
Projekt 1028: OK, 29 CSV-Dateien, 00:00
--- Projekt 1029 (Beispiel WP WG 1 - Erdwärme) ---
      | [12:16:54] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:54] Simulation startet fuer Projekt 1029 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [12:16:55] Simulation beendet, Ergebnis-Kopf-ID 218.
      | [12:16:55] Projekt 1029: 29 CSV-Dateien, 152 Skalare.
Projekt 1029: OK, 29 CSV-Dateien, 00:00
--- Projekt 1030 (Referenz BHKW-Kaskade (Regressionstest)) ---
      | [12:16:55] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:55] Simulation startet fuer Projekt 1030 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | [12:16:56] Simulation beendet, Ergebnis-Kopf-ID 219.
      | [12:16:56] Projekt 1030: 22 CSV-Dateien, 150 Skalare.
Projekt 1030: OK, 22 CSV-Dateien, 00:00
--- Projekt 1039 (Wärmepumpe WG - BHKW) ---
      | [12:16:56] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:56] Simulation startet fuer Projekt 1039 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | [12:16:56] Simulation beendet, Ergebnis-Kopf-ID 220.
      | [12:16:56] Projekt 1039: 18 CSV-Dateien, 60 Skalare.
Projekt 1039: OK, 18 CSV-Dateien, 00:00
--- Projekt 1043 (Booster-Kette mit Kombi-Speicher) ---
      | [12:16:56] DB-Pfad der App verifiziert: P:\merge4\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [12:16:56] Simulation startet fuer Projekt 1043 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Speicher-Registry: Puffer 1054198 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Speicher-Registry: Puffer 1054197 (Puffer 3000Ltr) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 34,8 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Schichtmodell: Am Puffer 1054197 (Puffer 3000Ltr) liegt die Mindest-Nutztemperatur Brauchwasser mit 55 °C ÜBER der wirksamen Vorlauftemperatur von 10 °C. Keine Schicht könnte sie je erreichen, der Brauchwasserkanal wäre dauerhaft gesperrt. Gerechnet wird mit 10 °C - bitte T_Nutz_BW oder das Temperaturpaar des Speichers berichtigen.
      | Simulation Warnung: Speicher-Registry: Puffer 1054199 (Puffer 3000Ltr (2)) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 34,8 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Wärmesenke: Die Anlage 14786 ist auf PufferKombi gesetzt, hat aber KEINEN Pufferspeicher zugeordnet (WS_ID_Puffer leer). Sie rechnet deshalb auf den HEIZKREIS.
      | Simulation Warnung: Wärmesenke: Die Anlage 14818 hat eine Zweitsenke PufferKombi ohne zugeordneten Pufferspeicher (WS_ID_Puffer2 leer). Die Zweitsenke bleibt unberücksichtigt.
      | Simulation Warnung: Anlage „CS7800iLW 16": Der Pufferspeicher „Puffer 3000Ltr (2)" ist als Wärmequelle gewählt, aber „Quelle unbegrenzt verfügbar" ist gesetzt — die Speicherkopplung ist damit abgeschaltet, gerechnet wird mit konstant 45 °C. Das Häkchen im Quellendialog entfernen, damit die Quelltemperatur dem Speicherzustand folgt.
      | Simulation Warnung: Anlage „CS7800iLW 16" (Rang 1): Der Speicher „Stora B 1000-6 ER 1 B" wird als Pufferspeicher Heizung geladen, sein Klassen-Set lautet aber Brauchwasser. Der Kanal Heizung fehlt — was mit diesem Zweck geladen wird, entlädt der Speicher nie.
      | Simulation Warnung: Speicher „Puffer 3000Ltr": Die Nutztemperatur Brauchwasser 55 °C liegt über dem wirksamen Vorlauf 10 °C. Der Lauf klemmt sie auf 10 °C - sonst wäre der Brauchwasserkanal dauerhaft abgeschaltet.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 8760 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Stromspeicher: Dem Projekt ist kein Stromspeicher zugeordnet - die Speicherrechnung entfällt.
      | [12:16:57] Simulation beendet, Ergebnis-Kopf-ID 221.
      | [12:16:57] Projekt 1043: 34 CSV-Dateien, 197 Skalare.
Projekt 1043: OK, 34 CSV-Dateien, 00:00

Fertig. Gesamtdauer 00:00:10
Erfolgreich: 14 von 14
```
