# Referenzlauf-Protokoll

**Zeitpunkt:** 02.09.2026 23:12:44

**Quelle (produktiv, nur gelesen):** `P:\pa0\Quelle\Kenndaten.sqlite`

**Arbeitskopie (beschrieben):** `P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite`

**Zielordner:** `C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\2026-09-02_PA1_nach-PaketA`

**Gesamtdauer:** 00:00:21  |  **Timeout je Projekt:** 900 s

**Warnungen:** 24  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 21 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:02 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:01 | 21 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:01 | 22 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:01 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:01 | 26 | OK |
| 1026 | Beispiel WP WG 1 | Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1028 | Beispiel WP WG mit Erdwärme | Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1029 | Beispiel WP WG 1 - Erdwärme | Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:01 | 22 | OK |
| 1039 | Wärmepumpe WG - BHKW | Tools: Wärmepumpe / Anlagen: BHKW | per --projekte vorgegeben | 00:01 | 18 | OK |
| 1043 | Booster-Kette mit Kombi-Speicher | Tools: Wärmepumpe, Heizkessel, Solarthermie, Photovoltaik, Stromspeicher / Anlagen: WP,Kessel,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:01 | 34 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: P:\pa1\src
Zielordner:    C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\2026-09-02_PA1_nach-PaketA
Timeout je Projekt: 900 s

Quelle vorgegeben (--quelle): P:\pa0\Quelle\Kenndaten.sqlite
Arbeitskopie angelegt: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite (64 MB)
DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite

Schema-Migration der Arbeitskopie ...
  P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
  Zeitpunkt: 02.09.2026 23:12:23
  Schemastand vorher: 61   (Zielstand 62)
  Schritt 62  PV-Anlagenparameter (PV_WrWirkungsgrad, PV_Systemverluste) an Tab_Energieanlagen anlegen (Paket A, Stufe E1.3): OK
          - Tab_Energieanlagen.PV_WrWirkungsgrad: angelegt
          - Tab_Energieanlagen.PV_Systemverluste: angelegt
          - 62: 2 Spalte(n) (PV_WrWirkungsgrad, PV_Systemverluste) an Tab_Energieanlagen sichergestellt. KEIN DML: beide Spalten bleiben NULL, und NULL heisst 0,95 (Wechselrichter-Wirkungsgrad) bzw. 0 % (Systemverluste) - genau der bisher fest verdrahtete Rechenweg. KEIN Rechenergebnis aendert sich.
  Schemastand nachher: 62   (Zielstand 62)
Migration: ERFOLG (Zielstand 62).

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
      | [23:12:24] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:25] Simulation startet fuer Projekt 1007 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Ablytek 6MN6A270": T_NOCT ist mit 9.340 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Ablytek 6MN6A275": T_NOCT ist mit 9.420 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [23:12:26] Simulation beendet, Ergebnis-Kopf-ID 208.
      | [23:12:26] Projekt 1007: 29 CSV-Dateien, 99 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:01
--- Projekt 1008 (Heinestr 15) ---
      | [23:12:26] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:26] Simulation startet fuer Projekt 1008 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Anlage „CS7800iLW 16": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS5800i AW 12 M + AW 5 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [23:12:27] Simulation beendet, Ergebnis-Kopf-ID 209.
      | [23:12:27] Projekt 1008: 21 CSV-Dateien, 101 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:01
--- Projekt 1011 (test1) ---
      | [23:12:28] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:28] Simulation startet fuer Projekt 1011 ...
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
      | [23:12:29] Simulation beendet, Ergebnis-Kopf-ID 210.
      | [23:12:29] Projekt 1011: 29 CSV-Dateien, 121 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:02
--- Projekt 1017 (WP_PV-Speicher) ---
      | [23:12:30] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:30] Simulation startet fuer Projekt 1017 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „WPE-I 59 H 400 Premium": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der BHKW-Anlage „BHKW EW K 10 S [K] Heizol" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „eloBLOCK VE 28" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Zum Stichtag 01.01.2026 gab es noch keine Preisversion - es gilt die aelteste vorhandene.
      | [23:12:31] Simulation beendet, Ergebnis-Kopf-ID 211.
      | [23:12:31] Projekt 1017: 21 CSV-Dateien, 114 Skalare.
Projekt 1017: OK, 21 CSV-Dateien, 00:01
--- Projekt 1018 (BHKW Test München) ---
      | [23:12:31] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:31] Simulation startet fuer Projekt 1018 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Speicher-Registry: Puffer 1054175 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „Vitocrossal 200 CM2 raumluftabh�ngig" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | [23:12:32] Simulation beendet, Ergebnis-Kopf-ID 212.
      | [23:12:32] Projekt 1018: 22 CSV-Dateien, 141 Skalare.
Projekt 1018: OK, 22 CSV-Dateien, 00:01
--- Projekt 1021 (TestSpeichernUnter) ---
      | [23:12:32] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:32] Simulation startet fuer Projekt 1021 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „CS7800iLW 12": Der Speicher „allSTOR exclusiv VPS 800/3-7" ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen. Nach der Startfüllung liefe die Quelle leer.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [23:12:33] Simulation beendet, Ergebnis-Kopf-ID 213.
      | [23:12:33] Projekt 1021: 21 CSV-Dateien, 94 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:01
--- Projekt 1023 (Wöhler - Test1) ---
      | [23:12:33] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:33] Simulation startet fuer Projekt 1023 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Warnung: Anlage „CS7800iLW 12": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Anlage „CS6800iAW MB + AW 10 OR-T": Der Erzeuger-Vorlauf 45 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers „Vitocell 140-E 600 Ltr". Der Erzeuger kann den Speicher nie auf Solltemperatur laden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoVIT VKK 186/5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [23:12:34] Simulation beendet, Ergebnis-Kopf-ID 214.
      | [23:12:34] Projekt 1023: 25 CSV-Dateien, 136 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:01
--- Projekt 1024 (Wöhler - Test2) ---
      | [23:12:35] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:35] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der Speicherstufe. Er rechnet deshalb als Mitglied der Stundenschleife an seiner Kaskadenposition mit (Phase B) - ohne Puffer-Senke als reine Heizkreis-Stufe.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [23:12:35] Simulation beendet, Ergebnis-Kopf-ID 215.
      | [23:12:35] Projekt 1024: 26 CSV-Dateien, 157 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:01
--- Projekt 1026 (Beispiel WP WG 1) ---
      | [23:12:36] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:36] Simulation startet fuer Projekt 1026 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [23:12:37] Simulation beendet, Ergebnis-Kopf-ID 216.
      | [23:12:37] Projekt 1026: 29 CSV-Dateien, 137 Skalare.
Projekt 1026: OK, 29 CSV-Dateien, 00:01
--- Projekt 1028 (Beispiel WP WG mit Erdwärme) ---
      | [23:12:37] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:38] Simulation startet fuer Projekt 1028 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [23:12:38] Simulation beendet, Ergebnis-Kopf-ID 217.
      | [23:12:38] Projekt 1028: 29 CSV-Dateien, 137 Skalare.
Projekt 1028: OK, 29 CSV-Dateien, 00:01
--- Projekt 1029 (Beispiel WP WG 1 - Erdwärme) ---
      | [23:12:39] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:39] Simulation startet fuer Projekt 1029 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": T_NOCT ist mit 9.014 Grad C nicht plausibel (erwartet werden 20 bis 60 Grad C). Gerechnet wird mit dem Rueckfall 45 Grad C. Der Wert laesst sich im Modulkatalog pflegen.
      | Simulation Hinweis: PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [23:12:40] Simulation beendet, Ergebnis-Kopf-ID 218.
      | [23:12:40] Projekt 1029: 29 CSV-Dateien, 152 Skalare.
Projekt 1029: OK, 29 CSV-Dateien, 00:01
--- Projekt 1030 (Referenz BHKW-Kaskade (Regressionstest)) ---
      | [23:12:40] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:41] Simulation startet fuer Projekt 1030 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | [23:12:41] Simulation beendet, Ergebnis-Kopf-ID 219.
      | [23:12:41] Projekt 1030: 22 CSV-Dateien, 150 Skalare.
Projekt 1030: OK, 22 CSV-Dateien, 00:01
--- Projekt 1039 (Wärmepumpe WG - BHKW) ---
      | [23:12:42] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:42] Simulation startet fuer Projekt 1039 ...
      | Simulation Hinweis: Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..
      | [23:12:42] Simulation beendet, Ergebnis-Kopf-ID 220.
      | [23:12:43] Projekt 1039: 18 CSV-Dateien, 60 Skalare.
Projekt 1039: OK, 18 CSV-Dateien, 00:01
--- Projekt 1043 (Booster-Kette mit Kombi-Speicher) ---
      | [23:12:43] DB-Pfad der App verifiziert: P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [23:12:43] Simulation startet fuer Projekt 1043 ...
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
      | [23:12:44] Simulation beendet, Ergebnis-Kopf-ID 221.
      | [23:12:44] Projekt 1043: 34 CSV-Dateien, 197 Skalare.
Projekt 1043: OK, 34 CSV-Dateien, 00:01

Fertig. Gesamtdauer 00:00:21
Erfolgreich: 14 von 14
```


---

# Einordnung: Referenzbasis PA1 — der Stand NACH Paket A (B1 + E1)

Diese Basis friert den Rechenkern **nach** der Umsetzung von Paket A des
`Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` ein (Befund **B1** Zeitbasis
UTC/Ortszeit und Stufe **E1** „Eine Wahrheit"). Sie löst
`2026-09-02_PA0_vor-PaketA` als aktuelle Basis ab und ist zugleich die
**Bitgleichheits-Basis für das Modell EINFACH** des Pakets B (Konzept N2.5,
Kriterium 1).

Vollständige Umsetzung, Verifikation und die Zuordnung jedes einzelnen Deltas:
[`PaketA_Zeitbasis_E1_Protokoll.md`](../../WindowsFormsApplication1/Allgemein/Simulation/PaketA_Zeitbasis_E1_Protokoll.md).

## Codestand

| | |
|---|---|
| **Commit (gebaut)** | `7c622b1`, Branch `ios_migration` — PA1c, der letzte Codecommit des Pakets |
| **Commits des Pakets** | `36c5401` PA1a (Migration 62 + Anlagenparameter) · `aced014` PA1b (Zeitbasis + E1.1/E1.2/E1.5) · `7c622b1` PA1c (Oberfläche, Ressourcen) |
| **Bauweg** | `git archive HEAD` nach `P:\pa1\src` (außerhalb des Repos), dann `MSBuild.exe` aus `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\` mit `-restore -p:Configuration=Debug -p:Platform=x64`. **0 Fehler**; das Warnungsprofil ist zum Bestand **identisch** (CS0108 2, CS0109 2, NU1510 2, WFO0003 1, WFO1000 30 — in beiden Ständen gleich). |
| **Warum außerhalb des Repos** | Im Arbeitsbaum lief Visual Studio, und eine **fremde Sitzung** hält uncommittete Änderungen (`MenueCtrl.cs`, beide `Resource*.resx`, `Form_Klimadaten.Designer.cs`, `Views/Projekt/*`, `Views/Wizard/*`, `KD6_Protokoll.md`). Der Export von `HEAD` hält sie aus der Referenz heraus. |
| **Werkzeug** | `Referenzlauf.exe` aus demselben Export, `P:\pa1\src\Referenzlauf\bin\x64\Debug\net10.0-windows\` |

## Datenquelle

| | |
|---|---|
| **Quelle** | `P:\pa0\Quelle\Kenndaten.sqlite` — **derselbe Snapshot wie PA0**, MD5 `47bcefaca0f18d2180ba37786c6cb6b3`, 67 706 880 Byte. Ein neuer Snapshot hätte den Vergleich mit Datenänderungen des Anwenders vermischt. |
| **Schemastand** | **61 → 62**: Die Migration der Arbeitskopie fährt den neuen Schritt 62 (PV-Anlagenparameter) — im Ablaufprotokoll oben als „Schemastand vorher: 61 / nachher: 62" zu sehen. |
| **Produktive Datei** | `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` **nie beschrieben** — nach dem Lauf unverändert: Zeitstempel 02.09.2026 22:07:36, 67 706 880 Byte, **SchemaVersion 61**, in `Tab_Energieanlagen` weiterhin nur `PV_Leistung` als `PV_*`-Spalte. |
| **Arbeitskopie** | `P:\pa1\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite` (außerhalb des Repos, per Reflection umgebogen und je Kindprozess verifiziert) |

## Projektmenge

**Dieselben vierzehn Projekte wie PA0, 355 CSV:**

```powershell
& $exe lauf --ziel <ordner> --quelle P:\pa0\Quelle\Kenndaten.sqlite `
            --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1026,1028,1029,1030,1039,1043
```

## Selbstvergleich — die Basis ist reproduzierbar

Zweiter Lauf desselben Codes auf derselben Quelle:

```
vergleich 2026-09-02_PA1_nach-PaketA P:\pa1\selbstvergleich
  → 14/14 PASS (3 882 476 Werte)
Byte-/MD5-Vergleich: 355 von 355 CSV identisch
pruefen 2026-09-02_PA1_nach-PaketA
  → GESAMT: plausibel (keine NaN/Inf, Rasterlängen 8760/35040 korrekt)
```

Die drei `pruefen`-Hinweise („Jahressumme 0 — Gewerk aktiviert, aber kein Modul
zugeordnet") bei 1007 (Solarthermie), 1039 (Wärmepumpe) und 1043 (PV und
Solarthermie) sind unverändert der Bestand aus PA0.

## Vergleich gegen `2026-09-02_PA0_vor-PaketA`

**Der Toleranzvergleich meldet FAIL in allen 14 Projekten — und das ist die
Ansage des Konzepts.** B1 verschiebt die Stundentemperatur in JEDEM Projekt, und
die Toleranz der Suite (1e-4 relativ) ist enger als jede der beabsichtigten
Änderungen. Ein PASS wäre hier der Fehlschlag gewesen.

Der vollständige Skalarvergleich (jeder Schlüssel jeder `aggregate.csv`, ohne die
volatilen Autowert-IDs) zählt **391 geänderte Skalare**; **kein Schlüssel ist neu
oder entfallen**. Jedes Delta ist zugeordnet — die vier Familien:

| # | Familie | Betroffen | Größenordnung |
|---|---|---|---|
| 1 | **Stundentemperatur selbst** (B1) | 14 von 14 | Summe −8,85 K (Stuttgart) bzw. −4,48 K (München) auf 8 760 Stunden = **die zwei Umstellstunden**, −0,010 % / −0,005 % |
| 2 | **PV-Jahreserzeugung** (E1.1 + E1.4) | 1007, 1011, 1026, 1028, 1029 | **+0,0013 % bis +0,0468 %** — höchstens der Katalogfaktor, wie gefordert |
| 3 | **Paarung Erzeugung ↔ Bedarf** (B1) | 1007, 1026, 1028, 1029 (1011 untauglich) | EVQ **−0,95 pp bis +0,12 pp**, Speicherfüllstand bis **−2,3 %** |
| 4 | **temperaturabhängige Wärmeseite** (B1) | alle WP-/Kesselprojekte | Bivalenzpunkt bis +6 %, Kesselwärme bis +0,29 %, Erdreich ≈ 0 |

### Familie 1 — der unabhängige Gegenbeweis

`Vektor.stundentemperatur.Summe` (und `wp_quellentemperatur`) ändert sich um
**genau** die zwei Stunden, die die Verschiebungsregel doppelt nutzt bzw.
auslässt:

| Region | PA0 | PA1 | Δ |
|---|---:|---:|---:|
| Stuttgart (11 Projekte) | 86 502,49 | 86 493,64 | **−8,85 K** |
| München (1018, 1030) | 83 874,19 | 83 869,71 | **−4,48 K** |

Der Prüfharness misst dieselbe Differenz unabhängig an derselben Reihe. Reihe und
Rechenkern sind nachweislich um dasselbe verschoben.

### Familie 2 — PV-Jahreserzeugung

| Projekt | Modul (Neigung) | Katalogfaktor | PA0 [kWh] | PA1 [kWh] | Δ |
|---|---|---:|---:|---:|---:|
| 1007 | Ablytek 6MN6A270 (0°) | **+0,0332 %** | 6 053,139 | 6 055,971 | **+0,0468 %** |
| 1011 | Jinkosolar JKM 260P-60 (0°, 2 Felder) | 0,0000 % | 17 986,629 | 17 988,696 | **+0,0115 %** |
| 1026 / 1028 / 1029 | Jinkosolar JKM 260P-60 (30°) | 0,0000 % | 6 713,372 | 6 713,459 | **+0,0013 %** |
| 1043 | Gewerk ohne Modul | — | 0,00 | 0,00 | **0** |

Die Jinkosolar-Projekte belegen den **reinen E1.4-Anteil** (1-basierter
Tagindex): Ihr Katalog ist konsistent, γ = 0, `T_NOCT` fällt auf 45 °C zurück —
E1.1 und E1.2 sind dort rechnerisch wirkungslos. 1007 ist die Summe aus
Katalogfaktor und demselben geometrieabhängigen Tagindex-Anteil.
**Ein T_NOCT-Effekt tritt nirgends auf:** In allen sechs Modulen steht in
`T_NOCT` der Wert von `I_Kurzschluss` (9,014 / 9,34 / 9,42), also außerhalb des
Fensters 20…60 °C — der Rückfall 45 °C greift überall und das Protokoll sagt es
je Modul.

### Familie 3 — Eigenverbrauch, Überschuss, Speicher

| Projekt | PV genutzt | PV Überschuss | EVQ PA0 → PA1 | Autarkie PA0 → PA1 |
|---|---:|---:|---:|---:|
| 1007 | −1,06 % | **+6,79 %** | 85,95 % → **85,00 %** | 9,28 % → 9,18 % |
| 1026 / 1028 | +0,09 % | −0,15 % | 64,56 % → **64,62 %** | 15,84 % → 15,84 % |
| 1029 | +0,20 % | −0,33 % | 61,89 % → **62,02 %** | 18,63 % → 18,66 % |
| 1011 | +0,01 % | 0 | 100 % → 100 % (untauglich) | 0,26 % → 0,26 % |

Solarthermie 1011: Jahresertrag 643,58 → 643,71 kWh (**+0,02 %**), Deckungsgrad
unverändert 0,01 %.

**Die Wirkung ist kleiner als die Konzeptschätzung („mehrere Prozentpunkte").**
Der Grund liegt in den Daten: Die Referenzprojekte fahren synthetische
Wochenprofile, und die sind über den Tag flach — eine Verschiebung um 1 bis 2
Stunden bewegt dort wenig. Am klarsten zeigt sich der Effekt an den
Speichergrößen (Summe Speicherfüllstand 1026/1028 −2,22 %, 1029 −2,33 %;
PV-Ladung des Pufferspeichers 1026/1028 14,26 → 12,99 kWh = −8,9 %).

### Familie 4 — und was sich NICHT geändert hat

| Projekt | geänderte Skalare | was |
|---|---:|---|
| 1017 | **1** | nur `Vektor.stundentemperatur.Summe` |
| 1018 | **1** | nur `Vektor.stundentemperatur.Summe` |
| 1030 | **1** | nur `Vektor.stundentemperatur.Summe` |
| 1039 | **2** | nur die beiden Temperaturreihen |

1018 und 1030 sind die BHKW-/Kesselprojekte: Ihr Wärmebedarf hängt am Tagesmittel
aus `Tab_Klimadaten`, nicht an der Stundenreihe. **BHKW-Stromproduktion,
Kesselwärme, Vollbenutzungsstunden und die ganze KWKG-Kette bleiben auf die
letzte Stelle gleich** — der Beweis, dass Paket A außerhalb von PV, Solarthermie
und Stundentemperatur nichts bewegt.

Die größten Ausschläge der Wärmeseite: Bivalenzpunkt 1008 6,11 → 6,48 °C
(+6,06 %) und 1024 20,73 → 21,20 °C (+2,27 %), Puffer-Entladung Brauchwasser 1024
−1,76 %, Kesselwärme 1043 +0,29 % / 1026 +0,22 % / 1023 +0,09 %,
Erdreich-Jahresentzug 1029 −0,001 %.

## Warnungen des Laufs (24, alle Bestand)

Unverändert die 24 Warnungen aus **1043** (Puffer ohne Temperaturpaar,
Mindest-Nutztemperatur über wirksamem Vorlauf, `PufferKombi` ohne Puffer,
`WQ_Unbegrenzt` an „CS7800iLW 16", Klassen-Set-Konflikt) — dieselbe Menge wie in
PA0. **Paket A bringt keine neue Warnung.**

Neu unter den **Hinweisen** (nicht gezählt), je Lauf einmal:

* `Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10.`
  — in allen 14 Projekten;
* `PV-Modul "…": T_NOCT ist mit 9.014 Grad C nicht plausibel … Rueckfall 45 Grad C`
  — in den fünf PV-Projekten;
* `PV-Modul "Jinkosolar JKM 260P-60": Es ist kein Temperaturgang hinterlegt (gamma_PMP = 0)`
  — in 1011, 1026, 1028, 1029.

## Offene Punkte nach dieser Basis

1. **Katalogpflege `T_NOCT`** für die sechs Referenzmodule: Solange dort der Wert
   von `I_Kurzschluss` steht, ist E1.2 rechnerisch wirkungslos. Nach der Pflege
   sind ±0,5 % Jahresertrag zu erwarten — dann wird ein neuer Basiswechsel
   nötig.
2. **Sichtabnahme** der beiden PV-Masken (`Form_PV`, `Form_AdminPV`) durch
   Philipp.
3. **Paket B (Stufe E2)** setzt auf dieser Basis auf; für das Modell EINFACH gilt
   Bitgleichheit gegen PA1 als Abnahmekriterium.
