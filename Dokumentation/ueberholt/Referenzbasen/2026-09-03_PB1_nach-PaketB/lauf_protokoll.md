# Referenzlauf-Protokoll

**Zeitpunkt:** 03.09.2026 01:45:20

**Quelle (produktiv, nur gelesen):** `P:\pa0\Quelle\Kenndaten.sqlite`

**Arbeitskopie (beschrieben):** `P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite`

**Zielordner:** `C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\2026-09-03_PB1_nach-PaketB`

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
Projektwurzel: P:\pb1\src
Zielordner:    C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\2026-09-03_PB1_nach-PaketB
Timeout je Projekt: 300 s

Quelle vorgegeben (--quelle): P:\pa0\Quelle\Kenndaten.sqlite
WARNUNG: Neben der Quelldatenbank liegt Kenndaten.sqlite-wal. Die Datenbank wurde nicht sauber geschlossen; die Kopie enthaelt nur den eingecheckpointeten Stand.
WARNUNG: Neben der Quelldatenbank liegt Kenndaten.sqlite-shm. Die Datenbank wurde nicht sauber geschlossen; die Kopie enthaelt nur den eingecheckpointeten Stand.
Arbeitskopie angelegt: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite (64 MB)
DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite

Schema-Migration der Arbeitskopie ...
  P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
  Zeitpunkt: 03.09.2026 01:45:10
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
      | [01:45:10] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:10] Simulation startet fuer Projekt 1007 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Ablytek 6MN6A270": T_NOCT ist mit 9.340 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Ablytek 6MN6A275": T_NOCT ist mit 9.420 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [01:45:11] Simulation beendet, Ergebnis-Kopf-ID 208.
      | [01:45:11] Projekt 1007: 29 CSV-Dateien, 99 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:00
--- Projekt 1008 (Heinestr 15) ---
      | [01:45:11] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:11] Simulation startet fuer Projekt 1008 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Anlage „CS7800iLW 16": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS5800i AW 12 M + AW 5 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [01:45:11] Simulation beendet, Ergebnis-Kopf-ID 209.
      | [01:45:11] Projekt 1008: 21 CSV-Dateien, 101 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:00
--- Projekt 1011 (test1) ---
      | [01:45:11] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:12] Simulation startet fuer Projekt 1011 ...
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
      | [01:45:12] Simulation beendet, Ergebnis-Kopf-ID 210.
      | [01:45:12] Projekt 1011: 29 CSV-Dateien, 121 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:00
--- Projekt 1017 (WP_PV-Speicher) ---
      | [01:45:12] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:12] Simulation startet fuer Projekt 1017 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „WPE-I 59 H 400 Premium": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der BHKW-Anlage „BHKW EW K 10 S [K] Heizol" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „eloBLOCK VE 28" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Zum Stichtag 01.01.2026 gab es noch keine Preisversion - es gilt die aelteste vorhandene.
      | [01:45:13] Simulation beendet, Ergebnis-Kopf-ID 211.
      | [01:45:13] Projekt 1017: 21 CSV-Dateien, 114 Skalare.
Projekt 1017: OK, 21 CSV-Dateien, 00:00
--- Projekt 1018 (BHKW Test München) ---
      | [01:45:13] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:13] Simulation startet fuer Projekt 1018 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Speicher-Registry: Puffer 1054175 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „Vitocrossal 200 CM2 raumluftabh�ngig" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | [01:45:13] Simulation beendet, Ergebnis-Kopf-ID 212.
      | [01:45:13] Projekt 1018: 22 CSV-Dateien, 141 Skalare.
Projekt 1018: OK, 22 CSV-Dateien, 00:00
--- Projekt 1021 (TestSpeichernUnter) ---
      | [01:45:13] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:13] Simulation startet fuer Projekt 1021 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „CS7800iLW 12": Der Speicher „allSTOR exclusiv VPS 800/3-7" ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen. Nach der Startfüllung liefe die Quelle leer.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [01:45:14] Simulation beendet, Ergebnis-Kopf-ID 213.
      | [01:45:14] Projekt 1021: 21 CSV-Dateien, 94 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:00
--- Projekt 1023 (Wöhler - Test1) ---
      | [01:45:14] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:14] Simulation startet fuer Projekt 1023 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „CS7800iLW 12": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Anlage „CS6800iAW MB + AW 10 OR-T": Der Erzeuger-Vorlauf 45 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers „Vitocell 140-E 600 Ltr". Der Erzeuger kann den Speicher nie auf Solltemperatur laden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoVIT VKK 186/5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [01:45:14] Simulation beendet, Ergebnis-Kopf-ID 214.
      | [01:45:14] Projekt 1023: 25 CSV-Dateien, 136 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:00
--- Projekt 1024 (Wöhler - Test2) ---
      | [01:45:14] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:14] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der Speicherstufe. Er rechnet deshalb als Mitglied der Stundenschleife an seiner Kaskadenposition mit (Phase B) - ohne Puffer-Senke als reine Heizkreis-Stufe.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [01:45:15] Simulation beendet, Ergebnis-Kopf-ID 215.
      | [01:45:15] Projekt 1024: 26 CSV-Dateien, 157 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:00
--- Projekt 1026 (Beispiel WP WG 1) ---
      | [01:45:15] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:15] Simulation startet fuer Projekt 1026 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [01:45:15] Simulation beendet, Ergebnis-Kopf-ID 216.
      | [01:45:15] Projekt 1026: 29 CSV-Dateien, 137 Skalare.
Projekt 1026: OK, 29 CSV-Dateien, 00:00
--- Projekt 1028 (Beispiel WP WG mit Erdwärme) ---
      | [01:45:16] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:16] Simulation startet fuer Projekt 1028 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [01:45:16] Simulation beendet, Ergebnis-Kopf-ID 217.
      | [01:45:16] Projekt 1028: 29 CSV-Dateien, 137 Skalare.
Projekt 1028: OK, 29 CSV-Dateien, 00:00
--- Projekt 1029 (Beispiel WP WG 1 - Erdwärme) ---
      | [01:45:17] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:17] Simulation startet fuer Projekt 1029 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [01:45:17] Simulation beendet, Ergebnis-Kopf-ID 218.
      | [01:45:17] Projekt 1029: 29 CSV-Dateien, 152 Skalare.
Projekt 1029: OK, 29 CSV-Dateien, 00:00
--- Projekt 1030 (Referenz BHKW-Kaskade (Regressionstest)) ---
      | [01:45:18] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:18] Simulation startet fuer Projekt 1030 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | [01:45:18] Simulation beendet, Ergebnis-Kopf-ID 219.
      | [01:45:18] Projekt 1030: 22 CSV-Dateien, 150 Skalare.
Projekt 1030: OK, 22 CSV-Dateien, 00:00
--- Projekt 1039 (Wärmepumpe WG - BHKW) ---
      | [01:45:18] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:18] Simulation startet fuer Projekt 1039 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | [01:45:18] Simulation beendet, Ergebnis-Kopf-ID 220.
      | [01:45:18] Projekt 1039: 18 CSV-Dateien, 60 Skalare.
Projekt 1039: OK, 18 CSV-Dateien, 00:00
--- Projekt 1043 (Booster-Kette mit Kombi-Speicher) ---
      | [01:45:19] DB-Pfad der App verifiziert: P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [01:45:19] Simulation startet fuer Projekt 1043 ...
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
      | [01:45:19] Simulation beendet, Ergebnis-Kopf-ID 221.
      | [01:45:19] Projekt 1043: 34 CSV-Dateien, 197 Skalare.
Projekt 1043: OK, 34 CSV-Dateien, 00:00

Fertig. Gesamtdauer 00:00:10
Erfolgreich: 14 von 14
```


---

# Einordnung: Referenzbasis PB1 — der Stand NACH Paket B (Stufe E2, Modellwahl)

Diese Basis friert den Rechenkern **nach** der Umsetzung von Paket B des
`Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` ein (Stufe **E2**, Nachtrag 2:
Modellwahl je Anlage, Hay-Davies, Huld-Schwachlichtmodell,
Wechselrichter-Teillastkennlinie mit Clipping, Degradation).

**Sie ist byte-gleich zu `2026-09-02_PA1_nach-PaketA`** — und genau das ist ihr
Zweck. Paket B fuegt eine ZWEITE Rechentiefe hinzu, ohne die erste anzutasten:
Alle Bestandsanlagen stehen auf `PV_Modell = NULL`, und NULL heisst EINFACH, also
der Rechenweg aus Paket A. Der Ordner belegt das (Konzept N2.5, Kriterium 1) und
loest PA1 als aktuelle Basis ab.

Vollstaendige Umsetzung und Verifikation:
[`PaketB_E2_Modellwahl_Protokoll.md`](../../WindowsFormsApplication1/Allgemein/Simulation/PaketB_E2_Modellwahl_Protokoll.md).

## Codestand

| | |
|---|---|
| **Commit (gebaut)** | `36acbf1`, Branch `ios_migration` — PB1d, der letzte Codecommit des Pakets |
| **Commits des Pakets** | `f1d16e3` PB1a (Migration 63 + Datenmodell) · `4bd8752` PB1b (Rechenmodell ERWEITERT) · `74f9acf` PB1c (Degradation + Dialogfeld) · `36acbf1` PB1d (Oberflaeche, Importe, Ressourcen) |
| **Bauweg** | `git archive HEAD` nach `P:\pb1\src` (ausserhalb des Repos), dann `MSBuild.exe` aus `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\` mit `-restore -p:Configuration=Debug -p:Platform=x64`. **0 Fehler**; das Warnungsprofil ist zum PA1-Export **identisch** — beide Exporte wurden dafuer mit demselben Befehl neu gebaut: CS0108 2, CS0109 2, NU1510 4, WFO0003 1, WFO1000 30. |
| **Warum ausserhalb des Repos** | Im Arbeitsbaum liegen uncommittete Aenderungen zweier **fremder Sitzungen** (`PvModulPlausibilitaet.cs` samt Aufrufen, `CECDataService`, `PanModule`, `KostenProjektPositionenCtrl`, `MenueCtrl`, `Views/Projekt/*`, `Views/Wizard/*`, beide `Resource*.resx`). Der Export von `HEAD` haelt sie aus der Referenz heraus. |
| **Werkzeug** | `Referenzlauf.exe` aus demselben Export, `P:\pb1\src\Referenzlauf\bin\x64\Debug\net10.0-windows\` |

## Datenquelle

| | |
|---|---|
| **Quelle** | `P:\pa0\Quelle\Kenndaten.sqlite` — **derselbe Snapshot wie PA0 und PA1**, MD5 `47bcefaca0f18d2180ba37786c6cb6b3`, 67 706 880 Byte. Ein neuer Snapshot haette den Bitgleichheitsnachweis mit Datenaenderungen des Anwenders vermischt. |
| **Schemastand** | **61 → 63**: Die Arbeitskopie faehrt Schritt 62 (Paket A) und Schritt 63 (Paket B) hintereinander — im Ablaufprotokoll oben nachzulesen. Beide Schritte sind reines DDL. |
| **Produktive Datei** | `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` **nie beschrieben** — nach dem Lauf unveraendert: Zeitstempel 02.09.2026 22:07:36, 67 706 880 Byte (derselbe Stand wie vor Paket A). |
| **Arbeitskopie** | `P:\pb1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite` (ausserhalb des Repos) |

## Projektmenge

**Dieselben vierzehn Projekte wie PA0 und PA1, 355 CSV:**

```powershell
& $exe lauf --ziel <ordner> --quelle P:\pa0\Quelle\Kenndaten.sqlite `
            --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1026,1028,1029,1030,1039,1043
```

## Der Bitgleichheitsnachweis

```
vergleich 2026-09-02_PA1_nach-PaketA 2026-09-03_PB1_nach-PaketB
  → 14/14 PASS (3 882 476 Werte)  ·  GESAMT: PASS
Byte-/MD5-Vergleich: 355 von 355 CSV identisch, 0 abweichend
  keine Datei nur in PA1, keine nur in PB1
pruefen 2026-09-03_PB1_nach-PaketB
  → GESAMT: plausibel
```

Die drei `pruefen`-Hinweise („Jahressumme 0 — Gewerk aktiviert, aber kein Modul
zugeordnet") bei 1007 (Solarthermie), 1039 (Waermepumpe) und 1043 (PV und
Solarthermie) sind unveraendert der Bestand aus PA0/PA1. Die 24 Warnungen des Laufs
stammen wie dort saemtlich aus 1043.

**Was der Nachweis wert ist.** Er deckt sechs Umbauten auf einmal ab, die alle den
PV-Rechenweg beruehren und dennoch nichts an ihm aendern duerfen:

1. Migrationsschritt **63** legt acht Spalten an (davon `PV_Modell`) — kein DML.
2. `SimulationPV.Berechnung` hat eine **Modellweiche** bekommen; der EINFACH-Zweig ist
   als eigene Schleife Zeichen fuer Zeichen der alte.
3. `SolarCalculator.CalculateHourly` ruft die Sonnengeometrie jetzt aus einer
   **gemeinsamen Hilfsmethode** — die 355 byte-gleichen CSV belegen, dass die
   Auslagerung nicht einmal im letzten Bit etwas verschoben hat.
4. `PvErloesRechner` rechnet mit einem **Degradationsfaktor**, der bei NULL exakt 1,0 ist.
5. `WErzeugerCtrl.AusZeile` liest fuenf Spalten mehr, `SQL_ANLAGE_INSERT` schreibt sie.
6. `PhotovoltaikCtrl`/`-StammCtrl` fuehren die Spalte `Technologie` mit.

## ERWEITERT — die Gegenprobe, dass das neue Modell auch rechnet

Bitgleichheit allein bewiese nur, dass nichts passiert ist. Zwei Smoke-Laeufe auf
**Kopien** derselben Quelle zeigen die andere Haelfte (Projekt 1026, PV-Anlage
„Jinkosolar JKM 260P-60", 20 Module x 260 W = **5,20 kWp**, Neigung 30°, Azimut 0):

| | EINFACH (= PA1/PB1) | A: ERWEITERT, C_SI, WR 4,16 kW | B: ERWEITERT, ohne WR-Daten, Technologie NULL |
|---|---:|---:|---:|
| PV-Erzeugung [MWh/a] | 6,71 | **6,45** (−3,94 %) | **6,94** (+3,37 %) |
| davon genutzt [MWh/a] | 4,34 | 4,27 | 4,37 |
| Eigenverbrauchsquote | 64,68 % | **66,20 %** (+1,52 pp) | **62,97 %** (−1,71 pp) |
| Strombedarfsdeckung | 15,84 % | 15,60 % | 15,96 % |
| Netzrestbezug [MWh/a] | 21,95 | 22,01 | 21,88 |
| max. Einstrahlung [W/m²] | 1 058,93 | 1 080,46 | 1 080,46 |
| DC/AC | — | **1,25** | nicht bestimmbar |
| Volllaststunden | — | 1 240 | 1 335 |
| Wechselrichterverlust [kWh/a] | — | 292,3 | 350,1 |
| Clipping-Verlust [kWh/a] | — | **40,1** | 0,0 |

**Die Richtung stimmt in jeder Zeile.** Lauf A liegt mit −3,9 % im angesagten Band
−1 … −5 %; das Schwachlichtmodell nimmt mehr weg, als Hay-Davies dazugibt, und das
Clipping bei DC/AC 1,25 kappt 40 kWh (0,6 % des Ertrags). Weil Clipping ausgerechnet
die Einspeisespitzen trifft, STEIGT die Eigenverbrauchsquote um 1,5 Punkte — genau die
Wirkung, die das Konzept fuer E2.1 angesagt hat. Lauf B zeigt die Gegenrichtung: ohne
Technologie faellt das Schwachlichtmodell weg, uebrig bleibt der Hay-Davies-Gewinn
(+3,4 %), und die Eigenverbrauchsquote sinkt entsprechend. Die maximale Einstrahlung
steigt in beiden Laeufen von 1 058,9 auf 1 080,5 W/m² — der circumsolare Anteil, den
das isotrope Modell nicht kennt.

**Jede Rueckfallebene meldet sich** (Konzept N2.5, Kriterium 2). Lauf B protokolliert
alle drei:

```
PV-Anlage "Jinkosolar JKM 260P-60" rechnet im erweiterten Modell, das Modul
  fuehrt aber keine Zelltechnologie. Ohne sie gibt es keine Schwachlicht-
  Koeffizienten; gerechnet wird die Modulformel des einfachen Modells
  (Nennleistung, gamma_PMP, NOCT) auf der Hay-Davies-Einstrahlung. …
PV-Anlage "…": Die Wechselrichter-Kennlinie ist nicht vollstaendig gepflegt.
  Gerechnet wird mit 0.940 / 0.975 / 0.970 bei 10 / 50 / 100 % Auslastung …
PV-Anlage "…": Es ist keine Wechselrichter-Nennleistung gepflegt. Gerechnet wird
  OHNE Clipping; die Auslastung der Kennlinie bezieht sich ersatzweise auf die
  DC-Nennleistung der Anlage (5.20 kWp).
PV-Anlage "…" (Modell erweitert): DC/AC … Jahresertrag … Volllaststunden,
  Wechselrichterverlust …, Clipping-Verlust ….
```

Die Smoke-Ordner sind **bewusst nicht** abgelegt: Sie sind keine Basis, sondern eine
Wirkprobe auf praeparierten Kopien. Ihre Zahlen stehen hier und im Paket-B-Protokoll.

## Wirtschaftlichkeit: die INEKON-Referenz bleibt unveraendert

Der Referenzlauf deckt den Rechenkern ab, nicht die Wirtschaftlichkeit. Fuer sie gilt
die P6-Referenz „INEKON Schulung 01" (Pruefstand `kd1runner`, Modus `pv6`), gegen den
Paket-B-Build gerechnet:

```
PV6-SMOKE: 28 PASS, 0 FAIL
  I3 Ueberschuss ±1 %      (91867 gegen 92568: -0.76 %)
  I4 Volleinspeisung ±1 %  (-23087 gegen -22979: -0.47 %)
```

Zahl fuer Zahl dieselben Werte wie im Protokoll der Etappe Ä24 — die Degradation
steht auf NULL, und NULL liefert den Faktor exakt 1,0.

## Offene Punkte nach dieser Basis

1. **Sichtabnahme** der drei Masken durch Philipp: `Form_PV` (Panel jetzt zwei Spalten,
   vier Zeilen, Maske 57 px hoeher), `Form_PVModell` (neu) und `Form_AdminPV`
   (Technologie-Auswahl unter dem NOCT-Feld).
2. **Katalogpflege** — solange `Technologie` in allen sechs Referenzmodulen leer ist,
   faellt das erweiterte Modell auf die Modulformel zurueck; `T_NOCT` und `gamma_PMP`
   sind unabhaengig davon weiter unplausibel (Paket-A-Befund, Reparaturskript unter
   `sql/pv_katalog/` wartet auf Freigabe).
3. **Schemastand 63.** `.wpx`-Pakete mit Stand 62 werden abgewiesen — systemimmanent.
4. Diese Basis bleibt gueltig, solange keine Anlage produktiv auf ERWEITERT steht.
   **Sobald der Anwender das Modell umstellt, ist ein Basiswechsel faellig.**